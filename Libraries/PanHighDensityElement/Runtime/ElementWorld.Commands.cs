using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;



namespace Pan.HighDensityElement
{
    public sealed partial class ElementWorld
    {



        public bool TrySubmit(in ElementCommand command)
        {
            if (disposed || !IsAlive(command.Target)) { return false; }

            if (dispatchDepth > 0) { postEventCommands.Enqueue(command); }
            else { commands.Enqueue(command); }
            return true;
        }





        public void FlushCommands()
        {
            ThrowIfDisposed();
            lastCommandCount = DrainCommands(commands) + DrainCommands(postEventCommands);
        }





        private int DrainCommands(NativeQueue<ElementCommand> source)
        {
            NativeList<ElementKey> physicsDespawnKeys = default;
            int appliedCount = 0;
            try
            {
                while (source.TryDequeue(out ElementCommand command))
                {
                    if (!TryGetRegistryEntry(command.Target, out ElementRegistryEntry entry) ||
                        entry.Lifecycle != ElementLifecycle.Alive)
                    {
                        continue;
                    }

                    appliedCount++;

                    if (command.Type == ElementCommandType.Despawn)
                    {
                        bool batchPhysicsDespawn = entry.Lane == ElementLane.DynamicBodySprite2D ||
                            entry.Lane == ElementLane.AreaSensorSprite2D;
                        ApplyDespawn(command.Target, entry, unregisterPhysicsBody: !batchPhysicsDespawn);
                        if (batchPhysicsDespawn)
                        {
                            if (!physicsDespawnKeys.IsCreated)
                            {
                                physicsDespawnKeys = new NativeList<ElementKey>(Allocator.Temp);
                            }
                            physicsDespawnKeys.Add(command.Target);
                        }
                        continue;
                    }

                    if (!TryGetState(command.Target, out NativeElementState state)) { continue; }

                    switch (command.Type)
                    {
                        case ElementCommandType.SetPosition2D:
                            state.PreviousPosition = command.Value;
                            state.Position = command.Value;
                            break;
                        case ElementCommandType.SetVelocity2D:
                            state.Velocity = command.Value;
                            break;
                        case ElementCommandType.SetVisualId:
                            state.VisualId = command.IntValue;
                            break;
                        case ElementCommandType.SetScale2D:
                            state.Scale = command.Value;
                            break;
                        case ElementCommandType.SetRotation2D:
                            if (math.isfinite(command.Value.x))
                            {
                                state.PreviousRotationRadians = command.Value.x;
                                state.RotationRadians = command.Value.x;
                            }
                            break;
                        case ElementCommandType.SetColor:
                            state.Color = SetElementColor.UnpackColor(command.IntValue);
                            break;
                        case ElementCommandType.SetRemainingLifetime:
                            if (lifetimeFeatures.TryGet(
                                    command.Target,
                                    out NativeElementLifetimeState lifetime))
                            {
                                lifetime.Feature = lifetime.Feature.WithRemainingUnits(
                                    math.max(0f, command.Value.x));
                                lifetime.PendingUnits = 0f;
                                lifetimeFeatures.TrySet(command.Target, in lifetime);
                            }
                            else if ((state.Capabilities & ElementCapabilities.Lifetime) != 0 &&
                                command.Value.x > 0f)
                            {
                                lifetime = new NativeElementLifetimeState
                                {
                                    Feature = new ElementLifetimeFeature(
                                        command.Value.x,
                                        ElementUpdateSchedule.UpdateSeconds()),
                                    AdvanceWithFixedTick = 1
                                };
                                if (lifetimeFeatures.TryAdd(command.Target, in lifetime))
                                {
                                    IncrementStructuralRevision();
                                }
                            }
                            break;
                        case ElementCommandType.SetAcceleration2D:
                            state.Acceleration = command.Value;
                            break;
                        case ElementCommandType.SetLinearDamping:
                            state.LinearDamping = math.max(0f, command.Value.x);
                            break;
                        case ElementCommandType.RequestStrictCcd2D:
                            state.StrictCcdRemainingSteps = (ushort)math.clamp(command.IntValue, 0, ushort.MaxValue);
                            break;
                        case ElementCommandType.MarkTeleported2D:
                            state.TeleportPending = 1;
                            state.PreviousPosition = state.Position;
                            break;
                        case ElementCommandType.Teleport2D:
                            state.PreviousPosition = command.Value;
                            state.Position = command.Value;
                            state.TeleportPending = 1;
                            break;
                        case ElementCommandType.ConfigureDirectionalMotion2D:
                            if (Enum.IsDefined(typeof(ElementDirectionalMotionMode), command.IntValue) &&
                                math.all(math.isfinite(command.Value)) &&
                                math.all(math.isfinite(command.SecondaryValue)) &&
                                command.Value.y >= 0f)
                            {
                                var directional = new ElementDirectionalMotionFeature(
                                    (ElementDirectionalMotionMode)command.IntValue,
                                    command.Value.x,
                                    command.SecondaryValue,
                                    command.Value.y);
                                TrySetDirectionalMotion(command.Target, in directional);
                            }
                            continue;
                        case ElementCommandType.RemoveDirectionalMotion2D:
                            RemoveDirectionalMotion(command.Target);
                            continue;
                        case ElementCommandType.ConfigureWaveMotion2D:
                            if (math.all(math.isfinite(command.Value)) &&
                                math.isfinite(command.SecondaryValue.x))
                            {
                                var wave = new ElementWaveMotionFeature(
                                    command.Value.x,
                                    command.Value.y,
                                    command.SecondaryValue.x);
                                TrySetWaveMotion(command.Target, in wave);
                            }
                            continue;
                        case ElementCommandType.RemoveWaveMotion2D:
                            RemoveWaveMotion(command.Target);
                            continue;
                        case ElementCommandType.ConfigureVisualOrientation2D:
                            if (Enum.IsDefined(typeof(ElementVisualOrientationMode), command.IntValue) &&
                                math.all(math.isfinite(command.Value)))
                            {
                                var orientation = new ElementVisualOrientationFeature(
                                    (ElementVisualOrientationMode)command.IntValue,
                                    command.Value.x,
                                    command.Value.y);
                                TrySetVisualOrientation(command.Target, in orientation);
                            }
                            continue;
                        case ElementCommandType.RemoveVisualOrientation2D:
                            RemoveVisualOrientation(command.Target);
                            continue;
                        case ElementCommandType.ConfigureBoundary2D:
                            if (Enum.IsDefined(typeof(ElementBoundaryMode2D), command.IntValue) &&
                                math.isfinite(command.Value.x) && command.Value.x >= 0f)
                            {
                                var boundary = new ElementBoundaryFeature(
                                    (ElementBoundaryMode2D)command.IntValue,
                                    command.Value.x,
                                    command.SecondaryIntValue != 0);
                                TrySetBoundary(command.Target, in boundary);
                            }
                            continue;
                        case ElementCommandType.RemoveBoundary2D:
                            RemoveBoundary(command.Target);
                            continue;
                    }

                    SetState(entry, state);
                    if (entry.Lane == ElementLane.DynamicBodySprite2D ||
                        entry.Lane == ElementLane.AreaSensorSprite2D)
                    {
                        physicsCoreLane.ApplyState(in state);
                    }
                }
            }
            finally
            {
                if (physicsDespawnKeys.IsCreated)
                {
                    physicsCoreLane.UnregisterBatch(physicsDespawnKeys.AsArray().AsReadOnlySpan());
                    physicsDespawnKeys.Dispose();
                }
            }

            return appliedCount;
        }





        private void SetState(ElementRegistryEntry entry, NativeElementState state)
        {
            NativeList<NativeElementState> states = GetStates(entry.Lane);
            states[entry.DenseIndex] = state;
            SynchronizePoseState(entry, in state);
        }
    }
}
