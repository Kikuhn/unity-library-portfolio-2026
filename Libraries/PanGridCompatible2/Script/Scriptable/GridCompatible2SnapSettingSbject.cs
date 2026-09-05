using Pan.Util;
using Sirenix.OdinInspector;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;



namespace Pan.GridCompatibles2
{
    public interface IGridCompatibleSnapSetting
    {
        GridCompatible2SnapSetting GridCompatibleSnapSetting { get; }
    }



    [Serializable]
    public class GridCompatible2SnapSetting : ICopyable<GridCompatible2SnapSetting>
    {
        ///======================================================================================================================================================



        //? 스냅 설정 : Swizzle



        [BoxGroup("Snap 설정")]
        [BoxGroup("Snap 설정/Swizzle", false)]
        [LabelText("스냅 Swizzle")]
        [SerializeField]
        [PropertyOrder(0)]
        private Grid.CellSwizzle swizzle;
        public Grid.CellSwizzle Swizzle => swizzle;

        /// <summary>
        /// 월드 좌표를 원래 XY 평면으로 되돌릴 때 사용할 역 스위즐입니다.
        /// </summary>
        private Grid.CellSwizzle InverseSwizzle => swizzle switch
        {
            Grid.CellSwizzle.YZX => Grid.CellSwizzle.ZXY,
            Grid.CellSwizzle.ZXY => Grid.CellSwizzle.YZX,
            _ => swizzle
        };



        ///======================================================================================================================================================



        //? 스냅 설정 : 그리드 단위



        [BoxGroup("Snap 설정")]
        [BoxGroup("Snap 설정/그리드 단위")]
        [HorizontalGroup("Snap 설정/그리드 단위/그리드단위가로")]
        [LabelText("너비"), LabelWidth(30)]
        [SerializeField]
        [PropertyOrder(1)]
        [MinValue(1)]
        private int gridUnitX_Width = 1;
        public int GridUnitX_Width => gridUnitX_Width;



        [BoxGroup("Snap 설정")]
        [BoxGroup("Snap 설정/그리드 단위")]
        [HorizontalGroup("Snap 설정/그리드 단위/그리드단위가로")]
        [LabelText("높이"), LabelWidth(30)]
        [SerializeField]
        [PropertyOrder(1)]
        [MinValue(1)]
        private int gridUnitY_Height = 1;
        public int GridUnitY_Height => gridUnitY_Height;

        [BoxGroup("Snap 설정")]
        [BoxGroup("Snap 설정/그리드 단위")]
        [HorizontalGroup("Snap 설정/그리드 단위/그리드단위가로")]
        [LabelText("깊이"), LabelWidth(30)]
        [SerializeField]
        [PropertyOrder(1)]
        [MinValue(0)]
        private int gridUnitZ_Depth = 1;
        public int GridUnitZ_Depth => gridUnitZ_Depth;



        /// <summary>
        /// 그리드 단위 (Original), Swizzle 연산이 적용 되지 않은 그리드 단위 Vector3Int
        /// </summary>
        public Vector3Int GridUnitOriginalVector3 => new(gridUnitX_Width, gridUnitY_Height, gridUnitZ_Depth);



        /// <summary>
        /// 그리드 단위 (Original), Swizzle 연산이 적용 되지 않은 그리드 단위 Vector2Int
        /// </summary>
        public Vector2Int GridUnitOriginalVector2 => new(gridUnitX_Width, gridUnitY_Height);



        /// <summary>
        /// 그리드 단위 (Current), Swizzle 연산이 적용 되지만,<see cref="gridUnitZ_Depth"/> 는 0으로 사용하여 연산한다
        /// </summary>
        public Vector3 GridUnitFlatCurrent
        {
            get
            {
                return new Vector3(gridUnitX_Width, gridUnitY_Height, 0).SwizzlesVector(swizzle);
            }
        }




