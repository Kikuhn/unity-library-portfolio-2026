using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Pan.Util;
using System;
using System.Text;
using Cysharp.Threading.Tasks;
using Pan.GridCompatibles2;
using Pan.StageGenerators;
using System.Threading.Tasks;
using Sirenix.OdinInspector;



namespace Pan.StageGenerators
{
    public partial class StageGenerator
    {
        public interface ISpace
        {

        }



        [Serializable]
        public class Space
        {
            ///======================================================================================================================================================



            public Space(StageGeneratorSetting stageGeneratorSetting, Rect spaceRect, Vector2 spaceTransformOrigin)
            {
                //. 스테이지 생성 설정을 먼저 할당해야, 하위 코드들이 잘 작동한다
                this.stageGeneratorSetting = stageGeneratorSetting;

                SpaceRect = spaceRect;
                this.spaceTransformOrigin = spaceTransformOrigin;

                totalConnectingSpaces = new List<Space>();
                ConnectingSpaces_Down = new List<Space>();
                ConnectingSpaces_Up = new List<Space>();
                ConnectingSpaces_Left = new List<Space>();
                ConnectingSpaces_Right = new List<Space>();

                TotalMaxNodeCount = stageGeneratorSetting.SpaceNodeConnect.NodeCountMax;
            }



            #region Space 중복 방지용 Equals, GetHashCode 오버라이드

            public bool Equals(Space other)
                    => other != null && SpaceIndex == other.SpaceIndex;

            public override bool Equals(object obj)
                => obj is Space s && Equals(s);

            public override int GetHashCode() => SpaceIndex;

            #endregion



            ///======================================================================================================================================================



            ///<summary>
            /// 공간 이진 트리의 Index
            /// </summary>
            [TitleGroup("기타"), BoxGroup("기타/박스", false), FoldoutGroup("기타/박스/자세히 보기")]
            [DisplayAsString, EnableGUI]
            [LabelText("공간Index")]
            [PropertyTooltip("이 공간이 공간 이진 리스트에 속한 Index")]
            [PropertyOrder(99)]
            public int SpaceIndex;


            //[TitleGroup("기타"), BoxGroup("기타/박스", false), FoldoutGroup("기타/박스/자세히 보기")]
            //[DisplayAsString, EnableGUI]
            //[LabelText("공간Index 도어 잇기")]
            //[PropertyOrder(99)]
            //public int SpaceIndex_ConnectDoor;



            /// <summary>
            /// 공간이 공간이 생성된 순서를 기록
            /// </summary>
            [TitleGroup("기타"), BoxGroup("기타/박스", false), FoldoutGroup("기타/박스/자세히 보기")]
            [DisplayAsString, EnableGUI]
            [LabelText("공간Index: 방 인접 순서")]
            [PropertyTooltip("방 인접을 사용할때 저장되는 우선 순서")]
            [PropertyOrder(99)]
            public int OrderIndex_RoomNearest;



            /// <summary>
            /// 복도 생성을 위해 정렬된 이 공간의 순서를 기록
            /// </summary>
            [TitleGroup("기타"), BoxGroup("기타/박스", false), FoldoutGroup("기타/박스/자세히 보기")]
            [DisplayAsString, EnableGUI]
            [LabelText("복도 생성 순서")]
            [PropertyTooltip("복도 생성을 사용할때 저장되는 우선 순서")]
            [PropertyOrder(99)]
            public int OrderIndex_Hallways;



            ///<summary>
            /// 이 공간에 배치된 방
            ///</summary>
            [TitleGroup("기타"), BoxGroup("기타/박스", false), FoldoutGroup("기타/박스/자세히 보기")]
            [LabelText("방")]
            [PropertyTooltip("이 공간에 배치된 방")]
            [PropertyOrder(100)]
            [ShowInInspector]
            public RoomObject PlacedRoom { get => placedRoom; set => placedRoom = value; }
            private RoomObject placedRoom;



            ///======================================================================================================================================================



            [SerializeField][HideInInspector] private StageGeneratorSetting stageGeneratorSetting;



            ///======================================================================================================================================================



            //? 공간 벡터



            #region 공간 벡터



#if UNITY_EDITOR
            [TitleGroup("공간 벡터"), BoxGroup("공간 벡터/박스", false)]
            [BoxGroup("공간 벡터/박스/요약박스", false)]
            [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Left, EnableRichText = true, Overflow = false), EnableGUI]
            [PropertyOrder(-99)]
            private string dummyTitle_SpacVectorInfo
            {
                get
                {
                    return $"Index: <b><color=white>{SpaceIndex}</color></b> (RoomNearest <b><color=white>{OrderIndex_RoomNearest}</color></b>)\n공간 크기: <b><color=#2ecc71>[{spaceRect.width}, {spaceRect.height}]</color></b> 중심점: <b><color=#ac92ec>({spaceRect.center.x}, {spaceRect.center.y})</color></b> 좌측하단: <b><color=#f7da64>({spaceRect.x}, {spaceRect.y})</color></b>\n방 생성 {((canCreateRoom) ? "<color=#4fc1e9><b>가능<b></color>" : "<color=#ed5565><b>불가능</b></color>")}";
                    //return $"<color=white><size=13>그리드 배열 크기: <color=#2ecc71><b>{GridArrayTotalLength}</b></color>";
                }
            }

#endif



