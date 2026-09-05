using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.U2D.Physics;
using UnityEngine;



namespace Pan.HighDensityElement
{
    [BurstCompile]
    internal struct ElementMotionJob : IJobParallelFor
    {
        public NativeArray<NativeElementState> States;
        public float DeltaTime;
        public ushort SubstepIndex;
        public uint FixedStepIndex;
        public NativeQueue<ElementFact>.ParallelWriter Facts;

        [ReadOnly]
        public NativeParallelHashMap<ElementKey, int>.ReadOnly DirectionalMotionIndices;

        [ReadOnly]
        public NativeArray<ElementDirectionalMotionFeature> DirectionalMotionFeatures;

        [ReadOnly]
        public NativeParallelHashMap<ElementKey, int>.ReadOnly WaveMotionIndices;

        [NativeDisableContainerSafetyRestriction]
        [NativeDisableParallelForRestriction]
        public NativeArray<NativeElementWaveMotionState> WaveMotionFeatures;

        [ReadOnly]
        public NativeParallelHashMap<ElementKey, int>.ReadOnly VisualOrientationIndices;

        [NativeDisableContainerSafetyRestriction]
        [NativeDisableParallelForRestriction]
        public NativeArray<NativeElementVisualOrientationState> VisualOrientationFeatures;

        [ReadOnly]
        public NativeParallelHashMap<ElementKey, int>.ReadOnly BoundaryIndices;

        [NativeDisableContainerSafetyRestriction]
        [NativeDisableParallelForRestriction]
        public NativeArray<NativeElementBoundaryState> BoundaryFeatures;

        public ElementBounds2D WorldBoundaryBounds;
        public ElementBounds2D ViewBoundaryBounds;
        public byte HasWorldBoundaryBounds;
        public byte HasViewBoundaryBounds;



        public void Execute(int index)
        {
            NativeElementState state = States[index];
            if (state.Lifecycle != ElementLifecycle.Alive) { return; }

            float effectiveDeltaTime = DeltaTime;
            bool isBodylessQuery = (state.Capabilities & ElementCapabilities.QuerySensor2D) != 0 &&
                (state.Capabilities & (ElementCapabilities.DynamicBody2D | ElementCapabilities.QueryTarget2D)) == 0;
            if (isBodylessQuery && state.HasLocalClock != 0)
            {
                effectiveDeltaTime = state.LocalClockPaused != 0
                    ? 0f
                    : DeltaTime * state.LocalTimeScale;
            }

            state.TeleportedThisStep = state.TeleportPending;
            state.TeleportPending = 0;
            state.PreviousPosition = state.Position;
            state.PreviousRotationRadians = state.RotationRadians;
            state.PreviousScale = state.Scale;
            if ((state.Capabilities & ElementCapabilities.KinematicMotion2D) != 0)
            {
                state.Velocity += state.Acceleration * effectiveDeltaTime;
                if (state.LinearDamping > 0f)
                {
                    state.Velocity *= 1f / (1f + state.LinearDamping * effectiveDeltaTime);
                }

                ApplyDirectionalMotion(ref state, effectiveDeltaTime);
                state.Position += state.Velocity * effectiveDeltaTime;
                ApplyWaveMotion(ref state, effectiveDeltaTime);
            }

            ApplyVisualOrientation(ref state, effectiveDeltaTime);

            bool boundaryExit = ApplyBoundary(
                in state,
                out bool hasBoundary,
                out int boundaryIndex,
                out NativeElementBoundaryState boundary);
            if (hasBoundary) { BoundaryFeatures[boundaryIndex] = boundary; }
            if (boundaryExit)
            {
                state.Lifecycle = ElementLifecycle.DespawnPending;
                Facts.Enqueue(new ElementFact
                {
                    Type = ElementFactType.BoundaryExited,
                    Element = state.Key,
                    Position = state.Position,
                    TargetId = -1,
                    BridgeTargetId = -1,
                    SubstepIndex = SubstepIndex,
                    FixedStepIndex = FixedStepIndex
                });
                States[index] = state;
                return;
            }

            float minimumExtent = math.max(
                0.0001f,
                state.Radius * 2f * math.max(0.0001f, math.min(math.abs(state.Scale.x), math.abs(state.Scale.y))));
            float threshold = minimumExtent * math.max(0.01f, state.StrictCcdThresholdRatio);
            bool forced = state.StrictCcdRemainingSteps > 0;
            state.StrictCcdForcedThisStep = (byte)(forced ? 1 : 0);
            bool automatic = state.StrictCcdOverride == StrictCcdOverride2D.Auto &&
                math.lengthsq(state.Position - state.PreviousPosition) > threshold * threshold;
            state.StrictCcdActive = (byte)(forced ||
                state.StrictCcdOverride == StrictCcdOverride2D.ForceOn ||
                automatic ? 1 : 0);
            if (state.StrictCcdRemainingSteps > 0) { state.StrictCcdRemainingSteps--; }

            if ((state.Capabilities & ElementCapabilities.PeriodicFact) != 0 && state.PeriodicInterval > 0f)
            {
                state.PeriodicRemaining -= effectiveDeltaTime;
                if (state.PeriodicRemaining <= 0f)
                {
                    state.PeriodicRemaining += state.PeriodicInterval;
                    Facts.Enqueue(new ElementFact
                    {
                        Type = ElementFactType.Periodic,
                        Element = state.Key,
                        Position = state.Position,
                        TargetId = -1,
                        BridgeTargetId = -1,
                        SubstepIndex = SubstepIndex,
                        FixedStepIndex = FixedStepIndex
                    });
                }
            }

            States[index] = state;
        }



