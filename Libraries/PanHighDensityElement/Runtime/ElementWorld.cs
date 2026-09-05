using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;



namespace Pan.HighDensityElement
{
    public delegate void ElementFactHandler(in ElementFact fact);



    public sealed partial class ElementWorld : IDisposable
    {
        private static int nextWorldId;

        private NativeList<ElementRegistryEntry> registry;
        private NativeList<int> freeSlots;
        private NativeList<NativeElementPoseState> poseStates;
        private NativeList<NativeElementState> kinematicStates;
        private NativeList<NativeElementState> areaStates;
        private NativeList<NativeElementState> physicsStates;
        private NativeQueue<ElementCommand> commands;
        private NativeQueue<ElementCommand> postEventCommands;
        private NativeQueue<ElementFact> kinematicFactQueue;
        private NativeQueue<ElementFact> areaFactQueue;
        private NativeQueue<ElementFact> physicsFactQueue;
        private NativeList<ElementFact> factBuffer;
        private readonly ElementSparseFeatureStore<NativeElementLifetimeState> lifetimeFeatures;
        private readonly ElementSparseFeatureStore<ElementLocalClock> localClocks;
        private readonly ElementSparseFeatureStore<ElementDirectionalMotionFeature> directionalMotionFeatures;
        private readonly ElementSparseFeatureStore<NativeElementWaveMotionState> waveMotionFeatures;
        private readonly ElementSparseFeatureStore<NativeElementVisualOrientationState> visualOrientationFeatures;
        private readonly ElementSparseFeatureStore<NativeElementBoundaryState> boundaryFeatures;
        private readonly PhysicsCore2DLane physicsCoreLane;
        private readonly List<ElementFactHandler> factHandlers;
        private readonly List<List<ElementFactHandler>> factDispatchScratchByDepth;
        private readonly HashSet<ContactFactIdentity> contactFactIdentities;
        private int dispatchDepth;
        private uint fixedStepIndex;
        private int lastCommandCount;
        private int lastStrictCcdCandidateCount;
        private int lastForcedStrictCcdCount;
        private int lastTeleportCount;
        private int lastBoundaryExitCount;
        private ElementBounds2D worldBoundaryBounds;
        private ElementBounds2D viewBoundaryBounds;
        private byte hasWorldBoundaryBounds;
        private byte hasViewBoundaryBounds;
        private ulong structuralRevision;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly ElementDebugFactRingBuffer recentDebugFacts;
        private bool manualEnableDetailedDiagnostics;
        private bool manualEnableDebugCapture;
        private int debugDetailedDiagnosticsLeaseCount;
        private int debugFactCaptureLeaseCount;
#endif
        private bool disposed;



        /// <summary>
        /// 지정한 초기 용량으로 PhysicsCore2D 기반 Element world를 생성합니다.
        /// </summary>
        /// <param name="initialCapacity">초기 Element 저장 용량입니다. 필요하면 simulation 경계에서 확장됩니다.</param>
        public ElementWorld(int initialCapacity = 512)
        {
            if (initialCapacity < 1) { throw new ArgumentOutOfRangeException(nameof(initialCapacity)); }

            WorldId = Interlocked.Increment(ref nextWorldId);
            registry = new NativeList<ElementRegistryEntry>(initialCapacity, Allocator.Persistent);
            freeSlots = new NativeList<int>(initialCapacity, Allocator.Persistent);
            poseStates = new NativeList<NativeElementPoseState>(initialCapacity, Allocator.Persistent);
            kinematicStates = new NativeList<NativeElementState>(initialCapacity, Allocator.Persistent);
            areaStates = new NativeList<NativeElementState>(math.max(16, initialCapacity / 8), Allocator.Persistent);
            physicsStates = new NativeList<NativeElementState>(math.max(8, initialCapacity / 32), Allocator.Persistent);
            commands = new NativeQueue<ElementCommand>(Allocator.Persistent);
            postEventCommands = new NativeQueue<ElementCommand>(Allocator.Persistent);
            kinematicFactQueue = new NativeQueue<ElementFact>(Allocator.Persistent);
            areaFactQueue = new NativeQueue<ElementFact>(Allocator.Persistent);
            physicsFactQueue = new NativeQueue<ElementFact>(Allocator.Persistent);
            factBuffer = new NativeList<ElementFact>(initialCapacity, Allocator.Persistent);
            lifetimeFeatures = new ElementSparseFeatureStore<NativeElementLifetimeState>(initialCapacity);
            localClocks = new ElementSparseFeatureStore<ElementLocalClock>(math.max(16, initialCapacity / 8));
            directionalMotionFeatures = new ElementSparseFeatureStore<ElementDirectionalMotionFeature>(
                math.max(16, initialCapacity / 8));
            waveMotionFeatures = new ElementSparseFeatureStore<NativeElementWaveMotionState>(
                math.max(16, initialCapacity / 8));
            visualOrientationFeatures = new ElementSparseFeatureStore<NativeElementVisualOrientationState>(
                math.max(16, initialCapacity / 8));
            boundaryFeatures = new ElementSparseFeatureStore<NativeElementBoundaryState>(
                math.max(16, initialCapacity / 8));
            physicsCoreLane = new PhysicsCore2DLane(WorldId);
            factHandlers = new List<ElementFactHandler>(8);
            contactFactIdentities = new HashSet<ContactFactIdentity>();
            factDispatchScratchByDepth = new List<List<ElementFactHandler>>(2)
            {
                new List<ElementFactHandler>(8)
            };
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            recentDebugFacts = new ElementDebugFactRingBuffer(128);
            ElementWorldDebugRegistry.Register(this);
#endif
        }



