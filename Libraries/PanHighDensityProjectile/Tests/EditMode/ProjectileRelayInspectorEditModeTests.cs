using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Pan.HighDensityProjectile;



namespace Pan.HighDensityProjectile.Tests
{
    public sealed class ProjectileRelayInspectorEditModeTests
    {
        private const BindingFlags InstancePrivateFlags = BindingFlags.Instance | BindingFlags.NonPublic;



        [Test]
        public void ProjectileGrazeRelay_OnValidateClampsSerializedGrazeValue()
        {
            GameObject targetObject = new GameObject("ProjectileGrazeRelayOnValidateTarget");
            try
            {
                ProjectileGrazeRelay relay = targetObject.AddComponent<ProjectileGrazeRelay>();
                ProjectileGrazeReceiverProbe receiver = targetObject.AddComponent<ProjectileGrazeReceiverProbe>();

                typeof(ProjectileGrazeRelay)
                    .GetField("grazeValue", InstancePrivateFlags)
                    ?.SetValue(relay, -3f);
                typeof(ProjectileGrazeRelay)
                    .GetMethod("OnValidate", InstancePrivateFlags)
                    ?.Invoke(relay, null);

                Assert.AreEqual(0f, relay.GrazeValue);

                ProjectileHitPayload hit = CreateProjectileRelayTestHitPayload(relay, targetObject, 46, 1f, 4);
                Assert.IsFalse(relay.TryReceiveProjectileHit(in hit));
                Assert.AreEqual(1, relay.HitCount);
                Assert.AreEqual(1, relay.GrazeDispatchCount);
                Assert.AreEqual(1, receiver.GrazeCount);
                Assert.AreEqual(0f, relay.LastGraze.GrazeValue);
                Assert.AreEqual(0f, receiver.LastGraze.GrazeValue);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
            }
        }



