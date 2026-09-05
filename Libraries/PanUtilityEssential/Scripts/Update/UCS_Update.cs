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
using System.Runtime.Serialization;
using Sirenix.OdinInspector;



namespace Pan.Util
{
    ///======================================================================================================================================================



    //? 업데이트 열거형



    ///<summary>
    /// 업데이트 방식이 자체 업데이트인지, 중앙-집중형 업데이트인지 확인하는 열거형
    /// </summary>
    public enum EUpdateMode
    {
        /// <summary>
        /// 중앙-집중형 업데이트를 사용
        /// <para><see cref="CentralizedUpdater"/></para>
        /// </summary>
        [LabelText("🀄 중앙-집중형 업데이트")] CentralizedUpdate,
        /// <summary>
        /// 자체 업데이트를 사용
        /// </summary>
        [LabelText("😎 자체 업데이트")] SelfUpdate
    }



    /// <summary>
    /// 업데이트 단계 열거형 
    /// </summary>
    public enum EUpdatePhase
    {
        [LabelText("Update")] Update,
        [LabelText("LateUpdate")] LateUpdate,
        [LabelText("FixedUpdate")] FixedUpdate,
    }



    ///======================================================================================================================================================



    //? 업데이트 모듈



    public interface IUpdateModule
    {
        /// <summary>
        /// 모듈의 활성화 여부
        /// <para>비활성화 되어있다면, 이 모듈이 속한 업데이트 목록에서 제거 할 필요가 있음</para>
        /// </summary>
        bool EnabledUpdateModule { get; }

        /// <summary>
        /// 업데이트 이벤트를 <b>추가</b>
        /// </summary>
        /// <param name="updateEvent">등록할 업데이트 이벤트</param>
        void AddUpdate(Action updateEvent);

        /// <summary>
        /// 업데이트 이벤트를 <b>제거</b>
        /// </summary>
        /// <param name="updateEvent">등록할 업데이트 이벤트</param>
        void RemoveUpdate(Action updateEvent);

        /// <summary>
        /// 모든 이벤트와 조건을 정리합니다.
        /// </summary>
        void ClearUpdate();

        /// <summary>
        /// 모듈 업데이트 실행
        /// </summary>
        void ExecuteModuleUpdate();
    }



    [Serializable]
    public class UpdateModule<TMonoBehaviour> : IUpdateModule where TMonoBehaviour : MonoBehaviour
    {
        ///======================================================================================================================================================



        ///<summary>
        /// 업데이트 모듈 초기화 (capacity 각각 지정)
        /// </summary>
        public UpdateModule(TMonoBehaviour currentMonoBehaviour, int capacity_UpdateEvents, int capacity_WaitersWillAdd, int capacity_WaitersWillRemove)
        {
            CurrentMonoBehaviour = currentMonoBehaviour;
            UpdateEvents = new IndexedSet<Action>(capacity_UpdateEvents);
            Waiters_WillAdd = new List<Action>(capacity_WaitersWillAdd);
            Waiters_WillRemove = new List<Action>(capacity_WaitersWillRemove);
        }



        ///<summary>
        /// 업데이트 모듈 초기화 (Waiters 리스트의 capacity는 <paramref name="capacity"/>의 <b>1/4</b> 로 자동지정)
        /// </summary>
        public UpdateModule(TMonoBehaviour currentMonoBehaviour, int capacity) : this(currentMonoBehaviour, capacity, capacity / 4, capacity / 4)
        {

        }



        ///======================================================================================================================================================



        /// <summary>
        /// 업데이트 대상 MonoBehaviour를 참조합니다.
        /// </summary>
        protected readonly TMonoBehaviour CurrentMonoBehaviour;



        /// <summary>
        /// 업데이트 대상 MonoBehaviour가 유효한지 확인
        /// <para>유효하지않다면, Update에서 이를 확인하고 중단</para>
        /// </summary>
        protected bool CurrentMonoBehaviourIsValid => CurrentMonoBehaviour != null;



