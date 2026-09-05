using System.Collections.Generic;
using NUnit.Framework;
using Pan.HighDensityElement;
using Unity.U2D.Physics;
using UnityEngine;



namespace Pan.HighDensityProjectile.Tests
{
    public sealed class ElementProjectileBodyEditModeTests
    {
        private sealed class ConsumingTarget : MonoBehaviour, IProjectileHitTarget
        {
            public int HitCount { get; private set; }



            public bool TryReceiveProjectileHit(in ProjectileHitPayload hit)
            {
                HitCount++;
                return true;
            }
        }



        private sealed class NonConsumingTarget : MonoBehaviour, IProjectileHitTarget
        {
            public int HitCount { get; private set; }



            public bool TryReceiveProjectileHit(in ProjectileHitPayload hit)
            {
                HitCount++;
                return false;
            }
        }



        private sealed class SingleResolvedTarget : IElementProjectileTargetResolver
        {
            private readonly int targetId;
            private readonly Collider2D collider;
            private readonly bool duplicateFirstReceiver;



            public SingleResolvedTarget(int targetId, Collider2D collider, bool duplicateFirstReceiver = false)
            {
                this.targetId = targetId;
                this.collider = collider;
                this.duplicateFirstReceiver = duplicateFirstReceiver;
            }



            public bool TryResolveTargets(
                in ElementFact fact,
                List<IProjectileHitTarget> receivers,
                out Component targetComponent,
                out Collider2D resolvedCollider)
            {
                receivers.Clear();
                resolvedCollider = fact.BridgeTargetId == targetId ? collider : null;
                targetComponent = resolvedCollider;
                if (resolvedCollider == null) { return false; }
                resolvedCollider.GetComponentsInParent(false, receivers);
                if (duplicateFirstReceiver && receivers.Count > 0)
                {
                    receivers.Add(receivers[0]);
                }
                return true;
            }
        }



        [Test]
        public void MotionSpec_MatchesExistingIntegrationContract()
        {
            using var body = new ElementProjectileBody(8);
            ProjectileMotionSpec motion = new ProjectileMotionSpec(
                true,
                new Vector3(0f, -10f, 0f),
                1f,
                0.5f);
            ProjectileSpawnRequest request = CreateRequest(
                Vector3.zero,
                new Vector3(10f, 0f, 0f),
                motion,
                ~0,
                false);
            Assert.IsTrue(body.TrySpawn(in request, out int projectileId));

            body.Simulate(0.5f);

            Assert.IsTrue(body.TryGetSnapshotById(projectileId, out ProjectileSnapshot snapshot));
            Vector3 expectedVelocity = motion.IntegrateVelocity(request.Velocity, 0.5f);
            Vector3 expectedPosition = expectedVelocity * 0.5f;
            Assert.That(snapshot.Velocity.x, Is.EqualTo(expectedVelocity.x).Within(0.0001f));
            Assert.That(snapshot.Velocity.y, Is.EqualTo(expectedVelocity.y).Within(0.0001f));
            Assert.That(snapshot.Position.x, Is.EqualTo(expectedPosition.x).Within(0.0001f));
            Assert.That(snapshot.Position.y, Is.EqualTo(expectedPosition.y).Within(0.0001f));
            Assert.That(snapshot.RemainingLifetime, Is.EqualTo(1.5f).Within(0.0001f));
        }


        [Test]
        public void PrepareFixedStep_DoesNotTickSharedElementWorld()
        {
            using var world = new ElementWorld(8, 1f);
            using var body = new ElementProjectileBody(world, 8);
            ProjectileSpawnRequest request = CreateRequest(
                Vector3.zero,
                Vector3.right * 4f,
                ProjectileMotionSpec.None,
                0,
                false);
            Assert.IsTrue(body.TrySpawn(in request, out int projectileId));

            body.PrepareFixedStep(7);

            Assert.IsTrue(body.TryGetSnapshotById(projectileId, out ProjectileSnapshot beforeTick));
            Assert.That(beforeTick.Position.x, Is.EqualTo(0f).Within(0.0001f));

            world.Tick(0.25f, 7);

            Assert.IsTrue(body.TryGetSnapshotById(projectileId, out ProjectileSnapshot afterTick));
            Assert.That(afterTick.Position.x, Is.EqualTo(1f).Within(0.0001f));
        }


