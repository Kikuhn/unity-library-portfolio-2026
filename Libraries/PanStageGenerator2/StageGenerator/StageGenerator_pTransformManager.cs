using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Pan.Util;
using System;
using System.Text;
using Cysharp.Threading.Tasks;
using Pan.GridCompatibles2;
using Pan.StageGenerators;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Threading;




namespace Pan.StageGenerators
{
	public partial class StageGenerator : MonoBehaviour
	{
		[Serializable]
		public class TransformManager : BaseManager
		{
			///======================================================================================================================================================



			public override void WakeUp(StageGenerator main)
			{
				base.WakeUp(main);

				CachingVectors();
			}



			///======================================================================================================================================================



			#region 스테이지 생성기 부모



			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[BoxGroup("트랜스폼 매니저/박스/스테이지 부모")]
			[ShowInInspector]
			[LabelText("스테이지 부모")]
			[PropertyTooltip("절대 null이 되지 않는다, 비어있다면 이 오브젝트가 할당되며, 별도로 지정 또한 가능하다")]
			[PropertyOrder(10)]
			public Transform StageParent
			{
				get
				{
					if (stageParent == null) { SetParentStageGenerator(); }
					return stageParent;
				}
				set
				{
					stageParent = value;
				}
			}
			[SerializeField, HideInInspector] private Transform stageParent;



			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[BoxGroup("트랜스폼 매니저/박스/스테이지 부모")]
			[ButtonGroup("트랜스폼 매니저/박스/스테이지 부모/버튼그룹")]
			[Button("S.G를 부모로", Icon = SdfIconType.LightningFill, Stretch = false, ButtonAlignment = 1f), GUIColor(0.97f, 0.85f, 0.39f)]
			[PropertyOrder(10)]
			/// <summary>
			/// 스테이지의 부모를 자기 자신 오브젝트로 지정한다
			/// </summary>
			private void SetParentStageGenerator()
			{
				stageParent = Main.transform;
			}



			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[BoxGroup("트랜스폼 매니저/박스/스테이지 부모")]
			[ButtonGroup("트랜스폼 매니저/박스/스테이지 부모/버튼그룹")]
			[Button("부모 활성화/비활성화", Icon = SdfIconType.NintendoSwitch, Stretch = false, ButtonAlignment = 1f)]
			[PropertyOrder(10)]
#if UNITY_EDITOR
			[GUIColor(nameof(editorGUIColor_StageParentActiveColor))]
#endif
			/// <summary>
			/// 스테이지의 부모를 자기 자신 오브젝트로 지정한다
			/// </summary>
			private void ToogleActiveStageParent()
			{
				StageParent.gameObject.SetActive(!StageParent.gameObject.activeSelf);
			}

#if UNITY_EDITOR

			private Color editorGUIColor_StageParentActiveColor
			{
				get
				{
					if (StageParent.gameObject.activeSelf)
					{
						return "ed5565".HexToColor();
					}
					else
					{
						return "4fc1e9".HexToColor();
					}
				}
			}

#endif



			//? 부모 좌표



			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[FoldoutGroup("트랜스폼 매니저/박스/스테이지 부모/좌표 정보 보기")]
			[ShowInInspector, EnableGUI, DisplayAsString]
			[LabelText("부모 좌표 (Original)")]
			[PropertyOrder(10)]
			public Vector3 StageParentPositionOriginal
			{
				get
				{
					return StageParent.position;
				}
			}

			#region 캐시
			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[FoldoutGroup("트랜스폼 매니저/박스/스테이지 부모/좌표 정보 보기")]
			[ShowInInspector, DisplayAsString]
			[LabelText("(캐시) 부모 좌표 (Original)")]
			[PropertyTooltip("병렬 연산, 멀티 스레드에서 유니티 Transform을 호출하지 않기 위해, 생성 전에 이 값을 갱신하고, 연산에 이 값을 사용한다")]
			[PropertyOrder(10)]
			[Indent(1)]
			public Vector3 StageParentPositionOriginalCache => stageParentPositionOriginalCache;
			[SerializeField, HideInInspector] private Vector3 stageParentPositionOriginalCache;
			#endregion



			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[FoldoutGroup("트랜스폼 매니저/박스/스테이지 부모/좌표 정보 보기")]
			[ShowInInspector, EnableGUI, DisplayAsString]
			[LabelText("부모 좌표 (Current)")]
			[PropertyOrder(10)]
			public Vector3 StageParentPositionCurrent
			{
				get
				{

					return StageParent.position.SwizzlesVector(Setting.SnapSetting.Swizzle);
				}
			}

