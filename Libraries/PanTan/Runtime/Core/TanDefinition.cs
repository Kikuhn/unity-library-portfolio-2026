using System;
using Pan.HighDensityElement;
using UnityEngine;



namespace Pan.Tan
{
    /// <summary>
    /// backend 종류를 진단 화면과 로그에 표시하기 위한 값입니다.
    /// spawn backend 선택은 호출자가 보유한 <see cref="ITanRuntimeBackend"/> instance로 결정합니다.
    /// </summary>
    public enum TanBackendKind : byte
    {
        Unknown = 0,
        Normal = 1,
        ElementQuery = 2,

        //. 값 3은 제거된 계약에 사용했으므로 이후 release에서도 재사용하지 않습니다.
    }



    /// <summary>
    /// 선택적으로 존재하는 float 값을 0과 구분해 전달합니다.
    /// </summary>
    [Serializable]
    public readonly struct OptionalTanFloat
    {
        public OptionalTanFloat(float value)
        {
            Enabled = true;
            Value = Mathf.Max(0f, value);
        }



        public bool Enabled { get; }
        public float Value { get; }
        public static OptionalTanFloat Disabled => default;
    }



    /// <summary>
    /// Tan의 속도에 적용할 범용 2D 운동 규칙입니다.
    /// </summary>
    [Serializable]
    public readonly struct TanMotionDefinition
    {
        public TanMotionDefinition(Vector2 acceleration, float linearDamping = 0f)
        {
            Acceleration = acceleration;
            LinearDamping = Mathf.Max(0f, linearDamping);
        }



        public Vector2 Acceleration { get; }
        public float LinearDamping { get; }
        public static TanMotionDefinition None => default;
    }



    /// <summary>
    /// Tan 하나에 적용할 추가 고속 충돌 보강 정책을 지정합니다.
    /// 기본 물리 충돌 검사를 끄지 않고 엄격 sweep 후보 판정만 제어합니다.
    /// </summary>
    public enum TanCcdMode : byte
    {
        /// <summary>
        /// runtime이 제공하던 기존 엄격 CCD 기본값을 그대로 사용합니다.
        /// </summary>
        RuntimeDefault = 0,

        /// <summary>
        /// 이동 거리와 유효 지름의 비율에 따라 엄격 경로를 자동 선택합니다.
        /// </summary>
        Auto = 1,

        /// <summary>
        /// 지원 형상에서 추가 엄격 CCD 후보 판정을 항상 사용합니다.
        /// </summary>
        Always = 2,

        /// <summary>
        /// 기본 충돌 검사는 유지하면서 추가 엄격 경로만 사용하지 않습니다.
        /// </summary>
        Never = 3
    }



    /// <summary>
    /// 이동 거리와 유효 지름을 비교해 엄격 CCD 사용 여부를 결정하는 불변 설정입니다.
    /// RuntimeDefault는 backend 생성 시 전달된 기존 기본값을 그대로 사용합니다.
    /// </summary>
    [Serializable]
    public readonly struct TanCcdPolicy
    {
        /// <summary>
        /// 지정한 모드와 자동 판정 비율로 불변 CCD 정책을 만듭니다.
        /// </summary>
        public TanCcdPolicy(TanCcdMode mode, float motionToDiameterRatio = 0.5f)
        {
            Mode = mode;
            MotionToDiameterRatio = float.IsFinite(motionToDiameterRatio) && motionToDiameterRatio > 0f
                ? motionToDiameterRatio
                : 0.5f;
        }



        /// <summary>
        /// backend가 적용할 엄격 CCD 후보 판정 방식입니다.
        /// </summary>
        public TanCcdMode Mode { get; }

        /// <summary>
        /// 자동 판정에서 한 스텝 이동 거리와 비교할 유효 지름 배율입니다.
        /// </summary>
        public float MotionToDiameterRatio { get; }

        /// <summary>
        /// runtime 생성 시 전달한 기존 backend 기본값을 사용합니다.
        /// </summary>
        public static TanCcdPolicy RuntimeDefault => default;

        /// <summary>
        /// 이동 거리와 유효 지름의 비율에 따라 엄격 CCD를 자동 선택합니다.
        /// </summary>
        public static TanCcdPolicy Auto => new TanCcdPolicy(TanCcdMode.Auto);

        /// <summary>
        /// 기본 충돌 검사에 더해 엄격 CCD 후보 판정을 항상 활성화합니다.
        /// </summary>
        public static TanCcdPolicy Always => new TanCcdPolicy(TanCcdMode.Always);

        /// <summary>
        /// 기본 충돌 검사는 유지하면서 추가 엄격 CCD 후보 판정만 비활성화합니다.
        /// </summary>
        public static TanCcdPolicy Never => new TanCcdPolicy(TanCcdMode.Never);
    }