            [TitleGroup("공간 벡터"), BoxGroup("공간 벡터/박스", false), FoldoutGroup("공간 벡터/박스/자세히 보기")]
            [ShowInInspector, EnableGUI, Sirenix.OdinInspector.ReadOnly]
            [LabelText("공간 Rect")]
            [PropertyOrder(10)]
            public Rect SpaceRect
            {
                get => spaceRect;
                set
                {
                    spaceRect = value;

                    //? 공간 Rect가 달라질때마다 (아마 맨 처음만 그러겠지만)
                    //? 방 생성 가능 여부를 새로 계산하여 갱신한다
                    canCreateRoom =
                        spaceRect.width >= stageGeneratorSetting.Room.RoomPlacementMinWidth &&
                        spaceRect.height >= stageGeneratorSetting.Room.RoomPlacementMinHeight;
                }
            }
            [SerializeField, HideInInspector]
            private Rect spaceRect;



            [TitleGroup("공간 벡터"), BoxGroup("공간 벡터/박스", false), FoldoutGroup("공간 벡터/박스/자세히 보기")]
            [ShowInInspector, EnableGUI, Sirenix.OdinInspector.ReadOnly]
            [LabelText("공간 기준점 (Trasnform)")]
            [PropertyOrder(10)]
            public Vector2 SpaceTransformOrigin => spaceTransformOrigin;
            [SerializeField, HideInInspector]
            private Vector2 spaceTransformOrigin;



            [TitleGroup("공간 벡터"), BoxGroup("공간 벡터/박스", false), FoldoutGroup("공간 벡터/박스/자세히 보기")]
            [ShowInInspector, EnableGUI, Sirenix.OdinInspector.ReadOnly]
            [LabelText("공간 Rect (Trasnform)")]
            [PropertyOrder(10)]
            public Rect SpaceTransformRect
            {
                get
                {
                    var spaceRect = SpaceRect;
                    spaceRect.x *= stageGeneratorSetting.SnapSetting.GridUnitX_Width;
                    spaceRect.y *= stageGeneratorSetting.SnapSetting.GridUnitY_Height;
                    spaceRect.width *= stageGeneratorSetting.SnapSetting.GridUnitX_Width;
                    spaceRect.height *= stageGeneratorSetting.SnapSetting.GridUnitY_Height;

                    return spaceRect;
                }
            }



            [TitleGroup("공간 벡터"), BoxGroup("공간 벡터/박스", false), FoldoutGroup("공간 벡터/박스/자세히 보기")]
            [ShowInInspector, EnableGUI, Sirenix.OdinInspector.ReadOnly]
            [LabelText("공간 기준점 Rect (Trasnform)")]
            [PropertyOrder(10)]
            public Rect SpaceOriginTransformRect
            {
                get
                {
                    var spaceTransformRect = SpaceTransformRect;
                    spaceTransformRect.position += spaceTransformOrigin;
                    return spaceTransformRect;
                }
            }



            /// <summary>
            /// 공간 중심점을 <see cref="StageGenerator"/>을 기준으로 구한다
            /// </summary>
            /// <param name="stageGenerator"></param>
            /// <returns></returns>
            public Vector3 SpaceStageGeneratorTransformCurrentCenter(StageGenerator stageGenerator)
            {
                var center = SpaceTransformRect.center;
                return center.SwizzlesVector2To3(stageGenerator.Setting.SnapSetting.Swizzle);
            }



            /// <summary>
            /// 이 공간이 방을 생성할수 있는 공간인지의 여부
            /// </summary>
            [TitleGroup("공간 벡터"), BoxGroup("공간 벡터/박스", false), FoldoutGroup("공간 벡터/박스/자세히 보기")]
            [ShowInInspector, EnableGUI, Sirenix.OdinInspector.ReadOnly]
            [LabelText("방 생성 가능 여부")]
            [PropertyTooltip("공간Rect의 따라, 이 공간 안에 방 생성이 가능한지 정해진다")]
            [PropertyOrder(10)]
            public bool CanCreateRoom => canCreateRoom;
            private bool canCreateRoom;



            #endregion



            ///======================================================================================================================================================



            //? 노드



            #region 노드



#if UNITY_EDITOR

