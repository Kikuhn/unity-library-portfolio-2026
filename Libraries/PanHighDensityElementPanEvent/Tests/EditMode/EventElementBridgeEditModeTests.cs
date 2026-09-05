using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Pan.Event;
using Pan.HighDensityElement.Editor;
using Pan.HighDensityElement.PanEvent.Editor;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;



namespace Pan.HighDensityElement.PanEvent.Tests
{
    public sealed class EventElementBridgeEditModeTests
    {
        private sealed class TestOwner : IEventAble
        {
            public TestOwner()
            {
                EventAble = new EventAble(this, 2, EventAbleTableAllocationMode.Lazy);
            }



            public EventAble EventAble { get; }
        }



        [SetUp]
        public void SetUp()
        {
            PanEventGeneralManager.Initialize();
            EventElementDebuggerInspectorExtension.Instance.ReleaseSession();
            ElementDebuggerSelection.Clear();
        }



        [TearDown]
        public void TearDown()
        {
            ElementDebuggerSelection.Clear();
            EventElementDebuggerInspectorExtension.Instance.ReleaseSession();
        }



        [Test]
        public void EventHost_StaysLazyUntilFirstEventValueAccess()
        {
            using var world = new ElementWorld(4);
            using var hosts = new EventElementHostRegistry(world);
            EventElementHandle handle = hosts.Wrap(SpawnElement(world));

            Assert.AreEqual(0, hosts.ActiveHostCount);

            EventAble eventAble = handle.EventAble;

            Assert.AreEqual(1, hosts.ActiveHostCount);
            Assert.IsFalse(eventAble.IsEventValueTableAllocated);

            ElementFactProbeValue value = ElementFactProbeValue.Require(handle);

            Assert.IsNotNull(value);
            Assert.IsTrue(eventAble.IsEventValueTableAllocated);
        }



        [Test]
        public void DebuggerProvider_ClaimsOnlyExplicitlyWrappedKeyWithoutAllocatingOnSelection()
        {
            using var world = new ElementWorld(4);
            using var hosts = new EventElementHostRegistry(world);
            ElementHandle element = SpawnElement(world);
            Assert.IsTrue(element.TryGetSnapshot(out ElementSnapshot snapshot));
            var context = new ElementDebuggerInspectorContext(
                world,
                element,
                snapshot,
                Array.Empty<ElementFact>());
            EventElementDebuggerInspectorExtension extension = EventElementDebuggerInspectorExtension.Instance;
            var providers = new List<IElementDebuggerComponentProvider>();

            Assert.IsTrue(ElementDebuggerSelection.TrySet(world, element.Key));
            ElementDebuggerComponentProviderRegistry.CopyProviders(providers);
            CollectionAssert.Contains(providers, extension);
            Assert.IsFalse(extension.TryGetDescriptor(in context, out _));
            Assert.AreEqual(0, hosts.ActiveHostCount);
            Assert.IsFalse(extension.HasSession);

            EventElementHandle wrapped = hosts.Wrap(element);

            Assert.IsFalse(extension.TryGetDescriptor(in context, out _));
            Assert.AreEqual(0, hosts.ActiveHostCount);
            Assert.IsFalse(extension.HasSession);

            EventAble eventAble = wrapped.EventAble;

            Assert.IsTrue(extension.TryGetDescriptor(in context, out ElementDebuggerComponentDescriptor descriptor));
            Assert.AreEqual(EventElementDebuggerInspectorExtension.ComponentId, descriptor.Id);
            Assert.AreEqual(ElementDebuggerComponentStatus.Lazy, descriptor.Status);
            Assert.AreEqual(1, hosts.ActiveHostCount);
            Assert.IsFalse(eventAble.IsEventValueTableAllocated);
            Assert.IsFalse(extension.TryBindAllocatedSession(in context));
            Assert.IsFalse(extension.HasSession);
            Assert.IsTrue(wrapped.IsAlive);
        }



