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
using Unity.Burst;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Pool;



namespace Pan.StageGenerators
{
    public partial class StageGenerator
    {
        public partial class PlaceManger : BaseManager
        {
            ///======================================================================================================================================================



            ///<summary>
            ///복도 생성기
            /// </summary>
            [Serializable]
            public class Generator4_HallwayCreater
            {
                ///======================================================================================================================================================



                //? 생성



                #region Legacy 복도 생성

                ///// <summary>
                ///// 모든 복도들의 그리드 배치가 끝난 이후에, 복도 오브젝트를 생성한다
                ///// </summary>
                //public void Legacy_Generate(StageGenerator main, Transform parent)
                //{
                //    //. 프리팹 가용성 체크
                //    bool canSpawnHallway = main.PrefabSetting.HallwayPrefabList != null && main.PrefabSetting.HallwayPrefabList.Count > 0;
                //    bool canSpawnHallwayEdge = main.PrefabSetting.HallwayEdgePrefabList != null && main.PrefabSetting.HallwayEdgePrefabList.Count > 0;

                //    //. 커스텀 이벤트: 복도들 소환 이전
                //    main.Setting.CustomEvent.SummonHallway?.Before_SummonHallways(main, main.placeM);

                //    //. 풀/스택 준비 (가용할 때만 미리 생성)
                //    var hallwayCount = main.placeM.PlaceObjects_Hallway.PlacedGrids.Count;
                //    var hallwayEdgeCount = main.placeM.PlaceObjects_HallwayEdge.PlacedGrids.Count;

                //    var willInstanceHallwayStack = new Stack<GridCompatible2Object>(canSpawnHallway ? hallwayCount : 0);
                //    var willInstanceHallwayEdgeStack = new Stack<GridCompatible2Object>(canSpawnHallwayEdge ? hallwayEdgeCount : 0);

                //    if (canSpawnHallway)
                //    {
                //        for (int i = 0; i < hallwayCount; i++)
                //        {
                //            var obj = GetInstance_Hallway(main, parent);
                //            if (obj != null) { willInstanceHallwayStack.Push(obj); } //. null 회피
                //        }
                //    }

                //    if (canSpawnHallwayEdge)
                //    {
                //        for (int i = 0; i < hallwayEdgeCount; i++)    //. //! 버그픽스: 에지 개수 사용
                //        {
                //            var obj = GetInstance_HallwayEdge(main, parent);
                //            if (obj != null) { willInstanceHallwayEdgeStack.Push(obj); } //. null 회피
                //        }
                //    }

                //    foreach (var grid in main.placeM.PlaceObjects_Hallway.PlacedGrids)
                //    {
                //        var gridWorldPosition = grid.WorldPosition_Cached;

                //        //. 커스텀 이벤트: 복도 소환 이전
                //        main.Setting.CustomEvent.SummonHallway?.Before_SummonHallway_ByGrid(main, main.gridM, grid);

                //        //. 커스텀 스위치 + 프리팹 가용성 체크
                //        if (canSpawnHallway && (main.Setting.CustomEvent.SummonHallway == null || !main.Setting.CustomEvent.SummonHallway.Switch_SummonHallway_ByGrid(main, main.gridM, grid)))
                //        {
                //            //? 스택에서 복도를 뽑는다 (없다면 새로 생성한다)
                //            if (!willInstanceHallwayStack.TryPop(out var instanced_Hallway) || instanced_Hallway == null) { instanced_Hallway = GetInstance_Hallway(main, parent); }
                //            if (instanced_Hallway != null)    //. //! null 가드
                //            {
                //                grid.CurrentSummonedObject = instanced_Hallway;
                //                instanced_Hallway.transform.position = gridWorldPosition + instanced_Hallway.GridFloorCenterTransformCurrentPosition;
                //                main.placeM.PlaceObjects_Hallway.AddSummonedObject(instanced_Hallway.gameObject);
                //            }
                //        }

                //        //. 커스텀 이벤트: 복도 소환 이후
                //        main.Setting.CustomEvent.SummonHallway?.After_SummonHallway_ByGrid(main, main.gridM, grid);
                //    }

                //    foreach (var grid in main.placeM.PlaceObjects_HallwayEdge.PlacedGrids)
                //    {
                //        var gridWorldPosition = grid.WorldPosition_Cached;

                //        //. 커스텀 이벤트: 복도 테두리 소환 이전
                //        main.Setting.CustomEvent.SummonHallway?.Before_SummonHallwayEdge_ByGrid(main, main.gridM, grid);

                //        //. 커스텀 스위치 + 프리팹 가용성 체크
                //        if (canSpawnHallwayEdge && (main.Setting.CustomEvent.SummonHallway == null || !main.Setting.CustomEvent.SummonHallway.Switch_SummonHallwayEdge_ByGrid(main, main.gridM, grid)))
                //        {
                //            if (!willInstanceHallwayEdgeStack.TryPop(out var instanced_HallwayEdge) || instanced_HallwayEdge == null) { instanced_HallwayEdge = GetInstance_HallwayEdge(main, parent); }
                //            if (instanced_HallwayEdge != null)   //. //! null 가드
                //            {
                //                grid.CurrentSummonedObject = instanced_HallwayEdge;
                //                instanced_HallwayEdge.transform.position = gridWorldPosition + instanced_HallwayEdge.GridFloorCenterTransformCurrentPosition;
                //                main.placeM.PlaceObjects_HallwayEdge.AddSummonedObject(instanced_HallwayEdge.gameObject);
                //            }
                //        }

