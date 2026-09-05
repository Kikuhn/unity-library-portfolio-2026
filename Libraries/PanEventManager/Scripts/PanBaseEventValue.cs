using UnityEngine;
using Pan.Util;
using System;
using Sirenix.OdinInspector;
using Cysharp.Threading.Tasks;


//? PanBaseEventValue



namespace Pan.Event
{
    /// <summary>
    /// 판 이벤트 밸류의 <b>베이스</b> 클래스
    /// <para>모든 이벤트 밸류는 이 클래스를 상속받아 사용된다</para>
    /// <para><see cref="Base{TTarget, TEventValueSelf}"/> 또는 <see cref="EventAbles{TEventValue}"/>를 상속받아 구현해야한다</para>
    /// </summary>
    [Serializable]
    public abstract partial class PanBaseEventValue : ICreateCount
    {
        ///======================================================================================================================================================



        //? 생성 개수 / 타입 캐싱



        /// <summary>
        /// 이벤트 밸류가 생성 개수 계약을 제공할 때 사용하는 기본값입니다.
        /// <para>ScriptableObject 설정 없이 초기화할 때는 타입 인스턴스를 만들지 않으므로 이 값이 자동으로 조회되지 않습니다.</para>
        /// <para>미리 생성할 개수는 초기화 설정의 <c>CreateCount</c> 또는 <see cref="PanEventValueManager.CachingEventValue{TEventValue}(int)"/>로 명시합니다.</para>
        /// </summary>
        public virtual int CreateCount { get; } = 0;



        ///======================================================================================================================================================



        //? EventAble  관리



        /// <summary> 
        /// 이 이벤트 밸류와 연결되있는 <see cref="EventAble"/>
        /// <para>이 이밴트 밸류를 활성화 할때, 할당 되어야 한다</para>
        /// </summary>
        public EventAble CurrentEventAble { get; protected set; } = null;



        /// <summary>
        /// 이 이밴트 밸류가 활성화 되어있는가?
        /// <para><see cref="CurrentEventAble"/>의 null 여부에 따라 결정된다</para>
        /// <para><see cref="CurrentEventAble"/>는 활성화 될때 할당 되기 때문에 이를 통해 구분한다</para>
        /// <para>비활성화 되어있다는 것은, 이 객체가 <see cref="EventAble"/> 내부에 존재하지 않는다는 뜻</para>
        /// </summary>
        public bool Valid_CurrentEventAble => CurrentEventAble != null;



        private bool isDisabling;



        ///======================================================================================================================================================



        //? 이벤트 밸류 활성화 / 활성화



        /// <summary>
        /// 이 이벤트 밸류를 <b>비활성화</b> 한다
        /// </summary>
        /// <param name="executeFromEventAbleForRemove">
        /// <see cref="EventAble"/>에서 이 메서드가 호출되어, <see cref="EventAble"/>의 이벤트 테이블에서 이 이벤트 밸류를 제거할 필요가 없을경우 할당한다
        /// <para><see cref="EventAble"/>의 제거로직 에서 호출되므로, this를 넣으면 된다</para>
        /// </param>
        public abstract bool DisableValue(EventAble executeFromEventAbleForRemove = null);



        ///======================================================================================================================================================



        //? [Protected] 이벤트 밸류 비활성화 (내수용)



        /// <summary>
        ///이 객체가 Enable와 같은 활성화 메서드에서, 조건에 맞지않아 즉시 비활성화 할 필요가 있을때 내부에서 호출한다
        ///<para>별도로 구분하는 이유는, 입구 컷이 났기 때문에 Disable 등의 이벤트 호출이 필요 없기 때문</para>
        /// </summary>
        protected abstract void DisableCurrentNow();



        ///======================================================================================================================================================



        //? [Protected] 이벤트 밸류 활성화 / 비활성화