			#region 캐시
			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[FoldoutGroup("트랜스폼 매니저/박스/스테이지 부모/좌표 정보 보기")]
			[ShowInInspector, DisplayAsString]
			[LabelText("(캐시)부모 좌표 (Current)")]
			[PropertyTooltip("병렬 연산, 멀티 스레드에서 유니티 Transform을 호출하지 않기 위해, 생성 전에 이 값을 갱신하고, 연산에 이 값을 사용한다")]
			[PropertyOrder(10)]
			[Indent(1)]
			public Vector3 StageParentPositionCurrentCache => stageParentPositionCurrentCache;
			[SerializeField, HideInInspector] private Vector3 stageParentPositionCurrentCache;
			#endregion



			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[FoldoutGroup("트랜스폼 매니저/박스/스테이지 부모/좌표 정보 보기")]
			[ShowInInspector, EnableGUI, DisplayAsString]
			[LabelText("스냅된 부모 좌표 (Original)")]
			[PropertyOrder(10)]
			public Vector3Int StageParentSnappedPositionOriginal
			{
				get
				{
					var stageParentPosition = StageParent.position;
					return Setting.SnapSetting.SnapToGridUnit(stageParentPosition, true);
					//Setting.SnapSetting.SnapToGridUnit(ref stageParentPosition, true);
					//return stageParentPosition;
				}
			}

			#region 캐시
			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[FoldoutGroup("트랜스폼 매니저/박스/스테이지 부모/좌표 정보 보기")]
			[ShowInInspector, DisplayAsString]
			[LabelText("(캐시) 스냅된 부모 좌표 (Original)")]
			[PropertyTooltip("병렬 연산, 멀티 스레드에서 유니티 Transform을 호출하지 않기 위해, 생성 전에 이 값을 갱신하고, 연산에 이 값을 사용한다")]
			[PropertyOrder(10)]
			[Indent(1)]
			public Vector3Int StageParentSnappedPositionOriginalCache => stageParentSnappedPositionOriginalCache;
			[SerializeField, HideInInspector] private Vector3Int stageParentSnappedPositionOriginalCache;
			#endregion



			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[FoldoutGroup("트랜스폼 매니저/박스/스테이지 부모/좌표 정보 보기")]
			[ShowInInspector, EnableGUI, DisplayAsString]
			[LabelText("스냅된 부모 좌표 (Current)")]
			[PropertyOrder(10)]
			public Vector3Int StageParentSnappedPositionCurrent
			{
				get
				{
					var stageParentPositionCurrent = StageParent.position.SwizzlesVector(Setting.SnapSetting.Swizzle);
					return Setting.SnapSetting.SnapToGridUnit(stageParentPositionCurrent, false);
					//Setting.SnapSetting.SnapToGridUnit(ref stageParentPositionCurrent, false);
					//return stageParentPositionCurrent;
				}
			}

			#region 캐시
			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[FoldoutGroup("트랜스폼 매니저/박스/스테이지 부모/좌표 정보 보기")]
			[ShowInInspector, DisplayAsString]
			[LabelText("(캐시) 스냅된 부모 좌표 (Current)")]
			[PropertyTooltip("병렬 연산, 멀티 스레드에서 유니티 Transform을 호출하지 않기 위해, 생성 전에 이 값을 갱신하고, 연산에 이 값을 사용한다")]
			[PropertyOrder(10)]
			[Indent(1)]
			public Vector3Int StageParentSnappedPositionCurrentCache => stageParentSnappedPositionCurrentCache;
			[SerializeField, HideInInspector] private Vector3Int stageParentSnappedPositionCurrentCache;
			#endregion



			#endregion



			#region 스테이지 중심점



			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[FoldoutGroup("트랜스폼 매니저/박스/스테이지 중심점")]
			[ShowInInspector, EnableGUI, DisplayAsString]
			[LabelText("중심점 (Original)")]
			[PropertyOrder(10)]
			public Vector3 StageCenterPositionOriginal
				=> Main.IsValid_Setting ? StageParentSnappedPositionOriginal + Setting.StageVector.StageCenterOffsetCurrent : default;