            [TitleGroup("노드"), BoxGroup("노드/박스", false)]
            [BoxGroup("노드/박스/요약박스", false)]
            [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Left, EnableRichText = true, Overflow = false), EnableGUI]
            [PropertyOrder(20)]
            private string dummy_NodeInfo
            {
                get
                {
                    return $"통합 노드 개수:  <b><color=#2ecc71>{TotalConnectingSpacesCount}<b></color> / <b><color=#ed5565>{TotalMaxNodeCount}</color></b>\n" +
                        $"{"↓:",-5} {$"<b>{((HasNode_Down) ? "<color=#2ecc71>" : "<color=white>")}{$"{CurrentNodeCount_Down}</color></b> / ",-13}"}<b><color=#ed5565>{MaxNodeCount_Down}</color></b>\t" +
                        $"{"↑:",-5} {$"<b>{((HasNode_Up) ? "<color=#2ecc71>" : "<color=white>")}{$"{CurrentNodeCount_Up}</color></b> / ",-13}"}<b><color=#ed5565>{MaxNodeCount_Up}</color></b>\n" +
                        $"{"←:",-5} {$"<b>{((HasNode_Left) ? "<color=#2ecc71>" : "<color=white>")}{$"{CurrentNodeCount_Left}</color></b> / ",-13}"}<b><color=#ed5565>{MaxNodeCount_Left}</color></b>\t" +
                        $"{"→:",-5} {$"<b>{((HasNode_Right) ? "<color=#2ecc71>" : "<color=#white>")}{$"{CurrentNodeCount_Right}</color></b> / ",-13}"}<b><color=#ed5565>{MaxNodeCount_Right}</color></b>\n" +
                        $"노드 추가 <b>{(PossibleAddMoreNode ? "<color=#4fc1e9>가능</color>" : "<color=#ed5565>불가능</color>")}</b>";
                }
            }

#endif



            [TitleGroup("노드"), BoxGroup("노드/박스", false)]
            [FoldoutGroup("노드/박스/자세히 보기")]
            [DisplayAsString, EnableGUI]
            [LabelText("통합 최대 노드 개수")]
            [PropertyTooltip("이 공간이 보유할수있는 최대 노드 개수")]
            [PropertyOrder(21)]
            public int TotalMaxNodeCount;



            [TitleGroup("노드"), BoxGroup("노드/박스", false)]
            [FoldoutGroup("노드/박스/자세히 보기")]
            [DisplayAsString, EnableGUI]
            [LabelText("노드 추가 가능 여부")]
            [PropertyTooltip("이 공간이 보유할수있는 최대 노드 개수")]
            [PropertyOrder(21)]
            public bool PossibleAddMoreNode => TotalConnectingSpacesCount <= TotalMaxNodeCount;



            [TitleGroup("노드"), BoxGroup("노드/박스", false)]
            [FoldoutGroup("노드/박스/자세히 보기")]
            [ShowInInspector, DisplayAsString, EnableGUI]
            [LabelText("↓하단 최대 노드")]
            [PropertyOrder(21)]
            public int MaxNodeCount_Down
            {
                get => maxNodeCount_Down;
                set
                {
                    maxNodeCount_Down.SetClampMin(value, 0);
                    maxNodeCount_Down.SetClampMax(value, getAllDirectionMaxNodeCount);
                }
            }
            [SerializeField, HideInInspector]
            private int maxNodeCount_Down = 1;



            [TitleGroup("노드"), BoxGroup("노드/박스", false)]
            [FoldoutGroup("노드/박스/자세히 보기")]
            [ShowInInspector, DisplayAsString, EnableGUI]
            [LabelText("↑상단 최대 노드")]
            [PropertyOrder(21)]
            public int MaxNodeCount_Up
            {
                get => maxNodeCount_Up;
                set
                {
                    maxNodeCount_Up.SetClampMin(value, 0);
                    maxNodeCount_Up.SetClampMax(value, getAllDirectionMaxNodeCount);
                }
            }
            [SerializeField, HideInInspector]
            private int maxNodeCount_Up = 1;



            [TitleGroup("노드"), BoxGroup("노드/박스", false)]
            [FoldoutGroup("노드/박스/자세히 보기")]
            [ShowInInspector, DisplayAsString, EnableGUI]
            [LabelText("←좌측 최대 노드")]
            [PropertyOrder(21)]
            public int MaxNodeCount_Left
            {
                get => maxNodeCount_Left;
                set
                {
                    maxNodeCount_Left.SetClampMin(value, 0);
                    maxNodeCount_Left.SetClampMax(value, getAllDirectionMaxNodeCount);
                }
            }
            [SerializeField, HideInInspector]
            private int maxNodeCount_Left = 1;



