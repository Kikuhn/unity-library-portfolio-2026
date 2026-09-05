using System.Collections.Generic;
using NUnit.Framework;
using Pan.HighDensityElement;
using Pan.HighDensityElement.Editor;
using Pan.Tan.Element;
using Pan.Tan.Editor;
using UnityEngine;



namespace Pan.Tan.Tests
{
    public sealed class ElementTanRuntimeEditModeTests
    {
        private sealed class TargetResolver : IElementTanTargetResolver
        {
            public TanTargetHandle Target { get; set; }

            public bool TryResolveTarget(in ElementFact fact, out TanTargetHandle target)
            {
                target = Target;
                return target.IsValid;
            }
        }



        [Test]
        public void Handle_RuntimeDisposed_AllAccessReturnsFalse()
        {
            var runtime = new ElementTanRuntime(ElementTanBackendOptions.Query, 4);
            Assert.IsTrue(runtime.TrySpawn(CreateRequest(), out TanHandle handle));

            runtime.Dispose();

            Assert.IsFalse(handle.IsAlive);
            Assert.IsFalse(handle.TryGetSnapshot(out _));
            Assert.IsFalse(handle.TrySubmit(new DespawnTan()));
        }



        [Test]
        public void DebuggerRow_UsesActualElementTanMappingAndGeneration()
        {
            using var runtime = new ElementTanRuntime(ElementTanBackendOptions.Query, 4);
            Assert.IsTrue(runtime.TrySpawn(CreateRequest(), out TanHandle handle));
            ElementKey key = new ElementKey(handle.Key.ContextId, handle.Key.Slot, handle.Key.Generation);
            Assert.IsTrue(runtime.ElementWorld.TryGetSnapshot(key, out ElementSnapshot snapshot));
            var context = new ElementDebuggerRowContext(runtime.ElementWorld, snapshot);

            Assert.IsTrue(ElementTanDebuggerRowProvider.Instance.TryGetMetadata(in context, out ElementDebuggerRowMetadata metadata));
            Assert.AreEqual($"ElementTan {key.Slot}:{key.Generation}", metadata.DisplayName);
            Assert.AreEqual("ElementTan", metadata.Kind);
            Assert.IsNotNull(metadata.Icon);

            Assert.IsTrue(handle.TrySubmit(new DespawnTan()));
            runtime.ElementWorld.FlushCommands();
            Assert.IsFalse(ElementTanDebugRegistry.TryResolve(runtime.ElementWorld, key, out _));
        }



        [Test]
        public void LifetimeCapability_IsPhysicallyAbsentWhenDisabled()
        {
            using var runtime = new ElementTanRuntime(ElementTanBackendOptions.Query, 4);
            Assert.IsTrue(runtime.TrySpawn(CreateRequest(), out TanHandle handle));
            ElementKey key = new ElementKey(handle.Key.ContextId, handle.Key.Slot, handle.Key.Generation);

            Assert.IsTrue(runtime.ElementWorld.TryGetSnapshot(key, out ElementSnapshot element));
            Assert.AreEqual(ElementCapabilities.None, element.Capabilities & ElementCapabilities.Lifetime);
            Assert.IsFalse(runtime.ElementWorld.TryGetLifetime(key, out _));
            Assert.IsTrue(handle.TryGetSnapshot(out TanSnapshot tan));
            Assert.IsFalse(tan.RemainingLifetime.Enabled);
        }



        [TestCase(TanCcdMode.Auto, StrictCcdOverride2D.Auto, 0.25f)]
        [TestCase(TanCcdMode.Always, StrictCcdOverride2D.ForceOn, 0.75f)]
        [TestCase(TanCcdMode.Never, StrictCcdOverride2D.ForceOff, 1.25f)]
        public void Spawn_PerTanCcdPolicyOverridesRuntimeDefault(
            TanCcdMode mode,
            StrictCcdOverride2D expected,
            float ratio)
        {
            using var runtime = new ElementTanRuntime(
                new ElementTanBackendOptions(StrictCcdOverride2D.ForceOff, 2f),
                4);
            var collision = new TanCollisionDefinition(0, ~0);
            var definition = new TanDefinition(
                0.1f,
                in collision,
                ccdPolicy: new TanCcdPolicy(mode, ratio));
            var request = new TanSpawnRequest(in definition, Vector2.zero, Vector2.right);

            Assert.IsTrue(runtime.TrySpawn(in request, out TanHandle handle));
            Assert.IsTrue(runtime.TryGetElementHandle(handle.Key, out ElementHandle element));
            Assert.IsTrue(element.TryGetSnapshot(out ElementSnapshot snapshot));
            Assert.AreEqual(expected, snapshot.StrictCcdOverride);
            Assert.AreEqual(ratio, snapshot.StrictCcdThresholdRatio, 0.000001f);
        }