        /// <summary>
        /// 이 이벤트 밸류 활성화
        /// <para>예외를 던질 수 있는 구현은 구독이나 외부 상태 변경을 스스로 원자적으로 처리하거나 직접 복구해야 합니다.</para>
        /// <para>코어는 활성화 실패 시 소유자 참조, 이벤트 테이블, 풀 소유권만 복구합니다.</para>
        /// </summary>
        protected virtual void Enable() { }



        /// <summary>
        /// 이 이벤트 밸류 비활성화
        /// </summary>
        protected virtual void Disable() { }



        ///======================================================================================================================================================



        //? Enable, Disable 되기 전 또는 후에 작동하는 함수는 이곳에



        private void ExecuteEnable()
        {
            Enable();

            //. 에디터 환경에서, 테스트를 위한 업데이터를 실행한다
#if UNITY_EDITOR
            if (UseCurrentUpdateEditorForTest)
            {
                editorUpdater ??= new UniTaskUpdater();
                editorUpdater.ExecuteUpdate(CurrentUpdateEditorForTest, PlayerLoopTiming.Update);
            }
#endif
        }



        private void ExecuteDisable()
        {
            //. 에디터 환경에서, 테스트를 위한 업데이터를 종료한다
#if UNITY_EDITOR
            if (UseCurrentUpdateEditorForTest)
            {
                editorUpdater?.QuitUpdate();
            }
#endif

            EventAble eventAble = CurrentEventAble;
            PanBaseEventValue previousDisablingValue = eventAble.IsDisablingEventValue;
            eventAble.IsDisablingEventValue = this;
            isDisabling = true;
            try
            {
                Disable();
            }
            finally
            {
                isDisabling = false;
                eventAble.IsDisablingEventValue = previousDisablingValue;
            }
        }



        ///======================================================================================================================================================



        //. 에디터 환경에서 테스트를 위한 업데이터



#if UNITY_EDITOR



        [Obsolete("에디터에서만 테스트로 사용, 주의", false)]
        private UniTaskUpdater editorUpdater;



        [Obsolete("에디터에서만 테스트로 사용, 주의", false)]
        protected virtual bool UseCurrentUpdateEditorForTest => false;



        [Obsolete("에디터에서만 테스트로 사용, 주의", false)]
        protected virtual void CurrentUpdateEditorForTest() { }



#endif



        ///======================================================================================================================================================
    }



    public abstract partial class PanBaseEventValue
    {
        #region 인터페이스

        /// <summary>
        /// <see cref="EventAble"/> 에서 사용하기 위한, <see cref="PanBaseEventValue"/> 를 활성화시키는 <see cref="EnableValue(TTarget)"/> 를 보유중인 인터페이스
        /// </summary>
        /// <typeparam name="TTarget"></typeparam>
        public interface IEnableValue<TTarget> where TTarget : class, IEventAble
        {
            bool EnableValue(TTarget current);
        }



        /// <summary>
        /// <see cref="EventAble"/> 에서 사용하기 위한, <see cref="PanBaseEventValue"/> 를 캐스팅 없이 강제로 활성화시키는 인터페이스
        /// </summary>
        public interface IEnableValueNonCasting
        {
            Type GetCurrentType { get; }
            bool EnableValue(object current);
        }



        /// <summary>
        /// 이 인터페이스를 상속받은 <see cref="PanBaseEventValue"/>는 <see cref="IEnableValue{TTarget}.EnableValue(TTarget)"/>를 실행함에 있어서
        /// <para><b>조건 심사</b>가 존재하여, 활성화에 실패 할 수도 있음</para>
        /// </summary>
        /// <typeparam name="TTarget"></typeparam>
        public interface IEnableCondition<TTarget> where TTarget : class, IEventAble
        {
            ///<summary>
            /// 이 이벤트 밸류가 "활성화" 되기 위한 <b>조건심사</b>
            /// <para>이 결과값에 따라, 이벤트 밸류가 "활성화"에 실패 할 수도 있음</para>
            /// <para><b>절대 이 메서드 내에서 <see cref="PanBaseEventValue.Base{TEventValueSelf, TTarget}.Current"/>를 호출하면 안됨! 할당 되기 직전이기에 무조건 null을 반환함</b></para>
            /// </summary>
            bool EnableValueCondition(TTarget current);
        }