                //        //. 커스텀 이벤트: 복도 테두리 소환 이후
                //        main.Setting.CustomEvent.SummonHallway?.After_SummonHallwayEdge_ByGrid(main, main.gridM, grid);
                //    }

                //    //? 남은 복도/복도 테두리가 있다면, 파괴한다
                //    while (willInstanceHallwayStack.Count != 0) { var o = willInstanceHallwayStack.Pop(); if (o != null) o.DestroyAuto(); }
                //    while (willInstanceHallwayEdgeStack.Count != 0) { var o = willInstanceHallwayEdgeStack.Pop(); if (o != null) o.DestroyAuto(); }

                //    //. 커스텀 이벤트: 복도들 소환 이후
                //    main.Setting.CustomEvent.SummonHallway?.After_SummonHallways(main, main.placeM);
                //}


                ///// <summary>
                ///// [비동기] 모든 복도들의 그리드 배치가 끝난 이후에, 복도 오브젝트를 생성한다
                ///// </summary>
                //public async UniTask Legacy_GenerateAsync(StageGenerator main, Transform parent)
                //{
                //    //. 프리팹 가용성 체크
                //    bool canSpawnHallway = main.PrefabSetting.HallwayPrefabList != null && main.PrefabSetting.HallwayPrefabList.Count > 0;
                //    bool canSpawnHallwayEdge = main.PrefabSetting.HallwayEdgePrefabList != null && main.PrefabSetting.HallwayEdgePrefabList.Count > 0;

                //    //. 풀링(비동기) – null 반환 시 빈 스택으로
                //    GridCompatible2Object[] arrH = canSpawnHallway ? (await GetInstance_HallwayAsync(main, parent, main.placeM.PlaceObjects_Hallway.PlacedGrids.Count)) : Array.Empty<GridCompatible2Object>();
                //    GridCompatible2Object[] arrHE = canSpawnHallwayEdge ? (await GetInstance_HallwayEdgeAsync(main, parent, main.placeM.PlaceObjects_HallwayEdge.PlacedGrids.Count)) : Array.Empty<GridCompatible2Object>();

                //    var willInstanceHallways = new Stack<GridCompatible2Object>(arrH ?? Array.Empty<GridCompatible2Object>());
                //    var willInstanceHallwayEdges = new Stack<GridCompatible2Object>(arrHE ?? Array.Empty<GridCompatible2Object>());

                //    // . 커스텀 이벤트: 복도들 소환 이전
                //    await (main.Setting.CustomEvent.SummonHallway?.Before_SummonHallwaysAsync(main, main.placeM) ?? UniTask.CompletedTask);

                //    var hallwayTransforms = new List<Transform>(main.placeM.PlaceObjects_Hallway.PlacedGrids.Count);
                //    var hallwayPositions = new List<Vector3>(main.placeM.PlaceObjects_Hallway.PlacedGrids.Count);
                //    var hallwayEdgeTransforms = new List<Transform>(main.placeM.PlaceObjects_HallwayEdge.PlacedGrids.Count);
                //    var hallwayEdgePositions = new List<Vector3>(main.placeM.PlaceObjects_HallwayEdge.PlacedGrids.Count);

                //    foreach (var grid in main.placeM.PlaceObjects_Hallway.PlacedGrids)
                //    {
                //        var gridWorldPosition = grid.WorldPosition_Cached;
                //        await (main.Setting.CustomEvent.SummonHallway?.Before_SummonHallway_ByGridAsync(main, main.gridM, grid) ?? UniTask.CompletedTask);

                //        if (canSpawnHallway && (main.Setting.CustomEvent.SummonHallway == null || !await main.Setting.CustomEvent.SummonHallway.Switch_SummonHallway_ByGridAsync(main, main.gridM, grid)))
                //        {
                //            if (!willInstanceHallways.TryPop(out var instanced_Hallway) || instanced_Hallway == null) { instanced_Hallway = await GetInstance_HallwayAsync(main, parent); }
                //            if (instanced_Hallway != null)  //. //! null 가드
                //            {
                //                grid.CurrentSummonedObject = instanced_Hallway;
                //                hallwayTransforms.Add(instanced_Hallway.transform);
                //                hallwayPositions.Add(gridWorldPosition + instanced_Hallway.GridFloorCenterTransformCurrentPosition);
                //                main.placeM.PlaceObjects_Hallway.AddSummonedObject(instanced_Hallway.gameObject);
                //            }
                //        }

                //        await (main.Setting.CustomEvent.SummonHallway?.After_SummonHallway_ByGridAsync(main, main.gridM, grid) ?? UniTask.CompletedTask);
                //    }

