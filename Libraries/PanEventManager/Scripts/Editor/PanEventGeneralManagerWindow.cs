using Pan.Util;
using Pan.Util.Editors;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEditor.EditorApplication;



namespace Pan.Event.Editor
{
    //? PanBaseEvent의 요소를 이쁘게 드로잉하기위한 구조체
    [Serializable]
    public struct PanBaseEventStatus
    {
        public PanBaseEventStatus(Type type, PanBaseEvent panBaseEvent)
        {
            Type = type;
            PanBaseEvent = panBaseEvent;
        }



        private bool PanBaseEventIsNotNull => PanBaseEvent != null;
        private bool PanBaseEventIsNull => PanBaseEvent == null;



        private Color Editor_GetColor
        {
            get
            {
                if (PanBaseEvent == null)
                {
                    return Color.red;
                }
                return Color.green;
            }
        }



        [HideInInspector]
        public Type Type;



        //[ShowInInspector]
        //[GUIColor(nameof(Editor_GetColor))]
        //[HorizontalGroup("가로그룹", Width = 0.2f)]
        //[HideLabel]
        //[DisplayAsString]
        //[PropertyOrder(0)]
        public readonly string TypeName => Type.Name;



        public readonly string TypeName_IsEnabled => $"{Type.Name}\n(Enabled)";
        public readonly string TypeName_IsDisabled => $"{Type.Name}\n(Disabled)";



        [ShowInInspector]
        [HorizontalGroup("가로그룹", Width = 0.2f)]
        [Button("@TypeName_IsDisabled", ButtonSizes.Large), GUIColor(0.93f, 0.33f, 0.40f)]
        [PropertyOrder(0)]
        [ShowIf(nameof(PanBaseEventIsNull))]
        private void Editor_ExecuteCaching()
        {
            PanEventGeneralManager.EventManager.GetEvent(Type);
        }


        [ShowInInspector]
        [HorizontalGroup("가로그룹", Width = 0.2f)]
        [Button("@TypeName_IsEnabled", ButtonSizes.Large), GUIColor(0.31f, 0.76f, 0.91f)]
        [PropertyOrder(0)]
        [ShowIf(nameof(PanBaseEventIsNotNull))]
        private void Editor_ExecuteRelease()
        {
            PanEventGeneralManager.EventManager.ReleaseEvent(Type);
        }




        [ShowInInspector]
        [HideReferenceObjectPicker]
        [HorizontalGroup("가로그룹")]
        [HideLabel]
        [ShowIf(nameof(PanBaseEventIsNotNull))]
        [PropertyOrder(1)]
        public PanBaseEvent PanBaseEvent;




        //[TitleGroup("테스트")]
        //[ButtonGroup("테스트/버튼그룹")]
        //[Button("캐싱 해보기", Icon = SdfIconType.ExclamationSquareFill), GUIColor(0.31f, 0.76f, 0.91f)]
        //[ShowIf(nameof(PanBaseEventIsNull))]
        //private void Editor_ExecuteCaching()
        //{
        //    PanEventGeneralManager.EventManager.GetEvent(Type);
        //}


        //[TitleGroup("테스트")]
        //[ButtonGroup("테스트/버튼그룹")]
        //[Button("해제 해보기", Icon = SdfIconType.ExclamationSquareFill), GUIColor(0.93f, 0.33f, 0.40f)]
        //[ShowIf(nameof(PanBaseEventIsNotNull))]
        //private void Editor_ExecuteRelease()
        //{
        //    PanEventGeneralManager.EventManager.ReleaseEvent(Type);
        //}
    }



    //? PanBaseEventValue의 요소를 이쁘게 드로잉하기위한 구조체
    [Serializable]
    public struct PanBaseEventValueStatus
    {
        ///======================================================================================================================================================



        public PanBaseEventValueStatus(Type type, InfinityStack_ClassInstance<PanBaseEventValue> panBaseEventValueInfinityStack)
        {
            Type = type;
            PanBaseEventValueInfinityStack = panBaseEventValueInfinityStack;
            drawTitleField = "";
            dummyField = "";
        }



        ///======================================================================================================================================================



        private readonly string TypeName => Type.Name;
        private readonly string TypeFullName => Type.FullName;



