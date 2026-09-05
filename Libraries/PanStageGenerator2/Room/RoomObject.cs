using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pan.Util;
using System.Linq;
using System;
using Sirenix.OdinInspector;
using Pan.GridCompatibles2;
using Pan.StageGenerators;
using Cysharp.Threading.Tasks;
using System.Text;



namespace Pan.StageGenerators
{
	/// <summary>
	/// 공간 안에 방을 생성하기위한, 공간, 공간에 생성할 방, 공간의 조건에 맞는 방의 ActivatedRoomRect
	/// </summary>
	public struct RoomObjectPlaceSpaceInfo
	{
		public RoomObjectPlaceSpaceInfo(StageGenerator.Space space, RoomObject roomObject, CustomRect2DCentered activatedRect, CustomRect2DCentered activatedRectExpand)
		{
			Space = space;
			RoomObjectPrefab = roomObject;
			ActivatedRoomRect = activatedRect;
			ActivatedRoomRectExpand = activatedRectExpand;
		}

		public StageGenerator.Space Space;
		public RoomObject RoomObjectPrefab;
		public CustomRect2DCentered ActivatedRoomRect;
		public CustomRect2DCentered ActivatedRoomRectExpand;
	}



	/// <summary>
	/// <see cref="GridCompatible2Object"/> 기반의 방 오브젝트
	/// </summary>
	[RequireComponent(typeof(RoomObject))]
	public partial class RoomObject : GridCompatible2Object, IBaseMonoBehaviourHolder<RoomObject>
	{
		///======================================================================================================================================================



		//? RoomObject 하위 매니저



		public abstract class RoomComponent
		{
			[FoldoutGroup("기타", false, Order = 99)]
			[LabelText("부모 방 오브젝트")]
			[Sirenix.OdinInspector.ReadOnly]
			[SerializeField]
			[PropertyOrder(999)]
			protected RoomObject parentRoomObject;

			public RoomObject ParentRoomObject => parentRoomObject;



			///<summary>
			/// <see cref="RoomComponent"/> 최초 초기화
			/// </summary>
			public void Initialize(RoomObject parentRoomObject)
			{
				this.parentRoomObject = parentRoomObject;
			}
		}



		[Serializable]
		public abstract class BaseManager : MainSlave_WakeUpVer<RoomObject> { }



#if UNITY_EDITOR

		[ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Center, EnableRichText = true, Overflow = false), EnableGUI]
		[PropertyOrder(-100)]
		[PropertySpace(8, 8)]
		private string dummy_Title
		{
			get
			{
				return $"<b><size=15>방 오브젝트 ({name})</size></b>\n" +
					$"유효성: " + (IsValid_RoomObject ? $"✔️ <color=#2ecc71>Valid</color>" : $" ❌ <color=#ed5565>Invalid</color>\n" +
					$"<i>(겹쳐져있는 도어가 없어야 함)</i>");
			}
		}

#endif



		/// <summary>
		/// 방 오브젝트가 유효한지 확인
		/// </summary>		
		public bool IsValid_RoomObject
		{
			get
			{
				if (roomDoorM.CheckDoorGaps_Overlapped()) { return false; }
				return true;
			}
		}



		[TitleGroup("방 매니저"), FoldoutGroup("방 매니저/도어 매니저")]
		[SerializeField]
		[InlineProperty, HideLabel]
		protected DoorManager roomDoorM = new DoorManager();
		public DoorManager RoomDoorM => roomDoorM;



		[TitleGroup("방 매니저"), FoldoutGroup("방 매니저/바리에이션 매니저")]
		[SerializeField]
		[InlineProperty, HideLabel]
		protected VariationManager roomVariationM = new VariationManager();
		public VariationManager RoomVariationM => roomVariationM;





		//. 에디터에서 매니저들 상시 WakeUp 시켜주는 OnValidate
#if UNITY_EDITOR

		private void OnValidate()
		{
			//. 에디터에서 매니저들을 가용 가능한 상태로 항상 유지한다 (상시 강제 WakeUp)
			ManagersWakeUp(true);

			if (!editor_CanUseDoorDown) editor_RoomRect_UseDownCount = 0;
			if (!editor_CanUseDoorUp) editor_RoomRect_UseUpCount = 0;
			if (!editor_CanUseDoorLeft) editor_RoomRect_UseLeftCount = 0;
			if (!editor_CanUseDoorRight) editor_RoomRect_UseRightCount = 0;

			//if (!editor_CanUseDoorDown) editor_RoomRect_UseDown = false;// editor_RoomRect_UseDownCount = 0;
			//if (!editor_CanUseDoorUp) editor_RoomRect_UseUp = false;// editor_RoomRect_UseUpCount = 0;
			//if (!editor_CanUseDoorLeft) editor_RoomRect_UseLeft = false; //editor_RoomRect_UseLeftCount = 0;
			//if (!editor_CanUseDoorRight) editor_RoomRect_UseRight = false; //editor_RoomRect_UseRightCount = 0;

			//editor_RoomRect_UseDown = false;
			//editor_RoomRect_UseUp = false;
			//editor_RoomRect_UseLeft = false;
			//editor_RoomRect_UseRight = false;
			//editor_RoomRect_UseDownCount = 0;
			//editor_RoomRect_UseUpCount = 0;
			//editor_RoomRect_UseLeftCount = 0;
			//editor_RoomRect_UseRightCount = 0;
		}

#endif



		///======================================================================================================================================================



		//? 방 벡터 조정



		[TitleGroup("방 벡터 조정"), BoxGroup("방 벡터 조정/박스", false)]
		[ShowInInspector]
		[LabelText("방 셀프 확장 Size ↓하단")]
#if UNITY_EDITOR
		[GUIColor(nameof(editorRoomSelfExpandSizeColor_Down))]
#endif
		public int RoomSelfExpandSize_Down;

		[TitleGroup("방 벡터 조정"), BoxGroup("방 벡터 조정/박스", false)]
		[ShowInInspector]
		[LabelText("방 셀프 확장 Size ↑상단")]
#if UNITY_EDITOR
		[GUIColor(nameof(editorRoomSelfExpandSizeColor_Up))]
#endif
		public int RoomSelfExpandSize_Up;

		[TitleGroup("방 벡터 조정"), BoxGroup("방 벡터 조정/박스", false)]
		[ShowInInspector]
		[LabelText("방 셀프 확장 Size ←좌측")]
#if UNITY_EDITOR
		[GUIColor(nameof(editorRoomSelfExpandSizeColor_Left))]
#endif
		public int RoomSelfExpandSize_Left;

		[TitleGroup("방 벡터 조정"), BoxGroup("방 벡터 조정/박스", false)]
		[ShowInInspector]
		[LabelText("방 셀프 확장 Size →우측")]
#if UNITY_EDITOR
		[GUIColor(nameof(editorRoomSelfExpandSizeColor_Right))]
#endif
		public int RoomSelfExpandSize_Right;



#if UNITY_EDITOR
		private Color editorRoomSelfExpandSizeColor_Down => RoomSelfExpandSize_Down == 0 ? Color.white : Color.yellow;
		private Color editorRoomSelfExpandSizeColor_Up => RoomSelfExpandSize_Up == 0 ? Color.white : Color.yellow;
		private Color editorRoomSelfExpandSizeColor_Left => RoomSelfExpandSize_Left == 0 ? Color.white : Color.yellow;
		private Color editorRoomSelfExpandSizeColor_Right => RoomSelfExpandSize_Right == 0 ? Color.white : Color.yellow;
#endif



		private void Calculate_SelfExpand(ref Rect rect)
		{
			//. 각 방향 확장치를 Rect 가장자리로 직접 반영
			rect.yMin -= RoomSelfExpandSize_Down;
			rect.yMax += RoomSelfExpandSize_Up;
			rect.xMin -= RoomSelfExpandSize_Left;
			rect.xMax += RoomSelfExpandSize_Right;
		}



		///======================================================================================================================================================



		//? RoomObject 인스턴스 정보, 인스턴스로 생성하기



