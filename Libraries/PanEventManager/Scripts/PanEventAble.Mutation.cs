using System;
using System.Collections.Generic;
using UnityEngine;



namespace Pan.Event
{
    public partial class EventAble
    {
        /// <summary>
        /// 현재 활성화 조건을 평가 중인 이벤트 밸류 타입입니다.
        /// <para>조건 콜백에서 같은 타입을 다시 요청해도 재귀 평가나 추가 풀 대여가 발생하지 않게 합니다.</para>
        /// </summary>
        private HashSet<Type> EvaluatingEnableConditionTypes;
        /// <summary>
        /// 지정한 타입의 활성화 조건 평가를 시작합니다.
        /// </summary>
        /// <param name="eventValueType">평가할 이벤트 밸류 타입입니다.</param>
        /// <returns>같은 타입을 아직 평가 중이지 않아 시작할 수 있으면 <c>true</c>입니다.</returns>
        internal bool TryBeginEnableCondition(Type eventValueType)
        {
            EvaluatingEnableConditionTypes ??= new HashSet<Type>();
            return EvaluatingEnableConditionTypes.Add(eventValueType);
        }



        /// <summary>
        /// 지정한 타입의 활성화 조건 평가가 끝났음을 기록합니다.
        /// </summary>
        /// <param name="eventValueType">평가를 마친 이벤트 밸류 타입입니다.</param>
        internal void EndEnableCondition(Type eventValueType)
        {
            EvaluatingEnableConditionTypes?.Remove(eventValueType);
        }



        /// <summary>
        /// 지정한 타입의 활성화 조건을 현재 평가 중인지 확인합니다.
        /// </summary>
        /// <param name="eventValueType">확인할 이벤트 밸류 타입입니다.</param>
        /// <returns>평가 중이면 <c>true</c>입니다.</returns>
        private bool IsEvaluatingEnableCondition(Type eventValueType)
        {
            return EvaluatingEnableConditionTypes?.Contains(eventValueType) == true;
        }
        ///======================================================================================================================================================



        //? 디버깅



        /// <summary>
        /// 디버깅: <see cref="Require{TEventValue}"/>가 이벤트 밸류의 "비활성화" 도중 실행 되었음을 출력
        /// </summary>
        /// <param name="type"></param>
        private void DebugMsg_CalledRequireMethod_WhenDisablingEventValue(Type type)
        {
            //. 이 메서드가 EventValue의 Disable 도중에 호출되었고, 이를 검수하는 디버그모드가 활성화 되어있을경우, 메시지를 출력한다
            if (IsDisablingEventValue != null && Debug_CheckAddEventValue_WhenEventValueDisableing)
            {
                Debug.LogWarning($"<color=red>PanEventAble 디버깅</color>\t{IsDisablingEventValue.GetType().Name}의 Disable 도중, {type.Name} 가 Require로 호출됨");
            }
        }



        /// <summary>
        /// 디버깅: "이벤트 테이블에 새로운 이벤트 밸류를 추가하려는 시도"가 이벤트 밸류의 "비활성화" 도중 실행 되었음을 출력
        /// </summary>
        /// <param name="type"></param>
        private void DebugMsg_AddedEventValue_WhenDisablingEventValue(Type type)
        {
            //. 이 메서드가 EventValue의 Disable 도중에 호출되었고, 이를 검수하는 디버그모드가 활성화 되어있을경우, 메시지를 출력한다
            if (IsDisablingEventValue != null && Debug_CheckAddEventValue_WhenEventValueDisableing)
            {
                Debug.LogWarning($"<color=red>PanEventAble 디버깅</color>\t{IsDisablingEventValue.GetType().Name}의 Disable 도중, {type.Name} 가 추가됨");
            }
        }



        ///======================================================================================================================================================



        //? 이벤트 밸류 반환 내부 메서드



