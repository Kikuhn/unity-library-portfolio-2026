using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using SitraUtils;
using Pan.Util;
using Pan.StageGenerators;
using Pan.GridCompatibles2;
using Sirenix.OdinInspector;
using System.Linq;
using Sirenix.Serialization;



namespace Pan.StageGenerators
{
    public interface IStageGeneratorSetting
    {
        StageGeneratorSetting Setting { get; }
    }



    [Serializable]
    public class StageGeneratorSetting : IStageGeneratorSetting, ICopyable<StageGeneratorSetting>
    {
        ///======================================================================================================================================================



        StageGeneratorSetting IStageGeneratorSetting.Setting => this;



        ///======================================================================================================================================================



        public void Copy(StageGeneratorSetting original)
        {
            prefabSettingSbject = original.prefabSettingSbject;
            stageVector.Copy(original.stageVector);
            stageGrid.Copy(original.stageGrid);
            space.Copy(original.space);
            spaceNodeConnect.Copy(original.spaceNodeConnect);
            room.Copy(original.room);
            hallway.Copy(original.hallway);
            customEvent.Copy(original.customEvent);
        }



        public void OnValidate(bool absolute)
        {
            if (absolute || !stageVector.IsWakeUp) stageVector.WakeUp(this);
            if (absolute || !stageGrid.IsWakeUp) stageGrid.WakeUp(this);
            if (absolute || !space.IsWakeUp) space.WakeUp(this);
            if (absolute || !spaceNodeConnect.IsWakeUp) spaceNodeConnect.WakeUp(this);
            if (absolute || !room.IsWakeUp) room.WakeUp(this);
            if (absolute || !hallway.IsWakeUp) hallway.WakeUp(this);
            if (absolute || !customEvent.IsWakeUp) customEvent.WakeUp(this);
        }



        ///======================================================================================================================================================



        //? 내부 클래스의 베이스



        [Serializable]
        public abstract class BaseMain<TCRTP> : MainSlaveSerialized_WakeUpVer<StageGeneratorSetting>, ICopyable<TCRTP>
        {
            public abstract void Copy(TCRTP obj);

            public abstract void Refresh();
        }



        ///======================================================================================================================================================



        //? 스테이지 프리팹 설정 (필수)



        [TitleGroup("스테이지 프리팹 설정")]
        [InlineEditor]
        [SerializeField]
        [LabelText("프리팹 설정 SO")]
        [OnValueChanged(nameof(OnValidate), true)]
        [PropertyOrder(0)]
        [InfoBox("가용성이 유효한 스테이지 프리팹 설정이 할당 되어 있어야, 설정이 가능", InfoMessageType.Warning, VisibleIf = "@!IsValid")]
        private StageGeneratorSettingPrefabSbject prefabSettingSbject;



        /// <summary>
        /// 스테이지 프리팹 설정 SO 얻기
        /// </summary>
        public StageGeneratorSettingPrefabSbject PrefabSettingSbject => prefabSettingSbject;



        /// <summary>
        /// 스테이지 프리팹 설정의 그리드 스냅 설정 얻기
        /// </summary>
        public GridCompatible2SnapSetting SnapSetting
        {
            get
            {
                if (!IsValid) { return snapSettingDummy; }

                return prefabSettingSbject.Setting.GridCompatibleSnapSettingSbject.GridCompatibleSnapSetting;
            }
        }



        //! null 에러 방지용, 더미 스냅 설정
        private static GridCompatible2SnapSetting _snapSettingDummy = new GridCompatible2SnapSetting();
        private static GridCompatible2SnapSetting snapSettingDummy
        {
            get
            {
                if (_snapSettingDummy == null) { _snapSettingDummy = new GridCompatible2SnapSetting(); }
                return _snapSettingDummy;
            }
        }



        ///======================================================================================================================================================



        //? 유효



        /// <summary>
        /// 이 <see cref="StageGeneratorSetting"/>이 유효한지 여부 
        /// <para>스테이지 프리팹 SO가 할당 되어 있어야 하고, 해당 설정이 유효해야한다</para>
        /// </summary>
        public bool IsValid => prefabSettingSbject != null && prefabSettingSbject.Setting.IsValidToUse;



        ///======================================================================================================================================================



        //? 스테이지 그리드



        [Serializable]
        public class StageGrids : BaseMain<StageGrids>
        {

            ///======================================================================================================================================================



            public StageGrids()
            {
                gridTagSettingDefaultList = new List<GridTagSettingSbject>
                {
                    gridTagSetting_RoomMain,
                    gridTagSetting_RoomSafe,
                    gridTagSetting_RoomExpandSafe,
                    gridTagSetting_RoomEdge,
                    gridTagSetting_RoomDoor,
                    gridTagSetting_RoomDoorStart,
                    gridTagSetting_RoomDoorEnd,
                    gridTagSetting_RoomDoorAreaStartEnd,
                    gridTagSetting_RoomDoorArea,
                    gridTagSetting_RoomDoorAreaExpand,
                    gridTagSetting_HallwayMain,
                    gridTagSetting_HallwayExpand,
                    gridTagSetting_HallwayEdge,
                    gridTagSetting_HallwaySafe,
                    gridTagSetting_HallwayDoorCorrectionEdge,
                    gridTagSetting_HallwayDoorCorrectionSafe
                };
            }



#if UNITY_EDITOR

            [TitleGroup("스테이지 그리드"), BoxGroup("스테이지 그리드/박스", false)]
            [ShowInInspector, HideLabel, DisplayAsString(EnableRichText = true, Overflow = false, Alignment = TextAlignment.Center), EnableGUI]
            [PropertyOrder(0)]
            [PropertySpace(8, 8)]
            private string dummy_GridSnapInfo
            {
                get
                {
                    return $"<b>GridSwizzle</b>: {Main.SnapSetting.Swizzle}\n<b>GridUnit</b>: {Main.SnapSetting.GridUnitOriginalVector3}";
                }
            }

#endif



            ///======================================================================================================================================================



            #region 기본 그리드 태그 설정



