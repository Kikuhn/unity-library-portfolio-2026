using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Pan.HighDensityProjectile;



namespace Pan.HighDensityProjectile.Tests
{
    public sealed class ProjectileEmitterInspectorEditModeTests
    {
        private const BindingFlags InstancePrivateFlags = BindingFlags.Instance | BindingFlags.NonPublic;



        [Test]
        public void ProjectileEmitter_OnValidateClampsSerializedSpawnSettingsAndSpawnBurstUsesClampedValues()
        {
            GameObject targetObject = new GameObject("ProjectileEmitterOnValidateTarget");
            try
            {
                ManagedProjectileBody manager = targetObject.AddComponent<ManagedProjectileBody>();
                ProjectileEmitter emitter = targetObject.AddComponent<ProjectileEmitter>();
                manager.Initialize(2, 4);
                emitter.Initialize(manager);
                emitter.UseTransformRotation = false;

                SetPrivateField(emitter, "burstsPerSecond", -1f);
                SetPrivateField(emitter, "maxBurstsPerTick", 0);
                SetPrivateField(emitter, "projectilesPerBurst", 0);
                SetPrivateField(emitter, "speed", -7f);
                SetPrivateField(emitter, "radius", -0.3f);
                SetPrivateField(emitter, "lifetime", -2f);
                SetPrivateField(emitter, "damage", -5f);
                SetPrivateField(emitter, "visualFrameIndex", -1);
                SetPrivateField(emitter, "fixedWorldScale", -4f);

                typeof(ProjectileEmitter)
                    .GetMethod("OnValidate", InstancePrivateFlags)
                    ?.Invoke(emitter, null);

                Assert.AreEqual(0f, emitter.BurstsPerSecond);
                Assert.AreEqual(1, emitter.MaxBurstsPerTick);
                Assert.AreEqual(1, emitter.ProjectilesPerBurst);
                Assert.AreEqual(0f, emitter.Speed);
                Assert.AreEqual(0f, emitter.Radius);
                Assert.AreEqual(0f, emitter.Lifetime);
                Assert.AreEqual(0f, emitter.Damage);
                Assert.AreEqual(0, emitter.VisualFrameIndex);
                Assert.AreEqual(0f, emitter.FixedWorldScale);
                Assert.AreEqual(0, emitter.Tick(1f));

                int spawned = emitter.SpawnBurst(Vector3.zero, 0f);

                Assert.AreEqual(1, spawned);
                Assert.IsTrue(manager.TryGetSnapshot(0, out ProjectileSnapshot snapshot));
                Assert.AreEqual(Vector3.zero, snapshot.Velocity);
                Assert.AreEqual(0f, snapshot.Radius);
                Assert.AreEqual(0f, snapshot.RemainingLifetime);
                Assert.AreEqual(0f, snapshot.Damage);
                Assert.AreEqual(0, snapshot.VisualFrameIndex);
                Assert.AreEqual(0f, snapshot.Visual.FixedWorldScale);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
            }
        }



        [Test]
        public void ProjectileEmitter_OnDisableResetsEmissionStateWithoutClearingSharedBodyByDefault()
        {
            GameObject targetObject = new GameObject("ProjectileEmitterDisableResetTarget");
            try
            {
                ManagedProjectileBody manager = targetObject.AddComponent<ManagedProjectileBody>();
                ProjectileEmitter emitter = targetObject.AddComponent<ProjectileEmitter>();
                manager.Initialize(4, 4);
                emitter.Initialize(manager);
                emitter.UseTransformRotation = false;
                emitter.ProjectilesPerBurst = 2;
                emitter.BurstsPerSecond = 10f;
                emitter.ResetEmissionStateOnDisable = true;
                emitter.ClearProjectilesOnDisable = false;

                Assert.AreEqual(0, emitter.Tick(0.05f));
                Assert.AreEqual(0.5f, emitter.PendingBurstFraction);
                Assert.AreEqual(2, emitter.SpawnBurst(Vector3.zero, 0f));
                Assert.AreEqual(1, emitter.TotalBurstsEmitted);
                Assert.AreEqual(2, emitter.TotalProjectilesEmitted);
                Assert.AreEqual(2, manager.ActiveCount);

                emitter.RunDisableForDiagnostics();

                Assert.AreEqual(0f, emitter.PendingBurstFraction);
                Assert.AreEqual(0, emitter.TotalBurstsEmitted);
                Assert.AreEqual(0, emitter.TotalProjectilesEmitted);
                Assert.AreEqual(2, manager.ActiveCount);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
            }
        }



