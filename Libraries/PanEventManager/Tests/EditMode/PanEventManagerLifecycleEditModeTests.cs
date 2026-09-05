using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Pan.Event;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;



namespace Pan.EventManagers.Tests
{
    public sealed partial class PanEventManagerLifecycleEditModeTests
    {
        private sealed class EventAbleInspectorHarnessWindow : EditorWindow
        {
            internal EventAbleController Controller;
            private PropertyTree propertyTree;



            private void OnGUI()
            {
                if (Controller == null) { return; }

                propertyTree ??= PropertyTree.Create(Controller);
                propertyTree.Draw(false);
            }



            private void OnDisable()
            {
                propertyTree?.Dispose();
                propertyTree = null;
            }
        }



        private PanEventInitializeSettingSbject eventSettings;
        private PanEventValueInitializeSettingSbject eventValueSettings;



        [SetUp]
        public void SetUp()
        {
            ResetGeneralManager();
            ResetTypeDiscoveryCaches();
            TestLifecycleEvent.ResetInstanceCount();
            TestThrowingEnableValue.ThrowOnEnable = true;
            TestThrowingConditionValue.ResetState();
            TestReentrantRequireValue.ResetState();
            TestReentrantConditionValue.ResetState();
            TestCustomTargetConditionValue.ResetState();
        }



        [TearDown]
        public void TearDown()
        {
            ResetGeneralManager();
            ResetTypeDiscoveryCaches();

            if (eventSettings != null)
            {
                UnityEngine.Object.DestroyImmediate(eventSettings);
                eventSettings = null;
            }

            if (eventValueSettings != null)
            {
                UnityEngine.Object.DestroyImmediate(eventValueSettings);
                eventValueSettings = null;
            }
        }






        private static void ResetGeneralManager()
        {
            SetStaticAutoProperty(nameof(PanEventGeneralManager.IsInitialize), false);
            SetStaticAutoProperty(nameof(PanEventGeneralManager.EventManager), null);
            SetStaticAutoProperty(nameof(PanEventGeneralManager.EventValueManager), null);
        }



        private static void SetStaticAutoProperty(string propertyName, object value)
        {
            FieldInfo field = typeof(PanEventGeneralManager).GetField(
                $"<{propertyName}>k__BackingField",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.IsNotNull(field, $"PanEventGeneralManager.{propertyName} backing field를 찾을 수 없습니다.");
            field.SetValue(null, value);
        }



        private static void ResetTypeDiscoveryCaches()
        {
            const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
            typeof(PanEventsInitializeSettingSbjectBase)
                .GetField("panBaseEventTypesAll", Flags)
                ?.SetValue(null, null);
            typeof(PanEventsInitializeSettingSbjectBase)
                .GetField("panBaseEventValueTypesAll", Flags)
                ?.SetValue(null, null);
        }



        private static void SetTypeDiscoveryCachesForLifecycleFixtures()
        {
            const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
            typeof(PanEventsInitializeSettingSbjectBase)
                .GetField("panBaseEventTypesAll", Flags)
                ?.SetValue(null, new[] { typeof(TestLifecycleEvent) });
            typeof(PanEventsInitializeSettingSbjectBase)
                .GetField("panBaseEventValueTypesAll", Flags)
                ?.SetValue(null, new[] { typeof(TestLifecycleValue) });
        }
    }



    public sealed class EventManagerTestOwner : IEventAble
    {
        public EventManagerTestOwner()
            : this(EventAbleTableAllocationMode.Eager)
        {
        }



        public EventManagerTestOwner(EventAbleTableAllocationMode tableAllocationMode)
        {
            EventAble = new EventAble(this, 4, tableAllocationMode);
        }



        public EventAble EventAble { get; }
    }



    public sealed class TestEventAbleControllerOwner : MonoBehaviour, IEventAble
    {
        public EventAble EventAble => Controller?.EventAble;



        public EventAbleController Controller { get; private set; }



