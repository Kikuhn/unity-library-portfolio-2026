using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Pan.GridCompatibles2;

using Pan.StageGenerators;
using Pan.StageGenerators.Editor;
using Pan.Util;
using Pan.Util.Editors;



namespace Pan.StageGenerators.Editor
{
    //[InitializeOnLoad]
    [CanEditMultipleObjects]
    [CustomEditor(typeof(StageGeneratorSettingGizmoSbject), true)]
    public class StageGeneratorGizmoSettingSbject_Editor : EditorExpand_InspectorGUI<StageGeneratorSettingGizmoSbject>, IDrawInspectorGUI_Current
    {
        ///======================================================================================================================================================



        private StageGeneratorSettingGizmo Setting => Target.Setting;
        private readonly string[] SettingFieldPath = new string[] { nameof(Setting)._LowerFirst() };



        ///======================================================================================================================================================



        protected override void OnInspectorGUI_Current()
        {
            DrawInspectorGUI_Current();
        }



        ///======================================================================================================================================================



        public void DrawInspectorGUI_Current()
        {
            SU_CustomEditor.AutoLabelField_Head("통합 기즈모 설정", SU_CustomEditor.LabelHeadType.H1, () =>
            {
                SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.UseGizmo), "통합 기즈모", "", SettingFieldPath);
            });



