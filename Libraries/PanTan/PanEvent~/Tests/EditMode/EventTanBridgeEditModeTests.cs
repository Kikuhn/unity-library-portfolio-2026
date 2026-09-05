using System;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Pan.Event;
using Pan.HighDensityElement.Editor;
using Pan.HighDensityElement;
using Pan.Tan.Editor;
using Pan.Tan.Element;
using Pan.Tan.PanEvent.Editor;
using UnityEngine;
using UnityEngine.TestTools;



namespace Pan.Tan.PanEvent.Tests
{
    public sealed class EventTanBridgeEditModeTests
    {
        [SetUp]
        public void SetUp()
        {
            PanEventGeneralManager.Initialize();
            EventTanDebuggerComponentProvider.Instance.ReleaseSession();
        }



        [TearDown]
        public void TearDown()
        {
            EventTanDebuggerComponentProvider.Instance.ReleaseSession();
            ElementDebuggerSelection.Clear();
        }



        private sealed class FakeRuntime : ITanRuntimeBackend
        {
            private readonly TanKey key = new TanKey(41, 0, 1);
            private bool alive = true;
            private bool disposed;



            public int ContextId => 41;
            public TanBackendKind BackendKind => TanBackendKind.Normal;
            public int ActiveCount => alive ? 1 : 0;
            public bool IsAvailable => !disposed;
            public event TanFactHandler FactRaised;



            public TanHandle CreateHandle() => new TanHandle(this, in key);
            public bool IsAlive(in TanKey candidate) => !disposed && alive && candidate == key;
            public bool TrySpawn(in TanSpawnRequest request, out TanHandle handle)
            {
                handle = default;
                return false;
            }

            public bool TryGetSnapshot(in TanKey candidate, out TanSnapshot snapshot)
            {
                snapshot = default;
                return IsAlive(in candidate);
            }

            public bool TrySubmit(in TanKey candidate, in TanCommand command) => IsAlive(in candidate);

            public void RaiseDespawned()
            {
                alive = false;
                var fact = new TanFact(TanFactType.Despawned, in key, default, Vector2.zero);
                FactRaised?.Invoke(in fact);
            }

            public void RaiseFact(TanFactType type)
            {
                var targetKey = new TanKey(72, 3, 1);
                var target = new TanTargetHandle(TanTargetKind.External, in targetKey);
                var fact = new TanFact(type, in key, in target, Vector2.one);
                FactRaised?.Invoke(in fact);
            }

            public void Dispose()
            {
                alive = false;
                disposed = true;
            }
        }



        [Test]
        public void Wrap_DoesNotAllocateHostUntilEventAbleIsRequested()
        {
            using var runtime = new FakeRuntime();
            using var registry = new EventTanHostRegistry(runtime);

            EventTanHandle handle = registry.Wrap(runtime.CreateHandle());

            Assert.AreEqual(0, registry.ActiveHostCount);
            Assert.IsFalse(handle.TryGetAllocatedEventAble(out EventAble missing));
            Assert.IsNull(missing);
            Assert.IsNotNull(handle.EventAble);
            Assert.AreEqual(1, registry.ActiveHostCount);
            Assert.IsTrue(handle.TryGetAllocatedEventAble(out EventAble allocated));
            Assert.AreSame(handle.EventAble, allocated);
        }



        [Test]
        public void ApplyLoadout_SuccessPublishesOneCompletedHost()
        {
            using var runtime = new FakeRuntime();
            using var registry = new EventTanHostRegistry(runtime);
            EventTanHandle handle = registry.Wrap(runtime.CreateHandle());

            Assert.IsTrue(handle.TryApplyLoadout(Install, out string failureReason), failureReason);
            Assert.AreEqual(string.Empty, failureReason);
            Assert.AreEqual(1, registry.ActiveHostCount);
            Assert.AreEqual(0, registry.PooledHostCount);
            Assert.AreEqual(0, registry.QuarantinedHostCount);
            Assert.IsTrue(handle.TryGetAllocatedEventAble(out EventAble eventAble));
            Assert.IsNotNull(eventAble.Peek<TanFactProbeValue>());

            static bool Install(in EventTanHandle tan, out string reason)
            {
                TanFactProbeValue.Require(tan);
                reason = string.Empty;
                return true;
            }
        }



