using System.Collections.Generic;
using UnityEngine;
using Pan.Util;
using System;
using System.Text;
using Cysharp.Threading.Tasks;
using System.Threading;
using Pan.GridCompatibles2;
using Pan.StageGenerators;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine.Pool;



namespace Pan.StageGenerators
{
    public partial class StageGenerator
    {
        public partial class PlaceManger : BaseManager
        {
            ///======================================================================================================================================================



            ///<summary>
            ///방 생성기
            /// </summary>
            [Serializable]
            public class Generator1_RoomCreater
            {
                ///======================================================================================================================================================



                //? 생성



                /// <summary>
                /// 공간 리스트를 받아와, 각각 그 안에 적절한 방들을 생성한다 (가능하다면)
                /// </summary>
                public bool Generate(StageGenerator main, IReadOnlyList<Space> spaceList, Transform parent)
                {
                    main.GenerateM.GeneratingCurrentSteps = GenerateManager.GenerateSteps.GenStep3_Places_Gen1_CreateRooms;

                    //. 먼저, 방 트랜스폼 정보 캐싱을 무조건 초기화하고 시작한다
                    main.PlaceM.PlacedRoomTFInfoCacheM.Clear_PlacedRoomTransformInfoCache();


                    //? 커스텀이벤트: 방들 소환 이전
                    if (main.Setting.CustomEvent.SummonRoom != null) { main.Setting.CustomEvent.SummonRoom.Before_SummonRooms(main, spaceList, parent); }


                    //? 공간 안에 방을 생성한다
                    //!     하나라도 방 생성에 실패하면, 즉시 실패한다
                    if (!Execute_CreateRoomsInSpaces(main, parent, spaceList)) { return false; }


                    //? 방 인접 사용
                    if (main.Setting.Room.UseRoomPlaceNearest) { Execute_MoveNearestRooms(main, parent, spaceList); }


                    //? 배치가 모두 끝나면, 방들의 그리드이벤트를 실행
                    GridEvents_AllRooms(main, spaceList);


                    //? 커스텀이벤트: 방들 소환 이후
                    if (main.Setting.CustomEvent.SummonRoom != null) { main.Setting.CustomEvent.SummonRoom.After_SummonRooms(main, spaceList, parent); }


                    return true;
                }



                /// <summary>
                /// 공간을 받아와, 그 안에 적절한 방을 소환한다
                /// </summary>
                private bool Execute_CreateRoomsInSpaces(StageGenerator main, Transform parent, IReadOnlyList<Space> spaceList)
                {
                    //. 공간에 방을 생성하기 위한 정보가 들어있는 딕셔너리
                    // 기존: Dictionary<RoomObject, List<RoomObjectPlaceSpaceInfo>> roomInstanceInfos = new(spaceList.Count);
                    Dictionary<RoomObject, List<RoomObjectPlaceSpaceInfo>> roomInstanceInfos
                        = UnityEngine.Pool.DictionaryPool<RoomObject, List<RoomObjectPlaceSpaceInfo>>.Get(); //! 풀에서 획득

                    //. 생성된 방들의 정보를 저장하는 딕셔너리
                    // 기존: Dictionary<RoomObject, Stack<RoomObject>> instanceRoomStacks = new(spaceList.Count);
                    Dictionary<RoomObject, List<RoomObject>> instanceRoomLists
                        = UnityEngine.Pool.DictionaryPool<RoomObject, List<RoomObject>>.Get(); //! 풀에서 획득 (List를 LIFO로 사용)

                    try
                    {
                        //. 우선 전체 공간을 순회하면서, 대상 공간, 방, 방 활성화 Rect 정보가 들어있는 딕셔너리를 만든다
                        for (int i = 0; i < spaceList.Count; i++)
                        {
                            //. 공간에 방을 배치할수있도록 해본다
                            //!     공간 안에 방을 생성할수 있음에도, 생성하지 못했다면 실패한다
                            if (!SettingRoomInSpace(main, spaceList[i], roomInstanceInfos)) { return false; }
                        }

                        //. 그리고 방을 "무차별 생성" 하여 각 딕셔너리 스택(→리스트)에 넣는다
                        foreach (var item in roomInstanceInfos)
                        {
                            // 기존: Stack<RoomObject> stack = new(item.Value.Count);
                            var list = UnityEngine.Pool.ListPool<RoomObject>.Get(); //? 리스트 풀
                                                                                    //. 필요시 힌트
                            if (item.Value != null) { list.Capacity = item.Value.Count; } //. EnsureCapacity 대신 Capacity

                            for (int i = 0; i < item.Value.Count; i++)
                            {
                                //. 방을 무차별 생성하여 리스트 끝에 추가한다 (Push 대체)
                                list.Add(RoomObject.InstanceRoomObject(item.Key, parent));
                            }

                            instanceRoomLists.Add(item.Key, list);
                        }

                        //. 그릐고 방을 "배치 및 설정" 한다
                        foreach (var item in roomInstanceInfos)
                        {
                            for (int i = 0; i < item.Value.Count; i++)
                            {
                                //. 현재 방 생성 정보
                                var roomPlaceSpaceInfo = item.Value[i];

                                //. 현재 방 생성 정보에 의해 무차별 생성된 인스턴스 방을 하나 얻는다
                                // 기존: var instancedRoom = instanceRoomStacks[item.Key].Pop();
                                var bucket = instanceRoomLists[item.Key];
                                var lastIdx = bucket.Count - 1;
                                var instancedRoom = bucket[lastIdx];   //? Peek 대체
                                bucket.RemoveAt(lastIdx);              //? Pop 대체

                                //. 생성된 Room을 소환 위치로 이동
                                instancedRoom.transform.position = GetRoomSummonPosition(main, ref roomPlaceSpaceInfo, instancedRoom);

                                //! 복도 안전구역을 사용한다면, 생성된 해당 방에 사용중인 방향 쪽으로 확장시킨다!
                                //? 그냥 전방향 확장?
                                if (main.Setting.Hallawy.UseCreateHallwaySafeArea)
                                {
                                    instancedRoom.RoomSelfExpandSize_Down += main.Setting.Hallawy.HallwaySafeAreaLength;
                                    instancedRoom.RoomSelfExpandSize_Up += main.Setting.Hallawy.HallwaySafeAreaLength;
                                    instancedRoom.RoomSelfExpandSize_Left += main.Setting.Hallawy.HallwaySafeAreaLength;
                                    instancedRoom.RoomSelfExpandSize_Right += main.Setting.Hallawy.HallwaySafeAreaLength;
                                    //if (roomPlaceSpaceInfo.Space.HasNode_Down) { instancedRoom.RoomSelfExpandSize_Down += main.Setting.Hallawy.HallwaySafeAreaLength; }
                                    //if (roomPlaceSpaceInfo.Space.HasNode_Up) { instancedRoom.RoomSelfExpandSize_Up += main.Setting.Hallawy.HallwaySafeAreaLength; }
                                    //if (roomPlaceSpaceInfo.Space.HasNode_Left) { instancedRoom.RoomSelfExpandSize_Left += main.Setting.Hallawy.HallwaySafeAreaLength; }
                                    //if (roomPlaceSpaceInfo.Space.HasNode_Right) { instancedRoom.RoomSelfExpandSize_Right += main.Setting.Hallawy.HallwaySafeAreaLength; }
                                }

                                //. 배치된 방 트랜스폼 정보를 캐싱한다!
                                main.PlaceM.PlacedRoomTFInfoCacheM.Add_PlacedRoomTransformInfoCache(instancedRoom, instancedRoom.GetInstanceRoomActivatedTransformRectExpand(main.Setting));

                                //. 생성된 Room에 공간 지정
                                instancedRoom.CurrentSpace = roomPlaceSpaceInfo.Space;

                                //. 공간에 생성된 Room을 지정해준다
                                roomPlaceSpaceInfo.Space.PlacedRoom = instancedRoom;

                                //. 생성된 Room에 바리에이션을 무작위로 활성화한다
                                instancedRoom.RoomVariationM.EnableVariation_CustomRandom(main.GenerateInfoM.Random, true);

                                //. 방의 그리드 좌표 기준점을 스테이지 기준점으로 지정하기
                                instancedRoom.GridPositionCorrectionPlus -= main.TransformM.GetStageInstancePoint_GridPositionCorrection(true);
                                //instancedRoom.GridPositionCorrectionPlus -= new Vector2Int((int)main.TransformM.StageInstancePointCurrentCache.x, (int)main.TransformM.StageInstancePointCurrentCache.y);

                                //. Room을 성공적으로 소환했으니, RoomList에 추가한다
                                main.placeM.PlaceObjects_Room.AddSummonedObject(instancedRoom);

                                //? 커스텀 이벤트: 방 소환 이후
                                if (main.Setting.CustomEvent.SummonRoom != null) { main.Setting.CustomEvent.SummonRoom.After_SummonRoom(main, parent, instancedRoom, roomPlaceSpaceInfo.Space, spaceList); }
                            }
                        }
                    }
                    finally
                    {
                        //. 값으로 보관한 리스트/딕셔너리부터 반납 (LIFO 버킷)
                        foreach (var kv in instanceRoomLists)
                        {
                            kv.Value.Clear();
                            UnityEngine.Pool.ListPool<RoomObject>.Release(kv.Value); //! 리스트 반환
                        }
                        instanceRoomLists.Clear();
                        UnityEngine.Pool.DictionaryPool<RoomObject, List<RoomObject>>.Release(instanceRoomLists); //! 딕셔너리 반환

                        //. roomInstanceInfos (값: List<RoomObjectPlaceSpaceInfo>)도 모두 반납
                        foreach (var kv in roomInstanceInfos)
                        {
                            if (kv.Value != null)
                            {
                                kv.Value.Clear();
                                UnityEngine.Pool.ListPool<RoomObjectPlaceSpaceInfo>.Release(kv.Value); //! 리스트 반환
                            }
                        }
                        roomInstanceInfos.Clear();
                        UnityEngine.Pool.DictionaryPool<RoomObject, List<RoomObjectPlaceSpaceInfo>>.Release(roomInstanceInfos); //! 딕셔너리 반환
                    }

                    return true;
                }




                //? 비동기



                /// <summary>
                /// 공간 리스트를 받아와, 각각 그 안에 적절한 방들을 생성한다 (가능하다면)
                /// </summary>
                public async UniTask<bool> GenerateAsync(StageGenerator main, IReadOnlyList<Space> spaceList, Transform parent)
                {
                    main.GenerateM.GeneratingCurrentSteps = GenerateManager.GenerateSteps.GenStep3_Places_Gen1_CreateRooms;

                    //. 먼저, 방 트랜스폼 정보 캐싱을 무조건 초기화하고 시작한다
                    main.placeM.PlacedRoomTFInfoCacheM.Clear_PlacedRoomTransformInfoCache();


                    //? 커스텀이벤트: 방들 소환 이전
                    await (main.Setting.CustomEvent.SummonRoom != null ? main.Setting.CustomEvent.SummonRoom.Before_SummonRoomsAsync(main, spaceList, parent) : UniTask.CompletedTask);


                    //? 공간 안에 방을 생성한다
                    //!     하나라도 방 생성에 실패하면, 즉시 실패한다
                    if (!await Execute_CreateRoomsInSpacesAsync(main, parent, spaceList)) { return false; }


                    //? 방 인접 사용
                    if (main.Setting.Room.UseRoomPlaceNearest) { await Execute_MoveNearestRoomsAsync(main, parent, spaceList); }


                    //? 배치가 모두 끝나면, 방들의 그리드이벤트를 실행
                    GridEvents_AllRooms(main, spaceList);


                    //? 커스텀이벤트: 방들 소환 이후
                    await (main.Setting.CustomEvent.SummonRoom != null ? main.Setting.CustomEvent.SummonRoom.After_SummonRoomsAsync(main, spaceList, parent) : UniTask.CompletedTask);


                    return true;
                }



                /// <summary>
                /// [비동기] 공간을 받아와, 그 안에 적절한 방을 소환한다
                /// </summary>
                private async UniTask<bool> Execute_CreateRoomsInSpacesAsync(StageGenerator main, Transform parent, IReadOnlyList<Space> spaceList)
                {
                    //. 공간에 방을 생성하기 위한 정보가 들어있는 딕셔너리
                    Dictionary<RoomObject, List<RoomObjectPlaceSpaceInfo>> roomInstanceInfos
                        = UnityEngine.Pool.DictionaryPool<RoomObject, List<RoomObjectPlaceSpaceInfo>>.Get(); //! 풀에서 획득

                    //. 생성된 방들의 정보를 저장하는 딕셔너리 (비동기에서도 List-LIFO로 통일)
                    Dictionary<RoomObject, List<RoomObject>> instanceRoomLists
                        = UnityEngine.Pool.DictionaryPool<RoomObject, List<RoomObject>>.Get(); //! 풀에서 획득

                    //. 작업 리스트 빌리기
                    List<UniTask> tasks_InstanceRoom = UnityEngine.Pool.ListPool<UniTask>.Get();

                    try
                    {
                        //. 우선 전체 공간을 순회하면서, 대상 공간, 방, 방 활성화 Rect 정보가 들어있는 딕셔너리를 만든다
                        for (int i = 0; i < spaceList.Count; i++)
                        {
                            //. 공간에 방을 배치할수있도록 해본다
                            //!     공간 안에 방을 생성할수 있음에도, 생성하지 못했다면 실패한다
                            if (!SettingRoomInSpace(main, spaceList[i], roomInstanceInfos)) { return false; }
                        }

                        //. 그리고 방을 "비동기 무차별 생성" 하여 각 딕셔너리 리스트에 넣는다
                        foreach (var item in roomInstanceInfos)
                        {
                            tasks_InstanceRoom.Add(UniTask.RunOnThreadPool(async () =>
                            {
                                await UniTask.SwitchToMainThread();

                                //. 통째로 배열로 생성 (기존 API 사용) 후, List-LIFO 버킷으로 옮긴다
                                var instancedRooms = await RoomObject.InstanceRoomsObjectAsync(item.Key, item.Value.Count, parent);

                                var list = UnityEngine.Pool.ListPool<RoomObject>.Get(); //? 리스트 풀
                                list.Capacity = instancedRooms.Length; //. 힌트
                                list.AddRange(instancedRooms);         //. 배열 → 리스트로 복사 (LIFO 운용)

                                instanceRoomLists.Add(item.Key, list);
                                return;
                            }));
                        }
                        await UniTask.WhenAll(tasks_InstanceRoom);

                        //? 생성된 Room들을 모두 배치 & 설정
                        foreach (var item in roomInstanceInfos)
                        {
                            var bucket = instanceRoomLists[item.Key]; //. LIFO 버킷
                            for (int i = 0; i < item.Value.Count; i++)
                            {
                                //. 현재 방 생성 정보
                                var roomPlaceSpaceInfo = item.Value[i];

                                //. 현재 방 생성 정보에 의해 무차별 생성된 인스턴스 방을 하나 얻는다 (LIFO)
                                var lastIdx = bucket.Count - 1;
                                var instancedRoom = bucket[lastIdx];
                                bucket.RemoveAt(lastIdx);

                                //. 생성된 Room을 소환 위치로 이동
                                instancedRoom.transform.position = GetRoomSummonPosition(main, ref roomPlaceSpaceInfo, instancedRoom);

                                //! 복도 안전구역을 사용한다면, 생성된 해당 방에 사용중인 방향 쪽으로 확장시킨다!
                                //? 그냥 전방향 확장?
                                if (main.Setting.Hallawy.UseCreateHallwaySafeArea)
                                {
                                    instancedRoom.RoomSelfExpandSize_Down += main.Setting.Hallawy.HallwaySafeAreaLength;
                                    instancedRoom.RoomSelfExpandSize_Up += main.Setting.Hallawy.HallwaySafeAreaLength;
                                    instancedRoom.RoomSelfExpandSize_Left += main.Setting.Hallawy.HallwaySafeAreaLength;
                                    instancedRoom.RoomSelfExpandSize_Right += main.Setting.Hallawy.HallwaySafeAreaLength;
                                    //if (roomPlaceSpaceInfo.Space.HasNode_Down) { instancedRoom.RoomSelfExpandSize_Down += main.Setting.Hallawy.HallwaySafeAreaLength; }
                                    //if (roomPlaceSpaceInfo.Space.HasNode_Up) { instancedRoom.RoomSelfExpandSize_Up += main.Setting.Hallawy.HallwaySafeAreaLength; }
                                    //if (roomPlaceSpaceInfo.Space.HasNode_Left) { instancedRoom.RoomSelfExpandSize_Left += main.Setting.Hallawy.HallwaySafeAreaLength; }
                                    //if (roomPlaceSpaceInfo.Space.HasNode_Right) { instancedRoom.RoomSelfExpandSize_Right += main.Setting.Hallawy.HallwaySafeAreaLength; }
                                }

                                //. 배치된 방 트랜스폼 정보를 캐싱한다!
                                main.PlaceM.PlacedRoomTFInfoCacheM.Add_PlacedRoomTransformInfoCache(instancedRoom, instancedRoom.GetInstanceRoomActivatedTransformRectExpand(main.Setting));

                                //. 생성된 Room에 공간 지정
                                instancedRoom.CurrentSpace = roomPlaceSpaceInfo.Space;

                                //. 공간에 생성된 Room을 지정해준다
                                roomPlaceSpaceInfo.Space.PlacedRoom = instancedRoom;

                                //. 생성된 Room에 바리에이션을 무작위로 활성화한다
                                instancedRoom.RoomVariationM.EnableVariation_CustomRandom(main.GenerateInfoM.Random, true);

                                //. 방의 그리드 좌표 기준점을 스테이지 기준점으로 지정하기
                                instancedRoom.GridPositionCorrectionPlus -= main.TransformM.GetStageInstancePoint_GridPositionCorrection(true);
                                //instancedRoom.GridPositionCorrectionPlus -= new Vector2Int((int)main.TransformM.StageInstancePointCurrentCache.x, (int)main.TransformM.StageInstancePointCurrentCache.y);

                                //. Room을 성공적으로 소환했으니, RoomList에 추가한다
                                main.placeM.PlaceObjects_Room.AddSummonedObject(instancedRoom);

                                //? 커스텀 이벤트: 방 소환 이후
                                await (main.Setting.CustomEvent.SummonRoom != null ? main.Setting.CustomEvent.SummonRoom.After_SummonRoomAsync(main, parent, instancedRoom, roomPlaceSpaceInfo.Space, spaceList) : UniTask.CompletedTask);
                            }
                        }
                    }
                    finally
                    {
                        // 리스트/딕셔너리/작업리스트 반납 순서

                        //. instanceRoomLists 먼저
                        foreach (var kv in instanceRoomLists)
                        {
                            kv.Value.Clear();
                            UnityEngine.Pool.ListPool<RoomObject>.Release(kv.Value); //! 리스트 반환
                        }
                        instanceRoomLists.Clear();
                        UnityEngine.Pool.DictionaryPool<RoomObject, List<RoomObject>>.Release(instanceRoomLists); //! 딕셔너리 반환

                        //. roomInstanceInfos (값: List<RoomObjectPlaceSpaceInfo>) 반납
                        foreach (var kv in roomInstanceInfos)
                        {
                            if (kv.Value != null)
                            {
                                kv.Value.Clear();
                                UnityEngine.Pool.ListPool<RoomObjectPlaceSpaceInfo>.Release(kv.Value); //! 리스트 반환
                            }
                        }
                        roomInstanceInfos.Clear();
                        UnityEngine.Pool.DictionaryPool<RoomObject, List<RoomObjectPlaceSpaceInfo>>.Release(roomInstanceInfos); //! 딕셔너리 반환

                        //. 작업 리스트
                        tasks_InstanceRoom.Clear();
                        UnityEngine.Pool.ListPool<UniTask>.Release(tasks_InstanceRoom); //! 작업 리스트 반환
                        tasks_InstanceRoom = null; //. GC 루트 제거
                    }

                    return true;
                }



                ///======================================================================================================================================================



                //? 방 리스트 배치, 필터링, 무작위 선택



                /// <summary>
                /// 공간 안에 생성이 가능한 방 리스트를 필터링하여, 해당 공간 안에 방들의정보도 포함 되어있는 <see cref="RoomObjectPlaceSpaceInfo"/> 리스트를 얻는다
                /// <para>하나도 찾지 못했다면, 실패한다</para>
                /// </summary>
                private bool TryGetRoomList_Possible(StageGenerator main, Space space, in List<RoomObjectPlaceSpaceInfo> possibleRoomPlaceSpaceInfoList)
                {
                    var roomPrefabList = main.PrefabSetting.RoomPrefabList;

                    //. 방 프리팹을 순회하며, 조건에 맞는 방 프리팹을 리스트에 추가한다
                    for (int i = 0; i < roomPrefabList.Count; i++)
                    {
                        var roomPrefab = roomPrefabList[i];

                        //. 받아온 공간에 방 생성이 가능하다면, 리스트에 추가한다
                        if (roomPrefab.Confirm_PlaceSpaceInfo(main, main.Setting, space, out var roomActivatedRect, out var roomActivatedRectExpand))
                        {
                            //. 아직 소환 위치는 모르니, 중심점을 Vector2.zero로 등록한다
                            var placedActivatedRect = new CustomRect2DCentered(Vector2.zero, roomActivatedRect.Value);
                            var placedActivatedRectExpand = new CustomRect2DCentered(Vector2.zero, roomActivatedRectExpand.Value);

                            //! 복도 안전구역을 사용한다면, Rect를 방향 쪽으로 확장시킨다!
                            //? 그냥 전방향 확장?
                            if (main.Setting.Hallawy.UseCreateHallwaySafeArea)
                            {
                                placedActivatedRect.yMin -= main.Setting.Hallawy.HallwaySafeAreaLength;
                                placedActivatedRectExpand.yMin -= main.Setting.Hallawy.HallwaySafeAreaLength;

                                placedActivatedRect.yMax += main.Setting.Hallawy.HallwaySafeAreaLength;
                                placedActivatedRectExpand.yMax += main.Setting.Hallawy.HallwaySafeAreaLength;

                                placedActivatedRect.xMin -= main.Setting.Hallawy.HallwaySafeAreaLength;
                                placedActivatedRectExpand.xMin -= main.Setting.Hallawy.HallwaySafeAreaLength;

                                placedActivatedRect.xMax += main.Setting.Hallawy.HallwaySafeAreaLength;
                                placedActivatedRectExpand.xMax += main.Setting.Hallawy.HallwaySafeAreaLength;

                                //if (space.HasNode_Down)
                                //{
                                //    placedActivatedRect.yMin -= main.Setting.Hallawy.HallwaySafeAreaLength;
                                //    placedActivatedRectExpand.yMin -= main.Setting.Hallawy.HallwaySafeAreaLength;
                                //}
                                //if (space.HasNode_Up)
                                //{
                                //    placedActivatedRect.yMax += main.Setting.Hallawy.HallwaySafeAreaLength;
                                //    placedActivatedRectExpand.yMax += main.Setting.Hallawy.HallwaySafeAreaLength;
                                //}
                                //if (space.HasNode_Left)
                                //{
                                //    placedActivatedRect.xMin -= main.Setting.Hallawy.HallwaySafeAreaLength;
                                //    placedActivatedRectExpand.xMin -= main.Setting.Hallawy.HallwaySafeAreaLength;
                                //}
                                //if (space.HasNode_Right)
                                //{
                                //    placedActivatedRect.xMax += main.Setting.Hallawy.HallwaySafeAreaLength;
                                //    placedActivatedRectExpand.xMax += main.Setting.Hallawy.HallwaySafeAreaLength;
                                //}
                            }

                            possibleRoomPlaceSpaceInfoList.Add(new(space, roomPrefab, placedActivatedRect, placedActivatedRectExpand));
                        }
                    }

                    //. 생성 가능한 방의 개수가 1개 이상이라면 true, 1개도 없다면 false
                    return (possibleRoomPlaceSpaceInfoList.Count > 0);
                }



                /// <summary>
                /// 받아온 공간 안에 적절한 방을 배치 할수 있도록 한다
                /// </summary>
                private bool SettingRoomInSpace(StageGenerator main, Space space, Dictionary<RoomObject, List<RoomObjectPlaceSpaceInfo>> roomInstanceInfoDictionary)
                {
                    //? 해당 공간에 이미 생성된 방 있거나, 방을 생성할수 없는 공간이라면, true를 반환하고 그냥 넘어간다
                    if (space.PlacedRoom != null || !space.CanCreateRoom) { return true; }

                    //. 리스트풀에서 잠시 빌린다
                    List<RoomObjectPlaceSpaceInfo> possibleRoomPlaceSpaceInfoList = UnityEngine.Pool.ListPool<RoomObjectPlaceSpaceInfo>.Get();

                    try
                    {
                        //. 공간 안에 생성이 가능한 방 리스트를 필터링하여 얻는다
                        //!     1개도 찾지 못했다면, 실패한다
                        if (!TryGetRoomList_Possible(main, space, possibleRoomPlaceSpaceInfoList))
                        {
                            //! 공간 안에 방을 생성할수 있는 조건에도 불구하고, 방을 생성하지 못했을때의 에러
                            main.logM.LogError_Place_Gen1_NotFoundRoomFromSpace(main, space);
                            return false;
                        }

                        //. 가능한 방 리스트에서, 무작위 방을 선택한다
                        var randSelectedRoom = SelectRandRoom(main, possibleRoomPlaceSpaceInfoList);

                        //. 딕셔너리에 방의 생성 정보를 등록한다
                        if (roomInstanceInfoDictionary.ContainsKey(randSelectedRoom.RoomObjectPrefab))
                        {
                            roomInstanceInfoDictionary[randSelectedRoom.RoomObjectPrefab].Add(randSelectedRoom);
                        }
                        else
                        {
                            // 기존: roomInstanceInfoDictionary.Add(randSelectedRoom.RoomObjectPrefab, new List<RoomObjectPlaceSpaceInfo>());
                            var pooledList = UnityEngine.Pool.ListPool<RoomObjectPlaceSpaceInfo>.Get(); //! 풀에서 리스트 생성
                            roomInstanceInfoDictionary.Add(randSelectedRoom.RoomObjectPrefab, pooledList); //? 소유권: 호출자 메서드가 finally에서 Release 한다
                            pooledList.Add(randSelectedRoom);
                        }

                        return true;
                    }
                    finally
                    {
                        possibleRoomPlaceSpaceInfoList.Clear();
                        UnityEngine.Pool.ListPool<RoomObjectPlaceSpaceInfo>.Release(possibleRoomPlaceSpaceInfoList); //! 리스트풀 반환
                    }
                }



