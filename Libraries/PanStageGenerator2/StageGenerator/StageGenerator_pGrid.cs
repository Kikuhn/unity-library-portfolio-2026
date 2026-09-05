using System.Collections.Generic;
using UnityEngine;
using Pan.Util;
using System;
using Pan.GridCompatibles2;
using Pan.StageGenerators;
using System.Text;
using Sirenix.OdinInspector;
using System.Runtime.CompilerServices;



namespace Pan.StageGenerators
{
    public partial class StageGenerator
    {
        /// <summary>
        /// <see cref="StageGenerator"/> 에서 사용되는 그리드
        /// </summary>
        [Serializable]
        public partial class Grid : MainSlaveSerialized<StageGenerator>
        {
            ///======================================================================================================================================================



            public Grid(StageGenerator main, int x, int y) : base(main)
            {
                //? 커스텀이벤트: 그리드 생성 이전
                main.Setting.CustomEvent.CreateGrid?.Before_CreateGrid(main, this);


                gridPositionFixed = new Vector2Int(x, y);


                Refresh_WorldPosition();


                //. IsOccupied_ValueControlManager의 getter에서 에디터에서의 반직렬화를 위해 그곳에서도 초기화 코드가 존재
                isOccupied_ValueControlManager ??= new ValueControlManager<bool>((oldV, newV) => { isOccupied = newV; }, false);


                //? 커스텀이벤트: 그리드 생성 이후
                main.Setting.CustomEvent.CreateGrid?.After_CreateGrid(main, this);
            }



            ///======================================================================================================================================================



            /// <summary>
            /// 그리드 설정 초기화
            /// </summary>
            [Button("그리드 설정 초기화")]
            public void ResetGridSettings()
            {
                CurrentSummonedObject = null;
                ClearTags(); //. 태그 정보 초기화
                pathCost = GridManager.PATHCOST_DEFAULT; //. 경로 비용 정보 초기화
                GridDebuggingMemo = string.Empty; //. 디버깅 메모 정보 초기화
                IsOccupied_ValueControlManager.Reset(false); // 점유 정보 초기화
                //isOccupied = false;
            }



            ///======================================================================================================================================================



#if UNITY_EDITOR



            [ShowInInspector, HideLabel, DisplayAsString(EnableRichText = true, Overflow = false, Alignment = TextAlignment.Left), EnableGUI]
            [BoxGroup("박스", false), HorizontalGroup("박스/가로", 0.25f)]
            [PropertyOrder(-999)]
            private string dummy_GridInfo
            {
                get
                {
                    var worldPos = WorldPosition_Calculate;

                    return $"<size=14><color=#2ecc71>[{gridPositionFixed.x}, {gridPositionFixed.y}]</color></size><size=13>\nX: <color=#f7da64>{worldPos.x}</color>\nY: <color=#f7da64>{worldPos.y}</color>\nZ: <color=#f7da64>{worldPos.z}</color></size>";
                }
            }



#endif



            ///======================================================================================================================================================



            //? 좌표



            ///<summary>
            ///그리드 좌표 (Vector2Int, 고정)
            ///</summary>
            public Vector2Int GridPositionFixed => gridPositionFixed;
            [HideInInspector][ReadOnly][SerializeField] private Vector2Int gridPositionFixed;



            ///<summary>
            ///그리드 월드좌표 (캐싱)
            ///</summary>
            public Vector3 WorldPosition_Cached => worldPosition_Cached;
            [HideInInspector][ReadOnly][SerializeField] private Vector3 worldPosition_Cached;



            ///<summary>
            ///그리드 월드좌표 (얻어 올 때 마다 갱신하여 반환)
            ///</summary>
            public Vector3 WorldPosition_Calculate
            {
                get
                {
                    Refresh_WorldPosition();
                    return worldPosition_Cached;
                }
            }



            /// <summary>
            /// <see cref="StageGenerator"/>의 정보에 맞춰, <see cref="WorldPosition_Cached"/> 을 갱신한다
            /// </summary>
            public void Refresh_WorldPosition()
            {
                if (Main == null) { return; }
                worldPosition_Cached = Main.TransformM.ConvertGridPositionToTransformPosition(gridPositionFixed, true);
            }



            ///======================================================================================================================================================



            //? 그리드 상태



            #region 점유 상태



            ///<summary>
            ///이 그리드의 점유 여부 (<see cref="ValueControlManager{bool}"/> 로 관리됨)<br/>
            ///setter 시도시, 0번 레이어로 중첩되어 적용됨
            ///</summary>
            [HorizontalGroup("박스/가로"), VerticalGroup("박스/가로/우측")]
            [BoxGroup("박스/가로/우측/점유")]
            [HorizontalGroup("박스/가로/우측/점유/점유정보가로", 0.3f)]
            [ShowInInspector]
            [Sirenix.OdinInspector.ReadOnly]
            [LabelText("점유")]
            [LabelWidth(40)]
            [PropertyOrder(1)]
            public bool IsOccupied
            {
                get => isOccupied;
                set
                {
                    SetOccupied(value, 0, true);
                }
            }
            [HideInInspector][SerializeField] private bool isOccupied = false;



            /// <summary>
            /// 점유 여부 밸류 컨트롤 매니저
            /// </summary>
            [HorizontalGroup("박스/가로"), VerticalGroup("박스/가로/우측")]
            [BoxGroup("박스/가로/우측/점유")]
            [ShowInInspector]
            [EnableGUI]
            [Sirenix.OdinInspector.ReadOnly]
            [LabelText("점유 ValueControl")]
            [PropertyOrder(1)]
            private ValueControlManager<bool> IsOccupied_ValueControlManager
            {
                get
                {
                    isOccupied_ValueControlManager ??= new ValueControlManager<bool>((oldV, newV) => { isOccupied = newV; }, false);
                    return isOccupied_ValueControlManager;
                }
            }
            [SerializeField][HideInInspector] private ValueControlManager<bool> isOccupied_ValueControlManager;



