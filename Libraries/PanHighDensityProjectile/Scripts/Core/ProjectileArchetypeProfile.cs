using Pan.HighDensityElement;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Pan.HighDensityProjectile
{

    /// <summary>
    /// 투사체라면 대부분 가져야 하는 물리, 충돌, 피해, 표시, backend 선택값을 묶는 범용 authoring profile입니다.
    /// <para>이 프로필은 Addressables, PanEvent, ObjectManager, AllStar 타입을 알지 않으며, package 순수 request와 emitter/controller 값만 생성합니다.</para>
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectileArchetypeProfile", menuName = "Pan/HighDensityProjectile/Projectile Archetype Profile")]
    public sealed class ProjectileArchetypeProfile : ScriptableObject
    {
        [FoldoutGroup("기본 탄 속성"), SerializeField, MinValue(0f), LabelText("반지름")]
        private float radius = 0.05f;

        [FoldoutGroup("기본 탄 속성"), SerializeField, MinValue(0f), LabelText("초기 속도")]
        private float speed = 8f;

        [FoldoutGroup("기본 탄 속성"), SerializeField, MinValue(0f), LabelText("수명(초)")]
        private float lifetime = 3f;

        [FoldoutGroup("기본 탄 속성"), SerializeField, LabelText("2D 판정 사용")]
        private bool use2D = true;

        [FoldoutGroup("운동 보정"), SerializeField, LabelText("중력 사용")]
        private bool useGravity;

        [FoldoutGroup("운동 보정"), SerializeField, LabelText("중력 벡터"), ShowIf(nameof(useGravity))]
        private Vector3 gravity = Physics.gravity;

        [FoldoutGroup("운동 보정"), SerializeField, MinValue(0f), LabelText("선형 감쇠")]
        private float linearDamping;

        [FoldoutGroup("운동 보정"), SerializeField, MinValue(0f), LabelText("마찰 계수")]
        private float frictionCoefficient;

        [FoldoutGroup("피해/팀"), SerializeField, MinValue(0f), LabelText("피해량")]
        private float damage = 1f;

        [FoldoutGroup("피해/팀"), SerializeField, LabelText("팀 ID")]
        private int teamId;

        [FoldoutGroup("충돌"), SerializeField, LabelText("충돌 대상 Layer")]
        private LayerMask hitLayers;

        [FoldoutGroup("충돌"), SerializeField, LabelText("Receiver 없는 충돌도 소모")]
        private bool consumeHitWithoutReceiver;

        [FoldoutGroup("Collision"), SerializeField, LabelText("Trigger 포함")]
        private bool includeTriggers = true;

        [FoldoutGroup("충돌"), SerializeField, LabelText("Core/Hybrid 조회 대상")]
        [Tooltip("활성화하면 passive PhysicsCore shape가 생겨 HybridPhysicsWorld2D와 다른 Core query에 노출됩니다. 일반 대량 탄막은 비활성화하세요.")]
        private bool queryableByHybridPhysics2D;

        [FoldoutGroup("충돌"), SerializeField, LabelText("엄격 CCD")]
        [Tooltip("Auto는 빠른 이동일 때만 엄격 CCD를 켭니다. 일반 대량 탄막에는 Auto를 권장합니다.")]
        private StrictCcdOverride2D strictCcdOverride = StrictCcdOverride2D.Auto;

        [FoldoutGroup("충돌"), SerializeField, MinValue(0.01f), LabelText("엄격 CCD 기준 비율")]
        [Tooltip("한 고정 스텝의 이동량이 형상 최소 크기의 이 비율을 넘으면 Auto 엄격 CCD가 활성화됩니다.")]
        private float strictCcdThresholdRatio = 0.5f;

        [FoldoutGroup("표시"), SerializeField, LabelText("Sprite")]
        private Sprite sprite;

        [FoldoutGroup("표시"), SerializeField, MinValue(0), LabelText("Frame Index")]
        private int visualFrameIndex;

        [FoldoutGroup("표시"), SerializeField, MinValue(0f), LabelText("고정 월드 스케일")]
        private float fixedWorldScale;

        [FoldoutGroup("표시"), SerializeField, LabelText("Render Mode")]
        private ProjectileRenderMode renderMode = ProjectileRenderMode.InstancedSprite;

        [FoldoutGroup("표시"), SerializeField, LabelText("Render Scale Mode")]
        private ProjectileRenderScaleMode renderScaleMode = ProjectileRenderScaleMode.PhysicsDiameter;

        [FoldoutGroup("Backend"), SerializeField, LabelText("Body Backend")]
        private ProjectileBodyBackendMode bodyBackendMode = ProjectileBodyBackendMode.NativeProjectileBody;

        [FoldoutGroup("Backend"), SerializeField, MinValue(1), LabelText("초기 Capacity")]
        private int initialCapacity = 512;

        public float Radius { get => radius; set => radius = Mathf.Max(0f, value); }
        public float Speed { get => speed; set => speed = Mathf.Max(0f, value); }
        public float Lifetime { get => lifetime; set => lifetime = Mathf.Max(0f, value); }
        public bool Use2D { get => use2D; set => use2D = value; }
        public bool UseGravity { get => useGravity; set => useGravity = value; }
        public Vector3 Gravity { get => gravity; set => gravity = value; }
        public float LinearDamping { get => linearDamping; set => linearDamping = Mathf.Max(0f, value); }
        public float FrictionCoefficient { get => frictionCoefficient; set => frictionCoefficient = Mathf.Max(0f, value); }
        public float Damage { get => damage; set => damage = Mathf.Max(0f, value); }
        public int TeamId { get => teamId; set => teamId = value; }
        public LayerMask HitLayers { get => hitLayers; set => hitLayers = value; }
        public bool ConsumeHitWithoutReceiver { get => consumeHitWithoutReceiver; set => consumeHitWithoutReceiver = value; }
        public bool IncludeTriggers { get => includeTriggers; set => includeTriggers = value; }
        /// <summary>
        /// passive PhysicsCore query target shape를 생성할지 결정합니다.
        /// Hybrid 조회뿐 아니라 같은 PhysicsCore world의 query에도 노출됩니다.
        /// </summary>
        public bool QueryableByHybridPhysics2D { get => queryableByHybridPhysics2D; set => queryableByHybridPhysics2D = value; }
        public StrictCcdOverride2D StrictCcdOverride { get => strictCcdOverride; set => strictCcdOverride = value; }
        public float StrictCcdThresholdRatio { get => strictCcdThresholdRatio; set => strictCcdThresholdRatio = Mathf.Max(0.01f, value); }
        public Sprite Sprite { get => sprite; set => sprite = value; }
        public int VisualFrameIndex { get => visualFrameIndex; set => visualFrameIndex = Mathf.Max(0, value); }
        public float FixedWorldScale { get => fixedWorldScale; set => fixedWorldScale = Mathf.Max(0f, value); }
        public ProjectileRenderMode RenderMode { get => renderMode; set => renderMode = value; }
        public ProjectileRenderScaleMode RenderScaleMode { get => renderScaleMode; set => renderScaleMode = value; }
        public ProjectileBodyBackendMode BodyBackendMode { get => bodyBackendMode; set => bodyBackendMode = value; }
        public int InitialCapacity { get => initialCapacity; set => initialCapacity = Mathf.Max(1, value); }
        public ProjectileMotionSpec Motion => new(useGravity, gravity, linearDamping, frictionCoefficient);



        private void OnValidate()
        {
            radius = Mathf.Max(0f, radius);
            speed = Mathf.Max(0f, speed);
            lifetime = Mathf.Max(0f, lifetime);
            linearDamping = Mathf.Max(0f, linearDamping);
            frictionCoefficient = Mathf.Max(0f, frictionCoefficient);
            damage = Mathf.Max(0f, damage);
            visualFrameIndex = Mathf.Max(0, visualFrameIndex);
            fixedWorldScale = Mathf.Max(0f, fixedWorldScale);
            initialCapacity = Mathf.Max(1, initialCapacity);
            strictCcdThresholdRatio = Mathf.Max(0.01f, strictCcdThresholdRatio);
        }



        /// <summary>
        /// 지정된 위치와 방향으로 즉시 spawn 가능한 request를 생성합니다.
        /// </summary>
        public ProjectileSpawnRequest CreateSpawnRequest(Component source, Vector3 position, Vector3 direction)
        {
            Vector3 normalizedDirection = direction.sqrMagnitude > 0.000001f ? direction.normalized : Vector3.right;
            return new ProjectileSpawnRequest
            {
                Source = source,
                Kinematic = new KinematicCircle2DState(position, normalizedDirection * speed, radius, use2D),
                Lifetime = TimedLifetimeState.Start(lifetime),
                Motion = Motion,
                Collision = new CollisionLayerFilter(source != null ? source.gameObject.layer : 0, hitLayers, consumeHitWithoutReceiver, includeTriggers),
                Team = new TeamRelationTag(teamId),
                Hit = new HitDamageSpec(damage),
                Visual = new SpriteVisualSpec(sprite, visualFrameIndex, fixedWorldScale),
                QueryableByHybridPhysics2D = queryableByHybridPhysics2D,
                StrictCcdOverride = strictCcdOverride,
                StrictCcdThresholdRatio = strictCcdThresholdRatio,
            };
        }



        /// <summary>
        /// emitter가 다음 burst부터 이 archetype 값을 사용하도록 복사합니다. pattern의 발사 주기와 분포 설정은 건드리지 않습니다.
        /// </summary>
        public void ApplyTo(ProjectileEmitter emitter)
        {
            if (emitter == null)
            {
                return;
            }

            emitter.Speed = speed;
            emitter.Radius = radius;
            emitter.Lifetime = lifetime;
            emitter.UseGravity = useGravity;
            emitter.Gravity = gravity;
            emitter.LinearDamping = linearDamping;
            emitter.FrictionCoefficient = frictionCoefficient;
            emitter.Damage = damage;
            emitter.TeamId = teamId;
            emitter.VisualFrameIndex = visualFrameIndex;
            emitter.HitLayers = hitLayers;
            emitter.ConsumeHitWithoutReceiver = consumeHitWithoutReceiver;
            emitter.IncludeTriggers = includeTriggers;
            emitter.QueryableByHybridPhysics2D = queryableByHybridPhysics2D;
            emitter.StrictCcdOverride = strictCcdOverride;
            emitter.StrictCcdThresholdRatio = strictCcdThresholdRatio;
            emitter.Sprite = sprite;
            emitter.FixedWorldScale = fixedWorldScale;
            emitter.Use2D = use2D;
        }



        /// <summary>
        /// controller가 이 archetype의 backend, render, query 정책을 사용하도록 복사합니다.
        /// </summary>
        public void ApplyTo(ProjectilePatternController controller)
        {
            if (controller == null)
            {
                return;
            }

            controller.BodyBackendMode = bodyBackendMode;
            controller.RenderMode = renderMode;
            controller.RenderScaleMode = renderScaleMode;
            controller.InitialCapacity = initialCapacity;
        }
    }
}
