using NUnit.Framework;
using UnityEngine;
using Pan.HighDensityProjectile;



namespace Pan.HighDensityProjectile.Tests
{
    public sealed class ProjectileMotionContractEditModeTests
    {
        [Test]
        public void ProjectileMotionSpec_DefaultPreservesStraightMovement()
        {
            GameObject owner = new GameObject("ManagedProjectileBody_Motion_Default");
            try
            {
                ManagedProjectileBody body = owner.AddComponent<ManagedProjectileBody>();
                body.SimulationBackend = ProjectileSimulationBackend.MainThread;
                body.InitializeProjectileBody(new ProjectileBodyInitializationSettings(8, 4));

                ProjectileSpawnRequest request = CreateRequest(Vector3.zero, new Vector3(2f, 3f, 0f), ProjectileMotionSpec.None);
                Assert.IsTrue(body.TrySpawn(in request, out int projectileId));

                body.Simulate(0.25f);

                Assert.IsTrue(body.TryGetSnapshotById(projectileId, out ProjectileSnapshot snapshot));
                Assert.That(snapshot.Position.x, Is.EqualTo(0.5f).Within(0.0001f));
                Assert.That(snapshot.Position.y, Is.EqualTo(0.75f).Within(0.0001f));
                Assert.That(snapshot.Velocity.x, Is.EqualTo(2f).Within(0.0001f));
                Assert.That(snapshot.Velocity.y, Is.EqualTo(3f).Within(0.0001f));
                Assert.IsFalse(snapshot.UseGravity);
                Assert.That(snapshot.LinearDamping, Is.EqualTo(0f));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }



        [TestCase(ProjectileSimulationBackend.MainThread)]
        [TestCase(ProjectileSimulationBackend.JobsMovementOnlyWhenNoCollision)]
        [TestCase(ProjectileSimulationBackend.JobsMovementThenMainThreadCollision)]
        public void NativeAndManagedBodies_ApplyMotionSpecWithSameResult(ProjectileSimulationBackend managedBackend)
        {
            GameObject nativeOwner = new GameObject("NativeProjectileBody_Motion");
            GameObject managedOwner = new GameObject("ManagedProjectileBody_Motion");
            try
            {
                NativeProjectileBody nativeBody = nativeOwner.AddComponent<NativeProjectileBody>();
                nativeBody.InitializeProjectileBody(new ProjectileBodyInitializationSettings(8, 4));

                ManagedProjectileBody managedBody = managedOwner.AddComponent<ManagedProjectileBody>();
                managedBody.SimulationBackend = managedBackend;
                managedBody.InitializeProjectileBody(new ProjectileBodyInitializationSettings(8, 4));

                ProjectileMotionSpec motion = new ProjectileMotionSpec(true, new Vector3(0f, -10f, 0f), 1f, 0.5f);
                ProjectileSpawnRequest request = CreateRequest(Vector3.zero, new Vector3(10f, 0f, 0f), motion);
                Assert.IsTrue(nativeBody.TrySpawn(in request, out int nativeId));
                Assert.IsTrue(managedBody.TrySpawn(in request, out int managedId));

                nativeBody.Simulate(0.5f);
                managedBody.Simulate(0.5f);

                Assert.IsTrue(nativeBody.TryGetSnapshotById(nativeId, out ProjectileSnapshot nativeSnapshot));
                Assert.IsTrue(managedBody.TryGetSnapshotById(managedId, out ProjectileSnapshot managedSnapshot));

                AssertSnapshotClose(managedSnapshot, nativeSnapshot);
                Assert.IsTrue(nativeSnapshot.UseGravity);
                Assert.That(nativeSnapshot.Velocity.x, Is.LessThan(10f));
                Assert.That(nativeSnapshot.Velocity.y, Is.LessThan(0f));
            }
            finally
            {
                Object.DestroyImmediate(nativeOwner);
                Object.DestroyImmediate(managedOwner);
            }
        }



        [Test]
        public void ProjectileSpawnRequest_GravitySetterKeepsVectorWhenUseGravityIsEnabledLater()
        {
            ProjectileSpawnRequest request = CreateRequest(Vector3.zero, Vector3.right, ProjectileMotionSpec.None);
            Vector3 gravity = new Vector3(0f, -12f, 0f);

            request.Gravity = gravity;
            request.UseGravity = true;

            Assert.IsTrue(request.UseGravity);
            Assert.That(request.Gravity, Is.EqualTo(gravity));
        }



        [Test]
        public void TrySpawnBatch_PreservesMotionInSnapshots()
        {
            GameObject owner = new GameObject("ManagedProjectileBody_Motion_Batch");
            try
            {
                ManagedProjectileBody body = owner.AddComponent<ManagedProjectileBody>();
                body.InitializeProjectileBody(new ProjectileBodyInitializationSettings(8, 4));

                ProjectileMotionSpec motion = new ProjectileMotionSpec(true, new Vector3(0f, -4f, 0f), 0.25f, 0f);
                ProjectileSpawnRequest[] requests =
                {
                    CreateRequest(Vector3.zero, Vector3.right, motion),
                    CreateRequest(Vector3.one, Vector3.up, motion),
                };
                int[] projectileIds = new int[2];

                int spawned = body.TrySpawnBatch(requests, requests.Length, projectileIds);

                Assert.That(spawned, Is.EqualTo(2));
                Assert.IsTrue(body.TryGetSnapshotById(projectileIds[0], out ProjectileSnapshot snapshot));
                Assert.IsTrue(snapshot.UseGravity);
                Assert.That(snapshot.Gravity.y, Is.EqualTo(-4f));
                Assert.That(snapshot.LinearDamping, Is.EqualTo(0.25f));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }



        [Test]
        public void ProjectileArchetypeProfile_CreatesSpawnRequestWithMotionAndCoreStats()
        {
            ProjectileArchetypeProfile profile = ScriptableObject.CreateInstance<ProjectileArchetypeProfile>();
            try
            {
                profile.Speed = 12f;
                profile.Radius = 0.2f;
                profile.Lifetime = 5f;
                profile.UseGravity = true;
                profile.Gravity = new Vector3(0f, -9f, 0f);
                profile.LinearDamping = 0.1f;
                profile.Damage = 3f;
                profile.TeamId = 7;

                ProjectileSpawnRequest request = profile.CreateSpawnRequest(null, new Vector3(1f, 2f, 0f), Vector3.up);

                Assert.That(request.Position, Is.EqualTo(new Vector3(1f, 2f, 0f)));
                Assert.That(request.Velocity, Is.EqualTo(Vector3.up * 12f));
                Assert.That(request.Radius, Is.EqualTo(0.2f));
                Assert.That(request.LifetimeSeconds, Is.EqualTo(5f));
                Assert.IsTrue(request.UseGravity);
                Assert.That(request.Gravity.y, Is.EqualTo(-9f));
                Assert.That(request.LinearDamping, Is.EqualTo(0.1f));
                Assert.That(request.Damage, Is.EqualTo(3f));
                Assert.That(request.TeamId, Is.EqualTo(7));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }



        private static ProjectileSpawnRequest CreateRequest(Vector3 position, Vector3 velocity, ProjectileMotionSpec motion)
        {
            return new ProjectileSpawnRequest
            {
                Kinematic = new KinematicCircle2DState(position, velocity, 0.1f),
                Lifetime = TimedLifetimeState.Start(2f),
                Motion = motion,
                Collision = new CollisionLayerFilter(0, 0, false),
                Team = new TeamRelationTag(1),
                Hit = new HitDamageSpec(1f),
                Visual = new SpriteVisualSpec(null, 0, 0f),
            };
        }



        private static void AssertSnapshotClose(ProjectileSnapshot expected, ProjectileSnapshot actual)
        {
            Assert.That(actual.Position.x, Is.EqualTo(expected.Position.x).Within(0.0001f));
            Assert.That(actual.Position.y, Is.EqualTo(expected.Position.y).Within(0.0001f));
            Assert.That(actual.Velocity.x, Is.EqualTo(expected.Velocity.x).Within(0.0001f));
            Assert.That(actual.Velocity.y, Is.EqualTo(expected.Velocity.y).Within(0.0001f));
            Assert.That(actual.RemainingLifetime, Is.EqualTo(expected.RemainingLifetime).Within(0.0001f));
        }
    }
}
