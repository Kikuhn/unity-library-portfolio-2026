using Pan.Util;
using Sirenix.OdinInspector;
using UnityEngine;



namespace Pan.StageGenerators
{
    [CreateAssetMenu(menuName = CreateAssetMenuInfo.STAGEGEN_SINGLETON_SO_MANAGER, fileName = "StageGenerator2SingletonSOManager")]
    public class StageGenerator2SingletonSOManager : SingleTon_ScriptableObject<StageGenerator2SingletonSOManager>
    {
#if UNITY_EDITOR

        [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Center, EnableRichText = true, Overflow = false), EnableGUI]
        [PropertyOrder(-100)]
        [PropertySpace(8, 8)]
        private string dummy_Title
        {
            get
            {
                return $"<b><size=15><color=white>StageGenerator 싱글톤 SO 매니저</color></size></b>\n<i>StageGenerator2 패키지 경로에 단 하나만 생성할것</i>\n<color=red><b>제거 금지</b></color>";
            }
        }
#endif



        public GridTagSettingSbject GridTagSetting_RoomMain => gridTagSetting_RoomMain;
        [TitleGroup("그리드 태그 설정 기본값"), BoxGroup("그리드 태그 설정 기본값/박스", false)]
        [Title("방 메인")]
        [LabelText("RoomMain")]
        [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
        private GridTagSettingSbject gridTagSetting_RoomMain;



        public GridTagSettingSbject GridTagSetting_RoomSafe => gridTagSetting_RoomSafe;
        [TitleGroup("그리드 태그 설정 기본값"), BoxGroup("그리드 태그 설정 기본값/박스", false)]
        [Title("방 안전구역")]
        [LabelText("RoomSafe")]
        [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
        private GridTagSettingSbject gridTagSetting_RoomSafe;



        public GridTagSettingSbject GridTagSetting_RoomExpandSafe => gridTagSetting_RoomExpandSafe;
        [TitleGroup("그리드 태그 설정 기본값"), BoxGroup("그리드 태그 설정 기본값/박스", false)]
        [Title("방 확장 안전구역")]
        [LabelText("RoomExpandSafe")]
        [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
        private GridTagSettingSbject gridTagSetting_RoomExpandSafe;



        public GridTagSettingSbject GridTagSetting_RoomEdge => gridTagSetting_RoomEdge;
        [TitleGroup("그리드 태그 설정 기본값"), BoxGroup("그리드 태그 설정 기본값/박스", false)]
        [Title("방 테두리")]
        [LabelText("RoomEdge")]
        [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
        private GridTagSettingSbject gridTagSetting_RoomEdge;



        public GridTagSettingSbject GridTagSetting_RoomDoor => gridTagSetting_RoomDoor;
        [TitleGroup("그리드 태그 설정 기본값"), BoxGroup("그리드 태그 설정 기본값/박스", false)]
        [Title("방 도어")]
        [LabelText("RoomDoor")]
        [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
        private GridTagSettingSbject gridTagSetting_RoomDoor;



        public GridTagSettingSbject GridTagSetting_RoomDoorStart => gridTagSetting_RoomDoorStart;
        [TitleGroup("그리드 태그 설정 기본값"), BoxGroup("그리드 태그 설정 기본값/박스", false)]
        [Title("방 도어 시작")]
        [LabelText("RoomDoorStart")]
        [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
        private GridTagSettingSbject gridTagSetting_RoomDoorStart;



        public GridTagSettingSbject GridTagSetting_RoomDoorEnd => gridTagSetting_RoomDoorEnd;
        [TitleGroup("그리드 태그 설정 기본값"), BoxGroup("그리드 태그 설정 기본값/박스", false)]
        [Title("방 도어 끝")]
        [LabelText("RoomDoorEnd")]
        [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
        private GridTagSettingSbject gridTagSetting_RoomDoorEnd;



        public GridTagSettingSbject GridTagSetting_RoomDoorAreaStartEnd => gridTagSetting_RoomDoorAreaStartEnd;
        [TitleGroup("그리드 태그 설정 기본값"), BoxGroup("그리드 태그 설정 기본값/박스", false)]
        [Title("방 도어 영역 시작끝")]
        [LabelText("RoomDoorAreaStartEnd")]
        [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
        private GridTagSettingSbject gridTagSetting_RoomDoorAreaStartEnd;



        public GridTagSettingSbject GridTagSetting_RoomDoorArea => gridTagSetting_RoomDoorArea;
        [TitleGroup("그리드 태그 설정 기본값"), BoxGroup("그리드 태그 설정 기본값/박스", false)]
        [Title("방 도어 영역")]
        [LabelText("RoomDoorArea")]
        [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
        private GridTagSettingSbject gridTagSetting_RoomDoorArea;



        public GridTagSettingSbject GridTagSetting_RoomDoorAreaExpand => gridTagSetting_RoomDoorAreaExpand;
        [TitleGroup("그리드 태그 설정 기본값"), BoxGroup("그리드 태그 설정 기본값/박스", false)]
        [Title("방 도어 확장")]
        [LabelText("RoomDoorAreaExpand")]
        [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
        private GridTagSettingSbject gridTagSetting_RoomDoorAreaExpand;



        public GridTagSettingSbject GridTagSetting_HallwayMain => gridTagSetting_HallwayMain;
        [TitleGroup("그리드 태그 설정 기본값"), BoxGroup("그리드 태그 설정 기본값/박스", false)]
        [Title("메인 복도")]
        [LabelText("HallwayMain")]
        [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
        private GridTagSettingSbject gridTagSetting_HallwayMain;



        public GridTagSettingSbject GridTagSetting_HallwayExpand => gridTagSetting_HallwayExpand;
        [TitleGroup("그리드 태그 설정 기본값"), BoxGroup("그리드 태그 설정 기본값/박스", false)]
        [Title("확장 복도")]
        [LabelText("HallwayExpand")]
        [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
        private GridTagSettingSbject gridTagSetting_HallwayExpand;



        public GridTagSettingSbject GridTagSetting_HallwayEdge => gridTagSetting_HallwayEdge;
        [TitleGroup("그리드 태그 설정 기본값"), BoxGroup("그리드 태그 설정 기본값/박스", false)]
        [Title("복도 테두리")]
        [LabelText("HallwayEdge")]
        [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
        private GridTagSettingSbject gridTagSetting_HallwayEdge;



        public GridTagSettingSbject GridTagSetting_HallwaySafe => gridTagSetting_HallwaySafe;
        [TitleGroup("그리드 태그 설정 기본값"), BoxGroup("그리드 태그 설정 기본값/박스", false)]
        [Title("복도 안전구역")]
        [LabelText("HallwaySafe")]
        [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
        private GridTagSettingSbject gridTagSetting_HallwaySafe;



        public GridTagSettingSbject GridTagSetting_HallwayDoorCorrectionEdge => gridTagSetting_HallwayDoorCorrectionEdge;
        [TitleGroup("그리드 태그 설정 기본값"), BoxGroup("그리드 태그 설정 기본값/박스", false)]
        [Title("도어 보정 복도 테두리")]
        [LabelText("HallwayDoorCorrectionEdge")]
        [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
        private GridTagSettingSbject gridTagSetting_HallwayDoorCorrectionEdge;



        public GridTagSettingSbject GridTagSetting_HallwayDoorCorrectionSafe => gridTagSetting_HallwayDoorCorrectionSafe;
        [TitleGroup("그리드 태그 설정 기본값"), BoxGroup("그리드 태그 설정 기본값/박스", false)]
        [Title("도어 보정 복도 안전구역")]
        [LabelText("HallwayDoorCorrectionSafe")]
        [SerializeField, InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
        private GridTagSettingSbject gridTagSetting_HallwayDoorCorrectionSafe;
    }
}
