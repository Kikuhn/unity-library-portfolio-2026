using System;
using Unity.Mathematics;



namespace Pan.HighDensityElement
{
    /// <summary>
    /// 모든 Element가 반드시 보유하는 최소 2D pose입니다.
    /// </summary>
    [Serializable]
    public readonly struct ElementPose2D : IEquatable<ElementPose2D>
    {
        public ElementPose2D(float2 position, float2 scale)
            : this(position, position, 0f, 0f, scale, scale)
        {
        }



        public ElementPose2D(float2 previousPosition, float2 position, float2 scale)
            : this(previousPosition, position, 0f, 0f, scale, scale)
        {
        }



        public ElementPose2D(
            float2 previousPosition,
            float2 position,
            float previousRotationRadians,
            float rotationRadians,
            float2 previousScale,
            float2 scale)
        {
            PreviousPosition = previousPosition;
            Position = position;
            PreviousRotationRadians = previousRotationRadians;
            RotationRadians = rotationRadians;
            PreviousScale = previousScale;
            Scale = scale;
        }



        public float2 PreviousPosition { get; }
        public float2 Position { get; }
        public float PreviousRotationRadians { get; }
        public float RotationRadians { get; }
        public float2 PreviousScale { get; }
        public float2 Scale { get; }



        public bool Equals(ElementPose2D other) =>
            PreviousPosition.Equals(other.PreviousPosition) &&
            Position.Equals(other.Position) &&
            PreviousRotationRadians.Equals(other.PreviousRotationRadians) &&
            RotationRadians.Equals(other.RotationRadians) &&
            PreviousScale.Equals(other.PreviousScale) &&
            Scale.Equals(other.Scale);



        public override bool Equals(object obj) => obj is ElementPose2D other && Equals(other);
        public override int GetHashCode()
        {
            int first = HashCode.Combine(PreviousPosition, Position, PreviousRotationRadians);
            int second = HashCode.Combine(RotationRadians, PreviousScale, Scale);
            return HashCode.Combine(first, second);
        }
    }



    /// <summary>
    /// Feature 시간이 Unity time scale의 영향을 받는지 지정합니다.
    /// </summary>
    public enum ElementTimeDomain : byte
    {
        Scaled = 0,
        Unscaled = 1
    }



    /// <summary>
    /// Feature update 주기를 초 또는 프레임 단위로 지정합니다.
    /// </summary>
    public enum ElementUpdateUnit : byte
    {
        Seconds = 0,
        Frames = 1
    }



    /// <summary>
    /// Feature가 갱신될 시간 영역과 cadence를 나타냅니다.
    /// </summary>
    [Serializable]
    public readonly struct ElementUpdateSchedule : IEquatable<ElementUpdateSchedule>
    {
        private ElementUpdateSchedule(
            ElementUpdateUnit unit,
            ElementTimeDomain timeDomain,
            float intervalUnits)
        {
            Unit = unit;
            TimeDomain = timeDomain;
            IntervalUnits = intervalUnits;
        }



        public ElementUpdateUnit Unit { get; }
        public ElementTimeDomain TimeDomain { get; }
        public float IntervalUnits { get; }



        /// <summary>
        /// 선택한 시간 영역의 초 단위로 Feature를 갱신합니다. 0은 매 호출의 delta를 그대로 사용합니다.
        /// </summary>
        public static ElementUpdateSchedule UpdateSeconds(
            float intervalSeconds = 0f,
            ElementTimeDomain timeDomain = ElementTimeDomain.Scaled)
        {
            if (!math.isfinite(intervalSeconds) || intervalSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(intervalSeconds));
            }

            return new ElementUpdateSchedule(ElementUpdateUnit.Seconds, timeDomain, intervalSeconds);
        }



        /// <summary>
        /// 선택한 시간 영역의 프레임 단위로 Feature를 갱신합니다.
        /// </summary>
        public static ElementUpdateSchedule UpdateFrames(
            int intervalFrames = 1,
            ElementTimeDomain timeDomain = ElementTimeDomain.Scaled)
        {
            if (intervalFrames < 1) { throw new ArgumentOutOfRangeException(nameof(intervalFrames)); }
            return new ElementUpdateSchedule(ElementUpdateUnit.Frames, timeDomain, intervalFrames);
        }



