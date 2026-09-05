using System;
using System.Collections.Generic;
using Pan.Event;
using UnityEngine;



namespace Pan.HighDensityElement.PanEvent
{
    /// <summary>
    /// backend 종류와 무관하게 Element 명령·snapshot과 PanEvent EventAble 조립을 함께 제공하는 경계입니다.
    /// </summary>
    public interface IEventElement : IElement, IEventAble
    {
    }



    /// <summary>
    /// 하나의 Element fact를 해당 Element의 local EventAble에만 전달하는 값형식 envelope입니다.
    /// </summary>
    public readonly struct ElementFactSignal
    {
        public ElementFactSignal(in ElementFact fact) => Fact = fact;
        public ElementFact Fact { get; }
    }



    /// <summary>
    /// generation-safe Element handle과 lazy EventAble 접근을 결합한 gameplay용 값형식 handle입니다.
    /// raw EventAble 참조는 보관하지 말고 이 handle을 다시 검증해 사용해야 합니다.
    /// </summary>
    public readonly struct EventElementHandle : IEventElement, IEquatable<EventElementHandle>
    {
        internal EventElementHandle(EventElementHostRegistry registry, ElementHandle element)
        {
            Registry = registry;
            Element = element;
        }



        private EventElementHostRegistry Registry { get; }
        public ElementHandle Element { get; }
        public ElementKey Key => Element.Key;
        public ElementLifecycle Lifecycle => Element.Lifecycle;
        public ElementCapabilities Capabilities => Element.Capabilities;
        public ElementPhysicsCapability PhysicsCapabilities => Element.PhysicsCapabilities;
        public bool IsAlive => Element.IsAlive;



        /// <summary>
        /// 살아 있는 Element에 대응하는 lazy EventAble을 반환합니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">Element가 despawn됐거나 다른 registry 소유일 때 발생합니다.</exception>
        public EventAble EventAble
        {
            get
            {
                if (Registry == null || !Element.IsAlive)
                {
                    throw new InvalidOperationException($"stale Element handle에서는 EventAble을 사용할 수 없습니다: {Key}");
                }

                return Registry.GetOrCreateEventAble(this);
            }
        }



        public bool TryGetSnapshot(out ElementSnapshot snapshot) => Element.TryGetSnapshot(out snapshot);



        /// <summary>
        /// EventAble과 같은 generation 검증을 유지하면서 표시용 보간 pose를 조회합니다.
        /// </summary>
        public bool TryGetRenderSnapshot(float interpolationAlpha, out ElementRenderSnapshot snapshot) =>
            Element.TryGetRenderSnapshot(interpolationAlpha, out snapshot);



        public bool TrySubmit<TCommand>(in TCommand command) where TCommand : unmanaged, IElementCommand =>
            Element.TrySubmit(in command);



        public void Submit<TCommand>(in TCommand command) where TCommand : unmanaged, IElementCommand =>
            Element.Submit(in command);



        public bool Equals(EventElementHandle other) => ReferenceEquals(Registry, other.Registry) && Element.Equals(other.Element);
        public override bool Equals(object obj) => obj is EventElementHandle other && Equals(other);
        public override int GetHashCode() => Element.GetHashCode();
        public static bool operator ==(EventElementHandle left, EventElementHandle right) => left.Equals(right);
        public static bool operator !=(EventElementHandle left, EventElementHandle right) => !left.Equals(right);
    }



    /// <summary>
    /// Element별 EventAble host를 첫 접근 시에만 만들고 despawn 뒤 안전하게 회수합니다.
    /// gameplay callback 예외는 기록하되 ElementWorld의 generation 전이를 막지 않습니다.
    /// </summary>
    public sealed partial class EventElementHostRegistry : IDisposable
    {
        private sealed class EventHost
        {
            private EventElementHandle owner;
            private EventAble eventAble;
            private bool releasing;



            public ElementKey Key => owner.Key;
            public bool HasEventAble => eventAble != null;
            public EventAble AllocatedEventAble => eventAble;



            public void Acquire(EventElementHandle nextOwner)
            {
                owner = nextOwner;
                eventAble = null;
                releasing = false;
            }



            public EventAble GetOrCreate(int initialCapacity)
            {
                if (releasing && eventAble == null)
                {
                    throw new InvalidOperationException("회수 중인 EventHost에는 새 EventAble을 만들 수 없습니다.");
                }
                return eventAble ??= new EventAble(owner, initialCapacity, EventAbleTableAllocationMode.Lazy);
            }



            public void Dispatch(in ElementFact fact)
            {
                if (eventAble == null) { return; }
                ElementFactSignal signal = new ElementFactSignal(in fact);
                eventAble.DispatchLocal(in signal);
            }



            public void Release()
            {
                if (releasing) { return; }

                releasing = true;
                try
                {
                    eventAble?.Reset();
                }
                finally
                {
                    eventAble = null;
                    owner = default;
                    releasing = false;
                }
            }



            public void Abandon()
            {
                eventAble = null;
                owner = default;
                releasing = false;
            }
        }



