using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Pan.GridCompatibles2;

using Pan.StageGenerators;
using Pan.StageGenerators.Editor;
using Pan.Util.Editors;



namespace Pan.StageGenerators.Editor
{
    [CustomEditor(typeof(RoomGizmoSettingSbject), true)]
    public class RoomGizmoSettingSbject_Editor : EditorExpand_InspectorGUI<RoomGizmoSettingSbject>
    {
        RoomGizmoSettingSbject.IEdit TargetEdit => Target;
        RoomGizmoSetting Setting => Target.GetRoomGizmoSetting;
        private bool UseGizmo => Setting.UseGizmo;



        private static readonly string[] CurrentRoomGizmoSettingPath = new string[] { nameof(RoomGizmoSettingSbject.IEdit.RoomGizmoSetting) };


        protected override void OnInspectorGUI_Current()
        {
            SU_CustomEditor.AutoLabelField_Head("기즈모 설정", SU_CustomEditor.LabelHeadType.H1, () =>
            {
                SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.UseGizmo), "기즈모 사용", "", CurrentRoomGizmoSettingPath);

                SU_CustomEditor.ActiveGUICondition(UseGizmo, () =>
                {
                    SU_CustomEditor.HorizontalGUI(() =>
                    {
                        SU_CustomEditor.LabelField_TextAutoWidthHeight("Prefab에서만 텍스트 출력");
                        SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.UseGizmo_UseTextOnlyPrefab), "", "", CurrentRoomGizmoSettingPath);
                    });

                    SU_CustomEditor.HorizontalGUI(() =>
                    {
                        SU_CustomEditor.LabelField_TextAutoWidthHeight("오브젝트를 선택할때만 출력");
                        SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.UseGizmo_OnlySelected), "", "", CurrentRoomGizmoSettingPath);
                    });
                });
            });



            SU_CustomEditor.ActiveGUICondition(UseGizmo, () =>
            {
                SU_CustomEditor.AutoLabelField_Head("그리드", SU_CustomEditor.LabelHeadType.H1, () =>
                {
                    SU_CustomEditor.AutoLabelField_Head("방 그리드", SU_CustomEditor.LabelHeadType.H2, () =>
                    {
                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.UseGizmo_UseGrid), "방 그리드", "", CurrentRoomGizmoSettingPath).boolValue, () =>
                            {
                                SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.Grid_GridColor), "", "", CurrentRoomGizmoSettingPath);
                            });
                        });

                        SU_CustomEditor.ActiveGUICondition(Setting.UseGizmo_UseGrid, () =>
                        {
                            SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.Grid_GridColor_DownLeft), "좌측하단 강조색", "", CurrentRoomGizmoSettingPath);
                            SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.Grid_GridColor_UpRight), "우측상단 강조색", "", CurrentRoomGizmoSettingPath);
                        });
                    });

                    SU_CustomEditor.AutoLabelField_Head("방 그리드 그리드 좌표 텍스트", SU_CustomEditor.LabelHeadType.H2, () =>
                    {
                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.UseGizmo_UseGrid_GridPosiiton), "방 Grid 그리드 좌표", "", CurrentRoomGizmoSettingPath).boolValue, () =>
                            {
                                SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.Grid_GridPositionTextColor), "", "", CurrentRoomGizmoSettingPath);
                            });
                        });

                        SU_CustomEditor.Format_IndentLevel_Plus();
                        SU_CustomEditor.ActiveGUICondition(Setting.UseGizmo_UseGrid_GridPosiiton, () =>
                        {
                            SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.Grid_GridPositionTextPosition), "상대좌표", "", CurrentRoomGizmoSettingPath);
                        });
                        SU_CustomEditor.Format_IndentLevel_Minus();
                    });

                    SU_CustomEditor.AutoLabelField_Head("방 그리드 월드 좌표 텍스트", SU_CustomEditor.LabelHeadType.H2, () =>
                    {
                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.UseGizmo_UseGrid_Posiiton), "방 Grid 월드 좌표", "", CurrentRoomGizmoSettingPath).boolValue, () =>
                            {
                                SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.Grid_PositionTextColor), "", "", CurrentRoomGizmoSettingPath);
                            });
                        });

                        SU_CustomEditor.Format_IndentLevel_Plus();
                        SU_CustomEditor.ActiveGUICondition(Setting.UseGizmo_UseGrid_Posiiton, () =>
                        {
                            SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.Grid_PositionTextPosition), "상대좌표", "", CurrentRoomGizmoSettingPath);
                        });
                        SU_CustomEditor.Format_IndentLevel_Minus();
                    });

                    SU_CustomEditor.AutoLabelField_Head("ETC", SU_CustomEditor.LabelHeadType.H2, () =>
                    {
                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.UseTextMouseRadius), "텍스트 출력 마우스", "", CurrentRoomGizmoSettingPath).boolValue, () =>
                            {
                                SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.TextMouseRadius), "", "", CurrentRoomGizmoSettingPath);
                            });
                        });
                        SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.UseSwizzleAxisGizmo), "기즈모 Axis", "", CurrentRoomGizmoSettingPath);

                    }, false);
                });



                SU_CustomEditor.AutoLabelField_Head("Rect", SU_CustomEditor.LabelHeadType.H1, () =>
                {
                    SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.UseGizmo_UseRoomRect), "방 Rect", "", CurrentRoomGizmoSettingPath).boolValue, () =>
                    {
                        SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.RoomRect_RectColor), "방 Rect 색", "", CurrentRoomGizmoSettingPath);
                        SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.RoomRect_InfoTextColor), "방 정보 텍스트 색", "", CurrentRoomGizmoSettingPath);

                        SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.UseGizmo_UseRoomRect_SafeArea), "방 안전구역 Rect", "", CurrentRoomGizmoSettingPath).boolValue, () =>
                        {
                            SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.RoomRect_RectSafeAreaColor), "방 안전구역 Rect 색", "", CurrentRoomGizmoSettingPath);
                            SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.RoomRect_RectSafeAreaTextColor), "방 안전구역 정보 텍스트 색", "", CurrentRoomGizmoSettingPath);
                        });

                        SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.UseGizmo_UseRoomRect_SafeAreaCurrent), "방 현재 안전구역 Rect", "", CurrentRoomGizmoSettingPath).boolValue, () =>
                        {
                            SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.RoomRect_RectSafeAreaCurrentColor), "방 현재 안전구역 Rect 색", "", CurrentRoomGizmoSettingPath);
                            SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.RoomRect_RectSafeAreaCurrentTextColor), "방 현재 안전구역 정보 텍스트 색", "", CurrentRoomGizmoSettingPath);
                        });


                        SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.UseGizmo_UseRoomCenter), "방 중심", "", CurrentRoomGizmoSettingPath).boolValue, () =>
                        {
                            SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.RoomCenterColor), "방 중심 색", "", CurrentRoomGizmoSettingPath);
                            SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.RoomCenterTextColor), "방 중심 정보 텍스트 색", "", CurrentRoomGizmoSettingPath);
                        });

                    });
                });



                SU_CustomEditor.AutoLabelField_Head("문", SU_CustomEditor.LabelHeadType.H1, () =>
                {
                    SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.UseGizmo_Door), "문 Gizmo", "", CurrentRoomGizmoSettingPath).boolValue, () =>
                    {
                        SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.Door_EnableColor), "활성화된 문", "", CurrentRoomGizmoSettingPath);
                        SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.Door_DisableColor), "비활성화된 문", "", CurrentRoomGizmoSettingPath);
                        SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.Door_NodeColor), "문 노드", "", CurrentRoomGizmoSettingPath);
                        SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.DoorGizmoTextPlusPositionCorrection), "문 텍스트 위치 보정값", "", CurrentRoomGizmoSettingPath);
                    });
                });


                SU_CustomEditor.AutoLabelField_Head("그리드", SU_CustomEditor.LabelHeadType.H1, () =>
                {
                    SU_CustomEditor.HorizontalGUI(() =>
                    {
                        SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.UseGizmo_DoorGrid), "도어가 보유중인 도어 그리드", "", CurrentRoomGizmoSettingPath).boolValue, () =>
                        {
                            SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.DoorGridColor), "", "", CurrentRoomGizmoSettingPath);
                        });
                    });
                    SU_CustomEditor.HorizontalGUI(() =>
                    {
                        SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.UseGizmo_CorrectionHallwayGrid), "도어가 보유중인 보정 복도 그리드", "", CurrentRoomGizmoSettingPath).boolValue, () =>
                        {
                            SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.CorrectionHallwayGridColor), "", "", CurrentRoomGizmoSettingPath);
                        });
                    });
                    SU_CustomEditor.HorizontalGUI(() =>
                    {
                        SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.UseGizmo_PathHallwayGrid), "도어가 보유중인 경로 복도 그리드", "", CurrentRoomGizmoSettingPath).boolValue, () =>
                        {
                            SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.PathHallwayGridColor), "", "", CurrentRoomGizmoSettingPath);
                        });
                    });
                });


                SU_CustomEditor.RenderField_Property_Path(this, nameof(TargetEdit.RoomGizmoSetting.Gizmo_CorrectionLength), "기즈모 크기 보정치", "", CurrentRoomGizmoSettingPath);
            });
        }
    }
}