        /// <summary>
        /// 이 인터페이스를 상속받은 <see cref="PanBaseEventValue"/>는 "활성화 조건 심사"가 존재 하지 않기로 약속
        /// </summary>
        public interface INotUseEnableCondition { }

        #endregion



        #region 베이스의 베이스 (제네릭)

        /// <summary>
        /// 이밴트 밸류 : <para><typeparamref name="TTarget"/>을 사용중인 <see cref="EventAble.Master"/> 전용 이벤트 밸류의 메이스</para>
        /// <para><i>사실상 이게 진짜 베이스, 제네릭을 사용하기에 제약 조건으로서는 활용이 힘들어서 분리</i></para>
        /// </summary>
        /// <typeparam name="TEventValueSelf">CRTP 패턴</typeparam>
        /// <typeparam name="TTarget">대상 타겟 제네릭, <see cref="IEventAble"/>를 상속받는 Class 여야 함</typeparam>

        [Serializable]
        public abstract class Base<TEventValueSelf, TTarget> : PanBaseEventValue, IEnableValue<TTarget>, IEnableValueNonCasting
            where TEventValueSelf : Base<TEventValueSelf, TTarget>, new()
            where TTarget : class, IEventAble
        {
            ///======================================================================================================================================================



            //? 대상



            /// <summary>
            /// 이 이밴트 밸류를 가지고있는 대상 (<see cref="IEventAble"/>를 보유중인 클래스)
            /// <para><see cref="EnableValue"/> 에서 할당된다</para>
            /// </summary>
            protected TTarget Current { get; private set; } = null;



#if UNITY_EDITOR



            //. CRTP의 닫힌 제네릭 타입마다 한 번만 만들고 모든 인스턴스가 같은 rich-text 헤더를 재사용한다
            private static readonly string editorActiveInfo = $"<color=#4fc1e9><b>{typeof(TEventValueSelf).Name}</b></color>";
            private static readonly string editorInactiveInfo = $"<color=#ed5565><b>{typeof(TEventValueSelf).Name}</b></color>";



            [HideInInspector]
            [HorizontalGroup("가로그룹", 0.4f)]
            [HideLabel]
            [EnableGUI, DisplayAsString(EnableRichText = true)]
            [PropertyOrder(-1000)]
            private string Editor_Info
            {
                get
                {
                    if (Valid_CurrentEventAble)
                    {
                        return editorActiveInfo;
                        //return $"<color=#4fc1e9><b>{GetType().Name}</b></color> <size=10><i>({GetType().FullName} {GetHashCode()})</i></size>";
                    }
                    else
                    {

                        return editorInactiveInfo;
                        //return $"<color=#ed5565><b>{GetType().Name}</b></color> <size=10><i>({GetType().FullName} {GetHashCode()})</i></size>";
                    }
                }
            }



            [HideInInspector]
            [HorizontalGroup("가로그룹", 0.3f)]
            [HideLabel]//[LabelText("대상 Current")]
            [PropertyOrder(-999)]
            private TTarget Editor_Current => Current;



#endif



            ///// <summary>
            ///// <see cref="Current"/>의 캐스팅 조건 확인
            ///// </summary>
            ///// <typeparam name="T"></typeparam>
            ///// <param name="current"></param>
            ///// <param name="resultCastingCurrent"></param>
            ///// <returns></returns>
            //public bool CurrentTryCasting<T>(TTarget current, out T resultCastingCurrent)
            //{
            //    if (current is T casted)
            //    {
            //        resultCastingCurrent = casted;
            //        return true;
            //    }

            //    resultCastingCurrent = default!;
            //    return false;
            //}



            ///====================================================================================================================================================== <summary>



            //? 이밴트 밸류 활성화 / 비활성화



            /// <summary>
            /// 이 이벤트 밸류의 <b>활성화 조건</b> 사용 여부
            /// <para>하위 추상화 클래스에서, sealed로 이 상태를 결정 권장</para>
            /// <para>true: 이 이벤트 객체는 <see cref="EnableValue(TTarget)"/>가 활성화 조건에 의해 실패 할 수도 있음</para>
            /// <para>false: 이 이벤트 객체는 <see cref="EnableValue(TTarget)"/>에 있어서 활성화 조건을 고려하지 않음</para>
            /// </summary>
            protected abstract bool UseEnableCondition { get; }



            /// <summary>
            /// 이 이벤트 밸류를 <b>활성화</b> 한다
            /// </summary>
            /// <param name="current"></param>
            public bool EnableValue(TTarget current)
            {
                //! 이미 이벤트 밸류가 활성화가 되어있다면 실패하되, false를 반환 하지는 않는다
                if (Valid_CurrentEventAble) { return true; }


                //! 활성화 조건을 통과하지 못하면 소유자 참조를 설정하지 않는다
                if (!CheckEnableCondition(current)) { return false; }


                EnableValueAfterCondition(current);
                return true;
            }



            /// <summary>
            /// 현재 대상이 이 이벤트 밸류의 활성화 조건을 만족하는지 확인합니다.
            /// </summary>
            /// <param name="current">활성화 대상입니다.</param>
            /// <returns>조건을 사용하지 않거나 조건을 통과하면 <c>true</c>입니다.</returns>
            internal bool CheckEnableCondition(TTarget current)
            {
                if (!UseEnableCondition) { return true; }

                EventAble conditionOwner = current.EventAble;
                Type eventValueType = typeof(TEventValueSelf);

                //! 같은 소유자에서 동일 타입의 조건을 이미 평가 중이면 재귀 호출을 실패로 끊는다
                if (!conditionOwner.TryBeginEnableCondition(eventValueType)) { return false; }


                try
                {
                    if (this is IEnableCondition<TTarget> typedCondition)
                    {
                        return typedCondition.EnableValueCondition(current);
                    }

                    //? 커스텀 대상용 조건 베이스는 공개 계약상 IEventAble 조건을 사용하므로 호환 경로를 유지한다
                    if (this is IEnableCondition<IEventAble> eventAbleCondition)
                    {
                        return eventAbleCondition.EnableValueCondition(current);
                    }

                    return true;
                }
                finally
                {
                    conditionOwner.EndEnableCondition(eventValueType);
                }
            }



            /// <summary>
            /// 조건 검사가 끝난 이벤트 밸류에 소유자를 연결하고 활성화 콜백을 실행합니다.
            /// <para>파생 <see cref="PanBaseEventValue.Enable"/> 구현은 예외를 던지기 전에 자신이 만든 외부 부작용을 직접 복구해야 합니다.</para>
            /// <para>이 메서드는 코어가 소유하는 대상 참조만 원자적으로 복구합니다.</para>
            /// </summary>
            /// <param name="current">활성화 대상입니다.</param>
            internal void EnableValueAfterCondition(TTarget current)
            {
                if (Valid_CurrentEventAble) { return; }

                CurrentEventAble = current.EventAble;
                Current = current;


                try
                {
                    ExecuteEnable();
                }
                catch
                {
                    //! 활성화 콜백 실패 시 풀로 되돌릴 수 있도록 코어 소유자 참조를 즉시 복구한다
                    CurrentEventAble = null;
                    Current = null;
                    throw;
                }
            }



            Type IEnableValueNonCasting.GetCurrentType => typeof(TTarget);



            bool IEnableValueNonCasting.EnableValue(object current) => EnableValue(current as TTarget);



            /// <inheritdoc/>
            public sealed override bool DisableValue(EventAble executeFromEventAbleForRemove = null)
            {
                //! 이미 이벤트 밸류가 비활성화가 되어있거나,
                //! EventAble에서 제거를 위해 호출되었으나 그 EventAble이 CurrentEventAble와 다르다면 실패한다
                if (!Valid_CurrentEventAble ||
                    isDisabling ||
                    (executeFromEventAbleForRemove != null && CurrentEventAble != executeFromEventAbleForRemove)) { return false; }


                //? "비활성화" 메서드를 실행한다
                ExecuteDisable();



                //? 이 이벤트 밸류에 직접 접근하여 "비활성화" 시키는 것이라면 (EventAble로 부터 "제거"를 위해 "비활성화"가 호출 된 것이 아님)
                if (executeFromEventAbleForRemove == null)
                {
                    //. EventAble에서도 이 이벤트 밸류를 "제거"할 필요가 있으니 "제거"를 하되,
                    //. 그곳에서 "비활성화" 메서드는 실행시키지 않게끔 한다 (여기에서 "비활성화" 메서드가 이미 실행 됨)
                    CurrentEventAble.RemoveValue(this, false);
                }


                //? CurrentEventAble, Current 할당을 해제한다
                CurrentEventAble = null;
                Current = null;


                return true;
            }



            /// <summary>Enable에서 입구컷에 날때 사용하는 Disable, Disable액션 외의 나머지액션들을 수행</summary>
            protected sealed override void DisableCurrentNow()
            {
                CurrentEventAble.RemoveValue(this, false);
                CurrentEventAble = null;
                Current = null;
            }



            ///======================================================================================================================================================



            //? Static



            ///<summary>
            /// <typeparamref name="TEventValue"/>을 <b>엿본다</b>
            /// <para><typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
            /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
            /// <para><see cref="Master"/>를 <typeparamref name="TTarget"/>으로 캐스팅을 <b>시도한다</b> (실패시 false)</para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, "활성화"를 <b>시도한다</b> (실패시 false, 이미 활성화가 되어있다면 <b>성공</b>)</para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면, <b>실패한다</b> (실패시 false)</para>
            ///</summary>
            public static bool TryPeek(TTarget target, out TEventValueSelf resultValue)
            {
                return target.EventAble.TryPeek<TEventValueSelf, TTarget>(out resultValue);
            }



            ///<summary>
            /// <typeparamref name="TEventValue"/>을 <b>엿본다</b>
            /// <para><typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
            /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
            /// <para><see cref="Master"/>를 <typeparamref name="TTarget"/>으로 캐스팅을 <b>시도한다</b> (실패시 null)</para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, "활성화"를 <b>시도한다</b> (실패시 null, 이미 활성화가 되어있다면 <b>성공</b>)</para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면, <b>실패한다</b> (실패시 null)</para>
            ///</summary>
            public static TEventValueSelf Peek(TTarget target)
            {
                return target.EventAble.Peek<TEventValueSelf, TTarget>();
            }


            ///<summary>
            /// <typeparamref name="TEventValue"/>을 <b>얻는다</b>
            /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
            /// <para><see cref="Master"/>를 <typeparamref name="TTarget"/>으로 캐스팅을 <b>시도한다</b> (실패시 false)</para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, "활성화"를 <b>시도한다</b> (실패시 false, 이미 활성화가 되어있다면 <b>성공</b>)</para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면, 새롭게 이벤트 밸류를 꺼낸 후, "활성화"를 <b>시도한다</b> (실패시 다시 반환, false)</para>
            ///</summary>
            public static bool TryGain(TTarget target, out TEventValueSelf resultValue)
            {
                return target.EventAble.TryGain<TEventValueSelf, TTarget>(out resultValue);
            }


            ///<summary>
            /// <typeparamref name="TEventValue"/>을 <b>얻는다</b>
            /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
            /// <para><see cref="Master"/>를 <typeparamref name="TTarget"/>으로 캐스팅을 <b>시도한다</b> (실패시 false)</para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, "활성화"를 <b>시도한다</b> (실패시 false, 이미 활성화가 되어있다면 <b>성공</b>)</para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면, 새롭게 이벤트 밸류를 꺼낸 후, "활성화"를 <b>시도한다</b> (실패시 다시 반환, false)</para>
            ///</summary>
            public static TEventValueSelf Gain(TTarget target)
            {
                return target.EventAble.Gain<TEventValueSelf, TTarget>();
            }



            ///<summary>
            ///이벤트 밸류를 <b>제거한다</b>
            ///</summary>
            public static void Remove(TTarget target)
            {
                target.EventAble.RemoveValue<TEventValueSelf>();
            }



            ///======================================================================================================================================================
        }

