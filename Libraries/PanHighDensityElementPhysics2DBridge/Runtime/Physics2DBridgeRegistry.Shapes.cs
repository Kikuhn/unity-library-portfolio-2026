using System;
using System.Collections.Generic;
using Unity.U2D.Physics;
using UnityEngine;
using UnityEngine.Tilemaps;



namespace Pan.HighDensityElement.Physics2DBridge
{
    public sealed partial class Physics2DBridgeRegistry
    {
        ///======================================================================================================================================================
        //? Legacy/Provider 형상을 PhysicsCore body와 shape로 투영하는 변환 경계
        ///======================================================================================================================================================



        private BodyRecord RegisterGroup(
            HighDensityPhysicsBridge2D owner,
            Rigidbody2D rigidbody,
            IList<Collider2D> colliders)
        {
            var record = new BodyRecord
            {
                Owner = owner,
                Rigidbody = rigidbody
            };
            for (int i = 0; i < colliders.Count; i++)
            {
                Collider2D collider = colliders[i];
                if (collider == null || targetIdsByCollider.ContainsKey(collider) || IsCompositedSource(collider))
                {
                    continue;
                }
                int targetId = NextTargetId();
                record.Colliders.Add(collider);
                record.ColliderProjectionSignatures.Add(CalculateColliderProjectionSignature(collider));
                record.TargetIds.Add(targetId);
                targets.Add(targetId, new TargetRecord(
                    collider,
                    record,
                    collider,
                    collider.gameObject.layer,
                    collider.isTrigger));
                targetIdsByCollider.Add(collider, targetId);
            }

            if (record.Colliders.Count == 0) { return null; }
            if (!CreateBodyAndShapes(record))
            {
                RemoveTargetMappings(record);
                return null;
            }
            RefreshFactReceiverCache(record);
            bodies.Add(record);
            RefreshAggregateCounts();
            return record;
        }



        private bool CreateBodyAndShapes(BodyRecord record)
        {
            record.ShapeCount = 0;
            record.MinimumShapeExtent = float.PositiveInfinity;
            record.MaximumShapeRadius = 0f;
            record.ShapeProxies.Clear();
            record.ShapeTargetIds.Clear();
            record.ShapeCategoryMasks.Clear();
            record.ShapeContactMasks.Clear();
            record.ShapeTriggers.Clear();
            PhysicsBodyDefinition definition = PhysicsBodyDefinition.defaultDefinition;
            Rigidbody2D rigidbody = record.Rigidbody;
            Physics2DBridgeBodyState providerState = default;
            if (record.Provider != null && !record.Provider.TryGetBodyState(out providerState))
            {
                return false;
            }

            if (record.Provider != null)
            {
                definition.type = providerState.IsStatic
                    ? PhysicsBody.BodyType.Static
                    : PhysicsBody.BodyType.Kinematic;
                definition.position = providerState.Position;
                definition.rotation = PhysicsRotate.FromDegrees(providerState.RotationDegrees);
                definition.linearVelocity = Vector2.zero;
                definition.angularVelocity = 0f;
                definition.enabled = providerState.Enabled;
            }
            else
            {
                definition.type = rigidbody == null || rigidbody.bodyType == RigidbodyType2D.Static
                    ? PhysicsBody.BodyType.Static
                    : PhysicsBody.BodyType.Kinematic;
            }
            definition.transformWriteMode = PhysicsBody.TransformWriteMode.Off;
            if (rigidbody != null)
            {
                definition.position = rigidbody.position;
                definition.rotation = PhysicsRotate.FromDegrees(rigidbody.rotation);
                definition.linearVelocity = Vector2.zero;
                definition.angularVelocity = 0f;
                definition.enabled = rigidbody.simulated && rigidbody.gameObject.activeInHierarchy;
            }

            PhysicsBody body = lane.World.CreateBody(definition);
            int createdShapeCount = 0;
            try
            {
                if (record.Provider != null)
                {
                    createdShapeCount = CreateProviderShapes(body, record);
                }
                else for (int i = 0; i < record.Colliders.Count; i++)
                {
                    createdShapeCount += CreateColliderShapes(
                        body,
                        record.Colliders[i],
                        record.TargetIds[i],
                        record);
                }
                if (createdShapeCount == 0)
                {
                    body.Destroy();
                    return false;
                }

                PhysicsCore2DLane.TagBridgeTarget(body, record.TargetIds[0]);
                record.Body = body;
                Vector2 initialPosition = record.Provider != null
                    ? providerState.Position
                    : rigidbody != null ? rigidbody.position : Vector2.zero;
                float initialRotation = record.Provider != null
                    ? providerState.RotationDegrees
                    : rigidbody != null ? rigidbody.rotation : 0f;
                if (!record.HasMotionPose)
                {
                    record.PreviousLegacyPosition = initialPosition;
                    record.LegacyPosition = initialPosition;
                    record.PreviousLegacyRotationDegrees = initialRotation;
                    record.LegacyRotationDegrees = initialRotation;
                    record.HasMotionPose = true;
                }
                return true;
            }
            catch
            {
                if (body.isValid) { body.Destroy(); }
                throw;
            }
        }



