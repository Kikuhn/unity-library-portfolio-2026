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
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Collections;
using UnityEngine.Pool;



namespace Pan.StageGenerators
{
    public partial class StageGenerator
    {
        public partial class PlaceManger : BaseManager
        {
            ///======================================================================================================================================================



            ///<summary>
            ///길 이어주기
            /// </summary>
            [Serializable]
            public class Generator_ASharpPathFinder
            {
                ///======================================================================================================================================================



                //? 델리게이트



                ///<summary>경로 찾기 실패 전용 델리게이트</summary>
                ///<param name="integrated_bannedMainPathList">전체 금지 경로 리스트</param>
                ///<param name="current_bannedMainPathList">현재 루프의 금지 경로 리스트</param>
                public delegate bool FailedPathFinding_Retry(List<Vector2Int> integrated_bannedMainPathList, List<Vector2Int> current_bannedMainPathList);



                ///<summary>[비동기] 경로 찾기 실패 전용 델리게이트</summary>
                ///<param name="integrated_bannedMainPathList">전체 금지 경로 리스트</param>
                ///<param name="current_bannedMainPathList">현재 루프의 금지 경로 리스트</param>
                public delegate UniTask<bool> FailedPathFinding_RetryAsync(List<Vector2Int> integrated_bannedMainPathList, List<Vector2Int> current_bannedMainPathList);



                ///======================================================================================================================================================



                //? 패스파인딩 실행



                /// <summary> 그리드 좌표의 시작점과 끝 좌표를 받아와, 그 둘을 이어주는 Vector2Int 리스트를 반환한다<br/>
                /// (A* 알고리즘을 사용하여 경로 탐색)
                /// </summary>
                public bool Execute_PathFinding(StageGenerator main, List<Vector2Int> targetPathList, Vector2Int start, Vector2Int end, int pathWidth, int pathHeight, int safeExpand, bool expandPositiveHorizontal, bool expandPositiveVertical, HeuristicType pathHeuristicType = HeuristicType.Manhattan, float? pathHeuristicFactor = null, Vector2Int? clampBottomLeft = null, Vector2Int? clampTopRight = null, FailedPathFinding_Retry failed_RetryAction = null, IReadOnlyCollection<Grid> IgnoreWhenExpandGrids_A = null, IReadOnlyCollection<Grid> IgnoreWhenExpandGrids_B = null)
                {
                    pathWidth = Mathf.Max(1, pathWidth);
                    pathHeight = Mathf.Max(1, pathHeight);
                    targetPathList = Start_PathFinding(main, targetPathList, start, end, pathWidth, pathHeight, safeExpand, expandPositiveHorizontal, expandPositiveVertical, pathHeuristicType, pathHeuristicFactor, clampBottomLeft, clampTopRight, false, failed_RetryAction, IgnoreWhenExpandGrids_A, IgnoreWhenExpandGrids_B);


                    //! 최대 경로 반복 횟수를 넘거나, 재시도를 했는데도 실패했거나, 뭘했든 경로 탐색에 실패한다면 실패
                    if (targetPathList == null || targetPathList.Count == 0)
                    {
                        main.logM.LogError_Gen_PathFinding(main, start, end, pathWidth, pathHeight, expandPositiveVertical, expandPositiveHorizontal, pathHeuristicType, pathHeuristicFactor, clampBottomLeft, clampTopRight, failed_RetryAction, IgnoreWhenExpandGrids_A, IgnoreWhenExpandGrids_B);
                        return false;
                    }


                    return true;
                }



                /// <summary>[비동기] 그리드 좌표의 시작점과 끝 좌표를 받아와, 그 둘을 이어주는 Vector2Int 리스트를 반환한다<br/>
                /// (A* 알고리즘을 사용하여 경로 탐색)
                /// </summary>
                public async UniTask<bool> Execute_PathFindingAsync(StageGenerator main, List<Vector2Int> targetPathList, Vector2Int start, Vector2Int end, int pathWidth, int pathHeight, int safeExpand, bool expandPositiveHorizontal, bool expandPositiveVertical, HeuristicType pathHeuristicType = HeuristicType.Manhattan, float? pathHeuristicFactor = null, Vector2Int? clampBottomLeft = null, Vector2Int? clampTopRight = null, FailedPathFinding_RetryAsync failed_RetryAction = null, IReadOnlyCollection<Grid> IgnoreWhenExpandGrids_A = null, IReadOnlyCollection<Grid> IgnoreWhenExpandGrids_B = null)
                {
                    pathWidth = Mathf.Max(1, pathWidth);
                    pathHeight = Mathf.Max(1, pathHeight);
                    var resultPathList = await Start_PathFindingAsync(main, targetPathList, start, end, pathWidth, pathHeight, safeExpand, expandPositiveHorizontal, expandPositiveVertical, pathHeuristicType, pathHeuristicFactor, clampBottomLeft, clampTopRight, false, failed_RetryAction, IgnoreWhenExpandGrids_A, IgnoreWhenExpandGrids_B);


                    //! 최대 경로 반복 횟수를 넘거나, 재시도를 했는데도 실패했거나, 뭘했든 경로 탐색에 실패한다면 실패
                    if (resultPathList == null || resultPathList.Count == 0)
                    {
                        main.logM.LogError_Gen_PathFindingAsync(main, start, end, pathWidth, pathHeight, expandPositiveVertical, expandPositiveHorizontal, pathHeuristicType, pathHeuristicFactor, clampBottomLeft, clampTopRight, failed_RetryAction, IgnoreWhenExpandGrids_A, IgnoreWhenExpandGrids_B);
                        return false;
                    }

                    return true;
                }



                private readonly List<Vector2Int> pathFinding_Integrated_bannedMainPathList = new List<Vector2Int>(); //. 통합 밴 메인경로 리스트
                private readonly List<Vector2Int> pathFinding_current_bannedMainPathList = new List<Vector2Int>(); //. 현재 밴 메인경로 리스트



