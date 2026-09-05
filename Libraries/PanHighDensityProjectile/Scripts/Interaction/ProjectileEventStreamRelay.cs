using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Pan.HighDensityProjectile
{

    /// <summary>
    /// `IPanProjectileBody`의 spawn/contact/despawn event를 같은 hierarchy의 receiver 인터페이스로 전달하는 경량 relay입니다.
    /// body backend가 native인지 managed인지 모르는 consumer UI, damage, graze 처리 코드는 이 component만 구독하면 됩니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProjectileEventStreamRelay : MonoBehaviour
    {
        [FoldoutGroup("연결"), SerializeField, LabelText("검증용 Managed Body")] private ManagedProjectileBody projectileManager;
        [FoldoutGroup("연결"), SerializeField, LabelText("Body Source"), Tooltip("ManagedProjectileBody가 아닌 IPanProjectileBody 구현체를 직접 연결할 때 사용합니다.")]
        private MonoBehaviour projectileBodySource;
        [FoldoutGroup("Receiver"), SerializeField, LabelText("Receiver Root")] private Transform receiverRoot;
        [FoldoutGroup("Receiver"), SerializeField, LabelText("비활성 Receiver 포함")] private bool includeInactiveReceivers;
        [FoldoutGroup("Receiver"), SerializeField, LabelText("OnEnable 때 Receiver 재수집")] private bool refreshReceiversOnEnable = true;
        [FoldoutGroup("연결"), SerializeField, LabelText("Body 자동 탐색")] private bool autoResolveManager = true;
        [SerializeField, Tooltip("On이면 비활성화될 때 runtime counter를 초기화합니다. 풀링 재사용 시 이전 이벤트 통계가 남지 않게 합니다.")]
        private bool resetCountersOnDisable = true;
        [SerializeField]
        [Tooltip("켜져 있으면 body의 spawn event를 receiver 계층으로 전달합니다.")]
        private bool relaySpawnEvents = true;
        [SerializeField]
        [Tooltip("contact event를 어떤 범위로 receiver에게 전달할지 결정합니다.")]
        private ProjectileContactRelayMode contactRelayMode = ProjectileContactRelayMode.All;
        [SerializeField]
        [Tooltip("켜져 있으면 body의 despawn event를 receiver 계층으로 전달합니다.")]
        private bool relayDespawnEvents = true;



        private readonly List<IProjectileSpawnReceiver> spawnReceivers = new List<IProjectileSpawnReceiver>(4);
        private readonly List<IProjectileContactReceiver> contactReceivers = new List<IProjectileContactReceiver>(4);
        private readonly List<IProjectileDespawnReceiver> despawnReceivers = new List<IProjectileDespawnReceiver>(4);
        private IPanProjectileBody projectileBody;
        private MonoBehaviour resolvedProjectileBodySource;
        private IPanProjectileBody subscribedBody;
        private bool subscribedSpawnEvents;
        private bool subscribedContactEvents;
        private bool subscribedDespawnEvents;
        private readonly List<MonoBehaviour> bodyLookupBuffer = new List<MonoBehaviour>(4);
        /// <summary>현재 relay가 구독 중이거나 구독할 `IPanProjectileBody`입니다.</summary>
        public IPanProjectileBody CurrentBody => projectileBody;

        /// <summary>spawn event를 받을 receiver 수입니다.</summary>
        public int SpawnReceiverCount => spawnReceivers.Count;

        /// <summary>contact event를 받을 receiver 수입니다.</summary>
        public int ContactReceiverCount => contactReceivers.Count;

        /// <summary>despawn event를 받을 receiver 수입니다.</summary>
        public int DespawnReceiverCount => despawnReceivers.Count;

        /// <summary>relay가 전달한 spawn event 누적 수입니다.</summary>
        public int SpawnedCount { get; private set; }

        /// <summary>relay가 전달한 contact event 누적 수입니다.</summary>
        public int ContactCount { get; private set; }

        /// <summary>relay가 전달한 despawn event 누적 수입니다.</summary>
        public int DespawnedCount { get; private set; }

        /// <summary>relay mode 때문에 전달하지 않은 contact event 누적 수입니다.</summary>
        public int FilteredContactCount { get; private set; }

        /// <summary>
        /// 비활성화될 때 spawn/contact/despawn counter를 초기화할지 결정합니다.
        /// 풀링으로 relay GameObject를 재사용하면 true가 안전하고, 누적 profile 값을 유지하려면 false로 둡니다.
        /// </summary>
        public bool ResetCountersOnDisable
        {
            get => resetCountersOnDisable;
            set => resetCountersOnDisable = value;
        }

        /// <summary>Inspector에서 `IPanProjectileBody` 구현 MonoBehaviour를 직접 주입할 수 있는 source입니다.</summary>
        public MonoBehaviour ProjectileBodySource
        {
            get => projectileBodySource != null
            ? projectileBodySource
            : projectileManager != null
            ? projectileManager
            : resolvedProjectileBodySource;
            set
            {
                if (projectileBodySource == value && resolvedProjectileBodySource == null)
                {
                    return;
                }

                Unsubscribe();
                projectileBodySource = value;
                resolvedProjectileBodySource = null;
                projectileBody = value as IPanProjectileBody;
                projectileManager = value as ManagedProjectileBody;
                SubscribeIfActive();
            }
        }

        public IPanProjectileBody ProjectileBody
        {
            get
            {
                ResolveProjectileBodyIfNeeded();
                return projectileBody;
            }
            set
            {
                if (projectileBody == value && resolvedProjectileBodySource == null)
                {
                    return;
                }

                Unsubscribe();
                projectileBody = value;
                projectileManager = value as ManagedProjectileBody;
                projectileBodySource = value as MonoBehaviour;
                resolvedProjectileBodySource = null;
                SubscribeIfActive();
            }
        }

        /// <summary>
        /// spawn event를 receiver 계층으로 전달할지 결정합니다.
        /// 비활성화하면 relay가 spawn stream을 구독하지 않으므로 대량 생성 구간의 bridge 호출을 줄일 수 있습니다.
        /// </summary>
        public bool RelaySpawnEvents
        {
            get => relaySpawnEvents;
            set
            {
                if (relaySpawnEvents == value)
                {
                    return;
                }

                relaySpawnEvents = value;
                ResubscribeIfActive();
            }
        }

        /// <summary>
        /// contact event 전달 정책입니다. 기본값은 기존 동작과 같은 `All`입니다.
        /// graze처럼 모든 contact가 필요하지 않은 흐름은 `Disabled`나 소비 여부 필터를 사용합니다.
        /// </summary>
        public ProjectileContactRelayMode ContactRelayMode
        {
            get => contactRelayMode;
            set
            {
                if (contactRelayMode == value)
                {
                    return;
                }

                contactRelayMode = value;
                ResubscribeIfActive();
            }
        }

        /// <summary>
        /// despawn event를 receiver 계층으로 전달할지 결정합니다.
        /// 비활성화하면 relay가 despawn stream을 구독하지 않습니다.
        /// </summary>
        public bool RelayDespawnEvents
        {
            get => relayDespawnEvents;
            set
            {
                if (relayDespawnEvents == value)
                {
                    return;
                }

                relayDespawnEvents = value;
                ResubscribeIfActive();
            }
        }



        private void Awake()
        {
            ResolveProjectileBodyIfNeeded();
        }



        private void OnEnable()
        {
            ResolveProjectileBodyIfNeeded();
            if (refreshReceiversOnEnable)
            {
                RefreshReceivers();
            }

            SubscribeIfActive();
        }



        private void OnDisable()
        {
            Unsubscribe();
            if (resetCountersOnDisable)
            {
                ResetCounters();
            }
        }
        [Button("Receiver 다시 수집")]
        [ContextMenu("Refresh Receivers")]
        public void RefreshReceivers()
        {
            Transform root = receiverRoot != null ? receiverRoot : transform;
            spawnReceivers.Clear();
            contactReceivers.Clear();
            despawnReceivers.Clear();

            root.GetComponentsInChildren(includeInactiveReceivers, spawnReceivers);
            root.GetComponentsInChildren(includeInactiveReceivers, contactReceivers);
            root.GetComponentsInChildren(includeInactiveReceivers, despawnReceivers);
        }



        [Button("현재 Relay 상태 로그")]
        private void LogInspectorDiagnostics()
        {
            Debug.Log($"[ProjectileEventStreamRelay] SpawnReceivers={SpawnReceiverCount}, ContactReceivers={ContactReceiverCount}, DespawnReceivers={DespawnReceiverCount}, Spawned={SpawnedCount}, Contact={ContactCount}, Despawned={DespawnedCount}", this);
        }



        public void Initialize(IPanProjectileBody body, Transform receivers = null)
        {
            ProjectileBody = body;
            InitializeReceivers(receivers);
        }



        private void InitializeReceivers(Transform receivers)
        {
            if (receivers != null)
            {
                receiverRoot = receivers;
            }

            RefreshReceivers();
            SubscribeIfActive();
        }



        public void ResetCounters()
        {
            SpawnedCount = 0;
            ContactCount = 0;
            DespawnedCount = 0;
            FilteredContactCount = 0;
        }



        private void ResolveProjectileBodyIfNeeded()
        {
            if (!autoResolveManager || projectileBody != null)
            {
                return;
            }

            if (ProjectileBodyResolver.TryResolveBodyInSelfOrParents(
            transform,
            this,
            projectileBodySource,
            projectileManager,
            resolvedProjectileBodySource,
            bodyLookupBuffer,
            out MonoBehaviour resolvedSource,
            out IPanProjectileBody resolvedBody))
            {
                projectileBody = resolvedBody;
                resolvedProjectileBodySource = resolvedSource == projectileBodySource || resolvedSource == projectileManager
                ? null
                : resolvedSource;
                if (projectileBodySource == null)
                {
                    projectileManager = resolvedSource as ManagedProjectileBody;
                }
            }
        }



        private void SubscribeIfActive()
        {
            if (!isActiveAndEnabled || projectileBody == null || subscribedBody == projectileBody)
            {
                return;
            }

            Unsubscribe();
            if (relaySpawnEvents)
            {
                projectileBody.ProjectileSpawned += HandleProjectileSpawned;
                subscribedSpawnEvents = true;
            }

            if (contactRelayMode != ProjectileContactRelayMode.Disabled)
            {
                projectileBody.ProjectileContacted += HandleProjectileContacted;
                subscribedContactEvents = true;
            }

            if (relayDespawnEvents)
            {
                projectileBody.ProjectileDespawned += HandleProjectileDespawned;
                subscribedDespawnEvents = true;
            }

            subscribedBody = projectileBody;
        }



        private void Unsubscribe()
        {
            if (subscribedBody == null)
            {
                return;
            }

            if (subscribedSpawnEvents)
            {
                subscribedBody.ProjectileSpawned -= HandleProjectileSpawned;
            }

            if (subscribedContactEvents)
            {
                subscribedBody.ProjectileContacted -= HandleProjectileContacted;
            }

            if (subscribedDespawnEvents)
            {
                subscribedBody.ProjectileDespawned -= HandleProjectileDespawned;
            }

            subscribedSpawnEvents = false;
            subscribedContactEvents = false;
            subscribedDespawnEvents = false;
            subscribedBody = null;
        }



        private void HandleProjectileSpawned(in ProjectileSnapshot snapshot)
        {
            SpawnedCount++;
            for (int i = 0; i < spawnReceivers.Count; i++)
            {
                spawnReceivers[i]?.OnProjectileSpawned(in snapshot);
            }
        }



        private void HandleProjectileContacted(in ProjectileContactPayload contact)
        {
            if (!ShouldRelayContact(in contact))
            {
                FilteredContactCount++;
                return;
            }

            ContactCount++;
            for (int i = 0; i < contactReceivers.Count; i++)
            {
                contactReceivers[i]?.OnProjectileContacted(in contact);
            }
        }



        private void HandleProjectileDespawned(in ProjectileSnapshot snapshot)
        {
            DespawnedCount++;
            for (int i = 0; i < despawnReceivers.Count; i++)
            {
                despawnReceivers[i]?.OnProjectileDespawned(in snapshot);
            }
        }



        private void ResubscribeIfActive()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            Unsubscribe();
            SubscribeIfActive();
        }



        private bool ShouldRelayContact(in ProjectileContactPayload contact)
        {
            switch (contactRelayMode)
            {
                case ProjectileContactRelayMode.All:
                return true;
                case ProjectileContactRelayMode.ConsumedOnly:
                return contact.Consumed;
                case ProjectileContactRelayMode.NonConsumedOnly:
                return !contact.Consumed;
                default:
                return false;
            }
        }
    }
}
