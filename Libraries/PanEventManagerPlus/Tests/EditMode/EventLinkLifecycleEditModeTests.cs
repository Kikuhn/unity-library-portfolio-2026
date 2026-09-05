using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Pan.Event;
using Pan.Util;
using UnityEngine;



namespace Pan.EventManagers.Plus.Tests
{
    public sealed class EventLinkLifecycleEditModeTests
    {
        private TestEventAbleOwner owner;
        private readonly List<GameObject> createdObjects = new();



        [SetUp]
        public void SetUp()
        {
            ResetGeneralManager();
            PanEventGeneralManager.Initialize();
            owner = new TestEventAbleOwner();
        }



        [TearDown]
        public void TearDown()
        {
            owner?.EventAble.Reset();

            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }

            createdObjects.Clear();
            owner = null;
            ResetGeneralManager();
        }



        [Test]
        public void SetLink_ReplacingAndRemoving_NotifiesOldThenNew_AndRejectsReentry()
        {
            TestLinkEventValue value = owner.EventAble.Require<TestLinkEventValue>();
            SimpleUpdateObject firstLink = CreateLink("EventManagerPlusFirstLink");
            SimpleUpdateObject secondLink = CreateLink("EventManagerPlusSecondLink");

            Assert.IsTrue(value.SetLink(firstLink));
            value.ClearNotifications();
            value.ReenterRemoveDuringRemovalNotification = true;

            Assert.IsTrue(value.SetLink(secondLink));

            Assert.AreEqual(2, value.NotificationArguments.Count);
            Assert.IsNull(value.NotificationArguments[0]);
            Assert.AreSame(firstLink, value.CurrentLinksDuringNotification[0]);
            Assert.AreSame(secondLink, value.NotificationArguments[1]);
            Assert.AreSame(secondLink, value.CurrentLinksDuringNotification[1]);
            Assert.AreEqual(false, value.LastReentrantRemoveResult);
            Assert.AreSame(secondLink, value.CurrentLink);

            value.ClearNotifications();
            value.ReenterRemoveDuringRemovalNotification = true;

            Assert.IsTrue(value.RemoveLink());

            Assert.AreEqual(1, value.NotificationArguments.Count);
            Assert.IsNull(value.NotificationArguments[0]);
            Assert.AreSame(secondLink, value.CurrentLinksDuringNotification[0]);
            Assert.AreEqual(false, value.LastReentrantRemoveResult);
            Assert.IsFalse(value.HasLink);
        }



        [Test]
        public void Disable_NotifiesRemovalBeforeClearing_AndPooledReuseHasNoStaleLink()
        {
            TestLinkEventValue value = owner.EventAble.Require<TestLinkEventValue>();
            SimpleUpdateObject link = CreateLink("EventManagerPlusDisableLink");

            Assert.IsTrue(value.SetLink(link));
            value.ClearNotifications();

            Assert.IsTrue(owner.EventAble.RemoveValue<TestLinkEventValue>());

            Assert.AreEqual(1, value.NotificationArguments.Count);
            Assert.IsNull(value.NotificationArguments[0]);
            Assert.AreSame(link, value.CurrentLinksDuringNotification[0]);
            Assert.IsFalse(value.HasLink);
            Assert.IsFalse(link.gameObject.activeSelf);
            Assert.IsFalse(owner.EventAble.CheckValueTable<TestLinkEventValue>(false));

            TestLinkEventValue reused = owner.EventAble.Require<TestLinkEventValue>();

            Assert.AreSame(value, reused);
            Assert.IsFalse(reused.HasLink);
        }



        [Test]
        public void TypeDiscovery_ExcludesEventValueFromTestAssembly()
        {
            Assert.IsFalse(PanEventsInitializeSettingSbjectBase.GetPanBaseEventValueTypesAll()
                .Contains(typeof(TestLinkEventValue)));
        }



