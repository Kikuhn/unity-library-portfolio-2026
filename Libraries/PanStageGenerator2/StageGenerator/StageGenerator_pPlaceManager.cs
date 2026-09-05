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
using Sirenix.Serialization;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine.Jobs;
using Unity.Jobs;
using Unity.Burst;



namespace Pan.StageGenerators
{
    public partial class StageGenerator
    {
        [Serializable]
        public partial class PlaceManger : BaseManager
        {
            ///======================================================================================================================================================



            #region 내부 클래스



            ///<summary>
            /// 생성된 오브젝트 리스트, 그 오브젝트가 차지하는 그리드 리스트를 저장하는 클래스<br/>
            /// 단독 사용 불가능, <see cref="PlaceObjectList"/> 또는 <see cref="PlaceObjectList{TObject}"/>를 사용하자
            /// </summary>
            [Serializable]
            public abstract class BasePlaceObjectList<TObject> where TObject : UnityEngine.Object
            {
                ///======================================================================================================================================================



                public BasePlaceObjectList()
                {
                    summonedObjectList = new List<TObject>();
                    placedGrids = new HashSet<Grid>();
                }



                public BasePlaceObjectList(int capacity_ObjectList, int capacity_GridList)
                {
                    summonedObjectList = new List<TObject>(capacity_ObjectList);
                    placedGrids = new HashSet<Grid>(capacity_GridList);
                }



                ///======================================================================================================================================================



                ///<summary>
                ///생성된 오브젝트들이 저장되는 리스트
                /// </summary>
                [ListDrawerSettings(DefaultExpandedState = false, HideAddButton = true, HideRemoveButton = true, DraggableItems = false)]
                [SerializeField]
                [LabelText("생성된 오브젝트 목록")]
                private List<TObject> summonedObjectList = new List<TObject>();

                ///<summary>
                ///생성된 오브젝트들이 저장되는 리스트
                /// </summary>
                public IReadOnlyList<TObject> SummonedObjectList => summonedObjectList;



                ///<summary>
                ///설정된 그리드들이 저장되는 리스트
                /// </summary>
                [ListDrawerSettings(DefaultExpandedState = false, HideAddButton = true, HideRemoveButton = true, DraggableItems = false)]
                [ShowInInspector]
                [LabelText("배치된 그리드 목록")]
                private HashSet<Grid> placedGrids = new HashSet<Grid>();

                ///<summary>
                ///설정된 그리드들이 저장되는 리스트
                /// </summary>
                public IReadOnlyCollection<Grid> PlacedGrids => placedGrids;

                ///<summary>
                ///설정된 그리드들이 저장되는 리스트
                /// </summary>
                public HashSet<Grid> GetPlacedGrids => placedGrids;



                ///======================================================================================================================================================



                ///<summary>
                ///생성된 오브젝트 추가
                /// </summary>
                public void AddSummonedObject(TObject value)
                {
                    summonedObjectList.Add(value);
                }



                ///<summary>
                /// 초기화
                /// </summary>
                /// <param name="destroyBeforeClear">리스트에서 제거하기 전, 오브젝트 파괴 여부</param>
                public void Clear(StageGenerator main, bool destroyBeforeClear)
                {
                    if (destroyBeforeClear) { main.GenerateM.DestroyObjects(summonedObjectList); }

                    summonedObjectList.Clear();
                    placedGrids.Clear();
                }



                ///<summary>
                /// [비동기] 초기화 (무조건 오브젝트 까지 파괴)
                /// </summary>
                public async UniTask ClearDestroyAsync(StageGenerator main)
                {
                    await main.GenerateM.DestroyObjectsAsync(summonedObjectList);

                    summonedObjectList.Clear();
                    placedGrids.Clear();
                }



                ///======================================================================================================================================================
            }



            [Serializable]
            public class PlaceObjectList : BasePlaceObjectList<GameObject> { }



            [Serializable]
            public class PlaceObjectList<TObject> : BasePlaceObjectList<TObject> where TObject : MonoBehaviour { }




            private static class Util
            {
                /// <summary>
                /// <b>중심점 좌표(그리드) 2개를 비교하여, 중심점 2가 중심점1의 우or상 에 존재하는지 (True), 좌or하 에 존재하는지 (False) 확인하는 메서드</b><br/>
                /// <paramref name="center2"/>가 <paramref name="center1"/> 보다 양수(우측 or 상단)에 위치해있다면 True를 반환한다<br/>
                /// (<paramref name="center1"/>와 <paramref name="center2"/>가 같다면, False를 반환)
                /// </summary>
                /// <param name="equalHorizontal">
                /// 두 중심점들의 X값이 같아서, Y축을 비교해야한다면 True<br/>
                /// 두 중심점들의 Y값이 같아서, X축을 비교해야한다면 False
                /// </param>
                /// <param name="center1">중심점1</param>
                /// <param name="center2">중심점2</param>
                public static bool EqualCenters_CheckPositivePosition(bool equalHorizontal, Vector2Int center1, Vector2Int center2)
                {
                    if (equalHorizontal)
                    {
                        if (center1.y < center2.y) { return true; }
                        else { return false; }
                    }
                    else
                    {
                        if (center1.x < center2.x) { return true; }
                        else { return false; }
                    }
                }



