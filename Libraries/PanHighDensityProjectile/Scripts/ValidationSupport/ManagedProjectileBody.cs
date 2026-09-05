using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Pan.HighDensityProjectile
{

    /// <summary>
    /// `ManagedProjectileBody`가 simulation을 처리하는 backend 정책입니다.
    /// gameplay 충돌이 필요한 경우에는 main thread dispatch 경계를 유지합니다.
    /// </summary>
    public enum ProjectileSimulationBackend
    {
        /// <summary>이동, 수명, 충돌 dispatch를 모두 main thread에서 처리합니다.</summary>
        MainThread,

        /// <summary>충돌 layer가 없는 순수 이동 탄막만 Unity Jobs로 갱신하고, 충돌이 있으면 main thread로 fallback합니다.</summary>
        JobsMovementOnlyWhenNoCollision,

        /// <summary>이동/수명 갱신은 Unity Jobs로 처리하고, target query와 hit dispatch는 main thread에서 처리합니다.</summary>
        JobsMovementThenMainThreadCollision
    }



    /// <summary>
    /// 단일 component가 다수 투사체를 struct 배열로 관리하는 기본 projectile backend입니다.
    /// 탄 하나마다 별도 GameObject나 구체 물리 actor를 만들지 않기 위한 기준 구현입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class ManagedProjectileBody : MonoBehaviour, IPanProjectileBody, IProjectileBatchSpawnTarget, IProjectileBodyInitializable, IProjectileBodySimulationMetrics, IProjectilePhysicsHitPolicyConfigurable
    {
        [FoldoutGroup("검증 Backend"), SerializeField, Min(1), LabelText("초기 Capacity")] private int initialCapacity = 512;
        [FoldoutGroup("충돌"), SerializeField, Min(1), LabelText("Hit Buffer 크기")] private int hitBufferSize = 16;
        [FoldoutGroup("Simulation"), SerializeField, LabelText("Update 자동 Simulation")] private bool simulateOnUpdate = true;
        [FoldoutGroup("Simulation"), SerializeField, LabelText("Unscaled Time 사용")] private bool useUnscaledDeltaTime;
        [FoldoutGroup("Simulation"), SerializeField, LabelText("Simulation Backend")] private ProjectileSimulationBackend simulationBackend = ProjectileSimulationBackend.MainThread;
        [FoldoutGroup("Simulation"), SerializeField, Min(1), LabelText("Movement Job Batch Size")] private int movementJobBatchSize = 64;
        [FoldoutGroup("충돌"), SerializeField, LabelText("Receiver 없는 Physics Hit 소모")] private bool consumePhysicsHitWithoutReceiver;



        private ProjectileSlot[] slots;
        private int activeCount;
        private int nextProjectileId = 1;
        private int totalSpawned;
        private int totalDespawned;
        private int totalHits;
        private int lastMovementJobSimulatedCount;
        private NativeArray<float3> movementJobPositions;
        private NativeArray<float3> movementJobVelocities;
        private NativeArray<float> movementJobRemainingLifetimes;
        private NativeArray<byte> movementJobUseGravities;
        private NativeArray<float3> movementJobGravities;
        private NativeArray<float> movementJobLinearDampings;
        private NativeArray<float> movementJobFrictionCoefficients;
        private ProjectileInteractionPipeline interactionPipeline;



        /// <summary>현재 활성 투사체 수입니다.</summary>
        public int ActiveCount => activeCount;

        /// <summary>현재 내부 슬롯 배열 capacity입니다.</summary>
        public int Capacity => slots != null ? slots.Length : 0;

        /// <summary>누적 생성 수입니다.</summary>
        public int TotalSpawned => totalSpawned;

        /// <summary>누적 제거 수입니다.</summary>
        public int TotalDespawned => totalDespawned;

        /// <summary>target이 consume한 hit 누적 수입니다.</summary>
        public int TotalHits => totalHits;

        /// <summary>마지막 simulation에서 movement-only job backend가 처리한 투사체 수입니다.</summary>
        public int LastMovementJobSimulatedCount => lastMovementJobSimulatedCount;

        /// <summary>Update에서 자동 simulation을 수행할지 결정합니다.</summary>
        public bool SimulateOnUpdate { get => simulateOnUpdate; set => simulateOnUpdate = value; }

        /// <summary>이 manager의 simulation backend 정책입니다.</summary>
        public ProjectileSimulationBackend SimulationBackend { get => simulationBackend; set => simulationBackend = value; }

        /// <summary>Jobs movement-only backend에서 사용할 batch size입니다.</summary>
        public int MovementJobBatchSize { get => movementJobBatchSize; set => movementJobBatchSize = Mathf.Max(1, value); }

        public bool ConsumePhysicsHitWithoutReceiver { get => consumePhysicsHitWithoutReceiver; set => consumePhysicsHitWithoutReceiver = value; }

        /// <summary>
        /// 테스트 assembly에서 source override가 slot까지 전달됐는지 확인하기 위한 내부 진단 API입니다.
        /// production consumer가 slot 배열 구조를 알 필요가 없도록 public API로 노출하지 않습니다.
        /// </summary>
        internal bool TryGetSlotSourceForDiagnostics(int index, out Component source)
        {
            if (slots == null || index < 0 || index >= activeCount)
            {
                source = null;
                return false;
            }

            source = slots[index].Source;
            return true;
        }

        /// <summary>투사체 생성 event입니다.</summary>
        public event ProjectileSnapshotHandler ProjectileSpawned
        {
            add => EnsureInteractionPipeline().ProjectileSpawned += value;
            remove => EnsureInteractionPipeline().ProjectileSpawned -= value;
        }

        /// <summary>투사체 contact event입니다.</summary>
        public event ProjectileContactHandler ProjectileContacted
        {
            add => EnsureInteractionPipeline().ProjectileContacted += value;
            remove => EnsureInteractionPipeline().ProjectileContacted -= value;
        }

        /// <summary>투사체 제거 event입니다.</summary>
        public event ProjectileSnapshotHandler ProjectileDespawned
        {
            add => EnsureInteractionPipeline().ProjectileDespawned += value;
            remove => EnsureInteractionPipeline().ProjectileDespawned -= value;
        }



        private void OnValidate()
        {
            initialCapacity = Mathf.Max(1, initialCapacity);
            hitBufferSize = Mathf.Max(1, hitBufferSize);
            movementJobBatchSize = Mathf.Max(1, movementJobBatchSize);
        }



        [Button("현재 Managed Body 상태 로그")]
        private void LogInspectorDiagnostics()
        {
            Debug.Log($"[ManagedProjectileBody] Active={ActiveCount}, Capacity={Capacity}, Spawned={TotalSpawned}, Despawned={TotalDespawned}, Hits={TotalHits}, Backend={simulationBackend}", this);
        }



        private void Awake()
        {
            EnsureInitialized();
        }



        private void OnDestroy()
        {
            DisposeMovementJobBuffers();
        }



        private void Update()
        {
            if (!simulateOnUpdate)
            {
                return;
            }

            Simulate(useUnscaledDeltaTime ? Time.unscaledDeltaTime : Time.deltaTime);
        }



        /// <summary>
        /// 내부 슬롯과 collision buffer를 명시 capacity로 초기화합니다.
        /// 기존 활성 투사체와 통계는 모두 초기화되지만 projectile id 시퀀스는 유지해 stale handle이 새 탄을 가리키지 않게 합니다.
        /// </summary>
        public void Initialize(int capacity, int collisionBufferSize = 16)
        {
            initialCapacity = Mathf.Max(1, capacity);
            hitBufferSize = Mathf.Max(1, collisionBufferSize);
            slots = new ProjectileSlot[initialCapacity];
            activeCount = 0;
            totalSpawned = 0;
            totalDespawned = 0;
            totalHits = 0;
            lastMovementJobSimulatedCount = 0;
            DisposeMovementJobBuffers();
            ProjectileInteractionPipeline pipeline = EnsureInteractionPipeline();
            pipeline.Initialize(hitBufferSize);
            pipeline.PrepareNotificationCapacity(initialCapacity);
        }



        /// <summary>
        /// controller/bootstrap이 concrete manager 타입을 몰라도 저장소와 interaction buffer를 초기화할 수 있게 하는 선택 계약 구현입니다.
        /// </summary>
        public void InitializeProjectileBody(in ProjectileBodyInitializationSettings settings)
        {
            Initialize(settings.Capacity, settings.CollisionBufferSize);
        }



        /// <summary>
        /// 투사체 생성을 시도하고 id를 반환합니다. 현재 구현은 필요 시 capacity를 확장하므로 일반적으로 true를 반환합니다.
        /// </summary>
        public bool TrySpawn(in ProjectileSpawnRequest spawnData, out int projectileId)
        {
            EnsureInitialized();
            EnsureCapacity(activeCount + 1);

            projectileId = nextProjectileId++;
            ProjectileSlot slot = new ProjectileSlot(
            projectileId,
            spawnData.Source,
            spawnData.Kinematic.Position,
            spawnData.Kinematic.Velocity,
            Mathf.Max(0f, spawnData.Kinematic.Radius),
            Mathf.Max(0f, spawnData.Lifetime.InitialSeconds),
            spawnData.Hit.Damage,
            spawnData.Team.TeamId,
            spawnData.Collision.HitLayers,
            spawnData.Collision.IncludeTriggers,
            spawnData.Kinematic.Use2D,
            spawnData.Visual.FrameIndex,
            spawnData.Motion);

            slots[activeCount] = slot;
            activeCount++;
            totalSpawned++;
            ProjectileSnapshot spawnedSnapshot = slot.ToSnapshot();
            EnsureInteractionPipeline().NotifySpawned(in spawnedSnapshot);
            return true;
        }



        /// <summary>
        /// 투사체를 생성하고 id를 반환합니다. 실패 가능성을 명시적으로 다루려면 `TrySpawn`을 사용합니다.
        /// </summary>
        public int Spawn(in ProjectileSpawnRequest spawnData)
        {
            TrySpawn(in spawnData, out int projectileId);
            return projectileId;
        }



        /// <summary>
        /// 여러 spawn data를 한 번에 받아 내부 capacity를 한 번만 준비한 뒤 순서대로 생성합니다.
        /// 외부 사용법은 `IPanProjectileBody` 그대로 유지하면서 burst 생성 루프의 반복 준비 비용을 줄이는 선택 경로입니다.
        /// </summary>
        public int TrySpawnBatch(ProjectileSpawnRequest[] spawnDataBuffer, int count, int[] projectileIds = null)
        {
            EnsureInitialized();
            if (spawnDataBuffer == null || count <= 0)
            {
                return 0;
            }

            int requestedCount = Mathf.Min(count, spawnDataBuffer.Length);
            EnsureCapacity(activeCount + requestedCount);

            int spawned = 0;
            ProjectileInteractionPipeline pipeline = EnsureInteractionPipeline();
            for (int i = 0; i < requestedCount; i++)
            {
                ProjectileSpawnRequest spawnData = spawnDataBuffer[i];
                int projectileId = nextProjectileId++;
                ProjectileSlot slot = new ProjectileSlot(
                projectileId,
                spawnData.Source,
                spawnData.Kinematic.Position,
                spawnData.Kinematic.Velocity,
                Mathf.Max(0f, spawnData.Kinematic.Radius),
                Mathf.Max(0f, spawnData.Lifetime.InitialSeconds),
                spawnData.Hit.Damage,
                spawnData.Team.TeamId,
                spawnData.Collision.HitLayers,
                spawnData.Collision.IncludeTriggers,
                spawnData.Kinematic.Use2D,
                spawnData.Visual.FrameIndex,
                spawnData.Motion);

                slots[activeCount] = slot;
                activeCount++;
                totalSpawned++;

                if (projectileIds != null && spawned < projectileIds.Length)
                {
                    projectileIds[spawned] = projectileId;
                }

                spawned++;
                ProjectileSnapshot spawnedSnapshot = slot.ToSnapshot();
                pipeline.NotifySpawned(in spawnedSnapshot);
            }

            return spawned;
        }



        /// <summary>
        /// deltaTime만큼 모든 활성 투사체를 갱신합니다.
        /// target query가 있으면 simulation step 시작 시 target cache를 준비하고, contact event는 순회 안정성을 위해 step 끝에서 flush합니다.
        /// </summary>
        public void Simulate(float deltaTime)
        {
            EnsureInitialized();
            if (deltaTime <= 0f || activeCount == 0)
            {
                return;
            }

            ProjectileInteractionPipeline pipeline = EnsureInteractionPipeline();
            pipeline.BeginSimulationStep();

            try
            {
                if (simulationBackend == ProjectileSimulationBackend.JobsMovementOnlyWhenNoCollision &&
                TrySimulateMovementOnlyWithJobs(deltaTime))
                {
                    return;
                }

                if (simulationBackend == ProjectileSimulationBackend.JobsMovementThenMainThreadCollision &&
                TrySimulateMovementThenMainThreadCollision(deltaTime))
                {
                    return;
                }

                lastMovementJobSimulatedCount = 0;

                int index = 0;
                while (index < activeCount)
                {
                    ProjectileSlot slot = slots[index];
                    slot.RemainingLifetime -= deltaTime;

                    if (slot.RemainingLifetime <= 0f)
                    {
                        RemoveAt(index);
                        continue;
                    }

                    slot.Velocity = slot.Motion.IntegrateVelocity(slot.Velocity, deltaTime);
                    Vector3 nextPosition = slot.Position + slot.Velocity * deltaTime;
                    if (TryHit(in slot, nextPosition))
                    {
                        totalHits++;
                        if (RemoveAtIfSlotStillMatches(index, slot.ProjectileId))
                        {
                            continue;
                        }

                        index = Mathf.Min(index, activeCount);
                        continue;
                    }

                    slot.Position = nextPosition;
                    if (TryWriteSlotIfStillMatches(index, slot.ProjectileId, in slot))
                    {
                        index++;
                    }
                    else
                    {
                        index = Mathf.Min(index, activeCount);
                    }
                }
            }
            finally
            {
                pipeline.EndSimulationStep();
            }
        }



        /// <summary>
        /// 모든 활성 투사체를 제거하고 despawn event를 발행합니다.
        /// </summary>
        public void ClearAll()
        {
            ProjectileInteractionPipeline pipeline = EnsureInteractionPipeline();
            for (int i = 0; i < activeCount; i++)
            {
                ProjectileSnapshot snapshot = slots[i].ToSnapshot();
                pipeline.NotifyDespawned(in snapshot);
            }

            totalDespawned += activeCount;
            activeCount = 0;
            lastMovementJobSimulatedCount = 0;
            pipeline.FlushPendingNotificationsIfIdle();
        }



        /// <summary>
        /// projectile id가 일치하는 투사체를 제거합니다.
        /// </summary>
        public bool Despawn(int projectileId)
        {
            EnsureInitialized();
            for (int i = 0; i < activeCount; i++)
            {
                if (slots[i].ProjectileId == projectileId)
                {
                    RemoveAt(i);
                    return true;
                }
            }

            return false;
        }



        /// <summary>
        /// 활성 슬롯 순서 기준 snapshot을 가져옵니다.
        /// </summary>
        public bool TryGetSnapshot(int index, out ProjectileSnapshot snapshot)
        {
            EnsureInitialized();
            if (index < 0 || index >= activeCount)
            {
                snapshot = default;
                return false;
            }

            ProjectileSlot slot = slots[index];
            snapshot = slot.ToSnapshot();
            return true;
        }



        /// <summary>
        /// projectile id 기준 snapshot을 가져옵니다.
        /// </summary>
        public bool TryGetSnapshotById(int projectileId, out ProjectileSnapshot snapshot)
        {
            EnsureInitialized();
            for (int i = 0; i < activeCount; i++)
            {
                if (slots[i].ProjectileId == projectileId)
                {
                    snapshot = slots[i].ToSnapshot();
                    return true;
                }
            }

            snapshot = default;
            return false;
        }



        /// <summary>
        /// 현재 활성 투사체 snapshot을 buffer에 복사합니다.
        /// buffer가 작으면 들어가는 만큼만 복사하고, 복사한 개수를 반환합니다.
        /// </summary>
        public int CopySnapshots(ProjectileSnapshot[] buffer)
        {
            EnsureInitialized();
            if (buffer == null || buffer.Length == 0)
            {
                return 0;
            }

            int count = Mathf.Min(activeCount, buffer.Length);
            for (int i = 0; i < count; i++)
            {
                buffer[i] = slots[i].ToSnapshot();
            }

            return count;
        }



        private void EnsureInitialized()
        {
            if (slots != null)
            {
                return;
            }

            Initialize(initialCapacity, hitBufferSize);
        }



        private void EnsureCapacity(int requiredCapacity)
        {
            if (slots.Length >= requiredCapacity)
            {
                return;
            }

            int newCapacity = Mathf.Max(requiredCapacity, slots.Length * 2);
            System.Array.Resize(ref slots, newCapacity);
            EnsureInteractionPipeline().PrepareNotificationCapacity(newCapacity);
        }



        private ProjectileInteractionPipeline EnsureInteractionPipeline()
        {
            if (interactionPipeline == null)
            {
                interactionPipeline = new ProjectileInteractionPipeline(hitBufferSize);
            }

            return interactionPipeline;
        }



        /// <summary>
        /// 활성 투사체 저장소는 유지하고 Unity Physics hit buffer만 현재 설정에 맞춰 다시 준비합니다.
        /// </summary>
        private void ReinitializeInteractionPipelineBuffers()
        {
            if (interactionPipeline == null)
            {
                return;
            }

            interactionPipeline.Initialize(hitBufferSize);
            interactionPipeline.PrepareNotificationCapacity(Capacity > 0 ? Capacity : initialCapacity);
        }
    }
}
