using UnityEngine;
namespace Pan.HighDensityProjectile
{

    public sealed partial class ProjectileInteractionPipeline
    {
        /// <summary>
        /// receiver가 하나 이상 처리한 hit만 contact event로 노출합니다.
        /// </summary>
        private void NotifyContactIfNeeded(in ProjectileHitPayload payload, HitDispatchResult result, bool allowConsumedWithoutTarget = false)
        {
            using (ContactNotificationProfilerMarker.Auto())
            {
                if (result.TargetCount <= 0 && !(allowConsumedWithoutTarget && result.Consumed))
                {
                    return;
                }

                ProjectileContactPayload contact = new ProjectileContactPayload(payload, result.Consumed, result.TargetCount);
                NotifyContacted(in contact);
            }
        }



        private static HitDispatchResult PromotePhysicsHitIfNeeded(
        in ProjectileInteractionRequest request,
        HitDispatchResult result)
        {
            if (!request.ConsumePhysicsHitWithoutReceiver || result.TargetCount > 0 || result.Consumed)
            {
                return result;
            }

            return new HitDispatchResult(true, 0);
        }



        /// <summary>
        /// contact event를 즉시 전달하거나, simulation 중이면 step 종료까지 보류합니다.
        /// </summary>
        private void NotifyContacted(in ProjectileContactPayload contact)
        {
            if (ProjectileContacted == null)
            {
                return;
            }

            if (simulationDepth > 0)
            {
                pendingNotifications.Add(ProjectileNotification.Contacted(contact));
                return;
            }

            ProjectileContacted.Invoke(in contact);
        }



        /// <summary>
        /// 2D collider의 parent 계층에서 `IProjectileHitTarget` 구현체를 찾습니다.
        /// </summary>
        private HitDispatchResult DispatchHitTargets(Collider2D collider2D, in ProjectileHitPayload payload)
        {
            hitTargetBuffer.Clear();
            collider2D.GetComponentsInParent(false, hitTargetBuffer);
            return DispatchHitTargets(in payload);
        }



        /// <summary>
        /// 3D collider의 parent 계층에서 `IProjectileHitTarget` 구현체를 찾습니다.
        /// </summary>
        private HitDispatchResult DispatchHitTargets(Collider collider3D, in ProjectileHitPayload payload)
        {
            hitTargetBuffer.Clear();
            collider3D.GetComponentsInParent(false, hitTargetBuffer);
            return DispatchHitTargets(in payload);
        }



        /// <summary>
        /// 수집된 receiver를 중복 없이 한 번씩 호출합니다.
        /// </summary>
        private HitDispatchResult DispatchHitTargets(in ProjectileHitPayload payload)
        {
            using (ReceiverDispatchProfilerMarker.Auto())
            {
                bool consumed = false;
                int targetCount = 0;
                IProjectileHitTarget lastTarget = null;
                for (int i = 0; i < hitTargetBuffer.Count; i++)
                {
                    IProjectileHitTarget target = hitTargetBuffer[i];
                    if (target == null)
                    {
                        continue;
                    }

                    if (ReferenceEquals(target, lastTarget))
                    {
                        continue;
                    }

                    if (dispatchedHitTargetBuffer.Contains(target))
                    {
                        continue;
                    }

                    dispatchedHitTargetBuffer.Add(target);
                    lastTarget = target;
                    targetCount++;
                    consumed |= target.TryReceiveProjectileHit(in payload);
                }

                return new HitDispatchResult(consumed, targetCount);
            }
        }



        /// <summary>
        /// 보류된 spawn/contact/despawn 알림을 발생 순서대로 전달합니다.
        /// receiver가 simulation 중 manager 상태를 다시 만지는 경우를 피하기 위해 idle 시점에만 호출합니다.
        /// </summary>
        private void FlushPendingNotifications()
        {
            using (NotificationFlushProfilerMarker.Auto())
            {
                if (pendingNotifications.Count == 0)
                {
                    return;
                }

                for (int i = 0; i < pendingNotifications.Count; i++)
                {
                    ProjectileNotification notification = pendingNotifications[i];
                    switch (notification.Type)
                    {
                        case ProjectileNotificationType.Spawned:
                        ProjectileSnapshot spawnedSnapshot = notification.Snapshot;
                        ProjectileSpawned?.Invoke(in spawnedSnapshot);
                        break;
                        case ProjectileNotificationType.Contacted:
                        ProjectileContactPayload contact = notification.Contact;
                        ProjectileContacted?.Invoke(in contact);
                        break;
                        case ProjectileNotificationType.Despawned:
                        ProjectileSnapshot despawnedSnapshot = notification.Snapshot;
                        ProjectileDespawned?.Invoke(in despawnedSnapshot);
                        break;
                    }
                }

                pendingNotifications.Clear();
            }
        }



        private static Vector3 ResolveNormal(Vector3 point, Vector3 colliderCenter, Vector3 velocity)
        {
            Vector3 normal = point - colliderCenter;
            if (normal.sqrMagnitude > 0.0001f)
            {
                return normal.normalized;
            }

            if (velocity.sqrMagnitude > 0.0001f)
            {
                return -velocity.normalized;
            }

            return Vector3.zero;
        }



        private readonly struct HitDispatchResult
        {
            public HitDispatchResult(bool consumed, int targetCount)
            {
                Consumed = consumed;
                TargetCount = targetCount;
            }



            public bool Consumed { get; }

            /// <summary>이번 hit 후보를 실제 receiver가 하나 이상 처리했는지 나타냅니다.</summary>
            public int TargetCount { get; }
        }



        private enum ProjectileNotificationType
        {
            Spawned,
            Contacted,
            Despawned
        }



        private readonly struct ProjectileNotification
        {
            private ProjectileNotification(ProjectileNotificationType type, ProjectileSnapshot snapshot, ProjectileContactPayload contact)
            {
                Type = type;
                Snapshot = snapshot;
                Contact = contact;
            }



            public ProjectileNotificationType Type { get; }
            public ProjectileSnapshot Snapshot { get; }
            public ProjectileContactPayload Contact { get; }



            public static ProjectileNotification Spawned(ProjectileSnapshot snapshot)
            {
                return new ProjectileNotification(ProjectileNotificationType.Spawned, snapshot, default);
            }



            public static ProjectileNotification Contacted(ProjectileContactPayload contact)
            {
                return new ProjectileNotification(ProjectileNotificationType.Contacted, default, contact);
            }



            public static ProjectileNotification Despawned(ProjectileSnapshot snapshot)
            {
                return new ProjectileNotification(ProjectileNotificationType.Despawned, snapshot, default);
            }
        }

    }
}