            [TitleGroup("노드"), BoxGroup("노드/박스", false)]
            [FoldoutGroup("노드/박스/자세히 보기")]
            [ShowInInspector, DisplayAsString, EnableGUI]
            [LabelText("→우측 최대 노드")]
            [PropertyOrder(21)]
            public int MaxNodeCount_Right
            {
                get => maxNodeCount_Right;
                set
                {
                    maxNodeCount_Right.SetClampMin(value, 0);
                    maxNodeCount_Right.SetClampMax(value, getAllDirectionMaxNodeCount);
                }
            }
            [SerializeField, HideInInspector]
            private int maxNodeCount_Right = 1;



            /// <summary>
            /// 모든 4방향의 최대 노드를 더해 반환한다
            /// </summary>
            private int getAllDirectionMaxNodeCount => maxNodeCount_Down + maxNodeCount_Up + maxNodeCount_Left + maxNodeCount_Right;



            ///<summary>
            /// 해당하는 방향의 최대 노드 개수를 얻기
            /// </summary>
            public int GetMaxNodeCount(EDirection4 dir)
            {
                switch (dir)
                {
                    case EDirection4.Down: return MaxNodeCount_Down;
                    case EDirection4.Up: return MaxNodeCount_Up;
                    case EDirection4.Left: return MaxNodeCount_Left;
                    case EDirection4.Right: return MaxNodeCount_Right;
                }
                return 0;
            }



            //? 현재 노드 정보



            ///<summary>
            ///이 공간과 연결되어있는 공간의 총 개수
            ///</summary>
            [TitleGroup("노드"), BoxGroup("노드/박스", false)]
            [FoldoutGroup("노드/박스/자세히 보기")]
            [ShowInInspector]
            [DisplayAsString, EnableGUI]
            [LabelText("현재 총 노드 개수")]
            [LabelWidth(200)]
            [PropertyOrder(21)]
            public int TotalConnectingSpacesCount => totalConnectingSpaces.Count;



            ///<summary>
            ///이 공간과 연결되어있는 공간의 [하단] 방향의 개수
            ///</summary>
            [TitleGroup("노드"), BoxGroup("노드/박스", false)]
            [FoldoutGroup("노드/박스/자세히 보기")]
            [ShowInInspector]
            [DisplayAsString, EnableGUI]
            [LabelText("현재 ↓하단 노드 개수")]
            [LabelWidth(200)]
            [PropertyOrder(21)]
            public int CurrentNodeCount_Down => ConnectingSpaces_Down.Count;

            ///<summary>
            ///이 공간과 연결되어있는 공간의 [상단] 방향의 개수
            ///</summary>
            [TitleGroup("노드"), BoxGroup("노드/박스", false)]
            [FoldoutGroup("노드/박스/자세히 보기")]
            [ShowInInspector]
            [DisplayAsString, EnableGUI]
            [LabelText("현재 ↑상단 노드 개수")]
            [LabelWidth(200)]
            [PropertyOrder(21)]
            public int CurrentNodeCount_Up => ConnectingSpaces_Up.Count;

            ///<summary>
            ///이 공간과 연결되어있는 공간의 [좌측] 방향의 개수
            ///</summary>
            [TitleGroup("노드"), BoxGroup("노드/박스", false)]
            [FoldoutGroup("노드/박스/자세히 보기")]
            [ShowInInspector]
            [DisplayAsString, EnableGUI]
            [LabelText("현재 ←좌측 노드 개수")]
            [LabelWidth(200)]
            [PropertyOrder(21)]
            public int CurrentNodeCount_Left => ConnectingSpaces_Left.Count;

            ///<summary>
            ///이 공간과 연결되어있는 공간의 [우측] 방향의 개수
            ///</summary>
            [TitleGroup("노드"), BoxGroup("노드/박스", false)]
            [FoldoutGroup("노드/박스/자세히 보기")]
            [ShowInInspector]
            [DisplayAsString, EnableGUI]
            [LabelText("현재 →우측 노드 개수")]
            [LabelWidth(200)]
            [PropertyOrder(21)]
            public int CurrentNodeCount_Right => ConnectingSpaces_Right.Count;



            public int GetCurrentNodeCount(EDirection4 direction)
            {
                switch (direction)
                {
                    case EDirection4.Down: return CurrentNodeCount_Down;
                    case EDirection4.Up: return CurrentNodeCount_Up;
                    case EDirection4.Left: return CurrentNodeCount_Left;
                    case EDirection4.Right: return CurrentNodeCount_Right;
                    default: return 0;
                }
            }


            ///<summary>
            ///이 공간과 [하단] 방향과 연결되어있는 공간이 있는가
            ///</summary>
            public bool HasNode_Down => ConnectingSpaces_Down.Count > 0;

            ///<summary>
            ///이 공간과 [상단] 방향과 연결되어있는 공간이 있는가
            ///</summary>
            public bool HasNode_Up => ConnectingSpaces_Up.Count > 0;

