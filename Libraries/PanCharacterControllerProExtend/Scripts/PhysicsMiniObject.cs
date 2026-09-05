using UnityEngine;
using Sirenix.OdinInspector;
using System;
using System.Runtime.CompilerServices;
using Pan.Util.PhysicsAgent;
using Pan.Util.Game;
using System.Buffers;



public interface IPhysicsMiniObject
{


}



[DisallowMultipleComponent]
public class PhysicsMiniObject : MonoBehaviour,
    IPhysicsMiniObject,
    IDimensionPhysicsAgent,
    IPhysicsPositionPhysicsAgent,
    IInstantMovePositionPhysicsAgent,
    IPreviousFramePhysicsPositionPhysicsAgent,
    IExpectedVelocityPhysicsAgent,
    IActualVelocityPhysicsAgent,
    IBodySizeRadiusPhysicsAgent,
    IMovingPhysicsAgent,
    IHoldMovementScaledPhysicsAgentCore,
    IHoldMovementUnScaledPhysicsAgentCore,
    IPhysicsUpdatePreSimulationPhysicsAgent<PhysicsMiniObject>, IPhysicsUpdatePostSimulationPhysicsAgent<PhysicsMiniObject>,
    ITimeScaling
{
    ///======================================================================================================================================================



#if UNITY_EDITOR

    [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Center, EnableRichText = true, Overflow = false), EnableGUI]
    [PropertyOrder(-1000)]
    private string editor_Title => $"<b><size=14>PhysicsMiniObject</size></b>\n<i><size=12>Kinematic + Sweep/Slide (2D/3D)</size></i>";
    private bool editor_ApplicationIsPlaying => Application.isPlaying;
#endif



    ///======================================================================================================================================================



    //. 컴포넌트 (2D/3D)



    [TitleGroup("컴포넌트"), FoldoutGroup("컴포넌트/박스", false)]
    [SerializeField, ShowIf(nameof(is2D)), ReadOnly] private Rigidbody2D m_Rigidbody2D;
    [TitleGroup("컴포넌트"), FoldoutGroup("컴포넌트/박스", false)]
    [SerializeField, ShowIf(nameof(is2D)), ReadOnly] private CircleCollider2D m_CircleCollider2D;

    [TitleGroup("컴포넌트"), FoldoutGroup("컴포넌트/박스", false)]
    [SerializeField, HideIf(nameof(is2D)), ReadOnly] private Rigidbody m_Rigidbody3D;
    [TitleGroup("컴포넌트"), FoldoutGroup("컴포넌트/박스", false)]
    [SerializeField, HideIf(nameof(is2D)), ReadOnly] private SphereCollider m_SphereCollider3D;



    [MethodImpl(MethodImplOptions.AggressiveInlining)] private static bool UNull(UnityEngine.Object o) => o == null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] private bool Has2D() => m_Rigidbody2D && m_CircleCollider2D;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] private bool Has3D() => m_Rigidbody3D && m_SphereCollider3D;



    private bool EnsureComponents2D()
    {
        if (UNull(m_Rigidbody2D)) m_Rigidbody2D = null;
        if (UNull(m_CircleCollider2D)) m_CircleCollider2D = null;

        if (m_Rigidbody2D == null && !TryGetComponent(out m_Rigidbody2D)) m_Rigidbody2D = gameObject.AddComponent<Rigidbody2D>();
        if (m_CircleCollider2D == null && !TryGetComponent(out m_CircleCollider2D)) m_CircleCollider2D = gameObject.AddComponent<CircleCollider2D>();

        //! 실패시 비활성
        if (UNull(m_Rigidbody2D) || UNull(m_CircleCollider2D)) { Debug.LogError("[PhysicsMiniObject] 2D 컴포넌트 생성 실패", this); return false; }

        //. 런타임 설정
        m_Rigidbody2D.simulated = true;
        m_Rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        m_Rigidbody2D.interpolation = rbInterpolation2D;
        m_Rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Discrete;

        m_CircleCollider2D.isTrigger = initialSelfIsTrigger;

        //. 반지름 동기화
        float r = initialBodyRadius > 0f ? initialBodyRadius
                 : (m_CircleCollider2D.radius > 0f ? m_CircleCollider2D.radius : 0.25f);
        r = Mathf.Max(0f, r);
        m_CircleCollider2D.radius = r;
        initialBodyRadius = r;
        return true;
    }



    private bool EnsureComponents3D()
    {
        if (UNull(m_Rigidbody3D)) m_Rigidbody3D = null;
        if (UNull(m_SphereCollider3D)) m_SphereCollider3D = null;

        if (m_Rigidbody3D == null && !TryGetComponent(out m_Rigidbody3D)) m_Rigidbody3D = gameObject.AddComponent<Rigidbody>();
        if (m_SphereCollider3D == null && !TryGetComponent(out m_SphereCollider3D)) m_SphereCollider3D = gameObject.AddComponent<SphereCollider>();

        //! 실패시 비활성
        if (UNull(m_Rigidbody3D) || UNull(m_SphereCollider3D)) { Debug.LogError("[PhysicsMiniObject] 3D 컴포넌트 생성 실패", this); return false; }

        //. 런타임 설정
        m_Rigidbody3D.isKinematic = true;
        m_Rigidbody3D.interpolation = rbInterpolation3D;
        m_Rigidbody3D.collisionDetectionMode = CollisionDetectionMode.Discrete;

        m_SphereCollider3D.isTrigger = initialSelfIsTrigger;

        //. 반지름 동기화
        float r = initialBodyRadius > 0f ? initialBodyRadius
                 : (m_SphereCollider3D.radius > 0f ? m_SphereCollider3D.radius : 0.25f);
        r = Mathf.Max(0f, r);
        m_SphereCollider3D.radius = r;
        initialBodyRadius = r;
        return true;
    }



    ///======================================================================================================================================================



    //. 좌표



    [TitleGroup("좌표"), BoxGroup("좌표/박스", false)]
    [ShowInInspector, LabelText("물리 좌표")]
    [PropertyTooltip("에디터에선 Transform.position을 그대로 사용/표시합니다.")]
    public Vector3 PhysicsPosition
    {
        get
        {
            if (!Application.isPlaying) return transform.position;
            if (is2D && Has2D()) return new Vector3(m_Rigidbody2D.position.x, m_Rigidbody2D.position.y, 0f);
            if (!is2D && Has3D()) return m_Rigidbody3D.position;
            return transform.position;
        }
        set
        {
            if (!Application.isPlaying) { transform.position = value; return; }
            if (is2D && Has2D()) m_Rigidbody2D.position = (Vector2)value;
            else if (!is2D && Has3D()) m_Rigidbody3D.position = value;
        }
    }

    [TitleGroup("좌표"), BoxGroup("좌표/박스", false)]
    [ShowInInspector, DisplayAsString, LabelText("이전 프레임 물리 좌표")]
    public Vector3 PreviousFramePhysicsPosition { get; protected set; }



    ///======================================================================================================================================================



    //. 이동



    [TitleGroup("이동"), BoxGroup("이동/박스", false)]
    [ShowInInspector, DisplayAsString, LabelText("예상 Velocity")]
    public Vector3 ExpectedVelocity { get; protected set; } = Vector3.zero;



    [TitleGroup("이동"), BoxGroup("이동/박스", false)]
    [ShowInInspector, DisplayAsString, LabelText("실제 Velocity")]
    public Vector3 ActualVelocity { get; protected set; } = Vector3.zero;