        private int CreateProviderShapes(PhysicsBody body, BodyRecord record)
        {
            providerShapeScratch.Clear();
            vertexScratch.Clear();
            int declaredCount = record.Provider.CopyShapes(providerShapeScratch, vertexScratch);
            int descriptorCount = Mathf.Min(declaredCount, providerShapeScratch.Count);
            if (descriptorCount <= 0) { return 0; }

            int targetId = record.TargetIds[0];
            record.ProviderLayer = providerShapeScratch[0].Layer;
            record.ProviderIsTrigger = providerShapeScratch[0].IsTrigger;
            int createdCount = 0;
            for (int i = 0; i < descriptorCount; i++)
            {
                Physics2DBridgeShapeDescriptor descriptor = providerShapeScratch[i];
                PhysicsShapeDefinition definition = CreateShapeDefinition(in descriptor);
                switch (descriptor.Type)
                {
                    case Physics2DBridgeShapeType.Circle:
                        Tag(body.CreateShape(new CircleGeometry
                        {
                            center = descriptor.PointA,
                            radius = Mathf.Max(0.0001f, descriptor.Radius)
                        }, definition), targetId, record);
                        createdCount++;
                        break;

                    case Physics2DBridgeShapeType.Box:
                        Tag(body.CreateShape(PolygonGeometry.CreateBox(
                            new Vector2(
                                Mathf.Max(0.0001f, descriptor.Size.x),
                                Mathf.Max(0.0001f, descriptor.Size.y)),
                            Mathf.Max(0f, descriptor.Radius),
                            new PhysicsTransform(
                                descriptor.PointA,
                                PhysicsRotate.FromDegrees(descriptor.RotationDegrees)),
                            true), definition), targetId, record);
                        createdCount++;
                        break;

                    case Physics2DBridgeShapeType.Capsule:
                        Tag(body.CreateShape(CapsuleGeometry.Create(
                            descriptor.PointA,
                            descriptor.PointB,
                            Mathf.Max(0.0001f, descriptor.Radius)), definition), targetId, record);
                        createdCount++;
                        break;

                    case Physics2DBridgeShapeType.Polygon:
                        Vector2[] polygonVertices = CopyProviderVertices(in descriptor, 3);
                        Tag(body.CreateShape(PolygonGeometry.Create(
                            polygonVertices,
                            Mathf.Max(0f, descriptor.Radius)), definition), targetId, record);
                        createdCount++;
                        break;

                    case Physics2DBridgeShapeType.Segment:
                        PhysicsShape segmentShape = descriptor.Radius > 0f
                            ? body.CreateShape(CapsuleGeometry.Create(
                                descriptor.PointA,
                                descriptor.PointB,
                                descriptor.Radius), definition)
                            : body.CreateShape(SegmentGeometry.Create(
                                descriptor.PointA,
                                descriptor.PointB), definition);
                        Tag(segmentShape, targetId, record);
                        createdCount++;
                        break;

                    case Physics2DBridgeShapeType.Chain:
                        Vector2[] chainVertices = CopyProviderVertices(in descriptor, 2);
                        for (int vertexIndex = 0; vertexIndex < chainVertices.Length - 1; vertexIndex++)
                        {
                            PhysicsShape shape = descriptor.Radius > 0f
                                ? body.CreateShape(CapsuleGeometry.Create(
                                    chainVertices[vertexIndex],
                                    chainVertices[vertexIndex + 1],
                                    descriptor.Radius), definition)
                                : body.CreateShape(SegmentGeometry.Create(
                                    chainVertices[vertexIndex],
                                    chainVertices[vertexIndex + 1]), definition);
                            Tag(shape, targetId, record);
                            createdCount++;
                        }
                        break;

                    default:
                        throw new NotSupportedException($"Unsupported provider shape type: {descriptor.Type}");
                }
            }
            return createdCount;
        }