		[TitleGroup("방 인스턴스 정보")]
		[SerializeField]
		[Sirenix.OdinInspector.ReadOnly]
		[LabelText("인스턴스로 생성됨")]
		[PropertyTooltip("이 RoomObject가 인스턴스로 생성 되었는지 여부")]
		private bool roomObjectIsInstanced;
		///<summary>
		///이 <see cref="RoomObject"/>가 인스턴스로 생성 되었는지 여부
		///</summary>
		public bool RoomObjectIsInstanced
		{
			get => roomObjectIsInstanced;
			private set => roomObjectIsInstanced = value;
		}



		[TitleGroup("방 인스턴스 정보")]
		[SerializeField]
		[Sirenix.OdinInspector.ReadOnly]
		[LabelText("인스턴스 원본 프리팹 RoomObject")]
		private RoomObject instanceBaseRoomObjectPrefab = null;
		public RoomObject InstanceBaseRoomObjectPrefab => instanceBaseRoomObjectPrefab;



		RoomObject IBaseMonoBehaviourHolder<RoomObject>.BaseMonoBehvaiour { get => instanceBaseRoomObjectPrefab; set => instanceBaseRoomObjectPrefab = value; }
		GameObject IBaseGameObjectHolder.BaseGameObject { get => instanceBaseRoomObjectPrefab.gameObject; set => instanceBaseRoomObjectPrefab = value.GetComponent<RoomObject>(); }



		/// <summary>
		/// <see cref="RoomObject"/>를 인스턴스로 생성 한 후에 실행되는 공통 이벤트
		/// </summary>
		/// <param name="instancedroomObject">인스턴스로 생성된 <see cref="RoomObject"/></param>ㄴ
		private static RoomObject InstanceRoomObjectAfterInternal(RoomObject instancedRoomObject, RoomObject roomObjectPrefab)
		{
			//! 만약 StageParent가 비활성화 되어있다면, RoomObject의 Awake가 실행되지 않기 때문에, 강제로 호출해준다
			instancedRoomObject.ForceAwake();

			instancedRoomObject.RoomObjectIsInstanced = true;
			instancedRoomObject.instanceBaseRoomObjectPrefab = roomObjectPrefab;
			return instancedRoomObject;
		}



		/// <summary>
		/// <see cref="RoomObject"/>를 인스턴스로 생성<br/>
		/// </summary>
		/// <param name="roomObjectPrefab">베이스 <see cref="RoomObject"/> 프리팹</param>
		/// <param name="parent"></param>
		/// <returns></returns>
		public static RoomObject InstanceRoomObject(RoomObject roomObjectPrefab, Vector3 position, Quaternion rotation, Transform parent)
		{
			var instancedRoomObject = Instantiate(roomObjectPrefab, position, rotation, parent);
			return InstanceRoomObjectAfterInternal(instancedRoomObject, roomObjectPrefab);
		}



		/// <summary>
		/// <see cref="RoomObject"/>를 인스턴스로 생성<br/>
		/// </summary>
		/// <param name="roomObjectPrefab">베이스 <see cref="RoomObject"/> 프리팹</param>
		/// <param name="parent"></param>
		/// <returns></returns>
		public static RoomObject InstanceRoomObject(RoomObject roomObjectPrefab, Transform parent)
		{
			var instancedRoomObject = Instantiate(roomObjectPrefab, parent);
			return InstanceRoomObjectAfterInternal(instancedRoomObject, roomObjectPrefab);
		}



		/// <summary>
		/// [비동기] <see cref="RoomObject"/>를 인스턴스로 생성<br/>
		/// </summary>
		/// <param name="roomObjectPrefab">베이스 <see cref="RoomObject"/> 프리팹</param>
		/// <param name="parent"></param>
		/// <returns></returns>
		public static async UniTask<RoomObject> InstanceRoomObjectAsync(RoomObject roomObjectPrefab, Vector3 position, Quaternion rotation, Transform parent)
		{
			var operation = InstantiateAsync(roomObjectPrefab, parent, position, rotation);

			await operation;

			if (operation.Result != null)
			{
				var instancedRoomObject = operation.Result[0].GetComponent<RoomObject>();
				return InstanceRoomObjectAfterInternal(instancedRoomObject, roomObjectPrefab);
			}

			return null;
		}



		/// <summary>
		/// [비동기] 복수의 <see cref="RoomObject"/>들을 인스턴스로 생성<br/>
		/// </summary>
		/// <param name="roomObjectPrefab">베이스 <see cref="RoomObject"/> 프리팹</param>
		/// <param name="count">인스턴스로 생성할 개수</param>
		/// <param name="parent"></param>
		/// <returns></returns>
		public static async UniTask<RoomObject[]> InstanceRoomsObjectAsync(RoomObject roomObjectPrefab, int count, Transform parent)
		{
			var operation = await InstantiateAsync(roomObjectPrefab, count, parent);

			if (operation != null)
			{
				RoomObject[] instancedRoomObjects = new RoomObject[count];

				for (int i = 0; i < operation.Length; i++)
				{
					instancedRoomObjects[i] = operation[i].GetComponent<RoomObject>();
					InstanceRoomObjectAfterInternal(instancedRoomObjects[i], roomObjectPrefab);
				}

				return instancedRoomObjects;
			}

			return null;
		}



		///======================================================================================================================================================



		//? 방 Rect 정보



#if UNITY_EDITOR

		[TitleGroup("방 Rect 정보"), BoxGroup("방 Rect 정보/더미박스", false)]
		[PropertyTooltip("그리드 호환의 ObjectTransformRect을 반환함, 사실상 동일")]
		[ShowInInspector, EnableGUI, DisplayAsString(Alignment = TextAlignment.Left, EnableRichText = true, Overflow = false), HideLabel]
		private string dummy_RoomSizeInfoText
		{
			get
			{
				var totalRoomRectMin_Maximum = TotalRoomRectMin_Maximum;
				var totalRoomRectMax_Maximum = TotalRoomRectMax_Maximum;

				StringBuilder sb = new StringBuilder();

				sb.Append("<size=12>");
				sb.AppendLine($"중심 좌표: (<b><color=#2ecc71>{GridCenterTransformPositionVector2.x}</color>, <color=#2ecc71>{GridCenterTransformPositionVector2.y}</color></b>)");
				sb.AppendLine($"방 크기: <b><color=#f7da64>{GridCompatible.ObjectSizeX_Width}</color> x <color=#f7da64>{GridCompatible.ObjectSizeY_Height}</color></b>");
				sb.AppendLine($"(최소) 통합 방 Rect 최대 크기: <color=#f7da64>{totalRoomRectMin_Maximum.Size.x}</color> x <color=#f7da64>{totalRoomRectMin_Maximum.Size.y}</color> 오프셋: (<color=#2ecc71>{totalRoomRectMin_Maximum.Offset.x}</color>, <color=#2ecc71>{totalRoomRectMin_Maximum.Offset.y}</color>)</size>");
				sb.AppendLine($"(최대) 통합 방 Rect 최대 크기: <color=#f7da64>{totalRoomRectMax_Maximum.Size.x}</color> x <color=#f7da64>{totalRoomRectMax_Maximum.Size.y}</color> 오프셋: (<color=#2ecc71>{totalRoomRectMax_Maximum.Offset.x}</color>, <color=#2ecc71>{totalRoomRectMax_Maximum.Offset.y}</color>)</size>");
				return sb.ToString(true);
			}
		}

#endif



		//? 통합 방 Rect