        [Test]
        public void ApplyLoadout_RejectedRollsBackEveryAttachedValueAndPoolsCleanHost()
        {
            using var runtime = new FakeRuntime();
            using var registry = new EventTanHostRegistry(runtime);
            EventTanHandle handle = registry.Wrap(runtime.CreateHandle());
            TanFactProbeValue attached = null;

            Assert.IsFalse(handle.TryApplyLoadout(Install, out string failureReason));
            Assert.AreEqual("PresetRejected", failureReason);
            Assert.AreEqual(1, attached.DisableCount);
            Assert.AreEqual(0, registry.ActiveHostCount);
            Assert.AreEqual(1, registry.PooledHostCount);
            Assert.AreEqual(0, registry.QuarantinedHostCount);
            Assert.IsFalse(handle.TryGetAllocatedEventAble(out _));

            bool Install(in EventTanHandle tan, out string reason)
            {
                attached = TanFactProbeValue.Require(tan);
                reason = "PresetRejected";
                return false;
            }
        }



        [Test]
        public void ApplyLoadout_InstallerExceptionRollsBackAndReportsOriginalFailure()
        {
            using var runtime = new FakeRuntime();
            using var registry = new EventTanHostRegistry(runtime);
            EventTanHandle handle = registry.Wrap(runtime.CreateHandle());
            LogAssert.Expect(LogType.Exception, new Regex("loadout install failed", RegexOptions.IgnoreCase));

            Assert.IsFalse(handle.TryApplyLoadout(Install, out string failureReason));
            StringAssert.Contains("EventTanLoadoutException:InvalidOperationException", failureReason);
            Assert.AreEqual(0, registry.ActiveHostCount);
            Assert.AreEqual(1, registry.PooledHostCount);
            Assert.AreEqual(0, registry.QuarantinedHostCount);

            static bool Install(in EventTanHandle tan, out string reason)
            {
                TanFactProbeValue.Require(tan);
                reason = string.Empty;
                throw new InvalidOperationException("loadout install failed");
            }
        }



        [Test]
        public void ApplyLoadout_RollbackExceptionQuarantinesDirtyHost()
        {
            using var runtime = new FakeRuntime();
            using var registry = new EventTanHostRegistry(runtime);
            EventTanHandle handle = registry.Wrap(runtime.CreateHandle());
            LogAssert.Expect(LogType.Exception, new Regex("throwing Tan disable", RegexOptions.IgnoreCase));

            Assert.IsFalse(handle.TryApplyLoadout(Install, out string failureReason));
            StringAssert.Contains("EventTanLoadoutRollbackException:InvalidOperationException", failureReason);
            Assert.AreEqual(0, registry.ActiveHostCount);
            Assert.AreEqual(0, registry.PooledHostCount);
            Assert.AreEqual(1, registry.QuarantinedHostCount);
            Assert.IsFalse(handle.TryGetAllocatedEventAble(out _));

            static bool Install(in EventTanHandle tan, out string reason)
            {
                ThrowingTanDisableValue.Require(tan);
                reason = "PresetRejected";
                return false;
            }
        }



        [Test]
        public void ApplyLoadout_ExistingHostIsRejectedWithoutMutatingItsValues()
        {
            using var runtime = new FakeRuntime();
            using var registry = new EventTanHostRegistry(runtime);
            EventTanHandle handle = registry.Wrap(runtime.CreateHandle());
            TanFactProbeValue existing = TanFactProbeValue.Require(handle);
            bool installerCalled = false;

            Assert.IsFalse(handle.TryApplyLoadout(Install, out string failureReason));
            Assert.AreEqual("EventTanHostAlreadyAllocated", failureReason);
            Assert.IsFalse(installerCalled);
            Assert.AreEqual(1, registry.ActiveHostCount);
            Assert.AreSame(existing, handle.EventAble.Peek<TanFactProbeValue>());

            bool Install(in EventTanHandle tan, out string reason)
            {
                installerCalled = true;
                reason = string.Empty;
                return true;
            }
        }



        [Test]
        public void AllocatedEventAble_ReceivesSpawnContactAndLifetimeFacts()
        {
            using var runtime = new FakeRuntime();
            using var registry = new EventTanHostRegistry(runtime);
            EventTanHandle handle = registry.Wrap(runtime.CreateHandle());
            TanFactProbeValue probe = TanFactProbeValue.Require(handle);

            runtime.RaiseFact(TanFactType.Spawned);
            runtime.RaiseFact(TanFactType.Contact);
            runtime.RaiseFact(TanFactType.LifetimeExpired);

            Assert.AreEqual(3, probe.SignalCount);
            Assert.AreEqual(TanFactType.LifetimeExpired, probe.LastFactType);
            Assert.IsTrue(probe.WasAliveDuringFinalSignal);
        }



