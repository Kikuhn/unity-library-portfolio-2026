using Lightbug.CharacterControllerPro.Core;
using UnityEngine;
using Pan.Util.PhysicsAgent;
using Sirenix.OdinInspector;
using System.Runtime.CompilerServices;
using System;
using Pan.Util.Game;
using Pan.Util;



public interface ICCPObject
{
    CharacterActor CharacterActor { get; }
    CharacterBody CharacterBody { get; }
    Vector2 BodySizeVector2 { get; set; }
    Vector2 DefaultBodySizeVector2 { get; }
    bool IsCharacterColliderEnabled { get; }
    bool TrySetBodySizeVector2(Vector2 value, CharacterActor.SizeReferenceType sizeReferenceType = CharacterActor.SizeReferenceType.Bottom);
    bool TryInterpolateBodySizeVector2(Vector2 targetSize, float lerpFactor, CharacterActor.SizeReferenceType sizeReferenceType = CharacterActor.SizeReferenceType.Bottom);
}



internal static class PhysicsVelocityUtility
{
    internal static Vector3 FromDelta(Vector3 delta, float deltaTime)
    {
        return deltaTime > 0f ? delta / deltaTime : Vector3.zero;
    }
}



[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterActor), typeof(CharacterBody))]
public class CCPObject : MonoBehaviour,
    ICCPObject,
    IDimensionPhysicsAgent,
    IHoldMovementScaledPhysicsAgentCore,
    IHoldMovementUnScaledPhysicsAgentCore,
    IPhysicsPositionPhysicsAgent,
    IInstantMovePositionPhysicsAgent,
    IMovingPhysicsAgent,
    IExpectedVelocityPhysicsAgent,
    IActualVelocityPhysicsAgent,
    IPreviousFramePhysicsPositionPhysicsAgent,
    IGroundedPhysicsAgent,
    IStableGroundedPhysicsAgent,
    IBodySizeVector2PhysicsAgent,
    IMassPhysicsAgent,
    IPhysicsUpdatePreSimulationPhysicsAgent<CCPObject>, IPhysicsUpdatePostSimulationPhysicsAgent<CCPObject>,
    IGroundedPhysicsAgentEvents<CCPObject>, IStableGroundedPhysicsAgentEvents<CCPObject>,
    IForceNotGroundedPhysicsAgent,
    IOneWayPlatformsLayerMaskPhysicsAgent, IStableGroundAnglePhysicsAgent, IStableGroundLayerMaskPhysicsAgent, IDynamicGroundLayerMaskPhysicsAgent, IPushableRigidbodyLayerMaskPhysicsAgent,
    IStepUpDistancePhysicsAgent, IStepDownDistancePhysicsAgent,
    IStandardTopPositionPhysicsAgent,
    IStandardFrontPositionPhysicsAgent,
    ITimeScaling
{
    ///======================================================================================================================================================



#if UNITY_EDITOR



    [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Center, EnableRichText = true, Overflow = false), EnableGUI]
    [PropertyOrder(-10)]
    private string editor_Title
    {
        get
        {
            return $"<b><size=14>CharacterControllerProObject</size></b>\n<i><size=12>Scale 조정 금지 (1,1,1)</size></i>";
        }
    }


    private static readonly Color editorMovingColor = new Color(0.31f, 0.76f, 0.91f, 1f);

    [NonSerialized] private int editorMovementSnapshotFrame = -1;
    [NonSerialized] private Vector3 editorMovementSnapshotExpectedVelocity;
    [NonSerialized] private Vector3 editorMovementSnapshotActualVelocity;
    [NonSerialized] private bool editorSnapshot_IsMovingExpected;
    [NonSerialized] private bool editorSnapshot_IsMovingExpectedX;
    [NonSerialized] private bool editorSnapshot_IsMovingExpectedY;
    [NonSerialized] private bool editorSnapshot_IsMovingExpectedZ;
    [NonSerialized] private bool editorSnapshot_IsMovingActual;
    [NonSerialized] private bool editorSnapshot_IsMovingActualX;
    [NonSerialized] private bool editorSnapshot_IsMovingActualY;
    [NonSerialized] private bool editorSnapshot_IsMovingActualZ;

    [NonSerialized] private int editorTimeInfoFrame = -1;
    [NonSerialized] private bool editorCachedTimeScalingEnabled;
    [NonSerialized] private float editorCachedTimeScale;
    [NonSerialized] private string editorCachedTimeInfo;


    /// <summary>
    /// Odin이 값과 색상을 각각 평가하더라도 같은 게임 프레임의 이동 상태는 한 번만 계산합니다.
    /// </summary>
    private void Editor_RefreshMovementSnapshot()
    {
        int currentFrame = Time.frameCount;
        Vector3 expectedVelocity = ExpectedVelocity;
        Vector3 actualVelocity = ActualVelocity;

        if (editorMovementSnapshotFrame == currentFrame &&
            editorMovementSnapshotExpectedVelocity == expectedVelocity &&
            editorMovementSnapshotActualVelocity == actualVelocity)
        {
            return;
        }

        editorMovementSnapshotFrame = currentFrame;
        editorMovementSnapshotExpectedVelocity = expectedVelocity;
        editorMovementSnapshotActualVelocity = actualVelocity;

        editorSnapshot_IsMovingExpected = this.IsMovingExpected(true);
        editorSnapshot_IsMovingExpectedX = this.IsMovingExpectedX(true);
        editorSnapshot_IsMovingExpectedY = this.IsMovingExpectedY(true);
        editorSnapshot_IsMovingExpectedZ = this.IsMovingExpectedZ(true);
        editorSnapshot_IsMovingActual = this.IsMovingActual(true);
        editorSnapshot_IsMovingActualX = this.IsMovingActualX(true);
        editorSnapshot_IsMovingActualY = this.IsMovingActualY(true);
        editorSnapshot_IsMovingActualZ = this.IsMovingActualZ(true);
    }


