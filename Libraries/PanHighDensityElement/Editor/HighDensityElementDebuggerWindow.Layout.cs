using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.U2D.Physics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;



namespace Pan.HighDensityElement.Editor
{
    public sealed partial class HighDensityElementDebuggerWindow
    {
        public void CreateGUI()
        {
            //? Dock 최대화와 layout 복원은 같은 EditorWindow 인스턴스에서 CreateGUI를 다시 호출할 수 있습니다.
            //  이전 panel에 예약된 callback을 먼저 끊어 invalid window를 참조하지 않게 합니다.
            CancelScheduledItems();
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            InitializeTreeIcons();
            hierarchyWidthPreferenceKey = GetHierarchyWidthPreferenceKey();
            hierarchyPaneWidth = EditorPrefs.GetFloat(hierarchyWidthPreferenceKey, hierarchyPaneWidth);

            titleContent = new GUIContent(
                "Element World Explorer",
                EditorGUIUtility.ObjectContent(null, typeof(SceneAsset)).image,
                "GameObject 없이 실행되는 ElementWorld와 Element를 탐색합니다.");

            var toolbar = new VisualElement { name = "element-world-explorer-toolbar" };
            toolbar.style.flexShrink = 0f;
            var primaryToolbar = new Toolbar { name = "element-world-explorer-toolbar-primary" };
            searchField = new ToolbarSearchField();
            searchField.style.flexGrow = 1f;
            searchField.style.minWidth = 80f;
            searchField.RegisterValueChangedCallback(_ =>
            {
                searchDebounceItem?.Pause();
                searchDebounceItem?.ExecuteLater(180);
            });
            primaryToolbar.Add(searchField);

            ToolbarButton expandAllButton = CreateTextButton(
                ExpandAllGroups,
                "펼치기",
                "현재 World와 Lane을 모두 펼칩니다.");
            primaryToolbar.Add(expandAllButton);

            ToolbarButton collapseAllButton = CreateTextButton(
                CollapseAllGroups,
                "접기",
                "현재 World와 Lane을 모두 접습니다.");
            primaryToolbar.Add(collapseAllButton);

            liveToggle = new Toggle("Live") { value = true, tooltip = "주기적인 상태 갱신을 켜거나 멈춥니다." };
            liveToggle.style.flexShrink = 0f;
            primaryToolbar.Add(liveToggle);

            ToolbarButton refreshButton = CreateTextButton(
                () => RefreshWorldsAndItems(force: true),
                "새로고침",
                "구조와 선택 Inspector를 즉시 새로고침합니다.");
            primaryToolbar.Add(refreshButton);
            toolbar.Add(primaryToolbar);

            var secondaryToolbar = new Toolbar { name = "element-world-explorer-toolbar-secondary" };
            drawSceneToggle = new Toggle("Scene 형상") { value = true };
            drawSceneToggle.tooltip = "Scene View에서 bodyless Element 충돌 형상을 표시합니다.";
            drawSceneToggle.RegisterValueChangedCallback(_ => RequestSceneRepaint());
            secondaryToolbar.Add(drawSceneToggle);

            scenePosePopup = new PopupField<string>(ScenePoseNames, 0);
            scenePosePopup.tooltip = "Presentation은 화면 pose, Physics Diagnostic은 고정 스텝 pose와 sweep을 함께 표시합니다.";
            scenePosePopup.style.width = 150f;
            scenePosePopup.RegisterValueChangedCallback(_ => RequestSceneRepaint());
            secondaryToolbar.Add(scenePosePopup);

            summaryLabel = new Label("실행 중인 ElementWorld가 없습니다.");
            summaryLabel.style.flexGrow = 1f;
            summaryLabel.style.minWidth = 60f;
            summaryLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            summaryLabel.style.whiteSpace = WhiteSpace.NoWrap;
            summaryLabel.style.overflow = Overflow.Hidden;
            secondaryToolbar.Add(summaryLabel);
            toolbar.Add(secondaryToolbar);
            rootVisualElement.Add(toolbar);

            splitView = new VisualElement
            {
                name = "element-world-explorer-split",
                viewDataKey = "Pan.HighDensityElement.Explorer.Split"
            };
            splitView.style.flexDirection = FlexDirection.Row;
            splitView.style.flexGrow = 1f;
            hierarchyPane = new VisualElement
            {
                name = "element-world-explorer-hierarchy-pane",
                viewDataKey = "Pan.HighDensityElement.Explorer.HierarchyPane"
            };
            hierarchyPane.style.minWidth = MinimumHierarchyWidth;
            hierarchyPane.style.width = Mathf.Max(MinimumHierarchyWidth, hierarchyPaneWidth);
            hierarchyPane.style.flexShrink = 0f;
            elementTree = new TreeView(20, MakeTreeRow, BindTreeRow)
            {
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                viewDataKey = "Pan.HighDensityElement.Explorer.Tree"
            };
            elementTree.style.flexGrow = 1f;
            elementTree.selectionChanged += OnTreeSelectionChanged;
            elementTree.itemExpandedChanged += OnTreeItemExpandedChanged;
            hierarchyPane.Add(elementTree);

            splitHandle = new VisualElement
            {
                name = "element-world-explorer-split-handle",
                tooltip = "드래그하여 Hierarchy와 Inspector의 폭을 조절합니다."
            };
            splitHandle.style.width = SplitterWidth;
            splitHandle.style.flexShrink = 0f;
            splitHandle.style.backgroundColor = new Color(0f, 0f, 0f, 0.001f);
            var splitLine = new VisualElement();
            splitLine.style.position = Position.Absolute;
            splitLine.style.left = (SplitterWidth - 1f) * 0.5f;
            splitLine.style.top = 0f;
            splitLine.style.bottom = 0f;
            splitLine.style.width = 1f;
            splitLine.style.backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(0.38f, 0.38f, 0.38f, 0.9f)
                : new Color(0.55f, 0.55f, 0.55f, 0.9f);
            splitLine.pickingMode = PickingMode.Ignore;
            splitHandle.Add(splitLine);
            splitHandle.RegisterCallback<PointerDownEvent>(OnSplitPointerDown);
            splitHandle.RegisterCallback<PointerMoveEvent>(OnSplitPointerMove);
            splitHandle.RegisterCallback<PointerUpEvent>(OnSplitPointerUp);
            splitHandle.RegisterCallback<PointerCaptureOutEvent>(OnSplitPointerCaptureOut);
            splitView.Add(hierarchyPane);
            splitView.Add(splitHandle);

            inspectorPane = new VisualElement
            {
                name = "element-world-explorer-inspector-pane",
                viewDataKey = "Pan.HighDensityElement.Explorer.InspectorPane"
            };
            inspectorPane.style.minWidth = MinimumInspectorWidth;
            inspectorPane.style.flexGrow = 1f;
            inspectorPane.style.flexShrink = 1f;
            inspectorScroll = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "element-world-explorer-inspector-scroll",
                viewDataKey = "Pan.HighDensityElement.Explorer.InspectorScroll"
            };
            inspectorScroll.style.flexGrow = 1f;
            inspectorPane.Add(inspectorScroll);
            splitView.Add(inspectorPane);
            rootVisualElement.Add(splitView);