            public GridTagSettingSbject GridTagSetting_RoomMain => gridTagSetting_RoomMain;
            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로10")]
            [LabelText("방 메인")]
            [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
            [PropertyOrder(2)]
            private GridTagSettingSbject gridTagSetting_RoomMain;



            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로10", 0.2f)]
            [Button("SetDefault", ButtonSizes.Small), GUIColor(0.93f, 0.33f, 0.40f)]
            [PropertyOrder(2)]
            private void ApplyDefault_RoomMain()
            {
                gridTagSetting_RoomMain = StageGenerator2SingletonSOManager.O.GridTagSetting_RoomMain;
            }



            public GridTagSettingSbject GridTagSetting_RoomSafe => gridTagSetting_RoomSafe;
            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로20")]
            [LabelText("방 안전구역")]
            [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
            [PropertyOrder(2)]
            private GridTagSettingSbject gridTagSetting_RoomSafe;



            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로20", 0.2f)]
            [Button("SetDefault", ButtonSizes.Small), GUIColor(0.93f, 0.33f, 0.40f)]
            [PropertyOrder(2)]
            private void ApplyDefault_RoomSafe()
            {
                gridTagSetting_RoomSafe = StageGenerator2SingletonSOManager.O.GridTagSetting_RoomSafe;
            }



            public GridTagSettingSbject GridTagSetting_RoomExpandSafe => gridTagSetting_RoomExpandSafe;
            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로30")]
            [LabelText("방 확장 안전구역")]
            [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
            [PropertyOrder(2)]
            private GridTagSettingSbject gridTagSetting_RoomExpandSafe;



            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로30", 0.2f)]
            [Button("SetDefault", ButtonSizes.Small), GUIColor(0.93f, 0.33f, 0.40f)]
            [PropertyOrder(2)]
            private void ApplyDefault_RoomExpandSafe()
            {
                gridTagSetting_RoomExpandSafe = StageGenerator2SingletonSOManager.O.GridTagSetting_RoomExpandSafe;
            }



            public GridTagSettingSbject GridTagSetting_RoomEdge => gridTagSetting_RoomEdge;
            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로40")]
            [LabelText("방 테두리")]
            [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
            [PropertyOrder(2)]
            private GridTagSettingSbject gridTagSetting_RoomEdge;



            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로40", 0.2f)]
            [Button("SetDefault", ButtonSizes.Small), GUIColor(0.93f, 0.33f, 0.40f)]
            [PropertyOrder(2)]
            private void ApplyDefault_RoomEdge()
            {
                gridTagSetting_RoomEdge = StageGenerator2SingletonSOManager.O.GridTagSetting_RoomEdge;
            }



            public GridTagSettingSbject GridTagSetting_RoomDoor => gridTagSetting_RoomDoor;
            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로50")]
            [LabelText("방 도어")]
            [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
            [PropertyOrder(2)]
            private GridTagSettingSbject gridTagSetting_RoomDoor;



            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로50", 0.2f)]
            [Button("SetDefault", ButtonSizes.Small), GUIColor(0.93f, 0.33f, 0.40f)]
            [PropertyOrder(2)]
            private void ApplyDefault_RoomDoor()
            {
                gridTagSetting_RoomDoor = StageGenerator2SingletonSOManager.O.GridTagSetting_RoomDoor;
            }



            public GridTagSettingSbject GridTagSetting_RoomDoorStart => gridTagSetting_RoomDoorStart;
            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로60")]
            [LabelText("방 도어 시작")]
            [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
            [PropertyOrder(2)]
            private GridTagSettingSbject gridTagSetting_RoomDoorStart;



            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로60", 0.2f)]
            [Button("SetDefault", ButtonSizes.Small), GUIColor(0.93f, 0.33f, 0.40f)]
            [PropertyOrder(2)]
            private void ApplyDefault_RoomDoorStart()
            {
                gridTagSetting_RoomDoorStart = StageGenerator2SingletonSOManager.O.GridTagSetting_RoomDoorStart;
            }



            public GridTagSettingSbject GridTagSetting_RoomDoorEnd => gridTagSetting_RoomDoorEnd;
            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로70")]
            [LabelText("방 도어 끝")]
            [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
            [PropertyOrder(2)]
            private GridTagSettingSbject gridTagSetting_RoomDoorEnd;



            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로70", 0.2f)]
            [Button("SetDefault", ButtonSizes.Small), GUIColor(0.93f, 0.33f, 0.40f)]
            [PropertyOrder(2)]
            private void ApplyDefault_RoomDoorEnd()
            {
                gridTagSetting_RoomDoorEnd = StageGenerator2SingletonSOManager.O.GridTagSetting_RoomDoorEnd;
            }



            public GridTagSettingSbject GridTagSetting_RoomDoorAreaStartEnd => gridTagSetting_RoomDoorAreaStartEnd;
            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로80")]
            [LabelText("방 도어 영역 시작끝")]
            [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
            [PropertyOrder(2)]
            private GridTagSettingSbject gridTagSetting_RoomDoorAreaStartEnd;



            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로80", 0.2f)]
            [Button("SetDefault", ButtonSizes.Small), GUIColor(0.93f, 0.33f, 0.40f)]
            [PropertyOrder(2)]
            private void ApplyDefault_RoomDoorAreaStartEnd()
            {
                gridTagSetting_RoomDoorAreaStartEnd = StageGenerator2SingletonSOManager.O.GridTagSetting_RoomDoorAreaStartEnd;
            }



            public GridTagSettingSbject GridTagSetting_RoomDoorArea => gridTagSetting_RoomDoorArea;
            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로90")]
            [LabelText("방 도어 영역")]
            [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
            [PropertyOrder(2)]
            private GridTagSettingSbject gridTagSetting_RoomDoorArea;



            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로90", 0.2f)]
            [Button("SetDefault", ButtonSizes.Small), GUIColor(0.93f, 0.33f, 0.40f)]
            [PropertyOrder(2)]
            private void ApplyDefault_RoomDoorArea()
            {
                gridTagSetting_RoomDoorArea = StageGenerator2SingletonSOManager.O.GridTagSetting_RoomDoorArea;
            }



            public GridTagSettingSbject GridTagSetting_RoomDoorAreaExpand => gridTagSetting_RoomDoorAreaExpand;
            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로100")]
            [LabelText("방 도어 확장")]
            [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
            [PropertyOrder(2)]
            private GridTagSettingSbject gridTagSetting_RoomDoorAreaExpand;



            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로100", 0.2f)]
            [Button("SetDefault", ButtonSizes.Small), GUIColor(0.93f, 0.33f, 0.40f)]
            [PropertyOrder(2)]
            private void ApplyDefault_RoomDoorAreaExpand()
            {
                gridTagSetting_RoomDoorAreaExpand = StageGenerator2SingletonSOManager.O.GridTagSetting_RoomDoorAreaExpand;
            }



            public GridTagSettingSbject GridTagSetting_HallwayMain => gridTagSetting_HallwayMain;
            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로110")]
            [LabelText("메인 복도")]
            [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
            [PropertyOrder(2)]
            private GridTagSettingSbject gridTagSetting_HallwayMain;



            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로110", 0.2f)]
            [Button("SetDefault", ButtonSizes.Small), GUIColor(0.93f, 0.33f, 0.40f)]
            [PropertyOrder(2)]
            private void ApplyDefault_HallwayMain()
            {
                gridTagSetting_HallwayMain = StageGenerator2SingletonSOManager.O.GridTagSetting_HallwayMain;
            }



            public GridTagSettingSbject GridTagSetting_HallwayExpand => gridTagSetting_HallwayExpand;
            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로120")]
            [LabelText("확장 복도")]
            [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
            [PropertyOrder(2)]
            private GridTagSettingSbject gridTagSetting_HallwayExpand;



            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로120", 0.2f)]
            [Button("SetDefault", ButtonSizes.Small), GUIColor(0.93f, 0.33f, 0.40f)]
            [PropertyOrder(2)]
            private void ApplyDefault_HallwayExpand()
            {
                gridTagSetting_HallwayExpand = StageGenerator2SingletonSOManager.O.GridTagSetting_HallwayExpand;
            }



            public GridTagSettingSbject GridTagSetting_HallwayEdge => gridTagSetting_HallwayEdge;
            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로130")]
            [LabelText("복도 테두리")]
            [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
            [PropertyOrder(2)]
            private GridTagSettingSbject gridTagSetting_HallwayEdge;



            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로130", 0.2f)]
            [Button("SetDefault", ButtonSizes.Small), GUIColor(0.93f, 0.33f, 0.40f)]
            [PropertyOrder(2)]
            private void ApplyDefault_HallwayEdge()
            {
                gridTagSetting_HallwayEdge = StageGenerator2SingletonSOManager.O.GridTagSetting_HallwayEdge;
            }



            public GridTagSettingSbject GridTagSetting_HallwaySafe => gridTagSetting_HallwaySafe;
            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로140")]
            [LabelText("복도 안전구역")]
            [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
            [PropertyOrder(2)]
            private GridTagSettingSbject gridTagSetting_HallwaySafe;



            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로140", 0.2f)]
            [Button("SetDefault", ButtonSizes.Small), GUIColor(0.93f, 0.33f, 0.40f)]
            [PropertyOrder(2)]
            private void ApplyDefault_HallwaySafe()
            {
                gridTagSetting_HallwaySafe = StageGenerator2SingletonSOManager.O.GridTagSetting_HallwaySafe;
            }



            public GridTagSettingSbject GridTagSetting_HallwayDoorCorrectionEdge => gridTagSetting_HallwayDoorCorrectionEdge;
            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로150")]
            [LabelText("도어 보정 복도 테두리")]
            [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
            [PropertyOrder(2)]
            private GridTagSettingSbject gridTagSetting_HallwayDoorCorrectionEdge;



            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로150", 0.2f)]
            [Button("SetDefault", ButtonSizes.Small), GUIColor(0.93f, 0.33f, 0.40f)]
            [PropertyOrder(2)]
            private void ApplyDefault_HallwayDoorCorrectionEdge()
            {
                gridTagSetting_HallwayDoorCorrectionEdge = StageGenerator2SingletonSOManager.O.GridTagSetting_HallwayDoorCorrectionEdge;
            }



            public GridTagSettingSbject GridTagSetting_HallwayDoorCorrectionSafe => gridTagSetting_HallwayDoorCorrectionSafe;
            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로160")]
            [LabelText("도어 보정 복도 안전구역")]
            [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
            [PropertyOrder(2)]
            private GridTagSettingSbject gridTagSetting_HallwayDoorCorrectionSafe;



            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [HorizontalGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기/가로160", 0.2f)]
            [Button("SetDefault", ButtonSizes.Small), GUIColor(0.93f, 0.33f, 0.40f)]
            [PropertyOrder(2)]
            private void ApplyDefault_HallwayDoorCorrectionSafe()
            {
                gridTagSetting_HallwayDoorCorrectionSafe = StageGenerator2SingletonSOManager.O.GridTagSetting_HallwayDoorCorrectionSafe;
            }



            #endregion



            [FoldoutGroup("스테이지 그리드/박스/그리드 태그 설정 SO 보기")]
            [Button("모든 그리드 태그 설정을 기본값으로 초기화"), GUIColor(0.97f, 0.85f, 0.39f)]
            [PropertyOrder(3)]
            private void ApplyDefault_AllGridTagSetting()
            {
                ApplyDefault_RoomMain();
                ApplyDefault_RoomSafe();
                ApplyDefault_RoomExpandSafe();
                ApplyDefault_RoomEdge();
                ApplyDefault_RoomDoor();
                ApplyDefault_RoomDoorStart();
                ApplyDefault_RoomDoorEnd();
                ApplyDefault_RoomDoorAreaStartEnd();
                ApplyDefault_RoomDoorArea();
                ApplyDefault_RoomDoorAreaExpand();
                ApplyDefault_HallwayMain();
                ApplyDefault_HallwayExpand();
                ApplyDefault_HallwayEdge();
                ApplyDefault_HallwaySafe();
                ApplyDefault_HallwayDoorCorrectionEdge();
                ApplyDefault_HallwayDoorCorrectionSafe();
            }



            [SerializeField][HideInInspector] private List<GridTagSettingSbject> gridTagSettingDefaultList;



            [TitleGroup("스테이지 그리드"), BoxGroup("스테이지 그리드/박스", false)]
            [LabelText("추가 그리드 태그 확장 설정 목록")]
            [SerializeField]
            [PropertyOrder(4)]
            private List<GridTagSettingSbject> gridTagSettingExtendList = new List<GridTagSettingSbject>();



            //public void GridTag(StageGenerator.GridTag gridTag, bool isEnable)
            //{
            //    for (int i = 0; i < gridTagSettingDefaultList.Count; i++)
            //    {
            //        var gridTagSetting = gridTagSettingDefaultList[i];
            //    }
            //}



            ///======================================================================================================================================================



            public override void Copy(StageGrids original)
            {
                gridTagSetting_RoomMain = original.gridTagSetting_RoomMain;
                gridTagSetting_RoomSafe = original.gridTagSetting_RoomSafe;
                gridTagSetting_RoomExpandSafe = original.gridTagSetting_RoomExpandSafe;
                gridTagSetting_RoomEdge = original.gridTagSetting_RoomEdge;
                gridTagSetting_RoomDoor = original.gridTagSetting_RoomDoor;
                gridTagSetting_RoomDoorStart = original.gridTagSetting_RoomDoorStart;
                gridTagSetting_RoomDoorEnd = original.gridTagSetting_RoomDoorEnd;
                gridTagSetting_RoomDoorAreaStartEnd = original.gridTagSetting_RoomDoorAreaStartEnd;
                gridTagSetting_RoomDoorArea = original.gridTagSetting_RoomDoorArea;
                gridTagSetting_RoomDoorAreaExpand = original.gridTagSetting_RoomDoorAreaExpand;
                gridTagSetting_HallwayMain = original.gridTagSetting_HallwayMain;
                gridTagSetting_HallwayExpand = original.gridTagSetting_HallwayExpand;
                gridTagSetting_HallwayEdge = original.gridTagSetting_HallwayEdge;
                gridTagSetting_HallwaySafe = original.gridTagSetting_HallwaySafe;
                gridTagSetting_HallwayDoorCorrectionEdge = original.gridTagSetting_HallwayDoorCorrectionEdge;
                gridTagSetting_HallwayDoorCorrectionSafe = original.gridTagSetting_HallwayDoorCorrectionSafe;


                gridTagSettingDefaultList = new List<GridTagSettingSbject>
                {
                    gridTagSetting_RoomMain,
                    gridTagSetting_RoomSafe,
                    gridTagSetting_RoomExpandSafe,
                    gridTagSetting_RoomEdge,
                    gridTagSetting_RoomDoor,
                    gridTagSetting_RoomDoorStart,
                    gridTagSetting_RoomDoorEnd,
                    gridTagSetting_RoomDoorAreaStartEnd,
                    gridTagSetting_RoomDoorArea,
                    gridTagSetting_RoomDoorAreaExpand,
                    gridTagSetting_HallwayMain,
                    gridTagSetting_HallwayExpand,
                    gridTagSetting_HallwayEdge,
                    gridTagSetting_HallwaySafe,
                    gridTagSetting_HallwayDoorCorrectionEdge,
                    gridTagSetting_HallwayDoorCorrectionSafe,
                };
                gridTagSettingExtendList = new List<GridTagSettingSbject>(original.gridTagSettingExtendList);
            }




            public override void Refresh()
            {

            }



            ///======================================================================================================================================================

        }



        public StageGrids StageGrid => stageGrid;
        [InlineProperty, HideLabel, HideReferenceObjectPicker, SerializeField, ShowIf(nameof(IsValid))]
        private StageGrids stageGrid = new();



        ///======================================================================================================================================================



        //? 스테이지 벡터



        [Serializable]
        public class StageVectors : BaseMain<StageVectors>
        {
            ///======================================================================================================================================================



            //? 스테이지 크기



            [TitleGroup("스테이지 벡터"), BoxGroup("스테이지 벡터/박스", false)]
            [HorizontalGroup("스테이지 벡터/박스/가로")]
            [ShowInInspector]
            [LabelText("너비")]
            [LabelWidth(40)]
            [PropertyOrder(1)]
            [DelayedProperty]
            public int StageWidth
            {
                get => stageWidth;
                set
                {
                    stageWidth = value;


                    //. 공간 분할 단위를 갱신한다
                    Main.Space.SpaceDivideUnit = Main.Space.SpaceDivideUnit;

                    //. 공간의 최대 너비도 같이 갱신
                    Main.space.SpaceWidthMax = Main.space.SpaceWidthMax;
                }
            }
            [SerializeField, HideInInspector] private int stageWidth = 100;



            [TitleGroup("스테이지 벡터"), BoxGroup("스테이지 벡터/박스", false)]
            [HorizontalGroup("스테이지 벡터/박스/가로")]
            [ShowInInspector]
            [LabelText("높이")]
            [LabelWidth(40)]
            [PropertyOrder(1)]
            [DelayedProperty]
            public int StageHeight
            {
                get => stageHeight;
                set
                {
                    stageHeight = value;

                    //. 공간 분할 단위를 갱신한다
                    Main.Space.SpaceDivideUnit = Main.Space.SpaceDivideUnit;

                    //. 공간의 최대 높이도 같이 갱신
                    Main.space.SpaceHeightMax = Main.space.SpaceHeightMax;
                }
            }
            [SerializeField, HideInInspector] private int stageHeight = 100;



            /// <summary>
            /// 스테이지 너비 (Current)
            /// </summary>
            public int StageWidthTransform => StageWidth * Main.SnapSetting.GridUnitX_Width;

            /// <summary>
            /// 스테이지 높이 (Current)
            /// </summary>
            public int StageHeightTransform => StageHeight * Main.SnapSetting.GridUnitY_Height;



            /// <summary>
            /// 스테이지의 너비/높이 <see cref="Vector2Int"/> 로 얻기
            /// </summary>
            public Vector2Int StageSizeVector => new Vector2Int(stageWidth, stageHeight);



            [TitleGroup("스테이지 벡터"), BoxGroup("스테이지 벡터/박스", false)]
            [ShowInInspector, EnableGUI, DisplayAsString]
            [LabelText("스테이지 크기 V2 (Current)")]
            [PropertyOrder(1)]
            [Indent(1)]
            public Vector2Int StageSizeVector2Transform => new Vector2Int(StageWidthTransform, StageHeightTransform);



            [TitleGroup("스테이지 벡터"), BoxGroup("스테이지 벡터/박스", false)]
            [ShowInInspector, EnableGUI, DisplayAsString]
            [LabelText("스테이지 크기 V3 (Current)")]
            [PropertyOrder(1)]
            [Indent(1)]
            public Vector3Int StageSizeVector3Transform => StageSizeVector2Transform.SwizzlesVectorInt2To3(Main.SnapSetting.Swizzle);



            ///======================================================================================================================================================



            //? 스테이지 중심점



            /// <summary>
            /// 자동 중심점을 사용시, 스테이지의 중심 기준점 기준
            /// </summary>
            [TitleGroup("스테이지 벡터"), BoxGroup("스테이지 벡터/박스", false)]
            [LabelText("중심점 좌표 기준")]
            [PropertyOrder(2)]
            [PropertyTooltip("공간 격자들은 기본 중심점으로 부터 좌측 하단으로부터, 우측 상단으로 생성된다")]
            public ECenterStandard StageCenterMode = ECenterStandard.LowerLeft;



            //? 중심점 좌표 연산
            private Vector2 CalculateStageCenterOffsetInternal
            {
                get
                {
                    switch (StageCenterMode)
                    {
                        case ECenterStandard.UpperLeft: return new Vector2(stageWidth * 0.5f, -stageHeight * 0.5f);
                        case ECenterStandard.UpperCenter: return new Vector2(0, -stageHeight * 0.5f);
                        case ECenterStandard.UpperRight: return new Vector2(-stageWidth * 0.5f, -stageHeight * 0.5f);

                        case ECenterStandard.MiddleLeft: return new Vector2(stageWidth * 0.5f, 0);
                        case ECenterStandard.MiddleCenter: return new Vector2(0, 0);
                        case ECenterStandard.MiddleRight: return new Vector2(-stageWidth * 0.5f, 0);

                        case ECenterStandard.LowerLeft: return new Vector2(stageWidth * 0.5f, stageHeight * 0.5f);
                        case ECenterStandard.LowerCenter: return new Vector2(0, stageHeight * 0.5f);
                        case ECenterStandard.LowerRight: return new Vector2(-stageWidth * 0.5f, stageHeight * 0.5f);

                        default: return Vector2.zero;
                    }
                }
            }



            [TitleGroup("스테이지 벡터"), BoxGroup("스테이지 벡터/박스", false)]
            [ShowInInspector, EnableGUI, DisplayAsString]
            [LabelText("중심점 오프셋")]
            [PropertyOrder(2)]
            [Indent(1)]
            public Vector2 StageCenterOffset
            {
                get
                {
                    Vector2 stageCenterOffset = CalculateStageCenterOffsetInternal;

                    //. 중심 스냅을 사용할경우, 중심점을 그리드단위의 절반만큼 보정한다 
                    if (Main.SnapSetting.SnapToGridCellCenter) { stageCenterOffset -= new Vector2(Main.SnapSetting.GridUnitX_Width * 0.5f, Main.SnapSetting.GridUnitY_Height * 0.5f); }

                    return stageCenterOffset;
                }
            }



            [TitleGroup("스테이지 벡터"), BoxGroup("스테이지 벡터/박스", false)]
            [ShowInInspector, EnableGUI, DisplayAsString]
            [LabelText("중심점 오프셋 (Current)")]
            [PropertyOrder(2)]
            [Indent(2)]
            public Vector3 StageCenterOffsetCurrent
                => StageCenterOffset.SwizzlesVector2To3(Main.SnapSetting.Swizzle);



            [TitleGroup("스테이지 벡터"), BoxGroup("스테이지 벡터/박스", false)]
            [ShowInInspector, EnableGUI, DisplayAsString]
            [LabelText("중심점 오프셋 (Transform)")]
            [PropertyOrder(2)]
            [Indent(1)]
            public Vector2 StageCenterOffsetTransform
            {
                get
                {
                    Vector2 stageCenterOffset = CalculateStageCenterOffsetInternal;

                    //. 그리드 단위 연산
                    stageCenterOffset = new Vector2(
                        stageCenterOffset.x * Main.SnapSetting.GridUnitX_Width,
                        stageCenterOffset.y * Main.SnapSetting.GridUnitY_Height);

                    //. 중심 스냅을 사용할경우, 중심점을 그리드단위의 절반만큼 보정한다 
                    if (Main.SnapSetting.SnapToGridCellCenter) { stageCenterOffset -= new Vector2(Main.SnapSetting.GridUnitX_Width * 0.5f, Main.SnapSetting.GridUnitY_Height * 0.5f); }

                    return stageCenterOffset;
                }
            }



            [TitleGroup("스테이지 벡터"), BoxGroup("스테이지 벡터/박스", false)]
            [ShowInInspector, EnableGUI, DisplayAsString]
            [LabelText("중심점 오프셋 (Transform, Current)")]
            [PropertyOrder(2)]
            [Indent(2)]
            public Vector3 StageCenterOffsetTransformCurrent
                => StageCenterOffsetTransform.SwizzlesVector2To3(Main.SnapSetting.Swizzle);



            //? 생성 지점



            [TitleGroup("스테이지 벡터"), BoxGroup("스테이지 벡터/박스", false)]
            [ShowInInspector, EnableGUI, DisplayAsString]
            [LabelText("생성 지점")]
            [PropertyTooltip("스테이지 크기의 좌측 하단 지점")]
            [PropertyOrder(2)]
            [Indent(1)]
            public Vector2 StageInstancePoint
            {
                get
                {
                    Vector2 stageInstancePoint;

                    switch (StageCenterMode)
                    {
                        case ECenterStandard.UpperLeft: stageInstancePoint = new Vector2(0, -stageHeight); break;
                        case ECenterStandard.UpperCenter: stageInstancePoint = new Vector2(-stageWidth * 0.5f, -stageHeight); break;
                        case ECenterStandard.UpperRight: stageInstancePoint = new Vector2(-stageWidth, -stageHeight); break;

                        case ECenterStandard.MiddleLeft: stageInstancePoint = new Vector2(0, -stageHeight * 0.5f); break;
                        case ECenterStandard.MiddleCenter: stageInstancePoint = new Vector3(-stageWidth * 0.5f, -stageHeight * 0.5f); break;
                        case ECenterStandard.MiddleRight: stageInstancePoint = new Vector2(-stageWidth, -stageHeight * 0.5f); break;

                        case ECenterStandard.LowerLeft: stageInstancePoint = new Vector2(0, 0); break;
                        case ECenterStandard.LowerCenter: stageInstancePoint = new Vector2(-stageWidth * 0.5f, 0); break;
                        case ECenterStandard.LowerRight: stageInstancePoint = new Vector2(-stageWidth, 0); break;

                        default: stageInstancePoint = Vector2.zero; break;
                    }


                    //. 반칸 보정 (0.5)
                    if (Main.SnapSetting.SnapToGridCellCenter) { stageInstancePoint -= new Vector2(0.5f, 0.5f); }

                    return stageInstancePoint;
                }
            }



            [TitleGroup("스테이지 벡터"), BoxGroup("스테이지 벡터/박스", false)]
            [ShowInInspector, EnableGUI, DisplayAsString]
            [LabelText("생성 지점 (Current)")]
            [PropertyTooltip("스테이지 크기의 좌측 하단 지점")]
            [PropertyOrder(2)]
            [Indent(2)]
            public Vector3 StageInstancePointCurrent
                => StageInstancePoint.SwizzlesVector2To3(Main.SnapSetting.Swizzle);



            [TitleGroup("스테이지 벡터"), BoxGroup("스테이지 벡터/박스", false)]
            [ShowInInspector, EnableGUI, DisplayAsString]
            [LabelText("생성 지점 (Transform)")]
            [PropertyTooltip("스테이지 크기의 좌측 하단 지점")]
            [PropertyOrder(2)]
            [Indent(1)]
            public Vector2 StageInstanceTransformPoint
            {
                get
                {
                    Vector2 stageInstancePoint;
                    switch (StageCenterMode)
                    {
                        case ECenterStandard.UpperLeft: stageInstancePoint = new Vector2(0, -StageHeightTransform); break;
                        case ECenterStandard.UpperCenter: stageInstancePoint = new Vector2(-StageWidthTransform * 0.5f, -StageHeightTransform); break;
                        case ECenterStandard.UpperRight: stageInstancePoint = new Vector2(-StageWidthTransform, -StageHeightTransform); break;

                        case ECenterStandard.MiddleLeft: stageInstancePoint = new Vector2(0, -StageHeightTransform * 0.5f); break;
                        case ECenterStandard.MiddleCenter: stageInstancePoint = new Vector3(-StageWidthTransform * 0.5f, -StageHeightTransform * 0.5f); break;
                        case ECenterStandard.MiddleRight: stageInstancePoint = new Vector2(-StageWidthTransform, -StageHeightTransform * 0.5f); break;

                        case ECenterStandard.LowerLeft: stageInstancePoint = new Vector2(0, 0); break;
                        case ECenterStandard.LowerCenter: stageInstancePoint = new Vector2(-StageWidthTransform * 0.5f, 0); break;
                        case ECenterStandard.LowerRight: stageInstancePoint = new Vector2(-StageWidthTransform, 0); break;

                        default: return Vector2.zero;
                    }

                    //. 반칸 보정
                    if (Main.SnapSetting.SnapToGridCellCenter) { stageInstancePoint -= new Vector2(Main.SnapSetting.GridUnitX_Width * 0.5f, Main.SnapSetting.GridUnitY_Height * 0.5f); }

                    return stageInstancePoint;
                }
            }



            [TitleGroup("스테이지 벡터"), BoxGroup("스테이지 벡터/박스", false)]
            [ShowInInspector, EnableGUI, DisplayAsString]
            [LabelText("생성 지점 (Transform, Current)")]
            [PropertyTooltip("스테이지 크기의 좌측 하단 지점")]
            [PropertyOrder(2)]
            [Indent(2)]
            public Vector3 StageInstancePointTransformCurrent
                => StageInstanceTransformPoint.SwizzlesVector2To3(Main.SnapSetting.Swizzle);



            ///======================================================================================================================================================



            [TitleGroup("스테이지 벡터"), BoxGroup("스테이지 벡터/박스", false)]
            [ShowInInspector, EnableGUI, DisplayAsString]
            [LabelText("스테이지 Rect")]
            [PropertyOrder(2)]
            public Rect StageRect
            => new Rect((StageInstancePoint), StageSizeVector);



            [TitleGroup("스테이지 벡터"), BoxGroup("스테이지 벡터/박스", false)]
            [ShowInInspector, EnableGUI, DisplayAsString]
            [LabelText("스테이지 Rect (Transform)")]
            [PropertyOrder(2)]
            public Rect StageRectTransform => new Rect((StageInstanceTransformPoint), StageSizeVector2Transform);



            ///======================================================================================================================================================



            public override void Copy(StageVectors original)
            {
                StageWidth = original.StageWidth;
                StageHeight = original.StageHeight;
                StageCenterMode = original.StageCenterMode;
            }



            public override void Refresh()
            {
                StageWidth = StageWidth;
                StageHeight = StageHeight;
                //StageOriginMode = StageOriginMode;
            }



            ///======================================================================================================================================================
        }



        public StageVectors StageVector => stageVector;
        [InlineProperty, HideLabel, HideReferenceObjectPicker, SerializeField, ShowIf(nameof(IsValid))]
        private StageVectors stageVector = new();



        ///======================================================================================================================================================



        //? 공간



        [Serializable]
        public class Spaces : BaseMain<Spaces>
        {
            ///======================================================================================================================================================



            #region 공간 총 생성 개수 



            /// <summary>
            /// 생성되는 공간들의 최소 개수
            /// </summary>
            [TitleGroup("공간 생성"), BoxGroup("공간 생성/박스", false)]
            [BoxGroup("공간 생성/박스/공간 총 생성 개수")]
            [HorizontalGroup("공간 생성/박스/공간 총 생성 개수/가로")]
            [EnableGUI, ShowInInspector]
            [LabelText("총 생성 개수")]
            [LabelWidth(100)]
            [PropertyOrder(1)]
            [PropertyTooltip("생성될 공간의 최소~최대 개수, 현재 최대 개수는 보장되지 않음 (아마도)")]
            public int SpaceCountMin
            {
                get => spaceCountMin;
                private set
                {
                    spaceCountMin.SetClamp(value, 1, SpaceCountMax);
                }
            }
            [SerializeField, HideInInspector] private int spaceCountMin = 1;



            /// <summary>
            /// 생성되는 공간들의 최대 개수 (스테이지의 총 너비에 따라, 최대 개수보다 적게 생성될수있음)
            /// </summary>
            [TitleGroup("공간 생성"), BoxGroup("공간 생성/박스", false)]
            [BoxGroup("공간 생성/박스/공간 총 생성 개수")]
            [HorizontalGroup("공간 생성/박스/공간 총 생성 개수/가로")]
            [EnableGUI, ShowInInspector]
            [LabelText(" ~")]
            [LabelWidth(20)]
            [PropertyOrder(1)]
            public int SpaceCountMax
            {
                get => spaceCountMax;
                private set
                {
                    spaceCountMax.SetClamp(value, SpaceCountMin, int.MaxValue);
                }
            }
            [SerializeField, HideInInspector] private int spaceCountMax = 255;



            #endregion



            ///======================================================================================================================================================



            #region 공간 분할



            /// <summary>
            /// 공간 분할 지점 단위 (공간을 무작위로 나눌때, 공간을 나누는 단위)<br/>
            /// EX) 값이 10이고, 최소값이 20 ~ 50이라면: 20,30,40,50 중에 무작위 선택<br/>
            /// EX) 값이 5이고, 최소값이 0 ~ 20이라면: 0,5,10,15,20 중에 무작위 선택
            /// </summary>
            [TitleGroup("공간 생성"), BoxGroup("공간 생성/박스", false)]
            [BoxGroup("공간 생성/박스/공간 분할")]
            [EnableGUI, ShowInInspector]
            [LabelText("분할 지점 단위")]
            [PropertyOrder(2)]
            [DelayedProperty]
            public int SpaceDivideUnit
            {
                get => divideSpaceUnit;
                set
                {
                    //. 1 ~ (스테이지 너비, 높이 중 작은 값) 만큼 제약한다
                    divideSpaceUnit.SetClamp(value, 1, Mathf.Min(Main.stageVector.StageWidth, Main.stageVector.StageHeight));


                    //. 공간 (최소/최대) (너비/높이)를 모두 갱신한다
                    SpaceWidthMax = SpaceWidthMax;
                    SpaceHeightMax = SpaceHeightMax;
                    //. Max 이후에 Min을 호출해야 안정적으로 CLamp
                    SpaceWidthMin = SpaceWidthMin;
                    SpaceHeightMin = SpaceHeightMin;
                }
            }
            [SerializeField, HideInInspector] private int divideSpaceUnit = 10;



            public enum EDivideMode
            {
                /// <summary>수평 우선 (너비와 높이가 같을때)</summary>
                [LabelText("수평 우선 (너비와 높이가 같을때)")]
                Priority_Horizontal,
                /// <summary>수직 우선 (너비와 높이가 같을때)</summary>
                [LabelText("수직 우선 (너비와 높이가 같을때)")]
                Priority_Vertical,
                /// <summary>수평,수직 무작위 (너비와 높이가 같을때)</summary>
                [LabelText("무작위 (너비와 높이가 같을때)")]
                Random
            }



            /// <summary>
            /// 공간 분할 모드
            /// </summary>
            [TitleGroup("공간 생성"), BoxGroup("공간 생성/박스", false)]
            [BoxGroup("공간 생성/박스/공간 분할")]
            [LabelText("분할 모드")]
            [ShowInInspector]
            [PropertyOrder(3)]
            public EDivideMode SpaceDivideMode;



            /// <summary>
            /// 공간 분할 재귀 중단 확률
            /// </summary>
            [TitleGroup("공간 생성"), BoxGroup("공간 생성/박스", false)]
            [BoxGroup("공간 생성/박스/공간 분할")]
            [EnableGUI, ShowInInspector]
            [LabelText("분할 재귀 중단 확률")]
            [PropertyOrder(3)]
            [PropertyRange(0f, 1f)]
            public float StopDivideProbability
            {
                get => stopDivideProbability;
                set
                {
                    stopDivideProbability = value.SetClamp(0, 1);
                }
            }
            [SerializeField, HideInInspector] private float stopDivideProbability = 0.5f;



            /// <summary>
            /// 공간을 최대한 작게 분할할 정도
            /// </summary>
            [TitleGroup("공간 생성"), BoxGroup("공간 생성/박스", false)]
            [BoxGroup("공간 생성/박스/공간 분할")]
            [EnableGUI, ShowInInspector]
            [LabelText("분할 최소치 무작위성")]
            [PropertyOrder(3)]
            [PropertyRange(0f, 1f)]
            public float DivideSmallASAPRandomLength
            {
                get => divideSmallASAPRandomLength;
                set
                {
                    divideSmallASAPRandomLength = value.SetClamp(0, 1);
                }
            }
            [SerializeField, HideInInspector] private float divideSmallASAPRandomLength = 0.5f;



            #endregion



            ///======================================================================================================================================================



            #region 공간 크기



            /// <summary>
            /// 공간의 최소 너비<br/>
            /// (이 값 + 스테이지의 총 너비 + 공간을나누는 단위값에 값에 따라, 이 너비보다 작은 공간이 나올수도 있음, 최대공배수가 같으면 안나옴)
            /// </summary>
            [TitleGroup("공간 생성"), BoxGroup("공간 생성/박스", false)]
            [BoxGroup("공간 생성/박스/공간 크기")]
            [HorizontalGroup("공간 생성/박스/공간 크기/최소크기가로")]
            [EnableGUI, ShowInInspector]
            [LabelText("최소 크기")]
            [LabelWidth(60)]
            [PropertyOrder(4)]
            [DelayedProperty]
            public int SpaceWidthMin
            {
                get => spaceWidthMin;
                set
                {
                    //. 공간 분할 단위 ~ 공간 최대 너비 만큼 제약한다
                    spaceWidthMin.SetClamp(value, divideSpaceUnit, spaceWidthMax);

                    //. 노드 생성에 필요한 최소 공간 너비를 갱신한다
                    Main.spaceNodeConnect.NodeLimit_MinimumSpaceWidth = Main.spaceNodeConnect.NodeLimit_MinimumSpaceWidth;

                    //. 공간 크기 높이 노드 제약의 너비 최소 크기를 갱신한다
                    Main.spaceNodeConnect.NodeLimit_SpaceSize_Height_ExtendLimitWidth = Main.spaceNodeConnect.NodeLimit_SpaceSize_Height_ExtendLimitWidth;
                }
            }
            [SerializeField, HideInInspector] private int spaceWidthMin = 10;



            /// <summary>
            /// 공간의 최소 높이<br/>
            /// (이 값 + 스테이지의 총 높이 + 공간을나누는 단위값에 값에 따라, 이 높이보다 작은 공간이 나올수도 있음, 최대공배수가 같으면 안나옴)
            /// </summary>
            [TitleGroup("공간 생성"), BoxGroup("공간 생성/박스", false)]
            [BoxGroup("공간 생성/박스/공간 크기")]
            [HorizontalGroup("공간 생성/박스/공간 크기/최소크기가로")]
            [EnableGUI, ShowInInspector]
            [LabelText(" x")]
            [LabelWidth(20)]
            [PropertyOrder(4)]
            [DelayedProperty]
            public int SpaceHeightMin
            {
                get => spaceHeightMin;
                set
                {
                    //. 공간 분할 단위 ~ 공간 최대 높이 만큼 제약한다
                    spaceHeightMin.SetClamp(value, divideSpaceUnit, spaceHeightMax);

                    //. 노드 생성에 필요한 최소 공간 높이를 갱신한다
                    Main.spaceNodeConnect.NodeLimit_MinimumSpaceHeight = Main.spaceNodeConnect.NodeLimit_MinimumSpaceHeight;

                    //. 공간 크기 너비 노드 제약의 높이 최소 크기를 갱신한다
                    Main.spaceNodeConnect.NodeLimit_SpaceSize_Width_ExtendLimitHeight = Main.spaceNodeConnect.NodeLimit_SpaceSize_Width_ExtendLimitHeight;
                }
            }
            [SerializeField, HideInInspector] private int spaceHeightMin = 10;



            /// <summary>
            /// 공간의 최대 너비
            /// </summary>
            [TitleGroup("공간 생성"), BoxGroup("공간 생성/박스", false)]
            [BoxGroup("공간 생성/박스/공간 크기")]
            [HorizontalGroup("공간 생성/박스/공간 크기/최대크기가로")]
            [EnableGUI, ShowInInspector]
            [LabelText("최대 크기")]
            [LabelWidth(60)]
            [PropertyOrder(4)]
            [DelayedProperty]
            public int SpaceWidthMax
            {
                get => spaceWidthMax;
                set
                {
                    //. 공간 분할 단위 ~ 스테이지 너비 만큼 제약한다
                    spaceWidthMax.SetClamp(value, divideSpaceUnit, Main.stageVector.StageWidth);

                    //. 공간 크기 너비 노드 제약, 공간 크기 높이 노드 제약의 너비 최소 크기를 갱신한다
                    Main.spaceNodeConnect.NodeLimit_SpaceSize_Width = Main.spaceNodeConnect.NodeLimit_SpaceSize_Width;
                    Main.spaceNodeConnect.NodeLimit_SpaceSize_Height_ExtendLimitWidth = Main.spaceNodeConnect.NodeLimit_SpaceSize_Height_ExtendLimitWidth;

                    //. 노드 생성에 필요한 최소 공간 너비를 갱신한다
                    Main.spaceNodeConnect.NodeLimit_MinimumSpaceWidth = Main.spaceNodeConnect.NodeLimit_MinimumSpaceWidth;

                    //. 방 생성 최소 공간 너비를 갱신한다
                    Main.room.RoomPlacementMinWidth = Main.room.RoomPlacementMinWidth;
                }
            }
            [SerializeField, HideInInspector] private int spaceWidthMax = 10;



            /// <summary>
            /// 공간의 최대 높이
            /// </summary>
            [TitleGroup("공간 생성"), BoxGroup("공간 생성/박스", false)]
            [BoxGroup("공간 생성/박스/공간 크기")]
            [HorizontalGroup("공간 생성/박스/공간 크기/최대크기가로")]
            [EnableGUI, ShowInInspector]
            [LabelText(" x")]
            [LabelWidth(20)]
            [PropertyOrder(4)]
            [DelayedProperty]
            public int SpaceHeightMax
            {
                get => spaceHeightMax;
                set
                {
                    //. 공간 분할 단위 ~ 스테이지 높이 만큼 제약한다
                    spaceHeightMax.SetClamp(value, divideSpaceUnit, Main.stageVector.StageHeight);

                    //. 공간 크기 높이 노드 제약, 공간 크기 너비 노드 제약의 높이 최소 크기를 갱신한다
                    Main.spaceNodeConnect.NodeLimit_SpaceSize_Height = Main.spaceNodeConnect.NodeLimit_SpaceSize_Height;
                    Main.spaceNodeConnect.NodeLimit_SpaceSize_Width_ExtendLimitHeight = Main.spaceNodeConnect.NodeLimit_SpaceSize_Width_ExtendLimitHeight;

                    //. 노드 생성에 필요한 최소 공간 높이를 갱신한다
                    Main.spaceNodeConnect.NodeLimit_MinimumSpaceHeight = Main.spaceNodeConnect.NodeLimit_MinimumSpaceHeight;

                    //. 방 생성 최소 공간 높이를 갱신한다
                    Main.room.RoomPlacementMinHeight = Main.room.RoomPlacementMinHeight;
                }
            }
            [SerializeField, HideInInspector] private int spaceHeightMax = 10;



            #endregion



            ///======================================================================================================================================================



            public override void Copy(Spaces original)
            {
                SpaceCountMin = original.SpaceCountMin;
                SpaceCountMax = original.SpaceCountMax;

                SpaceDivideUnit = original.SpaceDivideUnit;
                SpaceDivideMode = original.SpaceDivideMode;

                SpaceWidthMin = original.SpaceWidthMin;
                SpaceWidthMax = original.SpaceWidthMax;
                SpaceHeightMin = original.SpaceHeightMin;
                SpaceHeightMax = original.SpaceHeightMax;

                StopDivideProbability = original.StopDivideProbability;
                DivideSmallASAPRandomLength = original.DivideSmallASAPRandomLength;
            }



            public override void Refresh()
            {
                SpaceCountMin = SpaceCountMin;
                SpaceCountMax = SpaceCountMax;

                SpaceDivideUnit = SpaceDivideUnit;
                //SpaceDivideMode = SpaceDivideMode;

                SpaceWidthMin = SpaceWidthMin;
                SpaceWidthMax = SpaceWidthMax;
                SpaceHeightMin = SpaceHeightMin;
                SpaceHeightMax = SpaceHeightMax;

                StopDivideProbability = StopDivideProbability;
                DivideSmallASAPRandomLength = DivideSmallASAPRandomLength;
            }



            ///======================================================================================================================================================
        }



        public Spaces Space => space;
        [InlineProperty, HideLabel, HideReferenceObjectPicker, SerializeField, ShowIf(nameof(IsValid))]
        private Spaces space = new();



        ///======================================================================================================================================================



        //? 공간 노드 연결



        [Serializable]
        public class SpaceNodeConnects : BaseMain<SpaceNodeConnects>
        {
            ///======================================================================================================================================================



            #region 노드 정보



            /// <summary>
            /// 공간들을 이어주는 노드의 길이 하나로 이어지게 할지 여부
            /// </summary>
            [TitleGroup("공간 노드 연결"), BoxGroup("공간 노드 연결/박스", false)]
            [BoxGroup("공간 노드 연결/박스/노드 정보")]
            [EnableGUI, ShowInInspector]
            [LabelText("⛓️최대한 모든 공간 노드 연결")]
            [LabelWidth(200)]
            [PropertyOrder(1)]
            [PropertyTooltip("최대한 그 어떤 노드에서 시작하더라도 모든 공간에 닿을수 있게끔 한다\n 최대 노드 개수에 따라, 반드시 보장되지는 않는다")]
            public bool UseCombineSpaceNodes
            {
                get => useCombineSpaceNodes;
                private set
                {
                    useCombineSpaceNodes = value;
                }
            }
            [SerializeField, HideInInspector] private bool useCombineSpaceNodes = true;



            public enum EConnectOneMode
            {
                /// <summary>
                /// 매 순회마다 전체 리스트를 항상 안전하게 갱신<br/>
                /// 공간끼리 연결할때, 다른 공간의 연결점에 영향을 준다면 이 모드를 사용
                /// </summary>
                [LabelText("안전하게 매번 갱신")]
                RefreshLoopSafety,

                /// <summary>
                /// 맨 처음에만 전체 리스트를 갱신<br/>
                /// 공간끼리 연결할때, 다른 공간의 연결점에 영향을 주지 않는다면 이 모드를 사용
                /// </summary>
                [LabelText("한번만 갱신")]
                RefreshOnece
            }



            ///<summary>
            ///공간을 연결할때의 모드
            ///</summary>
            [TitleGroup("공간 노드 연결"), BoxGroup("공간 노드 연결/박스", false)]
            [BoxGroup("공간 노드 연결/박스/노드 정보")]
            [EnableGUI, ShowInInspector]
            [LabelText("연결 모드")]
            [PropertyOrder(1)]
            public EConnectOneMode ConnectOneMode
            {
                get => connectOneMode;
                private set
                {
                    connectOneMode = value;
                }
            }
            [SerializeField, HideInInspector] private EConnectOneMode connectOneMode = EConnectOneMode.RefreshOnece;



            //? 최대 공간 노드 개수 



            /// <summary>
            /// 노드 최대 개수 (각각의 공간 기준)
            /// </summary>
            [TitleGroup("공간 노드 연결"), BoxGroup("공간 노드 연결/박스", false)]
            [BoxGroup("공간 노드 연결/박스/노드 정보")]
            [ShowInInspector]
            [LabelText("최대 노드 개수")]
            [LabelWidth(100)]
            [PropertyOrder(1)]
            [DelayedProperty]
            public int NodeCountMax
            {
                get => nodeCountMax;
                private set
                {
                    nodeCountMax.SetClampMin(value, nodeMaxCount_Down + nodeMaxCount_Up + nodeMaxCount_Left + nodeMaxCount_Right);
                }
            }
            [SerializeField, HideInInspector] private int nodeCountMax = 4;



            /// <summary>
            /// 하단 노드 최대 개수
            /// </summary>
            [TitleGroup("공간 노드 연결"), BoxGroup("공간 노드 연결/박스", false)]
            [BoxGroup("공간 노드 연결/박스/노드 정보")]
            [HorizontalGroup("공간 노드 연결/박스/노드 정보/최대개수가로")]
            [ShowInInspector]
            [LabelText("↓하")]
            [LabelWidth(30)]
            [PropertyOrder(1)]
            [DelayedProperty]
            public int NodeMaxCount_Down
            {
                get => nodeMaxCount_Down;
                private set
                {
                    nodeMaxCount_Down = value.SetClamp0();

                    //. 최대 노드 개수 갱신
                    NodeCountMax = NodeCountMax;
                }
            }
            [SerializeField, HideInInspector] private int nodeMaxCount_Down = 1;



            /// <summary>
            /// 상단 노드 최대 개수
            /// </summary>
            [TitleGroup("공간 노드 연결"), BoxGroup("공간 노드 연결/박스", false)]
            [BoxGroup("공간 노드 연결/박스/노드 정보")]
            [HorizontalGroup("공간 노드 연결/박스/노드 정보/최대개수가로")]
            [ShowInInspector]
            [LabelText("↑상")]
            [LabelWidth(30)]
            [PropertyOrder(1)]
            [DelayedProperty]
            public int NodeMaxCount_Up
            {
                get => nodeMaxCount_Up;
                private set
                {
                    nodeMaxCount_Up = value.SetClamp0();

                    //. 최대 노드 개수 갱신
                    NodeCountMax = NodeCountMax;
                }
            }
            [SerializeField, HideInInspector] private int nodeMaxCount_Up = 1;



            /// <summary>
            /// 좌측 노드 최대 개수
            /// </summary>
            [TitleGroup("공간 노드 연결"), BoxGroup("공간 노드 연결/박스", false)]
            [BoxGroup("공간 노드 연결/박스/노드 정보")]
            [HorizontalGroup("공간 노드 연결/박스/노드 정보/최대개수가로")]
            [ShowInInspector]
            [LabelText("←좌")]
            [LabelWidth(30)]
            [PropertyOrder(1)]
            [DelayedProperty]
            public int NodeMaxCount_Left
            {
                get => nodeMaxCount_Left;
                private set
                {
                    nodeMaxCount_Left = value.SetClamp0();

                    //. 최대 노드 개수 갱신
                    NodeCountMax = NodeCountMax;
                }
            }
            [SerializeField, HideInInspector] private int nodeMaxCount_Left = 1;



            /// <summary>
            /// 우측 노드 최대 개수
            /// </summary>
            [TitleGroup("공간 노드 연결"), BoxGroup("공간 노드 연결/박스", false)]
            [BoxGroup("공간 노드 연결/박스/노드 정보")]
            [HorizontalGroup("공간 노드 연결/박스/노드 정보/최대개수가로")]
            [ShowInInspector]
            [LabelText("→우")]
            [LabelWidth(30)]
            [PropertyOrder(1)]
            [DelayedProperty]
            public int NodeMaxCount_Right
            {
                get => nodeMaxCount_Right;
                private set
                {
                    nodeMaxCount_Right = value.SetClamp0();

                    //. 최대 노드 개수 갱신
                    NodeCountMax = NodeCountMax;
                }
            }
            [SerializeField, HideInInspector] private int nodeMaxCount_Right = 1;



            #endregion



            #region 공간 노드 제약 (공간 크기)



            #region 노드 생성 제약 (최소의 공간 크기)



            /// <summary>
            /// 공간의 너비(좌/우) 방향의 노드를 생성하기 위한 최소 너비 값을 제약할지의 여부
            /// </summary>
            [TitleGroup("공간 노드 연결"), BoxGroup("공간 노드 연결/박스", false)]
            [BoxGroup("공간 노드 연결/박스/노드 생성 제약")]
            [HorizontalGroup("공간 노드 연결/박스/노드 생성 제약/너비가로")]
            [EnableGUI, ShowInInspector]
            [LabelText("노드 생성에 필요한 최소 ↔너비")]
            [LabelWidth(180)]
            [PropertyOrder(1)]
            [PropertyTooltip("공간의 너비가 이 값보다 커야만, 노드가 생성된다 \n(노드가 생성되는 최소 너비)")]
            public bool UseNodeLimit_MinimumSpaceWidth
            {
                get => useNodeLimit_MinimumSpaceWidth;
                private set
                {
                    useNodeLimit_MinimumSpaceWidth = value;

                    //. 노드 생성에 필요한 최소 너비값도 갱신시켜준다
                    NodeLimit_MinimumSpaceWidth = NodeLimit_MinimumSpaceWidth;
                }
            }
            [SerializeField, HideInInspector] private bool useNodeLimit_MinimumSpaceWidth;



            /// <summary>
            /// 공간의 너비(좌/우) 방향의 노드를 생성하기 위한 최소 너비 값
            /// </summary>
            [TitleGroup("공간 노드 연결"), BoxGroup("공간 노드 연결/박스", false)]
            [BoxGroup("공간 노드 연결/박스/노드 생성 제약")]
            [HorizontalGroup("공간 노드 연결/박스/노드 생성 제약/너비가로")]
            [ShowInInspector]
            [EnableIf(nameof(UseNodeLimit_MinimumSpaceWidth))]
            [HideLabel]
            [LabelWidth(20)]
            [PropertyOrder(1)]
            public int NodeLimit_MinimumSpaceWidth
            {
                get => nodeLimit_MinimumSpaceWidth;
                set
                {
                    nodeLimit_MinimumSpaceWidth.SetClamp(value, Main.space.SpaceWidthMin, Main.space.SpaceWidthMax);
                }
            }
            [SerializeField, HideInInspector] private int nodeLimit_MinimumSpaceWidth;



            /// <summary>
            /// 공간의 높이(하/상) 방향의 노드를 생성하기 위한 최소 높이값을 제약할지의 여부
            /// </summary>
            [TitleGroup("공간 노드 연결"), BoxGroup("공간 노드 연결/박스", false)]
            [BoxGroup("공간 노드 연결/박스/노드 생성 제약")]
            [HorizontalGroup("공간 노드 연결/박스/노드 생성 제약/높이가로")]
            [EnableGUI, ShowInInspector]
            [LabelText("노드 생성에 필요한 최소 ↕높이")]
            [LabelWidth(180)]
            [PropertyOrder(1)]
            [PropertyTooltip("공간의 높이가 이 값보다 커야만, 노드가 생성된다 \n(노드가 생성되는 최소 높이)")]
            public bool UseNodeLimit_MinimumSpaceHeight
            {
                get => useNodeLimit_MinimumSpaceHeight;
                private set
                {
                    useNodeLimit_MinimumSpaceHeight = value;

                    //. 노드 생성에 필요한 최소 높이값도 갱신시켜준다
                    NodeLimit_MinimumSpaceHeight = NodeLimit_MinimumSpaceHeight;
                }
            }
            [SerializeField, HideInInspector] private bool useNodeLimit_MinimumSpaceHeight;



            /// <summary>
            /// 공간의 높이(하/상) 방향의 노드를 생성하기 위한 최소 높이 값
            /// </summary>
            [TitleGroup("공간 노드 연결"), BoxGroup("공간 노드 연결/박스", false)]
            [BoxGroup("공간 노드 연결/박스/노드 생성 제약")]
            [HorizontalGroup("공간 노드 연결/박스/노드 생성 제약/높이가로")]
            [ShowInInspector]
            [EnableIf(nameof(UseNodeLimit_MinimumSpaceHeight))]
            [HideLabel]
            [LabelWidth(20)]
            [PropertyOrder(1)]
            public int NodeLimit_MinimumSpaceHeight
            {
                get => nodeLimit_MinimumSpaceHeight;
                set
                {
                    nodeLimit_MinimumSpaceHeight.SetClamp(value, Main.space.SpaceHeightMin, Main.space.SpaceHeightMax);
                }
            }
            [SerializeField, HideInInspector] private int nodeLimit_MinimumSpaceHeight;



            #endregion



            #region 노드 생성 개수 제약 (공간 크기)



            /// <summary>
            /// 공간의 노드 개수를 제약 (공간의 크기에 따라서)
            /// </summary>
            [TitleGroup("공간 노드 연결"), BoxGroup("공간 노드 연결/박스", false)]
            [BoxGroup("공간 노드 연결/박스/노드 생성 개수 제약 (공간 크기)")]
            [EnableGUI, ShowInInspector]
            [LabelText("공간의 크기로 노드 제약")]
            [LabelWidth(150)]
            [PropertyOrder(1)]
            [InfoBox("공간의 너비/높이의 크기에 따라, 해당 공간의 노드 개수를 제약한다\n지정된 값 (너비/높이) 단위로 노드가 생성되지며, 각각 방향별 최대 개수는 초과 할 수 없다")]
            public bool UseNodeLimit_SpaceSize
            {
                get => useNodeLimit_SpaceSize;
                private set
                {
                    useNodeLimit_SpaceSize = value;
                    if (!value)
                    {
                        UseNodeLimit_SpaceSize_Width = false;
                        UseNodeLimit_SpaceSize_Width_ExtendLimitHeight = false;
                        UseNodeLimit_SpaceSize_Height = false;
                        useNodeLimit_SpaceSize_Height_ExtendLimitWidth = false;
                    }
                }
            }
            [SerializeField, HideInInspector] private bool useNodeLimit_SpaceSize;



            //? 공간 너비 노드 제약



            /// <summary>
            /// 공간의 너비에 따라, 노드를 제약할 지 여부 (하단, 상단 노드가 제약됨)
            /// </summary>
            [TitleGroup("공간 노드 연결"), BoxGroup("공간 노드 연결/박스", false)]
            [BoxGroup("공간 노드 연결/박스/노드 생성 개수 제약 (공간 크기)")]
            [HorizontalGroup("공간 노드 연결/박스/노드 생성 개수 제약 (공간 크기)/너비노드단위가로")]
            [ShowInInspector]
            [EnableIf(nameof(UseNodeLimit_SpaceSize))]
            [LabelText("공간 ↔너비 노드 단위")]
            [LabelWidth(200)]
            [Indent(1)]
            [PropertyOrder(1)]
            [PropertyTooltip("공간의 너비 / 이 값 = N (소수점 절삭) 만큼 하단/상단 노드가 해당 공간에 제약된다")]
            public bool UseNodeLimit_SpaceSize_Width
            {
                get => useNodeLimit_SpaceSize_Width;
                private set
                {
                    useNodeLimit_SpaceSize_Width = value;

                    //. 값도 갱신시켜준다
                    NodeLimit_SpaceSize_Width = NodeLimit_SpaceSize_Width;
                }
            }
            [SerializeField, HideInInspector] private bool useNodeLimit_SpaceSize_Width;



            /// <summary>
            /// 공간의 너비에 따라, 노드를 제약 하는 단위(하단, 상단 노드가 제약됨)
            /// <para>이 값 만큼 공간 너비에서 나누어진 값 만큼 (소수점 절삭) 하단/상단 노드가 제약된다</para>
            /// </summary>
            [TitleGroup("공간 노드 연결"), BoxGroup("공간 노드 연결/박스", false)]
            [BoxGroup("공간 노드 연결/박스/노드 생성 개수 제약 (공간 크기)")]
            [HorizontalGroup("공간 노드 연결/박스/노드 생성 개수 제약 (공간 크기)/너비노드단위가로", 0.3f)]
            [ShowInInspector]
            [EnableIf(nameof(UseNodeLimit_SpaceSize_Width))]
            [HideLabel]
            [PropertyOrder(1)]
            public int NodeLimit_SpaceSize_Width
            {
                get => nodeLimit_SpaceSize_Width;
                set
                {
                    //. 0 ~ 최대 공간 너비 만큼 제약한다
                    nodeLimit_SpaceSize_Width.SetClamp(value, 0, Main.space.SpaceWidthMax);
                }
            }
            [SerializeField, HideInInspector] private int nodeLimit_SpaceSize_Width = 0;



            //? 공간 너비 노드 제약 + 제약, 높이 제약



            /// <summary>
            /// 공간의 너비를 따라, 노드를 제약할려고 할 때의 추가 제약 (공간 높이 최소값)
            /// </summary>
            [TitleGroup("공간 노드 연결"), BoxGroup("공간 노드 연결/박스", false)]
            [BoxGroup("공간 노드 연결/박스/노드 생성 개수 제약 (공간 크기)")]
            [HorizontalGroup("공간 노드 연결/박스/노드 생성 개수 제약 (공간 크기)/너비제약추가높이최소크기")]
            [ShowInInspector]
            [EnableIf(nameof(UseNodeLimit_SpaceSize_Width))]
            [LabelText("┗추가 ↕높이 최소 크기")]
            [LabelWidth(200)]
            [Indent(2)]
            [PropertyOrder(1)]
            [PropertyTooltip("공간 너비 단위를 제약하려고 할 때, 추가로 하는 제약\n해당 공간의 높이가 이 값보다 같거나 높아야만, 공간 너비 단위로 제약이 된다")]
            public bool UseNodeLimit_SpaceSize_Width_ExtendLimitHeight
            {
                get => useNodeLimit_SpaceSize_Width_ExtendLimitHeight;
                private set
                {
                    useNodeLimit_SpaceSize_Width_ExtendLimitHeight = value;

                    //. 값도 갱신시켜준다
                    NodeLimit_SpaceSize_Width_ExtendLimitHeight = NodeLimit_SpaceSize_Width_ExtendLimitHeight;
                }
            }
            [SerializeField, HideInInspector] private bool useNodeLimit_SpaceSize_Width_ExtendLimitHeight;



            /// <summary>
            /// 공간의 너비를 따라, 노드를 제약할려고 할 때, 그 공간의 높이가 이 값보다 높아야 한다
            /// </summary>
            [TitleGroup("공간 노드 연결"), BoxGroup("공간 노드 연결/박스", false)]
            [BoxGroup("공간 노드 연결/박스/노드 생성 개수 제약 (공간 크기)")]
            [HorizontalGroup("공간 노드 연결/박스/노드 생성 개수 제약 (공간 크기)/너비제약추가높이최소크기", 0.3f)]
            [ShowInInspector]
            [EnableIf(nameof(UseNodeLimit_SpaceSize_Width_ExtendLimitHeight))]
            [HideLabel]
            [LabelWidth(200)]
            [PropertyOrder(1)]
            public int NodeLimit_SpaceSize_Width_ExtendLimitHeight
            {
                get => nodeLimit_SpaceSize_Width_ExtendLimitHeight;
                set
                {
                    //. 최소값을 공간의 최소 높이로 제약한다
                    nodeLimit_SpaceSize_Width_ExtendLimitHeight.SetClampMin(value, Main.space.SpaceHeightMin);
                }
            }
            [SerializeField, HideInInspector] private int nodeLimit_SpaceSize_Width_ExtendLimitHeight = 0;



            //? 공간 높이 노드 제약



            /// <summary>
            /// 공간의 높이에 따라, 노드를 제약할 지 여부 (좌측, 우측 노드가 제약됨)
            /// </summary>
            [TitleGroup("공간 노드 연결"), BoxGroup("공간 노드 연결/박스", false)]
            [BoxGroup("공간 노드 연결/박스/노드 생성 개수 제약 (공간 크기)")]
            [HorizontalGroup("공간 노드 연결/박스/노드 생성 개수 제약 (공간 크기)/높이노드단위가로")]
            [ShowInInspector]
            [EnableIf(nameof(UseNodeLimit_SpaceSize))]
            [LabelText("공간 ↕높이 노드 단위")]
            [LabelWidth(200)]
            [Indent(1)]
            [PropertyOrder(1)]
            [PropertyTooltip("공간의 높이 / 이 값 = N (소수점 절삭) 만큼 좌측/우측 노드가 해당 공간에 제약된다")]
            public bool UseNodeLimit_SpaceSize_Height
            {
                get => useNodeLimit_SpaceSize_Height;
                private set
                {
                    useNodeLimit_SpaceSize_Height = value;

                    //. 값도 갱신시켜준다
                    NodeLimit_SpaceSize_Height = NodeLimit_SpaceSize_Height;
                }
            }
            [SerializeField, HideInInspector] private bool useNodeLimit_SpaceSize_Height;



            /// <summary>
            /// 공간의 높이에 따라, 노드를 제약 하는 단위(좌측, 우측 노드가 제약됨)
            /// <para>이 값 만큼 공간 높이에서 나누어진 값 만큼 (소수점 절삭) 좌측/우측 노드가 제약된다</para>
            /// </summary>
            [TitleGroup("공간 노드 연결"), BoxGroup("공간 노드 연결/박스", false)]
            [BoxGroup("공간 노드 연결/박스/노드 생성 개수 제약 (공간 크기)")]
            [HorizontalGroup("공간 노드 연결/박스/노드 생성 개수 제약 (공간 크기)/높이노드단위가로", 0.3f)]
            [ShowInInspector]
            [EnableIf(nameof(UseNodeLimit_SpaceSize_Height))]
            [HideLabel]
            [PropertyOrder(1)]
            public int NodeLimit_SpaceSize_Height
            {
                get => nodeLimit_SpaceSize_Height;
                set
                {
                    //. 0 ~ 최대 공간 너비 만큼 제약한다
                    nodeLimit_SpaceSize_Height.SetClamp(value, 0, Main.space.SpaceHeightMax);
                }
            }
            [SerializeField, HideInInspector] private int nodeLimit_SpaceSize_Height = 0;



            //? 공간 높이 노드 제약 + 제약, 너비 제약



            /// <summary>
            /// 공간의 높이를 따라, 노드를 제약할려고 할 때의 추가 제약 (공간 너비 최소값)
            /// </summary>
            [TitleGroup("공간 노드 연결"), BoxGroup("공간 노드 연결/박스", false)]
            [BoxGroup("공간 노드 연결/박스/노드 생성 개수 제약 (공간 크기)")]
            [HorizontalGroup("공간 노드 연결/박스/노드 생성 개수 제약 (공간 크기)/높이제약추가너비최소크기")]
            [ShowInInspector]
            [EnableIf(nameof(UseNodeLimit_SpaceSize_Height))]
            [LabelText("┗추가 ↔너비 최소 크기")]
            [LabelWidth(200)]
            [Indent(2)]
            [PropertyOrder(1)]
            [PropertyTooltip("공간 높이 단위를 제약하려고 할 때, 추가로 하는 제약\n해당 공간의 너비가 이 값보다 같거나 넓어야만, 공간 높이 단위로 제약이 된다")]
            public bool UseNodeLimit_SpaceSize_Height_ExtendLimitWidth
            {
                get => useNodeLimit_SpaceSize_Height_ExtendLimitWidth;
                private set
                {
                    useNodeLimit_SpaceSize_Height_ExtendLimitWidth = value;

                    //. 값도 갱신시켜준다
                    NodeLimit_SpaceSize_Height_ExtendLimitWidth = NodeLimit_SpaceSize_Height_ExtendLimitWidth;
                }
            }
            [SerializeField, HideInInspector] private bool useNodeLimit_SpaceSize_Height_ExtendLimitWidth;



            /// <summary>
            /// 공간의 높이를 따라, 노드를 제약할려고 할 때, 그 공간의 너비가 이 값보다 넓어야 한다
            /// </summary>
            [TitleGroup("공간 노드 연결"), BoxGroup("공간 노드 연결/박스", false)]
            [BoxGroup("공간 노드 연결/박스/노드 생성 개수 제약 (공간 크기)")]
            [HorizontalGroup("공간 노드 연결/박스/노드 생성 개수 제약 (공간 크기)/높이제약추가너비최소크기", 0.3f)]
            [ShowInInspector]
            [EnableIf(nameof(UseNodeLimit_SpaceSize_Height_ExtendLimitWidth))]
            [HideLabel]
            [LabelWidth(200)]
            [PropertyOrder(1)]
            public int NodeLimit_SpaceSize_Height_ExtendLimitWidth
            {
                get => nodeLimit_SpaceSize_Height_ExtendLimitWidth;
                set
                {
                    //. 최소값을 공간의 최소 너비로 제약한다
                    nodeLimit_SpaceSize_Height_ExtendLimitWidth.SetClampMin(value, Main.space.SpaceWidthMin);
                }
            }
            [SerializeField, HideInInspector] private int nodeLimit_SpaceSize_Height_ExtendLimitWidth = 0;



            #endregion



            #endregion



            ///======================================================================================================================================================



            public override void Copy(SpaceNodeConnects original)
            {
                NodeMaxCount_Down = original.NodeMaxCount_Down;
                NodeMaxCount_Up = original.NodeMaxCount_Up;
                NodeMaxCount_Left = original.NodeMaxCount_Left;
                NodeMaxCount_Right = original.NodeMaxCount_Right;
                UseCombineSpaceNodes = original.UseCombineSpaceNodes;

                UseNodeLimit_SpaceSize = original.UseNodeLimit_SpaceSize;

                UseNodeLimit_SpaceSize_Width = original.UseNodeLimit_SpaceSize_Width;
                NodeLimit_SpaceSize_Width = original.NodeLimit_SpaceSize_Width;
                UseNodeLimit_SpaceSize_Width_ExtendLimitHeight = original.UseNodeLimit_SpaceSize_Width_ExtendLimitHeight;
                NodeLimit_SpaceSize_Width_ExtendLimitHeight = original.NodeLimit_SpaceSize_Width_ExtendLimitHeight;

                UseNodeLimit_SpaceSize_Height = original.UseNodeLimit_SpaceSize_Height;
                NodeLimit_SpaceSize_Height = original.NodeLimit_SpaceSize_Height;
                UseNodeLimit_SpaceSize_Height_ExtendLimitWidth = original.UseNodeLimit_SpaceSize_Height_ExtendLimitWidth;
                NodeLimit_SpaceSize_Height_ExtendLimitWidth = original.NodeLimit_SpaceSize_Height_ExtendLimitWidth;

                UseNodeLimit_MinimumSpaceWidth = original.UseNodeLimit_MinimumSpaceWidth;
                UseNodeLimit_MinimumSpaceHeight = original.UseNodeLimit_MinimumSpaceHeight;
                NodeLimit_MinimumSpaceWidth = original.NodeLimit_MinimumSpaceWidth;
                NodeLimit_MinimumSpaceHeight = original.NodeLimit_MinimumSpaceHeight;
                ConnectOneMode = original.ConnectOneMode;
            }



            public override void Refresh()
            {
                NodeMaxCount_Down = NodeMaxCount_Down;
                NodeMaxCount_Up = NodeMaxCount_Up;
                NodeMaxCount_Left = NodeMaxCount_Left;
                NodeMaxCount_Right = NodeMaxCount_Right;
                UseCombineSpaceNodes = UseCombineSpaceNodes;

                UseNodeLimit_SpaceSize = UseNodeLimit_SpaceSize;

                UseNodeLimit_SpaceSize_Width = UseNodeLimit_SpaceSize_Width;
                NodeLimit_SpaceSize_Width = NodeLimit_SpaceSize_Width;
                UseNodeLimit_SpaceSize_Width_ExtendLimitHeight = UseNodeLimit_SpaceSize_Width_ExtendLimitHeight;
                NodeLimit_SpaceSize_Width_ExtendLimitHeight = NodeLimit_SpaceSize_Width_ExtendLimitHeight;

                UseNodeLimit_SpaceSize_Height = UseNodeLimit_SpaceSize_Height;
                NodeLimit_SpaceSize_Height = NodeLimit_SpaceSize_Height;
                UseNodeLimit_SpaceSize_Height_ExtendLimitWidth = UseNodeLimit_SpaceSize_Height_ExtendLimitWidth;
                NodeLimit_SpaceSize_Height_ExtendLimitWidth = NodeLimit_SpaceSize_Height_ExtendLimitWidth;

                UseNodeLimit_MinimumSpaceWidth = UseNodeLimit_MinimumSpaceWidth;
                UseNodeLimit_MinimumSpaceHeight = UseNodeLimit_MinimumSpaceHeight;
                NodeLimit_MinimumSpaceWidth = NodeLimit_MinimumSpaceWidth;
                NodeLimit_MinimumSpaceHeight = NodeLimit_MinimumSpaceHeight;
                ConnectOneMode = ConnectOneMode;
            }



            ///======================================================================================================================================================


        }



        public SpaceNodeConnects SpaceNodeConnect => spaceNodeConnect;
        [InlineProperty, HideLabel, HideReferenceObjectPicker, SerializeField, ShowIf(nameof(IsValid))]
        private SpaceNodeConnects spaceNodeConnect = new();



        ///======================================================================================================================================================



        //? 방



        [Serializable]
        public class Rooms : BaseMain<Rooms>
        {
            ///======================================================================================================================================================



            #region 방 배치



            /// <summary>
            /// <b>방 배치 무작위성</b><br/>
            /// 0이라면, 무작위로 배치되지 않는다<br/>
            /// 값이 커질수록, 지정된 위치에서 벗어나 배치될 확률이 높아진다<br/>
            /// (최대값: 1)
            /// </summary>
            [TitleGroup("방"), BoxGroup("방/박스", false)]
            [BoxGroup("방/박스/방 배치")]
            [ShowInInspector]
            [DisableIf(nameof(UseRoomPlaceNearest))]
            [LabelText("방 배치 무작위성")]
            [PropertyOrder(1)]
            [PropertyRange(0f, 1f)]
            public float RoomPlaceRandomness
            {
                get => roomPlaceRandomness;
                set
                {
                    roomPlaceRandomness = value;
                    roomPlaceRandomness.SetClamp01();
                }
            }
            [SerializeField, HideInInspector] private float roomPlaceRandomness = 0.5f;



            /// <summary>
            /// <b>방 배치 인접성</b> 을 사용할지 여부
            /// </summary>
            [TitleGroup("방"), BoxGroup("방/박스", false)]
            [BoxGroup("방/박스/방 배치")]
            [EnableGUI, ShowInInspector]
            [LabelText("방 배치 인접성")]
            [PropertyOrder(1)]
            public bool UseRoomPlaceNearest
            {
                get => useRoomPlaceNearest;
                set
                {
                    useRoomPlaceNearest = value;
                    if (!value)
                    {
                        UseRoomPlaceNearest_PostPlace = false;
                    }
                }
            }
            [SerializeField, HideInInspector] private bool useRoomPlaceNearest = false;



            [TitleGroup("방"), BoxGroup("방/박스", false)]
            [BoxGroup("방/박스/방 배치")]
            [HorizontalGroup("방/박스/방 배치/1차인접단방향가로")]
            [ShowInInspector]
            [EnableIf(nameof(UseRoomPlaceNearest))]
            [LabelText("1차 인접 단방향 확장인접 사용")]
            [PropertyTooltip("단방향, 단일 방에 연결된 노드 방향의 반대축에 해당하는 축을 이동한다\n이 연산을 사용할때, 여러번 반복해야 의도대로 인접한다")]
            [LabelWidth(225)]
            [Indent(1)]
            [PropertyOrder(1)]
            public bool RoomPlaceNearest_First_UseOneWayNearExpand
            {
                get => UseRoomPlaceNearest && roomPlaceNearest_First_UseOneWayNearExpand;
                set
                {
                    roomPlaceNearest_First_UseOneWayNearExpand = value;
                }
            }
            [SerializeField, HideInInspector] private bool roomPlaceNearest_First_UseOneWayNearExpand;



            [TitleGroup("방"), BoxGroup("방/박스", false)]
            [BoxGroup("방/박스/방 배치")]
            [HorizontalGroup("방/박스/방 배치/1차인접단방향가로")]
            [ShowInInspector]
            [EnableIf(nameof(RoomPlaceNearest_First_UseOneWayNearExpand))]
            [LabelText("최대 반복 횟수")]
            [PropertyOrder(1)]
            public int RoomPlaceNearest_First_UseOneWayNearExpand_MaxCount
            {
                get => roomPlaceNearest_First_UseOneWayNearExpand_MaxCount;
                set
                {
                    roomPlaceNearest_First_UseOneWayNearExpand_MaxCount = Mathf.Max(1, value);
                }
            }
            [SerializeField, HideInInspector] private int roomPlaceNearest_First_UseOneWayNearExpand_MaxCount = 50;



            [TitleGroup("방"), BoxGroup("방/박스", false)]
            [BoxGroup("방/박스/방 배치")]
            [HorizontalGroup("방/박스/방 배치/2차인접단방향가로")]
            [ShowInInspector]
            [EnableIf(nameof(RoomPlaceNearest_First_UseOneWayNearExpand))]
            [LabelText("2차 인접 사용")]
            [LabelWidth(225)]
            [Indent(1)]
            [PropertyOrder(1)]
            public bool RoomPlaceNearest_UseSecond
            {
                get => UseRoomPlaceNearest && roomPlaceNearest_UseSecond;
                set
                {
                    roomPlaceNearest_UseSecond = value;
                }
            }
            [SerializeField, HideInInspector] private bool roomPlaceNearest_UseSecond;



            /// <summary>
            /// <b>방 배치 인접성</b> 연산 횟수
            /// </summary>
            [TitleGroup("방"), BoxGroup("방/박스", false)]
            [BoxGroup("방/박스/방 배치")]
            [HorizontalGroup("방/박스/방 배치/2차인접단방향가로")]
            [ShowInInspector]
            [EnableIf(nameof(RoomPlaceNearest_UseSecond))]
            [LabelText("최대 반복 횟수")]
            [PropertyOrder(1)]
            public int RoomPlaceNearest_SecondMaxCount
            {
                get => roomPlaceNearest_SecondMaxCount;
                set
                {
                    roomPlaceNearest_SecondMaxCount = Mathf.Max(1, value);
                }
            }
            [SerializeField, HideInInspector] private int roomPlaceNearest_SecondMaxCount = 50;



            /// <summary>
            /// 방 인접 이후 Post 배치 오프셋 사용 여부
            /// </summary>
            [TitleGroup("방"), BoxGroup("방/박스", false)]
            [BoxGroup("방/박스/방 배치")]
            [HorizontalGroup("방/박스/방 배치/방인접오프셋가로")]
            [ShowInInspector]
            [EnableIf(nameof(UseRoomPlaceNearest))]
            [LabelText("방 인접 Post 배치 사용")]
            [InfoBox("한쪽면에 붙이게 배치할경우,\n여전히 Stage 전체 Rect 경계선를 넘어가지 못하기 때문에,\n양끝에 도어가 배치되었다면 그 도어가 경계선을 넘어버려 생성에 실패 할 수도 있음", InfoMessageType.Warning)]
            [LabelWidth(225)]
            [Indent(1)]
            [PropertyOrder(1)]
            public bool UseRoomPlaceNearest_PostPlace
            {
                get => UseRoomPlaceNearest && useRoomPlaceNearest_PostPlace;
                set
                {
                    useRoomPlaceNearest_PostPlace = value;
                }
            }
            [SerializeField, HideInInspector] private bool useRoomPlaceNearest_PostPlace;



            /// <summary>
            /// 방 인접 이후 Post 배치 오프셋
            /// </summary>
            [TitleGroup("방"), BoxGroup("방/박스", false)]
            [BoxGroup("방/박스/방 배치")]
            [HorizontalGroup("방/박스/방 배치/방인접오프셋가로", 0.35f)]
            [ShowInInspector]
            [EnableIf(nameof(UseRoomPlaceNearest_PostPlace))]
            [HideLabel]
            [LabelWidth(40)]
            [PropertyOrder(1)]
            public ECenterStandard RoomPlaceNearest_PostPlace
            {
                get => roomPlaceNearest_PostPlace;
                set
                {
                    roomPlaceNearest_PostPlace = value;
                }
            }
            [SerializeField, HideInInspector] private ECenterStandard roomPlaceNearest_PostPlace = ECenterStandard.MiddleCenter;



            /// <summary>
            /// 방 인접 이후 방 배치시 Job System 사용
            /// </summary>
            [TitleGroup("방"), BoxGroup("방/박스", false)]
            [BoxGroup("방/박스/방 배치")]
            [ShowInInspector]
            [EnableIf(nameof(UseRoomPlaceNearest))]
            [LabelText("인접후 배치시 Job 사용")]
            [LabelWidth(225)]
            [Indent(1)]
            [PropertyOrder(1)]
            public bool RoomPlaceNearest_UseJobSystem
            {
                get => roomPlaceNearest_UseJobSystem;
                set
                {
                    roomPlaceNearest_UseJobSystem = value;
                }
            }
            [SerializeField, HideInInspector] private bool roomPlaceNearest_UseJobSystem;



            /// <summary>
            /// 복도 생성에 실패하여, 방 인접을 크로노브레이크 할때, 최대 탐색 횟수
            /// </summary>
            [TitleGroup("방"), BoxGroup("방/박스", false)]
            [BoxGroup("방/박스/방 배치")]
            [ShowInInspector]
            [Sirenix.OdinInspector.ReadOnly]
            //[EnableIf(nameof(UseRoomPlaceNearest))]
            [LabelText("(미사용) 복도 크로노브레이크 최대 횟수")]
            [LabelWidth(250)]
            [Indent(1)]
            [PropertyOrder(1)]
            [PropertyTooltip("복도 생성에 실패하여, 방 인접을 크로노브레이크 할때, 최대 탐색 횟수")]
            public int RoomPlaceNearest_MaxChoronoBreak_ByHallways
            {
                get => roomPlaceNearest_MaxChoronoBreak_ByHallways;
                set
                {
                    roomPlaceNearest_MaxChoronoBreak_ByHallways.SetClamp0(value);
                }
            }
            [SerializeField, HideInInspector] private int roomPlaceNearest_MaxChoronoBreak_ByHallways = 50;



            #endregion



            #region 방 배치 조건



            /// <summary>
            /// 공간 안에서 생성되는 방의 최소 너비
            /// </summary>
            [TitleGroup("방"), BoxGroup("방/박스", false)]
            [BoxGroup("방/박스/방 배치 조건")]
            [ShowInInspector]
            [LabelText("방 배치가 가능한 최소 ↔너비")]
            [LabelWidth(255)]
            [PropertyOrder(4)]
            public int RoomPlacementMinWidth
            {
                get => roomPlacementMinWidth;
                set
                {
                    //. 0 ~ 공간의 최대 너비로 제약한다
                    roomPlacementMinWidth.SetClamp(value, 0, Main.space.SpaceWidthMax);
                }
            }
            [SerializeField, HideInInspector] private int roomPlacementMinWidth = 10;



            /// <summary>
            /// 공간 안에서 생성되는 방의 최소 높이
            /// </summary>
            [TitleGroup("방"), BoxGroup("방/박스", false)]
            [BoxGroup("방/박스/방 배치 조건")]
            [ShowInInspector]
            [LabelText("방 배치가 가능한 최소 ↕높이")]
            [LabelWidth(255)]
            [PropertyOrder(4)]
            public int RoomPlacementMinHeight
            {
                get => roomPlacementMinHeight;
                set
                {
                    //. 0 ~ 공간의 최대 높이로 제약한다
                    roomPlacementMinHeight.SetClamp(value, 0, Main.space.SpaceHeightMax);
                }
            }
            [SerializeField, HideInInspector] private int roomPlacementMinHeight = 10;



            ///<summary>
            ///방의 추가 보정 너비 (공간 안에 방을 생성할때, 방의 너비에 이 값을 더해 계산)<br/>
            ///사용할땐, 좌/우에 사용해야되기에 x2 를 해야한다
            ///</summary>
            [TitleGroup("방"), BoxGroup("방/박스", false)]
            [BoxGroup("방/박스/방 배치 조건")]
            [ShowInInspector]
            [LabelText("방 배치 추가 보정 ↔너비")]
            [LabelWidth(255)]
            [PropertyOrder(4)]
            [PropertyTooltip("방 배치를 위해 크기를 계산 할 때, 너비에 이 값을 더하여 보정한다")]
            public int RoomPlacementWidthCorrection
            {
                get => roomSizeCorrectionInSpaceWidth;
                private set
                {
                    //. 최소값을 0으로 제약한다
                    roomSizeCorrectionInSpaceWidth.SetClampMin(value, 0);
                }
            }
            [SerializeField, HideInInspector] private int roomSizeCorrectionInSpaceWidth = 0;



            ///<summary>
            ///방의 추가 보정 높이 (공간 안에 방을 생성할때, 방의 높이에 이 값을 더해 계산)<br/>
            ///사용할땐, 좌/우에 사용해야되기에 x2 를 해야한다
            ///</summary>
            [TitleGroup("방"), BoxGroup("방/박스", false)]
            [BoxGroup("방/박스/방 배치 조건")]
            [ShowInInspector]
            [LabelText("방 배치 추가 보정 ↕높이")]
            [LabelWidth(255)]
            [PropertyOrder(4)]
            [PropertyTooltip("방 배치를 위해 방 크기를 계산 할 때, 높이에 이 값을 더하여 보정한다")]
            public int RoomPlacementHeightCorrection
            {
                get => roomPlacementHeightCorrection;
                private set
                {
                    //. 최소값을 0으로 제약한다
                    roomPlacementHeightCorrection.SetClampMin(value, 0);
                }
            }
            [SerializeField, HideInInspector] private int roomPlacementHeightCorrection = 0;



            /// <summary>
            /// 방 배치 추가 보정 크기를 벡터로 구한다
            /// </summary>
            public Vector2Int RoomPlacementSizeCorrection => new Vector2Int(RoomPlacementWidthCorrection, RoomPlacementHeightCorrection);



            /// <summary>
            /// 공간에 방을 배치할 때 공간 크기와 비슷한 방을 우선적으로 배치할지 결정
            /// </summary>
            [TitleGroup("방"), BoxGroup("방/박스", false)]
            [BoxGroup("방/박스/방 배치 조건")]
            [ShowInInspector]
            [LabelText("방 배치 공간 매치 우선")]
            [LabelWidth(255)]
            [PropertyOrder(4)]
            [PropertyTooltip("공간 안에 방을 배치할때, 공간의 크기와 최대한 비슷한 방을 최우선적으로 배치 할 지 여부")]
            public bool UsePrioritizeMatchingRoomSize
            {
                get => usePrioritizeMatchingRoomSize;
                private set
                {
                    usePrioritizeMatchingRoomSize = value;
                }
            }
            [SerializeField, HideInInspector] private bool usePrioritizeMatchingRoomSize = true;



            #endregion



            ///======================================================================================================================================================



            /// <summary>
            /// 통합 방 Rect 을 받아와, 이 스테이지 설정에 맞춰 "통합 방 Rect 확장" 을 만든다
            /// </summary>
            public void SetTotalRoomRectToExpand(ref CustomRect2DRelative totalRoomRect, out int applyedCurrentHallwayWidth, bool useDoor_Down, bool useDoor_Up, bool useDoor_Left, bool useDoor_Right)
            {
                //. 총 현재 복도 두께 구하기
                applyedCurrentHallwayWidth = Main.hallway.GetTotalCurrentHallWayMinWidth;

                //. 각 방향별로, 사용중인 방향일경우 그 방향으로 총 현재 복도 두께만큼 Rect를 확장한다
                if (useDoor_Down) { totalRoomRect.yMin -= applyedCurrentHallwayWidth; }
                if (useDoor_Up) { totalRoomRect.yMax += applyedCurrentHallwayWidth; }
                if (useDoor_Left) { totalRoomRect.xMin -= applyedCurrentHallwayWidth; }
                if (useDoor_Right) { totalRoomRect.xMax += applyedCurrentHallwayWidth; }


                #region Legacy
                ////. 각 방향별 최대 도어 너비와, 복도의 최소 너비 중, 큰 값의 크기만큼 확장된다 (방향별로)
                ////! 241227 복도 안전구역도 추가


                //float hallwaySafeAreaLength = GetHallwaySafeAreaLength();
                ////float hallwaySafeAreaLength = HallwaySafeAreaLength;

                //if (useDoor_Down)
                //{
                //    roomObject.RoomDoorM.TryGetMaxDoorWidth(EDirection4.Down, out var maxDoorWidth_Down);
                //    totalRoomRect.yMin -= Mathf.Max(maxDoorWidth_Down, HallwayWidthMin);

                //    int downDoorCount = roomObject.RoomDoorM.GetDoorList(EDirection4.Down).Count;

                //    if (useHallwaySafeArea && downDoorCount > 0)
                //    {
                //        totalRoomRect.yMin -= hallwaySafeAreaLength * downDoorCount;
                //    }
                //}

                //if (useDoor_Up)
                //{
                //    roomObject.RoomDoorM.TryGetMaxDoorWidth(EDirection4.Up, out var maxDoorWidth_Up);
                //    totalRoomRect.yMax += Mathf.Max(maxDoorWidth_Up, HallwayWidthMin);

                //    int upDoorCount = roomObject.RoomDoorM.GetDoorList(EDirection4.Up).Count;

                //    if (useHallwaySafeArea && upDoorCount > 0)
                //    {
                //        totalRoomRect.yMax += hallwaySafeAreaLength * upDoorCount;
                //    }
                //}

                //if (useDoor_Left)
                //{
                //    roomObject.RoomDoorM.TryGetMaxDoorWidth(EDirection4.Left, out var maxDoorWidth_Left);
                //    totalRoomRect.xMin -= Mathf.Max(maxDoorWidth_Left, HallwayWidthMin);

                //    int leftDoorCount = roomObject.RoomDoorM.GetDoorList(EDirection4.Left).Count;

                //    if (useHallwaySafeArea && leftDoorCount > 0)
                //    {
                //        totalRoomRect.xMin -= hallwaySafeAreaLength * leftDoorCount;
                //    }
                //}

                //if (useDoor_Right)
                //{
                //    roomObject.RoomDoorM.TryGetMaxDoorWidth(EDirection4.Right, out var maxDoorWidth_Right);
                //    totalRoomRect.xMax += Mathf.Max(maxDoorWidth_Right, HallwayWidthMin);

                //    int rightDoorCount = roomObject.RoomDoorM.GetDoorList(EDirection4.Right).Count;

                //    if (useHallwaySafeArea && rightDoorCount > 0)
                //    {
                //        totalRoomRect.xMax += hallwaySafeAreaLength * rightDoorCount;
                //    }
                //} 
                #endregion
            }



            ///======================================================================================================================================================



            public override void Copy(Rooms original)
            {
                RoomPlaceRandomness = original.RoomPlaceRandomness;
                UseRoomPlaceNearest = original.UseRoomPlaceNearest;
                RoomPlaceNearest_First_UseOneWayNearExpand = original.RoomPlaceNearest_First_UseOneWayNearExpand;
                RoomPlaceNearest_First_UseOneWayNearExpand_MaxCount = original.RoomPlaceNearest_First_UseOneWayNearExpand_MaxCount;
                RoomPlaceNearest_UseSecond = original.RoomPlaceNearest_UseSecond;
                RoomPlaceNearest_SecondMaxCount = original.RoomPlaceNearest_SecondMaxCount;
                UseRoomPlaceNearest_PostPlace = original.UseRoomPlaceNearest_PostPlace;
                RoomPlaceNearest_PostPlace = original.RoomPlaceNearest_PostPlace;
                RoomPlaceNearest_UseJobSystem = original.RoomPlaceNearest_UseJobSystem;
                RoomPlaceNearest_MaxChoronoBreak_ByHallways = original.RoomPlaceNearest_MaxChoronoBreak_ByHallways;
                RoomPlacementMinWidth = original.RoomPlacementMinWidth;
                RoomPlacementMinHeight = original.RoomPlacementMinHeight;
                RoomPlacementWidthCorrection = original.RoomPlacementWidthCorrection;
                RoomPlacementHeightCorrection = original.RoomPlacementHeightCorrection;
                UsePrioritizeMatchingRoomSize = original.UsePrioritizeMatchingRoomSize;
            }



            public override void Refresh()
            {
                RoomPlaceRandomness = RoomPlaceRandomness;
                UseRoomPlaceNearest = UseRoomPlaceNearest;
                RoomPlaceNearest_First_UseOneWayNearExpand = RoomPlaceNearest_First_UseOneWayNearExpand;
                RoomPlaceNearest_First_UseOneWayNearExpand_MaxCount = RoomPlaceNearest_First_UseOneWayNearExpand_MaxCount;
                RoomPlaceNearest_UseSecond = RoomPlaceNearest_UseSecond;
                RoomPlaceNearest_SecondMaxCount = RoomPlaceNearest_SecondMaxCount;
                UseRoomPlaceNearest_PostPlace = UseRoomPlaceNearest_PostPlace;
                RoomPlaceNearest_PostPlace = RoomPlaceNearest_PostPlace;
                RoomPlaceNearest_UseJobSystem = RoomPlaceNearest_UseJobSystem;
                RoomPlaceNearest_MaxChoronoBreak_ByHallways = RoomPlaceNearest_MaxChoronoBreak_ByHallways;
                RoomPlacementMinWidth = RoomPlacementMinWidth;
                RoomPlacementMinHeight = RoomPlacementMinHeight;
                RoomPlacementWidthCorrection = RoomPlacementWidthCorrection;
                RoomPlacementHeightCorrection = RoomPlacementHeightCorrection;
                UsePrioritizeMatchingRoomSize = UsePrioritizeMatchingRoomSize;
            }



            ///======================================================================================================================================================
        }



        public Rooms Room => room;
        [InlineProperty, HideLabel, HideReferenceObjectPicker, SerializeField, ShowIf(nameof(IsValid))]
        private Rooms room = new();



        ///======================================================================================================================================================



        //? 복도



        [Serializable]
        public class Hallways : BaseMain<Hallways>
        {
            ///======================================================================================================================================================



            #region 복도 크기



            ///<summary>
            ///복도의 최소 너비
            ///</summary>
            [TitleGroup("복도"), BoxGroup("복도/박스", false)]
            [BoxGroup("복도/박스/복도 크기")]
            [ShowInInspector]
            [LabelText("복도 최소 너비")]
            [PropertyOrder(1)]
            public int HallwayMinWidth
            {
                get => hallwayMinWidth;
                private set
                {
                    //. 최소값을 1로 제약한다
                    hallwayMinWidth.SetClampMin(value, 1);
                }
            }
            [SerializeField, HideInInspector] private int hallwayMinWidth = 1;



            /// <summary>
            /// 복도를 확장할때, 우/상 을 우선적으로 확장할지 여부
            /// </summary>
            [TitleGroup("복도"), BoxGroup("복도/박스", false)]
            [BoxGroup("복도/박스/복도 크기")]
            [ShowInInspector]
            [LabelText("복도 확장 Positive")]
            [PropertyOrder(1)]
            [PropertyTooltip("복도가 확장 될 때, 우측/상단을 우선적으로 확장 할지 여부")]
            public bool HallwayExpandPositive
            {
                get => hallwayExpandPositive;
                private set
                {
                    hallwayExpandPositive = value;
                }
            }
            [SerializeField, HideInInspector] private bool hallwayExpandPositive = true;



            /// <summary>
            /// 총 현재 복도 최소 너비 구하기
            /// <para>복도 최소 너비 + (복도 안전구역을 사용중이라면, 복도 안전구역 너비x1)</para>
            /// </summary>
            /// <returns></returns>
            [TitleGroup("복도"), BoxGroup("복도/박스", false)]
            [BoxGroup("복도/박스/복도 크기")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            [LabelText("총 현재 복도 최소 너비")]
            [PropertyOrder(1)]
            public int GetTotalCurrentHallWayMinWidth => HallwayMinWidth + CurrentHallwaySafeAreaLength;



            /// <summary>
            /// 현재 복도 테두리 크기 얻기
            /// <para>(복도 테두리를 사용중이라면, <c>복도 테두리 너비x2</c>, 아니면 0)</para>
            /// </summary>
            public int CurrentHallwayEdgeLength
                => useCreateHallwayEdge ? hallwayEdgeLength * 2 : 0;



            /// <summary>
            /// 현재 도어 복도 테두리 크기 얻기
            /// <para>(도어 복도 테두리를 사용중이라면, <c>도어 복도 테두리 너비x2</c>, 아니면 0)</para>
            /// </summary>
            public int CurrentDoorHallwayEdgeLength
                => useCreateDoorHallwayEdge ? doorHallwayEdgeLength * 2 : 0;



            /// <summary>
            /// 현재 복도 안전구역 크기 얻기
            /// <para>(복도 안전구역을 사용중이라면, <c>복도 안전구역 너비x2</c>, 아니면 0)</para>
            /// </summary>
            public int CurrentHallwaySafeAreaLength
                => UseCreateHallwaySafeArea ? hallwaySafeAreaLength * 2 : 0;



            /// <summary>
            /// 복도의 너비를 받아와, 설정에 기반하여 <b>기본 복도 너비</b>, <b>최대 복도 너비</b> 를 각각 반환한다
            /// </summary>
            public void GetHallwayWidths_FromWidth(int hallwayWidth, out int mainHallwayWidth, out int maxHallwayWidth)
            {
                mainHallwayWidth = Mathf.Max(1, hallwayWidth - CurrentHallwayEdgeLength);
                maxHallwayWidth = hallwayWidth;
            }



            /// <summary>
            /// 현재 설정에 맞춰, 도어 복도의 너비를 <b>기본 복도 너비</b>, <b>최대 복도 너비</b> 를 각각 반환한다
            /// </summary>
            public void GetDoorHallwayWidths_FromWidth(int hallwayWidth, out int mainHallwayWidth, out int maxHallwayWidth)
            {
                mainHallwayWidth = Mathf.Max(1, hallwayWidth - CurrentDoorHallwayEdgeLength);
                maxHallwayWidth = hallwayWidth;
            }



            #region Legacy 복도 어쩌구 얻기

            ///// <summary>
            ///// 복도 테두리의 크기 얻기 (x2) <b>(비활성화되어있다면 0을 반환!)</b>
            ///// </summary>
            //public int GetHallwayEdgeLength()
            //{
            //    return UseCreateHallwayEdge ? HallwayEdgeLength * 2 : 0;
            //}



            ///// <summary>
            ///// 도어 복도 테두리의 크기 얻기 (x2) <b>(비활성화되어있다면 0을 반환!)</b>
            ///// </summary>
            //public int GetDoorHallwayEdgeLength()
            //{
            //    return UseCreateDoorHallwayEdge ? DoorHallwayEdgeLength * 2 : 0;
            //}



            ///// <summary>
            ///// 설정값에 따른 복도의 최대 확장 너비 구하기 (복도의 너비에서 더 확장!)<br/>
            ///// <i>(패스파인딩에서 포함하여 계산할때 사용)</i><br/>
            ///// <i>(현재 이 복도의 총 최대 너비를 구하려면, 이 값과 기존 복도의 너비를 같이 더해야함)</i>
            ///// </summary>
            //public int GetHallwayMaxExpandWidth()
            //{
            //    return GetHallwayEdgeLength();
            //    //! 241030 이거 의미가있나? 테두리는 내부로바뀌었고, 안전구역은 실제 복도가 아니라 그냥 점유구역일뿐인데
            //    //! 241120 여전히 필요해보임, 내부에 생성되기때문에 테두리를 빼고 복도를 생성한 뒤에, 나머지 구역에 테두리를 생성하는 구조이기 때문
            //}



            ///// <summary>
            ///// 설정값에 따른 복도의 최대 확장 너비 구하기 (복도의 너비에서 더 확장!)<br/>
            ///// <i>(패스파인딩에서 포함하여 계산할때 사용)</i><br/>
            ///// <i>(현재 이 복도의 총 최대 너비를 구하려면, 이 값과 기존 복도의 너비를 같이 더해야함)</i>
            ///// </summary>
            //public int GetDoorHallwayMaxExpandWidth()
            //{
            //    //return GetHallwaySafeAreaLength();
            //    return GetDoorHallwayEdgeLength();
            //}

            #endregion



            #endregion



            ///======================================================================================================================================================



            #region 복도 경로 탐색



            /// <summary>
            /// 방 인접을 사용중일때, 복도가 충돌했다면 크로노 브레이크를 사용할지의 여부 (무조건 활성화 권장)
            /// </summary>
            [TitleGroup("복도"), BoxGroup("복도/박스", false)]
            [BoxGroup("복도/박스/경로 탐색")]
            [ShowInInspector]
            [LabelText("(미사용) 크로노 브레이크 사용 (복도 충돌 & 방 인접시)")]
            [Sirenix.OdinInspector.ReadOnly]
            [LabelWidth(250)]
            [PropertyOrder(1)]
            //[PropertyTooltip("방 인접을 사용중일 때, 복도가 충돌했다면 크로노 브레이크를 사용할지 여부 (무조건 활성화 권장)")]
            //[InfoBox("무조건 활성화 권장", InfoMessageType.Error, VisibleIf = nameof(editor_NotUseChoronoBreak_ByHallways_RoomPlaceNearest))]
#if UNITY_EDITOR
            [GUIColor(nameof(editorColor_NotUseChoronoBreak_ByHallways_RoomPlaceNearest))]
#endif
            public bool UseChoronoBreak_ByHallways_RoomPlaceNearest
            {
                get => useChoronoBreak_ByHallways_RoomPlaceNearest;
                set
                {
                    useChoronoBreak_ByHallways_RoomPlaceNearest = value;
                }
            }
            [SerializeField, HideInInspector] private bool useChoronoBreak_ByHallways_RoomPlaceNearest = true;



#if UNITY_EDITOR

            private bool editor_NotUseChoronoBreak_ByHallways_RoomPlaceNearest => !useChoronoBreak_ByHallways_RoomPlaceNearest;

            private Color editorColor_NotUseChoronoBreak_ByHallways_RoomPlaceNearest
            {
                get { return (useChoronoBreak_ByHallways_RoomPlaceNearest) ? Color.white : Color.red; }
            }

#endif



            /// <summary>
            /// 경로 재탐색 최대 횟수
            /// </summary>
            [TitleGroup("복도"), BoxGroup("복도/박스", false)]
            [BoxGroup("복도/박스/경로 탐색")]
            [ShowInInspector]
            [LabelText("최대 경로 재탐색 횟수")]
            [LabelWidth(225)]
            [PropertyOrder(1)]
            public int HallwayPathFinding_MaxReCalculateLoopCount
            {
                get => hallwayPathFinding_MaxReCalculateLoopCount;
                set
                {
                    //. 최소값을 1로 제약한다
                    hallwayPathFinding_MaxReCalculateLoopCount.SetClampMin(value, 1);
                }
            }
            [SerializeField, HideInInspector] private int hallwayPathFinding_MaxReCalculateLoopCount = 50;



            /// <summary>
            /// 복도 휴리스틱 타입
            /// </summary>
            [TitleGroup("복도"), BoxGroup("복도/박스", false)]
            [BoxGroup("복도/박스/경로 탐색")]
            [ShowInInspector]
            [LabelText("복도 휴리스틱 타입")]
            [PropertyOrder(1)]
            public StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType HallwayHeuristicType
            {
                get => hallwayHeuristicType;
                set
                {
                    bool defaultType = hallwayHeuristicType != value;

                    hallwayHeuristicType = value;

                    //. 이전과 다른 새로운 타입의 값이 적용되었다면, Factor를 해당 타입의 맞는 기본값으로 설정
                    if (defaultType) { SetHallwayHeuristicFactor_Default(); }
                }
            }
            [SerializeField, HideInInspector] private StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType hallwayHeuristicType = StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.Manhattan;



            /// <summary>
            /// 복도 휴리스틱 타입 Factor
            /// </summary>
            [TitleGroup("복도"), BoxGroup("복도/박스", false)]
            [BoxGroup("복도/박스/경로 탐색")]
            [ShowInInspector]
            [LabelText("복도 휴리스틱 Factor")]
            [PropertyOrder(1)]
            [Indent(1)]
#if UNITY_EDITOR
            [ShowIf(nameof(editorUseHallwayHeuristicFactor_Default))]
#endif
            public float HallwayHeuristicFactor
            {
                get => hallwayHeuristicFactor;
                set
                {
                    hallwayHeuristicFactor = value;

                    switch (hallwayHeuristicType)
                    {
                        //. 이 타입들은 factor를 사용하지 않으므로 -1로 설정
                        case StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.Manhattan:
                        case StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.Euclidean:
                        hallwayHeuristicFactor = -1;
                        break;

                        //. 0.0f ~ 1.0f 범위로 제약
                        case StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.WeightedDiagonal:
                        case StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.LerpHybrid:
                        case StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.AdaptiveWeightedHybrid:
                        case StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.SplineInterpolation:
                        hallwayHeuristicFactor = Mathf.Clamp(hallwayHeuristicFactor, 0.0f, 1.0f);
                        break;

                        //. 최소값 0.1f로 제약
                        case StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.WeightedEuclidean:
                        hallwayHeuristicFactor.SetClampMin(0.1f);
                        break;

                        //. 최소값 1.0f로 제약
                        case StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.Relaxation:
                        hallwayHeuristicFactor.SetClampMin(1.0f);
                        break;
                    }
                }
            }
            [SerializeField, HideInInspector] private float hallwayHeuristicFactor;



            /// <summary>
            /// 복도 휴리스틱 타입 Factor를 기본값으로 변경
            /// </summary>
            private void SetHallwayHeuristicFactor_Default()
            {
                switch (HallwayHeuristicType)
                {
                    case StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.WeightedDiagonal:
                    HallwayHeuristicFactor = 0.9f;
                    break;
                    case StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.LerpHybrid:
                    HallwayHeuristicFactor = 0.5f;
                    break;
                    case StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.WeightedEuclidean:
                    HallwayHeuristicFactor = 1.2f;
                    break;
                    case StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.AdaptiveWeightedHybrid:
                    HallwayHeuristicFactor = 1.0f;
                    break;
                    case StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.SplineInterpolation:
                    HallwayHeuristicFactor = 0.5f;
                    break;
                    case StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.Relaxation:
                    HallwayHeuristicFactor = 2.0f;
                    break;
                }
            }



            //. 휴리스틱 정보 띄우기
#if UNITY_EDITOR

            [TitleGroup("복도"), BoxGroup("복도/박스", false)]
            [BoxGroup("복도/박스/경로 탐색")]
            [BoxGroup("복도/박스/경로 탐색/휴리스틱정보박스", false)]
            [PropertyOrder(1)]
            [Indent(1)]
            [ShowInInspector, HideLabel, DisplayAsString(EnableRichText = true, Overflow = false), EnableGUI]
            private string dummy_HallwayHeuristicInfo
            {
                get
                {
                    return editorGetHeuristicDescription(hallwayHeuristicType, true);
                }
            }

            private bool editorUseHallwayHeuristicFactor_Default
            {
                get
                {
                    if (hallwayHeuristicType == StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.Manhattan ||
                        hallwayHeuristicType == StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.Euclidean)
                    {
                        return false;
                    }
                    return true;
                }
            }

            private static string editorGetHeuristicDescription(StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType heuristicType, bool useColor)
            {
                string method = string.Empty;
                string characteristics = string.Empty;
                string factorRange = "없음";
                string defaultValue = "없음";

                switch (heuristicType)
                {
                    case StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.Manhattan:
                    method = "맨해튼 거리 기반";
                    characteristics = "직선 위주의 뻣뻣한 경로 생성, 대각선 이동은 고려하지 않음";
                    break;

                    case StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.Euclidean:
                    method = "유클리드 거리 기반";
                    characteristics = "두 점 간의 직선 거리를 기반으로 경로 생성, 대각선 포함";
                    break;

                    case StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.WeightedDiagonal:
                    method = "대각선 선호 거리 기반";
                    characteristics = "대각선 이동을 더 선호하도록 가중치를 부여한 경로 생성";
                    factorRange = "0.0 ~ 1.0";
                    defaultValue = "0.9";
                    break;

                    case StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.LerpHybrid:
                    method = "유클리드와 맨해튼 거리 혼합";
                    characteristics = "유클리드 거리와 맨해튼 거리 간의 보간을 통해 부드러운 경로 생성";
                    factorRange = "0.0 ~ 1.0";
                    defaultValue = "0.5";
                    break;

                    case StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.WeightedEuclidean:
                    method = "유클리드 거리 기반, 가중치 추가";
                    characteristics = "유클리드 거리에 가중치를 부여하여 경로의 비용을 조정";
                    factorRange = "0.1 ~ ∞";
                    defaultValue = "1.2";
                    break;

                    case StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.AdaptiveWeightedHybrid:
                    method = "동적 가중치 기반 하이브리드";
                    characteristics = "맨해튼 거리와 유클리드 거리의 비율을 동적으로 조정하여 경로 생성";
                    factorRange = "0.0 ~ 1.0";
                    defaultValue = "1.0";
                    break;

                    case StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.SplineInterpolation:
                    method = "Spline 보간 기반";
                    characteristics = "두 점 사이를 보간하여 더 자연스럽고 부드러운 경로 생성";
                    factorRange = "0.0 ~ 1.0";
                    defaultValue = "0.5";
                    break;

                    case StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.Relaxation:
                    method = "Relaxation 기법";
                    characteristics = "꺾임을 부드럽게 조정하여 매끄러운 경로 생성";
                    factorRange = "1.0 ~ ∞";
                    defaultValue = "2.0";
                    break;

                    default:
                    throw new ArgumentOutOfRangeException(nameof(heuristicType), "Unknown heuristic type");
                }

                return editorFormatDescription(heuristicType, method, characteristics, factorRange, defaultValue, useColor);

            }

            private static string editorFormatDescription(StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType heuristicType, string method, string characteristics, string factorRange, string defaultValue, bool useColor)
            {
                if (useColor)
                {
                    return $"<color=#ac92ec>경로 생성 방식:\t</color> <color=#f7da64>{method} ({heuristicType})</color>\n" +
                           $"\t<color=#ac92ec>특성:\t</color> <color=#f7da64>{characteristics}</color>\n" +
                           $"\t<color=#ac92ec>범위:\t</color> <color=#2ecc71>{factorRange}</color>\n" +
                           $"\t<color=#ac92ec>기본값:\t</color> <color=#2ecc71>{defaultValue}</color>";
                }
                else
                {
                    return $"경로 생성 방식:\t {method} ({heuristicType})\n" +
                           $"\t특성:\t {characteristics}\n" +
                           $"\t범위:\t {factorRange}\n" +
                           $"\t기본값:\t {defaultValue}";
                }
            }

#endif


            #endregion



            ///======================================================================================================================================================



            #region 복도 추가 생성



            /// <summary>
            /// 복도 테두리 생성하기
            /// </summary>
            [TitleGroup("복도"), BoxGroup("복도/박스", false)]
            [BoxGroup("복도/박스/복도 추가 생성")]
            [HorizontalGroup("복도/박스/복도 추가 생성/복도테두리가로", 0.4f)]
            [ShowInInspector]
            [LabelText("복도 테두리 생성")]
            [LabelWidth(140)]
            [PropertyOrder(1)]
            [PropertyTooltip("복도 주위에 복도 테두리가 생기는 것이 아니라, 복도 내부에 생성되기에 복도 너비보다 크다면, 생성되지 않는다")]
            public bool UseCreateHallwayEdge
            {
                get => useCreateHallwayEdge;
                set
                {
                    useCreateHallwayEdge = value;
                }
            }
            [SerializeField, HideInInspector] private bool useCreateHallwayEdge = true;



            /// <summary>
            /// 복도 테두리 크기
            /// </summary>
            [TitleGroup("복도"), BoxGroup("복도/박스", false)]
            [BoxGroup("복도/박스/복도 추가 생성")]
            [HorizontalGroup("복도/박스/복도 추가 생성/복도테두리가로")]
            [EnableIf(nameof(UseCreateHallwayEdge))]
            [ShowInInspector]
            [LabelText("너비")]
            [LabelWidth(50)]
            [PropertyOrder(1)]
            public int HallwayEdgeLength
            {
                get => hallwayEdgeLength;
                set
                {
                    //. 최소값을 0으로 제약한다
                    hallwayEdgeLength.SetClampMin(value, 0);
                }
            }
            [SerializeField, HideInInspector] private int hallwayEdgeLength = 1;



            /// <summary>
            /// 도어 복도 테두리 생성하기
            /// </summary>
            [TitleGroup("복도"), BoxGroup("복도/박스", false)]
            [BoxGroup("복도/박스/복도 추가 생성")]
            [HorizontalGroup("복도/박스/복도 추가 생성/도어테두리가로", 0.4f)]
            [ShowInInspector]
            [LabelText("도어 복도 테두리 생성")]
            [LabelWidth(width: 140)]
            [PropertyOrder(1)]
            [PropertyTooltip("도어 복도 주위에 도어 복도 테두리가 생기는 것이 아니라, 도어 복도 내부에 생성되기에 도어 복도 너비보다 크다면, 생성되지 않는다")]
            public bool UseCreateDoorHallwayEdge
            {
                get => useCreateDoorHallwayEdge;
                set
                {
                    useCreateDoorHallwayEdge = value;
                }
            }
            [SerializeField, HideInInspector] private bool useCreateDoorHallwayEdge = true;



            /// <summary>
            /// 도어 복도 테두리 크기
            /// </summary>
            [TitleGroup("복도"), BoxGroup("복도/박스", false)]
            [BoxGroup("복도/박스/복도 추가 생성")]
            [HorizontalGroup("복도/박스/복도 추가 생성/도어테두리가로")]
            [EnableIf(nameof(UseCreateDoorHallwayEdge))]
            [ShowInInspector]
            [LabelText("너비")]
            [LabelWidth(50)]
            [PropertyOrder(1)]
            public int DoorHallwayEdgeLength
            {
                get => doorHallwayEdgeLength;
                set
                {
                    //. 최소값을 0으로 제약한다
                    doorHallwayEdgeLength.SetClampMin(value, 0);
                }
            }
            [SerializeField, HideInInspector] private int doorHallwayEdgeLength = 1;



            /// <summary>
            /// 복도 안전구역 생성하기
            /// </summary>
            [TitleGroup("복도"), BoxGroup("복도/박스", false)]
            [BoxGroup("복도/박스/복도 추가 생성")]
            [HorizontalGroup("복도/박스/복도 추가 생성/복도안전구역테두리가로", 0.4f)]
            [ShowInInspector]
            [LabelText("복도 안전구역 생성")]
            [LabelWidth(140)]
            [PropertyOrder(1)]
            [PropertyTooltip("복도 주위에 생성되는 안전구역, 복도 생성을 위해 패스파인딩 할 때, 해당 복도와 겹치지 않게 회피한다")]
            public bool UseCreateHallwaySafeArea
            {
                get => useCreateHallwaySafeArea;
                set
                {
                    useCreateHallwaySafeArea = value;
                    //Refresh_RoomSizeCorrectionInSpaceWidth();
                    //Refresh_RoomSizeCorrectionInSpaceHeight();
                }
            }
            [SerializeField, HideInInspector] private bool useCreateHallwaySafeArea = true;



            /// <summary>
            /// 복도 안전구역 크기
            /// </summary>
            [TitleGroup("복도"), BoxGroup("복도/박스", false)]
            [BoxGroup("복도/박스/복도 추가 생성")]
            [HorizontalGroup("복도/박스/복도 추가 생성/복도안전구역테두리가로")]
            [EnableIf(nameof(UseCreateHallwaySafeArea))]
            [ShowInInspector]
            [LabelText("너비")]
            [LabelWidth(50)]
            [PropertyOrder(1)]
            public int HallwaySafeAreaLength
            {
                get => hallwaySafeAreaLength;
                set
                {
                    //. 최소값을 0으로 제약한다
                    hallwaySafeAreaLength.SetClampMin(value, 0);
                }
            }
            [SerializeField, HideInInspector] private int hallwaySafeAreaLength = 1;



            #endregion



            ///======================================================================================================================================================



            public override void Copy(Hallways original)
            {
                HallwayMinWidth = original.HallwayMinWidth;
                HallwayExpandPositive = original.HallwayExpandPositive;
                HallwayPathFinding_MaxReCalculateLoopCount = original.HallwayPathFinding_MaxReCalculateLoopCount;
                UseCreateHallwayEdge = original.UseCreateHallwayEdge;
                HallwayEdgeLength = original.HallwayEdgeLength;
                UseCreateDoorHallwayEdge = original.UseCreateDoorHallwayEdge;
                DoorHallwayEdgeLength = original.DoorHallwayEdgeLength;
                UseCreateHallwaySafeArea = original.UseCreateHallwaySafeArea;
                HallwaySafeAreaLength = original.HallwaySafeAreaLength;
                HallwayHeuristicType = original.HallwayHeuristicType;
                HallwayHeuristicFactor = original.HallwayHeuristicFactor;
                UseChoronoBreak_ByHallways_RoomPlaceNearest = original.UseChoronoBreak_ByHallways_RoomPlaceNearest;
            }



            public override void Refresh()
            {
                HallwayMinWidth = HallwayMinWidth;
                HallwayExpandPositive = HallwayExpandPositive;
                HallwayPathFinding_MaxReCalculateLoopCount = HallwayPathFinding_MaxReCalculateLoopCount;
                UseCreateHallwayEdge = UseCreateHallwayEdge;
                HallwayEdgeLength = HallwayEdgeLength;
                UseCreateDoorHallwayEdge = UseCreateDoorHallwayEdge;
                DoorHallwayEdgeLength = DoorHallwayEdgeLength;
                UseCreateHallwaySafeArea = UseCreateHallwaySafeArea;
                HallwaySafeAreaLength = HallwaySafeAreaLength;
                HallwayHeuristicType = HallwayHeuristicType;
                HallwayHeuristicFactor = HallwayHeuristicFactor;
                UseChoronoBreak_ByHallways_RoomPlaceNearest = UseChoronoBreak_ByHallways_RoomPlaceNearest;
            }



            ///======================================================================================================================================================

        }



        public Hallways Hallawy => hallway;
        [InlineProperty, HideLabel, HideReferenceObjectPicker, SerializeField, ShowIf(nameof(IsValid))]
        private Hallways hallway = new();



        ///======================================================================================================================================================



        //? 커스텀 이벤트



        [Serializable]
        public class CustomEvents : BaseMain<CustomEvents>
        {
            ///======================================================================================================================================================



            //? 커스텀 이벤트 (커스텀 델리게이트 이벤트 SO)



#if UNITY_EDITOR



            [TitleGroup("커스텀 이벤트"), BoxGroup("커스텀 이벤트/박스", false)]
            [BoxGroup("커스텀 이벤트/박스/더미박스", false)]
            [PropertyOrder(14)]
            [ShowInInspector, HideLabel, DisplayAsString(EnableRichText = true, Overflow = false, Alignment = TextAlignment.Center), EnableGUI]
            private string dummy_CustomEventInfo
            {
                get
                {
                    return $"<color=red>배치된 커스텀 이벤트에 따라, 생성이 실패 할 수도 있으니 주의</color>";
                }
            }



#endif



            [TitleGroup("커스텀 이벤트"), BoxGroup("커스텀 이벤트/박스", false)]
            [ShowInInspector]
            [LabelText("그리드 생성")]
            [PropertyOrder(1)]
            [InlineEditor]
            public StageGenerateCustomEventBase_CreateGrid CreateGrid { get => createGrid; private set => createGrid = value; }
            [SerializeField, HideInInspector] private StageGenerateCustomEventBase_CreateGrid createGrid;


            [TitleGroup("커스텀 이벤트"), BoxGroup("커스텀 이벤트/박스", false)]
            [ShowInInspector]
            [LabelText("방 소환")]
            [PropertyOrder(1)]
            [InlineEditor]
            public StageGenerateCustomEventBase_SummonRoom SummonRoom { get => summonRoom; private set => summonRoom = value; }
            [SerializeField, HideInInspector] private StageGenerateCustomEventBase_SummonRoom summonRoom;



            [TitleGroup("커스텀 이벤트"), BoxGroup("커스텀 이벤트/박스", false)]
            [ShowInInspector]
            [LabelText("방 연결")]
            [PropertyOrder(1)]
            [InlineEditor]
            public StageGenerateCustomEventBase_ConnectRoom ConnectRoom { get => connectRoom; private set => connectRoom = value; }
            [SerializeField, HideInInspector] private StageGenerateCustomEventBase_ConnectRoom connectRoom;



            [TitleGroup("커스텀 이벤트"), BoxGroup("커스텀 이벤트/박스", false)]
            [ShowInInspector]
            [LabelText("복도 배치")]
            [PropertyOrder(1)]
            [InlineEditor]
            public StageGenerateCustomEventBase_PlaceHallway PlaceHallway { get => placeHallway; private set => placeHallway = value; }
            [SerializeField, HideInInspector] private StageGenerateCustomEventBase_PlaceHallway placeHallway;



            [TitleGroup("커스텀 이벤트"), BoxGroup("커스텀 이벤트/박스", false)]
            [ShowInInspector]
            [LabelText("복도 소환")]
            [PropertyOrder(1)]
            [InlineEditor]
            public StageGenerateCustomEventBase_SummonHallway SummonHallway { get => summonHallway; private set => summonHallway = value; }
            [SerializeField, HideInInspector] private StageGenerateCustomEventBase_SummonHallway summonHallway;



            [TitleGroup("커스텀 이벤트"), BoxGroup("커스텀 이벤트/박스", false)]
            [ShowInInspector]
            [LabelText("포스트 이벤트")]
            [PropertyOrder(1)]
            [InlineEditor]
            public StageGenerateCustomEventBase_PostEvent PostEvent { get => postEvent; private set => postEvent = value; }
            [SerializeField, HideInInspector] private StageGenerateCustomEventBase_PostEvent postEvent;



            ///======================================================================================================================================================



            public override void Copy(CustomEvents original)
            {
                CreateGrid = original.CreateGrid;
                SummonRoom = original.SummonRoom;
                ConnectRoom = original.ConnectRoom;
                PlaceHallway = original.PlaceHallway;
                SummonHallway = original.SummonHallway;
                PostEvent = original.PostEvent;
            }



            public override void Refresh()
            {
                CreateGrid = CreateGrid;
                SummonRoom = SummonRoom;
                ConnectRoom = ConnectRoom;
                PlaceHallway = PlaceHallway;
                SummonHallway = SummonHallway;
                PostEvent = PostEvent;
            }



            ///======================================================================================================================================================
        }



        public CustomEvents CustomEvent => customEvent;
        [InlineProperty, HideLabel, HideReferenceObjectPicker, SerializeField, ShowIf(nameof(IsValid))]
        private CustomEvents customEvent = new();



        ///======================================================================================================================================================
    }



    [CreateAssetMenu(menuName = CreateAssetMenuInfo.STAGEGEN_SETTING)]
    public class StageGeneratorSettingSbject : ScriptableObject, IStageGeneratorSetting
    {
#if UNITY_EDITOR

        [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Center, EnableRichText = true, Overflow = false), EnableGUI]
        [PropertyOrder(-100)]
        [PropertySpace(8, 8)]
        private string dummy_Title
        {
            get
            {
                return $"<b><size=15>스테이지 생성기 설정 SO</size></b>\n" +
                    $"유효성: " + (setting.IsValid ? $"✔️ <color=#2ecc71>Valid</color>" : $" ❌ <color=#ed5565>Invalid</color>");
            }
        }



        //. 에디터에서 매니저들 상시 WakeUp 시켜주는 OnValidate
        private void OnValidate()
        {
            setting.OnValidate(true);
        }



#endif



        [SerializeField, InlineProperty, HideLabel]
        private StageGeneratorSetting setting;
        public StageGeneratorSetting Setting => setting;
    }
}