                /// <summary>
                /// 핵심 패스파인딩 시작
                /// </summary>
                /// <param name="main"></param>
                /// <param name="start">시작</param>
                /// <param name="end">끝</param>
                /// <param name="pathWidth">경로의 너비</param>
                /// <param name="pathHeight">경로늬 높이</param>
                /// <param name="expandPositiveHorizontal">너비를 확장할때, 우측을 우선확장</param>
                /// <param name="expandPositiveVertical">너비를 확장할때, 하단을 우선확장</param>
                /// <param name="pathHeuristicType">휴리스틱 타입</param>
                /// <param name="pathHeuristicFactor">휴리스틱 값</param>
                /// <param name="clampBottomLeft">범위 제한 [좌측하단]</param>
                /// <param name="clampTopRight">범위 제한 [우측상단]</param>
                /// <param name="isRetrying_ExpandClamp">범위를 제한했다가 경로 탐색에 실패해, 범위 제한을 조정한뒤 재시도중인지 여부</param>
                /// <param name="failed_RetryAction">실패시 이벤트</param>
                /// <param name="IgnoreWhenExpandGrids_A">이 배열의 그리드들은 확장할때 충돌무시A</param>
                /// <param name="IgnoreWhenExpandGrids_B">이 배열의 그리드들은 확장할때 충돌무시B</param>
                private List<Vector2Int> Start_PathFinding(StageGenerator main, List<Vector2Int> targetPathList, Vector2Int start, Vector2Int end, int pathWidth, int pathHeight, int safeExpand, bool expandPositiveHorizontal, bool expandPositiveVertical, HeuristicType pathHeuristicType = HeuristicType.Manhattan, float? pathHeuristicFactor = null, Vector2Int? clampBottomLeft = null, Vector2Int? clampTopRight = null, bool isRetrying_ExpandClamp = false, FailedPathFinding_Retry failed_RetryAction = null, IReadOnlyCollection<Grid> IgnoreWhenExpandGrids_A = null, IReadOnlyCollection<Grid> IgnoreWhenExpandGrids_B = null)
                {
                    GridManager gridManager = main.gridM;


                    pathFinding_Integrated_bannedMainPathList.Clear();
                    pathFinding_current_bannedMainPathList.Clear();


                    int count = 0;


                    while (true)
                    {
                        //! 최대 경로 재탐색 횟수보다 크면, 무한루프로 간주하여 실패한다
                        if (gridManager.StageGenerator.Setting.Hallawy.HallwayPathFinding_MaxReCalculateLoopCount <= count)
                        {
                            //Debug.LogError($"(코드내부) <color=red><b>{start}~{end} 실패 {count}회반복</b></color>");
                            ////? 하지만 재구성 코드 실행 이후 재시도 하는것을 반환
                            //if (failed_RetryAction != null && failed_RetryAction.Invoke(integrated_bannedMainPathList, current_bannedMainPathList))
                            //{
                            //    return Start_PathFinding(main, start, end, pathWidth, pathHeight, expandPositiveHorizontal, expandPositiveVertical, pathHeuristicType, pathHeuristicFactor, clampBottomLeft, clampTopRight, false, null, IgnoreWhenExpandGrids_A, IgnoreWhenExpandGrids_B);
                            //}

                            main.logM.LogError_Gen_PathFinding_InfinityLoop(main, count);
                            return null;
                        }


                        //? 패스파인딩 시작! (제외 해야하는 좌표들이 있다면 포함)
                        bool pathFinding = Pathfind.PathFinding(gridManager, targetPathList, start, end, pathHeuristicType, pathHeuristicFactor, pathFinding_Integrated_bannedMainPathList, clampBottomLeft, clampTopRight);


                        //! 경로 찾기 자체에 실패
                        if (!pathFinding)
                        {
                            //? Clamp를 사용하여 제한된 범위 안에서만 한것이 원인이 될수도 있으므로, 확장한뒤 시도해본다 (1번만)
                            //. 리미트 해제 패스파인딩!
                            if ((clampBottomLeft.HasValue || clampTopRight.HasValue) && isRetrying_ExpandClamp == false)
                            {
                                int expandMaxSize = Mathf.Max(pathWidth, pathHeight);
                                if (clampBottomLeft.HasValue) { clampBottomLeft -= new Vector2Int(expandMaxSize, expandMaxSize); }
                                if (clampTopRight.HasValue) { clampTopRight += new Vector2Int(expandMaxSize, expandMaxSize); }
                                //! 그냥 Clamp자체를 없애버리면 어떻게든 억지로 경로가 생성되어버려 오히려 생성에 큰 방해가됨!

                                return Start_PathFinding(main, targetPathList, start, end, pathWidth, pathHeight, safeExpand, expandPositiveHorizontal, expandPositiveVertical, pathHeuristicType, pathHeuristicFactor, clampBottomLeft, clampTopRight, true, failed_RetryAction, IgnoreWhenExpandGrids_A, IgnoreWhenExpandGrids_B);
                            }

                            //? 그래도 안되면 하지만 재구성 코드 실행 이후 재시도 하는것을 반환
                            if (failed_RetryAction != null && failed_RetryAction.Invoke(pathFinding_Integrated_bannedMainPathList, pathFinding_current_bannedMainPathList))
                            {

                                return Start_PathFinding(main, targetPathList, start, end, pathWidth, pathHeight, safeExpand, expandPositiveHorizontal, expandPositiveVertical, pathHeuristicType, pathHeuristicFactor, clampBottomLeft, clampTopRight, false, null, IgnoreWhenExpandGrids_A, IgnoreWhenExpandGrids_B);
                            }

                            return null;
                        }


                        //? 경로 리스트를, 너비를 각각 확장하여, 확장된 부분이 점유상태라면, 해당 경로를 리스트에 추가한다
                        if (CheckPathList_ExpandWidth_Conditions(gridManager, targetPathList, pathWidth, pathHeight, safeExpand, expandPositiveHorizontal, expandPositiveVertical,
                            in pathFinding_current_bannedMainPathList, pathFinding_Integrated_bannedMainPathList, in IgnoreWhenExpandGrids_A, in IgnoreWhenExpandGrids_B))
                        {
                            //! 리스트가 0이면, 확장된 좌표가 모두 미점유 상태라는것이므로, 경로 생성에 성공한다!
                            // 반복문 탈출
                            break;
                        }


                        //. 제외할 중심 좌표를 제외 목록에 추가한다
                        foreach (var bannedMainPath in pathFinding_current_bannedMainPathList)
                        {
                            pathFinding_Integrated_bannedMainPathList.Add(bannedMainPath);
                            var grid = gridManager.GetGrid(bannedMainPath);
                            grid.AddTag(GridTag.WasBanned_MainPath);
                            //grid.GridDebuggingMemo += $"Start: {start} // End: {end}\n";
                        }


                        //. 매 반복마다 밴 리스트 초기화
                        pathFinding_current_bannedMainPathList.Clear();


                        count++;
                    }


                    return targetPathList;
                }



                /// <summary>
                /// [비동기] 핵심 패스파인딩 시작
                /// </summary>
                /// <param name="main"></param>
                /// <param name="start">시작</param>
                /// <param name="end">끝</param>
                /// <param name="pathWidth">경로의 너비</param>
                /// <param name="pathHeight">경로늬 높이</param>
                /// <param name="expandPositiveHorizontal">너비를 확장할때, 우측을 우선확장</param>
                /// <param name="expandPositiveVertical">너비를 확장할때, 하단을 우선확장</param>
                /// <param name="pathHeuristicType">휴리스틱 타입</param>
                /// <param name="pathHeuristicFactor">휴리스틱 값</param>
                /// <param name="clampBottomLeft">범위 제한 [좌측하단]</param>
                /// <param name="clampTopRight">범위 제한 [우측상단]</param>
                /// <param name="isRetrying_ExpandClamp">범위를 제한했다가 경로 탐색에 실패해, 범위 제한을 조정한뒤 재시도중인지 여부</param>
                /// <param name="failed_RetryAction">실패시 이벤트</param>
                /// <param name="IgnoreWhenExpandGrids_A">이 배열의 그리드들은 확장할때 충돌무시A</param>
                /// <param name="IgnoreWhenExpandGrids_B">이 배열의 그리드들은 확장할때 충돌무시B</param>
                private async UniTask<List<Vector2Int>> Start_PathFindingAsync(StageGenerator main, List<Vector2Int> targetPathList, Vector2Int start, Vector2Int end, int pathWidth, int pathHeight, int safeExpand, bool expandPositiveHorizontal, bool expandPositiveVertical, HeuristicType pathHeuristicType = HeuristicType.Manhattan, float? pathHeuristicFactor = null, Vector2Int? clampBottomLeft = null, Vector2Int? clampTopRight = null, bool isRetrying_ExpandClamp = false, FailedPathFinding_RetryAsync failed_RetryAction = null, IReadOnlyCollection<Grid> IgnoreWhenExpandGrids_A = null, IReadOnlyCollection<Grid> IgnoreWhenExpandGrids_B = null)
                {
                    GridManager gridManager = main.gridM;


                    pathFinding_Integrated_bannedMainPathList.Clear();
                    pathFinding_current_bannedMainPathList.Clear();


                    int count = 0;


                    while (true)
                    {
                        //! 최대 경로 재탐색 횟수보다 크면, 무한루프로 간주하여 실패한다
                        if (gridManager.Setting.Hallawy.HallwayPathFinding_MaxReCalculateLoopCount <= count)
                        {
                            //Debug.LogError($"(코드내부) <color=red><b>{start}~{end} 실패 {count}회반복</b></color>");
                            ////? 하지만 재구성 코드 실행 이후 재시도 하는것을 반환
                            //if (failed_RetryAction != null && failed_RetryAction.Invoke(integrated_bannedMainPathList, current_bannedMainPathList))
                            //{
                            //    return Start_PathFinding(main, start, end, pathWidth, pathHeight, expandPositiveHorizontal, expandPositiveVertical, pathHeuristicType, pathHeuristicFactor, clampBottomLeft, clampTopRight, false, null, IgnoreWhenExpandGrids_A, IgnoreWhenExpandGrids_B);
                            //}

                            main.logM.LogError_Gen_PathFinding_InfinityLoop(main, count);
                            return null;
                        }


                        //? 패스파인딩 시작! (제외 해야하는 좌표들이 있다면 포함)
                        bool pathFinding = Pathfind.PathFinding(gridManager, targetPathList, start, end, pathHeuristicType, pathHeuristicFactor, pathFinding_Integrated_bannedMainPathList, clampBottomLeft, clampTopRight);


                        await UniTask.Yield(); //. 매 탐색마다 고정 양보


                        //! 경로 찾기 자체에 실패
                        if (!pathFinding)
                        {
                            //? Clamp를 사용하여 제한된 범위 안에서만 한것이 원인이 될수도 있으므로, 확장한뒤 시도해본다 (1번만)
                            //. 리미트 해제 패스파인딩!
                            if ((clampBottomLeft.HasValue || clampTopRight.HasValue) && isRetrying_ExpandClamp == false)
                            {
                                int expandMaxSize = Mathf.Max(pathWidth, pathHeight);
                                if (clampBottomLeft.HasValue) { clampBottomLeft -= new Vector2Int(expandMaxSize, expandMaxSize); }
                                if (clampTopRight.HasValue) { clampTopRight += new Vector2Int(expandMaxSize, expandMaxSize); }
                                //! 그냥 Clamp자체를 없애버리면 어떻게든 억지로 경로가 생성되어버려 오히려 생성에 큰 방해가됨!

                                return await Start_PathFindingAsync(main, targetPathList, start, end, pathWidth, pathHeight, safeExpand, expandPositiveHorizontal, expandPositiveVertical, pathHeuristicType, pathHeuristicFactor, clampBottomLeft, clampTopRight, true, failed_RetryAction, IgnoreWhenExpandGrids_A, IgnoreWhenExpandGrids_B);
                            }

                            //? 그래도 안되면 하지만 재구성 코드 실행 이후 재시도 하는것을 반환
                            if (failed_RetryAction != null && await failed_RetryAction.Invoke(pathFinding_Integrated_bannedMainPathList, pathFinding_current_bannedMainPathList))
                            {

                                return await Start_PathFindingAsync(main, targetPathList, start, end, pathWidth, pathHeight, safeExpand, expandPositiveHorizontal, expandPositiveVertical, pathHeuristicType, pathHeuristicFactor, clampBottomLeft, clampTopRight, false, null, IgnoreWhenExpandGrids_A, IgnoreWhenExpandGrids_B);
                            }

                            return null;
                        }


                        //? 경로 리스트를, 너비를 각각 확장하여, 확장된 부분이 점유상태라면, 해당 경로를 리스트에 추가한다
                        if (CheckPathList_ExpandWidth_Conditions(gridManager, targetPathList, pathWidth, pathHeight, safeExpand, expandPositiveHorizontal, expandPositiveVertical,
                            in pathFinding_current_bannedMainPathList, pathFinding_Integrated_bannedMainPathList, in IgnoreWhenExpandGrids_A, in IgnoreWhenExpandGrids_B))
                        {
                            //! 리스트가 0이면, 확장된 좌표가 모두 미점유 상태라는것이므로, 경로 생성에 성공한다!
                            // 반복문 탈출
                            break;
                        }


                        //. 제외할 중심 좌표를 제외 목록에 추가한다
                        foreach (var bannedMainPath in pathFinding_current_bannedMainPathList)
                        {
                            pathFinding_Integrated_bannedMainPathList.Add(bannedMainPath);
                            var grid = gridManager.GetGrid(bannedMainPath);
                            grid.AddTag(GridTag.WasBanned_MainPath);
                            //grid.GridDebuggingMemo += $"Start: {start} // End: {end}\n";
                        }


                        //. 매 반복마다 밴 리스트 초기화
                        pathFinding_current_bannedMainPathList.Clear();


                        count++;


                        await UniTask.Yield(); //. 매 재탐색마다 고정 양보
                    }


                    return targetPathList;
                }



