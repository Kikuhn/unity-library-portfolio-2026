#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pan.Util;
using System.Text;
using DG.DemiEditor;
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using Pan.GridCompatibles2;
using Pan.StageGenerators;
using Pan.Util.Editors;



namespace Pan.StageGenerators.Gizmo
{
    //[Serializable]
    //public class RoomDoor_Gizmo : BaseGizmoClass<RoomDoor>
    //{
    //    ///======================================================================================================================================================



    //    private readonly StringBuilder stringBuilder_DoorText = new StringBuilder();



    //    //? 기즈모를 그릴수있는지 확인
    //    private bool CanDrawGizmo(RoomDoor main, out RoomGizmoSetting roomGizmoSetting)
    //    {
    //        if (main == null ||
    //            main.ParentRoom == null ||
    //            main.ParentRoom.RoomGizmo == null ||
    //            main.ParentRoom.RoomGizmo.CurrentGizmoSetting == null ||
    //            main.ParentRoom.RoomGizmo.CanDrawGizmo(main.ParentRoom) == false
    //            )
    //        {
    //            roomGizmoSetting = null;
    //            return false;
    //        }

    //        roomGizmoSetting = main.ParentRoom.RoomGizmo.CurrentGizmoSetting.GetRoomGizmoSetting;

    //        if (!roomGizmoSetting.UseGizmo || !roomGizmoSetting.UseGizmo_Door)
    //        {
    //            return false;
    //        }

    //        return true;
    //    }



    //    //? 텍스트를 그릴수있는지 확인
    //    private bool Check_CanDrawText(RoomGizmoSetting setting)
    //    {
    //        return !(setting.UseGizmo_UseTextOnlyPrefab && !PrefabStageUtility.GetCurrentPrefabStage());
    //    }



    //    //? 도어 정보 GUI 스타일 얻기
    //    private GUIStyle DoorInfoTextGUIStyle() => new GUIStyle()
    //    {
    //        fontSize = 10,
    //        alignment = TextAnchor.MiddleCenter,
    //        fontStyle = FontStyle.Bold,
    //        richText = true,
    //        normal = { textColor = Gizmos.color }
    //    };



    //    //? 도어 정보 텍스트 좌표 얻기
    //    private Vector3 DoorIntoTextPosition(RoomDoor main, Vector3 originalPosition, GridCompatible2 gridCompatible)
    //    {
    //        var result = originalPosition + new Vector3(0, 0, main.Size.z / 2f).SwizzlesVector(gridCompatible.CurrentSnapSetting.Swizzle, true);

    //        Vector3 plusPosition = Vector3.zero;

    //        switch (main.Direction)
    //        {
    //            case EDirection4.Down:
    //            plusPosition += new Vector3(0, main.Size.y, 0);
    //            break;

    //            case EDirection4.Up:
    //            plusPosition += new Vector3(0, -main.Size.y, 0);
    //            break;

    //            case EDirection4.Left:
    //            plusPosition += new Vector3(main.Size.x, 0, 0);
    //            break;

    //            case EDirection4.Right:
    //            plusPosition += new Vector3(-main.Size.x, 0, 0);
    //            break;
    //        }

    //        plusPosition.MultiplyRef(1f - (gridCompatible.CurrentSnapSetting.GridUnitVector3Original.GetMaxComponent() * 0.5f));

    //        plusPosition = plusPosition.SwizzlesVector(gridCompatible.CurrentSnapSetting.Swizzle, false);

    //        return result + plusPosition;
    //    }



    //    //? 문의 방향에 따라 텍스트 반환
    //    private string GetDoorDirectionText(RoomDoor main)
    //    {
    //        switch (main.Direction)
    //        {
    //            case EDirection4.Down: return "[↓] Down";
    //            case EDirection4.Up: return "[↑] Up";
    //            case EDirection4.Left: return "[←] Left";
    //            case EDirection4.Right: return "[→] Right";
    //            default: return "";
    //        }
    //    }



    //    ///======================================================================================================================================================



    //    //? 기즈모 그리기



    //    protected override void DrawGizmo(RoomDoor main)
    //    {
    //        if (!CanDrawGizmo(main, out var roomGizmoSetting)) { return; }

    //        var gridCompatible = main.ParentRoom.GridCompatible;
    //        var swizzle = gridCompatible.CurrentSnapSetting.Swizzle;

    //        var roomSafeAreaRect = main.ParentRoom.GetRoomSafeAreaRect_AllDoors(ESelectActivesMode.All);


    //        //? 문의 기즈모 색 지정 (활성화/비활성화 여부에 따라)
    //        Gizmos.color = main.IsDoorEnable ? roomGizmoSetting.Door_EnableColor : roomGizmoSetting.Door_DisableColor;