            ///<summary>
            ///이 공간과 [좌측] 방향과 연결되어있는 공간이 있는가
            ///</summary>
            public bool HasNode_Left => ConnectingSpaces_Left.Count > 0;

            ///<summary>
            ///이 공간과 [우측] 방향과 연결되어있는 공간이 있는가
            ///</summary>
            public bool HasNode_Right => ConnectingSpaces_Right.Count > 0;



            ///<summary>
            ///이 공간과 [하단 또는 상단] 방향과 연결되어있는가
            ///</summary>
            public bool HasNode_Vertical => HasNode_Down || HasNode_Up;

            ///<summary>
            ///이 공간과 [좌측 또는 우측] 방향과 연결되어있는가
            ///</summary>
            public bool HasNode_Horizontal => HasNode_Left || HasNode_Right;



            ///<summary>
            ///세로축에서 단방향 노드가 [하단]에만 존재하는가
            ///</summary>
            public bool IsOneWayNode_Down => HasNode_Down && !HasNode_Up;

            ///<summary>
            ///세로축에서 단방향 노드가 [상단]에만 존재하는가
            ///</summary>
            public bool IsOneWayNode_Up => HasNode_Up && !HasNode_Down;

            ///<summary>
            ///가로축에서 단방향 노드가 [좌측]에만 존재하는가
            ///</summary>
            public bool IsOneWayNode_Left => HasNode_Left && !HasNode_Right;

            ///<summary>
            ///가로축에서 단방향 노드가 [우측]에만 존재하는가
            ///</summary>
            public bool IsOneWayNode_Right => HasNode_Right && !HasNode_Left;



            ///<summary>
            ///세로축 기준으로 단방향 노드가 존재하는가
            ///</summary>
            public bool HasOneWayNode_Vertical => IsOneWayNode_Down || IsOneWayNode_Up;

            ///<summary>
            ///가로축 기준으로 단방향 노드가 존재하는가
            ///</summary>
            public bool HasOneWayNode_Horizontal => IsOneWayNode_Left || IsOneWayNode_Right;



            ///<summary>
            ///세로축 기준으로 양방향 노드가 존재하는가 (하단·상단 모두 연결)
            ///</summary>
            public bool IsTwoWayNode_Vertical => HasNode_Down && HasNode_Up;

            ///<summary>
            ///가로축 기준으로 양방향 노드가 존재하는가 (좌측·우측 모두 연결)
            ///</summary>
            public bool IsTwoWayNode_Horizontal => HasNode_Left && HasNode_Right;



            ///<summary>
            ///4방향 중 [하단]만 연결되어있는가
            ///</summary>
            public bool IsOnlyNode_Down => HasNode_Down && !HasNode_Up && !HasNode_Left && !HasNode_Right;

            ///<summary>
            ///4방향 중 [상단]만 연결되어있는가
            ///</summary>
            public bool IsOnlyNode_Up => HasNode_Up && !HasNode_Down && !HasNode_Left && !HasNode_Right;

            ///<summary>
            ///4방향 중 [좌측]만 연결되어있는가
            ///</summary>
            public bool IsOnlyNode_Left => HasNode_Left && !HasNode_Right && !HasNode_Down && !HasNode_Up;

            ///<summary>
            ///4방향 중 [우측]만 연결되어있는가
            ///</summary>
            public bool IsOnlyNode_Right => HasNode_Right && !HasNode_Left && !HasNode_Down && !HasNode_Up;





            /// <summary>
            /// 각 방향별로, 노드가 있는지 한번에 반환한다
            /// </summary>
            public void GetHasNodesInfoAtOnece(out bool isConnecting_Down, out bool isConnecting_Up, out bool isConnecting_Left, out bool isConnecting_Right)
            {
                isConnecting_Down = HasNode_Down;
                isConnecting_Up = HasNode_Up;
                isConnecting_Left = HasNode_Left;
                isConnecting_Right = HasNode_Right;
            }



            #endregion



            ///======================================================================================================================================================



            //? 연결



            #region 연결



#if UNITY_EDITOR

            private List<int> dummy_neighborSpaceIndexList = new List<int>();