			#region 캐시
			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[FoldoutGroup("트랜스폼 매니저/박스/스테이지 중심점")]
			[ShowInInspector, DisplayAsString]
			[LabelText("(캐시) 중심점 (Original)")]
			[PropertyOrder(10)]
			[Indent(1)]
			public Vector3 StageCenterPositionOriginalCache => stageCenterPositionOriginalCache;
			[SerializeField, HideInInspector] private Vector3 stageCenterPositionOriginalCache;
			#endregion



			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[FoldoutGroup("트랜스폼 매니저/박스/스테이지 중심점")]
			[ShowInInspector, EnableGUI, DisplayAsString]
			[LabelText("중심점 (Current)")]
			[PropertyOrder(10)]
			public Vector3 StageCenterPositionCurrent
				=> Main.IsValid_Setting ? StageCenterPositionOriginal.SwizzlesVector(Setting.SnapSetting.Swizzle) : default;

			#region 캐시
			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[FoldoutGroup("트랜스폼 매니저/박스/스테이지 중심점")]
			[ShowInInspector, DisplayAsString]
			[LabelText("(캐시) 중심점 (Current)")]
			[PropertyOrder(10)]
			[Indent(1)]
			public Vector3 StageCenterPositionCurrentCache => stageCenterPositionCurrentCache;
			[SerializeField, HideInInspector] private Vector3 stageCenterPositionCurrentCache;
			#endregion



			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[FoldoutGroup("트랜스폼 매니저/박스/스테이지 중심점")]
			[ShowInInspector, EnableGUI, DisplayAsString]
			[LabelText("중심점 (Transform)")]
			[PropertyOrder(10)]
			public Vector3 StageCenterPositionTransform
				=> Main.IsValid_Setting ? StageParentSnappedPositionOriginal + Setting.StageVector.StageCenterOffsetTransformCurrent : default;


			#region 캐시
			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[FoldoutGroup("트랜스폼 매니저/박스/스테이지 중심점")]
			[ShowInInspector, DisplayAsString]
			[LabelText("(캐시) 중심점 (Transform)")]
			[PropertyOrder(10)]
			[Indent(1)]
			public Vector3 StageCenterPositionTransformCache => stageCenterPositionTransformCache;
			[SerializeField, HideInInspector] private Vector3 stageCenterPositionTransformCache;
			#endregion



			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[FoldoutGroup("트랜스폼 매니저/박스/스테이지 중심점")]
			[ShowInInspector, EnableGUI, DisplayAsString]
			[LabelText("중심점 (Transform, Current)")]
			[PropertyOrder(10)]
			public Vector3 StageCenterPositionTransformCurrent
				=> Main.IsValid_Setting ? StageCenterPositionTransform.SwizzlesVector(Setting.SnapSetting.Swizzle) : default;

			#region 캐시
			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[FoldoutGroup("트랜스폼 매니저/박스/스테이지 중심점")]
			[ShowInInspector, DisplayAsString]
			[LabelText("(캐시) 중심점 (Transform, Current)")]
			[PropertyOrder(10)]
			[Indent(1)]
			public Vector3 StageCenterPositionTransformCurrentCache => stageCenterPositionTransformCurrentCache;
			[SerializeField, HideInInspector] private Vector3 stageCenterPositionTransformCurrentCache;
			#endregion



			#endregion



			#region 생성 지점 좌표



			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[FoldoutGroup("트랜스폼 매니저/박스/생성 지점")]
			[ShowInInspector, EnableGUI, DisplayAsString]
			[LabelText("생성 지점")]
			[PropertyOrder(10)]
			public Vector3 StageInstancePoint
				=> Main.IsValid_Setting ? StageParentSnappedPositionOriginal + Setting.StageVector.StageInstancePointCurrent : default;
			#region 캐시
			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[FoldoutGroup("트랜스폼 매니저/박스/생성 지점")]
			[ShowInInspector, DisplayAsString]
			[LabelText("(캐시) 생성 지점")]
			[PropertyOrder(10)]
			[Indent(1)]
			public Vector3 StageInstancePointCache => stageInstancePointCache;
			[SerializeField, HideInInspector] private Vector3 stageInstancePointCache;
			#endregion




			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[FoldoutGroup("트랜스폼 매니저/박스/생성 지점")]
			[ShowInInspector, EnableGUI, DisplayAsString]
			[LabelText("생성 지점 (Current)")]
			[PropertyOrder(10)]
			public Vector3 StageInstancePointCurrent
				=> Main.IsValid_Setting ? StageInstancePoint.SwizzlesVector(Setting.SnapSetting.Swizzle) : default;