        [BoxGroup("Snap 설정")]
        [BoxGroup("Snap 설정/그리드 단위")]
        [LabelText("그리드 단위 (Current)"), LabelWidth(200)]
        [PropertyTooltip("Swizzle 연산이 적용된 그리드 단위")]
        [ShowInInspector, EnableGUI, DisplayAsString]
        [PropertyOrder(2)]
        /// <summary>
        /// 그리드 단위 (Current), Swizzle 연산이 적용된 그리드 단위
        /// </summary>
        public Vector3Int GridUnitCurrent
        {
            get
            {
                return GridUnitOriginalVector3.SwizzlesVectorInt(swizzle);
            }
        }



        /// <summary>
        /// 보정된 그리드 유닛의 평균값 (Z축은 제외됨)
        /// </summary>
        public float GridUnitAverageCorrection
        {
            get
            {
                var gridUnit = GridUnitOriginalVector2;
                var clamped_GridUnit = new Vector2(Mathf.Max(gridUnit.x, 1), Mathf.Max(gridUnit.y, 1));
                var average = ((Mathf.Max(gridUnit.x, 1) + Mathf.Max(gridUnit.y, 1)) / 2f).ClampedMax(clamped_GridUnit.GetMinComponent() * 2f);
                return average;
            }
        }



        ///======================================================================================================================================================



        //? 벡터 스냅



        #region Helpers

        //? 공용 스칼라 스냅(실수)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float SnapScalar(float v, int unit, bool snapUp)
        {
            //! 단위가 0 이하라면 스냅 불가 -> 그대로 반환
            if (unit <= 0) { return v; }

            //. 나눗셈 대신 역수를 곱해 미세하게 빠르게
            float inv = 1f / unit;
            return (snapUp ? Mathf.Ceil(v * inv) : Mathf.Floor(v * inv)) * unit;
        }

        //? 공용 스칼라 스냅(정수 결과)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int SnapScalarInt(float v, int unit, bool snapUp)
        {
            //! 단위가 0 이하라면 스냅 불가 -> 0 기준 반올림 결과를 반환(보수적으로 0)
            if (unit <= 0) { return Mathf.RoundToInt(v); }

            float inv = 1f / unit;
            return (snapUp ? Mathf.CeilToInt(v * inv) : Mathf.FloorToInt(v * inv)) * unit;
        }

        //? XY 2축 스냅(실수)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SnapXYRef(ref float x, ref float y, bool snapUp)
        {
            x = SnapScalar(x, GridUnitX_Width, snapUp);
            y = SnapScalar(y, GridUnitY_Height, snapUp);
        }

        #endregion


        /// <summary>
        /// 받아온 벡터를 GridUnit 스냅 한다
        /// </summary>
        /// <param name="target">스냅할 좌표</param>
        public void SnapToGridUnit(ref Vector2 target)
        {
            //? 셀 중심 스냅이면 올림(Ceil), 아니면 바닥(Floor)
            bool snapUp = SnapToGridCellCenter;
            SnapXYRef(ref target.x, ref target.y, snapUp);
        }

        /// <summary>
        /// 받아온 벡터를 GridUnit 스냅 한다
        /// </summary>
        /// <param name="target">스냅할 좌표</param>
        public Vector2Int SnapToGridUnit(Vector2 target)
        {
            bool snapUp = SnapToGridCellCenter;

            //. 정수 그리드 좌표로 반환
            return new Vector2Int(
                SnapScalarInt(target.x, GridUnitX_Width, snapUp),
                SnapScalarInt(target.y, GridUnitY_Height, snapUp)
            );
        }

        /// <summary>
        /// 받아온 Rect.position 을 GridUnit 스냅 한다
        /// </summary>
        /// <param name="target">스냅할 Rect (position 기준)</param>
        public void SnapToGridUnit(ref Rect target)
        {
            //. 위치만 스냅, 크기는 보존
            Vector2 p = target.position;
            SnapToGridUnit(ref p);
            target.position = p;
        }

