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



        public ElementSpawnResult Spawn(in ElementSpawnBuilder builder)
        {
            if (disposed) { return new ElementSpawnResult(ElementSpawnStatus.WorldDisposed, default); }
            if (!builder.Archetype.IsValid)
            {
                return new ElementSpawnResult(ElementSpawnStatus.InvalidArchetype, default);
            }
            ElementKey key = AllocateKey(builder.Archetype.Lane);
            NativeElementState state = CreateState(key, in builder);
            AddState(builder.Archetype.Lane, state, ResolveInitialLifetime(in builder));

            ElementHandle handle = new ElementHandle(this, key);
            ElementFact fact = new ElementFact
            {
                Type = ElementFactType.Spawned,
                Element = key,
                TargetId = -1,
                BridgeTargetId = -1,
                Position = builder.Position,
                FixedStepIndex = fixedStepIndex
            };
            DispatchFact(in fact);
            return new ElementSpawnResult(ElementSpawnStatus.Success, handle);
        }





        public int SpawnBatch(
            ReadOnlySpan<ElementSpawnBuilder> builders,
            Span<ElementSpawnResult> results)
        {
            if (results.Length < builders.Length)
            {
                throw new ArgumentException("결과 span은 builder 수 이상이어야 합니다.", nameof(results));
            }

            using var pendingStates = new NativeList<NativeElementState>(builders.Length, Allocator.Temp);
            using var pendingLanes = new NativeList<ElementLane>(builders.Length, Allocator.Temp);
            using var pendingResultIndices = new NativeList<int>(builders.Length, Allocator.Temp);
            using var pendingPhysicsStates = new NativeList<NativeElementState>(builders.Length, Allocator.Temp);

            for (int i = 0; i < builders.Length; i++)
            {
                results[i] = default;
                if (disposed)
                {
                    results[i] = new ElementSpawnResult(ElementSpawnStatus.WorldDisposed, default);
                    continue;
                }

                ElementSpawnBuilder builder = builders[i];
                if (!builder.Archetype.IsValid)
                {
                    results[i] = new ElementSpawnResult(ElementSpawnStatus.InvalidArchetype, default);
                    continue;
                }
                ElementKey key = AllocateKey(builder.Archetype.Lane);
                NativeElementState state = CreateState(key, in builder);
                pendingStates.Add(state);
                pendingLanes.Add(builder.Archetype.Lane);
                pendingResultIndices.Add(i);
                if (RequiresPhysicsBody(state.Capabilities))
                {
                    pendingPhysicsStates.Add(state);
                }
            }

            try
            {
                if (pendingPhysicsStates.Length > 0)
                {
                    physicsCoreLane.RegisterBatch(pendingPhysicsStates.AsArray());
                }

                for (int i = 0; i < pendingStates.Length; i++)
                {
                    NativeElementState state = pendingStates[i];
                    ElementLane lane = pendingLanes[i];
                    AddState(
                        lane,
                        state,
                        ResolveInitialLifetime(in builders[pendingResultIndices[i]]),
                        registerPhysicsBody: false);
                    results[pendingResultIndices[i]] = new ElementSpawnResult(
                        ElementSpawnStatus.Success,
                        new ElementHandle(this, state.Key));
                }
            }
            catch
            {
                for (int i = 0; i < pendingStates.Length; i++)
                {
                    ReleaseReservedKey(pendingStates[i].Key);
                }

                throw;
            }

            for (int i = 0; i < pendingStates.Length; i++)
            {
                NativeElementState state = pendingStates[i];
                ElementFact fact = new ElementFact
                {
                    Type = ElementFactType.Spawned,
                    Element = state.Key,
                    TargetId = -1,
                    BridgeTargetId = -1,
                    Position = state.Position,
                    FixedStepIndex = fixedStepIndex
                };
                DispatchFact(in fact);
            }

            return pendingStates.Length;
        }





        public bool IsAlive(ElementKey key)
        {
            return TryGetRegistryEntry(key, out ElementRegistryEntry entry) &&
                entry.Lifecycle == ElementLifecycle.Alive;
        }





        public bool TryGetLifecycle(ElementKey key, out ElementLifecycle lifecycle)
        {
            if (TryGetRegistryEntry(key, out ElementRegistryEntry entry))
            {
                lifecycle = entry.Lifecycle;
                return true;
            }

            lifecycle = ElementLifecycle.Invalid;
            return false;
        }





        public bool TryDespawnImmediately(ElementKey key)
        {
            if (disposed || !TryGetRegistryEntry(key, out ElementRegistryEntry entry) ||
                entry.Lifecycle != ElementLifecycle.Alive)
            {
                return false;
            }

            if (dispatchDepth > 0)
            {
                return TrySubmit(new ElementCommand(key, ElementCommandType.Despawn, default));
            }

            ApplyDespawn(key, entry);
            return true;
        }





        private ElementKey AllocateKey(ElementLane lane)
        {
            int slot;
            uint generation;

            if (freeSlots.Length > 0)
            {
                int lastIndex = freeSlots.Length - 1;
                slot = freeSlots[lastIndex];
                freeSlots.RemoveAt(lastIndex);
                ElementRegistryEntry recycled = registry[slot];
                generation = recycled.Generation;
            }
            else
            {
                slot = registry.Length;
                generation = 1;
                registry.Add(default);
            }

            registry[slot] = new ElementRegistryEntry
            {
                Generation = generation,
                DenseIndex = -1,
                PoseDenseIndex = -1,
                Lane = lane,
                Lifecycle = ElementLifecycle.Reserved
            };
            return new ElementKey(WorldId, slot, generation);
        }





        private static NativeElementState CreateState(ElementKey key, in ElementSpawnBuilder builder)
        {
            return new NativeElementState
            {
                Key = key,
                Capabilities = builder.Archetype.Capabilities,
                Lifecycle = ElementLifecycle.Alive,
                PreviousPosition = builder.Position,
                Position = builder.Position,
                PreviousRotationRadians = builder.RotationRadians,
                RotationRadians = builder.RotationRadians,
                Velocity = builder.Velocity,
                Acceleration = builder.Acceleration,
                LinearDamping = builder.LinearDamping,
                PreviousScale = builder.Scale,
                Scale = builder.Scale,
                Color = builder.Color,
                Radius = builder.Archetype.Radius,
                PeriodicInterval = builder.Archetype.PeriodicInterval,
                PeriodicRemaining = builder.Archetype.PeriodicInterval,
                VisualId = builder.Archetype.VisualId,
                OwnerId = builder.OwnerId,
                TeamId = builder.TeamId,
                PhysicsCategoryMask = builder.PhysicsCategoryMask,
                InteractionLayerMask = builder.InteractionLayerMask,
                IncludeTriggers = builder.IncludeTriggers,
                PhysicsMaterial = builder.PhysicsMaterial,
                Penetrating = builder.Archetype.Penetrating ? (byte)1 : (byte)0,
                PhysicsBodyMode = builder.Archetype.PhysicsBodyMode,
                PhysicsShape = builder.Archetype.PhysicsShape,
                PhysicsIsTrigger = builder.Archetype.PhysicsIsTrigger ? (byte)1 : (byte)0,
                HasPhysicsMaterial = builder.HasPhysicsMaterial,
                StrictCcdOverride = builder.StrictCcdOverride,
                StrictCcdThresholdRatio = math.max(0.01f, builder.StrictCcdThresholdRatio),
                LocalTimeScale = 1f
            };
        }





        private static float ResolveInitialLifetime(in ElementSpawnBuilder builder) =>
            builder.HasLifetimeOverride != 0
                ? builder.LifetimeOverride
                : builder.Archetype.Lifetime;





        private void AddState(
            ElementLane lane,
            NativeElementState state,
            float initialLifetime,
            bool registerPhysicsBody = true)
        {
            int denseIndex;
            switch (lane)
            {
                case ElementLane.QuerySprite2D:
                    denseIndex = kinematicStates.Length;
                    kinematicStates.Add(state);
                    if (registerPhysicsBody && RequiresPhysicsBody(state.Capabilities))
                    {
                        physicsCoreLane.Register(in state);
                    }
                    break;
                case ElementLane.AreaSensorSprite2D:
                    denseIndex = areaStates.Length;
                    areaStates.Add(state);
                    if (registerPhysicsBody) { physicsCoreLane.Register(in state); }
                    break;
                case ElementLane.DynamicBodySprite2D:
                    denseIndex = physicsStates.Length;
                    physicsStates.Add(state);
                    if (registerPhysicsBody) { physicsCoreLane.Register(in state); }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lane));
            }

            ElementRegistryEntry entry = registry[state.Key.Slot];
            entry.DenseIndex = denseIndex;
            entry.PoseDenseIndex = poseStates.Length;
            entry.Lifecycle = ElementLifecycle.Alive;
            registry[state.Key.Slot] = entry;

            poseStates.Add(new NativeElementPoseState
            {
                Key = state.Key,
                PreviousPosition = state.PreviousPosition,
                Position = state.Position,
                PreviousRotationRadians = state.PreviousRotationRadians,
                RotationRadians = state.RotationRadians,
                PreviousScale = state.PreviousScale,
                Scale = state.Scale
            });
            if ((state.Capabilities & ElementCapabilities.Lifetime) != 0 && initialLifetime > 0f)
            {
                var lifetime = new NativeElementLifetimeState
                {
                    Feature = new ElementLifetimeFeature(
                        initialLifetime,
                        ElementUpdateSchedule.UpdateSeconds()),
                    AdvanceWithFixedTick = 1
                };
                lifetimeFeatures.TryAdd(state.Key, in lifetime);
            }
            IncrementStructuralRevision();
        }





        private void DespawnExpiredStates()
        {
            DespawnExpiredStates(kinematicStates);
            DespawnExpiredStates(areaStates);
            DespawnExpiredPhysicsStates();
        }





        private void DespawnExpiredPhysicsStates()
        {
            if (physicsCoreLane == null) { return; }

            using var physicsDespawnKeys = new NativeList<ElementKey>(Allocator.Temp);
            for (int i = physicsStates.Length - 1; i >= 0; i--)
            {
                NativeElementState state = physicsStates[i];
                if (state.Lifecycle != ElementLifecycle.DespawnPending) { continue; }
                if (!TryGetRegistryEntry(state.Key, out ElementRegistryEntry entry)) { continue; }

                ApplyDespawn(state.Key, entry, unregisterPhysicsBody: false);
                physicsDespawnKeys.Add(state.Key);
            }

            if (physicsDespawnKeys.Length > 0)
            {
                physicsCoreLane.UnregisterBatch(physicsDespawnKeys.AsArray().AsReadOnlySpan());
            }
        }





        private void DespawnExpiredStates(NativeList<NativeElementState> states)
        {
            for (int i = states.Length - 1; i >= 0; i--)
            {
                NativeElementState state = states[i];
                if (state.Lifecycle != ElementLifecycle.DespawnPending) { continue; }
                if (TryGetRegistryEntry(state.Key, out ElementRegistryEntry entry))
                {
                    ApplyDespawn(state.Key, entry);
                }
            }
        }





        private void ApplyDespawn(
            ElementKey key,
            ElementRegistryEntry entry,
            bool unregisterPhysicsBody = true)
        {
            if (!TryGetState(key, out NativeElementState state)) { return; }

            ElementFact fact = new ElementFact
            {
                Type = ElementFactType.Despawned,
                Element = key,
                TargetId = -1,
                BridgeTargetId = -1,
                Position = state.Position,
                FixedStepIndex = fixedStepIndex
            };
            DispatchFact(in fact);

            lifetimeFeatures.Remove(key);
            localClocks.Remove(key);
            directionalMotionFeatures.Remove(key);
            waveMotionFeatures.Remove(key);
            visualOrientationFeatures.Remove(key);
            boundaryFeatures.Remove(key);
            RemoveStateAtSwapBack(entry.Lane, entry.DenseIndex);
            RemovePoseAtSwapBack(entry.PoseDenseIndex);
            if (RequiresPhysicsBody(state.Capabilities) && unregisterPhysicsBody)
            {
                physicsCoreLane.Unregister(key);
            }

            entry.DenseIndex = -1;
            entry.PoseDenseIndex = -1;
            entry.Lifecycle = ElementLifecycle.Despawned;
            entry.Generation = entry.Generation == uint.MaxValue ? 1u : entry.Generation + 1u;
            registry[key.Slot] = entry;
            freeSlots.Add(key.Slot);
            IncrementStructuralRevision();
        }





        private void ReleaseReservedKey(ElementKey key)
        {
            if (key.WorldId != WorldId || key.Slot < 0 || key.Slot >= registry.Length) { return; }

            ElementRegistryEntry entry = registry[key.Slot];
            if (entry.Generation != key.Generation || entry.Lifecycle != ElementLifecycle.Reserved) { return; }

            entry.DenseIndex = -1;
            entry.Lifecycle = ElementLifecycle.Despawned;
            entry.Generation = entry.Generation == uint.MaxValue ? 1u : entry.Generation + 1u;
            registry[key.Slot] = entry;
            freeSlots.Add(key.Slot);
        }





        private void DespawnAll(NativeList<NativeElementState> states)
        {
            while (states.Length > 0)
            {
                ElementKey key = states[states.Length - 1].Key;
                if (!TryGetRegistryEntry(key, out ElementRegistryEntry entry)) { break; }
                ApplyDespawn(key, entry);
            }
        }
    }
}
