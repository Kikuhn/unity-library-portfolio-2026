using System;
using System.Collections.Generic;
using Pan.HighDensityElement;
using Unity.Collections;
using Unity.U2D.Physics;
using UnityEngine;



namespace Pan.HighDensityElement.Physics2DBridge
{
    /// <summary>
    /// Unity Physics2D와 전용 PhysicsCore2D world를 하나의 결과 목록으로 조회하는 main-thread facade입니다.
    /// </summary>
    public sealed class HybridPhysicsWorld2D
    {
        private enum LegacyOverlapKind : byte
        {
            Point,
            Circle,
            Box,
            Capsule
        }



        private readonly ElementWorld elementWorld;
        private readonly HashSet<int> colliderIds = new HashSet<int>();
        private readonly HashSet<ElementKey> elementKeys = new HashSet<ElementKey>();
        private RaycastHit2D[] legacyCastBuffer = new RaycastHit2D[32];
        private Collider2D[] legacyOverlapBuffer = new Collider2D[32];



        /// <summary>
        /// 같은 PhysicsCore2D lane을 공유하는 ElementWorld와 bridge registry를 묶습니다.
        /// </summary>
        public HybridPhysicsWorld2D(ElementWorld elementWorld, Physics2DBridgeRegistry bridgeRegistry)
        {
            this.elementWorld = elementWorld ?? throw new ArgumentNullException(nameof(elementWorld));
            if (bridgeRegistry == null) { throw new ArgumentNullException(nameof(bridgeRegistry)); }
            if (elementWorld.IsDisposed) { throw new ObjectDisposedException(nameof(elementWorld)); }
            if (bridgeRegistry.IsDisposed) { throw new ObjectDisposedException(nameof(bridgeRegistry)); }
            if (!ReferenceEquals(elementWorld.PhysicsCoreLane, bridgeRegistry.Lane))
            {
                throw new ArgumentException(
                    "HybridPhysicsWorld2D의 ElementWorld와 Physics2DBridgeRegistry는 같은 PhysicsCore2D lane을 사용해야 합니다.",
                    nameof(bridgeRegistry));
            }
        }



        /// <summary>
        /// 선택한 물리 공간에 ray를 투사하고 거리순 결과를 재사용 목록에 기록합니다.
        /// </summary>
        public int Raycast(
            Vector2 origin,
            Vector2 direction,
            float distance,
            in HybridPhysicsQueryFilter2D filter,
            List<HybridPhysicsHit2D> results)
        {
            float directionLength = direction.magnitude;
            if (directionLength <= float.Epsilon || distance <= 0f)
            {
                Prepare(results);
                return 0;
            }

            Vector2 translation = direction / directionLength * distance;
            Prepare(results);
            AppendLegacyRaycast(origin, translation.normalized, distance, in filter, results);
            if (IncludesCore(filter.Worlds))
            {
                PhysicsQuery.CastRayInput input = new PhysicsQuery.CastRayInput(origin, translation);
                using NativeArray<PhysicsQuery.WorldCastResult> hits = elementWorld.PhysicsCoreLane.World.CastRay(
                    input,
                    CreateCoreFilter(in filter),
                    PhysicsQuery.WorldCastMode.AllSorted,
                    Allocator.Temp);
                AppendCoreCastResults(hits, distance, in filter, results);
            }
            SortCastResults(results);
            return results.Count;
        }



        /// <summary>
        /// 시작점과 끝점 사이를 raycast하고 거리순 결과를 기록합니다.
        /// </summary>
        public int Linecast(
            Vector2 start,
            Vector2 end,
            in HybridPhysicsQueryFilter2D filter,
            List<HybridPhysicsHit2D> results)
        {
            Vector2 delta = end - start;
            return Raycast(start, delta, delta.magnitude, in filter, results);
        }



        /// <summary>
        /// 원형 geometry를 이동시켜 양쪽 물리 공간의 접촉 결과를 기록합니다.
        /// </summary>
        public int CircleCast(
            Vector2 origin,
            float radius,
            Vector2 direction,
            float distance,
            in HybridPhysicsQueryFilter2D filter,
            List<HybridPhysicsHit2D> results)
        {
            Prepare(results);
            AppendLegacyCircleCast(origin, radius, direction, distance, in filter, results);
            if (IncludesCore(filter.Worlds))
            {
                CircleGeometry geometry = new CircleGeometry
                {
                    center = origin,
                    radius = Mathf.Max(0.0001f, radius)
                };
                AppendCoreGeometryCast(geometry, direction, distance, in filter, results);
            }
            SortCastResults(results);
            return results.Count;
        }



