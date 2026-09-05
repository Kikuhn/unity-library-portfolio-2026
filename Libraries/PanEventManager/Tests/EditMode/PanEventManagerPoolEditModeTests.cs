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
        public void EventAble_RemoveAndReattach_ReusesPooledValue_AndResetClearsState()
        {
            PanEventGeneralManager.Initialize();
            var owner = new EventManagerTestOwner();

            TestLifecycleValue first = owner.EventAble.Require<TestLifecycleValue>();

            Assert.IsTrue(first.Valid_CurrentEventAble);
            Assert.AreEqual(1, first.EnableCount);
            Assert.IsTrue(owner.EventAble.RemoveValue<TestLifecycleValue>());
            Assert.IsFalse(first.Valid_CurrentEventAble);
            Assert.IsFalse(owner.EventAble.CheckValueTable<TestLifecycleValue>(false));

            TestLifecycleValue reattached = owner.EventAble.Require<TestLifecycleValue>();

            Assert.AreSame(first, reattached);
            Assert.AreEqual(2, reattached.EnableCount);
            Assert.IsTrue(owner.EventAble.TryPeek<TestLifecycleValue>(out TestLifecycleValue peeked));
            Assert.AreSame(reattached, peeked);

            owner.EventAble.Reset();

            Assert.IsFalse(reattached.Valid_CurrentEventAble);
            Assert.AreEqual(2, reattached.DisableCount);
            Assert.IsFalse(owner.EventAble.CheckValueTable<TestLifecycleValue>(false));
            Assert.IsFalse(owner.EventAble.TryPeek<TestLifecycleValue>(out _));
        }
        [Test]
        public void EventAble_Reset_AllowsDisableCallbacksToRemoveSiblingValues()
        {
            PanEventGeneralManager.Initialize();
            var owner = new EventManagerTestOwner();
            TestResetFirstValue first = owner.EventAble.Require<TestResetFirstValue>();
            TestResetSecondValue second = owner.EventAble.Require<TestResetSecondValue>();

            Assert.DoesNotThrow(owner.EventAble.Reset);

            Assert.AreEqual(1, first.DisableCount);
            Assert.AreEqual(1, second.DisableCount);
            Assert.IsTrue(first.RemovedSibling == true || second.RemovedSibling == true);
            Assert.IsFalse(first.Valid_CurrentEventAble);
            Assert.IsFalse(second.Valid_CurrentEventAble);
            Assert.IsFalse(owner.EventAble.CheckValueTable<TestResetFirstValue>(false));
            Assert.IsFalse(owner.EventAble.CheckValueTable<TestResetSecondValue>(false));
        }



        [Test]
        public void EventAble_DisableException_RestoresGuardAndCanBeRetried()
        {
            PanEventGeneralManager.Initialize();
            var owner = new EventManagerTestOwner();
            TestThrowingDisableValue value = owner.EventAble.Require<TestThrowingDisableValue>();

            Assert.Throws<InvalidOperationException>(
                () => owner.EventAble.RemoveValue<TestThrowingDisableValue>());

            Assert.IsNull(owner.EventAble.IsDisablingEventValue);
            Assert.IsTrue(value.Valid_CurrentEventAble);
            Assert.IsTrue(owner.EventAble.CheckValueTable<TestThrowingDisableValue>(false));

            value.ThrowOnDisable = false;

            Assert.IsTrue(owner.EventAble.RemoveValue<TestThrowingDisableValue>());
            Assert.IsFalse(value.Valid_CurrentEventAble);
            Assert.AreEqual(2, value.DisableCount);
        }



        [Test]
        public void EventAble_ResetException_PreservesTableAndCanBeRetried()
        {
            PanEventGeneralManager.Initialize();
            var owner = new EventManagerTestOwner();
            TestThrowingDisableValue value = owner.EventAble.Require<TestThrowingDisableValue>();

            Assert.Throws<InvalidOperationException>(owner.EventAble.Reset);
            Assert.IsNull(owner.EventAble.IsDisablingEventValue);
            Assert.IsTrue(value.Valid_CurrentEventAble);
            Assert.IsTrue(owner.EventAble.CheckValueTable<TestThrowingDisableValue>(false));

            value.ThrowOnDisable = false;

            Assert.DoesNotThrow(owner.EventAble.Reset);
            Assert.IsFalse(value.Valid_CurrentEventAble);
            Assert.IsFalse(owner.EventAble.CheckValueTable<TestThrowingDisableValue>(false));
            Assert.AreEqual(2, value.DisableCount);

            TestThrowingDisableValue pooled =
                PanEventGeneralManager.EventValueManager.PopEventValue<TestThrowingDisableValue>(null);

            Assert.AreSame(value, pooled);
            Assert.IsNull(
                PanEventGeneralManager.EventValueManager.PopEventValue<TestThrowingDisableValue>(null));
            PanEventGeneralManager.EventValueManager.PushEventValue(pooled);
        }
    }
}