                /// <summary>
                /// 생성 가능한 방 리스트에서, 방을 무작위로 선택해 반환한다
                /// </summary>
                private RoomObjectPlaceSpaceInfo SelectRandRoom(StageGenerator main, List<RoomObjectPlaceSpaceInfo> possibleRoomPlaceSpaceInfoList)
                {
                    var rand = main.GenerateInfoM.Random;


                    //. 방 배치 공간 매치 우선이 활성화되어있을경우, 필터링된 방들 중에서 가장 큰 넓이의 방들 중에서 무작위로 선택한다
                    if (main.Setting.Room.UsePrioritizeMatchingRoomSize)
                    {
                        List<RoomObjectPlaceSpaceInfo> largestCandidates = UnityEngine.Pool.ListPool<RoomObjectPlaceSpaceInfo>.Get();

                        try
                        {
                            //. 입력 순서를 유지하며 최대 크기 후보를 한 번의 스캔으로 모은다
                            StageGenerationAlgorithms.CollectMaxCandidates(
                                possibleRoomPlaceSpaceInfoList,
                                largestCandidates,
                                x => x.RoomObjectPrefab.GridCompatible.ObjectSizeArea);

                            return largestCandidates[rand.Range(0, largestCandidates.Count)];
                        }
                        finally
                        {
                            largestCandidates.Clear();
                            UnityEngine.Pool.ListPool<RoomObjectPlaceSpaceInfo>.Release(largestCandidates);
                        }
                    }


                    //. 그 외에는 그냥 무작위 선택한다
                    return possibleRoomPlaceSpaceInfoList[rand.Range(0, possibleRoomPlaceSpaceInfoList.Count)];
                }



                ///======================================================================================================================================================



                //? 방 소환 좌표 연산



                /// <summary>
                ///  <see cref="RoomObject"/>가 소환될 위치를 정해 반환한다
                /// </summary>
                private Vector3 GetRoomSummonPosition(StageGenerator main, ref RoomObjectPlaceSpaceInfo roomPlaceSpaceInfo, RoomObject instancedRoom)
                {
                    //. 현재 배치 해야할 방의 Activated Room Rect 확장 얻기 (변형 예정, 값타입이니까 깊은복사)
                    var activatedRoomRect = roomPlaceSpaceInfo.ActivatedRoomRectExpand;


                    //. Activated Room Rect 확장 의 오프셋을 통해 "실제 트랜스폼 오프셋"을 구하기위해 GridUnit 연산한다
                    var offset_Transform = activatedRoomRect.Offset.Multiply(main.Setting.SnapSetting.GridUnitOriginalVector2);


                    //. 현재 배치해야할 공간의 트랜스폼 오리진 Rect 
                    //var spaceTransformRect = roomPlaceSpaceInfo.Space.SpaceTransformRect;
                    var spaceOriginTransformRect = roomPlaceSpaceInfo.Space.SpaceOriginTransformRect;


                    //. 공간의 정 중앙 Transform 좌표를 구하고
                    //.     "트랜스폼 오프셋" 만큼 빼서, Activated Room Rect에서 확장된 크기의 절반 만큼 감소된 위치에 배치되게한다
                    //.         이래야 Activated Room Rect의 기준의 중앙을 기준으로 배치된다
                    var spaceTransformCurrentCenter = spaceOriginTransformRect.center - offset_Transform;


                    float gridUnitXHalf = main.Setting.SnapSetting.GridUnitX_Width * 0.5f;
                    float gridUnitYHalf = main.Setting.SnapSetting.GridUnitY_Height * 0.5f;


                    //. Activated Room Rect 확장 의 중심점을 새로 지정한다 (그 전엔 그냥 깡통인 Vector2.zero로 저장되어있을거임)
                    //!                                                 (인접코드 바뀌어서 아닐수도있고)
                    //.     "공간의 중앙 좌표" (오프셋이 더해진) 를 "그리드 스냅 좌표"로 변환하여, 지정한다
                    activatedRoomRect.Center += main.Setting.SnapSetting.CalculateGridSnapPosition_byTransformPositionV2(spaceTransformCurrentCenter);  //activatedRoomRect.Center += spaceTransformCurrentCenter;
                                                                                                                                                        //activatedRoomRect.Center = main.Setting.SnapSetting.Calculate_TransformPositionV2_To_GridSnapTranformPosition(spaceTransformCurrentCenter, true);


                    //. 스냅 보정 연산 (왜하는지 모름 근데 해야됨...)
                    //if (main.Setting.SnapSetting.SnapToGridCellCenter)
                    activatedRoomRect.Center += new Vector2(gridUnitXHalf, gridUnitYHalf);


                    //. Activated Room Rect 확장의 오프셋도 지정한다
                    activatedRoomRect.Offset = offset_Transform;


                    //. Size도 Transform Size로 지정한다
                    activatedRoomRect.Size = activatedRoomRect.Size.Multiply(main.Setting.SnapSetting.GridUnitOriginalVector2);


                    //? 공간을 벗어나지 않는 범위 안에서, 무작위 위치에 배치되게끔 한다
                    if (main.Setting.Room.UseRoomPlaceNearest == false && main.Setting.Room.RoomPlaceRandomness > 0f)
                    {
                        float minX = spaceOriginTransformRect.xMin - (activatedRoomRect.xMin - gridUnitXHalf);
                        float maxX = spaceOriginTransformRect.xMax - (activatedRoomRect.xMax - gridUnitXHalf);
                        float minY = spaceOriginTransformRect.yMin - (activatedRoomRect.yMin - gridUnitYHalf);
                        float maxY = spaceOriginTransformRect.yMax - (activatedRoomRect.yMax - gridUnitYHalf);


                        //. 극단적이게 랜덤 (양끝만나옴)
                        //var _tempRandX = main.generateInfoM.Random.ValueBool() ? minX : maxX;
                        //var _tempRandY = main.generateInfoM.Random.ValueBool() ? minY : maxY;                        
                        //var tempRandX = Mathf.FloorToInt(_tempRandX / main.Setting.SnapSetting.GridUnitX_Width) * main.Setting.SnapSetting.GridUnitX_Width;
                        //var tempRandY = Mathf.FloorToInt(_tempRandY / main.Setting.SnapSetting.GridUnitY_Height) * main.Setting.SnapSetting.GridUnitY_Height;


                        float randX = main.generateInfoM.Random.RangeUnitFromMid(minX, maxX, main.Setting.SnapSetting.GridUnitX_Width, main.Setting.Room.RoomPlaceRandomness);
                        float randY = main.generateInfoM.Random.RangeUnitFromMid(minY, maxY, main.Setting.SnapSetting.GridUnitY_Height, main.Setting.Room.RoomPlaceRandomness);


                        //. 적용
                        //activatedRoomRect.Center += new Vector2(tempRandX, tempRandY);
                        activatedRoomRect.Center += new Vector2(randX, randY);
                    }


                    //. 생성된 방에, 연산되니 Activated Room Rect 를 할당한다
                    instancedRoom.ApplyInstanceRoomActivatedRect(roomPlaceSpaceInfo.ActivatedRoomRect.CustomRect2DRelative, roomPlaceSpaceInfo.ActivatedRoomRectExpand.CustomRect2DRelative);


                    //. 소환될 좌표를 Swizzle하여 실제 트랜스폼 좌표에 잘 배치될수 있게끔 변환하여 반환한다
                    Vector3 result = activatedRoomRect.Center;
                    result.z = main.TransformM.StageParentPositionCurrentCache.z + main.Setting.SnapSetting.FloorStandardPositionLength; //? 바닥 Z축 배치
                    result.SwizzlesVectorRef(main.Setting.SnapSetting.Swizzle);
                    return result;


                    #region Legacys
                    #region L
                    ////. 연산에 사용할 Rect들
                    //var spaceTransformRect = roomPlaceSpaceInfo.Space.SpaceTransformRect;
                    //var activatedRoomRect = roomPlaceSpaceInfo.ActivatedRoomRect;


                    ////. 공간의 정 중앙 Transform 좌표를 구한다
                    //var spaceTransformCurrentCenter = roomPlaceSpaceInfo.Space.SpaceStageGeneratorTransformCurrentCenter(main);
                    ////var spaceTransformCurrentCenter = spaceTransformRect.center;

                    //if (roomPlaceSpaceInfo.Space.SpaceIndex == 0)
                    //{
                    //    Debug.Log($"센타 {spaceTransformCurrentCenter} / {spaceTransformRect.center}");
                    //}

                    ////. 공간의 정 중앙 좌표를 그리드 스냅하여 얻기
                    //var spaceTransformCurrentCenterSnapped = main.Setting.SnapSetting.CalculateGridSnapTransformPositionVector3_bySnapSetting(spaceTransformCurrentCenter, true, true);
                    //Debug.LogWarning($"{spaceTransformCurrentCenter} >> {spaceTransformCurrentCenterSnapped}");

                    ////. Activated Room Rect의 오프셋을 통해 "실제 트랜스폼 오프셋"을 구하기위해 GridUnit 연산한다
                    //var transformOffset = activatedRoomRect.Offset.Multiply(main.Setting.SnapSetting.GridUnitOriginalVector2);


                    ////. 공간의 정 중앙에서, Activated Room Rect의 오프셋만큼 벡터를 빼서, Activated Room Rect를 기준으로 중심점에 배치될수 있도록 한다
                    //spaceTransformCurrentCenterSnapped -= transformOffset.SwizzlesVector2To3(main.Setting.SnapSetting.Swizzle);


                    ////? 공간을 벗어나지 않는 범위 안에서, 무작위 위치에 배치되게끔 한다
                    //if (main.Setting.Room.UseRoomPlaceNeareast == false && main.Setting.Room.RoomPlaceRandomness > 0f)
                    //{
                    //    Debug.Log("무작위배치 임시조치");

                    //    if (roomPlaceSpaceInfo.Space.SpaceIndex == 0)
                    //    {
                    //        //float minX = spaceRect.xMin - willApplyedActivatedRoomRect.xMin;
                    //        //float maxX = spaceRect.xMax - willApplyedActivatedRoomRect.xMax;
                    //        //float minY = spaceRect.yMin - willApplyedActivatedRoomRect.yMin;
                    //        //float maxY = spaceRect.yMax - willApplyedActivatedRoomRect.yMax;
                    //        float minX = spaceTransformRect.xMin - activatedRoomRect.xMin;
                    //        float maxX = spaceTransformRect.xMax - activatedRoomRect.xMax;
                    //        float minY = spaceTransformRect.yMin - activatedRoomRect.yMin;
                    //        float maxY = spaceTransformRect.yMax - activatedRoomRect.yMax;

                    //        //Debug.Log($"{spaceTransformCurrentCenter}");
                    //        Debug.Log($"{minX} ~ {maxX}");

                    //        //float randX = main.generateInfoM.Random.RangeUnitFromMid(minX, maxX, main.Setting.SnapSetting.GridUnitX_Width, main.Setting.Room.RoomPlaceRandomness);
                    //        //float randY = main.generateInfoM.Random.RangeUnitFromMid(minY, maxY, main.Setting.SnapSetting.GridUnitY_Height, main.Setting.Room.RoomPlaceRandomness);

                    //        ////. 적용한다 (ActivatedRoomRect에도 적용한다)
                    //        //willApplyedActivatedRoomRect.position += new Vector2(randX, randY);
                    //        //spaceTransformCurrentCenterSnapped += new Vector3(randX, randY, 0).SwizzlesVector(main.Setting.SnapSetting.Swizzle);

                    //    }

                    //}


                    ////. 생성된 방에 Room Activated Rect 할당
                    //instancedRoom.ApplyInstanceRoomActivatedRect(roomPlaceSpaceInfo.ActivatedRoomRect.CustomRect2DRelative);


                    //return spaceTransformCurrentCenterSnapped;



                    #endregion
                    #region L
                    ////. 연산에 사용할 Rect들
                    //var spaceTransformRect = roomPlaceSpaceInfo.Space.SpaceTransformRect;
                    //var activatedRoomRect = roomPlaceSpaceInfo.ActivatedRoomRect;


                    ////. 공간의 정 중앙 Transform 좌표를 구한다
                    //var spaceTransformCurrentCenter = roomPlaceSpaceInfo.Space.SpaceStageGeneratorTransformCurrentCenter(main);
                    ////var spaceTransformCurrentCenter = spaceTransformRect.center;

                    //if (roomPlaceSpaceInfo.Space.SpaceIndex == 0)
                    //{
                    //    Debug.Log($"센타 {spaceTransformCurrentCenter} / {spaceTransformRect.center}");
                    //}

                    ////. 공간의 정 중앙 좌표를 그리드 스냅하여 얻기
                    //var spaceTransformCurrentCenterSnapped = main.Setting.SnapSetting.CalculateGridSnapTransformPositionVector3_bySnapSetting(spaceTransformCurrentCenter, true, true);


                    ////. Activated Room Rect의 오프셋을 통해 "실제 트랜스폼 오프셋"을 구하기위해 GridUnit 연산한다
                    //var transformOffset = activatedRoomRect.Offset.Multiply(main.Setting.SnapSetting.GridUnitOriginalVector2);


                    ////. 공간의 정 중앙에서, Activated Room Rect의 오프셋만큼 벡터를 빼서, Activated Room Rect를 기준으로 중심점에 배치될수 있도록 한다
                    //spaceTransformCurrentCenterSnapped -= transformOffset.SwizzlesVector2To3(main.Setting.SnapSetting.Swizzle);


                    ////? 공간을 벗어나지 않는 범위 안에서, 무작위 위치에 배치되게끔 한다
                    //if (main.Setting.Room.UseRoomPlaceNeareast == false && main.Setting.Room.RoomPlaceRandomness > 0f)
                    //{
                    //    Debug.Log("무작위배치 임시조치");

                    //    if (roomPlaceSpaceInfo.Space.SpaceIndex == 0)
                    //    {
                    //        //float minX = spaceRect.xMin - willApplyedActivatedRoomRect.xMin;
                    //        //float maxX = spaceRect.xMax - willApplyedActivatedRoomRect.xMax;
                    //        //float minY = spaceRect.yMin - willApplyedActivatedRoomRect.yMin;
                    //        //float maxY = spaceRect.yMax - willApplyedActivatedRoomRect.yMax;
                    //        float minX = spaceTransformRect.xMin - activatedRoomRect.xMin;
                    //        float maxX = spaceTransformRect.xMax - activatedRoomRect.xMax;
                    //        float minY = spaceTransformRect.yMin - activatedRoomRect.yMin;
                    //        float maxY = spaceTransformRect.yMax - activatedRoomRect.yMax;

                    //        //Debug.Log($"{spaceTransformCurrentCenter}");
                    //        Debug.Log($"{minX} ~ {maxX}");

                    //        //float randX = main.generateInfoM.Random.RangeUnitFromMid(minX, maxX, main.Setting.SnapSetting.GridUnitX_Width, main.Setting.Room.RoomPlaceRandomness);
                    //        //float randY = main.generateInfoM.Random.RangeUnitFromMid(minY, maxY, main.Setting.SnapSetting.GridUnitY_Height, main.Setting.Room.RoomPlaceRandomness);

                    //        ////. 적용한다 (ActivatedRoomRect에도 적용한다)
                    //        //willApplyedActivatedRoomRect.position += new Vector2(randX, randY);
                    //        //spaceTransformCurrentCenterSnapped += new Vector3(randX, randY, 0).SwizzlesVector(main.Setting.SnapSetting.Swizzle);

                    //    }

                    //}


                    ////. 생성된 방에 Room Activated Rect 할당
                    //instancedRoom.ApplyInstanceRoomActivatedRect(roomPlaceSpaceInfo.ActivatedRoomRect.CustomRect2DRelative);


                    //return spaceTransformCurrentCenterSnapped;

                    #endregion
                    #region Legacy 250805 
                    ////. 연산에 사용할 Rect들
                    //var spaceRect = roomPlaceSpaceInfo.Space.SpaceRect;
                    //var activatedRoomRect = roomPlaceSpaceInfo.ActivatedRoomRect;

                    ////. 공간의 정 중앙 좌표를 구하기
                    //var spaceTransformCurrentCenter = roomPlaceSpaceInfo.Space.SpaceStageGeneratorTransformCurrentCenter(main);

                    ////. 공간의 정 중앙 좌표를 그리드 스냅하여 얻기
                    //var spaceTransformCurrentCenterSnapped = main.Setting.SnapSetting.CalculateGridSnapTransformPositionVector3_bySnapSetting(spaceTransformCurrentCenter, true, true);

                    ////. Activated Room Rect의 오프셋만큼 벡터를 빼서, Activated Room Rect를 기준으로 중심점에 배치될수 있도록 한다
                    //spaceTransformCurrentCenterSnapped -= activatedRoomRect.Offset.SwizzlesVector2To3(main.Setting.SnapSetting.Swizzle);


                    ////. 소환 위치를 정함과 방에 ActivatedRoomRect 등록
                    //var willApplyedActivatedRoomRect = activatedRoomRect.Rect;

                    ////. ActivatedRoomRect를 공간 안에 배치된 위치와 맞추기 위해 연산한다
                    //willApplyedActivatedRoomRect.position -= activatedRoomRect.Offset;
                    //willApplyedActivatedRoomRect.position += spaceRect.position + new Vector2(Mathf.Floor(spaceRect.width * 0.5f), Mathf.Floor(spaceRect.height * 0.5f));
                    //willApplyedActivatedRoomRect.position +=
                    //    (main.Setting.SnapSetting.SnapToGridCellCenter) ?
                    //    new Vector2(main.Setting.SnapSetting.GridUnitX_Width * 0.5f, main.Setting.SnapSetting.GridUnitY_Height * 0.5f) :
                    //    new Vector2(main.Setting.SnapSetting.GridUnitX_Width, main.Setting.SnapSetting.GridUnitY_Height);



                    ////? 공간을 벗어나지 않는 범위 안에서, 무작위 위치에 배치되게끔 한다
                    //if (main.Setting.Room.UseRoomPlaceNeareast == false && main.Setting.Room.RoomPlaceRandomness > 0f)
                    //{
                    //    float minX = spaceRect.xMin - willApplyedActivatedRoomRect.xMin;
                    //    float maxX = spaceRect.xMax - willApplyedActivatedRoomRect.xMax;
                    //    float minY = spaceRect.yMin - willApplyedActivatedRoomRect.yMin;
                    //    float maxY = spaceRect.yMax - willApplyedActivatedRoomRect.yMax;
                    //    float randX = main.generateInfoM.Random.RangeUnitFromMid(minX, maxX, main.Setting.SnapSetting.GridUnitX_Width, main.Setting.Room.RoomPlaceRandomness);
                    //    float randY = main.generateInfoM.Random.RangeUnitFromMid(minY, maxY, main.Setting.SnapSetting.GridUnitY_Height, main.Setting.Room.RoomPlaceRandomness);

                    //    //. 적용한다 (ActivatedRoomRect에도 적용한다)
                    //    willApplyedActivatedRoomRect.position += new Vector2(randX, randY);
                    //    spaceTransformCurrentCenterSnapped += new Vector3(randX, randY, 0).SwizzlesVector(main.Setting.SnapSetting.Swizzle);
                    //}

                    //instancedRoom.ApplyInstanceRoomActivatedRect(new RectWithOffset(willApplyedActivatedRoomRect, willApplyedActivatedRoomRect.center - instancedRoom.GridCenterTransformPositionVector2));


                    //var spaceTransformCurrentCenterSnapped2 = main.Setting.SnapSetting.CalculateGridSnapTransformPositionVector3_bySnapSetting(spaceTransformCurrentCenterSnapped, true, true);

                    ////? 중심 스냅 보정
                    ////. 이미 위 메서드에서 보정이 되어야 할 것 처럼 보이지만, 또 해주어야 한다..
                    //if (main.Setting.SnapSetting.SnapToGridCellCenter)
                    //{
                    //    spaceTransformCurrentCenterSnapped2 += new Vector3(
                    //        main.Setting.SnapSetting.GridUnitX_Width * 0.5f,
                    //        main.Setting.SnapSetting.GridUnitY_Height * 0.5f,
                    //        0).SwizzlesVector(main.Setting.SnapSetting.Swizzle);
                    //}

                    //if (roomPlaceSpaceInfo.Space.SpaceIndex == 0)
                    //{
                    //    Debug.Log($"0번공간: {spaceTransformCurrentCenterSnapped2}");
                    //}

                    //return spaceTransformCurrentCenterSnapped2; 
                    #endregion 
                    #endregion
                }



                ///======================================================================================================================================================



                //? 방 그리드 이벤트



                /// <summary>
                /// 모든 공간을 순회하여,<br/>
                /// <b>인스턴스화</b>된 <see cref="RoomObject"/>을 받아와, 그 방의 Rect를 기반으로<br/>
                /// 그리드 이벤트를 실행한다
                /// </summary>
                /// <param name="main"></param>
                /// <param name="spaceList"></param>
                private void GridEvents_AllRooms(StageGenerator main, IReadOnlyList<Space> spaceList)
                {
                    //. 병렬 연산을위해, 유니티 메인 쓰레드 연산을 미리 캐싱한다
                    foreach (var space in spaceList)
                    {
                        if (space.PlacedRoom == null) { continue; }
                        space.PlacedRoom.Caching_GridRectCache();
                    }


                    //? 배치가 모두 끝나면, 방들의 그리드이벤트를 실행
                    Parallel.ForEach(spaceList, (space) =>
                    {
                        //! 방이 존재하지 않으면 continue
                        if (space.PlacedRoom == null) { return; }


                        //? 커스텀 이벤트:  방 소환 (그리드 이벤트 실행 이전)
                        if (main.Setting.CustomEvent.SummonRoom != null) { main.Setting.CustomEvent.SummonRoom.AfterPost_SummonRoom_BeforeGridEvent(main, space.PlacedRoom, space, spaceList); }


                        //. 커스텀 이벤트 스위치 (그리드 이벤트 실행 전)
                        if ((main.Setting.CustomEvent.SummonRoom == null || !main.Setting.CustomEvent.SummonRoom.Switch_SummonRoom_GridEvent(main, space.PlacedRoom, space, spaceList)))
                        {
                            GridEvent_PlacedRoom(main, space, space.PlacedRoom);
                        }


                        //? 커스텀 이벤트:  방 소환 (그리드 이벤트 실행 이후)
                        if (main.Setting.CustomEvent.SummonRoom != null) { main.Setting.CustomEvent.SummonRoom.AfterPost_SummonRoom_AfterGridEvent(main, space.PlacedRoom, space, spaceList); }
                    });


                    #region 병렬X, 방미존재시 continue 인것을 return으로 수정하기금지
                    //foreach (var space in spaceList)
                    //{
                    //    ////! 방이 존재하지 않으면 continue
                    //    //if (space.CurrentRoomObject == null) { continue; }


                    //    ////? 커스텀 이벤트:  방 소환 (그리드 이벤트 실행 이전)
                    //    //main.Setting.CustomEvent_SummonRoom?.AfterPost_SummonRoom_BeforeGridEvent(main, space.CurrentRoomObject, space, spaceList);


                    //    ////. 커스텀 이벤트 스위치 (그리드 이벤트 실행 전)
                    //    //if ((main.Setting.CustomEvent_SummonRoom == null || !main.Setting.CustomEvent_SummonRoom.Switch_SummonRoom_GridEvent(main, space.CurrentRoomObject, space, spaceList)))
                    //    //{
                    //    //    GridEvent_SummoonedRoom(main, space, space.CurrentRoomObject);
                    //    //}


                    //    ////? 커스텀 이벤트:  방 소환 (그리드 이벤트 실행 이후)
                    //    //main.Setting.CustomEvent_SummonRoom?.AfterPost_SummonRoom_AfterGridEvent(main, space.CurrentRoomObject, space, spaceList);
                    //} 
                    #endregion
                }



