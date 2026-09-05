using System.Collections.Generic;
using NUnit.Framework;
using Unity.Mathematics;
using Unity.U2D.Physics;
using UnityEngine;



namespace Pan.HighDensityElement.Physics2DBridge.Tests
{
    internal sealed class RecordingElementPhysicsFactReceiver2D : MonoBehaviour, IElementPhysicsFactReceiver2D
    {
        public int ReceiveCount { get; private set; }
        public ElementPhysicsFact2D LastFact { get; private set; }



        public void ReceiveElementPhysicsFact2D(in ElementPhysicsFact2D fact)
        {
            ReceiveCount++;
            LastFact = fact;
        }
    }



    public sealed class Physics2DBridgeRegistryEditModeTests
    {
        private sealed class BoxShapeProvider : IPhysics2DBridgeShapeProvider
        {
            private readonly Component target;
            private readonly Vector2 position;
            private readonly Vector2 size;



            public BoxShapeProvider(Component target, Vector2 position, Vector2 size)
            {
                this.target = target;
                this.position = position;
                this.size = size;
            }



            public Component TargetComponent => target;
            public int GeometryRevision => 1;



            public bool TryGetBodyState(out Physics2DBridgeBodyState state)
            {
                state = new Physics2DBridgeBodyState(position, 0f, Vector2.zero, 0f, true);
                return true;
            }



            public int CopyShapes(
                List<Physics2DBridgeShapeDescriptor> shapes,
                List<Vector2> vertices)
            {
                shapes.Clear();
                vertices.Clear();
                shapes.Add(Physics2DBridgeShapeDescriptor.Box(Vector2.zero, size, 0f, target.gameObject.layer));
                return 1;
            }
        }



        private sealed class NotifyingBoxShapeProvider :
            IPhysics2DBridgeShapeProvider,
            IPhysics2DBridgeChangeSource
        {
            private readonly Component target;
            private Vector2 size;



            public NotifyingBoxShapeProvider(Component target, Vector2 size)
            {
                this.target = target;
                this.size = size;
            }



            public event System.Action GeometryChanged;
            public event System.Action StateChanged;

            public Component TargetComponent => target;
            public int GeometryRevision => 1;
            public int CopyCount { get; private set; }
            public int GeometrySubscriberCount => GeometryChanged?.GetInvocationList().Length ?? 0;
            public int StateSubscriberCount => StateChanged?.GetInvocationList().Length ?? 0;



            public bool TryGetBodyState(out Physics2DBridgeBodyState state)
            {
                state = new Physics2DBridgeBodyState(
                    target.transform.position,
                    target.transform.eulerAngles.z,
                    new Vector2(100f, 0f),
                    360f,
                    target.gameObject.activeInHierarchy);
                return true;
            }



            public int CopyShapes(
                List<Physics2DBridgeShapeDescriptor> shapes,
                List<Vector2> vertices)
            {
                CopyCount++;
                shapes.Clear();
                vertices.Clear();
                shapes.Add(Physics2DBridgeShapeDescriptor.Box(
                    Vector2.zero,
                    size,
                    0f,
                    target.gameObject.layer));
                return 1;
            }



            public void SetSize(Vector2 value)
            {
                size = value;
                GeometryChanged?.Invoke();
            }



            public void NotifyStateChanged() => StateChanged?.Invoke();
        }



        private readonly List<ElementFact> facts = new List<ElementFact>();



        [SetUp]
        public void SetUp() => facts.Clear();



        [Test]
        public void StaticSceneScan_MirrorsColliderAndProducesBridgeContact()
        {
            using var world = new ElementWorld(8);
            Physics2DBridgeSettings settings = CreateSettings(31);
            var targetObject = new GameObject("StaticPhysics2DBridgeTarget") { layer = 31 };
            try
            {
                targetObject.transform.position = new Vector3(5f, 0f, 0f);
                Collider2D targetCollider = targetObject.AddComponent<BoxCollider2D>();
                using var registry = new Physics2DBridgeRegistry(world.PhysicsCoreLane, settings);
                world.FactDispatched += OnFact;

                Assert.AreEqual(1, registry.RegisterStaticSceneColliders());
                Assert.AreEqual(1, registry.TargetCount);
                Assert.AreEqual(1, registry.BodyCount);
                ElementHandle projectile = SpawnQueryProjectile(world, Vector2.zero, new Vector2(20f, 0f));
                facts.Clear();

                world.Tick(0.5f, 3);

                ElementFact contact = facts.Find(fact =>
                    fact.Type == ElementFactType.Contact && fact.Element == projectile.Key);
                Assert.Greater(contact.BridgeTargetId, 0);
                Assert.AreEqual(3, contact.SubstepIndex);
                Assert.IsTrue(registry.TryGetCollider(contact.BridgeTargetId, out Collider2D resolved));
                Assert.AreSame(targetCollider, resolved);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(settings);
            }
        }



        [Test]
        public void DynamicComponent_LegacyPoseIsAuthoritativeForCoreProxy()
        {
            using var world = new ElementWorld(8);
            Physics2DBridgeSettings settings = CreateSettings(30);
            using var registry = new Physics2DBridgeRegistry(world.PhysicsCoreLane, settings);
            var targetObject = new GameObject("DynamicPhysics2DBridgeTarget") { layer = 30 };
            try
            {
                Rigidbody2D rigidbody = targetObject.AddComponent<Rigidbody2D>();
                rigidbody.bodyType = RigidbodyType2D.Kinematic;
                targetObject.AddComponent<CircleCollider2D>();
                HighDensityPhysicsBridge2D bridge = targetObject.AddComponent<HighDensityPhysicsBridge2D>();
                if (registry.TargetCount == 0) { Assert.IsTrue(registry.Register(bridge)); }
                registry.SynchronizePoses();
                Assert.AreEqual(1, registry.TargetCount);

                rigidbody.position = new Vector2(5f, 0f);
                registry.SynchronizePoses();
                world.FactDispatched += OnFact;
                ElementHandle first = SpawnQueryProjectile(world, Vector2.zero, new Vector2(20f, 0f));
                facts.Clear();
                world.Tick(0.5f);
                Assert.That(facts.Exists(fact =>
                    fact.Type == ElementFactType.Contact && fact.Element == first.Key), Is.True);

                rigidbody.position = new Vector2(20f, 0f);
                registry.SynchronizePoses();
                ElementHandle second = SpawnQueryProjectile(
                    world,
                    new Vector2(15f, 0f),
                    new Vector2(20f, 0f));
                facts.Clear();
                world.Tick(0.5f);
                Assert.That(facts.Exists(fact =>
                    fact.Type == ElementFactType.Contact && fact.Element == second.Key), Is.True);
                Assert.That(rigidbody.position.x, Is.EqualTo(20f).Within(0.0001f));
                Assert.That(targetObject.transform.position.x, Is.EqualTo(0f).Within(0.0001f),
                    "Core proxy는 legacy Transform으로 pose를 write-back하지 않아야 합니다.");
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(settings);
            }
        }



