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



        /// <summary>
        /// alive Element에 native sparse Lifetime Feature를 추가하거나 교체합니다.
        /// </summary>
        public bool TrySetLifetime(ElementKey key, in ElementLifetimeFeature lifetime)
        {
            if (!TryGetRegistryEntry(key, out ElementRegistryEntry entry) ||
                entry.Lifecycle != ElementLifecycle.Alive ||
                !TryGetState(key, out NativeElementState state))
            {
                return false;
            }

            var native = new NativeElementLifetimeState
            {
                Feature = lifetime,
                AdvanceWithFixedTick = 0
            };
            bool added = lifetimeFeatures.AddOrSet(key, in native);
            state.Capabilities |= ElementCapabilities.Lifetime;
            SetState(entry, state);
            if (added) { IncrementStructuralRevision(); }
            return true;
        }





        /// <summary>
        /// alive Element의 native sparse Lifetime Feature를 조회합니다.
        /// </summary>
        public bool TryGetLifetime(ElementKey key, out ElementLifetimeFeature lifetime)
        {
            if (IsAlive(key) && lifetimeFeatures.TryGet(key, out NativeElementLifetimeState native))
            {
                lifetime = native.Feature;
                return true;
            }

            lifetime = default;
            return false;
        }





        /// <summary>
        /// alive Element에서 선택형 Lifetime Feature를 제거합니다.
        /// </summary>
        public bool RemoveLifetime(ElementKey key)
        {
            if (!TryGetRegistryEntry(key, out ElementRegistryEntry entry) ||
                entry.Lifecycle != ElementLifecycle.Alive ||
                !lifetimeFeatures.Remove(key) ||
                !TryGetState(key, out NativeElementState state))
            {
                return false;
            }

            state.Capabilities &= ~ElementCapabilities.Lifetime;
            SetState(entry, state);
            IncrementStructuralRevision();
            return true;
        }





        /// <summary>
        /// alive Element에 선택형 로컬 시계를 추가하거나 교체합니다.
        /// </summary>
        public bool TrySetLocalClock(ElementKey key, in ElementLocalClock clock)
        {
            if (!TryGetRegistryEntry(key, out ElementRegistryEntry entry) ||
                entry.Lifecycle != ElementLifecycle.Alive ||
                !TryGetState(key, out NativeElementState state))
            {
                return false;
            }

            bool added = localClocks.AddOrSet(key, in clock);
            state.HasLocalClock = 1;
            state.LocalTimeScale = clock.TimeScale;
            state.LocalClockPaused = (byte)(clock.Paused ? 1 : 0);
            SetState(entry, state);
            if (added) { IncrementStructuralRevision(); }
            return true;
        }





        /// <summary>
        /// alive Element의 선택형 로컬 시계를 조회합니다.
        /// </summary>
        public bool TryGetLocalClock(ElementKey key, out ElementLocalClock clock)
        {
            if (IsAlive(key)) { return localClocks.TryGet(key, out clock); }
            clock = default;
            return false;
        }





        /// <summary>
        /// alive Element에서 선택형 로컬 시계를 제거합니다.
        /// </summary>
        public bool RemoveLocalClock(ElementKey key)
        {
            if (!TryGetRegistryEntry(key, out ElementRegistryEntry entry) ||
                entry.Lifecycle != ElementLifecycle.Alive ||
                !localClocks.Remove(key) ||
                !TryGetState(key, out NativeElementState state))
            {
                return false;
            }

            state.HasLocalClock = 0;
            state.LocalTimeScale = 1f;
            state.LocalClockPaused = 0;
            SetState(entry, state);
            IncrementStructuralRevision();
            return true;
        }





        private void AdvanceLifetimeFeatures(
            in ElementUpdateContext context,
            bool advanceWithFixedTick,
            bool enqueueFacts)
        {
            for (int i = lifetimeFeatures.Count - 1; i >= 0; i--)
            {
                ElementKey key = lifetimeFeatures.GetKeyAt(i);
                NativeElementLifetimeState lifetime = lifetimeFeatures.GetValueAt(i);
                if (!TryGetRegistryEntry(key, out ElementRegistryEntry entry) ||
                    entry.Lifecycle != ElementLifecycle.Alive ||
                    !TryGetState(key, out NativeElementState state))
                {
                    lifetimeFeatures.RemoveAtSwapBack(i);
                    continue;
                }
                if ((lifetime.AdvanceWithFixedTick != 0) != advanceWithFixedTick) { continue; }

                ElementUpdateSchedule schedule = lifetime.Feature.Schedule;
                float deltaUnits = context.GetDeltaUnits(in schedule);
                if (lifetime.Feature.UseLocalClock && localClocks.TryGet(key, out ElementLocalClock clock))
                {
                    deltaUnits = clock.Paused ? 0f : deltaUnits * clock.TimeScale;
                }
                if (deltaUnits <= 0f) { continue; }

                float consumedUnits;
                float intervalUnits = lifetime.Feature.Schedule.IntervalUnits;
                if (intervalUnits <= 0f)
                {
                    consumedUnits = deltaUnits;
                    lifetime.PendingUnits = 0f;
                }
                else
                {
                    lifetime.PendingUnits += deltaUnits;
                    int updateCount = (int)math.floor((lifetime.PendingUnits + 0.000001f) / intervalUnits);
                    if (updateCount <= 0)
                    {
                        lifetimeFeatures.SetValueAt(i, in lifetime);
                        continue;
                    }

                    consumedUnits = updateCount * intervalUnits;
                    lifetime.PendingUnits = math.max(0f, lifetime.PendingUnits - consumedUnits);
                }

                lifetime.Feature = lifetime.Feature.WithRemainingUnits(
                    lifetime.Feature.RemainingUnits - consumedUnits);
                lifetimeFeatures.SetValueAt(i, in lifetime);
                if (lifetime.Feature.RemainingUnits <= 0f)
                {
                    state.Lifecycle = ElementLifecycle.DespawnPending;
                    ElementFact fact = new ElementFact
                    {
                        Type = ElementFactType.LifetimeExpired,
                        Element = key,
                        Position = state.Position,
                        TargetId = -1,
                        BridgeTargetId = -1,
                        FixedStepIndex = fixedStepIndex
                    };
                    if (enqueueFacts) { GetFactQueue(entry.Lane).Enqueue(fact); }
                    else { factBuffer.Add(fact); }
                }

                SetState(entry, state);
            }
        }





        private float GetRemainingLifetimeUnits(ElementKey key) =>
            lifetimeFeatures.TryGet(key, out NativeElementLifetimeState lifetime)
                ? lifetime.Feature.RemainingUnits
                : 0f;
    }
}