            /// <summary>
            /// 가장 높은 점유중인 레이어
            /// </summary>
            /// <returns></returns>
            [HorizontalGroup("박스/가로"), VerticalGroup("박스/가로/우측")]
            [BoxGroup("박스/가로/우측/점유")]
            [HorizontalGroup("박스/가로/우측/점유/점유정보가로")]
            [ShowInInspector]
            [EnableGUI]
            [DisplayAsString]
            [Sirenix.OdinInspector.ReadOnly]
            [LabelText("최상 레이어")]
            [LabelWidth(80)]
            [PropertyOrder(1)]
            public int IsOccupied_GetHighestLayer
            {
                get
                {
                    return IsOccupied_ValueControlManager.GetHighstChip;
                }
            }



            /// <summary>
            /// 점유 상태 변경 (레이어)
            /// </summary>
            public bool SetOccupied(bool value, int occupiedLayer = 0, bool overlap = false)
            {
                return IsOccupied_ValueControlManager.SetValue(occupiedLayer, value, overlap);
            }



            /// <summary>
            /// 점유 상태 제거 (레이어)
            /// </summary>
            public bool RemoveOccupied(int keyRank)
            {
                return IsOccupied_ValueControlManager.RemoveValue(keyRank);
            }



            /// <summary>
            /// 점유 상태 변경 (레이어)
            /// </summary>
            public bool SetOccupied(object controller, bool value, int occupiedLayer = 0, bool overlap = false)
            {
                return IsOccupied_ValueControlManager.SetValue(controller, occupiedLayer, value, overlap);
            }



            /// <summary>
            /// 점유 상태 제거 (레이어)
            /// </summary>
            public bool RemoveOccupied(object controller, int keyRank)
            {
                return IsOccupied_ValueControlManager.RemoveValue(controller, keyRank);
            }



            #endregion



            #region 경로 비용



            ///<summary>
            ///경로 비용<br/>
            ///(패스파인딩시, 값이 높을수록 이 그리드를 되도록 기피함)
            ///</summary>
            [HorizontalGroup("박스/가로"), VerticalGroup("박스/가로/우측")]
            [BoxGroup("박스/가로/우측/경로 비용", false)]
            [ShowInInspector]
            [LabelText("경로 비용")]
            [PropertyOrder(2)]
            public int PathCost
            {
                get => pathCost;
                set
                {
                    pathCost = Mathf.Clamp(value, 0, int.MaxValue);
                }
            }
            [HideInInspector][SerializeField] private int pathCost = GridManager.PATHCOST_DEFAULT;



            ///<summary>
            ///PathCost 조작
            /// </summary>
            /// <param name="addPathCostValue">더할 PathCost 값</param>
            /// <param name="remainPathCost">PathCost가 음수가 되어버려서, 0으로 초기화되었을때, 버려진 음수 값</param>
            public int ControlPathCost(int addPathCostValue, out int? remainPathCost)
            {
                int temp = PathCost + addPathCostValue;

                if (temp < 0)
                {
                    remainPathCost = temp;
                    temp = 0;
                }
                else
                {
                    remainPathCost = null;
                }

                PathCost = temp;

                return PathCost;
            }



            #endregion



            ///======================================================================================================================================================



            //? 그리드 태그



            [SerializeField] private GridTag flagTags = GridTag.None;
            [HorizontalGroup("박스/가로"), VerticalGroup("박스/가로/우측")]
            [BoxGroup("박스/가로/우측/태그")]
            [ShowInInspector, LabelText("태그 Flag 목록")]
            [LabelWidth(120)]
            [PropertyOrder(3)]
            public GridTag FlagTags
            {
                get => flagTags;
                set
                {
                    //. 추가 · 제거된 비트 계산
                    GridTag added = value & ~flagTags; //? 새로 ON
                    GridTag removed = flagTags & ~value; //! OFF

                    if (added != GridTag.None) flagTagsCount += CountBits(added);
                    if (removed != GridTag.None) flagTagsCount -= CountBits(removed);

                    //. 최종 적용
                    flagTags = value;
                }
            }



            [HorizontalGroup("박스/가로"), VerticalGroup("박스/가로/우측")]
            [BoxGroup("박스/가로/우측/태그")]
            [DisplayAsString, Sirenix.OdinInspector.ReadOnly, EnableGUI]
            [ShowInInspector, LabelText("적용된 태그 개수")]
            [LabelWidth(100)]
            [PropertyOrder(3)]
            private int flagTagsCount;
            public int FlagTagsCount => flagTagsCount;



            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int CountBits(GridTag mask)
            {
                ulong v = (ulong)mask;
                int c = 0;
                while (v != 0) { v &= v - 1; c++; }   //. Brian Kernighan 팝카운트 (변한 비트 수 만큼 loop)
                return c;
            }



            /// <summary>
            /// 받아온 <see cref="GridTag"/>(들)이 모두 포함되어있는지 확인
            /// </summary>
            /// <param name="tagMask"></param>
            /// <returns></returns>
            public bool ContainsTag(GridTag tagMask) =>
                (flagTags & tagMask) == tagMask;

            /// <summary>
            /// 받아온 <see cref="GridTag"/>(들) 중, 하나라도 포함되어있는지 확인
            /// </summary>
            /// <param name="tagMask"></param>
            /// <returns></returns>
            public bool AnyTag(GridTag tagMask) =>
                (flagTags & tagMask) != 0;

