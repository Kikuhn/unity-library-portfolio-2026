using Pan.Util;
using Sirenix.OdinInspector;
using UnityEngine;



namespace Pan.GridCompatibles2
{
    [ExecuteAlways]
    public partial class GridCompatible2Object : MonoBehaviour, IObjectCaching
    {
        ///======================================================================================================================================================



        //? 에디터 더미



#if UNITY_EDITOR



        private const int PROPERTY_ORDER = -10000;



        [Title("그리드 호환 오브젝트")]
        [LabelText("그리드 호환 싱글톤 SO")]
        [ShowInInspector]
        [InlineEditor]
        [EnableGUI]
        [PropertyOrder(0 + PROPERTY_ORDER)]
        private GridCompatible2SingletonSettingSbject SingleTon => GridCompatible2SingletonSettingSbject.O;


        [FoldoutGroup("그리드 호환 오브젝트 설정"), TitleGroup("그리드 호환 오브젝트 설정/요약")]
        [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Center, EnableRichText = true), EnableGUI]
        [PropertyOrder(1 + PROPERTY_ORDER)]
        [PropertySpace(8, 0)]
        private string dummy_GridPositionText
        {
            get
            {
                var gridPosition = GridPosition;
                return $"<b><size=14>현재 그리드 좌표: [<color=#2ecc71><b>{gridPosition.x}</b></color>, <color=#2ecc71><b>{gridPosition.y}</b></color>]</size></b>";
            }
        }

        [FoldoutGroup("그리드 호환 오브젝트 설정"), TitleGroup("그리드 호환 오브젝트 설정/요약")]
        [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Center, EnableRichText = true), EnableGUI]
        [PropertyOrder(1 + PROPERTY_ORDER)]
        [PropertySpace(8, 0)]
        private string dummy_GridPositionText2
        {
            get
            {
                if (GridPositionCorrectionPlus == Vector2Int.zero) { return ""; }

                var beforeGridPosition = GridCompatible.GridPosition(transform.position);

                string xColor = GridPositionCorrectionPlus.x < 0 ? "#ed5565" : "#4fc1e9";
                string yColor = GridPositionCorrectionPlus.y < 0 ? "#ed5565" : "#4fc1e9";

                return $"<b><size=12> 이 오브젝트의 그리드 좌표 추가 보정 [<color=#f7da64>{beforeGridPosition.x}</color>, <color=#f7da64>{beforeGridPosition.y}</color>] + " +
                       $"(<color={xColor}>{(GridPositionCorrectionPlus.x >= 0 ? "+" : "")}{GridPositionCorrectionPlus.x}</color>, " +
                       $"<color={yColor}>{(GridPositionCorrectionPlus.y >= 0 ? "+" : "")}{GridPositionCorrectionPlus.y}</color>)</b></size>";
            }
        }
        private Color GridPositionCorrectionPlusColor
        {
            get
            {
                if (GridPositionCorrectionPlus == Vector2Int.zero)
                {
                    return Color.white;
                }
                else
                {
                    return new Color(0.93f, 0.33f, 0.40f, 1f);
                }
            }
        }

#endif



        ///======================================================================================================================================================



        private Vector3 TransformPositionInternal => OC == null ? transform.position : OC.Position;



        ///======================================================================================================================================================



        //? 그리드 좌표



        #region 그리드 좌표

        [ShowInInspector]
        [FoldoutGroup("그리드 호환 오브젝트 설정"), TitleGroup("그리드 호환 오브젝트 설정/그리드 좌표")]
        [LabelText("그리드 좌표 보정 Plus")]
        [PropertyTooltip("Snap 설정에 있는 그리드 좌표 보정에 더해 이 오브젝트에만 추가로 사용되는 보정, 이 값만큼 그리드 좌표가 더해 보정된다\n그리드 좌표에만 적용되며 트랜스폼 좌표에는 영향을 주지 않는다")]
#if UNITY_EDITOR
        [PropertyOrder(2 + PROPERTY_ORDER)]
        [GUIColor(nameof(GridPositionCorrectionPlusColor))]
#endif
        public Vector2Int GridPositionCorrectionPlus;

        [FoldoutGroup("그리드 호환 오브젝트 설정"), TitleGroup("그리드 호환 오브젝트 설정/그리드 좌표")]
        [LabelText("현재 그리드 좌표")]
        [ShowInInspector, EnableGUI, DisplayAsString]
#if UNITY_EDITOR
        [PropertyOrder(3 + PROPERTY_ORDER)]
#endif
        public Vector2Int GridPosition =>
            GridCompatible.GridPosition(transform.position) + GridPositionCorrectionPlus;

        [FoldoutGroup("그리드 호환 오브젝트 설정"), TitleGroup("그리드 호환 오브젝트 설정/그리드 좌표")]
        [LabelText("현재 그리드 좌표 (Current)")]
        [ShowInInspector, EnableGUI, DisplayAsString]
#if UNITY_EDITOR
        [PropertyOrder(3 + PROPERTY_ORDER)]