        [Test]
        public void Spawn_RuntimeDefaultCcdPolicyPreservesBackendOptions()
        {
            using var runtime = new ElementTanRuntime(
                new ElementTanBackendOptions(StrictCcdOverride2D.ForceOn, 0.8f),
                4);

            Assert.IsTrue(runtime.TrySpawn(CreateRequest(), out TanHandle handle));
            Assert.IsTrue(runtime.TryGetElementHandle(handle.Key, out ElementHandle element));
            Assert.IsTrue(element.TryGetSnapshot(out ElementSnapshot snapshot));
            Assert.AreEqual(StrictCcdOverride2D.ForceOn, snapshot.StrictCcdOverride);
            Assert.AreEqual(0.8f, snapshot.StrictCcdThresholdRatio, 0.000001f);
        }



        [Test]
        public void LifetimeCapability_IsStoredWhenEnabled()
        {
            using var runtime = new ElementTanRuntime(ElementTanBackendOptions.Query, 4);
            ElementUpdateSchedule schedule = ElementUpdateSchedule.UpdateFrames(2, ElementTimeDomain.Unscaled);
            var tanLifetime = new TanLifetimeDefinition(3f, schedule, useLocalClock: true);
            Assert.IsTrue(runtime.TrySpawn(CreateRequest(in tanLifetime), out TanHandle handle));
            ElementKey key = new ElementKey(handle.Key.ContextId, handle.Key.Slot, handle.Key.Generation);

            Assert.IsTrue(runtime.ElementWorld.TryGetSnapshot(key, out ElementSnapshot element));
            Assert.AreNotEqual(ElementCapabilities.None, element.Capabilities & ElementCapabilities.Lifetime);
            Assert.IsTrue(runtime.ElementWorld.TryGetLifetime(key, out ElementLifetimeFeature lifetimeFeature));
            Assert.AreEqual(3f, lifetimeFeature.RemainingUnits);
            Assert.AreEqual(ElementUpdateUnit.Frames, lifetimeFeature.Schedule.Unit);
            Assert.AreEqual(ElementTimeDomain.Unscaled, lifetimeFeature.Schedule.TimeDomain);
            Assert.IsTrue(lifetimeFeature.UseLocalClock);
            Assert.IsTrue(handle.TryGetSnapshot(out TanSnapshot tan));
            Assert.IsTrue(tan.RemainingLifetime.Enabled);
        }



        [Test]
        public void DespawnedHandle_RemainsStaleAfterSlotReuse()
        {
            using var runtime = new ElementTanRuntime(ElementTanBackendOptions.Query, 1);
            Assert.IsTrue(runtime.TrySpawn(CreateRequest(), out TanHandle first));
            Assert.IsTrue(first.TrySubmit(new DespawnTan()));
            runtime.ElementWorld.FlushCommands();
            Assert.IsFalse(first.IsAlive);

            Assert.IsTrue(runtime.TrySpawn(CreateRequest(), out TanHandle second));

            Assert.AreEqual(first.Key.Slot, second.Key.Slot);
            Assert.AreNotEqual(first.Key.Generation, second.Key.Generation);
            Assert.IsFalse(first.IsAlive);
            Assert.IsFalse(first.TryGetSnapshot(out _));
            Assert.IsTrue(second.IsAlive);
        }