        private Vector2[] CopyProviderVertices(
            in Physics2DBridgeShapeDescriptor descriptor,
            int minimumCount)
        {
            if (descriptor.VertexStart < 0 || descriptor.VertexCount < minimumCount ||
                descriptor.VertexStart + descriptor.VertexCount > vertexScratch.Count)
            {
                throw new InvalidOperationException(
                    $"Invalid provider vertex range: start={descriptor.VertexStart}, count={descriptor.VertexCount}.");
            }

            var vertices = new Vector2[descriptor.VertexCount];
            vertexScratch.CopyTo(descriptor.VertexStart, vertices, 0, descriptor.VertexCount);
            return vertices;
        }



        private int CreateColliderShapes(
            PhysicsBody body,
            Collider2D collider,
            int targetId,
            BodyRecord record)
        {
            if (collider == null || !collider.isActiveAndEnabled) { return 0; }
            PhysicsShapeGroup2D group = colliderShapeGroupScratch;
            if (collider.GetShapes(group) <= 0) { return 0; }

            PhysicsShapeDefinition definition = CreateShapeDefinition(collider);
            int count = 0;
            for (int shapeIndex = 0; shapeIndex < group.shapeCount; shapeIndex++)
            {
                PhysicsShape2D sourceShape = group.GetShape(shapeIndex);
                vertexScratch.Clear();
                group.GetShapeVertices(shapeIndex, vertexScratch);
                try
                {
                    count += CreateShape(body, sourceShape, vertexScratch, definition, targetId, record);
                }
                catch (Exception exception)
                {
                    ReportConversionErrorOnce(collider, sourceShape.shapeType, exception.Message);
                }
            }
            return count;
        }



