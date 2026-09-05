using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Pan.Util;
using System;
using System.Text;
using Cysharp.Threading.Tasks;
using System.Threading;
using Pan.GridCompatibles2;
using Pan.StageGenerators;
using Sirenix.Utilities;
using UnityEngine.Pool;



namespace Pan.StageGenerators
{
    public partial class StageGenerator
    {
        public partial class PlaceManger : BaseManager
        {
            ///======================================================================================================================================================



            ///<summary>
            ///방 이어주기 (물리적으로 X, 개념적으로 O)
            /// </summary>
            [Serializable]
            public class Generator2_LinkRooms
            {

                ///======================================================================================================================================================



                /// <summary>
                /// 인스턴스화된 방들을 가지고있는 공간 리스트를 받아와,<br/>
                /// 방향별 공간 연결 노드에 맞춰,<br/>
                /// 방향별 도어들을 각각 열결시켜준다
                /// </summary>
                public void Generate(StageGenerator main, IReadOnlyList<Space> spaceList)
                {
                    main.GenerateM.GeneratingCurrentSteps = GenerateManager.GenerateSteps.GenStep4_Places_Gen2_LinkRooms;

                    //. 커스텀 이벤트: 방 이어주기 이전
                    if (main.Setting.CustomEvent.ConnectRoom != null) { main.Setting.CustomEvent.ConnectRoom.Before_RoomsConnectToDoor(main, spaceList); }


                    //. 연결된 노드 개수를 내림차순으로 공간들을 순회하며, 공간들을 이어준다
                    foreach (var space in spaceList)
                    {
                        //! 방이 존재하지 않으면 continue
                        if (space.PlacedRoom == null) { continue; }

                        //? 도어를 개방하고 도어끼리 연결하기
                        ConnectRoomDoorToRoomDoor_bySpace(main, space);
                    }


                    //. 커스텀이벤트: 방이 이어진 이후
                    if (main.Setting.CustomEvent.ConnectRoom != null) { main.Setting.CustomEvent.ConnectRoom.After_RoomsConnectToDoor(main, spaceList); }


                    //. 그리드 이벤트
                    foreach (var space in spaceList)
                    {
                        //! 방이 존재하지 않으면 continue
                        if (space.PlacedRoom == null) { continue; }


                        //. 커스텀 이벤트: 도어 그리드 이벤트 이전
                        if (main.Setting.CustomEvent.ConnectRoom != null) { main.Setting.CustomEvent.ConnectRoom.Before_Door_GridEvent(main, space.PlacedRoom); }


                        //. 커스텀 이벤트: 도어 그리드 이벤트 스위치
                        if (main.Setting.CustomEvent.ConnectRoom == null || !main.Setting.CustomEvent.ConnectRoom.Switch_SummonHallway_ByGrid(main, space.PlacedRoom))
                        {
                            //? 도어 그리드 이벤트
                            GridEvent_RoomDoors(main, space.PlacedRoom);
                        }


                        //. 커스텀 이벤트: 도어 그리드 이벤트 이후
                        if (main.Setting.CustomEvent.ConnectRoom != null) { main.Setting.CustomEvent.ConnectRoom.After_Door_GridEvent(main, space.PlacedRoom); }
                    }


                    //. 커스텀 이벤트: 방 이어주기 이후, 도어 그리드 이벤트가 실행된 이후
                    if (main.Setting.CustomEvent.ConnectRoom != null) { main.Setting.CustomEvent.ConnectRoom.AfterPost_RoomsConnectToDoor_AfterGridEvent(main, spaceList); }
                }



                /// <summary>
                /// [비동기]인스턴스화된 방들을 가지고있는 공간 리스트를 받아와,<br/>
                /// 방향별 공간 연결 노드에 맞춰,<br/>
                /// 방향별 도어들을 각각 열결시켜준다
                /// </summary>
                public async UniTask GenerateAsync(StageGenerator main, IReadOnlyList<Space> spaceList)
                {
                    main.GenerateM.GeneratingCurrentSteps = GenerateManager.GenerateSteps.GenStep4_Places_Gen2_LinkRooms;

                    //. 커스텀 이벤트: 방 이어주기 이전
                    await (main.Setting.CustomEvent.ConnectRoom != null ? main.Setting.CustomEvent.ConnectRoom.Before_RoomsConnectToDoorAsync(main, spaceList) : UniTask.CompletedTask);


                    //. 연결된 노드 개수를 내림차순으로 공간들을 순회하며, 공간들을 이어준다
                    foreach (var space in spaceList)
                    {
                        //! 방이 존재하지 않으면 continue
                        if (space.PlacedRoom == null) { continue; }

                        //? 도어를 개방하고 도어끼리 연결하기
                        await ConnectRoomDoorToRoomDoor_bySpaceAsync(main, space);
                    }


                    //. 커스텀이벤트: 방이 이어진 이후
                    await (main.Setting.CustomEvent.ConnectRoom != null ? main.Setting.CustomEvent.ConnectRoom.After_RoomsConnectToDoorAsync(main, spaceList) : UniTask.CompletedTask);


                    //. 그리드 이벤트
                    foreach (var space in spaceList)
                    {
                        //! 방이 존재하지 않으면 continue
                        if (space.PlacedRoom == null) { continue; }


                        //. 커스텀 이벤트: 도어 그리드 이벤트 이전
                        await (main.Setting.CustomEvent.ConnectRoom != null ? main.Setting.CustomEvent.ConnectRoom.Before_Door_GridEventAsync(main, space.PlacedRoom) : UniTask.CompletedTask);


                        //. 커스텀 이벤트: 도어 그리드 이벤트 스위치
                        if (main.Setting.CustomEvent.ConnectRoom == null || !await main.Setting.CustomEvent.ConnectRoom.Switch_SummonHallway_ByGridAsync(main, space.PlacedRoom))
                        {
                            //? 도어 그리드 이벤트
                            GridEvent_RoomDoors(main, space.PlacedRoom);
                        }


                        //. 커스텀 이벤트: 도어 그리드 이벤트 이후
                        await (main.Setting.CustomEvent.ConnectRoom != null ? main.Setting.CustomEvent.ConnectRoom.After_Door_GridEventAsync(main, space.PlacedRoom) : UniTask.CompletedTask);
                    }


                    //. 커스텀 이벤트: 방 이어주기 이후, 도어 그리드 이벤트가 실행된 이후
                    await (main.Setting.CustomEvent.ConnectRoom != null ? main.Setting.CustomEvent.ConnectRoom.AfterPost_RoomsConnectToDoor_AfterGridEventAsync(main, spaceList) : UniTask.CompletedTask);
                }



