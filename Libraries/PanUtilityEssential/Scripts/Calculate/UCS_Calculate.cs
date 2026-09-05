using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;



//? 코드 연산들이 정리되어있는 정도의 코드



namespace Pan.Util
{
    public static class UCS_Calculate
    {
        ///======================================================================================================================================================



        //? 병렬처리



        /// <summary>
        /// 주어진 컬렉션의 실제 자료구조 타입(<typeparamref name="T"/>)에 따라
        /// 자동으로 병렬 처리 함수를 선택하여 실행합니다.
        /// <para>배치 크기(<paramref name="batchSize"/>)만큼 분할하여 <paramref name="action"/> 작업을 병렬로 수행합니다.</para>
        /// </summary>
        /// <typeparam name="T">컬렉션의 요소 타입</typeparam>
        /// <param name="collection">병렬 처리할 원본 컬렉션</param>
        /// <param name="batchSize">한 번에 처리할 항목 수</param>
        /// <param name="action">각 항목에 대해 수행할 동작</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="collection"/>이 null이거나, <paramref name="action"/>이 null인 경우
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="batchSize"/>가 1 미만인 경우
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// 지원되지 않는 컬렉션 타입인 경우
        /// </exception>
        public static void ParallelProcess<T>(this IEnumerable<T> collection, int batchSize, Action<T> action)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection), "컬렉션은 null일 수 없습니다.");
            if (action == null) throw new ArgumentNullException(nameof(action), "작업(action)은 null일 수 없습니다.");
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize), "배치 크기는 1 이상이어야 합니다.");

            switch (collection)
            {
                case T[] array:
                ParallelForArray(array, batchSize, action);
                break;
                case List<T> list:
                ParallelForList(list, batchSize, action);
                break;
                case HashSet<T> hashSet:
                ParallelForHashSet(hashSet, batchSize, action);
                break;
                case IEnumerable<T> enumerable:
                ParallelForEnumerable(enumerable, batchSize, action);
                break;
                default:
                throw new NotSupportedException($"지원되지 않는 컬렉션 타입: {collection.GetType().Name}");
            }
        }



        /// <summary>
        /// 배열(<paramref name="array"/>)을 배치 크기(<paramref name="batchSize"/>)만큼 분할하여 병렬 처리합니다.
        /// </summary>
        /// <typeparam name="T">배열 요소 타입</typeparam>
        /// <param name="array">처리할 배열</param>
        /// <param name="batchSize">한 번에 처리할 항목 수</param>
        /// <param name="action">각 요소에 대해 수행할 작업</param>
        public static void ParallelForArray<T>(T[] array, int batchSize, Action<T> action)
        {
            if (array.Length == 0) return;

            var segments = array.ChunkArraySegment(batchSize);
            Parallel.ForEach(segments, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, segment =>
            {
                foreach (var item in segment)
                {
                    action(item);
                }
            });
        }



        /// <summary>
        /// 리스트(<paramref name="list"/>)를 배치 크기(<paramref name="batchSize"/>)만큼 분할하여 병렬 처리합니다.
        /// </summary>
        /// <typeparam name="T">리스트 요소 타입</typeparam>
        /// <param name="list">처리할 리스트</param>
        /// <param name="batchSize">한 번에 처리할 항목 수</param>
        /// <param name="action">각 요소에 대해 수행할 작업</param>
        public static void ParallelForList<T>(IReadOnlyList<T> list, int batchSize, Action<T> action)
        {
            if (list.Count == 0) return;

            var chunks = list.ChunkImmediate(batchSize);
            Parallel.ForEach(chunks, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, chunk =>
            {
                foreach (var item in chunk)
                {
                    action(item);
                }
            });
        }



        /// <summary>
        /// HashSet(<paramref name="hashSet"/>)을 배치 크기(<paramref name="batchSize"/>)만큼 분할하여 병렬 처리합니다.
        /// </summary>
        /// <typeparam name="T">HashSet 요소 타입</typeparam>
        /// <param name="hashSet">처리할 HashSet</param>
        /// <param name="batchSize">한 번에 처리할 항목 수</param>
        /// <param name="action">각 요소에 대해 수행할 작업</param>
        public static void ParallelForHashSet<T>(HashSet<T> hashSet, int batchSize, Action<T> action)
        {
            if (hashSet.Count == 0) return;

            var chunks = hashSet.ChunkDirect(batchSize);
            Parallel.ForEach(chunks, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, chunk =>
            {
                foreach (var item in chunk)
                {
                    action(item);
                }
            });
        }



        /// <summary>
        /// 일반적인 IEnumerable(<paramref name="enumerable"/>) 컬렉션을 배치 크기(<paramref name="batchSize"/>)만큼 분할하여 병렬 처리합니다.
        /// </summary>
        /// <typeparam name="T">컬렉션 요소 타입</typeparam>
        /// <param name="enumerable">처리할 원본 IEnumerable</param>
        /// <param name="batchSize">한 번에 처리할 항목 수</param>
        /// <param name="action">각 요소에 대해 수행할 작업</param>
        public static void ParallelForEnumerable<T>(IEnumerable<T> enumerable, int batchSize, Action<T> action)
        {
            var chunks = enumerable.ChunkDirect(batchSize);
            Parallel.ForEach(chunks, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, chunk =>
            {
                foreach (var item in chunk)
                {
                    action(item);
                }
            });
        }



        ///======================================================================================================================================================


        //? 병렬 연산을 위한 청크


        /// <summary>
        /// 배열(<paramref name="source"/>)을 <paramref name="size"/>만큼 분할하여
        /// <see cref="ArraySegment{T}"/> 형태로 나누어 반환합니다.
        /// </summary>
        /// <typeparam name="T">배열 요소 타입</typeparam>
        /// <param name="source">나눌 대상 배열</param>
        /// <param name="size">배치 크기</param>
        /// <returns><paramref name="size"/> 단위로 분할된 <see cref="ArraySegment{T}"/> 열거</returns>
        public static IEnumerable<ArraySegment<T>> ChunkArraySegment<T>(this T[] source, int size)
        {
            for (int i = 0; i < source.Length; i += size)
            {
                yield return new ArraySegment<T>(source, i, Math.Min(size, source.Length - i));
            }
        }



        /// <summary>
        /// IReadOnlyList(<paramref name="source"/>)를 <paramref name="size"/>만큼 분할해
        /// 임시 배열로 만들어 반환합니다.
        /// <para>내부적으로 <see cref="ArrayPool{T}"/>을 사용하여 메모리를 재활용합니다.</para>
        /// </summary>
        /// <typeparam name="T">리스트 요소 타입</typeparam>
        /// <param name="source">분할할 IReadOnlyList</param>
        /// <param name="size">한 번에 처리할 항목 수</param>
        /// <returns>배치 크기만큼 분할된 T[] 열거</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/>가 null일 경우</exception>
        /// <exception cref="ArgumentException"><paramref name="size"/>가 1 미만인 경우</exception>
        public static IEnumerable<T[]> ChunkImmediate<T>(this IReadOnlyList<T> source, int size)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), "소스 리스트는 null일 수 없습니다.");

            if (size <= 0)
                throw new ArgumentException("배치 크기는 1 이상이어야 합니다.", nameof(size));

            int totalItems = source.Count;
            ArrayPool<T> arrayPool = ArrayPool<T>.Shared;

            for (int i = 0; i < totalItems; i += size)
            {
                int currentBatchSize = Math.Min(size, totalItems - i);
                T[] batch = arrayPool.Rent(size); // 항상 고정된 크기로 가져옴

                try
                {
                    for (int j = 0; j < currentBatchSize; j++)
                    {
                        batch[j] = source[i + j];
                    }

                    T[] result = new T[currentBatchSize];
                    Array.Copy(batch, result, currentBatchSize);
                    yield return result;
                }
                finally
                {
                    arrayPool.Return(batch, clearArray: true);
                }
            }
        }



        /// <summary>
        /// IEnumerable(<paramref name="source"/>)를 <paramref name="size"/>만큼 분할하여
        /// 조각 단위의 IEnumerable로 반환합니다.
        /// </summary>
        /// <typeparam name="T">컬렉션 요소 타입</typeparam>
        /// <param name="source">분할할 IEnumerable</param>
        /// <param name="size">한 번에 묶을 항목 수</param>
        /// <returns>요소를 <paramref name="size"/>만큼 나눈 그룹 열거</returns>
        public static IEnumerable<IEnumerable<T>> ChunkDirect<T>(this IEnumerable<T> source, int size)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), "소스는 null일 수 없습니다.");

            if (size <= 0)
                throw new ArgumentException("배치 크기는 1 이상이어야 합니다.", nameof(size));

            using var enumerator = source.GetEnumerator();
            while (enumerator.MoveNext())
            {
                yield return YieldChunkElements(enumerator, size - 1);
            }
        }



        /// <summary>
        /// 내부적으로 <paramref name="enumerator"/>를 순회하며
        /// 지정된 <paramref name="size"/> 범위만큼의 요소를 하나씩 반환합니다.
        /// </summary>
        /// <typeparam name="T">컬렉션 요소 타입</typeparam>
        /// <param name="enumerator">이동할 IEnumerator</param>
        /// <param name="size">추가로 가져올 개수</param>
        /// <returns>현재 위치부터 <paramref name="size"/> 만큼의 요소 열거</returns>
        private static IEnumerable<T> YieldChunkElements<T>(IEnumerator<T> enumerator, int size)
        {
            yield return enumerator.Current;

            for (int i = 0; i < size && enumerator.MoveNext(); i++)
            {
                yield return enumerator.Current;
            }
        }



        ///======================================================================================================================================================
    }
}