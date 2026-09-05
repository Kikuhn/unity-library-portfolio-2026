using Pan.Event;
using Pan.EventManagers.Editor;
using Pan.HighDensityElement.Editor;
using UnityEditor;
using UnityEngine;



namespace Pan.HighDensityElement.PanEvent.Editor
{
    /// <summary>
    /// 명시적으로 EventElement로 감싼 Element의 기존 EventValue를 debugger 구성 요소로 표시합니다.
    /// </summary>
    [InitializeOnLoad]
    internal sealed class EventElementDebuggerInspectorExtension : IElementDebuggerComponentProvider
    {
        internal const string ComponentId = "panevent.eventable";
        internal static readonly EventElementDebuggerInspectorExtension Instance =
            new EventElementDebuggerInspectorExtension();

        private EventAbleEditorSession session;
        private EventAble boundEventAble;
        private ElementKey boundKey;



        static EventElementDebuggerInspectorExtension()
        {
            ElementDebuggerComponentProviderRegistry.Register(Instance);
            ElementDebuggerSelection.Changed += Instance.ReleaseSession;
            EventElementHostRegistry.EditorEventAbleReleasing += Instance.OnEventAbleReleasing;
            AssemblyReloadEvents.beforeAssemblyReload += Cleanup;
        }



        private EventElementDebuggerInspectorExtension()
        {
        }



        public int Order => 200;



        internal bool HasSession => session != null;
        internal ElementKey BoundKey => boundKey;



        public bool TryGetDescriptor(
            in ElementDebuggerInspectorContext context,
            out ElementDebuggerComponentDescriptor descriptor)
        {
            if (session != null && boundKey != context.Key) { ReleaseSession(); }

            if (!TryGetAllocatedOwner(
                    in context,
                    out _,
                    out _,
                    out int attachedCount))
            {
                ReleaseSession();
                descriptor = default;
                return false;
            }

            if (attachedCount == 0) { ReleaseSession(); }
            descriptor = new ElementDebuggerComponentDescriptor(
                ComponentId,
                "EventAble / EventValue",
                attachedCount > 0
                    ? ElementDebuggerComponentStatus.Allocated
                    : ElementDebuggerComponentStatus.Lazy,
                attachedCount > 0
                    ? $"장착된 EventValue {attachedCount:N0}개를 편집할 수 있습니다."
                    : "EventAble은 할당됐지만 장착된 EventValue가 없습니다.");
            return true;
        }



        public void OnInspectorGUI(in ElementDebuggerInspectorContext context)
        {
            if (!TryGetAllocatedOwner(
                    in context,
                    out EventElementHandle owner,
                    out EventAble eventAble,
                    out int attachedCount))
            {
                ReleaseSession();
                EditorGUILayout.HelpBox(
                    "선택만으로 EventAble을 만들지 않습니다. gameplay에서 EventValue가 장착되면 이 영역에서 편집할 수 있습니다.",
                    MessageType.Info);
                return;
            }

            if (attachedCount == 0)
            {
                ReleaseSession();
                EditorGUILayout.HelpBox("현재 장착된 EventValue가 없습니다.", MessageType.Info);
                return;
            }

            if (!EnsureSession(owner, eventAble))
            {
                EditorGUILayout.HelpBox("EventAble Editor session을 만들 수 없습니다.", MessageType.Warning);
                return;
            }

            if (!session.DrawAttachedEventValuesOnly() && !session.IsValid)
            {
                ReleaseSession();
            }
        }



        internal bool TryBindAllocatedSession(in ElementDebuggerInspectorContext context)
        {
            if (!TryGetAllocatedOwner(
                    in context,
                    out EventElementHandle owner,
                    out EventAble eventAble,
                    out int attachedCount) ||
                attachedCount == 0)
            {
                ReleaseSession();
                return false;
            }

            return EnsureSession(owner, eventAble);
        }



        private static bool TryGetAllocatedOwner(
            in ElementDebuggerInspectorContext context,
            out EventElementHandle owner,
            out EventAble eventAble,
            out int attachedCount)
        {
            attachedCount = 0;
            if (!context.Handle.IsAlive ||
                !EventElementHostRegistry.EditorTryGetEligibleRegistry(
                    context.World,
                    context.Key,
                    out EventElementHostRegistry registry) ||
                !registry.EditorTryGetAllocatedEventAble(context.Key, out eventAble) ||
                !registry.EditorTryGetOwner(context.Handle, out owner) ||
                !EventAbleEditorSession.TryGetAttachedEventValueCount(owner, out attachedCount))
            {
                owner = default;
                eventAble = null;
                attachedCount = 0;
                return false;
            }

            return true;
        }



        internal void ReleaseSession()
        {
            session?.Dispose();
            session = null;
            boundEventAble = null;
            boundKey = default;
        }



        private static void Cleanup()
        {
            ElementDebuggerComponentProviderRegistry.Unregister(Instance);
            ElementDebuggerSelection.Changed -= Instance.ReleaseSession;
            EventElementHostRegistry.EditorEventAbleReleasing -= Instance.OnEventAbleReleasing;
            Instance.ReleaseSession();
        }



        private bool EnsureSession(IEventAble owner, EventAble eventAble)
        {
            if (session != null && ReferenceEquals(boundEventAble, eventAble) && session.IsValid) { return true; }

            ReleaseSession();
            if (!EventAbleEditorSession.TryCreate(owner, out EventAbleEditorSession nextSession) ||
                !ReferenceEquals(nextSession.EventAble, eventAble))
            {
                nextSession?.Dispose();
                return false;
            }

            session = nextSession;
            boundEventAble = eventAble;
            boundKey = owner is EventElementHandle elementOwner ? elementOwner.Key : default;
            return true;
        }



        private void OnEventAbleReleasing(ElementKey key, EventAble eventAble)
        {
            if (boundKey == key && ReferenceEquals(boundEventAble, eventAble)) { ReleaseSession(); }
        }
    }
}