                ///======================================================================================================================================================



                //? 공간의 정보에 맞춰 도어 활성화, 연결



                /// <summary>인스턴스화된 방을 가지고있는 공간을 받아와,<br/>
                /// 방향별 공간 연결 노드에 맞춰, 방향별 도어들의 "조건에 맞게" 각각 연결한다<br/>
                /// <b><i>[하, 상, 좌, 우 모든방향 실행]</i></b>
                /// </summary>
                private bool ConnectRoomDoorToRoomDoor_bySpace(StageGenerator main, Space space)
                {
                    //! 공간과 연결되어있는 공간 자체가 없거나, 공간 안에 방이 없다면 실패, return
                    if (space.TotalConnectingSpacesCount == 0 || space.PlacedRoom == null) { return false; }


                    ConnectRoomDoorToRoomDoor_bySpace(main, space, space.PlacedRoom, EDirection4.Down);
                    ConnectRoomDoorToRoomDoor_bySpace(main, space, space.PlacedRoom, EDirection4.Up);
                    ConnectRoomDoorToRoomDoor_bySpace(main, space, space.PlacedRoom, EDirection4.Left);
                    ConnectRoomDoorToRoomDoor_bySpace(main, space, space.PlacedRoom, EDirection4.Right);


                    return true;
                }



                /// <summary>[비동기] 인스턴스화된 방을 가지고있는 공간을 받아와,<br/>
                /// 방향별 공간 연결 노드에 맞춰, 방향별 도어들의 "조건에 맞게" 각각 연결한다<br/>
                /// <b><i>[하, 상, 좌, 우 모든방향 실행]</i></b>
                /// </summary>
                private async UniTask<bool> ConnectRoomDoorToRoomDoor_bySpaceAsync(StageGenerator main, Space space)
                {
                    //! 공간과 연결되어있는 공간 자체가 없거나, 공간 안에 방이 없다면 실패, return
                    if (space.TotalConnectingSpacesCount == 0 || space.PlacedRoom == null) { return false; }


                    await ConnectRoomDoorToRoomDoor_bySpaceAsync(main, space, space.PlacedRoom, EDirection4.Down);
                    await ConnectRoomDoorToRoomDoor_bySpaceAsync(main, space, space.PlacedRoom, EDirection4.Up);
                    await ConnectRoomDoorToRoomDoor_bySpaceAsync(main, space, space.PlacedRoom, EDirection4.Left);
                    await ConnectRoomDoorToRoomDoor_bySpaceAsync(main, space, space.PlacedRoom, EDirection4.Right);


                    return true;
                }



                /// <summary>인스턴스화된 방을 가지고있는 공간을 받아와,<br/>
                /// 방향별 공간 연결 노드에 맞춰, 방향별 도어들의 최단거리를 구해 각각 연결한다</summary>
                private void ConnectRoomDoorToRoomDoor_bySpace(StageGenerator main, Space space, RoomObject roomObject, EDirection4 dir)
                {
                    if (TryConnectRoomDoorToRoomDoor_bySpace(space, roomObject, dir, out var aDoor, out var bDoor))
                    {
                        //. 커스텀이벤트: 방 연결 이후
                        main.Setting.CustomEvent.ConnectRoom?.After_RoomConnectToDoor(main, roomObject, aDoor, bDoor.ParentRoomObject, bDoor);
                    }
                }



                /// <summary>[비동기] 인스턴스화된 방을 가지고있는 공간을 받아와,<br/>
                /// 방향별 공간 연결 노드에 맞춰, 방향별 도어들의 최단거리를 구해 각각 연결한다</summary>
                private async UniTask ConnectRoomDoorToRoomDoor_bySpaceAsync(StageGenerator main, Space space, RoomObject roomObject, EDirection4 dir)
                {
                    if (TryConnectRoomDoorToRoomDoor_bySpace(space, roomObject, dir, out var aDoor, out var bDoor))
                    {
                        await (main.Setting.CustomEvent.ConnectRoom?.After_RoomConnectToDoorAsync(main, roomObject, aDoor, bDoor.ParentRoomObject, bDoor) ?? UniTask.CompletedTask);
                    }
                }