        private void ApplyDirectionalMotion(ref NativeElementState state, float deltaTime)
        {
            if (deltaTime <= 0f ||
                !DirectionalMotionIndices.TryGetValue(state.Key, out int featureIndex) ||
                featureIndex < 0 || featureIndex >= DirectionalMotionFeatures.Length)
            {
                return;
            }

            ElementDirectionalMotionFeature feature = DirectionalMotionFeatures[featureIndex];
            float speedSquared = math.lengthsq(state.Velocity);
            if (speedSquared <= 0.00000001f) { return; }

            float turnRadians = 0f;
            switch (feature.Mode)
            {
                case ElementDirectionalMotionMode.AngularTurn:
                    turnRadians = feature.AngularSpeedRadiansPerSecond * deltaTime;
                    break;
                case ElementDirectionalMotionMode.HomingPoint:
                    float2 toTarget = feature.HomingPoint - state.Position;
                    if (math.lengthsq(toTarget) <= 0.00000001f) { return; }

                    float currentAngle = math.atan2(state.Velocity.y, state.Velocity.x);
                    float targetAngle = math.atan2(toTarget.y, toTarget.x);
                    float angleDelta = math.atan2(
                        math.sin(targetAngle - currentAngle),
                        math.cos(targetAngle - currentAngle));
                    float maximumTurn = feature.MaxTurnRadiansPerSecond * deltaTime;
                    turnRadians = math.clamp(angleDelta, -maximumTurn, maximumTurn);
                    break;
                default:
                    return;
            }

            state.Velocity = Rotate(state.Velocity, turnRadians);
        }



        private void ApplyWaveMotion(ref NativeElementState state, float deltaTime)
        {
            if (!WaveMotionIndices.TryGetValue(state.Key, out int featureIndex) ||
                featureIndex < 0 || featureIndex >= WaveMotionFeatures.Length)
            {
                return;
            }

            NativeElementWaveMotionState native = WaveMotionFeatures[featureIndex];
            float2 fallbackAxis = native.Initialized != 0
                ? native.PreviousAxis
                : new float2(0f, 1f);
            float2 forward = math.normalizesafe(state.Velocity, new float2(fallbackAxis.y, -fallbackAxis.x));
            float2 axis = new float2(-forward.y, forward.x);
            native.ElapsedSeconds += math.max(0f, deltaTime);
            float newOffset = native.Feature.Amplitude * math.sin(
                native.Feature.AngularFrequencyRadiansPerSecond * native.ElapsedSeconds +
                native.Feature.PhaseRadians);
            float2 previousWave = native.Initialized != 0
                ? native.PreviousAxis * native.PreviousOffset
                : default;
            state.Position += axis * newOffset - previousWave;
            native.PreviousAxis = axis;
            native.PreviousOffset = newOffset;
            native.Initialized = 1;
            WaveMotionFeatures[featureIndex] = native;
        }



