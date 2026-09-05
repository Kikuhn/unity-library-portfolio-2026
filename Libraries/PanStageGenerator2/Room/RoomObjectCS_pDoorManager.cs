using Pan.Util;
using Sirenix.OdinInspector;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Pan.StageGenerators
{
    public partial class RoomObject
    {

        [Serializable]
        public class DoorManager : BaseManager
        {
            ///======================================================================================================================================================



            public override void WakeUp(RoomObject main)
            {
                base.WakeUp(main);

                Refresh_DoorList_Down();
                Refresh_DoorList_Up();
                Refresh_DoorList_Left();
                Refresh_DoorList_Right();
            }



            ///======================================================================================================================================================



            //? 도어 리스트



#if UNITY_EDITOR

            [TitleGroup("도어 리스트")]
            [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Center, EnableRichText = true), EnableGUI]
            [PropertyOrder(0)]
            private string dummyTitle_DoorsInfo
            {
                get
                {
                    return $"↓하단도어 <color=#2ecc71>{DoorList_Down.Count}</color>개 | ↑상단도어 <color=#2ecc71>{DoorList_Up.Count}</color>개 | ←좌측도어 <color=#2ecc71>{DoorList_Left.Count}</color>개 | →우측도어 <color=#2ecc71>{DoorList_Right.Count}</color>개";
                }
            }
#endif



            /// <summary>
            /// 하단 도어 리스트
            /// </summary>
            [TitleGroup("도어 리스트"), TabGroup("도어 리스트/탭", "↓하단 도어 목록")]
            [SerializeField]
            [OnValueChanged(nameof(Refresh_DoorList_Down), true)]
            [OnCollectionChanged(nameof(Refresh_DoorList_Down))]
            [ListDrawerSettings(CustomAddFunction = nameof(AddRoomDoor_Down), DraggableItems = false, DefaultExpandedState = true)]
            [LabelText("↓하단 도어 리스트")]
            [PropertyOrder(1)]
            private List<Door> DoorList_Down = new List<Door>();



            /// <summary>
            /// 상단 도어 리스트
            /// </summary>
            [TitleGroup("도어 리스트"), TabGroup("도어 리스트/탭", "↑상단 도어 목록")]
            [SerializeField]
            [OnValueChanged(nameof(Refresh_DoorList_Up), true)]
            [OnCollectionChanged(nameof(Refresh_DoorList_Up))]
            [ListDrawerSettings(CustomAddFunction = nameof(AddRoomDoor_Up), DraggableItems = false, DefaultExpandedState = true)]
            [LabelText("↑상단 도어 리스트")]
            [PropertyOrder(2)]
            private List<Door> DoorList_Up = new List<Door>();



            /// <summary>
            /// 좌측 도어 리스트
            /// </summary>
            [TitleGroup("도어 리스트"), TabGroup("도어 리스트/탭", "←좌측 도어 목록")]
            [SerializeField]
            [OnValueChanged(nameof(Refresh_DoorList_Left), true)]
            [OnCollectionChanged(nameof(Refresh_DoorList_Left))]
            [ListDrawerSettings(CustomAddFunction = nameof(AddRoomDoor_Left), DraggableItems = false, DefaultExpandedState = true)]
            [LabelText("←좌측 도어 리스트")]
            [PropertyOrder(3)]
            private List<Door> DoorList_Left = new List<Door>();



            /// <summary>
            /// 우측 도어 리스트
            /// </summary>
            [TitleGroup("도어 리스트"), TabGroup("도어 리스트/탭", "→우측 도어 목록")]
            [SerializeField]
            [OnValueChanged(nameof(Refresh_DoorList_Right), true)]
            [OnCollectionChanged(nameof(Refresh_DoorList_Right))]
            [ListDrawerSettings(CustomAddFunction = nameof(AddRoomDoor_Right), DraggableItems = false, DefaultExpandedState = true)]
            [LabelText("→우측 도어 리스트")]
            [PropertyOrder(4)]
            private List<Door> DoorList_Right = new List<Door>();



#if UNITY_EDITOR



            private string editorDoorsInfo(EDirection4 doorDirection)
            {
                var doorList = GetDoorList(doorDirection);

                var sb = new StringBuilder();
                //switch (doorDirection)
                //{
                //    case EDirection4.Down: sb.Append($"↓하단"); break;
                //    case EDirection4.Up: sb.Append($"↑상단"); break;
                //    case EDirection4.Left: sb.Append($"←좌측"); break;
                //    case EDirection4.Right: sb.Append($"→우측"); break;
                //}

                sb.AppendLine($"<size=13><color=white>총 도어 개수: {doorList.Count}</color></size>");

                if (TryGetNeighborDoorGaps(doorDirection, out var minGap, out var maxGap))
                {
                    if (minGap < 0 || maxGap < 0)
                    {
                        sb.AppendLine($"<size=13><color=red><b>도어 간격이 겹쳐있음! minGap: {minGap} maxGap: {maxGap}</b></color></size>");
                    }
                    else
                    {
                        sb.AppendLine($"<color=white>도어 간격 minGap: {minGap} maxGap: {maxGap}</color>");
                    }
                }

                return sb.ToString();
            }



            [TitleGroup("도어 리스트"), TabGroup("도어 리스트/탭", "↓하단 도어 목록")]
            [ShowInInspector, HideLabel, DisplayAsString(EnableRichText = true, Overflow = false, Alignment = TextAlignment.Center), EnableGUI]
            private string editorDoorsInfo_Down => editorDoorsInfo(EDirection4.Down);

            [TitleGroup("도어 리스트"), TabGroup("도어 리스트/탭", "↓하단 도어 목록")]
            [Button("↓하단 도어 비활성화 및 연결 정보 제거", Icon = SdfIconType.LightningFill), GUIColor(0.97f, 0.85f, 0.39f)]
            public void SetDisableAndRemoveConnectedDoorInfo_Down()
            {
                DoorListControlInternal(DoorList_Down, false, true);
            }



            [TitleGroup("도어 리스트"), TabGroup("도어 리스트/탭", "↑상단 도어 목록")]
            [ShowInInspector, HideLabel, DisplayAsString(EnableRichText = true, Overflow = false, Alignment = TextAlignment.Center), EnableGUI]
            private string editorDoorsInfo_Up => editorDoorsInfo(EDirection4.Up);

            [TitleGroup("도어 리스트"), TabGroup("도어 리스트/탭", "↑상단 도어 목록")]
            [Button("↑상단 도어 비활성화 및 연결 정보 제거", Icon = SdfIconType.LightningFill), GUIColor(0.97f, 0.85f, 0.39f)]
            public void SetDisableAndRemoveConnectedDoorInfo_Up()
            {
                DoorListControlInternal(DoorList_Up, false, true);
            }



            [TitleGroup("도어 리스트"), TabGroup("도어 리스트/탭", "←좌측 도어 목록")]
            [ShowInInspector, HideLabel, DisplayAsString(EnableRichText = true, Overflow = false, Alignment = TextAlignment.Center), EnableGUI]
            private string editorDoorsInfo_Left => editorDoorsInfo(EDirection4.Left);

            [TitleGroup("도어 리스트"), TabGroup("도어 리스트/탭", "←좌측 도어 목록")]
            [Button("←좌측 도어 비활성화 및 연결 정보 제거", Icon = SdfIconType.LightningFill), GUIColor(0.97f, 0.85f, 0.39f)]
            public void SetDisableAndRemoveConnectedDoorInfo_Left()
            {
                DoorListControlInternal(DoorList_Left, false, true);
            }



            [TitleGroup("도어 리스트"), TabGroup("도어 리스트/탭", "→우측 도어 목록")]
            [ShowInInspector, HideLabel, DisplayAsString(EnableRichText = true, Overflow = false, Alignment = TextAlignment.Center), EnableGUI]
            private string editorDoorsInfo_Right => editorDoorsInfo(EDirection4.Right);

            [TitleGroup("도어 리스트"), TabGroup("도어 리스트/탭", "→우측 도어 목록")]
            [Button("→우측 도어 비활성화 및 연결 정보 제거", Icon = SdfIconType.LightningFill), GUIColor(0.97f, 0.85f, 0.39f)]
            public void SetDisableAndRemoveConnectedDoorInfo_Right()
            {
                DoorListControlInternal(DoorList_Right, false, true);
            }



#endif



            /// <summary>
            /// 도어 총 개수
            /// </summary>
            [TitleGroup("도어 리스트")]
            [LabelText("총 도어 개수")]
            [ShowInInspector, EnableGUI, DisplayAsString]
            public int DoorTotalCount => DoorList_Down.Count + DoorList_Up.Count + DoorList_Left.Count + DoorList_Right.Count;



            /// <summary>
            /// 도어 리스트를 방향별로 얻기
            /// </summary>
            public List<Door> GetDoorList(EDirection4 doorDirection)
            {
                switch (doorDirection)
                {
                    case EDirection4.Down: return DoorList_Down;
                    case EDirection4.Up: return DoorList_Up;
                    case EDirection4.Left: return DoorList_Left;
                    case EDirection4.Right: return DoorList_Right;
                    default: return null;
                }
            }



            /// <summary>
            /// 해당 방향의 도어 리스트가 유효한지 (1개 이상인지) 확인
            /// </summary>
            public bool GetDoorEnabled(EDirection4 doorDirection)
            {
                return GetDoorList(doorDirection).Count > 0;
            }



            /// <summary>
            /// 4방향 도어 리스트를 하나로 통합해서 얻기
            /// <para>별도로 4개를 통합한 리스트를 생성해 반환</para>
            /// </summary>
            /// <returns></returns>
            public List<Door> GetTotalDoorList()
            {
                var totalDoorList = new List<Door>(DoorTotalCount);
                totalDoorList.AddRange(DoorList_Down);
                totalDoorList.AddRange(DoorList_Up);
                totalDoorList.AddRange(DoorList_Left);
                totalDoorList.AddRange(DoorList_Right);
                return totalDoorList;
            }



            /// <summary>
            /// 해당 방향에 활성화가 되어있는 도어의 개수를 가져오기
            /// </summary>
            /// <param name="doorDirection"></param>
            /// <returns></returns>
            public int GetEnabledDoorCount(EDirection4 doorDirection)
            {
                var doorList = GetDoorList(doorDirection);
                if (doorList.Count == 0) { return 0; }
                return doorList.CountWhere(x => x.DoorEnable);
            }



            //? 도어 리스트 컨트롤



            #region 도어 리스트에 도어 추가

            //[TitleGroup("도어 리스트"), ButtonGroup("도어 리스트/도어추가그룹")]
            //[Button("하단 도어 추가", Icon = SdfIconType.ArrowDown)]
            //[GUIColor(0.97f, 0.85f, 0.39f)]
            //[PropertyOrder(0)]
            private void AddRoomDoor_Down()
            {
                DoorList_Down.Add(CreateRoomDoor(EDirection4.Down));
            }

            //[TitleGroup("도어 리스트"), ButtonGroup("도어 리스트/도어추가그룹")]
            //[Button("상단 도어 추가", Icon = SdfIconType.ArrowUp)]
            //[GUIColor(0.97f, 0.85f, 0.39f)]
            //[PropertyOrder(0)]
            private void AddRoomDoor_Up()
            {
                DoorList_Up.Add(CreateRoomDoor(EDirection4.Up));
            }

            //[TitleGroup("도어 리스트"), ButtonGroup("도어 리스트/도어추가그룹")]
            //[Button("좌측 도어 추가", Icon = SdfIconType.ArrowLeft)]
            //[GUIColor(0.97f, 0.85f, 0.39f)]
            //[PropertyOrder(0)]
            private void AddRoomDoor_Left()
            {
                DoorList_Left.Add(CreateRoomDoor(EDirection4.Left));
            }

            //[TitleGroup("도어 리스트"), ButtonGroup("도어 리스트/도어추가그룹")]
            //[Button("우측 도어 추가", Icon = SdfIconType.ArrowRight)]
            //[GUIColor(0.97f, 0.85f, 0.39f)]
            //[PropertyOrder(0)]
            private void AddRoomDoor_Right()
            {
                DoorList_Right.Add(CreateRoomDoor(EDirection4.Right));
            }



            /// <summary>
            /// <see cref="Door"/>를 초기화 하여 생성
            /// </summary>
            /// <param name="doorDirection"></param>
            /// <returns></returns>
            protected Door CreateRoomDoor(EDirection4 doorDirection)
            {
                var roomDoor = new Door();
                roomDoor.InitializeRoomDoor(Main, doorDirection);
                return roomDoor;
            }



            #endregion



            #region 도어 리스트 리프레시

            private void Refresh_DoorList_Down()
            {
                for (int i = 0; i < DoorList_Down.Count; i++)
                {
                    DoorList_Down[i].InitializeRoomDoor(Main, EDirection4.Down);
                }
            }
            private void Refresh_DoorList_Up()
            {
                for (int i = 0; i < DoorList_Up.Count; i++)
                {
                    DoorList_Up[i].InitializeRoomDoor(Main, EDirection4.Up);
                }
            }
            private void Refresh_DoorList_Left()
            {
                for (int i = 0; i < DoorList_Left.Count; i++)
                {
                    DoorList_Left[i].InitializeRoomDoor(Main, EDirection4.Left);
                }
            }
            private void Refresh_DoorList_Right()
            {
                for (int i = 0; i < DoorList_Right.Count; i++)
                {
                    DoorList_Right[i].InitializeRoomDoor(Main, EDirection4.Right);
                }
            }

            #endregion



            /// <summary>
            /// 도어 리스트를 순회하며 동작을 수행한다
            /// </summary>
            /// <param name="doorList">대상 도어 리스트</param>
            /// <param name="doorEnable">해당 도어를 활성화 시킬지 여부</param>
            /// <param name="removeConnectedDoorInfo">해당 도어의 연결 도어 정보를 초기화 시킬지 여부</param>
            private void DoorListControlInternal(List<Door> doorList, bool doorEnable, bool removeConnectedDoorInfo)
            {
                foreach (var door in doorList)
                {
                    if (door == null) { continue; }
                    door.ActiveDoor(doorEnable);
                    if (removeConnectedDoorInfo) { door.RemoveConnectedDoorInfo(); }
                }
            }



            /// <summary>
            /// 모든 도어들을 비활성화 시키고, 연결된 도어 정보를 모두 제거한다
            /// </summary>
            [Button("모든 도어 비활성화 및 연결 정보 제거", Icon = SdfIconType.LightningFill), GUIColor(0.97f, 0.85f, 0.39f)]
            public void SetDisableAndRemoveConnectedDoorInfo_AllDoors()
            {
                DoorListControlInternal(DoorList_Down, false, true);
                DoorListControlInternal(DoorList_Up, false, true);
                DoorListControlInternal(DoorList_Left, false, true);
                DoorListControlInternal(DoorList_Right, false, true);
            }



            ///======================================================================================================================================================



            //? 도어리스트 내의 도어 정보



            /// <summary>
            /// 지정 <paramref name="doorDirection"/> 방향에서 가장 큰 <b>도어 너비</b>를 구합니다.<br/>
            /// 도어가 없으면 -1을 반환하고 <c>false</c>를 리턴합니다.
            /// </summary>
            /// <param name="doorDirection">검색할 방향</param>
            /// <param name="doorWidth">최대 너비, 없으면 -1</param>
            /// <returns>도어가 존재하면 <c>true</c>, 없으면 <c>false</c></returns>
            public bool TryGetMaxDoorWidth(EDirection4 doorDirection, out int doorWidth)
            {
                var doorList = GetDoorList(doorDirection);
                if (doorList.Count == 0) { doorWidth = -1; return false; }

                int maxDoorWidth = doorList[0].DoorWidth;                   //? 첫 값으로 초기화

                for (int i = 1; i < doorList.Count; i++)
                {
                    int width = doorList[i].DoorWidth;
                    if (width > maxDoorWidth)
                    {
                        maxDoorWidth = width;                               //! 더 크면 갱신
                    }
                }

                doorWidth = maxDoorWidth;
                return true;
            }



            /// <summary>
            /// 4 방향(Down·Up·Left·Right)의 최대 도어 너비를 한 번에 구합니다.<br/>
            /// 해당 방향에 도어가 없으면 -1이 반환되며, 네 방향 모두 없으면 <c>false</c>를 반환합니다.
            /// </summary>
            /// <param name="downDoorWidth">Down 방향 최대 너비(없으면 -1)</param>
            /// <param name="upDoorWidth">Up 방향 최대 너비(없으면 -1)</param>
            /// <param name="leftDoorWidth">Left 방향 최대 너비(없으면 -1)</param>
            /// <param name="rightDoorWidth">Right 방향 최대 너비(없으면 -1)</param>
            /// <returns>하나라도 도어가 존재하면 <c>true</c>, 전부 없으면 <c>false</c></returns>
            public bool TryGetMaxDoorWidths(out int downDoorWidth, out int upDoorWidth, out int leftDoorWidth, out int rightDoorWidth)
            {
                bool hasAny = false;

                hasAny |= TryGetMaxDoorWidth(EDirection4.Down, out downDoorWidth);
                hasAny |= TryGetMaxDoorWidth(EDirection4.Up, out upDoorWidth);
                hasAny |= TryGetMaxDoorWidth(EDirection4.Left, out leftDoorWidth);
                hasAny |= TryGetMaxDoorWidth(EDirection4.Right, out rightDoorWidth);

                return hasAny;
            }



            /// <summary>
            /// 네 방향 전체에서 가장 큰 <b>도어 너비</b>를 구합니다.<br/>
            /// 도어가 하나도 없으면 -1을 반환하고 <c>false</c>를 리턴합니다.
            /// </summary>
            /// <param name="doorWidth">최대 너비(없으면 -1)</param>
            /// <returns>도어가 하나라도 있으면 <c>true</c>, 없으면 <c>false</c></returns>
            public bool TryGetMaxDoorWidthAll(out int doorWidth)
            {
                int maxWidth = -1;                       //. 초기값 (-1: 없음)
                bool hasDoor = false;                    //. 도어 존재 여부

                //? 각 방향별 리스트를 개별 처리 (로컬 함수로 중복 최소화)
                ProcessList(GetDoorList(EDirection4.Down));
                ProcessList(GetDoorList(EDirection4.Up));
                ProcessList(GetDoorList(EDirection4.Left));
                ProcessList(GetDoorList(EDirection4.Right));

                doorWidth = maxWidth;
                return hasDoor;

                //? 로컬 함수: 리스트 순회하며 최대값 · 존재 여부 갱신
                void ProcessList(List<Door> list)
                {
                    if (list.Count == 0) return;

                    hasDoor = true;                      //! 하나라도 존재
                    for (int i = 0; i < list.Count; i++)
                    {
                        int w = list[i].DoorWidth;
                        if (w > maxWidth) maxWidth = w;  //! 더 크면 갱신
                    }
                }
            }



            /// <summary>
            /// 지정 <paramref name="doorDirection"/> 방향에서 가장 큰 <b>도어 총 길이 거리</b>를 구합니다.<br/>
            /// 도어가 없으면 -1을 반환하고 <c>false</c>를 리턴합니다.
            /// </summary>
            /// <param name="doorDirection">검색할 방향</param>
            /// <param name="doorTotalDistance">최대 총 길이 거리, 없으면 -1</param>
            /// <returns>도어가 존재하면 <c>true</c>, 없으면 <c>false</c></returns>
            public bool TryGetMaxFullDoorTotalDistance(EDirection4 doorDirection, out int doorTotalDistance)
            {
                var doorList = GetDoorList(doorDirection);
                if (doorList.Count == 0) { doorTotalDistance = -1; return false; }

                int maxDoorTotalDistance = doorList[0].FullDoorTotalDistance;

                for (int i = 1; i < doorList.Count; i++)
                {
                    int totalDistance = doorList[i].FullDoorTotalDistance;
                    if (totalDistance > maxDoorTotalDistance)
                    {
                        maxDoorTotalDistance = totalDistance;
                    }
                }

                doorTotalDistance = maxDoorTotalDistance;
                return true;
            }



            /// <summary>
            /// 지정 <paramref name="doorDirection"/> 방향에서 가장 큰 <b>도어 총 트랜스폼 길이 거리</b>를 구합니다.<br/>
            /// 도어가 없으면 -1을 반환하고 <c>false</c>를 리턴합니다.
            /// </summary>
            /// <param name="doorDirection">검색할 방향</param>
            /// <param name="doorTotalTransformDistance">최대 총 트랜스폼 길이 거리, 없으면 -1</param>
            /// <returns>도어가 존재하면 <c>true</c>, 없으면 <c>false</c></returns>
            public bool TryGetMaxFullDoorTotalTransformDistance(EDirection4 doorDirection, out float doorTotalTransformDistance)
            {
                var doorList = GetDoorList(doorDirection);
                if (doorList.Count == 0) { doorTotalTransformDistance = -1; return false; }

                float maxDoorTotalTransformDistance = doorList[0].FullDoorTotalTransformDistance;

                for (int i = 1; i < doorList.Count; i++)
                {
                    int totalTransformDistance = doorList[i].FullDoorTotalTransformDistance;
                    if (totalTransformDistance > maxDoorTotalTransformDistance)
                    {
                        maxDoorTotalTransformDistance = totalTransformDistance;
                    }
                }

                doorTotalTransformDistance = maxDoorTotalTransformDistance;
                return true;
            }



            /// <summary>
            /// <paramref name="doorDirection"/> 방향 도어들의
            /// <see cref="Door.FullDoorTotalDistance"/> 를 조사해  
            /// <paramref name="needCount"/> 개만 선택한 뒤,
            /// 그 집합에서 가장 큰 값을 반환한다.<br/>
            /// 선택 대상은 <paramref name="isMin"/> 값으로 결정한다.  
            /// <list type="bullet">
            ///   <item><b>isMin == true</b>  → 작은 값부터 <paramref name="needCount"/> 개</item>
            ///   <item><b>isMin == false</b> → 큰 값부터 <paramref name="needCount"/> 개</item>
            /// </list>
            /// 도어가 없거나 <paramref name="needCount"/> ≤ 0 이면 –1 반환, <c>false</c> 리턴.
            /// </summary>
            public bool TryGetDoorTotalDistanceByCount
            (
                EDirection4 doorDirection,
                int needCount,
                bool isMin,
                out int doorTotalDistance
            )
            {
                doorTotalDistance = -1;                          //. 실패 기본값

                var doors = GetDoorList(doorDirection);
                if (doors.Count == 0 || needCount <= 0) return false;

                int take = Mathf.Min(needCount, doors.Count);   //. 실제 확인할 개수

                //. 큰 값 선택이거나 전체를 확인할 때는 정렬 없이 최댓값을 구한다
                //. 작은 값 선택은 앞에서 needCount 개를 추출한 뒤 그중 최댓값을 반환한다
                doorTotalDistance = isMin && take < doors.Count
                    ? SelectKthSmallestDoorDistance(doors, take, useTransformDistance: false)
                    : GetMaximumDoorDistance(doors, useTransformDistance: false);

                return true;
            }



            /// <summary>
            /// <paramref name="doorDirection"/> 방향 도어들의
            /// <see cref="Door.FullDoorTotalTransformDistance"/> 를 조사해  
            /// <paramref name="needCount"/> 개만 추출한 뒤
            /// 그 집합에서 가장 큰 값을 반환한다.<br/>
            /// 추출 대상은 <paramref name="isMin"/> 으로 결정된다.
            /// <list type="bullet">
            ///   <item><b>isMin == true</b>  → 작은 값부터 <paramref name="needCount"/> 개 선택</item>
            ///   <item><b>isMin == false</b> → 큰 값부터 <paramref name="needCount"/> 개 선택</item>
            /// </list>
            /// 도어가 없거나 <paramref name="needCount"/> ≤ 0 이면 –1 f 를 반환하고 <c>false</c> 리턴.
            /// </summary>
            public bool TryGetDoorTotalTransformDistanceByCount
            (
                EDirection4 doorDirection,
                int needCount,
                bool isMin,
                out float doorTotalTransformDistance
            )
            {
                doorTotalTransformDistance = -1f;                        //. 실패 기본값

                var doors = GetDoorList(doorDirection);
                if (doors.Count == 0 || needCount <= 0) return false;

                int take = Mathf.Min(needCount, doors.Count);            //. 실제 확인할 개수

                //. 큰 값 선택이거나 전체를 확인할 때는 정렬 없이 최댓값을 구한다
                //. 작은 값 선택은 앞에서 needCount 개를 추출한 뒤 그중 최댓값을 반환한다
                doorTotalTransformDistance = isMin && take < doors.Count
                    ? SelectKthSmallestDoorDistance(doors, take, useTransformDistance: true)
                    : GetMaximumDoorDistance(doors, useTransformDistance: true);

                return true;
            }



            private static int SelectKthSmallestDoorDistance(
                IReadOnlyList<Door> doors,
                int take,
                bool useTransformDistance)
            {
                const int StackallocThreshold = 128;
                int[] rentedDistances = null;
                Span<int> distances = doors.Count <= StackallocThreshold
                    ? stackalloc int[doors.Count]
                    : (rentedDistances = ArrayPool<int>.Shared.Rent(doors.Count));

                try
                {
                    for (int i = 0; i < doors.Count; i++)
                    {
                        distances[i] = useTransformDistance
                            ? doors[i].FullDoorTotalTransformDistance
                            : doors[i].FullDoorTotalDistance;
                    }

                    return SelectKthSmallest(distances.Slice(0, doors.Count), take - 1);
                }
                finally
                {
                    if (rentedDistances != null)
                    {
                        ArrayPool<int>.Shared.Return(rentedDistances, clearArray: false);
                    }
                }
            }



            private static int SelectKthSmallest(Span<int> values, int targetIndex)
            {
                int left = 0;
                int right = values.Length - 1;

                while (left < right)
                {
                    int pivot = values[left + ((right - left) >> 1)];
                    int lower = left;
                    int upper = right;

                    while (lower <= upper)
                    {
                        while (values[lower] < pivot) { lower++; }
                        while (values[upper] > pivot) { upper--; }

                        if (lower > upper) { break; }

                        (values[lower], values[upper]) = (values[upper], values[lower]);
                        lower++;
                        upper--;
                    }

                    if (targetIndex <= upper)
                    {
                        right = upper;
                    }
                    else if (targetIndex >= lower)
                    {
                        left = lower;
                    }
                    else
                    {
                        return values[targetIndex];
                    }
                }

                return values[left];
            }



            private static int GetMaximumDoorDistance(
                IReadOnlyList<Door> doors,
                bool useTransformDistance)
            {
                int maximum = int.MinValue;

                for (int i = 0; i < doors.Count; i++)
                {
                    int distance = useTransformDistance
                        ? doors[i].FullDoorTotalTransformDistance
                        : doors[i].FullDoorTotalDistance;

                    if (distance > maximum) { maximum = distance; }
                }

                return maximum;
            }



            /// <summary>
            /// 지정 <paramref name="doorDirection"/> 방향의 모든 도어에 대해
            /// <b>DoorAxisOverhangSigned</b>의 최솟값과 최댓값을 계산합니다.
            /// <br/>도어가 하나도 없으면 <c>false</c> 를 반환하고, 두 값 모두 -1로 설정합니다.
            /// </summary>
            /// <param name="doorDirection">검사할 방향</param>
            /// <param name="minOverhang">가장 작은 Overhang(음수 가능)</param>
            /// <param name="maxOverhang">가장 큰 Overhang(양수 가능)</param>
            /// <returns>도어가 하나 이상 존재하면 <c>true</c>, 아니면 <c>false</c></returns>
            public bool TryGetDoorAxisOverhangSignedExtrema(EDirection4 doorDirection, out int minOverhang, out int maxOverhang)
            {
                var doors = GetDoorList(doorDirection);

                //. 기본 실패값
                minOverhang = -1;
                maxOverhang = -1;

                //! 도어가 없으면 실패
                if (doors == null || doors.Count == 0) return false;

                int minVal = int.MaxValue;   //? 최솟값 누적
                int maxVal = int.MinValue;   //? 최댓값 누적

                for (int i = 0; i < doors.Count; i++)
                {
                    int v = doors[i].DoorAxisOverhangSigned;   //? 음수: 좌/하로 초과, 양수: 우/상으로 초과, 0: 초과 없음
                    if (v < minVal) minVal = v;                //! 더 작으면 갱신
                    if (v > maxVal) maxVal = v;                //! 더 크면 갱신
                }

                minOverhang = minVal;
                maxOverhang = maxVal;
                return true;
            }



            /// <summary>
            /// 지정 방향 도어들의 축 초과량을 <b>쪽별</b>로 집계합니다.
            /// <br/>worstLow: 좌/하 쪽으로 가장 많이 넘친 셀 수(>=0)
            /// <br/>worstHigh: 우/상 쪽으로 가장 많이 넘친 셀 수(>=0)
            /// <br/>도어가 하나도 없으면 false와 함께 -1, -1 반환.
            /// </summary>
            public bool TryGetDoorAxisOverhangPerSideExtrema(EDirection4 doorDirection, out int worstLow, out int worstHigh)
            {
                var list = GetDoorList(doorDirection);

                if (list == null || list.Count == 0)
                {
                    worstLow = -1;   //. 결과 없음
                    worstHigh = -1;  //. 결과 없음
                    return false;
                }

                //. 방 내부 유효 인덱스 범위 [minInside..maxInside] 산출
                bool vertical = (doorDirection == EDirection4.Down) || (doorDirection == EDirection4.Up);
                int L = vertical
                    ? Main.GridCompatible.ObjectSizeX_Width
                    : Main.GridCompatible.ObjectSizeY_Height;

                int minInside = -(L / 2);
                int maxInside = minInside + (L - 1);

                int maxLow = 0;   //. 좌/하 초과 최댓값(셀)
                int maxHigh = 0;  //. 우/상 초과 최댓값(셀)

                for (int i = 0; i < list.Count; i++)
                {
                    var d = list[i];

                    //. 도어가 차지하는 축 범위 [left..right] (포함 범위)
                    int w = d.DoorWidth;
                    int axis = d.DoorGridPositionAxis;
                    int left, right;

                    if ((w & 1) == 0)
                    {
                        //? 짝수 폭: 중심 2칸 → axis는 좌/하 중심
                        int half = w / 2;
                        left = axis - (half - 1);
                        right = axis + half;
                    }
                    else
                    {
                        //? 홀수 폭: 단일 중심
                        int half = (w - 1) / 2;
                        left = axis - half;
                        right = axis + half;
                    }

                    //. 쪽별 초과량(음수 없음, 내부면 0)
                    int overflowLow = minInside - left;    //. 좌/하
                    if (overflowLow > 0 && overflowLow > maxLow) maxLow = overflowLow;

                    int overflowHigh = right - maxInside;   //. 우/상
                    if (overflowHigh > 0 && overflowHigh > maxHigh) maxHigh = overflowHigh;
                }

                worstLow = maxLow;
                worstHigh = maxHigh;
                return true;
            }



            /// <summary>
            /// 지정 <paramref name="doorDirection"/> 방향의 도어들을 축 기준으로 정렬하여,
            /// 인접 도어 쌍들의 간격(셀 수)의 최소/최대를 구합니다.
            /// <br/>도어가 2개 미만이면 <c>false</c> 를 반환하고 <paramref name="minGap"/> / <paramref name="maxGap"/> 에 -1을 설정합니다.
            /// <br/>간격 정의: <b>다음 도어의 시작셀 − 이전 도어의 끝셀 − 1</b>
            /// (겹치면 음수, 맞닿으면 0, 떨어지면 양수)
            /// </summary>
            /// <param name="doorDirection">검사할 방향</param>
            /// <param name="minGap">가장 작은 인접 간격(겹치면 음수)</param>
            /// <param name="maxGap">가장 큰 인접 간격</param>
            /// <returns>해당 방향에 도어가 2개 이상이면 <c>true</c>, 아니면 <c>false</c></returns>
            public bool TryGetNeighborDoorGaps(EDirection4 doorDirection, out int minGap, out int maxGap)
            {
                var list = GetDoorList(doorDirection);

                if (list == null || list.Count <= 1)
                {
                    minGap = -1;  //. 결과 없음
                    maxGap = -1;  //. 결과 없음
                    return false;
                }

                //? DoorGridPositionAxis 기준으로 안정 정렬 (Down/Up= X축, Left/Right= Y축)
                var ordered = list.OrderBy(door => door.DoorGridPositionAxis).ToList();

                //? 첫 쌍으로 초기화
                GetSpan(ordered[0], out int prevLeft, out int prevRight);
                int localMin = int.MaxValue;
                int localMax = int.MinValue;

                for (int i = 1; i < ordered.Count; i++)
                {
                    GetSpan(ordered[i], out int curLeft, out int curRight);

                    //. 인접 간격: 다음 시작 - 이전 끝 - 1
                    int gap = curLeft - prevRight - 1;

                    if (gap < localMin) localMin = gap;              //! 최소 갱신
                    if (gap > localMax) localMax = gap;              //! 최대 갱신

                    prevLeft = curLeft;
                    prevRight = curRight;
                }

                minGap = localMin;
                maxGap = localMax;
                return true;

                //? 도어가 차지하는 축 범위 [left..right] (포함 범위) 계산
                //.  짝수 폭은 "왼쪽/하단 중심"을 DoorGridPositionAxis가 가리킨다고 가정 (이전 로직과 동일)
                void GetSpan(RoomObject.Door d, out int left, out int right)
                {
                    int w = d.DoorWidth;

                    if ((w & 1) == 0)
                    {
                        //. 짝수 폭: 중심 2칸 → Axis는 좌/하 중심
                        int half = w / 2;
                        left = d.DoorGridPositionAxis - (half - 1);   //? 예: w=4, axis=0 → [ -1 .. 2 ]
                        right = d.DoorGridPositionAxis + half;
                    }
                    else
                    {
                        //. 홀수 폭: 단일 중심
                        int half = (w - 1) / 2;
                        left = d.DoorGridPositionAxis - half;         //? 예: w=3, axis=0 → [ -1 .. 1 ]
                        right = d.DoorGridPositionAxis + half;
                    }
                }
            }



            /// <summary>
            /// 도어들 중에, 같은 방향 내에서 서로 겹쳐 있는(간격 < 0) 도어가 하나라도 있으면 true.
            /// </summary>
            public bool CheckDoorGaps_Overlapped()
            {
                //. 네 방향 모두 검사
                return HasOverlap(EDirection4.Down)
                    || HasOverlap(EDirection4.Up)
                    || HasOverlap(EDirection4.Left)
                    || HasOverlap(EDirection4.Right);

                //? 해당 방향의 도어들을 축 기준으로 정렬 후,
                //? 인접 도어 간격의 최소값이 음수면 "겹침"으로 판정
                bool HasOverlap(EDirection4 dir)
                {
                    if (!TryGetNeighborDoorGaps(dir, out var minGap, out _))
                        return false; //. 도어가 2개 미만이면 겹침 없음

                    return minGap < 0; //! 인접 쌍 중 하나라도 겹치면 true
                }
            }



            ///======================================================================================================================================================



            #region Legacy



            //? 도어 Rect Mid



            ///// <summary>
            ///// 받아온 도어 방향에 있는 MidRect를 모두 얻어 <paramref name="targetCollection"/>에 추가
            ///// </summary>
            //public void GetDoorRect_Mid(ESelectActivesMode getMode, EDirection4 doorDir, in ICollection<Rect> targetCollection)
            //{
            //    foreach (var roomDoor in GetDoorList(doorDir))
            //    {
            //        switch (getMode)
            //        {
            //            case ESelectActivesMode.All:

            //            targetCollection.Add(roomDoor.FullDoorTransformRect);

            //            break;

            //            case ESelectActivesMode.OnlyEnable:

            //            if (roomDoor.DoorEnable)
            //            {
            //                targetCollection.Add(roomDoor.FullDoorTransformRect);
            //            }

            //            break;

            //            case ESelectActivesMode.OnlyDisable:

            //            if (!roomDoor.DoorEnable)
            //            {
            //                targetCollection.Add(roomDoor.FullDoorTransformRect);
            //            }
            //            break;
            //        }
            //    }
            //}



            ///// <summary>
            ///// 받아온 도어 방향에 있는 MidRect를 모두 얻어 리스트로 반환
            ///// </summary>
            //public List<Rect> GetDoorRect_Mid(ESelectActivesMode getMode, EDirection4 doorDir)
            //{
            //    List<Rect> result = new List<Rect>();

            //    GetDoorRect_Mid(getMode, doorDir, result);

            //    return result;
            //}



            ///// <summary>
            ///// 받아온 도어 방향들에 있는 MidRect를 모두 얻어 <paramref name="targetCollection"/>에 추가
            ///// </summary>
            //public void GetDoorRects_Mid(ESelectActivesMode getMode, in ICollection<Rect> targetCollection, params EDirection4[] doorDirs)
            //{
            //    for (int i = 0; i < doorDirs.Length; i++)
            //    {
            //        GetDoorRect_Mid(getMode, doorDirs[i], targetCollection);
            //    }
            //}



            ///// <summary>
            ///// 받아온 도어 방향들에 있는 MidRect를 모두 얻어 리스트로 반환
            ///// </summary>
            //public List<Rect> GetDoorRects_Mid(ESelectActivesMode getMode, params EDirection4[] doorDirs)
            //{
            //    List<Rect> result = new List<Rect>();

            //    GetDoorRects_Mid(getMode, result, doorDirs);

            //    return result;
            //}



            ///// <summary>
            ///// 모든 도어들(4방향)에 있는 MidRect를 모두 얻어 <paramref name="targetCollection"/>에 추가
            ///// </summary>
            //public void GetAllDoorRect_Mid(ESelectActivesMode getMode, in ICollection<Rect> targetCollection)
            //{
            //    GetDoorRect_Mid(getMode, EDirection4.Down, in targetCollection);
            //    GetDoorRect_Mid(getMode, EDirection4.Up, in targetCollection);
            //    GetDoorRect_Mid(getMode, EDirection4.Left, in targetCollection);
            //    GetDoorRect_Mid(getMode, EDirection4.Right, in targetCollection);
            //}



            ///// <summary>
            ///// 모든 도어들(4방향)에 있는 MidRect를 모두 얻어 리스트로 반환
            ///// </summary>
            //public List<Rect> GetAllDoorRect_Mid(ESelectActivesMode getMode)
            //{
            //    List<Rect> result = new List<Rect>();

            //    GetAllDoorRect_Mid(getMode, result);

            //    return result;
            //}

            ///

            //! 250627 새벽 비활성화

            ///// <summary>
            ///// 도어들의 MidRect를 통해, Rect를 얻는다<br/>
            ///// 가장 좌하단에있는 MidRect와 가장 우상단에있는 MidRect를 통해 Rect를 만든다<br/>
            ///// (도어들이 포함된 방 안전구역을 얻어올때 사용됨)<br/>
            ///// <b>사실상 방 안전구역을 가져오는 것과 같다</b>
            ///// </summary>
            ///// <param name="doorSelectMode">
            ///// 도어를 확인할때, 도어의 상태 고려<br/>
            ///// All: 도어를 무조건 가져옴<br/>
            ///// OnlyEnable: 활성화된 도어들만 고려됨<br/>
            ///// OnlyDisable: 비활성화된 도어들만 고려됨<br/>
            ///// </param>
            ///// <param name="useDoor_Down">하단 도어들이 포함될지 여부</param>
            ///// <param name="useDoor_Up">상단 도어들이 포함될지 여부</param>
            ///// <param name="useDoor_Left">좌측 도어들이 포함될지 여부</param>
            ///// <param name="useDoor_Right">우측 도어들이 포함될지 여부</param>
            ///// <param name="standardBottomLeft">기준이되는 좌하단 벡터</param>
            ///// <param name="standardTopRight">기존이되는 우상단 벡터</param>
            //public Rect GetRect_ByDoorMidRects(ESelectActivesMode doorSelectMode, bool useDoor_Down, bool useDoor_Up, bool useDoor_Left, bool useDoor_Right, Vector2 standardBottomLeft, Vector2 standardTopRight)
            //{
            //    // 방향별로 도어 확인 및 Rect 갱신
            //    if (useDoor_Down) { UpdateRectBounds(doorSelectMode, EDirection4.Down, ref standardBottomLeft, ref standardTopRight); }
            //    if (useDoor_Up) { UpdateRectBounds(doorSelectMode, EDirection4.Up, ref standardBottomLeft, ref standardTopRight); }
            //    if (useDoor_Left) { UpdateRectBounds(doorSelectMode, EDirection4.Left, ref standardBottomLeft, ref standardTopRight); }
            //    if (useDoor_Right) { UpdateRectBounds(doorSelectMode, EDirection4.Right, ref standardBottomLeft, ref standardTopRight); }

            //    // Rect를 만들어 반환한다
            //    return SU_TF_Rect.RectFromCorners(standardBottomLeft, standardTopRight);
            //}



            ///// <summary>
            ///// 특정 방향의 도어들을 순회하며 좌하단 및 우상단 값을 갱신
            ///// </summary>
            //private void UpdateRectBounds(ESelectActivesMode doorSelectMode, EDirection4 doorDir, ref Vector2 bottomLeft, ref Vector2 topRight)
            //{
            //    foreach (var roomDoor in GetDoorList(doorDir))
            //    {
            //        // 도어 상태에 따라 필터링
            //        if (doorSelectMode == ESelectActivesMode.OnlyEnable && !roomDoor.DoorEnable) continue;
            //        if (doorSelectMode == ESelectActivesMode.OnlyDisable && roomDoor.DoorEnable) continue;

            //        // 도어의 MidRect 가져오기
            //        Rect midDoorRect = roomDoor.FullDoorTransformRect;

            //        // bottomLeft 갱신
            //        bottomLeft = Vector2.Min(bottomLeft, midDoorRect.GetPosition(ECenterStandard.LowerLeft));

            //        // topRight 갱신
            //        topRight = Vector2.Max(topRight, midDoorRect.GetPosition(ECenterStandard.UpperRight));
            //    }
            //}

            ///

            //? 레거시



            ///// <summary>
            ///// 도어들의 MidRect를 통해, Rect를 얻는다<br/>
            ///// 가장 좌하단에있는 MidRect와 가장 우상단에있는 MidRect를 통해 Rect를 만든다<br/>
            ///// (도어들이 포함된 방 안전구역을 얻어올때 사용됨)<br/>
            ///// <b>사실상 방 안전구역을 가져오는 것과 같다</b>
            ///// </summary>
            ///// <param name="doorSelectMode">
            ///// 도어를 확인할때, 도어의 상태 고려<br/>
            ///// All: 도어를 무조건 가져옴<br/>
            ///// OnlyEnable: 활성화된 도어들만 고려됨<br/>
            ///// OnlyDisable: 비활성화된 도어들만 고려됨<br/>
            ///// </param>
            ///// <param name="useDoor_Down">하단 도어들이 포함될지 여부</param>
            ///// <param name="useDoor_Up">상단 도어들이 포함될지 여부</param>
            ///// <param name="useDoor_Left">좌측 도어들이 포함될지 여부</param>
            ///// <param name="useDoor_Right">우측 도어들이 포함될지 여부</param>
            ///// <param name="standardBottomLeft">기준이되는 좌하단 벡터</param>
            ///// <param name="standardTopRight">기존이되는 우상단 벡터</param>
            //[Obsolete]
            //public Rect Legacy_GetRect_ByDoorMidRects(ESelectActivesMode doorSelectMode, bool useDoor_Down, bool useDoor_Up, bool useDoor_Left, bool useDoor_Right, Vector2 standardBottomLeft, Vector2 standardTopRight)
            //{
            //    List<Rect> midDoorRectList; // 도어의 Mid Rect들을 얻어와 추가할 리스트


            //    // 불필요한 리스트 확장을 막기위해, 방향별 도어들의 총 개수들을 구한다
            //    // (Capacity를 미리 구하는 최적화)
            //    int listCapacity = 0;
            //    if (useDoor_Down) { listCapacity += GetDoorList(EDirection4.Down).Count; }
            //    if (useDoor_Up) { listCapacity += GetDoorList(EDirection4.Up).Count; }
            //    if (useDoor_Left) { listCapacity += GetDoorList(EDirection4.Left).Count; }
            //    if (useDoor_Right) { listCapacity += GetDoorList(EDirection4.Right).Count; }


            //    midDoorRectList = new List<Rect>(listCapacity); //구한 Capacity들로 리스트를 선언한다


            //    //받아온 방향별 도어 사용 여부에 따라, 각각 Rect들을 리스트에 더해준다
            //    if (useDoor_Down) { GetDoorRect_Mid(doorSelectMode, EDirection4.Down, midDoorRectList); }
            //    if (useDoor_Up) { GetDoorRect_Mid(doorSelectMode, EDirection4.Up, midDoorRectList); }
            //    if (useDoor_Left) { GetDoorRect_Mid(doorSelectMode, EDirection4.Left, midDoorRectList); }
            //    if (useDoor_Right) { GetDoorRect_Mid(doorSelectMode, EDirection4.Right, midDoorRectList); }


            //    // 리스트를 순회하며, 가장 작은 좌측하단과, 가장 큰 우측상단을 얻어온다
            //    for (int i = 0; i < midDoorRectList.Count; i++)
            //    {
            //        Rect midDoorRect = midDoorRectList[i];


            //        // bottomLeft를 가장 작은 값으로 업데이트
            //        standardBottomLeft = Vector2.Min(standardBottomLeft, midDoorRect.GetPosition(ECenterStandard.LowerLeft));


            //        // topRight를 가장 큰 값으로 업데이트
            //        standardTopRight = Vector2.Max(standardTopRight, midDoorRect.GetPosition(ECenterStandard.UpperRight));
            //    }


            //    // Rect를 만들어 반환한다
            //    return SU_TF_Rect.RectFromCorners(standardBottomLeft, standardTopRight);
            //}



            ///======================================================================================================================================================



            //? 도어 정보 얻기



            ///// <summary>
            ///// 해당 방향에 있는 도어가 존재하는지<br/>
            ///// (도어가 1개 이상인지)
            ///// </summary>
            //public bool IsVailed_DoorDir(EDirection4 doorDir)
            //{
            //    return GetDoorList(doorDir).Count != 0;
            //}



            ///// <summary>
            ///// 해당 방향에 있는 도어들이 사용 가능한 상태인지<br/>
            ///// (활성화된 도어가 1개 이상인지)
            ///// </summary>
            //public bool IsEnabled_DoorDir(EDirection4 doorDir)
            //{
            //    var doorList = GetDoorList(doorDir);

            //    if (doorList.Count == 0) { return false; }

            //    return doorList.Any(x => x.DoorEnable);
            //}



            ///======================================================================================================================================================



            ////? 도어 (단일) 얻기



            ///// <summary>
            ///// 도어 리스트의 First 도어 가져오기
            ///// </summary>
            //public RoomDoor GetDoor_First(EDirection4 doorDir)
            //{
            //    var doorList = GetDoorList(doorDir);

            //    if (doorList.Count == 0) { return null; }

            //    return doorList[0];
            //}



            ///// <summary>
            ///// 도어 리스트의 Last 도어 가져오기
            ///// </summary>
            //public RoomDoor GetDoor_Last(EDirection4 doorDir)
            //{
            //    var doorList = GetDoorList(doorDir);

            //    if (doorList.Count == 0) { return null; }

            //    return doorList[doorList.Count - 1];
            //}



            ///// <summary>
            ///// 도어 리스트에서 index로 가져오기
            ///// </summary>
            //public RoomDoor GetDoor_ByIndex(EDirection4 doorDir, int index)
            //{
            //    var doorList = GetDoorList(doorDir);

            //    if (doorList.Count <= index)
            //    {
            //        return null;
            //    }

            //    return doorList[index];
            //}



            ///// <summary>
            ///// 도어 리스트에서 연결되어있지 않는 도어를 찾아 반환하기
            ///// </summary>
            ///// <param name="findFirst">
            ///// 활성화시, 리스트의 처음부터 탐색시작<br/>
            ///// 비활성화시, 리스트의 끝에서부터 탐색 시작
            ///// </param>
            //public RoomDoor GetDoor_ByDir_FindNotConnect(EDirection4 doorDir, bool findFirst)
            //{
            //    var doorList = GetDoorList(doorDir);

            //    RoomDoor result;

            //    if (findFirst)
            //    {
            //        result = doorList.Find(x => { return x.DoorEnable && x.ConnectecDoor == null; });
            //    }
            //    else
            //    {
            //        result = doorList.FindLast(x => { return x.DoorEnable && x.ConnectecDoor == null; });
            //    }


            //    return result;
            //}



            ///// <summary>
            ///// 도어 리스트의 First 도어 가져오기
            ///// </summary>
            //public RoomDoor GetDoor_Condition(EDirection4 doorDir)
            //{
            //    var doorList = GetDoorList(doorDir);

            //    if (doorList.Count == 0) { return null; }

            //    return doorList[0];
            //}



            ///======================================================================================================================================================



            //? 도어 리스트 private 이벤트



            ///// <summary>
            ///// 도어 리스트에 추가한다 (+인덱스, 부모방 갱신 등등)
            ///// </summary>
            //private void AddDoor_ToList(RoomDoor door, in List<RoomDoor> doorList)
            //{
            //    //door.Index = doorList.Count;
            //    //door.ParentRoomObject = Main;

            //    door.InitializeRoomDoor(Main, null);
            //    doorList.Add(door);
            //}



            ///// <summary>
            ///// 도어 를 직접 받아와활성/비활성화 시킨다
            ///// </summary>
            //private void SetActive(RoomDoor door, bool enable)
            //{
            //    if (enable) { door.EnableDoor(); }
            //    else { door.DisableDoor(); }
            //}



            /////<summary>
            /////도어 리스트를 직접 받아와 모두 활성/비활성화 시킨다<br/>
            /////연결 정보도 초기화시킬수 있다
            /////</summary>
            //private bool SetActiveDoorList(List<RoomDoor> doorsList, bool enable, bool resetConnectInfo)
            //{
            //    foreach (var door in doorsList)
            //    {
            //        if (door == null) { continue; }
            //        door.ActiveDoor(enable);
            //        if (resetConnectInfo)
            //        {
            //            door.ConnectecDoor = null;
            //            //doorEdit.IsConnectedForHallway = false;
            //            door.SetDisable_HallwayCorrectionGrids(false, StageGenerator.GridManager.GridEvent_RemoveHallwayTags);
            //            door.SetDisable_HallwayPathGrids(false, StageGenerator.GridManager.GridEvent_RemoveHallwayTags);
            //            //door.ResetCurrentHallwayGrids();
            //        }
            //    }

            //    return true;
            //}



            /////<summary>
            /////도어 리스트를 직접 받아와 연결 정보를 초기화시킨다
            /////</summary>
            //private bool ResetConnectInfoDoors(List<RoomDoor> doorsList)
            //{
            //    foreach (var door in doorsList)
            //    {
            //        if (door == null) { continue; }
            //        door.ConnectecDoor = null;
            //        //doorEdit.IsConnectedForHallway = false;
            //        door.SetDisable_HallwayCorrectionGrids(false, StageGenerator.GridManager.GridEvent_RemoveHallwayTags);
            //        door.SetDisable_HallwayCorrectionGrids(false, StageGenerator.GridManager.GridEvent_RemoveHallwayTags);
            //        //door.ResetCurrentHallwayGrids();
            //    }

            //    return true;
            //}



            ///// <summary>
            ///// 도어 리스트 안에있는 도어 인덱스를 갱신한다 (보통 리스트 재정렬 이후에 사용)
            ///// </summary>
            //private void RefreshDoors_FromDoorList(List<RoomDoor> doorList)
            //{
            //    for (int i = 0; i < doorList.Count; i++)
            //    {
            //        var door = doorList[i];
            //        //door.Index = i;
            //    }
            //}



            ///======================================================================================================================================================



            //? 도어 리스트 관리



            /////<summary>
            /////도어를 해당 방향 도어 리스트에 추가한다
            /////</summary>
            /////<param name="order">추가한뒤, 재정렬한다</param>
            //public void AddDoor(RoomDoor door, bool order = true)
            //{
            //    switch (door.DoorDirection)
            //    {
            //        case EDirection4.Down: AddDoor_ToList(door, in DoorList_Down); break;
            //        case EDirection4.Up: AddDoor_ToList(door, in DoorList_Up); break;
            //        case EDirection4.Left: AddDoor_ToList(door, in DoorList_Left); break;
            //        case EDirection4.Right: AddDoor_ToList(door, in DoorList_Right); break;
            //    }

            //    if (order)
            //    {
            //        Order_DoorList(door.DoorDirection);
            //    }
            //}



            ///// <summary>도어 리스트를 초기화한다 (4방향 선택 / 4방향 전부)</summary>
            ///// <param name="doorDir">null을 받아오면, 4방향 모두 해당된다</param>
            //public void ClearDoor(EDirection4? doorDir)
            //{
            //    switch (doorDir)
            //    {
            //        case EDirection4.Down: ClearDoorList(DoorList_Down); break;

            //        case EDirection4.Up: ClearDoorList(DoorList_Up); break;

            //        case EDirection4.Left: ClearDoorList(DoorList_Left); break;

            //        case EDirection4.Right: ClearDoorList(DoorList_Right); break;

            //        case null:

            //        ClearDoorList(DoorList_Down);
            //        ClearDoorList(DoorList_Up);
            //        ClearDoorList(DoorList_Left);
            //        ClearDoorList(DoorList_Right);

            //        break;
            //    }
            //}



            ///// <summary>
            ///// 도어 리스트를 초기화 (초기화 전 모두 비활성화)
            ///// </summary>
            //public void ClearDoorList(List<RoomDoor> doorsList)
            //{
            //    if (doorsList.Count != 0)
            //    {
            //        SetActiveDoorList(doorsList, false, false);
            //    }

            //    doorsList.Clear();
            //}



            ///======================================================================================================================================================




            ///// <summary>
            ///// 도어리스트의 맨 첫부분을 활성화/비활성화 시키고 반환하기<br/>
            ///// 정순으로 탐색을 시작하여, 맨 처음 발견한 <paramref name="enable"/>와 반대되는 상태를 변경한뒤 반환한다
            ///// </summary>
            //public RoomDoor SetActive_FirstDifferenceOne(EDirection4 doorDir, bool enable)
            //{
            //    var doorList = GetDoorList(doorDir);
            //    int currentCount = doorList.Count;


            //    for (int i = 0; i < currentCount; i++)
            //    {
            //        var door = doorList[i];

            //        //! 이미 enable와 같은 상태라면 다음으로넘어가기
            //        if (door.DoorEnable == enable)
            //        {
            //            currentCount = Mathf.Clamp(currentCount + 1, 0, doorList.Count);
            //            continue;
            //        }

            //        door.ActiveDoor(enable);
            //        return door;
            //    }

            //    return null;
            //}



            ///// <summary>
            ///// 도어리스트의 마지막을 역순으로 활성화/비활성화 시키고 반환하기
            ///// </summary>
            ///// 정순으로 탐색을 시작하여, 맨 처음 발견한 <paramref name="enable"/>와 반대되는 상태를 변경한뒤 반환한다
            //public RoomDoor SetActive_LastDifferenceOne(EDirection4 doorDir, bool enable)
            //{
            //    var doorList = GetDoorList(doorDir);
            //    int activatedCount = 0; // 활성화된 문의 수를 추적하는 변수

            //    // 리스트를 역순으로 순회
            //    for (int i = doorList.Count - 1; i >= 0 && activatedCount < 1; i--)
            //    {
            //        var door = doorList[i];

            //        //! 이미 enable와 같은 상태라면 다음으로넘어가기
            //        if (door.DoorEnable == enable)
            //        {
            //            continue;
            //        }

            //        // 문을 활성화/비활성화
            //        door.ActiveDoor(enable);
            //        return door;
            //    }

            //    return null;
            //}



            /////<summary>
            /////받아온 방향에 있는 도어 리스트의 도어 연결 정보들을 초기화한다<br/>
            /////</summary>
            //public void ResetConnectInfo_All(EDirection4 doorDir)
            //{
            //    switch (doorDir)
            //    {
            //        case EDirection4.Down:

            //        ResetConnectInfoDoors(DoorList_Down);

            //        break;

            //        case EDirection4.Up:

            //        ResetConnectInfoDoors(DoorList_Up);

            //        break;

            //        case EDirection4.Left:

            //        ResetConnectInfoDoors(DoorList_Left);

            //        break;

            //        case EDirection4.Right:

            //        ResetConnectInfoDoors(DoorList_Right);

            //        break;
            //    }
            //}



            /////<summary>
            /////모든 방향에 있는 도어 리스트의 도어 연결 정보들을 초기화한다<br/>
            /////</summary>
            //public void ResetConnectInfo_All()
            //{
            //    ResetConnectInfo_All(EDirection4.Down);
            //    ResetConnectInfo_All(EDirection4.Up);
            //    ResetConnectInfo_All(EDirection4.Left);
            //    ResetConnectInfo_All(EDirection4.Right);
            //}



            //? 도어 이벤트 : 활성/비활성화, 연결정보 초기화



            /////<summary>
            /////받아온 방향에 있는 도어 리스트를 모두 활성/비활성화 시킨다<br/>
            /////연결 정보도 초기화시킬수 있다
            /////</summary>
            //public void SetActive_All(EDirection4 doorDir, bool enable, bool resetConnectInfo)
            //{
            //    switch (doorDir)
            //    {
            //        case EDirection4.Down: DoorListControlInternal(DoorList_Down, enable, resetConnectInfo); break;
            //        case EDirection4.Up: DoorListControlInternal(DoorList_Up, enable, resetConnectInfo); break;
            //        case EDirection4.Left: DoorListControlInternal(DoorList_Left, enable, resetConnectInfo); break;
            //        case EDirection4.Right: DoorListControlInternal(DoorList_Right, enable, resetConnectInfo); break;
            //    }
            //}






            /////<summary>
            /////모든 방향에 있는 도어 리스트를 모두 활성/비활성화 시킨다
            /////</summary>
            //public void SetActive_All(bool enable, bool resetConnectInfo)
            //{
            //    SetActive_All(EDirection4.Down, enable, resetConnectInfo);
            //    SetActive_All(EDirection4.Up, enable, resetConnectInfo);
            //    SetActive_All(EDirection4.Left, enable, resetConnectInfo);
            //    SetActive_All(EDirection4.Right, enable, resetConnectInfo);
            //}



            /////<summary>
            /////받아온 방향에 있는 도어 리스트 중에서 무작위 하나를 활성/비활성화 시킨다
            /////</summary>
            ///// <param name="disableAllBeforeApply">적용하기 전에, 모두 비활성화 할지 여부</param>
            //public void SetActive_Random(EDirection4 doorDir, bool enable, bool disableAllBeforeApply, CustomRandom random, out RoomDoor resultDoor)
            //{
            //    //? 초기화하고 시작할지 여부 (리스트내 전부 비활성화 시키기)
            //    if (disableAllBeforeApply) { SetActive_All(doorDir, false, false); }

            //    List<RoomDoor> doorList = GetDoorList(doorDir);

            //    int randomIndex = random.RangeExcluding(0, doorList.Count - 1, GetDoorList(doorDir).FindAllIndices(x => x.DoorEnable == enable)) ?? 0;

            //    resultDoor = doorList[randomIndex];

            //    resultDoor.ActiveDoor(enable);
            //}



            /////<summary>
            /////받아온 방향에 있는 도어 리스트 중에서 무작위 하나를 활성/비활성화 시킨다
            /////</summary>
            ///// <param name="disableAllBeforeApply">적용하기 전에, 모두 비활성화 할지 여부</param>
            //public void SetActive_Random(EDirection4 doorDir, bool enable, bool disableAllBeforeApply, CustomRandom random)
            //{
            //    SetActive_Random(doorDir, enable, disableAllBeforeApply, random, out var randomIndex);
            //}



            ///// <summary>
            ///// 도어리스트의 첫부분 부터 활성화/비활성화 시키기
            ///// </summary>
            ///// <param name="disableAllBeforeApply">적용하기 전에, 모두 비활성화 할지 여부</param>
            ///// <param name="continueOverlap">탐색 도중, 이미 <paramref name="enable"/>와 같은 대상이라면, continue 할지 여부</param>
            //public void SetActive_First(EDirection4 doorDir, int count, bool enable, bool disableAllBeforeApply, bool continueOverlap, Action<RoomDoor> loopEvent = null)
            //{
            //    if (count == 0) { return; }


            //    //? 초기화하고 시작할지 여부 (리스트내 전부 비활성화 시키기)
            //    if (disableAllBeforeApply) { SetActive_All(doorDir, false, false); }


            //    var doorList = GetDoorList(doorDir);
            //    int currentCount = Mathf.Min(count, doorList.Count);



            //    for (int i = 0; i < currentCount; i++)
            //    {
            //        var door = doorList[i];

            //        //! 중복시 스킵 기능이 ON + 이미 enable와 같은 상태라면 다음으로넘어가기
            //        if (continueOverlap && door.DoorEnable == enable)
            //        {
            //            currentCount = Mathf.Clamp(currentCount + 1, 0, doorList.Count);
            //            continue;
            //        }

            //        door.ActiveDoor(enable);
            //        loopEvent?.Invoke(door);
            //    }
            //}



            ///// <summary>
            ///// 도어리스트의 마지막부분 부터 역순으로 활성화/비활성화 시키기
            ///// </summary>
            ///// <param name="disableAllBeforeApply">적용하기 전에, 모두 비활성화 할지 여부</param>
            ///// <param name="continueOverlap">탐색 도중, 이미 <paramref name="enable"/>와 같은 대상이라면, continue 할지 여부</param>
            //public void SetActive_Last(EDirection4 doorDir, int count, bool enable, bool disableAllBeforeApply, bool continueOverlap, Action<RoomDoor> loopEvent = null)
            //{
            //    if (count == 0) { return; }

            //    //? 초기화하고 시작할지 여부 (리스트 내 전부 비활성화 시키기)
            //    if (disableAllBeforeApply) { SetActive_All(doorDir, false, false); }

            //    var doorList = GetDoorList(doorDir);
            //    int activatedCount = 0; // 활성화된 문의 수를 추적하는 변수

            //    // 리스트를 역순으로 순회
            //    for (int i = doorList.Count - 1; i >= 0 && activatedCount < count; i--)
            //    {
            //        var door = doorList[i];

            //        //! 중복시 스킵 기능이 ON + 이미 enable와 같은 상태라면 다음으로넘어가기
            //        if (continueOverlap && door.DoorEnable == enable)
            //        {
            //            continue;
            //        }

            //        // 문을 활성화/비활성화
            //        door.ActiveDoor(enable);
            //        loopEvent?.Invoke(door);

            //        activatedCount++; // 활성화된 문의 수 증가

            //        // 활성화해야 할 문의 개수에 도달했으면 루프를 중단
            //        if (activatedCount >= count) break;
            //    }
            //}



            ///======================================================================================================================================================



            //? 도어 리스트 정렬



            ///// <summary>
            ///// 도어 리스트를 정렬한다<br/>
            ///// <b>이웃 도어들까지 갱신된다!</b><br/>
            ///// 하단문: 좌측->우측<br/>
            ///// 상단문: 좌측->우측<br/>
            ///// 좌측문: 하단->상단<br/>
            ///// 우측문: 하단->상단<br/>
            ///// </summary>
            ///// <param name="refreshDoorIndex">정렬이 완료된뒤, 도어의 인덱스를 갱신히킨다</param>
            //private void Order_DoorList(EDirection4 doorDir, bool refreshDoorIndex = true)
            //{
            //    var doorList = GetDoorList(doorDir);
            //    var swizzle = Main.GridCompatible.CurrentSnapSetting.Swizzle;


            //    //! 250625 이웃도어 코드 폐기


            //    //? 이웃 도어 초기화
            //    foreach (var door in doorList)
            //    {
            //        //door.Neighbor_Door_Low = null;
            //        //door.Neighbor_Door_High = null;
            //    }


            //    switch (doorDir)
            //    {
            //        case EDirection4.Down:
            //        doorList.Sort((a, b) => a.DoorStartTransformPositionCurrent.SwizzlesVector(swizzle).x.CompareTo(SU_TF_Vector.SwizzlesVector(b.DoorStartTransformPositionCurrent, swizzle).x));
            //        break;

            //        case EDirection4.Up:
            //        doorList.Sort((a, b) => a.DoorStartTransformPositionCurrent.SwizzlesVector(swizzle).x.CompareTo(SU_TF_Vector.SwizzlesVector(b.DoorStartTransformPositionCurrent, swizzle).x));
            //        break;

            //        case EDirection4.Left:
            //        doorList.Sort((a, b) => a.DoorStartTransformPositionCurrent.SwizzlesVector(swizzle).y.CompareTo(SU_TF_Vector.SwizzlesVector(b.DoorStartTransformPositionCurrent, swizzle).y));
            //        break;

            //        case EDirection4.Right:
            //        doorList.Sort((a, b) => a.DoorStartTransformPositionCurrent.SwizzlesVector(swizzle).y.CompareTo(SU_TF_Vector.SwizzlesVector(b.DoorStartTransformPositionCurrent, swizzle).y));
            //        break;
            //    }


            //    //? 이웃 도어 등록
            //    for (int i = 0; i < doorList.Count; i++)
            //    {
            //        RoomDoor door = doorList[i];

            //        if (i > 0 && i < doorList.Count)
            //        {
            //            //door.Neighbor_Door_Low = doorList[i - 1];
            //        }

            //        if (i + 1 < doorList.Count)
            //        {
            //            //door.Neighbor_Door_High = doorList[i + 1];
            //        }
            //    }


            //    if (refreshDoorIndex) { RefreshDoors_FromDoorList(doorList); }
            //}



            ///// <summary>
            ///// 도어 리스트를 정렬한다 (4방향 전부)<br/>
            ///// 하단문: 좌측->우측<br/>
            ///// 상단문: 좌측->우측<br/>
            ///// 좌측문: 하단->상단<br/>
            ///// 우측문: 하단->상단<br/>
            ///// </summary>
            ///// <param name="refreshDoorIndex">정렬이 완료된뒤, 도어의 인덱스를 갱신히킨다</param>
            //private void Order_DoorListAll(bool refreshDoorIndex = true)
            //{
            //    Order_DoorList(EDirection4.Down, refreshDoorIndex);
            //    Order_DoorList(EDirection4.Up, refreshDoorIndex);
            //    Order_DoorList(EDirection4.Left, refreshDoorIndex);
            //    Order_DoorList(EDirection4.Right, refreshDoorIndex);
            //}



            ///======================================================================================================================================================



            //? 도어 이웃들의 간격 얻기



            /////<summary>
            /////도어 이웃들의 최소 간격, 최대 간격을 구하기 (방향별)
            ///// </summary>
            //public bool GetDoorsGaps(EDirection4 doorDireciton, out float minGap, out float maxGap)
            //{
            //    var doorList = GetDoorList(doorDireciton);


            //    minGap = int.MaxValue;
            //    maxGap = -int.MaxValue;


            //    //! 도어가 1개 초과가 아니라면, 이웃은 존재하지 않기때문에 실패
            //    if (doorList.Count <= 1) { return false; }


            //    foreach (var currentDoor in doorList)
            //    {
            //        var neighborLow = currentDoor.TryGet_NeighborGap(true, out var lowGap);
            //        var neighborHigh = currentDoor.TryGet_NeighborGap(false, out var highGap);

            //        if (neighborLow)
            //        {
            //            if (lowGap < minGap) { minGap = lowGap; }
            //            if (lowGap > maxGap) { maxGap = lowGap; }
            //        }

            //        if (neighborHigh)
            //        {
            //            if (highGap < minGap) { minGap = highGap; }
            //            if (highGap > maxGap) { maxGap = highGap; }
            //        }
            //    }

            //    return true;
            //}



            /////<summary>
            /////도어들의 최소 간격, 최대 간격을 구하기 (방향별)
            ///// </summary>
            //public bool GetDoorsGaps(out float minGap, out float maxGap)
            //{
            //    bool gap_Down = GetDoorsGaps(EDirection4.Down, out var minGap_Down, out var maxGap_Down);
            //    bool gap_Up = GetDoorsGaps(EDirection4.Up, out var minGap_Up, out var maxGap_Up);
            //    bool gap_Left = GetDoorsGaps(EDirection4.Left, out var minGap_Left, out var maxGap_Left);
            //    bool gap_Right = GetDoorsGaps(EDirection4.Right, out var minGap_Right, out var maxGap_Right);


            //    if (!gap_Down && !gap_Up && !gap_Left && !gap_Right)
            //    {
            //        minGap = int.MaxValue;
            //        maxGap = -int.MaxValue;
            //        return false;
            //    }


            //    minGap = Mathf.Min(minGap_Down, minGap_Up, minGap_Left, minGap_Right);
            //    maxGap = Mathf.Max(maxGap_Down, maxGap_Up, maxGap_Left, maxGap_Right);

            //    return true;
            //}

            //! 간격얻는거 일단비활성화, 유효성검사에 쓰이는데 개편해야될듯



            #endregion



            ///======================================================================================================================================================
        }
    }
}