#if UNITY_EDITOR

    [TitleGroup("이동"), BoxGroup("이동/박스", false)]
    [ShowInInspector, DisplayAsString, EnableGUI, LabelText("예상 이동 여부")]
    [GUIColor(nameof(editorColor_IsMovingExpected))] private bool editor_IsMovingExpected => willApplyDelta.sqrMagnitude > 1e-8f;

    [TitleGroup("이동"), BoxGroup("이동/박스", false)]
    [HorizontalGroup("이동/박스/예상축별", Width = 0.333f)]
    [ShowInInspector, DisplayAsString, EnableGUI, Indent(1), LabelText("X"), GUIColor(nameof(editorColor_IsMovingExpectedX))] private bool editor_IsMovingExpectedX => Mathf.Abs(willApplyDelta.x) > 1e-4f;
    [HorizontalGroup("이동/박스/예상축별", Width = 0.333f)]
    [ShowInInspector, DisplayAsString, EnableGUI, LabelText("Y"), GUIColor(nameof(editorColor_IsMovingExpectedY))] private bool editor_IsMovingExpectedY => Mathf.Abs(willApplyDelta.y) > 1e-4f;
    [HorizontalGroup("이동/박스/예상축별", Width = 0.334f)]
    [ShowInInspector, DisplayAsString, EnableGUI, LabelText("Z"), GUIColor(nameof(editorColor_IsMovingExpectedZ))] private bool editor_IsMovingExpectedZ => Mathf.Abs(willApplyDelta.z) > 1e-4f;

    private Color editorColor_IsMovingExpected => editor_IsMovingExpected ? new Color(0.31f, 0.76f, 0.91f, 1f) : Color.white;
    private Color editorColor_IsMovingExpectedX => editor_IsMovingExpectedX ? new Color(0.31f, 0.76f, 0.91f, 1f) : Color.white;
    private Color editorColor_IsMovingExpectedY => editor_IsMovingExpectedY ? new Color(0.31f, 0.76f, 0.91f, 1f) : Color.white;
    private Color editorColor_IsMovingExpectedZ => editor_IsMovingExpectedZ ? new Color(0.31f, 0.76f, 0.91f, 1f) : Color.white;

    [TitleGroup("이동"), BoxGroup("이동/박스", false)]
    [ShowInInspector, DisplayAsString, EnableGUI, LabelText("실측 이동 여부")]
    [GUIColor(nameof(editorColor_IsMovingActual))] private bool editor_IsMovingActual => ActualVelocity.sqrMagnitude > 1e-8f;

    [TitleGroup("이동"), BoxGroup("이동/박스", false)]
    [HorizontalGroup("이동/박스/실측축별", Width = 0.333f)]
    [ShowInInspector, DisplayAsString, EnableGUI, Indent(1), LabelText("X"), GUIColor(nameof(editorColor_IsMovingActualX))] private bool editor_IsMovingActualX => Mathf.Abs(ActualVelocity.x) > 1e-4f;
    [HorizontalGroup("이동/박스/실측축별", Width = 0.333f)]
    [ShowInInspector, DisplayAsString, EnableGUI, LabelText("Y"), GUIColor(nameof(editorColor_IsMovingActualY))] private bool editor_IsMovingActualY => Mathf.Abs(ActualVelocity.y) > 1e-4f;
    [HorizontalGroup("이동/박스/실측축별", Width = 0.334f)]
    [ShowInInspector, DisplayAsString, EnableGUI, LabelText("Z"), GUIColor(nameof(editorColor_IsMovingActualZ))] private bool editor_IsMovingActualZ => Mathf.Abs(ActualVelocity.z) > 1e-4f;

    private Color editorColor_IsMovingActual => editor_IsMovingActual ? new Color(0.31f, 0.76f, 0.91f, 1f) : Color.white;
    private Color editorColor_IsMovingActualX => editor_IsMovingActualX ? new Color(0.31f, 0.76f, 0.91f, 1f) : Color.white;
    private Color editorColor_IsMovingActualY => editor_IsMovingActualY ? new Color(0.31f, 0.76f, 0.91f, 1f) : Color.white;
    private Color editorColor_IsMovingActualZ => editor_IsMovingActualZ ? new Color(0.31f, 0.76f, 0.91f, 1f) : Color.white;