        private readonly ElementWorld world;
        private readonly int initialEventTableCapacity;
        private readonly Dictionary<ElementKey, EventHost> activeHosts;
        private readonly Stack<EventHost> pooledHosts;
        private readonly HashSet<ElementKey> eligibleKeys;
        private bool disposed;



        /// <summary>
        /// 지정한 ElementWorld에 lazy EventAble host registry를 연결합니다.
        /// registry를 World보다 먼저 Dispose해야 final fact 구독이 안전하게 해제됩니다.
        /// </summary>
        public EventElementHostRegistry(ElementWorld world, int initialEventTableCapacity = 2, int initialHostCapacity = 64)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            if (initialEventTableCapacity < 0) { throw new ArgumentOutOfRangeException(nameof(initialEventTableCapacity)); }
            if (initialHostCapacity < 0) { throw new ArgumentOutOfRangeException(nameof(initialHostCapacity)); }

            this.initialEventTableCapacity = initialEventTableCapacity;
            activeHosts = new Dictionary<ElementKey, EventHost>(initialHostCapacity);
            pooledHosts = new Stack<EventHost>(initialHostCapacity);
            eligibleKeys = new HashSet<ElementKey>();
            world.FactDispatched += OnElementFact;
#if UNITY_EDITOR
            EditorRegister(this);
#endif
        }



        public int ActiveHostCount => activeHosts.Count;
        public int PooledHostCount => pooledHosts.Count;



        /// <summary>
        /// 같은 World의 살아 있는 ElementHandle을 EventAble 지원 handle로 감쌉니다.
        /// 이 호출만으로는 EventHost나 EventValue table을 할당하지 않습니다.
        /// </summary>
        public EventElementHandle Wrap(ElementHandle element)
        {
            ThrowIfDisposed();
            if (element.Key.WorldId != world.WorldId)
            {
                throw new ArgumentException("다른 ElementWorld의 handle은 이 EventHost registry로 감쌀 수 없습니다.", nameof(element));
            }
            if (!element.IsAlive) { throw new InvalidOperationException($"stale Element handle은 노출할 수 없습니다: {element.Key}"); }
            eligibleKeys.Add(element.Key);
            return new EventElementHandle(this, element);
        }



        public bool TryWrap(ElementHandle element, out EventElementHandle handle)
        {
            if (disposed || element.Key.WorldId != world.WorldId || !element.IsAlive)
            {
                handle = default;
                return false;
            }

            eligibleKeys.Add(element.Key);
            handle = new EventElementHandle(this, element);
            return true;
        }



        public void Dispose()
        {
            if (disposed) { return; }

            world.FactDispatched -= OnElementFact;
#if UNITY_EDITOR
            EditorUnregister(this);
#endif
            foreach (EventHost host in activeHosts.Values)
            {
                try
                {
#if UNITY_EDITOR
                    EditorNotifyEventAbleReleasing(host.Key, host.AllocatedEventAble);
#endif
                    host.Release();
                }
                catch (Exception exception)
                {
                    host.Abandon();
                    Debug.LogException(exception);
                }
            }

            activeHosts.Clear();
            pooledHosts.Clear();
            eligibleKeys.Clear();
            disposed = true;
        }



        internal EventAble GetOrCreateEventAble(EventElementHandle owner)
        {
            ThrowIfDisposed();
            if (owner.Key.WorldId != world.WorldId)
            {
                throw new InvalidOperationException($"다른 ElementWorld의 handle입니다: {owner.Key}");
            }
            if (!owner.IsAlive) { throw new InvalidOperationException($"stale Element handle입니다: {owner.Key}"); }

            if (!activeHosts.TryGetValue(owner.Key, out EventHost host))
            {
                host = pooledHosts.Count > 0 ? pooledHosts.Pop() : new EventHost();
                host.Acquire(owner);
                activeHosts.Add(owner.Key, host);
            }

            return host.GetOrCreate(initialEventTableCapacity);
        }



        private void OnElementFact(in ElementFact fact)
        {
            bool isDespawn = fact.Type == ElementFactType.Despawned;
            if (!activeHosts.TryGetValue(fact.Element, out EventHost host))
            {
                if (isDespawn) { eligibleKeys.Remove(fact.Element); }
                return;
            }

            try
            {
                host.Dispatch(in fact);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            if (!isDespawn) { return; }

            bool reusable = false;
            try
            {
                //? Reset 중 sibling 제거가 owner.EventAble에 재진입할 수 있으므로 active map은 Reset이 끝날 때까지 유지합니다.
#if UNITY_EDITOR
                EditorNotifyEventAbleReleasing(fact.Element, host.AllocatedEventAble);
#endif
                host.Release();
                reusable = true;
            }
            catch (Exception exception)
            {
                host.Abandon();
                Debug.LogException(exception);
            }
            finally
            {
                if (activeHosts.TryGetValue(fact.Element, out EventHost current) && ReferenceEquals(current, host))
                {
                    activeHosts.Remove(fact.Element);
                }
            }

            if (reusable) { pooledHosts.Push(host); }
            eligibleKeys.Remove(fact.Element);
        }



        private void ThrowIfDisposed()
        {
            if (disposed) { throw new ObjectDisposedException(nameof(EventElementHostRegistry)); }
        }
    }
}