        [Test]
        public void DebuggerSelection_WithoutEventAbleHasNoSectionAndDoesNotAllocate()
        {
            using var runtime = new ElementTanRuntime(ElementTanBackendOptions.Query, 4);
            Assert.IsTrue(runtime.TrySpawn(CreateElementRequest(), out TanHandle tan));
            using var registry = new EventTanHostRegistry(runtime);
            ElementDebuggerInspectorContext context = CreateDebuggerContext(runtime, tan);

            Assert.IsFalse(EventTanDebuggerComponentProvider.Instance.TryGetDescriptor(
                in context,
                out ElementDebuggerComponentDescriptor descriptor));
            Assert.AreEqual(default(ElementDebuggerComponentDescriptor).Id, descriptor.Id);
            Assert.AreEqual(0, registry.ActiveHostCount);
            Assert.IsFalse(EventTanDebuggerComponentProvider.Instance.HasSession);
        }



        [Test]
        public void TenThousandLoadoutlessElementTans_DoNotAllocateEventHosts()
        {
            const int count = 10000;
            using var runtime = new ElementTanRuntime(ElementTanBackendOptions.Query, count);
            using var registry = new EventTanHostRegistry(runtime);
            TanSpawnRequest request = CreateElementRequest();

            for (int i = 0; i < count; i++)
            {
                Assert.IsTrue(runtime.TrySpawn(in request, out TanHandle tan), $"spawn {i}");
                Assert.IsTrue(registry.TryWrap(tan, out _), $"wrap {i}");
            }

            Assert.AreEqual(count, runtime.ActiveCount);
            Assert.AreEqual(0, registry.ActiveHostCount);
        }



        [Test]
        public void DebuggerAllocatedSession_IsReleasedBeforeDespawnInvalidatesHost()
        {
            using var runtime = new ElementTanRuntime(ElementTanBackendOptions.Query, 4);
            Assert.IsTrue(runtime.TrySpawn(CreateElementRequest(), out TanHandle tan));
            using var registry = new EventTanHostRegistry(runtime);
            EventTanHandle eventTan = registry.Wrap(tan);
            _ = eventTan.EventAble;
            ElementDebuggerInspectorContext context = CreateDebuggerContext(runtime, tan);

            Assert.IsTrue(EventTanDebuggerComponentProvider.Instance.TryGetDescriptor(
                in context,
                out ElementDebuggerComponentDescriptor descriptor));
            Assert.AreEqual(ElementDebuggerComponentStatus.Lazy, descriptor.Status);
            Assert.IsTrue(EventTanDebuggerComponentProvider.Instance.TryBindAllocated(in context));
            Assert.IsTrue(EventTanDebuggerComponentProvider.Instance.HasSession);

            Assert.IsTrue(tan.TrySubmit(new DespawnTan()));
            runtime.ElementWorld.FlushCommands();

            Assert.IsFalse(EventTanDebuggerComponentProvider.Instance.HasSession);
            Assert.AreEqual(0, registry.ActiveHostCount);
        }



        [Test]
        public void DebuggerDescriptor_UsesStableIdForGenericProviderConflictDetection()
        {
            Assert.AreEqual("panevent.eventable", EventTanDebuggerComponentProvider.ComponentId);
        }



        [Test]
        public void DebuggerDescriptor_WithAttachedEventValueIsAllocated()
        {
            using var runtime = new ElementTanRuntime(ElementTanBackendOptions.Query, 4);
            Assert.IsTrue(runtime.TrySpawn(CreateElementRequest(), out TanHandle tan));
            using var registry = new EventTanHostRegistry(runtime);
            EventTanHandle eventTan = registry.Wrap(tan);
            TanLifetimeEventValue.Require(eventTan);
            ElementDebuggerInspectorContext context = CreateDebuggerContext(runtime, tan);

            Assert.IsTrue(EventTanDebuggerComponentProvider.Instance.TryGetDescriptor(
                in context,
                out ElementDebuggerComponentDescriptor descriptor));
            Assert.AreEqual(ElementDebuggerComponentStatus.Allocated, descriptor.Status);
        }



