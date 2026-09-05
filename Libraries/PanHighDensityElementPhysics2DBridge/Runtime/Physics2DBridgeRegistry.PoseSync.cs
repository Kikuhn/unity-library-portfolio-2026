using System;
using System.Collections.Generic;
using Unity.U2D.Physics;
using UnityEngine;



namespace Pan.HighDensityElement.Physics2DBridge
{
    public sealed partial class Physics2DBridgeRegistry
    {
        ///======================================================================================================================================================
        //? Legacy와 provider pose를 수동 Core proxy에 동기화
        ///======================================================================================================================================================



        /// <summary>
        /// pending 등록과 geometry dirty를 처리하고 legacy/provider pose를 Core proxy에 batch 경계로 반영합니다.
        /// </summary>
        public void SynchronizePoses()
        {
            ThrowIfDisposed();
            long currentSynchronizationSequence = ++synchronizationSequence;
            StrictMotionCount = 0;
            receiverDispatchStamps.Clear();
            if (pendingComponents.Count > 0)
            {
                Physics2D.SyncTransforms();
                for (int i = pendingComponents.Count - 1; i >= 0; i--)
                {
                    HighDensityPhysicsBridge2D component = pendingComponents[i];
                    pendingComponents.RemoveAt(i);
                    if (component == null || !component.isActiveAndEnabled) { continue; }

                    //? Collider/provider 형상이 아직 준비되지 않았다면 다음 동기화 경계에서 다시 등록을 시도합니다.
                    if (!TryRegister(component, out _)) { QueuePending(component); }
                }
            }

            foreach (ComponentRecord registration in componentBodies.Values)
            {
                if (!registration.GeometryDirty) { continue; }
                RebuildComponentRecords(registration);
                registration.GeometryDirty = false;
            }

            for (int i = bodies.Count - 1; i >= 0; i--)
            {
                BodyRecord record = bodies[i];

                if (ShouldRemoveAutomaticRecord(record))
                {
                    DestroyRecord(record);
                    continue;
                }

                //? 런타임 bridge 대상만 저비용 shape hash를 확인합니다. 자동 수집된 대형 정적 지형은 Tile 이벤트/명시적 dirty 경로를 사용합니다.
                if (record.Provider == null && record.Owner != null && HasColliderProjectionChanged(record))
                {
                    record.GeometryDirty = true;
                }

                if (record.GeometryDirty)
                {
                    RebuildRecord(record);
                    record.GeometryDirty = false;
                }

                if (record.FactReceiversDirty)
                {
                    RefreshFactReceiverCache(record);
                    record.FactReceiversDirty = false;
                }

                if (record.Provider != null)
                {
                    if (record.ChangeSource == null &&
                        record.Provider.GeometryRevision != record.ProviderGeometryRevision)
                    {
                        RebuildRecord(record);
                        record.ProviderGeometryRevision = record.Provider.GeometryRevision;
                    }

                    SynchronizeProviderState(record);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    record.LastSynchronizationSequence = currentSynchronizationSequence;
#endif
                    record.StateDirty = false;
                    continue;
                }

                Rigidbody2D rigidbody = record.Rigidbody;
                if (rigidbody == null)
                {
                    if (record.Owner != null && record.StateDirty && record.Body.isValid)
                    {
                        record.Body.enabled = record.Owner.isActiveAndEnabled;
                        record.StateDirty = false;
                    }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    record.LastSynchronizationSequence = currentSynchronizationSequence;
#endif
                    continue;
                }

                if (!record.Body.isValid) { continue; }
                UpdateMotionState(record, rigidbody.position, rigidbody.rotation);
                record.Body.position = rigidbody.position;
                record.Body.rotation = PhysicsRotate.FromDegrees(rigidbody.rotation);
                //? Legacy pose를 이미 직접 투영했으므로 Core simulate에서 속도로 다시 적분하지 않습니다.
                record.Body.linearVelocity = Vector2.zero;
                record.Body.angularVelocity = 0f;
                record.Body.enabled = rigidbody.simulated && rigidbody.gameObject.activeInHierarchy;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                record.LastSynchronizationSequence = currentSynchronizationSequence;
#endif
                record.StateDirty = false;
            }

            SubmitMovingProjections(currentSynchronizationSequence);
            RefreshAggregateCounts();
        }



        private void SynchronizeProviderState(BodyRecord record)
        {
            if (!record.Body.isValid || record.Provider == null ||
                !record.Provider.TryGetBodyState(out Physics2DBridgeBodyState state))
            {
                if (record.Body.isValid) { record.Body.enabled = false; }
                return;
            }

            UpdateMotionState(record, state.Position, state.RotationDegrees);
            record.Body.position = state.Position;
            record.Body.rotation = PhysicsRotate.FromDegrees(state.RotationDegrees);
            //? provider도 Legacy pose의 passive mirror이며 Core 쪽에서 속도를 다시 적분하지 않습니다.
            record.Body.linearVelocity = Vector2.zero;
            record.Body.angularVelocity = 0f;
            record.Body.enabled = state.Enabled;
        }



        private void UpdateMotionState(
            BodyRecord record,
            Vector2 position,
            float rotationDegrees)
        {
            bool teleported = record.TeleportPending;
            record.TeleportPending = false;
            record.TeleportedThisSync = teleported;

            if (teleported)
            {
                record.PreviousLegacyPosition = position;
                record.PreviousLegacyRotationDegrees = rotationDegrees;
            }
            else
            {
                record.PreviousLegacyPosition = record.LegacyPosition;
                record.PreviousLegacyRotationDegrees = record.LegacyRotationDegrees;
            }

            record.LegacyPosition = position;
            record.LegacyRotationDegrees = rotationDegrees;

            float minimumExtent = float.IsPositiveInfinity(record.MinimumShapeExtent)
                ? 0f
                : Mathf.Max(0.0001f, record.MinimumShapeExtent);
            float linearDistance = Vector2.Distance(record.PreviousLegacyPosition, position);
            float angularDistance = Mathf.Abs(Mathf.DeltaAngle(
                record.PreviousLegacyRotationDegrees,
                rotationDegrees)) * Mathf.Deg2Rad * record.MaximumShapeRadius;
            bool automaticStrict = !teleported && minimumExtent > 0f &&
                linearDistance + angularDistance >= minimumExtent * settings.StrictCcdMotionRatio;
            bool manuallyStrict = !teleported && record.StrictCcdRemainingSteps > 0;
            record.StrictCcdRequestedThisSync = automaticStrict || manuallyStrict;
            if (record.StrictCcdRemainingSteps > 0) { record.StrictCcdRemainingSteps--; }
            if (record.StrictCcdRequestedThisSync) { StrictMotionCount++; }
        }



    }
}
