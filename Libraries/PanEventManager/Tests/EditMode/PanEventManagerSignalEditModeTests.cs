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
        public void DispatchLocal_SelfRemoval_UsesMutationSafeSnapshot()
        {
            PanEventGeneralManager.Initialize();
            var owner = new EventManagerTestOwner(EventAbleTableAllocationMode.Lazy);
            TestLifecycleValue value = TestLifecycleValue.Require(owner);
            value.RemoveOnSignal = true;
            int signal = 7;

            int receivedCount = owner.EventAble.DispatchLocal(in signal);

            Assert.AreEqual(1, receivedCount);
            Assert.AreEqual(1, value.SignalCount);
            Assert.IsFalse(owner.EventAble.CheckValueTable<TestLifecycleValue>(false));
            Assert.GreaterOrEqual(owner.EventAble.EventValueTableRevision, 2u);
        }



        [Test]
        public void DispatchLocal_ThrowingReceiver_DoesNotBlockLaterReceiver()
        {
            PanEventGeneralManager.Initialize();
            var owner = new EventManagerTestOwner(EventAbleTableAllocationMode.Lazy);
            TestThrowingSignalValue.Require(owner);
            TestFollowingSignalValue follower = TestFollowingSignalValue.Require(owner);
            int signal = 9;

            LogAssert.Expect(LogType.Exception, "InvalidOperationException: Expected local signal failure");
            int receivedCount = owner.EventAble.DispatchLocal(in signal);

            Assert.AreEqual(1, receivedCount);
            Assert.AreEqual(1, follower.SignalCount);
        }



    }
}