        ///<summary>
        /// <typeparamref name="TEventValue"/>을 <b>무조건 반환</b>한다
        /// <para><typeparamref name="TEventValue"/>를 반환하는것이 반드시 <b>보장</b>된다</para>
        /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 반드시 <b>보장</b>된다</para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, 반환한다</para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면,  <see cref="EventValueManager"/>에서 꺼내 테이블에 넣은 뒤, 반환한다</para>
        ///</summary>
        ///<param name="type"><b>반드시<see cref="PanBaseEventValue.INotUseEnableCondition"/> 을 상속 받는, 활성화 조건을 사용하지 않는 Type이여야 한다</b></param>
        ///<param name="master"><typeparamref name="TTarget"/>으로 캐스팅 된 <see cref="Master"/>, 미리 캐스팅 성공 여부 검사 후 할당 필수</param>
        private TEventValue RequireInternal<TEventValue, TTarget>(Type type, TTarget master)
            where TEventValue : PanBaseEventValue, PanBaseEventValue.IEnableValue<TTarget>, PanBaseEventValue.INotUseEnableCondition, new()
            where TTarget : class, IEventAble
        {
            DebugMsg_CalledRequireMethod_WhenDisablingEventValue(type); //. 이밴트 밸류의 비활성화 메서드 내에서 이 Require 메서드가 실행된 것을 디버깅


            //? #1 테이블에 해당 이벤트 밸류가 존재 한다면, 그것을 반환한다
            if (EventValueTables.TryGetEventValue(false, type, out var gottenEventValue))
            {
                //. 얻어온 이벤트 밸류를 TEventValue로 캐스팅한다
                var eventValue = gottenEventValue as TEventValue;


                //. 테이블에서 얻어 왔음에도, 비활성화 되어있다면 다시 활성화해준다 (활성화 조건이 무조건 true이기에 확정 활성화)
                if (!eventValue.Valid_CurrentEventAble) { eventValue.EnableValue(master); }


                return eventValue;
            }


            //? #2 이벤트 밸류의 "비활성화" 메서드 도중에 실행된 것이 아니고,
            //? 테이블에 해당 이벤트 밸류가 존재 하지 않는다면
            //? 이벤트 밸류 매니저에서 새롭게 꺼내 테이블에 넣고, 반환한다
            else if (IsDisablingEventValue == null)
            {
                //. 새롭게 이벤트 밸류를 꺼낸다
                TEventValue eventValue = EventValueManager.PopEventValue<TEventValue>(1);
                bool tableAdded = false;


                try
                {
                    //? 활성화 콜백의 동일 타입 Require 재진입이 같은 인스턴스를 찾도록 먼저 테이블에 예약한다
                    EventValueTables.AddEventTable(type, eventValue);
                    tableAdded = true;

                    if (eventValue.EnableValue(master) &&
                        EventValueTables.TryGetEventValue(false, type, out var attachedValue) &&
                        ReferenceEquals(attachedValue, eventValue) &&
                        eventValue.Valid_CurrentEventAble)
                    {
                        return eventValue;
                    }
                }
                catch
                {
                    bool reservationRemoved = tableAdded && EventValueTables.RemoveEventTable(type, eventValue);
                    if (!tableAdded || reservationRemoved)
                    {
                        EventValueManager.PushEventValue(type, eventValue);
                    }
                    throw;
                }


                bool removed = EventValueTables.RemoveEventTable(type, eventValue);
                if (removed) { EventValueManager.PushEventValue(type, eventValue); }

                throw new InvalidOperationException($"{type.Name} 활성화가 완료되기 전에 이벤트 테이블에서 제거되었습니다.");
            }


            //! #3  이벤트 밸류의 "비활성화" 메서드 도중 테이블에 존재 하지 않는 이벤트 밸류를 추가 하려는 시도를 출력 하는 에러
            DebugMsg_AddedEventValue_WhenDisablingEventValue(type);
            return null;
        }



