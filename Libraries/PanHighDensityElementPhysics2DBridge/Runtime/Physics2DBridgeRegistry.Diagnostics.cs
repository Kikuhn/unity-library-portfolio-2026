using System.Collections.Generic;
using Unity.U2D.Physics;
using UnityEngine;



namespace Pan.HighDensityElement.Physics2DBridge
{
    public sealed partial class Physics2DBridgeRegistry
    {
        ///======================================================================================================================================================
        //? Editor와 Development Build에서만 활성화되는 브리지 진단·그리기
        ///======================================================================================================================================================



        /// <summary>
        /// 등록된 body의 Legacy, Core와 표시 보간 pose를 Editor/Development 진단 정보로 반환합니다.
        /// </summary>
        public bool TryGetDiagnostics(
            Physics2DBridgeRegistrationHandle handle,
            int bodyIndex,
            float interpolationAlpha,
            out Physics2DBridgePoseDiagnostics diagnostics)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!disposed && IsRegistered(handle) &&
                registrationsBySlot.TryGetValue(handle.Slot, out ComponentRecord registration) &&
                bodyIndex >= 0 && bodyIndex < registration.Bodies.Count)
            {
                BodyRecord record = registration.Bodies[bodyIndex];
                if (record != null && record.Body.isValid)
                {
                    float alpha = Mathf.Clamp01(interpolationAlpha);
                    Vector2 interpolatedPosition = Vector2.LerpUnclamped(
                        record.PreviousLegacyPosition,
                        record.LegacyPosition,
                        alpha);
                    float interpolatedRotation = Mathf.LerpAngle(
                        record.PreviousLegacyRotationDegrees,
                        record.LegacyRotationDegrees,
                        alpha);
                    ResolvePresentationPose(
                        registration.Owner,
                        record,
                        interpolatedPosition,
                        interpolatedRotation,
                        out Vector2 presentationPosition,
                        out float presentationRotation);
                    diagnostics = new Physics2DBridgePoseDiagnostics(
                        handle,
                        bodyIndex,
                        registration.Owner != null
                            ? registration.Owner.ShapeSourceMode
                            : Physics2DBridgeShapeSourceMode.CollidersOnly,
                        record.LegacyPosition,
                        record.LegacyRotationDegrees,
                        record.Body.position,
                        record.Body.rotation.degrees,
                        interpolatedPosition,
                        interpolatedRotation,
                        presentationPosition,
                        presentationRotation,
                        record.TargetIds.Count,
                        record.ShapeProxies.Count,
                        record.Body.enabled,
                        record.LastSynchronizationSequence);
                    return true;
                }
            }
#endif
            diagnostics = default;
            return false;
        }



        /// <summary>
        /// Core Shape 생성 직후 캐시한 body-local 형상의 AABB를 반환합니다.
        /// Collider2D를 다시 추출하지 않으므로 표시와 실제 충돌 형상이 같은 자료를 사용합니다.
        /// </summary>
        public bool TryGetDebugShapeLocalBounds(
            Physics2DBridgeRegistrationHandle handle,
            int bodyIndex,
            int shapeIndex,
            out Bounds bounds)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!disposed && IsRegistered(handle) &&
                registrationsBySlot.TryGetValue(handle.Slot, out ComponentRecord registration) &&
                bodyIndex >= 0 && bodyIndex < registration.Bodies.Count)
            {
                BodyRecord record = registration.Bodies[bodyIndex];
                if (record != null && shapeIndex >= 0 && shapeIndex < record.ShapeProxies.Count)
                {
                    PhysicsAABB aabb = record.ShapeProxies[shapeIndex].aabb;
                    Vector2 center = aabb.center;
                    Vector2 size = aabb.extents * 2f;
                    bounds = new Bounds(
                        new Vector3(center.x, center.y, 0f),
                        new Vector3(size.x, size.y, 0f));
                    return true;
                }
            }