                ///======================================================================================================================================================



                //? A* 휴리스틱 맨허튼 경로탐색 메서드



                public enum HeuristicType
                {
                    Manhattan,             // 뻣뻣한 직선 경로 (맨해튼 거리 기반)
                    Euclidean,             // 부드럽게1: 유클리드 거리 기반
                    WeightedDiagonal,      // 부드럽게2: 대각선 선호 거리 기반
                    LerpHybrid,            // 부드럽게3: 유클리드와 맨해튼 혼합 (보간 방식)
                    WeightedEuclidean,     // 부드럽게4: 유클리드 거리 기반, 가중치 추가
                    AdaptiveWeightedHybrid,// 부드럽게5: 동적 가중치 기반 하이브리드 방식
                    SplineInterpolation,   // 부드럽게6: Spline 보간을 사용한 경로 생성
                    Relaxation             // 부드럽게7: Relaxation 기법으로 꺾임을 부드럽게
                }



                public class Pathfinder : IDisposable
                {


                    public const int MOVECOST_DEFAULT = 1;


                    // 필드 변수 선언
                    private IndexedPriorityQueue<Vector2Int> _openList;
                    private Dictionary<Vector2Int, Vector2Int> _cameFrom;
                    private Dictionary<Vector2Int, int> _gScore;
                    private Dictionary<Vector2Int, int> _fScore;
                    private HashSet<Vector2Int> _closedList;



                    public bool EnablePathFinder
                    {
                        get => enablePathFinder;
                        set
                        {
                            if (!enablePathFinder && value)
                            {
                                _openList = new IndexedPriorityQueue<Vector2Int>();
                                _cameFrom = new Dictionary<Vector2Int, Vector2Int>();
                                _gScore = new Dictionary<Vector2Int, int>();
                                _fScore = new Dictionary<Vector2Int, int>();
                                _closedList = new HashSet<Vector2Int>();
                            }
                            else if (enablePathFinder && !value)
                            {
                                _openList = null;
                                _cameFrom = null;
                                _gScore = null;
                                _fScore = null;
                                _closedList = null;
                            }
                            enablePathFinder = value;
                        }
                    }
                    public bool enablePathFinder = false;



                    public void Dispose()
                    {
                        EnablePathFinder = false;
                    }



                    public Pathfinder()
                    {
                        // 필드 초기화
                        _openList = new IndexedPriorityQueue<Vector2Int>();
                        _cameFrom = new Dictionary<Vector2Int, Vector2Int>();
                        _gScore = new Dictionary<Vector2Int, int>();
                        _fScore = new Dictionary<Vector2Int, int>();
                        _closedList = new HashSet<Vector2Int>();
                        EnablePathFinder = true;
                    }



                    public bool PathFinding(
                        GridManager gridManager,
                        List<Vector2Int> targetPathList,
                        in Vector2Int start,
                        in Vector2Int goal,
                        HeuristicType pathHeuristicType = HeuristicType.Manhattan,
                        float? pathHeuristic = null,
                        IReadOnlyCollection<Vector2Int> bannedPositions = null,
                        Vector2Int? bottomLeft = null,
                        Vector2Int? topRight = null)
                    {
                        if (!EnablePathFinder) { EnablePathFinder = true; }


                        // 필드 초기화
                        _openList.Clear();
                        _cameFrom.Clear();
                        _gScore.Clear();
                        _fScore.Clear();
                        _closedList.Clear();


                        _gScore[start] = 0;
                        _fScore[start] = CalculateHeuristic(start, goal, pathHeuristicType, pathHeuristic);
                        _openList.TryEnqueue(start, _fScore[start]);


                        while (_openList.Count > 0)
                        {
                            var current = _openList.Dequeue();

                            if (current == goal)
                            {
                                Hallways_FindPath_ReconstructPath(targetPathList, _cameFrom, current);
                                return true;
                            }

                            _closedList.Add(current);

                            foreach (var neighbor in gridManager.GetNeighborsGrids_NotOccupied(current))
                            {
                                if (bottomLeft.HasValue && topRight.HasValue)
                                {
                                    if (neighbor.x < bottomLeft.Value.x || neighbor.x > topRight.Value.x ||
                                        neighbor.y < bottomLeft.Value.y || neighbor.y > topRight.Value.y)
                                    {
                                        continue;
                                    }
                                }


                                if (_closedList.Contains(neighbor) || (bannedPositions != null && bannedPositions.Contains(neighbor)))
                                {
                                    continue;
                                }


                                int tentativeGScore = _gScore[current] + MOVECOST_DEFAULT + gridManager.GetGrid(neighbor).PathCost;

                                if (!_gScore.ContainsKey(neighbor) || tentativeGScore < _gScore[neighbor])
                                {
                                    _cameFrom[neighbor] = current;
                                    _gScore[neighbor] = tentativeGScore;

                                    int heuristic = CalculateHeuristic(neighbor, goal, pathHeuristicType, pathHeuristic);
                                    _fScore[neighbor] = _gScore[neighbor] + heuristic;

                                    if (!_openList.TryEnqueue(neighbor, _fScore[neighbor]))
                                    {
                                        _openList.UpdatePriority(neighbor, _fScore[neighbor]);
                                    }
                                }
                            }
                        }
                        return false;
                    }



                    private int CalculateHeuristic(Vector2Int a, Vector2Int b, HeuristicType heuristicType, float? factor)
                    {
                        // -1은 null과 동일하게 처리
                        float? adjustedFactor = factor == -1 ? null : factor;

                        switch (heuristicType)
                        {
                            case HeuristicType.Manhattan:
                            // 뻣뻣한 직선 경로 (맨해튼 거리 기반)
                            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

                            case HeuristicType.Euclidean:
                            // 부드럽게1: 유클리드 거리 기반
                            return Mathf.RoundToInt(Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y)));

                            case HeuristicType.WeightedDiagonal:
                            {
                                // 부드럽게2: 대각선 선호 거리 기반
                                float weight = adjustedFactor ?? 0.9f; // 기본 가중치
                                weight = Mathf.Clamp01(weight); // 안전한 범위로 Clamp
                                int dx = Mathf.Abs(a.x - b.x);
                                int dy = Mathf.Abs(a.y - b.y);
                                return Mathf.RoundToInt(Mathf.Sqrt(dx * dx + dy * dy) * weight + Mathf.Min(dx, dy) * (1 - weight));
                            }

                            case HeuristicType.LerpHybrid:
                            {
                                // 부드럽게3: 유클리드와 맨해튼 혼합 (보간 방식)
                                float lerpFactor = adjustedFactor ?? 0.5f; // 기본 보간 계수
                                lerpFactor = Mathf.Clamp01(lerpFactor); // 안전한 범위로 Clamp
                                float distance = Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
                                return Mathf.RoundToInt(Mathf.Lerp(distance, Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y), lerpFactor));
                            }

                            case HeuristicType.WeightedEuclidean:
                            {
                                // 부드럽게4: 유클리드 거리 기반, 가중치 추가
                                float weight = adjustedFactor ?? 1.2f; // 기본 가중치
                                weight = Mathf.Max(0.1f, weight); // 최소값 0.1f로 Clamp
                                return Mathf.RoundToInt(weight * Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y)));
                            }