        /// <summary>
        /// 회전 가능한 box geometry를 이동시켜 접촉 결과를 기록합니다.
        /// </summary>
        public int BoxCast(
            Vector2 origin,
            Vector2 size,
            float angle,
            Vector2 direction,
            float distance,
            in HybridPhysicsQueryFilter2D filter,
            List<HybridPhysicsHit2D> results)
        {
            Prepare(results);
            AppendLegacyBoxCast(origin, size, angle, direction, distance, in filter, results);
            if (IncludesCore(filter.Worlds))
            {
                PolygonGeometry geometry = PolygonGeometry.CreateBox(
                    size,
                    0f,
                    new PhysicsTransform(origin, PhysicsRotate.FromDegrees(angle)),
                    true);
                AppendCoreGeometryCast(geometry, direction, distance, in filter, results);
            }
            SortCastResults(results);
            return results.Count;
        }



        /// <summary>
        /// capsule geometry를 이동시켜 접촉 결과를 기록합니다.
        /// </summary>
        public int CapsuleCast(
            Vector2 origin,
            Vector2 size,
            CapsuleDirection2D capsuleDirection,
            float angle,
            Vector2 direction,
            float distance,
            in HybridPhysicsQueryFilter2D filter,
            List<HybridPhysicsHit2D> results)
        {
            Prepare(results);
            AppendLegacyCapsuleCast(origin, size, capsuleDirection, angle, direction, distance, in filter, results);
            if (IncludesCore(filter.Worlds))
            {
                CapsuleGeometry geometry = CreateCapsule(origin, size, capsuleDirection, angle);
                AppendCoreGeometryCast(geometry, direction, distance, in filter, results);
            }
            SortCastResults(results);
            return results.Count;
        }



        /// <summary>
        /// 한 점과 겹치는 대상을 안정적인 identity 순으로 기록합니다.
        /// </summary>
        public int OverlapPoint(
            Vector2 point,
            in HybridPhysicsQueryFilter2D filter,
            List<HybridPhysicsHit2D> results)
        {
            Prepare(results);
            AppendLegacyOverlapPoint(point, in filter, results);
            if (IncludesCore(filter.Worlds))
            {
                using NativeArray<PhysicsQuery.WorldOverlapResult> hits = elementWorld.PhysicsCoreLane.World.OverlapPoint(
                    point,
                    CreateCoreFilter(in filter),
                    Allocator.Temp);
                AppendCoreOverlapResults(hits, point, in filter, results);
            }
            SortOverlapResults(results);
            return results.Count;
        }



        /// <summary>
        /// 원과 겹치는 대상을 안정적인 identity 순으로 기록합니다.
        /// </summary>
        public int OverlapCircle(
            Vector2 point,
            float radius,
            in HybridPhysicsQueryFilter2D filter,
            List<HybridPhysicsHit2D> results)
        {
            Prepare(results);
            AppendLegacyOverlapCircle(point, radius, in filter, results);
            if (IncludesCore(filter.Worlds))
            {
                CircleGeometry geometry = new CircleGeometry
                {
                    center = point,
                    radius = Mathf.Max(0.0001f, radius)
                };
                using NativeArray<PhysicsQuery.WorldOverlapResult> hits = elementWorld.PhysicsCoreLane.World.OverlapGeometry(
                    geometry,
                    CreateCoreFilter(in filter),
                    Allocator.Temp);
                AppendCoreOverlapResults(hits, point, in filter, results);
            }
            SortOverlapResults(results);
            return results.Count;
        }



        /// <summary>
        /// 회전 가능한 box와 겹치는 대상을 안정적인 identity 순으로 기록합니다.
        /// </summary>
        public int OverlapBox(
            Vector2 point,
            Vector2 size,
            float angle,
            in HybridPhysicsQueryFilter2D filter,
            List<HybridPhysicsHit2D> results)
        {
            Prepare(results);
            AppendLegacyOverlapBox(point, size, angle, in filter, results);
            if (IncludesCore(filter.Worlds))
            {
                PolygonGeometry geometry = PolygonGeometry.CreateBox(
                    size,
                    0f,
                    new PhysicsTransform(point, PhysicsRotate.FromDegrees(angle)),
                    true);
                using NativeArray<PhysicsQuery.WorldOverlapResult> hits = elementWorld.PhysicsCoreLane.World.OverlapGeometry(
                    geometry,
                    CreateCoreFilter(in filter),
                    Allocator.Temp);
                AppendCoreOverlapResults(hits, point, in filter, results);
            }
            SortOverlapResults(results);
            return results.Count;
        }



