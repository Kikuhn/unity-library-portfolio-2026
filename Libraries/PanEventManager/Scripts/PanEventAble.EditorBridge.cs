#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;



namespace Pan.Event
{
    /// <summary>
    /// Play Mode에서 EventAble Inspector를 갱신하는 기준입니다.
    /// </summary>
    internal enum EventAbleInspectorRefreshMode
    {
        [LabelText("변경 시에만")]
        ChangeOnly,

        [LabelText("게임 프레임 동기화")]
        GameFrameSync,

        [LabelText("10 Hz 제한 갱신")]
        Throttled10Hz,

        [LabelText("에디터 프레임 실시간")]
        EditorRealtime
    }



    /// <summary>
    /// EventAble의 런타임 계약을 노출하지 않고 전용 Editor drawer가 읽을 수 있는 변경 추적 표면입니다.
    /// </summary>
    public partial class EventAble
    {
        [ThreadStatic]
        private static int editorLegacyAddMenuSuppressionDepth;

        [ThreadStatic]
        private static int editorStructuralActionSuppressionDepth;



        /// <summary>
        /// 기존 EventAble Inspector에서 실험용 추가 메뉴를 표시할지 결정합니다.
        /// </summary>
        private bool Editor_ShowLegacyAddMenu =>
            IsMaster_Valid && editorLegacyAddMenuSuppressionDepth == 0;



        /// <summary>
        /// Editor session이 자체 추가 메뉴를 그리는 동안 기존 실험용 드롭다운을 숨깁니다.
        /// </summary>
        internal static void EditorBeginLegacyAddMenuSuppression()
        {
            editorLegacyAddMenuSuppressionDepth++;
        }



        /// <summary>
        /// Editor session의 기존 추가 메뉴 숨김 범위를 종료합니다.
        /// </summary>
        internal static void EditorEndLegacyAddMenuSuppression()
        {
            if (editorLegacyAddMenuSuppressionDepth > 0)
            {
                editorLegacyAddMenuSuppressionDepth--;
            }
        }



        /// <summary>
        /// 읽기·값 편집 전용 Editor session이 EventValue 추가·해제·고정 UI를 숨기는 범위를 시작합니다.
        /// </summary>
        internal static void EditorBeginStructuralActionSuppression()
        {
            editorStructuralActionSuppressionDepth++;
        }



        /// <summary>
        /// EventValue 구조 변경 UI 숨김 범위를 종료합니다.
        /// </summary>
        internal static void EditorEndStructuralActionSuppression()
        {
            if (editorStructuralActionSuppressionDepth > 0)
            {
                editorStructuralActionSuppressionDepth--;
            }
        }



        internal static bool EditorAreStructuralActionsSuppressed =>
            editorStructuralActionSuppressionDepth > 0;



        /// <summary>
        /// EventValue 테이블의 구조가 변경되었을 때 Editor 표시 계층에 알립니다.
        /// </summary>
        internal static event Action<EventAble> EditorEventValueTableChanged;



        /// <summary>
        /// EventValue 타입별 전역 고정 상태가 변경되었을 때 열린 Inspector에 알립니다.
        /// </summary>
        internal static event Action EditorInspectorPinSettingsChanged;



        /// <summary>
        /// EventValue 테이블의 구조가 바뀔 때마다 증가하는 Editor 전용 버전입니다.
        /// </summary>
        internal int EditorEventValueChangeVersion { get; private set; }



        /// <summary>
        /// 현재 EventValue 테이블의 항목 수입니다.
        /// </summary>
        internal int EditorEventValueCount => EventValueTables.EditorCount;



        /// <summary>
        /// EventValue 테이블이 생성될 때 지정한 초기 용량입니다.
        /// </summary>
        internal int EditorInitialEventValueCapacity => EventValueTables.EventValueTablesInitializeCapacity;



        /// <summary>
        /// 현재 Inspector가 사용하는 Play Mode 갱신 방식입니다.
        /// </summary>
        internal EventAbleInspectorRefreshMode EditorInspectorRefreshMode => EventValueTables.EditorRefreshMode;



        /// <summary>
        /// 다음 Repaint 종료 시점에 적용할 Inspector 표시 변경이 있는지 확인합니다.
        /// </summary>
        internal bool EditorNeedsInspectorViewCommit => EventValueTables.EditorNeedsInspectorViewCommit;



        /// <summary>
        /// 현재 테이블이 소유한 EventValue 참조를 전달된 목록에 복사합니다.
        /// </summary>
        /// <param name="destination">기존 내용을 비운 뒤 현재 값이 채워질 대상 목록입니다.</param>
        internal void EditorCopyEventValues(List<PanBaseEventValue> destination)
        {
            if (destination == null) { throw new ArgumentNullException(nameof(destination)); }

            EventValueTables.EditorCopyEventValues(destination);
        }



        /// <summary>
        /// 전달된 EventValue가 현재 테이블이 소유한 정확한 인스턴스인지 확인합니다.
        /// </summary>
        /// <param name="eventValue">확인할 EventValue입니다.</param>
        internal bool EditorContainsEventValue(PanBaseEventValue eventValue)
        {
            return EventValueTables.EditorContainsEventValue(eventValue);
        }



