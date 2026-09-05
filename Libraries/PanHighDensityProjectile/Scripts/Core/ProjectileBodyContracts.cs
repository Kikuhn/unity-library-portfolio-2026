using System.Collections.Generic;
using UnityEngine;

namespace Pan.HighDensityProjectile
{

    /// <summary>
    /// 투사체의 실제 구현체가 무엇인지 숨기고, 생성/갱신/제거/스냅샷/이벤트만 노출하는 런타임 계약입니다.
    /// native, ECS, 검증용 managed backend, 별도 풀 기반 backend가 모두 이 계약으로 연결될 수 있습니다.
    /// gameplay 코드는 이 계약만 사용해 일반 물리 오브젝트처럼 탄을 생성하고, backend 내부 저장 방식에는 의존하지 않습니다.
    /// </summary>
    public interface IPanProjectileBody
    {
        /// <summary>현재 살아 있는 투사체 수입니다.</summary>
        int ActiveCount { get; }

        /// <summary>현재 backend가 추가 할당 없이 보관할 수 있는 투사체 슬롯 수입니다.</summary>
        int Capacity { get; }

        /// <summary>이 body에서 지금까지 생성된 투사체 누적 수입니다.</summary>
        int TotalSpawned { get; }

        /// <summary>수명 종료, 충돌, 수동 제거 등으로 사라진 투사체 누적 수입니다.</summary>
        int TotalDespawned { get; }

        /// <summary>target이 consume한 hit 누적 수입니다. 비소모 graze 같은 접촉은 별도 contact event로 확인합니다.</summary>
        int TotalHits { get; }

        /// <summary>투사체가 생성된 뒤 snapshot 경계로 알립니다. 호출자는 backend 내부 슬롯을 보관하지 말고 snapshot/id만 사용합니다.</summary>
        event ProjectileSnapshotHandler ProjectileSpawned;

        /// <summary>투사체가 target과 접촉했을 때 hit payload 경계로 알립니다. consume되지 않는 graze/contact도 이 event로 관찰할 수 있습니다.</summary>
        event ProjectileContactHandler ProjectileContacted;

        /// <summary>투사체가 제거된 뒤 마지막 snapshot 경계로 알립니다. pooling 재사용 전 마지막 읽기 전용 상태입니다.</summary>
        event ProjectileSnapshotHandler ProjectileDespawned;

        /// <summary>
        /// 투사체 생성을 시도합니다. 성공하면 backend 고유 id를 반환하며, capacity, 비활성 상태, disposed 상태 같은 실패 정책은 구현체가 결정합니다.
        /// 호출자는 concrete manager/native/ECS 타입을 분기하지 않고 이 경로로 생성합니다.
        /// </summary>
        bool TrySpawn(in ProjectileSpawnRequest spawnData, out int projectileId);

        /// <summary>
        /// 투사체를 생성하고 id를 반환합니다. 실패 가능성을 직접 처리해야 하는 gameplay나 검증 코드는 `TrySpawn`을 사용합니다.
        /// </summary>
        int Spawn(in ProjectileSpawnRequest spawnData);

        /// <summary>
        /// deltaTime만큼 backend simulation을 진행합니다. 구현체는 이동, 수명, target query, hit dispatch 순서를 내부 정책에 맞게 처리합니다.
        /// 호출자는 구현체가 GameObject, native array, ECS 중 무엇인지 알 필요가 없습니다.
        /// </summary>
        void Simulate(float deltaTime);

        /// <summary>현재 살아 있는 모든 투사체를 제거하고 backend 상태를 비웁니다. render/event consumer는 이후 snapshot copy 결과가 0개라고 가정할 수 있습니다.</summary>
        void ClearAll();

        /// <summary>id가 일치하는 투사체를 제거합니다. 이미 수명 종료나 hit로 사라진 id면 false를 반환합니다.</summary>
        bool Despawn(int projectileId);

        /// <summary>활성 슬롯 순서 기준 snapshot을 가져옵니다. index 순서는 backend 내부 압축/정렬 정책에 따라 simulation 뒤 바뀔 수 있습니다.</summary>
        bool TryGetSnapshot(int index, out ProjectileSnapshot snapshot);

        /// <summary>투사체 id 기준 snapshot을 가져옵니다. 이벤트 이후 특정 탄 상태를 다시 확인할 때 사용하며, 이미 제거된 id는 false를 반환합니다.</summary>
        bool TryGetSnapshotById(int projectileId, out ProjectileSnapshot snapshot);

