using Pan.HighDensityElement;
using Unity.Mathematics;
using UnityEngine;



namespace Pan.Tan.Element
{
    /// <summary>
    /// backend 중립 Tan Feature와 HDE native sparse Feature 사이의 변환을 담당합니다.
    /// </summary>
    public sealed partial class ElementTanRuntime
    {
        /// <summary>
        /// 실제 Element에 할당된 선택형 이동·표시 Feature를 backend 중립 값으로 복사합니다.
        /// </summary>
        public bool TryGetFeatureSnapshot(in TanKey key, out TanFeatureSnapshot snapshot)
        {
            if (!IsAlive(in key) || !records.TryGetValue(key, out TanRecord record))
            {
                snapshot = default;
                return false;
            }

            ElementKey elementKey = record.Element.Key;
            TanVisualOrientationDefinition orientation = world.TryGetVisualOrientation(
                elementKey,
                out ElementVisualOrientationFeature elementOrientation)
                ? ToTanVisualOrientation(in elementOrientation, in record.VisualOrientation)
                : default;
            TanPrimaryMotionDefinition primary = world.TryGetDirectionalMotion(
                elementKey,
                out ElementDirectionalMotionFeature elementPrimary)
                ? ToTanPrimaryMotion(in elementPrimary, in record.PrimaryMotion)
                : default;
            TanWaveMotionDefinition wave = world.TryGetWaveMotion(
                elementKey,
                out ElementWaveMotionFeature elementWave)
                ? ToTanWaveMotion(in elementWave)
                : default;
            TanBoundaryDefinition boundary = world.TryGetBoundary(
                elementKey,
                out ElementBoundaryFeature elementBoundary)
                ? ToTanBoundary(in elementBoundary)
                : default;

            snapshot = new TanFeatureSnapshot(in orientation, in primary, in wave, in boundary);
            return true;
        }



        /// <summary>
        /// consumer가 계산한 World 또는 Camera 공유 경계를 다음 simulation 전에 교체합니다.
        /// </summary>
        public bool TrySetBoundaryBounds2D(TanBoundaryMode mode, Rect bounds)
        {
            if (!IsAvailable || mode == TanBoundaryMode.None) { return false; }
            var definition = new TanBoundaryDefinition(mode, bounds);
            if (!definition.Enabled) { return false; }
            SetElementBoundaryBounds(in definition);
            return true;
        }



        private bool TryConfigureSpawnFeatures(ElementKey key, in TanDefinition definition)
        {
            if (definition.VisualOrientation.Enabled)
            {
                TanVisualOrientationDefinition tanOrientation = definition.VisualOrientation;
                ElementVisualOrientationFeature orientation = ToElementVisualOrientation(in tanOrientation);
                if (!world.TrySetVisualOrientation(key, in orientation)) { return false; }
            }

            if (definition.PrimaryMotion.Enabled)
            {
                TanPrimaryMotionDefinition tanPrimary = definition.PrimaryMotion;
                if (!TryCreateElementPrimaryMotion(
                        in tanPrimary,
                        out ElementDirectionalMotionFeature primary) ||
                    !world.TrySetDirectionalMotion(key, in primary))
                {
                    return false;
                }
            }

            if (definition.WaveMotion.Enabled)
            {
                TanWaveMotionDefinition tanWave = definition.WaveMotion;
                ElementWaveMotionFeature wave = ToElementWaveMotion(in tanWave);
                if (!world.TrySetWaveMotion(key, in wave)) { return false; }
            }

            if (definition.Boundary.Enabled)
            {
                TanBoundaryDefinition tanBoundary = definition.Boundary;
                ElementBoundaryFeature boundary = ToElementBoundary(in tanBoundary);
                SetElementBoundaryBounds(in tanBoundary);
                if (!world.TrySetBoundary(key, in boundary)) { return false; }
            }

            return true;
        }



        private static ElementVisualOrientationFeature ToElementVisualOrientation(
            in TanVisualOrientationDefinition definition)
        {
            ElementVisualOrientationMode mode = definition.FacingMode == TanFacingMode.Velocity
                ? ElementVisualOrientationMode.Velocity
                : ElementVisualOrientationMode.Manual;
            return new ElementVisualOrientationFeature(
                mode,
                math.radians(definition.AxisOffsetDegrees),
                math.radians(definition.SpinDegreesPerSecond));
        }