                /// <summary>
                /// <b>두 중심점 중에서 지정된 조건에 따라 적절한 중심점을 반환하는 메서드</b><br/>
                /// 중심점들 간의 비교를 통해 우or상에 위치한 중심점을 반환하거나, 좌or하에 위치한 중심점을 반환한다.<br/>
                /// <paramref name="negative"/>가 true이면 반대의 중심점을 반환한다.
                /// </summary>
                /// <param name="equalHorizontal">
                /// 두 중심점들의 X값이 같아서, Y축을 비교해야 한다면 True<br/>
                /// 두 중심점들의 Y값이 같아서, X축을 비교해야 한다면 False
                /// </param>
                /// <param name="center1">중심점1</param>
                /// <param name="center2">중심점2</param>
                /// <param name="negative">
                /// 반대의 중심점을 반환해야 한다면 true<br/>
                /// 지정된 조건에 따라 적절한 중심점을 반환해야 한다면 false
                /// </param>
                /// <returns>조건에 따라 선택된 중심점 (<paramref name="center1"/>> 아니면 <paramref name="center2"/>)</returns>
                public static Vector2Int GetCenter_PositivePositions(bool equalHorizontal, Vector2Int center1, Vector2Int center2, bool negative = false)
                {
                    if (EqualCenters_CheckPositivePosition(equalHorizontal, center1, center2))
                    {
                        return !negative ? center2 : center1;
                    }
                    return !negative ? center1 : center2;
                }
            }



            #endregion



            ///======================================================================================================================================================



            public override void WakeUp(StageGenerator main)
            {
                base.WakeUp(main);
                PlacedRoomTFInfoCacheM.WakeUp(main);
            }



            ///======================================================================================================================================================



#if UNITY_EDITOR

            [TitleGroup("배치 매니저"), BoxGroup("배치 매니저/박스", false)]
            [ShowInInspector, HideLabel, DisplayAsString(EnableRichText = true, Overflow = false), EnableGUI]
            [PropertyOrder(-99)]
            private string dummy_PlacedInfos1
            {
                get
                {
                    return $"Room: <color=#2ecc71><b>{placeObjects_Room.SummonedObjectList.Count}</b></color> | Grid: <color=#ac92ec><b>{placeObjects_Room.PlacedGrids.Count}</b></color>\n" +
                        $"Hallway: <color=#2ecc71><b>{placeObjects_Hallway.SummonedObjectList.Count}</b></color> | Grid: <color=#ac92ec><b>{placeObjects_Hallway.PlacedGrids.Count}</b></color>\n" +
                        $"HallwayEdge: <color=#2ecc71><b>{placeObjects_HallwayEdge.SummonedObjectList.Count}</b></color> | Grid: <color=#ac92ec><b>{placeObjects_HallwayEdge.PlacedGrids.Count}</b></color>\n" +
                        $"ETC: <color=#2ecc71><b>{placeObjects_ETC.SummonedObjectList.Count}</b></color> | Grid: <color=#ac92ec><b>{placeObjects_ETC.PlacedGrids.Count}</b></color>\n" +
                        $"Custom Grid Expand: <color=#ac92ec><b>{grids_CustomExpand.Count}</b></color>";
                }
            }


#endif



            //? 생성 & 배치된 오브젝트 & 그리드들 관리



            public PlaceObjectList<RoomObject> PlaceObjects_Room
            {
                get => placeObjects_Room;
            }
            [TitleGroup("배치 매니저"), BoxGroup("배치 매니저/박스", false)]
            [FoldoutGroup("배치 매니저/박스/생성 및 배치 정보")]
            [LabelText("생성&배치된 Room")]
            [SerializeField]
            private PlaceObjectList<RoomObject> placeObjects_Room = new();



            public PlaceObjectList PlaceObjects_Hallway
            {
                get => placeObjects_Hallway;
            }
            [TitleGroup("배치 매니저"), BoxGroup("배치 매니저/박스", false)]
            [FoldoutGroup("배치 매니저/박스/생성 및 배치 정보")]
            [LabelText("생성&배치된 Hallway")]
            [SerializeField]
            private PlaceObjectList placeObjects_Hallway = new();



            public PlaceObjectList PlaceObjects_HallwayEdge
            {
                get => placeObjects_HallwayEdge;
            }
            [TitleGroup("배치 매니저"), BoxGroup("배치 매니저/박스", false)]
            [FoldoutGroup("배치 매니저/박스/생성 및 배치 정보")]
            [LabelText("생성&배치된 HallwayEdge")]
            [SerializeField]
            private PlaceObjectList placeObjects_HallwayEdge = new();



            public PlaceObjectList PlaceObjects_ETC
            {
                get => placeObjects_ETC;
            }
            [TitleGroup("배치 매니저"), BoxGroup("배치 매니저/박스", false)]
            [FoldoutGroup("배치 매니저/박스/생성 및 배치 정보")]
            [LabelText("생성&배치된 ETC")]
            [SerializeField]
            private PlaceObjectList placeObjects_ETC = new();



            //? 소환된 오브젝트 없이 그리드만 수정되는 커스텀 확장 리스트



            ///<summary>
            /// 그리드 리스트 : 커스텀 확장
            /// </summary>
            public HashSet<Grid> GetGrids_CustomExpand => grids_CustomExpand;

            ///<summary>
            /// 그리드 리스트 : 커스텀 확장
            /// </summary>
            public IReadOnlyCollection<Grid> Grids_CustomExpand => grids_CustomExpand;

            [TitleGroup("배치 매니저"), BoxGroup("배치 매니저/박스", false)]
            [FoldoutGroup("배치 매니저/박스/생성 및 배치 정보")]
            [LabelText("커스텀 확장 그리드")]
            [PropertyTooltip("안전구역과 같은 대응되는 오브젝트는 존재하지않지만, 그리드 상에는 존재하는 것들이 배치됨")]
            [ShowInInspector]
            private HashSet<Grid> grids_CustomExpand = new HashSet<Grid>();



