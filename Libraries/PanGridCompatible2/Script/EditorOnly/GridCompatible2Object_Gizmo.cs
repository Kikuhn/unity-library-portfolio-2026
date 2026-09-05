#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Pan.GridCompatibles2;
using Pan.Util;
using Pan.Util.Editors;
using Sirenix.OdinInspector;



namespace Pan.GridCompatibles2
{
    public partial class GridCompatible2Object
    {
        private readonly GridCompatible2Object_Gizmo gizmo = new GridCompatible2Object_Gizmo();


        [BoxGroup("기즈모박스그룹", false)]
        [LabelText(" Current 그리드 호환 기즈모 사용", Icon = SdfIconType.BorderAll)]
        [ShowInInspector]
        [PropertyOrder(9999)]
        private bool UseGizmo_GridCompatible2Object { get => gizmo.useGizmo; set => gizmo.useGizmo = value; }



        protected virtual void OnDrawGizmos()
        {
            gizmo.OnDrawGizmos(this);
        }



        private class GridCompatible2Object_Gizmo : BaseGizmoClass<GridCompatible2Object>
        {
            ///======================================================================================================================================================



            private static GUIStyle guiStyle_GridPosition;
            private static GUIStyle GuiStyle_GridPosition
            {
                get
                {
                    if (guiStyle_GridPosition == null)
                    {
                        guiStyle_GridPosition = new GUIStyle
                        {
                            alignment = TextAnchor.MiddleCenter,
                            fontSize = 12,
                            normal = new GUIStyleState { textColor = Color.white },
                            richText = true
                        };
                    }

                    return guiStyle_GridPosition;
                }
            }



            bool IsObjectSelected_WithChild;
            bool IsObjectSelected_WithParent;



            ///======================================================================================================================================================



            Vector3 GridCenterTFCPosition;
            Vector3 GridSnapTFCPosition;
            Vector3 GridFloorCenterTFCPosition;
            Vector3 GriMiddleCenterTFCPosition;
            Vector3 GridCeilingCenterTFCPosition;



            //. 그리드 바닥면을 그리기 위한 납작한 오브젝트의 크기 (깊이Z축: 0)
            Vector3 GridFlatTFSize;



            ///======================================================================================================================================================



            private static GridCompatible2SingletonSettingSbject singleton => GridCompatible2SingletonSettingSbject.O;



            ///======================================================================================================================================================



            protected override void DrawGizmo(GridCompatible2Object main)
            {
                if (GridCompatible2SingletonSettingSbject.O.UseGridGizmo == false) { return; }

                //. 오브젝트 선택 여부 갱신
                IsObjectSelected_WithChild = SU_EditorControl.IsObjectSelected(main.transform, true, false, true);
                IsObjectSelected_WithParent = SU_EditorControl.IsObjectSelected(main.transform, true, true, false);

                if (GridCompatible2SingletonSettingSbject.O.UseGridGizmoOnlySelect && !IsObjectSelected_WithParent) { return; }


                GridCenterTFCPosition = main.GridCenterTransformCurrentPosition;
                GridSnapTFCPosition = main.GridSnapTransformCurrentPosition + main.GridCompatible.ObjectDepthZTransformCurrent;
                GridFloorCenterTFCPosition = main.GridFloorCenterTransformCurrentPosition;
                GriMiddleCenterTFCPosition = main.GridMiddleCenterTransformCurrentPosition;
                GridCeilingCenterTFCPosition = main.GridCeilingCenterTransformCurrentPosition;
                GridFlatTFSize = main.GridCompatible.ObjectFlatSizeTransformCurrent;


                DrawGizmo_FloorToCeilingArrow(main);
                DrawGizmo_FloorFlatGridObject(main);
                DrawGizmo2_GridObjectDepth(main);
                DrawGizmo3_StandardGridPosition(main);
            }



            //? "바닥 기준 좌표"와 "트랜스폼 오브젝트의 깊이Z축"의 떨어진 정도를 표시하는 화살표를 그린다
            private void DrawGizmo_FloorToCeilingArrow(GridCompatible2Object main)
            {
                if (!singleton.FloorToCeilingArrowColor.TryGetColor(out var floorToCilingArrowColor)) { return; }
                Gizmos.color = floorToCilingArrowColor;
                SU_Gizmo.DrawArrow(main.TransformPositionInternal, main.TransformPositionInternal + main.GridCompatible.ObjectDepthZTransformCurrent - new Vector3(0, 0, main.TransformPositionInternal.GetAxisBeforeSwizzle(EAxis.Z, main.GridCompatible.CurrentSnapSetting.Swizzle)).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle));
            }



