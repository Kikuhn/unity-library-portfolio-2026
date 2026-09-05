using Sirenix.OdinInspector;
using UnityEngine;
namespace Pan.HighDensityProjectile
{

    public sealed partial class ProjectilePatternController
    {
        private void Awake()
        {
            EnsureRuntimeComponents();
        }



        private void Start()
        {
            if (playOnStart)
            {
                PlayPattern();
            }
        }



        private void OnDisable()
        {
            if (resetRuntimeStateOnDisable)
            {
                ResetRuntimeStateCore(false);
            }
        }



        private void OnValidate()
        {
            initialCapacity = Mathf.Max(1, initialCapacity);
            collisionBufferSize = Mathf.Max(1, collisionBufferSize);
            movementJobBatchSize = Mathf.Max(1, movementJobBatchSize);
            fixedRenderWorldScale = Mathf.Max(0f, fixedRenderWorldScale);
            runtimeConfigured = false;
        }



        [Button("Runtime 구성 다시 적용")]
        private void ReconfigureFromInspector()
        {
            ReconfigureRuntimeComponents();
        }



        [Button("현재 Controller 상태 로그")]
        private void LogInspectorDiagnostics()
        {
            ResolveProjectileBody();
            string bodyName = projectileBody != null ? projectileBody.GetType().Name : "연결 안 됨";
            Debug.Log($"[ProjectilePatternController] Body={bodyName}, Active={projectileBody?.ActiveCount ?? 0}, LastSpawned={lastSpawnedCount}, LastRendered={lastRenderedCount}", this);
        }



        /// <summary>
        /// runtime component 연결을 초기화하고 다시 구성합니다.
        /// </summary>
        public void ReconfigureRuntimeComponents()
        {
            runtimeConfigured = false;
            EnsureRuntimeComponents();
        }



        /// <summary>
        /// manager/emitter/runner/render/query component를 찾거나 생성하고 현재 설정을 적용합니다.
        /// </summary>
        public void EnsureRuntimeComponents()
        {
            ResolveProjectileBody();

            if (projectileEmitter == null)
            {
                projectileEmitter = GetComponent<ProjectileEmitter>();
                if (projectileEmitter == null)
                {
                    projectileEmitter = gameObject.AddComponent<ProjectileEmitter>();
                }
            }

            if (patternRunner == null)
            {
                patternRunner = GetComponent<ProjectilePatternRunner>();
                if (patternRunner == null)
                {
                    patternRunner = gameObject.AddComponent<ProjectilePatternRunner>();
                }
            }

            ProjectileRenderMode effectiveRenderMode = ResolveEffectiveRenderMode();
            if (effectiveRenderMode == ProjectileRenderMode.InstancedSprite && renderProxy == null)
            {
                renderProxy = GetComponent<ProjectileInstancedSpriteRenderProxy>();
                if (renderProxy == null)
                {
                    renderProxy = gameObject.AddComponent<ProjectileInstancedSpriteRenderProxy>();
                }
            }
            else if (effectiveRenderMode == ProjectileRenderMode.PooledSpriteRenderer && spriteRenderProxy == null)
            {
                spriteRenderProxy = GetComponent<ProjectileSpriteRenderProxy>();
                if (spriteRenderProxy == null)
                {
                    spriteRenderProxy = gameObject.AddComponent<ProjectileSpriteRenderProxy>();
                }
            }
            if (eventStreamRelay == null)
            {
                eventStreamRelay = GetComponent<ProjectileEventStreamRelay>();
                if (createEventStreamRelay && eventStreamRelay == null)
                {
                    eventStreamRelay = gameObject.AddComponent<ProjectileEventStreamRelay>();
                }
            }

            ManagedProjectileBody activeManager = projectileBody as ManagedProjectileBody;
            NativeProjectileBody activeNativeBody = projectileBody as NativeProjectileBody;
            DisableInactiveConcreteBackends(activeManager, activeNativeBody);

            if (runtimeConfigured)
            {
                return;
            }

            int configuredCapacity = ResolveConfiguredCapacity();
            InitializeProjectileBodyIfSupported(configuredCapacity);

            if (activeManager != null)
            {
                activeManager.SimulateOnUpdate = simulateOnUpdate;
                activeManager.SimulationBackend = simulationBackend;
                activeManager.MovementJobBatchSize = movementJobBatchSize;
            }
            else if (activeNativeBody != null)
            {
                activeNativeBody.SimulateOnUpdate = simulateOnUpdate;
                activeNativeBody.MovementJobBatchSize = movementJobBatchSize;
            }
            ApplyPhysicsHitPolicyToActiveBody();

            projectileEmitter.Initialize(projectileBody);
            patternRunner.Initialize(projectileEmitter, patternProfile);
            patternRunner.PlayOnEnable = false;

            ConfigureRenderProxy(configuredCapacity);

            if (renderProxy != null)
            {
                renderProxy.RenderOnLateUpdate = effectiveRenderMode == ProjectileRenderMode.InstancedSprite && renderOnLateUpdate;
            }

            if (spriteRenderProxy != null)
            {
                spriteRenderProxy.RenderOnLateUpdate = effectiveRenderMode == ProjectileRenderMode.PooledSpriteRenderer && renderOnLateUpdate;
            }

            if (eventStreamRelay != null)
            {
                eventStreamRelay.Initialize(projectileBody);
            }

            runtimeConfigured = true;
        }



