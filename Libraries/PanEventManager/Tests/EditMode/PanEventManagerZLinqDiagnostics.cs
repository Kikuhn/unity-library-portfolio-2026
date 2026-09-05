using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using Pan.Event;
using Pan.Util;
using Unity.Profiling;
using UnityEditor.Compilation;
using ZLinq;



namespace Pan.EventManagers.Tests
{
    public sealed class PanEventManagerZLinqDiagnostics
    {
        private const int DiagnosticWarmupIterations = 10;
        private const int DiagnosticIterationsPerSet = 50;
        private const int DiagnosticSetCount = 3;



        private readonly struct DiagnosticMeasurement
        {
            public DiagnosticMeasurement(long elapsedTicks, long allocationCalls, int checksum)
            {
                ElapsedTicks = elapsedTicks;
                AllocationCalls = allocationCalls;
                Checksum = checksum;
            }



            public long ElapsedTicks { get; }



            public long AllocationCalls { get; }



            public int Checksum { get; }



            public double ElapsedMilliseconds => ElapsedTicks * 1000d / Stopwatch.Frequency;
        }



        private static readonly Action EmptyAction = Empty;



        [TestCase(typeof(PanBaseEvent))]
        [TestCase(typeof(PanBaseEventValue))]
        public void InitializableTypeDiscovery_ZLinqFilter_MatchesSystemLinqReference(Type baseType)
        {
            Type[] source = SU_Collection_Types.GetTypesAssignableTo(
                baseType,
                SU_Collection_Types.TypeSearch.ConcreteClasses);
            Type[] expected = FilterWithSystemLinq(source);

            MethodInfo getInitializableTypes = typeof(PanEventsInitializeSettingSbjectBase).GetMethod(
                "GetInitializableTypes",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.IsNotNull(getInitializableTypes);

            Type[] actual = (Type[])getInitializableTypes.Invoke(null, new object[] { baseType });

            CollectionAssert.AreEqual(expected, actual);
        }



        [Test]
        public void InitializableTypeFilter_SystemLinqAndZLinq_ReportAllocationAndTimeWithoutThreshold()
        {
            Type[] source = SU_Collection_Types.GetTypesAssignableTo(
                typeof(PanBaseEventValue),
                SU_Collection_Types.TypeSearch.ConcreteClasses);
            Func<Type[]> systemLinqFilter = () => FilterWithSystemLinq(source);
            Func<Type[]> zLinqFilter = () => FilterWithZLinq(source);

            for (int i = 0; i < DiagnosticWarmupIterations; i++)
            {
                Type[] systemWarmup = systemLinqFilter();
                Type[] zLinqWarmup = zLinqFilter();
                CollectionAssert.AreEqual(systemWarmup, zLinqWarmup);
            }

            var systemLinqSets = new DiagnosticMeasurement[DiagnosticSetCount];
            var zLinqSets = new DiagnosticMeasurement[DiagnosticSetCount];
            long emptyActionAllocationCalls;

            var allocationRecorder = new ProfilerRecorder(
                ProfilerCategory.Memory,
                "GC.Alloc",
                1,
                ProfilerRecorderOptions.WrapAroundWhenCapacityReached |
                ProfilerRecorderOptions.SumAllSamplesInFrame |
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);

            try
            {
                Assert.IsTrue(
                    allocationRecorder.Valid,
                    "Memory/GC.Alloc ProfilerRecorder를 현재 Unity Editor에서 사용할 수 없습니다.");

                MeasureAllocationCalls(EmptyAction, ref allocationRecorder);
                emptyActionAllocationCalls = MeasureAllocationCalls(EmptyAction, ref allocationRecorder);

                Assert.Zero(
                    emptyActionAllocationCalls,
                    "빈 작업에서도 GC.Alloc이 기록되어 allocation 호출 수를 신뢰할 수 없습니다.");

                for (int setIndex = 0; setIndex < DiagnosticSetCount; setIndex++)
                {
                    if ((setIndex & 1) == 0)
                    {
                        systemLinqSets[setIndex] = Measure(
                            systemLinqFilter,
                            DiagnosticIterationsPerSet,
                            ref allocationRecorder);
                        zLinqSets[setIndex] = Measure(
                            zLinqFilter,
                            DiagnosticIterationsPerSet,
                            ref allocationRecorder);
                    }
                    else
                    {
                        zLinqSets[setIndex] = Measure(
                            zLinqFilter,
                            DiagnosticIterationsPerSet,
                            ref allocationRecorder);
                        systemLinqSets[setIndex] = Measure(
                            systemLinqFilter,
                            DiagnosticIterationsPerSet,
                            ref allocationRecorder);
                    }

                    Assert.AreEqual(systemLinqSets[setIndex].Checksum, zLinqSets[setIndex].Checksum);
                }
            }
            finally
            {
                allocationRecorder.Dispose();
            }

            long systemElapsedTicksMedian = GetMedianElapsedTicks(systemLinqSets);
            long zLinqElapsedTicksMedian = GetMedianElapsedTicks(zLinqSets);
            long systemAllocationCallsMedian = GetMedianAllocationCalls(systemLinqSets);
            long zLinqAllocationCallsMedian = GetMedianAllocationCalls(zLinqSets);
            bool allocationReduced = zLinqAllocationCallsMedian < systemAllocationCallsMedian;
            bool cpuWithinTolerance = zLinqElapsedTicksMedian <= systemElapsedTicksMedian * 1.05d;

            var message = new StringBuilder();
            message.AppendLine(
                $"PanEventManager 초기화 타입 필터 진단 " +
                $"(warm-up {DiagnosticWarmupIterations}회, {DiagnosticIterationsPerSet}회 x {DiagnosticSetCount}세트, " +
                $"ProfilerRecorder Memory/GC.Alloc allocation calls, 빈 작업 보정 {emptyActionAllocationCalls} calls)");

            for (int setIndex = 0; setIndex < DiagnosticSetCount; setIndex++)
            {
                message.AppendLine(
                    $"세트 {setIndex + 1}: System.Linq {systemLinqSets[setIndex].ElapsedMilliseconds:F3} ms / " +
                    $"{systemLinqSets[setIndex].AllocationCalls} allocation calls, " +
                    $"ZLinq {zLinqSets[setIndex].ElapsedMilliseconds:F3} ms / " +
                    $"{zLinqSets[setIndex].AllocationCalls} allocation calls");
            }

            message.AppendLine(
                $"중앙값: System.Linq {ToMilliseconds(systemElapsedTicksMedian):F3} ms / " +
                $"{systemAllocationCallsMedian} allocation calls, " +
                $"ZLinq {ToMilliseconds(zLinqElapsedTicksMedian):F3} ms / " +
                $"{zLinqAllocationCallsMedian} allocation calls");
            message.AppendLine(
                $"진단 판정(assert 아님): allocation 감소={allocationReduced}, CPU 중앙값 5% 이내 비열화={cpuWithinTolerance}");

            string diagnosticMessage = message.ToString();
            TestContext.WriteLine(diagnosticMessage);
            UnityEngine.Debug.Log(diagnosticMessage);
        }



        private static Type[] FilterWithSystemLinq(Type[] source)
        {
            var editorAssemblyNames = new HashSet<string>(
                CompilationPipeline
                    .GetAssemblies(AssembliesType.Editor)
                    .Select(assembly => assembly.name),
                StringComparer.Ordinal);
            var playerAssemblyNames = new HashSet<string>(
                CompilationPipeline
                    .GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
                    .Select(assembly => assembly.name),
                StringComparer.Ordinal);

            return source
                .Where(type =>
                {
                    var assembly = type.Assembly;
                    string assemblyName = assembly.GetName().Name;

                    if (editorAssemblyNames.Contains(assemblyName))
                    {
                        return playerAssemblyNames.Contains(assemblyName);
                    }

                    return !ReferencesEditorOrTestAssemblyWithSystemLinq(assembly);
                })
                .ToArray();
        }



        private static Type[] FilterWithZLinq(Type[] source)
        {
            var editorAssemblyNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var assembly in CompilationPipeline
                .GetAssemblies(AssembliesType.Editor)
                .AsValueEnumerable())
            {
                editorAssemblyNames.Add(assembly.name);
            }

            var playerAssemblyNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var assembly in CompilationPipeline
                .GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
                .AsValueEnumerable())
            {
                playerAssemblyNames.Add(assembly.name);
            }

