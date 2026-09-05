using System;
using UnityEngine;



namespace Pan.Tan
{
    /// <summary>
    /// Tan 그래픽의 자동 회전 기준을 지정합니다.
    /// </summary>
    public enum TanFacingMode : byte
    {
        None = 0,
        Velocity = 1,
        Manual = 2
    }



    /// <summary>
    /// Tan의 기본 진행 방향을 계산하는 주 이동 방식을 지정합니다.
    /// </summary>
    public enum TanPrimaryMotionMode : byte
    {
        Straight = 0,
        AngularTurn = 1,
        Homing = 2
    }



    /// <summary>
    /// Homing이 추적할 목표의 표현 방식을 지정합니다.
    /// </summary>
    public enum TanHomingTargetMode : byte
    {
        None = 0,
        Target = 1,
        FixedPoint = 2
    }



    /// <summary>
    /// Homing 대상의 현재 위치를 더 이상 복원할 수 없을 때의 동작을 지정합니다.
    /// </summary>
    public enum TanHomingTargetLossPolicy : byte
    {
        ContinueCurrentDirection = 0,
        TrackLastPosition = 1,
        Despawn = 2
    }



    /// <summary>
    /// Tan을 유효 영역 밖에서 정리하는 경계 의미를 지정합니다.
    /// 실제 영역은 consumer가 World 또는 Camera로부터 계산해 전달합니다.
    /// </summary>
    public enum TanBoundaryMode : byte
    {
        None = 0,
        WorldBounds = 1,
        CameraViewport = 2
    }



    /// <summary>
    /// 이동 방향 정렬과 회전 애니메이션을 결합한 선택형 표시 규칙입니다.
    /// default 값은 비활성 상태이므로 사용하지 않는 Tan은 희소 상태를 할당하지 않습니다.
    /// </summary>
    [Serializable]
    public readonly struct TanVisualOrientationDefinition
    {
        /// <summary>
        /// 자동 정렬 방식, Sprite 축 보정, 초당 spin으로 표시 규칙을 만듭니다.
        /// </summary>
        public TanVisualOrientationDefinition(
            TanFacingMode facingMode,
            float axisOffsetDegrees = 0f,
            float spinDegreesPerSecond = 0f)
        {
            Enabled = true;
            FacingMode = facingMode is TanFacingMode.None or TanFacingMode.Velocity or TanFacingMode.Manual
                ? facingMode
                : TanFacingMode.None;
            AxisOffsetDegrees = SanitizeFinite(axisOffsetDegrees);
            SpinDegreesPerSecond = SanitizeFinite(spinDegreesPerSecond);
        }



        /// <summary>
        /// 실제 희소 표시 방향 상태가 필요한지 나타냅니다.
        /// </summary>
        public bool Enabled { get; }

        /// <summary>
        /// 그래픽의 자동 회전 기준입니다.
        /// </summary>
        public TanFacingMode FacingMode { get; }

        /// <summary>
        /// Sprite 원본의 전방 축을 보정하는 degree 각도입니다.
        /// </summary>
        public float AxisOffsetDegrees { get; }

        /// <summary>
        /// 자동 또는 수동 기준 회전에 더할 초당 degree 각속도입니다.
        /// </summary>
        public float SpinDegreesPerSecond { get; }

        /// <summary>
        /// 희소 표시 방향 상태를 만들지 않는 기본값입니다.
        /// </summary>
        public static TanVisualOrientationDefinition Disabled => default;



        private static float SanitizeFinite(float value) => float.IsFinite(value) ? value : 0f;
    }



