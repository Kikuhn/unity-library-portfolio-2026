using Pan.Util;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Pan.StageGenerators
{
    public partial class RoomObject
    {
        [Serializable]
        public class Door : RoomComponent
        {
            ///======================================================================================================================================================



            //? 도어 상태 정보



            //[BoxGroup("도어 상태 정보", false, Order = -99), HorizontalGroup("도어 상태 정보/가로")]
            //[Sirenix.OdinInspector.ReadOnly]
            //[LabelText("도어 활성화 여부")]
            //[PropertyTooltip("도어의 개폐 여부가 아닌 가용 여부를 나타냄")]
            //[SerializeField]
            private bool doorEnable = false;

            ///<summary>
            ///도어 활성화 여부 (도어의 개폐여부가 아닌, 가용 여부)
            /// </summary>
            public bool DoorEnable => doorEnable;



            //[BoxGroup("도어 상태 정보", false, Order = -99), HorizontalGroup("도어 상태 정보/가로")]
            //[Sirenix.OdinInspector.ReadOnly]
            //[LabelText("도어 열림 여부")]
            //[SerializeField]
            private bool isDoorOpen;

            /// <summary>
            /// 도어의 열림 여부
            /// </summary>
            public bool IsDoorOpen => isDoorOpen;



            /// <summary>
            /// 이 도어를 활성화한다 (이미 활성화된 상태라면 false 반환)
            /// </summary>
            [BoxGroup("도어 상태 정보", false, Order = 50), ButtonGroup("도어 상태 정보/버튼그룹")]
            [Button("도어 활성화", DrawResult = false)]
            [DisableIf(nameof(doorEnable))]
            [GUIColor(0.31f, 0.76f, 0.91f)]
            public bool EnableDoor()
            {
                if (doorEnable) { return false; }
                doorEnable = true;
                EnableDoorEvent?.Invoke();
                return true;
            }



            /// <summary>
            /// 이 도어를 비활성화한다 (이미 활성화된 상태라면 false 반환)
            /// </summary>
            [BoxGroup("도어 상태 정보", false), ButtonGroup("도어 상태 정보/버튼그룹")]
            [Button("도어 비활성화", DrawResult = false)]
            [EnableIf(nameof(doorEnable))]
            [GUIColor(0.93f, 0.33f, 0.40f)]
            public bool DisableDoor()
            {
                if (!doorEnable) { return false; }
                doorEnable = false;
                DisableDoorEvent?.Invoke();
                return true;
            }



            /// <summary>
            ///  도어를 활성화 또는 비활성화 한다 
            /// </summary>
            /// <param name="enable"></param>
            public void ActiveDoor(bool enable)
            {
                if (enable)
                {
                    EnableDoor();
                }
                else
                {
                    DisableDoor();
                }
            }



            /// <summary>
            /// 이 도어를 연다 (이미 열린 상태라면 false 반환)
            /// </summary>
            [BoxGroup("도어 상태 정보", false), ButtonGroup("도어 상태 정보/버튼그룹")]
            [Button("도어 열기", DrawResult = false)]
            [DisableIf(nameof(isDoorOpen))]
            [GUIColor(0.31f, 0.76f, 0.91f)]
            public bool OpenDoor()
            {
                if (!doorEnable && isDoorOpen) { return false; }

                isDoorOpen = true;
                OpenDoorEvent?.Invoke();
                return true;
            }



            /// <summary>
            /// 이 도어를 닫는다 (이미 닫힌 상태라면 false 반환)
            /// </summary>
            [BoxGroup("도어 상태 정보", false), ButtonGroup("도어 상태 정보/버튼그룹")]
            [Button("도어 닫기", DrawResult = false)]
            [EnableIf(nameof(isDoorOpen))]
            [GUIColor(0.93f, 0.33f, 0.40f)]
            public bool CloseDoor()
            {
                if (!doorEnable && !isDoorOpen) { return false; }

                isDoorOpen = false;
                CloseDoorEvent?.Invoke();
                return true;
            }



            ///======================================================================================================================================================



            //? 도어 벡터 정보



            private EDirection4 doorDirection;

            [BoxGroup("도어 벡터 정보", false), BoxGroup("도어 벡터 정보/도어 크기", false), HorizontalGroup("도어 벡터 정보/도어 크기/도어너비길이")]
            [HideLabel] //[LabelText("도어 방향")]
            [ShowInInspector]
            [PropertyOrder(0)]
            public EDirection4 DoorDirection => doorDirection;



            /// <summary>
            /// 도어의 방향이, 하단/상단 인지 확인하기
            /// </summary>
            public bool CheckDoorDirectionVertical => DoorDirection.IsVertical();



            /// <summary>
            /// 도어 방향에 따른 그리드 단위를 반환한다
            /// <para>도어의 방향이 <c>세로</c> 라면 GridUnit-Y 를 반환하며</para>
            /// <para>도어의 방향이 <c>가로</c> 라면 GridUnit-X 를 반환한다</para>
            /// </summary>
            /// <returns></returns>
            public int GetGridUnitByDoorDirection()
            {
                return CheckDoorDirectionVertical ? ParentRoomObject.GridCompatible.CurrentSnapSetting.GridUnitY_Height : ParentRoomObject.GridCompatible.CurrentSnapSetting.GridUnitX_Width;
            }



            /// <summary>
            /// 도어 방향에 따른 그리드 단위를 <b>반대로</b> 반환한다
            /// <para>도어의 방향이 <c>세로</c> 라면 GridUnit-X 를 반환하며</para>
            /// <para>도어의 방향이 <c>가로</c> 라면 GridUnit-Y 를 반환한다</para>
            /// </summary>
            /// <returns></returns>
            public int GetGridUnitByDoorDirectionReverse()
            {
                return CheckDoorDirectionVertical ? ParentRoomObject.GridCompatible.CurrentSnapSetting.GridUnitX_Width : ParentRoomObject.GridCompatible.CurrentSnapSetting.GridUnitY_Height;
            }



            //? 도어 크기



            [BoxGroup("도어 벡터 정보", false), BoxGroup("도어 벡터 정보/도어 크기", false), HorizontalGroup("도어 벡터 정보/도어 크기/도어너비길이")]
            [LabelText("도어 너비"), LabelWidth(80)]
            [PropertyTooltip("도어 방향에 따라, 이 너비의 값이 X축 또는 Y축에 적용된다\n 이 너비와 동일하게 도어의 길이 또한 동일하게 증가한다")]
            [SerializeField]
            [MinValue(1)]
            public int DoorWidth = 1;

            public int DoorWidthTransform => DoorWidth * GetGridUnitByDoorDirectionReverse();



            [BoxGroup("도어 벡터 정보", false), BoxGroup("도어 벡터 정보/도어 크기", false), HorizontalGroup("도어 벡터 정보/도어 크기/도어너비길이")]
            [LabelText("도어 추가 길이"), LabelWidth(90)]
            [SerializeField]
            [MinValue(0)]
            public int DoorPlusLength = 0;



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 크기 정보")]
            [LabelText("도어 총 길이")]
            [PropertyTooltip("도어 너비 + 도어 추가 길이 + (도어너비가 1이하라면 +1)")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public int DoorTotalLength => DoorWidth + DoorPlusLength + (DoorWidth <= 1 ? 1 : 0);



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 크기 정보")]
            [LabelText("도어 총 길이 (Transform)")]
            [PropertyTooltip("도어의 방향에 따라, 도어 총 길이에 GridUnit-X/Y 를 연산해 반환한다")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public int DoorTotalTransformLength => DoorTotalLength * GetGridUnitByDoorDirection();



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 크기 정보")]
            [LabelText("단일 도어 크기 (Current)")]
            [PropertyTooltip("도어의 방향에 따라, 단일 도어 크기 벡터를 연산해 반환한다\n메인 방향이 존재하지 않기에 Original은 별도로 존재하지않는다")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public Vector2Int SingleDoorSize => CheckDoorDirectionVertical ? new Vector2Int(DoorWidth, 1) : new Vector2Int(1, DoorWidth);



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 크기 정보")]
            [LabelText("단일 도어 크기 V2 (Transform)")]
            [PropertyTooltip("도어의 방향에 따라, 단일 도어 크기 벡터 연산과 GridUnit-X/Y 를 연산해 Vector2Int을 반환한다")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public Vector2Int SingleDoorSizeTransformVector2
            {
                get
                {
                    if (DoorDirection.IsVertical())
                    {
                        return new Vector2Int(
                            DoorWidth * ParentRoomObject.GridCompatible.CurrentSnapSetting.GridUnitX_Width,
                            ParentRoomObject.GridCompatible.CurrentSnapSetting.GridUnitY_Height);
                    }
                    else
                    {
                        return new Vector2Int(
                            ParentRoomObject.GridCompatible.CurrentSnapSetting.GridUnitX_Width,
                            DoorWidth * ParentRoomObject.GridCompatible.CurrentSnapSetting.GridUnitY_Height);
                    }
                }
            }



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 크기 정보")]
            [LabelText("단일 도어 크기 V3 (Transform, Current)")]
            [PropertyTooltip("도어의 방향에 따라, 단일 도어 크기 벡터 연산과 GridUnit-X/Y 를 연산해 Swizzle 까지 연산하여 Vector3Int을 반환한다")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public Vector3Int SingleDoorSizeTransformCurrentVector3
            {
                get
                {
                    Vector3Int result;

                    if (DoorDirection.IsVertical())
                    {
                        result = new Vector3Int(
                            DoorWidth * ParentRoomObject.GridCompatible.CurrentSnapSetting.GridUnitX_Width,
                            ParentRoomObject.GridCompatible.CurrentSnapSetting.GridUnitY_Height,
                            0);
                    }
                    else
                    {
                        result = new Vector3Int(
                            ParentRoomObject.GridCompatible.CurrentSnapSetting.GridUnitX_Width,
                            DoorWidth * ParentRoomObject.GridCompatible.CurrentSnapSetting.GridUnitY_Height,
                            0);
                    }

                    return result.SwizzlesVectorInt(ParentRoomObject.GridCompatible.CurrentSnapSetting.Swizzle);
                }
            }



            //? Full 도어 크기



            /// <summary>
            /// Full 도어 크기 (Original), 도어 방향과 상관 없는 고정된 크기 (<see cref="DoorWidth"/> x <see cref="DoorTotalLength"/>)
            /// </summary>
            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 크기 정보")]
            [LabelText("Full 도어 크기 (Original)")]
            [PropertyTooltip("Full 도어 크기 (방향 연산 X)")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public Vector2Int FullDoorSizeOriginal => new Vector2Int(DoorWidth, DoorTotalLength);



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 크기 정보")]
            [LabelText("Full 도어 크기 (Original, CurrentDireciton)")]
            [PropertyTooltip("Full 도어 크기 (방향 연산 O)")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public Vector2Int FullDoorSizeOriginalCurrentDireciton => CheckDoorDirectionVertical ? new Vector2Int(DoorWidth, DoorTotalLength) : new Vector2Int(DoorTotalLength, DoorWidth);



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 크기 정보")]
            [LabelText("Full 도어 크기 (Transform)")]
            [PropertyTooltip("Full 도어 Transform 크기 (방향 연산 X)")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public Vector2Int FullDoorSizeTransformDireciton
            {
                get
                {
                    return new Vector2Int(DoorWidthTransform, DoorTotalTransformLength);
                }
            }



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 크기 정보")]
            [LabelText("Full 도어 크기 (Transform, CurrentDireciton)")]
            [PropertyTooltip("Full 도어 Transform 크기 (방향 연산 O)")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public Vector2Int FullDoorSizeTransformCurrentDireciton
            {
                get
                {
                    return CheckDoorDirectionVertical ? new Vector2Int(DoorWidthTransform, DoorTotalTransformLength) : new Vector2Int(DoorTotalTransformLength, DoorWidthTransform);
                }
            }



            //? Full 도어 길이 거리



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 크기 정보")]
            [LabelText("Full 도어 길이 거리")]
            [PropertyTooltip("도어 거리를 포함하여, 방의 끝부분부터 떨어진 총 거리")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public int FullDoorTotalDistance => DoorTotalLength + DoorGridPositionDistance;



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 크기 정보")]
            [LabelText("Full 도어 길이 거리 길이 (Transform)")]
            [PropertyTooltip("도어 트랜스폼 거리를 포함하여, 방의 끝부분부터 떨어진 총 트랜스폼 거리")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public int FullDoorTotalTransformDistance => DoorTotalTransformLength + DoorGridPositionDistanceTransform;



            ///======================================================================================================================================================



            [BoxGroup("도어 벡터 정보", false), BoxGroup("도어 벡터 정보/도어 그리드 좌표", false), HorizontalGroup("도어 벡터 정보/도어 그리드 좌표/가로")]
            [LabelText("도어 그리드 좌표")]
            [PropertyTooltip("이 값이 변경될때마다 도어 리스트가 이 값의 오름차순으로 정렬 되어야함")]
#if UNITY_EDITOR
            [GUIColor(nameof(editorDoorGridPositionAxisColor))]
#endif

            public int DoorGridPositionAxis;



#if UNITY_EDITOR            
            private Color editorDoorGridPositionAxisColor
            {
                get
                {
                    return IsDoorGridPositionAxisInsideBounds ? Color.white : Color.red;
                }
            }
#endif



            #region 도어 그리드 좌표 관련 확장



            //. 도어 축/방 내부 경계 계산 유틸
            private void GetRoomAxisBounds(out int minInside, out int maxInside)
            {
                //. 벽을 따라가는 축 길이 L(셀 수): 상/하 도어는 방 X폭, 좌/우 도어는 방 Y폭
                int L = DoorDirection.IsVertical()
                    ? ParentRoomObject.GridCompatible.ObjectSizeX_Width
                    : ParentRoomObject.GridCompatible.ObjectSizeY_Height;

                //. 방 내부 유효 인덱스 범위 [minInside..maxInside]
                //  예) L=5 → [-2..2], L=4 → [-2..1]
                minInside = -(L / 2);
                maxInside = minInside + (L - 1);
            }



            //. 도어가 차지하는 축 범위 [left..right] 계산 유틸 (전부 정수)
            private void GetDoorSpan(out int left, out int right)
            {
                if ((DoorWidth & 1) == 0)
                {
                    //? 짝수 폭: 중심 칸이 2개이며 DoorGridPositionAxis는 "좌/하 중심"을 가리킨다고 가정
                    int half = DoorWidth / 2;
                    left = DoorGridPositionAxis - (half - 1);
                    right = DoorGridPositionAxis + half;
                }
                else
                {
                    //? 홀수 폭: 단일 중심
                    int half = (DoorWidth - 1) / 2;
                    left = DoorGridPositionAxis - half;
                    right = DoorGridPositionAxis + half;
                }
            }



            ///<summary>
            ///방 안쪽(벽을 따라가는 축)에서 도어 전체 폭이 벗어나지 않는지 검사한다 (정수 연산 전용)
            /// </summary>
            public bool IsDoorGridPositionAxisInsideBounds
            {
                get
                {
                    GetRoomAxisBounds(out int minInside, out int maxInside);
                    GetDoorSpan(out int left, out int right);
                    //! 도어가 방 안쪽 범위를 벗어나면 false
                    return (left >= minInside) && (right <= maxInside);
                }
            }



            //. 내부 유틸: 좌/하, 우/상 초과 길이(셀)를 각각 계산
            private void GetDoorAxisOverhang(out int overflowLow, out int overflowHigh)
            {
                GetRoomAxisBounds(out int minInside, out int maxInside);
                GetDoorSpan(out int left, out int right);

                //. 좌/하 쪽으로 벗어난 셀 수
                overflowLow = Mathf.Max(0, minInside - left);

                //. 우/상 쪽으로 벗어난 셀 수
                overflowHigh = Mathf.Max(0, right - maxInside);
            }



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 좌표 정보")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            [LabelText("도어 좌표 초과(부호 포함)")]
            [PropertyTooltip("좌/하로 넘치면 음수, 우/상으로 넘치면 양수, 내부면 0\n양쪽 모두 넘치면 큰 쪽 방향을 따르며 |값| = 큰쪽−작은쪽")]
            public int DoorAxisOverhangSigned
            {
                get
                {
                    GetDoorAxisOverhang(out int low, out int high);

                    //? 부호 규칙: (우/상 초과) - (좌/하 초과)
                    //! 양쪽이 동시에 넘치면 큰 쪽의 부호가 선택되고, 크기는 차이만큼
                    return high - low;
                }
            }



            #endregion



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 좌표 정보")]
            [LabelText("도어 그리드 좌표 (Transform)")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public int DoorGridPositionAxisTransform => DoorGridPositionAxis * (CheckDoorDirectionVertical ? ParentRoomObject.GridCompatible.CurrentSnapSetting.GridUnitX_Width : ParentRoomObject.GridCompatible.CurrentSnapSetting.GridUnitY_Height);



            [BoxGroup("도어 벡터 정보", false), BoxGroup("도어 벡터 정보/도어 그리드 좌표", false), HorizontalGroup("도어 벡터 정보/도어 그리드 좌표/가로", 0.15f), ButtonGroup("도어 벡터 정보/도어 그리드 좌표/가로/버튼")]
            [Button("  -  ", Stretch = false, ButtonAlignment = 0.7f)]
            private void Minus_DoorGridPositionAxis() => DoorGridPositionAxis--;

            [BoxGroup("도어 벡터 정보", false), BoxGroup("도어 벡터 정보/도어 그리드 좌표", false), HorizontalGroup("도어 벡터 정보/도어 그리드 좌표/가로", 0.15f), ButtonGroup("도어 벡터 정보/도어 그리드 좌표/가로/버튼")]
            [Button("  +  ", Stretch = false, ButtonAlignment = 0.8f)]
            private void Plus_DoorGridPositionAxis() => DoorGridPositionAxis++;



            [BoxGroup("도어 벡터 정보", false), BoxGroup("도어 벡터 정보/도어 그리드 좌표", false)]
            [LabelText("도어 그리드 거리")]
            [MinValue(0)]
            public int DoorGridPositionDistance;



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 좌표 정보")]
            [LabelText("도어 그리드 거리 (Transform)")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public int DoorGridPositionDistanceTransform => DoorGridPositionDistance * (!CheckDoorDirectionVertical ? ParentRoomObject.GridCompatible.CurrentSnapSetting.GridUnitX_Width : ParentRoomObject.GridCompatible.CurrentSnapSetting.GridUnitY_Height);



            //? 도어 입구 (Start)



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 좌표 정보")]
            [LabelText("도어 Start 좌표 (Grid)")]
            [PropertyTooltip("도어의 입구 (Start) 의 그리드 좌표")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public Vector2Int DoorStartGridPosition
            {
                get
                {
                    //? 부모 방의 좌표 보정 Plus 베이스로 연산 시작
                    Vector2Int gridPositionStandard = Vector2Int.zero;//ParentRoomObject.GridPositionCorrectionPlus;

                    switch (doorDirection)
                    {
                        case EDirection4.Down:
                        gridPositionStandard += ParentRoomObject.GridPositionStandard(ECenterStandard.LowerCenter);
                        return new Vector2Int(gridPositionStandard.x + DoorGridPositionAxis, gridPositionStandard.y - DoorGridPositionDistance - 1);

                        case EDirection4.Up:
                        gridPositionStandard += ParentRoomObject.GridPositionStandard(ECenterStandard.UpperCenter);
                        return new Vector2Int(gridPositionStandard.x + DoorGridPositionAxis, gridPositionStandard.y + DoorGridPositionDistance + 1);

                        case EDirection4.Left:
                        gridPositionStandard += ParentRoomObject.GridPositionStandard(ECenterStandard.MiddleLeft);
                        return new Vector2Int(gridPositionStandard.x - DoorGridPositionDistance - 1, gridPositionStandard.y + DoorGridPositionAxis);

                        case EDirection4.Right:
                        gridPositionStandard += ParentRoomObject.GridPositionStandard(ECenterStandard.MiddleRight);
                        return new Vector2Int(gridPositionStandard.x + DoorGridPositionDistance + 1, gridPositionStandard.y + DoorGridPositionAxis);

                        default: return Vector2Int.zero;
                    }

                    #region Legacy
                    //var roomCenterGridPosition = ParentRoomObject.GridPosition;

                    //int roomWidth = ParentRoomObject.GridCompatible.ObjectSizeX_Width;
                    //int roomHeight = ParentRoomObject.GridCompatible.ObjectSizeY_Height;
                    //bool roomSnapToGridCellCenter = ParentRoomObject.GridCompatible.CurrentSnapSetting.SnapToGridCellCenter;




                    ////var down = roomCenterGridPosition.y;
                    ////down -= ParentRoomObject.GridCompatible.ObjectSizeY_Height / 2;
                    ////if ((!ParentRoomObject.GridCompatible.ObjectSizeY_Height.IsEven() && ParentRoomObject.GridCompatible.CurrentSnapSetting.SnapToGridCellCenter))
                    ////{
                    ////    down -= 1;
                    ////}

                    ////return new Vector2Int(DoorGridPositionAxis, down);




                    //switch (DoorDirection)
                    //{
                    //    //case EDirection4.Down:
                    //    //return new Vector2Int(DoorGridPositionAxis, roomCenterGridPosition.y - (roomHeight / 2) + ((!roomHeight.IsEven() && roomSnapToGridCellCenter) ? -1 : 0));

                    //    //case EDirection4.Up:
                    //    //return new Vector2Int(DoorGridPositionAxis, roomCenterGridPosition.y + (roomHeight / 2) + ((!roomHeight.IsEven() && roomSnapToGridCellCenter) ? 1 : 1));

                    //    //case EDirection4.Left:
                    //    //return new Vector2Int(roomCenterGridPosition.x - (roomWidth / 2) + ((!roomWidth.IsEven() && roomSnapToGridCellCenter) ? -1 : 0), DoorGridPositionAxis);

                    //    //case EDirection4.Right:
                    //    //return new Vector2Int(roomCenterGridPosition.x + (roomWidth / 2) + ((!roomWidth.IsEven() && roomSnapToGridCellCenter) ? 1 : 1), DoorGridPositionAxis);

                    //    case EDirection4.Down:
                    //    return new Vector2Int(DoorGridPositionAxis, roomCenterGridPosition.y - (roomHeight / 2) + ((!roomHeight.IsEven()) ? -1 : 0));

                    //    case EDirection4.Up:
                    //    return new Vector2Int(DoorGridPositionAxis, roomCenterGridPosition.y + (roomHeight / 2) + ((!roomHeight.IsEven()) ? 1 : 0));

                    //    case EDirection4.Left:
                    //    return new Vector2Int(roomCenterGridPosition.x - (roomWidth / 2) + ((!roomWidth.IsEven()) ? -1 : 0), DoorGridPositionAxis);

                    //    case EDirection4.Right:
                    //    return new Vector2Int(roomCenterGridPosition.x + (roomWidth / 2) + ((!roomWidth.IsEven()) ? 1 : 0), DoorGridPositionAxis);

                    //    default: return default;
                    //}
                    #endregion

                    #region Legacy2
                    //var roomGridRect = ParentRoomObject.GridRect;
                    //var roomGridPosition = ParentRoomObject.GridPosition;
                    //int rectPos;
                    //switch (DoorDirection)
                    //{
                    //    case EDirection4.Down:

                    //    rectPos = ParentRoomObject.GridCompatible.ObjectSizeY_Height.IsEven() ? Mathf.FloorToInt(roomGridRect.yMin) : Mathf.CeilToInt(roomGridRect.yMin);
                    //    return new Vector2Int(roomGridPosition.x + DoorGridPositionAxis, rectPos - DoorGridPositionDistance + (roomGridRect.height.IsEven() ? 0 : -1));

                    //    case EDirection4.Up:

                    //    rectPos = ParentRoomObject.GridCompatible.ObjectSizeY_Height.IsEven() ? Mathf.FloorToInt(roomGridRect.yMax) : Mathf.CeilToInt(roomGridRect.yMax);
                    //    return new Vector2Int(roomGridPosition.x + DoorGridPositionAxis, rectPos + DoorGridPositionDistance + (roomGridRect.height.IsEven() ? 1 : 0));


                    //    case EDirection4.Left:

                    //    rectPos = ParentRoomObject.GridCompatible.ObjectSizeX_Width.IsEven() ? Mathf.FloorToInt(roomGridRect.xMin) : Mathf.CeilToInt(roomGridRect.xMin);
                    //    return new Vector2Int(rectPos - DoorGridPositionDistance + (roomGridRect.width.IsEven() ? 0 : -1), roomGridPosition.y + DoorGridPositionAxis);


                    //    case EDirection4.Right:

                    //    rectPos = ParentRoomObject.GridCompatible.ObjectSizeX_Width.IsEven() ? Mathf.FloorToInt(roomGridRect.xMax) : Mathf.CeilToInt(roomGridRect.xMax);
                    //    return new Vector2Int(rectPos + DoorGridPositionDistance + (roomGridRect.width.IsEven() ? 1 : 0), roomGridPosition.y + DoorGridPositionAxis);


                    //    default: return default;
                    //} 
                    #endregion
                }
            }


            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 좌표 정보")]
            [LabelText("도어 Start 좌표")]
            [PropertyTooltip("도어의 입구 (Start) 의 좌표")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public Vector2 DoorStartPosition
            {
                get
                {
                    Vector2 startPosition = DoorStartGridPosition;

                    //? GridUnit을 연산하지 않기에, 0.5f를 고정으로 보정한다
                    if (!ParentRoomObject.GridCompatible.CurrentSnapSetting.SnapToGridCellCenter)
                    {
                        startPosition += new Vector2(0.5f, 0.5f);
                    }

                    return startPosition;
                }
            }



            /// <summary>
            /// 도어의 Start 트랜스폼 좌표를 얻기 (Swizzle 선택)
            /// </summary>
            /// <param name="resultWithSwizzle"></param>
            /// <returns></returns>
            private Vector3 GetDoorStartTransformPositionInternal(bool resultWithSwizzle)
            {
                var transformPosition = ParentRoomObject.GridCompatible.Calculate_GridPosition_To_TransformPosition(DoorStartGridPosition - ParentRoomObject.GridPositionCorrectionPlus, true);
                var startTransformPosition = ParentRoomObject.GridCompatible.GridSnapTransformPosition_CustomSize(transformPosition, SingleDoorSize, resultWithSwizzle);
                return startTransformPosition;
            }



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 좌표 정보")]
            [LabelText("도어 Start 좌표 (Transform)")]
            [PropertyTooltip("도어의 입구 (Start) 의 트랜스폼 좌표")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public Vector2 DoorStartTransformPosition => GetDoorStartTransformPositionInternal(false);



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 좌표 정보")]
            [LabelText("도어 Start 좌표 (Transform, Current)")]
            [PropertyTooltip("현재 도어의 입구 (Start) 의 트랜스폼 좌표")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public Vector3 DoorStartTransformPositionCurrent => GetDoorStartTransformPositionInternal(true);



            //? 도어 출구 (End)



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 좌표 정보")]
            [LabelText("도어 End 좌표 (Grid)")]
            [PropertyTooltip("도어의 출구 (End) 의 그리드 좌표")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public Vector2Int DoorEndGridPosition
            {
                get
                {
                    var result = DoorStartGridPosition;

                    switch (DoorDirection)
                    {
                        case EDirection4.Down: result += new Vector2Int(0, -DoorTotalLength + 1); break;
                        case EDirection4.Up: result += new Vector2Int(0, DoorTotalLength - 1); break;
                        case EDirection4.Left: result += new Vector2Int(-DoorTotalLength + 1, 0); break;
                        case EDirection4.Right: result += new Vector2Int(DoorTotalLength - 1, 0); break;
                    }

                    return result;
                }
            }



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 좌표 정보")]
            [LabelText("도어 End 좌표")]
            [PropertyTooltip("도어의 입구 (Start) 의 좌표")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public Vector2 DoorEndPosition
            {
                get
                {
                    Vector2 endPosition = DoorEndGridPosition;

                    //? GridUnit을 연산하지 않기에, 0.5f를 고정으로 보정한다
                    if (!ParentRoomObject.GridCompatible.CurrentSnapSetting.SnapToGridCellCenter)
                    {
                        endPosition += new Vector2(0.5f, 0.5f);
                    }

                    return endPosition;
                }
            }



            /// <summary>
            /// 도어의 End 트랜스폼 좌표를 얻기 (Swizzle 선택)
            /// </summary>
            /// <param name="resultWithSwizzle"></param>
            /// <returns></returns>
            private Vector3 GetDoorEndTransformPositionInternal(bool resultWithSwizzle)
            {
                var transformPosition = ParentRoomObject.GridCompatible.Calculate_GridPosition_To_TransformPosition(DoorEndGridPosition - ParentRoomObject.GridPositionCorrectionPlus, true);
                var endTransformPosition = ParentRoomObject.GridCompatible.GridSnapTransformPosition_CustomSize(transformPosition, SingleDoorSize, resultWithSwizzle);
                return endTransformPosition;
            }



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 좌표 정보")]

            [LabelText("도어 End 좌표 (Transform)")]
            [PropertyTooltip("도어의 출구 (End) 의 트랜스폼 좌표")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public Vector2 DoorEndTransformPosition => GetDoorEndTransformPositionInternal(false);



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 좌표 정보")]

            [LabelText("도어 End 좌표 (Transform, Current)")]
            [PropertyTooltip("현재 도어의 출구 (End) 의 트랜스폼 좌표")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public Vector3 DoorEndTransformPositionCurrent => GetDoorEndTransformPositionInternal(true);



            ///======================================================================================================================================================



            //? 도어 Rect



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 Rect 정보")]
            [LabelText("도어 Start Rect")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public Rect DoorStartRect
            {
                get
                {
                    //. 좌표에 그리드 단위 만큼 나눠, 보정된 좌표를 기준으로 계산해야함
                    //.     그래야 그리드 단위가 1 초과일때, 어긋나지 않음
                    return SU_TF_Rect.RectFromCenter(DoorStartTransformPosition.Divide(ParentRoomObject.GridCompatible.CurrentSnapSetting.GridUnitOriginalVector2), SingleDoorSize);
                }
            }



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 Rect 정보")]
            [LabelText("도어 Start Rect (Transform)")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public Rect DoorStartTransformRect => SU_TF_Rect.RectFromCenter(DoorStartTransformPosition, SingleDoorSizeTransformVector2);



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 Rect 정보")]
            [LabelText("도어 End Rect")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public Rect DoorEndRect
            {
                get
                {
                    //. 좌표에 그리드 단위 만큼 나눠, 보정된 좌표를 기준으로 계산해야함
                    //.     그래야 그리드 단위가 1 초과일때, 어긋나지 않음
                    return SU_TF_Rect.RectFromCenter(DoorEndTransformPosition.Divide(ParentRoomObject.GridCompatible.CurrentSnapSetting.GridUnitOriginalVector2), SingleDoorSize);
                }
            }



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 Rect 정보")]
            [LabelText("도어 End Rect (Transform)")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public Rect DoorEndTransformRect => SU_TF_Rect.RectFromCenter(DoorEndTransformPosition, SingleDoorSizeTransformVector2);



            private Rect FullDoorRectInternal(bool isTransform)
            {
                var fullDoorSize = !isTransform ? FullDoorSizeOriginalCurrentDireciton : FullDoorSizeTransformCurrentDireciton;
                var singleDoorSize = !isTransform ? SingleDoorSize : SingleDoorSizeTransformVector2;


                if (DoorDirection.IsDown() || DoorDirection.IsLeft())
                {
                    var doorEndPosition = DoorEndTransformPosition;

                    if (!isTransform)
                    {
                        //. 좌표에 그리드 단위 만큼 나눠, 보정된 좌표를 기준으로 계산해야함
                        //.     그래야 그리드 단위가 1 초과일때, 어긋나지 않음
                        doorEndPosition.DivideRef(ParentRoomObject.GridCompatible.CurrentSnapSetting.GridUnitOriginalVector2);
                    }

                    return new Rect(
                        (doorEndPosition.x - (singleDoorSize.x / 2f)),
                        (doorEndPosition.y - (singleDoorSize.y / 2f)),
                        fullDoorSize.x,
                        fullDoorSize.y);
                }
                else
                {
                    var doorStartPosition = DoorStartTransformPosition;

                    if (!isTransform)
                    {
                        //. 좌표에 그리드 단위 만큼 나눠, 보정된 좌표를 기준으로 계산해야함
                        //.     그래야 그리드 단위가 1 초과일때, 어긋나지 않음
                        doorStartPosition.DivideRef(ParentRoomObject.GridCompatible.CurrentSnapSetting.GridUnitOriginalVector2);
                    }

                    return new Rect(
                        (doorStartPosition.x - (singleDoorSize.x / 2f)),
                        (doorStartPosition.y - (singleDoorSize.y / 2f)),
                        fullDoorSize.x,
                        fullDoorSize.y);
                }
            }



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 Rect 정보")]
            [LabelText("Full 도어 Rect")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public Rect FullDoorRect
                => FullDoorRectInternal(false);



            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 Rect 정보")]
            [LabelText("Full 도어 Rect (Transform)")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public Rect FullDoorTransformRect
                => FullDoorRectInternal(true);



            /// <summary>
            /// <see cref="StageGeneratorSetting"/> 을 기반으로 확장된 Full Door Rect
            /// </summary>
            /// <param name="setting"></param>
            /// <returns></returns>
            public Rect FullDoorRectExpand(StageGeneratorSetting setting)
            {
                Rect fullDoorRect = FullDoorRect;

                int correction = Mathf.Max(DoorWidth, setting.Hallawy.HallwayMinWidth);


                switch (DoorDirection)
                {
                    case EDirection4.Down:
                    fullDoorRect.yMin -= correction;
                    break;

                    case EDirection4.Up:
                    fullDoorRect.yMax += correction;
                    break;

                    case EDirection4.Left:
                    fullDoorRect.xMin -= correction;
                    break;

                    case EDirection4.Right:
                    fullDoorRect.xMax += correction;
                    break;
                }


                return fullDoorRect;
            }



            ///======================================================================================================================================================



            //? 도어 입구/출구 중심 좌표



#if UNITY_EDITOR


            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 입출구 그리드 정보")]
            [HideLabel, ShowInInspector, EnableGUI, DisplayAsString]
            private string dummy_GridPositionStartText
            {
                get
                {
                    CenterGridPosition_Start(out var doubleCenter, out var center1, out var center2);
                    if (doubleCenter)
                    {
                        return $"도어 입구 (Start) 그리드 좌표 중심점 {center1}, {center2}";
                    }
                    else
                    {
                        return $"도어 입구 (Start) 그리드 좌표 중심점 {center1}";
                    }
                }
            }

            [BoxGroup("도어 벡터 정보", false), FoldoutGroup("도어 벡터 정보/도어 벡터 정보 요약", false), TitleGroup("도어 벡터 정보/도어 벡터 정보 요약/도어 입출구 그리드 정보")]
            [HideLabel, ShowInInspector, EnableGUI, DisplayAsString]
            private string dummy_GridPositionEndText
            {
                get
                {
                    CenterGridPosition_End(out var doubleCenter, out var center1, out var center2);
                    if (doubleCenter)
                    {
                        return $"도어 출구 (End) 그리드 좌표 중심점 {center1}, {center2}";
                    }
                    else
                    {
                        return $"도어 출구 (End) 그리드 좌표 중심점 {center1}";
                    }
                }
            }



#endif



            /// <summary>
            /// 도어의 그리드 중심 좌표(들)을 연산하는 메서드
            /// <para>도어 너비가 짝수면 중심점이 2개가 반환되며, (좌측/하단) 에 있는 중심점이 중심점1, 나머지가 중심점2가 된다</para>
            /// </summary>
            /// <param name="isStartGridPosition">도어 입구 (Start)인지, 도어 출구 (End) 도어인지 여부</param>
            /// <param name="doubleCenter">도어의 중심점이 2개 인지 여부</param>
            /// <param name="center1">중심점 1</param>
            /// <param name="center2">중심점 2</param>
            private void CenterGridPositionInetrnal(bool isStartGridPosition, out bool doubleCenter, out Vector2Int center1, out Vector2Int center2)
            {
                doubleCenter = DoorWidth.IsEven();

                if (doubleCenter)
                {
                    center1 = isStartGridPosition ? DoorStartGridPosition : DoorEndGridPosition;
                    center2 = CheckDoorDirectionVertical ? new Vector2Int(center1.x + 1, center1.y) : new Vector2Int(center1.x, center1.y + 1);
                }
                else
                {
                    center1 = isStartGridPosition ? DoorStartGridPosition : DoorEndGridPosition;
                    center2 = center1;
                }
            }

            /// <summary>
            /// 도어의 현재 트랜스폼 중심 좌표(들)을 연산하는 메서드
            /// <para>도어 너비가 짝수면 중심점이 2개가 반환되며, (좌측/하단) 에 있는 중심점이 중심점1, 나머지가 중심점2가 된다</para>
            /// </summary>
            /// <param name="isStartGridPosition">도어 입구 (Start)인지, 도어 출구 (End) 도어인지 여부</param>
            /// <param name="doubleCenter">도어의 중심점이 2개 인지 여부</param>
            /// <param name="center1">중심점 1</param>
            /// <param name="center2">중심점 2</param>
            private void CenterTransformCurrentPositionInternal(bool isStartGridPosition, out bool doubleCenter, out Vector3 center1, out Vector3 center2)
            {
                doubleCenter = DoorWidth.IsEven();

                if (doubleCenter)
                {
                    float gridUnitHalfX = ParentRoomObject.GridCompatible.CurrentSnapSetting.GridUnitX_Width * 0.5f;
                    float gridUnitHalfY = ParentRoomObject.GridCompatible.CurrentSnapSetting.GridUnitY_Height * 0.5f;


                    //. 우선 Swizzle되지 않은 Transform 좌표를 할당한다
                    center1 = isStartGridPosition ? DoorStartTransformPosition : DoorEndTransformPosition;
                    center2 = center1;


                    //. 도어 방향에 맞춰 GridUnit 만큼 좌표를 연산 한 뒤
                    if (CheckDoorDirectionVertical) { center1.x -= gridUnitHalfX; }
                    else { center1.y -= gridUnitHalfY; }


                    //. 도어 방향에 맞춰 GridUnit 만큼 좌표를 연산 한 뒤
                    if (CheckDoorDirectionVertical) { center2.x += gridUnitHalfX; }
                    else { center2.y += gridUnitHalfY; }


                    //. 스냅 설정의 바닥Z축도 연산해준다
                    center1.z = ParentRoomObject.GridCompatible.CurrentSnapSetting.FloorStandardPositionLength;
                    center2.z = center1.z;


                    center1.SwizzlesVectorRef(ParentRoomObject.GridCompatible.CurrentSnapSetting.Swizzle);
                    center2.SwizzlesVectorRef(ParentRoomObject.GridCompatible.CurrentSnapSetting.Swizzle);


                    #region 각각 따로따로 연산하던거 (지금은 통합해서 효율적이게 연산함)
                    //float gridUnitHalfX = ParentRoomObject.GridCompatible.CurrentSnapSetting.GridUnitX_Width * 0.5f;
                    //float gridUnitHalfY = ParentRoomObject.GridCompatible.CurrentSnapSetting.GridUnitY_Height * 0.5f;


                    ////? Center1 연산

                    ////. 우선 Swizzle되지 않은 Transform 좌표를 할당한다
                    //center1 = isStartGridPosition ? DoorStartTransformPosition : DoorEndTransformPosition;

                    ////. 도어 방향에 맞춰 GridUnit 만큼 좌표를 연산 한 뒤
                    //if (CheckDoorDirectionVertical) { center1.x -= gridUnitHalfX; }
                    //else { center1.y -= gridUnitHalfY; }


                    ////. 스냅 설정의 바닥Z축도 연산해준다
                    //center1.z = ParentRoomObject.GridCompatible.CurrentSnapSetting.FloorStandardPosition;

                    ////. Swizzle하여 Current 좌표가 되도록 한다
                    //center1.SwizzlesVectorRef(ParentRoomObject.GridCompatible.CurrentSnapSetting.Swizzle);

                    ////? Center2 연산

                    ////. 우선 Swizzle되지 않은 Transform 좌표를 할당한다
                    //center2 = isStartGridPosition ? DoorStartTransformPosition : DoorEndTransformPosition;

                    ////. 도어 방향에 맞춰 GridUnit 만큼 좌표를 연산 한 뒤
                    //if (CheckDoorDirectionVertical) { center2.x += gridUnitHalfX; }
                    //else { center2.y += gridUnitHalfY; }

                    ////. 스냅 설정의 바닥Z축도 연산해준다
                    //center2.z = ParentRoomObject.GridCompatible.CurrentSnapSetting.FloorStandardPosition;

                    ////. Swizzle하여 Current 좌표가 되도록 한다
                    //center2.SwizzlesVectorRef(ParentRoomObject.GridCompatible.CurrentSnapSetting.Swizzle); 
                    #endregion
                }
                else
                {
                    center1 = isStartGridPosition ? DoorStartTransformPositionCurrent : DoorEndTransformPositionCurrent;
                    center2 = center1;
                }
            }



            //? 도어 입구 좌표 반환



            /// <summary>
            /// 도어 입구 (Start) 의 그리드 중심 좌표(들)을 연산하는 메서드
            /// <para>도어 너비가 짝수면 중심점이 2개가 반환되며, (좌측/하단) 에 있는 중심점이 중심점1, 나머지가 중심점2가 된다</para>
            /// </summary>
            /// <param name="doubleCenter">도어 입구의 중심점이 2개 인지 여부</param>
            /// <param name="center1">중심점 1</param>
            /// <param name="center2">중심점 2</param>
            public void CenterGridPosition_Start(out bool doubleCenter, out Vector2Int center1, out Vector2Int center2)
                => CenterGridPositionInetrnal(true, out doubleCenter, out center1, out center2);


            /// <summary>
            /// 도어 입구 (Start) 의 현재 트랜스폼 중심 좌표(들)을 연산하는 메서드
            /// <para>도어 너비가 짝수면 중심점이 2개가 반환되며, (좌측/하단) 에 있는 중심점이 중심점1, 나머지가 중심점2가 된다</para>
            /// </summary>
            /// <param name="doubleCenter">도어 입구의 중심점이 2개 인지 여부</param>
            /// <param name="center1">중심점 1</param>
            /// <param name="center2">중심점 2</param>
            public void CenterTransformCurrentPosition_Start(out bool doubleCenter, out Vector3 center1, out Vector3 center2)
            => CenterTransformCurrentPositionInternal(true, out doubleCenter, out center1, out center2);




            //? 도어 출구 좌표 반환



            /// <summary>
            /// 도어 출구 (End) 의 그리드 중심 좌표(들)을 연산하는 메서드
            /// <para>도어 너비가 짝수면 중심점이 2개가 반환되며, (좌측/하단) 에 있는 중심점이 중심점1, 나머지가 중심점2가 된다</para>
            /// </summary>
            /// <param name="doubleCenter">도어 출구의 중심점이 2개 인지 여부</param>
            /// <param name="center1">중심점 1</param>
            /// <param name="center2">중심점 2</param>
            public void CenterGridPosition_End(out bool doubleCenter, out Vector2Int center1, out Vector2Int center2)
            => CenterGridPositionInetrnal(false, out doubleCenter, out center1, out center2);

            /// <summary>
            /// 도어 출구 (End) 의 현재 트랜스폼 중심 좌표(들)을 연산하는 메서드
            /// <para>도어 너비가 짝수면 중심점이 2개가 반환되며, (좌측/하단) 에 있는 중심점이 중심점1, 나머지가 중심점2가 된다</para>
            /// </summary>
            /// <param name="doubleCenter">도어 출구의 중심점이 2개 인지 여부</param>
            /// <param name="center1">중심점 1</param>
            /// <param name="center2">중심점 2</param>
            public void CenterTransformCurrentPosition_End(out bool doubleCenter, out Vector3 center1, out Vector3 center2)
             => CenterTransformCurrentPositionInternal(false, out doubleCenter, out center1, out center2);



            ///======================================================================================================================================================



            //? 스테이지 생성: 도어 그리드 저장소



            ///<summary>
            /// 이 도어가 차지하고있는 Grid들의 리스트
            /// </summary>
            [SerializeField]
            [HideInInspector]
            private List<StageGenerator.Grid> DoorGrids = null;

            ///<summary>
            /// 이 도어가 차지하고있는 Grid들의 리스트 얻기
            /// </summary>
            [FoldoutGroup("스테이지 생성 정보"), TitleGroup("스테이지 생성 정보/도어 그리드 정보", Order = 1)]
            [ShowInInspector]
            [Sirenix.OdinInspector.ReadOnly]
            [LabelText("차지중인 그리드")]
            public IReadOnlyList<StageGenerator.Grid> GetDoorGrids => DoorGrids;



            ///<summary>
            /// 이 도어가 차지하고있는 Grid들의 리스트를 초기화 한 뒤 반환한다
            /// </summary>
            public IList<StageGenerator.Grid> GetDoorGridsWithReset()
            {
                //. 그리드 리스트가 null일경우, 도어의 넓이 만큼 capacity로 생성한다
                if (DoorGrids == null)
                {
                    DoorGrids = new List<StageGenerator.Grid>(DoorWidth * DoorTotalLength);
                }

                //. 그리드 리스트가 존재핧경우, 도어의 넓이 만큼 capacity를 지정시키고 Clear 하여 초기화한다
                else
                {
                    DoorGrids.Capacity = DoorWidth * DoorTotalLength;
                    DoorGrids.Clear();
                }


                return DoorGrids;
            }



            #region 도어 입구중 가장 낮은/높은 그리드


            private StageGenerator.Grid currentGrid_DoorStart_Min = null;

            /// <summary>
            /// 이 도어의 입구 (Start) 에서 가장 낮은(Min) 그리드
            /// </summary>
            [FoldoutGroup("스테이지 생성 정보"), TitleGroup("스테이지 생성 정보/도어 그리드 정보")]
            [ShowInInspector]
            [Sirenix.OdinInspector.ReadOnly]
            [LabelText("도어 입구중 가장 낮은 그리드")]
            public StageGenerator.Grid CurrentGrid_DoorStart_Min { get => currentGrid_DoorStart_Min; set => currentGrid_DoorStart_Min = value; }




            private StageGenerator.Grid currentGrid_DoorStart_Max = null;

            /// <summary>
            /// 이 도어의 입구 (Start) 에서 가장 높은(Max) 그리드
            /// </summary>
            [FoldoutGroup("스테이지 생성 정보"), TitleGroup("스테이지 생성 정보/도어 그리드 정보")]
            [ShowInInspector]
            [Sirenix.OdinInspector.ReadOnly]
            [LabelText("도어 입구중 가장 높은 그리드")]
            public StageGenerator.Grid CurrentGrid_DoorStart_Max { get => currentGrid_DoorStart_Max; set => currentGrid_DoorStart_Max = value; }

            #endregion



            #region 도어 출구중 가장 낮은/높은 그리드


            private StageGenerator.Grid currentGrid_DoorEnd_Min;

            /// <summary>
            /// 이 도어의 출구 (End) 에서 가장 낮은(Min) 그리드
            /// </summary>
            [FoldoutGroup("스테이지 생성 정보"), TitleGroup("스테이지 생성 정보/도어 그리드 정보")]
            [ShowInInspector]
            [Sirenix.OdinInspector.ReadOnly]
            [LabelText("도어 출구중 가장 낮은 그리드")]
            public StageGenerator.Grid CurrentGrid_DoorEnd_Min { get => currentGrid_DoorEnd_Min; set => currentGrid_DoorEnd_Min = value; }




            private StageGenerator.Grid currentGrid_DoorEnd_Max;

            /// <summary>
            /// 이 도어의 출구 (End) 에서 가장 높은(Max) 그리드
            /// </summary>
            [FoldoutGroup("스테이지 생성 정보"), TitleGroup("스테이지 생성 정보/도어 그리드 정보")]
            [ShowInInspector]
            [Sirenix.OdinInspector.ReadOnly]
            [LabelText("도어 출구중 가장 높은 그리드")]
            public StageGenerator.Grid CurrentGrid_DoorEnd_Max { get => currentGrid_DoorEnd_Max; set => currentGrid_DoorEnd_Max = value; }

            #endregion



            #region 도어 NavMesh중 가장 낮은/높은 그리드


            private StageGenerator.Grid totalGrids_NavMesh_DownLeft;

            /// <summary>
            /// 이 도어의 NavMesh 범위의 좌측하단 그리드
            /// </summary>
            [FoldoutGroup("스테이지 생성 정보"), TitleGroup("스테이지 생성 정보/도어 그리드 정보")]
            [ShowInInspector]
            [Sirenix.OdinInspector.ReadOnly]
            [LabelText("연결된 도어 NavMesh중 가장 낮은 그리드")]
            [PropertyTooltip("이 도어와 연결된 도어의 그리드들 중에서 가장 낮은 그리드")]
            public StageGenerator.Grid TotalGrids_NavMesh_DownLeft { get => totalGrids_NavMesh_DownLeft; private set => totalGrids_NavMesh_DownLeft = value; }




            private StageGenerator.Grid totalGrids_NavMesh_UpRight;

            /// <summary>
            /// 이 도어의 NavMesh 범위의 우측상단 그리드
            /// </summary>
            [FoldoutGroup("스테이지 생성 정보"), TitleGroup("스테이지 생성 정보/도어 그리드 정보")]
            [ShowInInspector]
            [Sirenix.OdinInspector.ReadOnly]
            [LabelText("연결된 도어 NavMesh중 가장 높은 그리드")]
            [PropertyTooltip("이 도어와 연결된 도어의 그리드들 중에서 가장 높은 그리드")]
            public StageGenerator.Grid TotalGrids_NavMesh_UpRight { get => totalGrids_NavMesh_UpRight; private set => totalGrids_NavMesh_UpRight = value; }

            #endregion



            ///======================================================================================================================================================



            //? 도어의 연결 정보



            ///<summary>
            /// 연결된 도어
            /// </summary>
            [SerializeReference][HideInInspector] private Door connectedDoor;

            [FoldoutGroup("스테이지 생성 정보"), TitleGroup("스테이지 생성 정보/연결 도어 정보", Order = 2)]
            [ShowInInspector]
            [Sirenix.OdinInspector.ReadOnly]
            [LabelText("연결된 도어")]
            [PropertyOrder(1)]
            public Door ConnectecDoor { get => connectedDoor; set => connectedDoor = value; }



            [FoldoutGroup("스테이지 생성 정보"), TitleGroup("스테이지 생성 정보/연결 도어 정보")]
            [ShowInInspector]
            [Sirenix.OdinInspector.ReadOnly]
            [LabelText("복도 보정 그리드 등록 여부")]
            [PropertyOrder(2)]
            private bool isEnabledCorrectionHallway;

            /// <summary>
            ///  복도 보정 그리드 등록 여부
            /// </summary>
            public bool IsEnabledCorrectionHallway => isEnabledCorrectionHallway;



            [FoldoutGroup("스테이지 생성 정보"), TitleGroup("스테이지 생성 정보/연결 도어 정보")]
            [ShowInInspector]
            [Sirenix.OdinInspector.ReadOnly]
            [LabelText("복도 경로 그리드 등록 여부")]
            [PropertyOrder(3)]
            private bool isEnabledPathHallway;

            /// <summary>
            ///  복도 경로 그리드 등록 여부
            /// </summary>
            public bool IsEnabledPathHallway => isEnabledPathHallway;



            /// <summary>
            /// 연결된 도어를 해제하고, 생성된 복도 보정, 복도 경로 그리드 들도 초기화한다
            /// </summary>
            public void RemoveConnectedDoorInfo()
            {
                ConnectecDoor = null;
                SetDisable_HallwayCorrectionGrids(false, StageGenerator.GridManager.GridEvent_RemoveHallwayTags);
                SetDisable_HallwayPathGrids(false, StageGenerator.GridManager.GridEvent_RemoveHallwayTags);
            }



            ///======================================================================================================================================================



            //? 스테이지 생성: 복도 그리드 저장소



            ///<summary>
            ///도어로 부터 생성된 <b>복도 보정 그리드</b> 들을 저장하는 리스트
            /// </summary>
            [SerializeField]
            [HideInInspector]
            private List<StageGenerator.Grid> _hallwayCorrectionGridList = new List<StageGenerator.Grid>();

            [FoldoutGroup("스테이지 생성 정보"), TitleGroup("스테이지 생성 정보/복도 그리드 정보", Order = 3)]
            [ShowInInspector]
            [Sirenix.OdinInspector.ReadOnly]
            [LabelText("생성된 복도 보정 그리드")]
            public IReadOnlyList<StageGenerator.Grid> GetHallwayCorrectionGridList => _hallwayCorrectionGridList;



            /// <summary>
            ///도어로 부터 생성된 <b>복도 경로 그리드</b> 들을 저장하는 리스트
            /// <para>(복도 확장, 복도 안전구역 등 모든 복도 요소들이 포함됨)</para>
            /// </summary>
            [SerializeField]
            [HideInInspector]
            private List<StageGenerator.Grid> _hallwayPathGridList = new List<StageGenerator.Grid>();

            [FoldoutGroup("스테이지 생성 정보"), TitleGroup("스테이지 생성 정보/복도 그리드 정보")]
            [ShowInInspector]
            [Sirenix.OdinInspector.ReadOnly]
            [LabelText("생성된 복도 경로 그리드")]
            public IReadOnlyList<StageGenerator.Grid> GetHallwayPathGridList => _hallwayPathGridList;



            //? 복도 보정 그리드 메서드



            /// <summary>
            /// <c>생성된 복도 보정 그리드</c>에, 생성된 <b>복도 보정 그리드</b>들을 등록한다
            /// </summary>
            /// <param name="hallwayCorrectionGridList"></param>
            /// <returns></returns>
            public bool SetEnable_HallwayCorrectionGrids(List<StageGenerator.Grid> hallwayCorrectionGridList)
            {
                if (isEnabledCorrectionHallway) { return false; }

                _hallwayCorrectionGridList.AddRange(hallwayCorrectionGridList);
                isEnabledCorrectionHallway = true;
                //foreach (var item in _hallwayCorrectionGridList)
                //{
                //    item.AddTag(StageGenerator.GridTag.TempTest);
                //    item.GridDebuggingMemo += "door";
                //}
                return true;
            }



            /// <summary>
            /// <c>생성된 복도 보정 그리드</c>에, 생성된 <b>복도 보정 그리드</b>들을 해제한다
            /// </summary>
            /// <param name="clearGridSettings">그리드 내의 정보 자체를 초기화 할 것인지 여부</param>
            /// <param name="beforeClearGridEvent">복도 보정 그리드를 해제하기 전, 실행되는 이벤트</param>
            /// <returns></returns>
            public bool SetDisable_HallwayCorrectionGrids(bool clearGridSettings, Action<StageGenerator.Grid> beforeClearGridEvent = null)
            {
                if (!isEnabledCorrectionHallway) { return false; }

                if (clearGridSettings || beforeClearGridEvent != null)
                {
                    for (int i = 0; i < _hallwayCorrectionGridList.Count; i++)
                    {
                        StageGenerator.Grid grid = _hallwayCorrectionGridList[i];
                        if (clearGridSettings) { grid.ResetGridSettings(); }
                        beforeClearGridEvent?.Invoke(grid);
                    }
                }
                //foreach (var item in _hallwayCorrectionGridList)
                //{
                //    item.RemoveTag(StageGenerator.GridTag.TempTest);
                //    item.GridDebuggingMemo = "";
                //}
                _hallwayCorrectionGridList.Clear();
                isEnabledCorrectionHallway = false;

                return true;
            }



            /// <summary>
            /// 이 도어로부터 생성된  <b>복도 보정 그리드</b>들에게 이벤트를 실행한다<br/>
            /// </summary>
            public void HallwayCorrectionGridEvent(Action<StageGenerator.Grid> hallwayGridEvent)
            {
                if (_hallwayCorrectionGridList == null) { return; }

                for (int i = 0; i < _hallwayCorrectionGridList.Count; i++)
                {
                    StageGenerator.Grid grid = _hallwayCorrectionGridList[i];
                    hallwayGridEvent?.Invoke(grid);
                }
            }



            /// <summary>
            /// 이 도어로부터 생성된 <b>복도 보정 그리드</b>들을 모두 초기화한다
            /// <para>(해당 그리드의 설정을 초기화시키고, 보유중한 복도 그리드들을 제거한다)</para>
            /// </summary>
            public void ResetHallwayCorrectionGrids()
            {
                if (_hallwayCorrectionGridList == null) { return; }

                for (int i = 0; i < _hallwayCorrectionGridList.Count; i++)
                {
                    StageGenerator.Grid grid = _hallwayCorrectionGridList[i];
                    grid.ResetGridSettings();
                }

                _hallwayCorrectionGridList.Clear();
            }



            //? 복도 경로 그리드 메서드



            /// <summary>
            /// <c>생성된 복도 경로 그리드</c>에, 생성된 <b>복도 경로 그리드</b>들을 등록한다
            /// </summary>
            /// <param name="hallwayGridList"></param>
            /// <param name="totalGrids_NavMesh_DownLeft"></param>
            /// <param name="totalGrids_NavMesh_UpRight"></param>
            /// <returns></returns>
            public bool SetEnable_HallwayPathGrids(List<StageGenerator.Grid> hallwayGridList, StageGenerator.Grid totalGrids_NavMesh_DownLeft, StageGenerator.Grid totalGrids_NavMesh_UpRight)
            {
                if (isEnabledPathHallway) { return false; }

                _hallwayPathGridList.AddRange(hallwayGridList);
                TotalGrids_NavMesh_DownLeft = totalGrids_NavMesh_DownLeft;
                TotalGrids_NavMesh_UpRight = totalGrids_NavMesh_UpRight;
                //foreach (var item in _hallwayPathGridList)
                //{
                //    item.AddTag(StageGenerator.GridTag.TempTest);
                //    item.GridDebuggingMemo += "hallway";
                //}
                isEnabledPathHallway = true;

                return true;
            }



            /// <summary>
            /// <c>생성된 복도 경로 그리드</c>에, 생성된 <b>복도 경로 그리드</b>들을 해제
            /// </summary>
            /// <param name="connect"></param>
            /// <param name="hallwayGridList"></param>
            /// <param name="navMeshGrid_DownLeft"></param>
            /// <param name="navMeshGrid_UpRight"></param>
            /// <returns></returns>
            public bool SetDisable_HallwayPathGrids(bool clearGridSettings, Action<StageGenerator.Grid> beforeClearGridEvent = null)
            {
                if (!isEnabledPathHallway) { return false; }

                if (clearGridSettings || beforeClearGridEvent != null)
                {
                    for (int i = 0; i < _hallwayPathGridList.Count; i++)
                    {
                        StageGenerator.Grid grid = _hallwayPathGridList[i];
                        if (clearGridSettings) { grid.ResetGridSettings(); }
                        beforeClearGridEvent?.Invoke(grid);
                    }
                }
                //foreach (var item in _hallwayPathGridList)
                //{
                //    item.RemoveTag(StageGenerator.GridTag.TempTest);
                //    item.GridDebuggingMemo = "";
                //}
                _hallwayPathGridList.Clear();
                isEnabledPathHallway = false;

                return true;
            }



            /// <summary>
            /// 이 도어로부터 생성된  <b>복도 경로 그리드</b>들에게 이벤트를 실행한다<br/>
            /// </summary>
            public void HallwayPathGridEvent(Action<StageGenerator.Grid> hallwayGridEvent)
            {
                if (_hallwayPathGridList == null) { return; }

                for (int i = 0; i < _hallwayPathGridList.Count; i++)
                {
                    StageGenerator.Grid grid = _hallwayPathGridList[i];
                    hallwayGridEvent?.Invoke(grid);
                }
            }



            /// <summary>
            /// 이 도어로부터 생성된 <b>복도 경로 그리드</b>들을 모두 초기화한다
            /// <para>(해당 그리드의 설정을 초기화시키고, 보유중한 복도 그리드들을 제거한다)</para>
            /// </summary>
            public void ResetHallwayPathGrids()
            {
                if (_hallwayPathGridList == null) { return; }

                for (int i = 0; i < _hallwayPathGridList.Count; i++)
                {
                    StageGenerator.Grid grid = _hallwayPathGridList[i];
                    grid.ResetGridSettings();
                }
                _hallwayPathGridList.Clear();
            }



            ///======================================================================================================================================================



            //? 이벤트



            [FoldoutGroup("기타", false)]
            [LabelText("도어 활성화 이벤트")]
            [SerializeField]
            private UnityEvent EnableDoorEvent;

            [FoldoutGroup("기타", false)]
            [LabelText("도어 비활성화 이벤트")]
            [SerializeField]
            private UnityEvent DisableDoorEvent;



            [FoldoutGroup("기타", false)]
            [LabelText("도어 열기 이벤트")]
            [SerializeField]
            private UnityEvent OpenDoorEvent;

            [FoldoutGroup("기타", false)]
            [LabelText("도어 닫기 이벤트")]
            [SerializeField]
            private UnityEvent CloseDoorEvent;



            ///======================================================================================================================================================

#if UNITY_EDITOR

            [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Center, EnableRichText = true, Overflow = false), EnableGUI]
            [PropertyOrder(9999)]
            [PropertySpace(0, 8)]
            public string editor_JustPropertySpace => "";

#endif

            ///======================================================================================================================================================



            ///<summary>
            /// <see cref="Door"/> 최초 초기화
            /// </summary>
            public void InitializeRoomDoor(RoomObject parentRoomObject, EDirection4? direction)
            {
                Initialize(parentRoomObject);

                if (direction != null) doorDirection = direction.Value;
                //DisableDoor();
                //CloseDoor();
            }



            ///======================================================================================================================================================



            #region Legacy


            //! 이웃도어 간격 감지 일단 그냥 비활성화, 디버깅용이니까 정리대강끝나고 만들자


            //? 이웃 도어



            /////<summary>
            ///// 이웃 도어 (낮은쪽, 방향에 따라 결정)
            ///// </summary>
            //[ReadOnly][SerializeField] public RoomDoor Neighbor_Door_Low;

            /////<summary>
            ///// 이웃 도어 (높은은쪽, 방향에 따라 결정)
            ///// </summary>
            //[ReadOnly][SerializeField] public RoomDoor Neighbor_Door_High;


            ///// <summary>
            ///// 이웃 도어간의 간격 얻어보기
            ///// </summary>
            ///// <param name="isLowNeighbor">낮은쪽 이웃을 사용할지</param>
            ///// <param name="gap">반환되는 간격</param>
            ///// <returns></returns>
            //public bool TryGet_NeighborGap(bool isLowNeighbor, out float gap)
            //{
            //    RoomDoor targetRoomDoor;

            //    targetRoomDoor = isLowNeighbor ? neighbor_Door_Low : neighbor_Door_High;

            //    if (targetRoomDoor == null) { gap = 0; return false; }

            //    gap = GetDoorRect_Start().GetGap(targetRoomDoor.GetDoorRect_Start(), !CheckDoorDirectionVertical);

            //    return true;
            //}



            ///// <summary>
            ///// 이웃 도어간의 간격이 에러인지 확인하기 (에러라면 true)
            ///// </summary>
            ///// <param name="standard">간격이 이 값 미만일경우, 에러</param>
            ///// <returns></returns>
            //public bool TryGet_NeighborGapWarning(float standard = 0f)
            //{
            //    var isGap_Low = TryGet_NeighborGap(true, out var gap_Low);
            //    var isGap_High = TryGet_NeighborGap(false, out var gap_High);
            //    if (!isGap_Low && !isGap_High) { return false; }
            //    if (gap_Low < standard || gap_High < standard) { return true; }
            //    return false;
            //}



            ///======================================================================================================================================================



            ///// <summary>
            ///// Start 좌표를 사용하여, 중심점 그리드 좌표로 반환한다<br/>
            ///// 중심점을 2개 사용할경우, center2에 두번째 중심점이 반환되며,
            ///// True를 반환한다
            ///// </summary>
            ///// <param name="isDoubleCenter">중심점을 2개 사용하는지 여부</param>
            ///// <param name="center1">중심점1 (중심점을 하나만 사용한다면, 이걸 사용)</param>
            ///// <param name="center2">중심점2 (중심점이 2개라면, 두번때 중심점은 이걸 사용)</param>
            //[Obsolete("폐기준비")]
            //public void CenterGridPositions_Start(Vector3 origin, out bool isDoubleCenter, out Vector2Int center1, out Vector2Int center2)
            //{
            //    isDoubleCenter = CenterGridPositions(origin, DoorStartTransformPositionCurrent, out center1, out center2);
            //}
            ////CenterGridPositions_Start


            ///// <summary>
            ///// End 좌표를 사용하여, 중심점 그리드 좌표로 반환한다<br/>
            ///// 중심점을 2개 사용할경우, center2에 두번째 중심점이 반환되며,
            ///// True를 반환한다
            ///// </summary>
            ///// <param name="isDoubleCenter">중심점을 2개 사용하는지 여부</param>
            ///// <param name="center1">중심점1 (중심점을 하나만 사용한다면, 이걸 사용)</param>
            ///// <param name="center2">중심점2 (중심점이 2개라면, 두번때 중심점은 이걸 사용)</param>
            //[Obsolete("폐기준비")]
            //public void CenterGridPositions_End(Vector3 origin, out bool isDoubleCenter, out Vector2Int center1, out Vector2Int center2)
            //{
            //    isDoubleCenter = CenterGridPositions(origin, DoorEndTransformPositionCurrent, out center1, out center2);
            //}
            ////CenterGridPositions_End


            ///// <summary>
            ///// 도어의 좌표를 받아와, 중심점 그리드 좌표로 반환한다<br/>
            ///// 중심점을 2개 사용할경우, center2에 두번째 중심점이 반환되며,
            ///// True를 반환한다
            ///// </summary>
            ///// <param name="doorPosition">도어의 Vector3 좌표</param>
            ///// <param name="center1">중심점1 (중심점을 하나만 사용한다면, 이걸 사용)</param>
            ///// <param name="center2">중심점2 (중심점이 2개라면, 두번때 중심점은 이걸 사용)</param>
            ///// <returns>중심점을 2개 사용할경우 True를 반환한다</returns>
            //[Obsolete("폐기준비")]
            //private bool CenterGridPositions(Vector3 origin, Vector3 doorPosition, out Vector2Int center1, out Vector2Int center2)
            //{
            //    //! 이거 새로 추가한 DoorGridPosition 쓰면 더 간단하지않나

            //    //Refresh_EndDistance(); //한번 갱신해줘야 EndPos 잘읽어옴

            //    //? 도어의 너비(가변)이 짝수인지 확인
            //    bool doorWidthIsEven_SoOneCenter = DoorWidth.IsEven();

            //    var gridCompatible = ParentRoomObject.GridCompatible;
            //    var gridUnit = gridCompatible.CurrentSnapSetting.GridUnitOriginalVector2;

            //    doorPosition = doorPosition.SwizzlesVector(gridCompatible.CurrentSnapSetting.Swizzle);
            //    doorPosition -= origin;
            //    center1 = new Vector2Int((int)(doorPosition.x / gridUnit.x), (int)(doorPosition.y / gridUnit.y));


            //    //. 중심점 1을 구하는 방식은, 월드 좌표를 그리드 좌표로 연산,
            //    //. 중심점 2를 구하는 방식은 중심점 1을 기준으로 , x축과 y축중 하나를 한쪽 방향으로 -1 된 값이 된다 (도어의 방향에 따라 결정)


            //    //? 중심점2는, 도어의 폭이 짝수일때 계산하며
            //    if (doorWidthIsEven_SoOneCenter)
            //    {
            //        center2 = !CheckDoorDirectionVertical ?

            //            //? 도어의 방향이 (좌 or 우) 일 경우,
            //            //! 중심점1의 한칸 하단이 중심점2가된다
            //            new Vector2Int(center1.x, center1.y - 1) :

            //            //? 도어의 방향이 (하 or 상) 일경우,
            //            //! 중심점1의 한칸 좌측이 중심점2가된다
            //            new Vector2Int(center1.x - 1, center1.y);
            //    }

            //    //? 홀수라면 중심점1이랑 같은 값을 사용한다
            //    else
            //    {
            //        center2 = center1;
            //    }

            //    return doorWidthIsEven_SoOneCenter;
            //}



            #endregion



            ///======================================================================================================================================================
        }
    }
}