    /// <summary>
    /// NormalTan과 ElementTan이 함께 해석하는 선택형 수명 정의입니다.
    /// </summary>
    [Serializable]
    public readonly struct TanLifetimeDefinition
    {
        public TanLifetimeDefinition(
            float remainingUnits,
            ElementUpdateSchedule schedule = default,
            bool useLocalClock = false)
        {
            Enabled = true;
            RemainingUnits = Mathf.Max(0f, remainingUnits);
            Schedule = schedule;
            UseLocalClock = useLocalClock;
        }

        public TanLifetimeDefinition(OptionalTanFloat lifetime)
        {
            Enabled = lifetime.Enabled;
            RemainingUnits = lifetime.Value;
            Schedule = ElementUpdateSchedule.UpdateSeconds();
            UseLocalClock = false;
        }



        public bool Enabled { get; }
        public float RemainingUnits { get; }
        public float Value => RemainingUnits;
        public ElementUpdateSchedule Schedule { get; }
        public bool UseLocalClock { get; }
        public static TanLifetimeDefinition Disabled => default;
    }



    /// <summary>
    /// Legacy Physics2D와 PhysicsCore2D backend가 공유하는 충돌 필터입니다.
    /// </summary>
    [Serializable]
    public readonly struct TanCollisionDefinition
    {
        public TanCollisionDefinition(
            int objectLayer,
            LayerMask hitLayers,
            bool includeTriggers = true)
        {
            ObjectLayer = Mathf.Clamp(objectLayer, 0, 31);
            HitLayers = hitLayers;
            IncludeTriggers = includeTriggers;
        }



        public int ObjectLayer { get; }
        public LayerMask HitLayers { get; }
        public bool IncludeTriggers { get; }
    }