#endif



    ///======================================================================================================================================================



    //. CCP 컴포넌트



    [TitleGroup("컴포넌트"), FoldoutGroup("컴포넌트/박스", false)]
    [SerializeField] protected CharacterActor characterActor;
    protected CharacterActor CharacterActor
    {
        get
        {
            if (characterActor == null) { characterActor = GetComponent<CharacterActor>(); }
            return characterActor;
        }
    }
    CharacterActor ICCPObject.CharacterActor => CharacterActor;


    [TitleGroup("컴포넌트"), FoldoutGroup("컴포넌트/박스", false)]
    [SerializeField] protected CharacterBody characterBody;
    protected CharacterBody CharacterBody
    {
        get
        {
            if (characterBody == null) { characterBody = GetComponent<CharacterBody>(); }
            return characterBody;
        }
    }

    CharacterBody ICCPObject.CharacterBody => CharacterBody;



    ///======================================================================================================================================================



    //. 물리 컴포넌트



    [TitleGroup("컴포넌트"), FoldoutGroup("컴포넌트/박스", false)]
    [SerializeField]
    [Sirenix.OdinInspector.ReadOnly]
    [DisableIf(nameof(Is2D))]
    protected Rigidbody m_Rigidbody;
    protected Rigidbody Rigidbody
    {
        get
        {
            if (m_Rigidbody == null) { m_Rigidbody = GetComponent<Rigidbody>(); }
            return m_Rigidbody;
        }
    }



    [TitleGroup("컴포넌트"), FoldoutGroup("컴포넌트/박스", false)]
    [SerializeField]
    [Sirenix.OdinInspector.ReadOnly] protected Rigidbody2D m_Rigidbody2D;
    [ShowIf(nameof(Is2D))]
    protected Rigidbody2D Rigidbody2D
    {
        get
        {
            if (m_Rigidbody2D == null) { m_Rigidbody2D = GetComponent<Rigidbody2D>(); }
            return m_Rigidbody2D;
        }
    }



#if UNITY_EDITOR

    private bool editor_IsReadyPhysicsAgent
    {
        get
        {
            if (!isAwaken) { return false; }
            if ((!Is2D && Rigidbody == null) || (Is2D && Rigidbody2D == null))
            {
                return false;
            }
            return true;
        }
    }

#endif



    ///======================================================================================================================================================



    //. 좌표



    [FoldoutGroup("실시간 진단", false)]
    [TitleGroup("실시간 진단/좌표"), BoxGroup("실시간 진단/좌표/박스", false)]
    [ShowInInspector]
    [LabelText("물리 좌표")]
    [PropertyTooltip("PhysicsPosition\n에디터에서도 이 값을 조정하여 실시간 좌표 조정 가능\n런타임이 아닐땐, transform.position가 보여진다")]
#if UNITY_EDITOR
    [EnableIf(nameof(editor_IsReadyPhysicsAgent))]