        [Test]
        public void DeepElementHandle_RequiresLiveMatchingTanGeneration()
        {
            using var runtime = new ElementTanRuntime(ElementTanBackendOptions.Query, 1);
            Assert.IsTrue(runtime.TrySpawn(CreateRequest(), out TanHandle tan));
            Assert.IsTrue(runtime.TryGetElementHandle(tan.Key, out ElementHandle element));
            Assert.AreEqual(tan.Key.Slot, element.Key.Slot);
            Assert.AreEqual(tan.Key.Generation, element.Key.Generation);

            TanKey stale = tan.Key;
            Assert.IsTrue(tan.TrySubmit(new DespawnTan()));
            runtime.ElementWorld.FlushCommands();

            Assert.IsFalse(runtime.TryGetElementHandle(stale, out ElementHandle staleElement));
            Assert.IsFalse(staleElement.Key.IsValid);
        }



        [Test]
        public void Contact_IsRecordedOncePerEpisode_RecontactAfterSeparation_AndSourceIsExcluded()
        {
            var sourceKey = new TanKey(9, 3, 2);
            var source = new TanTargetHandle(TanTargetKind.External, in sourceKey);
            var collision = new TanCollisionDefinition(0, ~0);
            var definition = new TanDefinition(0.1f, in collision);
            var request = new TanSpawnRequest(in definition, Vector2.zero, Vector2.right, source);
            using var runtime = new ElementTanRuntime(ElementTanBackendOptions.Query, 4);
            Assert.IsTrue(runtime.TrySpawn(in request, out TanHandle handle));
            var resolver = new TargetResolver();
            runtime.SetTargetResolver(resolver);
            var contacts = new List<TanFact>();
            runtime.FactRaised += CaptureContact;

            var firstKey = new TanKey(11, 4, 1);
            var secondKey = new TanKey(11, 5, 1);
            var first = new TanTargetHandle(TanTargetKind.External, in firstKey);
            var second = new TanTargetHandle(TanTargetKind.External, in secondKey);
            var fact = new ElementFact
            {
                Type = ElementFactType.Contact,
                Element = new ElementKey(handle.Key.ContextId, handle.Key.Slot, handle.Key.Generation),
                Position = new Unity.Mathematics.float2(1f, 2f),
                Normal = new Unity.Mathematics.float2(-1f, 0f),
                ImpactCenter = new Unity.Mathematics.float2(0.25f, 0f),
                TimeOfImpact = 0.25f,
                FixedStepIndex = 7,
                SubstepIndex = 1,
                Flags = ElementFactFlags.HasSurfacePoint |
                        ElementFactFlags.HasSurfaceNormal |
                        ElementFactFlags.HasImpactCenter
            };

            runtime.PrepareFixedStep(0);
            resolver.Target = first;
            runtime.OnElementFact(in fact);
            runtime.OnElementFact(in fact);
            resolver.Target = second;
            runtime.OnElementFact(in fact);
            resolver.Target = source;
            runtime.OnElementFact(in fact);

            runtime.PrepareFixedStep(0);
            resolver.Target = first;
            runtime.OnElementFact(in fact);

            runtime.PrepareFixedStep(0);
            runtime.PrepareFixedStep(0);
            runtime.OnElementFact(in fact);
            runtime.ElementWorld.FlushCommands();

            Assert.AreEqual(3, contacts.Count);
            Assert.AreEqual(first, contacts[0].Target);
            Assert.AreEqual(second, contacts[1].Target);
            Assert.AreEqual(first, contacts[2].Target);
            Assert.AreEqual(new Vector2(1f, 2f), contacts[0].SurfacePoint);
            Assert.AreEqual(new Vector2(0.25f, 0f), contacts[0].TanCenterAtImpact);
            Assert.IsTrue(contacts[0].SurfacePointValid);
            Assert.IsTrue(contacts[0].NormalValid);
            Assert.IsTrue(contacts[0].IncomingDisplacementValid);
            Assert.IsTrue(handle.IsAlive);

            void CaptureContact(in TanFact emitted)
            {
                if (emitted.Type == TanFactType.Contact) { contacts.Add(emitted); }
            }
        }



