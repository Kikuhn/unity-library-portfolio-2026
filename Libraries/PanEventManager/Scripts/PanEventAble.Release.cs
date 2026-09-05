using System;



namespace Pan.Event
{
    public partial class EventAble
    {
        ///======================================================================================================================================================



        //? 이벤트 밸류 "제거"



        /// <summary>
        /// 이벤트 밸류를 <b>제거한다</b>
        /// </summary>
        private bool RemoveValueInternal(Type eventValueType, PanBaseEventValue eventValue, bool executeDisableEvent, bool removeEventTable)// where TEventValue : PanBaseEventValue, new()
        {
            //. 활성 값에만 "비활성화" 메서드를 실행한다. 테이블이 보유한 비활성 값은 제거와 풀 반환을 계속한다
            if (executeDisableEvent && eventValue.Valid_CurrentEventAble && !eventValue.DisableValue(this)) { return false; }


            //? 테이블 보유를 요청한 값은 비활성 상태로 남겨 이후 Require/Peek/Gain에서 같은 인스턴스를 재활성화한다
            if (!removeEventTable) { return true; }


            //! 현재 테이블이 정확히 이 인스턴스를 소유할 때만 제거하고 풀로 반환한다
            if (!EventValueTables.RemoveEventTable(eventValueType, eventValue))
            {
                //? Disable 콜백이 같은 인스턴스를 이미 제거했다면 중복 Push 없이 완료로 처리한다
                return !EventValueTables.TryGetEventValue(false, eventValueType, out _);
            }


            //? 이벤트 밸류 매니저에 제거한 이벤트 밸류를 반환한다
            EventValueManager.PushEventValue(eventValueType, eventValue);


            return true;
        }



        ///<summary>
        ///이벤트 밸류를 <b>제거한다</b>(값을 받아와서 제거)
        ///<para>이벤트를 직접 받아와 제거하며,</para>
        ///<para><see cref="EventValueTables"/>에 해당 객체가 존재해야 한다</para>
        ///</summary>
        /// <param name="executeDisableEvent">
        /// <paramref name="eventValue"/>의 "비활성화" 메서드를 실행할 지 여부
        /// <para></para><paramref name="eventValue"/>의 "비활성화" 메서드에서 실행되어 테이블에서 제거하기위해 이 메서드가 실행 된 것이라면,
        /// <para>또 "비활성화" 메서드를 실행할 필요는 없기때문에, 그때 <c>false</c>로 사용한다</para>
        /// <para><i>기본값: true</i></para>
        /// </param>
        /// <param name="removeEventTable">
        /// <see cref="EventValueTables"/>에서 <paramref name="eventValue"/>를 제거할지 여부입니다.
        /// <para><c>false</c>이면 비활성 값의 소유권을 테이블에 유지하며 풀에는 반환하지 않습니다.</para>
        /// <para>나중에 완전히 반환하려면 이 값을 <c>true</c>로 호출합니다. 이미 비활성인 값에는 비활성화 콜백을 다시 호출하지 않습니다.</para>
        /// <para><i>기본값: true</i></para>
        /// </param>
        public bool RemoveValue<TEventValue>(TEventValue eventValue, bool executeDisableEvent = true, bool removeEventTable = true) where TEventValue : PanBaseEventValue, new()
        {
            var eventValueType = eventValue.GetType();

            if (EventValueTables.TryGetEventValue(false, eventValueType, out var result) &&
                ReferenceEquals(result, eventValue))
            {
                return RemoveValueInternal(eventValueType, eventValue, executeDisableEvent, removeEventTable);
            }

            return false;
        }



        ///<summary>
        ///이벤트 밸류를 <b>제거한다</b>(<see cref="PanBaseEventValue"/>를 받아와서 제거)
        ///<para>이벤트를 직접 받아와 제거하며,</para>
        ///<para><see cref="EventValueTables"/>에 해당 객체가 존재해야 한다</para>
        ///</summary>
        /// <param name="executeDisableEvent">
        /// <paramref name="eventValue"/>의 "비활성화" 메서드를 실행할 지 여부
        /// <para></para><paramref name="eventValue"/>의 "비활성화" 메서드에서 실행되어 테이블에서 제거하기위해 이 메서드가 실행 된 것이라면,
        /// <para>또 "비활성화" 메서드를 실행할 필요는 없기때문에, 그때 <c>false</c>로 사용한다</para>
        /// <para><i>기본값: true</i></para>
        /// </param>
        /// <param name="removeEventTable">
        /// <see cref="EventValueTables"/>에서 <paramref name="eventValue"/>를 제거할지 여부입니다.
        /// <para><c>false</c>이면 비활성 값의 소유권을 테이블에 유지하며 풀에는 반환하지 않습니다.</para>
        /// <para>나중에 완전히 반환하려면 이 값을 <c>true</c>로 호출합니다. 이미 비활성인 값에는 비활성화 콜백을 다시 호출하지 않습니다.</para>
        /// <para><i>기본값: true</i></para>
        /// </param>
        internal bool RemoveValue(PanBaseEventValue eventValue, bool executeDisableEvent = true, bool removeEventTable = true)
        {
            var eventValueType = eventValue.GetType();

            if (EventValueTables.TryGetEventValue(false, eventValueType, out var result) &&
                ReferenceEquals(result, eventValue))
            {
                return RemoveValueInternal(eventValueType, eventValue, executeDisableEvent, removeEventTable);
            }

            return false;
        }