                //    foreach (var grid in main.placeM.PlaceObjects_HallwayEdge.PlacedGrids)
                //    {
                //        var gridWorldPosition = grid.WorldPosition_Cached;
                //        await (main.Setting.CustomEvent.SummonHallway?.Before_SummonHallwayEdge_ByGridAsync(main, main.gridM, grid) ?? UniTask.CompletedTask);

                //        if (canSpawnHallwayEdge && (main.Setting.CustomEvent.SummonHallway == null || !await main.Setting.CustomEvent.SummonHallway.Switch_SummonHallwayEdge_ByGridAsync(main, main.gridM, grid)))
                //        {
                //            if (!willInstanceHallwayEdges.TryPop(out var instanced_HallwayEdge) || instanced_HallwayEdge == null) { instanced_HallwayEdge = await GetInstance_HallwayEdgeAsync(main, parent); }
                //            if (instanced_HallwayEdge != null) //. //! null 가드
                //            {
                //                grid.CurrentSummonedObject = instanced_HallwayEdge;
                //                hallwayEdgeTransforms.Add(instanced_HallwayEdge.transform);
                //                hallwayEdgePositions.Add(gridWorldPosition + instanced_HallwayEdge.GridFloorCenterTransformCurrentPosition);
                //                main.placeM.PlaceObjects_HallwayEdge.AddSummonedObject(instanced_HallwayEdge.gameObject);
                //            }
                //        }

                //        await (main.Setting.CustomEvent.SummonHallway?.After_SummonHallwayEdge_ByGridAsync(main, main.gridM, grid) ?? UniTask.CompletedTask);
                //    }

                //    //. 일괄 적용(Job)
                //    var hallwayAccessArray = new TransformAccessArray(hallwayTransforms.ToArray());
                //    var hallwayPosArray = new NativeArray<float3>(hallwayPositions.Count, Allocator.TempJob);
                //    var hallwayEdgeAccessArray = new TransformAccessArray(hallwayEdgeTransforms.ToArray());
                //    var hallwayEdgePosArray = new NativeArray<float3>(hallwayEdgePositions.Count, Allocator.TempJob);

                //    try
                //    {
                //        for (int i = 0; i < hallwayPositions.Count; i++) { hallwayPosArray[i] = hallwayPositions[i]; }
                //        for (int i = 0; i < hallwayEdgePositions.Count; i++) { hallwayEdgePosArray[i] = hallwayEdgePositions[i]; }

                //        var hallwayJob = new ApplyPositionJob { positions = hallwayPosArray }.Schedule(hallwayAccessArray);
                //        var hallwayEdgeJob = new ApplyPositionJob { positions = hallwayEdgePosArray }.Schedule(hallwayEdgeAccessArray);

                //        var combinedHandle = JobHandle.CombineDependencies(hallwayJob, hallwayEdgeJob);
                //        combinedHandle.Complete();
                //    }
                //    finally
                //    {
                //        hallwayPosArray.Dispose();
                //        hallwayAccessArray.Dispose();
                //        hallwayEdgePosArray.Dispose();
                //        hallwayEdgeAccessArray.Dispose();
                //    }

                //    //? 남은 복도/복도 테두리가 있다면, 파괴한다
                //    while (willInstanceHallways.Count != 0) { var o = willInstanceHallways.Pop(); if (o != null) o.DestroyAuto(); }
                //    while (willInstanceHallwayEdges.Count != 0) { var o = willInstanceHallwayEdges.Pop(); if (o != null) o.DestroyAuto(); }

                //    // . 커스텀 이벤트: 복도들 소환 이후
                //    await (main.Setting.CustomEvent.SummonHallway?.After_SummonHallwaysAsync(main, main.placeM) ?? UniTask.CompletedTask);
                //}

                #endregion



                /// <summary>
                /// Job에서 병렬로 position을 적용하기 위한 IJobParallelForTransform 구조체
                /// </summary>
                [BurstCompile]
                private struct ApplyPositionJob : IJobParallelForTransform
                {
                    [Unity.Collections.ReadOnly] public NativeArray<float3> positions;
                    public void Execute(int index, TransformAccess transform)
                    {
                        transform.position = positions[index];
                    }
                }



