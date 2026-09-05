using System;
using UnityEngine;



namespace Pan.Tan.PanEvent
{
    /// <summary>
    /// EventTan loadout 설치를 원자적으로 적용하고 실패 시 역순 수명주기를 보장합니다.
    /// </summary>
    public sealed partial class EventTanHostRegistry
    {
        internal bool TryApplyLoadout(
            EventTanHandle owner,
            EventTanLoadoutInstaller installer,
            out string failureReason)
        {
            failureReason = string.Empty;
            if (disposed)
            {
                failureReason = "EventTanRegistryDisposed";
                return false;
            }
            if (installer == null)
            {
                failureReason = "EventTanLoadoutInstallerMissing";
                return false;
            }
            if (!owner.Tan.IsOwnedBy(runtime) || !owner.Tan.IsAlive)
            {
                failureReason = "EventTanStaleOrForeign";
                return false;
            }
            if (activeHosts.ContainsKey(owner.Key))
            {
                failureReason = "EventTanHostAlreadyAllocated";
                return false;
            }

            EventHost host = pooledHosts.Count > 0 ? pooledHosts.Pop() : new EventHost();
            host.Acquire(owner);
            activeHosts.Add(owner.Key, host);

            string installFailure = string.Empty;
            try
            {
                host.GetOrCreate(initialEventTableCapacity);
                bool installed = installer(in owner, out installFailure);
                bool stillOwned = activeHosts.TryGetValue(owner.Key, out EventHost current) &&
                                  ReferenceEquals(current, host);
                if (installed && owner.Tan.IsAlive && stillOwned)
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(installFailure))
                {
                    installFailure = owner.Tan.IsAlive
                        ? "EventTanLoadoutRejected"
                        : "EventTanBecameStaleDuringLoadout";
                }
            }
            catch (Exception exception)
            {
                installFailure = $"EventTanLoadoutException:{exception.GetType().Name}";
                Debug.LogException(exception);
            }

            failureReason = installFailure;
            if (activeHosts.TryGetValue(owner.Key, out EventHost active) && ReferenceEquals(active, host))
            {
                RollbackLoadout(owner, host, ref failureReason);
            }
            return false;
        }



        private void RollbackLoadout(
            EventTanHandle owner,
            EventHost host,
            ref string failureReason)
        {
            if (activeHosts.TryGetValue(owner.Key, out EventHost current) && ReferenceEquals(current, host))
            {
                activeHosts.Remove(owner.Key);
            }

            try
            {
#if UNITY_EDITOR
                EditorNotifyEventAbleReleasing(owner.Key, host.AllocatedEventAble);
#endif
                host.BeginRetirement();
                host.Release();
                pooledHosts.Push(host);
            }
            catch (Exception exception)
            {
                QuarantineHost(host);
                failureReason = $"{failureReason}|EventTanLoadoutRollbackException:{exception.GetType().Name}";
                Debug.LogException(exception);
            }
        }
    }
}
