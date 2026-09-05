using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using Unity.U2D.Physics;
using UnityEngine;



namespace Pan.HighDensityElement
{
    public sealed partial class PhysicsCore2DLane
    {
        private const ulong BridgeTargetTag = 1ul << 63;
        private const ulong TargetIdMask = uint.MaxValue;



        public static void TagBridgeTarget(PhysicsBody body, int bridgeTargetId)
        {
            if (!body.isValid) { throw new ArgumentException("유효하지 않은 Physics Core body입니다.", nameof(body)); }
            body.userData = new PhysicsUserData { int64Value = PackBridgeTargetId(bridgeTargetId) };
        }



        public static void TagBridgeTarget(PhysicsShape shape, int bridgeTargetId)
        {
            if (!shape.isValid) { throw new ArgumentException("유효하지 않은 Physics Core shape입니다.", nameof(shape)); }
            shape.userData = new PhysicsUserData { int64Value = PackBridgeTargetId(bridgeTargetId) };
        }



        internal static ulong GetCounterpartUserData(PhysicsShape shape)
        {
            if (!shape.isValid) { return 0ul; }
            ulong shapeData = shape.userData.int64Value;
            return shapeData != 0ul ? shapeData : shape.body.userData.int64Value;
        }



        internal static bool TryResolveCounterpart(
            PhysicsShape shape,
            int elementWorldId,
            out ElementKey element,
            out int bridgeTargetId)
        {
            return TryResolvePackedCounterpart(
                GetCounterpartUserData(shape),
                elementWorldId,
                out element,
                out bridgeTargetId);
        }



        private static bool TryResolvePackedCounterpart(
            ulong packed,
            int elementWorldId,
            out ElementKey element,
            out int bridgeTargetId)
        {
            if ((packed & BridgeTargetTag) != 0ul)
            {
                element = default;
                bridgeTargetId = unchecked((int)(packed & TargetIdMask));
                return bridgeTargetId > 0;
            }

            int slot = unchecked((int)(packed & uint.MaxValue));
            uint generation = unchecked((uint)(packed >> 32));
            element = new ElementKey(elementWorldId, slot, generation);
            bridgeTargetId = -1;
            return element.IsValid;
        }



        /// <summary>
        /// PhysicsCore query가 반환한 shape를 Element 또는 외부 bridge target으로 해석합니다.
        /// </summary>
        public static bool TryResolveQueryTarget(
            PhysicsShape shape,
            int elementWorldId,
            out ElementKey element,
            out int externalTargetId)
        {
            return TryResolveCounterpart(shape, elementWorldId, out element, out externalTargetId);
        }



        private static ulong PackBridgeTargetId(int bridgeTargetId)
        {
            if (bridgeTargetId <= 0) { throw new ArgumentOutOfRangeException(nameof(bridgeTargetId)); }
            return BridgeTargetTag | unchecked((uint)bridgeTargetId);
        }



        private static ulong PackKey(ElementKey key) =>
            ((ulong)key.Generation << 32) | unchecked((uint)key.Slot);
    }
}
