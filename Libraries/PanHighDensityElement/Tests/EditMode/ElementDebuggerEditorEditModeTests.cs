using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Pan.HighDensityElement.Editor;
using UnityEditor;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;



namespace Pan.HighDensityElement.Tests
{
    public sealed class ElementDebuggerEditorEditModeTests
    {
        [TearDown]
        public void TearDown()
        {
            ElementDebuggerSelection.Clear();
        }



        [Test]
        public void SharedSelection_RecycledSlotWithDifferentGenerationBecomesStale()
        {
            using var world = new ElementWorld(1);
            ElementCompiledArchetype archetype = CreateArchetype();
            ElementHandle first = world.Spawn(ElementSpawnBuilder.From(archetype)).Handle;
            int changeCount = 0;
            void CountChange() => changeCount++;
            ElementDebuggerSelection.Changed += CountChange;
            try
            {
                Assert.IsTrue(ElementDebuggerSelection.TrySet(world, first.Key));
                Assert.IsTrue(ElementDebuggerSelection.TryGet(out ElementWorld selectedWorld, out _, out _));
                Assert.AreSame(world, selectedWorld);

                Assert.IsTrue(world.TryDespawnImmediately(first.Key));
                ElementHandle replacement = world.Spawn(ElementSpawnBuilder.From(archetype)).Handle;
                Assert.AreEqual(first.Key.Slot, replacement.Key.Slot);
                Assert.AreNotEqual(first.Key.Generation, replacement.Key.Generation);

                Assert.IsFalse(ElementDebuggerSelection.Validate());
                Assert.IsFalse(ElementDebuggerSelection.Key.IsValid);
                Assert.AreEqual(2, changeCount);
                Assert.IsFalse(ElementDebuggerSelection.TrySet(world, first.Key));
                Assert.IsTrue(ElementDebuggerSelection.TrySet(world, replacement.Key));
            }
            finally
            {
                ElementDebuggerSelection.Changed -= CountChange;
            }
        }



        [Test]
        public void InspectorExtensionRegistry_SortsByOrderThenDisplayNameAndIgnoresDuplicates()
        {
            var late = new TestInspectorExtension("Late", 20);
            var beta = new TestInspectorExtension("Beta", 10);
            var alpha = new TestInspectorExtension("Alpha", 10);
            var extensions = new List<IElementDebuggerInspectorExtension>();
            var baseline = new List<IElementDebuggerInspectorExtension>();
            try
            {
                ElementDebuggerInspectorExtensionRegistry.CopyExtensions(baseline);
                ElementDebuggerInspectorExtensionRegistry.Register(late);
                ElementDebuggerInspectorExtensionRegistry.Register(beta);
                ElementDebuggerInspectorExtensionRegistry.Register(alpha);
                ElementDebuggerInspectorExtensionRegistry.Register(alpha);

                ElementDebuggerInspectorExtensionRegistry.CopyExtensions(extensions);

                Assert.AreEqual(baseline.Count + 3, extensions.Count);
                Assert.AreEqual(1, extensions.FindAll(extension => ReferenceEquals(extension, alpha)).Count);
                Assert.Less(extensions.IndexOf(alpha), extensions.IndexOf(beta));
                Assert.Less(extensions.IndexOf(beta), extensions.IndexOf(late));
            }
            finally
            {
                ElementDebuggerInspectorExtensionRegistry.Unregister(alpha);
                ElementDebuggerInspectorExtensionRegistry.Unregister(beta);
                ElementDebuggerInspectorExtensionRegistry.Unregister(late);
            }
        }



