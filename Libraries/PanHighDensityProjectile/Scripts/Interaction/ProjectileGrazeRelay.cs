using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pan.HighDensityProjectile
{

    /// <summary>
    /// 공통 hit payload를 graze payload로 변환해 parent 계층의 `IProjectileGrazeReceiver`에 전달하는 relay입니다.
    /// 기본적으로 투사체를 소비하지 않으므로, 탄막 회피 보상/점수/집중 게이지 같은 흐름을 damage와 분리할 수 있습니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProjectileGrazeRelay : MonoBehaviour, IProjectileHitTarget
    {
        [SerializeField, Tooltip("graze receiver가 graze를 수락했을 때 투사체를 소비할지 결정합니다. 일반 탄막 graze는 보통 Off입니다.")]
        private bool consumeProjectileWhenGrazeAccepted;

        [SerializeField, Tooltip("On이면 hit payload의 team id를 기준으로 필터링합니다.")]
        private bool useTeamFilter;

        [SerializeField, Tooltip("이 relay가 속한 team id입니다.")]
        private int teamId;

        [SerializeField, Tooltip("비어 있지 않으면 이 source의 현재 team id를 TeamId 대신 사용합니다.")]
        private MonoBehaviour teamSource;

        [SerializeField, Tooltip("On이면 같은 team id에서 온 hit를 무시합니다.")]
        private bool ignoreSameTeam = true;

        [SerializeField, Min(0f), Tooltip("graze payload에 넣을 보상 값입니다.")]
        private float grazeValue = 1f;

        [SerializeField, Tooltip("On이면 비활성 parent의 graze receiver도 검색합니다.")]
        private bool includeInactiveGrazeReceivers;

        [SerializeField, Tooltip("On이면 같은 projectile id가 같은 relay에서 graze를 여러 번 발생시키지 않게 기억합니다.")]
        private bool rememberProjectileIds = true;



        private readonly List<IProjectileGrazeReceiver> grazeReceiverBuffer = new List<IProjectileGrazeReceiver>(4);
        private readonly HashSet<int> grazedProjectileIds = new HashSet<int>();
        private IProjectileTeamSource cachedTeamSource;



        /// <summary>hit payload가 relay에 도착했을 때 발생합니다.</summary>
        public event Action<ProjectileHitPayload> HitReceived;

        /// <summary>graze payload를 만든 뒤 receiver dispatch 전에 발생합니다.</summary>
        public event Action<ProjectileGrazePayload> GrazeReceived;



        /// <summary>graze receiver가 graze를 수락했을 때 투사체를 소비할지 결정합니다.</summary>
        public bool ConsumeProjectileWhenGrazeAccepted
        {
            get => consumeProjectileWhenGrazeAccepted;
            set => consumeProjectileWhenGrazeAccepted = value;
        }

        /// <summary>team id 기반 필터를 사용할지 결정합니다.</summary>
        public bool UseTeamFilter
        {
            get => useTeamFilter;
            set => useTeamFilter = value;
        }

        /// <summary>이 relay가 속한 team id입니다.</summary>
        public int TeamId
        {
            get => teamId;
            set => teamId = value;
        }

        /// <summary>
        /// 현재 team id를 제공하는 선택 source입니다.
        /// source가 `IProjectileTeamSource`를 구현하면 수동 `TeamId` 대신 현재 값을 읽습니다.
        /// </summary>
        public MonoBehaviour TeamSource
        {
            get => teamSource;
            set
            {
                if (teamSource == value)
                {
                    return;
                }

                teamSource = value;
                cachedTeamSource = value as IProjectileTeamSource;
            }
        }

        /// <summary>현재 필터링에 사용할 실제 team id입니다.</summary>
        public int EffectiveTeamId => ResolveTeamId();

        /// <summary>같은 team id에서 온 hit를 무시할지 결정합니다.</summary>
        public bool IgnoreSameTeam
        {
            get => ignoreSameTeam;
            set => ignoreSameTeam = value;
        }

        /// <summary>graze payload에 넣을 보상 값입니다.</summary>
        public float GrazeValue
        {
            get => grazeValue;
            set => grazeValue = Mathf.Max(0f, value);
        }

        /// <summary>비활성 parent의 graze receiver도 검색할지 결정합니다.</summary>
        public bool IncludeInactiveGrazeReceivers
        {
            get => includeInactiveGrazeReceivers;
            set => includeInactiveGrazeReceivers = value;
        }

        /// <summary>같은 projectile id가 같은 relay에서 graze를 여러 번 발생시키지 않게 기억할지 결정합니다.</summary>
        public bool RememberProjectileIds
        {
            get => rememberProjectileIds;
            set => rememberProjectileIds = value;
        }

        /// <summary>이 relay가 받은 hit 누적 수입니다.</summary>
        public int HitCount { get; private set; }

        /// <summary>graze receiver에 dispatch를 시도한 누적 수입니다.</summary>
        public int GrazeDispatchCount { get; private set; }

        /// <summary>마지막으로 받은 hit payload입니다.</summary>
        public ProjectileHitPayload LastHit { get; private set; }

        /// <summary>마지막으로 생성한 graze payload입니다.</summary>
        public ProjectileGrazePayload LastGraze { get; private set; }



        private void OnValidate()
        {
            grazeValue = Mathf.Max(0f, grazeValue);
        }



        private void OnDisable()
        {
            ResetRuntimeState();
        }



        /// <summary>기억해 둔 projectile id를 모두 지웁니다. pooling 재사용 전에 호출할 수 있습니다.</summary>
        public void ClearRememberedProjectiles()
        {
            grazedProjectileIds.Clear();
        }



        /// <summary>
        /// pooling 재사용 전에 누적 counter, 마지막 payload, graze 중복 기억, 임시 receiver buffer를 초기화합니다.
        /// relay 설정값과 event 구독은 유지하므로 같은 hurtbox를 다시 활성화해도 구성은 바뀌지 않습니다.
        /// </summary>
        public void ResetRuntimeState()
        {
            HitCount = 0;
            GrazeDispatchCount = 0;
            LastHit = default;
            LastGraze = default;
            grazeReceiverBuffer.Clear();
            ClearRememberedProjectiles();
        }



        /// <summary>
        /// 공통 hit payload를 graze payload로 변환하고 parent 계층 receiver에 전달합니다.
        /// 반환값은 투사체를 소비해야 하는지를 의미합니다.
        /// </summary>
        public bool TryReceiveProjectileHit(in ProjectileHitPayload hit)
        {
            if (!isActiveAndEnabled)
            {
                return false;
            }

            if (ShouldIgnoreByTeam(in hit))
            {
                return false;
            }

            if (rememberProjectileIds && !grazedProjectileIds.Add(hit.ProjectileId))
            {
                return false;
            }

            HitCount++;
            LastHit = hit;
            HitReceived?.Invoke(hit);

            ProjectileGrazePayload graze = new ProjectileGrazePayload(hit, this, gameObject, grazeValue);
            LastGraze = graze;
            GrazeReceived?.Invoke(graze);

            grazeReceiverBuffer.Clear();
            GetComponentsInParent(includeInactiveGrazeReceivers, grazeReceiverBuffer);

            bool grazeAccepted = false;
            for (int i = 0; i < grazeReceiverBuffer.Count; i++)
            {
                IProjectileGrazeReceiver receiver = grazeReceiverBuffer[i];
                if (receiver == null)
                {
                    continue;
                }

                GrazeDispatchCount++;
                grazeAccepted |= receiver.TryReceiveProjectileGraze(in graze);
            }

            return consumeProjectileWhenGrazeAccepted && grazeAccepted;
        }



        private bool ShouldIgnoreByTeam(in ProjectileHitPayload hit)
        {
            return useTeamFilter && ignoreSameTeam && hit.TeamId == ResolveTeamId();
        }



        private int ResolveTeamId()
        {
            if (teamSource == null)
            {
                cachedTeamSource = null;
                return teamId;
            }

            if (cachedTeamSource == null)
            {
                cachedTeamSource = teamSource as IProjectileTeamSource;
            }

            return cachedTeamSource?.ProjectileTeamId ?? teamId;
        }
    }
}
