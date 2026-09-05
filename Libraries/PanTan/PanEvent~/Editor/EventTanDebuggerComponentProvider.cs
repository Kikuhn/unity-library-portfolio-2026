using Pan.Event;
using Pan.EventManagers.Editor;
using Pan.HighDensityElement.Editor;
using Pan.Tan.Element;
using UnityEditor;
using UnityEngine;



namespace Pan.Tan.PanEvent.Editor
{
    /// <summary>
    /// 선택된 실제 EventTan에 이미 할당된 EventAble과 EventValue만 인라인으로 편집합니다.
    /// </summary>
    [InitializeOnLoad]
    internal sealed class EventTanDebuggerComponentProvider : IElementDebuggerComponentProvider
    {
        internal const string ComponentId = "panevent.eventable";
        internal static readonly EventTanDebuggerComponentProvider Instance =
            new EventTanDebuggerComponentProvider();

        private EventAbleEditorSession session;
        private EventAble boundEventAble;
        private TanKey boundKey;



        static EventTanDebuggerComponentProvider()
        {
            ElementDebuggerComponentProviderRegistry.Register(Instance);
            ElementDebuggerSelection.Changed += Instance.ReleaseSession;
            EventTanHostRegistry.EditorEventAbleReleasing += Instance.OnEventAbleReleasing;
            AssemblyReloadEvents.beforeAssemblyReload += Cleanup;
        }

        private EventTanDebuggerComponentProvider()
        {
        }



        public int Order => 100;
        internal bool HasSession => session != null;



        public bool TryGetDescriptor(
            in ElementDebuggerInspectorContext context,
            out ElementDebuggerComponentDescriptor descriptor)
        {
            if (!TryResolve(in context, out ElementTanDebugMapping mapping))
            {
                ReleaseSession();
                descriptor = default;
                return false;
            }
            if (session != null && boundKey != mapping.Key) { ReleaseSession(); }

            if (!EventTanHostRegistry.EditorTryGetAllocatedEventAble(
                    mapping.Runtime,
                    mapping.Key,
                    out _,
                    out EventTanHandle owner,
                    out EventAble eventAble) ||
                !EnsureSession(owner, eventAble))
            {
                ReleaseSession();
                descriptor = default;
                return false;
            }

            int attachedCount = session.AttachedEventValueCount;
            descriptor = new ElementDebuggerComponentDescriptor(
                ComponentId,
                "EventAble",
                attachedCount == 0
                    ? ElementDebuggerComponentStatus.Lazy
                    : ElementDebuggerComponentStatus.Allocated,
                attachedCount == 0
                    ? "EventAble은 있지만 연결된 EventValue가 없습니다."
                    : $"연결된 EventValue {attachedCount:N0}개");
            return true;
        }



        public void OnInspectorGUI(in ElementDebuggerInspectorContext context)
        {
            if (!TryResolve(in context, out ElementTanDebugMapping mapping))
            {
                ReleaseSession();
                EditorGUILayout.HelpBox("선택한 Element의 EventTan registry를 찾을 수 없습니다.", MessageType.Info);
                return;
            }

            if (!EventTanHostRegistry.EditorTryGetAllocatedEventAble(
                    mapping.Runtime,
                    mapping.Key,
                    out EventTanHostRegistry registry,
                    out EventTanHandle owner,
                    out EventAble eventAble))
            {
                ReleaseSession();
                EditorGUILayout.HelpBox(
                    "EventAble이 아직 할당되지 않았습니다. 선택만으로 host나 table을 만들지 않습니다.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField(
                "Hosts",
                $"active {registry.ActiveHostCount:N0}, pooled {registry.PooledHostCount:N0}");

            if (!EnsureSession(owner, eventAble))
            {
                EditorGUILayout.HelpBox("EventAble Editor session을 만들 수 없습니다.", MessageType.Warning);
                return;
            }

            if (session.AttachedEventValueCount == 0)
            {
                EditorGUILayout.HelpBox("연결된 EventValue가 없습니다.", MessageType.Info);
                return;
            }

            if (!session.DrawAttachedEventValuesOnly() && !session.IsValid) { ReleaseSession(); }
        }



        internal bool TryBindAllocated(in ElementDebuggerInspectorContext context)
        {
            if (!TryResolve(in context, out ElementTanDebugMapping mapping) ||
                !EventTanHostRegistry.EditorTryGetAllocatedEventAble(
                    mapping.Runtime,
                    mapping.Key,
                    out _,
                    out EventTanHandle owner,
                    out EventAble eventAble))
            {
                ReleaseSession();
                return false;
            }

            return EnsureSession(owner, eventAble);
        }



        internal void ReleaseSession()
        {
            session?.Dispose();
            session = null;
            boundEventAble = null;
            boundKey = default;
        }



        private static bool TryResolve(
            in ElementDebuggerInspectorContext context,
            out ElementTanDebugMapping mapping)
        {
            if (context.Handle.IsAlive &&
                ElementTanDebugRegistry.TryResolve(context.World, context.Key, out mapping))
            {
                return true;
            }

            mapping = default;
            return false;
        }

        private bool EnsureSession(EventTanHandle owner, EventAble eventAble)
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
            boundKey = owner.Key;
            return true;
        }

        private void OnEventAbleReleasing(TanKey key, EventAble eventAble)
        {
            if (boundKey == key && ReferenceEquals(boundEventAble, eventAble)) { ReleaseSession(); }
        }

        private static void Cleanup()
        {
            ElementDebuggerComponentProviderRegistry.Unregister(Instance);
            ElementDebuggerSelection.Changed -= Instance.ReleaseSession;
            EventTanHostRegistry.EditorEventAbleReleasing -= Instance.OnEventAbleReleasing;
            AssemblyReloadEvents.beforeAssemblyReload -= Cleanup;
            Instance.ReleaseSession();
        }
    }
}
