using NUnit.Framework;
using UnityEngine;
using Pan.HighDensityProjectile;



namespace Pan.HighDensityProjectile.Tests
{
    public sealed class ProjectilePatternImmediateIdEditModeTests
    {
        [Test]
        public void ProjectileSpawnUtility_EarlyFailureClearsOnlyRequestedIdRange()
        {
            GameObject root = new GameObject("ProjectileSpawnUtilityEarlyFailureRoot");

            try
            {
                ManagedProjectileBody body = root.AddComponent<ManagedProjectileBody>();
                body.Initialize(4, 2);
                var spawnRequests = new ProjectileSpawnRequest[2];
                int[] projectileIds = { 99, 88, 77 };

                int missingBodyCount = ProjectileSpawnUtility.TrySpawnMany(
                    null,
                    spawnRequests,
                    2,
                    projectileIds);

                Assert.AreEqual(0, missingBodyCount);
                CollectionAssert.AreEqual(new[] { 0, 0, 77 }, projectileIds);

                projectileIds[0] = 66;
                projectileIds[1] = 55;

                int missingBufferCount = ProjectileSpawnUtility.TrySpawnMany(
                    body,
                    null,
                    2,
                    projectileIds);

                Assert.AreEqual(0, missingBufferCount);
                CollectionAssert.AreEqual(new[] { 0, 0, 77 }, projectileIds);

                projectileIds[0] = 44;
                projectileIds[1] = 33;

                Assert.AreEqual(
                    0,
                    ProjectileSpawnUtility.TrySpawnMany(body, spawnRequests, 0, projectileIds));
                Assert.AreEqual(
                    0,
                    ProjectileSpawnUtility.TrySpawnMany(body, spawnRequests, -1, projectileIds));
                CollectionAssert.AreEqual(new[] { 44, 33, 77 }, projectileIds);

                Assert.AreEqual(
                    0,
                    ProjectileSpawnUtility.TrySpawnMany(null, spawnRequests, 2, null));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }



        [Test]
        public void ProjectilePatternRunner_ManualStepEntryCanReturnImmediateBurstIds()
        {
            GameObject root = new GameObject("ProjectilePatternRunnerImmediateIdRoot");
            ProjectilePatternProfile pattern = null;
            ProjectileEmitterProfile firstEmitter = null;
            ProjectileEmitterProfile secondEmitter = null;

            try
            {
                ManagedProjectileBody manager = root.AddComponent<ManagedProjectileBody>();
                ProjectileEmitter emitter = root.AddComponent<ProjectileEmitter>();
                ProjectilePatternRunner runner = root.AddComponent<ProjectilePatternRunner>();
                manager.Initialize(8, 4);
                emitter.Initialize(manager);

                pattern = CreateTwoStepImmediatePattern(out firstEmitter, out secondEmitter);
                runner.Initialize(emitter, pattern);

                int[] firstIds = { 99, 99, 99 };
                int[] secondIds = { 77, 77 };

                Assert.IsTrue(runner.Restart(firstIds));
                Assert.AreEqual(3, runner.LastEnterSpawnedCount);
                Assert.AreEqual(3, manager.ActiveCount);
                AssertPositiveIds(firstIds, 3);

                Assert.IsTrue(runner.AdvanceToNextStep(secondIds));
                Assert.AreEqual(2, runner.LastEnterSpawnedCount);
                Assert.AreEqual(5, manager.ActiveCount);
                AssertPositiveIds(secondIds, 2);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(pattern);
                Object.DestroyImmediate(firstEmitter);
                Object.DestroyImmediate(secondEmitter);
            }
        }



        [Test]
        public void ProjectilePatternRunner_InvalidStepClearsImmediateBurstIds()
        {
            GameObject root = new GameObject("ProjectilePatternRunnerInvalidStepIdRoot");
            ProjectilePatternProfile pattern = null;
            ProjectileEmitterProfile firstEmitter = null;
            ProjectileEmitterProfile secondEmitter = null;

            try
            {
                ManagedProjectileBody manager = root.AddComponent<ManagedProjectileBody>();
                ProjectileEmitter emitter = root.AddComponent<ProjectileEmitter>();
                ProjectilePatternRunner runner = root.AddComponent<ProjectilePatternRunner>();
                manager.Initialize(8, 4);
                emitter.Initialize(manager);

                pattern = CreateTwoStepImmediatePattern(out firstEmitter, out secondEmitter);
                runner.Initialize(emitter, pattern);

                int[] projectileIds = { 99, 99, 99 };

                Assert.IsTrue(runner.Restart(projectileIds));
                Assert.AreEqual(3, runner.LastEnterSpawnedCount);
                AssertPositiveIds(projectileIds, 3);

                Assert.IsFalse(runner.TryEnterStep(99, false, projectileIds));
                Assert.AreEqual(0, runner.LastEnterSpawnedCount);
                AssertZeroIds(projectileIds, 3);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(pattern);
                Object.DestroyImmediate(firstEmitter);
                Object.DestroyImmediate(secondEmitter);
            }
        }



        [Test]
        public void ProjectilePatternController_PlayPatternCanReturnImmediateBurstIds()
        {
            GameObject root = new GameObject("ProjectilePatternControllerImmediateIdRoot");
            ProjectilePatternProfile pattern = null;
            ProjectileEmitterProfile firstEmitter = null;
            ProjectileEmitterProfile secondEmitter = null;

            try
            {
                ManagedProjectileBody manager = root.AddComponent<ManagedProjectileBody>();
                ProjectilePatternController controller = root.AddComponent<ProjectilePatternController>();
                manager.Initialize(8, 4);
                pattern = CreateTwoStepImmediatePattern(out firstEmitter, out secondEmitter);

                controller.ProjectileBodySource = manager;
                controller.PatternProfile = pattern;
                controller.CreateRenderProxy = false;

                int[] firstIds = { 99, 99, 99 };
                int[] secondIds = { 77, 77 };

                Assert.IsTrue(controller.PlayPattern(firstIds));
                Assert.AreEqual(3, controller.LastSpawnedCount);
                Assert.AreEqual(3, manager.ActiveCount);
                AssertPositiveIds(firstIds, 3);
                Assert.IsTrue(controller.TryGetProjectileSnapshotById(firstIds[0], out ProjectileSnapshot firstSnapshot));
                Assert.AreEqual(firstIds[0], firstSnapshot.ProjectileId);
                Assert.AreEqual(3, controller.CopyProjectileSnapshots(new ProjectileSnapshot[8]));
                Assert.IsTrue(controller.DespawnProjectile(firstIds[0]));
                Assert.AreEqual(2, manager.ActiveCount);
                Assert.IsFalse(controller.TryGetProjectileSnapshotById(firstIds[0], out _));

                Assert.IsTrue(controller.AdvanceToNextStep(secondIds));
                Assert.AreEqual(2, controller.LastSpawnedCount);
                Assert.AreEqual(4, manager.ActiveCount);
                AssertPositiveIds(secondIds, 2);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(pattern);
                Object.DestroyImmediate(firstEmitter);
                Object.DestroyImmediate(secondEmitter);
            }
        }



        [Test]
        public void ProjectilePatternController_PlayPatternCanReturnImmediateBurstHandles()
        {
            GameObject root = new GameObject("ProjectilePatternControllerImmediateHandleRoot");
            ProjectilePatternProfile pattern = null;
            ProjectileEmitterProfile firstEmitter = null;
            ProjectileEmitterProfile secondEmitter = null;

            try
            {
                ManagedProjectileBody manager = root.AddComponent<ManagedProjectileBody>();
                ProjectilePatternController controller = root.AddComponent<ProjectilePatternController>();
                manager.Initialize(8, 4);
                pattern = CreateTwoStepImmediatePattern(out firstEmitter, out secondEmitter);

                controller.ProjectileBodySource = manager;
                controller.PatternProfile = pattern;
                controller.CreateRenderProxy = false;

                ProjectileHandle[] firstHandles = new ProjectileHandle[3];
                ProjectileHandle[] secondHandles = new ProjectileHandle[2];

                Assert.IsTrue(controller.PlayPattern(firstHandles));
                Assert.AreEqual(3, controller.LastSpawnedCount);
                Assert.AreEqual(3, manager.ActiveCount);
                AssertCreatedHandles(firstHandles, 3);
                Assert.IsTrue(controller.TryGetProjectileSnapshot(firstHandles[0], out ProjectileSnapshot firstSnapshot));
                Assert.AreEqual(firstHandles[0].ProjectileId, firstSnapshot.ProjectileId);
                Assert.IsTrue(controller.DespawnProjectile(firstHandles[0]));
                Assert.AreEqual(2, manager.ActiveCount);
                Assert.IsTrue(firstHandles[0].IsCreated);
                Assert.IsFalse(firstHandles[0].IsAlive);
                Assert.IsFalse(controller.TryGetProjectileSnapshot(firstHandles[0], out _));
                Assert.IsFalse(controller.DespawnProjectile(firstHandles[0]));
                Assert.IsFalse(firstHandles[0].TryGetSnapshot(out _));

                Assert.IsTrue(controller.AdvanceToNextStep(secondHandles));
                Assert.AreEqual(2, controller.LastSpawnedCount);
                Assert.AreEqual(4, manager.ActiveCount);
                AssertCreatedHandles(secondHandles, 2);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(pattern);
                Object.DestroyImmediate(firstEmitter);
                Object.DestroyImmediate(secondEmitter);
            }
        }



        [Test]
        public void ProjectilePatternController_ClearsImmediateHandlesWhenStepDoesNotFire()
        {
            GameObject root = new GameObject("ProjectilePatternControllerNoFireHandleRoot");
            ProjectilePatternProfile pattern = null;
            ProjectileEmitterProfile firstEmitter = null;
            ProjectileEmitterProfile secondEmitter = null;

            try
            {
                ManagedProjectileBody manager = root.AddComponent<ManagedProjectileBody>();
                ProjectilePatternController controller = root.AddComponent<ProjectilePatternController>();
                manager.Initialize(8, 4);
                pattern = CreateTwoStepImmediatePattern(
                    out firstEmitter,
                    out secondEmitter,
                    secondStepFireBurst: false);

                controller.ProjectileBodySource = manager;
                controller.PatternProfile = pattern;
                controller.CreateRenderProxy = false;

                ProjectileHandle[] projectileHandles =
                {
                    new ProjectileHandle(null, 77),
                    new ProjectileHandle(null, 88),
                    new ProjectileHandle(null, 99),
                };

                Assert.IsTrue(controller.PlayPattern(projectileHandles));
                Assert.AreEqual(3, controller.LastSpawnedCount);
                AssertCreatedHandles(projectileHandles, 3);

                Assert.IsTrue(controller.AdvanceToNextStep(projectileHandles));
                Assert.AreEqual(0, controller.LastSpawnedCount);
                AssertDefaultHandles(projectileHandles, 3);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(pattern);
                Object.DestroyImmediate(firstEmitter);
                Object.DestroyImmediate(secondEmitter);
            }
        }



        [Test]
        public void ProjectilePatternController_StepVisualFrameOverrideCanReuseEmitterProfile()
        {
            GameObject root = new GameObject("ProjectilePatternControllerVisualFrameOverrideRoot");
            ProjectilePatternProfile pattern = null;
            ProjectileEmitterProfile emitterProfile = null;

            try
            {
                ManagedProjectileBody manager = root.AddComponent<ManagedProjectileBody>();
                ProjectilePatternController controller = root.AddComponent<ProjectilePatternController>();
                manager.Initialize(8, 4);

                emitterProfile = CreateEmitterProfile(1);
                emitterProfile.VisualFrameIndex = 0;

                pattern = ScriptableObject.CreateInstance<ProjectilePatternProfile>();
                pattern.Loop = false;
                pattern.SetSteps(new[]
                {
                    new ProjectilePatternStep
                    {
                        EmitterProfile = emitterProfile,
                        Duration = 1f,
                        FireBurstOnEnter = true,
                        ResetEmissionStateOnEnter = true,
                        OverrideVisualFrameIndex = true,
                        VisualFrameIndex = 1,
                    },
                    new ProjectilePatternStep
                    {
                        EmitterProfile = emitterProfile,
                        Duration = 1f,
                        FireBurstOnEnter = true,
                        ResetEmissionStateOnEnter = true,
                        OverrideVisualFrameIndex = true,
                        VisualFrameIndex = 2,
                    },
                });

                controller.ProjectileBodySource = manager;
                controller.PatternProfile = pattern;
                controller.CreateRenderProxy = false;

                int[] projectileIds = { 99 };
                Assert.IsTrue(controller.PlayPattern(projectileIds));
                Assert.AreEqual(1, controller.LastSpawnedCount);
                Assert.IsTrue(controller.TryGetProjectileSnapshotById(projectileIds[0], out ProjectileSnapshot firstSnapshot));
                Assert.AreEqual(1, firstSnapshot.VisualFrameIndex);

                Assert.IsTrue(controller.AdvanceToNextStep(projectileIds));
                Assert.AreEqual(1, controller.LastSpawnedCount);
                Assert.IsTrue(controller.TryGetProjectileSnapshotById(projectileIds[0], out ProjectileSnapshot secondSnapshot));
                Assert.AreEqual(2, secondSnapshot.VisualFrameIndex);
                Assert.AreEqual(0, emitterProfile.VisualFrameIndex);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(pattern);
                Object.DestroyImmediate(emitterProfile);
            }
        }



        private static ProjectilePatternProfile CreateTwoStepImmediatePattern(
            out ProjectileEmitterProfile firstEmitter,
            out ProjectileEmitterProfile secondEmitter,
            bool secondStepFireBurst = true)
        {
            firstEmitter = CreateEmitterProfile(3);
            secondEmitter = CreateEmitterProfile(2);

            var firstStep = new ProjectilePatternStep
            {
                EmitterProfile = firstEmitter,
                Duration = 1f,
                FireBurstOnEnter = true,
                ResetEmissionStateOnEnter = true,
            };
            var secondStep = new ProjectilePatternStep
            {
                EmitterProfile = secondEmitter,
                Duration = 1f,
                FireBurstOnEnter = secondStepFireBurst,
                ResetEmissionStateOnEnter = true,
            };

            ProjectilePatternProfile pattern = ScriptableObject.CreateInstance<ProjectilePatternProfile>();
            pattern.Loop = false;
            pattern.SetSteps(new[] { firstStep, secondStep });
            return pattern;
        }



        private static ProjectileEmitterProfile CreateEmitterProfile(int projectileCount)
        {
            ProjectileEmitterProfile profile = ScriptableObject.CreateInstance<ProjectileEmitterProfile>();
            profile.ProjectilesPerBurst = projectileCount;
            profile.UseTransformRotation = false;
            profile.Speed = 0f;
            profile.Radius = 0.05f;
            profile.Lifetime = 3f;
            return profile;
        }



        private static void AssertPositiveIds(int[] projectileIds, int count)
        {
            Assert.IsNotNull(projectileIds);
            for (int i = 0; i < count; i++)
            {
                Assert.Greater(projectileIds[i], 0);
            }
        }



        private static void AssertZeroIds(int[] projectileIds, int count)
        {
            Assert.IsNotNull(projectileIds);
            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(0, projectileIds[i]);
            }
        }



        private static void AssertCreatedHandles(ProjectileHandle[] projectileHandles, int count)
        {
            Assert.IsNotNull(projectileHandles);
            for (int i = 0; i < count; i++)
            {
                Assert.IsTrue(projectileHandles[i].IsCreated);
                Assert.IsTrue(projectileHandles[i].IsAlive);
            }
        }



        private static void AssertDefaultHandles(ProjectileHandle[] projectileHandles, int count)
        {
            Assert.IsNotNull(projectileHandles);
            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(default(ProjectileHandle), projectileHandles[i]);
            }
        }
    }
}