            [TitleGroup("연결"), BoxGroup("연결/박스", false)]
            [BoxGroup("연결/박스/요약박스", false)]
            [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Left, EnableRichText = true, Overflow = false), EnableGUI]
            [PropertyOrder(30)]
            private string dummy_NeighborSpaceInfo
            {
                get
                {
                    var sb = SU_String.GetStringBuilderPublic(true);

                    if (neighborSpaces != null)
                    {
                        dummy_neighborSpaceIndexList.Clear();

                        for (int i = 0; i < neighborSpaces.Count; i++)
                        {
                            dummy_neighborSpaceIndexList.Add(neighborSpaces[i].SpaceIndex);
                        }

                        sb.Append($"<color=#2ecc71><b>{neighborSpaces.Count}</b></color>개 이웃 공간 [ ");
                        sb.Append("<color=#f7da64><b>");
                        sb.Append(string.Join("</b></color>, <color=#f7da64><b>", dummy_neighborSpaceIndexList));
                        sb.Append("</b></color>]");
                    }

                    //if (neighborSpaces != null)
                    //{
                    //    sb.Append("<color=white>");
                    //    sb.Append($"{"이웃 공간 개수:",-10} <color=#2ecc71><b>{neighborSpaces.Count}</b></color>\n{"",-26}아녕")}));
                    //}

                    var result = sb.ToString(true);
                    sb.Clear();
                    return result;
                    //return $"<color=white>{((neighborSpaces != null) ? </color>";
                }
            }



            private List<int> dummy_ConnectingSpaceIndexList = new List<int>();



            [TitleGroup("연결"), BoxGroup("연결/박스", false)]
            [BoxGroup("연결/박스/요약박스", false)]
            //[FoldoutGroup("연결/박스/자세히 보기")]
            [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Left, EnableRichText = true, Overflow = false), EnableGUI]
            [PropertyOrder(31)]
            private string dummy_ConnectingSpaceInfo
            {
                get
                {
                    if (totalConnectingSpaces.Count == 0) { dummy_ConnectingSpaceIndexList = null; return $"연결된 공간이 없음"; }

                    if (dummy_ConnectingSpaceIndexList == null) { return ""; }

                    dummy_ConnectingSpaceIndexList.Clear();
                    dummy_ConnectingSpaceIndexList.Capacity = totalConnectingSpaces.Count;

                    for (int i = 0; i < totalConnectingSpaces.Count; i++)
                    {
                        var connectingSpace = totalConnectingSpaces[i];
                        dummy_ConnectingSpaceIndexList.Add(connectingSpace.SpaceIndex);
                    }
                    //<b><color=#2ecc71>{TotalConnectingSpacesCount}</color></b>
                    return $"<color=#2ecc71>{TotalConnectingSpacesCount}</color>개 연결 공간 [ <color=#f7da64><b>{string.Join("</b></color>, <color=#f7da64><b>", dummy_ConnectingSpaceIndexList)}</b></color> ]";
                }
            }
