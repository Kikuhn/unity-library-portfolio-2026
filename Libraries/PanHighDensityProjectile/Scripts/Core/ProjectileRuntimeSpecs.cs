using Pan.HighDensityElement;
using UnityEngine;

namespace Pan.HighDensityProjectile
{

    /// <summary>
    /// 위치, 속도, 원형 반경처럼 "움직이는 원형 물체"라면 재사용할 수 있는 운동 상태입니다.
    /// <para>투사체 전용 값이 아니므로 일반 물리 오브젝트, 임시 hit volume, 고밀도탄 native slot에도 같은 의미로 사용할 수 있습니다.</para>
    /// </summary>
    public struct KinematicCircle2DState
    {
        public KinematicCircle2DState(Vector3 position, Vector3 velocity, float radius, bool use2D = true)
        {
            Position = position;
            Velocity = velocity;
            Radius = Mathf.Max(0f, radius);
            Use2D = use2D;
        }

        /// <summary>월드 위치입니다.</summary>
        public Vector3 Position;

        /// <summary>월드 속도입니다.</summary>
        public Vector3 Velocity;

        /// <summary>원형 판정 반경입니다.</summary>
        public float Radius;

        /// <summary>2D 판정 경로를 사용할지 나타냅니다.</summary>
        public bool Use2D;
    }



    /// <summary>
    /// 시간 기반 자동 소멸이나 진행도 계산에 쓰는 범용 수명 상태입니다.
    /// </summary>
    public readonly struct TimedLifetimeState
    {
        public TimedLifetimeState(float initialSeconds, float remainingSeconds)
        {
            InitialSeconds = Mathf.Max(0f, initialSeconds);
            RemainingSeconds = Mathf.Max(0f, remainingSeconds);
            ElapsedSeconds = Mathf.Max(0f, InitialSeconds - RemainingSeconds);
            Normalized = InitialSeconds > 0f ? Mathf.Clamp01(ElapsedSeconds / InitialSeconds) : 0f;
        }

        /// <summary>처음 설정된 수명입니다.</summary>
        public float InitialSeconds { get; }

        /// <summary>남은 수명입니다.</summary>
        public float RemainingSeconds { get; }

        /// <summary>경과한 시간입니다.</summary>
        public float ElapsedSeconds { get; }

        /// <summary>0에서 1 사이의 진행도입니다.</summary>
        public float Normalized { get; }

        /// <summary>수명 시작 상태를 만듭니다.</summary>
        public static TimedLifetimeState Start(float seconds)
        => new(seconds, seconds);

        /// <summary>남은 시간만 갱신한 새 상태를 만듭니다.</summary>
        public TimedLifetimeState WithRemaining(float remainingSeconds)
        => new(InitialSeconds, remainingSeconds);

        public static implicit operator TimedLifetimeState(float seconds)
        => Start(seconds);

        public static implicit operator float(TimedLifetimeState state)
        => state.InitialSeconds;
    }



