using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pan.HighDensityProjectile
{

    /// <summary>
    /// 탄막 pattern의 한 단계입니다.
    /// step에 진입할 때 emitter profile을 적용하고, 필요하면 기존 탄 제거/발사 상태 초기화/즉시 burst를 수행합니다.
    /// </summary>
    [Serializable]
    public sealed class ProjectilePatternStep
    {
        [SerializeField, Tooltip("이 step에 진입할 때 ProjectileEmitter에 복사할 발사 설정입니다.")]
        private ProjectileEmitterProfile emitterProfile;

        [SerializeField, Tooltip("On이면 emitter profile은 재사용하면서 이 step의 sprite/frame index만 덮어씁니다.")]
        private bool overrideVisualFrameIndex;

        [SerializeField, Min(0), Tooltip("override가 켜져 있을 때 snapshot에 실어 보낼 sprite/frame index입니다. 0이면 renderer 기본 sprite를 사용합니다.")]
        private int visualFrameIndex;

        [SerializeField, Min(0f), Tooltip("이 step을 유지할 시간입니다. 0이면 자동 진행하지 않고 외부 제어를 기다립니다.")]
        private float duration = 1f;

        [SerializeField, Tooltip("On이면 step 진입 시 기존 투사체를 모두 제거합니다.")]
        private bool clearProjectilesOnEnter;

        [SerializeField, Tooltip("On이면 step 진입 시 emitter의 burst 누적 상태를 초기화합니다.")]
        private bool resetEmissionStateOnEnter = true;

        [SerializeField, Tooltip("On이면 step 진입 직후 burst를 한 번 발사합니다.")]
        private bool fireBurstOnEnter;

        [SerializeField, Tooltip("즉시 burst를 발사할 때 base angle에 더할 각도입니다.")]
        private float fireAngleOffsetDegrees;



        /// <summary>step 진입 시 적용할 emitter profile입니다.</summary>
        public ProjectileEmitterProfile EmitterProfile { get => emitterProfile; set => emitterProfile = value; }

        /// <summary>이 step에서 emitter profile의 visual frame index를 덮어쓸지 결정합니다.</summary>
        public bool OverrideVisualFrameIndex { get => overrideVisualFrameIndex; set => overrideVisualFrameIndex = value; }

        /// <summary>override가 켜져 있을 때 사용할 sprite/frame index입니다.</summary>
        public int VisualFrameIndex { get => visualFrameIndex; set => visualFrameIndex = Mathf.Max(0, value); }

        /// <summary>step 유지 시간입니다. 0이면 runner가 시간 기준으로 자동 진행하지 않습니다.</summary>
        public float Duration { get => duration; set => duration = Mathf.Max(0f, value); }

        /// <summary>step 진입 시 기존 투사체를 모두 제거할지 결정합니다.</summary>
        public bool ClearProjectilesOnEnter { get => clearProjectilesOnEnter; set => clearProjectilesOnEnter = value; }

        /// <summary>step 진입 시 emitter의 burst 누적 상태를 초기화할지 결정합니다.</summary>
        public bool ResetEmissionStateOnEnter { get => resetEmissionStateOnEnter; set => resetEmissionStateOnEnter = value; }

        /// <summary>step 진입 직후 burst를 한 번 발사할지 결정합니다.</summary>
        public bool FireBurstOnEnter { get => fireBurstOnEnter; set => fireBurstOnEnter = value; }

        /// <summary>즉시 burst 발사 시 base angle에 더할 각도입니다.</summary>
        public float FireAngleOffsetDegrees { get => fireAngleOffsetDegrees; set => fireAngleOffsetDegrees = value; }



        /// <summary>Inspector나 코드에서 들어온 값을 안전 범위로 보정합니다.</summary>
        public void Normalize()
        {
            duration = Mathf.Max(0f, duration);
            visualFrameIndex = Mathf.Max(0, visualFrameIndex);
        }
    }



    /// <summary>
    /// 여러 `ProjectilePatternStep`을 순서대로 실행하는 탄막 phase asset입니다.
    /// boss/enemy phase별로 생성해 `ProjectilePatternRunner` 또는 `ProjectilePatternController`에 연결합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectilePatternProfile", menuName = "Pan/HighDensityProjectile/Projectile Pattern Profile")]
    public sealed class ProjectilePatternProfile : ScriptableObject
    {
        [SerializeField, Tooltip("On이면 마지막 step 이후 첫 step으로 돌아갑니다.")]
        private bool loop = true;

        [SerializeField, Tooltip("순서대로 실행할 탄막 step 목록입니다.")]
        private List<ProjectilePatternStep> steps = new List<ProjectilePatternStep>();



        /// <summary>마지막 step 이후 첫 step으로 돌아갈지 결정합니다.</summary>
        public bool Loop { get => loop; set => loop = value; }

        /// <summary>등록된 step 수입니다.</summary>
        public int StepCount => steps != null ? steps.Count : 0;

        /// <summary>읽기 전용 step 목록입니다.</summary>
        public IReadOnlyList<ProjectilePatternStep> Steps => steps;

        /// <summary>모든 step의 emitter profile 중 한 burst에서 생성할 수 있는 최대 투사체 수입니다.</summary>
        public int MaxProjectilesPerBurst
        {
            get
            {
                if (steps == null || steps.Count == 0)
                {
                    return 0;
                }

                int maxCount = 0;
                for (int i = 0; i < steps.Count; i++)
                {
                    ProjectilePatternStep step = steps[i];
                    ProjectileEmitterProfile profile = step != null ? step.EmitterProfile : null;
                    if (profile != null)
                    {
                        maxCount = Mathf.Max(maxCount, profile.ProjectilesPerBurst);
                    }
                }

                return maxCount;
            }
        }



        private void OnValidate()
        {
            NormalizeSteps();
        }



        /// <summary>index에 해당하는 step을 가져옵니다. null step은 실패로 처리합니다.</summary>
        public bool TryGetStep(int index, out ProjectilePatternStep step)
        {
            if (steps == null || index < 0 || index >= steps.Count)
            {
                step = null;
                return false;
            }

            step = steps[index];
            return step != null;
        }



        /// <summary>외부 step 목록으로 profile step을 교체합니다. null step은 건너뜁니다.</summary>
        public void SetSteps(IEnumerable<ProjectilePatternStep> sourceSteps)
        {
            if (steps == null)
            {
                steps = new List<ProjectilePatternStep>();
            }

            steps.Clear();
            if (sourceSteps == null)
            {
                return;
            }

            foreach (ProjectilePatternStep step in sourceSteps)
            {
                if (step == null)
                {
                    continue;
                }

                step.Normalize();
                steps.Add(step);
            }
        }



        private void NormalizeSteps()
        {
            if (steps == null)
            {
                steps = new List<ProjectilePatternStep>();
                return;
            }

            for (int i = 0; i < steps.Count; i++)
            {
                steps[i]?.Normalize();
            }
        }
    }
}
