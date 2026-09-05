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
        private void RefreshWorldsAndItems(bool force)
        {
            if (!force && EditorApplication.timeSinceStartup < nextRefreshTime) { return; }
            nextRefreshTime = EditorApplication.timeSinceStartup + RefreshIntervalSeconds;

            double startedAt = EditorApplication.timeSinceStartup;
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();

            ElementWorldDebugRegistry.CopyWorlds(worlds);
            staleWorldIds.Clear();
            foreach (KeyValuePair<int, WorldSnapshotSet> pair in snapshotSetsByWorldId)
            {
                staleWorldIds.Add(pair.Key);
            }

            int totalAlive = 0;
            for (int i = 0; i < worlds.Count; i++)
            {
                ElementWorld world = worlds[i];
                if (world == null || world.IsDisposed) { continue; }
                staleWorldIds.Remove(world.WorldId);
                if (!snapshotSetsByWorldId.TryGetValue(world.WorldId, out WorldSnapshotSet set) ||
                    !ReferenceEquals(set.World, world))
                {
                    set = new WorldSnapshotSet(world);
                    snapshotSetsByWorldId[world.WorldId] = set;
                    structureDirty = true;
                }

                RefreshWorldMetadata(set);
                if (set.StructuralRevision != world.StructuralRevision)
                {
                    set.StructuralRevision = world.StructuralRevision;
                    using (StructureRefreshMarker.Auto())
                    {
                        world.CopySnapshots(set.Snapshots);
                        set.RefreshLookup();
                        structureEnumerationCount++;
                    }
                    set.RowMetadataDirty = true;
                    structureDirty = true;
                }
                ElementWorldDebugRegistry.CopyExtensionSummaries(world, set.ExtensionSummaries);
                totalAlive += world.AliveCount;
            }

            for (int i = 0; i < staleWorldIds.Count; i++)
            {
                int worldId = staleWorldIds[i];
                snapshotSetsByWorldId.Remove(worldId);
                ReleaseCaptureLease(worldId);
                structureDirty = true;
            }

            ElementDebuggerSelection.Validate();
            ValidateWindowSelection();
            snapshotRefreshSerial++;
            RefreshPendingEditState();
            if (windowSelection.Kind == DebuggerTreeItemKind.Element) { CopySelectedFacts(); }
            UpdateWorldSummary(totalAlive);

            bool rebuildStructure = force || structureDirty;
            if (rebuildStructure)
            {
                RebuildTree();
            }
            else
            {
                elementTree?.RefreshItems();
            }
            if (rebuildStructure)
            {
                inspectorDirty = true;
                RefreshInspector();
            }
            else
            {
                RefreshInspectorValues();
            }
            if (drawSceneToggle?.value ?? false) { RequestSceneRepaint(); }

            lastTotalAlive = totalAlive;
            lastRefreshMilliseconds = (EditorApplication.timeSinceStartup - startedAt) * 1000d;
            lastRefreshAllocatedBytes = Math.Max(0L, GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);
            UpdateStatusBar();
        }


        private void RebuildTree()
        {
            if (elementTree == null) { return; }

            using (StructureRefreshMarker.Auto())
            {

            treeRoots.Clear();
            visibleElementItems.Clear();
            currentGroupTreeIds.Clear();
            string search = searchField?.value?.Trim();
            for (int worldIndex = 0; worldIndex < worlds.Count; worldIndex++)
            {
                ElementWorld world = worlds[worldIndex];
                if (world == null || world.IsDisposed) { continue; }
                if (!snapshotSetsByWorldId.TryGetValue(world.WorldId, out WorldSnapshotSet set)) { continue; }

                RefreshRowMetadata(set);

                var laneChildren = new List<TreeViewItemData<DebuggerTreeItem>>(3);
                AddLaneNode(set, ElementLane.QuerySprite2D, search, laneChildren);
                AddLaneNode(set, ElementLane.AreaSensorSprite2D, search, laneChildren);
                AddLaneNode(set, ElementLane.DynamicBodySprite2D, search, laneChildren);

                var worldItem = new DebuggerTreeItem(
                    DebuggerTreeItemKind.World,
                    world,
                    default,
                    ElementLane.None);
                treeRoots.Add(new TreeViewItemData<DebuggerTreeItem>(
                    GetGroupTreeId($"world:{set.StableOwnerId}"),
                    worldItem,
                    laneChildren));
            }

            synchronizingTreeSelection = true;
            restoringTreeExpansion = true;
            try
            {
                elementTree.SetRootItems(treeRoots);
                elementTree.Rebuild();
                RestoreTreeExpansion();
                SynchronizeTreeSelection();
                structureDirty = false;
                structureRebuildCount++;
            }
            finally
            {
                restoringTreeExpansion = false;
                synchronizingTreeSelection = false;
            }
            }

            treeExpansionRestoreItem?.Pause();
            treeExpansionRestoreItem = rootVisualElement.schedule.Execute(() =>
            {
                if (elementTree == null) { return; }
                restoringTreeExpansion = true;
                try { RestoreTreeExpansion(); }
                finally
                {
                    restoringTreeExpansion = false;
                    treeExpansionRestoreItem = null;
                }
            });
            treeExpansionRestoreItem.ExecuteLater(1);
        }


        private void RestoreTreeExpansion()
        {
            foreach (int treeId in currentGroupTreeIds)
            {
                if (!groupIdentitiesByTreeId.TryGetValue(treeId, out string identity)) { continue; }

                bool expanded = groupExpansionStates.TryGetValue(identity, out bool saved)
                    ? saved
                    : SessionState.GetBool(
                        ExpansionSessionPrefix + identity,
                        identity.StartsWith("world:", StringComparison.Ordinal));
                groupExpansionStates[identity] = expanded;
                if (expanded) { elementTree.ExpandItem(treeId, expandAllChildren: false, refresh: false); }
                else { elementTree.CollapseItem(treeId, collapseAllChildren: false, refresh: false); }
            }

            elementTree.RefreshItems();
        }


        private void UpdateWorldSummary(int totalAlive)
        {
            if (worlds.Count == 0)
            {
                summaryLabel.text = "실행 중인 ElementWorld가 없습니다.";
                return;
            }

            summaryLabel.text = $"World {worlds.Count:N0}  ·  Element {totalAlive:N0}  ·  " +
                ((liveToggle?.value ?? false) ? "Live" : "Paused");
        }



        private void RefreshWorldMetadata(WorldSnapshotSet set)
        {
            if (set == null || (!set.MetadataDirty && !string.IsNullOrWhiteSpace(set.StableOwnerId))) { return; }

            ElementDebuggerWorldMetadata metadata = default;
            bool found = false;
            for (int i = 0; i < worldMetadataProviders.Count; i++)
            {
                try
                {
                    if (!worldMetadataProviders[i].TryGetMetadata(set.World, out metadata)) { continue; }
                    found = true;
                    break;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            if (!found)
            {
                metadata = new ElementDebuggerWorldMetadata(
                    $"world-{set.World.WorldId}",
                    $"World {set.World.WorldId}",
                    null,
                    nameof(ElementWorld),
                    !set.World.IsDisposed,
                    "프로젝트 metadata provider 미등록",
                    "프로젝트 metadata provider 미등록",
                    "프로젝트 metadata provider 미등록");
            }

            set.Metadata = metadata;
            set.StableOwnerId = string.IsNullOrWhiteSpace(metadata.StableOwnerId)
                ? $"world-{set.World.WorldId}"
                : metadata.StableOwnerId;
            set.MetadataDirty = false;
        }
        private void RetainCaptureLease(ElementWorld world)
        {
            if (world == null) { ReleaseCaptureLeases(); return; }
            var releaseIds = new List<int>();
            foreach (int worldId in captureLeases.Keys)
            {
                if (worldId != world.WorldId) { releaseIds.Add(worldId); }
            }
            for (int i = 0; i < releaseIds.Count; i++) { ReleaseCaptureLease(releaseIds[i]); }
            EnsureCaptureLease(world);
        }
        private void UpdateStatusBar()
        {
            if (statusLabel == null) { return; }
            statusLabel.text = $"{((liveToggle?.value ?? false) ? "Live" : "Paused")}  |  " +
                $"갱신 {lastRefreshMilliseconds:0.000} ms  |  GC {lastRefreshAllocatedBytes:N0} B  |  " +
                $"표시 {visibleElementItems.Count:N0}/{lastTotalAlive:N0}  |  Rebuild {structureRebuildCount:N0}";
            statusLabel.tooltip = $"구조 전체 열거 {structureEnumerationCount:N0}회";
        }
    }
}