        [ShowInInspector]
        [HideLabel]
        [TitleGroup("@TypeName", Subtitle = "@TypeFullName", Alignment = TitleAlignments.Split, HorizontalLine = false, BoldTitle = true)]
        [DisplayAsString]
        [PropertyOrder(0)]
        [PropertySpace(-8, -8)]
        private readonly string drawTitleField;



        private readonly bool PanBaseEventIsNotNull => PanBaseEventValueInfinityStack != null;
        private readonly bool PanBaseEventIsNotNull_And_CreatedElementsCountIsNotZero => PanBaseEventIsNotNull && PanBaseEventValueInfinityStack.CreatedElementsCount_Accumulate != 0;
        private readonly bool PanBaseEventIsNotNull_And_CreatedElementsCountIsZero => PanBaseEventIsNotNull && PanBaseEventValueInfinityStack.CreatedElementsCount_Accumulate == 0;
        private readonly bool PanBaseEventIsNotNull_And_PoolCountIsNotZero => PanBaseEventIsNotNull && PanBaseEventValueInfinityStack.PoolCount != 0;
        private readonly bool PanBaseEventIsNotNull_And_PoolCountIsZero => PanBaseEventIsNotNull && PanBaseEventValueInfinityStack.PoolCount == 0;
        private readonly bool PanBaseEventIsNotNull_And_InUseIsNotZero => PanBaseEventIsNotNull && PanBaseEventValueInfinityStack.InUseCount != 0;
        private readonly bool PanBaseEventIsNotNull_And_InUseIsZero => PanBaseEventIsNotNull && PanBaseEventValueInfinityStack.InUseCount == 0;



        ///======================================================================================================================================================



        //! "이벤트 밸류 무한 스택" 탭



        [TabGroup("이벤트밸류탭그룹", "이벤트 밸류 무한 스택", Icon = SdfIconType.Magic, TextColor = "blue")]
        [ShowInInspector, DisplayAsString]
        [HideLabel]
        [PropertySpace(-8, -8)]
        private string dummyField;



        [ShowInInspector, ShowIf(nameof(PanBaseEventIsNotNull))]
        [HideLabel, DisplayAsString(EnableRichText = true), EnableGUI]
        [TabGroup("이벤트밸류탭그룹", "이벤트 밸류 무한 스택")]
        private readonly string CreatedEventValues
        {
            get
            {
                if (PanBaseEventIsNotNull_And_CreatedElementsCountIsNotZero)
                {
                    //string colorTag_Count = (PanBaseEventValueInfinityStack.CreatedElementsCount_Accumulate >= PanBaseEventValueInfinityStack.MaxElementCount) ? "<color=red>" : "<color=#4fc1e9>";
                    var result = $"<b>누적된 총 생성/해제 </b><color=#f7da64>{TypeName}</color></b> 개수: <size=15><b><color=#4fc1e9>{PanBaseEventValueInfinityStack.CreatedElementsCount_Accumulate}</color> / <color=#ed5565>{PanBaseEventValueInfinityStack.ReleasedElementsCount_Accumulate}</b></size></color>";
                    return result;
                }

                return $"<b><color=#f7da64>{TypeName}</color></b> 가 생성 되지 않음";
            }
        }



        [ShowInInspector, ShowIf(nameof(PanBaseEventIsNotNull))]
        [HideLabel, DisplayAsString(EnableRichText = true), EnableGUI]
        [TabGroup("이벤트밸류탭그룹", "이벤트 밸류 무한 스택")]
        private readonly string CurrentEventValuesCount
        {
            get
            {
                if (PanBaseEventIsNotNull_And_CreatedElementsCountIsNotZero)
                {
                    string colorTag_Count = (PanBaseEventValueInfinityStack.CreatedElementsCount_Accumulate >= PanBaseEventValueInfinityStack.MaxElementCount) ? "<color=red>" : "<color=#2ecc71>";
                    var result = $"<b>가용중인</b> 총 <b><color=#f7da64>{TypeName}</color></b> 개수: <size=15><b>{colorTag_Count}{PanBaseEventValueInfinityStack.CurrentElementsCount}</color></b></size> / <color=#ac92ec>{((PanBaseEventValueInfinityStack.MaxElementCount >= int.MaxValue) ? "∞" : PanBaseEventValueInfinityStack.MaxElementCount)}</color></b></size>";
                    return result;
                }
                return "";
            }
        }



