using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using Unity.U2D.Physics;
using UnityEngine;
using UnityEngine.TestTools;



namespace Pan.HighDensityElement.Tests
{
    public sealed class ElementWorldEditModeTests
    {
        private readonly List<ElementFact> facts = new List<ElementFact>();



        [SetUp]
        public void SetUp() => facts.Clear();



        [Test]
        public void ElementFact_ContactGeometryContractFitsEightyBytes()
        {
            Assert.AreEqual(80, Marshal.SizeOf<ElementFact>());
        }



        [Test]
        public void RecycledSlot_NeverRevivesStaleKey()
        {
            using var world = new ElementWorld(4);
            ElementCompiledArchetype archetype = CreateArchetype(
                ElementCapabilities.KinematicMotion2D | ElementCapabilities.Lifetime);
            ElementHandle first = world.Spawn(ElementSpawnBuilder.From(archetype)).Handle;
            var despawn = new DespawnElement();

            Assert.IsTrue(first.TrySubmit(in despawn));
            world.Tick(0f);

            ElementHandle second = world.Spawn(ElementSpawnBuilder.From(archetype)).Handle;

            Assert.AreEqual(first.Key.Slot, second.Key.Slot);
            Assert.AreNotEqual(first.Key.Generation, second.Key.Generation);
            Assert.IsFalse(first.IsAlive);
            Assert.IsFalse(first.TrySubmit(in despawn));
            Assert.IsTrue(second.IsAlive);
        }



        [Test]
        public void KinematicMotion_UpdatesPreviousAndCurrentPose()
        {
            using var world = new ElementWorld(4);
            ElementCompiledArchetype archetype = CreateArchetype(
                ElementCapabilities.KinematicMotion2D | ElementCapabilities.Lifetime);
            ElementSpawnBuilder builder = ElementSpawnBuilder.From(archetype)
                .WithPose(new float2(1f, 2f), new float2(1f))
                .WithVelocity(new float2(3f, -2f));
            ElementHandle handle = world.Spawn(in builder).Handle;

            world.Tick(0.5f);

            Assert.IsTrue(handle.TryGetSnapshot(out ElementSnapshot snapshot));
            Assert.AreEqual(new float2(1f, 2f), snapshot.PreviousPosition);
            Assert.AreEqual(new float2(2.5f, 1f), snapshot.Position);
        }



        [Test]
        public void RenderSnapshot_InterpolatesBetweenFixedStepPosesAndClampsAlpha()
        {
            using var world = new ElementWorld(4);
            ElementCompiledArchetype archetype = CreateArchetype(
                ElementCapabilities.KinematicMotion2D | ElementCapabilities.SpriteVisual2D);
            ElementHandle handle = world.Spawn(
                ElementSpawnBuilder.From(archetype)
                    .WithVelocity(new float2(4f, 0f))).Handle;

            world.Tick(0.5f);

            Assert.IsTrue(handle.TryGetRenderSnapshot(0.25f, out ElementRenderSnapshot interpolated));
            Assert.AreEqual(float2.zero, interpolated.PreviousPosition);
            Assert.AreEqual(new float2(2f, 0f), interpolated.CurrentPosition);
            Assert.That(interpolated.Position.x, Is.EqualTo(0.5f).Within(0.0001f));

            Assert.IsTrue(handle.TryGetRenderSnapshot(float.NaN, out ElementRenderSnapshot invalidAlpha));
            Assert.AreEqual(invalidAlpha.CurrentPosition, invalidAlpha.Position);
            Assert.That(invalidAlpha.InterpolationAlpha, Is.EqualTo(1f));

            Assert.IsTrue(handle.TryGetRenderSnapshot(-1f, out ElementRenderSnapshot before));
            Assert.AreEqual(before.PreviousPosition, before.Position);
            Assert.IsTrue(handle.TryGetRenderSnapshot(2f, out ElementRenderSnapshot after));
            Assert.AreEqual(after.CurrentPosition, after.Position);
        }



        [Test]
        public void CommandDrain_AppliesVisualChangeBeforeTickSnapshot()
        {
            using var world = new ElementWorld(4);
            ElementCompiledArchetype archetype = CreateArchetype(
                ElementCapabilities.KinematicMotion2D | ElementCapabilities.SpriteVisual2D);
            ElementHandle handle = world.Spawn(ElementSpawnBuilder.From(archetype)).Handle;
            var setVisual = new SetElementVisualId(17);

            handle.Submit(in setVisual);
            world.Tick(0f);

            Assert.IsTrue(handle.TryGetSnapshot(out ElementSnapshot snapshot));
            Assert.AreEqual(17, snapshot.VisualId);
        }



        [Test]
        public void WorldDispose_InvalidatesHandlesAndRejectsFurtherTicks()
        {
            var world = new ElementWorld(4);
            ElementCompiledArchetype archetype = CreateArchetype(ElementCapabilities.KinematicMotion2D);
            ElementHandle handle = world.Spawn(ElementSpawnBuilder.From(archetype)).Handle;
            world.Dispose();
            var velocity = new SetElementVelocity2D(new float2(1f));

            Assert.IsFalse(handle.IsAlive);
            Assert.IsFalse(handle.TrySubmit(in velocity));
            Assert.Throws<System.ObjectDisposedException>(() => world.Tick(0f));
        }



        [Test]
        public void AllLaneJobs_TickAndDispose_DoNotLeaveNativeContainerDependencies()
        {
            var world = new ElementWorld(8);
            Exception tickException = null;
            Exception disposeException = null;

            try
            {
                world.Spawn(ElementSpawnBuilder.From(CreateArchetype(
                    ElementCapabilities.KinematicMotion2D | ElementCapabilities.QuerySensor2D)));
                world.Spawn(ElementSpawnBuilder.From(CreateArchetype(
                    ElementCapabilities.AreaSensor2D)));
                world.Spawn(ElementSpawnBuilder.From(CreateArchetype(
                    ElementCapabilities.DynamicBody2D,
                    physicsBodyMode: PhysicsCoreBodyMode.Dynamic)));

                try { world.Tick(0.02f); }
                catch (Exception exception) { tickException = exception; }
            }
            finally
            {
                try { world.Dispose(); }
                catch (Exception exception) { disposeException = exception; }
            }

            if (tickException != null || disposeException != null)
            {
                Assert.Fail(
                    $"모든 lane의 Tick/Dispose가 Job dependency를 남기지 않아야 합니다.\n" +
                    $"Tick: {tickException}\nDispose: {disposeException}");
            }
        }



        [Test]
        public void FactSubscriberException_DoesNotBlockLaterSubscriberOrGenerationTransition()
        {
            using var world = new ElementWorld(4);
            ElementHandle handle = world.Spawn(
                ElementSpawnBuilder.From(CreateArchetype(ElementCapabilities.KinematicMotion2D))).Handle;
            facts.Clear();
            world.FactDispatched += ThrowingFactSubscriber;
            world.FactDispatched += OnFact;
            LogAssert.Expect(LogType.Exception, new Regex("throwing fact subscriber", RegexOptions.IgnoreCase));

            Assert.IsTrue(world.TryDespawnImmediately(handle.Key));
            world.FactDispatched -= ThrowingFactSubscriber;

            Assert.IsFalse(handle.IsAlive);
            Assert.That(facts.Exists(fact =>
                fact.Type == ElementFactType.Despawned && fact.Element == handle.Key), Is.True);
            ElementHandle recycled = world.Spawn(
                ElementSpawnBuilder.From(CreateArchetype(ElementCapabilities.KinematicMotion2D))).Handle;
            Assert.AreEqual(handle.Key.Slot, recycled.Key.Slot);
            Assert.AreNotEqual(handle.Key.Generation, recycled.Key.Generation);
        }