        /// <summary>활성 투사체 snapshot을 buffer에 복사하고 복사한 개수를 반환합니다. buffer가 작으면 앞쪽 일부만 복사할 수 있습니다.</summary>
        int CopySnapshots(ProjectileSnapshot[] buffer);
    }



    /// <summary>
    /// 특정 투사체를 가리키는 가벼운 값 타입 핸들입니다.
    /// 실제 탄 상태는 `IPanProjectileBody` backend에 남기고, 호출자는 이 핸들로 snapshot 조회와 despawn만 수행합니다.
    /// 필요할 때는 읽기 전용 `IPanPhysicsActor`로도 해석할 수 있어, per-bullet component 없이 일반 물리 actor 계약에 연결할 수 있습니다.
    /// </summary>
    public readonly struct ProjectileHandle : IPanPhysicsActor
    {
        public ProjectileHandle(IPanProjectileBody body, int projectileId)
        {
            Body = body;
            ProjectileId = projectileId;
        }



        /// <summary>이 투사체를 보관하는 body backend입니다.</summary>
        public IPanProjectileBody Body { get; }

        /// <summary>body 안에서 투사체를 식별하는 id입니다.</summary>
        public int ProjectileId { get; }

        /// <summary>body와 양수 id를 모두 가진 핸들인지 확인합니다. 활성 상태 조회가 필요한 경우 `IsAlive`나 `TryGetSnapshot`을 사용합니다.</summary>
        public bool IsCreated => HasValidBody && ProjectileId > 0;



        /// <summary>현재 body 안에서 이 id의 투사체가 아직 살아 있는지 확인합니다. 내부적으로 snapshot 조회를 수행합니다.</summary>
        public bool IsAlive => TryGetSnapshot(out _);



        /// <summary>현재 snapshot을 조회합니다. 이미 제거된 탄이면 false를 반환합니다.</summary>
        public bool TryGetSnapshot(out ProjectileSnapshot snapshot)
        {
            if (!IsCreated)
            {
                snapshot = default;
                return false;
            }

            return Body.TryGetSnapshotById(ProjectileId, out snapshot);
        }



        /// <summary>
        /// 현재 투사체 snapshot을 일반 physics actor snapshot으로 변환합니다.
        /// 위치와 속도, 원형 반경만 제공하는 읽기 전용 actor이며, 이동 명령은 지원하지 않습니다.
        /// </summary>
        public bool TryGetPhysicsActorSnapshot(out PanPhysicsActorSnapshot snapshot)
        {
            if (!TryGetSnapshot(out ProjectileSnapshot projectileSnapshot))
            {
                snapshot = default;
                return false;
            }

            float radius = Mathf.Max(0f, projectileSnapshot.Kinematic.Radius);
            float diameter = radius * 2f;
            snapshot = new PanPhysicsActorSnapshot(
            Body as Component,
            projectileSnapshot.Kinematic.Position,
            projectileSnapshot.Kinematic.Velocity,
            projectileSnapshot.Kinematic.Velocity,
            PanPhysicsActorShape.Radius,
            new Vector3(diameter, diameter, projectileSnapshot.Kinematic.Use2D ? 0f : diameter),
            radius,
            true,
            true,
            true,
            false,
            false,
            false,
            true,
            projectileSnapshot.Kinematic.Use2D);
            return true;
        }



        /// <summary>이 핸들이 가리키는 투사체를 제거합니다. 이미 제거되었거나 잘못된 핸들이면 false를 반환합니다.</summary>
        public bool Despawn()
        {
            return IsCreated && Body.Despawn(ProjectileId);
        }



        Component IPanPhysicsActor.Source => Body as Component;



        bool IPanPhysicsActor.TryGetSnapshot(out PanPhysicsActorSnapshot snapshot)
        {
            return TryGetPhysicsActorSnapshot(out snapshot);
        }



        bool IPanPhysicsActor.TrySetPhysicsPosition(Vector3 position)
        {
            return false;
        }



        bool IPanPhysicsActor.TryMoveInstant(Vector3 delta)
        {
            return false;
        }



        bool IPanPhysicsActor.TryAddScaledMovement(Vector3 delta)
        {
            return false;
        }



        bool IPanPhysicsActor.TryAddUnscaledMovement(Vector3 delta)
        {
            return false;
        }



        private bool HasValidBody
        {
            get
            {
                if (Body == null)
                {
                    return false;
                }

                return !(Body is UnityEngine.Object unityObject) || unityObject != null;
            }
        }
    }



