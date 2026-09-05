using System;
using System.Collections.Generic;
using Pan.HighDensityElement;
using Unity.Mathematics;
using UnityEngine;



namespace Pan.Tan.Element
{
    /// <summary>
    /// 범용 ElementWorld 위에서 bodyless Query/Kinematic ElementTan을 실행합니다.
    /// </summary>
    public sealed partial class ElementTanRuntime :
        ITanRuntimeBackend,
        ITanFeatureRuntimeBackend,
        ITanBoundaryRuntimeBackend
    {
        private struct TanRecord
        {
            public ElementHandle Element;
            public TanSpawnRequest Spawn;
            public TanVisualOrientationDefinition VisualOrientation;
            public TanPrimaryMotionDefinition PrimaryMotion;
            public TanWaveMotionDefinition WaveMotion;
            public TanBoundaryDefinition Boundary;
            public List<TanTargetHandle> PreviousStepContacts;
            public List<TanTargetHandle> CurrentStepContacts;
        }



        private readonly ElementWorld world;
        private readonly bool ownsWorld;
        private readonly ElementTanBackendOptions options;
        private readonly Dictionary<TanKey, TanRecord> records;
        private readonly List<TanKey> keyScratch;
        private readonly Stack<List<TanTargetHandle>> contactListPool;
        private IElementTanTargetResolver targetResolver;
        private bool disposed;



        public ElementTanRuntime(
            ElementWorld world,
            ElementTanBackendOptions options,
            int capacity = 512)
            : this(world, false, options, capacity)
        {
        }



        public ElementTanRuntime(
            ElementTanBackendOptions options,
            int capacity = 512)
            : this(new ElementWorld(Mathf.Max(1, capacity)), true, options, capacity)
        {
        }



        private ElementTanRuntime(
            ElementWorld world,
            bool ownsWorld,
            ElementTanBackendOptions options,
            int capacity)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            this.ownsWorld = ownsWorld;
            this.options = options;
            Capacity = Mathf.Max(1, capacity);
            records = new Dictionary<TanKey, TanRecord>(Capacity);
            keyScratch = new List<TanKey>(Capacity);
            contactListPool = new Stack<List<TanTargetHandle>>(Mathf.Min(Capacity, 64));
            world.FactDispatched += OnElementFact;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ElementTanDebugRegistry.Register(this);
#endif
        }



        public int ContextId => world.WorldId;
        public TanBackendKind BackendKind => TanBackendKind.ElementQuery;
        public int ActiveCount => records.Count;
        public int Capacity { get; private set; }
        public bool IsAvailable => !disposed && !world.IsDisposed;
        public ElementWorld ElementWorld => world;



        public event TanFactHandler FactRaised;



        public void SetTargetResolver(IElementTanTargetResolver resolver) => targetResolver = resolver;

        public void EnsureCapacity(int capacity)
        {
            if (!IsAvailable) { return; }
            Capacity = Mathf.Max(Capacity, capacity);
        }

        public bool IsAlive(in TanKey key) =>
            IsAvailable && key.ContextId == ContextId && records.TryGetValue(key, out TanRecord record) && record.Element.IsAlive;

        /// <summary>
        /// 같은 runtime이 소유한 살아 있는 Tan의 저수준 Element handle을 반환합니다.
        /// </summary>
        public bool TryGetElementHandle(in TanKey key, out ElementHandle handle)
        {
            if (IsAlive(in key) && records.TryGetValue(key, out TanRecord record))
            {
                handle = record.Element;
                return true;
            }

            handle = default;
            return false;
        }



        /// <summary>
        /// 이 runtime이 World를 단독 소유할 때 사용하는 호환 simulation entry입니다.
        /// </summary>
        public void SimulateFixedStep(float deltaTime, ushort substepIndex)
        {
            if (!IsAvailable) { return; }
            PrepareFixedStep(substepIndex);
            world.Tick(Mathf.Max(0f, deltaTime), substepIndex);
        }

        /// <summary>
        /// Seconds/Frames 및 Scaled/Unscaled schedule을 사용하는 sparse feature를 physics Tick과 별도로 갱신합니다.
        /// 공유 ElementWorld에서는 consumer service가 World당 한 번만 호출해야 합니다.
        /// </summary>
        public void TickUpdateFeatures(in ElementUpdateContext context)
        {
            if (!IsAvailable) { return; }
            world.TickUpdateFeatures(in context);
        }

        public void TickUpdateFeatures(
            float scaledDeltaSeconds,
            float unscaledDeltaSeconds,
            float scaledFrameUnits = -1f,
            float unscaledFrameUnits = -1f)
        {
            var context = new ElementUpdateContext(
                scaledDeltaSeconds,
                unscaledDeltaSeconds,
                scaledFrameUnits,
                unscaledFrameUnits);
            TickUpdateFeatures(in context);
        }



        public void ClearAll()
        {
            if (!IsAvailable || records.Count == 0) { return; }
            keyScratch.Clear();
            foreach (TanKey key in records.Keys) { keyScratch.Add(key); }
            for (int i = 0; i < keyScratch.Count; i++)
            {
                if (records.TryGetValue(keyScratch[i], out TanRecord record))
                {
                    world.TryDespawnImmediately(record.Element.Key);
                }
            }
            keyScratch.Clear();
        }



        public void Dispose()
        {
            if (disposed) { return; }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ElementTanDebugRegistry.Unregister(this);
#endif
            ClearAll();
            world.FactDispatched -= OnElementFact;
            records.Clear();
            keyScratch.Clear();
            contactListPool.Clear();
            disposed = true;
            if (ownsWorld) { world.Dispose(); }
        }



        private static TanKey ToTanKey(in ElementKey key) => new TanKey(key.WorldId, key.Slot, key.Generation);
    }
}
