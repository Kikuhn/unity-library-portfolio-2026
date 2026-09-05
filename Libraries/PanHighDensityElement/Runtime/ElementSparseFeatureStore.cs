using System;
using Unity.Collections;



namespace Pan.HighDensityElement
{
    /// <summary>
    /// generation-safe ElementKey를 dense unmanaged Feature 값에 연결하는 typed sparse store입니다.
    /// </summary>
    public sealed class ElementSparseFeatureStore<T> : IDisposable where T : unmanaged
    {
        private NativeList<ElementKey> keys;
        private NativeList<T> values;
        private NativeParallelHashMap<ElementKey, int> indices;
        private bool disposed;



        public ElementSparseFeatureStore(int initialCapacity = 16, Allocator allocator = Allocator.Persistent)
        {
            if (initialCapacity < 1) { throw new ArgumentOutOfRangeException(nameof(initialCapacity)); }
            keys = new NativeList<ElementKey>(initialCapacity, allocator);
            values = new NativeList<T>(initialCapacity, allocator);
            indices = new NativeParallelHashMap<ElementKey, int>(initialCapacity, allocator);
        }



        public int Count
        {
            get
            {
                ThrowIfDisposed();
                return values.Length;
            }
        }



        public bool TryAdd(ElementKey key, in T value)
        {
            ThrowIfDisposed();
            if (!key.IsValid || indices.ContainsKey(key)) { return false; }
            EnsureCapacity(values.Length + 1);

            int index = values.Length;
            keys.Add(key);
            values.Add(value);
            if (indices.TryAdd(key, index)) { return true; }

            keys.RemoveAtSwapBack(index);
            values.RemoveAtSwapBack(index);
            return false;
        }



        public bool AddOrSet(ElementKey key, in T value)
        {
            ThrowIfDisposed();
            if (!key.IsValid) { return false; }
            if (indices.TryGetValue(key, out int index))
            {
                values[index] = value;
                return false;
            }

            TryAdd(key, in value);
            return true;
        }



        public bool TryGet(ElementKey key, out T value)
        {
            ThrowIfDisposed();
            if (key.IsValid && indices.TryGetValue(key, out int index))
            {
                value = values[index];
                return true;
            }

            value = default;
            return false;
        }



#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal int Capacity
        {
            get
            {
                ThrowIfDisposed();
                return values.Capacity;
            }
        }



        internal bool TryGetDenseIndex(ElementKey key, out int denseIndex)
        {
            ThrowIfDisposed();
            denseIndex = -1;
            return key.IsValid && indices.TryGetValue(key, out denseIndex);
        }
#endif



        public bool TrySet(ElementKey key, in T value)
        {
            ThrowIfDisposed();
            if (!key.IsValid || !indices.TryGetValue(key, out int index)) { return false; }
            values[index] = value;
            return true;
        }



        public bool Remove(ElementKey key)
        {
            ThrowIfDisposed();
            if (!key.IsValid || !indices.TryGetValue(key, out int index)) { return false; }
            RemoveAtSwapBack(index);
            return true;
        }



        public void Clear()
        {
            ThrowIfDisposed();
            keys.Clear();
            values.Clear();
            indices.Clear();
        }



        internal ElementKey GetKeyAt(int index)
        {
            ThrowIfDisposed();
            return keys[index];
        }



        internal T GetValueAt(int index)
        {
            ThrowIfDisposed();
            return values[index];
        }



        internal void SetValueAt(int index, in T value)
        {
            ThrowIfDisposed();
            values[index] = value;
        }



        internal NativeParallelHashMap<ElementKey, int>.ReadOnly GetIndicesReadOnly()
        {
            ThrowIfDisposed();
            return indices.AsReadOnly();
        }



        internal NativeArray<T> GetValuesArray()
        {
            ThrowIfDisposed();
            return values.AsArray();
        }



        internal void RemoveAtSwapBack(int index)
        {
            ThrowIfDisposed();
            int lastIndex = values.Length - 1;
            ElementKey removedKey = keys[index];
            indices.Remove(removedKey);

            if (index != lastIndex)
            {
                ElementKey movedKey = keys[lastIndex];
                keys[index] = movedKey;
                values[index] = values[lastIndex];
                indices[movedKey] = index;
            }

            keys.RemoveAt(lastIndex);
            values.RemoveAt(lastIndex);
        }



        public void Dispose()
        {
            if (disposed) { return; }
            if (keys.IsCreated) { keys.Dispose(); }
            if (values.IsCreated) { values.Dispose(); }
            if (indices.IsCreated) { indices.Dispose(); }
            disposed = true;
        }



        private void EnsureCapacity(int requiredCapacity)
        {
            if (keys.Capacity < requiredCapacity) { keys.Capacity = requiredCapacity; }
            if (values.Capacity < requiredCapacity) { values.Capacity = requiredCapacity; }
            if (indices.Capacity < requiredCapacity) { indices.Capacity = requiredCapacity; }
        }



        private void ThrowIfDisposed()
        {
            if (disposed) { throw new ObjectDisposedException(nameof(ElementSparseFeatureStore<T>)); }
        }
    }
}
