using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using ZLinq;



namespace Pan.StageGenerators.Tests
{
    public class StageGenerationPerformanceDiagnostics
    {
        private const ProfilerRecorderOptions AllocationRecorderOptions =
            ProfilerRecorderOptions.SumAllSamplesInFrame |
            ProfilerRecorderOptions.CollectOnlyOnCurrentThread |
            ProfilerRecorderOptions.WrapAroundWhenCapacityReached;
        private static readonly Action EmptyDiagnosticAction = () => { };
        private static int diagnosticSink;



        [Test]
        [Explicit("수동 성능 판정용 진단입니다. 일반 회귀 테스트에서는 실행하지 않습니다.")]
        [Category("Performance")]
        public void MaxCandidate_ReportsAllocationAndCpuMedians()
        {
            var source = CreateCandidateInput(4096);
            var optimizedCandidates = new List<Candidate>();

            RunDiagnostic(
                "Gen1 maximum candidate selection",
                () => RunLegacySelection(source, 17),
                () => RunOptimizedSelection(source, optimizedCandidates, 17),
                candidate => candidate.Id,
                (legacy, optimized) => Assert.That(optimized.Id, Is.EqualTo(legacy.Id)));
        }



        [Test]
        [Explicit("수동 성능 판정용 진단입니다. 일반 회귀 테스트에서는 실행하지 않습니다.")]
        [Category("Performance")]
        public void DoorDistanceSelection_ReportsAllocationAndCpuMedians()
        {
            const int inputCount = 1024;
            const int takeCount = 128;
            var distanceManager = CreateDistanceManager(inputCount);
            var distanceDoors = distanceManager.GetDoorList(Pan.Util.EDirection4.Down);

            RunDiagnostic(
                "DoorManager distance OrderBy/Take/Max",
                () => RunSystemDoorDistance(distanceDoors, takeCount),
                () => RunProductionDoorDistance(distanceManager, takeCount),
                value => value,
                (legacy, optimized) => Assert.That(optimized, Is.EqualTo(legacy)));
        }



        [Test]
        [Explicit("production에서 제거된 ZLinq 후보를 재평가할 때만 실행합니다.")]
        [Category("Performance")]
        public void RejectedZLinqCandidates_ReportAllocationAndCpuMedians()
        {
            const int inputCount = 1024;
            var spaceInput = CreateSpaceInput(inputCount);
            var doorPointInput = CreateDoorPointInput(inputCount);

            RunDiagnostic(
                "Rejected Gen3 OrderByDescending/ThenByDescending/ToArray",
                () => RunSystemSpaceOrdering(spaceInput),
                () => RunZLinqSpaceOrdering(spaceInput),
                CalculateSpaceChecksum,
                AssertSpaceSequenceEqual);

            RunDiagnostic(
                "Rejected Gen2 DoorPoint OrderBy/ToArray",
                () => RunSystemDoorPointOrdering(doorPointInput),
                () => RunZLinqDoorPointOrdering(doorPointInput),
                CalculateDoorPointChecksum,
                AssertDoorPointSequenceEqual);
        }



        private static List<Candidate> CreateCandidateInput(int count)
        {
            var result = new List<Candidate>(count);

            for (int i = 0; i < count; i++)
            {
                result.Add(new Candidate(i, i % 31));
            }

            return result;
        }



        private static RoomObject.DoorManager CreateDistanceManager(int count)
        {
            var manager = new RoomObject.DoorManager();
            var doors = manager.GetDoorList(Pan.Util.EDirection4.Down);

            for (int i = 0; i < count; i++)
            {
                doors.Add(new RoomObject.Door
                {
                    DoorWidth = 1,
                    DoorGridPositionDistance = (i * 37) % 997
                });
            }

            return manager;
        }



        private static SpaceMetric[] CreateSpaceInput(int count)
        {
            var result = new SpaceMetric[count];

            for (int i = 0; i < count; i++)
            {
                result[i] = new SpaceMetric(i, (i * 17) % 101, (i * 19) % 8);
            }

            return result;
        }



        private static StageGenerator.PlaceManger.Generator2_LinkRooms.DoorSpineConnector.DoorPoint[] CreateDoorPointInput(int count)
        {
            var result = new StageGenerator.PlaceManger.Generator2_LinkRooms.DoorSpineConnector.DoorPoint[count];

            for (int i = 0; i < count; i++)
            {
                result[i] = new StageGenerator.PlaceManger.Generator2_LinkRooms.DoorSpineConnector.DoorPoint(
                    new Vector2(i, (i * 29) % 113),
                    i);
            }

            return result;
        }



        private static Candidate RunLegacySelection(IReadOnlyList<Candidate> source, int selectedIndex)
        {
            int maxScore = source.Max(x => x.Score);
            var filtered = source.Where(x => x.Score == maxScore);
            int count = filtered.Count();
            return filtered.Skip(selectedIndex % count).FirstOrDefault();
        }