                private bool TryConnectRoomDoorToRoomDoor_bySpace(
                    Space space,
                    RoomObject roomObject,
                    EDirection4 dir,
                    out RoomObject.Door aDoor,
                    out RoomObject.Door bDoor)
                {
                    aDoor = null;
                    bDoor = null;

                    var connectingSpaces = space.GetConnectingSpaces(dir);
                    if (connectingSpaces.Count == 0) { return false; }
                    if (roomObject.RoomDoorM.GetEnabledDoorCount(dir) != 0) { return false; }

                    List<RoomObject.Door> roomDoors = ListPool<RoomObject.Door>.Get();

                    try
                    {
                        CollectAvailableDoors(
                            roomObject.RoomDoorM.GetDoorList(dir),
                            roomDoors);

                        if (roomDoors.Count == 0) { return false; }

                        var axis = (dir == EDirection4.Left || dir == EDirection4.Right)
                            ? DoorSpineConnector.SplitAxis.Vertical
                            : DoorSpineConnector.SplitAxis.Horizontal;

                        foreach (var connectingSpace in connectingSpaces)
                        {
                            if (connectingSpace.PlacedRoom == null) { continue; }

                            List<RoomObject.Door> targetDoors = ListPool<RoomObject.Door>.Get();

                            try
                            {
                                CollectAvailableDoors(
                                    connectingSpace.PlacedRoom.RoomDoorM.GetDoorList(dir.GetOpposite()),
                                    targetDoors);

                                if (targetDoors.Count == 0) { continue; }

                                //. 기존 안정 정렬의 첫 항목과 동일하게, 최소 투영값의 첫 도어를 한 번의 순회로 선택한다
                                aDoor = DoorSpineConnector.SelectFirstDoorByProjection(axis, roomDoors);
                                bDoor = DoorSpineConnector.SelectFirstDoorByProjection(axis, targetDoors);

                                aDoor.EnableDoor();
                                bDoor.EnableDoor();
                                aDoor.ConnectecDoor = bDoor;
                                bDoor.ConnectecDoor = aDoor;

                                return true;
                            }
                            finally
                            {
                                targetDoors.Clear();
                                ListPool<RoomObject.Door>.Release(targetDoors);
                            }
                        }
                    }
                    finally
                    {
                        roomDoors.Clear();
                        ListPool<RoomObject.Door>.Release(roomDoors);
                    }

                    return false;
                }



                private static void CollectAvailableDoors(
                    IReadOnlyList<RoomObject.Door> source,
                    List<RoomObject.Door> destination)
                {
                    destination.Clear();

                    for (int i = 0; i < source.Count; i++)
                    {
                        var door = source[i];
                        if (door == null || door.ConnectecDoor != null) { continue; }

                        destination.Add(door);
                    }
                }



                #region Legacy

                ///// <summary>인스턴스화된 방을 가지고있는 공간을 받아와,<br/>
                ///// 방향별 공간 연결 노드에 맞춰, 방향별 도어들의 최단거리를 구해 각각 연결한다</summary>
                //private void Legacy_ConnectRoomDoorToRoomDoor_bySpace(StageGenerator main, Space space, RoomObject roomObject, EDirection4 dir)
                //{
                //    //. 공간과 [방향]에 연결되어있는 공간 리스트를 가져온다
                //    var connectingSpaces = space.GetConnectingSpaces(dir);


                //    //! 이 방향과 연결되어있는 공간이 없다면 (Count=0) 실패
                //    if (connectingSpaces.Count == 0) { return; }


                //    //! 방과 [방향]에 연결된 다른 방이 이미 있을경우 실패
                //    if (roomObject.RoomDoorM.GetEnabledDoorCount(dir) != 0) { return; }
                //    //. 방의 [방향] 도어에 활성화되어있는 도어의 개수가 있는지 확인하고, 있다면 실패
                //    //. 이 로직이 없다면, 2개 이상의 노드를 가지고 있는 방들이 마주보고 있을때,
                //    //. 2개의 노드가 모두 이어져버린다. (ㅁ-ㅁ 이 되어야하는데 ㅁ=ㅁ이 되어버림)
                //    //. 공간 노드에 맞춰 생성되는 복도는, 방의 문이 많아도 그중 하나만 이어주면 되게 때문


                //    //. 방과 [방향]에 연결된 도어 리스트를 가져온다
                //    var room_DoorList = roomObject.RoomDoorM.GetDoorList(dir);


                //    //x . 현재 방의 [방향]의 도어들중 가장 길이가 긴 길이를 구한다 (정렬에 사용?)
                //    //x roomObject.RoomDoorM.TryGetMaxFullDoorTotalDistance(dir, out var doorTotalDistance);


                //    //. 공간 리스트들을 순회한다 (공간과 [방향]에 연결되어있는 공간들)
                //    foreach (var connectingSpace in connectingSpaces)
                //    {
                //        //! 해당 공간안에 방이 없다면 실패, continue
                //        if (connectingSpace.PlacedRoom == null) { continue; }


                //        //? 각 방들의 서로 맞대고있는 도어 리스트들 중에, 아직 연결이 안된 도어들 중에서,
                //        //. 가장 가까운, 서로 연결할 도어 한쌍을 얻는다
                //        //!     얻어오기에 실패하면, continue
                //        if (!SU_TF_Vector.TryFindClosestPair(room_DoorList, connectingSpace.PlacedRoom.RoomDoorM.GetDoorList(dir.GetOpposite()),
                //            x => x.DoorEndPosition,
                //            x => x.DoorEndPosition,
                //            (doorA, doorB) =>
                //            {
                //                return (doorA != null && doorA.ConnectecDoor == null && doorB != null && doorB.ConnectecDoor == null);
                //            }, float.PositiveInfinity, out var door, out var connectingDoor, out var doorDistance)) { continue; }



                //        #region Legacy, 폐기해야할 정렬된 도어 리스트 순으로 이어주던 코드
                //        //? 각 방들의 서로 맞대고있는 도어 리스트들을 사용하여,
                //        // 서로 연결할 도어 한쌍을 얻는다
                //        //! 얻어오기에 실패하면, continue
                //        //if (!GetDoors_BetweenRooms(room_DoorList, connectingSpace.PlacedRoom.RoomDoorM.GetDoorList(dir.GetOpposite()), out var door, out var connectingDoor)) { continue; }  
                //        #endregion


                //        //? 최단거리 도어 to 도어 연결 성공!


                //        //. 양쪽 도어를 모두 "활성화"해준다
                //        door.EnableDoor();
                //        connectingDoor.EnableDoor();


                //        //. 양쪽 문을 연결한다 (서로 참조만 할뿐, 물리적으로 연결되지는 않음)
                //        door.ConnectecDoor = connectingDoor;
                //        connectingDoor.ConnectecDoor = door;