        private void ApplyVisualOrientation(ref NativeElementState state, float deltaTime)
        {
            if (!VisualOrientationIndices.TryGetValue(state.Key, out int featureIndex) ||
                featureIndex < 0 || featureIndex >= VisualOrientationFeatures.Length)
            {
                return;
            }

            NativeElementVisualOrientationState native = VisualOrientationFeatures[featureIndex];
            float spinDelta = native.Feature.SpinRadiansPerSecond * math.max(0f, deltaTime);
            native.SpinPhaseRadians += spinDelta;
            if (native.Feature.Mode == ElementVisualOrientationMode.Velocity &&
                math.lengthsq(state.Velocity) > 0.00000001f)
            {
                state.RotationRadians = math.atan2(state.Velocity.y, state.Velocity.x) +
                    native.Feature.AxisOffsetRadians + native.SpinPhaseRadians;
            }
            else if (native.Feature.Mode == ElementVisualOrientationMode.Manual)
            {
                state.RotationRadians += spinDelta;
            }

            VisualOrientationFeatures[featureIndex] = native;
        }



        private bool ApplyBoundary(
            in NativeElementState state,
            out bool hasBoundary,
            out int featureIndex,
            out NativeElementBoundaryState native)
        {
            hasBoundary = false;
            featureIndex = -1;
            native = default;
            if (!BoundaryIndices.TryGetValue(state.Key, out featureIndex) ||
                featureIndex < 0 || featureIndex >= BoundaryFeatures.Length)
            {
                return false;
            }

            hasBoundary = true;
            native = BoundaryFeatures[featureIndex];
            bool hasBounds;
            ElementBounds2D bounds;
            if (native.Feature.Mode == ElementBoundaryMode2D.WorldBounds)
            {
                hasBounds = HasWorldBoundaryBounds != 0;
                bounds = WorldBoundaryBounds;
            }
            else
            {
                hasBounds = HasViewBoundaryBounds != 0;
                bounds = ViewBoundaryBounds;
            }

            if (!hasBounds) { return false; }
            bool inside = bounds.Contains(state.Position, native.Feature.Margin);
            if (inside)
            {
                native.HasEntered = 1;
                return false;
            }

            return !native.Feature.RequireEnteredBeforeExit || native.HasEntered != 0;
        }



        private static float2 Rotate(float2 value, float radians)
        {
            math.sincos(radians, out float sine, out float cosine);
            return new float2(
                value.x * cosine - value.y * sine,
                value.x * sine + value.y * cosine);
        }
    }



    [BurstCompile]
    internal struct PhysicsCoreProjectileQueryJob : IJobParallelFor
    {
        [ReadOnly]
        public PhysicsWorld World;

        [ReadOnly]
        public NativeArray<NativeElementState> States;

        [ReadOnly]
        public NativeArray<ElementRegistryEntry> Registry;

        [ReadOnly]
        public NativeArray<NativeElementState> KinematicStates;

        [ReadOnly]
        public NativeArray<NativeElementState> AreaStates;

        [ReadOnly]
        public NativeArray<NativeElementState> PhysicsStates;

        [ReadOnly]
        public NativeArray<MovingProjectionShape2D> MovingProjections;

        public int WorldId;
        public ushort SubstepIndex;
        public uint FixedStepIndex;
        public NativeQueue<ElementFact>.ParallelWriter Facts;