            /// <summary>
            /// 배치된 모든 그리드들의 정보를 초기화한다<br/>
            /// (생성된 오브젝트는 방치)
            /// </summary>
            public void ClearAll_PlacesGridInfo()
            {
                placeObjects_Room.GetPlacedGrids.Clear();
                placeObjects_Hallway.GetPlacedGrids.Clear();
                placeObjects_HallwayEdge.GetPlacedGrids.Clear();
                placeObjects_ETC.GetPlacedGrids.Clear();
                grids_CustomExpand.Clear();
            }



            ///======================================================================================================================================================



            //? 기록 정보 (매 생성마다 초기화)



            ///<summary>
            ///복도 재구성이 실행된 횟수
            ///</summary>
            [TitleGroup("배치 매니저"), BoxGroup("배치 매니저/박스", false)]
            [BoxGroup("배치 매니저/박스/기록")]
            [LabelText("복도 재구성 횟수")]
            [DisplayAsString]
            public int Reconstruction_ConnectHallways_ReportCount = 0;



            /// <summary>
            /// 1차 인접 (단방향 단일 반대축 연산)으로 순회한 횟수
            /// </summary>
            [TitleGroup("배치 매니저"), BoxGroup("배치 매니저/박스", false)]
            [BoxGroup("배치 매니저/박스/기록")]
            [LabelText("1차 인접 순회 횟수")]
            [PropertyTooltip("단방향 단일 반대축 연산으로 순회한 횟수")]
            [DisplayAsString]
            public int MoveNearestRooms_First_ReportCount = 0;



            /// <summary>
            /// 2차 인접이 순회한 횟수
            /// </summary>
            [TitleGroup("배치 매니저"), BoxGroup("배치 매니저/박스", false)]
            [BoxGroup("배치 매니저/박스/기록")]
            [LabelText("2차 인접 순회 횟수")]
            [DisplayAsString]
            public int MoveNearestRooms_Second_ReportCount = 0;



            ///<summary>
            ///  경로 또는 복도의 실패 디버깅 메시지 출력 여부<br/>
            ///  (복도 재구성을 하거나, 크로노브레이크 등으로 다시 정상적으로 생성될 가능성이 있다면, 굳이 에러 디버깅 메시지를 유니티에서 출력할 필요가 없기 때문)<br/>
            /// </summary>
            [field: NonSerialized] public bool EnableDebugLog_FailedPathFindingOrHallway { get; private set; } = false;



            ///======================================================================================================================================================



            //? 배치된 방 캐시 매니저



            [Serializable]
            public class PlacedRoomTransformInfoCacheManager : MainSlaveSerialized_WakeUpVer<StageGenerator>
            {
                ///======================================================================================================================================================



                /// <summary>
                /// 방을 배치 한 이후에, 추가로 좌표 이동하려고 할때 그 정보를 각기 저장하는 구조체
                /// </summary>
                public struct RoomActivatedRectExpandWithPlaceVelocity
                {
                    public RoomActivatedRectExpandWithPlaceVelocity(CustomRect2DCentered rect, Vector2 velocity)
                    {
                        RoomActivatedTransformRectExpand = rect;
                        RoomPlaceVelocity = velocity;
                    }

                    public CustomRect2DCentered RoomActivatedTransformRectExpand;
                    public Vector2 RoomPlaceVelocity;

                }



                /// <summary>
                /// 방을 배치 한 이후에, 좌표이동을 하기 위해 방의 Transform 정보와 이동할 Velocity를 캐싱,관리하는 딕셔너리
                /// </summary>
                [ShowInInspector]
                [LabelText("배치된 방 트랜스폼 캐시")]
                private Dictionary<RoomObject, RoomActivatedRectExpandWithPlaceVelocity> _placedRoomTransformInfoCache;

                private Dictionary<RoomObject, RoomActivatedRectExpandWithPlaceVelocity> placedRoomTransformInfoCache
                {
                    get
                    {
                        _placedRoomTransformInfoCache ??= new Dictionary<RoomObject, RoomActivatedRectExpandWithPlaceVelocity>();
                        return _placedRoomTransformInfoCache;
                    }
                }



                ///======================================================================================================================================================



                /// <summary>
                /// 배치된 방의 트랜스폼 정보를 캐싱한다
                /// </summary>
                /// <param name="placedRoom"></param>
                /// <param name="activatedRoomRectExpand"></param>
                public void Add_PlacedRoomTransformInfoCache(RoomObject placedRoom, CustomRect2DCentered activatedRoomRectExpand)
                {
                    placedRoomTransformInfoCache.Add(placedRoom, new RoomActivatedRectExpandWithPlaceVelocity(activatedRoomRectExpand, Vector2.zero));
                }




                /// <summary>
                /// 배치된 방 트랜스폼 정보 초기화 하기
                /// </summary>
                public void Clear_PlacedRoomTransformInfoCache(bool clearAndNull = false)
                {
                    placedRoomTransformInfoCache.Clear();
                    if (clearAndNull)
                    {
                        _placedRoomTransformInfoCache = null;
                    }
                }



                /// <summary>
                /// 배치된 방의 ActivatedRectExpand를 캐시에서 얻기 (실패시, 책임지지않음)
                /// </summary>
                public CustomRect2DCentered GetRoomActivatedRectExpand_byCache(RoomObject placedRoom)
                {
                    return placedRoomTransformInfoCache[placedRoom].RoomActivatedTransformRectExpand;
                }



