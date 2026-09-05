#if UNITY_EDITOR
using Pan.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using UnityEditor;
using Pan.GridCompatibles2;
using Pan.StageGenerators;

using System;
using Pan.Util.Editors;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Linq;
using UnityEngine.Pool;
using Sirenix.OdinInspector;
using System.Threading.Tasks;



namespace Pan.StageGenerators
{

    public partial class StageGenerator
    {
        private readonly StageGenerator_Gizmo gizmo = new();



        [BoxGroup("기즈모박스그룹", false)]
        [LabelText("StageGenerator 기즈모 사용", Icon = SdfIconType.BorderAll)]
        [ShowInInspector]
        [PropertyOrder(9999)]
        private bool UseGizmo_StageGenerator { get => gizmo.useGizmo; set => gizmo.useGizmo = value; }



        protected virtual void OnDrawGizmos()
        {
            gizmo.OnDrawGizmos(this);
        }



        private class StageGenerator_Gizmo : BaseGizmoClass<StageGenerator>
        {
            ///======================================================================================================================================================



            private StageGeneratorSingletonSettingSbject singleTonSetting => StageGeneratorSingletonSettingSbject.O;



            private static GUIStyle _guiStyle_Label;
            private static GUIStyle guiStyle_Label
            {
                get
                {
                    if (_guiStyle_Label == null)
                    {
                        _guiStyle_Label = new GUIStyle
                        {
                            alignment = TextAnchor.MiddleCenter,
                            fontSize = 12,
                            normal = new GUIStyleState { textColor = Color.white },
                            richText = true
                        };
                    }

                    return _guiStyle_Label;
                }
            }



            private bool isStageGeneratorObjectSelected;
            Vector3 FloorStandardPositionCurrent;




            protected override void DrawGizmo(StageGenerator main)
            {
                if (!main.IsValid_Setting) { return; }
                isStageGeneratorObjectSelected = SU_EditorControl.IsObjectSelected(main.transform, true, true);


                FloorStandardPositionCurrent = main.Setting.SnapSetting.GetFloorStandardPositionCurrent;
                var snapSetting = main.Setting.SnapSetting;
                var swizzle = main.Setting.SnapSetting.Swizzle;


                DrawGizmo_FloorToCeilingArrow(main, snapSetting, swizzle);
                DrawGizmo_Stage(main, snapSetting, swizzle);
                DrawGizmo_Space(main, snapSetting, swizzle);
                DrawGizmo_Grid(main, snapSetting, swizzle);
            }



            //? "바닥 기준 좌표"와 "트랜스폼 오브젝트의 깊이Z축"의 떨어진 정도를 표시하는 화살표를 그린다
            private void DrawGizmo_FloorToCeilingArrow(StageGenerator main, GridCompatible2SnapSetting snapSetting, GridLayout.CellSwizzle swizzle)
            {
                if (!GridCompatible2SingletonSettingSbject.O.FloorToCeilingArrowColor.TryGetColor(out var floorToCilingArrowColor)) { return; }
                Gizmos.color = floorToCilingArrowColor;
                SU_Gizmo.DrawArrow(main.TransformM.StageParentPositionOriginal, main.TransformM.StageParentPositionOriginal + FloorStandardPositionCurrent);
            }