    /// <summary>
    /// 한 번에 여러 projectile spawn data를 받을 수 있는 선택 계약입니다.
    /// 기본 `IPanProjectileBody` 사용법은 유지하고, 대량 탄막 backend만 burst 생성 비용을 줄이고 싶을 때 구현합니다.
    /// caller는 이 계약이 없으면 `TrySpawn` 반복으로 fallback해야 하며, gameplay 의미는 두 경로가 같아야 합니다.
    /// </summary>
    public interface IProjectileBatchSpawnTarget
    {
        /// <summary>
        /// spawnDataBuffer의 앞쪽 count개를 순서대로 생성합니다.
        /// projectileIds가 null이 아니면 성공한 projectile id를 같은 성공 순서로 채웁니다.
        /// 반환값은 실제 생성된 projectile 수입니다. 부분 성공이 가능하므로 반환값만 authoritative count로 사용합니다.
        /// </summary>
        int TrySpawnBatch(ProjectileSpawnRequest[] spawnDataBuffer, int count, int[] projectileIds = null);
    }



    /// <summary>
    /// 호출자가 backend 종류를 몰라도 같은 방식으로 여러 투사체를 생성하게 하는 작은 유틸리티입니다.
    /// backend가 batch 생성을 지원하면 그 경로를 쓰고, 아니면 `IPanProjectileBody.TrySpawn` 반복으로 같은 의미를 유지합니다.
    /// </summary>
    public static class ProjectileSpawnUtility
    {
        /// <summary>
        /// spawnDataBuffer 앞쪽 count개를 생성하고 실제 생성된 수를 반환합니다.
        /// projectileIds가 null이 아니면 성공한 생성 순서대로 id를 채우고, 실패한 요청 범위는 0으로 정리합니다.
        /// </summary>
        public static int TrySpawnMany(
        IPanProjectileBody body,
        ProjectileSpawnRequest[] spawnDataBuffer,
        int count,
        int[] projectileIds = null)
        {
            int requestedCount = Mathf.Max(0, count);
            if (spawnDataBuffer != null)
            {
                requestedCount = Mathf.Min(requestedCount, spawnDataBuffer.Length);
            }

            if (body == null || spawnDataBuffer == null)
            {
                ClearUnusedProjectileIds(projectileIds, 0, requestedCount);
                return 0;
            }

            if (requestedCount <= 0)
            {
                return 0;
            }

            if (body is IProjectileBatchSpawnTarget batchSpawnTarget)
            {
                int batchSpawned = batchSpawnTarget.TrySpawnBatch(spawnDataBuffer, requestedCount, projectileIds);
                ClearUnusedProjectileIds(projectileIds, batchSpawned, requestedCount);
                return batchSpawned;
            }

            int spawned = 0;
            for (int i = 0; i < requestedCount; i++)
            {
                if (!body.TrySpawn(in spawnDataBuffer[i], out int projectileId))
                {
                    continue;
                }

                if (projectileIds != null && spawned < projectileIds.Length)
                {
                    projectileIds[spawned] = projectileId;
                }

                spawned++;
            }

            ClearUnusedProjectileIds(projectileIds, spawned, requestedCount);
            return spawned;
        }



        /// <summary>
        /// id buffer 결과를 값 타입 projectile handle buffer로 변환합니다.
        /// 핸들은 per-bullet GameObject를 만들지 않고 body/id만 들고 있으므로 대량 탄막 사용법을 단순화하는 용도입니다.
        /// </summary>
        public static void FillHandles(
        IPanProjectileBody body,
        int[] projectileIds,
        int spawned,
        int requestedCount,
        ProjectileHandle[] projectileHandles)
        {
            if (projectileHandles == null)
            {
                return;
            }

            int safeSpawned = Mathf.Max(0, spawned);
            int safeRequestedCount = Mathf.Max(0, requestedCount);
            int fillCount = projectileIds == null
            ? 0
            : Mathf.Min(Mathf.Min(safeSpawned, projectileIds.Length), projectileHandles.Length);

            for (int i = 0; i < fillCount; i++)
            {
                projectileHandles[i] = new ProjectileHandle(body, projectileIds[i]);
            }

            int clearEnd = Mathf.Min(safeRequestedCount, projectileHandles.Length);
            for (int i = fillCount; i < clearEnd; i++)
            {
                projectileHandles[i] = default;
            }
        }



        /// <summary>
        /// projectile id 결과 buffer를 기본값으로 비웁니다.
        /// spawn 실패, 즉시 발사 없는 pattern step, 잘못된 step 진입처럼 호출자가 이전 id를 재사용하면 안 되는 경로에서 사용합니다.
        /// </summary>
        public static void ClearProjectileIds(int[] projectileIds, int requestedCount)
        {
            ClearUnusedProjectileIds(projectileIds, 0, requestedCount);
        }