#endif
            bounds = default;
            return false;
        }



        /// <summary>
        /// 캐시된 실제 Core ShapeProxy를 선택한 물리 또는 표시 pose에 제출합니다.
        /// </summary>
        /// <returns>이번 호출에서 제출한 geometry 수입니다.</returns>
        public int DrawDiagnostics(
            Physics2DBridgeRegistrationHandle handle,
            int bodyIndex,
            in Physics2DBridgePoseDiagnostics diagnostics,
            Physics2DBridgeDebugDrawMode mode)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (mode == Physics2DBridgeDebugDrawMode.Off || disposed || !debugDrawLeaseAcquired ||
                !IsRegistered(handle) ||
                !registrationsBySlot.TryGetValue(handle.Slot, out ComponentRecord registration) ||
                bodyIndex < 0 || bodyIndex >= registration.Bodies.Count)
            {
                return 0;
            }

            BodyRecord record = registration.Bodies[bodyIndex];
            if (record == null || record.ShapeProxies.Count == 0) { return 0; }

            PhysicsWorld world = lane.World;
            int drawCount = 0;
            if (mode == Physics2DBridgeDebugDrawMode.Presentation ||
                mode == Physics2DBridgeDebugDrawMode.Both)
            {
                PhysicsTransform presentationTransform = new PhysicsTransform(
                    diagnostics.PresentationPosition,
                    PhysicsRotate.FromDegrees(diagnostics.PresentationRotationDegrees));
                drawCount += DrawCachedShapes(
                    world,
                    record.ShapeProxies,
                    presentationTransform,
                    new Color(0.1f, 0.55f, 1f, 0.95f));
            }

            if (mode == Physics2DBridgeDebugDrawMode.RawPhysics ||
                mode == Physics2DBridgeDebugDrawMode.Both)
            {
                PhysicsTransform rawTransform = new PhysicsTransform(
                    diagnostics.CorePosition,
                    PhysicsRotate.FromDegrees(diagnostics.CoreRotationDegrees));
                drawCount += DrawCachedShapes(
                    world,
                    record.ShapeProxies,
                    rawTransform,
                    new Color(0f, 1f, 0.9f, 0.45f));
            }

            if (mode == Physics2DBridgeDebugDrawMode.Both)
            {
                world.DrawCircle(
                    diagnostics.PresentationPosition,
                    0.05f,
                    new Color(1f, 0.85f, 0.1f, 0.95f),
                    0f,
                    PhysicsWorld.DrawFillOptions.Outline);
            }
            return drawCount;
#else
            return 0;
#endif
        }



#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static void ResolvePresentationPose(
            HighDensityPhysicsBridge2D owner,
            BodyRecord record,
            Vector2 interpolatedPosition,
            float interpolatedRotation,
            out Vector2 presentationPosition,
            out float presentationRotation)
        {
            if (owner != null && owner.TryGetConfiguredPresentationPose(
                    out presentationPosition,
                    out presentationRotation))
            {
                return;
            }

            if (record.Rigidbody != null)
            {
                Transform source = record.Rigidbody.transform;
                presentationPosition = source.position;
                presentationRotation = source.eulerAngles.z;
                return;
            }

            if (owner != null)
            {
                presentationPosition = owner.transform.position;
                presentationRotation = owner.transform.eulerAngles.z;
                return;
            }

            presentationPosition = interpolatedPosition;
            presentationRotation = interpolatedRotation;
        }



        private static int DrawCachedShapes(
            PhysicsWorld world,
            List<PhysicsShape.ShapeProxy> shapeProxies,
            PhysicsTransform pose,
            Color color)
        {
            for (int i = 0; i < shapeProxies.Count; i++)
            {
                world.DrawShapeProxy(
                    shapeProxies[i],
                    pose,
                    color,
                    0f,
                    PhysicsWorld.DrawFillOptions.Outline);
            }
            return shapeProxies.Count;
        }
#endif



        /// <summary>
        /// projectile layer mask 전체가 자동 mirror 설정으로 지원되는지 확인합니다.
        /// </summary>
        public bool ValidateHitLayers(LayerMask hitLayers, out LayerMask missingLayers)
        {
            int missing = hitLayers.value & ~settings.AutomaticMirrorLayers.value;
            missingLayers = missing;
            return missing == 0;
        }
    }
}