            //? 스테이지 그리기
            private void DrawGizmo_Stage(StageGenerator main, GridCompatible2SnapSetting snapSetting, GridLayout.CellSwizzle swizzle)
            {
                if (!singleTonSetting.UseStageGeneratorGizmo || (singleTonSetting.UseStageGeneratorGizmoOnlySelect && !isStageGeneratorObjectSelected)) { return; }

                if (singleTonSetting.StageColor.TryGetColor(out var stageColor))
                {
                    Gizmos.color = stageColor;
                    main.TransformM.GetStageGeneratorTransformCenterAndSize(out var center, out var size);

                    center += FloorStandardPositionCurrent;

                    //. 스테이지 테두리 그리기
                    Gizmos.DrawWireCube(center, size);


                    //. 스테이지 테두리를 한번 더 두르는 점선 그리기
                    SU_Gizmo.TempGizmoColor_Action(Gizmos.color.WithMultipliedAlpha(0.1f), () =>
                    {
                        SU_Gizmo.DrawDottedWireCube(center, ((Vector3)size).Multiply(1.01f), size.GetMaxComponent() * 0.025f, size.GetMaxComponent() * 0.0125f);
                    });

                    var instnacePoint = main.TransformM.StageInstancePointTransform;
                    var instnacePointCircleRadius = snapSetting.GridUnitOriginalVector2.GetMaxComponent() * 0.5f;

                    //. 생성 지점 표시
                    SU_Gizmo.DrawCircle(instnacePoint, instnacePointCircleRadius, swizzle);
                    SU_Gizmo.DrawCircle(instnacePoint, instnacePointCircleRadius * 0.75f, swizzle);


                    //. 스테이지 생성기를 선택중일때
                    if (isStageGeneratorObjectSelected)
                    {
                        Vector3 lowerCenter = center + new Vector3(0, -main.Setting.StageVector.StageHeightTransform * 0.5f - ((main.Setting.SnapSetting.GridUnitY_Height / 2f)), 0).SwizzlesVector(swizzle);
                        Handles.Label(lowerCenter, $"StageGenerator\n<color=#2ecc71>{main.Setting.StageVector.StageWidth}x{main.Setting.StageVector.StageHeight}</color>", guiStyle_Label);
                        Handles.Label(instnacePoint, $"<color=white>Instance Point</color>", guiStyle_Label);

                        Gizmos.color = stageColor.WithMultipliedAlpha(0.01f);
                        Gizmos.DrawCube(center, size);
                    }
                }
            }



