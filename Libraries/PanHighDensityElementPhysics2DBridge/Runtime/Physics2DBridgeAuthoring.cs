using System;
using System.Collections.Generic;
using UnityEngine;



namespace Pan.HighDensityElement.Physics2DBridge
{
    /// <summary>
    /// 선택된 Physics2D bridge를 Scene View에 어떤 시간대의 pose로 표시할지 결정합니다.
    /// </summary>
    public enum Physics2DBridgeDebugDrawMode
    {
        Off = 0,
        Presentation = 1,
        RawPhysics = 2,
        Both = 3
    }



    /// <summary>
    /// Collider2D와 독립적으로 작성하는 PhysicsCore2D proxy 형상입니다.
    /// 좌표와 크기는 선택된 pose source의 body-local 단위를 사용합니다.
    /// </summary>
    [Serializable]
    public sealed class Physics2DBridgeManualShape2D
    {
        [SerializeField]
        private Physics2DBridgeShapeType shapeType = Physics2DBridgeShapeType.Capsule;

        [SerializeField]
        private Vector2 center;

        [SerializeField]
        private Vector2 size = Vector2.one;

        [SerializeField, Min(0f)]
        private float radius = 0.5f;

        [SerializeField]
        private float rotationDegrees;

        [SerializeField]
        private CapsuleDirection2D capsuleDirection = CapsuleDirection2D.Vertical;

        [SerializeField]
        private Vector2 pointA = Vector2.left * 0.5f;

        [SerializeField]
        private Vector2 pointB = Vector2.right * 0.5f;

        [SerializeField]
        private Vector2[] vertices = Array.Empty<Vector2>();

        [SerializeField, Range(0, 31)]
        private int layer;

        [SerializeField]
        private bool isTrigger;

        [SerializeField, Min(0f)]
        private float density = 1f;

        [SerializeField, Min(0f)]
        private float friction = 0.4f;

        [SerializeField, Min(0f)]
        private float bounciness;

        [SerializeField]
        private PhysicsMaterialCombine2D frictionCombine = PhysicsMaterialCombine2D.Average;

        [SerializeField]
        private PhysicsMaterialCombine2D bounceCombine = PhysicsMaterialCombine2D.Average;

        [SerializeField]
        private PhysicsMaterial2D material;



        public Physics2DBridgeShapeType ShapeType
        {
            get => shapeType;
            set => shapeType = value;
        }

        public Vector2 Center
        {
            get => center;
            set => center = value;
        }

        public Vector2 Size
        {
            get => size;
            set => size = value;
        }

        public float Radius
        {
            get => radius;
            set => radius = Mathf.Max(0f, value);
        }

        public float RotationDegrees
        {
            get => rotationDegrees;
            set => rotationDegrees = value;
        }

        public CapsuleDirection2D CapsuleDirection
        {
            get => capsuleDirection;
            set => capsuleDirection = value;
        }

        public Vector2 PointA
        {
            get => pointA;
            set => pointA = value;
        }

        public Vector2 PointB
        {
            get => pointB;
            set => pointB = value;
        }

        public IReadOnlyList<Vector2> Vertices => vertices;

        public int Layer
        {
            get => layer;
            set => layer = Mathf.Clamp(value, 0, 31);
        }

        public bool IsTrigger
        {
            get => isTrigger;
            set => isTrigger = value;
        }

        public PhysicsMaterial2D Material
        {
            get => material;
            set => material = value;
        }



        /// <summary>
        /// 게임플레이용 원형 형상을 만듭니다.
        /// </summary>
        public static Physics2DBridgeManualShape2D Circle(
            Vector2 center,
            float radius,
            int layer,
            bool isTrigger = false)
        {
            return new Physics2DBridgeManualShape2D
            {
                shapeType = Physics2DBridgeShapeType.Circle,
                center = center,
                radius = Mathf.Max(0f, radius),
                layer = Mathf.Clamp(layer, 0, 31),
                isTrigger = isTrigger
            };
        }



        /// <summary>
        /// 게임플레이용 사각형 형상을 만듭니다.
        /// </summary>
        public static Physics2DBridgeManualShape2D Box(
            Vector2 center,
            Vector2 size,
            float rotationDegrees,
            int layer,
            bool isTrigger = false)
        {
            return new Physics2DBridgeManualShape2D
            {
                shapeType = Physics2DBridgeShapeType.Box,
                center = center,
                size = size,
                rotationDegrees = rotationDegrees,
                layer = Mathf.Clamp(layer, 0, 31),
                isTrigger = isTrigger,
                radius = 0f
            };
        }