        [Test]
        public void SingleComponent_GroupsChildCollidersByAttachedRigidbody()
        {
            using var world = new ElementWorld(8);
            Physics2DBridgeSettings settings = CreateSettings(29);
            using var registry = new Physics2DBridgeRegistry(world, settings);
            var root = new GameObject("MultiBodyBridgeRoot") { layer = 29 };
            var child = new GameObject("ChildBody") { layer = 29 };
            child.transform.SetParent(root.transform, false);
            try
            {
                root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                root.AddComponent<CircleCollider2D>();
                child.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                child.AddComponent<BoxCollider2D>();
                HighDensityPhysicsBridge2D bridge = root.AddComponent<HighDensityPhysicsBridge2D>();
                Assert.IsTrue(bridge.Configure(registry));

                Assert.IsTrue(bridge.RegistrationHandle.IsValid);
                Assert.IsTrue(registry.IsRegistered(bridge.RegistrationHandle));
                Assert.AreEqual(2, registry.BodyCount);
                Assert.AreEqual(2, registry.TargetCount);

                Physics2DBridgeRegistrationHandle stale = bridge.RegistrationHandle;
                Assert.IsTrue(registry.Unregister(stale));
                Assert.IsFalse(registry.IsRegistered(stale));
                Assert.IsTrue(registry.TryRegister(bridge, out Physics2DBridgeRegistrationHandle current));
                Assert.IsTrue(registry.IsRegistered(current));
                Assert.AreNotEqual(stale.Generation, current.Generation);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(settings);
            }
        }



        [Test]
        public void ColliderlessProvider_ProducesResolvableBridgeContact()
        {
            using var world = new ElementWorld(8);
            Physics2DBridgeSettings settings = CreateSettings(28);
            using var registry = new Physics2DBridgeRegistry(world, settings);
            var targetObject = new GameObject("ProviderBridgeTarget") { layer = 28 };
            try
            {
                HighDensityPhysicsBridge2D bridge = targetObject.AddComponent<HighDensityPhysicsBridge2D>();
                var provider = new BoxShapeProvider(targetObject.transform, new Vector2(5f, 0f), Vector2.one);
                Assert.IsTrue(bridge.Configure(registry, provider));
                Physics2DBridgeRegistrationHandle firstHandle = bridge.RegistrationHandle;
                Assert.IsTrue(bridge.Configure(registry, provider));
                Assert.AreEqual(firstHandle, bridge.RegistrationHandle);
                world.FactDispatched += OnFact;
                ElementHandle projectile = SpawnQueryProjectile(world, Vector2.zero, new Vector2(20f, 0f));
                facts.Clear();

                world.Tick(0.5f, 7);

                ElementFact contact = facts.Find(fact =>
                    fact.Type == ElementFactType.Contact && fact.Element == projectile.Key);
                Assert.Greater(contact.BridgeTargetId, 0);
                Assert.IsTrue(registry.TryGetTarget(contact.BridgeTargetId, out Physics2DBridgeTarget target));
                Assert.AreSame(targetObject.transform, target.Owner);
                Assert.IsNull(target.Collider);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(settings);
            }
        }



        [Test]
        public void CollidersThenProvider_UsesCapsuleGeometryIncludingOffset()
        {
            using var world = new ElementWorld(8);
            Physics2DBridgeSettings settings = CreateSettings(20);
            using var registry = new Physics2DBridgeRegistry(world, settings);
            var targetObject = new GameObject("OffsetCapsuleBridgeTarget") { layer = 20 };
            try
            {
                Rigidbody2D rigidbody = targetObject.AddComponent<Rigidbody2D>();
                rigidbody.bodyType = RigidbodyType2D.Kinematic;
                CapsuleCollider2D capsule = targetObject.AddComponent<CapsuleCollider2D>();
                capsule.direction = CapsuleDirection2D.Vertical;
                capsule.size = new Vector2(1f, 3.45f);
                capsule.offset = new Vector2(0f, 3.225f);

                HighDensityPhysicsBridge2D bridge = targetObject.AddComponent<HighDensityPhysicsBridge2D>();
                var fallbackProvider = new BoxShapeProvider(
                    targetObject.transform,
                    Vector2.zero,
                    new Vector2(1f, 4.95f));
                Assert.IsTrue(bridge.Configure(
                    registry,
                    fallbackProvider,
                    Physics2DBridgeShapeSourceMode.CollidersThenProvider));
                registry.SynchronizePoses();

                world.FactDispatched += OnFact;
                ElementHandle capsuleHit = SpawnQueryProjectile(
                    world,
                    new Vector2(-2f, capsule.offset.y),
                    new Vector2(8f, 0f));
                facts.Clear();
                world.Tick(0.5f);

                ElementFact contact = facts.Find(fact =>
                    fact.Type == ElementFactType.Contact && fact.Element == capsuleHit.Key);
                Assert.Greater(contact.BridgeTargetId, 0);
                Assert.IsTrue(registry.TryGetCollider(contact.BridgeTargetId, out Collider2D resolved));
                Assert.AreSame(capsule, resolved);

                ElementHandle providerOnlyArea = SpawnQueryProjectile(
                    world,
                    new Vector2(-2f, 0f),
                    new Vector2(8f, 0f));
                facts.Clear();
                world.Tick(0.5f);

                Assert.IsFalse(facts.Exists(fact =>
                    fact.Type == ElementFactType.Contact && fact.Element == providerOnlyArea.Key));
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(settings);
            }
        }



