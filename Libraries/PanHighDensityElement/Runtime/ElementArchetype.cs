using System;
using Unity.Mathematics;
using UnityEngine;



namespace Pan.HighDensityElement
{
    [CreateAssetMenu(menuName = "Pan/High Density Element/Element Archetype", fileName = "ElementArchetype")]
    public sealed class ElementArchetype : ScriptableObject
    {
        [SerializeField]
        private ElementCapabilities features =
            ElementCapabilities.Lifetime |
            ElementCapabilities.KinematicMotion2D |
            ElementCapabilities.SpriteVisual2D;

        [SerializeField, Min(0f)]
        private float radius = 0.1f;

        [SerializeField, Min(0f)]
        private float lifetime = 5f;

        [SerializeField]
        private int visualId;

        [SerializeField, Min(0f)]
        private float periodicInterval;

        [SerializeField]
        private bool penetrating;

        [SerializeField]
        private PhysicsCoreBodyMode physicsBodyMode;

        [SerializeField]
        private PhysicsCoreShape2D physicsShape;

        [SerializeField]
        private bool physicsIsTrigger;

        [SerializeField]
        private StrictCcdOverride2D strictCcdOverride;

        [SerializeField, Min(0.01f)]
        private float strictCcdThresholdRatio = 0.5f;



        public ElementCapabilities Features => features;



        public bool TryCompile(out ElementCompiledArchetype compiled, out ElementSpawnStatus failure)
        {
            return ElementCompiledArchetype.TryCreate(
                features,
                radius,
                lifetime,
                visualId,
                periodicInterval,
                penetrating,
                physicsBodyMode,
                physicsShape,
                out compiled,
                out failure,
                physicsIsTrigger,
                strictCcdOverride,
                strictCcdThresholdRatio);
        }
    }



    [Serializable]
    public readonly struct ElementCompiledArchetype
    {
        private ElementCompiledArchetype(
            ElementCapabilities capabilities,
            ElementLane lane,
            float radius,
            float lifetime,
            int visualId,
            float periodicInterval,
            bool penetrating,
            PhysicsCoreBodyMode physicsBodyMode,
            PhysicsCoreShape2D physicsShape,
            bool physicsIsTrigger,
            StrictCcdOverride2D strictCcdOverride,
            float strictCcdThresholdRatio)
        {
            Capabilities = capabilities;
            Lane = lane;
            Radius = radius;
            Lifetime = lifetime;
            VisualId = visualId;
            PeriodicInterval = periodicInterval;
            Penetrating = penetrating;
            PhysicsBodyMode = physicsBodyMode;
            PhysicsShape = physicsShape;
            PhysicsIsTrigger = physicsIsTrigger;
            StrictCcdOverride = strictCcdOverride;
            StrictCcdThresholdRatio = strictCcdThresholdRatio;
        }



        public ElementCapabilities Capabilities { get; }
        public ElementLane Lane { get; }
        public float Radius { get; }
        public float Lifetime { get; }
        public int VisualId { get; }
        public float PeriodicInterval { get; }
        public bool Penetrating { get; }
        public PhysicsCoreBodyMode PhysicsBodyMode { get; }
        public PhysicsCoreShape2D PhysicsShape { get; }
        public bool PhysicsIsTrigger { get; }
        public StrictCcdOverride2D StrictCcdOverride { get; }
        public float StrictCcdThresholdRatio { get; }
        public ElementExecutionModel2D ExecutionModel =>
            (Capabilities & ElementCapabilities.DynamicBody2D) != 0
                ? ElementExecutionModel2D.Simulated
                : ElementExecutionModel2D.Query;
        public bool IsValid => Lane != ElementLane.None;



        public static bool TryCreate(
            ElementCapabilities capabilities,
            float radius,
            float lifetime,
            int visualId,
            float periodicInterval,
            bool penetrating,
            PhysicsCoreBodyMode physicsBodyMode,
            PhysicsCoreShape2D physicsShape,
            out ElementCompiledArchetype compiled,
            out ElementSpawnStatus failure,
            bool physicsIsTrigger = false,
            StrictCcdOverride2D strictCcdOverride = StrictCcdOverride2D.Auto,
            float strictCcdThresholdRatio = 0.5f)
        {
            compiled = default;

            if (radius < 0f || lifetime < 0f || periodicInterval < 0f ||
                !math.isfinite(strictCcdThresholdRatio) || strictCcdThresholdRatio <= 0f)
            {
                failure = ElementSpawnStatus.InvalidArchetype;
                return false;
            }

            bool usesQuerySensor = (capabilities & ElementCapabilities.QuerySensor2D) != 0;
            bool usesAreaSensor = (capabilities & ElementCapabilities.AreaSensor2D) != 0;
            bool usesDynamicBody = (capabilities & ElementCapabilities.DynamicBody2D) != 0;
            bool usesQueryTarget = (capabilities & ElementCapabilities.QueryTarget2D) != 0;
            bool usesKinematic = (capabilities & ElementCapabilities.KinematicMotion2D) != 0;
            bool usesPeriodic = (capabilities & ElementCapabilities.PeriodicFact) != 0;
            const ElementCapabilities RuntimeSparseCapabilities =
                ElementCapabilities.DirectionalMotion2D |
                ElementCapabilities.WaveMotion2D |
                ElementCapabilities.VisualOrientation2D |
                ElementCapabilities.Boundary2D;

            if ((capabilities & RuntimeSparseCapabilities) != 0 ||
                (usesQuerySensor && (usesAreaSensor || usesDynamicBody)) ||
                (usesAreaSensor && usesDynamicBody) ||
                (usesQuerySensor && !usesKinematic))
            {
                failure = ElementSpawnStatus.UnsupportedLayout;
                return false;
            }

            if (usesPeriodic && (!usesAreaSensor || periodicInterval <= 0f))
            {
                failure = ElementSpawnStatus.UnsupportedLayout;
                return false;
            }

            ElementLane lane = usesAreaSensor
                ? ElementLane.AreaSensorSprite2D
                : usesDynamicBody || usesQueryTarget
                    ? ElementLane.DynamicBodySprite2D
                    : ElementLane.QuerySprite2D;

            compiled = new ElementCompiledArchetype(
                capabilities,
                lane,
                radius,
                lifetime,
                visualId,
                periodicInterval,
                penetrating,
                physicsBodyMode,
                physicsShape,
                physicsIsTrigger,
                strictCcdOverride,
                strictCcdThresholdRatio);
            failure = ElementSpawnStatus.Success;
            return true;
        }
    }



