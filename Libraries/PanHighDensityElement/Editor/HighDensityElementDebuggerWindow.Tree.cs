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
        private VisualElement MakeTreeRow()
        {
            return new DebuggerTreeRow();
        }


        private void BindTreeRow(VisualElement element, int index)
        {
            using (VisibleRowsMarker.Auto())
            {
            DebuggerTreeItem item = elementTree.GetItemDataForIndex<DebuggerTreeItem>(index);
            var row = (DebuggerTreeRow)element;
            Label label = row.Label;
            row.Icon.image = GetTreeIcon(in item);
            label.tooltip = string.Empty;
            switch (item.Kind)
            {
                case DebuggerTreeItemKind.World:
                    if (item.World != null && !item.World.IsDisposed &&
                        snapshotSetsByWorldId.TryGetValue(item.World.WorldId, out WorldSnapshotSet worldSet))
                    {
                        string worldName = string.IsNullOrWhiteSpace(worldSet.Metadata.DisplayName)
                            ? $"World {item.World.WorldId}"
                            : worldSet.Metadata.DisplayName;
                        label.text = $"{worldName} ({item.World.AliveCount:N0})";
                        label.tooltip = string.IsNullOrWhiteSpace(worldSet.Metadata.RuntimeTypeName)
                            ? $"ElementWorld {item.World.WorldId}"
                            : worldSet.Metadata.RuntimeTypeName;
                    }
                    else
                    {
                        label.text = $"World {item.World.WorldId}";
                    }
                    break;
                case DebuggerTreeItemKind.Lane:
                    label.text = $"{GetLaneDisplayName(item.Lane)} ({GetLaneCount(item.World, item.Lane):N0})";
                    label.tooltip = GetLaneDescription(item.Lane);
                    break;
                case DebuggerTreeItemKind.Element:
                    BindElementTreeRow(label, in item);
                    break;
                default:
                    label.text = item.Kind.ToString();
                    break;
            }
            label.style.unityFontStyleAndWeight = item.Kind == DebuggerTreeItemKind.Element
                ? FontStyle.Normal
                : FontStyle.Bold;
            }
        }


        private void InitializeTreeIcons()
        {
            worldTreeIcon = EditorGUIUtility.ObjectContent(null, typeof(SceneAsset)).image;
            physicsTreeIcon = EditorGUIUtility.ObjectContent(null, typeof(Collider2D)).image;
            dynamicBodyTreeIcon = EditorGUIUtility.ObjectContent(null, typeof(Rigidbody2D)).image;
            queryElementTreeIcon = EditorGUIUtility.ObjectContent(null, typeof(CircleCollider2D)).image;
        }


        private Texture GetTreeIcon(in DebuggerTreeItem item)
        {
            if (item.Kind == DebuggerTreeItemKind.Element &&
                snapshotSetsByWorldId.TryGetValue(item.World.WorldId, out WorldSnapshotSet set) &&
                set.RowMetadataByKey.TryGetValue(item.Key, out ElementDebuggerRowMetadata metadata) &&
                metadata.Icon != null)
            {
                return metadata.Icon;
            }

            return item.Kind switch
            {
                DebuggerTreeItemKind.World => worldTreeIcon,
                DebuggerTreeItemKind.Lane when item.Lane == ElementLane.DynamicBodySprite2D =>
                    dynamicBodyTreeIcon,
                DebuggerTreeItemKind.Lane => physicsTreeIcon,
                DebuggerTreeItemKind.Element when item.Lane == ElementLane.DynamicBodySprite2D =>
                    dynamicBodyTreeIcon,
                DebuggerTreeItemKind.Element => queryElementTreeIcon,
                _ => null
            };
        }


        private void OnTreeItemExpandedChanged(TreeViewExpansionChangedArgs args)
        {
            if (restoringTreeExpansion || args.id < 0 ||
                !groupIdentitiesByTreeId.TryGetValue(args.id, out string identity))
            {
                return;
            }

            groupExpansionStates[identity] = args.isExpanded;
            SessionState.SetBool(ExpansionSessionPrefix + identity, args.isExpanded);
        }


        private void ExpandAllGroups()
        {
            SetAllCurrentGroupExpansion(expanded: true);
        }


        private void CollapseAllGroups()
        {
            SetAllCurrentGroupExpansion(expanded: false);
        }


        private void SetAllCurrentGroupExpansion(bool expanded)
        {
            if (elementTree == null) { return; }

            foreach (int treeId in currentGroupTreeIds)
            {
                if (groupIdentitiesByTreeId.TryGetValue(treeId, out string identity))
                {
                    groupExpansionStates[identity] = expanded;
                    SessionState.SetBool(ExpansionSessionPrefix + identity, expanded);
                }
            }

            restoringTreeExpansion = true;
            try
            {
                if (expanded) { elementTree.ExpandAll(); }
                else { elementTree.CollapseAll(); }
            }
            finally
            {
                restoringTreeExpansion = false;
            }
        }


        private void ApplySearchFilter()
        {
            structureDirty = true;
            RebuildTree();
        }


        private void BindElementTreeRow(Label label, in DebuggerTreeItem item)
        {
            if (!TryGetCurrentSnapshot(item.World, item.Key, out ElementSnapshot snapshot))
            {
                label.text = $"{item.Key.Slot}:{item.Key.Generation}  stale";
                return;
            }

            ElementDebuggerRowMetadata metadata = default;
            if (snapshotSetsByWorldId.TryGetValue(item.World.WorldId, out WorldSnapshotSet set))
            {
                set.RowMetadataByKey.TryGetValue(item.Key, out metadata);
            }

            string displayName = metadata.DisplayName;
            string kind = metadata.Kind;
            label.tooltip = metadata.Tooltip;
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = $"Element {snapshot.Key.Slot}:{snapshot.Key.Generation}";
            }
            string kindPrefix = string.IsNullOrWhiteSpace(kind) ? string.Empty : $"[{kind}] ";
            bool hasAllocatedLifetime = set != null && set.AllocatedLifetimeKeys.Contains(snapshot.Key);
            string lifetime = hasAllocatedLifetime
                ? $"  Life {snapshot.RemainingLifetime:0.##}"
                : string.Empty;
            label.text = $"{kindPrefix}{displayName}  {snapshot.Key.Slot}:{snapshot.Key.Generation}  " +
                $"{snapshot.Lifecycle}{lifetime}";
        }


        private void AddLaneNode(
            WorldSnapshotSet set,
            ElementLane lane,
            string search,
            List<TreeViewItemData<DebuggerTreeItem>> destination)
        {
            var elementChildren = new List<TreeViewItemData<DebuggerTreeItem>>();
            for (int i = 0; i < set.Snapshots.Count; i++)
            {
                ElementSnapshot snapshot = set.Snapshots[i];
                set.RowMetadataByKey.TryGetValue(snapshot.Key, out ElementDebuggerRowMetadata metadata);
                if (snapshot.Lane != lane || !MatchesSearch(in snapshot, in metadata, search))
                {
                    continue;
                }

                var elementItem = new DebuggerTreeItem(
                    DebuggerTreeItemKind.Element,
                    set.World,
                    snapshot.Key,
                    lane);
                var identity = new DebuggerElementIdentity(set.World.WorldId, snapshot.Key);
                int treeId = GetElementTreeId(in identity);
                elementChildren.Add(new TreeViewItemData<DebuggerTreeItem>(treeId, elementItem));
                visibleElementItems.Add(identity);
            }

            if (elementChildren.Count == 0) { return; }

            var laneItem = new DebuggerTreeItem(
                DebuggerTreeItemKind.Lane,
                set.World,
                default,
                lane);
            destination.Add(new TreeViewItemData<DebuggerTreeItem>(
                GetGroupTreeId($"lane:{set.StableOwnerId}:{lane}"),
                laneItem,
                elementChildren));
        }


        private static bool MatchesSearch(
            in ElementSnapshot snapshot,
            in ElementDebuggerRowMetadata metadata,
            string search)
        {
            if (string.IsNullOrEmpty(search)) { return true; }

            return snapshot.Key.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                snapshot.Lane.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                snapshot.ExecutionModel.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                snapshot.Capabilities.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (!string.IsNullOrWhiteSpace(metadata.DisplayName) &&
                    metadata.DisplayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrWhiteSpace(metadata.Kind) &&
                    metadata.Kind.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                snapshot.OwnerId.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                snapshot.TeamId.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }


        private int GetGroupTreeId(string identity)
        {
            if (groupTreeIds.TryGetValue(identity, out int id))
            {
                currentGroupTreeIds.Add(id);
                return id;
            }

            id = nextTreeId++;
            groupTreeIds.Add(identity, id);
            groupIdentitiesByTreeId.Add(id, identity);
            currentGroupTreeIds.Add(id);
            return id;
        }


        private int GetElementTreeId(in DebuggerElementIdentity identity)
        {
            if (elementTreeIds.TryGetValue(identity, out int id)) { return id; }

            id = nextTreeId++;
            elementTreeIds.Add(identity, id);
            return id;
        }



        private string GetStableOwnerId(ElementWorld world)
        {
            if (world != null && snapshotSetsByWorldId.TryGetValue(world.WorldId, out WorldSnapshotSet set))
            {
                return set.StableOwnerId;
            }
            return world == null ? string.Empty : $"world-{world.WorldId}";
        }



        private int GetExistingGroupTreeId(string identity) =>
            !string.IsNullOrWhiteSpace(identity) && groupTreeIds.TryGetValue(identity, out int id) ? id : -1;
    }
}