        [TestCase(false)]
        [TestCase(true)]
        public void SetLink_WhenRemovalObserverEndsValueLifecycle_AbortsReplacement(bool resetOwner)
        {
            TestLinkEventValue value = owner.EventAble.Require<TestLinkEventValue>();
            SimpleUpdateObject firstLink = CreateLink("EventManagerPlusLifecycleFirstLink");
            SimpleUpdateObject secondLink = CreateLink("EventManagerPlusLifecycleSecondLink");

            Assert.IsTrue(value.SetLink(firstLink));
            value.ClearNotifications();
            value.RemoveOwnerDuringRemovalNotification = !resetOwner;
            value.ResetOwnerDuringRemovalNotification = resetOwner;

            Assert.IsFalse(value.SetLink(secondLink));

            Assert.AreEqual(resetOwner, value.OwnerResetCalled);
            if (resetOwner)
            {
                Assert.IsNull(value.LastOwnerRemovalResult);
            }
            else
            {
                Assert.AreEqual(true, value.LastOwnerRemovalResult);
            }
            Assert.IsFalse(value.Valid_CurrentEventAble);
            Assert.IsFalse(value.HasLink);
            Assert.IsFalse(firstLink.gameObject.activeSelf);
            Assert.IsTrue(secondLink.gameObject.activeSelf);
            Assert.IsFalse(owner.EventAble.CheckValueTable<TestLinkEventValue>(false));

            TestLinkEventValue reused = owner.EventAble.Require<TestLinkEventValue>();

            Assert.AreSame(value, reused);
            Assert.IsFalse(reused.HasLink);
        }



        private SimpleUpdateObject CreateLink(string objectName)
        {
            var gameObject = new GameObject(objectName);
            createdObjects.Add(gameObject);
            return gameObject.AddComponent<SimpleUpdateObject>();
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
    }



    public sealed class TestEventAbleOwner : IEventAble
    {
        public TestEventAbleOwner()
        {
            EventAble = new EventAble(this, 4);
        }



        public EventAble EventAble { get; }
    }



    public sealed class TestLinkEventValue :
        BaseEventValue_Link<TestLinkEventValue, SimpleUpdateObject>,
        Observer_LinkManager.IS.IAlarm_ControlLinks
    {
        public List<MonoBehaviour> NotificationArguments { get; } = new();
        public List<SimpleUpdateObject> CurrentLinksDuringNotification { get; } = new();
        public bool ReenterRemoveDuringRemovalNotification { get; set; }
        public bool RemoveOwnerDuringRemovalNotification { get; set; }
        public bool ResetOwnerDuringRemovalNotification { get; set; }
        public bool? LastReentrantRemoveResult { get; private set; }
        public bool? LastOwnerRemovalResult { get; private set; }
        public bool OwnerResetCalled { get; private set; }



        protected override void Enable()
        {
            Observer_LinkManager.Require(Current).AddOB(this);
        }



        protected override void DisableCurrent()
        {
            if (Observer_LinkManager.TryPeek(Current, out Observer_LinkManager observer))
            {
                observer.RemoveOB(this);
            }
        }



        public void Alarm_ControlLinks(MonoBehaviour link)
        {
            NotificationArguments.Add(link);
            CurrentLinksDuringNotification.Add(CurrentLink);

            if (link == null && ReenterRemoveDuringRemovalNotification)
            {
                LastReentrantRemoveResult = RemoveLink();
            }

            if (link == null && ResetOwnerDuringRemovalNotification)
            {
                Current.EventAble.Reset();
                OwnerResetCalled = true;
            }
            else if (link == null && RemoveOwnerDuringRemovalNotification)
            {
                LastOwnerRemovalResult = Current.EventAble.RemoveValue<TestLinkEventValue>();
            }
        }



        public void ClearNotifications()
        {
            NotificationArguments.Clear();
            CurrentLinksDuringNotification.Clear();
            LastReentrantRemoveResult = null;
            LastOwnerRemovalResult = null;
            OwnerResetCalled = false;
        }
    }
}
