using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Pan.Util;
using System;
using System.Text;
using Cysharp.Threading.Tasks;
using System.Collections;
using Pan.GridCompatibles2;
using Pan.StageGenerators;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using System.Runtime.CompilerServices;
using Sirenix.Serialization;
using UnityEngine.Pool;
using System.Buffers;



namespace Pan.StageGenerators
{
    public partial class StageGenerator
    {
        [Flags]
        public enum GridTag : uint   // ← 32-bit로 변경
        {
            None = 0,

            //. 금지/충돌
            WasBanned_MainPath = 1U << 0,
            WasBanned_CollisionOtherHallway = 1U << 1,

            //. 방(Room)
            Room = 1U << 2,
            Room_Safe = 1U << 3,
            Room_Edge = 1U << 4,
            Room_ExpandSafe = 1U << 5,
            Room_Safe_Down = 1U << 6,
            Room_Safe_Up = 1U << 7,
            Room_Safe_Left = 1U << 8,
            Room_Safe_Right = 1U << 9,
            Room_Safe_Mini = 1U << 10,

            //. 문(Door)
            Room_Door = 1U << 11,
            Room_Door_Start = 1U << 12,
            Room_Door_BothEnd_Min = 1U << 13,
            Room_Door_BothEnd_Max = 1U << 14,
            Room_Door_End = 1U << 15,
            Room_Door_Area_StartEnd = 1U << 16,
            Room_Door_Area = 1U << 17,
            Room_Door_Area_Expand = 1U << 18,

            //. 복도(Hallway)
            Hallway = 1U << 19,
            Hallway_Path_Main = 1U << 20,
            Hallway_Path_Expand = 1U << 21,
            Hallway_Edge = 1U << 22,
            Hallway_Edge_DoorCorrection = 1U << 23,
            Hallway_Safe_DoorCorrection = 1U << 24,
            Hallway_Safe = 1U << 25,

            //. 임시(Temp)
            TempControl_byConnectHallway = 1U << 26,
            TempTest = 1U << 27
        }



        public const GridTag GRIDTAGS_Hallways = GridTag.Hallway | GridTag.Hallway_Path_Main | GridTag.Hallway_Path_Expand | GridTag.Hallway_Edge | GridTag.Hallway_Safe;
        public const GridTag GRIDTAGS_HallwayCorrections = GridTag.Hallway | GridTag.Hallway_Path_Main | GridTag.Hallway_Path_Expand | GridTag.Hallway_Edge_DoorCorrection | GridTag.Hallway_Safe_DoorCorrection;



        /// <summary>
        /// <see cref="Grid"/>를 2차원 배열로 관리하는 매니저
        /// </summary>
        [Serializable]
        public class GridManager : BaseManager
        {
            ///======================================================================================================================================================



            //? Grid 2차원 배열



#if UNITY_EDITOR

            [TitleGroup("그리드 매니저"), BoxGroup("그리드 매니저/박스", false)]
            [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Center, EnableRichText = true, Overflow = false), EnableGUI]
            [PropertyOrder(-99)]
            private string dummyTitle_GridsInfo
            {
                get
                {
                    if (Grids != null && Grids.IsValid)
                    {
                        return $"<color=white><size=13>그리드 배열 크기: <color=#2ecc71><b>{GridArrayTotalLength}</b></color> [<color=#f7da64><b>{GridArrayCountX}</b></color> x <color=#f7da64><b>{GridArrayCountY}</b></color>]</size></color>";
                    }
                    else
                    {
                        return $"<color=#ed5565><size=12>그리드 배열이 유효하지 않음(생성되지않음)</size></color>";
                    }
                }
            }

#endif



            ///<summary>
            ///그리드 2차원 배열
            /// </summary>
            [TitleGroup("그리드 매니저"), BoxGroup("그리드 매니저/박스", false)]
            //[SerializeField]
            [LabelText("그리드 배열")]
            private LinearGrid2D<Grid> Grids;



            /// <summary>
            /// 그리드 배열 얻기
            /// </summary>
            public Grid[] GetGrids => Grids != null && Grids.IsValid ? Grids.GetCells : null;



            /// <summary>
            /// 그리드 배열의 유효성
            ///</summary>
            public bool IsValid_GridArray => Grids != null && Grids.IsValid && Grids.Length > 0;

            /// <summary>
            /// 그리드 배열 총 크기
            /// </summary>
            public int GridArrayTotalLength => Grids.Length;

            /// <summary>
            /// 그리드 배열 너비(X) 크기
            ///</summary>
            public int GridArrayCountX => Grids.Width;

            /// <summary>
            /// 그리드 배열 높이(Y) 크기
            ///</summary>
            public int GridArrayCountY => Grids.Height;



            ///======================================================================================================================================================



            //? CONST 기본 상수 설정



            public const int PATHCOST_DEFAULT = 100;



            #region Legacy (CONST 그리드 기본 태그)
            ////? CONST 그리드 기본 태그



            //public const string TAG_WASBANNED_MAINPATH = "WASBANNED_MAINPATH";
            //public const string TAG_WASBANNED_COLLISIONOTHERHALLWAYS = "WASBANNED_COLLISIONOTHERHALLWAYS ";

            //public const string TAG_ROOM = "ROOM";
            //public const string TAG_ROOM_SAFE = "ROOM_SAFE";
            //public const string TAG_ROOM_EDGE = "ROOM_EDGE";
            //public const string TAG_ROOM_EXPANDSAFE = "ROOM_EXPANDSAFE";
            //public const string TAG_ROOM_SAFE_DOWN = "ROOM_SAFE_DOWN";
            //public const string TAG_ROOM_SAFE_UP = "ROOM_SAFE_UP";
            //public const string TAG_ROOM_SAFE_LEFT = "ROOM_SAFE_LEFT";
            //public const string TAG_ROOM_SAFE_RIGHT = "ROOM_SAFE_RIGHT";

            //public const string TAG_ROOM_SAFE_MINI = "TAG_ROOM_SAFE_MINI";


            //public const string TAG_ROOM_DOOR = "ROOM_DOOR";
            //public const string TAG_ROOM_DOOR_START = "ROOM_DOOR_START";
            //public const string TAG_ROOM_DOOR_BOTHEND_MIN = "ROOM_DOOR_BOTHEND_MIN";
            //public const string TAG_ROOM_DOOR_BOTHEND_MAX = "ROOM_DOOR_BOTHEND_MAX";
            //public const string TAG_ROOM_DOOR_END = "ROOM_DOOR_END";
            //public const string TAG_ROOM_DOOR_AREA_STARTEND = "ROOM_DOOR_AREA_STARTEND";
            //public const string TAG_ROOM_DOOR_AREA = "ROOM_DOOR_AREA";
            //public const string TAG_ROOM_DOOR_AREA_EXPAND = "ROOM_DOOR_AREA_EXPAND";


            //public const string TAG_HALLWAY = "HALLWAY";
            //public const string TAG_HALLWAY_PATH_MAIN = "HALLWAY_PATH_MAIN";
            //public const string TAG_HALLWAY_PATH_EXPAND = "HALLWAY_PATH_EXPAND";
            //public const string TAG_HALLWAY_EDGE = "HALLWAY_EDGE";
            //public const string TAG_HALLWAY_EDGE_DOORCORRECTION = "HALLWAY_EDGE_DOORCORRECTION";
            //public const string TAG_HALLWAY_SAFE_DOORCORRECTION = "HALLWAY_SAFE_DOORCORRECTION";
            //public const string TAG_HALLWAY_SAFE = "HALLWAY_SAFE";


            //public const string TAG_TEMP_CONTROL_PATHCOST = "TEMP_CONTROL_PATHCOST";
            //public const string TAG_TEMP_CONTROL_PATHCOST2 = "TEMP_CONTROL_PATHCOST2";



            //public static readonly string[] TagSet_Hallways = new string[]
            //{
            //    TAG_HALLWAY,
            //    TAG_HALLWAY_PATH_MAIN,
            //    TAG_HALLWAY_PATH_EXPAND,
            //    TAG_HALLWAY_EDGE,
            //    TAG_HALLWAY_EDGE,
            //    TAG_HALLWAY_EDGE_DOORCORRECTION,
            //    TAG_HALLWAY_SAFE_DOORCORRECTION,
            //    TAG_HALLWAY_SAFE,
            //}; 
            #endregion



            ///======================================================================================================================================================



            //? Generate 생성 메서드



            ///<summary>
            ///<see cref="Grids"/>를 초기화하여 생성한다
            ///</summary>
            public bool Generate_GridArray()
            {
                Main.GenerateM.GeneratingCurrentSteps = GenerateManager.GenerateSteps.GenStep1_Grids;

                Grids?.Clear();

                //? 커스텀이벤트: 그리드 배열 생성 이전
                if (Main.Setting.CustomEvent.CreateGrid != null) { Main.Setting.CustomEvent.CreateGrid.Before_CreateGridArray(Main); }

                Grids = NewStageGridArray(Main, Setting.StageVector.StageWidth, Setting.StageVector.StageHeight);

                //? 커스텀이벤트: 그리드 배열 생성 이후
                if (Main.Setting.CustomEvent.CreateGrid != null) { Main.Setting.CustomEvent.CreateGrid.After_CreateGridArray(Main, Grids); }

                return true;
            }



            ///<summary>
            ///2차원 스테이지 그리드 배열을 새로 생성
            ///</summary>
            private static LinearGrid2D<Grid> NewStageGridArray(StageGenerator main, int stageWidth, int stageHeight)
            {
                //. 그리드 배열 초기화
                LinearGrid2D<Grid> grids = new LinearGrid2D<Grid>(stageWidth, stageHeight);


                //. 2차원 배열을 순회하며, Grid들을 생성한다
                for (int x = 0; x < stageWidth; x++)
                {
                    for (int y = 0; y < stageHeight; y++)
                    {
                        grids[x, y] = new Grid(main, x, y);
                    }
                }


                return grids;
            }