            return source
                .AsValueEnumerable()
                .Where(type =>
                {
                    var assembly = type.Assembly;
                    string assemblyName = assembly.GetName().Name;

                    if (editorAssemblyNames.Contains(assemblyName))
                    {
                        return playerAssemblyNames.Contains(assemblyName);
                    }

                    return !ReferencesEditorOrTestAssemblyWithZLinq(assembly);
                })
                .ToArray();
        }



        private static bool ReferencesEditorOrTestAssemblyWithSystemLinq(System.Reflection.Assembly assembly)
        {
            if (IsEditorOrTestAssemblyName(assembly.GetName().Name)) { return true; }

            return assembly
                .GetReferencedAssemblies()
                .Any(reference => IsEditorOrTestAssemblyName(reference.Name));
        }



        private static bool ReferencesEditorOrTestAssemblyWithZLinq(System.Reflection.Assembly assembly)
        {
            if (IsEditorOrTestAssemblyName(assembly.GetName().Name)) { return true; }

            return assembly
                .GetReferencedAssemblies()
                .AsValueEnumerable()
                .Any(reference => IsEditorOrTestAssemblyName(reference.Name));
        }



        private static bool IsEditorOrTestAssemblyName(string assemblyName)
        {
            return assemblyName.StartsWith("UnityEditor", StringComparison.Ordinal) ||
                string.Equals(assemblyName, "UnityEngine.TestRunner", StringComparison.Ordinal) ||
                string.Equals(assemblyName, "UnityEditor.TestRunner", StringComparison.Ordinal) ||
                string.Equals(assemblyName, "nunit.framework", StringComparison.Ordinal);
        }