                /// <summary>
                /// 배치된 방의 Velocity 값을 연산하여 더한다  
                /// <para>스테이지 경계를 벗어나지 않게 1차 클램프 후,  
                /// 다른 방(Rect)과 겹치지 않도록 축 단위(X → Y)로 이동량을 제한한다.</para>
                /// </summary>
                public Vector2 AddVelocity_byPlacedRoomTransformInfoCache(
                    IReadOnlyList<Space> spaceList,
                    RoomObject placedRoom,
                    Vector2 velocity,
                    Rect clampTransformRect
                )
                {
                    //! 캐싱된 정보가 없으면 즉시 실패
                    if (!placedRoomTransformInfoCache.TryGetValue(placedRoom, out var info))
                        return Vector2.zero;

                    var rectTF = info.RoomActivatedTransformRectExpand;
                    var center = rectTF.CenterWithOffset;
                    var half = rectTF.Size * 0.5f;

                    //? #1 스테이지 경계 1차 클램프 (반폭/반높이 고려)
                    Vector2 target = center + velocity;
                    target.x = Mathf.Clamp(target.x, clampTransformRect.xMin + half.x, clampTransformRect.xMax - half.x);
                    target.y = Mathf.Clamp(target.y, clampTransformRect.yMin + half.y, clampTransformRect.yMax - half.y);


                    Vector2 vStage = target - center;
                    if (vStage == Vector2.zero) return Vector2.zero;

                    //? #2 축 단위 스윕 충돌 검사 (X → 반영 → Y 순서)
                    float dx = vStage.x;
                    float dy = vStage.y;

                    //. X축 이동
                    if (!Mathf.Approximately(dx, 0f))
                    {
                        float cur_xMin = rectTF.xMin, cur_xMax = rectTF.xMax;
                        float cur_yMin = rectTF.yMin, cur_yMax = rectTF.yMax;

                        for (int i = 0; i < spaceList.Count; i++)
                        {
                            var s = spaceList[i];
                            if (s.PlacedRoom == null || s.PlacedRoom == placedRoom) continue;

                            var other = GetRoomActivatedRectExpand_byCache(s.PlacedRoom);

                            //. Y축에서 겹칠 때만 충돌 후보(모서리만 닿는 건 허용)
                            bool overlapY = (cur_yMin < other.yMax - Mathf.Epsilon) && (cur_yMax > other.yMin + Mathf.Epsilon);
                            if (!overlapY) continue;

                            if (dx > 0f)
                            {
                                //? 오른쪽(+X)으로 전진할 때는 "앞쪽"에 있는 것만 고려 (현재 A의 오른변 ≤ B의 왼변)
                                if (cur_xMax <= other.xMin + Mathf.Epsilon)
                                {
                                    float gap = other.xMin - cur_xMax;
                                    if (gap < dx) dx = Mathf.Min(dx, Mathf.Max(0f, gap));
                                }
                                //. 뒤쪽(또는 이미 겹쳐있는) 대상은 이번 전진에 영향 없음
                            }
                            else // dx < 0
                            {
                                //? 왼쪽(−X)으로 전진할 때는 "앞쪽"에 있는 것만 고려 (현재 A의 왼변 ≥ B의 오른변)
                                if (cur_xMin >= other.xMax - Mathf.Epsilon)
                                {
                                    float gap = other.xMax - cur_xMin; // 음수/0 가능
                                    if (gap > dx) dx = Mathf.Max(dx, Mathf.Min(0f, gap));
                                }
                                //. 뒤쪽(또는 이미 겹쳐있는) 대상은 이번 전진에 영향 없음
                            }
                        }

                        if (!Mathf.Approximately(dx, 0f))
                            rectTF.Center += new Vector2(dx, 0f);
                    }

                    //. Y축 이동
                    if (!Mathf.Approximately(dy, 0f))
                    {
                        float cur_xMin = rectTF.xMin, cur_xMax = rectTF.xMax;
                        float cur_yMin = rectTF.yMin, cur_yMax = rectTF.yMax;

                        for (int i = 0; i < spaceList.Count; i++)
                        {
                            var s = spaceList[i];
                            if (s.PlacedRoom == null || s.PlacedRoom == placedRoom) continue;

                            var other = GetRoomActivatedRectExpand_byCache(s.PlacedRoom);

                            //. X축에서 겹칠 때만 충돌 후보(모서리만 닿는 건 허용)
                            bool overlapX = (cur_xMin < other.xMax - Mathf.Epsilon) && (cur_xMax > other.xMin + Mathf.Epsilon);
                            if (!overlapX) continue;

                            if (dy > 0f)
                            {
                                //? 위(+Y)로 전진할 때는 "앞쪽"에 있는 것만 고려 (현재 A의 윗변 ≤ B의 아랫변)
                                if (cur_yMax <= other.yMin + Mathf.Epsilon)
                                {
                                    float gap = other.yMin - cur_yMax;
                                    if (gap < dy) dy = Mathf.Min(dy, Mathf.Max(0f, gap));
                                }
                            }
                            else // dy < 0
                            {
                                //? 아래(−Y)로 전진할 때는 "앞쪽"에 있는 것만 고려 (현재 A의 아랫변 ≥ B의 윗변)
                                if (cur_yMin >= other.yMax - Mathf.Epsilon)
                                {
                                    float gap = other.yMax - cur_yMin; // 음수/0 가능
                                    if (gap > dy) dy = Mathf.Max(dy, Mathf.Min(0f, gap));
                                }
                            }
                        }

                        if (!Mathf.Approximately(dy, 0f))
                            rectTF.Center += new Vector2(0f, dy);
                    }

                    //? #3 최종 반영
                    Vector2 vApplied = new Vector2(dx, dy);
                    if (vApplied == Vector2.zero) return Vector2.zero;

                    info.RoomActivatedTransformRectExpand = rectTF;
                    info.RoomPlaceVelocity += vApplied;
                    placedRoomTransformInfoCache[placedRoom] = info;

                    return vApplied;
                }