                //        //. 커스텀 이벤트: 방 연결 이후 실행
                //        if (main.Setting.CustomEvent.ConnectRoom != null) { main.Setting.CustomEvent.ConnectRoom.After_RoomConnectToDoor(main, roomObject, door, connectingDoor.ParentRoomObject, connectingDoor); }
                //    }
                //}

                ///// <summary>인스턴스화된 방을 가지고있는 공간을 받아와,<br/>
                ///// 방향별 공간 연결 노드에 맞춰, 방향별 도어들의 최단거리를 구해 각각 연결한다</summary>
                //private async UniTask Legacy_ConnectRoomDoorToRoomDoor_bySpaceAsync(StageGenerator main, Space space, RoomObject roomObject, EDirection4 dir)
                //{
                //    //. 공간과 [방향]에 연결되어있는 공간 리스트를 가져온다
                //    var connectingSpaces = space.GetConnectingSpaces(dir);


                //    //! 이 방향과 연결되어있는 공간이 없다면 (Count=0) 실패
                //    if (connectingSpaces.Count == 0) { return; }


                //    //! 방과 [방향]에 연결된 다른 방이 이미 있을경우 실패
                //    if (roomObject.RoomDoorM.GetEnabledDoorCount(dir) != 0) { return; }
                //    //. 방의 [방향] 도어에 활성화되어있는 도어의 개수가 있는지 확인하고, 있다면 실패
                //    //. 이 로직이 없다면, 2개 이상의 노드를 가지고 있는 방들이 마주보고 있을때,
                //    //. 2개의 노드가 모두 이어져버린다. (ㅁ-ㅁ 이 되어야하는데 ㅁ=ㅁ이 되어버림)
                //    //. 공간 노드에 맞춰 생성되는 복도는, 방의 문이 많아도 그중 하나만 이어주면 되게 때문


                //    //. 방과 [방향]에 연결된 도어 리스트를 가져온다
                //    var room_DoorList = roomObject.RoomDoorM.GetDoorList(dir);


                //    //. 공간 리스트들을 순회한다 (공간과 [방향]에 연결되어있는 공간들)
                //    foreach (var connectingSpace in connectingSpaces)
                //    {
                //        //! 해당 공간안에 방이 없다면 실패, continue
                //        if (connectingSpace.PlacedRoom == null) { continue; }


                //        //? 각 방들의 서로 맞대고있는 도어 리스트들 중에, 아직 연결이 안된 도어들 중에서,
                //        //. 가장 가까운, 서로 연결할 도어 한쌍을 얻는다
                //        //!     얻어오기에 실패하면, continue
                //        if (!SU_TF_Vector.TryFindClosestPair(room_DoorList, connectingSpace.PlacedRoom.RoomDoorM.GetDoorList(dir.GetOpposite()),
                //            x => x.DoorEndPosition,
                //            x => x.DoorEndPosition,
                //            (doorA, doorB) =>
                //            {
                //                return (doorA != null && doorA.ConnectecDoor == null && doorB != null && doorB.ConnectecDoor == null);
                //            }, float.PositiveInfinity, out var door, out var connectingDoor, out var doorDistance)) { continue; }


                //        #region Legacy, 폐기해야할 정렬된 도어 리스트 순으로 이어주던 코드
                //        //? 각 방들의 서로 맞대고있는 도어 리스트들을 사용하여,
                //        // 서로 연결할 도어 한쌍을 얻는다
                //        //! 얻어오기에 실패하면, continue
                //        //if (!GetDoors_BetweenRooms(room_DoorList, connectingSpace.PlacedRoom.RoomDoorM.GetDoorList(dir.GetOpposite()), out var door, out var connectingDoor)) { continue; }  
                //        #endregion


                //        //? 최단거리 도어 to 도어 연결 성공!


                //        //. 양쪽 도어를 모두 "활성화"해준다
                //        door.EnableDoor();
                //        connectingDoor.EnableDoor();


                //        //. 양쪽 문을 연결한다 (서로 참조만 할뿐, 물리적으로 연결되지는 않음)
                //        door.ConnectecDoor = connectingDoor;
                //        connectingDoor.ConnectecDoor = door;


                //        //. 커스텀 이벤트: 방 연결 이후 실행
                //        await (main.Setting.CustomEvent.ConnectRoom != null ? main.Setting.CustomEvent.ConnectRoom.After_RoomConnectToDoorAsync(main, roomObject, door, connectingDoor.ParentRoomObject, connectingDoor) : UniTask.CompletedTask);
                //    }
                //}

                #endregion



                ///======================================================================================================================================================



                //? (Legacy) 두 방의 서로 맞대는 방향의 도어들 중에서, 어떤 도어를 가져올지 정하기



                #region Legacy

                ///// <summary>
                ///// 연결된 두 방에, 서로 맞대고 있는 방향들로, 서로 연결할 문들을 각각 반환한다<br/>
                ///// <i>(별다른 조건 없이, foreach로 순회하면서 유효한 방을 우선으로 선택된다)</i>
                ///// <i>(도어 리스트가 정렬되어있다는것을 전제로 실행됨)</i>
                ///// </summary>
                ///// <param name="current_RoomDoorList">기존 방의 (방향)에 있는 도어 리스트</param>
                ///// <param name="target_RoomDoorList">기존 방과 연결되어있는 방의 (역방향)을 바라보는 도어 리스트</param>
                ///// <param name="currentDoor">선택된 현재 방의 도어를 반환</param>
                ///// <param name="connectingDoor">선택된 대상 방의 도어를 반환</param>
                ///// <returns>가장 가까운 두 문을 찾으면 <c>true</c>, 그렇지 않으면 <c>false</c>를 반환</returns>
                //private bool GetDoors_BetweenRooms(IEnumerable<RoomObject.Door> current_RoomDoorList, IEnumerable<RoomObject.Door> target_RoomDoorList, out RoomObject.Door currentDoor, out RoomObject.Door connectingDoor)
                //{
                //    currentDoor = null;
                //    connectingDoor = null;


