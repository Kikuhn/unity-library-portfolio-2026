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
using System.Threading.Tasks;
using UnityEngine.Pool;



namespace Pan.StageGenerators
{
    public partial class StageGenerator
    {
        public partial class PlaceManger : BaseManager
        {
            ///======================================================================================================================================================



            ///<summary>
            ///도어와 도어를 복도로 이어주기
            /// </summary>
            [Serializable]
            public class Generator3_ConnectHallways
            {
                ///======================================================================================================================================================



                //? 상수 레이어



                /// <summary>
                /// 최대 점유 레이어 (복도를 이어줄때, 시작점과 끝점의 점유를 무조건 해제해야 하기 떄문에 사용하는 레이어)
                /// </summary>
                private const int MAX_OCCUPIED_LAYER = 9999;


                /// <summary>
                /// 임시 점유 레이어 (복도를 이어줄때 방해를 덜받기 위해 다른 구역을 점유할때 사용하는 레이어)
                /// </summary>
                private const int TEMP_OCCUPIED_LAYER = 5;



                ///======================================================================================================================================================



                //? 생성 시작



                ///<summary>
                ///<b>"인스턴스화된 방"</b>과 <b>"공간의 정보에 맞춰 방의 도어끼리 연결되어있는"</b><br/>
                ///공간 리스트를 받아와, 문과 문 사이의 복도를 생성한다 (A*)
                ///</summary>
                public bool Generate(StageGenerator main, IReadOnlyList<Space> spaceList)
                {
                    main.GenerateM.GeneratingCurrentSteps = GenerateManager.GenerateSteps.GenStep5_Places_Gen3_ConnectHallways;


                    using (main.placeM.Gen_ASharpPathFinder.Pathfind)
                    {
                        //? 패스파인더 활성화
                        main.placeM.Gen_ASharpPathFinder.Pathfind.EnablePathFinder = true;


                        //. 커스텀 이벤트: 복도들 생성 전
                        if (main.Setting.CustomEvent.PlaceHallway != null) { main.Setting.CustomEvent.PlaceHallway.Before_GenerateHallways(main, spaceList); }


                        main.placeM.EnableDebugLog_FailedPathFindingOrHallway = main.Setting.Room.UseRoomPlaceNearest && main.Setting.Hallawy.UseChoronoBreak_ByHallways_RoomPlaceNearest;


                        var connectHallways = Execute_ConnectHallways(main, spaceList);


                        //! 복도 연결에 실패
                        if (!connectHallways)
                        {
                            main.placeM.EnableDebugLog_FailedPathFindingOrHallway = false;
                            return false;

                            //! 250819~ 개편된 인접 로직에 따라, 크르노 브레이크 코드 미사용


                            //! 크로노 브레이크 기능이 꺼져있다면, 실패한다
                            if (!main.Setting.Hallawy.UseChoronoBreak_ByHallways_RoomPlaceNearest) { return false; }


                            //! 방 인접성을 사용중이고, 크로노 브레이크 기능이 켜져있다면
                            //? 방 인접을 점진적으로 되돌리며 크로노브레이크를 시도한다
                            main.placeM.EnableDebugLog_FailedPathFindingOrHallway = false;

                            if (ChoronoBreak_ByHallways_ReGenerate(main, spaceList))
                            {
                                connectHallways = true;
                            }
                            else
                            {
                                //! 크로노 브레이크를 했음에도 실패
                                return false;
                            }
                        }


                        //? (병렬) 그리드 리스트틀에 생성한 정보들을 저장, 적용
                        Parallel.ForEach(spaceList, (space) =>
                        {
                            if (space.PlacedRoom == null) { return; }
                            var roomObject = space.PlacedRoom;

                            Parallel.ForEach(spaceList, (space) =>
                            {
                                if (space.PlacedRoom == null) { return; }
                                var roomObject = space.PlacedRoom;
                                Parallel.Invoke(() =>
                                {
                                    addPlaceGrids_Hallways(roomObject, EDirection4.Down);
                                }, () =>
                                {
                                    addPlaceGrids_Hallways(roomObject, EDirection4.Up);
                                }, () =>
                                {
                                    addPlaceGrids_Hallways(roomObject, EDirection4.Left);
                                }, () =>
                                {
                                    addPlaceGrids_Hallways(roomObject, EDirection4.Right);
                                });
                            }); ;
                        });


                        //. 커스텀 이벤트: 복도들 생성 이후
                        if (main.Setting.CustomEvent.PlaceHallway != null) { main.Setting.CustomEvent.PlaceHallway.After_GenerateHallways(main, spaceList); }


                        return connectHallways;
                    }


                    void addPlaceGrids_Hallways(RoomObject roomObject, EDirection4 direction)
                    {
                        var doorList = roomObject.RoomDoorM.GetDoorList(direction);


                        //? 병렬처리
                        if (doorList.Count > main.CalculateSetting.PlaceGrid_HallwaysFromDoor_ParallelCount)
                        {
                            Parallel.ForEach(doorList, (door) =>
                            {
                                addPlaceGrid(door.GetDoorGrids);
                                addPlaceGrid(door.GetHallwayCorrectionGridList);
                                addPlaceGrid(door.GetHallwayPathGridList);
                            });
                        }
                        else
                        {
                            foreach (var door in roomObject.RoomDoorM.GetDoorList(direction))
                            {
                                addPlaceGrid(door.GetDoorGrids);
                                addPlaceGrid(door.GetHallwayCorrectionGridList);
                                addPlaceGrid(door.GetHallwayPathGridList);
                            }
                        }


                        void addPlaceGrid(IReadOnlyCollection<Grid> grids)
                        {
                            foreach (var grid in grids)
                            {
                                if (grid.ContainsTag(GridTag.Hallway))
                                {
                                    lock (main.placeM.PlaceObjects_Hallway.GetPlacedGrids)
                                    {
                                        main.placeM.PlaceObjects_Hallway.GetPlacedGrids.Add(grid);
                                    }
                                }
                                else if (grid.ContainsTag(GridTag.Hallway_Edge))
                                {
                                    lock (main.placeM.placeObjects_HallwayEdge.GetPlacedGrids)
                                    {
                                        main.placeM.placeObjects_HallwayEdge.GetPlacedGrids.Add(grid);
                                    }
                                }
                                else if (grid.ContainsTag(GridTag.Hallway_Safe))
                                {
                                    lock (main.placeM.grids_CustomExpand)
                                    {
                                        main.placeM.grids_CustomExpand.Add(grid);
                                    }
                                }
                            }
                        }
                    }
                }



                ///<summary>
                ///<b>"인스턴스화된 방"</b>과 <b>"공간의 정보에 맞춰 방의 도어끼리 연결되어있는"</b><br/>
                ///공간 리스트를 받아와, 문과 문 사이의 복도를 생성한다 (A*)
                ///</summary>
                public async UniTask<bool> GenerateAsync(StageGenerator main, IReadOnlyList<Space> spaceList)
                {
                    main.GenerateM.GeneratingCurrentSteps = GenerateManager.GenerateSteps.GenStep5_Places_Gen3_ConnectHallways;


                    using (main.placeM.Gen_ASharpPathFinder.Pathfind)
                    {
                        //? 패스파인더 활성화
                        main.placeM.Gen_ASharpPathFinder.Pathfind.EnablePathFinder = true;


                        //. 커스텀 이벤트: 복도들 생성 전
                        await (main.Setting.CustomEvent.PlaceHallway != null ? main.Setting.CustomEvent.PlaceHallway.Before_GenerateHallwaysAsync(main, spaceList) : UniTask.CompletedTask);


                        main.placeM.EnableDebugLog_FailedPathFindingOrHallway = main.Setting.Room.UseRoomPlaceNearest && main.Setting.Hallawy.UseChoronoBreak_ByHallways_RoomPlaceNearest;


                        var connectHallways = await Execute_ConnectHallwaysAsync(main, spaceList);


                        //! 복도 연결에 실패
                        if (!connectHallways)
                        {
                            main.placeM.EnableDebugLog_FailedPathFindingOrHallway = false;
                            return false;

                            //! 250819~ 개편된 인접 로직에 따라, 크르노 브레이크 코드 미사용

                            //! 크로노 브레이크 기능이 꺼져있다면, 실패한다
                            if (!main.Setting.Hallawy.UseChoronoBreak_ByHallways_RoomPlaceNearest) { return false; }


                            //! 방 인접성을 사용중이고, 크로노 브레이크 기능이 켜져있다면
                            //? 방 인접을 점진적으로 되돌리며 크로노브레이크를 시도한다
                            main.placeM.EnableDebugLog_FailedPathFindingOrHallway = false;

                            if (await ChoronoBreak_ByHallways_ReGenerateAsync(main, spaceList))
                            {
                                connectHallways = true;
                            }
                            else
                            {
                                //! 크로노 브레이크를 했음에도 실패
                                return false;
                            }
                        }


                        //? (병렬) 그리드 리스트틀에 생성한 정보들을 저장, 적용
                        Parallel.ForEach(spaceList, (space) =>
                        {
                            if (space.PlacedRoom == null) { return; }
                            var roomObject = space.PlacedRoom;
                            Parallel.Invoke(() =>
                            {
                                addPlaceGrids_Hallways(roomObject, EDirection4.Down);
                            }, () =>
                            {
                                addPlaceGrids_Hallways(roomObject, EDirection4.Up);
                            }, () =>
                            {
                                addPlaceGrids_Hallways(roomObject, EDirection4.Left);
                            }, () =>
                            {
                                addPlaceGrids_Hallways(roomObject, EDirection4.Right);
                            });
                        });


                        //. 커스텀 이벤트: 복도들 생성 이후
                        await (main.Setting.CustomEvent.PlaceHallway != null ? main.Setting.CustomEvent.PlaceHallway.After_GenerateHallwaysAsync(main, spaceList) : UniTask.CompletedTask);


                        return connectHallways;
                    }


                    void addPlaceGrids_Hallways(RoomObject roomObject, EDirection4 direction)
                    {
                        var doorList = roomObject.RoomDoorM.GetDoorList(direction);


                        //? 병렬처리
                        if (doorList.Count > main.CalculateSetting.PlaceGrid_HallwaysFromDoor_ParallelCount)
                        {
                            Parallel.ForEach(doorList, (door) =>
                            {
                                addPlaceGrid(door.GetDoorGrids);
                                addPlaceGrid(door.GetHallwayCorrectionGridList);
                                addPlaceGrid(door.GetHallwayPathGridList);
                            });
                        }
                        else
                        {
                            foreach (var door in roomObject.RoomDoorM.GetDoorList(direction))
                            {
                                addPlaceGrid(door.GetDoorGrids);
                                addPlaceGrid(door.GetHallwayCorrectionGridList);
                                addPlaceGrid(door.GetHallwayPathGridList);
                            }
                        }


                        void addPlaceGrid(IReadOnlyCollection<Grid> grids)
                        {
                            foreach (var grid in grids)
                            {
                                if (grid.ContainsTag(GridTag.Hallway))
                                {
                                    lock (main.placeM.PlaceObjects_Hallway.GetPlacedGrids)
                                    {
                                        main.placeM.PlaceObjects_Hallway.GetPlacedGrids.Add(grid);
                                    }
                                }
                                else if (grid.ContainsTag(GridTag.Hallway_Edge))
                                {
                                    lock (main.placeM.placeObjects_HallwayEdge.GetPlacedGrids)
                                    {
                                        main.placeM.placeObjects_HallwayEdge.GetPlacedGrids.Add(grid);
                                    }
                                }
                                else if (grid.ContainsTag(GridTag.Hallway_Safe))
                                {
                                    lock (main.placeM.grids_CustomExpand)
                                    {
                                        main.placeM.grids_CustomExpand.Add(grid);
                                    }
                                }
                            }
                        }
                    }
                }



                ///======================================================================================================================================================



                //? 생성



                /// <summary>
                /// 복도 연결을 위해, 공간들을 정렬하여 반환한다<br/>
                /// <b>1. 공간의 크기 + 방의 크기 (존재한다면) 내림차순</b> <i>(크기가 큰 순)</i><br/>
                /// <b>2. 해당 공간의 노드 개수 내림차순</b> <i>(노드가 많은 순)</i>
                /// </summary>
                private IOrderedEnumerable<Space> GetOrdered_SpaceList_ForConnectHallways(IReadOnlyList<Space> spaceList)
                {
                    var result = spaceList
                        .OrderByDescending(x =>
                        {
                            float r = 0f;
                            if (x.PlacedRoom != null) { r = x.PlacedRoom.GridCompatible.ObjectSizeOriginalVector2.sqrMagnitude; }
                            return x.SpaceRect.size.sqrMagnitude + r;
                        })
                        .ThenByDescending(x => x.TotalConnectingSpacesCount);

                    return result;
                }



                ///<summary>
                ///<b>"인스턴스화된 방"</b>과 <b>"공간의 정보에 맞춰 방의 도어끼리 연결되어있는"</b><br/>
                ///공간 리스트를 받아와, 문과 문 사이의 복도를 생성한다 (A*)
                ///</summary>
                private bool Execute_ConnectHallways(StageGenerator main, IReadOnlyList<Space> spaceList)
                {
                    //? 모든 방의 "단일 방향 안전구역 확장" 을 [점유] 시킨다,
                    //. 방이 모두 생성된 이후 실행되는 첫 순회 이니, 방의 CurrentGirds에 "안전구역 확장" 있는 모든 그리드들도 겸사겸사 같이 추가시킨다 (addCurrentGrids 파라미터)
                    ControlGridsLoop_RoomSafeAreaExpand_SingleDirection(main, spaceList, TEMP_OCCUPIED_LAYER, true, true, true);
                    //. 실행되면, 모든 방들이, 활성화된 도어가 있는 방향의 "안전구역 확장" 이 "점유"된다,
                    //. 4방향이 모두 열려있다면 십자 모양으로 점유가 된것처럼 보이기도 한다


                    //? 복도 연결을 위해, 전용 정렬 로직으로 공간들을 정렬하여 얻는다
                    var sortList = GetOrdered_SpaceList_ForConnectHallways(spaceList);


                    int index = 1;
                    foreach (var space in sortList)
                    {
                        //! 검사 전, 공간과 연결되어있는 공간 자체가 없거나, 공간 안에 방이 없다면 복도를 생성할 필요 조차 없으므로 실패, continue
                        if (space.TotalConnectingSpacesCount == 0 || space.PlacedRoom == null) { continue; }


                        //. 방, 방 Rect
                        RoomObject roomObject = space.PlacedRoom;
                        if (!roomObject.TryGetInstanceRoomActivatedRectExpand(out var instanceRoomActivatedRectExpand)) { continue; }
                        //Rect roomRectSafeAreaExpand = main.placeM.RoomRectsCacheM.GetCachedInstanced_RoomSafeAreaExpands[roomObject].Rect;


                        //. 공간에 순서 기록
                        space.OrderIndex_Hallways = index;


                        //! 4개의 방향에 각각 복도를 생성한다
                        //! 복도 생성이 하나라도 실패시, 즉시 절차적 생성 자체가 실패한다
                        if (!Execute_ConnectDoorToDoor_4DirectionHallways(main, roomObject, in instanceRoomActivatedRectExpand)) { return false; }


                        index++;
                    }


                    //? 모든 방의 "단일 방향 안전구역 확장"를 [점유해제] 시킨다
                    ControlGridsLoop_RoomSafeAreaExpand_SingleDirection(main, spaceList, TEMP_OCCUPIED_LAYER, false, true, false);


                    return true;
                }



                ///<summary>
                ///[비동기] <b>"인스턴스화된 방"</b>과 <b>"공간의 정보에 맞춰 방의 도어끼리 연결되어있는"</b><br/>
                ///공간 리스트를 받아와, 문과 문 사이의 복도를 생성한다 (A*)
                ///</summary>
                private async UniTask<bool> Execute_ConnectHallwaysAsync(StageGenerator main, IReadOnlyList<Space> spaceList)
                {
                    //? 모든 방의 "단일 방향 안전구역 확장" 을 [점유] 시킨다,
                    //. 방이 모두 생성된 이후 실행되는 첫 순회 이니, 방의 CurrentGirds에 "안전구역 확장" 있는 모든 그리드들도 겸사겸사 같이 추가시킨다 (addCurrentGrids 파라미터)
                    ControlGridsLoop_RoomSafeAreaExpand_SingleDirection(main, spaceList, TEMP_OCCUPIED_LAYER, true, true, true);
                    //. 실행되면, 모든 방들이, 활성화된 도어가 있는 방향의 "안전구역 확장" 이 "점유"된다,
                    //. 4방향이 모두 열려있다면 십자 모양으로 점유가 된것처럼 보이기도 한다


                    //? 복도 연결을 위해, 전용 정렬 로직으로 공간들을 정렬하여 얻는다
                    var sortList = GetOrdered_SpaceList_ForConnectHallways(spaceList);


                    int index = 1;
                    foreach (var space in sortList)
                    {
                        //! 검사 전, 공간과 연결되어있는 공간 자체가 없거나, 공간 안에 방이 없다면 복도를 생성할 필요 조차 없으므로 실패, continue
                        if (space.TotalConnectingSpacesCount == 0 || space.PlacedRoom == null) { continue; }


                        //. 방, 방 Rect
                        RoomObject roomObject = space.PlacedRoom;
                        if (!roomObject.TryGetInstanceRoomActivatedRectExpand(out var instanceRoomActivatedRectExpand)) { continue; }
                        //Rect roomRectSafeAreaExpand = main.placeM.RoomRectsCacheM.GetCachedInstanced_RoomSafeAreaExpands[roomObject].Rect;
                        #region 캐싱 미사용
                        //Rect roomRectSafeAreaExpand = room.GetRoomSafeAreaExpandRect_BySpace(ESelectActivesMode.All, space, out var offset, main.gridM.Setting); //방 안전구역 확장 (추후 캐싱으로 교체?)
                        #endregion


                        //. 공간에 순서 기록
                        space.OrderIndex_Hallways = index;


                        //! 4개의 방향에 각각 복도를 생성한다
                        //! 복도 생성이 하나라도 실패시, 즉시 절차적 생성 자체가 실패한다
                        if (!await Execute_ConnectDoorToDoor_4DirectionHallwaysAsync(main, roomObject, instanceRoomActivatedRectExpand)) { return false; }


                        index++;


                        await UniTask.Yield();
                    }


                    //? 모든 방의 "단일 방향 안전구역 확장"를 [점유해제] 시킨다
                    ControlGridsLoop_RoomSafeAreaExpand_SingleDirection(main, spaceList, TEMP_OCCUPIED_LAYER, false, true, false);


                    return true;
                }



