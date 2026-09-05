using System;
using Unity.Collections;
using Unity.U2D.Physics;



namespace Pan.HighDensityElement
{
    /// <summary>
    /// Element query, area sensor, dynamic body가 공유하는 전용 Physics Core 2D world입니다.
    /// </summary>
    public sealed partial class PhysicsCore2DLane : IDisposable
    {
        private NativeList<MovingProjectionShape2D> movingProjections;
        private readonly int worldId;
        private PhysicsWorld world;
        private bool disposed;



        public PhysicsCore2DLane()
            : this(0)
        {
        }



        internal PhysicsCore2DLane(int worldId)
        {
            this.worldId = worldId;
            PhysicsWorldDefinition definition = PhysicsWorldDefinition.defaultDefinition;
            definition.continuousAllowed = true;
            definition.transformWriteMode = PhysicsWorld.TransformWriteMode.Off;
            world = PhysicsWorld.Create(definition);
            movingProjections = new NativeList<MovingProjectionShape2D>(16, Allocator.Persistent);
        }



        public PhysicsWorld World => world;
        public bool IsValid => !disposed && world.isValid;
        public int BodyCount => bodies.Count;
        public int BodyBatchCreateCount { get; private set; }
        public int BodyBatchDestroyCount { get; private set; }
        public int MovingProjectionCount => movingProjections.IsCreated ? movingProjections.Length : 0;
        public long MovingProjectionSynchronizationSequence { get; private set; } = -1;



        /// <summary>
        /// 다음 World Tick의 엄격 CCD가 사용할 외부 Projection 이동 형상 전체를 교체합니다.
        /// </summary>
        public void ReplaceMovingProjections(
            ReadOnlySpan<MovingProjectionShape2D> projections,
            long synchronizationSequence)
        {
            if (!IsValid) { throw new ObjectDisposedException(nameof(PhysicsCore2DLane)); }
            if (synchronizationSequence < MovingProjectionSynchronizationSequence)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(synchronizationSequence),
                    "이전 동기화 순서보다 오래된 Projection 입력은 적용할 수 없습니다.");
            }

            movingProjections.Clear();
            if (movingProjections.Capacity < projections.Length)
            {
                movingProjections.Capacity = projections.Length;
            }

            for (int i = 0; i < projections.Length; i++)
            {
                MovingProjectionShape2D projection = projections[i];
                if (projection.IsValid) { movingProjections.Add(projection); }
            }

            movingProjections.Sort();
            MovingProjectionSynchronizationSequence = synchronizationSequence;
        }



        internal NativeArray<MovingProjectionShape2D> GetMovingProjections() =>
            movingProjections.IsCreated ? movingProjections.AsArray() : default;



        public void Simulate(float deltaTime)
        {
            if (!IsValid || deltaTime <= 0f) { return; }
            world.Simulate(deltaTime);
        }



        public void Dispose()
        {
            if (disposed) { return; }

            if (bodies.Count > 0 && world.isValid)
            {
                using var removedBodies = new NativeList<PhysicsBody>(bodies.Count, Allocator.Temp);
                foreach (PhysicsBody body in bodies.Values)
                {
                    if (body.isValid) { removedBodies.Add(body); }
                }
                if (removedBodies.Length > 0)
                {
                    PhysicsWorld.DestroyBodyBatch(removedBodies.AsArray().AsReadOnlySpan());
                    BodyBatchDestroyCount++;
                }
            }

            bodies.Clear();
            areaElements.Clear();
            dynamicElements.Clear();
            activeAreaPairs.Clear();
            enteredAreaPairs.Clear();
            emittedContactPairs.Clear();
            activeDynamicContacts.Clear();
            currentDynamicContacts.Clear();
            activeDynamicTriggerPairs.Clear();
            enteredDynamicTriggerPairs.Clear();
            pairScratch.Clear();
            contactPairScratch.Clear();
            if (movingProjections.IsCreated) { movingProjections.Dispose(); }
            if (world.isValid) { world.Destroy(); }
            disposed = true;
        }



    }
}