        private static bool TryCreateElementPrimaryMotion(
            in TanPrimaryMotionDefinition definition,
            out ElementDirectionalMotionFeature feature)
        {
            switch (definition.Mode)
            {
                case TanPrimaryMotionMode.AngularTurn:
                    feature = new ElementDirectionalMotionFeature(
                        ElementDirectionalMotionMode.AngularTurn,
                        math.radians(definition.TurnDegreesPerSecond));
                    return true;
                case TanPrimaryMotionMode.Homing when definition.HomingTarget.IsValid:
                    Vector2 point = definition.HomingTarget.FixedPoint;
                    feature = new ElementDirectionalMotionFeature(
                        ElementDirectionalMotionMode.HomingPoint,
                        homingPoint: new float2(point.x, point.y),
                        maxTurnRadiansPerSecond: math.radians(definition.TurnDegreesPerSecond));
                    return true;
                default:
                    feature = default;
                    return false;
            }
        }



        private static ElementWaveMotionFeature ToElementWaveMotion(in TanWaveMotionDefinition definition) =>
            new ElementWaveMotionFeature(
                definition.Amplitude,
                definition.FrequencyHz * math.PI * 2f,
                math.radians(definition.PhaseDegrees));



        private static ElementBoundaryFeature ToElementBoundary(in TanBoundaryDefinition definition) =>
            new ElementBoundaryFeature(
                ToElementBoundaryMode(definition.Mode),
                definition.Margin,
                definition.RequireEntered);



        private void SetElementBoundaryBounds(in TanBoundaryDefinition definition)
        {
            Rect bounds = definition.Bounds;
            var elementBounds = new ElementBounds2D(
                new float2(bounds.xMin, bounds.yMin),
                new float2(bounds.xMax, bounds.yMax));
            world.SetBoundaryBounds2D(ToElementBoundaryMode(definition.Mode), in elementBounds);
        }



        private static ElementBoundaryMode2D ToElementBoundaryMode(TanBoundaryMode mode) =>
            mode == TanBoundaryMode.CameraViewport
                ? ElementBoundaryMode2D.ViewBounds
                : ElementBoundaryMode2D.WorldBounds;



        private TanVisualOrientationDefinition ToTanVisualOrientation(
            in ElementVisualOrientationFeature feature,
            in TanVisualOrientationDefinition recorded)
        {
            TanFacingMode mode = recorded.Enabled
                ? recorded.FacingMode
                : feature.Mode == ElementVisualOrientationMode.Velocity
                    ? TanFacingMode.Velocity
                    : TanFacingMode.Manual;
            return new TanVisualOrientationDefinition(
                mode,
                math.degrees(feature.AxisOffsetRadians),
                math.degrees(feature.SpinRadiansPerSecond));
        }



        private static TanPrimaryMotionDefinition ToTanPrimaryMotion(
            in ElementDirectionalMotionFeature feature,
            in TanPrimaryMotionDefinition recorded)
        {
            if (feature.Mode == ElementDirectionalMotionMode.AngularTurn)
            {
                return TanPrimaryMotionDefinition.AngularTurn(
                    math.degrees(feature.AngularSpeedRadiansPerSecond));
            }

            var point = new Vector2(feature.HomingPoint.x, feature.HomingPoint.y);
            TanHomingTarget target = recorded.Mode == TanPrimaryMotionMode.Homing &&
                                     recorded.HomingTarget.Mode == TanHomingTargetMode.Target
                ? TanHomingTarget.ForTarget(recorded.HomingTarget.Target, point)
                : TanHomingTarget.ForFixedPoint(point);
            TanHomingTargetLossPolicy lossPolicy = recorded.Mode == TanPrimaryMotionMode.Homing
                ? recorded.TargetLossPolicy
                : TanHomingTargetLossPolicy.ContinueCurrentDirection;
            return TanPrimaryMotionDefinition.Homing(
                in target,
                math.degrees(feature.MaxTurnRadiansPerSecond),
                lossPolicy);
        }



        private static TanWaveMotionDefinition ToTanWaveMotion(in ElementWaveMotionFeature feature) =>
            new TanWaveMotionDefinition(
                feature.Amplitude,
                feature.AngularFrequencyRadiansPerSecond / (math.PI * 2f),
                math.degrees(feature.PhaseRadians));



        private TanBoundaryDefinition ToTanBoundary(in ElementBoundaryFeature feature)
        {
            Rect bounds = default;
            if (world.TryGetBoundaryBounds2D(feature.Mode, out ElementBounds2D elementBounds))
            {
                bounds = Rect.MinMaxRect(
                    elementBounds.Minimum.x,
                    elementBounds.Minimum.y,
                    elementBounds.Maximum.x,
                    elementBounds.Maximum.y);
            }

            TanBoundaryMode mode = feature.Mode == ElementBoundaryMode2D.ViewBounds
                ? TanBoundaryMode.CameraViewport
                : TanBoundaryMode.WorldBounds;
            return new TanBoundaryDefinition(
                mode,
                bounds,
                feature.Margin,
                feature.RequireEnteredBeforeExit);
        }
    }
}