            ///<summary>
            ///[비동기] <see cref="Grids"/>를 초기화하여 생성한다
            ///</summary>
            public async UniTask<bool> Generate_GridArrayAsync()
            {
                Main.GenerateM.GeneratingCurrentSteps = GenerateManager.GenerateSteps.GenStep1_Grids;

                Grids?.Clear();

                //? 커스텀이벤트: 그리드 배열 생성 이전
                if (Main.Setting.CustomEvent.CreateGrid != null) { await Main.Setting.CustomEvent.CreateGrid.Before_CreateGridArrayAsync(Main); } else { await UniTask.CompletedTask; }

                Grids = await NewStageGridArrayAsync(Main, Setting.StageVector.StageWidth, Setting.StageVector.StageHeight);

                //? 커스텀이벤트: 그리드 배열 생성 이후
                if (Main.Setting.CustomEvent.CreateGrid != null) { await Main.Setting.CustomEvent.CreateGrid.After_CreateGridArrayAsync(Main, Grids); } else { await UniTask.CompletedTask; }

                return true;
            }



            /// <summary>
            /// [비동기] 2차원 스테이지 그리드 배열을 새로 생성
            /// </summary>
            private static async UniTask<LinearGrid2D<Grid>> NewStageGridArrayAsync(StageGenerator main, int stageWidth, int stageHeight)
            {
                //. 2차원 배열 미리 할당
                LinearGrid2D<Grid> grids = new LinearGrid2D<Grid>(stageWidth, stageHeight);


                //. 전체 Grid 개수
                int totalGrids = stageWidth * stageHeight;


                //. 시작 시간
                float startTime = Time.realtimeSinceStartup;


                //. "이번 프레임에서 만든 Grid" 카운트
                int createdCount = 0;


                for (int x = 0; x < stageWidth; x++)
                {
                    for (int y = 0; y < stageHeight; y++)
                    {
                        //. (1) 경과 시간 체크
                        float elapsedTime = Time.realtimeSinceStartup - startTime;


                        //. (2) 최대 허용 시간 초과 → 남은 Grid 즉시 생성 후 종료
                        if (elapsedTime > main.CalculateSetting.GridsGenerate_MaxSecondsAsync)
                        {
                            // 남아 있는 루프 구간은 동기로 생성 or 무시
                            for (int xx = x; xx < stageWidth; xx++)
                            {
                                int startY = (xx == x ? y : 0);
                                for (int yy = startY; yy < stageHeight; yy++)
                                {
                                    grids[xx, yy] = new Grid(main, xx, yy);
                                }
                            }

                            return grids;
                        }


                        //. (3) 동적 생성 개수 계산
                        float remainingTime = main.CalculateSetting.GridsGenerate_MaxSecondsAsync - elapsedTime;
                        int dynamicCreateCount = Mathf.Max(
                            1,
                            (int)(totalGrids * main.CalculateSetting.GridsGenerate_LengthAsync * remainingTime * 10)
                        );


                        //. (4) 실제 Grid 생성
                        grids[x, y] = new Grid(main, x, y);
                        createdCount++;


                        //. (5) "동적 생성 개수"만큼 채우면 다음 프레임으로
                        if (createdCount >= dynamicCreateCount)
                        {
                            createdCount = 0;
                            await UniTask.Yield(PlayerLoopTiming.Update);
                        }
                    }
                }

                return grids;
            }



            ///<summary>
            ///2차원 스테이지 그리드 배열 초기화
            ///</summary>
            [TitleGroup("그리드 매니저"), BoxGroup("그리드 매니저/박스", false)]
            [Button("그리드 배열 초기화", Icon = SdfIconType.Trash), GUIColor(0.67f, 0.57f, 0.93f)]
            public void ClearGridArray()
            {
                //Grids = null;
                Grids?.Clear();
            }



            ///======================================================================================================================================================



            //? Grid 유효성 확인



            #region Grid 유효성 확인

            /// <summary>받아온 <paramref name="gridPositionX"/> 가 유효한지 확인 (범위 안에 있는지 확인)</summary>
            public bool IsValid_GridPositionX(int gridPositionX)
            {
                return IsValid_GridArray && gridPositionX >= 0 && gridPositionX < GridArrayCountX;
            }



            /// <summary>받아온 <paramref name="gridPositionY"/> 가 유효한지 확인 (범위 안에 있는지 확인)</summary>
            public bool IsValid_GridPositionY(int gridPositionY)
            {
                return IsValid_GridArray && gridPositionY >= 0 && gridPositionY < GridArrayCountY;
            }



            /// <summary>받아온 <paramref name="gridPosition"/> 가 유효한지 확인 (범위 안에 있는지 확인)</summary>
            public bool IsValid_GridPosition(Vector2Int gridPosition)
            {
                return IsValid_GridPositionX(gridPosition.x) && IsValid_GridPositionY(gridPosition.y);
            }



            /// <summary>받아온 <paramref name="gridPositionX"/>, <paramref name="gridPositionY"/>가 유효한지 확인 (범위 안에 있는지 확인)</summary>
            public bool IsValid_GridPosition(int gridPositionX, int gridPositionY)
            {
                return IsValid_GridPosition(new Vector2Int(gridPositionX, gridPositionY));
            }



            /// <summary>유효한 <paramref name="gridPosition"/>을 받아왔다면 해당 <see cref="Grid"/>를 반환</summary>
            public bool TryGet_GridPosition(Vector2Int gridPosition, out Grid result)
            {
                if (!IsValid_GridPosition(gridPosition)) { result = null; return false; }
                result = Grids[gridPosition.x, gridPosition.y];
                return true;
            }



            /// <summary>유효한 <paramref name="gridPositionX"/>,<paramref name="gridPositionY"/>을 받아왔다면 해당 <see cref="Grid"/>를 반환</summary>
            public bool TryGet_GridPosition(int gridPositionX, int gridPositionY, out Grid result)
            {
                return TryGet_GridPosition(new Vector2Int(gridPositionX, gridPositionY), out result);
            }



            /// <summary>
            /// 유효한 <paramref name="gridPosition"/>을 받아왔다면 해당 <see cref="Grid"/>를 반환하지만,<br/>
            /// 유효하지 않을경우 유효한 범위 내에 가장 가까이 있는 그리드를 반환한다
            /// </summary>
            public bool TryGet_GridPositionClamp(Vector2Int gridPosition, out Vector2Int clampedGridPosition, out Grid result)
            {
                if (!IsValid_GridPosition(gridPosition))
                {
                    result = null;
                    clampedGridPosition = new Vector2Int(
                        Mathf.Clamp(gridPosition.x, 0, GridArrayCountX - 1), Mathf.Clamp(gridPosition.y, 0, GridArrayCountY - 1));
                    return false;
                }

                result = Grids[gridPosition.x, gridPosition.y];
                clampedGridPosition = new Vector2Int(gridPosition.x, gridPosition.y);
                return true;
            }



            /// <summary>
            /// 유효한 <paramref name="gridPosition"/>을 받아왔다면 해당 <see cref="Grid"/>를 반환하지만,<br/>
            /// 유효하지 않을경우 유효한 범위 내에 가장 가까이 있는 그리드를 반환한다
            /// </summary>
            public bool TryGet_GridPositionClamp(Vector2Int gridPosition, out Grid result)
            {
                return TryGet_GridPositionClamp(gridPosition, out var temp, out result);
            }



            #endregion



            ///======================================================================================================================================================



            //? Grid 그냥 얻기



            ///<summary>
            ///Grid를 그냥 얻어본다<br/>
            ///<b>유효성 검사를 하지 않아, null을 반환할수도 있다</b>
            /// </summary>
            public Grid GetGrid(Vector2Int gridPosition)
            {
                return Grids[gridPosition.x, gridPosition.y];
            }



            ///<summary>
            ///Grid를 그냥 얻어본다<br/>
            ///<b>유효성 검사를 하지 않아, null을 반환할수도 있다</b>
            /// </summary>
            public Grid GetGrid(int x, int y)
            {
                return GetGrid(new Vector2Int(x, y));
            }



            ///======================================================================================================================================================



            //? Grid 정보 확인, 반환 메소드



            /// <summary>받아온 Vector2Int의 <see cref="Grid"/>가 점유중인지 확인하기 (유효성검사 포함)</summary>
            public bool IsOccupied(Vector2Int gridPosition)
            {
                return IsValid_GridPosition(gridPosition) && Grids[gridPosition.x, gridPosition.y].IsOccupied;
            }



            /// <summary>
            /// 최대 4개의 이웃 좌표를 담고, foreach로 순회할 수 있는 구조체
            /// </summary>
            public struct UpTo4Neighbors : IEnumerable<Vector2Int>
            {
                public int Count;           // 유효한 이웃 개수 (0~4)
                public Vector2Int N0;
                public Vector2Int N1;
                public Vector2Int N2;
                public Vector2Int N3;

                /// <summary>
                /// 인덱서로 0~3번 이웃을 꺼낼 수 있음
                /// </summary>
                public Vector2Int this[int index]
                {
                    get
                    {
                        return index switch
                        {
                            0 => N0,
                            1 => N1,
                            2 => N2,
                            3 => N3,
                            _ => throw new IndexOutOfRangeException($"Invalid index: {index}")
                        };
                    }
                }

                // ─────────────────────────────────────────────────────────
                // IEnumerator<T>를 직접 struct로 구현해 "foreach"시 GC 할당을 없앱니다.
                // ─────────────────────────────────────────────────────────
                public struct Enumerator : IEnumerator<Vector2Int>
                {
                    private readonly UpTo4Neighbors _parent;
                    private int _index; // -1부터 시작

                    public Enumerator(in UpTo4Neighbors parent)
                    {
                        _parent = parent;
                        _index = -1;
                    }

                    public Vector2Int Current => _parent[_index];
                    object IEnumerator.Current => Current;

                    public bool MoveNext()
                    {
                        _index++;
                        return _index < _parent.Count;
                    }

                    public void Reset() => _index = -1;
                    public void Dispose() { /* no-op */ }
                }

                // foreach에서 사용할 GetEnumerator()
                public Enumerator GetEnumerator() => new Enumerator(this);

                // 인터페이스 명시적 구현
                IEnumerator<Vector2Int> IEnumerable<Vector2Int>.GetEnumerator() => GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }



            /// <summary>
            /// 받아온 Vector2Int와 1칸씩 인접해있는, 미점유된 그리드들의 그리드 좌표 배열을 struct로 반환
            /// </summary>
            public UpTo4Neighbors GetNeighborsGrids_NotOccupied(Vector2Int gridPosition)
            {
                // 1) 반환할 struct
                UpTo4Neighbors neighbors = default;
                // neighbors.Count = 0; // 구조체 초기화하면 0

                // 2) 4방향 좌표 미리 계산
                Vector2Int left = new Vector2Int(gridPosition.x - 1, gridPosition.y);
                Vector2Int right = new Vector2Int(gridPosition.x + 1, gridPosition.y);
                Vector2Int down = new Vector2Int(gridPosition.x, gridPosition.y - 1);
                Vector2Int up = new Vector2Int(gridPosition.x, gridPosition.y + 1);

                // 3) 각각 검사해서 유효하면 neighbors에 채워넣음
                if (IsValid_GridPosition(left) && !IsOccupied(left))
                {
                    neighbors.N0 = left;
                    neighbors.Count++;
                }
                if (IsValid_GridPosition(right) && !IsOccupied(right))
                {
                    switch (neighbors.Count)
                    {
                        case 0: neighbors.N0 = right; break;
                        case 1: neighbors.N1 = right; break;
                        case 2: neighbors.N2 = right; break;
                        case 3: neighbors.N3 = right; break;
                    }
                    neighbors.Count++;
                }
                if (IsValid_GridPosition(down) && !IsOccupied(down))
                {
                    switch (neighbors.Count)
                    {
                        case 0: neighbors.N0 = down; break;
                        case 1: neighbors.N1 = down; break;
                        case 2: neighbors.N2 = down; break;
                        case 3: neighbors.N3 = down; break;
                    }
                    neighbors.Count++;
                }
                if (IsValid_GridPosition(up) && !IsOccupied(up))
                {
                    switch (neighbors.Count)
                    {
                        case 0: neighbors.N0 = up; break;
                        case 1: neighbors.N1 = up; break;
                        case 2: neighbors.N2 = up; break;
                        case 3: neighbors.N3 = up; break;
                    }
                    neighbors.Count++;
                }

                // 4) 반환
                return neighbors;
            }



            /// <summary>
            /// 받아온 Vector2Int와 1칸씩 인접해있는, 미점유된 그리드들의 그리드 좌표 배열을 ReadOnlySpan으로 반환
            /// </summary>
            /// <param name="gridPosition">그리드 위치</param>
            /// <returns>미점유된 인접 그리드들의 ReadOnlySpan</returns>
            public ReadOnlySpan<Vector2Int> GetNeighborsGridSpan_NotOccupied_Legacy(Vector2Int gridPosition)
            {
                #region (설명보기) Span으로 배열을 잘라, 조건에 맞는 값이 들어있는 배열을 Span으로 반환하는 원리
                /*
                    초기 배열 상태:
                    ["A", "B", "C", "D"]

                    조건에 만족하지 않는 값이 "B" 라고 가정했을때

                    for 반복문 과정:
                    첫 번째 반복(i = 0)
                    요소: A
                    조건 확인: A는 조건에 만족합니다.
                    count가 0이므로, A를 자기 자신의 위치에 그대로 둡니다.
                    count를 1 증가시킵니다.
                    배열 상태: ["A", "B", "C", "D"]

                    count: 1
                    두 번째 반복(i = 1)
                    요소: B
                    조건 확인: B는 조건에 만족하지 않습니다.
                    B는 이동하지 않고, count도 증가하지 않습니다.
                    배열 상태: ["A", "B", "C", "D"]

                    count: 1
                    세 번째 반복(i = 2)
                    요소: C
                    조건 확인: C는 조건에 만족합니다.
                    count가 1이므로, C를 배열의 1 위치로 이동시킵니다.
                    count를 1 증가시킵니다.
                    배열 상태: ["A", "C", "C", "D"](이 시점에서 중복 C는 무시하고 진행)

                    count: 2
                    네 번째 반복(i = 3)
                    요소: D
                    조건 확인: D는 조건에 만족합니다.
                    count가 2이므로, D를 배열의 2 위치로 이동시킵니다.
                    count를 1 증가시킵니다.
                    배열 상태: ["A", "C", "D", "D"](이 시점에서 중복 D는 무시하고 진행)

                    count: 3
                    최종 배열 상태:
                    ["A", "C", "D", "D"]
                    유효한 요소 개수: count = 3

                    이 예에서, 배열의 마지막 요소 D가 중복되는 것처럼 보이지만, ReadOnlySpan으로 이 배열을 0에서 count까지의 범위로 참조하게 되면, 원하는 요소만 포함된 유효한 범위인["A", "C", "D"]만을 사용하게 됩니다.배열의 나머지 부분은 무시됩니다.
                    */
                #endregion


                Vector2Int[] possibleNeighbors = new Vector2Int[4]
                {
                    new Vector2Int(gridPosition.x - 1, gridPosition.y), //좌
                    new Vector2Int(gridPosition.x + 1, gridPosition.y), //우
                    new Vector2Int(gridPosition.x, gridPosition.y - 1), //하
                    new Vector2Int(gridPosition.x, gridPosition.y + 1)  //상
                };


                int count = 0;


                for (int i = 0; i < possibleNeighbors.Length; i++)
                {
                    Vector2Int neighbor = possibleNeighbors[i];

                    //? 이웃이 유효한 상태고, 점유 상태가 아닐겅우, 배열 앞으로 당겨오고 count에 1을 더한다
                    if (IsValid_GridPosition(neighbor) && !IsOccupied(neighbor))
                    {
                        possibleNeighbors[count++] = neighbor;
                    }
                }


                //? ReadOnlySpan을 사용하여 필요한 부분만큼의 배열을 참조한다.
                return new ReadOnlySpan<Vector2Int>(possibleNeighbors, 0, count);
            }



            ///======================================================================================================================================================



            //? Grid 이벤트 : 전체 실행



            /// <summary>
            /// 범위 전체에 <paramref name="gridEvent"/>를 실행
            /// </summary>
            public void GridsEvent_All(Action<Grid> gridEvent)
            {
                if (gridEvent == null) { return; }

                for (int x = 0; x < Setting.StageVector.StageWidth; x++)
                {
                    for (int y = 0; y < Setting.StageVector.StageHeight; y++)
                    {
                        gridEvent.Invoke(Grids[x, y]);
                    }
                }
            }



            /// <summary>
            /// (비동기) 범위 전체에 <paramref name="gridEvent"/>를 실행
            /// </summary>
            public async UniTask GridsEvent_AllAsync(Action<Grid> gridEvent)
            {
                if (gridEvent == null) return;

                int totalTasks = Setting.StageVector.StageWidth * Setting.StageVector.StageHeight;

                //. ArrayPool에서 필요한 크기만큼 임대
                var tasks = ArrayPool<UniTask>.Shared.Rent(totalTasks);
                try
                {
                    int index = 0;

                    for (int x = 0; x < Setting.StageVector.StageWidth; x++)
                    {
                        for (int y = 0; y < Setting.StageVector.StageHeight; y++)
                        {
                            //? 메인스레드에서 실행되도록 래핑
                            tasks[index++] = gridEvent_Async(gridEvent, x, y);
                        }
                    }

                    //! 여기서는 index == totalTasks가 보장됨 → 전체 구간을 그대로 WhenAll에 전달
                    await UniTask.WhenAll(tasks);
                }
                finally
                {
                    //. 반납(내용 클리어는 선택적)
                    ArrayPool<UniTask>.Shared.Return(tasks, clearArray: true);
                }

                async UniTask gridEvent_Async(Action<Grid> action, int x, int y)
                {
                    await UniTask.SwitchToMainThread();
                    action.Invoke(Grids[x, y]);
                }
            }




            ///======================================================================================================================================================



            //? Grid 이벤트 : Rect로 이벤트 실행
            ///
            //! 트랜스폼 Rect를 기준으로 작동? 하게? 할까??
            //! 트랜스폼 Rect를 기준으로 작동? 하게? 할까??
            //! 트랜스폼 Rect를 기준으로 작동? 하게? 할까??
            //! 트랜스폼 Rect를 기준으로 작동? 하게? 할까??


            #region Grid 이벤트 : Rect

            /// <summary>
            /// <paramref name="rect"/> 범위 안에 <paramref name="gridEvent"/>를 실행
            /// </summary>
            public void GridsEvent_Rect(bool safeMode, in Rect rect, Action<Grid> gridEvent, bool isMultiThread = false)
            {
                if (gridEvent == null) { return; }

                GetGridPositionsByRect_ForLoop(in rect, out int startX, out int endX, out int startY, out int endY, isMultiThread);

                if (safeMode)
                {
                    for (int x = startX; x < endX; x++)
                    {
                        for (int y = startY; y < endY; y++)
                        {
                            if (TryGet_GridPosition(x, y, out var grid))
                            {
                                gridEvent.Invoke(grid);
                            }
                        }
                    }
                }
                else
                {
                    for (int x = startX; x < endX; x++)
                    {
                        for (int y = startY; y < endY; y++)
                        {
                            gridEvent.Invoke(Grids[x, y]);
                        }
                    }
                }
            }



            /// <summary>
            /// <paramref name="rects"/>를 순회하며 범위 안에 <paramref name="gridEvent"/>를 실행
            /// </summary>
            public void GridsEvent_Rects(bool safeMode, ICollection<Rect> rects, Action<Grid> gridEvent)
            {
                if (gridEvent == null) { return; }

                foreach (var rect in rects)
                {
                    GridsEvent_Rect(safeMode, rect, gridEvent);
                }
            }



            /// <summary>
            /// <paramref name="rect"/> 범위 안에 <paramref name="gridEvent"/>를 실행<br/>
            /// <paramref name="gridEvent"/>에 성공하고, <paramref name="resultGrids"/>가 null이 아니라면, 해당 콜렉션에 추가
            /// </summary>
            /// <param name="resultGrids"><paramref name="gridEvent"/>실행에 성공하면 해당 그리드가 추가된다 (null이 아니라면!)</param>
            public void GridsEvent_Rect(bool safeMode, in Rect rect, Func<Grid, bool> gridEvent, in ICollection<Grid> resultGrids = null, bool isMultiThread = false)
            {
                if (gridEvent == null) { return; }

                GetGridPositionsByRect_ForLoop(in rect, out int startX, out int endX, out int startY, out int endY, isMultiThread);

                if (safeMode)
                {
                    for (int x = startX; x < endX; x++)
                    {
                        for (int y = startY; y < endY; y++)
                        {
                            if (TryGet_GridPosition(x, y, out var grid) && gridEvent.Invoke(grid) && resultGrids != null)
                            {
                                resultGrids.Add(grid);
                            }
                        }
                    }
                }
                else
                {
                    for (int x = startX; x < endX; x++)
                    {
                        for (int y = startY; y < endY; y++)
                        {
                            var grid = Grids[x, y];

                            if (gridEvent.Invoke(grid) && resultGrids != null)
                            {
                                resultGrids.Add(grid);
                            }
                        }
                    }
                }
            }