        [Test]
        public void DebuggerLookup_UsesRegistryThatOwnsSelectedTanHost()
        {
            using var runtime = new ElementTanRuntime(ElementTanBackendOptions.Query, 4);
            Assert.IsTrue(runtime.TrySpawn(CreateElementRequest(), out TanHandle tan));
            using var owningRegistry = new EventTanHostRegistry(runtime);
            EventTanHandle eventTan = owningRegistry.Wrap(tan);
            _ = eventTan.EventAble;
            using var laterEmptyRegistry = new EventTanHostRegistry(runtime);
            ElementDebuggerInspectorContext context = CreateDebuggerContext(runtime, tan);

            Assert.IsTrue(EventTanDebuggerComponentProvider.Instance.TryGetDescriptor(
                in context,
                out ElementDebuggerComponentDescriptor descriptor));
            Assert.AreEqual("panevent.eventable", descriptor.Id);
            Assert.AreEqual(1, owningRegistry.ActiveHostCount);
            Assert.AreEqual(0, laterEmptyRegistry.ActiveHostCount);
        }



        [Test]
        public void Despawn_ReleasesHostAndMakesHandleStale()
        {
            using var runtime = new FakeRuntime();
            using var registry = new EventTanHostRegistry(runtime);
            EventTanHandle handle = registry.Wrap(runtime.CreateHandle());
            _ = handle.EventAble;

            runtime.RaiseDespawned();

            Assert.IsFalse(handle.IsAlive);
            Assert.AreEqual(0, registry.ActiveHostCount);
            Assert.AreEqual(1, registry.PooledHostCount);
            Assert.Throws<InvalidOperationException>(() => _ = handle.EventAble);
        }



        [Test]
        public void Despawn_FinalFactAndSiblingResetUseRetiringHostThenInvalidate()
        {
            using var runtime = new FakeRuntime();
            using var registry = new EventTanHostRegistry(runtime);
            EventTanHandle handle = registry.Wrap(runtime.CreateHandle());
            TanSiblingRemovingValue remover = TanSiblingRemovingValue.Require(handle);
            TanFactProbeValue sibling = TanFactProbeValue.Require(handle);

            runtime.RaiseDespawned();

            Assert.AreEqual(1, sibling.SignalCount);
            Assert.AreEqual(TanFactType.Despawned, sibling.LastFactType);
            Assert.IsTrue(sibling.WasAliveDuringFinalSignal);
            Assert.AreEqual(1, remover.DisableCount);
            Assert.AreEqual(1, sibling.DisableCount);
            Assert.IsFalse(handle.IsAlive);
            Assert.AreEqual(0, registry.ActiveHostCount);
            Assert.AreEqual(1, registry.PooledHostCount);
        }



        [Test]
        public void ThrowingDisable_QuarantinesHostAndStillInvalidatesTan()
        {
            using var runtime = new FakeRuntime();
            using var registry = new EventTanHostRegistry(runtime);
            EventTanHandle handle = registry.Wrap(runtime.CreateHandle());
            ThrowingTanDisableValue.Require(handle);
            LogAssert.Expect(LogType.Exception, new Regex("throwing Tan disable", RegexOptions.IgnoreCase));

            runtime.RaiseDespawned();

            Assert.IsFalse(handle.IsAlive);
            Assert.AreEqual(0, registry.ActiveHostCount);
            Assert.AreEqual(0, registry.PooledHostCount);
            Assert.AreEqual(1, registry.QuarantinedHostCount);
        }



        [Test]
        public void RuntimeDispose_MakesEveryHandleAccessFalse()
        {
            var runtime = new FakeRuntime();
            using var registry = new EventTanHostRegistry(runtime);
            EventTanHandle handle = registry.Wrap(runtime.CreateHandle());

            runtime.Dispose();

            Assert.IsFalse(handle.IsAlive);
            Assert.IsFalse(handle.TryGetSnapshot(out _));
            Assert.IsFalse(handle.TrySubmit(new DespawnTan()));
        }



        [Test]
        public void LifetimeEventValue_UsesOnlySparseBackendStateAndRemoveClearsIt()
        {
            using var runtime = new ElementTanRuntime(ElementTanBackendOptions.Query, 4);
            Assert.IsTrue(runtime.TrySpawn(CreateElementRequest(), out TanHandle tan));
            using var registry = new EventTanHostRegistry(runtime);
            EventTanHandle handle = registry.Wrap(tan);
            TanLifetimeEventValue value = TanLifetimeEventValue.Require(handle);
            ElementKey key = new ElementKey(tan.Key.ContextId, tan.Key.Slot, tan.Key.Generation);
            var lifetime = new TanLifetimeDefinition(
                5f,
                ElementUpdateSchedule.UpdateFrames(2, ElementTimeDomain.Unscaled),
                useLocalClock: true);

            Assert.IsFalse(runtime.ElementWorld.TryGetLifetime(key, out _));
            Assert.IsTrue(value.Configure(in lifetime));
            Assert.IsTrue(runtime.ElementWorld.TryGetLifetime(key, out ElementLifetimeFeature native));
            Assert.AreEqual(5f, native.RemainingUnits);
            Assert.IsTrue(value.TryGetRemainingUnits(out float remaining));
            Assert.AreEqual(5f, remaining);
            Assert.IsTrue(value.RemoveLifetime());
            Assert.IsFalse(runtime.ElementWorld.TryGetLifetime(key, out _));

            FieldInfo[] fields = typeof(TanLifetimeEventValue).GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            Assert.IsFalse(Array.Exists(fields, field => field.FieldType == typeof(float)));
        }