        [Test]
        public void TriggerPolicy_ExcludedTriggerDoesNotDispatchOrConsume()
        {
            using var body = new ElementProjectileBody(8);
            var targetObject = new GameObject("PhysicsCoreExcludedTriggerTarget") { layer = 3 };
            PhysicsBody coreTarget = default;
            try
            {
                targetObject.transform.position = new Vector3(5f, 0f, 0f);
                Collider2D collider = targetObject.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                ConsumingTarget target = targetObject.AddComponent<ConsumingTarget>();
                coreTarget = CreateCoreTarget(body, targetObject.transform.position, targetObject.layer, 107);
                body.SetTargetResolver(new SingleResolvedTarget(107, collider));

                ProjectileSpawnRequest request = CreateRequest(
                    Vector3.zero,
                    new Vector3(20f, 0f, 0f),
                    ProjectileMotionSpec.None,
                    1 << 3,
                    true);
                request.Collision.IncludeTriggers = false;
                Assert.IsTrue(body.TrySpawn(in request, out int projectileId));

                body.SimulateFixedStep(0.5f, 1);

                Assert.AreEqual(0, target.HitCount);
                Assert.AreEqual(1, body.ActiveCount);
                Assert.IsTrue(body.TryGetSnapshotById(projectileId, out _));
            }
            finally
            {
                if (coreTarget.isValid) { coreTarget.Destroy(); }
                Object.DestroyImmediate(targetObject);
            }
        }


        [Test]
        public void PhysicsCoreShapeCast_DuplicateReceiverInstanceIsDispatchedOnce()
        {
            using var body = new ElementProjectileBody(8);
            var targetObject = new GameObject("PhysicsCoreDuplicateReceiverTarget") { layer = 3 };
            PhysicsBody coreTarget = default;
            try
            {
                targetObject.transform.position = new Vector3(5f, 0f, 0f);
                Collider2D collider = targetObject.AddComponent<BoxCollider2D>();
                NonConsumingTarget target = targetObject.AddComponent<NonConsumingTarget>();
                coreTarget = CreateCoreTarget(body, targetObject.transform.position, targetObject.layer, 108);
                body.SetTargetResolver(new SingleResolvedTarget(108, collider, duplicateFirstReceiver: true));
                ProjectileSpawnRequest request = CreateRequest(
                    Vector3.zero,
                    new Vector3(20f, 0f, 0f),
                    ProjectileMotionSpec.None,
                    1 << 3,
                    false);
                Assert.IsTrue(body.TrySpawn(in request, out _));

                body.SimulateFixedStep(0.5f, 1);

                Assert.AreEqual(1, target.HitCount);
            }
            finally
            {
                if (coreTarget.isValid) { coreTarget.Destroy(); }
                Object.DestroyImmediate(targetObject);
            }
        }


        [Test]
        public void PhysicsCoreShapeCast_SourceHierarchyIsIgnored()
        {
            using var body = new ElementProjectileBody(8);
            var sourceObject = new GameObject("PhysicsCoreSourceIgnoreTarget") { layer = 3 };
            PhysicsBody coreTarget = default;
            try
            {
                sourceObject.transform.position = new Vector3(5f, 0f, 0f);
                Collider2D collider = sourceObject.AddComponent<BoxCollider2D>();
                ConsumingTarget target = sourceObject.AddComponent<ConsumingTarget>();
                coreTarget = CreateCoreTarget(body, sourceObject.transform.position, sourceObject.layer, 109);
                body.SetTargetResolver(new SingleResolvedTarget(109, collider));
                ProjectileSpawnRequest request = CreateRequest(
                    Vector3.zero,
                    new Vector3(20f, 0f, 0f),
                    ProjectileMotionSpec.None,
                    1 << 3,
                    true);
                request.Source = sourceObject.transform;
                Assert.IsTrue(body.TrySpawn(in request, out int projectileId));

                body.SimulateFixedStep(0.5f, 1);

                Assert.AreEqual(0, target.HitCount);
                Assert.IsTrue(body.TryGetSnapshotById(projectileId, out _));
            }
            finally
            {
                if (coreTarget.isValid) { coreTarget.Destroy(); }
                Object.DestroyImmediate(sourceObject);
            }
        }


        [Test]
        public void ElementHandle_StrictCcdAndTeleportCommandsAreAccepted()
        {
            using var body = new ElementProjectileBody(8);
            ProjectileSpawnRequest request = CreateRequest(
                Vector3.zero,
                Vector3.right,
                ProjectileMotionSpec.None,
                0,
                false);
            request.StrictCcdOverride = StrictCcdOverride2D.ForceOff;
            Assert.IsTrue(body.TrySpawn(in request, out int projectileId));
            Assert.IsTrue(body.TryGetElementHandle(projectileId, out ElementProjectileHandle handle));

            Assert.IsTrue(handle.RequestStrictCcd(2));
            Assert.IsTrue(handle.MarkTeleported());
        }