        /// <summary>
        /// 받아온 벡터를 GridUnit 스냅 한다
        /// </summary>
        /// <param name="target">스냅할 좌표</param>
        /// <param name="useSwizzle">true면 스냅 전/후 스위즐 적용</param>
        public void SnapToGridUnit(ref Vector3 target, bool useSwizzle)
        {
            if (useSwizzle) { target.SwizzlesVectorRef(InverseSwizzle); }

            bool snapUp = SnapToGridCellCenter;
            SnapXYRef(ref target.x, ref target.y, snapUp);   //. Z는 보존

            if (useSwizzle) { target.SwizzlesVectorRef(Swizzle); }
        }

        /// <summary>
        /// 받아온 벡터를 GridUnit 스냅 한다
        /// </summary>
        /// <param name="target">스냅할 좌표</param>
        /// <param name="useSwizzle">true면 스냅 전 스위즐, 결과는 역스위즐로 되돌림</param>
        public Vector3Int SnapToGridUnit(Vector3 target, bool useSwizzle)
        {
            if (useSwizzle) { target.SwizzlesVectorRef(InverseSwizzle); }

            bool snapUp = SnapToGridCellCenter;

            //. 정수 그리드 좌표로 반환 (Z는 0 유지/보존 정책에 맞게 필요 시 확장)
            Vector3Int result = new Vector3Int(
                SnapScalarInt(target.x, GridUnitX_Width, snapUp),
                SnapScalarInt(target.y, GridUnitY_Height, snapUp),
                0
            );

            if (useSwizzle) { result.SwizzlesVectorIntRef(Swizzle); }

            return result;
        }



        #region 통합 전
        ///// <summary>
        ///// 받아온 벡터를 GridUnit 스냅 한다
        ///// </summary>
        ///// <param name="target"></param>
        //public void SnapToGridUnit(ref Vector2 target)
        //{
        //    if (SnapToGridCellCenter)
        //    {
        //        target.x = Mathf.Ceil(target.x / GridUnitX_Width) * GridUnitX_Width;
        //        target.y = Mathf.Ceil(target.y / GridUnitY_Height) * GridUnitY_Height;
        //    }
        //    else
        //    {
        //        target.x = Mathf.Floor(target.x / GridUnitX_Width) * GridUnitX_Width;
        //        target.y = Mathf.Floor(target.y / GridUnitY_Height) * GridUnitY_Height;
        //    }
        //}



        ///// <summary>
        ///// 받아온 벡터를 GridUnit 스냅 한다
        ///// </summary>
        ///// <param name="target"></param>
        //public Vector2Int SnapToGridUnit(Vector2 target)
        //{
        //    Vector2Int result = Vector2Int.zero;
        //    if (SnapToGridCellCenter)
        //    {
        //        result.x = Mathf.CeilToInt(target.x / GridUnitX_Width) * GridUnitX_Width;
        //        result.y = Mathf.CeilToInt(target.y / GridUnitY_Height) * GridUnitY_Height;
        //    }
        //    else
        //    {
        //        result.x = Mathf.FloorToInt(target.x / GridUnitX_Width) * GridUnitX_Width;
        //        result.y = Mathf.FloorToInt(target.y / GridUnitY_Height) * GridUnitY_Height;
        //    }
        //    return result;
        //}



        //public void SnapToGridUnit(ref Rect target)
        //{
        //    Vector2 snappedPosition = target.position;
        //    SnapToGridUnit(ref snappedPosition);
        //    target.position = snappedPosition;
        //}



        ///// <summary>
        ///// 받아온 벡터를 GridUnit 스냅 한다
        ///// </summary>
        ///// <param name="target"></param>
        //public void SnapToGridUnit(ref Vector3 target, bool useSwizzle)
        //{
        //    if (useSwizzle) { target.SwizzlesVectorRef(Swizzle); }

        //    if (SnapToGridCellCenter)
        //    {
        //        target.x = Mathf.Ceil(target.x / GridUnitX_Width) * GridUnitX_Width;
        //        target.y = Mathf.Ceil(target.y / GridUnitY_Height) * GridUnitY_Height;
        //    }
        //    else
        //    {
        //        target.x = Mathf.Floor(target.x / GridUnitX_Width) * GridUnitX_Width;
        //        target.y = Mathf.Floor(target.y / GridUnitY_Height) * GridUnitY_Height;
        //    }

