using System.Collections;
using UnityEngine;
using UnityEditor;
using Pan.Util;
using System.Text;
using Pan.GridCompatibles2;

using Pan.StageGenerators;
using Pan.StageGenerators.Editor;
using System;
using System.Collections.Generic;
using Pan.Util.Editors;



namespace Pan.StageGenerators.Editor
{
    //[CanEditMultipleObjects]
    //[CustomEditor(typeof(RoomDoor), true)]
    //public class RoomDoor_Editor : BaseRoomChildObject_Editor<RoomDoor>
    //{
    //    ///======================================================================================================================================================



    //    private RoomDoor.IEdit TargetEdit => Target;



    //    ///======================================================================================================================================================



    //    protected override void Awake()
    //    {
    //        base.Awake();
    //        if (Target.ParentRoom != null && !Target.ParentRoom.IsInstanced)
    //        {
    //            Target.Refresh_All();
    //        }
    //    }



    //    protected override void OnEnable()
    //    {
    //        base.OnEnable();
    //        if (Target.ParentRoom != null && !Target.ParentRoom.IsInstanced)
    //        {
    //            Target.Refresh_All();
    //        }
    //    }



    //    ///======================================================================================================================================================



    //    private readonly StringBuilder stringBuilder_WarningSummary = new StringBuilder();
    //    private readonly StringBuilder stringBuilder_Summary = new StringBuilder();
    //    private readonly StringBuilder stringBuilder_ConnectSummary1 = new StringBuilder();
    //    private readonly StringBuilder stringBuilder_ConnectSummary2 = new StringBuilder();



    //    private string Hangul_ValidParentRoom()
    //    {
    //        return (Target.ParentRoom != null) ? "<color=#2ecc71><b>종속됨 확인</b></color>" : "<color=#ed5565><b>미확인</b></color>";
    //    }

    //    private string Hangul_DoorActivity()
    //    {
    //        return (Target.IsDoorEnable) ? "<color=#2ecc71><b>활성화 Enabled</b></color>" : "<color=#ed5565><b>비활성화 Disabled</b></color>";
    //    }

    //    private string Hangul_DoorOpen()
    //    {
    //        return (Target.IsDoorOpen) ? "<color=#2ecc71><b>개 Opened</b></color>" : "<color=#ed5565><b>폐 Closed</b></color>";
    //    }

    //    private string Hangul_DoorWidthAxis()
    //    {
    //        return Target.CheckDirectionVertical() ? "X축" : "Y축";
    //    }

    //    private string Hangul_DoorHeightAxis()
    //    {
    //        return Target.CheckDirectionVertical() ? "Y축" : "X축";
    //    }

    //    private string Hangul_ConnectHallway()
    //    {
    //        return (Target.IsCreatedPathHallway) ? $"<color=#2ecc71><b>복도와 연결됨</b></color>" : "<color=#ed5565><b>복도와 연결안됨</b></color>";
    //    }

    //    private string Hangul_ConnectDoor()
    //    {
    //        return (Target.ConnectingDoor != null) ? $"<color=#2ecc71><b>연결됨 {Target.ParentRoom.name} - {Target.ConnectingDoor.name}</b></color>" : "<color=#ed5565><b>없음</b></color>";
    //    }



    //    ///======================================================================================================================================================



    //    protected override void OnInspectorGUI_Current()
    //    {
    //        SU_CustomEditor.AutoLabelField_Head($"RoomDoor (<color=#4fc1e9>{Target.name}</color>)", SU_CustomEditor.LabelHeadType.H1, () =>
    //        {
    //            SU_CustomEditor.RenderField_Property_Path(this, nameof(Target.Gizmo.UseGizmo)._LowerFirst(), "방 도어 기즈모", "", nameof(Target.Gizmo)._LowerFirst());

    //            stringBuilder_WarningSummary.Clear();

    //            if (Target.TryGet_NeighborGapWarning())
    //            {
    //                stringBuilder_WarningSummary.AppendLine($"<color=red><b>에러 발생</b></color>");
    //                stringBuilder_WarningSummary.AppendLine($"<color=red><b>도어의 간격이 겹쳐있음</b></color>");
    //                SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder_WarningSummary.ToString(true));
    //                Debug.LogError($"{Target.name}의 도어가 겹쳐있음");
    //            }