            //? 바닥 면, 중심점 표식 그리기
            private void DrawGizmo_FloorFlatGridObject(GridCompatible2Object main)
            {
                //. 바닥면 + 테두리 그리기
                if (singleton.GridColor.TryGetColor(out var gridColor))
                {
                    Gizmos.color = gridColor;
                    Gizmos.DrawWireCube(GridFloorCenterTFCPosition, GridFlatTFSize);

                    Gizmos.color = gridColor.WithMultipliedAlpha(0.1f);

                    Gizmos.DrawCube(GridFloorCenterTFCPosition + (singleton.GetCorrectionSizeDepthZLength(main.GridCompatible, true)), GridFlatTFSize + singleton.GetCorrectionSizeDepthZLength(main.GridCompatible, false));
                    //. 깊이축을 Z축을 0으로 하면, Z Depth Fighting 현상이 일어나, 0.001f 값 만큼 더한 평면으로 그린다
                }


                //. 중심점 표식 그리기
                if (singleton.GridCenterColor.TryGetColor(out var gridCenterColor))
                {
                    Gizmos.color = gridCenterColor;
                    var gizmoCenterMarkRadius = singleton.GetGizmoMarkRadius(main.GridCompatible);

                    //. "중심점 표식" 그리기
                    SU_Gizmo.DrawCircleX(GridFloorCenterTFCPosition, gizmoCenterMarkRadius, main.GridCompatible.CurrentSnapSetting.Swizzle, true);

                    //. "스냅 중심점" 그리기
                    Gizmos.DrawWireCube(GridSnapTFCPosition, main.GridCompatible.CurrentSnapSetting.GridUnitFlatCurrent * 0.5f);

                    //. 오브젝트를 선택하고있을때, 그리드 좌표 텍스트 출력하기
                    if (IsObjectSelected_WithChild)
                    {
                        var gridPosition = main.GridPosition;
                        var correction = singleton.GetGizmoTextCorrectionDistance(main.GridCompatible);

                        var gridPositionTextPosition = GridSnapTFCPosition + correction;
                        Handles.Label(gridPositionTextPosition, $"<color={gridColor.ToHex()}><b>[{gridPosition.x}, {gridPosition.y}]</b>\n {main.transform.position}\n<b>{main.GridCompatible.ObjectSizeX_Width} x {main.GridCompatible.ObjectSizeY_Height}</b></color>", GuiStyle_GridPosition);
                    }
                }


                //. 크기가 1x1를 넘는다면 내부에 그리드 라인을 그린다 (선택중일때만)
                if ((IsObjectSelected_WithParent || IsObjectSelected_WithChild))
                {
                    if (singleton.GridLineColor.TryGetColor(out var gridLineColor) && (main.GridCompatible.ObjectSizeX_Width > 1 || main.GridCompatible.ObjectSizeY_Height > 1))
                    {
                        Gizmos.color = gridLineColor;
                        SU_Gizmo.DrawGridLine2D(GridFloorCenterTFCPosition, main.GridCompatible.ObjectSizeTransformVector2, main.GridCompatible.CurrentSnapSetting.GridUnitOriginalVector2, main.GridCompatible.CurrentSnapSetting.Swizzle, false);
                    }
                }
            }



