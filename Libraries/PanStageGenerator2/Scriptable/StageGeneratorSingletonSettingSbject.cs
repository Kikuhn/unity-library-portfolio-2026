using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Pan.Util;
using Pan.GridCompatibles2;
using Pan.StageGenerators;
using Sirenix.OdinInspector;



namespace Pan.StageGenerators
{
    [CreateAssetMenu(menuName = CreateAssetMenuInfo.STAGEGEN_SINGLETON_SETTING)]
    public class StageGeneratorSingletonSettingSbject : SingleTon_ScriptableObject<StageGeneratorSingletonSettingSbject>
    {
        ///======================================================================================================================================================



        [TitleGroup("기즈모")]
        [LabelText("기즈모 사용")]
        public bool UseGizmo = true;



        ///======================================================================================================================================================



        //? 스테이지 생성 기즈모



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/스테이지 생성 기즈모")]
        [EnableIf(nameof(UseGizmo))]
        [Indent(1)]
        [LabelText("스테이지 생성 기즈모 사용")]
        [SerializeField]
        [LabelWidth(200)]
        private bool useStageGeneratorGizmo = true;
        public bool UseStageGeneratorGizmo => UseGizmo && useStageGeneratorGizmo;



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/스테이지 생성 기즈모")]
        [EnableIf(nameof(UseStageGeneratorGizmo))]
        [Indent(2)]
        [LabelText("선택 되었을 때만 사용")]
        [SerializeField]
        [LabelWidth(200)]
        private bool useStageGeneratorGizmoOnlySelect = false;
        public bool UseStageGeneratorGizmoOnlySelect => UseStageGeneratorGizmo && useStageGeneratorGizmoOnlySelect;



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/스테이지 생성 기즈모")]
        [EnableIf(nameof(UseStageGeneratorGizmo))]
        [Indent(1)]
        [LabelText("스테이지 색")]
        [LabelWidth(200)]
        public ToggleColor StageColor = new ToggleColor(Color.white);



        ///======================================================================================================================================================



        //? 공간



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/공간")]
        [EnableIf(nameof(UseGizmo))]
        [Indent(1)]
        [LabelText("스페이스 기즈모 사용")]
        [SerializeField]
        [LabelWidth(200)]
        private bool useSpaceGizmo = true;
        public bool UseSpaceGizmo => UseGizmo && useSpaceGizmo;



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/공간")]
        [EnableIf(nameof(UseSpaceGizmo))]
        [Indent(2)]
        [LabelText("선택 되었을 때만 사용")]
        [SerializeField]
        [LabelWidth(200)]
        private bool useSpaceGizmoOnlySelect = false;
        public bool UseSpaceGizmoOnlySelect => UseSpaceGizmo && useSpaceGizmoOnlySelect;



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/공간")]
        [EnableIf(nameof(UseSpaceGizmo))]
        [Indent(1)]
        [LabelText("공간 색")]
        [LabelWidth(200)]
        public ToggleColor SpaceColor = new ToggleColor(Color.blue);



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/공간")]
        [EnableIf(nameof(UseSpaceGizmo))]
        [Indent(2)]
        [LabelText("공간 정보 표시")]
        [LabelWidth(200)]
        public bool UseSpaceInfoext;



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/공간")]
        [EnableIf(nameof(UseSpaceGizmo))]
        [Indent(2)]
        [LabelText("공간 정보 표시 (마우스)")]
        [LabelWidth(200)]
        public bool UseSpaceInfoTextMouseHovering;



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/공간")]
        [EnableIf(nameof(UseSpaceGizmo))]
        [Indent(2)]
        [LabelText("공간 내부 비율")]
        [LabelWidth(200)]
        public float SpaceInsideLength = -0.2f;



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/공간")]
        [EnableIf(nameof(UseSpaceGizmo))]
        [Indent(2)]
        [LabelText("공간 노드 색")]
        [LabelWidth(200)]
        public ToggleColor SpaceNodeColor = new ToggleColor(Color.cyan);



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/공간")]
        [EnableIf(nameof(UseSpaceGizmo))]
        [Indent(3)]
        [LabelText("노드를 거미줄처럼 표시")]
        [LabelWidth(200)]
        public bool UseSpaceNodeWebStyle;



        ///======================================================================================================================================================