                ///======================================================================================================================================================




                //? Velocity 적용



                /// <summary>
                /// 캐시된 모든 방의 트랜스폼 정보를 토대로, 방들에게 <see cref="RoomActivatedRectExpandWithPlaceVelocity.RoomPlaceVelocity"/> 를 일괄 적용하기
                /// </summary>
                public void ApplyBatchVelocity_PlacedRoomTransformInfoCache(StageGenerator main, Vector2 plusVelocity)
                {
                    //. 한방에이동
                    foreach (var item in placedRoomTransformInfoCache)
                    {
                        item.Key.transform.position += (item.Value.RoomPlaceVelocity + plusVelocity).SwizzlesVector2To3(main.Setting.SnapSetting.Swizzle);
                    }
                }



                /// <summary>
                /// 각 트랜스폼에 미리 계산된 델타를 더하는 Burst 잡입니다.
                /// </summary>
                [BurstCompile]
                private struct AddPositionJob : IJobParallelForTransform
                {
                    /// <summary>
                    /// 인덱스가 동일한 트랜스폼에 더할 월드 좌표계 델타 값들입니다.
                    /// </summary>
                    [Unity.Collections.ReadOnly] public NativeArray<Vector3> Deltas;

                    /// <inheritdoc />
                    public void Execute(int index, TransformAccess transform)
                    {
                        transform.position += Deltas[index];
                    }
                }



