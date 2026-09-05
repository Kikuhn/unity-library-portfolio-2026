using System;
using System.Collections.Generic;
using Pan.Event;
using UnityEngine;



namespace Pan.Tan.PanEvent
{
    /// <summary>
    /// Tan별 EventAble host를 첫 접근 시에만 만들고 despawn 시 회수합니다.
    /// </summary>
    public sealed partial class EventTanHostRegistry : IDisposable
    {
        private sealed class EventHost
        {
            private EventTanHandle owner;
            private EventAble eventAble;
            private bool retiring;
            private bool releasing;



            public bool HasEventAble => eventAble != null;
            public bool IsRetiring => retiring;
            public EventTanHandle Owner => owner;
            public EventAble AllocatedEventAble => eventAble;



            public void Acquire(EventTanHandle nextOwner)
            {
                owner = nextOwner;
                eventAble = null;
                retiring = false;
                releasing = false;
            }

            public bool Owns(EventTanHandle candidate) => owner.Equals(candidate);

            public void BeginRetirement() => retiring = true;

            public EventAble GetOrCreate(int initialCapacity)
            {
                if (retiring)
                {
                    return eventAble ?? throw new InvalidOperationException(
                        "회수 중인 EventHost에는 새 EventAble을 만들 수 없습니다.");
                }

                return eventAble ??= new EventAble(owner, initialCapacity, EventAbleTableAllocationMode.Lazy);
            }

            public void Dispatch(in TanFact fact)
            {
                if (eventAble == null) { return; }
                TanFactSignal signal = new TanFactSignal(in fact);
                eventAble.DispatchLocal(in signal);
            }

            public void Release()
            {
                if (releasing) { return; }
                retiring = true;
                releasing = true;
                eventAble?.Reset();
                eventAble = null;
                owner = default;
                retiring = false;
                releasing = false;
            }

            public void Quarantine()
            {
                owner = default;
                retiring = true;
                releasing = true;
            }
        }



        private readonly ITanRuntimeBackend runtime;
        private readonly int initialEventTableCapacity;
        private readonly Dictionary<TanKey, EventHost> activeHosts;
        private readonly Stack<EventHost> pooledHosts;
        private readonly List<EventHost> quarantinedHosts;
        private bool disposed;



        public EventTanHostRegistry(
            ITanRuntimeBackend runtime,
            int initialEventTableCapacity = 2,
            int initialHostCapacity = 64)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            if (initialEventTableCapacity < 0) { throw new ArgumentOutOfRangeException(nameof(initialEventTableCapacity)); }
            if (initialHostCapacity < 0) { throw new ArgumentOutOfRangeException(nameof(initialHostCapacity)); }

            this.initialEventTableCapacity = initialEventTableCapacity;
            activeHosts = new Dictionary<TanKey, EventHost>(initialHostCapacity);
            pooledHosts = new Stack<EventHost>(initialHostCapacity);
            quarantinedHosts = new List<EventHost>();
            runtime.FactRaised += OnTanFact;
#if UNITY_EDITOR
            EditorRegister(this);
#endif
        }



        public int ActiveHostCount => activeHosts.Count;
        public int PooledHostCount => pooledHosts.Count;
        public int QuarantinedHostCount => quarantinedHosts.Count;



        public EventTanHandle Wrap(TanHandle tan)
        {
            ThrowIfDisposed();
            if (!tan.IsOwnedBy(runtime))
            {
                throw new ArgumentException("다른 Tan runtime의 handle은 이 registry로 감쌀 수 없습니다.", nameof(tan));
            }
            if (!tan.IsAlive) { throw new InvalidOperationException($"stale Tan handle은 감쌀 수 없습니다: {tan.Key}"); }
            return new EventTanHandle(this, tan);
        }

        public bool TryWrap(TanHandle tan, out EventTanHandle handle)
        {
            if (disposed || !tan.IsOwnedBy(runtime) || !tan.IsAlive)
            {
                handle = default;
                return false;
            }

            handle = new EventTanHandle(this, tan);
            return true;
        }

        public void Dispose()
        {
            if (disposed) { return; }
            runtime.FactRaised -= OnTanFact;
#if UNITY_EDITOR
            EditorUnregister(this);
#endif
            foreach (EventHost host in activeHosts.Values)
            {
                try
                {
#if UNITY_EDITOR
                    EditorNotifyEventAbleReleasing(host.Owner.Key, host.AllocatedEventAble);
#endif
                    host.BeginRetirement();
                    host.Release();
                }
                catch (Exception exception)
                {
                    QuarantineHost(host);
                    Debug.LogException(exception);
                }
            }

            activeHosts.Clear();
            pooledHosts.Clear();
            quarantinedHosts.Clear();
            disposed = true;
        }



        internal bool IsHandleAlive(EventTanHandle owner) =>
            !disposed && owner.Tan.IsOwnedBy(runtime) &&
            (owner.Tan.IsAlive ||
                activeHosts.TryGetValue(owner.Key, out EventHost host) &&
                host.IsRetiring &&
                host.HasEventAble &&
                host.Owns(owner));

        internal EventAble GetOrCreateEventAble(EventTanHandle owner)
        {
            ThrowIfDisposed();
            if (!owner.Tan.IsOwnedBy(runtime))
            {
                throw new InvalidOperationException("다른 Tan runtime의 handle입니다.");
            }
            if (activeHosts.TryGetValue(owner.Key, out EventHost host))
            {
                if (!host.Owns(owner)) { throw new InvalidOperationException("EventHost 소유권이 일치하지 않습니다."); }
                if (!owner.Tan.IsAlive && !host.IsRetiring)
                {
                    throw new InvalidOperationException($"stale Tan handle입니다: {owner.Key}");
                }

                return host.GetOrCreate(initialEventTableCapacity);
            }

            if (!owner.Tan.IsAlive) { throw new InvalidOperationException($"stale Tan handle입니다: {owner.Key}"); }
            host = pooledHosts.Count > 0 ? pooledHosts.Pop() : new EventHost();
            host.Acquire(owner);
            activeHosts.Add(owner.Key, host);
            return host.GetOrCreate(initialEventTableCapacity);
        }

        internal bool TryGetAllocatedEventAble(
            EventTanHandle owner,
            out EventAble eventAble)
        {
            if (!disposed &&
                activeHosts.TryGetValue(owner.Key, out EventHost host) &&
                host.Owns(owner) &&
                host.HasEventAble)
            {
                eventAble = host.AllocatedEventAble;
                return eventAble != null;
            }

            eventAble = null;
            return false;
        }



        private void QuarantineHost(EventHost host)
        {
            host.Quarantine();
            if (!quarantinedHosts.Contains(host)) { quarantinedHosts.Add(host); }
        }

        private void ThrowIfDisposed()
        {
            if (disposed) { throw new ObjectDisposedException(nameof(EventTanHostRegistry)); }
        }
    }
}
