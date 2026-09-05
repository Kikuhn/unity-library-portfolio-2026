#if UNITY_EDITOR || DEVELOPMENT_BUILD

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.U2D.Physics;



namespace Pan.HighDensityElement
{
    public sealed partial class ElementWorld
    {
        internal void AcquireDebugCapture(bool detailedDiagnostics, bool factCapture)
        {
            if (detailedDiagnostics) { debugDetailedDiagnosticsLeaseCount++; }
            if (factCapture) { debugFactCaptureLeaseCount++; }
        }

        internal void ReleaseDebugCapture(bool detailedDiagnostics, bool factCapture)
        {
            if (detailedDiagnostics)
            {
                debugDetailedDiagnosticsLeaseCount = Math.Max(0, debugDetailedDiagnosticsLeaseCount - 1);
            }
            if (factCapture)
            {
                debugFactCaptureLeaseCount = Math.Max(0, debugFactCaptureLeaseCount - 1);
            }
        }



        /// <summary>
        /// 선택된 Element의 native storage와 sparse feature 상태를 값 복사본으로 조회합니다.
        /// </summary>
        public bool TryGetDebugSnapshot(ElementKey key, out ElementDebugSnapshot snapshot)
        {
            if (!TryGetRegistryEntry(key, out ElementRegistryEntry entry) ||
                entry.Lifecycle == ElementLifecycle.Despawned ||
                !TryGetState(key, out NativeElementState state) ||
                entry.PoseDenseIndex < 0 ||
                entry.PoseDenseIndex >= poseStates.Length)
            {
                snapshot = default;
                return false;
            }

            NativeElementPoseState poseState = poseStates[entry.PoseDenseIndex];
            if (poseState.Key != key)
            {
                snapshot = default;
                return false;
            }

            bool hasLifetime = lifetimeFeatures.TryGet(key, out NativeElementLifetimeState nativeLifetime);
            bool hasLocalClock = localClocks.TryGet(key, out ElementLocalClock localClock);
            lifetimeFeatures.TryGetDenseIndex(key, out int lifetimeDenseIndex);
            localClocks.TryGetDenseIndex(key, out int localClockDenseIndex);
            PhysicsBody physicsBody = default;
            bool hasPhysicsBody = physicsCoreLane != null &&
                physicsCoreLane.TryGetBody(key, out physicsBody);
            int physicsBodyHandleIndex = -1;
            ushort physicsBodyHandleGeneration = 0;
            int physicsShapeHandleIndex = -1;
            ushort physicsShapeHandleGeneration = 0;
            if (hasPhysicsBody)
            {
                PhysicsHandle bodyHandle = physicsBody.physicsHandle;
                physicsBodyHandleIndex = bodyHandle.index;
                physicsBodyHandleGeneration = bodyHandle.generation;
                using NativeArray<PhysicsShape> shapes = physicsBody.GetShapes(Allocator.Temp);
                if (shapes.Length > 0 && shapes[0].isValid)
                {
                    PhysicsHandle shapeHandle = shapes[0].physicsHandle;
                    physicsShapeHandleIndex = shapeHandle.index;
                    physicsShapeHandleGeneration = shapeHandle.generation;
                }
            }
            ElementSnapshot gameplaySnapshot = state.ToSnapshot(
                hasLifetime ? nativeLifetime.Feature.RemainingUnits : 0f);
            ElementPose2D pose = poseState.ToPose();
            var storage = new ElementDebugStorageMetadata(
                entry.Lane,
                entry.DenseIndex,
                entry.PoseDenseIndex,
                structuralRevision,
                hasLifetime,
                hasLifetime ? lifetimeDenseIndex : -1,
                hasLocalClock,
                hasLocalClock ? localClockDenseIndex : -1,
                hasPhysicsBody,
                physicsBodyHandleIndex,
                physicsBodyHandleGeneration,
                physicsShapeHandleIndex,
                physicsShapeHandleGeneration);
            ElementLifetimeFeature lifetime = hasLifetime ? nativeLifetime.Feature : default;
            snapshot = new ElementDebugSnapshot(
                in gameplaySnapshot,
                in pose,
                in storage,
                hasLifetime,
                in lifetime,
                hasLocalClock,
                in localClock);
            return true;
        }



        /// <summary>
        /// 지정 Lane의 dense native 저장소 상태를 reflection 없이 조회합니다.
        /// </summary>
        public bool TryGetLaneStorageDiagnostics(
            ElementLane lane,
            out ElementLaneStorageDiagnostics diagnostics)
        {
            if (disposed || lane == ElementLane.None)
            {
                diagnostics = default;
                return false;
            }

            NativeList<NativeElementState> states;
            switch (lane)
            {
                case ElementLane.QuerySprite2D:
                    states = kinematicStates;
                    break;
                case ElementLane.AreaSensorSprite2D:
                    states = areaStates;
                    break;
                case ElementLane.DynamicBodySprite2D:
                    states = physicsStates;
                    break;
                default:
                    diagnostics = default;
                    return false;
            }

            diagnostics = new ElementLaneStorageDiagnostics(
                lane,
                states.Length,
                states.Capacity,
                poseStates.Length,
                poseStates.Capacity,
                lane == ElementLane.DynamicBodySprite2D);
            return true;
        }



        /// <summary>
        /// World 공용 저장소와 선택형 sparse feature 저장소의 현재 사용량을 조회합니다.
        /// </summary>
        /// <param name="diagnostics">조회에 성공하면 현재 count와 capacity를 받습니다.</param>
        /// <returns>World가 유효하면 true를 반환합니다.</returns>
        public bool TryGetStorageDiagnostics(out ElementWorldStorageDiagnostics diagnostics)
        {
            if (disposed)
            {
                diagnostics = default;
                return false;
            }

            diagnostics = new ElementWorldStorageDiagnostics(
                registry.Length,
                registry.Capacity,
                poseStates.Length,
                poseStates.Capacity,
                lifetimeFeatures.Count,
                lifetimeFeatures.Capacity,
                localClocks.Count,
                localClocks.Capacity);
            return true;
        }
    }



    internal sealed class ElementDebugFactRingBuffer
    {
        private readonly ElementFact[] items;
        private int start;
        private int count;



        public ElementDebugFactRingBuffer(int capacity)
        {
            if (capacity < 1) { throw new ArgumentOutOfRangeException(nameof(capacity)); }
            items = new ElementFact[capacity];
        }



        public void Add(in ElementFact fact)
        {
            int index = (start + count) % items.Length;
            if (count == items.Length)
            {
                items[start] = fact;
                start = (start + 1) % items.Length;
                return;
            }

            items[index] = fact;
            count++;
        }

        public void CopyTo(List<ElementFact> destination)
        {
            for (int i = 0; i < count; i++)
            {
                destination.Add(items[(start + i) % items.Length]);
            }
        }

        public void Clear()
        {
            start = 0;
            count = 0;
        }
    }
}

#endif
