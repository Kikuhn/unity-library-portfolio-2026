using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Pan.HighDensityProjectile
{

    /// <summary>
    /// 투사체 상태를 `NativeList`에 소유하고, 이동/수명 갱신을 Burst job으로 처리하는 `IPanProjectileBody` 구현입니다.
    /// hit 판정과 gameplay event는 `ProjectileInteractionPipeline`을 재사용하므로 target/receiver 쪽은 backend 종류를 몰라도 됩니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class NativeProjectileBody : MonoBehaviour, IPanProjectileBody, IProjectileBatchSpawnTarget, IProjectileBodyInitializable, IProjectileBodySimulationMetrics, IProjectilePhysicsHitPolicyConfigurable
    {
        [FoldoutGroup("Native Backend"), SerializeField, Min(1), LabelText("초기 Capacity"), Tooltip("NativeList 초기 capacity입니다. 부족하면 런타임에 2배씩 확장합니다.")]
        private int initialCapacity = 512;

        [FoldoutGroup("충돌"), SerializeField, Min(1), LabelText("Hit Buffer 크기"), Tooltip("한 발의 Unity Physics fallback hit query에서 재사용할 collider buffer 크기입니다.")]
        private int hitBufferSize = 16;

        [FoldoutGroup("Simulation"), SerializeField, LabelText("Update 자동 Simulation"), Tooltip("On이면 Update에서 자동으로 Simulate를 호출합니다. 패턴 컨트롤러가 직접 Tick한다면 Off로 둡니다.")]
        private bool simulateOnUpdate = true;

        [FoldoutGroup("Simulation"), SerializeField, LabelText("Unscaled Time 사용"), Tooltip("On이면 자동 Update simulation에 Time.unscaledDeltaTime을 사용합니다.")]
        private bool useUnscaledDeltaTime;

        [FoldoutGroup("Simulation"), SerializeField, Min(1), LabelText("Movement Job Batch Size"), Tooltip("Burst movement job의 batch size입니다.")]
        private int movementJobBatchSize = 64;

        [SerializeField, Tooltip("On이면 receiver가 없는 Unity Physics collider hit도 소비된 hit로 처리합니다.")]
        private bool consumePhysicsHitWithoutReceiver;



        private NativeList<NativeProjectileSlot> slots;
        private Component[] sourceSidecar;
        private int nextProjectileId = 1;
        private int totalSpawned;
        private int totalDespawned;
        private int totalHits;
        private int lastMovementJobSimulatedCount;
        private ProjectileInteractionPipeline interactionPipeline;



        /// <summary>현재 활성 투사체 수입니다.</summary>
        public int ActiveCount => slots.IsCreated ? slots.Length : 0;

        /// <summary>현재 NativeList capacity입니다.</summary>
        public int Capacity => slots.IsCreated ? slots.Capacity : 0;

        /// <summary>누적 생성 수입니다.</summary>
        public int TotalSpawned => totalSpawned;

        /// <summary>누적 제거 수입니다.</summary>
        public int TotalDespawned => totalDespawned;

        /// <summary>target이 consume한 hit 누적 수입니다.</summary>
        public int TotalHits => totalHits;

        /// <summary>마지막 simulation에서 Burst job이 갱신한 투사체 수입니다.</summary>
        public int LastMovementJobSimulatedCount => lastMovementJobSimulatedCount;

        /// <summary>Update에서 자동 simulation을 수행할지 결정합니다.</summary>
        public bool SimulateOnUpdate { get => simulateOnUpdate; set => simulateOnUpdate = value; }

        /// <summary>자동 simulation에서 unscaled time을 사용할지 결정합니다.</summary>
        public bool UseUnscaledDeltaTime { get => useUnscaledDeltaTime; set => useUnscaledDeltaTime = value; }

        /// <summary>Burst movement job batch size입니다.</summary>
        public int MovementJobBatchSize { get => movementJobBatchSize; set => movementJobBatchSize = Mathf.Max(1, value); }

        public bool ConsumePhysicsHitWithoutReceiver { get => consumePhysicsHitWithoutReceiver; set => consumePhysicsHitWithoutReceiver = value; }

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



        [Button("현재 Native Body 상태 로그")]
        private void LogInspectorDiagnostics()
        {
            Debug.Log($"[NativeProjectileBody] Active={ActiveCount}, Capacity={Capacity}, Spawned={TotalSpawned}, Despawned={TotalDespawned}, Hits={TotalHits}, LastJob={lastMovementJobSimulatedCount}", this);
        }



        private void Awake()
        {
            EnsureInitialized();
        }



        private void OnDestroy()
        {
            DisposeNativeState();
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
        /// Native storage와 hit buffer를 명시 capacity로 초기화합니다.
        /// event 구독자는 유지하고, 현재 활성 투사체와 통계는 초기화합니다. projectile id 시퀀스는 유지해 stale handle이 새 탄을 가리키지 않게 합니다.
        /// </summary>
        public void Initialize(int capacity, int collisionBufferSize = 16)
        {
            initialCapacity = Mathf.Max(1, capacity);
            hitBufferSize = Mathf.Max(1, collisionBufferSize);
            DisposeNativeState();
            slots = new NativeList<NativeProjectileSlot>(initialCapacity, Allocator.Persistent);
            sourceSidecar = new Component[initialCapacity];
            totalSpawned = 0;
            totalDespawned = 0;
            totalHits = 0;
            lastMovementJobSimulatedCount = 0;
            ProjectileInteractionPipeline pipeline = EnsureInteractionPipeline();
            pipeline.Initialize(hitBufferSize);
            pipeline.PrepareNotificationCapacity(initialCapacity);
        }



        /// <summary>
        /// controller/bootstrap이 concrete native body 타입을 몰라도 저장소와 interaction buffer를 초기화할 수 있게 하는 선택 계약 구현입니다.
        /// </summary>
        public void InitializeProjectileBody(in ProjectileBodyInitializationSettings settings)
        {
            Initialize(settings.Capacity, settings.CollisionBufferSize);
        }



        /// <summary>
        /// NativeList에 새 투사체를 추가하고 backend 고유 id를 반환합니다.
        /// source component는 job에 넣지 않고 managed sidecar에 따로 보관합니다.
        /// </summary>
        public bool TrySpawn(in ProjectileSpawnRequest spawnData, out int projectileId)
        {
            EnsureInitialized();
            EnsureCapacity(slots.Length + 1);

            projectileId = nextProjectileId++;
            NativeProjectileSlot slot = new NativeProjectileSlot(
            projectileId,
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

            int index = slots.Length;
            sourceSidecar[index] = spawnData.Source;
            slots.Add(slot);
            totalSpawned++;

            ProjectileSnapshot spawnedSnapshot = slot.ToSnapshot();
            EnsureInteractionPipeline().NotifySpawned(in spawnedSnapshot);
            return true;
        }



        /// <summary>
        /// 투사체를 생성하고 id를 반환합니다. 실패 가능성을 직접 처리해야 한다면 `TrySpawn`을 사용합니다.
        /// </summary>
        public int Spawn(in ProjectileSpawnRequest spawnData)
        {
            TrySpawn(in spawnData, out int projectileId);
            return projectileId;
        }



        /// <summary>
        /// 여러 spawn data를 한 번에 받아 NativeList capacity와 managed sidecar를 한 번만 준비한 뒤 순서대로 생성합니다.
        /// hit/query/event 계약은 개별 `TrySpawn`과 동일하게 유지합니다.
        /// </summary>
        public int TrySpawnBatch(ProjectileSpawnRequest[] spawnDataBuffer, int count, int[] projectileIds = null)
        {
            EnsureInitialized();
            if (spawnDataBuffer == null || count <= 0)
            {
                return 0;
            }

            int requestedCount = Mathf.Min(count, spawnDataBuffer.Length);
            EnsureCapacity(slots.Length + requestedCount);

            int spawned = 0;
            ProjectileInteractionPipeline pipeline = EnsureInteractionPipeline();
            for (int i = 0; i < requestedCount; i++)
            {
                ProjectileSpawnRequest spawnData = spawnDataBuffer[i];
                int projectileId = nextProjectileId++;
                NativeProjectileSlot slot = new NativeProjectileSlot(
                projectileId,
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

                int index = slots.Length;
                sourceSidecar[index] = spawnData.Source;
                slots.Add(slot);
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
        /// 이동/수명 갱신은 Burst job으로 처리하고, hit dispatch는 main-thread pipeline에서 처리합니다.
        /// </summary>
        public void Simulate(float deltaTime)
        {
            EnsureInitialized();
            if (deltaTime <= 0f || slots.Length == 0)
            {
                return;
            }

            ProjectileInteractionPipeline pipeline = EnsureInteractionPipeline();
            pipeline.BeginSimulationStep();

            try
            {
                NativeArray<NativeProjectileSlot> slotArray = slots.AsArray();
                NativeProjectileMovementJob job = new NativeProjectileMovementJob
                {
                    Slots = slotArray,
                    DeltaTime = deltaTime,
                };

                JobHandle handle = job.Schedule(slotArray.Length, movementJobBatchSize);
                handle.Complete();

                lastMovementJobSimulatedCount = slotArray.Length;
                for (int i = slots.Length - 1; i >= 0; i--)
                {
                    if (i >= slots.Length)
                    {
                        i = slots.Length;
                        continue;
                    }

                    NativeProjectileSlot slot = slots[i];
                    if (slot.RemainingLifetime <= 0f)
                    {
                        RemoveAt(i);
                        continue;
                    }

                    if (slot.HitLayerMask != 0 && TryHit(i, in slot))
                    {
                        totalHits++;
                        RemoveAtIfSlotStillMatches(i, slot.ProjectileId);
                    }
                }
            }
            finally
            {
                pipeline.EndSimulationStep();
            }
        }



        /// <summary>
        /// 모든 활성 투사체를 제거하고 despawn event를 발생시킵니다.
        /// </summary>
        public void ClearAll()
        {
            EnsureInitialized();
            ProjectileInteractionPipeline pipeline = EnsureInteractionPipeline();
            for (int i = 0; i < slots.Length; i++)
            {
                ProjectileSnapshot snapshot = slots[i].ToSnapshot();
                pipeline.NotifyDespawned(in snapshot);
            }

            int removedCount = slots.Length;
            slots.Clear();
            if (sourceSidecar != null && removedCount > 0)
            {
                System.Array.Clear(sourceSidecar, 0, Mathf.Min(removedCount, sourceSidecar.Length));
            }

            totalDespawned += removedCount;
            lastMovementJobSimulatedCount = 0;
            pipeline.FlushPendingNotificationsIfIdle();
        }



        /// <summary>
        /// projectile id가 일치하는 투사체를 제거합니다.
        /// </summary>
        public bool Despawn(int projectileId)
        {
            EnsureInitialized();
            for (int i = 0; i < slots.Length; i++)
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
        /// 활성 순서 기준 snapshot을 가져옵니다. 제거는 swap-back 방식이므로 순서는 안정 계약이 아닙니다.
        /// </summary>
        public bool TryGetSnapshot(int index, out ProjectileSnapshot snapshot)
        {
            EnsureInitialized();
            if (index < 0 || index >= slots.Length)
            {
                snapshot = default;
                return false;
            }

            snapshot = slots[index].ToSnapshot();
            return true;
        }



        /// <summary>
        /// projectile id 기준 snapshot을 가져옵니다.
        /// </summary>
        public bool TryGetSnapshotById(int projectileId, out ProjectileSnapshot snapshot)
        {
            EnsureInitialized();
            for (int i = 0; i < slots.Length; i++)
            {
                NativeProjectileSlot slot = slots[i];
                if (slot.ProjectileId == projectileId)
                {
                    snapshot = slot.ToSnapshot();
                    return true;
                }
            }

            snapshot = default;
            return false;
        }



        /// <summary>
        /// 렌더링/디버깅용 snapshot을 managed buffer에 복사합니다.
        /// </summary>
        public int CopySnapshots(ProjectileSnapshot[] buffer)
        {
            EnsureInitialized();
            if (buffer == null || buffer.Length == 0)
            {
                return 0;
            }

            int count = Mathf.Min(slots.Length, buffer.Length);
            for (int i = 0; i < count; i++)
            {
                buffer[i] = slots[i].ToSnapshot();
            }

            return count;
        }



        private void EnsureInitialized()
        {
            if (slots.IsCreated)
            {
                return;
            }

            Initialize(initialCapacity, hitBufferSize);
        }



        private void EnsureCapacity(int requiredCapacity)
        {
            bool storageCapacityGrew = false;
            if (slots.Capacity < requiredCapacity)
            {
                int newCapacity = Mathf.Max(requiredCapacity, Mathf.Max(1, slots.Capacity * 2));
                slots.Capacity = newCapacity;
                storageCapacityGrew = true;
            }

            if (sourceSidecar == null || sourceSidecar.Length < slots.Capacity)
            {
                System.Array.Resize(ref sourceSidecar, slots.Capacity);
            }

            if (storageCapacityGrew)
            {
                EnsureInteractionPipeline().PrepareNotificationCapacity(slots.Capacity);
            }
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
        /// Native storage는 유지하고 Unity Physics hit buffer만 현재 설정에 맞춰 다시 준비합니다.
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



        /// <summary>
        /// NativeList와 managed sidecar를 함께 해제합니다.
        /// source component는 job에 넣을 수 없으므로 native slot과 같은 index의 sidecar 배열로만 보관합니다.
        /// </summary>
        private void DisposeNativeState()
        {
            if (slots.IsCreated)
            {
                slots.Dispose();
            }

            sourceSidecar = null;
        }
    }
}