        //    if (useSwizzle) { target.SwizzlesVectorRef(Swizzle); }
        //}



        ///// <summary>
        ///// 받아온 벡터를 GridUnit 스냅 한다
        ///// </summary>
        ///// <param name="target"></param>
        //public Vector3Int SnapToGridUnit(Vector3 target, bool useSwizzle)
        //{
        //    Vector3Int result = Vector3Int.zero;

        //    if (useSwizzle) { target.SwizzlesVectorRef(Swizzle); }

        //    if (SnapToGridCellCenter)
        //    {
        //        result.x = Mathf.CeilToInt(target.x / GridUnitX_Width) * GridUnitX_Width;
        //        result.y = Mathf.CeilToInt(target.y / GridUnitY_Height) * GridUnitY_Height;
        //    }
        //    else
        //    {
        //        result.x = Mathf.FloorToInt(target.x / GridUnitX_Width) * GridUnitX_Width;
        //        result.y = Mathf.FloorToInt(target.y / GridUnitY_Height) * GridUnitY_Height;
        //    }

        //    if (useSwizzle) { result.SwizzlesVectorIntRef(Swizzle); }

        //    return result;
        //} 
        #endregion



        ///======================================================================================================================================================



        //? 스냅 설정 : Depth-Z 보정


        //[BoxGroup("Snap 설정")]
        //[BoxGroup("Snap 설정/Depth-Z 보정", true)]
        //[HorizontalGroup("Snap 설정/Depth-Z 보정/자동깊이가로", 0.4f)] //[HorizontalGroup("오브젝트 설정/오프셋/자동오프셋가로", Width = 130)]
        //[LabelText("자동 Depth-Z 보정"), LabelWidth(80)]
        //[PropertyOrder(3)]
        //public float DepthZLength;



        //[BoxGroup("Snap 설정")]
        //[BoxGroup("Snap 설정/Depth-Z 보정", true)]
        //[HorizontalGroup("Snap 설정/Depth-Z 보정/자동깊이가로", 0.4f)] //[HorizontalGroup("오브젝트 설정/오프셋/자동오프셋가로", Width = 130)]
        //[LabelText("자동 Depth-Z 보정"), LabelWidth(80)]
        //[PropertyOrder(3)]
        //public bool UseAutoDepthZ = true;



        //[BoxGroup("Snap 설정")]
        //[BoxGroup("Snap 설정/Depth-Z 보정", true)]
        //[HorizontalGroup("Snap 설정/Depth-Z 보정/자동깊이가로")]
        //[HideLabel]
        //[SerializeField]
        //[EnableIf(nameof(UseAutoDepthZ))]
        //[PropertyOrder(4)]
        //private EDepthStandard depthStandard = EDepthStandard.Top;
        //public EDepthStandard DepthStandard => depthStandard;



        ///======================================================================================================================================================



        //? 스냅 설정 : 기타



        [BoxGroup("Snap 설정")]
        [BoxGroup("Snap 설정/기타", false)]
        [LabelText("바닥 기준 좌표 길이")]
        [SerializeField]
        [PropertyOrder(5)]
        private float floorStandardPositionLength;
        public float FloorStandardPositionLength => floorStandardPositionLength;



        [BoxGroup("Snap 설정")]
        [BoxGroup("Snap 설정/기타", false)]
        [LabelText("바닥 기준 좌표 (Current)")]
        [PropertyOrder(5)]
        public Vector3 GetFloorStandardPositionCurrent => new Vector3(0, 0, FloorStandardPositionLength).SwizzlesVector(Swizzle);