#endif
    ///<inheritdoc/>
    public Vector3 PhysicsPosition
    {
        get
        {
#if UNITY_EDITOR
            if (!editor_IsReadyPhysicsAgent) { return transform.position; }
#endif
            return CharacterActor.Position;
        }
        set => CharacterActor.Position = value;
    }



    [FoldoutGroup("실시간 진단", false)]
    [TitleGroup("실시간 진단/좌표"), BoxGroup("실시간 진단/좌표/박스", false)]
    [ShowInInspector, DisplayAsString]
    [LabelText("이전 프레임 물리 좌표")]
    [PropertyTooltip("PreviousFramePhysicsPosition")]
    ///<inheritdoc/>
    public Vector3 PreviousFramePhysicsPosition { get; protected set; }



    ///======================================================================================================================================================



    //. 이동



    [FoldoutGroup("실시간 진단", false)]
    [TitleGroup("실시간 진단/이동"), BoxGroup("실시간 진단/이동/박스", false)]
    [ShowInInspector, DisplayAsString]
    [LabelText("예상 Velocity")]
    [PropertyTooltip("ExpectedVelocity")]
    ///<inheritdoc/>
    public Vector3 ExpectedVelocity { get; protected set; } = Vector3.zero;



    [FoldoutGroup("실시간 진단", false)]
    [TitleGroup("실시간 진단/이동"), BoxGroup("실시간 진단/이동/박스", false)]
    [ShowInInspector, DisplayAsString]
    [LabelText("실제 Velocity")]
    [PropertyTooltip("ActualVelocity")]
    ///<inheritdoc/>
    public Vector3 ActualVelocity { get; protected set; } = Vector3.zero;



#if UNITY_EDITOR



    [FoldoutGroup("실시간 진단", false)]
    [TitleGroup("실시간 진단/이동"), BoxGroup("실시간 진단/이동/박스", false)]
    [ShowInInspector, DisplayAsString, EnableGUI]
    [LabelText("예상 이동 여부")]
    [PropertyTooltip("IsMovingExpected (deadzone O)")]
    [GUIColor(nameof(editorColor_IsMovingExpected))]
    private bool editor_IsMovingExpected
    {
        get
        {
            Editor_RefreshMovementSnapshot();
            return editorSnapshot_IsMovingExpected;
        }
    }


    [FoldoutGroup("실시간 진단", false)]
    [TitleGroup("실시간 진단/이동"), BoxGroup("실시간 진단/이동/박스", false)]
    [HorizontalGroup("실시간 진단/이동/박스/예상축별가로", Width = 0.333f)]
    [ShowInInspector, DisplayAsString, EnableGUI]
    [LabelText("X")]
    [PropertyTooltip("IsMovingExpectedX (deadzone O)")]
    [GUIColor(nameof(editorColor_IsMovingExpectedX))]
    [Indent(1)]
    private bool editor_IsMovingExpectedX
    {
        get
        {
            Editor_RefreshMovementSnapshot();
            return editorSnapshot_IsMovingExpectedX;
        }
    }

    [HorizontalGroup("실시간 진단/이동/박스/예상축별가로", Width = 0.333f)]
    [ShowInInspector, DisplayAsString, EnableGUI]
    [LabelText("Y")]
    [PropertyTooltip("IsMovingExpectedY (deadzone O)")]
    [GUIColor(nameof(editorColor_IsMovingExpectedY))]
    private bool editor_IsMovingExpectedY
    {
        get
        {
            Editor_RefreshMovementSnapshot();
            return editorSnapshot_IsMovingExpectedY;
        }
    }

    [HorizontalGroup("실시간 진단/이동/박스/예상축별가로", Width = 0.334f)]
    [ShowInInspector, DisplayAsString, EnableGUI]
    [LabelText("Z")]
    [PropertyTooltip("IsMovingExpectedZ (deadzone O)")]
    [GUIColor(nameof(editorColor_IsMovingExpectedZ))]
    private bool editor_IsMovingExpectedZ
    {
        get
        {
            Editor_RefreshMovementSnapshot();
            return editorSnapshot_IsMovingExpectedZ;
        }
    }

    #region Color
    private Color editorColor_IsMovingExpected => editor_IsMovingExpected ? editorMovingColor : Color.white;
    private Color editorColor_IsMovingExpectedX => editor_IsMovingExpectedX ? editorMovingColor : Color.white;
    private Color editorColor_IsMovingExpectedY => editor_IsMovingExpectedY ? editorMovingColor : Color.white;
    private Color editorColor_IsMovingExpectedZ => editor_IsMovingExpectedZ ? editorMovingColor : Color.white;
    #endregion



    [FoldoutGroup("실시간 진단", false)]
    [TitleGroup("실시간 진단/이동"), BoxGroup("실시간 진단/이동/박스", false)]
    [ShowInInspector, DisplayAsString, EnableGUI]
    [LabelText("실측 이동 여부")]
    [PropertyTooltip("IsMovingActual (deadzone O)")]
    [GUIColor(nameof(editorColor_IsMovingActual))]
    private bool editor_IsMovingActual
    {
        get
        {
            Editor_RefreshMovementSnapshot();
            return editorSnapshot_IsMovingActual;
        }
    }


    //. 축별(실측)
    [FoldoutGroup("실시간 진단", false)]
    [TitleGroup("실시간 진단/이동"), BoxGroup("실시간 진단/이동/박스", false)]
    [HorizontalGroup("실시간 진단/이동/박스/실측축별가로", Width = 0.333f)]
    [ShowInInspector, DisplayAsString, EnableGUI]
    [LabelText("X")]
    [PropertyTooltip("IsMovingActualX (deadzone O)")]
    [GUIColor(nameof(editorColor_IsMovingActualX))]
    [Indent(1)]
    private bool editor_IsMovingActualX
    {
        get
        {
            Editor_RefreshMovementSnapshot();
            return editorSnapshot_IsMovingActualX;
        }
    }

    [HorizontalGroup("실시간 진단/이동/박스/실측축별가로", Width = 0.333f)]
    [ShowInInspector, DisplayAsString, EnableGUI]
    [LabelText("Y")]
    [PropertyTooltip("IsMovingActualY (deadzone O)")]
    [GUIColor(nameof(editorColor_IsMovingActualY))]
    private bool editor_IsMovingActualY
    {
        get
        {
            Editor_RefreshMovementSnapshot();
            return editorSnapshot_IsMovingActualY;
        }
    }

    [HorizontalGroup("실시간 진단/이동/박스/실측축별가로", Width = 0.334f)]
    [ShowInInspector, DisplayAsString, EnableGUI]
    [LabelText("Z")]
    [PropertyTooltip("IsMovingActualZ (deadzone O)")]
    [GUIColor(nameof(editorColor_IsMovingActualZ))]
    private bool editor_IsMovingActualZ
    {
        get
        {
            Editor_RefreshMovementSnapshot();
            return editorSnapshot_IsMovingActualZ;
        }
    }

    #region Color
    private Color editorColor_IsMovingActual => editor_IsMovingActual ? editorMovingColor : Color.white;
    private Color editorColor_IsMovingActualX => editor_IsMovingActualX ? editorMovingColor : Color.white;
    private Color editorColor_IsMovingActualY => editor_IsMovingActualY ? editorMovingColor : Color.white;
    private Color editorColor_IsMovingActualZ => editor_IsMovingActualZ ? editorMovingColor : Color.white;

    #endregion