        [Test]
        public void ProjectileEventStreamRelay_OnDisableCanResetCountersAndUnsubscribe()
        {
            GameObject root = new GameObject("ProjectileEventStreamRelayResetOnDisableRoot");
            try
            {
                ProjectileEventStreamRelay relay = root.AddComponent<ProjectileEventStreamRelay>();
                ProjectileEventStreamBodyProbe body = root.AddComponent<ProjectileEventStreamBodyProbe>();
                ProjectileEventStreamReceiverProbe receiver = root.AddComponent<ProjectileEventStreamReceiverProbe>();
                relay.ResetCountersOnDisable = true;
                relay.Initialize(body, root.transform);

                body.EmitSpawn();
                body.EmitContact(true);
                body.EmitDespawn();

                Assert.AreEqual(1, relay.SpawnedCount);
                Assert.AreEqual(1, relay.ContactCount);
                Assert.AreEqual(1, relay.DespawnedCount);
                Assert.AreEqual(1, receiver.SpawnCount);
                Assert.AreEqual(1, receiver.ContactCount);
                Assert.AreEqual(1, receiver.DespawnCount);

                InvokePrivate(relay, "OnDisable");

                Assert.AreEqual(0, relay.SpawnedCount);
                Assert.AreEqual(0, relay.ContactCount);
                Assert.AreEqual(0, relay.DespawnedCount);

                body.EmitSpawn();
                body.EmitContact(true);
                body.EmitDespawn();

                Assert.AreEqual(0, relay.SpawnedCount);
                Assert.AreEqual(0, relay.ContactCount);
                Assert.AreEqual(0, relay.DespawnedCount);
                Assert.AreEqual(1, receiver.SpawnCount);
                Assert.AreEqual(1, receiver.ContactCount);
                Assert.AreEqual(1, receiver.DespawnCount);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }



        [Test]
        public void ProjectileEventStreamRelay_OnDisableCanKeepCountersForProfiling()
        {
            GameObject root = new GameObject("ProjectileEventStreamRelayKeepCountersRoot");
            try
            {
                ProjectileEventStreamRelay relay = root.AddComponent<ProjectileEventStreamRelay>();
                ProjectileEventStreamBodyProbe body = root.AddComponent<ProjectileEventStreamBodyProbe>();
                root.AddComponent<ProjectileEventStreamReceiverProbe>();
                relay.ResetCountersOnDisable = false;
                relay.Initialize(body, root.transform);

                body.EmitSpawn();
                body.EmitContact(false);
                body.EmitDespawn();

                InvokePrivate(relay, "OnDisable");

                Assert.AreEqual(1, relay.SpawnedCount);
                Assert.AreEqual(1, relay.ContactCount);
                Assert.AreEqual(1, relay.DespawnedCount);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }



        private static void InvokePrivate(object target, string methodName)
        {
            target.GetType().GetMethod(methodName, InstancePrivateFlags)?.Invoke(target, null);
        }



        private static ProjectileHitPayload CreateProjectileRelayTestHitPayload(
            Component source,
            GameObject hitGameObject,
            int projectileId,
            float damage,
            int teamId)
        {
            return new ProjectileHitPayload(
                projectileId,
                source,
                null,
                null,
                null,
                hitGameObject != null ? hitGameObject.transform.position : Vector3.zero,
                Vector3.left,
                Vector3.right,
                damage,
                teamId,
                true,
                source,
                hitGameObject);
        }



        private sealed class ProjectileEventStreamBodyProbe : MonoBehaviour, IPanProjectileBody
        {
            private int nextProjectileId = 1;



            public int ActiveCount { get; private set; }
            public int Capacity => 32;
            public int TotalSpawned { get; private set; }
            public int TotalDespawned { get; private set; }
            public int TotalHits { get; private set; }
            public event ProjectileSnapshotHandler ProjectileSpawned;
            public event ProjectileContactHandler ProjectileContacted;
            public event ProjectileSnapshotHandler ProjectileDespawned;



            public void EmitSpawn()
            {
                TrySpawn(CreateSpawnData(this), out _);
            }



            public void EmitContact(bool consumed)
            {
                ProjectileHitPayload hit = CreateProjectileRelayTestHitPayload(this, gameObject, nextProjectileId, 1f, 1);
                var contact = new ProjectileContactPayload(hit, consumed, 1);
                TotalHits++;
                ProjectileContacted?.Invoke(in contact);
            }



            public void EmitDespawn()
            {
                ProjectileSnapshot snapshot = CreateSnapshot(nextProjectileId++);
                if (ActiveCount > 0)
                {
                    ActiveCount--;
                }

                TotalDespawned++;
                ProjectileDespawned?.Invoke(in snapshot);
            }



            public bool TrySpawn(in ProjectileSpawnRequest spawnData, out int projectileId)
            {
                projectileId = nextProjectileId++;
                ProjectileSnapshot snapshot = CreateSnapshot(projectileId);
                ActiveCount++;
                TotalSpawned++;
                ProjectileSpawned?.Invoke(in snapshot);
                return true;
            }



            public int Spawn(in ProjectileSpawnRequest spawnData)
            {
                TrySpawn(in spawnData, out int projectileId);
                return projectileId;
            }



            public void Simulate(float deltaTime)
            {
            }



            public void ClearAll()
            {
                ActiveCount = 0;
            }



            public bool Despawn(int projectileId)
            {
                if (ActiveCount <= 0)
                {
                    return false;
                }

                ActiveCount--;
                TotalDespawned++;
                ProjectileSnapshot snapshot = CreateSnapshot(projectileId);
                ProjectileDespawned?.Invoke(in snapshot);
                return true;
            }



            public bool TryGetSnapshot(int index, out ProjectileSnapshot snapshot)
            {
                snapshot = default;
                return false;
            }



            public bool TryGetSnapshotById(int projectileId, out ProjectileSnapshot snapshot)
            {
                snapshot = default;
                return false;
            }



            public int CopySnapshots(ProjectileSnapshot[] buffer)
            {
                return 0;
            }



            private static ProjectileSpawnRequest CreateSpawnData(Component source)
            {
                return new ProjectileSpawnRequest
                {
                    Source = source,
                    Position = Vector3.zero,
                    Velocity = Vector3.right,
                    Radius = 0.1f,
                    Lifetime = 1f,
                    Damage = 1f,
                    TeamId = 1,
                    HitLayers = 0,
                    Use2D = true,
                };
            }



            private static ProjectileSnapshot CreateSnapshot(int projectileId)
            {
                return new ProjectileSnapshot(
                    projectileId,
                    Vector3.zero,
                    Vector3.right,
                    0.1f,
                    1f,
                    1f,
                    1,
                    true);
            }
        }



        private sealed class ProjectileEventStreamReceiverProbe : MonoBehaviour, IProjectileSpawnReceiver, IProjectileContactReceiver, IProjectileDespawnReceiver
        {
            public int SpawnCount { get; private set; }
            public int ContactCount { get; private set; }
            public int DespawnCount { get; private set; }



            public void OnProjectileSpawned(in ProjectileSnapshot snapshot)
            {
                SpawnCount++;
            }



            public void OnProjectileContacted(in ProjectileContactPayload contact)
            {
                ContactCount++;
            }



            public void OnProjectileDespawned(in ProjectileSnapshot snapshot)
            {
                DespawnCount++;
            }
        }



        private sealed class ProjectileGrazeReceiverProbe : MonoBehaviour, IProjectileGrazeReceiver
        {
            public int GrazeCount { get; private set; }
            public ProjectileGrazePayload LastGraze { get; private set; }



            public bool TryReceiveProjectileGraze(in ProjectileGrazePayload graze)
            {
                GrazeCount++;
                LastGraze = graze;
                return true;
            }
        }
    }
}
