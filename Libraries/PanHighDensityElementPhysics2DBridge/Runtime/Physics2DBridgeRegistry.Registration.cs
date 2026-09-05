using System;
using System.Collections.Generic;
using Unity.U2D.Physics;
using UnityEngine;



namespace Pan.HighDensityElement.Physics2DBridge
{
    public sealed partial class Physics2DBridgeRegistry
    {
        ///======================================================================================================================================================
        //? 브리지 등록, generation handle과 Default registry 수명
        ///======================================================================================================================================================



        /// <summary>
        /// 설정 layer에 속한 활성 static Collider2D를 scene에서 찾아 한 번 mirror합니다.
        /// </summary>
        /// <returns>새로 등록된 target 수입니다.</returns>
        public int RegisterStaticSceneColliders()
        {
            ThrowIfDisposed();
            Collider2D[] sceneColliders = UnityEngine.Object.FindObjectsByType<Collider2D>(
                FindObjectsInactive.Exclude);
            int before = targets.Count;
            var rigidbodyGroups = new Dictionary<Rigidbody2D, List<Collider2D>>();
            for (int i = 0; i < sceneColliders.Length; i++)
            {
                Collider2D collider = sceneColliders[i];
                if (!ShouldAutoRegister(collider) || targetIdsByCollider.ContainsKey(collider)) { continue; }

                Rigidbody2D rigidbody = collider.attachedRigidbody;
                if (rigidbody == null)
                {
                    colliderScratch.Clear();
                    colliderScratch.Add(collider);
                    RegisterGroup(null, null, colliderScratch);
                    continue;
                }

                if (!rigidbodyGroups.TryGetValue(rigidbody, out List<Collider2D> group))
                {
                    group = new List<Collider2D>(4);
                    rigidbodyGroups.Add(rigidbody, group);
                }
                group.Add(collider);
            }

            foreach (KeyValuePair<Rigidbody2D, List<Collider2D>> pair in rigidbodyGroups)
            {
                RegisterGroup(null, pair.Key, pair.Value);
            }
            return targets.Count - before;
        }



        /// <summary>
        /// bridge component를 idempotent하게 등록합니다.
        /// </summary>
        public bool Register(HighDensityPhysicsBridge2D component)
        {
            return TryRegister(component, out _);
        }



        /// <summary>
        /// bridge component를 등록하고 registry-scoped generation handle을 반환합니다.
        /// </summary>
        public bool TryRegister(
            HighDensityPhysicsBridge2D component,
            out Physics2DBridgeRegistrationHandle handle)
        {
            ThrowIfDisposed();
            handle = default;
            if (component == null) { return false; }
            if (componentBodies.TryGetValue(component, out ComponentRecord existing))
            {
                handle = existing.Handle;
                return true;
            }

            handle = AllocateRegistrationHandle();
            var registration = new ComponentRecord
            {
                Owner = component,
                Handle = handle
            };

            if (!BuildComponentRecords(registration))
            {
                ReleaseRegistrationHandle(handle);
                handle = default;
                return false;
            }

            componentBodies.Add(component, registration);
            registrationsBySlot.Add(handle.Slot, registration);
            component.SetRegistration(this, handle);
            return true;
        }



        internal bool TryRegisterOrQueue(
            HighDensityPhysicsBridge2D component,
            out Physics2DBridgeRegistrationHandle handle)
        {
            if (TryRegister(component, out handle)) { return true; }
            QueuePending(component);
            return pendingComponents.Contains(component);
        }



        /// <summary>
        /// component가 소유한 모든 Core proxy body와 target mapping을 해제합니다.
        /// </summary>
        public bool Unregister(HighDensityPhysicsBridge2D component)
        {
            if (disposed || component == null || !componentBodies.TryGetValue(component, out ComponentRecord record))
            {
                return false;
            }

            return Unregister(record.Handle);
        }



        /// <summary>
        /// 현재 registry의 유효한 registration handle을 해제합니다.
        /// </summary>
        public bool Unregister(Physics2DBridgeRegistrationHandle handle)
        {
            if (disposed || !IsRegistered(handle) ||
                !registrationsBySlot.TryGetValue(handle.Slot, out ComponentRecord registration))
            {
                return false;
            }

            registrationsBySlot.Remove(handle.Slot);
            componentBodies.Remove(registration.Owner);
            pendingComponents.Remove(registration.Owner);
            for (int i = registration.Bodies.Count - 1; i >= 0; i--)
            {
                DestroyRecord(registration.Bodies[i]);
            }
            registration.Bodies.Clear();
            registration.Owner?.ClearRegistration(this);
            ReleaseRegistrationHandle(handle);
            return true;
        }