        /// <summary>
        /// 현재 body backend에 receiver 없는 물리 충돌의 소비 정책을 적용합니다.
        /// </summary>
        private void ApplyPhysicsHitPolicyToActiveBody()
        {
            if (projectileBody is IProjectilePhysicsHitPolicyConfigurable configurableBody)
            {
                configurableBody.ConsumePhysicsHitWithoutReceiver = consumePhysicsHitWithoutReceiver;
            }
        }



        private void ApplyRenderScaleToActiveProxies()
        {
            if (renderProxy != null)
            {
                renderProxy.ScaleMode = renderScaleMode;
                renderProxy.FixedWorldScale = fixedRenderWorldScale;
            }

            if (spriteRenderProxy != null)
            {
                spriteRenderProxy.ScaleMode = renderScaleMode;
                spriteRenderProxy.FixedWorldScale = fixedRenderWorldScale;
            }
        }



        /// <summary>
        /// body가 선택 초기화 계약을 지원하면 controller가 구체 구현을 몰라도 저장소를 준비합니다.
        /// </summary>
        private void InitializeProjectileBodyIfSupported(int configuredCapacity)
        {
            if (!(projectileBody is IProjectileBodyInitializable initializableBody))
            {
                return;
            }

            var settings = new ProjectileBodyInitializationSettings(
                configuredCapacity,
                collisionBufferSize);
            initializableBody.InitializeProjectileBody(in settings);
        }



        /// <summary>
        /// pattern을 처음부터 재생합니다.
        /// </summary>
        public bool PlayPattern()
        {
            return PlayPattern((int[])null);
        }



        /// <summary>
        /// pattern을 처음부터 재생하고, 첫 step 진입 즉시 burst가 있으면 성공한 projectile id를 buffer에 채웁니다.
        /// </summary>
        public bool PlayPattern(int[] immediateProjectileIds)
        {
            EnsureRuntimeComponents();
            bool started = patternRunner.Restart(immediateProjectileIds);
            lastSpawnedCount = patternRunner.LastEnterSpawnedCount;
            return started;
        }



        /// <summary>
        /// pattern을 처음부터 재생하고, 첫 step 진입 즉시 burst가 있으면 성공한 projectile handle을 buffer에 채웁니다.
        /// handle은 body/id만 들고 있으므로 호출자는 concrete body를 꺼내지 않고 특정 탄을 조회하거나 제거할 수 있습니다.
        /// </summary>
        public bool PlayPattern(ProjectileHandle[] immediateProjectileHandles)
        {
            EnsureImmediateProjectileIdBuffer(immediateProjectileHandles);
            bool started = PlayPattern(immediateProjectileIdBuffer);
            FillImmediateProjectileHandles(immediateProjectileHandles);
            return started;
        }



        /// <summary>
        /// pattern 재생을 중지합니다.
        /// </summary>
        public void StopPattern()
        {
            EnsureRuntimeComponents();
            patternRunner.Stop();
        }



        /// <summary>
        /// 지정 step으로 이동하고 필요하면 즉시 재생합니다.
        /// </summary>
        public bool TryEnterStep(int stepIndex, bool play)
        {
            return TryEnterStep(stepIndex, play, (int[])null);
        }



