using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Spine;
using Unity.Profiling;
using ZLinq;

namespace Pan.SpinePackage.Tests
{
    public class RunTimeSkinManagerTests
    {
        private const int MainSlot = 0;
        private const int ExtraSlot = 1;
        private const string Placeholder = "attachment";

        [Test]
        public void SkinManager_RemovingHigherRankRestoresLowerRankThenMainSkin()
        {
            var manager = new RunTimeSkinManager_Skin(null, "test-husk");
            var main = CreateSkin("main", (MainSlot, "main"));
            var lower = CreateSkin("lower", (MainSlot, "lower"));
            var higher = CreateSkin("higher", (MainSlot, "higher"));

            manager.SetMainSkin(main);
            Assert.That(manager.PutUpSkin(lower, 10), Is.True);
            Assert.That(manager.PutUpSkin(higher, 20), Is.True);
            AssertAttachmentName(GetHusk(manager), MainSlot, "higher");

            Assert.That(manager.PutDownSkin(higher, 20), Is.True);
            AssertAttachmentName(GetHusk(manager), MainSlot, "lower");

            Assert.That(manager.PutDownSkin(lower, 10), Is.True);
            AssertAttachmentName(GetHusk(manager), MainSlot, "main");
        }

        [Test]
        public void SkinManager_NegativeRanksUseAscendingOrderRegardlessOfInsertionOrder()
        {
            var manager = new RunTimeSkinManager_Skin(null, "test-husk");
            var main = CreateSkin("main", (MainSlot, "main"));
            var higher = CreateSkin("higher", (MainSlot, "higher"));
            var lower = CreateSkin("lower", (MainSlot, "lower"));

            manager.SetMainSkin(main);
            Assert.That(manager.PutUpSkin(higher, -1), Is.True);
            Assert.That(manager.PutUpSkin(lower, -20), Is.True);

            AssertAttachmentName(GetHusk(manager), MainSlot, "higher");

            Assert.That(manager.PutDownSkin(higher, -1), Is.True);
            AssertAttachmentName(GetHusk(manager), MainSlot, "lower");
        }

        [Test]
        public void SkinManager_OverlapReplacementDoesNotLeaveOldLayerData()
        {
            var manager = new RunTimeSkinManager_Skin(null, "test-husk");
            var main = CreateSkin("main", (MainSlot, "main"), (ExtraSlot, "main-extra"));
            var oldLayer = CreateSkin("old", (MainSlot, "old"), (ExtraSlot, "old-extra"));
            var replacement = CreateSkin("replacement", (MainSlot, "replacement"));

            manager.SetMainSkin(main);
            Assert.That(manager.PutUpSkin(oldLayer, 5), Is.True);
            Assert.That(manager.PutUpSkin(replacement, 5, overlap: true), Is.True);

            Skin husk = GetHusk(manager);
            AssertAttachmentName(husk, MainSlot, "replacement");
            AssertAttachmentName(husk, ExtraSlot, "main-extra");
            Assert.That(manager.PutDownSkin(oldLayer, 5), Is.False);
            Assert.That(manager.PutDownSkin(replacement, 5), Is.True);
        }

        [Test]
        public void SkinManager_AttachmentOnlyLayerDoesNotAddRequiredBones()
        {
            var manager = new RunTimeSkinManager_Skin(null, "test-husk");
            var main = CreateSkin("main", (MainSlot, "main"));
            var attachmentOnly = CreateSkin("attachment-only", (MainSlot, "overlay"));
            var requiredBone = new BoneData(0, "required", null);
            attachmentOnly.Bones.Add(requiredBone);

            manager.SetMainSkin(main);
            Assert.That(manager.PutUpSkin(attachmentOnly, 1, putOnlyAttachment: true), Is.True);

            AssertAttachmentName(GetHusk(manager), MainSlot, "overlay");
            Assert.That(GetHusk(manager).Bones.Contains(requiredBone), Is.False);
        }

        [Test]
        public void SkinManager_ClearBeforeApplyingMainSkinRemovesMixLayers()
        {
            var manager = new RunTimeSkinManager_Skin(null, "test-husk");
            var firstMain = CreateSkin("first-main", (MainSlot, "first-main"));
            var mix = CreateSkin("mix", (MainSlot, "mix"), (ExtraSlot, "mix-extra"));
            var secondMain = CreateSkin("second-main", (MainSlot, "second-main"));

            manager.SetMainSkin(firstMain);
            Assert.That(manager.PutUpSkin(mix, 10), Is.True);

            manager.SetMainSkin(secondMain, clearBeforeApply: true);

            Skin husk = GetHusk(manager);
            AssertAttachmentName(husk, MainSlot, "second-main");
            Assert.That(husk.GetAttachment(ExtraSlot, Placeholder), Is.Null);
            Assert.That(manager.PutDownSkin(mix, 10), Is.False);
        }

