using System;
using System.Collections.Generic;
using System.Threading;
using Unity.U2D.Physics;
using UnityEngine;
using UnityEngine.Tilemaps;



namespace Pan.HighDensityElement.Physics2DBridge
{
    /// <summary>
    /// Legacy Physics2D 형상을 하나의 PhysicsCore2D lane에 mirror하고 target identity를 관리합니다.
    /// </summary>
    /// <remarks>
    /// 등록과 형상 추출은 main thread에서 실행되며, 움직이는 Rigidbody2D의 Transform은 legacy Physics2D가 권위자입니다.
    /// </remarks>
    public sealed partial class Physics2DBridgeRegistry : IDisposable
    {
        private sealed class BodyRecord
        {
            public readonly List<Collider2D> Colliders = new List<Collider2D>(4);
            public readonly List<uint> ColliderProjectionSignatures = new List<uint>(4);
            public readonly List<int> TargetIds = new List<int>(4);
            public HighDensityPhysicsBridge2D Owner;
            public Rigidbody2D Rigidbody;
            public IPhysics2DBridgeShapeProvider Provider;
            public IPhysics2DBridgeChangeSource ChangeSource;
            public Action GeometryChangedHandler;
            public Action StateChangedHandler;
            public Component ProviderTarget;
            public int ProviderGeometryRevision;
            public int ProviderLayer;
            public bool ProviderIsTrigger;
            public PhysicsBody Body;
            public bool GeometryDirty;
            public bool StateDirty;
            public bool FactReceiversDirty;
            public Vector2 PreviousLegacyPosition;
            public Vector2 LegacyPosition;
            public float PreviousLegacyRotationDegrees;
            public float LegacyRotationDegrees;
            public bool HasMotionPose;
            public bool TeleportPending;
            public bool TeleportedThisSync;
            public int StrictCcdRemainingSteps;
            public bool StrictCcdRequestedThisSync;
            public float MinimumShapeExtent = float.PositiveInfinity;
            public float MaximumShapeRadius;
            public int ShapeCount;
            public readonly List<PhysicsShape.ShapeProxy> ShapeProxies =
                new List<PhysicsShape.ShapeProxy>(8);
            public readonly List<int> ShapeTargetIds = new List<int>(8);
            public readonly List<ulong> ShapeCategoryMasks = new List<ulong>(8);
            public readonly List<ulong> ShapeContactMasks = new List<ulong>(8);
            public readonly List<byte> ShapeTriggers = new List<byte>(8);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            public long LastSynchronizationSequence;
#endif
        }



#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private sealed class DebugDrawLease
        {
            public PhysicsWorld.DrawOptions PreviousOptions;
            public int ReferenceCount;
        }
#endif



        private sealed class ComponentRecord
        {
            public readonly List<BodyRecord> Bodies = new List<BodyRecord>(2);
            public HighDensityPhysicsBridge2D Owner;
            public Physics2DBridgeRegistrationHandle Handle;
            public bool GeometryDirty;
        }



        private sealed class TargetRecord
        {
            public TargetRecord(
                Collider2D collider,
                BodyRecord body,
                Component owner,
                int layer,
                bool isTrigger)
            {
                Collider = collider;
                Body = body;
                Owner = owner;
                Layer = layer;
                IsTrigger = isTrigger;
                Receivers = Array.Empty<IElementPhysicsFactReceiver2D>();
            }



            public Collider2D Collider { get; }
            public BodyRecord Body { get; }
            public Component Owner { get; }
            public int Layer { get; }
            public bool IsTrigger { get; }
            public IElementPhysicsFactReceiver2D[] Receivers { get; set; }
        }



        private readonly struct ReceiverDispatchStamp : IEquatable<ReceiverDispatchStamp>
        {
            public ReceiverDispatchStamp(
                ElementKey element,
                ElementFactType type,
                ushort substepIndex,
                long synchronizationSequence)
            {
                Element = element;
                Type = type;
                SubstepIndex = substepIndex;
                SynchronizationSequence = synchronizationSequence;
            }



            public ElementKey Element { get; }
            public ElementFactType Type { get; }
            public ushort SubstepIndex { get; }
            public long SynchronizationSequence { get; }



            public bool Equals(ReceiverDispatchStamp other) =>
                Element.Equals(other.Element) &&
                Type == other.Type &&
                SubstepIndex == other.SubstepIndex &&
                SynchronizationSequence == other.SynchronizationSequence;
        }



        private static readonly List<HighDensityPhysicsBridge2D> EnabledComponents =
            new List<HighDensityPhysicsBridge2D>(64);
        private static readonly List<Physics2DBridgeRegistry> Registries =
            new List<Physics2DBridgeRegistry>(4);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly Dictionary<PhysicsCore2DLane, DebugDrawLease> DebugDrawLeases =
            new Dictionary<PhysicsCore2DLane, DebugDrawLease>(4);
#endif
        private static int nextContextId;