        //? 이벤트 밸류 풀 스택 표시



        [ShowInInspector, ShowIf(nameof(PanBaseEventIsNotNull_And_CreatedElementsCountIsNotZero))]
        [TabGroup("이벤트밸류탭그룹", "이벤트 밸류 무한 스택"), HorizontalGroup("이벤트밸류탭그룹/이벤트 밸류 무한 스택/가로그룹"), TitleGroup("이벤트밸류탭그룹/이벤트 밸류 무한 스택/가로그룹/이벤트 밸류 풀 스택")]
        [HideLabel, DisplayAsString(EnableRichText = true), EnableGUI]
        private readonly string Editor_PoolStackCount
        {
            get
            {
                string result;
                if (PanBaseEventValueInfinityStack.PoolCount > 0)
                {
                    result = $"풀에 저장된 개수: <color=#ed5565><b><size=15>{PanBaseEventValueInfinityStack.PoolCount}</size></b></color>";
                }
                else
                {
                    result = $"풀에 저장된 개수: <color=red><b>X</b></color>";
                }

                return result;
            }
        }



        [ShowInInspector, ShowIf(nameof(PanBaseEventIsNotNull_And_PoolCountIsNotZero))]
        [TabGroup("이벤트밸류탭그룹", "이벤트 밸류 무한 스택"), HorizontalGroup("이벤트밸류탭그룹/이벤트 밸류 무한 스택/가로그룹"), TitleGroup("이벤트밸류탭그룹/이벤트 밸류 무한 스택/가로그룹/이벤트 밸류 풀 스택"), FoldoutGroup("이벤트밸류탭그룹/이벤트 밸류 무한 스택/가로그룹/이벤트 밸류 풀 스택/대여 이벤트 밸류"), LabelText("이벤트 밸류 풀 스택")]
        [ListDrawerSettings(DraggableItems = false, ShowFoldout = false, HideAddButton = true, HideRemoveButton = true), HideReferenceObjectPicker]
        private readonly List<PanBaseEventValue> Editor_GetPoolStackConvertedList
        {
            get { return PanBaseEventValueInfinityStack.editorCachedPoolStack; }
            set { PanBaseEventValueInfinityStack.editorCachedPoolStack = value; }
        }



        //? 대여 이벤트 밸류 표시



        [ShowInInspector, ShowIf(nameof(PanBaseEventIsNotNull_And_CreatedElementsCountIsNotZero))]
        [TabGroup("이벤트밸류탭그룹", "이벤트 밸류 무한 스택"), HorizontalGroup("이벤트밸류탭그룹/이벤트 밸류 무한 스택/가로그룹"), TitleGroup("이벤트밸류탭그룹/이벤트 밸류 무한 스택/가로그룹/대여 이벤트 밸류")]
        [HideLabel, DisplayAsString(EnableRichText = true), EnableGUI]
        private readonly string Editor_InUseCount
        {
            get
            {
                string result;
                if (PanBaseEventValueInfinityStack.InUseCount > 0)
                {
                    result = $"대여 중인 개수: <color=#4fc1e9><b><size=15>{PanBaseEventValueInfinityStack.InUseCount}</size></b></color>";
                }
                else
                {
                    result = $"대여 중인 개수: <color=red><b>X</b></color>";
                }

                return result;
            }
        }



        [ShowInInspector, ShowIf(nameof(PanBaseEventIsNotNull_And_InUseIsNotZero))]
        [TabGroup("이벤트밸류탭그룹", "이벤트 밸류 무한 스택"), HorizontalGroup("이벤트밸류탭그룹/이벤트 밸류 무한 스택/가로그룹"), TitleGroup("이벤트밸류탭그룹/이벤트 밸류 무한 스택/가로그룹/대여 이벤트 밸류"), FoldoutGroup("이벤트밸류탭그룹/이벤트 밸류 무한 스택/가로그룹/대여 이벤트 밸류/대여 이벤트 밸류"), LabelText("대여 이벤트 밸류")]
        [ListDrawerSettings(DraggableItems = false, ShowFoldout = false, HideAddButton = true, HideRemoveButton = true), HideReferenceObjectPicker]
        private readonly List<PanBaseEventValue> Editor_InUseList
        {
            get { return PanBaseEventValueInfinityStack.editorCache_InUseList; }
            set { PanBaseEventValueInfinityStack.editorCache_InUseList = value; }
        }