        public EventAbleController InitializeController(int initialEventTableCapacity)
        {
            Controller = EventAbleController.Initialize(this, initialEventTableCapacity);
            return Controller;
        }
    }



    [PanEventUsageProfileAttribute(PanEventUsageProfile.ValidationOnly)]
    public sealed class TestLifecycleEvent : PanBaseEvent<TestLifecycleEvent>
    {
        public TestLifecycleEvent()
        {
            InstanceCount++;
        }



        public static int InstanceCount { get; private set; }



        public static void ResetInstanceCount()
        {
            InstanceCount = 0;
        }
    }



    [PanEventUsageProfileAttribute(PanEventUsageProfile.ValidationOnly)]
    public sealed class TestLifecycleValue :
        PanBaseEventValue.EventAbles<TestLifecycleValue>,
        IEventAbleSignalReceiver<int>
    {
        public int EnableCount { get; private set; }
        public int DisableCount { get; private set; }
        public int SignalCount { get; private set; }
        public bool RemoveOnSignal { get; set; }

        [ShowInInspector]
        public int EditorMutableValue;

        [ShowInInspector]
        private int Editor_EnableCount => EnableCount;



        protected override void Enable()
        {
            EnableCount++;
            SignalCount = 0;
            RemoveOnSignal = false;
        }



        protected override void Disable()
        {
            DisableCount++;
        }



        public void ReceiveLocalSignal(IEventAble source, in int signal)
        {
            SignalCount++;
            if (RemoveOnSignal)
            {
                source.EventAble.RemoveValue(this);
            }
        }
    }



    [PanEventUsageProfileAttribute(PanEventUsageProfile.ValidationOnly)]
    public sealed class TestThrowingSignalValue :
        PanBaseEventValue.EventAbles<TestThrowingSignalValue>,
        IEventAbleSignalReceiver<int>
    {
        public void ReceiveLocalSignal(IEventAble source, in int signal)
        {
            throw new InvalidOperationException("Expected local signal failure");
        }
    }



    [PanEventUsageProfileAttribute(PanEventUsageProfile.ValidationOnly)]
    public sealed class TestFollowingSignalValue :
        PanBaseEventValue.EventAbles<TestFollowingSignalValue>,
        IEventAbleSignalReceiver<int>
    {
        public int SignalCount { get; private set; }

        protected override void Enable() => SignalCount = 0;

        public void ReceiveLocalSignal(IEventAble source, in int signal)
        {
            SignalCount++;
        }
    }



    [PanEventUsageProfileAttribute(PanEventUsageProfile.ValidationOnly)]
    public sealed class TestRejectingEventValue :
        PanBaseEventValue.EventAblesCondition<TestRejectingEventValue>
    {
        public int PublicPropertyWithoutInspectorAttribute => 1;



        public override bool EnableValueCondition(IEventAble current)
        {
            return false;
        }
    }



    [PanEventUsageProfileAttribute(PanEventUsageProfile.ValidationOnly)]
    public sealed class TestThrowingConditionValue :
        PanBaseEventValue.EventAblesCondition<TestThrowingConditionValue>
    {
        public static bool ThrowOnCondition { get; set; } = true;
        public static TestThrowingConditionValue LastCheckedInstance { get; private set; }



        public static void ResetState()
        {
            ThrowOnCondition = true;
            LastCheckedInstance = null;
        }



        public override bool EnableValueCondition(IEventAble current)
        {
            LastCheckedInstance = this;
            if (ThrowOnCondition)
            {
                throw new InvalidOperationException("Expected condition failure.");
            }

            return true;
        }
    }



    [PanEventUsageProfileAttribute(PanEventUsageProfile.ValidationOnly)]
    public sealed class TestThrowingEnableValue : PanBaseEventValue.EventAbles<TestThrowingEnableValue>
    {
        public static bool ThrowOnEnable { get; set; } = true;
        public int EnableAttemptCount { get; private set; }



        protected override void Enable()
        {
            EnableAttemptCount++;
            if (ThrowOnEnable)
            {
                throw new InvalidOperationException("Expected enable failure.");
            }
        }
    }



