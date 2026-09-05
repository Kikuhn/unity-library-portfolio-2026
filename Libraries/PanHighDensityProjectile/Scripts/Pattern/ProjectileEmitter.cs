using System.Collections.Generic;
using Pan.HighDensityElement;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Pan.HighDensityProjectile
{

    /// <summary>
    /// 한 burst에서 탄을 어떤 각도 분포로 뿌릴지 결정합니다.
    /// </summary>
    public enum ProjectileEmitterSpreadMode
    {
        /// <summary>360도 또는 지정 spread를 균등 분할합니다.</summary>
        FullCircle,

        /// <summary>base angle을 중심으로 지정 spread 범위 안에 균등 배치합니다.</summary>
        Arc,
    }



    /// <summary>
    /// `IPanProjectileBody`에 burst 단위 투사체 생성을 요청하는 런타임 emitter입니다.
    /// 탄 하나마다 GameObject를 만들지 않고, 패턴/프로필 설정을 spawn data로 변환하는 역할만 담당합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class ProjectileEmitter : MonoBehaviour
    {
        [FoldoutGroup("연결"), SerializeField, LabelText("검증용 Managed Body"), Tooltip("비워두면 같은 GameObject 또는 부모의 ManagedProjectileBody/IPanProjectileBody를 자동으로 찾습니다.")]
        private ManagedProjectileBody projectileManager;
        [FoldoutGroup("연결"), SerializeField, LabelText("Body Source"), Tooltip("ManagedProjectileBody가 아닌 IPanProjectileBody 구현체를 직접 연결할 때 사용합니다.")]
        private MonoBehaviour projectileBodySource;
        [FoldoutGroup("연결"), SerializeField, LabelText("발사 Source Override"), Tooltip("비어 있지 않으면 spawn payload의 Source로 사용합니다. 플레이어 탄막이 자기 collider를 맞지 않게 하는 데 사용합니다.")]
        private Component projectileSourceOverride;

        [FoldoutGroup("프로필"), SerializeField, LabelText("Emitter Profile"), Tooltip("있으면 Awake 또는 ApplyProfile 호출 시 emitter 설정에 복사됩니다.")]
        private ProjectileEmitterProfile profile;

        [FoldoutGroup("자동 발사"), SerializeField, LabelText("Update 자동 발사"), Tooltip("On이면 Update에서 자동으로 Tick을 호출합니다. 패턴 컨트롤러가 직접 제어하면 Off로 둡니다.")]
        private bool emitOnUpdate;

        [FoldoutGroup("자동 발사"), SerializeField, LabelText("Unscaled Time 사용"), Tooltip("On이면 Time.unscaledDeltaTime으로 발사 주기를 계산합니다.")]
        private bool useUnscaledDeltaTime;

        [FoldoutGroup("자동 발사"), SerializeField, Min(0f), LabelText("초당 Burst"), Tooltip("초당 burst 수입니다. 0이면 자동 Tick에서 발사하지 않습니다.")]
        private float burstsPerSecond = 1f;

        [FoldoutGroup("자동 발사"), SerializeField, Min(1), LabelText("Tick당 최대 Burst"), Tooltip("프레임 드랍 후 한 Tick에서 몰아서 발사할 최대 burst 수입니다.")]
        private int maxBurstsPerTick = 8;

        [FoldoutGroup("분포"), SerializeField, LabelText("분포 방식"), Tooltip("FullCircle은 원형 분포, Arc는 base angle 중심 부채꼴 분포입니다.")]
        private ProjectileEmitterSpreadMode spreadMode = ProjectileEmitterSpreadMode.FullCircle;

        [FoldoutGroup("분포"), SerializeField, Min(1), LabelText("Burst당 탄 수"), Tooltip("burst 한 번에 생성할 탄 수입니다.")]
        private int projectilesPerBurst = 16;

        [SerializeField, Tooltip("기본 발사 각도입니다. Use Transform Rotation이 켜져 있으면 transform Z 회전이 더해집니다.")]
        private float baseAngleDegrees;

        [SerializeField, Tooltip("분포 각도입니다. FullCircle 기본값은 360도입니다.")]
        private float spreadAngleDegrees = 360f;

        [SerializeField, Tooltip("On이면 transform.eulerAngles.z를 base angle에 더합니다.")]
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

        [SerializeField, Tooltip("아군/적군 구분에 사용하는 team id입니다.")]
        private int teamId;

        [SerializeField, Min(0), Tooltip("snapshot에 실어 보낼 sprite/frame index입니다. 0이면 renderer 기본 sprite를 사용합니다.")]
        private int visualFrameIndex;

        [SerializeField, Tooltip("충돌 대상으로 사용할 Unity layer mask입니다.")]
        private LayerMask hitLayers;

        [SerializeField, Tooltip("Receiver가 없는 Unity Physics hit를 consume으로 처리할지 결정합니다.")]
        private bool consumeHitWithoutReceiver;

        [SerializeField, Tooltip("Trigger Collider를 충돌 후보에 포함할지 결정합니다.")]
        private bool includeTriggers = true;

        [SerializeField, Tooltip("활성화하면 PhysicsCore/Hybrid query가 이 투사체를 target으로 찾을 수 있습니다. 일반 대량 탄막은 끄세요.")]
        private bool queryableByHybridPhysics2D;

        [SerializeField, Tooltip("Bodyless PhysicsCore2D 투사체의 엄격 CCD 정책입니다.")]
        private StrictCcdOverride2D strictCcdOverride = StrictCcdOverride2D.Auto;

        [SerializeField, Min(0.01f), Tooltip("Auto 엄격 CCD를 활성화할 이동량/형상 크기 비율입니다.")]
        private float strictCcdThresholdRatio = 0.5f;

        [SerializeField, Tooltip("spawn snapshot에 복사할 sprite입니다.")]
        private Sprite sprite;

        [SerializeField, Min(0f), Tooltip("spawn snapshot에 복사할 고정 월드 스케일입니다.")]
        private float fixedWorldScale;

        [SerializeField, Tooltip("On이면 2D hit/query 경로를 사용합니다.")]
        private bool use2D = true;

        [SerializeField, Tooltip("On이면 비활성화될 때 burst 누적값과 발사 통계를 초기화합니다. pooled emitter는 true가 안전합니다.")]
        private bool resetEmissionStateOnDisable = true;

        [SerializeField, Tooltip("On이면 비활성화될 때 연결된 projectile body의 활성 투사체도 정리합니다. 여러 emitter가 같은 body를 공유하면 false로 둡니다.")]
        private bool clearProjectilesOnDisable;



        private float burstAccumulator;
        private int totalBurstsEmitted;
        private int totalProjectilesEmitted;
        private IPanProjectileBody projectileBody;
        private MonoBehaviour resolvedProjectileBodySource;
        private readonly List<MonoBehaviour> bodyLookupBuffer = new List<MonoBehaviour>(4);
        private ProjectileSpawnRequest[] spawnDataBuffer;
        private int[] projectileIdBuffer;
        private Vector3[] directionCache;
        private int directionCacheCount = -1;
        private ProjectileEmitterSpreadMode directionCacheSpreadMode;
        private float directionCacheSpreadAngleDegrees;



        /// <summary>Inspector에서 `IPanProjectileBody` 구현 MonoBehaviour를 직접 주입할 수 있는 source입니다.</summary>
        public MonoBehaviour ProjectileBodySource
        {
            get => projectileBodySource != null
            ? projectileBodySource
            : projectileManager != null
            ? projectileManager
            : resolvedProjectileBodySource;
            set
            {
                projectileBodySource = value;
                resolvedProjectileBodySource = null;
                projectileBody = value as IPanProjectileBody;
                projectileManager = value as ManagedProjectileBody;
            }
        }

        /// <summary>`ManagedProjectileBody`, native body, ECS body 등을 모두 받을 수 있는 느슨한 body 계약입니다.</summary>
        public IPanProjectileBody ProjectileBody
        {
            get
            {
                EnsureProjectileBody();
                return projectileBody;
            }
            set
            {
                projectileBody = value;
                projectileManager = value as ManagedProjectileBody;
                projectileBodySource = value as MonoBehaviour;
                resolvedProjectileBodySource = null;
            }
        }

        /// <summary>발사체 hit payload와 source self-hit 제외에 사용할 gameplay source입니다.</summary>
        public Component ProjectileSourceOverride { get => projectileSourceOverride; set => projectileSourceOverride = value; }

        /// <summary>이 emitter에 적용할 ScriptableObject profile입니다.</summary>
        public ProjectileEmitterProfile Profile { get => profile; set => profile = value; }

        /// <summary>Update에서 자동 발사 Tick을 돌릴지 결정합니다.</summary>
        public bool EmitOnUpdate { get => emitOnUpdate; set => emitOnUpdate = value; }

        /// <summary>자동 Tick에서 unscaled time을 사용할지 결정합니다.</summary>
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

        /// <summary>transform Z 회전을 발사 각도에 반영할지 결정합니다.</summary>
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

        /// <summary>현재 emitter 값을 spawn request에 넣을 운동 규칙으로 변환합니다.</summary>
        public ProjectileMotionSpec Motion => new(useGravity, gravity, linearDamping, frictionCoefficient);

        /// <summary>hit payload에 들어갈 기본 damage 값입니다.</summary>
        public float Damage { get => damage; set => damage = Mathf.Max(0f, value); }

        /// <summary>생성되는 투사체의 team id입니다.</summary>
        public int TeamId { get => teamId; set => teamId = value; }

        /// <summary>snapshot에 실어 보낼 sprite/frame index입니다. simulation과 hit 판정에는 관여하지 않습니다.</summary>
        public int VisualFrameIndex { get => visualFrameIndex; set => visualFrameIndex = Mathf.Max(0, value); }

        /// <summary>Unity Physics fallback 경로에서 사용할 hit layer mask입니다.</summary>
        public LayerMask HitLayers { get => hitLayers; set => hitLayers = value; }

        /// <summary>receiver가 없는 Unity Physics hit를 consume으로 처리할지 결정합니다.</summary>
        public bool ConsumeHitWithoutReceiver { get => consumeHitWithoutReceiver; set => consumeHitWithoutReceiver = value; }

        public bool IncludeTriggers { get => includeTriggers; set => includeTriggers = value; }

        /// <summary>
        /// Core 또는 Hybrid query 대상용 passive shape를 생성할지 결정합니다.
        /// </summary>
        public bool QueryableByHybridPhysics2D { get => queryableByHybridPhysics2D; set => queryableByHybridPhysics2D = value; }

        /// <summary>
        /// Bodyless PhysicsCore2D 투사체의 엄격 CCD 정책입니다.
        /// </summary>
        public StrictCcdOverride2D StrictCcdOverride { get => strictCcdOverride; set => strictCcdOverride = value; }

        /// <summary>
        /// 자동 엄격 CCD를 활성화할 이동량과 형상 크기의 비율입니다.
        /// </summary>
        public float StrictCcdThresholdRatio { get => strictCcdThresholdRatio; set => strictCcdThresholdRatio = Mathf.Max(0.01f, value); }

        /// <summary>spawn snapshot에 복사할 sprite입니다.</summary>
        public Sprite Sprite { get => sprite; set => sprite = value; }

        /// <summary>spawn snapshot에 복사할 고정 월드 스케일입니다.</summary>
        public float FixedWorldScale { get => fixedWorldScale; set => fixedWorldScale = Mathf.Max(0f, value); }

        /// <summary>2D hit/query 경로를 사용할지 결정합니다.</summary>
        public bool Use2D { get => use2D; set => use2D = value; }

        /// <summary>
        /// 비활성화될 때 발사 누적값과 emitter 통계를 초기화할지 결정합니다.
        /// projectile body 안의 활성 탄은 `ClearProjectilesOnDisable`이 켜진 경우에만 정리합니다.
        /// </summary>
        public bool ResetEmissionStateOnDisable { get => resetEmissionStateOnDisable; set => resetEmissionStateOnDisable = value; }

        /// <summary>
        /// 비활성화될 때 연결된 body의 활성 투사체도 함께 정리할지 결정합니다.
        /// 여러 emitter가 같은 body를 공유하는 구성에서는 false를 유지해야 다른 emitter의 탄을 지우지 않습니다.
        /// </summary>
        public bool ClearProjectilesOnDisable { get => clearProjectilesOnDisable; set => clearProjectilesOnDisable = value; }

        /// <summary>누적 발사 burst 수입니다.</summary>
        public int TotalBurstsEmitted => totalBurstsEmitted;

        /// <summary>누적 생성 투사체 수입니다.</summary>
        public int TotalProjectilesEmitted => totalProjectilesEmitted;

        /// <summary>다음 burst까지 누적된 소수 burst 값입니다.</summary>
        public float PendingBurstFraction => burstAccumulator;



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
            fixedWorldScale = Mathf.Max(0f, fixedWorldScale);
            strictCcdThresholdRatio = Mathf.Max(0.01f, strictCcdThresholdRatio);
        }



        private void Awake()
        {
            ApplyProfile();
            EnsureProjectileBody();
        }



        private void Update()
        {
            if (!emitOnUpdate)
            {
                return;
            }

            Tick(useUnscaledDeltaTime ? Time.unscaledDeltaTime : Time.deltaTime);
        }



        private void OnDisable()
        {
            if (resetEmissionStateOnDisable)
            {
                ResetEmissionState();
            }

            if (clearProjectilesOnDisable)
            {
                ClearProjectiles();
            }
        }

        /// <summary>
        /// EditMode 테스트에서 Unity lifecycle 반영 지연 없이 disable 동작 계약을 확인하기 위한 내부 진단 API입니다.
        /// production 코드에서는 GameObject/Component lifecycle을 사용합니다.
        /// </summary>
        internal void RunDisableForDiagnostics()
        {
            OnDisable();
        }



        [Button("현재 Emitter 상태 로그")]
        private void LogInspectorDiagnostics()
        {
            EnsureProjectileBody();
            string bodyName = projectileBody != null ? projectileBody.GetType().Name : "연결 안 됨";
            Debug.Log($"[ProjectileEmitter] Body={bodyName}, Burst={totalBurstsEmitted}, Spawned={totalProjectilesEmitted}", this);
        }



        /// <summary>
        /// 임의의 `IPanProjectileBody` backend로 emitter를 초기화합니다.
        /// </summary>
        public void Initialize(IPanProjectileBody body)
        {
            ProjectileBody = body;
            ResetEmissionState();
        }



        /// <summary>
        /// 현재 profile을 emitter 설정에 적용합니다.
        /// </summary>
        public bool ApplyProfile()
        {
            return ApplyProfile(profile);
        }



        /// <summary>
        /// 지정 profile을 emitter 설정에 적용합니다.
        /// </summary>
        public bool ApplyProfile(ProjectileEmitterProfile profileToApply)
        {
            if (profileToApply == null)
            {
                return false;
            }

            profile = profileToApply;
            profileToApply.ApplyTo(this);
            return true;
        }



        /// <summary>
        /// burst 누적값과 runtime 발사 통계를 초기화합니다.
        /// </summary>
        public void ResetEmissionState()
        {
            burstAccumulator = 0f;
            totalBurstsEmitted = 0;
            totalProjectilesEmitted = 0;
        }



        /// <summary>
        /// deltaTime만큼 발사 누적값을 갱신하고 필요한 burst를 생성합니다.
        /// </summary>
        public int Tick(float deltaTime)
        {
            if (deltaTime <= 0f || burstsPerSecond <= 0f)
            {
                return 0;
            }

            burstAccumulator += deltaTime * burstsPerSecond;
            int burstCount = Mathf.Min(maxBurstsPerTick, Mathf.FloorToInt(burstAccumulator));
            if (burstCount <= 0)
            {
                return 0;
            }

            burstAccumulator -= burstCount;

            int spawned = 0;
            for (int i = 0; i < burstCount; i++)
            {
                spawned += SpawnBurst();
            }

            return spawned;
        }



    }
}
