using System;
using Unity.Mathematics;
using UnityEngine;



namespace Pan.HighDensityElement
{
    internal struct ElementRegistryEntry
    {
        public uint Generation;
        public int DenseIndex;
        public int PoseDenseIndex;
        public ElementLane Lane;
        public ElementLifecycle Lifecycle;
    }



    internal struct NativeElementPoseState
    {
        public ElementKey Key;
        public float2 PreviousPosition;
        public float2 Position;
        public float PreviousRotationRadians;
        public float RotationRadians;
        public float2 PreviousScale;
        public float2 Scale;



        public ElementPose2D ToPose() => new ElementPose2D(
            PreviousPosition,
            Position,
            PreviousRotationRadians,
            RotationRadians,
            PreviousScale,
            Scale);
    }



    internal struct NativeElementLifetimeState
    {
        public ElementLifetimeFeature Feature;
        public float PendingUnits;
        public byte AdvanceWithFixedTick;
    }



    internal struct NativeElementWaveMotionState
    {
        public ElementWaveMotionFeature Feature;
        public float ElapsedSeconds;
        public float PreviousOffset;
        public float2 PreviousAxis;
        public byte Initialized;
    }



    internal struct NativeElementVisualOrientationState
    {
        public ElementVisualOrientationFeature Feature;
        public float SpinPhaseRadians;
    }



    internal struct NativeElementBoundaryState
    {
        public ElementBoundaryFeature Feature;
        public byte HasEntered;
    }



    internal struct NativeElementState
    {
        public ElementKey Key;
        public ElementCapabilities Capabilities;
        public ElementLifecycle Lifecycle;
        public float2 PreviousPosition;
        public float2 Position;
        public float PreviousRotationRadians;
        public float RotationRadians;
        public float2 Velocity;
        public float2 Acceleration;
        public float LinearDamping;
        public float2 PreviousScale;
        public float2 Scale;
        public Color32 Color;
        public float Radius;
        public float PeriodicInterval;
        public float PeriodicRemaining;
        public int VisualId;
        public uint OwnerId;
        public uint TeamId;
        public ulong PhysicsCategoryMask;
        public ulong InteractionLayerMask;
        public byte IncludeTriggers;
        public ElementPhysicsMaterial2D PhysicsMaterial;
        public byte Penetrating;
        public PhysicsCoreBodyMode PhysicsBodyMode;
        public PhysicsCoreShape2D PhysicsShape;
        public byte PhysicsIsTrigger;
        public byte HasPhysicsMaterial;
        public StrictCcdOverride2D StrictCcdOverride;
        public float StrictCcdThresholdRatio;
        public ushort StrictCcdRemainingSteps;
        public byte StrictCcdActive;
        public byte StrictCcdForcedThisStep;
        public byte TeleportedThisStep;
        public byte TeleportPending;
        public float LocalTimeScale;
        public byte HasLocalClock;
        public byte LocalClockPaused;



        public ElementSnapshot ToSnapshot(float remainingLifetime)
        {
            return new ElementSnapshot(
                Key,
                Lifecycle,
                Capabilities,
                ResolvePhysicsCapabilities(Capabilities),
                ResolveExecutionModel(Capabilities),
                ResolveLane(Capabilities),
                PreviousPosition,
                Position,
                PreviousRotationRadians,
                RotationRadians,
                Velocity,
                Acceleration,
                LinearDamping,
                Scale,
                Color,
                Radius,
                remainingLifetime,
                VisualId,
                OwnerId,
                TeamId,
                PhysicsCategoryMask,
                InteractionLayerMask,
                IncludeTriggers != 0,
                PhysicsShape,
                StrictCcdOverride,
                StrictCcdThresholdRatio,
                StrictCcdRemainingSteps,
                StrictCcdActive != 0,
                TeleportedThisStep != 0);
        }



        private static ElementPhysicsCapability ResolvePhysicsCapabilities(ElementCapabilities capabilities)
        {
            ElementPhysicsCapability result = ElementPhysicsCapability.None;
            if ((capabilities & ElementCapabilities.QuerySensor2D) != 0)
            {
                result |= ElementPhysicsCapability.QuerySensor2D;
            }
            if ((capabilities & ElementCapabilities.QueryTarget2D) != 0)
            {
                result |= ElementPhysicsCapability.QueryTarget2D;
            }
            if ((capabilities & ElementCapabilities.AreaSensor2D) != 0)
            {
                result |= ElementPhysicsCapability.AreaSensor2D;
            }
            if ((capabilities & ElementCapabilities.DynamicBody2D) != 0)
            {
                result |= ElementPhysicsCapability.DynamicBody2D;
            }
            return result;
        }



        private static ElementExecutionModel2D ResolveExecutionModel(ElementCapabilities capabilities) =>
            (capabilities & ElementCapabilities.DynamicBody2D) != 0
                ? ElementExecutionModel2D.Simulated
                : ElementExecutionModel2D.Query;



        private static ElementLane ResolveLane(ElementCapabilities capabilities)
        {
            if ((capabilities & ElementCapabilities.AreaSensor2D) != 0)
            {
                return ElementLane.AreaSensorSprite2D;
            }

            if ((capabilities & (ElementCapabilities.DynamicBody2D | ElementCapabilities.QueryTarget2D)) != 0)
            {
                return ElementLane.DynamicBodySprite2D;
            }

            return ElementLane.QuerySprite2D;
        }
    }



    internal readonly struct AreaPair : IEquatable<AreaPair>
    {
        public AreaPair(ElementKey element, ElementKey targetElement, int bridgeTargetId)
        {
            Element = element;
            TargetElement = targetElement;
            BridgeTargetId = bridgeTargetId;
        }



        public ElementKey Element { get; }
        public ElementKey TargetElement { get; }
        public int BridgeTargetId { get; }



        public bool Equals(AreaPair other) =>
            Element.Equals(other.Element) &&
            TargetElement.Equals(other.TargetElement) &&
            BridgeTargetId == other.BridgeTargetId;
        public override bool Equals(object obj) => obj is AreaPair other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Element.GetHashCode();
                hashCode = (hashCode * 397) ^ TargetElement.GetHashCode();
                hashCode = (hashCode * 397) ^ BridgeTargetId;
                return hashCode;
            }
        }
    }



    public struct ElementRenderItem : IComparable<ElementRenderItem>
    {
        public int VisualId;
        public float2 Position;
        public float RotationRadians;
        public float2 Scale;
        public Color32 Color;
        public int2 SpatialChunk;



        public int CompareTo(ElementRenderItem other)
        {
            int visualComparison = VisualId.CompareTo(other.VisualId);
            if (visualComparison != 0) { return visualComparison; }

            int xComparison = SpatialChunk.x.CompareTo(other.SpatialChunk.x);
            return xComparison != 0 ? xComparison : SpatialChunk.y.CompareTo(other.SpatialChunk.y);
        }
    }
}