		/// <summary>
		/// 도어 거리(수직 확장) + 도어 오버행(벽을 따라가는 축 확장) + 셀프 확장을 모두 반영하여
		/// 최종 방 Rect를 계산합니다. (그리드 정수 중심 로직 유지)
		/// </summary>
		private CustomRect2DRelative CalculateExpandedRect(int useDownCount, int useUpCount, int useLeftCount, int useRightCount, bool isMin)
		{
			//. 0) 원본 방(기본) Rect
			var size = GridCompatible.ObjectSizeOriginalVector2;
			Vector2 offset = Vector2.zero;
			Rect originalRect = SU_TF_Rect.RectFromCenter(offset, size);

			//. 1) 도어 “거리” 기반 수직 확장 (방의 높이/너비 + 중심 이동)
			ApplyDoorDistance(EDirection4.Down, useDownCount, isMin, ref size, ref offset);
			ApplyDoorDistance(EDirection4.Up, useUpCount, isMin, ref size, ref offset);
			ApplyDoorDistance(EDirection4.Left, useLeftCount, isMin, ref size, ref offset);
			ApplyDoorDistance(EDirection4.Right, useRightCount, isMin, ref size, ref offset);

			Rect rect = SU_TF_Rect.RectFromCenter(offset, size);

			//. 2) 도어 “오버행(±)” 기반, 벽을 따라가는 축 확장
			//? X축 확장: (Down·Up)에서 넘친 정도(좌<0 / 우>0)
			if (TryGetAxisOverhangSignedRange(EDirection4.Down, EDirection4.Up, out int minSignedX, out int maxSignedX))
			{
				rect.xMin = Mathf.Min(rect.xMin, originalRect.xMin + minSignedX);
				rect.xMax = Mathf.Max(rect.xMax, originalRect.xMax + maxSignedX);
			}

			//? Y축 확장: (Left·Right)에서 넘친 정도(하<0 / 상>0)
			if (TryGetAxisOverhangSignedRange(EDirection4.Left, EDirection4.Right, out int minSignedY, out int maxSignedY))
			{
				rect.yMin = Mathf.Min(rect.yMin, originalRect.yMin + minSignedY);
				rect.yMax = Mathf.Max(rect.yMax, originalRect.yMax + maxSignedY);
			}

			//. 3) 셀프 확장
			Calculate_SelfExpand(ref rect);

			return new CustomRect2DRelative(rect.center, rect.size);

			//====================== Local Helpers ======================

			//! 도어 “거리(FullDoorTotalDistance)”로 방의 크기/중심을 수직(법선) 방향으로 확장
			void ApplyDoorDistance(EDirection4 dir, int useCount, bool pickMin, ref Vector2Int s, ref Vector2 c)
			{
				if (useCount <= 0) return;
				if (!roomDoorM.TryGetDoorTotalDistanceByCount(dir, useCount, pickMin, out int dist)) return;

				switch (dir)
				{
					case EDirection4.Down:
					s.y += dist;
					c.y -= dist * 0.5f;  //! 아래로 키우면 중심은 아래로 이동
					break;

					case EDirection4.Up:
					s.y += dist;
					c.y += dist * 0.5f;
					break;

					case EDirection4.Left:
					s.x += dist;
					c.x -= dist * 0.5f;
					break;

					case EDirection4.Right:
					s.x += dist;
					c.x += dist * 0.5f;
					break;
				}
			}

			//? 두 방향(예: Down·Up / Left·Right)을 합쳐, 축 오버행의 “부호 포함 최소/최대”를 구함
			bool TryGetAxisOverhangSignedRange(EDirection4 a, EDirection4 b, out int minSigned, out int maxSigned)
			{
				bool hasAny = false;
				minSigned = int.MaxValue;  //. 초기 극값
				maxSigned = int.MinValue;

				if (roomDoorM.TryGetDoorAxisOverhangSignedExtrema(a, out int minA, out int maxA))
				{
					minSigned = Mathf.Min(minSigned, minA);
					maxSigned = Mathf.Max(maxSigned, maxA);
					hasAny = true;
				}

				if (roomDoorM.TryGetDoorAxisOverhangSignedExtrema(b, out int minB, out int maxB))
				{
					minSigned = Mathf.Min(minSigned, minB);
					maxSigned = Mathf.Max(maxSigned, maxB);
					hasAny = true;
				}

				if (!hasAny)
				{
					minSigned = 0;
					maxSigned = 0;
				}
				return hasAny;
			}

			#region Legacy 1
			//var size = GridCompatible.ObjectSizeOriginalVector2;
			//Vector2 offset = Vector2.zero;

			//Rect originalRect = SU_TF_Rect.RectFromCenter(offset, size);



			//if (roomDoorM.TryGetDoorTotalDistanceByCount(EDirection4.Down, useDownCount, isMin, out int doorTotalDistance_Down))
			//{
			//	size.y += doorTotalDistance_Down;
			//	offset.y -= doorTotalDistance_Down * 0.5f;
			//}
			//if (roomDoorM.TryGetDoorTotalDistanceByCount(EDirection4.Up, useUpCount, isMin, out int doorTotalDistance_Up))
			//{
			//	size.y += doorTotalDistance_Up;
			//	offset.y += doorTotalDistance_Up * 0.5f;
			//}
			//if (roomDoorM.TryGetDoorTotalDistanceByCount(EDirection4.Left, useLeftCount, isMin, out int doorTotalDistance_Left))
			//{
			//	size.x += doorTotalDistance_Left;
			//	offset.x -= doorTotalDistance_Left * 0.5f;
			//}
			//if (roomDoorM.TryGetDoorTotalDistanceByCount(EDirection4.Right, useRightCount, isMin, out int doorTotalDistance_Right))
			//{
			//	size.x += doorTotalDistance_Right;
			//	offset.x += doorTotalDistance_Right * 0.5f;
			//}


			//Rect rect = SU_TF_Rect.RectFromCenter(offset, size);



			//if (roomDoorM.TryGetDoorAxisOverhangSignedExtrema(EDirection4.Down, out var wrostLow_Down, out var wrostHigh_Down))
			//{
			//	rect.xMin = Mathf.Min(rect.xMin, originalRect.xMin + wrostLow_Down);
			//	rect.xMax = Mathf.Max(rect.xMax, originalRect.xMax + wrostHigh_Down);
			//}
			//if (roomDoorM.TryGetDoorAxisOverhangSignedExtrema(EDirection4.Up, out var wrostLow_Up, out var wrostHigh_Up))
			//{
			//	rect.xMin = Mathf.Min(rect.xMin, originalRect.xMin + wrostLow_Up);
			//	rect.xMax = Mathf.Max(rect.xMax, originalRect.xMax + wrostHigh_Up);
			//}
			//if (roomDoorM.TryGetDoorAxisOverhangSignedExtrema(EDirection4.Left, out var wrostLow_Left, out var wrostHigh_Left))
			//{
			//	rect.yMin = Mathf.Min(rect.yMin, originalRect.yMin + wrostLow_Left);
			//	rect.yMax = Mathf.Max(rect.yMax, originalRect.yMax + wrostHigh_Left);
			//}
			//if (roomDoorM.TryGetDoorAxisOverhangSignedExtrema(EDirection4.Right, out var wrostLow_Right, out var wrostHigh_Right))
			//{
			//	rect.yMin = Mathf.Min(rect.yMin, originalRect.yMin + wrostLow_Right);
			//	rect.yMax = Mathf.Max(rect.yMax, originalRect.yMax + wrostHigh_Right);
			//}


			////. 셀프 확장 연산
			//Calculate_SelfExpand(ref rect);


			//return new CustomRect2DRelative(rect.center, rect.size); 
			#endregion
			#region Legacy (DoorEndRect를 사용하던 폐기물)
			//var center = CorrectionGridCenterTransformPositionVector2;


			////? GridUnit이 1 초과이고, 이 오브젝트의 좌표가 0,0이 아닐때, 이동 거리에 비례해서 Rect가 비정상적이게 확장되는것을 방지
			////!     다시보니 억지계산인데? 이러면 GridUnit이 1일때 이동거리에 비례해서 Rect가 비정상적이게 확장돼
			////.         아닌데 그냥 이렇게 보정하자
			//center -= new Vector2(GridPosition.x * (GridCompatible.CurrentSnapSetting.GridUnitX_Width - 1), GridPosition.y * (GridCompatible.CurrentSnapSetting.GridUnitX_Width - 1));


			////. 시작 Rect
			//Rect baseRect = SU_TF_Rect.RectFromCenter(center, GridCompatible.ObjectSizeOriginalVector2);


			////. 코너 좌표
			//Vector2 downLeft = new(baseRect.xMin, baseRect.yMin);
			//Vector2 upRight = new(baseRect.xMax, baseRect.yMax);


			////? 방향별 처리
			//ProcessDirection(roomDoorM.GetDoorList(EDirection4.Down), useDownCount, d => d.DoorEndRect.yMin, false);
			//ProcessDirection(roomDoorM.GetDoorList(EDirection4.Up), useUpCount, d => d.DoorEndRect.yMax, true);
			//ProcessDirection(roomDoorM.GetDoorList(EDirection4.Left), useLeftCount, d => d.DoorEndRect.xMin, false);
			//ProcessDirection(roomDoorM.GetDoorList(EDirection4.Right), useRightCount, d => d.DoorEndRect.xMax, true);



			////if (!GridCompatible.CurrentSnapSetting.SnapToGridCellCenter)
			////{
			////    if (GridCompatible.CurrentSnapSetting.GridUnitY_Height.IsEven() && !GridCompatible.ObjectSizeY_Height.IsEven())
			////    {
			////        downLeft += new Vector2(0, 0.5f);
			////    }
			////    if (GridCompatible.CurrentSnapSetting.GridUnitX_Width.IsEven() && !GridCompatible.ObjectSizeX_Width.IsEven())
			////    {
			////        downLeft += new Vector2(0.5f, 0);
			////    }
			////}



			//var size = upRight - downLeft;

			////Debug.Log($"DL {downLeft} UR {upRight} Size {size}");
			////Debug.Log($"(2x) DL {downLeft * 2f} UR {upRight * 2f} Size {size * 2f}");

			////if (!GridCompatible.CurrentSnapSetting.SnapToGridCellCenter)
			////{
			////    if ((GridCompatible.ObjectSizeTransformX_Width).IsEven())
			////    {
			////        if (GridCompatible.PreferUpperRightGridCell)
			////        {
			////            size.x += 0.5f;
			////            center.x += 0.5f;
			////        }
			////        else
			////        {
			////            size.x -= 0.5f;
			////            center.x -= 0.5f;
			////        }
			////    }


			////    if ((GridCompatible.ObjectSizeTransformY_Height).IsEven())
			////    {
			////        if (GridCompatible.PreferUpperRightGridCell)
			////        {
			////            size.y += 0.5f;
			////            center.y += 0.5f;
			////        }
			////        else
			////        {
			////            size.y -= 0.5f;
			////            center.y -= 0.5f;
			////        }
			////    }
			////}


			//var rect = new Rect(downLeft, size);
			////.     rect를 사용안하고 압축한 연산 (rect의 중심점만 필요하기에 그냥 직접 연산)
			////return new CustomRect2DRelative(new Vector2(downLeft.x + size.x * 0.5f, downLeft.y + size.y * 0.5f) - center, size);
			//return new CustomRect2DRelative(rect.center - center, size);

			////! 도어 정렬 및 코너 확장
			//void ProcessDirection(List<Door> doors, int needCnt, Func<Door, float> keySelector, bool isPositive)
			//{
			//    if (needCnt <= 0 || doors is not { Count: > 0 }) return;

			//    //. isMin 과 방향성(±)에 따라 오름/내림차순 결정
			//    bool ascending = isMin == isPositive;
			//    IEnumerable<Door> ordered = ascending ? doors.OrderBy(keySelector) : doors.OrderByDescending(keySelector);

			//    int take = Mathf.Min(needCnt, doors.Count);

			//    foreach (var door in ordered.Take(take)) Extend(ref downLeft, ref upRight, door.DoorEndRect);
			//}

			////. 좌하‧우상 갱신
			//void Extend(ref Vector2 dl, ref Vector2 ur, Rect r)
			//{
			//    dl.x = Mathf.Min(dl.x, r.xMin);
			//    dl.y = Mathf.Min(dl.y, r.yMin);
			//    ur.x = Mathf.Max(ur.x, r.xMax);
			//    ur.y = Mathf.Max(ur.y, r.yMax);
			//} 
			#endregion
		}