                            case HeuristicType.AdaptiveWeightedHybrid:
                            {
                                // 부드럽게5: 동적 가중치 기반 하이브리드 방식
                                float adaptiveFactor = adjustedFactor ?? 1.0f; // 기본 동적 가중치
                                adaptiveFactor = Mathf.Clamp01(adaptiveFactor); // 안전한 범위로 Clamp
                                float manhattan = Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
                                float euclidean = Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
                                return Mathf.RoundToInt(Mathf.Lerp(manhattan, euclidean, adaptiveFactor));
                            }

                            case HeuristicType.SplineInterpolation:
                            {
                                // 부드럽게6: Spline 보간을 사용한 경로 생성
                                float t = adjustedFactor ?? 0.5f; // 기본 보간 계수
                                t = Mathf.Clamp01(t); // 안전한 범위로 Clamp
                                Vector2 interpolated = Vector2.Lerp(a, b, t);
                                return Mathf.RoundToInt(Vector2.Distance(interpolated, b));
                            }

                            case HeuristicType.Relaxation:
                            {
                                // 부드럽게7: Relaxation 기법으로 꺾임을 부드럽게
                                float relaxationFactor = adjustedFactor ?? 2.0f; // 기본 Relaxation 강도
                                relaxationFactor = Mathf.Max(1.0f, relaxationFactor); // 최소값 1.0f로 Clamp
                                int avgX = (a.x + b.x) / Mathf.RoundToInt(relaxationFactor);
                                int avgY = (a.y + b.y) / Mathf.RoundToInt(relaxationFactor);
                                return Mathf.Abs(avgX - b.x) + Mathf.Abs(avgY - b.y);
                            }