                /// <summary>
                /// 모든 복도들의 그리드 배치가 끝난 이후에, 복도 오브젝트를 생성한다
                /// </summary>
                public void Generate(StageGenerator main, Transform parent)
                {
                    main.GenerateM.GeneratingCurrentSteps = GenerateManager.GenerateSteps.GenStep6_Places_Gen4_CreateHallways;

                    bool canSpawnHallway = main.PrefabSetting.HallwayPrefabList != null && main.PrefabSetting.HallwayPrefabList.Count > 0;
                    bool canSpawnHallwayEdge = main.PrefabSetting.HallwayEdgePrefabList != null && main.PrefabSetting.HallwayEdgePrefabList.Count > 0;

                    var hallwayCount = main.placeM.PlaceObjects_Hallway.PlacedGrids.Count;
                    var hallwayEdgeCount = main.placeM.PlaceObjects_HallwayEdge.PlacedGrids.Count;

                    Vector3 gridPositionPlus = main.Setting.SnapSetting.GetFloorStandardPositionCurrent + new Vector3(0, 0, main.TransformM.StageParentPositionCurrentCache.z).SwizzlesVector(main.Setting.SnapSetting.Swizzle);

                    // 기존 ↓
                    // var willInstanceHallwayStack = new Stack<GridCompatible2Object>(canSpawnHallway ? hallwayCount : 0);
                    // var willInstanceHallwayEdgeStack = new Stack<GridCompatible2Object>(canSpawnHallwayEdge ? hallwayEdgeCount : 0);

                    // 변경 ↓ (List를 LIFO로 사용 + 풀링)
                    var willInstanceHallwayList = ListPool<GridCompatible2Object>.Get(); //? LIFO 버킷
                    var willInstanceHallwayEdgeList = ListPool<GridCompatible2Object>.Get(); //? LIFO 버킷
                    willInstanceHallwayList.Capacity = canSpawnHallway ? hallwayCount : 0; //. 힌트
                    willInstanceHallwayEdgeList.Capacity = canSpawnHallwayEdge ? hallwayEdgeCount : 0; //. 힌트

                    //. 커스텀 이벤트: 복도들 소환 이전
                    if (main.Setting.CustomEvent.SummonHallway != null) { main.Setting.CustomEvent.SummonHallway.Before_SummonHallways(main, main.placeM); }

                    try
                    {
                        //? 미리 풀링 수만큼 확보(가용 시에만)
                        if (canSpawnHallway)
                        {
                            for (int i = 0; i < hallwayCount; i++)
                            {
                                var o = GetInstance_Hallway(main, parent);
                                if (o != null) willInstanceHallwayList.Add(o); //. null 회피
                            }
                        }
                        if (canSpawnHallwayEdge)
                        {
                            for (int i = 0; i < hallwayEdgeCount; i++)
                            {
                                var o = GetInstance_HallwayEdge(main, parent);
                                if (o != null) willInstanceHallwayEdgeList.Add(o); //. null 회피
                            }
                        }

                        var hallwayTransforms = ListPool<Transform>.Get(); //. 위치 일괄 적용용
                        var hallwayPositions = ListPool<Vector3>.Get();
                        var hallwayEdgeTransforms = ListPool<Transform>.Get();
                        var hallwayEdgePositions = ListPool<Vector3>.Get();
                        hallwayTransforms.Capacity = hallwayCount;
                        hallwayPositions.Capacity = hallwayCount;
                        hallwayEdgeTransforms.Capacity = hallwayEdgeCount;
                        hallwayEdgePositions.Capacity = hallwayEdgeCount;

                        try
                        {
                            //? 복도 생성
                            foreach (var grid in main.placeM.PlaceObjects_Hallway.PlacedGrids)
                            {
                                var gridWorldPosition = grid.WorldPosition_Cached + gridPositionPlus;

                                if (main.Setting.CustomEvent.SummonHallway != null) { main.Setting.CustomEvent.SummonHallway.Before_SummonHallway_ByGrid(main, main.gridM, grid); }

                                if (canSpawnHallway && (main.Setting.CustomEvent.SummonHallway == null ||
                                    !main.Setting.CustomEvent.SummonHallway.Switch_SummonHallway_ByGrid(main, main.gridM, grid)))
                                {
                                    // 기존: TryPop → 변경: LIFO (list[^1] 후 RemoveAt)
                                    GridCompatible2Object instanced = null;
                                    if (willInstanceHallwayList.Count != 0)
                                    {
                                        int last = willInstanceHallwayList.Count - 1;
                                        instanced = willInstanceHallwayList[last];
                                        willInstanceHallwayList.RemoveAt(last);
                                    }
                                    if (instanced == null) instanced = GetInstance_Hallway(main, parent);

                                    if (instanced != null)   //. //! null 가드
                                    {
                                        grid.CurrentSummonedObject = instanced;
                                        hallwayTransforms.Add(instanced.transform);
                                        hallwayPositions.Add(gridWorldPosition); //+ instanced.GridFloorCenterTransformCurrentPosition
                                        main.placeM.PlaceObjects_Hallway.AddSummonedObject(instanced.gameObject);
                                    }
                                }

                                if (main.Setting.CustomEvent.SummonHallway != null) { main.Setting.CustomEvent.SummonHallway.After_SummonHallway_ByGrid(main, main.gridM, grid); }
                            }

                            //? 복도 에지 생성
                            foreach (var grid in main.placeM.PlaceObjects_HallwayEdge.PlacedGrids)
                            {
                                var gridWorldPosition = grid.WorldPosition_Cached + gridPositionPlus;

                                if (main.Setting.CustomEvent.SummonHallway != null) { main.Setting.CustomEvent.SummonHallway.Before_SummonHallwayEdge_ByGrid(main, main.gridM, grid); }

                                if (canSpawnHallwayEdge && (main.Setting.CustomEvent.SummonHallway == null ||
                                    !main.Setting.CustomEvent.SummonHallway.Switch_SummonHallwayEdge_ByGrid(main, main.gridM, grid)))
                                {
                                    GridCompatible2Object instanced = null;
                                    if (willInstanceHallwayEdgeList.Count != 0)
                                    {
                                        int last = willInstanceHallwayEdgeList.Count - 1;
                                        instanced = willInstanceHallwayEdgeList[last];
                                        willInstanceHallwayEdgeList.RemoveAt(last);
                                    }
                                    if (instanced == null) instanced = GetInstance_HallwayEdge(main, parent);

                                    if (instanced != null)   //. //! null 가드
                                    {
                                        grid.CurrentSummonedObject = instanced;
                                        hallwayEdgeTransforms.Add(instanced.transform);
                                        hallwayEdgePositions.Add(gridWorldPosition); //+ instanced.GridFloorCenterTransformCurrentPosition
                                        main.placeM.PlaceObjects_HallwayEdge.AddSummonedObject(instanced.gameObject);
                                    }
                                }

                                if (main.Setting.CustomEvent.SummonHallway != null) { main.Setting.CustomEvent.SummonHallway.After_SummonHallwayEdge_ByGrid(main, main.gridM, grid); }
                            }

                            //? 한 번에 적용 (잡 or 루프)
                            ApplyPositionsBatch(main, hallwayTransforms, hallwayPositions, main.CalculateSetting.RoomPlaceUseJob);
                            ApplyPositionsBatch(main, hallwayEdgeTransforms, hallwayEdgePositions, main.CalculateSetting.RoomPlaceUseJob);

                            //? 남은 풀링 오브젝트 정리 (LIFO)
                            while (willInstanceHallwayList.Count != 0) { var o = willInstanceHallwayList[^1]; willInstanceHallwayList.RemoveAt(willInstanceHallwayList.Count - 1); if (o != null) o.DestroyAuto(); }
                            while (willInstanceHallwayEdgeList.Count != 0) { var o = willInstanceHallwayEdgeList[^1]; willInstanceHallwayEdgeList.RemoveAt(willInstanceHallwayEdgeList.Count - 1); if (o != null) o.DestroyAuto(); }

                            //. 커스텀 이벤트: 복도들 소환 이후
                            if (main.Setting.CustomEvent.SummonHallway != null) { main.Setting.CustomEvent.SummonHallway.After_SummonHallways(main, main.placeM); }
                        }
                        finally
                        {
                            // 내부 리스트 반환
                            hallwayTransforms.Clear(); ListPool<Transform>.Release(hallwayTransforms);
                            hallwayPositions.Clear(); ListPool<Vector3>.Release(hallwayPositions);
                            hallwayEdgeTransforms.Clear(); ListPool<Transform>.Release(hallwayEdgeTransforms);
                            hallwayEdgePositions.Clear(); ListPool<Vector3>.Release(hallwayEdgePositions);
                        }
                    }
                    finally
                    {
                        // LIFO 버킷 반환 (반드시 Clear 후)
                        willInstanceHallwayList.Clear(); ListPool<GridCompatible2Object>.Release(willInstanceHallwayList);
                        willInstanceHallwayEdgeList.Clear(); ListPool<GridCompatible2Object>.Release(willInstanceHallwayEdgeList);
                    }
                }