            //? 그리드 오브젝트의 깊이, 큐브 그리기 (점선을 사용하기위해 Cube가 아니라 여러 선으로 큐브를 구현)
            private void DrawGizmo2_GridObjectDepth(GridCompatible2Object main)
            {
                if (!singleton.GridColor.TryGetColor(out var gridColor)) { return; }
                Gizmos.color = gridColor;

                //. 깊이Z축이 도합 0 초과일때만 그리게 한다
                if (main.GridCompatible.CurrentSnapSetting.GridUnitZ_Depth > 0 && main.GridCompatible.ObjectSizeZ_Depth > 0)
                {
                    //. 그리드 오브젝트의 천장 테두리를 그리기
                    Gizmos.DrawWireCube(GridCeilingCenterTFCPosition, GridFlatTFSize);

                    float dashSize = 0.2f;
                    float gapSize = 0.1f;

                    var gridObjectFlatTransformSizeHalf = GridFlatTFSize * 0.5f;
                    var gridObjectFlatTransformSizeHalf2 = gridObjectFlatTransformSizeHalf;
                    gridObjectFlatTransformSizeHalf2.ModifyVectorBySwizzle(EAxis.Y, gridObjectFlatTransformSizeHalf2.GetAxisBeforeSwizzle(EAxis.Y, main.GridCompatible.CurrentSnapSetting.Swizzle) - main.GridCompatible.ObjectSizeTransformY_Height, main.GridCompatible.CurrentSnapSetting.Swizzle);


                    //. 좌측 하단 선
                    SU_Gizmo.DrawDottedLine(GridFloorCenterTFCPosition - gridObjectFlatTransformSizeHalf, GridCeilingCenterTFCPosition - gridObjectFlatTransformSizeHalf, dashSize, gapSize);

                    //. 좌측 상단 선
                    SU_Gizmo.DrawDottedLine(GridFloorCenterTFCPosition - gridObjectFlatTransformSizeHalf2, GridCeilingCenterTFCPosition - gridObjectFlatTransformSizeHalf2, dashSize, gapSize);

                    //. 우측 상단 선
                    SU_Gizmo.DrawDottedLine(GridFloorCenterTFCPosition + gridObjectFlatTransformSizeHalf, GridCeilingCenterTFCPosition + gridObjectFlatTransformSizeHalf, dashSize, gapSize);

                    //. 우측 하단 선
                    SU_Gizmo.DrawDottedLine(GridFloorCenterTFCPosition + gridObjectFlatTransformSizeHalf2, GridCeilingCenterTFCPosition + gridObjectFlatTransformSizeHalf2, dashSize, gapSize);
                }


                //. 큐브 가운데 십자가 그리기
                SU_Gizmo.DrawCross(GriMiddleCenterTFCPosition, (Vector3)main.GridCompatible.CurrentSnapSetting.GridUnitCurrent * 0.1f);
            }