        [Test]
        public void PhysicsCoreShapeCast_ReceiverConsumesAndDespawnsInSameSubstep()
        {
            using var body = new ElementProjectileBody(8);
            var targetObject = new GameObject("PhysicsCoreShapeCastTarget") { layer = 3 };
            PhysicsBody coreTarget = default;
            try
            {
                targetObject.transform.position = new Vector3(5f, 0f, 0f);
                Collider2D collider = targetObject.AddComponent<BoxCollider2D>();
                ConsumingTarget target = targetObject.AddComponent<ConsumingTarget>();
                coreTarget = CreateCoreTarget(body, targetObject.transform.position, targetObject.layer, 101);
                body.SetTargetResolver(new SingleResolvedTarget(101, collider));

                int contactCount = 0;
                int despawnCount = 0;
                bool consumed = false;
                body.ProjectileContacted += (in ProjectileContactPayload contact) =>
                {
                    contactCount++;
                    consumed = contact.Consumed;
                };
                body.ProjectileDespawned += (in ProjectileSnapshot _) => despawnCount++;

                ProjectileSpawnRequest request = CreateRequest(
                    Vector3.zero,
                    new Vector3(20f, 0f, 0f),
                    ProjectileMotionSpec.None,
                    1 << 3,
                    false);
                Assert.IsTrue(body.TrySpawn(in request, out int projectileId));
                Assert.IsTrue(body.TryGetElementHandle(projectileId, out ElementProjectileHandle handle));

                body.SimulateFixedStep(0.5f, 11);

                Assert.AreEqual(1, target.HitCount);
                Assert.AreEqual(1, contactCount);
                Assert.IsTrue(consumed);
                Assert.AreEqual(1, despawnCount);
                Assert.AreEqual(0, body.ActiveCount);
                Assert.IsFalse(handle.IsAlive);
                Assert.IsFalse(body.TryGetSnapshotById(projectileId, out _));
            }
            finally
            {
                if (coreTarget.isValid) { coreTarget.Destroy(); }
                Object.DestroyImmediate(targetObject);
            }
        }



        [Test]
        public void PhysicsCoreShapeCast_InvokesAllUniqueReceiversBeforeConsumption()
        {
            using var body = new ElementProjectileBody(8);
            var targetObject = new GameObject("PhysicsCoreMultipleReceiverTarget") { layer = 3 };
            PhysicsBody coreTarget = default;
            try
            {
                targetObject.transform.position = new Vector3(5f, 0f, 0f);
                Collider2D collider = targetObject.AddComponent<BoxCollider2D>();
                ConsumingTarget consuming = targetObject.AddComponent<ConsumingTarget>();
                NonConsumingTarget observing = targetObject.AddComponent<NonConsumingTarget>();
                coreTarget = CreateCoreTarget(body, targetObject.transform.position, targetObject.layer, 105);
                body.SetTargetResolver(new SingleResolvedTarget(105, collider));
                int dispatchedTargetCount = 0;
                body.ProjectileContacted += (in ProjectileContactPayload contact) =>
                    dispatchedTargetCount = contact.TargetCount;
                ProjectileSpawnRequest request = CreateRequest(
                    Vector3.zero,
                    new Vector3(20f, 0f, 0f),
                    ProjectileMotionSpec.None,
                    1 << 3,
                    false);
                Assert.IsTrue(body.TrySpawn(in request, out _));

                body.SimulateFixedStep(0.5f, 1);

                Assert.AreEqual(1, consuming.HitCount);
                Assert.AreEqual(1, observing.HitCount);
                Assert.AreEqual(2, dispatchedTargetCount);
                Assert.AreEqual(0, body.ActiveCount);
                Assert.AreEqual(1, body.TotalHits);
            }
            finally
            {
                if (coreTarget.isValid) { coreTarget.Destroy(); }
                Object.DestroyImmediate(targetObject);
            }
        }