                /// <summary>
                /// [비동기] 모든 복도들의 그리드 배치가 끝난 이후에, 복도 오브젝트를 생성한다
                /// </summary>
                public async UniTask GenerateAsync(StageGenerator main, Transform parent)
                {
                    main.GenerateM.GeneratingCurrentSteps = GenerateManager.GenerateSteps.GenStep6_Places_Gen4_CreateHallways;

                    bool canSpawnHallway = main.PrefabSetting.HallwayPrefabList != null && main.PrefabSetting.HallwayPrefabList.Count > 0;
                    bool canSpawnHallwayEdge = main.PrefabSetting.HallwayEdgePrefabList != null && main.PrefabSetting.HallwayEdgePrefabList.Count > 0;

                    int hallwayCount = main.placeM.PlaceObjects_Hallway.PlacedGrids.Count;
                    int hallwayEdgeCount = main.placeM.PlaceObjects_HallwayEdge.PlacedGrids.Count;

                    //? 풀링(비동기 생성) – null 안전
                    var arrH = canSpawnHallway ? (await GetInstance_HallwayAsync(main, parent, hallwayCount)) : Array.Empty<GridCompatible2Object>();
                    var arrHE = canSpawnHallwayEdge ? (await GetInstance_HallwayEdgeAsync(main, parent, hallwayEdgeCount)) : Array.Empty<GridCompatible2Object>();

                    Vector3 gridPositionPlus = main.Setting.SnapSetting.GetFloorStandardPositionCurrent + new Vector3(0, 0, main.TransformM.StageParentPositionCurrentCache.z).SwizzlesVector(main.Setting.SnapSetting.Swizzle);

                    // 기존 ↓
                    // var willInstanceHallways     = new Stack<GridCompatible2Object>(arrH ?? Array.Empty<GridCompatible2Object>());
                    // var willInstanceHallwayEdges = new Stack<GridCompatible2Object>(arrHE ?? Array.Empty<GridCompatible2Object>());

                    // 변경 ↓ (List-LIFO + 풀링)
                    var willInstanceHallways = ListPool<GridCompatible2Object>.Get();
                    var willInstanceHallwayEdges = ListPool<GridCompatible2Object>.Get();
                    if (arrH != null) { willInstanceHallways.Capacity = arrH.Length; willInstanceHallways.AddRange(arrH); }
                    if (arrHE != null) { willInstanceHallwayEdges.Capacity = arrHE.Length; willInstanceHallwayEdges.AddRange(arrHE); }

                    // . 커스텀 이벤트: 복도들 소환 이전
                    await (main.Setting.CustomEvent.SummonHallway != null ? main.Setting.CustomEvent.SummonHallway.Before_SummonHallwaysAsync(main, main.placeM) : UniTask.CompletedTask);

                    var hallwayTransforms = ListPool<Transform>.Get();
                    var hallwayPositions = ListPool<Vector3>.Get();
                    var hallwayEdgeTransforms = ListPool<Transform>.Get();
                    var hallwayEdgePositions = ListPool<Vector3>.Get();
                    hallwayTransforms.Capacity = hallwayCount;
                    hallwayPositions.Capacity = hallwayCount;
                    hallwayEdgeTransforms.Capacity = hallwayEdgeCount;
                    hallwayEdgePositions.Capacity = hallwayEdgeCount;

                    try
                    {
                        //? 복도
                        foreach (var grid in main.placeM.PlaceObjects_Hallway.PlacedGrids)
                        {
                            var gridWorldPosition = grid.WorldPosition_Cached + gridPositionPlus;

                            await (main.Setting.CustomEvent.SummonHallway != null ? main.Setting.CustomEvent.SummonHallway.Before_SummonHallway_ByGridAsync(main, main.gridM, grid) : UniTask.CompletedTask);

                            if (canSpawnHallway && (main.Setting.CustomEvent.SummonHallway == null ||
                                !await main.Setting.CustomEvent.SummonHallway.Switch_SummonHallway_ByGridAsync(main, main.gridM, grid)))
                            {
                                GridCompatible2Object instanced = null;
                                if (willInstanceHallways.Count != 0)
                                {
                                    int last = willInstanceHallways.Count - 1;
                                    instanced = willInstanceHallways[last];
                                    willInstanceHallways.RemoveAt(last);
                                }
                                if (instanced == null) instanced = await GetInstance_HallwayAsync(main, parent);

                                if (instanced != null)   //. //! null 가드
                                {
                                    grid.CurrentSummonedObject = instanced;
                                    hallwayTransforms.Add(instanced.transform);
                                    hallwayPositions.Add(gridWorldPosition);//+ instanced.GridFloorCenterTransformCurrentPosition
                                    main.placeM.PlaceObjects_Hallway.AddSummonedObject(instanced.gameObject);
                                }
                            }

                            await (main.Setting.CustomEvent.SummonHallway != null ? main.Setting.CustomEvent.SummonHallway.After_SummonHallway_ByGridAsync(main, main.gridM, grid) : UniTask.CompletedTask);

                        }

                        //? 복도 에지
                        foreach (var grid in main.placeM.PlaceObjects_HallwayEdge.PlacedGrids)
                        {
                            var gridWorldPosition = grid.WorldPosition_Cached + gridPositionPlus;

                            await (main.Setting.CustomEvent.SummonHallway != null ? main.Setting.CustomEvent.SummonHallway.Before_SummonHallwayEdge_ByGridAsync(main, main.gridM, grid) : UniTask.CompletedTask);

                            if (canSpawnHallwayEdge && (main.Setting.CustomEvent.SummonHallway == null ||
                                !await main.Setting.CustomEvent.SummonHallway.Switch_SummonHallwayEdge_ByGridAsync(main, main.gridM, grid)))
                            {
                                GridCompatible2Object instanced = null;
                                if (willInstanceHallwayEdges.Count != 0)
                                {
                                    int last = willInstanceHallwayEdges.Count - 1;
                                    instanced = willInstanceHallwayEdges[last];
                                    willInstanceHallwayEdges.RemoveAt(last);
                                }
                                if (instanced == null) instanced = await GetInstance_HallwayEdgeAsync(main, parent);

                                if (instanced != null)   //. //! null 가드
                                {
                                    grid.CurrentSummonedObject = instanced;
                                    hallwayEdgeTransforms.Add(instanced.transform);
                                    hallwayEdgePositions.Add(gridWorldPosition);// + instanced.GridFloorCenterTransformCurrentPosition
                                    main.placeM.PlaceObjects_HallwayEdge.AddSummonedObject(instanced.gameObject);
                                }
                            }

                            await (main.Setting.CustomEvent.SummonHallway != null ? main.Setting.CustomEvent.SummonHallway.After_SummonHallwayEdge_ByGridAsync(main, main.gridM, grid) : UniTask.CompletedTask);
                        }

                        //? 한 번에 적용 (잡 or 분할 루프)
                        await ApplyPositionsBatchAsync(main, hallwayTransforms, hallwayPositions, main.CalculateSetting.RoomPlaceUseJob);
                        await ApplyPositionsBatchAsync(main, hallwayEdgeTransforms, hallwayEdgePositions, main.CalculateSetting.RoomPlaceUseJob);

                        //? 남은 풀링 오브젝트 정리 (LIFO)
                        while (willInstanceHallways.Count != 0) { var o = willInstanceHallways[^1]; willInstanceHallways.RemoveAt(willInstanceHallways.Count - 1); if (o != null) o.DestroyAuto(); }
                        while (willInstanceHallwayEdges.Count != 0) { var o = willInstanceHallwayEdges[^1]; willInstanceHallwayEdges.RemoveAt(willInstanceHallwayEdges.Count - 1); if (o != null) o.DestroyAuto(); }

                        // . 커스텀 이벤트: 복도들 소환 이후
                        await (main.Setting.CustomEvent.SummonHallway != null ? main.Setting.CustomEvent.SummonHallway.After_SummonHallwaysAsync(main, main.placeM) : UniTask.CompletedTask);
                    }
                    finally
                    {
                        // 리스트 반환 (반드시 Clear 후)
                        willInstanceHallways.Clear(); ListPool<GridCompatible2Object>.Release(willInstanceHallways);
                        willInstanceHallwayEdges.Clear(); ListPool<GridCompatible2Object>.Release(willInstanceHallwayEdges);
                        hallwayTransforms.Clear(); ListPool<Transform>.Release(hallwayTransforms);
                        hallwayPositions.Clear(); ListPool<Vector3>.Release(hallwayPositions);
                        hallwayEdgeTransforms.Clear(); ListPool<Transform>.Release(hallwayEdgeTransforms);
                        hallwayEdgePositions.Clear(); ListPool<Vector3>.Release(hallwayEdgePositions);
                    }
                }