        /// <summary>
        /// 정렬된 Element fact를 main thread에서 전달합니다.
        /// 개별 subscriber 예외는 기록되지만 다른 subscriber와 Element 수명 전이를 중단시키지 않습니다.
        /// </summary>
        public event ElementFactHandler FactDispatched
        {
            add
            {
                if (value != null) { factHandlers.Add(value); }
            }
            remove
            {
                if (value == null) { return; }

                int index = factHandlers.LastIndexOf(value);
                if (index >= 0) { factHandlers.RemoveAt(index); }
            }
        }



        public int WorldId { get; }

        public bool IsDisposed => disposed;
        public int AliveCount => kinematicStates.Length + areaStates.Length + physicsStates.Length;
        public int KinematicCount => kinematicStates.Length;
        public int AreaCount => areaStates.Length;
        public int PhysicsCoreCount => physicsStates.Length;
        public int LastCoreQueryFactCount { get; private set; }
        public int LastFactCount { get; private set; }
        public bool EnableTimingMetrics { get; set; }
        public double LastSimulationMilliseconds { get; private set; }
        public double LastFactDispatchMilliseconds { get; private set; }
        public PhysicsCore2DLane PhysicsCoreLane => physicsCoreLane;
        public uint FixedStepIndex => fixedStepIndex;
        public ulong StructuralRevision => structuralRevision;
        public int DirectionalMotionFeatureCount => directionalMotionFeatures.Count;
        public int WaveMotionFeatureCount => waveMotionFeatures.Count;
        public int VisualOrientationFeatureCount => visualOrientationFeatures.Count;
        public int BoundaryFeatureCount => boundaryFeatures.Count;
        public int LastBoundaryExitCount => lastBoundaryExitCount;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool EnableDetailedDiagnostics
        {
            get => manualEnableDetailedDiagnostics || debugDetailedDiagnosticsLeaseCount > 0;
            set => manualEnableDetailedDiagnostics = value;
        }
        public bool EnableDebugCapture
        {
            get => manualEnableDebugCapture || debugFactCaptureLeaseCount > 0;
            set => manualEnableDebugCapture = value;
        }
#else
        public bool EnableDetailedDiagnostics { get => false; set { } }
        public bool EnableDebugCapture { get => false; set { } }
#endif
        public void Dispose()
        {
            if (disposed) { return; }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ElementWorldDebugRegistry.Unregister(this);
#endif
            DespawnAll(kinematicStates);
            DespawnAll(areaStates);
            DespawnAll(physicsStates);
            disposed = true;
            physicsCoreLane.Dispose();

            if (registry.IsCreated) { registry.Dispose(); }
            if (freeSlots.IsCreated) { freeSlots.Dispose(); }
            if (poseStates.IsCreated) { poseStates.Dispose(); }
            if (kinematicStates.IsCreated) { kinematicStates.Dispose(); }
            if (areaStates.IsCreated) { areaStates.Dispose(); }
            if (physicsStates.IsCreated) { physicsStates.Dispose(); }
            if (commands.IsCreated) { commands.Dispose(); }
            if (postEventCommands.IsCreated) { postEventCommands.Dispose(); }
            if (kinematicFactQueue.IsCreated) { kinematicFactQueue.Dispose(); }
            if (areaFactQueue.IsCreated) { areaFactQueue.Dispose(); }
            if (physicsFactQueue.IsCreated) { physicsFactQueue.Dispose(); }
            if (factBuffer.IsCreated) { factBuffer.Dispose(); }
            lifetimeFeatures.Dispose();
            localClocks.Dispose();
            directionalMotionFeatures.Dispose();
            waveMotionFeatures.Dispose();
            visualOrientationFeatures.Dispose();
            boundaryFeatures.Dispose();
            factHandlers.Clear();
            for (int i = 0; i < factDispatchScratchByDepth.Count; i++)
            {
                factDispatchScratchByDepth[i].Clear();
            }
            factDispatchScratchByDepth.Clear();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            recentDebugFacts.Clear();
#endif
            contactFactIdentities.Clear();
        }



        private void ThrowIfDisposed()
        {
            if (disposed) { throw new ObjectDisposedException(nameof(ElementWorld)); }
        }



        private void IncrementStructuralRevision()
        {
            structuralRevision = structuralRevision == ulong.MaxValue ? 1ul : structuralRevision + 1ul;
        }



        private readonly struct ContactFactIdentity : IEquatable<ContactFactIdentity>
        {
            public ContactFactIdentity(in ElementFact fact)
            {
                Element = fact.Element;
                TargetElement = fact.TargetElement;
                BridgeTargetId = fact.BridgeTargetId;
                SubstepIndex = fact.SubstepIndex;
                FixedStepIndex = fact.FixedStepIndex;
            }



            private ElementKey Element { get; }
            private ElementKey TargetElement { get; }
            private int BridgeTargetId { get; }
            private ushort SubstepIndex { get; }
            private uint FixedStepIndex { get; }



            public bool Equals(ContactFactIdentity other) =>
                Element == other.Element &&
                TargetElement == other.TargetElement &&
                BridgeTargetId == other.BridgeTargetId &&
                SubstepIndex == other.SubstepIndex &&
                FixedStepIndex == other.FixedStepIndex;



            public override bool Equals(object obj) => obj is ContactFactIdentity other && Equals(other);



            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = Element.GetHashCode();
                    hashCode = (hashCode * 397) ^ TargetElement.GetHashCode();
                    hashCode = (hashCode * 397) ^ BridgeTargetId;
                    hashCode = (hashCode * 397) ^ SubstepIndex;
                    return (hashCode * 397) ^ (int)FixedStepIndex;
                }
            }
        }
    }
}
