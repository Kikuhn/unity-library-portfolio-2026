using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace Pan.HighDensityProjectile
{

    /// <summary>
    /// 투사체 backend의 저장소와 분리된 main-thread 상호작용 파이프라인입니다.
    /// native/ECS backend가 들어와도 target query, payload 생성, receiver dispatch, event 순서를 재사용하기 위한 내부 경계입니다.
    /// </summary>
    public sealed partial class ProjectileInteractionPipeline
    {
        private const int MaxUnityPhysicsFallbackBufferSize = 1024;



        /// <summary>투사체 1발의 상호작용 판정 전체 구간을 측정하는 Unity Profiler marker 이름입니다.</summary>
        public const string ResolveProfilerMarkerName = "Pan.Projectile.Interaction.Resolve";

        /// <summary>`IProjectileHitTarget` 중복 제거와 callback 호출 비용을 측정하는 Unity Profiler marker 이름입니다.</summary>
        public const string ReceiverDispatchProfilerMarkerName = "Pan.Projectile.Interaction.ReceiverDispatch";

        /// <summary>hit 결과를 contact 알림으로 변환하고 queue에 넣는 비용을 측정하는 Unity Profiler marker 이름입니다.</summary>
        public const string ContactNotificationProfilerMarkerName = "Pan.Projectile.Interaction.ContactNotification";

        /// <summary>보류된 spawn/contact/despawn 알림을 실제 receiver로 flush하는 비용을 측정하는 Unity Profiler marker 이름입니다.</summary>
        public const string NotificationFlushProfilerMarkerName = "Pan.Projectile.Interaction.NotificationFlush";

        /// <summary>target query가 없거나 소비하지 않았을 때 Unity Physics fallback query 비용을 측정하는 Unity Profiler marker 이름입니다.</summary>
        public const string UnityPhysicsFallbackProfilerMarkerName = "Pan.Projectile.Interaction.UnityPhysicsFallback";



        /// <summary>투사체 1발의 상호작용 판정 전체 구간 marker입니다. 검증 profile에서 recorder로 같은 marker를 읽습니다.</summary>
        public static readonly ProfilerMarker ResolveProfilerMarker = new ProfilerMarker(ProfilerCategory.Scripts, ResolveProfilerMarkerName);

        /// <summary>`IProjectileHitTarget` callback dispatch 구간 marker입니다.</summary>
        public static readonly ProfilerMarker ReceiverDispatchProfilerMarker = new ProfilerMarker(ProfilerCategory.Scripts, ReceiverDispatchProfilerMarkerName);

        /// <summary>contact payload 생성과 event queue 연결 구간 marker입니다.</summary>
        public static readonly ProfilerMarker ContactNotificationProfilerMarker = new ProfilerMarker(ProfilerCategory.Scripts, ContactNotificationProfilerMarkerName);

        /// <summary>보류 event flush 구간 marker입니다.</summary>
        public static readonly ProfilerMarker NotificationFlushProfilerMarker = new ProfilerMarker(ProfilerCategory.Scripts, NotificationFlushProfilerMarkerName);

        /// <summary>Unity Physics fallback query 구간 marker입니다.</summary>
        public static readonly ProfilerMarker UnityPhysicsFallbackProfilerMarker = new ProfilerMarker(ProfilerCategory.Scripts, UnityPhysicsFallbackProfilerMarkerName);



        private Collider2D[] hitBuffer2D;
        private Collider[] hitBuffer3D;
        private RaycastHit2D[] castHitBuffer2D;
        private RaycastHit[] castHitBuffer3D;
        private readonly List<IProjectileHitTarget> hitTargetBuffer = new List<IProjectileHitTarget>(4);
        private readonly List<IProjectileHitTarget> dispatchedHitTargetBuffer = new List<IProjectileHitTarget>(4);
        private readonly List<ProjectileNotification> pendingNotifications = new List<ProjectileNotification>(16);
        private int simulationDepth;



        /// <summary>
        /// 지정된 hit buffer 크기로 Unity Physics fallback 버퍼를 준비합니다.
        /// </summary>
        public ProjectileInteractionPipeline(int hitBufferSize)
        {
            Initialize(hitBufferSize);
        }



        /// <summary>투사체 생성 알림입니다. simulation 중 발생하면 step 종료 후 순서대로 전달합니다.</summary>
        public event ProjectileSnapshotHandler ProjectileSpawned;

        /// <summary>투사체 contact 알림입니다. receiver dispatch 결과와 같은 순서로 전달합니다.</summary>
        public event ProjectileContactHandler ProjectileContacted;

        /// <summary>투사체 제거 알림입니다. simulation 중 발생하면 step 종료 후 순서대로 전달합니다.</summary>
        public event ProjectileSnapshotHandler ProjectileDespawned;



        /// <summary>
        /// manager 초기화 또는 capacity 변경 시 runtime buffer를 다시 준비합니다.
        /// event 구독자는 pipeline 인스턴스에 남겨두고, query만 현재 component 상태로 다시 확인합니다.
        /// </summary>
        public void Initialize(int hitBufferSize)
        {
            int capacity = Mathf.Clamp(hitBufferSize, 1, MaxUnityPhysicsFallbackBufferSize);
            EnsureBufferSize(ref hitBuffer2D, capacity);
            EnsureBufferSize(ref hitBuffer3D, capacity);
            EnsureBufferSize(ref castHitBuffer2D, capacity);
            EnsureBufferSize(ref castHitBuffer3D, capacity);
        }



        /// <summary>
        /// 한 simulation step에서 발생할 수 있는 contact/despawn 알림 수를 기준으로 queue capacity를 미리 준비합니다.
        /// 대량 탄막이 한 frame에 모두 consume될 때 `List`가 반복 확장되며 만드는 managed allocation을 simulation 밖으로 이동합니다.
        /// </summary>
        public void PrepareNotificationCapacity(int expectedProjectileCount)
        {
            int projectileCount = Mathf.Max(0, expectedProjectileCount);
            long requestedCapacity = System.Math.Max(16L, (long)projectileCount * 2L);
            int capacity = requestedCapacity >= int.MaxValue ? int.MaxValue : (int)requestedCapacity;
            if (pendingNotifications.Capacity < capacity)
            {
                pendingNotifications.Capacity = capacity;
            }
        }



        /// <summary>
        /// 한 simulation tick의 상호작용 구간을 시작합니다.
        /// target query가 frame-local cache를 만들 수 있도록 begin hook을 한 번만 호출합니다.
        /// </summary>
        public void BeginSimulationStep()
        {
            simulationDepth++;
            if (simulationDepth != 1)
            {
                return;
            }

        }



        /// <summary>
        /// 한 simulation tick의 상호작용 구간을 끝내고, 보류된 spawn/contact/despawn 알림을 순서대로 flush합니다.
        /// </summary>
        public void EndSimulationStep()
        {
            if (simulationDepth <= 0)
            {
                return;
            }

            simulationDepth--;
            if (simulationDepth != 0)
            {
                return;
            }

            FlushPendingNotifications();
        }



        /// <summary>
        /// simulation 바깥에서 발생한 알림을 즉시 비웁니다. clear/pool reset 같은 외부 제어에서 사용합니다.
        /// </summary>
        public void FlushPendingNotificationsIfIdle()
        {
            if (simulationDepth == 0)
            {
                FlushPendingNotifications();
            }
        }



        /// <summary>
        /// backend가 계산한 이전/다음 위치를 기준으로 target query와 Unity Physics fallback을 실행합니다.
        /// </summary>
        public ProjectileInteractionResult Resolve(in ProjectileInteractionRequest request)
        {
            using (ResolveProfilerMarker.Auto())
            {
                dispatchedHitTargetBuffer.Clear();
                if (request.HitLayerMask == 0)
                {
                    return ProjectileInteractionResult.NoConsume;
                }

                bool consumed;
                using (UnityPhysicsFallbackProfilerMarker.Auto())
                {
                    consumed = request.Snapshot.Use2D
                    ? TryHit2D(in request)
                    : TryHit3D(in request);
                }

                return consumed ? ProjectileInteractionResult.ConsumedHit : ProjectileInteractionResult.NoConsume;
            }
        }



        /// <summary>
        /// 생성 event를 즉시 전달하거나, simulation 중이면 step 종료까지 보류합니다.
        /// </summary>
        public void NotifySpawned(in ProjectileSnapshot snapshot)
        {
            if (ProjectileSpawned == null)
            {
                return;
            }

            if (simulationDepth > 0)
            {
                pendingNotifications.Add(ProjectileNotification.Spawned(snapshot));
                return;
            }

            ProjectileSpawned.Invoke(in snapshot);
        }



        /// <summary>
        /// 제거 event를 즉시 전달하거나, simulation 중이면 step 종료까지 보류합니다.
        /// </summary>
        public void NotifyDespawned(in ProjectileSnapshot snapshot)
        {
            if (ProjectileDespawned == null)
            {
                return;
            }

            if (simulationDepth > 0)
            {
                pendingNotifications.Add(ProjectileNotification.Despawned(snapshot));
                return;
            }

            ProjectileDespawned.Invoke(in snapshot);
        }
}



    /// <summary>
    /// backend가 한 발의 이전/다음 위치를 main-thread interaction pipeline에 전달할 때 쓰는 요청입니다.
    /// native backend는 숫자 상태를 job에서 갱신한 뒤 이 요청만 만들어 gameplay dispatch를 재사용합니다.
    /// </summary>
    public readonly struct ProjectileInteractionRequest
    {
        public ProjectileInteractionRequest(
            ProjectileSnapshot snapshot,
            Component source,
            Component projectile,
            int hitLayerMask,
            Vector3 startPosition,
            Vector3 endPosition,
            bool consumePhysicsHitWithoutReceiver = false,
            bool includeTriggers = true)
        {
            Snapshot = snapshot;
            Source = source;
            Projectile = projectile;
            HitLayerMask = hitLayerMask;
            StartPosition = startPosition;
            EndPosition = endPosition;
            ConsumePhysicsHitWithoutReceiver = consumePhysicsHitWithoutReceiver;
            IncludeTriggers = includeTriggers;
        }



        public ProjectileSnapshot Snapshot { get; }

        /// <summary>투사체를 생성하거나 소유한 source component입니다.</summary>
        public Component Source { get; }

        /// <summary>per-projectile component가 있을 때의 projectile component입니다. data-oriented backend에서는 body 자신이 들어갈 수 있습니다.</summary>
        public Component Projectile { get; }

        /// <summary>Unity Physics fallback에서 사용할 layer mask입니다. 0이면 fallback을 건너뜁니다.</summary>
        public int HitLayerMask { get; }

        /// <summary>이번 simulation step에서 투사체가 출발한 world position입니다.</summary>
        public Vector3 StartPosition { get; }

        /// <summary>이번 simulation step에서 투사체가 도착한 world position입니다.</summary>
        public Vector3 EndPosition { get; }

        /// <summary>receiver가 없는 Unity Physics collider hit도 소비된 hit로 처리할지 결정합니다.</summary>
        public bool ConsumePhysicsHitWithoutReceiver { get; }

        /// <summary>
        /// Unity Physics 호환 조회에서 Trigger 형상을 포함할지 결정합니다.
        /// </summary>
        public bool IncludeTriggers { get; }
    }



    /// <summary>
    /// interaction pipeline 처리 결과입니다. backend는 consumed hit일 때만 제거와 hit 통계를 반영합니다.
    /// </summary>
    public readonly struct ProjectileInteractionResult
    {
        private ProjectileInteractionResult(bool consumed)
        {
            Consumed = consumed;
        }



        public bool Consumed { get; }



        /// <summary>receiver가 consume하지 않았거나 hit 대상이 없는 결과입니다.</summary>
        public static ProjectileInteractionResult NoConsume { get; } = new ProjectileInteractionResult(false);

        /// <summary>receiver가 consume한 hit 결과입니다. body는 이 값을 기준으로 투사체 제거와 hit 통계를 반영합니다.</summary>
        public static ProjectileInteractionResult ConsumedHit { get; } = new ProjectileInteractionResult(true);
    }
}