    /// <summary>
    /// NormalTan과 ElementTan backend가 공유하는 불변 생성 정의입니다.
    /// </summary>
    [Serializable]
    public readonly struct TanDefinition
    {
        public TanDefinition(
            float radius,
            in TanCollisionDefinition collision,
            int teamId = 0,
            OptionalTanFloat lifetime = default,
            TanMotionDefinition motion = default,
            int visualId = 0,
            Vector2 visualScale = default,
            Color32 color = default,
            bool queryableByElementPhysics = false,
            TanCcdPolicy ccdPolicy = default,
            float rotationDegrees = 0f,
            TanVisualOrientationDefinition visualOrientation = default,
            TanPrimaryMotionDefinition primaryMotion = default,
            TanWaveMotionDefinition waveMotion = default,
            TanBoundaryDefinition boundary = default)
        {
            Radius = Mathf.Max(0.0001f, radius);
            Collision = collision;
            TeamId = Mathf.Max(0, teamId);
            Lifetime = new TanLifetimeDefinition(lifetime);
            Motion = motion;
            VisualId = Mathf.Max(0, visualId);
            VisualScale = visualScale == default ? Vector2.one : visualScale;
            Color = color.Equals(default(Color32)) ? new Color32(255, 255, 255, 255) : color;
            QueryableByElementPhysics = queryableByElementPhysics;
            CcdPolicy = ccdPolicy;
            RotationDegrees = float.IsFinite(rotationDegrees) ? rotationDegrees : 0f;
            VisualOrientation = visualOrientation;
            PrimaryMotion = primaryMotion;
            WaveMotion = waveMotion;
            Boundary = boundary;
        }

        public TanDefinition(
            float radius,
            in TanCollisionDefinition collision,
            in TanLifetimeDefinition lifetime,
            int teamId = 0,
            TanMotionDefinition motion = default,
            int visualId = 0,
            Vector2 visualScale = default,
            Color32 color = default,
            bool queryableByElementPhysics = false,
            TanCcdPolicy ccdPolicy = default,
            float rotationDegrees = 0f,
            TanVisualOrientationDefinition visualOrientation = default,
            TanPrimaryMotionDefinition primaryMotion = default,
            TanWaveMotionDefinition waveMotion = default,
            TanBoundaryDefinition boundary = default)
        {
            Radius = Mathf.Max(0.0001f, radius);
            Collision = collision;
            TeamId = Mathf.Max(0, teamId);
            Lifetime = lifetime;
            Motion = motion;
            VisualId = Mathf.Max(0, visualId);
            VisualScale = visualScale == default ? Vector2.one : visualScale;
            Color = color.Equals(default(Color32)) ? new Color32(255, 255, 255, 255) : color;
            QueryableByElementPhysics = queryableByElementPhysics;
            CcdPolicy = ccdPolicy;
            RotationDegrees = float.IsFinite(rotationDegrees) ? rotationDegrees : 0f;
            VisualOrientation = visualOrientation;
            PrimaryMotion = primaryMotion;
            WaveMotion = waveMotion;
            Boundary = boundary;
        }



        public float Radius { get; }
        public TanCollisionDefinition Collision { get; }
        public int TeamId { get; }
        public TanLifetimeDefinition Lifetime { get; }
        public TanMotionDefinition Motion { get; }
        public int VisualId { get; }
        public Vector2 VisualScale { get; }
        public Color32 Color { get; }
        public bool QueryableByElementPhysics { get; }

        /// <summary>
        /// 이 Tan에 적용할 backend 중립 고속 충돌 보강 정책입니다.
        /// </summary>
        public TanCcdPolicy CcdPolicy { get; }

        /// <summary>
        /// 생성 시 적용할 Tan 그래픽의 초기 월드 회전입니다.
        /// </summary>
        public float RotationDegrees { get; }

        /// <summary>
        /// 이동 방향 정렬과 회전 애니메이션을 위한 선택형 표시 규칙입니다.
        /// </summary>
        public TanVisualOrientationDefinition VisualOrientation { get; }

        /// <summary>
        /// 직선 이외의 선택형 주 이동 규칙입니다.
        /// </summary>
        public TanPrimaryMotionDefinition PrimaryMotion { get; }

        /// <summary>
        /// 진행 방향에 합성할 선택형 사인파 이동 규칙입니다.
        /// </summary>
        public TanWaveMotionDefinition WaveMotion { get; }

        /// <summary>
        /// World 또는 Camera 영역을 벗어난 Tan을 정리하는 선택형 규칙입니다.
        /// </summary>
        public TanBoundaryDefinition Boundary { get; }
    }



    /// <summary>
    /// backend에 한 발의 Tan 생성을 요청하는 값 형식 입력입니다.
    /// </summary>
    [Serializable]
    public readonly struct TanSpawnRequest
    {
        public TanSpawnRequest(
            in TanDefinition definition,
            Vector2 position,
            Vector2 velocity,
            TanTargetHandle source = default)
        {
            Definition = definition;
            Position = position;
            Velocity = velocity;
            Source = source;
        }



        public TanDefinition Definition { get; }
        public Vector2 Position { get; }
        public Vector2 Velocity { get; }
        public TanTargetHandle Source { get; }
    }
}