        public void Execute(int index)
        {
            NativeElementState state = States[index];
            if (state.Lifecycle != ElementLifecycle.Alive ||
                (state.Capabilities & ElementCapabilities.QuerySensor2D) == 0)
            {
                return;
            }

            CircleGeometry geometry = new CircleGeometry
            {
                center = new Vector2(state.PreviousPosition.x, state.PreviousPosition.y),
                radius = math.max(0.0001f, state.Radius)
            };
            PhysicsQuery.QueryFilter filter = PhysicsQuery.QueryFilter.defaultFilter;
            filter.categories = new PhysicsMask { bitMask = state.PhysicsCategoryMask };
            filter.hitCategories = new PhysicsMask { bitMask = state.InteractionLayerMask };

            using NativeArray<PhysicsQuery.WorldOverlapResult> overlaps = World.OverlapGeometry(
                geometry,
                filter,
                Allocator.Temp);
            for (int hitIndex = 0; hitIndex < overlaps.Length; hitIndex++)
            {
                PhysicsQuery.WorldOverlapResult overlap = overlaps[hitIndex];
                if (!overlap.isValid || IsDuplicateOverlap(overlaps, hitIndex, overlap.shape)) { continue; }
                ResolveStartingOverlap(
                    in state,
                    overlap.shape,
                    out float2 surfacePoint,
                    out float2 surfaceNormal,
                    out ElementFactFlags flags);
                EnqueueFact(
                    in state,
                    overlap.shape,
                    surfacePoint,
                    surfaceNormal,
                    state.PreviousPosition,
                    0f,
                    flags);
            }

            float2 translation = state.Position - state.PreviousPosition;
            if (math.lengthsq(translation) > 0.00000001f)
            {
                using NativeArray<PhysicsQuery.WorldCastResult> hits = World.CastGeometry(
                    geometry,
                    new Vector2(translation.x, translation.y),
                    filter,
                    PhysicsQuery.WorldCastMode.AllSorted,
                    Allocator.Temp);
                for (int hitIndex = 0; hitIndex < hits.Length; hitIndex++)
                {
                    PhysicsQuery.WorldCastResult hit = hits[hitIndex];
                    if (!hit.isValid ||
                        IsDuplicateCast(hits, hitIndex, hit.shape) ||
                        WasOverlapped(overlaps, hit.shape))
                    {
                        continue;
                    }

                    EnqueueFact(
                        in state,
                        hit.shape,
                        new float2(hit.point.x, hit.point.y),
                        new float2(hit.normal.x, hit.normal.y),
                        math.lerp(state.PreviousPosition, state.Position, hit.fraction),
                        hit.fraction,
                        ElementFactFlags.HasImpactCenter |
                        ElementFactGeometry.GetSurfaceFlags(
                            new float2(hit.point.x, hit.point.y),
                            new float2(hit.normal.x, hit.normal.y)));
                }
            }

            if (MovingProjections.IsCreated && MovingProjections.Length > 0)
            {
                QueryMovingProjections(in state);
            }
        }



        private static void ResolveStartingOverlap(
            in NativeElementState state,
            PhysicsShape targetShape,
            out float2 surfacePoint,
            out float2 surfaceNormal,
            out ElementFactFlags flags)
        {
            surfacePoint = default;
            surfaceNormal = default;
            flags = ElementFactFlags.StartedOverlapped | ElementFactFlags.HasImpactCenter;

            CircleGeometry projectileGeometry = new CircleGeometry
            {
                center = Vector2.zero,
                radius = math.max(0.0001f, state.Radius)
            };
            PhysicsTransform projectileTransform = new PhysicsTransform(
                new Vector2(state.PreviousPosition.x, state.PreviousPosition.y),
                PhysicsRotate.identity);
            PhysicsShape.ContactManifold manifold;
            switch (targetShape.shapeType)
            {
                case PhysicsShape.ShapeType.Circle:
                    manifold = PhysicsQuery.CircleAndCircle(
                        targetShape.circleGeometry,
                        targetShape.transform,
                        projectileGeometry,
                        projectileTransform);
                    break;
                case PhysicsShape.ShapeType.Capsule:
                    manifold = PhysicsQuery.CapsuleAndCircle(
                        targetShape.capsuleGeometry,
                        targetShape.transform,
                        projectileGeometry,
                        projectileTransform);
                    break;
                case PhysicsShape.ShapeType.Polygon:
                    manifold = PhysicsQuery.PolygonAndCircle(
                        targetShape.polygonGeometry,
                        targetShape.transform,
                        projectileGeometry,
                        projectileTransform);
                    break;
                case PhysicsShape.ShapeType.Segment:
                    manifold = PhysicsQuery.SegmentAndCircle(
                        targetShape.segmentGeometry,
                        targetShape.transform,
                        projectileGeometry,
                        projectileTransform);
                    break;
                case PhysicsShape.ShapeType.ChainSegment:
                    manifold = PhysicsQuery.ChainSegmentAndCircle(
                        targetShape.chainSegmentGeometry,
                        targetShape.transform,
                        projectileGeometry,
                        projectileTransform);
                    break;
                default:
                    return;
            }

            if (manifold.pointCount <= 0) { return; }
            PhysicsShape.ContactManifold.ManifoldPoint manifoldPoint = manifold.points[0];
            surfacePoint = new float2(manifoldPoint.point.x, manifoldPoint.point.y);
            surfaceNormal = new float2(manifold.normal.x, manifold.normal.y);
            flags |= ElementFactGeometry.GetSurfaceFlags(surfacePoint, surfaceNormal);
        }