        [Test]
        public void PhysicsCoreShapeCast_FalseReceiverDoesNotBecomeBlockingColliderPolicy()
        {
            using var body = new ElementProjectileBody(8);
            var targetObject = new GameObject("PhysicsCoreFalseReceiverTarget") { layer = 3 };
            PhysicsBody coreTarget = default;
            try
            {
                targetObject.transform.position = new Vector3(5f, 0f, 0f);
                Collider2D collider = targetObject.AddComponent<BoxCollider2D>();
                NonConsumingTarget target = targetObject.AddComponent<NonConsumingTarget>();
                coreTarget = CreateCoreTarget(body, targetObject.transform.position, targetObject.layer, 106);
                body.SetTargetResolver(new SingleResolvedTarget(106, collider));
                ProjectileSpawnRequest request = CreateRequest(
                    Vector3.zero,
                    new Vector3(20f, 0f, 0f),
                    ProjectileMotionSpec.None,
                    1 << 3,
                    true);
                Assert.IsTrue(body.TrySpawn(in request, out int projectileId));

                body.SimulateFixedStep(0.5f, 1);

                Assert.AreEqual(1, target.HitCount);
                Assert.AreEqual(1, body.ActiveCount);
                Assert.AreEqual(0, body.TotalHits);
                Assert.IsTrue(body.TryGetSnapshotById(projectileId, out _));
            }
            finally
            {
                if (coreTarget.isValid) { coreTarget.Destroy(); }
                Object.DestroyImmediate(targetObject);
            }
        }



        [Test]
        public void PhysicsCoreShapeCast_ConsumeWithoutReceiverPreservesLegacyPolicy()
        {
            using var body = new ElementProjectileBody(8);
            var targetObject = new GameObject("PhysicsCoreBlockingTarget") { layer = 3 };
            PhysicsBody coreTarget = default;
            try
            {
                targetObject.transform.position = new Vector3(5f, 0f, 0f);
                Collider2D collider = targetObject.AddComponent<BoxCollider2D>();
                coreTarget = CreateCoreTarget(body, targetObject.transform.position, targetObject.layer, 102);
                body.SetTargetResolver(new SingleResolvedTarget(102, collider));

                bool consumed = false;
                body.ProjectileContacted += (in ProjectileContactPayload contact) => consumed |= contact.Consumed;
                ProjectileSpawnRequest request = CreateRequest(
                    Vector3.zero,
                    new Vector3(20f, 0f, 0f),
                    ProjectileMotionSpec.None,
                    1 << 3,
                    true);
                Assert.IsTrue(body.TrySpawn(in request, out _));

                body.Simulate(0.5f);

                Assert.IsTrue(consumed);
                Assert.AreEqual(0, body.ActiveCount);
                Assert.AreEqual(1, body.TotalHits);
            }
            finally
            {
                if (coreTarget.isValid) { coreTarget.Destroy(); }
                Object.DestroyImmediate(targetObject);
            }
        }



        [Test]
        public void PhysicsCoreShapeCast_NonConsumingColliderDoesNotDespawn()
        {
            using var body = new ElementProjectileBody(8);
            var targetObject = new GameObject("PhysicsCorePassThroughTarget") { layer = 3 };
            PhysicsBody coreTarget = default;
            try
            {
                targetObject.transform.position = new Vector3(5f, 0f, 0f);
                Collider2D collider = targetObject.AddComponent<BoxCollider2D>();
                coreTarget = CreateCoreTarget(body, targetObject.transform.position, targetObject.layer, 103);
                body.SetTargetResolver(new SingleResolvedTarget(103, collider));
                ProjectileSpawnRequest request = CreateRequest(
                    Vector3.zero,
                    new Vector3(20f, 0f, 0f),
                    ProjectileMotionSpec.None,
                    1 << 3,
                    false);
                Assert.IsTrue(body.TrySpawn(in request, out int projectileId));

                body.Simulate(0.5f);

                Assert.AreEqual(1, body.ActiveCount);
                Assert.AreEqual(0, body.TotalHits);
                Assert.IsTrue(body.TryGetSnapshotById(projectileId, out _));
            }
            finally
            {
                if (coreTarget.isValid) { coreTarget.Destroy(); }
                Object.DestroyImmediate(targetObject);
            }
        }



