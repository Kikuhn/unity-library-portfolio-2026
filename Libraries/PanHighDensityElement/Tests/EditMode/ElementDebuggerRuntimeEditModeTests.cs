using System.Collections.Generic;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;



namespace Pan.HighDensityElement.Tests
{
    public sealed class ElementDebuggerRuntimeEditModeTests
    {
        [Test]
        public void StructuralRevision_ChangesOnlyWhenStorageStructureChanges()
        {
            using var world = new ElementWorld(4);
            ElementHandle handle = world.Spawn(
                ElementSpawnBuilder.From(CreateArchetype(ElementCapabilities.KinematicMotion2D)))
                .Handle;
            ulong afterSpawn = world.StructuralRevision;

            Assert.IsTrue(handle.TrySubmit(new SetElementVelocity2D(new float2(3f, 4f))));
            world.Tick(0f);
            Assert.AreEqual(afterSpawn, world.StructuralRevision);

            var lifetime = new ElementLifetimeFeature(
                2f,
                ElementUpdateSchedule.UpdateSeconds());
            Assert.IsTrue(world.TrySetLifetime(handle.Key, in lifetime));
            ulong afterLifetimeAdded = world.StructuralRevision;
            Assert.Greater(afterLifetimeAdded, afterSpawn);

            lifetime = new ElementLifetimeFeature(
                1f,
                lifetime.Schedule,
                lifetime.UseLocalClock);
            Assert.IsTrue(world.TrySetLifetime(handle.Key, in lifetime));
            Assert.AreEqual(afterLifetimeAdded, world.StructuralRevision);

            Assert.IsTrue(world.TrySetLocalClock(handle.Key, new ElementLocalClock(0.5f)));
            ulong afterClockAdded = world.StructuralRevision;
            Assert.Greater(afterClockAdded, afterLifetimeAdded);

            Assert.IsTrue(world.RemoveLocalClock(handle.Key));
            Assert.Greater(world.StructuralRevision, afterClockAdded);
        }



        [Test]
        public void DebugSnapshot_ReportsActualSparseAndPhysicsStorage()
        {
            using var world = new ElementWorld(4);
            ElementHandle query = world.Spawn(
                ElementSpawnBuilder.From(CreateArchetype(ElementCapabilities.KinematicMotion2D)))
                .Handle;
            ElementHandle physics = world.Spawn(
                ElementSpawnBuilder.From(CreateArchetype(ElementCapabilities.QueryTarget2D)))
                .Handle;

            var lifetime = new ElementLifetimeFeature(
                3f,
                ElementUpdateSchedule.UpdateSeconds());
            Assert.IsTrue(world.TrySetLifetime(query.Key, in lifetime));
            Assert.IsTrue(world.TrySetLocalClock(query.Key, new ElementLocalClock(0.75f)));

            Assert.IsTrue(world.TryGetDebugSnapshot(query.Key, out ElementDebugSnapshot queryDebug));
            Assert.IsTrue(queryDebug.HasLifetimeFeature);
            Assert.GreaterOrEqual(queryDebug.Storage.LifetimeDenseIndex, 0);
            Assert.IsTrue(queryDebug.HasLocalClock);
            Assert.GreaterOrEqual(queryDebug.Storage.LocalClockDenseIndex, 0);
            Assert.IsFalse(queryDebug.Storage.HasPhysicsBody);

            Assert.IsTrue(world.TryGetDebugSnapshot(physics.Key, out ElementDebugSnapshot physicsDebug));
            Assert.IsFalse(physicsDebug.HasLifetimeFeature);
            Assert.AreEqual(-1, physicsDebug.Storage.LifetimeDenseIndex);
            Assert.IsTrue(physicsDebug.Storage.HasPhysicsBody);
            Assert.GreaterOrEqual(physicsDebug.Storage.PhysicsBodyHandleIndex, 0);
            Assert.GreaterOrEqual(physicsDebug.Storage.PhysicsShapeHandleIndex, 0);
        }