    [PanEventUsageProfileAttribute(PanEventUsageProfile.ValidationOnly)]
    public sealed class TestReentrantRequireValue : PanBaseEventValue.EventAbles<TestReentrantRequireValue>
    {
        public static TestReentrantRequireValue ReenteredValue { get; private set; }
        public int EnableCount { get; private set; }

        [ShowInInspector]
        private int Editor_EnableCount => EnableCount;



        public static void ResetState()
        {
            ReenteredValue = null;
        }



        protected override void Enable()
        {
            EnableCount++;
            ReenteredValue = Current.EventAble.Require<TestReentrantRequireValue>();
        }
    }



    [PanEventUsageProfileAttribute(PanEventUsageProfile.ValidationOnly)]
    public sealed class TestCustomTargetConditionValue :
        PanBaseEventValue.CustomEventAblesCondition<TestCustomTargetConditionValue, EventManagerTestOwner>
    {
        public static bool AllowEnable { get; set; }
        public static int ConditionCallCount { get; private set; }



        public static void ResetState()
        {
            AllowEnable = false;
            ConditionCallCount = 0;
        }



        public override bool EnableValueCondition(IEventAble current)
        {
            ConditionCallCount++;
            return AllowEnable;
        }
    }



    [PanEventUsageProfileAttribute(PanEventUsageProfile.ValidationOnly)]
    public sealed class TestReentrantConditionValue :
        PanBaseEventValue.EventAblesCondition<TestReentrantConditionValue>
    {
        public static int ConditionCallCount { get; private set; }
        public static bool ReentrantGainResult { get; private set; }
        public static TestReentrantConditionValue ReentrantValue { get; private set; }



        public static void ResetState()
        {
            ConditionCallCount = 0;
            ReentrantGainResult = false;
            ReentrantValue = null;
        }



        public override bool EnableValueCondition(IEventAble current)
        {
            ConditionCallCount++;
            ReentrantGainResult = current.EventAble.TryGainCondition(out TestReentrantConditionValue value);
            ReentrantValue = value;
            return true;
        }
    }



    [PanEventUsageProfileAttribute(PanEventUsageProfile.ValidationOnly)]
    public sealed class TestSelfRemovingValue : PanBaseEventValue.EventAbles<TestSelfRemovingValue>
    {
        public bool NestedRemovalResult { get; private set; }



        protected override void Disable()
        {
            NestedRemovalResult = Current.EventAble.RemoveValue(this, false);
        }
    }



    [PanEventUsageProfileAttribute(PanEventUsageProfile.ValidationOnly)]
    public sealed class TestResetFirstValue : PanBaseEventValue.EventAbles<TestResetFirstValue>
    {
        public int DisableCount { get; private set; }
        public bool? RemovedSibling { get; private set; }



        protected override void Disable()
        {
            DisableCount++;
            RemovedSibling = Current.EventAble.RemoveValue<TestResetSecondValue>();
        }
    }



    [PanEventUsageProfileAttribute(PanEventUsageProfile.ValidationOnly)]
    public sealed class TestResetSecondValue : PanBaseEventValue.EventAbles<TestResetSecondValue>
    {
        public int DisableCount { get; private set; }
        public bool? RemovedSibling { get; private set; }



        protected override void Disable()
        {
            DisableCount++;
            RemovedSibling = Current.EventAble.RemoveValue<TestResetFirstValue>();
        }
    }



    [PanEventUsageProfileAttribute(PanEventUsageProfile.ValidationOnly)]
    public sealed class TestThrowingDisableValue : PanBaseEventValue.EventAbles<TestThrowingDisableValue>
    {
        public bool ThrowOnDisable { get; set; } = true;
        public int DisableCount { get; private set; }



        protected override void Disable()
        {
            DisableCount++;
            if (ThrowOnDisable)
            {
                throw new InvalidOperationException("Expected disable failure.");
            }
        }
    }
}
