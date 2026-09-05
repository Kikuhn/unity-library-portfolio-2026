using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Pan.StageGenerators.Tests
{
    public class GenerateRetryPolicyTests
    {
        [TestCase(0, 3, true)]
        [TestCase(1, 3, true)]
        [TestCase(2, 3, true)]
        [TestCase(3, 3, false)]
        [TestCase(0, 0, false)]
        [TestCase(0, -1, false)]
        [TestCase(1, -1, false)]
        public void CanReGenerate_UsesConfiguredCountAsAdditionalRetries(
            int currentRetryCount,
            int configuredRetryCount,
            bool expected)
        {
            Assert.That(
                StageGenerator.GenerateManager.CanReGenerate(currentRetryCount, configuredRetryCount),
                Is.EqualTo(expected));
        }

        [Test]
        public void RetryLimit_DoesNotRerollSeed()
        {
            GameObject root = new GameObject("StageGeneratorRetryPolicyTests");

            try
            {
                StageGenerator generator = root.AddComponent<StageGenerator>();
                generator.Refresh_StageGenerator(true);
                generator.GenerateInfoM.UseSeed_NextWillApplied = true;
                generator.GenerateInfoM.RefreshRandom(13579);
                SetPrivateField(generator.GenerateInfoM, "reGenerateMaxCount", 0);
                SetPrivateField(
                    generator.GenerateInfoM,
                    "failureGenerateEvent",
                    StageGenerator.EFailureGenerateEvent.Regenrate_Absolute);

                int seedBefore = generator.GenerateInfoM.Random.Seed;
                MethodInfo branch = typeof(StageGenerator.GenerateManager).GetMethod(
                    "ReGenerateBranch",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                object[] arguments =
                {
                    (int?)24680,
                    StageGenerator.EGenerateState.Failure,
                    0
                };

                Assert.That(branch, Is.Not.Null);
                Assert.That((bool?)branch.Invoke(generator.GenerateM, arguments), Is.False);
                Assert.That((int?)arguments[0], Is.EqualTo(24680));
                Assert.That(generator.GenerateInfoM.Random.Seed, Is.EqualTo(seedBefore));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RetryPolicy_TreatsCustomSeedAsFixedUntilAbsoluteReroll()
        {
            GameObject root = new GameObject("StageGeneratorCustomSeedRetryTests");

            try
            {
                StageGenerator generator = root.AddComponent<StageGenerator>();
                generator.Refresh_StageGenerator(true);
                SetPrivateField(generator.GenerateInfoM, "reGenerateMaxCount", 1);
                MethodInfo branch = typeof(StageGenerator.GenerateManager).GetMethod(
                    "ReGenerateBranch",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                SetPrivateField(
                    generator.GenerateInfoM,
                    "failureGenerateEvent",
                    StageGenerator.EFailureGenerateEvent.ReGenerate_WhenFixSeedNotUsed);
                object[] fixedArguments =
                {
                    (int?)24680,
                    StageGenerator.EGenerateState.Failure,
                    0
                };

                Assert.That(branch, Is.Not.Null);
                Assert.That((bool?)branch.Invoke(generator.GenerateM, fixedArguments), Is.False);
                Assert.That((int?)fixedArguments[0], Is.EqualTo(24680));

                SetPrivateField(
                    generator.GenerateInfoM,
                    "failureGenerateEvent",
                    StageGenerator.EFailureGenerateEvent.Regenrate_Absolute);
                generator.GenerateInfoM.RefreshRandom(13579);
                object[] absoluteArguments =
                {
                    (int?)24680,
                    StageGenerator.EGenerateState.Failure,
                    0
                };

                Assert.That((bool?)branch.Invoke(generator.GenerateM, absoluteArguments), Is.Null);
                Assert.That((int?)absoluteArguments[0], Is.EqualTo(generator.GenerateInfoM.Random.Seed));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DestroyStage_WithAbsoluteChildCleanup_RemovesStageChildren()
        {
            GameObject root = new GameObject("StageGeneratorDestroyTests");

            try
            {
                StageGenerator generator = root.AddComponent<StageGenerator>();
                generator.Refresh_StageGenerator(true);
                new GameObject("GeneratedChild").transform.SetParent(
                    generator.TransformM.StageParent,
                    false);

                Assert.That(generator.GenerateM.DestroyStage(true, true), Is.True);
                Assert.That(generator.TransformM.StageParent.childCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HallwaySafeAreaLength_PreservesSettingWhileCurrentValueTracksFeature()
        {
            var hallways = new StageGeneratorSetting.Hallways
            {
                UseCreateHallwaySafeArea = true,
                HallwaySafeAreaLength = 3
            };

            Assert.That(hallways.HallwaySafeAreaLength, Is.EqualTo(3));

            hallways.UseCreateHallwaySafeArea = false;

            Assert.That(hallways.HallwaySafeAreaLength, Is.EqualTo(3));
            Assert.That(hallways.CurrentHallwaySafeAreaLength, Is.Zero);

            hallways.UseCreateHallwaySafeArea = true;

            Assert.That(hallways.HallwaySafeAreaLength, Is.EqualTo(3));
            Assert.That(hallways.CurrentHallwaySafeAreaLength, Is.EqualTo(6));
        }

        [Test]
        public void DoorHallwayEdgeLength_UsesDoorSpecificSetting()
        {
            var hallways = new StageGeneratorSetting.Hallways
            {
                UseCreateHallwayEdge = true,
                HallwayEdgeLength = 1,
                UseCreateDoorHallwayEdge = true,
                DoorHallwayEdgeLength = 3
            };

            Assert.That(hallways.CurrentHallwayEdgeLength, Is.EqualTo(2));
            Assert.That(hallways.CurrentDoorHallwayEdgeLength, Is.EqualTo(6));

            hallways.GetDoorHallwayWidths_FromWidth(
                10,
                out int mainHallwayWidth,
                out int maxHallwayWidth);

            Assert.That(mainHallwayWidth, Is.EqualTo(4));
            Assert.That(maxHallwayWidth, Is.EqualTo(10));
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
