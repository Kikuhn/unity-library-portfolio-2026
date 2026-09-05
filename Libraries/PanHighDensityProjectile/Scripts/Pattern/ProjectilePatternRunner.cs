using UnityEngine;

namespace Pan.HighDensityProjectile
{

    /// <summary>
    /// `ProjectilePatternProfile`의 step을 시간 순서대로 실행하는 runtime runner입니다.
    /// 실제 탄 생성은 `ProjectileEmitter`에 위임하므로, body backend가 manager/native/ECS로 바뀌어도 pattern 흐름은 유지됩니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProjectilePatternRunner : MonoBehaviour
    {
        // 아주 작은 duration을 가진 loop pattern이 한 frame 안에서 과도하게 순환하지 않도록 막는 안전장치입니다.
        private const int MaxStepTransitionsPerTick = 1024;

        [SerializeField, Tooltip("패턴 step이 제어할 emitter입니다. 비워두면 같은 GameObject에서 자동 검색합니다.")]
        private ProjectileEmitter projectileEmitter;

        [SerializeField, Tooltip("실행할 탄막 pattern profile입니다.")]
        private ProjectilePatternProfile patternProfile;

        [SerializeField, Tooltip("On이면 OnEnable에서 pattern을 처음부터 재생합니다.")]
        private bool playOnEnable;

        [SerializeField, Tooltip("On이면 Update 자동 Tick에서 Time.unscaledDeltaTime을 사용합니다.")]
        private bool useUnscaledDeltaTime;



        private int currentStepIndex = -1;
        private float elapsedInStep;
        private bool isPlaying;
        private bool isCompleted;
        private int loopCount;
        private int lastEnterSpawnedCount;



        /// <summary>패턴 step이 제어할 emitter입니다.</summary>
        public ProjectileEmitter ProjectileEmitter
        {
            get => projectileEmitter;
            set => projectileEmitter = value;
        }

        /// <summary>실행할 pattern profile입니다. 변경하면 현재 재생을 멈춥니다.</summary>
        public ProjectilePatternProfile PatternProfile
        {
            get => patternProfile;
            set
            {
                if (patternProfile == value)
                {
                    return;
                }

                patternProfile = value;
                Stop();
            }
        }

        /// <summary>OnEnable에서 pattern을 자동 재생할지 결정합니다.</summary>
        public bool PlayOnEnable { get => playOnEnable; set => playOnEnable = value; }

        /// <summary>Update 자동 Tick에서 unscaled time을 사용할지 결정합니다.</summary>
        public bool UseUnscaledDeltaTime { get => useUnscaledDeltaTime; set => useUnscaledDeltaTime = value; }

        /// <summary>현재 실행 중인 step index입니다. 재생 중이 아니면 -1입니다.</summary>
        public int CurrentStepIndex => currentStepIndex;

        /// <summary>현재 step에 진입한 뒤 흐른 시간입니다.</summary>
        public float ElapsedInStep => elapsedInStep;

        /// <summary>runner가 pattern을 재생 중인지 나타냅니다.</summary>
        public bool IsPlaying => isPlaying;

        /// <summary>loop가 꺼진 pattern이 마지막 step을 끝냈는지 나타냅니다.</summary>
        public bool IsCompleted => isCompleted;

        /// <summary>loop pattern에서 첫 step으로 되돌아간 횟수입니다.</summary>
        public int LoopCount => loopCount;

        /// <summary>마지막 step 진입 시 즉시 burst로 생성된 투사체 수입니다.</summary>
        public int LastEnterSpawnedCount => lastEnterSpawnedCount;



        private void Awake()
        {
            EnsureEmitter();
        }



        private void OnEnable()
        {
            if (playOnEnable)
            {
                Restart();
            }
        }



        private void Update()
        {
            if (!isPlaying)
            {
                return;
            }

            Tick(useUnscaledDeltaTime ? Time.unscaledDeltaTime : Time.deltaTime);
        }



        /// <summary>
        /// emitter와 pattern을 연결하고 재생 상태를 초기화합니다.
        /// </summary>
        public void Initialize(ProjectileEmitter emitter, ProjectilePatternProfile pattern)
        {
            projectileEmitter = emitter;
            patternProfile = pattern;
            Stop();
        }



        /// <summary>
        /// pattern을 첫 step부터 다시 시작합니다. emitter나 step이 없으면 false를 반환합니다.
        /// </summary>
        public bool Restart()
        {
            return Restart(null);
        }



        /// <summary>
        /// pattern을 첫 step부터 다시 시작하고, step 진입 즉시 burst가 있으면 성공한 projectile id를 buffer에 채웁니다.
        /// </summary>
        public bool Restart(int[] immediateProjectileIds)
        {
            Stop();
            EnsureEmitter();
            if (!CanPlay())
            {
                ClearImmediateProjectileIds(immediateProjectileIds);
                return false;
            }

            currentStepIndex = 0;
            elapsedInStep = 0f;
            isPlaying = true;
            isCompleted = false;
            loopCount = 0;
            EnterCurrentStep(immediateProjectileIds);
            return true;
        }



        /// <summary>현재 재생 상태를 멈추고 step 진행 상태를 초기화합니다.</summary>
        public void Stop()
        {
            currentStepIndex = -1;
            elapsedInStep = 0f;
            isPlaying = false;
            isCompleted = false;
            lastEnterSpawnedCount = 0;
        }



        /// <summary>
        /// 지정 step으로 즉시 진입합니다. play가 true이면 진입 이후 자동 진행 상태로 둡니다.
        /// </summary>
        public bool TryEnterStep(int stepIndex, bool play)
        {
            return TryEnterStep(stepIndex, play, null);
        }



        /// <summary>
        /// 지정 step으로 즉시 진입하고, step 진입 즉시 burst가 있으면 성공한 projectile id를 buffer에 채웁니다.
        /// </summary>
        public bool TryEnterStep(int stepIndex, bool play, int[] immediateProjectileIds)
        {
            EnsureEmitter();
            if (!CanPlay() || !patternProfile.TryGetStep(stepIndex, out _))
            {
                lastEnterSpawnedCount = 0;
                ClearImmediateProjectileIds(immediateProjectileIds);
                return false;
            }

            currentStepIndex = stepIndex;
            elapsedInStep = 0f;
            isPlaying = play;
            isCompleted = false;
            EnterCurrentStep(immediateProjectileIds);
            return true;
        }



        /// <summary>
        /// 다음 step으로 진행합니다. 현재 step이 없으면 처음부터 재시작합니다.
        /// </summary>
        public bool AdvanceToNextStep()
        {
            return AdvanceToNextStep(null);
        }



        /// <summary>
        /// 다음 step으로 진행하고, step 진입 즉시 burst가 있으면 성공한 projectile id를 buffer에 채웁니다.
        /// </summary>
        public bool AdvanceToNextStep(int[] immediateProjectileIds)
        {
            EnsureEmitter();
            if (!CanPlay())
            {
                lastEnterSpawnedCount = 0;
                ClearImmediateProjectileIds(immediateProjectileIds);
                return false;
            }

            if (currentStepIndex < 0)
            {
                return Restart(immediateProjectileIds);
            }

            int nextStepIndex = currentStepIndex + 1;
            return TryEnterResolvedNextStep(nextStepIndex, immediateProjectileIds);
        }



        /// <summary>
        /// deltaTime만큼 pattern과 emitter를 수동 갱신하고, 이번 tick에서 생성된 투사체 수를 반환합니다.
        /// </summary>
        public int Tick(float deltaTime)
        {
            if (!isPlaying || deltaTime <= 0f)
            {
                return 0;
            }

            EnsureEmitter();
            if (projectileEmitter == null)
            {
                Stop();
                return 0;
            }

            int spawned = 0;
            int transitionCount = 0;
            float remainingTime = deltaTime;

            while (isPlaying && remainingTime > 0f)
            {
                if (!patternProfile.TryGetStep(currentStepIndex, out ProjectilePatternStep step))
                {
                    Stop();
                    isCompleted = true;
                    break;
                }

                if (step.Duration <= 0f)
                {
                    spawned += projectileEmitter.Tick(remainingTime);
                    elapsedInStep += remainingTime;
                    break;
                }

                float remainingStepTime = step.Duration - elapsedInStep;
                if (remainingStepTime <= 0f)
                {
                    if (!TryAdvanceToNextStepDuringTick(ref transitionCount))
                    {
                        break;
                    }

                    continue;
                }

                float tickSlice = Mathf.Min(remainingTime, remainingStepTime);
                spawned += projectileEmitter.Tick(tickSlice);
                elapsedInStep += tickSlice;
                remainingTime -= tickSlice;

                if (elapsedInStep >= step.Duration && !TryAdvanceToNextStepDuringTick(ref transitionCount))
                {
                    break;
                }
            }

            return spawned;
        }



        private bool CanPlay()
        {
            return projectileEmitter != null && patternProfile != null && patternProfile.StepCount > 0;
        }



        private void EnterCurrentStep(int[] immediateProjectileIds = null)
        {
            lastEnterSpawnedCount = 0;
            ClearImmediateProjectileIds(immediateProjectileIds);
            if (!patternProfile.TryGetStep(currentStepIndex, out ProjectilePatternStep step))
            {
                Stop();
                isCompleted = true;
                return;
            }

            if (step.EmitterProfile != null)
            {
                projectileEmitter.ApplyProfile(step.EmitterProfile);
            }

            if (step.OverrideVisualFrameIndex)
            {
                projectileEmitter.VisualFrameIndex = step.VisualFrameIndex;
            }

            if (step.ClearProjectilesOnEnter)
            {
                projectileEmitter.ClearProjectiles();
            }

            if (step.ResetEmissionStateOnEnter)
            {
                projectileEmitter.ResetEmissionState();
            }

            if (step.FireBurstOnEnter)
            {
                lastEnterSpawnedCount = projectileEmitter.SpawnBurst(transform.position, step.FireAngleOffsetDegrees, immediateProjectileIds);
            }
        }



        private bool TryEnterResolvedNextStep(int nextStepIndex, int[] immediateProjectileIds = null)
        {
            if (nextStepIndex >= patternProfile.StepCount)
            {
                if (!patternProfile.Loop)
                {
                    isPlaying = false;
                    isCompleted = true;
                    lastEnterSpawnedCount = 0;
                    ClearImmediateProjectileIds(immediateProjectileIds);
                    return false;
                }

                nextStepIndex = 0;
                loopCount++;
            }

            currentStepIndex = nextStepIndex;
            elapsedInStep = 0f;
            EnterCurrentStep(immediateProjectileIds);
            return true;
        }



        private bool TryAdvanceToNextStepDuringTick(ref int transitionCount)
        {
            if (transitionCount >= MaxStepTransitionsPerTick)
            {
                return false;
            }

            transitionCount++;
            return TryEnterResolvedNextStep(currentStepIndex + 1);
        }



        private static void ClearImmediateProjectileIds(int[] immediateProjectileIds)
        {
            ProjectileSpawnUtility.ClearProjectileIds(
            immediateProjectileIds,
            immediateProjectileIds != null ? immediateProjectileIds.Length : 0);
        }



        private void EnsureEmitter()
        {
            if (projectileEmitter == null)
            {
                projectileEmitter = GetComponent<ProjectileEmitter>();
            }
        }
    }
}
