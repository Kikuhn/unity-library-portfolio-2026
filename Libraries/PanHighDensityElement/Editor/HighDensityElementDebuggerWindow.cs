using System;
using System.Collections.Generic;
using System.Text;
using Unity.Mathematics;
using Unity.U2D.Physics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Profiling;



namespace Pan.HighDensityElement.Editor
{
    /// <summary>
    /// GameObject가 없는 Element를 Hierarchy 목록과 단일 Inspector 흐름으로 확인하는 진단 창입니다.
    /// </summary>
    public sealed partial class HighDensityElementDebuggerWindow : EditorWindow
    {
        private const double RefreshIntervalSeconds = 0.2d;
        private const float DefaultHierarchyWidth = 300f;
        private const float MinimumHierarchyWidth = 170f;
        private const float MinimumInspectorWidth = 280f;
        private const float SplitterWidth = 7f;
        private const string ExpansionSessionPrefix = "Pan.HighDensityElement.Explorer.Expansion.";
        private static readonly ProfilerMarker StructureRefreshMarker =
            new ProfilerMarker("Pan.HighDensityElement.Debugger.StructureRefresh");
        private static readonly ProfilerMarker VisibleRowsMarker =
            new ProfilerMarker("Pan.HighDensityElement.Debugger.VisibleRows");
        private static readonly ProfilerMarker InspectorMarker =
            new ProfilerMarker("Pan.HighDensityElement.Debugger.Inspector");
        private static readonly ProfilerMarker SceneCacheMarker =
            new ProfilerMarker("Pan.HighDensityElement.Debugger.SceneCache");
        private static readonly List<string> ScenePoseNames = new List<string>
        {
            "Presentation",
            "PhysicsDiagnostic"
        };

        private readonly List<ElementWorld> worlds = new List<ElementWorld>(4);
        private readonly Dictionary<int, WorldSnapshotSet> snapshotSetsByWorldId =
            new Dictionary<int, WorldSnapshotSet>(4);
        private readonly List<int> staleWorldIds = new List<int>(4);
        private readonly List<TreeViewItemData<DebuggerTreeItem>> treeRoots =
            new List<TreeViewItemData<DebuggerTreeItem>>(4);
        private readonly Dictionary<string, int> groupTreeIds = new Dictionary<string, int>(32);
        private readonly Dictionary<int, string> groupIdentitiesByTreeId = new Dictionary<int, string>(32);
        private readonly Dictionary<string, bool> groupExpansionStates =
            new Dictionary<string, bool>(StringComparer.Ordinal);
        private readonly HashSet<int> currentGroupTreeIds = new HashSet<int>();
        private readonly Dictionary<DebuggerElementIdentity, int> elementTreeIds =
            new Dictionary<DebuggerElementIdentity, int>(1024);
        private readonly HashSet<DebuggerElementIdentity> visibleElementItems =
            new HashSet<DebuggerElementIdentity>();
        private readonly List<ElementFact> recentFacts = new List<ElementFact>(128);
        private readonly List<IElementDebuggerRowProvider> rowProviders =
            new List<IElementDebuggerRowProvider>(4);
        private readonly List<IElementDebuggerComponentProvider> componentProviders =
            new List<IElementDebuggerComponentProvider>(4);
        private readonly List<IElementDebuggerWorldMetadataProvider> worldMetadataProviders =
            new List<IElementDebuggerWorldMetadataProvider>(4);
        private readonly List<VisibleComponent> visibleComponents = new List<VisibleComponent>(8);
        private readonly Dictionary<string, int> componentIdCounts =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, bool> componentFoldouts =
            new Dictionary<string, bool>(StringComparer.Ordinal);
        private readonly HashSet<string> drawnConflictIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<int, ElementWorldDebugCaptureLease> captureLeases =
            new Dictionary<int, ElementWorldDebugCaptureLease>(4);
        private readonly List<IElementDebuggerInspectorExtension> inspectorExtensions =
            new List<IElementDebuggerInspectorExtension>(4);
        private readonly Dictionary<int, ScenePickTarget> scenePickTargets =
            new Dictionary<int, ScenePickTarget>(1024);
        private readonly List<SceneVisibleCandidate> sceneVisibleCandidates =
            new List<SceneVisibleCandidate>(1024);