                /// <summary>
                /// <c>PlacedRoomTransformInfoCache</c>를 기반으로
                /// <see cref="TransformAccessArray"/>와 이에 매칭되는 <see cref="NativeArray{T}"/> 델타를 생성합니다.
                /// </summary>
                /// <param name="plusVelocity">모든 방에 공통으로 더할 추가 속도(그리드 단위, 스위즐 전).</param>
                /// <param name="taa">생성된 트랜스폼 액세스 배열. 호출 측에서 <c>Dispose</c>해야 합니다.</param>
                /// <param name="deltas">생성된 월드 좌표 델타 배열. 호출 측에서 <c>Dispose</c>해야 합니다.</param>
                /// <returns>생성된 항목 수. 캐시가 비어 있으면 0을 반환합니다.</returns>
                /// <remarks>
                /// 모든 엔트리가 유효한 <see cref="RoomObject"/> 및 <see cref="Transform"/>을 가진다고 가정합니다(널 체크 없음).
                /// 크기가 정확히 <c>Count</c>인 배열만 할당하며, 임시 리스트를 만들지 않습니다.
                /// </remarks>
                int BuildTAAAndDeltas_NoNull(StageGenerator main, Vector2 plusVelocity,
            out TransformAccessArray taa, out NativeArray<Vector3> deltas)
                {
                    int count = placedRoomTransformInfoCache.Count;
                    if (count == 0) { taa = default; deltas = default; return 0; }

                    var swz = main.Setting.SnapSetting.Swizzle;

                    taa = new TransformAccessArray(count);            //? 임시 배열 제거
                    deltas = new NativeArray<Vector3>(count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                    int i = 0;
                    foreach (var kv in placedRoomTransformInfoCache)
                    {
                        var room = kv.Key;
                        var v2 = kv.Value.RoomPlaceVelocity + plusVelocity;

                        taa.Add(room.transform);                         //. 바로 Add
                        deltas[i++] = v2.SwizzlesVector2To3(swz);
                    }
                    return count;
                }



                /// <summary>
                /// <c>PlacedRoomTransformInfoCache</c>에 누적된 이동을 한 번에 적용합니다(동기).
                /// </summary>
                /// <param name="plusVelocity">모든 방에 공통으로 더할 추가 속도(그리드 단위, 스위즐 전).</param>
                /// <param name="useJobs">
                /// <c>true</c>면 C# Job System을 사용해 즉시 완료(<c>Complete</c>)하고,
                /// <c>false</c>면 메인 스레드 루프로 바로 적용합니다.
                /// </param>
                /// <remarks>
                /// 소규모 데이터 또는 잡 미사용 시에는 직접 루프가 더 빠르고 할당도 없습니다.
                /// 임계치는 <see cref="Main.CalculateSetting.RoomPlaceNotUseJobCount"/>를 사용합니다.
                /// 잡 사용 시에는 스케줄 후 같은 프레임에 <c>Complete</c>합니다.
                /// </remarks>
                public void ApplyBatchVelocity_PlacedRoomTransformInfoCache(StageGenerator main, Vector2 plusVelocity, bool useJobs)
                {
                    int n = placedRoomTransformInfoCache.Count;

                    //! 소규모 또는 잡 미사용 경로: 즉시 루프 적용
                    if (!useJobs || n <= main.CalculateSetting.RoomPlaceNotUseJobCount)
                    {
                        var swz = main.Setting.SnapSetting.Swizzle;
                        foreach (var kv in placedRoomTransformInfoCache)
                        {
                            var v3 = (kv.Value.RoomPlaceVelocity + plusVelocity).SwizzlesVector2To3(swz);
                            kv.Key.transform.position += v3;
                        }
                        return;
                    }

                    //? 잡 경로
                    TransformAccessArray taa = default;
                    NativeArray<Vector3> deltas = default;
                    try
                    {
                        int count = BuildTAAAndDeltas_NoNull(main, plusVelocity, out taa, out deltas);
                        if (count == 0) return;

                        var job = new AddPositionJob { Deltas = deltas };
                        var handle = job.Schedule(taa);
                        handle.Complete();
                    }
                    finally
                    {
                        if (taa.isCreated) taa.Dispose();
                        if (deltas.IsCreated) deltas.Dispose();
                    }
                }



                /// <summary>
                /// <c>PlacedRoomTransformInfoCache</c>에 누적된 이동을 한 번에 적용합니다(비동기).
                /// </summary>
                /// <param name="plusVelocity">모든 방에 공통으로 더할 추가 속도(그리드 단위, 스위즐 전).</param>
                /// <param name="useJobs">
                /// <c>true</c>면 잡을 스케줄하고 완료까지 비차단 방식으로 대기하고,
                /// <c>false</c>면 큰 데이터셋을 프레임 분할로 메인 스레드에서 적용합니다.
                /// </param>
                /// <param name="ct">작업을 안전하게 중단하기 위한 취소 토큰.</param>
                /// <returns>모든 트랜스폼 적용이 끝나면 완료되는 작업.</returns>
                /// <remarks>
                /// 잡 미사용 시: 큰 N는 청크로 나누어 각 프레임에 일부만 처리하여 프레임 타임을 보호합니다.
                /// 잡 사용 시: <see cref="IJobParallelForTransform"/>을 스케줄하고 <c>await</c>로 완료까지 양보하며, 바쁜 대기를 하지 않습니다.
                /// </remarks>
                public async UniTask ApplyBatchVelocity_PlacedRoomTransformInfoCacheAsync(
                    StageGenerator main,
                    Vector2 plusVelocity,
                    bool useJobs,
                    CancellationToken ct = default)
                {
                    ct.ThrowIfCancellationRequested();

                    int n = placedRoomTransformInfoCache.Count;

                    //? 잡 미사용: 프레임 친화적 분할 적용
                    if (!useJobs)
                    {
                        var swz = main.Setting.SnapSetting.Swizzle;

                        if (n <= main.CalculateSetting.RoomPlaceNotUseJobCount)
                        {
                            foreach (var kv in placedRoomTransformInfoCache)
                            {
                                ct.ThrowIfCancellationRequested();
                                var v3 = (kv.Value.RoomPlaceVelocity + plusVelocity).SwizzlesVector2To3(swz);
                                kv.Key.transform.position += v3;
                            }
                            return;
                        }

                        const int Chunk = 256; //. 프레임 분할 크기

                        // 기존: new RoomObject[n], new Vector3[n]
                        // 변경: ArrayPool 대여/반납으로 대형 배열 할당 제거
                        var poolRoom = System.Buffers.ArrayPool<RoomObject>.Shared;
                        var poolVec3 = System.Buffers.ArrayPool<Vector3>.Shared;

                        RoomObject[] keys = poolRoom.Rent(n);   //? 대여
                        Vector3[] deltas = poolVec3.Rent(n);   //? 대여

                        try
                        {
                            //? 한 번 전개해서 캐시 친화적으로 적용
                            int k = 0;
                            foreach (var kv in placedRoomTransformInfoCache)
                            {
                                keys[k] = kv.Key;
                                deltas[k] = (kv.Value.RoomPlaceVelocity + plusVelocity).SwizzlesVector2To3(swz);
                                k++;
                            }

                            int i = 0;
                            while (i < n)
                            {
                                ct.ThrowIfCancellationRequested();

                                int end = Mathf.Min(i + Chunk, n);
                                for (int j = i; j < end; j++)
                                {
                                    //. Transform 접근은 메인스레드에서
                                    keys[j].transform.position += deltas[j];
                                }

                                i = end;
                                await UniTask.Yield(PlayerLoopTiming.Update, ct); //? 프레임 양보
                            }
                        }
                        finally
                        {
                            //. 풀 반납(덮어썼으므로 clear 불필요)
                            poolRoom.Return(keys, clearArray: false);
                            poolVec3.Return(deltas, clearArray: false);
                        }

                        return;
                    }

                    //! 잡 사용: 완료까지 비차단 대기
                    TransformAccessArray taa = default;
                    NativeArray<Vector3> naDeltas = default;
                    try
                    {
                        int count = BuildTAAAndDeltas_NoNull(main, plusVelocity, out taa, out naDeltas);
                        if (count == 0) return;

                        var job = new AddPositionJob { Deltas = naDeltas };
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
                        if (naDeltas.IsCreated) naDeltas.Dispose();
                    }
                }




                ///======================================================================================================================================================


            }



            [TitleGroup("배치 매니저"), BoxGroup("배치 매니저/박스", false)]
            [LabelText("배치된 방 Transform 정보 캐시 매니저")]
            [SerializeField]
            private PlacedRoomTransformInfoCacheManager placedRoomTFInfoCacheM = new PlacedRoomTransformInfoCacheManager();

            public PlacedRoomTransformInfoCacheManager PlacedRoomTFInfoCacheM => placedRoomTFInfoCacheM;



            ///======================================================================================================================================================



            //? 생성 분담 클래스



            private readonly Generator1_RoomCreater Gen1_RoomCreater = new();
            private readonly Generator2_LinkRooms Gen2_LinkRooms = new();
            private readonly Generator3_ConnectHallways Gen3_ConnectDoorsHallways = new();
            private readonly Generator4_HallwayCreater Gen4_HallwayCreater = new();
            private readonly Generator5_PostGenerate Gen5_PostGenerate = new();
            private readonly Generator6_CurtainCall Gen6_CurtainCall = new();
            private readonly Generator_ASharpPathFinder Gen_ASharpPathFinder = new();



            ///======================================================================================================================================================



            //? Generate 생성 메서드



