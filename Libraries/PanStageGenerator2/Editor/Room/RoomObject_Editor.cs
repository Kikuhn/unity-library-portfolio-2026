using System.Collections;
using UnityEngine;
using UnityEditor;
using System.Text;
using Pan.Util;
using System.Linq;
using DG.Tweening;
using Pan.GridCompatibles2;

using Pan.StageGenerators;
using Pan.StageGenerators.Editor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using Pan.Util.Editors;



namespace Pan.StageGenerators.Editor
{
    //[CanEditMultipleObjects]
    //[CustomEditor(typeof(RoomObject), true)]
    //public class RoomObject_Editor : GridCompatible2Object_Editor<RoomObject>
    //{
    //    ///======================================================================================================================================================



    //    protected override void Awake()
    //    {
    //        base.Awake();
    //        if (!Target.IsInstanced)
    //        {
    //            Target.ManagersWakeUp();
    //        }
    //    }



    //    protected override void OnEnable()
    //    {
    //        base.OnEnable();
    //        if (!Target.IsInstanced)
    //        {
    //            Target.ManagersWakeUp();
    //            Target.FindChilds(false);
    //        }


    //        //? 프리팹 모드라면 베이스 오브젝트 갱신
    //        if (PrefabUtility.GetPrefabAssetType(Target) != PrefabAssetType.NotAPrefab || SU_EditorControl.IsPrefabEditMode())
    //        {
    //            (Target as IBaseMonoBehaviourHolder<RoomObject>).BaseMonoBehvaiour = Target;
    //        }
    //    }



    //    ///======================================================================================================================================================



    //    ///<summary>
    //    ///모든 문을 활성화/비활성화 버튼 여부
    //    /// </summary>
    //    private static bool SetDoorsActive_Enable;



    //    ///======================================================================================================================================================



    //    //? 유틸리티 메서드



    //    ///<summary>
    //    ///에디터용 문의 한글 이름 얻기
    //    /// </summary>
    //    public static string Hangul_DoorDir(EDirection4 doorDir)
    //    {
    //        string roomName;

    //        switch (doorDir)
    //        {
    //            case EDirection4.Left:
    //            roomName = "좌[←]도어";
    //            break;
    //            case EDirection4.Right:
    //            roomName = "우[→]도어";
    //            break;
    //            case EDirection4.Down:
    //            roomName = "하[↓]도어";
    //            break;
    //            case EDirection4.Up:
    //            roomName = "상[↑]도어";
    //            break;
    //            default:
    //            roomName = "";
    //            break;
    //        }

    //        return roomName;
    //    }



    //    ///======================================================================================================================================================



    //    //? 모든 문을 활성화/비활성화 시키는 버튼들
    //    private void Buttons_DoorActivityAll()
    //    {
    //        string label = SetDoorsActive_Enable ? "모든 도어 활성화" : "모든 도어 비활성화";

    //        Color buttonColor = SetDoorsActive_Enable ? SU_ColorPresetRGB.Green_Emerald() : SU_ColorPresetRGB.Red_GrapeFruit1();

    //        SU_CustomEditor.HorizontalGUI(() =>
    //        {
    //            SU_CustomEditor.RenderField_Bool(this, ref SetDoorsActive_Enable, label);

    //            SU_CustomEditor.Render_ButtonWithStyle(this, label, "", x =>
    //            {
    //                x.RoomDoorM.SetActive_All(SetDoorsActive_Enable, false);
    //            }, buttonColor, Color.white);
    //        });
    //    }



    //    //? 방향의 문별로 활성화/비활성화 시키는 버튼들
    //    private void Buttons_DoorActivity(EDirection4 doorDir)
    //    {
    //        string label1 = $"무작위 활성화 (단일)";
    //        string label2 = $"무작위 활성화";
    //        string label3 = $"비활성화 (전체)";

    //        string label4 = $"무작위 열기 (단일)";
    //        string label5 = $"무작위 열기";
    //        string label6 = $"닫기 (전체)";

    //        SU_CustomEditor.AutoLabelField_Head($"{Hangul_DoorDir(doorDir)}", SU_CustomEditor.LabelHeadType.H3, () =>
    //        {
    //            SU_CustomEditor.HorizontalGUI(() =>
    //            {
    //                SU_CustomEditor.Render_ButtonWithStyle(this, label1, "", x => { x.RoomDoorM.SetActive_Random(doorDir, true, true, CustomRandom.Instance); }, SU_ColorPresetRGB.Green_Emerald(), Color.white);
    //                SU_CustomEditor.Render_ButtonWithStyle(this, label2, "", x => { x.RoomDoorM.SetActive_Random(doorDir, true, false, CustomRandom.Instance); }, SU_ColorPresetRGB.Green_Emerald(), Color.white);
    //                SU_CustomEditor.Render_ButtonWithStyle(this, label3, "", x => { x.RoomDoorM.SetActive_All(doorDir, false, false); }, SU_ColorPresetRGB.Red_GrapeFruit1(), Color.white);
    //            });
    //        }, false, false, false);

