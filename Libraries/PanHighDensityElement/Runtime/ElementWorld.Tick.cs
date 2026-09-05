using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;



namespace Pan.HighDensityElement
{
    public sealed partial class ElementWorld
    {



        public void Tick(float deltaTime, ushort substepIndex = 0)
        {
            ThrowIfDisposed();
            if (deltaTime < 0f) { throw new ArgumentOutOfRangeException(nameof(deltaTime)); }

            fixedStepIndex = fixedStepIndex == uint.MaxValue ? 1u : fixedStepIndex + 1u;
            long simulationStart = EnableTimingMetrics ? System.Diagnostics.Stopwatch.GetTimestamp() : 0L;
            lastCommandCount = DrainCommands(commands);
            kinematicFactQueue.Clear();
            areaFactQueue.Clear();
            physicsFactQueue.Clear();
            factBuffer.Clear();
            lastBoundaryExitCount = 0;
            int kinematicCount = kinematicStates.Length;
            int areaCount = areaStates.Length;
            int physicsCount = physicsStates.Length;
            JobHandle kinematicMotion = default;
            JobHandle areaMotion = default;
            JobHandle physicsMotion = default;

            try
            {
                if (kinematicCount > 0)
                {
                    kinematicMotion = CreateMotionJob(
                        kinematicStates.AsArray(),
                        deltaTime,
                        substepIndex,
                        kinematicFactQueue.AsParallelWriter()).Schedule(kinematicCount, 64);
                }

                if (areaCount > 0)
                {
                    areaMotion = CreateMotionJob(
                        areaStates.AsArray(),
                        deltaTime,
                        substepIndex,
                        areaFactQueue.AsParallelWriter()).Schedule(areaCount, 32);
                }

                if (physicsCount > 0)
                {
                    physicsMotion = CreateMotionJob(
                        physicsStates.AsArray(),
                        deltaTime,
                        substepIndex,
                        physicsFactQueue.AsParallelWriter()).Schedule(physicsCount, 16);
                }
            }
            finally
            {
                //? 예약 이후 어느 단계가 실패해도 NativeContainer를 점유한 Job을 world 경계 밖에 남기지 않습니다.
                JobHandle motionDependency = JobHandle.CombineDependencies(kinematicMotion, areaMotion);
                JobHandle.CombineDependencies(motionDependency, physicsMotion).Complete();
            }

            SynchronizeAllPoseStates();
            AdvanceLifetimeFeatures(
                new ElementUpdateContext(deltaTime, deltaTime),
                advanceWithFixedTick: true,
                enqueueFacts: true);

            //? 일반 bodyless QuerySensor는 dictionary lookup에서 제외하고, opt-in QueryTarget shape만 동기화합니다.
            physicsCoreLane.ApplyKinematicQueryTargetStates(kinematicStates);
            physicsCoreLane.ApplyKinematicStates(areaStates);
            physicsCoreLane.ApplyKinematicStates(physicsStates);
            physicsCoreLane.Simulate(deltaTime);
            physicsCoreLane.SynchronizeDynamicStates(physicsStates);
            SynchronizePoseStates(physicsStates);
            physicsCoreLane.CollectFacts(factBuffer, substepIndex, fixedStepIndex);
            RefreshSimulatedStrictCcdFlags();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RefreshDetailedDiagnostics();
#endif

            JobHandle kinematicQuery = default;
            JobHandle physicsQuery = default;
            try
            {
                if (kinematicCount > 0)
                {
                    kinematicQuery = new PhysicsCoreProjectileQueryJob
                    {
                        World = physicsCoreLane.World,
                        States = kinematicStates.AsArray(),
                        Registry = registry.AsArray(),
                        KinematicStates = kinematicStates.AsArray(),
                        AreaStates = areaStates.AsArray(),
                        PhysicsStates = physicsStates.AsArray(),
                        MovingProjections = physicsCoreLane.GetMovingProjections(),
                        WorldId = WorldId,
                        SubstepIndex = substepIndex,
                        FixedStepIndex = fixedStepIndex,
                        Facts = kinematicFactQueue.AsParallelWriter()
                    }.Schedule(kinematicCount, 32);
                }

                if (physicsCount > 0)
                {
                    physicsQuery = new PhysicsCoreProjectileQueryJob
                    {
                        World = physicsCoreLane.World,
                        States = physicsStates.AsArray(),
                        Registry = registry.AsArray(),
                        KinematicStates = kinematicStates.AsArray(),
                        AreaStates = areaStates.AsArray(),
                        PhysicsStates = physicsStates.AsArray(),
                        MovingProjections = physicsCoreLane.GetMovingProjections(),
                        WorldId = WorldId,
                        SubstepIndex = substepIndex,
                        FixedStepIndex = fixedStepIndex,
                        Facts = physicsFactQueue.AsParallelWriter()
                    }.Schedule(physicsCount, 32);
                }
            }
            finally
            {
                JobHandle.CombineDependencies(kinematicQuery, physicsQuery).Complete();
            }

            CollectQueuedFacts();
            SortFactsDeterministically();
            DeduplicateContactFacts();
            LastFactCount = factBuffer.Length;
            LastCoreQueryFactCount = 0;
            for (int i = 0; i < factBuffer.Length; i++)
            {
                if (factBuffer[i].Type == ElementFactType.BoundaryExited) { lastBoundaryExitCount++; }
            }
            if (EnableTimingMetrics)
            {
                for (int i = 0; i < factBuffer.Length; i++)
                {
                    if (factBuffer[i].Type == ElementFactType.Contact &&
                        TryGetSnapshot(factBuffer[i].Element, out ElementSnapshot snapshot) &&
                        (snapshot.Capabilities & ElementCapabilities.QuerySensor2D) != 0)
                    {
                        LastCoreQueryFactCount++;
                    }
                }
            }

            long dispatchStart = 0L;
            if (EnableTimingMetrics)
            {
                dispatchStart = System.Diagnostics.Stopwatch.GetTimestamp();
                LastSimulationMilliseconds = TicksToMilliseconds(dispatchStart - simulationStart);
            }

            for (int i = 0; i < factBuffer.Length; i++)
            {
                ElementFact fact = factBuffer[i];
                DispatchFact(in fact);
            }

            DespawnExpiredStates();
            lastCommandCount += DrainCommands(postEventCommands);

            if (EnableTimingMetrics)
            {
                LastFactDispatchMilliseconds = TicksToMilliseconds(
                    System.Diagnostics.Stopwatch.GetTimestamp() - dispatchStart);
            }
        }