        [Test]
        public void DebuggerSession_ClosesBeforeDespawnAndDoesNotFollowRecycledSlot()
        {
            using var world = new ElementWorld(4);
            using var hosts = new EventElementHostRegistry(world);
            ElementHandle first = SpawnElement(world);
            Assert.IsTrue(first.TryGetSnapshot(out ElementSnapshot firstSnapshot));
            var firstContext = new ElementDebuggerInspectorContext(
                world,
                first,
                firstSnapshot,
                Array.Empty<ElementFact>());
            EventElementDebuggerInspectorExtension extension = EventElementDebuggerInspectorExtension.Instance;
            EventElementHandle firstWrapped = hosts.Wrap(first);
            ElementFactProbeValue.Require(firstWrapped);

            Assert.IsTrue(extension.TryGetDescriptor(
                in firstContext,
                out ElementDebuggerComponentDescriptor firstDescriptor));
            Assert.AreEqual(ElementDebuggerComponentStatus.Allocated, firstDescriptor.Status);
            Assert.IsTrue(extension.TryBindAllocatedSession(in firstContext));
            Assert.IsTrue(hosts.EditorTryGetAllocatedEventAble(first.Key, out EventAble firstEventAble));
            Assert.IsTrue(extension.HasSession);

            Assert.IsTrue(world.TryDespawnImmediately(first.Key));

            Assert.IsFalse(extension.HasSession);
            Assert.AreEqual(0, hosts.ActiveHostCount);
            Assert.AreEqual(1, hosts.PooledHostCount);
            Assert.IsFalse(hosts.EditorTryGetAllocatedEventAble(first.Key, out _));

            ElementHandle recycled = SpawnElement(world);
            Assert.AreEqual(first.Key.Slot, recycled.Key.Slot);
            Assert.AreNotEqual(first.Key.Generation, recycled.Key.Generation);
            Assert.IsTrue(recycled.TryGetSnapshot(out ElementSnapshot recycledSnapshot));
            var recycledContext = new ElementDebuggerInspectorContext(
                world,
                recycled,
                recycledSnapshot,
                Array.Empty<ElementFact>());

            Assert.IsFalse(extension.TryGetDescriptor(in recycledContext, out _));
            Assert.AreEqual(0, hosts.ActiveHostCount);
            EventElementHandle recycledWrapped = hosts.Wrap(recycled);
            Assert.IsFalse(extension.TryGetDescriptor(in recycledContext, out _));
            _ = recycledWrapped.EventAble;
            Assert.IsTrue(extension.TryGetDescriptor(in recycledContext, out ElementDebuggerComponentDescriptor lazyDescriptor));
            Assert.AreEqual(ElementDebuggerComponentStatus.Lazy, lazyDescriptor.Status);
            Assert.IsFalse(extension.TryBindAllocatedSession(in recycledContext));
            ElementFactProbeValue.Require(recycledWrapped);
            Assert.IsTrue(extension.TryGetDescriptor(
                in recycledContext,
                out ElementDebuggerComponentDescriptor allocatedDescriptor));
            Assert.AreEqual(ElementDebuggerComponentStatus.Allocated, allocatedDescriptor.Status);
            Assert.IsTrue(extension.TryBindAllocatedSession(in recycledContext));
            Assert.IsTrue(hosts.EditorTryGetAllocatedEventAble(recycled.Key, out EventAble recycledEventAble));
            Assert.AreNotSame(firstEventAble, recycledEventAble);
            Assert.AreEqual(recycled.Key, extension.BoundKey);
        }



        [Test]
        public void Despawn_DispatchesLastFactThenReleasesHostAndInvalidatesHandle()
        {
            using var world = new ElementWorld(4);
            using var hosts = new EventElementHostRegistry(world);
            EventElementHandle handle = hosts.Wrap(SpawnElement(world));
            ElementFactProbeValue value = ElementFactProbeValue.Require(handle);
            var despawn = new DespawnElement();

            Assert.IsTrue(handle.TrySubmit(in despawn));
            world.Tick(0f);

            Assert.AreEqual(1, value.SignalCount);
            Assert.AreEqual(ElementFactType.Despawned, value.LastFactType);
            Assert.AreEqual(1, value.DisableCount);
            Assert.AreEqual(0, hosts.ActiveHostCount);
            Assert.AreEqual(1, hosts.PooledHostCount);
            Assert.IsFalse(handle.IsAlive);
            Assert.IsFalse(handle.TrySubmit(in despawn));
            Assert.Throws<InvalidOperationException>(() =>
            {
                _ = handle.EventAble;
            });
        }