            //? 공간 그리기
            private void DrawGizmo_Space(StageGenerator main, GridCompatible2SnapSetting snapSetting, GridLayout.CellSwizzle swizzle)
            {
                if (!singleTonSetting.UseSpaceGizmo || (singleTonSetting.UseSpaceGizmoOnlySelect && !isStageGeneratorObjectSelected)) { return; }


                bool isDrawSpaceColor = singleTonSetting.SpaceColor.TryGetColor(out var spaceColor);
                bool isDrawSpaceNodeColor = singleTonSetting.SpaceNodeColor.TryGetColor(out var spaceNodeColor);


                if ((isDrawSpaceColor || isDrawSpaceNodeColor) && main.spaceM.SpacesBinaryTree != null)
                {
                    var spaceList = main.spaceM.SpacesBinaryTree;
                    string spaceColorHex = spaceColor.ToHex();
                    float spaceSizeInsideLength = snapSetting.GridUnitOriginalVector2.GetMaxComponent() * singleTonSetting.SpaceInsideLength;


                    //! 노드그릴때 연결된거 중복그리기 방지용
                    HashSet<Space> antiOverlapSpaceList = HashSetPool<Space>.Get();
                    antiOverlapSpaceList.EnsureCapacity(spaceList.Count);


                    for (int i = 0; i < spaceList.Count; i++)
                    {
                        //. 공간
                        var space = spaceList[i];


                        //. 공간의 Rect (StageGenerator)
                        var spaceSGRect = space.SpaceOriginTransformRect;

                        //. 공간의 크기 (StageGenerator)
                        var spaceSGCenter = spaceSGRect.center.SwizzlesVector2To3(swizzle) + FloorStandardPositionCurrent;
                        var spaceSGSize = spaceSGRect.size.SwizzlesVector2To3(swizzle);

                        //. 기즈모용 공간 내부 크기
                        //r spaceSGSize_Inside = spaceSGSize * spaceSizeInsideLength;
                        var spaceSGSizeV2_Inside = spaceSGRect.size.OffsetUniform(spaceSizeInsideLength);
                        var spaceSGSizeV3_Inside = spaceSGSizeV2_Inside.SwizzlesVector2To3(swizzle);
                        var spaceSGSizeV2_InsideHalf = spaceSGRect.size.OffsetUniform(spaceSizeInsideLength) * 0.5f;

                        //. 공간 위에 마우스가 있는지
                        var isMouseHoveringInSpace = SU_Editor_Input.IsWithinEditorMouseBounds(spaceSGCenter, spaceSGRect.size, swizzle);


                        //? 공간 그리기
                        if (isDrawSpaceColor)
                        {
                            Gizmos.color = spaceColor;

                            //? 공간 박스 기즈모 그리기
                            Gizmos.DrawWireCube(spaceSGCenter, spaceSGSize);


                            ////? 공간 내부 박스 기즈모 그리기 (연하게)
                            //SU_Gizmo.TempGizmoColor_Action(Gizmos.color.WithMultipliedAlpha(0.2f), () =>
                            //{
                            //    Gizmos.DrawWireCube(spaceSGCenter, spaceSGSizeV3_Inside);
                            //});


                            //? 공간 정 중앙에 중심 십자 그리기
                            SU_Gizmo.TempGizmoColor_Action(Gizmos.color.WithMultipliedAlpha(0.5f), () =>
                            {
                                SU_Gizmo.DrawCross(spaceSGCenter, snapSetting.GridUnitFlatCurrent);
                            });


                            //? 공간 위 마우스 강조
                            if (isMouseHoveringInSpace)
                            {
                                SU_Gizmo.TempGizmoColor_Action(spaceColor.WithMultipliedAlpha(0.1f), () =>
                                {
                                    Gizmos.DrawCube(spaceSGCenter + new Vector3(0, 0, 0.05f).SwizzlesVector(swizzle), spaceSGSize);
                                });

                                //. 연결공간 강조
                                SU_Gizmo.TempGizmoColor_Action(spaceColor.WithMultipliedAlpha(0.05f), () =>
                                {
                                    var connectingSpaces = space.TotalConnectingSpaces;

                                    for (int i = 0; i < connectingSpaces.Count; i++)
                                    {
                                        //. 연결된 공간
                                        var connectSpace = connectingSpaces[i];

                                        //. 연결된 공간의 Rect
                                        var connectSpaceSGRect = connectSpace.SpaceOriginTransformRect;

                                        //. 연결된 공간의 크기 (StageGenerator)
                                        var connectSpaceSGCenter = (connectSpaceSGRect.center + new Vector2(0, 0.05f)).SwizzlesVector2To3(swizzle);
                                        var connectSpaceSGSize = connectSpaceSGRect.size.SwizzlesVector2To3(swizzle);

                                        Gizmos.DrawCube(connectSpaceSGCenter + FloorStandardPositionCurrent, connectSpaceSGSize);
                                    }
                                });

                            }


                            //? 공간 정보 그리기
                            if (singleTonSetting.UseSpaceInfoext)
                            {
                                Handles.Label(spaceSGCenter + new Vector2(0, snapSetting.GridUnitY_Height).SwizzlesVector2To3(swizzle), $"<color={spaceColorHex}>{space.SpaceIndex}</color>", guiStyle_Label);
                            }
                        }


                        //? 공간 노드 그리기
                        if (isDrawSpaceNodeColor)
                        {
                            if (!antiOverlapSpaceList.Contains(space))
                            {
                                Gizmos.color = spaceNodeColor;

                                //. 4방향별 기즈모 그리기
                                if (space.HasNode_Down) { drawNodeGizmo(EDirection4.Down, space.ConnectingSpaces_Down); }
                                if (space.HasNode_Up) { drawNodeGizmo(EDirection4.Up, space.ConnectingSpaces_Up); }
                                if (space.HasNode_Left) { drawNodeGizmo(EDirection4.Left, space.ConnectingSpaces_Left); }
                                if (space.HasNode_Right) { drawNodeGizmo(EDirection4.Right, space.ConnectingSpaces_Right); }

                                void drawNodeGizmo(EDirection4 direciton, List<Space> connectSpaceList)
                                {

                                    for (int j = 0; j < connectSpaceList.Count; j++)
                                    {
                                        //! 노드를 거미줄처럼 그리기 (간단)
                                        if (singleTonSetting.UseSpaceNodeWebStyle)
                                        {
                                            Gizmos.DrawLine(spaceSGCenter, connectSpaceList[j].SpaceOriginTransformRect.center.SwizzlesVector2To3(swizzle) + FloorStandardPositionCurrent);
                                            continue;
                                        }


                                        if (!isMouseHoveringInSpace && antiOverlapSpaceList.Contains(connectSpaceList[j])) { continue; }


                                        //. 연결 공간
                                        var connectSpace = connectSpaceList[j];

                                        //. 연결 공간의 Rect (StageGenerator)
                                        var connectSSGRect = connectSpace.SpaceOriginTransformRect;

                                        //. 공간의 크기 (StageGenerator)
                                        var connectSpaceSGCenter = connectSSGRect.center.SwizzlesVector2To3(swizzle);
                                        var connectSpaceSGSize = connectSSGRect.size.SwizzlesVector2To3(swizzle);

                                        //. 기즈모용 공간 내부 크기
                                        var connectSpaceSGSizeV2_Inside = connectSSGRect.size.OffsetUniform(spaceSizeInsideLength);
                                        var connectSpaceSGSizeV2Half_Inside = connectSpaceSGSizeV2_Inside * 0.5f;

                                        Vector3 nodeStartPoint = spaceSGRect.center;
                                        switch (direciton)
                                        {
                                            case EDirection4.Down: nodeStartPoint.y -= spaceSGSizeV2_InsideHalf.y; break;
                                            case EDirection4.Up: nodeStartPoint.y += spaceSGSizeV2_InsideHalf.y; break;
                                            case EDirection4.Left: nodeStartPoint.x -= spaceSGSizeV2_InsideHalf.x; break;
                                            case EDirection4.Right: nodeStartPoint.x += spaceSGSizeV2_InsideHalf.x; break;
                                        }

                                        Vector3 nodeEndPoint = connectSSGRect.center;
                                        switch (direciton)
                                        {
                                            case EDirection4.Down: nodeEndPoint.y += connectSpaceSGSizeV2Half_Inside.y; break;
                                            case EDirection4.Up: nodeEndPoint.y -= connectSpaceSGSizeV2Half_Inside.y; break;
                                            case EDirection4.Left: nodeEndPoint.x += connectSpaceSGSizeV2Half_Inside.x; break;
                                            case EDirection4.Right: nodeEndPoint.x -= connectSpaceSGSizeV2Half_Inside.x; break;
                                        }


                                        nodeStartPoint = nodeStartPoint.SwizzlesVector(swizzle) + FloorStandardPositionCurrent;
                                        nodeEndPoint = nodeEndPoint.SwizzlesVector(swizzle) + FloorStandardPositionCurrent;

                                        //
                                        if (isMouseHoveringInSpace)
                                        {
                                            var sqr = (nodeEndPoint - nodeStartPoint).sqrMagnitude;
                                            var arrowEndPointSize = Mathf.Clamp(sqr, 0, (snapSetting.GridUnitOriginalVector2.GetMaxComponent() * 0.5f));
                                            SU_Gizmo.DrawArrow(nodeStartPoint, nodeEndPoint, 30, arrowEndPointSize);
                                        }
                                        else
                                        {
                                            Gizmos.DrawLine(nodeStartPoint, nodeEndPoint);
                                        }
                                    }
                                }

                                antiOverlapSpaceList.Add(space);
                            }
                        }
                    }
                }
            }