            public bool AddTag(GridTag mask)
            {
                //. 새로 켜질 비트만 추출
                GridTag diff = mask & ~flagTags;

                //. 이미 전부 포함 → 변화 없음
                if (diff == GridTag.None) return false;

                //. 적용
                flagTags |= diff;

                //. 변한 비트 수만큼 +1
                flagTagsCount += CountBits(diff);



                return true;
            }

            public bool RemoveTag(GridTag mask)
            {
                //. 실제로 꺼질 비트
                GridTag diff = mask & flagTags;

                //. 제거할 게 없음
                if (diff == GridTag.None) return false;

                //. 제거
                flagTags &= ~diff;

                //. 변한 비트 수만큼 -1
                flagTagsCount -= CountBits(diff);

                return true;
            }

            public void ClearTags()
            {
                flagTags = GridTag.None;
                flagTagsCount = 0;
            }



            #region 그리드 태그 정보



            #region Legacy 태그 리스트



            /////<summary>이 그리드의 태그 리스트<br/>
            /////(문자열 기반, 중복제외 여러개 보유 가능)
            /////</summary>
            //[HorizontalGroup("박스/가로"), VerticalGroup("박스/가로/우측")]
            //[BoxGroup("박스/가로/우측/태그")]
            //[ShowInInspector]
            //[LabelText("태그 목록")]
            //[SerializeField]
            //[PropertyOrder(3)]
            //private List<string> TagList;



            //public IReadOnlyList<string> GetTagList => TagList;



            /////<summary>
            /////태그 추가 (중복X)
            /////</summary>
            //public bool AddTag(string tag)
            //{
            //    if (ContainsTag(tag)) { return false; }
            //    TagList.Add(tag);
            //    return ApplyGridSetting(tag, true);
            //}



            /////<summary>
            /////태그 추가 (중복X)
            /////</summary>
            //public void AddTags(IEnumerable<string> tags)
            //{
            //    foreach (var tag in tags)
            //    {
            //        AddTag(tag);
            //    }
            //}



            /////<summary>
            /////태그 제거
            /////</summary>
            //public bool RemoveTag(string tag)
            //{
            //    if (!ContainsTag(tag)) { return false; }
            //    TagList.Remove(tag);
            //    ApplyGridSetting(tag, false);
            //    return true;
            //}



            /////<summary>
            /////태그 제거
            /////</summary>
            //public void RemoveTags(params string[] tags)
            //{
            //    for (int i = 0; i < tags.Length; i++)
            //    {
            //        RemoveTag(tags[i]);
            //    }
            //}



            /////<summary>
            /////태그 제거
            /////</summary>
            //public void RemoveTags(IEnumerable<string> tags)
            //{
            //    foreach (var tag in tags)
            //    {
            //        RemoveTag(tag);
            //    }
            //}



            /////<summary>
            /////태그 전부 제거
            /////</summary>
            //public void ClearTags()
            //{
            //    TagList.Clear();
            //}



            /////<summary>
            /////태그가 하나라도 있다면 true, 없다면 false
            /////</summary>
            //public bool ContainsTag()
            //{
            //    return TagList.Count != 0;
            //}



            /////<summary>
            /////받아온 태그가 있다면 true, 없다면 false
            /////</summary>
            //public bool ContainsTag(string tag)
            //{
            //    return TagList.Contains(tag);
            //}



            /////<summary>
            /////받아온 태그 배열이 모두 있다면 true, 하나라도 없다면 false
            /////</summary>
            //public bool ContainsTags(params string[] tags)
            //{
            //    foreach (var t in tags)
            //    {
            //        if (!TagList.Contains(t)) { return false; }
            //    }

            //    return true;
            //}



            ///// <summary>
            ///// 받아온 태그의 배열중에 하나라도 존재하면 즉시 true 반환
            ///// </summary>
            //public bool CheckOneTags(params string[] tags)
            //{
            //    foreach (var t in tags)
            //    {
            //        if (TagList.Contains(t)) { return true; }
            //    }

            //    return false;
            //} 


            //? 그리드 태그의 이벤트 적용



            ///// <summary>
            ///// <see cref="ExpandSetting"/>을 적용한다<br/>
            ///// <paramref name="active"/>에 따라 <see cref="ExpandSetting"/>에 있는 <see cref="ExpandSetting.EnableGrid(Grid)"/> 또는 <see cref="ExpandSetting.DisableGrid(Grid)"/> 가 적용된다
            ///// </summary>
            //private bool ApplyGridSetting(string gridSettingName, bool active)
            //{
            //    //? 해당 설정을 찾을수 없다면 실패
            //    if (!Main.Setting.StageGrid.TryGetGridExpandSetting(gridSettingName, out var gridSetting)) { return false; }

            //    return active ? gridSetting.EnableGrid(this) : gridSetting.DisableGrid(this);
            //}

            #endregion



            #endregion



            ///======================================================================================================================================================



            ///<summary>
            ///이 그리드 위에 소환된 그리드 호환 오브젝트
            ///</summary>
            [HorizontalGroup("박스/가로"), VerticalGroup("박스/가로/우측")]
            [BoxGroup("박스/가로/우측/기타", false)]
            [ShowInInspector]
            [LabelText("소환된 GO")]
            [PropertyOrder(4)]
            public GridCompatible2Object CurrentSummonedObject
            {
                get
                {
                    return currentSummonedObject;
                }
                set
                {
                    currentSummonedObject = value;
                }
            }
            [HideInInspector][SerializeField] private GridCompatible2Object currentSummonedObject;