                //    //. 기존방의 도어 리스트와 연결된방의 도어리스트들을 2중리스트로 모두 비교하여,
                //    //. 가능한 도어들 중에서 가장 가까운 도어의 쌍을 얻어온다


                //    bool breaker = false;

                //    //. 현재 방의 문 목록을 순회합니다.
                //    foreach (var currentRoomDoor in current_RoomDoorList)
                //    {
                //        // 문이 유효한지 확인합니다.
                //        if (!RoomDoorIsValid(currentRoomDoor)) { continue; }

                //        // 연결할 방의 문 목록을 순회합니다.
                //        foreach (var connectingRoomDoor in target_RoomDoorList)
                //        {
                //            // 문이 유효한지 확인합니다.
                //            if (!RoomDoorIsValid(connectingRoomDoor)) { continue; }


                //            currentDoor = currentRoomDoor;
                //            connectingDoor = connectingRoomDoor;
                //            breaker = true;
                //            break;
                //        }

                //        if (breaker) { break; }
                //    }


                //    //. 유효한 문을 찾았는지 여부를 반환합니다.
                //    if (currentDoor == null || connectingDoor == null) { return false; }


                //    return true;
                //}



                ///// <summary>
                ///// 연결된 두 방에, 서로 맞대고 있는 방향들로, 서로 연결할 문들을 각각 반환한다<br/>
                ///// <i>(서로 가장 가까운 도어들 끼리 연결된다)</i>
                ///// <i>(도어 리스트가 정렬되어있다는것을 전제로 실행됨)</i>
                ///// </summary>
                ///// <param name="current_RoomDoorList">기존 방의 (방향)에 있는 도어 리스트</param>
                ///// <param name="target_RoomDoorList">기존 방과 연결되어있는 방의 (역방향)을 바라보는 도어 리스트</param>
                ///// <param name="currentDoor">선택된 현재 방의 도어를 반환</param>
                ///// <param name="connectingDoor">선택된 대상 방의 도어를 반환</param>
                ///// <returns>가장 가까운 두 문을 찾으면 <c>true</c>, 그렇지 않으면 <c>false</c>를 반환</returns>
                //[Obsolete]
                //private bool GetDoors_BetweenRooms_Shortest(IEnumerable<RoomObject.Door> current_RoomDoorList, IEnumerable<RoomObject.Door> target_RoomDoorList, out RoomObject.Door currentDoor, out RoomObject.Door connectingDoor)
                //{
                //    float minDistance = Mathf.Infinity;
                //    currentDoor = null;
                //    connectingDoor = null;


                //    //! 기존방의 도어 리스트와 연결된방의 도어리스트들을 2중리스트로 모두 비교하여,
                //    //? 가능한 도어들 중에서 가장 가까운 도어의 쌍을 얻어온다


                //    // 현재 방의 문 목록을 순회합니다.
                //    foreach (var currentRoomDoor in current_RoomDoorList)
                //    {
                //        // 문이 유효한지 확인합니다.
                //        if (!RoomDoorIsValid(currentRoomDoor)) { continue; }

                //        // 연결할 방의 문 목록을 순회합니다.
                //        foreach (var connectingRoomDoor in target_RoomDoorList)
                //        {
                //            // 문이 유효한지 확인합니다.
                //            if (!RoomDoorIsValid(connectingRoomDoor)) { continue; }

                //            // 두 문 사이의 제곱 거리를 계산합니다.
                //            float distance = currentRoomDoor.DoorStartTransformPositionCurrent.SqrDistance(connectingRoomDoor.DoorStartTransformPositionCurrent);

                //            // 현재 가장 짧은 거리보다 짧으면, 최소 거리를 갱신하고 해당 문을 기록합니다.
                //            if (distance < minDistance)
                //            {
                //                minDistance = distance;
                //                currentDoor = currentRoomDoor;
                //                connectingDoor = connectingRoomDoor;
                //            }
                //        }
                //    }


                //    // 유효한 문을 찾았는지 여부를 반환합니다.
                //    if (currentDoor == null || connectingDoor == null) { return false; }


                //    return true;
                //}
                ////! 기존에는 서로 제일 가까이있는 도어들끼리 우선으로 연결하기위해, 이 메서드를 사용했으나 모종의 이유로, 취소 (아마 복도 충돌로 추정), / 생각해보니 방 인접 연산이 되기 전이기에 연결하는 상황에서 거리순으로 하면 정확하지 않아서 인듯?



                ///// <summary>
                ///// 주어진 문이 유효한지 확인합니다.
                ///// </summary>
                ///// <param name="roomDoor">확인할 문입니다.</param>
                ///// <returns>문이 유효하면 <c>true</c>, 그렇지 않으면 <c>false</c>를 반환합니다.</returns>
                //private bool RoomDoorIsValid(RoomObject.Door roomDoor)
                //{
                //    // 문이 null이 아니고, 연결된 문이 없는 경우 유효합니다.
                //    return roomDoor != null && roomDoor.ConnectecDoor == null;
                //} 

                #endregion



                ///======================================================================================================================================================



                //? 초기화



                ///<summary>
                /// 재생성을 위한, 초기화<br/>
                /// 모든 도어들을 비활성화하고, 연결 정보들을 초기화한다
                /// </summary>
                public void ChoronoBreak_ByHallways_ResetAllDoors(StageGenerator main, IReadOnlyList<Space> spaceList)
                {
                    for (int i = 0; i < spaceList.Count; i++)
                    {
                        Space space = spaceList[i];
                        if (space.PlacedRoom == null) { continue; }


                        //space.CurrentRoomObject.RoomDoorM.SetActive_All(false, true);
                        space.PlacedRoom.RoomDoorM.SetDisableAndRemoveConnectedDoorInfo_AllDoors();
                        //. 도어들을 모두 비활성화 하고,
                        //. 연결 정보들을 초기화시킨다
                    }
                }



                ///======================================================================================================================================================



                //? 방 도어 그리드 이벤트