            SU_CustomEditor.ActiveGUICondition(Setting.UseGizmo, () =>
            {
                SU_CustomEditor.AutoLabelField_Head("공간", SU_CustomEditor.LabelHeadType.H2, () =>
                {
                    SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.UseGizmo_Spaces), "공간 기즈모", "", SettingFieldPath).boolValue, () =>
                    {
                        SU_CustomEditor.Format_IndentLevel_Plus();


                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.UseGizmo_SpaceRect), "공간 Rect", "", SettingFieldPath).boolValue, () =>
                            {
                                SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.SpaceRectColor), "", "", SettingFieldPath);
                            });
                        });


                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.UseGizmo_SpaceNode), "공간 노드", "", SettingFieldPath).boolValue, () =>
                            {
                                SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.SpaceNodeColor), "", "", SettingFieldPath);
                            });
                        });

                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.UseGizmo_SpaceInRoomNode), "공간 방 노드", "", SettingFieldPath).boolValue, () =>
                            {
                                SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.SpaceInRoomNodeColor), "", "", SettingFieldPath);
                            });
                        });


                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.UseGizmo_SpaceInfoText), "공간 정보 텍스트", "", SettingFieldPath).boolValue, () =>
                            {
                                SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.SpaceInfoTextColor), "", "", SettingFieldPath);
                            });
                        });

                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.UseGizmo_SpaceRoomRect), "방 Rect", "", SettingFieldPath).boolValue, () =>
                            {
                                SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.SpaceRoomRectColor), "", "", SettingFieldPath);
                            });
                        });


                        SU_CustomEditor.Format_IndentLevel_Minus();
                    });
                });


                SU_CustomEditor.AutoLabelField_Head("그리드", SU_CustomEditor.LabelHeadType.H2, () =>
                {
                    SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.UseGizmo_Grids), "그리드 기즈모", "", SettingFieldPath).boolValue, () =>
                    {
                        SU_CustomEditor.Format_IndentLevel_Plus();

                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.UseGizmo_AllGrid), "전체 그리드", "", SettingFieldPath).boolValue, () =>
                            {
                                SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.AllGridColor), "", "", SettingFieldPath);
                            });
                        });



                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.UseGizmo_OccupiedGrid), "점유 그리드", "", SettingFieldPath).boolValue, () =>
                            {
                                SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.OccupiedGridColor), "", "", SettingFieldPath);
                            });
                        });



                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.UseGizmo_PathCostGrid), "경로 비용 그리드", "", SettingFieldPath).boolValue, () =>
                            {
                                SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.PathCostGridColor), "", "", SettingFieldPath);
                            });
                        });

                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.UseGizmo_WasBannedMainPath), "패스파인딩 금지 메인경로 그리드", "", SettingFieldPath).boolValue, () =>
                            {
                                SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.WasBannedMainPathColor), "", "", SettingFieldPath);
                            });
                        });

                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.UseGizmo_WasBanned_CollisionOtherHallways), "복도 충돌 금지 그리드", "", SettingFieldPath).boolValue, () =>
                            {
                                SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.WasBannedCollisionOtherHallwaysColor), "", "", SettingFieldPath);
                            });
                        });

                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.UseGizmo_RoomGrid), "방 그리드", "", SettingFieldPath).boolValue, () =>
                            {
                                SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.RoomGridColor), "", "", SettingFieldPath);
                            });
                        });



                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.UseGizmo_RoomSafeAreaGrid), "방 안전구역 그리드", "", SettingFieldPath).boolValue, () =>
                            {
                                SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.RoomSafeAreaGridColor), "", "", SettingFieldPath);
                            });
                        });


                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.UseGizmo_RoomExpandSafeAreaGrid), "방 확장 안전구역 그리드", "", SettingFieldPath).boolValue, () =>
                            {
                                SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.RoomExpandSafeAreaGridColor), "", "", SettingFieldPath);
                            });
                        });



                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.UseGizmo_HallwayGrid), "복도 그리드", "", SettingFieldPath).boolValue, () =>
                            {
                                SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.HallwayGridColor), "", "", SettingFieldPath);
                            });
                        });



                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.UseGizmo_HallwayEdgeGrid), "복도 테두리 그리드", "", SettingFieldPath).boolValue, () =>
                            {
                                SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.HallwayEdgeGridColor), "", "", SettingFieldPath);
                            });
                        });



                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.UseGizmo_HallwaySafeAreaGrid), "복도 테두리 안전구역", "", SettingFieldPath).boolValue, () =>
                            {
                                SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.HallwaySafeAreaGridColor), "", "", SettingFieldPath);
                            });
                        });


                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.UseGizmo_RoomDoorGrid), "방 도어 그리드", "", SettingFieldPath).boolValue, () =>
                            {
                                SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.RoomDoorGridColor), "", "", SettingFieldPath);
                            });
                        });



                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.UseGizmo_DrawNearGridInfoFromMouse), "마우스 주위 그리드 정보 표시", "", SettingFieldPath).boolValue, () =>
                            {
                                SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.DrawNearGridInfoFromMouseRadius), "", "", SettingFieldPath);
                                SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.DrawNearGridInfoFromMouseTextColor), "", "", SettingFieldPath);
                            });
                        });

                        SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.UseGizmo_MouseDrawGUI_GridInfo), "마우스의 그리드 정보 보기", "", SettingFieldPath);

                        EditorGUILayout.Space();


                        SU_CustomEditor.Format_IndentLevel_Minus();
                    });
                });

                SU_CustomEditor.AutoLabelField_Head("기타", SU_CustomEditor.LabelHeadType.H2, () =>
                {

                    SU_CustomEditor.HorizontalGUI(() =>
                    {
                        SU_CustomEditor.ActiveGUICondition(SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.UseGizmo_HallwayNavMesh), "복도 NavMesh", "", SettingFieldPath).boolValue, () =>
                        {
                            SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.HallwayNavMeshColor), "", "", SettingFieldPath);
                        });
                    });


                });

                SU_CustomEditor.AutoLabelField_Head("임시", SU_CustomEditor.LabelHeadType.H2, () =>
                {
                    SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.CustomTempDrawGridGizmoColor), "임시 그리드 기즈모", "", SettingFieldPath);

                    SU_CustomEditor.Format_IndentLevel_Plus();
                    SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.DrawTempCacheRoomRects_RoomRectColor), "방 Rect", "", SettingFieldPath);
                    SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.DrawTempCacheRoomRects_RoomSafeAreaRectColor), "방 안전구역 Rect", "", SettingFieldPath);
                    SU_CustomEditor.RenderField_Property_Path(this, nameof(Setting.DrawTempCacheRoomRects_RoomSafeAreaExpandRectColor), "방 안전구역 확장 Rect", "", SettingFieldPath);
                    SU_CustomEditor.Format_IndentLevel_Minus();
                });


                SU_CustomEditor.Render_Button(this, "모든 색 옵션을 보색으로 뒤집기 (텍스트 제외!)", (t) =>
                {
                    t.Setting.ConvertAllColor_Complementary();
                });
            });
        }



        ///======================================================================================================================================================
    }
}



///======================================================================================================================================================