                /// <summary>
                /// 배치된된 <see cref="RoomObject"/>을 받아와, 그 방의 Rect를 기반으로 그리드 이벤트를 실행한다
                /// </summary>
                /// <param name="main"></param>
                /// <param name="placedRoom"></param>
                public static void GridEvent_PlacedRoom(StageGenerator main, Space space, RoomObject placedRoom)
                {
                    placedRoom.TryGetInstanceRoomActivatedRect(out var roomActivatedRect);
                    placedRoom.TryGetInstanceRoomActivatedRectExpand(out var roomActivatedRectExpand);


                    var roomRect = placedRoom.GridRectCache.Value;


                    #region 그리드 변환연산이 변경되며 사용하지않는 연산
                    ////? 보정연산 (스테이지 부모 좌표에 따른 보정)
                    //var correction = main.TransformM.StageParentPositionCurrentCache;


                    //////? 그리드 단위 보정 연산 (그리드 단위가 1 초과일때, 스테이지 부모 좌표가 다를때)
                    ////var correctionUnit = (Vector2)correction;
                    ////main.Setting.SnapSetting.SnapToGridUnit(ref correctionUnit);
                    ////correctionUnit.DivideRef(main.Setting.SnapSetting.GridUnitOriginalVector2);
                    ////roomRect.position += correctionUnit;


                    //if (main.Setting.SnapSetting.SnapToGridCellCenter)
                    //{
                    //    if (correction.x > 0)
                    //    {
                    //        var cx = roomRect.position;
                    //        cx.x -= main.Setting.SnapSetting.GridUnitX_Width * 0.5f;
                    //        roomRect.position = cx;
                    //    }

                    //    if (correction.y > 0)
                    //    {
                    //        var cy = roomRect.position;
                    //        cy.y -= main.Setting.SnapSetting.GridUnitY_Height * 0.5f;
                    //        roomRect.position = cy;
                    //    }
                    //} 
                    #endregion
                    var roomRectEdge = roomRect.ExpandFromCenter(2, 2); //. (안전구역 미포함에서 +2/+2) (도어까지 생성한 뒤에, 도어와 겹치지 않게 배치됨)
                    var roomSafeArea = new Rect(roomRect.center + roomActivatedRect.Offset - roomActivatedRect.Size * 0.5f, roomActivatedRect.Size);
                    var roomSafeAreaExpand = new Rect(roomRect.center + roomActivatedRectExpand.Offset - roomActivatedRectExpand.Size * 0.5f, roomActivatedRectExpand.Size);


                    //. 방 RoomRect에 <Room_Main> 그리드 이벤트를 실행
                    lock (main.PlaceM.PlaceObjects_Room.GetPlacedGrids)
                    {
                        main.GridM.GridsEvent_Rect(false, roomRect, main.Setting.StageGrid.GridTagSetting_RoomMain.EnableGrid, main.PlaceM.PlaceObjects_Room.GetPlacedGrids, true);
                    }


                    //. 방 RoomRect 안전구역에 <Room_Safe> 그리드 이벤트 실행
                    lock (main.PlaceM.GetGrids_CustomExpand)
                    {
                        main.GridM.GridsEvent_Rect(false, roomSafeArea, main.Setting.StageGrid.GridTagSetting_RoomSafe.EnableGrid, main.PlaceM.GetGrids_CustomExpand, true);
                    }


                    //. 방 RoomRect 주위에 1x2칸씩, <Room_Edge> 그리드 이벤트를 실행
                    lock (main.PlaceM.GetGrids_CustomExpand)
                    {
                        main.GridM.GridsEvent_Rect(false, roomRectEdge, main.Setting.StageGrid.GridTagSetting_RoomEdge.EnableGrid, main.PlaceM.GetGrids_CustomExpand, true);
                    }


                    //. 방 RoomRect 안전구역 확장 <Room_ExpandSafe> 그리드 이벤트 실행
                    lock (main.PlaceM.GetGrids_CustomExpand)
                    {
                        main.GridM.GridsEvent_Rect(false, roomSafeAreaExpand, main.Setting.StageGrid.GridTagSetting_RoomExpandSafe.EnableGrid, main.PlaceM.GetGrids_CustomExpand, true);
                    }


                    //. 방 RoomRect 안전구역 확장에 특별한 추가로 그리드 이벤트 실행
                    lock (main.PlaceM.GetGrids_CustomExpand)
                    {
                        main.GridM.GridsEvent_Rect(false, roomSafeAreaExpand, (grid) =>
                        {
                            //! 안전구역 또는 안전구역 확장이 아닐경우, 적용되지 않는다
                            if (!grid.AnyTag(GridTag.Room_Safe | GridTag.Room_ExpandSafe)) { return false; }


                            //? 하단 안전구역
                            if (grid.GridPositionFixed.y < roomRect.yMin) { grid.AddTag(GridTag.Room_Safe_Down); }


                            //? 상단 안전구역
                            else if (grid.GridPositionFixed.y > roomRect.yMax) { grid.AddTag(GridTag.Room_Safe_Up); }


                            //? 좌측 안전구역
                            if (grid.GridPositionFixed.x < roomRect.xMin) { grid.AddTag(GridTag.Room_Safe_Left); }


                            //? 우측 안전구역
                            else if (grid.GridPositionFixed.x > roomRect.xMax) { grid.AddTag(GridTag.Room_Safe_Right); }


                            return true;

                        }, main.PlaceM.GetGrids_CustomExpand, true);
                    }


                    return;
                    #region Legacy
                    //// 현재 방의 RoomRect
                    //var currentRoomRect =
                    //main.placeM.RoomRectsCacheM.GetCachedInstanced_Rooms[roomObject];
                    ////main.placeM.RoomRectsCacheM.GetRooms[roomObject];
                    ////main.placeM.RoomRectsCacheM.RoomRect_ApplyPosition(roomObject);
                    //#region 깡으로 가져오기
                    ////var currentRoomRect =
                    ////    roomObject.GetRoomRect();
                    //#endregion


                    //// 현재 방의 RoomRect 테두리
                    //var currentRoomRectEdge =
                    //new Rect(currentRoomRect.position, currentRoomRect.size).ExpandFromCenter(2, 2);


                    //// 현재 방의 RoomRect 안전구역
                    //var currentRoomRect_SafeArea =
                    //main.placeM.RoomRectsCacheM.GetCachedInstanced_RoomSafeAreas[roomObject].Rect;
                    ////main.placeM.RoomRectsCacheM.GetRoomSafeAreas[roomObject].Rect;
                    ////main.placeM.RoomRectsCacheM.RoomSafeAreaRect_ApplyPosition(main, space, roomObject).Rect;
                    //#region 깡으로 가져오기
                    ////var currentRoomRect_SafeArea =
                    ////    roomObject.GetRoomRect_SafeArea_BySpace(ESelectActivesMode.All, space); // 아직 도어가 활성화 되어있지 않기 때문에, All로 가져온다 
                    //#endregion


                    //// 현재 방의 RoomRect 안전구역 확장
                    //var currentRoomRect_SafeAreaExpand =
                    //    main.placeM.RoomRectsCacheM.GetCachedInstanced_RoomSafeAreaExpands[roomObject].Rect;
                    ////main.placeM.RoomRectsCacheM.GetRoomSafeAreaExpands[roomObject].Rect;
                    ////main.placeM.RoomRectsCacheM.RoomSafeAreaExpandRect_ApplyPosition(main, space, roomObject).Rect;
                    //#region 깡으로 가져오기
                    ////var currentRoomRect_SafeAreaExpand =
                    ////    roomObject.GetRoomRect_SafeAreaExpand_BySpace(ESelectActivesMode.All, space, main.Setting); // 아직 도어가 활성화 되어있지 않기 때문에, All로 가져온다 
                    //#endregion


                    ////? 방 RoomRect에 <Room_Main> 그리드 이벤트를 실행
                    //lock (main.PlaceM.PlaceObjects_Room.GetPlacedGrids)
                    //{
                    //    main.GridM.GridsEvent_Rect(false, currentRoomRect, main.Setting.StageGrid.GridTagSetting_RoomMain.EnableGrid, main.PlaceM.PlaceObjects_Room.GetPlacedGrids, true);
                    //}


                    ////? 방 RoomRect 안전구역에 <Room_Safe> 그리드 이벤트 실행
                    //lock (main.PlaceM.GetGrids_CustomExpand)
                    //{
                    //    main.GridM.GridsEvent_Rect(false, currentRoomRect_SafeArea, main.Setting.StageGrid.GridTagSetting_RoomSafe.EnableGrid, main.PlaceM.GetGrids_CustomExpand, true);
                    //}


                    ////? 방 RoomRect 주위에 1칸씩 <Room_Edge> 그리드 이벤트를 실행
                    //lock (main.PlaceM.GetGrids_CustomExpand)
                    //{
                    //    main.GridM.GridsEvent_Rect(false, currentRoomRectEdge, main.Setting.StageGrid.GridTagSetting_RoomEdge.EnableGrid, main.PlaceM.GetGrids_CustomExpand, true);
                    //}
                    ////. (안전구역 미포함에서 +2/+2) (도어까지 생성한 뒤에, 도어와 겹치지 않게 배치됨)


                    ////? 방 RoomRect 안전구역 확장 <Room_ExpandSafe> 그리드 이벤트 실행
                    //lock (main.PlaceM.GetGrids_CustomExpand)
                    //{
                    //    main.GridM.GridsEvent_Rect(false, currentRoomRect_SafeAreaExpand, main.Setting.StageGrid.GridTagSetting_RoomExpandSafe.EnableGrid, main.PlaceM.GetGrids_CustomExpand, true);
                    //}


                    ////? 현재 방의 그리드 RoomRect
                    //var currentRoomGridRect = roomObject.ObjectTransformRect;


                    ////? 방 RoomRect 안전구역 확장에 그리드 이벤트 실행
                    //lock (main.PlaceM.GetGrids_CustomExpand)
                    //{
                    //    main.GridM.GridsEvent_Rect(false, currentRoomRect_SafeAreaExpand, (grid) =>
                    //    {
                    //        //! 안전구역 또는 안전구역 확장이 아닐경우, 적용되지 않는다
                    //        if (!grid.ContainsAny(GridTag.Room_Safe | GridTag.Room_ExpandSafe)) { return false; }


                    //        //? 하단 안전구역
                    //        if (grid.GridPositionFixed.y < currentRoomGridRect.yMin) { grid.AddTag(GridTag.Room_Safe_Down); }


                    //        //? 상단 안전구역
                    //        else if (grid.GridPositionFixed.y > currentRoomGridRect.yMax) { grid.AddTag(GridTag.Room_Safe_Up); }


                    //        //? 좌측 안전구역
                    //        if (grid.GridPositionFixed.x < currentRoomGridRect.xMin) { grid.AddTag(GridTag.Room_Safe_Left); }


                    //        //? 우측 안전구역
                    //        else if (grid.GridPositionFixed.x > currentRoomGridRect.xMax) { grid.AddTag(GridTag.Room_Safe_Right); }


                    //        return true;

                    //    }, main.PlaceM.GetGrids_CustomExpand, true);
                    //} 
                    #endregion
                }



                ///======================================================================================================================================================



                //? 방 인접



                private void Execute_MoveNearestRooms(StageGenerator main, Transform parent, IReadOnlyList<Space> spaceList)
                {
                    if (main.Setting.Room.UseRoomPlaceNearest)
                    {
                        Vector2 plusVelocity = Vector2.zero;

                        //! 왜 이걸 넣어야만하는거지
                        //main.PlaceM.Setting_PlacedRoomTransformInfoCache(spaceList);


                        //? 1차 인접
                        Execute_MoveNearestRooms_First(main, parent, spaceList);


                        //? 2차 인접
                        if (main.Setting.Room.RoomPlaceNearest_UseSecond)
                        {
                            Execute_MoveNearestRooms_Second(main, parent, spaceList);
                        }


                        //? 인접 Post 배치
                        if (main.Setting.Room.UseRoomPlaceNearest_PostPlace) { RoomPlaceNearest_PostPlace(main, parent, spaceList, ref plusVelocity); }


                        //. 연산된 인접 Velocity들을  한번에 적용
                        //main.PlaceM.ApplyBatchVelocity_PlacedRoomTransformInfoCache(plusVelocity);
                        main.PlaceM.PlacedRoomTFInfoCacheM.ApplyBatchVelocity_PlacedRoomTransformInfoCache(main, plusVelocity, main.CalculateSetting.RoomPlaceUseJob);
                    }
                }

                //! 비동기
                private async UniTask Execute_MoveNearestRoomsAsync(StageGenerator main, Transform parent, IReadOnlyList<Space> spaceList)
                {
                    if (main.Setting.Room.UseRoomPlaceNearest)
                    {
                        Vector2 plusVelocity = Vector2.zero;


                        //? 1차 인접
                        await Execute_MoveNearestRooms_FirstAsync(main, parent, spaceList);


                        //? 2차 인접
                        if (main.Setting.Room.RoomPlaceNearest_UseSecond)
                        {
                            await Execute_MoveNearestRooms_SecondAsync(main, parent, spaceList);
                        }


                        //? 인접 Post 배치
                        if (main.Setting.Room.UseRoomPlaceNearest_PostPlace) { RoomPlaceNearest_PostPlace(main, parent, spaceList, ref plusVelocity); }


                        //. 연산된 인접 Velocity들을  한번에 적용
                        //main.PlaceM.ApplyBatchVelocity_PlacedRoomTransformInfoCache(plusVelocity);
                        await main.PlaceM.PlacedRoomTFInfoCacheM.ApplyBatchVelocity_PlacedRoomTransformInfoCacheAsync(main, plusVelocity, main.CalculateSetting.RoomPlaceUseJob);
                    }
                }



                //? 1차 인접을 실행한다
                private void Execute_MoveNearestRooms_First(StageGenerator main, Transform parent, IReadOnlyList<Space> spaceList)
                {
                    //? "단방향 단일 반대축 연산"을 사용하지 않을경우, <1회>만 인접을 실행한다
                    if (!main.Setting.Room.RoomPlaceNearest_First_UseOneWayNearExpand)
                    {
                        MoveNearestRooms_First(main, parent, spaceList, false);
                    }

                    //? "단방향 단일 반대축 연산"을 사용 할 경우, <최대 반복 횟수> 만큼 인접을 실행한다
                    //.     반복 도중, 그 어떠한 움직임도 감지되지 않으면, 즉시 탈출한다
                    else
                    {
                        for (int i = 0; i < main.Setting.Room.RoomPlaceNearest_First_UseOneWayNearExpand_MaxCount; i++)
                        {
                            main.PlaceM.MoveNearestRooms_First_ReportCount = i + 1; //. 순회 횟수 기록

                            //. 어떤 움직임이 나오지않을때까지 반복한다
                            if (!MoveNearestRooms_First(main, parent, spaceList, true))
                            {
                                break;
                            }
                        }
                    }
                }

                //! 비동기
                private async UniTask Execute_MoveNearestRooms_FirstAsync(StageGenerator main, Transform parent, IReadOnlyList<Space> spaceList)
                {
                    //? "단방향 단일 반대축 연산"을 사용하지 않을경우, <1회>만 인접을 실행한다
                    if (!main.Setting.Room.RoomPlaceNearest_First_UseOneWayNearExpand)
                    {
                        await MoveNearestRooms_FirstAsync(main, parent, spaceList, false);
                    }

                    //? "단방향 단일 반대축 연산"을 사용 할 경우, <최대 반복 횟수> 만큼 인접을 실행한다
                    //.     반복 도중, 그 어떠한 움직임도 감지되지 않으면, 즉시 탈출한다
                    else
                    {
                        for (int i = 0; i < main.Setting.Room.RoomPlaceNearest_First_UseOneWayNearExpand_MaxCount; i++)
                        {
                            main.PlaceM.MoveNearestRooms_First_ReportCount = i + 1; //. 순회 횟수 기록

                            //. 어떤 움직임이 나오지않을때까지 반복한다
                            if (!await MoveNearestRooms_FirstAsync(main, parent, spaceList, true))
                            {
                                break;
                            }
                        }
                    }
                }



                //. 1차 인접, 공간 밖을 벗어나지 않는 선에서, 최대한 인접한다
                private bool MoveNearestRooms_First(StageGenerator main, Transform parent, IReadOnlyList<Space> spaceList, bool roomPlaceNearest_First_UseOneWayNear)
                {
                    //. 인접 도중, 이동이 이루어졌는지 확인하는 플래그
                    bool isMovedSomething = false;


                    for (int i = 0; i < spaceList.Count; i++)
                    {
                        Space space = spaceList[i];

                        //! 방이 없다면, 인접할 방도 뭣도 없으니 continue
                        if (space.PlacedRoom == null) { continue; }


                        #region 내부 필드

                        //. 확장된 방 Activated TransformRect 기준으로 연산한다
                        var roomActivatedTransformRectExpand = main.PlaceM.PlacedRoomTFInfoCacheM.GetRoomActivatedRectExpand_byCache(space.PlacedRoom);

                        //. 중심점 또한!
                        var roomCetnerWithOffset = roomActivatedTransformRectExpand.CenterWithOffset;


                        //? 보정연산 (스테이지 부모 좌표에 따른 보정)
                        roomCetnerWithOffset -= (Vector2Int)main.TransformM.StageParentSnappedPositionCurrentCache;


                        //. 공간 트랜스폼 내부의 최소~최대값
                        var spaceTransformRect = space.SpaceOriginTransformRect;
                        var clampSpaceTransformRect = SU_TF_Rect.RectFromCorners(
                            spaceTransformRect.xMin - roomActivatedTransformRectExpand.xMin,
                            spaceTransformRect.yMin - roomActivatedTransformRectExpand.yMin,
                            spaceTransformRect.xMax - roomActivatedTransformRectExpand.xMax,
                            spaceTransformRect.yMax - roomActivatedTransformRectExpand.yMax);


                        //. 세로 노드 (하단, 상단) 의 의한 이동 Velocity
                        Vector2 moveVelocity_byVerticalNode = Vector2.zero;
                        //. 가로 노드 (좌측, 우측) 의 의한 이동 Velocity
                        Vector2 moveVelocity_byHorizontalNode = Vector2.zero;

                        //. 방향별 노드 개수
                        int nodeCount_Down = space.GetCurrentNodeCount(EDirection4.Down);
                        int nodeCount_Up = space.GetCurrentNodeCount(EDirection4.Up);
                        int nodeCount_Left = space.GetCurrentNodeCount(EDirection4.Left);
                        int nodeCount_Right = space.GetCurrentNodeCount(EDirection4.Right);

                        //. 세로, 가로 노드 정보
                        bool hasNode_Vertical = space.HasNode_Vertical;
                        bool hasNode_Horizontal = space.HasNode_Horizontal;

                        //. 단방향 노드 정보
                        bool isOneWayNode_Down = space.IsOneWayNode_Down;
                        bool isOneWayNode_Up = space.IsOneWayNode_Up;
                        bool isOneWayNode_Left = space.IsOneWayNode_Left;
                        bool isOneWayNode_Right = space.IsOneWayNode_Right;

                        //. 단일 단방향 노드 정보
                        bool isOnlyNode_Down = space.IsOnlyNode_Down;
                        bool isOnlyNode_Up = space.IsOnlyNode_Up;
                        bool isOnlyNode_Left = space.IsOnlyNode_Left;
                        bool isOnlyNode_Right = space.IsOnlyNode_Right;

                        //. 양방향 노드 정보
                        bool isTwoWayNode_Vertical = space.IsTwoWayNode_Vertical;
                        bool isTwoWayNode_Horizontal = space.IsTwoWayNode_Horizontal;

                        #endregion


                        //. 우선, 방향별로 노드가 단방향으로 존재하다면, 각 공간의 끝부분까지 이동시킨다 (방향별로 끝부분 위치가 달라짐)
                        //?     "단방향" 노드이지많, 여러개의 노드가 있을경우, 연결된 "공간"들의 "평균 중심점" 만큼 연산하여 이동한다
                        //!         "단방향" 만 연산되기에, (하-상) or (좌-우) 로 연결되어있다면, 각각 이동하지않는다
                        move_byVerticalNodeMaxClamp();
                        move_byHorizontalNodeMaxClamp();
                        void move_byVerticalNodeMaxClamp()
                        {
                            //. 세로 노드를 사용중인가?
                            if (hasNode_Vertical)
                            {
                                //. 하단 단방향 일경우, 공간의 최하단Y으로 이동시킨다
                                if (isOneWayNode_Down) { moveVelocity_byVerticalNode.y = clampSpaceTransformRect.yMin; }

                                //. 상단 단방향 일경우, 공간의 최상단Y으로 이동시킨다
                                if (isOneWayNode_Up) { moveVelocity_byVerticalNode.y = clampSpaceTransformRect.yMax; }
                            }
                        }
                        void move_byHorizontalNodeMaxClamp()
                        {
                            //. 가로 노드를 사용중인가?
                            if (hasNode_Horizontal)
                            {
                                //. 좌측 단방향 일경우, 공간의 최좌측X으로 이동시킨다
                                if (isOneWayNode_Left) { moveVelocity_byHorizontalNode.x = clampSpaceTransformRect.xMin; }

                                //. 우측 단방향 일경우, 공간의 최우측Y으로 이동시킨다
                                if (isOneWayNode_Right) { moveVelocity_byHorizontalNode.x = clampSpaceTransformRect.xMax; }
                            }
                        }


                        //? "단방향"에 해당하는 방을 각 공간의 끝부분까지 이동시키기 완료!

                        //. "단방향" 이지만, 해당 방향에 여러 노드가 있을경우, 그 평균점으로 "노드의 반대축"을 이동시켜야 한다
                        //.     (공간의 평균점을 구한다)
                        //. "양방향" 이라면, 양쪽에있는 모든 노드들의 평균점을 가져와, "노드의 반대축"을 이동시켜야 한다 
                        //.     (방의 평균점을 구한다)

                        //? "세로 노드"는 X축을 이동시키며, "가로 노드"는 Y축을 이동시킨다

                        moveReverseAxis_VerticalNode();
                        moveReverseAxis_HorizontalNode();

                        void moveReverseAxis_VerticalNode()
                        {
                            //. 세로 노드를 사용중인가?
                            if (hasNode_Vertical)
                            {
                                //. 세로 노드에 "단방향"으로 연결 되어 있다면
                                if (!isTwoWayNode_Vertical)
                                {
                                    //? "단뱡향"인 하단 or 상단 노드가 둘중 하나라도 1개 초과일경우
                                    if (nodeCount_Up > 1 || nodeCount_Down > 1)
                                    {
                                        //. 하단 or 상단 노드들의 "공간 평균 중심점" 좌표를 얻어와,
                                        //.     X축을 이동시킨다
                                        //.     이때, "가로노드"에서 적용되어있는 X축을 초기화 시킨다 (가로노드가 존재 유무와 상관없이 그냥 초기화 해준다)
                                        moveHorizontal_byVerticalNodes(false, out var sum1X);
                                        moveVelocity_byVerticalNode.x = Mathf.Floor(sum1X.x - roomCetnerWithOffset.x);
                                        moveVelocity_byHorizontalNode.x = 0;
                                    }


                                    //? 하단 or 상단 노드의 노드 개수가 1개이고,
                                    //? 노드가 4방향중 (하단 or 상단) 에만 연결되어있고
                                    //? "단방향 단일 반대축 연산" 여부가 활성화 되어있다면
                                    //! "단방향 단일 반대축" 연산을 실행한다
                                    else if ((isOnlyNode_Down || isOnlyNode_Up) && roomPlaceNearest_First_UseOneWayNear)
                                    {
                                        //. "단일 하단 반대축" 연산
                                        //.     하단 방의 중심점을 기준으로 연산한다
                                        if (isOneWayNode_Down &&
                                            space.ConnectingSpaces_Down != null &&
                                            space.ConnectingSpaces_Down[0] != null &&
                                            space.ConnectingSpaces_Down[0].PlacedRoom != null)
                                        {
                                            var connectingSpaceOne_Down = space.ConnectingSpaces_Down[0];
                                            var downNodeCenter = getRoomCenter_bySpace(main, connectingSpaceOne_Down, 1);
                                            moveVelocity_byVerticalNode.x = Mathf.Floor(downNodeCenter.x - roomCetnerWithOffset.x);
                                            moveVelocity_byHorizontalNode.x = 0;
                                        }

                                        //. "단일 상단 반대축" 연산
                                        //.     상단 방의 중심점을 기준으로 연산한다
                                        if (isOneWayNode_Up &&
                                            space.ConnectingSpaces_Up != null &&
                                            space.ConnectingSpaces_Up[0] != null &&
                                            space.ConnectingSpaces_Up[0].PlacedRoom != null)
                                        {
                                            var connectingSpaceOne_Up = space.ConnectingSpaces_Up[0];
                                            var upNodeCenter = getRoomCenter_bySpace(main, connectingSpaceOne_Up, 1);
                                            moveVelocity_byVerticalNode.x = Mathf.Floor(upNodeCenter.x - roomCetnerWithOffset.x);
                                            moveVelocity_byHorizontalNode.x = 0;
                                        }
                                    }
                                }

                                //. 하단 and 상단 "양방향"으로 연결 되어 있다면.
                                else
                                {
                                    //x 하단 and 상단 노드들의 "방 평균 중심점" 좌표를 얻어와,
                                    //. 하단 and 상단 노드들의 "공간 평균 중심점" 좌표를 얻어와,
                                    //.     X축을 이동시킨다
                                    //.     이때, "가로노드"에서 적용되어있는 X축을 초기화 시킨다 (가로노드가 존재 유무와 상관없이 그냥 초기화 해준다)
                                    moveHorizontal_byVerticalNodes(false, out var sum2X);
                                    moveVelocity_byVerticalNode.x = Mathf.Floor(sum2X.x - (roomCetnerWithOffset.x));// * ((sum2X.x >= roomCetnerWithOffset.x) ? 1f : -1f);
                                    moveVelocity_byHorizontalNode.x = 0;
                                }
                            }
                        }
                        void moveReverseAxis_HorizontalNode()
                        {
                            //. 가로 노드를 사용중인가?
                            if (hasNode_Horizontal)
                            {
                                //. 가로 노드에 "단방향"으로 연결 되어 있다면
                                if (!isTwoWayNode_Horizontal)
                                {
                                    //? "단뱡향"인 좌측 or 우측 노드가 둘중 하나라도 1개 초과일경우
                                    if ((nodeCount_Right > 1 || nodeCount_Left > 1))
                                    {
                                        //. 좌측 or 우측 노드들의 "공간 평균 중심점" 좌표를 얻어와,
                                        //.     Y축을 이동시킨다
                                        //.     이때, "세로노드"에서 적용되어있는 Y축을 초기화 시킨다 (세로노드가 존재 유무와 상관없이 그냥 초기화 해준다)
                                        moveVertical_byHorizontalNodes(false, out var sum1Y);
                                        moveVelocity_byHorizontalNode.y = Mathf.Floor(sum1Y.y - roomCetnerWithOffset.y);
                                        moveVelocity_byVerticalNode.y = 0;
                                    }


                                    //? 좌측 or 우측 노드의 노드 개수가 1개이고,
                                    //? 노드가 4방향중 (좌측 or 우측) 에만 연결되어있고
                                    //? "단방향 단일 반대축 연산" 여부가 활성화 되어있다면
                                    //! "단방향 단일 반대축" 연산을 실행한다
                                    else if ((isOnlyNode_Left || isOnlyNode_Right) && roomPlaceNearest_First_UseOneWayNear)
                                    {
                                        //. "단일 좌측 반대축" 연산
                                        //.     좌측 방의 중심점을 기준으로 연산한다
                                        if (isOneWayNode_Left &&
                                            space.ConnectingSpaces_Left != null &&
                                            space.ConnectingSpaces_Left[0] != null &&
                                            space.ConnectingSpaces_Left[0].PlacedRoom != null)
                                        {
                                            var connectingSpaceOne_Left = space.ConnectingSpaces_Left[0];
                                            var leftNodeCenter = getRoomCenter_bySpace(main, connectingSpaceOne_Left, 1);
                                            moveVelocity_byHorizontalNode.y = Mathf.Floor(leftNodeCenter.y - roomCetnerWithOffset.y);
                                            moveVelocity_byVerticalNode.y = 0;
                                        }

                                        //. "단일 우측 반대축" 연산
                                        //.     우측 방의 중심점을 기준으로 연산한다
                                        if (isOneWayNode_Right &&
                                            space.ConnectingSpaces_Right != null &&
                                            space.ConnectingSpaces_Right[0] != null &&
                                            space.ConnectingSpaces_Right[0].PlacedRoom != null)
                                        {
                                            var connectingSpaceOne_Right = space.ConnectingSpaces_Right[0];
                                            var rightNodeCenter = getRoomCenter_bySpace(main, connectingSpaceOne_Right, 1);
                                            moveVelocity_byHorizontalNode.y = Mathf.Floor(rightNodeCenter.y - roomCetnerWithOffset.y);
                                            moveVelocity_byVerticalNode.y = 0;
                                        }
                                    }
                                }

                                //. 좌측 and 우측 "양방향"으로 연결 되어 있다면.
                                else
                                {
                                    //x 좌측 and 우측 노드들의 "방 평균 중심점" 좌표를 얻어와,
                                    //. 좌측 and 우측 노드들의 "공간 평균 중심점" 좌표를 얻어와,
                                    //.     Y축을 이동시킨다
                                    //.     이때, "세로노드"에서 적용되어있는 Y축을 초기화 시킨다 (세로노드가 존재 유무와 상관없이 그냥 초기화 해준다)
                                    moveVertical_byHorizontalNodes(false, out var sum2Y);
                                    moveVelocity_byHorizontalNode.y = Mathf.Floor(sum2Y.y - (roomCetnerWithOffset.y));// * ((sum2Y.y >= roomCetnerWithOffset.y) ? 1f : -1f);
                                    moveVelocity_byVerticalNode.y = 0;
                                }
                            }
                        }


                        #region 내부 연산용 메서드

                        //. "세로 노드"에 의한, "가로" 이동 (Y축 노드 -> Y축 이동)
                        void moveHorizontal_byVerticalNodes(bool useRoomCenter, out Vector2 sumVector)
                        {
                            //. 하단 or 상단의 이어진 방들의 중심점 X축을 받아와, 그 평균값 만큼 X축 이동 명령
                            sumVector = Vector2.zero;


                            //? 하단, 상단의 공간에 해당하는 방들이 존재하는 개수 기록
                            //.     별도로 변수를 선언하여, 방이 없는 공간들은 평균값 계산에서 제외하도록 한다
                            int placedRoomCount_Vertical = nodeCount_Down + nodeCount_Up;


                            if (placedRoomCount_Vertical <= 0) { sumVector = Vector2.zero; return; }


                            //? 하단, 상단의 공간을 순회하면서 (노드가 존재한다면), 방이 존재하는 각평균값을 연산한다
                            if (nodeCount_Down > 0) { getSumCenterVector_byConnectingSpaces(useRoomCenter, 2, ref sumVector, ref placedRoomCount_Vertical, space.ConnectingSpaces_Down); }
                            if (nodeCount_Up > 0) { getSumCenterVector_byConnectingSpaces(useRoomCenter, 2, ref sumVector, ref placedRoomCount_Vertical, space.ConnectingSpaces_Up); }


                            //. 평균값 연산
                            //.     (하단, 상단에 있는 방들의 중심점 벡터 ÷ 하단, 상단에 있는 방들의 개수)
                            sumVector /= placedRoomCount_Vertical;
                        }


                        //. "가로 노드"에 의한, "세로" 이동 (X축 노드 -> Y축 이동)
                        void moveVertical_byHorizontalNodes(bool useRoomCenter, out Vector2 sumVector)
                        {
                            //. 좌측 or 우측의 이어진 방들의 중심점 Y축을 받아와, 그 평균값 만큼 Y축 이동 명령
                            sumVector = Vector2.zero;


                            //? 좌측, 우측의 공간에 해당하는 방들이 존재하는 개수 기록
                            //.     별도로 변수를 선언하여, 방이 없는 공간들은 평균값 계산에서 제외하도록 한다
                            int placedRoomCount_Horizontal = nodeCount_Left + nodeCount_Right;


                            if (placedRoomCount_Horizontal <= 0) { sumVector = Vector2.zero; return; }


                            //? 좌측, 우측의 공간을 순회하면서 (노드가 존재한다면), 방이 존재하는 각평균값을 연산한다
                            if (nodeCount_Left > 0) { getSumCenterVector_byConnectingSpaces(useRoomCenter, 2, ref sumVector, ref placedRoomCount_Horizontal, space.ConnectingSpaces_Left); }
                            if (nodeCount_Right > 0) { getSumCenterVector_byConnectingSpaces(useRoomCenter, 2, ref sumVector, ref placedRoomCount_Horizontal, space.ConnectingSpaces_Right); }


                            //. 평균값 연산
                            //.     (좌측, 우측에 있는 방들의 중심점 벡터 ÷ 좌측, 우측에 있는 방들의 개수)
                            sumVector /= placedRoomCount_Horizontal;
                        }


                        //. "연결된 공간 목록"을 받아와, 그 연결된 공간들의 (방 중심점 / 공간 중심점) 들의 평균을 계산해 반환한다
                        //.     원활한 평균값 연산을 위해, 방이 없는 공간 개수 만큼 총 방 개수를 감소시킨다
                        void getSumCenterVector_byConnectingSpaces(bool useRoomCenter, int transformOffsetMultiply, ref Vector2 sumCenterVector, ref int placedRoomCount, IReadOnlyList<Space> connectingSpaces)
                        {
                            for (int i = 0; i < connectingSpaces.Count; i++)
                            {
                                Space connectSpace = connectingSpaces[i];

                                //! 방이없다면, continue하되, 평균값 연산을 위해 1 감소시킨다
                                if (connectSpace.PlacedRoom == null) { placedRoomCount--; continue; }

                                sumCenterVector += getCenter_bySpace(main, connectSpace, useRoomCenter, transformOffsetMultiply);
                            }
                        }

                        #endregion


                        //. 총 Velocity를 합산하여, GridUnit 연산을 하고, 공간을 벗어나지않게 제약한뒤, 적용한다
                        var combineVelocity = moveVelocity_byVerticalNode + moveVelocity_byHorizontalNode;

                        bool currentMovedSomething = ApplyVelocity_RoomNearestRoom(main, spaceList, space.PlacedRoom, combineVelocity, spaceTransformRect);

                        //? 연산된 Velocity가 이동값이 (0,0)이 아니라면, 이동 여부 플래그를 활성화시킨다
                        if (!isMovedSomething && currentMovedSomething) { isMovedSomething = true; }
                    }

                    return isMovedSomething;
                }