        [Test]
        public void ColliderGeometryChanges_AreProjectedAtNextSynchronizationWithoutManualDirtyCall()
        {
            using var world = new ElementWorld(8);
            Physics2DBridgeSettings settings = CreateSettings(20);
            using var registry = new Physics2DBridgeRegistry(world, settings);
            var targetObject = new GameObject("RuntimeCapsuleProjectionTarget") { layer = 20 };
            try
            {
                Rigidbody2D rigidbody = targetObject.AddComponent<Rigidbody2D>();
                rigidbody.bodyType = RigidbodyType2D.Kinematic;
                CapsuleCollider2D capsule = targetObject.AddComponent<CapsuleCollider2D>();
                capsule.direction = CapsuleDirection2D.Vertical;
                capsule.size = new Vector2(1f, 2f);

                HighDensityPhysicsBridge2D bridge = targetObject.AddComponent<HighDensityPhysicsBridge2D>();
                Assert.IsTrue(bridge.Configure(
                    registry,
                    null,
                    Physics2DBridgeShapeSourceMode.CollidersOnly));
                registry.SynchronizePoses();

                capsule.offset = new Vector2(0f, 4f);
                capsule.size = new Vector2(1.5f, 3f);
                registry.SynchronizePoses();

                world.FactDispatched += OnFact;
                ElementHandle oldArea = SpawnQueryProjectile(
                    world,
                    new Vector2(-2f, 0f),
                    new Vector2(8f, 0f));
                facts.Clear();
                world.Tick(0.5f);
                Assert.IsFalse(facts.Exists(fact =>
                    fact.Type == ElementFactType.Contact && fact.Element == oldArea.Key));

                ElementHandle movedArea = SpawnQueryProjectile(
                    world,
                    new Vector2(-2f, capsule.offset.y),
                    new Vector2(8f, 0f));
                facts.Clear();
                world.Tick(0.5f);

                ElementFact contact = facts.Find(fact =>
                    fact.Type == ElementFactType.Contact && fact.Element == movedArea.Key);
                Assert.Greater(contact.BridgeTargetId, 0);
                Assert.IsTrue(registry.TryGetCollider(contact.BridgeTargetId, out Collider2D resolved));
                Assert.AreSame(capsule, resolved);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(settings);
            }
        }



        [Test]
        public void ColliderDisableAndReenable_UpdatesProjectionAtSynchronizationBoundary()
        {
            using var world = new ElementWorld(8);
            Physics2DBridgeSettings settings = CreateSettings(20);
            using var registry = new Physics2DBridgeRegistry(world, settings);
            var targetObject = new GameObject("RuntimeColliderStateProjectionTarget") { layer = 20 };
            try
            {
                BoxCollider2D collider = targetObject.AddComponent<BoxCollider2D>();
                HighDensityPhysicsBridge2D bridge = targetObject.AddComponent<HighDensityPhysicsBridge2D>();
                Assert.IsTrue(bridge.Configure(registry));
                registry.SynchronizePoses();
                world.FactDispatched += OnFact;

                collider.enabled = false;
                registry.SynchronizePoses();
                ElementHandle disabledArea = SpawnQueryProjectile(
                    world,
                    new Vector2(-2f, 0f),
                    new Vector2(8f, 0f));
                facts.Clear();
                world.Tick(0.5f);
                Assert.IsFalse(facts.Exists(fact =>
                    fact.Type == ElementFactType.Contact && fact.Element == disabledArea.Key));

                collider.enabled = true;
                registry.SynchronizePoses();
                ElementHandle enabledArea = SpawnQueryProjectile(
                    world,
                    new Vector2(-2f, 0f),
                    new Vector2(8f, 0f));
                facts.Clear();
                world.Tick(0.5f);
                Assert.IsTrue(facts.Exists(fact =>
                    fact.Type == ElementFactType.Contact && fact.Element == enabledArea.Key));
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(settings);
            }
        }



        [Test]
        public void CollidersThenProvider_FallsBackWhenColliderIsUnavailable()
        {
            using var world = new ElementWorld(8);
            Physics2DBridgeSettings settings = CreateSettings(19);
            using var registry = new Physics2DBridgeRegistry(world, settings);
            var targetObject = new GameObject("FallbackProviderBridgeTarget") { layer = 19 };
            try
            {
                HighDensityPhysicsBridge2D bridge = targetObject.AddComponent<HighDensityPhysicsBridge2D>();
                var fallbackProvider = new BoxShapeProvider(
                    targetObject.transform,
                    new Vector2(5f, 0f),
                    Vector2.one);
                Assert.IsTrue(bridge.Configure(
                    registry,
                    fallbackProvider,
                    Physics2DBridgeShapeSourceMode.CollidersThenProvider));

                world.FactDispatched += OnFact;
                ElementHandle projectile = SpawnQueryProjectile(
                    world,
                    Vector2.zero,
                    new Vector2(20f, 0f));
                facts.Clear();
                world.Tick(0.5f);

                ElementFact contact = facts.Find(fact =>
                    fact.Type == ElementFactType.Contact && fact.Element == projectile.Key);
                Assert.Greater(contact.BridgeTargetId, 0);
                Assert.IsTrue(registry.TryGetTarget(contact.BridgeTargetId, out Physics2DBridgeTarget target));
                Assert.IsNull(target.Collider);
                Assert.AreSame(targetObject.transform, target.Owner);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(settings);
            }
        }



        [Test]
        public void SelectedColliders_RegistersOnlyTheExplicitGameplayShape()
        {
            using var world = new ElementWorld(8);
            Physics2DBridgeSettings settings = CreateSettings(18);
            using var registry = new Physics2DBridgeRegistry(world, settings);
            var root = new GameObject("SelectedColliderBridgeRoot") { layer = 18 };
            var child = new GameObject("SelectedGameplayCollider") { layer = 18 };
            child.transform.SetParent(root.transform, false);
            child.transform.localPosition = new Vector3(5f, 0f, 0f);
            try
            {
                Rigidbody2D rigidbody = root.AddComponent<Rigidbody2D>();
                rigidbody.bodyType = RigidbodyType2D.Kinematic;
                root.AddComponent<CircleCollider2D>().radius = 2f;
                BoxCollider2D selected = child.AddComponent<BoxCollider2D>();
                HighDensityPhysicsBridge2D bridge = root.AddComponent<HighDensityPhysicsBridge2D>();
                var configuration = new Physics2DBridgeRuntimeConfiguration(
                    Physics2DBridgeShapeSourceMode.SelectedColliders,
                    selectedColliders: new[] { selected });

                Assert.IsTrue(bridge.Configure(registry, in configuration));
                registry.SynchronizePoses();
                Assert.AreEqual(1, registry.TargetCount);
                Assert.AreEqual(1, registry.BodyCount);

                world.FactDispatched += OnFact;
                ElementHandle projectile = SpawnQueryProjectile(
                    world,
                    new Vector2(3f, 0f),
                    new Vector2(8f, 0f));
                facts.Clear();
                world.Tick(0.5f);

                ElementFact contact = facts.Find(fact =>
                    fact.Type == ElementFactType.Contact && fact.Element == projectile.Key);
                Assert.Greater(contact.BridgeTargetId, 0);
                Assert.IsTrue(registry.TryGetCollider(contact.BridgeTargetId, out Collider2D resolved));
                Assert.AreSame(selected, resolved);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(settings);
            }
        }