            /// <summary>
            /// <paramref name="rects"/>를 순회하며 범위 안에 <paramref name="gridEvent"/>를 실행<br/>
            /// <paramref name="gridEvent"/>에 성공하고, <paramref name="resultGrids"/>가 null이 아니라면, 해당 콜렉션에 추가
            /// </summary>
            /// <param name="resultGrids"><paramref name="gridEvent"/>실행에 성공하면 해당 그리드가 추가된다 (null이 아니라면!)</param>
            public void GridsEvent_Rects(bool safeMode, ICollection<Rect> rects, Func<Grid, bool> gridEvent, in ICollection<Grid> resultGrids = null)
            {
                if (gridEvent == null) { return; }

                foreach (var rect in rects)
                {
                    GridsEvent_Rect(safeMode, rect, gridEvent, resultGrids);
                }
            }



            /// <summary>
            /// <paramref name="rect"/> 범위 안에 <paramref name="gridEvent"/>를 실행<br/>
            /// <paramref name="gridEvent"/>에 성공하고, <paramref name="resultGrids"/>가 null이 아니라면, 해당 콜렉션에 추가
            /// </summary>
            /// <param name="resultGrids"><paramref name="gridEvent"/>실행에 성공하면 해당 그리드가 추가된다 (null이 아니라면!)</param>
            public void GridsEvent_Rect(bool safeMode, in Rect rect, Func<GridManager, Grid, bool> gridEvent, in ICollection<Grid> resultGrids = null, bool isMultiThread = false)
            {
                if (gridEvent == null) { return; }

                GetGridPositionsByRect_ForLoop(in rect, out int startX, out int endX, out int startY, out int endY, isMultiThread);

                if (safeMode)
                {
                    for (int x = startX; x < endX; x++)
                    {
                        for (int y = startY; y < endY; y++)
                        {
                            if (TryGet_GridPosition(x, y, out var grid) && gridEvent.Invoke(this, grid) && resultGrids != null)
                            {
                                resultGrids.Add(grid);
                            }
                        }
                    }
                }
                else
                {
                    for (int x = startX; x < endX; x++)
                    {
                        for (int y = startY; y < endY; y++)
                        {
                            var grid = Grids[x, y];

                            if (gridEvent.Invoke(this, grid) && resultGrids != null)
                            {
                                resultGrids.Add(grid);
                            }
                        }
                    }
                }
            }



            /// <summary>
            /// <paramref name="rects"/>를 순회하며 범위 안에 <paramref name="gridEvent"/>를 실행<br/>
            /// <paramref name="gridEvent"/>에 성공하고, <paramref name="resultGrids"/>가 null이 아니라면, 해당 콜렉션에 추가
            /// </summary>
            /// <param name="resultGrids"><paramref name="gridEvent"/>실행에 성공하면 해당 그리드가 추가된다 (null이 아니라면!)</param>
            public void GridsEvent_Rects(bool safeMode, ICollection<Rect> rects, Func<GridManager, Grid, bool> gridEvent, in ICollection<Grid> resultGrids = null)
            {
                if (gridEvent == null) { return; }

                foreach (var rect in rects)
                {
                    GridsEvent_Rect(safeMode, rect, gridEvent, resultGrids);
                }
            }



            ///<summary>
            ///<paramref name="rect"/>안에 있는 그리드 좌표 X,Y의 시작~끝 좌표를 연산하여, 각 좌표들을 반환한다<br/>
            ///딱히 유효성 검사는 하지 않으며, 받아온 Rect를 <see cref="StageGeneratorSetting.GetStageOriginTransformCurrent"/>에 맞춰 변환된다<br/>
            ///</summary>
            ///<param name="startX">X좌표의 시작점</param>
            ///<param name="endX">X좌표의 종점</param>
            ///<param name="startY">Y좌표의 시작점</param>
            ///<param name="endY">Y좌표의 종점</param>
            private void GetGridPositionsByRect_ForLoop(in Rect rect, out int startX, out int endX, out int startY, out int endY, bool isMultiThread = false)
            {
                //. 원래 복잡했었는데 rect 연산 구조 깔끔하게 만드니까 따로 여기서 연산 할 필요가 없어졌음
                startX = Mathf.CeilToInt(rect.xMin);
                endX = Mathf.CeilToInt(rect.xMax);
                startY = Mathf.CeilToInt(rect.yMin);
                endY = Mathf.CeilToInt(rect.yMax);

                //. 혹시 모르니까 범위 제약
                startX = Mathf.Max(0, startX);
                endX = Mathf.Min(GridArrayCountX, endX);
                startY = Mathf.Max(0, startY);
                endY = Mathf.Min(GridArrayCountY, endY);
            }



            #endregion



            #region Grid 이벤트 : RectInt


            /// <summary>
            /// <paramref name="rectInts"/> 범위 안에 <paramref name="gridEvent"/>를 실행
            /// </summary>
            public void GridsEvent_RectInt(bool safeMode, in RectInt rectInts, Action<Grid> gridEvent)
            {
                if (gridEvent == null) { return; }

                GetGridPositionsByRect_ForLoop(in rectInts, out int startX, out int endX, out int startY, out int endY);

                if (safeMode)
                {
                    for (int x = startX; x < endX; x++)
                    {
                        for (int y = startY; y < endY; y++)
                        {
                            if (TryGet_GridPosition(x, y, out var grid))
                            {
                                gridEvent.Invoke(grid);
                            }
                        }
                    }
                }
                else
                {
                    for (int x = startX; x < endX; x++)
                    {
                        for (int y = startY; y < endY; y++)
                        {
                            gridEvent.Invoke(Grids[x, y]);
                        }
                    }
                }
            }



            /// <summary>
            /// <paramref name="rectInts"/>를 순회하며 범위 안에 <paramref name="gridEvent"/>를 실행
            /// </summary>
            public void GridsEvent_RectInts(bool safeMode, ICollection<RectInt> rectInts, Action<Grid> gridEvent)
            {
                if (gridEvent == null) { return; }

                foreach (var rect in rectInts)
                {
                    GridsEvent_RectInt(safeMode, rect, gridEvent);
                }
            }



            /// <summary>
            /// <paramref name="rectInts"/> 범위 안에 <paramref name="gridEvent"/>를 실행<br/>
            /// <paramref name="gridEvent"/>에 성공하고, <paramref name="resultGrids"/>가 null이 아니라면, 해당 콜렉션에 추가
            /// </summary>
            /// <param name="resultGrids"><paramref name="gridEvent"/>실행에 성공하면 해당 그리드가 추가된다 (null이 아니라면!)</param>
            public void GridsEvent_RectInt(bool safeMode, in RectInt rectInts, Func<Grid, bool> gridEvent, in ICollection<Grid> resultGrids = null)
            {
                if (gridEvent == null) { return; }

                GetGridPositionsByRect_ForLoop(in rectInts, out int startX, out int endX, out int startY, out int endY);

                if (safeMode)
                {
                    for (int x = startX; x < endX; x++)
                    {
                        for (int y = startY; y < endY; y++)
                        {
                            if (TryGet_GridPosition(x, y, out var grid) && gridEvent.Invoke(grid) && resultGrids != null)
                            {
                                resultGrids.Add(grid);
                            }
                        }
                    }
                }
                else
                {
                    for (int x = startX; x < endX; x++)
                    {
                        for (int y = startY; y < endY; y++)
                        {
                            var grid = Grids[x, y];

                            if (gridEvent.Invoke(grid) && resultGrids != null)
                            {
                                resultGrids.Add(grid);
                            }
                        }
                    }
                }
            }



            /// <summary>
            /// <paramref name="rectInts"/>를 순회하며 범위 안에 <paramref name="gridEvent"/>를 실행<br/>
            /// <paramref name="gridEvent"/>에 성공하고, <paramref name="resultGrids"/>가 null이 아니라면, 해당 콜렉션에 추가
            /// </summary>
            /// <param name="resultGrids"><paramref name="gridEvent"/>실행에 성공하면 해당 그리드가 추가된다 (null이 아니라면!)</param>
            public void GridsEvent_RectInts(bool safeMode, ICollection<RectInt> rectInts, Func<Grid, bool> gridEvent, in ICollection<Grid> resultGrids = null)
            {
                if (gridEvent == null) { return; }

                foreach (var rect in rectInts)
                {
                    GridsEvent_RectInt(safeMode, rect, gridEvent, resultGrids);
                }
            }



            /// <summary>
            /// <paramref name="rectInts"/> 범위 안에 <paramref name="gridEvent"/>를 실행<br/>
            /// <paramref name="gridEvent"/>에 성공하고, <paramref name="resultGrids"/>가 null이 아니라면, 해당 콜렉션에 추가
            /// </summary>
            /// <param name="resultGrids"><paramref name="gridEvent"/>실행에 성공하면 해당 그리드가 추가된다 (null이 아니라면!)</param>
            public void GridsEvent_RectInt(bool safeMode, in RectInt rectInts, Func<GridManager, Grid, bool> gridEvent, in ICollection<Grid> resultGrids = null)
            {
                if (gridEvent == null) { return; }

                GetGridPositionsByRect_ForLoop(in rectInts, out int startX, out int endX, out int startY, out int endY);

                if (safeMode)
                {
                    for (int x = startX; x < endX; x++)
                    {
                        for (int y = startY; y < endY; y++)
                        {
                            if (TryGet_GridPosition(x, y, out var grid) && gridEvent.Invoke(this, grid) && resultGrids != null)
                            {
                                resultGrids.Add(grid);
                            }
                        }
                    }
                }
                else
                {
                    for (int x = startX; x < endX; x++)
                    {
                        for (int y = startY; y < endY; y++)
                        {
                            var grid = Grids[x, y];

                            if (gridEvent.Invoke(this, grid) && resultGrids != null)
                            {
                                resultGrids.Add(grid);
                            }
                        }
                    }
                }
            }



            /// <summary>
            /// <paramref name="rectInts"/>를 순회하며 범위 안에 <paramref name="gridEvent"/>를 실행<br/>
            /// <paramref name="gridEvent"/>에 성공하고, <paramref name="resultGrids"/>가 null이 아니라면, 해당 콜렉션에 추가
            /// </summary>
            /// <param name="resultGrids"><paramref name="gridEvent"/>실행에 성공하면 해당 그리드가 추가된다 (null이 아니라면!)</param>
            public void GridsEvent_RectInts(bool safeMode, ICollection<RectInt> rectInts, Func<GridManager, Grid, bool> gridEvent, in ICollection<Grid> resultGrids = null)
            {
                if (gridEvent == null) { return; }

                foreach (var rect in rectInts)
                {
                    GridsEvent_RectInt(safeMode, rect, gridEvent, resultGrids);
                }
            }