        [Test]
        public void QuerySensor_IsBodylessAndShapeCastHitsFastProjectile()
        {
            using var world = new ElementWorld(4);
            world.FactDispatched += OnFact;
            ElementHandle target = SpawnTarget(world, 5f, PhysicsCoreShape2D.Circle);
            ElementCompiledArchetype projectileArchetype = CreateArchetype(
                ElementCapabilities.KinematicMotion2D | ElementCapabilities.QuerySensor2D,
                radius: 0.25f);
            ElementSpawnBuilder projectileBuilder = ElementSpawnBuilder.From(projectileArchetype)
                .WithVelocity(new float2(20f, 0f));
            ElementHandle projectile = world.Spawn(in projectileBuilder).Handle;
            facts.Clear();

            world.Tick(0.5f, 7);

            Assert.AreEqual(1, world.PhysicsCoreLane.BodyCount, "QuerySensor projectile은 PhysicsBody를 만들지 않아야 합니다.");
            ElementFact contact = facts.Find(fact =>
                fact.Type == ElementFactType.Contact &&
                fact.Element == projectile.Key &&
                fact.TargetElement == target.Key);
            Assert.AreEqual(7, contact.SubstepIndex);
            Assert.That(contact.TimeOfImpact, Is.InRange(float.Epsilon, 0.999999f));
            Assert.IsFalse(contact.StartedOverlapped);
            Assert.IsTrue(contact.HasSurfacePoint);
            Assert.IsTrue(contact.HasSurfaceNormal);
            Assert.IsTrue(contact.HasImpactCenter);
            Assert.AreEqual(contact.Position, contact.SurfacePoint);
            Assert.AreEqual(contact.Normal, contact.SurfaceNormal);
            Assert.That(contact.ImpactCenter.x, Is.EqualTo(10f * contact.TimeOfImpact).Within(0.0001f));
            Assert.Less(contact.SurfaceNormal.x, 0f);
        }



        [Test]
        public void QuerySensor_StartingOverlapReportsFractionZero()
        {
            using var world = new ElementWorld(4);
            world.FactDispatched += OnFact;
            ElementHandle target = SpawnTarget(world, 0f, PhysicsCoreShape2D.Box);
            ElementCompiledArchetype projectileArchetype = CreateArchetype(
                ElementCapabilities.KinematicMotion2D | ElementCapabilities.QuerySensor2D,
                radius: 0.25f);
            ElementHandle projectile = world.Spawn(ElementSpawnBuilder.From(projectileArchetype)).Handle;
            facts.Clear();

            world.Tick(0.02f);

            ElementFact contact = facts.Find(fact =>
                fact.Type == ElementFactType.Contact &&
                fact.Element == projectile.Key &&
                fact.TargetElement == target.Key);
            Assert.AreEqual(0f, contact.TimeOfImpact);
            Assert.IsTrue(contact.StartedOverlapped);
            Assert.IsTrue(contact.HasSurfacePoint);
            Assert.IsTrue(contact.HasSurfaceNormal);
            Assert.IsTrue(contact.HasImpactCenter);
            Assert.AreEqual(float2.zero, contact.ImpactCenter);
            Assert.IsTrue(math.all(math.isfinite(contact.SurfacePoint)));
            Assert.That(math.length(contact.SurfaceNormal), Is.EqualTo(1f).Within(0.0001f));
        }



        [TestCase(PhysicsShape.ShapeType.Circle)]
        [TestCase(PhysicsShape.ShapeType.Capsule)]
        [TestCase(PhysicsShape.ShapeType.Polygon)]
        [TestCase(PhysicsShape.ShapeType.Segment)]
        [TestCase(PhysicsShape.ShapeType.ChainSegment)]
        public void QuerySensor_StartingOverlapProvidesManifoldSurfaceGeometry(
            PhysicsShape.ShapeType shapeType)
        {
            using var world = new ElementWorld(4);
            PhysicsBody targetBody = CreateBridgeContactGeometryBody(
                world.PhysicsCoreLane,
                500 + (int)shapeType,
                shapeType);
            try
            {
                world.FactDispatched += OnFact;
                ElementCompiledArchetype projectileArchetype = CreateArchetype(
                    ElementCapabilities.KinematicMotion2D | ElementCapabilities.QuerySensor2D,
                    radius: 0.25f);
                ElementHandle projectile = world.Spawn(
                    ElementSpawnBuilder.From(projectileArchetype)
                        .WithPose(new float2(0f, -0.2f), new float2(1f))).Handle;
                facts.Clear();

                world.Tick(0.02f);

                ElementFact contact = facts.Find(fact =>
                    fact.Type == ElementFactType.Contact &&
                    fact.Element == projectile.Key &&
                    fact.BridgeTargetId == 500 + (int)shapeType);
                Assert.IsTrue(contact.StartedOverlapped, shapeType.ToString());
                Assert.IsTrue(contact.HasSurfacePoint, shapeType.ToString());
                Assert.IsTrue(contact.HasSurfaceNormal, shapeType.ToString());
                Assert.IsTrue(contact.HasImpactCenter, shapeType.ToString());
                Assert.AreEqual(new float2(0f, -0.2f), contact.ImpactCenter);
                Assert.IsTrue(math.all(math.isfinite(contact.SurfacePoint)), shapeType.ToString());
                Assert.That(math.length(contact.SurfaceNormal), Is.EqualTo(1f).Within(0.0001f), shapeType.ToString());
                Assert.Less(contact.SurfaceNormal.y, 0f, shapeType.ToString());
            }
            finally
            {
                if (targetBody.isValid) { targetBody.Destroy(); }
            }
        }



        [Test]
        public void QuerySensor_MultipleHitsAreSortedByTimeOfImpact()
        {
            using var world = new ElementWorld(8);
            world.FactDispatched += OnFact;
            ElementHandle nearTarget = SpawnTarget(world, 3f, PhysicsCoreShape2D.Circle);
            ElementHandle farTarget = SpawnTarget(world, 7f, PhysicsCoreShape2D.Circle);
            ElementCompiledArchetype projectileArchetype = CreateArchetype(
                ElementCapabilities.KinematicMotion2D | ElementCapabilities.QuerySensor2D,
                radius: 0.25f);
            ElementSpawnBuilder projectileBuilder = ElementSpawnBuilder.From(projectileArchetype)
                .WithVelocity(new float2(10f, 0f));
            ElementHandle projectile = world.Spawn(in projectileBuilder).Handle;
            facts.Clear();

            world.Tick(1f);

            List<ElementFact> contacts = facts.FindAll(fact =>
                fact.Type == ElementFactType.Contact && fact.Element == projectile.Key);
            Assert.That(contacts, Has.Count.EqualTo(2));
            Assert.AreEqual(nearTarget.Key, contacts[0].TargetElement);
            Assert.AreEqual(farTarget.Key, contacts[1].TargetElement);
            Assert.Less(contacts[0].TimeOfImpact, contacts[1].TimeOfImpact);
        }



        [Test]
        public void QuerySensor_IgnoresQueryableElementWithSameNonZeroOwner()
        {
            using var world = new ElementWorld(8);
            world.FactDispatched += OnFact;
            ElementCompiledArchetype targetArchetype = CreateArchetype(
                ElementCapabilities.QueryTarget2D,
                radius: 0.5f);
            ElementHandle target = world.Spawn(
                ElementSpawnBuilder.From(targetArchetype)
                    .WithPose(new float2(3f, 0f), new float2(1f))
                    .WithOwner(77u, 1u)).Handle;
            ElementCompiledArchetype projectileArchetype = CreateArchetype(
                ElementCapabilities.KinematicMotion2D | ElementCapabilities.QuerySensor2D,
                radius: 0.25f);
            ElementHandle sameOwner = world.Spawn(
                ElementSpawnBuilder.From(projectileArchetype)
                    .WithVelocity(new float2(10f, 0f))
                    .WithOwner(77u, 1u)).Handle;
            facts.Clear();

            world.Tick(0.5f);

            Assert.That(facts.Exists(fact =>
                fact.Type == ElementFactType.Contact &&
                fact.Element == sameOwner.Key &&
                fact.TargetElement == target.Key), Is.False);

            ElementHandle differentOwner = world.Spawn(
                ElementSpawnBuilder.From(projectileArchetype)
                    .WithVelocity(new float2(10f, 0f))
                    .WithOwner(78u, 1u)).Handle;
            facts.Clear();
            world.Tick(0.5f);

            Assert.That(facts.Exists(fact =>
                fact.Type == ElementFactType.Contact &&
                fact.Element == differentOwner.Key &&
                fact.TargetElement == target.Key), Is.True);
        }