        /// <summary>
        /// 중심, 전체 크기와 방향으로 게임플레이용 Capsule을 만듭니다.
        /// </summary>
        public static Physics2DBridgeManualShape2D Capsule(
            Vector2 center,
            Vector2 size,
            CapsuleDirection2D direction,
            float rotationDegrees,
            int layer,
            bool isTrigger = false)
        {
            return new Physics2DBridgeManualShape2D
            {
                shapeType = Physics2DBridgeShapeType.Capsule,
                center = center,
                size = size,
                capsuleDirection = direction,
                rotationDegrees = rotationDegrees,
                layer = Mathf.Clamp(layer, 0, 31),
                isTrigger = isTrigger
            };
        }



        /// <summary>
        /// 두 끝점으로 segment 형상을 만듭니다.
        /// </summary>
        public static Physics2DBridgeManualShape2D Segment(
            Vector2 pointA,
            Vector2 pointB,
            float radius,
            int layer,
            bool isTrigger = false)
        {
            return new Physics2DBridgeManualShape2D
            {
                shapeType = Physics2DBridgeShapeType.Segment,
                pointA = pointA,
                pointB = pointB,
                radius = Mathf.Max(0f, radius),
                layer = Mathf.Clamp(layer, 0, 31),
                isTrigger = isTrigger
            };
        }



        /// <summary>
        /// body-local 정점으로 Polygon 또는 Chain 형상을 만듭니다.
        /// </summary>
        public static Physics2DBridgeManualShape2D VerticesShape(
            Physics2DBridgeShapeType type,
            IReadOnlyList<Vector2> sourceVertices,
            Vector2 center,
            float rotationDegrees,
            float radius,
            int layer,
            bool isTrigger = false)
        {
            if (type != Physics2DBridgeShapeType.Polygon && type != Physics2DBridgeShapeType.Chain)
            {
                throw new ArgumentOutOfRangeException(nameof(type), type, "Polygon 또는 Chain만 사용할 수 있습니다.");
            }

            return new Physics2DBridgeManualShape2D
            {
                shapeType = type,
                vertices = CopyVertices(sourceVertices),
                center = center,
                rotationDegrees = rotationDegrees,
                radius = Mathf.Max(0f, radius),
                layer = Mathf.Clamp(layer, 0, 31),
                isTrigger = isTrigger
            };
        }



        internal Physics2DBridgeManualShape2D Clone()
        {
            return new Physics2DBridgeManualShape2D
            {
                shapeType = shapeType,
                center = center,
                size = size,
                radius = radius,
                rotationDegrees = rotationDegrees,
                capsuleDirection = capsuleDirection,
                pointA = pointA,
                pointB = pointB,
                vertices = CopyVertices(vertices),
                layer = layer,
                isTrigger = isTrigger,
                density = density,
                friction = friction,
                bounciness = bounciness,
                frictionCombine = frictionCombine,
                bounceCombine = bounceCombine,
                material = material
            };
        }



        internal bool IsValid
        {
            get
            {
                Vector2 absoluteSize = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
                switch (shapeType)
                {
                    case Physics2DBridgeShapeType.Circle:
                        return radius > 0f;

                    case Physics2DBridgeShapeType.Box:
                    case Physics2DBridgeShapeType.Capsule:
                        return absoluteSize.x > 0f && absoluteSize.y > 0f;

                    case Physics2DBridgeShapeType.Segment:
                        return (pointB - pointA).sqrMagnitude > 0.0000001f;

                    case Physics2DBridgeShapeType.Polygon:
                        return vertices != null && vertices.Length >= 3;

                    case Physics2DBridgeShapeType.Chain:
                        return vertices != null && vertices.Length >= 2;

                    default:
                        return false;
                }
            }
        }