            ///<summary>
            ///<paramref name="rectInt"/>안에 있는 그리드 좌표 X,Y의 시작~끝 좌표를 연산하여, 각 좌표들을 반환한다<br/>
            ///딱히 유효성 검사는 하지 않으며, 받아온 Rect를 <see cref="StageGeneratorSetting.GetStageOriginTransformCurrent"/>에 맞춰 변환된다<br/>
            ///</summary>
            ///<param name="startX">X좌표의 시작점</param>
            ///<param name="endX">X좌표의 종점</param>
            ///<param name="startY">Y좌표의 시작점</param>
            ///<param name="endY">Y좌표의 종점</param>
            private void GetGridPositionsByRect_ForLoop(in RectInt rectInt, out int startX, out int endX, out int startY, out int endY)
            {
                startX = rectInt.xMin;
                endX = rectInt.xMax;
                startY = rectInt.yMin;
                endY = rectInt.yMax;


                // 범위를 벗어나지 않게 최소값~최대값 을 조정한다
                startX = Mathf.Max(startX, 0);  // 범위 내 최소값 조정
                endX = Mathf.Min(endX, Setting.StageVector.StageWidth);  // 범위 내 최대값 조정
                startY = Mathf.Max(startY, 0);  // 범위 내 최소값 조정
                endY = Mathf.Min(endY, Setting.StageVector.StageHeight);  // 범위 내 최대값 조정
            }



            #endregion



            ///======================================================================================================================================================



            //? Grid 이벤트 : 좌표 배열



            #region Grid 이벤트 : 좌표 배열

            /// <summary><paramref name="gridPositions"/>를 순회하며 이벤트 실행</summary>
            /// <param name="safeMode">
            /// <paramref name="safeMode"/>가 활성화되면, 매 반복문마다 해당 좌표가 유효한지 검사한다
            /// </param>
            /// <param name="resultGrids"><paramref name="gridEvent"/>실행에 성공하면 해당 그리드가 추가된다 (null이 아니라면!)</param>
            public void GridsEvent_Vectors(IEnumerable<Vector2Int> gridPositions, bool safeMode, Func<GridManager, Grid, bool> gridEvent, in ICollection<Grid> resultGrids = null)
            {
                if (gridEvent == null) { return; }

                if (safeMode)
                {
                    GridsEvent_Vectors_Safe(gridPositions, gridEvent, resultGrids);
                }
                else
                {
                    GridsEvent_Vectors_UnSafe(gridPositions, gridEvent, resultGrids);
                }
            }



            /// <summary><paramref name="gridPositions"/>를 순회하며 이벤트 실행</summary>
            /// <param name="gridPositions">중심 좌표 배열</param>
            /// <param name="resultGrids"><paramref name="gridEvent"/>실행에 성공하면 해당 그리드가 추가된다 (null이 아니라면!)</param>
            public void GridsEvent_Vectors(bool useSafeMode, Func<GridManager, Grid, bool> gridEvent, in ICollection<Grid> resultGrids = null, params Vector2Int[] gridPositions) => GridsEvent_Vectors(gridPositions, useSafeMode, gridEvent, resultGrids); // params를 사용하기위해 Overload



            //? [안전] 실행



            /// <summary>
            /// <paramref name="gridPositions"/>를 순회하며 이벤트 실행 (안전하게!)
            /// </summary>
            /// <param name="resultGrids"><paramref name="gridEvent"/>실행에 성공하면 해당 그리드가 추가된다 (null이 아니라면!)</param>
            public void GridsEvent_Vectors_Safe(IEnumerable<Vector2Int> gridPositions, Func<GridManager, Grid, bool> gridEvent, in ICollection<Grid> resultGrids = null)
            {
                foreach (var vector in gridPositions)
                {
                    if (!TryGet_GridPosition(vector, out var grid)) { continue; }

                    if (gridEvent.Invoke(this, grid))
                    {
                        if (resultGrids != null) { resultGrids.Add(grid); }
                    }
                }
            }



            /// <summary>
            /// <paramref name="gridPositions"/>를 순회하며 이벤트 실행 (안전하게!)
            /// </summary>
            /// <param name="resultGrids"><paramref name="gridEvent"/>실행에 성공하면 해당 그리드가 추가된다 (null이 아니라면!)</param>
            public void GridsEvent_Vectors_Safe(Func<GridManager, Grid, bool> gridConditionEvent, in ICollection<Grid> resultGrids = null, params Vector2Int[] gridPositions) => GridsEvent_Vectors_Safe(gridPositions, gridConditionEvent, in resultGrids); // params를 사용하기위해 Overload



            //? [위험] 실행



            /// <summary>
            /// <paramref name="gridPositions"/>를 순회하며 이벤트 실행 (위험하게!)
            /// </summary>
            /// <param name="resultGrids"><paramref name="gridEvent"/>실행에 성공하면 해당 그리드가 추가된다 (null이 아니라면!)</param>
            public void GridsEvent_Vectors_UnSafe(IEnumerable<Vector2Int> gridPositions, Func<GridManager, Grid, bool> gridEvent, in ICollection<Grid> resultGrids = null)
            {
                foreach (var vector in gridPositions)
                {
                    try
                    {
                        //Debug.LogError($"{vector}");

                        var grid = Grids[vector.x, vector.y];
                        if (gridEvent.Invoke(this, grid))
                        {
                            if (resultGrids != null) { resultGrids.Add(grid); }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"{vector.x}/{vector.y}\n{ex}");
                    }

                }
            }



            /// <summary>
            /// <paramref name="gridPositions"/>를 순회하며 이벤트 실행 (위험하게!)
            /// </summary>
            /// <param name="resultGrids"><paramref name="gridEvent"/>실행에 성공하면 해당 그리드가 추가된다 (null이 아니라면!)</param>
            public void GridsEvent_Vectors_UnSafe(Func<GridManager, Grid, bool> gridEvent, in ICollection<Grid> resultGrids = null, params Vector2Int[] gridPositions) => GridsEvent_Vectors_UnSafe(gridPositions, gridEvent, in resultGrids); // params를 사용하기위해 Overload            

            #endregion



            ///======================================================================================================================================================



            //? Grid 이벤트 : Grid 배열



            #region Grid 이벤트 : Grid 배열

            /// <summary><paramref name="grids"/>를 순회하며 이벤트 실행</summary>
            public void GridsEvent_Grids(IEnumerable<Grid> grids, Action<GridManager, Grid> gridEvent)
            {
                if (gridEvent == null) { return; }

                foreach (var grid in grids)
                {
                    gridEvent.Invoke(this, grid);
                }
            }



            /// <summary>[비동기] <paramref name="grids"/>를 순회하며 이벤트 실행</summary>
            public async UniTask GridsEvent_GridsAsync(IEnumerable<Grid> grids, Func<GridManager, Grid, UniTask> gridEvent)
            {
                if (gridEvent == null) { return; }

                //! 매 프레임/자주 호출될 수 있는 메서드이므로, 임시 List 할당을 풀링으로 대체
                using (var pooled = ListPool<UniTask>.Get(out var tasks)) //? Dispose 시 자동 Clear & Release
                {
                    foreach (var grid in grids)
                    {
                        //? 각 그리드의 비동기 작업을 수집(동시 실행)
                        tasks.Add(gridEvent(this, grid));
                    }

                    //? IEnumerable<UniTask> 오버로드를 사용하므로 List 그대로 전달 가능
                    await UniTask.WhenAll(tasks);
                    // using 블록 종료 시점에 리스트는 풀로 반환됨
                }
            }





            /// <summary><paramref name="grids"/>를 순회하며 이벤트 실행</summary>
            /// <param name="grids">중심 좌표 배열</param>
            public void GridsEvent_Grids(Action<GridManager, Grid> gridEvent, params Grid[] grids) => GridsEvent_Grids(grids, gridEvent); // params를 사용하기위해 Overload



            #endregion



            #region Grid 이벤트 : Grid 배열

            ///// <summary><paramref name="grids"/>를 순회하며 이벤트 실행</summary>
            ///// <param name="safeMode">
            ///// <paramref name="safeMode"/>가 활성화되면, 매 반복문마다 해당 좌표가 유효한지 검사한다
            ///// </param>
            ///// <param name="resultGrids"><paramref name="gridEvent"/>실행에 성공하면 해당 그리드가 추가된다 (null이 아니라면!)</param>
            //public void GridsEvent_Grids(IEnumerable<Grid> grids, bool safeMode, Func<GridManager, Grid, bool> gridEvent, in ICollection<Grid> resultGrids = null)
            //{
            //    if (gridEvent == null) { return; }

            //    if (safeMode)
            //    {
            //        GridsEvent_Grids_Safe(grids, gridEvent, resultGrids);
            //    }
            //    else
            //    {
            //        GridsEvent_Grids_UnSafe(grids, gridEvent, resultGrids);
            //    }
            //}



            ///// <summary><paramref name="grids"/>를 순회하며 이벤트 실행</summary>
            ///// <param name="grids">중심 좌표 배열</param>
            ///// <param name="resultGrids"><paramref name="gridConditionEvent"/>의 조건에 부합해, 이벤트가 실행된 그리드들이 이 리스트에 추가된다, null을 넣으면 추가되지않는다</param>
            //public void GridsEvent_Grids(bool useSafeMode, Func<GridManager, Grid, bool> gridConditionEvent, in ICollection<Grid> resultGrids = null, params Grid[] grids) => GridsEvent_Grids(grids, useSafeMode, gridConditionEvent, resultGrids); // params를 사용하기위해 Overload



            ////? [안전] 실행



            ///// <summary>
            ///// <paramref name="grids"/>를 순회하며 이벤트 실행 (안전하게!)
            ///// </summary>
            ///// <param name="resultGrids"><paramref name="gridEvent"/>실행에 성공하면 해당 그리드가 추가된다 (null이 아니라면!)</param>
            //public void GridsEvent_Grids_Safe(IEnumerable<Grid> grids, Func<GridManager, Grid, bool> gridEvent, in ICollection<Grid> resultGrids = null)
            //{
            //    foreach (var grid in grids)
            //    {
            //        if (gridEvent.Invoke(this, grid))
            //        {
            //            if (resultGrids != null) { resultGrids.Add(grid); }
            //        }
            //    }
            //}