		/// <summary>
		/// <b>최소 크기</b>의 통합 방 Rect를 얻는다
		/// <para>방 RecWithOffset 확장 요소에 포함 될 도어들을 방향별로 각각 받아와 선택 할 수 있다</para>
		/// </summary>
		/// <param name="useDownCount">하단 도어들을 Rect에 포함 할 개수</param>
		/// <param name="useUpCount">상단 도어들을 Rect에 포함 할 개수</param>
		/// <param name="useLeftCount">좌측 도어들을 Rect에 포함 할 개수</param>
		/// <param name="useRightCount">우측 도어들을 Rect에 포함 할 개수</param>
		/// <returns></returns>
		public CustomRect2DRelative TotalRoomRectMin(int useDownCount, int useUpCount, int useLeftCount, int useRightCount)
			=> CalculateExpandedRect(useDownCount, useUpCount, useLeftCount, useRightCount, true);

		/// <summary>
		/// <b>최대 크기</b>의 통합 방 Rect를 얻는다
		/// <para>방 RecWithOffset 확장 요소에 포함 될 도어들을 방향별로 각각 받아와 선택 할 수 있다</para>
		/// </summary>
		/// <param name="useDownCount">하단 도어들을 Rect에 포함 할 개수</param>
		/// <param name="useUpCount">상단 도어들을 Rect에 포함 할 개수</param>
		/// <param name="useLeftCount">좌측 도어들을 Rect에 포함 할 개수</param>
		/// <param name="useRightCount">우측 도어들을 Rect에 포함 할 개수</param>
		/// <returns></returns>
		public CustomRect2DRelative TotalRoomRectMax(int useDownCount, int useUpCount, int useLeftCount, int useRightCount)
			=> CalculateExpandedRect(useDownCount, useUpCount, useLeftCount, useRightCount, false);



		/// <summary>
		/// <b>최소 크기</b>의 통합 방 Rect를 얻는다
		/// <para>방 RecWithOffset 확장 요소에 포함될 도어들을 방향별로 각각 받아와 선택 할 수 있다</para>
		/// </summary>
		/// <param name="useDownCount">하단 도어들을 Rect에 포함</param>
		/// <param name="useUp">상단 도어들을 Rect에 포함</param>
		/// <param name="useLeft">좌측 도어들을 Rect에 포함</param>
		/// <param name="useRight">우측 도어들을 Rect에 포함</param>
		/// <returns></returns>
		public CustomRect2DRelative TotalRoomRectMin(bool useDown, bool useUp, bool useLeft, bool useRight)
			=> CalculateExpandedRect(useDown ? 1 : 0, useUp ? 1 : 0, useLeft ? 1 : 0, useRight ? 1 : 0, true);

		/// <summary>
		/// <b>최대 크기</b>의 통합 방 Rect를 얻는다
		/// <para>방 RecWithOffset 확장 요소에 포함될 도어들을 방향별로 각각 받아와 선택 할 수 있다</para>
		/// </summary>
		/// <param name="useDown">하단 도어들을 Rect에 포함</param>
		/// <param name="useUp">상단 도어들을 Rect에 포함</param>
		/// <param name="useLeft">좌측 도어들을 Rect에 포함</param>
		/// <param name="useRight">우측 도어들을 Rect에 포함</param>
		/// <returns></returns>
		public CustomRect2DRelative TotalRoomRectMax(bool useDown, bool useUp, bool useLeft, bool useRight)
			=> CalculateExpandedRect(useDown ? 1 : 0, useUp ? 1 : 0, useLeft ? 1 : 0, useRight ? 1 : 0, false);



		[TitleGroup("방 Rect 정보")]
		[ShowInInspector, EnableGUI, DisplayAsString]
		[LabelText("(최소크기) 통합 방 Rect 맥시멈")]
		[PropertyTooltip("모든 방향의 도어가 가용된 통합 방 Rect")]
		public CustomRect2DRelative TotalRoomRectMin_Maximum
			=> TotalRoomRectMin(true, true, true, true);

		[TitleGroup("방 Rect 정보")]
		[ShowInInspector, EnableGUI, DisplayAsString]
		[LabelText("(최대크기) 통합 방 Rect 맥시멈")]
		[PropertyTooltip("모든 방향의 도어가 가용된 통합 방 Rect")]
		public CustomRect2DRelative TotalRoomRectMax_Maximum
			=> TotalRoomRectMax(true, true, true, true);



		//? 통합 방 Rect 확장 (스테이지 설정으로 확장)