        private static DiagnosticMeasurement Measure(
            Func<Type[]> filter,
            int iterations,
            ref ProfilerRecorder allocationRecorder)
        {
            int checksum = 0;
            long elapsedTicks;

            allocationRecorder.Reset();
            allocationRecorder.Start();
            long elapsedTicksBefore = Stopwatch.GetTimestamp();

            try
            {
                for (int i = 0; i < iterations; i++)
                {
                    checksum += filter().Length;
                }
            }
            finally
            {
                elapsedTicks = Stopwatch.GetTimestamp() - elapsedTicksBefore;
                allocationRecorder.Stop();
            }

            long allocationCalls = GetAllocationCallCount(ref allocationRecorder);

            return new DiagnosticMeasurement(elapsedTicks, allocationCalls, checksum);
        }



        private static long GetMedianElapsedTicks(DiagnosticMeasurement[] measurements)
        {
            var values = new long[measurements.Length];

            for (int i = 0; i < measurements.Length; i++)
            {
                values[i] = measurements[i].ElapsedTicks;
            }

            Array.Sort(values);
            return values[values.Length / 2];
        }



        private static long GetMedianAllocationCalls(DiagnosticMeasurement[] measurements)
        {
            var values = new long[measurements.Length];

            for (int i = 0; i < measurements.Length; i++)
            {
                values[i] = measurements[i].AllocationCalls;
            }

            Array.Sort(values);
            return values[values.Length / 2];
        }



        private static double ToMilliseconds(long elapsedTicks)
        {
            return elapsedTicks * 1000d / Stopwatch.Frequency;
        }



        private static long MeasureAllocationCalls(Action action, ref ProfilerRecorder allocationRecorder)
        {
            allocationRecorder.Reset();
            allocationRecorder.Start();

            try
            {
                action();
            }
            finally
            {
                allocationRecorder.Stop();
            }

            return GetAllocationCallCount(ref allocationRecorder);
        }



        private static long GetAllocationCallCount(ref ProfilerRecorder allocationRecorder)
        {
            return allocationRecorder.Count == 0
                ? 0
                : allocationRecorder.GetSample(0).Count;
        }



        private static void Empty()
        {
        }
    }
}
