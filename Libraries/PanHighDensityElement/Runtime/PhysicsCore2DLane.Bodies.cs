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
        private readonly Dictionary<ElementKey, PhysicsBody> bodies = new Dictionary<ElementKey, PhysicsBody>();
        private readonly HashSet<ElementKey> areaElements = new HashSet<ElementKey>();
        private readonly HashSet<ElementKey> dynamicElements = new HashSet<ElementKey>();



        internal void Register(in NativeElementState state)
        {
            if (!RequiresBody(state.Capabilities) || !IsValid || bodies.ContainsKey(state.Key)) { return; }

            PhysicsBody body = world.CreateBody(CreateBodyDefinition(in state));
            ConfigureBody(body, in state);
            bodies.Add(state.Key, body);
            if ((state.Capabilities & ElementCapabilities.AreaSensor2D) != 0) { areaElements.Add(state.Key); }
            if ((state.Capabilities & ElementCapabilities.DynamicBody2D) != 0) { dynamicElements.Add(state.Key); }
        }



        internal void RegisterBatch(NativeArray<NativeElementState> states)
        {
            if (!IsValid) { throw new InvalidOperationException("Physics Core 2D world가 유효하지 않습니다."); }
            if (states.Length == 0) { return; }

            var definitions = new NativeArray<PhysicsBodyDefinition>(
                states.Length,
                Allocator.Temp,
                NativeArrayOptions.UninitializedMemory);
            try
            {
                for (int i = 0; i < states.Length; i++)
                {
                    NativeElementState state = states[i];
                    if (!RequiresBody(state.Capabilities) || bodies.ContainsKey(state.Key))
                    {
                        throw new InvalidOperationException($"등록할 수 없는 Physics Core body입니다: {state.Key}");
                    }
                    definitions[i] = CreateBodyDefinition(in state);
                }

                using NativeArray<PhysicsBody> createdBodies = world.CreateBodyBatch(
                    definitions.AsReadOnlySpan(),
                    Allocator.Temp);
                try
                {
                    for (int i = 0; i < createdBodies.Length; i++)
                    {
                        NativeElementState state = states[i];
                        PhysicsBody body = createdBodies[i];
                        ConfigureBody(body, in state);
                        bodies.Add(state.Key, body);
                        if ((state.Capabilities & ElementCapabilities.AreaSensor2D) != 0)
                        {
                            areaElements.Add(state.Key);
                        }
                        if ((state.Capabilities & ElementCapabilities.DynamicBody2D) != 0)
                        {
                            dynamicElements.Add(state.Key);
                        }
                    }
                    BodyBatchCreateCount++;
                }
                catch
                {
                    for (int i = 0; i < states.Length; i++)
                    {
                        bodies.Remove(states[i].Key);
                        areaElements.Remove(states[i].Key);
                        dynamicElements.Remove(states[i].Key);
                    }
                    PhysicsWorld.DestroyBodyBatch(createdBodies.AsReadOnlySpan());
                    throw;
                }
            }
            finally
            {
                definitions.Dispose();
            }
        }



        public bool TryGetBody(ElementKey key, out PhysicsBody body)
        {
            return bodies.TryGetValue(key, out body) && body.isValid;
        }



        internal void ApplyState(in NativeElementState state)
        {
            if (!TryGetBody(state.Key, out PhysicsBody body)) { return; }

            body.position = new Vector2(state.Position.x, state.Position.y);
            //? Kinematic/passive body는 Element pose의 mirror이며, PhysicsCore가 속도를 다시 적분하면 안 됩니다.
            body.linearVelocity = Vector2.zero;
            body.linearDamping = state.LinearDamping;
        }



        internal void ApplyKinematicStates(NativeList<NativeElementState> states)
        {
            for (int i = 0; i < states.Length; i++)
            {
                NativeElementState state = states[i];
                if (state.PhysicsBodyMode != PhysicsCoreBodyMode.Dynamic) { ApplyState(in state); }
            }
        }



        internal void ApplyKinematicQueryTargetStates(NativeList<NativeElementState> states)
        {
            for (int i = 0; i < states.Length; i++)
            {
                NativeElementState state = states[i];
                if ((state.Capabilities & ElementCapabilities.QueryTarget2D) == 0 ||
                    state.PhysicsBodyMode == PhysicsCoreBodyMode.Dynamic)
                {
                    continue;
                }

                ApplyState(in state);
            }
        }



        internal void SynchronizeDynamicStates(NativeList<NativeElementState> states)
        {
            for (int i = 0; i < states.Length; i++)
            {
                NativeElementState state = states[i];
                if (state.PhysicsBodyMode != PhysicsCoreBodyMode.Dynamic ||
                    !TryGetBody(state.Key, out PhysicsBody body))
                {
                    continue;
                }

                Vector2 position = body.position;
                Vector2 velocity = body.linearVelocity;
                state.PreviousPosition = state.Position;
                state.Position = new float2(position.x, position.y);
                state.Velocity = new float2(velocity.x, velocity.y);
                states[i] = state;
            }
        }



        internal void Unregister(ElementKey key)
        {
            if (!bodies.TryGetValue(key, out PhysicsBody body)) { return; }

            body.Destroy();
            bodies.Remove(key);
            areaElements.Remove(key);
            dynamicElements.Remove(key);
            RemoveAreaPairs(key);
            RemoveDynamicPairs(key);
        }



        internal void UnregisterBatch(ReadOnlySpan<ElementKey> keys)
        {
            if (!IsValid || keys.Length == 0) { return; }

            using var removedBodies = new NativeList<PhysicsBody>(keys.Length, Allocator.Temp);
            for (int i = 0; i < keys.Length; i++)
            {
                ElementKey key = keys[i];
                if (!bodies.TryGetValue(key, out PhysicsBody body)) { continue; }

                bodies.Remove(key);
                areaElements.Remove(key);
                dynamicElements.Remove(key);
                RemoveAreaPairs(key);
                RemoveDynamicPairs(key);
                if (body.isValid) { removedBodies.Add(body); }
            }

            if (removedBodies.Length == 0) { return; }
            PhysicsWorld.DestroyBodyBatch(removedBodies.AsArray().AsReadOnlySpan());
            BodyBatchDestroyCount++;
        }



        private static bool RequiresBody(ElementCapabilities capabilities) =>
            (capabilities & (ElementCapabilities.AreaSensor2D |
                ElementCapabilities.DynamicBody2D |
                ElementCapabilities.QueryTarget2D)) != 0;



        private static PhysicsBodyDefinition CreateBodyDefinition(in NativeElementState state)
        {
            PhysicsBodyDefinition definition = PhysicsBodyDefinition.defaultDefinition;
            bool isDynamic = (state.Capabilities & ElementCapabilities.DynamicBody2D) != 0 &&
                state.PhysicsBodyMode == PhysicsCoreBodyMode.Dynamic;
            definition.type = isDynamic
                ? PhysicsBody.BodyType.Dynamic
                : PhysicsBody.BodyType.Kinematic;
            definition.position = new Vector2(state.Position.x, state.Position.y);
            definition.linearVelocity = isDynamic
                ? new Vector2(state.Velocity.x, state.Velocity.y)
                : Vector2.zero;
            definition.linearDamping = state.LinearDamping;
            if ((state.Capabilities & ElementCapabilities.DynamicBody2D) != 0)
            {
                definition.collisionThreshold = 0f;
            }
            if (state.HasPhysicsMaterial != 0)
            {
                definition.gravityScale = state.PhysicsMaterial.GravityScale;
            }
            definition.fastCollisionsAllowed = true;
            definition.transformWriteMode = PhysicsBody.TransformWriteMode.Off;
            return definition;
        }



        private static void ConfigureBody(PhysicsBody body, in NativeElementState state)
        {
            ulong packedKey = PackKey(state.Key);
            body.userData = new PhysicsUserData { int64Value = packedKey };

            PhysicsShapeDefinition shapeDefinition = PhysicsShapeDefinition.defaultDefinition;
            bool producesContactEvents =
                (state.Capabilities & (ElementCapabilities.AreaSensor2D | ElementCapabilities.DynamicBody2D)) != 0;
            bool participatesInTriggerEvents =
                (state.Capabilities & (ElementCapabilities.AreaSensor2D |
                    ElementCapabilities.DynamicBody2D |
                    ElementCapabilities.QueryTarget2D)) != 0;
            shapeDefinition.contactEvents = producesContactEvents;
            shapeDefinition.hitEvents = (state.Capabilities & ElementCapabilities.DynamicBody2D) != 0;
            shapeDefinition.triggerEvents = participatesInTriggerEvents;
            shapeDefinition.isTrigger = state.PhysicsIsTrigger != 0 ||
                (state.Capabilities & ElementCapabilities.AreaSensor2D) != 0;
            PhysicsShape.ContactFilter contactFilter = PhysicsShape.ContactFilter.defaultFilter;
            contactFilter.categories = new PhysicsMask { bitMask = state.PhysicsCategoryMask };
            contactFilter.contacts = new PhysicsMask { bitMask = state.InteractionLayerMask };
            shapeDefinition.contactFilter = contactFilter;
            if (state.HasPhysicsMaterial != 0)
            {
                shapeDefinition.density = state.PhysicsMaterial.Density;
                PhysicsShape.SurfaceMaterial material = PhysicsShape.SurfaceMaterial.defaultMaterial;
                material.friction = state.PhysicsMaterial.Friction;
                material.bounciness = state.PhysicsMaterial.Bounciness;
                shapeDefinition.surfaceMaterial = material;
            }

            PhysicsShape shape;
            if (state.PhysicsShape == PhysicsCoreShape2D.Box)
            {
                Vector2 size = new Vector2(
                    Mathf.Max(0.0001f, state.Scale.x * 2f * state.Radius),
                    Mathf.Max(0.0001f, state.Scale.y * 2f * state.Radius));
                shape = body.CreateShape(PolygonGeometry.CreateBox(size, 0f, true), shapeDefinition);
            }
            else
            {
                CircleGeometry circle = new CircleGeometry
                {
                    center = Vector2.zero,
                    radius = Mathf.Max(0.0001f, state.Radius)
                };
                shape = body.CreateShape(circle, shapeDefinition);
            }

            shape.userData = new PhysicsUserData { int64Value = packedKey };
        }
    }
}