        /// <summary>
        /// capsule과 겹치는 대상을 안정적인 identity 순으로 기록합니다.
        /// </summary>
        public int OverlapCapsule(
            Vector2 point,
            Vector2 size,
            CapsuleDirection2D capsuleDirection,
            float angle,
            in HybridPhysicsQueryFilter2D filter,
            List<HybridPhysicsHit2D> results)
        {
            Prepare(results);
            AppendLegacyOverlapCapsule(point, size, capsuleDirection, angle, in filter, results);
            if (IncludesCore(filter.Worlds))
            {
                CapsuleGeometry geometry = CreateCapsule(point, size, capsuleDirection, angle);
                using NativeArray<PhysicsQuery.WorldOverlapResult> hits = elementWorld.PhysicsCoreLane.World.OverlapGeometry(
                    geometry,
                    CreateCoreFilter(in filter),
                    Allocator.Temp);
                AppendCoreOverlapResults(hits, point, in filter, results);
            }
            SortOverlapResults(results);
            return results.Count;
        }



        private void AppendCoreGeometryCast(
            CircleGeometry geometry,
            Vector2 direction,
            float distance,
            in HybridPhysicsQueryFilter2D filter,
            List<HybridPhysicsHit2D> results)
        {
            Vector2 translation = NormalizeTranslation(direction, distance);
            if (translation.sqrMagnitude <= float.Epsilon) { return; }
            using NativeArray<PhysicsQuery.WorldCastResult> hits = elementWorld.PhysicsCoreLane.World.CastGeometry(
                geometry, translation, CreateCoreFilter(in filter), PhysicsQuery.WorldCastMode.AllSorted, Allocator.Temp);
            AppendCoreCastResults(hits, distance, in filter, results);
        }



        private void AppendCoreGeometryCast(
            CapsuleGeometry geometry,
            Vector2 direction,
            float distance,
            in HybridPhysicsQueryFilter2D filter,
            List<HybridPhysicsHit2D> results)
        {
            Vector2 translation = NormalizeTranslation(direction, distance);
            if (translation.sqrMagnitude <= float.Epsilon) { return; }
            using NativeArray<PhysicsQuery.WorldCastResult> hits = elementWorld.PhysicsCoreLane.World.CastGeometry(
                geometry, translation, CreateCoreFilter(in filter), PhysicsQuery.WorldCastMode.AllSorted, Allocator.Temp);
            AppendCoreCastResults(hits, distance, in filter, results);
        }



        private void AppendCoreGeometryCast(
            PolygonGeometry geometry,
            Vector2 direction,
            float distance,
            in HybridPhysicsQueryFilter2D filter,
            List<HybridPhysicsHit2D> results)
        {
            Vector2 translation = NormalizeTranslation(direction, distance);
            if (translation.sqrMagnitude <= float.Epsilon) { return; }
            using NativeArray<PhysicsQuery.WorldCastResult> hits = elementWorld.PhysicsCoreLane.World.CastGeometry(
                geometry, translation, CreateCoreFilter(in filter), PhysicsQuery.WorldCastMode.AllSorted, Allocator.Temp);
            AppendCoreCastResults(hits, distance, in filter, results);
        }