                // 동기: Transform/Position 리스트를 받아 조건부로 Job 또는 루프 적용
                void ApplyPositionsBatch(
                    StageGenerator main,
                    List<Transform> transforms,
                    List<Vector3> positions,
                    bool useJobs
                )
                {
                    //! 방어코드: 리스트 길이 불일치라면 적용 불가
                    if (transforms.Count != positions.Count) { return; }

                    int n = transforms.Count;
                    if (n == 0) return;

                    if (!useJobs || n <= main.CalculateSetting.RoomPlaceNotUseJobCount)
                    {
                        for (int i = 0; i < n; i++) transforms[i].position = positions[i];
                        return;
                    }

                    TransformAccessArray taa = default;
                    NativeArray<float3> posNa = default;
                    try
                    {
                        taa = new TransformAccessArray(transforms.ToArray());
                        posNa = new NativeArray<float3>(n, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                        for (int i = 0; i < n; i++) posNa[i] = positions[i];

                        var job = new ApplyPositionJob { positions = posNa };
                        var handle = job.Schedule(taa);
                        handle.Complete();
                    }
                    finally
                    {
                        if (taa.isCreated) taa.Dispose();
                        if (posNa.IsCreated) posNa.Dispose();
                    }
                }



                // 비동기: 잡 or 프레임 분할 루프
                async UniTask ApplyPositionsBatchAsync(
                    StageGenerator main,
                    List<Transform> transforms,
                    List<Vector3> positions,
                    bool useJobs,
                    CancellationToken ct = default
                )
                {
                    ct.ThrowIfCancellationRequested();

                    //! 방어코드: 리스트 길이 불일치라면 적용 불가
                    if (transforms.Count != positions.Count) { return; }

                    int n = transforms.Count;
                    if (n == 0) return;

                    if (!useJobs)
                    {
                        if (n <= main.CalculateSetting.RoomPlaceNotUseJobCount)
                        {
                            for (int i = 0; i < n; i++)
                            {
                                ct.ThrowIfCancellationRequested();
                                transforms[i].position = positions[i];
                            }
                            return;
                        }

                        const int Chunk = 256;
                        int i0 = 0;
                        while (i0 < n)
                        {
                            ct.ThrowIfCancellationRequested();
                            int end = Mathf.Min(i0 + Chunk, n);
                            for (int i = i0; i < end; i++) transforms[i].position = positions[i];
                            i0 = end;
                            await UniTask.Yield(PlayerLoopTiming.Update, ct);
                        }
                        return;
                    }

                    TransformAccessArray taa = default;
                    NativeArray<float3> posNa = default;
                    try
                    {
                        taa = new TransformAccessArray(transforms.ToArray());
                        posNa = new NativeArray<float3>(n, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                        for (int i = 0; i < n; i++) posNa[i] = positions[i];

                        var job = new ApplyPositionJob { positions = posNa };
                        var handle = job.Schedule(taa);

                        while (!handle.IsCompleted)
                        {
                            ct.ThrowIfCancellationRequested();
                            await UniTask.Yield(PlayerLoopTiming.EarlyUpdate, ct);
                        }
                        handle.Complete();
                    }
                    finally
                    {
                        if (taa.isCreated) taa.Dispose();
                        if (posNa.IsCreated) posNa.Dispose();
                    }
                }



                ///======================================================================================================================================================



                //? 복도 오브젝트 얻기 & 소환



                ///<summary>
                /// 복도 오브젝트를 무작위로 얻기
                /// </summary>
                private GridCompatible2Object GetHallway_Rand(StageGenerator main)
                {
                    if (main.PrefabSetting.HallwayPrefabList == null || main.PrefabSetting.HallwayPrefabList.Count == 0) { return null; }
                    return main.PrefabSetting.HallwayPrefabList[main.GenerateInfoM.Random.Range(0, main.PrefabSetting.HallwayPrefabList.Count)];
                }

                ///<summary>
                /// 복도 테두리 오브젝트를 무작위로 얻기
                /// </summary>
                private GridCompatible2Object GetHallwayEdge_Rand(StageGenerator main)
                {
                    if (main.PrefabSetting.HallwayEdgePrefabList == null || main.PrefabSetting.HallwayEdgePrefabList.Count == 0) { return null; }
                    return main.PrefabSetting.HallwayEdgePrefabList[main.GenerateInfoM.Random.Range(0, main.PrefabSetting.HallwayEdgePrefabList.Count)];   //. //! 버그픽스: 올바른 리스트 Count 사용
                }



                /// <summary>
                /// 복도 오브젝트를 생성하여 반환한다
                /// </summary>
                /// <param name="target">null일경우, 리스트에서 무작위로 선택되어 생성된다</param>
                private GridCompatible2Object GetInstance_Hallway(StageGenerator main, Transform parent, GridCompatible2Object target = null)
                {
                    var hallway_Selected = target != null ? target : GetHallway_Rand(main);
                    if (hallway_Selected == null) { return null; }
                    var instanced_Hallway = Instantiate(hallway_Selected, parent);
                    return instanced_Hallway;
                }



                /// <summary>
                /// 복도 오브젝트를 생성하여 반환한다
                /// </summary>
                /// <param name="target">null일경우, 리스트에서 무작위로 선택되어 생성된다</param>
                private GridCompatible2Object GetInstance_HallwayEdge(StageGenerator main, Transform parent, GridCompatible2Object target = null)
                {
                    var hallwayEdge_Selected = target != null ? target : GetHallwayEdge_Rand(main);
                    if (hallwayEdge_Selected == null) { return null; }
                    var instanced_HallwayEdge = Instantiate(hallwayEdge_Selected, parent);
                    return instanced_HallwayEdge;
                }



                /// <summary>
                /// 복도 오브젝트를 생성하여 반환한다
                /// </summary>
                /// <param name="target">null일경우, 리스트에서 무작위로 선택되어 생성된다</param>
                private async UniTask<GridCompatible2Object> GetInstance_HallwayAsync(StageGenerator main, Transform parent, GridCompatible2Object target = null)
                {
                    var hallway_Selected = target != null ? target : GetHallway_Rand(main);
                    if (hallway_Selected == null) { return null; }
                    var operation = await InstantiateAsync(hallway_Selected, 1, parent);

                    if (operation != null)
                    {
                        return operation[0];
                    }

                    return null;
                }



                /// <summary>
                /// 복도 오브젝트를 생성하여 반환한다
                /// </summary>
                /// <param name="target">null일경우, 리스트에서 무작위로 선택되어 생성된다</param>
                private async UniTask<GridCompatible2Object[]> GetInstance_HallwayAsync(StageGenerator main, Transform parent, int count, GridCompatible2Object target = null)
                {
                    var hallway_Selected = target != null ? target : GetHallway_Rand(main);
                    if (hallway_Selected == null) { return null; }
                    if (count == 0) { return null; }
                    var operation = await InstantiateAsync(hallway_Selected, count, parent);

                    if (operation != null)
                    {
                        return operation;
                    }

                    return null;
                }



                /// <summary>
                /// 복도 오브젝트를 생성하여 반환한다
                /// </summary>
                /// <param name="target">null일경우, 리스트에서 무작위로 선택되어 생성된다</param>
                private async UniTask<GridCompatible2Object> GetInstance_HallwayEdgeAsync(StageGenerator main, Transform parent, GridCompatible2Object target = null)
                {
                    var hallwayEdge_Selected = target != null ? target : GetHallwayEdge_Rand(main);
                    if (hallwayEdge_Selected == null) { return null; }
                    var operation = await InstantiateAsync(hallwayEdge_Selected, 1, parent);

                    if (operation != null)
                    {
                        return operation[0];
                    }

                    return null;
                }



                /// <summary>
                /// 복도 오브젝트를 생성하여 반환한다
                /// </summary>
                /// <param name="target">null일경우, 리스트에서 무작위로 선택되어 생성된다</param>
                private async UniTask<GridCompatible2Object[]> GetInstance_HallwayEdgeAsync(StageGenerator main, Transform parent, int count, GridCompatible2Object target = null)
                {
                    var hallwayEdge_Selected = target != null ? target : GetHallwayEdge_Rand(main);
                    if (hallwayEdge_Selected == null) { return null; }
                    if (count == 0) { return null; }
                    var operation = await InstantiateAsync(hallwayEdge_Selected, count, parent);

                    if (operation != null)
                    {
                        return operation;
                    }

                    return null;
                }



                ///======================================================================================================================================================
            }



            ///======================================================================================================================================================
        }
    }
}