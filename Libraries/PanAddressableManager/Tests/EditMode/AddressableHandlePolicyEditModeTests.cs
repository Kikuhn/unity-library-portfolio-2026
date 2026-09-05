using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TestTools;

namespace Pan.AddressableManagers.Tests
{
    public sealed class AddressableHandlePolicyEditModeTests
    {
        private const string MissingKey = "__pan_addressable_missing_key__";

        [UnityTest]
        public IEnumerator MissingKey_FailureHandleMatchesOwnershipPolicy()
        {
            return UniTask.ToCoroutine(async () =>
            {
                await InitializeAddressables();
                var manager = new PanAddressableManager(8);
                AsyncOperationHandle<Texture2D> retainedSync = default;
                AsyncOperationHandle<Texture2D> retainedAsync = default;
                bool previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
                LogAssert.ignoreFailingMessages = true;

                try
                {
                    Assert.IsNull(manager.Loader.Load<Texture2D>(
                        MissingKey, out AsyncOperationHandle<Texture2D> releasedSync, true));
                    Assert.IsFalse(releasedSync.IsValid());

                    Assert.IsNull(manager.Loader.Load<Texture2D>(MissingKey, out retainedSync, false));
                    Assert.IsTrue(retainedSync.IsValid());
                    Assert.AreEqual(AsyncOperationStatus.Failed, retainedSync.Status);

                    AsyncOperationHandle<Texture2D> releasedAsync =
                        await manager.Loader.LoadAsyncHandle<Texture2D>(MissingKey, true);
                    Assert.IsFalse(releasedAsync.IsValid());

                    AsyncOperationHandle<IList<UnityEngine.Object>> releasedMultiAsync =
                        await manager.Loader.LoadsAsyncHandle<UnityEngine.Object>(
                            new[] { MissingKey, MissingKey + "_2" },
                            Addressables.MergeMode.Intersection,
                            autoReleaseHandleWhenFailedResult: true);
                    Assert.IsFalse(releasedMultiAsync.IsValid());

                    retainedAsync = await manager.Loader.LoadAsyncHandle<Texture2D>(MissingKey, false);
                    Assert.IsTrue(retainedAsync.IsValid());
                    Assert.AreEqual(AsyncOperationStatus.Failed, retainedAsync.Status);

                    int before = GetOperationCacheCount();
                    IList<UnityEngine.Object> result = await manager.Loader.LoadsAsync<UnityEngine.Object>(
                        new[] { MissingKey, MissingKey + "_2" },
                        Addressables.MergeMode.Intersection,
                        _ => Assert.Fail("A failed wrapper must not invoke its callback."),
                        false);
                    Assert.IsNull(result);
                    Assert.LessOrEqual(GetOperationCacheCount(), before);
                }
                finally
                {
                    Release(manager, retainedAsync);
                    Release(manager, retainedSync);
                    manager.Loader.ReleaseAllCache();
                    LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
                }
            });
        }

        [UnityTest]
        public IEnumerator DownloadSize_AllOverloadsReleaseTheirOperations()
        {
            return UniTask.ToCoroutine(async () =>
            {
                await InitializeAddressables();
                var manager = new PanAddressableManager(4);
                bool previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
                LogAssert.ignoreFailingMessages = true;

                try
                {
                    int before = GetOperationCacheCount();
                    manager.Downloader.GetDownloadSize((object)MissingKey);
                    manager.Downloader.GetDownloadSize(MissingKey);
                    manager.Downloader.GetDownloadSize((IEnumerable)new object[] { MissingKey });
                    await manager.Downloader.GetDownloadSizeAsync((object)MissingKey);
                    await manager.Downloader.GetDownloadSizeAsync(MissingKey);
                    await manager.Downloader.GetDownloadSizeAsync(
                        (IEnumerable)new object[] { MissingKey });
                    Assert.LessOrEqual(GetOperationCacheCount(), before);
                }
                finally
                {
                    LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
                }
            });
        }

        private static async UniTask InitializeAddressables()
        {
            AsyncOperationHandle handle = Addressables.InitializeAsync(false);
            await handle.Task;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, handle.Status);
            Addressables.Release(handle);
        }

        private static int GetOperationCacheCount()
        {
            PropertyInfo property = Addressables.ResourceManager.GetType().GetProperty(
                "OperationCacheCount",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property);
            return Convert.ToInt32(property.GetValue(Addressables.ResourceManager));
        }

        private static void Release(PanAddressableManager manager, AsyncOperationHandle handle)
        {
            if (handle.IsValid())
            {
                manager.Loader.Release(handle);
            }
        }
    }
}