        /// <summary>
        /// 이 업데이트 모듈의 활성화 여부
        /// <para>이 모듈에 1개 이상의 업데이트 이벤트가 추가 된다면, <b>활성화</b></para>
        /// <para>이 모듈에 더이상 남은 업데이트 이벤트가 존재하지 않는다면, <b>비활성화</b></para>
        /// <para>업데이트 콜렉션 순회에서, 에서 매번 이 모듈에활성화 여부를 확인하며</para>
        /// <para><b>비활성화</b>되어있다면, 업데이트 이벤트를 실행 조차 하지 않으며 (어차피 텅 비어있어서 무의미)</para>
        /// <para>그 업데이트 콜렉션 순회가 끝난 후, 제거된다</para>
        /// </summary>
        [ShowInInspector, LabelText("업데이트 모듈 활성화 여부"), DisplayAsString]
        [PropertyTooltip("이 모듈에 1개 이상의 업데이트 이벤트가 추가되면 활성화, 더이상 남은 업데이트 이벤트가 없다면 비활성화 됩니다.")]
        public virtual bool EnabledUpdateModule { get; protected set; }



        /// <summary>
        /// 모듈 내부에서 스레드 안전성을 위해 사용하는 락 오브젝트입니다.
        /// </summary>
        private readonly object _lockModule = new object();



        ///======================================================================================================================================================



        //? 업데이트 이벤트 콜렉션



        /// <summary>
        /// <see cref="CentralizedUpdater"/>에서 특정 프레임마다 실행될 <b>이벤트들</b>
        /// </summary>
        protected readonly IndexedSet<Action> UpdateEvents;



        /// <summary>
        /// <see cref="UpdateEvents"/>의 Count
        /// </summary>
        [ShowInInspector, LabelText("현재 업데이트 이벤트 개수"), DisplayAsString, EnableGUI]
        public int Count_UpdateEvents => UpdateEvents != null ? UpdateEvents.Count : 0;



        //? 업데이트 이벤트 추가/제거 예약 콜렉션



        /// <summary>
        /// <see cref="ExecuteModuleUpdate"/> 메세드의 순회 도중,
        /// <para><b>Update 이벤트 추가</b>를 순회가 끝난 뒤에 시행하기 위한 콜렉션</para>
        /// </summary>
        protected readonly List<Action> Waiters_WillAdd;


        /// <summary>
        /// <see cref="ExecuteModuleUpdate"/> 메세드의 순회 도중,
        /// <para><b>Update 이벤트 제거</b>를 순회가 끝난 뒤에 시행하기 위한 콜렉션</para>
        /// </summary>
        protected readonly List<Action> Waiters_WillRemove;



        /// <summary>
        /// <see cref="ExecuteModuleUpdate"/> 메세드의 순회 도중,
        /// <para><b>Update 이벤트 전체 제거</b>를 순회가 끝난 뒤에 시행하기 위한 콜렉션</para>
        /// <para><b>추가/제거 보다 우선적으로 시행됨!</b></para>
        /// </summary>
        public bool Waiter_WillClear { get; private set; } = false;



        ///======================================================================================================================================================



        //? 순회용 변수



        /// <summary>
        /// 현재 루프 중인지 여부를 나타냅니다.
        /// </summary>
        public bool IsLooping { get; private set; } = false;



        ///======================================================================================================================================================



        ///<inheritdoc/>
        public void AddUpdate(Action updateEvent)
        {
            //. "추가"를 하는것이기 때문에, 모듈이 비활성화 되어있다면 활성화
            if (!EnabledUpdateModule) { EnabledUpdateModule = true; }

            lock (_lockModule)
            {
                //? "순회" 중이라면, 순회가 끝난 뒤에 "추가" 되게끔 예약한다
                if (IsLooping) { Waiters_WillAdd.Add(updateEvent); }

                //. "순회" 중이 아니라면, 즉시 "추가" 되게끔 한다
                else { AddUpdateEventInternal(updateEvent); }
            }
        }

        private void AddUpdateEventInternal(Action updateEvent)
        {
            UpdateEvents.Add(updateEvent);
        }



        ///<inheritdoc/>
        public void RemoveUpdate(Action updateEvent)
        {
            lock (_lockModule)
            {
                //? "순회" 중이라면, 순회가 끝난 뒤에 "제거" 되게끔 예약한다
                if (IsLooping) { Waiters_WillRemove.Add(updateEvent); }

                //. "순회" 중이 아니라면, 즉시 "제거" 되게끔 한다
                else { RemoveUpdateEventInternal(updateEvent); }
            }
        }

        private void RemoveUpdateEventInternal(Action updateEvent)
        {
            UpdateEvents.Remove(updateEvent);

            //. "제거" 이후, 남은 업데이트 이벤트가 존재하지 않는다면, 모듈 비활성화
            if (UpdateEvents.Count == 0)
            {
                EnabledUpdateModule = false;
            }
        }