            /// <summary>
            /// 디버깅용 메모 문자열
            /// </summary>
            [HorizontalGroup("박스/가로"), VerticalGroup("박스/가로/우측")]
            [BoxGroup("박스/가로/우측/기타", false)]
            [ShowInInspector]
            [LabelText("디버깅용 메모")]
            [PropertyOrder(4)]
            public string GridDebuggingMemo;



            ///======================================================================================================================================================



            ///<summary>
            ///그리드 정보 요약을 문자열로 얻기 (에디터에서 사용)
            /// </summary>
            public string GetGridInfoText_ForEditor(bool simple)
            {
                StringBuilder sb = SU_String.GetStringBuilderPublic();

                var gridWorldPosition = WorldPosition_Cached;

                if (simple)
                {
                    sb.AppendLine($"[{gridPositionFixed.x}, {gridPositionFixed.y}]");
                    sb.AppendLine($"{gridWorldPosition}");

                    sb.AppendLine($"점유/비용: {IsOccupied}, {PathCost}");
                    sb.AppendLine($"점유 가장 높은 레이어: {IsOccupied_GetHighestLayer}");

                    if (GridDebuggingMemo != null && GridDebuggingMemo != "")
                    {
                        sb.AppendLine($"디버깅 메모: {GridDebuggingMemo}");
                    }

                    if (flagTagsCount > 0)
                    {
                        sb.AppendLine($"태그: {flagTagsCount}개");
                        sb.AppendLine(flagTags.ToString().Replace(", ", ",\n"));
                        sb.Length -= 1; //. 줄바꿈 제거
                    }
                }
                else
                {
                    sb.AppendLine($"{"Grid:",-10}<color=#2ecc71><b>[{gridPositionFixed.x}, {gridPositionFixed.y}]</b></color>");

                    sb.AppendLine($"{"World:",-8}<color=#ac92ec><b>{gridWorldPosition}</b></color>");

                    if (IsOccupied)
                    {
                        sb.AppendLine($"{"점유:",-8}<color=#ed5565><b>점유</b></color>\t(Highest: <b><color=#f7da64>{IsOccupied_GetHighestLayer}</color></b>)");
                    }
                    else
                    {
                        sb.AppendLine($"{"점유:",-8}<color=#4fc1e9><b>미점유</b></color>\t(Highest: <b><color=#f7da64>{IsOccupied_GetHighestLayer}</color></b>)");
                    }

                    sb.AppendLine($"{"비용:",-8}<color=#f7da64><b>{PathCost}</b></color>");

                    if (GridDebuggingMemo != null && GridDebuggingMemo != "")
                    {
                        sb.AppendLine($"디버깅 메모: {GridDebuggingMemo}");
                    }

                    if (flagTagsCount > 0)
                    {
                        sb.AppendLine($"{"태그:",-8}<color=#2ecc71><b>{flagTagsCount}개</b></color>");
                        sb.AppendLine($"\t       <color=white><b>{flagTags.ToString()}</b></color>".Replace(", ", ",\n\t      "));
                        sb.Length -= 1; //. 줄바꿈 제거
                    }
                    //if (TagList != null)
                    //{
                    //    sb.AppendLine($"{"태그:",-8}(<color=#2ecc71><b>{TagList.Count}</b></color>)");
                    //    if (TagList.Count == 1)
                    //    {
                    //        sb.AppendLine($"\t{TagList[0]}");
                    //    }
                    //    else if (TagList.Count > 1)
                    //    {
                    //        sb.AppendLine($"\t{string.Join(",\n\t", TagList)}");
                    //    }
                    //}
                }

                string result = sb.ToString(true);
                sb.Clear();

                return result;
            }



            ///======================================================================================================================================================
        }



        #region Legacy 그리드 태그설정 등을 클래스로관리했던코드



        //// Expand Setting 베이스
        //public partial class Grid
        //{
        //    /// <summary>
        //    /// <see cref="StageGenerator"/> 에서 사용되는 그리드의 설정 확장
        //    /// </summary>
        //    [Serializable]
        //    public class ExpandSetting
        //    {
        //        ///======================================================================================================================================================


        //        public virtual string Tag
        //        {
        //            get => tag;
        //            protected set => tag = value;
        //        }
        //        [SerializeField, LabelText("태그")]
        //        private string tag;



        //        public bool UsePathCost
        //        {
        //            get => usePathCost;
        //            protected set => usePathCost = value;
        //        }
        //        [HorizontalGroup("PathCostGroup")]
        //        [SerializeField, LabelText("경로 Cost 사용"), LabelWidth(150)]
        //        private bool usePathCost = false;



        //        public int PathCost
        //        {
        //            get => pathCost;
        //            protected set => pathCost = value;
        //        }
        //        [HorizontalGroup("PathCostGroup")]
        //        [SerializeField, LabelText("경로"), EnableIf(nameof(UsePathCost))]
        //        private int pathCost = 0;



        //        public bool UseOccupied
        //        {
        //            get => useOccupied;
        //            protected set => useOccupied = value;
        //        }
        //        [HorizontalGroup("OccupiedGroup")]
        //        [SerializeField, LabelText("점유 사용"), LabelWidth(100)]
        //        private bool useOccupied = false;



        //        public bool Occupied
        //        {
        //            get => occupied;
        //            protected set => occupied = value;
        //        }
        //        [HorizontalGroup("OccupiedGroup")]
        //        [SerializeField, LabelText("점유 여부"), EnableIf(nameof(UseOccupied)), LabelWidth(80)]
        //        private bool occupied = false;



