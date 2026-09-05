#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pan.Util;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using Pan.GridCompatibles2;
using Pan.StageGenerators;
using System;
using Pan.Util.Editors;
using RoomObject = Pan.StageGenerators.RoomObject;
using System.Linq;

using Sirenix.OdinInspector;



namespace Pan.StageGenerators
{

	public partial class RoomObject
	{
		private readonly RoomObject_Gizmo roomGizmo = new();



		[BoxGroup("기즈모박스그룹", false)]
		[LabelText(" Current 방 기즈모 사용", Icon = SdfIconType.BorderAll)]
		[ShowInInspector]
		[PropertyOrder(9999)]
		private bool UseGizmo_RoomObject { get => roomGizmo.useGizmo; set => roomGizmo.useGizmo = value; }



		protected override void OnDrawGizmos()
		{
			roomGizmo.OnDrawGizmos(this); //. 그리드 호환 기즈모 보다 먼저 그리도록 한다
			base.OnDrawGizmos();
		}



		[Serializable]
		private class RoomObject_Gizmo : BaseGizmoClass<RoomObject>
		{
			///======================================================================================================================================================



			private void CalculateTransformRectForGizmo(GridCompatible2Object main, CustomRect2DRelative rect, out Vector2 transformCenter, out Vector2 transformSize)
			{
				transformCenter = (main.GridCenterTransformPositionVector2 + rect.Offset.Multiply(main.GridCompatible.CurrentSnapSetting.GridUnitOriginalVector2));
				transformSize = (rect.Size.Multiply(main.GridCompatible.CurrentSnapSetting.GridUnitOriginalVector2));
			}



			private void CalculateTransformCurrentRectForGizmo(GridCompatible2Object main, CustomRect2DRelative rect, out Vector3 transformCurrentCenter, out Vector3 transformCurrentSize)
			{
				CalculateTransformRectForGizmo(main, rect, out var _transformCurrentCenter, out var _transformCurrentSize);
				transformCurrentCenter = _transformCurrentCenter.SwizzlesVector2To3(main.GridCompatible.CurrentSnapSetting.Swizzle);
				transformCurrentCenter += FloorStandardPositionCurrent;
				transformCurrentSize = _transformCurrentSize.SwizzlesVector2To3(main.GridCompatible.CurrentSnapSetting.Swizzle);
			}



			///======================================================================================================================================================



			private static GUIStyle guiStyle;
			private static GUIStyle GuiStyle
			{
				get
				{
					if (guiStyle == null)
					{
						guiStyle = new GUIStyle
						{
							alignment = TextAnchor.MiddleCenter,
							fontSize = 12,
							normal = new GUIStyleState { textColor = Color.white },
							richText = true
						};
					}

					return guiStyle;
				}
			}


			bool IsObjectSelected_WithChild;
			bool IsObjectSelected_WithParent;



			Vector3 FloorStandardPositionCurrent;



			///======================================================================================================================================================



			protected override void DrawGizmo(RoomObject main)
			{
				if (StageGeneratorSingletonSettingSbject.O.UseRoomGizmo == false) { return; }

				//. 오브젝트 선택 여부 갱신
				IsObjectSelected_WithChild = SU_EditorControl.IsObjectSelected(main.transform, true, true, true);
				IsObjectSelected_WithParent = SU_EditorControl.IsObjectSelected(main.transform, true, true, false);

				if (StageGeneratorSingletonSettingSbject.O.UseRoomGizmoOnlySelect && !IsObjectSelected_WithChild) { return; }


				FloorStandardPositionCurrent = main.GridCompatible.CurrentSnapSetting.GetFloorStandardPositionCurrent;
				var snapSetting = main.GridCompatible.CurrentSnapSetting;
				var swizzle = snapSetting.Swizzle;

				DrawGizmo_TotalRoomRects(main, snapSetting, swizzle);
				DrawGizmo_TotalRoomRectViewer(main, snapSetting, swizzle);
				DrawGizmo_Doors(main, snapSetting, swizzle);
			}



