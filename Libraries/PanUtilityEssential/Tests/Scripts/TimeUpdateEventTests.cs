using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Pan.Util.Tests
{
    public class TimeUpdateEventTests
    {
        private TimeUpdateEvent updater;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            updater?.Disable(true);
            updater = null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator ConstructorWithDotweenMode_Updates()
        {
            var updateCount = 0;
            updater = new TimeUpdateEvent(TimeUpdateEvent.TimeUpdateMode.DOTween);
            updater.Enable(() => 0.1f, null, (_, _) => updateCount++);
            updater.EndTime = -1f;

            updater.StartWorking();
            yield return null;

            Assert.Greater(updateCount, 0);
        }

        [Test]
        public void Enable_ModeOptionalDefault_RemainsDotween()
        {
            MethodInfo enableMethod = typeof(TimeUpdateEvent).GetMethod(nameof(TimeUpdateEvent.Enable));
            Assert.That(enableMethod, Is.Not.Null);

            ParameterInfo modeParameter = enableMethod.GetParameters()[3];
            Assert.That(modeParameter.HasDefaultValue, Is.True);
            Assert.That(
                System.Convert.ToInt32(modeParameter.DefaultValue),
                Is.EqualTo((int)TimeUpdateEvent.TimeUpdateMode.DOTween));
        }

        [UnityTest]
        public IEnumerator ConstructorWithUniTaskMode_PreservesModeWhenEnableReceivesExplicitNull()
        {
            var updateCount = 0;
            updater = new TimeUpdateEvent(TimeUpdateEvent.TimeUpdateMode.UniTask);
            updater.Enable(() => 0.1f, null, (_, _) => updateCount++, mode: null);
            updater.EndTime = -1f;

            updater.StartWorking();
            yield return null;

            Assert.AreEqual(TimeUpdateEvent.TimeUpdateMode.UniTask, updater.CurrentUpdateMode);
            Assert.Greater(updateCount, 0);
        }

        [UnityTest]
        public IEnumerator DisableWithoutKill_PausesAndCanReuseSequence()
        {
            var updateCount = 0;
            updater = new TimeUpdateEvent(TimeUpdateEvent.TimeUpdateMode.DOTween);
            updater.Enable(() => 0.1f, null, (_, _) => updateCount++);
            updater.EndTime = -1f;
            updater.StartWorking();
            yield return null;

            updater.Disable(false);
            var countAfterDisable = updateCount;
            yield return null;

            Assert.AreEqual(countAfterDisable, updateCount);

            updater.Enable(() => 0.1f, null, (_, _) => updateCount++);
            updater.EndTime = -1f;
            updater.StartWorking();
            yield return null;

            Assert.Greater(updateCount, countAfterDisable);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ShutdownWithSuccess_InvokesSuccessAndEndOnce(bool isLoop)
        {
            var successCount = 0;
            var endCount = 0;
            updater = new TimeUpdateEvent(TimeUpdateEvent.TimeUpdateMode.DOTween);
            updater.Enable(() => 0.1f, () => successCount++);
            updater.EndEvent += () => endCount++;
            updater.EndTime = -1f;
            updater.IsLoop = isLoop;
            updater.LoopCondition = () => true;
            updater.StartWorking();

            updater.ShutDown(true);

            Assert.AreEqual(1, successCount);
            Assert.AreEqual(1, endCount);
            Assert.IsFalse(updater.IsWorking);
        }
    }

    public sealed class MissingAddressableSingleton :
        SingleTon_ScriptableObject<MissingAddressableSingleton>
    {
    }

    public class ScriptableSingletonHandleTests
    {
        [Test]
        public void MissingSingleton_RepeatedFailuresAndCacheClearsDoNotRetainHandles()
        {
            bool previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;

            try
            {
                Assert.That(MissingAddressableSingleton.LoadSingleTon(true), Is.Null);
                Assert.That(GetHandleIsValid(), Is.False);
                Assert.That(MissingAddressableSingleton.LoadSingleTon(true), Is.Null);
                Assert.That(GetHandleIsValid(), Is.False);

                Assert.DoesNotThrow(MissingAddressableSingleton.UnLoadSingleTon);
                Assert.DoesNotThrow(MissingAddressableSingleton.UnLoadSingleTon);
                Assert.That(GetHandleIsValid(), Is.False);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
            }
        }

        private static bool GetHandleIsValid()
        {
            FieldInfo field = typeof(SingleTon_ScriptableObject<MissingAddressableSingleton>)
                .GetField("instanceHandle", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);

            object handle = field.GetValue(null);
            MethodInfo method = handle.GetType().GetMethod("IsValid");
            Assert.That(method, Is.Not.Null);
            return (bool)method.Invoke(handle, null);
        }
    }
}