        private static int CreateShape(
            PhysicsBody body,
            PhysicsShape2D source,
            List<Vector2> vertices,
            PhysicsShapeDefinition definition,
            int targetId,
            BodyRecord record)
        {
            switch (source.shapeType)
            {
                case PhysicsShapeType2D.Circle:
                    RequireVertexCount(vertices, 1, source.shapeType);
                    Tag(body.CreateShape(new CircleGeometry
                    {
                        center = vertices[0],
                        radius = Mathf.Max(0.0001f, source.radius)
                    }, definition), targetId, record);
                    return 1;

                case PhysicsShapeType2D.Capsule:
                    RequireVertexCount(vertices, 2, source.shapeType);
                    Tag(body.CreateShape(
                        CapsuleGeometry.Create(vertices[0], vertices[1], Mathf.Max(0.0001f, source.radius)),
                        definition), targetId, record);
                    return 1;

                case PhysicsShapeType2D.Polygon:
                    if (vertices.Count < 3)
                    {
                        throw new InvalidOperationException("Polygon은 최소 3개 vertex가 필요합니다.");
                    }
                    Tag(body.CreateShape(
                        PolygonGeometry.Create(vertices.ToArray(), Mathf.Max(0f, source.radius)),
                        definition), targetId, record);
                    return 1;

                case PhysicsShapeType2D.Edges:
                    if (vertices.Count < 2)
                    {
                        throw new InvalidOperationException("Edges는 최소 2개 vertex가 필요합니다.");
                    }
                    int count = 0;
                    for (int i = 0; i < vertices.Count - 1; i++)
                    {
                        PhysicsShape shape = source.radius > 0f
                            ? body.CreateShape(CapsuleGeometry.Create(vertices[i], vertices[i + 1], source.radius), definition)
                            : body.CreateShape(SegmentGeometry.Create(vertices[i], vertices[i + 1]), definition);
                        Tag(shape, targetId, record);
                        count++;
                    }
                    return count;

                default:
                    throw new NotSupportedException($"지원하지 않는 PhysicsShapeType2D입니다: {source.shapeType}");
            }
        }



        private static PhysicsShapeDefinition CreateShapeDefinition(Collider2D collider)
        {
            PhysicsShapeDefinition definition = PhysicsShapeDefinition.defaultDefinition;
            definition.contactEvents = true;
            definition.triggerEvents = true;
            definition.isTrigger = collider.isTrigger;
            definition.density = Mathf.Max(0f, collider.density);

            PhysicsShape.ContactFilter filter = PhysicsShape.ContactFilter.defaultFilter;
            filter.categories = new PhysicsMask { bitMask = 1ul << collider.gameObject.layer };
            filter.contacts = new PhysicsMask { bitMask = CalculateContactMask(collider) };
            definition.contactFilter = filter;

            PhysicsShape.SurfaceMaterial material = PhysicsShape.SurfaceMaterial.defaultMaterial;
            material.friction = Mathf.Max(0f, collider.friction);
            material.bounciness = Mathf.Max(0f, collider.bounciness);
            material.frictionMixing = ConvertMixing(collider.frictionCombine);
            material.bouncinessMixing = ConvertMixing(collider.bounceCombine);
            definition.surfaceMaterial = material;
            return definition;
        }



        private static PhysicsShapeDefinition CreateShapeDefinition(
            in Physics2DBridgeShapeDescriptor descriptor)
        {
            PhysicsShapeDefinition definition = PhysicsShapeDefinition.defaultDefinition;
            definition.contactEvents = true;
            definition.triggerEvents = true;
            definition.isTrigger = descriptor.IsTrigger;
            definition.density = descriptor.Density;

            PhysicsShape.ContactFilter filter = PhysicsShape.ContactFilter.defaultFilter;
            filter.categories = new PhysicsMask { bitMask = 1ul << descriptor.Layer };
            filter.contacts = new PhysicsMask { bitMask = CalculateContactMask(descriptor.Layer) };
            definition.contactFilter = filter;

            PhysicsShape.SurfaceMaterial material = PhysicsShape.SurfaceMaterial.defaultMaterial;
            material.friction = descriptor.Friction;
            material.bounciness = descriptor.Bounciness;
            material.frictionMixing = ConvertMixing(descriptor.FrictionCombine);
            material.bouncinessMixing = ConvertMixing(descriptor.BounceCombine);
            definition.surfaceMaterial = material;
            return definition;
        }



        private static ulong CalculateContactMask(Collider2D collider)
        {
            int layer = collider.gameObject.layer;
            ulong mask = 0ul;
            for (int otherLayer = 0; otherLayer < 32; otherLayer++)
            {
                if (!Physics2D.GetIgnoreLayerCollision(layer, otherLayer)) { mask |= 1ul << otherLayer; }
            }
            mask |= unchecked((uint)collider.includeLayers.value);
            mask &= ~unchecked((uint)collider.excludeLayers.value);
            return mask;
        }