		/// <summary>
		/// 스테이지 설정에 맞춰 방 통합 Rect를 확장한다
		/// </summary>
		/// <param name="stageGeneratorSetting"></param>
		/// <param name="totalRoomRect"></param>
		/// <param name="useDown"></param>
		/// <param name="useUp"></param>
		/// <param name="useLeft"></param>
		/// <param name="useRight"></param>
		private void ExpandTotalRoomRectInternal(IStageGeneratorSetting stageGeneratorSetting, ref CustomRect2DRelative totalRoomRect, bool useDown, bool useUp, bool useLeft, bool useRight)
		{
			//. 설정에 의한 확장
			stageGeneratorSetting.Setting.Room.SetTotalRoomRectToExpand(ref totalRoomRect,
				out var applyedCurrentHallwayWidth,
				useDown && roomDoorM.GetDoorEnabled(EDirection4.Down),
				useUp && roomDoorM.GetDoorEnabled(EDirection4.Up),
				useLeft && roomDoorM.GetDoorEnabled(EDirection4.Left),
				useRight && roomDoorM.GetDoorEnabled(EDirection4.Right));

			//? 방향별 도어의 가용여부 & 해당 방향 도어중 가장 넓은 너비가 확장된 값보다 작다면, 더 넓어야 하는 만큼 추가 확장
			bool hasDoor_Down = RoomDoorM.TryGetMaxDoorWidth(EDirection4.Down, out var doorMaxWidth_Down);
			bool hasDoor_Up = RoomDoorM.TryGetMaxDoorWidth(EDirection4.Up, out var doorMaxWidth_Up);
			bool hasDoor_Left = RoomDoorM.TryGetMaxDoorWidth(EDirection4.Left, out var doorMaxWidth_Left);
			bool hasDoor_Right = RoomDoorM.TryGetMaxDoorWidth(EDirection4.Right, out var doorMaxWidth_Right);
			if (useDown && hasDoor_Down && applyedCurrentHallwayWidth < doorMaxWidth_Down) { totalRoomRect.yMin -= doorMaxWidth_Down - applyedCurrentHallwayWidth; }
			if (useUp && hasDoor_Up && applyedCurrentHallwayWidth < doorMaxWidth_Up) { totalRoomRect.yMax += doorMaxWidth_Up - applyedCurrentHallwayWidth; }
			if (useLeft && hasDoor_Left && applyedCurrentHallwayWidth < doorMaxWidth_Left) { totalRoomRect.xMin -= doorMaxWidth_Left - applyedCurrentHallwayWidth; }
			if (useRight && hasDoor_Right && applyedCurrentHallwayWidth < doorMaxWidth_Right) { totalRoomRect.xMax += doorMaxWidth_Right - applyedCurrentHallwayWidth; }
		}



		/// <summary>
		///<b>최소 크기</b>로 확장된 통합 방 Rect를 얻는다
		/// <para>방 Rect 확장 요소에 포함 될 도어들을 방향별로 각각 받아와 선택 할 수 있으며</para>
		/// <para>스테이지 설정 인터페이스를 받아와, 해당 설정값에 맞춰 한번 더 확장하여 반환한다</para>
		/// </summary>
		/// <param name="useDownCount">하단 도어들을 Rect에 포함 할 개수</param>
		/// <param name="useUpCount">상단 도어들을 Rect에 포함 할 개수</param>
		/// <param name="useLeftCount">좌측 도어들을 Rect에 포함 할 개수</param>
		/// <param name="useRightCount">우측 도어들을 Rect에 포함 할 개수</param>
		/// <returns></returns>
		public CustomRect2DRelative TotalRoomRectExpandMin(IStageGeneratorSetting stageGeneratorSetting, int useDownCount, int useUpCount, int useLeftCount, int useRightCount)
		{
			var rect = TotalRoomRectMin(useDownCount, useUpCount, useLeftCount, useRightCount);
			ExpandTotalRoomRectInternal(stageGeneratorSetting, ref rect, useDownCount > 0, useUpCount > 0, useLeftCount > 0, useRightCount > 0);
			return rect;
		}

		/// <summary>
		///<b>최대 크기</b>로 확장된 통합 방 Rect를 얻는다
		/// <para>방 Rect 확장 요소에 포함 될 도어들을 방향별로 각각 받아와 선택 할 수 있으며</para>
		/// <para>스테이지 설정 인터페이스를 받아와, 해당 설정값에 맞춰 한번 더 확장하여 반환한다</para>
		/// </summary>
		/// <param name="useDownCount">하단 도어들을 Rect에 포함 할 개수</param>
		/// <param name="useUpCount">상단 도어들을 Rect에 포함 할 개수</param>
		/// <param name="useLeftCount">좌측 도어들을 Rect에 포함 할 개수</param>
		/// <param name="useRightCount">우측 도어들을 Rect에 포함 할 개수</param>
		/// <returns></returns>
		public CustomRect2DRelative TotalRoomRectExpandMax(IStageGeneratorSetting stageGeneratorSetting, int useDownCount, int useUpCount, int useLeftCount, int useRightCount)
		{
			var rect = TotalRoomRectMax(useDownCount, useUpCount, useLeftCount, useRightCount);
			ExpandTotalRoomRectInternal(stageGeneratorSetting, ref rect, useDownCount > 0, useUpCount > 0, useLeftCount > 0, useRightCount > 0);
			return rect;
		}



		/// <summary>
		///<b>최소 크기</b>로 확장된 통합 방 Rect를 얻는다
		/// <para>방 Rect 확장 요소에 포함될 도어들을 방향별로 각각 받아와 선택 할 수 있으며</para>
		/// <para>스테이지 설정 인터페이스를 받아와, 해당 설정값에 맞춰 한번 더 확장하여 반환한다</para>
		/// </summary>
		/// <param name="useDown">하단 도어들을 Rect에 포함</param>
		/// <param name="useUp">상단 도어들을 Rect에 포함</param>
		/// <param name="useLeft">좌측 도어들을 Rect에 포함</param>
		/// <param name="useRight">우측 도어들을 Rect에 포함</param>
		/// <returns></returns>
		public CustomRect2DRelative TotalRoomRectExpandMin(IStageGeneratorSetting stageGeneratorSetting, bool useDown, bool useUp, bool useLeft, bool useRight)
			=> TotalRoomRectExpandMin(stageGeneratorSetting, useDown ? 1 : 0, useUp ? 1 : 0, useLeft ? 1 : 0, useRight ? 1 : 0);


		/// <summary>
		///<b>최대 크기</b>로 확장된 통합 방 Rect를 얻는다
		/// <para>방 Rect 확장 요소에 포함될 도어들을 방향별로 각각 받아와 선택 할 수 있으며</para>
		/// <para>스테이지 설정 인터페이스를 받아와, 해당 설정값에 맞춰 한번 더 확장하여 반환한다</para>
		/// </summary>
		/// <param name="useDown">하단 도어들을 Rect에 포함</param>
		/// <param name="useUp">상단 도어들을 Rect에 포함</param>
		/// <param name="useLeft">좌측 도어들을 Rect에 포함</param>
		/// <param name="useRight">우측 도어들을 Rect에 포함</param>
		/// <returns></returns>
		public CustomRect2DRelative TotalRoomRectExpandMax(IStageGeneratorSetting stageGeneratorSetting, bool useDown, bool useUp, bool useLeft, bool useRight)
			=> TotalRoomRectExpandMax(stageGeneratorSetting, useDown ? 1 : 0, useUp ? 1 : 0, useLeft ? 1 : 0, useRight ? 1 : 0);



		/// <summary>
		/// <b>최소 크기</b>로 확장된 통합 방 Rect의 최대 크기 (모든 방향의 도어 Rect를 가용)
		/// </summary>
		public CustomRect2DRelative TotalRoomRectExpandMin_Maximum(IStageGeneratorSetting stageGeneratorSetting)
			=> TotalRoomRectExpandMin(stageGeneratorSetting, true, true, true, true);

		/// <summary>
		/// <b>최대 크기</b>로확장된 통합 방 Rect의 최대 크기 (모든 방향의 도어 Rect를 가용)
		/// </summary>
		public CustomRect2DRelative TotalRoomRectExpandMax_Maximum(IStageGeneratorSetting stageGeneratorSetting)
			=> TotalRoomRectExpandMax(stageGeneratorSetting, true, true, true, true);



		///======================================================================================================================================================



		//. 통합 방 Rect 테스트 뷰어
#if UNITY_EDITOR

