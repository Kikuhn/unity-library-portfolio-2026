using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Events;
using DG.Tweening;
using Pan.Util;
using System.Reflection.Emit;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;



//? [CustomUpdateExecutor] 업데이트를 별도로 실행이 가능한 업데이트 집행자 가 정리되어있는 정도의 코드 (중앙-집중형 업데이트, 자체 UniTask 업데이트)



namespace Pan.Util
{
    ///======================================================================================================================================================



    /// <summary>
    /// 업데이트 집행자 (<see cref="CustomUpdateExecutor{TMonoBehaviour}"/>) 보유 인터페이스
    /// </summary>
    /// <typeparam name="TMonoBehaviour"></typeparam>
    public interface IHoldCustomUpdateExecutor<TMonoBehaviour> where TMonoBehaviour : MonoBehaviour
    {
        CustomUpdateExecutor<TMonoBehaviour> UpdateExecutor { get; }
    }



    ///<summary>
    /// 업데이트 집행자
    ///<para><see cref="MonoBehaviour"/>의 <c>Update</c> 메서드를 사용하면, 여러 비효율 적인 이슈가 있기에,</para>
    ///<para>이 업데이트 집행자를 통해, 자체적으로 UniTask로 실행할지,</para>
    ///<para>중앙-집중형 으로 실행할지 관리해주는 집행자</para>
    ///<para> 최초 지정한 업데이트 단계, 모드는 변경할수 없음</para>
    /// </summary>
    [Serializable]
    public class CustomUpdateExecutor<TMonoBehaviour> : IHoldCustomUpdateExecutor<TMonoBehaviour> where TMonoBehaviour : MonoBehaviour
    {
        ///======================================================================================================================================================



        //. 직렬화용 프라이빗 생성자
        protected CustomUpdateExecutor() { }



        /// <summary>
        /// 업데이트 집행자 초기화
        /// <para>Awake에서 사용 권장</para>
        /// </summary>
        /// <param name="monoBehaviour">대상 Monobehaviour</param>
        /// <param name="updateInternalAction">대상 Monobehaviour의 업데이트 이벤트를 델리게이트로 받기</param>
        /// <param name="initialUpdateModeUpdatePhase">업데이트 단계 (미 지정시, Update 또는 인스펙터에서 지정한 단계가 기본으로 적용)</param>
        /// <param name="initialUpdateMode">업데이트 모드 (미 지정시, 자체 업데이트 또는 인스펙터에서 지정한 모드가 기본으로 적용)</param>
        public CustomUpdateExecutor(TMonoBehaviour monoBehaviour, Action updateInternalAction, EUpdatePhase? initialUpdateModeUpdatePhase = null, EUpdateMode? initialUpdateMode = null, int? updateModuleInitialCapacity = null)
        {
            Target = monoBehaviour;
            UpdateInternalEvent = updateInternalAction;

            //. 업데이트 설정을 받아왔다면, 각각 적용한다
            if (updateModuleInitialCapacity.HasValue) { UpdateModuleInitialCapacity = updateModuleInitialCapacity.Value; } //! 제일 먼저 적용해야함
            if (initialUpdateModeUpdatePhase.HasValue) { InitalizeUpdatePhase = initialUpdateModeUpdatePhase.Value; }
            if (initialUpdateMode.HasValue) { InitializeUpdateMode = initialUpdateMode.Value; }


            //. 업데이트모듈 초기화
            UpdateModule = new UpdateModule<TMonoBehaviour>(Target, UpdateModuleInitialCapacity);


            //. 초기 업데이트 모드 초기화
            ApplyInitialModeOnce(InitializeUpdateMode);

            //. 초기화 완료
            Initialized = true;


            void ApplyInitialModeOnce(EUpdateMode updateMode)
            {
                //. 업데이트 모드 적용
                switch (updateMode)
                {
                    //? "중앙-집중형 업데이트" 사용
                    case EUpdateMode.CentralizedUpdate:

                    UpdateModule.AddUpdate(UpdateInternalEvent);

                    //. 중앙-집중형 싱글톤 매니저에 업데이트 모듈을 등록해본다
                    //!     만약 등록에 실패했다면, "자체 업데이트"로 강제로 변환되어 다시 재귀한다
                    if (!TryAddUpdateModule_ToCentralizedUpdate())
                    {
                        UpdateModule.RemoveUpdate(UpdateInternalEvent);
                        InitializeUpdateMode = EUpdateMode.SelfUpdate;
                        ApplyInitialModeOnce(EUpdateMode.SelfUpdate);
                        return;
                    }

                    break;


                    //? "자체 업데이트" 사용
                    case EUpdateMode.SelfUpdate:

                    //. UniTask 업데이터가 초기화 되어있지 않다면 초기화해주고, 업데이트를 설정한다
                    UniTaskUpdater ??= new UniTaskUpdater();

                    switch (InitalizeUpdatePhase)
                    {
                        case EUpdatePhase.Update: UniTaskUpdater.Setting(UpdateInternalEvent, PlayerLoopTiming.Update); break;
                        case EUpdatePhase.LateUpdate: UniTaskUpdater.Setting(UpdateInternalEvent, PlayerLoopTiming.PreLateUpdate); break;
                        case EUpdatePhase.FixedUpdate: UniTaskUpdater.Setting(UpdateInternalEvent, PlayerLoopTiming.FixedUpdate); break;
                    }

                    break;
                }
            }
        }



