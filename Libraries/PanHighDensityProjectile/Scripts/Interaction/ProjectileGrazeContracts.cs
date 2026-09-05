using UnityEngine;

namespace Pan.HighDensityProjectile
{

    /// <summary>
    /// 투사체가 스치기 판정에 들어왔을 때 graze 처리 계층으로 전달하는 payload입니다.
    /// damage와 별도 payload로 두어 탄을 소비하지 않는 보상/점수/집중 게이지 흐름을 분리합니다.
    /// </summary>
    public readonly struct ProjectileGrazePayload
    {
        /// <summary>
        /// graze payload를 생성합니다. relay는 payload를 만든 중계 컴포넌트이며 receiverObject는 graze를 받은 GameObject입니다.
        /// </summary>
        public ProjectileGrazePayload(ProjectileHitPayload hit, Component relay, GameObject receiverObject, float grazeValue)
        {
            Hit = hit;
            Relay = relay;
            ReceiverObject = receiverObject;
            GrazeValue = grazeValue;
        }



        /// <summary>원본 투사체 hit payload입니다.</summary>
        public ProjectileHitPayload Hit { get; }

        /// <summary>graze payload를 생성한 relay component입니다.</summary>
        public Component Relay { get; }

        /// <summary>graze를 받은 GameObject입니다.</summary>
        public GameObject ReceiverObject { get; }

        /// <summary>relay 정책이 부여한 graze 값입니다.</summary>
        public float GrazeValue { get; }



        /// <summary>원본 hit의 projectile id shortcut입니다.</summary>
        public int ProjectileId => Hit.ProjectileId;

        /// <summary>투사체를 생성하거나 소유한 source component shortcut입니다.</summary>
        public Component Source => Hit.Source;

        /// <summary>per-projectile component shortcut입니다. data-oriented 탄막에서는 null일 수 있습니다.</summary>
        public Component Projectile => Hit.Projectile;

        /// <summary>아군/적군/중립 구분용 team id shortcut입니다.</summary>
        public int TeamId => Hit.TeamId;

        /// <summary>graze가 발생한 world position shortcut입니다.</summary>
        public Vector3 Point => Hit.Point;

        /// <summary>target 표면 normal shortcut입니다.</summary>
        public Vector3 Normal => Hit.Normal;

        /// <summary>투사체 진행 방향 shortcut입니다.</summary>
        public Vector3 Direction => Hit.Direction;

        /// <summary>2D 판정으로 생성된 hit인지 나타내는 shortcut입니다.</summary>
        public bool Is2D => Hit.Is2D;
    }



    /// <summary>
    /// graze payload를 점수, 자원, 회피 보상 같은 gameplay 로직으로 넘기는 최소 계약입니다.
    /// 반환값은 graze 처리가 수락되었는지를 의미하며, 기본 relay 정책에서는 투사체를 소비하지 않습니다.
    /// </summary>
    public interface IProjectileGrazeReceiver
    {
        /// <summary>graze payload를 처리합니다. true를 반환하면 graze가 수락된 것으로 간주합니다.</summary>
        bool TryReceiveProjectileGraze(in ProjectileGrazePayload graze);
    }
}