#endif
        public Vector2Int GridPositionCurrent =>
            (GridCompatible.GridPosition(transform.position) + GridPositionCorrectionPlus).Multiply(GridCompatible.CurrentSnapSetting.GridUnitOriginalVector2);

        #endregion



        ///======================================================================================================================================================



        //? 그리드 스냅 좌표



        #region 그리드 스냅 좌표

        [FoldoutGroup("그리드 호환 오브젝트 설정"), TitleGroup("그리드 호환 오브젝트 설정/그리드 트랜스폼 좌표")]
        [LabelText("그리드 스냅 좌표 (Transform)")]
        [ShowInInspector, EnableGUI, DisplayAsString]
#if UNITY_EDITOR
        [PropertyOrder(4 + PROPERTY_ORDER)]
#endif
        public Vector2 GridSnapTransformPositionVector2 =>
    GridCompatible.GridSnapTransformPosition(TransformPositionInternal, false);

        [FoldoutGroup("그리드 호환 오브젝트 설정"), TitleGroup("그리드 호환 오브젝트 설정/그리드 트랜스폼 좌표")]
        [LabelText("그리드 스냅 좌표 (Transform, Current)")]
        [ShowInInspector, EnableGUI, DisplayAsString]
#if UNITY_EDITOR
        [PropertyOrder(4 + PROPERTY_ORDER)]
#endif
        public Vector3 GridSnapTransformCurrentPosition =>
            GridCompatible.GridSnapTransformPosition(TransformPositionInternal, true);

        #endregion



        ///======================================================================================================================================================



        //? 그리드 중심 좌표



        #region 그리드 중심 좌표

        [FoldoutGroup("그리드 호환 오브젝트 설정"), TitleGroup("그리드 호환 오브젝트 설정/그리드 트랜스폼 좌표")]
        [LabelText("그리드 중심 좌표 (Transform)")]
        [ShowInInspector, EnableGUI, DisplayAsString]
#if UNITY_EDITOR
        [PropertyOrder(5 + PROPERTY_ORDER)]
#endif
        public Vector2 GridCenterTransformPositionVector2 =>
          GridCompatible.GridCenterTransformPosition(TransformPositionInternal, false);


        [FoldoutGroup("그리드 호환 오브젝트 설정"), TitleGroup("그리드 호환 오브젝트 설정/그리드 트랜스폼 좌표")]
        [LabelText("그리드 중심 좌표 (Transform, Current)")]
        [ShowInInspector, EnableGUI, DisplayAsString]
#if UNITY_EDITOR
        [PropertyOrder(5 + PROPERTY_ORDER)]
#endif
        public Vector3 GridCenterTransformCurrentPosition =>
            GridCompatible.GridCenterTransformPosition(TransformPositionInternal, true);

        [FoldoutGroup("그리드 호환 오브젝트 설정"), TitleGroup("그리드 호환 오브젝트 설정/그리드 트랜스폼 좌표")]
        [LabelText("그리드 중심 바닥 좌표 (Transform, Current)")]
        [ShowInInspector, EnableGUI, DisplayAsString]
#if UNITY_EDITOR
        [PropertyOrder(5 + PROPERTY_ORDER)]
#endif
        public Vector3 GridFloorCenterTransformCurrentPosition =>
            GridCompatible.GridFloorCenterTransformPosition(TransformPositionInternal, true);

        [FoldoutGroup("그리드 호환 오브젝트 설정"), TitleGroup("그리드 호환 오브젝트 설정/그리드 트랜스폼 좌표")]
        [LabelText("그리드 중심 중앙 좌표 (Transform, Current)")]
        [ShowInInspector, EnableGUI, DisplayAsString]
#if UNITY_EDITOR
        [PropertyOrder(5 + PROPERTY_ORDER)]
#endif
        public Vector3 GridMiddleCenterTransformCurrentPosition =>
            GridCompatible.GridMiddleCenterTransformPosition(TransformPositionInternal, true);

        [FoldoutGroup("그리드 호환 오브젝트 설정"), TitleGroup("그리드 호환 오브젝트 설정/그리드 트랜스폼 좌표")]
        [LabelText("그리드 천장 중심 좌표 (Transform, Current)")]
        [ShowInInspector, EnableGUI, DisplayAsString]
#if UNITY_EDITOR
        [PropertyOrder(5 + PROPERTY_ORDER)]
#endif
        public Vector3 GridCeilingCenterTransformCurrentPosition =>
            GridCompatible.GridCeilingCenterTransformPosition(TransformPositionInternal, true);



        /// <summary>
        /// 기준 위치의 그리드 좌표
        /// </summary>
        /// <param name="standard"></param>
        /// <returns></returns>
        public Vector2Int GridPositionStandard(ECenterStandard standard) =>
            GridCompatible.GetGridPositionStandard(GridPosition, standard);

        /// <summary>
        /// 기준 위치의 그리드 좌표 의 트랜스폼 좌표
        /// </summary>
        /// <param name="standard"></param>
        /// <returns></returns>
        public Vector3 GridPositionStandardTransformPosition(ECenterStandard standard, bool useSwizzle) =>
            GridCompatible.GridPositionStandardTransformPosition(GridPositionStandard(standard) - GridPositionCorrectionPlus, standard, useSwizzle);



        #endregion



        ///======================================================================================================================================================



        [FoldoutGroup("그리드 호환 오브젝트 설정"), TitleGroup("그리드 호환 오브젝트 설정/그리드 트랜스폼 좌표")]
        [LabelText("그리드 Rect")]
        [ShowInInspector, EnableGUI, DisplayAsString]
