using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;



namespace Pan.HighDensityElement.Editor
{
    public sealed partial class HighDensityElementDebuggerWindow
    {
        private Vector2Field positionEditField;
        private FloatField rotationEditField;
        private Vector2Field scaleEditField;
        private Vector2Field velocityEditField;
        private Vector2Field accelerationEditField;
        private ColorField colorEditField;
        private FloatField lifetimeEditField;
        private HelpBox pendingEditStatusBox;
        private int boundElementWorldId;
        private ElementKey boundElementKey;



        private void RefreshInspector()
        {
            if (!inspectorDirty || inspectorScroll == null) { return; }
            using (InspectorMarker.Auto())
            {
                inspectorDirty = false;
                ResetElementInspectorBindings();
                inspectorScroll.Clear();

                if (!windowSelection.IsValid)
                {
                    inspectorScroll.Add(CreateHelpBox(
                        "Hierarchy에서 World, Lane 또는 Element를 선택하세요. 선택은 generation까지 검증됩니다.",
                        HelpBoxMessageType.Info));
                    return;
                }

                switch (windowSelection.Kind)
                {
                    case DebuggerTreeItemKind.World:
                        BuildWorldInspector(windowSelection.World);
                        break;
                    case DebuggerTreeItemKind.Lane:
                        BuildLaneInspector(windowSelection.World, windowSelection.Lane);
                        break;
                    case DebuggerTreeItemKind.Element:
                        BuildElementInspector(windowSelection.World, windowSelection.Key);
                        break;
                }
            }
        }



