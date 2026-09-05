using Pan.HighDensityElement;
using Unity.Mathematics;
using UnityEngine;



namespace Pan.Tan.Element
{
    /// <summary>
    /// backend 중립 Tan 명령을 generation-safe Element 명령으로 변환합니다.
    /// </summary>
    public sealed partial class ElementTanRuntime
    {
        public bool TrySubmit(in TanKey key, in TanCommand command)
        {
            if (!IsAlive(in key) || !records.TryGetValue(key, out TanRecord record)) { return false; }

            switch (command.Type)
            {
                case TanCommandType.Despawn:
                    return record.Element.TrySubmit(new DespawnElement());
                case TanCommandType.SetPosition2D:
                    return record.Element.TrySubmit(new SetElementPosition2D(
                        new float2(command.VectorValue.x, command.VectorValue.y)));
                case TanCommandType.SetVelocity2D:
                    return record.Element.TrySubmit(new SetElementVelocity2D(
                        new float2(command.VectorValue.x, command.VectorValue.y)));
                case TanCommandType.SetRemainingLifetime:
                    if (!world.TryGetLifetime(record.Element.Key, out ElementLifetimeFeature lifetime))
                    {
                        return false;
                    }

                    var updatedLifetime = new ElementLifetimeFeature(
                        command.FloatValue,
                        lifetime.Schedule,
                        lifetime.UseLocalClock);
                    return world.TrySetLifetime(record.Element.Key, in updatedLifetime);
                case TanCommandType.RequestStrictCcd2D:
                    return record.Element.TrySubmit(new RequestElementStrictCcd2D(command.IntValue));
                case TanCommandType.MarkTeleported2D:
                    return record.Element.TrySubmit(new MarkElementTeleported2D());
                case TanCommandType.ConfigureLifetime:
                    if (!command.LifetimeValue.Enabled)
                    {
                        return !world.TryGetLifetime(record.Element.Key, out _) ||
                               world.RemoveLifetime(record.Element.Key);
                    }

                    var configuredLifetime = new ElementLifetimeFeature(
                        command.LifetimeValue.RemainingUnits,
                        command.LifetimeValue.Schedule,
                        command.LifetimeValue.UseLocalClock);
                    return world.TrySetLifetime(record.Element.Key, in configuredLifetime);
                case TanCommandType.RemoveLifetime:
                    return !world.TryGetLifetime(record.Element.Key, out _) ||
                           world.RemoveLifetime(record.Element.Key);
                case TanCommandType.ConfigureLocalClock:
                    if (float.IsNaN(command.FloatValue) || float.IsInfinity(command.FloatValue)) { return false; }

                    var localClock = new ElementLocalClock(command.FloatValue, command.IntValue != 0);
                    return world.TrySetLocalClock(record.Element.Key, in localClock);
                case TanCommandType.RemoveLocalClock:
                    return !world.TryGetLocalClock(record.Element.Key, out _) ||
                           world.RemoveLocalClock(record.Element.Key);
                case TanCommandType.SetVisualId:
                    return record.Element.TrySubmit(new SetElementVisualId(command.IntValue));
                case TanCommandType.SetColor:
                    return record.Element.TrySubmit(new SetElementColor(command.ColorValue));
                case TanCommandType.SetVisualScale2D:
                    if (!math.all(math.isfinite(new float2(command.VectorValue.x, command.VectorValue.y))))
                    {
                        return false;
                    }

                    return record.Element.TrySubmit(new SetElementScale2D(
                        new float2(command.VectorValue.x, command.VectorValue.y)));
                case TanCommandType.SetRotation2D:
                    if (!float.IsFinite(command.FloatValue)) { return false; }
                    return record.Element.TrySubmit(new SetElementRotation2D(math.radians(command.FloatValue)));
                case TanCommandType.ConfigureVisualOrientation:
                    TanVisualOrientationDefinition orientation = command.VisualOrientationValue;
                    return TryConfigureVisualOrientation(in key, record, in orientation);
                case TanCommandType.RemoveVisualOrientation:
                    return TryRemoveVisualOrientation(in key, record);
                case TanCommandType.ConfigurePrimaryMotion:
                    TanPrimaryMotionDefinition primary = command.PrimaryMotionValue;
                    return TryConfigurePrimaryMotion(in key, record, in primary);
                case TanCommandType.RemovePrimaryMotion:
                    return TryRemovePrimaryMotion(in key, record);
                case TanCommandType.UpdateHomingTargetPosition2D:
                    return TryUpdateHomingTarget(
                        in key,
                        record,
                        command.VectorValue,
                        command.IntValue != 0);
                case TanCommandType.ConfigureWaveMotion:
                    TanWaveMotionDefinition wave = command.WaveMotionValue;
                    return TryConfigureWaveMotion(in key, record, in wave);
                case TanCommandType.RemoveWaveMotion:
                    return TryRemoveWaveMotion(in key, record);
                case TanCommandType.ConfigureBoundary2D:
                    TanBoundaryDefinition boundary = command.BoundaryValue;
                    return TryConfigureBoundary(in key, record, in boundary);
                case TanCommandType.UpdateBoundaryBounds2D:
                    return TryUpdateBoundaryBounds(in key, record, command.BoundaryValue.Bounds);
                case TanCommandType.RemoveBoundary2D:
                    return TryRemoveBoundary(in key, record);
                default:
                    return false;
            }
        }