    //            SU_CustomEditor.AutoLabelField_Head("요약", SU_CustomEditor.LabelHeadType.H2, () =>
    //            {
    //                stringBuilder_Summary.Clear();
    //                stringBuilder_Summary.AppendLine($"부모 {nameof(RoomObject)} 종속 여부: {Hangul_ValidParentRoom()}");

    //                stringBuilder_Summary.AppendLine($"활성화 여부: {Hangul_DoorActivity()}");
    //                stringBuilder_Summary.AppendLine($"개/폐 여부: {Hangul_DoorOpen()}");

    //                var canLow = Target.TryGet_NeighborGap(true, out var gapLow);
    //                var canHigh = Target.TryGet_NeighborGap(false, out var gapHigh);
    //                if (canLow)
    //                {
    //                    stringBuilder_Summary.Append($"낮은 이웃 도어: <color=#4fc1e9><b>{Target.Neighbor_Door_Low.name}</b></color> 간격: ");
    //                    if (gapLow >= 0)
    //                    {
    //                        stringBuilder_Summary.AppendLine($"<color=#2ecc71><b>{gapLow}</b></color>");
    //                    }
    //                    else
    //                    {
    //                        stringBuilder_Summary.AppendLine($"<color=red><b>{gapLow}</b></color>");
    //                    }
    //                }
    //                if (canHigh)
    //                {
    //                    stringBuilder_Summary.Append($"높은 이웃 도어: <color=#4fc1e9><b>{Target.Neighbor_Door_High.name}</b></color> 간격: ");
    //                    if (gapHigh >= 0)
    //                    {
    //                        stringBuilder_Summary.AppendLine($"<color=#2ecc71><b>{gapHigh}</b></color>");
    //                    }
    //                    else
    //                    {
    //                        stringBuilder_Summary.AppendLine($"<color=red><b>{gapHigh}</b></color>");
    //                    }
    //                }


    //                stringBuilder_Summary.AppendLine($"방향: {SU_String.WrapTargetSubstring(RoomObject_Editor.Hangul_DoorDir(Target.Direction), "<color=#4fc1e9><b>", "</b></color>")}\tIndex: <color=#ed5565><b><i>{Target.Index}</i></b></color>");
    //                stringBuilder_Summary.AppendLine($"크기: <color=#2ecc71><b>{Target.GetSize(false, false)}</b></color>");
    //                stringBuilder_Summary.AppendLine($"너비 크기: <color=#2ecc71><b>{Target.GetWidth(false, false)}</b></color> (<color=#4fc1e9><b>{Hangul_DoorWidthAxis()}</b></color>)");
    //                stringBuilder_Summary.AppendLine($"높이 크기: <color=#2ecc71><b>{Target.GetHeight(false, false)}</b></color> (<color=#4fc1e9><b>{Hangul_DoorHeightAxis()}</b></color>)");
    //                stringBuilder_Summary.AppendLine($"보정거리: <color=#2ecc71><b>{Target.EndDistance}</b></color>");
    //                stringBuilder_Summary.AppendLine($"Start 좌표: <color=#2ecc71><b>{Target.GetStartPosition}</b></color>");
    //                stringBuilder_Summary.AppendLine($"End 좌표: <color=#2ecc71><b>{Target.GetEndPosition(true)}</b></color>\n상대좌표(Start~): <color=#2ecc71><b>{Target.GetEndPosition_Relative(true)}</b></color>");


    //                var positionOrigin = -Target.ParentRoom.GetPositionOrigin_Swizzle();
    //                Target.CenterGridPositions_Start(positionOrigin, out bool isStartGridDoubleCenter, out var startCenter1, out var startCenter2);
    //                Target.CenterGridPositions_End(positionOrigin, out bool isEndGridDoubleCenter, out var endCenter1, out var endCenter2);



