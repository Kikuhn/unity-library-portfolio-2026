using Pan.HighDensityElement;
using Unity.Mathematics;
using UnityEngine;



namespace Pan.Tan.Element
{
    /// <summary>
    /// ElementTan의 생성 조립과 읽기 전용 snapshot 변환을 담당합니다.
    /// </summary>
    public sealed partial class ElementTanRuntime
    {
        public bool TrySpawn(in TanSpawnRequest request, out TanHandle handle)
        {
            handle = default;
            if (!IsAvailable || records.Count >= Capacity) { return false; }

            TanDefinition definition = request.Definition;
            //. ElementTan은 runtime VisualId 변경을 지원하므로 spawn 시 표시가 없어도 시각 capability를 유지합니다.
            ElementCapabilities capabilities =
                ElementCapabilities.KinematicMotion2D |
                ElementCapabilities.QuerySensor2D |
                ElementCapabilities.SpriteVisual2D;
            if (definition.QueryableByElementPhysics) { capabilities |= ElementCapabilities.QueryTarget2D; }

            ResolveStrictCcdPolicy(
                definition.CcdPolicy,
                out StrictCcdOverride2D strictCcdOverride,
                out float strictCcdThresholdRatio);

            if (!ElementCompiledArchetype.TryCreate(
                    capabilities,
                    definition.Radius,
                    0f,
                    definition.VisualId,
                    0f,
                    false,
                    PhysicsCoreBodyMode.Kinematic,
                    PhysicsCoreShape2D.Circle,
                    out ElementCompiledArchetype archetype,
                    out _,
                    false,
                    strictCcdOverride,
                    strictCcdThresholdRatio))
            {
                return false;
            }

            Vector2 scale = definition.VisualScale;
            ElementSpawnBuilder builder = ElementSpawnBuilder.From(archetype)
                .WithPose(
                    new float2(request.Position.x, request.Position.y),
                    new float2(scale.x, scale.y))
                .WithRotation(math.radians(definition.RotationDegrees))
                .WithVelocity(new float2(request.Velocity.x, request.Velocity.y))
                .WithOwner(request.Source.IsValid ? request.Source.ToStableOwnerId() : 0u, (uint)definition.TeamId)
                .WithColor(definition.Color)
                .WithPhysicsFilter(
                    1ul << definition.Collision.ObjectLayer,
                    unchecked((uint)definition.Collision.HitLayers.value))
                .WithTriggerInteraction(definition.Collision.IncludeTriggers)
                .WithStrictCcd(strictCcdOverride, strictCcdThresholdRatio);

            builder = builder.WithMotion(
                new float2(definition.Motion.Acceleration.x, definition.Motion.Acceleration.y),
                definition.Motion.LinearDamping);

            ElementSpawnResult result = world.Spawn(in builder);
            if (!result.Succeeded) { return false; }

            if (definition.Lifetime.Enabled)
            {
                var lifetime = new ElementLifetimeFeature(
                    definition.Lifetime.RemainingUnits,
                    definition.Lifetime.Schedule,
                    definition.Lifetime.UseLocalClock);
                if (!world.TrySetLifetime(result.Handle.Key, in lifetime))
                {
                    world.TryDespawnImmediately(result.Handle.Key);
                    return false;
                }
            }

            if (!TryConfigureSpawnFeatures(result.Handle.Key, in definition))
            {
                world.TryDespawnImmediately(result.Handle.Key);
                return false;
            }

            TanKey key = ToTanKey(result.Handle.Key);
            records.Add(key, new TanRecord
            {
                Element = result.Handle,
                Spawn = request,
                VisualOrientation = definition.VisualOrientation,
                PrimaryMotion = definition.PrimaryMotion,
                WaveMotion = definition.WaveMotion,
                Boundary = definition.Boundary
            });
            handle = new TanHandle(this, in key);
            RaiseFact(new TanFact(TanFactType.Spawned, in key, default, request.Position));
            return true;
        }



        private void ResolveStrictCcdPolicy(
            in TanCcdPolicy policy,
            out StrictCcdOverride2D strictCcdOverride,
            out float strictCcdThresholdRatio)
        {
            if (policy.Mode == TanCcdMode.RuntimeDefault)
            {
                strictCcdOverride = options.StrictCcdOverride;
                strictCcdThresholdRatio = options.StrictCcdThresholdRatio;
                return;
            }

            strictCcdOverride = policy.Mode switch
            {
                TanCcdMode.Always => StrictCcdOverride2D.ForceOn,
                TanCcdMode.Never => StrictCcdOverride2D.ForceOff,
                _ => StrictCcdOverride2D.Auto
            };
            strictCcdThresholdRatio = Mathf.Max(0.01f, policy.MotionToDiameterRatio);
        }



        public bool TryGetSnapshot(in TanKey key, out TanSnapshot snapshot)
        {
            if (IsAlive(in key) && records[key].Element.TryGetSnapshot(out ElementSnapshot element))
            {
                TanRecord record = records[key];
                TanDefinition definition = record.Spawn.Definition;
                OptionalTanFloat lifetime = world.TryGetLifetime(record.Element.Key, out ElementLifetimeFeature feature)
                    ? new OptionalTanFloat(feature.RemainingUnits)
                    : default;
                snapshot = new TanSnapshot(
                    in key,
                    BackendKind,
                    new Vector2(element.PreviousPosition.x, element.PreviousPosition.y),
                    new Vector2(element.Position.x, element.Position.y),
                    new Vector2(element.Velocity.x, element.Velocity.y),
                    element.Radius,
                    lifetime,
                    definition.TeamId,
                    element.VisualId,
                    new Vector2(element.Scale.x, element.Scale.y),
                    element.Color,
                    math.degrees(element.RotationRadians));
                return true;
            }

            snapshot = default;
            return false;
        }
    }
}
