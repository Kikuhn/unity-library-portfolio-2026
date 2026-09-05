using System;
using Unity.Mathematics;
using Unity.U2D.Physics;
using UnityEditor;
using UnityEngine;



namespace Pan.HighDensityElement.Editor
{
    public sealed partial class HighDensityElementDebuggerWindow
    {
        ///======================================================================================================================================================
        //? Scene View 표시, 피킹, 선택 대상 이동
        ///======================================================================================================================================================



        private void OnSceneGui(SceneView sceneView)
        {
            Event currentEvent = Event.current;
            float interpolationAlpha = GetPresentationAlpha();
            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.F &&
                !currentEvent.alt && !currentEvent.control && !currentEvent.command)
            {
                if (FrameSelected(sceneView, interpolationAlpha)) { currentEvent.Use(); }
                return;
            }

            if (drawSceneToggle == null || !drawSceneToggle.value) { return; }

            RegisterSceneControls(sceneView.camera, currentEvent, interpolationAlpha);
            HandleSceneControlInput(sceneView, currentEvent, interpolationAlpha);
            if (currentEvent.type == EventType.Repaint)
            {
                DrawSceneGeometry(sceneView.camera, interpolationAlpha);
            }
        }



        private void DrawSceneGeometry(Camera camera, float interpolationAlpha)
        {
            using (SceneCacheMarker.Auto())
            {
            RefreshSceneVisibleCache(camera, interpolationAlpha);
            bool hasSelection = ElementDebuggerSelection.TryGet(
                out ElementWorld selectedWorld,
                out _,
                out ElementSnapshot selectedSnapshot);
            float2 selectedPresentationPosition = default;
            bool hasSelectedPresentation = hasSelection &&
                TryResolvePresentationPosition(
                    selectedWorld,
                    in selectedSnapshot,
                    interpolationAlpha,
                    out selectedPresentationPosition);

            for (int worldIndex = 0; worldIndex < worlds.Count; worldIndex++)
            {
                ElementWorld world = worlds[worldIndex];
                if (world == null || world.IsDisposed) { continue; }
                if (!snapshotSetsByWorldId.TryGetValue(world.WorldId, out WorldSnapshotSet set) ||
                    !world.PhysicsCoreLane.IsValid)
                {
                    continue;
                }

                DrawCachedPresentationGeometryBatch(
                    world,
                    new Color(0.1f, 0.65f, 1f, 0.48f),
                    hasSelection ? selectedWorld : null,
                    hasSelection ? selectedSnapshot.Key : default);

                if (!IsPresentationMode)
                {
                    DrawPoseGeometryBatch(
                        world,
                        set,
                        camera,
                        interpolationAlpha,
                        usePresentationPose: false,
                        new Color(0.05f, 0.9f, 0.82f, 0.52f),
                        hasSelection ? selectedWorld : null,
                        hasSelection ? selectedSnapshot.Key : default);
                    DrawSweepPaths(
                        world,
                        set,
                        camera,
                        hasSelection ? selectedWorld : null,
                        hasSelection ? selectedSnapshot.Key : default);
                }
            }

            if (hasSelectedPresentation)
            {
                DrawSelectedGeometry(in selectedSnapshot, selectedPresentationPosition, Color.yellow, 3f);
                if (!IsPresentationMode)
                {
                    DrawSelectedGeometry(
                        in selectedSnapshot,
                        selectedSnapshot.Position,
                        new Color(0.05f, 1f, 0.9f, 1f),
                        3f);
                }
            }

            DrawSceneLegend();
            }
        }