        internal bool TryAppendDescriptor(
            List<Physics2DBridgeShapeDescriptor> shapes,
            List<Vector2> destinationVertices)
        {
            if (shapes == null || destinationVertices == null) { return false; }

            float resolvedFriction = material != null ? material.friction : friction;
            float resolvedBounciness = material != null ? material.bounciness : bounciness;
            PhysicsMaterialCombine2D resolvedFrictionCombine = material != null
                ? material.frictionCombine
                : frictionCombine;
            PhysicsMaterialCombine2D resolvedBounceCombine = material != null
                ? material.bounceCombine
                : bounceCombine;

            Physics2DBridgeShapeDescriptor descriptor;
            Vector2 absoluteSize = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
            switch (shapeType)
            {
                case Physics2DBridgeShapeType.Circle:
                    if (radius <= 0f) { return false; }
                    descriptor = CreateDescriptor(shapeType, center, default, default, 0f, radius, 0, 0,
                        resolvedFriction, resolvedBounciness, resolvedFrictionCombine, resolvedBounceCombine);
                    break;

                case Physics2DBridgeShapeType.Box:
                    if (absoluteSize.x <= 0f || absoluteSize.y <= 0f) { return false; }
                    float boxRadius = Mathf.Clamp(radius, 0f, Mathf.Min(absoluteSize.x, absoluteSize.y) * 0.5f);
                    descriptor = CreateDescriptor(shapeType, center, default, absoluteSize, rotationDegrees, boxRadius, 0, 0,
                        resolvedFriction, resolvedBounciness, resolvedFrictionCombine, resolvedBounceCombine);
                    break;

                case Physics2DBridgeShapeType.Capsule:
                    if (absoluteSize.x <= 0f || absoluteSize.y <= 0f) { return false; }
                    bool vertical = capsuleDirection == CapsuleDirection2D.Vertical;
                    float capsuleRadius = (vertical ? absoluteSize.x : absoluteSize.y) * 0.5f;
                    float fullLength = vertical ? absoluteSize.y : absoluteSize.x;
                    float halfSegment = Mathf.Max(0f, fullLength * 0.5f - capsuleRadius);
                    Vector2 axis = Rotate(vertical ? Vector2.up : Vector2.right, rotationDegrees) * halfSegment;
                    descriptor = CreateDescriptor(shapeType, center - axis, center + axis, default, 0f, capsuleRadius, 0, 0,
                        resolvedFriction, resolvedBounciness, resolvedFrictionCombine, resolvedBounceCombine);
                    break;

                case Physics2DBridgeShapeType.Segment:
                    Vector2 segmentA = center + Rotate(pointA, rotationDegrees);
                    Vector2 segmentB = center + Rotate(pointB, rotationDegrees);
                    if ((segmentB - segmentA).sqrMagnitude <= 0.0000001f) { return false; }
                    descriptor = CreateDescriptor(shapeType, segmentA, segmentB, default, 0f, Mathf.Max(0f, radius), 0, 0,
                        resolvedFriction, resolvedBounciness, resolvedFrictionCombine, resolvedBounceCombine);
                    break;

                case Physics2DBridgeShapeType.Polygon:
                case Physics2DBridgeShapeType.Chain:
                    int minimum = shapeType == Physics2DBridgeShapeType.Polygon ? 3 : 2;
                    if (vertices == null || vertices.Length < minimum) { return false; }
                    int vertexStart = destinationVertices.Count;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        destinationVertices.Add(center + Rotate(vertices[i], rotationDegrees));
                    }
                    descriptor = CreateDescriptor(shapeType, default, default, default, 0f, Mathf.Max(0f, radius),
                        vertexStart, vertices.Length, resolvedFriction, resolvedBounciness,
                        resolvedFrictionCombine, resolvedBounceCombine);
                    break;

                default:
                    return false;
            }

            shapes.Add(descriptor);
            return true;
        }



        private Physics2DBridgeShapeDescriptor CreateDescriptor(
            Physics2DBridgeShapeType type,
            Vector2 descriptorPointA,
            Vector2 descriptorPointB,
            Vector2 descriptorSize,
            float descriptorRotation,
            float descriptorRadius,
            int vertexStart,
            int vertexCount,
            float resolvedFriction,
            float resolvedBounciness,
            PhysicsMaterialCombine2D resolvedFrictionCombine,
            PhysicsMaterialCombine2D resolvedBounceCombine)
        {
            return new Physics2DBridgeShapeDescriptor(
                type,
                descriptorPointA,
                descriptorPointB,
                descriptorSize,
                descriptorRotation,
                descriptorRadius,
                vertexStart,
                vertexCount,
                layer,
                isTrigger,
                density,
                resolvedFriction,
                resolvedBounciness,
                resolvedFrictionCombine,
                resolvedBounceCombine);
        }



        private static Vector2 Rotate(Vector2 value, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
        }