            ///// <summary>
            ///// <paramref name="grids"/>를 순회하며 이벤트 실행 (안전하게!)
            ///// </summary>
            ///// <param name="resultGrids"><paramref name="gridEvent"/>실행에 성공하면 해당 그리드가 추가된다 (null이 아니라면!)</param>
            //public void GridsEvent_Grids_Safe(Func<GridManager, Grid, bool> gridConditionEvent, in ICollection<Grid> resultGrids = null, params Grid[] grids) => GridsEvent_Grids_Safe(grids, gridConditionEvent, in resultGrids); // params를 사용하기위해 Overload



            ////? [위험] 실행



            ///// <summary>
            ///// <paramref name="grids"/>를 순회하며 이벤트 실행 (위험하게!)
            ///// </summary>
            ///// <param name="resultGrids"><paramref name="gridEvent"/>실행에 성공하면 해당 그리드가 추가된다 (null이 아니라면!)</param>
            //public void GridsEvent_Grids_UnSafe(IEnumerable<Grid> grids, Func<GridManager, Grid, bool> gridEvent, in ICollection<Grid> resultGrids = null)
            //{
            //    foreach (var grid in grids)
            //    {
            //        try
            //        {
            //            if (gridEvent.Invoke(this, grid))
            //            {
            //                if (resultGrids != null) { resultGrids.Add(grid); }
            //            }
            //        }
            //        catch (Exception ex)
            //        {
            //            Debug.LogError($"{grid.GridPosition.x}/{grid.GridPosition.y}\n{ex}");
            //        }
            //    }
            //}



            ///// <summary>
            ///// <paramref name="grids"/>를 순회하며 이벤트 실행 (위험하게!)
            ///// </summary>
            ///// <param name="resultGrids"><paramref name="gridEvent"/>실행에 성공하면 해당 그리드가 추가된다 (null이 아니라면!)</param>
            //public void GridsEvent_Grids_UnSafe(Func<GridManager, Grid, bool> gridEvent, in ICollection<Grid> resultGrids = null, params Grid[] grids) => GridsEvent_Grids_UnSafe(grids, gridEvent, in resultGrids); // params를 사용하기위해 Overload            

            #endregion



            ///======================================================================================================================================================



            //? Grid 이벤트 : 좌표 경로 리스트



            /// <summary>
            /// 좌표 경로 리스트를 받아와, 그 경로들을 순회하며, 각각 좌표값의 중심으로 부터 너비/높이로 확장하여, 반복문에 사용할수있는 값들을 반환한다
            /// </summary>
            /// <param name="pathList">중심 좌표 경로 리스트</param>
            /// <param name="width">너비</param>
            /// <param name="height">높이</param>
            /// <param name="expandPositiveHorizontal">비대칭으로 확장을 해야할때, 가로 방향을 양수로 확장할지 여부</param>
            /// <param name="expandPositiveVertical">비대칭으로 확장을 해야할때, 세로 방향을 양수로 확장할지 여부</param>
            /// <param name="resultGrids"><paramref name="gridEvent"/>실행에 성공하면 해당 그리드가 추가된다 (null이 아니라면!)</param>
            public bool GridsEvent_PathList(IList<Vector2Int> pathList, bool safeMode, bool bedingCorrection, int width, int height, bool expandPositiveHorizontal, bool expandPositiveVertical, Func<GridManager, Grid, bool> gridEvent, in ICollection<Grid> resultGrids = null)
            {
                if (gridEvent == null) { return false; }


                // 작업하기전에, 좌표가 유효한지 확인, 유효하지 않으면 false 반환
                // 확장이 아닌 메인 좌표부터 유효하지 않다면, 실행 조차 되지 않아야한다
                foreach (var vector in pathList)
                {
                    if (!IsValid_GridPosition(vector)) { return false; }
                }


                for (int i = 0; i < pathList.Count; i++)
                {
                    Vector2Int path = pathList[i];

                    // 경로 리스트 확장 파라미터 연산
                    Calculate_ForPathList(pathList, i, bedingCorrection, width, height, expandPositiveHorizontal, expandPositiveVertical, out int current_Width, out int current_Height, out bool current_expandPositiveHorizontal, out bool current_expandPositiveVertical);


                    //이벤트를 실행한다 (너비, 높이, 너비양수확장,높이양수확장)
                    // 유효하지 않은 좌표를 넣었다면, 즉시 false가 반환된다
                    bool execute = GridEvent_Expand(path, safeMode, current_Width, current_Height, current_expandPositiveHorizontal, current_expandPositiveVertical, gridEvent, resultGrids);
                    if (execute == false) { return false; }
                }

                return true;
            }



            /// <summary>
            /// 좌표 리스트인 <paramref name="pathList"/>와 인덱스를 받아와 (for 반복문에 사용한다)<br/>
            /// 현재 좌표가 꺾이고 있거나, 좌표가 향하는 방향을 구하여,<br/>
            /// 좌표의 너비/높이, 확장 수평/수직 을 전환하여 각각 반환한다<br/>
            /// (EX1: (선택가능)현재 좌표가 꺾이고 있다면, 자연스러운 형성을 위해 너비/높이가 모두 <b>너비</b>가 된다)<br/>
            /// (EX2: 현재 좌표가 꺾이지 않고있고, 좌/우 로 뻗어있다면, <b>너비/높이</b>, <b>확장 수평/수직</b> 을 서로 바꾸어 반환한다
            /// </summary>
            /// <param name="pathList">대상이 되는 경로 리스트</param>
            /// <param name="currentIndex">현재 인덱스 (보통은 i )</param>
            /// <param name="bedingCorrection">활성화시, 좌표가 꺾이고 있을때, 너비와 높이가 모두 너비를 사용한다</param>
            /// <param name="width">확장 너비</param>
            /// <param name="height">확장 높이</param>
            /// <param name="expandPositiveHorizontal">너비 양수 확장</param>
            /// <param name="expandPositiveVertical">높이 양수 확장</param>
            public static void Calculate_ForPathList(IList<Vector2Int> pathList, int currentIndex,
               bool bedingCorrection, int width, int height, bool expandPositiveHorizontal, bool expandPositiveVertical,
                out int result_Width, out int result_Height, out bool result_ExpandPositiveHorizontal, out bool result_ExpandPositiveVertical)
            {
                // 너비와 높이를 반대로 적용하는 상황을 대비해, 따로 변수 구분한다



                //! 현재 이 좌표가 "꺾이고" 있다면
                if (bedingCorrection && pathList.IsPathTurning(currentIndex))
                {
                    // 꺾이고 있다면, 너비와 높이 둘다 "너비"를 사용한다
                    // 이렇게 하지 않으면, 복도가 꺾이는 부분에 "너비"가 제대로 적용되지 않는다
                    result_Width = width;
                    result_Height = width;
                    result_ExpandPositiveHorizontal = expandPositiveHorizontal;
                    result_ExpandPositiveVertical = expandPositiveVertical;
                }


                //! 복도가 안꺾였으며, 복도가 좌or우 로 뻗어있을경우,
                else if (pathList.Count > 1 && pathList.Get4DirectionFromVector2Int(currentIndex).IsHorizontal())
                {
                    //너비와 높이의 값을 각각 반대로 적용
                    result_Width = height;
                    result_Height = width;
                    result_ExpandPositiveHorizontal = expandPositiveVertical;
                    result_ExpandPositiveVertical = expandPositiveHorizontal;
                }
                else
                {
                    result_Width = width;
                    result_Height = height;
                    result_ExpandPositiveHorizontal = expandPositiveHorizontal;
                    result_ExpandPositiveVertical = expandPositiveVertical;
                }
            }



            ///======================================================================================================================================================



            //? Grid 이벤트 : 좌표 확장(인접) 이벤트



            /// <summary>
            /// 좌표값의 중심으로 부터 너비/높이로 확장하여, 반복문에 사용할수있는 값들을 반환한다
            /// </summary>
            /// <param name="gridPosition">중심 좌표</param>
            /// <param name="width">너비</param>
            /// <param name="height">높이</param>
            /// <param name="expandPositiveHorizontal">비대칭으로 확장을 해야할때, 가로 방향을 양수로 확장할지 여부</param>
            /// <param name="expandPositiveVertical">비대칭으로 확장을 해야할때, 세로 방향을 양수로 확장할지 여부</param>
            /// <param name="resultGrids"><paramref name="gridEvent"/>실행에 성공하면 해당 그리드가 추가된다 (null이 아니라면!)</param>
            public bool GridEvent_Expand(Vector2Int gridPosition, bool safeMode, int width, int height, bool expandPositiveHorizontal, bool expandPositiveVertical, Func<GridManager, Grid, bool> gridEvent, in ICollection<Grid> resultGrids = null)
            {
                // 너비와 높이가 모두 1이라면, 확장할 필요가 없으니 해당 좌표에 실행되고 return 된다
                if (width == 1 && height == 1)
                {
                    executeEvent(gridPosition.x, gridPosition.y, resultGrids);
                    return true;
                }


                //? 반복문 사용을 위한 그리드 좌표의 시작점~끝점을 X,Y로 나눠 얻는다
                // 이 값이 모두 포함된 좌표로 실행된다 (EX: startX:0, endX: 2 = 0, 1, 2)
                GetExpandGridPositionsFromGridPosition_ForLoop(gridPosition, width, height, expandPositiveHorizontal, expandPositiveVertical, out int startX, out int endX, out int startY, out int endY);


                // 2중반복문으로 각각 GridEvent를 실행한다,
                //x 실행에 실패했다면, (유효하지 않은 좌표) 즉시 false를 반환한다
                for (int x = startX; x <= endX; x++)
                {
                    for (int y = startY; y <= endY; y++)
                    {
                        executeEvent(x, y, resultGrids);
                    }
                }


                return true;


                //? GridEvent를 실행, 성공시 배열에 추가됨
                bool executeEvent(int x, int y, in ICollection<Grid> resultGrids)
                {
                    if (safeMode && !TryGet_GridPosition(x, y, out var grid))
                    {
                        return false;
                    }
                    else
                    {
                        grid = Grids[x, y];
                    }
                    if (gridEvent.Invoke(this, grid) && resultGrids != null)
                    {
                        resultGrids.Add(grid);
                    }

                    return true;
                }
            }