        private void EnqueueFact(
            in NativeElementState state,
            PhysicsShape shape,
            float2 position,
            float2 normal,
            float2 impactCenter,
            float timeOfImpact,
            ElementFactFlags flags)
        {
            if (!PhysicsCore2DLane.TryResolveCounterpart(
                    shape,
                    WorldId,
                    out ElementKey targetElement,
                    out int bridgeTargetId) ||
                targetElement == state.Key)
            {
                return;
            }
            if (state.IncludeTriggers == 0 && shape.isTrigger) { return; }

            if (targetElement.IsValid &&
                TryGetTargetState(targetElement, out NativeElementState targetState) &&
                state.OwnerId != 0u &&
                targetState.OwnerId == state.OwnerId)
            {
                //? QueryTarget opt-in Element가 같은 owner의 projectile을 자기 충돌 대상으로 만들지 않게 합니다.
                return;
            }

            Facts.Enqueue(new ElementFact
            {
                Type = ElementFactType.Contact,
                Element = state.Key,
                TargetElement = targetElement,
                TargetId = bridgeTargetId >= 0 ? bridgeTargetId : targetElement.Slot,
                BridgeTargetId = bridgeTargetId,
                Position = position,
                Normal = normal,
                ImpactCenter = impactCenter,
                TimeOfImpact = timeOfImpact,
                IsTrigger = shape.isTrigger ? (byte)1 : (byte)0,
                Flags = flags,
                SubstepIndex = SubstepIndex,
                FixedStepIndex = FixedStepIndex
            });
        }



        private void QueryMovingProjections(in NativeElementState state)
        {
            CircleGeometry projectileGeometry = new CircleGeometry
            {
                center = Vector2.zero,
                radius = math.max(0.0001f, state.Radius)
            };
            PhysicsShape.ShapeProxy projectileShape = projectileGeometry.CreateShapeProxy();
            PhysicsRotate identityRotation = PhysicsRotate.FromDegrees(0f);
            PhysicsQuery.ShapeSweep projectileSweep = new PhysicsQuery.ShapeSweep
            {
                localCOM = Vector2.zero,
                positionStart = new Vector2(state.PreviousPosition.x, state.PreviousPosition.y),
                positionEnd = new Vector2(state.Position.x, state.Position.y),
                rotationStart = identityRotation,
                rotationEnd = identityRotation
            };

            int projectionIndex = 0;
            while (projectionIndex < MovingProjections.Length)
            {
                int bridgeTargetId = MovingProjections[projectionIndex].BridgeTargetId;
                int groupEnd = projectionIndex + 1;
                while (groupEnd < MovingProjections.Length &&
                    MovingProjections[groupEnd].BridgeTargetId == bridgeTargetId)
                {
                    groupEnd++;
                }

                bool found = false;
                float bestFraction = float.MaxValue;
                float2 bestPosition = default;
                float2 bestNormal = default;
                int bestStableShapeId = int.MaxValue;
                byte bestIsTrigger = 0;
                ElementFactFlags bestFlags = ElementFactFlags.None;
                for (int i = projectionIndex; i < groupEnd; i++)
                {
                    MovingProjectionShape2D projection = MovingProjections[i];
                    if (projection.IsTeleported ||
                        !CanInteract(in state, in projection) ||
                        !SweptAabbsOverlap(in state, in projection))
                    {
                        continue;
                    }

                    PhysicsQuery.ShapeSweep projectionSweep = new PhysicsQuery.ShapeSweep
                    {
                        localCOM = Vector2.zero,
                        positionStart = projection.PreviousTransform.position,
                        positionEnd = projection.CurrentTransform.position,
                        rotationStart = projection.PreviousTransform.rotation,
                        rotationEnd = projection.CurrentTransform.rotation
                    };
                    if (!TryGetMovingProjectionHit(
                            in state,
                            in projection,
                            projectileShape,
                            in projectileSweep,
                            in projectionSweep,
                            out float fraction,
                            out float2 position,
                            out float2 normal,
                            out ElementFactFlags flags))
                    {
                        continue;
                    }

                    if (!found ||
                        fraction < bestFraction ||
                        (math.abs(fraction - bestFraction) <= 0.000001f &&
                            projection.StableShapeId < bestStableShapeId))
                    {
                        found = true;
                        bestFraction = fraction;
                        bestPosition = position;
                        bestNormal = normal;
                        bestStableShapeId = projection.StableShapeId;
                        bestIsTrigger = projection.IsTrigger;
                        bestFlags = flags;
                    }
                }

                if (found)
                {
                    Facts.Enqueue(new ElementFact
                    {
                        Type = ElementFactType.Contact,
                        Element = state.Key,
                        TargetId = bridgeTargetId,
                        BridgeTargetId = bridgeTargetId,
                        Position = bestPosition,
                        Normal = bestNormal,
                        ImpactCenter = math.lerp(state.PreviousPosition, state.Position, bestFraction),
                        TimeOfImpact = bestFraction,
                        IsTrigger = bestIsTrigger,
                        Flags = bestFlags,
                        SubstepIndex = SubstepIndex,
                        FixedStepIndex = FixedStepIndex
                    });
                }

                projectionIndex = groupEnd;
            }
        }



