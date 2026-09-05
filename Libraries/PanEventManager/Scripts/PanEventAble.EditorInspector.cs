#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;



namespace Pan.Event
{
    public partial class EventAble
    {
        private partial class EventValueTable
        {


            private const string EditorEventTableGroup = "이벤트 테이블 보기";
            private const string EditorAllNamespacesLabel = "전체 네임스페이스";
            private const string EditorPinPrefsPrefix = "Pan.EventManager.EventAbleInspector.Pin.";



            //. Odin 기본 목록의 페이징으로 한 번에 생성·표시되는 상세 drawer 수를 제한한다.
            private const int EditorEventValueItemsPerPage = 12;



            private enum EditorContentFilter
            {
                [LabelText("모든 항목")]
                All,

                [LabelText("상세 필드 있음")]
                WithDetails,

                [LabelText("상세 필드 없음")]
                WithoutDetails,

                [LabelText("고정 항목만")]
                PinnedOnly
            }



            private static readonly Dictionary<Type, bool> EditorMeaningfulContentCache = new Dictionary<Type, bool>();
            private static readonly Dictionary<string, bool> EditorPinCache = new Dictionary<string, bool>(StringComparer.Ordinal);
            private static int editorGlobalPinRevision;



            //? 실제 테이블은 즉시 바뀌어도 Odin이 그리는 배열은 현재 Repaint가 끝난 뒤 한 번만 교체한다.
            private bool editorEventValueTableCacheDirty = true;
            private int editorObservedPinRevision;
            [ShowInInspector]
            [EnableGUI]
            [HideReferenceObjectPicker]
            [Searchable(Recursive = false, FilterOptions = SearchFilterOptions.ISearchFilterableInterface)]
            [ListDrawerSettings(
                IsReadOnly = false,
                DraggableItems = false,
                HideAddButton = true,
                HideRemoveButton = true,
                ShowPaging = true,
                ShowFoldout = false,
                NumberOfItemsPerPage = EditorEventValueItemsPerPage)]
            [FoldoutGroup(EditorEventTableGroup)]
            [LabelText("전체 보기")]
            [PropertyOrder(4)]
            [PropertySpace(4, 4)]
            private PanBaseEventValueStatus[] EventValueTablesList = Array.Empty<PanBaseEventValueStatus>();
            private string[] editorNamespaceOptions = { EditorAllNamespacesLabel };
            private readonly List<PanBaseEventValue> editorPendingRemovals = new List<PanBaseEventValue>();
            private readonly Dictionary<string, bool> editorPendingPinChanges = new Dictionary<string, bool>(StringComparer.Ordinal);
            private bool editorRowActionsEnabled = true;
            private bool editorRowActionsVisible = true;



            [ShowInInspector]
            [FoldoutGroup(EditorEventTableGroup, Expanded = true)]
            [LabelText("Play 갱신")]
            [OnValueChanged(nameof(Editor_RequestInspectorRepaint))]
            [PropertyOrder(0)]
            private EventAbleInspectorRefreshMode editorRefreshMode;



            [ShowInInspector]
            [FoldoutGroup(EditorEventTableGroup)]
            [LabelText("내용")]
            [OnValueChanged(nameof(Editor_MarkEventValueViewDirty))]
            [PropertyOrder(1)]
            private EditorContentFilter editorContentFilter;



            [ShowInInspector]
            [FoldoutGroup(EditorEventTableGroup)]
            [ValueDropdown(nameof(EditorNamespaceOptions))]
            [LabelText("네임스페이스")]
            [OnValueChanged(nameof(Editor_MarkEventValueViewDirty))]
            [PropertyOrder(2)]
            private string editorNamespaceFilter = EditorAllNamespacesLabel;



            [HideLabel]
            [InlineProperty]
            private sealed class PanBaseEventValueStatus : ISearchFilterable
            {
                public PanBaseEventValueStatus(EventValueTable owner, PanBaseEventValue panBaseEventValue)
                {
                    Owner = owner;
                    Type type = panBaseEventValue?.GetType();
                    TypeName = type?.Name ?? "(EventValue 참조 없음)";
                    TypeFullName = type?.FullName ?? "EventValue 참조가 없습니다.";
                    Namespace = type?.Namespace ?? "(전역 네임스페이스)";
                    PinKey = type == null ? TypeFullName : Editor_GetTypePinKey(type);
                    ActiveTypeLabel = $"<color=#4fc1e9><b>{TypeName}</b></color>";
                    InactiveTypeLabel = $"<color=#ed5565><b>{TypeName}</b></color>";
                    HasDetails = type != null && Editor_HasMeaningfulInspectorContent(type);
                    PanBaseEventValue = panBaseEventValue;
                    Editor_EventValue = panBaseEventValue;
                }