		//. 에디터에서 각 4가지 방향의 도어를 각기 가용했을때 Rect가 어떻게 변화하는지 테스트 하기 위해 존재


		[NonSerialized]
		private bool editor_RoomRect_UseDown;
		[NonSerialized]
		private bool editor_RoomRect_UseUp;
		[NonSerialized]
		private bool editor_RoomRect_UseLeft;
		[NonSerialized]
		private bool editor_RoomRect_UseRight;



		[TitleGroup("방 Rect 정보"), BoxGroup("방 Rect 정보/테스트 방 통합 Rect 뷰어")]
		[HorizontalGroup("방 Rect 정보/테스트 방 통합 Rect 뷰어/도어필요개수가로")]
		[ShowIf(nameof(editor_RoomRect_UseDown))]
		[LabelWidth(40)]
		[PropertyOrder(100)]
		[NonSerialized, ShowInInspector]
		[MinValue(0)]
		[LabelText("↓하단")]
		[PropertyTooltip("하단 방향에 필요한 도어 총 개수, 이 값에 따라 조건에 충족하는 최소, 최대 Rect가 표시된다")]
		private int editor_RoomRect_UseDownCount;

		[TitleGroup("방 Rect 정보"), BoxGroup("방 Rect 정보/테스트 방 통합 Rect 뷰어")]
		[HorizontalGroup("방 Rect 정보/테스트 방 통합 Rect 뷰어/도어필요개수가로")]
		[ShowIf(nameof(editor_RoomRect_UseUp))]
		[LabelWidth(40)]
		[PropertyOrder(100)]
		[NonSerialized, ShowInInspector]
		[MinValue(0)]
		[LabelText("↑상단")]
		[PropertyTooltip("상단 방향에 필요한 도어 총 개수, 이 값에 따라 조건에 충족하는 최소, 최대 Rect가 표시된다")]
		private int editor_RoomRect_UseUpCount;

		[TitleGroup("방 Rect 정보"), BoxGroup("방 Rect 정보/테스트 방 통합 Rect 뷰어")]
		[HorizontalGroup("방 Rect 정보/테스트 방 통합 Rect 뷰어/도어필요개수가로")]
		[ShowIf(nameof(editor_RoomRect_UseLeft))]
		[LabelWidth(40)]
		[PropertyOrder(100)]
		[NonSerialized, ShowInInspector]
		[MinValue(0)]
		[LabelText("←좌측")]
		[PropertyTooltip("좌측 방향에 필요한 도어 총 개수, 이 값에 따라 조건에 충족하는 최소, 최대 Rect가 표시된다")]
		private int editor_RoomRect_UseLeftCount;

		[TitleGroup("방 Rect 정보"), BoxGroup("방 Rect 정보/테스트 방 통합 Rect 뷰어")]
		[HorizontalGroup("방 Rect 정보/테스트 방 통합 Rect 뷰어/도어필요개수가로")]
		[ShowIf(nameof(editor_RoomRect_UseRight))]
		[LabelWidth(40)]
		[PropertyOrder(100)]
		[NonSerialized, ShowInInspector]
		[MinValue(0)]
		[LabelText("→우측")]
		[PropertyTooltip("우측 방향에 필요한 도어 총 개수, 이 값에 따라 조건에 충족하는 최소, 최대 Rect가 표시된다")]
		private int editor_RoomRect_UseRightCount;



		private string editor_ButtonName_Down => editor_RoomRect_UseDown ? "↓하단 비활성화" : "↓하단 활성화";
		private string editor_ButtonName_Up => editor_RoomRect_UseUp ? "↑상단 비활성화" : "↑상단 활성화";
		private string editor_ButtonName_Left => editor_RoomRect_UseLeft ? "←좌측 비활성화" : "←좌측 활성화";
		private string editor_ButtonName_Right => editor_RoomRect_UseRight ? "→우측 비활성화" : "→우측 활성화";



		private Color editor_ButtonColor_Down => editor_RoomRect_UseDown ? "#2ecc71".HexToColor() : "#ffffff".HexToColor();
		private Color editor_ButtonColor_Up => editor_RoomRect_UseUp ? "#2ecc71".HexToColor() : "#ffffff".HexToColor();
		private Color editor_ButtonColor_Left => editor_RoomRect_UseLeft ? "#2ecc71".HexToColor() : "#ffffff".HexToColor();
		private Color editor_ButtonColor_Right => editor_RoomRect_UseRight ? "#2ecc71".HexToColor() : "#ffffff".HexToColor();


		private bool editor_CanUseDoorDown => roomDoorM.GetDoorEnabled(EDirection4.Down);
		private bool editor_CanUseDoorUp => roomDoorM.GetDoorEnabled(EDirection4.Up);
		private bool editor_CanUseDoorLeft => roomDoorM.GetDoorEnabled(EDirection4.Left);
		private bool editor_CanUseDoorRight => roomDoorM.GetDoorEnabled(EDirection4.Right);


		private CustomRect2DRelative TestTotalRoomRectMin_Viewer
		{
			get
			{
				if (editorTest_CurrentStageGeneratorSetting != null)
				{
					return TotalRoomRectExpandMin(editorTest_CurrentStageGeneratorSetting.Setting, editor_RoomRect_UseDownCount, editor_RoomRect_UseUpCount, editor_RoomRect_UseLeftCount, editor_RoomRect_UseRightCount);
				}
				else
				{
					return TotalRoomRectMin(editor_RoomRect_UseDownCount, editor_RoomRect_UseUpCount, editor_RoomRect_UseLeftCount, editor_RoomRect_UseRightCount);
				}
			}
		}

		private CustomRect2DRelative TestTotalRoomRectMax_Viewer
		{
			get
			{
				if (editorTest_CurrentStageGeneratorSetting != null)
				{
					return TotalRoomRectExpandMax(editorTest_CurrentStageGeneratorSetting.Setting, editor_RoomRect_UseDownCount, editor_RoomRect_UseUpCount, editor_RoomRect_UseLeftCount, editor_RoomRect_UseRightCount);
				}
				else
				{
					return TotalRoomRectMax(editor_RoomRect_UseDownCount, editor_RoomRect_UseUpCount, editor_RoomRect_UseLeftCount, editor_RoomRect_UseRightCount);
				}
			}
		}



		[TitleGroup("방 Rect 정보"), BoxGroup("방 Rect 정보/테스트 방 통합 Rect 뷰어")]
		[ShowInInspector, EnableGUI, DisplayAsString(Alignment = TextAlignment.Left, EnableRichText = true, Overflow = false), HideLabel]
		[PropertyOrder(99)]
		private string TestTotalRoomRect_ViewerText
		{
			get
			{
				var totalRoomRectMin_Viewer = TestTotalRoomRectMin_Viewer;
				var totalRoomRectMax_Viewer = TestTotalRoomRectMax_Viewer;

				StringBuilder sb = new StringBuilder();
				if (editorTest_CurrentStageGeneratorSetting != null)
				{
					sb.AppendLine($"<size=12><color=#ed5565>{editorTest_CurrentStageGeneratorSetting.name} 적용중</color></size>");
				}


				sb.AppendLine($"(최소) 통합 방 Rect 최대 크기: <color=#f7da64>{totalRoomRectMin_Viewer.Size.x}</color> x <color=#f7da64>{totalRoomRectMin_Viewer.Size.y}</color> 오프셋: (<color=#2ecc71>{totalRoomRectMin_Viewer.Offset.x}</color>, <color=#2ecc71>{totalRoomRectMin_Viewer.Offset.y}</color>)</size>");
				sb.AppendLine($"(최대) 통합 방 Rect 최대 크기: <color=#f7da64>{totalRoomRectMax_Viewer.Size.x}</color> x <color=#f7da64>{totalRoomRectMax_Viewer.Size.y}</color> 오프셋: (<color=#2ecc71>{totalRoomRectMax_Viewer.Offset.x}</color>, <color=#2ecc71>{totalRoomRectMax_Viewer.Offset.y}</color>)</size>");


				return sb.ToString();
			}
		}