        ///<summary>
        /// <typeparamref name="TEventValue"/>을 <b>획득시도</b>한다
        /// <para><typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
        /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
        /// <para><see cref="Master"/>를 <typeparamref name="TTarget"/>으로 캐스팅을 <b>시도한다</b> (실패시 false)</para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, "활성화"를 <b>시도한다</b> (실패시 false, 이미 활성화가 되어있다면 <b>성공</b>)</para>
        /// <para><paramref name="isGain"/>이 false이고, <see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면, <b>실패한다</b></para>
        /// <para><paramref name="isGain"/>이 true이고, <see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면, 새롭게 이벤트 밸류를 꺼낸 후, "활성화"를 <b>시도한다</b> (실패시 다시 반환, false)</para>
        ///</summary>
        private bool TryGetValueInternal<TEventValue, TTarget>(bool isGain, TTarget master, out TEventValue resultEventValue)
            where TEventValue : PanBaseEventValue.Base<TEventValue, TTarget>, new()
            where TTarget : class, IEventAble
        {
            var type = typeof(TEventValue);


            //! 조건 콜백에서 같은 타입을 다시 요청하면 재귀 평가와 불필요한 풀 대여 없이 실패로 반환한다
            if (IsEvaluatingEnableCondition(type))
            {
                resultEventValue = null;
                return false;
            }


            //? #1 테이블에 해당 이벤트 밸류가 존재 한다면, 그것을 반환한다
            if (EventValueTables.TryGetEventValue(false, type, out var gottenEventValue))
            {
                resultEventValue = gottenEventValue as TEventValue;

                //. 테이블에 있는 이벤트 밸류가 "활성화" 되어있다면, 반환 성공
                if (resultEventValue.Valid_CurrentEventAble) { return true; }


                //. 테이블에 있는 이벤트 밸류가 "비활성화" 되어있다면, 조건 확인 후 다시 활성화한다
                if (resultEventValue.CheckEnableCondition(master))
                {
                    resultEventValue.EnableValueAfterCondition(master);
                    return true;
                }


                //! 테이블에 있는 이벤트 밸류가 "비활성화" 되어있길래 "활성화"를 시도하였으나, 실패 한다면 반환에 실패
                resultEventValue = null;
                return false;
            }


            //? #2 이벤트 밸류의 "비활성화" 메서드 도중에 실행된 것이 아니고,
            //? 테이블에 해당 이벤트 밸류가 존재 하지 않고,
            //? 이벤트 테이블에 새롭게 추가 하는것이 허용 되어있다면
            //? 이벤트 밸류 매니저에서 새롭게 꺼내 테이블에 넣고, 반환한다
            else if (isGain == true && IsDisablingEventValue == null)
            {
                //. 이벤트 밸류 매니저 로 부터 새롭게  이벤트 밸류를 꺼낸다
                var eventValue = EventValueManager.PopEventValue<TEventValue>(1);


                bool conditionPassed;
                try
                {
                    conditionPassed = eventValue.CheckEnableCondition(master);
                }
                catch
                {
                    EventValueManager.PushEventValue(type, eventValue);
                    throw;
                }


                //! 새롭게 꺼낸 이벤트 밸류가 활성화 조건을 통과하지 못했다면 즉시 풀로 반환한다
                if (!conditionPassed)
                {
                    EventValueManager.PushEventValue(type, eventValue);
                    resultEventValue = null;
                    return false;
                }


                bool tableAdded = false;
                try
                {
                    //? 조건 검사가 끝난 값만 예약하여, Enable 내부 동일 타입 조회가 같은 인스턴스를 반환하게 한다
                    EventValueTables.AddEventTable(type, eventValue);
                    tableAdded = true;
                    eventValue.EnableValueAfterCondition(master);

                    if (EventValueTables.TryGetEventValue(false, type, out var attachedValue) &&
                        ReferenceEquals(attachedValue, eventValue) &&
                        eventValue.Valid_CurrentEventAble)
                    {
                        resultEventValue = eventValue;
                        return true;
                    }
                }
                catch
                {
                    bool reservationRemoved = tableAdded && EventValueTables.RemoveEventTable(type, eventValue);
                    if (!tableAdded || reservationRemoved)
                    {
                        EventValueManager.PushEventValue(type, eventValue);
                    }
                    throw;
                }


                bool removed = EventValueTables.RemoveEventTable(type, eventValue);
                if (removed) { EventValueManager.PushEventValue(type, eventValue); }
                resultEventValue = null;
                return false;
            }


            //. 이벤트 밸류의 "비활성화" 메서드 도중 테이블에 존재 하지 않는 이벤트 밸류를 추가 하려는 시도를 출력 하는 에러
            if (isGain && IsDisablingEventValue != null) { DebugMsg_AddedEventValue_WhenDisablingEventValue(type); }


            //! #3 이벤트 밸류를 테이블에서만 Peek 하려고 했으나 테이블에 존재 하지 않았다면 실패
            resultEventValue = null;
            return false;
        }



