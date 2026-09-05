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
        public void EventAble_EditorInspector_UsesNativePagedCollectionAndHidesBaseChrome()
        {
            FieldInfo tableField = typeof(EventAble).GetField(
                "EventValueTables",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo masterField = typeof(EventAble).GetField(
                "Master",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(tableField);
            Assert.IsNotNull(masterField);
            Assert.IsTrue(tableField.CustomAttributes.Any(attribute =>
                attribute.AttributeType.Name == "ShowInInspectorAttribute"));
            Assert.IsTrue(tableField.CustomAttributes.Any(attribute =>
                attribute.AttributeType.Name == "InlinePropertyAttribute"));
            Assert.IsTrue(tableField.CustomAttributes.Any(attribute =>
                attribute.AttributeType.Name == "EnableGUIAttribute"));
            Assert.IsTrue(masterField.CustomAttributes.Any(attribute =>
                attribute.AttributeType.Name == "HideInInspector" ||
                attribute.AttributeType.Name == "HideInInspectorAttribute"));

            Type tableType = tableField.FieldType;
            FieldInfo fullViewField = tableType.GetField(
                "EventValueTablesList",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(fullViewField);

            var listSettings = fullViewField.GetCustomAttribute<ListDrawerSettingsAttribute>();
            var searchable = fullViewField.GetCustomAttribute<SearchableAttribute>();

            Assert.IsNotNull(listSettings);
            Assert.IsNotNull(searchable);
            Assert.IsTrue(fullViewField.CustomAttributes.Any(attribute =>
                attribute.AttributeType.Name == "EnableGUIAttribute"));
            Assert.IsFalse(listSettings.IsReadOnly);
            Assert.IsFalse(listSettings.DraggableItems);
            Assert.IsTrue(listSettings.HideAddButton);
            Assert.IsTrue(listSettings.HideRemoveButton);
            Assert.IsTrue(listSettings.ShowPaging);
            Assert.IsFalse(listSettings.ShowFoldout);
            Assert.AreEqual(12, listSettings.NumberOfItemsPerPage);
            Assert.IsTrue(string.IsNullOrEmpty(listSettings.ListElementLabelName));
            Assert.IsFalse(searchable.Recursive);
            Assert.AreEqual(SearchFilterOptions.ISearchFilterableInterface, searchable.FilterOptions);

            FieldInfo contentFilterField = tableType.GetField("editorContentFilter", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo namespaceFilterField = tableType.GetField("editorNamespaceFilter", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(contentFilterField);
            Assert.IsNotNull(namespaceFilterField);
            Assert.IsFalse(contentFilterField.CustomAttributes.Any(attribute =>
                attribute.AttributeType.Name == "HorizontalGroupAttribute"));
            Assert.IsFalse(namespaceFilterField.CustomAttributes.Any(attribute =>
                attribute.AttributeType.Name == "HorizontalGroupAttribute"));

            Type genericEventValueBaseType = typeof(TestLifecycleValue).BaseType?.BaseType;
            Assert.IsNotNull(genericEventValueBaseType);
            PropertyInfo editorInfoProperty = genericEventValueBaseType.GetProperty(
                "Editor_Info",
                BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo editorCurrentProperty = genericEventValueBaseType.GetProperty(
                "Editor_Current",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo disableButtonMethod = genericEventValueBaseType.GetMethod(
                "Editor_DisableCurrent",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNull(disableButtonMethod);

            MemberInfo[] legacyChromeMembers =
            {
                editorInfoProperty,
                editorCurrentProperty
            };

            foreach (MemberInfo legacyChromeMember in legacyChromeMembers.Where(member => member != null))
            {
                Assert.IsTrue(legacyChromeMember.CustomAttributes.Any(attribute =>
                    attribute.AttributeType.Name == "HideInInspector" ||
                    attribute.AttributeType.Name == "HideInInspectorAttribute"));
                Assert.IsFalse(legacyChromeMember.CustomAttributes.Any(attribute =>
                    attribute.AttributeType.Name == "ShowInInspectorAttribute"));
            }
        }



        [Test]
        public void EventAble_EditorBridge_TracksCopiesAndExactOwnership()
        {
            PanEventGeneralManager.Initialize();
            var owner = new EventManagerTestOwner();
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

            PropertyInfo versionProperty = typeof(EventAble).GetProperty(
                "EditorEventValueChangeVersion",
                Flags);
            PropertyInfo countProperty = typeof(EventAble).GetProperty(
                "EditorEventValueCount",
                Flags);
            PropertyInfo capacityProperty = typeof(EventAble).GetProperty(
                "EditorInitialEventValueCapacity",
                Flags);
            MethodInfo copyMethod = typeof(EventAble).GetMethod(
                "EditorCopyEventValues",
                Flags);
            MethodInfo containsMethod = typeof(EventAble).GetMethod(
                "EditorContainsEventValue",
                Flags);

            Assert.IsNotNull(versionProperty);
            Assert.IsNotNull(countProperty);
            Assert.IsNotNull(capacityProperty);
            Assert.IsNotNull(copyMethod);
            Assert.IsNotNull(containsMethod);

            int initialVersion = (int)versionProperty.GetValue(owner.EventAble);
            TestLifecycleValue lifecycleValue = owner.EventAble.Require<TestLifecycleValue>();
            int attachedVersion = (int)versionProperty.GetValue(owner.EventAble);
            var copiedValues = new List<PanBaseEventValue>();

            copyMethod.Invoke(owner.EventAble, new object[] { copiedValues });

            Assert.Greater(attachedVersion, initialVersion);
            Assert.AreEqual(1, countProperty.GetValue(owner.EventAble));
            Assert.AreEqual(4, capacityProperty.GetValue(owner.EventAble));
            Assert.That(copiedValues, Has.Count.EqualTo(1));
            Assert.AreSame(lifecycleValue, copiedValues[0]);
            Assert.AreEqual(true, containsMethod.Invoke(owner.EventAble, new object[] { lifecycleValue }));

            Assert.IsTrue(owner.EventAble.RemoveValue(lifecycleValue));

            int removedVersion = (int)versionProperty.GetValue(owner.EventAble);
            copyMethod.Invoke(owner.EventAble, new object[] { copiedValues });

            Assert.Greater(removedVersion, attachedVersion);
            Assert.AreEqual(0, countProperty.GetValue(owner.EventAble));
            Assert.IsEmpty(copiedValues);
            Assert.AreEqual(false, containsMethod.Invoke(owner.EventAble, new object[] { lifecycleValue }));
        }



        [Test]
        public void EventAble_EditorBridge_OnlyRequestsCommitWhileViewIsDirty()
        {
            PanEventGeneralManager.Initialize();
            var owner = new EventManagerTestOwner();
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

            PropertyInfo needsCommitProperty = typeof(EventAble).GetProperty(
                "EditorNeedsInspectorViewCommit",
                Flags);
            MethodInfo commitMethod = typeof(EventAble).GetMethod(
                "EditorCommitInspectorView",
                Flags);

            Assert.IsNotNull(needsCommitProperty);
            Assert.IsNotNull(commitMethod);
            Assert.AreEqual(true, needsCommitProperty.GetValue(owner.EventAble));
            Assert.AreEqual(true, commitMethod.Invoke(owner.EventAble, null));
            Assert.AreEqual(false, needsCommitProperty.GetValue(owner.EventAble));

            TestLifecycleValue lifecycleValue = owner.EventAble.Require<TestLifecycleValue>();

            Assert.AreEqual(true, needsCommitProperty.GetValue(owner.EventAble));
            Assert.AreEqual(true, commitMethod.Invoke(owner.EventAble, null));
            Assert.AreEqual(false, needsCommitProperty.GetValue(owner.EventAble));

            Assert.IsTrue(owner.EventAble.RemoveValue(lifecycleValue));
            Assert.AreEqual(true, needsCommitProperty.GetValue(owner.EventAble));
        }



        [Test]
        public void EventAble_EditorDrawer_DelayedCommitAfterDispose_IsIgnored()
        {
            Type drawerType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("Pan.Event.Editor.EventAbleInspectorDrawer", false))
                .FirstOrDefault(type => type != null);
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

            Assert.IsNotNull(drawerType);

            object drawer = Activator.CreateInstance(drawerType, true);
            FieldInfo observedField = drawerType.GetField("observedEventAble", Flags);
            FieldInfo queuedEventAbleField = drawerType.GetField("queuedCommitEventAble", Flags);
            FieldInfo queuedField = drawerType.GetField("viewCommitQueued", Flags);
            MethodInfo commitMethod = drawerType.GetMethod("Editor_CommitQueuedView", Flags);
            var owner = new EventManagerTestOwner();

            Assert.IsNotNull(observedField);
            Assert.IsNotNull(queuedEventAbleField);
            Assert.IsNotNull(queuedField);
            Assert.IsNotNull(commitMethod);

            observedField.SetValue(drawer, owner.EventAble);
            queuedEventAbleField.SetValue(drawer, owner.EventAble);
            queuedField.SetValue(drawer, true);

            ((IDisposable)drawer).Dispose();

            Assert.DoesNotThrow(() => commitMethod.Invoke(drawer, null));
            Assert.AreEqual(false, queuedField.GetValue(drawer));
            Assert.IsNull(observedField.GetValue(drawer));
            Assert.IsNull(queuedEventAbleField.GetValue(drawer));
        }



        [Test]
        public void EventAbleController_ForceInitialize_DefersReplacementAndResetsOldEventAble()
        {
            PanEventGeneralManager.Initialize();
            var gameObject = new GameObject(nameof(TestEventAbleControllerOwner));

            try
            {
                TestEventAbleControllerOwner owner = gameObject.AddComponent<TestEventAbleControllerOwner>();
                EventAbleController controller = owner.InitializeController(8);
                EventAble oldEventAble = controller.EventAble;
                TestLifecycleValue oldValue = oldEventAble.Require<TestLifecycleValue>();
                const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
                MethodInfo queueMethod = typeof(EventAbleController).GetMethod(
                    "Editor_InitializeEventAble",
                    Flags);
                MethodInfo applyMethod = typeof(EventAbleController).GetMethod(
                    "Editor_ApplyInitializeEventAble",
                    Flags);
                FieldInfo queuedField = typeof(EventAbleController).GetField(
                    "editorInitializeEventAbleQueued",
                    Flags);

                Assert.IsNotNull(queueMethod);
                Assert.IsNotNull(applyMethod);
                Assert.IsNotNull(queuedField);

                var delayedApply = (EditorApplication.CallbackFunction)Delegate.CreateDelegate(
                    typeof(EditorApplication.CallbackFunction),
                    controller,
                    applyMethod);

                queueMethod.Invoke(controller, null);

                try
                {
                    Assert.AreSame(oldEventAble, controller.EventAble);
                    Assert.IsTrue((bool)queuedField.GetValue(controller));
                }
                finally
                {
                    //. 테스트가 직접 적용 콜백을 호출하므로 EditorApplication에 남은 예약을 반드시 제거한다
                    EditorApplication.delayCall -= delayedApply;
                }

                applyMethod.Invoke(controller, null);

                Assert.IsFalse((bool)queuedField.GetValue(controller));
                Assert.AreNotSame(oldEventAble, controller.EventAble);
                Assert.AreSame(controller.EventAble, owner.EventAble);
                Assert.IsFalse(oldValue.Valid_CurrentEventAble);
                Assert.IsNull(oldEventAble.Peek<TestLifecycleValue>());
                Assert.IsNull(controller.EventAble.Peek<TestLifecycleValue>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }



        [Test]
        public void EventAble_EditorDrawer_PreservesNativeListFeaturesAndSafeSchedulerContract()
        {
            Type drawerType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("Pan.Event.Editor.EventAbleInspectorDrawer", false))
                .FirstOrDefault(type => type != null);

            Assert.IsNotNull(drawerType);
            Assert.That(drawerType.BaseType?.Name, Does.StartWith("OdinValueDrawer"));
            FieldInfo idleLifetimeField = drawerType.GetField(
                "DrawerIdleLifetime",
                BindingFlags.Static | BindingFlags.NonPublic);
            FieldInfo throttledIntervalField = drawerType.GetField(
                "ThrottledRefreshInterval",
                BindingFlags.Static | BindingFlags.NonPublic);
            FieldInfo tableField = typeof(EventAble).GetField(
                "EventValueTables",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Type tableType = tableField?.FieldType;
            Type refreshModeType = typeof(EventAble).Assembly.GetType(
                "Pan.Event.EventAbleInspectorRefreshMode",
                false);
            Type contentFilterType = tableType?.GetNestedType(
                "EditorContentFilter",
                BindingFlags.NonPublic);
            Type rowType = tableType?.GetNestedType(
                "PanBaseEventValueStatus",
                BindingFlags.NonPublic);
            MethodInfo meaningfulContentMethod = tableType?.GetMethod(
                "Editor_HasMeaningfulInspectorContent",
                BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo removeMethod = rowType?.GetMethod(
                "Editor_Remove",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo pinMethod = rowType?.GetMethod(
                "Editor_Pin",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo unpinMethod = rowType?.GetMethod(
                "Editor_Unpin",
                BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo typeLabelProperty = rowType?.GetProperty(
                "Editor_TypeLabel",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo detailField = rowType?.GetField(
                "Editor_EventValue",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Type detailDrawerType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("Pan.Event.Editor.EventAbleInspectorDetailDrawer", false))
                .FirstOrDefault(type => type != null);

            Assert.IsNotNull(idleLifetimeField);
            Assert.IsNotNull(throttledIntervalField);
            Assert.IsNotNull(tableType);
            Assert.IsNotNull(refreshModeType);
            Assert.IsNotNull(contentFilterType);
            Assert.IsNotNull(rowType);
            Assert.IsNotNull(meaningfulContentMethod);
            Assert.IsNotNull(removeMethod);
            Assert.IsNotNull(pinMethod);
            Assert.IsNotNull(unpinMethod);
            Assert.IsNotNull(typeLabelProperty);
            Assert.IsNotNull(detailField);
            Assert.IsNull(detailDrawerType);
            Assert.IsTrue(typeof(ISearchFilterable).IsAssignableFrom(rowType));
            Assert.IsTrue(rowType.CustomAttributes.Any(attribute =>
                attribute.AttributeType.Name == "HideLabelAttribute"));
            Assert.AreEqual(typeof(PanBaseEventValue), detailField.FieldType);
            Assert.IsFalse(detailField.IsInitOnly);
            Assert.IsFalse(detailField.CustomAttributes.Any(attribute =>
                attribute.AttributeType.Name == "FoldoutGroupAttribute"));
            Assert.IsTrue(detailField.CustomAttributes.Any(attribute =>
                attribute.AttributeType.Name == "EnableGUIAttribute"));
            var typeLabelDisplay = typeLabelProperty.GetCustomAttribute<DisplayAsStringAttribute>();
            Assert.IsNotNull(typeLabelDisplay);
            Assert.IsTrue(typeLabelDisplay.EnableRichText);
            Assert.AreEqual(1d, idleLifetimeField.GetRawConstantValue());
            Assert.AreEqual(0.1d, throttledIntervalField.GetRawConstantValue());
            Assert.That(Enum.GetNames(refreshModeType), Is.EqualTo(new[]
            {
                "ChangeOnly",
                "GameFrameSync",
                "Throttled10Hz",
                "EditorRealtime"
            }));
            Assert.That(Enum.GetNames(contentFilterType), Is.EqualTo(new[]
            {
                "All",
                "WithDetails",
                "WithoutDetails",
                "PinnedOnly"
            }));
            Assert.AreEqual(true, meaningfulContentMethod.Invoke(null, new object[] { typeof(TestLifecycleValue) }));
            Assert.AreEqual(false, meaningfulContentMethod.Invoke(null, new object[] { typeof(TestRejectingEventValue) }));

            CustomAttributeData removeButton = removeMethod.CustomAttributes.First(attribute =>
                attribute.AttributeType.Name == "ButtonAttribute");
            CustomAttributeData removeColor = removeMethod.CustomAttributes.First(attribute =>
                attribute.AttributeType.Name == "GUIColorAttribute");
            CustomAttributeData pinButton = pinMethod.CustomAttributes.First(attribute =>
                attribute.AttributeType.Name == "ButtonAttribute");
            CustomAttributeData unpinButton = unpinMethod.CustomAttributes.First(attribute =>
                attribute.AttributeType.Name == "ButtonAttribute");

            Assert.AreEqual("해제", removeButton.ConstructorArguments[0].Value);
            Assert.AreEqual(string.Empty, pinButton.ConstructorArguments[0].Value);
            Assert.AreEqual(string.Empty, unpinButton.ConstructorArguments[0].Value);
            Assert.That(removeButton.NamedArguments.Any(argument =>
                argument.MemberName == "Icon" &&
                Convert.ToInt32(argument.TypedValue.Value) == (int)SdfIconType.ExclamationSquareFill));
            Assert.That(pinButton.NamedArguments.Any(argument =>
                argument.MemberName == "Icon" &&
                Convert.ToInt32(argument.TypedValue.Value) == (int)SdfIconType.Star));
            Assert.That(unpinButton.NamedArguments.Any(argument =>
                argument.MemberName == "Icon" &&
                Convert.ToInt32(argument.TypedValue.Value) == (int)SdfIconType.StarFill));
            Assert.That(removeColor.ConstructorArguments.Take(3).Select(argument => (float)argument.Value).ToArray(),
                Is.EqualTo(new[] { 0.93f, 0.33f, 0.40f }));
            foreach (MethodInfo rowActionMethod in new[] { removeMethod, pinMethod, unpinMethod })
            {
                Assert.IsTrue(rowActionMethod.CustomAttributes.Any(attribute =>
                    attribute.AttributeType.Name == "DisableIfAttribute"));
            }
        }



        [Test]
        public void EventAble_EditorEventValueDetail_RemainsEditableThroughOdinPropertyTree()
        {
            FieldInfo tableField = typeof(EventAble).GetField(
                "EventValueTables",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Type rowType = tableField?.FieldType.GetNestedType(
                "PanBaseEventValueStatus",
                BindingFlags.NonPublic);
            ConstructorInfo rowConstructor = rowType?.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { tableField.FieldType, typeof(PanBaseEventValue) },
                null);
            var eventValue = new TestLifecycleValue();
            object row = rowConstructor?.Invoke(new object[] { null, eventValue });

            Assert.IsNotNull(rowType);
            Assert.IsNotNull(rowConstructor);
            Assert.IsNotNull(row);

            using (PropertyTree propertyTree = PropertyTree.Create(row))
            {
                propertyTree.UpdateTree();

                InspectorProperty eventValueProperty = propertyTree.GetPropertyAtPath("Editor_EventValue");
                InspectorProperty mutableFieldProperty = eventValueProperty?.Children[nameof(TestLifecycleValue.EditorMutableValue)];

                Assert.IsNotNull(eventValueProperty);
                Assert.IsTrue(eventValueProperty.ValueEntry.IsEditable);
                Assert.IsNotNull(mutableFieldProperty);
                Assert.IsTrue(mutableFieldProperty.ValueEntry.IsEditable);

                mutableFieldProperty.ValueEntry.WeakSmartValue = 37;
                propertyTree.ApplyChanges();
            }

            Assert.AreEqual(37, eventValue.EditorMutableValue);
        }



        [Test]
        public void EventAble_EditorNativeList_DefersPinAndNormalizesMissingNamespaceFilter()
        {
            PanEventGeneralManager.Initialize();
            var owner = new EventManagerTestOwner();
            TestLifecycleValue eventValue = owner.EventAble.Require<TestLifecycleValue>();
            const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.NonPublic;
            FieldInfo tableField = typeof(EventAble).GetField("EventValueTables", InstanceFlags);
            object table = tableField?.GetValue(owner.EventAble);
            Type tableType = table?.GetType();
            FieldInfo namespaceFilterField = tableType?.GetField("editorNamespaceFilter", InstanceFlags);
            FieldInfo rowsField = tableType?.GetField("EventValueTablesList", InstanceFlags);
            FieldInfo pinRevisionField = tableType?.GetField("editorGlobalPinRevision", StaticFlags);
            FieldInfo pinCacheField = tableType?.GetField("EditorPinCache", StaticFlags);
            MethodInfo refreshMethod = tableType?.GetMethod("Editor_Refresh_EventValueTablesList", InstanceFlags);
            MethodInfo commitMethod = typeof(EventAble).GetMethod("EditorCommitInspectorView", InstanceFlags);
            MethodInfo rowActionsMethod = typeof(EventAble).GetMethod("EditorSetInspectorRowActionsEnabled", InstanceFlags);

            Assert.IsNotNull(namespaceFilterField);
            Assert.IsNotNull(rowsField);
            Assert.IsNotNull(pinRevisionField);
            Assert.IsNotNull(pinCacheField);
            Assert.IsNotNull(refreshMethod);
            Assert.IsNotNull(commitMethod);
            Assert.IsNotNull(rowActionsMethod);

            namespaceFilterField.SetValue(table, "Missing.Namespace");
            refreshMethod.Invoke(table, null);

            Assert.AreEqual("전체 네임스페이스", namespaceFilterField.GetValue(table));

            var rows = (Array)rowsField.GetValue(table);
            Assert.AreEqual(1, rows.Length);

            object row = rows.GetValue(0);
            Type rowType = row.GetType();
            MethodInfo pinMethod = rowType.GetMethod("Editor_Pin", InstanceFlags);
            PropertyInfo actionsDisabledProperty = rowType.GetProperty("EditorRowActionsDisabled", InstanceFlags);
            FieldInfo pinKeyField = rowType.GetField("PinKey", InstanceFlags);
            string pinKey = (string)pinKeyField.GetValue(row);
            string editorPrefsKey = "Pan.EventManager.EventAbleInspector.Pin." + pinKey;
            var pinCache = (IDictionary<string, bool>)pinCacheField.GetValue(null);
            bool prefsExisted = EditorPrefs.HasKey(editorPrefsKey);
            bool previousPrefsValue = EditorPrefs.GetBool(editorPrefsKey, false);
            bool cacheExisted = pinCache.TryGetValue(pinKey, out bool previousCacheValue);
            int previousRevision = (int)pinRevisionField.GetValue(null);

            try
            {
                EditorPrefs.SetBool(editorPrefsKey, false);
                pinCache[pinKey] = false;

                pinMethod.Invoke(row, null);

                Assert.AreEqual(previousRevision, pinRevisionField.GetValue(null));
                Assert.IsFalse(EditorPrefs.GetBool(editorPrefsKey));
                Assert.AreEqual(true, commitMethod.Invoke(owner.EventAble, null));
                Assert.Greater((int)pinRevisionField.GetValue(null), previousRevision);
                Assert.IsTrue(EditorPrefs.GetBool(editorPrefsKey));

                rowActionsMethod.Invoke(owner.EventAble, new object[] { false });
                Assert.AreEqual(true, actionsDisabledProperty.GetValue(row));
            }
            finally
            {
                pinRevisionField.SetValue(null, previousRevision);

                if (prefsExisted) { EditorPrefs.SetBool(editorPrefsKey, previousPrefsValue); }
                else { EditorPrefs.DeleteKey(editorPrefsKey); }

                if (cacheExisted) { pinCache[pinKey] = previousCacheValue; }
                else { pinCache.Remove(pinKey); }

                owner.EventAble.RemoveValue(eventValue);
            }
        }



        [UnityTest]
        public IEnumerator EventAble_EditorDrawer_ForceInitializeAcrossGuiFrames_DoesNotBreakLayout()
        {
            PanEventGeneralManager.Initialize();
            var gameObject = new GameObject(nameof(TestEventAbleControllerOwner));
            EventAbleInspectorHarnessWindow window = null;

            try
            {
                TestEventAbleControllerOwner owner = gameObject.AddComponent<TestEventAbleControllerOwner>();
                EventAbleController controller = owner.InitializeController(8);
                EventAble oldEventAble = controller.EventAble;
                oldEventAble.Require<TestLifecycleValue>();
                oldEventAble.Require<TestReentrantRequireValue>();

                window = ScriptableObject.CreateInstance<EventAbleInspectorHarnessWindow>();
                window.Controller = controller;
                window.position = new Rect(100f, 100f, 500f, 600f);
                window.ShowUtility();

                for (int i = 0; i < 3; i++)
                {
                    window.Repaint();
                    yield return null;
                }

                MethodInfo queueMethod = typeof(EventAbleController).GetMethod(
                    "Editor_InitializeEventAble",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.IsNotNull(queueMethod);
                queueMethod.Invoke(controller, null);

                //? 전체 테스트 묶음에서는 다른 Editor 작업이 delayCall보다 먼저 실행될 수 있으므로 실제 교체 완료까지 기다린다.
                for (int i = 0; i < 60 && ReferenceEquals(oldEventAble, controller.EventAble); i++)
                {
                    window.Repaint();
                    yield return null;
                }

                Assert.AreNotSame(oldEventAble, controller.EventAble);
                Assert.IsNull(oldEventAble.Peek<TestLifecycleValue>());
                Assert.IsNull(oldEventAble.Peek<TestReentrantRequireValue>());
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                if (window != null) { window.Close(); }
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }



    }
}