        private void RefreshSceneVisibleCache(Camera camera, float interpolationAlpha)
        {
            sceneVisibleCandidates.Clear();
            for (int worldIndex = 0; worldIndex < worlds.Count; worldIndex++)
            {
                ElementWorld world = worlds[worldIndex];
                if (world == null || world.IsDisposed) { continue; }
                if (!snapshotSetsByWorldId.TryGetValue(world.WorldId, out WorldSnapshotSet set)) { continue; }
                for (int i = 0; i < set.Snapshots.Count; i++)
                {
                    ElementKey key = set.Snapshots[i].Key;
                    if (!world.TryGetSnapshot(key, out ElementSnapshot snapshot) ||
                        !TryResolvePresentationPosition(
                            world,
                            in snapshot,
                            interpolationAlpha,
                            out float2 position) ||
                        !IsVisible(camera, position))
                    {
                        continue;
                    }
                    sceneVisibleCandidates.Add(new SceneVisibleCandidate(world, in snapshot, position));
                }
            }
        }



        private void DrawCachedPresentationGeometryBatch(
            ElementWorld world,
            Color color,
            ElementWorld selectedWorld,
            ElementKey selectedKey)
        {
            EnsureGeometryCapacity(sceneVisibleCandidates.Count);
            int circleCount = 0;
            int polygonCount = 0;
            for (int i = 0; i < sceneVisibleCandidates.Count; i++)
            {
                SceneVisibleCandidate candidate = sceneVisibleCandidates[i];
                if (!ReferenceEquals(candidate.World, world) ||
                    (ReferenceEquals(world, selectedWorld) && candidate.Snapshot.Key == selectedKey))
                {
                    continue;
                }

                if (candidate.Snapshot.PhysicsShape == PhysicsCoreShape2D.Box)
                {
                    ElementSnapshot snapshot = candidate.Snapshot;
                    Vector2 size = GetBoxSize(in snapshot);
                    PolygonGeometry box = PolygonGeometry.CreateBox(size, 0f, true);
                    polygonScratch[polygonCount++] = box.Transform(new PhysicsTransform(
                        new Vector2(candidate.Position.x, candidate.Position.y),
                        PhysicsRotate.FromDegrees(0f)));
                }
                else
                {
                    circleScratch[circleCount++] = new CircleGeometry
                    {
                        center = new Vector2(candidate.Position.x, candidate.Position.y),
                        radius = Mathf.Max(0.0001f, candidate.Snapshot.Radius)
                    };
                }
            }

            PhysicsTransform identity = new PhysicsTransform(Vector2.zero, PhysicsRotate.FromDegrees(0f));
            if (circleCount > 0)
            {
                world.PhysicsCoreLane.World.DrawGeometry(
                    circleScratch.AsSpan(0, circleCount),
                    identity,
                    color,
                    0f,
                    PhysicsWorld.DrawFillOptions.Outline);
            }
            if (polygonCount > 0)
            {
                world.PhysicsCoreLane.World.DrawGeometry(
                    polygonScratch.AsSpan(0, polygonCount),
                    identity,
                    color,
                    0f,
                    PhysicsWorld.DrawFillOptions.Outline);
            }
        }



        private void DrawPoseGeometryBatch(
            ElementWorld world,
            WorldSnapshotSet set,
            Camera camera,
            float interpolationAlpha,
            bool usePresentationPose,
            Color color,
            ElementWorld selectedWorld,
            ElementKey selectedKey)
        {
            EnsureGeometryCapacity(set.Snapshots.Count);
            int circleCount = 0;
            int polygonCount = 0;
            for (int i = 0; i < set.Snapshots.Count; i++)
            {
                ElementSnapshot cached = set.Snapshots[i];
                if (!world.TryGetSnapshot(cached.Key, out ElementSnapshot snapshot)) { continue; }
                if (ReferenceEquals(world, selectedWorld) && snapshot.Key == selectedKey) { continue; }

                float2 position;
                bool resolved = usePresentationPose
                    ? TryResolvePresentationPosition(world, in snapshot, interpolationAlpha, out position)
                    : TryResolvePhysicsPosition(world, in snapshot, out position);
                if (!resolved || !IsVisible(camera, position)) { continue; }

                if (snapshot.PhysicsShape == PhysicsCoreShape2D.Box)
                {
                    Vector2 size = GetBoxSize(in snapshot);
                    PolygonGeometry box = PolygonGeometry.CreateBox(size, 0f, true);
                    polygonScratch[polygonCount++] = box.Transform(new PhysicsTransform(
                        new Vector2(position.x, position.y),
                        PhysicsRotate.FromDegrees(0f)));
                }
                else
                {
                    circleScratch[circleCount++] = new CircleGeometry
                    {
                        center = new Vector2(position.x, position.y),
                        radius = Mathf.Max(0.0001f, snapshot.Radius)
                    };
                }
            }

            PhysicsTransform identity = new PhysicsTransform(Vector2.zero, PhysicsRotate.FromDegrees(0f));
            if (circleCount > 0)
            {
                world.PhysicsCoreLane.World.DrawGeometry(
                    circleScratch.AsSpan(0, circleCount),
                    identity,
                    color,
                    0f,
                    PhysicsWorld.DrawFillOptions.Outline);
            }
            if (polygonCount > 0)
            {
                world.PhysicsCoreLane.World.DrawGeometry(
                    polygonScratch.AsSpan(0, polygonCount),
                    identity,
                    color,
                    0f,
                    PhysicsWorld.DrawFillOptions.Outline);
            }
        }



