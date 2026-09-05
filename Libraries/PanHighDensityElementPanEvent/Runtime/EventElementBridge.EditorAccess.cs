#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Pan.Event;
using UnityEngine;



[assembly: InternalsVisibleTo("Pan.HighDensityElement.PanEvent.Editor")]
[assembly: InternalsVisibleTo("Pan.HighDensityElement.PanEvent.Tests.EditMode")]



namespace Pan.HighDensityElement.PanEvent
{
    /// <summary>
    /// 런타임 API를 넓히지 않고 Editor 확장에 현재 registry와 lazy EventAble 상태를 전달합니다.
    /// </summary>
    public sealed partial class EventElementHostRegistry
    {
        private static readonly List<EventElementHostRegistry> EditorRegistries =
            new List<EventElementHostRegistry>(4);



        internal static event Action<ElementKey, EventAble> EditorEventAbleReleasing;



        internal static bool EditorTryGetRegistry(
            ElementWorld targetWorld,
            out EventElementHostRegistry registry)
        {
            for (int i = EditorRegistries.Count - 1; i >= 0; i--)
            {
                EventElementHostRegistry candidate = EditorRegistries[i];
                if (candidate == null || candidate.disposed || candidate.world == null || candidate.world.IsDisposed)
                {
                    EditorRegistries.RemoveAt(i);
                    continue;
                }

                if (!ReferenceEquals(candidate.world, targetWorld)) { continue; }

                registry = candidate;
                return true;
            }

            registry = null;
            return false;
        }



        internal static bool EditorTryGetEligibleRegistry(
            ElementWorld targetWorld,
            ElementKey key,
            out EventElementHostRegistry registry)
        {
            for (int i = EditorRegistries.Count - 1; i >= 0; i--)
            {
                EventElementHostRegistry candidate = EditorRegistries[i];
                if (candidate == null || candidate.disposed || candidate.world == null || candidate.world.IsDisposed)
                {
                    EditorRegistries.RemoveAt(i);
                    continue;
                }

                if (!ReferenceEquals(candidate.world, targetWorld) || !candidate.eligibleKeys.Contains(key)) { continue; }

                registry = candidate;
                return true;
            }

            registry = null;
            return false;
        }



        internal bool EditorTryGetAllocatedEventAble(ElementKey key, out EventAble eventAble)
        {
            if (!disposed && activeHosts.TryGetValue(key, out EventHost host) && host.HasEventAble)
            {
                eventAble = host.AllocatedEventAble;
                return eventAble != null;
            }

            eventAble = null;
            return false;
        }



        internal bool EditorTryGetOwner(ElementHandle element, out EventElementHandle owner)
        {
            if (disposed ||
                element.Key.WorldId != world.WorldId ||
                !element.IsAlive ||
                !eligibleKeys.Contains(element.Key))
            {
                owner = default;
                return false;
            }

            owner = new EventElementHandle(this, element);
            return true;
        }



        private static void EditorRegister(EventElementHostRegistry registry)
        {
            if (registry != null && !EditorRegistries.Contains(registry)) { EditorRegistries.Add(registry); }
        }



        private static void EditorUnregister(EventElementHostRegistry registry)
        {
            if (registry != null) { EditorRegistries.Remove(registry); }
        }



        private static void EditorNotifyEventAbleReleasing(ElementKey key, EventAble eventAble)
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