    //    }



    //    ///======================================================================================================================================================



    //    protected override void OnInspectorGUI_Current()
    //    {
    //        base.OnInspectorGUI_Current();

    //        OnInspectorGUI_RoomObject();
    //    }



    //    ///======================================================================================================================================================



    //    //? 이 RoomObject의 인스펙터
    //    private void OnInspectorGUI_RoomObject()
    //    {
    //        SU_CustomEditor.AutoLabelField_Head($"RoomObject (<color=#4fc1e9>{Target.name}</color>)", SU_CustomEditor.LabelHeadType.H1, () =>
    //        {
    //            SU_CustomEditor.RenderField_Property_Path(this, nameof(Target.RoomGizmo.UseGizmo)._LowerFirst(), "방 기즈모 사용", "", nameof(Target.RoomGizmo)._LowerFirst());
    //            SU_CustomEditor.RenderField_Property_Path(this, nameof(Target.RoomGizmo.CurrentGizmoSetting)._LowerFirst(), "방 기즈모", "", nameof(Target.RoomGizmo)._LowerFirst());
    //            OnInspectorGUI_RoomObject_Summary();
    //            OnInspectorGUI_RoomObject_Info();
    //            OnInspectorGUI_RoomObject_AutoFillChilds();
    //            OnInspectorGUI_RoomObject_RoomDoors();
    //            OnInspectorGUI_RoomObject_RoomVariations();
    //        });
    //    }



    //    private readonly StringBuilder stringBuilder_Summary = new StringBuilder();



    //    //? 이 RoomObject의 요약
    //    private void OnInspectorGUI_RoomObject_Summary()
    //    {
    //        void activeDoorsInfo(StringBuilder stringBuilder, EDirection4 doorDir)
    //        {
    //            if (Target.RoomDoorM.GetDoorCount_FindCondition(doorDir, true) == 0) { return; }

    //            var doorList = Target.RoomDoorM.GetDoorList(doorDir);

    //            for (int i = 0; i < doorList.Count; i++)
    //            {
    //                RoomDoor door = doorList[i];

    //                if (door.IsDoorEnable)
    //                {
    //                    stringBuilder.Append($" <b><i>[{(i)}: <color=#4fc1e9>{door.name}</color>]</i></b>");
    //                }

    //                if (i < doorList.Count - 1) { stringBuilder.Append(", "); }
    //            }
    //        }



    //        SU_CustomEditor.AutoLabelField_Head("요약", SU_CustomEditor.LabelHeadType.H2, () =>
    //        {
    //            int doorCountAll = Target.RoomDoorM.GetDoorCountAll();
    //            int doorCount_Down = Target.RoomDoorM.GetDoorList(EDirection4.Down).Count;
    //            int doorCount_Up = Target.RoomDoorM.GetDoorList(EDirection4.Up).Count;
    //            int doorCount_Left = Target.RoomDoorM.GetDoorList(EDirection4.Left).Count;
    //            int doorCount_Right = Target.RoomDoorM.GetDoorList(EDirection4.Right).Count;

    //            int enableDoorCount_Down = Target.RoomDoorM.GetDoorCount_FindCondition(EDirection4.Down, true);
    //            int enableDoorCount_Up = Target.RoomDoorM.GetDoorCount_FindCondition(EDirection4.Up, true);
    //            int enableDoorCount_Left = Target.RoomDoorM.GetDoorCount_FindCondition(EDirection4.Left, true);
    //            int enableDoorCount_Right = Target.RoomDoorM.GetDoorCount_FindCondition(EDirection4.Right, true);

    //            int variationCount = Target.RoomVariationM.Variations_Count;
    //            bool variationIsActive = Target.RoomVariationM.IsActiveVariation;
    //            var variationActive = Target.RoomVariationM.ActiveVariation;
    //            string variationActiveName = "";
    //            if (variationActive != null) { variationActiveName = variationActive.Name; }


    //            stringBuilder_Summary.Clear();