        [Test]
        public void ContactGeometry_DistinguishesSurfaceImpactCenterAndEndPosition()
        {
            var tan = new TanKey(3, 4, 5);
            var targetKey = new TanKey(7, 8, 9);
            var target = new TanTargetHandle(TanTargetKind.External, in targetKey);
            Vector2 incoming = TanContactMath.CalculateIncomingDisplacement(Vector2.zero, new Vector2(4f, 0f));
            Vector2 impactCenter = TanContactMath.CalculateCenterAtImpact(Vector2.zero, new Vector2(4f, 0f), 0.25f);
            Vector2 normal = TanContactMath.NormalizeTargetToTanNormal(Vector2.right, incoming);
            var geometry = new TanContactGeometry(
                new Vector2(0.8f, 0f),
                impactCenter,
                new Vector2(4f, 0f),
                normal,
                incoming,
                0.25f,
                TanContactGeometryFlags.SurfacePointValid |
                TanContactGeometryFlags.NormalValid |
                TanContactGeometryFlags.IncomingDisplacementValid |
                TanContactGeometryFlags.ImpactCenterValid);

            TanFact fact = TanFact.CreateContact(in tan, in target, in geometry, 12, 1);

            Assert.AreEqual(new Vector2(0.8f, 0f), fact.Position);
            Assert.AreEqual(fact.SurfacePoint, fact.Position);
            Assert.AreEqual(new Vector2(1f, 0f), fact.TanCenterAtImpact);
            Assert.AreEqual(new Vector2(4f, 0f), fact.EndOfStepPosition);
            Assert.AreEqual(Vector2.left, fact.Normal);
            Assert.AreEqual(new Vector2(4f, 0f), fact.IncomingDisplacement);
            Assert.IsTrue(fact.SurfacePointValid);
            Assert.IsTrue(fact.NormalValid);
            Assert.IsTrue(fact.IncomingDisplacementValid);
            Assert.IsTrue(fact.HasImpactCenter);
            Assert.IsFalse(fact.StartedOverlapped);
            Assert.AreEqual(Vector2.left, TanContactMath.ReflectVelocity(Vector2.right, Vector2.left));
            Vector2 separated = TanContactMath.ApplySeparationEpsilon(
                new Vector2(1f, 0f),
                Vector2.left);
            Assert.AreEqual(0.999f, separated.x, 0.000001f);
            Assert.AreEqual(0f, separated.y, 0.000001f);
        }



        [Test]
        public void StartedOverlap_UsesImpactCenterFallbackWithoutClaimingSurfaceManifold()
        {
            var collision = new TanCollisionDefinition(0, ~0);
            var definition = new TanDefinition(0.1f, in collision);
            var request = new TanSpawnRequest(in definition, new Vector2(5f, 0f), Vector2.right);
            using var runtime = new ElementTanRuntime(ElementTanBackendOptions.Query, 4);
            Assert.IsTrue(runtime.TrySpawn(in request, out TanHandle handle));
            runtime.SetTargetResolver(new TargetResolver
            {
                Target = new TanTargetHandle(
                    TanTargetKind.External,
                    new TanKey(8, 2, 1))
            });
            runtime.SimulateFixedStep(0.5f, 0);

            TanFact captured = default;
            runtime.FactRaised += Capture;
            var raw = new ElementFact
            {
                Type = ElementFactType.Contact,
                Element = new ElementKey(handle.Key.ContextId, handle.Key.Slot, handle.Key.Generation),
                Position = new Unity.Mathematics.float2(float.NaN, float.NaN),
                Normal = new Unity.Mathematics.float2(float.NaN, float.NaN),
                ImpactCenter = new Unity.Mathematics.float2(5f, 0f),
                TimeOfImpact = 0f,
                Flags = ElementFactFlags.StartedOverlapped | ElementFactFlags.HasImpactCenter
            };

            runtime.OnElementFact(in raw);

            Assert.AreEqual(TanFactType.Contact, captured.Type);
            Assert.IsTrue(captured.StartedOverlapped);
            Assert.IsFalse(captured.HasSurfacePoint);
            Assert.IsFalse(captured.HasSurfaceNormal);
            Assert.IsTrue(captured.HasImpactCenter);
            Assert.AreEqual(new Vector2(5f, 0f), captured.SurfacePoint);
            Assert.AreEqual(new Vector2(5f, 0f), captured.TanCenterAtImpact);
            Assert.AreEqual(new Vector2(5.5f, 0f), captured.EndOfStepPosition);
            Assert.AreEqual(new Vector2(0.5f, 0f), captured.IncomingDisplacement);

            void Capture(in TanFact fact)
            {
                if (fact.Type == TanFactType.Contact) { captured = fact; }
            }
        }



