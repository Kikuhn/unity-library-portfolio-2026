#if UNITY_EDITOR || DEVELOPMENT_BUILD

using System;



namespace Pan.HighDensityElement
{
    /// <summary>
    /// ElementWorld가 소유한 공용 native 저장소와 선택형 sparse feature 저장소의 현재 사용량입니다.
    /// </summary>
    public readonly struct ElementWorldStorageDiagnostics
    {
        public ElementWorldStorageDiagnostics(
            int registrySlotCount,
            int registrySlotCapacity,
            int poseCount,
            int poseCapacity,
            int lifetimeCount,
            int lifetimeCapacity,
            int localClockCount,
            int localClockCapacity)
        {
            RegistrySlotCount = registrySlotCount;
            RegistrySlotCapacity = registrySlotCapacity;
            PoseCount = poseCount;
            PoseCapacity = poseCapacity;
            LifetimeCount = lifetimeCount;
            LifetimeCapacity = lifetimeCapacity;
            LocalClockCount = localClockCount;
            LocalClockCapacity = localClockCapacity;
        }



        /// <summary>
        /// 생성 세대와 dense 위치를 보관하는 registry slot의 사용 개수입니다.
        /// </summary>
        public int RegistrySlotCount { get; }

        /// <summary>
        /// 재할당 없이 보관할 수 있는 registry slot의 현재 용량입니다.
        /// </summary>
        public int RegistrySlotCapacity { get; }

        /// <summary>
        /// 살아 있는 Element가 사용하는 pose 항목 수입니다.
        /// </summary>
        public int PoseCount { get; }

        /// <summary>
        /// 재할당 없이 보관할 수 있는 pose 항목의 현재 용량입니다.
        /// </summary>
        public int PoseCapacity { get; }

        /// <summary>
        /// Lifetime feature가 실제로 할당된 Element 수입니다.
        /// </summary>
        public int LifetimeCount { get; }

        /// <summary>
        /// Lifetime sparse 저장소가 재할당 없이 보관할 수 있는 현재 용량입니다.
        /// </summary>
        public int LifetimeCapacity { get; }

        /// <summary>
        /// LocalClock feature가 실제로 할당된 Element 수입니다.
        /// </summary>
        public int LocalClockCount { get; }

        /// <summary>
        /// LocalClock sparse 저장소가 재할당 없이 보관할 수 있는 현재 용량입니다.
        /// </summary>
        public int LocalClockCapacity { get; }
    }



    /// <summary>
    /// 한 실행 Lane이 사용하는 dense native 저장소의 현재 크기와 용량입니다.
    /// </summary>
    public readonly struct ElementLaneStorageDiagnostics
    {
        public ElementLaneStorageDiagnostics(
            ElementLane lane,
            int denseCount,
            int denseCapacity,
            int poseCount,
            int poseCapacity,
            bool usesPhysicsCoreBodies)
        {
            Lane = lane;
            DenseCount = denseCount;
            DenseCapacity = denseCapacity;
            PoseCount = poseCount;
            PoseCapacity = poseCapacity;
            UsesPhysicsCoreBodies = usesPhysicsCoreBodies;
        }



        public ElementLane Lane { get; }
        public int DenseCount { get; }
        public int DenseCapacity { get; }
        public int PoseCount { get; }
        public int PoseCapacity { get; }
        public bool UsesPhysicsCoreBodies { get; }
    }