        ///======================================================================================================================================================



        //! "무한스택 직접 보기" 탭



        [ShowInInspector]
        [HideReferenceObjectPicker]
        [TabGroup("이벤트밸류탭그룹", "무한 스택 직접 보기", Icon = SdfIconType.Gear, TextColor = "purple")]
        [HideLabel]
        [ShowIf(nameof(PanBaseEventIsNotNull))]
        private InfinityStack_ClassInstance<PanBaseEventValue> PanBaseEventValueInfinityStack;



        ///======================================================================================================================================================




        [TitleGroup("요소 테스트")]
        [ButtonGroup("요소 테스트/버튼그룹")]
        [Button("캐싱 해보기 (Create 호출)", Icon = SdfIconType.ExclamationSquareFill), GUIColor(0.93f, 0.33f, 0.40f)]
        private void Editor_ExecuteCaching()
        {
            var infinityStack = PanBaseEventValueInfinityStack; //. 지역 변수로 복사
            EditorNumberInputPopup.Show("캐싱 개수 입력", count =>
            {
                infinityStack.Create(count);
            }, 1);
        }



        [TitleGroup("요소 테스트")]
        [ButtonGroup("요소 테스트/버튼그룹")]
        [Button("해제 해보기 (Release 호출)", Icon = SdfIconType.ExclamationSquareFill), GUIColor(0.93f, 0.33f, 0.40f)]
        private void Editor_ExecuteRelease()
        {
            var infinityStack = PanBaseEventValueInfinityStack; //. 지역 변수로 복사
            EditorNumberInputPopup.Show("해제 개수 입력", count =>
            {
                infinityStack.Release(count);
            }, 1);
        }



        private static class EditorNumberInputPopup
        {
            public static void Show(string title, Action<int> onConfirm, int defaultValue = 0)
            {
                PopupWindow.Show(new Rect(UnityEngine.Event.current.mousePosition, Vector2.zero), new NumberInputContent(title, onConfirm, defaultValue));
            }

            private class NumberInputContent : PopupWindowContent
            {
                private readonly string _title;
                private readonly Action<int> _onConfirm;
                private int _value;
                private bool _requestFocus = true; //. 포커스 요청할지 여부

                public NumberInputContent(string title, Action<int> onConfirm, int defaultValue)
                {
                    _title = title;
                    _onConfirm = onConfirm;
                    _value = defaultValue;
                }

                public override Vector2 GetWindowSize() => new Vector2(250, 80);

                public override void OnGUI(Rect rect)
                {
                    GUILayout.Label(_title, EditorStyles.boldLabel);

                    GUI.SetNextControlName("NumberInputField");
                    _value = EditorGUILayout.IntField("입력", _value);

                    if (_requestFocus)
                    {
                        _requestFocus = false;
                        EditorGUI.FocusTextInControl("NumberInputField"); //. 입력창에 커서 이동
                    }

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("확인"))
                    {
                        _onConfirm?.Invoke(_value);
                        editorWindow.Close();
                    }
                }
            }
        }



        ///======================================================================================================================================================



        [HideInInspector]
        private Type Type;



        ///======================================================================================================================================================



        [ShowInInspector]
        [DisplayAsString]
        [HideLabel]
        [PropertyOrder(1000)]
        private readonly string dummyLastField => "";