        [Test]
        public void OptionalFeatureRemoval_IsIdempotent_AndDisabledConfigureMeansRemove()
        {
            using var runtime = new ElementTanRuntime(ElementTanBackendOptions.Query, 4);
            Assert.IsTrue(runtime.TrySpawn(CreateRequest(), out TanHandle handle));

            Assert.IsTrue(handle.TrySubmit(new RemoveTanLifetime()));
            Assert.IsTrue(handle.TrySubmit(new RemoveTanLocalClock()));
            Assert.IsTrue(handle.TrySubmit(new ConfigureTanLifetime(TanLifetimeDefinition.Disabled)));

            var lifetime = new TanLifetimeDefinition(2f, ElementUpdateSchedule.UpdateSeconds());
            Assert.IsTrue(handle.TrySubmit(new ConfigureTanLifetime(in lifetime)));
            Assert.IsTrue(handle.TrySubmit(new ConfigureTanLifetime(TanLifetimeDefinition.Disabled)));
            Assert.IsTrue(handle.TrySubmit(new RemoveTanLifetime()));
        }



        [Test]
        public void ExplicitLifetime_AdvancesOnlyThroughScheduledFeatureTick()
        {
            using var runtime = new ElementTanRuntime(ElementTanBackendOptions.Query, 4);
            var lifetime = new TanLifetimeDefinition(3f, ElementUpdateSchedule.UpdateSeconds());
            Assert.IsTrue(runtime.TrySpawn(CreateRequest(in lifetime), out TanHandle handle));

            runtime.SimulateFixedStep(1f, 0);
            Assert.IsTrue(handle.TryGetSnapshot(out TanSnapshot afterPhysics));
            Assert.AreEqual(3f, afterPhysics.RemainingLifetime.Value);

            runtime.TickUpdateFeatures(1f, 0.25f);
            Assert.IsTrue(handle.TryGetSnapshot(out TanSnapshot afterUpdate));
            Assert.AreEqual(2f, afterUpdate.RemainingLifetime.Value);
        }



        [Test]
        public void LocalClock_HalfSpeedAndPauseControlScheduledLifetime()
        {
            using var runtime = new ElementTanRuntime(ElementTanBackendOptions.Query, 4);
            var lifetime = new TanLifetimeDefinition(
                10f,
                ElementUpdateSchedule.UpdateSeconds(),
                useLocalClock: true);
            Assert.IsTrue(runtime.TrySpawn(CreateRequest(in lifetime), out TanHandle handle));
            ElementKey key = new ElementKey(handle.Key.ContextId, handle.Key.Slot, handle.Key.Generation);

            Assert.IsTrue(handle.TrySubmit(new ConfigureTanLocalClock(0.5f)));
            Assert.IsTrue(runtime.ElementWorld.TryGetLocalClock(key, out ElementLocalClock halfSpeed));
            Assert.AreEqual(0.5f, halfSpeed.TimeScale);
            runtime.TickUpdateFeatures(2f, 2f);
            Assert.IsTrue(handle.TryGetSnapshot(out TanSnapshot afterHalfSpeed));
            Assert.AreEqual(9f, afterHalfSpeed.RemainingLifetime.Value);

            Assert.IsTrue(handle.TrySubmit(new ConfigureTanLocalClock(0.5f, paused: true)));
            runtime.TickUpdateFeatures(2f, 2f);
            Assert.IsTrue(handle.TryGetSnapshot(out TanSnapshot afterPause));
            Assert.AreEqual(9f, afterPause.RemainingLifetime.Value);

            Assert.IsTrue(handle.TrySubmit(new RemoveTanLocalClock()));
            Assert.IsFalse(runtime.ElementWorld.TryGetLocalClock(key, out _));
            runtime.TickUpdateFeatures(2f, 2f);
            Assert.IsTrue(handle.TryGetSnapshot(out TanSnapshot afterRemove));
            Assert.AreEqual(7f, afterRemove.RemainingLifetime.Value);
        }