        [Test]
        public void RowProviderRegistry_SortsDeterministicallyAndIgnoresDuplicateInstance()
        {
            var alpha = new TestAlphaRowProvider();
            var beta = new TestBetaRowProvider();
            var late = new TestLateRowProvider();
            var providers = new List<IElementDebuggerRowProvider>();
            try
            {
                ElementDebuggerRowProviderRegistry.Register(late);
                ElementDebuggerRowProviderRegistry.Register(beta);
                ElementDebuggerRowProviderRegistry.Register(alpha);
                ElementDebuggerRowProviderRegistry.Register(alpha);

                ElementDebuggerRowProviderRegistry.CopyProviders(providers);

                Assert.AreEqual(1, providers.FindAll(provider => ReferenceEquals(provider, alpha)).Count);
                Assert.Less(providers.IndexOf(alpha), providers.IndexOf(beta));
                Assert.Less(providers.IndexOf(beta), providers.IndexOf(late));
            }
            finally
            {
                ElementDebuggerRowProviderRegistry.Unregister(alpha);
                ElementDebuggerRowProviderRegistry.Unregister(beta);
                ElementDebuggerRowProviderRegistry.Unregister(late);
            }
        }



        [Test]
        public void ComponentProviderRegistry_PreservesDuplicateIdsForConflictDetection()
        {
            var alpha = new TestAlphaComponentProvider();
            var beta = new TestBetaComponentProvider();
            var providers = new List<IElementDebuggerComponentProvider>();
            try
            {
                ElementDebuggerComponentProviderRegistry.Register(beta);
                ElementDebuggerComponentProviderRegistry.Register(alpha);
                ElementDebuggerComponentProviderRegistry.Register(alpha);

                ElementDebuggerComponentProviderRegistry.CopyProviders(providers);

                Assert.AreEqual(1, providers.FindAll(provider => ReferenceEquals(provider, alpha)).Count);
                Assert.Less(providers.IndexOf(alpha), providers.IndexOf(beta));
                Assert.AreEqual(alpha.ComponentId, beta.ComponentId);
            }
            finally
            {
                ElementDebuggerComponentProviderRegistry.Unregister(alpha);
                ElementDebuggerComponentProviderRegistry.Unregister(beta);
            }
        }



        [Test]
        public void LaneExpansion_RemainsExpandedAfterLaneDisappearsAndReturns()
        {
            using var world = new ElementWorld(1);
            ElementCompiledArchetype archetype = CreateArchetype(ElementCapabilities.KinematicMotion2D);
            ElementHandle first = world.Spawn(ElementSpawnBuilder.From(archetype)).Handle;
            var window = ScriptableObject.CreateInstance<HighDensityElementDebuggerWindow>();
            var metadataProvider = new TestWorldMetadataProvider("test-owner");
            try
            {
                ElementDebuggerWorldMetadataProviderRegistry.Register(metadataProvider);
                window.CreateGUI();
                InvokeWindowRefresh(window);
                TreeView tree = window.rootVisualElement.Q<TreeView>();
                Assert.IsNotNull(tree);

                Dictionary<string, int> groupIds = GetGroupTreeIds(window);
                string laneIdentity = $"lane:test-owner:{ElementLane.QuerySprite2D}";
                Assert.IsTrue(groupIds.TryGetValue(laneIdentity, out int laneTreeId));
                tree.ExpandItem(laneTreeId);
                Assert.IsTrue(tree.IsExpanded(laneTreeId));

                Assert.IsTrue(world.TryDespawnImmediately(first.Key));
                InvokeWindowRefresh(window);

                world.Spawn(ElementSpawnBuilder.From(archetype));
                InvokeWindowRefresh(window);

                Assert.IsTrue(groupIds.TryGetValue(laneIdentity, out int restoredLaneTreeId));
                Assert.AreEqual(laneTreeId, restoredLaneTreeId);
                Assert.IsTrue(tree.IsExpanded(restoredLaneTreeId));
            }
            finally
            {
                ElementDebuggerWorldMetadataProviderRegistry.Unregister(metadataProvider);
                UnityEngine.Object.DestroyImmediate(window);
            }
        }