        public bool Equals(ElementUpdateSchedule other) =>
            Unit == other.Unit && TimeDomain == other.TimeDomain && IntervalUnits.Equals(other.IntervalUnits);



        public override bool Equals(object obj) => obj is ElementUpdateSchedule other && Equals(other);
        public override int GetHashCode() => HashCode.Combine((byte)Unit, (byte)TimeDomain, IntervalUnits);
    }



    /// <summary>
    /// 한 Element의 선택형 로컬 시간 배율과 일시 정지 상태입니다.
    /// </summary>
    [Serializable]
    public readonly struct ElementLocalClock : IEquatable<ElementLocalClock>
    {
        public ElementLocalClock(float timeScale = 1f, bool paused = false)
        {
            if (!math.isfinite(timeScale) || timeScale < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(timeScale));
            }

            TimeScale = timeScale;
            Paused = paused;
        }



        public float TimeScale { get; }
        public bool Paused { get; }



        public bool Equals(ElementLocalClock other) =>
            TimeScale.Equals(other.TimeScale) && Paused == other.Paused;



        public override bool Equals(object obj) => obj is ElementLocalClock other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(TimeScale, Paused);
    }



    /// <summary>
    /// Physics 고정 Tick과 분리된 Feature update 입력입니다.
    /// </summary>
    public readonly struct ElementUpdateContext
    {
        public ElementUpdateContext(
            float scaledDeltaSeconds,
            float unscaledDeltaSeconds,
            float scaledFrameUnits = -1f,
            float unscaledFrameUnits = -1f)
        {
            ScaledDeltaSeconds = SanitizeDelta(scaledDeltaSeconds, nameof(scaledDeltaSeconds));
            UnscaledDeltaSeconds = SanitizeDelta(unscaledDeltaSeconds, nameof(unscaledDeltaSeconds));
            ScaledFrameUnits = scaledFrameUnits < 0f
                ? (ScaledDeltaSeconds > 0f ? 1f : 0f)
                : SanitizeDelta(scaledFrameUnits, nameof(scaledFrameUnits));
            UnscaledFrameUnits = unscaledFrameUnits < 0f
                ? 1f
                : SanitizeDelta(unscaledFrameUnits, nameof(unscaledFrameUnits));
        }



        public float ScaledDeltaSeconds { get; }
        public float UnscaledDeltaSeconds { get; }
        public float ScaledFrameUnits { get; }
        public float UnscaledFrameUnits { get; }



        internal float GetDeltaUnits(in ElementUpdateSchedule schedule)
        {
            if (schedule.Unit == ElementUpdateUnit.Seconds)
            {
                return schedule.TimeDomain == ElementTimeDomain.Scaled
                    ? ScaledDeltaSeconds
                    : UnscaledDeltaSeconds;
            }

            return schedule.TimeDomain == ElementTimeDomain.Scaled
                ? ScaledFrameUnits
                : UnscaledFrameUnits;
        }



        private static float SanitizeDelta(float value, string parameterName)
        {
            if (!math.isfinite(value) || value < 0f) { throw new ArgumentOutOfRangeException(parameterName); }
            return value;
        }
    }



    /// <summary>
    /// Native sparse store가 원본인 범용 Element 수명 Feature입니다.
    /// </summary>
    [Serializable]
    public readonly struct ElementLifetimeFeature : IEquatable<ElementLifetimeFeature>
    {
        public ElementLifetimeFeature(
            float remainingUnits,
            ElementUpdateSchedule schedule,
            bool useLocalClock = false)
        {
            if (!math.isfinite(remainingUnits) || remainingUnits < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(remainingUnits));
            }

            RemainingUnits = remainingUnits;
            Schedule = schedule;
            UseLocalClock = useLocalClock;
        }



        public float RemainingUnits { get; }
        public ElementUpdateSchedule Schedule { get; }
        public bool UseLocalClock { get; }



        internal ElementLifetimeFeature WithRemainingUnits(float remainingUnits) =>
            new ElementLifetimeFeature(math.max(0f, remainingUnits), Schedule, UseLocalClock);



        public bool Equals(ElementLifetimeFeature other) =>
            RemainingUnits.Equals(other.RemainingUnits) &&
            Schedule.Equals(other.Schedule) &&
            UseLocalClock == other.UseLocalClock;



        public override bool Equals(object obj) => obj is ElementLifetimeFeature other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(RemainingUnits, Schedule, UseLocalClock);
    }
}
