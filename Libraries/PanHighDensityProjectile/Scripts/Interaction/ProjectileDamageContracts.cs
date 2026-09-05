using UnityEngine;

namespace Pan.HighDensityProjectile
{

    /// <summary>
    /// 투사체 hit가 실제 damage 처리 계층으로 전달될 때 사용하는 payload입니다.
    /// `ProjectileHitPayload`를 그대로 감싸므로 collider 기반 hit와 collider-less target area hit를 같은 damage receiver로 보낼 수 있습니다.
    /// </summary>
    public readonly struct ProjectileDamagePayload
    {
        /// <summary>
        /// damage payload를 생성합니다. relay는 payload를 만든 중계 컴포넌트이며 receiverObject는 damage를 받은 GameObject입니다.
        /// </summary>
        public ProjectileDamagePayload(ProjectileHitPayload hit, Component relay, GameObject receiverObject, float damage)
        {
            Hit = hit;
            Relay = relay;
            ReceiverObject = receiverObject;
            Damage = damage;
        }



        /// <summary>원본 투사체 hit payload입니다.</summary>
        public ProjectileHitPayload Hit { get; }

        /// <summary>damage payload를 생성한 relay component입니다.</summary>
        public Component Relay { get; }

        /// <summary>damage를 받은 GameObject입니다.</summary>
        public GameObject ReceiverObject { get; }

        /// <summary>relay 정책이 보정한 최종 damage 값입니다.</summary>
        public float Damage { get; }



        /// <summary>원본 hit의 projectile id shortcut입니다.</summary>
        public int ProjectileId => Hit.ProjectileId;

        /// <summary>투사체를 생성하거나 소유한 source component shortcut입니다.</summary>
        public Component Source => Hit.Source;

        /// <summary>per-projectile component shortcut입니다. data-oriented 탄막에서는 null일 수 있습니다.</summary>
        public Component Projectile => Hit.Projectile;

        /// <summary>아군/적군/중립 구분용 team id shortcut입니다.</summary>
        public int TeamId => Hit.TeamId;

        /// <summary>damage가 발생한 world position shortcut입니다.</summary>
        public Vector3 Point => Hit.Point;

        /// <summary>target 표면 normal shortcut입니다.</summary>
        public Vector3 Normal => Hit.Normal;

        /// <summary>투사체 진행 방향 shortcut입니다.</summary>
        public Vector3 Direction => Hit.Direction;

        /// <summary>2D 판정으로 생성된 hit인지 나타내는 shortcut입니다.</summary>
        public bool Is2D => Hit.Is2D;
    }



    /// <summary>
    /// damage payload를 실제 체력/방어/무적 로직으로 넘기는 최소 계약입니다.
    /// 반환값은 damage가 실제로 수락되었는지를 의미하며, 투사체 소비 여부는 relay 정책에서 별도로 결정합니다.
    /// </summary>
    public interface IProjectileDamageReceiver
    {
        /// <summary>damage payload를 처리합니다. true를 반환하면 damage가 수락된 것으로 간주합니다.</summary>
        bool TryReceiveProjectileDamage(in ProjectileDamagePayload damage);
    }
}