        private static void DrawSweepPaths(
            ElementWorld world,
            WorldSnapshotSet set,
            Camera camera,
            ElementWorld selectedWorld,
            ElementKey selectedKey)
        {
            for (int i = 0; i < set.Snapshots.Count; i++)
            {
                ElementSnapshot cached = set.Snapshots[i];
                if (!world.TryGetSnapshot(cached.Key, out ElementSnapshot snapshot) ||
                    (!IsVisible(camera, snapshot.PreviousPosition) && !IsVisible(camera, snapshot.Position)))
                {
                    continue;
                }

                bool isSelected = ReferenceEquals(world, selectedWorld) && snapshot.Key == selectedKey;
                Handles.color = isSelected
                    ? new Color(1f, 0.92f, 0.25f, 0.72f)
                    : new Color(0.65f, 0.65f, 0.65f, 0.42f);
                Handles.DrawLine(
                    new Vector3(snapshot.PreviousPosition.x, snapshot.PreviousPosition.y, 0f),
                    new Vector3(snapshot.Position.x, snapshot.Position.y, 0f),
                    isSelected ? 2f : 1f);
            }
        }



        private static void DrawSelectedGeometry(
            in ElementSnapshot snapshot,
            float2 position,
            Color color,
            float thickness)
        {
            Handles.color = color;
            if (snapshot.PhysicsShape == PhysicsCoreShape2D.Box)
            {
                Vector2 size = GetBoxSize(in snapshot);
                float halfX = size.x * 0.5f;
                float halfY = size.y * 0.5f;
                var bottomLeft = new Vector3(position.x - halfX, position.y - halfY, 0f);
                var topLeft = new Vector3(position.x - halfX, position.y + halfY, 0f);
                var topRight = new Vector3(position.x + halfX, position.y + halfY, 0f);
                var bottomRight = new Vector3(position.x + halfX, position.y - halfY, 0f);
                Handles.DrawLine(bottomLeft, topLeft, thickness);
                Handles.DrawLine(topLeft, topRight, thickness);
                Handles.DrawLine(topRight, bottomRight, thickness);
                Handles.DrawLine(bottomRight, bottomLeft, thickness);
                return;
            }

            Handles.DrawWireDisc(
                new Vector3(position.x, position.y, 0f),
                Vector3.forward,
                Mathf.Max(0.0001f, snapshot.Radius),
                thickness);
        }



        private void DrawSceneLegend()
        {
            Handles.BeginGUI();
            float height = IsPresentationMode ? 44f : 82f;
            GUI.Box(new Rect(12f, 12f, 220f, height), GUIContent.none, EditorStyles.helpBox);
            DrawLegendRow(22f, new Color(0.1f, 0.65f, 1f, 1f), "Presentation pose (picking)");
            if (!IsPresentationMode)
            {
                DrawLegendRow(42f, new Color(0.05f, 0.9f, 0.82f, 1f), "Physics current pose");
                DrawLegendRow(62f, new Color(0.65f, 0.65f, 0.65f, 0.7f), "Previous → Current sweep");
            }
            Handles.EndGUI();
        }