        private static ulong CalculateContactMask(int layer)
        {
            ulong mask = 0ul;
            for (int otherLayer = 0; otherLayer < 32; otherLayer++)
            {
                if (!Physics2D.GetIgnoreLayerCollision(layer, otherLayer)) { mask |= 1ul << otherLayer; }
            }
            return mask;
        }



        private static PhysicsShape.SurfaceMaterial.MixingMode ConvertMixing(PhysicsMaterialCombine2D mode)
        {
            switch (mode)
            {
                case PhysicsMaterialCombine2D.Minimum: return PhysicsShape.SurfaceMaterial.MixingMode.Minimum;
                case PhysicsMaterialCombine2D.Multiply: return PhysicsShape.SurfaceMaterial.MixingMode.Multiply;
                case PhysicsMaterialCombine2D.Maximum: return PhysicsShape.SurfaceMaterial.MixingMode.Maximum;
                default: return PhysicsShape.SurfaceMaterial.MixingMode.Average;
            }
        }



        private void RebuildRecord(BodyRecord record)
        {
            ProcessPendingTilemapChanges(record);
            PhysicsBody previousBody = record.Body;
            int previousShapeCount = record.ShapeCount;
            float previousMinimumShapeExtent = record.MinimumShapeExtent;
            float previousMaximumShapeRadius = record.MaximumShapeRadius;
            var previousShapeProxies = new List<PhysicsShape.ShapeProxy>(record.ShapeProxies);
            var previousShapeTargetIds = new List<int>(record.ShapeTargetIds);
            var previousShapeCategoryMasks = new List<ulong>(record.ShapeCategoryMasks);
            var previousShapeContactMasks = new List<ulong>(record.ShapeContactMasks);
            var previousShapeTriggers = new List<byte>(record.ShapeTriggers);
            if (!CreateBodyAndShapes(record))
            {
                record.Body = previousBody;
                record.ShapeCount = previousShapeCount;
                record.MinimumShapeExtent = previousMinimumShapeExtent;
                record.MaximumShapeRadius = previousMaximumShapeRadius;
                record.ShapeProxies.Clear();
                record.ShapeProxies.AddRange(previousShapeProxies);
                record.ShapeTargetIds.Clear();
                record.ShapeTargetIds.AddRange(previousShapeTargetIds);
                record.ShapeCategoryMasks.Clear();
                record.ShapeCategoryMasks.AddRange(previousShapeCategoryMasks);
                record.ShapeContactMasks.Clear();
                record.ShapeContactMasks.AddRange(previousShapeContactMasks);
                record.ShapeTriggers.Clear();
                record.ShapeTriggers.AddRange(previousShapeTriggers);
                if (previousBody.isValid) { previousBody.enabled = false; }
                RefreshColliderProjectionSignatures(record);
                if (record.Provider != null || HasAnyActiveCollider(record))
                {
                    Debug.LogError("Physics2D bridge geometry를 재구성하지 못했습니다.", record.Owner);
                }
                return;
            }

            if (previousBody.isValid) { previousBody.Destroy(); }
            RefreshColliderProjectionSignatures(record);

            RefreshTargetRecords(record);
            RefreshFactReceiverCache(record);
        }



        private void RefreshTargetRecords(BodyRecord record)
        {
            if (record.Provider != null && record.TargetIds.Count > 0)
            {
                int targetId = record.TargetIds[0];
                targets[targetId] = new TargetRecord(
                    null,
                    record,
                    record.ProviderTarget,
                    record.ProviderLayer,
                    record.ProviderIsTrigger);
                return;
            }

            int count = Mathf.Min(record.Colliders.Count, record.TargetIds.Count);
            for (int i = 0; i < count; i++)
            {
                Collider2D collider = record.Colliders[i];
                if (collider == null) { continue; }
                int targetId = record.TargetIds[i];
                targets[targetId] = new TargetRecord(
                    collider,
                    record,
                    collider,
                    collider.gameObject.layer,
                    collider.isTrigger);
            }
        }