                /// <summary>
                /// 해당 방의, 4방향의 도어들에게,<br/>
                /// 연결된 도어가 있다면, 복도로 이어준다<br/>
                /// 복도 생성이 하나라도 실패한다면, 중단되고 false를 반환한다
                /// </summary>
                /// <param name="main"></param>
                /// <param name="roomObject"></param>
                /// <param name="instanceRoomActivatedRectExpand"></param>
                /// <returns></returns>
                private bool Execute_ConnectDoorToDoor_4DirectionHallways(StageGenerator main, RoomObject roomObject, in CustomRect2DRelative instanceRoomActivatedRectExpand)
                {
                    if (!Execute_ConnectDoorToDoor_Hallway(main, roomObject, EDirection4.Down, in instanceRoomActivatedRectExpand)) { return false; }
                    if (!Execute_ConnectDoorToDoor_Hallway(main, roomObject, EDirection4.Up, in instanceRoomActivatedRectExpand)) { return false; }
                    if (!Execute_ConnectDoorToDoor_Hallway(main, roomObject, EDirection4.Left, in instanceRoomActivatedRectExpand)) { return false; }
                    if (!Execute_ConnectDoorToDoor_Hallway(main, roomObject, EDirection4.Right, in instanceRoomActivatedRectExpand)) { return false; }

                    //. 받아온 방의 4방향의 복도 생성에 모두 성공!

                    return true;
                }



                /// <summary>
                /// [비동기] 해당 방의, 4방향의 도어들에게,<br/>
                /// 연결된 도어가 있다면, 복도로 이어준다<br/>
                /// 복도 생성이 하나라도 실패한다면, 중단되고 false를 반환한다
                /// </summary>
                /// <param name="main"></param>
                /// <param name="roomObject"></param>
                /// <param name="instanceRoomActivatedRectExpand"></param>
                /// <returns></returns>
                private async UniTask<bool> Execute_ConnectDoorToDoor_4DirectionHallwaysAsync(StageGenerator main, RoomObject roomObject, CustomRect2DRelative instanceRoomActivatedRectExpand)
                {
                    if (!await Execute_ConnectDoorToDoor_HallwayAsync(main, roomObject, EDirection4.Down, instanceRoomActivatedRectExpand)) { return false; }
                    if (!await Execute_ConnectDoorToDoor_HallwayAsync(main, roomObject, EDirection4.Up, instanceRoomActivatedRectExpand)) { return false; }
                    if (!await Execute_ConnectDoorToDoor_HallwayAsync(main, roomObject, EDirection4.Left, instanceRoomActivatedRectExpand)) { return false; }
                    if (!await Execute_ConnectDoorToDoor_HallwayAsync(main, roomObject, EDirection4.Right, instanceRoomActivatedRectExpand)) { return false; }

                    //. 받아온 방의 4방향의 복도 생성에 모두 성공!

                    return true;
                }



                /// <summary>
                /// 해당 방의, 해당 방향의 도어들에게,<br/>
                /// 연결된 도어가 있다면, 복도로 이어준다
                /// </summary>
                /// <param name="main"></param>
                /// <param name="roomObject"></param>
                /// <param name="connectDirection"></param>
                /// <param name="instanceRoomActivatedRectExpand"></param>
                /// <returns></returns>
                private bool Execute_ConnectDoorToDoor_Hallway(StageGenerator main, RoomObject roomObject, EDirection4 connectDirection, in CustomRect2DRelative instanceRoomActivatedRectExpand)
                {
                    //? "해당 방향"에 있는 "단일 방향 안전구역 확장 구역"의 그리드들에게
                    //? 도어를 제외하고,
                    //? [점유해제] 시킨다
                    ControlGrids_RoomSafeAreaExpand_SelectSingleDirectionWithOutDoor(main, roomObject, in instanceRoomActivatedRectExpand, connectDirection, TEMP_OCCUPIED_LAYER, false, true);


                    //? 해당 방향의 도어 리스트로 순회한다
                    foreach (var door in roomObject.RoomDoorM.GetDoorList(connectDirection))
                    {
                        //! 도어가 활성화 되지 않았다면 넘어간다 continue
                        if (!door.DoorEnable) { continue; }


                        //! 도어와 도어를 이어준다!
                        //! 실패시 즉시 순회를 종료하고 실패한다
                        if (!ConnectDoorToDoor_Hallway(main, roomObject, door)) { return false; }
                    }


                    //. 받아온 방의 지정된 방향의 복도 생성에 성공!


                    //? 도어를 제외하고,
                    //? "해당 방향"에 있는 "단일 방향 안전구역 확장 구역"의 그리드들에게
                    //? [점유] 시킨다
                    ControlGrids_RoomSafeAreaExpand_SelectSingleDirectionWithOutDoor(main, roomObject, in instanceRoomActivatedRectExpand, connectDirection, TEMP_OCCUPIED_LAYER, true, true);


                    return true;
                }



                /// <summary>
                /// [비동기] 해당 방의, 해당 방향의 도어들에게,<br/>
                /// 연결된 도어가 있다면, 복도로 이어준다
                /// </summary>
                /// <param name="main"></param>
                /// <param name="roomObject"></param>
                /// <param name="connectDirection"></param>
                /// <param name="instanceRoomActivatedRectExpand"></param>
                /// <returns></returns>
                private async UniTask<bool> Execute_ConnectDoorToDoor_HallwayAsync(StageGenerator main, RoomObject roomObject, EDirection4 connectDirection, CustomRect2DRelative instanceRoomActivatedRectExpand)
                {
                    //? "해당 방향"에 있는 "단일 방향 안전구역 확장 구역"의 그리드들에게
                    //? 도어를 제외하고,
                    //? [점유해제] 시킨다
                    ControlGrids_RoomSafeAreaExpand_SelectSingleDirectionWithOutDoor(main, roomObject, in instanceRoomActivatedRectExpand, connectDirection, TEMP_OCCUPIED_LAYER, false, true);


                    //? 해당 방향의 도어 리스트로 순회한다
                    foreach (var door in roomObject.RoomDoorM.GetDoorList(connectDirection))
                    {
                        //! 도어가 활성화 되지 않았다면 넘어간다 continue
                        if (!door.DoorEnable) { continue; }


                        //! 도어와 도어를 이어준다!
                        //! 실패시 즉시 순회를 종료하고 실패한다
                        if (!await ConnectDoorToDoor_HallwayAsync(main, roomObject, door)) { return false; }
                    }


                    //. 받아온 방의 지정된 방향의 복도 생성에 성공!


                    //? 도어를 제외하고,
                    //? "해당 방향"에 있는 "단일 방향 안전구역 확장 구역"의 그리드들에게
                    //? [점유] 시킨다
                    ControlGrids_RoomSafeAreaExpand_SelectSingleDirectionWithOutDoor(main, roomObject, in instanceRoomActivatedRectExpand, connectDirection, TEMP_OCCUPIED_LAYER, true, true);


                    return true;
                }



                /// <summary>
                /// 해당 방의, 해당 방향의 도어들에게,<br/>
                /// 연결된 도어가 있다면, 복도로 이어준다<br/>
                /// <see cref="Execute_ConnectDoorToDoor_Hallway"/> 에서 실행되어야함
                /// </summary>
                private bool ConnectDoorToDoor_Hallway(StageGenerator main, RoomObject currentRoomObject, RoomObject.Door currentDoor)
                {
                    //! 이미 복도로 연결이 되었거나 ||
                    //! 도어와 연결된 도어가 없거나 ||
                    //! 연결된 도어가 이미 복도로 연결이 되어있다면
                    //! return (실패는 아니기에, false가 아닌 true를 반환한다)
                    if (currentDoor.IsEnabledPathHallway || currentDoor.ConnectecDoor == null || currentDoor.ConnectecDoor.IsEnabledPathHallway) { return true; }


                    //. 연결 도어, 방, 방 Rect
                    var connectingDoor = currentDoor.ConnectecDoor;
                    //Rect connectingRoomRectSafeAreaExpand = main.placeM.RoomRectsCacheM.GetCachedInstanced_RoomSafeAreaExpands[connectingDoor.ParentRoomObject].Rect;
                    //! 250808 추가
                    if (!connectingDoor.ParentRoomObject.TryGetInstanceRoomActivatedRectExpand(out var connectingDoor_RoomActivatedRectExpand)) { return false; }

                    #region 캐싱 미사용
                    //var connectingRoomRectSafeAreaExpand = connectingDoor.ParentRoom.GetRoomSafeAreaExpandRect_BySpace(ESelectActivesMode.All, connectingDoor.ParentRoom.CurrentSpace, main.gridM.Setting); //연결 방 안전구역 확장
                    #endregion


                    //? 복도를 생성하기 전 그리드 설정
                    ControlGrids_CreateHallway_Before(main, currentDoor, connectingDoor, connectingDoor_RoomActivatedRectExpand);


                    //. ★ 복도 연결 시작! Start ★


                    bool isSuccess = true;


                    //? 이 도어의 Start ~ End 까지 복도 연결 시도
                    if (isSuccess && !Hallway_DoorStart_To_DoorEnd(main, currentDoor, false))
                    {
                        main.logM.LogError_Place_Gen3_DoorStartToDoorEnd(main, currentDoor);
                        isSuccess = false;
                    }


                    //? 연결된 도어의 Start ~ End 까지 복도 연결 시도
                    if (isSuccess && !Hallway_DoorStart_To_DoorEnd(main, connectingDoor, false))
                    {
                        main.logM.LogError_Place_Gen3_DoorStartToDoorEnd(main, connectingDoor);
                        isSuccess = false;
                    }


                    //? 도어의 End ~ 연결된 도어의 보정거리 까지 복도 연결
                    if (isSuccess && !Hallway_DoorEnd_To_DoorEnd(main, currentDoor, connectingDoor, false, null))
                    {
                        main.logM.LogError_Place_Gen3_DoorEndToDoorEnd(main, currentDoor, connectingDoor);
                        isSuccess = false;
                    }


                    if (!isSuccess) { return false; }


                    //. ★ 복도 연결 끝! End ★


                    //? 복도를 생성한 후 그리드 설정
                    ControlGrids_CreateHallway_After(main, currentDoor, connectingDoor, connectingDoor_RoomActivatedRectExpand);


                    return true;
                }



                /// <summary>
                /// [비동기] 해당 방의, 해당 방향의 도어들에게,<br/>
                /// 연결된 도어가 있다면, 복도로 이어준다<br/>
                /// <see cref="Execute_ConnectDoorToDoor_HallwayAsync"/> 에서 실행되어야함
                /// </summary>
                private async UniTask<bool> ConnectDoorToDoor_HallwayAsync(StageGenerator main, RoomObject currentRoomObject, RoomObject.Door currentDoor)
                {
                    //! 이미 복도로 연결이 되었거나 ||
                    //! 도어와 연결된 도어가 없거나 ||
                    //! 연결된 도어가 이미 복도로 연결이 되어있다면
                    //! return (실패는 아니기에, false가 아닌 true를 반환한다)
                    if (currentDoor.IsEnabledPathHallway || currentDoor.ConnectecDoor == null || currentDoor.ConnectecDoor.IsEnabledPathHallway) { return true; }


                    //. 연결 도어, 방, 방 Rect
                    var connectingDoor = currentDoor.ConnectecDoor;
                    //Rect connectingRoomRectSafeAreaExpand = main.placeM.RoomRectsCacheM.GetCachedInstanced_RoomSafeAreaExpands[connectingDoor.ParentRoomObject].Rect;
                    //! 250808 추가
                    if (!connectingDoor.ParentRoomObject.TryGetInstanceRoomActivatedRectExpand(out var connectingDoor_RoomActivatedRectExpand)) { return false; }
                    #region 캐싱 미사용
                    //var connectingRoomRectSafeAreaExpand = connectingDoor.ParentRoom.GetRoomSafeAreaExpandRect_BySpace(ESelectActivesMode.All, connectingDoor.ParentRoom.CurrentSpace, main.gridM.Setting); //연결 방 안전구역 확장
                    #endregion


                    //? 복도를 생성하기 전 그리드 설정
                    ControlGrids_CreateHallway_Before(main, currentDoor, connectingDoor, connectingDoor_RoomActivatedRectExpand);


                    //. ★ 복도 연결 시작! Start ★


                    bool isSuccess = true;


                    //? 이 도어의 Start ~ End 까지 복도 연결 시도
                    if (isSuccess && !await Hallway_DoorStart_To_DoorEndAsync(main, currentDoor, false))
                    {
                        main.logM.LogError_Place_Gen3_DoorStartToDoorEnd(main, currentDoor);
                        isSuccess = false;
                    }


                    //? 연결된 도어의 Start ~ End 까지 복도 연결 시도
                    if (isSuccess && !await Hallway_DoorStart_To_DoorEndAsync(main, connectingDoor, false))
                    {
                        main.logM.LogError_Place_Gen3_DoorStartToDoorEnd(main, connectingDoor);
                        isSuccess = false;
                    }


                    //? 도어의 End ~ 연결된 도어의 보정거리 까지 복도 연결
                    if (isSuccess && !await Hallway_DoorEnd_To_DoorEndAsync(main, currentDoor, connectingDoor, false, null))
                    {
                        main.logM.LogError_Place_Gen3_DoorEndToDoorEnd(main, currentDoor, connectingDoor);
                        isSuccess = false;
                    }


                    if (!isSuccess) { return false; }


                    //. ★ 복도 연결 끝! End ★


                    //? 복도를 생성한 후 그리드 설정
                    ControlGrids_CreateHallway_After(main, currentDoor, connectingDoor, connectingDoor_RoomActivatedRectExpand);


                    return true;
                }



                ///======================================================================================================================================================



                //? 크로노 브레이크 (개편 이후 미사용)



                #region 미사용 크로노브레이크

                ///<summary>
                /// 복도가 충돌했고, 방 인접성을 사용중이라면,<br/>
                /// 설정값에 따라 크로노브레이크를 계속 시도한다
                /// </summary>
                public bool ChoronoBreak_ByHallways_ReGenerate(StageGenerator main, IReadOnlyList<Space> spaceList)
                {
                    //! 250813 준폐기

                    return false;

                    //main.logM.Log_Place_Gen3_ChoronoBreak_ByHallways_ReGenerate();


                    //// 설정값의 최대 탐색횟수가 인접한 횟수보다 크면 안되니까 제약한다
                    //int maxLoop = Mathf.Max(main.Setting.Room.RoomPlaceNearest_MaxChoronoBreak_ByHallways, main.placeM.Gen1_RoomCreater.GetBackUpRoomRects_HighCountIndex - 1); // RoomRect는 1부터 시작하니까 -1


                    //// 점진적으로 크로노브레이크 횟수가 증가하게
                    //int choronoBreakAdditiion = 1;
                    //for (int i = 1; i < maxLoop; i++) //! i는 1부터시작!!
                    //{
                    //    //? 크로노 브레이크
                    //    main.placeM.Gen1_RoomCreater.ChoronoBreak_ByHallways_ReGenerate_SecondRoomRects(main, spaceList, choronoBreakAdditiion);


                    //    //? 다시 연결 시도, 성공시 true 반환
                    //    if (Execute_ConnectHallways(main, spaceList))
                    //    {
                    //        main.placeM.Count_ChoronoBreakLoopBack_ByHallways = choronoBreakAdditiion;
                    //        main.logM.Log_Place_Gen3_ChoronoBreak_ByHallways_ReGenerate_Success(main.placeM.Count_ChoronoBreakLoopBack_ByHallways);
                    //        return true;
                    //    }


                    //    choronoBreakAdditiion += 1; //점진적으로 1씩 증가 (등차수열)
                    //}


                    //main.logM.LogError_Place_Gen3_ChoronoBreak_ByHallways_ReGenerate_Failure();


                    //return false;
                }



                ///<summary>
                /// [비동기] 복도가 충돌했고, 방 인접성을 사용중이라면,<br/>
                /// 설정값에 따라 크로노브레이크를 계속 시도한다
                /// </summary>
                public async UniTask<bool> ChoronoBreak_ByHallways_ReGenerateAsync(StageGenerator main, IReadOnlyList<Space> spaceList)
                {
                    await UniTask.CompletedTask;

                    return false;   //! 250813 준폐기

                    //main.logM.Log_Place_Gen3_ChoronoBreak_ByHallways_ReGenerate();


                    //// 설정값의 최대 탐색횟수가 인접한 횟수보다 크면 안되니까 제약한다
                    //int maxLoop = Mathf.Max(main.Setting.Room.RoomPlaceNearest_MaxChoronoBreak_ByHallways, main.placeM.Gen1_RoomCreater.GetBackUpRoomRects_HighCountIndex - 1); // RoomRect는 1부터 시작하니까 -1


                    //// 점진적으로 크로노브레이크 횟수가 증가하게
                    //int choronoBreakAdditiion = 1;
                    //for (int i = 1; i < maxLoop; i++) //! i는 1부터시작!!
                    //{
                    //    //? 크로노 브레이크
                    //    await main.placeM.Gen1_RoomCreater.ChoronoBreak_ByHallways_ReGenerate_SecondRoomRectsAsync(main, spaceList, choronoBreakAdditiion);


                    //    //? 다시 연결 시도, 성공시 true 반환
                    //    if (await Execute_ConnectHallwaysAsync(main, spaceList))
                    //    {
                    //        main.placeM.Count_ChoronoBreakLoopBack_ByHallways = choronoBreakAdditiion;
                    //        main.logM.Log_Place_Gen3_ChoronoBreak_ByHallways_ReGenerate_Success(main.placeM.Count_ChoronoBreakLoopBack_ByHallways);
                    //        return true;
                    //    }


                    //    choronoBreakAdditiion += 1; //점진적으로 1씩 증가 (등차수열)                        
                    //    await UniTask.Yield();
                    //}


                    //main.logM.LogError_Place_Gen3_ChoronoBreak_ByHallways_ReGenerate_Failure();


                    //return false;
                }

