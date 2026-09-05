using System;
using Unity.Mathematics;
using Unity.U2D.Physics;
using UnityEngine;



namespace Pan.HighDensityElement
{
    [Flags]
    public enum ElementCapabilities : uint
    {
        None = 0,
        Lifetime = 1 << 0,
        KinematicMotion2D = 1 << 1,
        SpriteVisual2D = 1 << 2,
        QuerySensor2D = 1 << 3,
        AreaSensor2D = 1 << 4,
        DynamicBody2D = 1 << 5,
        PeriodicFact = 1 << 6,
        QueryTarget2D = 1 << 7,
        DirectionalMotion2D = 1 << 8,
        WaveMotion2D = 1 << 9,
        VisualOrientation2D = 1 << 10,
        Boundary2D = 1 << 11
    }



    [Flags]
    public enum ElementPhysicsCapability : byte
    {
        None = 0,
        QuerySensor2D = 1 << 0,
        QueryTarget2D = 1 << 1,
        AreaSensor2D = 1 << 2,
        DynamicBody2D = 1 << 3
    }



    /// <summary>
    /// Element가 PhysicsCore2D를 사용하는 방식을 게임 코드에 단순하게 노출합니다.
    /// </summary>
    public enum ElementExecutionModel2D : byte
    {
        /// <summary>
        /// solver Body 없이 쿼리 중심으로 이동하고 접촉을 검사합니다.
        /// </summary>
        Query = 0,

        /// <summary>
        /// PhysicsCore2D Body를 생성하고 solver 시뮬레이션에 참여합니다.
        /// </summary>
        Simulated = 1
    }



    /// <summary>
    /// 빠른 상대 운동에 대한 엄격 CCD 후보 판정 방식을 지정합니다.
    /// </summary>
    public enum StrictCcdOverride2D : byte
    {
        Auto = 0,
        ForceOn = 1,
        ForceOff = 2
    }



    public enum ElementLifecycle : byte
    {
        Invalid = 0,
        Reserved = 1,
        Alive = 2,
        DespawnPending = 3,
        Despawned = 4
    }



    public enum ElementLane : byte
    {
        None = 0,
        QuerySprite2D = 1,
        AreaSensorSprite2D = 2,
        DynamicBodySprite2D = 3
    }



    public enum ElementSpawnStatus : byte
    {
        Success = 0,
        InvalidArchetype = 1,
        UnsupportedLayout = 2,
        WorldDisposed = 3,
        CapacityExceeded = 4
    }



    public enum ElementFactType : byte
    {
        Spawned = 0,
        Contact = 1,
        Enter = 2,
        Stay = 3,
        Exit = 4,
        Periodic = 5,
        LifetimeExpired = 6,
        Despawned = 7,
        BoundaryExited = 8
    }



    [Flags]
    public enum ElementFactFlags : byte
    {
        None = 0,
        StartedOverlapped = 1 << 0,
        HasSurfacePoint = 1 << 1,
        HasSurfaceNormal = 1 << 2,
        HasImpactCenter = 1 << 3
    }



    public enum PhysicsCoreBodyMode : byte
    {
        Kinematic = 0,
        Dynamic = 1
    }



    public enum PhysicsCoreShape2D : byte
    {
        Circle = 0,
        Box = 1
    }



    [Serializable]
    public readonly struct ElementPhysicsMaterial2D
    {
        public ElementPhysicsMaterial2D(
            float density,
            float friction,
            float bounciness,
            float gravityScale = 1f)
        {
            Density = math.max(0.0001f, density);
            Friction = math.max(0f, friction);
            Bounciness = math.max(0f, bounciness);
            GravityScale = gravityScale;
        }



        public float Density { get; }
        public float Friction { get; }
        public float Bounciness { get; }
        public float GravityScale { get; }



        public static ElementPhysicsMaterial2D Default =>
            new ElementPhysicsMaterial2D(1f, 0.2f, 0f, 1f);
    }