        private void BuildWorldInspector(ElementWorld world)
        {
            if (world == null || world.IsDisposed ||
                !snapshotSetsByWorldId.TryGetValue(world.WorldId, out WorldSnapshotSet set))
            {
                inspectorScroll.Add(CreateHelpBox("선택한 World가 더 이상 유효하지 않습니다.", HelpBoxMessageType.Warning));
                return;
            }

            ElementWorldDiagnostics diagnostics = world.GetDiagnostics();
            AddHeader(set.Metadata.DisplayName, $"ElementWorld {world.WorldId} · {(set.Metadata.IsRunning ? "실행 중" : "중지")}");

            Foldout owner = CreateSection("world.owner", "관리 주체와 갱신 경로", true);
            AddReadOnlyRow(owner, "Runtime", set.Metadata.RuntimeTypeName, "이 World를 생성하고 수명 주기를 소유하는 서비스입니다.");
            AddReadOnlyRow(owner, "Update Feature", set.Metadata.UpdatePath, "Lifetime과 LocalClock 같은 Update feature 경로입니다.");
            AddReadOnlyRow(owner, "PhysicsCore", set.Metadata.FixedUpdatePath, "Legacy Physics2D 완료 후 Core가 실행되는 고정 스텝 경로입니다.");
            AddReadOnlyRow(owner, "Presentation", set.Metadata.PresentationPath, "보간 snapshot과 instanced renderer 실행 경로입니다.");
            if (set.Metadata.OwnerObject != null)
            {
                var ping = new Button(() =>
                {
                    Selection.activeObject = set.Metadata.OwnerObject;
                    EditorGUIUtility.PingObject(set.Metadata.OwnerObject);
                }) { text = $"{set.Metadata.OwnerObject.name} 선택 및 Ping" };
                ping.tooltip = "실제 World 소유 GameObject를 Hierarchy와 Inspector에서 선택합니다.";
                owner.Add(ping);
            }
            inspectorScroll.Add(owner);

            Foldout totals = CreateSection("world.statistics", "World 상태", true);
            AddReadOnlyRow(totals, "활성 Element", diagnostics.AliveCount.ToString("N0"));
            AddReadOnlyRow(totals, "Query / Area / Simulated",
                $"{diagnostics.QueryCount:N0} / {diagnostics.AreaCount:N0} / {diagnostics.SimulatedCount:N0}");
            AddReadOnlyRow(totals, "PhysicsCore Body", diagnostics.PhysicsBodyCount.ToString("N0"));
            AddReadOnlyRow(totals, "Fixed Step", diagnostics.FixedStepIndex.ToString("N0"));
            AddReadOnlyRow(totals, "명령 / Fact",
                $"{diagnostics.LastCommandCount:N0} / {diagnostics.LastFactCount:N0}");
            AddReadOnlyRow(totals, "엄격 CCD / 강제 / Teleport",
                $"{diagnostics.StrictCcdCandidateCount:N0} / {diagnostics.ForcedStrictCcdCount:N0} / {diagnostics.TeleportCount:N0}");
            AddReadOnlyRow(totals, "방향 / Wave / 표시 회전 / 경계",
                $"{diagnostics.DirectionalMotionFeatureCount:N0} / {diagnostics.WaveMotionFeatureCount:N0} / " +
                $"{diagnostics.VisualOrientationFeatureCount:N0} / {diagnostics.BoundaryFeatureCount:N0}");
            AddReadOnlyRow(totals, "최근 경계 소거", diagnostics.BoundaryExitCount.ToString("N0"));
            AddReadOnlyRow(totals, "Simulation / Dispatch",
                $"{diagnostics.SimulationMilliseconds:0.000} ms / {diagnostics.FactDispatchMilliseconds:0.000} ms");
            AddReadOnlyRow(totals, "Structural Revision", world.StructuralRevision.ToString("N0"));
            inspectorScroll.Add(totals);

            Foldout lanes = CreateSection("world.lanes", "Lane 저장소", true);
            AddLaneSummary(lanes, world, ElementLane.QuerySprite2D);
            AddLaneSummary(lanes, world, ElementLane.AreaSensorSprite2D);
            AddLaneSummary(lanes, world, ElementLane.DynamicBodySprite2D);
            inspectorScroll.Add(lanes);

            if (set.ExtensionSummaries.Count > 0)
            {
                Foldout extensions = CreateSection("world.extensions", "Projection과 확장 진단", false);
                for (int i = 0; i < set.ExtensionSummaries.Count; i++)
                {
                    extensions.Add(new Label(set.ExtensionSummaries[i]) { style = { whiteSpace = WhiteSpace.Normal } });
                }
                inspectorScroll.Add(extensions);
            }

            Foldout debugger = CreateSection("world.debugger", "Explorer 비용", true);
            AddReadOnlyRow(debugger, "최근 갱신", $"{lastRefreshMilliseconds:0.000} ms");
            AddReadOnlyRow(debugger, "최근 Editor GC", $"{lastRefreshAllocatedBytes:N0} B");
            AddReadOnlyRow(debugger, "표시 / 전체", $"{visibleElementItems.Count:N0} / {lastTotalAlive:N0}");
            AddReadOnlyRow(debugger, "Tree rebuild / 구조 열거",
                $"{structureRebuildCount:N0} / {structureEnumerationCount:N0}");
            inspectorScroll.Add(debugger);
        }



        private void BuildLaneInspector(ElementWorld world, ElementLane lane)
        {
            if (world == null || world.IsDisposed)
            {
                inspectorScroll.Add(CreateHelpBox("선택한 Lane의 World가 더 이상 유효하지 않습니다.", HelpBoxMessageType.Warning));
                return;
            }

            AddHeader(GetLaneDisplayName(lane), $"World {world.WorldId} · {GetLaneCount(world, lane):N0} Element");
            inspectorScroll.Add(CreateHelpBox(GetLaneDescription(lane), HelpBoxMessageType.Info));

            Foldout overview = CreateSection("lane.overview", "실행 방식", true);
            AddReadOnlyRow(overview, "Execution Model", lane == ElementLane.DynamicBodySprite2D ? "Query 또는 Simulated" : "Query");
            AddReadOnlyRow(overview, "갱신 단계", GetLaneUpdateStage(lane));
            AddReadOnlyRow(overview, "지원 Capability", GetLaneCapabilities(lane));
            inspectorScroll.Add(overview);

            Foldout storage = CreateSection("lane.storage", "Native 저장소", true);
            if (world.TryGetLaneStorageDiagnostics(lane, out ElementLaneStorageDiagnostics diagnostics))
            {
                AddReadOnlyRow(storage, "Dense Count / Capacity", $"{diagnostics.DenseCount:N0} / {diagnostics.DenseCapacity:N0}");
                AddReadOnlyRow(storage, "Pose Count / Capacity", $"{diagnostics.PoseCount:N0} / {diagnostics.PoseCapacity:N0}");
                AddReadOnlyRow(storage, "PhysicsCore Body", diagnostics.UsesPhysicsCoreBodies ? "필요한 Element만 사용" : "기본 bodyless");
            }
            inspectorScroll.Add(storage);
        }