        [Test]
        public void Explorer_UsesTwoToolbarRowsAndDedicatedSplitterSibling()
        {
            var window = ScriptableObject.CreateInstance<HighDensityElementDebuggerWindow>();
            try
            {
                window.CreateGUI();

                Assert.IsNotNull(window.rootVisualElement.Q<VisualElement>(
                    "element-world-explorer-toolbar-primary"));
                Assert.IsNotNull(window.rootVisualElement.Q<VisualElement>(
                    "element-world-explorer-toolbar-secondary"));
                VisualElement split = window.rootVisualElement.Q<VisualElement>(
                    "element-world-explorer-split");
                Assert.IsNotNull(split);
                Assert.AreEqual(3, split.childCount);
                Assert.IsNotNull(window.rootVisualElement.Q<VisualElement>(
                    "element-world-explorer-split-handle"));
                Assert.IsFalse(string.IsNullOrWhiteSpace(split.viewDataKey));
                Assert.IsFalse(string.IsNullOrWhiteSpace(window.rootVisualElement.Q<TreeView>().viewDataKey));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }



        [UnityTest]
        public IEnumerator Explorer_ResolvedLayoutKeepsToolbarsAndSplitPanesAligned()
        {
            var window = ScriptableObject.CreateInstance<HighDensityElementDebuggerWindow>();
            try
            {
                window.CreateGUI();
                window.position = new Rect(60f, 60f, 520f, 640f);
                window.Show();
                yield return null;

                VisualElement primary = window.rootVisualElement.Q<VisualElement>(
                    "element-world-explorer-toolbar-primary");
                VisualElement secondary = window.rootVisualElement.Q<VisualElement>(
                    "element-world-explorer-toolbar-secondary");
                VisualElement split = window.rootVisualElement.Q<VisualElement>(
                    "element-world-explorer-split");
                VisualElement hierarchy = window.rootVisualElement.Q<VisualElement>(
                    "element-world-explorer-hierarchy-pane");
                VisualElement inspector = window.rootVisualElement.Q<VisualElement>(
                    "element-world-explorer-inspector-pane");
                VisualElement handle = window.rootVisualElement.Q<VisualElement>(
                    "element-world-explorer-split-handle");

                Assert.IsNotNull(primary);
                Assert.IsNotNull(secondary);
                Assert.IsNotNull(split);
                Assert.IsNotNull(hierarchy);
                Assert.IsNotNull(inspector);
                Assert.IsNotNull(handle);
                Assert.LessOrEqual(primary.worldBound.yMax, secondary.worldBound.yMin + 0.5f);
                Assert.GreaterOrEqual(hierarchy.resolvedStyle.width, 169.5f);
                Assert.GreaterOrEqual(inspector.resolvedStyle.width, 279.5f);
                Assert.AreEqual(
                    handle.worldBound.xMax,
                    inspector.worldBound.xMin,
                    3f,
                    "splitter와 실제 pane 경계가 분리되었습니다.");
                Assert.AreEqual(hierarchy.worldBound.xMax, handle.worldBound.xMin, 3f);

                InvokeHierarchyWidth(window, 230f);
                yield return null;
                Assert.AreEqual(230f, hierarchy.resolvedStyle.width, 3f);
                Assert.AreEqual(hierarchy.worldBound.xMax, handle.worldBound.xMin, 3f);
                Assert.AreEqual(handle.worldBound.xMax, inspector.worldBound.xMin, 3f);

                window.position = new Rect(60f, 60f, 718f, 640f);
                yield return null;
                Assert.GreaterOrEqual(inspector.resolvedStyle.width, 279.5f);
                Assert.AreEqual(handle.worldBound.xMax, inspector.worldBound.xMin, 3f);

                window.position = new Rect(60f, 60f, 1200f, 700f);
                InvokeHierarchyWidth(window, 360f);
                yield return null;
                Assert.AreEqual(360f, hierarchy.resolvedStyle.width, 3f);
                Assert.AreEqual(hierarchy.worldBound.xMax, handle.worldBound.xMin, 3f);
                Assert.AreEqual(handle.worldBound.xMax, inspector.worldBound.xMin, 3f);
            }
            finally
            {
                window.Close();
                UnityEngine.Object.DestroyImmediate(window);
            }
        }



        [UnityTest]
        public IEnumerator Explorer_SplitterPointerDragResizesHierarchyAndPersistsWidth()
        {
            var window = ScriptableObject.CreateInstance<HighDensityElementDebuggerWindow>();
            try
            {
                window.CreateGUI();
                window.position = new Rect(70f, 70f, 900f, 640f);
                window.Show();
                yield return null;

                InvokeHierarchyWidth(window, 300f);
                yield return null;

                VisualElement hierarchy = window.rootVisualElement.Q<VisualElement>(
                    "element-world-explorer-hierarchy-pane");
                VisualElement handle = window.rootVisualElement.Q<VisualElement>(
                    "element-world-explorer-split-handle");
                Assert.IsNotNull(hierarchy);
                Assert.IsNotNull(handle);

                float startWidth = hierarchy.resolvedStyle.width;
                Vector2 start = handle.worldBound.center;
                SendPointerEvent(handle, EventType.MouseDown, start, 0);
                SendPointerEvent(handle, EventType.MouseMove, start + new Vector2(90f, 0f), 0);
                SendPointerEvent(handle, EventType.MouseUp, start + new Vector2(90f, 0f), 0);
                yield return null;

                Assert.AreEqual(startWidth + 90f, hierarchy.resolvedStyle.width, 0.01f);
            }
            finally
            {
                window.Close();
                UnityEngine.Object.DestroyImmediate(window);
            }
        }



        [Test]
        public void Explorer_WorldLaneAndElementSelectionBuildDifferentInspectors()
        {
            using var world = new ElementWorld(1);
            ElementHandle handle = world.Spawn(
                ElementSpawnBuilder.From(CreateArchetype(ElementCapabilities.KinematicMotion2D)))
                .Handle;
            var provider = new TestWorldMetadataProvider("selection-owner");
            var window = ScriptableObject.CreateInstance<HighDensityElementDebuggerWindow>();
            try
            {
                ElementDebuggerWorldMetadataProviderRegistry.Register(provider);
                window.CreateGUI();
                InvokeWindowRefresh(window);
                TreeView tree = window.rootVisualElement.Q<TreeView>();
                Dictionary<string, int> groupIds = GetGroupTreeIds(window);

                tree.SetSelectionById(groupIds["world:selection-owner"]);
                Assert.IsTrue(ContainsLabel(window.rootVisualElement, "관리 주체와 갱신 경로"));

                tree.SetSelectionById(groupIds[$"lane:selection-owner:{ElementLane.QuerySprite2D}"]);
                Assert.IsTrue(ContainsLabel(window.rootVisualElement, "Native 저장소"));

                Dictionary<object, int> elementIds = GetElementTreeIds(window);
                Assert.IsTrue(TryFindElementTreeId(elementIds, handle.Key, out int elementTreeId));
                tree.SetSelectionById(elementTreeId);
                Assert.IsTrue(ContainsLabel(window.rootVisualElement, "Transform 2D"));
            }
            finally
            {
                ElementDebuggerWorldMetadataProviderRegistry.Unregister(provider);
                UnityEngine.Object.DestroyImmediate(window);
            }
        }



        [Test]
        public void Explorer_SteadyRefreshDoesNotReenumerateOrRebuildStructure()
        {
            const int elementCount = 10_000;
            using var world = new ElementWorld(elementCount);
            ElementCompiledArchetype archetype = CreateArchetype(ElementCapabilities.KinematicMotion2D);
            for (int i = 0; i < elementCount; i++)
            {
                world.Spawn(ElementSpawnBuilder.From(archetype));
            }

            var window = ScriptableObject.CreateInstance<HighDensityElementDebuggerWindow>();
            try
            {
                window.CreateGUI();
                InvokeWindowRefresh(window);
                int enumerationBaseline = GetPrivateInt(window, "structureEnumerationCount");
                int rebuildBaseline = GetPrivateInt(window, "structureRebuildCount");

                for (int i = 0; i < 100; i++)
                {
                    SetPrivateDouble(window, "nextRefreshTime", 0d);
                    InvokeWindowRefresh(window, force: false);
                }

                Assert.AreEqual(enumerationBaseline, GetPrivateInt(window, "structureEnumerationCount"));
                Assert.AreEqual(rebuildBaseline, GetPrivateInt(window, "structureRebuildCount"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }



        [Test]
        public void Explorer_LaneExpansionSurvivesWorldRecreationWithStableOwnerId()
        {
            var provider = new TestWorldMetadataProvider("recreated-owner");
            var window = ScriptableObject.CreateInstance<HighDensityElementDebuggerWindow>();
            ElementWorld firstWorld = null;
            ElementWorld secondWorld = null;
            try
            {
                ElementDebuggerWorldMetadataProviderRegistry.Register(provider);
                ElementCompiledArchetype archetype = CreateArchetype(ElementCapabilities.KinematicMotion2D);
                firstWorld = new ElementWorld(1);
                firstWorld.Spawn(ElementSpawnBuilder.From(archetype));
                window.CreateGUI();
                InvokeWindowRefresh(window);

                TreeView tree = window.rootVisualElement.Q<TreeView>();
                Dictionary<string, int> groupIds = GetGroupTreeIds(window);
                string identity = $"lane:recreated-owner:{ElementLane.QuerySprite2D}";
                Assert.IsTrue(groupIds.TryGetValue(identity, out int laneId));
                tree.ExpandItem(laneId);
                Assert.IsTrue(tree.IsExpanded(laneId));

                firstWorld.Dispose();
                firstWorld = null;
                secondWorld = new ElementWorld(1);
                secondWorld.Spawn(ElementSpawnBuilder.From(archetype));
                InvokeWindowRefresh(window);

                Assert.IsTrue(groupIds.TryGetValue(identity, out int recreatedLaneId));
                Assert.AreEqual(laneId, recreatedLaneId);
                Assert.IsTrue(tree.IsExpanded(recreatedLaneId));
            }
            finally
            {
                firstWorld?.Dispose();
                secondWorld?.Dispose();
                ElementDebuggerWorldMetadataProviderRegistry.Unregister(provider);
                UnityEngine.Object.DestroyImmediate(window);
            }
        }



        [UnityTest]
        public IEnumerator Explorer_PositionFieldSubmitsTeleportAtNextCommandBoundary()
        {
            using var world = new ElementWorld(1);
            ElementHandle handle = world.Spawn(
                ElementSpawnBuilder.From(CreateArchetype(ElementCapabilities.KinematicMotion2D)))
                .Handle;
            var provider = new TestWorldMetadataProvider("edit-owner");
            var window = ScriptableObject.CreateInstance<HighDensityElementDebuggerWindow>();
            try
            {
                ElementDebuggerWorldMetadataProviderRegistry.Register(provider);
                window.CreateGUI();
                window.position = new Rect(80f, 80f, 800f, 640f);
                window.Show();
                yield return null;
                InvokeWindowRefresh(window);
                TreeView tree = window.rootVisualElement.Q<TreeView>();
                Dictionary<object, int> elementIds = GetElementTreeIds(window);
                Assert.IsTrue(TryFindElementTreeId(elementIds, handle.Key, out int elementTreeId));
                tree.SetSelectionById(elementTreeId);

                Vector2Field positionField = FindVector2Field(window.rootVisualElement, "Position");
                Assert.IsNotNull(positionField);
                FloatField positionComponent = positionField.Q<FloatField>();
                Assert.IsNotNull(positionComponent);
                window.Focus();
                positionComponent.Focus();
                yield return null;
                Assert.AreSame(
                    positionField,
                    positionField.panel.focusController.focusedElement,
                    "?뚯뒪???꾨뱶媛 ?쇰떒 ?ㅼ젣 ?낅젰 focus瑜??삉???덈뒗吏 ?뺤씤?⑸땲??");
                for (int refreshIndex = 0; refreshIndex < 10; refreshIndex++)
                {
                    InvokeInspectorValueRefresh(window);
                }
                Assert.AreSame(positionField, FindVector2Field(window.rootVisualElement, "Position"));
                Assert.AreSame(positionField, positionField.panel.focusController.focusedElement);
                positionField.value = new Vector2(12f, -7f);
                Assert.IsTrue(handle.TryGetSnapshot(out ElementSnapshot before));
                Assert.AreNotEqual(new float2(12f, -7f), before.Position);

                world.Tick(0f);
                Assert.IsTrue(handle.TryGetSnapshot(out ElementSnapshot after));
                Assert.AreEqual(new float2(12f, -7f), after.Position);
                Assert.AreEqual(after.Position, after.PreviousPosition);

                Foldout identity = FindFoldout(window.rootVisualElement, "Identity");
                Assert.IsNotNull(identity);
                Assert.IsFalse(string.IsNullOrWhiteSpace(identity.tooltip));
                Assert.IsNotNull(FindFoldout(window.rootVisualElement, "용어 설명"));
            }
            finally
            {
                ElementDebuggerWorldMetadataProviderRegistry.Unregister(provider);
                window.Close();
                UnityEngine.Object.DestroyImmediate(window);
            }
        }



        [Test]
        public void Explorer_DisableCancelsEveryScheduledCallback()
        {
            var window = ScriptableObject.CreateInstance<HighDensityElementDebuggerWindow>();
            try
            {
                window.CreateGUI();
                MethodInfo disable = typeof(HighDensityElementDebuggerWindow).GetMethod(
                    "OnDisable",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(disable);

                disable.Invoke(window, null);

                Assert.IsNull(GetPrivateField(window, "searchDebounceItem"));
                Assert.IsNull(GetPrivateField(window, "refreshScheduledItem"));
                Assert.IsNull(GetPrivateField(window, "treeExpansionRestoreItem"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }



        private static ElementCompiledArchetype CreateArchetype(
            ElementCapabilities capabilities = ElementCapabilities.None)
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



        private static object GetPrivateField(
            HighDensityElementDebuggerWindow window,
            string fieldName)
        {
            FieldInfo field = typeof(HighDensityElementDebuggerWindow).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"{fieldName} 필드를 찾지 못했습니다.");
            return field.GetValue(window);
        }



        [UnityTest]
        public IEnumerator Explorer_FrameMovesPivotWithoutChangingSceneViewZoomOrOrientation()
        {
            using var world = new ElementWorld(1);
            ElementHandle handle = world.Spawn(
                ElementSpawnBuilder.From(CreateArchetype(ElementCapabilities.KinematicMotion2D))
                    .WithPose(new float2(14f, -9f), new float2(1f, 1f)))
                .Handle;
            var sceneView = ScriptableObject.CreateInstance<SceneView>();
            try
            {
                sceneView.Show();
                yield return null;
                Quaternion rotation = Quaternion.Euler(17f, 28f, 3f);
                const float sceneSize = 23f;
                sceneView.LookAt(Vector3.zero, rotation, sceneSize, true, true);
                Quaternion preservedRotation = sceneView.rotation;
                float preservedSize = sceneView.size;
                bool preservedOrthographic = sceneView.orthographic;
                Assert.IsTrue(ElementDebuggerSelection.TrySet(world, handle.Key));

                MethodInfo method = typeof(HighDensityElementDebuggerWindow).GetMethod(
                    "FrameSelected",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.IsNotNull(method);
                Assert.IsTrue((bool)method.Invoke(null, new object[] { sceneView, 1f, true }));

                Assert.AreEqual(preservedSize, sceneView.size, 0.0001f);
                Assert.AreEqual(preservedRotation, sceneView.rotation);
                Assert.AreEqual(preservedOrthographic, sceneView.orthographic);
                Assert.AreEqual(new Vector3(14f, -9f, 0f), sceneView.pivot);
            }
            finally
            {
                sceneView.Close();
                UnityEngine.Object.DestroyImmediate(sceneView);
            }
        }



        private static void SendPointerEvent(
            VisualElement target,
            EventType eventType,
            Vector2 position,
            int button)
        {
            var systemEvent = new Event
            {
                type = eventType,
                mousePosition = position,
                button = button
            };
            using EventBase pointerEvent = eventType switch
            {
                EventType.MouseDown => PointerDownEvent.GetPooled(systemEvent),
                EventType.MouseUp => PointerUpEvent.GetPooled(systemEvent),
                _ => PointerMoveEvent.GetPooled(systemEvent)
            };
            target.SendEvent(pointerEvent);
        }



        private static void InvokeWindowRefresh(HighDensityElementDebuggerWindow window, bool force = true)
        {
            MethodInfo method = typeof(HighDensityElementDebuggerWindow).GetMethod(
                "RefreshWorldsAndItems",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            method.Invoke(window, new object[] { force });
        }



        private static void InvokeInspectorValueRefresh(HighDensityElementDebuggerWindow window)
        {
            MethodInfo method = typeof(HighDensityElementDebuggerWindow).GetMethod(
                "RefreshInspectorValues",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            method.Invoke(window, Array.Empty<object>());
        }



        private static void InvokeHierarchyWidth(
            HighDensityElementDebuggerWindow window,
            float width)
        {
            MethodInfo method = typeof(HighDensityElementDebuggerWindow).GetMethod(
                "ApplyHierarchyWidth",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            method.Invoke(window, new object[] { width });
        }



        private static Dictionary<string, int> GetGroupTreeIds(
            HighDensityElementDebuggerWindow window)
        {
            FieldInfo field = typeof(HighDensityElementDebuggerWindow).GetField(
                "groupTreeIds",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            return (Dictionary<string, int>)field.GetValue(window);
        }



        private static Dictionary<object, int> GetElementTreeIds(HighDensityElementDebuggerWindow window)
        {
            FieldInfo field = typeof(HighDensityElementDebuggerWindow).GetField(
                "elementTreeIds",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            object raw = field.GetValue(window);
            var result = new Dictionary<object, int>();
            foreach (object entry in (System.Collections.IEnumerable)raw)
            {
                Type entryType = entry.GetType();
                result[entryType.GetProperty("Key")?.GetValue(entry)] =
                    (int)entryType.GetProperty("Value")?.GetValue(entry);
            }
            return result;
        }



        private static bool TryFindElementTreeId(
            Dictionary<object, int> entries,
            ElementKey key,
            out int treeId)
        {
            foreach (KeyValuePair<object, int> pair in entries)
            {
                PropertyInfo keyProperty = pair.Key?.GetType().GetProperty("Key");
                if (keyProperty?.GetValue(pair.Key) is ElementKey candidate && candidate == key)
                {
                    treeId = pair.Value;
                    return true;
                }
            }
            treeId = -1;
            return false;
        }



        private static bool ContainsLabel(VisualElement root, string text)
        {
            if (root is Label label && label.text == text) { return true; }
            for (int i = 0; i < root.hierarchy.childCount; i++)
            {
                if (ContainsLabel(root.hierarchy[i], text)) { return true; }
            }
            return false;
        }



        private static Vector2Field FindVector2Field(VisualElement root, string label)
        {
            if (root is Vector2Field field && field.label == label) { return field; }
            for (int i = 0; i < root.hierarchy.childCount; i++)
            {
                Vector2Field candidate = FindVector2Field(root.hierarchy[i], label);
                if (candidate != null) { return candidate; }
            }
            return null;
        }



        private static Foldout FindFoldout(VisualElement root, string text)
        {
            if (root is Foldout foldout && foldout.text == text) { return foldout; }
            for (int i = 0; i < root.hierarchy.childCount; i++)
            {
                Foldout candidate = FindFoldout(root.hierarchy[i], text);
                if (candidate != null) { return candidate; }
            }
            return null;
        }



        private static int GetPrivateInt(HighDensityElementDebuggerWindow window, string fieldName)
        {
            FieldInfo field = typeof(HighDensityElementDebuggerWindow).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            return (int)field.GetValue(window);
        }



        private static void SetPrivateDouble(
            HighDensityElementDebuggerWindow window,
            string fieldName,
            double value)
        {
            FieldInfo field = typeof(HighDensityElementDebuggerWindow).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            field.SetValue(window, value);
        }



        private sealed class TestInspectorExtension : IElementDebuggerInspectorExtension
        {
            public TestInspectorExtension(string displayName, int order)
            {
                DisplayName = displayName;
                Order = order;
            }



            public string DisplayName { get; }
            public int Order { get; }
            public bool IsVisible(in ElementDebuggerInspectorContext context) => true;
            public void OnInspectorGUI(in ElementDebuggerInspectorContext context) { }
        }



        private sealed class TestAlphaRowProvider : IElementDebuggerRowProvider
        {
            public int Order => 1000;
            public bool TryGetMetadata(
                in ElementDebuggerRowContext context,
                out ElementDebuggerRowMetadata metadata)
            {
                metadata = default;
                return false;
            }
        }



        private sealed class TestBetaRowProvider : IElementDebuggerRowProvider
        {
            public int Order => 1000;
            public bool TryGetMetadata(
                in ElementDebuggerRowContext context,
                out ElementDebuggerRowMetadata metadata)
            {
                metadata = default;
                return false;
            }
        }



        private sealed class TestLateRowProvider : IElementDebuggerRowProvider
        {
            public int Order => 1001;
            public bool TryGetMetadata(
                in ElementDebuggerRowContext context,
                out ElementDebuggerRowMetadata metadata)
            {
                metadata = default;
                return false;
            }
        }



        private sealed class TestWorldMetadataProvider : IElementDebuggerWorldMetadataProvider
        {
            private readonly string stableOwnerId;

            public TestWorldMetadataProvider(string stableOwnerId)
            {
                this.stableOwnerId = stableOwnerId;
            }

            public int Order => -1000;

            public bool TryGetMetadata(ElementWorld world, out ElementDebuggerWorldMetadata metadata)
            {
                metadata = new ElementDebuggerWorldMetadata(
                    stableOwnerId,
                    "Test World",
                    null,
                    "Test Runtime",
                    true,
                    "Test Update",
                    "Test Fixed",
                    "Test Presentation");
                return true;
            }
        }



        private abstract class TestComponentProvider : IElementDebuggerComponentProvider
        {
            public string ComponentId => "panevent.eventable";
            public int Order => 1000;

            public bool TryGetDescriptor(
                in ElementDebuggerInspectorContext context,
                out ElementDebuggerComponentDescriptor descriptor)
            {
                descriptor = new ElementDebuggerComponentDescriptor(
                    ComponentId,
                    GetType().Name,
                    ElementDebuggerComponentStatus.Allocated);
                return true;
            }

            public void OnInspectorGUI(in ElementDebuggerInspectorContext context) { }
        }



        private sealed class TestAlphaComponentProvider : TestComponentProvider { }
        private sealed class TestBetaComponentProvider : TestComponentProvider { }
    }
}
