using NUnit.Framework;
using Pan.Tan.Normal;
using UnityEngine;



namespace Pan.Tan.Tests
{
    public sealed class NormalTanContractsEditModeTests
    {
        [Test]
        public void BackendOptions_DefaultStructUsesAutoHalfCcdPolicy()
        {
            NormalTanBackendOptions options = default;

            Assert.AreEqual(TanCcdMode.Auto, options.DefaultCcdPolicy.Mode);
            Assert.AreEqual(0.5f, options.DefaultCcdPolicy.MotionToDiameterRatio, 0.000001f);
        }



        [Test]
        public void BackendOptions_RuntimeDefaultInputNormalizesToAutoHalf()
        {
            var options = new NormalTanBackendOptions(
                defaultCcdPolicy: new TanCcdPolicy(TanCcdMode.RuntimeDefault, 2f));

            Assert.AreEqual(TanCcdMode.Auto, options.DefaultCcdPolicy.Mode);
            Assert.AreEqual(0.5f, options.DefaultCcdPolicy.MotionToDiameterRatio, 0.000001f);
        }



        [Test]
        public void BackendOptions_ExplicitCcdPolicyIsPreserved()
        {
            var options = new NormalTanBackendOptions(
                defaultCcdPolicy: new TanCcdPolicy(TanCcdMode.Always, 0.75f));

            Assert.AreEqual(TanCcdMode.Always, options.DefaultCcdPolicy.Mode);
            Assert.AreEqual(0.75f, options.DefaultCcdPolicy.MotionToDiameterRatio, 0.000001f);
        }



        [Test]
        public void Snapshot_LegacyConstructorDefaultsRotationToZero()
        {
            var key = new TanKey(1, 2, 3);
            var snapshot = new TanSnapshot(
                in key,
                TanBackendKind.Normal,
                Vector2.zero,
                Vector2.one,
                Vector2.right,
                0.1f,
                default,
                0,
                4,
                Vector2.one,
                new Color32(255, 255, 255, 255));

            Assert.AreEqual(0f, snapshot.RotationDegrees);
        }



        [Test]
        public void BehaviorDefinitions_DefaultValuesDoNotEnableSparseFeatures()
        {
            TanVisualOrientationDefinition orientation = default;
            TanPrimaryMotionDefinition primary = default;
            TanWaveMotionDefinition wave = default;
            TanBoundaryDefinition boundary = default;

            Assert.IsFalse(orientation.Enabled);
            Assert.IsFalse(primary.Enabled);
            Assert.IsFalse(wave.Enabled);
            Assert.IsFalse(boundary.Enabled);
        }



        [Test]
        public void VisualAndMotionCommands_PreserveBackendNeutralPayloads()
        {
            var orientation = new TanVisualOrientationDefinition(TanFacingMode.Velocity, -90f, 45f);
            var target = TanHomingTarget.ForFixedPoint(new Vector2(3f, 4f));
            TanPrimaryMotionDefinition primary = TanPrimaryMotionDefinition.Homing(
                in target,
                180f,
                TanHomingTargetLossPolicy.TrackLastPosition);
            var wave = new TanWaveMotionDefinition(2f, 3f, 90f);
            var boundary = new TanBoundaryDefinition(
                TanBoundaryMode.CameraViewport,
                new Rect(-5f, -4f, 10f, 8f),
                1f,
                requireEntered: true);

            TanCommand orientationCommand = new ConfigureTanVisualOrientation(in orientation).ToTanCommand();
            TanCommand primaryCommand = new ConfigureTanPrimaryMotion(in primary).ToTanCommand();
            TanCommand waveCommand = new ConfigureTanWaveMotion(in wave).ToTanCommand();
            TanCommand boundaryCommand = new ConfigureTanBoundary2D(in boundary).ToTanCommand();

            Assert.AreEqual(TanCommandType.ConfigureVisualOrientation, orientationCommand.Type);
            Assert.AreEqual(-90f, orientationCommand.VisualOrientationValue.AxisOffsetDegrees);
            Assert.AreEqual(TanPrimaryMotionMode.Homing, primaryCommand.PrimaryMotionValue.Mode);
            Assert.AreEqual(new Vector2(3f, 4f), primaryCommand.PrimaryMotionValue.HomingTarget.FixedPoint);
            Assert.AreEqual(3f, waveCommand.WaveMotionValue.FrequencyHz);
            Assert.AreEqual(TanBoundaryMode.CameraViewport, boundaryCommand.BoundaryValue.Mode);
            Assert.AreEqual(1f, boundaryCommand.BoundaryValue.Margin);
        }
    }
}