        [Test]
        public void NativeAndGameObjectAdapters_ApplyCommonCommandsAndLifecycle()
        {
            using var world = new ElementWorld(4);
            using var hosts = new EventElementHostRegistry(world);
            ElementCompiledArchetype archetype = CreateArchetype();
            ElementSpawnBuilder nativeBuilder = ElementSpawnBuilder.From(archetype)
                .WithVelocity(new float2(4f, -2f));
            EventElementHandle native = hosts.Wrap(world.Spawn(in nativeBuilder).Handle);
            ElementFactProbeValue nativeProbe = ElementFactProbeValue.Require(native);

            var gameObject = new GameObject("GameObjectElementAdapter_Test");
            try
            {
                var owner = new TestOwner();
                using var adapter = new GameObjectElementAdapter(
                    owner,
                    gameObject.transform,
                    ElementCapabilities.KinematicMotion2D | ElementCapabilities.Lifetime,
                    0.1f,
                    10f,
                    new float2(4f, -2f),
                    resetEventAbleOnDespawn: true);
                ElementFactProbeValue adapterProbe = ElementFactProbeValue.Require(adapter);

                world.Tick(0.5f);
                adapter.Tick(0.5f);

                Assert.IsTrue(native.TryGetSnapshot(out ElementSnapshot nativeSnapshot));
                Assert.IsTrue(adapter.TryGetSnapshot(out ElementSnapshot adapterSnapshot));
                Assert.AreEqual(nativeSnapshot.Position, adapterSnapshot.Position);
                Assert.AreEqual(nativeSnapshot.Velocity, adapterSnapshot.Velocity);
                Assert.AreEqual(nativeSnapshot.RemainingLifetime, adapterSnapshot.RemainingLifetime);

                var despawn = new DespawnElement();
                native.Submit(in despawn);
                adapter.Submit(in despawn);
                world.Tick(0f);

                Assert.AreEqual(ElementFactType.Despawned, nativeProbe.LastFactType);
                Assert.AreEqual(ElementFactType.Despawned, adapterProbe.LastFactType);
                Assert.IsFalse(native.IsAlive);
                Assert.IsFalse(adapter.IsAlive);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }



        [Test]
        public void LocalFactSignal_PreservesPhysicsCoreEnvelopeAndDoesNotBroadcast()
        {
            using var world = new ElementWorld(4);
            using var hosts = new EventElementHostRegistry(world);
            EventElementHandle recipient = hosts.Wrap(SpawnElement(world));
            EventElementHandle unrelated = hosts.Wrap(SpawnElement(world));
            ElementFactProbeValue recipientProbe = ElementFactProbeValue.Require(recipient);
            ElementFactProbeValue unrelatedProbe = ElementFactProbeValue.Require(unrelated);
            var fact = new ElementFact
            {
                Type = ElementFactType.Contact,
                Element = recipient.Key,
                TargetElement = unrelated.Key,
                TargetId = 41,
                BridgeTargetId = 73,
                Position = new float2(3f, -2f),
                Normal = new float2(0f, 1f),
                TimeOfImpact = 0.375f,
                SubstepIndex = 12
            };
            var signal = new ElementFactSignal(in fact);

            recipient.EventAble.DispatchLocal(in signal);

            Assert.AreEqual(1, recipientProbe.SignalCount);
            Assert.AreEqual(0, unrelatedProbe.SignalCount);
            Assert.AreEqual(fact.TargetElement, recipientProbe.LastFact.TargetElement);
            Assert.AreEqual(73, recipientProbe.LastFact.BridgeTargetId);
            Assert.AreEqual(12, recipientProbe.LastFact.SubstepIndex);
            Assert.AreEqual(0.375f, recipientProbe.LastFact.TimeOfImpact);
            Assert.AreEqual(new float2(3f, -2f), recipientProbe.LastFact.Position);
            Assert.AreEqual(new float2(0f, 1f), recipientProbe.LastFact.Normal);
        }



        [Test]
        public void Despawn_ResetSiblingRemoval_DoesNotCreateZombieHost()
        {
            using var world = new ElementWorld(4);
            using var hosts = new EventElementHostRegistry(world);
            EventElementHandle handle = hosts.Wrap(SpawnElement(world));
            SiblingRemovingValue remover = SiblingRemovingValue.Require(handle);
            ElementFactProbeValue sibling = ElementFactProbeValue.Require(handle);

            Assert.IsTrue(world.TryDespawnImmediately(handle.Key));

            Assert.AreEqual(1, remover.DisableCount);
            Assert.AreEqual(1, sibling.DisableCount);
            Assert.AreEqual(0, hosts.ActiveHostCount);
            Assert.AreEqual(1, hosts.PooledHostCount);
        }



        [Test]
        public void ThrowingLocalReceiver_DoesNotBlockDespawnGenerationTransition()
        {
            using var world = new ElementWorld(4);
            using var hosts = new EventElementHostRegistry(world);
            EventElementHandle handle = hosts.Wrap(SpawnElement(world));
            ThrowingSignalValue.Require(handle);
            LogAssert.Expect(LogType.Exception, new Regex("throwing local signal", RegexOptions.IgnoreCase));

            Assert.IsTrue(world.TryDespawnImmediately(handle.Key));

            Assert.IsFalse(handle.IsAlive);
            Assert.AreEqual(0, hosts.ActiveHostCount);
            Assert.AreEqual(1, hosts.PooledHostCount);
            ElementHandle recycled = SpawnElement(world);
            Assert.AreEqual(handle.Key.Slot, recycled.Key.Slot);
            Assert.AreNotEqual(handle.Key.Generation, recycled.Key.Generation);
        }



        [Test]
        public void ThrowingDisable_QuarantinesHostAndStillInvalidatesElement()
        {
            using var world = new ElementWorld(4);
            using var hosts = new EventElementHostRegistry(world);
            EventElementHandle handle = hosts.Wrap(SpawnElement(world));
            ThrowingDisableValue.Require(handle);
            LogAssert.Expect(LogType.Exception, new Regex("throwing disable", RegexOptions.IgnoreCase));

            Assert.IsTrue(world.TryDespawnImmediately(handle.Key));

            Assert.IsFalse(handle.IsAlive);
            Assert.AreEqual(0, hosts.ActiveHostCount);
            Assert.AreEqual(0, hosts.PooledHostCount);
        }



        [Test]
        public void Wrap_RejectsLiveHandleFromAnotherWorld()
        {
            using var worldA = new ElementWorld(4);
            using var worldB = new ElementWorld(4);
            using var hostsA = new EventElementHostRegistry(worldA);
            ElementHandle foreign = SpawnElement(worldB);

            Assert.Throws<ArgumentException>(() => hostsA.Wrap(foreign));
            Assert.AreEqual(0, hostsA.ActiveHostCount);
        }



        [Test]
        public void TryWrap_IsNonAllocatingAndRejectsStaleHandle()
        {
            using var world = new ElementWorld(4);
            using var hosts = new EventElementHostRegistry(world);
            ElementHandle element = SpawnElement(world);

            Assert.IsTrue(hosts.TryWrap(element, out EventElementHandle wrapped));
            Assert.AreEqual(element.Key, wrapped.Key);
            Assert.AreEqual(0, hosts.ActiveHostCount);

            var despawn = new DespawnElement();
            element.Submit(in despawn);
            world.Tick(0f);

            Assert.IsFalse(hosts.TryWrap(element, out _));
            Assert.Throws<System.InvalidOperationException>(() => _ = wrapped.EventAble);
        }



        [Test]
        public void WrappedEligibility_IsRemovedOnDespawnWithoutAllocatingHost()
        {
            using var world = new ElementWorld(4);
            using var hosts = new EventElementHostRegistry(world);
            ElementHandle element = SpawnElement(world);

            Assert.IsTrue(hosts.TryWrap(element, out _));
            Assert.IsTrue(EventElementHostRegistry.EditorTryGetEligibleRegistry(
                world,
                element.Key,
                out EventElementHostRegistry eligibleRegistry));
            Assert.AreSame(hosts, eligibleRegistry);
            Assert.AreEqual(0, hosts.ActiveHostCount);

            Assert.IsTrue(world.TryDespawnImmediately(element.Key));

            Assert.IsFalse(EventElementHostRegistry.EditorTryGetEligibleRegistry(
                world,
                element.Key,
                out _));
            Assert.AreEqual(0, hosts.ActiveHostCount);
        }



        private static ElementHandle SpawnElement(ElementWorld world)
        {
            ElementCompiledArchetype archetype = CreateArchetype();
            ElementSpawnResult result = world.Spawn(ElementSpawnBuilder.From(archetype));
            Assert.IsTrue(result.Succeeded);
            return result.Handle;
        }



        private static ElementCompiledArchetype CreateArchetype()
        {
            Assert.IsTrue(ElementCompiledArchetype.TryCreate(
                ElementCapabilities.KinematicMotion2D | ElementCapabilities.Lifetime,
                0.1f,
                10f,
                0,
                0f,
                false,
                PhysicsCoreBodyMode.Kinematic,
                PhysicsCoreShape2D.Circle,
                out ElementCompiledArchetype archetype,
                out ElementSpawnStatus failure),
                failure.ToString());
            return archetype;
        }
    }



