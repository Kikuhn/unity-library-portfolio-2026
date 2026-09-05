using UnityEngine;
using System;
using Sirenix.OdinInspector;
using Pan.Util;




namespace Pan.GridCompatibles2
{
    [Serializable]
    public class GridCompatible2 : ICopyable<GridCompatible2>, ISerializationCallbackReceiver
    {
        ///======================================================================================================================================================



        //? 직렬화 전/후 호출



        void ISerializationCallbackReceiver.OnBeforeSerialize() { }



        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            //ApplyGridPositionCorrection();
            //ApplyAutoDepthZ();
            ValueChangedSnapSetting();
        }



        ///======================================================================================================================================================



        //? Snap 설정



        #region Snap 설정
        [TitleGroup("Snap 설정")]
        [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Center, EnableRichText = true), EnableGUI]
        [PropertyOrder(0)]
        [PropertySpace(8, 8)]
        private string dummy_SnapSettingMessage
        {
            get
            {
                if (!IsUseExternalSnapSetting)
                {
                    return "<color=white><size=13><b><color=#4fc1e9>내장</color></b> Snap 설정 적용중</size></color>";
                }
                else
                {
                    return "<color=white><size=13><b><color=#ed5565>외장</color></b> Snap 설정 적용중</size></color>";
                }
            }
        }



        [TitleGroup("Snap 설정")]
        [HideLabel, InlineProperty]
        [HideIf(nameof(IsUseExternalSnapSetting))]
        [SerializeField]
        [PropertyOrder(1)]
        [OnValueChanged(nameof(ValueChangedSnapSetting), true)]
        private GridCompatible2SnapSetting SnapSettingInternal = new();



        [TitleGroup("Snap 설정")]
        [LabelText("💾 외장 Snap 설정 SO")]
        [InlineEditor]
        [SerializeField]
        [PropertyOrder(1)]
        [OnValueChanged(nameof(ValueChangedSnapSetting), true)]
        private GridCompatible2SnapSettingSbject SnapSettingExternal;



        public GridCompatible2SnapSettingSbject GetSnapSettingExternal => SnapSettingExternal;



        [TitleGroup("Snap 설정")]
        [ShowIf(nameof(IsUseExternalSnapSetting))]
        [Button("외장 Snap 설정 제거", Icon = SdfIconType.X, ButtonAlignment = 1f, Stretch = false), GUIColor(0.93f, 0.33f, 0.40f)]
        [Tooltip("외장 Snap 설정을 제거하고 내장 Snap 설정으로 되돌립니다")]
        [PropertyOrder(1)]
        private void RemoveSnapSettingExternal()
        {
            if (SnapSettingExternal != null)
            {
                SnapSettingInternal.Copy(SnapSettingExternal.GridCompatibleSnapSetting);
                SnapSettingExternal = null;
            }
        }



        private bool IsUseExternalSnapSetting => SnapSettingExternal != null;



        /// <summary>
        /// 현재 Snap 설정을 반환 (내장 Snap 설정 또는 외장 Snap 설정)
        /// </summary>
        public GridCompatible2SnapSetting CurrentSnapSetting
        {
            get
            {
                if (IsUseExternalSnapSetting)
                {
                    return SnapSettingExternal.GridCompatibleSnapSetting;
                }
                else
                {
                    return SnapSettingInternal;
                }
            }
        }



        private void ValueChangedSnapSetting()
        {
            //ApplyAutoDepthZ();
        }
        #endregion



        ///======================================================================================================================================================



        //? 오브젝트 설정 : Size



        #region Size

        [TitleGroup("오브젝트 설정")]
        [BoxGroup("오브젝트 설정/Size", true)]
        [HorizontalGroup("오브젝트 설정/Size/오브젝트크기가로")]
        [LabelText("너비"), LabelWidth(30)]
        [SerializeField]
        [PropertyOrder(2)]
        [MinValue(1)]
        private int objectSizeX_Width = 1;
        public int ObjectSizeX_Width => objectSizeX_Width;
        public int ObjectSizeTransformX_Width => ObjectSizeX_Width * CurrentSnapSetting.GridUnitX_Width;

        [TitleGroup("오브젝트 설정")]
        [BoxGroup("오브젝트 설정/Size", true)]
        [HorizontalGroup("오브젝트 설정/Size/오브젝트크기가로")]
        [LabelText("높이"), LabelWidth(30)]
        [SerializeField]
        [PropertyOrder(2)]
        [MinValue(1)]
        private int objectSizeY_Height = 1;
        public int ObjectSizeY_Height => objectSizeY_Height;
        public int ObjectSizeTransformY_Height => ObjectSizeY_Height * CurrentSnapSetting.GridUnitY_Height;

        [TitleGroup("오브젝트 설정")]
        [BoxGroup("오브젝트 설정/Size", true)]
        [HorizontalGroup("오브젝트 설정/Size/오브젝트크기가로")]
        [LabelText("깊이"), LabelWidth(30)]
        [SerializeField]
        [PropertyOrder(2)]
        [MinValue(0)]
        private float objectSizeZ_Depth = 0f;
        public float ObjectSizeZ_Depth => objectSizeZ_Depth;
        public int ObjectSizeZ_DepthInt => Mathf.CeilToInt(objectSizeZ_Depth);
        public float ObjectSizeTransformZ_Depth => ObjectSizeZ_Depth * CurrentSnapSetting.GridUnitZ_Depth;



        /// <summary>
        /// 오브젝트 크기 (Original), Swizzle 연산이 적용 되지 않은 Vector3Int
        /// </summary>
        /// 
        public Vector3Int ObjectSizeOriginalVector3 => new(objectSizeX_Width, objectSizeY_Height, ObjectSizeZ_DepthInt);



        /// <summary>
        /// 오브젝트 크기 (Original), Swizzle 연산이 적용 되지 않은 Vector2Int
        /// </summary>
        public Vector2Int ObjectSizeOriginalVector2 => new(objectSizeX_Width, objectSizeY_Height);



        [TitleGroup("오브젝트 설정")]
        [BoxGroup("오브젝트 설정/Size", true)]
        [LabelText("오브젝트 크기 (Current)")]
        [PropertyTooltip("Swizzle 연산이 적용된 오브젝트 Size")]
        [ShowInInspector, EnableGUI, DisplayAsString]
        [PropertyOrder(2)]
        ///<summary>
        /// 오브젝트 크기 (Current), Swizzle 연산이 적용된 오브젝트 Size
        /// </summary>
        public Vector3Int ObjectSizeCurrentVector3 => ObjectSizeOriginalVector3.SwizzlesVectorInt(CurrentSnapSetting.Swizzle);



        /// <summary>
        /// 오브젝트 크기 (Transform)
        /// </summary>
        [TitleGroup("오브젝트 설정")]
        [BoxGroup("오브젝트 설정/Size", true)]
        [LabelText("오브젝트 크기 (Transform)")]
        [PropertyTooltip("GridUnit 연산이 적용된 오브젝트의 트랜스폼 Size")]
        [ShowInInspector, EnableGUI, DisplayAsString]
        [PropertyOrder(2)]
        public Vector2Int ObjectSizeTransformVector2 => ObjectSizeOriginalVector2 * CurrentSnapSetting.GridUnitOriginalVector2;



        [TitleGroup("오브젝트 설정")]
        [BoxGroup("오브젝트 설정/Size", true)]
        [LabelText("오브젝트 크기 (Transform, Current)")]
        [PropertyTooltip("Swizzle 연산과 GridUnit 연산이 적용된 실제 오브젝트의 트랜스폼 Size")]
        [ShowInInspector, EnableGUI, DisplayAsString]
        [PropertyOrder(2)]
        ///<summary>
        /// 오브젝트 크기 (Transform, Current), Swizzle 연산과 GridUnit이 연산이 적용된  오브젝트 Size
        /// </summary>
        public Vector3 ObjectSizeTransformCurrentVector3 => ObjectSizeCurrentVector3.Multiply(CurrentSnapSetting.GridUnitCurrent);



        /// <summary>
        /// 오브젝트의 평면 크기 얻기
        /// <para>Swizzle 되기 전의 깊이Z축을 0인 상태로 반환한다</para>
        /// 오브젝트 평면 크기 (Transform, Current), Swizzle 연산과 GridUnit이 연산이 적용된  오브젝트 Size를 반환하지만, <see cref="objectSizeZ_Depth"/>는 사용하지 않고 0으로 연산한다
        /// </summary>
        internal Vector3 ObjectFlatSizeTransformCurrent
        {
            get
            {
                return new Vector3(objectSizeX_Width, objectSizeY_Height, 0).SwizzlesVector(CurrentSnapSetting.Swizzle).Multiply(CurrentSnapSetting.GridUnitCurrent);
            }
        }



        [TitleGroup("오브젝트 설정")]
        [BoxGroup("오브젝트 설정/Size", true)]
        [LabelText("오브젝트 넓이")]
        [PropertyTooltip("너비 x 높이")]
        [ShowInInspector, EnableGUI, DisplayAsString]
        [PropertyOrder(2)]
        ///<summary>
        /// 오브젝트 넓이 (너비 x 높이)
        /// </summary>
        public int ObjectSizeArea => objectSizeX_Width * objectSizeY_Height;

        [TitleGroup("오브젝트 설정")]
        [BoxGroup("오브젝트 설정/Size", true)]
        [LabelText("오브젝트 넓이 (Transform)")]
        [PropertyTooltip("너비 x 높이 (GridUnit 연산)")]
        [ShowInInspector, EnableGUI, DisplayAsString]
        [PropertyOrder(2)]
        ///<summary>
        /// 오브젝트 넓이 (Transform, 너비 x 높이) GridUnit 연산이 적용된
        /// </summary>
        public int ObjectSizeAreaTransform => ObjectSizeTransformX_Width * ObjectSizeTransformY_Height;



        [TitleGroup("오브젝트 설정")]
        [BoxGroup("오브젝트 설정/Size", true)]
        [LabelText("오브젝트 부피")]
        [PropertyTooltip("너비 x 높이 x 깊이")]
        [ShowInInspector, EnableGUI, DisplayAsString]
        [PropertyOrder(2)]
        ///<summary>
        /// 오브젝트 부피 (너비 x 높이 x 깊이)
        /// </summary>
        public float ObjectSizeShape => objectSizeX_Width * objectSizeY_Height * objectSizeZ_Depth;

        [TitleGroup("오브젝트 설정")]
        [BoxGroup("오브젝트 설정/Size", true)]
        [LabelText("오브젝트 부피 (Transform)")]
        [PropertyTooltip("너비 x 높이 x 깊이")]
        [ShowInInspector, EnableGUI, DisplayAsString]
        [PropertyOrder(2)]
        ///<summary>
        /// 오브젝트 부피 (Transform, 너비 x 높이 x 깊이) GridUnit 연산이 적용된
        /// </summary>
        public float ObjectSizeShapeTransform => ObjectSizeTransformX_Width * ObjectSizeTransformY_Height * ObjectSizeTransformZ_Depth;



        /// <summary>
        /// 그리드 Rect 얻기 (트랜스폼 좌표와, 보정 그리드 좌표를 통해 연산)
        /// </summary>
        /// <param name="transformPositionV2"></param>
        /// <param name="gridPositionCorrectionPlus">보정 그리드 좌표</param>
        public Rect GetObjectGridRect(Vector2 transformPositionV2, Vector2Int gridPositionCorrectionPlus)
        {
            //. 좌표에 그리드 단위 만큼 나눠, 보정된 좌표를 기준으로 계산해야함
            //.     그래야 그리드 단위가 1 초과일때, 어긋나지 않음
            return SU_TF_Rect.RectFromCenter(transformPositionV2.Divide(CurrentSnapSetting.GridUnitOriginalVector2) + gridPositionCorrectionPlus, ObjectSizeOriginalVector2);
        }



        /// <summary>
        /// 트랜스폼 Rect 얻기 (트랜스폼 좌표를 통해 연산)
        /// </summary>
        /// <param name="transformPositionV2"></param>
        public Rect GetTransformObjectRect(Vector2 transformPositionV2)
        {
            return SU_TF_Rect.RectFromCenter(transformPositionV2, ObjectSizeTransformVector2);
        }

        #endregion



        ///======================================================================================================================================================



        //? 오브젝트 설정: Depth-Z 보정



        #region Depth-Z 보정

        [TitleGroup("오브젝트 설정")]
        [BoxGroup("오브젝트 설정/Depth-Z 보정", true)]
        [LabelText("깊이 Offset")]
        [DisplayAsString]
        [ShowInInspector]
        [PropertyOrder(3)]
        public float ObjectDepthZ => CurrentSnapSetting.FloorStandardPositionLength;

        public float ObjectTransformDepthZ => ObjectDepthZ * CurrentSnapSetting.GridUnitZ_Depth;




        [TitleGroup("오브젝트 설정")]
        [BoxGroup("오브젝트 설정/Depth-Z 보정", true)]
        [LabelText("오브젝트 Depth-Z (Current)")]
        [PropertyTooltip("Swizzle 연산이 적용된 오브젝트 Depth-Z")]
        [ShowInInspector, EnableGUI, DisplayAsString]
        [PropertyOrder(3)]
        ///<summary>
        /// 오브젝트 Depth-Z (Current), Swizzle 연산이 적용된
        /// </summary>
        public Vector3 ObjectDepthZCurrent
        {
            get
            {
                return new Vector3(0, 0, ObjectDepthZ).SwizzlesVector(CurrentSnapSetting.Swizzle);
            }
        }



        [TitleGroup("오브젝트 설정")]
        [BoxGroup("오브젝트 설정/Depth-Z 보정", true)]
        [LabelText("오브젝트 Depth-Z (Transform, Current)")]
        [PropertyTooltip("Swizzle 연산과 GridUnit 연산이 적용된 오브젝트 Depth-Z")]
        [ShowInInspector, EnableGUI, DisplayAsString]
        [PropertyOrder(3)]
        ///<summary>
        /// 오브젝트 Depth-Z (Trasnform Current), Swizzle 연산과  GridUnit 연산이 적용된
        /// </summary>
        public Vector3 ObjectDepthZTransformCurrent
        {
            get
            {
                return new Vector3(0, 0, ObjectTransformDepthZ).SwizzlesVector(CurrentSnapSetting.Swizzle);
            }
        }

        #endregion



        ///======================================================================================================================================================



        //! (폐기됨) 그리드 보정 : 그리드 위치 보정



        #region (폐기) 그리드 위치 보정

        //[TitleGroup("그리드 보정")]
        //[BoxGroup("그리드 보정/그리드 위치 보정", true)]
        //[LabelText("그리드 위치 보정 사용")]
        //[PropertyTooltip("사용할경우, 보정한 값 만큼 위치 자체가 이동한다")]
        //[OnValueChanged(nameof(ApplyGridPositionCorrection))]
        //[SerializeField]
        //[PropertyOrder(4)]
        //public bool UseGridPositionCorrection;



        //[TitleGroup("그리드 보정")]
        //[BoxGroup("그리드 보정/그리드 위치 보정", true)]
        //[HorizontalGroup("그리드 보정/그리드 위치 보정/그리드위치보정가로")]
        //[LabelText("X축 보정")]
        //[ShowIf(nameof(UseGridPositionCorrection))]
        //[OnValueChanged(nameof(ApplyGridPositionCorrection))]
        //[SerializeField]
        //[PropertyOrder(5)]
        //private int gridPositionCorrectionX;
        //public int GridPositionCorrectionX => gridPositionCorrectionX;


        //[TitleGroup("그리드 보정")]
        //[BoxGroup("그리드 보정/그리드 위치 보정", true)]
        //[HorizontalGroup("그리드 보정/그리드 위치 보정/그리드위치보정가로")]
        //[LabelText("Y축 보정")]
        //[ShowIf(nameof(UseGridPositionCorrection))]
        //[OnValueChanged(nameof(ApplyGridPositionCorrection))]
        //[SerializeField]
        //[PropertyOrder(6)]
        //private int gridPositionCorrectionY;
        //public int GridPositionCorrectionY => gridPositionCorrectionY;



        /////<summary>
        ///// 그리드 위치 보정 (Transform, Current) Swizzle 연산과 GridUnit 연산이 적용된 그리드 위치 보정
        ///// </summary>
        //[TitleGroup("그리드 보정")]
        //[BoxGroup("그리드 보정/그리드 위치 보정", true)]
        //[LabelText("그리드 위치 보정 (Transform)")]
        //[PropertyTooltip("GridUnit 연산이 적용된 그리드 위치 보정")]
        //[ShowIf(nameof(UseGridPositionCorrection))]
        //[ShowInInspector]
        //[PropertyOrder(7)]
        //public Vector2 GridPositionCorrectionTransform
        //{
        //    get
        //    {
        //        return new Vector2(
        //            gridPositionCorrectionX * CurrentSnapSetting.GridUnitX_Width,
        //            gridPositionCorrectionY * CurrentSnapSetting.GridUnitY_Height);
        //    }
        //}



        /////<summary>
        ///// 그리드 위치 보정 (Transform, Current) Swizzle 연산과 GridUnit 연산이 적용된 그리드 위치 보정
        ///// </summary>
        //[TitleGroup("그리드 보정")]
        //[BoxGroup("그리드 보정/그리드 위치 보정", true)]
        //[LabelText("그리드 위치 보정 (Transform, Current)")]
        //[PropertyTooltip("Swizzle 연산과 GridUnit 연산이 적용된 그리드 위치 보정")]
        //[ShowIf(nameof(UseGridPositionCorrection))]
        //[ShowInInspector]
        //[PropertyOrder(8)]
        //public Vector3Int GridPositionCorrectionTransformCurrent
        //{
        //    get
        //    {
        //        return new Vector3Int(
        //            gridPositionCorrectionX * CurrentSnapSetting.GridUnitX_Width,
        //            gridPositionCorrectionY * CurrentSnapSetting.GridUnitY_Height,
        //            0).SwizzlesVectorInt(CurrentSnapSetting.Swizzle);
        //    }
        //}



        ////? 그리드보정 적용
        //private void ApplyGridPositionCorrection()
        //{
        //    if (!UseGridPositionCorrection)
        //    {
        //        gridPositionCorrectionX = 0;
        //        gridPositionCorrectionY = 0;
        //    }
        //}

        #endregion



        ///======================================================================================================================================================



        //? 그리드 연산



        #region 그리드 스냅 연산 (반드시 그리드 셀 면 중심을 얻는다)



        #region Internal

        private void CalculateGridSnapPositionInternal(ref Vector2 gridPosition)
        {
            if (CurrentSnapSetting.SnapToGridCellCenter)
            {
                gridPosition.x -= CurrentSnapSetting.GridUnitX_Width * 0.5f;
                gridPosition.y -= CurrentSnapSetting.GridUnitY_Height * 0.5f;
            }
            else
            {
                gridPosition.x += CurrentSnapSetting.GridUnitX_Width * 0.5f;
                gridPosition.y += CurrentSnapSetting.GridUnitY_Height * 0.5f;
            }
            //gridPosition += CurrentSnapSetting.GridPositionCorrection;
        }



        /// <summary>
        /// <b>트랜스폼 좌표</b>를 받아와, <c>그리드 스냅 중심 트랜스폼 좌표 V2</c> 를 연산한다
        /// </summary>
        /// <param name="transformPosition">대상 트랜스폼 좌표</param>
        private Vector2 CalculateGridSnapPosition_byTransformPositionV3(Vector3 transformPosition)
        {
            var gridPosition = CurrentSnapSetting.CalculateGridSnapPosition_byTransformPositionV3(transformPosition);
            CalculateGridSnapPositionInternal(ref gridPosition);
            return gridPosition;
        }



        /// <summary>
        /// <b>트랜스폼 좌표 V2</b>를 받아와, <c>그리드 스냅 중심 트랜스폼 좌표 V2</c> 를 연산한다
        /// </summary>
        /// <param name="transformPositionV2">대상 트랜스폼 좌표</param>
        private Vector2 CalculateGridSnapPosition_byTransformPositionV2(Vector2 transformPositionV2)
        {
            var gridPosition = CurrentSnapSetting.CalculateGridSnapPosition_byTransformPositionV2(transformPositionV2);
            CalculateGridSnapPositionInternal(ref gridPosition);
            return gridPosition;
        }

        #endregion



        /// <summary>
        /// <b>트랜스폼 좌표</b>를 받아와, <c>그리드 스냅 좌표</c>를 연산한다
        /// <para>반드시 그리드 셀의 <b>면</b>의 중심 좌표를 얻는다 (스냅하여 얻는다)</para>
        /// </summary>
        /// <param name="transformPosition">대상 트랜스폼 좌표</param>
        public Vector3 GridSnapTransformPosition(Vector3 transformPosition, bool useSwizzle = true)
        {
            var gridPosition = CalculateGridSnapPosition_byTransformPositionV3(transformPosition);

            return useSwizzle ? gridPosition.SwizzlesVector2To3(CurrentSnapSetting.Swizzle) : gridPosition;
        }



        /// <summary>
        /// <b>트랜스폼 좌표 V2</b>를 받아와, <c>그리드 스냅 좌표</c>를 연산한다
        /// <para>반드시 그리드 셀의 <b>면</b>의 중심 좌표를 얻는다 (스냅하여 얻는다)</para>
        /// </summary>
        /// <param name="transformPositionV2">대상 트랜스폼 좌표</param>
        public Vector3 GridSnapTransformPosition(Vector2 transformPositionV2, bool useSwizzle = true)
        {
            var gridPosition = CalculateGridSnapPosition_byTransformPositionV2(transformPositionV2);

            return useSwizzle ? gridPosition.SwizzlesVector2To3(CurrentSnapSetting.Swizzle) : gridPosition;
        }



        /// <summary>
        /// 받아온 트랜스폼 좌표를 통해, 이 <see cref="GridCompatible2"/>의 오브젝트 크기가아닌, 별도의 크기(<paramref name="customSize"/>) 를 받아와
        /// <para>해당 객체를 스냅하고, 중심 좌표를 연산하기에, 반환되는 좌표는 그리드 셀의 <b>면</b> 또는 <b>선</b>이며</para>
        /// <para>그리드 셀의 <b>면</b>을 무조건 보장하지 않기에, 이 좌표를 기준으로 별도의 크기를 가진 그리드 오브젝트를 그릴때, 해당 그리드 오브젝트의 중심점을 알아내기 위해 사용된다</para>
        /// </summary>
        /// <param name="transformPosition">대상 좌표</param>
        /// <param name="customSize">커스텀 크기</param>
        /// <param name="useSwizzle">반환시 Swizzle 한 뒤 반환</param>
        /// <returns></returns>
        public Vector3 GridSnapTransformPosition_CustomSize(Vector3 transformPosition, Vector2Int customSize, bool useSwizzle)
        {
            var snapPosition = GridSnapTransformPosition(transformPosition, false);

            if (customSize.x.IsEven()) { snapPosition.x += CurrentSnapSetting.GridUnitX_Width * 0.5f; }
            if (customSize.y.IsEven()) { snapPosition.y += CurrentSnapSetting.GridUnitY_Height * 0.5f; }

            return useSwizzle ? snapPosition.SwizzlesVector(CurrentSnapSetting.Swizzle) : snapPosition;
        }



        #endregion



        #region 그리드 중심 연산 (그리드 선 또는 면의 중심 좌표를 스냅하지 않고 얻는다)



        /// <summary>
        /// <b>트랜스폼 좌표</b>를 받아와, <c>그리드 중심 좌표</c>를 연산한다
        /// <para>반환되는 좌표는 그리드 셀의 <b>면</b> 또는 <b>선</b>이며, 이는 그리드 단위의 홀짝, 중심 스냅, 오브젝트 크기의 홀짝 여부에 따라 변화된다</para>
        /// <para>그리드 셀의 <b>면</b>을 무조건 보장하지 않기에, 이 좌표를 기준으로 그리드 오브젝트를 그릴때, 해당 그리드 오브젝트의 중심점을 알아내기 위해 사용된다</para>
        /// </summary>
        /// <param name="transformPosition">대상 트랜스폼 좌표</param>
        public Vector3 GridCenterTransformPosition(Vector3 transformPosition, bool useSwizzle = true)
        {
            //. 스냅 좌표를 받고, 오브젝트 크기에 따라 좌표를 조정해 중심 좌표를 연산한다
            var gridSnapPosition = CalculateGridSnapPosition_byTransformPositionV3(transformPosition);

            if (objectSizeX_Width.IsEven()) { gridSnapPosition.x -= CurrentSnapSetting.GridUnitX_Width * 0.5f; }
            if (objectSizeY_Height.IsEven()) { gridSnapPosition.y -= CurrentSnapSetting.GridUnitY_Height * 0.5f; }

            return useSwizzle ? gridSnapPosition.SwizzlesVector2To3(CurrentSnapSetting.Swizzle) : gridSnapPosition;
        }



        /// <summary>
        /// <b>트랜스폼 좌표</b>를 받아와, <c>바닥 그리드 중심 좌표</c>를 연산한다
        /// <para>반환되는 좌표는 그리드 셀의 <b>면</b> 또는 <b>선</b>이며, 이는 그리드 단위의 홀짝, 중심 스냅, 오브젝트 크기의 홀짝 여부에 따라 변화된다</para>
        /// <para>그리드 셀의 <b>면</b>을 무조건 보장하지 않기에, 이 좌표를 기준으로 그리드 오브젝트를 그릴때, 해당 그리드 오브젝트의 중심점을 알아내기 위해 사용된다</para>
        /// </summary>
        /// <param name="transformPosition">대상 트랜스폼 좌표</param>
        public Vector3 GridFloorCenterTransformPosition(Vector3 transformPosition, bool useSwizzle = true)
        {
            var gridCenterTransformPosition = GridCenterTransformPosition(transformPosition, false);
            gridCenterTransformPosition.z += ObjectTransformDepthZ;
            return useSwizzle ? gridCenterTransformPosition.SwizzlesVector(CurrentSnapSetting.Swizzle) : gridCenterTransformPosition;
        }

        /// <summary>
        /// <b>트랜스폼 좌표</b>를 받아와, <c>중앙 그리드 중심 좌표</c>를 연산한다
        /// <para>반환되는 좌표는 그리드 셀의 <b>면</b> 또는 <b>선</b>이며, 이는 그리드 단위의 홀짝, 중심 스냅, 오브젝트 크기의 홀짝 여부에 따라 변화된다</para>
        /// <para>그리드 셀의 <b>면</b>을 무조건 보장하지 않기에, 이 좌표를 기준으로 그리드 오브젝트를 그릴때, 해당 그리드 오브젝트의 중심점을 알아내기 위해 사용된다</para>
        /// </summary>
        /// <param name="transformPosition">대상 트랜스폼 좌표</param>
        public Vector3 GridMiddleCenterTransformPosition(Vector3 transformPosition, bool useSwizzle = true)
        {
            var gridFloorCenterTransformPosition = GridFloorCenterTransformPosition(transformPosition, false);
            gridFloorCenterTransformPosition.z += ObjectSizeTransformZ_Depth * 0.5f;
            return useSwizzle ? gridFloorCenterTransformPosition.SwizzlesVector(CurrentSnapSetting.Swizzle) : gridFloorCenterTransformPosition;
        }

        /// <summary>
        /// <b>트랜스폼 좌표</b>를 받아와, <c>천장 그리드 중심 좌표</c>를 연산한다
        /// <para>반환되는 좌표는 그리드 셀의 <b>면</b> 또는 <b>선</b>이며, 이는 그리드 단위의 홀짝, 중심 스냅, 오브젝트 크기의 홀짝 여부에 따라 변화된다</para>
        /// <para>그리드 셀의 <b>면</b>을 무조건 보장하지 않기에, 이 좌표를 기준으로 그리드 오브젝트를 그릴때, 해당 그리드 오브젝트의 중심점을 알아내기 위해 사용된다</para>
        /// </summary>
        /// <param name="transformPosition">대상 트랜스폼 좌표</param>
        public Vector3 GridCeilingCenterTransformPosition(Vector3 transformPosition, bool useSwizzle = true)
        {
            var gridCenterTransformPosition = GridCenterTransformPosition(transformPosition, false);
            gridCenterTransformPosition.z += ObjectTransformDepthZ + ObjectSizeTransformZ_Depth;
            return useSwizzle ? gridCenterTransformPosition.SwizzlesVector(CurrentSnapSetting.Swizzle) : gridCenterTransformPosition;
        }



        #endregion



        #region 그리드 좌표 연산



        private void GridPositionInternal(Vector2 gridSnapTransformPosition, out Vector2Int gridPosition)
        {
            if (CurrentSnapSetting.SnapToGridCellCenter)
            {
                gridPosition = new Vector2Int(Mathf.CeilToInt(gridSnapTransformPosition.x / CurrentSnapSetting.GridUnitX_Width), Mathf.CeilToInt(gridSnapTransformPosition.y / CurrentSnapSetting.GridUnitY_Height));
            }
            else
            {
                gridPosition = new Vector2Int(Mathf.FloorToInt(gridSnapTransformPosition.x / CurrentSnapSetting.GridUnitX_Width), Mathf.FloorToInt(gridSnapTransformPosition.y / CurrentSnapSetting.GridUnitY_Height));
            }

            //gridPosition += CurrentSnapSetting.GridPositionCorrection; //. 스냅의 그리드 좌표 보정 사용 (+)
        }

        /// <summary>
        ///<b>트랜스폼 좌표</b>를 받아와, <c>그리드 좌표</c>를 연산한다
        /// </summary>
        /// <param name="transformPositionV3">대상 트랜스폼 좌표</param>
        public Vector2Int GridPosition(Vector3 transformPositionV3)
        {
            GridPositionInternal(CalculateGridSnapPosition_byTransformPositionV3(transformPositionV3), out var gridPosition);
            return gridPosition;
        }

        /// <summary>
        ///<b>트랜스폼 좌표 V2</b>를 받아와, <c>그리드 좌표</c>를 연산한다
        /// </summary>
        /// <param name="transformPositionV2">대상 트랜스폼 좌표</param>
        public Vector2Int GridPosition(Vector2 transformPositionV2)
        {
            GridPositionInternal(CalculateGridSnapPosition_byTransformPositionV2(transformPositionV2), out var gridPosition);
            return gridPosition;
        }



        /// <summary>
        /// 기준 위치에 맞추어 그리드 좌표를 반환
        /// </summary>
        public Vector2Int GetGridPositionStandard(Vector2Int gridPosition, ECenterStandard centerStandard)
        {
            //. 절반 크기 캐시
            int halfX = objectSizeX_Width / 2;
            int halfY = objectSizeY_Height / 2;

            //. X축 오프셋 계산
            int offsetX = centerStandard switch
            {
                ECenterStandard.MiddleLeft or ECenterStandard.LowerLeft or ECenterStandard.UpperLeft
                    => -halfX,
                ECenterStandard.MiddleRight or ECenterStandard.LowerRight or ECenterStandard.UpperRight
                          => objectSizeX_Width.IsEven() ? halfX - 1 : halfX,
                _ => 0
            };

            //. Y축 오프셋 계산
            int offsetY = centerStandard switch
            {
                ECenterStandard.LowerCenter or ECenterStandard.LowerLeft or ECenterStandard.LowerRight
                    => -halfY,
                ECenterStandard.UpperCenter or ECenterStandard.UpperLeft or ECenterStandard.UpperRight
                    => objectSizeY_Height.IsEven() ? halfY - 1 : halfY,
                _ => 0
            };


            return gridPosition + new Vector2Int(offsetX, offsetY);
        }

        /// <summary>
        /// <b>그리드 좌표</b>를 받아와,<c>트랜스폼 좌표</c>를 연산한다
        /// <para>해당 그리드의 스냅된 정 중앙인 트랜스폼 좌표로 변환한다</para>
        /// <para>그리드 호환 내의 특정 그리드 좌표의 트랜스폼 좌표를 변환해 얻고 싶을때 사용한다</para>
        /// </summary>
        /// <param name="gridPosition"></param>
        /// <returns></returns>
        public Vector3 Calculate_GridPosition_To_TransformPosition(Vector2Int gridPosition, bool useSwizzle)
        {
            //gridPosition -= CurrentSnapSetting.GridPositionCorrection; //. 스냅의 그리드 좌표 보정 사용 (-)

            var transformPosition = CalculateGridSnapPosition_byTransformPositionV3(new Vector3(
                 gridPosition.x * CurrentSnapSetting.GridUnitX_Width,
                 gridPosition.y * CurrentSnapSetting.GridUnitY_Height,
                 0).SwizzlesVector(CurrentSnapSetting.Swizzle));
            return useSwizzle ? transformPosition.SwizzlesVector2To3(CurrentSnapSetting.Swizzle) : transformPosition;
        }

        /// <summary>
        /// 기준 위치의 그리드 좌표 의 트랜스폼 좌표
        /// </summary>
        /// <param name="standard"></param>
        /// <returns></returns>
        public Vector3 GridPositionStandardTransformPosition(Vector2Int gridPosition, ECenterStandard standard, bool useSwizzle)
            => Calculate_GridPosition_To_TransformPosition(gridPosition, useSwizzle);



        #endregion



        ///======================================================================================================================================================



        public void Copy(GridCompatible2 original)
        {
            //! 원본이 null이라면 복사를 진행할 수 없습니다
            if (original == null) { return; }


            //. Snap 설정 복사
            SnapSettingInternal.Copy(original.SnapSettingInternal);       //? 값만 깊은 복사
            SnapSettingExternal = original.SnapSettingExternal;           //? SO 참조는 얕은 복사


            //. 오브젝트 크기 복사
            objectSizeX_Width = original.objectSizeX_Width;
            objectSizeY_Height = original.objectSizeY_Height;
            objectSizeZ_Depth = original.objectSizeZ_Depth;


            //. Depth-Z 보정 복사
            //ObjectDepthZ = original.ObjectDepthZ;


            //. 그리드 위치 보정 복사
            //UseGridPositionCorrection = original.UseGridPositionCorrection;
            //gridPositionCorrectionX = original.gridPositionCorrectionX;
            //gridPositionCorrectionY = original.gridPositionCorrectionY;


            //. 우선순위 플래그 복사
            //PreferUpperRightGridCell = original.PreferUpperRightGridCell;


            //? 파생 값 재계산
            ValueChangedSnapSetting();
        }



        ///======================================================================================================================================================
    }
}