                [HideInInspector]
                private readonly EventValueTable Owner;



                [HideInInspector]
                internal readonly string TypeName;



                [HideInInspector]
                internal readonly string TypeFullName;



                [HideInInspector]
                internal readonly string Namespace;



                [HideInInspector]
                internal readonly string PinKey;



                [HideInInspector]
                internal readonly bool HasDetails;



                [HideInInspector]
                private readonly string ActiveTypeLabel;



                [HideInInspector]
                private readonly string InactiveTypeLabel;



                [HideInInspector]
                private bool EditorPinned => Owner != null && Editor_IsPinned(PinKey);



                [HideInInspector]
                private bool EditorRowActionsDisabled => Owner == null || !Owner.editorRowActionsEnabled;



                [HideInInspector]
                private bool EditorRowActionsVisible => Owner != null && Owner.editorRowActionsVisible;



                [HideInInspector]
                private bool EditorShowPinButton => EditorRowActionsVisible && !EditorPinned;



                [HideInInspector]
                private bool EditorShowUnpinButton => EditorRowActionsVisible && EditorPinned;



                /// <summary>
                /// Odin 비재귀 검색이 EventValue의 하위 property를 순회하지 않고 타입 경로만 비교하도록 합니다.
                /// </summary>
                public override string ToString()
                {
                    return TypeFullName;
                }



                /// <summary>
                /// EventValue의 상세 property를 검색하지 않고 타입 정보만 비교합니다.
                /// </summary>
                /// <param name="searchString">Odin 검색창에 입력된 문자열입니다.</param>
                public bool IsMatch(string searchString)
                {
                    if (string.IsNullOrWhiteSpace(searchString)) { return true; }

                    return TypeName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        TypeFullName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        Namespace.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0;
                }



                [HorizontalGroup("행", Width = 24f)]
                [ShowIf(nameof(EditorShowPinButton))]
                [DisableIf(nameof(EditorRowActionsDisabled))]
                [Button("", Icon = SdfIconType.Star, Stretch = false, ButtonAlignment = 0.5f)]
                [PropertyOrder(0)]
                private void Editor_Pin()
                {
                    Owner?.Editor_RequestPinned(PinKey, true);
                }



                [HorizontalGroup("행", Width = 24f)]
                [ShowIf(nameof(EditorShowUnpinButton))]
                [DisableIf(nameof(EditorRowActionsDisabled))]
                [Button("", Icon = SdfIconType.StarFill, Stretch = false, ButtonAlignment = 0.5f)]
                [GUIColor(0.97f, 0.85f, 0.39f)]
                [PropertyOrder(0)]
                private void Editor_Unpin()
                {
                    Owner?.Editor_RequestPinned(PinKey, false);
                }



                [ShowInInspector]
                [HorizontalGroup("행")]
                [HideLabel]
                [EnableGUI]
                [DisplayAsString(EnableRichText = true)]
                [PropertyOrder(1)]
                private string Editor_TypeLabel =>
                    PanBaseEventValue != null && PanBaseEventValue.Valid_CurrentEventAble ?
                        ActiveTypeLabel :
                        InactiveTypeLabel;



                [HorizontalGroup("행", Width = 56f)]
                [ShowIf(nameof(EditorRowActionsVisible))]
                [DisableIf(nameof(EditorRowActionsDisabled))]
                [Button("해제", Icon = SdfIconType.ExclamationSquareFill, Stretch = false, ButtonAlignment = 1f)]
                [GUIColor(0.93f, 0.33f, 0.40f)]
                [PropertyOrder(2)]
                private void Editor_Remove()
                {
                    Owner?.Editor_RequestRemove(PanBaseEventValue);
                }



                [HideInInspector]
                internal readonly PanBaseEventValue PanBaseEventValue;



                [ShowInInspector]
                [ShowIf(nameof(HasDetails))]
                [HideLabel]
                [HideReferenceObjectPicker]
                [HideDuplicateReferenceBox]
                [InlineProperty]
                [EnableGUI]
                [PropertyOrder(3)]
                private PanBaseEventValue Editor_EventValue;
            }


            [ShowInInspector]
            [FoldoutGroup(EditorEventTableGroup)]
            [DisplayAsString(EnableRichText = true)]
            [LabelText("테이블")]
            [PropertyOrder(3)]
            private string EditorTableSummary =>
                $"보유 <color=#4fc1e9><b>{EventValueTables?.Count ?? 0}</b></color> / " +
                $"표시 <color=#2ecc71><b>{EventValueTablesList.Length}</b></color> / " +
                $"초기 용량 <color=#f7da64><b>{EventValueTablesInitializeCapacity}</b></color>";



