using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Pan.StageGenerators.Tests
{
    public sealed class StageChildLifecycleProbe : MonoBehaviour
    {
        public int EnableCount { get; private set; }

        private void OnEnable()
        {
            EnableCount++;
        }
    }

    public class StageDestroyLifecycleTests
    {
        [UnityTest]
        public IEnumerator DestroyStage_DetachesAndDisablesChildrenBeforeDeferredDestroy()
        {
            GameObject root = new GameObject("StageGeneratorDestroyPlayModeTests");
            StageGenerator generator = root.AddComponent<StageGenerator>();
            generator.Refresh_StageGenerator(true);
            GameObject generatedChild = new GameObject("GeneratedChild");
            generatedChild.transform.SetParent(generator.TransformM.StageParent, false);

            Assert.That(generator.GenerateM.DestroyStage(true, true), Is.True);
            Assert.That(generator.TransformM.StageParent.childCount, Is.Zero);
            Assert.That(generatedChild.activeSelf, Is.False);

            yield return null;

            Assert.That(generatedChild == null, Is.True);
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator DestroyStage_DisablesInactiveParentChildBeforeDetaching()
        {
            GameObject root = new GameObject("StageGeneratorInactiveParentDestroyTests");
            StageGenerator generator = root.AddComponent<StageGenerator>();
            generator.Refresh_StageGenerator(true);
            generator.TransformM.StageParent.gameObject.SetActive(false);
            GameObject generatedChild = new GameObject("GeneratedChild");
            generatedChild.transform.SetParent(generator.TransformM.StageParent, false);
            StageChildLifecycleProbe probe = generatedChild.AddComponent<StageChildLifecycleProbe>();

            Assert.That(probe.EnableCount, Is.Zero);
            Assert.That(generator.GenerateM.DestroyStage(true, true), Is.True);
            Assert.That(probe.EnableCount, Is.Zero);

            yield return null;

            Assert.That(generatedChild == null, Is.True);
            Object.Destroy(root);
        }
    }
}
