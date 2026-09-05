using UnityEngine;

namespace Pan.HighDensityProjectile
{

    /// <summary>
    /// 여러 물리 형상이 같은 논리적 피격 대상을 나타낼 때 공유 identity를 제공하는 호환 계약입니다.
    /// 0.5 production contact 경로는 resolver의 receiver instance identity를 사용하므로 이 계약을 사용하지 않습니다.
    /// </summary>
    public interface IProjectileReceiverGroupKeySource
    {
        /// <summary>
        /// 같은 대상에 속한 receiver들이 공유하는 안정적인 key입니다.
        /// </summary>
        object ProjectileReceiverGroupKey { get; }
    }

    /// <summary>
    /// 투사체 상호작용에서 현재 team id를 제공하는 선택형 계약입니다.
    /// </summary>
    public interface IProjectileTeamSource
    {
        int ProjectileTeamId { get; }
    }

    /// <summary>
    /// 투사체가 target과 만났을 때 gameplay 계층으로 전달하는 공통 hit 정보입니다.
    /// Legacy collider와 PhysicsCore Element contact가 같은 구조로 들어오도록 구성합니다.
    /// target은 이 payload만 보고 처리하며, 투사체의 Query 또는 Simulated 실행 모델을 알 필요가 없습니다.
    /// </summary>
    public readonly struct ProjectileHitPayload
    {
        /// <summary>
        /// collider 기반 hit payload를 생성합니다. `HitGameObject`는 2D/3D collider에서 지연 계산됩니다.
        /// </summary>
        public ProjectileHitPayload(
        int projectileId,
        Component source,
        Component projectile,
        Collider collider3D,
        Collider2D collider2D,
        Vector3 point,
        Vector3 normal,
        Vector3 direction,
        float damage,
        int teamId,
        bool is2D)
        : this(projectileId, source, projectile, collider3D, collider2D, point, normal, direction, damage, teamId, is2D, null, null)
        {
        }



        /// <summary>
        /// 명시 target과 hit GameObject를 포함한 hit payload를 생성합니다.
        /// Collider가 없는 논리적 hit 대상을 식별할 때 사용합니다.
        /// </summary>
        public ProjectileHitPayload(
        int projectileId,
        Component source,
        Component projectile,
        Collider collider3D,
        Collider2D collider2D,
        Vector3 point,
        Vector3 normal,
        Vector3 direction,
        float damage,
        int teamId,
        bool is2D,
        Component target,
        GameObject hitGameObject)
        {
            ProjectileId = projectileId;
            Source = source;
            Projectile = projectile;
            Collider3D = collider3D;
            Collider2D = collider2D;
            Target = target;
            explicitHitGameObject = hitGameObject;
            Point = point;
            Normal = normal;
            Direction = direction;
            Damage = damage;
            TeamId = teamId;
            Is2D = is2D;
        }



        /// <summary>hit를 발생시킨 backend 내부 투사체 id입니다.</summary>
        public int ProjectileId { get; }

        /// <summary>투사체를 생성하거나 소유한 source component입니다.</summary>
        public Component Source { get; }

        /// <summary>per-projectile component가 있을 때의 projectile component입니다. data-oriented 탄막에서는 null일 수 있으므로 필수 의존성으로 쓰지 않습니다.</summary>
        public Component Projectile { get; }

        /// <summary>target query가 명시적으로 지정한 target component입니다. collider-less hit에서는 이 값이 주 dispatch 경계입니다.</summary>
        public Component Target { get; }

        /// <summary>3D collider hit일 때의 collider입니다.</summary>
        public Collider Collider3D { get; }

        /// <summary>2D collider hit일 때의 collider입니다.</summary>
        public Collider2D Collider2D { get; }

        /// <summary>hit가 발생한 world position입니다.</summary>
        public Vector3 Point { get; }

        /// <summary>target 표면 기준 normal입니다.</summary>
        public Vector3 Normal { get; }

        /// <summary>투사체 진행 방향입니다.</summary>
        public Vector3 Direction { get; }

        /// <summary>투사체가 전달하는 기본 damage 값입니다.</summary>
        public float Damage { get; }

        /// <summary>아군/적군/중립 등 충돌 필터링에 사용하는 team id입니다.</summary>
        public int TeamId { get; }

        /// <summary>2D 판정으로 생성된 payload인지 나타냅니다.</summary>
        public bool Is2D { get; }

        private readonly GameObject explicitHitGameObject;



        /// <summary>
        /// hit를 받은 GameObject입니다. 명시값이 없으면 2D/3D collider에서 계산합니다.
        /// data-oriented query에서는 collider가 없을 수 있으므로 null도 정상 입력으로 취급합니다.
        /// </summary>
        public GameObject HitGameObject
        {
            get
            {
                if (explicitHitGameObject != null)
                {
                    return explicitHitGameObject;
                }

                if (Is2D)
                {
                    return Collider2D != null ? Collider2D.gameObject : null;
                }

                return Collider3D != null ? Collider3D.gameObject : null;
            }
        }
    }



    /// <summary>
    /// 투사체 hit를 받을 수 있는 target의 최소 계약입니다.
    /// 반환값은 이 hit가 투사체를 소비해야 하는지를 의미하며, damage/graze 처리 성공 여부와 반드시 같을 필요는 없습니다.
    /// target 구현체는 구체 actor 타입 대신 payload와 자신의 상태만 보고 처리합니다.
    /// </summary>
    public interface IProjectileHitTarget
    {
        /// <summary>
        /// 투사체 hit를 처리합니다. true를 반환하면 body는 해당 투사체를 소비된 hit로 집계하고 제거할 수 있습니다.
        /// false는 비소모 접촉, graze, 무적 상태 통과처럼 투사체를 계속 유지해야 하는 경우에 사용합니다.
        /// </summary>
        bool TryReceiveProjectileHit(in ProjectileHitPayload hit);
    }
}