        ///======================================================================================================================================================



        //? 이벤트 밸류 반환: Require, "무조건 반환", 캐스팅 없이 Master를 IEventAble 그대로 사용하고, "활성화 조건"이 없는 이벤트 밸류만 호출이 가능



        ///<summary>
        /// <typeparamref name="TEventValue"/>을 <b>무조건 반환</b>한다
        /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 반드시 <b>보장</b>된다</para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, 반환한다</para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면,  <see cref="EventValueManager"/>에서 꺼내 테이블에 넣은 뒤, 반환한다</para>
        /// <para><i><see cref="Master"/>인 <see cref="IEventAble"/>를 직접적으로 사용하며, "활성화 조건 심사"가 존재 하지 않기에 무조건 반환이 가능</i></para>
        ///</summary>
        public void RequireOut<TEventValue>(out TEventValue resultEventValue)
            where TEventValue : PanBaseEventValue.EventAbles<TEventValue>, new()
        {
            resultEventValue = Require<TEventValue>();
        }

        ///<summary>
        /// <typeparamref name="TEventValue"/>을 <b>무조건 반환</b>한다
        /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 반드시 <b>보장</b>된다</para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, 반환한다</para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면,  <see cref="EventValueManager"/>에서 꺼내 테이블에 넣은 뒤, 반환한다</para>
        /// <para><i><see cref="Master"/>인 <see cref="IEventAble"/>를 직접적으로 사용하며, "활성화 조건 심사"가 존재 하지 않기에 무조건 반환이 가능</i></para>
        ///</summary>
        public TEventValue Require<TEventValue>()
            where TEventValue : PanBaseEventValue.EventAbles<TEventValue>, new()
        {
            return RequireInternal<TEventValue, IEventAble>(typeof(TEventValue), Master);
        }



        //? 이벤트 밸류 반환: 활성 상태 조회



        /// <summary>
        /// 현재 테이블에 붙어 있고 활성화된 이벤트 밸류만 조회합니다.
        /// </summary>
        /// <remarks>
        /// 이 메서드는 새 값을 대여하거나 비활성 값을 다시 활성화하지 않습니다.
        /// </remarks>
        public bool TryGetActive<TEventValue>(out TEventValue resultEventValue)
            where TEventValue : PanBaseEventValue.Base<TEventValue, IEventAble>, new()
        {
            if (EventValueTables.TryGetEventValue(true, typeof(TEventValue), out PanBaseEventValue eventValue) &&
                eventValue is TEventValue typedEventValue)
            {
                resultEventValue = typedEventValue;
                return true;
            }

            resultEventValue = null;
            return false;
        }



        /// <summary>
        /// 소유자가 <typeparamref name="TTarget"/>인 경우에만 현재 활성화된 이벤트 밸류를 조회합니다.
        /// </summary>
        /// <remarks>
        /// 이 메서드는 소유자 캐스팅과 활성 상태만 확인하며, 활성화 조건을 평가하지 않습니다.
        /// </remarks>
        public bool TryGetActive<TEventValue, TTarget>(out TEventValue resultEventValue)
            where TEventValue : PanBaseEventValue.Base<TEventValue, TTarget>, new()
            where TTarget : class, IEventAble
        {
            if (Master is TTarget &&
                EventValueTables.TryGetEventValue(true, typeof(TEventValue), out PanBaseEventValue eventValue) &&
                eventValue is TEventValue typedEventValue)
            {
                resultEventValue = typedEventValue;
                return true;
            }

            resultEventValue = null;
            return false;
        }