        private static bool HasColliderProjectionChanged(BodyRecord record)
        {
            if (record.Colliders.Count != record.ColliderProjectionSignatures.Count) { return true; }

            for (int i = 0; i < record.Colliders.Count; i++)
            {
                Collider2D collider = record.Colliders[i];
                if (collider == null ||
                    CalculateColliderProjectionSignature(collider) != record.ColliderProjectionSignatures[i])
                {
                    return true;
                }
            }
            return false;
        }



        private static bool HasAnyActiveCollider(BodyRecord record)
        {
            for (int i = 0; i < record.Colliders.Count; i++)
            {
                Collider2D collider = record.Colliders[i];
                if (collider != null && collider.isActiveAndEnabled) { return true; }
            }
            return false;
        }



        private static void RefreshColliderProjectionSignatures(BodyRecord record)
        {
            record.ColliderProjectionSignatures.Clear();
            for (int i = 0; i < record.Colliders.Count; i++)
            {
                Collider2D collider = record.Colliders[i];
                record.ColliderProjectionSignatures.Add(
                    collider != null ? CalculateColliderProjectionSignature(collider) : 0u);
            }
        }



        private static uint CalculateColliderProjectionSignature(Collider2D collider)
        {
            unchecked
            {
                uint signature = 2166136261u;
                AddSignatureValue(ref signature, collider.GetShapeHash());
                AddSignatureValue(ref signature, (uint)collider.gameObject.layer);
                AddSignatureValue(ref signature, collider.enabled ? 1u : 0u);
                AddSignatureValue(ref signature, collider.gameObject.activeInHierarchy ? 1u : 0u);
                AddSignatureValue(ref signature, collider.isTrigger ? 1u : 0u);
                AddSignatureValue(ref signature, (uint)collider.includeLayers.value);
                AddSignatureValue(ref signature, (uint)collider.excludeLayers.value);
                AddSignatureValue(ref signature, (uint)BitConverter.SingleToInt32Bits(collider.density));
                AddSignatureValue(ref signature, (uint)BitConverter.SingleToInt32Bits(collider.friction));
                AddSignatureValue(ref signature, (uint)BitConverter.SingleToInt32Bits(collider.bounciness));
                AddSignatureValue(ref signature, (uint)collider.frictionCombine);
                AddSignatureValue(ref signature, (uint)collider.bounceCombine);

                Rigidbody2D attachedRigidbody = collider.attachedRigidbody;
                if (attachedRigidbody == null)
                {
                    Matrix4x4 matrix = collider.transform.localToWorldMatrix;
                    for (int i = 0; i < 16; i++)
                    {
                        AddSignatureValue(ref signature, (uint)BitConverter.SingleToInt32Bits(matrix[i]));
                    }
                }
                return signature;
            }
        }



        private static void AddSignatureValue(ref uint signature, uint value)
        {
            unchecked
            {
                signature ^= value;
                signature *= 16777619u;
            }
        }



        private void DestroyRecord(BodyRecord record)
        {
            if (record == null) { return; }
            UnsubscribeProviderChanges(record);
            if (record.Body.isValid) { record.Body.Destroy(); }
            RemoveTargetMappings(record);
            bodies.Remove(record);
        }



        private static void SubscribeProviderChanges(BodyRecord record)
        {
            if (record?.Provider is not IPhysics2DBridgeChangeSource changeSource) { return; }

            record.ChangeSource = changeSource;
            record.GeometryChangedHandler = () => record.GeometryDirty = true;
            record.StateChangedHandler = () => record.StateDirty = true;
            changeSource.GeometryChanged += record.GeometryChangedHandler;
            changeSource.StateChanged += record.StateChangedHandler;
        }