        [Test]
        public void ProjectileEmitter_OnDisableCanClearProjectilesWhenExplicitlyConfigured()
        {
            GameObject targetObject = new GameObject("ProjectileEmitterDisableClearTarget");
            try
            {
                ManagedProjectileBody manager = targetObject.AddComponent<ManagedProjectileBody>();
                ProjectileEmitter emitter = targetObject.AddComponent<ProjectileEmitter>();
                manager.Initialize(4, 4);
                emitter.Initialize(manager);
                emitter.UseTransformRotation = false;
                emitter.ProjectilesPerBurst = 2;
                emitter.ResetEmissionStateOnDisable = false;
                emitter.ClearProjectilesOnDisable = true;

                Assert.AreEqual(2, emitter.SpawnBurst(Vector3.zero, 0f));
                Assert.AreEqual(1, emitter.TotalBurstsEmitted);
                Assert.AreEqual(2, emitter.TotalProjectilesEmitted);
                Assert.AreEqual(2, manager.ActiveCount);

                emitter.RunDisableForDiagnostics();

                Assert.AreEqual(0, manager.ActiveCount);
                Assert.AreEqual(2, manager.TotalDespawned);
                Assert.AreEqual(1, emitter.TotalBurstsEmitted);
                Assert.AreEqual(2, emitter.TotalProjectilesEmitted);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
            }
        }