            /// <summary>
            /// 현재 테이블과 필터를 기준으로 Odin 목록에 전달할 새 불변 배열을 구성합니다.
            /// </summary>
            internal void Editor_Refresh_EventValueTablesList()
            {
                var allRows = new List<PanBaseEventValueStatus>(EventValueTables?.Count ?? 0);
                var namespaces = new HashSet<string>(StringComparer.Ordinal);

                if (EventValueTables != null)
                {
                    foreach (PanBaseEventValue eventValue in EventValueTables.Values)
                    {
                        var row = new PanBaseEventValueStatus(this, eventValue);
                        allRows.Add(row);
                        namespaces.Add(row.Namespace);
                    }
                }

                //. 사라진 네임스페이스 선택값을 먼저 정규화해야 같은 pass에서 전체 항목을 다시 표시할 수 있다.
                Editor_RebuildNamespaceOptions(namespaces);

                var nextRows = new List<PanBaseEventValueStatus>(allRows.Count);

                for (int i = 0; i < allRows.Count; i++)
                {
                    PanBaseEventValueStatus row = allRows[i];
                    if (!Editor_MatchesContentFilter(row)) { continue; }
                    if (!string.Equals(editorNamespaceFilter, EditorAllNamespacesLabel, StringComparison.Ordinal) &&
                        !string.Equals(editorNamespaceFilter, row.Namespace, StringComparison.Ordinal)) { continue; }

                    nextRows.Add(row);
                }

                nextRows.Sort(Editor_CompareEventValueStatus);
                EventValueTablesList = nextRows.ToArray();
                editorObservedPinRevision = editorGlobalPinRevision;
                editorEventValueTableCacheDirty = false;
            }



            /// <summary>
            /// Odin ValueDropdown이 재사용할 네임스페이스 선택지입니다.
            /// </summary>
            private IEnumerable<string> EditorNamespaceOptions => editorNamespaceOptions;



            private void Editor_RebuildNamespaceOptions(HashSet<string> namespaces)
            {
                var nextOptions = new List<string>(namespaces.Count + 1) { EditorAllNamespacesLabel };
                nextOptions.AddRange(namespaces);
                nextOptions.Sort(1, nextOptions.Count - 1, StringComparer.Ordinal);
                editorNamespaceOptions = nextOptions.ToArray();

                if (Array.IndexOf(editorNamespaceOptions, editorNamespaceFilter) < 0)
                {
                    editorNamespaceFilter = EditorAllNamespacesLabel;
                }
            }



            private bool Editor_MatchesContentFilter(PanBaseEventValueStatus row)
            {
                switch (editorContentFilter)
                {
                    case EditorContentFilter.WithDetails:
                        return row.HasDetails;

                    case EditorContentFilter.WithoutDetails:
                        return !row.HasDetails;

                    case EditorContentFilter.PinnedOnly:
                        return Editor_IsPinned(row.PinKey);

                    default:
                        return true;
                }
            }



            private static int Editor_CompareEventValueStatus(PanBaseEventValueStatus left, PanBaseEventValueStatus right)
            {
                bool leftPinned = Editor_IsPinned(left.PinKey);
                bool rightPinned = Editor_IsPinned(right.PinKey);
                if (leftPinned != rightPinned) { return leftPinned ? -1 : 1; }

                int shortNameComparison = string.CompareOrdinal(left.TypeName, right.TypeName);
                return shortNameComparison != 0 ? shortNameComparison : string.CompareOrdinal(left.TypeFullName, right.TypeFullName);
            }



            private void Editor_RequestPinned(string pinKey, bool pinned)
            {
                editorPendingPinChanges[pinKey] = pinned;
                Editor_RequestInspectorRepaint();
            }



            /// <summary>
            /// 행의 ShowIf 조건이 현재 GUI 이벤트 도중 바뀌지 않도록 고정 상태를 Repaint 종료 뒤에 적용합니다.
            /// </summary>
            private void Editor_ApplyPendingPinChanges()
            {
                if (editorPendingPinChanges.Count == 0) { return; }

                foreach (KeyValuePair<string, bool> pendingChange in editorPendingPinChanges)
                {
                    EditorPinCache[pendingChange.Key] = pendingChange.Value;
                    UnityEditor.EditorPrefs.SetBool(EditorPinPrefsPrefix + pendingChange.Key, pendingChange.Value);
                }

                editorPendingPinChanges.Clear();

                unchecked
                {
                    editorGlobalPinRevision++;
                }

                editorEventValueTableCacheDirty = true;
                Main.Editor_NotifyInspectorPinSettingsChanged();
            }