#endif

    [TitleGroup("이동"), BoxGroup("이동/박스", false)]
    [ShowInInspector, DisplayAsString, LabelText("적용 예정 Velocity")]
    [PropertyTooltip("다음 물리 스텝에서 소비될 누적 Δ(이동량). CCPObject와 동일하게 Δ를 누적해 한 번에 적용합니다.")]
    public Vector3 WillApplyVelocity => willApplyDelta; //. 이름은 호환 유지
    protected Vector3 willApplyDelta = Vector3.zero;    //. CCP와 동일하게 Δ를 보관



    ///======================================================================================================================================================



    //. 상태



    ///======================================================================================================================================================



    //. 바디



    [TitleGroup("바디"), BoxGroup("바디/박스", false)]
    [SerializeField, LabelText("2D 모드")]
    [PropertyTooltip("On : 2D(Rigidbody2D, CircleCollider2D)\nOff : 3D(Rigidbody, SphereCollider)")]
    private bool is2D = true;
    public bool Is2D => is2D;



    [SerializeField, HideInInspector] private float initialBodyRadius = 0.5f;

    [TitleGroup("바디"), BoxGroup("바디/박스", false)]
    [ShowInInspector, LabelText("반지름 (Radius)")]
    [PropertyTooltip("런타임 전에는 내부 필드만, 런타임에는 실제 콜라이더 반지름과 동기화합니다.")]
    public float BodySizeRadius
    {
        get
        {
            if (Application.isPlaying)
            {
                if (is2D && Has2D()) return m_CircleCollider2D.radius;
                if (!is2D && Has3D()) return m_SphereCollider3D.radius;
            }
            return initialBodyRadius;
        }
        set
        {
            initialBodyRadius = Mathf.Max(0f, value);
            if (!Application.isPlaying) return;
            if (is2D && Has2D()) m_CircleCollider2D.radius = initialBodyRadius;
            else if (!is2D && Has3D()) m_SphereCollider3D.radius = initialBodyRadius;
        }
    }



    //. 자기 콜라이더 isTrigger (초기/런타임 동기화)
    [TitleGroup("바디"), BoxGroup("바디/박스", false)]
    [SerializeField, LabelText("콜라이더 isTrigger (초기)")]
#if UNITY_EDITOR
    [DisableIf(nameof(editor_ApplicationIsPlaying))]
#endif
    private bool initialSelfIsTrigger = false;



    [TitleGroup("바디"), BoxGroup("바디/박스", false)]
    [ShowInInspector, LabelText("콜라이더 isTrigger")]
#if UNITY_EDITOR
    [ShowIf(nameof(editor_ApplicationIsPlaying))]
