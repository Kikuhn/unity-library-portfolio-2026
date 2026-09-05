using UnityEngine;
namespace Pan.HighDensityProjectile
{

    public sealed partial class ProjectileInteractionPipeline
    {
        /// <summary>
        /// 2D physics hit을 gameplay payload로 바꾸고 receiver 처리 결과를 contact event로 연결합니다.
        /// </summary>
        private bool TryReceive2D(in ProjectileInteractionRequest request, Vector3 point, Collider2D collider2D, Vector3 normal)
        {
            ProjectileHitPayload payload = CreatePayload(in request, null, collider2D, point, normal, true, null, null);
            HitDispatchResult result = DispatchHitTargets(collider2D, in payload);
            result = PromotePhysicsHitIfNeeded(in request, result);
            NotifyContactIfNeeded(in payload, result, request.ConsumePhysicsHitWithoutReceiver);
            return result.Consumed;
        }



        /// <summary>
        /// 3D physics hit을 gameplay payload로 바꾸고 receiver 처리 결과를 contact event로 연결합니다.
        /// </summary>
        private bool TryReceive3D(in ProjectileInteractionRequest request, Vector3 point, Collider collider3D, Vector3 normal)
        {
            ProjectileHitPayload payload = CreatePayload(in request, collider3D, null, point, normal, false, null, null);
            HitDispatchResult result = DispatchHitTargets(collider3D, in payload);
            result = PromotePhysicsHitIfNeeded(in request, result);
            NotifyContactIfNeeded(in payload, result, request.ConsumePhysicsHitWithoutReceiver);
            return result.Consumed;
        }



        /// <summary>
        /// 물리/query hit 정보를 `ProjectileHitPayload`로 변환합니다.
        /// </summary>
        private ProjectileHitPayload CreatePayload(
        in ProjectileInteractionRequest request,
        Collider collider3D,
        Collider2D collider2D,
        Vector3 point,
        Vector3 normal,
        bool is2D,
        Component target,
        GameObject hitGameObject)
        {
            return new ProjectileHitPayload(
            request.Snapshot.ProjectileId,
            request.Source,
            request.Projectile,
            collider3D,
            collider2D,
            point,
            normal,
            request.Snapshot.Kinematic.Velocity.normalized,
            request.Snapshot.Hit.Damage,
            request.Snapshot.Team.TeamId,
            is2D,
            target,
            hitGameObject);
        }
    }
}