    //            var roomRect = Target.GetRoomRect();
    //            var roomRectSafeArea_AllDoors_All = Target.GetRoomSafeAreaRect_AllDoors(ESelectActivesMode.All);
    //            var roomRectSafeArea_AllDoors_OnlyEnable = Target.GetRoomSafeAreaRect_AllDoors(ESelectActivesMode.OnlyEnable);
    //            var roomRectSafeArea_AllDoors_OnlyDisable = Target.GetRoomSafeAreaRect_AllDoors(ESelectActivesMode.OnlyDisable);
    //            var roomGridRect = Target.GetRoomGridRect_NotUseSize();


    //            stringBuilder_Summary.AppendLine($"방 Rect:\n\t<color=#ac92ec><b>center: {roomRect.center}\n\tsize: {roomRect.size}</b></color>");
    //            stringBuilder_Summary.AppendLine();
    //            stringBuilder_Summary.AppendLine($"방 그리드 Rect:\n\t<color=#ac92ec><b>{roomGridRect}</b></color>");
    //            stringBuilder_Summary.AppendLine();
    //            stringBuilder_Summary.AppendLine($"   방 안전구역 Rect (모든문:All):\n\t<color=#ac92ec><b>center: {roomRectSafeArea_AllDoors_All.center}\n\tsize: {roomRectSafeArea_AllDoors_All.size}</b></color>");
    //            stringBuilder_Summary.AppendLine();
    //            stringBuilder_Summary.AppendLine($"   방 안전구역 Rect (모든문:OnlyEnable):\n\t<color=#ac92ec><b>center: {roomRectSafeArea_AllDoors_OnlyEnable.center}\n\tsize: {roomRectSafeArea_AllDoors_OnlyEnable.size}</b></color>");
    //            stringBuilder_Summary.AppendLine();
    //            stringBuilder_Summary.AppendLine($"   방 안전구역 Rect (모든문:OnlyDisable):\n\t<color=#ac92ec><b>center: {roomRectSafeArea_AllDoors_OnlyEnable.center}\n\tsize: {roomRectSafeArea_AllDoors_OnlyEnable.size}</b></color>");
    //            stringBuilder_Summary.AppendLine();


    //            stringBuilder_Summary.AppendLine($"모든 문Door의 총 개수: <color=#2ecc71><b><i>{doorCountAll}</i></b></color>");



    //            stringBuilder_Summary.Append("   ");
    //            if (doorCount_Down == 0) { stringBuilder_Summary.Append("<color=grey>"); }

    //            stringBuilder_Summary.Append($"하[↓] 문Door의 개수: ");
    //            stringBuilder_Summary.AppendLine($"<color=#2ecc71><b><i>{doorCount_Down}</i></b></color>");
    //            stringBuilder_Summary.Append($"      활성화된 문Door 개수:<color=#2ecc71><b><i> {enableDoorCount_Down}</i></b></color>");
    //            activeDoorsInfo(stringBuilder_Summary, EDirection4.Down);
    //            stringBuilder_Summary.AppendLine();

    //            if (doorCount_Down == 0) { stringBuilder_Summary.Append("</color>"); }



    //            stringBuilder_Summary.Append("   ");
    //            if (doorCount_Up == 0) { stringBuilder_Summary.Append("<color=grey>"); }

    //            stringBuilder_Summary.Append($"상[↑] 문Door의 개수: ");
    //            stringBuilder_Summary.AppendLine($"<color=#2ecc71><b><i>{doorCount_Up}</i></b></color>");
    //            stringBuilder_Summary.Append($"      활성화된 문Door 개수: <color=#2ecc71><b><i>{enableDoorCount_Up}</i></b></color>");
    //            activeDoorsInfo(stringBuilder_Summary, EDirection4.Up);
    //            stringBuilder_Summary.AppendLine();

    //            if (doorCount_Up == 0) { stringBuilder_Summary.Append("</color>"); }



    //            stringBuilder_Summary.Append("   ");
    //            if (doorCount_Left == 0) { stringBuilder_Summary.Append("<color=grey>"); }

    //            stringBuilder_Summary.Append($"좌[←] 문Door의 개수: ");
    //            stringBuilder_Summary.AppendLine($"<color=#2ecc71><b><i>{doorCount_Left}</i></b></color>");
    //            stringBuilder_Summary.Append($"      활성화된 문Door 개수: <color=#2ecc71><b><i>{enableDoorCount_Left}</i></b></color>");
    //            activeDoorsInfo(stringBuilder_Summary, EDirection4.Left);
    //            stringBuilder_Summary.AppendLine();

    //            if (doorCount_Left == 0) { stringBuilder_Summary.Append("</color>"); }