        [Test]
        public void ManualShapes_UsesDedicatedCapsuleAndRejectsEmptyRuntimeReplacement()
        {
            using var world = new ElementWorld(8);
            Physics2DBridgeSettings settings = CreateSettings(17);
            using var registry = new Physics2DBridgeRegistry(world, settings);
            var target = new GameObject("ManualCapsuleBridgeTarget") { layer = 17 };
            try
            {
                target.transform.position = new Vector3(5f, 0f, 0f);
                HighDensityPhysicsBridge2D bridge = target.AddComponent<HighDensityPhysicsBridge2D>();
                Physics2DBridgeManualShape2D capsule = Physics2DBridgeManualShape2D.Capsule(
                    new Vector2(0f, 2.5f),
                    new Vector2(1f, 5f),
                    CapsuleDirection2D.Vertical,
                    0f,
                    target.layer);
                var configuration = new Physics2DBridgeRuntimeConfiguration(
                    Physics2DBridgeShapeSourceMode.ManualShapes,
                    manualShapes: new[] { capsule },
                    poseTransform: target.transform,
                    targetComponent: bridge,
                    manualBodyMode: Physics2DBridgeManualBodyMode.Kinematic);

                Assert.IsTrue(bridge.Configure(registry, in configuration));
                Physics2DBridgeRegistrationHandle originalHandle = bridge.RegistrationHandle;
                Assert.IsTrue(registry.IsRegistered(originalHandle));

                var invalidReplacement = new Physics2DBridgeRuntimeConfiguration(
                    Physics2DBridgeShapeSourceMode.ManualShapes,
                    manualShapes: System.Array.Empty<Physics2DBridgeManualShape2D>());
                Assert.IsFalse(bridge.Configure(registry, in invalidReplacement));
                Assert.AreEqual(originalHandle, bridge.RegistrationHandle);
                Assert.AreEqual(1, registry.TargetCount);

                var invalidShapeReplacement = new Physics2DBridgeRuntimeConfiguration(
                    Physics2DBridgeShapeSourceMode.ManualShapes,
                    manualShapes: new[]
                    {
                        Physics2DBridgeManualShape2D.Capsule(
                            Vector2.zero,
                            Vector2.zero,
                            CapsuleDirection2D.Vertical,
                            0f,
                            target.layer)
                    });
                Assert.IsFalse(bridge.Configure(registry, in invalidShapeReplacement));
                Assert.AreEqual(originalHandle, bridge.RegistrationHandle);
                Assert.AreEqual(1, registry.TargetCount);

                world.FactDispatched += OnFact;
                ElementHandle projectile = SpawnQueryProjectile(
                    world,
                    new Vector2(2f, 2.5f),
                    new Vector2(8f, 0f));
                facts.Clear();
                world.Tick(0.5f);

                ElementFact contact = facts.Find(fact =>
                    fact.Type == ElementFactType.Contact && fact.Element == projectile.Key);
                Assert.Greater(contact.BridgeTargetId, 0);
                Assert.IsTrue(registry.TryGetTarget(contact.BridgeTargetId, out Physics2DBridgeTarget resolved));
                Assert.AreSame(bridge, resolved.Owner);
                Assert.IsNull(resolved.Collider);
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(settings);
            }
        }



        [Test]
        public void ChangeSource_RebuildsOnlyAfterEvent_AndUnsubscribesOnUnregister()
        {
            using var world = new ElementWorld(8);
            Physics2DBridgeSettings settings = CreateSettings(16);
            using var registry = new Physics2DBridgeRegistry(world, settings);
            var target = new GameObject("NotifyingProviderBridgeTarget") { layer = 16 };
            try
            {
                HighDensityPhysicsBridge2D bridge = target.AddComponent<HighDensityPhysicsBridge2D>();
                var provider = new NotifyingBoxShapeProvider(target.transform, Vector2.one);
                Assert.IsTrue(bridge.Configure(registry, provider));
                Assert.AreEqual(1, provider.CopyCount);
                Assert.AreEqual(1, provider.GeometrySubscriberCount);
                Assert.AreEqual(1, provider.StateSubscriberCount);

                registry.SynchronizePoses();
                registry.SynchronizePoses();
                Assert.AreEqual(1, provider.CopyCount);

                provider.NotifyStateChanged();
                registry.SynchronizePoses();
                Assert.AreEqual(1, provider.CopyCount);

                provider.SetSize(new Vector2(2f, 3f));
                registry.SynchronizePoses();
                Assert.AreEqual(2, provider.CopyCount);

                Assert.IsTrue(registry.Unregister(bridge.RegistrationHandle));
                Assert.AreEqual(0, provider.GeometrySubscriberCount);
                Assert.AreEqual(0, provider.StateSubscriberCount);
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(settings);
            }
        }



        [Test]
        public void PassiveProxy_DoesNotIntegrateLegacyVelocityInCoreWorld()
        {
            using var world = new ElementWorld(4);
            Physics2DBridgeSettings settings = CreateSettings(15);
            using var registry = new Physics2DBridgeRegistry(world, settings);
            var target = new GameObject("PassiveVelocityBridgeTarget") { layer = 15 };
            try
            {
                Rigidbody2D rigidbody = target.AddComponent<Rigidbody2D>();
                rigidbody.bodyType = RigidbodyType2D.Kinematic;
                rigidbody.position = new Vector2(3f, 2f);
                rigidbody.linearVelocity = new Vector2(100f, 0f);
                rigidbody.angularVelocity = 720f;
                target.AddComponent<CircleCollider2D>();
                HighDensityPhysicsBridge2D bridge = target.AddComponent<HighDensityPhysicsBridge2D>();
                Assert.IsTrue(bridge.Configure(registry));
                registry.SynchronizePoses();

                Assert.IsTrue(registry.TryGetDiagnostics(
                    bridge.RegistrationHandle,
                    0,
                    1f,
                    out Physics2DBridgePoseDiagnostics before));
                world.Tick(0.02f);
                Assert.IsTrue(registry.TryGetDiagnostics(
                    bridge.RegistrationHandle,
                    0,
                    1f,
                    out Physics2DBridgePoseDiagnostics after));

                Assert.That(after.CorePosition.x, Is.EqualTo(before.CorePosition.x).Within(0.0001f));
                Assert.That(after.CorePosition.y, Is.EqualTo(before.CorePosition.y).Within(0.0001f));
                Assert.That(after.CoreRotationDegrees, Is.EqualTo(before.CoreRotationDegrees).Within(0.0001f));
                Assert.That(after.LegacyToCoreDelta.sqrMagnitude, Is.LessThanOrEqualTo(0.00000001f));
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(settings);
            }
        }