                //! 비동기
                private async UniTask<bool> MoveNearestRooms_FirstAsync(StageGenerator main, Transform parent, IReadOnlyList<Space> spaceList, bool roomPlaceNearest_First_UseOneWayNear)
                {
                    //. 인접 도중, 이동이 이루어졌는지 확인하는 플래그
                    bool isMovedSomething = false;


                    for (int i = 0; i < spaceList.Count; i++)
                    {
                        Space space = spaceList[i];

                        //! 방이 없다면, 인접할 방도 뭣도 없으니 continue
                        if (space.PlacedRoom == null) { continue; }


                        #region 내부 필드

                        //. 확장된 방 Activated TransformRect 기준으로 연산한다
                        var roomActivatedTransformRectExpand = main.PlaceM.PlacedRoomTFInfoCacheM.GetRoomActivatedRectExpand_byCache(space.PlacedRoom);

                        //. 중심점 또한!
                        var roomCetnerWithOffset = roomActivatedTransformRectExpand.CenterWithOffset;


                        //. 공간 트랜스폼 내부의 최소~최대값
                        var spaceTransformRect = space.SpaceOriginTransformRect;
                        var clampSpaceTransformRect = SU_TF_Rect.RectFromCorners(
                            spaceTransformRect.xMin - roomActivatedTransformRectExpand.xMin,
                            spaceTransformRect.yMin - roomActivatedTransformRectExpand.yMin,
                            spaceTransformRect.xMax - roomActivatedTransformRectExpand.xMax,
                            spaceTransformRect.yMax - roomActivatedTransformRectExpand.yMax);


                        //? 보정연산 (스테이지 부모 좌표에 따른 보정)
                        roomCetnerWithOffset -= (Vector2Int)main.TransformM.StageParentSnappedPositionCurrentCache;


                        //. 세로 노드 (하단, 상단) 의 의한 이동 Velocity
                        Vector2 moveVelocity_byVerticalNode = Vector2.zero;
                        //. 가로 노드 (좌측, 우측) 의 의한 이동 Velocity
                        Vector2 moveVelocity_byHorizontalNode = Vector2.zero;

                        //. 방향별 노드 개수
                        int nodeCount_Down = space.GetCurrentNodeCount(EDirection4.Down);
                        int nodeCount_Up = space.GetCurrentNodeCount(EDirection4.Up);
                        int nodeCount_Left = space.GetCurrentNodeCount(EDirection4.Left);
                        int nodeCount_Right = space.GetCurrentNodeCount(EDirection4.Right);

                        //. 세로, 가로 노드 정보
                        bool hasNode_Vertical = space.HasNode_Vertical;
                        bool hasNode_Horizontal = space.HasNode_Horizontal;

                        //. 단방향 노드 정보
                        bool isOneWayNode_Down = space.IsOneWayNode_Down;
                        bool isOneWayNode_Up = space.IsOneWayNode_Up;
                        bool isOneWayNode_Left = space.IsOneWayNode_Left;
                        bool isOneWayNode_Right = space.IsOneWayNode_Right;

                        //. 단일 단방향 노드 정보
                        bool isOnlyNode_Down = space.IsOnlyNode_Down;
                        bool isOnlyNode_Up = space.IsOnlyNode_Up;
                        bool isOnlyNode_Left = space.IsOnlyNode_Left;
                        bool isOnlyNode_Right = space.IsOnlyNode_Right;

                        //. 양방향 노드 정보
                        bool isTwoWayNode_Vertical = space.IsTwoWayNode_Vertical;
                        bool isTwoWayNode_Horizontal = space.IsTwoWayNode_Horizontal;

                        #endregion


                        //. 우선, 방향별로 노드가 단방향으로 존재하다면, 각 공간의 끝부분까지 이동시킨다 (방향별로 끝부분 위치가 달라짐)
                        //?     "단방향" 노드이지많, 여러개의 노드가 있을경우, 연결된 "공간"들의 "평균 중심점" 만큼 연산하여 이동한다
                        //!         "단방향" 만 연산되기에, (하-상) or (좌-우) 로 연결되어있다면, 각각 이동하지않는다
                        move_byVerticalNodeMaxClamp();
                        move_byHorizontalNodeMaxClamp();
                        void move_byVerticalNodeMaxClamp()
                        {
                            //. 세로 노드를 사용중인가?
                            if (hasNode_Vertical)
                            {
                                //. 하단 단방향 일경우, 공간의 최하단Y으로 이동시킨다
                                if (isOneWayNode_Down) { moveVelocity_byVerticalNode.y = clampSpaceTransformRect.yMin; }

                                //. 상단 단방향 일경우, 공간의 최상단Y으로 이동시킨다
                                if (isOneWayNode_Up) { moveVelocity_byVerticalNode.y = clampSpaceTransformRect.yMax; }
                            }
                        }
                        void move_byHorizontalNodeMaxClamp()
                        {
                            //. 가로 노드를 사용중인가?
                            if (hasNode_Horizontal)
                            {
                                //. 좌측 단방향 일경우, 공간의 최좌측X으로 이동시킨다
                                if (isOneWayNode_Left) { moveVelocity_byHorizontalNode.x = clampSpaceTransformRect.xMin; }

                                //. 우측 단방향 일경우, 공간의 최우측Y으로 이동시킨다
                                if (isOneWayNode_Right) { moveVelocity_byHorizontalNode.x = clampSpaceTransformRect.xMax; }
                            }
                        }


                        //? "단방향"에 해당하는 방을 각 공간의 끝부분까지 이동시키기 완료!

                        //. "단방향" 이지만, 해당 방향에 여러 노드가 있을경우, 그 평균점으로 "노드의 반대축"을 이동시켜야 한다
                        //.     (공간의 평균점을 구한다)
                        //. "양방향" 이라면, 양쪽에있는 모든 노드들의 평균점을 가져와, "노드의 반대축"을 이동시켜야 한다 
                        //.     (방의 평균점을 구한다)

                        //? "세로 노드"는 X축을 이동시키며, "가로 노드"는 Y축을 이동시킨다

                        moveReverseAxis_VerticalNode();
                        moveReverseAxis_HorizontalNode();

                        void moveReverseAxis_VerticalNode()
                        {
                            //. 세로 노드를 사용중인가?
                            if (hasNode_Vertical)
                            {
                                //. 세로 노드에 "단방향"으로 연결 되어 있다면
                                if (!isTwoWayNode_Vertical)
                                {
                                    //? "단뱡향"인 하단 or 상단 노드가 둘중 하나라도 1개 초과일경우
                                    if (nodeCount_Up > 1 || nodeCount_Down > 1)
                                    {
                                        //. 하단 or 상단 노드들의 "공간 평균 중심점" 좌표를 얻어와,
                                        //.     X축을 이동시킨다
                                        //.     이때, "가로노드"에서 적용되어있는 X축을 초기화 시킨다 (가로노드가 존재 유무와 상관없이 그냥 초기화 해준다)
                                        moveHorizontal_byVerticalNodes(false, out var sum1X);
                                        moveVelocity_byVerticalNode.x = Mathf.Floor(sum1X.x - roomCetnerWithOffset.x);
                                        moveVelocity_byHorizontalNode.x = 0;
                                    }


                                    //? 하단 or 상단 노드의 노드 개수가 1개이고,
                                    //? 노드가 4방향중 (하단 or 상단) 에만 연결되어있고
                                    //? "단방향 단일 반대축 연산" 여부가 활성화 되어있다면
                                    //! "단방향 단일 반대축" 연산을 실행한다
                                    else if ((isOnlyNode_Down || isOnlyNode_Up) && roomPlaceNearest_First_UseOneWayNear)
                                    {
                                        //. "단일 하단 반대축" 연산
                                        //.     하단 방의 중심점을 기준으로 연산한다
                                        if (isOneWayNode_Down &&
                                            space.ConnectingSpaces_Down != null &&
                                            space.ConnectingSpaces_Down[0] != null &&
                                            space.ConnectingSpaces_Down[0].PlacedRoom != null)
                                        {
                                            var connectingSpaceOne_Down = space.ConnectingSpaces_Down[0];
                                            var downNodeCenter = getRoomCenter_bySpace(main, connectingSpaceOne_Down, 1);
                                            moveVelocity_byVerticalNode.x = Mathf.Floor(downNodeCenter.x - roomCetnerWithOffset.x);
                                            moveVelocity_byHorizontalNode.x = 0;
                                        }

                                        //. "단일 상단 반대축" 연산
                                        //.     상단 방의 중심점을 기준으로 연산한다
                                        if (isOneWayNode_Up &&
                                            space.ConnectingSpaces_Up != null &&
                                            space.ConnectingSpaces_Up[0] != null &&
                                            space.ConnectingSpaces_Up[0].PlacedRoom != null)
                                        {
                                            var connectingSpaceOne_Up = space.ConnectingSpaces_Up[0];
                                            var upNodeCenter = getRoomCenter_bySpace(main, connectingSpaceOne_Up, 1);
                                            moveVelocity_byVerticalNode.x = Mathf.Floor(upNodeCenter.x - roomCetnerWithOffset.x);
                                            moveVelocity_byHorizontalNode.x = 0;
                                        }
                                    }
                                }

                                //. 하단 and 상단 "양방향"으로 연결 되어 있다면.
                                else
                                {
                                    //x 하단 and 상단 노드들의 "방 평균 중심점" 좌표를 얻어와,
                                    //. 하단 and 상단 노드들의 "공간 평균 중심점" 좌표를 얻어와,
                                    //.     X축을 이동시킨다
                                    //.     이때, "가로노드"에서 적용되어있는 X축을 초기화 시킨다 (가로노드가 존재 유무와 상관없이 그냥 초기화 해준다)
                                    moveHorizontal_byVerticalNodes(false, out var sum2X);
                                    moveVelocity_byVerticalNode.x = Mathf.Floor(sum2X.x - (roomCetnerWithOffset.x));// * ((sum2X.x >= roomCetnerWithOffset.x) ? 1f : -1f);
                                    moveVelocity_byHorizontalNode.x = 0;
                                }
                            }
                        }
                        void moveReverseAxis_HorizontalNode()
                        {
                            //. 가로 노드를 사용중인가?
                            if (hasNode_Horizontal)
                            {
                                //. 가로 노드에 "단방향"으로 연결 되어 있다면
                                if (!isTwoWayNode_Horizontal)
                                {
                                    //? "단뱡향"인 좌측 or 우측 노드가 둘중 하나라도 1개 초과일경우
                                    if ((nodeCount_Right > 1 || nodeCount_Left > 1))
                                    {
                                        //. 좌측 or 우측 노드들의 "공간 평균 중심점" 좌표를 얻어와,
                                        //.     Y축을 이동시킨다
                                        //.     이때, "세로노드"에서 적용되어있는 Y축을 초기화 시킨다 (세로노드가 존재 유무와 상관없이 그냥 초기화 해준다)
                                        moveVertical_byHorizontalNodes(false, out var sum1Y);
                                        moveVelocity_byHorizontalNode.y = Mathf.Floor(sum1Y.y - roomCetnerWithOffset.y);
                                        moveVelocity_byVerticalNode.y = 0;
                                    }


                                    //? 좌측 or 우측 노드의 노드 개수가 1개이고,
                                    //? 노드가 4방향중 (좌측 or 우측) 에만 연결되어있고
                                    //? "단방향 단일 반대축 연산" 여부가 활성화 되어있다면
                                    //! "단방향 단일 반대축" 연산을 실행한다
                                    else if ((isOnlyNode_Left || isOnlyNode_Right) && roomPlaceNearest_First_UseOneWayNear)
                                    {
                                        //. "단일 좌측 반대축" 연산
                                        //.     좌측 방의 중심점을 기준으로 연산한다
                                        if (isOneWayNode_Left &&
                                            space.ConnectingSpaces_Left != null &&
                                            space.ConnectingSpaces_Left[0] != null &&
                                            space.ConnectingSpaces_Left[0].PlacedRoom != null)
                                        {
                                            var connectingSpaceOne_Left = space.ConnectingSpaces_Left[0];
                                            var leftNodeCenter = getRoomCenter_bySpace(main, connectingSpaceOne_Left, 1);
                                            moveVelocity_byHorizontalNode.y = Mathf.Floor(leftNodeCenter.y - roomCetnerWithOffset.y);
                                            moveVelocity_byVerticalNode.y = 0;
                                        }

                                        //. "단일 우측 반대축" 연산
                                        //.     우측 방의 중심점을 기준으로 연산한다
                                        if (isOneWayNode_Right &&
                                            space.ConnectingSpaces_Right != null &&
                                            space.ConnectingSpaces_Right[0] != null &&
                                            space.ConnectingSpaces_Right[0].PlacedRoom != null)
                                        {
                                            var connectingSpaceOne_Right = space.ConnectingSpaces_Right[0];
                                            var rightNodeCenter = getRoomCenter_bySpace(main, connectingSpaceOne_Right, 1);
                                            moveVelocity_byHorizontalNode.y = Mathf.Floor(rightNodeCenter.y - roomCetnerWithOffset.y);
                                            moveVelocity_byVerticalNode.y = 0;
                                        }
                                    }
                                }

                                //. 좌측 and 우측 "양방향"으로 연결 되어 있다면.
                                else
                                {
                                    //x 좌측 and 우측 노드들의 "방 평균 중심점" 좌표를 얻어와,
                                    //. 좌측 and 우측 노드들의 "공간 평균 중심점" 좌표를 얻어와,
                                    //.     Y축을 이동시킨다
                                    //.     이때, "세로노드"에서 적용되어있는 Y축을 초기화 시킨다 (세로노드가 존재 유무와 상관없이 그냥 초기화 해준다)
                                    moveVertical_byHorizontalNodes(false, out var sum2Y);
                                    moveVelocity_byHorizontalNode.y = Mathf.Floor(sum2Y.y - (roomCetnerWithOffset.y));// * ((sum2Y.y >= roomCetnerWithOffset.y) ? 1f : -1f);
                                    moveVelocity_byVerticalNode.y = 0;
                                }
                            }
                        }


                        #region 내부 연산용 메서드

                        //. "세로 노드"에 의한, "가로" 이동 (Y축 노드 -> Y축 이동)
                        void moveHorizontal_byVerticalNodes(bool useRoomCenter, out Vector2 sumVector)
                        {
                            //. 하단 or 상단의 이어진 방들의 중심점 X축을 받아와, 그 평균값 만큼 X축 이동 명령
                            sumVector = Vector2.zero;


                            //? 하단, 상단의 공간에 해당하는 방들이 존재하는 개수 기록
                            //.     별도로 변수를 선언하여, 방이 없는 공간들은 평균값 계산에서 제외하도록 한다
                            int placedRoomCount_Vertical = nodeCount_Down + nodeCount_Up;


                            if (placedRoomCount_Vertical <= 0) { sumVector = Vector2.zero; return; }


                            //? 하단, 상단의 공간을 순회하면서 (노드가 존재한다면), 방이 존재하는 각평균값을 연산한다
                            if (nodeCount_Down > 0) { getSumCenterVector_byConnectingSpaces(useRoomCenter, 2, ref sumVector, ref placedRoomCount_Vertical, space.ConnectingSpaces_Down); }
                            if (nodeCount_Up > 0) { getSumCenterVector_byConnectingSpaces(useRoomCenter, 2, ref sumVector, ref placedRoomCount_Vertical, space.ConnectingSpaces_Up); }


                            //. 평균값 연산
                            //.     (하단, 상단에 있는 방들의 중심점 벡터 ÷ 하단, 상단에 있는 방들의 개수)
                            sumVector /= placedRoomCount_Vertical;
                        }


                        //. "가로 노드"에 의한, "세로" 이동 (X축 노드 -> Y축 이동)
                        void moveVertical_byHorizontalNodes(bool useRoomCenter, out Vector2 sumVector)
                        {
                            //. 좌측 or 우측의 이어진 방들의 중심점 Y축을 받아와, 그 평균값 만큼 Y축 이동 명령
                            sumVector = Vector2.zero;


                            //? 좌측, 우측의 공간에 해당하는 방들이 존재하는 개수 기록
                            //.     별도로 변수를 선언하여, 방이 없는 공간들은 평균값 계산에서 제외하도록 한다
                            int placedRoomCount_Horizontal = nodeCount_Left + nodeCount_Right;


                            if (placedRoomCount_Horizontal <= 0) { sumVector = Vector2.zero; return; }


                            //? 좌측, 우측의 공간을 순회하면서 (노드가 존재한다면), 방이 존재하는 각평균값을 연산한다
                            if (nodeCount_Left > 0) { getSumCenterVector_byConnectingSpaces(useRoomCenter, 2, ref sumVector, ref placedRoomCount_Horizontal, space.ConnectingSpaces_Left); }
                            if (nodeCount_Right > 0) { getSumCenterVector_byConnectingSpaces(useRoomCenter, 2, ref sumVector, ref placedRoomCount_Horizontal, space.ConnectingSpaces_Right); }


                            //. 평균값 연산
                            //.     (좌측, 우측에 있는 방들의 중심점 벡터 ÷ 좌측, 우측에 있는 방들의 개수)
                            sumVector /= placedRoomCount_Horizontal;
                        }


                        //. "연결된 공간 목록"을 받아와, 그 연결된 공간들의 (방 중심점 / 공간 중심점) 들의 평균을 계산해 반환한다
                        //.     원활한 평균값 연산을 위해, 방이 없는 공간 개수 만큼 총 방 개수를 감소시킨다
                        void getSumCenterVector_byConnectingSpaces(bool useRoomCenter, int transformOffsetMultiply, ref Vector2 sumCenterVector, ref int placedRoomCount, IReadOnlyList<Space> connectingSpaces)
                        {
                            for (int i = 0; i < connectingSpaces.Count; i++)
                            {
                                Space connectSpace = connectingSpaces[i];

                                //! 방이없다면, continue하되, 평균값 연산을 위해 1 감소시킨다
                                if (connectSpace.PlacedRoom == null) { placedRoomCount--; continue; }

                                sumCenterVector += getCenter_bySpace(main, connectSpace, useRoomCenter, transformOffsetMultiply);
                            }
                        }

                        #endregion


                        //. 총 Velocity를 합산하여, GridUnit 연산을 하고, 공간을 벗어나지않게 제약한뒤, 적용한다
                        var combineVelocity = moveVelocity_byVerticalNode + moveVelocity_byHorizontalNode;
                        bool currentMovedSomething = ApplyVelocity_RoomNearestRoom(main, spaceList, space.PlacedRoom, combineVelocity, spaceTransformRect);

                        //? 연산된 Velocity가 이동값이 (0,0)이 아니라면, 이동 여부 플래그를 활성화시킨다
                        if (!isMovedSomething && currentMovedSomething) { isMovedSomething = true; }

                        //! 비동기 프레임 양보
                        if (i % main.CalculateSetting.RoomNearestYieldUnitAsync == 0) { await UniTask.Yield(); }
                    }

                    return isMovedSomething;
                }



                //. 2차 인접 실행
                private void Execute_MoveNearestRooms_Second(StageGenerator main, Transform parent, IReadOnlyList<Space> spaceList)
                {
                    //? <최대 반복 횟수> 만큼 인접을 실행한다
                    //.     반복 도중, 그 어떠한 움직임도 감지되지 않으면, 즉시 탈출한다
                    for (int i = 0; i < main.Setting.Room.RoomPlaceNearest_SecondMaxCount; i++)
                    {
                        main.PlaceM.MoveNearestRooms_Second_ReportCount = i + 1; //. 순회 횟수 기록
                                                                                 //. 어떤 움직임이 나오지않을때까지 반복한다
                        if (!MoveNearestRooms_Second(main, parent, spaceList))
                        {
                            break;
                        }
                    }
                }

                //! 비동기
                private async UniTask Execute_MoveNearestRooms_SecondAsync(StageGenerator main, Transform parent, IReadOnlyList<Space> spaceList)
                {
                    //? <최대 반복 횟수> 만큼 인접을 실행한다
                    //.     반복 도중, 그 어떠한 움직임도 감지되지 않으면, 즉시 탈출한다
                    for (int i = 0; i < main.Setting.Room.RoomPlaceNearest_SecondMaxCount; i++)
                    {
                        main.PlaceM.MoveNearestRooms_Second_ReportCount = i + 1; //. 순회 횟수 기록

                        //. 어떤 움직임이 나오지않을때까지 반복한다
                        if (!await MoveNearestRooms_SecondAsync(main, parent, spaceList))
                        {
                            break;
                        }
                    }
                }