            /// <summary>
            /// 좌표값의 중심으로 부터 너비/높이로 확장하여, 반복문에 사용할수있는 값들을 반환한다<br/>
            /// <b>반환되는 좌표들은, 안전검사는 하지 않아 유효하지 않은 (범위를 벗어난) 좌표를 반환할수도 있다</b>
            /// </summary>
            /// <param name="gridPosition">중심 좌표</param>
            /// <param name="height">높이, 1 초과일경우, 중심점을 기준으로 확장됨</param>
            /// <param name="height">중심을 기준으로 확장할 높이</param>
            /// <param name="expandPositiveHorizontal">가로 방향을 양수로 확장할지 여부</param>
            /// <param name="expandPositiveVertical">세로 방향을 양수로 확장할지 여부</param>
            public static void GetExpandGridPositionsFromGridPosition_ForLoop(Vector2Int gridPosition, int width, int height, bool expandPositiveHorizontal, bool expandPositiveVertical, out int startX, out int endX, out int startY, out int endY)
            {
                width = Mathf.Max(1, width);
                height = Mathf.Max(1, height);
                calculateSingleAxisExpansion(gridPosition.x, width, expandPositiveHorizontal, out startX, out endX);
                calculateSingleAxisExpansion(gridPosition.y, height, expandPositiveVertical, out startY, out endY);

                static void calculateSingleAxisExpansion(int center, int expandValue, bool expandPositive, out int start, out int end)
                {
                    int expandHalf = expandValue / 2; //소수점버림


                    //? 확장 값이 짝수
                    if (expandValue.IsEven())
                    {
                        //exnandHalf는, 소수점이 버려지지 않고 잘려진다

                        //! 우선 짝수확장만 생각해보자
                        //! 짝수니까 expandValue를 나눌떄 소수점이 버려지지 않는다
                        if (expandPositive)
                        {
                            start = center - (expandHalf - 1);
                            end = center + expandHalf;
                        }


                        else
                        {
                            start = center - expandHalf;
                            end = center + (expandHalf - 1);
                        }
                    }


                    else
                    {
                        int temp = (expandValue - 1) / 2;

                        //expandHalf는, 소수점이 버려진채 잘려있다
                        //expandValue에 중심점을 빼기위해 -1을 하면, 짝수로 딱딱 잘리므로, expandPositive는 필요없다
                        start = center - temp;
                        end = center + temp;
                    }
                }
            }



            public delegate bool GridExpandPositionsEvent(int x, int y);



            /// <summary>
            /// 좌표값의 중심으로 부터 너비/높이로 확장하여, 그 범위안에 있는 그리드들에게 이벤트를 실행한다<br/>
            /// (<see cref="GetExpandGridPositionsFromGridPosition_ForLoop"/> 활용)
            /// </summary>
            /// <param name="gridPosition">중심 좌표</param>
            /// <param name="height">높이, 1 초과일경우, 중심점을 기준으로 확장됨</param>
            /// <param name="height">중심을 기준으로 확장할 높이</param>
            /// <param name="expandPositiveHorizontal">가로 방향을 양수로 확장할지 여부</param>
            /// <param name="expandPositiveVertical">세로 방향을 양수로 확장할지 여부</param>
            public static void GridExpandPositionsEvent_GridPosition(Vector2Int gridPosition, int width, int height, bool expandPositiveHorizontal, bool expandPositiveVertical, GridExpandPositionsEvent action)
            {
                GetExpandGridPositionsFromGridPosition_ForLoop(gridPosition, width, height, expandPositiveHorizontal, expandPositiveVertical, out int startX, out int endX, out int startY, out int endY);

                // 2중반복문으로 각각 GridEvent를 실행한다,
                for (int x = startX; x <= endX; x++)
                {
                    for (int y = startY; y <= endY; y++)
                    {
                        action?.Invoke(x, y);
                    }
                }
            }



            ///======================================================================================================================================================



            //? 그리드  Static 커스텀



            ///<summary>
            ///그리드가 <see cref="TAG_ROOM_SAFE_DOWN"/> 외의 다른 방향의 안전구역은 가지고 있지 않을 경우 true
            /// </summary>
            public static bool CheckTag_OnlyRoomSafeDown(Grid grid)
            {
                return grid.ContainsTag(GridTag.Room_Safe_Down) && !(grid.AnyTag(GridTag.Room_Safe_Left | GridTag.Room_Safe_Right));
            }



            ///<summary>
            ///그리드가 <see cref="TAG_ROOM_SAFE_UP"/> 외의 다른 방향의 안전구역은 가지고 있지 않을 경우 true
            /// </summary>
            public static bool CheckTag_OnlyRoomSafeUp(Grid grid)
            {
                return grid.ContainsTag(GridTag.Room_Safe_Up) && !(grid.AnyTag(GridTag.Room_Safe_Left | GridTag.Room_Safe_Right));
            }



            ///<summary>
            ///그리드가 <see cref="TAG_ROOM_SAFE_LEFT"/> 외의 다른 방향의 안전구역은 가지고 있지 않을 경우 true
            /// </summary>
            public static bool CheckTag_OnlyRoomSafeLeft(Grid grid)
            {
                return grid.ContainsTag(GridTag.Room_Safe_Left) && !(grid.AnyTag(GridTag.Room_Safe_Down | GridTag.Room_Safe_Up));
            }



            ///<summary>
            ///그리드가 <see cref="TAG_ROOM_SAFE_RIGHT"/> 외의 다른 방향의 안전구역은 가지고 있지 않을 경우 true
            /// </summary>
            public static bool CheckTag_OnlyRoomSafeRight(Grid grid)
            {
                return grid.ContainsTag(GridTag.Room_Safe_Right) && !(grid.AnyTag(GridTag.Room_Safe_Down | GridTag.Room_Safe_Up));
            }



            ///<summary>
            ///그리드가 단일 방향의 안전구역만 가지고 있을 경우 true
            /// </summary>
            public static bool CheckTag_RoomSafe_SingleDirection(Grid grid)
            {
                bool onlyDown = CheckTag_OnlyRoomSafeDown(grid);
                bool onlyUp = CheckTag_OnlyRoomSafeUp(grid);
                bool onlyLeft = CheckTag_OnlyRoomSafeLeft(grid);
                bool onlyRight = CheckTag_OnlyRoomSafeRight(grid);


                return (onlyDown || onlyUp || onlyLeft || onlyRight);
            }



            /// <summary>
            /// 받아온 그리드가 코너인지 확인 (주위에 붙어있는 방향이 정확히 2개이면서 해당 태그를 포함해야 함)
            /// </summary>
            public static bool CheckTag_IsCorner(GridManager gridManager, Grid grid, GridTag conditionTags)
            {
                var gridPosition = grid.GridPositionFixed;


                // 4방향의 유효한 그리드 여부 및 태그 확인
                bool validDown = gridManager.TryGet_GridPosition(gridPosition.x, gridPosition.y - 1, out var gridDown) &&
                                 gridDown.ContainsTag(conditionTags);
                bool validUp = gridManager.TryGet_GridPosition(gridPosition.x, gridPosition.y + 1, out var gridUp) &&
                               gridUp.ContainsTag(conditionTags);
                bool validLeft = gridManager.TryGet_GridPosition(gridPosition.x - 1, gridPosition.y, out var gridLeft) &&
                                 gridLeft.ContainsTag(conditionTags);
                bool validRight = gridManager.TryGet_GridPosition(gridPosition.x + 1, gridPosition.y, out var gridRight) &&
                                  gridRight.ContainsTag(conditionTags);


                // 붙어있는 방향의 개수를 계산
                int validCount = (validDown ? 1 : 0) + (validUp ? 1 : 0) + (validLeft ? 1 : 0) + (validRight ? 1 : 0);


                // 코너 조건: 붙어있는 방향이 정확히 2개여야 함
                if (validCount != 2) return false;


                // 붙어있는 두 방향이 서로 직교(인접)해야 함
                if ((validDown && validLeft) || (validDown && validRight) ||
                    (validUp && validLeft) || (validUp && validRight))
                {
                    return true; // 코너 조건 만족
                }


                return false; // 직교하지 않으면 코너가 아님
            }



            /// <summary>
            /// 고정 그리드 이벤트: 순수한 복도 관련 태그 일괄 제거 (보정거리 복도는 제거하지 않음)
            /// </summary>
            /// <param name="grid"></param>
            public static void GridEvent_RemoveHallwayTags(Grid grid)
            {
                if (!grid.AnyTag(GridTag.Room_Door_Area_StartEnd | GridTag.Hallway_Edge_DoorCorrection | GridTag.Hallway_Safe_DoorCorrection))
                {
                    grid.RemoveTag(GRIDTAGS_Hallways);
                }
            }


            /// <summary>
            /// 고정 그리드 이벤트: 도어 보정 복도가 아니라면,복도 관련 태그 일괄 제거
            /// </summary>
            /// <param name="grid"></param>
            public static void GridEvent_RemoveHallwayCorrectionTags(Grid grid)
            {
                if (grid.AnyTag(GridTag.Room_Door_Area_StartEnd | GridTag.Hallway_Edge_DoorCorrection | GridTag.Hallway_Safe_DoorCorrection))
                {
                    grid.RemoveTag(GRIDTAGS_HallwayCorrections);
                }
            }



            ///======================================================================================================================================================



            //? 전체 그리드 초기화



            private static readonly Action<Grid> ResetGridEvent = (grid) =>
            {
                grid.ResetGridSettings();
            };



            /// <summary>
            /// 모든 그리드들의 그리드 설정을 초기화한다
            /// </summary>
            public void GridsEvent_ResetAllGrids()
            {
                GridsEvent_All(ResetGridEvent);
            }



            ///======================================================================================================================================================



            //? Grid 이벤트 : 그리드 경계를 두르는 그리드들의 이벤트



            /// <summary>
            /// <paramref name="grids"/>의 경계 부분에 이벤트를 실행
            /// </summary>
            /// <param name="grids">그리드 배열</param>
            /// <param name="borderWidth">적용할 경계 너비</param>
            /// <param name="borderHeight">적용할 경계 높이</param>
            /// <param name="resultGrids"><paramref name="gridEvent"/>실행에 성공하면 해당 그리드가 추가된다 (null이 아니라면!)</param>
            [Obsolete("테스트 필요")]
            public bool GridsEvent_GridBorder(IEnumerable<Grid> grids, int borderWidth, int borderHeight, Func<GridManager, Grid, bool> gridEvent, in ICollection<Grid> resultGrids = null)
            {
                if (gridEvent == null) { return false; }

                foreach (var grid in grids)
                {
                    GridEvent_GridBorder(grid, borderWidth, borderHeight, gridEvent, resultGrids);
                }

                return true;
            }