        private static void UnsubscribeProviderChanges(BodyRecord record)
        {
            if (record?.ChangeSource == null) { return; }

            record.ChangeSource.GeometryChanged -= record.GeometryChangedHandler;
            record.ChangeSource.StateChanged -= record.StateChangedHandler;
            record.ChangeSource = null;
            record.GeometryChangedHandler = null;
            record.StateChangedHandler = null;
        }



        private void RemoveTargetMappings(BodyRecord record)
        {
            for (int i = 0; i < record.TargetIds.Count; i++)
            {
                targets.Remove(record.TargetIds[i]);
                if (i < record.Colliders.Count && !ReferenceEquals(record.Colliders[i], null))
                {
                    targetIdsByCollider.Remove(record.Colliders[i]);
                }
            }
        }



        private bool ShouldAutoRegister(Collider2D collider)
        {
            if (collider == null || !collider.isActiveAndEnabled || !settings.IncludesLayer(collider.gameObject.layer) ||
                IsCompositedSource(collider))
            {
                return false;
            }

            Rigidbody2D rigidbody = collider.attachedRigidbody;
            return rigidbody == null ||
                (rigidbody.bodyType == RigidbodyType2D.Static &&
                    rigidbody.simulated &&
                    rigidbody.gameObject.activeInHierarchy);
        }



        private static bool ShouldRemoveAutomaticRecord(BodyRecord record)
        {
            if (record == null || record.Owner != null || record.Provider != null) { return false; }

            Rigidbody2D rigidbody = record.Rigidbody;
            if (!ReferenceEquals(rigidbody, null) &&
                (rigidbody == null || !rigidbody.simulated || !rigidbody.gameObject.activeInHierarchy))
            {
                return true;
            }

            for (int i = 0; i < record.Colliders.Count; i++)
            {
                Collider2D collider = record.Colliders[i];
                if (collider != null && collider.isActiveAndEnabled) { return false; }
            }
            return true;
        }



        private static bool IsCompositedSource(Collider2D collider) =>
            !(collider is CompositeCollider2D) &&
            collider.compositeOperation != Collider2D.CompositeOperation.None &&
            collider.composite != null;



        private void ReportConversionErrorOnce(
            Collider2D collider,
            PhysicsShapeType2D shapeType,
            string reason)
        {
            int instanceId = collider != null ? collider.GetEntityId().GetHashCode() : 0;
            if (!reportedConversionErrors.Add(instanceId)) { return; }
            string path = collider != null ? GetHierarchyPath(collider.transform) : "<destroyed>";
            Debug.LogError($"Physics2D bridge 변환 실패: {path}, shape={shapeType}, reason={reason}", collider);
        }



        private void OnTilemapTileChanged(Tilemap tilemap, Tilemap.SyncTile[] changedTiles)
        {
            if (disposed || tilemap == null) { return; }
            TilemapCollider2D tilemapCollider = tilemap.GetComponent<TilemapCollider2D>();
            if (tilemapCollider == null) { return; }

            Collider2D target = tilemapCollider.compositeOperation != Collider2D.CompositeOperation.None &&
                tilemapCollider.composite != null
                    ? tilemapCollider.composite
                    : tilemapCollider;
            if (targetIdsByCollider.TryGetValue(target, out int targetId) &&
                targets.TryGetValue(targetId, out TargetRecord record))
            {
                record.Body.GeometryDirty = true;
            }
        }



        private static void ProcessPendingTilemapChanges(BodyRecord record)
        {
            for (int i = 0; i < record.Colliders.Count; i++)
            {
                Collider2D collider = record.Colliders[i];
                if (collider is TilemapCollider2D tilemapCollider)
                {
                    if (tilemapCollider.hasTilemapChanges) { tilemapCollider.ProcessTilemapChanges(); }
                    continue;
                }

                if (!(collider is CompositeCollider2D)) { continue; }
                TilemapCollider2D source = collider.GetComponent<TilemapCollider2D>();
                if (source != null && source.hasTilemapChanges) { source.ProcessTilemapChanges(); }
            }
        }
    }
}