    /// <summary>
    /// 투사체의 속도에 매 프레임 적용할 범용 운동 보정값입니다.
    /// <para>기본값은 중력과 감쇠가 모두 꺼진 상태이므로 기존 직선 이동 동작을 그대로 보존합니다.</para>
    /// </summary>
    public readonly struct ProjectileMotionSpec
    {
        public static readonly ProjectileMotionSpec None = new(false, Vector3.zero, 0f, 0f);

        public ProjectileMotionSpec(bool useGravity, Vector3 gravity, float linearDamping = 0f, float frictionCoefficient = 0f)
        {
            UseGravity = useGravity;
            Gravity = useGravity ? gravity : Vector3.zero;
            LinearDamping = Mathf.Max(0f, linearDamping);
            FrictionCoefficient = Mathf.Max(0f, frictionCoefficient);
        }

        /// <summary>중력 가속도를 속도에 적용할지 여부입니다.</summary>
        public bool UseGravity { get; }

        /// <summary>초당 속도 변화량으로 적용할 중력 벡터입니다.</summary>
        public Vector3 Gravity { get; }

        /// <summary>속도를 줄이는 선형 감쇠 계수입니다. 0이면 감쇠하지 않습니다.</summary>
        public float LinearDamping { get; }

        /// <summary>표면 마찰처럼 추가로 속도를 줄이는 계수입니다. 0이면 마찰을 적용하지 않습니다.</summary>
        public float FrictionCoefficient { get; }

        /// <summary>중력 또는 감쇠 계산이 실제로 필요한지 반환합니다.</summary>
        public bool HasVelocityModifier => UseGravity || LinearDamping > 0f || FrictionCoefficient > 0f;

        /// <summary>deltaTime 동안 속도에 motion 규칙을 적용합니다.</summary>
        public Vector3 IntegrateVelocity(Vector3 velocity, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return velocity;
            }

            if (UseGravity)
            {
                velocity += Gravity * deltaTime;
            }

            float damping = LinearDamping + FrictionCoefficient;
            if (damping > 0f)
            {
                velocity *= 1f / (1f + damping * deltaTime);
            }

            return velocity;
        }
    }



    /// <summary>
    /// Unity layer 기반 충돌 필터입니다. 물리 layer와 gameplay team/damage 규칙은 서로 분리해서 다룹니다.
    /// </summary>
    public struct CollisionLayerFilter
    {
        public CollisionLayerFilter(int objectLayer, LayerMask hitLayers, bool consumeHitWithoutReceiver)
            : this(objectLayer, hitLayers, consumeHitWithoutReceiver, true)
        {
        }


        public CollisionLayerFilter(
            int objectLayer,
            LayerMask hitLayers,
            bool consumeHitWithoutReceiver,
            bool includeTriggers)
        {
            ObjectLayer = Mathf.Clamp(objectLayer, 0, 31);
            HitLayers = hitLayers;
            ConsumeHitWithoutReceiver = consumeHitWithoutReceiver;
            IncludeTriggers = includeTriggers;
        }

        /// <summary>이 오브젝트 자신에게 적용할 Unity layer입니다.</summary>
        public int ObjectLayer;

        /// <summary>충돌 대상으로 인정할 Unity layer mask입니다.</summary>
        public LayerMask HitLayers;

        /// <summary>receiver가 없는 단순 collider 충돌도 소비된 hit로 볼지 결정합니다.</summary>
        public bool ConsumeHitWithoutReceiver;

        /// <summary>
        /// Trigger 형상을 충돌 후보에 포함할지 결정합니다.
        /// </summary>
        public bool IncludeTriggers;
    }



    /// <summary>
    /// gameplay 피아식별 태그입니다. 물리 layer와 별도로 damage/graze 규칙에서 사용합니다.
    /// </summary>
    public readonly struct TeamRelationTag
    {
        public TeamRelationTag(int teamId)
        {
            TeamId = teamId;
        }

        /// <summary>아군/적군/중립 등 gameplay 필터링에 사용하는 id입니다.</summary>
        public int TeamId { get; }
    }



    /// <summary>
    /// hit가 발생했을 때 전달할 damage 값입니다. 체력 시스템 외의 hit payload에도 재사용할 수 있습니다.
    /// </summary>
    public readonly struct HitDamageSpec
    {
        public HitDamageSpec(float damage)
        {
            Damage = Mathf.Max(0f, damage);
        }

        /// <summary>기본 damage 값입니다.</summary>
        public float Damage { get; }
    }



    /// <summary>
    /// Sprite 계열 표시 설정입니다. 외부 오브젝트 풀, SpriteRenderer, instanced renderer가 같은 값을 읽습니다.
    /// </summary>
    public struct SpriteVisualSpec
    {
        public SpriteVisualSpec(Sprite sprite, int frameIndex, float fixedWorldScale, int sortingLayerId = 0, int sortingOrder = 0)
        {
            Sprite = sprite;
            FrameIndex = frameIndex;
            FixedWorldScale = Mathf.Max(0f, fixedWorldScale);
            SortingLayerId = sortingLayerId;
            SortingOrder = sortingOrder;
        }

        /// <summary>직접 주입된 sprite입니다. 이름 기반 asset 로드는 상위 profile/factory가 담당합니다.</summary>
        public Sprite Sprite;

        /// <summary>여러 frame을 쓰는 renderer에서 선택할 frame index입니다.</summary>
        public int FrameIndex;

        /// <summary>반경과 별도로 적용할 월드 기준 표시 스케일입니다.</summary>
        public float FixedWorldScale;

        /// <summary>정렬 레이어 id입니다.</summary>
        public int SortingLayerId;

        /// <summary>정렬 순서입니다.</summary>
        public int SortingOrder;
    }



    /// <summary>
    /// 접촉 피드백을 어떤 방식으로 표시할지 나타내는 공통 모드입니다.
    /// <para>패키지는 이 값을 저장하고 전달만 하며, 실제 표시 구현은 consumer 프로젝트나 별도 renderer가 담당합니다.</para>
    /// </summary>
    public enum ProjectileContactExplosionMode
    {
        /// <summary>접촉 피드백을 만들지 않습니다.</summary>
        Disabled,

        /// <summary>consumer 프로젝트가 제공하는 풀링 렌더 오브젝트로 피드백을 표시합니다.</summary>
        PooledRenderSprite,

        /// <summary>instanced/GPU 계열 renderer가 짧은 flash를 표시합니다.</summary>
        InstancedFlash,
    }



    /// <summary>
    /// 접촉 시 재생할 피드백 설정입니다. 총알 폭발뿐 아니라 피격 이펙트나 임시 VFX에도 재사용합니다.
    /// </summary>
    public struct ContactFeedbackSpec
    {
        public ContactFeedbackSpec(ProjectileContactExplosionMode mode, Sprite sprite, float worldScale, float duration)
        {
            Mode = mode;
            Sprite = sprite;
            WorldScale = Mathf.Max(0f, worldScale);
            Duration = Mathf.Max(0f, duration);
        }

        /// <summary>접촉 피드백 재생 방식입니다.</summary>
        public ProjectileContactExplosionMode Mode;

        /// <summary>피드백에 사용할 sprite입니다.</summary>
        public Sprite Sprite;

        /// <summary>피드백 시작 스케일입니다.</summary>
        public float WorldScale;

        /// <summary>피드백 지속 시간입니다.</summary>
        public float Duration;
    }



    /// <summary>
    /// backend에 한 개체 생성을 요청할 때 사용하는 조립형 입력입니다.
    /// <para>일반탄과 고밀도탄은 이 요청을 공유하고, 각 backend가 자신에게 맞는 runtime 상태로 변환합니다.</para>
    /// </summary>
    public struct ProjectileSpawnRequest
    {
        /// <summary>요청을 만든 spawner, actor, manager 등 source component입니다.</summary>
        public Component Source;

        /// <summary>운동/원형 판정 상태입니다.</summary>
        public KinematicCircle2DState Kinematic;

        /// <summary>시간 기반 수명 상태입니다.</summary>
        public TimedLifetimeState Lifetime;

        /// <summary>중력, 감쇠, 마찰처럼 속도에 누적 적용할 운동 규칙입니다.</summary>
        public ProjectileMotionSpec Motion;

        /// <summary>Unity layer 충돌 필터입니다.</summary>
        public CollisionLayerFilter Collision;

        /// <summary>gameplay 피아식별 태그입니다.</summary>
        public TeamRelationTag Team;

        /// <summary>hit payload damage 설정입니다.</summary>
        public HitDamageSpec Hit;

        /// <summary>표시 설정입니다.</summary>
        public SpriteVisualSpec Visual;

        /// <summary>
        /// GameObject 없는 Element renderer가 사용하는 visual registry id입니다.
        /// 원본 frame index 계약과 분리되며 다른 backend는 이 값을 무시합니다.
        /// </summary>
        public int ElementVisualId;

        /// <summary>
        /// PhysicsCore2D query target shape를 만들어 HybridPhysicsWorld2D와 다른 Core query에 노출할지 결정합니다.
        /// 같은 non-zero OwnerId의 projectile query에서는 제외되며 일반 대량 탄막은 false가 기본입니다.
        /// </summary>
        public bool QueryableByHybridPhysics2D;

        /// <summary>
        /// Bodyless PhysicsCore2D 투사체의 엄격 CCD 활성화 정책입니다.
        /// 다른 backend는 이 값을 무시할 수 있습니다.
        /// </summary>
        public StrictCcdOverride2D StrictCcdOverride;

        /// <summary>
        /// Auto 정책에서 이동량과 형상 최소 크기를 비교할 비율입니다.
        /// 0 이하이면 기본값 0.5를 사용합니다.
        /// </summary>
        public float StrictCcdThresholdRatio;

        /// <summary>접촉 피드백 설정입니다.</summary>
        public ContactFeedbackSpec Feedback;

        public Vector3 Position { get => Kinematic.Position; set => Kinematic.Position = value; }
        public Vector3 Velocity { get => Kinematic.Velocity; set => Kinematic.Velocity = value; }
        public float Radius { get => Kinematic.Radius; set => Kinematic.Radius = Mathf.Max(0f, value); }
        public bool Use2D { get => Kinematic.Use2D; set => Kinematic.Use2D = value; }
        public float LifetimeSeconds { get => Lifetime.InitialSeconds; set => Lifetime = TimedLifetimeState.Start(value); }
        public bool UseGravity { get => Motion.UseGravity; set => Motion = new ProjectileMotionSpec(value, Motion.Gravity, Motion.LinearDamping, Motion.FrictionCoefficient); }
        public Vector3 Gravity { get => Motion.Gravity; set => Motion = new ProjectileMotionSpec(true, value, Motion.LinearDamping, Motion.FrictionCoefficient); }
        public float LinearDamping { get => Motion.LinearDamping; set => Motion = new ProjectileMotionSpec(Motion.UseGravity, Motion.Gravity, value, Motion.FrictionCoefficient); }
        public float FrictionCoefficient { get => Motion.FrictionCoefficient; set => Motion = new ProjectileMotionSpec(Motion.UseGravity, Motion.Gravity, Motion.LinearDamping, value); }
        public float Damage { get => Hit.Damage; set => Hit = new HitDamageSpec(value); }
        public int TeamId { get => Team.TeamId; set => Team = new TeamRelationTag(value); }
        public LayerMask HitLayers { get => Collision.HitLayers; set => Collision.HitLayers = value; }
        public int VisualFrameIndex { get => Visual.FrameIndex; set => Visual.FrameIndex = value; }
        public float EffectiveStrictCcdThresholdRatio =>
            StrictCcdThresholdRatio > 0f ? StrictCcdThresholdRatio : 0.5f;

        /// <summary>중력 사용 여부, 중력 벡터, 감쇠 값을 한 번에 지정합니다.</summary>
        public void SetMotion(bool useGravity, Vector3 gravity, float linearDamping = 0f, float frictionCoefficient = 0f)
        {
            Motion = new ProjectileMotionSpec(useGravity, gravity, linearDamping, frictionCoefficient);
        }
    }



    /// <summary>
    /// 렌더링, 디버그, 이벤트, 외부 adapter가 읽는 조립형 snapshot입니다.
    /// <para>backend 내부 슬롯, pooled object, native container를 직접 노출하지 않기 위한 경계입니다.</para>
    /// </summary>
    public readonly struct ProjectileSnapshot
    {
        public ProjectileSnapshot(
        int projectileId,
        KinematicCircle2DState kinematic,
        TimedLifetimeState lifetime,
        TeamRelationTag team,
        HitDamageSpec hit,
        SpriteVisualSpec visual,
        ProjectileMotionSpec motion = default)
        {
            ProjectileId = projectileId;
            Kinematic = kinematic;
            Lifetime = lifetime;
            Team = team;
            Hit = hit;
            Visual = visual;
            Motion = motion;
        }

        public ProjectileSnapshot(int projectileId, Vector3 position, Vector3 velocity, float radius, float remainingLifetime, float damage, int teamId, bool use2D, int visualFrameIndex = 0, float initialLifetime = -1f, ProjectileMotionSpec motion = default)
        : this(
        projectileId,
        new KinematicCircle2DState(position, velocity, radius, use2D),
        new TimedLifetimeState(initialLifetime > 0f ? initialLifetime : remainingLifetime, remainingLifetime),
        new TeamRelationTag(teamId),
        new HitDamageSpec(damage),
        new SpriteVisualSpec(null, visualFrameIndex, 0f),
        motion)
        {
        }

        /// <summary>backend 내부에서 발급한 id입니다.</summary>
        public int ProjectileId { get; }

        /// <summary>운동/원형 판정 snapshot입니다.</summary>
        public KinematicCircle2DState Kinematic { get; }

        /// <summary>수명 snapshot입니다.</summary>
        public TimedLifetimeState Lifetime { get; }

        /// <summary>snapshot 시점의 운동 규칙입니다. snapshot은 backend 상태에서 복사된 읽기 전용 값입니다.</summary>
        public ProjectileMotionSpec Motion { get; }

        /// <summary>피아식별 snapshot입니다.</summary>
        public TeamRelationTag Team { get; }

        /// <summary>damage snapshot입니다.</summary>
        public HitDamageSpec Hit { get; }

        /// <summary>표시 snapshot입니다.</summary>
        public SpriteVisualSpec Visual { get; }

        public Vector3 Position => Kinematic.Position;
        public Vector3 Velocity => Kinematic.Velocity;
        public float Radius => Kinematic.Radius;
        public bool Use2D => Kinematic.Use2D;
        public float RemainingLifetime => Lifetime.RemainingSeconds;
        public float InitialLifetime => Lifetime.InitialSeconds;
        public float ElapsedLifetime => Lifetime.ElapsedSeconds;
        public float NormalizedLifetime => Lifetime.Normalized;
        public bool UseGravity => Motion.UseGravity;
        public Vector3 Gravity => Motion.Gravity;
        public float LinearDamping => Motion.LinearDamping;
        public float FrictionCoefficient => Motion.FrictionCoefficient;
        public float Damage => Hit.Damage;
        public int TeamId => Team.TeamId;
        public int VisualFrameIndex => Visual.FrameIndex;
    }
}