        private bool TryConfigureVisualOrientation(
            in TanKey key,
            TanRecord record,
            in TanVisualOrientationDefinition definition)
        {
            if (!definition.Enabled) { return TryRemoveVisualOrientation(in key, record); }
            ElementVisualOrientationFeature feature = ToElementVisualOrientation(in definition);
            if (!record.Element.TrySubmit(new ConfigureElementVisualOrientation2D(in feature))) { return false; }
            record.VisualOrientation = definition;
            records[key] = record;
            return true;
        }



        private bool TryRemoveVisualOrientation(in TanKey key, TanRecord record)
        {
            if (!record.Element.TrySubmit(new RemoveElementVisualOrientation2D())) { return false; }
            record.VisualOrientation = default;
            records[key] = record;
            return true;
        }



        private bool TryConfigurePrimaryMotion(
            in TanKey key,
            TanRecord record,
            in TanPrimaryMotionDefinition definition)
        {
            if (!definition.Enabled) { return TryRemovePrimaryMotion(in key, record); }
            if (!TryCreateElementPrimaryMotion(in definition, out ElementDirectionalMotionFeature feature) ||
                !record.Element.TrySubmit(new ConfigureElementDirectionalMotion2D(in feature)))
            {
                return false;
            }

            record.PrimaryMotion = definition;
            records[key] = record;
            return true;
        }



        private bool TryRemovePrimaryMotion(in TanKey key, TanRecord record)
        {
            if (!record.Element.TrySubmit(new RemoveElementDirectionalMotion2D())) { return false; }
            record.PrimaryMotion = default;
            records[key] = record;
            return true;
        }



        private bool TryUpdateHomingTarget(
            in TanKey key,
            TanRecord record,
            Vector2 position,
            bool targetAlive)
        {
            TanPrimaryMotionDefinition current = record.PrimaryMotion;
            if (current.Mode != TanPrimaryMotionMode.Homing) { return false; }

            if (!targetAlive)
            {
                return current.TargetLossPolicy switch
                {
                    TanHomingTargetLossPolicy.TrackLastPosition => true,
                    TanHomingTargetLossPolicy.Despawn => record.Element.TrySubmit(new DespawnElement()),
                    _ => TryRemovePrimaryMotion(in key, record)
                };
            }

            if (!float.IsFinite(position.x) || !float.IsFinite(position.y)) { return false; }
            TanHomingTarget updatedTarget = current.HomingTarget.Mode == TanHomingTargetMode.Target
                ? TanHomingTarget.ForTarget(current.HomingTarget.Target, position)
                : TanHomingTarget.ForFixedPoint(position);
            TanPrimaryMotionDefinition updated = TanPrimaryMotionDefinition.Homing(
                in updatedTarget,
                current.TurnDegreesPerSecond,
                current.TargetLossPolicy);
            return TryConfigurePrimaryMotion(in key, record, in updated);
        }



        private bool TryConfigureWaveMotion(
            in TanKey key,
            TanRecord record,
            in TanWaveMotionDefinition definition)
        {
            if (!definition.Enabled) { return TryRemoveWaveMotion(in key, record); }
            ElementWaveMotionFeature feature = ToElementWaveMotion(in definition);
            if (!record.Element.TrySubmit(new ConfigureElementWaveMotion2D(in feature))) { return false; }
            record.WaveMotion = definition;
            records[key] = record;
            return true;
        }



        private bool TryRemoveWaveMotion(in TanKey key, TanRecord record)
        {
            if (!record.Element.TrySubmit(new RemoveElementWaveMotion2D())) { return false; }
            record.WaveMotion = default;
            records[key] = record;
            return true;
        }



        private bool TryConfigureBoundary(
            in TanKey key,
            TanRecord record,
            in TanBoundaryDefinition definition)
        {
            if (!definition.Enabled) { return TryRemoveBoundary(in key, record); }
            SetElementBoundaryBounds(in definition);
            ElementBoundaryFeature feature = ToElementBoundary(in definition);
            if (!record.Element.TrySubmit(new ConfigureElementBoundary2D(in feature))) { return false; }
            record.Boundary = definition;
            records[key] = record;
            return true;
        }



        private bool TryUpdateBoundaryBounds(in TanKey key, TanRecord record, Rect bounds)
        {
            if (!record.Boundary.Enabled) { return false; }
            var updated = new TanBoundaryDefinition(
                record.Boundary.Mode,
                bounds,
                record.Boundary.Margin,
                record.Boundary.RequireEntered);
            SetElementBoundaryBounds(in updated);
            record.Boundary = updated;
            records[key] = record;
            return true;
        }



        private bool TryRemoveBoundary(in TanKey key, TanRecord record)
        {
            if (!record.Element.TrySubmit(new RemoveElementBoundary2D())) { return false; }
            record.Boundary = default;
            records[key] = record;
            return true;
        }
    }
}