        //        public int OccupiedLayer
        //        {
        //            get => occupiedLayer;
        //            protected set => occupiedLayer = value;
        //        }
        //        [HorizontalGroup("OccupiedGroup")]
        //        [SerializeField, LabelText("점유 레이어"), EnableIf(nameof(UseOccupied)), LabelWidth(100)]
        //        private int occupiedLayer = 0;



        //        ///======================================================================================================================================================



        //        public bool EnableGrid(Grid grid)
        //        {
        //            if (EnableCondition(grid))
        //            {
        //                if (usePathCost) { grid.PathCost += PathCost; }
        //                if (useOccupied) { grid.SetOccupied(occupied, occupiedLayer, true); }
        //                //if (ExpandTags.Count != 0) { grid.AddTags(ExpandTags); }
        //                EnableEvent(grid);
        //                return true;
        //            }

        //            return false;
        //        }



        //        public bool DisableGrid(Grid grid)
        //        {
        //            if (DisableCondition(grid))
        //            {
        //                if (usePathCost) { grid.PathCost -= PathCost; }
        //                if (useOccupied) { grid.RemoveOccupied(occupiedLayer); }
        //                //if (ExpandTags.Count != 0) { grid.RemoveTags(ExpandTags); }
        //                DisableEvent(grid);
        //                return true;
        //            }

        //            return false;
        //        }



        //        ///======================================================================================================================================================



        //        protected virtual bool EnableCondition(Grid grid) => true;



        //        protected virtual bool DisableCondition(Grid grid) => true;



        //        protected virtual void EnableEvent(Grid grid) { }



        //        protected virtual void DisableEvent(Grid grid) { }



        //        ///======================================================================================================================================================
        //    }



        //    /// <summary>
        //    /// <see cref="ExpandSetting"/>의 추가버전<br/>
        //    /// 객체지향으로 각각 <see cref="ExpandSetting.EnableGrid(Grid)"/>, <see cref="ExpandSetting.DisableGrid(Grid)"/> 가능
        //    /// </summary>
        //    [Serializable]
        //    public abstract class ExpandSettingPlus : ExpandSetting { }



        //    /// <summary>
        //    /// 셀프 제네릭 확장용 클래스
        //    /// </summary>
        //    [Serializable]
        //    public abstract class ExpandSettingPlus<T> : ExpandSettingPlus where T : ExpandSettingPlus, new()
        //    {
        //        ///======================================================================================================================================================



        //        /// <summary>
        //        /// 매니저를 바탕으로, 즉시 객체 얻기
        //        /// </summary>
        //        public static T Get(ExpandSettingPlusManager manager)
        //        {
        //            return manager.Get<T>();
        //        }



        //        ///======================================================================================================================================================



        //        /// <summary>
        //        /// 매니저를 바탕으로, 즉시 객체 활성화
        //        /// </summary>
        //        public static bool EnableGrid(ExpandSettingPlusManager manager, Grid grid)
        //        {
        //            return Get(manager).EnableGrid(grid);
        //        }

        //        /// <summary>
        //        /// 매니저를 바탕으로, 즉시 객체 활성화
        //        /// </summary>
        //        public static bool EnableGrid(StageGenerator stageGenerator, Grid grid) => EnableGrid(stageGenerator.GridM.ExpandSettingPlusManager, grid);

        //        /// <summary>
        //        /// 매니저를 바탕으로, 즉시 객체 활성화
        //        /// </summary>
        //        public static bool EnableGrid(GridManager gridManager, Grid grid) => EnableGrid(gridManager.ExpandSettingPlusManager, grid);



        //        ///======================================================================================================================================================



        //        /// <summary>
        //        /// 매니저를 바탕으로, 즉시 객체 비활성화
        //        /// </summary>
        //        public static bool DisableGrid(ExpandSettingPlusManager manager, Grid grid)
        //        {
        //            return Get(manager).DisableGrid(grid);
        //        }

        //        /// <summary>
        //        /// 매니저를 바탕으로, 즉시 객체 비활성화
        //        /// </summary>
        //        public static bool DisableGrid(StageGenerator stageGenerator, Grid grid) => DisableGrid(stageGenerator.GridM.ExpandSettingPlusManager, grid);

        //        /// <summary>
        //        /// 매니저를 바탕으로, 즉시 객체 비활성화
        //        /// </summary>
        //        public static bool DisableGrid(GridManager gridManager, Grid grid) => DisableGrid(gridManager.ExpandSettingPlusManager, grid);



        //        ///======================================================================================================================================================
        //    }



        //    ///// <summary>
        //    ///// <see cref="ExpandSettingPlus"/>의 클래스들을 관리하는 매니저
        //    ///// </summary>
        //    //[Serializable]
        //    //public class ExpandSettingPlusManager
        //    //{
        //    //    public readonly AutoTypeDictionary<ExpandSettingPlus> Settings = new AutoTypeDictionary<ExpandSettingPlus>();

        //    //    public T Get<T>() where T : ExpandSettingPlus, new()
        //    //    {
        //    //        return Settings.Get<T>();
        //    //    }
        //    //}
        //} 



        #region 그 확장 코드

        // Expand Settings

        //public partial class Grid
        //{
        //    /////======================================================================================================================================================



        //    ////? 방



        //    ///// <summary> 
        //    ///// 방 설정
        //    ///// </summary>
        //    //[Serializable]
        //    //public class EX_Room_Main : ExpandSettingPlus<EX_Room_Main>
        //    //{
        //    //    public EX_Room_Main()
        //    //    {
        //    //        UseOccupied = true;
        //    //        Occupied = true;
        //    //        OccupiedLayer = 2;

        //    //        //AddExpandTag(GridManager.TAG_ROOM);
        //    //    }