        /// <summary>
        /// 지정 step으로 이동하고, step 진입 즉시 burst가 있으면 성공한 projectile id를 buffer에 채웁니다.
        /// </summary>
        public bool TryEnterStep(int stepIndex, bool play, int[] immediateProjectileIds)
        {
            EnsureRuntimeComponents();
            bool entered = patternRunner.TryEnterStep(stepIndex, play, immediateProjectileIds);
            lastSpawnedCount = patternRunner.LastEnterSpawnedCount;
            return entered;
        }



        /// <summary>
        /// 지정 step으로 이동하고, step 진입 즉시 burst가 있으면 성공한 projectile handle을 buffer에 채웁니다.
        /// </summary>
        public bool TryEnterStep(int stepIndex, bool play, ProjectileHandle[] immediateProjectileHandles)
        {
            EnsureImmediateProjectileIdBuffer(immediateProjectileHandles);
            bool entered = TryEnterStep(stepIndex, play, immediateProjectileIdBuffer);
            FillImmediateProjectileHandles(immediateProjectileHandles);
            return entered;
        }



        /// <summary>
        /// 다음 pattern step으로 이동합니다.
        /// </summary>
        public bool AdvanceToNextStep()
        {
            return AdvanceToNextStep((int[])null);
        }



        /// <summary>
        /// 다음 pattern step으로 이동하고, step 진입 즉시 burst가 있으면 성공한 projectile id를 buffer에 채웁니다.
        /// </summary>
        public bool AdvanceToNextStep(int[] immediateProjectileIds)
        {
            EnsureRuntimeComponents();
            bool advanced = patternRunner.AdvanceToNextStep(immediateProjectileIds);
            lastSpawnedCount = patternRunner.LastEnterSpawnedCount;
            return advanced;
        }



        /// <summary>
        /// 다음 pattern step으로 이동하고, step 진입 즉시 burst가 있으면 성공한 projectile handle을 buffer에 채웁니다.
        /// </summary>
        public bool AdvanceToNextStep(ProjectileHandle[] immediateProjectileHandles)
        {
            EnsureImmediateProjectileIdBuffer(immediateProjectileHandles);
            bool advanced = AdvanceToNextStep(immediateProjectileIdBuffer);
            FillImmediateProjectileHandles(immediateProjectileHandles);
            return advanced;
        }



        /// <summary>
        /// 현재 body의 모든 투사체를 제거합니다.
        /// </summary>
        public void ClearProjectiles()
        {
            EnsureRuntimeComponents();
            projectileEmitter.ClearProjectiles();
        }



        /// <summary>
        /// 현재 body에서 지정 id의 투사체를 제거합니다.
        /// controller를 사용하는 enemy/boss pattern 코드는 concrete body를 몰라도 같은 경로로 개별 탄을 정리할 수 있습니다.
        /// </summary>
        public bool DespawnProjectile(int projectileId)
        {
            EnsureRuntimeComponents();
            return projectileEmitter != null && projectileEmitter.DespawnProjectile(projectileId);
        }



        /// <summary>
        /// 현재 body에서 지정 handle의 투사체를 제거합니다.
        /// controller 사용자는 backend 종류나 id 추출을 신경 쓰지 않고 같은 controller 표면에서 개별 탄을 정리할 수 있습니다.
        /// </summary>
        public bool DespawnProjectile(ProjectileHandle projectileHandle)
        {
            EnsureRuntimeComponents();
            return projectileEmitter != null && projectileEmitter.DespawnProjectile(projectileHandle);
        }



        /// <summary>
        /// 활성 index 기준 snapshot을 가져옵니다. index 순서는 backend 내부 저장 방식에 따라 frame마다 달라질 수 있습니다.
        /// </summary>
        public bool TryGetProjectileSnapshot(int index, out ProjectileSnapshot snapshot)
        {
            EnsureRuntimeComponents();
            if (projectileEmitter == null)
            {
                snapshot = default;
                return false;
            }

            return projectileEmitter.TryGetProjectileSnapshot(index, out snapshot);
        }



        /// <summary>
        /// projectile id 기준 snapshot을 가져옵니다. `PlayPattern(..., immediateProjectileIds)`나 `TryEnterStep(..., immediateProjectileIds)`로 받은 id를 확인할 때 사용합니다.
        /// </summary>
        public bool TryGetProjectileSnapshotById(int projectileId, out ProjectileSnapshot snapshot)
        {
            EnsureRuntimeComponents();
            if (projectileEmitter == null)
            {
                snapshot = default;
                return false;
            }

            return projectileEmitter.TryGetProjectileSnapshotById(projectileId, out snapshot);
        }