    //        Vector3 doorSize_Swizzle = main.GetSize(true, true);
    //        Vector3 doorSize = main.GetSize(false, true);

    //        Vector3 doorSize_Swizzle2 = doorSize_Swizzle;
    //        SU_TF_Vector.ModifyVectorBySwizzle(ref doorSize_Swizzle2, EAxis.X, Mathf.Max(doorSize.x, doorSize.y), swizzle);
    //        SU_TF_Vector.ModifyVectorBySwizzle(ref doorSize_Swizzle2, EAxis.Y, Mathf.Max(doorSize.x, doorSize.y), swizzle);


    //        Vector3 originalPos = main.GetOriginalPosition;
    //        Vector3 startDoorPos = main.GetStartPosition + new Vector3(0, 0, doorSize.z / 2f).SwizzlesVector(swizzle, true);
    //        Vector3 endDoorPos = main.GetEndPosition(true, false);
    //        Vector3 endDoorPos_Correction = main.GetEndPosition(true, true);


    //        var gridUnit = gridCompatible.CurrentSnapSetting.GridUnitVector3Original;
    //        var gridUnit_Radius = gridUnit.GetMinComponent();
    //        var gridUnit_RadiusHalf = gridUnit_Radius / 2f;
    //        var gridUnit_Swizzled = gridCompatible.CurrentSnapSetting.GridUnitCurrent;


    //        //? 중심점 그리기
    //        SU_Gizmo.TempGizmoColor_Action(SU_Color.WithAlpha(Gizmos.color, 0.5f), () =>
    //        {
    //            Gizmos.DrawSphere(originalPos, gridCompatible.CurrentSnapSetting.GridUnitVector3Original.GetMaxComponent() / 4f);
    //        });



    //        //? Start 도어 큐브 그리기 (테두리)
    //        Gizmos.DrawWireCube(startDoorPos, doorSize_Swizzle);



    //        //? End 도어 큐브 그리기 (테두리)
    //        Gizmos.DrawWireCube(endDoorPos, doorSize_Swizzle);
    //        Gizmos.DrawWireCube(endDoorPos_Correction, doorSize_Swizzle);



    //        //? Start, End 도어 큐브 그리기 (투명도값을 낮춘후 지정)
    //        SU_Gizmo.TempGizmoColor_Action(SU_Color.WithMultipliedAlpha(Gizmos.color, 0.2f), () =>
    //        {
    //            Gizmos.DrawCube(startDoorPos, doorSize_Swizzle);
    //            Gizmos.DrawCube(endDoorPos, doorSize_Swizzle);
    //            //Gizmos.DrawCube(endDoorPos_Correction, doorSize_Swizzle);
    //        });



    //        //? 도어 정보 텍스트 그리기
    //        if (Check_CanDrawText(roomGizmoSetting))
    //        {
    //            SU_Gizmo.TempGizmoColor_Action(Gizmos.color.WithAlpha(1f), () =>
    //            {
    //                stringBuilder_DoorText.Clear();
    //                stringBuilder_DoorText.AppendLine($"{GetDoorDirectionText(main)} Door [ {main.Index.ToString("D2")} ]");
    //                stringBuilder_DoorText.AppendLine($"({main.gameObject.name})");

    //                Handles.Label(DoorIntoTextPosition(main, originalPos, gridCompatible), stringBuilder_DoorText.ToString(true), DoorInfoTextGUIStyle());
    //            });
    //        }



    //        //? 도어 연결 기즈모 그리기
    //        if (main.ConnectingDoor != null)
    //        {
    //            Gizmos.color = roomGizmoSetting.Door_NodeColor;
    //            Gizmos.DrawLine(main.GetStartPosition, main.ConnectingDoor.GetStartPosition);
    //        }



    //        //? 도어 Start~End 화살표 그리기
    //        SU_Gizmo.TempGizmoColor_Action(Gizmos.color.WithAlpha(1f), () =>
    //        {
    //            SU_Gizmo.DrawArrow(startDoorPos, endDoorPos);
    //        });



    //        //? 도어 End~End방향 화살표 그리기
    //        //Vector3 arrow2StartPos = endDoorPos + (endPosition_Relative / 2f);
    //        Vector3 arrow2StartPos = endDoorPos_Correction + (main.Direction.ToVector2() / 2f).SwizzlesVector2To3(swizzle);
    //        Vector3 arrow2EndPos = arrow2StartPos;



    //        switch (main.Direction)
    //        {
    //            case EDirection4.Down:
    //            arrow2EndPos -= new Vector3(0, gridUnit.y * 2, 0).SwizzlesVector(swizzle);
    //            break;