        /// <summary>
        /// handle이 이 registry의 현재 generation을 가리키는지 확인합니다.
        /// </summary>
        public bool IsRegistered(Physics2DBridgeRegistrationHandle handle)
        {
            return !disposed && handle.IsValid && handle.ContextId == contextId &&
                handle.Slot < registrationGenerations.Count &&
                registrationGenerations[handle.Slot] == handle.Generation &&
                registrationsBySlot.ContainsKey(handle.Slot);
        }



        public void MarkGeometryDirty(HighDensityPhysicsBridge2D component)
        {
            if (!disposed && component != null && componentBodies.TryGetValue(component, out ComponentRecord record))
            {
                record.GeometryDirty = true;
                return;
            }
            QueuePending(component);
        }



        public void MarkStateDirty(HighDensityPhysicsBridge2D component)
        {
            if (!disposed && component != null && componentBodies.TryGetValue(component, out ComponentRecord record))
            {
                for (int i = 0; i < record.Bodies.Count; i++) { record.Bodies[i].StateDirty = true; }
                return;
            }
            QueuePending(component);
        }



        /// <summary>
        /// 다음 고정 동기화에서 지정한 bridge의 접촉 receiver 캐시를 다시 만듭니다.
        /// </summary>
        public void MarkFactReceiversDirty(HighDensityPhysicsBridge2D component)
        {
            if (!disposed && component != null && componentBodies.TryGetValue(component, out ComponentRecord record))
            {
                for (int i = 0; i < record.Bodies.Count; i++) { record.Bodies[i].FactReceiversDirty = true; }
                return;
            }
            QueuePending(component);
        }



        internal static void NotifyEnabled(HighDensityPhysicsBridge2D component)
        {
            if (component == null) { return; }
            if (!EnabledComponents.Contains(component)) { EnabledComponents.Add(component); }
            Default?.TryRegisterOrQueue(component, out _);
        }



        internal static void NotifyDisabled(HighDensityPhysicsBridge2D component)
        {
            if (component == null) { return; }
            EnabledComponents.Remove(component);
            if (Default == null) { return; }
            Default.pendingComponents.Remove(component);
            Default.Unregister(component);
        }



        private static void PromoteDefault(Physics2DBridgeRegistry registry)
        {
            Default = registry;
            if (registry != null) { RegisterEnabledComponents(registry); }
        }



        private static void RegisterEnabledComponents(Physics2DBridgeRegistry registry)
        {
            if (registry == null || registry.disposed) { return; }

            for (int i = EnabledComponents.Count - 1; i >= 0; i--)
            {
                HighDensityPhysicsBridge2D component = EnabledComponents[i];
                if (component == null)
                {
                    EnabledComponents.RemoveAt(i);
                    continue;
                }

                if (component.isActiveAndEnabled)
                {
                    registry.TryRegisterOrQueue(component, out _);
                }
            }
        }



        private bool BuildComponentRecords(ComponentRecord registration)
        {
            HighDensityPhysicsBridge2D owner = registration.Owner;
            IPhysics2DBridgeShapeProvider provider = owner.ShapeProvider;

            //? 실제 Collider 형상은 offset, direction, 자식 Transform을 포함한 Unity 저수준 형상이므로 provider 근사보다 우선할 수 있어야 합니다.
            switch (owner.ShapeSourceMode)
            {
                case Physics2DBridgeShapeSourceMode.ProviderOnly:
                case Physics2DBridgeShapeSourceMode.ManualShapes:
                    return BuildProviderRecord(registration, owner, provider);

                case Physics2DBridgeShapeSourceMode.CollidersThenProvider:
                    return BuildColliderRecords(registration, owner) ||
                        BuildProviderRecord(registration, owner, provider);

                case Physics2DBridgeShapeSourceMode.SelectedColliders:
                case Physics2DBridgeShapeSourceMode.CollidersOnly:
                default:
                    return BuildColliderRecords(registration, owner);
            }
        }



        private bool BuildProviderRecord(
            ComponentRecord registration,
            HighDensityPhysicsBridge2D owner,
            IPhysics2DBridgeShapeProvider provider)
        {
            BodyRecord providerRecord = RegisterProviderGroup(owner, provider);
            if (providerRecord == null) { return false; }

            registration.Bodies.Add(providerRecord);
            return true;
        }



