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
        private void OnTreeSelectionChanged(IEnumerable<object> selectedItems)
        {
            if (synchronizingTreeSelection) { return; }

            foreach (object selectedItem in selectedItems)
            {
                if (selectedItem is DebuggerTreeItem item)
                {
                    windowSelection = new DebuggerWindowSelection(item.Kind, item.World, item.Lane, item.Key);
                    if (item.Kind == DebuggerTreeItemKind.Element)
                    {
                        ElementDebuggerSelection.TrySet(item.World, item.Key);
                        RetainCaptureLease(item.World);
                    }
                    else
                    {
                        ElementDebuggerSelection.Clear();
                        ReleaseCaptureLeases();
                    }
                    inspectorDirty = true;
                    RefreshInspector();
                    return;
                }
            }

            windowSelection = default;
            ElementDebuggerSelection.Clear();
            ReleaseCaptureLeases();
            inspectorDirty = true;
            RefreshInspector();
        }


        private void SynchronizeTreeSelection()
        {
            if (windowSelection.Kind != DebuggerTreeItemKind.Element)
            {
                int groupId = windowSelection.Kind == DebuggerTreeItemKind.World
                    ? GetExistingGroupTreeId($"world:{GetStableOwnerId(windowSelection.World)}")
                    : windowSelection.Kind == DebuggerTreeItemKind.Lane
                        ? GetExistingGroupTreeId($"lane:{GetStableOwnerId(windowSelection.World)}:{windowSelection.Lane}")
                        : -1;
                if (groupId >= 0) { elementTree.SetSelectionById(groupId); }
                return;
            }

            if (!ElementDebuggerSelection.TryGet(out ElementWorld world, out _, out _) ||
                !visibleElementItems.Contains(new DebuggerElementIdentity(world.WorldId, ElementDebuggerSelection.Key)) ||
                !elementTreeIds.TryGetValue(
                    new DebuggerElementIdentity(world.WorldId, ElementDebuggerSelection.Key),
                    out int treeId))
            {
                elementTree.ClearSelection();
                return;
            }

            elementTree.SetSelectionById(treeId);
            elementTree.ScrollToItemById(treeId);
        }


        private void OnSharedSelectionChanged()
        {
            CopySelectedFacts();
            if (elementTree != null)
            {
                synchronizingTreeSelection = true;
                try
                {
                    SynchronizeTreeSelection();
                }
                finally
                {
                    synchronizingTreeSelection = false;
                }
            }

            if (ElementDebuggerSelection.TryGet(out ElementWorld selectedWorld, out _, out ElementSnapshot selectedSnapshot))
            {
                windowSelection = new DebuggerWindowSelection(
                    DebuggerTreeItemKind.Element,
                    selectedWorld,
                    selectedSnapshot.Lane,
                    selectedSnapshot.Key);
                RetainCaptureLease(selectedWorld);
            }
            inspectorDirty = true;
            RefreshInspector();
            RequestSceneRepaint();
        }


        private void CopySelectedFacts()
        {
            recentFacts.Clear();
            if (ElementDebuggerSelection.TryGet(out ElementWorld world, out _, out _))
            {
                world.CopyRecentFacts(recentFacts);
            }
        }



        private void ValidateWindowSelection()
        {
            if (!windowSelection.IsValid) { windowSelection = default; return; }

            bool worldExists = false;
            for (int i = 0; i < worlds.Count; i++)
            {
                if (!ReferenceEquals(worlds[i], windowSelection.World)) { continue; }
                worldExists = true;
                break;
            }
            if (!worldExists)
            {
                windowSelection = default;
                ReleaseCaptureLeases();
                return;
            }

            if (windowSelection.Kind == DebuggerTreeItemKind.Element &&
                !windowSelection.World.IsAlive(windowSelection.Key))
            {
                windowSelection = default;
                ElementDebuggerSelection.Clear();
                ReleaseCaptureLeases();
            }
        }
    }
}
