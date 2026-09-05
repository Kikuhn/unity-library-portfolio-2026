using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Pan.HighDensityProjectile
{

    /// <summary>
    /// 고밀도탄, 보스 패턴, 검증용 burst처럼 "여러 탄을 하나의 body backend에 모아" 실행하는 상위 컨트롤러입니다.
    /// consumer 프로젝트가 소유한 일반탄 조립 경로는 이 컨트롤러를 거치지 않습니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class ProjectilePatternController : MonoBehaviour
    {
        [FoldoutGroup("Backend"), SerializeField, Min(1), LabelText("초기 Capacity"), Tooltip("자동 생성되는 projectile body backend의 초기 capacity입니다.")]
        private int initialCapacity = 512;

        [FoldoutGroup("Backend"), SerializeField, Min(1), LabelText("Hit Buffer 크기"), Tooltip("Unity Physics fallback 결과를 받을 hit buffer 크기입니다.")]
        private int collisionBufferSize = 16;


        [FoldoutGroup("Backend"), SerializeField, LabelText("Update Simulation"), Tooltip("자동 생성/연결된 projectile body backend가 Update에서 simulation을 돌릴지 결정합니다.")]
        private bool simulateOnUpdate = true;

        [FoldoutGroup("렌더링"), SerializeField, LabelText("LateUpdate 자동 렌더"), Tooltip("Instanced render proxy가 LateUpdate에서 자동 렌더 데이터를 만들지 결정합니다.")]
        private bool renderOnLateUpdate = true;

        [FoldoutGroup("렌더링"), SerializeField, LabelText("Render Proxy 자동 생성"), Tooltip("비어 있으면 ProjectileInstancedSpriteRenderProxy를 자동 생성합니다.")]
        private bool createRenderProxy = true;

        [FoldoutGroup("렌더링"), SerializeField, LabelText("Render Mode"), Tooltip("자동 생성할 projectile renderer입니다. None이면 body simulation만 구성합니다.")]
        private ProjectileRenderMode renderMode = ProjectileRenderMode.InstancedSprite;

        [SerializeField, Tooltip("렌더 크기를 물리 반지름 기반으로 둘지, 고정 월드 scale로 둘지 결정합니다.")]
        private ProjectileRenderScaleMode renderScaleMode = ProjectileRenderScaleMode.PhysicsDiameter;

        [SerializeField, Min(0f), Tooltip("RenderScaleMode가 FixedWorldScale일 때 사용할 월드 scale입니다.")]
        private float fixedRenderWorldScale = 1f;


        [SerializeField, Tooltip("On이면 spawn/contact/despawn 이벤트를 receiver 인터페이스로 전달하는 ProjectileEventStreamRelay를 자동 생성하고 연결합니다.")]
        private bool createEventStreamRelay;







        [SerializeField, Tooltip("Start 시점에 pattern을 자동 재생합니다.")]
        private bool playOnStart;

        [SerializeField, Tooltip("On이면 GameObject나 component가 비활성화될 때 pattern 진행, emitter 누적값, relay counter, 활성 투사체를 정리합니다. 풀링 spawner는 true가 안전합니다.")]
        private bool resetRuntimeStateOnDisable = true;

        [SerializeField, Tooltip("실행할 pattern profile입니다.")]
        private ProjectilePatternProfile patternProfile;

        [SerializeField, Tooltip("managed/native body에서 사용할 simulation backend 정책입니다.")]
        private ProjectileSimulationBackend simulationBackend = ProjectileSimulationBackend.JobsMovementOnlyWhenNoCollision;

        [SerializeField, Tooltip("ProjectileBodySource가 비어 있을 때 자동 생성/연결할 body backend입니다. 고밀도탄 기본값은 NativeProjectileBody입니다.")]
        private ProjectileBodyBackendMode bodyBackendMode = ProjectileBodyBackendMode.NativeProjectileBody;

        [SerializeField, Min(1), Tooltip("JobsMovementOnlyWhenNoCollision backend에서 사용할 batch size입니다.")]
        private int movementJobBatchSize = 64;


        [SerializeField, Tooltip("On이면 receiver가 없는 Unity Physics collider hit도 소비된 hit로 처리합니다. 벽/바닥에 닿으면 사라지는 플레이어 탄막에 사용합니다.")]
        private bool consumePhysicsHitWithoutReceiver;

        [FoldoutGroup("연결"), SerializeField, LabelText("Body Source"), Tooltip("ManagedProjectileBody가 아닌 IPanProjectileBody 구현체를 직접 주입할 때 사용합니다.")]
        private MonoBehaviour projectileBodySource;

        [FoldoutGroup("연결"), SerializeField, LabelText("검증용 Managed Body"), Tooltip("검증/비교용 managed backend 참조입니다. 실전 고밀도탄은 보통 NativeProjectileBody를 사용합니다.")]
        private ManagedProjectileBody projectileManager;

        [SerializeField, Tooltip("ProjectileEmitter 참조입니다. 없으면 자동 생성됩니다.")]
        private ProjectileEmitter projectileEmitter;

        [SerializeField, Tooltip("ProjectilePatternRunner 참조입니다. 없으면 자동 생성됩니다.")]
        private ProjectilePatternRunner patternRunner;

        [SerializeField, Tooltip("대량 탄막용 instanced render proxy입니다. createRenderProxy가 켜져 있으면 없을 때 자동 생성됩니다.")]
        private ProjectileInstancedSpriteRenderProxy renderProxy;

        [SerializeField, Tooltip("일반 물리 탄막과 디버깅용 pooled SpriteRenderer proxy입니다.")]
        private ProjectileSpriteRenderProxy spriteRenderProxy;



        [SerializeField, Tooltip("같은 GameObject에 이미 붙어 있는 투사체 event stream relay입니다. 있으면 선택된 body backend에 맞춰 재연결합니다.")]
        private ProjectileEventStreamRelay eventStreamRelay;



        private bool runtimeConfigured;
        private int lastSpawnedCount;
        private int lastRenderedCount;
        private IPanProjectileBody projectileBody;
        private MonoBehaviour resolvedProjectileBodySource;
        private readonly List<MonoBehaviour> bodyLookupBuffer = new List<MonoBehaviour>(4);
        private int[] immediateProjectileIdBuffer;



        /// <summary>현재 사용 중인 투사체 body 계약입니다.</summary>
        public IPanProjectileBody ProjectileBody
        {
            get
            {
                ResolveProjectileBody();
                return projectileBody;
            }
        }

        /// <summary>현재 연결된 emitter입니다.</summary>
        public ProjectileEmitter ProjectileEmitter => projectileEmitter;

        /// <summary>현재 연결된 pattern runner입니다.</summary>
        public ProjectilePatternRunner PatternRunner => patternRunner;

        /// <summary>현재 연결된 instanced render proxy입니다.</summary>
        public ProjectileInstancedSpriteRenderProxy RenderProxy => renderProxy;

        /// <summary>현재 연결된 pooled SpriteRenderer proxy입니다.</summary>
        public ProjectileSpriteRenderProxy SpriteRenderProxy => spriteRenderProxy;


        /// <summary>현재 연결된 spawn/contact/despawn event stream relay입니다.</summary>
        public ProjectileEventStreamRelay EventStreamRelay => eventStreamRelay;

        /// <summary>외부 `IPanProjectileBody` 구현체를 제공하는 MonoBehaviour source입니다.</summary>
        public MonoBehaviour ProjectileBodySource
        {
            get => projectileBodySource != null ? projectileBodySource : resolvedProjectileBodySource;
            set
            {
                projectileBodySource = value;
                resolvedProjectileBodySource = null;
                projectileBody = null;
                projectileManager = value as ManagedProjectileBody;
                runtimeConfigured = false;
            }
        }

        /// <summary>pattern runner가 현재 재생 중인지 나타냅니다.</summary>
        public bool IsPlaying => patternRunner != null && patternRunner.IsPlaying;

        /// <summary>pattern runner가 완료 상태인지 나타냅니다.</summary>
        public bool IsCompleted => patternRunner != null && patternRunner.IsCompleted;

        /// <summary>현재 pattern step index입니다.</summary>
        public int CurrentStepIndex => patternRunner != null ? patternRunner.CurrentStepIndex : -1;

        /// <summary>현재 loop count입니다.</summary>
        public int LoopCount => patternRunner != null ? patternRunner.LoopCount : 0;

        /// <summary>마지막 step 진입 또는 play에서 생성된 투사체 수입니다.</summary>
        public int LastSpawnedCount => lastSpawnedCount;

        /// <summary>마지막 수동 render에서 처리한 snapshot 수입니다.</summary>
        public int LastRenderedCount => lastRenderedCount;

        /// <summary>Start 시점에 pattern을 자동 재생할지 결정합니다.</summary>
        public bool PlayOnStart { get => playOnStart; set => playOnStart = value; }

        /// <summary>
        /// 비활성화될 때 runtime state를 정리할지 결정합니다.
        /// pooled spawner는 true가 안전하고, 비활성화 중에도 active projectile data를 보존해야 하는 특수 연출은 false로 둘 수 있습니다.
        /// </summary>
        public bool ResetRuntimeStateOnDisable { get => resetRuntimeStateOnDisable; set => resetRuntimeStateOnDisable = value; }

        /// <summary>render proxy를 자동 생성할지 결정합니다.</summary>
        public bool CreateRenderProxy
        {
            get => createRenderProxy && renderMode != ProjectileRenderMode.None;
            set
            {
                bool nextCreate = value;
                ProjectileRenderMode nextRenderMode = nextCreate && renderMode == ProjectileRenderMode.None
                ? ProjectileRenderMode.InstancedSprite
                : nextCreate
                ? renderMode
                : ProjectileRenderMode.None;
                if (createRenderProxy == nextCreate && renderMode == nextRenderMode)
                {
                    return;
                }

                createRenderProxy = nextCreate;
                renderMode = nextRenderMode;
                runtimeConfigured = false;
            }
        }

        /// <summary>자동 생성/연결할 renderer 종류입니다. spawn/pattern 사용법은 render mode와 무관하게 유지됩니다.</summary>
        public ProjectileRenderMode RenderMode
        {
            get => renderMode;
            set
            {
                if (renderMode == value)
                {
                    return;
                }

                renderMode = value;
                createRenderProxy = value != ProjectileRenderMode.None;
                runtimeConfigured = false;
            }
        }

        public ProjectileRenderScaleMode RenderScaleMode
        {
            get => renderScaleMode;
            set
            {
                if (renderScaleMode == value)
                {
                    return;
                }

                renderScaleMode = value;
                ApplyRenderScaleToActiveProxies();
            }
        }

        public float FixedRenderWorldScale
        {
            get => fixedRenderWorldScale;
            set
            {
                float nextValue = Mathf.Max(0f, value);
                if (Mathf.Approximately(fixedRenderWorldScale, nextValue))
                {
                    return;
                }

                fixedRenderWorldScale = nextValue;
                ApplyRenderScaleToActiveProxies();
            }
        }

        /// <summary>
        /// spawn/contact/despawn event relay를 자동 생성할지 결정합니다.
        /// 기본값은 false라 기존 prefab의 component 구성을 늘리지 않고, 필요한 spawner만 opt-in합니다.
        /// </summary>
        public bool CreateEventStreamRelay
        {
            get => createEventStreamRelay;
            set
            {
                if (createEventStreamRelay == value)
                {
                    return;
                }

                createEventStreamRelay = value;
                runtimeConfigured = false;
            }
        }



        /// <summary>manager backend가 Update에서 자동 simulation을 돌릴지 결정합니다.</summary>
        public bool SimulateOnUpdate
        {
            get => simulateOnUpdate;
            set
            {
                if (simulateOnUpdate == value)
                {
                    return;
                }

                simulateOnUpdate = value;
                runtimeConfigured = false;
            }
        }

        /// <summary>render proxy가 LateUpdate에서 자동 render data를 만들지 결정합니다.</summary>
        public bool RenderOnLateUpdate
        {
            get => renderOnLateUpdate;
            set
            {
                if (renderOnLateUpdate == value)
                {
                    return;
                }

                renderOnLateUpdate = value;
                runtimeConfigured = false;
            }
        }

        /// <summary>자동 생성 manager의 초기 capacity입니다.</summary>
        public int InitialCapacity
        {
            get => initialCapacity;
            set
            {
                int nextValue = Mathf.Max(1, value);
                if (initialCapacity == nextValue)
                {
                    return;
                }

                initialCapacity = nextValue;
                runtimeConfigured = false;
            }
        }

        /// <summary>Unity Physics fallback hit buffer 크기입니다.</summary>
        public int CollisionBufferSize
        {
            get => collisionBufferSize;
            set
            {
                int nextValue = Mathf.Max(1, value);
                if (collisionBufferSize == nextValue)
                {
                    return;
                }

                collisionBufferSize = nextValue;
                runtimeConfigured = false;
            }
        }


        /// <summary>manager backend의 simulation 정책입니다.</summary>
        public ProjectileSimulationBackend SimulationBackend
        {
            get => simulationBackend;
            set
            {
                if (simulationBackend == value)
                {
                    return;
                }

                simulationBackend = value;
                runtimeConfigured = false;
            }
        }

        /// <summary>외부 source가 없을 때 자동 구성할 body backend 종류입니다.</summary>
        public ProjectileBodyBackendMode BodyBackendMode
        {
            get => bodyBackendMode;
            set
            {
                if (bodyBackendMode == value && projectileBody != null)
                {
                    return;
                }

                bodyBackendMode = value;
                if (projectileBodySource == null)
                {
                    resolvedProjectileBodySource = null;
                    projectileBody = null;
                    projectileManager = null;
                }

                runtimeConfigured = false;
            }
        }

        /// <summary>movement-only job backend의 batch size입니다.</summary>
        public int MovementJobBatchSize
        {
            get => movementJobBatchSize;
            set
            {
                int nextValue = Mathf.Max(1, value);
                if (movementJobBatchSize == nextValue)
                {
                    return;
                }

                movementJobBatchSize = nextValue;
                runtimeConfigured = false;
            }
        }

        /// <summary>실행할 pattern profile입니다. 변경하면 runtime component 재구성이 필요해집니다.</summary>
        public bool ConsumePhysicsHitWithoutReceiver
        {
            get => consumePhysicsHitWithoutReceiver;
            set
            {
                if (consumePhysicsHitWithoutReceiver == value)
                {
                    return;
                }

                consumePhysicsHitWithoutReceiver = value;
                ApplyPhysicsHitPolicyToActiveBody();
            }
        }

        public ProjectilePatternProfile PatternProfile
        {
            get => patternProfile;
            set
            {
                if (patternProfile == value)
                {
                    return;
                }

                patternProfile = value;
                runtimeConfigured = false;
            }
        }
    }
}