        /// <summary>
        /// projectile handle 기준 snapshot을 가져옵니다.
        /// enemy/boss pattern 코드는 id를 다시 꺼내거나 concrete body를 만지지 않고 특정 탄 상태를 확인할 수 있습니다.
        /// </summary>
        public bool TryGetProjectileSnapshot(ProjectileHandle projectileHandle, out ProjectileSnapshot snapshot)
        {
            EnsureRuntimeComponents();
            if (projectileEmitter == null)
            {
                snapshot = default;
                return false;
            }

            return projectileEmitter.TryGetProjectileSnapshot(projectileHandle, out snapshot);
        }



        /// <summary>
        /// 현재 활성 투사체 snapshot을 buffer에 복사하고 복사한 수를 반환합니다.
        /// </summary>
        public int CopyProjectileSnapshots(ProjectileSnapshot[] buffer)
        {
            EnsureRuntimeComponents();
            return projectileEmitter != null ? projectileEmitter.CopyProjectileSnapshots(buffer) : 0;
        }



        /// <summary>
        /// pool 재사용 전에 pattern 진행, emitter 누적값, relay counter, 활성 투사체를 한 번에 초기화합니다.
        /// profile, backend, render/query 설정은 유지하므로 같은 controller prefab을 다시 활성화해도 구성은 바뀌지 않습니다.
        /// </summary>
        [ContextMenu("Reset Runtime State")]
        public void ResetRuntimeState()
        {
            ResetRuntimeStateCore(true);
        }



        private void ResetRuntimeStateCore(bool ensureRuntimeComponents)
        {
            if (ensureRuntimeComponents)
            {
                EnsureRuntimeComponents();
            }

            if (patternRunner != null)
            {
                patternRunner.Stop();
            }

            if (projectileEmitter != null)
            {
                projectileEmitter.ResetEmissionState();
                projectileEmitter.ClearProjectiles();
            }
            else if (projectileBody != null)
            {
                projectileBody.ClearAll();
            }
            else if (projectileManager != null)
            {
                projectileManager.ClearAll();
            }

            if (renderProxy != null)
            {
                renderProxy.ClearRenderData();
            }

            if (spriteRenderProxy != null)
            {
                spriteRenderProxy.ClearRenderedProjectiles();
            }

            if (eventStreamRelay != null)
            {
                eventStreamRelay.ResetCounters();
            }

            lastSpawnedCount = 0;
            lastRenderedCount = 0;
        }



        /// <summary>
        /// render proxy를 즉시 갱신하고 처리된 snapshot 수를 반환합니다.
        /// </summary>
        public int RenderNow()
        {
            EnsureRuntimeComponents();
            ProjectileRenderMode effectiveRenderMode = ResolveEffectiveRenderMode();
            if (effectiveRenderMode == ProjectileRenderMode.PooledSpriteRenderer)
            {
                lastRenderedCount = spriteRenderProxy != null ? spriteRenderProxy.RenderNow() : 0;
                return lastRenderedCount;
            }

            lastRenderedCount = effectiveRenderMode == ProjectileRenderMode.InstancedSprite && renderProxy != null
            ? renderProxy.RenderNow()
            : 0;
            return lastRenderedCount;
        }



        private void EnsureImmediateProjectileIdBuffer(ProjectileHandle[] immediateProjectileHandles)
        {
            int requiredLength = immediateProjectileHandles != null ? immediateProjectileHandles.Length : 1;
            requiredLength = Mathf.Max(1, requiredLength);
            if (immediateProjectileIdBuffer != null && immediateProjectileIdBuffer.Length >= requiredLength)
            {
                return;
            }

            int nextCapacity = immediateProjectileIdBuffer != null ? immediateProjectileIdBuffer.Length : 1;
            while (nextCapacity < requiredLength)
            {
                nextCapacity *= 2;
            }

            immediateProjectileIdBuffer = new int[nextCapacity];
        }



        private void FillImmediateProjectileHandles(ProjectileHandle[] immediateProjectileHandles)
        {
            ProjectileSpawnUtility.FillHandles(
            ProjectileBody,
            immediateProjectileIdBuffer,
            lastSpawnedCount,
            immediateProjectileIdBuffer != null ? immediateProjectileIdBuffer.Length : 0,
            immediateProjectileHandles);
        }




    }
}