        [Test]
        public void ProjectileEmitterProfile_ClampsNegativeDamageInSetterAndOnValidate()
        {
            ProjectileEmitterProfile profile = ScriptableObject.CreateInstance<ProjectileEmitterProfile>();
            try
            {
                profile.Damage = -3f;
                Assert.AreEqual(0f, profile.Damage);

                typeof(ProjectileEmitterProfile)
                    .GetField("damage", InstancePrivateFlags)
                    ?.SetValue(profile, -8f);
                typeof(ProjectileEmitterProfile)
                    .GetMethod("OnValidate", InstancePrivateFlags)
                    ?.Invoke(profile, null);

                Assert.AreEqual(0f, profile.Damage);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }



        [Test]
        public void ProjectileEmitter_CanReadAndDespawnSpawnedProjectileIdsWithoutConcreteBodyAccess()
        {
            GameObject targetObject = new GameObject("ProjectileEmitterSnapshotForwardingTarget");
            try
            {
                ManagedProjectileBody manager = targetObject.AddComponent<ManagedProjectileBody>();
                ProjectileEmitter emitter = targetObject.AddComponent<ProjectileEmitter>();
                manager.Initialize(4, 4);
                emitter.Initialize(manager);
                emitter.UseTransformRotation = false;
                emitter.ProjectilesPerBurst = 2;

                int[] projectileIds = new int[2];
                ProjectileSnapshot[] snapshots = new ProjectileSnapshot[4];

                int spawned = emitter.SpawnBurst(Vector3.one, 0f, projectileIds);

                Assert.AreEqual(2, spawned);
                Assert.Greater(projectileIds[0], 0);
                Assert.IsTrue(emitter.TryGetProjectileSnapshotById(projectileIds[0], out ProjectileSnapshot firstSnapshot));
                Assert.AreEqual(projectileIds[0], firstSnapshot.ProjectileId);
                Assert.AreEqual(2, emitter.CopyProjectileSnapshots(snapshots));

                Assert.IsTrue(emitter.DespawnProjectile(projectileIds[0]));
                Assert.AreEqual(1, manager.ActiveCount);
                Assert.IsFalse(emitter.TryGetProjectileSnapshotById(projectileIds[0], out _));
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
            }
        }



        [Test]
        public void ProjectileEmitter_SpawnBurstCanReturnProjectileHandles()
        {
            GameObject targetObject = new GameObject("ProjectileEmitterHandleTarget");
            try
            {
                ManagedProjectileBody manager = targetObject.AddComponent<ManagedProjectileBody>();
                ProjectileEmitter emitter = targetObject.AddComponent<ProjectileEmitter>();
                manager.Initialize(4, 4);
                emitter.Initialize(manager);
                emitter.UseTransformRotation = false;
                emitter.ProjectilesPerBurst = 2;

                ProjectileHandle[] projectileHandles = new ProjectileHandle[2];

                int spawned = emitter.SpawnBurst(Vector3.one, 0f, projectileHandles);

                Assert.AreEqual(2, spawned);
                Assert.IsTrue(projectileHandles[0].IsCreated);
                Assert.IsTrue(projectileHandles[0].IsAlive);
                Assert.IsTrue(emitter.TryGetProjectileSnapshot(projectileHandles[0], out ProjectileSnapshot firstSnapshot));
                Assert.AreEqual(projectileHandles[0].ProjectileId, firstSnapshot.ProjectileId);
                Assert.IsTrue(emitter.DespawnProjectile(projectileHandles[0]));
                Assert.AreEqual(1, manager.ActiveCount);
                Assert.IsTrue(projectileHandles[0].IsCreated);
                Assert.IsFalse(projectileHandles[0].IsAlive);
                Assert.IsFalse(emitter.TryGetProjectileSnapshot(projectileHandles[0], out _));
                Assert.IsFalse(emitter.DespawnProjectile(projectileHandles[0]));
                Assert.IsFalse(projectileHandles[0].TryGetSnapshot(out _));
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
            }
        }



        [Test]
        public void ProjectileEmitter_HandleOverloadsRejectForeignBodyHandles()
        {
            GameObject ownerObject = new GameObject("ProjectileEmitterHandleOwner");
            GameObject foreignObject = new GameObject("ProjectileEmitterForeignHandleOwner");
            try
            {
                ManagedProjectileBody ownerManager = ownerObject.AddComponent<ManagedProjectileBody>();
                ManagedProjectileBody foreignManager = foreignObject.AddComponent<ManagedProjectileBody>();
                ProjectileEmitter emitter = ownerObject.AddComponent<ProjectileEmitter>();
                ownerManager.Initialize(4, 4);
                foreignManager.Initialize(4, 4);
                emitter.Initialize(ownerManager);

                int foreignId = foreignManager.Spawn(new ProjectileSpawnRequest
                {
                    Source = foreignManager,
                    Position = Vector3.one,
                    Velocity = Vector3.right,
                    Radius = 0.1f,
                    Lifetime = 1f,
                    Damage = 1f,
                    TeamId = 1,
                    HitLayers = 0,
                    Use2D = true,
                });
                var foreignHandle = new ProjectileHandle(foreignManager, foreignId);

                Assert.IsTrue(foreignHandle.IsAlive);
                Assert.IsFalse(emitter.TryGetProjectileSnapshot(foreignHandle, out _));
                Assert.IsFalse(emitter.DespawnProjectile(foreignHandle));
                Assert.IsTrue(foreignManager.TryGetSnapshotById(foreignId, out _));
            }
            finally
            {
                Object.DestroyImmediate(ownerObject);
                Object.DestroyImmediate(foreignObject);
            }
        }



        [Test]
        public void ProjectileEmitter_SpawnBurstClearsReturnBuffersWhenNoBodyIsAvailable()
        {
            GameObject targetObject = new GameObject("ProjectileEmitterMissingBodyTarget");
            try
            {
                ProjectileEmitter emitter = targetObject.AddComponent<ProjectileEmitter>();
                emitter.UseTransformRotation = false;
                emitter.ProjectilesPerBurst = 2;

                int[] projectileIds = { 99, 99 };
                ProjectileHandle[] projectileHandles =
                {
                    new ProjectileHandle(null, 77),
                    new ProjectileHandle(null, 88),
                };

                Assert.AreEqual(0, emitter.SpawnBurst(Vector3.zero, 0f, projectileIds));
                Assert.AreEqual(0, projectileIds[0]);
                Assert.AreEqual(0, projectileIds[1]);

                Assert.AreEqual(0, emitter.SpawnBurst(Vector3.zero, 0f, projectileHandles));
                Assert.AreEqual(default(ProjectileHandle), projectileHandles[0]);
                Assert.AreEqual(default(ProjectileHandle), projectileHandles[1]);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
            }
        }



        [Test]
        public void ProjectileEmitterProfile_AppliesVisualFrameIndexToSpawnedSnapshots()
        {
            GameObject targetObject = new GameObject("ProjectileEmitterProfileVisualFrameTarget");
            ProjectileEmitterProfile profile = ScriptableObject.CreateInstance<ProjectileEmitterProfile>();
            try
            {
                ManagedProjectileBody manager = targetObject.AddComponent<ManagedProjectileBody>();
                ProjectileEmitter emitter = targetObject.AddComponent<ProjectileEmitter>();
                manager.Initialize(2, 4);
                emitter.Initialize(manager);
                emitter.UseTransformRotation = false;

                profile.ProjectilesPerBurst = 1;
                profile.Speed = 0f;
                profile.Radius = 0.25f;
                profile.Lifetime = 1f;
                profile.VisualFrameIndex = 3;

                Assert.IsTrue(emitter.ApplyProfile(profile));
                Assert.AreEqual(3, emitter.VisualFrameIndex);

                int spawned = emitter.SpawnBurst(Vector3.zero, 0f);

                Assert.AreEqual(1, spawned);
                Assert.IsTrue(manager.TryGetSnapshot(0, out ProjectileSnapshot snapshot));
                Assert.AreEqual(3, snapshot.VisualFrameIndex);
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(targetObject);
            }
        }



        [Test]
        public void ProjectileEmitter_SpawnBurstCanReturnProjectileIdsThroughOptionalBuffer()
        {
            GameObject targetObject = new GameObject("ProjectileEmitterIdBufferTarget");
            try
            {
                ProjectilePartialBatchBodyProbe body = targetObject.AddComponent<ProjectilePartialBatchBodyProbe>();
                ProjectileEmitter emitter = targetObject.AddComponent<ProjectileEmitter>();
                emitter.Initialize(body);
                emitter.UseTransformRotation = false;
                emitter.ProjectilesPerBurst = 4;

                int[] projectileIds = { 99, 99, 99, 99 };

                int spawned = emitter.SpawnBurst(Vector3.zero, 0f, projectileIds);

                Assert.AreEqual(2, spawned);
                Assert.AreEqual(2, body.TotalSpawned);
                Assert.AreEqual(1, body.BatchSpawnCallCount);
                Assert.AreEqual(101, projectileIds[0]);
                Assert.AreEqual(102, projectileIds[1]);
                Assert.AreEqual(0, projectileIds[2]);
                Assert.AreEqual(0, projectileIds[3]);
                Assert.AreEqual(1, emitter.TotalBurstsEmitted);
                Assert.AreEqual(2, emitter.TotalProjectilesEmitted);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
            }
        }



        [Test]
        public void ProjectileEmitter_SourceOverrideIsStoredInSpawnedManagerSlot()
        {
            GameObject sourceObject = new GameObject("ProjectileEmitterGameplaySource");
            GameObject targetObject = new GameObject("ProjectileEmitterSourceOverrideTarget");
            try
            {
                ManagedProjectileBody manager = targetObject.AddComponent<ManagedProjectileBody>();
                ProjectileEmitter emitter = targetObject.AddComponent<ProjectileEmitter>();
                manager.Initialize(2, 4);
                emitter.Initialize(manager);
                emitter.UseTransformRotation = false;
                emitter.ProjectilesPerBurst = 1;
                emitter.ProjectileSourceOverride = sourceObject.transform;

                Assert.AreEqual(1, emitter.SpawnBurst(Vector3.zero, 0f));

                Assert.IsTrue(manager.TryGetSlotSourceForDiagnostics(0, out Component slotSource));
                Assert.AreSame(sourceObject.transform, slotSource);
            }
            finally
            {
                Object.DestroyImmediate(sourceObject);
                Object.DestroyImmediate(targetObject);
            }
        }



        [Test]
        public void ProjectileArchetypeProfile_ApplyToEmitterPreservesSpawnRequestFields()
        {
            GameObject targetObject = new GameObject("ProjectileArchetypeEmitterParityTarget");
            Texture2D texture = new Texture2D(1, 1);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
            ProjectileArchetypeProfile profile = ScriptableObject.CreateInstance<ProjectileArchetypeProfile>();
            try
            {
                ProjectilePartialBatchBodyProbe body = targetObject.AddComponent<ProjectilePartialBatchBodyProbe>();
                ProjectileEmitter emitter = targetObject.AddComponent<ProjectileEmitter>();
                emitter.Initialize(body);

                profile.Speed = 11f;
                profile.Radius = 0.3f;
                profile.Lifetime = 4f;
                profile.UseGravity = true;
                profile.Gravity = new Vector3(0f, -7f, 0f);
                profile.LinearDamping = 0.2f;
                profile.FrictionCoefficient = 0.1f;
                profile.Damage = 5f;
                profile.TeamId = 9;
                profile.HitLayers = 1 << 6;
                profile.ConsumeHitWithoutReceiver = true;
                profile.IncludeTriggers = false;
                profile.StrictCcdOverride = Pan.HighDensityElement.StrictCcdOverride2D.ForceOn;
                profile.StrictCcdThresholdRatio = 0.25f;
                profile.Sprite = sprite;
                profile.VisualFrameIndex = 3;
                profile.FixedWorldScale = 2.25f;
                profile.Use2D = true;

                profile.ApplyTo(emitter);
                emitter.ProjectilesPerBurst = 1;
                emitter.SpreadMode = ProjectileEmitterSpreadMode.Arc;
                emitter.BaseAngleDegrees = 0f;
                emitter.SpreadAngleDegrees = 0f;
                emitter.UseTransformRotation = false;

                ProjectileSpawnRequest directRequest = profile.CreateSpawnRequest(null, Vector3.one, Vector3.right);

                Assert.AreEqual(1, emitter.SpawnBurst(Vector3.one, 0f));
                ProjectileSpawnRequest emittedRequest = body.LastSpawnRequest;

                Assert.AreEqual(directRequest.Velocity, emittedRequest.Velocity);
                Assert.AreEqual(directRequest.Radius, emittedRequest.Radius);
                Assert.AreEqual(directRequest.LifetimeSeconds, emittedRequest.LifetimeSeconds);
                Assert.AreEqual(directRequest.UseGravity, emittedRequest.UseGravity);
                Assert.AreEqual(directRequest.Gravity, emittedRequest.Gravity);
                Assert.AreEqual(directRequest.LinearDamping, emittedRequest.LinearDamping);
                Assert.AreEqual(directRequest.FrictionCoefficient, emittedRequest.FrictionCoefficient);
                Assert.AreEqual(directRequest.Damage, emittedRequest.Damage);
                Assert.AreEqual(directRequest.TeamId, emittedRequest.TeamId);
                Assert.AreEqual(directRequest.HitLayers.value, emittedRequest.HitLayers.value);
                Assert.IsTrue(emittedRequest.Collision.ConsumeHitWithoutReceiver);
                Assert.IsFalse(emittedRequest.Collision.IncludeTriggers);
                Assert.AreEqual(directRequest.StrictCcdOverride, emittedRequest.StrictCcdOverride);
                Assert.AreEqual(directRequest.StrictCcdThresholdRatio, emittedRequest.StrictCcdThresholdRatio);
                Assert.AreSame(sprite, emittedRequest.Visual.Sprite);
                Assert.AreEqual(directRequest.Visual.FrameIndex, emittedRequest.Visual.FrameIndex);
                Assert.AreEqual(directRequest.Visual.FixedWorldScale, emittedRequest.Visual.FixedWorldScale);
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(targetObject);
            }
        }



        [Test]
        public void ManagedProjectileBody_SimulateIgnoresSourceColliderOverlap()
        {
            GameObject sourceObject = new GameObject("ProjectileSourceColliderOwner");
            GameObject targetObject = new GameObject("ProjectileSourceColliderIgnoreTarget");
            try
            {
                sourceObject.layer = LayerMask.NameToLayer("Default");
                BoxCollider2D sourceCollider = sourceObject.AddComponent<BoxCollider2D>();
                sourceCollider.size = Vector2.one;

                ManagedProjectileBody manager = targetObject.AddComponent<ManagedProjectileBody>();
                ProjectileEmitter emitter = targetObject.AddComponent<ProjectileEmitter>();
                manager.Initialize(2, 4);
                manager.SimulationBackend = ProjectileSimulationBackend.MainThread;
                manager.ConsumePhysicsHitWithoutReceiver = true;
                emitter.Initialize(manager);
                emitter.UseTransformRotation = false;
                emitter.ProjectileSourceOverride = sourceObject.transform;
                emitter.ProjectilesPerBurst = 1;
                emitter.Speed = 0f;
                emitter.Radius = 0.25f;
                emitter.Lifetime = 1f;
                emitter.HitLayers = 1 << sourceObject.layer;
                emitter.Use2D = true;

                Assert.AreEqual(1, emitter.SpawnBurst(sourceObject.transform.position, 0f));

                manager.Simulate(0.02f);

                Assert.AreEqual(1, manager.ActiveCount);
                Assert.AreEqual(0, manager.TotalHits);
            }
            finally
            {
                Object.DestroyImmediate(sourceObject);
                Object.DestroyImmediate(targetObject);
            }
        }



        private static void SetPrivateField<T>(ProjectileEmitter emitter, string fieldName, T value)
        {
            typeof(ProjectileEmitter)
                .GetField(fieldName, InstancePrivateFlags)
                ?.SetValue(emitter, value);
        }



        private sealed class ProjectilePartialBatchBodyProbe : MonoBehaviour, IPanProjectileBody, IProjectileBatchSpawnTarget
        {
            private const int MaxSpawnCount = 2;



            public int ActiveCount => TotalSpawned - TotalDespawned;
            public int Capacity => 4;
            public int TotalSpawned { get; private set; }
            public int TotalDespawned { get; private set; }
            public int TotalHits => 0;
            public int BatchSpawnCallCount { get; private set; }
            public ProjectileSpawnRequest LastSpawnRequest { get; private set; }



            public event ProjectileSnapshotHandler ProjectileSpawned { add { } remove { } }
            public event ProjectileContactHandler ProjectileContacted { add { } remove { } }
            public event ProjectileSnapshotHandler ProjectileDespawned { add { } remove { } }



            public int TrySpawnBatch(ProjectileSpawnRequest[] spawnDataBuffer, int count, int[] projectileIds = null)
            {
                BatchSpawnCallCount++;
                LastSpawnRequest = count > 0 ? spawnDataBuffer[0] : default;
                int spawned = Mathf.Min(MaxSpawnCount, count);
                for (int i = 0; i < spawned; i++)
                {
                    int projectileId = 101 + i;
                    if (projectileIds != null && i < projectileIds.Length)
                    {
                        projectileIds[i] = projectileId;
                    }

                    TotalSpawned++;
                }

                return spawned;
            }



            public bool TrySpawn(in ProjectileSpawnRequest spawnData, out int projectileId)
            {
                projectileId = 0;
                return false;
            }



            public int Spawn(in ProjectileSpawnRequest spawnData)
            {
                return 0;
            }



            public void Simulate(float deltaTime)
            {
            }



            public void ClearAll()
            {
                TotalDespawned = TotalSpawned;
            }



            public bool Despawn(int projectileId)
            {
                return false;
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
        }
    }
}