        //? 그리드 기즈모



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/그리드 기즈모")]
        [EnableIf(nameof(UseGizmo))]
        [Indent(1)]
        [LabelText("그리드 기즈모 사용")]
        [SerializeField]
        [LabelWidth(200)]
        private bool useGridGizmo = true;
        public bool UseGridGizmo => UseGizmo && useGridGizmo;



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/그리드 기즈모")]
        [EnableIf(nameof(UseGridGizmo))]
        [Indent(2)]
        [LabelText("선택 되었을 때만 사용")]
        [SerializeField]
        [LabelWidth(200)]
        private bool useGridGizmoOnlySelect = false;
        public bool UseGridGizmoOnlySelect => UseGridGizmo && useGridGizmoOnlySelect;



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/그리드 기즈모")]
        [EnableIf(nameof(UseGridGizmo))]
        [Indent(2)]
        [LabelText("그리드 격자 색")]
        [LabelWidth(200)]
        public ToggleColor GridColor = new ToggleColor(new Color(1f, 1f, 0.8f, 0.2f));



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/그리드 기즈모")]
        [EnableIf(nameof(UseGridGizmo))]
        [Indent(3)]
        [LabelText("그리드 정보 표시 (마우스)")]
        [LabelWidth(200)]
        public bool UseGridInfoTextMouseHovering;



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/그리드 기즈모")]
        [EnableIf(nameof(UseGridGizmo))]
        [Indent(2)]
        [LabelText("점유 색")]
        [LabelWidth(200)]
        public ToggleColor GridColor_Occupied = new ToggleColor(Color.red);



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/그리드 기즈모")]
        [EnableIf(nameof(UseGridGizmo))]
        [Indent(2)]
        [LabelText("경로 비용 색")]
        [LabelWidth(200)]
        public ToggleColor GridColor_PathCost = new ToggleColor(Color.white);



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/그리드 기즈모")]
        [EnableIf(nameof(UseGridGizmo))]
        [Indent(2)]
        [LabelText("패스파인딩 금지 메인 경로 태그 색")]
        [LabelWidth(250)]
        public ToggleColor GridColor_WasBannedMainPath = new ToggleColor(Color.red);



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/그리드 기즈모")]
        [EnableIf(nameof(UseGridGizmo))]
        [Indent(2)]
        [LabelText("복도 충돌 금지 그리드 태그 색")]
        [LabelWidth(250)]
        public ToggleColor GridColor_WasBannedCollisionOtherHallways = new ToggleColor(Color.cyan);



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/그리드 기즈모")]
        [EnableIf(nameof(UseGridGizmo))]
        [Indent(2)]
        [LabelText("방 태그 색")]
        [LabelWidth(200)]
        public ToggleColor GridColor_RoomTag = new ToggleColor(Color.red);



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/그리드 기즈모")]
        [EnableIf(nameof(UseGridGizmo))]
        [Indent(2)]
        [LabelText("방 안전구역 태그 색")]
        [LabelWidth(200)]
        public ToggleColor GridColor_RoomSafeTag = new ToggleColor(Color.yellow.WithMultipliedAlpha(0.5f));



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/그리드 기즈모")]
        [EnableIf(nameof(UseGridGizmo))]
        [Indent(2)]
        [LabelText("방 안전구역 확장 태그 색")]
        [LabelWidth(200)]
        public ToggleColor GridColor_RoomExpandSafeTag = new ToggleColor(new Color(1f, 0.1961f, 0f, 0.5f));



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/그리드 기즈모")]
        [EnableIf(nameof(UseGridGizmo))]
        [Indent(2)]
        [LabelText("방 도어 태그 색")]
        [LabelWidth(200)]
        public ToggleColor GridColor_RoomDoorTag = new ToggleColor(Color.black.WithMultipliedAlpha(0.5f));



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/그리드 기즈모")]
        [EnableIf(nameof(UseGridGizmo))]
        [Indent(2)]
        [LabelText("복도 태그 색")]
        [LabelWidth(200)]
        public ToggleColor GridColor_HallwayTag = new ToggleColor(Color.cyan);



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/그리드 기즈모")]
        [EnableIf(nameof(UseGridGizmo))]
        [Indent(2)]
        [LabelText("복도 테두리 태그 색")]
        [LabelWidth(200)]
        public ToggleColor GridColor_HallwayEdgeTag = new ToggleColor(Color.red);



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/그리드 기즈모")]
        [EnableIf(nameof(UseGridGizmo))]
        [Indent(2)]
        [LabelText("복도 안전구역 태그 색")]
        [LabelWidth(200)]
        public ToggleColor GridColor_HallwaySafeTag = new ToggleColor(Color.green);



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/그리드 기즈모")]
        [EnableIf(nameof(UseGridGizmo))]
        [Indent(2)]
        [LabelText("복도 NavMesh 색")]
        [LabelWidth(200)]
        public ToggleColor GridColor_HallwayNavMesh = new ToggleColor(Color.magenta.WithMultipliedAlpha(0.5f));



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/그리드 기즈모")]
        [EnableIf(nameof(UseGridGizmo))]
        [Indent(2)]
        [LabelText("복도연결 임시제어 색")]
        [LabelWidth(200)]
        public ToggleColor GridColor_TempControl_byConnectHallway = new ToggleColor(Color.magenta.WithMultipliedAlpha(0.2f));



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/그리드 기즈모")]
        [EnableIf(nameof(UseGridGizmo))]
        [Indent(2)]
        [LabelText("임시 테스트색")]
        [LabelWidth(200)]
        public ToggleColor GridColor_TempTest = new ToggleColor(Color.white);



