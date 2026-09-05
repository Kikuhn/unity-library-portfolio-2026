using System;
using System.Collections.Generic;
using UnityEngine;



namespace Pan.HighDensityElement.Physics2DBridge
{
    /// <summary>
    /// 브리지 컴포넌트가 PhysicsCore2D proxy 형상을 구성할 원본을 결정합니다.
    /// 기존 Collider 우선과 Collider가 없는 커스텀 객체의 provider fallback을 명시적으로 조합할 수 있습니다.
    /// </summary>
    public enum Physics2DBridgeShapeSourceMode : byte
    {
        [InspectorName("Automatic")]
        CollidersOnly = 0,

        [InspectorName("Advanced / Provider Only")]
        ProviderOnly = 1,

        [InspectorName("Advanced / Automatic Then Provider")]
        CollidersThenProvider = 2,

        [InspectorName("Selected")]
        SelectedColliders = 3,

        [InspectorName("Manual")]
        ManualShapes = 4
    }



    /// <summary>
    /// 수동 형상의 Core proxy body 동작 방식을 결정합니다.
    /// </summary>
    public enum Physics2DBridgeManualBodyMode : byte
    {
        Auto = 0,
        Static = 1,
        Kinematic = 2
    }



    /// <summary>
    /// provider가 revision polling 없이 변경을 알릴 수 있는 선택형 계약입니다.
    /// 이벤트는 main thread의 bridge 동기화 경계 밖에서 PhysicsCore 구조를 직접 변경하지 않습니다.
    /// </summary>
    public interface IPhysics2DBridgeChangeSource
    {
        event Action GeometryChanged;
        event Action StateChanged;
    }



    /// <summary>
    /// 커스텀 provider가 PhysicsCore2D로 내보낼 수 있는 저수준 2D 형상 종류입니다.
    /// </summary>
    public enum Physics2DBridgeShapeType : byte
    {
        Circle = 0,
        Box = 1,
        Capsule = 2,
        Polygon = 3,
        Segment = 4,
        Chain = 5
    }



    /// <summary>
    /// 하나의 <see cref="Physics2DBridgeRegistry"/> 안에서 등록을 식별하는 generation-safe 값입니다.
    /// registry가 다르거나 등록이 반환된 뒤에는 같은 slot 값이라도 stale handle로 처리됩니다.
    /// </summary>
    public readonly struct Physics2DBridgeRegistrationHandle : IEquatable<Physics2DBridgeRegistrationHandle>
    {
        internal Physics2DBridgeRegistrationHandle(int contextId, int slot, uint generation)
        {
            ContextId = contextId;
            Slot = slot;
            Generation = generation;
        }



        public int ContextId { get; }
        public int Slot { get; }
        public uint Generation { get; }
        public bool IsValid => ContextId != 0 && Slot >= 0 && Generation != 0;



        public bool Equals(Physics2DBridgeRegistrationHandle other) =>
            ContextId == other.ContextId && Slot == other.Slot && Generation == other.Generation;

        public override bool Equals(object obj) => obj is Physics2DBridgeRegistrationHandle other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(ContextId, Slot, Generation);
        public static bool operator ==(Physics2DBridgeRegistrationHandle left, Physics2DBridgeRegistrationHandle right) => left.Equals(right);
        public static bool operator !=(Physics2DBridgeRegistrationHandle left, Physics2DBridgeRegistrationHandle right) => !left.Equals(right);
    }



    /// <summary>
    /// Legacy 물리 객체가 권위자로 제공하는 현재 body 상태입니다.
    /// </summary>
    public readonly struct Physics2DBridgeBodyState
    {
        public Physics2DBridgeBodyState(
            Vector2 position,
            float rotationDegrees,
            Vector2 linearVelocity,
            float angularVelocity,
            bool enabled,
            bool isStatic = false)
        {
            Position = position;
            RotationDegrees = rotationDegrees;
            LinearVelocity = linearVelocity;
            AngularVelocity = angularVelocity;
            Enabled = enabled;
            IsStatic = isStatic;
        }



        public Vector2 Position { get; }
        public float RotationDegrees { get; }
        public Vector2 LinearVelocity { get; }
        public float AngularVelocity { get; }
        public bool Enabled { get; }
        public bool IsStatic { get; }
    }