        //? 이벤트 밸류 반환: Peek, "엿보기"



        ///<summary>
        /// <typeparamref name="TEventValue"/>을 <b>엿본다</b>
        /// <para><typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
        /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, "활성화"를 <b>시도한다</b> (실패시 false, 이미 활성화가 되어있다면 <b>성공</b>)</para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면, <b>실패한다</b> (실패시 false)</para>
        ///</summary>
        public bool TryPeek<TEventValue>(out TEventValue resultEventValue)
            where TEventValue : PanBaseEventValue.Base<TEventValue, IEventAble>, new()
        {
            return TryGetValueInternal(false, Master, out resultEventValue);
        }

        ///<summary>
        /// <typeparamref name="TEventValue"/>을 <b>엿본다</b>
        /// <para><typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
        /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
        /// <para><see cref="Master"/>를 <typeparamref name="TTarget"/>으로 캐스팅을 <b>시도한다</b> (실패시 false)</para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, "활성화"를 <b>시도한다</b> (실패시 false, 이미 활성화가 되어있다면 <b>성공</b>)</para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면, <b>실패한다</b> (실패시 false)</para>
        ///</summary>
        public bool TryPeek<TEventValue, TTarget>(out TEventValue resultEventValue)
            where TEventValue : PanBaseEventValue.Base<TEventValue, TTarget>, new()
            where TTarget : class, IEventAble
        {
            if (Master is TTarget castingMaster) return TryGetValueInternal(false, castingMaster, out resultEventValue);

            //! Master의 TTarget 캐스팅에 실패
            resultEventValue = null;
            return false;
        }

        ///<summary>
        /// <typeparamref name="TEventValue"/>을 <b>엿본다</b>
        /// <para><typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
        /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, "활성화"를 <b>시도한다</b> (실패시 null, 이미 활성화가 되어있다면 <b>성공</b>)</para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면, <b>실패한다</b> (실패시 null)</para>
        ///</summary>
        public TEventValue Peek<TEventValue>()
            where TEventValue : PanBaseEventValue.Base<TEventValue, IEventAble>, new()
        {
            if (TryPeek<TEventValue>(out var result)) return result;

            //! Peek 실패
            return null;
        }

        ///<summary>
        /// <typeparamref name="TEventValue"/>을 <b>엿본다</b>
        /// <para><typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
        /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
        /// <para><see cref="Master"/>를 <typeparamref name="TTarget"/>으로 캐스팅을 <b>시도한다</b> (실패시 null)</para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, "활성화"를 <b>시도한다</b> (실패시 null, 이미 활성화가 되어있다면 <b>성공</b>)</para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면, <b>실패한다</b> (실패시 null)</para>
        ///</summary>
        public TEventValue Peek<TEventValue, TTarget>()
            where TEventValue : PanBaseEventValue.Base<TEventValue, TTarget>, new()
            where TTarget : class, IEventAble
        {
            if (TryPeek<TEventValue, TTarget>(out var result)) return result;

            //! Master의 TTarget 캐스팅에 실패
            return null;
        }



        //? 이벤트 밸류 반환: Gain, "얻기"



        ///<summary>
        /// <typeparamref name="TEventValue"/>을 <b>얻는다</b>
        /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, "활성화"를 <b>시도한다</b> (실패시 false, 이미 활성화가 되어있다면 <b>성공</b>)</para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면, 새롭게 이벤트 밸류를 꺼낸 후, "활성화"를 <b>시도한다</b> (실패시 다시 반환, false)</para>
        ///</summary>
        public bool TryGain<TEventValue>(out TEventValue resultEventValue)
            where TEventValue : PanBaseEventValue.EventAbles<TEventValue>, new()
        {
            return TryGetValueInternal(true, Master, out resultEventValue);
        }