        [Test]
        public void PassiveProviderProxy_TracksLegacyPoseForThreeHundredStepsWithoutLeading()
        {
            using var world = new ElementWorld(8);
            Physics2DBridgeSettings settings = CreateSettings(15);
            using var registry = new Physics2DBridgeRegistry(world, settings);
            var target = new GameObject("ThreeHundredStepBridgeTarget") { layer = 15 };
            try
            {
                HighDensityPhysicsBridge2D bridge = target.AddComponent<HighDensityPhysicsBridge2D>();
                var provider = new NotifyingBoxShapeProvider(target.transform, new Vector2(1f, 4f));
                Assert.IsTrue(bridge.Configure(registry, provider));

                float maximumPositionError = 0f;
                float maximumRotationError = 0f;
                int firstFailureStep = -1;
                long previousSequence = 0;

                for (int step = 0; step < 300; step++)
                {
                    Vector2 expectedPosition = ResolveVerificationPosition(step);
                    float expectedRotation = ResolveVerificationRotation(step);
                    target.transform.SetPositionAndRotation(
                        new Vector3(expectedPosition.x, expectedPosition.y, 0f),
                        Quaternion.Euler(0f, 0f, expectedRotation));

                    if (step == 150) { provider.SetSize(new Vector2(1.5f, 5f)); }

                    registry.SynchronizePoses();
                    Assert.IsTrue(registry.TryGetDiagnostics(
                        bridge.RegistrationHandle,
                        0,
                        1f,
                        out Physics2DBridgePoseDiagnostics afterSynchronization));

                    float syncPositionError = Vector2.Distance(
                        expectedPosition,
                        afterSynchronization.CorePosition);
                    float syncRotationError = Mathf.Abs(Mathf.DeltaAngle(
                        expectedRotation,
                        afterSynchronization.CoreRotationDegrees));
                    maximumPositionError = Mathf.Max(maximumPositionError, syncPositionError);
                    maximumRotationError = Mathf.Max(maximumRotationError, syncRotationError);
                    if (firstFailureStep < 0 &&
                        (syncPositionError > 0.0001f || syncRotationError > 0.0001f))
                    {
                        firstFailureStep = step;
                    }

                    Assert.AreEqual(
                        previousSequence + 1,
                        afterSynchronization.SynchronizationSequence,
                        $"step {step}: bridge sync가 정확히 한 번 진행되어야 합니다.");
                    previousSequence = afterSynchronization.SynchronizationSequence;

                    world.Tick(0.02f);
                    Assert.IsTrue(registry.TryGetDiagnostics(
                        bridge.RegistrationHandle,
                        0,
                        1f,
                        out Physics2DBridgePoseDiagnostics afterSimulation));

                    float simulatedPositionError = Vector2.Distance(
                        expectedPosition,
                        afterSimulation.CorePosition);
                    float simulatedRotationError = Mathf.Abs(Mathf.DeltaAngle(
                        expectedRotation,
                        afterSimulation.CoreRotationDegrees));
                    maximumPositionError = Mathf.Max(maximumPositionError, simulatedPositionError);
                    maximumRotationError = Mathf.Max(maximumRotationError, simulatedRotationError);
                    if (firstFailureStep < 0 &&
                        (simulatedPositionError > 0.0001f || simulatedRotationError > 0.0001f))
                    {
                        firstFailureStep = step;
                    }

                    Assert.AreEqual(
                        previousSequence,
                        afterSimulation.SynchronizationSequence,
                        $"step {step}: Core simulate가 bridge sync 순번을 변경하면 안 됩니다.");
                }

                Assert.That(
                    firstFailureStep,
                    Is.EqualTo(-1),
                    $"first failure step={firstFailureStep}, max position error={maximumPositionError}, " +
                    $"max rotation error={maximumRotationError}");
                Assert.That(maximumPositionError, Is.LessThanOrEqualTo(0.0001f));
                Assert.That(maximumRotationError, Is.LessThanOrEqualTo(0.0001f));
                Assert.AreEqual(2, provider.CopyCount, "형상 변경 이벤트당 한 번만 재구성되어야 합니다.");
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(settings);
            }
        }



        private static Vector2 ResolveVerificationPosition(int step)
        {
            if (step < 40) { return Vector2.zero; }
            if (step < 100) { return new Vector2((step - 40) * 0.05f, 0f); }
            if (step < 160) { return new Vector2(3f - (step - 100) * 0.075f, 0f); }
            if (step < 220) { return new Vector2(-1.5f, (step - 160) * 0.04f); }
            if (step == 220) { return new Vector2(12f, -7f); }
            return new Vector2(12f + (step - 220) * 0.02f, -7f);
        }



        private static float ResolveVerificationRotation(int step) =>
            step < 220 ? Mathf.Repeat(step * 1.25f, 360f) : -35f;



