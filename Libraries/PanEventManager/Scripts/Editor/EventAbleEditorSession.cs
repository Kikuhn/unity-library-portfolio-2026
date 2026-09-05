using System;
using System.Collections.Generic;
using Pan.Event;
using Sirenix.OdinInspector.Editor;



namespace Pan.EventManagers.Editor
{
    /// <summary>
    /// 일반 <see cref="IEventAble"/> 대상을 기존 Odin EventAble Inspector로 편집하는 Editor 전용 세션입니다.
    /// </summary>
    /// <remarks>
    /// 소유자의 <see cref="IEventAble.EventAble"/>이 교체되거나 더 이상 조회되지 않으면 세션은 즉시 폐기되며,
    /// 이전 EventAble에는 추가·제거 작업을 수행하지 않습니다.
    /// </remarks>
    public sealed class EventAbleEditorSession : IDisposable
    {
        private static readonly IReadOnlyList<Type> EmptyTypes = Array.Empty<Type>();



        private readonly List<PanBaseEventValue> attachedEventValues = new List<PanBaseEventValue>();



        private IEventAble owner;
        private EventAble eventAble;
        private PropertyTree propertyTree;
        private IReadOnlyList<Type> addableEventValueTypes = EmptyTypes;
        private int observedEventValueChangeVersion = -1;
        private bool disposed;



        /// <summary>
        /// 지정한 EventAble 소유자를 편집하는 Odin 세션을 생성합니다.
        /// </summary>
        /// <param name="owner">편집할 EventAble을 제공하는 현재 소유자입니다.</param>
        /// <exception cref="ArgumentNullException"><paramref name="owner"/> 또는 소유자의 EventAble이 null인 경우입니다.</exception>
        public EventAbleEditorSession(IEventAble owner)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            eventAble = owner.EventAble ?? throw new ArgumentNullException(nameof(owner), "소유자의 EventAble이 null입니다.");

            try
            {
                propertyTree = PropertyTree.Create(eventAble);
                RefreshAddableEventValueTypes();
            }
            catch
            {
                Dispose();
                throw;
            }
        }



        /// <summary>
        /// 세션이 현재 편집하는 소유자를 반환합니다.
        /// </summary>
        public IEventAble Owner => TryKeepCurrentOwner() ? owner : null;



        /// <summary>
        /// 소유자가 계속 보유 중인 EventAble을 반환합니다.
        /// </summary>
        /// <remarks>소유자가 stale 상태이면 세션을 폐기하고 null을 반환합니다.</remarks>
        public EventAble EventAble => TryKeepCurrentOwner() ? eventAble : null;



        /// <summary>
        /// 기존 <c>EventAbleInspectorDrawer</c>를 사용하는 Odin property tree를 반환합니다.
        /// </summary>
        /// <remarks>소유자가 stale 상태이면 세션을 폐기하고 null을 반환합니다.</remarks>
        public PropertyTree PropertyTree => TryKeepCurrentOwner() ? propertyTree : null;



        /// <summary>
        /// 현재 소유자 타입과 호환되며 아직 장착되지 않은 EventValue 타입 목록입니다.
        /// </summary>
        public IReadOnlyList<Type> AddableEventValueTypes
        {
            get
            {
                if (!TryKeepCurrentOwner()) { return EmptyTypes; }
                if (observedEventValueChangeVersion != eventAble.EditorEventValueChangeVersion)
                {
                    RefreshAddableEventValueTypes();
                }

                return addableEventValueTypes;
            }
        }



        /// <summary>
        /// 세션과 소유자의 EventAble 연결이 아직 유효한지 확인합니다.
        /// </summary>
        public bool IsValid => TryKeepCurrentOwner();



        /// <summary>
        /// 예외를 외부 Inspector로 전파하지 않고 Editor 세션 생성을 시도합니다.
        /// </summary>
        /// <param name="owner">편집할 EventAble 소유자입니다.</param>
        /// <param name="session">성공하면 생성된 세션입니다.</param>
        /// <returns>소유자와 EventAble이 유효하고 Odin tree를 만들 수 있으면 <c>true</c>입니다.</returns>
        public static bool TryCreate(IEventAble owner, out EventAbleEditorSession session)
        {
            session = null;

            try
            {
                session = new EventAbleEditorSession(owner);
                return true;
            }
            catch
            {
                session?.Dispose();
                session = null;
                return false;
            }
        }



