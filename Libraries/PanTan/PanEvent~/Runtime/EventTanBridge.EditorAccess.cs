#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using Pan.Event;
using UnityEngine;



namespace Pan.Tan.PanEvent
{
    /// <summary>
    /// 런타임 API를 넓히지 않고 Editor provider에 실제 EventTan registry와 할당 상태를 전달합니다.
    /// </summary>
    public sealed partial class EventTanHostRegistry
    {
        private static readonly List<EventTanHostRegistry> EditorRegistries =
            new List<EventTanHostRegistry>(4);



        internal static event Action<TanKey, EventAble> EditorEventAbleReleasing;



        internal static bool EditorTryGetAllocatedEventAble(
            ITanRuntimeBackend targetRuntime,
            TanKey key,
            out EventTanHostRegistry registry,
            out EventTanHandle owner,
            out EventAble eventAble)
        {
            for (int i = EditorRegistries.Count - 1; i >= 0; i--)
            {
                EventTanHostRegistry candidate = EditorRegistries[i];
                if (candidate == null || candidate.disposed ||
                    candidate.runtime == null || !candidate.runtime.IsAvailable)
                {
                    EditorRegistries.RemoveAt(i);
                    continue;
                }
                if (!ReferenceEquals(candidate.runtime, targetRuntime) ||
                    !candidate.EditorTryGetAllocatedEventAble(key, out owner, out eventAble))
                {
                    continue;
                }

                registry = candidate;
                return true;
            }

            registry = null;
            owner = default;
            eventAble = null;
            return false;
        }



        internal bool EditorTryGetAllocatedEventAble(
            TanKey key,
            out EventTanHandle owner,
            out EventAble eventAble)
        {
            if (!disposed && activeHosts.TryGetValue(key, out EventHost host) && host.HasEventAble)
            {
                owner = host.Owner;
                eventAble = host.AllocatedEventAble;
                return owner.IsAlive && eventAble != null;
            }

            owner = default;
            eventAble = null;
            return false;
        }



        private static void EditorRegister(EventTanHostRegistry registry)
        {
            if (registry != null && !EditorRegistries.Contains(registry)) { EditorRegistries.Add(registry); }
        }

        private static void EditorUnregister(EventTanHostRegistry registry)
        {
            if (registry != null) { EditorRegistries.Remove(registry); }
        }

        private static void EditorNotifyEventAbleReleasing(TanKey key, EventAble eventAble)
        {
            if (eventAble == null) { return; }

            try
            {
                EditorEventAbleReleasing?.Invoke(key, eventAble);
            }
            catch (Exception exception)
            {
                //! Editor 구독자 예외가 despawn과 generation 전이를 막지 않도록 런타임 경계에서 격리합니다.
                Debug.LogException(exception);
            }
        }
    }
}

#endif