        //    //    protected override bool EnableCondition(Grid grid)
        //    //    {
        //    //        return grid.IsOccupied == false;
        //    //    }
        //    //    protected override void EnableEvent(Grid grid)
        //    //    {
        //    //        base.EnableEvent(grid);
        //    //        grid.AddTag(GridTag.Room);
        //    //    }
        //    //}



        //    ///// <summary>
        //    ///// 방 안전구역 설정
        //    ///// </summary>
        //    //[Serializable]
        //    //public class EX_Room_Safe : ExpandSettingPlus<EX_Room_Safe>
        //    //{
        //    //    public EX_Room_Safe()
        //    //    {
        //    //        //AddExpandTag(GridManager.TAG_ROOM_SAFE);
        //    //    }

        //    //    protected override bool EnableCondition(Grid grid)
        //    //    {
        //    //        return !grid.ContainsTag(GridTag.Room);
        //    //    }
        //    //    protected override void EnableEvent(Grid grid)
        //    //    {
        //    //        base.EnableEvent(grid);
        //    //        grid.AddTag(GridTag.Room_Safe);
        //    //    }
        //    //}


        //    ///// <summary>
        //    ///// 방 확장 안전구역 설정 (공간안에 방을 배치할때 추가로 방에 더해지는 값
        //    ///// </summary>
        //    //[Serializable]
        //    //public class EX_Room_ExpandSafe : ExpandSettingPlus<EX_Room_ExpandSafe>
        //    //{
        //    //    public EX_Room_ExpandSafe()
        //    //    {
        //    //        //AddExpandTag(GridManager.TAG_ROOM_EXPANDSAFE);
        //    //    }

        //    //    protected override bool EnableCondition(Grid grid)
        //    //    {
        //    //        return !grid.ContainsAny(GridTag.Room | GridTag.Room_Safe);
        //    //        //return grid.CheckOneTags(GridManager.TAG_ROOM, GridManager.TAG_ROOM_SAFE) == false;
        //    //    }

        //    //    protected override void EnableEvent(Grid grid)
        //    //    {
        //    //        base.EnableEvent(grid);
        //    //        grid.AddTag(GridTag.Room_ExpandSafe);
        //    //    }
        //    //}



        //    ///// <summary>
        //    ///// 방 테두리 설정
        //    ///// </summary>
        //    //[Serializable]
        //    //public class EX_Room_Edge : ExpandSettingPlus<EX_Room_Edge>
        //    //{
        //    //    public EX_Room_Edge()
        //    //    {
        //    //        //AddExpandTag(GridManager.TAG_ROOM_EDGE);
        //    //    }

        //    //    protected override bool EnableCondition(Grid grid)
        //    //    {
        //    //        return !grid.ContainsAny(GridTag.Room | GridTag.Room_Door);
        //    //        //return grid.CheckOneTags(GridManager.TAG_ROOM, GridManager.TAG_ROOM_DOOR) == false;
        //    //    }

        //    //    protected override bool DisableCondition(Grid grid)
        //    //    {
        //    //        return EnableCondition(grid);
        //    //    }

        //    //    protected override void EnableEvent(Grid grid)
        //    //    {
        //    //        base.EnableEvent(grid);
        //    //        grid.AddTag(GridTag.Room_Edge);
        //    //    }
        //    //}



        //    /////======================================================================================================================================================



        //    ////? 방 도어



        //    ///// <summary>
        //    ///// 방 도어 설정
        //    ///// </summary>
        //    //[Serializable]
        //    //public class EX_Door : ExpandSettingPlus<EX_Door>
        //    //{
        //    //    public EX_Door()
        //    //    {
        //    //        //AddExpandTag(GridManager.TAG_ROOM_DOOR);
        //    //    }

        //    //    protected override void EnableEvent(Grid grid)
        //    //    {
        //    //        base.EnableEvent(grid);
        //    //        grid.AddTag(GridTag.Room_Door);
        //    //    }
        //    //}



        //    ///// <summary>
        //    ///// 방 도어 설정 (Start)
        //    ///// </summary>
        //    //[Serializable]
        //    //public class EX_Door_Start : ExpandSettingPlus<EX_Door_Start>
        //    //{
        //    //    public EX_Door_Start()
        //    //    {
        //    //        //AddExpandTag(GridManager.TAG_ROOM_DOOR_START);
        //    //    }
        //    //    protected override void EnableEvent(Grid grid)
        //    //    {
        //    //        base.EnableEvent(grid);
        //    //        grid.AddTag(GridTag.Room_Door_Start);
        //    //    }
        //    //}



        //    ///// <summary>
        //    ///// 방 도어 설정 (End)
        //    ///// </summary>
        //    //[Serializable]
        //    //public class EX_Door_End : ExpandSettingPlus<EX_Door_End>
        //    //{
        //    //    public EX_Door_End()
        //    //    {
        //    //        //AddExpandTag(GridManager.TAG_ROOM_DOOR_END);
        //    //    }

        //    //    protected override void EnableEvent(Grid grid)
        //    //    {
        //    //        base.EnableEvent(grid);
        //    //        grid.AddTag(GridTag.Room_Door_End);
        //    //    }
        //    //}



        //    ///// <summary>
        //    ///// 방 도어 설정 (영역 StartEnd)
        //    ///// </summary>
        //    //[Serializable]
        //    //public class EX_Door_Area_StartEnd : ExpandSettingPlus<EX_Door_Area_StartEnd>
        //    //{
        //    //    public EX_Door_Area_StartEnd()
        //    //    {
        //    //        //AddExpandTag(GridManager.TAG_ROOM_DOOR_AREA_STARTEND);
        //    //    }