        private ToolbarSearchField searchField;
        private IVisualElementScheduledItem searchDebounceItem;
        private IVisualElementScheduledItem refreshScheduledItem;
        private IVisualElementScheduledItem treeExpansionRestoreItem;
        private PopupField<string> scenePosePopup;
        private Toggle drawSceneToggle;
        private Toggle liveToggle;
        private Label summaryLabel;
        private Label statusLabel;
        private TreeView elementTree;
        private VisualElement splitView;
        private VisualElement splitHandle;
        private VisualElement hierarchyPane;
        private VisualElement inspectorPane;
        private ScrollView inspectorScroll;
        private IMGUIContainer inspectorContainer;
        private Texture worldTreeIcon;
        private Texture physicsTreeIcon;
        private Texture dynamicBodyTreeIcon;
        private Texture queryElementTreeIcon;
        private CircleGeometry[] circleScratch = Array.Empty<CircleGeometry>();
        private PolygonGeometry[] polygonScratch = Array.Empty<PolygonGeometry>();
        private int nextTreeId = 1;
        private int activeSceneControlId;
        private ElementKey pendingEditKey;
        private int pendingEditWorldId;
        private string pendingEditLabel;
        private ulong pendingEditRefreshSerial;
        private uint pendingEditFixedStepIndex;
        private ulong snapshotRefreshSerial;
        private double nextRefreshTime;
        private bool synchronizingTreeSelection;
        private bool restoringTreeExpansion;
        private bool structureDirty = true;
        private bool inspectorDirty = true;
        private DebuggerWindowSelection windowSelection;
        private int structureRebuildCount;
        private int structureEnumerationCount;
        private double lastRefreshMilliseconds;
        private long lastRefreshAllocatedBytes;
        private int lastTotalAlive;
        private bool splitDragging;
        private int splitPointerId = -1;
        private float splitDragStartX;
        private float splitDragStartWidth;
        private string hierarchyWidthPreferenceKey;

        [SerializeField]
        private float hierarchyPaneWidth = DefaultHierarchyWidth;



        [MenuItem("Window/Analysis/Element World Explorer")]
        public static void Open()
        {
            HighDensityElementDebuggerWindow window = GetWindow<HighDensityElementDebuggerWindow>();
            window.titleContent = new GUIContent("Element World Explorer");
            window.Show();
        }


        private void RefreshPendingEditState()
        {
            if (!pendingEditKey.IsValid) { return; }
            if (!snapshotSetsByWorldId.TryGetValue(pendingEditWorldId, out WorldSnapshotSet set) ||
                !set.World.TryGetSnapshot(pendingEditKey, out _) ||
                set.World.FixedStepIndex != pendingEditFixedStepIndex)
            {
                pendingEditKey = default;
                pendingEditWorldId = 0;
                pendingEditLabel = null;
                pendingEditRefreshSerial = 0;
                pendingEditFixedStepIndex = 0;
            }

            RefreshPendingEditStatus();
        }



        private bool TrySubmitEdit<TCommand>(
            ElementHandle handle,
            in TCommand command,
            string label)
            where TCommand : unmanaged, IElementCommand
        {
            if (!handle.IsAlive || !handle.TrySubmit(in command))
            {
                ShowNotification(new GUIContent($"{label} 변경 실패 · Element가 stale이거나 명령을 받을 수 없습니다."));
                RefreshInspectorValues();
                return false;
            }
            pendingEditKey = handle.Key;
            pendingEditWorldId = ElementDebuggerSelection.World?.WorldId ?? 0;
            pendingEditLabel = label;
            pendingEditRefreshSerial = snapshotRefreshSerial;
            pendingEditFixedStepIndex = ElementDebuggerSelection.World?.FixedStepIndex ?? 0;
            RefreshPendingEditStatus();
            return true;
        }



        private void EnsureCaptureLease(ElementWorld world)
        {
            if (captureLeases.TryGetValue(world.WorldId, out ElementWorldDebugCaptureLease lease) &&
                lease != null && lease.IsValid)
            {
                return;
            }

            lease?.Dispose();
            captureLeases[world.WorldId] = ElementWorldDebugRegistry.AcquireCapture(world);
        }

        private void ReleaseCaptureLease(int worldId)
        {
            if (!captureLeases.TryGetValue(worldId, out ElementWorldDebugCaptureLease lease)) { return; }
            captureLeases.Remove(worldId);
            lease?.Dispose();
        }

        private void ReleaseCaptureLeases()
        {
            foreach (ElementWorldDebugCaptureLease lease in captureLeases.Values)
            {
                lease?.Dispose();
            }
            captureLeases.Clear();
        }

        private bool TryGetCurrentSnapshot(
            ElementWorld world,
            ElementKey key,
            out ElementSnapshot snapshot)
        {
            if (world != null && !world.IsDisposed && world.TryGetSnapshot(key, out snapshot))
            {
                return true;
            }

            snapshot = default;
            return false;
        }
        private string GetDisplayName(ElementWorld world, ElementKey key)
        {
            if (world != null &&
                snapshotSetsByWorldId.TryGetValue(world.WorldId, out WorldSnapshotSet set) &&
                set.RowMetadataByKey.TryGetValue(key, out ElementDebuggerRowMetadata metadata) &&
                !string.IsNullOrWhiteSpace(metadata.DisplayName))
            {
                return metadata.DisplayName;
            }
            return $"Element {key.Slot}:{key.Generation}";
        }
        private sealed class WorldSnapshotSet
        {
            public WorldSnapshotSet(ElementWorld world)
            {
                World = world;
            }