                //. 2차 인접, 공간 밖을 벗어나 최대한 인접한다
                private bool MoveNearestRooms_Second(StageGenerator main, Transform parent, IReadOnlyList<Space> spaceList)
                {
                    bool isMovedSomething = false;

                    var stageTransformRect = main.TransformM.StageTransformRect;

                    int spaceOderIndex_RoomNearest = 0;
                    //! 별도로 정렬하지않아도 만족스러운 결과가 나오는 상태
                    foreach (Space space in spaceList)
                    {
                        space.OrderIndex_RoomNearest = spaceOderIndex_RoomNearest;
                        spaceOderIndex_RoomNearest++;

                        //! 방이 없다면, 인접할 방도 뭣도 없으니 continue
                        if (space.PlacedRoom == null) { continue; }


                        #region 내부 필드

                        //. 확장된 방 Activated TransformRect 기준으로 연산한다
                        var roomActivatedTransformRectExpand = main.PlaceM.PlacedRoomTFInfoCacheM.GetRoomActivatedRectExpand_byCache(space.PlacedRoom);
                        var roomActivatedTransformRectExpandHalfSize = roomActivatedTransformRectExpand.Size * 0.5f;

                        //. 중심점 또한!
                        var roomCetnerWithOffset = roomActivatedTransformRectExpand.CenterWithOffset;
                        var roomCetner = roomActivatedTransformRectExpand.Center;


                        //. 세로 노드 (하단, 상단) 의 의한 이동 Velocity
                        Vector2 moveVelocity_byVerticalNode = Vector2.zero;
                        //. 가로 노드 (좌측, 우측) 의 의한 이동 Velocity
                        Vector2 moveVelocity_byHorizontalNode = Vector2.zero;


                        //. 방향별 노드 개수
                        int nodeCount_Down = space.GetCurrentNodeCount(EDirection4.Down);
                        int nodeCount_Up = space.GetCurrentNodeCount(EDirection4.Up);
                        int nodeCount_Left = space.GetCurrentNodeCount(EDirection4.Left);
                        int nodeCount_Right = space.GetCurrentNodeCount(EDirection4.Right);

                        //. 세로, 가로 노드 정보
                        bool hasNode_Vertical = space.HasNode_Vertical;
                        bool hasNode_Horizontal = space.HasNode_Horizontal;

                        //. 단방향 노드 정보
                        bool isOneWayNode_Down = space.IsOneWayNode_Down;
                        bool isOneWayNode_Up = space.IsOneWayNode_Up;
                        bool isOneWayNode_Left = space.IsOneWayNode_Left;
                        bool isOneWayNode_Right = space.IsOneWayNode_Right;

                        //. 단일 단방향 노드 정보
                        bool isOnlyNode_Down = space.IsOnlyNode_Down;
                        bool isOnlyNode_Up = space.IsOnlyNode_Up;
                        bool isOnlyNode_Left = space.IsOnlyNode_Left;
                        bool isOnlyNode_Right = space.IsOnlyNode_Right;

                        //. 양방향 노드 정보
                        bool isTwoWayNode_Vertical = space.IsTwoWayNode_Vertical;
                        bool isTwoWayNode_Horizontal = space.IsTwoWayNode_Horizontal;


                        //. 방향별 노드 가용 여부
                        bool hasNode_Down = space.HasNode_Down;
                        bool hasNode_Up = space.HasNode_Up;
                        bool hasNode_Left = space.HasNode_Left;
                        bool hasNode_Right = space.HasNode_Right;

                        #endregion


                        //. 해당 방향의 노드가 가용중이라면, 해당 방향에 존재하는 모든 방의 ActivatedRectExpand를 합쳐 Rect로 얻는다
                        Rect combinedRect_ActivatedTFRectExpand_Down = hasNode_Down ? getConnectingRooms_CombinedRect_ActivatedTransformRectExpand(space.ConnectingSpaces_Down) : new Rect();
                        Rect combinedRect_ActivatedTFRectExpand_Up = hasNode_Up ? getConnectingRooms_CombinedRect_ActivatedTransformRectExpand(space.ConnectingSpaces_Up) : new Rect();
                        Rect combinedRect_ActivatedTFRectExpand_Left = hasNode_Left ? getConnectingRooms_CombinedRect_ActivatedTransformRectExpand(space.ConnectingSpaces_Left) : new Rect();
                        Rect combinedRect_ActivatedTFRectExpand_Right = hasNode_Right ? getConnectingRooms_CombinedRect_ActivatedTransformRectExpand(space.ConnectingSpaces_Right) : new Rect();


                        //? #1 "단일축" "단방향" 노드를 연결된 방들의 가장 끝부분에 이동(밀착) 되게 한다
                        move_OnlyOneAxisOneWayNode_MaxClamp_Vertical();
                        move_OnlyOneAxisOneWayNode_MaxClamp_Horizontal();
                        void move_OnlyOneAxisOneWayNode_MaxClamp_Vertical()
                        {
                            //? "하단 or 상단" 노드를 사용하고, "좌측 or 우측" 노드를 사용중이지 않은, 단일축 단방향 노드일경우
                            if (hasNode_Vertical && !hasNode_Horizontal)
                            {
                                //. "하단" 노드만 사용할경우,
                                //. 하단 노드에 연결된 모든 방의 Transform Activated Rect Expand를 구해,
                                //. 그 끝거리(yMax)만큼 이동하여, 방을 하단 방향의 방쪽으로 이동하게 한다
                                if (isOneWayNode_Down) { moveVelocity_byVerticalNode.y += combinedRect_ActivatedTFRectExpand_Down.yMax - roomActivatedTransformRectExpand.yMin; }

                                //. "상단" 노드만 사용할경우,
                                //. 상단 노드에 연결된 모든 방의 Transform Activated Rect Expand를 구해,
                                //. 그 끝거리(yMin)만큼 이동하여, 방을 상단 방향의 방쪽으로 이동하게 한다
                                if (isOneWayNode_Up) { moveVelocity_byVerticalNode.y += combinedRect_ActivatedTFRectExpand_Up.yMin - roomActivatedTransformRectExpand.yMax; }
                            }
                        }
                        void move_OnlyOneAxisOneWayNode_MaxClamp_Horizontal()
                        {
                            //? "좌측 or 우측" 노드를 사용하고, "하단 or 상단" 노드를 사용중이지 않은, 단일축 단방향 노드일경우
                            if (hasNode_Horizontal && !hasNode_Vertical)
                            {
                                //. "좌측" 노드만 사용할경우,
                                //. 좌측 노드에 연결된 모든 방의 Transform Activated Rect Expand를 구해,
                                //. 그 끝거리(xMax)만큼 이동하여, 방을 좌측 방향의 방쪽으로 이동하게 한다
                                if (isOneWayNode_Left) { moveVelocity_byHorizontalNode.x += combinedRect_ActivatedTFRectExpand_Left.xMax - roomActivatedTransformRectExpand.xMin; }

                                //. "우측" 노드만 사용할경우,
                                //. 우측 노드에 연결된 모든 방의 Transform Activated Rect Expand를 구해,
                                //. 그 끝거리(xMin)만큼 이동하여, 방을 우측 방향의 방쪽으로 이동하게 한다
                                if (isOneWayNode_Right) { moveVelocity_byHorizontalNode.x += combinedRect_ActivatedTFRectExpand_Right.xMin - roomActivatedTransformRectExpand.xMax; }
                            }
                        }


                        //? #2 "양방향" 노드를 제외한, 가로 세로축을 동시에 사용하는 노드들을 이동한다 (대각 노드)
                        //!     (양방향 노드는 제외된다)
                        //.     (하단+좌측, 하단+우측, 상단+좌측, 상단+우측)
                        move_DoubleAxes();
                        void move_DoubleAxes()
                        {
                            //? 대각선 노드 조건 확인 (양방향 노드 제외)
                            if ((hasNode_Vertical && hasNode_Horizontal) && !isTwoWayNode_Vertical && !isTwoWayNode_Horizontal)
                            {
                                //. (좌측 or 우측) or (하단 or 상단)에 있는 RoomActivatedRectExpand가 합쳐진 Rect가 지정될 예정
                                Rect currentCombinedRect_ActivatedTFRectExpand;


                                moveAxisY();
                                moveAxisX();

                                void moveAxisY()
                                {
                                    //. 합Rect에 (하단 or 상단)에 따라 맞는 합Rect를 지정한다
                                    currentCombinedRect_ActivatedTFRectExpand = hasNode_Down ? combinedRect_ActivatedTFRectExpand_Down : combinedRect_ActivatedTFRectExpand_Up;

                                    //? 좌측 + (하단 or 상단)
                                    if (hasNode_Left)
                                    {
                                        //. "좌측 합Rect"에 이동(밀착) 하기
                                        float moveLeft_Max = combinedRect_ActivatedTFRectExpand_Left.xMax - roomActivatedTransformRectExpand.xMin;

                                        //? 좌측에 이미 밀착된 상태가 아닐때만 연산 (0이라면 이미 밀착)
                                        if (moveLeft_Max != 0f)
                                        {
                                            //. "하단 or 상단 의 합Rect"의 중심점보다 더 "좌측"으로 가지 않도록 제약 (단, 오프셋을 포함해 테두리가 닿을때까진 OK)
                                            float moveLeft_Clamped = currentCombinedRect_ActivatedTFRectExpand.center.x - roomActivatedTransformRectExpandHalfSize.x - roomCetnerWithOffset.x;
                                            //! 좌측 이동은 음수 → 더 큰 값을 허용
                                            moveVelocity_byHorizontalNode.x += Mathf.Floor(Mathf.Max(moveLeft_Max, moveLeft_Clamped));
                                        }
                                    }

                                    //? 우측 + (하단 or 상단)
                                    else if (hasNode_Right)
                                    {
                                        //. "우측 합Rect"에 이동(밀착) 하기
                                        float moveRight_Max = combinedRect_ActivatedTFRectExpand_Right.xMin - roomActivatedTransformRectExpand.xMax;

                                        //? 우측에 이미 밀착된 상태가 아닐때만 연산 (0이라면 이미 밀착)
                                        if (moveRight_Max != 0f)
                                        {
                                            //. "하단 or 상단 의 합Rect"의 중심점보다 더 "우측"으로 가지 않도록 제약 (단, 오프셋을 포함해 테두리가 닿을때까진 OK)
                                            float moveRight_Clamped = currentCombinedRect_ActivatedTFRectExpand.center.x + roomActivatedTransformRectExpandHalfSize.x - roomCetnerWithOffset.x;
                                            //! 우측 이동은 양수 → 더 작은 값을 허용
                                            moveVelocity_byHorizontalNode.x += Mathf.Floor(Mathf.Min(moveRight_Max, moveRight_Clamped));
                                        }
                                    }
                                }
                                void moveAxisX()
                                {
                                    //. 좌측 or 우측 합Rect
                                    currentCombinedRect_ActivatedTFRectExpand = hasNode_Left ? combinedRect_ActivatedTFRectExpand_Left : combinedRect_ActivatedTFRectExpand_Right;

                                    //? 하단 + (좌측 or 우측)
                                    if (hasNode_Down)
                                    {
                                        //. "하단 합Rect"에 이동(밀착) 하기
                                        float moveDown_Max = combinedRect_ActivatedTFRectExpand_Down.yMax - roomActivatedTransformRectExpand.yMin;

                                        //? 하단에 이미 밀착된 상태가 아닐때만 연산 (0이라면 이미 밀착)
                                        if (moveDown_Max != 0f)
                                        {
                                            //. "좌측 or 우측 의 합Rect"의 중심점보다 더 "하단"으로 가지 않도록 제약 (단, 오프셋을 포함해 테두리가 닿을때까진 OK)
                                            float moveDown_Clamped = currentCombinedRect_ActivatedTFRectExpand.center.y - roomActivatedTransformRectExpandHalfSize.y - roomCetnerWithOffset.y;
                                            //! 하단 이동은 음수 → 더 큰 값을 허용
                                            moveVelocity_byVerticalNode.y += Mathf.Floor(Mathf.Max(moveDown_Max, moveDown_Clamped));
                                        }
                                    }

                                    //? 상단 + (좌측 or 우측)
                                    else if (hasNode_Up)
                                    {
                                        //. "상단 합Rect"에 이동(밀착) 하기
                                        float moveUp_Max = combinedRect_ActivatedTFRectExpand_Up.yMin - roomActivatedTransformRectExpand.yMax;

                                        //? 상단에 이미 밀착된 상태가 아닐때만 연산 (0이라면 이미 밀착)
                                        if (moveUp_Max != 0f)
                                        {
                                            //. "좌측 or 우측 의 합Rect"의 중심점보다 더 "상단"으로 가지 않도록 제약 (단, 오프셋을 포함해 테두리가 닿을때까진 OK)
                                            float moveUp_Clamped = currentCombinedRect_ActivatedTFRectExpand.center.y + roomActivatedTransformRectExpandHalfSize.y - roomCetnerWithOffset.y;
                                            //! 상단 이동은 양수 → 더 작은 값을 허용
                                            moveVelocity_byVerticalNode.y += Mathf.Floor(Mathf.Min(moveUp_Max, moveUp_Clamped));
                                        }
                                    }
                                }
                            }
                        }



                        //? #3 "양방향" 노드의 인접을 시작한다
                        //!     기준 방은 고정하고, 해당 축의 반대편 ‘덩어리’를 제약 범위 내에서 기준 쪽으로 일괄 끌어온다
                        moveTwoWayNode();
                        void moveTwoWayNode()
                        {
                            //? 양방향 - 하단 or 상단
                            if (isTwoWayNode_Vertical)
                            {
                                //? [수직·하단] 기준 하단선과( center.yMin ) 하단 덩어리의 yMax 사이에 ‘양수 갭’ 존재
                                //!  → 하단 서브그래프가 기준 방과 떨어져 있으므로 위(+Y)로 당겨 붙인다
                                //.  (제약: 스테이지 경계/이웃충돌/반대축 평균선, 스냅 후 ΔY 누적 적용)
                                if (hasNode_Down && roomActivatedTransformRectExpand.yMin - combinedRect_ActivatedTFRectExpand_Down.yMax > 0f)
                                {
                                    pullComponentTowardCenter_WithConstraints(main, spaceList, space, roomActivatedTransformRectExpand, stageTransformRect, EDirection4.Down);
                                }

                                //? [수직·상단] 상단 덩어리의 yMin 과 기준 상단선( center.yMax ) 사이에 ‘양수 갭’ 존재
                                //!  → 상단 서브그래프가 기준 방과 떨어져 있으므로 아래(−Y)로 당겨 붙인다
                                //.  (제약 동일, 스냅 후 ΔY 누적 적용)
                                if (hasNode_Up && combinedRect_ActivatedTFRectExpand_Up.yMin - roomActivatedTransformRectExpand.yMax > 0f)
                                {
                                    pullComponentTowardCenter_WithConstraints(main, spaceList, space, roomActivatedTransformRectExpand, stageTransformRect, EDirection4.Up);
                                }
                            }

                            //? 양방향 - 좌측 or 우측
                            if (isTwoWayNode_Horizontal)
                            {
                                //? [수평·좌측] 기준 좌측선( center.xMin )과 좌측 덩어리의 xMax 사이에 ‘양수 갭’ 존재
                                //!  → 좌측 서브그래프가 기준 방과 떨어져 있으므로 오른쪽(+X)으로 당겨 붙인다
                                //.  (제약 동일, 스냅 후 ΔX 누적 적용)
                                if (hasNode_Left && roomActivatedTransformRectExpand.xMin - combinedRect_ActivatedTFRectExpand_Left.xMax > 0f)
                                {
                                    pullComponentTowardCenter_WithConstraints(main, spaceList, space, roomActivatedTransformRectExpand, stageTransformRect, EDirection4.Left);
                                }

                                //? [수평·우측] 우측 덩어리의 xMin 과 기준 우측선( center.xMax ) 사이에 ‘양수 갭’ 존재
                                //!  → 우측 서브그래프가 기준 방과 떨어져 있으므로 왼쪽(−X)으로 당겨 붙인다
                                //.  (제약 동일, 스냅 후 ΔX 누적 적용)
                                if (hasNode_Right && combinedRect_ActivatedTFRectExpand_Right.xMin - roomActivatedTransformRectExpand.xMax > 0f)
                                {
                                    pullComponentTowardCenter_WithConstraints(main, spaceList, space, roomActivatedTransformRectExpand, stageTransformRect, EDirection4.Right);
                                }
                            }
                        }


                        #region 내부 메서드 

                        //. 받아온 공간 내에 존재하는 모든 방들의 ActivatedTransformRectExpand를 모두 하나로 합쳐, Rect로 반환
                        Rect getConnectingRooms_CombinedRect_ActivatedTransformRectExpand(IReadOnlyList<Space> connectingSpaces)
                        {
                            float xMin = float.MaxValue;
                            float xMax = float.MinValue;
                            float yMin = float.MaxValue;
                            float yMax = float.MinValue;

                            for (int i = 0; i < connectingSpaces.Count; i++)
                            {
                                Space connectSpace = connectingSpaces[i];

                                //! 방이 있는것들만 해당됨
                                if (connectSpace.PlacedRoom == null) { continue; }

                                var rect = main.PlaceM.PlacedRoomTFInfoCacheM.GetRoomActivatedRectExpand_byCache(connectSpace.PlacedRoom);

                                if (xMin > rect.xMin) { xMin = rect.xMin; }
                                if (xMax < rect.xMax) { xMax = rect.xMax; }
                                if (yMin > rect.yMin) { yMin = rect.yMin; }
                                if (yMax < rect.yMax) { yMax = rect.yMax; }
                            }

                            return SU_TF_Rect.RectFromCorners(new Vector2(xMin, yMin), new Vector2(xMax, yMax));
                        }


                        #region 양방향 통합 이전 코드
                        ////. ─────────────────────────────────────────────────────────────────────────────
                        ////. [NEW] 기준 방을 중심으로, 양방향(상·하 / 좌·우) 이웃 방들을
                        ////.       "제약된 범위" 안에서 기준 방 쪽으로 끌어오는 보조 메서드들
                        ////.       (기준 방은 고정, 이웃 방들을 이동)
                        ////.       - ActivatedTransformRectExpand 기준
                        ////.       - 반대축 연결 상태 고려하여 Clamp
                        ////.       - GridUnit, StageRect, Swizzle 모두 반영
                        ////. ─────────────────────────────────────────────────────────────────────────────

                        //void PullVerticalComponentTowardCenter_WithConstraints(
                        //    StageGenerator _main,
                        //    Space centerSpace,
                        //    in CustomRect2DCentered centerRectTF,
                        //    in Rect stageRect,
                        //    EDirection4 direction,      //. Down → 위로 끌기, Up → 아래로 끌기
                        //    int maxDepth = 256
                        //)
                        //{
                        //    if (direction != EDirection4.Down && direction != EDirection4.Up) return;

                        //    var snap = _main.Setting.SnapSetting;
                        //    var swz = snap.Swizzle;

                        //    float center_yMin = centerRectTF.yMin;
                        //    float center_yMax = centerRectTF.yMax;

                        //    //. ─────────────────────────────────────
                        //    //. 1) ‘한쪽 반공간’ 서브그래프 수집 (BFS)
                        //    //.    Down: 기준 하단선 이하(yMax <= center_yMin)만 포함
                        //    //.    Up  : 기준 상단선 이상(yMin >= center_yMax)만 포함
                        //    //. ─────────────────────────────────────
                        //    bool IsOnSide(Space s)
                        //    {
                        //        if (s?.PlacedRoom == null) return false;
                        //        //var r = s.PlacedRoom.GetInstanceRoomActivatedTransformRectExpand(_main.Setting);
                        //        var r = main.PlaceM.GetRoomActivatedRectExpand_byCache(s.PlacedRoom);
                        //        return (direction == EDirection4.Down) ? (r.yMax <= center_yMin) : (r.yMin >= center_yMax);
                        //    }

                        //    var comp = new HashSet<Space>();
                        //    var q = new Queue<(Space s, int d)>();

                        //    IReadOnlyList<Space> firsts = (direction == EDirection4.Down) ? centerSpace.ConnectingSpaces_Down : centerSpace.ConnectingSpaces_Up;
                        //    if (firsts == null || firsts.Count == 0) return;

                        //    for (int i = 0; i < firsts.Count; i++)
                        //    {
                        //        var nx = firsts[i];
                        //        if (IsOnSide(nx) && comp.Add(nx)) q.Enqueue((nx, 0));
                        //    }

                        //    while (q.Count > 0)
                        //    {
                        //        var (cur, depth) = q.Dequeue();
                        //        if (depth >= maxDepth) continue;

                        //        void Enq(IReadOnlyList<Space> lst)
                        //        {
                        //            if (lst == null) return;
                        //            for (int k = 0; k < lst.Count; k++)
                        //            {
                        //                var nb = lst[k];
                        //                if (!IsOnSide(nb)) continue;
                        //                if (comp.Add(nb)) q.Enqueue((nb, depth + 1));
                        //            }
                        //        }

                        //        Enq(cur.ConnectingSpaces_Left);
                        //        Enq(cur.ConnectingSpaces_Right);
                        //        Enq(cur.ConnectingSpaces_Down);
                        //        Enq(cur.ConnectingSpaces_Up);
                        //    }

                        //    if (comp.Count == 0) return;

                        //    //. ─────────────────────────────────────
                        //    //. 2) 합Rect & 이상적 델타(부호 주의!)
                        //    //. ─────────────────────────────────────
                        //    Rect CompUnion()
                        //    {
                        //        float xMin = float.MaxValue, xMax = float.MinValue, yMin = float.MaxValue, yMax = float.MinValue;
                        //        foreach (var sp in comp)
                        //        {
                        //            //var r = sp.PlacedRoom.GetInstanceRoomActivatedTransformRectExpand(_main.Setting);
                        //            var r = main.PlaceM.GetRoomActivatedRectExpand_byCache(sp.PlacedRoom);
                        //            if (xMin > r.xMin) xMin = r.xMin;
                        //            if (xMax < r.xMax) xMax = r.xMax;
                        //            if (yMin > r.yMin) yMin = r.yMin;
                        //            if (yMax < r.yMax) yMax = r.yMax;
                        //        }
                        //        return SU_TF_Rect.RectFromCorners(new Vector2(xMin, yMin), new Vector2(xMax, yMax));
                        //    }

                        //    var compUnion = CompUnion();

                        //    //. 방향별 올바른 부호
                        //    float idealDelta =
                        //        (direction == EDirection4.Down)
                        //        ? (center_yMin - compUnion.yMax)      //. 아래 덩어리를 ‘위로’ (+) 끌기
                        //        : (center_yMax - compUnion.yMin);     //. 위   덩어리를 ‘아래로’ (−) 끌기

                        //    if (Mathf.Approximately(idealDelta, 0f)) return;

                        //    //. ─────────────────────────────────────
                        //    //. 3) 외부 이웃 기준 제약 → 덩어리 공통 Δ는 최소 허용치
                        //    //.    내부(comp) 이웃은 제외 (같이 움직이므로 제약 X)
                        //    //. ─────────────────────────────────────
                        //    float globalDelta = idealDelta;

                        //    foreach (var sp in comp)
                        //    {
                        //        //var r = sp.PlacedRoom.GetInstanceRoomActivatedTransformRectExpand(_main.Setting);
                        //        var r = main.PlaceM.GetRoomActivatedRectExpand_byCache(sp.PlacedRoom);

                        //        //. 스테이지 경계
                        //        float minY = stageRect.yMin - r.yMin;   //. 아래(-)
                        //        float maxY = stageRect.yMax - r.yMax;   //. 위(+)

                        //        float allow = Mathf.Clamp(idealDelta, minY, maxY);

                        //        //. 좌/우 ‘외부’ 평균 선 클램프
                        //        float ClampByExternalLR(float want)
                        //        {
                        //            int cnt = 0; Vector2 sum = Vector2.zero;

                        //            void Acc(IReadOnlyList<Space> lst)
                        //            {
                        //                if (lst == null) return;
                        //                for (int i = 0; i < lst.Count; i++)
                        //                {
                        //                    var nb = lst[i];
                        //                    if (nb?.PlacedRoom == null) continue;
                        //                    if (comp.Contains(nb)) continue; //. 내부 제외
                        //                    cnt++;
                        //                    sum += getRoomCenter_bySpace(main, nb, 2);
                        //                }
                        //            }
                        //            Acc(sp.ConnectingSpaces_Left);
                        //            Acc(sp.ConnectingSpaces_Right);

                        //            if (cnt == 0) return want;

                        //            float avgY = (sum / cnt).y;
                        //            float halfH = r.Size.y * 0.5f;

                        //            if (want > 0f) // 위로
                        //            {
                        //                float yMinAfter = r.yMin + want;
                        //                float yMinMax = avgY - halfH;
                        //                if (yMinAfter > yMinMax) want = Mathf.Max(0f, yMinMax - r.yMin);
                        //            }
                        //            else if (want < 0f) // 아래로
                        //            {
                        //                float yMaxAfter = r.yMax + want;
                        //                float yMaxMin = avgY + halfH;
                        //                if (yMaxAfter < yMaxMin) want = Mathf.Min(0f, yMaxMin - r.yMax);
                        //            }
                        //            return want;
                        //        }

                        //        //. 상/하 ‘외부’ 이웃 클램프
                        //        float ClampByExternalUpDown(float want)
                        //        {
                        //            if (want > 0f) // 위로
                        //            {
                        //                float limit = float.PositiveInfinity;
                        //                var ups = sp.ConnectingSpaces_Up;
                        //                if (ups != null)
                        //                {
                        //                    for (int i = 0; i < ups.Count; i++)
                        //                    {
                        //                        var nb = ups[i];
                        //                        if (nb?.PlacedRoom == null) continue;
                        //                        if (comp.Contains(nb)) continue;
                        //                        //var nr = nb.PlacedRoom.GetInstanceRoomActivatedTransformRectExpand(_main.Setting);
                        //                        var nr = main.PlaceM.GetRoomActivatedRectExpand_byCache(nb.PlacedRoom);
                        //                        limit = Mathf.Min(limit, nr.yMin - r.yMax);
                        //                    }
                        //                }
                        //                if (!float.IsInfinity(limit)) want = Mathf.Min(want, limit);
                        //            }
                        //            else if (want < 0f) // 아래로
                        //            {
                        //                float limit = float.NegativeInfinity;
                        //                var dns = sp.ConnectingSpaces_Down;
                        //                if (dns != null)
                        //                {
                        //                    for (int i = 0; i < dns.Count; i++)
                        //                    {
                        //                        var nb = dns[i];
                        //                        if (nb?.PlacedRoom == null) continue;
                        //                        if (comp.Contains(nb)) continue;
                        //                        //var nr = nb.PlacedRoom.GetInstanceRoomActivatedTransformRectExpand(_main.Setting);
                        //                        var nr = main.PlaceM.GetRoomActivatedRectExpand_byCache(nb.PlacedRoom);
                        //                        limit = Mathf.Max(limit, nr.yMax - r.yMin); // 음수 한계
                        //                    }
                        //                }
                        //                if (!float.IsNegativeInfinity(limit)) want = Mathf.Max(want, limit);
                        //            }
                        //            return want;
                        //        }

                        //        allow = ClampByExternalLR(allow);
                        //        allow = ClampByExternalUpDown(allow);

                        //        //. 덩어리 공통 Δ 갱신
                        //        if (idealDelta > 0f) globalDelta = Mathf.Min(globalDelta, allow);
                        //        else globalDelta = Mathf.Max(globalDelta, allow);
                        //    }

                        //    //. ─────────────────────────────────────
                        //    //. 4) 스냅 & 일괄 적용
                        //    //. ─────────────────────────────────────
                        //    float deltaY = globalDelta;
                        //    if (deltaY > 0f) deltaY = Mathf.Ceil(deltaY) * snap.GridUnitY_Height / snap.GridUnitY_Height;
                        //    else deltaY = -Mathf.Ceil(-deltaY) * snap.GridUnitY_Height / snap.GridUnitY_Height;

                        //    if (!Mathf.Approximately(deltaY, 0f))
                        //    {
                        //        foreach (var sp in comp)
                        //        {
                        //            //! 250813 
                        //            main.placeM.AddVelocity_byPlacedRoomTransformInfoCache(sp.PlacedRoom, new Vector2(0f, deltaY));
                        //            //sp.PlacedRoom.transform.position += new Vector2(0f, deltaY).SwizzlesVector2To3(swz);
                        //        }

                        //        //! 반복 수렴 플래그
                        //        isMovedSomething = true; //.!
                        //    }
                        //}


                        //// ─────────────────────────────────────────────────────────────────────────────
                        //// [NEW] 가로 "서브그래프"를 기준 방 쪽으로 당기기 (기준 방 고정, 덩어리 전체 동시 이동)
                        ////  - 시작: 기준 방의 Left(또는 Right) 이웃들
                        ////  - BFS로 좌/우/상/하 전방향 탐색하되, 기준선의 '한쪽 반공간'만 포함
                        ////  - 이동량: (기준과의 gap)과 각 방의 "외부 이웃" 제약을 모두 만족하는 공통 ΔX
                        //// ─────────────────────────────────────────────────────────────────────────────
                        //void PullHorizontalComponentTowardCenter_WithConstraints(
                        //    StageGenerator _main,
                        //    Space centerSpace,
                        //    in CustomRect2DCentered centerRectTF,
                        //    in Rect stageRect,
                        //    EDirection4 direction,      //. Left → 오른쪽(+X)으로 끌기, Right → 왼쪽(−X)으로 끌기
                        //    int maxDepth = 256
                        //)
                        //{
                        //    if (direction != EDirection4.Left && direction != EDirection4.Right) return;

                        //    var snap = _main.Setting.SnapSetting;
                        //    var swz = snap.Swizzle;

                        //    float center_xMin = centerRectTF.xMin;
                        //    float center_xMax = centerRectTF.xMax;

                        //    // 1) '한쪽 반공간' 서브그래프 수집 (BFS)
                        //    //    Left : 기준 좌측선 이하(xMax <= center_xMin)만 포함
                        //    //    Right: 기준 우측선 이상(xMin >= center_xMax)만 포함
                        //    bool IsOnSide(Space s)
                        //    {
                        //        if (s?.PlacedRoom == null) return false;
                        //        //var r = s.PlacedRoom.GetInstanceRoomActivatedTransformRectExpand(_main.Setting);
                        //        var r = main.PlaceM.GetRoomActivatedRectExpand_byCache(s.PlacedRoom);
                        //        return (direction == EDirection4.Left) ? (r.xMax <= center_xMin) : (r.xMin >= center_xMax);
                        //    }

                        //    var comp = new HashSet<Space>();
                        //    var q = new Queue<(Space s, int d)>();

                        //    IReadOnlyList<Space> firsts = (direction == EDirection4.Left) ? centerSpace.ConnectingSpaces_Left : centerSpace.ConnectingSpaces_Right;
                        //    if (firsts == null || firsts.Count == 0) return;

                        //    for (int i = 0; i < firsts.Count; i++)
                        //    {
                        //        var nx = firsts[i];
                        //        if (IsOnSide(nx) && comp.Add(nx)) q.Enqueue((nx, 0));
                        //    }

                        //    while (q.Count > 0)
                        //    {
                        //        var (cur, depth) = q.Dequeue();
                        //        if (depth >= maxDepth) continue;

                        //        void Enq(IReadOnlyList<Space> lst)
                        //        {
                        //            if (lst == null) return;
                        //            for (int k = 0; k < lst.Count; k++)
                        //            {
                        //                var nb = lst[k];
                        //                if (!IsOnSide(nb)) continue;
                        //                if (comp.Add(nb)) q.Enqueue((nb, depth + 1));
                        //            }
                        //        }

                        //        Enq(cur.ConnectingSpaces_Left);
                        //        Enq(cur.ConnectingSpaces_Right);
                        //        Enq(cur.ConnectingSpaces_Down);
                        //        Enq(cur.ConnectingSpaces_Up);
                        //    }

                        //    if (comp.Count == 0) return;

                        //    // 2) 합Rect & 이상적 ΔX(부호 주의)
                        //    Rect CompUnion()
                        //    {
                        //        float xMin = float.MaxValue, xMax = float.MinValue, yMin = float.MaxValue, yMax = float.MinValue;
                        //        foreach (var sp in comp)
                        //        {
                        //            //var r = sp.PlacedRoom.GetInstanceRoomActivatedTransformRectExpand(_main.Setting);
                        //            var r = main.PlaceM.GetRoomActivatedRectExpand_byCache(sp.PlacedRoom);
                        //            if (xMin > r.xMin) xMin = r.xMin;
                        //            if (xMax < r.xMax) xMax = r.xMax;
                        //            if (yMin > r.yMin) yMin = r.yMin;
                        //            if (yMax < r.yMax) yMax = r.yMax;
                        //        }
                        //        return SU_TF_Rect.RectFromCorners(new Vector2(xMin, yMin), new Vector2(xMax, yMax));
                        //    }

                        //    var compUnion = CompUnion();

                        //    // Left  덩어리 → 오른쪽(+X)으로: center_xMin - compUnion.xMax
                        //    // Right 덩어리 → 왼쪽(−X)으로 : center_xMax - compUnion.xMin (보통 음수)
                        //    float idealDelta =
                        //        (direction == EDirection4.Left)
                        //        ? (center_xMin - compUnion.xMax)
                        //        : (center_xMax - compUnion.xMin);

                        //    if (Mathf.Approximately(idealDelta, 0f)) return;

                        //    // 3) 외부 이웃 기준 제약 → 덩어리 공통 ΔX
                        //    float globalDelta = idealDelta;

                        //    foreach (var sp in comp)
                        //    {
                        //        //var r = sp.PlacedRoom.GetInstanceRoomActivatedTransformRectExpand(_main.Setting);
                        //        var r = main.PlaceM.GetRoomActivatedRectExpand_byCache(sp.PlacedRoom);

                        //        // 스테이지 경계
                        //        float minX = stageRect.xMin - r.xMin;   // 왼쪽(−)
                        //        float maxX = stageRect.xMax - r.xMax;   // 오른쪽(+)

                        //        float allow = Mathf.Clamp(idealDelta, minX, maxX);

                        //        // (A) 상/하 '외부' 평균선 클램프 (내부는 제외) → 너무 X로 치우치지 않도록
                        //        float ClampByExternalUD(float want)
                        //        {
                        //            int cnt = 0; Vector2 sum = Vector2.zero;

                        //            void Acc(IReadOnlyList<Space> lst)
                        //            {
                        //                if (lst == null) return;
                        //                for (int i = 0; i < lst.Count; i++)
                        //                {
                        //                    var nb = lst[i];
                        //                    if (nb?.PlacedRoom == null) continue;
                        //                    if (comp.Contains(nb)) continue; // 내부 제외
                        //                    cnt++;
                        //                    sum += getRoomCenter_bySpace(main, nb, 2);
                        //                }
                        //            }
                        //            Acc(sp.ConnectingSpaces_Up);
                        //            Acc(sp.ConnectingSpaces_Down);

                        //            if (cnt == 0) return want;

                        //            float avgX = (sum / cnt).x;
                        //            float halfW = r.Size.x * 0.5f;

                        //            if (want > 0f) // 오른쪽으로
                        //            {
                        //                float xMinAfter = r.xMin + want;
                        //                float xMinMax = avgX - halfW;
                        //                if (xMinAfter > xMinMax) want = Mathf.Max(0f, xMinMax - r.xMin);
                        //            }
                        //            else if (want < 0f) // 왼쪽으로
                        //            {
                        //                float xMaxAfter = r.xMax + want; // want<0
                        //                float xMaxMin = avgX + halfW;
                        //                if (xMaxAfter < xMaxMin) want = Mathf.Min(0f, xMaxMin - r.xMax);
                        //            }
                        //            return want;
                        //        }

                        //        // (B) 좌/우 '외부' 이웃과의 겹침 방지 (내부는 제외)
                        //        float ClampByExternalLR(float want)
                        //        {
                        //            if (want > 0f) // 오른쪽으로 당길 때: 오른쪽 이웃들의 xMin을 넘지 않게
                        //            {
                        //                float limit = float.PositiveInfinity;
                        //                var rights = sp.ConnectingSpaces_Right;
                        //                if (rights != null)
                        //                {
                        //                    for (int i = 0; i < rights.Count; i++)
                        //                    {
                        //                        var nb = rights[i];
                        //                        if (nb?.PlacedRoom == null) continue;
                        //                        if (comp.Contains(nb)) continue;
                        //                        //var nr = nb.PlacedRoom.GetInstanceRoomActivatedTransformRectExpand(_main.Setting);
                        //                        var nr = main.PlaceM.GetRoomActivatedRectExpand_byCache(nb.PlacedRoom);
                        //                        limit = Mathf.Min(limit, nr.xMin - r.xMax);
                        //                    }
                        //                }
                        //                if (!float.IsInfinity(limit)) want = Mathf.Min(want, limit);
                        //            }
                        //            else if (want < 0f) // 왼쪽으로 당길 때: 왼쪽 이웃들의 xMax를 넘지 않게
                        //            {
                        //                float limit = float.NegativeInfinity;
                        //                var lefts = sp.ConnectingSpaces_Left;
                        //                if (lefts != null)
                        //                {
                        //                    for (int i = 0; i < lefts.Count; i++)
                        //                    {
                        //                        var nb = lefts[i];
                        //                        if (nb?.PlacedRoom == null) continue;
                        //                        if (comp.Contains(nb)) continue;
                        //                        //var nr = nb.PlacedRoom.GetInstanceRoomActivatedTransformRectExpand(_main.Setting);
                        //                        var nr = main.PlaceM.GetRoomActivatedRectExpand_byCache(nb.PlacedRoom);
                        //                        limit = Mathf.Max(limit, nr.xMax - r.xMin); // 음수 한계
                        //                    }
                        //                }
                        //                if (!float.IsNegativeInfinity(limit)) want = Mathf.Max(want, limit);
                        //            }
                        //            return want;
                        //        }

                        //        allow = ClampByExternalUD(allow);
                        //        allow = ClampByExternalLR(allow);

                        //        // 덩어리 공통 ΔX 갱신 (부호에 따라 Min/Max)
                        //        if (idealDelta > 0f) globalDelta = Mathf.Min(globalDelta, allow);
                        //        else globalDelta = Mathf.Max(globalDelta, allow);
                        //    }

                        //    // 4) 스냅 & 일괄 적용
                        //    float deltaX = (globalDelta > 0f)
                        //        ? Mathf.Ceil(globalDelta) * snap.GridUnitX_Width / snap.GridUnitX_Width
                        //        : -Mathf.Ceil(-globalDelta) * snap.GridUnitX_Width / snap.GridUnitX_Width;

                        //    if (!Mathf.Approximately(deltaX, 0f))
                        //    {
                        //        foreach (var sp in comp)
                        //        {
                        //            //! 250813 
                        //            main.placeM.AddVelocity_byPlacedRoomTransformInfoCache(sp.PlacedRoom, new Vector2(deltaX, 0f));
                        //            //sp.PlacedRoom.transform.position += new Vector2(deltaX, 0f).SwizzlesVector2To3(swz);
                        //        }

                        //        isMovedSomething = true; // 반복 수렴 유도
                        //    }
                        //} 
                        #endregion

                        #endregion


                        //. 총 Velocity를 합산하여, GridUnit 연산을 하고, 공간을 벗어나지않게 제약한뒤, 적용한다
                        var combineVelocity = moveVelocity_byVerticalNode + moveVelocity_byHorizontalNode;
                        bool currentMovedSomething = ApplyVelocity_RoomNearestRoom(main, spaceList, space.PlacedRoom, combineVelocity, stageTransformRect);

                        //? 연산된 Velocity가 이동값이 (0,0)이 아니라면, 이동 여부 플래그를 활성화시킨다
                        if (!isMovedSomething && currentMovedSomething) { isMovedSomething = true; }
                    }


                    return isMovedSomething;
                }