        //    //    protected override void EnableEvent(Grid grid)
        //    //    {
        //    //        base.EnableEvent(grid);
        //    //        grid.AddTag(GridTag.Room_Door_Area_StartEnd);
        //    //    }
        //    //}



        //    ///// <summary>
        //    ///// 방 도어 설정 (영역 Area)
        //    ///// </summary>
        //    //[Serializable]
        //    //public class EX_Door_Area : ExpandSettingPlus<EX_Door_Area>
        //    //{
        //    //    public EX_Door_Area()
        //    //    {
        //    //        //AddExpandTag(GridManager.TAG_ROOM_DOOR_AREA);
        //    //    }

        //    //    protected override void EnableEvent(Grid grid)
        //    //    {
        //    //        base.EnableEvent(grid);
        //    //        grid.AddTag(GridTag.Room_Door_Area);
        //    //    }
        //    //}



        //    ///// <summary>
        //    ///// 방 도어 확장 설정 (영역 Area)
        //    ///// </summary>
        //    //[Serializable]
        //    //public class EX_Door_Area_Expand : ExpandSettingPlus<EX_Door_Area_Expand>
        //    //{
        //    //    public EX_Door_Area_Expand()
        //    //    {
        //    //        //AddExpandTag(GridManager.TAG_ROOM_DOOR_AREA_EXPAND);
        //    //    }

        //    //    protected override bool EnableCondition(Grid grid)
        //    //    {
        //    //        return !grid.ContainsTag(GridTag.Room_Door_Area);
        //    //        //return !grid.ContainsTag(GridManager.TAG_ROOM_DOOR_AREA);
        //    //    }
        //    //    protected override bool DisableCondition(Grid grid)
        //    //    {
        //    //        return !grid.ContainsTag(GridTag.Room_Door_Area);
        //    //        //return !grid.ContainsTag(GridManager.TAG_ROOM_DOOR_AREA);
        //    //    }

        //    //    protected override void EnableEvent(Grid grid)
        //    //    {
        //    //        base.EnableEvent(grid);
        //    //        grid.AddTag(GridTag.Room_Door_Area_Expand);
        //    //    }
        //    //}



        //    /////======================================================================================================================================================



        //    ////? 복도



        //    ///// <summary>
        //    ///// 메인 복도 설정
        //    ///// </summary>
        //    //[Serializable]
        //    //public class EX_Hallway_Main : ExpandSettingPlus<EX_Hallway_Main>
        //    //{
        //    //    public EX_Hallway_Main()
        //    //    {
        //    //        //AddExpandTag(GridManager.TAG_HALLWAY, GridManager.TAG_HALLWAY_PATH_MAIN);

        //    //        UseOccupied = true;
        //    //        Occupied = true;
        //    //        OccupiedLayer = 10;
        //    //    }

        //    //    protected override bool EnableCondition(Grid grid)
        //    //    {
        //    //        return !grid.ContainsAny(GridTag.Room | GridTag.Hallway);
        //    //        //return grid.CheckOneTags(GridManager.TAG_ROOM, GridManager.TAG_HALLWAY) == false;
        //    //    }

        //    //    protected override bool DisableCondition(Grid grid)
        //    //    {
        //    //        return grid.ContainsTag(GridTag.Hallway_Path_Main);
        //    //        //return grid.CheckOneTags(GridManager.TAG_HALLWAY_PATH_MAIN);
        //    //    }

        //    //    protected override void EnableEvent(Grid grid)
        //    //    {
        //    //        //base.EnableEvent(grid);
        //    //        grid.AddTag(GridTag.Hallway | GridTag.Hallway_Path_Main);
        //    //    }
        //    //}



        //    ///// <summary>
        //    ///// 확장 복도 설정
        //    ///// </summary>
        //    //[Serializable]
        //    //public class EX_Hallway_Expand : ExpandSettingPlus<EX_Hallway_Expand>
        //    //{
        //    //    public EX_Hallway_Expand()
        //    //    {
        //    //        //AddExpandTag(GridManager.TAG_HALLWAY, GridManager.TAG_HALLWAY_PATH_EXPAND);


        //    //        UseOccupied = true;
        //    //        Occupied = true;
        //    //        OccupiedLayer = 10;
        //    //    }

        //    //    protected override bool EnableCondition(Grid grid)
        //    //    {
        //    //        return !grid.ContainsAny(GridTag.Room | GridTag.Hallway_Path_Main);
        //    //        //return grid.CheckOneTags(GridManager.TAG_ROOM, GridManager.TAG_HALLWAY_PATH_MAIN) == false;
        //    //    }

        //    //    protected override bool DisableCondition(Grid grid)
        //    //    {
        //    //        return grid.ContainsTag(GridTag.Hallway_Path_Expand);
        //    //        //return grid.CheckOneTags(GridManager.TAG_HALLWAY_PATH_EXPAND);
        //    //    }

        //    //    protected override void EnableEvent(Grid grid)
        //    //    {
        //    //        base.EnableEvent(grid);
        //    //        grid.AddTag(GridTag.Hallway | GridTag.Hallway_Path_Expand);
        //    //    }
        //    //}



        //    ///// <summary>
        //    ///// 복도 테두리 설정
        //    ///// </summary>
        //    //[Serializable]
        //    //public class EX_Hallway_Edge : ExpandSettingPlus<EX_Hallway_Edge>
        //    //{
        //    //    public EX_Hallway_Edge()
        //    //    {
        //    //        //AddExpandTag(GridManager.TAG_HALLWAY_EDGE);

        //    //        UseOccupied = true;
        //    //        Occupied = true;
        //    //        OccupiedLayer = 10;
        //    //    }