            //? 그리드 스탠다드 좌표 그리기 (상하좌우 + 대각선4종)
            private void DrawGizmo3_StandardGridPosition(GridCompatible2Object main)
            {
                if (!IsObjectSelected_WithParent) { return; }

                if (!singleton.GridColor.TryGetColor(out var gridColor)) { return; }

                //! 오브젝트 크기가 3x3 이상일 때에만 이 스탠다드 그리드 좌표를 그린다
                if (main.GridCompatible.ObjectSizeX_Width < 3 || main.GridCompatible.ObjectSizeY_Height < 3) { return; }

                Gizmos.color = gridColor;

                var colorHex = Gizmos.color.ToHex();
                //var cubeSize = main.GridCompatible.CurrentSnapSetting.GridUnitFlatCurrent * 0.5f;

                var lowerCenter = main.GridPositionStandard(ECenterStandard.LowerCenter);
                var lowerLeft = main.GridPositionStandard(ECenterStandard.LowerLeft);
                var lowerRight = main.GridPositionStandard(ECenterStandard.LowerRight);

                var upperCenter = main.GridPositionStandard(ECenterStandard.UpperCenter);
                var upperLeft = main.GridPositionStandard(ECenterStandard.UpperLeft);
                var upperRight = main.GridPositionStandard(ECenterStandard.UpperRight);

                var middleLeft = main.GridPositionStandard(ECenterStandard.MiddleLeft);
                var middleRight = main.GridPositionStandard(ECenterStandard.MiddleRight);



                //var lowerCenterTransformPosition = main.GridCompatible.ConvertGridPositionToTransformPosition(lowerCenter - main.GridPositionCorrectionPlus, true);
                //var lowerLeftTransformPosition = main.GridCompatible.ConvertGridPositionToTransformPosition(lowerLeft - main.GridPositionCorrectionPlus, true);
                //var lowerRightTransformPosition = main.GridCompatible.ConvertGridPositionToTransformPosition(lowerRight - main.GridPositionCorrectionPlus, true);

                //var upperCenterTransformPosition = main.GridCompatible.ConvertGridPositionToTransformPosition(upperCenter - main.GridPositionCorrectionPlus, true);
                //var upperLeftTransformPosition = main.GridCompatible.ConvertGridPositionToTransformPosition(upperLeft - main.GridPositionCorrectionPlus, true);
                //var upperRightTransformPosition = main.GridCompatible.ConvertGridPositionToTransformPosition(upperRight - main.GridPositionCorrectionPlus, true);

                //var middleLeftTransformPosition = main.GridCompatible.ConvertGridPositionToTransformPosition(middleLeft - main.GridPositionCorrectionPlus, true);
                //var middleRightTransformPosition = main.GridCompatible.ConvertGridPositionToTransformPosition(middleRight - main.GridPositionCorrectionPlus, true);


                var lowerCenterTransformPosition = main.GridPositionStandardTransformPosition(ECenterStandard.LowerCenter, true);
                var lowerLeftTransformPosition = main.GridPositionStandardTransformPosition(ECenterStandard.LowerLeft, true);
                var lowerRightTransformPosition = main.GridPositionStandardTransformPosition(ECenterStandard.LowerRight, true);

                var upperCenterTransformPosition = main.GridPositionStandardTransformPosition(ECenterStandard.UpperCenter, true);
                var upperLeftTransformPosition = main.GridPositionStandardTransformPosition(ECenterStandard.UpperLeft, true);
                var upperRightTransformPosition = main.GridPositionStandardTransformPosition(ECenterStandard.UpperRight, true);

                var middleLeftTransformPosition = main.GridPositionStandardTransformPosition(ECenterStandard.MiddleLeft, true);
                var middleRightTransformPosition = main.GridPositionStandardTransformPosition(ECenterStandard.MiddleRight, true);

                var textCorrection = main.GridCompatible.ObjectDepthZCurrent + GridCompatible2SingletonSettingSbject.O.GetGizmoTextCorrectionDistance(main.GridCompatible);

                //var zOffset = main.GridCompatible.ObjectDepthZCurrent + (main.GridCompatible.ObjectDepthZTransformCurrent * 0.5f);

                lowerCenterTransformPosition += textCorrection;
                lowerLeftTransformPosition += textCorrection;
                lowerRightTransformPosition += textCorrection;


                upperCenterTransformPosition += textCorrection;
                upperLeftTransformPosition += textCorrection;
                upperRightTransformPosition += textCorrection;

                middleLeftTransformPosition += textCorrection;
                middleRightTransformPosition += textCorrection;


                //Gizmos.DrawWireCube(lowerCenterTransformPosition, cubeSize);
                //Gizmos.DrawWireCube(lowerLeftTransformPosition, cubeSize);
                //Gizmos.DrawWireCube(lowerRightTransformPosition, cubeSize);

                //Gizmos.DrawWireCube(upperCenterTransformPosition, cubeSize);
                //Gizmos.DrawWireCube(upperLeftTransformPosition, cubeSize);
                //Gizmos.DrawWireCube(upperRightTransformPosition, cubeSize);

                //Gizmos.DrawWireCube(middleLeftTransformPosition, cubeSize);
                //Gizmos.DrawWireCube(middleRightTransformPosition, cubeSize);

                //Gizmos.color = singleton.GridCenterColor;

                Handles.Label(lowerCenterTransformPosition, $"<color=#{colorHex}><b>↓\n[{lowerCenter.x}, {lowerCenter.y}]</b></color>", GuiStyle_GridPosition);
                Handles.Label(lowerLeftTransformPosition, $"<color=#{colorHex}><b>↙\n[{lowerLeft.x}, {lowerLeft.y}]</b></color>", GuiStyle_GridPosition);
                Handles.Label(lowerRightTransformPosition, $"<color=#{colorHex}><b>↘\n[{lowerRight.x}, {lowerRight.y}]</b></color>", GuiStyle_GridPosition);

                Handles.Label(upperCenterTransformPosition, $"<color=#{colorHex}><b>↑\n[{upperCenter.x}, {upperCenter.y}]</b></color>", GuiStyle_GridPosition);
                Handles.Label(upperLeftTransformPosition, $"<color=#{colorHex}><b>↖\n[{upperLeft.x}, {upperLeft.y}]</b></color>", GuiStyle_GridPosition);
                Handles.Label(upperRightTransformPosition, $"<color=#{colorHex}><b>↗\n[{upperRight.x}, {upperRight.y}]</b></color>", GuiStyle_GridPosition);

                Handles.Label(middleLeftTransformPosition, $"<color=#{colorHex}><b>←\n[{middleLeft.x}, {middleLeft.y}]</b></color>", GuiStyle_GridPosition);
                Handles.Label(middleRightTransformPosition, $"<color=#{colorHex}><b>→\n[{middleRight.x}, {middleRight.y}]</b></color>", GuiStyle_GridPosition);
            }



            ///======================================================================================================================================================
        }
    }
}



#endif