                private async UniTask<bool> MoveNearestRooms_SecondAsync(StageGenerator main, Transform parent, IReadOnlyList<Space> spaceList)
                {
                    bool isMovedSomething = false;

                    var stageTransformRect = main.TransformM.StageTransformRect;

                    int spaceOderIndex_RoomNearest = 0;
                    int yieldCount = 0;

                    //! 별도로 정렬하지않아도 만족스러운 결과가 나오는 상태
                    foreach (Space space in spaceList)
                    {
                        space.OrderIndex_RoomNearest = spaceOderIndex_RoomNearest;
                        spaceOderIndex_RoomNearest++;

                        //! 방이 없다면, 인접할 방도 뭣도 없으니 continue
                        if (space.PlacedRoom == null) { continue; }


                        #region 내부 필드

                        //. 확장된 방 Activated TransformRect 기준으로 연산한다
                        var roomActivatedTransformRectExpand = main.PlaceM.PlacedRoomTFInfoCacheM.GetRoomActivatedRectExpand_byCache(space.PlacedRoom);
                        var roomActivatedTransformRectExpandHalfSize = roomActivatedTransformRectExpand.Size * 0.5f;

                        //. 중심점 또한!
                        var roomCetnerWithOffset = roomActivatedTransformRectExpand.CenterWithOffset;
                        var roomCetner = roomActivatedTransformRectExpand.Center;


                        //. 세로 노드 (하단, 상단) 의 의한 이동 Velocity
                        Vector2 moveVelocity_byVerticalNode = Vector2.zero;
                        //. 가로 노드 (좌측, 우측) 의 의한 이동 Velocity
                        Vector2 moveVelocity_byHorizontalNode = Vector2.zero;


                        //. 방향별 노드 개수
                        int nodeCount_Down = space.GetCurrentNodeCount(EDirection4.Down);
                        int nodeCount_Up = space.GetCurrentNodeCount(EDirection4.Up);
                        int nodeCount_Left = space.GetCurrentNodeCount(EDirection4.Left);
                        int nodeCount_Right = space.GetCurrentNodeCount(EDirection4.Right);

                        //. 세로, 가로 노드 정보
                        bool hasNode_Vertical = space.HasNode_Vertical;
                        bool hasNode_Horizontal = space.HasNode_Horizontal;

                        //. 단방향 노드 정보
                        bool isOneWayNode_Down = space.IsOneWayNode_Down;
                        bool isOneWayNode_Up = space.IsOneWayNode_Up;
                        bool isOneWayNode_Left = space.IsOneWayNode_Left;
                        bool isOneWayNode_Right = space.IsOneWayNode_Right;

                        //. 단일 단방향 노드 정보
                        bool isOnlyNode_Down = space.IsOnlyNode_Down;
                        bool isOnlyNode_Up = space.IsOnlyNode_Up;
                        bool isOnlyNode_Left = space.IsOnlyNode_Left;
                        bool isOnlyNode_Right = space.IsOnlyNode_Right;

                        //. 양방향 노드 정보
                        bool isTwoWayNode_Vertical = space.IsTwoWayNode_Vertical;
                        bool isTwoWayNode_Horizontal = space.IsTwoWayNode_Horizontal;


                        //. 방향별 노드 가용 여부
                        bool hasNode_Down = space.HasNode_Down;
                        bool hasNode_Up = space.HasNode_Up;
                        bool hasNode_Left = space.HasNode_Left;
                        bool hasNode_Right = space.HasNode_Right;

                        #endregion


                        //. 해당 방향의 노드가 가용중이라면, 해당 방향에 존재하는 모든 방의 ActivatedRectExpand를 합쳐 Rect로 얻는다
                        Rect combinedRect_ActivatedTFRectExpand_Down = hasNode_Down ? getConnectingRooms_CombinedRect_ActivatedTransformRectExpand(space.ConnectingSpaces_Down) : new Rect();
                        Rect combinedRect_ActivatedTFRectExpand_Up = hasNode_Up ? getConnectingRooms_CombinedRect_ActivatedTransformRectExpand(space.ConnectingSpaces_Up) : new Rect();
                        Rect combinedRect_ActivatedTFRectExpand_Left = hasNode_Left ? getConnectingRooms_CombinedRect_ActivatedTransformRectExpand(space.ConnectingSpaces_Left) : new Rect();
                        Rect combinedRect_ActivatedTFRectExpand_Right = hasNode_Right ? getConnectingRooms_CombinedRect_ActivatedTransformRectExpand(space.ConnectingSpaces_Right) : new Rect();


                        //? #1 "단일축" "단방향" 노드를 연결된 방들의 가장 끝부분에 이동(밀착) 되게 한다
                        move_OnlyOneAxisOneWayNode_MaxClamp_Vertical();
                        move_OnlyOneAxisOneWayNode_MaxClamp_Horizontal();
                        void move_OnlyOneAxisOneWayNode_MaxClamp_Vertical()
                        {
                            //? "하단 or 상단" 노드를 사용하고, "좌측 or 우측" 노드를 사용중이지 않은, 단일축 단방향 노드일경우
                            if (hasNode_Vertical && !hasNode_Horizontal)
                            {
                                //. "하단" 노드만 사용할경우,
                                //. 하단 노드에 연결된 모든 방의 Transform Activated Rect Expand를 구해,
                                //. 그 끝거리(yMax)만큼 이동하여, 방을 하단 방향의 방쪽으로 이동하게 한다
                                if (isOneWayNode_Down) { moveVelocity_byVerticalNode.y += combinedRect_ActivatedTFRectExpand_Down.yMax - roomActivatedTransformRectExpand.yMin; }

                                //. "상단" 노드만 사용할경우,
                                //. 상단 노드에 연결된 모든 방의 Transform Activated Rect Expand를 구해,
                                //. 그 끝거리(yMin)만큼 이동하여, 방을 상단 방향의 방쪽으로 이동하게 한다
                                if (isOneWayNode_Up) { moveVelocity_byVerticalNode.y += combinedRect_ActivatedTFRectExpand_Up.yMin - roomActivatedTransformRectExpand.yMax; }
                            }
                        }
                        void move_OnlyOneAxisOneWayNode_MaxClamp_Horizontal()
                        {
                            //? "좌측 or 우측" 노드를 사용하고, "하단 or 상단" 노드를 사용중이지 않은, 단일축 단방향 노드일경우
                            if (hasNode_Horizontal && !hasNode_Vertical)
                            {
                                //. "좌측" 노드만 사용할경우,
                                //. 좌측 노드에 연결된 모든 방의 Transform Activated Rect Expand를 구해,
                                //. 그 끝거리(xMax)만큼 이동하여, 방을 좌측 방향의 방쪽으로 이동하게 한다
                                if (isOneWayNode_Left) { moveVelocity_byHorizontalNode.x += combinedRect_ActivatedTFRectExpand_Left.xMax - roomActivatedTransformRectExpand.xMin; }

                                //. "우측" 노드만 사용할경우,
                                //. 우측 노드에 연결된 모든 방의 Transform Activated Rect Expand를 구해,
                                //. 그 끝거리(xMin)만큼 이동하여, 방을 우측 방향의 방쪽으로 이동하게 한다
                                if (isOneWayNode_Right) { moveVelocity_byHorizontalNode.x += combinedRect_ActivatedTFRectExpand_Right.xMin - roomActivatedTransformRectExpand.xMax; }
                            }
                        }


                        //? #2 "양방향" 노드를 제외한, 가로 세로축을 동시에 사용하는 노드들을 이동한다 (대각 노드)
                        //!     (양방향 노드는 제외된다)
                        //.     (하단+좌측, 하단+우측, 상단+좌측, 상단+우측)
                        move_DoubleAxes();
                        void move_DoubleAxes()
                        {
                            //? 대각선 노드 조건 확인 (양방향 노드 제외)
                            if ((hasNode_Vertical && hasNode_Horizontal) && !isTwoWayNode_Vertical && !isTwoWayNode_Horizontal)
                            {
                                //. (좌측 or 우측) or (하단 or 상단)에 있는 RoomActivatedRectExpand가 합쳐진 Rect가 지정될 예정
                                Rect currentCombinedRect_ActivatedTFRectExpand;


                                moveAxisY();
                                moveAxisX();

                                void moveAxisY()
                                {
                                    //. 합Rect에 (하단 or 상단)에 따라 맞는 합Rect를 지정한다
                                    currentCombinedRect_ActivatedTFRectExpand = hasNode_Down ? combinedRect_ActivatedTFRectExpand_Down : combinedRect_ActivatedTFRectExpand_Up;

                                    //? 좌측 + (하단 or 상단)
                                    if (hasNode_Left)
                                    {
                                        //. "좌측 합Rect"에 이동(밀착) 하기
                                        float moveLeft_Max = combinedRect_ActivatedTFRectExpand_Left.xMax - roomActivatedTransformRectExpand.xMin;

                                        //? 좌측에 이미 밀착된 상태가 아닐때만 연산 (0이라면 이미 밀착)
                                        if (moveLeft_Max != 0f)
                                        {
                                            //. "하단 or 상단 의 합Rect"의 중심점보다 더 "좌측"으로 가지 않도록 제약 (단, 오프셋을 포함해 테두리가 닿을때까진 OK)
                                            float moveLeft_Clamped = currentCombinedRect_ActivatedTFRectExpand.center.x - roomActivatedTransformRectExpandHalfSize.x - roomCetnerWithOffset.x;
                                            //! 좌측 이동은 음수 → 더 큰 값을 허용
                                            moveVelocity_byHorizontalNode.x += Mathf.Floor(Mathf.Max(moveLeft_Max, moveLeft_Clamped));
                                        }
                                    }

                                    //? 우측 + (하단 or 상단)
                                    else if (hasNode_Right)
                                    {
                                        //. "우측 합Rect"에 이동(밀착) 하기
                                        float moveRight_Max = combinedRect_ActivatedTFRectExpand_Right.xMin - roomActivatedTransformRectExpand.xMax;

                                        //? 우측에 이미 밀착된 상태가 아닐때만 연산 (0이라면 이미 밀착)
                                        if (moveRight_Max != 0f)
                                        {
                                            //. "하단 or 상단 의 합Rect"의 중심점보다 더 "우측"으로 가지 않도록 제약 (단, 오프셋을 포함해 테두리가 닿을때까진 OK)
                                            float moveRight_Clamped = currentCombinedRect_ActivatedTFRectExpand.center.x + roomActivatedTransformRectExpandHalfSize.x - roomCetnerWithOffset.x;
                                            //! 우측 이동은 양수 → 더 작은 값을 허용
                                            moveVelocity_byHorizontalNode.x += Mathf.Floor(Mathf.Min(moveRight_Max, moveRight_Clamped));
                                        }
                                    }
                                }
                                void moveAxisX()
                                {
                                    //. 좌측 or 우측 합Rect
                                    currentCombinedRect_ActivatedTFRectExpand = hasNode_Left ? combinedRect_ActivatedTFRectExpand_Left : combinedRect_ActivatedTFRectExpand_Right;

                                    //? 하단 + (좌측 or 우측)
                                    if (hasNode_Down)
                                    {
                                        //. "하단 합Rect"에 이동(밀착) 하기
                                        float moveDown_Max = combinedRect_ActivatedTFRectExpand_Down.yMax - roomActivatedTransformRectExpand.yMin;

                                        //? 하단에 이미 밀착된 상태가 아닐때만 연산 (0이라면 이미 밀착)
                                        if (moveDown_Max != 0f)
                                        {
                                            //. "좌측 or 우측 의 합Rect"의 중심점보다 더 "하단"으로 가지 않도록 제약 (단, 오프셋을 포함해 테두리가 닿을때까진 OK)
                                            float moveDown_Clamped = currentCombinedRect_ActivatedTFRectExpand.center.y - roomActivatedTransformRectExpandHalfSize.y - roomCetnerWithOffset.y;
                                            //! 하단 이동은 음수 → 더 큰 값을 허용
                                            moveVelocity_byVerticalNode.y += Mathf.Floor(Mathf.Max(moveDown_Max, moveDown_Clamped));
                                        }
                                    }

                                    //? 상단 + (좌측 or 우측)
                                    else if (hasNode_Up)
                                    {
                                        //. "상단 합Rect"에 이동(밀착) 하기
                                        float moveUp_Max = combinedRect_ActivatedTFRectExpand_Up.yMin - roomActivatedTransformRectExpand.yMax;

                                        //? 상단에 이미 밀착된 상태가 아닐때만 연산 (0이라면 이미 밀착)
                                        if (moveUp_Max != 0f)
                                        {
                                            //. "좌측 or 우측 의 합Rect"의 중심점보다 더 "상단"으로 가지 않도록 제약 (단, 오프셋을 포함해 테두리가 닿을때까진 OK)
                                            float moveUp_Clamped = currentCombinedRect_ActivatedTFRectExpand.center.y + roomActivatedTransformRectExpandHalfSize.y - roomCetnerWithOffset.y;
                                            //! 상단 이동은 양수 → 더 작은 값을 허용
                                            moveVelocity_byVerticalNode.y += Mathf.Floor(Mathf.Min(moveUp_Max, moveUp_Clamped));
                                        }
                                    }
                                }
                            }
                        }



                        //? #3 "양방향" 노드의 인접을 시작한다
                        //!     기준 방은 고정하고, 해당 축의 반대편 ‘덩어리’를 제약 범위 내에서 기준 쪽으로 일괄 끌어온다
                        moveTwoWayNode();
                        void moveTwoWayNode()
                        {
                            //? 양방향 - 하단 or 상단
                            if (isTwoWayNode_Vertical)
                            {
                                //? [수직·하단] 기준 하단선과( center.yMin ) 하단 덩어리의 yMax 사이에 ‘양수 갭’ 존재
                                //!  → 하단 서브그래프가 기준 방과 떨어져 있으므로 위(+Y)로 당겨 붙인다
                                //.  (제약: 스테이지 경계/이웃충돌/반대축 평균선, 스냅 후 ΔY 누적 적용)
                                if (hasNode_Down && roomActivatedTransformRectExpand.yMin - combinedRect_ActivatedTFRectExpand_Down.yMax > 0f)
                                {
                                    pullComponentTowardCenter_WithConstraints(main, spaceList, space, roomActivatedTransformRectExpand, stageTransformRect, EDirection4.Down);
                                }

                                //? [수직·상단] 상단 덩어리의 yMin 과 기준 상단선( center.yMax ) 사이에 ‘양수 갭’ 존재
                                //!  → 상단 서브그래프가 기준 방과 떨어져 있으므로 아래(−Y)로 당겨 붙인다
                                //.  (제약 동일, 스냅 후 ΔY 누적 적용)
                                if (hasNode_Up && combinedRect_ActivatedTFRectExpand_Up.yMin - roomActivatedTransformRectExpand.yMax > 0f)
                                {
                                    pullComponentTowardCenter_WithConstraints(main, spaceList, space, roomActivatedTransformRectExpand, stageTransformRect, EDirection4.Up);
                                }
                            }

                            //? 양방향 - 좌측 or 우측
                            if (isTwoWayNode_Horizontal)
                            {
                                //? [수평·좌측] 기준 좌측선( center.xMin )과 좌측 덩어리의 xMax 사이에 ‘양수 갭’ 존재
                                //!  → 좌측 서브그래프가 기준 방과 떨어져 있으므로 오른쪽(+X)으로 당겨 붙인다
                                //.  (제약 동일, 스냅 후 ΔX 누적 적용)
                                if (hasNode_Left && roomActivatedTransformRectExpand.xMin - combinedRect_ActivatedTFRectExpand_Left.xMax > 0f)
                                {
                                    pullComponentTowardCenter_WithConstraints(main, spaceList, space, roomActivatedTransformRectExpand, stageTransformRect, EDirection4.Left);
                                }

                                //? [수평·우측] 우측 덩어리의 xMin 과 기준 우측선( center.xMax ) 사이에 ‘양수 갭’ 존재
                                //!  → 우측 서브그래프가 기준 방과 떨어져 있으므로 왼쪽(−X)으로 당겨 붙인다
                                //.  (제약 동일, 스냅 후 ΔX 누적 적용)
                                if (hasNode_Right && combinedRect_ActivatedTFRectExpand_Right.xMin - roomActivatedTransformRectExpand.xMax > 0f)
                                {
                                    pullComponentTowardCenter_WithConstraints(main, spaceList, space, roomActivatedTransformRectExpand, stageTransformRect, EDirection4.Right);
                                }
                            }
                        }


                        #region 내부 메서드 

                        //. 받아온 공간 내에 존재하는 모든 방들의 ActivatedTransformRectExpand를 모두 하나로 합쳐, Rect로 반환
                        Rect getConnectingRooms_CombinedRect_ActivatedTransformRectExpand(IReadOnlyList<Space> connectingSpaces)
                        {
                            float xMin = float.MaxValue;
                            float xMax = float.MinValue;
                            float yMin = float.MaxValue;
                            float yMax = float.MinValue;

                            for (int i = 0; i < connectingSpaces.Count; i++)
                            {
                                Space connectSpace = connectingSpaces[i];

                                //! 방이 있는것들만 해당됨
                                if (connectSpace.PlacedRoom == null) { continue; }

                                var rect = main.PlaceM.PlacedRoomTFInfoCacheM.GetRoomActivatedRectExpand_byCache(connectSpace.PlacedRoom);

                                if (xMin > rect.xMin) { xMin = rect.xMin; }
                                if (xMax < rect.xMax) { xMax = rect.xMax; }
                                if (yMin > rect.yMin) { yMin = rect.yMin; }
                                if (yMax < rect.yMax) { yMax = rect.yMax; }
                            }

                            return SU_TF_Rect.RectFromCorners(new Vector2(xMin, yMin), new Vector2(xMax, yMax));
                        }


                        #region 양방향 통합 이전 코드
                        ////. ─────────────────────────────────────────────────────────────────────────────
                        ////. [NEW] 기준 방을 중심으로, 양방향(상·하 / 좌·우) 이웃 방들을
                        ////.       "제약된 범위" 안에서 기준 방 쪽으로 끌어오는 보조 메서드들
                        ////.       (기준 방은 고정, 이웃 방들을 이동)
                        ////.       - ActivatedTransformRectExpand 기준
                        ////.       - 반대축 연결 상태 고려하여 Clamp
                        ////.       - GridUnit, StageRect, Swizzle 모두 반영
                        ////. ─────────────────────────────────────────────────────────────────────────────

                        //void PullVerticalComponentTowardCenter_WithConstraints(
                        //    StageGenerator _main,
                        //    Space centerSpace,
                        //    in CustomRect2DCentered centerRectTF,
                        //    in Rect stageRect,
                        //    EDirection4 direction,      //. Down → 위로 끌기, Up → 아래로 끌기
                        //    int maxDepth = 256
                        //)
                        //{
                        //    if (direction != EDirection4.Down && direction != EDirection4.Up) return;

                        //    var snap = _main.Setting.SnapSetting;
                        //    var swz = snap.Swizzle;

                        //    float center_yMin = centerRectTF.yMin;
                        //    float center_yMax = centerRectTF.yMax;

                        //    //. ─────────────────────────────────────
                        //    //. 1) ‘한쪽 반공간’ 서브그래프 수집 (BFS)
                        //    //.    Down: 기준 하단선 이하(yMax <= center_yMin)만 포함
                        //    //.    Up  : 기준 상단선 이상(yMin >= center_yMax)만 포함
                        //    //. ─────────────────────────────────────
                        //    bool IsOnSide(Space s)
                        //    {
                        //        if (s?.PlacedRoom == null) return false;
                        //        //var r = s.PlacedRoom.GetInstanceRoomActivatedTransformRectExpand(_main.Setting);
                        //        var r = main.PlaceM.GetRoomActivatedRectExpand_byCache(s.PlacedRoom);
                        //        return (direction == EDirection4.Down) ? (r.yMax <= center_yMin) : (r.yMin >= center_yMax);
                        //    }

                        //    var comp = new HashSet<Space>();
                        //    var q = new Queue<(Space s, int d)>();

                        //    IReadOnlyList<Space> firsts = (direction == EDirection4.Down) ? centerSpace.ConnectingSpaces_Down : centerSpace.ConnectingSpaces_Up;
                        //    if (firsts == null || firsts.Count == 0) return;

                        //    for (int i = 0; i < firsts.Count; i++)
                        //    {
                        //        var nx = firsts[i];
                        //        if (IsOnSide(nx) && comp.Add(nx)) q.Enqueue((nx, 0));
                        //    }

                        //    while (q.Count > 0)
                        //    {
                        //        var (cur, depth) = q.Dequeue();
                        //        if (depth >= maxDepth) continue;

                        //        void Enq(IReadOnlyList<Space> lst)
                        //        {
                        //            if (lst == null) return;
                        //            for (int k = 0; k < lst.Count; k++)
                        //            {
                        //                var nb = lst[k];
                        //                if (!IsOnSide(nb)) continue;
                        //                if (comp.Add(nb)) q.Enqueue((nb, depth + 1));
                        //            }
                        //        }

                        //        Enq(cur.ConnectingSpaces_Left);
                        //        Enq(cur.ConnectingSpaces_Right);
                        //        Enq(cur.ConnectingSpaces_Down);
                        //        Enq(cur.ConnectingSpaces_Up);
                        //    }

                        //    if (comp.Count == 0) return;

                        //    //. ─────────────────────────────────────
                        //    //. 2) 합Rect & 이상적 델타(부호 주의!)
                        //    //. ─────────────────────────────────────
                        //    Rect CompUnion()
                        //    {
                        //        float xMin = float.MaxValue, xMax = float.MinValue, yMin = float.MaxValue, yMax = float.MinValue;
                        //        foreach (var sp in comp)
                        //        {
                        //            //var r = sp.PlacedRoom.GetInstanceRoomActivatedTransformRectExpand(_main.Setting);
                        //            var r = main.PlaceM.GetRoomActivatedRectExpand_byCache(sp.PlacedRoom);
                        //            if (xMin > r.xMin) xMin = r.xMin;
                        //            if (xMax < r.xMax) xMax = r.xMax;
                        //            if (yMin > r.yMin) yMin = r.yMin;
                        //            if (yMax < r.yMax) yMax = r.yMax;
                        //        }
                        //        return SU_TF_Rect.RectFromCorners(new Vector2(xMin, yMin), new Vector2(xMax, yMax));
                        //    }

                        //    var compUnion = CompUnion();

                        //    //. 방향별 올바른 부호
                        //    float idealDelta =
                        //        (direction == EDirection4.Down)
                        //        ? (center_yMin - compUnion.yMax)      //. 아래 덩어리를 ‘위로’ (+) 끌기
                        //        : (center_yMax - compUnion.yMin);     //. 위   덩어리를 ‘아래로’ (−) 끌기

                        //    if (Mathf.Approximately(idealDelta, 0f)) return;

                        //    //. ─────────────────────────────────────
                        //    //. 3) 외부 이웃 기준 제약 → 덩어리 공통 Δ는 최소 허용치
                        //    //.    내부(comp) 이웃은 제외 (같이 움직이므로 제약 X)
                        //    //. ─────────────────────────────────────
                        //    float globalDelta = idealDelta;

                        //    foreach (var sp in comp)
                        //    {
                        //        //var r = sp.PlacedRoom.GetInstanceRoomActivatedTransformRectExpand(_main.Setting);
                        //        var r = main.PlaceM.GetRoomActivatedRectExpand_byCache(sp.PlacedRoom);

                        //        //. 스테이지 경계
                        //        float minY = stageRect.yMin - r.yMin;   //. 아래(-)
                        //        float maxY = stageRect.yMax - r.yMax;   //. 위(+)

                        //        float allow = Mathf.Clamp(idealDelta, minY, maxY);

                        //        //. 좌/우 ‘외부’ 평균 선 클램프
                        //        float ClampByExternalLR(float want)
                        //        {
                        //            int cnt = 0; Vector2 sum = Vector2.zero;

                        //            void Acc(IReadOnlyList<Space> lst)
                        //            {
                        //                if (lst == null) return;
                        //                for (int i = 0; i < lst.Count; i++)
                        //                {
                        //                    var nb = lst[i];
                        //                    if (nb?.PlacedRoom == null) continue;
                        //                    if (comp.Contains(nb)) continue; //. 내부 제외
                        //                    cnt++;
                        //                    sum += getRoomCenter_bySpace(main, nb, 2);
                        //                }
                        //            }
                        //            Acc(sp.ConnectingSpaces_Left);
                        //            Acc(sp.ConnectingSpaces_Right);

                        //            if (cnt == 0) return want;

                        //            float avgY = (sum / cnt).y;
                        //            float halfH = r.Size.y * 0.5f;

                        //            if (want > 0f) // 위로
                        //            {
                        //                float yMinAfter = r.yMin + want;
                        //                float yMinMax = avgY - halfH;
                        //                if (yMinAfter > yMinMax) want = Mathf.Max(0f, yMinMax - r.yMin);
                        //            }
                        //            else if (want < 0f) // 아래로
                        //            {
                        //                float yMaxAfter = r.yMax + want;
                        //                float yMaxMin = avgY + halfH;
                        //                if (yMaxAfter < yMaxMin) want = Mathf.Min(0f, yMaxMin - r.yMax);
                        //            }
                        //            return want;
                        //        }

                        //        //. 상/하 ‘외부’ 이웃 클램프
                        //        float ClampByExternalUpDown(float want)
                        //        {
                        //            if (want > 0f) // 위로
                        //            {
                        //                float limit = float.PositiveInfinity;
                        //                var ups = sp.ConnectingSpaces_Up;
                        //                if (ups != null)
                        //                {
                        //                    for (int i = 0; i < ups.Count; i++)
                        //                    {
                        //                        var nb = ups[i];
                        //                        if (nb?.PlacedRoom == null) continue;
                        //                        if (comp.Contains(nb)) continue;
                        //                        //var nr = nb.PlacedRoom.GetInstanceRoomActivatedTransformRectExpand(_main.Setting);
                        //                        var nr = main.PlaceM.GetRoomActivatedRectExpand_byCache(nb.PlacedRoom);
                        //                        limit = Mathf.Min(limit, nr.yMin - r.yMax);
                        //                    }
                        //                }
                        //                if (!float.IsInfinity(limit)) want = Mathf.Min(want, limit);
                        //            }
                        //            else if (want < 0f) // 아래로
                        //            {
                        //                float limit = float.NegativeInfinity;
                        //                var dns = sp.ConnectingSpaces_Down;
                        //                if (dns != null)
                        //                {
                        //                    for (int i = 0; i < dns.Count; i++)
                        //                    {
                        //                        var nb = dns[i];
                        //                        if (nb?.PlacedRoom == null) continue;
                        //                        if (comp.Contains(nb)) continue;
                        //                        //var nr = nb.PlacedRoom.GetInstanceRoomActivatedTransformRectExpand(_main.Setting);
                        //                        var nr = main.PlaceM.GetRoomActivatedRectExpand_byCache(nb.PlacedRoom);
                        //                        limit = Mathf.Max(limit, nr.yMax - r.yMin); // 음수 한계
                        //                    }
                        //                }
                        //                if (!float.IsNegativeInfinity(limit)) want = Mathf.Max(want, limit);
                        //            }
                        //            return want;
                        //        }

                        //        allow = ClampByExternalLR(allow);
                        //        allow = ClampByExternalUpDown(allow);

                        //        //. 덩어리 공통 Δ 갱신
                        //        if (idealDelta > 0f) globalDelta = Mathf.Min(globalDelta, allow);
                        //        else globalDelta = Mathf.Max(globalDelta, allow);
                        //    }

                        //    //. ─────────────────────────────────────
                        //    //. 4) 스냅 & 일괄 적용
                        //    //. ─────────────────────────────────────
                        //    float deltaY = globalDelta;
                        //    if (deltaY > 0f) deltaY = Mathf.Ceil(deltaY) * snap.GridUnitY_Height / snap.GridUnitY_Height;
                        //    else deltaY = -Mathf.Ceil(-deltaY) * snap.GridUnitY_Height / snap.GridUnitY_Height;

                        //    if (!Mathf.Approximately(deltaY, 0f))
                        //    {
                        //        foreach (var sp in comp)
                        //        {
                        //            //! 250813 
                        //            main.placeM.AddVelocity_byPlacedRoomTransformInfoCache(sp.PlacedRoom, new Vector2(0f, deltaY));
                        //            //sp.PlacedRoom.transform.position += new Vector2(0f, deltaY).SwizzlesVector2To3(swz);
                        //        }

                        //        //! 반복 수렴 플래그
                        //        isMovedSomething = true; //.!
                        //    }
                        //}


                        //// ─────────────────────────────────────────────────────────────────────────────
                        //// [NEW] 가로 "서브그래프"를 기준 방 쪽으로 당기기 (기준 방 고정, 덩어리 전체 동시 이동)
                        ////  - 시작: 기준 방의 Left(또는 Right) 이웃들
                        ////  - BFS로 좌/우/상/하 전방향 탐색하되, 기준선의 '한쪽 반공간'만 포함
                        ////  - 이동량: (기준과의 gap)과 각 방의 "외부 이웃" 제약을 모두 만족하는 공통 ΔX
                        //// ─────────────────────────────────────────────────────────────────────────────
                        //void PullHorizontalComponentTowardCenter_WithConstraints(
                        //    StageGenerator _main,
                        //    Space centerSpace,
                        //    in CustomRect2DCentered centerRectTF,
                        //    in Rect stageRect,
                        //    EDirection4 direction,      //. Left → 오른쪽(+X)으로 끌기, Right → 왼쪽(−X)으로 끌기
                        //    int maxDepth = 256
                        //)
                        //{
                        //    if (direction != EDirection4.Left && direction != EDirection4.Right) return;

                        //    var snap = _main.Setting.SnapSetting;
                        //    var swz = snap.Swizzle;

                        //    float center_xMin = centerRectTF.xMin;
                        //    float center_xMax = centerRectTF.xMax;

                        //    // 1) '한쪽 반공간' 서브그래프 수집 (BFS)
                        //    //    Left : 기준 좌측선 이하(xMax <= center_xMin)만 포함
                        //    //    Right: 기준 우측선 이상(xMin >= center_xMax)만 포함
                        //    bool IsOnSide(Space s)
                        //    {
                        //        if (s?.PlacedRoom == null) return false;
                        //        //var r = s.PlacedRoom.GetInstanceRoomActivatedTransformRectExpand(_main.Setting);
                        //        var r = main.PlaceM.GetRoomActivatedRectExpand_byCache(s.PlacedRoom);
                        //        return (direction == EDirection4.Left) ? (r.xMax <= center_xMin) : (r.xMin >= center_xMax);
                        //    }

                        //    var comp = new HashSet<Space>();
                        //    var q = new Queue<(Space s, int d)>();

                        //    IReadOnlyList<Space> firsts = (direction == EDirection4.Left) ? centerSpace.ConnectingSpaces_Left : centerSpace.ConnectingSpaces_Right;
                        //    if (firsts == null || firsts.Count == 0) return;

                        //    for (int i = 0; i < firsts.Count; i++)
                        //    {
                        //        var nx = firsts[i];
                        //        if (IsOnSide(nx) && comp.Add(nx)) q.Enqueue((nx, 0));
                        //    }

                        //    while (q.Count > 0)
                        //    {
                        //        var (cur, depth) = q.Dequeue();
                        //        if (depth >= maxDepth) continue;

                        //        void Enq(IReadOnlyList<Space> lst)
                        //        {
                        //            if (lst == null) return;
                        //            for (int k = 0; k < lst.Count; k++)
                        //            {
                        //                var nb = lst[k];
                        //                if (!IsOnSide(nb)) continue;
                        //                if (comp.Add(nb)) q.Enqueue((nb, depth + 1));
                        //            }
                        //        }

                        //        Enq(cur.ConnectingSpaces_Left);
                        //        Enq(cur.ConnectingSpaces_Right);
                        //        Enq(cur.ConnectingSpaces_Down);
                        //        Enq(cur.ConnectingSpaces_Up);
                        //    }

                        //    if (comp.Count == 0) return;

                        //    // 2) 합Rect & 이상적 ΔX(부호 주의)
                        //    Rect CompUnion()
                        //    {
                        //        float xMin = float.MaxValue, xMax = float.MinValue, yMin = float.MaxValue, yMax = float.MinValue;
                        //        foreach (var sp in comp)
                        //        {
                        //            //var r = sp.PlacedRoom.GetInstanceRoomActivatedTransformRectExpand(_main.Setting);
                        //            var r = main.PlaceM.GetRoomActivatedRectExpand_byCache(sp.PlacedRoom);
                        //            if (xMin > r.xMin) xMin = r.xMin;
                        //            if (xMax < r.xMax) xMax = r.xMax;
                        //            if (yMin > r.yMin) yMin = r.yMin;
                        //            if (yMax < r.yMax) yMax = r.yMax;
                        //        }
                        //        return SU_TF_Rect.RectFromCorners(new Vector2(xMin, yMin), new Vector2(xMax, yMax));
                        //    }

                        //    var compUnion = CompUnion();

                        //    // Left  덩어리 → 오른쪽(+X)으로: center_xMin - compUnion.xMax
                        //    // Right 덩어리 → 왼쪽(−X)으로 : center_xMax - compUnion.xMin (보통 음수)
                        //    float idealDelta =
                        //        (direction == EDirection4.Left)
                        //        ? (center_xMin - compUnion.xMax)
                        //        : (center_xMax - compUnion.xMin);

                        //    if (Mathf.Approximately(idealDelta, 0f)) return;

                        //    // 3) 외부 이웃 기준 제약 → 덩어리 공통 ΔX
                        //    float globalDelta = idealDelta;

                        //    foreach (var sp in comp)
                        //    {
                        //        //var r = sp.PlacedRoom.GetInstanceRoomActivatedTransformRectExpand(_main.Setting);
                        //        var r = main.PlaceM.GetRoomActivatedRectExpand_byCache(sp.PlacedRoom);

                        //        // 스테이지 경계
                        //        float minX = stageRect.xMin - r.xMin;   // 왼쪽(−)
                        //        float maxX = stageRect.xMax - r.xMax;   // 오른쪽(+)

                        //        float allow = Mathf.Clamp(idealDelta, minX, maxX);

                        //        // (A) 상/하 '외부' 평균선 클램프 (내부는 제외) → 너무 X로 치우치지 않도록
                        //        float ClampByExternalUD(float want)
                        //        {
                        //            int cnt = 0; Vector2 sum = Vector2.zero;

                        //            void Acc(IReadOnlyList<Space> lst)
                        //            {
                        //                if (lst == null) return;
                        //                for (int i = 0; i < lst.Count; i++)
                        //                {
                        //                    var nb = lst[i];
                        //                    if (nb?.PlacedRoom == null) continue;
                        //                    if (comp.Contains(nb)) continue; // 내부 제외
                        //                    cnt++;
                        //                    sum += getRoomCenter_bySpace(main, nb, 2);
                        //                }
                        //            }
                        //            Acc(sp.ConnectingSpaces_Up);
                        //            Acc(sp.ConnectingSpaces_Down);

                        //            if (cnt == 0) return want;

                        //            float avgX = (sum / cnt).x;
                        //            float halfW = r.Size.x * 0.5f;

                        //            if (want > 0f) // 오른쪽으로
                        //            {
                        //                float xMinAfter = r.xMin + want;
                        //                float xMinMax = avgX - halfW;
                        //                if (xMinAfter > xMinMax) want = Mathf.Max(0f, xMinMax - r.xMin);
                        //            }
                        //            else if (want < 0f) // 왼쪽으로
                        //            {
                        //                float xMaxAfter = r.xMax + want; // want<0
                        //                float xMaxMin = avgX + halfW;
                        //                if (xMaxAfter < xMaxMin) want = Mathf.Min(0f, xMaxMin - r.xMax);
                        //            }
                        //            return want;
                        //        }

                        //        // (B) 좌/우 '외부' 이웃과의 겹침 방지 (내부는 제외)
                        //        float ClampByExternalLR(float want)
                        //        {
                        //            if (want > 0f) // 오른쪽으로 당길 때: 오른쪽 이웃들의 xMin을 넘지 않게
                        //            {
                        //                float limit = float.PositiveInfinity;
                        //                var rights = sp.ConnectingSpaces_Right;
                        //                if (rights != null)
                        //                {
                        //                    for (int i = 0; i < rights.Count; i++)
                        //                    {
                        //                        var nb = rights[i];
                        //                        if (nb?.PlacedRoom == null) continue;
                        //                        if (comp.Contains(nb)) continue;
                        //                        //var nr = nb.PlacedRoom.GetInstanceRoomActivatedTransformRectExpand(_main.Setting);
                        //                        var nr = main.PlaceM.GetRoomActivatedRectExpand_byCache(nb.PlacedRoom);
                        //                        limit = Mathf.Min(limit, nr.xMin - r.xMax);
                        //                    }
                        //                }
                        //                if (!float.IsInfinity(limit)) want = Mathf.Min(want, limit);
                        //            }
                        //            else if (want < 0f) // 왼쪽으로 당길 때: 왼쪽 이웃들의 xMax를 넘지 않게
                        //            {
                        //                float limit = float.NegativeInfinity;
                        //                var lefts = sp.ConnectingSpaces_Left;
                        //                if (lefts != null)
                        //                {
                        //                    for (int i = 0; i < lefts.Count; i++)
                        //                    {
                        //                        var nb = lefts[i];
                        //                        if (nb?.PlacedRoom == null) continue;
                        //                        if (comp.Contains(nb)) continue;
                        //                        //var nr = nb.PlacedRoom.GetInstanceRoomActivatedTransformRectExpand(_main.Setting);
                        //                        var nr = main.PlaceM.GetRoomActivatedRectExpand_byCache(nb.PlacedRoom);
                        //                        limit = Mathf.Max(limit, nr.xMax - r.xMin); // 음수 한계
                        //                    }
                        //                }
                        //                if (!float.IsNegativeInfinity(limit)) want = Mathf.Max(want, limit);
                        //            }
                        //            return want;
                        //        }

                        //        allow = ClampByExternalUD(allow);
                        //        allow = ClampByExternalLR(allow);

                        //        // 덩어리 공통 ΔX 갱신 (부호에 따라 Min/Max)
                        //        if (idealDelta > 0f) globalDelta = Mathf.Min(globalDelta, allow);
                        //        else globalDelta = Mathf.Max(globalDelta, allow);
                        //    }

                        //    // 4) 스냅 & 일괄 적용
                        //    float deltaX = (globalDelta > 0f)
                        //        ? Mathf.Ceil(globalDelta) * snap.GridUnitX_Width / snap.GridUnitX_Width
                        //        : -Mathf.Ceil(-globalDelta) * snap.GridUnitX_Width / snap.GridUnitX_Width;

                        //    if (!Mathf.Approximately(deltaX, 0f))
                        //    {
                        //        foreach (var sp in comp)
                        //        {
                        //            //! 250813 
                        //            main.placeM.AddVelocity_byPlacedRoomTransformInfoCache(sp.PlacedRoom, new Vector2(deltaX, 0f));
                        //            //sp.PlacedRoom.transform.position += new Vector2(deltaX, 0f).SwizzlesVector2To3(swz);
                        //        }

                        //        isMovedSomething = true; // 반복 수렴 유도
                        //    }
                        //} 
                        #endregion

                        #endregion


                        //. 총 Velocity를 합산하여, GridUnit 연산을 하고, 공간을 벗어나지않게 제약한뒤, 적용한다
                        var combineVelocity = moveVelocity_byVerticalNode + moveVelocity_byHorizontalNode;
                        bool currentMovedSomething = ApplyVelocity_RoomNearestRoom(main, spaceList, space.PlacedRoom, combineVelocity, stageTransformRect);

                        //? 연산된 Velocity가 이동값이 (0,0)이 아니라면, 이동 여부 플래그를 활성화시킨다
                        if (!isMovedSomething && currentMovedSomething) { isMovedSomething = true; }

                        //! 비동기 프레임 양보
                        if (yieldCount % main.CalculateSetting.RoomNearestYieldUnitAsync == 0) { await UniTask.Yield(); }
                    }


                    return isMovedSomething;
                }