        [Test]
        public void MovingQueryTarget_SynchronizesPassivePhysicsCoreShape()
        {
            using var world = new ElementWorld(4);
            ElementCompiledArchetype targetArchetype = CreateArchetype(
                ElementCapabilities.KinematicMotion2D | ElementCapabilities.QueryTarget2D,
                radius: 0.5f);
            ElementHandle target = world.Spawn(
                ElementSpawnBuilder.From(targetArchetype)
                    .WithVelocity(new float2(6f, 0f))).Handle;

            world.Tick(0.25f);

            Assert.IsTrue(target.TryGetSnapshot(out ElementSnapshot snapshot));
            Assert.IsTrue(world.PhysicsCoreLane.TryGetBody(target.Key, out var body));
            Assert.That(snapshot.Position.x, Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(body.position.x, Is.EqualTo(snapshot.Position.x).Within(0.0001f));
            Assert.That(body.position.y, Is.EqualTo(snapshot.Position.y).Within(0.0001f));
        }



        [Test]
        public void LifetimeExpiration_DispatchesExpiredBeforeDespawned()
        {
            using var world = new ElementWorld(4);
            world.FactDispatched += OnFact;
            ElementCompiledArchetype archetype = CreateArchetype(ElementCapabilities.Lifetime);
            ElementHandle handle = world.Spawn(ElementSpawnBuilder.From(archetype).WithLifetime(0.1f)).Handle;
            facts.Clear();

            world.Tick(0.2f);

            Assert.AreEqual(ElementFactType.LifetimeExpired, facts[0].Type);
            Assert.AreEqual(ElementFactType.Despawned, facts[1].Type);
            Assert.IsFalse(handle.IsAlive);
        }



        [Test]
        public void AreaSensor_ProducesEnterStayExitAgainstQueryTarget()
        {
            using var world = new ElementWorld(4);
            world.FactDispatched += OnFact;
            ElementCompiledArchetype areaArchetype = CreateArchetype(
                ElementCapabilities.Lifetime | ElementCapabilities.AreaSensor2D,
                radius: 2f);
            ElementHandle area = world.Spawn(ElementSpawnBuilder.From(areaArchetype)).Handle;
            ElementHandle target = SpawnTarget(world, 0f, PhysicsCoreShape2D.Circle);
            facts.Clear();

            world.Tick(0.02f);
            Assert.That(facts.Exists(fact =>
                fact.Type == ElementFactType.Enter &&
                fact.Element == area.Key &&
                fact.TargetElement == target.Key), Is.True);

            facts.Clear();
            world.Tick(0.02f);
            Assert.That(facts.Exists(fact =>
                fact.Type == ElementFactType.Stay &&
                fact.Element == area.Key &&
                fact.TargetElement == target.Key), Is.True);

            facts.Clear();
            var moveAway = new SetElementPosition2D(new float2(20f, 0f));
            target.Submit(in moveAway);
            world.Tick(0.02f);
            Assert.That(facts.Exists(fact =>
                fact.Type == ElementFactType.Exit &&
                fact.Element == area.Key &&
                fact.TargetElement == target.Key), Is.True);
        }



        [Test]
        public void AreaSensor_ProducesPeriodicFactWithoutContact()
        {
            using var world = new ElementWorld(4);
            world.FactDispatched += OnFact;
            ElementCompiledArchetype archetype = CreateArchetype(
                ElementCapabilities.AreaSensor2D | ElementCapabilities.PeriodicFact,
                periodicInterval: 0.25f);
            world.Spawn(ElementSpawnBuilder.From(archetype));
            facts.Clear();

            world.Tick(0.3f);

            Assert.That(facts.Exists(fact => fact.Type == ElementFactType.Periodic), Is.True);
        }



        [Test]
        public void UnsupportedFeatureCombination_FailsExplicitly()
        {
            bool success = ElementCompiledArchetype.TryCreate(
                ElementCapabilities.QuerySensor2D | ElementCapabilities.AreaSensor2D,
                1f,
                1f,
                0,
                0f,
                false,
                PhysicsCoreBodyMode.Kinematic,
                PhysicsCoreShape2D.Circle,
                out _,
                out ElementSpawnStatus failure);

            Assert.IsFalse(success);
            Assert.AreEqual(ElementSpawnStatus.UnsupportedLayout, failure);
        }



        [Test]
        public void RenderSpriteInstanceLayout_UsesUnityRequiredFieldNames()
        {
            Assert.DoesNotThrow(() => Marshal.OffsetOf<RenderSpriteInstanceData>("objectToWorld"));
            Assert.DoesNotThrow(() => Marshal.OffsetOf<RenderSpriteInstanceData>("spriteColor"));
            Assert.DoesNotThrow(() => Marshal.OffsetOf<RenderSpriteInstanceData>("renderingLayerMask"));
            Assert.LessOrEqual(ElementSpriteRenderer.ConservativeDefaultInstancesPerBatch, 1023);
        }



        [Test]
        public void DynamicBody_CreatesAndDestroysCircleBody()
        {
            using var world = new ElementWorld(4);
            ElementCompiledArchetype archetype = CreateArchetype(
                ElementCapabilities.DynamicBody2D | ElementCapabilities.SpriteVisual2D,
                physicsShape: PhysicsCoreShape2D.Circle,
                physicsBodyMode: PhysicsCoreBodyMode.Dynamic);
            ElementHandle handle = world.Spawn(ElementSpawnBuilder.From(archetype)).Handle;

            Assert.IsTrue(world.PhysicsCoreLane.IsValid);
            Assert.AreEqual(1, world.PhysicsCoreLane.BodyCount);
            Assert.IsTrue(world.PhysicsCoreLane.TryGetBody(handle.Key, out _));

            var despawn = new DespawnElement();
            handle.Submit(in despawn);
            world.Tick(0f);
            Assert.AreEqual(0, world.PhysicsCoreLane.BodyCount);
        }



        [Test]
        public void DynamicBody_BatchCreateAndDestroy_UsesPhysicsCoreBatchApis()
        {
            using var world = new ElementWorld(16);
            ElementCompiledArchetype archetype = CreateArchetype(
                ElementCapabilities.DynamicBody2D | ElementCapabilities.SpriteVisual2D,
                physicsShape: PhysicsCoreShape2D.Box,
                physicsBodyMode: PhysicsCoreBodyMode.Dynamic);
            var builders = new ElementSpawnBuilder[8];
            var results = new ElementSpawnResult[8];
            for (int i = 0; i < builders.Length; i++)
            {
                builders[i] = ElementSpawnBuilder.From(archetype)
                    .WithPose(new float2(i * 2f, 0f), new float2(1f));
            }

            Assert.AreEqual(builders.Length, world.SpawnBatch(builders, results));
            Assert.AreEqual(builders.Length, world.PhysicsCoreLane.BodyCount);
            Assert.AreEqual(1, world.PhysicsCoreLane.BodyBatchCreateCount);

            for (int i = 0; i < results.Length; i++)
            {
                var despawn = new DespawnElement();
                results[i].Handle.Submit(in despawn);
            }
            world.Tick(0f);

            Assert.AreEqual(0, world.PhysicsCoreLane.BodyCount);
            Assert.AreEqual(1, world.PhysicsCoreLane.BodyBatchDestroyCount);
            for (int i = 0; i < results.Length; i++) { Assert.IsFalse(results[i].Handle.IsAlive); }
        }



        [Test]
        public void DynamicBody_SynchronizesPoseAndProducesCircleBoxContact()
        {
            using var world = new ElementWorld(4);
            world.FactDispatched += OnFact;
            ElementCompiledArchetype dynamicCircle = CreateArchetype(
                ElementCapabilities.DynamicBody2D,
                radius: 0.5f,
                physicsBodyMode: PhysicsCoreBodyMode.Dynamic);
            ElementCompiledArchetype targetBox = CreateArchetype(
                ElementCapabilities.QueryTarget2D,
                radius: 0.5f,
                physicsShape: PhysicsCoreShape2D.Box);
            ElementHandle moving = world.Spawn(
                ElementSpawnBuilder.From(dynamicCircle).WithVelocity(new float2(1f, 0f))).Handle;
            ElementHandle target = world.Spawn(
                ElementSpawnBuilder.From(targetBox).WithPose(new float2(0.75f, 0f), new float2(1f))).Handle;
            facts.Clear();

            world.Tick(1f / 30f);

            Assert.IsTrue(moving.TryGetSnapshot(out ElementSnapshot snapshot));
            Assert.IsTrue(world.PhysicsCoreLane.TryGetBody(moving.Key, out var physicsBody));
            Assert.That(snapshot.Position.x, Is.EqualTo(physicsBody.position.x).Within(0.0001f));
            Assert.That(snapshot.Position.y, Is.EqualTo(physicsBody.position.y).Within(0.0001f));
            Assert.That(facts.Exists(fact =>
                fact.Type == ElementFactType.Contact &&
                fact.Element == moving.Key &&
                fact.TargetElement == target.Key), Is.True);
        }



        [Test]
        public void DynamicBodies_EmitBidirectionalContactEnterStayExit()
        {
            using var world = new ElementWorld(4);
            world.FactDispatched += OnFact;
            ElementCompiledArchetype archetype = CreateArchetype(
                ElementCapabilities.DynamicBody2D,
                radius: 0.75f,
                physicsBodyMode: PhysicsCoreBodyMode.Dynamic);
            ElementHandle first = world.Spawn(
                ElementSpawnBuilder.From(archetype).WithPose(new float2(-0.25f, 0f), new float2(1f))).Handle;
            ElementHandle second = world.Spawn(
                ElementSpawnBuilder.From(archetype).WithPose(new float2(0.25f, 0f), new float2(1f))).Handle;

            facts.Clear();
            world.Tick(0.02f);
            AssertDirectedPhase(first.Key, second.Key, ElementFactType.Contact);
            AssertDirectedPhase(second.Key, first.Key, ElementFactType.Contact);
            AssertDirectedPhase(first.Key, second.Key, ElementFactType.Enter);
            AssertDirectedPhase(second.Key, first.Key, ElementFactType.Enter);
            ElementFact firstContact = facts.Find(fact =>
                fact.Type == ElementFactType.Contact &&
                fact.Element == first.Key &&
                fact.TargetElement == second.Key);
            ElementFact secondContact = facts.Find(fact =>
                fact.Type == ElementFactType.Contact &&
                fact.Element == second.Key &&
                fact.TargetElement == first.Key);
            Assert.IsTrue(firstContact.HasSurfacePoint);
            Assert.IsTrue(firstContact.HasSurfaceNormal);
            Assert.IsFalse(firstContact.HasImpactCenter);
            Assert.Less(firstContact.SurfaceNormal.x, 0f);
            Assert.Greater(secondContact.SurfaceNormal.x, 0f);
            Assert.That(math.dot(firstContact.SurfaceNormal, secondContact.SurfaceNormal),
                Is.EqualTo(-1f).Within(0.0001f));
            ElementFact firstEnter = facts.Find(fact =>
                fact.Type == ElementFactType.Enter &&
                fact.Element == first.Key &&
                fact.TargetElement == second.Key);
            Assert.IsTrue(firstEnter.HasSurfacePoint);
            Assert.IsTrue(firstEnter.HasSurfaceNormal);

            Assert.IsTrue(world.PhysicsCoreLane.TryGetBody(first.Key, out PhysicsBody firstBody));
            Assert.IsTrue(world.PhysicsCoreLane.TryGetBody(second.Key, out PhysicsBody secondBody));
            firstBody.position = new Vector2(-0.25f, 0f);
            secondBody.position = new Vector2(0.25f, 0f);
            firstBody.linearVelocity = Vector2.zero;
            secondBody.linearVelocity = Vector2.zero;
            facts.Clear();
            world.Tick(0.02f);
            AssertDirectedPhase(first.Key, second.Key, ElementFactType.Stay);
            AssertDirectedPhase(second.Key, first.Key, ElementFactType.Stay);

            secondBody.position = new Vector2(10f, 0f);
            secondBody.linearVelocity = Vector2.zero;
            facts.Clear();
            world.Tick(0.02f);
            AssertDirectedPhase(first.Key, second.Key, ElementFactType.Exit);
            AssertDirectedPhase(second.Key, first.Key, ElementFactType.Exit);
            ElementFact firstExit = facts.Find(fact =>
                fact.Type == ElementFactType.Exit &&
                fact.Element == first.Key &&
                fact.TargetElement == second.Key);
            Assert.AreEqual(ElementFactFlags.None, firstExit.Flags);
        }



        [Test]
        public void DynamicBodyAndProjectionTrigger_EmitPhasesWithoutMovingProjectionBody()
        {
            using var world = new ElementWorld(4);
            world.FactDispatched += OnFact;
            ElementCompiledArchetype archetype = CreateArchetype(
                ElementCapabilities.DynamicBody2D,
                radius: 0.5f,
                physicsBodyMode: PhysicsCoreBodyMode.Dynamic);
            ElementHandle element = world.Spawn(ElementSpawnBuilder.From(archetype)).Handle;
            PhysicsBody projectionBody = CreateBridgeCircleBody(
                world.PhysicsCoreLane,
                bridgeTargetId: 9001,
                position: Vector2.zero,
                radius: 1f,
                isTrigger: true);
            Vector2 authoritativePosition = projectionBody.position;

            try
            {
                facts.Clear();
                world.Tick(0.02f);
                AssertBridgePhase(element.Key, 9001, ElementFactType.Enter, expectedTrigger: true);
                ElementFact triggerEnter = facts.Find(fact =>
                    fact.Type == ElementFactType.Enter &&
                    fact.Element == element.Key &&
                    fact.BridgeTargetId == 9001);
                Assert.AreEqual(ElementFactFlags.None, triggerEnter.Flags);
                Assert.That(projectionBody.position, Is.EqualTo(authoritativePosition));
                Assert.That(projectionBody.linearVelocity, Is.EqualTo(Vector2.zero));

                facts.Clear();
                world.Tick(0.02f);
                AssertBridgePhase(element.Key, 9001, ElementFactType.Stay, expectedTrigger: true);
                Assert.That(projectionBody.position, Is.EqualTo(authoritativePosition));

                projectionBody.position = new Vector2(10f, 0f);
                facts.Clear();
                world.Tick(0.02f);
                AssertBridgePhase(element.Key, 9001, ElementFactType.Exit, expectedTrigger: true);
            }
            finally
            {
                if (projectionBody.isValid) { projectionBody.Destroy(); }
            }
        }



        [Test]
        public void DynamicBodyAndSolidProjection_EmitContactEnterStayExitWithoutWritingProjection()
        {
            using var world = new ElementWorld(4);
            world.FactDispatched += OnFact;
            ElementCompiledArchetype archetype = CreateArchetype(
                ElementCapabilities.DynamicBody2D,
                radius: 0.75f,
                physicsBodyMode: PhysicsCoreBodyMode.Dynamic);
            ElementHandle element = world.Spawn(
                ElementSpawnBuilder.From(archetype).WithPose(new float2(-0.25f, 0f), new float2(1f))).Handle;
            PhysicsBody projectionBody = CreateBridgeCircleBody(
                world.PhysicsCoreLane,
                bridgeTargetId: 9002,
                position: new Vector2(0.25f, 0f),
                radius: 0.75f,
                isTrigger: false);
            Vector2 authoritativePosition = projectionBody.position;

            try
            {
                facts.Clear();
                world.Tick(0.02f);
                AssertBridgePhase(element.Key, 9002, ElementFactType.Contact, expectedTrigger: false);
                AssertBridgePhase(element.Key, 9002, ElementFactType.Enter, expectedTrigger: false);
                Assert.That(projectionBody.position, Is.EqualTo(authoritativePosition));
                Assert.That(projectionBody.linearVelocity, Is.EqualTo(Vector2.zero));

                Assert.IsTrue(world.PhysicsCoreLane.TryGetBody(element.Key, out PhysicsBody dynamicBody));
                dynamicBody.position = new Vector2(-0.25f, 0f);
                dynamicBody.linearVelocity = Vector2.zero;
                projectionBody.position = authoritativePosition;
                facts.Clear();
                world.Tick(0.02f);
                AssertBridgePhase(element.Key, 9002, ElementFactType.Stay, expectedTrigger: false);
                Assert.That(projectionBody.position, Is.EqualTo(authoritativePosition));

                projectionBody.position = new Vector2(10f, 0f);
                facts.Clear();
                world.Tick(0.02f);
                AssertBridgePhase(element.Key, 9002, ElementFactType.Exit, expectedTrigger: false);
            }
            finally
            {
                if (projectionBody.isValid) { projectionBody.Destroy(); }
            }
        }



        private void OnFact(in ElementFact fact) => facts.Add(fact);



        private void AssertDirectedPhase(ElementKey element, ElementKey target, ElementFactType type)
        {
            Assert.That(facts.Exists(fact =>
                fact.Type == type && fact.Element == element && fact.TargetElement == target), Is.True);
        }



        private void AssertBridgePhase(
            ElementKey element,
            int bridgeTargetId,
            ElementFactType type,
            bool expectedTrigger)
        {
            Assert.That(facts.Exists(fact =>
                fact.Type == type &&
                fact.Element == element &&
                fact.BridgeTargetId == bridgeTargetId &&
                fact.TriggerContact == expectedTrigger), Is.True);
        }



        private static PhysicsBody CreateBridgeCircleBody(
            PhysicsCore2DLane lane,
            int bridgeTargetId,
            Vector2 position,
            float radius,
            bool isTrigger)
        {
            PhysicsBodyDefinition bodyDefinition = PhysicsBodyDefinition.defaultDefinition;
            bodyDefinition.type = PhysicsBody.BodyType.Kinematic;
            bodyDefinition.position = position;
            bodyDefinition.linearVelocity = Vector2.zero;
            bodyDefinition.angularVelocity = 0f;
            bodyDefinition.transformWriteMode = PhysicsBody.TransformWriteMode.Off;
            PhysicsBody body = lane.World.CreateBody(bodyDefinition);
            PhysicsCore2DLane.TagBridgeTarget(body, bridgeTargetId);

            PhysicsShapeDefinition shapeDefinition = PhysicsShapeDefinition.defaultDefinition;
            shapeDefinition.contactEvents = true;
            shapeDefinition.triggerEvents = true;
            shapeDefinition.isTrigger = isTrigger;
            PhysicsShape shape = body.CreateShape(
                new CircleGeometry { center = Vector2.zero, radius = radius },
                shapeDefinition);
            PhysicsCore2DLane.TagBridgeTarget(shape, bridgeTargetId);
            return body;
        }



        private static PhysicsBody CreateBridgeContactGeometryBody(
            PhysicsCore2DLane lane,
            int bridgeTargetId,
            PhysicsShape.ShapeType shapeType)
        {
            PhysicsBodyDefinition bodyDefinition = PhysicsBodyDefinition.defaultDefinition;
            bodyDefinition.type = PhysicsBody.BodyType.Kinematic;
            bodyDefinition.transformWriteMode = PhysicsBody.TransformWriteMode.Off;
            PhysicsBody body = lane.World.CreateBody(bodyDefinition);
            PhysicsCore2DLane.TagBridgeTarget(body, bridgeTargetId);

            PhysicsShapeDefinition shapeDefinition = PhysicsShapeDefinition.defaultDefinition;
            PhysicsShape shape;
            switch (shapeType)
            {
                case PhysicsShape.ShapeType.Circle:
                    shape = body.CreateShape(
                        new CircleGeometry { center = Vector2.zero, radius = 0.5f },
                        shapeDefinition);
                    break;
                case PhysicsShape.ShapeType.Capsule:
                    shape = body.CreateShape(
                        CapsuleGeometry.Create(new Vector2(-0.5f, 0f), new Vector2(0.5f, 0f), 0.25f),
                        shapeDefinition);
                    break;
                case PhysicsShape.ShapeType.Polygon:
                    shape = body.CreateShape(
                        PolygonGeometry.CreateBox(new Vector2(1f, 0.5f), 0f, true),
                        shapeDefinition);
                    break;
                case PhysicsShape.ShapeType.Segment:
                    shape = body.CreateShape(
                        SegmentGeometry.Create(new Vector2(-1f, 0f), new Vector2(1f, 0f)),
                        shapeDefinition);
                    break;
                case PhysicsShape.ShapeType.ChainSegment:
                    shape = body.CreateShape(
                        new ChainSegmentGeometry
                        {
                            ghost1 = new Vector2(-2f, 0f),
                            segment = SegmentGeometry.Create(new Vector2(-1f, 0f), new Vector2(1f, 0f)),
                            ghost2 = new Vector2(2f, 0f)
                        },
                        shapeDefinition);
                    break;
                default:
                    body.Destroy();
                    throw new ArgumentOutOfRangeException(nameof(shapeType));
            }

            PhysicsCore2DLane.TagBridgeTarget(shape, bridgeTargetId);
            return body;
        }



        private static void ThrowingFactSubscriber(in ElementFact fact)
        {
            throw new InvalidOperationException("throwing fact subscriber");
        }



        private static ElementHandle SpawnTarget(
            ElementWorld world,
            float x,
            PhysicsCoreShape2D shape)
        {
            ElementCompiledArchetype targetArchetype = CreateArchetype(
                ElementCapabilities.QueryTarget2D,
                radius: 0.5f,
                physicsShape: shape);
            return world.Spawn(
                ElementSpawnBuilder.From(targetArchetype)
                    .WithPose(new float2(x, 0f), new float2(1f)))
                .Handle;
        }



        [Test]
        public void ExecutionModelAndStrictCcd_AreExposedWithoutBackendInspection()
        {
            using var world = new ElementWorld(8)
            {
                EnableDetailedDiagnostics = true
            };
            ElementCompiledArchetype queryArchetype = CreateArchetype(
                ElementCapabilities.KinematicMotion2D | ElementCapabilities.QuerySensor2D);
            ElementCompiledArchetype simulatedArchetype = CreateArchetype(
                ElementCapabilities.DynamicBody2D,
                physicsBodyMode: PhysicsCoreBodyMode.Dynamic);

            ElementHandle query = world.Spawn(
                ElementSpawnBuilder.From(queryArchetype)
                    .WithVelocity(new float2(2f, 0f))
                    .WithStrictCcd(StrictCcdOverride2D.Auto, 0.5f)).Handle;
            ElementHandle simulated = world.Spawn(ElementSpawnBuilder.From(simulatedArchetype)).Handle;

            world.Tick(0.5f);

            Assert.IsTrue(query.TryGetSnapshot(out ElementSnapshot querySnapshot));
            Assert.AreEqual(ElementExecutionModel2D.Query, querySnapshot.ExecutionModel);
            Assert.IsTrue(querySnapshot.StrictCcdActive);
            Assert.IsTrue(simulated.TryGetSnapshot(out ElementSnapshot simulatedSnapshot));
            Assert.AreEqual(ElementExecutionModel2D.Simulated, simulatedSnapshot.ExecutionModel);
            Assert.AreEqual(1u, world.FixedStepIndex);
            Assert.GreaterOrEqual(world.GetDiagnostics().StrictCcdCandidateCount, 1);
        }



        [Test]
        public void StrictCcdRequestOverridesForceOffAndTeleportIsReportedForOneStep()
        {
            using var world = new ElementWorld(4)
            {
                EnableDetailedDiagnostics = true
            };
            ElementCompiledArchetype archetype = CreateArchetype(ElementCapabilities.KinematicMotion2D);
            ElementHandle handle = world.Spawn(
                ElementSpawnBuilder.From(archetype)
                    .WithStrictCcd(StrictCcdOverride2D.ForceOff)).Handle;

            Assert.IsTrue(handle.RequestStrictCcd(1));
            Assert.IsTrue(handle.MarkTeleported());
            world.Tick(0.02f);

            Assert.IsTrue(handle.TryGetSnapshot(out ElementSnapshot first));
            Assert.IsTrue(first.StrictCcdActive);
            Assert.IsTrue(first.TeleportedThisStep);
            Assert.AreEqual(1, world.GetDiagnostics().ForcedStrictCcdCount);
            Assert.AreEqual(1, world.GetDiagnostics().TeleportCount);

            world.Tick(0.02f);

            Assert.IsTrue(handle.TryGetSnapshot(out ElementSnapshot second));
            Assert.IsFalse(second.StrictCcdActive);
            Assert.IsFalse(second.TeleportedThisStep);
        }



        [Test]
        public void MovingProjectionCrossesStationaryQueryElement_EmitsContact()
        {
            using var world = new ElementWorld(4);
            ElementCompiledArchetype projectileArchetype = CreateArchetype(
                ElementCapabilities.KinematicMotion2D | ElementCapabilities.QuerySensor2D,
                radius: 0.1f);
            ElementHandle projectile = world.Spawn(ElementSpawnBuilder.From(projectileArchetype)).Handle;
            MovingProjectionShape2D moving = CreateMovingProjection(
                bridgeTargetId: 41,
                stableShapeId: 1,
                previous: new float2(-2f, 0f),
                current: new float2(2f, 0f));
            world.PhysicsCoreLane.ReplaceMovingProjections(new[] { moving }, 1);
            world.FactDispatched += CaptureFact;

            world.Tick(0.02f);

            Assert.That(facts.FindAll(fact => fact.Type == ElementFactType.Contact).Count, Is.EqualTo(1));
            ElementFact contact = facts.Find(fact => fact.Type == ElementFactType.Contact);
            Assert.AreEqual(projectile.Key, contact.Element);
            Assert.AreEqual(41, contact.BridgeTargetId);
            Assert.That(contact.TimeOfImpact, Is.InRange(0f, 1f));
            Assert.IsFalse(contact.StartedOverlapped);
            Assert.IsTrue(contact.HasSurfacePoint);
            Assert.IsTrue(contact.HasSurfaceNormal);
            Assert.IsTrue(contact.HasImpactCenter);
            Assert.Greater(contact.SurfaceNormal.x, 0f);
        }



        [Test]
        public void MovingProjection_StartingOverlapProvidesFlagsAndImpactCenter()
        {
            using var world = new ElementWorld(4);
            ElementCompiledArchetype projectileArchetype = CreateArchetype(
                ElementCapabilities.KinematicMotion2D | ElementCapabilities.QuerySensor2D,
                radius: 0.25f);
            ElementHandle projectile = world.Spawn(ElementSpawnBuilder.From(projectileArchetype)).Handle;
            MovingProjectionShape2D moving = CreateMovingProjection(
                bridgeTargetId: 42,
                stableShapeId: 1,
                previous: new float2(0.1f, 0f),
                current: new float2(0.1f, 0f));
            world.PhysicsCoreLane.ReplaceMovingProjections(new[] { moving }, 1);
            world.FactDispatched += CaptureFact;

            world.Tick(0.02f);

            ElementFact contact = facts.Find(fact =>
                fact.Type == ElementFactType.Contact &&
                fact.Element == projectile.Key &&
                fact.BridgeTargetId == 42);
            Assert.IsTrue(contact.StartedOverlapped);
            Assert.IsTrue(contact.HasSurfacePoint);
            Assert.IsTrue(contact.HasSurfaceNormal);
            Assert.IsTrue(contact.HasImpactCenter);
            Assert.AreEqual(float2.zero, contact.ImpactCenter);
            Assert.Less(contact.SurfaceNormal.x, 0f);
        }



        [Test]
        public void MovingProjectionTrigger_IsFilteredBeforeFactDispatch()
        {
            using var world = new ElementWorld(4);
            ElementCompiledArchetype query = CreateArchetype(
                ElementCapabilities.KinematicMotion2D | ElementCapabilities.QuerySensor2D,
                radius: 0.25f);
            ElementHandle projectile = world.Spawn(
                ElementSpawnBuilder.From(query).WithTriggerInteraction(includeTriggers: false)).Handle;
            MovingProjectionShape2D moving = CreateMovingProjection(
                711,
                1,
                new float2(-2f, 0f),
                new float2(2f, 0f));
            moving.IsTrigger = 1;
            world.PhysicsCoreLane.ReplaceMovingProjections(new[] { moving }, 1);
            world.FactDispatched += CaptureFact;

            world.Tick(0.02f);

            Assert.That(facts.Exists(fact =>
                fact.Type == ElementFactType.Contact && fact.Element == projectile.Key), Is.False);
        }



        [Test]
        public void NormalCastAndStrictToiForSameBridgeTarget_DispatchEarliestContactOnlyOnce()
        {
            using var world = new ElementWorld(4);
            PhysicsBody bridgeBody = CreateBridgeCircleBody(
                world.PhysicsCoreLane,
                bridgeTargetId: 712,
                position: Vector2.zero,
                radius: 0.5f,
                isTrigger: false);
            try
            {
                ElementCompiledArchetype query = CreateArchetype(
                    ElementCapabilities.KinematicMotion2D | ElementCapabilities.QuerySensor2D,
                    radius: 0.25f);
                ElementHandle projectile = world.Spawn(
                    ElementSpawnBuilder.From(query)
                        .WithPose(new float2(-2f, 0f), new float2(1f))
                        .WithVelocity(new float2(4f, 0f))).Handle;
                MovingProjectionShape2D duplicateProjection = CreateMovingProjection(
                    712,
                    1,
                    float2.zero,
                    float2.zero);
                world.PhysicsCoreLane.ReplaceMovingProjections(new[] { duplicateProjection }, 1);
                world.FactDispatched += CaptureFact;

                world.Tick(1f);

                List<ElementFact> contacts = facts.FindAll(fact =>
                    fact.Type == ElementFactType.Contact &&
                    fact.Element == projectile.Key &&
                    fact.BridgeTargetId == 712);
                Assert.AreEqual(1, contacts.Count);
                Assert.That(contacts[0].TimeOfImpact, Is.InRange(0f, 1f));
            }
            finally
            {
                if (bridgeBody.isValid) { bridgeBody.Destroy(); }
            }
        }



        [Test]
        public void TeleportedOrBroadphaseRejectedProjection_DoesNotCreateSweptContact()
        {
            using var world = new ElementWorld(4);
            ElementCompiledArchetype projectileArchetype = CreateArchetype(
                ElementCapabilities.KinematicMotion2D | ElementCapabilities.QuerySensor2D,
                radius: 0.1f);
            world.Spawn(ElementSpawnBuilder.From(projectileArchetype));
            MovingProjectionShape2D teleported = CreateMovingProjection(
                51,
                1,
                new float2(-2f, 0f),
                new float2(2f, 0f),
                teleported: true);
            MovingProjectionShape2D farAway = CreateMovingProjection(
                52,
                1,
                new float2(-2f, 20f),
                new float2(2f, 20f));
            world.PhysicsCoreLane.ReplaceMovingProjections(new[] { teleported, farAway }, 1);
            world.FactDispatched += CaptureFact;

            world.Tick(0.02f);

            Assert.IsFalse(facts.Exists(fact => fact.Type == ElementFactType.Contact));
        }



        [Test]
        public void MovingProjectionMultipleShapes_ChoosesEarliestAndDeduplicatesTarget()
        {
            using var world = new ElementWorld(4);
            ElementCompiledArchetype projectileArchetype = CreateArchetype(
                ElementCapabilities.KinematicMotion2D | ElementCapabilities.QuerySensor2D,
                radius: 0.1f);
            world.Spawn(ElementSpawnBuilder.From(projectileArchetype));
            MovingProjectionShape2D late = CreateMovingProjection(
                61,
                2,
                new float2(-3f, 0f),
                new float2(1f, 0f));
            MovingProjectionShape2D early = CreateMovingProjection(
                61,
                1,
                new float2(-1f, 0f),
                new float2(3f, 0f));
            world.PhysicsCoreLane.ReplaceMovingProjections(new[] { late, early }, 1);
            world.FactDispatched += CaptureFact;

            world.Tick(0.02f);

            List<ElementFact> contacts = facts.FindAll(fact => fact.Type == ElementFactType.Contact);
            Assert.AreEqual(1, contacts.Count);
            Assert.AreEqual(61, contacts[0].BridgeTargetId);
            Assert.That(contacts[0].TimeOfImpact, Is.LessThan(0.5f));
        }



        [Test]
        public void RegularCastAndMovingProjectionFacts_AreSortedByEarliestTimeOfImpact()
        {
            using var world = new ElementWorld(8);
            ElementCompiledArchetype projectileArchetype = CreateArchetype(
                ElementCapabilities.KinematicMotion2D | ElementCapabilities.QuerySensor2D,
                radius: 0.1f);
            ElementHandle projectile = world.Spawn(
                ElementSpawnBuilder.From(projectileArchetype)
                    .WithPose(new float2(-2f, 0f), new float2(1f))
                    .WithVelocity(new float2(4f, 0f))).Handle;
            SpawnTarget(world, 1.25f, PhysicsCoreShape2D.Circle);
            MovingProjectionShape2D moving = CreateMovingProjection(
                66,
                1,
                new float2(0f, -2f),
                new float2(0f, 2f));
            world.PhysicsCoreLane.ReplaceMovingProjections(new[] { moving }, 1);
            world.FactDispatched += CaptureFact;

            world.Tick(1f);

            List<ElementFact> contacts = facts.FindAll(fact =>
                fact.Type == ElementFactType.Contact && fact.Element == projectile.Key);
            Assert.GreaterOrEqual(contacts.Count, 2);
            for (int i = 1; i < contacts.Count; i++)
            {
                Assert.LessOrEqual(contacts[i - 1].TimeOfImpact, contacts[i].TimeOfImpact);
            }
        }



        [Test]
        public void EveryElement_HasGenerationSafeMandatoryPose()
        {
            using var world = new ElementWorld(4);
            ElementCompiledArchetype archetype = CreateArchetype(ElementCapabilities.None);
            ElementHandle first = world.Spawn(
                ElementSpawnBuilder.From(archetype).WithPose(new float2(2f, 3f), new float2(4f, 5f))).Handle;

            Assert.IsTrue(world.TryGetPose(first.Key, out ElementPose2D firstPose));
            Assert.AreEqual(new float2(2f, 3f), firstPose.Position);
            Assert.AreEqual(new float2(4f, 5f), firstPose.Scale);
            var changedPose = new ElementPose2D(
                new float2(6f, 7f),
                new float2(7f, 8f),
                0.25f,
                0.5f,
                new float2(1.5f),
                new float2(2f));
            Assert.IsTrue(world.TrySetPose(first.Key, in changedPose));
            Assert.IsTrue(world.TryGetPose(first.Key, out ElementPose2D storedPose));
            Assert.AreEqual(0.25f, storedPose.PreviousRotationRadians);
            Assert.AreEqual(0.5f, storedPose.RotationRadians);
            Assert.AreEqual(new float2(1.5f), storedPose.PreviousScale);
            Assert.IsTrue(first.TryGetSnapshot(out ElementSnapshot changed));
            Assert.AreEqual(new float2(7f, 8f), changed.Position);

            ElementKey stale = first.Key;
            Assert.IsTrue(world.TryDespawnImmediately(first.Key));
            ElementHandle recycled = world.Spawn(ElementSpawnBuilder.From(archetype)).Handle;

            Assert.AreEqual(stale.Slot, recycled.Key.Slot);
            Assert.AreNotEqual(stale.Generation, recycled.Key.Generation);
            Assert.IsFalse(world.TryGetPose(stale, out _));
        }



        [Test]
        public void SparseFeatureStore_RemoveSwapBack_PreservesMovedKeyAndRejectsStaleGeneration()
        {
            using var store = new ElementSparseFeatureStore<int>(4, Allocator.Temp);
            var first = new ElementKey(10, 0, 1);
            var second = new ElementKey(10, 1, 1);
            var staleSecond = new ElementKey(10, 1, 2);

            Assert.IsTrue(store.TryAdd(first, 11));
            Assert.IsTrue(store.TryAdd(second, 22));
            Assert.IsTrue(store.Remove(first));
            Assert.IsTrue(store.TryGet(second, out int moved));
            Assert.AreEqual(22, moved);
            Assert.IsFalse(store.TryGet(staleSecond, out _));
            Assert.AreEqual(1, store.Count);
        }



        [Test]
        public void LifetimeFeature_UnscaledSeconds_ExpiresWithoutRunningPhysicsTick()
        {
            using var world = new ElementWorld(4);
            ElementHandle handle = world.Spawn(
                ElementSpawnBuilder.From(CreateArchetype(ElementCapabilities.None))).Handle;
            world.FactDispatched += CaptureFact;
            Assert.IsTrue(world.TrySetLifetime(
                handle.Key,
                new ElementLifetimeFeature(
                    1.5f,
                    ElementUpdateSchedule.UpdateSeconds(0f, ElementTimeDomain.Unscaled))));

            world.TickUpdateFeatures(new ElementUpdateContext(0f, 1f));

            Assert.AreEqual(0u, world.FixedStepIndex);
            Assert.IsTrue(world.TryGetLifetime(handle.Key, out ElementLifetimeFeature active));
            Assert.AreEqual(0.5f, active.RemainingUnits, 0.0001f);

            world.TickUpdateFeatures(new ElementUpdateContext(0f, 0.5f));

            Assert.IsFalse(handle.IsAlive);
            Assert.AreEqual(ElementFactType.LifetimeExpired, facts[0].Type);
            Assert.AreEqual(ElementFactType.Despawned, facts[1].Type);
        }



        [Test]
        public void LifetimeFeature_UpdateFrames_UsesOptionalLocalClock()
        {
            using var world = new ElementWorld(4);
            ElementHandle handle = world.Spawn(
                ElementSpawnBuilder.From(CreateArchetype(ElementCapabilities.None))).Handle;
            Assert.IsTrue(world.TrySetLocalClock(handle.Key, new ElementLocalClock(0.5f)));
            Assert.IsTrue(world.TrySetLifetime(
                handle.Key,
                new ElementLifetimeFeature(
                    3f,
                    ElementUpdateSchedule.UpdateFrames(1, ElementTimeDomain.Unscaled),
                    useLocalClock: true)));

            world.TickUpdateFeatures(new ElementUpdateContext(0f, 0.25f));
            Assert.IsTrue(world.TryGetLifetime(handle.Key, out ElementLifetimeFeature first));
            Assert.AreEqual(3f, first.RemainingUnits, 0.0001f);

            world.TickUpdateFeatures(new ElementUpdateContext(0f, 0.25f));
            Assert.IsTrue(world.TryGetLifetime(handle.Key, out ElementLifetimeFeature second));
            Assert.AreEqual(2f, second.RemainingUnits, 0.0001f);

            Assert.IsTrue(world.TrySetLocalClock(handle.Key, new ElementLocalClock(1f, paused: true)));
            world.TickUpdateFeatures(new ElementUpdateContext(0f, 10f));
            Assert.IsTrue(world.TryGetLifetime(handle.Key, out ElementLifetimeFeature paused));
            Assert.AreEqual(2f, paused.RemainingUnits, 0.0001f);
        }



        [Test]
        public void LocalClock_ScalesBodylessQueryMotion_ButNotDynamicSolver()
        {
            using var queryWorld = new ElementWorld(4);
            ElementCompiledArchetype queryArchetype = CreateArchetype(
                ElementCapabilities.KinematicMotion2D | ElementCapabilities.QuerySensor2D,
                radius: 0.1f);
            ElementHandle query = queryWorld.Spawn(
                ElementSpawnBuilder.From(queryArchetype).WithVelocity(new float2(2f, 0f))).Handle;
            Assert.IsTrue(queryWorld.TrySetLocalClock(query.Key, new ElementLocalClock(0.5f)));

            queryWorld.Tick(1f);

            Assert.IsTrue(query.TryGetSnapshot(out ElementSnapshot querySnapshot));
            Assert.AreEqual(1f, querySnapshot.Position.x, 0.0001f);

            using var dynamicWorld = new ElementWorld(4);
            ElementCompiledArchetype dynamicArchetype = CreateArchetype(
                ElementCapabilities.KinematicMotion2D | ElementCapabilities.DynamicBody2D,
                radius: 0.1f,
                physicsBodyMode: PhysicsCoreBodyMode.Dynamic);
            ElementSpawnBuilder dynamicBuilder = ElementSpawnBuilder.From(dynamicArchetype)
                .WithVelocity(new float2(2f, 0f))
                .WithPhysicsMaterial(new ElementPhysicsMaterial2D(1f, 0f, 0f, 0f));
            ElementHandle dynamicElement = dynamicWorld.Spawn(dynamicBuilder).Handle;
            Assert.IsTrue(dynamicWorld.TrySetLocalClock(
                dynamicElement.Key,
                new ElementLocalClock(0f, paused: true)));

            dynamicWorld.Tick(1f);

            Assert.IsTrue(dynamicElement.TryGetSnapshot(out ElementSnapshot dynamicSnapshot));
            Assert.AreEqual(2f, dynamicSnapshot.Position.x, 0.05f);
        }



        [Test]
        public void RotationCommand_UpdatesSnapshotAndInterpolatedRenderItem()
        {
            using var world = new ElementWorld(4);
            ElementCompiledArchetype archetype = CreateArchetype(
                ElementCapabilities.KinematicMotion2D | ElementCapabilities.SpriteVisual2D);
            ElementHandle handle = world.Spawn(
                ElementSpawnBuilder.From(archetype).WithRotation(math.radians(170f))).Handle;

            Assert.IsTrue(handle.TrySubmit(new SetElementRotation2D(math.radians(-170f))));
            world.FlushCommands();
            Assert.IsTrue(handle.TryGetSnapshot(out ElementSnapshot snapshot));
            Assert.AreEqual(math.radians(-170f), snapshot.RotationRadians, 0.0001f);

            Assert.IsTrue(world.TrySetPose(
                handle.Key,
                new ElementPose2D(
                    snapshot.Position,
                    snapshot.Position,
                    math.radians(170f),
                    math.radians(-170f),
                    snapshot.Scale,
                    snapshot.Scale)));
            Assert.IsTrue(world.TryGetRenderSnapshot(
                handle.Key,
                0.5f,
                out ElementRenderSnapshot renderSnapshot));
            Assert.AreEqual(math.radians(180f), renderSnapshot.RotationRadians, 0.0001f);
        }



        [Test]
        public void DirectionalAndWaveFeatures_UseSparseEntriesAndLocalClock()
        {
            using var world = new ElementWorld(4);
            ElementCompiledArchetype archetype = CreateArchetype(
                ElementCapabilities.KinematicMotion2D | ElementCapabilities.QuerySensor2D);
            ElementHandle handle = world.Spawn(
                ElementSpawnBuilder.From(archetype).WithVelocity(new float2(1f, 0f))).Handle;
            Assert.AreEqual(0, world.DirectionalMotionFeatureCount);
            Assert.AreEqual(0, world.WaveMotionFeatureCount);
            Assert.IsTrue(world.TrySetDirectionalMotion(
                handle.Key,
                new ElementDirectionalMotionFeature(
                    ElementDirectionalMotionMode.AngularTurn,
                    math.PI * 0.5f)));
            Assert.IsTrue(world.TrySetWaveMotion(
                handle.Key,
                new ElementWaveMotionFeature(1f, math.PI, 0f)));
            Assert.IsTrue(world.TrySetLocalClock(handle.Key, new ElementLocalClock(0.5f)));

            world.Tick(1f);

            Assert.IsTrue(handle.TryGetSnapshot(out ElementSnapshot snapshot));
            Assert.AreEqual(math.sqrt(0.5f), snapshot.Velocity.x, 0.0002f);
            Assert.AreEqual(math.sqrt(0.5f), snapshot.Velocity.y, 0.0002f);
            Assert.AreEqual(1, world.DirectionalMotionFeatureCount);
            Assert.AreEqual(1, world.WaveMotionFeatureCount);
            Assert.IsTrue(world.RemoveDirectionalMotion(handle.Key));
            Assert.IsTrue(world.RemoveWaveMotion(handle.Key));
            Assert.AreEqual(0, world.DirectionalMotionFeatureCount);
            Assert.AreEqual(0, world.WaveMotionFeatureCount);
        }



        [Test]
        public void HomingAndVelocityOrientation_TurnTowardPointAndRotateSprite()
        {
            using var world = new ElementWorld(4);
            ElementCompiledArchetype archetype = CreateArchetype(
                ElementCapabilities.KinematicMotion2D | ElementCapabilities.SpriteVisual2D);
            ElementHandle handle = world.Spawn(
                ElementSpawnBuilder.From(archetype).WithVelocity(new float2(1f, 0f))).Handle;
            Assert.IsTrue(world.TrySetDirectionalMotion(
                handle.Key,
                new ElementDirectionalMotionFeature(
                    ElementDirectionalMotionMode.HomingPoint,
                    homingPoint: new float2(0f, 10f),
                    maxTurnRadiansPerSecond: math.PI * 0.25f)));
            Assert.IsTrue(world.TrySetVisualOrientation(
                handle.Key,
                new ElementVisualOrientationFeature(
                    ElementVisualOrientationMode.Velocity,
                    axisOffsetRadians: math.PI * 0.5f)));

            world.Tick(1f);

            Assert.IsTrue(handle.TryGetSnapshot(out ElementSnapshot snapshot));
            Assert.AreEqual(math.PI * 0.25f, math.atan2(snapshot.Velocity.y, snapshot.Velocity.x), 0.0002f);
            Assert.AreEqual(math.PI * 0.75f, snapshot.RotationRadians, 0.0002f);
        }



        [Test]
        public void BoundaryFeature_CanWaitForFirstEntryThenDespawnOnExit()
        {
            using var world = new ElementWorld(4);
            world.FactDispatched += CaptureFact;
            world.SetBoundaryBounds2D(
                ElementBoundaryMode2D.ViewBounds,
                new ElementBounds2D(new float2(-1f), new float2(1f)));
            ElementHandle handle = world.Spawn(
                ElementSpawnBuilder.From(CreateArchetype(ElementCapabilities.KinematicMotion2D))
                    .WithPose(new float2(2f, 0f), new float2(1f))
                    .WithVelocity(new float2(-1f, 0f))).Handle;
            Assert.IsTrue(world.TrySetBoundary(
                handle.Key,
                new ElementBoundaryFeature(
                    ElementBoundaryMode2D.ViewBounds,
                    requireEnteredBeforeExit: true)));

            world.Tick(1f);
            Assert.IsTrue(handle.IsAlive);
            world.Tick(3f);

            Assert.IsFalse(handle.IsAlive);
            Assert.AreEqual(1, world.LastBoundaryExitCount);
            Assert.IsTrue(facts.Exists(fact => fact.Type == ElementFactType.BoundaryExited));
            Assert.AreEqual(0, world.BoundaryFeatureCount);
        }



        [Test]
        public void MovingProjectionReplacement_RequiresMonotonicSequenceAndCanClear()
        {
            var world = new ElementWorld(4);
            MovingProjectionShape2D moving = CreateMovingProjection(
                71,
                1,
                new float2(-1f, 0f),
                new float2(1f, 0f));

            world.PhysicsCoreLane.ReplaceMovingProjections(new[] { moving }, 2);

            Assert.AreEqual(1, world.PhysicsCoreLane.MovingProjectionCount);
            Assert.AreEqual(2, world.PhysicsCoreLane.MovingProjectionSynchronizationSequence);
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                world.PhysicsCoreLane.ReplaceMovingProjections(new[] { moving }, 1));

            world.PhysicsCoreLane.ReplaceMovingProjections(System.ReadOnlySpan<MovingProjectionShape2D>.Empty, 3);
            Assert.AreEqual(0, world.PhysicsCoreLane.MovingProjectionCount);

            PhysicsCore2DLane lane = world.PhysicsCoreLane;
            world.Dispose();
            Assert.Throws<System.ObjectDisposedException>(() =>
                lane.ReplaceMovingProjections(new[] { moving }, 4));
        }



