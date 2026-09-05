using UnityEngine;

namespace Pan.HighDensityProjectile
{
    public sealed partial class ProjectileEmitter
    {
        private bool IsCurrentBodyHandle(ProjectileHandle projectileHandle)
        {
            return projectileBody != null
            && projectileHandle.IsCreated
            && ReferenceEquals(projectileHandle.Body, projectileBody);
        }



        private void EnsureProjectileBody()
        {
            if (projectileBody != null)
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



        private void EnsureProjectileIdBuffer(int requestedCount, ProjectileHandle[] projectileHandles)
        {
            int requiredLength = projectileHandles != null ? Mathf.Min(requestedCount, projectileHandles.Length) : requestedCount;
            requiredLength = Mathf.Max(1, requiredLength);
            if (projectileIdBuffer != null && projectileIdBuffer.Length >= requiredLength)
            {
                return;
            }

            int nextCapacity = projectileIdBuffer != null ? projectileIdBuffer.Length : 1;
            while (nextCapacity < requiredLength)
            {
                nextCapacity *= 2;
            }

            projectileIdBuffer = new int[nextCapacity];
        }



        /// <summary>
        /// 같은 pattern 설정에서는 burst마다 각 탄의 기준 방향을 다시 삼각함수로 계산하지 않도록 캐시합니다.
        /// base angle이나 transform 회전은 burst 단위 offset으로만 적용되므로, 캐시 invalidation 대상에서 제외합니다.
        /// </summary>
        private void EnsureDirectionCache(int requiredCount)
        {
            if (directionCache != null &&
            directionCache.Length >= requiredCount &&
            directionCacheCount == requiredCount &&
            directionCacheSpreadMode == spreadMode &&
            directionCacheSpreadAngleDegrees == spreadAngleDegrees)
            {
                return;
            }

            if (directionCache == null || directionCache.Length < requiredCount)
            {
                int nextCapacity = directionCache != null ? directionCache.Length : 1;
                while (nextCapacity < requiredCount)
                {
                    nextCapacity *= 2;
                }

                directionCache = new Vector3[nextCapacity];
            }

            for (int i = 0; i < requiredCount; i++)
            {
                float angle = ResolveProjectileAngle(i, requiredCount, 0f);
                directionCache[i] = AngleToDirection(angle);
            }

            directionCacheCount = requiredCount;
            directionCacheSpreadMode = spreadMode;
            directionCacheSpreadAngleDegrees = spreadAngleDegrees;
        }



        private Vector3 ResolveCachedDirection(int index, float cos, float sin, bool rotateDirections)
        {
            Vector3 direction = directionCache[index];
            if (!rotateDirections)
            {
                return direction;
            }

            return new Vector3(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos,
            0f);
        }



        private void EnsureSpawnDataBuffer(int requiredCount)
        {
            if (spawnDataBuffer != null && spawnDataBuffer.Length >= requiredCount)
            {
                return;
            }

            int nextCapacity = spawnDataBuffer != null ? spawnDataBuffer.Length : 1;
            while (nextCapacity < requiredCount)
            {
                nextCapacity *= 2;
            }

            spawnDataBuffer = new ProjectileSpawnRequest[nextCapacity];
        }



        private float ResolveBaseAngle()
        {
            float angle = baseAngleDegrees;
            if (useTransformRotation)
            {
                angle += transform.eulerAngles.z;
            }

            return angle;
        }



        private float ResolveProjectileAngle(int index, int count, float angleOffsetDegrees)
        {
            if (count <= 1)
            {
                return angleOffsetDegrees;
            }

            if (spreadMode == ProjectileEmitterSpreadMode.FullCircle)
            {
                return angleOffsetDegrees + spreadAngleDegrees * index / count;
            }

            float halfSpread = spreadAngleDegrees * 0.5f;
            float step = spreadAngleDegrees / (count - 1);
            return angleOffsetDegrees - halfSpread + step * index;
        }



        private static Vector3 AngleToDirection(float angleDegrees)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
        }
    }
}
