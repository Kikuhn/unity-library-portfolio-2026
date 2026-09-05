using System;
using UnityEngine;

namespace Pan.HighDensityProjectile
{

    /// <summary>
    /// 일반 GameObject가 projectile damage, graze와 수신 gate를 함께 제공할 때 쓰는 작은 상태 컴포넌트입니다.
    /// 체력, 무적, 사망, pooling gate를 하나의 계약으로 노출하므로 contact resolver가 구체 물리 타입을 몰라도 됩니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProjectileInteractionState :
    MonoBehaviour,
    IProjectileDamageReceiver,
    IProjectileGrazeReceiver,
    IProjectileTeamSource
    {
        [SerializeField, Tooltip("이 object가 속한 team id입니다.")]
        private int teamId;

        [SerializeField, Min(0f), Tooltip("최대 체력입니다.")]
        private float maxLife = 1f;

        [SerializeField, Min(0f), Tooltip("현재 체력입니다.")]
        private float currentLife = 1f;

        [SerializeField, Tooltip("On이면 damage payload를 처리합니다.")]
        private bool acceptDamage = true;

        [SerializeField, Tooltip("On이면 damage를 체력에 반영하지 않고 block 처리합니다.")]
        private bool invulnerable;

        [SerializeField, Tooltip("damage를 수락했을 때 투사체를 소비할지 결정합니다.")]
        private bool consumeProjectileOnDamage = true;

        [SerializeField, Tooltip("무적 block 중에도 투사체를 소비할지 결정합니다.")]
        private bool consumeProjectileWhileInvulnerable = true;

        [SerializeField, Tooltip("On이면 graze payload를 처리합니다.")]
        private bool acceptGraze = true;

        [SerializeField, Tooltip("이 object가 projectile contact를 받을 수 있는지 결정하는 원본 gate입니다.")]
        private bool canReceiveProjectiles = true;

        [SerializeField, Tooltip("On이면 이 component가 disabled 상태일 때 projectile hit 후보에서 제외됩니다.")]
        private bool blockProjectilesWhenDisabled = true;

        [SerializeField, Tooltip("On이면 체력이 0 이하일 때 projectile hit 후보에서 제외됩니다.")]
        private bool blockProjectilesWhenDead = true;



        public event Action<ProjectileDamagePayload> DamageReceived;
        public event Action<ProjectileDamagePayload> DamageBlocked;
        public event Action<ProjectileGrazePayload> GrazeReceived;



        /// <summary>이 object가 속한 team id입니다.</summary>
        public int TeamId
        {
            get => teamId;
            set => teamId = value;
        }

        /// <summary>hurtbox/relay가 읽는 projectile team id입니다.</summary>
        public int ProjectileTeamId => teamId;

        /// <summary>최대 체력입니다.</summary>
        public float MaxLife
        {
            get => maxLife;
            set
            {
                maxLife = Mathf.Max(0f, value);
                currentLife = Mathf.Clamp(currentLife, 0f, maxLife);
            }
        }

        /// <summary>현재 체력입니다.</summary>
        public float CurrentLife
        {
            get => currentLife;
            set => currentLife = Mathf.Clamp(value, 0f, maxLife);
        }

        /// <summary>damage payload를 처리할지 결정합니다.</summary>
        public bool AcceptDamage
        {
            get => acceptDamage;
            set => acceptDamage = value;
        }

        /// <summary>damage를 체력에 반영하지 않고 block 처리할지 결정합니다.</summary>
        public bool Invulnerable
        {
            get => invulnerable;
            set => invulnerable = value;
        }

        /// <summary>damage를 수락했을 때 투사체를 소비할지 결정합니다.</summary>
        public bool ConsumeProjectileOnDamage
        {
            get => consumeProjectileOnDamage;
            set => consumeProjectileOnDamage = value;
        }

        /// <summary>무적 block 중에도 투사체를 소비할지 결정합니다.</summary>
        public bool ConsumeProjectileWhileInvulnerable
        {
            get => consumeProjectileWhileInvulnerable;
            set => consumeProjectileWhileInvulnerable = value;
        }

        /// <summary>graze payload를 처리할지 결정합니다.</summary>
        public bool AcceptGraze
        {
            get => acceptGraze;
            set => acceptGraze = value;
        }

        /// <summary>
        /// projectile contact를 받을 수 있는 최종 상태입니다.
        /// 무적은 damage block 처리를 위해 hit를 받아야 하므로 여기서 제외 조건으로 쓰지 않습니다.
        /// </summary>
        public bool CanReceiveProjectiles =>
        canReceiveProjectiles &&
        (!blockProjectilesWhenDisabled || isActiveAndEnabled) &&
        (!blockProjectilesWhenDead || !IsDead);

        /// <summary>disabled/dead 조건을 제외한 원본 projectile 수신 gate입니다.</summary>
        public bool RawCanReceiveProjectiles
        {
            get => canReceiveProjectiles;
            set => canReceiveProjectiles = value;
        }

        /// <summary>disabled 상태를 projectile 수신 차단 조건으로 사용할지 결정합니다.</summary>
        public bool BlockProjectilesWhenDisabled
        {
            get => blockProjectilesWhenDisabled;
            set => blockProjectilesWhenDisabled = value;
        }

        /// <summary>사망 상태를 projectile 수신 차단 조건으로 사용할지 결정합니다.</summary>
        public bool BlockProjectilesWhenDead
        {
            get => blockProjectilesWhenDead;
            set => blockProjectilesWhenDead = value;
        }

        /// <summary>현재 체력이 0 이하인지 나타냅니다.</summary>
        public bool IsDead => currentLife <= 0f;

        /// <summary>damage payload를 받은 누적 수입니다.</summary>
        public int DamageReceiveCount { get; private set; }

        /// <summary>실제로 체력에 반영된 damage 누적 수입니다.</summary>
        public int AcceptedDamageCount { get; private set; }

        /// <summary>무적 등으로 block된 damage 누적 수입니다.</summary>
        public int BlockedDamageCount { get; private set; }

        /// <summary>누적 damage량입니다.</summary>
        public float TotalDamageTaken { get; private set; }

        /// <summary>graze payload를 받은 누적 수입니다.</summary>
        public int GrazeReceiveCount { get; private set; }

        /// <summary>수락한 graze 누적 수입니다.</summary>
        public int AcceptedGrazeCount { get; private set; }

        /// <summary>누적 graze 값입니다.</summary>
        public float TotalGrazeValue { get; private set; }

        /// <summary>마지막 damage payload입니다.</summary>
        public ProjectileDamagePayload LastDamage { get; private set; }

        /// <summary>마지막 graze payload입니다.</summary>
        public ProjectileGrazePayload LastGraze { get; private set; }



        private void OnValidate()
        {
            maxLife = Mathf.Max(0f, maxLife);
            currentLife = Mathf.Clamp(currentLife, 0f, maxLife);
        }



        /// <summary>현재/최대 체력을 함께 설정합니다.</summary>
        public void SetLife(float current, float max)
        {
            maxLife = Mathf.Max(0f, max);
            currentLife = Mathf.Clamp(current, 0f, maxLife);
        }



        /// <summary>
        /// 체력과 누적 counter를 초기 상태로 되돌립니다.
        /// 명시적인 projectile 수신 gate는 유지하므로 pooling 정책은 호출자가 그대로 제어할 수 있습니다.
        /// </summary>
        public void ResetState()
        {
            currentLife = maxLife;
            DamageReceiveCount = 0;
            AcceptedDamageCount = 0;
            BlockedDamageCount = 0;
            TotalDamageTaken = 0f;
            GrazeReceiveCount = 0;
            AcceptedGrazeCount = 0;
            TotalGrazeValue = 0f;
            LastDamage = default;
            LastGraze = default;
        }



        /// <summary>projectile hit 후보로 다시 허용합니다.</summary>
        public void AllowProjectiles()
        {
            canReceiveProjectiles = true;
        }



        /// <summary>projectile hit 후보에서 제외합니다.</summary>
        public void BlockProjectiles()
        {
            canReceiveProjectiles = false;
        }



        /// <summary>
        /// damage payload를 처리하고, 투사체를 소비해야 하면 true를 반환합니다.
        /// </summary>
        public bool TryReceiveProjectileDamage(in ProjectileDamagePayload damage)
        {
            DamageReceiveCount++;
            LastDamage = damage;

            if (!acceptDamage)
            {
                return false;
            }

            if (invulnerable)
            {
                BlockedDamageCount++;
                DamageBlocked?.Invoke(damage);
                return consumeProjectileWhileInvulnerable;
            }

            float damageAmount = Mathf.Max(0f, damage.Damage);
            currentLife = Mathf.Clamp(currentLife - damageAmount, 0f, maxLife);
            TotalDamageTaken += damageAmount;
            AcceptedDamageCount++;
            DamageReceived?.Invoke(damage);
            return consumeProjectileOnDamage;
        }



        /// <summary>
        /// graze payload를 처리하고, graze를 수락했으면 true를 반환합니다.
        /// </summary>
        public bool TryReceiveProjectileGraze(in ProjectileGrazePayload graze)
        {
            GrazeReceiveCount++;
            LastGraze = graze;

            if (!acceptGraze)
            {
                return false;
            }

            TotalGrazeValue += Mathf.Max(0f, graze.GrazeValue);
            AcceptedGrazeCount++;
            GrazeReceived?.Invoke(graze);
            return true;
        }
    }
}
