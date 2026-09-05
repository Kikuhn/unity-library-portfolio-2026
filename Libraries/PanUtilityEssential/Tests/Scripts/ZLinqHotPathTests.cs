using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using ZLinq;

namespace Pan.Util.Tests
{
    public class ZLinqHotPathTests
    {
        [Test]
        public void GetBoundingRect_EnumeratesSingleUseSourceOnce()
        {
            var source = new SingleUseEnumerable<Rect>(
                new Rect(4f, -2f, 3f, 5f),
                new Rect(-5f, 6f, 2f, 4f));

            Rect result = SU_TF_Rect.GetBoundingRect(source);

            Assert.That(source.EnumerationCount, Is.EqualTo(1));
            Assert.That(result.xMin, Is.EqualTo(-5f));
            Assert.That(result.yMin, Is.EqualTo(-2f));
            Assert.That(result.xMax, Is.EqualTo(7f));
            Assert.That(result.yMax, Is.EqualTo(10f));
        }



        [Test]
        public void ChunkImmediate_ReturnsExactIndependentArrays()
        {
            var chunks = new List<int[]>();
            foreach (int[] chunk in new[] { 1, 2, 3, 4, 5 }.ChunkImmediate(2))
            {
                chunks.Add(chunk);
            }

            Assert.That(chunks.Count, Is.EqualTo(3));
            CollectionAssert.AreEqual(new[] { 1, 2 }, chunks[0]);
            CollectionAssert.AreEqual(new[] { 3, 4 }, chunks[1]);
            CollectionAssert.AreEqual(new[] { 5 }, chunks[2]);

            chunks[0][0] = 99;
            Assert.That(chunks[1][0], Is.EqualTo(3));
        }



        [Test]
        public void InfinityManagerConstructors_CacheSingleUseSourcesOnce()
        {
            var elements = new SingleUseEnumerable<TestPoolItem>(
                new TestPoolItem("alpha"),
                new TestPoolItem("beta"));
            var preKeyManager = new TestPreKeyManager(elements);

            var keys = new SingleUseEnumerable<TestKey>(new TestKeyA(), new TestKeyB());
            var sharedManager = new TestSharedClassKeyManager(keys);

            try
            {
                TestPoolItem popped = sharedManager.Pop<TestKeyA>();

                Assert.That(popped, Is.Not.Null);
                Assert.That(elements.EnumerationCount, Is.EqualTo(1));
                Assert.That(preKeyManager.GetPoolDictionaryCount, Is.EqualTo(2));
                Assert.That(keys.EnumerationCount, Is.EqualTo(1));
                Assert.That(sharedManager.GetPoolDictionaryCount, Is.EqualTo(2));
            }
            finally
            {
                preKeyManager.Dispose();
                sharedManager.Dispose();
            }
        }



        [Test]
        public void HighestChip_PreservesEmptyAndNegativeRankSemantics()
        {
            var manager = new ValueControlManager<int, object>((_, _) => { });

            Assert.That(manager.GetHighstChip, Is.EqualTo(0));
            Assert.That(manager.SetValue(new object(), -20, 1), Is.True);
            Assert.That(manager.SetValue(new object(), -3, 2), Is.True);
            Assert.That(manager.GetHighstChip, Is.EqualTo(-3));
        }



        [Test]
        public void EmptySequences_PreserveNeutralResults()
        {
            Assert.That(SU_TF_Rect.GetBoundingRect(Array.Empty<Rect>()), Is.EqualTo(new Rect()));

            using (IEnumerator<int[]> chunks = Array.Empty<int>().ChunkImmediate(4).GetEnumerator())
            {
                Assert.That(chunks.MoveNext(), Is.False);
            }

            var manager = new TestPreKeyManager(Array.Empty<TestPoolItem>());
            try
            {
                Assert.That(manager.GetPoolDictionaryCount, Is.EqualTo(0));
            }
            finally
            {
                manager.Dispose();
            }
        }



        [Test]
        [Explicit("ZLinq 핫패스의 할당과 CPU 중앙값을 수동 비교할 때 실행합니다.")]
        public void EnumerableToArray_Diagnostic_ReportsSemanticsAllocationCallsAndCpuRatio()
        {
            IEnumerable<int> source = CreateDiagnosticSource(256);

            int[] baselineResult = System.Linq.Enumerable.ToArray(source);
            int[] optimizedResult = source.AsValueEnumerable().ToArray();
            CollectionAssert.AreEqual(baselineResult, optimizedResult);

            int baselineChecksum = CalculateChecksum(baselineResult);
            int optimizedChecksum = CalculateChecksum(optimizedResult);
            Assert.That(optimizedChecksum, Is.EqualTo(baselineChecksum));

            DiagnosticSample calibration = Measure(static () => { });
            Assert.That(
                calibration.MedianAllocationCalls,
                Is.Zero,
                "빈 action 계측에서 GC.Alloc 호출이 검출되어 진단 결과를 신뢰할 수 없습니다.");

            DiagnosticSample baseline = Measure(() => ConsumeLinqToArray(source));
            DiagnosticSample optimized = Measure(() => ConsumeZLinqToArray(source));
            double cpuRatio = baseline.MedianTicks == 0
                ? double.NaN
                : (double)optimized.MedianTicks / baseline.MedianTicks;
            bool allocationCallsReduced =
                optimized.MedianAllocationCalls < baseline.MedianAllocationCalls;
            bool cpuWithinThreshold = !double.IsNaN(cpuRatio) && cpuRatio <= 1.05d;

            string summary =
                $"EnumerableToArray: semanticEqual=True, checksum={baselineChecksum}, " +
                $"calibration={calibration.MedianAllocationCalls} allocation calls, " +
                $"baseline={baseline.MedianTicks} ticks/{baseline.MedianAllocationCalls} allocation calls, " +
                $"optimized={optimized.MedianTicks} ticks/{optimized.MedianAllocationCalls} allocation calls, " +
                $"cpu ratio={cpuRatio:F3}x, " +
                $"allocation call delta={optimized.MedianAllocationCalls - baseline.MedianAllocationCalls}, " +
                $"gates: allocation calls reduced={allocationCallsReduced}, " +
                $"cpu ratio <= 1.05={cpuWithinThreshold}, " +
                $"accepted={allocationCallsReduced && cpuWithinThreshold}";

            TestContext.WriteLine(summary);
            UnityEngine.Debug.Log(summary);
        }



