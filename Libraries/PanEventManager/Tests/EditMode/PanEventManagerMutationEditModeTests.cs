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
        [Test]
        public void EventAble_ConditionFailure_ReturnsValueToPoolWithoutAttaching()
        {
            PanEventGeneralManager.Initialize();
            var owner = new EventManagerTestOwner();

            bool gained = owner.EventAble.TryGainCondition<TestRejectingEventValue>(
                out TestRejectingEventValue rejected);

            Assert.IsFalse(gained);
            Assert.IsNull(rejected);
            Assert.IsFalse(owner.EventAble.CheckValueTable<TestRejectingEventValue>(false));

            TestRejectingEventValue pooled =
                PanEventGeneralManager.EventValueManager.PopEventValue<TestRejectingEventValue>(null);

            Assert.IsNotNull(pooled);
            Assert.IsFalse(pooled.Valid_CurrentEventAble);

            PanEventGeneralManager.EventValueManager.PushEventValue(pooled);
        }



        [Test]
        public void EventAble_ConditionException_ReturnsValueToPoolWithoutAttaching()
        {
            PanEventGeneralManager.Initialize();
            var owner = new EventManagerTestOwner();

            Assert.Throws<InvalidOperationException>(
                () => owner.EventAble.TryGainCondition<TestThrowingConditionValue>(out _));

            Assert.IsFalse(owner.EventAble.CheckValueTable<TestThrowingConditionValue>(false));

            TestThrowingConditionValue pooled =
                PanEventGeneralManager.EventValueManager.PopEventValue<TestThrowingConditionValue>(null);

            Assert.AreSame(TestThrowingConditionValue.LastCheckedInstance, pooled);
            Assert.IsFalse(pooled.Valid_CurrentEventAble);
            Assert.IsNull(
                PanEventGeneralManager.EventValueManager.PopEventValue<TestThrowingConditionValue>(null));
            PanEventGeneralManager.EventValueManager.PushEventValue(pooled);

            TestThrowingConditionValue.ThrowOnCondition = false;

            Assert.IsTrue(owner.EventAble.TryGainCondition<TestThrowingConditionValue>(out var recovered));
            Assert.AreSame(pooled, recovered);
        }



        [Test]
        public void EventAble_EnableException_RollsBackOwnerAndReturnsValueToPool()
        {
            PanEventGeneralManager.Initialize();
            var owner = new EventManagerTestOwner();

            Assert.Throws<InvalidOperationException>(
                () => owner.EventAble.Require<TestThrowingEnableValue>());

            Assert.IsFalse(owner.EventAble.CheckValueTable<TestThrowingEnableValue>(false));

            TestThrowingEnableValue pooled =
                PanEventGeneralManager.EventValueManager.PopEventValue<TestThrowingEnableValue>(null);

            Assert.IsNotNull(pooled);
            Assert.IsFalse(pooled.Valid_CurrentEventAble);
            Assert.AreEqual(1, pooled.EnableAttemptCount);
            Assert.IsNull(
                PanEventGeneralManager.EventValueManager.PopEventValue<TestThrowingEnableValue>(null));
            PanEventGeneralManager.EventValueManager.PushEventValue(pooled);

            TestThrowingEnableValue.ThrowOnEnable = false;
            TestThrowingEnableValue recovered = owner.EventAble.Require<TestThrowingEnableValue>();

            Assert.AreSame(pooled, recovered);
            Assert.IsTrue(recovered.Valid_CurrentEventAble);
        }



        [Test]
        public void EventAble_EnableReentrantRequire_ReturnsReservedInstance()
        {
            PanEventGeneralManager.Initialize();
            var owner = new EventManagerTestOwner();

            TestReentrantRequireValue value = owner.EventAble.Require<TestReentrantRequireValue>();

            Assert.AreSame(value, TestReentrantRequireValue.ReenteredValue);
            Assert.AreEqual(1, value.EnableCount);
            Assert.IsTrue(owner.EventAble.CheckValueTable<TestReentrantRequireValue>(true));
        }



        [Test]
        public void EventAble_CustomTargetCondition_IsEvaluated()
        {
            PanEventGeneralManager.Initialize();
            var owner = new EventManagerTestOwner();

            bool gained = owner.EventAble.TryGain<TestCustomTargetConditionValue, EventManagerTestOwner>(
                out TestCustomTargetConditionValue result);

            Assert.IsFalse(gained);
            Assert.IsNull(result);
            Assert.AreEqual(1, TestCustomTargetConditionValue.ConditionCallCount);
            Assert.IsFalse(owner.EventAble.CheckValueTable<TestCustomTargetConditionValue>(false));
        }



        [Test]
        public void EventAble_TryGetActive_ReturnsOnlyActiveValueWithoutMutation()
        {
            PanEventGeneralManager.Initialize();
            var owner = new EventManagerTestOwner();
            TestLifecycleValue value = owner.EventAble.Require<TestLifecycleValue>();
            uint activeRevision = owner.EventAble.EventValueTableRevision;

            Assert.IsTrue(owner.EventAble.TryGetActive(out TestLifecycleValue active));
            Assert.AreSame(value, active);
            Assert.AreEqual(1, value.EnableCount);
            Assert.AreEqual(activeRevision, owner.EventAble.EventValueTableRevision);

            Assert.IsTrue(owner.EventAble.RemoveValue(value, true, false));
            uint inactiveRevision = owner.EventAble.EventValueTableRevision;

            Assert.IsFalse(owner.EventAble.TryGetActive(out TestLifecycleValue inactive));
            Assert.IsNull(inactive);
            Assert.AreEqual(1, value.EnableCount);
            Assert.AreEqual(inactiveRevision, owner.EventAble.EventValueTableRevision);

            Assert.IsFalse(owner.EventAble.TryGetActive(out TestFollowingSignalValue missing));
            Assert.IsNull(missing);
            Assert.AreEqual(inactiveRevision, owner.EventAble.EventValueTableRevision);
        }



        [Test]
        public void EventAble_TryGetActive_CustomTarget_ValidatesOwnerTypeWithoutMutation()
        {
            PanEventGeneralManager.Initialize();
            var owner = new EventManagerTestOwner();
            TestCustomTargetConditionValue.AllowEnable = true;
            Assert.IsTrue(owner.EventAble.TryGain<TestCustomTargetConditionValue, EventManagerTestOwner>(
                out TestCustomTargetConditionValue value));
            uint revision = owner.EventAble.EventValueTableRevision;

            Assert.IsTrue(owner.EventAble.TryGetActive<TestCustomTargetConditionValue, EventManagerTestOwner>(
                out TestCustomTargetConditionValue active));
            Assert.AreSame(value, active);
            Assert.AreEqual(1, TestCustomTargetConditionValue.ConditionCallCount);
            Assert.AreEqual(revision, owner.EventAble.EventValueTableRevision);

            var wrongOwner = new TestEventAbleControllerOwner();
            wrongOwner.InitializeController(4);

            Assert.IsFalse(wrongOwner.EventAble.TryGetActive<TestCustomTargetConditionValue, EventManagerTestOwner>(out _));
            Assert.AreEqual(0u, wrongOwner.EventAble.EventValueTableRevision);

            Assert.IsTrue(owner.EventAble.RemoveValue(value, true, false));
            revision = owner.EventAble.EventValueTableRevision;

            Assert.IsFalse(owner.EventAble.TryGetActive<TestCustomTargetConditionValue, EventManagerTestOwner>(out _));
            Assert.AreEqual(1, TestCustomTargetConditionValue.ConditionCallCount);
            Assert.AreEqual(revision, owner.EventAble.EventValueTableRevision);
        }



        [Test]
        public void EventAble_ConditionReentrantGain_ReturnsFalseWithoutRecursingAndOuterGainCompletes()
        {
            PanEventGeneralManager.Initialize();
            var owner = new EventManagerTestOwner();

            bool gained = owner.EventAble.TryGainCondition<TestReentrantConditionValue>(
                out TestReentrantConditionValue value);

            Assert.IsTrue(gained);
            Assert.IsNotNull(value);
            Assert.IsTrue(value.Valid_CurrentEventAble);
            Assert.AreEqual(1, TestReentrantConditionValue.ConditionCallCount);
            Assert.IsFalse(TestReentrantConditionValue.ReentrantGainResult);
            Assert.IsNull(TestReentrantConditionValue.ReentrantValue);
        }



        [Test]
        public void EventAble_RemoveWithoutTableRemoval_KeepsValueOutOfPoolUntilReactivated()
        {
            PanEventGeneralManager.Initialize();
            var owner = new EventManagerTestOwner();
            TestLifecycleValue value = owner.EventAble.Require<TestLifecycleValue>();

            Assert.IsTrue(owner.EventAble.RemoveValue(value, true, false));
            Assert.IsFalse(value.Valid_CurrentEventAble);
            Assert.IsTrue(owner.EventAble.CheckValueTable<TestLifecycleValue>(false));
            Assert.IsFalse(owner.EventAble.CheckValueTable<TestLifecycleValue>(true));
            Assert.IsNull(PanEventGeneralManager.EventValueManager.PopEventValue<TestLifecycleValue>(null));

            TestLifecycleValue reactivated = owner.EventAble.Require<TestLifecycleValue>();

            Assert.AreSame(value, reactivated);
            Assert.IsTrue(reactivated.Valid_CurrentEventAble);

            Assert.IsTrue(owner.EventAble.RemoveValue(reactivated, true, false));
            Assert.DoesNotThrow(owner.EventAble.Reset);
            Assert.IsFalse(owner.EventAble.CheckValueTable<TestLifecycleValue>(false));

            TestLifecycleValue pooled =
                PanEventGeneralManager.EventValueManager.PopEventValue<TestLifecycleValue>(null);

            Assert.AreSame(value, pooled);
            Assert.IsNull(PanEventGeneralManager.EventValueManager.PopEventValue<TestLifecycleValue>(null));
            PanEventGeneralManager.EventValueManager.PushEventValue(pooled);
        }



        [Test]
        public void EventAble_RemoveByStaleReference_DoesNotRemoveCurrentValue()
        {
            PanEventGeneralManager.Initialize();
            var firstOwner = new EventManagerTestOwner();
            var secondOwner = new EventManagerTestOwner();
            TestLifecycleValue staleValue = firstOwner.EventAble.Require<TestLifecycleValue>();

            Assert.IsTrue(firstOwner.EventAble.RemoveValue(staleValue));
            Assert.AreSame(staleValue, secondOwner.EventAble.Require<TestLifecycleValue>());

            TestLifecycleValue currentValue = firstOwner.EventAble.Require<TestLifecycleValue>();

            Assert.AreNotSame(staleValue, currentValue);
            Assert.IsFalse(firstOwner.EventAble.RemoveValue(staleValue));
            Assert.AreSame(currentValue, firstOwner.EventAble.Peek<TestLifecycleValue>());
            Assert.IsTrue(currentValue.Valid_CurrentEventAble);
        }



        [Test]
        public void EventAble_ReentrantSelfRemoval_DoesNotPushValueTwice()
        {
            PanEventGeneralManager.Initialize();
            var owner = new EventManagerTestOwner();
            TestSelfRemovingValue value = owner.EventAble.Require<TestSelfRemovingValue>();

            Assert.IsTrue(owner.EventAble.RemoveValue(value));
            Assert.IsTrue(value.NestedRemovalResult);
            Assert.IsFalse(owner.EventAble.CheckValueTable<TestSelfRemovingValue>(false));

            TestSelfRemovingValue firstPop =
                PanEventGeneralManager.EventValueManager.PopEventValue<TestSelfRemovingValue>(null);
            TestSelfRemovingValue secondPop =
                PanEventGeneralManager.EventValueManager.PopEventValue<TestSelfRemovingValue>(null);

            Assert.AreSame(value, firstPop);
            Assert.IsNull(secondPop);
            PanEventGeneralManager.EventValueManager.PushEventValue(firstPop);
        }
    }
}