        /// <summary>
        /// Editor 도구가 지정한 EventValue를 풀에서 대여해 현재 소유자에게 활성화하고 테이블에 장착합니다.
        /// </summary>
        /// <param name="owner">현재 <see cref="EventAble"/>을 소유한 대상입니다.</param>
        /// <param name="eventValueType">추가할 구체 EventValue 타입입니다.</param>
        /// <param name="eventValue">성공하면 활성화되어 테이블에 장착된 값입니다.</param>
        /// <returns>활성화와 장착이 모두 완료되면 <c>true</c>입니다.</returns>
        internal bool EditorTryAddEventValue(
            IEventAble owner,
            Type eventValueType,
            out PanBaseEventValue eventValue)
        {
            eventValue = null;

            if (owner == null ||
                eventValueType == null ||
                eventValueType.IsAbstract ||
                eventValueType.ContainsGenericParameters ||
                !typeof(PanBaseEventValue).IsAssignableFrom(eventValueType) ||
                !ReferenceEquals(owner.EventAble, this) ||
                EventValueTables.ContainsEventValue(false, eventValueType) ||
                !PanEventGeneralManager.IsInitialize ||
                EventValueManager == null)
            {
                return false;
            }

            PanBaseEventValue rentedEventValue = EventValueManager.PopEventValue(eventValueType, 1);
            if (rentedEventValue == null) { return false; }

            if (!(rentedEventValue is PanBaseEventValue.IEnableValueNonCasting enableValue) ||
                !enableValue.GetCurrentType.IsInstanceOfType(owner))
            {
                EventValueManager.PushEventValue(eventValueType, rentedEventValue);
                return false;
            }

            bool tableAdded = false;

            try
            {
                EventValueTables.AddEventTable(eventValueType, rentedEventValue);
                tableAdded = true;

                if (enableValue.EnableValue(owner) &&
                    rentedEventValue.Valid_CurrentEventAble &&
                    ReferenceEquals(rentedEventValue.CurrentEventAble, this) &&
                    EventValueTables.TryGetEventValue(false, eventValueType, out PanBaseEventValue attachedEventValue) &&
                    ReferenceEquals(attachedEventValue, rentedEventValue))
                {
                    eventValue = rentedEventValue;
                    return true;
                }

                //. 조건 실패 또는 활성화 중 자기 제거가 발생하면 정확히 현재 테이블이 소유한 값만 회수합니다.
                RemoveValue(rentedEventValue);
                return false;
            }
            catch
            {
                //! 활성화 예외 뒤에도 테이블 예약과 풀 소유권이 남지 않도록 같은 정리 경로를 사용합니다.
                if (tableAdded) { RemoveValue(rentedEventValue); }
                else { EventValueManager.PushEventValue(eventValueType, rentedEventValue); }

                throw;
            }
        }



        /// <summary>
        /// Odin native 목록이 현재 Repaint를 끝낸 뒤 보류된 UI 변경을 적용합니다.
        /// </summary>
        internal bool EditorCommitInspectorView()
        {
            return EventValueTables.EditorCommitInspectorView();
        }



        /// <summary>
        /// 다중 객체 Inspector에서는 행 동작이 서로 다른 EventAble에 병렬 적용되지 않도록 제한합니다.
        /// </summary>
        /// <param name="enabled">현재 property tree가 단일 대상을 나타내는지 여부입니다.</param>
        internal void EditorSetInspectorRowActionsEnabled(bool enabled)
        {
            EventValueTables.EditorSetRowActionsEnabled(enabled);
        }



        internal void EditorSetInspectorRowActionsVisible(bool visible)
        {
            EventValueTables.EditorSetRowActionsVisible(visible);
        }



        /// <summary>
        /// EventValue 테이블 구조가 바뀌었음을 Editor drawer에 알립니다.
        /// </summary>
        private void Editor_IncrementEventValueChangeVersion()
        {
            unchecked
            {
                EditorEventValueChangeVersion++;
            }

            EditorEventValueTableChanged?.Invoke(this);
        }



        private void Editor_NotifyInspectorPinSettingsChanged()
        {
            EditorInspectorPinSettingsChanged?.Invoke();
        }



        private partial class EventValueTable
        {
            internal int EditorCount => EventValueTables?.Count ?? 0;



            internal EventAbleInspectorRefreshMode EditorRefreshMode => editorRefreshMode;



            internal bool EditorNeedsInspectorViewCommit =>
                editorEventValueTableCacheDirty ||
                editorPendingPinChanges.Count > 0 ||
                editorPendingRemovals.Count > 0 ||
                editorObservedPinRevision != editorGlobalPinRevision;



            internal void EditorCopyEventValues(List<PanBaseEventValue> destination)
            {
                destination.Clear();
                if (EventValueTables == null) { return; }

                foreach (PanBaseEventValue eventValue in EventValueTables.Values)
                {
                    destination.Add(eventValue);
                }
            }



            internal bool EditorContainsEventValue(PanBaseEventValue eventValue)
            {
                if (eventValue == null) { return false; }

                return EventValueTables != null &&
                    EventValueTables.TryGetValue(eventValue.GetType(), out PanBaseEventValue currentEventValue) &&
                    ReferenceEquals(currentEventValue, eventValue);
            }



            internal void EditorSetRowActionsEnabled(bool enabled)
            {
                if (editorRowActionsEnabled == enabled) { return; }

                editorRowActionsEnabled = enabled;
            }



            internal void EditorSetRowActionsVisible(bool visible)
            {
                if (editorRowActionsVisible == visible) { return; }

                editorRowActionsVisible = visible;
            }



            internal bool EditorCommitInspectorView()
            {
                Editor_ApplyPendingPinChanges();
                Editor_ApplyPendingRemovals();

                if (editorObservedPinRevision != editorGlobalPinRevision)
                {
                    editorEventValueTableCacheDirty = true;
                }

                if (!editorEventValueTableCacheDirty) { return false; }

                Editor_Refresh_EventValueTablesList();
                return true;
            }
        }
    }
}

#endif