        /// <summary>
        /// 기존 EventAble Odin drawer와 모든 EventValue 하위 필드를 그립니다.
        /// </summary>
        /// <param name="drawMonoScript">Odin tree의 MonoScript 항목을 함께 그릴지 여부입니다.</param>
        /// <returns>현재 소유자가 유효하여 tree를 그렸으면 <c>true</c>입니다.</returns>
        public bool Draw(bool drawMonoScript = false)
        {
            if (!TryKeepCurrentOwner()) { return false; }

            EventAble.EditorBeginLegacyAddMenuSuppression();

            try
            {
                propertyTree.Draw(drawMonoScript);
                return true;
            }
            finally
            {
                EventAble.EditorEndLegacyAddMenuSuppression();
            }
        }



        /// <summary>
        /// PropertyTree를 만들지 않고 현재 소유자에게 장착된 EventValue 개수를 조회합니다.
        /// </summary>
        /// <param name="owner">조회할 EventAble 소유자입니다.</param>
        /// <param name="count">성공하면 현재 장착된 EventValue 개수입니다.</param>
        /// <returns>소유자와 EventAble을 안전하게 조회했다면 <c>true</c>입니다.</returns>
        public static bool TryGetAttachedEventValueCount(IEventAble owner, out int count)
        {
            count = 0;
            if (owner == null) { return false; }

            try
            {
                EventAble currentEventAble = owner.EventAble;
                if (currentEventAble == null) { return false; }

                count = currentEventAble.EditorEventValueCount;
                return true;
            }
            catch
            {
                return false;
            }
        }



        /// <summary>
        /// 현재 EventAble에 실제로 장착된 EventValue 개수를 반환합니다.
        /// </summary>
        public int AttachedEventValueCount
        {
            get
            {
                if (!TryKeepCurrentOwner()) { return 0; }

                eventAble.EditorCopyEventValues(attachedEventValues);
                return attachedEventValues.Count;
            }
        }



        /// <summary>
        /// 현재 장착된 EventValue의 Odin 필드만 편집하고 추가·해제·고정 같은 구조 변경 UI는 표시하지 않습니다.
        /// </summary>
        /// <param name="drawMonoScript">Odin tree의 MonoScript 항목을 함께 그릴지 여부입니다.</param>
        /// <returns>현재 소유자가 유효하여 tree를 그렸다면 <c>true</c>입니다.</returns>
        public bool DrawAttachedEventValuesOnly(bool drawMonoScript = false)
        {
            if (!TryKeepCurrentOwner()) { return false; }

            EventAble.EditorBeginLegacyAddMenuSuppression();
            EventAble.EditorBeginStructuralActionSuppression();

            try
            {
                propertyTree.Draw(drawMonoScript);
                return true;
            }
            finally
            {
                EventAble.EditorEndStructuralActionSuppression();
                EventAble.EditorEndLegacyAddMenuSuppression();
            }
        }



        /// <summary>
        /// 현재 소유자와 실제 장착 상태를 기준으로 추가 가능한 EventValue 타입을 다시 계산합니다.
        /// </summary>
        public void RefreshAddableEventValueTypes()
        {
            if (!TryKeepCurrentOwner())
            {
                addableEventValueTypes = EmptyTypes;
                return;
            }

            eventAble.EditorCopyEventValues(attachedEventValues);
            var attachedTypes = new HashSet<Type>();

            foreach (PanBaseEventValue attachedEventValue in attachedEventValues)
            {
                if (attachedEventValue != null) { attachedTypes.Add(attachedEventValue.GetType()); }
            }

            var nextTypes = new List<Type>();
            foreach (Type eventValueType in PanEventsInitializeSettingSbjectBase.GetPanBaseEventValueTypesAll())
            {
                if (attachedTypes.Contains(eventValueType) || !IsCompatibleEventValueType(eventValueType)) { continue; }

                nextTypes.Add(eventValueType);
            }

            nextTypes.Sort((left, right) => string.Compare(
                left.FullName ?? left.Name,
                right.FullName ?? right.Name,
                StringComparison.Ordinal));
            addableEventValueTypes = nextTypes.AsReadOnly();
            observedEventValueChangeVersion = eventAble.EditorEventValueChangeVersion;
        }