        private static void DrawLegendRow(float y, Color color, string label)
        {
            EditorGUI.DrawRect(new Rect(22f, y, 12f, 12f), color);
            GUI.Label(new Rect(40f, y - 2f, 180f, 18f), label, EditorStyles.miniLabel);
        }



        private void RegisterSceneControls(Camera camera, Event currentEvent, float interpolationAlpha)
        {
            scenePickTargets.Clear();
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            float nearestDistance = float.PositiveInfinity;
            ScenePickTarget nearestTarget = default;
            bool hasTarget = false;
            for (int i = 0; i < sceneVisibleCandidates.Count; i++)
            {
                SceneVisibleCandidate candidate = sceneVisibleCandidates[i];
                ElementSnapshot snapshot = candidate.Snapshot;
                float distance = GetGuiDistanceToGeometry(
                    in snapshot,
                    candidate.Position,
                    currentEvent.mousePosition);
                if (distance > 10f || distance >= nearestDistance) { continue; }
                nearestDistance = distance;
                nearestTarget = new ScenePickTarget(candidate.World, snapshot.Key);
                hasTarget = true;
            }

            if (!hasTarget) { return; }
            scenePickTargets[controlId] = nearestTarget;
            if (currentEvent.type == EventType.Layout) { HandleUtility.AddControl(controlId, nearestDistance); }
        }



        private void HandleSceneControlInput(
            SceneView sceneView,
            Event currentEvent,
            float interpolationAlpha)
        {
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.alt &&
                scenePickTargets.TryGetValue(HandleUtility.nearestControl, out ScenePickTarget target))
            {
                activeSceneControlId = HandleUtility.nearestControl;
                GUIUtility.hotControl = activeSceneControlId;
                if (ElementDebuggerSelection.TrySet(target.World, target.Key) && currentEvent.clickCount >= 2)
                {
                    FrameSelected(sceneView, interpolationAlpha);
                }
                currentEvent.Use();
                return;
            }