        private ElementMotionJob CreateMotionJob(
            NativeArray<NativeElementState> states,
            float deltaTime,
            ushort substepIndex,
            NativeQueue<ElementFact>.ParallelWriter facts)
        {
            return new ElementMotionJob
            {
                States = states,
                DeltaTime = deltaTime,
                SubstepIndex = substepIndex,
                FixedStepIndex = fixedStepIndex,
                Facts = facts,
                DirectionalMotionIndices = directionalMotionFeatures.GetIndicesReadOnly(),
                DirectionalMotionFeatures = directionalMotionFeatures.GetValuesArray(),
                WaveMotionIndices = waveMotionFeatures.GetIndicesReadOnly(),
                WaveMotionFeatures = waveMotionFeatures.GetValuesArray(),
                VisualOrientationIndices = visualOrientationFeatures.GetIndicesReadOnly(),
                VisualOrientationFeatures = visualOrientationFeatures.GetValuesArray(),
                BoundaryIndices = boundaryFeatures.GetIndicesReadOnly(),
                BoundaryFeatures = boundaryFeatures.GetValuesArray(),
                WorldBoundaryBounds = worldBoundaryBounds,
                ViewBoundaryBounds = viewBoundaryBounds,
                HasWorldBoundaryBounds = hasWorldBoundaryBounds,
                HasViewBoundaryBounds = hasViewBoundaryBounds
            };
        }





        /// <summary>
        /// PhysicsCore fixed Tick을 실행하지 않고 sparse Update Feature만 갱신합니다.
        /// </summary>
        public void TickUpdateFeatures(in ElementUpdateContext context)
        {
            ThrowIfDisposed();
            factBuffer.Clear();
            AdvanceLifetimeFeatures(in context, advanceWithFixedTick: false, enqueueFacts: false);
            SortFactsDeterministically();
            LastFactCount = factBuffer.Length;

            for (int i = 0; i < factBuffer.Length; i++)
            {
                ElementFact fact = factBuffer[i];
                DispatchFact(in fact);
            }

            DespawnExpiredStates();
            lastCommandCount = DrainCommands(postEventCommands);
        }