		[TitleGroup("방 Rect 정보"), BoxGroup("방 Rect 정보/테스트 방 통합 Rect 뷰어")]
		[Button("$editor_ButtonName_Down"), ButtonGroup("방 Rect 정보/테스트 방 통합 Rect 뷰어/방Rect테스트버튼그룹")]
		[GUIColor(nameof(editor_ButtonColor_Down))]
		[EnableIf(nameof(editor_CanUseDoorDown))]
		[PropertyOrder(99)]
		private void EditorRoomRectToogle_Down()
		{
			UnityEditor.Undo.RecordObject(this, $"{name}'s RoomRect Test Toogled");
			editor_RoomRect_UseDown = !editor_RoomRect_UseDown;
			editor_RoomRect_UseDownCount = editor_RoomRect_UseDown ? 1 : 0;
			Sirenix.Utilities.Editor.GUIHelper.RequestRepaint(); //. 인스펙터 즉시 새로고침
		}

		[TitleGroup("방 Rect 정보"), BoxGroup("방 Rect 정보/테스트 방 통합 Rect 뷰어")]
		[Button("$editor_ButtonName_Up"), ButtonGroup("방 Rect 정보/테스트 방 통합 Rect 뷰어/방Rect테스트버튼그룹")]
		[GUIColor(nameof(editor_ButtonColor_Up))]
		[EnableIf(nameof(editor_CanUseDoorUp))]
		[PropertyOrder(99)]
		private void EditorRoomRectToogle_Up()
		{
			UnityEditor.Undo.RecordObject(this, $"{name}'s RoomRect Test Toogled");
			editor_RoomRect_UseUp = !editor_RoomRect_UseUp;
			editor_RoomRect_UseUpCount = editor_RoomRect_UseUp ? 1 : 0;
			Sirenix.Utilities.Editor.GUIHelper.RequestRepaint(); //. 인스펙터 즉시 새로고침
		}


		[TitleGroup("방 Rect 정보"), BoxGroup("방 Rect 정보/테스트 방 통합 Rect 뷰어")]
		[Button("$editor_ButtonName_Left"), ButtonGroup("방 Rect 정보/테스트 방 통합 Rect 뷰어/방Rect테스트버튼그룹")]
		[GUIColor(nameof(editor_ButtonColor_Left))]
		[EnableIf(nameof(editor_CanUseDoorLeft))]
		[PropertyOrder(99)]
		private void EditorRoomRectToogle_Left()
		{
			UnityEditor.Undo.RecordObject(this, $"{name}'s RoomRect Test Toogled");
			editor_RoomRect_UseLeft = !editor_RoomRect_UseLeft;
			editor_RoomRect_UseLeftCount = editor_RoomRect_UseLeft ? 1 : 0;
			Sirenix.Utilities.Editor.GUIHelper.RequestRepaint(); //. 인스펙터 즉시 새로고침
		}

		[TitleGroup("방 Rect 정보"), BoxGroup("방 Rect 정보/테스트 방 통합 Rect 뷰어")]
		[Button("$editor_ButtonName_Right"), ButtonGroup("방 Rect 정보/테스트 방 통합 Rect 뷰어/방Rect테스트버튼그룹")]
		[GUIColor(nameof(editor_ButtonColor_Right))]
		[EnableIf(nameof(editor_CanUseDoorRight))]
		[PropertyOrder(99)]
		private void EditorRoomRectToogle_Right()
		{
			UnityEditor.Undo.RecordObject(this, $"{name}'s RoomRect Test Toogled");
			editor_RoomRect_UseRight = !editor_RoomRect_UseRight;
			editor_RoomRect_UseRightCount = editor_RoomRect_UseRight ? 1 : 0;
			Sirenix.Utilities.Editor.GUIHelper.RequestRepaint(); //. 인스펙터 즉시 새로고침
		}



		[TitleGroup("방 Rect 정보"), BoxGroup("방 Rect 정보/테스트 방 통합 Rect 뷰어")]
		[LabelText("(테스트용)이 방에 설정된 스테이지 설정")]
		[ShowInInspector]
		[PropertyOrder(110)]
		private StageGeneratorSettingSbject editorTest_CurrentStageGeneratorSetting;




		[TitleGroup("방 Rect 정보"), BoxGroup("방 Rect 정보/테스트 방 통합 Rect 뷰어")]
		[LabelText("(테스트용)이 방에 설정된 스테이지 설정")]
		[ButtonGroup("방 Rect 정보/테스트 방 통합 Rect 뷰어/버튼들"), Button("스테이지 설정 제거")]
		[ShowIf("@editorTest_CurrentStageGeneratorSetting!=null")]
		[GUIColor(0.93f, 0.33f, 0.40f)]
		[PropertyOrder(110)]
		private void EditorTest_Remove_CurrentStageGeneratorSetting()
		{
			editorTest_CurrentStageGeneratorSetting = null;
		}



#endif



		///======================================================================================================================================================



		//? 스테이지 생성 정보



		private StageGenerator.Space currentSpace = null;

		/// <summary>
		/// 이 <see cref="RoomObject"/>이 속해있는 <see cref="StageGenerator.Space"/><br/>
		/// </summary>
		[TitleGroup("방의 스테이지 생성 정보")]
		[LabelText("이 방이 속해있는 Space")]
		[ShowInInspector, Sirenix.OdinInspector.ReadOnly]
		[PropertyOrder(0)]
		public StageGenerator.Space CurrentSpace { get => currentSpace; set => currentSpace = value; }



		private HashSet<StageGenerator.Grid> currentGrids = null;

		/// <summary>
		/// 이 <see cref="RoomObject"/>이 속해있는 <see cref="StageGenerator.Grid"/>들<br/>
		/// </summary>
		[TitleGroup("방의 스테이지 생성 정보")]
		[LabelText("이 방에 속해있는 Grid들")]
		[ShowInInspector, Sirenix.OdinInspector.ReadOnly]
		[PropertyOrder(0)]
		public HashSet<StageGenerator.Grid> CurrentGrids => currentGrids ??= new HashSet<StageGenerator.Grid>();



		#region Activated Rect



		[TitleGroup("방의 스테이지 생성 정보")]
		[LabelText("인스턴스 방 Activated Rect 할당 여부")]
		[LabelWidth(200)]
		[Sirenix.OdinInspector.ReadOnly]
		[PropertyOrder(0)]
		[SerializeField] private bool Applyed_InstanceRoomActivatedRect;



		[TitleGroup("방의 스테이지 생성 정보")]
		[LabelText("인스턴스 방 Activated Rect")]
		[ShowIf(nameof(Applyed_InstanceRoomActivatedRect))]
		[Sirenix.OdinInspector.ReadOnly]
		[PropertyOrder(0)]
		[Indent(1)]
		[SerializeField] private CustomRect2DRelative instanceRoomActivatedRect;
		public CustomRect2DRelative InstanceRoomActivatedRect => instanceRoomActivatedRect;



		public CustomRect2DCentered GetInstanceRoomActivatedTransformRect(StageGeneratorSetting setting)
		{
			var roomActivatedTransformRect = new CustomRect2DCentered(Vector2.zero, instanceRoomActivatedRect);
			roomActivatedTransformRect.Center = GridCenterTransformPositionVector2;
			roomActivatedTransformRect.Offset = roomActivatedTransformRect.Offset.Multiply(setting.SnapSetting.GridUnitOriginalVector2);
			roomActivatedTransformRect.Size = roomActivatedTransformRect.Size.Multiply(setting.SnapSetting.GridUnitOriginalVector2);
			return roomActivatedTransformRect;
		}



		[TitleGroup("방의 스테이지 생성 정보")]
		[LabelText("인스턴스 방 Activated Rect Expand")]
		[ShowIf(nameof(Applyed_InstanceRoomActivatedRect))]
		[Sirenix.OdinInspector.ReadOnly]
		[PropertyOrder(0)]
		[Indent(1)]
		[SerializeField] private CustomRect2DRelative instanceRoomActivatedRectExpand;
		public CustomRect2DRelative InstanceRoomActivatedRectExpand => instanceRoomActivatedRectExpand;



		public CustomRect2DCentered GetInstanceRoomActivatedTransformRectExpand(StageGeneratorSetting setting)
		{
			var roomActivatedTransformRect = new CustomRect2DCentered(Vector2.zero, instanceRoomActivatedRectExpand);
			roomActivatedTransformRect.Center = GridCenterTransformPositionVector2;
			roomActivatedTransformRect.Offset = roomActivatedTransformRect.Offset.Multiply(setting.SnapSetting.GridUnitOriginalVector2);
			roomActivatedTransformRect.Size = roomActivatedTransformRect.Size.Multiply(setting.SnapSetting.GridUnitOriginalVector2);
			return roomActivatedTransformRect;
		}