            if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0 &&
                activeSceneControlId != 0 && GUIUtility.hotControl == activeSceneControlId)
            {
                GUIUtility.hotControl = 0;
                activeSceneControlId = 0;
                currentEvent.Use();
            }
        }



        private static bool FrameSelected(
            SceneView sceneView,
            float interpolationAlpha,
            bool instant = false)
        {
            if (sceneView == null || !ElementDebuggerSelection.TryGet(
                    out ElementWorld world,
                    out _,
                    out ElementSnapshot snapshot) ||
                !TryResolvePresentationPosition(
                    world,
                    in snapshot,
                    interpolationAlpha,
                    out float2 presentationPosition))
            {
                return false;
            }

            Vector3 target = new Vector3(presentationPosition.x, presentationPosition.y, 0f);
            sceneView.LookAt(
                target,
                sceneView.rotation,
                sceneView.size,
                sceneView.orthographic,
                instant);
            return true;
        }

        private static bool TryResolvePresentationPosition(
            ElementWorld world,
            in ElementSnapshot snapshot,
            float interpolationAlpha,
            out float2 position)
        {
            if (world != null && world.TryGetRenderSnapshot(
                    snapshot.Key,
                    interpolationAlpha,
                    out ElementRenderSnapshot renderSnapshot))
            {
                position = renderSnapshot.Position;
                return true;
            }

            position = default;
            return false;
        }



        private static bool TryResolvePhysicsPosition(
            ElementWorld world,
            in ElementSnapshot snapshot,
            out float2 position)
        {
            if (world != null && world.TryGetSnapshot(snapshot.Key, out ElementSnapshot diagnosticSnapshot))
            {
                position = diagnosticSnapshot.Position;
                return true;
            }

            position = default;
            return false;
        }



        private bool IsPresentationMode => scenePosePopup == null || scenePosePopup.index == 0;



        private static float GetPresentationAlpha()
        {
            if (!EditorApplication.isPlaying || Time.fixedDeltaTime <= 0f) { return 1f; }
            return Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
        }



        private static float GetGuiDistanceToGeometry(
            in ElementSnapshot snapshot,
            float2 position,
            Vector2 mousePosition)
        {
            Vector2 center = HandleUtility.WorldToGUIPoint(new Vector3(position.x, position.y, 0f));
            if (snapshot.PhysicsShape == PhysicsCoreShape2D.Box)
            {
                Vector2 size = GetBoxSize(in snapshot);
                float halfX = size.x * 0.5f;
                float halfY = size.y * 0.5f;
                Vector2 bottomLeft = HandleUtility.WorldToGUIPoint(
                    new Vector3(position.x - halfX, position.y - halfY, 0f));
                Vector2 topLeft = HandleUtility.WorldToGUIPoint(
                    new Vector3(position.x - halfX, position.y + halfY, 0f));
                Vector2 topRight = HandleUtility.WorldToGUIPoint(
                    new Vector3(position.x + halfX, position.y + halfY, 0f));
                Vector2 bottomRight = HandleUtility.WorldToGUIPoint(
                    new Vector3(position.x + halfX, position.y - halfY, 0f));
                float xMin = Mathf.Min(Mathf.Min(bottomLeft.x, topLeft.x), Mathf.Min(topRight.x, bottomRight.x));
                float xMax = Mathf.Max(Mathf.Max(bottomLeft.x, topLeft.x), Mathf.Max(topRight.x, bottomRight.x));
                float yMin = Mathf.Min(Mathf.Min(bottomLeft.y, topLeft.y), Mathf.Min(topRight.y, bottomRight.y));
                float yMax = Mathf.Max(Mathf.Max(bottomLeft.y, topLeft.y), Mathf.Max(topRight.y, bottomRight.y));
                var bounds = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
                if (bounds.Contains(mousePosition)) { return 0f; }

                float dx = Mathf.Max(bounds.xMin - mousePosition.x, 0f, mousePosition.x - bounds.xMax);
                float dy = Mathf.Max(bounds.yMin - mousePosition.y, 0f, mousePosition.y - bounds.yMax);
                return Mathf.Sqrt(dx * dx + dy * dy);
            }

            Vector2 edge = HandleUtility.WorldToGUIPoint(new Vector3(
                position.x + Mathf.Max(0.0001f, snapshot.Radius),
                position.y,
                0f));
            float radius = Vector2.Distance(center, edge);
            return Mathf.Max(0f, Vector2.Distance(center, mousePosition) - radius);
        }



        private void EnsureGeometryCapacity(int capacity)
        {
            if (circleScratch.Length >= capacity && polygonScratch.Length >= capacity) { return; }

            int expandedCapacity = Mathf.NextPowerOfTwo(Mathf.Max(16, capacity));
            if (circleScratch.Length < expandedCapacity) { circleScratch = new CircleGeometry[expandedCapacity]; }
            if (polygonScratch.Length < expandedCapacity) { polygonScratch = new PolygonGeometry[expandedCapacity]; }
        }



        private static Vector2 GetBoxSize(in ElementSnapshot snapshot) => new Vector2(
            Mathf.Max(0.0001f, snapshot.Scale.x * 2f * snapshot.Radius),
            Mathf.Max(0.0001f, snapshot.Scale.y * 2f * snapshot.Radius));



        private static bool IsVisible(Camera camera, float2 position)
        {
            if (camera == null) { return true; }
            Vector3 viewport = camera.WorldToViewportPoint(new Vector3(position.x, position.y, 0f));
            return viewport.z >= 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
        }



        private void RequestSceneRepaint()
        {
            if (drawSceneToggle?.value ?? false) { SceneView.RepaintAll(); }
        }
    }
}