            //? 그리드 그리기
            private void DrawGizmo_Grid(StageGenerator main, GridCompatible2SnapSetting snapSetting, GridLayout.CellSwizzle swizzle)
            {
                if (!singleTonSetting.UseGridGizmo || (singleTonSetting.UseGridGizmoOnlySelect && !isStageGeneratorObjectSelected)) { return; }


                //. 그리드 셀 모음
                var gridArray = main.gridM.GetGrids;
                if (gridArray == null || gridArray.Length == 0) { return; }


                //. 중심 좌표 목록 생성 (LINQ‧메모리 할당 최소화)
                //? 필요 시 Span·Temp 리스트 풀로 교체 가능
                List<Vector3> centers = ListPool<Vector3>.Get();
                centers.Capacity = gridArray.Length;


                for (int i = 0; i < gridArray.Length; ++i)
                {
                    centers.Add(gridArray[i].WorldPosition_Cached + FloorStandardPositionCurrent);
                }


                //. 그리드 격자 그리기
                if (singleTonSetting.GridColor.TryGetColor(out var gridColor))
                {
                    Gizmos.color = gridColor;

                    //. ▶ 최적화된 격자 기즈모 그리기
                    SU_Gizmo.DrawGridByCells2D
                    (
                        centers,
                        snapSetting.GridUnitOriginalVector2,
                        main.Setting.SnapSetting.FloorStandardPositionLength,
                        swizzle,
                        drawEndLine: true
                    );
                }


                float centerOneRadius = snapSetting.GridUnitAverageCorrection * 0.5f;
                var gridFlatSize = snapSetting.GridUnitFlatCurrent + new Vector3(0, 0, snapSetting.GridUnitOriginalVector2.GetMinComponent() * 0.01f).SwizzlesVector(swizzle);
                var gridRadiusSize = snapSetting.GridUnitAverageCorrection * 0.5f;


                for (int i = 0; i < gridArray.Length; i++)
                {
                    Grid grid = gridArray[i];

                    //. 현재 선택중인 그리드 위에, 현재 기즈모의 반전 색을 채운 큐브를 그려, 현재 마우스를 올리고있는 그리드를 강조한다
                    if (singleTonSetting.GridColor.Use)
                    {
                        bool isHovering = SU_Editor_Input.IsWithinEditorMouseBounds(grid.WorldPosition_Cached, snapSetting.GridUnitOriginalVector2, swizzle);
                        if (isHovering)
                        {
                            Gizmos.color = gridColor.WithMultipliedAlpha(0.5f);
                            Gizmos.DrawCube(grid.WorldPosition_Cached + FloorStandardPositionCurrent + new Vector3(0, 0, 0.01f).SwizzlesVector(swizzle), snapSetting.GridUnitFlatCurrent);

                            //SU_Gizmo.TempGizmoColor_Action
                            //(
                            //    gridColor.WithMultipliedAlpha(0.8f).GetComplementaryColor(), () => Gizmos.DrawCube(grid.WorldPosition_Cached + new Vector3(0, 0, 0.01f).SwizzlesVector(swizzle), snapSetting.GridUnitFlatCurrent)
                            //);
                        }
                    }

                    //. 각 그리드 정보(태그) 그리기
                    DrawGizmo_EachGrid(main, grid, gridFlatSize, gridRadiusSize);
                }


                centers.Clear();
                ListPool<Vector3>.Release(centers);   //. 풀 반환
            }