        [Test]
        public void LaneStorageDiagnostics_ReportsDenseCountAndCapacityWithoutReflection()
        {
            using var world = new ElementWorld(4);
            world.Spawn(ElementSpawnBuilder.From(CreateArchetype(ElementCapabilities.KinematicMotion2D)));
            world.Spawn(ElementSpawnBuilder.From(CreateArchetype(ElementCapabilities.QueryTarget2D)));

            Assert.IsTrue(world.TryGetLaneStorageDiagnostics(
                ElementLane.QuerySprite2D,
                out ElementLaneStorageDiagnostics query));
            Assert.AreEqual(1, query.DenseCount);
            Assert.GreaterOrEqual(query.DenseCapacity, query.DenseCount);
            Assert.IsFalse(query.UsesPhysicsCoreBodies);

            Assert.IsTrue(world.TryGetLaneStorageDiagnostics(
                ElementLane.DynamicBodySprite2D,
                out ElementLaneStorageDiagnostics physics));
            Assert.AreEqual(1, physics.DenseCount);
            Assert.GreaterOrEqual(physics.DenseCapacity, physics.DenseCount);
            Assert.IsTrue(physics.UsesPhysicsCoreBodies);
        }



        [Test]
        public void WorldStorageDiagnostics_ReportsRegistryPoseAndSparseFeatureUsage()
        {
            using var world = new ElementWorld(4);
            ElementHandle withFeatures = world.Spawn(
                ElementSpawnBuilder.From(CreateArchetype(ElementCapabilities.KinematicMotion2D)))
                .Handle;
            ElementHandle withoutFeatures = world.Spawn(
                ElementSpawnBuilder.From(CreateArchetype(ElementCapabilities.None)))
                .Handle;
            var lifetime = new ElementLifetimeFeature(
                3f,
                ElementUpdateSchedule.UpdateSeconds());
            Assert.IsTrue(world.TrySetLifetime(withFeatures.Key, in lifetime));
            Assert.IsTrue(world.TrySetLocalClock(withFeatures.Key, new ElementLocalClock(0.5f)));

            Assert.IsTrue(world.TryGetStorageDiagnostics(out ElementWorldStorageDiagnostics diagnostics));
            Assert.AreEqual(2, diagnostics.RegistrySlotCount);
            Assert.GreaterOrEqual(diagnostics.RegistrySlotCapacity, diagnostics.RegistrySlotCount);
            Assert.AreEqual(2, diagnostics.PoseCount);
            Assert.GreaterOrEqual(diagnostics.PoseCapacity, diagnostics.PoseCount);
            Assert.AreEqual(1, diagnostics.LifetimeCount);
            Assert.GreaterOrEqual(diagnostics.LifetimeCapacity, diagnostics.LifetimeCount);
            Assert.AreEqual(1, diagnostics.LocalClockCount);
            Assert.GreaterOrEqual(diagnostics.LocalClockCapacity, diagnostics.LocalClockCount);

            var despawn = new DespawnElement();
            Assert.IsTrue(withoutFeatures.TrySubmit(in despawn));
            world.Tick(0f);
            Assert.IsTrue(world.TryGetStorageDiagnostics(out diagnostics));
            Assert.AreEqual(2, diagnostics.RegistrySlotCount);
            Assert.AreEqual(1, diagnostics.PoseCount);
            Assert.AreEqual(1, diagnostics.LifetimeCount);
            Assert.AreEqual(1, diagnostics.LocalClockCount);
        }



        [Test]
        public void DebugCaptureLease_IsReferenceCountedAndRestoresDisabledState()
        {
            using var world = new ElementWorld(1);
            Assert.IsFalse(world.EnableDetailedDiagnostics);
            Assert.IsFalse(world.EnableDebugCapture);

            ElementWorldDebugCaptureLease first = ElementWorldDebugRegistry.AcquireCapture(world);
            ElementWorldDebugCaptureLease second = ElementWorldDebugRegistry.AcquireCapture(world);
            Assert.IsTrue(world.EnableDetailedDiagnostics);
            Assert.IsTrue(world.EnableDebugCapture);

            first.Dispose();
            Assert.IsTrue(world.EnableDetailedDiagnostics);
            Assert.IsTrue(world.EnableDebugCapture);

            second.Dispose();
            Assert.IsFalse(world.EnableDetailedDiagnostics);
            Assert.IsFalse(world.EnableDebugCapture);
        }



        [Test]
        public void DebugFactCapture_UsesBoundedOldestFirstRingBuffer()
        {
            using var world = new ElementWorld(132);
            using ElementWorldDebugCaptureLease lease = ElementWorldDebugRegistry.AcquireCapture(
                world,
                detailedDiagnostics: false,
                factCapture: true);
            ElementCompiledArchetype archetype = CreateArchetype(ElementCapabilities.None);
            for (int i = 0; i < 130; i++)
            {
                world.Spawn(ElementSpawnBuilder.From(archetype));
            }

            var facts = new List<ElementFact>(128);
            world.CopyRecentFacts(facts);

            Assert.AreEqual(128, facts.Count);
            Assert.AreEqual(2, facts[0].Element.Slot);
            Assert.AreEqual(129, facts[^1].Element.Slot);
            Assert.AreEqual(ElementFactType.Spawned, facts[0].Type);
            Assert.AreEqual(ElementFactType.Spawned, facts[^1].Type);
        }