            public ElementWorld World { get; }
            public List<ElementSnapshot> Snapshots { get; } = new List<ElementSnapshot>(1024);
            public Dictionary<ElementKey, ElementSnapshot> SnapshotsByKey { get; } =
                new Dictionary<ElementKey, ElementSnapshot>(1024);
            public Dictionary<ElementKey, ElementDebuggerRowMetadata> RowMetadataByKey { get; } =
                new Dictionary<ElementKey, ElementDebuggerRowMetadata>(1024);
            public HashSet<ElementKey> AllocatedLifetimeKeys { get; } = new HashSet<ElementKey>();
            public List<string> ExtensionSummaries { get; } = new List<string>(4);
            public ulong StructuralRevision { get; set; } = ulong.MaxValue;
            public bool RowMetadataDirty { get; set; } = true;
            public bool MetadataDirty { get; set; } = true;
            public ElementDebuggerWorldMetadata Metadata { get; set; }
            public string StableOwnerId { get; set; } = string.Empty;



            public void RefreshLookup()
            {
                SnapshotsByKey.Clear();
                for (int i = 0; i < Snapshots.Count; i++)
                {
                    SnapshotsByKey[Snapshots[i].Key] = Snapshots[i];
                }
            }

            public int Count(ElementLane lane)
            {
                int count = 0;
                for (int i = 0; i < Snapshots.Count; i++)
                {
                    if (Snapshots[i].Lane == lane) { count++; }
                }
                return count;
            }
        }



        private readonly struct DebuggerWindowSelection
        {
            public DebuggerWindowSelection(
                DebuggerTreeItemKind kind,
                ElementWorld world,
                ElementLane lane,
                ElementKey key)
            {
                Kind = kind;
                World = world;
                Lane = lane;
                Key = key;
            }



            public DebuggerTreeItemKind Kind { get; }
            public ElementWorld World { get; }
            public ElementLane Lane { get; }
            public ElementKey Key { get; }
            public bool IsValid => World != null && !World.IsDisposed;
        }



        private readonly struct ScenePickTarget
        {
            public ScenePickTarget(ElementWorld world, ElementKey key)
            {
                World = world;
                Key = key;
            }



            public ElementWorld World { get; }
            public ElementKey Key { get; }
        }



        private readonly struct SceneVisibleCandidate
        {
            public SceneVisibleCandidate(ElementWorld world, in ElementSnapshot snapshot, float2 position)
            {
                World = world;
                Snapshot = snapshot;
                Position = position;
            }



            public ElementWorld World { get; }
            public ElementSnapshot Snapshot { get; }
            public float2 Position { get; }
        }
        private sealed class DebuggerTreeRow : VisualElement
        {
            public DebuggerTreeRow()
            {
                style.flexDirection = FlexDirection.Row;
                style.alignItems = Align.Center;

                Icon = new Image
                {
                    scaleMode = ScaleMode.ScaleToFit,
                    style =
                    {
                        width = 16f,
                        height = 16f,
                        marginRight = 3f,
                        flexShrink = 0f
                    }
                };
                Add(Icon);

                Label = new Label
                {
                    style =
                    {
                        flexGrow = 1f,
                        unityTextAlign = TextAnchor.MiddleLeft,
                        whiteSpace = WhiteSpace.NoWrap
                    }
                };
                Add(Label);
            }



            public Image Icon { get; }
            public Label Label { get; }
        }



        private enum DebuggerTreeItemKind : byte
        {
            World,
            Lane,
            Element
        }



        private readonly struct DebuggerTreeItem
        {
            public DebuggerTreeItem(
                DebuggerTreeItemKind kind,
                ElementWorld world,
                ElementKey key,
                ElementLane lane)
            {
                Kind = kind;
                World = world;
                Key = key;
                Lane = lane;
            }



            public DebuggerTreeItemKind Kind { get; }
            public ElementWorld World { get; }
            public ElementKey Key { get; }
            public ElementLane Lane { get; }
        }



        private readonly struct VisibleComponent
        {
            public VisibleComponent(
                IElementDebuggerComponentProvider provider,
                in ElementDebuggerComponentDescriptor descriptor)
            {
                Provider = provider;
                Descriptor = descriptor;
            }



            public IElementDebuggerComponentProvider Provider { get; }
            public ElementDebuggerComponentDescriptor Descriptor { get; }
        }



        private readonly struct DebuggerElementIdentity : IEquatable<DebuggerElementIdentity>
        {
            public DebuggerElementIdentity(int worldId, ElementKey key)
            {
                WorldId = worldId;
                Key = key;
            }



            public int WorldId { get; }
            public ElementKey Key { get; }



            public bool Equals(DebuggerElementIdentity other) =>
                WorldId == other.WorldId && Key.Equals(other.Key);

            public override bool Equals(object obj) =>
                obj is DebuggerElementIdentity other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(WorldId, Key);
        }
    }
}