        [BoxGroup("Snap 설정")]
        [BoxGroup("Snap 설정/기타", false)]
        [LabelText("그리드 셀 중심에 스냅")]
        [PropertyTooltip(
        "체크 해제 시:\n" +
        " - 오브젝트 중심이 그리드 교차점(선)에 스냅됩니다.\n" +
        "체크 시:\n" +
        " - 오브젝트 중심이 그리드 셀의 중앙에 스냅됩니다.\n" +
        "   (셀 크기 1일 때 [0,0]은 [0.5,0.5]가 됩니다.)")]
        [SerializeField]
        [PropertyOrder(6)]
        private bool snapToGridCellCenter = true;
        public bool SnapToGridCellCenter => snapToGridCellCenter;



//        [BoxGroup("Snap 설정")]
//        [BoxGroup("Snap 설정/기타", false)]
//        [LabelText("그리드 좌표 보정")]
//        [PropertyTooltip("이 값만큼 그리드 좌표가 더해 보정된다\n그리드 좌표에만 적용되며 트랜스폼 좌표에는 영향을 주지 않는다")]
//        [SerializeField]
//        [PropertyOrder(7)]
//        [GUIColor(nameof(editorGridPositionCorrectionColor))]
//        public Vector2Int GridPositionCorrection;



//#if UNITY_EDITOR
//        private Color editorGridPositionCorrectionColor
//        {
//            get
//            {
//                if (GridPositionCorrection == Vector2Int.zero)
//                {
//                    return Color.white;
//                }
//                else
//                {
//                    return new Color(0.93f, 0.33f, 0.40f, 1f);
//                }
//            }
//        }
//#endif



        ///======================================================================================================================================================



        [BoxGroup("Snap 설정")]
        [Button("Snap 설정 초기화", Icon = SdfIconType.ArrowClockwise, ButtonAlignment = 1f, Stretch = false), GUIColor(0.97f, 0.85f, 0.39f)]
        [PropertyOrder(8)]
        private void RefreshSnapSetting()
        {
            swizzle = GridLayout.CellSwizzle.XYZ;
            gridUnitX_Width = 1;
            gridUnitY_Height = 1;
            gridUnitZ_Depth = 1;
            snapToGridCellCenter = true;
            floorStandardPositionLength = 0;

            //DepthZLength = 0;
            //UseAutoDepthZ = true;
            //depthStandard = EDepthStandard.Top;

            //GridPositionCorrection = Vector2Int.zero;
        }



        ///======================================================================================================================================================



        //? 그리드 스냅 좌표 연산


        private Vector2 CalculateGridSnapPositionInternal(Vector2 gridSnapPosition)
        {
            if (SnapToGridCellCenter)
            {
                //. 이런 방식으로 연산해야, 중앙 스냅이여도, 그리드 스냅이 원활하게 이루어진다
                gridSnapPosition.x = Mathf.CeilToInt((gridSnapPosition.x + GridUnitX_Width * 0.5f) / GridUnitX_Width) * GridUnitX_Width;
                gridSnapPosition.y = Mathf.CeilToInt((gridSnapPosition.y + GridUnitY_Height * 0.5f) / GridUnitY_Height) * GridUnitY_Height;
                gridSnapPosition.x -= GridUnitX_Width * 0.5f;
                gridSnapPosition.y -= GridUnitY_Height * 0.5f;
            }
            else
            {
                gridSnapPosition.x = Mathf.FloorToInt(gridSnapPosition.x / GridUnitX_Width) * GridUnitX_Width;
                gridSnapPosition.y = Mathf.FloorToInt(gridSnapPosition.y / GridUnitY_Height) * GridUnitY_Height;
            }

            return gridSnapPosition;
        }

        /// <summary>
        /// <b>트랜스폼 좌표 V3</b>를 받아와, <c>그리드 스냅 좌표</c>로 연산한다
        /// </summary>
        public Vector2 CalculateGridSnapPosition_byTransformPositionV3(Vector3 transformPositionV3) => CalculateGridSnapPositionInternal(transformPositionV3.SwizzlesVector(InverseSwizzle));