                ///<summary>
                /// 방 도어들의 그리드 이벤트를 실행한다
                /// </summary>
                public static void GridEvent_RoomDoors(StageGenerator main, RoomObject roomObject)
                {
                    var stageOrigin = main.TransformM.StageCenterPositionTransform;


                    //? 방 도어들에게 <Door_Start>, <Door_End>, <Door> 그리드 이벤트 실행 (보정 거리에 있는 도어 X)
                    GridEvent_RoomDoor(main, stageOrigin, roomObject.RoomDoorM.GetDoorList(EDirection4.Down));
                    GridEvent_RoomDoor(main, stageOrigin, roomObject.RoomDoorM.GetDoorList(EDirection4.Up));
                    GridEvent_RoomDoor(main, stageOrigin, roomObject.RoomDoorM.GetDoorList(EDirection4.Left));
                    GridEvent_RoomDoor(main, stageOrigin, roomObject.RoomDoorM.GetDoorList(EDirection4.Right));
                }



                ///<summary>
                /// 각 방향의 도어들에게 그리드 이벤트를 실행한다
                /// </summary>
                public static void GridEvent_RoomDoor(StageGenerator main, Vector3 origin, List<RoomObject.Door> roomDoorList)
                {
                    //! 복도 이어주는거 만들어보고, 복도 인접 보고, 정리 마무리하기
                    //! 현재 도어를 더 확장해서 영역 차지시키는게 

                    foreach (var roomDoor in roomDoorList)
                    {
                        //! 도어가 활성화 되어 있지 않다면 continue
                        if (!roomDoor.DoorEnable) { continue; }


                        //. 도어의 Start 그리드 좌표 얻기
                        roomDoor.CenterGridPosition_Start(out bool isStartGridDoubleCenter, out var startCenter1, out var startCenter2);


                        ////? 그리드 좌표 보정 적용
                        //startCenter1 -= main.Setting.SnapSetting.GridPositionCorrection;
                        //startCenter2 -= main.Setting.SnapSetting.GridPositionCorrection;


                        //? Start의 중심점의 개수에 맞춰,  도어 시작점(들)에 <Door_Start> 그리드 이벤트를 실행한다
                        if (isStartGridDoubleCenter) { main.GridM.GridsEvent_Vectors(true, main.Setting.StageGrid.GridTagSetting_RoomDoorStart.EnableGrid, null, startCenter1, startCenter2); }
                        else { main.GridM.GridsEvent_Vectors(true, main.Setting.StageGrid.GridTagSetting_RoomDoorStart.EnableGrid, null, startCenter1); }


                        //. 도어의 End 그리드 좌표 얻기
                        roomDoor.CenterGridPosition_End(out bool isEndGridDoubleCenter, out var endCenter1, out var endCenter2);


                        ////? 그리드 좌표 보정 적용
                        //endCenter1 -= main.Setting.SnapSetting.GridPositionCorrection;
                        //endCenter2 -= main.Setting.SnapSetting.GridPositionCorrection;


                        //? End의 중심점의 개수에 맞춰,  도어 끝점(들)에  <Door_End> 그리드 이벤트를 실행한다
                        if (isEndGridDoubleCenter) { main.GridM.GridsEvent_Vectors(true, main.Setting.StageGrid.GridTagSetting_RoomDoorEnd.EnableGrid, null, endCenter1, endCenter2); }
                        else { main.GridM.GridsEvent_Vectors(true, main.Setting.StageGrid.GridTagSetting_RoomDoorEnd.EnableGrid, null, endCenter1); }


                        var doorSize = roomDoor.SingleDoorSize;
                        var doorStartRect = roomDoor.DoorStartRect;
                        var doorEndRect = roomDoor.DoorEndRect;
                        var doorStartEndRect = SU_TF_Rect.RectFromCorners(new Vector2(Mathf.Min(doorStartRect.xMin, doorEndRect.xMin), Mathf.Min(doorStartRect.yMin, doorEndRect.yMin)), new Vector2(Mathf.Max(doorStartRect.xMax, doorEndRect.xMax), Mathf.Max(doorStartRect.yMax, doorEndRect.yMax)));

                        //var fullDoorRect_E = roomDoor.FullDoorRect;
                        var fullDoorRect_E = roomDoor.FullDoorRectExpand(main.Setting);


                        //? Rect들에게, 생성 지점만큼 좌표 이동
                        //var instancePoint = (Vector2)main.TransformM.StageInstancePointCurrentCache;
                        var instancePoint = main.TransformM.GetStageInstancePoint_GridPositionCorrection(true);
                        doorStartRect.position -= instancePoint;
                        doorEndRect.position -= instancePoint;
                        doorStartEndRect.position -= instancePoint;
                        fullDoorRect_E.position -= instancePoint;


                        //? 도어의 Start, End에의 양 끝 (Min,Max)에 그리드 이벤트 를 위한 리스트
                        var doorStartEndBothEnds_Start = ListPool<Grid>.Get(); //? List 풀링
                        var doorStartEndBothEnds_End = ListPool<Grid>.Get(); //? List 풀링


                        //. 필요 시 용량 선할당 (EnsureCapacity 대신 Capacity 직접 설정)
                        if (doorStartEndBothEnds_Start.Capacity < roomDoor.DoorWidth) { doorStartEndBothEnds_Start.Capacity = roomDoor.DoorWidth; } //. Start 리스트 용량 보정
                        if (doorStartEndBothEnds_End.Capacity < roomDoor.DoorWidth) { doorStartEndBothEnds_End.Capacity = roomDoor.DoorWidth; } //. End 리스트 용량 보정

                        try
                        {
                            //? 도어의 Start, End에 도어의 크기 만큼 <EX_Door> 그리드 이벤트를 실행한다
                            main.gridM.GridsEvent_Rect(true, doorStartRect, main.Setting.StageGrid.GridTagSetting_RoomDoor.EnableGrid, doorStartEndBothEnds_Start);
                            main.gridM.GridsEvent_Rect(true, doorEndRect, main.Setting.StageGrid.GridTagSetting_RoomDoor.EnableGrid, doorStartEndBothEnds_End);
                            main.gridM.GridsEvent_Rect(true, doorStartEndRect, main.Setting.StageGrid.GridTagSetting_RoomDoorAreaStartEnd.EnableGrid);

                            //? 도어의 Start, End에의 양 끝 (Min,Max)에 그리드 이벤트
                            if (doorStartEndBothEnds_Start.Count != 0)
                            {
                                int minGridPosX = int.MaxValue; //. 최소 X
                                int minGridPosY = int.MaxValue; //. 최소 Y
                                int maxGridPosX = -int.MaxValue; //. 최대 X
                                int maxGridPosY = -int.MaxValue; //. 최대 Y

                                foreach (var grid in doorStartEndBothEnds_Start)
                                {
                                    if (minGridPosX > grid.GridPositionFixed.x) { minGridPosX = grid.GridPositionFixed.x; }
                                    if (minGridPosY > grid.GridPositionFixed.y) { minGridPosY = grid.GridPositionFixed.y; }
                                    if (maxGridPosX < grid.GridPositionFixed.x) { maxGridPosX = grid.GridPositionFixed.x; }
                                    if (maxGridPosY < grid.GridPositionFixed.y) { maxGridPosY = grid.GridPositionFixed.y; }
                                }

                                roomDoor.CurrentGrid_DoorStart_Min = main.gridM.GetGrid(minGridPosX, minGridPosY);
                                roomDoor.CurrentGrid_DoorStart_Max = main.gridM.GetGrid(maxGridPosX, maxGridPosY);
                                roomDoor.CurrentGrid_DoorStart_Min.AddTag(GridTag.Room_Door_BothEnd_Min);
                                roomDoor.CurrentGrid_DoorStart_Max.AddTag(GridTag.Room_Door_BothEnd_Max);
                            }

                            if (doorStartEndBothEnds_End.Count != 0)
                            {
                                int minGridPosX = int.MaxValue; //. 최소 X
                                int minGridPosY = int.MaxValue; //. 최소 Y
                                int maxGridPosX = -int.MaxValue; //. 최대 X
                                int maxGridPosY = -int.MaxValue; //. 최대 Y

                                foreach (var grid in doorStartEndBothEnds_End)
                                {
                                    if (minGridPosX > grid.GridPositionFixed.x) { minGridPosX = grid.GridPositionFixed.x; }
                                    if (minGridPosY > grid.GridPositionFixed.y) { minGridPosY = grid.GridPositionFixed.y; }
                                    if (maxGridPosX < grid.GridPositionFixed.x) { maxGridPosX = grid.GridPositionFixed.x; }
                                    if (maxGridPosY < grid.GridPositionFixed.y) { maxGridPosY = grid.GridPositionFixed.y; }
                                }

                                roomDoor.CurrentGrid_DoorEnd_Min = main.gridM.GetGrid(minGridPosX, minGridPosY);
                                roomDoor.CurrentGrid_DoorEnd_Max = main.gridM.GetGrid(maxGridPosX, maxGridPosY);
                                roomDoor.CurrentGrid_DoorEnd_Min.AddTag(GridTag.Room_Door_BothEnd_Min);
                                roomDoor.CurrentGrid_DoorEnd_Max.AddTag(GridTag.Room_Door_BothEnd_Max);
                            }

                            //? 도어의 MidRect, Expand에 <Door_Area>, <Door_Area_Expand> 그리드 이벤트를 실행한다
                            main.gridM.GridsEvent_Rect(true, fullDoorRect_E, main.Setting.StageGrid.GridTagSetting_RoomDoorArea.EnableGrid, roomDoor.GetDoorGridsWithReset());
                        }
                        finally
                        {
                            //. 리스트풀 반환 (반드시 Clear 후 Release)
                            doorStartEndBothEnds_Start.Clear();
                            doorStartEndBothEnds_End.Clear();
                            ListPool<Grid>.Release(doorStartEndBothEnds_Start); //! Start 리스트 반환
                            ListPool<Grid>.Release(doorStartEndBothEnds_End);   //! End 리스트 반환
                        }
                    }
                }



