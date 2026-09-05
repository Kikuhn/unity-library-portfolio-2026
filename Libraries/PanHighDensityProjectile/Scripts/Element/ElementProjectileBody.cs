using System;
using System.Collections.Generic;
using Pan.HighDensityElement;
using Unity.Mathematics;
using UnityEngine;



namespace Pan.HighDensityProjectile
{
    /// <summary>
    /// 기존 <see cref="IPanProjectileBody"/> API를 범용 <see cref="ElementWorld"/> 위에서 구현합니다.
    /// </summary>
    public sealed class ElementProjectileBody :
        IPanProjectileBody,
        IProjectileBatchSpawnTarget,
        IProjectileBodySimulationMetrics,
        IDisposable
    {
        private struct ProjectileRecord
        {
            public ElementHandle Element;
            public ProjectileSpawnRequest Spawn;
            public bool ConsumedPending;
        }



        private readonly ElementWorld world;
        private readonly bool ownsWorld;
        private readonly ElementProjectileExecutionModel executionModel;
        private readonly SimulatedProjectileOptions2D simulatedOptions;
        private readonly Dictionary<int, ProjectileRecord> records;
        private readonly Dictionary<ElementKey, int> projectileIdsByElement;
        private IElementProjectileTargetResolver targetResolver;
        private readonly List<IProjectileHitTarget> hitTargetBuffer;
        private readonly List<IProjectileHitTarget> dispatchedHitTargetBuffer;
        private readonly List<int> projectileIdScratch;
        private ElementKey dispatchElement;
        private ushort dispatchSubstep;
        private int nextProjectileId;
        private bool disposed;



        public ElementProjectileBody(int capacity = 512, float spatialCellSize = 1f)
            : this(
                new ElementWorld(capacity, spatialCellSize),
                capacity,
                ownsWorld: true,
                ElementProjectileExecutionModel.Query,
                SimulatedProjectileOptions2D.Default)
        {
        }



        public ElementProjectileBody(ElementWorld world, int capacity = 512)
            : this(
                world,
                capacity,
                ownsWorld: false,
                ElementProjectileExecutionModel.Query,
                SimulatedProjectileOptions2D.Default)
        {
        }



        public ElementProjectileBody(
            ElementProjectileExecutionModel executionModel,
            int capacity = 512,
            float spatialCellSize = 1f,
            SimulatedProjectileOptions2D simulatedOptions = default)
            : this(
                new ElementWorld(capacity, spatialCellSize),
                capacity,
                ownsWorld: true,
                executionModel,
                simulatedOptions)
        {
        }



        public ElementProjectileBody(
            ElementWorld world,
            ElementProjectileExecutionModel executionModel,
            int capacity = 512,
            SimulatedProjectileOptions2D simulatedOptions = default)
            : this(world, capacity, ownsWorld: false, executionModel, simulatedOptions)
        {
        }



        private ElementProjectileBody(
            ElementWorld world,
            int capacity,
            bool ownsWorld,
            ElementProjectileExecutionModel executionModel,
            SimulatedProjectileOptions2D simulatedOptions)
        {
            if (capacity < 1) { throw new ArgumentOutOfRangeException(nameof(capacity)); }

            Capacity = capacity;
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            this.ownsWorld = ownsWorld;
            this.executionModel = executionModel;
            this.simulatedOptions = simulatedOptions.IsValid
                ? simulatedOptions
                : SimulatedProjectileOptions2D.Default;
            world.FactDispatched += OnElementFact;
            records = new Dictionary<int, ProjectileRecord>(capacity);
            projectileIdsByElement = new Dictionary<ElementKey, int>(capacity);
            hitTargetBuffer = new List<IProjectileHitTarget>(8);
            dispatchedHitTargetBuffer = new List<IProjectileHitTarget>(8);
            projectileIdScratch = new List<int>(capacity);
        }



        public int ActiveCount => records.Count;
        public int Capacity { get; private set; }
        public int TotalSpawned { get; private set; }
        public int TotalDespawned { get; private set; }
        public int TotalHits { get; private set; }
        public int LastMovementJobSimulatedCount { get; private set; }
        public ElementWorld ElementWorld => world;
        public ElementProjectileExecutionModel ExecutionModel => executionModel;



        public event ProjectileSnapshotHandler ProjectileSpawned;
        public event ProjectileContactHandler ProjectileContacted;
        public event ProjectileSnapshotHandler ProjectileDespawned;



        /// <summary>
        /// PhysicsCore contact fact를 gameplay target과 receiver로 복원할 resolver를 설정합니다.
        /// resolver가 없거나 false를 반환한 contact는 소비하지 않고 무시합니다.
        /// </summary>
        /// <param name="resolver">consumer가 소유하는 main-thread resolver입니다.</param>
        public void SetTargetResolver(IElementProjectileTargetResolver resolver)
        {
            targetResolver = resolver;
        }



        public void EnsureCapacity(int capacity)
        {
            ThrowIfDisposed();
            if (capacity > Capacity) { Capacity = capacity; }
        }



        public bool TrySpawn(in ProjectileSpawnRequest spawnData, out int projectileId)
        {
            ThrowIfDisposed();
            projectileId = 0;
            if (!spawnData.Use2D || records.Count >= Capacity) { return false; }

            bool usesDynamicBody = executionModel == ElementProjectileExecutionModel.Simulated;
            ElementCapabilities capabilities = ElementCapabilities.Lifetime | ElementCapabilities.SpriteVisual2D;
            capabilities |= usesDynamicBody
                ? ElementCapabilities.DynamicBody2D
                : ElementCapabilities.KinematicMotion2D | ElementCapabilities.QuerySensor2D;
            if (spawnData.QueryableByHybridPhysics2D)
            {
                capabilities |= ElementCapabilities.QueryTarget2D;
            }
            if (!ElementCompiledArchetype.TryCreate(
                    capabilities,
                    spawnData.Radius,
                    spawnData.Lifetime.InitialSeconds,
                    spawnData.ElementVisualId,
                    0f,
                    false,
                    usesDynamicBody ? PhysicsCoreBodyMode.Dynamic : PhysicsCoreBodyMode.Kinematic,
                    usesDynamicBody ? simulatedOptions.Shape : PhysicsCoreShape2D.Circle,
                    out ElementCompiledArchetype archetype,
                    out _))
            {
                return false;
            }

            ElementSpawnBuilder builder = ElementSpawnBuilder.From(archetype)
                .WithPose(
                    new float2(spawnData.Position.x, spawnData.Position.y),
                    new float2(Mathf.Max(0.0001f, spawnData.Visual.FixedWorldScale)))
                .WithVelocity(new float2(spawnData.Velocity.x, spawnData.Velocity.y))
                .WithOwner(
                    spawnData.Source != null
                        ? unchecked((uint)EntityId.ToULong(spawnData.Source.GetEntityId()))
                        : 0u,
                    (uint)Mathf.Max(0, spawnData.TeamId))
                .WithLifetime(spawnData.Lifetime.RemainingSeconds)
                .WithPhysicsFilter(
                    1ul << spawnData.Collision.ObjectLayer,
                    unchecked((uint)spawnData.HitLayers.value));
            if (usesDynamicBody)
            {
                builder.LinearDamping = spawnData.Motion.LinearDamping + spawnData.Motion.FrictionCoefficient;
                builder = builder.WithPhysicsMaterial(simulatedOptions.Material);
            }
            else
            {
                builder = builder.WithMotion(
                    spawnData.Motion.UseGravity
                        ? new float2(spawnData.Motion.Gravity.x, spawnData.Motion.Gravity.y)
                        : float2.zero,
                    spawnData.Motion.LinearDamping + spawnData.Motion.FrictionCoefficient);
            }

            builder = builder.WithStrictCcd(
                spawnData.StrictCcdOverride,
                spawnData.EffectiveStrictCcdThresholdRatio);
            builder = builder.WithTriggerInteraction(spawnData.Collision.IncludeTriggers);

            ElementSpawnResult result = world.Spawn(in builder);
            if (!result.Succeeded) { return false; }

            projectileId = NextProjectileId();
            var record = new ProjectileRecord
            {
                Element = result.Handle,
                Spawn = spawnData
            };
            records.Add(projectileId, record);
            projectileIdsByElement.Add(result.Handle.Key, projectileId);
            TotalSpawned++;

            ProjectileSnapshot snapshot = CreateSnapshot(projectileId, in record);
            ProjectileSpawned?.Invoke(in snapshot);
            return true;
        }



        public int Spawn(in ProjectileSpawnRequest spawnData)
        {
            if (!TrySpawn(in spawnData, out int projectileId))
            {
                throw new InvalidOperationException("Element projectile 생성에 실패했습니다.");
            }

            return projectileId;
        }



        public int TrySpawnBatch(ProjectileSpawnRequest[] spawnDataBuffer, int count, int[] projectileIds = null)
        {
            if (spawnDataBuffer == null) { return 0; }
            int requestCount = Mathf.Min(Mathf.Max(0, count), spawnDataBuffer.Length);
            int spawned = 0;

            for (int i = 0; i < requestCount; i++)
            {
                if (!TrySpawn(in spawnDataBuffer[i], out int projectileId)) { continue; }
                if (projectileIds != null && spawned < projectileIds.Length) { projectileIds[spawned] = projectileId; }
                spawned++;
            }

            return spawned;
        }



        public void Simulate(float deltaTime)
        {
            SimulateFixedStep(deltaTime, 0);
        }



        public void SimulateFixedStep(float deltaTime, ushort substepIndex)
        {
            ThrowIfDisposed();
            PrepareFixedStep(substepIndex);
            world.Tick(deltaTime, substepIndex);
        }



        /// <summary>
        /// 공유 ElementWorld의 단일 Tick 직전에 투사체 도메인의 고정 스텝 상태를 준비합니다.
        /// </summary>
        /// <remarks>
        /// 외부 World 소유자는 이 메서드를 호출한 뒤 같은 substep으로 World를 정확히 한 번 Tick해야 합니다.
        /// </remarks>
        public void PrepareFixedStep(ushort substepIndex)
        {
            ThrowIfDisposed();
            LastMovementJobSimulatedCount = records.Count;
            dispatchElement = default;
            dispatchSubstep = substepIndex;
            dispatchedHitTargetBuffer.Clear();
        }



        public void ClearAll()
        {
            ThrowIfDisposed();
            if (records.Count == 0) { return; }

            projectileIdScratch.Clear();
            foreach (int projectileId in records.Keys) { projectileIdScratch.Add(projectileId); }
            for (int i = 0; i < projectileIdScratch.Count; i++) { Despawn(projectileIdScratch[i]); }
            projectileIdScratch.Clear();
        }



        public bool Despawn(int projectileId)
        {
            if (!records.TryGetValue(projectileId, out ProjectileRecord record)) { return false; }
            return world.TryDespawnImmediately(record.Element.Key);
        }



        public bool TryGetSnapshot(int index, out ProjectileSnapshot snapshot)
        {
            if (index < 0 || index >= records.Count)
            {
                snapshot = default;
                return false;
            }

            int current = 0;
            foreach (KeyValuePair<int, ProjectileRecord> pair in records)
            {
                if (current++ != index) { continue; }
                ProjectileRecord record = pair.Value;
                snapshot = CreateSnapshot(pair.Key, in record);
                return true;
            }

            snapshot = default;
            return false;
        }



        public bool TryGetSnapshotById(int projectileId, out ProjectileSnapshot snapshot)
        {
            if (records.TryGetValue(projectileId, out ProjectileRecord record) && record.Element.IsAlive)
            {
                snapshot = CreateSnapshot(projectileId, in record);
                return true;
            }

            snapshot = default;
            return false;
        }



        public int CopySnapshots(ProjectileSnapshot[] buffer)
        {
            if (buffer == null) { return 0; }
            int copied = 0;
            foreach (KeyValuePair<int, ProjectileRecord> pair in records)
            {
                if (copied >= buffer.Length) { break; }
                ProjectileRecord record = pair.Value;
                buffer[copied++] = CreateSnapshot(pair.Key, in record);
            }

            return copied;
        }



        public bool TryGetElementHandle(int projectileId, out ElementProjectileHandle handle)
        {
            if (records.TryGetValue(projectileId, out ProjectileRecord record) && record.Element.IsAlive)
            {
                handle = new ElementProjectileHandle(new ProjectileHandle(this, projectileId), record.Element);
                return true;
            }

            handle = default;
            return false;
        }



        /// <summary>
        /// projectile id로 엄격 CCD를 일정 고정 스텝 동안 강제 활성화합니다.
        /// </summary>
        public bool RequestStrictCcd(int projectileId, int fixedStepCount)
        {
            return records.TryGetValue(projectileId, out ProjectileRecord record) &&
                record.Element.RequestStrictCcd(fixedStepCount);
        }



        /// <summary>
        /// projectile id의 현재 위치 변경을 순간이동으로 표시합니다.
        /// </summary>
        public bool MarkTeleported(int projectileId)
        {
            return records.TryGetValue(projectileId, out ProjectileRecord record) &&
                record.Element.MarkTeleported();
        }



        public void Dispose()
        {
            if (disposed) { return; }
            ClearAll();
            world.FactDispatched -= OnElementFact;
            if (ownsWorld) { world.Dispose(); }
            records.Clear();
            projectileIdsByElement.Clear();
            hitTargetBuffer.Clear();
            dispatchedHitTargetBuffer.Clear();
            projectileIdScratch.Clear();
            disposed = true;
        }



        private void OnElementFact(in ElementFact fact)
        {
            if (!projectileIdsByElement.TryGetValue(fact.Element, out int projectileId) ||
                !records.TryGetValue(projectileId, out ProjectileRecord record))
            {
                return;
            }

            if (fact.Type == ElementFactType.Contact)
            {
                DispatchContact(projectileId, in record, in fact);
                return;
            }

            if (fact.Type != ElementFactType.Despawned) { return; }

            ProjectileSnapshot snapshot = CreateSnapshot(projectileId, in record);
            records.Remove(projectileId);
            projectileIdsByElement.Remove(fact.Element);
            TotalDespawned++;
            ProjectileDespawned?.Invoke(in snapshot);
        }



        private void DispatchContact(int projectileId, in ProjectileRecord record, in ElementFact fact)
        {
            if (record.ConsumedPending) { return; }

            hitTargetBuffer.Clear();
            Component targetComponent;
            Collider2D collider;
            if (targetResolver == null ||
                !targetResolver.TryResolveTargets(
                    in fact,
                    hitTargetBuffer,
                    out targetComponent,
                    out collider))
            {
                return;
            }

            if (!record.Spawn.Collision.IncludeTriggers && collider != null && collider.isTrigger)
            {
                return;
            }

            if (IsSourceTarget(record.Spawn.Source, targetComponent, collider)) { return; }

            if (dispatchElement != fact.Element || dispatchSubstep != fact.SubstepIndex)
            {
                dispatchElement = fact.Element;
                dispatchSubstep = fact.SubstepIndex;
                dispatchedHitTargetBuffer.Clear();
            }

            Vector3 direction = Vector3.zero;
            if (record.Element.TryGetSnapshot(out ElementSnapshot currentElement))
            {
                direction = new Vector3(currentElement.Velocity.x, currentElement.Velocity.y, 0f).normalized;
            }

            var hit = new ProjectileHitPayload(
                projectileId,
                record.Spawn.Source,
                null,
                null,
                collider,
                new Vector3(fact.Position.x, fact.Position.y, 0f),
                new Vector3(fact.Normal.x, fact.Normal.y, 0f),
                direction,
                record.Spawn.Damage,
                record.Spawn.TeamId,
                true,
                targetComponent,
                targetComponent != null
                    ? targetComponent.gameObject
                    : collider != null
                        ? collider.gameObject
                        : null);

            bool receiverFound = false;
            bool consumed = false;
            int targetCount = 0;
            for (int i = 0; i < hitTargetBuffer.Count; i++)
            {
                IProjectileHitTarget target = hitTargetBuffer[i];
                if (target == null) { continue; }
                receiverFound = true;
                if (dispatchedHitTargetBuffer.Contains(target)) { continue; }

                dispatchedHitTargetBuffer.Add(target);
                targetCount++;
                consumed |= target.TryReceiveProjectileHit(in hit);
            }

            if (!receiverFound && record.Spawn.Collision.ConsumeHitWithoutReceiver)
            {
                consumed = true;
            }

            if (targetCount > 0 || consumed)
            {
                ProjectileContactPayload contact = new ProjectileContactPayload(hit, consumed, targetCount);
                ProjectileContacted?.Invoke(in contact);
            }

            if (!consumed) { return; }

            ProjectileRecord updated = record;
            updated.ConsumedPending = true;
            records[projectileId] = updated;
            TotalHits++;
            var despawn = new DespawnElement();
            updated.Element.TrySubmit(in despawn);
        }



        private ProjectileSnapshot CreateSnapshot(int projectileId, in ProjectileRecord record)
        {
            if (!record.Element.TryGetSnapshot(out ElementSnapshot element)) { return default; }

            return new ProjectileSnapshot(
                projectileId,
                new Vector3(element.Position.x, element.Position.y, record.Spawn.Position.z),
                new Vector3(element.Velocity.x, element.Velocity.y, record.Spawn.Velocity.z),
                element.Radius,
                element.RemainingLifetime,
                record.Spawn.Damage,
                record.Spawn.TeamId,
                true,
                record.Spawn.VisualFrameIndex,
                record.Spawn.Lifetime.InitialSeconds,
                record.Spawn.Motion);
        }



        private static bool IsSourceTarget(
            Component source,
            Component targetComponent,
            Collider2D collider)
        {
            if (source == null) { return false; }
            Transform sourceTransform = source.transform;
            Transform targetTransform = targetComponent != null
                ? targetComponent.transform
                : collider != null
                    ? collider.transform
                    : null;
            return targetTransform != null &&
                (targetTransform == sourceTransform || targetTransform.IsChildOf(sourceTransform));
        }



        private int NextProjectileId()
        {
            do
            {
                nextProjectileId = nextProjectileId == int.MaxValue ? 1 : nextProjectileId + 1;
            }
            while (records.ContainsKey(nextProjectileId));
            return nextProjectileId;
        }



        private void ThrowIfDisposed()
        {
            if (disposed) { throw new ObjectDisposedException(nameof(ElementProjectileBody)); }
        }
    }
}
