using System;
using Unity.Mathematics;



namespace Pan.HighDensityElement
{
    /// <summary>
    /// 선택형 방향 이동 Feature가 속도 방향을 갱신하는 방식을 지정합니다.
    /// </summary>
    public enum ElementDirectionalMotionMode : byte
    {
        Straight = 0,
        AngularTurn = 1,
        HomingPoint = 2
    }



    /// <summary>
    /// Sprite 표시 회전을 수동 각도 또는 이동 방향에서 계산할지 지정합니다.
    /// </summary>
    public enum ElementVisualOrientationMode : byte
    {
        Manual = 0,
        Velocity = 1
    }



    /// <summary>
    /// Element가 어느 공유 2D 경계에 의해 수명을 종료할지 지정합니다.
    /// </summary>
    public enum ElementBoundaryMode2D : byte
    {
        WorldBounds = 0,
        ViewBounds = 1
    }



    /// <summary>
    /// 정렬된 최소·최대 좌표로 구성된 유효한 2D 경계입니다.
    /// </summary>
    [Serializable]
    public readonly struct ElementBounds2D : IEquatable<ElementBounds2D>
    {
        public ElementBounds2D(float2 minimum, float2 maximum)
        {
            if (!math.all(math.isfinite(minimum)) || !math.all(math.isfinite(maximum)))
            {
                throw new ArgumentOutOfRangeException(nameof(minimum));
            }

            Minimum = math.min(minimum, maximum);
            Maximum = math.max(minimum, maximum);
        }



        public float2 Minimum { get; }
        public float2 Maximum { get; }



        public bool Contains(float2 point, float margin = 0f)
        {
            float safeMargin = math.max(0f, math.isfinite(margin) ? margin : 0f);
            return math.all(point >= Minimum - safeMargin) && math.all(point <= Maximum + safeMargin);
        }



        public bool Equals(ElementBounds2D other) =>
            Minimum.Equals(other.Minimum) && Maximum.Equals(other.Maximum);



        public override bool Equals(object obj) => obj is ElementBounds2D other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Minimum, Maximum);
    }



    /// <summary>
    /// 직선·각속도 회전·고정점 유도 중 하나를 선택적으로 적용하는 native sparse 이동 상태입니다.
    /// </summary>
    [Serializable]
    public readonly struct ElementDirectionalMotionFeature : IEquatable<ElementDirectionalMotionFeature>
    {
        public ElementDirectionalMotionFeature(
            ElementDirectionalMotionMode mode,
            float angularSpeedRadiansPerSecond = 0f,
            float2 homingPoint = default,
            float maxTurnRadiansPerSecond = 0f)
        {
            if (!Enum.IsDefined(typeof(ElementDirectionalMotionMode), mode) ||
                !math.isfinite(angularSpeedRadiansPerSecond) ||
                !math.all(math.isfinite(homingPoint)) ||
                !math.isfinite(maxTurnRadiansPerSecond) ||
                maxTurnRadiansPerSecond < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            Mode = mode;
            AngularSpeedRadiansPerSecond = angularSpeedRadiansPerSecond;
            HomingPoint = homingPoint;
            MaxTurnRadiansPerSecond = maxTurnRadiansPerSecond;
        }



        public ElementDirectionalMotionMode Mode { get; }
        public float AngularSpeedRadiansPerSecond { get; }
        public float2 HomingPoint { get; }
        public float MaxTurnRadiansPerSecond { get; }



        public bool Equals(ElementDirectionalMotionFeature other) =>
            Mode == other.Mode &&
            AngularSpeedRadiansPerSecond.Equals(other.AngularSpeedRadiansPerSecond) &&
            HomingPoint.Equals(other.HomingPoint) &&
            MaxTurnRadiansPerSecond.Equals(other.MaxTurnRadiansPerSecond);



        public override bool Equals(object obj) => obj is ElementDirectionalMotionFeature other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(
            (byte)Mode,
            AngularSpeedRadiansPerSecond,
            HomingPoint,
            MaxTurnRadiansPerSecond);
    }



    /// <summary>
    /// 진행 방향의 수직축으로 사인파 offset을 합성하는 native sparse 이동 상태입니다.
    /// </summary>
    [Serializable]
    public readonly struct ElementWaveMotionFeature : IEquatable<ElementWaveMotionFeature>
    {
        public ElementWaveMotionFeature(
            float amplitude,
            float angularFrequencyRadiansPerSecond,
            float phaseRadians = 0f)
        {
            if (!math.isfinite(amplitude) ||
                !math.isfinite(angularFrequencyRadiansPerSecond) ||
                !math.isfinite(phaseRadians))
            {
                throw new ArgumentOutOfRangeException(nameof(amplitude));
            }

            Amplitude = amplitude;
            AngularFrequencyRadiansPerSecond = angularFrequencyRadiansPerSecond;
            PhaseRadians = phaseRadians;
        }



        public float Amplitude { get; }
        public float AngularFrequencyRadiansPerSecond { get; }
        public float PhaseRadians { get; }



        public bool Equals(ElementWaveMotionFeature other) =>
            Amplitude.Equals(other.Amplitude) &&
            AngularFrequencyRadiansPerSecond.Equals(other.AngularFrequencyRadiansPerSecond) &&
            PhaseRadians.Equals(other.PhaseRadians);



        public override bool Equals(object obj) => obj is ElementWaveMotionFeature other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(
            Amplitude,
            AngularFrequencyRadiansPerSecond,
            PhaseRadians);
    }



    /// <summary>
    /// 표시 각도를 이동 방향 또는 수동 각도로 유지하고 선택형 spin을 합성하는 native sparse 상태입니다.
    /// </summary>
    [Serializable]
    public readonly struct ElementVisualOrientationFeature : IEquatable<ElementVisualOrientationFeature>
    {
        public ElementVisualOrientationFeature(
            ElementVisualOrientationMode mode,
            float axisOffsetRadians = 0f,
            float spinRadiansPerSecond = 0f)
        {
            if (!Enum.IsDefined(typeof(ElementVisualOrientationMode), mode) ||
                !math.isfinite(axisOffsetRadians) ||
                !math.isfinite(spinRadiansPerSecond))
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            Mode = mode;
            AxisOffsetRadians = axisOffsetRadians;
            SpinRadiansPerSecond = spinRadiansPerSecond;
        }



        public ElementVisualOrientationMode Mode { get; }
        public float AxisOffsetRadians { get; }
        public float SpinRadiansPerSecond { get; }



        public bool Equals(ElementVisualOrientationFeature other) =>
            Mode == other.Mode &&
            AxisOffsetRadians.Equals(other.AxisOffsetRadians) &&
            SpinRadiansPerSecond.Equals(other.SpinRadiansPerSecond);



        public override bool Equals(object obj) => obj is ElementVisualOrientationFeature other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(
            (byte)Mode,
            AxisOffsetRadians,
            SpinRadiansPerSecond);
    }



    /// <summary>
    /// 공유 World 또는 View 경계를 벗어났을 때 Element 수명을 종료하는 native sparse 상태입니다.
    /// </summary>
    [Serializable]
    public readonly struct ElementBoundaryFeature : IEquatable<ElementBoundaryFeature>
    {
        public ElementBoundaryFeature(
            ElementBoundaryMode2D mode,
            float margin = 0f,
            bool requireEnteredBeforeExit = false)
        {
            if (!Enum.IsDefined(typeof(ElementBoundaryMode2D), mode) || !math.isfinite(margin) || margin < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            Mode = mode;
            Margin = margin;
            RequireEnteredBeforeExit = requireEnteredBeforeExit;
        }



        public ElementBoundaryMode2D Mode { get; }
        public float Margin { get; }
        public bool RequireEnteredBeforeExit { get; }



        public bool Equals(ElementBoundaryFeature other) =>
            Mode == other.Mode &&
            Margin.Equals(other.Margin) &&
            RequireEnteredBeforeExit == other.RequireEnteredBeforeExit;



        public override bool Equals(object obj) => obj is ElementBoundaryFeature other && Equals(other);
        public override int GetHashCode() => HashCode.Combine((byte)Mode, Margin, RequireEnteredBeforeExit);
    }
}