        [Test]
        public void DynamicCoreProjectile_StopsWithCcdAndConsumesConfirmedContact()
        {
            var material = new ElementPhysicsMaterial2D(
                density: 1.5f,
                friction: 0.35f,
                bounciness: 0.8f,
                gravityScale: 0f);
            var options = new SimulatedProjectileOptions2D(PhysicsCoreShape2D.Box, in material);
            using var body = new ElementProjectileBody(
                ElementProjectileExecutionModel.Simulated,
                capacity: 8,
                simulatedOptions: options);
            var targetObject = new GameObject("DynamicCoreProjectileTarget") { layer = 3 };
            PhysicsBody coreTarget = default;
            try
            {
                targetObject.transform.position = new Vector3(2f, 0f, 0f);
                Collider2D collider = targetObject.AddComponent<BoxCollider2D>();
                coreTarget = CreateCoreTarget(body, targetObject.transform.position, targetObject.layer, 104);
                body.SetTargetResolver(new SingleResolvedTarget(104, collider));
                ProjectileSpawnRequest request = CreateRequest(
                    Vector3.zero,
                    new Vector3(20f, 0f, 0f),
                    ProjectileMotionSpec.None,
                    1 << 3,
                    true);

                Assert.IsTrue(body.TrySpawn(in request, out int projectileId));
                Assert.IsTrue(body.TryGetElementHandle(projectileId, out ElementProjectileHandle handle));
                Assert.AreEqual(ElementProjectileExecutionModel.Simulated, body.ExecutionModel);
                Assert.AreEqual(ElementPhysicsCapability.DynamicBody2D, handle.Element.PhysicsCapabilities);
                Assert.AreEqual(1, body.ElementWorld.PhysicsCoreLane.BodyCount);
                Assert.IsTrue(
                    body.ElementWorld.PhysicsCoreLane.TryGetBody(handle.Element.Key, out PhysicsBody dynamicBody));

                body.SimulateFixedStep(0.2f, 1);

                Assert.Less(dynamicBody.position.x, targetObject.transform.position.x);
                if (body.ActiveCount > 0) { body.SimulateFixedStep(0.02f, 2); }

                string diagnostics = body.ActiveCount == 0
                    ? string.Empty
                    : $"position={dynamicBody.position}, velocity={dynamicBody.linearVelocity}, " +
                      $"contactBegin={body.ElementWorld.PhysicsCoreLane.World.contactBeginEvents.Length}, " +
                      $"contactHit={body.ElementWorld.PhysicsCoreLane.World.contactHitEvents.Length}, " +
                      $"hitThreshold={body.ElementWorld.PhysicsCoreLane.World.contactHitEventThreshold}, " +
                      $"facts={body.ElementWorld.LastFactCount}, hits={body.TotalHits}";
                Assert.AreEqual(0, body.ActiveCount, diagnostics);
                Assert.AreEqual(1, body.TotalHits);
                Assert.IsFalse(handle.IsAlive);
            }
            finally
            {
                if (coreTarget.isValid) { coreTarget.Destroy(); }
                Object.DestroyImmediate(targetObject);
            }
        }



        private static PhysicsBody CreateCoreTarget(
            ElementProjectileBody projectileBody,
            Vector3 position,
            int layer,
            int bridgeTargetId)
        {
            PhysicsBodyDefinition bodyDefinition = PhysicsBodyDefinition.defaultDefinition;
            bodyDefinition.type = PhysicsBody.BodyType.Static;
            bodyDefinition.position = new Vector2(position.x, position.y);
            bodyDefinition.transformWriteMode = PhysicsBody.TransformWriteMode.Off;
            PhysicsBody target = projectileBody.ElementWorld.PhysicsCoreLane.World.CreateBody(bodyDefinition);

            PhysicsShapeDefinition shapeDefinition = PhysicsShapeDefinition.defaultDefinition;
            shapeDefinition.contactEvents = true;
            shapeDefinition.triggerEvents = true;
            PhysicsShape.ContactFilter filter = PhysicsShape.ContactFilter.defaultFilter;
            filter.categories = new PhysicsMask { bitMask = 1ul << layer };
            filter.contacts = new PhysicsMask { bitMask = ulong.MaxValue };
            shapeDefinition.contactFilter = filter;
            PhysicsShape shape = target.CreateShape(
                PolygonGeometry.CreateBox(Vector2.one, 0f, true),
                shapeDefinition);
            PhysicsCore2DLane.TagBridgeTarget(shape, bridgeTargetId);
            return target;
        }



        private static ProjectileSpawnRequest CreateRequest(
            Vector3 position,
            Vector3 velocity,
            ProjectileMotionSpec motion,
            LayerMask hitLayers,
            bool consumeHitWithoutReceiver)
        {
            return new ProjectileSpawnRequest
            {
                Kinematic = new KinematicCircle2DState(position, velocity, 0.1f),
                Lifetime = TimedLifetimeState.Start(2f),
                Motion = motion,
                Collision = new CollisionLayerFilter(0, hitLayers, consumeHitWithoutReceiver),
                Team = new TeamRelationTag(1),
                Hit = new HitDamageSpec(3f),
                Visual = new SpriteVisualSpec(null, 0, 1f)
            };
        }
    }
}