        private static Vector2[] CopyVertices(IReadOnlyList<Vector2> source)
        {
            if (source == null || source.Count == 0) { return Array.Empty<Vector2>(); }
            var copy = new Vector2[source.Count];
            for (int i = 0; i < copy.Length; i++) { copy[i] = source[i]; }
            return copy;
        }
    }



    /// <summary>
    /// 풀링 factory가 직렬화 기본값을 안전하게 덮어쓸 때 사용하는 불변 설정 묶음입니다.
    /// 브리지 컴포넌트는 전달된 목록을 복사하므로 호출자 목록의 이후 변경에 의존하지 않습니다.
    /// </summary>
    public readonly struct Physics2DBridgeRuntimeConfiguration
    {
        public Physics2DBridgeRuntimeConfiguration(
            Physics2DBridgeShapeSourceMode shapeSourceMode,
            IReadOnlyList<Collider2D> selectedColliders = null,
            IReadOnlyList<Physics2DBridgeManualShape2D> manualShapes = null,
            Rigidbody2D poseRigidbody = null,
            Transform poseTransform = null,
            Component targetComponent = null,
            Physics2DBridgeManualBodyMode manualBodyMode = Physics2DBridgeManualBodyMode.Auto,
            IPhysics2DBridgeShapeProvider provider = null)
        {
            ShapeSourceMode = shapeSourceMode;
            SelectedColliders = selectedColliders;
            ManualShapes = manualShapes;
            PoseRigidbody = poseRigidbody;
            PoseTransform = poseTransform;
            TargetComponent = targetComponent;
            ManualBodyMode = manualBodyMode;
            Provider = provider;
        }



        public Physics2DBridgeShapeSourceMode ShapeSourceMode { get; }
        public IReadOnlyList<Collider2D> SelectedColliders { get; }
        public IReadOnlyList<Physics2DBridgeManualShape2D> ManualShapes { get; }
        public Rigidbody2D PoseRigidbody { get; }
        public Transform PoseTransform { get; }
        public Component TargetComponent { get; }
        public Physics2DBridgeManualBodyMode ManualBodyMode { get; }
        public IPhysics2DBridgeShapeProvider Provider { get; }
    }



    /// <summary>
    /// Editor와 Development Build에서 Legacy, Core와 표시 보간 pose를 비교하는 값형식 진단 정보입니다.
    /// </summary>
    public readonly struct Physics2DBridgePoseDiagnostics
    {
        internal Physics2DBridgePoseDiagnostics(
            Physics2DBridgeRegistrationHandle registration,
            int bodyIndex,
            Physics2DBridgeShapeSourceMode shapeSourceMode,
            Vector2 legacyPosition,
            float legacyRotationDegrees,
            Vector2 corePosition,
            float coreRotationDegrees,
            Vector2 interpolatedPosition,
            float interpolatedRotationDegrees,
            Vector2 presentationPosition,
            float presentationRotationDegrees,
            int targetCount,
            int shapeCount,
            bool enabled,
            long synchronizationSequence)
        {
            Registration = registration;
            BodyIndex = bodyIndex;
            ShapeSourceMode = shapeSourceMode;
            LegacyPosition = legacyPosition;
            LegacyRotationDegrees = legacyRotationDegrees;
            CorePosition = corePosition;
            CoreRotationDegrees = coreRotationDegrees;
            InterpolatedPosition = interpolatedPosition;
            InterpolatedRotationDegrees = interpolatedRotationDegrees;
            PresentationPosition = presentationPosition;
            PresentationRotationDegrees = presentationRotationDegrees;
            TargetCount = targetCount;
            ShapeCount = shapeCount;
            Enabled = enabled;
            SynchronizationSequence = synchronizationSequence;
        }



        public Physics2DBridgeRegistrationHandle Registration { get; }
        public int BodyIndex { get; }
        public Physics2DBridgeShapeSourceMode ShapeSourceMode { get; }
        public Vector2 LegacyPosition { get; }
        public float LegacyRotationDegrees { get; }
        public Vector2 CorePosition { get; }
        public float CoreRotationDegrees { get; }
        public Vector2 InterpolatedPosition { get; }
        public float InterpolatedRotationDegrees { get; }



        /// <summary>
        /// 화면 표시와 같은 시간대에 맞춘 진단용 위치입니다.
        /// </summary>
        public Vector2 PresentationPosition { get; }



        /// <summary>
        /// 화면 표시와 같은 시간대에 맞춘 진단용 회전입니다.
        /// </summary>
        public float PresentationRotationDegrees { get; }
        public int TargetCount { get; }



        /// <summary>
        /// Core Shape 생성 직후 캐시한 실제 진단 형상 수입니다.
        /// </summary>
        public int ShapeCount { get; }
        public bool Enabled { get; }



        /// <summary>
        /// 이 Body가 마지막으로 Legacy pose를 투영받은 bridge 동기화 순번입니다.
        /// </summary>
        public long SynchronizationSequence { get; }
        public Vector2 LegacyToCoreDelta => CorePosition - LegacyPosition;
    }
}