        private bool BuildColliderRecords(
            ComponentRecord registration,
            HighDensityPhysicsBridge2D owner)
        {
            int initialBodyCount = registration.Bodies.Count;

            colliderScratch.Clear();
            if (owner.CollectColliders(colliderScratch) == 0) { return false; }

            var rigidbodyGroups = new Dictionary<Rigidbody2D, List<Collider2D>>();
            List<Collider2D> staticGroup = null;
            for (int i = 0; i < colliderScratch.Count; i++)
            {
                Collider2D collider = colliderScratch[i];
                if (collider == null || IsCompositedSource(collider)) { continue; }

                Rigidbody2D rigidbody = collider.attachedRigidbody;
                if (rigidbody == null)
                {
                    staticGroup ??= new List<Collider2D>(4);
                    staticGroup.Add(collider);
                    continue;
                }

                if (!rigidbodyGroups.TryGetValue(rigidbody, out List<Collider2D> group))
                {
                    group = new List<Collider2D>(4);
                    rigidbodyGroups.Add(rigidbody, group);
                }
                group.Add(collider);
            }

            if (staticGroup != null)
            {
                BodyRecord staticRecord = RegisterGroup(owner, null, staticGroup);
                if (staticRecord != null) { registration.Bodies.Add(staticRecord); }
            }

            foreach (KeyValuePair<Rigidbody2D, List<Collider2D>> pair in rigidbodyGroups)
            {
                BodyRecord bodyRecord = RegisterGroup(owner, pair.Key, pair.Value);
                if (bodyRecord != null) { registration.Bodies.Add(bodyRecord); }
            }
            return registration.Bodies.Count > initialBodyCount;
        }



        private void RebuildComponentRecords(ComponentRecord registration)
        {
            for (int i = registration.Bodies.Count - 1; i >= 0; i--)
            {
                DestroyRecord(registration.Bodies[i]);
            }
            registration.Bodies.Clear();
            if (!BuildComponentRecords(registration))
            {
                Debug.LogError("Physics2D bridge registration no longer exposes a supported shape.", registration.Owner);
            }
        }



        private BodyRecord RegisterProviderGroup(
            HighDensityPhysicsBridge2D owner,
            IPhysics2DBridgeShapeProvider provider)
        {
            if (provider == null || !provider.TryGetBodyState(out _)) { return null; }

            int targetId = NextTargetId();
            var record = new BodyRecord
            {
                Owner = owner,
                Provider = provider,
                ProviderTarget = provider.TargetComponent != null ? provider.TargetComponent : owner,
                ProviderGeometryRevision = provider.GeometryRevision
            };
            record.TargetIds.Add(targetId);
            if (!CreateBodyAndShapes(record)) { return null; }

            targets.Add(targetId, new TargetRecord(
                null,
                record,
                record.ProviderTarget,
                record.ProviderLayer,
                record.ProviderIsTrigger));
            RefreshFactReceiverCache(record);
            SubscribeProviderChanges(record);
            bodies.Add(record);
            RefreshAggregateCounts();
            return record;
        }



        private Physics2DBridgeRegistrationHandle AllocateRegistrationHandle()
        {
            int slot;
            if (freeRegistrationSlots.Count > 0)
            {
                slot = freeRegistrationSlots.Pop();
            }
            else
            {
                slot = registrationGenerations.Count;
                registrationGenerations.Add(1u);
            }

            uint generation = registrationGenerations[slot];
            if (generation == 0u)
            {
                generation = 1u;
                registrationGenerations[slot] = generation;
            }
            return new Physics2DBridgeRegistrationHandle(contextId, slot, generation);
        }



        private void ReleaseRegistrationHandle(Physics2DBridgeRegistrationHandle handle)
        {
            if (!handle.IsValid || handle.ContextId != contextId || handle.Slot >= registrationGenerations.Count)
            {
                return;
            }

            uint generation = registrationGenerations[handle.Slot] + 1u;
            registrationGenerations[handle.Slot] = generation == 0u ? 1u : generation;
            freeRegistrationSlots.Push(handle.Slot);
        }



        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null) { return "<null>"; }
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }



        private int NextTargetId()
        {
            do
            {
                nextTargetId = nextTargetId == int.MaxValue ? 1 : nextTargetId + 1;
            }
            while (targets.ContainsKey(nextTargetId));
            return nextTargetId;
        }



        private void QueuePending(HighDensityPhysicsBridge2D component)
        {
            if (disposed || component == null || componentBodies.ContainsKey(component) ||
                pendingComponents.Contains(component))
            {
                return;
            }
            pendingComponents.Add(component);
        }

    }
}