                #region 인접 관련 메서드



                //. 공간 내 "공간 중심점" 연산
                private Vector2 getSpaceCenter_bySpace(StageGenerator main, Space space, int transformOffsetMultiply)
                {
                    Vector2 center = space.SpaceTransformRect.center;

                    //. 공간 중앙 좌표 연산에 방Activated Rect 크기의 홀짝에따라  보정
                    if (!((int)(space.PlacedRoom.InstanceRoomActivatedRectExpand.Size.x)).IsEven())
                    {
                        center.x += main.Setting.SnapSetting.GridUnitX_Width * 0.5f;
                    }
                    if (!((int)(space.PlacedRoom.InstanceRoomActivatedRectExpand.Size.y)).IsEven())
                    {
                        center.y += main.Setting.SnapSetting.GridUnitY_Height * 0.5f;
                    }

                    //. 공간 중심점에, 오프셋까지 연산한다
                    if (transformOffsetMultiply > 0) { center += main.PlaceM.PlacedRoomTFInfoCacheM.GetRoomActivatedRectExpand_byCache(space.PlacedRoom).Offset * transformOffsetMultiply; }

                    return center;
                }



                //. 공간 내 "방 중심점" 연산
                private Vector2 getRoomCenter_bySpace(StageGenerator main, Space space, int transformOffsetMultiply)
                {
                    if (space.PlacedRoom == null) { return Vector2.zero; }

                    var rect = main.PlaceM.PlacedRoomTFInfoCacheM.GetRoomActivatedRectExpand_byCache(space.PlacedRoom);

                    //? 보정연산 (스테이지 부모 좌표에 따른 보정)
                    rect.Center -= (Vector2Int)main.TransformM.StageParentSnappedPositionCurrentCache;

                    Vector2 center = rect.Center;

                    if (transformOffsetMultiply > 0) { center += rect.Offset * transformOffsetMultiply; }
                    //. 오프셋에 2x 곱하는게 무슨의미인지 사실 모르겠어 근데 해야돼     

                    return center;
                }