            /// <summary>
            /// <see cref="PlaceManger"/> 생성: 공간 안에 방들을을 배치하고, 복도로 이어준다<br/>
            /// 다른 매니저들과 달리, 생성에 "실패" 할수도 있다 (공간 안에 조건에 맞는 방 배치, 복도 생성 충돌)
            /// </summary>
            /// <param name="spaceList">대상 공간 리스트</param>
            /// <param name="target">방을 배치하기위한 대상 트랜스폼</param>
            public bool TryGenerate_Place(IReadOnlyList<Space> spaceList, Transform target)
            {
                //. Gen1: 공간들 안에 방들을을 소환한다
                Main.LogM.Log_Place_Gen1_Ready();
                if (!Gen1_RoomCreater.Generate(Main, spaceList, target))
                {
                    Main.LogM.Log_Place_Gen1_Failure();
                    return false;
                }
                Main.LogM.Log_Place_Gen1_Success();

                if (Main.GenerateM.GeneratingStepsBreak == GenerateManager.GenerateSteps.GenStep3_Places_Gen1_CreateRooms) { return true; } //! 생성 스텝 브레이크

                //. Gen2: 공간에 있는 방들을 4방향 모두 각각 연결되어있는 공간의 가장 가까이 있는 문과 연결시킨다 (위 작업에서는 해당 근처의 문을 열기만할뿐 서로 이어주지는 않음)
                //. (쉽게 말해 문과 문을 연결한다)
                Gen2_LinkRooms.Generate(Main, spaceList);

                if (Main.GenerateM.GeneratingStepsBreak == GenerateManager.GenerateSteps.GenStep4_Places_Gen2_LinkRooms) { return true; } //! 생성 스텝 브레이크

                //. Gen3: 생성된 방들의 문과 문 사이를 이어주는 복도를 생성한다
                Main.LogM.Log_Place_Gen3_Ready();
                if (!Gen3_ConnectDoorsHallways.Generate(Main, spaceList))
                {
                    Main.LogM.Log_Place_Gen3_Failure();
                    return false;
                }
                Main.LogM.Log_Place_Gen3_Success();

                if (Main.GenerateM.GeneratingStepsBreak == GenerateManager.GenerateSteps.GenStep5_Places_Gen3_ConnectHallways) { return true; } //! 생성 스텝 브레이크

                //. Gen4: 복도 오브젝트를 생성
                Gen4_HallwayCreater.Generate(Main, target);

                if (Main.GenerateM.GeneratingStepsBreak == GenerateManager.GenerateSteps.GenStep6_Places_Gen4_CreateHallways) { return true; } //! 생성 스텝 브레이크

                //. Gen5: Post Generate 실행
                Gen5_PostGenerate.PostGenerate(Main);

                if (Main.GenerateM.GeneratingStepsBreak == GenerateManager.GenerateSteps.GenStep7_Places_Gen5_PostGenerate) { return true; } //! 생성 스텝 브레이크

                //. Gen6: 커튼콜 마무리
                Gen6_CurtainCall.CurtainCall(Main);

                if (Main.GenerateM.GeneratingStepsBreak == GenerateManager.GenerateSteps.GenStep8_Places_Gen6_CurtainCall) { return true; } //! 생성 스텝 브레이크

                return true;
            }



            /// <summary>
            /// <see cref="PlaceManger"/> 생성: 공간 안에 방들을을 배치하고, 복도로 이어준다<br/>
            /// 다른 매니저들과 달리, 생성에 "실패" 할수도 있다 (공간 안에 조건에 맞는 방 배치, 복도 생성 충돌)
            /// </summary>
            /// <param name="spaceList">대상 공간 리스트</param>
            /// <param name="target">방을 배치하기위한 대상 트랜스폼</param>
            public async UniTask<bool> TryGenerate_PlaceAsync(IReadOnlyList<Space> spaceList, Transform target)
            {
                //. Gen1: 공간들 안에 방들을을 소환한다
                Main.LogM.Log_Place_Gen1_Ready();
                if (!await Gen1_RoomCreater.GenerateAsync(Main, spaceList, target))
                {
                    Main.LogM.Log_Place_Gen1_Failure();
                    return false;
                }
                Main.LogM.Log_Place_Gen1_Success();

                if (Main.GenerateM.GeneratingStepsBreak == GenerateManager.GenerateSteps.GenStep3_Places_Gen1_CreateRooms) { return true; } //! 생성 스텝 브레이크


                //. Gen2: 공간에 있는 방들을 4방향 모두 각각 연결되어있는 공간의 가장 가까이 있는 문과 연결시킨다 (위 작업에서는 해당 근처의 문을 열기만할뿐 서로 이어주지는 않음)
                //. (쉽게 말해 문과 문을 연결한다)
                await Gen2_LinkRooms.GenerateAsync(Main, spaceList);

                if (Main.GenerateM.GeneratingStepsBreak == GenerateManager.GenerateSteps.GenStep4_Places_Gen2_LinkRooms) { return true; } //! 생성 스텝 브레이크

                //. Gen3: 생성된 방들의 문과 문 사이를 이어주는 복도를 생성한다
                Main.LogM.Log_Place_Gen3_Ready();
                if (!await Gen3_ConnectDoorsHallways.GenerateAsync(Main, spaceList))
                {
                    //ErrorMsg_FailedGenerate(StageGenerator, "복도 생성 실패");
                    Main.LogM.Log_Place_Gen3_Failure();
                    return false;
                }
                Main.LogM.Log_Place_Gen3_Success();

                if (Main.GenerateM.GeneratingStepsBreak == GenerateManager.GenerateSteps.GenStep5_Places_Gen3_ConnectHallways) { return true; } //! 생성 스텝 브레이크

                //. Gen4: 복도 오브젝트를 생성
                await Gen4_HallwayCreater.GenerateAsync(Main, target);

                if (Main.GenerateM.GeneratingStepsBreak == GenerateManager.GenerateSteps.GenStep6_Places_Gen4_CreateHallways) { return true; } //! 생성 스텝 브레이크

                //. Gen5: Post Generate 실행
                await Gen5_PostGenerate.PostGenerateAsync(Main);

                if (Main.GenerateM.GeneratingStepsBreak == GenerateManager.GenerateSteps.GenStep7_Places_Gen5_PostGenerate) { return true; } //! 생성 스텝 브레이크

                //. Gen6: 커튼콜 마무리
                await Gen6_CurtainCall.CurtainCallAsync(Main);

                if (Main.GenerateM.GeneratingStepsBreak == GenerateManager.GenerateSteps.GenStep8_Places_Gen6_CurtainCall) { return true; } //! 생성 스텝 브레이크

                return true;
            }