        private void CaptureFact(in ElementFact fact) => facts.Add(fact);



        private static MovingProjectionShape2D CreateMovingProjection(
            int bridgeTargetId,
            int stableShapeId,
            float2 previous,
            float2 current,
            bool teleported = false)
        {
            CircleGeometry geometry = new CircleGeometry
            {
                center = Vector2.zero,
                radius = 0.2f
            };
            return new MovingProjectionShape2D
            {
                Shape = geometry.CreateShapeProxy(),
                PreviousTransform = new PhysicsTransform(
                    new Vector2(previous.x, previous.y),
                    PhysicsRotate.FromDegrees(0f)),
                CurrentTransform = new PhysicsTransform(
                    new Vector2(current.x, current.y),
                    PhysicsRotate.FromDegrees(0f)),
                BridgeTargetId = bridgeTargetId,
                StableShapeId = stableShapeId,
                CategoryMask = 1ul,
                ContactMask = ulong.MaxValue,
                Teleported = (byte)(teleported ? 1 : 0)
            };
        }



        private static ElementCompiledArchetype CreateArchetype(
            ElementCapabilities capabilities,
            float radius = 0.5f,
            PhysicsCoreShape2D physicsShape = PhysicsCoreShape2D.Circle,
            PhysicsCoreBodyMode physicsBodyMode = PhysicsCoreBodyMode.Kinematic,
            bool physicsIsTrigger = false,
            float periodicInterval = 0f,
            bool penetrating = false)
        {
            Assert.IsTrue(ElementCompiledArchetype.TryCreate(
                capabilities,
                radius,
                5f,
                0,
                periodicInterval,
                penetrating,
                physicsBodyMode,
                physicsShape,
                out ElementCompiledArchetype archetype,
                out ElementSpawnStatus failure,
                physicsIsTrigger), failure.ToString());
            return archetype;
        }
    }
}