        #endregion



        #region Current로 IEventAble를 직접 사용하는 이벤트 밸류

        /// <summary>
        /// <see cref="Base{TEventValueSelf, TTarget}.Current"/>가 직접적으로 <see cref="IEventAble"/>로 지정 되어있는 이벤트 밸류의 베이스
        /// <para><see cref="Enable"/>가 실패하지 않는다</para>
        /// </summary>
        /// <typeparam name="TEventValueSelf">CRTP 패턴</typeparam>
        [Serializable]
        public abstract class EventAbles<TEventValueSelf> : Base<TEventValueSelf, IEventAble>, INotUseEnableCondition
            where TEventValueSelf : EventAbles<TEventValueSelf>, new()
        {
            ///======================================================================================================================================================



            ///<inheritdoc/>
            protected sealed override bool UseEnableCondition => false;



            ///======================================================================================================================================================



            //? Static (활성화 실패 가능성이 없기에 Require 사용 가능)



            ///<summary>
            /// <typeparamref name="TEventValue"/>을 <b>무조건 반환</b>한다
            /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 반드시 <b>보장</b>된다</para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, 반환한다</para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면,  <see cref="EventValueManager"/>에서 꺼내 테이블에 넣은 뒤, 반환한다</para>
            /// <para><i><see cref="Master"/>인 <see cref="IEventAble"/>를 직접적으로 사용하며, "활성화 조건 심사"가 존재 하지 않기에 무조건 반환이 가능</i></para>
            ///</summary>
            public static void RequireOut(IEventAble target, out TEventValueSelf resultValue)
            {
                target.EventAble.RequireOut(out resultValue);
            }



            ///<summary>
            /// <typeparamref name="TEventValue"/>을 <b>무조건 반환</b>한다
            /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 반드시 <b>보장</b>된다</para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, 반환한다</para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면,  <see cref="EventValueManager"/>에서 꺼내 테이블에 넣은 뒤, 반환한다</para>
            /// <para><i><see cref="Master"/>인 <see cref="IEventAble"/>를 직접적으로 사용하며, "활성화 조건 심사"가 존재 하지 않기에 무조건 반환이 가능</i></para>
            ///</summary>
            public static TEventValueSelf Require(IEventAble target)
            {
                return target.EventAble.Require<TEventValueSelf>();
            }



            ///<summary>
            /// <typeparamref name="TEventValue"/>을 <b>엿본다</b>
            /// <para><typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
            /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, "활성화"를 <b>시도한다</b> (실패시 false, 이미 활성화가 되어있다면 <b>성공</b>)</para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면, <b>실패한다</b> (실패시 false)</para>
            ///</summary>
            public static new bool TryPeek(IEventAble target, out TEventValueSelf resultValue)
            {
                return target.EventAble.TryPeek<TEventValueSelf>(out resultValue);
            }



            ///<summary>
            /// <typeparamref name="TEventValue"/>을 <b>엿본다</b>
            /// <para><typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
            /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, "활성화"를 <b>시도한다</b> (실패시 null, 이미 활성화가 되어있다면 <b>성공</b>)</para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면, <b>실패한다</b> (실패시 null)</para>
            ///</summary>
            public static new TEventValueSelf Peek(IEventAble target)
            {
                return target.EventAble.Peek<TEventValueSelf>();
            }


            ///<summary>
            /// <typeparamref name="TEventValue"/>을 <b>얻는다</b>
            /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, "활성화"를 <b>시도한다</b> (실패시 false, 이미 활성화가 되어있다면 <b>성공</b>)</para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면, 새롭게 이벤트 밸류를 꺼낸 후, "활성화"를 <b>시도한다</b> (실패시 다시 반환, false)</para>
            ///</summary>
            public static new bool TryGain(IEventAble target, out TEventValueSelf resultValue)
            {
                return target.EventAble.TryGain<TEventValueSelf>(out resultValue);
            }


            ///<summary>
            /// <typeparamref name="TEventValue"/>을 <b>얻는다</b>
            /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, "활성화"를 <b>시도한다</b> (실패시 false, 이미 활성화가 되어있다면 <b>성공</b>)</para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면, 새롭게 이벤트 밸류를 꺼낸 후, "활성화"를 <b>시도한다</b> (실패시 다시 반환, false)</para>
            ///</summary>
            public static new TEventValueSelf Gain(IEventAble target)
            {
                return target.EventAble.Gain<TEventValueSelf>();
            }



            ///======================================================================================================================================================
        }



