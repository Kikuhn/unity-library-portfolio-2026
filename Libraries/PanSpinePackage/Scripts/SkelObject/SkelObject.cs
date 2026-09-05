using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine;
using Spine.Unity;
using System.Linq;
using System;
using Pan.Util;
using Pan.Util.IOB;
using Pan.Event;
using Pan.SpinePackage;
using static Pan.SpinePackage.SkelObject.AniCore.Players;
using Pan.Util.Game;
using Cysharp.Threading.Tasks;
using System.Text;
using Sirenix.OdinInspector;



namespace Pan.SpinePackage
{
    [RequireComponent(typeof(SkeletonAnimation))]
    public partial class SkelObject :
        EventAbleControllerObject,
        IRendererObject,
        ITimeScaling,
        IBillBoardAble,
        IBaseClassDB<SkelSbject>,
        SkelObject.SkelCore.IGetBonesRunTime
    {
        ///======================================================================================================================================================



        ////? 이 SkelObject의 상위 Eventable



        /////<summary>
        /////이 <see cref="SkelObject"/>를 단독으로 사용하지 않고,
        /////<para>특정 <see cref="IEventAble"/>의 자식으로 운용될 경우 사용</para>
        ///// </summary>
        //public IEventAble MasterEventAble { get; set; }



        ///======================================================================================================================================================



        //? 코어 필드



        ///<summary>
        /// <b>스켈레톤</b> 관련 기능이 모여있는 코어
        /// <para><see cref="CurrentDB"/>에서 초기화까지 해야 사용할수있다</para>
        /// </summary>
        public SkelCore Skel { get => skel; private set => skel = value; }
        [TitleGroup("코어"), FoldoutGroup("코어/Skel 코어"), HideLabel]
        [SerializeField] private SkelCore skel;



        ///<summary>
        /// <b>애니메이션</b> 관련 기능이 모여있는 코어
        /// <para><see cref="CurrentDB"/>에서 초기화까지 해야 사용할수있다</para>
        /// </summary>
        public AniCore Ani { get => ani; private set => ani = value; }
        [TitleGroup("코어"), FoldoutGroup("코어/Ani 코어"), HideLabel]
        [SerializeField] private AniCore ani;



        ///======================================================================================================================================================



        //? 옵저버



        [field: TitleGroup("옵저버"), FoldoutGroup("옵저버/SkelObject 옵저버"), HideLabel]
        [field: SerializeField] public Observers Observer { get; private set; }



        ///======================================================================================================================================================



        //? Time



        ///<summary>
        /// TimeScale 을 반환하는 델리게이트
        /// <para>null일경우, <see cref="GetTimeScale"/>는 <see cref="Time.timeScale"/>를 반환한다</para>
        /// </summary>
        public Func<float> TimeScaling { get; set; }



        ///======================================================================================================================================================



        //? 데이터베이스에 해당하는 SkelSbject 관련



        ///<summary>
        ///이 <see cref="SkelObject"/> 에 해당하는 데이터베이스, <see cref="SkelSbject"/>
        ///<para>할당시, <see cref="Skel"/>과 <see cref="Ani"/>가 Create되며,</para>
        ///<para><see cref="SkelSbject.WakeUp_SkelObject(SkelObject)"/> 가 실행되어 WakeUp 된다</para>
        ///<para><b>이 <see cref="SkelObject"/>를 사용하기 위해서 필수로 할당 해야한다</b></para>
        /// </summary>
        public SkelSbject CurrentDB
        {
            get => currentDB;
            set
            {
                Ani?.UnbindAnimationStateEvents();
                Ani = null;
                currentDB = value;
                SkelCore.CreateSkelCore(this, value);
                AniCore.CreateAniCore(this, value);
                currentDB.WakeUp_SkelObject(this);
            }
        }
        [TitleGroup("대상 SkelSbject"), LabelText("대상 SkelSbject"), OnValueChanged("Inspector_ApplyCurrentDB"), PropertyOrder(-100)]
        [ReadOnlyCustom(true)][SerializeField] private SkelSbject currentDB;



        ///<summary>
        ///캐스팅 하여  <see cref="currentDB>"/> 를 얻는다
        ///</summary>
        public TDB GetDB<TDB>() where TDB : SkelSbject, new()
        {
            if (currentDB is TDB casting)
            {
                return casting;
            }

            return null;
        }

        public TDB GetRequiredDB<TDB>() where TDB : SkelSbject, new()
        {
            var result = GetDB<TDB>();
            if (result != null) { return result; }
            throw new InvalidOperationException($"{name} SkelObject required DB not found. Type='{typeof(TDB).FullName}'.");
        }



        /// <summary>
        /// 캐스팅 하여 <see cref="currentDB"/> 얻기 시도
        /// </summary>
        /// <typeparam name="TDB"></typeparam>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryGetDB<TDB>(out TDB result) where TDB : SkelSbject, new()
        {
            if (currentDB is TDB casting)
            {
                result = casting;
                return true;
            }

            result = null;
            return false;
        }



        ///======================================================================================================================================================



        //? 컴포넌트들


        [TitleGroup("컴포넌트"), FoldoutGroup("컴포넌트/컴포넌트")]
        [SerializeField][ReadOnlyCustom] private SkeletonAnimation m_SkeletonAnimation;
        [TitleGroup("컴포넌트"), FoldoutGroup("컴포넌트/컴포넌트")]
        [SerializeField][ReadOnlyCustom] private Renderer m_Renderer;



        public Renderer Renderer => m_Renderer;



        ///======================================================================================================================================================



        //? 업데이트 이벤트



        /// <summary>
        /// 애니메이션 <see cref="Update_Internal"/>에서 제일 마지막에 실행되는 이벤트
        /// </summary>
        public event Action<SkelObject> PostUpdateEvent = null;


        internal event Action<SkelObject> RuntimePosePreparedEvent = null;


        private bool isSkeletonUpdateRunning;
        private bool runtimeSkeletonRefreshRequested;


        ///======================================================================================================================================================



        //? 업데이트 집행자



        public CustomUpdateExecutor<SkelObject> CustomUpdateExecutor { get => customUpdateExecutor; private set => customUpdateExecutor = value; }

        [TitleGroup("업데이터"), FoldoutGroup("업데이터/통합 업데이트 집행자"), InlineProperty, HideLabel]
        [SerializeField] private CustomUpdateExecutor<SkelObject> customUpdateExecutor;



        ///======================================================================================================================================================



        //? 기타 설정값 변수



        ///<summary>
        /// 런타임 태그 슬롯 딕셔너리 초기 용량
        /// </summary>
        [TitleGroup("기타 설정 값 변수"), FoldoutGroup("기타 설정 값 변수/기타 설정 값 변수"), LabelText("런타임 태그슬롯 초기 용량"), MinValue(0), LabelWidth(250)]
        public int RunTimeTagSlotInitialCapacity;



        ///<summary>
        /// 애니메이션 트랙 메모 초기 용량
        /// </summary>
        [TitleGroup("기타 설정 값 변수"), FoldoutGroup("기타 설정 값 변수/기타 설정 값 변수"), LabelText("애니메이션 트랙 메모 초기 용량"), MinValue(0), LabelWidth(250)]
        public int AnimationTrackMemoInitialCapacity;



        [TitleGroup("기타 설정 값 변수"), FoldoutGroup("기타 설정 값 변수/기타 설정 값 변수"), LabelText("커스텀 드로우 오더 버킷 초기 용량"), MinValue(0), LabelWidth(250)]
        public int CustomDrawOrderExecutes_InitialMaxBucketLength;



        [TitleGroup("기타 설정 값 변수"), FoldoutGroup("기타 설정 값 변수/기타 설정 값 변수"), LabelText("커스텀 드로우 오더 인덱스 딕셔너리 초기 용량"), MinValue(0), LabelWidth(250)]
        public int CustomDrawOrderExecutes_Indexes_InitialCapacity;



        ///======================================================================================================================================================



        /// <summary>
        /// 중심을 맞춰 좌표 이동
        /// <para><see cref="CurrentDB"/>의 <see cref="SkelSbject.CenterLocalPosition"/> 만큼 더해 이동</para>
        /// </summary>
        public void SetPositionWithCenter(Vector2 position)
        {
            OC.SetPosition(position + currentDB.CenterLocalPosition);
        }



        ///======================================================================================================================================================



        //? 인터페이스 구현



        ///<inheritdoc/>
        public SkelCore.RunTimeBone GetRunTimeBone(string boneName) => skel.RunTimeBones.GetRunTimeBone(boneName);



        ///<inheritdoc/>
        public Bone GetBone(string boneName) => skel.RunTimeBones.GetBone(boneName);



        ///======================================================================================================================================================



        //? 초기화



        protected override void Awake()
        {
            base.Awake();


            //. 컴포넌트 지정
            m_SkeletonAnimation = GetComponent<SkeletonAnimation>();
            m_SkeletonAnimation.OnAnimationRebuild -= HandleSkeletonAnimationRebuild;
            m_SkeletonAnimation.OnAnimationRebuild += HandleSkeletonAnimationRebuild;
            m_SkeletonAnimation.UpdateLocal -= ApplyRuntimeSkeletonChangesAfterAnimation;
            m_SkeletonAnimation.UpdateLocal += ApplyRuntimeSkeletonChangesAfterAnimation;
            m_Renderer = GetComponent<Renderer>();
            Observer = new Observers();


            //. SkelObject는 비활성화 될 때 마다 OC를 초기화되게 설정
            IsAutoResetOC_Disable = true;


            //. SkelSbject가 이미 등록되어있다면, CurrentDB setter를 실행시킨다
            if (currentDB != null) { CurrentDB = currentDB; }


            //. 업데이트 집행자 초기화
            CustomUpdateExecutor = new CustomUpdateExecutor<SkelObject>(this, Update_Internal);
        }



        ///======================================================================================================================================================



        //? Enable / Disable / 갱신



        protected override void OnEnable()
        {
            base.OnEnable();

            //! 애초에 DB자체가 존재하지 않는다면, 그 어떤 이벤트도 실행되지 않는다
            if (currentDB == null) { return; }


            //. 기본, 코어의 갱신, Enable
            //? 최상단
            Refresh_SkelObject();
            Skel?.EnableCore();
            Ani?.EnableCore();


            CustomUpdateExecutor.ExecuteUpdate();


            //. DB의 Enable 이벤트
            //? 최하단
            if (currentDB != null) { currentDB.Enable_SkelObject(this); }
        }



        protected override void OnDisable()
        {
            base.OnDisable();

            //! 애초에 DB자체가 존재하지 않는다면, 그 어떤 이벤트도 실행되지 않는다
            if (currentDB == null) { return; }


            //. 기본, 코어의 갱신, Disable
            //? 최상단
            Refresh_SkelObject();
            Skel?.DisableCore();
            Ani?.DisableCore();


            CustomUpdateExecutor.QuitUpdate(false);

            TimeScaling = null;

            //. 렌더러 Sort 관련 정보 초기화
            //m_Renderer.SetSortingLayerExt(SU_SortingLayers.EL.Default);
            m_Renderer.sortingLayerID = 0;
            m_Renderer.sortingOrder = 0;


            //. DB의 Disable 이벤트
            //? 최하단
            if (currentDB != null) { currentDB.Disable_SkelObject(this); }
        }



        protected override void OnDestroy()
        {
            Ani?.UnbindAnimationStateEvents();
            base.OnDestroy();
            if (m_SkeletonAnimation != null)
            {
                m_SkeletonAnimation.OnAnimationRebuild -= HandleSkeletonAnimationRebuild;
                m_SkeletonAnimation.UpdateLocal -= ApplyRuntimeSkeletonChangesAfterAnimation;
            }
            CustomUpdateExecutor.QuitUpdate(true);
        }



        private void HandleSkeletonAnimationRebuild(ISkeletonAnimation skeletonAnimation)
        {
            if (currentDB == null) { return; }
            Ani?.BindAnimationStateEvents();
        }



        private void Refresh_SkelObject()
        {
            PostUpdateEvent = null;
            RuntimePosePreparedEvent = null;
            Observer.ClearOB();
        }



        ///======================================================================================================================================================



        //? SkelObject 업데이트



        ///<summary>
        /// SkelObject 업데이트
        /// </summary>
        private void Update_Internal()
        {
            //. Time 값 구하기
            float timeScale = this.GetTimeScale();
            float deltaTime = Time.unscaledDeltaTime;


            //. AniCore 업데이트
            isSkeletonUpdateRunning = true;
            try
            {
                ani.Update(deltaTime, timeScale);
            }
            finally
            {
                isSkeletonUpdateRunning = false;
            }
        }



        private void LateUpdate()
        {
            if (isSkeletonUpdateRunning || m_SkeletonAnimation == null) { return; }

            ani?.Player?.UpdatePlayer(this.GetTimeScale());

            if (skel?.RunTimeBones == null || !skel.RunTimeBones.HasOverriddenRunTimeBones) { return; }

            CommitRuntimeSkeletonPoseAfterRuntimeOverrides(false, true);
        }



        internal void RequestRuntimeSkeletonRefresh()
        {
            if (isSkeletonUpdateRunning)
            {
                runtimeSkeletonRefreshRequested = true;
                return;
            }

            RefreshRuntimeSkeletonImmediately();
        }



        private void RefreshRuntimeSkeletonImmediately()
        {
            if (m_SkeletonAnimation == null) { return; }

            isSkeletonUpdateRunning = true;
            try
            {
                m_SkeletonAnimation.Update(0f);
            }
            finally
            {
                isSkeletonUpdateRunning = false;
                runtimeSkeletonRefreshRequested = false;
            }

            UpdateSkeletonRendererMesh();
        }



        private void ApplyRuntimeSkeletonChangesAfterAnimation(ISkeletonRenderer skeletonRenderer)
        {
            if (skel == null) { return; }

            CommitRuntimeSkeletonPoseAfterRuntimeOverrides(true, false);

            if (runtimeSkeletonRefreshRequested)
            {
                runtimeSkeletonRefreshRequested = false;
            }
        }



        private bool CommitRuntimeSkeletonPoseAfterRuntimeOverrides(bool invokePostUpdateEvent, bool updateWorldTransform)
        {
            if (skel == null) { return false; }

            if (invokePostUpdateEvent)
            {
                //. 애니메이션 적용 직후, world transform 계산 전에 실행되는 이벤트
                PostUpdateEvent?.Invoke(this);
            }

            bool changed = false;

            //! 애니메이션이 아닌 스크립트로 조작된 Override Bone들이 있다면,
            //! Spine 4.3의 UpdateWorldTransform 전에 Pose에 적용되어야 제약조건에 반영된다
            changed |= skel.RunTimeBones?.Update_RunTimeBones() ?? false;
            changed |= skel.RunTimeSlots?.ApplyCustomDrawOrders() ?? false;

            //. RuntimeBone/Slot이 반영된 동일한 로컬 포즈에서 정책 결과를 평가한다.
            RuntimePosePreparedEvent?.Invoke(this);

            if (changed && updateWorldTransform && m_SkeletonAnimation?.Skeleton != null)
            {
                m_SkeletonAnimation.Skeleton.UpdateWorldTransform(Spine.Physics.Update);
                UpdateSkeletonRendererMesh();
            }

            return changed;
        }



        private void UpdateSkeletonRendererMesh()
        {
            var skeletonRenderer = m_SkeletonAnimation?.Renderer;
            if (skeletonRenderer != null && skeletonRenderer.IsValid)
            {
                skeletonRenderer.UpdateMesh();
            }
        }



        ///======================================================================================================================================================



        //? 인스펙터 전용 메서드



        private void Inspector_ApplyCurrentDB()
        {
            var skeletonAnimation = GetComponent<SkeletonAnimation>();

            if (currentDB != null && currentDB.Skeleton_DataAsset != null)
            {
                skeletonAnimation.skeletonDataAsset = currentDB.Skeleton_DataAsset;
                skeletonAnimation.Initialize(true);
            }
            else
            {
                skeletonAnimation.skeletonDataAsset = null;
                skeletonAnimation.Initialize(true);
            }
        }



        ///======================================================================================================================================================
    }
}