    [PanEventUsageProfile(PanEventUsageProfile.ValidationOnly)]
    public sealed class ElementFactProbeValue :
        PanBaseEventValue.EventAbles<ElementFactProbeValue>,
        IEventAbleSignalReceiver<ElementFactSignal>
    {
        public int SignalCount { get; private set; }
        public int DisableCount { get; private set; }
        public ElementFactType LastFactType { get; private set; }
        public ElementFact LastFact { get; private set; }



        protected override void Enable()
        {
            SignalCount = 0;
            DisableCount = 0;
            LastFactType = default;
            LastFact = default;
        }



        protected override void Disable()
        {
            DisableCount++;
        }



        public void ReceiveLocalSignal(IEventAble source, in ElementFactSignal signal)
        {
            SignalCount++;
            LastFactType = signal.Fact.Type;
            LastFact = signal.Fact;
        }
    }



    [PanEventUsageProfile(PanEventUsageProfile.ValidationOnly)]
    public sealed class SiblingRemovingValue : PanBaseEventValue.EventAbles<SiblingRemovingValue>
    {
        public int DisableCount { get; private set; }



        protected override void Enable() => DisableCount = 0;



        protected override void Disable()
        {
            DisableCount++;
            Current.EventAble.RemoveValue<ElementFactProbeValue>();
        }
    }



    [PanEventUsageProfile(PanEventUsageProfile.ValidationOnly)]
    public sealed class ThrowingSignalValue :
        PanBaseEventValue.EventAbles<ThrowingSignalValue>,
        IEventAbleSignalReceiver<ElementFactSignal>
    {
        public void ReceiveLocalSignal(IEventAble source, in ElementFactSignal signal)
        {
            throw new InvalidOperationException("throwing local signal");
        }
    }



    [PanEventUsageProfile(PanEventUsageProfile.ValidationOnly)]
    public sealed class ThrowingDisableValue : PanBaseEventValue.EventAbles<ThrowingDisableValue>
    {
        protected override void Disable()
        {
            throw new InvalidOperationException("throwing disable");
        }
    }
}