    /// <summary>
    /// 하나의 PhysicsCore2D proxy shape를 body-local 좌표로 기술하는 값형식입니다.
    /// </summary>
    public readonly struct Physics2DBridgeShapeDescriptor
    {
        public Physics2DBridgeShapeDescriptor(
            Physics2DBridgeShapeType type,
            Vector2 pointA,
            Vector2 pointB,
            Vector2 size,
            float rotationDegrees,
            float radius,
            int vertexStart,
            int vertexCount,
            int layer,
            bool isTrigger,
            float density = 1f,
            float friction = 0.4f,
            float bounciness = 0f,
            PhysicsMaterialCombine2D frictionCombine = PhysicsMaterialCombine2D.Average,
            PhysicsMaterialCombine2D bounceCombine = PhysicsMaterialCombine2D.Average)
        {
            Type = type;
            PointA = pointA;
            PointB = pointB;
            Size = size;
            RotationDegrees = rotationDegrees;
            Radius = radius;
            VertexStart = vertexStart;
            VertexCount = vertexCount;
            Layer = Mathf.Clamp(layer, 0, 31);
            IsTrigger = isTrigger;
            Density = Mathf.Max(0f, density);
            Friction = Mathf.Max(0f, friction);
            Bounciness = Mathf.Max(0f, bounciness);
            FrictionCombine = frictionCombine;
            BounceCombine = bounceCombine;
        }



        public Physics2DBridgeShapeType Type { get; }
        public Vector2 PointA { get; }
        public Vector2 PointB { get; }
        public Vector2 Size { get; }
        public float RotationDegrees { get; }
        public float Radius { get; }
        public int VertexStart { get; }
        public int VertexCount { get; }
        public int Layer { get; }
        public bool IsTrigger { get; }
        public float Density { get; }
        public float Friction { get; }
        public float Bounciness { get; }
        public PhysicsMaterialCombine2D FrictionCombine { get; }
        public PhysicsMaterialCombine2D BounceCombine { get; }



        public static Physics2DBridgeShapeDescriptor Circle(
            Vector2 center,
            float radius,
            int layer,
            bool isTrigger = false) =>
            new Physics2DBridgeShapeDescriptor(
                Physics2DBridgeShapeType.Circle,
                center,
                default,
                default,
                0f,
                radius,
                0,
                0,
                layer,
                isTrigger);



        public static Physics2DBridgeShapeDescriptor Box(
            Vector2 center,
            Vector2 size,
            float rotationDegrees,
            int layer,
            bool isTrigger = false) =>
            new Physics2DBridgeShapeDescriptor(
                Physics2DBridgeShapeType.Box,
                center,
                default,
                size,
                rotationDegrees,
                0f,
                0,
                0,
                layer,
                isTrigger);



        /// <summary>
        /// 두 중심점과 반경으로 capsule 형상을 만듭니다.
        /// </summary>
        public static Physics2DBridgeShapeDescriptor Capsule(
            Vector2 pointA,
            Vector2 pointB,
            float radius,
            int layer,
            bool isTrigger = false) =>
            new Physics2DBridgeShapeDescriptor(
                Physics2DBridgeShapeType.Capsule,
                pointA,
                pointB,
                default,
                0f,
                radius,
                0,
                0,
                layer,
                isTrigger);



        /// <summary>
        /// 두 끝점과 선택적 edge radius로 segment 형상을 만듭니다.
        /// </summary>
        public static Physics2DBridgeShapeDescriptor Segment(
            Vector2 pointA,
            Vector2 pointB,
            float radius,
            int layer,
            bool isTrigger = false) =>
            new Physics2DBridgeShapeDescriptor(
                Physics2DBridgeShapeType.Segment,
                pointA,
                pointB,
                default,
                0f,
                radius,
                0,
                0,
                layer,
                isTrigger);



        /// <summary>
        /// provider vertex buffer의 연속 범위를 참조하는 convex polygon 형상을 만듭니다.
        /// </summary>
        public static Physics2DBridgeShapeDescriptor Polygon(
            int vertexStart,
            int vertexCount,
            float radius,
            int layer,
            bool isTrigger = false) =>
            new Physics2DBridgeShapeDescriptor(
                Physics2DBridgeShapeType.Polygon,
                default,
                default,
                default,
                0f,
                radius,
                vertexStart,
                vertexCount,
                layer,
                isTrigger);



        /// <summary>
        /// provider vertex buffer의 연속 범위를 연결하는 chain 형상을 만듭니다.
        /// </summary>
        public static Physics2DBridgeShapeDescriptor Chain(
            int vertexStart,
            int vertexCount,
            float radius,
            int layer,
            bool isTrigger = false) =>
            new Physics2DBridgeShapeDescriptor(
                Physics2DBridgeShapeType.Chain,
                default,
                default,
                default,
                0f,
                radius,
                vertexStart,
                vertexCount,
                layer,
                isTrigger);
    }



    /// <summary>
    /// Collider2D가 없는 객체가 main thread 동기화 경계에서 PhysicsCore proxy 형상을 제공하는 계약입니다.
    /// 구현은 전달받은 목록을 비운 뒤 descriptor와 vertex를 채우며, 반환값은 최종 descriptor 수와 같아야 합니다.
    /// </summary>
    public interface IPhysics2DBridgeShapeProvider
    {
        /// <summary>
        /// contact 결과에서 gameplay 대상 identity로 복원할 Unity component입니다.
        /// </summary>
        Component TargetComponent { get; }

        /// <summary>
        /// 형상이 바뀔 때 증가하거나 달라져야 하는 revision입니다.
        /// </summary>
        int GeometryRevision { get; }

        /// <summary>
        /// legacy 쪽이 권위자인 현재 body pose와 velocity를 제공합니다.
        /// </summary>
        bool TryGetBodyState(out Physics2DBridgeBodyState state);

        /// <summary>
        /// body local-space 형상과 vertex를 복사합니다. PhysicsCore Job 안에서는 호출되지 않습니다.
        /// </summary>
        int CopyShapes(
            List<Physics2DBridgeShapeDescriptor> shapes,
            List<Vector2> vertices);
    }



    /// <summary>
    /// PhysicsCore contact의 bridge target id를 원본 Legacy 물리 객체로 복원한 결과입니다.
    /// </summary>
    public readonly struct Physics2DBridgeTarget
    {
        public Physics2DBridgeTarget(
            int targetId,
            Collider2D collider,
            Rigidbody2D rigidbody,
            Component owner,
            int layer,
            bool isTrigger)
        {
            TargetId = targetId;
            Collider = collider;
            Rigidbody = rigidbody;
            Owner = owner;
            Layer = layer;
            IsTrigger = isTrigger;
        }



        public int TargetId { get; }
        public Collider2D Collider { get; }
        public Rigidbody2D Rigidbody { get; }
        public Component Owner { get; }
        public GameObject GameObject => Owner != null ? Owner.gameObject : Collider != null ? Collider.gameObject : null;
        public int Layer { get; }
        public bool IsTrigger { get; }
        public bool IsValid => TargetId > 0 && (Collider != null || Owner != null);
    }
}