        [Test]
        public void LifetimeEventValue_DisablePolicyRemovesSparseFeature()
        {
            using var runtime = new ElementTanRuntime(ElementTanBackendOptions.Query, 4);
            Assert.IsTrue(runtime.TrySpawn(CreateElementRequest(), out TanHandle tan));
            using var registry = new EventTanHostRegistry(runtime);
            EventTanHandle handle = registry.Wrap(tan);
            TanLifetimeEventValue value = TanLifetimeEventValue.Require(handle);
            ElementKey key = new ElementKey(tan.Key.ContextId, tan.Key.Slot, tan.Key.Generation);
            var lifetime = new TanLifetimeDefinition(2f, ElementUpdateSchedule.UpdateSeconds());
            Assert.IsTrue(value.Configure(in lifetime, removeOnDisable: true));
            Assert.IsTrue(runtime.ElementWorld.TryGetLifetime(key, out _));

            Assert.IsTrue(handle.EventAble.RemoveValue<TanLifetimeEventValue>());

            Assert.IsFalse(runtime.ElementWorld.TryGetLifetime(key, out _));
        }



        private static TanSpawnRequest CreateElementRequest()
        {
            var collision = new TanCollisionDefinition(0, ~0);
            var definition = new TanDefinition(0.1f, in collision);
            return new TanSpawnRequest(in definition, Vector2.zero, Vector2.right);
        }

        private static ElementDebuggerInspectorContext CreateDebuggerContext(
            ElementTanRuntime runtime,
            TanHandle tan)
        {
            ElementKey key = new ElementKey(tan.Key.ContextId, tan.Key.Slot, tan.Key.Generation);
            Assert.IsTrue(runtime.ElementWorld.TryGetHandle(key, out ElementHandle elementHandle));
            Assert.IsTrue(runtime.ElementWorld.TryGetSnapshot(key, out ElementSnapshot snapshot));
            return new ElementDebuggerInspectorContext(
                runtime.ElementWorld,
                elementHandle,
                snapshot,
                Array.Empty<ElementFact>());
        }
    }



    [PanEventUsageProfile(PanEventUsageProfile.ValidationOnly)]
    public sealed class TanFactProbeValue :
        PanBaseEventValue.EventAbles<TanFactProbeValue>,
        IEventAbleSignalReceiver<TanFactSignal>
    {
        public int SignalCount { get; private set; }
        public int DisableCount { get; private set; }
        public TanFactType LastFactType { get; private set; }
        public bool WasAliveDuringFinalSignal { get; private set; }



        protected override void Enable()
        {
            SignalCount = 0;
            DisableCount = 0;
            LastFactType = default;
            WasAliveDuringFinalSignal = false;
        }

        protected override void Disable() => DisableCount++;

        public void ReceiveLocalSignal(IEventAble source, in TanFactSignal signal)
        {
            SignalCount++;
            LastFactType = signal.Fact.Type;
            WasAliveDuringFinalSignal = source is EventTanHandle handle && handle.IsAlive;
        }
    }



    [PanEventUsageProfile(PanEventUsageProfile.ValidationOnly)]
    public sealed class TanSiblingRemovingValue : PanBaseEventValue.EventAbles<TanSiblingRemovingValue>
    {
        public int DisableCount { get; private set; }
        protected override void Enable() => DisableCount = 0;

        protected override void Disable()
        {
            DisableCount++;
            Current.EventAble.RemoveValue<TanFactProbeValue>();
        }
    }



    [PanEventUsageProfile(PanEventUsageProfile.ValidationOnly)]
    public sealed class ThrowingTanDisableValue : PanBaseEventValue.EventAbles<ThrowingTanDisableValue>
    {
        protected override void Disable() => throw new InvalidOperationException("throwing Tan disable");
    }
}