    //            stringBuilder_Summary.Append("   ");
    //            if (doorCount_Right == 0) { stringBuilder_Summary.Append("<color=grey>"); }

    //            stringBuilder_Summary.Append($"우[→] 문Door의 개수: ");
    //            stringBuilder_Summary.AppendLine($"<color=#2ecc71><b><i>{doorCount_Right}</i></b></color>");
    //            stringBuilder_Summary.Append($"      활성화된 문Door 개수: <color=#2ecc71><b><i>{enableDoorCount_Right}</i></b></color>");
    //            activeDoorsInfo(stringBuilder_Summary, EDirection4.Right);
    //            stringBuilder_Summary.AppendLine();

    //            if (doorCount_Right == 0) { stringBuilder_Summary.Append("</color>"); }


    //            var gap_Down = Target.RoomDoorM.GetDoorsGaps(EDirection4.Down, out var min_Down, out var max_Down);
    //            var gap_Up = Target.RoomDoorM.GetDoorsGaps(EDirection4.Up, out var min_Up, out var max_Up);
    //            var gap_Left = Target.RoomDoorM.GetDoorsGaps(EDirection4.Left, out var min_Left, out var max_Left);
    //            var gap_Right = Target.RoomDoorM.GetDoorsGaps(EDirection4.Right, out var min_Right, out var max_Right);


    //            if (gap_Down)
    //            {
    //                stringBuilder_Summary.AppendLine($"하단 문Door의 이웃 간격 <color=#2ecc71><b>{min_Down}</b></color>\t최대 간격 <color=#2ecc71><b>{max_Down}</b></color>");
    //                if (min_Down < 0 || max_Down < 0)
    //                {
    //                    stringBuilder_Summary.AppendLine($"\t<color=red><b>도어 간격이 겹쳐있는 에러 발생!</b></color>");
    //                    Debug.LogError("하단 도어가 겹쳐있는 에러 발생!");
    //                }
    //            }

    //            if (gap_Up)
    //            {
    //                stringBuilder_Summary.AppendLine($"상단 문Door의 이웃 간격 <color=#2ecc71><b>{min_Up}</b></color>\t최대 간격 <color=#2ecc71><b>{max_Up}</b></color>");
    //                if (min_Up < 0 || max_Up < 0)
    //                {
    //                    stringBuilder_Summary.AppendLine($"\t<color=red><b>도어 간격이 겹쳐있는 에러 발생!</b></color>");
    //                    Debug.LogError("상단 도어가 겹쳐있는 에러 발생!");
    //                }
    //            }

    //            if (gap_Left)
    //            {
    //                stringBuilder_Summary.AppendLine($"좌측 문Door의 이웃 간격 <color=#2ecc71><b>{min_Left}</b></color>\t최대 간격 <color=#2ecc71><b>{max_Left}</b></color>");
    //                if (min_Left < 0 || max_Right < 0)
    //                {
    //                    stringBuilder_Summary.AppendLine($"\t<color=red><b>도어 간격이 겹쳐있는 에러 발생!</b></color>");
    //                    Debug.LogError("좌측 도어가 겹쳐있는 에러 발생!");
    //                }
    //            }

    //            if (gap_Right)
    //            {
    //                stringBuilder_Summary.AppendLine($"우측 문Door의 이웃 간격 <color=#2ecc71><b>{min_Right}</b></color>\t최대 간격 <color=#2ecc71><b>{max_Right}</b></color>");
    //                if (min_Right < 0 || max_Right < 0)
    //                {
    //                    stringBuilder_Summary.AppendLine($"\t<color=red><b>도어 간격이 겹쳐있는 에러 발생!</b></color>");
    //                    Debug.LogError("우측 도어가 겹쳐있는 에러 발생!");
    //                }
    //            }




    //            SU_String.WrapTargetSubstring(stringBuilder_Summary, "문Door", "<color=#4fc1e9><b>", "</b></color>");
    //            stringBuilder_Summary.Replace("문Door", "문");



    //            stringBuilder_Summary.AppendLine();
    //            stringBuilder_Summary.AppendLine($"바리에이션Variation의 총 개수: <color=#2ecc71><b><i>{variationCount}</i></b></color>");
    //            stringBuilder_Summary.Append($"   ");
    //            stringBuilder_Summary.Append($"활성화된 바리에이션Variation: ");

    //            if (variationIsActive)
    //            {
    //                stringBuilder_Summary.AppendLine($"{variationActiveName} 바리에이션Variation 활성화");
    //            }
    //            else
    //            {
    //                stringBuilder_Summary.AppendLine($"<color=grey>비활성화</color>");
    //            }