        private static void ClearUnusedProjectileIds(int[] projectileIds, int spawned, int requestedCount)
        {
            if (projectileIds == null)
            {
                return;
            }

            int start = Mathf.Clamp(spawned, 0, projectileIds.Length);
            int end = Mathf.Min(requestedCount, projectileIds.Length);
            for (int i = start; i < end; i++)
            {
                projectileIds[i] = 0;
            }
        }
    }



    /// <summary>
    /// projectile body를 owner/controller가 초기화할 때 넘기는 공통 설정입니다.
    /// 구현체는 필요한 값만 사용하고, 지원하지 않는 설정은 무시할 수 있습니다.
    /// </summary>
    public readonly struct ProjectileBodyInitializationSettings
    {
        public ProjectileBodyInitializationSettings(int capacity, int collisionBufferSize)
        {
            Capacity = capacity;
            CollisionBufferSize = collisionBufferSize;
        }



        /// <summary>body가 준비할 권장 투사체 capacity입니다.</summary>
        public int Capacity { get; }

        /// <summary>Unity Physics fallback hit buffer 크기입니다.</summary>
        public int CollisionBufferSize { get; }

    }



    /// <summary>
    /// controller/bootstrap이 concrete backend 타입을 몰라도 body 저장소와 hit/query buffer를 초기화할 수 있게 하는 선택 계약입니다.
    /// `IPanProjectileBody`의 필수 표면은 키우지 않고, 초기화 설정을 받을 수 있는 backend만 구현합니다.
    /// </summary>
    public interface IProjectileBodyInitializable
    {
        /// <summary>owner/controller가 결정한 초기화 설정을 body에 적용합니다.</summary>
        void InitializeProjectileBody(in ProjectileBodyInitializationSettings settings);
    }



    /// <summary>
    /// profiling/validation에서 backend의 최근 simulation 처리량을 읽기 위한 선택 계약입니다.
    /// `IPanProjectileBody`의 필수 표면은 키우지 않고, job/ECS/native 계열 backend만 필요할 때 구현합니다.
    /// gameplay 판정에 쓰는 값이 아니라 backend 비교와 stress 검증을 위한 관측값입니다.
    /// </summary>
    public interface IProjectileBodySimulationMetrics
    {
        /// <summary>마지막 simulation step에서 movement/update 경로가 처리한 projectile 수입니다.</summary>
        int LastMovementJobSimulatedCount { get; }
    }



    /// <summary>
    /// Unity Physics fallback hit가 receiver를 찾지 못해도 충돌 자체를 소비된 hit로 볼지 결정합니다.
    /// 플레이어 탄막처럼 벽/바닥에 닿으면 사라져야 하는 경우에 사용합니다.
    /// </summary>
    public interface IProjectilePhysicsHitPolicyConfigurable
    {
        bool ConsumePhysicsHitWithoutReceiver { get; set; }
    }



    /// <summary>
    /// projectile snapshot을 어떤 방식으로 화면에 표시할지 선택하는 공통 렌더링 모드입니다.
    /// spawn/pattern 코드는 이 값만 보고 renderer 구현체를 고르며, backend나 renderer concrete 타입별 사용법을 만들지 않습니다.
    /// </summary>
    public enum ProjectileRenderMode
    {
        /// <summary>자동 renderer를 만들지 않습니다. headless simulation, test, 외부 renderer 연결에서 사용합니다.</summary>
        None,

        /// <summary>풀링된 `SpriteRenderer`로 snapshot을 표시합니다. 디버깅과 일반 물리 탄막 확인에 적합합니다.</summary>
        PooledSpriteRenderer,

        /// <summary>consumer 프로젝트가 소유한 외부 오브젝트 풀로 snapshot을 표시합니다. 패키지는 구체 풀 타입을 알지 않습니다.</summary>
        ExternalObjectPool,

        /// <summary>`Graphics.RenderMeshInstanced` 기반 renderer로 snapshot을 표시합니다. 수백~수천 발 검증의 기본 후보입니다.</summary>
        InstancedSprite,
    }



    /// <summary>
    /// projectile 렌더링 크기를 물리 반지름 기반으로 둘지, 고정 월드 scale로 둘지 선택합니다.
    /// </summary>
    public enum ProjectileRenderScaleMode
    {
        PhysicsDiameter,
        FixedWorldScale,
    }