        /// <summary>
        /// 호환되는 EventValue를 PanEventManager 풀에서 대여해 활성화하고 현재 EventAble에 장착합니다.
        /// </summary>
        /// <param name="eventValueType">추가할 구체 EventValue 타입입니다.</param>
        /// <param name="eventValue">성공하면 활성화되어 장착된 EventValue입니다.</param>
        /// <returns>활성화와 장착이 모두 완료되면 <c>true</c>입니다.</returns>
        public bool TryAddEventValue(Type eventValueType, out PanBaseEventValue eventValue)
        {
            eventValue = null;
            if (!TryKeepCurrentOwner() || !IsCompatibleEventValueType(eventValueType)) { return false; }

            bool added = eventAble.EditorTryAddEventValue(owner, eventValueType, out eventValue);
            RefreshAddableEventValueTypes();
            return added;
        }



        /// <summary>
        /// 현재 EventAble이 정확히 소유한 EventValue를 비활성화하고 테이블에서 제거한 뒤 풀에 반환합니다.
        /// </summary>
        /// <param name="eventValue">제거할 EventValue 인스턴스입니다.</param>
        /// <returns>현재 테이블의 정확한 인스턴스를 제거했으면 <c>true</c>입니다.</returns>
        public bool TryRemoveEventValue(PanBaseEventValue eventValue)
        {
            if (!TryKeepCurrentOwner() ||
                eventValue == null ||
                !eventAble.EditorContainsEventValue(eventValue))
            {
                return false;
            }

            bool removed = eventAble.RemoveValue(eventValue);
            RefreshAddableEventValueTypes();
            return removed;
        }



        /// <summary>
        /// 현재 session Odin tree에서 지정한 EventValue 인스턴스를 직접 편집하는 property를 찾습니다.
        /// </summary>
        /// <param name="eventValue">현재 EventAble이 소유한 EventValue 인스턴스입니다.</param>
        /// <param name="eventValueProperty">성공하면 파생 EventValue의 전체 하위 필드를 가진 property입니다.</param>
        /// <returns>현재 tree에서 정확히 같은 EventValue 인스턴스의 편집 property를 찾으면 <c>true</c>입니다.</returns>
        public bool TryGetEventValueProperty(
            PanBaseEventValue eventValue,
            out InspectorProperty eventValueProperty)
        {
            eventValueProperty = null;

            if (!TryKeepCurrentOwner() ||
                eventValue == null ||
                !eventAble.EditorContainsEventValue(eventValue))
            {
                return false;
            }

            propertyTree.UpdateTree();

            foreach (InspectorProperty property in propertyTree.EnumerateTree(true, false))
            {
                if (property.ValueEntry != null &&
                    ReferenceEquals(property.ValueEntry.WeakSmartValue, eventValue))
                {
                    eventValueProperty = property;
                    return true;
                }
            }

            return false;
        }



        /// <summary>
        /// Odin property tree와 소유자 참조를 해제합니다.
        /// </summary>
        public void Dispose()
        {
            if (disposed) { return; }

            disposed = true;
            propertyTree?.Dispose();
            propertyTree = null;
            eventAble = null;
            owner = null;
            attachedEventValues.Clear();
            addableEventValueTypes = EmptyTypes;
            observedEventValueChangeVersion = -1;
        }



        private bool TryKeepCurrentOwner()
        {
            if (disposed || owner == null || eventAble == null || propertyTree == null) { return false; }

            try
            {
                EventAble currentEventAble = owner.EventAble;
                if (currentEventAble != null && ReferenceEquals(currentEventAble, eventAble)) { return true; }
            }
            catch
            {
                //. generation-safe handle처럼 stale 접근을 예외로 알리는 소유자도 Inspector 밖으로 예외를 누출하지 않습니다.
            }

            Dispose();
            return false;
        }



        private bool IsCompatibleEventValueType(Type eventValueType)
        {
            if (eventValueType == null ||
                eventValueType.IsAbstract ||
                eventValueType.ContainsGenericParameters ||
                !typeof(PanBaseEventValue).IsAssignableFrom(eventValueType))
            {
                return false;
            }

            foreach (Type interfaceType in eventValueType.GetInterfaces())
            {
                if (!interfaceType.IsGenericType ||
                    interfaceType.GetGenericTypeDefinition() != typeof(PanBaseEventValue.IEnableValue<>))
                {
                    continue;
                }

                Type targetType = interfaceType.GetGenericArguments()[0];
                return targetType.IsInstanceOfType(owner);
            }

            return false;
        }
    }
}