    /// <summary>
    /// Element가 현재 차지하는 native 저장 위치와 선택형 storage 상태를 나타냅니다.
    /// </summary>
    public readonly struct ElementDebugStorageMetadata : IEquatable<ElementDebugStorageMetadata>
    {
        public ElementDebugStorageMetadata(
            ElementLane lane,
            int laneDenseIndex,
            int poseDenseIndex,
            ulong structuralRevision,
            bool hasLifetimeFeature,
            int lifetimeDenseIndex,
            bool hasLocalClock,
            int localClockDenseIndex,
            bool hasPhysicsBody,
            int physicsBodyHandleIndex,
            ushort physicsBodyHandleGeneration,
            int physicsShapeHandleIndex,
            ushort physicsShapeHandleGeneration)
        {
            Lane = lane;
            LaneDenseIndex = laneDenseIndex;
            PoseDenseIndex = poseDenseIndex;
            StructuralRevision = structuralRevision;
            HasLifetimeFeature = hasLifetimeFeature;
            LifetimeDenseIndex = lifetimeDenseIndex;
            HasLocalClock = hasLocalClock;
            LocalClockDenseIndex = localClockDenseIndex;
            HasPhysicsBody = hasPhysicsBody;
            PhysicsBodyHandleIndex = physicsBodyHandleIndex;
            PhysicsBodyHandleGeneration = physicsBodyHandleGeneration;
            PhysicsShapeHandleIndex = physicsShapeHandleIndex;
            PhysicsShapeHandleGeneration = physicsShapeHandleGeneration;
        }



        public ElementLane Lane { get; }
        public int LaneDenseIndex { get; }
        public int PoseDenseIndex { get; }
        public ulong StructuralRevision { get; }
        public bool HasLifetimeFeature { get; }
        public int LifetimeDenseIndex { get; }
        public bool HasLocalClock { get; }
        public int LocalClockDenseIndex { get; }
        public bool HasPhysicsBody { get; }
        public int PhysicsBodyHandleIndex { get; }
        public ushort PhysicsBodyHandleGeneration { get; }
        public int PhysicsShapeHandleIndex { get; }
        public ushort PhysicsShapeHandleGeneration { get; }



        public bool Equals(ElementDebugStorageMetadata other) =>
            Lane == other.Lane &&
            LaneDenseIndex == other.LaneDenseIndex &&
            PoseDenseIndex == other.PoseDenseIndex &&
            StructuralRevision == other.StructuralRevision &&
            HasLifetimeFeature == other.HasLifetimeFeature &&
            LifetimeDenseIndex == other.LifetimeDenseIndex &&
            HasLocalClock == other.HasLocalClock &&
            LocalClockDenseIndex == other.LocalClockDenseIndex &&
            HasPhysicsBody == other.HasPhysicsBody &&
            PhysicsBodyHandleIndex == other.PhysicsBodyHandleIndex &&
            PhysicsBodyHandleGeneration == other.PhysicsBodyHandleGeneration &&
            PhysicsShapeHandleIndex == other.PhysicsShapeHandleIndex &&
            PhysicsShapeHandleGeneration == other.PhysicsShapeHandleGeneration;

        public override bool Equals(object obj) =>
            obj is ElementDebugStorageMetadata other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add((int)Lane);
            hash.Add(LaneDenseIndex);
            hash.Add(PoseDenseIndex);
            hash.Add(StructuralRevision);
            hash.Add(HasLifetimeFeature);
            hash.Add(LifetimeDenseIndex);
            hash.Add(HasLocalClock);
            hash.Add(LocalClockDenseIndex);
            hash.Add(HasPhysicsBody);
            hash.Add(PhysicsBodyHandleIndex);
            hash.Add(PhysicsBodyHandleGeneration);
            hash.Add(PhysicsShapeHandleIndex);
            hash.Add(PhysicsShapeHandleGeneration);
            return hash.ToHashCode();
        }
    }



    /// <summary>
    /// gameplay snapshot과 선택형 native feature 진단 값을 한 번에 복사한 읽기 전용 값입니다.
    /// </summary>
    public readonly struct ElementDebugSnapshot
    {
        public ElementDebugSnapshot(
            in ElementSnapshot snapshot,
            in ElementPose2D pose,
            in ElementDebugStorageMetadata storage,
            bool hasLifetimeFeature,
            in ElementLifetimeFeature lifetime,
            bool hasLocalClock,
            in ElementLocalClock localClock)
        {
            Snapshot = snapshot;
            Pose = pose;
            Storage = storage;
            HasLifetimeFeature = hasLifetimeFeature;
            Lifetime = lifetime;
            HasLocalClock = hasLocalClock;
            LocalClock = localClock;
        }



        public ElementSnapshot Snapshot { get; }
        public ElementPose2D Pose { get; }
        public ElementDebugStorageMetadata Storage { get; }
        public bool HasLifetimeFeature { get; }
        public ElementLifetimeFeature Lifetime { get; }
        public bool HasLocalClock { get; }
        public ElementLocalClock LocalClock { get; }
        public ElementKey Key => Snapshot.Key;
    }
}

#endif
