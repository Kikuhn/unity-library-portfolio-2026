using System;
using UnityEngine;



namespace Pan.Tan.PanEvent
{
    /// <summary>
    /// TanFact를 이미 할당된 local EventAble에 전달하고 despawn 후 host를 회수합니다.
    /// </summary>
    public sealed partial class EventTanHostRegistry
    {
        private void OnTanFact(in TanFact fact)
        {
            if (!activeHosts.TryGetValue(fact.Tan, out EventHost host)) { return; }

            if (fact.Type == TanFactType.Despawned) { host.BeginRetirement(); }

            try
            {
                host.Dispatch(in fact);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            if (fact.Type != TanFactType.Despawned) { return; }

            bool reusable = false;
            try
            {
#if UNITY_EDITOR
                EditorNotifyEventAbleReleasing(fact.Tan, host.AllocatedEventAble);
#endif
                host.Release();
                reusable = true;
            }
            catch (Exception exception)
            {
                QuarantineHost(host);
                Debug.LogException(exception);
            }
            finally
            {
                if (activeHosts.TryGetValue(fact.Tan, out EventHost current) && ReferenceEquals(current, host))
                {
                    activeHosts.Remove(fact.Tan);
                }
            }

            if (reusable) { pooledHosts.Push(host); }
        }
    }
}