                //. 공간 내 "공간 중심점" 또는 "방 중심점" 연산
                private Vector2 getCenter_bySpace(StageGenerator main, Space space, bool useRoomCenter, int transformOffsetMultiply)
                    => useRoomCenter ? getRoomCenter_bySpace(main, space, transformOffsetMultiply) : getSpaceCenter_bySpace(main, space, transformOffsetMultiply);



                /// <summary>
                /// 기준 방(<paramref name="centerSpace"/>)은 고정하고,
                /// 지정된 방향(<paramref name="direction"/>)의 한쪽 반공간에 속한 서브그래프(덩어리)를
                /// 기준 방 쪽으로 한 번에 끌어옵니다.
                /// </summary>
                /// <remarks>
                /// <list type="bullet">
                ///   <item>
                ///     <description>
                ///     동일 덩어리(서브그래프)는 동일한 Δ만큼 동시 이동하여 내부 상대 배치를 보존합니다.
                ///     </description>
                ///   </item>
                ///   <item>
                ///     <description>
                ///     이동량 Δ는 다음 제약을 모두 만족하는 최대(또는 최소) 허용치로 산정됩니다:
                ///     스테이지 경계 클램프, 같은 축 이웃과의 충돌 방지(붙을 때까지만 허용),
                ///     반대축 이웃의 평균 중심선 기준 클램프(과도한 치우침 방지).
                ///     </description>
                ///   </item>
                ///   <item>
                ///     <description>
                ///     산정된 Δ는 그리드 단위로 스냅됩니다(축별 GridUnit 적용).
                ///     실제 트랜스폼을 즉시 이동하지 않고, <c>placeM</c>의 누적 Velocity 캐시에 기록합니다.
                ///     </description>
                ///   </item>
                ///   <item>
                ///     <description>
                ///     방향 의미: <see cref="EDirection4.Down"/>/<see cref="EDirection4.Left"/>는
                ///     반대편에서 기준 방 쪽(+Y/+X)으로 끌어오며,
                ///     <see cref="EDirection4.Up"/>/<see cref="EDirection4.Right"/>는
                ///     기준 방에서 해당 쪽(−Y/−X)으로 붙입니다.
                ///     (내부적으로는 comp의 Max/Min과 기준 Min/Max의 차이로 이상적 Δ를 계산)
                ///     </description>
                ///   </item>
                /// </list>
                /// 알고리즘 개요:
                /// <list type="number">
                ///   <item><description>지정 방향의 반공간에 실제로 위치한 노드만 BFS로 수집하여 서브그래프를 구성합니다.</description></item>
                ///   <item><description>서브그래프의 합Rect와 기준 방의 경계로부터 이상적 Δ를 계산합니다.</description></item>
                ///   <item><description>각 노드별 외부 제약(경계/이웃)으로 허용 Δ를 계산하고, 부호를 보존한 공통 Δ로 집약합니다.</description></item>
                ///   <item><description>그리드 스냅 후, 서브그래프 전체에 동일 Δ를 누적 적용합니다.</description></item>
                /// </list>
                /// 평균 시간복잡도는 BFS(O(V+E))) + 이웃 검사(국소적)로 구성됩니다.
                /// </remarks>
                /// <param name="_main">생성기 및 스냅/스테이지 설정에 접근하기 위한 컨텍스트.</param>
                /// <param name="centerSpace">기준 방이 포함된 공간.</param>
                /// <param name="centerRectTF">기준 방의 ActivatedTransformRectExpand.</param>
                /// <param name="stageRect">스테이지 전체 경계 사각형.</param>
                /// <param name="direction">끌어올 방향(Down/Up/Left/Right).</param>
                /// <param name="maxDepth">BFS 탐색 최대 깊이(안전장치).</param>
                private void pullComponentTowardCenter_WithConstraints(
                        StageGenerator _main,
                        IReadOnlyList<Space> spaceList,
                        Space centerSpace,
                        in CustomRect2DCentered centerRectTF,
                        in Rect stageRect,
                        EDirection4 direction,
                        int maxDepth = 256)
                {
                    //? 수직 처리인지/수평 처리인지 구분하고(+방향/−방향 의미를 정의)
                    bool vertical = (direction == EDirection4.Down || direction == EDirection4.Up);
                    bool pullNeg = (direction == EDirection4.Down || direction == EDirection4.Left); // Down/Left는 ‘반대쪽에서 당김’
                    var snap = _main.Setting.SnapSetting;

                    //. 기준 방의 축별 경계값
                    float cMin = vertical ? centerRectTF.yMin : centerRectTF.xMin;
                    float cMax = vertical ? centerRectTF.yMax : centerRectTF.xMax;

                    //? [1] 반공간 서브그래프 수집(BFS)
                    //?  - 기준선(cMin/cMax)을 경계로, 지정된 쪽(Down/Up/Left/Right)에 실제로 위치한 방들만 comp에 포함
                    bool IsOnSide(Space s)
                    {
                        if (s?.PlacedRoom == null) return false;
                        var r = _main.placeM.PlacedRoomTFInfoCacheM.GetRoomActivatedRectExpand_byCache(s.PlacedRoom);

                        if (vertical)
                            return pullNeg ? (r.yMax <= cMin) : (r.yMin >= cMax);  //. Down: 기준 하단선 이하 / Up: 기준 상단선 이상
                        else
                            return pullNeg ? (r.xMax <= cMin) : (r.xMin >= cMax);  //. Left: 기준 좌측선 이하 / Right: 기준 우측선 이상
                    }

                    //. 시작 후보(기준 방의 지정 방향 이웃들)
                    IReadOnlyList<Space> Firsts()
                    {
                        return direction switch
                        {
                            EDirection4.Down => centerSpace.ConnectingSpaces_Down,
                            EDirection4.Up => centerSpace.ConnectingSpaces_Up,
                            EDirection4.Left => centerSpace.ConnectingSpaces_Left,
                            EDirection4.Right => centerSpace.ConnectingSpaces_Right,
                            _ => null
                        };
                    }

                    var comp = new HashSet<Space>();               //. 덩어리 구성원
                    var q = new Queue<(Space s, int d)>();      //. BFS 큐(깊이 포함)

                    var firsts = Firsts();
                    if (firsts == null || firsts.Count == 0) return;

                    //. 시작점 등록(반공간 조건 충족만)
                    for (int i = 0; i < firsts.Count; i++)
                        if (IsOnSide(firsts[i]) && comp.Add(firsts[i]))
                            q.Enqueue((firsts[i], 0));

                    //. 전방향으로 확장(BFS)하되, 반공간 조건 불일치/깊이 초과는 건너뜀
                    void Enq(IReadOnlyList<Space> lst, int depth)
                    {
                        if (lst == null) return;
                        for (int i = 0; i < lst.Count; i++)
                        {
                            var nb = lst[i];
                            if (!IsOnSide(nb)) continue;
                            if (comp.Add(nb)) q.Enqueue((nb, depth + 1));
                        }
                    }

                    while (q.Count > 0)
                    {
                        var (cur, depth) = q.Dequeue();
                        if (depth >= maxDepth) continue;

                        Enq(cur.ConnectingSpaces_Left, depth);
                        Enq(cur.ConnectingSpaces_Right, depth);
                        Enq(cur.ConnectingSpaces_Down, depth);
                        Enq(cur.ConnectingSpaces_Up, depth);
                    }
                    if (comp.Count == 0) return;

                    //? [2] 덩어리 합Rect 계산 + ‘이상적’ 이동량 산출
                    //?  - pullNeg(Down/Left): comp의 Max를 center Min에 붙이려 함 → ideal = cMin - uMax  (양수면 위/오른쪽으로)
                    //?  - !pullNeg(Up/Right): comp의 Min을 center Max에 붙이려 함 → ideal = cMax - uMin  (음수면 아래/왼쪽으로)
                    float uMin = float.MaxValue, uMax = float.MinValue;
                    foreach (var sp in comp)
                    {
                        var r = _main.placeM.PlacedRoomTFInfoCacheM.GetRoomActivatedRectExpand_byCache(sp.PlacedRoom);
                        float a = vertical ? r.yMin : r.xMin;
                        float b = vertical ? r.yMax : r.xMax;

                        if (a < uMin) uMin = a;
                        if (b > uMax) uMax = b;
                    }

                    float ideal = pullNeg ? (cMin - uMax) : (cMax - uMin);
                    if (Mathf.Approximately(ideal, 0f)) return;    //! 이미 붙어있음: 무이동

                    //? [3] 방별 제약을 모두 반영해 ‘공용 Δ’를 결정
                    //?  - 스테이지 경계
                    //?  - 같은 축 이웃과의 충돌(붙을 때까지만 허용)
                    //?  - 반대축 이웃 평균선 기준 클램프(너무 치우치지 않도록)
                    //?  => ideal의 부호를 유지한 채, 허용치의 최소(또는 최대)를 취함
                    float global = ideal;

                    foreach (var sp in comp)
                    {
                        var r = _main.placeM.PlacedRoomTFInfoCacheM.GetRoomActivatedRectExpand_byCache(sp.PlacedRoom);

                        //. (A) 스테이지 경계 한계
                        float min = vertical ? (stageRect.yMin - r.yMin) : (stageRect.xMin - r.xMin);  //. 음수 방향
                        float max = vertical ? (stageRect.yMax - r.yMax) : (stageRect.xMax - r.xMax);  //. 양수 방향
                        float allow = Mathf.Clamp(ideal, min, max);

                        //? (B) 반대축 ‘외부’ 이웃 평균선 기준 클램프
                        //?    - 내부(comp)에 속한 이웃은 동일 Δ로 같이 움직이므로 제약에서 제외
                        float ClampByExternalOrth(float want)
                        {
                            int cnt = 0; Vector2 sum = Vector2.zero;

                            void Acc(IReadOnlyList<Space> lst)
                            {
                                if (lst == null) return;
                                for (int i = 0; i < lst.Count; i++)
                                {
                                    var nb = lst[i];
                                    if (nb?.PlacedRoom == null) continue;
                                    if (comp.Contains(nb)) continue;          //. 내부 제외
                                    sum += getRoomCenter_bySpace(_main, nb, 2); // 기존 보정 방식 유지(오프셋*2)
                                    cnt++;
                                }
                            }

                            if (vertical) { Acc(sp.ConnectingSpaces_Left); Acc(sp.ConnectingSpaces_Right); }
                            else { Acc(sp.ConnectingSpaces_Up); Acc(sp.ConnectingSpaces_Down); }

                            if (cnt == 0) return want;

                            float avg = (sum / cnt)[vertical ? 1 : 0];     //. 평균선(Y 또는 X)
                            float half = (vertical ? r.Size.y : r.Size.x) * 0.5f;

                            if (want > 0f) // +방향(위/오른쪽)
                            {
                                float edgeAfter = (vertical ? r.yMin : r.xMin) + want; // 앞쪽 에지
                                float edgeMax = avg - half;
                                if (edgeAfter > edgeMax) want = Mathf.Max(0f, edgeMax - (vertical ? r.yMin : r.xMin));
                            }
                            else if (want < 0f) // −방향(아래/왼쪽)
                            {
                                float edgeAfter = (vertical ? r.yMax : r.xMax) + want; // 뒤쪽 에지
                                float edgeMin = avg + half;
                                if (edgeAfter < edgeMin) want = Mathf.Min(0f, edgeMin - (vertical ? r.yMax : r.xMax));
                            }
                            return want;
                        }

                        //. (C) 같은 축의 ‘외부’ 이웃과 충돌 방지(겹치기 직전까지만)
                        float ClampByExternalSameAxis(float want)
                        {
                            if (want > 0f) // +방향(위/오른쪽)
                            {
                                float lim = float.PositiveInfinity;
                                var posN = vertical ? sp.ConnectingSpaces_Up : sp.ConnectingSpaces_Right;

                                if (posN != null)
                                {
                                    for (int i = 0; i < posN.Count; i++)
                                    {
                                        var nb = posN[i];
                                        if (nb?.PlacedRoom == null) continue;
                                        if (comp.Contains(nb)) continue; // 내부 제외
                                        var nr = _main.placeM.PlacedRoomTFInfoCacheM.GetRoomActivatedRectExpand_byCache(nb.PlacedRoom);
                                        float g = vertical ? (nr.yMin - r.yMax) : (nr.xMin - r.xMax);
                                        if (g < lim) lim = g;
                                    }
                                }
                                if (!float.IsInfinity(lim)) want = Mathf.Min(want, lim);
                            }
                            else if (want < 0f) // −방향(아래/왼쪽)
                            {
                                float lim = float.NegativeInfinity;
                                var negN = vertical ? sp.ConnectingSpaces_Down : sp.ConnectingSpaces_Left;

                                if (negN != null)
                                {
                                    for (int i = 0; i < negN.Count; i++)
                                    {
                                        var nb = negN[i];
                                        if (nb?.PlacedRoom == null) continue;
                                        if (comp.Contains(nb)) continue; // 내부 제외
                                        var nr = _main.placeM.PlacedRoomTFInfoCacheM.GetRoomActivatedRectExpand_byCache(nb.PlacedRoom);
                                        float g = vertical ? (nr.yMax - r.yMin) : (nr.xMax - r.xMin); // 음수 한계
                                        if (g > lim) lim = g;
                                    }
                                }
                                if (!float.IsNegativeInfinity(lim)) want = Mathf.Max(want, lim);
                            }
                            return want;
                        }

                        allow = ClampByExternalOrth(allow);
                        allow = ClampByExternalSameAxis(allow);
                        global = (ideal > 0f) ? Mathf.Min(global, allow) : Mathf.Max(global, allow);  //. 이상적 부호 유지
                    }

                    //? [4] 그리드 스냅 후, comp 전체에 동일 Δ를 누적(캐시) 적용
                    static float SnapDelta(float v, float unit)
                        => (v >= 0f) ? Mathf.Ceil(v * unit) / unit
                                     : -Mathf.Ceil(-v * unit) / unit;

                    float delta = vertical
                        ? SnapDelta(global, snap.GridUnitY_Height)
                        : SnapDelta(global, snap.GridUnitX_Width);

                    if (Mathf.Approximately(delta, 0f)) return;    //! 스냅 결과 무이동이면 종료

                    //. 실제 트랜스폼을 즉시 이동하지 않고, placeM에 누적 Velocity로 등록
                    foreach (var sp in comp)
                        _main.placeM.PlacedRoomTFInfoCacheM.AddVelocity_byPlacedRoomTransformInfoCache(
                            spaceList,
                            sp.PlacedRoom,
                            vertical ? new Vector2(0f, delta) : new Vector2(delta, 0f),
                            stageRect
                        );
                }



                /// <summary>
                /// 주어진 기준(ECenterStandard)에 따라 입력된 Rect를 boundingRect 안에 배치하기 위해 필요한 이동 오프셋을 계산합니다.
                /// </summary>
                /// <param name="targetRect">배치할 대상 Rect입니다.</param>
                /// <param name="alignment">Rect를 배치할 기준을 결정하는 ECenterStandard 값입니다.</param>
                /// <param name="boundingRect">대상 Rect가 배치될 전체 영역(Rect)입니다.</param>
                /// <returns>대상 Rect를 주어진 기준에 맞게 배치하기 위해 필요한 이동량(Vector2)을 반환합니다.</returns>
                Vector2 getRectAlignmentOffset(Rect targetRect, ECenterStandard alignment, Rect boundingRect)
                {
                    //. 대상 Rect의 중심 좌표 계산
                    Vector2 targetCenter = targetRect.center;

                    //. Bounding Rect의 중심 좌표 계산
                    Vector2 boundingCenter = boundingRect.center;

                    //. 초기 오프셋 값 설정
                    Vector2 offset = Vector2.zero;

                    //. ECenterStandard에 따라 오프셋 계산
                    switch (alignment)
                    {
                        case ECenterStandard.MiddleCenter:
                        //. 중심을 기준으로 배치
                        offset = boundingCenter - targetCenter;
                        break;
                        case ECenterStandard.MiddleLeft:
                        //. 좌측 중앙을 기준으로 배치
                        offset = new Vector2(boundingRect.xMin - targetRect.xMin, boundingCenter.y - targetCenter.y);
                        break;
                        case ECenterStandard.MiddleRight:
                        //. 상단 중앙을 기준으로 배치
                        offset = new Vector2(boundingRect.xMax - targetRect.xMax, boundingCenter.y - targetCenter.y);
                        break;
                        case ECenterStandard.LowerCenter:
                        //. 좌측 중앙을 기준으로 배치
                        offset = new Vector2(boundingCenter.x - targetCenter.x, boundingRect.yMin - targetRect.yMin);
                        break;
                        case ECenterStandard.LowerLeft:
                        //. 좌측 좌측을 기준으로 배치
                        offset = new Vector2(boundingRect.xMin - targetRect.xMin, boundingRect.yMin - targetRect.yMin);
                        break;
                        case ECenterStandard.LowerRight:
                        //. 상단 좌측을 기준으로 배치
                        offset = new Vector2(boundingRect.xMax - targetRect.xMax, boundingRect.yMin - targetRect.yMin);
                        break;
                        case ECenterStandard.UpperCenter:
                        //. 상단 중앙을 기준으로 배치
                        offset = new Vector2(boundingCenter.x - targetCenter.x, boundingRect.yMax - targetRect.yMax);
                        break;
                        case ECenterStandard.UpperLeft:
                        //. 좌측 상단을 기준으로 배치
                        offset = new Vector2(boundingRect.xMin - targetRect.xMin, boundingRect.yMax - targetRect.yMax);
                        break;
                        case ECenterStandard.UpperRight:
                        //. 상단 상단을 기준으로 배치
                        offset = new Vector2(boundingRect.xMax - targetRect.xMax, boundingRect.yMax - targetRect.yMax);
                        break;
                    }

                    //. 계산된 오프셋 반환
                    return offset;
                }



                //? 인접 Velocity를 적용, 내부에서 연산하여 실제로 이동 했는지 여부를 반환한다 (Clamp 연산)
                private bool ApplyVelocity_RoomNearestRoom(StageGenerator main, IReadOnlyList<Space> spaceList, RoomObject placedRoom, Vector2 combineVelocity, Rect clampTransformRect)
                {
                    //. GridUnit 스냅 연산을 하고, 범위를 벗어나지않게 제약한뒤, 적용한다
                    main.Setting.SnapSetting.SnapToGridUnit(ref combineVelocity);

                    //. 캐싱에서 방의 Velocity를 이동시키되, 제약 연산이 고려되어 받아온 velocity가 변할수있다
                    var appliedVelocity = main.placeM.PlacedRoomTFInfoCacheM.AddVelocity_byPlacedRoomTransformInfoCache(spaceList, placedRoom, combineVelocity, clampTransformRect);

                    //? 적용된 velocity가 (0,0)이라면 false, 아니면 true
                    return appliedVelocity != Vector2.zero;
                }


                ////? velocity 적용, 내부 연산에서 이동값이 (0,0) 이 나왔다면 false를 반환한다
                //private bool ApplyVelocity_RoomNearestRoom(StageGenerator main, RoomObject placedRoom, Vector2 combineVelocity, Rect clampRect)
                //{
                //    //. GridUnit 연산을 하고, 범위를 벗어나지않게 제약한뒤, 적용한다
                //    var snap = main.Setting.SnapSetting;

                //    combineVelocity.x = (combineVelocity.x >= 0f) ? Mathf.Ceil(combineVelocity.x * snap.GridUnitX_Width) / snap.GridUnitX_Width : -Mathf.Ceil(-combineVelocity.x * snap.GridUnitX_Width) / snap.GridUnitX_Width;
                //    combineVelocity.y = (combineVelocity.y >= 0f) ? Mathf.Ceil(combineVelocity.y * snap.GridUnitY_Height) / snap.GridUnitY_Height : -Mathf.Ceil(-combineVelocity.y * snap.GridUnitY_Height) / snap.GridUnitY_Height;
                //    combineVelocity = new Vector2(Mathf.Clamp(combineVelocity.x, clampRect.xMin, clampRect.xMax), Mathf.Clamp(combineVelocity.y, clampRect.yMin, clampRect.yMax));

                //    Debug.Log($"<color=red>{placedRoom.CurrentSpace.SpaceIndex}의 방이 이동: {combineVelocity}</color>");

                //    if (combineVelocity == Vector2.zero) { return false; }


                //    main.placeM.PlacedRoomTFInfoCacheM.AddVelocity_byPlacedRoomTransformInfoCache(placedRoom, combineVelocity);

                //    return true;
                //}



                #endregion



                //? 인접 Post 배치
                private void RoomPlaceNearest_PostPlace(StageGenerator main, Transform parent, IReadOnlyList<Space> spaceList, ref Vector2 plusVelocity)
                {
                    float xMin = float.MaxValue;
                    float yMin = float.MaxValue;
                    float xMax = float.MinValue;
                    float yMax = float.MinValue;

                    //. 모든 Rect를 순회하며 가장 작은 좌측 하단 좌표와 가장 큰 우측 상단 좌표를 찾음
                    foreach (var space in spaceList)
                    {
                        if (space.PlacedRoom == null) { continue; }

                        var rect = main.PlaceM.PlacedRoomTFInfoCacheM.GetRoomActivatedRectExpand_byCache(space.PlacedRoom);

                        if (rect.xMin < xMin) xMin = rect.xMin; //. 현재 Rect의 좌측 경계가 더 왼쪽에 있는지 확인
                        if (rect.yMin < yMin) yMin = rect.yMin; //. 현재 Rect의 하단 경계가 더 아래에 있는지 확인
                        if (rect.xMax > xMax) xMax = rect.xMax; //. 현재 Rect의 우측 경계가 더 오른쪽에 있는지 확인
                        if (rect.yMax > yMax) yMax = rect.yMax; //. 현재 Rect의 상단 경계가 더 위에 있는지 확인
                    }

                    //. 계산된 좌측 하단 좌표와 우측 상단 좌표를 사용하여 새로운 Bounding Rect 생성
                    var maxRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
                    plusVelocity = getRectAlignmentOffset(maxRect, main.Setting.Room.RoomPlaceNearest_PostPlace, main.TransformM.StageTransformRect);
                    main.Setting.SnapSetting.SnapToGridUnit(ref plusVelocity);
                }



                ///======================================================================================================================================================



                //? 미사용



                #region 레거시 메서드

                /////<summary>공간을 받아온다, 그리고 공간에 보유중인 "이 공간과 연결중인 공간들"을 사용하여,<br/>
                /////이 공간이 보유중인 <paramref name="dir"/> 방향의 도어를 활성화시킨다 (상하: 좌우순, 좌우: 하상순)</summary>
                /////<param name="space">대상이되는 공간</param>
                /////<param name="dir">어떤 방향의 노드를 활성화할까</param>
                //[Obsolete]
                //private void Legacy_CreateDoorFromSpaceList(Space space, EDirection4 dir)
                //{
                //    //? 방향이 좌/우 인지 확인
                //    bool isDirectionIsHorizontal = dir == EDirection4.Left || dir == EDirection4.Right;

                //    foreach (var connectSpace in space.GetConnecting_SpacesByDir(dir))
                //    {
                //        //? 방향이 좌 or 하 일경우, 도어 리스트의 제일 첫번째 부분을,
                //        //? 방향이 우 or 상 일경우, 도어 리스트의 제일 마지막 부분을 활성화한다
                //        //! 이미 첫번째/마지막 부분이 활성화 되어있다면, 그 다음으로 가까운 비활성화된 도어가 활성화된다

                //        if (checkTwoSpaces(connectSpace)) //좌 or 하
                //        {
                //            space.CurrentRoomObject.RoomDoorM.SetActive_FirstDifferenceOne(dir, true);
                //        }
                //        else //우 or 상
                //        {
                //            space.CurrentRoomObject.RoomDoorM.SetActive_LastDifferenceOne(dir, true);
                //        }
                //    }

                //    //반복문의 "연결 공간"과 공간의 위치를 서로 비교하여, 이 공간으로부터 "연결 공간"이 좌/우 또는 하/상 에 있는지 확인한다
                //    //! 좌/하: true, 우/상: false
                //    bool checkTwoSpaces(Space connectSpace)
                //    {
                //        return (isDirectionIsHorizontal) ? Space.CheckTwoSpacesFromLeft(space, connectSpace) : Space.CheckTwoSpacesFromDown(space, connectSpace);
                //    }
                //}



                /////<summary> [하, 상, 좌, 우 일괄실행]
                /////공간을 받아온다, 그리고 공간에 보유중인 "이 공간과 연결중인 공간들"을 사용하여,<br/>
                /////이 공간이 보유중인 <paramref name="dir"/> 방향의 도어를 활성화시킨다 (상하: 좌우순, 좌우: 하상순)</summary>
                /////<param name="space">대상이되는 공간</param>
                /////<param name="dir">어떤 방향의 노드를 활성화할까</param>
                //[Obsolete]
                //private void Legacy_CreateDoorFromSpaceListAllDirection(Space space)
                //{
                //    Legacy_CreateDoorFromSpaceList(space, EDirection4.Down);
                //    Legacy_CreateDoorFromSpaceList(space, EDirection4.Up);
                //    Legacy_CreateDoorFromSpaceList(space, EDirection4.Left);
                //    Legacy_CreateDoorFromSpaceList(space, EDirection4.Right);
                //}

                #endregion



                ///======================================================================================================================================================
            }



            ///======================================================================================================================================================
        }
    }
}