        [Test]
        public void OptionalBehaviors_AreAbsentByDefaultAndAllocatedOnlyWhenConfigured()
        {
            using var runtime = new ElementTanRuntime(ElementTanBackendOptions.Query, 4);
            Assert.IsTrue(runtime.TrySpawn(CreateRequest(), out TanHandle plain));
            Assert.IsTrue(plain.TryGetFeatureSnapshot(out TanFeatureSnapshot plainFeatures));
            Assert.IsFalse(plainFeatures.HasVisualOrientation);
            Assert.IsFalse(plainFeatures.HasPrimaryMotion);
            Assert.IsFalse(plainFeatures.HasWaveMotion);
            Assert.IsFalse(plainFeatures.HasBoundary);

            var collision = new TanCollisionDefinition(0, ~0);
            var orientation = new TanVisualOrientationDefinition(TanFacingMode.Velocity, -90f, 30f);
            TanPrimaryMotionDefinition primary = TanPrimaryMotionDefinition.AngularTurn(45f);
            var wave = new TanWaveMotionDefinition(0.5f, 2f, 90f);
            var boundary = new TanBoundaryDefinition(
                TanBoundaryMode.WorldBounds,
                new Rect(-10f, -10f, 20f, 20f),
                1f,
                requireEntered: false);
            var definition = new TanDefinition(
                0.1f,
                in collision,
                visualId: 1,
                rotationDegrees: 15f,
                visualOrientation: orientation,
                primaryMotion: primary,
                waveMotion: wave,
                boundary: boundary);
            var request = new TanSpawnRequest(in definition, Vector2.zero, Vector2.right);

            Assert.IsTrue(runtime.TrySpawn(in request, out TanHandle configured));
            Assert.IsTrue(configured.TryGetSnapshot(out TanSnapshot snapshot));
            Assert.AreEqual(15f, snapshot.RotationDegrees, 0.0001f);
            Assert.IsTrue(configured.TryGetFeatureSnapshot(out TanFeatureSnapshot features));
            Assert.AreEqual(TanFacingMode.Velocity, features.VisualOrientation.FacingMode);
            Assert.AreEqual(TanPrimaryMotionMode.AngularTurn, features.PrimaryMotion.Mode);
            Assert.AreEqual(2f, features.WaveMotion.FrequencyHz, 0.0001f);
            Assert.AreEqual(TanBoundaryMode.WorldBounds, features.Boundary.Mode);
        }



        [Test]
        public void VisualCommands_ChangeOneElementTanAfterCommandBoundary()
        {
            using var runtime = new ElementTanRuntime(ElementTanBackendOptions.Query, 4);
            Assert.IsTrue(runtime.TrySpawn(CreateRequest(), out TanHandle handle));

            Assert.IsTrue(handle.TrySubmit(new SetTanVisualId(7)));
            Assert.IsTrue(handle.TrySubmit(new SetTanColor(new Color32(10, 20, 30, 40))));
            Assert.IsTrue(handle.TrySubmit(new SetTanVisualScale2D(new Vector2(2f, 3f))));
            Assert.IsTrue(handle.TrySubmit(new SetTanRotation2D(135f)));
            runtime.ElementWorld.FlushCommands();

            Assert.IsTrue(handle.TryGetSnapshot(out TanSnapshot snapshot));
            Assert.AreEqual(7, snapshot.VisualId);
            Assert.AreEqual(new Color32(10, 20, 30, 40), snapshot.Color);
            Assert.AreEqual(new Vector2(2f, 3f), snapshot.VisualScale);
            Assert.AreEqual(135f, snapshot.RotationDegrees, 0.0001f);
        }



