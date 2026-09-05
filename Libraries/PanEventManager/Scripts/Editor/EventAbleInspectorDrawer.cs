using System;
using System.Collections.Generic;
using Pan.Event;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;



namespace Pan.Event.Editor
{
    /// <summary>
    /// <see cref="EventAble"/>은 Odin 기본 drawer로 표시하고, 동적 목록 교체만 안전한 Repaint 종료 시점으로 예약합니다.
    /// </summary>
    public sealed class EventAbleInspectorDrawer : OdinValueDrawer<EventAble>, IDisposable
    {
        private const double DrawerIdleLifetime = 1d;
        private const double ThrottledRefreshInterval = 0.1d;



        private static readonly List<WeakReference<EventAbleInspectorDrawer>> ActiveDrawers =
            new List<WeakReference<EventAbleInspectorDrawer>>();



        private EventAble observedEventAble;
        private EditorWindow hostWindow;
        private bool schedulerRegistered;
        private bool viewCommitQueued;
        private Action commitInspectorViewAction;
        private EventAble queuedCommitEventAble;
        private double lastDrawTime;
        private double nextThrottledRefreshTime;
        private int lastObservedGameFrame = -1;



        static EventAbleInspectorDrawer()
        {
            EditorApplication.update += Editor_UpdateActiveDrawers;
            EventAble.EditorEventValueTableChanged += Editor_OnEventValueTableChanged;
            EventAble.EditorInspectorPinSettingsChanged += Editor_OnPinSettingsChanged;
        }



        /// <summary>
        /// Odin의 기본 property와 CollectionDrawer를 그대로 사용합니다.
        /// </summary>
        protected override void DrawPropertyLayout(GUIContent label)
        {
            EventAble eventAble = ValueEntry.SmartValue;
            Editor_TouchScheduler(eventAble);
            bool structuralActionsVisible =
                ValueEntry.ValueCount == 1 && !EventAble.EditorAreStructuralActionsSuppressed;
            eventAble?.EditorSetInspectorRowActionsEnabled(structuralActionsVisible);
            eventAble?.EditorSetInspectorRowActionsVisible(structuralActionsVisible);

            CallNextDrawer(label);

            Editor_QueueViewCommit(eventAble);
        }



        /// <summary>
        /// 이 drawer가 소유한 Editor 갱신 등록을 해제합니다.
        /// </summary>
        public void Dispose()
        {
            observedEventAble = null;
            hostWindow = null;
            viewCommitQueued = false;
            queuedCommitEventAble = null;
            Editor_UnregisterScheduler();
        }



        private void Editor_QueueViewCommit(EventAble eventAble)
        {
            if (eventAble == null || !eventAble.EditorNeedsInspectorViewCommit) { return; }

            queuedCommitEventAble = eventAble;
            if (viewCommitQueued) { return; }

            viewCommitQueued = true;
            commitInspectorViewAction ??= Editor_CommitQueuedView;

            //? CollectionDrawer가 현재 목록을 모두 그린 뒤에만 새 배열 참조를 공개한다.
            Property.Tree.DelayActionUntilRepaint(commitInspectorViewAction);
        }



        private void Editor_CommitQueuedView()
        {
            viewCommitQueued = false;

            EventAble eventAble = queuedCommitEventAble;
            queuedCommitEventAble = null;

            if (eventAble == null || !ReferenceEquals(observedEventAble, eventAble)) { return; }
            if (!eventAble.EditorCommitInspectorView()) { return; }

            Editor_RequestWindowRepaint();
        }



        private void Editor_TouchScheduler(EventAble eventAble)
        {
            observedEventAble = eventAble;
            lastDrawTime = EditorApplication.timeSinceStartup;

            EditorWindow currentWindow = GUIHelper.CurrentWindow;
            if (currentWindow != null) { hostWindow = currentWindow; }

            if (schedulerRegistered) { return; }

            schedulerRegistered = true;
            ActiveDrawers.Add(new WeakReference<EventAbleInspectorDrawer>(this));
        }



        private void Editor_UnregisterScheduler()
        {
            schedulerRegistered = false;

            for (int i = ActiveDrawers.Count - 1; i >= 0; i--)
            {
                if (!ActiveDrawers[i].TryGetTarget(out EventAbleInspectorDrawer drawer) ||
                    ReferenceEquals(drawer, this))
                {
                    ActiveDrawers.RemoveAt(i);
                }
            }
        }



        private void Editor_OnScheduledUpdate(double currentTime)
        {
            if (!EditorApplication.isPlaying || observedEventAble == null) { return; }
            if (currentTime - lastDrawTime > DrawerIdleLifetime) { return; }

            switch (observedEventAble.EditorInspectorRefreshMode)
            {
                case EventAbleInspectorRefreshMode.GameFrameSync:
                    int currentFrame = Time.frameCount;
                    if (currentFrame == lastObservedGameFrame) { return; }

                    lastObservedGameFrame = currentFrame;
                    Editor_RequestWindowRepaint();
                    break;

                case EventAbleInspectorRefreshMode.Throttled10Hz:
                    if (currentTime < nextThrottledRefreshTime) { return; }

                    nextThrottledRefreshTime = currentTime + ThrottledRefreshInterval;
                    Editor_RequestWindowRepaint();
                    break;

                case EventAbleInspectorRefreshMode.EditorRealtime:
                    Editor_RequestWindowRepaint();
                    break;
            }
        }



        private void Editor_RequestWindowRepaint()
        {
            if (hostWindow != null)
            {
                hostWindow.Repaint();
                return;
            }

            GUIHelper.RequestRepaint();
        }



        private static void Editor_UpdateActiveDrawers()
        {
            double currentTime = EditorApplication.timeSinceStartup;

            for (int i = ActiveDrawers.Count - 1; i >= 0; i--)
            {
                if (!ActiveDrawers[i].TryGetTarget(out EventAbleInspectorDrawer drawer))
                {
                    ActiveDrawers.RemoveAt(i);
                    continue;
                }

                drawer.Editor_OnScheduledUpdate(currentTime);
            }
        }



        private static void Editor_OnEventValueTableChanged(EventAble eventAble)
        {
            for (int i = ActiveDrawers.Count - 1; i >= 0; i--)
            {
                if (!ActiveDrawers[i].TryGetTarget(out EventAbleInspectorDrawer drawer))
                {
                    ActiveDrawers.RemoveAt(i);
                    continue;
                }

                if (ReferenceEquals(drawer.observedEventAble, eventAble))
                {
                    drawer.Editor_RequestWindowRepaint();
                }
            }
        }



        private static void Editor_OnPinSettingsChanged()
        {
            for (int i = ActiveDrawers.Count - 1; i >= 0; i--)
            {
                if (!ActiveDrawers[i].TryGetTarget(out EventAbleInspectorDrawer drawer))
                {
                    ActiveDrawers.RemoveAt(i);
                    continue;
                }

                drawer.Editor_RequestWindowRepaint();
            }
        }
    }
}
