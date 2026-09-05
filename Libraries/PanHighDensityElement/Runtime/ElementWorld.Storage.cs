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



        /// <summary>
        /// generation-safe key로 모든 Element가 보유한 최소 2D pose를 조회합니다.
        /// </summary>
        public bool TryGetPose(ElementKey key, out ElementPose2D pose)
        {
            if (TryGetRegistryEntry(key, out ElementRegistryEntry entry) &&
                entry.Lifecycle == ElementLifecycle.Alive &&
                entry.PoseDenseIndex >= 0 &&
                entry.PoseDenseIndex < poseStates.Length)
            {
                NativeElementPoseState state = poseStates[entry.PoseDenseIndex];
                if (state.Key == key)
                {
                    pose = state.ToPose();
                    return true;
                }
            }

            pose = default;
            return false;
        }





        /// <summary>
        /// generation-safe key가 가리키는 최소 2D pose와 기존 호환 state를 함께 갱신합니다.
        /// </summary>
        public bool TrySetPose(ElementKey key, in ElementPose2D pose)
        {
            if (!TryGetRegistryEntry(key, out ElementRegistryEntry entry) ||
                entry.Lifecycle != ElementLifecycle.Alive ||
                !TryGetState(key, out NativeElementState state))
            {
                return false;
            }

            state.PreviousPosition = pose.PreviousPosition;
            state.Position = pose.Position;
            state.PreviousRotationRadians = pose.PreviousRotationRadians;
            state.RotationRadians = pose.RotationRadians;
            state.PreviousScale = pose.PreviousScale;
            state.Scale = pose.Scale;
            SetState(entry, state);
            if (RequiresPhysicsBody(state.Capabilities)) { physicsCoreLane.ApplyState(in state); }
            return true;
        }





        public bool TryGetSnapshot(ElementKey key, out ElementSnapshot snapshot)
        {
            if (TryGetState(key, out NativeElementState state))
            {
                snapshot = state.ToSnapshot(GetRemainingLifetimeUnits(key));
                return true;
            }

            snapshot = default;
            return false;
        }





        /// <summary>
        /// 고정 스텝의 이전·현재 pose를 보간한 표시용 snapshot을 조회합니다.
        /// </summary>
        public bool TryGetRenderSnapshot(
            ElementKey key,
            float interpolationAlpha,
            out ElementRenderSnapshot snapshot)
        {
            if (TryGetState(key, out NativeElementState state))
            {
                snapshot = new ElementRenderSnapshot(
                    state.Key,
                    state.PreviousPosition,
                    state.Position,
                    state.PreviousRotationRadians,
                    state.RotationRadians,
                    interpolationAlpha);
                return true;
            }

            snapshot = default;
            return false;
        }





        /// <summary>
        /// 현재 world에서 살아 있는 Element의 generation-safe handle을 가져옵니다.
        /// </summary>
        public bool TryGetHandle(ElementKey key, out ElementHandle handle)
        {
            if (!disposed && IsAlive(key))
            {
                handle = new ElementHandle(this, key);
                return true;
            }

            handle = default;
            return false;
        }





        /// <summary>
        /// 현재 World의 모든 살아 있는 Element snapshot을 호출자가 재사용하는 목록에 복사합니다.
        /// </summary>
        public void CopySnapshots(List<ElementSnapshot> destination)
        {
            if (destination == null) { throw new ArgumentNullException(nameof(destination)); }
            ThrowIfDisposed();

            destination.Clear();
            if (destination.Capacity < AliveCount) { destination.Capacity = AliveCount; }
            AppendSnapshots(kinematicStates, destination);
            AppendSnapshots(areaStates, destination);
            AppendSnapshots(physicsStates, destination);
        }





        public ElementWorldDiagnostics GetDiagnostics() => new ElementWorldDiagnostics(
            AliveCount,
            KinematicCount,
            AreaCount,
            CountSimulatedElements(),
            physicsCoreLane?.BodyCount ?? 0,
            lastCommandCount,
            LastFactCount,
            LastCoreQueryFactCount,
            lastStrictCcdCandidateCount,
            lastForcedStrictCcdCount,
            lastTeleportCount,
            directionalMotionFeatures.Count,
            waveMotionFeatures.Count,
            visualOrientationFeatures.Count,
            boundaryFeatures.Count,
            lastBoundaryExitCount,
            fixedStepIndex,
            LastSimulationMilliseconds,
            LastFactDispatchMilliseconds);





        public void CopyRenderItems(NativeList<ElementRenderItem> destination)
        {
            CopyRenderItems(destination, 1f);
        }





        internal void CopyRenderItems(NativeList<ElementRenderItem> destination, float interpolationAlpha)
        {
            ThrowIfDisposed();
            destination.Clear();
            int requiredCapacity = AliveCount;
            if (destination.Capacity < requiredCapacity) { destination.Capacity = requiredCapacity; }

            float alpha = ElementRenderSnapshot.NormalizeAlpha(interpolationAlpha);
            CopyRenderItems(kinematicStates, destination, alpha);
            CopyRenderItems(areaStates, destination, alpha);
            CopyRenderItems(physicsStates, destination, alpha);
        }





        private static bool RequiresPhysicsBody(ElementCapabilities capabilities) =>
            (capabilities & (ElementCapabilities.AreaSensor2D |
                ElementCapabilities.DynamicBody2D |
                ElementCapabilities.QueryTarget2D)) != 0;





        private void AppendSnapshots(
            NativeList<NativeElementState> states,
            List<ElementSnapshot> destination)
        {
            for (int i = 0; i < states.Length; i++)
            {
                NativeElementState state = states[i];
                if (state.Lifecycle == ElementLifecycle.Alive ||
                    state.Lifecycle == ElementLifecycle.DespawnPending)
                {
                    destination.Add(state.ToSnapshot(GetRemainingLifetimeUnits(state.Key)));
                }
            }
        }





        private void RemoveStateAtSwapBack(ElementLane lane, int denseIndex)
        {
            NativeList<NativeElementState> states = GetStates(lane);
            int lastIndex = states.Length - 1;
            ElementKey movedKey = states[lastIndex].Key;
            states.RemoveAtSwapBack(denseIndex);

            if (denseIndex == lastIndex) { return; }

            ElementRegistryEntry movedEntry = registry[movedKey.Slot];
            movedEntry.DenseIndex = denseIndex;
            registry[movedKey.Slot] = movedEntry;
        }





        private void RemovePoseAtSwapBack(int denseIndex)
        {
            if (denseIndex < 0 || denseIndex >= poseStates.Length) { return; }
            int lastIndex = poseStates.Length - 1;
            ElementKey movedKey = poseStates[lastIndex].Key;
            poseStates.RemoveAtSwapBack(denseIndex);
            if (denseIndex == lastIndex) { return; }

            ElementRegistryEntry movedEntry = registry[movedKey.Slot];
            movedEntry.PoseDenseIndex = denseIndex;
            registry[movedKey.Slot] = movedEntry;
        }





        private NativeList<NativeElementState> GetStates(ElementLane lane)
        {
            switch (lane)
            {
                case ElementLane.QuerySprite2D: return kinematicStates;
                case ElementLane.AreaSensorSprite2D: return areaStates;
                case ElementLane.DynamicBodySprite2D: return physicsStates;
                default: throw new ArgumentOutOfRangeException(nameof(lane));
            }
        }





        private bool TryGetState(ElementKey key, out NativeElementState state)
        {
            if (TryGetRegistryEntry(key, out ElementRegistryEntry entry) && entry.DenseIndex >= 0)
            {
                NativeList<NativeElementState> states = GetStates(entry.Lane);
                if (entry.DenseIndex < states.Length)
                {
                    state = states[entry.DenseIndex];
                    return state.Key == key;
                }
            }

            state = default;
            return false;
        }





        private bool TryGetRegistryEntry(ElementKey key, out ElementRegistryEntry entry)
        {
            if (!disposed && key.WorldId == WorldId && key.Slot >= 0 && key.Slot < registry.Length)
            {
                entry = registry[key.Slot];
                return entry.Generation == key.Generation && entry.Lifecycle != ElementLifecycle.Despawned;
            }

            entry = default;
            return false;
        }





        private static void CopyRenderItems(
            NativeList<NativeElementState> states,
            NativeList<ElementRenderItem> destination,
            float interpolationAlpha)
        {
            for (int i = 0; i < states.Length; i++)
            {
                NativeElementState state = states[i];
                if (state.Lifecycle == ElementLifecycle.Despawned ||
                    (state.Capabilities & ElementCapabilities.SpriteVisual2D) == 0)
                {
                    continue;
                }

                var renderSnapshot = new ElementRenderSnapshot(
                    state.Key,
                    state.PreviousPosition,
                    state.Position,
                    state.PreviousRotationRadians,
                    state.RotationRadians,
                    interpolationAlpha);
                destination.Add(new ElementRenderItem
                {
                    VisualId = state.VisualId,
                    Position = renderSnapshot.Position,
                    RotationRadians = renderSnapshot.RotationRadians,
                    Scale = math.lerp(state.PreviousScale, state.Scale, interpolationAlpha),
                    Color = state.Color
                });
            }
        }
    }
}