        private static int diagnosticSink;



        private static IEnumerable<int> CreateDiagnosticSource(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return unchecked(i * 17 - 3);
            }
        }



        private static void ConsumeLinqToArray(IEnumerable<int> source)
        {
            diagnosticSink = CalculateChecksum(System.Linq.Enumerable.ToArray(source));
        }



        private static void ConsumeZLinqToArray(IEnumerable<int> source)
        {
            diagnosticSink = CalculateChecksum(source.AsValueEnumerable().ToArray());
        }



        private static int CalculateChecksum(IEnumerable<int> values)
        {
            int checksum = 0;
            foreach (int value in values)
            {
                checksum = unchecked(checksum * 31 + value);
            }

            return checksum;
        }



        private static DiagnosticSample Measure(Action action)
        {
            const int warmupCount = 10;
            const int iterationCount = 50;
            const int setCount = 3;

            for (int i = 0; i < warmupCount; i++) { action(); }

            var recorder = new ProfilerRecorder(
                ProfilerCategory.Memory,
                "GC.Alloc",
                1,
                ProfilerRecorderOptions.WrapAroundWhenCapacityReached |
                ProfilerRecorderOptions.SumAllSamplesInFrame |
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);

            try
            {
                Assert.That(recorder.Valid, Is.True, "GC.Alloc ProfilerRecorder가 유효하지 않습니다.");

                var elapsedTicks = new long[setCount];
                var allocationCalls = new long[setCount];
                for (int setIndex = 0; setIndex < setCount; setIndex++)
                {
                    recorder.Start();
                    long startedAt = Stopwatch.GetTimestamp();
                    for (int i = 0; i < iterationCount; i++) { action(); }

                    elapsedTicks[setIndex] = Stopwatch.GetTimestamp() - startedAt;
                    recorder.Stop();
                    allocationCalls[setIndex] = recorder.Count == 0
                        ? 0
                        : recorder.GetSample(0).Count;
                    recorder.Reset();
                }

                Array.Sort(elapsedTicks);
                Array.Sort(allocationCalls);
                return new DiagnosticSample(elapsedTicks[1], allocationCalls[1]);
            }
            finally
            {
                if (recorder.IsRunning) { recorder.Stop(); }
                recorder.Dispose();
            }
        }



        private readonly struct DiagnosticSample
        {
            public DiagnosticSample(long medianTicks, long medianAllocationCalls)
            {
                MedianTicks = medianTicks;
                MedianAllocationCalls = medianAllocationCalls;
            }

            public long MedianTicks { get; }
            public long MedianAllocationCalls { get; }
        }



        private sealed class SingleUseEnumerable<T> : IEnumerable<T>
        {
            private readonly T[] items;

            public SingleUseEnumerable(params T[] items)
            {
                this.items = items;
            }

            public int EnumerationCount { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                EnumerationCount++;
                if (EnumerationCount > 1)
                {
                    throw new InvalidOperationException("이 열거는 한 번만 순회할 수 있습니다.");
                }

                for (int i = 0; i < items.Length; i++) { yield return items[i]; }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }



        private sealed class TestPoolItem
        {
            public TestPoolItem()
            {
            }

            public TestPoolItem(string name)
            {
                Name = name;
            }

            public string Name { get; }
        }



        private sealed class TestPreKeyManager : InfinityStackManagerBase_PreKey<
            TestPoolItem,
            string,
            InfinityStack_ClassNew<TestPoolItem>>
        {
            public TestPreKeyManager(IEnumerable<TestPoolItem> elements) : base(elements)
            {
            }

            protected override void InitializeKeyElementMaps(
                IEnumerable<TestPoolItem> elements,
                out Dictionary<string, TestPoolItem> keyElementsMap)
            {
                keyElementsMap = new Dictionary<string, TestPoolItem>();
                foreach (TestPoolItem element in elements) { keyElementsMap.Add(element.Name, element); }
            }

            protected override void InitializePool(string key, out InfinityStack_ClassNew<TestPoolItem> pool)
            {
                pool = new InfinityStack_ClassNew<TestPoolItem>(0);
            }

            protected override void Initialize()
            {
            }
        }



        private abstract class TestKey
        {
        }

        private sealed class TestKeyA : TestKey
        {
        }

        private sealed class TestKeyB : TestKey
        {
        }



        private sealed class TestSharedClassKeyManager : InfinityStackManagerBase_Shared_ClassKey<
            TestPoolItem,
            TestKey,
            InfinityStack_ClassNew<TestPoolItem>>
        {
            public TestSharedClassKeyManager(IEnumerable<TestKey> keys)
                : base(new TestPoolItem(), keys)
            {
            }

            protected override void InitializePool(TestKey key, out InfinityStack_ClassNew<TestPoolItem> pool)
            {
                pool = new InfinityStack_ClassNew<TestPoolItem>(0);
            }

            protected override void Initialize()
            {
            }
        }
    }
}