			//? RoomRect 그리기, 도어에 의해서 확장된 "통합 방 Rect"를 그린다
			private void DrawGizmo_TotalRoomRects(RoomObject main, GridCompatible2SnapSetting snapSetting, GridLayout.CellSwizzle swizzle)
			{
				//! 도어가 없으면 그리지 않음
				if (main.RoomDoorM.DoorTotalCount <= 0) { return; }


				////. Z축 보정
				//var depthZCorrection = GridCompatible2SingletonSettingSbject.O.GetCorrectionSizeDepthZLength(main.GridCompatible, false);


				//. 방 Rect (그리드호환 기즈모에서 그리므로 겹치지 않게 그리기 위해 가용)
				var roomTransformRect = main.ObjectTransformRect;


				drawTotalRoomRect();
				void drawTotalRoomRect()
				{
					//? 인스턴스 Activated Rect가 할당되어있을경우, 그것을 그린다
					if (main.TryGetInstanceRoomActivatedRects(out var instanceRoomActivatedRect, out var instanceRoomActivatedRectExpand))
					{
						//. 테두리
						if (StageGeneratorSingletonSettingSbject.O.RoomRectColor.TryGetColor(out var totalRoomRectColor))
						{
							Gizmos.color = totalRoomRectColor;
							CalculateTransformCurrentRectForGizmo(main, instanceRoomActivatedRect, out var roomActivatedRectTransformCurrentCenter, out var roomActivatedRectTransformCurrentSize);
							CalculateTransformCurrentRectForGizmo(main, instanceRoomActivatedRectExpand, out var roomActivatedRectExpandTransformCurrentCenter, out var roomActivatedRectExpandTransformCurrentSize);

							Gizmos.DrawWireCube(roomActivatedRectTransformCurrentCenter, roomActivatedRectTransformCurrentSize);

							SU_Gizmo.DrawDottedWireCube(roomActivatedRectExpandTransformCurrentCenter, SU_TF_Vector.OffsetUniform(roomActivatedRectExpandTransformCurrentSize, 0.05f));
						}
					}


					//? 그 외에는 통합 맥시멈 Min,Max 를 그린다
					else
					{
						//. 통합 맥시멈 방 Rect Min
						var totalRoomRectMin = main.TotalRoomRectMin_Maximum;
						//CalculateTransformRectForGizmo(main, totalRoomRectMin, out var totalRoomRectMin_Center, out var totalRoomRectMin_Size);
						CalculateTransformCurrentRectForGizmo(main, totalRoomRectMin, out var totalRoomRectMin_TransformCurrentCenter, out var totalRoomRectMin_TransformCurrentSize);
						//var totalRoomRectMin_Rect = SU_TF_Rect.RectFromCenter(totalRoomRectMin_Center, totalRoomRectMin_Size);
						//var totalRoomRectMin_SbtractRectList = SU_TF_Rect.SubtractRect(totalRoomRectMin_Rect, roomTransformRect);


						//. 통합 맥시멈 방 Rect Max
						var totalRoomRectMax = main.TotalRoomRectMax_Maximum;
						CalculateTransformRectForGizmo(main, totalRoomRectMax, out var totalRoomRectMax_Center, out var totalRoomRectMax_Size);
						CalculateTransformCurrentRectForGizmo(main, totalRoomRectMax, out var totalRoomRectMax_TransformCurrentCenter, out var totalRoomRectMax_TransformCurrentSize);
						var totalRoomRectMax_Rect = SU_TF_Rect.RectFromCenter(totalRoomRectMax_Center, totalRoomRectMax_Size);
						var totalRoomRectMax_SbtractRectList = SU_TF_Rect.SubtractRect(totalRoomRectMax_Rect, roomTransformRect);


						//? 방 Rect Min 리스트 + Rect Max 리스트를 합쳐서 순회 시작, 내부 그리드 격자를 그린다
						if (IsObjectSelected_WithChild)
						{
							foreach (var item in totalRoomRectMax_SbtractRectList)
							{
								//Gizmos.color = GridCompatible2SingletonSettingSbject.O.GridLineColor.WithMultipliedAlpha(0.2f);
								SU_Gizmo.DrawGridLine2D(item.center.SwizzlesVector2To3(swizzle) + FloorStandardPositionCurrent, item.size, main.GridCompatible.CurrentSnapSetting.GridUnitOriginalVector2, main.GridCompatible.CurrentSnapSetting.Swizzle, true);
							}
						}


						if (StageGeneratorSingletonSettingSbject.O.RoomRectColor.TryGetColor(out var totalRoomRectColor))
						{
							Gizmos.color = totalRoomRectColor;

							//. Min 맥시멈 그리기
							SU_Gizmo.DrawDottedWireCube(totalRoomRectMin_TransformCurrentCenter, totalRoomRectMin_TransformCurrentSize);
							//. Max 맥시멈 그리기
							Gizmos.DrawWireCube(totalRoomRectMax_TransformCurrentCenter, totalRoomRectMax_TransformCurrentSize);
						}
					}
				}
			}