			#region 캐시
			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[FoldoutGroup("트랜스폼 매니저/박스/생성 지점")]
			[ShowInInspector, DisplayAsString]
			[LabelText("(캐시) 생성 지점 (Current)")]
			[PropertyOrder(10)]
			[Indent(1)]
			public Vector3 StageInstancePointCurrentCache => stageInstancePointCurrentCache;
			[SerializeField, HideInInspector] private Vector3 stageInstancePointCurrentCache;
			#endregion


			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[FoldoutGroup("트랜스폼 매니저/박스/생성 지점")]
			[ShowInInspector, EnableGUI, DisplayAsString]
			[LabelText("생성 지점 (Transform)")]
			[PropertyOrder(10)]
			public Vector3 StageInstancePointTransform
				=> Main.IsValid_Setting ? StageParentSnappedPositionOriginal + Setting.StageVector.StageInstancePointTransformCurrent : default;


			#region 캐시
			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[FoldoutGroup("트랜스폼 매니저/박스/생성 지점")]
			[ShowInInspector, DisplayAsString]
			[LabelText("(캐시) 생성 지점 (Transform)")]
			[PropertyOrder(10)]
			[Indent(1)]
			public Vector3 StageInstancePointTransformCache => stageInstancePointTransformCache;
			[SerializeField, HideInInspector] private Vector3 stageInstancePointTransformCache;
			#endregion


			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[FoldoutGroup("트랜스폼 매니저/박스/생성 지점")]
			[ShowInInspector, EnableGUI, DisplayAsString]
			[LabelText("생성 지점 (Transform, Current)")]
			[PropertyOrder(10)]
			public Vector3 StageInstancePointTransformCurrent
				=> Main.IsValid_Setting ? StageInstancePointTransform.SwizzlesVector(Setting.SnapSetting.Swizzle) : default;

			#region 캐시
			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[FoldoutGroup("트랜스폼 매니저/박스/생성 지점")]
			[ShowInInspector, DisplayAsString]
			[LabelText("(캐시) 생성 지점 (Transform, Current)")]
			[PropertyOrder(10)]
			[Indent(1)]
			public Vector3 StageInstancePointTransformCurrentCache => stageInstancePointTransformCurrentCache;
			[SerializeField, HideInInspector] private Vector3 stageInstancePointTransformCurrentCache;
			#endregion



			/// <summary>
			/// 생성지점좌표를 그리드 보정 좌표로 변환하여 반환
			/// </summary>
			/// <param name="useCache"></param>
			/// <returns></returns>
			public Vector2Int GetStageInstancePoint_GridPositionCorrection(bool useCache)
			{
				//return Setting.SnapSetting.SnapToGridUnit(useCache ? (Vector2)StageInstancePointCurrentCache : (Vector2)StageInstancePointCurrent);
				//var target = useCache ? (Vector2)StageInstancePointCurrentCache : (Vector2)StageInstancePointCurrent;
				var target = useCache ? (Vector2)StageInstancePointTransformCurrentCache : (Vector2)StageInstancePointTransformCurrent;


				Vector2Int result = Vector2Int.zero;


				if (Main.Setting.SnapSetting.SnapToGridCellCenter)
				{
					result.x = Mathf.CeilToInt(target.x / Main.Setting.SnapSetting.GridUnitX_Width);
					result.y = Mathf.CeilToInt(target.y / Main.Setting.SnapSetting.GridUnitY_Height);
				}
				else
				{
					result.x = Mathf.FloorToInt(target.x / Main.Setting.SnapSetting.GridUnitX_Width);
					result.y = Mathf.FloorToInt(target.y / Main.Setting.SnapSetting.GridUnitY_Height);
				}

				return result;
			}



			#endregion



			#region 스테이지 벡터



			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[FoldoutGroup("트랜스폼 매니저/박스/벡터")]
			[ShowInInspector, EnableGUI, DisplayAsString]
			[LabelText("스테이지 Transform Rect")]
			[PropertyOrder(10)]
			public Rect StageTransformRect
			{
				get
				{
					if (!Main.IsValid_Setting) { return default; }
					var stageRect = Setting.StageVector.StageRectTransform;
					stageRect.position += (Vector2Int)StageParentSnappedPositionCurrent;
					return stageRect;
				}
			}



