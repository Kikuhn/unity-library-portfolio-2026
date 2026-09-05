using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;



namespace Pan.Util.Tests
{
    public class InfinityStackEditorCacheTests
    {
        [Test]
        public void EditorSnapshot_RefreshesOnceAfterPoolMutation()
        {
            var pool = new InfinityStack_ClassNew<EditorCacheItem>(0);

            try
            {
                Assert.AreEqual(1, pool.Create(1));
                Assert.IsTrue(GetEditorCacheDirty(pool));
                Assert.IsNull(pool.editorCachedPoolStack);

                Assert.AreEqual(1, pool.PoolCount);
                Assert.IsFalse(GetEditorCacheDirty(pool));
                Assert.AreEqual(1, pool.editorCachedPoolStack.Count);
                Assert.IsEmpty(pool.editorCache_InUseList);

                var sentinel = new EditorCacheItem();
                pool.editorCachedPoolStack.Add(sentinel);
                Assert.AreEqual(1, pool.PoolCount);
                Assert.Contains(sentinel, pool.editorCachedPoolStack);
                pool.editorCachedPoolStack.Remove(sentinel);

                EditorCacheItem item = pool.Pop();

                Assert.IsTrue(GetEditorCacheDirty(pool));
                Assert.AreEqual(1, pool.editorCachedPoolStack.Count);
                Assert.AreEqual(1, pool.InUseCount);
                Assert.IsFalse(GetEditorCacheDirty(pool));
                Assert.IsEmpty(pool.editorCachedPoolStack);
                Assert.AreEqual(1, pool.editorCache_InUseList.Count);

                Assert.IsTrue(pool.Push(item));
                Assert.IsTrue(GetEditorCacheDirty(pool));
                Assert.AreEqual(1, pool.PoolCount);
                Assert.IsFalse(GetEditorCacheDirty(pool));
                Assert.AreEqual(1, pool.editorCachedPoolStack.Count);
                Assert.IsEmpty(pool.editorCache_InUseList);

                pool.ReleaseEventPost += _ => throw new InvalidOperationException("Expected release callback failure.");

                Assert.Throws<InvalidOperationException>(() => pool.Release(1));
                Assert.IsTrue(GetEditorCacheDirty(pool));
                Assert.AreEqual(0, pool.PoolCount);
                Assert.IsFalse(GetEditorCacheDirty(pool));
            }
            finally
            {
                pool.Dispose();
            }
        }



        [Test]
        public void ManagerSnapshot_RefreshesOnlyAfterDictionaryMutation()
        {
            var initialCounts = new Dictionary<Type, int>
            {
                { typeof(EditorManagerItemA), 0 }
            };
            var manager = new InfinityStackManager_TypeBaseClassInstance_Improved<EditorManagerItem>(
                initialCounts);

            try
            {
                manager.InitialInitialize();

                Assert.AreEqual(1, manager.GetPoolDictionaryCount);
                Assert.IsTrue(GetManagerEditorCacheDirty(manager));
                Assert.AreEqual(1, manager.EditorCached_GetPoolStackConvertedList.Count);
                Assert.IsFalse(GetManagerEditorCacheDirty(manager));

                manager.EditorCached_GetPoolStackConvertedList.Add("__sentinel", null);
                Assert.AreEqual(2, manager.EditorCached_GetPoolStackConvertedList.Count);
                manager.EditorCached_GetPoolStackConvertedList.Remove("__sentinel");

                Assert.AreEqual(0, manager.Create(typeof(EditorManagerItemB), 0));
                Assert.IsTrue(GetManagerEditorCacheDirty(manager));
                Assert.AreEqual(2, manager.EditorCached_GetPoolStackConvertedList.Count);
                Assert.IsFalse(GetManagerEditorCacheDirty(manager));
            }
            finally
            {
                manager.Dispose();
            }
        }



        private static bool GetEditorCacheDirty(InfinityStack_ClassNew<EditorCacheItem> pool)
        {
            FieldInfo field = typeof(InfinityStackBase<EditorCacheItem>).GetField(
                "editorCacheDirty",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(field);
            return (bool)field.GetValue(pool);
        }



        private static bool GetManagerEditorCacheDirty(
            InfinityStackManager_TypeBaseClassInstance_Improved<EditorManagerItem> manager)
        {
            Type managerBaseType = typeof(InfinityStackManagerBase<
                EditorManagerItem,
                Type,
                InfinityStack_ClassInstance<EditorManagerItem>>);
            FieldInfo field = managerBaseType.GetField(
                "editorPoolDictionaryCacheDirty",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(field);
            return (bool)field.GetValue(manager);
        }



        public sealed class EditorCacheItem
        {
            public EditorCacheItem()
            {
            }
        }



        public abstract class EditorManagerItem
        {
        }



        public sealed class EditorManagerItemA : EditorManagerItem
        {
            public EditorManagerItemA()
            {
            }
        }



        public sealed class EditorManagerItemB : EditorManagerItem
        {
            public EditorManagerItemB()
            {
            }
        }
    }
}
