using System;
using System.Collections.Generic;
using Pan.Util;
using UnityEngine;



namespace Pan.Event
{
    public partial class EventAble
    {
        //? 내부 클래스



        ///<summary>
        /// 이벤트 밸류의 딕셔너리 테이블을 관리하는 클래스
        ///</summary>
        [Serializable]
        private partial class EventValueTable : MainSlave<EventAble>
        {
            ///======================================================================================================================================================



            public EventValueTable(
                EventAble main,
                int capacity_EventValueTables,
                EventAbleTableAllocationMode allocationMode) : base(main)
            {
                EventValueTablesInitializeCapacity = capacity_EventValueTables;

                if (allocationMode == EventAbleTableAllocationMode.Eager)
                {
                    EventValueTables = new Dictionary<Type, PanBaseEventValue>(capacity_EventValueTables);
                }
            }



            ///======================================================================================================================================================



            /// <summary>
            /// 이 <see cref="EventAble"/>에 할당 되어 있는 이벤트 밸류들을 관리하는 <b>테이블</b>
            /// </summary>
            //[ShowInInspector]
            private Dictionary<Type, PanBaseEventValue> EventValueTables;



            /// <summary>
            /// 테이블의 구조 변경 횟수입니다.
            /// </summary>
            public uint Revision { get; private set; }



            public bool IsAllocated => EventValueTables != null;



            private Dictionary<Type, PanBaseEventValue> GetOrCreateEventValueTables()
            {
                return EventValueTables ??= new Dictionary<Type, PanBaseEventValue>(EventValueTablesInitializeCapacity);
            }



            /// <summary>
            /// <see cref="EventValueTables"/>의 최초 초기화 용량 크기
            /// </summary>
            [HideInInspector]
            public readonly int EventValueTablesInitializeCapacity;



            ///======================================================================================================================================================



            /// <summary>
            /// 받아온 <paramref name="type"/>의 이벤트 밸류가 존재하고, (활성화 되어있는지 확인) 반환
            /// </summary>
            /// <param name="checkValid">
            /// 그 이벤트 밸류가 "활성화" 되어있는지의 여부도 고려하여 검사한다
            /// <para>"비활성화" 되어있는데 테이블에 들어있는 경우는, "비활성화"되었으나, 아직 테이블에서 제거되지 않았을 수도 있기 때문</para>
            /// </param>
            /// <param name="type">이벤트 밸류의 타입</param>
            /// <param name="resultEventValue">반환되는 이벤트 밸류</param>
            /// <returns></returns>
            public bool TryGetEventValue(bool checkValid, Type type, out PanBaseEventValue resultEventValue)
            {
                //? 이벤트 테이블애 이벤트 밸류가 존재 하고, 그 이벤트 밸류가 "활성화" 되어있는지 까지 확인한다
                if (EventValueTables != null &&
                    EventValueTables.TryGetValue(type, out resultEventValue) &&
                    (!checkValid || resultEventValue.Valid_CurrentEventAble))
                {
                    return true;
                }
                else
                {
                    resultEventValue = null;
                    return false;
                }
            }



            /// <summary>
            /// 받아온 <paramref name="type"/>의 이벤트 밸류가 존재하고, (활성화 되어있는지) 확인
            /// </summary>
            /// <param name="checkValid">
            /// 그 이벤트 밸류가 "활성화" 되어있는지의 여부도 고려하여 검사한다
            /// <para>"비활성화" 되어있는데 테이블에 들어있는 경우는, "비활성화"되었으나, 아직 테이블에서 제거되지 않았을 수도 있기 때문</para>
            /// </param>
            /// <param name="type">이벤트 밸류의 타입</param>
            public bool ContainsEventValue(bool checkValid, Type type)
            {
                if (TryGetEventValue(checkValid, type, out var result))
                {
                    return true;
                }
                return false;
            }



            /// <summary>
            /// 이벤트 테이블에 추가한다
            /// </summary>
            /// <param name="key"></param>
            /// <param name="value"></param>
            public void AddEventTable(Type key, PanBaseEventValue value)
            {
                GetOrCreateEventValueTables().Add(key, value);
                Main.EnsureLocalSignalDispatchScratch();
                Revision++;
#if UNITY_EDITOR
                Editor_MarkEventValueTableCacheDirty();
#endif
            }



            /// <summary>
            /// 이벤트 테이블에 제거한다
            /// </summary>
            /// <param name="key"></param>
            public bool RemoveEventTable(Type key, PanBaseEventValue expectedValue = null)
            {
                if (EventValueTables == null) { return false; }

                if (expectedValue != null &&
                    (!EventValueTables.TryGetValue(key, out var currentValue) || !ReferenceEquals(currentValue, expectedValue)))
                {
                    return false;
                }

                bool removed = EventValueTables.Remove(key);
                if (removed) { Revision++; }
#if UNITY_EDITOR
                if (removed) { Editor_MarkEventValueTableCacheDirty(); }
#endif

                ////! 테이블 전체 초기화 에서 실행된 "제거" 로직이라면, foreach 도중 이기에, 별도의 리스트에 추가해
                ////! 테이블 foreach가 끝난 뒤에 제거되도록 유예시킨다
                //if (IsLoopTable_fromReset)
                //{
                //    EventValueTable_WillRemoves.Add(key);
                //    return;
                //}


                ////? 중간에 다시 활성화 되었다면, 테이블에서 제거하지않는다
                //if (EventValueTables.TryGetValue(key, out var eventValue) && eventValue.Valid_CurrentEventAble)
                //{
                //    EventValueTables.Remove(key);
                //}

                return removed;
            }



            /// <summary>
            /// 이벤트 테이블을 초기화한다
            /// </summary>
            public void ResetEventTable()
            {
                //! 애초에 테이블이 비어있다면, 아무 것도 하지 않는다
                if (EventValueTables == null || EventValueTables.Count == 0) { return; }


                //? 열거자를 종료한 뒤 한 항목씩 제거하면 Disable의 중첩 제거에 안전하면서 임시 배열 할당도 피할 수 있다
                while (EventValueTables.Count > 0)
                {
                    PanBaseEventValue eventValue;
                    using (var enumerator = EventValueTables.Values.GetEnumerator())
                    {
                        if (!enumerator.MoveNext()) { break; }
                        eventValue = enumerator.Current;
                    }

                    //! 정상적인 테이블 항목을 제거하지 못했다면 같은 항목의 무한 반복을 막고 현재 상태를 보존한다
                    if (!Main.RemoveValue(eventValue)) { break; }
                }
            }



            /// <summary>
            /// 현재 이벤트 밸류 참조를 로컬 신호 순회를 위한 재사용 목록에 복사합니다.
            /// </summary>
            public void CopyEventValuesTo(List<PanBaseEventValue> destination)
            {
                destination.Clear();
                if (EventValueTables == null) { return; }

                foreach (PanBaseEventValue eventValue in EventValueTables.Values)
                {
                    destination.Add(eventValue);
                }
            }



            /// <summary>
            /// 지정한 이벤트 밸류가 현재 테이블의 활성 항목인지 확인합니다.
            /// </summary>
            public bool ContainsCurrent(PanBaseEventValue eventValue)
            {
                return eventValue != null &&
                    EventValueTables != null &&
                    EventValueTables.TryGetValue(eventValue.GetType(), out PanBaseEventValue current) &&
                    ReferenceEquals(current, eventValue) &&
                    eventValue.Valid_CurrentEventAble;
            }



            ///======================================================================================================================================================
        }


    }
}
