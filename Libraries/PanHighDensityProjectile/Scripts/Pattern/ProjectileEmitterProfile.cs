using Sirenix.OdinInspector;
using UnityEngine;

namespace Pan.HighDensityProjectile
{

    /// <summary>
    /// `ProjectileEmitter`에 적용할 burst 발사 설정 asset입니다.
    /// 패턴 phase나 enemy 타입별로 같은 emitter 로직에 다른 발사 데이터를 주입하기 위한 ScriptableObject입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectileEmitterProfile", menuName = "Pan/HighDensityProjectile/Projectile Emitter Profile")]
    public sealed class ProjectileEmitterProfile : ScriptableObject
    {
        [FoldoutGroup("자동 발사"), SerializeField, LabelText("Update 자동 발사"), Tooltip("On이면 profile 적용 후 emitter가 Update에서 자동 발사합니다.")]
        private bool emitOnUpdate;

        [FoldoutGroup("자동 발사"), SerializeField, LabelText("Unscaled Time 사용"), Tooltip("On이면 자동 발사 주기를 unscaled time 기준으로 계산합니다.")]
        private bool useUnscaledDeltaTime;

        [FoldoutGroup("자동 발사"), SerializeField, Min(0f), LabelText("초당 Burst"), Tooltip("초당 burst 수입니다.")]
        private float burstsPerSecond = 1f;

        [FoldoutGroup("자동 발사"), SerializeField, Min(1), LabelText("Tick당 최대 Burst"), Tooltip("프레임 드랍 후 한 Tick에서 몰아서 발사할 최대 burst 수입니다.")]
        private int maxBurstsPerTick = 8;

        [FoldoutGroup("분포"), SerializeField, LabelText("분포 방식"), Tooltip("FullCircle은 원형 분포, Arc는 base angle 중심 부채꼴 분포입니다.")]
        private ProjectileEmitterSpreadMode spreadMode = ProjectileEmitterSpreadMode.FullCircle;

        [FoldoutGroup("분포"), SerializeField, Min(1), LabelText("Burst당 탄 수"), Tooltip("burst 한 번에 생성할 탄 수입니다.")]
        private int projectilesPerBurst = 16;

        [FoldoutGroup("분포"), SerializeField, LabelText("기본 각도"), Tooltip("기본 발사 각도입니다.")]
        private float baseAngleDegrees;

        [FoldoutGroup("분포"), SerializeField, LabelText("분포 각도"), Tooltip("분포 각도입니다.")]
        private float spreadAngleDegrees = 360f;

        [FoldoutGroup("분포"), SerializeField, LabelText("Transform 회전 반영"), Tooltip("On이면 emitter transform Z 회전을 base angle에 더합니다.")]
        private bool useTransformRotation = true;

        [SerializeField, Min(0f), Tooltip("생성되는 투사체의 초기 속력입니다.")]
        private float speed = 8f;

        [SerializeField, Min(0f), Tooltip("투사체 원형 판정 반경입니다.")]
        private float radius = 0.05f;

        [SerializeField, Min(0f), Tooltip("투사체 수명입니다.")]
        private float lifetime = 3f;

        [SerializeField, Tooltip("On이면 투사체 속도에 Gravity 벡터를 매 tick 누적합니다.")]
        private bool useGravity;

        [SerializeField, Tooltip("초당 속도 변화량으로 적용할 중력 벡터입니다.")]
        private Vector3 gravity = Physics.gravity;

        [SerializeField, Min(0f), Tooltip("투사체 속도를 점진적으로 줄이는 선형 감쇠 계수입니다.")]
        private float linearDamping;

        [SerializeField, Min(0f), Tooltip("마찰처럼 추가로 속도를 줄이는 계수입니다.")]
        private float frictionCoefficient;

        [SerializeField, Tooltip("hit payload에 들어갈 기본 damage 값입니다.")]
        private float damage = 1f;

        [SerializeField, Tooltip("투사체 team id입니다.")]
        private int teamId;

        [SerializeField, Min(0), Tooltip("snapshot에 실어 보낼 sprite/frame index입니다. 0이면 renderer 기본 sprite를 사용합니다.")]
        private int visualFrameIndex;

        [SerializeField, Tooltip("Unity Physics fallback 경로에서 사용할 hit layer mask입니다.")]
        private LayerMask hitLayers;

        [SerializeField, Tooltip("On이면 2D hit/query 경로를 사용합니다.")]
        private bool use2D = true;



        /// <summary>profile 적용 후 emitter가 Update에서 자동 발사할지 결정합니다.</summary>
        public bool EmitOnUpdate { get => emitOnUpdate; set => emitOnUpdate = value; }

        /// <summary>자동 발사 주기 계산에 unscaled time을 사용할지 결정합니다.</summary>
        public bool UseUnscaledDeltaTime { get => useUnscaledDeltaTime; set => useUnscaledDeltaTime = value; }

        /// <summary>초당 burst 수입니다.</summary>
        public float BurstsPerSecond { get => burstsPerSecond; set => burstsPerSecond = Mathf.Max(0f, value); }