    /// <summary>
    /// Homing 대상 identity 또는 고정 월드 좌표를 managed 참조 없이 전달합니다.
    /// Target identity의 실제 위치 복원은 consumer의 target registry가 담당합니다.
    /// </summary>
    [Serializable]
    public readonly struct TanHomingTarget
    {
        private TanHomingTarget(
            TanHomingTargetMode mode,
            in TanTargetHandle target,
            Vector2 fixedPoint)
        {
            Mode = mode;
            Target = target;
            bool pointValid = IsFinite(fixedPoint);
            FixedPoint = pointValid ? fixedPoint : Vector2.zero;
            IsValid = mode switch
            {
                TanHomingTargetMode.Target => target.IsValid && pointValid,
                TanHomingTargetMode.FixedPoint => pointValid,
                _ => false
            };
        }



        /// <summary>
        /// generation target 또는 고정점 중 사용 중인 표현입니다.
        /// </summary>
        public TanHomingTargetMode Mode { get; }

        /// <summary>
        /// Target 방식에서 추적할 generation-safe identity입니다.
        /// </summary>
        public TanTargetHandle Target { get; }

        /// <summary>
        /// 고정점 또는 Target의 마지막으로 복원된 월드 위치입니다.
        /// </summary>
        public Vector2 FixedPoint { get; }

        /// <summary>
        /// 선택한 방식에 필요한 identity 또는 좌표가 유효한지 나타냅니다.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// generation-safe 대상과 spawn 시 사용할 첫 복원 위치를 함께 보관합니다.
        /// 이후 위치는 consumer가 fixed 경계 전에 갱신합니다.
        /// </summary>
        public static TanHomingTarget ForTarget(in TanTargetHandle target, Vector2 initialPosition = default) =>
            new TanHomingTarget(TanHomingTargetMode.Target, in target, initialPosition);

        /// <summary>
        /// 고정 월드 좌표를 계속 추적하는 Homing 대상을 만듭니다.
        /// </summary>
        public static TanHomingTarget ForFixedPoint(Vector2 point) =>
            new TanHomingTarget(TanHomingTargetMode.FixedPoint, default, point);



        private static bool IsFinite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);
    }



    /// <summary>
    /// 직선, 각속도 회전 또는 목표 추적 중 하나를 선택하는 주 이동 정의입니다.
    /// Straight는 기존 가속도와 감쇠만 사용하며 희소 주 이동 상태를 요구하지 않습니다.
    /// </summary>
    [Serializable]
    public readonly struct TanPrimaryMotionDefinition
    {
        private TanPrimaryMotionDefinition(
            TanPrimaryMotionMode mode,
            float turnDegreesPerSecond,
            in TanHomingTarget homingTarget,
            TanHomingTargetLossPolicy targetLossPolicy)
        {
            Mode = mode;
            TurnDegreesPerSecond = float.IsFinite(turnDegreesPerSecond) ? turnDegreesPerSecond : 0f;
            HomingTarget = homingTarget;
            TargetLossPolicy = targetLossPolicy is
                TanHomingTargetLossPolicy.ContinueCurrentDirection or
                TanHomingTargetLossPolicy.TrackLastPosition or
                TanHomingTargetLossPolicy.Despawn
                ? targetLossPolicy
                : TanHomingTargetLossPolicy.ContinueCurrentDirection;
        }



        /// <summary>
        /// Tan의 주 이동 방식입니다.
        /// </summary>
        public TanPrimaryMotionMode Mode { get; }

        /// <summary>
        /// AngularTurn의 부호 있는 각속도 또는 Homing의 최대 회전 속도입니다.
        /// </summary>
        public float TurnDegreesPerSecond { get; }

        /// <summary>
        /// Homing 방식에서 사용할 목표 identity 또는 고정점입니다.
        /// </summary>
        public TanHomingTarget HomingTarget { get; }

        /// <summary>
        /// generation target을 잃었을 때 적용할 정책입니다.
        /// </summary>
        public TanHomingTargetLossPolicy TargetLossPolicy { get; }

        /// <summary>
        /// 직선 이외의 희소 주 이동 상태가 필요한지 나타냅니다.
        /// </summary>
        public bool Enabled => Mode != TanPrimaryMotionMode.Straight;

        /// <summary>
        /// 기존 가속도와 감쇠만 사용하는 기본 직선 이동입니다.
        /// </summary>
        public static TanPrimaryMotionDefinition Straight => default;

        /// <summary>
        /// 현재 속도 방향을 매초 지정한 degree만큼 회전시키는 이동을 만듭니다.
        /// </summary>
        public static TanPrimaryMotionDefinition AngularTurn(float degreesPerSecond) =>
            new TanPrimaryMotionDefinition(
                TanPrimaryMotionMode.AngularTurn,
                degreesPerSecond,
                default,
                TanHomingTargetLossPolicy.ContinueCurrentDirection);

        /// <summary>
        /// 현재 속도 크기를 유지하며 목표 방향으로 제한 회전하는 이동을 만듭니다.
        /// </summary>
        public static TanPrimaryMotionDefinition Homing(
            in TanHomingTarget target,
            float maximumTurnDegreesPerSecond,
            TanHomingTargetLossPolicy targetLossPolicy = TanHomingTargetLossPolicy.ContinueCurrentDirection) =>
            new TanPrimaryMotionDefinition(
                TanPrimaryMotionMode.Homing,
                float.IsFinite(maximumTurnDegreesPerSecond)
                    ? Mathf.Max(0f, maximumTurnDegreesPerSecond)
                    : 0f,
                in target,
                targetLossPolicy);
    }



    /// <summary>
    /// Tan의 진행 방향에 수직인 축으로 적용할 선택형 사인파 이동 정의입니다.
    /// frequency는 초당 주기 수이며 phase는 생성 시점의 시작 위상입니다.
    /// </summary>
    [Serializable]
    public readonly struct TanWaveMotionDefinition
    {
        /// <summary>
        /// 진폭, 초당 주기, 시작 위상으로 사인파 이동 규칙을 만듭니다.
        /// </summary>
        public TanWaveMotionDefinition(float amplitude, float frequencyHz, float phaseDegrees = 0f)
        {
            Enabled = true;
            Amplitude = float.IsFinite(amplitude) ? amplitude : 0f;
            FrequencyHz = float.IsFinite(frequencyHz) ? Mathf.Max(0f, frequencyHz) : 0f;
            PhaseDegrees = float.IsFinite(phaseDegrees) ? phaseDegrees : 0f;
        }



        /// <summary>
        /// 실제 희소 Wave 상태가 필요한지 나타냅니다.
        /// </summary>
        public bool Enabled { get; }

        /// <summary>
        /// 진행 방향의 수직축으로 이동할 최대 거리입니다.
        /// </summary>
        public float Amplitude { get; }

        /// <summary>
        /// 초당 반복할 사인파 주기 수입니다.
        /// </summary>
        public float FrequencyHz { get; }

        /// <summary>
        /// 생성 시 적용할 degree 시작 위상입니다.
        /// </summary>
        public float PhaseDegrees { get; }

        /// <summary>
        /// 희소 Wave 상태를 만들지 않는 기본값입니다.
        /// </summary>
        public static TanWaveMotionDefinition Disabled => default;
    }



    /// <summary>
    /// consumer가 계산한 월드 직사각형을 사용해 Tan 수명을 제한하는 선택형 정의입니다.
    /// CameraViewport에서 RequireEntered가 true이면 한 번 영역에 진입한 뒤 이탈한 Tan만 정리합니다.
    /// </summary>
    [Serializable]
    public readonly struct TanBoundaryDefinition
    {
        /// <summary>
        /// 경계 의미, 현재 영역, margin, 진입 조건으로 정리 규칙을 만듭니다.
        /// </summary>
        public TanBoundaryDefinition(
            TanBoundaryMode mode,
            Rect bounds,
            float margin = 0f,
            bool requireEntered = true)
        {
            Mode = mode is TanBoundaryMode.WorldBounds or TanBoundaryMode.CameraViewport
                ? mode
                : TanBoundaryMode.None;
            Bounds = Normalize(bounds);
            Margin = float.IsFinite(margin) ? Mathf.Max(0f, margin) : 0f;
            RequireEntered = Mode == TanBoundaryMode.CameraViewport && requireEntered;
        }



        /// <summary>
        /// World 또는 Camera 영역 의미입니다.
        /// </summary>
        public TanBoundaryMode Mode { get; }

        /// <summary>
        /// consumer가 현재 프레임에 복원한 월드 공간 직사각형입니다.
        /// </summary>
        public Rect Bounds { get; }

        /// <summary>
        /// 경계 판정 영역 바깥으로 확장할 거리입니다.
        /// </summary>
        public float Margin { get; }

        /// <summary>
        /// Camera 영역에 한 번 진입한 뒤 이탈했을 때만 정리할지 나타냅니다.
        /// </summary>
        public bool RequireEntered { get; }

        /// <summary>
        /// 실제 희소 경계 상태가 필요한지 나타냅니다.
        /// </summary>
        public bool Enabled => Mode != TanBoundaryMode.None;

        /// <summary>
        /// 희소 경계 상태를 만들지 않는 기본값입니다.
        /// </summary>
        public static TanBoundaryDefinition Disabled => default;



        private static Rect Normalize(Rect value)
        {
            if (!float.IsFinite(value.xMin) ||
                !float.IsFinite(value.yMin) ||
                !float.IsFinite(value.xMax) ||
                !float.IsFinite(value.yMax))
            {
                return default;
            }

            return Rect.MinMaxRect(
                Mathf.Min(value.xMin, value.xMax),
                Mathf.Min(value.yMin, value.yMax),
                Mathf.Max(value.xMin, value.xMax),
                Mathf.Max(value.yMin, value.yMax));
        }
    }



    /// <summary>
    /// backend 내부의 선택형 이동·표시 Feature가 실제로 할당되었는지 보여주는 복사본입니다.
    /// Native container나 backend 구현 세부 정보를 노출하지 않습니다.
    /// </summary>
    public readonly struct TanFeatureSnapshot
    {
        /// <summary>
        /// backend에서 실제 조회한 선택형 Feature 정의를 불변 복사본으로 만듭니다.
        /// </summary>
        public TanFeatureSnapshot(
            in TanVisualOrientationDefinition visualOrientation,
            in TanPrimaryMotionDefinition primaryMotion,
            in TanWaveMotionDefinition waveMotion,
            in TanBoundaryDefinition boundary)
        {
            VisualOrientation = visualOrientation;
            PrimaryMotion = primaryMotion;
            WaveMotion = waveMotion;
            Boundary = boundary;
        }



        /// <summary>
        /// 현재 할당된 표시 방향 Feature입니다.
        /// </summary>
        public TanVisualOrientationDefinition VisualOrientation { get; }

        /// <summary>
        /// 현재 할당된 주 이동 Feature입니다.
        /// </summary>
        public TanPrimaryMotionDefinition PrimaryMotion { get; }

        /// <summary>
        /// 현재 할당된 Wave Feature입니다.
        /// </summary>
        public TanWaveMotionDefinition WaveMotion { get; }

        /// <summary>
        /// 현재 할당된 경계 Feature입니다.
        /// </summary>
        public TanBoundaryDefinition Boundary { get; }

        /// <summary>
        /// 표시 방향 Feature가 실제 할당되었는지 나타냅니다.
        /// </summary>
        public bool HasVisualOrientation => VisualOrientation.Enabled;

        /// <summary>
        /// 주 이동 Feature가 실제 할당되었는지 나타냅니다.
        /// </summary>
        public bool HasPrimaryMotion => PrimaryMotion.Enabled;

        /// <summary>
        /// Wave Feature가 실제 할당되었는지 나타냅니다.
        /// </summary>
        public bool HasWaveMotion => WaveMotion.Enabled;

        /// <summary>
        /// 경계 Feature가 실제 할당되었는지 나타냅니다.
        /// </summary>
        public bool HasBoundary => Boundary.Enabled;
    }
}