        [Test]
        public void HomingTargetLossPolicy_ContinueRemovesFeature_TrackLastPreservesFeature()
        {
            using var runtime = new ElementTanRuntime(ElementTanBackendOptions.Query, 4);
            var targetHandle = new TanTargetHandle(TanTargetKind.External, new TanKey(20, 1, 1));
            TanHomingTarget homingTarget = TanHomingTarget.ForTarget(targetHandle, new Vector2(3f, 0f));
            TanPrimaryMotionDefinition homing = TanPrimaryMotionDefinition.Homing(
                in homingTarget,
                180f,
                TanHomingTargetLossPolicy.ContinueCurrentDirection);
            var collision = new TanCollisionDefinition(0, ~0);
            var definition = new TanDefinition(0.1f, in collision, primaryMotion: homing);
            var request = new TanSpawnRequest(in definition, Vector2.zero, Vector2.right);
            Assert.IsTrue(runtime.TrySpawn(in request, out TanHandle continueTan));

            Assert.IsTrue(continueTan.TrySubmit(new UpdateTanHomingTargetPosition2D(Vector2.zero, false)));
            runtime.ElementWorld.FlushCommands();
            Assert.IsTrue(continueTan.TryGetFeatureSnapshot(out TanFeatureSnapshot continued));
            Assert.IsFalse(continued.HasPrimaryMotion);

            homing = TanPrimaryMotionDefinition.Homing(
                in homingTarget,
                180f,
                TanHomingTargetLossPolicy.TrackLastPosition);
            definition = new TanDefinition(0.1f, in collision, primaryMotion: homing);
            request = new TanSpawnRequest(in definition, Vector2.zero, Vector2.right);
            Assert.IsTrue(runtime.TrySpawn(in request, out TanHandle lastPointTan));
            Assert.IsTrue(lastPointTan.TrySubmit(new UpdateTanHomingTargetPosition2D(Vector2.zero, false)));
            runtime.ElementWorld.FlushCommands();
            Assert.IsTrue(lastPointTan.TryGetFeatureSnapshot(out TanFeatureSnapshot tracked));
            Assert.IsTrue(tracked.HasPrimaryMotion);
        }



        [Test]
        public void SharedBoundaryRuntimeContract_UpdatesViewBoundsOnce()
        {
            using var runtime = new ElementTanRuntime(ElementTanBackendOptions.Query, 4);
            ITanBoundaryRuntimeBackend boundaryRuntime = runtime;
            var bounds = new Rect(-8f, -4f, 16f, 8f);

            Assert.IsTrue(boundaryRuntime.TrySetBoundaryBounds2D(TanBoundaryMode.CameraViewport, bounds));
            Assert.IsTrue(runtime.ElementWorld.TryGetBoundaryBounds2D(
                ElementBoundaryMode2D.ViewBounds,
                out ElementBounds2D stored));
            Assert.AreEqual(new Unity.Mathematics.float2(-8f, -4f), stored.Minimum);
            Assert.AreEqual(new Unity.Mathematics.float2(8f, 4f), stored.Maximum);
        }



        [Test]
        public void BoundaryExit_EmitsBackendNeutralFactBeforeDespawn()
        {
            using var runtime = new ElementTanRuntime(ElementTanBackendOptions.Query, 4);
            var collision = new TanCollisionDefinition(0, ~0);
            var boundary = new TanBoundaryDefinition(
                TanBoundaryMode.WorldBounds,
                new Rect(-1f, -1f, 2f, 2f));
            var definition = new TanDefinition(0.1f, in collision, boundary: boundary);
            var request = new TanSpawnRequest(in definition, Vector2.zero, new Vector2(4f, 0f));
            var facts = new List<TanFactType>();
            runtime.FactRaised += Capture;

            Assert.IsTrue(runtime.TrySpawn(in request, out _));
            runtime.SimulateFixedStep(1f, 0);

            int boundaryIndex = facts.IndexOf(TanFactType.BoundaryExited);
            int despawnIndex = facts.IndexOf(TanFactType.Despawned);
            Assert.GreaterOrEqual(boundaryIndex, 0);
            Assert.Greater(despawnIndex, boundaryIndex);

            void Capture(in TanFact fact) => facts.Add(fact.Type);
        }



        private static TanSpawnRequest CreateRequest(OptionalTanFloat lifetime = default)
        {
            var collision = new TanCollisionDefinition(0, ~0);
            var definition = new TanDefinition(0.1f, in collision, lifetime: lifetime);
            return new TanSpawnRequest(in definition, Vector2.zero, Vector2.right);
        }

        private static TanSpawnRequest CreateRequest(in TanLifetimeDefinition lifetime)
        {
            var collision = new TanCollisionDefinition(0, ~0);
            var definition = new TanDefinition(0.1f, in collision, in lifetime);
            return new TanSpawnRequest(in definition, Vector2.zero, Vector2.right);
        }
    }
}