        CustomUpdateExecutor<TMonoBehaviour> IHoldCustomUpdateExecutor<TMonoBehaviour>.UpdateExecutor => this;



        ///======================================================================================================================================================



#if UNITY_EDITOR

        [FoldoutGroup("업데이트 집행자")]
        [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Center, EnableRichText = true, Overflow = false), EnableGUI]
        [PropertyOrder(-100), PropertySpace(SpaceAfter = 8)]
        protected virtual string editorTitle => $"<b><size=12>업데이트 집행자</size></b>";

#endif


        ///======================================================================================================================================================



        [FoldoutGroup("업데이트 집행자"), BoxGroup("업데이트 집행자/초기화박스", false)]
        [LabelText("초기화 여부")]
        [ShowInInspector, ReadOnly]
        [PropertyOrder(50)]
        private readonly bool Initialized = false;



        [FoldoutGroup("업데이트 집행자"), BoxGroup("업데이트 집행자/초기화박스", false)]
        [LabelText("Target")]
        [ShowInInspector, ReadOnly]
        [PropertyOrder(50)]
        protected readonly TMonoBehaviour Target;



        [FoldoutGroup("업데이트 집행자"), BoxGroup("업데이트 집행자/초기화박스", false)]
        [LabelText("업데이트 실행중 여부")]
        [ShowInInspector, ReadOnly]
        [PropertyOrder(50)]
        private bool IsUpdateExecuting = false;



        ///<summary>
        /// 업데이트 모듈 초기 용량
        /// </summary>
        [FoldoutGroup("업데이트 집행자"), BoxGroup("업데이트 집행자/초기화박스", false)]
        [LabelText("업데이트 모듈 초기 용량"), LabelWidth(250), ShowInInspector]
        [DisableIf(nameof(Initialized))]
        [PropertyOrder(50)]
        private int UpdateModuleInitialCapacity
        {
            get => updateModuleInitialCapacity;
            set
            {
                updateModuleInitialCapacity = Mathf.Max(0, value);
            }
        }
        [SerializeField, HideInInspector]
        private int updateModuleInitialCapacity = 1;



        ///======================================================================================================================================================



        //? 업데이트 모드



        [FoldoutGroup("업데이트 집행자"), BoxGroup("업데이트 집행자/업데이트박스", false)]
        [LabelText("업데이트 단계"), EnumToggleButtons, PropertyOrder(0)]
        [EnableIf(nameof(InspectorEnableUpdatePhase))]
        [SerializeField]
        protected EUpdatePhase InitalizeUpdatePhase = EUpdatePhase.Update;



        [FoldoutGroup("업데이트 집행자"), BoxGroup("업데이트 집행자/업데이트박스", false)]
        [LabelText("업데이트 모드"), EnumToggleButtons, PropertyOrder(0)]
        [DisableIf(nameof(Initialized))]
        [SerializeField]
        protected EUpdateMode InitializeUpdateMode = EUpdateMode.SelfUpdate;



        private bool IsUpdateMode_CentralizedUpdate => InitializeUpdateMode == EUpdateMode.CentralizedUpdate;



        ///<summary>
        /// 업데이트 모듈
        ///</summary>
        [FoldoutGroup("업데이트 집행자"), BoxGroup("업데이트 집행자/업데이트박스", false)]
        [SerializeField, HideLabel, InlineProperty, PropertyOrder(1)]
        private UpdateModule<TMonoBehaviour> UpdateModule = null;



        //? 인스펙터 업데이트 단계 수정 활성화
        protected virtual bool InspectorEnableUpdatePhase => !Initialized;



        ///======================================================================================================================================================



        //? 자체 업데이터 (UniTask)



        /// <summary>
        /// 자체 업데이트 전용, UniTask 업데이터
        /// </summary>
        [FoldoutGroup("업데이트 집행자"), BoxGroup("업데이트 집행자/UniTask 업데이터")]
        [SerializeField, HideLabel, InlineProperty, PropertyOrder(1)]
        [DisableIf(nameof(IsUpdateMode_CentralizedUpdate))]
        private UniTaskUpdater UniTaskUpdater;



        ///======================================================================================================================================================