        ///<summary>
        ///이벤트 밸류를 <b>제거한다</b> (제네릭으로 제거)
        ///<para>이벤트를 직접 받아와 제거하며,</para>
        ///<para><see cref="EventValueTables"/>에 해당 객체가 존재해야 한다</para>
        ///</summary>
        /// <param name="executeDisableEvent">
        /// <paramref name="eventValue"/>의 "비활성화" 메서드를 실행할 지 여부
        /// <para></para><paramref name="eventValue"/>의 "비활성화" 메서드에서 실행되어 테이블에서 제거하기위해 이 메서드가 실행 된 것이라면,
        /// <para>또 "비활성화" 메서드를 실행할 필요는 없기때문에, 그때 <c>false</c>로 사용한다</para>
        /// <para><i>기본값: true</i></para>
        /// </param>
        /// <param name="removeEventTable">
        /// <see cref="EventValueTables"/>에서 이벤트 밸류를 제거할지 여부입니다.
        /// <para><c>false</c>이면 비활성 값의 소유권을 테이블에 유지하며 풀에는 반환하지 않습니다.</para>
        /// <para>나중에 완전히 반환하려면 이 값을 <c>true</c>로 호출합니다. 이미 비활성인 값에는 비활성화 콜백을 다시 호출하지 않습니다.</para>
        /// <para><i>기본값: true</i></para>
        /// </param>
        public bool RemoveValue<TEventValue>(bool executeDisableEvent = true, bool removeEventTable = true) where TEventValue : PanBaseEventValue, new()
        {
            var eventValueType = typeof(TEventValue);

            if (EventValueTables.TryGetEventValue(false, eventValueType, out var result))
            {
                return RemoveValueInternal(eventValueType, result as TEventValue, executeDisableEvent, removeEventTable);
            }

            return false;
        }



        ///======================================================================================================================================================



        //? 이벤트 밸류 체크




        /// <summary>
        /// <typeparamref name="TEventValue"/>가 이벤트 테이블에 존재 하는지 확인
        /// </summary>
        /// <typeparam name="TEventValue"></typeparam>
        /// <param name="checkValid">활성화 여부도 확인한다</param>
        public bool CheckValueTable<TEventValue>(bool checkValid) where TEventValue : PanBaseEventValue, new()
        {
            return EventValueTables.ContainsEventValue(checkValid, typeof(TEventValue));
        }



        /// <summary>
        /// <typeparamref name="TEventValue"/>가 이벤트 테이블에 존재 하는지 확인
        /// </summary>
        /// <typeparam name="TEventValue"></typeparam>
        /// <param name="checkValid">활성화 여부도 확인한다</param>
        public bool CheckValueTable<TEventValue>(bool checkValid, TEventValue value) where TEventValue : PanBaseEventValue, new()
        {
            return EventValueTables.ContainsEventValue(checkValid, value.GetType());
        }



        ///======================================================================================================================================================



        //? 초기화



        /// <summary>
        /// <see cref="EventAble"/> 초기화
        /// </summary>
        public void Reset()
        {
            EventValueTables.ResetEventTable();

            //if (EventValueTables.Count == 0) { return; }

            //IsLoopTable_fromReset = true;

            //foreach (var a in EventValueTables)
            //{
            //    RemoveValue(a.Value, true, false);
            //}

            //IsLoopTable_fromReset = false;

            //if (EventValueTable_WillRemoves.Count != 0)
            //{

            //    foreach (var a in EventValueTable_WillRemoves)
            //    {
            //        RemoveEventTableInternal(a);
            //    }

            //    EventValueTable_WillRemoves.Clear();
            //}

            //EventValueTables.Clear();
        }


    }
}
