using UnityEngine;

namespace Pan.HighDensityProjectile
{
    public sealed partial class ProjectileInteractionPipeline
    {
        /// <summary>
        /// 2D Collider 기반 호환 경로를 처리합니다.
        /// </summary>
        private bool TryHit2D(in ProjectileInteractionRequest request)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(request.HitLayerMask);
            filter.useTriggers = request.IncludeTriggers;

            Vector2 startPosition = request.StartPosition;
            Vector2 endPosition = request.EndPosition;
            Vector2 movement = endPosition - startPosition;
            float distance = movement.magnitude;
            if (distance > 0.0001f)
            {
                int castCount = QueryCircleCast2D(startPosition, request.Snapshot.Radius, movement / distance, filter, distance);
                for (int i = 0; i < castCount; i++)
                {
                    RaycastHit2D castHit = castHitBuffer2D[i];
                    if (castHit.collider == null || IsSourceCollider(request.Source, castHit.collider))
                    {
                        continue;
                    }

                    if (TryReceive2D(in request, castHit.point, castHit.collider, castHit.normal))
                    {
                        return true;
                    }
                }
            }

            int count = QueryOverlapCircle2D(request.EndPosition, request.Snapshot.Radius, filter);
            for (int i = 0; i < count; i++)
            {
                Collider2D hit = hitBuffer2D[i];
                if (hit == null || IsSourceCollider(request.Source, hit))
                {
                    continue;
                }

                Vector3 normal = ResolveNormal(request.EndPosition, hit.bounds.center, request.Snapshot.Velocity);
                if (TryReceive2D(in request, request.EndPosition, hit, normal))
                {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// 3D Collider 기반 호환 경로를 처리합니다.
        /// </summary>
        private bool TryHit3D(in ProjectileInteractionRequest request)
        {
            Vector3 movement = request.EndPosition - request.StartPosition;
            float distance = movement.magnitude;
            if (distance > 0.0001f)
            {
                int castCount = QuerySphereCast3D(
                    request.StartPosition,
                    request.Snapshot.Radius,
                    movement / distance,
                    distance,
                    request.HitLayerMask,
                    request.IncludeTriggers);
                for (int i = 0; i < castCount; i++)
                {
                    RaycastHit castHit = castHitBuffer3D[i];
                    if (castHit.collider == null || IsSourceCollider(request.Source, castHit.collider))
                    {
                        continue;
                    }

                    if (TryReceive3D(in request, castHit.point, castHit.collider, castHit.normal))
                    {
                        return true;
                    }
                }
            }

            int count = QueryOverlapSphere3D(
                request.EndPosition,
                request.Snapshot.Radius,
                request.HitLayerMask,
                request.IncludeTriggers);
            for (int i = 0; i < count; i++)
            {
                Collider hit = hitBuffer3D[i];
                if (hit == null || IsSourceCollider(request.Source, hit))
                {
                    continue;
                }

                Vector3 normal = ResolveNormal(request.EndPosition, hit.bounds.center, request.Snapshot.Velocity);
                if (TryReceive3D(in request, request.EndPosition, hit, normal))
                {
                    return true;
                }
            }

            return false;
        }


        private int QueryCircleCast2D(Vector2 startPosition, float radius, Vector2 direction, ContactFilter2D filter, float distance)
        {
            while (true)
            {
                int count = Physics2D.CircleCast(startPosition, radius, direction, filter, castHitBuffer2D, distance);
                if (count < castHitBuffer2D.Length || !TryGrowBuffer(ref castHitBuffer2D))
                {
                    return count;
                }
            }
        }


        private int QueryOverlapCircle2D(Vector2 position, float radius, ContactFilter2D filter)
        {
            while (true)
            {
                int count = Physics2D.OverlapCircle(position, radius, filter, hitBuffer2D);
                if (count < hitBuffer2D.Length || !TryGrowBuffer(ref hitBuffer2D))
                {
                    return count;
                }
            }
        }


        private int QuerySphereCast3D(
            Vector3 startPosition,
            float radius,
            Vector3 direction,
            float distance,
            int hitLayerMask,
            bool includeTriggers)
        {
            QueryTriggerInteraction triggerInteraction = includeTriggers
                ? QueryTriggerInteraction.Collide
                : QueryTriggerInteraction.Ignore;

            while (true)
            {
                int count = Physics.SphereCastNonAlloc(
                    startPosition,
                    radius,
                    direction,
                    castHitBuffer3D,
                    distance,
                    hitLayerMask,
                    triggerInteraction);
                if (count < castHitBuffer3D.Length || !TryGrowBuffer(ref castHitBuffer3D))
                {
                    return count;
                }
            }
        }


        private int QueryOverlapSphere3D(Vector3 position, float radius, int hitLayerMask, bool includeTriggers)
        {
            QueryTriggerInteraction triggerInteraction = includeTriggers
                ? QueryTriggerInteraction.Collide
                : QueryTriggerInteraction.Ignore;

            while (true)
            {
                int count = Physics.OverlapSphereNonAlloc(
                    position,
                    radius,
                    hitBuffer3D,
                    hitLayerMask,
                    triggerInteraction);
                if (count < hitBuffer3D.Length || !TryGrowBuffer(ref hitBuffer3D))
                {
                    return count;
                }
            }
        }


        private static bool TryGrowBuffer<T>(ref T[] buffer)
        {
            int currentCapacity = buffer != null ? buffer.Length : 0;
            int nextCapacity = Mathf.Min(MaxUnityPhysicsFallbackBufferSize, Mathf.Max(1, currentCapacity * 2));
            if (nextCapacity <= currentCapacity)
            {
                return false;
            }

            System.Array.Resize(ref buffer, nextCapacity);
            return true;
        }


        private static bool IsSourceCollider(Component source, Component collider)
        {
            if (source == null || collider == null)
            {
                return false;
            }

            Transform sourceTransform = source.transform;
            Transform colliderTransform = collider.transform;
            return colliderTransform == sourceTransform || colliderTransform.IsChildOf(sourceTransform);
        }


        private static void EnsureBufferSize<T>(ref T[] buffer, int capacity)
        {
            if (buffer != null && buffer.Length == capacity)
            {
                return;
            }

            buffer = new T[capacity];
        }
    }
}