        /// <summary>한 Tick에서 처리할 최대 burst 수입니다.</summary>
        public int MaxBurstsPerTick { get => maxBurstsPerTick; set => maxBurstsPerTick = Mathf.Max(1, value); }

        /// <summary>burst 내 탄 분포 방식입니다.</summary>
        public ProjectileEmitterSpreadMode SpreadMode { get => spreadMode; set => spreadMode = value; }

        /// <summary>burst 한 번에 생성할 탄 수입니다.</summary>
        public int ProjectilesPerBurst { get => projectilesPerBurst; set => projectilesPerBurst = Mathf.Max(1, value); }

        /// <summary>기본 발사 각도입니다.</summary>
        public float BaseAngleDegrees { get => baseAngleDegrees; set => baseAngleDegrees = value; }

        /// <summary>분포 각도입니다.</summary>
        public float SpreadAngleDegrees { get => spreadAngleDegrees; set => spreadAngleDegrees = value; }

        /// <summary>emitter transform Z 회전을 발사 각도에 반영할지 결정합니다.</summary>
        public bool UseTransformRotation { get => useTransformRotation; set => useTransformRotation = value; }

        /// <summary>생성되는 투사체의 초기 속력입니다.</summary>
        public float Speed { get => speed; set => speed = Mathf.Max(0f, value); }

        /// <summary>생성되는 투사체의 원형 판정 반경입니다.</summary>
        public float Radius { get => radius; set => radius = Mathf.Max(0f, value); }

        /// <summary>생성되는 투사체의 수명입니다.</summary>
        public float Lifetime { get => lifetime; set => lifetime = Mathf.Max(0f, value); }

        /// <summary>투사체 속도에 중력을 적용할지 여부입니다.</summary>
        public bool UseGravity { get => useGravity; set => useGravity = value; }

        /// <summary>초당 속도 변화량으로 적용할 중력 벡터입니다.</summary>
        public Vector3 Gravity { get => gravity; set => gravity = value; }

        /// <summary>투사체 속도를 점진적으로 줄이는 선형 감쇠 계수입니다.</summary>
        public float LinearDamping { get => linearDamping; set => linearDamping = Mathf.Max(0f, value); }

        /// <summary>마찰처럼 추가로 속도를 줄이는 계수입니다.</summary>
        public float FrictionCoefficient { get => frictionCoefficient; set => frictionCoefficient = Mathf.Max(0f, value); }

        /// <summary>현재 profile 값을 spawn request에 넣을 운동 규칙으로 변환합니다.</summary>
        public ProjectileMotionSpec Motion => new(useGravity, gravity, linearDamping, frictionCoefficient);

        /// <summary>hit payload에 들어갈 기본 damage 값입니다.</summary>
        public float Damage { get => damage; set => damage = Mathf.Max(0f, value); }

        /// <summary>생성되는 투사체의 team id입니다.</summary>
        public int TeamId { get => teamId; set => teamId = value; }

        /// <summary>snapshot에 실어 보낼 sprite/frame index입니다. simulation과 hit 판정에는 관여하지 않습니다.</summary>
        public int VisualFrameIndex { get => visualFrameIndex; set => visualFrameIndex = Mathf.Max(0, value); }

        /// <summary>Unity Physics fallback 경로에서 사용할 hit layer mask입니다.</summary>
        public LayerMask HitLayers { get => hitLayers; set => hitLayers = value; }

        /// <summary>2D hit/query 경로를 사용할지 결정합니다.</summary>
        public bool Use2D { get => use2D; set => use2D = value; }



        private void OnValidate()
        {
            burstsPerSecond = Mathf.Max(0f, burstsPerSecond);
            maxBurstsPerTick = Mathf.Max(1, maxBurstsPerTick);
            projectilesPerBurst = Mathf.Max(1, projectilesPerBurst);
            speed = Mathf.Max(0f, speed);
            radius = Mathf.Max(0f, radius);
            lifetime = Mathf.Max(0f, lifetime);
            linearDamping = Mathf.Max(0f, linearDamping);
            frictionCoefficient = Mathf.Max(0f, frictionCoefficient);
            damage = Mathf.Max(0f, damage);
            visualFrameIndex = Mathf.Max(0, visualFrameIndex);
        }



        /// <summary>
        /// profile 값을 지정 emitter에 복사합니다. emitter의 burst 누적 상태는 별도 정책으로 유지됩니다.
        /// </summary>
        public void ApplyTo(ProjectileEmitter emitter)
        {
            if (emitter == null)
            {
                return;
            }

            emitter.EmitOnUpdate = emitOnUpdate;
            emitter.UseUnscaledDeltaTime = useUnscaledDeltaTime;
            emitter.BurstsPerSecond = burstsPerSecond;
            emitter.MaxBurstsPerTick = maxBurstsPerTick;
            emitter.SpreadMode = spreadMode;
            emitter.ProjectilesPerBurst = projectilesPerBurst;
            emitter.BaseAngleDegrees = baseAngleDegrees;
            emitter.SpreadAngleDegrees = spreadAngleDegrees;
            emitter.UseTransformRotation = useTransformRotation;
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
            emitter.Use2D = use2D;
        }
    }
}
