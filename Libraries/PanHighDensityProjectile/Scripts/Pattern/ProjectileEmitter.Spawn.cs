using UnityEngine;

namespace Pan.HighDensityProjectile
{
    public sealed partial class ProjectileEmitter
    {
        /// <summary>
        /// 현재 transform 위치와 기본 각도로 burst를 한 번 생성합니다.
        /// </summary>
        public int SpawnBurst()
        {
            return SpawnBurst(transform.position, ResolveBaseAngle());
        }



        /// <summary>
        /// 현재 transform 위치와 기본 각도로 burst를 한 번 생성하고, 성공한 projectile id를 buffer에 채웁니다.
        /// 반환값이 항상 실제 생성 수이므로 호출자는 `projectileIds[0..return)` 범위만 사용해야 합니다.
        /// </summary>
        public int SpawnBurst(int[] projectileIds)
        {
            return SpawnBurst(transform.position, ResolveBaseAngle(), projectileIds);
        }



        /// <summary>
        /// 현재 transform 위치와 기본 각도로 burst를 한 번 생성하고, 성공한 projectile handle을 buffer에 채웁니다.
        /// handle은 body/id만 들고 있으므로 탄마다 GameObject를 만들지 않고도 특정 탄을 조회하거나 제거할 수 있습니다.
        /// </summary>
        public int SpawnBurst(ProjectileHandle[] projectileHandles)
        {
            return SpawnBurst(transform.position, ResolveBaseAngle(), projectileHandles);
        }



        /// <summary>
        /// 지정 origin과 angle offset으로 burst를 한 번 생성합니다.
        /// </summary>
        public int SpawnBurst(Vector3 origin, float angleOffsetDegrees)
        {
            return SpawnBurst(origin, angleOffsetDegrees, (int[])null);
        }



        /// <summary>
        /// 지정 origin과 angle offset으로 burst를 한 번 생성하고, 성공한 projectile id를 buffer에 채웁니다.
        /// `projectileIds`가 null이면 기존과 같이 id 복사 없이 생성만 수행합니다.
        /// </summary>
        public int SpawnBurst(Vector3 origin, float angleOffsetDegrees, int[] projectileIds)
        {
            EnsureProjectileBody();
            if (projectileBody == null)
            {
                ProjectileSpawnUtility.ClearProjectileIds(projectileIds, projectileIds != null ? projectileIds.Length : 0);
                return 0;
            }

            int count = Mathf.Max(1, projectilesPerBurst);
            EnsureSpawnDataBuffer(count);
            EnsureDirectionCache(count);

            float offsetRadians = angleOffsetDegrees * Mathf.Deg2Rad;
            bool rotateDirections = !Mathf.Approximately(angleOffsetDegrees, 0f);
            float cos = rotateDirections ? Mathf.Cos(offsetRadians) : 1f;
            float sin = rotateDirections ? Mathf.Sin(offsetRadians) : 0f;
            for (int i = 0; i < count; i++)
            {
                Vector3 direction = ResolveCachedDirection(i, cos, sin, rotateDirections);
                spawnDataBuffer[i] = new ProjectileSpawnRequest
                {
                    Source = projectileSourceOverride != null ? projectileSourceOverride : this,
                    Kinematic = new KinematicCircle2DState(origin, direction * speed, radius, use2D),
                    Lifetime = TimedLifetimeState.Start(lifetime),
                    Motion = Motion,
                    Collision = new CollisionLayerFilter(gameObject.layer, hitLayers, consumeHitWithoutReceiver, includeTriggers),
                    Team = new TeamRelationTag(teamId),
                    Hit = new HitDamageSpec(damage),
                    Visual = new SpriteVisualSpec(sprite, visualFrameIndex, fixedWorldScale),
                    QueryableByHybridPhysics2D = queryableByHybridPhysics2D,
                    StrictCcdOverride = strictCcdOverride,
                    StrictCcdThresholdRatio = strictCcdThresholdRatio,
                };
            }

            int spawned = ProjectileSpawnUtility.TrySpawnMany(projectileBody, spawnDataBuffer, count, projectileIds);

            totalBurstsEmitted++;
            totalProjectilesEmitted += spawned;
            return spawned;
        }