        ///======================================================================================================================================================



        //? 방 기즈모



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/방 기즈모")]
        [EnableIf(nameof(UseGizmo))]
        [Indent(1)]
        [LabelText("방 기즈모 사용")]
        [SerializeField]
        [LabelWidth(200)]
        private bool useRoomGizmo = true;
        public bool UseRoomGizmo => UseGizmo && useRoomGizmo;



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/방 기즈모")]
        [EnableIf(nameof(UseRoomGizmo))]
        [Indent(2)]
        [LabelText("선택 되었을 때만 사용")]
        [SerializeField]
        [LabelWidth(200)]
        private bool useRoomGizmoOnlySelect = false;
        public bool UseRoomGizmoOnlySelect => UseRoomGizmo && useRoomGizmoOnlySelect;



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/방 기즈모")]
        [EnableIf(nameof(UseRoomGizmo))]
        [Indent(1)]
        [LabelText("방 Rect 색")]
        [LabelWidth(200)]
        public ToggleColor RoomRectColor = new ToggleColor(Color.yellow);



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/방 기즈모")]
        [EnableIf(nameof(UseRoomGizmo))]
        [Indent(1)]
        [LabelText("활성화 도어 색")]
        [LabelWidth(200)]
        public ToggleColor DoorGridColor_Enabled = new ToggleColor(Color.green.WithMultipliedAlpha(0.5f));



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/방 기즈모")]
        [EnableIf(nameof(UseRoomGizmo))]
        [Indent(1)]
        [LabelText("비활성화 도어 색")]
        [LabelWidth(200)]
        public ToggleColor DoorGridColor_Disabled = new ToggleColor(Color.red.WithMultipliedAlpha(0.2f));



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/방 기즈모")]
        [EnableIf(nameof(UseRoomGizmo))]
        [Indent(1)]
        [LabelText("방 Rect 뷰어 색")]
        [LabelWidth(200)]
        public ToggleColor RoomRectViewerColor = new ToggleColor(Color.white);



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/방 기즈모")]
        [EnableIf(nameof(UseRoomGizmo))]
        [Indent(1)]
        [LabelText("방 도어 노드 색")]
        [LabelWidth(200)]
        public ToggleColor RoomDoorNodeColor = new ToggleColor(Color.green.WithMultipliedAlpha(0.5f));



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/방 기즈모")]
        [EnableIf(nameof(UseRoomGizmo))]
        [Indent(1)]
        [LabelText("방 도어 복도 보정 그리드 색")]
        [LabelWidth(200)]
        public ToggleColor RoomDoorHallwayCorrectionColor = new ToggleColor(Color.red.WithMultipliedAlpha(0.5f));



        [TitleGroup("기즈모"), FoldoutGroup("기즈모/방 기즈모")]
        [EnableIf(nameof(UseRoomGizmo))]
        [Indent(1)]
        [LabelText("방 도어 복도 경로 그리드 색")]
        [LabelWidth(200)]
        public ToggleColor RoomDoorHallwayPathColor = new ToggleColor(Color.blue.WithMultipliedAlpha(0.5f));



        ///======================================================================================================================================================
    }
}
