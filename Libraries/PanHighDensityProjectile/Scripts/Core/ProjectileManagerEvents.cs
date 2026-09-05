namespace Pan.HighDensityProjectile
{

    /// <summary>
    /// `ProjectileEventStreamRelay`가 contact event를 receiver에게 전달하는 범위를 결정합니다.
    /// 대량 탄막에서는 모든 접촉을 gameplay event로 흘리는 비용이 크므로, relay 단위로 필요한 stream만 켭니다.
    /// </summary>
    public enum ProjectileContactRelayMode
    {
        /// <summary>소비/비소비 contact를 모두 전달합니다. 기존 동작과 같은 기본값입니다.</summary>
        All,

        /// <summary>투사체를 소비한 contact만 전달합니다.</summary>
        ConsumedOnly,

        /// <summary>투사체를 소비하지 않은 contact만 전달합니다.</summary>
        NonConsumedOnly,

        /// <summary>contact stream을 구독하지 않습니다. 접촉 통계/receiver 호출 비용을 줄일 때 사용합니다.</summary>
        Disabled
    }



    /// <summary>
    /// 투사체가 target과 접촉했을 때 manager가 외부로 내보내는 event payload입니다.
    /// 실제 hit 정보와 투사체 소비 여부를 함께 보관합니다.
    /// </summary>
    public readonly struct ProjectileContactPayload
    {
        /// <summary>
        /// hit payload, consume 여부, dispatch된 target 수를 묶어 contact payload를 생성합니다.
        /// </summary>
        public ProjectileContactPayload(ProjectileHitPayload hit, bool consumed, int targetCount)
        {
            Hit = hit;
            Consumed = consumed;
            TargetCount = targetCount;
        }



        /// <summary>실제 hit 세부 정보입니다.</summary>
        public ProjectileHitPayload Hit { get; }

        /// <summary>이 contact가 투사체를 소비했는지 나타냅니다.</summary>
        public bool Consumed { get; }

        /// <summary>해당 hit payload를 받은 target receiver 수입니다.</summary>
        public int TargetCount { get; }



        /// <summary>hit를 발생시킨 투사체 id입니다.</summary>
        public int ProjectileId => Hit.ProjectileId;

        /// <summary>투사체 source component입니다.</summary>
        public UnityEngine.Component Source => Hit.Source;

        /// <summary>per-projectile component가 있을 때의 projectile component입니다.</summary>
        public UnityEngine.Component Projectile => Hit.Projectile;

        /// <summary>투사체 team id입니다.</summary>
        public int TeamId => Hit.TeamId;

        /// <summary>contact world position입니다.</summary>
        public UnityEngine.Vector3 Point => Hit.Point;

        /// <summary>contact normal입니다.</summary>
        public UnityEngine.Vector3 Normal => Hit.Normal;

        /// <summary>투사체 진행 방향입니다.</summary>
        public UnityEngine.Vector3 Direction => Hit.Direction;

        /// <summary>contact에 포함된 기본 damage 값입니다.</summary>
        public float Damage => Hit.Damage;

        /// <summary>2D contact인지 나타냅니다.</summary>
        public bool Is2D => Hit.Is2D;

        /// <summary>hit를 받은 GameObject입니다.</summary>
        public UnityEngine.GameObject HitGameObject => Hit.HitGameObject;
    }



    /// <summary>투사체 snapshot event를 구독하기 위한 allocation-free delegate입니다.</summary>
    public delegate void ProjectileSnapshotHandler(in ProjectileSnapshot snapshot);

    /// <summary>투사체 contact event를 구독하기 위한 allocation-free delegate입니다.</summary>
    public delegate void ProjectileContactHandler(in ProjectileContactPayload contact);



    /// <summary>
    /// `ProjectileEventStreamRelay`가 spawn event를 component 계층으로 전달할 때 사용하는 receiver 계약입니다.
    /// </summary>
    public interface IProjectileSpawnReceiver
    {
        /// <summary>투사체가 생성된 직후 호출됩니다.</summary>
        void OnProjectileSpawned(in ProjectileSnapshot snapshot);
    }



    /// <summary>
    /// `ProjectileEventStreamRelay`가 contact event를 component 계층으로 전달할 때 사용하는 receiver 계약입니다.
    /// </summary>
    public interface IProjectileContactReceiver
    {
        /// <summary>투사체가 target과 접촉한 뒤 호출됩니다.</summary>
        void OnProjectileContacted(in ProjectileContactPayload contact);
    }



    /// <summary>
    /// `ProjectileEventStreamRelay`가 despawn event를 component 계층으로 전달할 때 사용하는 receiver 계약입니다.
    /// </summary>
    public interface IProjectileDespawnReceiver
    {
        /// <summary>투사체가 제거된 직후 호출됩니다.</summary>
        void OnProjectileDespawned(in ProjectileSnapshot snapshot);
    }
}