        [Test]
        public void TeleportCommand_ResetsPreviousPoseAndDoesNotCreateSweep()
        {
            using var world = new ElementWorld(1);
            ElementHandle handle = world.Spawn(
                ElementSpawnBuilder.From(CreateArchetype(ElementCapabilities.KinematicMotion2D)))
                .Handle;

            Assert.IsTrue(handle.Teleport(new float2(8f, -3f)));
            world.Tick(0f);

            Assert.IsTrue(handle.TryGetSnapshot(out ElementSnapshot snapshot));
            Assert.AreEqual(new float2(8f, -3f), snapshot.PreviousPosition);
            Assert.AreEqual(new float2(8f, -3f), snapshot.Position);
            Assert.IsTrue(snapshot.TeleportedThisStep);
        }



        [Test]
        public void InspectorEditableCommands_ApplyAllSupportedValuesAtNextWorldBoundary()
        {
            using var world = new ElementWorld(1);
            ElementHandle handle = world.Spawn(
                ElementSpawnBuilder.From(CreateArchetype(ElementCapabilities.KinematicMotion2D)))
                .Handle;
            var lifetime = new ElementLifetimeFeature(
                9f,
                ElementUpdateSchedule.UpdateSeconds());
            Assert.IsTrue(world.TrySetLifetime(handle.Key, in lifetime));

            var position = new float2(7f, -2f);
            var velocity = new float2(3f, 4f);
            var acceleration = new float2(-1f, 0.5f);
            var scale = new float2(1.5f, 0.75f);
            var color = new Color32(12, 34, 56, 78);
            Assert.IsTrue(handle.TrySubmit(new TeleportElement2D(position)));
            Assert.IsTrue(handle.TrySubmit(new SetElementVelocity2D(velocity)));
            Assert.IsTrue(handle.TrySubmit(new SetElementAcceleration2D(acceleration)));
            Assert.IsTrue(handle.TrySubmit(new SetElementScale2D(scale)));
            Assert.IsTrue(handle.TrySubmit(new SetElementColor(color)));
            Assert.IsTrue(handle.TrySubmit(new SetElementRemainingLifetime(4.5f)));

            world.Tick(0f);

            Assert.IsTrue(handle.TryGetSnapshot(out ElementSnapshot snapshot));
            Assert.AreEqual(position, snapshot.Position);
            Assert.AreEqual(position, snapshot.PreviousPosition);
            Assert.AreEqual(velocity, snapshot.Velocity);
            Assert.AreEqual(acceleration, snapshot.Acceleration);
            Assert.AreEqual(scale, snapshot.Scale);
            Assert.AreEqual(color, snapshot.Color);
            Assert.AreEqual(4.5f, snapshot.RemainingLifetime, 0.0001f);
            Assert.IsTrue(world.TryGetLifetime(handle.Key, out ElementLifetimeFeature editedLifetime));
            Assert.AreEqual(4.5f, editedLifetime.RemainingUnits, 0.0001f);
        }



        [Test]
        public void TenThousandElements_CopySnapshotsKeepsExactActiveCount()
        {
            const int count = 10_000;
            using var world = new ElementWorld(count);
            ElementCompiledArchetype archetype = CreateArchetype(ElementCapabilities.None);
            for (int i = 0; i < count; i++)
            {
                world.Spawn(ElementSpawnBuilder.From(archetype));
            }

            ulong structuralRevision = world.StructuralRevision;
            var snapshots = new List<ElementSnapshot>(count);
            world.CopySnapshots(snapshots);

            Assert.AreEqual(count, world.AliveCount);
            Assert.AreEqual(count, snapshots.Count);
            Assert.AreEqual(structuralRevision, world.StructuralRevision);
        }



        private static ElementCompiledArchetype CreateArchetype(ElementCapabilities capabilities)
        {
            Assert.IsTrue(ElementCompiledArchetype.TryCreate(
                capabilities,
                0.5f,
                5f,
                0,
                0f,
                false,
                PhysicsCoreBodyMode.Kinematic,
                PhysicsCoreShape2D.Circle,
                out ElementCompiledArchetype archetype,
                out ElementSpawnStatus failure), failure.ToString());
            return archetype;
        }
    }
}