        /// <summary>
        /// <b>트랜스폼 좌표 V2</b>를 받아와, <c>그리드 스냅 좌표</c>로 연산한다
        /// <para><i>swizzle 된 Vector2 좌표를 받아야 한다</i></para>
        /// </summary>
        public Vector2 CalculateGridSnapPosition_byTransformPositionV2(Vector2 transformPositionV2) => CalculateGridSnapPositionInternal(transformPositionV2);



        //? 그리드 좌표 연산



        /// <summary>
        /// <b>트랜스폼 좌표 V3</b>를 받아와, <c>그리드 좌표</c>로 연산한다
        /// </summary>
        /// <param name="preferUpperRightGridCell">우측상단 셀 우선 여부</param>
        public Vector2Int CalculateGridPosition_byTransformPositionV3(Vector3 transformPosition)
        {
            var gridSnapPosition = CalculateGridSnapPosition_byTransformPositionV3(transformPosition);

            Vector2Int gridPosition =
                SnapToGridCellCenter ?
                new Vector2Int(Mathf.CeilToInt(gridSnapPosition.x) - 1, Mathf.CeilToInt(gridSnapPosition.y) - 1) :
                new Vector2Int(Mathf.CeilToInt(gridSnapPosition.x), Mathf.CeilToInt(gridSnapPosition.y));


            gridPosition.x /= GridUnitX_Width;
            gridPosition.y /= GridUnitY_Height;

            return gridPosition;
        }

        /// <summary>
        /// <b>그리드 좌표</b>를 받아와, <c>트랜스폼 좌표 V3</c>로 연산한다
        /// </summary>
        /// <param name="gridPosition">변환할 그리드 좌표(Vector2Int)</param>
        /// <param name="preferUpperRight">snapToCellCenter가 false일 때, 셀의 좌하단(false)과 우상단(true) 중 어느 코너를 기준으로 할지 결정합니다.</param>
        /// <param name="applySwizzle">계산 결과에 이 설정 파일의 Swizzle 설정을 적용할지 여부입니다.</param>
        /// <returns>계산된 최종 월드 좌표(Vector3)</returns>
        public Vector3 CalculateTransformPosition_byGridPosition(Vector2Int gridPosition, bool applySwizzle)
        {
            //. 그리드 좌표를 기본 로컬 좌표로 변환 (항상 좌측 하단 기준)
            float xPos = gridPosition.x * GridUnitX_Width;
            float yPos = gridPosition.y * GridUnitY_Height;

            //. 스냅 설정에 따라 위치 보정
            if (SnapToGridCellCenter)
            {
                //. 셀의 정중앙으로 보정
                xPos += GridUnitX_Width * 0.5f;
                yPos += GridUnitY_Height * 0.5f;
            }

            return applySwizzle ? new Vector3(xPos, yPos, 0).SwizzlesVector(Swizzle) : new Vector3(xPos, yPos, 0);
        }



        ///======================================================================================================================================================



        public void Copy(GridCompatible2SnapSetting original)
        {
            swizzle = original.swizzle;
            gridUnitX_Width = original.gridUnitX_Width;
            gridUnitY_Height = original.gridUnitY_Height;
            gridUnitZ_Depth = original.gridUnitZ_Depth;
            snapToGridCellCenter = original.snapToGridCellCenter;
            floorStandardPositionLength = original.floorStandardPositionLength;
        }



        ///======================================================================================================================================================

    }



    [CreateAssetMenu(fileName = nameof(GridCompatible2SnapSettingSbject), menuName = CreateAssetMenuInfo.GRIDCOMPATIBLE_SNAPSETTING)]
    public class GridCompatible2SnapSettingSbject : ScriptableObject, IGridCompatibleSnapSetting
    {
        public GridCompatible2SnapSetting GridCompatibleSnapSetting => gridCompatibleSnapSetting;

        [SerializeField]
        [HideLabel, InlineProperty]
        [Title("그리드 호환 Snap 설정 SO", HorizontalLine = false, TitleAlignment = TitleAlignments.Centered)]
        private GridCompatible2SnapSetting gridCompatibleSnapSetting = new GridCompatible2SnapSetting();
    }

}