    //                stringBuilder_Summary.AppendLine($"Start 중심점 복수 중심점 여부: <color=#4fc1e9><b>{isStartGridDoubleCenter}</b></color>");
    //                if (!isStartGridDoubleCenter)
    //                {
    //                    stringBuilder_Summary.AppendLine($"   그리드 중심점: <color=#2ecc71><b>{startCenter1}</b></color>");
    //                }
    //                else
    //                {
    //                    stringBuilder_Summary.AppendLine($"   그리드 중심점1,2: <color=#2ecc71><b>{startCenter1}</b></color>, <color=#2ecc71><b>{startCenter2}</b></color>");
    //                }



    //                stringBuilder_Summary.AppendLine($"End 중심점 복수 중심점 여부: <color=#4fc1e9><b>{isEndGridDoubleCenter}</b></color>");
    //                if (!isEndGridDoubleCenter)
    //                {
    //                    stringBuilder_Summary.AppendLine($"   그리드 중심점: <color=#2ecc71><b>{endCenter1}</b></color>");
    //                }
    //                else
    //                {
    //                    stringBuilder_Summary.AppendLine($"   그리드 중심점1,2: <color=#2ecc71><b>{endCenter1}</b></color>, <color=#2ecc71><b>{endCenter2}</b></color>");
    //                }



    //                SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder_Summary.ToString(true));
    //            });



    //            SU_CustomEditor.AutoLabelField_Head("그리드 정보", SU_CustomEditor.LabelHeadType.H2, () =>
    //            {
    //                SU_CustomEditor.Render_ButtonWithStyle(this, "보유 도어 그리드 출력하기", "", (x) =>
    //                {
    //                    StringBuilder sb = new StringBuilder();
    //                    sb.AppendLine($"{x.name}가 보유중인 도어 그리드:");

    //                    foreach (var grid in Target.GetDoorGrids)
    //                    {
    //                        sb.AppendLine($"\t{grid.GridPosition}");
    //                    }
    //                    Debug.Log(sb.ToString(true));
    //                }, SU_ColorPresetRGB.Green_Emerald(), null);

    //                SU_CustomEditor.Render_ButtonWithStyle(this, "보유 보정 복도 그리드 출력하기", "", (x) =>
    //                {
    //                    StringBuilder sb = new StringBuilder();
    //                    sb.AppendLine($"{x.name}가 보유중인 보정 복도 그리드:");

    //                    foreach (var grid in Target.GetDoorGrids)
    //                    {
    //                        sb.AppendLine($"\t{grid.GridPosition}");
    //                    }
    //                    Debug.Log(sb.ToString(true));
    //                }, SU_ColorPresetRGB.Green_Emerald(), null);
    //            });



    //            SU_CustomEditor.AutoLabelField_Head("연결 ", SU_CustomEditor.LabelHeadType.H2, () =>
    //            {
    //                SU_CustomEditor.AutoLabelField_Head("연결 요약", SU_CustomEditor.LabelHeadType.H3, () =>
    //                {
    //                    stringBuilder_ConnectSummary1.Clear();

    //                    stringBuilder_ConnectSummary1.AppendLine($"이 도어와 복도의 연결 여부: {Hangul_ConnectHallway()}");
    //                    stringBuilder_ConnectSummary1.AppendLine($"이 도어와 연결되어있는 도어: {Hangul_ConnectDoor()}");
    //                }, false, true, false);

    //                SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder_ConnectSummary1.ToString(true));

    //                if (Target.ConnectingDoor != null)
    //                {
    //                    SU_CustomEditor.RenderField_Property(this, nameof(Target.ConnectingDoor)._LowerFirst(), "연결되어있는 도어");
    //                }

    //                if (Target.IsCreatedPathHallway)
    //                {
    //                    //stringBuilder_ConnectSummary2.Clear();

    //                    //stringBuilder_ConnectSummary2.AppendLine($"이 도어로부터 생성된 복도 그리드들");

    //                    //foreach (var hallwayGrid in TargetEdit.CurrentHallwayGrids)
    //                    //{
    //                    //    stringBuilder_ConnectSummary2.AppendLine($"\t<color=#2ecc71>{hallwayGrid.GridPosition}</color>");
    //                    //}