        //    //    protected override bool EnableCondition(Grid grid)
        //    //    {
        //    //        //return !grid.CheckOneTags(GridManager.TAG_ROOM);
        //    //        //return !grid.CheckOneTags(GridManager.TAG_ROOM, GridManager.TAG_HALLWAY);
        //    //        return !grid.ContainsAny(GridTag.Room | GridTag.Hallway);

        //    //    }

        //    //    protected override bool DisableCondition(Grid grid)
        //    //    {
        //    //        //return grid.CheckOneTags(GridManager.TAG_HALLWAY, GridManager.TAG_HALLWAY_EDGE);
        //    //        return grid.ContainsAny(GridTag.Hallway | GridTag.Hallway_Edge);
        //    //    }

        //    //    protected override void EnableEvent(Grid grid)
        //    //    {
        //    //        base.EnableEvent(grid);
        //    //        grid.AddTag(GridTag.Hallway_Edge);
        //    //    }
        //    //}



        //    ///// <summary>
        //    ///// 복도 안전구역 설정
        //    ///// </summary>
        //    //[Serializable]
        //    //public class EX_Hallway_Safe : ExpandSettingPlus<EX_Hallway_Safe>
        //    //{
        //    //    public EX_Hallway_Safe()
        //    //    {
        //    //        //AddExpandTag(GridManager.TAG_HALLWAY_SAFE);
        //    //    }

        //    //    protected override bool EnableCondition(Grid grid)
        //    //    {
        //    //        return !grid.ContainsAny(GridTag.Room | GridTag.Hallway | GridTag.Hallway_Edge);
        //    //        //return !grid.CheckOneTags(GridManager.TAG_ROOM, GridManager.TAG_HALLWAY, GridManager.TAG_HALLWAY_EDGE);
        //    //    }

        //    //    protected override bool DisableCondition(Grid grid)
        //    //    {
        //    //        return grid.ContainsTag(GridTag.Hallway_Safe);
        //    //        //return grid.CheckOneTags(GridManager.TAG_HALLWAY_SAFE);
        //    //    }

        //    //    protected override void EnableEvent(Grid grid)
        //    //    {
        //    //        base.EnableEvent(grid);
        //    //        grid.AddTag(GridTag.Hallway_Safe);
        //    //    }
        //    //}



        //    ///// <summary>
        //    ///// 복도 테두리 설정 (도어 보정거리용)
        //    ///// </summary>
        //    //[Serializable]
        //    //public class EX_Hallway_DoorCorrection_Edge : ExpandSettingPlus<EX_Hallway_DoorCorrection_Edge>
        //    //{
        //    //    public EX_Hallway_DoorCorrection_Edge()
        //    //    {
        //    //        //AddExpandTag(GridManager.TAG_HALLWAY_EDGE, GridManager.TAG_HALLWAY_EDGE_DOORCORRECTION);

        //    //        UseOccupied = true;
        //    //        Occupied = true;
        //    //        OccupiedLayer = 10;
        //    //    }

        //    //    protected override bool EnableCondition(Grid grid)
        //    //    {
        //    //        //return !grid.CheckOneTags(GridManager.TAG_ROOM, GridManager.TAG_HALLWAY);
        //    //        return !grid.ContainsAny(GridTag.Room | GridTag.Hallway);
        //    //    }

        //    //    protected override bool DisableCondition(Grid grid)
        //    //    {
        //    //        //return grid.CheckOneTags(GridManager.TAG_HALLWAY_EDGE, GridManager.TAG_HALLWAY_EDGE_DOORCORRECTION);
        //    //        return grid.ContainsAny(GridTag.Hallway_Edge | GridTag.Hallway_Edge_DoorCorrection);
        //    //    }

        //    //    protected override void EnableEvent(Grid grid)
        //    //    {
        //    //        base.EnableEvent(grid);
        //    //        grid.AddTag(GridTag.Hallway_Edge | GridTag.Hallway_Edge_DoorCorrection);
        //    //    }
        //    //}



        //    ///// <summary>
        //    ///// 복도 안전구역 설정 (도어 보정거리용)
        //    ///// </summary>
        //    //[Serializable]
        //    //public class EX_Hallway_DoorCorrection_Safe : ExpandSettingPlus<EX_Hallway_DoorCorrection_Safe>
        //    //{
        //    //    public EX_Hallway_DoorCorrection_Safe()
        //    //    {
        //    //        //AddExpandTag(GridManager.TAG_HALLWAY_SAFE, GridManager.TAG_HALLWAY_SAFE_DOORCORRECTION);
        //    //    }

        //    //    protected override bool EnableCondition(Grid grid)
        //    //    {
        //    //        //return !grid.CheckOneTags(GridManager.TAG_ROOM, GridManager.TAG_HALLWAY, GridManager.TAG_HALLWAY_EDGE);
        //    //        return !grid.ContainsAny(GridTag.Room | GridTag.Hallway | GridTag.Hallway_Edge);
        //    //    }

        //    //    protected override bool DisableCondition(Grid grid)
        //    //    {
        //    //        //return grid.CheckOneTags(GridManager.TAG_HALLWAY_SAFE, GridManager.TAG_HALLWAY_SAFE_DOORCORRECTION);
        //    //        return grid.ContainsAny(GridTag.Hallway_Safe | GridTag.Hallway_Safe_DoorCorrection);
        //    //    }

        //    //    protected override void EnableEvent(Grid grid)
        //    //    {
        //    //        base.EnableEvent(grid);
        //    //        grid.AddTag(GridTag.Hallway_Safe | GridTag.Hallway_Safe_DoorCorrection);
        //    //    }
        //    //}



        //    /////======================================================================================================================================================
        //}

        #endregion



        #endregion

    }
}