        private static bool TryGetMovingProjectionHit(
            in NativeElementState state,
            in MovingProjectionShape2D projection,
            PhysicsShape.ShapeProxy projectileShape,
            in PhysicsQuery.ShapeSweep projectileSweep,
            in PhysicsQuery.ShapeSweep projectionSweep,
            out float fraction,
            out float2 position,
            out float2 normal,
            out ElementFactFlags flags)
        {
            flags = ElementFactFlags.StartedOverlapped | ElementFactFlags.HasImpactCenter;
            if (TryResolveProjectionStartingOverlap(
                    in state,
                    in projection,
                    out position,
                    out normal,
                    ref flags))
            {
                fraction = 0f;
                return true;
            }

            PhysicsQuery.TimeOfImpactResult timeOfImpact = PhysicsQuery.ShapeTimeOfImpact(
                new PhysicsQuery.TimeOfImpactInput
                {
                    shapeProxyA = projectileShape,
                    shapeProxyB = projection.Shape,
                    shapeSweepA = projectileSweep,
                    shapeSweepB = projectionSweep,
                    maxFraction = 1f
                });
            if (timeOfImpact.impactState == PhysicsQuery.TimeOfImpactResult.State.Hit ||
                timeOfImpact.impactState == PhysicsQuery.TimeOfImpactResult.State.Overlapped)
            {
                bool startedOverlapped =
                    timeOfImpact.impactState == PhysicsQuery.TimeOfImpactResult.State.Overlapped;
                fraction = startedOverlapped ? 0f : timeOfImpact.fraction;
                position = new float2(timeOfImpact.point.x, timeOfImpact.point.y);
                normal = -new float2(timeOfImpact.normal.x, timeOfImpact.normal.y);
                flags = ElementFactFlags.HasImpactCenter |
                    ElementFactGeometry.GetSurfaceFlags(position, normal);
                if (startedOverlapped)
                {
                    flags |= ElementFactFlags.StartedOverlapped;
                    TryResolveProjectionStartingOverlap(
                        in state,
                        in projection,
                        out position,
                        out normal,
                        ref flags);
                }
                return true;
            }

            PhysicsRotate previousRotation = projection.PreviousTransform.rotation;
            PhysicsRotate currentRotation = projection.CurrentTransform.rotation;
            if (math.abs(previousRotation.sin - currentRotation.sin) > 0.000001f ||
                math.abs(previousRotation.cos - currentRotation.cos) > 0.000001f)
            {
                fraction = default;
                position = default;
                normal = default;
                flags = default;
                return false;
            }

            float2 projectileTranslation = state.Position - state.PreviousPosition;
            float2 projectionTranslation = new float2(
                projection.CurrentTransform.position.x - projection.PreviousTransform.position.x,
                projection.CurrentTransform.position.y - projection.PreviousTransform.position.y);
            float2 relativeTranslation = projectileTranslation - projectionTranslation;
            if (math.lengthsq(relativeTranslation) <= 0.00000001f)
            {
                fraction = default;
                position = default;
                normal = default;
                flags = default;
                return false;
            }

            //? ShapeTimeOfImpact가 놓칠 수 있는 시작/끝 분리 상태의 중간 교차를 상대 이동 cast로 보완합니다.
            PhysicsQuery.CastResult cast = PhysicsQuery.CastShapes(
                new PhysicsQuery.CastShapePairInput
                {
                    shapeProxyA = projection.Shape,
                    shapeProxyB = projectileShape,
                    transformA = projection.PreviousTransform,
                    transformB = new PhysicsTransform(
                        new Vector2(state.PreviousPosition.x, state.PreviousPosition.y),
                        PhysicsRotate.identity),
                    translationB = new Vector2(relativeTranslation.x, relativeTranslation.y),
                    maxFraction = 1f,
                    canEncroach = true
                });
            if (!cast.isValid)
            {
                fraction = default;
                position = default;
                normal = default;
                flags = default;
                return false;
            }

            fraction = cast.fraction;
            position = new float2(cast.point.x, cast.point.y) + projectionTranslation * fraction;
            normal = new float2(cast.normal.x, cast.normal.y);
            flags = ElementFactFlags.HasImpactCenter;
            if (fraction <= 0.000001f)
            {
                flags |= ElementFactFlags.StartedOverlapped;
                TryResolveProjectionStartingOverlap(
                    in state,
                    in projection,
                    out position,
                    out normal,
                    ref flags);
            }
            else
            {
                flags |= ElementFactGeometry.GetSurfaceFlags(position, normal);
            }
            return true;
        }