        ///<inheritdoc/>
        public void ClearUpdate()
        {
            lock (_lockModule)
            {
                //? "순회" 중이라면, 순회가 끝난뒤에 "Clear" 되게끔 예약한다
                if (IsLooping) { Waiter_WillClear = true; }

                //. "순회" 중이 아니라면, 즉시 "Clear" 되게끔 예약한다
                else { ClearInternal(); }
            }
        }

        private void ClearInternal()
        {
            UpdateEvents.Clear();
            Waiters_WillAdd.Clear();
            Waiters_WillRemove.Clear();

            //. Clear시, 무조건 모듈 비활성화 (남은 업데이트 이벤트가 더 없으므로)
            EnabledUpdateModule = false;
        }



        ///======================================================================================================================================================



        //? 업데이트



        /// <summary>
        /// 매 프레임 업데이트를 수행하고, 등록된 이벤트들을 순회합니다.
        /// </summary>
        public void ExecuteModuleUpdate()
        {
            //! 이 MonoBehaviour가 유효하지 않다면, 모듈을 비활성화하고 return
            if (!CurrentMonoBehaviourIsValid) { EnabledUpdateModule = false; return; }

            //! 업데이트 이벤트가 전혀 존재하지 않는다면, return
            if (UpdateEvents.Count == 0) { return; }


            //. --- 업데이트 순회 시작 ---
            IsLooping = true;

            for (int i = 0; i < UpdateEvents.Count; i++)
            {
                UpdateEvents[i].Invoke();
            }

            IsLooping = false;
            //. --- 업데이트 순회 종료 ---


            //? #1 Clear 예약시, Clear 우선 실행, 나머지 추가/제거 예약은 무시
            if (Waiter_WillClear)
            {
                Waiter_WillClear = false;
                ClearInternal();
                return;
            }


            //? #2 Clear가 아닌, 추가/제거 예약 확인
            else
            {
                //? #2 "추가" 를 위한 예약 적용
                if (Waiters_WillAdd.Count != 0)
                {
                    for (int i = 0; i < Waiters_WillAdd.Count; i++)
                    {
                        AddUpdateEventInternal(Waiters_WillAdd[i]);
                    }
                    Waiters_WillAdd.Clear();
                }

                //? #2 "제거" 를 위한 예약 적용
                if (Waiters_WillRemove.Count != 0)
                {
                    for (int i = 0; i < Waiters_WillRemove.Count; i++)
                    {
                        RemoveUpdateEventInternal(Waiters_WillRemove[i]);
                    }

                    Waiters_WillRemove.Clear();
                }
            }
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================



    //? 업데이트 모듈 세트 



    /// <summary>
    /// 업데이트 모듈 세트의 베이스
    /// </summary>
    public abstract class UpdateModuleSetBase<TMonoBehaviour> where TMonoBehaviour : MonoBehaviour
    {
        ///======================================================================================================================================================



        /// <summary>
        /// Alpha와 Beta, 그 외 확장 단계(감마 등)를 어떤 순서로 적용할지 정하는 열거형입니다.
        /// </summary>
        public enum EUpdateOrder
        {
            /// <summary>
            /// <b>기본값</b>
            /// <para>1. <b>알파</b> 실행</para>
            /// <para>2. <b>베타</b> 실행</para>
            /// </summary>
            Alpha_Beta,
            /// <summary>
            /// 1. <b>감마</b> 실행
            /// <para>2. <b>알파</b> 실행</para>
            /// <para>3. <b>베타</b> 실행</para>
            /// </summary>
            Gamma_Alpha_Beta,
            /// <summary>
            /// 1. <b>알파</b> 실행
            /// <para>2. <b>감마</b> 실행</para>
            /// <para>3. <b>베타</b> 실행</para>
            /// </summary>
            Alpha_Gamma_Beta,
            /// <summary>
            /// 1. <b>알파</b> 실행
            /// <para>2. <b>베타</b> 실행</para>
            /// <para>3. <b>감마</b> 실행</para>
            /// </summary>
            Alpha_Beta_Gamma
        }



        ///======================================================================================================================================================



        //? 모듈 활성화



        ///<inheritdoc/>
        public bool EnabledUpdateModule
        {
            get { return enabledUpdateModule; }
            protected set
            {
                enabledUpdateModule = value;
            }
        }
        private bool enabledUpdateModule;



        ///======================================================================================================================================================



        ///<inheritdoc/>
        public abstract void ExecuteModuleUpdate();



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 업데이트 모듈 세트 (알파, 베타)
    /// </summary>
    /// <typeparam name="TMonoBehaviour">대상 MonoBehaviour</typeparam>
    public class UpdateModuleSet<TMonoBehaviour> : UpdateModuleSetBase<TMonoBehaviour> where TMonoBehaviour : MonoBehaviour
    {
        ///======================================================================================================================================================



        ///<summary>
        /// 기본 모듈 업데이트 세트 초기화 (capacity 각각 지정)
        /// </summary>
        public UpdateModuleSet(TMonoBehaviour currentMonobehaviour,
            int capacity_Alpha_UpdateEvents, int capacity_Alpha_WaitersWillAdd, int capacity_Alpha_WaitersWillRemove,
            int capacity_Beta_UpdateEvents, int capacity_Beta_WaitersWillAdd, int capacity_Beta_WaitersWillRemove
            )
        {
            CurrentMonobehaviour = currentMonobehaviour;

            //. 기본 모듈 초기화
            AlphaModule = new UpdateModule<TMonoBehaviour>(currentMonobehaviour, capacity_Alpha_UpdateEvents, capacity_Alpha_WaitersWillAdd, capacity_Alpha_WaitersWillRemove);
            BetaModule = new UpdateModule<TMonoBehaviour>(currentMonobehaviour, capacity_Beta_UpdateEvents, capacity_Beta_WaitersWillAdd, capacity_Beta_WaitersWillRemove);


            //. 기본 모듈 업데이트를 순서에 맞게  이벤트 등록
            InvokeUpdateEvent = () =>
            {
                ExecuteAlpha();
                ExecuteBeta();
            };
        }



        ///<summary>
        /// 기본 모듈 업데이트 세트 초기화 (Waiters 리스트의 capacity는 capacity의 <b>1/4</b> 로 자동지정)
        /// </summary>
        public UpdateModuleSet(TMonoBehaviour currentMonobehaviour,
            int capacity_Alpha,
            int capacity_Beta
            ) : this(currentMonobehaviour, capacity_Alpha, capacity_Alpha / 4, capacity_Alpha / 4, capacity_Beta, capacity_Beta / 4, capacity_Beta / 4)
        {

        }



        ///======================================================================================================================================================



        /// <summary>
        /// 대상 MonoBehaviour입니다.
        /// </summary>
        protected readonly TMonoBehaviour CurrentMonobehaviour;



        ///======================================================================================================================================================



        //? 업데이트 델리게이트



        /// <summary>
        /// 실제로 업데이트가 호출될 때 실행되는 이벤트 델리게이트입니다.
        /// </summary>
        protected Action InvokeUpdateEvent { get; set; } = null;



        ///======================================================================================================================================================



        //? 기본 모듈 (알파, 베타)



        /// <summary>
        /// Alpha 단계에서 동작하는 업데이트 모듈입니다.
        /// </summary>
        protected readonly UpdateModule<TMonoBehaviour> AlphaModule;



        /// <summary>
        /// Beta 단계에서 동작하는 업데이트 모듈입니다.
        /// </summary>
        protected readonly UpdateModule<TMonoBehaviour> BetaModule;



        ///======================================================================================================================================================



        //? 모듈 활성화/비활성화, 유효 확인, 실행 메서드



        ///<summary>
        ///소속중인 모듈들이 <b>유효</b> 한지 여부
        ///<para>모듈들 중의 하나의 모듈이라도 Count가 0이 아니라면 true</para>
        ///<para>모든 모듈들의 Count가 0이라면 false</para>
        /// </summary>
        protected virtual bool CheckValid_Modules => AlphaModule.Count_UpdateEvents != 0 || BetaModule.Count_UpdateEvents != 0;



        ///======================================================================================================================================================



        //? 업데이트 이벤트가 추가/제거 될 때 마다 실행



        /// <summary>
        /// 업데이트 이벤트를 <b>추가</b>할때마다, 그 전에 실행되는 메서드
        /// </summary>
        protected void WhenBefore_AddUpdateEvent()
        {
            if (!EnabledUpdateModule) { EnabledUpdateModule = true; }
        }



        /// <summary>
        /// 업데이트 이벤트를 <b>제거</b>할때마다, 그 후에 실행되는 메서드
        /// </summary>
        protected void WhenAfter_RemoveUpdateEvent()
        {
            if (!CheckValid_Modules) { EnabledUpdateModule = false; } //! 남은 게 없을 때만 비활성화
        }



        ///======================================================================================================================================================



        //? 업데이트 이벤트 추가 / 제거 / Clear



        /// <summary>
        /// Alpha 업데이트 모듈에 업데이트 이벤트 <b>추가</b>
        /// </summary>
        public void AddAlpha(Action alphaUpdateEvent)
        {
            WhenBefore_AddUpdateEvent();
            AlphaModule.AddUpdate(alphaUpdateEvent);
        }



        /// <summary>
        /// Alpha 업데이트 모듈에 업데이트 이벤트 <b>제거</b>
        /// </summary>
        public void RemoveAlpha(Action alphaUpdateEvent)
        {
            AlphaModule.RemoveUpdate(alphaUpdateEvent);
            WhenAfter_RemoveUpdateEvent();
        }



        /// <summary>
        /// Alpha 업데이트 모듈 <b>Clear</b>
        /// </summary>
        public void ClearAlpha()
        {
            AlphaModule.ClearUpdate();
            WhenAfter_RemoveUpdateEvent();
        }



        /// <summary>
        /// Beta 업데이트 모듈에 업데이트 이벤트 <b>추가</b>
        /// </summary>
        public void AddBeta(Action betaUpdateEvent)
        {
            WhenBefore_AddUpdateEvent();
            BetaModule.AddUpdate(betaUpdateEvent);
        }



        /// <summary>
        /// Beta 업데이트 모듈에 업데이트 이벤트 <b>제거</b>
        /// </summary>
        public void RemoveBeta(Action betaUpdateEvent)
        {
            BetaModule.RemoveUpdate(betaUpdateEvent);
            WhenAfter_RemoveUpdateEvent();
        }



        /// <summary>
        /// Beta 업데이트 모듈 <b>Clear</b>
        /// </summary>
        public void ClearBeta()
        {
            BetaModule.ClearUpdate();
            WhenAfter_RemoveUpdateEvent();
        }



        /// <summary>
        /// 모든 업데이트 모듈 <b>Clear</b>
        /// </summary>
        public virtual void ClearModules()
        {
            ClearAlpha();
            ClearBeta();
        }



        ///======================================================================================================================================================



        //? 업데이트



        /// <summary>
        ///<see cref="InvokeUpdateEvent"/>를 실행한다
        /// </summary>
        public override void ExecuteModuleUpdate()
        {
            InvokeUpdateEvent?.Invoke();
        }



        ///======================================================================================================================================================



        //? 모듈 업데이트 (각각)



        /// <summary>
        /// Alpha 단계 모듈들을 실행합니다.
        /// </summary>
        protected void ExecuteAlpha()
        {
            AlphaModule.ExecuteModuleUpdate();
        }



        /// <summary>
        /// Beta 단계 모듈들을 실행합니다.
        /// </summary>
        protected void ExecuteBeta()
        {
            BetaModule.ExecuteModuleUpdate();
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 업데이트 모듈 세트 확장 (알파, 베타, 감마)
    /// </summary>
    /// <typeparam name="TMonoBehaviour">대상 MonoBehaviour</typeparam>
    public class UpdateModuleSet_Expand<TMonoBehaviour> : UpdateModuleSet<TMonoBehaviour> where TMonoBehaviour : MonoBehaviour
    {
        ///======================================================================================================================================================



        ///<summary>
        /// 기본 모듈 업데이트 세트 초기화 (capacity 각각 지정)
        /// </summary>
        /// <param name="updateOrderMode">실행 순서 모드</param>
        public UpdateModuleSet_Expand(TMonoBehaviour currentMonobehaviour, EUpdateOrder updateOrderMode,
            int capacity_Alpha_UpdateEvents, int capacity_Alpha_WaitersWillAdd, int capacity_Alpha_WaitersWillRemove,
            int capacity_Beta_UpdateEvents, int capacity_Beta_WaitersWillAdd, int capacity_Beta_WaitersWillRemove,
            int capacity_Gamma_UpdateEvents, int capacity_Gamma_WaitersWillAdd, int capacity_Gamma_WaitersWillRemove
            )
            : base(currentMonobehaviour,
                capacity_Alpha_UpdateEvents, capacity_Alpha_WaitersWillAdd, capacity_Alpha_WaitersWillRemove,
                capacity_Beta_UpdateEvents, capacity_Beta_WaitersWillAdd, capacity_Beta_WaitersWillRemove
                )
        {
            GammaModule = new UpdateModule<TMonoBehaviour>(currentMonobehaviour, capacity_Gamma_UpdateEvents, capacity_Gamma_WaitersWillAdd, capacity_Gamma_WaitersWillRemove);
            UpdateOrderMode = updateOrderMode;
        }



        ///<summary>
        /// 기본 모듈 업데이트 세트 초기화 (Waiters 리스트의 capacity는 capacity의 <b>1/4</b> 로 자동지정)
        /// </summary>
        /// <param name="updateOrderMode">실행 순서 모드</param>
        public UpdateModuleSet_Expand(TMonoBehaviour currentMonobehaviour, EUpdateOrder updateOrderMode,
            int capacity_Alpha,
            int capacity_Beta,
            int capacity_Gamma
            )
            : this(currentMonobehaviour, updateOrderMode,
                capacity_Alpha, capacity_Alpha / 4, capacity_Alpha / 4,
                capacity_Beta, capacity_Beta / 4, capacity_Beta / 4,
                capacity_Gamma, capacity_Gamma / 4, capacity_Gamma / 4
                )
        {

        }



        ///======================================================================================================================================================



        ///<summary>
        ///업데이트 순서 모드, 이 열거형이 변경될때마다, <see cref="UpdateModuleSet{TMonoBehaviour}.InvokeUpdateEvent"/> 가 새롭게 갱신됨
        /// </summary>
        public EUpdateOrder UpdateOrderMode
        {
            get => updateOrderMode;
            set
            {
                if (updateOrderMode == value) { return; }

                switch (value)
                {
                    //. 알파 -> 베타
                    case EUpdateOrder.Alpha_Beta:

                    InvokeUpdateEvent = () =>
                    {
                        ExecuteAlpha();
                        ExecuteBeta();
                    };

                    break;

                    //. 감마 -> 알파 -> 베타
                    case EUpdateOrder.Gamma_Alpha_Beta:

                    InvokeUpdateEvent = () =>
                    {
                        ExecuteExpand();
                        ExecuteAlpha();
                        ExecuteBeta();
                    };

                    break;

                    //. 알파 -> 감마 -> 베타
                    case EUpdateOrder.Alpha_Gamma_Beta:

                    InvokeUpdateEvent = () =>
                    {
                        ExecuteAlpha();
                        ExecuteExpand();
                        ExecuteBeta();
                    };

                    break;

                    //. 알파 -> 베타 -> 감마
                    case EUpdateOrder.Alpha_Beta_Gamma:

                    InvokeUpdateEvent = () =>
                    {
                        ExecuteAlpha();
                        ExecuteBeta();
                        ExecuteExpand();
                    };

                    break;
                }

                updateOrderMode = value;
            }
        }
        private EUpdateOrder updateOrderMode;



        ///======================================================================================================================================================



        /// <summary>
        /// 감마(Expand) 단계 이벤트를 실행하는 모듈입니다.
        /// </summary>
        private readonly UpdateModule<TMonoBehaviour> GammaModule;



        ///======================================================================================================================================================



        //? 모듈 활성화/비활성화, 유효 확인, 실행 메서드



        ///<inheritdoc/>
        protected override bool CheckValid_Modules => base.CheckValid_Modules || GammaModule.Count_UpdateEvents != 0;




        ///======================================================================================================================================================



        //? 업데이트 이벤트 추가 / 제거 / Clear



        /// <summary>
        /// Gamma 업데이트 모듈에 업데이트 이벤트 <b>추가</b>
        /// </summary>
        public void AddGamma(Action gammaUpdateEvent)
        {
            WhenBefore_AddUpdateEvent();
            GammaModule.AddUpdate(gammaUpdateEvent);
        }



        /// <summary>
        /// Gamma 업데이트 모듈에 업데이트 이벤트 <b>제거</b>
        /// </summary>
        public void RemoveGamma(Action gammaUpdateEvent)
        {
            GammaModule.RemoveUpdate(gammaUpdateEvent);
            WhenAfter_RemoveUpdateEvent();
        }



        /// <summary>
        /// Gamma 업데이트 모듈 <b>Clear</b>
        /// </summary>
        public void ClearGamma()
        {
            GammaModule.ClearUpdate();
            WhenAfter_RemoveUpdateEvent();
        }



        /// <summary>
        /// Alpha, Beta, Expand 모듈을 모두 클리어하고 비활성화합니다.
        /// </summary>
        public override void ClearModules()
        {
            base.ClearModules();
            ClearGamma();
        }



        ///======================================================================================================================================================



        //? 모듈 업데이트



        /// <summary>
        /// 감마(Expand) 단계 모듈을 실행합니다.
        /// </summary>
        protected void ExecuteExpand()
        {
            GammaModule.ExecuteModuleUpdate();
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================
}