        private readonly PhysicsCore2DLane lane;
        private readonly ElementWorld elementWorld;
        private readonly Physics2DBridgeSettings settings;
        private readonly Dictionary<int, TargetRecord> targets;
        private readonly Dictionary<Collider2D, int> targetIdsByCollider;
        private readonly Dictionary<HighDensityPhysicsBridge2D, ComponentRecord> componentBodies;
        private readonly Dictionary<int, ComponentRecord> registrationsBySlot;
        private readonly List<uint> registrationGenerations;
        private readonly Stack<int> freeRegistrationSlots;
        private readonly List<BodyRecord> bodies;
        private readonly List<HighDensityPhysicsBridge2D> pendingComponents =
            new List<HighDensityPhysicsBridge2D>(16);
        private readonly List<Collider2D> colliderScratch = new List<Collider2D>(16);
        private readonly List<Vector2> vertexScratch = new List<Vector2>(16);
        private readonly PhysicsShapeGroup2D colliderShapeGroupScratch = new PhysicsShapeGroup2D(8, 64);
        private readonly List<Physics2DBridgeShapeDescriptor> providerShapeScratch =
            new List<Physics2DBridgeShapeDescriptor>(8);
        private readonly HashSet<int> reportedConversionErrors = new HashSet<int>();
        private readonly List<MonoBehaviour> receiverComponentScratch = new List<MonoBehaviour>(16);
        private readonly List<IElementPhysicsFactReceiver2D> receiverScratch =
            new List<IElementPhysicsFactReceiver2D>(8);
        private readonly HashSet<IElementPhysicsFactReceiver2D> receiverDedupScratch =
            new HashSet<IElementPhysicsFactReceiver2D>();
        private readonly Dictionary<IElementPhysicsFactReceiver2D, ReceiverDispatchStamp> receiverDispatchStamps =
            new Dictionary<IElementPhysicsFactReceiver2D, ReceiverDispatchStamp>(16);
        private MovingProjectionShape2D[] movingProjectionScratch;
        private int movingProjectionCount;
        private readonly int contextId;
        private int nextTargetId;
        private long synchronizationSequence;
        private bool disposed;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool debugDrawLeaseAcquired;
#endif



        /// <summary>
        /// 지정한 PhysicsCore2D lane에 bridge registry를 생성합니다.
        /// </summary>
        public Physics2DBridgeRegistry(PhysicsCore2DLane lane, Physics2DBridgeSettings settings)
            : this(lane, null, settings, Interlocked.Increment(ref nextContextId))
        {
        }



        /// <summary>
        /// 지정한 ElementWorld의 PhysicsCore2D lane에 bridge registry를 생성합니다.
        /// </summary>
        public Physics2DBridgeRegistry(ElementWorld world, Physics2DBridgeSettings settings)
            : this(
                world != null ? world.PhysicsCoreLane : throw new ArgumentNullException(nameof(world)),
                world,
                settings,
                Interlocked.Increment(ref nextContextId))
        {
        }



        private Physics2DBridgeRegistry(
            PhysicsCore2DLane lane,
            ElementWorld elementWorld,
            Physics2DBridgeSettings settings,
            int contextId)
        {
            this.lane = lane ?? throw new ArgumentNullException(nameof(lane));
            this.elementWorld = elementWorld;
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.contextId = contextId != 0 ? contextId : Interlocked.Increment(ref nextContextId);
            if (!lane.IsValid) { throw new ArgumentException("Physics Core 2D lane이 유효하지 않습니다.", nameof(lane)); }
            int capacity = Mathf.Max(16, settings.InitialTargetCapacity);
            targets = new Dictionary<int, TargetRecord>(capacity);
            targetIdsByCollider = new Dictionary<Collider2D, int>(capacity);
            componentBodies = new Dictionary<HighDensityPhysicsBridge2D, ComponentRecord>(64);
            registrationsBySlot = new Dictionary<int, ComponentRecord>(64);
            registrationGenerations = new List<uint>(64);
            freeRegistrationSlots = new Stack<int>(64);
            bodies = new List<BodyRecord>(capacity);
            movingProjectionScratch = new MovingProjectionShape2D[Mathf.Max(16, capacity * 2)];
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            AcquireDebugDrawLease();
#endif
            Registries.Add(this);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Physics2DBridgeDebugSummaryProvider2D.RegisterRegistry();
#endif
            Tilemap.tilemapTileChanged += OnTilemapTileChanged;
            if (elementWorld != null) { elementWorld.FactDispatched += OnElementFactDispatched; }
            if (Default == null) { PromoteDefault(this); }
        }



        /// <summary>
        /// 명시적으로 binding하지 않은 활성 bridge가 자동 등록되는 현재 registry입니다.
        /// </summary>
        public static Physics2DBridgeRegistry Default { get; private set; }
        public int ContextId => contextId;
        public int TargetCount => targets.Count;
        public int BodyCount => bodies.Count;
        public int ShapeCount { get; private set; }
        public int StrictMotionCount { get; private set; }
        public long SynchronizationSequence => synchronizationSequence;
        public Physics2DBridgeSettings Settings => settings;
        public bool IsDisposed => disposed;