        private void AppendCoreCastResults(
            NativeArray<PhysicsQuery.WorldCastResult> hits,
            float distance,
            in HybridPhysicsQueryFilter2D filter,
            List<HybridPhysicsHit2D> results)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                PhysicsQuery.WorldCastResult hit = hits[i];
                if (!hit.isValid) { continue; }
                TryAppendCoreTarget(
                    hit.shape,
                    hit.point,
                    hit.normal,
                    distance * hit.fraction,
                    hit.fraction,
                    in filter,
                    results);
            }
        }



        private void AppendCoreOverlapResults(
            NativeArray<PhysicsQuery.WorldOverlapResult> hits,
            Vector2 point,
            in HybridPhysicsQueryFilter2D filter,
            List<HybridPhysicsHit2D> results)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                PhysicsQuery.WorldOverlapResult hit = hits[i];
                if (!hit.isValid) { continue; }
                TryAppendCoreTarget(hit.shape, point, Vector2.zero, 0f, 0f, in filter, results);
            }
        }



        private bool TryAppendCoreTarget(
            PhysicsShape shape,
            Vector2 point,
            Vector2 normal,
            float distance,
            float fraction,
            in HybridPhysicsQueryFilter2D filter,
            List<HybridPhysicsHit2D> results)
        {
            if (!PhysicsCore2DLane.TryResolveQueryTarget(
                    shape,
                    elementWorld.WorldId,
                    out ElementKey element,
                    out int bridgeTargetId))
            {
                return false;
            }

            if (bridgeTargetId > 0)
            {
                //? Projection Shape는 Legacy target을 Core World에 복제한 구현 세부사항입니다.
                //? LegacyOnly/Both에서는 원본 Physics2D 결과만, CoreOnly에서는 native Core Element만 노출합니다.
                return false;
            }

            if (!elementWorld.TryGetSnapshot(element, out ElementSnapshot snapshot) ||
                (snapshot.Capabilities & ElementCapabilities.QueryTarget2D) == 0 ||
                !elementKeys.Add(element))
            {
                return false;
            }

            int layer = ResolveLayer(shape.contactFilter.categories.bitMask);
            if (!PassesTargetFilter(layer, shape.isTrigger, in filter)) { return false; }
            results.Add(new HybridPhysicsHit2D(
                HybridPhysicsHitKind2D.CoreElement,
                null,
                null,
                element,
                point,
                normal,
                distance,
                fraction,
                layer,
                shape.isTrigger,
                element.GetHashCode()));
            return true;
        }



        private void AppendLegacyRaycast(
            Vector2 origin,
            Vector2 direction,
            float distance,
            in HybridPhysicsQueryFilter2D filter,
            List<HybridPhysicsHit2D> results)
        {
            if (!IncludesLegacy(filter.Worlds)) { return; }
            ContactFilter2D legacyFilter = CreateLegacyFilter(in filter);
            int count;
            do
            {
                count = Physics2D.Raycast(origin, direction, legacyFilter, legacyCastBuffer, distance);
                if (count < legacyCastBuffer.Length) { break; }
                Array.Resize(ref legacyCastBuffer, legacyCastBuffer.Length * 2);
            }
            while (true);
            AppendLegacyCastBuffer(count, distance, in filter, results);
        }



        private void AppendLegacyCircleCast(Vector2 origin, float radius, Vector2 direction, float distance,
            in HybridPhysicsQueryFilter2D filter, List<HybridPhysicsHit2D> results)
        {
            if (!IncludesLegacy(filter.Worlds)) { return; }
            ContactFilter2D legacyFilter = CreateLegacyFilter(in filter);
            int count;
            do
            {
                count = Physics2D.CircleCast(origin, radius, direction.normalized, legacyFilter, legacyCastBuffer, distance);
                if (count < legacyCastBuffer.Length) { break; }
                Array.Resize(ref legacyCastBuffer, legacyCastBuffer.Length * 2);
            }
            while (true);
            AppendLegacyCastBuffer(count, distance, in filter, results);
        }



        private void AppendLegacyBoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance,
            in HybridPhysicsQueryFilter2D filter, List<HybridPhysicsHit2D> results)
        {
            if (!IncludesLegacy(filter.Worlds)) { return; }
            ContactFilter2D legacyFilter = CreateLegacyFilter(in filter);
            int count;
            do
            {
                count = Physics2D.BoxCast(origin, size, angle, direction.normalized, legacyFilter, legacyCastBuffer, distance);
                if (count < legacyCastBuffer.Length) { break; }
                Array.Resize(ref legacyCastBuffer, legacyCastBuffer.Length * 2);
            }
            while (true);
            AppendLegacyCastBuffer(count, distance, in filter, results);
        }



        private void AppendLegacyCapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection,
            float angle, Vector2 direction, float distance, in HybridPhysicsQueryFilter2D filter,
            List<HybridPhysicsHit2D> results)
        {
            if (!IncludesLegacy(filter.Worlds)) { return; }
            ContactFilter2D legacyFilter = CreateLegacyFilter(in filter);
            int count;
            do
            {
                count = Physics2D.CapsuleCast(
                    origin, size, capsuleDirection, angle, direction.normalized,
                    legacyFilter, legacyCastBuffer, distance);
                if (count < legacyCastBuffer.Length) { break; }
                Array.Resize(ref legacyCastBuffer, legacyCastBuffer.Length * 2);
            }
            while (true);
            AppendLegacyCastBuffer(count, distance, in filter, results);
        }



        private void AppendLegacyCastBuffer(int count, float queryDistance,
            in HybridPhysicsQueryFilter2D filter, List<HybridPhysicsHit2D> results)
        {
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = legacyCastBuffer[i];
                Collider2D collider = hit.collider;
                if (collider == null || !PassesTargetFilter(collider.gameObject.layer, collider.isTrigger, in filter))
                {
                    continue;
                }
                int stableId = GetColliderId(collider);
                if (!colliderIds.Add(stableId)) { continue; }
                results.Add(new HybridPhysicsHit2D(
                    HybridPhysicsHitKind2D.LegacyCollider,
                    collider,
                    collider,
                    default,
                    hit.point,
                    hit.normal,
                    hit.distance,
                    queryDistance > 0f ? hit.distance / queryDistance : 0f,
                    collider.gameObject.layer,
                    collider.isTrigger,
                    stableId));
            }
        }



        private void AppendLegacyOverlapPoint(Vector2 point, in HybridPhysicsQueryFilter2D filter,
            List<HybridPhysicsHit2D> results) =>
            AppendLegacyOverlap(LegacyOverlapKind.Point, point, default, 0f,
                CapsuleDirection2D.Vertical, in filter, results);

        private void AppendLegacyOverlapCircle(Vector2 point, float radius, in HybridPhysicsQueryFilter2D filter,
            List<HybridPhysicsHit2D> results) =>
            AppendLegacyOverlap(LegacyOverlapKind.Circle, point, new Vector2(radius, 0f), 0f,
                CapsuleDirection2D.Vertical, in filter, results);

        private void AppendLegacyOverlapBox(Vector2 point, Vector2 size, float angle,
            in HybridPhysicsQueryFilter2D filter, List<HybridPhysicsHit2D> results) =>
            AppendLegacyOverlap(LegacyOverlapKind.Box, point, size, angle,
                CapsuleDirection2D.Vertical, in filter, results);

        private void AppendLegacyOverlapCapsule(Vector2 point, Vector2 size, CapsuleDirection2D capsuleDirection,
            float angle, in HybridPhysicsQueryFilter2D filter, List<HybridPhysicsHit2D> results) =>
            AppendLegacyOverlap(LegacyOverlapKind.Capsule, point, size, angle,
                capsuleDirection, in filter, results);



        private void AppendLegacyOverlap(
            LegacyOverlapKind kind,
            Vector2 point,
            Vector2 size,
            float angle,
            CapsuleDirection2D capsuleDirection,
            in HybridPhysicsQueryFilter2D filter,
            List<HybridPhysicsHit2D> results)
        {
            if (!IncludesLegacy(filter.Worlds)) { return; }
            ContactFilter2D legacyFilter = CreateLegacyFilter(in filter);
            int count;
            do
            {
                switch (kind)
                {
                    case LegacyOverlapKind.Point:
                        count = Physics2D.OverlapPoint(point, legacyFilter, legacyOverlapBuffer);
                        break;
                    case LegacyOverlapKind.Circle:
                        count = Physics2D.OverlapCircle(point, size.x, legacyFilter, legacyOverlapBuffer);
                        break;
                    case LegacyOverlapKind.Box:
                        count = Physics2D.OverlapBox(point, size, angle, legacyFilter, legacyOverlapBuffer);
                        break;
                    case LegacyOverlapKind.Capsule:
                        count = Physics2D.OverlapCapsule(
                            point, size, capsuleDirection, angle, legacyFilter, legacyOverlapBuffer);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(kind));
                }
                if (count < legacyOverlapBuffer.Length) { break; }
                Array.Resize(ref legacyOverlapBuffer, legacyOverlapBuffer.Length * 2);
            }
            while (true);

            for (int i = 0; i < count; i++)
            {
                Collider2D collider = legacyOverlapBuffer[i];
                if (collider == null || !PassesTargetFilter(collider.gameObject.layer, collider.isTrigger, in filter))
                {
                    continue;
                }
                int stableId = GetColliderId(collider);
                if (!colliderIds.Add(stableId)) { continue; }
                results.Add(new HybridPhysicsHit2D(
                    HybridPhysicsHitKind2D.LegacyCollider,
                    collider,
                    collider,
                    default,
                    point,
                    Vector2.zero,
                    0f,
                    0f,
                    collider.gameObject.layer,
                    collider.isTrigger,
                    stableId));
            }
        }



        private void Prepare(List<HybridPhysicsHit2D> results)
        {
            if (results == null) { throw new ArgumentNullException(nameof(results)); }
            results.Clear();
            colliderIds.Clear();
            elementKeys.Clear();
        }



        private static ContactFilter2D CreateLegacyFilter(in HybridPhysicsQueryFilter2D filter)
        {
            ContactFilter2D result = new ContactFilter2D();
            result.SetLayerMask(ResolveLayerMask(in filter));
            result.useTriggers = filter.IncludeTriggers;
            return result;
        }



        private static PhysicsQuery.QueryFilter CreateCoreFilter(in HybridPhysicsQueryFilter2D filter)
        {
            int layerMask = ResolveLayerMask(in filter);
            PhysicsQuery.QueryFilter result = PhysicsQuery.QueryFilter.defaultFilter;
            result.categories = new PhysicsMask
            {
                bitMask = filter.SourceLayer >= 0 && filter.SourceLayer < 32
                    ? 1ul << filter.SourceLayer
                    : uint.MaxValue
            };
            result.hitCategories = new PhysicsMask { bitMask = unchecked((uint)layerMask) };
            return result;
        }



        private static int ResolveLayerMask(in HybridPhysicsQueryFilter2D filter)
        {
            int mask = filter.LayerMask.value;
            if (!filter.RespectLayerCollisionMatrix || filter.SourceLayer < 0 || filter.SourceLayer >= 32)
            {
                return mask;
            }

            int matrixMask = 0;
            for (int layer = 0; layer < 32; layer++)
            {
                if (!Physics2D.GetIgnoreLayerCollision(filter.SourceLayer, layer)) { matrixMask |= 1 << layer; }
            }
            return mask & matrixMask;
        }



        private static bool PassesTargetFilter(int layer, bool isTrigger,
            in HybridPhysicsQueryFilter2D filter) =>
            layer >= 0 && layer < 32 &&
            (ResolveLayerMask(in filter) & (1 << layer)) != 0 &&
            (filter.IncludeTriggers || !isTrigger);



        private static Vector2 NormalizeTranslation(Vector2 direction, float distance)
        {
            float length = direction.magnitude;
            return length > float.Epsilon && distance > 0f
                ? direction / length * distance
                : Vector2.zero;
        }



        private static CapsuleGeometry CreateCapsule(
            Vector2 center,
            Vector2 size,
            CapsuleDirection2D direction,
            float angle)
        {
            bool vertical = direction == CapsuleDirection2D.Vertical;
            float radius = Mathf.Max(0.0001f, (vertical ? size.x : size.y) * 0.5f);
            float halfSegment = Mathf.Max(0f, (vertical ? size.y : size.x) * 0.5f - radius);
            Vector2 axis = vertical ? Vector2.up : Vector2.right;
            float radians = angle * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            axis = new Vector2(axis.x * cos - axis.y * sin, axis.x * sin + axis.y * cos);
            return CapsuleGeometry.Create(center - axis * halfSegment, center + axis * halfSegment, radius);
        }



        private static int ResolveLayer(ulong categories)
        {
            for (int layer = 0; layer < 32; layer++)
            {
                if ((categories & (1ul << layer)) != 0ul) { return layer; }
            }
            return 0;
        }



        private static int GetColliderId(Collider2D collider) => collider.GetEntityId().GetHashCode();
        private static bool IncludesLegacy(HybridPhysicsQueryWorldMask mask) =>
            (mask & HybridPhysicsQueryWorldMask.Legacy) != 0;
        private static bool IncludesCore(HybridPhysicsQueryWorldMask mask) =>
            (mask & HybridPhysicsQueryWorldMask.Core) != 0;



        private static void SortCastResults(List<HybridPhysicsHit2D> results) =>
            results.Sort((left, right) =>
            {
                int distance = left.Distance.CompareTo(right.Distance);
                return distance != 0 ? distance : left.StableTargetId.CompareTo(right.StableTargetId);
            });



        private static void SortOverlapResults(List<HybridPhysicsHit2D> results) =>
            results.Sort((left, right) => left.StableTargetId.CompareTo(right.StableTargetId));
    }
}