            /// <summary>
            /// <paramref name="grid"/>의 경계 부분에 이벤트를 실행
            /// </summary>
            /// <param name="grid">작업할 그리드 객체</param>
            /// <param name="borderWidth">적용할 경계 너비</param>
            /// <param name="borderHeight">적용할 경계 높이</param>
            /// <param name="resultGrids"><paramref name="gridEvent"/>실행에 성공하면 해당 그리드가 추가된다 (null이 아니라면!)</param>
            [Obsolete("테스트 필요")]
            public void GridEvent_GridBorder(Grid grid, int borderWidth, int borderHeight, Func<GridManager, Grid, bool> gridEvent, in ICollection<Grid> resultGrids = null)
            {
                //? 그리드 경계 내의 시작 및 끝 X, Y 좌표 계산
                int startX = grid.GridPositionFixed.x - borderWidth;
                int endX = grid.GridPositionFixed.x + borderWidth;
                int startY = grid.GridPositionFixed.y - borderHeight;
                int endY = grid.GridPositionFixed.y + borderHeight;



                for (int x = startX; x <= endX; x++)
                {
                    for (int y = startY; y <= endY; y++)
                    {
                        //? 유효하지 않은 그리드 위치는 건너뛰기
                        if (!IsValid_GridPosition(x, y)) { continue; }

                        Grid currentGrid = Grids[x, y];

                        if (gridEvent.Invoke(this, currentGrid) && resultGrids != null)
                        {
                            resultGrids.Add(currentGrid);
                        }
                    }
                }
            }



            ///======================================================================================================================================================



            //? [미사용} 이전 복도생성 코드 (Legacy_GridsEvent_VectorsRange)



            #region 이전 복도생성 코드

            [Obsolete]
            private bool Legacy_GridsEvent_VectorsRange(IEnumerable<Vector2Int> vectors, int expandWidth, int expandHeight, bool useDoubleCenter, bool expandPositive, Func<Grid, bool> gridConditionEvent, in ICollection<Grid> resultGrids)
            {
                if (gridConditionEvent == null) { return false; }

                foreach (var path in vectors)
                {
                    Legacy_GridEvent_VectorsRange(path, expandWidth, expandHeight, useDoubleCenter, expandPositive, gridConditionEvent, resultGrids);
                }

                return true;
            }



            [Obsolete]
            private void Legacy_GridEvent_VectorsRange(Vector2Int vector, int expandWidth, int expandHeight, bool isDoubleCenter, bool expandPositive, Func<Grid, bool> gridConditionEvent, in ICollection<Grid> resultGrids)
            {
                //? 너비와 높이가 모두 1일 때는, 확장할 필요가 없으니 유효성 검사만 하고 즉시 이벤트를 실행한뒤 return
                if (expandWidth == 1 && expandHeight == 1)
                {
                    if (TryGet_GridPosition(vector, out var grid) && gridConditionEvent.Invoke(grid))
                    {
                        resultGrids.Add(grid);
                    }

                    return;
                }



                //? 반복문 사용을 위한 그리드 좌표의 시작점~끝점을 X,Y로 나눠 선언
                //! 이 값이 모두 포함된 좌표로 실행된다 (EX: startX:0, endX: 2 = 0, 1, 2)
                int startX, endX, startY, endY;



                int half_Width = expandWidth / 2;
                int half_Height = expandHeight / 2;

                bool isWidth_IsOdd = expandWidth % 2 == 1;
                bool isHeight_IsOdd = expandHeight % 2 == 1;


                int half_Width_Minused = half_Width == 0 ? 0 : half_Width - 1;
                int half_Width_Plused = half_Width == 0 ? 0 : half_Width + 1;

                int half_Height_Minused = half_Height == 0 ? 0 : half_Height - 1;
                int half_Height_Plused = half_Height == 0 ? 0 : half_Height + 1;


                //? 홀수전용 반쪽자리
                int oddHalf_Width = (expandWidth - 1) / 2;
                int oddHalf_Height = (expandHeight - 1) / 2;



                if (!isWidth_IsOdd)
                {
                    //? 짝수: 오른쪽 우선
                    if (expandPositive)
                    {
                        startX = vector.x - half_Width_Minused;
                        endX = vector.x + half_Width;
                    }

                    //? 짝수: 왼쪽 우선
                    else
                    {
                        startX = vector.x - half_Width;
                        endX = vector.x + half_Width_Minused;
                    }

                }
                else
                {
                    if (isDoubleCenter)
                    {
                        //? 홀수: 오른쪽 우선
                        if (expandPositive)
                        {
                            startX = vector.x - half_Width_Minused;
                            endX = vector.x + half_Width_Plused;
                        }
                        //? 홀수: 왼쪽 우선
                        else
                        {
                            startX = vector.x - half_Width_Plused;
                            endX = vector.x + half_Width_Minused;
                        }
                    }
                    else
                    {
                        startX = vector.x - oddHalf_Width;
                        endX = vector.x + oddHalf_Width;
                    }
                }



                if (!isHeight_IsOdd)
                {
                    //? 짝수: 위쪽 우선
                    if (expandPositive)
                    {
                        startY = vector.y - half_Height_Minused;
                        endY = vector.y + half_Height;
                    }

                    //? 짝수: 아래쪽 우선
                    else
                    {
                        startY = vector.y - half_Height;
                        endY = vector.y + half_Height_Minused;
                    }

                }
                else
                {
                    if (isDoubleCenter)
                    {
                        //? 홀수: 위쪽 우선
                        if (expandPositive)
                        {
                            startY = vector.y - half_Height_Minused;
                            endY = vector.y + half_Height_Plused;
                        }
                        //? 홀수: 아래쪽 우선
                        else
                        {
                            startY = vector.y - half_Height_Plused;
                            endY = vector.y + half_Height_Minused;
                        }
                    }
                    else
                    {
                        startY = vector.y - oddHalf_Height;
                        endY = vector.y + oddHalf_Height;
                    }
                }



                //Debug.LogWarning($"{isDoubleCenter} 복도 적용 범위 {vector}, width: {expandWidth} | {half_Width}, height: {expandHeight} | {half_Height} ||| x: [{vector.x}] : {startX}~{endX} , y: [{vector.y}] : {startY}~{endY}");



                for (int x = startX; x < endX + 1; x++)
                {
                    for (int y = startY; y < endY + 1; y++)
                    {
                        if (TryGet_GridPosition(x, y, out var grid) && gridConditionEvent.Invoke(grid))
                        {
                            //Debug.LogWarning($"그리드 적용: {gridPosition}");
                            resultGrids.Add(grid);
                        }
                    }
                }
            }

            #endregion



            //? [미사용] 복도 그룹화 



            #region 복도 그룹화 코드



            //[Obsolete]
            //private List<Rect> StageGridGroup_HallwayRects = new List<Rect>();
            //[Obsolete]
            //private bool[,] StageGridGroup_Visited;


            //[Obsolete]
            //private void StageGridGroup_GenerateHallwayRects()
            //{
            //    StageGridGroup_HallwayRects.Clear();

            //    int width = GridArray.GetLength(0);
            //    int height = GridArray.GetLength(1);
            //    StageGridGroup_Visited = new bool[width, height];

            //    for (int x = 0; x < width; x++)
            //    {
            //        for (int y = 0; y < height; y++)
            //        {
            //            if (StageGridGroup_IsHallwayGrid(x, y) && !StageGridGroup_Visited[x, y])
            //            {
            //                StageGridGroup_FindAndAddHallwayRect(x, y, width, height);
            //            }
            //        }
            //    }

            //    // 여기서 HallwayRects에는 꽉 찬 사각형들의 리스트가 포함됩니다.
            //    // 이 사각형들을 사용하여 복도 오브젝트를 생성할 수 있습니다.
            //}

            //[Obsolete]
            //private bool StageGridGroup_IsHallwayGrid(int x, int y)
            //{
            //    // StageGridArray[x, y]가 복도를 나타내는지 확인하는 로직
            //    // 예시를 위해 단순화됨
            //    return GridArray[x, y].ContainsInfo(Grid.Info.Hallway);
            //}

            //[Obsolete]
            //private void StageGridGroup_FindAndAddHallwayRect(int startX, int startY, int stageWidth, int stageHeight)
            //{
            //    if (StageGridGroup_Visited[startX, startY]) return; // 이미 방문한 시작점은 건너뛴다

            //    int maxWidth = StageGridGroup_FindMaxWidth(startX, startY, stageWidth);
            //    int maxHeight = StageGridGroup_FindMaxHeight(startX, startY, maxWidth, stageHeight);

            //    // 사각형을 리스트에 추가
            //    StageGridGroup_HallwayRects.Add(new Rect(startX, startY, maxWidth, maxHeight));

            //    // 방문한 그리드 마킹
            //    for (int x = startX; x < startX + maxWidth; x++)
            //    {
            //        for (int y = startY; y < startY + maxHeight; y++)
            //        {
            //            StageGridGroup_Visited[x, y] = true;
            //        }
            //    }
            //}

            //[Obsolete]
            //private int StageGridGroup_FindMaxWidth(int startX, int startY, int stageWidth)
            //{
            //    int maxWidth = 0;
            //    while (startX + maxWidth < stageWidth && StageGridGroup_IsHallwayGrid(startX + maxWidth, startY))
            //    {
            //        maxWidth++;
            //    }
            //    return maxWidth;
            //}

            //[Obsolete]
            //private int StageGridGroup_FindMaxHeight(int startX, int startY, int maxWidth, int stageHeight)
            //{
            //    int maxHeight = 0;
            //    bool columnValid = true;
            //    while (startY + maxHeight < stageHeight && columnValid)
            //    {
            //        for (int x = startX; x < startX + maxWidth; x++)
            //        {
            //            if (!StageGridGroup_IsHallwayGrid(x, startY + maxHeight))
            //            {
            //                columnValid = false;
            //                break;
            //            }
            //        }
            //        if (columnValid) maxHeight++;
            //    }
            //    return maxHeight;
            //}



            #endregion



            ///======================================================================================================================================================
        }
    }
}