        /// <summary>
        /// <see cref="Base{TEventValueSelf, TTarget}.Current"/>가 직접적으로 <see cref="IEventAble"/>로 지정 되어있는 이벤트 밸류의 베이스
        /// <para><see cref="UseEnableCondition"/> 와 <see cref="IEnableCondition{TTarget}"/>이 가용되어 <see cref="Enable"/>가 실패 할 가능성이 존재한다</para>
        /// <para>보통은 <see cref="Current"/>의 객체 타입 확인을 위해 사용한다 (특정 인터페이스/컴포넌트)를 상속받는가? 등등)</para>
        /// <para><i>(특정 인터페이스/컴포넌트)를 상속받는가? 등등)</i></para>
        /// </summary>
        /// <typeparam name="TEventValueSelf">CRTP 패턴</typeparam>
        [Serializable]
        public abstract class EventAblesCondition<TEventValueSelf> : Base<TEventValueSelf, IEventAble>, IEnableCondition<IEventAble>
            where TEventValueSelf : EventAblesCondition<TEventValueSelf>, new()
        {
            ///======================================================================================================================================================



            ///<inheritdoc/>
            protected sealed override bool UseEnableCondition => true;



            ///<inheritdoc/>
            public abstract bool EnableValueCondition(IEventAble current);



            ///======================================================================================================================================================



            //? Static



            ///<summary>
            /// <typeparamref name="TEventValue"/>을 <b>엿본다</b>
            /// <para><typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
            /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, "활성화"를 <b>시도한다</b> (실패시 false, 이미 활성화가 되어있다면 <b>성공</b>)</para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면, <b>실패한다</b> (실패시 false)</para>
            ///</summary>
            public static new bool TryPeek(IEventAble target, out TEventValueSelf resultValue)
            {
                return target.EventAble.TryPeek<TEventValueSelf>(out resultValue);
            }



            ///<summary>
            /// <typeparamref name="TEventValue"/>을 <b>엿본다</b>
            /// <para><typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
            /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, "활성화"를 <b>시도한다</b> (실패시 null, 이미 활성화가 되어있다면 <b>성공</b>)</para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면, <b>실패한다</b> (실패시 null)</para>
            ///</summary>
            public static new TEventValueSelf Peek(IEventAble target)
            {
                return target.EventAble.Peek<TEventValueSelf>();
            }



            ///<summary>
            /// <typeparamref name="TEventValue"/>을 <b>얻는다</b>
            /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, "활성화"를 <b>시도한다</b> (실패시 false, 이미 활성화가 되어있다면 <b>성공</b>)</para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면, 새롭게 이벤트 밸류를 꺼낸 후, "활성화"를 <b>시도한다</b> (실패시 다시 반환, false)</para>
            ///</summary>
            public static new bool TryGain(IEventAble target, out TEventValueSelf resultValue)
            {
                return target.EventAble.TryGainCondition<TEventValueSelf>(out resultValue);
            }



            ///<summary>
            /// <typeparamref name="TEventValue"/>을 <b>얻는다</b>
            /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, "활성화"를 <b>시도한다</b> (실패시 false, 이미 활성화가 되어있다면 <b>성공</b>)</para>
            /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면, 새롭게 이벤트 밸류를 꺼낸 후, "활성화"를 <b>시도한다</b> (실패시 다시 반환, false)</para>
            ///</summary>
            public static new TEventValueSelf Gain(IEventAble target)
            {
                return target.EventAble.GainCondition<TEventValueSelf>();
            }



            ///======================================================================================================================================================
        }