        //? 업데이트 실행 델리게이트



        private readonly Action UpdateInternalEvent;



        //. 업데이트 모듈을 중앙-집중형 싱글톤 매니저에 등록해본다
        private bool TryAddUpdateModule_ToCentralizedUpdate()
        {
            if (CentralizedUpdateSingleTonManager.TryGetSingleTon(out var centralizedUpdateManager))
            {
                switch (InitalizeUpdatePhase)
                {
                    case EUpdatePhase.Update: centralizedUpdateManager.Updater.AddUpdateModule(UpdateModule); break;
                    case EUpdatePhase.LateUpdate: centralizedUpdateManager.LateUpdater.AddUpdateModule(UpdateModule); break;
                    case EUpdatePhase.FixedUpdate: centralizedUpdateManager.FixedUpdater.AddUpdateModule(UpdateModule); break;
                }
                return true;
            }
            return false;
        }



        ///======================================================================================================================================================



        //? 업데이트 집행 실행 / 종료



        /// <summary>
        /// Update 시작
        /// <para>OnEnable 등 메서드에서 실행 권장</para>
        /// </summary>
        public void ExecuteUpdate()
        {
            if (Initialized == false) { throw new InvalidOperationException($"[{nameof(CustomUpdateExecutor<TMonoBehaviour>)}] : 초기화 되지 않은 상태에서 업데이트를 실행할 수 없습니다. 반드시 생성자를 통해 초기화 해주세요."); }
            if (IsUpdateExecuting) return; //! 중복 실행 방지


            switch (InitializeUpdateMode)
            {
                //? 중앙 집중형 업데이터라면, 중앙 집중형 업데이터에 업데이트 모듈을 등록해본다
                case EUpdateMode.CentralizedUpdate:

                //. 우선 업데이트 모듈에, 업데이트 이벤트를 추가한다
                UpdateModule.AddUpdate(UpdateInternalEvent);
                //? 중앙-집중형 업데이터 (싱글톤)에 업데이트 모듈을 등록해본다
                if (!TryAddUpdateModule_ToCentralizedUpdate()) { return; } //! 등록에 실패했다면, 업데이트를 실행하지 않는다
                break;


                //? 자체 업데이터라면, UniTaskUpdater를 실행한다
                case EUpdateMode.SelfUpdate:
                UniTaskUpdater.ExecuteUpdate();
                break;
            }


            IsUpdateExecuting = true;
        }



        /// <summary>
        /// Update 종료
        /// <para>OnDisable, OnDestroy 등 메서드에서 실행 권장</para>
        /// </summary>
        public void QuitUpdate(bool destroy)
        {
            if (!IsUpdateExecuting && !destroy) return; //! 중복 실행 방지 (destroy 제외)

            if (IsUpdateExecuting)
            {
                switch (InitializeUpdateMode)
                {
                    case EUpdateMode.CentralizedUpdate:

                    //? 업데이트 모듈 내의 업데이트 이벤트를 제거하여,
                    //? 중앙-집중형 업데이터에서, 자동으로 제거 되게끔 (업데이트 이벤트 개수 0개를 만들어 비활성화 -> 제거) 해준다
                    UpdateModule.RemoveUpdate(UpdateInternalEvent);

                    break;


                    case EUpdateMode.SelfUpdate:
                    UniTaskUpdater.QuitUpdate();
                    break;
                }

                IsUpdateExecuting = false;
            }


            if (destroy)
            {
                UpdateModule = null;
                UniTaskUpdater = null;
            }
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================



    /// <summary>
    /// 업데이트 집행자 (<see cref="CustomUpdateExecutor_Update{TMonoBehaviour}"/>) 보유 인터페이스"
    /// <para><i>Update 고정</i></para>
    /// </summary>
    /// <typeparam name="TMonoBehaviour"></typeparam>
    public interface IHoldCustomUpdateExecutor_Update<TMonoBehaviour> where TMonoBehaviour : MonoBehaviour
    {
        CustomUpdateExecutor_Update<TMonoBehaviour> UpdateExecutor_Update { get; }
    }

    /// <summary>
    /// 업데이트 집행자 (<see cref="CustomUpdateExecutor_LateUpdate{TMonoBehaviour}"/>) 보유 인터페이스" 
    /// <para><i>LateUpdate 고정</i></para>
    /// </summary>
    /// <typeparam name="TMonoBehaviour"></typeparam>
    public interface IHoldCustomUpdateExecutor_LateUpdate<TMonoBehaviour> where TMonoBehaviour : MonoBehaviour
    {
        CustomUpdateExecutor_LateUpdate<TMonoBehaviour> UpdateExecutor_LateUpdate { get; }
    }

    /// <summary>
    /// 업데이트 집행자 (<see cref="CustomUpdateExecutor_FixedUpdate{TMonoBehaviour}"/>) 보유 인터페이스" 
    /// <para><i>FixedUpdate 고정</i></para>
    /// </summary>
    /// <typeparam name="TMonoBehaviour"></typeparam>
    public interface IHoldCustomUpdateExecutor_FixedUpdate<TMonoBehaviour> where TMonoBehaviour : MonoBehaviour
    {
        CustomUpdateExecutor_FixedUpdate<TMonoBehaviour> UpdateExecutor_FixedUpdate { get; }
    }




    /// <summary>
    /// 업데이트 집행자 - Update 전용
    /// </summary>
    /// <typeparam name="TMonoBehaviour"></typeparam>
    [Serializable]
    public class CustomUpdateExecutor_Update<TMonoBehaviour> : CustomUpdateExecutor<TMonoBehaviour> where TMonoBehaviour : MonoBehaviour
    {
        private CustomUpdateExecutor_Update()
        {
            InitalizeUpdatePhase = EUpdatePhase.Update;
        }

        public CustomUpdateExecutor_Update(TMonoBehaviour monoBehaviour, Action updateInternalAction, EUpdateMode? initialUpdateMode = null, int? updateModuleInitialCapacity = null) : base(monoBehaviour, updateInternalAction, EUpdatePhase.Update, initialUpdateMode, updateModuleInitialCapacity) { }

#if UNITY_EDITOR

        [FoldoutGroup("업데이트 집행자")]
        [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Center, EnableRichText = true, Overflow = false), EnableGUI]
        [PropertyOrder(-100), PropertySpace(SpaceAfter = 8)]
        protected override sealed string editorTitle => $"<b><size=12>업데이트 집행자</size></b> <i>(Update 고정)</i>";

#endif

        //. 인스펙터 업데이트 단계 수정 비활성화
        protected sealed override bool InspectorEnableUpdatePhase => false;
    }