                #endregion



                ///======================================================================================================================================================



                //? 복도 연결



                /// <summary>복도를 이어준다 (<b>도어 Start </b>와 해당 <b>도어 End</b> 까지)</summary>
                /// <param name="door">대상 도어</param>
                /// <param name="resultHallwayList">복도를 설정한 그리드들을 반환한다, null일시 해당되지 않는다</param>
                private bool Hallway_DoorStart_To_DoorEnd(StageGenerator main, RoomObject.Door door, bool overlap)
                {
                    //! 이미 도어 보정거리가 생성되었다면 실패
                    if (door.IsEnabledCorrectionHallway && !overlap) { return true; }


                    //? 이미 도어 보정거리가 생성되었지만, 이를 무시하고 새로 생성하려 할 경우
                    //! 현재 생성된 도어 보정거리의 복도 관련 태그들을 제거하고, 보유 그리드 정보를 초기화한다
                    if (overlap && door.IsEnabledCorrectionHallway)
                    {
                        door.SetDisable_HallwayCorrectionGrids(false, GridManager.GridEvent_RemoveHallwayTags);
                    }


                    var stageOrigin = main.TransformM.StageCenterPositionTransform;


                    //. 도어의 Start 좌표 ~ End 좌표의 중심점(들)을 각각 얻어온다
                    door.CenterGridPosition_Start(out var start_IsDoubleCenter, out Vector2Int start_Center1, out var start_Center2);
                    door.CenterGridPosition_End(out var end_IsDoubleCenter, out var end_Center1, out var end_Center2);


                    ////? 그리드 좌표 보정 적용
                    //start_Center1 -= main.Setting.SnapSetting.GridPositionCorrection;
                    //start_Center2 -= main.Setting.SnapSetting.GridPositionCorrection;
                    //end_Center1 -= main.Setting.SnapSetting.GridPositionCorrection;
                    //end_Center2 -= main.Setting.SnapSetting.GridPositionCorrection;


                    #region 그리드 변환연산이 변경되며 사용하지않는 연산
                    ////? 보정연산 (스테이지 부모 좌표에 따른 보정)
                    //var correction = main.TransformM.StageParentPositionCurrentCache;
                    //if (main.Setting.SnapSetting.SnapToGridCellCenter)
                    //{
                    //    if (correction.x > 0)
                    //    {
                    //        start_Center1.x -= 1;
                    //        start_Center2.x -= 1;
                    //        end_Center1.x -= 1;
                    //        end_Center2.x -= 1;
                    //    }

                    //    if (correction.y > 0)
                    //    {
                    //        start_Center1.y -= 1;
                    //        start_Center2.y -= 1;
                    //        end_Center1.y -= 1;
                    //        end_Center2.y -= 1;
                    //    }
                    //} 
                    #endregion


                    //. 중심점이 1개라면, Center1을 사용하고,
                    //. 중심점이 2개라면, 그중 우/상에 위치한 Center가 final 변수에 들어간다
                    Vector2Int final_Start = start_IsDoubleCenter ? Util.GetCenter_PositivePositions(door.DoorDirection.IsHorizontal(), start_Center1, start_Center2) : start_Center1;
                    Vector2Int final_End = end_IsDoubleCenter ? Util.GetCenter_PositivePositions(door.DoorDirection.IsHorizontal(), end_Center1, end_Center2) : end_Center1;


                    //. 복도의 기본 너비, 최대 너비 구하기
                    main.Setting.Hallawy.GetDoorHallwayWidths_FromWidth(door.DoorWidth, out int mainHallwayWidth, out int maxHallwayWidth);


                    //? NavMesh를 지정해 A* 연산을 최적화한다
                    Vector2Int _clampBottomLeft = new Vector2Int(Mathf.Min(door.CurrentGrid_DoorStart_Min.GridPositionFixed.x, door.CurrentGrid_DoorEnd_Min.GridPositionFixed.x), Mathf.Min(door.CurrentGrid_DoorStart_Min.GridPositionFixed.y, door.CurrentGrid_DoorEnd_Min.GridPositionFixed.y));
                    Vector2Int _clampTopRight = new Vector2Int(Mathf.Max(door.CurrentGrid_DoorStart_Max.GridPositionFixed.x, door.CurrentGrid_DoorEnd_Max.GridPositionFixed.x), Mathf.Max(door.CurrentGrid_DoorStart_Max.GridPositionFixed.y, door.CurrentGrid_DoorEnd_Max.GridPositionFixed.y));
                    main.gridM.TryGet_GridPositionClamp(_clampBottomLeft - new Vector2Int(door.DoorWidth, door.DoorWidth), out var clampBottomLeft, out var t1);
                    main.gridM.TryGet_GridPositionClamp(_clampTopRight + new Vector2Int(door.DoorWidth, door.DoorWidth), out var clampTopRight, out var t2);



                    //! [ 도어Start Final ]-[ 도어End Final ] 의 최단 경로 리스트를 얻어보고,
                    //! 그 최단경로에 복도를 생성한다
                    //. 복도 재구성, 허용 그리드들은 지정하지 여기선 굳이 사용하지 않는다                   
                    using (var pooled = ListPool<Vector2Int>.Get(out var pathList))
                    {
                        if (main.placeM.Gen_ASharpPathFinder.Execute_PathFinding(main, pathList, final_Start, final_End, maxHallwayWidth, 1, main.Setting.Hallawy.HallwaySafeAreaLength, false, false, main.Setting.Hallawy.HallwayHeuristicType, main.Setting.Hallawy.HallwayHeuristicFactor, clampBottomLeft, clampTopRight) &&
                            TryGenerate_Hallways(main, pathList, true, mainHallwayWidth, maxHallwayWidth, false, false, out var correctionHallwayGridList))
                        {
                            door.SetEnable_HallwayCorrectionGrids(correctionHallwayGridList);
                            return true;
                        }
                    }

                    //! 위 조건에 불만족했을경우, 실패한다
                    return false;
                }



                /// <summary>[비동기] 복도를 이어준다 (<b>도어 Start </b>와 해당 <b>도어 End</b> 까지)</summary>
                /// <param name="door">대상 도어</param>
                /// <param name="resultHallwayList">복도를 설정한 그리드들을 반환한다, null일시 해당되지 않는다</param>
                private async UniTask<bool> Hallway_DoorStart_To_DoorEndAsync(StageGenerator main, RoomObject.Door door, bool overlap)
                {
                    //! 이미 도어 보정거리가 생성되었다면 실패
                    if (door.IsEnabledCorrectionHallway && !overlap) { return true; }


                    //? 이미 도어 보정거리가 생성되었지만, 이를 무시하고 새로 생성하려 할 경우
                    //! 현재 생성된 도어 보정거리의 복도 관련 태그들을 제거하고, 보유 그리드 정보를 초기화한다
                    if (overlap && door.IsEnabledCorrectionHallway)
                    {
                        door.SetDisable_HallwayCorrectionGrids(false, GridManager.GridEvent_RemoveHallwayTags);
                    }


                    var stageOrigin = main.TransformM.StageCenterPositionTransform;


                    //. 도어의 Start 좌표 ~ End 좌표의 중심점(들)을 각각 얻어온다
                    door.CenterGridPosition_Start(out var start_IsDoubleCenter, out Vector2Int start_Center1, out var start_Center2);
                    door.CenterGridPosition_End(out var end_IsDoubleCenter, out var end_Center1, out var end_Center2);


                    ////? 그리드 좌표 보정 적용
                    //start_Center1 -= main.Setting.SnapSetting.GridPositionCorrection;
                    //start_Center2 -= main.Setting.SnapSetting.GridPositionCorrection;
                    //end_Center1 -= main.Setting.SnapSetting.GridPositionCorrection;
                    //end_Center2 -= main.Setting.SnapSetting.GridPositionCorrection;


                    #region 그리드 변환연산이 변경되며 사용하지않는 연산
                    ////? 보정연산 (스테이지 부모 좌표에 따른 보정)
                    //var correction = main.TransformM.StageParentPositionCurrentCache;
                    //if (main.Setting.SnapSetting.SnapToGridCellCenter)
                    //{
                    //    if (correction.x > 0)
                    //    {
                    //        start_Center1.x -= 1;
                    //        start_Center2.x -= 1;
                    //        end_Center1.x -= 1;
                    //        end_Center2.x -= 1;
                    //    }

                    //    if (correction.y > 0)
                    //    {
                    //        start_Center1.y -= 1;
                    //        start_Center2.y -= 1;
                    //        end_Center1.y -= 1;
                    //        end_Center2.y -= 1;
                    //    }
                    //} 
                    #endregion


                    //. 중심점이 1개라면, Center1을 사용하고,
                    //. 중심점이 2개라면, 그중 우/상에 위치한 Center가 final 변수에 들어간다
                    Vector2Int final_Start = start_IsDoubleCenter ? Util.GetCenter_PositivePositions(door.DoorDirection.IsHorizontal(), start_Center1, start_Center2) : start_Center1;
                    Vector2Int final_End = end_IsDoubleCenter ? Util.GetCenter_PositivePositions(door.DoorDirection.IsHorizontal(), end_Center1, end_Center2) : end_Center1;


                    //. 복도의 기본 너비, 최대 너비 구하기
                    main.Setting.Hallawy.GetDoorHallwayWidths_FromWidth(door.DoorWidth, out int mainHallwayWidth, out int maxHallwayWidth);


                    //? NavMesh를 지정해 A* 연산을 최적화한다
                    Vector2Int _clampBottomLeft = new Vector2Int(Mathf.Min(door.CurrentGrid_DoorStart_Min.GridPositionFixed.x, door.CurrentGrid_DoorEnd_Min.GridPositionFixed.x), Mathf.Min(door.CurrentGrid_DoorStart_Min.GridPositionFixed.y, door.CurrentGrid_DoorEnd_Min.GridPositionFixed.y));
                    Vector2Int _clampTopRight = new Vector2Int(Mathf.Max(door.CurrentGrid_DoorStart_Max.GridPositionFixed.x, door.CurrentGrid_DoorEnd_Max.GridPositionFixed.x), Mathf.Max(door.CurrentGrid_DoorStart_Max.GridPositionFixed.y, door.CurrentGrid_DoorEnd_Max.GridPositionFixed.y));
                    main.gridM.TryGet_GridPositionClamp(_clampBottomLeft - new Vector2Int(door.DoorWidth, door.DoorWidth), out var clampBottomLeft, out var t1);
                    main.gridM.TryGet_GridPositionClamp(_clampTopRight + new Vector2Int(door.DoorWidth, door.DoorWidth), out var clampTopRight, out var t2);



                    //! [ 도어Start Final ]-[ 도어End Final ] 의 최단 경로 리스트를 얻어보고,
                    //! 그 최단경로에 복도를 생성한다
                    //. 복도 재구성, 허용 그리드들은 지정하지 여기선 굳이 사용하지 않는다
                    using (var pooled = ListPool<Vector2Int>.Get(out var pathList))
                    {
                        if (main.placeM.Gen_ASharpPathFinder.Execute_PathFinding(main, pathList, final_Start, final_End, maxHallwayWidth, 1, main.Setting.Hallawy.HallwaySafeAreaLength, false, false, main.Setting.Hallawy.HallwayHeuristicType, main.Setting.Hallawy.HallwayHeuristicFactor, clampBottomLeft, clampTopRight))
                        {
                            var correctionHallwayGridList = await TryGenerate_HallwaysAsync(main, pathList, true, mainHallwayWidth, maxHallwayWidth, false, false);
                            if (correctionHallwayGridList != null)
                            {
                                door.SetEnable_HallwayCorrectionGrids(correctionHallwayGridList);
                                return true;
                            }
                        }
                    }


                    //! 위 조건에 불만족했을경우, 실패한다
                    return false;
                }



