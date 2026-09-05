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
        private readonly HashSet<AreaPair> activeAreaPairs = new HashSet<AreaPair>();
        private readonly HashSet<AreaPair> enteredAreaPairs = new HashSet<AreaPair>();
        private readonly HashSet<DirectedContactPair> emittedContactPairs = new HashSet<DirectedContactPair>();
        private readonly HashSet<DirectedContactPair> activeDynamicContacts = new HashSet<DirectedContactPair>();
        private readonly HashSet<DirectedContactPair> currentDynamicContacts = new HashSet<DirectedContactPair>();
        private readonly HashSet<DirectedContactPair> activeDynamicTriggerPairs = new HashSet<DirectedContactPair>();
        private readonly HashSet<DirectedContactPair> enteredDynamicTriggerPairs = new HashSet<DirectedContactPair>();
        private readonly List<AreaPair> pairScratch = new List<AreaPair>(64);
        private readonly List<DirectedContactPair> contactPairScratch = new List<DirectedContactPair>(64);



        internal void CollectFacts(
            NativeList<ElementFact> destination,
            ushort substepIndex,
            uint fixedStepIndex)
        {
            if (!IsValid) { return; }

            emittedContactPairs.Clear();
            currentDynamicContacts.Clear();
            ReadOnlySpan<PhysicsEvents.ContactHitEvent> contactHitEvents = world.contactHitEvents;
            for (int i = 0; i < contactHitEvents.Length; i++)
            {
                AddContactFacts(contactHitEvents[i], destination, substepIndex, fixedStepIndex);
            }

            ReadOnlySpan<PhysicsEvents.ContactBeginEvent> contactEvents = world.contactBeginEvents;
            for (int i = 0; i < contactEvents.Length; i++)
            {
                AddContactFacts(contactEvents[i], destination, substepIndex, fixedStepIndex);
            }

            CollectCurrentDynamicContactFacts(destination, substepIndex, fixedStepIndex);
            foreach (DirectedContactPair pair in activeDynamicContacts)
            {
                if (!currentDynamicContacts.Contains(pair) &&
                    TryCreateDirectedPairFact(
                        ElementFactType.Exit,
                        in pair,
                        default,
                        default,
                        ElementFactFlags.None,
                        substepIndex,
                        fixedStepIndex,
                        out ElementFact exitFact))
                {
                    destination.Add(exitFact);
                }
            }
            activeDynamicContacts.Clear();
            foreach (DirectedContactPair pair in currentDynamicContacts) { activeDynamicContacts.Add(pair); }

            enteredAreaPairs.Clear();
            enteredDynamicTriggerPairs.Clear();
            ReadOnlySpan<PhysicsEvents.TriggerBeginEvent> triggerBeginEvents = world.triggerBeginEvents;
            for (int i = 0; i < triggerBeginEvents.Length; i++)
            {
                PhysicsEvents.TriggerBeginEvent trigger = triggerBeginEvents[i];
                if (TryCreateAreaPair(trigger.triggerShape, trigger.visitorShape, out AreaPair pair) &&
                    activeAreaPairs.Add(pair))
                {
                    enteredAreaPairs.Add(pair);
                }

                AddDynamicTriggerPair(trigger.triggerShape, trigger.visitorShape);
                AddDynamicTriggerPair(trigger.visitorShape, trigger.triggerShape);
            }

            ReadOnlySpan<PhysicsEvents.TriggerEndEvent> triggerEndEvents = world.triggerEndEvents;
            for (int i = 0; i < triggerEndEvents.Length; i++)
            {
                PhysicsEvents.TriggerEndEvent trigger = triggerEndEvents[i];
                if (TryCreateAreaPair(trigger.triggerShape, trigger.visitorShape, out AreaPair pair) &&
                    activeAreaPairs.Remove(pair))
                {
                    destination.Add(CreateAreaFact(ElementFactType.Exit, in pair, substepIndex, fixedStepIndex));
                }

                RemoveDynamicTriggerPair(
                    trigger.triggerShape,
                    trigger.visitorShape,
                    destination,
                    substepIndex,
                    fixedStepIndex);
                RemoveDynamicTriggerPair(
                    trigger.visitorShape,
                    trigger.triggerShape,
                    destination,
                    substepIndex,
                    fixedStepIndex);
            }

            foreach (AreaPair pair in activeAreaPairs)
            {
                destination.Add(CreateAreaFact(
                    enteredAreaPairs.Contains(pair) ? ElementFactType.Enter : ElementFactType.Stay,
                    in pair,
                    substepIndex,
                    fixedStepIndex));
            }

            foreach (DirectedContactPair pair in activeDynamicTriggerPairs)
            {
                if (TryCreateDirectedPairFact(
                        enteredDynamicTriggerPairs.Contains(pair) ? ElementFactType.Enter : ElementFactType.Stay,
                        in pair,
                        default,
                        default,
                        ElementFactFlags.None,
                        substepIndex,
                        fixedStepIndex,
                        out ElementFact triggerFact))
                {
                    destination.Add(triggerFact);
                }
            }
        }



        private void AddContactFacts(
            PhysicsEvents.ContactBeginEvent contact,
            NativeList<ElementFact> destination,
            ushort substepIndex,
            uint fixedStepIndex)
        {
            ResolveContactPoint(contact, out float2 point, out float2 normal, out ElementFactFlags flags);
            AddDirectedFact(contact.shapeA, contact.shapeB, point, -normal, flags, destination, substepIndex, fixedStepIndex);
            AddDirectedFact(contact.shapeB, contact.shapeA, point, normal, flags, destination, substepIndex, fixedStepIndex);
        }



        private void AddContactFacts(
            PhysicsEvents.ContactHitEvent contact,
            NativeList<ElementFact> destination,
            ushort substepIndex,
            uint fixedStepIndex)
        {
            float2 point = new float2(contact.point.x, contact.point.y);
            float2 normal = new float2(contact.normal.x, contact.normal.y);
            ElementFactFlags flags = ElementFactGeometry.GetSurfaceFlags(point, normal);
            AddDirectedFact(contact.shapeA, contact.shapeB, point, -normal, flags, destination, substepIndex, fixedStepIndex);
            AddDirectedFact(contact.shapeB, contact.shapeA, point, normal, flags, destination, substepIndex, fixedStepIndex);
        }



        private void CollectCurrentDynamicContactFacts(
            NativeList<ElementFact> destination,
            ushort substepIndex,
            uint fixedStepIndex)
        {
            foreach (ElementKey key in dynamicElements)
            {
                if (!TryGetBody(key, out PhysicsBody body)) { continue; }

                NativeArray<PhysicsShape.Contact> contacts = body.GetContacts(Allocator.Temp);
                try
                {
                    for (int i = 0; i < contacts.Length; i++)
                    {
                        PhysicsShape.Contact contact = contacts[i];
                        ResolveContactPoint(
                            in contact,
                            out float2 point,
                            out float2 normal,
                            out ElementFactFlags flags);
                        AddCurrentDynamicContactFact(
                            contact.shapeA,
                            contact.shapeB,
                            point,
                            -normal,
                            flags,
                            destination,
                            substepIndex,
                            fixedStepIndex);
                        AddCurrentDynamicContactFact(
                            contact.shapeB,
                            contact.shapeA,
                            point,
                            normal,
                            flags,
                            destination,
                            substepIndex,
                            fixedStepIndex);
                    }
                }
                finally
                {
                    if (contacts.IsCreated) { contacts.Dispose(); }
                }
            }
        }



        private void AddCurrentDynamicContactFact(
            PhysicsShape elementShape,
            PhysicsShape counterpartShape,
            float2 point,
            float2 normal,
            ElementFactFlags flags,
            NativeList<ElementFact> destination,
            ushort substepIndex,
            uint fixedStepIndex)
        {
            if (!TryResolveCounterpart(elementShape, worldId, out ElementKey element, out int bridgeTargetId) ||
                bridgeTargetId >= 0 ||
                !dynamicElements.Contains(element))
            {
                return;
            }

            var pair = new DirectedContactPair(
                element,
                GetCounterpartUserData(counterpartShape),
                elementShape.isTrigger || counterpartShape.isTrigger);
            if (!currentDynamicContacts.Add(pair)) { return; }

            bool wasActive = activeDynamicContacts.Contains(pair);
            if (!wasActive)
            {
                AddDirectedFact(
                    elementShape,
                    counterpartShape,
                    point,
                    normal,
                    flags,
                    destination,
                    substepIndex,
                    fixedStepIndex);
            }

            if (TryCreateDirectedPairFact(
                    wasActive ? ElementFactType.Stay : ElementFactType.Enter,
                    in pair,
                    point,
                    normal,
                    flags,
                    substepIndex,
                    fixedStepIndex,
                    out ElementFact phaseFact))
            {
                destination.Add(phaseFact);
            }
        }



        private void AddDirectedFact(
            PhysicsShape elementShape,
            PhysicsShape counterpartShape,
            float2 point,
            float2 normal,
            ElementFactFlags flags,
            NativeList<ElementFact> destination,
            ushort substepIndex,
            uint fixedStepIndex)
        {
            if (!TryResolveCounterpart(elementShape, worldId, out ElementKey element, out int elementBridgeId) ||
                elementBridgeId >= 0 ||
                !bodies.ContainsKey(element) ||
                !TryResolveCounterpart(counterpartShape, worldId, out ElementKey targetElement, out int bridgeTargetId))
            {
                return;
            }

            ulong counterpartUserData = GetCounterpartUserData(counterpartShape);
            if (!emittedContactPairs.Add(new DirectedContactPair(
                    element,
                    counterpartUserData,
                    elementShape.isTrigger || counterpartShape.isTrigger)))
            {
                return;
            }

            destination.Add(new ElementFact
            {
                Type = ElementFactType.Contact,
                Element = element,
                TargetElement = targetElement,
                TargetId = bridgeTargetId >= 0 ? bridgeTargetId : targetElement.Slot,
                BridgeTargetId = bridgeTargetId,
                Position = point,
                Normal = normal,
                TimeOfImpact = 0f,
                IsTrigger = elementShape.isTrigger || counterpartShape.isTrigger ? (byte)1 : (byte)0,
                Flags = flags,
                SubstepIndex = substepIndex,
                FixedStepIndex = fixedStepIndex
            });
        }



        private bool TryCreateDirectedPairFact(
            ElementFactType type,
            in DirectedContactPair pair,
            float2 point,
            float2 normal,
            ElementFactFlags flags,
            ushort substepIndex,
            uint fixedStepIndex,
            out ElementFact fact)
        {
            if (!TryResolvePackedCounterpart(
                    pair.CounterpartUserData,
                    worldId,
                    out ElementKey targetElement,
                    out int bridgeTargetId))
            {
                fact = default;
                return false;
            }

            fact = new ElementFact
            {
                Type = type,
                Element = pair.Element,
                TargetElement = targetElement,
                TargetId = bridgeTargetId >= 0 ? bridgeTargetId : targetElement.Slot,
                BridgeTargetId = bridgeTargetId,
                Position = point,
                Normal = normal,
                TimeOfImpact = 0f,
                IsTrigger = pair.IsTrigger,
                Flags = flags,
                SubstepIndex = substepIndex,
                FixedStepIndex = fixedStepIndex
            };
            return true;
        }



        private static void ResolveContactPoint(
            PhysicsEvents.ContactBeginEvent contact,
            out float2 point,
            out float2 normal,
            out ElementFactFlags flags)
        {
            PhysicsShape.Contact physicsContact = contact.contactId.contact;
            PhysicsShape.ContactManifold manifold = physicsContact.manifold;
            if (manifold.pointCount > 0)
            {
                PhysicsShape.ContactManifold.ManifoldPoint manifoldPoint = manifold.points[0];
                point = new float2(manifoldPoint.point.x, manifoldPoint.point.y);
                normal = new float2(manifold.normal.x, manifold.normal.y);
                flags = ElementFactGeometry.GetSurfaceFlags(point, normal);
                return;
            }

            Vector2 positionA = contact.shapeA.body.position;
            Vector2 positionB = contact.shapeB.body.position;
            point = new float2((positionA.x + positionB.x) * 0.5f, (positionA.y + positionB.y) * 0.5f);
            normal = math.normalizesafe(new float2(positionB.x - positionA.x, positionB.y - positionA.y));
            flags = ElementFactFlags.None;
        }



        private static void ResolveContactPoint(
            in PhysicsShape.Contact contact,
            out float2 point,
            out float2 normal,
            out ElementFactFlags flags)
        {
            PhysicsShape.ContactManifold manifold = contact.manifold;
            normal = new float2(manifold.normal.x, manifold.normal.y);
            if (manifold.pointCount > 0)
            {
                PhysicsShape.ContactManifold.ManifoldPoint manifoldPoint = manifold.points[0];
                point = new float2(manifoldPoint.point.x, manifoldPoint.point.y);
                flags = ElementFactGeometry.GetSurfaceFlags(point, normal);
                return;
            }

            Vector2 positionA = contact.shapeA.body.position;
            Vector2 positionB = contact.shapeB.body.position;
            point = new float2((positionA.x + positionB.x) * 0.5f, (positionA.y + positionB.y) * 0.5f);
            if (math.lengthsq(normal) <= float.Epsilon)
            {
                normal = math.normalizesafe(new float2(positionB.x - positionA.x, positionB.y - positionA.y));
            }
            flags = ElementFactFlags.None;
        }



        private bool TryCreateAreaPair(
            PhysicsShape triggerShape,
            PhysicsShape visitorShape,
            out AreaPair pair)
        {
            if (TryResolveCounterpart(triggerShape, worldId, out ElementKey triggerElement, out int triggerBridgeId) &&
                triggerBridgeId < 0 &&
                areaElements.Contains(triggerElement) &&
                TryResolveCounterpart(visitorShape, worldId, out ElementKey visitorElement, out int visitorBridgeId))
            {
                pair = new AreaPair(triggerElement, visitorElement, visitorBridgeId);
                return true;
            }

            pair = default;
            return false;
        }



        private void AddDynamicTriggerPair(PhysicsShape elementShape, PhysicsShape counterpartShape)
        {
            if (!TryCreateDynamicTriggerPair(elementShape, counterpartShape, out DirectedContactPair pair)) { return; }
            if (activeDynamicTriggerPairs.Add(pair)) { enteredDynamicTriggerPairs.Add(pair); }
        }



        private void RemoveDynamicTriggerPair(
            PhysicsShape elementShape,
            PhysicsShape counterpartShape,
            NativeList<ElementFact> destination,
            ushort substepIndex,
            uint fixedStepIndex)
        {
            if (!TryCreateDynamicTriggerPair(elementShape, counterpartShape, out DirectedContactPair pair) ||
                !activeDynamicTriggerPairs.Remove(pair))
            {
                return;
            }

            if (TryCreateDirectedPairFact(
                ElementFactType.Exit,
                in pair,
                default,
                default,
                ElementFactFlags.None,
                substepIndex,
                    fixedStepIndex,
                    out ElementFact exitFact))
            {
                destination.Add(exitFact);
            }
        }



        private bool TryCreateDynamicTriggerPair(
            PhysicsShape elementShape,
            PhysicsShape counterpartShape,
            out DirectedContactPair pair)
        {
            if (TryResolveCounterpart(elementShape, worldId, out ElementKey element, out int bridgeTargetId) &&
                bridgeTargetId < 0 &&
                dynamicElements.Contains(element))
            {
                ulong counterpartUserData = GetCounterpartUserData(counterpartShape);
                if (counterpartUserData != 0ul)
                {
                    pair = new DirectedContactPair(element, counterpartUserData, isTrigger: true);
                    return true;
                }
            }

            pair = default;
            return false;
        }



        private ElementFact CreateAreaFact(
            ElementFactType type,
            in AreaPair pair,
            ushort substepIndex,
            uint fixedStepIndex)
        {
            float2 position = default;
            if (TryGetBody(pair.Element, out PhysicsBody areaBody))
            {
                position = new float2(areaBody.position.x, areaBody.position.y);
            }

            return new ElementFact
            {
                Type = type,
                Element = pair.Element,
                TargetElement = pair.TargetElement,
                TargetId = pair.BridgeTargetId >= 0 ? pair.BridgeTargetId : pair.TargetElement.Slot,
                BridgeTargetId = pair.BridgeTargetId,
                Position = position,
                IsTrigger = 1,
                SubstepIndex = substepIndex,
                FixedStepIndex = fixedStepIndex
            };
        }



        private void RemoveAreaPairs(ElementKey key)
        {
            pairScratch.Clear();
            foreach (AreaPair pair in activeAreaPairs)
            {
                if (pair.Element == key || pair.TargetElement == key) { pairScratch.Add(pair); }
            }
            for (int i = 0; i < pairScratch.Count; i++) { activeAreaPairs.Remove(pairScratch[i]); }
            pairScratch.Clear();
        }



        private void RemoveDynamicPairs(ElementKey key)
        {
            contactPairScratch.Clear();
            foreach (DirectedContactPair pair in activeDynamicContacts)
            {
                if (pair.Element == key) { contactPairScratch.Add(pair); }
            }
            for (int i = 0; i < contactPairScratch.Count; i++)
            {
                activeDynamicContacts.Remove(contactPairScratch[i]);
            }

            contactPairScratch.Clear();
            foreach (DirectedContactPair pair in activeDynamicTriggerPairs)
            {
                if (pair.Element == key) { contactPairScratch.Add(pair); }
            }
            for (int i = 0; i < contactPairScratch.Count; i++)
            {
                activeDynamicTriggerPairs.Remove(contactPairScratch[i]);
            }
            contactPairScratch.Clear();
        }



        private readonly struct DirectedContactPair : IEquatable<DirectedContactPair>
        {
            public DirectedContactPair(
                ElementKey element,
                ulong counterpartUserData,
                bool isTrigger)
            {
                Element = element;
                CounterpartUserData = counterpartUserData;
                IsTrigger = isTrigger ? (byte)1 : (byte)0;
            }



            public ElementKey Element { get; }
            public ulong CounterpartUserData { get; }
            public byte IsTrigger { get; }



            public bool Equals(DirectedContactPair other) =>
                Element.Equals(other.Element) && CounterpartUserData == other.CounterpartUserData;



            public override bool Equals(object obj) => obj is DirectedContactPair other && Equals(other);



            public override int GetHashCode()
            {
                unchecked
                {
                    return (Element.GetHashCode() * 397) ^ CounterpartUserData.GetHashCode();
                }
            }
        }
    }
}