    public struct ElementSpawnBuilder
    {
        public ElementCompiledArchetype Archetype;
        public float2 Position;
        public float RotationRadians;
        public float2 Velocity;
        public float2 Acceleration;
        public float LinearDamping;
        public float2 Scale;
        public Color32 Color;
        public uint OwnerId;
        public uint TeamId;
        public ulong PhysicsCategoryMask;
        public ulong InteractionLayerMask;
        public ElementPhysicsMaterial2D PhysicsMaterial;
        public float LifetimeOverride;
        public byte HasLifetimeOverride;
        public byte HasPhysicsMaterial;
        public byte IncludeTriggers;
        public StrictCcdOverride2D StrictCcdOverride;
        public float StrictCcdThresholdRatio;



        public static bool TryCreate(
            ElementArchetype archetype,
            out ElementSpawnBuilder builder,
            out ElementSpawnStatus failure)
        {
            builder = default;
            failure = ElementSpawnStatus.InvalidArchetype;
            if (archetype == null || !archetype.TryCompile(out ElementCompiledArchetype compiled, out failure))
            {
                return false;
            }

            builder = From(compiled);
            return true;
        }



        public static ElementSpawnBuilder From(ElementCompiledArchetype archetype)
        {
            return new ElementSpawnBuilder
            {
                Archetype = archetype,
                Scale = new float2(1f),
                Color = new Color32(255, 255, 255, 255),
                PhysicsCategoryMask = 1ul,
                InteractionLayerMask = ulong.MaxValue,
                IncludeTriggers = 1,
                StrictCcdOverride = archetype.StrictCcdOverride,
                StrictCcdThresholdRatio = archetype.StrictCcdThresholdRatio
            };
        }



        public ElementSpawnBuilder WithPose(float2 position, float2 scale)
        {
            Position = position;
            Scale = scale;
            return this;
        }



        /// <summary>
        /// 초기 위치·크기·표시 회전을 함께 설정합니다.
        /// </summary>
        public ElementSpawnBuilder WithPose(
            float2 position,
            float2 scale,
            float rotationRadians)
        {
            Position = position;
            Scale = scale;
            RotationRadians = math.isfinite(rotationRadians) ? rotationRadians : 0f;
            return this;
        }



        /// <summary>
        /// 초기 표시 회전을 radians 단위로 설정합니다.
        /// </summary>
        public ElementSpawnBuilder WithRotation(float rotationRadians)
        {
            RotationRadians = math.isfinite(rotationRadians) ? rotationRadians : 0f;
            return this;
        }



        public ElementSpawnBuilder WithVelocity(float2 velocity)
        {
            Velocity = velocity;
            return this;
        }



        public ElementSpawnBuilder WithMotion(float2 acceleration, float linearDamping)
        {
            Acceleration = acceleration;
            LinearDamping = math.max(0f, linearDamping);
            return this;
        }



        public ElementSpawnBuilder WithOwner(uint ownerId, uint teamId)
        {
            OwnerId = ownerId;
            TeamId = teamId;
            return this;
        }



        public ElementSpawnBuilder WithPhysicsFilter(ulong categories, ulong hitCategories)
        {
            PhysicsCategoryMask = categories;
            InteractionLayerMask = hitCategories;
            return this;
        }



        public ElementSpawnBuilder WithPhysicsMaterial(in ElementPhysicsMaterial2D material)
        {
            PhysicsMaterial = material;
            HasPhysicsMaterial = 1;
            return this;
        }



        public ElementSpawnBuilder WithLifetime(float lifetime)
        {
            LifetimeOverride = math.max(0f, lifetime);
            HasLifetimeOverride = 1;
            return this;
        }



        public ElementSpawnBuilder WithColor(Color32 color)
        {
            Color = color;
            return this;
        }



        public ElementSpawnBuilder WithTriggerInteraction(bool includeTriggers)
        {
            IncludeTriggers = (byte)(includeTriggers ? 1 : 0);
            return this;
        }



        /// <summary>
        /// 이 spawn에 적용할 엄격 CCD 후보 판정 정책을 덮어씁니다.
        /// </summary>
        public ElementSpawnBuilder WithStrictCcd(
            StrictCcdOverride2D strictCcdOverride,
            float thresholdRatio = 0.5f)
        {
            StrictCcdOverride = strictCcdOverride;
            StrictCcdThresholdRatio = math.max(0.01f, thresholdRatio);
            return this;
        }
    }
}