                /// <summary>
                /// 복도를 이어준다 (<b>도어A의 End</b>와 <b>도어B의 End</b>)
                /// </summary>
                private bool Hallway_DoorEnd_To_DoorEnd(StageGenerator main, RoomObject.Door doorA, RoomObject.Door doorB, bool overlap, int? isReconstructionCount)
                {
                    //! 이미 도어 보정거리가 생성되었다면, 실행하진 않지만 false를 반환하지 않고 true를 반환한다
                    if ((doorA.IsEnabledPathHallway || doorB.IsEnabledPathHallway) && !overlap) { return true; }


                    //? 이미 도어가 연결되었고 그 사이의 복도가생성되었지만, 이를 무시하고 새로 생성하려 할 경우
                    //! 현재 생성된 경로 복도 관련 태그들을 제거하고, 보유 그리드 정보를 초기화한다
                    if (overlap)
                    {
                        if (doorA.IsEnabledPathHallway)
                        {
                            doorA.SetDisable_HallwayPathGrids(false, GridManager.GridEvent_RemoveHallwayTags);
                        }
                        if (doorB.IsEnabledPathHallway)
                        {
                            doorB.SetDisable_HallwayPathGrids(false, GridManager.GridEvent_RemoveHallwayTags);
                        }
                    }



                    #region 변수 선언 (좌표, 복도 확장, 복도 너비)


                    //? 도어A와, 도어B의 그리드 좌표가 들어갈곳
                    Vector2Int finalPos_DoorA;
                    Vector2Int finalPos_DoorB;



                    //? 복도가 확장될때, 양수(우or상)로 확장될지 결정
                    bool hallwayExpand_Positive_Horizontal = false;
                    bool hallwayExpand_Positive_Vertical = false;



                    //. 복도의 기본 너비, 최대 너비를 구하기
                    main.Setting.Hallawy.GetHallwayWidths_FromWidth(
                        Mathf.Max(main.Setting.Hallawy.HallwayMinWidth,
                        doorA.DoorWidth,
                        doorB.DoorWidth),
                        out int mainHallwayWidth, out int maxHallwayWidth); //. 설정값의 최소 복도 너비, 도어A의 너비, 도어B의 너비 중, 가장 큰 값을 사용한다

                    int hallwayPathFindingWidth = maxHallwayWidth + main.Setting.Hallawy.CurrentHallwaySafeAreaLength; //. 복도너비


                    var stageOrigin = main.TransformM.StageCenterPositionTransform;


                    #endregion



                    #region 도어A,B의 중심점들을 얻기


                    //? 도어A 와 도어B의 보정거리 중심점들을 얻어온다

                    doorA.CenterGridPosition_End(out var doorA_IsDoubleCenter, out var doorA_Center1, out var doorA_Center2);
                    doorB.CenterGridPosition_End(out var doorB_IsDoubleCenter, out var doorB_Center1, out var doorB_Center2);
                    //doorA.CenterGridPositions_End_PlusDir(stageOrigin, out var doorA_IsDoubleCenter, out var doorA_Center1, out var doorA_Center2);
                    //doorB.CenterGridPositions_End_PlusDir(stageOrigin, out var doorB_IsDoubleCenter, out var doorB_Center1, out var doorB_Center2);


                    ////? 그리드 좌표 보정 적용
                    //doorA_Center1 -= main.Setting.SnapSetting.GridPositionCorrection;
                    //doorA_Center2 -= main.Setting.SnapSetting.GridPositionCorrection;
                    //doorB_Center1 -= main.Setting.SnapSetting.GridPositionCorrection;
                    //doorB_Center2 -= main.Setting.SnapSetting.GridPositionCorrection;


                    #region 그리드 변환연산이 변경되며 사용하지않는 연산
                    ////? 보정연산 (스테이지 부모 좌표에 따른 보정)
                    //var correction = main.TransformM.StageParentPositionCurrentCache;
                    //if (main.Setting.SnapSetting.SnapToGridCellCenter)
                    //{
                    //    if (correction.x > 0)
                    //    {
                    //        doorA_Center1.x -= 1;
                    //        doorA_Center2.x -= 1;
                    //        doorB_Center1.x -= 1;
                    //        doorB_Center2.x -= 1;
                    //    }

                    //    if (correction.y > 0)
                    //    {
                    //        doorA_Center1.y -= 1;
                    //        doorA_Center2.y -= 1;
                    //        doorB_Center1.y -= 1;
                    //        doorB_Center2.y -= 1;
                    //    }
                    //} 
                    #endregion


                    #endregion



                    #region 도어A,B의 중심점들을 통해 FinalPos, 복도 비대칭 확장 방향 정하기


                    //? 도어A와 도어B의 중심점들을 통해, finalPos를 구한다.
                    //? 복도의 너비를 비대칭으로 확장시켜야 할때, 확장되는 방향도 정한다

                    //. 복도의 너비를 비대칭으로 적절한 방향으로 확장시키는 방법은 2가지를 사용한다.

                    //. 방법A: 범위 지정 그리드 이벤트에게 비대칭 확장 맡기는 방법,
                    //.        hallwayExpand_Positive_Vertical와 hallwayExpand_Positive_Horizontal를 사용해 결정된다

                    //. 방법B: 중심점이 2개일때, 두 점을 잇는 선을 골라, 그 선을 중심으로 확장시키는 방법
                    //.        (좌측/좌측)으로 확장해야한다면, (좌측/좌측)에 위치한 선을 기준으로 삼고,
                    //.        (상단/상단)으로 확장해야한다면, (상단/상단)에 위치한 선을 기준으로 삼아,
                    //.        그 선을 기준으로 확장시켜, 비대칭으로 확장시키는 방법




                    //! 도어A,B 두개 모두 중심점이 1개
                    if (!doorA_IsDoubleCenter && !doorB_IsDoubleCenter)
                    {
                        //! 복도의 최대 너비가 "짝수"에다가,
                        //! 설정값에 복도의 확장이 Positive 상태인 경우,
                        //. 직접 그리드를 확장시킬떄 Positive되게끔 적용한다
                        if (maxHallwayWidth.IsEven() && main.Setting.Hallawy.HallwayExpandPositive)
                        {
                            hallwayExpand_Positive_Vertical = true;
                            hallwayExpand_Positive_Horizontal = true;
                        }

                        finalPos_DoorA = doorA_Center1;
                        finalPos_DoorB = doorB_Center1;
                    }

                    //! 도어A,B 중에 중심점이 2개 인 요소 존재
                    //. ( A2 B2 || A1 B2 || A2 B1 )
                    else
                    {
                        //? FinalPos를 지정할때, 중심점2개 중에서 (좌측/좌측) 을 사용해야 하는지를 지정한다
                        //. 복도의 최대 너비가 홀수라면, HallwayExpandPositive를 그대로 사용하고,
                        //. 복도의 최대 너비가 짝수라면, (상단/상단) 을 고정으로 사용한다
                        bool negative = !maxHallwayWidth.IsEven() ? !main.Setting.Hallawy.HallwayExpandPositive : false;

                        //? 도어A,B의 중심점을 negative 변수에 따라, (좌측/좌측)으로 확장할지, (상단/상단) 으로 확장할지 결정한다
                        finalPos_DoorA = Util.GetCenter_PositivePositions(doorA.DoorDirection.IsHorizontal(), doorA_Center1, doorA_Center2, negative);
                        finalPos_DoorB = Util.GetCenter_PositivePositions(doorB.DoorDirection.IsHorizontal(), doorB_Center1, doorB_Center2, negative);
                        //. 방법B 사용
                    }


                    #endregion



                    #region 복도 패스파인딩 연산


                    //! [중요] 연산 전, 시작 지점과 끝 지점이 "점유해제" 시킨다 (레이어 9999)
                    var startGrid = main.GridM.GetGrid(finalPos_DoorA);
                    var endGrid = main.GridM.GetGrid(finalPos_DoorB);
                    startGrid.SetOccupied(this, false, MAX_OCCUPIED_LAYER, true);
                    endGrid.SetOccupied(this, false, MAX_OCCUPIED_LAYER, true);


                    //? 복도 NavMesh연산, 지정
                    //! 이거최적화되잖아 MinMax연산 나중에하자
                    main.gridM.TryGet_GridPositionClamp(new Vector2Int(Mathf.Min(doorA.CurrentGrid_DoorEnd_Min.GridPositionFixed.x, doorB.CurrentGrid_DoorEnd_Min.GridPositionFixed.x), Mathf.Min(doorA.CurrentGrid_DoorEnd_Min.GridPositionFixed.y, doorB.CurrentGrid_DoorEnd_Min.GridPositionFixed.y)) - new Vector2Int(hallwayPathFindingWidth, hallwayPathFindingWidth), out var clamp_DownLeft, out var navMeshGrid_DownLeft);
                    main.gridM.TryGet_GridPositionClamp(new Vector2Int(Mathf.Max(doorA.CurrentGrid_DoorEnd_Max.GridPositionFixed.x, doorB.CurrentGrid_DoorEnd_Max.GridPositionFixed.x), Mathf.Max(doorA.CurrentGrid_DoorEnd_Max.GridPositionFixed.y, doorB.CurrentGrid_DoorEnd_Max.GridPositionFixed.y)) + new Vector2Int(hallwayPathFindingWidth, hallwayPathFindingWidth), out var clamp_UpRight, out var navMeshGrid_UpRight);



                    //? [ 도어A ]-[ 도어B ] 를 패스파인딩 한다
                    using var pooled_pathList = ListPool<Vector2Int>.Get(out var pathList);

                    bool success_DoorAtoB = main.placeM.Gen_ASharpPathFinder.Execute_PathFinding(main, pathList, finalPos_DoorA, finalPos_DoorB, hallwayPathFindingWidth, 1, main.Setting.Hallawy.HallwaySafeAreaLength, hallwayExpand_Positive_Horizontal, hallwayExpand_Positive_Vertical,
                       main.Setting.Hallawy.HallwayHeuristicType, main.Setting.Hallawy.HallwayHeuristicFactor, clamp_DownLeft, clamp_UpRight, (iGrids, cGrids) =>
                    {
                        //. == 생성에 실패했을때 실행되는 이벤트 (재구성) ==

                        //if (isReconstructionCount.HasValue)
                        //{
                        //    Debug.Log($"<color=yellow>재구성중에 뭔 재구성이야 {isReconstructionCount}</color>");
                        //    return false;
                        //}

                        isReconstructionCount = isReconstructionCount.HasValue ? isReconstructionCount.Value + 1 : 1;
                        //Debug.Log($"재구성 시작! 연산횟수 {isReconstructionCount}");

                        return Reconstruction_ConnectHallways(main, doorA, doorB, maxHallwayWidth, 1, iGrids, cGrids, isReconstructionCount.Value);

                        ////! 재구성에서 실행되었는데 실패한것이라면, 그냥 실패를 반환한다
                        //if (isReconstruction)
                        //{
                        //    main.logM.LogFailed_Place_Gen3_HallwayReconstruction_IsOverlapedReconstruction();
                        //    return false;
                        //}

                    }, doorA.GetDoorGrids, doorB.GetDoorGrids);
                    //! 실패했다면, 재구성하며, 재구성도 실패한다면 실패한다
                    //. 각 도어의 [Start-End]는 경로 확장 금지가 무시된다


                    //? [중요] 성공 여부와 관계없이, 패스파인딩이 끝나면 점유 상태를 되돌린다
                    startGrid.RemoveOccupied(this, MAX_OCCUPIED_LAYER);
                    endGrid.RemoveOccupied(this, MAX_OCCUPIED_LAYER);


                    //! 연결에 실패
                    if (!success_DoorAtoB)
                    {
                        return false;
                    } //. 중심점1끼리의 연결에 실패한 시점에서, 중심점2끼리 연결되어도 소용없으니, 그냥 실패로 처리한다 


                    #endregion



                    #region 패스 리스트를 기반으로, 복도 생성, 복도생성 성공시 문과 문을 이어주며 성공되며 완


                    //. pathList로 복도 생성
                    bool execute = TryGenerate_Hallways(main, pathList, false, mainHallwayWidth, maxHallwayWidth, hallwayExpand_Positive_Horizontal, hallwayExpand_Positive_Vertical, out var hallwayGridList);


                    if (!execute) { return false; } //! 여기서 실패하는건 지금 디버깅되고있지 않음 (그런적이 없기때문)


                    //. 여기까지 왔다면, 도어A,B의 복도 생성은 성공
                    doorA.SetEnable_HallwayPathGrids(hallwayGridList, navMeshGrid_DownLeft, navMeshGrid_UpRight);
                    doorB.SetEnable_HallwayPathGrids(hallwayGridList, navMeshGrid_DownLeft, navMeshGrid_UpRight);

                    #endregion



                    return true;
                }



                /// <summary>
                /// [비동기]복도를 이어준다 (<b>도어A의 End</b>와 <b>도어B의 End</b>)
                /// </summary>
                private async UniTask<bool> Hallway_DoorEnd_To_DoorEndAsync(StageGenerator main, RoomObject.Door doorA, RoomObject.Door doorB, bool overlap, int? isReconstructionCount)
                {
                    //! 이미 도어 보정거리가 생성되었다면, 실행하진 않지만 false를 반환하지 않고 true를 반환한다
                    if ((doorA.IsEnabledPathHallway || doorB.IsEnabledPathHallway) && !overlap) { return true; }


                    //? 이미 도어가 연결되었고 그 사이의 복도가생성되었지만, 이를 무시하고 새로 생성하려 할 경우
                    //! 현재 생성된 경로 복도 관련 태그들을 제거하고, 보유 그리드 정보를 초기화한다
                    if (overlap)
                    {
                        if (doorA.IsEnabledPathHallway)
                        {
                            doorA.SetDisable_HallwayPathGrids(false, GridManager.GridEvent_RemoveHallwayTags);
                        }
                        if (doorB.IsEnabledPathHallway)
                        {
                            doorB.SetDisable_HallwayPathGrids(false, GridManager.GridEvent_RemoveHallwayTags);
                        }
                    }
                    //if (overlap && doorA.DoorDirection == EDirection4.Right) { Debug.Log($"테스트 캇"); return false; }


                    #region 변수 선언 (좌표, 복도 확장, 복도 너비)


                    //? 도어A와, 도어B의 그리드 좌표가 들어갈곳
                    Vector2Int finalPos_DoorA;
                    Vector2Int finalPos_DoorB;



                    //? 복도가 확장될때, 양수(우or상)로 확장될지 결정
                    bool hallwayExpand_Positive_Horizontal = false;
                    bool hallwayExpand_Positive_Vertical = false;



                    //. 복도의 기본 너비, 최대 너비를 구하기
                    main.Setting.Hallawy.GetHallwayWidths_FromWidth(
                        Mathf.Max(main.Setting.Hallawy.HallwayMinWidth,
                        doorA.DoorWidth,
                        doorB.DoorWidth),
                        out int mainHallwayWidth, out int maxHallwayWidth); //. 설정값의 최소 복도 너비, 도어A의 너비, 도어B의 너비 중, 가장 큰 값을 사용한다

                    int hallwayPathFindingWidth = maxHallwayWidth + main.Setting.Hallawy.CurrentHallwaySafeAreaLength; //. 복도너비


                    var stageOrigin = main.TransformM.StageCenterPositionTransform;


                    #endregion



                    #region 도어A,B의 중심점들을 얻기


                    //? 도어A 와 도어B의 보정거리 중심점들을 얻어온다

                    doorA.CenterGridPosition_End(out var doorA_IsDoubleCenter, out var doorA_Center1, out var doorA_Center2);
                    doorB.CenterGridPosition_End(out var doorB_IsDoubleCenter, out var doorB_Center1, out var doorB_Center2);
                    //doorA.CenterGridPositions_End_PlusDir(stageOrigin, out var doorA_IsDoubleCenter, out var doorA_Center1, out var doorA_Center2);
                    //doorB.CenterGridPositions_End_PlusDir(stageOrigin, out var doorB_IsDoubleCenter, out var doorB_Center1, out var doorB_Center2);


                    ////? 그리드 좌표 보정 적용
                    //doorA_Center1 -= main.Setting.SnapSetting.GridPositionCorrection;
                    //doorA_Center2 -= main.Setting.SnapSetting.GridPositionCorrection;
                    //doorB_Center1 -= main.Setting.SnapSetting.GridPositionCorrection;
                    //doorB_Center2 -= main.Setting.SnapSetting.GridPositionCorrection;


                    #region 그리드 변환연산이 변경되며 사용하지않는 연산
                    ////? 보정연산 (스테이지 부모 좌표에 따른 보정)
                    //var correction = main.TransformM.StageParentPositionCurrentCache;
                    //if (main.Setting.SnapSetting.SnapToGridCellCenter)
                    //{
                    //    if (correction.x > 0)
                    //    {
                    //        doorA_Center1.x -= 1;
                    //        doorA_Center2.x -= 1;
                    //        doorB_Center1.x -= 1;
                    //        doorB_Center2.x -= 1;
                    //    }

                    //    if (correction.y > 0)
                    //    {
                    //        doorA_Center1.y -= 1;
                    //        doorA_Center2.y -= 1;
                    //        doorB_Center1.y -= 1;
                    //        doorB_Center2.y -= 1;
                    //    }
                    //} 
                    #endregion


                    #endregion



                    #region 도어A,B의 중심점들을 통해 FinalPos, 복도 비대칭 확장 방향 정하기


                    //? 도어A와 도어B의 중심점들을 통해, finalPos를 구한다.
                    //? 복도의 너비를 비대칭으로 확장시켜야 할때, 확장되는 방향도 정한다

                    //. 복도의 너비를 비대칭으로 적절한 방향으로 확장시키는 방법은 2가지를 사용한다.

                    //. 방법A: 범위 지정 그리드 이벤트에게 비대칭 확장 맡기는 방법,
                    //.        hallwayExpand_Positive_Vertical와 hallwayExpand_Positive_Horizontal를 사용해 결정된다

                    //. 방법B: 중심점이 2개일때, 두 점을 잇는 선을 골라, 그 선을 중심으로 확장시키는 방법
                    //.        (좌측/좌측)으로 확장해야한다면, (좌측/좌측)에 위치한 선을 기준으로 삼고,
                    //.        (상단/상단)으로 확장해야한다면, (상단/상단)에 위치한 선을 기준으로 삼아,
                    //.        그 선을 기준으로 확장시켜, 비대칭으로 확장시키는 방법




                    //! 도어A,B 두개 모두 중심점이 1개
                    if (!doorA_IsDoubleCenter && !doorB_IsDoubleCenter)
                    {
                        //! 복도의 최대 너비가 "짝수"에다가,
                        //! 설정값에 복도의 확장이 Positive 상태인 경우,
                        //. 직접 그리드를 확장시킬떄 Positive되게끔 적용한다
                        if (maxHallwayWidth.IsEven() && main.Setting.Hallawy.HallwayExpandPositive)
                        {
                            hallwayExpand_Positive_Vertical = true;
                            hallwayExpand_Positive_Horizontal = true;
                        }

                        finalPos_DoorA = doorA_Center1;
                        finalPos_DoorB = doorB_Center1;
                    }

                    //! 도어A,B 중에 중심점이 2개 인 요소 존재
                    //. ( A2 B2 || A1 B2 || A2 B1 )
                    else
                    {
                        //? FinalPos를 지정할때, 중심점2개 중에서 (좌측/좌측) 을 사용해야 하는지를 지정한다
                        //. 복도의 최대 너비가 홀수라면, HallwayExpandPositive를 그대로 사용하고,
                        //. 복도의 최대 너비가 짝수라면, (상단/상단) 을 고정으로 사용한다
                        bool negative = !maxHallwayWidth.IsEven() ? !main.Setting.Hallawy.HallwayExpandPositive : false;

                        //? 도어A,B의 중심점을 negative 변수에 따라, (좌측/좌측)으로 확장할지, (상단/상단) 으로 확장할지 결정한다
                        finalPos_DoorA = Util.GetCenter_PositivePositions(doorA.DoorDirection.IsHorizontal(), doorA_Center1, doorA_Center2, negative);
                        finalPos_DoorB = Util.GetCenter_PositivePositions(doorB.DoorDirection.IsHorizontal(), doorB_Center1, doorB_Center2, negative);
                        //. 방법B 사용
                    }


                    #endregion



                    #region 복도 패스파인딩 연산


                    //! [중요] 연산 전, 시작 지점과 끝 지점이 "점유해제" 시킨다 (레이어 9999)
                    var startGrid = main.GridM.GetGrid(finalPos_DoorA);
                    var endGrid = main.GridM.GetGrid(finalPos_DoorB);
                    startGrid.SetOccupied(this, false, MAX_OCCUPIED_LAYER, true);
                    endGrid.SetOccupied(this, false, MAX_OCCUPIED_LAYER, true);


                    //? 복도 NavMesh연산, 지정
                    //! 이거최적화되잖아 MinMax연산 나중에하자
                    main.gridM.TryGet_GridPositionClamp(new Vector2Int(Mathf.Min(doorA.CurrentGrid_DoorEnd_Min.GridPositionFixed.x, doorB.CurrentGrid_DoorEnd_Min.GridPositionFixed.x), Mathf.Min(doorA.CurrentGrid_DoorEnd_Min.GridPositionFixed.y, doorB.CurrentGrid_DoorEnd_Min.GridPositionFixed.y)) - new Vector2Int(hallwayPathFindingWidth, hallwayPathFindingWidth), out var clamp_DownLeft, out var navMeshGrid_DownLeft);
                    main.gridM.TryGet_GridPositionClamp(new Vector2Int(Mathf.Max(doorA.CurrentGrid_DoorEnd_Max.GridPositionFixed.x, doorB.CurrentGrid_DoorEnd_Max.GridPositionFixed.x), Mathf.Max(doorA.CurrentGrid_DoorEnd_Max.GridPositionFixed.y, doorB.CurrentGrid_DoorEnd_Max.GridPositionFixed.y)) + new Vector2Int(hallwayPathFindingWidth, hallwayPathFindingWidth), out var clamp_UpRight, out var navMeshGrid_UpRight);



                    //? [ 도어A ]-[ 도어B ] 를 패스파인딩 한다
                    using var pooled_pathList = ListPool<Vector2Int>.Get(out var pathList);

                    bool success_DoorAtoB = await main.placeM.Gen_ASharpPathFinder.Execute_PathFindingAsync(main, pathList, finalPos_DoorA, finalPos_DoorB, hallwayPathFindingWidth, 1, main.Setting.Hallawy.HallwaySafeAreaLength, hallwayExpand_Positive_Horizontal, hallwayExpand_Positive_Vertical,
                   main.Setting.Hallawy.HallwayHeuristicType, main.Setting.Hallawy.HallwayHeuristicFactor, clamp_DownLeft, clamp_UpRight, async (iGrids, cGrids) =>
                   {
                       //. == 생성에 실패했을때 실행되는 이벤트 (재구성) ==

                       //if (isReconstructionCount.HasValue)
                       //{
                       //    Debug.Log($"<color=yellow>재구성중에 뭔 재구성이야 {isReconstructionCount}</color>");
                       //    return false;
                       //}

                       isReconstructionCount = isReconstructionCount.HasValue ? isReconstructionCount.Value + 1 : 1;
                       //Debug.Log($"재구성 시작! 연산횟수 {isReconstructionCount}");
                       return await Reconstruction_ConnectHallwaysAsync(main, doorA, doorB, maxHallwayWidth, 1, iGrids, cGrids, isReconstructionCount.Value);
                       //return Reconstruction_ConnectHallwaysAsync(main, doorA, doorB, hallwayPathFindingWidth, 1, iGrids, cGrids, isReconstructionCount.Value);

                       ////! 재구성에서 실행되었는데 실패한것이라면, 그냥 실패를 반환한다
                       //if (isReconstruction)
                       //{
                       //    main.logM.LogFailed_Place_Gen3_HallwayReconstruction_IsOverlapedReconstruction();
                       //    return false;
                       //}

                   }, doorA.GetDoorGrids, doorB.GetDoorGrids);
                    //! 실패했다면, 재구성하며, 재구성도 실패한다면 실패한다
                    //. 각 도어의 [Start-End]는 경로 확장 금지가 무시된다


                    //? [중요] 성공 여부와 관계없이, 패스파인딩이 끝나면 점유 상태를 되돌린다
                    startGrid.RemoveOccupied(this, MAX_OCCUPIED_LAYER);
                    endGrid.RemoveOccupied(this, MAX_OCCUPIED_LAYER);


                    //! 연결에 실패
                    if (!success_DoorAtoB)
                    {
                        return false;
                    } //. 중심점1끼리의 연결에 실패한 시점에서, 중심점2끼리 연결되어도 소용없으니, 그냥 실패로 처리한다 


                    #endregion



                    #region 패스 리스트를 기반으로, 복도 생성, 복도생성 성공시 문과 문을 이어주며 성공되며 완


                    //. pathList로 복도 생성
                    var hallwayGridList = await TryGenerate_HallwaysAsync(main, pathList, false, mainHallwayWidth, maxHallwayWidth, hallwayExpand_Positive_Horizontal, hallwayExpand_Positive_Vertical);


                    if (hallwayGridList == null) { return false; } //! 여기서 실패하는건 지금 디버깅되고있지 않음 (그런적이 없기때문)


                    //. 여기까지 왔다면, 도어A,B의 복도 생성은 성공
                    doorA.SetEnable_HallwayPathGrids(hallwayGridList, navMeshGrid_DownLeft, navMeshGrid_UpRight);
                    doorB.SetEnable_HallwayPathGrids(hallwayGridList, navMeshGrid_DownLeft, navMeshGrid_UpRight);

                    #endregion



                    return true;
                }