        private void BuildElementInspector(ElementWorld world, ElementKey key)
        {
            if (!ElementDebuggerSelection.TryGet(out ElementWorld selectedWorld, out ElementHandle handle, out ElementSnapshot snapshot) ||
                !ReferenceEquals(world, selectedWorld) || handle.Key != key)
            {
                inspectorScroll.Add(CreateHelpBox("선택한 Element가 despawn되었거나 generation이 변경되었습니다.", HelpBoxMessageType.Warning));
                return;
            }

            string displayName = GetDisplayName(world, key);
            boundElementWorldId = world.WorldId;
            boundElementKey = key;
            AddElementHeader(displayName, world, handle, in snapshot);

            Foldout glossary = CreateSection(
                "element.glossary",
                "용어 설명",
                false,
                "Element Inspector에서 반복해서 사용하는 식별자와 저장 구조의 의미입니다.");
            AddReadOnlyRow(glossary, "Key", "Context : Slot : Generation",
                "Element를 세대까지 구분하는 주소입니다. despawn 후 같은 Slot이 재사용되어도 이전 Key는 유효하지 않습니다.");
            AddReadOnlyRow(glossary, "Lifecycle", "Alive / DespawnPending",
                "Element가 현재 동작 중인지, 안전한 경계에서 제거되기를 기다리는지 나타냅니다.");
            AddReadOnlyRow(glossary, "Lane", "Native 실행 저장소",
                "같은 갱신 방식과 Job을 사용하는 Element가 함께 저장되는 Native dense 구역입니다.");
            AddReadOnlyRow(glossary, "Capability", "활성화된 런타임 기능",
                "해당 Element가 이동, Query, Dynamic Body, Rendering 같은 기능을 사용할 수 있음을 나타냅니다.");
            AddReadOnlyRow(glossary, "Sparse Feature", "실제로 할당된 선택 기능",
                "Lifetime처럼 사용하는 Element에만 별도 Native entry가 생기는 선택 상태입니다.");
            inspectorScroll.Add(glossary);

            Foldout identity = CreateSection(
                "element.identity",
                "Identity",
                true,
                "Element의 generation-safe 식별자와 실행 소속을 보여줍니다.");
            AddReadOnlyRow(identity, "Key", key.ToString(),
                "Context, Slot, Generation으로 구성된 세대 안전 식별자입니다.");
            AddReadOnlyRow(identity, "Lifecycle", snapshot.Lifecycle.ToString(),
                "현재 생명주기 상태입니다. DespawnPending은 다음 안전한 제거 경계를 기다리는 상태입니다.");
            AddReadOnlyRow(identity, "Lane / Execution", $"{snapshot.Lane} / {snapshot.ExecutionModel}",
                "Lane은 Native 저장·Job 처리 구역이고 Execution은 Query 또는 Dynamic 실행 모델입니다.");
            AddReadOnlyRow(identity, "Capabilities", snapshot.Capabilities.ToString(),
                "현재 Element에서 활성화된 이동, Query, PhysicsCore, Rendering 기능의 조합입니다.");
            AddReadOnlyRow(identity, "Owner / Team", $"{snapshot.OwnerId} / {snapshot.TeamId}",
                "게임플레이 소유자 식별자와 충돌·피해 규칙에서 사용하는 팀 식별자입니다.");
            inspectorScroll.Add(identity);

            Foldout transform = CreateSection("element.transform", "Transform 2D", true);
            AddReadOnlyRow(transform, "Previous Position", snapshot.PreviousPosition.ToString());
            positionEditField = AddEditableVector2(transform, "Position", snapshot.Position, "Enter 또는 포커스 이동 시 Teleport 명령으로 예약합니다.",
                value => new TeleportElement2D(value), handle);
            rotationEditField = new FloatField("Rotation (Degrees)")
            {
                value = math.degrees(snapshot.RotationRadians),
                isDelayed = true,
                tooltip = "표시 회전만 변경합니다. 충돌 형상의 방향이나 이동 방향은 바꾸지 않습니다."
            };
            rotationEditField.RegisterValueChangedCallback(evt =>
            {
                var command = new SetElementRotation2D(math.radians(evt.newValue));
                TrySubmitEdit(handle, in command, "Rotation");
            });
            transform.Add(rotationEditField);
            scaleEditField = AddEditableVector2(transform, "Scale", snapshot.Scale, "Enter 또는 포커스 이동 시 표시와 query 형상 scale 변경을 예약합니다.",
                value => new SetElementScale2D(value), handle);
            AddReadOnlyRow(transform, "Teleported This Step", snapshot.TeleportedThisStep.ToString());
            inspectorScroll.Add(transform);

            Foldout motion = CreateSection("element.motion", "Motion", true);
            velocityEditField = AddEditableVector2(motion, "Velocity", snapshot.Velocity, "Enter 또는 포커스 이동 시 속도 변경을 예약합니다.",
                value => new SetElementVelocity2D(value), handle);
            accelerationEditField = AddEditableVector2(motion, "Acceleration", snapshot.Acceleration, "Enter 또는 포커스 이동 시 가속도 변경을 예약합니다.",
                value => new SetElementAcceleration2D(value), handle);
            AddReadOnlyRow(motion, "Linear Damping", snapshot.LinearDamping.ToString("0.###"));
            inspectorScroll.Add(motion);

            Foldout physics = CreateSection("element.physics", "PhysicsCore", true);
            AddReadOnlyRow(physics, "Capability", snapshot.PhysicsCapabilities.ToString());
            AddReadOnlyRow(physics, "Shape", $"{snapshot.PhysicsShape} · Radius {snapshot.Radius:0.###}");
            AddReadOnlyRow(physics, "Category / Interaction",
                $"0x{snapshot.PhysicsCategoryMask:X16} / 0x{snapshot.InteractionLayerMask:X16}");
            AddReadOnlyRow(physics, "Trigger 포함", snapshot.IncludeTriggers.ToString());
            AddReadOnlyRow(physics, "Strict CCD",
                $"{snapshot.StrictCcdOverride} · Active {snapshot.StrictCcdActive} · Remaining {snapshot.StrictCcdRemainingSteps}");
            inspectorScroll.Add(physics);

            Foldout rendering = CreateSection("element.rendering", "Rendering", true);
            AddReadOnlyRow(rendering, "Visual Id", snapshot.VisualId.ToString());
            colorEditField = CreateCommitColorField(snapshot.Color, handle);
            rendering.Add(colorEditField);
            inspectorScroll.Add(rendering);

            if (world.TryGetDebugSnapshot(key, out ElementDebugSnapshot debugSnapshot))
            {
                if (debugSnapshot.HasLifetimeFeature)
                {
                    Foldout lifetime = CreateSection("element.lifetime", "Lifetime", true);
                    lifetimeEditField = new FloatField("Remaining")
                    {
                        value = debugSnapshot.Lifetime.RemainingUnits,
                        isDelayed = true,
                        tooltip = "이미 할당된 Lifetime 값만 수정합니다. 새 sparse feature를 추가하지 않습니다."
                    };
                    lifetimeEditField.RegisterValueChangedCallback(evt =>
                    {
                        var command = new SetElementRemainingLifetime(evt.newValue);
                        TrySubmitEdit(handle, in command, "Lifetime");
                    });
                    lifetime.Add(lifetimeEditField);
                    AddReadOnlyRow(lifetime, "Schedule", debugSnapshot.Lifetime.Schedule.ToString());
                    AddReadOnlyRow(lifetime, "Local Clock 사용", debugSnapshot.Lifetime.UseLocalClock.ToString());
                    inspectorScroll.Add(lifetime);
                }

                if (debugSnapshot.HasLocalClock)
                {
                    Foldout clock = CreateSection("element.local-clock", "Local Clock", true);
                    AddReadOnlyRow(clock, "Time Scale", debugSnapshot.LocalClock.TimeScale.ToString("0.###"));
                    AddReadOnlyRow(clock, "Paused", debugSnapshot.LocalClock.Paused.ToString());
                    inspectorScroll.Add(clock);
                }
            }

            if (world.TryGetDirectionalMotion(key, out ElementDirectionalMotionFeature directional))
            {
                Foldout feature = CreateSection("element.directional-motion", "Directional Motion", true);
                AddReadOnlyRow(feature, "Mode", directional.Mode.ToString());
                AddReadOnlyRow(feature, "Angular Speed", $"{directional.AngularSpeedRadiansPerSecond:0.###} rad/s");
                AddReadOnlyRow(feature, "Homing Point", directional.HomingPoint.ToString());
                AddReadOnlyRow(feature, "Max Turn", $"{directional.MaxTurnRadiansPerSecond:0.###} rad/s");
                inspectorScroll.Add(feature);
            }

            if (world.TryGetWaveMotion(key, out ElementWaveMotionFeature wave))
            {
                Foldout feature = CreateSection("element.wave-motion", "Wave Motion", true);
                AddReadOnlyRow(feature, "Amplitude", wave.Amplitude.ToString("0.###"));
                AddReadOnlyRow(feature, "Angular Frequency", $"{wave.AngularFrequencyRadiansPerSecond:0.###} rad/s");
                AddReadOnlyRow(feature, "Phase", $"{wave.PhaseRadians:0.###} rad");
                inspectorScroll.Add(feature);
            }

            if (world.TryGetVisualOrientation(key, out ElementVisualOrientationFeature orientation))
            {
                Foldout feature = CreateSection("element.visual-orientation", "Visual Orientation", true);
                AddReadOnlyRow(feature, "Mode", orientation.Mode.ToString());
                AddReadOnlyRow(feature, "Axis Offset", $"{orientation.AxisOffsetRadians:0.###} rad");
                AddReadOnlyRow(feature, "Spin", $"{orientation.SpinRadiansPerSecond:0.###} rad/s");
                inspectorScroll.Add(feature);
            }

            if (world.TryGetBoundary(key, out ElementBoundaryFeature boundary))
            {
                Foldout feature = CreateSection("element.boundary", "Boundary", true);
                AddReadOnlyRow(feature, "Mode", boundary.Mode.ToString());
                AddReadOnlyRow(feature, "Margin", boundary.Margin.ToString("0.###"));
                AddReadOnlyRow(feature, "진입 후 소거", boundary.RequireEnteredBeforeExit.ToString());
                inspectorScroll.Add(feature);
            }

            inspectorContainer = new IMGUIContainer(DrawSelectedProviderInspector)
            {
                name = "element-world-explorer-provider-inspector"
            };
            inspectorContainer.style.flexShrink = 0f;
            inspectorScroll.Add(inspectorContainer);

            Foldout facts = CreateSection("element.facts", "Recent Facts", false);
            AddFacts(facts, key);
            facts.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue) { RetainCaptureLease(world); }
            });
            inspectorScroll.Add(facts);

            Foldout advanced = CreateSection("element.advanced", "Advanced", false);
            if (world.TryGetDebugSnapshot(key, out debugSnapshot)) { AddAdvancedRows(advanced, in debugSnapshot); }
            inspectorScroll.Add(advanced);

            pendingEditStatusBox = CreateHelpBox(string.Empty, HelpBoxMessageType.Info);
            inspectorScroll.Add(pendingEditStatusBox);
            RefreshPendingEditStatus();
        }



        private void AddElementHeader(
            string displayName,
            ElementWorld world,
            ElementHandle handle,
            in ElementSnapshot snapshot)
        {
            ElementKey key = snapshot.Key;
            ElementLifecycle lifecycle = snapshot.Lifecycle;
            ElementLane lane = snapshot.Lane;
            AddHeader(displayName, $"{key} · {lifecycle} · World {world.WorldId} · {lane}");
            var actions = new Toolbar();
            actions.Add(new ToolbarButton(() => FrameSelected(SceneView.lastActiveSceneView, GetPresentationAlpha())) { text = "Frame" });
            actions.Add(new ToolbarButton(() => GUIUtility.systemCopyBuffer = key.ToString()) { text = "Copy Key" });
            actions.Add(new VisualElement { style = { flexGrow = 1f } });
            actions.Add(new ToolbarButton(() =>
            {
                if (!EditorUtility.DisplayDialog("Element Despawn", $"{displayName} ({key})의 despawn을 예약할까요?", "Despawn", "취소")) { return; }
                var command = new DespawnElement();
                TrySubmitEdit(handle, in command, "Despawn");
            }) { text = "Despawn" });
            inspectorScroll.Add(actions);
        }



        private void AddHeader(string title, string subtitle)
        {
            var titleLabel = new Label(title)
            {
                style =
                {
                    fontSize = 17f,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginTop = 8f,
                    marginLeft = 6f,
                    marginRight = 6f
                }
            };
            inspectorScroll.Add(titleLabel);
            var subtitleLabel = new Label(subtitle)
            {
                style =
                {
                    whiteSpace = WhiteSpace.Normal,
                    marginLeft = 6f,
                    marginRight = 6f,
                    marginBottom = 6f,
                    opacity = 0.72f
                }
            };
            inspectorScroll.Add(subtitleLabel);
        }



        private static Foldout CreateSection(
            string id,
            string title,
            bool expanded,
            string tooltip = null)
        {
            return new Foldout
            {
                text = title,
                value = expanded,
                tooltip = tooltip ?? string.Empty,
                viewDataKey = "Pan.HighDensityElement.Explorer.Section." + id,
                style = { marginLeft = 4f, marginRight = 4f, marginBottom = 3f }
            };
        }



        private static HelpBox CreateHelpBox(string text, HelpBoxMessageType type) => new HelpBox(text, type)
        {
            style = { marginLeft = 5f, marginRight = 5f, marginTop = 4f, marginBottom = 4f }
        };



        private static void AddReadOnlyRow(VisualElement parent, string label, string value, string tooltip = null)
        {
            var row = new VisualElement { tooltip = tooltip ?? string.Empty };
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 2f;
            var name = new Label(label) { style = { minWidth = 132f, opacity = 0.72f } };
            var content = new Label(value ?? string.Empty)
            {
                style = { flexGrow = 1f, whiteSpace = WhiteSpace.Normal }
            };
            row.Add(name);
            row.Add(content);
            parent.Add(row);
        }



        private Vector2Field AddEditableVector2<TCommand>(
            VisualElement parent,
            string label,
            float2 value,
            string tooltip,
            Func<float2, TCommand> createCommand,
            ElementHandle handle)
            where TCommand : unmanaged, IElementCommand
        {
            var field = new Vector2Field(label)
            {
                value = new Vector2(value.x, value.y),
                tooltip = tooltip
            };
            field.Query<FloatField>().ForEach(child => child.isDelayed = true);
            field.RegisterValueChangedCallback(evt =>
            {
                TCommand command = createCommand(new float2(evt.newValue.x, evt.newValue.y));
                TrySubmitEdit(handle, in command, label);
            });
            parent.Add(field);
            return field;
        }



        private ColorField CreateCommitColorField(Color value, ElementHandle handle)
        {
            var field = new ColorField("Color")
            {
                value = value,
                tooltip = "색상을 편집한 뒤 Enter 또는 포커스를 이동하면 변경을 예약합니다."
            };
            Color stagedValue = value;
            bool hasStagedValue = false;
            field.RegisterValueChangedCallback(evt =>
            {
                stagedValue = evt.newValue;
                hasStagedValue = true;
            });

            void Commit()
            {
                if (!hasStagedValue) { return; }
                hasStagedValue = false;
                var command = new SetElementColor((Color32)stagedValue);
                TrySubmitEdit(handle, in command, "Color");
            }

            field.RegisterCallback<FocusOutEvent>(_ => Commit());
            field.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter) { return; }
                Commit();
            });
            return field;
        }



        private void ResetElementInspectorBindings()
        {
            positionEditField = null;
            rotationEditField = null;
            scaleEditField = null;
            velocityEditField = null;
            accelerationEditField = null;
            colorEditField = null;
            lifetimeEditField = null;
            pendingEditStatusBox = null;
            boundElementWorldId = 0;
            boundElementKey = default;
        }



        private void RefreshInspectorValues()
        {
            if (windowSelection.Kind != DebuggerTreeItemKind.Element)
            {
                if (windowSelection.IsValid)
                {
                    inspectorDirty = true;
                    RefreshInspector();
                }
                return;
            }

            if (
                windowSelection.World == null ||
                windowSelection.World.IsDisposed ||
                windowSelection.World.WorldId != boundElementWorldId ||
                windowSelection.Key != boundElementKey ||
                !windowSelection.World.TryGetSnapshot(boundElementKey, out ElementSnapshot snapshot))
            {
                if (windowSelection.Kind == DebuggerTreeItemKind.Element)
                {
                    inspectorDirty = true;
                    RefreshInspector();
                }
                return;
            }

            SetFieldValueWhenIdle(positionEditField, new Vector2(snapshot.Position.x, snapshot.Position.y), "Position");
            SetFieldValueWhenIdle(rotationEditField, math.degrees(snapshot.RotationRadians), "Rotation");
            SetFieldValueWhenIdle(scaleEditField, new Vector2(snapshot.Scale.x, snapshot.Scale.y), "Scale");
            SetFieldValueWhenIdle(velocityEditField, new Vector2(snapshot.Velocity.x, snapshot.Velocity.y), "Velocity");
            SetFieldValueWhenIdle(
                accelerationEditField,
                new Vector2(snapshot.Acceleration.x, snapshot.Acceleration.y),
                "Acceleration");
            SetFieldValueWhenIdle(colorEditField, (Color)snapshot.Color, "Color");

            if (lifetimeEditField != null &&
                windowSelection.World.TryGetDebugSnapshot(boundElementKey, out ElementDebugSnapshot debugSnapshot) &&
                debugSnapshot.HasLifetimeFeature)
            {
                SetFieldValueWhenIdle(lifetimeEditField, debugSnapshot.Lifetime.RemainingUnits, "Lifetime");
            }

            RefreshPendingEditStatus();
        }



        private void RefreshPendingEditStatus()
        {
            if (pendingEditStatusBox == null) { return; }
            bool visible = pendingEditKey.IsValid &&
                pendingEditWorldId == boundElementWorldId &&
                pendingEditKey == boundElementKey &&
                !string.IsNullOrWhiteSpace(pendingEditLabel);
            pendingEditStatusBox.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible)
            {
                pendingEditStatusBox.text = $"{pendingEditLabel} 변경 예약됨 · 다음 안전한 command 경계에서 반영됩니다.";
            }
        }



        private void SetFieldValueWhenIdle<TValue>(BaseField<TValue> field, TValue value, string label)
        {
            if (field == null || IsFieldActivelyEditing(field) ||
                (pendingEditKey == boundElementKey && string.Equals(pendingEditLabel, label, StringComparison.Ordinal)))
            {
                return;
            }

            field.SetValueWithoutNotify(value);
        }



        private static bool IsFieldActivelyEditing(VisualElement field)
        {
            VisualElement focused = field?.panel?.focusController?.focusedElement as VisualElement;
            return focused != null && (ReferenceEquals(focused, field) || field.Contains(focused));
        }



        private static void AddAdvancedRows(VisualElement parent, in ElementDebugSnapshot snapshot)
        {
            ElementDebugStorageMetadata storage = snapshot.Storage;
            AddReadOnlyRow(parent, "Registry Slot", snapshot.Key.Slot.ToString());
            AddReadOnlyRow(parent, "Lane Dense Index", storage.LaneDenseIndex.ToString());
            AddReadOnlyRow(parent, "Pose Dense Index", storage.PoseDenseIndex.ToString());
            AddReadOnlyRow(parent, "Lifetime Sparse", storage.HasLifetimeFeature ? $"Dense {storage.LifetimeDenseIndex}" : "미할당");
            AddReadOnlyRow(parent, "LocalClock Sparse", storage.HasLocalClock ? $"Dense {storage.LocalClockDenseIndex}" : "미할당");
            AddReadOnlyRow(parent, "Physics Body", storage.HasPhysicsBody
                ? $"{storage.PhysicsBodyHandleIndex}:{storage.PhysicsBodyHandleGeneration}"
                : "미할당");
            AddReadOnlyRow(parent, "Physics Shape", storage.PhysicsShapeHandleIndex >= 0
                ? $"{storage.PhysicsShapeHandleIndex}:{storage.PhysicsShapeHandleGeneration}"
                : "미할당");
            AddReadOnlyRow(parent, "Structural Revision", storage.StructuralRevision.ToString("N0"));
        }



        private void AddFacts(VisualElement parent, ElementKey key)
        {
            int shown = 0;
            for (int i = recentFacts.Count - 1; i >= 0; i--)
            {
                ElementFact fact = recentFacts[i];
                if (fact.Element != key) { continue; }
                AddReadOnlyRow(parent,
                    $"{fact.FixedStepIndex}:{fact.SubstepIndex} {fact.Type}",
                    $"Target {fact.TargetId} · TOI {fact.TimeOfImpact:0.000}");
                shown++;
            }
            if (shown == 0) { parent.Add(new Label("캡처된 Fact가 없습니다.")); }
        }



        private static void AddLaneSummary(VisualElement parent, ElementWorld world, ElementLane lane)
        {
            if (!world.TryGetLaneStorageDiagnostics(lane, out ElementLaneStorageDiagnostics diagnostics)) { return; }
            AddReadOnlyRow(parent, GetLaneDisplayName(lane),
                $"{diagnostics.DenseCount:N0} / {diagnostics.DenseCapacity:N0}");
        }



        private static int GetLaneCount(ElementWorld world, ElementLane lane)
        {
            if (world == null || world.IsDisposed) { return 0; }
            return lane switch
            {
                ElementLane.QuerySprite2D => world.KinematicCount,
                ElementLane.AreaSensorSprite2D => world.AreaCount,
                ElementLane.DynamicBodySprite2D => world.PhysicsCoreCount,
                _ => 0
            };
        }



        private static string GetLaneDisplayName(ElementLane lane) => lane switch
        {
            ElementLane.QuerySprite2D => "Query Elements",
            ElementLane.AreaSensorSprite2D => "Area Sensors",
            ElementLane.DynamicBodySprite2D => "PhysicsCore Bodies",
            _ => lane.ToString()
        };



        private static string GetLaneDescription(ElementLane lane) => lane switch
        {
            ElementLane.QuerySprite2D =>
                "GameObject와 Physics Body 없이 Native 상태를 Burst Job으로 이동시키고 ShapeCast로 충돌을 확인하는 대량 Element Lane입니다.",
            ElementLane.AreaSensorSprite2D =>
                "지속 영역의 Enter·Stay·Exit pair를 관리하는 센서 Lane입니다.",
            ElementLane.DynamicBodySprite2D =>
                "실제 PhysicsCore Body가 필요한 Dynamic Element와 외부 query 대상 Element를 관리하는 Lane입니다.",
            _ => "ElementWorld 내부 실행 Lane입니다."
        };



        private static string GetLaneUpdateStage(ElementLane lane) => lane switch
        {
            ElementLane.QuerySprite2D => "Motion Job → bodyless ShapeCast → Fact",
            ElementLane.AreaSensorSprite2D => "Motion Job → pair cache → Enter/Stay/Exit",
            ElementLane.DynamicBodySprite2D => "PhysicsCore Simulate → body state readback → Fact",
            _ => "Unknown"
        };



        private static string GetLaneCapabilities(ElementLane lane) => lane switch
        {
            ElementLane.QuerySprite2D => "KinematicMotion2D, QuerySensor2D, SpriteVisual2D",
            ElementLane.AreaSensorSprite2D => "AreaSensor2D, SpriteVisual2D",
            ElementLane.DynamicBodySprite2D => "DynamicBody2D 또는 QueryTarget2D, SpriteVisual2D",
            _ => "None"
        };



    }
}