#endif
    [PropertyTooltip("2D: CircleCollider2D.isTrigger / 3D: SphereCollider.isTrigger")]
    public bool SelfColliderIsTrigger
    {
        get
        {
            if (Application.isPlaying)
            {
                if (is2D && Has2D()) return m_CircleCollider2D.isTrigger;
                if (!is2D && Has3D()) return m_SphereCollider3D.isTrigger;
            }
            return initialSelfIsTrigger;
        }
        set
        {
            initialSelfIsTrigger = value;
            if (!Application.isPlaying) return;
            if (is2D && Has2D()) m_CircleCollider2D.isTrigger = value;
            else if (!is2D && Has3D()) m_SphereCollider3D.isTrigger = value;
        }
    }



    [TitleGroup("바디"), BoxGroup("바디/박스", false)]
    [SerializeField, HideIf(nameof(is2D)), LabelText("3D Interpolation")]
    private RigidbodyInterpolation rbInterpolation3D = RigidbodyInterpolation.None;



    [TitleGroup("바디"), BoxGroup("바디/박스", false)]
    [SerializeField, ShowIf(nameof(is2D)), LabelText("2D Interpolation")]
    private RigidbodyInterpolation2D rbInterpolation2D = RigidbodyInterpolation2D.None;



    ///======================================================================================================================================================



    //. 설정



    [TitleGroup("설정"), BoxGroup("설정/충돌")]
    [SerializeField, LabelText("충돌 사용 (차폐)")]
    [PropertyTooltip("On : 스윕/슬라이드 차폐 연산\nOff : 캐스트 없이 이동")]
    private bool enableCollision = true;

    [TitleGroup("설정"), BoxGroup("설정/충돌")]
    [SerializeField, LabelText("충돌 레이어")]
    [EnableIf(nameof(enableCollision))]
    [Indent(1)]
    private LayerMask blockingLayers = ~0;

    [TitleGroup("설정"), BoxGroup("설정/박스", false)]
    [SerializeField, MinValue(0f), LabelText("skin")]
    [PropertyTooltip("접촉면 떨림 방지 여유거리. defaultContactOffset과의 최대값 사용")]
    private float skin = 0.01f;

    [TitleGroup("설정"), BoxGroup("설정/박스", false)]
    [SerializeField, MinValue(0f), LabelText("최소 이동")]
    [PropertyTooltip("이 값보다 작으면 캐스트 생략")]
    private float minMove = 1e-5f;

    [TitleGroup("설정"), BoxGroup("설정/박스", false)]
    [SerializeField, MinValue(0f), LabelText("최대 스윕 스텝 거리")]
    [PropertyTooltip("한 번에 캐스트할 최대 거리. 0이면 비활성")]
    private float maxSweepStep = 0f;

    [TitleGroup("설정"), BoxGroup("설정/박스", false)]
    [SerializeField, MinValue(0), LabelText("슬라이드 반복 횟수")]
    [PropertyTooltip("0=슬라이드 없음, 1~3 권장")]
    private int slideIterations = 1;

    [TitleGroup("설정"), BoxGroup("설정/박스", false)]
    [SerializeField, MinValue(1), LabelText("NonAlloc 버퍼 크기(초기)")]
    private int castBufferSize = 16;

    [TitleGroup("설정"), BoxGroup("설정/박스", false)]
    [SerializeField, LabelText("트리거를 차폐 대상으로 포함")]
    [PropertyTooltip("On: 장면의 isTrigger도 막힘 취급 / Off: 장면 트리거는 통과")]
    private bool collideTriggers = false;

    [TitleGroup("설정"), BoxGroup("설정/박스", false)]
    [SerializeField, LabelText("스폰/텔레포트 겹침 보정")]
    [PropertyTooltip("Awake/텔레포트 직후 겹침 시 작은 거리만큼 밀어냄")]
    private bool depenetrateOnAwake = true;

    [TitleGroup("설정"), BoxGroup("설정/박스", false)]
    [SerializeField, MinValue(0f), LabelText("Depenetration 최대 거리")]
    [PropertyTooltip("한 프레임 보정 상한 (보통 0.05~0.1)")]
    private float maxDepenetration = 0.08f;



    ///======================================================================================================================================================



    //. 이동 에이전트



    ///<inheritdoc/>
    public ScaledVelocityAgentMini MovementAgentScaled { get; protected set; }
    ///<inheritdoc/>
    public UnScaledVelocityAgentMini MovementAgentUnScaled { get; protected set; }



    #region 인터페이스

    MovementPhysicsAgentCore<Vector3, Vector3Ops> IHoldMovementScaledPhysicsAgentCore.MovementAgentScaled => MovementAgentScaled;
    MovementPhysicsAgentCore<Vector3, Vector3Ops> IHoldMovementUnScaledPhysicsAgentCore.MovementAgentUnScaled => MovementAgentUnScaled;

    #endregion



    #region 이동 에이전트 클래스

    public sealed class ScaledVelocityAgentMini : MovementPhysicsAgent_Pending<PhysicsMiniObject>
    {
        public ScaledVelocityAgentMini(PhysicsMiniObject main) : base(main) { }
        protected override void ApplyMovement(in Vector3 delta)
        {
            //. CCP와 동일: Δ 누적. 스케일 적용
            float ts = Main.GetTimeScale();
            Main.willApplyDelta += delta * ts;  //. //? Δ(이동량)를 누적
        }
    }



    public sealed class UnScaledVelocityAgentMini : MovementPhysicsAgent_Pending<PhysicsMiniObject>
    {
        public UnScaledVelocityAgentMini(PhysicsMiniObject main) : base(main) { }
        protected override void ApplyMovement(in Vector3 delta)
        {
            Main.willApplyDelta += delta;       //. //. 타임스케일 미적용 Δ 누적
        }
    }

    #endregion




    public void ApplyInstanceMovement_Velocity(Vector3 velocity)
    {
        //. 즉시 위치 스냅(캐스트 없이)
        if (is2D && Has2D()) m_Rigidbody2D.MovePosition((Vector2)PhysicsPosition + new Vector2(velocity.x, velocity.y));
        else if (!is2D && Has3D()) m_Rigidbody3D.MovePosition(m_Rigidbody3D.position + velocity);
        else transform.position += velocity;
    }



    public void ApplyInstanceMovement_Position(Vector3 position)
    {
        if (is2D && Has2D()) m_Rigidbody2D.position = (Vector2)position;
        else if (!is2D && Has3D()) m_Rigidbody3D.position = position;
        else transform.position = position;

        if (Application.isPlaying && depenetrateOnAwake)
        {
            if (is2D && Has2D()) ResolveOverlaps2D(m_Rigidbody2D.position, m_CircleCollider2D.radius);
            else if (!is2D && Has3D()) ResolveOverlaps3D(m_Rigidbody3D.position, m_SphereCollider3D.radius);
        }
    }



    ///======================================================================================================================================================



    //. 디버그



    [TitleGroup("디버그"), BoxGroup("디버그/박스", false)]
    [ShowInInspector, DisplayAsString, HideIf(nameof(is2D)), LabelText("마지막 히트(3D)")] public RaycastHit? LastHit3D { get; private set; }
    [TitleGroup("디버그"), BoxGroup("디버그/박스", false)]
    [ShowInInspector, DisplayAsString, ShowIf(nameof(is2D)), LabelText("마지막 히트(2D)")] public RaycastHit2D? LastHit2D { get; private set; }



    ///======================================================================================================================================================



    //. 물리 업데이트 이벤트



    public event Action<PhysicsMiniObject> OnPreSimulationEvent;
    public event Action<PhysicsMiniObject> OnPostSimulationEvent;
    public event Action<UnityEngine.Object> OnPreSimulationObjectEvent;
    public event Action<UnityEngine.Object> OnPostSimulationObjectEvent;



    ///======================================================================================================================================================



    //. 물리 상호작용 이벤트



    public event Action<PhysicsMiniObject, RaycastHit> OnHit3D;
    public event Action<PhysicsMiniObject, RaycastHit2D> OnHit2D;



    ///======================================================================================================================================================



    //. 타임



