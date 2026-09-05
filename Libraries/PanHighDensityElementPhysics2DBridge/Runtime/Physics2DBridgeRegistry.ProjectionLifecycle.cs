using System;
using System.Collections.Generic;
using Unity.U2D.Physics;
using UnityEngine;



namespace Pan.HighDensityElement.Physics2DBridge
{
    public sealed partial class Physics2DBridgeRegistry
    {
        ///======================================================================================================================================================
        //? 상대 운동 CCD projection 후보의 수명과 제출
        ///======================================================================================================================================================



        /// <summary>
        /// 빠른 상대 운동 검사를 위해 지정한 bridge를 일정 고정 스텝 동안 엄격 CCD 후보로 유지합니다.
        /// </summary>
        public void RequestStrictCcd(HighDensityPhysicsBridge2D component, int fixedStepCount = 1)
        {
            if (disposed || component == null || fixedStepCount <= 0 ||
                !componentBodies.TryGetValue(component, out ComponentRecord record))
            {
                return;
            }

            for (int i = 0; i < record.Bodies.Count; i++)
            {
                record.Bodies[i].StrictCcdRemainingSteps = Mathf.Max(
                    record.Bodies[i].StrictCcdRemainingSteps,
                    fixedStepCount);
            }
        }



        /// <summary>
        /// 다음 동기화에서 이전 pose와 현재 pose를 같게 만들어 중간 경로 충돌을 생략합니다.
        /// </summary>
        public void MarkTeleported(HighDensityPhysicsBridge2D component)
        {
            if (disposed || component == null || !componentBodies.TryGetValue(component, out ComponentRecord record))
            {
                return;
            }

            for (int i = 0; i < record.Bodies.Count; i++) { record.Bodies[i].TeleportPending = true; }
        }



        /// <summary>
        /// 이번 동기화에서 엄격 CCD 후보가 된 Projection Body를 호출자 재사용 목록에 복사합니다.
        /// </summary>
        public int CopyStrictMotions(List<Physics2DBridgeStrictMotion2D> destination)
        {
            if (destination == null) { throw new ArgumentNullException(nameof(destination)); }
            destination.Clear();
            if (disposed) { return 0; }

            foreach (ComponentRecord registration in componentBodies.Values)
            {
                for (int bodyIndex = 0; bodyIndex < registration.Bodies.Count; bodyIndex++)
                {
                    BodyRecord record = registration.Bodies[bodyIndex];
                    if (record == null || !record.Body.isValid || !record.StrictCcdRequestedThisSync) { continue; }

                    destination.Add(new Physics2DBridgeStrictMotion2D(
                        registration.Handle,
                        bodyIndex,
                        record.Body,
                        record.PreviousLegacyPosition,
                        record.PreviousLegacyRotationDegrees,
                        record.LegacyPosition,
                        record.LegacyRotationDegrees,
                        record.TeleportedThisSync,
                        true));
                }
            }
            return destination.Count;
        }



        private void RefreshAggregateCounts()
        {
            int shapeCount = 0;
            for (int i = 0; i < bodies.Count; i++)
            {
                BodyRecord record = bodies[i];
                if (record != null) { shapeCount += record.ShapeCount; }
            }
            ShapeCount = shapeCount;
        }



        private void SubmitMovingProjections(long currentSynchronizationSequence)
        {
            movingProjectionCount = 0;
            foreach (ComponentRecord registration in componentBodies.Values)
            {
                for (int bodyIndex = 0; bodyIndex < registration.Bodies.Count; bodyIndex++)
                {
                    BodyRecord record = registration.Bodies[bodyIndex];
                    if (record == null || !record.Body.isValid || !record.StrictCcdRequestedThisSync ||
                        record.TeleportedThisSync)
                    {
                        continue;
                    }

                    PhysicsTransform previousTransform = new PhysicsTransform(
                        record.PreviousLegacyPosition,
                        PhysicsRotate.FromDegrees(record.PreviousLegacyRotationDegrees));
                    PhysicsTransform currentTransform = new PhysicsTransform(
                        record.LegacyPosition,
                        PhysicsRotate.FromDegrees(record.LegacyRotationDegrees));
                    int shapeCount = Mathf.Min(record.ShapeProxies.Count, record.ShapeTargetIds.Count);
                    EnsureMovingProjectionCapacity(movingProjectionCount + shapeCount);
                    for (int shapeIndex = 0; shapeIndex < shapeCount; shapeIndex++)
                    {
                        int targetId = record.ShapeTargetIds[shapeIndex];
                        movingProjectionScratch[movingProjectionCount++] = new MovingProjectionShape2D
                        {
                            Shape = record.ShapeProxies[shapeIndex],
                            PreviousTransform = previousTransform,
                            CurrentTransform = currentTransform,
                            BridgeTargetId = targetId,
                            StableShapeId = unchecked(targetId * 397 + shapeIndex),
                            CategoryMask = record.ShapeCategoryMasks[shapeIndex],
                            ContactMask = record.ShapeContactMasks[shapeIndex],
                            IsTrigger = record.ShapeTriggers[shapeIndex],
                            Teleported = 0
                        };
                    }
                }
            }

            lane.ReplaceMovingProjections(
                new ReadOnlySpan<MovingProjectionShape2D>(
                    movingProjectionScratch,
                    0,
                    movingProjectionCount),
                currentSynchronizationSequence);
        }



        private void EnsureMovingProjectionCapacity(int requiredCapacity)
        {
            if (requiredCapacity <= movingProjectionScratch.Length) { return; }
            int newCapacity = Mathf.NextPowerOfTwo(requiredCapacity);
            Array.Resize(ref movingProjectionScratch, newCapacity);
        }



    }
}
