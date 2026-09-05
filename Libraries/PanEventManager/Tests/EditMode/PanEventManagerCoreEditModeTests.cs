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
        public void Initialize_WithoutSettings_IgnoresCachedTestFixtures()
        {
            SetTypeDiscoveryCachesForLifecycleFixtures();
            Assert.That(
                PanEventsInitializeSettingSbjectBase.GetPanBaseEventTypesAll(),
                Does.Contain(typeof(TestLifecycleEvent)));
            Assert.That(
                PanEventsInitializeSettingSbjectBase.GetPanBaseEventValueTypesAll(),
                Does.Contain(typeof(TestLifecycleValue)));

            PanEventGeneralManager.Initialize();

            Assert.IsTrue(PanEventGeneralManager.IsInitialize);
            Assert.IsNotNull(PanEventGeneralManager.EventManager);
            Assert.IsNotNull(PanEventGeneralManager.EventValueManager);
            Assert.IsFalse(PanEventGeneralManager.EventManager
                .ContainsEvent<TestLifecycleEvent>());
            Assert.IsFalse(PanEventGeneralManager.EventValueManager
                .GetEventValuePoolDictionary.ContainsKey(typeof(TestLifecycleValue)));
            Assert.AreEqual(0, TestLifecycleEvent.InstanceCount);
        }



        [Test]
        public void EventAble_LazyTable_AllocatesOnlyWhenValueIsAttached()
        {
            PanEventGeneralManager.Initialize();
            var owner = new EventManagerTestOwner(EventAbleTableAllocationMode.Lazy);

            Assert.IsFalse(owner.EventAble.IsEventValueTableAllocated);
            Assert.IsFalse(owner.EventAble.CheckValueTable<TestLifecycleValue>(false));
            Assert.IsFalse(owner.EventAble.IsEventValueTableAllocated);

            TestLifecycleValue value = TestLifecycleValue.Require(owner);

            Assert.IsNotNull(value);
            Assert.IsTrue(owner.EventAble.IsEventValueTableAllocated);
            Assert.AreEqual(1u, owner.EventAble.EventValueTableRevision);
        }



        [Test]
        public void Initialize_WithSettings_AutoCreatesEventAndPrewarmsValuePool()
        {
            eventSettings = ScriptableObject.CreateInstance<PanEventInitializeSettingSbject>();
            eventValueSettings = ScriptableObject.CreateInstance<PanEventValueInitializeSettingSbject>();
            SetTypeDiscoveryCachesForLifecycleFixtures();
            eventSettings.InitializeSettings();
            eventValueSettings.InitializeSettings();

            PanEventInitializeSetting eventSetting = eventSettings.GetPanEventInitializeSettings
                .Single(setting => setting.GetPanBaseEventType == typeof(TestLifecycleEvent));
            PanEventValueInitializeSetting eventValueSetting = eventValueSettings.GetPanEventValueInitializeSettings
                .Single(setting => setting.GetPanBaseEventType == typeof(TestLifecycleValue));
            eventSetting.AutoInitialize = true;
            eventValueSetting.CreateCount = 1;

            PanEventGeneralManager.Initialize(eventSettings, eventValueSettings);

            Assert.IsTrue(PanEventGeneralManager.IsInitialize);
            Assert.AreEqual(1, TestLifecycleEvent.InstanceCount);

            TestLifecycleEvent configuredEvent =
                PanEventGeneralManager.EventManager.GetEvent<TestLifecycleEvent>();
            TestLifecycleValue prewarmedValue =
                PanEventGeneralManager.EventValueManager.PopEventValue<TestLifecycleValue>(null);

            Assert.IsNotNull(configuredEvent);
            Assert.AreEqual(1, TestLifecycleEvent.InstanceCount);
            Assert.IsNotNull(prewarmedValue);
            Assert.IsFalse(prewarmedValue.Valid_CurrentEventAble);

            PanEventGeneralManager.EventValueManager.PushEventValue(prewarmedValue);
        }



        [Test]
        public void EventManager_ContainsEvent_DistinguishesRegistrationFromLiveInstance()
        {
            eventSettings = ScriptableObject.CreateInstance<PanEventInitializeSettingSbject>();
            SetTypeDiscoveryCachesForLifecycleFixtures();
            eventSettings.InitializeSettings();

            PanEventInitializeSetting eventSetting = eventSettings.GetPanEventInitializeSettings
                .Single(setting => setting.GetPanBaseEventType == typeof(TestLifecycleEvent));
            eventSetting.AutoInitialize = false;

            PanEventGeneralManager.Initialize(eventSettings);

            Assert.IsTrue(PanEventGeneralManager.EventManager.ContainsEvent<TestLifecycleEvent>());
            Assert.IsTrue(PanEventGeneralManager.EventManager.ContainsEvent(typeof(TestLifecycleEvent)));
            Assert.IsFalse(PanEventGeneralManager.EventManager.HasEventInstance<TestLifecycleEvent>());
            Assert.IsFalse(PanEventGeneralManager.EventManager.HasEventInstance(typeof(TestLifecycleEvent)));

            TestLifecycleEvent firstInstance = PanEventGeneralManager.EventManager.GetEvent<TestLifecycleEvent>();

            Assert.IsNotNull(firstInstance);
            Assert.IsTrue(PanEventGeneralManager.EventManager.HasEventInstance<TestLifecycleEvent>());
            Assert.IsTrue(PanEventGeneralManager.EventManager.HasEventInstance(typeof(TestLifecycleEvent)));
            Assert.AreEqual(1, TestLifecycleEvent.InstanceCount);

            Assert.IsTrue(PanEventGeneralManager.EventManager.ReleaseEvent<TestLifecycleEvent>());
            Assert.IsTrue(PanEventGeneralManager.EventManager.ContainsEvent<TestLifecycleEvent>());
            Assert.IsFalse(PanEventGeneralManager.EventManager.HasEventInstance<TestLifecycleEvent>());

            TestLifecycleEvent secondInstance = PanEventGeneralManager.EventManager.GetEvent(typeof(TestLifecycleEvent)) as TestLifecycleEvent;

            Assert.IsNotNull(secondInstance);
            Assert.AreNotSame(firstInstance, secondInstance);
            Assert.IsTrue(PanEventGeneralManager.EventManager.HasEventInstance(typeof(TestLifecycleEvent)));
            Assert.AreEqual(2, TestLifecycleEvent.InstanceCount);
            Assert.IsTrue(PanEventGeneralManager.EventManager.ReleaseEvent(typeof(TestLifecycleEvent)));
        }



        [Test]
        public void EventAbleController_ExplicitInitialCapacity_IsPreserved()
        {
            var gameObject = new GameObject(nameof(TestEventAbleControllerOwner));

            try
            {
                TestEventAbleControllerOwner owner = gameObject.AddComponent<TestEventAbleControllerOwner>();
                EventAbleController defaultController = EventAbleController.Initialize(owner);
                EventAbleController controller = owner.InitializeController(8);
                FieldInfo capacityField = typeof(EventAbleController).GetField(
                    "InitializeEventTableCapacity",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.IsNotNull(capacityField);
                Assert.IsNotNull(controller);
                Assert.AreSame(controller.EventAble, owner.EventAble);
                Assert.AreEqual(0, capacityField.GetValue(defaultController));
                Assert.AreEqual(8, capacityField.GetValue(controller));
                Assert.Throws<ArgumentOutOfRangeException>(() => EventAbleController.Initialize(owner, -1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }



        [Test]
        public void TypeDiscoveryAndSettings_ExcludeEditorAndTestAssemblyTypes()
        {
            Type[] eventTypes = PanEventsInitializeSettingSbjectBase.GetPanBaseEventTypesAll();
            Type[] eventValueTypes = PanEventsInitializeSettingSbjectBase.GetPanBaseEventValueTypesAll();

            Assert.IsFalse(eventTypes.Contains(typeof(TestLifecycleEvent)));
            Assert.IsFalse(eventValueTypes.Contains(typeof(TestLifecycleValue)));
            Assert.IsFalse(eventValueTypes.Contains(typeof(TestRejectingEventValue)));
            Assert.IsFalse(PanEventManager.Initialize_byReflection()
                .ContainsEvent<TestLifecycleEvent>());
            Assert.IsFalse(PanEventValueManager.Initialize_EachCreatCount()
                .GetEventValuePoolDictionary.ContainsKey(typeof(TestLifecycleValue)));

            eventSettings = ScriptableObject.CreateInstance<PanEventInitializeSettingSbject>();
            eventValueSettings = ScriptableObject.CreateInstance<PanEventValueInitializeSettingSbject>();
            eventSettings.InitializeSettings();
            eventValueSettings.InitializeSettings();

            string testAssemblyName = typeof(TestLifecycleEvent).Assembly.GetName().Name;

            Assert.IsFalse(eventSettings.GetPanEventInitializeSettings
                .Any(setting => setting.GetAssemblyName == testAssemblyName));
            Assert.IsFalse(eventValueSettings.GetPanEventValueInitializeSettings
                .Any(setting => setting.GetAssemblyName == testAssemblyName));
        }



        [Test]
        public void InitializeSettings_TransientObjectsRemainUnsaved()
        {
            eventSettings = ScriptableObject.CreateInstance<PanEventInitializeSettingSbject>();
            eventValueSettings = ScriptableObject.CreateInstance<PanEventValueInitializeSettingSbject>();

            eventSettings.InitializeSettings();
            eventValueSettings.InitializeSettings();

            Assert.IsFalse(UnityEditor.EditorUtility.IsPersistent(eventSettings));
            Assert.IsFalse(UnityEditor.EditorUtility.IsPersistent(eventValueSettings));
            Assert.IsFalse(UnityEditor.AssetDatabase.Contains(eventSettings));
            Assert.IsFalse(UnityEditor.AssetDatabase.Contains(eventValueSettings));
            Assert.AreEqual(string.Empty, UnityEditor.AssetDatabase.GetAssetPath(eventSettings));
            Assert.AreEqual(string.Empty, UnityEditor.AssetDatabase.GetAssetPath(eventValueSettings));
        }



        [Test]
        public void InitializeSettings_DoesNotSaveUnrelatedDirtyAsset()
        {
            string assetPath = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(
                "Assets/PanEventManagerDirtyAssetGuard.asset");
            var dirtySettings = ScriptableObject.CreateInstance<PanEventInitializeSettingSbject>();

            try
            {
                UnityEditor.AssetDatabase.CreateAsset(dirtySettings, assetPath);
                UnityEditor.AssetDatabase.SaveAssetIfDirty(dirtySettings);
                UnityEditor.EditorUtility.SetDirty(dirtySettings);
                Assert.IsTrue(UnityEditor.EditorUtility.IsDirty(dirtySettings));

                eventSettings = ScriptableObject.CreateInstance<PanEventInitializeSettingSbject>();
                eventValueSettings = ScriptableObject.CreateInstance<PanEventValueInitializeSettingSbject>();
                eventSettings.InitializeSettings();
                eventValueSettings.InitializeSettings();

                Assert.IsTrue(UnityEditor.EditorUtility.IsDirty(dirtySettings));
            }
            finally
            {
                if (UnityEditor.AssetDatabase.Contains(dirtySettings))
                {
                    UnityEditor.AssetDatabase.DeleteAsset(assetPath);
                }
                else if (dirtySettings != null)
                {
                    UnityEngine.Object.DestroyImmediate(dirtySettings);
                }
            }
        }
    }
}