#if UNITY_EDITOR
    [TitleGroup("타임"), BoxGroup("타임/박스", false)]
    [ShowInInspector, HideLabel, DisplayAsString(EnableRichText = true, Overflow = false), EnableGUI]
    private string editorTimeInfo
    {
        get
        {
            float ts = TimeScaling != null ? TimeScaling.Invoke() : 1f;
            return $"TimeScale : {ts}";
        }
    }
#endif



    public Func<float> TimeScaling { get; set; } = null;



    ///======================================================================================================================================================



    private void Awake()
    {
        //. 모드에 맞게 확보
        bool ok = is2D ? EnsureComponents2D() : EnsureComponents3D();
        if (!ok) { enabled = false; return; }

        //. 시작 좌표 동기화
        Vector3 start = transform.position;
        if (is2D) { m_Rigidbody2D.position = (Vector2)start; m_CircleCollider2D.radius = Mathf.Max(0f, initialBodyRadius); }
        else { m_Rigidbody3D.position = start; m_SphereCollider3D.radius = Mathf.Max(0f, initialBodyRadius); }

        //. 이동 에이전트
        MovementAgentScaled = new ScaledVelocityAgentMini(this);
        MovementAgentUnScaled = new UnScaledVelocityAgentMini(this);

        //. 초기 상태
        PreviousFramePhysicsPosition = PhysicsPosition;
        ExpectedVelocity = Vector3.zero;
        ActualVelocity = Vector3.zero;

        //. 스폰 겹침 보정
        if (depenetrateOnAwake)
        {
            if (is2D && Has2D()) ResolveOverlaps2D(m_Rigidbody2D.position, m_CircleCollider2D.radius);
            else if (!is2D && Has3D()) ResolveOverlaps3D(m_Rigidbody3D.position, m_SphereCollider3D.radius);
        }
    }

    private void OnDisable()
    {
        TimeScaling = null;
    }

    private void OnDestroy()
    {
        MovementAgentScaled = null;
        MovementAgentUnScaled = null;
    }



    ///======================================================================================================================================================



    //. 물리 업데이트



    private void FixedUpdate()
    {
        LastHit3D = null; LastHit2D = null;

        float dt = Time.fixedDeltaTime;
        var preA = OnPreSimulationEvent; var preO = OnPreSimulationObjectEvent;
        var postA = OnPostSimulationEvent; var postO = OnPostSimulationObjectEvent;

        //. 1) 이전 위치 기록
        PreviousFramePhysicsPosition = PhysicsPosition;

        //. 2) 외부 기여 수집 (펜딩 Δ 누적 구간)
        preA?.Invoke(this);
        preO?.Invoke(this);

        //. 3) Pending → Flush (Δ 누적을 willApplyDelta에 합산)
        MovementAgentScaled?.FlushPendingMovement();
        MovementAgentUnScaled?.FlushPendingMovement();

        //. 4) “예상 Velocity”는 이번 프레임에 소비할 Δ로부터 m/s 단위로 도출
        ExpectedVelocity = PhysicsVelocityUtility.FromDelta(willApplyDelta, dt);

        //. 5) Δ 적용 — 차폐/슬라이드 (Δ=한 프레임 이동량)
        if (willApplyDelta != Vector3.zero)
        {
            float contactOffset = Is2D ? Physics2D.defaultContactOffset : Physics.defaultContactOffset;
            float effSkin = Mathf.Max(skin, contactOffset);
            float effMinMove = Mathf.Max(minMove, contactOffset * 0.25f);

            if (is2D && Has2D())
            {
                Vector2 origin = m_Rigidbody2D.position;
                Vector2 disp = new Vector2(willApplyDelta.x, willApplyDelta.y); //. Δ 그대로 사용

                if (!enableCollision || disp.sqrMagnitude <= (effMinMove * effMinMove))
                {
                    m_Rigidbody2D.MovePosition(origin + disp);
                }
                else
                {
                    var target = SweepSlide2D_SubStepped(origin, disp, m_CircleCollider2D.radius, effSkin, blockingLayers, effMinMove);
                    m_Rigidbody2D.MovePosition(target);
                }
            }
            else if (!is2D && Has3D())
            {
                Vector3 origin = m_Rigidbody3D.position;
                Vector3 disp3 = willApplyDelta; //. Δ 그대로 사용

                if (!enableCollision || disp3.sqrMagnitude <= (effMinMove * effMinMove))
                {
                    m_Rigidbody3D.MovePosition(origin + disp3);
                }
                else
                {
                    var target3 = SweepSlide3D_SubStepped(origin, disp3, m_SphereCollider3D.radius, effSkin, blockingLayers, effMinMove);
                    m_Rigidbody3D.MovePosition(target3);
                }
            }

            //. 소모
            willApplyDelta = Vector3.zero;
        }

        //. 6) 실측 Velocity = (Δx / dt)
        var newPos = PhysicsPosition;
        var delta = newPos - PreviousFramePhysicsPosition;
        ActualVelocity = (dt > 0f) ? (delta / dt) : Vector3.zero;

        //. 7) 포스트 이벤트
        postA?.Invoke(this);
        postO?.Invoke(this);
    }



    ///======================================================================================================================================================



    //. 충돌 관련 메서드



    #region 충돌 관련 메서드

    //. Sweep / Slide 본체



    private static bool TryGetDir(Vector2 v, out Vector2 dir, out float dist) { dist = v.magnitude; if (dist > 1e-12f) { dir = v / dist; return true; } dir = Vector2.zero; return false; }
    private static bool TryGetDir(Vector3 v, out Vector3 dir, out float dist) { dist = v.magnitude; if (dist > 1e-12f) { dir = v / dist; return true; } dir = Vector3.zero; return false; }
    private static Vector2 ProjectOnPlane(Vector2 v, Vector2 n) => v - Vector2.Dot(v, n) * n;
    private static Vector3 ProjectOnPlane(Vector3 v, Vector3 n) => v - Vector3.Project(v, n);
    private static bool TooSmall(float sqr, float minMove) => sqr <= (minMove * minMove);

    private Vector2 SweepSlide2D(Vector2 origin, Vector2 disp, float radius, float skin, LayerMask layers, float minMove)
    {
        if (disp.sqrMagnitude <= minMove * minMove) return origin + disp;
        if (!TryGetDir(disp, out var dir, out var dist)) return origin + disp;

        //. 전방 1회 캐스트
        if (!TryFindNearest2D(origin, dir, radius, dist + skin, layers, out var hit1)) return origin + disp;

        LastHit2D = hit1; OnHit2D?.Invoke(this, hit1);
        float allowed = Mathf.Max(0f, hit1.distance - skin);
        Vector2 pos = origin + dir * Mathf.Min(dist, allowed);

        float remain = dist - allowed;
        if (remain <= minMove) return pos;

        int slides = Mathf.Max(0, slideIterations);
        Vector2 slide = ProjectOnPlane(dir * remain, hit1.normal);
        if (slide.sqrMagnitude <= minMove * minMove || slides == 0) return pos;

        while (slides-- > 0)
        {
            if (!TryGetDir(slide, out var dir2, out var dist2)) return pos;
            if (!TryFindNearest2D(pos, dir2, radius, dist2 + skin, layers, out var hit2)) return pos + slide;

            LastHit2D = hit2; OnHit2D?.Invoke(this, hit2);
            float allowed2 = Mathf.Max(0f, hit2.distance - skin);
            pos += dir2 * Mathf.Min(dist2, allowed2);

            float remain2 = dist2 - allowed2;
            if (remain2 <= minMove) return pos;

            slide = ProjectOnPlane(dir2 * remain2, hit2.normal);
            if (slide.sqrMagnitude <= minMove * minMove) return pos;
        }
        return pos;
    }

    private Vector3 SweepSlide3D(Vector3 origin, Vector3 disp, float radius, float skin, LayerMask layers, float minMove)
    {
        if (disp.sqrMagnitude <= minMove * minMove) return origin + disp;
        if (!TryGetDir(disp, out var dir, out var dist)) return origin + disp;

        //. 전방 1회 캐스트
        if (!TryFindNearest3D(origin, dir, radius, dist + skin, layers, out var hit1)) return origin + disp;

        LastHit3D = hit1; OnHit3D?.Invoke(this, hit1);
        float allowed = Mathf.Max(0f, hit1.distance - skin);
        Vector3 pos = origin + dir * Mathf.Min(dist, allowed);

        float remain = dist - allowed;
        if (remain <= minMove) return pos;

        int slides = Mathf.Max(0, slideIterations);
        Vector3 slide = ProjectOnPlane(dir * remain, hit1.normal);
        if (slide.sqrMagnitude <= minMove * minMove || slides == 0) return pos;

        while (slides-- > 0)
        {
            if (!TryGetDir(slide, out var dir2, out var dist2)) return pos;
            if (!TryFindNearest3D(pos, dir2, radius, dist2 + skin, layers, out var hit2)) return pos + slide;

            LastHit3D = hit2; OnHit3D?.Invoke(this, hit2);
            float allowed2 = Mathf.Max(0f, hit2.distance - skin);
            pos += dir2 * Mathf.Min(dist2, allowed2);

            float remain2 = dist2 - allowed2;
            if (remain2 <= minMove) return pos;

            slide = ProjectOnPlane(dir2 * remain2, hit2.normal);
            if (slide.sqrMagnitude <= minMove * minMove) return pos;
        }
        return pos;
    }

    private Vector2 SweepSlide2D_SubStepped(Vector2 origin, Vector2 totalDisp, float radius, float skin, LayerMask layers, float minMove)
    {
        if (maxSweepStep <= 0f || TooSmall(totalDisp.sqrMagnitude, minMove)) return SweepSlide2D(origin, totalDisp, radius, skin, layers, minMove);
        if (!TryGetDir(totalDisp, out var dir, out var remain)) return origin + totalDisp;

        Vector2 pos = origin;
        while (remain > 0f)
        {
            float step = Mathf.Min(remain, maxSweepStep);
            pos = SweepSlide2D(pos, dir * step, radius, skin, layers, minMove);
            remain -= step;
            if (remain <= minMove) break;
        }
        return pos;
    }

    private Vector3 SweepSlide3D_SubStepped(Vector3 origin, Vector3 totalDisp, float radius, float skin, LayerMask layers, float minMove)
    {
        if (maxSweepStep <= 0f || TooSmall(totalDisp.sqrMagnitude, minMove)) return SweepSlide3D(origin, totalDisp, radius, skin, layers, minMove);
        if (!TryGetDir(totalDisp, out var dir, out var remain)) return origin + totalDisp;

        Vector3 pos = origin;
        while (remain > 0f)
        {
            float step = Mathf.Min(remain, maxSweepStep);
            pos = SweepSlide3D(pos, dir * step, radius, skin, layers, minMove);
            remain -= step;
            if (remain <= minMove) break;
        }
        return pos;
    }



    ///======================================================================================================================================================



    //. Overlap Resolve (디페네트레이션)



    private void ResolveOverlaps3D(Vector3 center, float radius, int maxIters = 3)
    {
        var pool = ArrayPool<Collider>.Shared;
        int cap = Mathf.Max(8, castBufferSize);
        for (int iter = 0; iter < maxIters; iter++)
        {
            var cols = pool.Rent(cap);
            try
            {
                int cnt = Physics.OverlapSphereNonAlloc(center, radius, cols, blockingLayers,
                    collideTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore);
                if (cnt == 0) return;

                Vector3 total = Vector3.zero;
                for (int i = 0; i < cnt; i++)
                {
                    var c = cols[i];
                    if (!c) continue;
                    if (m_SphereCollider3D && c == m_SphereCollider3D) continue;

                    if (Physics.ComputePenetration(
                        m_SphereCollider3D, center, Quaternion.identity,
                        c, c.transform.position, c.transform.rotation,
                        out var dir, out var dist))
                    {
                        if (dist > 0f) total += dir * Mathf.Min(dist + skin * 0.5f, maxDepenetration);
                    }
                }

                if (total.sqrMagnitude <= 1e-10f) return;
                if (total.sqrMagnitude > (maxDepenetration * maxDepenetration)) total = total.normalized * maxDepenetration;

                center += total;
                m_Rigidbody3D.position = center;
            }
            finally { pool.Return(cols); }
            if (cap <= 16384) cap <<= 1;
        }
    }

    private void ResolveOverlaps2D(Vector2 center, float radius, int maxIters = 3)
    {
        var pool = ArrayPool<Collider2D>.Shared;
        int cap = Mathf.Max(8, castBufferSize);
        for (int iter = 0; iter < maxIters; iter++)
        {
            var cols = pool.Rent(cap);
            try
            {
                var filter = new ContactFilter2D();
                //. 레이어는 필터에서 처리
                filter.useLayerMask = true;
                filter.layerMask = blockingLayers;
                //. 트리거는 결과에 '포함'시켜 받아온 뒤, 아래 루프에서
                //. collideTriggers 옵션으로 수동 필터링합니다.
                filter.useTriggers = true;

                int cnt = Physics2D.OverlapCircle(center, radius, filter, cols);
                if (cnt == 0) return;

                Vector2 total = Vector2.zero;
                for (int i = 0; i < cnt; i++)
                {
                    var c = cols[i];
                    if (!c) continue;
                    if (c.isTrigger && !collideTriggers) continue;
                    if (m_CircleCollider2D && c == m_CircleCollider2D) continue;

                    var dist = Physics2D.Distance(c, m_CircleCollider2D);
                    if (dist.isOverlapped)
                    {
                        //. 상대→우리 방향, 깊이>0
                        var n = dist.normal;
                        float depth = -dist.distance;
                        total += n * Mathf.Min(depth + skin * 0.5f, maxDepenetration);
                    }
                }

                if (total.sqrMagnitude <= 1e-10f) return;
                if (total.sqrMagnitude > (maxDepenetration * maxDepenetration)) total = total.normalized * maxDepenetration;

                center += total;
                m_Rigidbody2D.position = center;
            }
            finally { pool.Return(cols); }
            if (cap <= 16384) cap <<= 1;
        }
    }



    ///======================================================================================================================================================



    //. NonAlloc 캐스트



    private bool TryFindNearest3D(Vector3 origin, Vector3 dir, float radius, float distance, LayerMask layers, out RaycastHit nearest)
    {
        var pool = ArrayPool<RaycastHit>.Shared;
        int cap = Mathf.Max(1, castBufferSize);
        nearest = default;

        for (int iter = 0; iter < 4; ++iter)
        {
            var buf = pool.Rent(cap);
            try
            {
                int count = Physics.SphereCastNonAlloc(origin, radius, dir, buf, distance, layers,
                    collideTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore);
                if (count == 0) return false;

                float best = float.PositiveInfinity; bool found = false;
                for (int i = 0; i < count; ++i)
                {
                    var h = buf[i];
                    if (!h.collider) continue;
                    if (m_SphereCollider3D && h.collider == m_SphereCollider3D) continue;
                    if (h.distance < best) { nearest = h; best = h.distance; found = true; }
                }
                if (found) return true;
                if (count >= cap) { cap <<= 1; castBufferSize = Mathf.Max(castBufferSize, cap); continue; }
                return false;
            }
            finally { pool.Return(buf); }
        }
        return false;
    }

    private bool TryFindNearest2D(Vector2 origin, Vector2 dir, float radius, float distance, LayerMask layers, out RaycastHit2D nearest)
    {
        var pool = ArrayPool<RaycastHit2D>.Shared;
        int cap = Mathf.Max(1, castBufferSize);
        nearest = default;

        for (int iter = 0; iter < 4; ++iter)
        {
            var buf = pool.Rent(cap);
            try
            {
                var filter = new ContactFilter2D();
                filter.useLayerMask = true;
                filter.layerMask = layers;
                //. 결과에 트리거도 포함시켜 받아온 뒤, 아래에서
                //. collideTriggers=false면 수동으로 거릅니다.
                filter.useTriggers = true;

                int count = Physics2D.CircleCast(origin, radius, dir, filter, buf, distance);
                if (count == 0) return false;

                float best = float.PositiveInfinity; bool found = false;
                for (int i = 0; i < count; ++i)
                {
                    var h = buf[i];
                    if (!h.collider) continue;
                    if (h.collider.isTrigger && !collideTriggers) continue;
                    if (m_CircleCollider2D && h.collider == m_CircleCollider2D) continue;
                    if (h.distance < best) { nearest = h; best = h.distance; found = true; }
                }
                if (found) return true;
                if (count >= cap) { cap <<= 1; castBufferSize = Mathf.Max(castBufferSize, cap); continue; }
                return false;
            }
            finally { pool.Return(buf); }
        }
        return false;
    }

    #endregion



    ///======================================================================================================================================================



    //. 기즈모



    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? PhysicsPosition : transform.position;

        //. 반지름
        float radius = Application.isPlaying
            ? (Is2D ? (m_CircleCollider2D ? m_CircleCollider2D.radius : initialBodyRadius)
                    : (m_SphereCollider3D ? m_SphereCollider3D.radius : initialBodyRadius))
            : initialBodyRadius;

        DrawWireCircleOrSphere(center, radius);

#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            var v = ActualVelocity;
            if (v.sqrMagnitude > 1e-12f)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(center, center + v * 0.1f);
                UnityEditor.Handles.ArrowHandleCap(0, center + v * 0.1f, Quaternion.LookRotation(v), 0.2f, EventType.Repaint);
                Gizmos.color = Color.white;
            }
        }
#endif
    }



    private void DrawWireCircleOrSphere(Vector3 center, float radius)
    {
        if (radius <= 0f) return;
        var prev = Gizmos.color;
        Gizmos.color = Color.white;

        if (Is2D)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.DrawWireDisc(center, Vector3.forward, radius);
#else
            Gizmos.DrawWireSphere(center, radius);
#endif
        }
        else Gizmos.DrawWireSphere(center, radius);

        Gizmos.color = prev;
    }



    ///======================================================================================================================================================
}