    //            SU_String.WrapTargetSubstring(stringBuilder_Summary, "바리에이션Variation", "<color=#4fc1e9><b>", "</b></color>");
    //            stringBuilder_Summary.Replace("바리에이션Variation", "바리에이션");



    //            SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder_Summary.ToString(true));
    //        });
    //    }



    //    //? 이 RoomObject의 메인정보
    //    private void OnInspectorGUI_RoomObject_Info()
    //    {
    //        //SU_CustomEditor.AutoLabelField_Head("정보", SU_CustomEditor.LabelHeadType.H2, () =>
    //        //{
    //        //    //SU_CustomEditor.RenderField_Property(this, nameof(Target.RoomSafeAreaSize).LowerFirst(), "방 안전구역 크기");
    //        //});
    //    }



    //    //? 이 RoomObject의 자식 자동 채우기
    //    private void OnInspectorGUI_RoomObject_AutoFillChilds()
    //    {
    //        SU_CustomEditor.AutoLabelField_Head("자식 자동 채우기", SU_CustomEditor.LabelHeadType.H2, () =>
    //        {
    //            SU_CustomEditor.Render_Button(this, "전부 자동 채우기", x =>
    //            {
    //                x.FindChilds(true);
    //            });

    //            SU_CustomEditor.HorizontalGUI(() =>
    //            {
    //                SU_CustomEditor.Render_Button(this, "도어 자동 채우기", x => x.RoomDoorM.FindChildDoors(true));
    //            });
    //        });
    //    }



    //    //? 이 RoomObject의 문 관련
    //    private void OnInspectorGUI_RoomObject_RoomDoors()
    //    {
    //        SU_CustomEditor.AutoLabelField_Head("문", SU_CustomEditor.LabelHeadType.H2, () =>
    //        {
    //            Buttons_DoorActivityAll();
    //            Buttons_DoorActivity(EDirection4.Down);
    //            Buttons_DoorActivity(EDirection4.Up);
    //            Buttons_DoorActivity(EDirection4.Left);
    //            Buttons_DoorActivity(EDirection4.Right);
    //        });
    //    }



    //    //? 이 RoomObject의 바리에이션 관련
    //    private void OnInspectorGUI_RoomObject_RoomVariations()
    //    {
    //        SU_CustomEditor.AutoLabelField_Head("바리에이션", SU_CustomEditor.LabelHeadType.H2, () =>
    //        {
    //            SU_CustomEditor.Render_ButtonWithStyle(this, "바리에이션 제거", "", x =>
    //            {
    //                x.RoomVariationM.Disable_ActiveVariation();
    //            }, SU_ColorPresetRGB.Red_GrapeFruit1(), Color.white);

    //            SU_CustomEditor.HorizontalGUI(() =>
    //            {
    //                SU_CustomEditor.Render_ButtonWithStyle(this, "무작위로 적용", "", x =>
    //                {
    //                    x.RoomVariationM.Enable_Variation_Random(CustomRandom.Instance, false);
    //                }, SU_ColorPresetRGB.Blue_Aqua1(), Color.white);

    //                SU_CustomEditor.Render_ButtonWithStyle(this, "무작위로 적용 (미적용 포함)", "", x =>
    //                {
    //                    x.RoomVariationM.Enable_Variation_Random(CustomRandom.Instance, true);
    //                }, SU_ColorPresetRGB.Blue_Aqua1(), Color.white);
    //            });

    //        }, false);
    //    }



    //    ///======================================================================================================================================================



    //    ////? 바리에이션 적용
    //    //private void Apply_Variable_Random(bool canApplyDefault)
    //    //{
    //    //    Undo.RecordObject(Target, Target.name + "바리에이션 무작위로 적용하기");

    //    //    Target.RoomVariationM.SetDisable_All();
    //    //    Target.RoomVariationM.Enable_Variation_Random(CustomRandom.Instance, true, canApplyDefault, out var rand);


    //    //    Debug.Log("바리에이션 적용 " + rand);

    //    //    EditorUtility.SetDirty(Target);
    //    //}



    //    ////? 바리에이션 활성화/비활성화
    //    //private void SetActiveVariable(bool enable)
    //    //{
    //    //    Undo.RecordObject(Target, Target.name + "바리에이션 활성화/비활성화");

    //    //    Target.RoomVariationM.SetDisable_All();

    //    //    EditorUtility.SetDirty(Target);
    //    //}



    //    ///======================================================================================================================================================
    //}

    //! 250619 일단전부 비활성화
}