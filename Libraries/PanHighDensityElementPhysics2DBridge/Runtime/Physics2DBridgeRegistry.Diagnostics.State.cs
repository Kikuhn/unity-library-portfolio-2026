using System.Collections.Generic;
using Unity.U2D.Physics;
using UnityEngine;



namespace Pan.HighDensityElement.Physics2DBridge
{
    public sealed partial class Physics2DBridgeRegistry
    {
        ///======================================================================================================================================================
        //? Registry 집계와 진단 draw lease 수명
        ///======================================================================================================================================================



        /// <summary>
        /// Editor와 gameplay debugger가 브리지 집계를 내부 컬렉션 노출 없이 읽도록 합니다.
        /// </summary>
        public Physics2DBridgeRegistryDiagnostics GetDiagnostics()
        {
            return new Physics2DBridgeRegistryDiagnostics(
                TargetCount,
                BodyCount,
                ShapeCount,
                StrictMotionCount,
                synchronizationSequence);
        }



#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void AcquireDebugDrawLease()
        {
#if !UNITY_EDITOR
            if (!settings.EnableDevelopmentDebugDraw) { return; }
#endif
            if (!DebugDrawLeases.TryGetValue(lane, out DebugDrawLease lease))
            {
                PhysicsWorld world = lane.World;
                lease = new DebugDrawLease
                {
                    PreviousOptions = world.drawOptions,
                    ReferenceCount = 0
                };
                DebugDrawLeases.Add(lane, lease);
                world.drawOptions = PhysicsWorld.DrawOptions.AllCustom;
            }

            lease.ReferenceCount++;
            debugDrawLeaseAcquired = true;
        }



        private void ReleaseDebugDrawLease()
        {
            if (!debugDrawLeaseAcquired || !DebugDrawLeases.TryGetValue(lane, out DebugDrawLease lease))
            {
                return;
            }

            debugDrawLeaseAcquired = false;
            lease.ReferenceCount--;
            if (lease.ReferenceCount > 0) { return; }

            PhysicsWorld world = lane.World;
            if (world.isValid) { world.drawOptions = lease.PreviousOptions; }
            DebugDrawLeases.Remove(lane);
        }
#endif



    }
}