        [Test]
        public void ExtendSkinManager_UsesSameRankedRecomposition()
        {
            var manager =
                new SkelObject.SkelCore.RunTimeSkinManager(null, null, "test-extend-husk");
            var main = new ExtendSkin(CreateSkin("main", (MainSlot, "main")), "main", 0);
            var lower = new ExtendSkin(CreateSkin("lower", (MainSlot, "lower")), "lower", 10);
            var higher = new ExtendSkin(CreateSkin("higher", (MainSlot, "higher")), "higher", 20);

            manager.SetMainSkin(main);
            Assert.That(manager.PutUpSkin(lower), Is.True);
            Assert.That(manager.PutUpSkin(higher), Is.True);
            AssertAttachmentName(GetHusk(manager), MainSlot, "higher");

            Assert.That(manager.PutDownSkin(higher), Is.True);
            AssertAttachmentName(GetHusk(manager), MainSlot, "lower");

            Assert.That(manager.PutDownSkin(lower), Is.True);
            AssertAttachmentName(GetHusk(manager), MainSlot, "main");
        }

        [Test]
        [Explicit("rank 정렬 재사용 버퍼의 할당과 CPU 중앙값을 수동 비교할 때 실행합니다.")]
        public void RankSort_Diagnostic_ReportsAllocationCallsAndCpuMedian()
        {
            var source = new Dictionary<int, int>(128);
            for (int i = 0; i < 128; i++)
            {
                int key = (i * 73) % 128 - 64;
                source.Add(key, i);
            }

            var buffer = new List<KeyValuePair<int, int>>(source.Count);
            int baselineChecksum = ConsumeOrderBy(source);
            int optimizedChecksum = ConsumeReusableBuffer(source, buffer);
            Assert.That(optimizedChecksum, Is.EqualTo(baselineChecksum));

            DiagnosticSample calibration = Measure(static () => { });
            Assert.That(
                calibration.MedianAllocationCalls,
                Is.Zero,
                "빈 action 계측에서 GC.Alloc 호출이 검출되어 진단 결과를 신뢰할 수 없습니다.");

            DiagnosticSample baseline = Measure(() => ConsumeOrderBy(source));
            DiagnosticSample optimized = Measure(() => ConsumeReusableBuffer(source, buffer));
            double cpuRatio = baseline.MedianTicks == 0
                ? double.NaN
                : (double)optimized.MedianTicks / baseline.MedianTicks;
            bool allocationCallsReduced =
                optimized.MedianAllocationCalls < baseline.MedianAllocationCalls;
            bool cpuWithinThreshold = !double.IsNaN(cpuRatio) && cpuRatio <= 1.05d;

            string summary =
                $"RankSort: semanticEqual=True, checksum={baselineChecksum}, " +
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

        private static Skin CreateSkin(
            string name,
            params (int slot, string attachmentName)[] attachments)
        {
            var skin = new Skin(name);
            foreach (var attachment in attachments)
            {
                skin.SetAttachment(
                    attachment.slot,
                    Placeholder,
                    new PointAttachment(attachment.attachmentName));
            }

            return skin;
        }

        private static int diagnosticSink;

        private static readonly Comparison<KeyValuePair<int, int>> DiagnosticRankComparison =
            static (left, right) => left.Key.CompareTo(right.Key);

        private static int ConsumeOrderBy(Dictionary<int, int> source)
        {
            int checksum = 0;
            foreach (KeyValuePair<int, int> item in source.OrderBy(item => item.Key))
            {
                checksum = unchecked(checksum * 31 + item.Key);
            }

            diagnosticSink = checksum;
            return checksum;
        }

        private static int ConsumeReusableBuffer(
            Dictionary<int, int> source,
            List<KeyValuePair<int, int>> buffer)
        {
            source.AsValueEnumerable().CopyTo(buffer);
            buffer.Sort(DiagnosticRankComparison);

            int checksum = 0;
            foreach (KeyValuePair<int, int> item in buffer)
            {
                checksum = unchecked(checksum * 31 + item.Key);
            }

            diagnosticSink = checksum;
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

        private static Skin GetHusk<TSkin>(BaseRunTimeSkinManager<TSkin> manager)
        {
            FieldInfo field = typeof(BaseRunTimeSkinManager<TSkin>).GetField(
                "HuskSkin",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            return (Skin)field.GetValue(manager);
        }

        private static void AssertAttachmentName(Skin skin, int slot, string expectedName)
        {
            Assert.That(skin.GetAttachment(slot, Placeholder)?.Name, Is.EqualTo(expectedName));
        }
    }
}