			#endregion



			#region 벡터 캐싱



			[TitleGroup("트랜스폼 매니저"), BoxGroup("트랜스폼 매니저/박스", false)]
			[ShowInInspector]
			[Button("좌표 캐싱", Icon = SdfIconType.LightningFill, Stretch = false, ButtonAlignment = 1f), GUIColor(0.97f, 0.85f, 0.39f)]
			[PropertyOrder(10)]
			public void CachingVectors()
			{
				//. 부모 좌표
				stageParentPositionOriginalCache = StageParentPositionOriginal;
				stageParentPositionCurrentCache = StageParentPositionCurrent;
				stageParentSnappedPositionOriginalCache = StageParentSnappedPositionOriginal;
				stageParentSnappedPositionCurrentCache = StageParentSnappedPositionCurrent;

				//. 중심점
				stageCenterPositionOriginalCache = StageCenterPositionOriginal;
				stageCenterPositionCurrentCache = StageCenterPositionCurrent;
				stageCenterPositionTransformCache = StageCenterPositionTransform;
				stageCenterPositionTransformCurrentCache = StageCenterPositionTransformCurrent;

				//. 생성 지점
				stageInstancePointCache = StageInstancePoint;
				stageInstancePointCurrentCache = StageInstancePointCurrent;
				stageInstancePointTransformCache = StageInstancePointTransform;
				stageInstancePointTransformCurrentCache = StageInstancePointTransformCurrent;
			}



			#endregion



			/// <summary>
			/// 스테이지 생성기의 Transform 중심점, 크기를 얻는다
			/// </summary>
			/// <param name="stageTransformCenter"></param>
			/// <param name="stageTransformSize"></param>
			public void GetStageGeneratorTransformCenterAndSize(out Vector3 stageTransformCenter, out Vector3 stageTransformSize)
			{
				if (!Main.IsValid_Setting)
				{
					stageTransformCenter = Vector3.zero;
					stageTransformSize = Vector3Int.zero;
					return;
				}

				//stageTransformCenter = StageCenterPositionTransform;
				//stageTransformSize = Setting.StageVector.StageSizeVector3Transform;

				stageTransformCenter = StageTransformRect.center.SwizzlesVector2To3(Setting.SnapSetting.Swizzle);
				stageTransformSize = StageTransformRect.size.SwizzlesVector2To3(Setting.SnapSetting.Swizzle);
			}



			/// <summary>
			/// 그리드 좌표를 스테이지 트랜스폼 좌표로 변환한다
			/// </summary>
			/// <param name="gridPosition"></param>
			/// <param name="resultWithSwizzle"></param>
			/// <returns></returns>
			public Vector3 ConvertGridPositionToTransformPosition(Vector2Int gridPosition, bool resultWithSwizzle)
			{
				if (!Main.IsValid_Setting) { return Vector3.zero; }


				//. 그리드 좌표를, 스냅 설정으로 트랜스폼 좌표로 변환한다 (좌측 하단 우선 고정)
				var transformPosition = Setting.SnapSetting.CalculateTransformPosition_byGridPosition(gridPosition, false);


				//. 그리드 단위에 따라 반칸 보정
				if (!Setting.SnapSetting.SnapToGridCellCenter)
				{
					transformPosition += new Vector3(Setting.SnapSetting.GridUnitX_Width * 0.5f, Setting.SnapSetting.GridUnitY_Height * 0.5f);
					//transformPosition -= new Vector3(Setting.SnapSetting.GridUnitX_Width * 0.5f, Setting.SnapSetting.GridUnitY_Height * 0.5f);
				}
				//transformPosition += new Vector3(Setting.SnapSetting.GridUnitX_Width * 0.5f, Setting.SnapSetting.GridUnitY_Height * 0.5f, 0);


				//. 스테이지 부모 좌표를 따라가기위해 더한다
				transformPosition += StageParentSnappedPositionCurrent;


				//. 그리드들은 좌측 하단에서부터 생성되기때문에, 이에 맞춘다
				transformPosition += (Vector3)Setting.StageVector.StageInstanceTransformPoint;


				//? 반환한다
				return (resultWithSwizzle) ? transformPosition.SwizzlesVector(Setting.SnapSetting.Swizzle) : transformPosition;

			}



			///======================================================================================================================================================
		}
	}
}