            ///======================================================================================================================================================



            //? 초기화



            ///<summary>
            /// 생성과 관련된 모든 정보를 초기화한다
            /// </summary>
            public void ClearPlaceManager(bool destroyObject, bool clearExpandedGrids, bool setNullCaches)
            {
                ClearInstanceObjects(destroyObject, clearExpandedGrids);
                Clear_Reports();
                PlacedRoomTFInfoCacheM.Clear_PlacedRoomTransformInfoCache(setNullCaches);
            }



            ///<summary>
            /// (비동기)생성과 관련된 모든 정보를 초기화한다
            /// </summary>
            public async UniTask ClearPlaceManagerAsync(bool destroyObject, bool clearExpandedGrids, bool setNullCaches)
            {
                Clear_Reports();
                await ClearInstanceObjectsAsync(destroyObject, clearExpandedGrids);
                PlacedRoomTFInfoCacheM.Clear_PlacedRoomTransformInfoCache(setNullCaches);
            }



            /// <summary>
            /// 생성 기록 초기화
            /// </summary>
            public void Clear_Reports()
            {
                Reconstruction_ConnectHallways_ReportCount = 0;
                MoveNearestRooms_First_ReportCount = 0;
                MoveNearestRooms_Second_ReportCount = 0;
            }



            /// <summary>
            /// 모든 방들에게, 자기 자신이 보유하고있는 스테이지 생성 관련 정보를 초기화한다
            /// </summary>
            public void Clear_Rooms_GeneratedInfo()
            {
                for (int i = 0; i < placeObjects_Room.SummonedObjectList.Count; i++)
                {
                    placeObjects_Room.SummonedObjectList[i].ClearStageGenerateInfo();
                }
            }



            /// <summary>
            /// <b>소환된 오브젝트</b>, 그리드들을 전부 제거 (파괴도 가능)
            /// </summary>
            /// <param name="destroyBeforeClear">true시, 오브젝트를 파괴도함</param>
            /// <param name="clearExpandGrids">확장 그리드 리스트도 초기화</param>
            public void ClearInstanceObjects(bool destroyBeforeClear, bool clearExpandGrids)
            {
                PlaceObjects_Room.Clear(Main, destroyBeforeClear);
                PlaceObjects_Hallway.Clear(Main, destroyBeforeClear);
                PlaceObjects_HallwayEdge.Clear(Main, destroyBeforeClear);
                PlaceObjects_ETC.Clear(Main, destroyBeforeClear);

                if (clearExpandGrids)
                {
                    grids_CustomExpand.Clear();
                }
            }



            /// <summary>
            /// [비동기] <b>소환된 오브젝트</b>, 그리드들을 전부 제거 (파괴도 가능)
            /// </summary>
            /// <param name="destroyBeforeClear">true시, 오브젝트를 파괴도함</param>
            /// <param name="clearExpandGrids">확장 그리드 리스트도 초기화</param>
            public async UniTask ClearInstanceObjectsAsync(bool destroyBeforeClear, bool clearExpandGrids)
            {
                if (destroyBeforeClear)
                {
                    await UniTask.WhenAll(
                        PlaceObjects_Room.ClearDestroyAsync(Main),
                        PlaceObjects_Hallway.ClearDestroyAsync(Main),
                        PlaceObjects_HallwayEdge.ClearDestroyAsync(Main),
                        PlaceObjects_ETC.ClearDestroyAsync(Main)
                        );
                }
                else
                {
                    PlaceObjects_Room.Clear(Main, false);
                    PlaceObjects_Hallway.Clear(Main, false);
                    PlaceObjects_HallwayEdge.Clear(Main, false);
                    PlaceObjects_ETC.Clear(Main, false);
                }


                if (clearExpandGrids)
                {
                    grids_CustomExpand.Clear();
                }
            }



            /// <summary>
            /// PlaceManager 내의 모든 스테이지 생성 관련 정보 초기화
            /// </summary>
            [TitleGroup("배치 매니저"), BoxGroup("배치 매니저/박스", false)]
            [Button("PlaceManager 생성정보 일괄 제거", Icon = SdfIconType.Trash), GUIColor(0.67f, 0.57f, 0.93f)]
            public void ClearPlaceManager_GeneratedInfo()
            {
                //. 생성된 모든 방들이 자신이 가지고 있는 정보 제거
                Clear_Rooms_GeneratedInfo();

                //. 등록된 모든 방들을이 담긴 목록을 초기화
                ClearPlaceManager(false, true, true);

                //. 기록 초기화
                Clear_Reports();

                //. 방 트랜스폼 캐시 매니저 초기화
                PlacedRoomTFInfoCacheM.Clear_PlacedRoomTransformInfoCache(true);
            }



            ///======================================================================================================================================================
        }
    }
}