        private static Candidate RunOptimizedSelection(
            IReadOnlyList<Candidate> source,
            List<Candidate> destination,
            int selectedIndex)
        {
            StageGenerationAlgorithms.CollectMaxCandidates(source, destination, x => x.Score);
            return destination[selectedIndex % destination.Count];
        }



        private static int RunSystemDoorDistance(IReadOnlyList<RoomObject.Door> source, int takeCount)
        {
            int minBound = source
                .OrderBy(door => door.FullDoorTotalDistance)
                .Take(takeCount)
                .Max(door => door.FullDoorTotalDistance);
            int maxBound = source.Max(door => door.FullDoorTotalDistance);

            return unchecked((minBound * 397) ^ maxBound);
        }



        private static int RunProductionDoorDistance(RoomObject.DoorManager manager, int takeCount)
        {
            manager.TryGetDoorTotalDistanceByCount(
                Pan.Util.EDirection4.Down,
                takeCount,
                true,
                out int minBound);
            manager.TryGetDoorTotalDistanceByCount(
                Pan.Util.EDirection4.Down,
                takeCount,
                false,
                out int maxBound);

            return unchecked((minBound * 397) ^ maxBound);
        }



        private static SpaceMetric[] RunSystemSpaceOrdering(SpaceMetric[] source)
        {
            return source
                .OrderByDescending(x => x.Area)
                .ThenByDescending(x => x.NodeCount)
                .ToArray();
        }



        private static SpaceMetric[] RunZLinqSpaceOrdering(SpaceMetric[] source)
        {
            return source
                .AsValueEnumerable()
                .OrderByDescending(x => x.Area)
                .ThenByDescending(x => x.NodeCount)
                .ToArray();
        }



        private static StageGenerator.PlaceManger.Generator2_LinkRooms.DoorSpineConnector.DoorPoint[] RunSystemDoorPointOrdering(
            StageGenerator.PlaceManger.Generator2_LinkRooms.DoorSpineConnector.DoorPoint[] source)
        {
            return source.OrderBy(x => x.Pos.y).ToArray();
        }



        private static StageGenerator.PlaceManger.Generator2_LinkRooms.DoorSpineConnector.DoorPoint[] RunZLinqDoorPointOrdering(
            StageGenerator.PlaceManger.Generator2_LinkRooms.DoorSpineConnector.DoorPoint[] source)
        {
            return source.AsValueEnumerable().OrderBy(x => x.Pos.y).ToArray();
        }



        private static int CalculateSpaceChecksum(SpaceMetric[] source)
        {
            int checksum = 17;

            for (int i = 0; i < source.Length; i++)
            {
                checksum = unchecked((checksum * 31) ^ source[i].Id);
            }

            return checksum;
        }



        private static int CalculateDoorPointChecksum(
            StageGenerator.PlaceManger.Generator2_LinkRooms.DoorSpineConnector.DoorPoint[] source)
        {
            int checksum = 17;

            for (int i = 0; i < source.Length; i++)
            {
                checksum = unchecked((checksum * 31) ^ (int)source[i].DoorRef);
            }

            return checksum;
        }



        private static void AssertSpaceSequenceEqual(SpaceMetric[] legacy, SpaceMetric[] optimized)
        {
            Assert.That(optimized.Length, Is.EqualTo(legacy.Length));

            for (int i = 0; i < legacy.Length; i++)
            {
                Assert.That(optimized[i].Id, Is.EqualTo(legacy[i].Id));
            }
        }



        private static void AssertDoorPointSequenceEqual(
            StageGenerator.PlaceManger.Generator2_LinkRooms.DoorSpineConnector.DoorPoint[] legacy,
            StageGenerator.PlaceManger.Generator2_LinkRooms.DoorSpineConnector.DoorPoint[] optimized)
        {
            Assert.That(optimized.Length, Is.EqualTo(legacy.Length));

            for (int i = 0; i < legacy.Length; i++)
            {
                Assert.That(optimized[i].Pos, Is.EqualTo(legacy[i].Pos));
                Assert.That(optimized[i].DoorRef, Is.EqualTo(legacy[i].DoorRef));
            }
        }