        private static bool TryResolveProjectionStartingOverlap(
            in NativeElementState state,
            in MovingProjectionShape2D projection,
            out float2 surfacePoint,
            out float2 surfaceNormal,
            ref ElementFactFlags flags)
        {
            surfacePoint = default;
            surfaceNormal = default;
            flags &= ~(ElementFactFlags.HasSurfacePoint | ElementFactFlags.HasSurfaceNormal);

            CircleGeometry projectileGeometry = new CircleGeometry
            {
                center = Vector2.zero,
                radius = math.max(0.0001f, state.Radius)
            };
            PhysicsTransform projectileTransform = new PhysicsTransform(
                new Vector2(state.PreviousPosition.x, state.PreviousPosition.y),
                PhysicsRotate.identity);
            PhysicsShape.ContactManifold manifold;
            switch (projection.Shape.shapeType)
            {
                case PhysicsShape.ShapeType.Circle:
                    manifold = PhysicsQuery.CircleAndCircle(
                        projection.Shape.circleGeometry,
                        projection.PreviousTransform,
                        projectileGeometry,
                        projectileTransform);
                    break;
                case PhysicsShape.ShapeType.Capsule:
                    manifold = PhysicsQuery.CapsuleAndCircle(
                        projection.Shape.capsuleGeometry,
                        projection.PreviousTransform,
                        projectileGeometry,
                        projectileTransform);
                    break;
                case PhysicsShape.ShapeType.Polygon:
                    manifold = PhysicsQuery.PolygonAndCircle(
                        projection.Shape.polygonGeometry,
                        projection.PreviousTransform,
                        projectileGeometry,
                        projectileTransform);
                    break;
                case PhysicsShape.ShapeType.Segment:
                case PhysicsShape.ShapeType.ChainSegment:
                    manifold = PhysicsQuery.SegmentAndCircle(
                        projection.Shape.segmentGeometry,
                        projection.PreviousTransform,
                        projectileGeometry,
                        projectileTransform);
                    break;
                default:
                    return false;
            }

            if (manifold.pointCount <= 0) { return false; }
            PhysicsShape.ContactManifold.ManifoldPoint manifoldPoint = manifold.points[0];
            surfacePoint = new float2(manifoldPoint.point.x, manifoldPoint.point.y);
            surfaceNormal = new float2(manifold.normal.x, manifold.normal.y);
            flags |= ElementFactGeometry.GetSurfaceFlags(surfacePoint, surfaceNormal);
            return true;
        }