                ///======================================================================================================================================================


                /// <summary>
                /// BSP 분할선(Spine)을 먼저 깔고, 모든 도어를 Spine에 수직 연결하는 유틸.
                /// 도어↔도어 직접 연결을 없애므로 교차가 원천적으로 불가능.
                /// </summary>
                public static class DoorSpineConnector
                {
                    public enum SplitAxis { Vertical /*x=const*/, Horizontal /*y=const*/ }

                    public readonly struct DoorPoint
                    {
                        public readonly Vector2 Pos;      // 그리드/월드 중심(선형 투영용)
                        public readonly object DoorRef;   // 기존 도어 객체 참조 보존용
                        public DoorPoint(Vector2 pos, object doorRef) { Pos = pos; DoorRef = doorRef; }
                    }

                    /// <summary>
                    /// Spine을 만든 뒤, 양쪽(또는 단일측) 도어들을 Spine에 수직 스텁으로 연결합니다.
                    /// </summary>
                    /// <param name="axis">분할축: Vertical이면 x=const, Horizontal이면 y=const</param>
                    /// <param name="spineConst">Vertical:x, Horizontal:y 의 상수값(분할선 위치)</param>
                    /// <param name="spineMinT">Spine 구간 시작(축 직교가 아닌, 투영축 값. V면 y-min, H면 x-min)</param>
                    /// <param name="spineMaxT">Spine 구간 끝(동일 기준)</param>
                    /// <param name="doorsA">Spine 좌/하측 도어들</param>
                    /// <param name="doorsB">Spine 우/상측 도어들(없으면 null/빈 배열 가능)</param>
                    /// <param name="hallwayWidth">복도 폭(그리드 단위)</param>
                    /// <param name="spawnMain">메인 Spine 복도 생성 콜백 (start,end)</param>
                    /// <param name="spawnStub">도어→Spine 수직 스텁 생성 콜백 (start,end)</param>
                    public static void ConnectViaSpine(
                        SplitAxis axis,
                        float spineConst,
                        float spineMinT,
                        float spineMaxT,
                        IReadOnlyList<DoorPoint> doorsA,
                        IReadOnlyList<DoorPoint> doorsB,
                        float hallwayWidth,
                        Action<Vector2, Vector2> spawnMain,
                        Action<Vector2, Vector2> spawnStub)
                    {
                        var orderedA = OrderDoorPoints(axis, doorsA);
                        var orderedB = OrderDoorPoints(axis, doorsB);

                        ConnectViaSpineOrdered(
                            axis,
                            spineConst,
                            spineMinT,
                            spineMaxT,
                            orderedA,
                            orderedB,
                            hallwayWidth,
                            spawnMain,
                            spawnStub);
                    }