    public enum ElementCommandType : byte
    {
        None = 0,
        SetPosition2D = 1,
        SetVelocity2D = 2,
        Despawn = 3,
        SetVisualId = 4,
        SetScale2D = 5,
        SetColor = 6,
        SetRemainingLifetime = 7,
        SetAcceleration2D = 8,
        SetLinearDamping = 9,
        RequestStrictCcd2D = 10,
        MarkTeleported2D = 11,
        Teleport2D = 12,
        SetRotation2D = 13,
        ConfigureDirectionalMotion2D = 14,
        RemoveDirectionalMotion2D = 15,
        ConfigureWaveMotion2D = 16,
        RemoveWaveMotion2D = 17,
        ConfigureVisualOrientation2D = 18,
        RemoveVisualOrientation2D = 19,
        ConfigureBoundary2D = 20,
        RemoveBoundary2D = 21
    }



    [Serializable]
    public readonly struct ElementKey : IEquatable<ElementKey>
    {
        public ElementKey(int worldId, int slot, uint generation)
        {
            WorldId = worldId;
            Slot = slot;
            Generation = generation;
        }



        public int WorldId { get; }
        public int Slot { get; }
        public uint Generation { get; }
        public bool IsValid => WorldId > 0 && Slot >= 0 && Generation > 0;



        public bool Equals(ElementKey other) =>
            WorldId == other.WorldId && Slot == other.Slot && Generation == other.Generation;



        public override bool Equals(object obj) => obj is ElementKey other && Equals(other);



        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = WorldId;
                hashCode = (hashCode * 397) ^ Slot;
                hashCode = (hashCode * 397) ^ (int)Generation;
                return hashCode;
            }
        }



        public static bool operator ==(ElementKey left, ElementKey right) => left.Equals(right);
        public static bool operator !=(ElementKey left, ElementKey right) => !left.Equals(right);
        public override string ToString() => $"Element({WorldId}:{Slot}:{Generation})";
    }



    public readonly struct ElementSnapshot
    {
        public ElementSnapshot(
            ElementKey key,
            ElementLifecycle lifecycle,
            ElementCapabilities capabilities,
            ElementPhysicsCapability physicsCapabilities,
            float2 previousPosition,
            float2 position,
            float2 velocity,
            float radius,
            float remainingLifetime,
            int visualId,
            uint ownerId,
            uint teamId)
            : this(
                key,
                lifecycle,
                capabilities,
                physicsCapabilities,
                physicsCapabilities.HasFlag(ElementPhysicsCapability.DynamicBody2D)
                    ? ElementExecutionModel2D.Simulated
                    : ElementExecutionModel2D.Query,
                ElementLane.None,
                previousPosition,
                position,
                velocity,
                default,
                0f,
                new float2(1f),
                new UnityEngine.Color32(255, 255, 255, 255),
                radius,
                remainingLifetime,
                visualId,
                ownerId,
                teamId,
                0ul,
                0ul,
                true,
                PhysicsCoreShape2D.Circle,
                StrictCcdOverride2D.Auto,
                0.5f,
                0,
                false,
                false)
        {
        }



        public ElementSnapshot(
            ElementKey key,
            ElementLifecycle lifecycle,
            ElementCapabilities capabilities,
            ElementPhysicsCapability physicsCapabilities,
            ElementExecutionModel2D executionModel,
            ElementLane lane,
            float2 previousPosition,
            float2 position,
            float2 velocity,
            float2 acceleration,
            float linearDamping,
            float2 scale,
            UnityEngine.Color32 color,
            float radius,
            float remainingLifetime,
            int visualId,
            uint ownerId,
            uint teamId,
            ulong physicsCategoryMask,
            ulong interactionLayerMask,
            bool includeTriggers,
            PhysicsCoreShape2D physicsShape,
            StrictCcdOverride2D strictCcdOverride,
            float strictCcdThresholdRatio,
            ushort strictCcdRemainingSteps,
            bool strictCcdActive,
            bool teleportedThisStep)
            : this(
                key,
                lifecycle,
                capabilities,
                physicsCapabilities,
                executionModel,
                lane,
                previousPosition,
                position,
                0f,
                0f,
                velocity,
                acceleration,
                linearDamping,
                scale,
                color,
                radius,
                remainingLifetime,
                visualId,
                ownerId,
                teamId,
                physicsCategoryMask,
                interactionLayerMask,
                includeTriggers,
                physicsShape,
                strictCcdOverride,
                strictCcdThresholdRatio,
                strictCcdRemainingSteps,
                strictCcdActive,
                teleportedThisStep)
        {
        }



        /// <summary>
        /// 현재 Element의 물리·표시 상태와 이전·현재 회전을 포함한 불변 snapshot을 생성합니다.
        /// </summary>
        public ElementSnapshot(
            ElementKey key,
            ElementLifecycle lifecycle,
            ElementCapabilities capabilities,
            ElementPhysicsCapability physicsCapabilities,
            ElementExecutionModel2D executionModel,
            ElementLane lane,
            float2 previousPosition,
            float2 position,
            float previousRotationRadians,
            float rotationRadians,
            float2 velocity,
            float2 acceleration,
            float linearDamping,
            float2 scale,
            UnityEngine.Color32 color,
            float radius,
            float remainingLifetime,
            int visualId,
            uint ownerId,
            uint teamId,
            ulong physicsCategoryMask,
            ulong interactionLayerMask,
            bool includeTriggers,
            PhysicsCoreShape2D physicsShape,
            StrictCcdOverride2D strictCcdOverride,
            float strictCcdThresholdRatio,
            ushort strictCcdRemainingSteps,
            bool strictCcdActive,
            bool teleportedThisStep)
        {
            Key = key;
            Lifecycle = lifecycle;
            Capabilities = capabilities;
            PhysicsCapabilities = physicsCapabilities;
            ExecutionModel = executionModel;
            Lane = lane;
            PreviousPosition = previousPosition;
            Position = position;
            PreviousRotationRadians = previousRotationRadians;
            RotationRadians = rotationRadians;
            Velocity = velocity;
            Acceleration = acceleration;
            LinearDamping = linearDamping;
            Scale = scale;
            Color = color;
            Radius = radius;
            RemainingLifetime = remainingLifetime;
            VisualId = visualId;
            OwnerId = ownerId;
            TeamId = teamId;
            PhysicsCategoryMask = physicsCategoryMask;
            InteractionLayerMask = interactionLayerMask;
            IncludeTriggers = includeTriggers;
            PhysicsShape = physicsShape;
            StrictCcdOverride = strictCcdOverride;
            StrictCcdThresholdRatio = strictCcdThresholdRatio;
            StrictCcdRemainingSteps = strictCcdRemainingSteps;
            StrictCcdActive = strictCcdActive;
            TeleportedThisStep = teleportedThisStep;
        }



        public ElementKey Key { get; }
        public ElementLifecycle Lifecycle { get; }
        public ElementCapabilities Capabilities { get; }
        public ElementPhysicsCapability PhysicsCapabilities { get; }
        public ElementExecutionModel2D ExecutionModel { get; }
        public ElementLane Lane { get; }
        public float2 PreviousPosition { get; }
        public float2 Position { get; }
        public float PreviousRotationRadians { get; }
        public float RotationRadians { get; }
        public float2 Velocity { get; }
        public float2 Acceleration { get; }
        public float LinearDamping { get; }
        public float2 Scale { get; }
        public UnityEngine.Color32 Color { get; }
        public float Radius { get; }
        public float RemainingLifetime { get; }
        public int VisualId { get; }
        public uint OwnerId { get; }
        public uint TeamId { get; }
        public ulong PhysicsCategoryMask { get; }
        public ulong InteractionLayerMask { get; }
        public bool IncludeTriggers { get; }
        public PhysicsCoreShape2D PhysicsShape { get; }
        public StrictCcdOverride2D StrictCcdOverride { get; }
        public float StrictCcdThresholdRatio { get; }
        public ushort StrictCcdRemainingSteps { get; }
        public bool StrictCcdActive { get; }
        public bool TeleportedThisStep { get; }
    }



    /// <summary>
    /// 이전·현재 고정 스텝 pose와 그 사이를 보간한 표시 pose를 함께 제공합니다.
    /// </summary>
    /// <remarks>
    /// 충돌 판정에는 사용하지 않고 렌더링과 추적 이펙트의 시각적 위치에만 사용합니다.
    /// </remarks>
    public readonly struct ElementRenderSnapshot
    {
        public ElementRenderSnapshot(
            ElementKey key,
            float2 previousPosition,
            float2 currentPosition,
            float interpolationAlpha)
            : this(key, previousPosition, currentPosition, 0f, 0f, interpolationAlpha)
        {
        }



        /// <summary>
        /// 이전·현재 위치와 회전을 가장 짧은 각도 경로로 보간한 표시 snapshot을 생성합니다.
        /// </summary>
        public ElementRenderSnapshot(
            ElementKey key,
            float2 previousPosition,
            float2 currentPosition,
            float previousRotationRadians,
            float currentRotationRadians,
            float interpolationAlpha)
        {
            Key = key;
            PreviousPosition = previousPosition;
            CurrentPosition = currentPosition;
            PreviousRotationRadians = previousRotationRadians;
            CurrentRotationRadians = currentRotationRadians;
            InterpolationAlpha = NormalizeAlpha(interpolationAlpha);
            Position = math.lerp(previousPosition, currentPosition, InterpolationAlpha);
            float rotationDelta = math.atan2(
                math.sin(currentRotationRadians - previousRotationRadians),
                math.cos(currentRotationRadians - previousRotationRadians));
            RotationRadians = previousRotationRadians + rotationDelta * InterpolationAlpha;
        }



        public ElementKey Key { get; }
        public float2 PreviousPosition { get; }
        public float2 CurrentPosition { get; }
        public float2 Position { get; }
        public float PreviousRotationRadians { get; }
        public float CurrentRotationRadians { get; }
        public float RotationRadians { get; }
        public float InterpolationAlpha { get; }



        internal static float NormalizeAlpha(float alpha) =>
            math.isfinite(alpha) ? math.clamp(alpha, 0f, 1f) : 1f;
    }



    public struct ElementFact : IComparable<ElementFact>
    {
        public ElementFactType Type;
        public ElementKey Element;
        public ElementKey TargetElement;
        public int TargetId;
        public int BridgeTargetId;
        public float2 Position;
        public float2 Normal;
        public float2 ImpactCenter;
        public float TimeOfImpact;
        public uint Sequence;
        public ushort SubstepIndex;
        public uint FixedStepIndex;
        public byte IsTrigger;
        public ElementFactFlags Flags;



        /// <summary>
        /// 접촉을 만든 형상 중 하나라도 Trigger였는지 반환합니다.
        /// </summary>
        public bool TriggerContact => IsTrigger != 0;



        public float2 SurfacePoint => Position;
        public float2 SurfaceNormal => Normal;
        public bool StartedOverlapped => (Flags & ElementFactFlags.StartedOverlapped) != 0;
        public bool HasSurfacePoint => (Flags & ElementFactFlags.HasSurfacePoint) != 0;
        public bool HasSurfaceNormal => (Flags & ElementFactFlags.HasSurfaceNormal) != 0;
        public bool HasImpactCenter => (Flags & ElementFactFlags.HasImpactCenter) != 0;



        public int CompareTo(ElementFact other)
        {
            int fixedStepComparison = FixedStepIndex.CompareTo(other.FixedStepIndex);
            if (fixedStepComparison != 0) { return fixedStepComparison; }

            int substepComparison = SubstepIndex.CompareTo(other.SubstepIndex);
            if (substepComparison != 0) { return substepComparison; }

            int slotComparison = Element.Slot.CompareTo(other.Element.Slot);
            if (slotComparison != 0) { return slotComparison; }

            int typeComparison = Type.CompareTo(other.Type);
            if (typeComparison != 0) { return typeComparison; }

            int timeComparison = TimeOfImpact.CompareTo(other.TimeOfImpact);
            if (timeComparison != 0) { return timeComparison; }

            int bridgeComparison = BridgeTargetId.CompareTo(other.BridgeTargetId);
            if (bridgeComparison != 0) { return bridgeComparison; }

            int targetComparison = TargetId.CompareTo(other.TargetId);
            return targetComparison != 0 ? targetComparison : Sequence.CompareTo(other.Sequence);
        }
    }



    internal static class ElementFactGeometry
    {
        public static ElementFactFlags GetSurfaceFlags(float2 point, float2 normal)
        {
            ElementFactFlags flags = ElementFactFlags.None;
            if (math.all(math.isfinite(point))) { flags |= ElementFactFlags.HasSurfacePoint; }
            if (math.all(math.isfinite(normal)) && math.lengthsq(normal) > 0.00000001f)
            {
                flags |= ElementFactFlags.HasSurfaceNormal;
            }
            return flags;
        }
    }



    public readonly struct ElementCommand
    {
        public ElementCommand(ElementKey target, ElementCommandType type, float2 value, int intValue = 0)
            : this(target, type, value, default, intValue, 0)
        {
        }



        /// <summary>
        /// 두 개의 native float2 payload와 정수 payload로 확장 명령을 구성합니다.
        /// </summary>
        public ElementCommand(
            ElementKey target,
            ElementCommandType type,
            float2 value,
            float2 secondaryValue,
            int intValue = 0,
            int secondaryIntValue = 0)
        {
            Target = target;
            Type = type;
            Value = value;
            SecondaryValue = secondaryValue;
            IntValue = intValue;
            SecondaryIntValue = secondaryIntValue;
        }



        public ElementKey Target { get; }
        public ElementCommandType Type { get; }
        public float2 Value { get; }
        public float2 SecondaryValue { get; }
        public int IntValue { get; }
        public int SecondaryIntValue { get; }
    }



    public interface IElementCommand
    {
        ElementCommand ToElementCommand(ElementKey target);
    }



    public readonly struct SetElementPosition2D : IElementCommand
    {
        public SetElementPosition2D(float2 position) => Position = position;
        public float2 Position { get; }
        public ElementCommand ToElementCommand(ElementKey target) =>
            new ElementCommand(target, ElementCommandType.SetPosition2D, Position);
    }



    /// <summary>
    /// 연속 이동 sweep를 만들지 않고 현재 위치를 즉시 교체하고 teleport 진단 상태를 남깁니다.
    /// </summary>
    public readonly struct TeleportElement2D : IElementCommand
    {
        public TeleportElement2D(float2 position) => Position = position;
        public float2 Position { get; }
        public ElementCommand ToElementCommand(ElementKey target) =>
            new ElementCommand(target, ElementCommandType.Teleport2D, Position);
    }



    public readonly struct SetElementVelocity2D : IElementCommand
    {
        public SetElementVelocity2D(float2 velocity) => Velocity = velocity;
        public float2 Velocity { get; }
        public ElementCommand ToElementCommand(ElementKey target) =>
            new ElementCommand(target, ElementCommandType.SetVelocity2D, Velocity);
    }



    public readonly struct DespawnElement : IElementCommand
    {
        public ElementCommand ToElementCommand(ElementKey target) =>
            new ElementCommand(target, ElementCommandType.Despawn, default);
    }



    public readonly struct SetElementVisualId : IElementCommand
    {
        public SetElementVisualId(int visualId) => VisualId = visualId;
        public int VisualId { get; }
        public ElementCommand ToElementCommand(ElementKey target) =>
            new ElementCommand(target, ElementCommandType.SetVisualId, default, VisualId);
    }



    public readonly struct SetElementScale2D : IElementCommand
    {
        public SetElementScale2D(float2 scale) => Scale = scale;
        public float2 Scale { get; }
        public ElementCommand ToElementCommand(ElementKey target) =>
            new ElementCommand(target, ElementCommandType.SetScale2D, Scale);
    }



    /// <summary>
    /// Element의 표시 회전을 radians 단위의 절대 각도로 generation-safe하게 교체합니다.
    /// </summary>
    public readonly struct SetElementRotation2D : IElementCommand
    {
        public SetElementRotation2D(float rotationRadians)
        {
            RotationRadians = math.isfinite(rotationRadians) ? rotationRadians : 0f;
        }

        public float RotationRadians { get; }

        public ElementCommand ToElementCommand(ElementKey target) =>
            new ElementCommand(
                target,
                ElementCommandType.SetRotation2D,
                new float2(RotationRadians, 0f));
    }



    public readonly struct SetElementColor : IElementCommand
    {
        public SetElementColor(UnityEngine.Color32 color) => Color = color;
        public UnityEngine.Color32 Color { get; }
        public ElementCommand ToElementCommand(ElementKey target) =>
            new ElementCommand(target, ElementCommandType.SetColor, default, PackColor(Color));



        internal static int PackColor(UnityEngine.Color32 color) =>
            color.r | (color.g << 8) | (color.b << 16) | (color.a << 24);



        internal static UnityEngine.Color32 UnpackColor(int value) => new UnityEngine.Color32(
            (byte)(value & 0xff),
            (byte)((value >> 8) & 0xff),
            (byte)((value >> 16) & 0xff),
            (byte)((value >> 24) & 0xff));
    }



    public readonly struct SetElementRemainingLifetime : IElementCommand
    {
        public SetElementRemainingLifetime(float remainingLifetime) =>
            RemainingLifetime = math.max(0f, remainingLifetime);

        public float RemainingLifetime { get; }

        public ElementCommand ToElementCommand(ElementKey target) =>
            new ElementCommand(target, ElementCommandType.SetRemainingLifetime, new float2(RemainingLifetime, 0f));
    }



    public readonly struct SetElementAcceleration2D : IElementCommand
    {
        public SetElementAcceleration2D(float2 acceleration) => Acceleration = acceleration;
        public float2 Acceleration { get; }

        public ElementCommand ToElementCommand(ElementKey target) =>
            new ElementCommand(target, ElementCommandType.SetAcceleration2D, Acceleration);
    }



    public readonly struct SetElementLinearDamping : IElementCommand
    {
        public SetElementLinearDamping(float linearDamping) => LinearDamping = math.max(0f, linearDamping);
        public float LinearDamping { get; }

        public ElementCommand ToElementCommand(ElementKey target) =>
            new ElementCommand(target, ElementCommandType.SetLinearDamping, new float2(LinearDamping, 0f));
    }



    /// <summary>
    /// 지정한 고정 스텝 수 동안 엄격 CCD 후보 판정을 강제로 활성화합니다.
    /// </summary>
    public readonly struct RequestElementStrictCcd2D : IElementCommand
    {
        public RequestElementStrictCcd2D(int fixedStepCount) => FixedStepCount = math.max(0, fixedStepCount);
        public int FixedStepCount { get; }

        public ElementCommand ToElementCommand(ElementKey target) =>
            new ElementCommand(target, ElementCommandType.RequestStrictCcd2D, default, FixedStepCount);
    }



    /// <summary>
    /// 현재 pose 변경이 연속 이동이 아닌 순간이동임을 다음 고정 스텝에 알립니다.
    /// </summary>
    public readonly struct MarkElementTeleported2D : IElementCommand
    {
        public ElementCommand ToElementCommand(ElementKey target) =>
            new ElementCommand(target, ElementCommandType.MarkTeleported2D, default);
    }



    public interface IElement
    {
        ElementKey Key { get; }
        ElementLifecycle Lifecycle { get; }
        ElementCapabilities Capabilities { get; }
        ElementPhysicsCapability PhysicsCapabilities { get; }
        bool IsAlive { get; }
        bool TryGetSnapshot(out ElementSnapshot snapshot);
        bool TrySubmit<TCommand>(in TCommand command) where TCommand : unmanaged, IElementCommand;
    }



    public readonly struct ElementHandle : IElement, IEquatable<ElementHandle>
    {
        internal ElementHandle(ElementWorld world, ElementKey key)
        {
            World = world;
            Key = key;
        }



        internal ElementWorld World { get; }
        public ElementKey Key { get; }
        public bool IsAlive => World != null && World.IsAlive(Key);



        public ElementLifecycle Lifecycle =>
            World != null && World.TryGetLifecycle(Key, out ElementLifecycle lifecycle) ? lifecycle : ElementLifecycle.Invalid;



        public ElementCapabilities Capabilities =>
            TryGetSnapshot(out ElementSnapshot snapshot) ? snapshot.Capabilities : ElementCapabilities.None;



        public ElementPhysicsCapability PhysicsCapabilities =>
            TryGetSnapshot(out ElementSnapshot snapshot) ? snapshot.PhysicsCapabilities : ElementPhysicsCapability.None;



        public bool TryGetSnapshot(out ElementSnapshot snapshot)
        {
            if (World != null) { return World.TryGetSnapshot(Key, out snapshot); }
            snapshot = default;
            return false;
        }



        /// <summary>
        /// 이 Element의 고정 스텝 사이 표시 pose를 generation-safe하게 조회합니다.
        /// </summary>
        public bool TryGetRenderSnapshot(float interpolationAlpha, out ElementRenderSnapshot snapshot)
        {
            if (World != null) { return World.TryGetRenderSnapshot(Key, interpolationAlpha, out snapshot); }
            snapshot = default;
            return false;
        }



        public bool TrySubmit<TCommand>(in TCommand command) where TCommand : unmanaged, IElementCommand
        {
            return World != null && World.TrySubmit(command.ToElementCommand(Key));
        }



        public void Submit<TCommand>(in TCommand command) where TCommand : unmanaged, IElementCommand
        {
            if (!TrySubmit(in command))
            {
                throw new InvalidOperationException($"유효하지 않은 Element handle에는 명령을 제출할 수 없습니다: {Key}");
            }
        }



        public bool RequestStrictCcd(int fixedStepCount) =>
            TrySubmit(new RequestElementStrictCcd2D(fixedStepCount));



        public bool MarkTeleported() => TrySubmit(new MarkElementTeleported2D());

        public bool Teleport(float2 position) => TrySubmit(new TeleportElement2D(position));



        public bool Equals(ElementHandle other) => ReferenceEquals(World, other.World) && Key.Equals(other.Key);
        public override bool Equals(object obj) => obj is ElementHandle other && Equals(other);
        public override int GetHashCode() => Key.GetHashCode();
        public static bool operator ==(ElementHandle left, ElementHandle right) => left.Equals(right);
        public static bool operator !=(ElementHandle left, ElementHandle right) => !left.Equals(right);
    }



    /// <summary>
    /// 에디터와 Development 진단 화면이 한 번에 읽는 ElementWorld 상태입니다.
    /// </summary>
    public readonly struct ElementWorldDiagnostics
    {
        public ElementWorldDiagnostics(
            int aliveCount,
            int queryCount,
            int areaCount,
            int simulatedCount,
            int physicsBodyCount,
            int lastCommandCount,
            int lastFactCount,
            int lastQueryFactCount,
            int strictCcdCandidateCount,
            int forcedStrictCcdCount,
            int teleportCount,
            uint fixedStepIndex,
            double simulationMilliseconds,
            double factDispatchMilliseconds)
            : this(
                aliveCount,
                queryCount,
                areaCount,
                simulatedCount,
                physicsBodyCount,
                lastCommandCount,
                lastFactCount,
                lastQueryFactCount,
                strictCcdCandidateCount,
                forcedStrictCcdCount,
                teleportCount,
                0,
                0,
                0,
                0,
                0,
                fixedStepIndex,
                simulationMilliseconds,
                factDispatchMilliseconds)
        {
        }



        /// <summary>
        /// sparse 이동·표시·경계 진단을 포함한 ElementWorld 상태를 생성합니다.
        /// </summary>
        public ElementWorldDiagnostics(
            int aliveCount,
            int queryCount,
            int areaCount,
            int simulatedCount,
            int physicsBodyCount,
            int lastCommandCount,
            int lastFactCount,
            int lastQueryFactCount,
            int strictCcdCandidateCount,
            int forcedStrictCcdCount,
            int teleportCount,
            int directionalMotionFeatureCount,
            int waveMotionFeatureCount,
            int visualOrientationFeatureCount,
            int boundaryFeatureCount,
            int boundaryExitCount,
            uint fixedStepIndex,
            double simulationMilliseconds,
            double factDispatchMilliseconds)
        {
            AliveCount = aliveCount;
            QueryCount = queryCount;
            AreaCount = areaCount;
            SimulatedCount = simulatedCount;
            PhysicsBodyCount = physicsBodyCount;
            LastCommandCount = lastCommandCount;
            LastFactCount = lastFactCount;
            LastQueryFactCount = lastQueryFactCount;
            StrictCcdCandidateCount = strictCcdCandidateCount;
            ForcedStrictCcdCount = forcedStrictCcdCount;
            TeleportCount = teleportCount;
            DirectionalMotionFeatureCount = directionalMotionFeatureCount;
            WaveMotionFeatureCount = waveMotionFeatureCount;
            VisualOrientationFeatureCount = visualOrientationFeatureCount;
            BoundaryFeatureCount = boundaryFeatureCount;
            BoundaryExitCount = boundaryExitCount;
            FixedStepIndex = fixedStepIndex;
            SimulationMilliseconds = simulationMilliseconds;
            FactDispatchMilliseconds = factDispatchMilliseconds;
        }



        public int AliveCount { get; }
        public int QueryCount { get; }
        public int AreaCount { get; }
        public int SimulatedCount { get; }
        public int PhysicsBodyCount { get; }
        public int LastCommandCount { get; }
        public int LastFactCount { get; }
        public int LastQueryFactCount { get; }
        public int StrictCcdCandidateCount { get; }
        public int ForcedStrictCcdCount { get; }
        public int TeleportCount { get; }
        public int DirectionalMotionFeatureCount { get; }
        public int WaveMotionFeatureCount { get; }
        public int VisualOrientationFeatureCount { get; }
        public int BoundaryFeatureCount { get; }
        public int BoundaryExitCount { get; }
        public uint FixedStepIndex { get; }
        public double SimulationMilliseconds { get; }
        public double FactDispatchMilliseconds { get; }
    }



    /// <summary>
    /// 외부 물리 공간에서 이전·현재 pose가 달라진 형상을 엄격 CCD 입력으로 전달합니다.
    /// </summary>
    /// <remarks>
    /// 이 구조체는 Collider2D 같은 managed 객체를 보관하지 않습니다. Physics2D 브리지는 고정 스텝
    /// 동기화 경계에서 형상을 값으로 변환한 뒤 <see cref="PhysicsCore2DLane.ReplaceMovingProjections"/>에 제출합니다.
    /// </remarks>
    public struct MovingProjectionShape2D : IComparable<MovingProjectionShape2D>
    {
        public PhysicsShape.ShapeProxy Shape;
        public PhysicsTransform PreviousTransform;
        public PhysicsTransform CurrentTransform;
        public int BridgeTargetId;
        public int StableShapeId;
        public ulong CategoryMask;
        public ulong ContactMask;
        public byte IsTrigger;
        public byte Teleported;



        public bool IsValid => Shape.isValid && BridgeTargetId >= 0;
        public bool IsTeleported => Teleported != 0;



        public int CompareTo(MovingProjectionShape2D other)
        {
            int targetComparison = BridgeTargetId.CompareTo(other.BridgeTargetId);
            return targetComparison != 0
                ? targetComparison
                : StableShapeId.CompareTo(other.StableShapeId);
        }
    }



    public readonly struct ElementSpawnResult
    {
        public ElementSpawnResult(ElementSpawnStatus status, ElementHandle handle)
        {
            Status = status;
            Handle = handle;
        }



        public ElementSpawnStatus Status { get; }
        public ElementHandle Handle { get; }
        public bool Succeeded => Status == ElementSpawnStatus.Success;
    }
}