        internal PhysicsCore2DLane Lane => lane;



#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal static int CopyDebugRegistries(
            PhysicsCore2DLane targetLane,
            List<Physics2DBridgeRegistry> destination)
        {
            for (int i = 0; i < Registries.Count; i++)
            {
                Physics2DBridgeRegistry registry = Registries[i];
                if (registry != null && !registry.disposed && ReferenceEquals(registry.lane, targetLane))
                {
                    destination.Add(registry);
                }
            }
            return destination.Count;
        }
#endif



        public void Dispose()
        {
            if (disposed) { return; }

            if (lane.IsValid)
            {
                lane.ReplaceMovingProjections(
                    ReadOnlySpan<MovingProjectionShape2D>.Empty,
                    ++synchronizationSequence);
            }

            var explicitlyBoundOwners = new List<HighDensityPhysicsBridge2D>(componentBodies.Count);
            foreach (ComponentRecord registration in componentBodies.Values)
            {
                HighDensityPhysicsBridge2D owner = registration.Owner;
                if (owner != null && owner.IsExplicitlyBoundTo(this)) { explicitlyBoundOwners.Add(owner); }
                owner?.ClearRegistration(this);
            }
            for (int i = 0; i < pendingComponents.Count; i++)
            {
                HighDensityPhysicsBridge2D owner = pendingComponents[i];
                if (owner != null && owner.IsExplicitlyBoundTo(this) && !explicitlyBoundOwners.Contains(owner))
                {
                    explicitlyBoundOwners.Add(owner);
                }
            }
            for (int i = bodies.Count - 1; i >= 0; i--) { DestroyRecord(bodies[i]); }
            bodies.Clear();
            targets.Clear();
            targetIdsByCollider.Clear();
            componentBodies.Clear();
            registrationsBySlot.Clear();
            registrationGenerations.Clear();
            freeRegistrationSlots.Clear();
            pendingComponents.Clear();
            reportedConversionErrors.Clear();
            receiverDispatchStamps.Clear();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ReleaseDebugDrawLease();
#endif
            Tilemap.tilemapTileChanged -= OnTilemapTileChanged;
            if (elementWorld != null) { elementWorld.FactDispatched -= OnElementFactDispatched; }
            Registries.Remove(this);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Physics2DBridgeDebugSummaryProvider2D.UnregisterRegistry();
#endif
            bool wasDefault = ReferenceEquals(Default, this);
            disposed = true;
            if (ReferenceEquals(Default, this))
            {
                PromoteDefault(Registries.Count > 0 ? Registries[0] : null);
            }

            for (int i = 0; i < explicitlyBoundOwners.Count; i++)
            {
                explicitlyBoundOwners[i]?.OnRegistryDisposed(this);
            }

            if (!wasDefault && Default != null)
            {
                RegisterEnabledComponents(Default);
            }
        }





        private static void Tag(PhysicsShape shape, int targetId, BodyRecord record)
        {
            PhysicsCore2DLane.TagBridgeTarget(shape, targetId);
            if (shape.isValid && record != null)
            {
                PhysicsShape.ShapeProxy proxy = shape.CreateShapeProxy(false);
                Vector2 extents = proxy.aabb.extents;
                float minimumExtent = float.PositiveInfinity;
                if (extents.x > 0.0001f) { minimumExtent = Mathf.Min(minimumExtent, extents.x * 2f); }
                if (extents.y > 0.0001f) { minimumExtent = Mathf.Min(minimumExtent, extents.y * 2f); }
                record.MinimumShapeExtent = Mathf.Min(record.MinimumShapeExtent, minimumExtent);
                record.MaximumShapeRadius = Mathf.Max(record.MaximumShapeRadius, extents.magnitude);
                record.ShapeCount++;
                record.ShapeProxies.Add(proxy);
                record.ShapeTargetIds.Add(targetId);
                record.ShapeCategoryMasks.Add(shape.contactFilter.categories.bitMask);
                record.ShapeContactMasks.Add(shape.contactFilter.contacts.bitMask);
                record.ShapeTriggers.Add(shape.isTrigger ? (byte)1 : (byte)0);
            }
        }



        private static void RequireVertexCount(
            List<Vector2> vertices,
            int required,
            PhysicsShapeType2D shapeType)
        {
            if (vertices.Count < required)
            {
                throw new InvalidOperationException($"{shapeType} shape의 vertex 수가 부족합니다: {vertices.Count}/{required}");
            }
        }



        private void ThrowIfDisposed()
        {
            if (disposed) { throw new ObjectDisposedException(nameof(Physics2DBridgeRegistry)); }
        }
    }
}