        private static bool CanInteract(
            in NativeElementState state,
            in MovingProjectionShape2D projection)
        {
            return (state.InteractionLayerMask & projection.CategoryMask) != 0ul &&
                (projection.ContactMask & state.PhysicsCategoryMask) != 0ul &&
                (state.IncludeTriggers != 0 || projection.IsTrigger == 0);
        }



        private static bool SweptAabbsOverlap(
            in NativeElementState state,
            in MovingProjectionShape2D projection)
        {
            float projectileRadius = math.max(0.0001f, state.Radius);
            float2 projectileMin = math.min(state.PreviousPosition, state.Position) - projectileRadius;
            float2 projectileMax = math.max(state.PreviousPosition, state.Position) + projectileRadius;

            PhysicsAABB localAabb = projection.Shape.aabb;
            float targetRadius = math.max(
                math.length(new float2(localAabb.lowerBound.x, localAabb.lowerBound.y)),
                math.length(new float2(localAabb.upperBound.x, localAabb.upperBound.y)));
            float2 previous = new float2(
                projection.PreviousTransform.position.x,
                projection.PreviousTransform.position.y);
            float2 current = new float2(
                projection.CurrentTransform.position.x,
                projection.CurrentTransform.position.y);
            float2 targetMin = math.min(previous, current) - targetRadius;
            float2 targetMax = math.max(previous, current) + targetRadius;
            return projectileMin.x <= targetMax.x && projectileMax.x >= targetMin.x &&
                projectileMin.y <= targetMax.y && projectileMax.y >= targetMin.y;
        }



        private bool TryGetTargetState(ElementKey key, out NativeElementState state)
        {
            if (key.WorldId != WorldId || key.Slot < 0 || key.Slot >= Registry.Length)
            {
                state = default;
                return false;
            }

            ElementRegistryEntry entry = Registry[key.Slot];
            if (entry.Generation != key.Generation ||
                entry.Lifecycle != ElementLifecycle.Alive ||
                entry.DenseIndex < 0)
            {
                state = default;
                return false;
            }

            switch (entry.Lane)
            {
                case ElementLane.QuerySprite2D:
                    return TryReadState(KinematicStates, entry.DenseIndex, out state);
                case ElementLane.AreaSensorSprite2D:
                    return TryReadState(AreaStates, entry.DenseIndex, out state);
                case ElementLane.DynamicBodySprite2D:
                    return TryReadState(PhysicsStates, entry.DenseIndex, out state);
                default:
                    state = default;
                    return false;
            }
        }



        private static bool TryReadState(
            NativeArray<NativeElementState> states,
            int index,
            out NativeElementState state)
        {
            if (index >= 0 && index < states.Length)
            {
                state = states[index];
                return true;
            }

            state = default;
            return false;
        }



        private static bool IsDuplicateOverlap(
            NativeArray<PhysicsQuery.WorldOverlapResult> hits,
            int hitIndex,
            PhysicsShape shape)
        {
            ulong userData = PhysicsCore2DLane.GetCounterpartUserData(shape);
            for (int i = 0; i < hitIndex; i++)
            {
                if (PhysicsCore2DLane.GetCounterpartUserData(hits[i].shape) == userData) { return true; }
            }
            return false;
        }



        private static bool IsDuplicateCast(
            NativeArray<PhysicsQuery.WorldCastResult> hits,
            int hitIndex,
            PhysicsShape shape)
        {
            ulong userData = PhysicsCore2DLane.GetCounterpartUserData(shape);
            for (int i = 0; i < hitIndex; i++)
            {
                if (PhysicsCore2DLane.GetCounterpartUserData(hits[i].shape) == userData) { return true; }
            }
            return false;
        }



        private static bool WasOverlapped(
            NativeArray<PhysicsQuery.WorldOverlapResult> overlaps,
            PhysicsShape shape)
        {
            ulong userData = PhysicsCore2DLane.GetCounterpartUserData(shape);
            for (int i = 0; i < overlaps.Length; i++)
            {
                if (PhysicsCore2DLane.GetCounterpartUserData(overlaps[i].shape) == userData) { return true; }
            }
            return false;
        }
    }
}