		/// <summary>
		/// 인스턴스 방 Activated Rect, 확장 적용하기
		/// </summary>
		/// <param name="instanceRoomActivatedRect"></param>
		/// <returns></returns>
		public void ApplyInstanceRoomActivatedRect(CustomRect2DRelative instanceRoomActivatedRect, CustomRect2DRelative instanceRoomActivatedRectExpand)
		{
			this.instanceRoomActivatedRect = instanceRoomActivatedRect;
			this.instanceRoomActivatedRectExpand = instanceRoomActivatedRectExpand;
			Applyed_InstanceRoomActivatedRect = true;
		}



		/// <summary>
		/// 인스턴스 방 Activated Rect들 제거하기
		/// </summary>
		/// <param name="instanceRoomActivatedRect"></param>
		/// <returns></returns>
		public void RemoveInstanceRoomActivatedRects()
		{
			instanceRoomActivatedRect = default;
			instanceRoomActivatedRectExpand = default;
			Applyed_InstanceRoomActivatedRect = false;
		}



		/// <summary>
		/// 인스턴스 방 Activated Rect 얻어보기
		/// </summary>
		/// <param name="instanceRoomActivatedRect"></param>
		/// <returns></returns>
		public bool TryGetInstanceRoomActivatedRect(out CustomRect2DRelative instanceRoomActivatedRect)
		{
			if (Applyed_InstanceRoomActivatedRect)
			{
				instanceRoomActivatedRect = this.instanceRoomActivatedRect;
				return true;
			}
			instanceRoomActivatedRect = default;
			return false;
		}




		/// <summary>
		/// 인스턴스 방 Activated Rect Expand 얻어보기
		/// </summary>
		/// <param name="instanceRoomActivatedRect"></param>
		/// <returns></returns>
		public bool TryGetInstanceRoomActivatedRectExpand(out CustomRect2DRelative instanceRoomActivatedRectExpand)
		{
			if (Applyed_InstanceRoomActivatedRect)
			{
				instanceRoomActivatedRectExpand = this.instanceRoomActivatedRectExpand;
				return true;
			}
			instanceRoomActivatedRectExpand = default;
			return false;
		}



		/// <summary>
		/// 인스턴스 방 Activated Rect들 얻어보기
		/// </summary>
		/// <param name="instanceRoomActivatedRect"></param>
		/// <returns></returns>
		public bool TryGetInstanceRoomActivatedRects(out CustomRect2DRelative instanceRoomActivatedRect, out CustomRect2DRelative instanceRoomActivatedRectExpand)
		{
			instanceRoomActivatedRect = default;
			instanceRoomActivatedRectExpand = default;
			return TryGetInstanceRoomActivatedRect(out instanceRoomActivatedRect) && TryGetInstanceRoomActivatedRectExpand(out instanceRoomActivatedRectExpand);
		}



		#endregion



		/// <summary>
		/// 이 방이 가지고있는 스테이지 생성 정보 초기화
		/// </summary>
		public void ClearStageGenerateInfo()
		{
			currentSpace = null;
			currentGrids?.Clear();
			Applyed_InstanceRoomActivatedRect = false;
			instanceRoomActivatedRect = default;
			instanceRoomActivatedRectExpand = default;
		}



		///======================================================================================================================================================



		///<summary>
		/// 받아온 공간 안에 이 방을 생성 할수 있는지 확인한다
		/// <para>스테이지 설정을 고려하며, 생성이 가능하다면 해당 공간의 조건에 일치하는 방 Activated Rect와 함께 성공을 반환한다</para>
		///</summary>
		public bool Confirm_PlaceSpaceInfo(StageGenerator main, StageGeneratorSetting setting, StageGenerator.Space space, out CustomRect2DRelative? roomActivatedRect, out CustomRect2DRelative? roomActivatedRectExpand)
		{
			//! 여기서 사용되는 방은 생성된 방이 아닌, 생성되기 전 (프리팹) 받아오는 방
			roomActivatedRect = null; //. (실패 할때마다 null 반환하는것이 귀찮으니, 기본값을 null)
			roomActivatedRectExpand = null; //. (실패 할때마다 null 반환하는것이 귀찮으니, 기본값을 null)


			//. 방의 방향별 도어를 확인하여, 공간의 노드에 충족이 가능한지 확인한다


			//! 방의 "하/상/좌/우" 방향의 문의 개수가, 공간의 "하/상/좌/우" 노드 개수보다 작다면 실패, false 반환
			if (roomDoorM.GetDoorList(EDirection4.Down).Count < space.CurrentNodeCount_Down) { return false; }
			if (roomDoorM.GetDoorList(EDirection4.Up).Count < space.CurrentNodeCount_Up) { return false; }
			if (roomDoorM.GetDoorList(EDirection4.Left).Count < space.CurrentNodeCount_Left) { return false; }
			if (roomDoorM.GetDoorList(EDirection4.Right).Count < space.CurrentNodeCount_Right) { return false; }


			//. 받아온 공간에 충족하는 방 Activated Rect를 구한다, 가능한 "최소치"를 구한다
			//.     여기서는 설정에 의해 확장되기 이전인, 순수한 이 방의 최소치 Rect를 구한다
			var _roomActivatedRect = TotalRoomRectMin(
				space.CurrentNodeCount_Down,
				space.CurrentNodeCount_Up,
				space.CurrentNodeCount_Left,
				space.CurrentNodeCount_Right);


			//. 스테이지 설정에 의한 Rect 확장 연산
			//.     "방 Activated Rect 확장" 을 구하며, 이것을 기준으로 충족하는지 확인한다
			CustomRect2DRelative _roomActivatedRectExpand = _roomActivatedRect;
			ExpandTotalRoomRectInternal(setting, ref _roomActivatedRectExpand, space.CurrentNodeCount_Down > 0, space.CurrentNodeCount_Up > 0, space.CurrentNodeCount_Left > 0, space.CurrentNodeCount_Right > 0);


			//? 방 Activated Rect 확장에 추가 보정 크기 적용
			_roomActivatedRectExpand.Size += main.Setting.Room.RoomPlacementSizeCorrection;


			//! 방 Activated Rect 확장의 너비 확인, 미충족시 실패
			float room_Width = _roomActivatedRectExpand.Size.x;
			//!		복도 안전구역을 사용한다면, 그만큼 더 확장시킨다
			if (setting.Hallawy.UseCreateHallwaySafeArea) { room_Width += setting.Hallawy.CurrentHallwaySafeAreaLength; }
			if (room_Width > space.SpaceRect.width) { return false; }


			//! 방 Activated Rect 확장의 높이 확인, 미충족시 실패
			float room_Height = _roomActivatedRectExpand.Size.x;
			//!		복도 안전구역을 사용한다면, 그만큼 더 확장시킨다
			if (setting.Hallawy.UseCreateHallwaySafeArea) { room_Height += setting.Hallawy.CurrentHallwaySafeAreaLength; }
			if (room_Height > space.SpaceRect.height) { return false; }


			//? 필터링 성공, Activated Rect들 반환
			roomActivatedRect = _roomActivatedRect;
			roomActivatedRectExpand = _roomActivatedRectExpand;


			return true;
		}



		///======================================================================================================================================================



		protected override void Awake()
		{
			base.Awake();
			ManagersWakeUp(false);

			currentSpace = null;
			currentGrids = null;
		}



		///<summary>
		///내부 매니저들을 WakeUp
		///</summary>
		protected void ManagersWakeUp(bool absolute)
		{
			if (absolute || !roomDoorM.IsWakeUp) { roomDoorM.WakeUp(this); }
			if (absolute || !roomVariationM.IsWakeUp) { roomVariationM.WakeUp(this); }
		}



		/// <summary>
		/// 강제로 외부에서 <see cref="Awake"/> 호출
		/// </summary>
		internal void ForceAwake()
		{
			Awake();
		}



		///======================================================================================================================================================
	}
}