			//? 테스트 용도의 통합 방 Rect 뷰어를 그린다
			private void DrawGizmo_TotalRoomRectViewer(RoomObject main, GridCompatible2SnapSetting snapSetting, GridLayout.CellSwizzle swizzle)
			{
				if (!(main.editor_RoomRect_UseDown || main.editor_RoomRect_UseUp || main.editor_RoomRect_UseLeft || main.editor_RoomRect_UseRight)) { return; }

				if (!StageGeneratorSingletonSettingSbject.O.RoomRectViewerColor.TryGetColor(out var roomRectViewerColor)) { return; }

				if (IsObjectSelected_WithParent)
				{
					var depthZCorrection = GridCompatible2SingletonSettingSbject.O.GetCorrectionSizeDepthZLength(main.GridCompatible, false);
					var depthZCorrectionHalf = depthZCorrection * 0.5f;
					var textCorrection = GridCompatible2SingletonSettingSbject.O.GetGizmoTextCorrectionDistance(main.GridCompatible) * 2f;// + (new Vector3(0, main.GridCompatible.CurrentSnapSetting.GridUnitY_Height * 0.5f, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle));
					var markRadius = GridCompatible2SingletonSettingSbject.O.GetGizmoMarkRadius(main.GridCompatible);


					//. 방 Transform Rect, 방 크기
					var roomTransformRect = main.ObjectTransformRect;
					var roomSize = main.GridCompatible.ObjectSizeOriginalVector2;


					//. 최소 Rect 크기, 트랜스폼 연산
					var totalRoomRectMinViewer = main.TestTotalRoomRectMin_Viewer;
					var totalRoomRectMin_TransformCenter = main.GridCenterTransformPositionVector2 + totalRoomRectMinViewer.Offset.Multiply(snapSetting.GridUnitOriginalVector2);
					var totalRoomRectMin_TransformSize = totalRoomRectMinViewer.Size.Multiply(snapSetting.GridUnitOriginalVector2);
					var totalRoomRectMin_TransformCurrentCenter = totalRoomRectMin_TransformCenter.SwizzlesVector2To3(swizzle);
					var totalRoomRectMin_TransformCurrentSize = totalRoomRectMin_TransformSize.SwizzlesVector2To3(swizzle);
					var calculatedRoomTransformRectMin = SU_TF_Rect.RectFromCenter(totalRoomRectMin_TransformCenter, totalRoomRectMin_TransformSize);


					//! 최소 Rect크기가 방 Rect 크기와 같다면, 그리지 않음 (최적화)
					if (Mathf.Approximately(totalRoomRectMinViewer.Size.x, roomSize.x) && Mathf.Approximately(totalRoomRectMinViewer.Size.y, roomSize.y)) { return; }


					//. 최대 Rect 크기, 트랜스폼 연산
					var totalRoomRectMaxViewer = main.TestTotalRoomRectMax_Viewer;
					var totalRoomRectMax_TransformCenter = main.GridCenterTransformPositionVector2 + totalRoomRectMaxViewer.Offset.Multiply(snapSetting.GridUnitOriginalVector2);
					var totalRoomRectMax_TransformSize = totalRoomRectMaxViewer.Size.Multiply(snapSetting.GridUnitOriginalVector2);
					var totalRoomRectMax_TransformCurrentCenter = totalRoomRectMax_TransformCenter.SwizzlesVector2To3(swizzle) + FloorStandardPositionCurrent;
					var totalRoomRectMax_TransformCurrentSize = totalRoomRectMax_TransformSize.SwizzlesVector2To3(swizzle);
					var calculatedRoomTransformRectMax = SU_TF_Rect.RectFromCenter(totalRoomRectMax_TransformCenter, totalRoomRectMax_TransformSize);


					//. 최소 Rect 크기와 최대 Rect 크기가 같은지 의 여부
					bool rectSameMinMax = Mathf.Approximately(totalRoomRectMinViewer.Size.x, totalRoomRectMaxViewer.Size.x) && Mathf.Approximately(totalRoomRectMinViewer.Size.y, totalRoomRectMaxViewer.Size.y);


					//? 최소 Rect의 크기가 최대 Rect 와 같지 않다면, 별도로 최소 Rect를 그린다
					if (!rectSameMinMax)
					{
						Gizmos.color = roomRectViewerColor;
						Gizmos.DrawWireCube(totalRoomRectMin_TransformCurrentCenter, totalRoomRectMin_TransformCurrentSize);
						SU_Gizmo.DrawDottedWireCube(totalRoomRectMin_TransformCurrentCenter, totalRoomRectMin_TransformCurrentSize + (main.GridCompatible.CurrentSnapSetting.GridUnitFlatCurrent * 0.2f));
					}


					//. 최대 Rect 그리기
					Gizmos.color = roomRectViewerColor;
					Gizmos.DrawWireCube(totalRoomRectMax_TransformCurrentCenter, totalRoomRectMax_TransformCurrentSize);
					Gizmos.color = roomRectViewerColor.WithMultipliedAlpha(0.25f);
					SU_Gizmo.DrawDottedWireCube(totalRoomRectMax_TransformCurrentCenter, totalRoomRectMax_TransformCurrentSize + (main.GridCompatible.CurrentSnapSetting.GridUnitFlatCurrent * 0.2f));
					Gizmos.color = roomRectViewerColor.WithMultipliedAlpha(0.075f);
					Gizmos.DrawCube(totalRoomRectMax_TransformCurrentCenter + depthZCorrectionHalf, totalRoomRectMax_TransformCurrentSize + depthZCorrection);

					//Gizmos.color = Color.cyan;
					//Gizmos.DrawWireCube(totalRoomRectMin_TransformCurrentCenter, Vector3.one * 0.5f);
					//Gizmos.DrawWireCube(totalRoomRectMax_TransformCurrentCenter, Vector3.one * 0.75f);


					//. 최대 Rect를 기준으로 그리드 격자 그리기
					drawGridLine();
					void drawGridLine()
					{
						//Gizmos.color = GridCompatible2SingletonSettingSbject.O.GridLineColor.WithMultipliedAlpha(0.2f);
						////Gizmos.color = Color.red;


						//var rectList = new List<Rect>(main.roomDoorM.DoorTotalCount + 1);
						//rectList.Add(roomTransformRect);
						//foreach (var door in main.roomDoorM.GetTotalDoorList())
						//{
						//    rectList.Add(door.FullDoorTransformRect);
						//}
						//var subRactRects = SU_TF_Rect.SubtractRects(totalRoomRectMaxViewer.Rect, rectList);
						//foreach (var item in subRactRects)
						//{
						//    //CalculateRectForGizmo(main, item, out var center, out var size);
						//    CalculateTransformRectForGizmo(main, item, out var transformCenter, out var transformSize, out var transformRect);
						//    SU_Gizmo.DrawGridLine2D(transformCenter, transformRect.size, main.GridCompatible.CurrentSnapSetting.GridUnitOriginalVector2, main.GridCompatible.CurrentSnapSetting.Swizzle, false);
						//}
						////Debug.Log(subRactRects.Count);

					}


					//. Rect 중심점 그리기
					Gizmos.color = roomRectViewerColor;
					if (!rectSameMinMax)
					{
						SU_Gizmo.DrawCircleX(totalRoomRectMin_TransformCurrentCenter, markRadius, main.GridCompatible.CurrentSnapSetting.Swizzle);
						Handles.Label(totalRoomRectMin_TransformCurrentCenter + textCorrection, $"Min\n<b>{totalRoomRectMin_TransformCurrentCenter}</b>", GuiStyle);
					}


					SU_Gizmo.DrawCross(totalRoomRectMax_TransformCurrentCenter, new Vector3(markRadius, markRadius, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle) * 2f);
					Handles.Label(totalRoomRectMax_TransformCurrentCenter + textCorrection, $"{(!rectSameMinMax ? "Max" : "Min=Max")}\n<b>{totalRoomRectMax_TransformCurrentCenter}</b>", GuiStyle);


					//? 하단 화살표
					if (main.editor_RoomRect_UseDown && roomTransformRect.yMin > calculatedRoomTransformRectMin.yMin)
					{
						Gizmos.color = roomRectViewerColor;
						float lengthMin = roomTransformRect.yMin - calculatedRoomTransformRectMin.yMin;
						float lengthMin_GridUnit = (roomSize.y * -0.5f) - totalRoomRectMinViewer.yMin;

						var arrowStart_Min = roomTransformRect.GetPosition(ECenterStandard.LowerCenter).SwizzlesVector2To3(main.GridCompatible.CurrentSnapSetting.Swizzle) + FloorStandardPositionCurrent;
						var arrowEnd_Min = arrowStart_Min + new Vector3(0, -lengthMin, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);

						SU_Gizmo.DrawArrow(arrowStart_Min, arrowEnd_Min);

						//. 화살표 텍스트
						var textPositionMin = Vector3.Lerp(arrowStart_Min, arrowEnd_Min, 0.5f);
						SU_Gizmo.DrawCircle(textPositionMin, GridCompatible2SingletonSettingSbject.O.GetGizmoMarkRadius(main.GridCompatible), main.GridCompatible.CurrentSnapSetting.Swizzle);
						textPositionMin += new Vector3(main.GridCompatible.CurrentSnapSetting.GridUnitX_Width * 0.5f, 0, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);
						Handles.Label(textPositionMin, $"+{lengthMin_GridUnit}", GuiStyle);


						//? 최대 화살표
						if (calculatedRoomTransformRectMin.yMin > calculatedRoomTransformRectMax.yMin)
						{
							float lengthMax = calculatedRoomTransformRectMin.yMin - calculatedRoomTransformRectMax.yMin;
							float lengthMax_GridUnit = totalRoomRectMinViewer.yMin - totalRoomRectMaxViewer.yMin;

							var arrowEnd_Max = arrowEnd_Min + new Vector3(0, -lengthMax, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);

							SU_Gizmo.DrawArrow(arrowEnd_Min, arrowEnd_Max);

							//. 화살표 텍스트
							var textPositionMax = Vector3.Lerp(arrowEnd_Min, arrowEnd_Max, 0.5f);
							SU_Gizmo.DrawCircle(textPositionMax, GridCompatible2SingletonSettingSbject.O.GetGizmoMarkRadius(main.GridCompatible), main.GridCompatible.CurrentSnapSetting.Swizzle);
							textPositionMax += new Vector3(main.GridCompatible.CurrentSnapSetting.GridUnitX_Width * 0.5f, 0, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);
							Handles.Label(textPositionMax, $"+{lengthMax_GridUnit}\n(+{lengthMin_GridUnit + lengthMax_GridUnit})", GuiStyle);
						}
					}


					//? 상단 화살표
					if (main.editor_RoomRect_UseUp && roomTransformRect.yMax < calculatedRoomTransformRectMin.yMax)
					{
						Gizmos.color = roomRectViewerColor;
						float lengthMin = calculatedRoomTransformRectMin.yMax - roomTransformRect.yMax;
						float lengthMin_GridUnit = (roomSize.y * -0.5f) + totalRoomRectMinViewer.yMax;

						var arrowStart_Min = roomTransformRect.GetPosition(ECenterStandard.UpperCenter).SwizzlesVector2To3(main.GridCompatible.CurrentSnapSetting.Swizzle) + FloorStandardPositionCurrent;
						var arrowEnd_Min = arrowStart_Min + new Vector3(0, lengthMin, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);

						SU_Gizmo.DrawArrow(arrowStart_Min, arrowEnd_Min);

						//. 화살표 텍스트
						var textPositionMin = Vector3.Lerp(arrowStart_Min, arrowEnd_Min, 0.5f);
						SU_Gizmo.DrawCircle(textPositionMin, GridCompatible2SingletonSettingSbject.O.GetGizmoMarkRadius(main.GridCompatible), main.GridCompatible.CurrentSnapSetting.Swizzle);
						textPositionMin += new Vector3(main.GridCompatible.CurrentSnapSetting.GridUnitX_Width * 0.5f, 0, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);
						Handles.Label(textPositionMin, $"+{lengthMin_GridUnit}", GuiStyle);


						//? 최대 화살표
						if (calculatedRoomTransformRectMin.yMax < calculatedRoomTransformRectMax.yMax)
						{
							float lengthMax = calculatedRoomTransformRectMax.yMax - calculatedRoomTransformRectMin.yMax;
							float lengthMax_GridUnit = totalRoomRectMaxViewer.yMax - totalRoomRectMinViewer.yMax;

							var arrowEnd_Max = arrowEnd_Min + new Vector3(0, lengthMax, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);

							SU_Gizmo.DrawArrow(arrowEnd_Min, arrowEnd_Max);

							//. 화살표 텍스트
							var textPositionMax = Vector3.Lerp(arrowEnd_Min, arrowEnd_Max, 0.5f);
							SU_Gizmo.DrawCircle(textPositionMax, GridCompatible2SingletonSettingSbject.O.GetGizmoMarkRadius(main.GridCompatible), main.GridCompatible.CurrentSnapSetting.Swizzle);
							textPositionMax += new Vector3(main.GridCompatible.CurrentSnapSetting.GridUnitX_Width * 0.5f, 0, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);
							Handles.Label(textPositionMax, $"+{lengthMax_GridUnit}\n(+{lengthMin_GridUnit + lengthMax_GridUnit})", GuiStyle);
						}
					}


					//? 좌측 화살표
					if (main.editor_RoomRect_UseLeft && roomTransformRect.xMin > calculatedRoomTransformRectMin.xMin)
					{
						Gizmos.color = roomRectViewerColor;
						float lengthMin = roomTransformRect.xMin - calculatedRoomTransformRectMin.xMin;
						float lengthMin_GridUnit = (roomSize.y * -0.5f) - totalRoomRectMinViewer.xMin;

						var arrowStart_Min = roomTransformRect.GetPosition(ECenterStandard.MiddleLeft).SwizzlesVector2To3(main.GridCompatible.CurrentSnapSetting.Swizzle) + FloorStandardPositionCurrent;
						var arrowEnd_Min = arrowStart_Min + new Vector3(-lengthMin, 0, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);

						SU_Gizmo.DrawArrow(arrowStart_Min, arrowEnd_Min);

						//. 화살표 텍스트
						var textPositionMin = Vector3.Lerp(arrowStart_Min, arrowEnd_Min, 0.5f);
						SU_Gizmo.DrawCircle(textPositionMin, GridCompatible2SingletonSettingSbject.O.GetGizmoMarkRadius(main.GridCompatible), main.GridCompatible.CurrentSnapSetting.Swizzle);
						textPositionMin += new Vector3(main.GridCompatible.CurrentSnapSetting.GridUnitX_Width * 0.5f, 0, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);
						Handles.Label(textPositionMin, $"+{lengthMin_GridUnit}", GuiStyle);


						//? 최대 화살표
						if (calculatedRoomTransformRectMin.xMin > calculatedRoomTransformRectMax.xMin)
						{
							float lengthMax = calculatedRoomTransformRectMin.xMin - calculatedRoomTransformRectMax.xMin;
							float lengthMax_GridUnit = totalRoomRectMinViewer.xMin - totalRoomRectMaxViewer.xMin;

							var arrowEnd_Max = arrowEnd_Min + new Vector3(-lengthMax, 0, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);

							SU_Gizmo.DrawArrow(arrowEnd_Min, arrowEnd_Max);

							//. 화살표 텍스트
							var textPositionMax = Vector3.Lerp(arrowEnd_Min, arrowEnd_Max, 0.5f);
							SU_Gizmo.DrawCircle(textPositionMax, GridCompatible2SingletonSettingSbject.O.GetGizmoMarkRadius(main.GridCompatible), main.GridCompatible.CurrentSnapSetting.Swizzle);
							textPositionMax += new Vector3(main.GridCompatible.CurrentSnapSetting.GridUnitX_Width * 0.5f, 0, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);
							Handles.Label(textPositionMax, $"+{lengthMax_GridUnit}\n(+{lengthMin_GridUnit + lengthMax_GridUnit})", GuiStyle);
						}
					}


					//? 우측 화살표
					if (main.editor_RoomRect_UseRight && roomTransformRect.xMax < calculatedRoomTransformRectMin.xMax)
					{
						Gizmos.color = roomRectViewerColor;
						float lengthMin = calculatedRoomTransformRectMin.xMax - roomTransformRect.xMax;
						float lengthMin_GridUnit = (roomSize.y * -0.5f) + totalRoomRectMinViewer.xMax;

						var arrowStart_Min = roomTransformRect.GetPosition(ECenterStandard.MiddleRight).SwizzlesVector2To3(main.GridCompatible.CurrentSnapSetting.Swizzle) + FloorStandardPositionCurrent;
						var arrowEnd_Min = arrowStart_Min + new Vector3(lengthMin, 0, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);

						SU_Gizmo.DrawArrow(arrowStart_Min, arrowEnd_Min);

						//. 화살표 텍스트
						var textPositionMin = Vector3.Lerp(arrowStart_Min, arrowEnd_Min, 0.5f);
						SU_Gizmo.DrawCircle(textPositionMin, GridCompatible2SingletonSettingSbject.O.GetGizmoMarkRadius(main.GridCompatible), main.GridCompatible.CurrentSnapSetting.Swizzle);
						textPositionMin += new Vector3(main.GridCompatible.CurrentSnapSetting.GridUnitX_Width * 0.5f, 0, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);
						Handles.Label(textPositionMin, $"+{lengthMin_GridUnit}", GuiStyle);


						//? 최대 화살표
						if (calculatedRoomTransformRectMin.xMax < calculatedRoomTransformRectMax.xMax)
						{
							float lengthMax = calculatedRoomTransformRectMax.xMax - calculatedRoomTransformRectMin.xMax;
							float lengthMax_GridUnit = totalRoomRectMaxViewer.xMax - totalRoomRectMinViewer.xMax;

							var arrowEnd_Max = arrowEnd_Min + new Vector3(lengthMax, 0, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);

							SU_Gizmo.DrawArrow(arrowEnd_Min, arrowEnd_Max);

							//. 화살표 텍스트
							var textPositionMax = Vector3.Lerp(arrowEnd_Min, arrowEnd_Max, 0.5f);
							SU_Gizmo.DrawCircle(textPositionMax, GridCompatible2SingletonSettingSbject.O.GetGizmoMarkRadius(main.GridCompatible), main.GridCompatible.CurrentSnapSetting.Swizzle);
							textPositionMax += new Vector3(main.GridCompatible.CurrentSnapSetting.GridUnitX_Width * 0.5f, 0, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);
							Handles.Label(textPositionMax, $"+{lengthMax_GridUnit}\n(+{lengthMin_GridUnit + lengthMax_GridUnit})", GuiStyle);
						}
					}


					#region 임시격리 (구 화살표)

					////? 상단 화살표
					//if (main.editor_RoomRect_UseUp && roomFakeRect1.yMax < totalRoomRectMinViewer.Rect.yMax)
					//{
					//    Gizmos.color = StageGeneratorSingletonSettingSbject.O.RoomRectViewerColor;

					//    float lengthMin = totalRoomRectMinViewer.Rect.yMax - roomFakeRect1.yMax;
					//    var arrowStart_Min = roomFakeRect1.GetPosition(ECenterStandard.UpperCenter).SwizzlesVector2To3(main.GridCompatible.CurrentSnapSetting.Swizzle);
					//    var arrowEnd_Min = arrowStart_Min + new Vector3(0, lengthMin, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);
					//    SU_Gizmo.DrawArrow(arrowStart_Min, arrowEnd_Min);


					//    //. 화살표 텍스트
					//    var textPositionMin = Vector3.Lerp(arrowStart_Min, arrowEnd_Min, 0.5f);
					//    SU_Gizmo.DrawCircle(textPositionMin, GridCompatible2SingletonSettingSbject.O.GetGizmoMarkRadius(main.GridCompatible), main.GridCompatible.CurrentSnapSetting.Swizzle);
					//    textPositionMin += new Vector3(main.GridCompatible.CurrentSnapSetting.GridUnitX_Width * 0.5f, 0, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);
					//    Handles.Label(textPositionMin, $"+{lengthMin}", GuiStyle);


					//    //? 최대 화살표
					//    if (totalRoomRectMinViewer.Rect.yMax < totalRoomRectMaxViewer.Rect.yMax)
					//    {
					//        float lengthMax = totalRoomRectMaxViewer.Rect.yMax - totalRoomRectMinViewer.Rect.yMax;

					//        var arrowEnd_Max = arrowEnd_Min + new Vector3(0, lengthMax, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);
					//        SU_Gizmo.DrawArrow(arrowEnd_Min, arrowEnd_Max);

					//        //. 화살표 텍스트
					//        var textPositionMax = Vector3.Lerp(arrowEnd_Min, arrowEnd_Max, 0.5f);
					//        SU_Gizmo.DrawCircle(textPositionMax, GridCompatible2SingletonSettingSbject.O.GetGizmoMarkRadius(main.GridCompatible), main.GridCompatible.CurrentSnapSetting.Swizzle);
					//        textPositionMax += new Vector3(main.GridCompatible.CurrentSnapSetting.GridUnitX_Width * 0.5f, 0, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);
					//        Handles.Label(textPositionMax, $"+{lengthMax}\n(+{lengthMin + lengthMax})", GuiStyle);
					//    }
					//}


					////? 좌측 화살표
					//if (main.editor_RoomRect_UseLeft && roomFakeRect1.xMin > totalRoomRectMinViewer.Rect.xMin)
					//{
					//    Gizmos.color = StageGeneratorSingletonSettingSbject.O.RoomRectViewerColor;

					//    float lengthMin = roomFakeRect1.xMin - totalRoomRectMinViewer.Rect.xMin;
					//    var arrowStart_Min = roomFakeRect1.GetPosition(ECenterStandard.MiddleLeft).SwizzlesVector2To3(main.GridCompatible.CurrentSnapSetting.Swizzle);
					//    var arrowEnd_Min = arrowStart_Min + new Vector3(-lengthMin, 0, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);
					//    SU_Gizmo.DrawArrow(arrowStart_Min, arrowEnd_Min);


					//    //. 화살표 텍스트
					//    var textPositionMin = Vector3.Lerp(arrowStart_Min, arrowEnd_Min, 0.5f);
					//    SU_Gizmo.DrawCircle(textPositionMin, GridCompatible2SingletonSettingSbject.O.GetGizmoMarkRadius(main.GridCompatible), main.GridCompatible.CurrentSnapSetting.Swizzle);
					//    textPositionMin += new Vector3(0, main.GridCompatible.CurrentSnapSetting.GridUnitY_Height * 0.5f, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);
					//    Handles.Label(textPositionMin, $"+{lengthMin}", GuiStyle);


					//    //? 최대 화살표
					//    if (totalRoomRectMinViewer.Rect.xMin > totalRoomRectMaxViewer.Rect.xMin)
					//    {
					//        float lengthMax = totalRoomRectMinViewer.Rect.xMin - totalRoomRectMaxViewer.Rect.xMin;

					//        var arrowEnd_Max = arrowEnd_Min + new Vector3(-lengthMax, 0, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);
					//        SU_Gizmo.DrawArrow(arrowEnd_Min, arrowEnd_Max);

					//        //. 화살표 텍스트
					//        var textPositionMax = Vector3.Lerp(arrowEnd_Min, arrowEnd_Max, 0.5f);
					//        SU_Gizmo.DrawCircle(textPositionMax, GridCompatible2SingletonSettingSbject.O.GetGizmoMarkRadius(main.GridCompatible), main.GridCompatible.CurrentSnapSetting.Swizzle);
					//        textPositionMax += new Vector3(0, main.GridCompatible.CurrentSnapSetting.GridUnitY_Height * 0.5f, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);
					//        Handles.Label(textPositionMax, $"+{lengthMax}\n(+{lengthMin + lengthMax})", GuiStyle);
					//    }
					//}


					////? 우측 화살표
					//if (main.editor_RoomRect_UseRight && roomFakeRect1.xMax < totalRoomRectMinViewer.Rect.xMax)
					//{
					//    Gizmos.color = StageGeneratorSingletonSettingSbject.O.RoomRectViewerColor;

					//    float lengthMin = totalRoomRectMinViewer.Rect.xMax - roomFakeRect1.xMax;
					//    var arrowStart_Min = roomFakeRect1.GetPosition(ECenterStandard.MiddleRight).SwizzlesVector2To3(main.GridCompatible.CurrentSnapSetting.Swizzle);
					//    var arrowEnd_Min = arrowStart_Min + new Vector3(lengthMin, 0, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);
					//    SU_Gizmo.DrawArrow(arrowStart_Min, arrowEnd_Min);


					//    //. 화살표 텍스트
					//    var textPositionMin = Vector3.Lerp(arrowStart_Min, arrowEnd_Min, 0.5f);
					//    SU_Gizmo.DrawCircle(textPositionMin, GridCompatible2SingletonSettingSbject.O.GetGizmoMarkRadius(main.GridCompatible), main.GridCompatible.CurrentSnapSetting.Swizzle);
					//    textPositionMin += new Vector3(0, main.GridCompatible.CurrentSnapSetting.GridUnitY_Height * 0.5f, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);
					//    Handles.Label(textPositionMin, $"+{lengthMin}", GuiStyle);


					//    //? 최대 화살표
					//    if (totalRoomRectMinViewer.Rect.xMax < totalRoomRectMaxViewer.Rect.xMax)
					//    {
					//        float lengthMax = totalRoomRectMaxViewer.Rect.xMax - totalRoomRectMinViewer.Rect.xMax;

					//        var arrowEnd_Max = arrowEnd_Min + new Vector3(lengthMax, 0, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);
					//        SU_Gizmo.DrawArrow(arrowEnd_Min, arrowEnd_Max);

					//        //. 화살표 텍스트
					//        var textPositionMax = Vector3.Lerp(arrowEnd_Min, arrowEnd_Max, 0.5f);
					//        SU_Gizmo.DrawCircle(textPositionMax, GridCompatible2SingletonSettingSbject.O.GetGizmoMarkRadius(main.GridCompatible), main.GridCompatible.CurrentSnapSetting.Swizzle);
					//        textPositionMax += new Vector3(0, main.GridCompatible.CurrentSnapSetting.GridUnitY_Height * 0.5f, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);
					//        Handles.Label(textPositionMax, $"+{lengthMax}\n(+{lengthMin + lengthMax})", GuiStyle);
					//    }
					//} 
					#endregion
				}
			}