        /// <summary>
        /// 지정 origin과 angle offset으로 burst를 한 번 생성하고, 성공한 projectile handle을 buffer에 채웁니다.
        /// 반환값은 실제 생성 수이며, buffer가 더 작으면 `projectileHandles[0..min(return, buffer.Length))` 범위만 채워집니다.
        /// </summary>
        public int SpawnBurst(Vector3 origin, float angleOffsetDegrees, ProjectileHandle[] projectileHandles)
        {
            int requestedCount = Mathf.Max(1, projectilesPerBurst);
            EnsureProjectileIdBuffer(requestedCount, projectileHandles);
            int spawned = SpawnBurst(origin, angleOffsetDegrees, projectileIdBuffer);
            ProjectileSpawnUtility.FillHandles(projectileBody, projectileIdBuffer, spawned, requestedCount, projectileHandles);
            return spawned;
        }



        /// <summary>
        /// 연결된 projectile body의 모든 활성 투사체를 제거합니다.
        /// </summary>
        public void ClearProjectiles()
        {
            EnsureProjectileBody();
            if (projectileBody != null)
            {
                projectileBody.ClearAll();
            }
        }



        /// <summary>
        /// 연결된 projectile body에서 지정 id의 투사체를 제거합니다.
        /// 호출자는 body 구현이 manager/native/ECS 중 무엇인지 알 필요 없이 같은 emitter 경로로 개별 탄을 정리할 수 있습니다.
        /// </summary>
        public bool DespawnProjectile(int projectileId)
        {
            EnsureProjectileBody();
            return projectileBody != null && projectileBody.Despawn(projectileId);
        }



        /// <summary>
        /// 연결된 projectile body에서 지정 handle의 투사체를 제거합니다.
        /// 현재 emitter가 연결한 body에서 만들어진 handle만 처리해, 다른 spawner의 탄을 실수로 제거하지 않게 합니다.
        /// </summary>
        public bool DespawnProjectile(ProjectileHandle projectileHandle)
        {
            EnsureProjectileBody();
            return IsCurrentBodyHandle(projectileHandle) && projectileBody.Despawn(projectileHandle.ProjectileId);
        }



        /// <summary>
        /// 활성 index 기준 snapshot을 가져옵니다. index 순서는 backend 내부 저장 방식에 따라 frame마다 달라질 수 있습니다.
        /// </summary>
        public bool TryGetProjectileSnapshot(int index, out ProjectileSnapshot snapshot)
        {
            EnsureProjectileBody();
            if (projectileBody == null)
            {
                snapshot = default;
                return false;
            }

            return projectileBody.TryGetSnapshot(index, out snapshot);
        }



        /// <summary>
        /// projectile id 기준 snapshot을 가져옵니다. `SpawnBurst(..., projectileIds)`로 받은 id를 다시 확인할 때 사용합니다.
        /// </summary>
        public bool TryGetProjectileSnapshotById(int projectileId, out ProjectileSnapshot snapshot)
        {
            EnsureProjectileBody();
            if (projectileBody == null)
            {
                snapshot = default;
                return false;
            }

            return projectileBody.TryGetSnapshotById(projectileId, out snapshot);
        }



        /// <summary>
        /// projectile handle 기준 snapshot을 가져옵니다.
        /// 호출자는 id를 다시 꺼내지 않고 같은 emitter 계층에서 특정 탄을 일반 물리 오브젝트처럼 조회할 수 있습니다.
        /// </summary>
        public bool TryGetProjectileSnapshot(ProjectileHandle projectileHandle, out ProjectileSnapshot snapshot)
        {
            EnsureProjectileBody();
            if (!IsCurrentBodyHandle(projectileHandle))
            {
                snapshot = default;
                return false;
            }

            return projectileBody.TryGetSnapshotById(projectileHandle.ProjectileId, out snapshot);
        }



        /// <summary>
        /// 현재 활성 투사체 snapshot을 buffer에 복사하고 복사한 수를 반환합니다.
        /// </summary>
        public int CopyProjectileSnapshots(ProjectileSnapshot[] buffer)
        {
            EnsureProjectileBody();
            return projectileBody != null ? projectileBody.CopySnapshots(buffer) : 0;
        }
    }
}