                    internal static void ConnectViaSpineOrdered(
                        SplitAxis axis,
                        float spineConst,
                        float spineMinT,
                        float spineMaxT,
                        IReadOnlyList<DoorPoint> orderedDoorsA,
                        IReadOnlyList<DoorPoint> orderedDoorsB,
                        float hallwayWidth,
                        Action<Vector2, Vector2> spawnMain,
                        Action<Vector2, Vector2> spawnStub)
                    {
                        //. 1) Spine 본선 생성
                        Vector2 s0, s1;
                        if (axis == SplitAxis.Vertical)
                        {
                            s0 = new Vector2(spineConst, spineMinT);
                            s1 = new Vector2(spineConst, spineMaxT);
                        }
                        else
                        {
                            s0 = new Vector2(spineMinT, spineConst);
                            s1 = new Vector2(spineMaxT, spineConst);
                        }
                        spawnMain(s0, s1);

                        //. 2) 도어들을 Spine으로 수직 연결(교차 불가)
                        ConnectDoorsToSpineOrdered(axis, spineConst, orderedDoorsA, hallwayWidth, spawnStub, negativeSide: true);
                        ConnectDoorsToSpineOrdered(axis, spineConst, orderedDoorsB, hallwayWidth, spawnStub, negativeSide: false);
                    }



                    internal static DoorPoint[] OrderDoorPoints(SplitAxis axis, IReadOnlyList<RoomObject.Door> doors)
                    {
                        if (doors == null || doors.Count == 0) { return Array.Empty<DoorPoint>(); }

                        return doors
                            .Select(d => new DoorPoint(d.DoorEndPosition, d))
                            .OrderBy(d => axis == SplitAxis.Vertical ? d.Pos.y : d.Pos.x)
                            .ToArray();
                    }



                    internal static RoomObject.Door SelectFirstDoorByProjection(
                        SplitAxis axis,
                        IReadOnlyList<RoomObject.Door> doors)
                    {
                        if (doors == null || doors.Count == 0) { return null; }

                        RoomObject.Door selectedDoor = null;
                        float selectedProjection = float.PositiveInfinity;

                        for (int i = 0; i < doors.Count; i++)
                        {
                            RoomObject.Door door = doors[i];
                            if (door == null) { continue; }

                            Vector2 position = door.DoorEndPosition;
                            float projection = axis == SplitAxis.Vertical ? position.y : position.x;

                            //. 같은 투영값에서는 처음 등장한 도어를 유지해 안정 정렬의 첫 항목과 동일하게 동작한다
                            if (selectedDoor == null || projection < selectedProjection)
                            {
                                selectedDoor = door;
                                selectedProjection = projection;
                            }
                        }

                        return selectedDoor;
                    }



                    internal static DoorPoint[] OrderDoorPoints(SplitAxis axis, IReadOnlyList<DoorPoint> doors)
                    {
                        if (doors == null || doors.Count == 0) { return Array.Empty<DoorPoint>(); }

                        return doors
                            .OrderBy(d => axis == SplitAxis.Vertical ? d.Pos.y : d.Pos.x)
                            .ToArray();
                    }



                    /// <summary>
                    /// 단일 측 도어들을 Spine으로 수직 연결.
                    /// </summary>
                    private static void ConnectDoorsToSpineOrdered(
                        SplitAxis axis, float spineConst,
                        IReadOnlyList<DoorPoint> orderedDoors,
                        float hallwayWidth,
                        Action<Vector2, Vector2> spawnStub,
                        bool negativeSide)
                    {
                        if (orderedDoors == null || orderedDoors.Count == 0) return;

                        //? 같은 t(투영값)에서 양측 스텁이 겹치지 않게 아주 약간의 오프셋 처리
                        const float epsilon = 0.001f;

                        for (int i = 0; i < orderedDoors.Count; i++)
                        {
                            var p = orderedDoors[i].Pos;
                            float t = (axis == SplitAxis.Vertical ? p.y : p.x) + i * epsilon; //. 미세 오프셋

                            Vector2 spinePoint = axis == SplitAxis.Vertical
                                ? new Vector2(spineConst, t)
                                : new Vector2(t, spineConst);

                            //? 수직 스텁 방향: negativeSide면 Spine쪽으로 +x/+y, 아니면 반대로
                            Vector2 stubEnd = axis == SplitAxis.Vertical
                                ? new Vector2(p.x, t)   // Horizontal(수평)으로 Spine↔Door
                                : new Vector2(t, p.y);  // Vertical(수직)으로 Spine↔Door

                            //! Door와 Spine 사이가 0이면(이미 정확히 붙어있으면) 스킵
                            if ((stubEnd - spinePoint).sqrMagnitude < 1e-6f) continue;

                            spawnStub(spinePoint, stubEnd);
                        }
                    }
                }



                ///======================================================================================================================================================
            }



            ///======================================================================================================================================================
        }
    }
}