    //                    //SU_CustomEditor.LabelField_TextAutoWidthHeight(SU_String.StringBuilderToString(stringBuilder_ConnectSummary2, true));
    //                }
    //            });



    //            SU_CustomEditor.AutoLabelField_Head("제어", SU_CustomEditor.LabelHeadType.H2, () =>
    //            {
    //                SU_CustomEditor.Render_Button(this, "부모 RoomObject 찾아 할당하기", () => { Find_ParentRoomObject(true, true); });

    //                SU_CustomEditor.HorizontalGUI(() =>
    //                {
    //                    SU_CustomEditor.Render_ButtonWithStyle(this, "활성화", "", (x) => { x.EnableDoor(); }, SU_ColorPresetRGB.Green_Emerald(), Color.white);
    //                    SU_CustomEditor.Render_ButtonWithStyle(this, "비활성화", "", (x) => { x.DisableDoor(); }, SU_ColorPresetRGB.Red_GrapeFruit1(), Color.white);
    //                });

    //                SU_CustomEditor.HorizontalGUI(() =>
    //                {
    //                    SU_CustomEditor.Render_ButtonWithStyle(this, "열기", "", (x) => { x.OpenDoor(); }, SU_ColorPresetRGB.Green_Emerald(), Color.white);
    //                    SU_CustomEditor.Render_ButtonWithStyle(this, "닫기", "", (x) => { x.CloseDoor(); }, SU_ColorPresetRGB.Red_GrapeFruit1(), Color.white);
    //                });

    //            });



    //            SU_CustomEditor.AutoLabelField_Head("도어 정보", SU_CustomEditor.LabelHeadType.H2, () =>
    //            {
    //                SU_CustomEditor.RenderField_Property(this, nameof(Target.ParentRoom)._LowerFirst(), "부모 RoomObject");
    //                SU_CustomEditor.CheckChangeAction(this, () =>
    //                {
    //                    SU_CustomEditor.RenderField_Property(this, nameof(Target.Direction)._LowerFirst(), "도어 방향");
    //                }, (x) =>
    //                {
    //                    x.Refresh_DoorSize();
    //                    x.Refresh_DoorWidthHeight();
    //                });

    //                SU_CustomEditor.CheckChangeAction(this, () =>
    //                {
    //                    SU_CustomEditor.RenderField_Property(this, nameof(Target.Size)._LowerFirst(), "도어 크기");
    //                }, x => x.Refresh_DoorSize());

    //                SU_CustomEditor.CheckChangeAction(this, () =>
    //                {
    //                    SU_CustomEditor.RenderField_Property(this, nameof(Target.Width)._LowerFirst(), "너비 (가변)");
    //                    SU_CustomEditor.RenderField_Property(this, nameof(Target.Height)._LowerFirst(), "높이 (가변)");
    //                }, x => x.Refresh_DoorWidthHeight());

    //                SU_CustomEditor.CheckChangeAction(this, () =>
    //                {
    //                    SU_CustomEditor.RenderField_Property(this, nameof(Target.EndDistance)._LowerFirst(), "도어 보정거리");
    //                }, x => x.Refresh_EndDistance());
    //            });



    //            SU_CustomEditor.AutoLabelField_Head("도어 이벤트", SU_CustomEditor.LabelHeadType.H2, () =>
    //            {
    //                SU_CustomEditor.RenderField_Property(this, nameof(TargetEdit.EnableDoorEvent), "도어의 활성화 이벤트");
    //                SU_CustomEditor.RenderField_Property(this, nameof(TargetEdit.DisableDoorEvent), "도어의 비활성화 이벤트");

    //                SU_CustomEditor.RenderField_Property(this, nameof(TargetEdit.OpenDoorEvent), "도어의 열기 이벤트");
    //                SU_CustomEditor.RenderField_Property(this, nameof(TargetEdit.CloseDoorEvent), "도어의 닫기 이벤트");
    //            }, false);
    //        });




    //        //BaseGizmoScript.Editor_DrawGizmoToggleButton<RoomDoor_Gizmo>(this);
    //    }



    //    ///======================================================================================================================================================
    //}

    //! 일단 전부 비활성화
}