    /// <summary>
    /// 업데이트 집행자 - LateUpdate 전용 
    /// </summary>
    /// <typeparam name="TMonoBehaviour"></typeparam>
    [Serializable]
    public class CustomUpdateExecutor_LateUpdate<TMonoBehaviour> : CustomUpdateExecutor<TMonoBehaviour> where TMonoBehaviour : MonoBehaviour
    {
        private CustomUpdateExecutor_LateUpdate()
        {
            InitalizeUpdatePhase = EUpdatePhase.LateUpdate;
        }

        public CustomUpdateExecutor_LateUpdate(TMonoBehaviour monoBehaviour, Action updateInternalAction, EUpdateMode? initialUpdateMode = null, int? updateModuleInitialCapacity = null) : base(monoBehaviour, updateInternalAction, EUpdatePhase.LateUpdate, initialUpdateMode, updateModuleInitialCapacity) { }

#if UNITY_EDITOR

        [FoldoutGroup("업데이트 집행자")]
        [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Center, EnableRichText = true, Overflow = false), EnableGUI]
        [PropertyOrder(-100), PropertySpace(SpaceAfter = 8)]
        protected override sealed string editorTitle => $"<b><size=12>업데이트 집행자</size></b> <i>(LateUpdate 고정)</i>";

#endif

        //. 인스펙터 업데이트 단계 수정 비활성화
        protected sealed override bool InspectorEnableUpdatePhase => false;
    }



    /// <summary>
    /// 업데이트 집행자 - FixedUpdate 전용 
    /// </summary>
    /// <typeparam name="TMonoBehaviour"></typeparam>
    [Serializable]
    public class CustomUpdateExecutor_FixedUpdate<TMonoBehaviour> : CustomUpdateExecutor<TMonoBehaviour> where TMonoBehaviour : MonoBehaviour
    {
        private CustomUpdateExecutor_FixedUpdate()
        {
            InitalizeUpdatePhase = EUpdatePhase.FixedUpdate;
        }

        public CustomUpdateExecutor_FixedUpdate(TMonoBehaviour monoBehaviour, Action updateInternalAction, EUpdateMode? initialUpdateMode = null, int? updateModuleInitialCapacity = null) : base(monoBehaviour, updateInternalAction, EUpdatePhase.FixedUpdate, initialUpdateMode, updateModuleInitialCapacity) { }

#if UNITY_EDITOR

        [FoldoutGroup("업데이트 집행자")]
        [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Center, EnableRichText = true, Overflow = false), EnableGUI]
        [PropertyOrder(-100), PropertySpace(SpaceAfter = 8)]
        protected override sealed string editorTitle => $"<b><size=12>업데이트 집행자</size></b> <i>(FixedUpdate 고정)</i>";

#endif

        //. 인스펙터 업데이트 단계 수정 비활성화
        protected sealed override bool InspectorEnableUpdatePhase => false;
    }



    ///======================================================================================================================================================
}