            //? 각각의 그리드 정보(태그) 그리기
            private void DrawGizmo_EachGrid(StageGenerator main, Grid grid, Vector3 gridFlatSize, float gridRadiusSize)
            {
                var snapSetting = main.Setting.SnapSetting;
                var swizzle = snapSetting.Swizzle;
                var gridWorldPosition = grid.WorldPosition_Cached + FloorStandardPositionCurrent;



                //. "점유 상태" 그리기
                if (grid.IsOccupied && singleTonSetting.GridColor_Occupied.TryGetColor(out var occupiedColor))
                {
                    Gizmos.color = occupiedColor;
                    //if (isHovering) Handles.Label(gridWorldPosition + new Vector3(0, 0, 1f).SwizzlesVector(swizzle), $"<size=15><color=#{Gizmos.color.ToHex()}><b>x{grid.IsOccupied_GetHighestLayer}</b></color></size>", GuiStyle_Label);
                    SU_Gizmo.DrawCircleX(gridWorldPosition + new Vector3(0, 0, 0.075f).SwizzlesVector(swizzle), gridRadiusSize, swizzle);
                    //Gizmos.DrawWireCube(gridWorldPosition, gridFlatSize * 0.5f);

                    //Gizmos.color = occupiedColor.WithMultipliedAlpha(0.1f);
                    //Gizmos.DrawCube(gridWorldPosition, gridFlatSize);
                }


                //. <방> 태그 그리기
                if (grid.ContainsTag(GridTag.Room) && singleTonSetting.GridColor_RoomTag.TryGetColor(out var roomColor))
                {
                    Gizmos.color = roomColor.WithMultipliedAlpha(0.3f);
                    Gizmos.DrawCube(gridWorldPosition, gridFlatSize);
                }


                //. <방 테두리> 태그 그리기 (방이랑 같은 색 사용)
                //if (grid.ContainsTag(GridTag.Room_Edge) && singleTonSetting.GridColor_RoomTag.TryGetColor(out var roomEdgeColor))
                //{
                //    Gizmos.color = roomEdgeColor.WithMultipliedAlpha(0.1f);
                //    Gizmos.DrawCube(gridWorldPosition, gridFlatSize);
                //}


                //. <방 안전구역> 태그 그리기
                if (grid.ContainsTag(GridTag.Room_Safe) && singleTonSetting.GridColor_RoomSafeTag.TryGetColor(out var roomSafeColor))
                {
                    Gizmos.color = roomSafeColor.WithMultipliedAlpha(0.3f);
                    Gizmos.DrawCube(gridWorldPosition, gridFlatSize);


                    ////. <방 안전구역 방향> 태그 그리기
                    //Gizmos.color = Gizmos.color.WithMultipliedAlpha(0.5f);
                    //drawDirectionSafe();
                }


                //. <방 안전구역 확장> 태그 그리기
                if (grid.ContainsTag(GridTag.Room_ExpandSafe) && singleTonSetting.GridColor_RoomExpandSafeTag.TryGetColor(out var roomExpandSafeColor))
                {
                    Gizmos.color = roomExpandSafeColor.WithMultipliedAlpha(0.3f);
                    Gizmos.DrawCube(gridWorldPosition, gridFlatSize);

                    ////. <방 안전구역 방향> 태그 그리기
                    //Gizmos.color = Gizmos.color.WithMultipliedAlpha(0.5f);
                    //drawDirectionSafe();
                }


                void drawDirectionSafe()
                {
                    int angle = 30;

                    if (grid.ContainsTag(GridTag.Room_Safe_Down))
                    {
                        if (grid.AnyTag(GridTag.Room_Safe_Left))
                        {
                            //SU_Gizmo.DrawSolidTriangle(gridWorldPosition, gridRadiusSize, EDirection8.LeftDown, swizzle);
                            SU_Gizmo.DrawArrowHead(gridWorldPosition, gridRadiusSize, EDirection8.LeftDown, swizzle, angle);
                        }
                        else if (grid.AnyTag(GridTag.Room_Safe_Right))
                        {
                            //SU_Gizmo.DrawSolidTriangle(gridWorldPosition, gridRadiusSize, EDirection8.RightDown, swizzle);
                            SU_Gizmo.DrawArrowHead(gridWorldPosition, gridRadiusSize, EDirection8.RightDown, swizzle, angle);
                        }
                        else
                        {
                            //SU_Gizmo.DrawSolidTriangle(gridWorldPosition, gridRadiusSize, EDirection8.Down, swizzle);
                            SU_Gizmo.DrawArrowHead(gridWorldPosition, gridRadiusSize, EDirection8.Down, swizzle, angle);
                        }
                    }
                    if (grid.ContainsTag(GridTag.Room_Safe_Up))
                    {
                        if (grid.AnyTag(GridTag.Room_Safe_Left))
                        {
                            //SU_Gizmo.DrawSolidTriangle(gridWorldPosition, gridRadiusSize, EDirection8.LeftUp, swizzle);
                            SU_Gizmo.DrawArrowHead(gridWorldPosition, gridRadiusSize, EDirection8.LeftUp, swizzle, angle);
                        }
                        else if (grid.AnyTag(GridTag.Room_Safe_Right))
                        {
                            //SU_Gizmo.DrawSolidTriangle(gridWorldPosition, gridRadiusSize, EDirection8.RightUp, swizzle);
                            SU_Gizmo.DrawArrowHead(gridWorldPosition, gridRadiusSize, EDirection8.RightUp, swizzle, angle);
                        }
                        else
                        {
                            //SU_Gizmo.DrawSolidTriangle(gridWorldPosition, gridRadiusSize, EDirection8.Up, swizzle);
                            SU_Gizmo.DrawArrowHead(gridWorldPosition, gridRadiusSize, EDirection8.Up, swizzle, angle);
                        }
                    }
                    if (grid.ContainsTag(GridTag.Room_Safe_Left))
                    {
                        if (!grid.AnyTag(GridTag.Room_Safe_Down | GridTag.Room_Safe_Up))
                        {
                            //SU_Gizmo.DrawSolidTriangle(gridWorldPosition, gridRadiusSize, EDirection8.Left, swizzle);
                            SU_Gizmo.DrawArrowHead(gridWorldPosition, gridRadiusSize, EDirection8.Left, swizzle, angle);
                        }
                    }
                    if (grid.ContainsTag(GridTag.Room_Safe_Right))
                    {
                        if (!grid.AnyTag(GridTag.Room_Safe_Down | GridTag.Room_Safe_Up))
                        {
                            //SU_Gizmo.DrawSolidTriangle(gridWorldPosition, gridRadiusSize, EDirection8.Right, swizzle);
                            SU_Gizmo.DrawArrowHead(gridWorldPosition, gridRadiusSize, EDirection8.Right, swizzle, angle);
                        }
                    }
                }


                //. <방 도어> 태그 그리기
                if (grid.ContainsTag(GridTag.Room_Door_Area) && singleTonSetting.GridColor_RoomDoorTag.TryGetColor(out var roomDoorColor))
                {
                    Gizmos.color = roomDoorColor.WithMultipliedAlpha(0.7f);
                    Gizmos.DrawCube(gridWorldPosition, gridFlatSize);

                    if (grid.AnyTag(GridTag.Room_Door_Start | GridTag.Room_Door_End))
                    {
                        Gizmos.DrawWireCube(gridWorldPosition, gridFlatSize);
                    }
                    //if (grid.ContainsAny(GridTag.Room_Door))
                    //{
                    //    Gizmos.color = roomDoorColor.WithMultipliedAlpha(0.75f);
                    //    SU_Gizmo.DrawCubeRotated45(gridWorldPosition, gridFlatSize*0.5f, swizzle);
                    //    //Gizmos.color = roomDoorColor.WithMultipliedAlpha(0.75f);
                    //    //Gizmos.DrawCube(gridWorldPosition, gridFlatSize);
                    //}
                }


                //. <복도> 태그 그리기
                if (grid.ContainsTag(GridTag.Hallway) && singleTonSetting.GridColor_HallwayTag.TryGetColor(out var hallwayColor))
                {
                    Gizmos.color = hallwayColor.WithMultipliedAlpha(0.7f);

                    if (grid.ContainsTag(GridTag.Hallway_Path_Main))
                    {
                        SU_Gizmo.DrawCubeRotated45(gridWorldPosition, gridFlatSize * 0.5f, swizzle);
                    }
                    if (grid.ContainsTag(GridTag.Hallway_Path_Expand))
                    {
                        SU_Gizmo.DrawCubeRotated45(gridWorldPosition, gridFlatSize * 0.25f, swizzle);
                    }
                }


                //. <복도 테두리> 태그 그리기
                if (grid.ContainsTag(GridTag.Hallway_Edge) && singleTonSetting.GridColor_HallwayEdgeTag.TryGetColor(out var hallwayEdgeColor))
                {
                    Gizmos.color = hallwayEdgeColor.WithMultipliedAlpha(0.7f);

                    SU_Gizmo.DrawCubeRotated45(gridWorldPosition, gridFlatSize * 0.5f, swizzle);
                }


                //. <복도 안전구역> 태그 그리기
                if (grid.ContainsTag(GridTag.Hallway_Safe) && singleTonSetting.GridColor_HallwaySafeTag.TryGetColor(out var hallwaySafeColor))
                {
                    Gizmos.color = hallwaySafeColor.WithMultipliedAlpha(0.7f);

                    SU_Gizmo.DrawCubeRotated45(gridWorldPosition, gridFlatSize * 0.5f, swizzle);
                }


                //. <패스파인딩 금지 메인 경로> 태그 그리기
                if (grid.ContainsTag(GridTag.WasBanned_MainPath) && singleTonSetting.GridColor_WasBannedMainPath.TryGetColor(out var wasBannedMainPathColor))
                {
                    Gizmos.color = wasBannedMainPathColor.WithMultipliedAlpha(0.7f);
                    //SU_Gizmo.DrawSolidCircle(gridWorldPosition, gridRadiusSize, swizzle);
                    Gizmos.DrawCube(gridWorldPosition, gridFlatSize * 0.75f);
                }


                //. <복도 충돌 금지 그리드> 태그 그리기
                if (grid.ContainsTag(GridTag.WasBanned_CollisionOtherHallway) && singleTonSetting.GridColor_WasBannedCollisionOtherHallways.TryGetColor(out var wasBannedCollisionOtherHallwaysColor))
                {
                    Gizmos.color = wasBannedCollisionOtherHallwaysColor.WithMultipliedAlpha(0.7f);
                    //SU_Gizmo.DrawSolidCircle(gridWorldPosition, gridRadiusSize, swizzle);
                    Gizmos.DrawCube(gridWorldPosition, gridFlatSize * 0.75f);
                }


                //. <복도 연결을위한 임시 제어> 태그 그리기
                if (grid.ContainsTag(GridTag.TempControl_byConnectHallway) && singleTonSetting.GridColor_TempControl_byConnectHallway.TryGetColor(out var tempControl_byConnectHallwayColor))
                {
                    Gizmos.color = tempControl_byConnectHallwayColor;
                    Gizmos.DrawCube(gridWorldPosition, gridFlatSize);
                }


                //. <테스트> 태그 그리기
                if (grid.ContainsTag(GridTag.TempTest) && singleTonSetting.GridColor_TempTest.TryGetColor(out var tempTestColor))
                {
                    Gizmos.color = tempTestColor;
                    if (grid.GridDebuggingMemo == null || grid.GridDebuggingMemo == "")
                    {
                        Gizmos.DrawCube(gridWorldPosition, gridFlatSize);
                    }
                    else
                    {
                        if (grid.GridDebuggingMemo.Contains("door"))
                        {
                            Gizmos.color = Color.red.WithMultipliedAlpha(0.5f);
                            Gizmos.DrawCube(gridWorldPosition, gridFlatSize);
                        }


                        if (grid.GridDebuggingMemo.Contains("hallway"))
                        {
                            Gizmos.color = Color.blue.WithMultipliedAlpha(0.5f);
                            Gizmos.DrawCube(gridWorldPosition, gridFlatSize);
                        }
                    }
                }


                //. <경로비용> 그리기
                if (grid.PathCost != GridManager.PATHCOST_DEFAULT && singleTonSetting.GridColor_PathCost.TryGetColor(out var pathCostColor))
                {
                    Handles.Label(gridWorldPosition, $"<color={pathCostColor.ToHex()}>{grid.PathCost}</color>", guiStyle_Label);
                }
            }



            ///======================================================================================================================================================
        }
    }

}

#endif