#endif



    /// <summary>
    /// 즉시 이동
    /// <para><see cref="CharacterActor.Position"/>에 <paramref name="velocity"/>를 더한다</para>
    /// </summary>
    public void ApplyInstanceMovement_Velocity(Vector3 velocity)
    {
        CharacterActor.Position += velocity;
        //. CCP 보간을 중단하지 않는다
    }



    /// <summary>
    /// 즉시 좌표 이동 (텔레포트)
    ///<para><see cref="CharacterActor.Teleport"/>를 호출한다</para>
    /// </summary>
    public void ApplyInstanceMovement_Position(Vector3 position)
    {
        CharacterActor.Teleport(position);
        //. CCP 보간을 중단하며, OnTeleport 이벤트가 호출된다
    }



    [FoldoutGroup("실시간 진단", false)]
    [TitleGroup("실시간 진단/이동"), BoxGroup("실시간 진단/이동/박스", false)]
    [ShowInInspector, DisplayAsString]
    [LabelText("적용 예정 Velocity")]
    [PropertyTooltip("WillApplyVelocity, 다음 물리 프레임에 이 속도를 모두 적용한뒤 초기화됨")]
    ///<inheritdoc/>
    public Vector3 WillApplyVelocity => willApplyVelocity;
    protected Vector3 willApplyVelocity = Vector3.zero;



    [FoldoutGroup("실시간 진단", false)]
    [TitleGroup("실시간 진단/이동"), BoxGroup("실시간 진단/이동/박스", false)]
    [ShowInInspector, DisplayAsString]
    [LabelText("CharacterActor Velocity")]
    [PropertyTooltip("캐릭터액터 내의 Velocity 보기")]
    public Vector3 CharacterActorVelocity
    {
        get => (characterActor != null && characterActor.RigidbodyComponent != null) ? characterActor.Velocity : Vector3.zero;
    }



    ///======================================================================================================================================================



    //. 상태



    [FoldoutGroup("실시간 진단", false)]
    [TitleGroup("실시간 진단/상태"), BoxGroup("실시간 진단/상태/박스", false)]
    [ShowInInspector, DisplayAsString, EnableGUI]
    [LabelText("접지 상태")]
    ///<inheritdoc/>
    public bool IsGrounded => characterActor.IsGrounded;



    [FoldoutGroup("실시간 진단", false)]
    [TitleGroup("실시간 진단/상태"), BoxGroup("실시간 진단/상태/박스", false)]
    [ShowInInspector, DisplayAsString, EnableGUI]
    [LabelText("안정적인 접지 상태")]
    ///<inheritdoc/>
    public bool IsStablyGrounded => characterActor.IsStable;



    ///======================================================================================================================================================



    //. 바디



    [TitleGroup("바디"), BoxGroup("바디/박스", false)]
    [ShowInInspector]
    [LabelText("2D 모드")]
