using Unity.Mathematics;



namespace Pan.HighDensityElement
{
    /// <summary>
    /// 선택형 방향 이동 Feature를 다음 안전한 command 경계에서 추가하거나 교체합니다.
    /// </summary>
    public readonly struct ConfigureElementDirectionalMotion2D : IElementCommand
    {
        public ConfigureElementDirectionalMotion2D(in ElementDirectionalMotionFeature feature) => Feature = feature;

        public ElementDirectionalMotionFeature Feature { get; }

        public ElementCommand ToElementCommand(ElementKey target) => new ElementCommand(
            target,
            ElementCommandType.ConfigureDirectionalMotion2D,
            new float2(Feature.AngularSpeedRadiansPerSecond, Feature.MaxTurnRadiansPerSecond),
            Feature.HomingPoint,
            (int)Feature.Mode);
    }



    /// <summary>
    /// 선택형 방향 이동 Feature를 다음 안전한 command 경계에서 제거합니다.
    /// </summary>
    public readonly struct RemoveElementDirectionalMotion2D : IElementCommand
    {
        public ElementCommand ToElementCommand(ElementKey target) =>
            new ElementCommand(target, ElementCommandType.RemoveDirectionalMotion2D, default);
    }



    /// <summary>
    /// 선택형 사인파 이동 Feature를 다음 안전한 command 경계에서 추가하거나 교체합니다.
    /// </summary>
    public readonly struct ConfigureElementWaveMotion2D : IElementCommand
    {
        public ConfigureElementWaveMotion2D(in ElementWaveMotionFeature feature) => Feature = feature;

        public ElementWaveMotionFeature Feature { get; }

        public ElementCommand ToElementCommand(ElementKey target) => new ElementCommand(
            target,
            ElementCommandType.ConfigureWaveMotion2D,
            new float2(Feature.Amplitude, Feature.AngularFrequencyRadiansPerSecond),
            new float2(Feature.PhaseRadians, 0f));
    }



    /// <summary>
    /// 선택형 사인파 이동 Feature를 다음 안전한 command 경계에서 제거합니다.
    /// </summary>
    public readonly struct RemoveElementWaveMotion2D : IElementCommand
    {
        public ElementCommand ToElementCommand(ElementKey target) =>
            new ElementCommand(target, ElementCommandType.RemoveWaveMotion2D, default);
    }



    /// <summary>
    /// 선택형 표시 회전 Feature를 다음 안전한 command 경계에서 추가하거나 교체합니다.
    /// </summary>
    public readonly struct ConfigureElementVisualOrientation2D : IElementCommand
    {
        public ConfigureElementVisualOrientation2D(in ElementVisualOrientationFeature feature) => Feature = feature;

        public ElementVisualOrientationFeature Feature { get; }

        public ElementCommand ToElementCommand(ElementKey target) => new ElementCommand(
            target,
            ElementCommandType.ConfigureVisualOrientation2D,
            new float2(Feature.AxisOffsetRadians, Feature.SpinRadiansPerSecond),
            default,
            (int)Feature.Mode);
    }



    /// <summary>
    /// 선택형 표시 회전 Feature를 다음 안전한 command 경계에서 제거합니다.
    /// </summary>
    public readonly struct RemoveElementVisualOrientation2D : IElementCommand
    {
        public ElementCommand ToElementCommand(ElementKey target) =>
            new ElementCommand(target, ElementCommandType.RemoveVisualOrientation2D, default);
    }



    /// <summary>
    /// 선택형 공유 경계 Feature를 다음 안전한 command 경계에서 추가하거나 교체합니다.
    /// </summary>
    public readonly struct ConfigureElementBoundary2D : IElementCommand
    {
        public ConfigureElementBoundary2D(in ElementBoundaryFeature feature) => Feature = feature;

        public ElementBoundaryFeature Feature { get; }

        public ElementCommand ToElementCommand(ElementKey target) => new ElementCommand(
            target,
            ElementCommandType.ConfigureBoundary2D,
            new float2(Feature.Margin, 0f),
            default,
            (int)Feature.Mode,
            Feature.RequireEnteredBeforeExit ? 1 : 0);
    }



    /// <summary>
    /// 선택형 공유 경계 Feature를 다음 안전한 command 경계에서 제거합니다.
    /// </summary>
    public readonly struct RemoveElementBoundary2D : IElementCommand
    {
        public ElementCommand ToElementCommand(ElementKey target) =>
            new ElementCommand(target, ElementCommandType.RemoveBoundary2D, default);
    }
}