            statusLabel = new Label("대기 중")
            {
                name = "element-world-explorer-status"
            };
            statusLabel.style.flexShrink = 0f;
            statusLabel.style.height = 19f;
            statusLabel.style.paddingLeft = 5f;
            statusLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            statusLabel.style.fontSize = 10f;
            rootVisualElement.Add(statusLabel);
            rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);

            SceneView.duringSceneGui -= OnSceneGui;
            SceneView.duringSceneGui += OnSceneGui;
            ElementDebuggerSelection.Changed -= OnSharedSelectionChanged;
            ElementDebuggerSelection.Changed += OnSharedSelectionChanged;
            ElementDebuggerInspectorExtensionRegistry.Changed -= OnInspectorExtensionsChanged;
            ElementDebuggerInspectorExtensionRegistry.Changed += OnInspectorExtensionsChanged;
            ElementDebuggerRowProviderRegistry.Changed -= OnRowProvidersChanged;
            ElementDebuggerRowProviderRegistry.Changed += OnRowProvidersChanged;
            ElementDebuggerComponentProviderRegistry.Changed -= OnComponentProvidersChanged;
            ElementDebuggerComponentProviderRegistry.Changed += OnComponentProvidersChanged;
            ElementDebuggerWorldMetadataProviderRegistry.Changed -= OnWorldMetadataProvidersChanged;
            ElementDebuggerWorldMetadataProviderRegistry.Changed += OnWorldMetadataProvidersChanged;