#if UNITY_EDITOR
    [DisableIf(nameof(isAwaken))]
#endif
    public bool Is2D
    {
        get
        {
            return characterBody.is2D;
        }
        set
        {
            if (!isAwaken)
            {
                characterBody.is2D = value;
            }
        }
    }



    [TitleGroup("바디"), BoxGroup("바디/박스", false)]
    [ShowInInspector]
    [LabelText("바디 크기")]
    [PropertyTooltip("CCP 1.4.15 기준 현재 바디 크기입니다. 런타임에서는 CharacterActor의 size 검증 API를 통해 변경합니다.")]
    ///<inheritdoc/>
    public Vector2 BodySizeVector2
    {
        get => isAwaken ? CharacterActor.BodySize : CharacterBody.BodySize;
        set => TrySetBodySizeVector2(value);
    }


    [TitleGroup("바디"), BoxGroup("바디/박스", false)]
    [ShowInInspector, DisplayAsString]
    [LabelText("기본 바디 크기")]
    [PropertyTooltip("런타임 Awake 시점의 CharacterBody.BodySize입니다. CCP 1.4.15부터 BodySize는 현재 크기이므로 기본 크기와 구분합니다.")]
    public Vector2 DefaultBodySizeVector2 => isAwaken ? CharacterActor.DefaultBodySize : CharacterBody.BodySize;


    [TitleGroup("바디"), BoxGroup("바디/박스", false)]
    [ShowInInspector, DisplayAsString]
    [LabelText("Collider 활성")]
    [PropertyTooltip("CCP 1.4.14에서 추가된 CharacterActor.IsColliderEnabled 기반 상태입니다.")]
    public bool IsCharacterColliderEnabled => !isAwaken || CharacterActor.IsColliderEnabled;


    public bool TrySetBodySizeVector2(Vector2 value, CharacterActor.SizeReferenceType sizeReferenceType = CharacterActor.SizeReferenceType.Bottom)
    {
        if (!isAwaken)
        {
            CharacterBody.BodySize = value;
            return true;
        }

        return CharacterActor.CheckAndSetSize(value, sizeReferenceType);
    }


    public bool TryInterpolateBodySizeVector2(Vector2 targetSize, float lerpFactor, CharacterActor.SizeReferenceType sizeReferenceType = CharacterActor.SizeReferenceType.Bottom)
    {
        if (!isAwaken)
        {
            CharacterBody.BodySize = targetSize;
            return true;
        }

        return CharacterActor.CheckAndInterpolateSize(targetSize, lerpFactor, sizeReferenceType);
    }



    [TitleGroup("바디"), BoxGroup("바디/박스", false)]
    [ShowInInspector]
    [LabelText("질량")]
    [PropertyTooltip("런타임중에는, CharacterActor.RigidbodyComponent.Mass를 조정합니다\n에디터에선 CharacterBody.mass를 조정합니다")]
    ///<inheritdoc/>
    public float Mass
    {
        get => (isAwaken) ? CharacterActor.RigidbodyComponent.Mass : CharacterBody.Mass;
        set
        {
            if (isAwaken)
            {
                CharacterActor.RigidbodyComponent.Mass = value;
            }
            else
            {
                //! 강제 노출
                CharacterBody.mass = value;
            }
        }
    }



    ///======================================================================================================================================================



    //. 이동 에이전트



    ///<inheritdoc/>
    public ScaledVelocityAgentCCP MovementAgentScaled { get; protected set; }
    ///<inheritdoc/>
    public UnScaledVelocityAgentCCP MovementAgentUnScaled { get; protected set; }



    #region 인터페이스

    MovementPhysicsAgentCore<Vector3, Vector3Ops> IHoldMovementScaledPhysicsAgentCore.MovementAgentScaled => MovementAgentScaled;
    MovementPhysicsAgentCore<Vector3, Vector3Ops> IHoldMovementUnScaledPhysicsAgentCore.MovementAgentUnScaled => MovementAgentUnScaled;

    #endregion



    #region 이동 에이전트 클래스

    /// <summary>
    /// Pending으로 누적된 "목표 속도"를 CharacterControllerPro의 Velocity에 반영한다.
    /// </summary>
    public sealed class ScaledVelocityAgentCCP : MovementPhysicsAgent_Pending<CCPObject>
    {
        public ScaledVelocityAgentCCP(CCPObject main) : base(main) { }

        /// <inheritdoc />
        protected override void ApplyMovement(in Vector3 delta)
        {
            float timeScale = Main.GetTimeScale();
            Main.willApplyVelocity += delta * timeScale;
        }
    }


    /// <summary>
    /// Pending으로 누적된 "목표 속도"를 CharacterControllerPro의 Velocity에 반영한다.
    /// </summary>
    public sealed class UnScaledVelocityAgentCCP : MovementPhysicsAgent_Pending<CCPObject>
    {
        public UnScaledVelocityAgentCCP(CCPObject main) : base(main) { }

        /// <inheritdoc />
        protected override void ApplyMovement(in Vector3 delta)
        {
            Main.willApplyVelocity += delta;
        }
    }

    #endregion



    ///======================================================================================================================================================



    [TitleGroup("확장"), BoxGroup("확장/박스", false)]
    [LabelText("일방통행 플랫폼 레이어 마스크")]
    [ShowInInspector]
    ///<inheritdoc/>
    public LayerMask OneWayPlatformLayerMask
    {
        get => CharacterActor.oneWayPlatformsLayerMask;
        set => CharacterActor.oneWayPlatformsLayerMask = value;
    }



    [TitleGroup("확장"), BoxGroup("확장/박스", false)]
    [LabelText("안정 지면 경사 각도")]
    [ShowInInspector]
    [PropertyRange(1f, 89f)]
    ///<inheritdoc/>
    public float StableGroundSlopeAngle { get => characterActor.slopeLimit; set => characterActor.slopeLimit = Mathf.Clamp(value, 1f, 89f); }



    [TitleGroup("확장"), BoxGroup("확장/박스", false)]
    [LabelText("안정 지면 레이어 마스크")]
    [ShowInInspector]
    ///<inheritdoc/>
    public LayerMask StableGroundLayerMask { get => CharacterActor.stableLayerMask; set => CharacterActor.stableLayerMask = value; }


    [TitleGroup("확장"), BoxGroup("확장/박스", false)]
    [LabelText("다이나믹 지면 레이어 마스크 사용")]
    [ShowInInspector]
    ///<inheritdoc/>
    public bool UseDynamicGroundLayerMask { get => CharacterActor.supportDynamicGround; set => CharacterActor.supportDynamicGround = value; }

    [TitleGroup("확장"), BoxGroup("확장/박스", false)]
    [LabelText("다이나믹 지면 레이어 마스크")]
    [ShowInInspector]
    [Indent(1)]
    ///<inheritdoc/>
    public LayerMask DynamicGroundLayerMask { get => CharacterActor.dynamicGroundLayerMask; set => CharacterActor.dynamicGroundLayerMask = value; }



    [TitleGroup("확장"), BoxGroup("확장/박스", false)]
    [LabelText("Pushable Rigidbody 사용")]
    [ShowInInspector]
    ///<inheritdoc/>
    public bool UsePushableRigidbodyLayerMask { get => CharacterActor.canPushDynamicRigidbodies; set => CharacterActor.canPushDynamicRigidbodies = value; }

    [TitleGroup("확장"), BoxGroup("확장/박스", false)]
    [LabelText("Pushable Rigidbody 레이어 마스크")]
    [ShowInInspector]
    [Indent(1)]
    ///<inheritdoc/>
    public LayerMask PushableRigidbodyLayerMask { get => CharacterActor.pushableRigidbodyLayerMask; set => CharacterActor.pushableRigidbodyLayerMask = value; }



    [TitleGroup("확장"), BoxGroup("확장/박스", false)]
    [LabelText("지면 오르기 스텝 보정 높이")]
    [ShowInInspector]
    public float StepUpDistance { get => CharacterActor.stepUpDistance; set => CharacterActor.stepUpDistance = value; }

    [TitleGroup("확장"), BoxGroup("확장/박스", false)]
    [LabelText("지면 내려가기 스텝 보정 높이")]
    [ShowInInspector]
    public float StepDownDistance { get => CharacterActor.stepDownDistance; set => CharacterActor.stepDownDistance = value; }



    [TitleGroup("확장"), BoxGroup("확장/박스", false)]
    [LabelText("상단 기준 방향 벡터")]
    [ShowInInspector]
    public Vector3 StandardTopDirection
    {
        get
        {
            return _standardTopDirection;
        }
        set
        {
            _standardTopDirection = value;
            if (isAwaken)
            {
                CharacterActor.Up = _standardTopDirection;
            }
        }
    }
    [SerializeField, HideInInspector] private Vector3 _standardTopDirection;



    [TitleGroup("확장"), BoxGroup("확장/박스", false)]
    [LabelText("전방 기준 방향 벡터")]
    [ShowInInspector]
    public Vector3 StandardFrontDirection
    {
        get
        {
            return _standardFrontDirection;
        }
        set
        {
            _standardFrontDirection = value;
            if (isAwaken)
            {
                CharacterActor.Forward = _standardFrontDirection;
            }
        }
    }
    [SerializeField, HideInInspector] private Vector3 _standardFrontDirection;



    ///======================================================================================================================================================



    //. 물리 업데이트 이벤트



    public event Action<CCPObject> OnPreSimulationEvent;
    public event Action<CCPObject> OnPostSimulationEvent;
    public event Action<UnityEngine.Object> OnPreSimulationObjectEvent;
    public event Action<UnityEngine.Object> OnPostSimulationObjectEvent;



    ///======================================================================================================================================================



    //. 물리 상호작용 이벤트



    public event Action<CCPObject> OnEnterGroundedEvent;
    public event Action<UnityEngine.Object> OnEnterGroundedObjectEvent;

    public event Action<CCPObject> OnExitGroundedEvent;
    public event Action<UnityEngine.Object> OnExitGroundedObjectEvent;

    public event Action<CCPObject> OnEnterStableGroundedEvent;
    public event Action<UnityEngine.Object> OnEnterStableGroundedObjectEvent;

    public event Action<CCPObject> OnExitStableGroundedEvent;
    public event Action<UnityEngine.Object> OnExitStableGroundedObjectEvent;



    ///======================================================================================================================================================



    //. 타임