        ///<summary>
        /// <typeparamref name="TEventValue"/>을 <b>얻는다</b>
        /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, "활성화"를 <b>시도한다</b> (실패시 false, 이미 활성화가 되어있다면 <b>성공</b>)</para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면, 새롭게 이벤트 밸류를 꺼낸 후, "활성화"를 <b>시도한다</b> (실패시 다시 반환, false)</para>
        ///</summary>
        public bool TryGainCondition<TEventValue>(out TEventValue resultEventValue)
            where TEventValue : PanBaseEventValue.EventAblesCondition<TEventValue>, new()
        {
            return TryGetValueInternal(true, Master, out resultEventValue);
        }

        ///<summary>
        /// <typeparamref name="TEventValue"/>을 <b>얻는다</b>
        /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
        /// <para><see cref="Master"/>를 <typeparamref name="TTarget"/>으로 캐스팅을 <b>시도한다</b> (실패시 false)</para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, "활성화"를 <b>시도한다</b> (실패시 false, 이미 활성화가 되어있다면 <b>성공</b>)</para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면, 새롭게 이벤트 밸류를 꺼낸 후, "활성화"를 <b>시도한다</b> (실패시 다시 반환, false)</para>
        ///</summary>
        public bool TryGain<TEventValue, TTarget>(out TEventValue resultEventValue)
            where TEventValue : PanBaseEventValue.Base<TEventValue, TTarget>, new()
            where TTarget : class, IEventAble
        {
            if (Master is TTarget castingMaster) return TryGetValueInternal(true, castingMaster, out resultEventValue);

            //! Master의 TTarget 캐스팅에 실패
            resultEventValue = null;
            return false;
        }

        ///<summary>
        /// <typeparamref name="TEventValue"/>을 <b>얻는다</b>
        /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, "활성화"를 <b>시도한다</b> (실패시 false, 이미 활성화가 되어있다면 <b>성공</b>)</para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면, 새롭게 이벤트 밸류를 꺼낸 후, "활성화"를 <b>시도한다</b> (실패시 다시 반환, false)</para>
        ///</summary>
        public TEventValue Gain<TEventValue>()
            where TEventValue : PanBaseEventValue.EventAbles<TEventValue>, new()
        {
            if (TryGain<TEventValue>(out var result)) return result;
            return null;
        }

        ///<summary>
        /// <typeparamref name="TEventValue"/>을 <b>얻는다</b>
        /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, "활성화"를 <b>시도한다</b> (실패시 false, 이미 활성화가 되어있다면 <b>성공</b>)</para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면, 새롭게 이벤트 밸류를 꺼낸 후, "활성화"를 <b>시도한다</b> (실패시 다시 반환, false)</para>
        ///</summary>
        public TEventValue GainCondition<TEventValue>()
            where TEventValue : PanBaseEventValue.EventAblesCondition<TEventValue>, new()
        {
            if (TryGainCondition<TEventValue>(out var result)) return result;
            return null;
        }

        ///<summary>
        /// <typeparamref name="TEventValue"/>을 <b>얻는다</b>
        /// <para>"활성화된" <typeparamref name="TEventValue"/>를 반환하는것이 <b>보장되지 않는다</b></para>
        /// <para><see cref="Master"/>를 <typeparamref name="TTarget"/>으로 캐스팅을 <b>시도한다</b> (실패시 false)</para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 한다면, "활성화"를 <b>시도한다</b> (실패시 false, 이미 활성화가 되어있다면 <b>성공</b>)</para>
        /// <para><see cref="EventValueTables"/>에 <typeparamref name="TEventValue"/>가 존재 하지 않는다면, 새롭게 이벤트 밸류를 꺼낸 후, "활성화"를 <b>시도한다</b> (실패시 다시 반환, false)</para>
        ///</summary>
        public TEventValue Gain<TEventValue, TTarget>()
            where TEventValue : PanBaseEventValue.Base<TEventValue, TTarget>, new()
            where TTarget : class, IEventAble
        {
            if (TryGain<TEventValue, TTarget>(out var result)) return result;

            //! Master의 TTarget 캐스팅에 실패
            return null;
        }


    }
}