        private static void RunDiagnostic<T>(
            string name,
            Func<T> baselineAction,
            Func<T> candidateAction,
            Func<T, int> checksum,
            Action<T, T> assertEquivalent)
        {
            const int WarmupCount = 10;
            const int IterationsPerSet = 50;
            const int SetCount = 3;

            T baselineResult = baselineAction();
            T candidateResult = candidateAction();
            assertEquivalent(baselineResult, candidateResult);
            Assert.That(checksum(candidateResult), Is.EqualTo(checksum(baselineResult)));

            for (int i = 0; i < WarmupCount; i++)
            {
                diagnosticSink ^= checksum(baselineAction());
                diagnosticSink ^= checksum(candidateAction());
            }

            var baselineTicks = new long[SetCount];
            var candidateTicks = new long[SetCount];
            var baselineAllocations = new long[SetCount];
            var candidateAllocations = new long[SetCount];
            var allocationCalibrations = new long[SetCount];

            for (int set = 0; set < SetCount; set++)
            {
                MeasureComparisonSet(
                    IterationsPerSet,
                    () => diagnosticSink ^= checksum(baselineAction()),
                    () => diagnosticSink ^= checksum(candidateAction()),
                    candidateFirst: (set & 1) != 0,
                    out baselineTicks[set],
                    out candidateTicks[set],
                    out baselineAllocations[set],
                    out candidateAllocations[set],
                    out allocationCalibrations[set]);
            }

            long baselineTickMedian = Median(baselineTicks);
            long candidateTickMedian = Median(candidateTicks);
            long baselineAllocationMedian = Median(baselineAllocations);
            long candidateAllocationMedian = Median(candidateAllocations);
            long allocationCalibrationMedian = Median(allocationCalibrations);
            double cpuRatio = baselineTickMedian == 0
                ? 0d
                : (double)candidateTickMedian / baselineTickMedian;
            bool allocationReduced = candidateAllocationMedian < baselineAllocationMedian;
            bool cpuWithinGate = cpuRatio <= 1.05d;

            string summary =
                $"{name}: warm-up={WarmupCount}, sets={SetCount}, iterations/set={IterationsPerSet}, " +
                $"baseline median ticks/op={baselineTickMedian}, candidate median ticks/op={candidateTickMedian}, " +
                $"CPU ratio={cpuRatio:F3}, CPU gate <= 1.05={cpuWithinGate}, " +
                $"empty calibration median GC.Alloc calls/set={allocationCalibrationMedian}, " +
                $"baseline median GC.Alloc calls/set={baselineAllocationMedian}, " +
                $"candidate median GC.Alloc calls/set={candidateAllocationMedian}, " +
                $"allocation reduced={allocationReduced}";

            TestContext.WriteLine(summary);
            UnityEngine.Debug.Log(summary);
        }



        private static void MeasureComparisonSet(
            int iterations,
            Action baselineAction,
            Action candidateAction,
            bool candidateFirst,
            out long baselineTicksPerOperation,
            out long candidateTicksPerOperation,
            out long baselineAllocationCalls,
            out long candidateAllocationCalls,
            out long allocationCalibrationCalls)
        {
            MeasureRawSet(iterations, EmptyDiagnosticAction, out _, out allocationCalibrationCalls);

            if (candidateFirst)
            {
                MeasureRawSet(iterations, candidateAction, out candidateTicksPerOperation, out long candidateRawAllocations);
                MeasureRawSet(iterations, baselineAction, out baselineTicksPerOperation, out long baselineRawAllocations);
                baselineAllocationCalls = Math.Max(0L, baselineRawAllocations - allocationCalibrationCalls);
                candidateAllocationCalls = Math.Max(0L, candidateRawAllocations - allocationCalibrationCalls);
                return;
            }

            MeasureRawSet(iterations, baselineAction, out baselineTicksPerOperation, out long baselineRawAllocationCalls);
            MeasureRawSet(iterations, candidateAction, out candidateTicksPerOperation, out long candidateRawAllocationCalls);
            baselineAllocationCalls = Math.Max(0L, baselineRawAllocationCalls - allocationCalibrationCalls);
            candidateAllocationCalls = Math.Max(0L, candidateRawAllocationCalls - allocationCalibrationCalls);
        }



        private static void MeasureRawSet(
            int iterations,
            Action action,
            out long ticksPerOperation,
            out long allocationCalls)
        {
            var samples = new List<ProfilerRecorderSample>(1);

            using var allocationRecorder = new ProfilerRecorder(
                ProfilerCategory.Memory,
                "GC.Alloc",
                1,
                AllocationRecorderOptions);
            Assert.That(allocationRecorder.Valid, Is.True);

            allocationRecorder.Start();
            long timestampBefore = Stopwatch.GetTimestamp();

            for (int i = 0; i < iterations; i++)
            {
                action();
            }

            long elapsedTicks = Stopwatch.GetTimestamp() - timestampBefore;
            allocationRecorder.Stop();
            allocationRecorder.CopyTo(samples, true);

            allocationCalls = 0L;

            for (int i = 0; i < samples.Count; i++)
            {
                allocationCalls += samples[i].Count;
            }

            ticksPerOperation = elapsedTicks / iterations;
        }



        private static long Median(long[] values)
        {
            long a = values[0];
            long b = values[1];
            long c = values[2];

            if (a > b) { (a, b) = (b, a); }
            if (b > c) { (b, c) = (c, b); }
            if (a > b) { (a, b) = (b, a); }
            return b;
        }



        private readonly struct Candidate
        {
            public readonly int Id;
            public readonly int Score;

            public Candidate(int id, int score)
            {
                Id = id;
                Score = score;
            }
        }



        private readonly struct SpaceMetric
        {
            public readonly int Id;
            public readonly float Area;
            public readonly int NodeCount;

            public SpaceMetric(int id, float area, int nodeCount)
            {
                Id = id;
                Area = area;
                NodeCount = nodeCount;
            }
        }
    }
}