    /// <summary>
    /// 투사체 pattern/bootstrap 계층에서 자동 구성할 기본 body backend 종류입니다.
    /// `ProjectileBodySource`가 명시되어 있으면 이 값보다 외부 source가 우선합니다.
    /// </summary>
    public enum ProjectileBodyBackendMode
    {
        /// <summary>검증/비교용 managed 배열 backend인 `ManagedProjectileBody`를 자동 생성하거나 연결합니다.</summary>
        ManagedProjectileBody,

        /// <summary>`NativeProjectileBody`를 자동 생성하거나 연결합니다. 이동/수명 갱신은 Burst job 기반입니다.</summary>
        NativeProjectileBody,

        /// <summary>Unity `Rigidbody`/`Collider` child object를 풀링하는 일반 물리 backend를 자동 생성하거나 연결합니다.</summary>
        UnityPhysicsPooled,

        /// <summary>consumer 프로젝트가 소유한 일반 물리 오브젝트 조립 경로입니다. `ProjectilePatternController` 자동 body backend가 아닙니다.</summary>
        ExternalRigidbody2D,
    }



    /// <summary>
    /// source가 명시되지 않은 투사체 consumer가 같은 GameObject와 parent 계층에서 단일 body만 자동 재사용하도록 돕는 내부 resolver입니다.
    /// 후보 body가 여러 개면 임의 선택하지 않고 false를 반환해 caller가 default fallback이나 null 상태를 직접 결정하게 합니다.
    /// </summary>
    internal static class ProjectileBodyResolver
    {
        public static bool TryResolveBodyInSelfOrParents(
        Transform origin,
        MonoBehaviour requester,
        MonoBehaviour primarySource,
        MonoBehaviour fallbackSource,
        MonoBehaviour cachedSource,
        List<MonoBehaviour> lookupBuffer,
        out MonoBehaviour bodySource,
        out IPanProjectileBody body)
        {
            if (TryUseSource(primarySource, out bodySource, out body) ||
            TryUseSource(fallbackSource, out bodySource, out body) ||
            TryUseSource(cachedSource, out bodySource, out body))
            {
                return true;
            }

            return TryResolveSingleBodyInSelfOrParents(
            origin,
            requester,
            lookupBuffer,
            out bodySource,
            out body,
            out _);
        }



        public static bool TryResolveSingleBodyInSelfOrParents(
        Transform origin,
        MonoBehaviour requester,
        List<MonoBehaviour> lookupBuffer,
        out MonoBehaviour bodySource,
        out IPanProjectileBody body,
        out bool ambiguous)
        {
            bodySource = null;
            body = null;
            ambiguous = false;

            if (origin == null)
            {
                return false;
            }

            if (lookupBuffer == null)
            {
                lookupBuffer = new List<MonoBehaviour>(4);
            }

            if (TryResolveSingleBodyOn(origin, requester, lookupBuffer, out bodySource, out body, out bool localAmbiguous))
            {
                return true;
            }

            if (localAmbiguous)
            {
                ambiguous = true;
                return false;
            }

            Transform current = origin.parent;
            while (current != null)
            {
                if (TryResolveSingleBodyOn(current, requester, lookupBuffer, out bodySource, out body, out bool parentAmbiguous))
                {
                    return true;
                }

                if (parentAmbiguous)
                {
                    ambiguous = true;
                    return false;
                }

                current = current.parent;
            }

            return false;
        }



        private static bool TryUseSource(
        MonoBehaviour source,
        out MonoBehaviour bodySource,
        out IPanProjectileBody body)
        {
            bodySource = null;
            body = null;

            if (source == null || source is not IPanProjectileBody resolvedBody)
            {
                return false;
            }

            bodySource = source;
            body = resolvedBody;
            return true;
        }



        private static bool TryResolveSingleBodyOn(
        Transform owner,
        MonoBehaviour requester,
        List<MonoBehaviour> lookupBuffer,
        out MonoBehaviour bodySource,
        out IPanProjectileBody body,
        out bool ambiguous)
        {
            bodySource = null;
            body = null;
            ambiguous = false;
            lookupBuffer.Clear();
            owner.GetComponents(lookupBuffer);

            int bodyCount = 0;
            for (int i = 0; i < lookupBuffer.Count; i++)
            {
                MonoBehaviour component = lookupBuffer[i];
                if (component == null || component == requester)
                {
                    continue;
                }

                if (component is IPanProjectileBody candidate)
                {
                    bodyCount++;
                    bodySource = component;
                    body = candidate;
                }
            }

            lookupBuffer.Clear();
            if (bodyCount == 1)
            {
                return true;
            }

            bodySource = null;
            body = null;
            ambiguous = bodyCount > 1;
            return false;
        }
    }
}