			//? 도어 기즈모 그리기
			private void DrawGizmo_Doors(RoomObject main, GridCompatible2SnapSetting snapSetting, GridLayout.CellSwizzle swizzle)
			{
				var textCorrection = GridCompatible2SingletonSettingSbject.O.GetGizmoTextCorrectionDistance(main.GridCompatible);

				var gridSize = main.GridCompatible.CurrentSnapSetting.GridUnitFlatCurrent;

				drawDoorList(main, main.RoomDoorM.GetDoorList(EDirection4.Down));
				drawDoorList(main, main.RoomDoorM.GetDoorList(EDirection4.Up));
				drawDoorList(main, main.RoomDoorM.GetDoorList(EDirection4.Left));
				drawDoorList(main, main.RoomDoorM.GetDoorList(EDirection4.Right));



				void drawDoorList(RoomObject main, IReadOnlyList<RoomObject.Door> roomDoorList)
				{
					var depthZCorrection = GridCompatible2SingletonSettingSbject.O.GetCorrectionSizeDepthZLength(main.GridCompatible, false);
					var depthZCorrectionHalf = depthZCorrection * 0.5f;

					foreach (var door in roomDoorList)
					{
						var startDoorCenter = door.DoorStartTransformPositionCurrent + FloorStandardPositionCurrent;
						var endDoorCenter = door.DoorEndTransformPositionCurrent + FloorStandardPositionCurrent;
						var doorSize = door.SingleDoorSizeTransformCurrentVector3;
						var fullDoorTransformRect = door.FullDoorTransformRect;
						var fullDoorTransformCenter = fullDoorTransformRect.center.SwizzlesVector2To3(swizzle) + FloorStandardPositionCurrent;
						var fullDoorTransformSize = fullDoorTransformRect.size.SwizzlesVector2To3(swizzle);


						Color doorGridColor_Enabled = new Color();
						Color doorGridColor_Disabled = new Color();
						Color currentDoorGridColor = new Color();
						if ((door.DoorEnable && StageGeneratorSingletonSettingSbject.O.DoorGridColor_Enabled.TryGetColor(out doorGridColor_Enabled)) ||
						 (!door.DoorEnable && StageGeneratorSingletonSettingSbject.O.DoorGridColor_Disabled.TryGetColor(out doorGridColor_Disabled))
						 )
						{
							currentDoorGridColor = door.DoorEnable ? doorGridColor_Enabled : doorGridColor_Disabled;
						}


						//? 풀 도어 그리드 그리기
						drawFullDoorGrid();
						void drawFullDoorGrid()
						{
							Gizmos.color = currentDoorGridColor;
							Gizmos.DrawWireCube(fullDoorTransformCenter, fullDoorTransformSize);

							//? 선택중이라면, 그리드 격자 그리기
							if (IsObjectSelected_WithChild)
							{
								//Gizmos.color = Gizmos.color.WithMultipliedAlpha(0.15f);
								SU_Gizmo.DrawGridLine2D(fullDoorTransformCenter, fullDoorTransformRect.size, main.GridCompatible.CurrentSnapSetting.GridUnitOriginalVector2, main.GridCompatible.CurrentSnapSetting.Swizzle, false);
							}
						}


						//? 도어 입구 그리드 그리기
						drawDoorStartGrid();
						void drawDoorStartGrid()
						{
							Gizmos.color = door.DoorEnable ? currentDoorGridColor.WithMultipliedAlpha(0.2f) : currentDoorGridColor.WithMultipliedAlpha(0.1f);
							Gizmos.DrawCube(startDoorCenter + depthZCorrectionHalf, doorSize + depthZCorrection);
							Gizmos.color = door.DoorEnable ? currentDoorGridColor : currentDoorGridColor.WithMultipliedAlpha(0.5f);
							Gizmos.DrawWireCube(startDoorCenter, doorSize);
						}


						//? 도어 출구 그리드 그리기
						drawDoorEndGrid();
						void drawDoorEndGrid()
						{
							Gizmos.color = door.DoorEnable ? currentDoorGridColor.WithMultipliedAlpha(0.2f) : currentDoorGridColor.WithMultipliedAlpha(0.1f);
							Gizmos.DrawCube(endDoorCenter + depthZCorrectionHalf, doorSize + depthZCorrection);
							Gizmos.color = door.DoorEnable ? currentDoorGridColor : currentDoorGridColor.WithMultipliedAlpha(0.5f);
							Gizmos.DrawWireCube(endDoorCenter, doorSize);
						}


						//? 도어 입출구 중심점 표식 그리기
						drawDoorCenterMark();
						void drawDoorCenterMark()
						{
							//Gizmos.color = door.DoorEnable ? GridCompatible2SingletonSettingSbject.O.GridCenterColor : GridCompatible2SingletonSettingSbject.O.GridCenterColor.WithMultipliedAlpha(0.5f);
							Gizmos.color = door.DoorEnable ? currentDoorGridColor : currentDoorGridColor;

							var textColor = Gizmos.color.ToHex(false);

							var doorCenterMarkSize1 = GridCompatible2SingletonSettingSbject.O.GetGizmoMarkRadius(main.GridCompatible);
							var doorCenterMarkSize2 = doorCenterMarkSize1;
							var doorCenterMarkSize2x = doorCenterMarkSize2 * 2f;


							//? 도어  입구 표식 그리기
							door.CenterTransformCurrentPosition_Start(out var doorStart_IsDoubleCenter, out var doorStart_Center1, out var doorStart_Center2);
							door.CenterGridPosition_Start(out var _, out var doorStart_GridCenter1, out var doorStart_GridCenter2);
							if (doorStart_IsDoubleCenter)
							{
								SU_Gizmo.DrawCircle(doorStart_Center1, doorCenterMarkSize1, main.GridCompatible.CurrentSnapSetting.Swizzle);
								SU_Gizmo.DrawCircleX(doorStart_Center1, doorCenterMarkSize1, main.GridCompatible.CurrentSnapSetting.Swizzle);

								SU_Gizmo.DrawCircle(doorStart_Center2, doorCenterMarkSize2, main.GridCompatible.CurrentSnapSetting.Swizzle);
								SU_Gizmo.DrawCross(doorStart_Center2, new Vector3(doorCenterMarkSize2x, doorCenterMarkSize2x, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle));

								//? 선택중이라면 좌표도 표시
								if (IsObjectSelected_WithParent)
								{
									var textPosition1 = doorStart_Center1 + textCorrection;
									var textPosition2 = doorStart_Center2 + textCorrection;
									Handles.Label(textPosition1, $"<b><color={textColor}>[{doorStart_GridCenter1.x}, {doorStart_GridCenter1.y}]</color></b>", GuiStyle);
									Handles.Label(textPosition2, $"<b><color={textColor}>[{doorStart_GridCenter2.x}, {doorStart_GridCenter2.y}]</color></b>", GuiStyle);
								}
							}
							else
							{
								SU_Gizmo.DrawCircle(doorStart_Center1, doorCenterMarkSize1, main.GridCompatible.CurrentSnapSetting.Swizzle);
								SU_Gizmo.DrawCircleX(doorStart_Center1, doorCenterMarkSize1, main.GridCompatible.CurrentSnapSetting.Swizzle);

								//? 선택중이라면 좌표도 표시
								if (IsObjectSelected_WithParent)
								{
									var textPosition1 = doorStart_Center1 + textCorrection;
									Handles.Label(textPosition1, $"<b><color={textColor}>[{doorStart_GridCenter1.x}, {doorStart_GridCenter1.y}]</color></b>", GuiStyle);
								}
							}


							//? 도어  출구 표식 그리기
							door.CenterTransformCurrentPosition_End(out var doorEnd_IsDoubleCenter, out var doorEnd_Center1, out var doorEnd_Center2);
							door.CenterGridPosition_End(out var _, out var doorEnd_GridCenter1, out var doorEnd_GridCenter2);

							if (doorEnd_IsDoubleCenter)
							{
								SU_Gizmo.DrawCircle(doorEnd_Center1, doorCenterMarkSize1, main.GridCompatible.CurrentSnapSetting.Swizzle);
								SU_Gizmo.DrawCircleX(doorEnd_Center1, doorCenterMarkSize1, main.GridCompatible.CurrentSnapSetting.Swizzle);

								SU_Gizmo.DrawCircle(doorEnd_Center2, doorCenterMarkSize2, main.GridCompatible.CurrentSnapSetting.Swizzle);
								SU_Gizmo.DrawCross(doorEnd_Center2, new Vector3(doorCenterMarkSize2x, doorCenterMarkSize2x, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle));

								//? 선택중이라면 좌표도 표시
								if (IsObjectSelected_WithParent)
								{
									var textPosition1 = doorEnd_Center1 + textCorrection;
									var textPosition2 = doorEnd_Center2 + textCorrection;
									Handles.Label(textPosition1, $"<b><color={textColor}>[{doorEnd_GridCenter1.x}, {doorEnd_GridCenter1.y}]</color></b>", GuiStyle);
									Handles.Label(textPosition2, $"<b><color={textColor}>[{doorEnd_GridCenter2.x}, {doorEnd_GridCenter2.y}]</color></b>", GuiStyle);
								}
							}
							else
							{
								SU_Gizmo.DrawCircle(doorEnd_Center1, doorCenterMarkSize1, main.GridCompatible.CurrentSnapSetting.Swizzle);
								SU_Gizmo.DrawCircleX(doorEnd_Center1, doorCenterMarkSize1, main.GridCompatible.CurrentSnapSetting.Swizzle);

								//? 선택중이라면 좌표도 표시
								if (IsObjectSelected_WithParent)
								{
									var textPosition1 = doorEnd_Center1 + textCorrection;
									Handles.Label(textPosition1, $"<b><color={textColor}>[{doorEnd_GridCenter1.x}, {doorEnd_GridCenter1.y}]</color></b>", GuiStyle);
								}
							}
						}


						//? 도어 출구에 방향 삼각 화살표 그리기
						drawEndDoorTriangleArrow();
						void drawEndDoorTriangleArrow()
						{
							Gizmos.color = door.DoorEnable ? currentDoorGridColor.WithMultipliedAlpha(0.5f) : currentDoorGridColor.WithMultipliedAlpha(0.25f);

							SU_Gizmo.DrawSolidTriangle(endDoorCenter, GridCompatible2SingletonSettingSbject.O.GridUnitLength_GizmoMarkRadius, door.DoorDirection, main.GridCompatible.CurrentSnapSetting.Swizzle);
							SU_Gizmo.DrawTriangle(endDoorCenter, GridCompatible2SingletonSettingSbject.O.GridUnitLength_GizmoMarkRadius, door.DoorDirection, main.GridCompatible.CurrentSnapSetting.Swizzle);
						}


						//? 도어 길이가 길다면 그 길이 사이에 방향 화살표 넣기
						drawDoorLengthArrow();
						void drawDoorLengthArrow()
						{
							Gizmos.color = door.DoorEnable ? currentDoorGridColor : currentDoorGridColor;

							if (door.DoorWidth > 1 || door.DoorPlusLength > 0)
							{
								var arrowStart = startDoorCenter;
								var arrowEnd = endDoorCenter;

								Vector3 correction;

								switch (door.DoorDirection)
								{
									case EDirection4.Down:
									correction = -new Vector3(0, main.GridCompatible.CurrentSnapSetting.GridUnitY_Height * 0.5f, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);
									break;
									case EDirection4.Up:
									correction = new Vector3(0, main.GridCompatible.CurrentSnapSetting.GridUnitY_Height * 0.5f, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);
									break;
									case EDirection4.Left:
									correction = -new Vector3(main.GridCompatible.CurrentSnapSetting.GridUnitX_Width * 0.5f, 0, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);
									break;
									case EDirection4.Right:
									correction = new Vector3(main.GridCompatible.CurrentSnapSetting.GridUnitX_Width * 0.5f, 0, 0).SwizzlesVector(main.GridCompatible.CurrentSnapSetting.Swizzle);
									break;
									default: correction = Vector3.zero; break;
								}

								arrowStart += correction;
								arrowEnd -= correction;

								SU_Gizmo.DrawArrow(arrowStart, arrowEnd);
							}
						}


						//? 도어가 다른 도어와 연결되어 활성화 되어 있다면, 노드 그리기
						drawConnectDoorNode();
						void drawConnectDoorNode()
						{
							if (!door.DoorEnable || door.ConnectecDoor == null) { return; }
							if (StageGeneratorSingletonSettingSbject.O.RoomDoorNodeColor.TryGetColor(out var roomDoorNodeColor))
							{
								Gizmos.color = roomDoorNodeColor;

								var connectedDoor = door.ConnectecDoor;

								var connected_StartDoorCenter = connectedDoor.DoorStartTransformPositionCurrent;
								var connected_EndDoorCenter = connectedDoor.DoorEndTransformPositionCurrent;
								var connected_DoorSize = connectedDoor.SingleDoorSizeTransformCurrentVector3;
								var connected_FullDoorTransformRect = connectedDoor.FullDoorTransformRect;
								var connected_FullDoorTransformCenter = connected_FullDoorTransformRect.center.SwizzlesVector2To3(swizzle);
								var connected_FullDoorTransformSize = connected_FullDoorTransformRect.size.SwizzlesVector2To3(swizzle);

								//SU_Gizmo.DrawDoubleArrow(endDoorCenter, connected_EndDoorCenter);
								SU_Gizmo.DrawCurvedLine(endDoorCenter, connected_EndDoorCenter + FloorStandardPositionCurrent, swizzle, 0.1f);
							}
						}


						//? 도어로부터 생성된 복도 보정그리드, 복도 그리기
						if (IsObjectSelected_WithParent)
						{
							if (StageGeneratorSingletonSettingSbject.O.RoomDoorHallwayCorrectionColor.TryGetColor(out var hallwayCorrectionColor))
							{
								Gizmos.color = hallwayCorrectionColor;
								foreach (var hallwayCorrectionGrid in door.GetHallwayCorrectionGridList)
								{
									var gridWorldPos = hallwayCorrectionGrid.WorldPosition_Cached;
									Gizmos.DrawCube(gridWorldPos, gridSize);
								}
							}

							if (StageGeneratorSingletonSettingSbject.O.RoomDoorHallwayPathColor.TryGetColor(out var hallwayPathColor))
							{
								Gizmos.color = hallwayPathColor;
								foreach (var hallwayPathGrid in door.GetHallwayPathGridList)
								{
									var gridWorldPos = hallwayPathGrid.WorldPosition_Cached;
									Gizmos.DrawCube(gridWorldPos, gridSize);
								}
							}
						}
					}
				}
			}



			///======================================================================================================================================================
		}
	}

}
#endif