    //            case EDirection4.Up:
    //            arrow2EndPos += new Vector3(0, gridUnit.y * 2, 0).SwizzlesVector(swizzle);
    //            break;

    //            case EDirection4.Left:
    //            arrow2EndPos -= new Vector3(gridUnit.x * 2, 0, 0).SwizzlesVector(swizzle);
    //            break;

    //            case EDirection4.Right:
    //            arrow2EndPos += new Vector3(gridUnit.x * 2, 0, 0).SwizzlesVector(swizzle);
    //            break;
    //        }



    //        SU_Gizmo.DrawArrow(arrow2StartPos, arrow2EndPos, 20, gridUnit.GetMaxComponent() / 1.25f);



    //        //? Mid 그리기
    //        Vector3 doorMidPosition = main.GetMidPosition(true);
    //        Vector3 doorMidSize = main.GetMidSize(true, true);

    //        SU_TF_Vector.ModifyVectorBySwizzle(ref doorMidSize, EAxis.Z, 0.1f, swizzle);


    //        SU_Gizmo.TempGizmoColor_Action(SU_Color.WithMultipliedAlpha(Gizmos.color, 0.1f), () =>
    //        {
    //            Gizmos.DrawCube(doorMidPosition, doorMidSize);
    //        });



    //        //Debug.Log($"{name}: 너비: {Main.Width} 가변 너비: {Main.GetWidth(false, false)} 가변 너비2: {Main.GetWidth(false, false)} 사이즈X: {Main.Size.x}");


    //        //? 선택시 가이드라인 생성
    //        if (SU_EditorControl.IsObjectSelected(main.transform, true, true) && Tools.current == Tool.Move)
    //        {
    //            Vector3 roomPos = main.ParentRoom.GridCenterTransformPosition;
    //            Vector3 roomSize = (main.ParentRoom.GetRoomSafeAreaRect_AllDoors(ESelectActivesMode.All).size).SwizzlesVector2To3(swizzle);
    //            SU_Gizmo.DrawCrossGuideLine(originalPos, roomPos, roomSize, swizzle, gridUnit.GetMaxComponent());
    //            SU_Gizmo.DrawCrossGuideLine(originalPos, roomSafeAreaRect.center.SwizzlesVector2To3(swizzle), roomSafeAreaRect.size.SwizzlesVector2To3(swizzle), swizzle, gridUnit.GetMaxComponent());
    //        }



    //        if (SU_EditorControl.IsObjectSelected(main.transform, true, true))
    //        {
    //            //? 도어 표시
    //            if (roomGizmoSetting.UseGizmo_DoorGrid && (main.GetDoorGrids != null && main.GetDoorGrids.Count != 0))
    //            {
    //                Gizmos.color = roomGizmoSetting.DoorGridColor;

    //                if (main.GetDoorGrids != null && main.GetDoorGrids.Count != 0)
    //                {
    //                    foreach (var hallwayGrid in main.GetDoorGrids)
    //                    {
    //                        Gizmos.DrawCube(hallwayGrid.WorldPosition, gridUnit);
    //                    }
    //                }
    //            }

    //            if (roomGizmoSetting.UseGizmo_CorrectionHallwayGrid && (main.GetHallwayCorrectionGrids != null && main.GetHallwayCorrectionGrids.Count != 0))
    //            {
    //                Gizmos.color = roomGizmoSetting.CorrectionHallwayGridColor;

    //                if (main.GetHallwayCorrectionGrids != null && main.GetHallwayCorrectionGrids.Count != 0)
    //                {
    //                    foreach (var hallwayGrid in main.GetHallwayCorrectionGrids)
    //                    {
    //                        Gizmos.DrawCube(hallwayGrid.WorldPosition, gridUnit);
    //                        //SU_Gizmo.DrawRotatedCube45(hallwayGrid.WorldPosition, gridUnit, swizzle);
    //                    }
    //                }
    //            }

    //            if (roomGizmoSetting.UseGizmo_PathHallwayGrid && (main.GetHallwayPathGrids != null && main.GetHallwayPathGrids.Count != 0))
    //            {
    //                Gizmos.color = roomGizmoSetting.PathHallwayGridColor;

    //                if (main.GetHallwayPathGrids != null && main.GetHallwayPathGrids.Count != 0)
    //                {
    //                    foreach (var hallwayGrid in main.GetHallwayPathGrids)
    //                    {
    //                        Gizmos.DrawCube(hallwayGrid.WorldPosition, gridUnit);
    //                        //SU_Gizmo.DrawRotatedCube45(hallwayGrid.WorldPosition, gridUnit * 0.9f, swizzle);
    //                    }
    //                }
    //            }
    //        }
    //    }



    //    ///======================================================================================================================================================
    //}
}
#endif