            RefreshProviderCaches();
            RefreshWorldsAndItems(force: true);
            searchDebounceItem = rootVisualElement.schedule.Execute(ApplySearchFilter);
            searchDebounceItem.Pause();
            refreshScheduledItem = rootVisualElement.schedule.Execute(() =>
            {
                if (liveToggle?.value ?? false) { RefreshWorldsAndItems(force: false); }
            }).Every(200);
        }


        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGui;
            ElementDebuggerSelection.Changed -= OnSharedSelectionChanged;
            ElementDebuggerInspectorExtensionRegistry.Changed -= OnInspectorExtensionsChanged;
            ElementDebuggerRowProviderRegistry.Changed -= OnRowProvidersChanged;
            ElementDebuggerComponentProviderRegistry.Changed -= OnComponentProvidersChanged;
            ElementDebuggerWorldMetadataProviderRegistry.Changed -= OnWorldMetadataProvidersChanged;
            CancelScheduledItems();
            splitDragging = false;
            splitPointerId = -1;
            ReleaseCaptureLeases();
        }


        private void OnDestroy()
        {
            //? Unity가 최대화용 임시 HostView를 폐기할 때 OnDisable 이후에도 예약 항목이 남지 않도록 방어합니다.
            CancelScheduledItems();
        }


        private void CancelScheduledItems()
        {
            searchDebounceItem?.Pause();
            searchDebounceItem = null;
            refreshScheduledItem?.Pause();
            refreshScheduledItem = null;
            treeExpansionRestoreItem?.Pause();
            treeExpansionRestoreItem = null;
        }


        private void OnSplitPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || splitHandle == null || hierarchyPane == null) { return; }

            splitDragging = true;
            splitPointerId = evt.pointerId;
            splitDragStartX = evt.position.x;
            splitDragStartWidth = hierarchyPane.resolvedStyle.width;
            splitHandle.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }


        private void OnSplitPointerMove(PointerMoveEvent evt)
        {
            if (!splitDragging || evt.pointerId != splitPointerId) { return; }

            ApplyHierarchyWidth(splitDragStartWidth + evt.position.x - splitDragStartX);
            evt.StopPropagation();
        }


        private void OnSplitPointerUp(PointerUpEvent evt)
        {
            if (!splitDragging || evt.pointerId != splitPointerId) { return; }

            ApplyHierarchyWidth(splitDragStartWidth + evt.position.x - splitDragStartX);
            if (splitHandle.HasPointerCapture(evt.pointerId)) { splitHandle.ReleasePointer(evt.pointerId); }
            splitDragging = false;
            splitPointerId = -1;
            StoreHierarchyWidth();
            evt.StopPropagation();
        }


        private void OnSplitPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (!splitDragging || evt.pointerId != splitPointerId) { return; }

            splitDragging = false;
            splitPointerId = -1;
            StoreHierarchyWidth();
        }


        private static ToolbarButton CreateTextButton(
            Action action,
            string text,
            string tooltip)
        {
            var button = new ToolbarButton(action)
            {
                tooltip = tooltip,
                name = text,
                text = text
            };
            button.style.flexShrink = 0f;
            return button;
        }


        private static string GetHierarchyWidthPreferenceKey()
        {
            Hash128 projectHash = Hash128.Compute(Application.dataPath);
            return $"Pan.HighDensityElement.Explorer.HierarchyWidth.{projectHash}";
        }



        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            if (splitView == null || hierarchyPane == null) { return; }

            float availableWidth = Mathf.Max(0f, evt.newRect.width);
            float maximumHierarchyWidth = Mathf.Max(
                MinimumHierarchyWidth,
                Mathf.Min(
                    availableWidth * 0.65f,
                    availableWidth - MinimumInspectorWidth - SplitterWidth));
            hierarchyPane.style.maxWidth = maximumHierarchyWidth;
            ApplyHierarchyWidth(hierarchyPaneWidth);
        }



        private void ApplyHierarchyWidth(float requestedWidth)
        {
            if (hierarchyPane == null || rootVisualElement == null) { return; }

            float availableWidth = Mathf.Max(0f, rootVisualElement.resolvedStyle.width);
            float maximumHierarchyWidth = Mathf.Max(
                MinimumHierarchyWidth,
                Mathf.Min(
                    availableWidth * 0.65f,
                    availableWidth - MinimumInspectorWidth - SplitterWidth));
            float clampedWidth = Mathf.Clamp(requestedWidth, MinimumHierarchyWidth, maximumHierarchyWidth);
            hierarchyPaneWidth = clampedWidth;
            hierarchyPane.style.width = clampedWidth;
        }



        private void StoreHierarchyWidth()
        {
            if (hierarchyPane == null || float.IsNaN(hierarchyPane.resolvedStyle.width)) { return; }
            hierarchyPaneWidth = Mathf.Max(MinimumHierarchyWidth, hierarchyPane.resolvedStyle.width);
            if (!string.IsNullOrWhiteSpace(hierarchyWidthPreferenceKey))
            {
                EditorPrefs.SetFloat(hierarchyWidthPreferenceKey, hierarchyPaneWidth);
            }
            EditorUtility.SetDirty(this);
        }
    }
}