                            default:
                            throw new ArgumentOutOfRangeException(nameof(heuristicType), "Unknown heuristic type");
                        }
                    }



                    private void Hallways_FindPath_ReconstructPath(List<Vector2Int> targetPathList, Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
                    {
                        targetPathList.Clear();
                        targetPathList.Add(current);

                        while (cameFrom.ContainsKey(current))
                        {
                            current = cameFrom[current];
                            targetPathList.Add(current);
                        }
                        targetPathList.Reverse();
                    }


                    internal sealed class IndexedPriorityQueue<T>
                    {
                        private readonly SortedSet<(int priority, int id, T item)> _elements;
                        private readonly Dictionary<T, (int priority, int id, T item)> _indexedElements;
                        private int _idCounter;

                        public IndexedPriorityQueue()
                        {
                            _elements = new SortedSet<(int priority, int id, T item)>(Comparer<(int priority, int id, T item)>.Create((a, b) =>
                            {
                                int priorityComparison = a.priority.CompareTo(b.priority);
                                return priorityComparison != 0 ? priorityComparison : a.id.CompareTo(b.id);
                            }));
                            _indexedElements = new Dictionary<T, (int priority, int id, T item)>();
                            _idCounter = 0;
                        }

                        public int Count => _elements.Count;

                        public bool TryEnqueue(T item, int priority)
                        {
                            if (_indexedElements.ContainsKey(item)) { return false; }

                            var element = (priority, _idCounter++, item);
                            _elements.Add(element);
                            _indexedElements.Add(item, element);
                            return true;
                        }

                        public T Dequeue()
                        {
                            var min = _elements.Min;
                            _elements.Remove(min);
                            _indexedElements.Remove(min.item);
                            return min.item;
                        }

                        public bool Contains(T item)
                        {
                            return _indexedElements.ContainsKey(item);
                        }

                        public void UpdatePriority(T item, int newPriority)
                        {
                            if (_indexedElements.TryGetValue(item, out var existing))
                            {
                                _elements.Remove(existing);
                                var updated = (newPriority, _idCounter++, item);
                                _elements.Add(updated);
                                _indexedElements[item] = updated;
                            }
                        }

                        public void Clear()
                        {
                            _elements.Clear();
                            _indexedElements.Clear();
                            _idCounter = 0;
                        }
                    }
                }



                public readonly Pathfinder Pathfind = new();



                /// <summary>
                /// 휴리스틱 타입에 맞춰 Factor의 최소값~최대값 정보 반환 (에디터 제약에서 사용)
                /// </summary>
                /// <param name="heuristicType"></param>
                /// <param name="min"></param>
                /// <param name="max"></param>
                /// <param name="noLimit_Min"></param>
                /// <param name="noLimit_Max"></param>
                /// <returns></returns>
                /// <exception cref="ArgumentOutOfRangeException"></exception>
                public static bool TryGetHeuristicFactorRange(HeuristicType heuristicType, out float min, out float max, out bool noLimit_Min, out bool noLimit_Max)
                {
                    switch (heuristicType)
                    {
                        case HeuristicType.WeightedDiagonal:
                        case HeuristicType.LerpHybrid:
                        case HeuristicType.AdaptiveWeightedHybrid:
                        case HeuristicType.SplineInterpolation:
                        min = 0.0f;
                        max = 1.0f;
                        noLimit_Min = false;
                        noLimit_Max = false;
                        return true;

                        case HeuristicType.WeightedEuclidean:
                        min = 0.1f;
                        max = float.MaxValue; // 가중치는 상한이 없음
                        noLimit_Min = false;
                        noLimit_Max = true;
                        return true;

                        case HeuristicType.Relaxation:
                        min = 1.0f;
                        max = float.MaxValue; // Relaxation 강도는 상한이 없음
                        noLimit_Min = false;
                        noLimit_Max = true;
                        return true;

                        case HeuristicType.Manhattan:
                        case HeuristicType.Euclidean:
                        // 이 유형은 factor를 사용하지 않음
                        min = 0;
                        max = 0;
                        noLimit_Min = false;
                        noLimit_Max = false;
                        return false;

                        default:
                        throw new ArgumentOutOfRangeException(nameof(heuristicType), "Unknown heuristic type");
                    }
                }



                #region 이전 패스파인더


                //public Pathfinder_Legacy Pathfind_Legacy = new Pathfinder_Legacy();


                [Obsolete]
                public class Pathfinder_Legacy
                {
                    public const int MOVECOST_DEFAULT = 1;

                    public List<Vector2Int> PathFinding(GridManager gridManager, in Vector2Int start, in Vector2Int goal, IReadOnlyCollection<Vector2Int> bannedPositions = null)
                    {
                        var openList = new List<Vector2Int>();
                        var closedList = new HashSet<Vector2Int>();
                        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();

                        var gScore = new Dictionary<Vector2Int, int>();
                        var fScore = new Dictionary<Vector2Int, int>();

                        openList.Add(start);
                        gScore[start] = 0;
                        fScore[start] = Hallways_FindPath_Heuristic(start, goal);

                        while (openList.Count > 0)
                        {
                            var current = Hallways_FindPath_GetLowestFScoreNode(openList, fScore);

                            if (current == goal)
                            {
                                var result = Hallways_FindPath_ReconstructPath(cameFrom, current);
                                return result;
                            }

                            openList.Remove(current);
                            closedList.Add(current);

                            var neighborList = gridManager.GetNeighborsGrids_NotOccupied(current);

                            foreach (var neighbor in neighborList)
                            {
                                if (closedList.Contains(neighbor) || (bannedPositions != null && bannedPositions.Contains(neighbor))) { continue; }

                                int moveCost = MOVECOST_DEFAULT + gridManager.GetGrid(neighbor).PathCost;
                                var tentativeGScore = gScore[current] + moveCost;

                                if (!openList.Contains(neighbor))
                                {
                                    openList.Add(neighbor);
                                }
                                else if (tentativeGScore >= gScore[neighbor])
                                {
                                    continue;
                                }

                                cameFrom[neighbor] = current;
                                gScore[neighbor] = tentativeGScore;
                                fScore[neighbor] = gScore[neighbor] + Hallways_FindPath_Heuristic(neighbor, goal);
                            }
                        }

                        return null;
                    }

                    private int Hallways_FindPath_Heuristic(Vector2Int a, Vector2Int b)
                    {
                        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
                    }

                    // F 스코어가 가장 낮은 노드 반환
                    private Vector2Int Hallways_FindPath_GetLowestFScoreNode(List<Vector2Int> openList, Dictionary<Vector2Int, int> fScore)
                    {
                        Vector2Int lowest = openList[0];

                        foreach (var node in openList)
                        {
                            if (!fScore.ContainsKey(node))
                            {
                                fScore[node] = int.MaxValue;
                            }

                            if (fScore[node] < fScore[lowest])
                            {
                                lowest = node;
                            }
                        }

                        return lowest;
                    }

                    // 경로 재구성
                    private List<Vector2Int> Hallways_FindPath_ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
                    {
                        var totalPath = new List<Vector2Int> { current };
                        while (cameFrom.ContainsKey(current))
                        {
                            current = cameFrom[current];
                            totalPath.Add(current);
                        }
                        totalPath.Reverse();
                        return totalPath;
                    }
                }


                #endregion



                #region 리팩토링 흔적



                //public class BAK_Original_NewRefectoring1
                //{
                //    public const int MOVECOST_DEFAULT = 1;

                //    public List<Vector2Int> PathFinding(GridManager gridManager, in Vector2Int start, in Vector2Int goal, IReadOnlyCollection<Vector2Int> bannedPositions = null)
                //    {
                //        var openList = new PriorityQueue<Vector2Int>();
                //        var closedList = new HashSet<Vector2Int>();
                //        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();

                //        var gScore = new Dictionary<Vector2Int, int>();
                //        var fScore = new Dictionary<Vector2Int, int>();

                //        openList.Enqueue(start, 0);
                //        gScore[start] = 0;
                //        fScore[start] = Hallways_FindPath_Heuristic(start, goal);

                //        while (openList.Count > 0)
                //        {
                //            var current = openList.Dequeue();

                //            if (current == goal)
                //            {
                //                return Hallways_FindPath_ReconstructPath(cameFrom, current);
                //            }

                //            closedList.Add(current);

                //            var neighborList = gridManager.GetNeighborsGridSpan_NotOccupied(current);

                //            foreach (var neighbor in neighborList)
                //            {
                //                if (closedList.Contains(neighbor) || (bannedPositions != null && bannedPositions.Contains(neighbor)))
                //                {
                //                    continue;
                //                }

                //                int moveCost = MOVECOST_DEFAULT + gridManager.GetGrid(neighbor).PathCost;
                //                var tentativeGScore = gScore[current] + moveCost;

                //                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                //                {
                //                    cameFrom[neighbor] = current;
                //                    gScore[neighbor] = tentativeGScore;
                //                    fScore[neighbor] = gScore[neighbor] + Hallways_FindPath_Heuristic(neighbor, goal);

                //                    if (!openList.Contains(neighbor))
                //                    {
                //                        openList.Enqueue(neighbor, fScore[neighbor]);
                //                    }
                //                    else
                //                    {
                //                        openList.UpdatePriority(neighbor, fScore[neighbor]);
                //                    }
                //                }
                //            }
                //        }

                //        return null;
                //    }

                //    private int Hallways_FindPath_Heuristic(Vector2Int a, Vector2Int b)
                //    {
                //        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
                //    }

                //    private List<Vector2Int> Hallways_FindPath_ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
                //    {
                //        var totalPath = new List<Vector2Int> { current };
                //        while (cameFrom.ContainsKey(current))
                //        {
                //            current = cameFrom[current];
                //            totalPath.Add(current);
                //        }
                //        totalPath.Reverse();
                //        return totalPath;
                //    }

                //    // 우선순위 큐 클래스 (내부 클래스)
                //    private class PriorityQueue<T>
                //    {
                //        private readonly SortedDictionary<int, Queue<T>> _elements = new();
                //        private readonly HashSet<T> _set = new();

                //        public int Count { get; private set; } = 0;

                //        public void Enqueue(T item, int priority)
                //        {
                //            if (!_elements.ContainsKey(priority))
                //            {
                //                _elements[priority] = new Queue<T>();
                //            }

                //            _elements[priority].Enqueue(item);
                //            _set.Add(item);
                //            Count++;
                //        }

                //        public T Dequeue()
                //        {
                //            if (Count == 0) throw new InvalidOperationException("Queue is empty");

                //            var firstPair = _elements.First();
                //            var item = firstPair.Value.Dequeue();

                //            if (firstPair.Value.Count == 0)
                //            {
                //                _elements.Remove(firstPair.Key);
                //            }

                //            _set.Remove(item);
                //            Count--;

                //            return item;
                //        }

                //        public bool Contains(T item)
                //        {
                //            return _set.Contains(item);
                //        }

                //        public void UpdatePriority(T item, int newPriority)
                //        {
                //            if (!Contains(item)) return;

                //            // 기존의 item을 제거
                //            foreach (var (priority, queue) in _elements)
                //            {
                //                if (queue.Contains(item))
                //                {
                //                    var newQueue = new Queue<T>(queue.Where(x => !x.Equals(item))); // 새로운 큐 생성
                //                    if (newQueue.Count > 0)
                //                    {
                //                        _elements[priority] = newQueue; // 기존 큐를 대체
                //                    }
                //                    else
                //                    {
                //                        _elements.Remove(priority); // 빈 큐는 제거
                //                    }
                //                    break;
                //                }
                //            }

                //            // 새로운 우선순위로 item 삽입
                //            Enqueue(item, newPriority);
                //        }

                //    }
                //}



                //public class BAK_Original_NewRefectoring2
                //{
                //    public const int MOVECOST_DEFAULT = 1;

                //    public List<Vector2Int> PathFinding(
                //        GridManager gridManager,
                //        in Vector2Int start,
                //        in Vector2Int goal,
                //        bool useSmoothPath = false,
                //        IReadOnlyCollection<Vector2Int> bannedPositions = null)
                //    {
                //        int gridWidth = gridManager.GridArrayLengthX;
                //        int gridHeight = gridManager.GridArrayLengthY;

                //        var openList = new PriorityQueue<Vector2Int>();
                //        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();

                //        var gScore = new int[gridWidth * gridHeight];
                //        var fScore = new int[gridWidth * gridHeight];
                //        var closedList = new BitArray(gridWidth * gridHeight);

                //        Array.Fill(gScore, int.MaxValue);
                //        Array.Fill(fScore, int.MaxValue);

                //        int startIndex = GetIndex(start, gridWidth);
                //        gScore[startIndex] = 0;
                //        fScore[startIndex] = Hallways_FindPath_Heuristic(start, goal, useSmoothPath);
                //        openList.Enqueue(start, fScore[startIndex]);

                //        while (openList.Count > 0)
                //        {
                //            var current = openList.Dequeue();
                //            int currentIndex = GetIndex(current, gridWidth);

                //            if (current == goal)
                //            {
                //                return Hallways_FindPath_ReconstructPath(cameFrom, current);
                //            }

                //            closedList[currentIndex] = true;

                //            var neighborList = gridManager.GetNeighborsGridSpan_NotOccupied(current);

                //            foreach (var neighbor in neighborList)
                //            {
                //                int neighborIndex = GetIndex(neighbor, gridWidth);

                //                if (closedList[neighborIndex] || (bannedPositions != null && bannedPositions.Contains(neighbor)))
                //                {
                //                    continue;
                //                }

                //                int moveCost = MOVECOST_DEFAULT + gridManager.GetGrid(neighbor).PathCost;
                //                int tentativeGScore = gScore[currentIndex] + moveCost;

                //                if (tentativeGScore < gScore[neighborIndex])
                //                {
                //                    cameFrom[neighbor] = current;
                //                    gScore[neighborIndex] = tentativeGScore;
                //                    fScore[neighborIndex] = gScore[neighborIndex] + Hallways_FindPath_Heuristic(neighbor, goal, useSmoothPath);

                //                    if (!openList.Contains(neighbor))
                //                    {
                //                        openList.Enqueue(neighbor, fScore[neighborIndex]);
                //                    }
                //                    else
                //                    {
                //                        openList.UpdatePriority(neighbor, fScore[neighborIndex]);
                //                    }
                //                }
                //            }
                //        }

                //        return null;
                //    }


                //    private int Hallways_FindPath_Heuristic(Vector2Int a, Vector2Int b, bool useSmoothPath)
                //    {
                //        if (useSmoothPath)
                //        {
                //            // 유클리드 거리
                //            return Mathf.RoundToInt(Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y)));
                //        }
                //        else
                //        {
                //            // 맨해튼 거리
                //            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
                //        }
                //    }

                //    private List<Vector2Int> Hallways_FindPath_ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
                //    {
                //        var totalPath = new List<Vector2Int> { current };
                //        while (cameFrom.ContainsKey(current))
                //        {
                //            current = cameFrom[current];
                //            totalPath.Add(current);
                //        }
                //        totalPath.Reverse();
                //        return totalPath;
                //    }

                //    private int GetIndex(Vector2Int position, int gridWidth)
                //    {
                //        return position.x + position.y * gridWidth;
                //    }

                //    // 우선순위 큐 클래스
                //    private class PriorityQueue<T>
                //    {
                //        private readonly SortedSet<(int priority, int id, T item)> _elements;
                //        private readonly HashSet<T> _set;
                //        private int _idCounter; // 고유 ID 생성용

                //        public PriorityQueue()
                //        {
                //            _elements = new SortedSet<(int priority, int id, T item)>(Comparer<(int priority, int id, T item)>.Create((a, b) =>
                //            {
                //                int priorityComparison = a.priority.CompareTo(b.priority);
                //                if (priorityComparison != 0)
                //                    return priorityComparison;

                //                // 우선순위가 같으면 ID로 정렬
                //                return a.id.CompareTo(b.id);
                //            }));

                //            _set = new HashSet<T>();
                //            _idCounter = 0;
                //        }

                //        public int Count => _elements.Count;

                //        public void Enqueue(T item, int priority)
                //        {
                //            if (_set.Contains(item)) return; // 중복 방지
                //            _elements.Add((priority, _idCounter++, item));
                //            _set.Add(item);
                //        }

                //        public T Dequeue()
                //        {
                //            if (_elements.Count == 0) throw new InvalidOperationException("Queue is empty");

                //            var min = _elements.Min;
                //            _elements.Remove(min);
                //            _set.Remove(min.item);

                //            return min.item;
                //        }

                //        public bool Contains(T item)
                //        {
                //            return _set.Contains(item);
                //        }

                //        public void UpdatePriority(T item, int newPriority)
                //        {
                //            if (!Contains(item)) return;

                //            // 기존 항목 삭제
                //            var existing = _elements.FirstOrDefault(e => e.item.Equals(item));
                //            if (!existing.Equals(default))
                //                _elements.Remove(existing);

                //            // 새로운 우선순위로 삽입
                //            Enqueue(item, newPriority);
                //        }
                //    }
                //}



                //public class BAK_Original_NewRefectoring3
                //{
                //    public const int MOVECOST_DEFAULT = 1;


                //    public List<Vector2Int> PathFinding(
                //        GridManager gridManager,
                //        in Vector2Int start,
                //        in Vector2Int goal,
                //        bool useSmoothPath = false,
                //        IReadOnlyCollection<Vector2Int> bannedPositions = null)
                //    {
                //        int gridWidth = gridManager.GridArrayLengthX;
                //        int gridHeight = gridManager.GridArrayLengthY;

                //        var openList = new PriorityQueue<Vector2Int>();
                //        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();

                //        var gScore = new int[gridWidth * gridHeight];
                //        var fScore = new int[gridWidth * gridHeight];
                //        var closedList = new BitArray(gridWidth * gridHeight);

                //        Array.Fill(gScore, int.MaxValue);
                //        Array.Fill(fScore, int.MaxValue);

                //        int startIndex = GetIndex(start, gridWidth);
                //        gScore[startIndex] = 0;
                //        fScore[startIndex] = Hallways_FindPath_Heuristic(start, goal, useSmoothPath);
                //        openList.Enqueue(start, fScore[startIndex]);

                //        while (openList.Count > 0)
                //        {
                //            var current = openList.Dequeue();
                //            int currentIndex = GetIndex(current, gridWidth);

                //            if (current == goal)
                //            {
                //                return Hallways_FindPath_ReconstructPath(cameFrom, current);
                //            }

                //            closedList[currentIndex] = true;

                //            ReadOnlySpan<Vector2Int> neighborList = gridManager.GetNeighborsGridSpan_NotOccupied(current);

                //            foreach (var neighbor in neighborList)
                //            {
                //                int neighborIndex = GetIndex(neighbor, gridWidth);

                //                if (closedList[neighborIndex] || (bannedPositions != null && bannedPositions.Contains(neighbor)))
                //                {
                //                    continue;
                //                }

                //                int moveCost = MOVECOST_DEFAULT + gridManager.GetGrid(neighbor).PathCost;
                //                int tentativeGScore = gScore[currentIndex] + moveCost;

                //                if (tentativeGScore < gScore[neighborIndex])
                //                {
                //                    cameFrom[neighbor] = current;
                //                    gScore[neighborIndex] = tentativeGScore;

                //                    fScore[neighborIndex] = gScore[neighborIndex]
                //                        + Hallways_FindPath_Heuristic(neighbor, goal, useSmoothPath);

                //                    if (!openList.Contains(neighbor))
                //                    {
                //                        openList.Enqueue(neighbor, fScore[neighborIndex]);
                //                    }
                //                    else
                //                    {
                //                        openList.UpdatePriority(neighbor, fScore[neighborIndex]);
                //                    }
                //                }
                //            }
                //        }

                //        return null;
                //    }

                //    private int Hallways_FindPath_Heuristic(Vector2Int a, Vector2Int b, bool useSmoothPath)
                //    {
                //        if (useSmoothPath)
                //        {
                //            return Mathf.RoundToInt(Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y)));
                //        }
                //        else
                //        {
                //            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
                //        }
                //    }

                //    private List<Vector2Int> Hallways_FindPath_ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
                //    {
                //        var totalPath = new List<Vector2Int> { current };
                //        while (cameFrom.ContainsKey(current))
                //        {
                //            current = cameFrom[current];
                //            totalPath.Add(current);
                //        }
                //        totalPath.Reverse();
                //        return totalPath;
                //    }

                //    private int GetIndex(Vector2Int position, int gridWidth)
                //    {
                //        return position.x + position.y * gridWidth;
                //    }

                //    private class PriorityQueue<T>
                //    {
                //        private readonly SortedSet<(int priority, int id, T item)> _elements;
                //        private readonly HashSet<T> _set;
                //        private int _idCounter;

                //        public PriorityQueue()
                //        {
                //            _elements = new SortedSet<(int priority, int id, T item)>(Comparer<(int priority, int id, T item)>.Create((a, b) =>
                //            {
                //                int priorityComparison = a.priority.CompareTo(b.priority);
                //                return priorityComparison != 0 ? priorityComparison : a.id.CompareTo(b.id);
                //            }));
                //            _set = new HashSet<T>();
                //            _idCounter = 0;
                //        }

                //        public int Count => _elements.Count;

                //        public void Enqueue(T item, int priority)
                //        {
                //            if (_set.Contains(item)) return;
                //            _elements.Add((priority, _idCounter++, item));
                //            _set.Add(item);
                //        }

                //        public T Dequeue()
                //        {
                //            var min = _elements.Min;
                //            _elements.Remove(min);
                //            _set.Remove(min.item);
                //            return min.item;
                //        }

                //        public bool Contains(T item)
                //        {
                //            return _set.Contains(item);
                //        }

                //        public void UpdatePriority(T item, int newPriority)
                //        {
                //            var existing = _elements.FirstOrDefault(e => e.item.Equals(item));
                //            if (existing.Equals(default)) return;

                //            _elements.Remove(existing);
                //            Enqueue(item, newPriority);
                //        }
                //    }
                //}



                //public class BAK_Original_NewRefectoring4
                //{
                //    public const int MOVECOST_DEFAULT = 1;

                //    public List<Vector2Int> PathFinding(
                //        GridManager gridManager,
                //        in Vector2Int start,
                //        in Vector2Int goal,
                //        bool useSmoothPath = false,
                //        IReadOnlyCollection<Vector2Int> bannedPositions = null,
                //        Vector2Int? bottomLeft = null,
                //        Vector2Int? topRight = null)
                //    {
                //        int gridWidth = gridManager.GridArrayLengthX;
                //        int gridHeight = gridManager.GridArrayLengthY;

                //        var openList = new PriorityQueue<Vector2Int>();
                //        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();

                //        var gScore = new int[gridWidth * gridHeight];
                //        var fScore = new int[gridWidth * gridHeight];
                //        var closedList = new BitArray(gridWidth * gridHeight);

                //        Array.Fill(gScore, int.MaxValue);
                //        Array.Fill(fScore, int.MaxValue);

                //        int startIndex = GetIndex(start, gridWidth);
                //        gScore[startIndex] = 0;
                //        fScore[startIndex] = Hallways_FindPath_Heuristic(start, goal, useSmoothPath);
                //        openList.Enqueue(start, fScore[startIndex]);

                //        while (openList.Count > 0)
                //        {
                //            var current = openList.Dequeue();
                //            int currentIndex = GetIndex(current, gridWidth);

                //            if (current == goal)
                //            {
                //                return Hallways_FindPath_ReconstructPath(cameFrom, current);
                //            }

                //            closedList[currentIndex] = true;

                //            ReadOnlySpan<Vector2Int> neighborList = gridManager.GetNeighborsGridSpan_NotOccupied(current);

                //            foreach (var neighbor in neighborList)
                //            {
                //                int neighborIndex = GetIndex(neighbor, gridWidth);

                //                // 탐색 범위 제한 조건 추가
                //                if (bottomLeft.HasValue && topRight.HasValue)
                //                {
                //                    if (neighbor.x < bottomLeft.Value.x || neighbor.x > topRight.Value.x ||
                //                        neighbor.y < bottomLeft.Value.y || neighbor.y > topRight.Value.y)
                //                    {
                //                        continue;
                //                    }
                //                }

                //                if (closedList[neighborIndex] || (bannedPositions != null && bannedPositions.Contains(neighbor)))
                //                {
                //                    continue;
                //                }

                //                int moveCost = MOVECOST_DEFAULT + gridManager.GetGrid(neighbor).PathCost;
                //                int tentativeGScore = gScore[currentIndex] + moveCost;

                //                if (tentativeGScore < gScore[neighborIndex])
                //                {
                //                    cameFrom[neighbor] = current;
                //                    gScore[neighborIndex] = tentativeGScore;

                //                    fScore[neighborIndex] = gScore[neighborIndex]
                //                        + Hallways_FindPath_Heuristic(neighbor, goal, useSmoothPath);

                //                    if (!openList.Contains(neighbor))
                //                    {
                //                        openList.Enqueue(neighbor, fScore[neighborIndex]);
                //                    }
                //                    else
                //                    {
                //                        openList.UpdatePriority(neighbor, fScore[neighborIndex]);
                //                    }
                //                }
                //            }
                //        }

                //        return null;
                //    }

                //    private int Hallways_FindPath_Heuristic(Vector2Int a, Vector2Int b, bool useSmoothPath)
                //    {
                //        if (useSmoothPath)
                //        {
                //            return Mathf.RoundToInt(Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y)));
                //        }
                //        else
                //        {
                //            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
                //        }
                //    }

                //    private List<Vector2Int> Hallways_FindPath_ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
                //    {
                //        var totalPath = new List<Vector2Int> { current };
                //        while (cameFrom.ContainsKey(current))
                //        {
                //            current = cameFrom[current];
                //            totalPath.Add(current);
                //        }
                //        totalPath.Reverse();
                //        return totalPath;
                //    }

                //    private int GetIndex(Vector2Int position, int gridWidth)
                //    {
                //        return position.x + position.y * gridWidth;
                //    }

                //    private class PriorityQueue<T>
                //    {
                //        private readonly SortedSet<(int priority, int id, T item)> _elements;
                //        private readonly HashSet<T> _set;
                //        private int _idCounter;

                //        public PriorityQueue()
                //        {
                //            _elements = new SortedSet<(int priority, int id, T item)>(Comparer<(int priority, int id, T item)>.Create((a, b) =>
                //            {
                //                int priorityComparison = a.priority.CompareTo(b.priority);
                //                return priorityComparison != 0 ? priorityComparison : a.id.CompareTo(b.id);
                //            }));
                //            _set = new HashSet<T>();
                //            _idCounter = 0;
                //        }

                //        public int Count => _elements.Count;

                //        public void Enqueue(T item, int priority)
                //        {
                //            if (_set.Contains(item)) return;
                //            _elements.Add((priority, _idCounter++, item));
                //            _set.Add(item);
                //        }

                //        public T Dequeue()
                //        {
                //            var min = _elements.Min;
                //            _elements.Remove(min);
                //            _set.Remove(min.item);
                //            return min.item;
                //        }

                //        public bool Contains(T item)
                //        {
                //            return _set.Contains(item);
                //        }

                //        public void UpdatePriority(T item, int newPriority)
                //        {
                //            var existing = _elements.FirstOrDefault(e => e.item.Equals(item));
                //            if (existing.Equals(default)) return;

                //            _elements.Remove(existing);
                //            Enqueue(item, newPriority);
                //        }

                //        public void Clear()
                //        {
                //            _elements.Clear();
                //            _set.Clear();
                //            _idCounter = 0;
                //        }
                //    }
                //}



                //public class BAK_Original_NewRefectoring5
                //{
                //    public const int MOVECOST_DEFAULT = 1;

                //    public List<Vector2Int> PathFinding(
                //        GridManager gridManager,
                //        in Vector2Int start,
                //        in Vector2Int goal,
                //        bool useSmoothPath = false,
                //        IReadOnlyCollection<Vector2Int> bannedPositions = null,
                //        Vector2Int? bottomLeft = null,
                //        Vector2Int? topRight = null)
                //    {
                //        var openList = new PriorityQueue<Vector2Int>();
                //        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();

                //        // Dictionary를 사용해 탐색된 노드만 관리
                //        var gScore = new Dictionary<Vector2Int, int>();
                //        var fScore = new Dictionary<Vector2Int, int>();
                //        var closedList = new HashSet<Vector2Int>();

                //        gScore[start] = 0;
                //        fScore[start] = Hallways_FindPath_Heuristic(start, goal, useSmoothPath);
                //        openList.Enqueue(start, fScore[start]);

                //        while (openList.Count > 0)
                //        {
                //            var current = openList.Dequeue();

                //            if (current == goal)
                //            {
                //                return Hallways_FindPath_ReconstructPath(cameFrom, current);
                //            }

                //            closedList.Add(current);

                //            ReadOnlySpan<Vector2Int> neighborList = gridManager.GetNeighborsGridSpan_NotOccupied(current);

                //            foreach (var neighbor in neighborList)
                //            {
                //                if (closedList.Contains(neighbor) || (bannedPositions != null && bannedPositions.Contains(neighbor)))
                //                {
                //                    continue;
                //                }

                //                // 탐색 범위 제한 조건 추가
                //                if (bottomLeft.HasValue && topRight.HasValue)
                //                {
                //                    if (neighbor.x < bottomLeft.Value.x || neighbor.x > topRight.Value.x ||
                //                        neighbor.y < bottomLeft.Value.y || neighbor.y > topRight.Value.y)
                //                    {
                //                        continue;
                //                    }
                //                }

                //                int tentativeGScore = gScore[current] + MOVECOST_DEFAULT + gridManager.GetGrid(neighbor).PathCost;

                //                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                //                {
                //                    cameFrom[neighbor] = current;
                //                    gScore[neighbor] = tentativeGScore;

                //                    int heuristic = Hallways_FindPath_Heuristic(neighbor, goal, useSmoothPath);
                //                    fScore[neighbor] = gScore[neighbor] + heuristic;

                //                    if (!openList.Contains(neighbor))
                //                    {
                //                        openList.Enqueue(neighbor, fScore[neighbor]);
                //                    }
                //                    else
                //                    {
                //                        openList.UpdatePriority(neighbor, fScore[neighbor]);
                //                    }
                //                }
                //            }
                //        }

                //        return null;
                //    }

                //    private int Hallways_FindPath_Heuristic(Vector2Int a, Vector2Int b, bool useSmoothPath)
                //    {
                //        if (useSmoothPath)
                //        {
                //            return Mathf.RoundToInt(Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y)));
                //        }
                //        else
                //        {
                //            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
                //        }
                //    }

                //    private List<Vector2Int> Hallways_FindPath_ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
                //    {
                //        var totalPath = new List<Vector2Int> { current };
                //        while (cameFrom.ContainsKey(current))
                //        {
                //            current = cameFrom[current];
                //            totalPath.Add(current);
                //        }
                //        totalPath.Reverse();
                //        return totalPath;
                //    }

                //    private class PriorityQueue<T>
                //    {
                //        private readonly SortedSet<(int priority, int id, T item)> _elements;
                //        private int _idCounter;

                //        public PriorityQueue()
                //        {
                //            _elements = new SortedSet<(int priority, int id, T item)>(Comparer<(int priority, int id, T item)>.Create((a, b) =>
                //            {
                //                int priorityComparison = a.priority.CompareTo(b.priority);
                //                return priorityComparison != 0 ? priorityComparison : a.id.CompareTo(b.id);
                //            }));
                //            _idCounter = 0;
                //        }

                //        public int Count => _elements.Count;

                //        public void Enqueue(T item, int priority)
                //        {
                //            _elements.Add((priority, _idCounter++, item));
                //        }

                //        public T Dequeue()
                //        {
                //            var min = _elements.Min;
                //            _elements.Remove(min);
                //            return min.item;
                //        }

                //        public bool Contains(T item)
                //        {
                //            return _elements.Any(e => e.item.Equals(item));
                //        }

                //        public void UpdatePriority(T item, int newPriority)
                //        {
                //            var existing = _elements.FirstOrDefault(e => e.item.Equals(item));
                //            if (!existing.Equals(default))
                //            {
                //                _elements.Remove(existing);
                //                Enqueue(item, newPriority);
                //            }
                //        }
                //    }
                //}



                #endregion



                ///======================================================================================================================================================



                //? 경로 확장 좌표 검사



                /////<summary>
                /////경로확장좌표 검사 반복문에서 사용할, 매 순회마다 확장 좌표들을 담는 리스트 (매번 Clear하며 재활용)
                ///// </summary>
                //private readonly List<Vector2Int> recycleExpandPathList = new List<Vector2Int>(); //! 스테이지 생성이 끝나면 null시켜버려도 되지않을까



                ///<summary>
                ///<paramref name="pathList"/>의 좌표들을 중심으로 확장하여,<br/>
                ///확장된 좌표를 검사하고,<br/>
                ///결과에 따라 중심 좌표를 <paramref name="bannedMainPathList"/>에 추가한다<br/>
                ///</summary>
                ///<returns>
                ///확장된 좌표들이 1개라도 점유상태라면 fasle를 반환하고<br/>
                ///확장된 좌표들이 모두 미점유 상태라면 true를 반환한다
                /// </returns>
                private bool CheckPathList_ExpandWidth_Conditions(GridManager gridManager, List<Vector2Int> pathList, int width, int height, int safeExpand, bool expandPositiveHorizontal, bool expandPositiveVertical,
                    in List<Vector2Int> bannedMainPathList, in IReadOnlyCollection<Vector2Int> integrated_BannedMainPathList, in IReadOnlyCollection<Grid> IgnoreWhenExpandGrids_A = null, in IReadOnlyCollection<Grid> IgnoreWhenExpandGrids_B = null)
                {
                    using var pooled_RecycleExpandPathList = ListPool<Vector2Int>.Get(out var recycleExpandPathList);


                    for (int i = 0; i < pathList.Count; i++)
                    {
                        Vector2Int currentPath = pathList[i];
                        Grid currentGrid = gridManager.GetGrid(currentPath);
                        bool isTurnning = pathList.IsPathTurning(i);
                        var currentWidth = width;
                        var currentHeight = height;


                        // 경로 리스트 확장 파라미터 연산
                        GridManager.Calculate_ForPathList(pathList, i, true, currentWidth, currentHeight, expandPositiveHorizontal, expandPositiveVertical, out int currentExpand_Width, out int currentExpand_Height, out bool currentExpand_expandPositiveHorizontal, out bool currentExpand_expandPositiveVertical);


                        #region Legacy 복도안전구역 크기만큼 보정?
                        ////? 복도 안전구역을 사용중이라면,
                        ////? 복도 안전구역끼리는 겹쳐도 되기때문에
                        ////? 다른 복도 안전구역만 의존시킨다, 확장 크기를 안전구역 크기만큼 줄인다
                        //if (isTurnning || gridManager.Setting.UseHallwaySafeArea)
                        //{
                        //int hallwaySafeLength = gridManager.Setting.HallwaySafeAreaLength * 2;
                        //currentExpand_Width = Mathf.Max(1, currentExpand_Width - hallwaySafeLength);
                        //currentExpand_Height = Mathf.Max(1, currentExpand_Height - hallwaySafeLength);
                        //}
                        #endregion


                        //? 연산된 너비/높이/수평,수직 양수확장 등으로, 현재 좌표의 확장 좌표들을 expandList에 추가한다
                        recycleExpandPathList.Clear();
                        GetExpandPositions(currentPath, currentExpand_Width, currentExpand_Height, currentExpand_expandPositiveHorizontal, currentExpand_expandPositiveVertical, in recycleExpandPathList, out int startX, out int endX, out int startY, out int endY);


                        //? expandList를 순회하면서,
                        //? 유효하지 않는 좌표(배열을 벗어난) 거나,
                        //? 점유된 좌표들을 bannedMainPathList에 추가한다 (확장된 좌표를 추가하는것이 아닌, 중심 좌표를 추가함)
                        foreach (var currentExpandPosition in recycleExpandPathList)
                        {
                            //? 자기 자신이거나,
                            //? 이미 금지 리스트에 추가된 좌표라면 스킵 (최적화)
                            if (currentExpandPosition == currentPath || integrated_BannedMainPathList.Contains(currentExpandPosition)) { continue; }


                            //. 확장된 좌표가 유효한지 확인한다
                            bool vailed_curentGridPosition = gridManager.TryGet_GridPosition(currentExpandPosition, out var currentExpandGrid);


                            //! #1 확장된 좌표가 유효하지 않다면,
                            //! 무조건 금지 리스트에 추가한다
                            if (!vailed_curentGridPosition)
                            {
                                bannedMainPathList.Add(currentPath);
                                continue;
                            }


                            //? #1.5 유효하지만 확장 금지 무시를 사용중이고, 해당될경우,
                            //? 그냥 스킵한다
                            if ((IgnoreWhenExpandGrids_A != null && IgnoreWhenExpandGrids_A.Contains(currentExpandGrid)) ||
                                (IgnoreWhenExpandGrids_B != null && IgnoreWhenExpandGrids_B.Contains(currentExpandGrid))) { continue; }



                            ////? 복도 안전구역을 구분하기위한 테두리 밴드 코드
                            //bool inEdgeBand = false;
                            //if (safeExpand > 0)
                            //{
                            //    //. 안전하게 비교: 내부 코어 경계
                            //    int innerXMin = startX + safeExpand;
                            //    int innerXMax = endX - safeExpand;
                            //    int innerYMin = startY + safeExpand;
                            //    int innerYMax = endY - safeExpand;

                            //    //. currentExpandPosition 이 "코어 내부"가 아니면 = 가장자리 띠에 해당
                            //    //. (safeExpand가 커서 inner 경계가 뒤집히면, 전체가 띠로 간주됨)
                            //    inEdgeBand =
                            //        (currentExpandPosition.x < innerXMin) ||
                            //        (currentExpandPosition.x > innerXMax) ||
                            //        (currentExpandPosition.y < innerYMin) ||
                            //        (currentExpandPosition.y > innerYMax);
                            //}


                            //! #2 확장구역이 유효하면서 "점유"
                            //!    단, edge band && "임시 제어 그리드" 라면 허용(continue)
                            if (currentExpandGrid.IsOccupied)
                            {
                                //x . "복도 안전구역은 임시 점유 상태를 무시할 수 있다" —> 가장자리 띠에서만!
                                //x !     +방, 복도,도어 영역  등의 태그가 없을경우
                                //if (inEdgeBand && currentExpandGrid.ContainsAny(GridTag.TempControl_byConnectHallway) &&
                                //    (!currentExpandGrid.ContainsAny(GridTag.Room_Safe | GridTag.Room_ExpandSafe | GridTag.Room_Door_Area) || (currentGrid.ContainsAny(GridTag.Room_Door_Area) && !currentExpandGrid.ContainsAny(GridTag.Room_Door_Area))))
                                //if (inEdgeBand && currentExpandGrid.ContainsAny(GridTag.TempControl_byConnectHallway))
                                //{
                                //    //? 안전구역 띠에서만 임시점유 무시
                                //    continue;
                                //}

                                //! 그 외 점유는 코어/비코어 불문 금지
                                bannedMainPathList.Add(currentPath);
                                continue;
                            }


                            #region 그냥 점유면 무조검 금지하던 코드
                            ////! #2 확정구역이 유효하면서 "점유"
                            ////!     +"임시 제어 그리드"가 아니며
                            ////! 금지 리스트에 추가한다

                            //if (currentExpandGrid.IsOccupied)
                            //{
                            //    #region 도어의 시작부분이 막힌다면, 이곳이 원인. 조건에 도어가 아닐경우에만 금지 리스트에 추가하도록 하면 해결가능
                            //    //if(!currentExpandGrid.CheckOneTags(GridManager.TAG_ROOM_DOOR_AREA_STARTEND)) 
                            //    #endregion

                            //    bannedMainPathList.Add(currentPath);
                            //    continue;
                            //} 
                            #endregion


                            #region Legacy 미점유 조건

                            //! #3 확장된 좌표가 유효하면서 "미점유"
                            //! 방 테두리이고, 방 도어가 아닐경우
                            //! 금지 리스트에 추가한다

                            //else if (currentExpandGrid.CheckOneTags(GridManager.TAG_ROOM_EDGE) && !currentExpandGrid.CheckOneTags(GridManager.TAG_ROOM_DOOR_AREA))
                            //{
                            //    bannedMainPathList.Add(currentPath);
                            //    continue;
                            //} 

                            #endregion
                        }
                    }


                    return bannedMainPathList.Count == 0;
                }



                ///<summary>
                ///해당 좌표를 기준으로 확장된 좌표들을 가져온다 (<paramref name="current"/>는 제외, 메인경로는 반환 되지 않음!)
                ///</summary>
                private void GetExpandPositions(Vector2Int current, int width, int height, bool expandPositiveHorizontal, bool expandPositiveVertical, in List<Vector2Int> expandList, out int startX, out int endX, out int startY, out int endY)
                {
                    // current를 중심으로 확장된 좌표들을 반복문으로 돌리기위해, 값들을 얻어온다
                    GridManager.GetExpandGridPositionsFromGridPosition_ForLoop(current, width, height, expandPositiveHorizontal, expandPositiveVertical, out startX, out endX, out startY, out endY);


                    for (int x = startX; x <= endX; x++)
                    {
                        for (int y = startY; y <= endY; y++)
                        {
                            //! 메인 경로는 포함하지 않는다, 오직 확장된 경로만
                            if (x == current.x && y == current.y) { continue; }
                            expandList.Add(new Vector2Int(x, y));
                        }
                    }
                }



                ///======================================================================================================================================================



                //? 레거시 디버깅



                #region 레거시 디버깅

                // 패스파인딩 경로 너비 확장 재연산 횟수 초과
                private static void ErrorMsg_Overed_PathFinding_MaxReCalculateLoopCount(int count)
                {
                    Debug.LogError($"패스파인딩 경로 너비 확장 연산을 {count}번 감지, 무한루프로 간주하여 생성 중단");
                }



                // 패스파인딩 경로 찾기 실패
                private static void ErrorMsg_PathFinding_Failed(in Vector2Int start, in Vector2Int end, int pathWidth, bool expandPositiveHorizontal, bool expandPositiveVertical)
                {
                    StringBuilder sb = new StringBuilder();

                    sb.AppendLine($"<color=red><b>패스파인딩 실패</b></color>");
                    sb.AppendLine($"경로: <b><color=#ed5565>{start}</color></b> ~ <b><color=#ed5565>{end}</color></b>");
                    sb.AppendLine($"경로 너비: <b><color=#ed5565>{pathWidth}</color></b>");
                    sb.AppendLine($"경로 확장 Positive 수평: <b><color=#ed5565>{expandPositiveHorizontal}</color></b>");
                    sb.AppendLine($"경로 확장 Positive 수직: <b><color=#ed5565>{expandPositiveVertical}</color></b>");


                    Debug.LogError(sb.ToString(false));
                    //Debug.LogError($"패스파인딩 경로 찾기 실패\n경로: <b><color=#ed5565>{start}</color></b> ~ <b><color=#ed5565>{end}</color></b>\n경로 너비: <b><color=#ed5565>{pathWidth}</color></b>\n경로 확장 Positive 수평/수직: <b><color=#ed5565>{expandPositiveHorizontal}</color></b>, <b><color=#ed5565>{expandPositiveVertical}</color></b>");
                }

                #endregion



                ///======================================================================================================================================================
            }



            ///======================================================================================================================================================
        }
    }
}