        [Test]
        public void Diagnostics_UsesPresentationPoseAndExactCachedCapsuleGeometry()
        {
            using var world = new ElementWorld(8);
            PhysicsWorld physicsWorld = world.PhysicsCoreLane.World;
            PhysicsWorld.DrawOptions originalDrawOptions = physicsWorld.drawOptions;
            Physics2DBridgeSettings settings = CreateSettings(15);
            var registry = new Physics2DBridgeRegistry(world, settings);
            var target = new GameObject("PresentationPoseBridgeTarget") { layer = 15 };
            try
            {
                target.transform.position = new Vector3(7f, 3f, 0f);
                HighDensityPhysicsBridge2D bridge = target.AddComponent<HighDensityPhysicsBridge2D>();
                Physics2DBridgeManualShape2D capsule = Physics2DBridgeManualShape2D.Capsule(
                    new Vector2(0f, 2.5f),
                    new Vector2(1f, 5f),
                    CapsuleDirection2D.Vertical,
                    0f,
                    target.layer);
                var configuration = new Physics2DBridgeRuntimeConfiguration(
                    Physics2DBridgeShapeSourceMode.ManualShapes,
                    manualShapes: new[] { capsule },
                    poseTransform: target.transform,
                    targetComponent: bridge,
                    manualBodyMode: Physics2DBridgeManualBodyMode.Kinematic,
                    provider: new BoxShapeProvider(
                        target.transform,
                        new Vector2(2f, -1f),
                        Vector2.one));

                Assert.IsTrue(bridge.Configure(registry, in configuration));
                registry.SynchronizePoses();
                Assert.IsTrue(registry.TryGetDiagnostics(
                    bridge.RegistrationHandle,
                    0,
                    1f,
                    out Physics2DBridgePoseDiagnostics diagnostics));

                Assert.That(diagnostics.CorePosition.x, Is.EqualTo(2f).Within(0.0001f));
                Assert.That(diagnostics.CorePosition.y, Is.EqualTo(-1f).Within(0.0001f));
                Assert.That(diagnostics.PresentationPosition.x, Is.EqualTo(7f).Within(0.0001f));
                Assert.That(diagnostics.PresentationPosition.y, Is.EqualTo(3f).Within(0.0001f));
                Assert.AreEqual(1, diagnostics.ShapeCount);

                Assert.IsTrue(registry.TryGetDebugShapeLocalBounds(
                    bridge.RegistrationHandle,
                    0,
                    0,
                    out Bounds bounds));
                Assert.That(bounds.center.x, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(bounds.center.y, Is.EqualTo(2.5f).Within(0.0001f));
                Assert.That(bounds.size.x, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(bounds.size.y, Is.EqualTo(5f).Within(0.0001f));

                Assert.AreEqual(0, registry.DrawDiagnostics(
                    bridge.RegistrationHandle,
                    0,
                    in diagnostics,
                    Physics2DBridgeDebugDrawMode.Off));
                Assert.AreEqual(1, registry.DrawDiagnostics(
                    bridge.RegistrationHandle,
                    0,
                    in diagnostics,
                    Physics2DBridgeDebugDrawMode.Presentation));
                Assert.AreEqual(1, registry.DrawDiagnostics(
                    bridge.RegistrationHandle,
                    0,
                    in diagnostics,
                    Physics2DBridgeDebugDrawMode.RawPhysics));
                Assert.AreEqual(2, registry.DrawDiagnostics(
                    bridge.RegistrationHandle,
                    0,
                    in diagnostics,
                    Physics2DBridgeDebugDrawMode.Both));
                Assert.AreEqual(PhysicsWorld.DrawOptions.AllCustom, physicsWorld.drawOptions);
            }
            finally
            {
                registry.Dispose();
                Assert.AreEqual(originalDrawOptions, physicsWorld.drawOptions);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(settings);
            }
        }



        [Test]
        public void HybridRaycast_BothWorldsDeduplicatesMirroredCollider()
        {
            using var world = new ElementWorld(8);
            Physics2DBridgeSettings settings = CreateSettings(27);
            using var registry = new Physics2DBridgeRegistry(world, settings);
            var targetObject = new GameObject("HybridRaycastTarget") { layer = 27 };
            try
            {
                targetObject.transform.position = new Vector3(5f, 0f, 0f);
                Collider2D collider = targetObject.AddComponent<BoxCollider2D>();
                Physics2D.SyncTransforms();
                Assert.AreEqual(1, registry.RegisterStaticSceneColliders());
                var hybrid = new HybridPhysicsWorld2D(world, registry);
                var results = new List<HybridPhysicsHit2D>(4);
                HybridPhysicsQueryFilter2D filter = HybridPhysicsQueryFilter2D.Default;
                filter.LayerMask = 1 << 27;

                Assert.AreEqual(1, hybrid.Raycast(Vector2.zero, Vector2.right, 10f, in filter, results));
                Assert.AreEqual(HybridPhysicsHitKind2D.LegacyCollider, results[0].Kind);
                Assert.AreSame(collider, results[0].Collider);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(settings);
            }
        }



        [Test]
        public void HybridRaycast_CoreOnlyReturnsOptInQueryTargetElement()
        {
            using var world = new ElementWorld(8);
            Physics2DBridgeSettings settings = CreateSettings(26);
            using var registry = new Physics2DBridgeRegistry(world, settings);
            try
            {
                Assert.IsTrue(ElementCompiledArchetype.TryCreate(
                    ElementCapabilities.Lifetime | ElementCapabilities.QueryTarget2D,
                    0.5f,
                    2f,
                    0,
                    0f,
                    false,
                    PhysicsCoreBodyMode.Kinematic,
                    PhysicsCoreShape2D.Circle,
                    out ElementCompiledArchetype archetype,
                    out ElementSpawnStatus failure), failure.ToString());
                ElementSpawnBuilder builder = ElementSpawnBuilder.From(archetype)
                    .WithPose(new float2(5f, 0f), new float2(1f))
                    .WithPhysicsFilter(1ul << 26, ulong.MaxValue);
                ElementHandle target = world.Spawn(in builder).Handle;

                var hybrid = new HybridPhysicsWorld2D(world, registry);
                var results = new List<HybridPhysicsHit2D>(4);
                HybridPhysicsQueryFilter2D filter = HybridPhysicsQueryFilter2D.Default;
                filter.Worlds = HybridPhysicsQueryWorldMask.Core;
                filter.LayerMask = 1 << 26;

                Assert.AreEqual(1, hybrid.Raycast(Vector2.zero, Vector2.right, 10f, in filter, results));
                Assert.AreEqual(HybridPhysicsHitKind2D.CoreElement, results[0].Kind);
                Assert.AreEqual(target.Key, results[0].Element);
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }



        [Test]
        public void RegistrationHandle_IsScopedToOneRegistryEvenWhenWorldIsShared()
        {
            using var world = new ElementWorld(8);
            Physics2DBridgeSettings settingsA = CreateSettings(25);
            Physics2DBridgeSettings settingsB = CreateSettings(25);
            using var registryA = new Physics2DBridgeRegistry(world, settingsA);
            using var registryB = new Physics2DBridgeRegistry(world, settingsB);
            var targetA = new GameObject("RegistryScopedTargetA") { layer = 25 };
            var targetB = new GameObject("RegistryScopedTargetB") { layer = 25 };
            try
            {
                targetA.AddComponent<BoxCollider2D>();
                targetB.AddComponent<BoxCollider2D>();
                HighDensityPhysicsBridge2D bridgeA = targetA.AddComponent<HighDensityPhysicsBridge2D>();
                HighDensityPhysicsBridge2D bridgeB = targetB.AddComponent<HighDensityPhysicsBridge2D>();
                bridgeA.Bind(registryA);
                bridgeB.Bind(registryB);

                Physics2DBridgeRegistrationHandle handleA = bridgeA.RegistrationHandle;
                Physics2DBridgeRegistrationHandle handleB = bridgeB.RegistrationHandle;
                Assert.AreNotEqual(handleA.ContextId, handleB.ContextId);
                Assert.IsFalse(registryB.Unregister(handleA));
                Assert.IsTrue(registryA.IsRegistered(handleA));
                Assert.IsTrue(registryB.IsRegistered(handleB));
            }
            finally
            {
                Object.DestroyImmediate(targetA);
                Object.DestroyImmediate(targetB);
                Object.DestroyImmediate(settingsA);
                Object.DestroyImmediate(settingsB);
            }
        }



        [Test]
        public void HybridWorld_RejectsRegistryFromAnotherPhysicsCoreLane()
        {
            using var worldA = new ElementWorld(4);
            using var worldB = new ElementWorld(4);
            Physics2DBridgeSettings settings = CreateSettings(24);
            using var registryB = new Physics2DBridgeRegistry(worldB, settings);
            try
            {
                Assert.Throws<System.ArgumentException>(() => new HybridPhysicsWorld2D(worldA, registryB));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }



        [Test]
        public void DefaultRegistryDispose_RebindsActiveAutomaticBridgeToSuccessor()
        {
            Assume.That(
                Physics2DBridgeRegistry.Default,
                Is.Null,
                "Default registry 승계 테스트는 격리된 registry domain이 필요합니다.");

            using var worldA = new ElementWorld(4);
            using var worldB = new ElementWorld(4);
            Physics2DBridgeSettings settingsA = CreateSettings(23);
            Physics2DBridgeSettings settingsB = CreateSettings(23);
            var registryA = new Physics2DBridgeRegistry(worldA, settingsA);
            using var registryB = new Physics2DBridgeRegistry(worldB, settingsB);
            var target = new GameObject("DefaultRegistryPromotionTarget") { layer = 23 };
            try
            {
                Assert.AreSame(registryA, Physics2DBridgeRegistry.Default);
                target.AddComponent<BoxCollider2D>();
                HighDensityPhysicsBridge2D bridge = target.AddComponent<HighDensityPhysicsBridge2D>();
                Assert.IsTrue(bridge.TryBind(null, out _));
                registryA.SynchronizePoses();
                Assert.IsTrue(
                    registryA.IsRegistered(bridge.RegistrationHandle),
                    $"handle(valid={bridge.RegistrationHandle.IsValid}, context={bridge.RegistrationHandle.ContextId}, " +
                    $"slot={bridge.RegistrationHandle.Slot}, generation={bridge.RegistrationHandle.Generation}), " +
                    $"registry(context={registryA.ContextId}, targets={registryA.TargetCount}, bodies={registryA.BodyCount})");

                registryA.Dispose();
                registryB.SynchronizePoses();

                Assert.AreSame(registryB, Physics2DBridgeRegistry.Default);
                Assert.IsTrue(registryB.IsRegistered(bridge.RegistrationHandle));
                Assert.AreEqual(1, registryB.TargetCount);
            }
            finally
            {
                registryA.Dispose();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(settingsA);
                Object.DestroyImmediate(settingsB);
            }
        }



        [Test]
        public void ExplicitRegistryDispose_DetachesBridgeWithoutLeavingDisposedReference()
        {
            using var worldA = new ElementWorld(4);
            using var worldB = new ElementWorld(4);
            Physics2DBridgeSettings settingsA = CreateSettings(22);
            Physics2DBridgeSettings settingsB = CreateSettings(22);
            var registryA = new Physics2DBridgeRegistry(worldA, settingsA);
            using var registryB = new Physics2DBridgeRegistry(worldB, settingsB);
            var target = new GameObject("ExplicitRegistryDisposeTarget") { layer = 22 };
            try
            {
                target.AddComponent<BoxCollider2D>();
                HighDensityPhysicsBridge2D bridge = target.AddComponent<HighDensityPhysicsBridge2D>();
                bridge.Bind(registryA);
                Assert.IsTrue(registryA.IsRegistered(bridge.RegistrationHandle));

                registryA.Dispose();
                target.SetActive(false);
                target.SetActive(true);

                Assert.DoesNotThrow(() => bridge.MarkStateDirty());
                Assert.IsTrue(registryB.IsRegistered(bridge.RegistrationHandle));
            }
            finally
            {
                registryA.Dispose();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(settingsA);
                Object.DestroyImmediate(settingsB);
            }
        }



        [Test]
        public void PendingRegistration_RetriesUntilColliderGeometryBecomesAvailable()
        {
            using var world = new ElementWorld(4);
            Physics2DBridgeSettings settings = CreateSettings(21);
            using var registry = new Physics2DBridgeRegistry(world, settings);
            var target = new GameObject("PendingBridgeTarget") { layer = 21 };
            try
            {
                HighDensityPhysicsBridge2D bridge = target.AddComponent<HighDensityPhysicsBridge2D>();
                Assert.IsTrue(bridge.TryBind(registry, out _));

                registry.SynchronizePoses();
                Assert.IsFalse(bridge.RegistrationHandle.IsValid);
                Assert.AreEqual(0, registry.TargetCount);

                target.AddComponent<BoxCollider2D>();
                registry.SynchronizePoses();

                Assert.IsTrue(registry.IsRegistered(bridge.RegistrationHandle));
                Assert.AreEqual(1, registry.TargetCount);
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(settings);
            }
        }



        [Test]
        public void AutomaticStaticRegistration_RemovesDestroyedSceneColliderOnNextSync()
        {
            using var world = new ElementWorld(4);
            Physics2DBridgeSettings settings = CreateSettings(31);
            using var registry = new Physics2DBridgeRegistry(world, settings);
            var target = new GameObject("DestroyedAutomaticStaticTarget") { layer = 31 };
            try
            {
                target.AddComponent<BoxCollider2D>();
                Assert.AreEqual(1, registry.RegisterStaticSceneColliders());
                Assert.AreEqual(1, registry.TargetCount);
                Assert.AreEqual(1, registry.BodyCount);

                Object.DestroyImmediate(target);
                target = null;
                registry.SynchronizePoses();

                Assert.AreEqual(0, registry.TargetCount);
                Assert.AreEqual(0, registry.BodyCount);
            }
            finally
            {
                if (target != null) { Object.DestroyImmediate(target); }
                Object.DestroyImmediate(settings);
            }
        }



        [Test]
        public void ElementWorldFact_IsDeliveredOnceToLegacyReceiver()
        {
            using var world = new ElementWorld(8);
            Physics2DBridgeSettings settings = CreateSettings(20);
            using var registry = new Physics2DBridgeRegistry(world, settings);
            var target = new GameObject("ElementFactReceiverTarget") { layer = 20 };
            try
            {
                target.transform.position = new Vector3(2f, 0f, 0f);
                BoxCollider2D collider = target.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                var receiver = target.AddComponent<RecordingElementPhysicsFactReceiver2D>();
                HighDensityPhysicsBridge2D bridge = target.AddComponent<HighDensityPhysicsBridge2D>();
                bridge.Bind(registry);
                registry.SynchronizePoses();

                ElementHandle projectile = SpawnQueryProjectile(world, Vector2.zero, new Vector2(10f, 0f));
                world.Tick(0.5f, 7);

                Assert.AreEqual(1, receiver.ReceiveCount);
                Assert.AreEqual(ElementFactType.Contact, receiver.LastFact.Type);
                Assert.AreEqual(projectile.Key, receiver.LastFact.Element);
                Assert.AreSame(collider, receiver.LastFact.Target.Collider);
                Assert.AreEqual(collider.isTrigger, receiver.LastFact.IsTrigger);
                Assert.AreEqual(7, receiver.LastFact.SubstepIndex);
                Assert.IsFalse(receiver.LastFact.StartedOverlapped);
                Assert.IsTrue(receiver.LastFact.HasSurfacePoint);
                Assert.IsTrue(receiver.LastFact.HasSurfaceNormal);
                Assert.IsTrue(receiver.LastFact.HasImpactCenter);
                Assert.AreEqual(receiver.LastFact.Position, receiver.LastFact.SurfacePoint);
                Assert.AreEqual(receiver.LastFact.Normal, receiver.LastFact.SurfaceNormal);
                Assert.Less(receiver.LastFact.SurfaceNormal.x, 0f);
                Assert.That(receiver.LastFact.ImpactCenter.x, Is.GreaterThan(0f));
                Assert.That(receiver.LastFact.ImpactCenter.x, Is.LessThan(5f));
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(settings);
            }
        }



        [Test]
        public void StrictMotion_AutomaticManualAndTeleportRulesAreDeterministic()
        {
            using var world = new ElementWorld(8);
            Physics2DBridgeSettings settings = CreateSettings(19);
            settings.StrictCcdMotionRatio = 0.5f;
            using var registry = new Physics2DBridgeRegistry(world, settings);
            var target = new GameObject("StrictProjectionTarget") { layer = 19 };
            var motions = new List<Physics2DBridgeStrictMotion2D>(2);
            try
            {
                Rigidbody2D rigidbody = target.AddComponent<Rigidbody2D>();
                rigidbody.bodyType = RigidbodyType2D.Kinematic;
                CircleCollider2D collider = target.AddComponent<CircleCollider2D>();
                collider.radius = 0.5f;
                HighDensityPhysicsBridge2D bridge = target.AddComponent<HighDensityPhysicsBridge2D>();
                bridge.Bind(registry);
                registry.SynchronizePoses();

                rigidbody.position = new Vector2(1f, 0f);
                registry.SynchronizePoses();
                Assert.AreEqual(1, registry.CopyStrictMotions(motions));
                Assert.AreEqual(Vector2.zero, motions[0].PreviousPosition);
                Assert.AreEqual(new Vector2(1f, 0f), motions[0].Position);

                bridge.MarkTeleported();
                rigidbody.position = new Vector2(5f, 0f);
                registry.SynchronizePoses();
                Assert.AreEqual(0, registry.CopyStrictMotions(motions));

                bridge.RequestStrictCcd(1);
                rigidbody.position = new Vector2(5.01f, 0f);
                registry.SynchronizePoses();
                Assert.AreEqual(1, registry.CopyStrictMotions(motions));
                Assert.IsTrue(motions[0].StrictCcdRequested);
                Assert.IsFalse(motions[0].Teleported);
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(settings);
            }
        }



        [Test]
        public void HybridCoreOnly_HidesLegacyProjectionImplementationShape()
        {
            using var world = new ElementWorld(8);
            Physics2DBridgeSettings settings = CreateSettings(18);
            using var registry = new Physics2DBridgeRegistry(world, settings);
            var target = new GameObject("HiddenProjectionTarget") { layer = 18 };
            try
            {
                target.transform.position = new Vector3(2f, 0f, 0f);
                target.AddComponent<BoxCollider2D>();
                HighDensityPhysicsBridge2D bridge = target.AddComponent<HighDensityPhysicsBridge2D>();
                bridge.Bind(registry);
                registry.SynchronizePoses();

                var hybrid = new HybridPhysicsWorld2D(world, registry);
                var results = new List<HybridPhysicsHit2D>(4);
                HybridPhysicsQueryFilter2D filter = HybridPhysicsQueryFilter2D.Default;
                filter.Worlds = HybridPhysicsQueryWorldMask.Core;
                filter.LayerMask = 1 << 18;

                Assert.AreEqual(0, hybrid.Raycast(Vector2.zero, Vector2.right, 10f, in filter, results));
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(settings);
            }
        }



        [Test]
        public void FastProjectionCrossingStationaryBodylessElement_ProducesContact()
        {
            using var world = new ElementWorld(8);
            Physics2DBridgeSettings settings = CreateSettings(17);
            settings.StrictCcdMotionRatio = 0.5f;
            using var registry = new Physics2DBridgeRegistry(world, settings);
            var target = new GameObject("FastCrossingProjectionTarget") { layer = 17 };
            try
            {
                Rigidbody2D rigidbody = target.AddComponent<Rigidbody2D>();
                rigidbody.bodyType = RigidbodyType2D.Kinematic;
                rigidbody.position = new Vector2(-2f, 0f);
                CircleCollider2D collider = target.AddComponent<CircleCollider2D>();
                collider.radius = 0.25f;
                HighDensityPhysicsBridge2D bridge = target.AddComponent<HighDensityPhysicsBridge2D>();
                bridge.Bind(registry);
                registry.SynchronizePoses();

                world.FactDispatched += OnFact;
                ElementHandle projectile = SpawnQueryProjectile(world, Vector2.zero, Vector2.zero);
                facts.Clear();
                rigidbody.position = new Vector2(2f, 0f);
                registry.SynchronizePoses();
                world.Tick(0.02f);

                Assert.That(facts.Exists(fact =>
                    fact.Type == ElementFactType.Contact &&
                    fact.Element == projectile.Key &&
                    fact.BridgeTargetId > 0), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(settings);
            }
        }



#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Test]
        public void DebugSummaryProvider_RegistersAndUnregistersWithRegistryLifetime()
        {
            using var world = new ElementWorld(4);
            Physics2DBridgeSettings settings = CreateSettings(16);
            var summaries = new List<string>(2);
            try
            {
                using (var registry = new Physics2DBridgeRegistry(world, settings))
                {
                    ElementWorldDebugRegistry.CopyExtensionSummaries(world, summaries);
                    Assert.That(summaries.Exists(summary =>
                        summary.Contains("Projection") && summary.Contains("Body 0")), Is.True);
                }

                ElementWorldDebugRegistry.CopyExtensionSummaries(world, summaries);
                Assert.That(summaries.Exists(summary => summary.Contains("Projection")), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }
#endif



        private void OnFact(in ElementFact fact) => facts.Add(fact);



        private static Physics2DBridgeSettings CreateSettings(int layer)
        {
            Physics2DBridgeSettings settings = ScriptableObject.CreateInstance<Physics2DBridgeSettings>();
            settings.AutomaticMirrorLayers = 1 << layer;
            settings.InitialTargetCapacity = 16;
            return settings;
        }



        private static ElementHandle SpawnQueryProjectile(
            ElementWorld world,
            Vector2 position,
            Vector2 velocity)
        {
            Assert.IsTrue(ElementCompiledArchetype.TryCreate(
                ElementCapabilities.KinematicMotion2D | ElementCapabilities.QuerySensor2D,
                0.1f,
                2f,
                0,
                0f,
                false,
                PhysicsCoreBodyMode.Kinematic,
                PhysicsCoreShape2D.Circle,
                out ElementCompiledArchetype archetype,
                out ElementSpawnStatus failure), failure.ToString());
            ElementSpawnBuilder builder = ElementSpawnBuilder.From(archetype)
                .WithPose(new float2(position.x, position.y), new float2(1f))
                .WithVelocity(new float2(velocity.x, velocity.y))
                .WithPhysicsFilter(1ul, ulong.MaxValue);
            return world.Spawn(in builder).Handle;
        }
    }
}