        #endregion



        #region Current로 IEventAble를 상속 받는 커스텀 객체를 사용하는 이벤트 밸류

        /// <summary>
        /// <see cref="Base{TEventValueSelf, TTarget}.Current"/>가 <see cref="IEventAble"/>를 상속받는 <b>특정 커스텀 객체</b>로 지정 되어있는 이벤트 밸류의 베이스
        /// <para>이 이벤트 밸류를 불러오는데에 있어, <see cref="EventAble.Master"/>의 <typeparamref name="TTarget"/> 캐스팅 실패 가능성이 존재한다</para>
        /// <para><see cref="Enable"/>가 실패하지는 않는다</para>
        /// </summary>
        /// <typeparam name="TEventValueSelf">CRTP 패턴</typeparam>
        [Serializable]
        public abstract class CustomEventAbles<TEventValueSelf, TTarget> : Base<TEventValueSelf, TTarget>, INotUseEnableCondition
            where TEventValueSelf : CustomEventAbles<TEventValueSelf, TTarget>, new()
            where TTarget : class, IEventAble
        {
            ///======================================================================================================================================================



            ///<inheritdoc/>
            protected sealed override bool UseEnableCondition => false;



            ///======================================================================================================================================================
        }



        /// <summary>
        /// <see cref="Base{TEventValueSelf, TTarget}.Current"/>가 <see cref="IEventAble"/>를 상속받는 <b>특정 커스텀 객체</b>로 지정 되어있는 이벤트 밸류의 베이스
        /// <para>이 이벤트 밸류를 불러오는데에 있어, <see cref="EventAble.Master"/>의 <typeparamref name="TTarget"/> 캐스팅 실패 가능성이 존재한다</para>
        /// <para><see cref="UseEnableCondition"/> 와 <see cref="IEnableCondition{TTarget}"/>이 가용되어 <see cref="Enable"/>가 실패 할 가능성이 존재한다</para>
        /// </summary>
        /// <typeparam name="TEventValueSelf">CRTP 패턴</typeparam>
        [Serializable]
        public abstract class CustomEventAblesCondition<TEventValueSelf, TTarget> : Base<TEventValueSelf, TTarget>, IEnableCondition<IEventAble>
            where TEventValueSelf : CustomEventAblesCondition<TEventValueSelf, TTarget>, new()
            where TTarget : class, IEventAble
        {
            ///======================================================================================================================================================



            ///<inheritdoc/>
            protected sealed override bool UseEnableCondition => true;

            ///<inheritdoc/>
            public abstract bool EnableValueCondition(IEventAble current);



            ///======================================================================================================================================================
        }

        #endregion
    }
}