#endif



            [TitleGroup("연결"), BoxGroup("연결/박스", false)]
            [FoldoutGroup("연결/박스/자세히 보기")]
            [SerializeReference, HideReferenceObjectPicker, HideDuplicateReferenceBox]
            [SerializeField]
            [Sirenix.OdinInspector.ReadOnly]
            [ListDrawerSettings(HideAddButton = true, HideRemoveButton = true, DraggableItems = false)]
            [LabelText("이웃 공간 목록")]
            [PropertyOrder(32)]
            private List<Space> neighborSpaces;
            /// <summary>
            /// 이웃 공간 목록
            /// <para>인접한 공간들을 가져오며, 어긋나게 인접해있다면 (삐져나오거나) 이웃 취급이 되지않아야 한다</para>
            /// </summary>
            public List<Space> NeighborSpaces
            {
                get => neighborSpaces;
                set { neighborSpaces = value; }
            }



            ///<summary>
            /// 연결 공간 목록
            ///</summary>
            [TitleGroup("연결"), BoxGroup("연결/박스", false)]
            [FoldoutGroup("연결/박스/자세히 보기")]
            [SerializeReference, HideReferenceObjectPicker, HideDuplicateReferenceBox]
            [Sirenix.OdinInspector.ReadOnly, EnableGUI]
            [ListDrawerSettings(HideAddButton = true, HideRemoveButton = true, DraggableItems = false)]
            [LabelText("총 연결 공간 목록")]
            [PropertyTooltip("방향과 상관없이, 이 공간과 연결된 공간들이 이곳에 저장된다")]
            [PropertyOrder(32)]
            private List<Space> totalConnectingSpaces;
            public IReadOnlyList<Space> TotalConnectingSpaces => totalConnectingSpaces;



            ///<summary>
            ///이 공간과 [하단]에 연결 되어있는 공간들
            ///</summary>
            [TitleGroup("연결"), BoxGroup("연결/박스", false)]
            [FoldoutGroup("연결/박스/자세히 보기")]
            [SerializeReference]
            [LabelText("하단 공간 연결 목록")]
            [PropertyOrder(32)]
            public List<Space> ConnectingSpaces_Down;

            ///<summary>
            ///이 공간과 [상단]에 연결 되어있는 공간들
            ///</summary>
            [TitleGroup("연결"), BoxGroup("연결/박스", false)]
            [FoldoutGroup("연결/박스/자세히 보기")]
            [SerializeReference]
            [LabelText("상단 공간 연결 목록")]
            [PropertyOrder(32)]
            public List<Space> ConnectingSpaces_Up;

            ///<summary>
            ///이 공간과 [좌측]에 연결 되어있는 공간들
            ///</summary>
            [TitleGroup("연결"), BoxGroup("연결/박스", false)]
            [FoldoutGroup("연결/박스/자세히 보기")]
            [SerializeReference]
            [LabelText("좌측 공간 연결 목록")]
            [PropertyOrder(32)]
            public List<Space> ConnectingSpaces_Left;

            ///<summary>
            ///이 공간과 [우측]에 연결 되어있는 공간들
            ///</summary>
            [TitleGroup("연결"), BoxGroup("연결/박스", false)]
            [FoldoutGroup("연결/박스/자세히 보기")]
            [SerializeReference]
            [LabelText("우측 공간 연결 목록")]
            [PropertyOrder(32)]
            public List<Space> ConnectingSpaces_Right;



            /// <summary>
            /// 방향별 연결 공간 목록을 얻기
            /// </summary>
            /// <param name="direction"></param>
            /// <returns></returns>
            private List<Space> GetConnectingSpacesInternal(EDirection4 direction)
            {
                switch (direction)
                {
                    case EDirection4.Down: return ConnectingSpaces_Down;
                    case EDirection4.Up: return ConnectingSpaces_Up;
                    case EDirection4.Left: return ConnectingSpaces_Left;
                    case EDirection4.Right: return ConnectingSpaces_Right;
                    default: return null;
                }
            }



            /// <summary>
            /// 방향별 연결 공간 목록을 얻기
            /// </summary>
            /// <param name="direction"></param>
            /// <returns></returns>
            public IReadOnlyList<Space> GetConnectingSpaces(EDirection4 direction)
            {
                switch (direction)
                {
                    case EDirection4.Down: return ConnectingSpaces_Down;
                    case EDirection4.Up: return ConnectingSpaces_Up;
                    case EDirection4.Left: return ConnectingSpaces_Left;
                    case EDirection4.Right: return ConnectingSpaces_Right;
                }
                return null;
            }



            //? 공간 연결 조작



            /// <summary>
            /// 이 공간과 연결되어있는 공간을 방향별로 나눈 리스트를 정렬한다 (상하: 좌우순, 좌우: 하상순)
            /// </summary>
            public void SortConnecting_Spaces()
            {
                ConnectingSpaces_Down.Sort((a, b) => a.SpaceRect.center.x.CompareTo(b.SpaceRect.center.x));
                ConnectingSpaces_Up.Sort((a, b) => a.SpaceRect.center.x.CompareTo(b.SpaceRect.center.x));
                ConnectingSpaces_Left.Sort((a, b) => a.SpaceRect.center.y.CompareTo(b.SpaceRect.center.y));
                ConnectingSpaces_Right.Sort((a, b) => a.SpaceRect.center.y.CompareTo(b.SpaceRect.center.y));
            }



            /// <summary>
            /// [병렬] 이 공간과 연결되어있는 공간을 방향별로 나눈 리스트를 정렬한다 (상하: 좌우순, 좌우: 하상순)
            /// </summary>
            public void SortConnecting_SpacesParallel()
            {
                Parallel.Invoke(
                    () => ConnectingSpaces_Down.Sort((a, b) => a.SpaceRect.center.x.CompareTo(b.SpaceRect.center.x)),
                    () => ConnectingSpaces_Up.Sort((a, b) => a.SpaceRect.center.x.CompareTo(b.SpaceRect.center.x)),
                    () => ConnectingSpaces_Left.Sort((a, b) => a.SpaceRect.center.y.CompareTo(b.SpaceRect.center.y)),
                    () => ConnectingSpaces_Right.Sort((a, b) => a.SpaceRect.center.y.CompareTo(b.SpaceRect.center.y))
                );
            }



            /// <summary>
            /// [비동기 + 병렬] 이 공간과 연결되어있는 공간을 방향별로 나눈 리스트를 정렬한다 (상하: 좌우순, 좌우: 하상순)
            /// </summary>
            public async UniTask SortConnecting_SpacesParallelAsync()
            {
                // (1) 병렬 작업 생성
                var downTask = UniTask.RunOnThreadPool(() => ConnectingSpaces_Down.Sort((a, b) => a.SpaceRect.center.x.CompareTo(b.SpaceRect.center.x)));
                var upTask = UniTask.RunOnThreadPool(() => ConnectingSpaces_Up.Sort((a, b) => a.SpaceRect.center.x.CompareTo(b.SpaceRect.center.x)));
                var leftTask = UniTask.RunOnThreadPool(() => ConnectingSpaces_Left.Sort((a, b) => a.SpaceRect.center.y.CompareTo(b.SpaceRect.center.y)));
                var rightTask = UniTask.RunOnThreadPool(() => ConnectingSpaces_Right.Sort((a, b) => a.SpaceRect.center.y.CompareTo(b.SpaceRect.center.y)));

                // (2) 모든 작업 완료 대기
                await UniTask.WhenAll(downTask, upTask, leftTask, rightTask);
            }



            //? 두 공간을 연결하는 핵심 메서드



            ///<summary>
            /// 공간A, 공간B의 정보를 비교하여, 연결이 가능하다면 연결시킨다
            /// <para><i>(두 공간이 모두 조건을 충족해야한다)</i></para>
            ///2개의 공간이 모두 조건을 충족해야 true를 반환하고, 반환 전에 각 방향의 CurrentConnectingCount도 더한다
            ///</summary>
            public static bool TryConnectTwoSpaces(Space spaceA, Space spaceB)
            {
                //! 이미 두 공간이 연결되어있다면, 실패한다
                if (spaceA.totalConnectingSpaces.Contains(spaceB)) { return false; }

                //! 각 공간의 Rect고 자시고 하기 이전에, 애초에 하나라도 더 노드 추가가 불가능하다면, 실패한다
                if (!spaceA.PossibleAddMoreNode || !spaceB.PossibleAddMoreNode) { return false; }


                //. "공간A를 기준" 으로, 공간B가 어떤 방향에 있는지를 구하고,
                //. 그 방향에 연결이 가능한지 노드 개수를 확인해, 가능 여부를 반환한다
                bool isPossible_SpaceAtoB = getDirecionToTargetSpace_AndPossible(spaceA, spaceB, out var directionSpaceAtoB);

                //! 공간A와 공간B가 연결이 불가능하다면, 실패한다
                if (!isPossible_SpaceAtoB) { return false; }


                //. "공간B를 기준" 으로, 공간A가 어떤 방향에 있는지를 구하고,
                //. 그 방향에 연결이 가능한지 노드 개수를 확인해, 가능 여부를 반환한다
                bool isPossible_SpaceBtoA = getDirecionToTargetSpace_AndPossible(spaceB, spaceA, out var directionSpaceBtoA);

                //! 공간B와 공간A가 연결이 불가능하다면, 실패한다
                if (!isPossible_SpaceBtoA) { return false; }


                //. 까다로운 여러 조건들을 통과해 검수가 끝났으니, 각각 리스트에 추가한다,
                //. 즉 공간A, 공간B를 서로 반대되는 방향의 공간 연결 리스트에 추가하여, 공간을 이어준다
                spaceA.GetConnectingSpacesInternal(directionSpaceAtoB).Add(spaceB);
                spaceA.totalConnectingSpaces.Add(spaceB);

                spaceB.GetConnectingSpacesInternal(directionSpaceBtoA).Add(spaceA);
                spaceB.totalConnectingSpaces.Add(spaceA);



                return true;


                /// <summary>
                /// 이 공간과 <paramref name="targetSpace"/>를 비교하여,
                /// <para><paramref name="targetSpace"/>가 어떤 방이 공간을 기준으로, 어떤 방향에 있는지 구한뒤</para>
                /// <para>그 방향에 추가가 가능한지 방향과 함께 반환</para>
                /// </summary>
                static bool getDirecionToTargetSpace_AndPossible(Space space, Space targetSpace, out EDirection4 resultDirection)
                {
                    //. 이 공간을 기준으로, targetSpace 가 어떤 방향에 있는지 구한다
                    resultDirection = SU_TF_Rect.GetRelativeDirection(space.spaceRect, targetSpace.spaceRect);

                    //. 그 방향에 해당하는 방향의 현재/최대 노드 개수를 구해, 더 추가가 가능한지의 여부를 반환한다
                    switch (resultDirection)
                    {
                        case EDirection4.Down: return (space.CurrentNodeCount_Down < space.MaxNodeCount_Down);
                        case EDirection4.Up: return (space.CurrentNodeCount_Up < space.MaxNodeCount_Up);
                        case EDirection4.Left: return (space.CurrentNodeCount_Left < space.MaxNodeCount_Left);
                        case EDirection4.Right: return (space.CurrentNodeCount_Right < space.MaxNodeCount_Right);
                        default: return false;
                    }
                }
            }



            #endregion



            ///======================================================================================================================================================


#if UNITY_EDITOR
            ///<summary>
            ///공간 정보 요약을 문자열로 얻기 (에디터에서 사용)
            /// </summary>
            public string GetSpaceInfoText_ForEditor()
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine(dummyTitle_SpacVectorInfo);
                sb.AppendLine();
                sb.AppendLine(dummy_NodeInfo);
                sb.AppendLine();
                sb.AppendLine(dummy_NeighborSpaceInfo);
                sb.AppendLine(dummy_ConnectingSpaceInfo);


                string result = sb.ToString(true);
                sb.Clear();

                return result;
            }
#endif


            ///======================================================================================================================================================
        }
    }
}