        private static double TicksToMilliseconds(long ticks)
        {
            return ticks * 1000d / System.Diagnostics.Stopwatch.Frequency;
        }





        private void RefreshSimulatedStrictCcdFlags()
        {
            for (int i = 0; i < physicsStates.Length; i++)
            {
                NativeElementState state = physicsStates[i];
                if ((state.Capabilities & ElementCapabilities.DynamicBody2D) == 0) { continue; }

                float minimumExtent = math.max(
                    0.0001f,
                    state.Radius * 2f * math.max(0.0001f, math.min(math.abs(state.Scale.x), math.abs(state.Scale.y))));
                float threshold = minimumExtent * math.max(0.01f, state.StrictCcdThresholdRatio);
                bool automatic = state.StrictCcdOverride == StrictCcdOverride2D.Auto &&
                    math.lengthsq(state.Position - state.PreviousPosition) > threshold * threshold;
                state.StrictCcdActive = (byte)(state.StrictCcdForcedThisStep != 0 ||
                    state.StrictCcdOverride == StrictCcdOverride2D.ForceOn ||
                    automatic ? 1 : 0);
                physicsStates[i] = state;
            }
        }


        private void RefreshDetailedDiagnostics()
        {
            if (!EnableDetailedDiagnostics)
            {
                lastStrictCcdCandidateCount = 0;
                lastForcedStrictCcdCount = 0;
                lastTeleportCount = 0;
                return;
            }

            lastStrictCcdCandidateCount = 0;
            lastForcedStrictCcdCount = 0;
            lastTeleportCount = 0;
            AccumulateDetailedDiagnostics(kinematicStates);
            AccumulateDetailedDiagnostics(areaStates);
            AccumulateDetailedDiagnostics(physicsStates);
        }





        private void AccumulateDetailedDiagnostics(NativeList<NativeElementState> states)
        {
            for (int i = 0; i < states.Length; i++)
            {
                NativeElementState state = states[i];
                if (state.StrictCcdActive != 0) { lastStrictCcdCandidateCount++; }
                if (state.StrictCcdForcedThisStep != 0 || state.StrictCcdOverride == StrictCcdOverride2D.ForceOn)
                {
                    lastForcedStrictCcdCount++;
                }
                if (state.TeleportedThisStep != 0) { lastTeleportCount++; }
            }
        }





        private int CountSimulatedElements()
        {
            int count = 0;
            for (int i = 0; i < physicsStates.Length; i++)
            {
                if ((physicsStates[i].Capabilities & ElementCapabilities.DynamicBody2D) != 0) { count++; }
            }
            return count;
        }





        private void SynchronizeAllPoseStates()
        {
            SynchronizePoseStates(kinematicStates);
            SynchronizePoseStates(areaStates);
            SynchronizePoseStates(physicsStates);
        }





        private void SynchronizePoseStates(NativeList<NativeElementState> states)
        {
            for (int i = 0; i < states.Length; i++)
            {
                NativeElementState state = states[i];
                if (TryGetRegistryEntry(state.Key, out ElementRegistryEntry entry))
                {
                    SynchronizePoseState(entry, in state);
                }
            }
        }





        private void SynchronizePoseState(ElementRegistryEntry entry, in NativeElementState state)
        {
            if (entry.PoseDenseIndex < 0 || entry.PoseDenseIndex >= poseStates.Length) { return; }
            NativeElementPoseState pose = poseStates[entry.PoseDenseIndex];
            if (pose.Key != state.Key) { return; }

            pose.PreviousPosition = state.PreviousPosition;
            pose.Position = state.Position;
            pose.PreviousRotationRadians = state.PreviousRotationRadians;
            pose.RotationRadians = state.RotationRadians;
            pose.PreviousScale = state.PreviousScale;
            pose.Scale = state.Scale;
            poseStates[entry.PoseDenseIndex] = pose;
        }
    }
}