                //? 복도 연결 재구성



                /// <summary>
                /// 복도 생성에 실패했다면, 재구성한다<br/>
                /// 충돌한 경로에 주위를 점유시키고,<br/>
                /// 충돌한 복도들을 재생성한뒤,<br/>
                /// 점유를 되돌리고, 다시 복도 생성을 시도한다
                /// </summary>
                /// <param name="main"></param>
                /// <param name="doorA"></param>
                /// <param name="doorB"></param>
                /// <param name="pathWidth"></param>
                /// <param name="pathHeight"></param>
                /// <param name="integrated_bannedMainPathList"></param>
                /// <param name="current_bannedMainPathList"></param>
                /// <returns></returns>
                private bool Reconstruction_ConnectHallways(StageGenerator main, RoomObject.Door doorA, RoomObject.Door doorB, int pathWidth, int pathHeight, List<Vector2Int> integrated_bannedMainPathList, List<Vector2Int> current_bannedMainPathList, int reconstructionCount)
                {
                    //. 이곳에서는 생성에 실패한 복도를 다시 생성하는것이 아닌,
                    //. 생성에 실패한 복도를 다시 생성할수 있게끔 충돌한 복도들을 재구성하는 메서드임

                    main.placeM.Reconstruction_ConnectHallways_ReportCount++;


                    main.logM.Log_Place_Gen3_HallwayReconstruction_Ready(doorA, doorB, reconstructionCount); //. 재구성 로깅 시작


                    ////. 도어A,B의 방 안전구역 확장 Rect 캐싱
                    //var doorA_ParentRoomRect = main.placeM.RoomRectsCacheM.GetCachedInstanced_RoomSafeAreaExpands[doorA.ParentRoomObject].Rect;
                    //var doorB_ParentRoomRect = main.placeM.RoomRectsCacheM.GetCachedInstanced_RoomSafeAreaExpands[doorB.ParentRoomObject].Rect;
                    if (!doorA.ParentRoomObject.TryGetInstanceRoomActivatedRectExpand(out var doorA_ParentRoomActivatedRect)) { return false; }
                    if (!doorB.ParentRoomObject.TryGetInstanceRoomActivatedRectExpand(out var doorB_ParentRoomActivatedRect)) { return false; }


                    //? 재구성 관련 작업 전, 도어A와 도어B를 이어주던 도중에 와버렸으니
                    //? 도어A와 도어B를 다시 [점유]시키고, 방의 해당방향 안전구역도 [점유]시킨다
                    //. 이곳에서 도어A와 도어B를 이어주는 복도를 생성하는것이 아니기 때문에, 다시 [점유]시켜버린다
                    ControlGrids_RoomSafeAreaExpand_SelectSingleDirectionWithOutDoor(main, doorA.ParentRoomObject, doorA_ParentRoomActivatedRect, doorA.DoorDirection, TEMP_OCCUPIED_LAYER, true, true);
                    ControlGrids_RoomSafeAreaExpand_SelectSingleDirectionWithOutDoor(main, doorB.ParentRoomObject, doorB_ParentRoomActivatedRect, doorB.DoorDirection, TEMP_OCCUPIED_LAYER, true, true);
                    ControlGrids_Door(main, doorA.ParentRoomObject, doorA, TEMP_OCCUPIED_LAYER, true, true);
                    ControlGrids_Door(main, doorB.ParentRoomObject, doorB, TEMP_OCCUPIED_LAYER, true, true);


                    //? 충돌한 경로를 확장한 그리드들을 저장하는 해시셋
                    using (var pooled_integrated_bannedMainPathList_Expand = HashSetPool<Grid>.Get(out var integrated_bannedMainPathList_Expand))
                    {
                        //. 경로의 너비/높이 중 가장 큰값을 기준으로 확장된다
                        int pathSize = Mathf.Max(pathWidth, pathHeight);


                        //? 복도 안전구역을 사용한다면, 크기 조정
                        if (main.Setting.Hallawy.UseCreateHallwaySafeArea)
                        {
                            //. 복도 안전구역의 크기만큼 더한다
                            //pathSize += main.Setting.Hallawy.HallwaySafeAreaLength * 4;
                            pathSize += main.Setting.Hallawy.CurrentDoorHallwayEdgeLength;
                        }


                        //if (reconstructionCount > 1)
                        //{
                        //pathSize *= reconstructionCount;
                        //}


                        //? 충돌한 도어A와 도어B의 좌하단, 우상단 지점을 구한다
                        //. 이 지점 안에서만 임시 점유 그리드가 활성화된다
                        main.gridM.TryGet_GridPositionClamp(
                          new Vector2Int(
                              Mathf.Min(doorA.CurrentGrid_DoorEnd_Min.GridPositionFixed.x, doorB.CurrentGrid_DoorEnd_Min.GridPositionFixed.x),
                              Mathf.Min(doorA.CurrentGrid_DoorEnd_Min.GridPositionFixed.y, doorB.CurrentGrid_DoorEnd_Min.GridPositionFixed.y)
                          ),
                          out var clamp_DownLeft, out var navMeshGrid_DownLeft);
                        main.gridM.TryGet_GridPositionClamp(
                            new Vector2Int(
                                Mathf.Max(doorA.CurrentGrid_DoorEnd_Max.GridPositionFixed.x, doorB.CurrentGrid_DoorEnd_Max.GridPositionFixed.x),
                                Mathf.Max(doorA.CurrentGrid_DoorEnd_Max.GridPositionFixed.y, doorB.CurrentGrid_DoorEnd_Max.GridPositionFixed.y)
                            ),
                            out var clamp_UpRight, out var navMeshGrid_UpRight);


                        //clamp_DownLeft -= new Vector2Int(pathSize, pathSize);
                        //clamp_UpRight += new Vector2Int(pathSize, pathSize);
                        //! 241230 다시 비활성화


                        //? 금지 경로 리스트를 순회한다
                        //? 금지 그리드에 확장된 그리드들까지 포함하여,
                        //? 그리드를 [점유]시키고, 그 그리드들을 해시셋에 추가한다
                        //. 받아온 금지 경로 리스트를 확장해 점유시켜버린다 (재구성으로 다시 생성되는 복도가 또 충돌하지 않게)
                        //. integrated_bannedMainPathList_Expand 에 추가된다
                        //. 이렇게 그리드 확장으로 초기에만 GridEvent_Expand를 사용하고, 이후는 integrated_bannedMainPathList_Expand로 관리한다
                        foreach (var banGridPos in integrated_bannedMainPathList)
                        {
                            main.gridM.GridEvent_Expand(main.gridM.GetGrid(banGridPos).GridPositionFixed, true, pathSize, pathSize, false, false, ban_CollisionHallwayGrids, integrated_bannedMainPathList_Expand);
                        }


                        main.logM.Log_Place_Gen3_HallwayReconstruction_PrintBannedGrids(integrated_bannedMainPathList_Expand); //. 재구성 로깅: 임시 금지 그리드 출력


                        //? 도어A와 도어B의 방으로 들어가, 그 방에있는 모든 방의 도어를 순회하고,
                        //? 그 순회한 도어들의 방으로부터 생긴 도어들도 순회하여,
                        //? (즉, 총 2번의 노드 이동으로 도어들을 가져온다)
                        //? 그 도어로부터 생성된 복도들과 금지 경로 리스트에 겹친 복도가 있다면,
                        //? 리스트에 추가한다
                        //. 충돌한 복도의 99.99%는 도어A의 방 또는 도어B의 방 이므로, 이곳에서 탐색하여 
                        //. integrated_bannedMainPathList_Expand 와 겹쳐져있는 도어를 이 리스트에 추가한다
                        //. 중복을 허용할 필요는 없기에 Hashset으로 선언해도되지만, 애초에 1-2개 정도 밖에 감지가 안될거같아 그냥 List로 사용
                        using (var pooled_doors_HaveCollisionHallways = ListPool<RoomObject.Door>.Get(out var doors_HaveCollisionHallways))
                        {
                            doors_HaveCollisionHallways.Capacity = doorA.ParentRoomObject.RoomDoorM.DoorTotalCount + doorB.ParentRoomObject.RoomDoorM.DoorTotalCount;


                            //. 도어A, 도어B 모두 탐색
                            getDoors_FromDoor4Direction(in integrated_bannedMainPathList_Expand, in doors_HaveCollisionHallways, doorA);
                            getDoors_FromDoor4Direction(in integrated_bannedMainPathList_Expand, in doors_HaveCollisionHallways, doorB);


                            //! 금지구역 확장과 겹치는 도어를 찾지 못했다면, 실패한다
                            //! 실패했다면, 점유해놨던걸 되돌려야하지만,
                            //! 어차피 이 이후 크로노브레이크 또는 절차적 생성 실패로 이어지기에 딱히 구현하지않음
                            if (doors_HaveCollisionHallways.Count == 0) { main.logM.LogFailed_Place_Gen3_HallwayReconstruction_Failure_NotFoundCollisionHallways(); return false; }


                            main.logM.Log_Place_Gen3_HallwayReconstruction_PrintDoorsHaveCollisionHallways(doors_HaveCollisionHallways); //. 재구성 로깅: 금지 그리드에 포함된 도어 출력


                            //? 재구성할 필요가 있는 도어들을 순회한다
                            //. (점유된 금지 그리드를 피해 재구성 하기 위해)
                            foreach (var door in doors_HaveCollisionHallways)
                            {
                                //. 혹시라도 도어A, 도어B 본인이 들어왔다면 스킵
                                if (door == doorA || door == doorB) { continue; }


                                //? 도어가 보유하고있는 복도 그리드에 이벤트를 실행한다
                                //? ROOM_AREA_STATEND 태그를 가지고 있지 않다면,
                                //? 해당 그리드의 복도 그리드 EX설정들을 모두 비활성화한다
                                //! 도어의 정보에서 이미 생성된 복도를 초기화시키는건
                                //! Hallway_DoorEnd_To_DoorEnd의 overlap 파라미터에서 실행되기 때문에 여기서는 생성된 복도 관련 그리드들만 지운다 (태그)
                                door.HallwayPathGridEvent(disableHallwayGridTags_OnlyPathHallway);


                                main.logM.Log_Place_Gen3_HallwayReconstruction_PrintNewHallwaysByDoor(door, door.ConnectecDoor); //. 금지 그리드에 겹쳐 새로 생성될 도어 복도 정보 출력


                                //? 새롭게 다시 복도를 생성하기위해, 충돌한 도어의 방향에 있는 방 안전구역 확장의 [점유해제] 시킨다 (도어 제외)
                                //! 250808 추가
                                if (!door.ParentRoomObject.TryGetInstanceRoomActivatedRectExpand(out var door_InstanceRoomActivatedRectExpand)) { continue; }
                                ControlGrids_RoomSafeAreaExpand_SelectSingleDirectionWithOutDoor(main, door.ParentRoomObject, door_InstanceRoomActivatedRectExpand, door.DoorDirection, TEMP_OCCUPIED_LAYER, false, true);


                                //? 연결된 도어의 방향에 있는 방 안전구역 확장을 [점유해제] 하고, 연결된 도어를 [점유해제] 시킨다
                                ControlGrids_CreateHallway_Before(main, door, door.ConnectecDoor);


                                //? 다시 복도를 재생성한다!
                                //! 또 실패한다면, 임시로 점유시켜놨던 그리드를 점유 해제하고, 다시 생성을 시도한다
                                //! 재생성의 재생성이 또 실패하면 그대로 실패한다
                                #region 이전 주석
                                ////! 여기서 복도 재생성에 실패했다면, 그냥 실패한다
                                ////! 실패했다면, 점유해놨던걸 되돌려야하지만, 어차피 이 이후 크로노브레이크 또는 절차적 생성 실패로 이어지기에
                                ////! 딱히 구현하지않음 
                                #endregion
                                
                                if (!Hallway_DoorEnd_To_DoorEnd(main, door, door.ConnectecDoor, true, reconstructionCount))
                                {
                                    //? 임시 점유구역 [점유해제]
                                    unban_CollisionHallwayGrids_ByGrids(main.gridM, integrated_bannedMainPathList_Expand);

                                    if (!Hallway_DoorEnd_To_DoorEnd(main, door, door.ConnectecDoor, true, reconstructionCount + 1))
                                    {
                                        //! 재생성의 재생성이 또 실패하면 그대로 실패한다
                                        main.logM.LogFailed_Place_Gen3_HallwayReconstruction_Failure_ReconstructionCollisionNewHallways(door, door.ConnectecDoor);
                                        return false;
                                    }

                                    //? 임시 점유구역 [점유]
                                    ban_CollisionHallwayGrids_ByGrids(main.gridM, integrated_bannedMainPathList_Expand);
                                }


                                //? 연결된 도어의 방향에 있는 방 안전구역 확장을 다시 [점유] 하고, 연결된 도어를 다시 [점유] 시킨다
                                ControlGrids_CreateHallway_After(main, door, door.ConnectecDoor);


                                //? 새롭게 다시 복도를 재생성 했으니, 충돌한 도어의 방향에 있는 방 안전구역 확장을 다시  [점유] 시킨다 (도어 제외, 위에서 이미함)
                                ControlGrids_RoomSafeAreaExpand_SelectSingleDirectionWithOutDoor(main, door.ParentRoomObject, door_InstanceRoomActivatedRectExpand, door.DoorDirection, TEMP_OCCUPIED_LAYER, true, true);
                            }
                        }
                   
                        //? 금지 경로 리스트를 다시 순회하여, 
                        //? 점유시켰던 그리드를 취소한다                    
                        unban_CollisionHallwayGrids_ByGrids(main.gridM, integrated_bannedMainPathList_Expand);


                        //? 재구성 관련 작업이 모두 끝났으니 , 도어A와 도어B를 다시 이어주기 위해
                        //? 도어A와 도어B를 다시 [점유해제] 시키고, 방의 해당방향 안전구역도 [점유해제] 시킨다
                        //. 이곳에서 도어A와 도어B를 이어주는 복도를 생성하는것이 아니기 때문에, 다시 점유시켜버린다
                        ControlGrids_Door(main, doorA.ParentRoomObject, doorA, TEMP_OCCUPIED_LAYER, false, true);
                        ControlGrids_Door(main, doorB.ParentRoomObject, doorB, TEMP_OCCUPIED_LAYER, false, true);
                        ControlGrids_RoomSafeAreaExpand_SelectSingleDirectionWithOutDoor(main, doorA.ParentRoomObject, doorA_ParentRoomActivatedRect, doorA.DoorDirection, TEMP_OCCUPIED_LAYER, false, true);
                        ControlGrids_RoomSafeAreaExpand_SelectSingleDirectionWithOutDoor(main, doorB.ParentRoomObject, doorB_ParentRoomActivatedRect, doorB.DoorDirection, TEMP_OCCUPIED_LAYER, false, true);


                        main.logM.Log_Place_Gen3_HallwayReconstruction_Success(); //. 재구성 로깅 성공


                        return true;


                        //? 보정 복도 관련 그디그 아닌 복도 경로 그리드를 초기화시킨다
                        void disableHallwayGridTags_OnlyPathHallway(Grid g)
                        {
                            //! #1 보정 복도에 포함되어있지 않은 복도 그리드들의 정보를 제거한다
                            if (!g.AnyTag(GridTag.Room_Door_Area_StartEnd | GridTag.Hallway_Safe_DoorCorrection))
                            {
                                main.Setting.StageGrid.GridTagSetting_HallwayMain.DisableGrid(main.gridM, g);
                                main.Setting.StageGrid.GridTagSetting_HallwayExpand.DisableGrid(main.gridM, g);
                                main.Setting.StageGrid.GridTagSetting_HallwayEdge.DisableGrid(main.gridM, g);
                                main.Setting.StageGrid.GridTagSetting_HallwaySafe.DisableGrid(main.gridM, g);
                            }

                            //! #2 보정 복도에 포함되어있지만, 경로 복도에 의해 겹쳐진 "복도"만 제거한다
                            else
                            {
                                if (g.AnyTag(GridTag.Hallway_Edge))
                                {
                                    main.Setting.StageGrid.GridTagSetting_HallwayMain.DisableGrid(main.gridM, g);
                                    main.Setting.StageGrid.GridTagSetting_HallwayExpand.DisableGrid(main.gridM, g);

                                    if (!g.AnyTag(GridTag.Hallway_Edge_DoorCorrection))
                                    {
                                        main.Setting.StageGrid.GridTagSetting_HallwayEdge.DisableGrid(main.gridM, g);
                                    }
                                }
                                if (g.AnyTag(GridTag.Hallway_Safe))
                                {
                                    main.Setting.StageGrid.GridTagSetting_HallwayMain.DisableGrid(main.gridM, g);
                                    main.Setting.StageGrid.GridTagSetting_HallwayExpand.DisableGrid(main.gridM, g);

                                    if (!g.AnyTag(GridTag.Hallway_Safe_DoorCorrection))
                                    {
                                        main.Setting.StageGrid.GridTagSetting_HallwaySafe.DisableGrid(main.gridM, g);
                                    }
                                }
                            }
                        }


                        //? 복도 임시 점유 밴 (조건)
                        bool ban_CollisionHallwayGrids(GridManager gridM, Grid grid)
                        {
                            if ((grid.GridPositionFixed.x >= clamp_DownLeft.x && grid.GridPositionFixed.y >= clamp_DownLeft.y && grid.GridPositionFixed.x <= clamp_UpRight.x && grid.GridPositionFixed.y <= clamp_UpRight.y))
                            {
                                bool roomAContains = doorA.ParentRoomObject.CurrentGrids.Contains(grid);
                                bool roomBContains = doorB.ParentRoomObject.CurrentGrids.Contains(grid);


                                if (!(grid.AnyTag(GridTag.Room | GridTag.Room_Door_Area)) &&
                                    (
                                    (!(roomAContains || roomBContains) && !grid.AnyTag(GridTag.Room_Safe | GridTag.Room_ExpandSafe)) ||
                                    (roomAContains || roomBContains)
                                    ))
                                {
                                    grid.SetOccupied(true, MAX_OCCUPIED_LAYER, true);
                                    grid.AddTag(GridTag.WasBanned_CollisionOtherHallway);
                                    integrated_bannedMainPathList_Expand.Add(grid);
                                    return true;
                                }
                            }

                            return false;
                        }


                        //? 복도 임시 점유 밴해제 (무조건)
                        bool unban_CollisionHallwayGrids(GridManager gridM, Grid grid)
                        {
                            grid.RemoveOccupied(MAX_OCCUPIED_LAYER);
                            return true;
                        }


                        //? 그리드들로 복도 임시 점유 밴 (조건)
                        void ban_CollisionHallwayGrids_ByGrids(GridManager gridM, HashSet<Grid> grids)
                        {
                            foreach (var grid in grids)
                            {
                                ban_CollisionHallwayGrids(gridM, grid);
                            }
                        }


                        //? 그리드들로 복도 임시 점유 밴해제 (무조건)
                        void unban_CollisionHallwayGrids_ByGrids(GridManager gridM, HashSet<Grid> grids)
                        {
                            foreach (var grid in grids)
                            {
                                unban_CollisionHallwayGrids(gridM, grid);
                            }
                        }


                        //? 받아온 도어의 부모에서 모든 4방향 도어들을 순회하여, 도어로부터 생성된 복도와 금지 경로 리스트가 겹친다면, 그 도어들을 리스트에 추가시킨다
                        void getDoors_FromDoor4Direction(in HashSet<Grid> integrated_bannedMainPathList_Expand, in List<RoomObject.Door> doors_HaveCollisionHallways, RoomObject.Door baseDoor)
                        {
                            getDoors_FromDoor(in integrated_bannedMainPathList_Expand, in doors_HaveCollisionHallways, baseDoor.ParentRoomObject.RoomDoorM.GetDoorList(EDirection4.Down), baseDoor);
                            getDoors_FromDoor(in integrated_bannedMainPathList_Expand, in doors_HaveCollisionHallways, baseDoor.ParentRoomObject.RoomDoorM.GetDoorList(EDirection4.Up), baseDoor);
                            getDoors_FromDoor(in integrated_bannedMainPathList_Expand, in doors_HaveCollisionHallways, baseDoor.ParentRoomObject.RoomDoorM.GetDoorList(EDirection4.Left), baseDoor);
                            getDoors_FromDoor(in integrated_bannedMainPathList_Expand, in doors_HaveCollisionHallways, baseDoor.ParentRoomObject.RoomDoorM.GetDoorList(EDirection4.Right), baseDoor);


                            //? 도어리스트로부터 도어를 얻는다
                            void getDoors_FromDoor(in HashSet<Grid> integrated_bannedMainPathList_Expand, in List<RoomObject.Door> doors_HaveCollisionHallways, List<RoomObject.Door> doorList, RoomObject.Door baseDoor)
                            {
                                //. 도어를 순회한다
                                foreach (var door in doorList)
                                {
                                    //? 받아온 도어와 같은경우 스킵한다
                                    if (baseDoor == door) { continue; }


                                    //. 해당 도어의 복도 그리드들을 순회한다
                                    foreach (var hallwayGrid in door.GetHallwayPathGridList)
                                    {
                                        //. 복도 그리드중에, 확장 금지 경로에 포함되어있다면, 해당 도어를 추가한다 (중복X), Hashset을 사용해도되지만 크기 자체가 작기때문에 무시
                                        if (integrated_bannedMainPathList_Expand.Contains(hallwayGrid))
                                        {
                                            if (!doors_HaveCollisionHallways.Contains(door))
                                            {
                                                doors_HaveCollisionHallways.Add(door);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }



                /// <summary>
                /// [비동기] 복도 생성에 실패했다면, 재구성한다<br/>
                /// 충돌한 경로에 주위를 점유시키고,<br/>
                /// 충돌한 복도들을 재생성한뒤,<br/>
                /// 점유를 되돌리고, 다시 복도 생성을 시도한다
                /// </summary>
                /// <param name="main"></param>
                /// <param name="doorA"></param>
                /// <param name="doorB"></param>
                /// <param name="pathWidth"></param>
                /// <param name="pathHeight"></param>
                /// <param name="integrated_bannedMainPathList"></param>
                /// <param name="current_bannedMainPathList"></param>
                /// <returns></returns>
                private async UniTask<bool> Reconstruction_ConnectHallwaysAsync(StageGenerator main, RoomObject.Door doorA, RoomObject.Door doorB, int pathWidth, int pathHeight, List<Vector2Int> integrated_bannedMainPathList, List<Vector2Int> current_bannedMainPathList, int reconstructionCount)
                {
                    //. 이곳에서는 생성에 실패한 복도를 다시 생성하는것이 아닌,
                    //. 생성에 실패한 복도를 다시 생성할수 있게끔 충돌한 복도들을 재구성하는 메서드임

                    main.placeM.Reconstruction_ConnectHallways_ReportCount++;


                    main.logM.Log_Place_Gen3_HallwayReconstruction_Ready(doorA, doorB, reconstructionCount); //. 재구성 로깅 시작


                    //. 도어A,B의 방 안전구역 확장 Rect 캐싱
                    //var doorA_ParentRoomRect = main.placeM.RoomRectsCacheM.GetCachedInstanced_RoomSafeAreaExpands[doorA.ParentRoomObject].Rect;
                    //var doorB_ParentRoomRect = main.placeM.RoomRectsCacheM.GetCachedInstanced_RoomSafeAreaExpands[doorB.ParentRoomObject].Rect;
                    if (!doorA.ParentRoomObject.TryGetInstanceRoomActivatedRectExpand(out var doorA_ParentRoomActivatedRect)) { return false; }
                    if (!doorB.ParentRoomObject.TryGetInstanceRoomActivatedRectExpand(out var doorB_ParentRoomActivatedRect)) { return false; }


                    //? 재구성 관련 작업 전, 도어A와 도어B를 이어주던 도중에 와버렸으니
                    //? 도어A와 도어B를 다시 [점유]시키고, 방의 해당방향 안전구역도 [점유]시킨다
                    //. 이곳에서 도어A와 도어B를 이어주는 복도를 생성하는것이 아니기 때문에, 다시 [점유]시켜버린다
                    ControlGrids_RoomSafeAreaExpand_SelectSingleDirectionWithOutDoor(main, doorA.ParentRoomObject, doorA_ParentRoomActivatedRect, doorA.DoorDirection, TEMP_OCCUPIED_LAYER, true, true);
                    ControlGrids_RoomSafeAreaExpand_SelectSingleDirectionWithOutDoor(main, doorB.ParentRoomObject, doorB_ParentRoomActivatedRect, doorB.DoorDirection, TEMP_OCCUPIED_LAYER, true, true);
                    ControlGrids_Door(main, doorA.ParentRoomObject, doorA, TEMP_OCCUPIED_LAYER, true, true);
                    ControlGrids_Door(main, doorB.ParentRoomObject, doorB, TEMP_OCCUPIED_LAYER, true, true);


                    //? 충돌한 경로를 확장한 그리드들을 저장하는 해시셋
                    using (var pooled_integrated_bannedMainPathList_Expand = HashSetPool<Grid>.Get(out var integrated_bannedMainPathList_Expand))
                    {
                        //. 경로의 너비/높이 중 가장 큰값을 기준으로 확장된다
                        int pathSize = Mathf.Max(pathWidth, pathHeight);


                        //? 복도 안전구역을 사용한다면, 크기 조정
                        if (main.Setting.Hallawy.UseCreateHallwaySafeArea)
                        {
                            //. 복도 안전구역의 크기만큼 더한다
                            //pathSize += main.Setting.Hallawy.HallwaySafeAreaLength * 4;
                            pathSize += main.Setting.Hallawy.CurrentDoorHallwayEdgeLength;
                        }


                        //if (reconstructionCount > 1)
                        //{
                        //pathSize *= reconstructionCount;
                        //}


                        //? 충돌한 도어A와 도어B의 좌하단, 우상단 지점을 구한다
                        //. 이 지점 안에서만 임시 점유 그리드가 활성화된다
                        main.gridM.TryGet_GridPositionClamp(
                          new Vector2Int(
                              Mathf.Min(doorA.CurrentGrid_DoorEnd_Min.GridPositionFixed.x, doorB.CurrentGrid_DoorEnd_Min.GridPositionFixed.x),
                              Mathf.Min(doorA.CurrentGrid_DoorEnd_Min.GridPositionFixed.y, doorB.CurrentGrid_DoorEnd_Min.GridPositionFixed.y)
                          ),
                          out var clamp_DownLeft, out var navMeshGrid_DownLeft);
                        main.gridM.TryGet_GridPositionClamp(
                            new Vector2Int(
                                Mathf.Max(doorA.CurrentGrid_DoorEnd_Max.GridPositionFixed.x, doorB.CurrentGrid_DoorEnd_Max.GridPositionFixed.x),
                                Mathf.Max(doorA.CurrentGrid_DoorEnd_Max.GridPositionFixed.y, doorB.CurrentGrid_DoorEnd_Max.GridPositionFixed.y)
                            ),
                            out var clamp_UpRight, out var navMeshGrid_UpRight);


                        //clamp_DownLeft -= new Vector2Int(pathSize, pathSize);
                        //clamp_UpRight += new Vector2Int(pathSize, pathSize);
                        //! 241230 다시 비활성화


                        //? 금지 경로 리스트를 순회한다
                        //? 금지 그리드에 확장된 그리드들까지 포함하여,
                        //? 그리드를 [점유]시키고, 그 그리드들을 해시셋에 추가한다
                        //. 받아온 금지 경로 리스트를 확장해 점유시켜버린다 (재구성으로 다시 생성되는 복도가 또 충돌하지 않게)
                        //. integrated_bannedMainPathList_Expand 에 추가된다
                        //. 이렇게 그리드 확장으로 초기에만 GridEvent_Expand를 사용하고, 이후는 integrated_bannedMainPathList_Expand로 관리한다
                        foreach (var banGridPos in integrated_bannedMainPathList)
                        {
                            main.gridM.GridEvent_Expand(main.gridM.GetGrid(banGridPos).GridPositionFixed, true, pathSize, pathSize, false, false, ban_CollisionHallwayGrids, integrated_bannedMainPathList_Expand);
                        }


                        main.logM.Log_Place_Gen3_HallwayReconstruction_PrintBannedGrids(integrated_bannedMainPathList_Expand); //. 재구성 로깅: 임시 금지 그리드 출력


                        //? 도어A와 도어B의 방으로 들어가, 그 방에있는 모든 방의 도어를 순회하고,
                        //? 그 순회한 도어들의 방으로부터 생긴 도어들도 순회하여,
                        //? (즉, 총 2번의 노드 이동으로 도어들을 가져온다)
                        //? 그 도어로부터 생성된 복도들과 금지 경로 리스트에 겹친 복도가 있다면,
                        //? 리스트에 추가한다
                        //. 충돌한 복도의 99.99%는 도어A의 방 또는 도어B의 방 이므로, 이곳에서 탐색하여 
                        //. integrated_bannedMainPathList_Expand 와 겹쳐져있는 도어를 이 리스트에 추가한다
                        //. 중복을 허용할 필요는 없기에 Hashset으로 선언해도되지만, 애초에 1-2개 정도 밖에 감지가 안될거같아 그냥 List로 사용
                        using (var pooled_doors_HaveCollisionHallways = ListPool<RoomObject.Door>.Get(out var doors_HaveCollisionHallways))
                        {
                            doors_HaveCollisionHallways.Capacity = doorA.ParentRoomObject.RoomDoorM.DoorTotalCount + doorB.ParentRoomObject.RoomDoorM.DoorTotalCount;

                            //. 도어A, 도어B 모두 탐색
                            getDoors_FromDoor4Direction(in integrated_bannedMainPathList_Expand, in doors_HaveCollisionHallways, doorA);
                            getDoors_FromDoor4Direction(in integrated_bannedMainPathList_Expand, in doors_HaveCollisionHallways, doorB);


                            //! 금지구역 확장과 겹치는 도어를 찾지 못했다면, 실패한다
                            //! 실패했다면, 점유해놨던걸 되돌려야하지만,
                            //! 어차피 이 이후 크로노브레이크 또는 절차적 생성 실패로 이어지기에 딱히 구현하지않음
                            if (doors_HaveCollisionHallways.Count == 0) { main.logM.LogFailed_Place_Gen3_HallwayReconstruction_Failure_NotFoundCollisionHallways(); return false; }


                            main.logM.Log_Place_Gen3_HallwayReconstruction_PrintDoorsHaveCollisionHallways(doors_HaveCollisionHallways); //. 재구성 로깅: 금지 그리드에 포함된 도어 출력


                            //? 재구성할 필요가 있는 도어들을 순회한다
                            //. (점유된 금지 그리드를 피해 재구성 하기 위해)
                            foreach (var door in doors_HaveCollisionHallways)
                            {
                                //. 혹시라도 도어A, 도어B 본인이 들어왔다면 스킵
                                if (door == doorA || door == doorB) { continue; }


                                //? 도어가 보유하고있는 복도 그리드에 이벤트를 실행한다
                                //? ROOM_AREA_STATEND 태그를 가지고 있지 않다면,
                                //? 해당 그리드의 복도 그리드 EX설정들을 모두 비활성화한다
                                //! 도어의 정보에서 이미 생성된 복도를 초기화시키는건
                                //! Hallway_DoorEnd_To_DoorEnd의 overlap 파라미터에서 실행되기 때문에 여기서는 생성된 복도 관련 그리드들만 지운다 (태그)
                                door.HallwayPathGridEvent(disableHallwayGridTags_OnlyPathHallway);
                              

                                main.logM.Log_Place_Gen3_HallwayReconstruction_PrintNewHallwaysByDoor(door, door.ConnectecDoor); //. 금지 그리드에 겹쳐 새로 생성될 도어 복도 정보 출력


                                //? 새롭게 다시 복도를 생성하기위해, 충돌한 도어의 방향에 있는 방 안전구역 확장의 [점유해제] 시킨다 (도어 제외)
                                //! 250808 추가
                                if (!door.ParentRoomObject.TryGetInstanceRoomActivatedRectExpand(out var door_InstanceRoomActivatedRectExpand)) { continue; }
                                ControlGrids_RoomSafeAreaExpand_SelectSingleDirectionWithOutDoor(main, door.ParentRoomObject, door_InstanceRoomActivatedRectExpand, door.DoorDirection, TEMP_OCCUPIED_LAYER, false, true);


                                //? 연결된 도어의 방향에 있는 방 안전구역 확장을 [점유해제] 하고, 연결된 도어를 [점유해제] 시킨다
                                ControlGrids_CreateHallway_Before(main, door, door.ConnectecDoor);


                                //? 다시 복도를 재생성한다!
                                //! 또 실패한다면, 임시로 점유시켜놨던 그리드를 점유 해제하고, 다시 생성을 시도한다
                                //! 재생성의 재생성이 또 실패하면 그대로 실패한다
                                #region 이전 주석
                                ////! 여기서 복도 재생성에 실패했다면, 그냥 실패한다
                                ////! 실패했다면, 점유해놨던걸 되돌려야하지만, 어차피 이 이후 크로노브레이크 또는 절차적 생성 실패로 이어지기에
                                ////! 딱히 구현하지않음 
                                #endregion
                                if (!await Hallway_DoorEnd_To_DoorEndAsync(main, door, door.ConnectecDoor, true, reconstructionCount))
                                {
                                    //? 임시 점유구역 [점유해제]
                                    unban_CollisionHallwayGrids_ByGrids(main.gridM, integrated_bannedMainPathList_Expand);

                                    if (!await Hallway_DoorEnd_To_DoorEndAsync(main, door, door.ConnectecDoor, true, reconstructionCount + 1))
                                    {
                                        //! 재생성의 재생성이 또 실패하면 그대로 실패한다
                                        main.logM.LogFailed_Place_Gen3_HallwayReconstruction_Failure_ReconstructionCollisionNewHallways(door, door.ConnectecDoor);
                                        return false;
                                    }

                                    //? 임시 점유구역 [점유]
                                    ban_CollisionHallwayGrids_ByGrids(main.gridM, integrated_bannedMainPathList_Expand);
                                }

                                //? 연결된 도어의 방향에 있는 방 안전구역 확장을 다시 [점유] 하고, 연결된 도어를 다시 [점유] 시킨다
                                ControlGrids_CreateHallway_After(main, door, door.ConnectecDoor);


                                //? 새롭게 다시 복도를 재생성 했으니, 충돌한 도어의 방향에 있는 방 안전구역 확장을 다시  [점유] 시킨다 (도어 제외, 위에서 이미함)
                                ControlGrids_RoomSafeAreaExpand_SelectSingleDirectionWithOutDoor(main, door.ParentRoomObject, door_InstanceRoomActivatedRectExpand, door.DoorDirection, TEMP_OCCUPIED_LAYER, true, true);
                            }
                        }

                        //? 금지 경로 리스트를 다시 순회하여, 
                        //? 점유시켰던 그리드를 취소한다                    
                        unban_CollisionHallwayGrids_ByGrids(main.gridM, integrated_bannedMainPathList_Expand);


                        //? 재구성 관련 작업이 모두 끝났으니 , 도어A와 도어B를 다시 이어주기 위해
                        //? 도어A와 도어B를 다시 [점유해제] 시키고, 방의 해당방향 안전구역도 [점유해제] 시킨다
                        //. 이곳에서 도어A와 도어B를 이어주는 복도를 생성하는것이 아니기 때문에, 다시 점유시켜버린다
                        ControlGrids_Door(main, doorA.ParentRoomObject, doorA, TEMP_OCCUPIED_LAYER, false, true);
                        ControlGrids_Door(main, doorB.ParentRoomObject, doorB, TEMP_OCCUPIED_LAYER, false, true);
                        ControlGrids_RoomSafeAreaExpand_SelectSingleDirectionWithOutDoor(main, doorA.ParentRoomObject, doorA_ParentRoomActivatedRect, doorA.DoorDirection, TEMP_OCCUPIED_LAYER, false, true);
                        ControlGrids_RoomSafeAreaExpand_SelectSingleDirectionWithOutDoor(main, doorB.ParentRoomObject, doorB_ParentRoomActivatedRect, doorB.DoorDirection, TEMP_OCCUPIED_LAYER, false, true);


                        main.logM.Log_Place_Gen3_HallwayReconstruction_Success(); //. 재구성 로깅 성공


                        return true;


                        //? 보정 복도 관련 그디그 아닌 복도 경로 그리드를 초기화시킨다
                        void disableHallwayGridTags_OnlyPathHallway(Grid g)
                        {
                            //! #1 보정 복도에 포함되어있지 않은 복도 그리드들의 정보를 제거한다
                            if (!g.AnyTag(GridTag.Room_Door_Area_StartEnd | GridTag.Hallway_Safe_DoorCorrection))
                            {
                                main.Setting.StageGrid.GridTagSetting_HallwayMain.DisableGrid(main.gridM, g);
                                main.Setting.StageGrid.GridTagSetting_HallwayExpand.DisableGrid(main.gridM, g);
                                main.Setting.StageGrid.GridTagSetting_HallwayEdge.DisableGrid(main.gridM, g);
                                main.Setting.StageGrid.GridTagSetting_HallwaySafe.DisableGrid(main.gridM, g);
                            }

                            //! #2 보정 복도에 포함되어있지만, 경로 복도에 의해 겹쳐진 "복도"만 제거한다
                            else
                            {
                                if (g.AnyTag(GridTag.Hallway_Edge))
                                {
                                    main.Setting.StageGrid.GridTagSetting_HallwayMain.DisableGrid(main.gridM, g);
                                    main.Setting.StageGrid.GridTagSetting_HallwayExpand.DisableGrid(main.gridM, g);

                                    if (!g.AnyTag(GridTag.Hallway_Edge_DoorCorrection))
                                    {
                                        main.Setting.StageGrid.GridTagSetting_HallwayEdge.DisableGrid(main.gridM, g);
                                    }
                                }
                                if (g.AnyTag(GridTag.Hallway_Safe))
                                {
                                    main.Setting.StageGrid.GridTagSetting_HallwayMain.DisableGrid(main.gridM, g);
                                    main.Setting.StageGrid.GridTagSetting_HallwayExpand.DisableGrid(main.gridM, g);

                                    if (!g.AnyTag(GridTag.Hallway_Safe_DoorCorrection))
                                    {
                                        main.Setting.StageGrid.GridTagSetting_HallwaySafe.DisableGrid(main.gridM, g);
                                    }
                                }
                            }
                        }


                        //? 복도 임시 점유 밴 (조건)
                        bool ban_CollisionHallwayGrids(GridManager gridM, Grid grid)
                        {
                            if ((grid.GridPositionFixed.x >= clamp_DownLeft.x && grid.GridPositionFixed.y >= clamp_DownLeft.y && grid.GridPositionFixed.x <= clamp_UpRight.x && grid.GridPositionFixed.y <= clamp_UpRight.y))
                            {
                                bool roomAContains = doorA.ParentRoomObject.CurrentGrids.Contains(grid);
                                bool roomBContains = doorB.ParentRoomObject.CurrentGrids.Contains(grid);


                                if (!(grid.AnyTag(GridTag.Room | GridTag.Room_Door_Area)) &&
                                    (
                                    (!(roomAContains || roomBContains) && !grid.AnyTag(GridTag.Room_Safe | GridTag.Room_ExpandSafe)) ||
                                    (roomAContains || roomBContains)
                                    ))
                                {
                                    grid.SetOccupied(true, MAX_OCCUPIED_LAYER, true);
                                    grid.AddTag(GridTag.WasBanned_CollisionOtherHallway);
                                    integrated_bannedMainPathList_Expand.Add(grid);
                                    return true;
                                }
                            }

                            return false;
                        }


                        //? 복도 임시 점유 밴해제 (무조건)
                        bool unban_CollisionHallwayGrids(GridManager gridM, Grid grid)
                        {
                            grid.RemoveOccupied(MAX_OCCUPIED_LAYER);
                            return true;
                        }


                        //? 그리드들로 복도 임시 점유 밴 (조건)
                        void ban_CollisionHallwayGrids_ByGrids(GridManager gridM, HashSet<Grid> grids)
                        {
                            foreach (var grid in grids)
                            {
                                ban_CollisionHallwayGrids(gridM, grid);
                            }
                        }


                        //? 그리드들로 복도 임시 점유 밴해제 (무조건)
                        void unban_CollisionHallwayGrids_ByGrids(GridManager gridM, HashSet<Grid> grids)
                        {
                            foreach (var grid in grids)
                            {
                                unban_CollisionHallwayGrids(gridM, grid);
                            }
                        }


                        //? 받아온 도어의 부모에서 모든 4방향 도어들을 순회하여, 도어로부터 생성된 복도와 금지 경로 리스트가 겹친다면, 그 도어들을 리스트에 추가시킨다
                        void getDoors_FromDoor4Direction(in HashSet<Grid> integrated_bannedMainPathList_Expand, in List<RoomObject.Door> doors_HaveCollisionHallways, RoomObject.Door baseDoor)
                        {
                            getDoors_FromDoor(in integrated_bannedMainPathList_Expand, in doors_HaveCollisionHallways, baseDoor.ParentRoomObject.RoomDoorM.GetDoorList(EDirection4.Down), baseDoor);
                            getDoors_FromDoor(in integrated_bannedMainPathList_Expand, in doors_HaveCollisionHallways, baseDoor.ParentRoomObject.RoomDoorM.GetDoorList(EDirection4.Up), baseDoor);
                            getDoors_FromDoor(in integrated_bannedMainPathList_Expand, in doors_HaveCollisionHallways, baseDoor.ParentRoomObject.RoomDoorM.GetDoorList(EDirection4.Left), baseDoor);
                            getDoors_FromDoor(in integrated_bannedMainPathList_Expand, in doors_HaveCollisionHallways, baseDoor.ParentRoomObject.RoomDoorM.GetDoorList(EDirection4.Right), baseDoor);


                            //? 도어리스트로부터 도어를 얻는다
                            void getDoors_FromDoor(in HashSet<Grid> integrated_bannedMainPathList_Expand, in List<RoomObject.Door> doors_HaveCollisionHallways, List<RoomObject.Door> doorList, RoomObject.Door baseDoor)
                            {
                                //. 도어를 순회한다
                                foreach (var door in doorList)
                                {
                                    //? 받아온 도어와 같은경우 스킵한다
                                    if (baseDoor == door) { continue; }


                                    //. 해당 도어의 복도 그리드들을 순회한다
                                    foreach (var hallwayGrid in door.GetHallwayPathGridList)
                                    {
                                        //. 복도 그리드중에, 확장 금지 경로에 포함되어있다면, 해당 도어를 추가한다 (중복X), Hashset을 사용해도되지만 크기 자체가 작기때문에 무시
                                        if (integrated_bannedMainPathList_Expand.Contains(hallwayGrid))
                                        {
                                            if (!doors_HaveCollisionHallways.Contains(door))
                                            {
                                                doors_HaveCollisionHallways.Add(door);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }



                ///======================================================================================================================================================



                //? PathList를 기반으로 복도 생성 (복도, 복도 테두리, 복도 안전구역 등등)



                /// <summary>
                /// <paramref name="pathList"/>를 기반으로, 복도를 생성한다.<br/>
                /// 기본 복도, 복도 확장, 복도 테두리, 복도 안전구역 
                /// </summary>
                /// <param name="main"></param>
                /// <param name="pathList"></param>
                /// <param name="hallwayMaxWidth"></param>
                /// <param name="expandPositiveVertical"></param>
                /// <param name="expandPositiveHorizontal"></param>
                private bool TryGenerate_Hallways(StageGenerator main, IList<Vector2Int> pathList, bool isDoorCorrection, int hallwayMainWidth, int hallwayMaxWidth, bool expandPositiveVertical, bool expandPositiveHorizontal, out List<Grid> gridList)
                {
                    //. 커스텀이벤트: 복도 생성 이전
                    if (main.Setting.CustomEvent.PlaceHallway != null) { main.Setting.CustomEvent.PlaceHallway.Before_GenerateHallway_FromDoor(main, pathList, isDoorCorrection, hallwayMainWidth, hallwayMaxWidth, expandPositiveVertical, expandPositiveHorizontal); }


                    gridList = new List<Grid>();
                    hallwayMainWidth = Mathf.Max(1, hallwayMainWidth);


                    //? 기본 1x1 복도 생성 시도
                    bool create_Hallway = main.gridM.GridsEvent_PathList(pathList, false, true, 1, 1, false, false, main.Setting.StageGrid.GridTagSetting_HallwayMain.EnableGrid, gridList);
                    if (!create_Hallway) { return false; } //! 실패


                    //? 기본 복도 + 확장된 복도 너비 생성
                    bool create_Hallway_Expand = main.gridM.GridsEvent_PathList(pathList, false, true, hallwayMainWidth, 1, expandPositiveHorizontal, expandPositiveVertical, main.Setting.StageGrid.GridTagSetting_HallwayExpand.EnableGrid, gridList);
                    if (!create_Hallway_Expand) { return false; } //! 실패


                    //? 복도 테두리 생성 (내부)
                    if (!isDoorCorrection && main.Setting.Hallawy.UseCreateHallwayEdge)
                    {
                        bool create_Hallway_Edge;
                        create_Hallway_Edge = main.gridM.GridsEvent_PathList(pathList, false, true, hallwayMaxWidth, 1, expandPositiveHorizontal, expandPositiveVertical, main.Setting.StageGrid.GridTagSetting_HallwayEdge.EnableGrid, gridList); //main.placeM.PlaceObjects_HallwayEdge.GetPlacedGridList
                        if (!create_Hallway_Edge) { return false; } //! 실패
                    }


                    //? 도어 복도 테두리 생성
                    if (isDoorCorrection && main.Setting.Hallawy.UseCreateDoorHallwayEdge)
                    {
                        bool create_Hallway_Edge;
                        create_Hallway_Edge = main.gridM.GridsEvent_PathList(pathList, false, true, hallwayMaxWidth, 1, expandPositiveHorizontal, expandPositiveVertical, main.Setting.StageGrid.GridTagSetting_HallwayDoorCorrectionEdge.EnableGrid, gridList); //main.placeM.PlaceObjects_HallwayEdge.GetPlacedGridList
                        if (!create_Hallway_Edge) { return false; } //! 실패
                    }


                    //? 복도 테두리를 1칸 감싸는 복도 안전구역 생성
                    if (main.Setting.Hallawy.UseCreateHallwaySafeArea)
                    {
                        int hallway_SafeArea_Width = main.Setting.Hallawy.CurrentHallwaySafeAreaLength;

                        bool create_Hallway_SafeArea;
                        if (isDoorCorrection)
                        {
                            create_Hallway_SafeArea = main.gridM.GridsEvent_PathList(pathList, true, true, hallwayMaxWidth + hallway_SafeArea_Width, 1, expandPositiveHorizontal, expandPositiveVertical, main.Setting.StageGrid.GridTagSetting_HallwayDoorCorrectionSafe.EnableGrid, gridList); //main.placeM.gridList_CustomExpand
                        }
                        else
                        {
                            create_Hallway_SafeArea = main.gridM.GridsEvent_PathList(pathList, true, true, hallwayMaxWidth + hallway_SafeArea_Width, 1, expandPositiveHorizontal, expandPositiveVertical, main.Setting.StageGrid.GridTagSetting_HallwaySafe.EnableGrid, gridList); //main.placeM.gridList_CustomExpand
                        }

                        if (!create_Hallway_SafeArea) { return false; } //! 실패
                    }


                    //. 커스텀이벤트: 복도 생성 이후
                    if (main.Setting.CustomEvent.PlaceHallway != null) { main.Setting.CustomEvent.PlaceHallway.After_GenerateHallway_FromDoor(main, pathList, isDoorCorrection, hallwayMainWidth, hallwayMaxWidth, expandPositiveVertical, expandPositiveHorizontal, gridList); }


                    //. 여기까지 왔으면, 성공!
                    return true;
                }



                /// <summary>
                /// [비동기] <paramref name="pathList"/>를 기반으로, 복도를 생성한다.<br/>
                /// 기본 복도, 복도 확장, 복도 테두리, 복도 안전구역 
                /// </summary>
                /// <param name="main"></param>
                /// <param name="pathList"></param>
                /// <param name="hallwayMaxWidth"></param>
                /// <param name="expandPositiveVertical"></param>
                /// <param name="expandPositiveHorizontal"></param>
                private async UniTask<List<Grid>> TryGenerate_HallwaysAsync(StageGenerator main, IList<Vector2Int> pathList, bool isDoorCorrection, int hallwayMainWidth, int hallwayMaxWidth, bool expandPositiveVertical, bool expandPositiveHorizontal)
                {
                    //. 커스텀이벤트: 복도 생성 이전
                    await (main.Setting.CustomEvent.PlaceHallway != null ? main.Setting.CustomEvent.PlaceHallway.Before_GenerateHallway_FromDoorAsync(main, pathList, isDoorCorrection, hallwayMainWidth, hallwayMaxWidth, expandPositiveVertical, expandPositiveHorizontal) : UniTask.CompletedTask);


                    var gridList = new List<Grid>();
                    hallwayMainWidth = Mathf.Max(1, hallwayMainWidth);


                    //? 기본 1x1 복도 생성 시도
                    bool create_Hallway = main.gridM.GridsEvent_PathList(pathList, false, true, 1, 1, false, false, main.Setting.StageGrid.GridTagSetting_HallwayMain.EnableGrid, gridList);
                    if (!create_Hallway) { return null; } //! 실패


                    //? 기본 복도 + 확장된 복도 너비 생성
                    bool create_Hallway_Expand = main.gridM.GridsEvent_PathList(pathList, false, true, hallwayMainWidth, 1, expandPositiveHorizontal, expandPositiveVertical, main.Setting.StageGrid.GridTagSetting_HallwayExpand.EnableGrid, gridList);
                    if (!create_Hallway_Expand) { return null; } //! 실패


                    //? 복도 테두리 생성 (내부)
                    if (!isDoorCorrection && main.Setting.Hallawy.UseCreateHallwayEdge)
                    {
                        bool create_Hallway_Edge;
                        create_Hallway_Edge = main.gridM.GridsEvent_PathList(pathList, false, true, hallwayMaxWidth, 1, expandPositiveHorizontal, expandPositiveVertical, main.Setting.StageGrid.GridTagSetting_HallwayEdge.EnableGrid, gridList); //main.placeM.PlaceObjects_HallwayEdge.GetPlacedGridList
                        if (!create_Hallway_Edge) { return null; } //! 실패
                    }


                    //? 도어 복도 테두리 생성
                    if (isDoorCorrection && main.Setting.Hallawy.UseCreateDoorHallwayEdge)
                    {
                        bool create_Hallway_Edge;
                        create_Hallway_Edge = main.gridM.GridsEvent_PathList(pathList, false, true, hallwayMaxWidth, 1, expandPositiveHorizontal, expandPositiveVertical, main.Setting.StageGrid.GridTagSetting_HallwayDoorCorrectionEdge.EnableGrid, gridList); //main.placeM.PlaceObjects_HallwayEdge.GetPlacedGridList
                        if (!create_Hallway_Edge) { return null; } //! 실패
                    }


                    //? 복도 테두리를 1칸 감싸는 복도 안전구역 생성
                    if (main.Setting.Hallawy.UseCreateHallwaySafeArea)
                    {
                        int hallway_SafeArea_Width = main.Setting.Hallawy.CurrentHallwaySafeAreaLength;

                        bool create_Hallway_SafeArea;
                        if (isDoorCorrection)
                        {
                            create_Hallway_SafeArea = main.gridM.GridsEvent_PathList(pathList, true, true, hallwayMaxWidth + hallway_SafeArea_Width, 1, expandPositiveHorizontal, expandPositiveVertical, main.Setting.StageGrid.GridTagSetting_HallwayDoorCorrectionSafe.EnableGrid, gridList); //main.placeM.gridList_CustomExpand
                        }
                        else
                        {
                            create_Hallway_SafeArea = main.gridM.GridsEvent_PathList(pathList, true, true, hallwayMaxWidth + hallway_SafeArea_Width, 1, expandPositiveHorizontal, expandPositiveVertical, main.Setting.StageGrid.GridTagSetting_HallwaySafe.EnableGrid, gridList); //main.placeM.gridList_CustomExpand
                        }

                        if (!create_Hallway_SafeArea) { return null; } //! 실패
                    }


                    //. 커스텀이벤트: 복도 생성 이후
                    await (main.Setting.CustomEvent.PlaceHallway != null ? main.Setting.CustomEvent.PlaceHallway.After_GenerateHallway_FromDoorAsync(main, pathList, isDoorCorrection, hallwayMainWidth, hallwayMaxWidth, expandPositiveVertical, expandPositiveHorizontal, gridList) : UniTask.CompletedTask);


                    await UniTask.Yield();


                    //. 여기까지 왔으면, 성공!
                    return gridList;
                }



                ///======================================================================================================================================================



                //? 임시 그리드 설정



                /// <summary>
                /// 모든 방들을 순회하며 "단일 방향 안전구역 확장"에 그리드를 설정한다
                /// </summary>
                /// <param name="addCurrentGrids">활성화시, 해당 RoomObject에 해당 그리드를 보유할수 있게 추가한다</param>
                private static void ControlGridsLoop_RoomSafeAreaExpand_SingleDirection(StageGenerator main, IReadOnlyList<Space> spaceList, int layer, bool setOccupied, bool overlap, bool addCurrentGrids)
                {
                    foreach (var space in spaceList)
                    {
                        //! 방을 보유하고있지 않다면, continue
                        if (space.PlacedRoom == null) { continue; }


                        var roomObject = space.PlacedRoom;
                        //Rect roomRectSafeAreaExpand = main.placeM.RoomRectsCacheM.GetCachedInstanced_RoomSafeAreaExpands[roomObject].Rect;
                        #region 캐싱 미사용
                        //var roomRectSafeAreaExpand = roomObject.GetRoomSafeAreaExpandRect_BySpace(ESelectActivesMode.All, roomObject.CurrentSpace, out var offset1, main.gridM.Setting); 
                        #endregion                        
                        //! 250808 추가
                        if (!roomObject.TryGetInstanceRoomActivatedRectExpand(out var instanceRoomActivatedRectExpand)) { continue; }
                        var rect = roomObject.GridRect;
                        rect.position += instanceRoomActivatedRectExpand.Offset;
                        rect.ExpandFromCenterRef(instanceRoomActivatedRectExpand.Size);


                        // 해당 방의 "안전구역 확장" 에 그리드 이벤트를 실행한다
                        main.gridM.GridsEvent_Rect(false, rect, (grid) =>
                        {
                            //. 사실상 방의 "안전구역 확장" 내의 모든 그리드를 추가한다
                            if (addCurrentGrids) { roomObject.CurrentGrids.Add(grid); }


                            // 해당 그리드가 단일 방향의 방 안전구역 이라면, 점유설정을 한다
                            if (GridManager.CheckTag_RoomSafe_SingleDirection(grid))
                            {
                                grid.SetOccupied(roomObject, setOccupied, layer, overlap);
                            }

                            //. ※중요: "복도 연결을위한 임시 제어" 태그를 설정한다
                            if (setOccupied) { grid.AddTag(GridTag.TempControl_byConnectHallway); }
                            else { grid.RemoveTag(GridTag.TempControl_byConnectHallway); }
                        });
                    }
                }



                /// <summary>
                /// 모든 방들을 순회하며 "단일 방향 안전구역 확장"에 그리드 설정을 삭제한다
                /// </summary>
                private static void ControlRemoveGridsLoop_RoomSafeAreaExpand_SingleDirection(StageGenerator main, IReadOnlyList<Space> spaceList, int layer)
                {
                    foreach (var space in spaceList)
                    {
                        //! 방을 보유하고있지 않다면, continue
                        if (space.PlacedRoom == null) { continue; }


                        var roomObject = space.PlacedRoom;
                        //Rect roomRectSafeAreaExpand = main.placeM.RoomRectsCacheM.GetCachedInstanced_RoomSafeAreaExpands[roomObject].Rect;
                        #region 캐싱 미사용
                        //var roomRectSafeAreaExpand = roomObject.GetRoomSafeAreaExpandRect_BySpace(ESelectActivesMode.All, roomObject.CurrentSpace, out var offset1, main.gridM.Setting); 
                        #endregion
                        //! 250808 추가
                        if (!roomObject.TryGetInstanceRoomActivatedRectExpand(out var instanceRoomActivatedRectExpand)) { continue; }
                        var rect = roomObject.GridRect;
                        rect.position += instanceRoomActivatedRectExpand.Offset;
                        rect.ExpandFromCenterRef(instanceRoomActivatedRectExpand.Size);


                        // 해당 방의 "안전구역 확장" 에 그리드 이벤트를 실행한다
                        main.gridM.GridsEvent_Rect(false, rect, (grid) =>
                        {
                            // 해당 그리드가 단일 방향의 방 안전구역 이라면, 삭제한다
                            if (GridManager.CheckTag_RoomSafe_SingleDirection(grid))
                            {
                                grid.RemoveOccupied(roomObject, layer);
                            }

                            //. ※중요: "복도 연결을위한 임시 제어" 태그를 제거한다
                            grid.RemoveTag(GridTag.TempControl_byConnectHallway);
                        });
                    }
                }



                /// <summary>
                /// 대상 방에 속한 "안전 구역 확장" 안에,<br/>
                /// 도어를 제외하고, '방향'과 같은 안전구역만이 포함된 그리드들에게<br/>
                /// 그리드를 설정한다
                /// </summary>
                private static void ControlGrids_RoomSafeAreaExpand_SelectSingleDirectionWithOutDoor(StageGenerator main, RoomObject roomObject, in CustomRect2DRelative instanceRoomActivatedRectExpand, EDirection4 connectDirection, int layer, bool setOccupied, bool overlap)
                {
                    //! 250808 추가
                    var rect = roomObject.GridRect;
                    rect.position += instanceRoomActivatedRectExpand.Offset;
                    rect.ExpandFromCenterRef(instanceRoomActivatedRectExpand.Size);

                    main.gridM.GridsEvent_Rect(false, rect, ((grid) =>
                    {
                        if (grid.ContainsTag(GridTag.Room_Door_Area)) { return; }

                        if (connectDirection == EDirection4.Down && !GridManager.CheckTag_OnlyRoomSafeDown(grid) ||
                        connectDirection == EDirection4.Up && !GridManager.CheckTag_OnlyRoomSafeUp(grid) ||
                        connectDirection == EDirection4.Left && !GridManager.CheckTag_OnlyRoomSafeLeft(grid) ||
                        connectDirection == EDirection4.Right && !GridManager.CheckTag_OnlyRoomSafeRight(grid))
                        {
                            return;
                        }

                        grid.SetOccupied(roomObject, setOccupied, layer, overlap);

                        //. ※중요: "복도 연결을위한 임시 제어" 태그를 설정한다
                        if (setOccupied) { grid.AddTag(GridTag.TempControl_byConnectHallway); }
                        else { grid.RemoveTag(GridTag.TempControl_byConnectHallway); }
                    }));
                }



                /// <summary>
                /// 대상 방에 속한 "안전 구역 확장" 안에,<br/>
                /// 도어를 제외하고, '방향'과 같은 안전구역만이 포함된 그리드들의<br/>
                /// 그리드 설정을 제거한다
                /// </summary>
                private static void ControlRemoveGrids_RoomSafeAreaExpand_SelectSingleDirectionWithOutDoor(StageGenerator main, RoomObject roomObject, in Rect roomRectSafeAreaExpand, EDirection4 connectDirection, int layer)
                {
                    main.gridM.GridsEvent_Rect(false, roomRectSafeAreaExpand, ((grid) =>
                    {
                        if (grid.ContainsTag(GridTag.Room_Door_Area)) { return; }


                        if (connectDirection == EDirection4.Down && !GridManager.CheckTag_OnlyRoomSafeDown(grid) ||
                        connectDirection == EDirection4.Up && !GridManager.CheckTag_OnlyRoomSafeUp(grid) ||
                        connectDirection == EDirection4.Left && !GridManager.CheckTag_OnlyRoomSafeLeft(grid) ||
                        connectDirection == EDirection4.Right && !GridManager.CheckTag_OnlyRoomSafeRight(grid))
                        {
                            return;
                        }


                        grid.RemoveOccupied(roomObject, layer);


                        //. ※중요: "복도 연결을위한 임시 제어" 태그를 제거한다
                        grid.RemoveTag(GridTag.TempControl_byConnectHallway);
                    }));
                }



                /// <summary>
                /// 대상 도어 안에,<br/>
                /// 그리드를 설정한다
                /// </summary>
                private static void ControlGrids_Door(StageGenerator main, RoomObject roomObject, RoomObject.Door door, int layer, bool setOccupied, bool overlap)
                {
                    main.gridM.GridsEvent_Grids(door.GetDoorGrids, ((gridM, grid) =>
                    {
                        grid.SetOccupied(roomObject, setOccupied, layer, overlap);

                        //. ※중요: "복도 연결을위한 임시 제어" 태그를 설정한다
                        if (setOccupied) { grid.AddTag(GridTag.TempControl_byConnectHallway); }
                        else { grid.RemoveTag(GridTag.TempControl_byConnectHallway); }
                    }));
                }



                /// <summary>
                /// 대상 도어 안에,<br/>
                /// 그리드 설정을 제거한다
                /// </summary>
                private static void ControlRemoveGrids_Door(StageGenerator main, RoomObject roomObject, RoomObject.Door door, int layer)
                {
                    main.gridM.GridsEvent_Grids(door.GetDoorGrids, ((gridM, grid) =>
                    {
                        grid.RemoveOccupied(roomObject, layer);

                        //. ※중요: "복도 연결을위한 임시 제어" 태그를 제거한다
                        grid.RemoveTag(GridTag.TempControl_byConnectHallway);
                    }));
                }



                //? 도어와 도어를 연결하기 전/후 실행



                /// <summary>
                /// "현재 도어" 와 "연결 도어" 를 연결하기전(BEFORE) 실행되는 그리드 컨트롤 작업<br/>
                /// <paramref name="currentDoor"/>의 반대방향과에 있는 연결 방 Rect (안전구역확장)을 [점유해제] 시킨뒤 (도어 제외)<br/>
                /// "현재 도어" 를 [점유해제] 시킨다<br/>
                /// "연결 도어" 를 [점유해제] 시킨다<br/>
                /// </summary>
                /// <param name="main"></param>
                /// <param name="currentDoor"></param>
                /// <param name="connectingDoor"></param>
                /// <param name="connectingDoor_RoomActivatedRectExpand"></param>
                private static void ControlGrids_CreateHallway_Before(StageGenerator main, RoomObject.Door currentDoor, RoomObject.Door connectingDoor, CustomRect2DRelative? connectingDoor_RoomActivatedRectExpand = null)
                {
                    RoomObject roomObject = currentDoor.ParentRoomObject;
                    RoomObject connectingRoomObject = connectingDoor.ParentRoomObject;


                    ////. "연결 방" 의 RoomRect를 캐싱에서 얻어온다 (못받아왔다면)
                    //Rect _connectingRoomRectSafeAreaExpand = connectingDoor_RoomActivatedRectExpand.HasValue ? connectingDoor_RoomActivatedRectExpand.Value : _connectingRoomRectSafeAreaExpand = main.placeM.RoomRectsCacheM.GetCachedInstanced_RoomSafeAreaExpands[connectingDoor.ParentRoomObject].Rect;
                    var _connectingDoor_RoomActivatedRectExpand = connectingDoor_RoomActivatedRectExpand.HasValue ? connectingDoor_RoomActivatedRectExpand.Value : connectingDoor.ParentRoomObject.InstanceRoomActivatedRectExpand;


                    //? "연결 방"을 기준으로, 반대 연결방향에 있는 단일 방향 안전 구역 확장
                    //? 도어를 제외하고,
                    //? "반대 연결방향"에 있는 "단일 방향 안전구역 확장 구역"의 그리드들에게
                    //? [점유해제] 시킨다
                    ControlGrids_RoomSafeAreaExpand_SelectSingleDirectionWithOutDoor(main, connectingRoomObject, in _connectingDoor_RoomActivatedRectExpand, connectingDoor.DoorDirection, TEMP_OCCUPIED_LAYER, false, true);


                    //? "현재도어"를 "점유해제" 시킨다
                    ControlGrids_Door(main, roomObject, currentDoor, TEMP_OCCUPIED_LAYER, false, true);


                    //? "연결도어"를 "점유해제" 시킨다
                    ControlGrids_Door(main, connectingRoomObject, connectingDoor, TEMP_OCCUPIED_LAYER, false, true);
                }



                /// <summary>
                /// "현재 도어" 와 "연결 도어" 를 연결한후(AFTER) 실행되는 그리드 컨트롤 작업<br/>
                /// "연결 도어" 를 [점유] 시킨다<br/>
                /// "현재 도어" 를 [점유] 시킨다<br/>
                /// <paramref name="currentDoor"/>의 반대방향과에 있는 연결 방 Rect (안전구역확장)을 [점유] 시킨다 (도어 제외)<br/>
                /// </summary>
                /// <param name="main"></param>
                /// <param name="currentDoor"></param>
                /// <param name="connectingDoor"></param>
                /// <param name="connectingDoor_RoomActivatedRectExpand"></param>
                private static void ControlGrids_CreateHallway_After(StageGenerator main, RoomObject.Door currentDoor, RoomObject.Door connectingDoor, CustomRect2DRelative? connectingDoor_RoomActivatedRectExpand = null)
                {
                    RoomObject roomObject = currentDoor.ParentRoomObject;
                    RoomObject connectingRoomObject = connectingDoor.ParentRoomObject;


                    ////. "연결 방" 의 RoomRect를 캐싱에서 얻어온다 (못받아왔다면)
                    //Rect _connectingRoomRectSafeAreaExpand = (connectingDoor_RoomActivatedRectExpand.HasValue) ? connectingDoor_RoomActivatedRectExpand.Value : _connectingRoomRectSafeAreaExpand = main.placeM.RoomRectsCacheM.GetCachedInstanced_RoomSafeAreaExpands[connectingDoor.ParentRoomObject].Rect;
                    var _connectingDoor_RoomActivatedRectExpand = connectingDoor_RoomActivatedRectExpand.HasValue ? connectingDoor_RoomActivatedRectExpand.Value : connectingDoor.ParentRoomObject.InstanceRoomActivatedRectExpand;


                    //? "연결도어"를 [점유] 시킨다
                    ControlGrids_Door(main, connectingRoomObject, connectingDoor, TEMP_OCCUPIED_LAYER, true, true);


                    //? "현재도어"를 [점유] 시킨다
                    ControlGrids_Door(main, roomObject, currentDoor, TEMP_OCCUPIED_LAYER, true, true);


                    //? "연결 방"을 기준으로, 반대 연결방향에 있는 단일 방향 안전 구역 확장
                    //? 도어를 제외하고,
                    //? "반대 연결방향"에 있는 "단일 방향 안전구역 확장 구역"의 그리드들에게
                    //? [점유] 시킨다
                    ControlGrids_RoomSafeAreaExpand_SelectSingleDirectionWithOutDoor(main, connectingRoomObject, in _connectingDoor_RoomActivatedRectExpand, connectingDoor.DoorDirection, TEMP_OCCUPIED_LAYER, true, true);
                }



                ///======================================================================================================================================================
            }



            ///======================================================================================================================================================
        }
    }
}