#if UNITY_EDITOR

    [FoldoutGroup("실시간 진단", false)]
    [TitleGroup("실시간 진단/타임"), BoxGroup("실시간 진단/타임/박스", false)]
    [ShowInInspector, HideLabel, DisplayAsString(EnableRichText = true, Overflow = false), EnableGUI]
    private string editorTimeInfo
    {
        get
        {
            int currentFrame = Time.frameCount;
            if (editorTimeInfoFrame != currentFrame)
            {
                editorTimeInfoFrame = currentFrame;
                bool timeScalingEnabled = TimeScaling != null;
                float timeScale = timeScalingEnabled ? TimeScaling.Invoke() : this.GetTimeScale();

                if (editorCachedTimeInfo == null ||
                    editorCachedTimeScalingEnabled != timeScalingEnabled ||
                    !editorCachedTimeScale.Equals(timeScale))
                {
                    editorCachedTimeScalingEnabled = timeScalingEnabled;
                    editorCachedTimeScale = timeScale;
                    editorCachedTimeInfo = timeScalingEnabled
                        ? $"TimeScale Func Enable: {timeScale}"
                        : $"TimeScale Func Disable: {timeScale}";
                }
            }

            return editorCachedTimeInfo;
        }
    }

#endif



    ///<inheritdoc/>
    public Func<float> TimeScaling { get; set; } = null;



    ///======================================================================================================================================================



    //. 업데이트 집행자



    [FoldoutGroup("실시간 진단", false)]
    [TitleGroup("실시간 진단/업데이터"), BoxGroup("실시간 진단/업데이터/박스", false)]
    [LabelText("물리 업데이트 집행자")]
    [SerializeField]
    private CustomUpdateExecutor_FixedUpdate<CCPObject> fixedUpdateExecutor;
    public CustomUpdateExecutor_FixedUpdate<CCPObject> FixedUpdateExecutor => fixedUpdateExecutor;



    ///======================================================================================================================================================



    [NonSerialized] private bool isAwaken;



    ///======================================================================================================================================================



    //. 초기화



    protected virtual void Awake()
    {
        isAwaken = true;

        characterActor = GetComponent<CharacterActor>();
        characterBody = GetComponent<CharacterBody>();

        MovementAgentScaled = new ScaledVelocityAgentCCP(this);
        MovementAgentUnScaled = new UnScaledVelocityAgentCCP(this);

        CharacterActor.OnPreSimulation += OnPreSimulation;
        CharacterActor.OnPostSimulation += OnPostSimulation;
        CharacterActor.OnGroundedStateEnter += OnGroundedStateEnter;
        CharacterActor.OnGroundedStateExit += OnGroundedStateExit;
        CharacterActor.OnStableStateEnter += OnStableStateEnter;
        CharacterActor.OnStableStateExit += OnStableStateExit;

        fixedUpdateExecutor = new CustomUpdateExecutor_FixedUpdate<CCPObject>(this, CurrentFixedUpdate);
    }



    protected virtual void Start()
    {
        //. Actor에 갱신하기 위한 1회 호출
        //! (Awake에서 호출하면, 아직 RigidodyComponent가 준비되지 않은 상태일 수 있음)
        StandardTopDirection = _standardTopDirection;
        StandardFrontDirection = _standardFrontDirection;
    }



    protected virtual void OnEnable()
    {
        FixedUpdateExecutor.ExecuteUpdate();
    }



    protected virtual void OnDisable()
    {
        FixedUpdateExecutor.QuitUpdate(false);
        TimeScaling = null;
    }



    protected virtual void OnDestroy()
    {
        FixedUpdateExecutor.QuitUpdate(true);

        if (CharacterActor != null)
        {
            CharacterActor.OnPreSimulation -= OnPreSimulation;
            CharacterActor.OnPostSimulation -= OnPostSimulation;
            CharacterActor.OnGroundedStateEnter -= OnGroundedStateEnter;
            CharacterActor.OnGroundedStateExit -= OnGroundedStateExit;
            CharacterActor.OnStableStateEnter -= OnStableStateEnter;
            CharacterActor.OnStableStateExit -= OnStableStateExit;
        }
        characterActor = null;
        characterBody = null;

        MovementAgentScaled = null;
        MovementAgentUnScaled = null;
    }



    ///======================================================================================================================================================



    //. 업데이트



    protected virtual void CurrentFixedUpdate()
    {

    }



    private void OnPreSimulation(float dt)
    {
        //. 이전 프레임 물리 좌표 갱신
        PreviousFramePhysicsPosition = PhysicsPosition;

        //. 업데이트 이벤트
        OnPreSimulationEvent?.Invoke(this);
        OnPreSimulationObjectEvent?.Invoke(this);

        //. 누적된 Δ(여기선 목표 Velocity)를 물리 타이밍에 맞춰 한 번에 적용
        MovementAgentScaled.FlushPendingMovement();
        MovementAgentUnScaled.FlushPendingMovement();


        if (willApplyVelocity != Vector3.zero && dt > 0f)
        {
            CharacterActor.Velocity = PhysicsVelocityUtility.FromDelta(willApplyVelocity, dt);

            ExpectedVelocity = CharacterActor.Velocity; //. 예상 Velocity(m/s) 갱신
        }
        else
        {
            CharacterActor.Velocity = Vector3.zero;
            ExpectedVelocity = Vector3.zero; //. 예상 Velocity(m/s) 갱신
        }

        willApplyVelocity = Vector3.zero;
    }



    private void OnPostSimulation(float dt)
    {
        var delta = PhysicsPosition - PreviousFramePhysicsPosition;
        ActualVelocity = PhysicsVelocityUtility.FromDelta(delta, dt); //. 실측 Velocity(m/s) 갱신

        //. 업데이트 이벤트
        OnPostSimulationEvent?.Invoke(this);
        OnPostSimulationObjectEvent?.Invoke(this);
    }



    ///======================================================================================================================================================



    //. 물리 상호작용 이벤트 호출



    private void OnGroundedStateEnter(Vector3 obj)
    {
        OnEnterGroundedEvent?.Invoke(this);
        OnEnterGroundedObjectEvent?.Invoke(this);
    }



    private void OnGroundedStateExit()
    {
        OnExitGroundedEvent?.Invoke(this);
        OnExitGroundedObjectEvent?.Invoke(this);
    }



    private void OnStableStateEnter(Vector3 obj)
    {
        OnEnterStableGroundedEvent?.Invoke(this);
        OnEnterStableGroundedObjectEvent?.Invoke(this);
    }



    private void OnStableStateExit()
    {
        OnExitStableGroundedEvent?.Invoke(this);
        OnExitStableGroundedObjectEvent?.Invoke(this);
    }



    ///======================================================================================================================================================



    //. 물리 상태 변경



    ///<inheritdoc/>
    public void ForceNotGrounded()
    {
        CharacterActor.ForceNotGrounded();
    }



    ///======================================================================================================================================================
}