        ///======================================================================================================================================================
    }



    public class PanEventGeneralManagerWindow : OdinEditorWindow
    {
        ///======================================================================================================================================================



        private EditorRepaintScheduler repaintScheduler;



        ///======================================================================================================================================================



        [MenuItem("판/PanEvent 통합 매니저")]
        private static void OpenWindow()
        {
            GetWindow<PanEventGeneralManagerWindow>().ShowUtility();
        }



        private void Awake()
        {
            PanEventList?.Clear();
            PanEventValuePoolList?.Clear();
        }



        protected override void OnEnable()
        {
            base.OnEnable();
            PanEventList?.Clear();
            repaintScheduler ??= EditorRepaintScheduler.StartWithSecondInterval(this, RepaintScheduleInterval, PostRepaintEvent);
        }



        protected override void OnDisable()
        {
            base.OnDisable();
            PanEventList?.Clear();
            repaintScheduler?.Dispose(); //! 중요: 반드시 Dispose 해줘야 GC-free 루프 해제됨
            repaintScheduler = null;
        }



        private void PostRepaintEvent()
        {
            RepaintedTime = DateTime.Now.ToString();


            //. 이 에디터 작업중 에러 방지용 초기화
            if (!Application.isPlaying)
            {
                if (PanEventList != null) { PanEventList.Clear(); }
                if (PanEventValuePoolList != null) { PanEventValuePoolList.Clear(); }
            }


            //! 초기화가 되어있어야만 진행
            if (!PanEventGeneralManager.IsInitialize) { return; }

            Refresh_PanEvents();
            Refresh_PanEventValues();
        }




        ///======================================================================================================================================================



        //? 리페인트 영역



        [ShowInInspector]
        [TitleGroup("Repaint"), BoxGroup("Repaint/리페인트박스", ShowLabel = false)]
        [HideLabel, PropertySpace(5, 5)]
        [DisplayAsString(EnableRichText = true)]
        private string RepaintTimeIntervalInfoText
        {
            get
            {
                if (RepaintScheduleInterval > 0)
                {
                    return $"Repaint 갱신 주기: 매 <b><color=#2ecc71>{RepaintScheduleInterval}</color>초</b>";
                }
                else
                {
                    return $"Repaint 갱신 주기: 매 <b><color=#2ecc71>프레임</color></b>";
                }
            }
        }



        [ShowInInspector]
        [TitleGroup("Repaint"), BoxGroup("Repaint/리페인트박스", ShowLabel = false)]
        [LabelText(" 마지막 Repaint 시간"), DisplayAsString, LabelWidth(140), PropertySpace(10, 10)]
        private string RepaintedTime;



        [TitleGroup("Repaint"), BoxGroup("Repaint/리페인트박스", ShowLabel = false), HorizontalGroup("Repaint/리페인트박스/갱신주기변경")]
        [LabelText("갱신 주기 변경 (초)")]
        [Range(0f, 1f)]
        [ShowInInspector]
        [OnValueChanged(nameof(SetRepaintScheduleInterval))]
        private float RepaintScheduleInterval = 0;



        private void SetRepaintScheduleInterval()
        {
            repaintScheduler?.Dispose();
            repaintScheduler = null;
            if (RepaintScheduleInterval > 0)
            {
                repaintScheduler = EditorRepaintScheduler.StartWithSecondInterval(this, RepaintScheduleInterval, PostRepaintEvent);
            }
            else
            {
                repaintScheduler = EditorRepaintScheduler.StartWithFrameInterval(this, 1, PostRepaintEvent);
            }
        }



        ///======================================================================================================================================================



        private bool PanEventGeneralManagerIsInitialize => PanEventGeneralManager.IsInitialize;



        private bool PanEventGeneralManagerIsNotInitialize => !PanEventGeneralManager.IsInitialize;



        ///======================================================================================================================================================



        [InfoBox("PanEventGeneralManager가 초기화 되지 않은 상태", InfoMessageType = InfoMessageType.Info)]
        [HideLabel]
        [DisplayAsString]
        [ShowInInspector, ShowIf(nameof(PanEventGeneralManagerIsNotInitialize))]
        private string dummy = "";



        ///======================================================================================================================================================



        //? 이벤트 매니저



        [ShowInInspector, ShowIf(nameof(PanEventGeneralManagerIsInitialize))]
        [BoxGroup("판 이벤트 매니저/PanEvent 목록", ShowLabel = false)]
        [HideLabel, DisplayAsString(EnableRichText = true), EnableGUI]
        [PropertySpace(8, 8)]
        [PropertyOrder(0)]
        private string EventListInfoText
        {
            get
            {
                if (PanEventList == null) { return ""; }
                return $"총 PanEvent 종류 개수: <color=#2ecc71><b>{PanEventList.Count}</b></color> (<color=#4fc1e9><b>{PanEventList_EnableCount}</b></color> / <color=#ed5565><b>{PanEventList_DisableCount}</b></color>)";
            }
        }



        [ShowInInspector, ShowIf(nameof(PanEventGeneralManagerIsInitialize))]
        [Searchable]
        [ListDrawerSettings(IsReadOnly = true, DraggableItems = false, DefaultExpandedState = false)]
        [BoxGroup("판 이벤트 매니저/PanEvent 목록", ShowLabel = false)]
        [LabelText("PanEvent 목록")]
        [PropertySpace(8)]
        [PropertyOrder(1)]
        private List<PanBaseEventStatus> PanEventList;



        private int PanEventList_EnableCount;
        private int PanEventList_DisableCount;



        //? 이벤트 리스트 관련 메서드



        [ShowIf(nameof(PanEventGeneralManagerIsInitialize))]
        [BoxGroup("판 이벤트 매니저/PanEvent 목록", ShowLabel = false)]
        [ButtonGroup("판 이벤트 매니저/PanEvent 목록/초기화"), Button("PanEvent 목록 Refresh", Icon = SdfIconType.LightningFill), GUIColor(0.31f, 0.76f, 0.91f)]
        [PropertyOrder(2)]
        private void Refresh_PanEvents()
        {
            PanEventList ??= new(PanEventGeneralManager.EventManager.GetEventsDictionary.Count);

            PanEventList.Clear();
            PanEventList_EnableCount = 0;
            PanEventList_DisableCount = 0;


            foreach (var item in PanEventGeneralManager.EventManager.GetEventsDictionary)
            {
                PanEventList.Add(new PanBaseEventStatus(item.Key, item.Value));

                if (item.Value != null) PanEventList_EnableCount++;
                else PanEventList_DisableCount++;
            }
        }



        [ShowIf(nameof(PanEventGeneralManagerIsInitialize))]
        [BoxGroup("판 이벤트 매니저/PanEvent 목록", ShowLabel = false)]
        [ButtonGroup("판 이벤트 매니저/PanEvent 목록/초기화"), Button("PanEvent 목록 Clear", Icon = SdfIconType.LightningFill), GUIColor(0.97f, 0.85f, 0.39f)]
        [PropertyOrder(2)]
        private void ClearPanEventList()
        {
            PanEventList.Clear();
        }



        [ShowIf(nameof(PanEventGeneralManagerIsInitialize))]
        [BoxGroup("판 이벤트 매니저/PanEvent 목록", ShowLabel = false)]
        [ButtonGroup("판 이벤트 매니저/PanEvent 목록/초기화"), Button("PanEvent 목록 Null", Icon = SdfIconType.LightningFill), GUIColor(0.97f, 0.85f, 0.39f)]
        [PropertyOrder(2)]
        private void NullPanEventList()
        {
            ClearPanEventList();
            PanEventList = null;
        }



        //? 테스트 메서드



        [ShowIf(nameof(PanEventGeneralManagerIsInitialize))]
        [BoxGroup("판 이벤트 매니저/PanEvent 목록", ShowLabel = false), TitleGroup("판 이벤트 매니저/PanEvent 목록/테스트")]
        [ButtonGroup("판 이벤트 매니저/PanEvent 목록/테스트/테스트"), Button("테스트", Icon = SdfIconType.LightningFill), GUIColor(0.97f, 0.85f, 0.39f)]
        [PropertyOrder(2)]
        private void Test()
        {
            Debug.Log("아직아무것도없음");
        }



        //? 이벤트 매니저



        [ShowInInspector, ShowIf(nameof(PanEventGeneralManagerIsInitialize))]
        [TitleGroup("판 이벤트 매니저")]
        [EnableGUI]
        [HideReferenceObjectPicker]
        [LabelText("EventManager 직접 보기")]
        [PropertyOrder(3)]
        public PanEventManager EventManager => (PanEventGeneralManager.IsInitialize) ? PanEventGeneralManager.EventManager : null;



        ///======================================================================================================================================================



        //? 이벤트 밸류 매니저



        [ShowInInspector, ShowIf(nameof(PanEventGeneralManagerIsInitialize))]
        [BoxGroup("판 이벤트 밸류 매니저/PanEventValue 풀 목록", ShowLabel = false)]
        [HideLabel, DisplayAsString(EnableRichText = true), EnableGUI]
        [PropertySpace(8, 8)]
        [PropertyOrder(10)]
        private string EventValueListInfoText
        {
            get
            {
                if (PanEventList == null) { return ""; }
                return $"총 PanEventValue 종류 개수: <color=#2ecc71><b>{PanEventValuePoolList.Count}</b></color>";
            }
        }



        [ShowInInspector, ShowIf(nameof(PanEventGeneralManagerIsInitialize))]
        [BoxGroup("판 이벤트 밸류 매니저/PanEventValue 풀 목록", ShowLabel = false)]
        [HideLabel, DisplayAsString(EnableRichText = true), EnableGUI]
        [PropertySpace(8, 8)]
        [PropertyOrder(10)]
        private string EventValueCountInfoText
        {
            get
            {
                return $"가용중인 PanEventValue 개수: <color=#2ecc71><b>{PanEventValuePoolList_AllEventValueCount}</b></color> (<color=#ed5565><b>{PanEventValuePoolList_InPoolCount}</b></color> / <color=#4fc1e9><b>{PanEventValuePoolList_InUseCount}</b></color>)";
            }
        }



        [ShowInInspector, ShowIf(nameof(PanEventGeneralManagerIsInitialize))]
        [Searchable]
        [ListDrawerSettings(IsReadOnly = true, DraggableItems = false, DefaultExpandedState = false)]
        [BoxGroup("판 이벤트 밸류 매니저/PanEventValue 풀 목록", ShowLabel = false)]
        [LabelText("PanEventValue 풀 목록")]
        [PropertyOrder(11)]

        private List<PanBaseEventValueStatus> PanEventValuePoolList;



        private int PanEventValuePoolList_AllEventValueCount;
        private int PanEventValuePoolList_InPoolCount;
        private int PanEventValuePoolList_InUseCount;



        //? 이벤트 밸류 풀 리스트 관련 메서드



        [ShowIf(nameof(PanEventGeneralManagerIsInitialize))]
        [BoxGroup("판 이벤트 밸류 매니저/PanEventValue 풀 목록", ShowLabel = false)]
        [ButtonGroup("판 이벤트 밸류 매니저/PanEventValue 풀 목록/초기화"), Button("PanEventValue 목록 갱신", Icon = SdfIconType.LightningFill), GUIColor(0.31f, 0.76f, 0.91f)]
        [PropertyOrder(12)]
        private void Refresh_PanEventValues()
        {
            PanEventValuePoolList ??= new(PanEventGeneralManager.EventValueManager.GetEventValueCount());
            PanEventValuePoolList.Clear();


            PanEventValuePoolList_AllEventValueCount = 0;
            PanEventValuePoolList_InPoolCount = 0;
            PanEventValuePoolList_InUseCount = 0;


            foreach (var item in PanEventGeneralManager.EventValueManager.GetEventValuePoolDictionary)
            {
                PanEventValuePoolList.Add(new PanBaseEventValueStatus(item.Key, item.Value));

                PanEventValuePoolList_AllEventValueCount += item.Value.CurrentElementsCount;
                PanEventValuePoolList_InPoolCount += item.Value.PoolCount;
                PanEventValuePoolList_InUseCount += item.Value.InUseCount;
            }
        }



        [ShowIf(nameof(PanEventGeneralManagerIsInitialize))]
        [BoxGroup("판 이벤트 밸류 매니저/PanEventValue 풀 목록", ShowLabel = false)]
        [ButtonGroup("판 이벤트 밸류 매니저/PanEventValue 풀 목록/초기화"), Button("PanEventValue 목록 Clear", Icon = SdfIconType.LightningFill), GUIColor(0.97f, 0.85f, 0.39f)]
        [PropertyOrder(12)]
        private void ClearPanEventValueList()
        {
            PanEventValuePoolList.Clear();
        }



        [ShowIf(nameof(PanEventGeneralManagerIsInitialize))]
        [BoxGroup("판 이벤트 밸류 매니저/PanEventValue 풀 목록", ShowLabel = false)]
        [ButtonGroup("판 이벤트 밸류 매니저/PanEventValue 풀 목록/초기화"), Button("PanEventValue 목록 Null", Icon = SdfIconType.LightningFill), GUIColor(0.97f, 0.85f, 0.39f)]
        [PropertyOrder(12)]
        private void NullPanEventValueList()
        {
            ClearPanEventList();
            PanEventValuePoolList = null;
        }



        [ShowIf(nameof(PanEventGeneralManagerIsInitialize))]
        [BoxGroup("판 이벤트 밸류 매니저/PanEventValue 풀 목록", ShowLabel = false)]
        [ButtonGroup("판 이벤트 밸류 매니저/PanEventValue 풀 목록/검사"), Button("PanEventValue 목록 중복 검사", Icon = SdfIconType.Search), GUIColor(0.93f, 0.33f, 0.40f)]
        [PropertyOrder(12)]
        private void CheckDuplicate()
        {
            EventValueManager.CheckEventValuePoolDictionary_IsDuplicate();
        }



        //? 테스트 메서드



        [ShowIf(nameof(PanEventGeneralManagerIsInitialize))]
        [BoxGroup("판 이벤트 밸류 매니저/PanEventValue 풀 목록", ShowLabel = false), TitleGroup("판 이벤트 밸류 매니저/PanEventValue 풀 목록/테스트")]
        [ButtonGroup("판 이벤트 밸류 매니저/PanEventValue 풀 목록/테스트/테스트"), Button("테스트", Icon = SdfIconType.LightningFill), GUIColor(0.97f, 0.85f, 0.39f)]
        [PropertyOrder(13)]
        private void Test2()
        {
            Debug.Log("아직아무것도없음");
        }



        //? 이벤트 밸류 매니저



        [ShowInInspector, ShowIf(nameof(PanEventGeneralManagerIsInitialize))]
        [TitleGroup("판 이벤트 밸류 매니저")]
        [EnableGUI]
        [HideReferenceObjectPicker]
        [LabelText("EventValueManager 직접 보기")]
        [PropertyOrder(14)]
        public PanEventValueManager EventValueManager
        {
            get
            {
                return (PanEventGeneralManager.IsInitialize) ? PanEventGeneralManager.EventValueManager : null;
            }
            private set { } //. 에디터에서 내부를 수정할수있게 억지로 만든 setter
        }



        ///======================================================================================================================================================



        //private Vector2 WindowScrollPos;



        //protected override void OnImGUI()
        //{
        //    WindowScrollPos = EditorGUILayout.BeginScrollView(WindowScrollPos);



        //    //base.OnImGUI();
        //    DrawEditor(0); // 기본 필드 수동 호출


        //    SU_CustomEditor.Format_HorizontalLine();


        //    if (!PanEventGeneralManager.IsInitialize)
        //    {
        //        SU_CustomEditor.HelpBox(MessageType.Info, $"{nameof(PanEventGeneralManager)} 가 초기화 되지 않음");
        //    }
        //    else
        //    {
        //        SU_CustomEditor.AutoLabelField_Head("이벤트 간략 정보", SU_CustomEditor.LabelHeadType.H1, () =>
        //        {
        //            if (PanEventList != null)
        //            {
        //                SU_CustomEditor.VerticalHelpBox(() =>
        //                {
        //                    GUILayout.Space(10);
        //                    SU_CustomEditor.UsingStringBuilder((sb) =>
        //                    {
        //                        sb.AppendLine($"전체 이벤트 개수: <b><color=#2ecc71>{PanEventList.Count}</color></b> [ <b><color=#4fc1e9>{PanEventList_EnableCount}</color></b> / <b><color=#ed5565>{PanEventList_DisableCount}</color></b> ]");

        //                        SU_CustomEditor.LabelField_TextAutoWidthHeight(sb.ToString(true));
        //                    });
        //                    GUILayout.Space(10);
        //                });
        //            }
        //        });
        //    }


        //    EditorGUILayout.EndScrollView();
        //}



        ///======================================================================================================================================================
    }
}