            private static bool Editor_IsPinned(string pinKey)
            {
                if (EditorPinCache.TryGetValue(pinKey, out bool pinned)) { return pinned; }

                pinned = UnityEditor.EditorPrefs.GetBool(EditorPinPrefsPrefix + pinKey, false);
                EditorPinCache.Add(pinKey, pinned);
                return pinned;
            }



            private static string Editor_GetTypePinKey(Type eventValueType)
            {
                return $"{eventValueType.Assembly.GetName().Name}|{eventValueType.FullName ?? eventValueType.Name}";
            }



            /// <summary>
            /// 행의 제거 버튼 요청을 현재 Repaint가 모두 끝날 때까지 보류합니다.
            /// </summary>
            private void Editor_RequestRemove(PanBaseEventValue eventValue)
            {
                if (!Editor_ContainsCurrentEventValue(eventValue)) { return; }
                if (!editorPendingRemovals.Contains(eventValue)) { editorPendingRemovals.Add(eventValue); }

                Editor_RequestInspectorRepaint();
            }



            private void Editor_ApplyPendingRemovals()
            {
                for (int i = 0; i < editorPendingRemovals.Count; i++)
                {
                    PanBaseEventValue eventValue = editorPendingRemovals[i];
                    if (!Editor_ContainsCurrentEventValue(eventValue)) { continue; }

                    try
                    {
                        Main.RemoveValue(eventValue);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }

                editorPendingRemovals.Clear();
            }



            private bool Editor_ContainsCurrentEventValue(PanBaseEventValue eventValue)
            {
                if (eventValue == null) { return false; }

                return EventValueTables != null &&
                    EventValueTables.TryGetValue(eventValue.GetType(), out PanBaseEventValue currentEventValue) &&
                    ReferenceEquals(currentEventValue, eventValue);
            }



            private void Editor_MarkEventValueViewDirty()
            {
                editorEventValueTableCacheDirty = true;
                Editor_RequestInspectorRepaint();
            }



            private void Editor_RequestInspectorRepaint()
            {
                Sirenix.Utilities.Editor.GUIHelper.RequestRepaint();
            }



            /// <summary>
            /// 에디터 표시 목록을 다음 안전한 Repaint 종료 시점에 재생성하도록 표시합니다.
            /// </summary>
            private void Editor_MarkEventValueTableCacheDirty()
            {
                editorEventValueTableCacheDirty = true;
                Main.Editor_IncrementEventValueChangeVersion();
            }



            private static bool Editor_HasMeaningfulInspectorContent(Type eventValueType)
            {
                if (EditorMeaningfulContentCache.TryGetValue(eventValueType, out bool cachedResult)) { return cachedResult; }

                bool hasMeaningfulContent = false;

                for (Type currentType = eventValueType;
                     currentType != null && currentType != typeof(PanBaseEventValue);
                     currentType = currentType.BaseType)
                {
                    if (Editor_IsPanEventFrameworkType(currentType)) { continue; }

                    if (Editor_HasVisibleDeclaredField(currentType) ||
                        Editor_HasVisibleDeclaredProperty(currentType) ||
                        Editor_HasVisibleDeclaredMethod(currentType))
                    {
                        hasMeaningfulContent = true;
                        break;
                    }
                }

                EditorMeaningfulContentCache.Add(eventValueType, hasMeaningfulContent);
                return hasMeaningfulContent;
            }



            private static bool Editor_HasVisibleDeclaredField(Type declaringType)
            {
                System.Reflection.FieldInfo[] fields = declaringType.GetFields(
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.DeclaredOnly);

                for (int i = 0; i < fields.Length; i++)
                {
                    System.Reflection.FieldInfo field = fields[i];
                    bool explicitlyShown = Editor_HasAttribute(field, "ShowInInspectorAttribute");
                    if (Editor_HasAttribute(field, "HideInInspector") && !explicitlyShown) { continue; }
                    if (field.IsNotSerialized && !explicitlyShown) { continue; }
                    if (field.Name.IndexOf("k__BackingField", StringComparison.Ordinal) >= 0 && !explicitlyShown) { continue; }

                    if (field.IsPublic ||
                        explicitlyShown ||
                        Editor_HasAttribute(field, "SerializeField") ||
                        Editor_HasAttribute(field, "SerializeReference") ||
                        Editor_HasAttribute(field, "OdinSerializeAttribute"))
                    {
                        return true;
                    }
                }

                return false;
            }



            private static bool Editor_HasVisibleDeclaredProperty(Type declaringType)
            {
                System.Reflection.PropertyInfo[] properties = declaringType.GetProperties(
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.DeclaredOnly);

                for (int i = 0; i < properties.Length; i++)
                {
                    System.Reflection.PropertyInfo property = properties[i];
                    bool explicitlyShown = Editor_HasAttribute(property, "ShowInInspectorAttribute");
                    if (Editor_HasAttribute(property, "HideInInspector") && !explicitlyShown) { continue; }
                    if (property.GetIndexParameters().Length > 0) { continue; }

                    if (explicitlyShown || Editor_HasAttribute(property, "OdinSerializeAttribute")) { return true; }
                }

                return false;
            }



            private static bool Editor_HasVisibleDeclaredMethod(Type declaringType)
            {
                System.Reflection.MethodInfo[] methods = declaringType.GetMethods(
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.DeclaredOnly);

                for (int i = 0; i < methods.Length; i++)
                {
                    System.Reflection.MethodInfo method = methods[i];
                    if (Editor_HasAttribute(method, "ButtonAttribute") ||
                        Editor_HasAttribute(method, "OnInspectorGUIAttribute"))
                    {
                        return true;
                    }
                }

                return false;
            }



            private static bool Editor_HasAttribute(System.Reflection.MemberInfo member, string attributeTypeName)
            {
                object[] attributes = member.GetCustomAttributes(false);

                for (int i = 0; i < attributes.Length; i++)
                {
                    Type attributeType = attributes[i].GetType();
                    if (string.Equals(attributeType.Name, attributeTypeName, StringComparison.Ordinal) ||
                        string.Equals(attributeType.FullName, attributeTypeName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                return false;
            }



            private static bool Editor_IsPanEventFrameworkType(Type declaringType)
            {
                return declaringType == typeof(PanBaseEventValue) || declaringType.DeclaringType == typeof(PanBaseEventValue);
            }


        }


        [ValueDropdown(nameof(Editor_GetAllEventValueTypes))]
        [ShowInInspector, OnValueChanged(nameof(Editor_OnValueChanged_Test_SelectEventValue))]
        [ShowIf(nameof(Editor_ShowLegacyAddMenu))]
        [Title("🧪 실험")]
        //? 이벤트테이블에 특정 EventValue를 강제 추가 를 고르는 드롭다운
        private Type Editor_Test_SelectEventValue;



        private Type[] Editor_GetAllEventValueTypes()
        {
            return PanEventsInitializeSettingSbjectBase.GetPanBaseEventValueTypesAll().Where(x =>
            {
                if (EventValueTables == null) { return true; }
                return !(EventValueTables.ContainsEventValue(false, x)); //! 이벤트 테이블에 존재하지않는 EventValue들만 드롭다운으로 표시
            }).ToArray();
        }



        //? 이벤트테이블에 특정 EventValue를 강제 추가
        private void Editor_OnValueChanged_Test_SelectEventValue()
        {
            if (Editor_Test_SelectEventValue != null)
            {
                //! 이미 이벤트 테이블에존재하면 return
                if (EventValueTables.ContainsEventValue(false, Editor_Test_SelectEventValue)) { return; }


                var eventValue = EventValueManager.PopEventValue(Editor_Test_SelectEventValue);

                Debug.Log($"<b>{Editor_Test_SelectEventValue.Name} 을 이벤트테이블에 강제 추가(장착, 활성화) 시도... {eventValue != null} </b>");

                if (eventValue != null)
                {
                    if (eventValue is PanBaseEventValue.IEnableValueNonCasting eventValueNonCasting)
                    {
                        if (eventValueNonCasting.GetCurrentType == typeof(IEventAble) || Master.GetType() == eventValueNonCasting.GetCurrentType)
                        {
                            eventValueNonCasting.EnableValue(Master);
                            EventValueTables.AddEventTable(eventValue.GetType(), eventValue);
                        }
                        else
                        {
                            Debug.LogWarning($"<b>실패, 마스터의 타입 ({Master.GetType()}) 와 {eventValue.GetType()}의 \"대상\" 타겟 타입이 비교에 실패({eventValueNonCasting.GetCurrentType})</b>");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"<b>실패, {eventValue.GetType().Name} 을 {nameof(PanBaseEventValue.IEnableValueNonCasting)} 로 캐스팅 하는데 실패함</b>");
                    }
                }
                else
                {
                    Debug.LogWarning($"<b>실패, {Editor_Test_SelectEventValue.GetType().Name} 을 얻어오는데 실패함</b>");
                }

                Editor_Test_SelectEventValue = null;
            }
        }

    }
}
#endif