#if UNITY_EDITOR
        [PropertyOrder(5 + PROPERTY_ORDER)]
#endif
        public Rect GridRect =>
            GridCompatible.GetObjectGridRect(GridCenterTransformPositionVector2, GridPositionCorrectionPlus);


        #region 캐시

        [FoldoutGroup("그리드 호환 오브젝트 설정"), TitleGroup("그리드 호환 오브젝트 설정/그리드 트랜스폼 좌표")]
        [LabelText("(캐시)그리드 Rect")]
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly, DisplayAsString]
        [Indent(1)]
#if UNITY_EDITOR
        [PropertyOrder(5 + PROPERTY_ORDER)]
#endif
        public Rect? GridRectCache => gridRectCache;
        [SerializeField, HideInInspector] private Rect? gridRectCache;

        public void Caching_GridRectCache()
        {
            gridRectCache = GridRect;
        }

        #endregion



        [FoldoutGroup("그리드 호환 오브젝트 설정"), TitleGroup("그리드 호환 오브젝트 설정/그리드 트랜스폼 좌표")]
        [LabelText("트랜스폼 Rect")]
        [ShowInInspector, EnableGUI, DisplayAsString]
#if UNITY_EDITOR
        [PropertyOrder(5 + PROPERTY_ORDER)]
#endif
        public Rect ObjectTransformRect =>
            GridCompatible.GetTransformObjectRect(GridCenterTransformPositionVector2);



        ///======================================================================================================================================================



#if UNITY_EDITOR
        [FoldoutGroup("그리드 호환 설정")]
        [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Center, EnableRichText = true), EnableGUI]
        [PropertySpace(8, 8)]
        [PropertyOrder(6 + PROPERTY_ORDER)]
        private string dummy_SnapSettingMessage
        {
            get
            {
                if (!IsUseExternalSetting)
                {
                    return "<color=white><size=14><b><color=#4fc1e9>내장</color></b> 그리드 호환 설정 적용중</size></color>";
                }
                else
                {
                    return "<color=white><size=14><b><color=#ed5565>외장</color></b> 그리드 호환 설정 적용중</size></color>";
                }
            }
        }
#endif



        ///======================================================================================================================================================




        [FoldoutGroup("그리드 호환 설정"), HideLabel, InlineProperty]
        [HideIf(nameof(IsUseExternalSetting))]
        [SerializeField]
#if UNITY_EDITOR
        [PropertyOrder(7 + PROPERTY_ORDER)]
#endif
        private GridCompatible2 GridCompatibleInternal = new();



        [FoldoutGroup("그리드 호환 설정")]
        [InlineEditor]
        [SerializeField]
        [LabelText("💾 외장 그리드 호환 설정")]
#if UNITY_EDITOR
        [PropertyOrder(8 + PROPERTY_ORDER)]
#endif
        private GridCompatible2SettingSbject GridCompatibleSnapSettingSbject;



        public bool IsUseExternalSetting => GridCompatibleSnapSettingSbject != null;



        /// <summary>
        /// 현재 그리드 호환 설정 얻기 (내장 또는 외장)
        /// </summary>
        public GridCompatible2 GridCompatible => IsUseExternalSetting ? GridCompatibleSnapSettingSbject.GridCompatible : GridCompatibleInternal;



        [FoldoutGroup("그리드 호환 설정")]
        [ShowIf(nameof(IsUseExternalSetting))]
        [Button("외장 그리드 호환 설정 제거", Icon = SdfIconType.X, ButtonAlignment = 1f, Stretch = false), GUIColor(0.93f, 0.33f, 0.40f)]
        [Tooltip("외장 그리드 호환 설정을 제거하고 내장 그리드 호환 설정으로 되돌립니다")]
#if UNITY_EDITOR
        [PropertyOrder(9 + PROPERTY_ORDER)]
#endif
        private void RemoveSnapSettingExternal()
        {
            if (GridCompatibleSnapSettingSbject != null)
            {
                //SnapSettingInternal.Copy(SnapSettingExternal.GridCompatibleSnapSetting);
                GridCompatibleInternal.Copy(GridCompatibleSnapSettingSbject.GridCompatible);
                GridCompatibleSnapSettingSbject = null;
            }
        }



        ///======================================================================================================================================================



        public ObjectCaching OC { get; private set; }



        protected virtual void Awake()
        {
            OC = new ObjectCaching(this);
        }



        protected virtual void OnEnable()
        {
            //GridCompatible?.ApplyAutoDepthZ();
        }



        ///======================================================================================================================================================
    }
}
