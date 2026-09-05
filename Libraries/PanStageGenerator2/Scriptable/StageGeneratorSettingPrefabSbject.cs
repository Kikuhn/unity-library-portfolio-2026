using System.Collections;
using UnityEngine;
using System;
using System.Collections.Generic;
using Pan.Util;
using Pan.GridCompatibles2;
using Pan.StageGenerators;
using System.Text;
using Sirenix.OdinInspector;
using System.Linq;



namespace Pan.StageGenerators
{
    public interface IStageGeneratorSettingPrefab
    {
        StageGeneratorSettingPrefab Setting { get; }
    }



    [Serializable]
    public class StageGeneratorSettingPrefab : IStageGeneratorSettingPrefab, ICopyable<StageGeneratorSettingPrefab>
    {
        ///======================================================================================================================================================



        StageGeneratorSettingPrefab IStageGeneratorSettingPrefab.Setting => this;



        public void Copy(StageGeneratorSettingPrefab original)
        {
            gridCompatibleSnapSetting = original.gridCompatibleSnapSetting;

            roomPrefabList = new List<RoomObject>(original.roomPrefabList);
            hallwayPrefabList = new List<GridCompatible2Object>(original.hallwayPrefabList);
            hallwayEdgePrefabList = new List<GridCompatible2Object>(original.hallwayEdgePrefabList);
        }



        ///======================================================================================================================================================



        //? 그리드 스냅 설정



        [TitleGroup("그리드 스냅 설정")]
        [OnValueChanged(nameof(RefreshValid), true)]
        [LabelText("그리드 스냅 설정")]
        [InlineEditor]
        [SerializeField]
        [InfoBox("그리드 스냅 설정이 할당 되어 있어야, 설정이 가능", InfoMessageType.Warning, VisibleIf = "@!IsValid")]
        private GridCompatible2SnapSettingSbject gridCompatibleSnapSetting;

        public GridCompatible2SnapSettingSbject GridCompatibleSnapSettingSbject => gridCompatibleSnapSetting;

        public GridCompatible2SnapSetting GridCompatibleSnapSetting => gridCompatibleSnapSetting.GridCompatibleSnapSetting;



        ///======================================================================================================================================================



        //? 유효



        /// <summary>
        /// 스테이지 프리팹 설정의 유효성 여부
        /// <para>"그리드 스냅 설정의 할당 여부" 에 따라 결정된다</para>
        /// </summary>
        public bool IsValid
        {
            get
            {
                //! 에디터 환경에서는 항상 RefresValid되니, 에디터 환경이 아닐때만 갱신한다
#if !UNITY_EDITOR
            RefreshValid();
#endif
                return gridCompatibleSnapSetting != null;
            }
        }



        /// <summary>
        /// 스테이지 프리팹 설정의 가용 유효성 여부
        /// <para>"그리드 스냅 설정의 할당 여부" + "방 프리팹이 1개이상 존재하는지 여부" 에 따라 결정된다</para>
        /// </summary>
        public bool IsValidToUse => IsValid && roomPrefabList.Count > 0;



        ///======================================================================================================================================================



#if UNITY_EDITOR

        [TitleGroup("방 프리팹 목록"), BoxGroup("방 프리팹 목록/박스", false)]
        [ShowInInspector, HideLabel, DisplayAsString(EnableRichText = true, Overflow = false), EnableGUI]
        [ShowIf(nameof(IsValid))]
        private string dummy_RoomPrefabListInfo
        {
            get
            {
                if (roomPrefabList == null || roomPrefabList.Count == 0) return "";

                //. 극값 및 대응 방 목록 구하기
                TryGetRoomsByGridArea(EBoundary.Min, out var roomsMinArea, out var minArea);
                TryGetRoomsByGridArea(EBoundary.Max, out var roomsMaxArea, out var maxArea);
                TryGetRoomsByGridSize(EBoundary.Min, EDimension2D.Width, out var roomsMinWidth, out var minWidth);
                TryGetRoomsByGridSize(EBoundary.Max, EDimension2D.Width, out var roomsMaxWidth, out var maxWidth);
                TryGetRoomsByGridSize(EBoundary.Min, EDimension2D.Height, out var roomsMinHeight, out var minHeight);
                TryGetRoomsByGridSize(EBoundary.Max, EDimension2D.Height, out var roomsMaxHeight, out var maxHeight);

                var minSize_InRoomList = GetExtremeRoomGridDimensions(EBoundary.Min);
                var maxSize_InRoomList = GetExtremeRoomGridDimensions(EBoundary.Max);

                //. ──────────────────────────────── helpers
                string C(int v) => $"<color=#2ecc71><b>{v}x</b></color>";   // Colorize
                string JoinNames(List<RoomObject> list) =>
                    list == null || list.Count == 0
                        ? "-"
                        : string.Join(",\n\t", list.Select(r => $"<color=#f7da64>{r.name}</color>"));
                //------------------------------------------------------------------

                var sb = SU_String.GetStringBuilderPublic(true);


                //. 리스트 전체 최소/최대 (너비, 높이)
                sb.AppendLine($"리스트 최소 너비·높이: ({C(minSize_InRoomList.x)} × {C(minSize_InRoomList.y)})");
                sb.AppendLine($"리스트 최대 너비·높이: ({C(maxSize_InRoomList.x)} × {C(maxSize_InRoomList.y)})");

                sb.AppendLine();

                //. 넓이
                if (roomsMinArea?.Count > 0)
                    sb.AppendLine($"넓이 (Area)가 가장 작은 방 ({C(minArea)}) (<b><color=#ac92ec>{roomsMinArea.Count}</color></b>):\n\t{JoinNames(roomsMinArea)}");

                //sb.AppendLine();

                if (roomsMaxArea?.Count > 0)
                    sb.AppendLine($"넓이 (Area)가 가장 큰 방 ({C(maxArea)}) (<b><color=#ac92ec>{roomsMaxArea.Count}</color></b>):\n\t{JoinNames(roomsMaxArea)}");

                sb.AppendLine();

                //. 너비
                if (roomsMinWidth?.Count > 0)
                    sb.AppendLine($"너비 (Width)가 가장 작은 방 ({C(minWidth)}) (<b><color=#ac92ec>{roomsMinWidth.Count}</color></b>):\n\t{JoinNames(roomsMinWidth)}");

                //sb.AppendLine();

                if (roomsMaxWidth?.Count > 0)
                    sb.AppendLine($"너비 (Width)가 가장 큰 방 ({C(maxWidth)}) (<b><color=#ac92ec>{roomsMaxWidth.Count}</color></b>):\n\t{JoinNames(roomsMaxWidth)}");

                sb.AppendLine();

                //. 높이
                if (roomsMinHeight?.Count > 0)
                    sb.AppendLine($"높이 (Height)가 가장 작은 방 ({C(minHeight)}) (<b><color=#ac92ec>{roomsMinHeight.Count}</color></b>):\n\t{JoinNames(roomsMinHeight)}");

                //sb.AppendLine();

                if (roomsMaxHeight?.Count > 0)
                    sb.AppendLine($"높이 (Height)가 가장 큰 방 ({C(maxHeight)}) (<b><color=#ac92ec>{roomsMaxHeight.Count}</color></b>):\n\t{JoinNames(roomsMaxHeight)}");

                //sb.AppendLine();

                return sb.ToString(true);
            }
        }

#endif



        [SerializeField]
        [TitleGroup("방 프리팹 목록")]
        [LabelText("방 프리팹 목록")]
        [InfoBox("방 생성시 기준이 되는 RoomObject들이 들어있는 목록")]
        [ShowIf(nameof(IsValid))]
        [OnValueChanged(nameof(CheckValid_RoomObjectPrefabList))]
        [OnCollectionChanged(nameof(CheckValid_RoomObjectPrefabList))]
        [AssetsOnly]
        [InlineEditor]
        private List<RoomObject> roomPrefabList = new List<RoomObject>();
        public List<RoomObject> RoomPrefabList => roomPrefabList;



        [SerializeField]
        [TitleGroup("복도 프리팹 목록")]
        [LabelText("복도 프리팹 목록")]
        [InfoBox("방을 이어줄때 생성이 되는 복도 오브젝트\n반드시 그리드 호환 오브젝트 크기가 1x1 이어야 한다")]
        [ShowIf(nameof(IsValid))]
        [OnValueChanged(nameof(CheckValid_HallyPrefabList))]
        [OnCollectionChanged(nameof(CheckValid_HallyPrefabList))]
        [AssetsOnly]
        [InlineEditor]
        private List<GridCompatible2Object> hallwayPrefabList = new List<GridCompatible2Object>();
        public List<GridCompatible2Object> HallwayPrefabList => hallwayPrefabList;



        [SerializeField]
        [TitleGroup("복도 테두리 프리팹 목록")]
        [LabelText("복도 테두리 프리팹 목록")]
        [InfoBox("방을 이어줄때 생성이 되는 복도 오브젝트\n반드시 그리드 호환 오브젝트 크기가 1x1 이어야 한다")]
        [ShowIf(nameof(IsValid))]
        [OnValueChanged(nameof(CheckValid_HallyWallPrefabList))]
        [OnCollectionChanged(nameof(CheckValid_HallyWallPrefabList))]
        [AssetsOnly]
        [InlineEditor]
        private List<GridCompatible2Object> hallwayEdgePrefabList = new List<GridCompatible2Object>();
        public List<GridCompatible2Object> HallwayEdgePrefabList => hallwayEdgePrefabList;



        ///======================================================================================================================================================



        //? 리스트 유효성 검사



        ///<summary>
        /// 유효성 검사 갱신
        /// </summary>
        public void RefreshValid(StageGeneratorSettingPrefabSbject so = null)
        {
            CheckValid_RoomObjectPrefabList();
            CheckValid_HallyPrefabList();
            CheckValid_HallyWallPrefabList();
        }



        /// <summary>
        /// 같은 그리드 스냅 설정을 사용하고있는지 비교한다
        /// </summary>
        /// <param name="gridCompatibleObject"></param>
        public bool CheckValid_CheckGridSnapInternal(GridCompatible2Object gridCompatibleObject, StageGeneratorSettingPrefabSbject so = null)
        {
            //! 스냅설정 자체가 없을경우 그냥 허용한다
            if (gridCompatibleSnapSetting == null) { return true; }

            return (gridCompatibleObject.GridCompatible.GetSnapSettingExternal == gridCompatibleSnapSetting);

            #region legacy
            //var currentSnapSetting = gridCompatibleSnapSetting.GridCompatibleSnapSetting;
            //var targetSnapSetting = gridCompatibleObject.GridCompatible.CurrentSnapSetting;

            //if ((currentSnapSetting.Swizzle != targetSnapSetting.Swizzle)||
            //    (currentSnapSetting.GridUnitOriginalVector3 != targetSnapSetting.GridUnitOriginalVector3)
            //    )
            //{

            //} 
            #endregion
        }



        public void CheckValid_RoomObjectPrefabList(StageGeneratorSettingPrefabSbject so = null)
        {
            for (int i = roomPrefabList.Count - 1; i >= 0; i--)
            {
                var obj = roomPrefabList[i];


                //! null이라면 목록에서 제거
                if (obj == null)
                {
                    roomPrefabList.RemoveAt(i);
                    continue;
                }


                //! 그리드 스냅 설정과 맞지 않다면 목록에서 제거
                if (!CheckValid_CheckGridSnapInternal(obj))
                {
                    Debug.LogError($"\"<b>{obj.name}</b>\" 오브젝트의 그리드 스냅 설정이 \"<b>{gridCompatibleSnapSetting.name}</b>\" 을 사용하지 않아, 목록에서 제거됨");
                    roomPrefabList.RemoveAt(i);
#if UNITY_EDITOR
                    if (so != null) { UnityEditor.EditorUtility.SetDirty(so); }
#endif
                    continue;
                }


                //! 유효하지 않다면 목록에서 제거
                if (!obj.IsValid_RoomObject)
                {
                    Debug.LogError($"\"<b>{obj.name}</b>\" 오브젝트가 유효하지 않아, 목록에서 제거됨");
                    roomPrefabList.RemoveAt(i);
#if UNITY_EDITOR
                    if (so != null) { UnityEditor.EditorUtility.SetDirty(so); }
#endif
                    continue;
                }
            }
        }



        public void CheckValid_HallyPrefabList(StageGeneratorSettingPrefabSbject so = null)
        {
            for (int i = hallwayPrefabList.Count - 1; i >= 0; i--)
            {
                var obj = hallwayPrefabList[i];


                //! null이라면 목록에서 제거
                if (obj == null)
                {
                    hallwayPrefabList.RemoveAt(i);
#if UNITY_EDITOR
                    if (so != null) { UnityEditor.EditorUtility.SetDirty(so); }
#endif
                    continue;
                }


                //! 그리드 스냅 설정과 맞지 않다면 목록에서 제거
                if (!CheckValid_CheckGridSnapInternal(obj))
                {
                    Debug.LogError($"\"<b>{obj.name}</b>\" 오브젝트가 그리드 스냅 설정이 \"<b>{gridCompatibleSnapSetting.name}</b>\" 을 사용하지 않아, 목록에서 제거됨");
                    hallwayPrefabList.RemoveAt(i);
#if UNITY_EDITOR
                    if (so != null) { UnityEditor.EditorUtility.SetDirty(so); }
#endif
                    continue;
                }


                //! 크기가 1x1이 아니라면 목록에서 제거
                if (obj.GridCompatible.ObjectSizeX_Width != 1 || obj.GridCompatible.ObjectSizeY_Height != 1)
                {
                    Debug.LogError($"\"<b>{obj.name}</b>\" 복도 오브젝트에 1x1 크기가 아닌 오브젝트가 할당되어, 목록에서 제거됨");
                    hallwayPrefabList.RemoveAt(i);
#if UNITY_EDITOR
                    if (so != null) { UnityEditor.EditorUtility.SetDirty(so); }
#endif
                    continue;
                }
            }
        }



        public void CheckValid_HallyWallPrefabList(StageGeneratorSettingPrefabSbject so = null)
        {
            for (int i = hallwayEdgePrefabList.Count - 1; i >= 0; i--)
            {
                var obj = hallwayEdgePrefabList[i];


                //! null이라면 목록에서 제거
                if (obj == null)
                {
                    hallwayEdgePrefabList.RemoveAt(i);
#if UNITY_EDITOR
                    if (so != null) { UnityEditor.EditorUtility.SetDirty(so); }
#endif
                    continue;
                }


                //! 그리드 스냅 설정과 맞지 않다면 목록에서 제거
                if (!CheckValid_CheckGridSnapInternal(obj))
                {
                    Debug.LogError($"\"<b>{obj.name}</b>\" 오브젝트가 그리드 스냅 설정이 \"<b>{gridCompatibleSnapSetting.name}</b>\" 을 사용하지 않아, 목록에서 제거됨");
                    hallwayEdgePrefabList.RemoveAt(i);
#if UNITY_EDITOR
                    if (so != null) { UnityEditor.EditorUtility.SetDirty(so); }
#endif
                    continue;
                }


                //! 크기가 1x1이 아니라면 목록에서 제거
                if (obj.GridCompatible.ObjectSizeX_Width != 1 || obj.GridCompatible.ObjectSizeY_Height != 1)
                {
                    Debug.LogError($"\"<b>{obj.name}</b>\" 복도 벽 오브젝트에 1x1 크기가 아닌 오브젝트가 할당되어, 목록에서 제거됨");
                    hallwayEdgePrefabList.RemoveAt(i);
#if UNITY_EDITOR
                    if (so != null) { UnityEditor.EditorUtility.SetDirty(so); }
#endif
                    continue;
                }
            }
        }



        ///======================================================================================================================================================



        //? RoomPrefabList 관련 메서드



        /// <summary>
        /// (너비/높이) 기준으로 방 리스트의 <b>최소·최대 길이값</b>만 반환한다.
        /// </summary>
        /// <param name="boundary">최소값인지 최대값인지</param>
        /// <param name="dimension">Width → 너비, Height → 높이</param>
        /// <returns>조건에 맞는 길이값(방이 없으면 0)</returns>
        public int GetExtremeRoomGridSizeValue(EBoundary boundary, EDimension2D dimension)
        {
            //. 방이 없으면 0
            if (RoomPrefabList == null || RoomPrefabList.Count == 0) return 0;

            int extreme = boundary == EBoundary.Min ? int.MaxValue : int.MinValue;

            foreach (var room in RoomPrefabList)
            {
                if (room == null) { continue; }

                int len = dimension == EDimension2D.Width
                          ? room.GridCompatible.ObjectSizeX_Width   //? 너비
                          : room.GridCompatible.ObjectSizeY_Height; //? 높이

                if ((boundary == EBoundary.Min && len < extreme) ||
                    (boundary == EBoundary.Max && len > extreme))
                {
                    extreme = len;
                }
            }
            return extreme;
        }



        /// <summary>
        /// 방 리스트에서 (너비, 높이) 각각의 <b>최소·최대</b> 길이값을 한 번에 가져온다.
        /// </summary>
        /// <param name="boundary">최소값인지 최대값인지 지정</param>
        /// <returns>X = 극값 너비, Y = 극값 높이</returns>
        /// <remarks>
        ///     반환되는 너비와 높이는 <u>반드시 같은 방에서 나온 값이 아닐 수 있다</u>.  
        ///     “동일 방의 너비·높이 쌍”이 필요하면 TryGetRoomsByGridArea 등을 사용해 넓이를 기준으로 구하거나,  
        ///     너비·높이 모두 같은 방만 필터링하는 추가 로직을 작성해야 한다.
        /// </remarks>
        public Vector2Int GetExtremeRoomGridDimensions(EBoundary boundary)
        {
            //. 너비, 높이 각각의 극값을 개별 메서드로 취득
            int extremeWidth = GetExtremeRoomGridSizeValue(boundary, EDimension2D.Width);  //? 너비
            int extremeHeight = GetExtremeRoomGridSizeValue(boundary, EDimension2D.Height); //? 높이

            return new Vector2Int(extremeWidth, extremeHeight);
        }



        /// <summary>
        /// (넓이) 기준으로 방 리스트의 <b>최소·최대 넓이값</b>만 반환한다.
        /// </summary>
        /// <param name="boundary">최소값인지 최대값인지</param>
        /// <returns>조건에 맞는 넓이값(방이 없으면 0)</returns>
        public int GetExtremeRoomGridAreaValue(EBoundary boundary)
        {
            //. 방이 없으면 0
            if (RoomPrefabList == null || RoomPrefabList.Count == 0) return 0;

            int extreme = boundary == EBoundary.Min ? int.MaxValue : int.MinValue;

            foreach (var room in RoomPrefabList)
            {
                if (room == null) { continue; }

                int area = room.GridCompatible.ObjectSizeX_Width *
                           room.GridCompatible.ObjectSizeY_Height; //? 넓이

                if ((boundary == EBoundary.Min && area < extreme) ||
                    (boundary == EBoundary.Max && area > extreme))
                {
                    extreme = area;
                }
            }
            return extreme;
        }



        /// <summary>
        /// (너비/높이) 기준으로 <b>최소·최대 길이값</b>을 가지는 모든 방을 찾는다.
        /// </summary>
        /// <param name="boundary">최소값인지 최대값인지</param>
        /// <param name="dimension">Width → 너비, Height → 높이</param>
        /// <param name="resultRooms">조건을 만족하는 방 리스트</param>
        /// <param name="extremeValue">조건을 만족하는 길이값</param>
        /// <returns>찾은 방이 하나라도 있으면 true</returns>
        public bool TryGetRoomsByGridSize(EBoundary boundary, EDimension2D dimension, out List<RoomObject> resultRooms, out int extremeValue)
        {
            resultRooms = new List<RoomObject>();
            extremeValue = 0;

            //! 방이 없으면 false
            if (RoomPrefabList == null || RoomPrefabList.Count == 0) return false;

            //. 1차 패스: extremeValue 계산
            extremeValue = GetExtremeRoomGridSizeValue(boundary, dimension);

            //. 2차 패스: 값이 같은 방 수집
            foreach (var room in RoomPrefabList)
            {
                if (room == null) { continue; }

                int len = dimension == EDimension2D.Width
                          ? room.GridCompatible.ObjectSizeX_Width   //? 너비
                          : room.GridCompatible.ObjectSizeY_Height; //? 높이

                if (len == extremeValue) resultRooms.Add(room);
            }
            return resultRooms.Count > 0;
        }



        /// <summary>
        /// (넓이) 기준으로 <b>최소·최대 넓이값</b>을 가지는 모든 방을 찾는다.
        /// </summary>
        /// <param name="boundary">최소값인지 최대값인지</param>
        /// <param name="resultRooms">조건을 만족하는 방 리스트</param>
        /// <param name="extremeArea">조건을 만족하는 넓이값</param>
        /// <returns>찾은 방이 하나라도 있으면 true</returns>
        public bool TryGetRoomsByGridArea(EBoundary boundary, out List<RoomObject> resultRooms, out int extremeArea)
        {
            resultRooms = new List<RoomObject>();
            extremeArea = 0;

            //! 방이 없으면 false
            if (RoomPrefabList == null || RoomPrefabList.Count == 0) return false;

            //. 1차 패스: extremeArea 계산
            extremeArea = GetExtremeRoomGridAreaValue(boundary);

            //. 2차 패스: 값이 같은 방 수집
            foreach (var room in RoomPrefabList)
            {
                if (room == null) { continue; }

                int area = room.GridCompatible.ObjectSizeX_Width *
                           room.GridCompatible.ObjectSizeY_Height; //? 넓이

                if (area == extremeArea) resultRooms.Add(room);
            }
            return resultRooms.Count > 0;
        }



        #region Legacy



        //        //? 검사



        //        ///<summary>
        //        ///프리팹 유효성 검사<br/>
        //        ///현재이 설정이 유효한지 검사한다
        //        /// </summary>
        //        /// <param name="autoFix">true시, 자동으로 수정할수 있는 부분은 수정한다</param>
        //        /// <param name="objectNameForDebug">디버그메시지 사용시, 표시할 오브젝트 이름</param>
        //        /// <param name="debugMsg">디버그 메시지를 사용한다 (에디터에서만 작동)</param>
        //        /// <param name="debugTMI">디버그 메시지에 현재 설정의 추가 정보도 작성한다</param>
        //        public static bool CheckValied_Prefabs(IStageGeneratorSettingPrefab stageGenSetting, bool autoFix, bool debugMsg, bool debugTMI, string objectNameForDebug)
        //        {
        //            var setting = stageGenSetting.Setting;

        //            bool roomCheck = setting.Check_PrefabList(nameof(RoomPrefabList), setting.RoomPrefabList, autoFix, debugMsg);
        //            bool hallwayCheck = setting.Check_PrefabList(nameof(HallwayPrefabList), setting.HallwayPrefabList, autoFix, debugMsg, (size) =>
        //            {
        //                return (size.x == 1 && size.y == 1);
        //            });
        //            bool hallwayWallCheck = setting.Check_PrefabList(nameof(HallwayWallPrefabList), setting.HallwayWallPrefabList, autoFix, debugMsg, (size) =>
        //            {
        //                return (size.x == 1 && size.y == 1);
        //            });

        //#if UNITY_EDITOR
        //            if (debugTMI)
        //            {
        //                setting.GetRoomSize(EBoundary.Min, EDimension2D.Width, false, out var minWidthRoom, out var minWidth);
        //                setting.GetRoomSize(EBoundary.Min, EDimension2D.Height, false, out var minHeightRoom, out var minHeight);


        //                setting.GetRoomSize(EBoundary.Max, EDimension2D.Width, false, out var maxWidthRoom, out var maxWidth);
        //                setting.GetRoomSize(EBoundary.Max, EDimension2D.Height, false, out var maxHeightRoom, out var maxHeight);

        //                setting.GetRoomSize(EBoundary.Max, EDimension2D.Width, true, out var maxWidthRoomSafeArea, out var maxWidthSafeArea);
        //                setting.GetRoomSize(EBoundary.Max, EDimension2D.Height, true, out var maxHeightRoomSafeArea, out var maxHeightSafeArea);


        //                setting.GetRoomSizeVector(EBoundary.Min, false, out var minRoom, out var minRoomSize);
        //                setting.GetRoomSizeVector(EBoundary.Max, false, out var maxRoom, out var maxRoomSize);
        //                setting.GetRoomSizeVector(EBoundary.Max, true, out var maxRoomSafeArea, out var maxRoomSafeAreaSize);


        //                if (debugMsg)
        //                {
        //                    StringBuilder sb = new StringBuilder();


        //                    sb.AppendLine($"프리팹 설정 <b><color=#ac92ec>[{objectNameForDebug}]</color></b>의 <b>TMI</b>");
        //                    sb.AppendLine();
        //                    sb.AppendLine($"가장 작은 방의 <b>너비</b>: <b><color=#2ecc71>{minWidth}</color></b>");
        //                    sb.AppendLine($"가장 작은 방의 <b>높이</b>: <b><color=#2ecc71>{minHeight}</color></b>");
        //                    sb.AppendLine();
        //                    sb.AppendLine($"가장 큰 방의 <b>너비</b>: <b><color=#2ecc71>{maxWidth}</color></b>");
        //                    sb.AppendLine($"가장 큰 방의 <b>높이</b>: <b><color=#2ecc71>{maxHeight}</color></b>");
        //                    sb.AppendLine();
        //                    sb.AppendLine($"가장 큰 방의 <b>너비(안전구역)</b>: <b><color=#2ecc71>{maxWidthSafeArea}</color></b>");
        //                    sb.AppendLine($"가장 큰 방의 <b>높이(안전구역)</b>: <b><color=#2ecc71>{maxHeightSafeArea}</color></b>");
        //                    sb.AppendLine();
        //                    sb.AppendLine($"가장 작은 방:\t\t<b><color=#ac92ec>{minRoom.name}</color></b>\t<b><color=#2ecc71>{minRoomSize}</color></b>");
        //                    sb.AppendLine($"가장 큰 방:\t\t<b><color=#ac92ec>{maxRoom.name}</color></b>\t<b><color=#2ecc71>{maxRoomSize}</color></b>");
        //                    sb.AppendLine($"가장 큰 방(안전구역):\t<b><color=#ac92ec>{maxRoomSafeArea.name}</color></b>\t<b><color=#2ecc71>{maxRoomSafeAreaSize}</color></b>");


        //                    Debug.Log(sb.ToString(false));
        //                }
        //            }
        //#endif


        //            if (roomCheck && hallwayCheck && hallwayWallCheck)
        //            {
        //#if UNITY_EDITOR
        //                if (debugMsg)
        //                {
        //                    Debug.Log($"프리팹 설정 <b><color=#ac92ec>[{objectNameForDebug}]</color></b>: 검사 완료 | <b><color=#4fc1e9>이상 무</color></b>\n");
        //                }
        //#endif
        //                return true;
        //            }
        //            else
        //            {
        //#if UNITY_EDITOR
        //                if (debugMsg)
        //                {
        //                    Debug.LogWarning($"프리팹 설정 <b><color=#ac92ec>[{objectNameForDebug}]</color></b>: 검사 완료 | <b><color=#ed5565>이상 유</color></b>\n");
        //                }
        //#endif
        //                return false;
        //            }
        //        }



        //        ///<summary>
        //        ///프리팹 유효성 검사<br/>
        //        ///현재이 설정이 유효한지 검사한다
        //        /// </summary>
        //        /// <param name="autoFix">true시, 자동으로 수정할수 있는 부분은 수정한다</param>
        //        /// <param name="debugMsg">디버그 메시지를 사용한다 (에디터에서만 작동)</param>
        //        /// <param name="debugTMI">디버그 메시지에 현재 설정의 추가 정보도 작성한다</param>
        //        public static bool CheckValied_Prefabs(StageGeneratorSettingPrefabSbject stageGenSettingSbject, bool autoFix, bool debugMsg, bool debugTMI)
        //        {
        //            return CheckValied_Prefabs(stageGenSettingSbject, autoFix, debugMsg, debugTMI, stageGenSettingSbject.name);
        //        }



        //        private bool Check_PrefabList<T>(string listName, IList<T> prefabList, bool autoFix, bool debugMsg, Func<Vector3, bool> checkObjectSizeCondition = null) where T : GridCompatible2Object
        //        {
        //            bool result = true;



        //            foreach (var obj in prefabList)
        //            {
        //                //! #1 null 검사
        //                if (obj == null)
        //                {
        //#if UNITY_EDITOR
        //                    if (debugMsg)
        //                    {
        //                        Debug.LogError($"<color=#61bd6d>[{listName}]</color> 의 <b><color=#ed5565>[{obj.name}]</color></b> 가 null로 확인됨");
        //                    }
        //#endif
        //                    result = false;
        //                    continue;
        //                }


        //                var gridCompatible = obj.GridCompatible;
        //                //GridCompatible.IEdit gridCompatibleEdit = obj.GetGridCompatible();


        //                //! #2 GridUnit 검사
        //                if (gridCompatible.CurrentSnapSetting.GridUnitOriginalVector3 != GridUnit)
        //                {
        //                    if (!autoFix)
        //                    {
        //#if UNITY_EDITOR
        //                        if (debugMsg)
        //                        {
        //                            Debug.LogError($"<color=#61bd6d>[{listName}]</color> 의 <b><color=#ed5565>[{obj.name}]</color></b> 가 <b>GridUnit</b>이 다름\n<color=red>({gridCompatible.CurrentSnapSetting.GridUnitOriginalVector3} X → {GridUnit} O )</color>");
        //                        }
        //#endif
        //                        result = false;
        //                    }


        //                    else
        //                    {
        //                        if (obj.IsUseExternalSetting)
        //                        {
        //#if UNITY_EDITOR
        //                            if (debugMsg)
        //                            {
        //                                Debug.LogWarning($"<color=#61bd6d>[{listName}]</color> 의 <b><color=#ed5565>[{obj.name}]</color></b> 가 <b>GridUnit</b>를 이 설정값으로 변경해야 하나, 외장 설정을 사용하고있어 실패\n<color=red>({gridCompatible.CurrentSnapSetting.GridUnitOriginalVector3} → {GridUnit})</color>");
        //                            }
        //#endif
        //                            result = false;
        //                        }
        //                        else
        //                        {
        //#if UNITY_EDITOR
        //                            if (debugMsg)
        //                            {
        //                                Debug.Log($"<color=#61bd6d>[{listName}]</color> 의 <b><color=#ed5565>[{obj.name}]</color></b> 가 GridUnit를 이 설정값으로 변경됨\n({gridCompatible.CurrentSnapSetting.GridUnitOriginalVector3} → {GridUnit})");
        //                            }
        //#endif
        //                            //gridCompatibleEdit.GridUnit = GridUnit;
        //                            //! 250619 수정하는 코드로 바꾸기
        //                        }
        //                    }
        //                }


        //                //! #3 GridSwizzle 검사
        //                if (gridCompatible.CurrentSnapSetting.Swizzle != GridSwizzle)
        //                {
        //                    if (!autoFix)
        //                    {
        //#if UNITY_EDITOR
        //                        if (debugMsg)
        //                        {
        //                            Debug.LogError($"<color=#61bd6d>[{listName}]</color> 의 <b><color=#ed5565>[{obj.name}]</color></b> 가 GridSwizzle가 다름 ({gridCompatible.CurrentSnapSetting.Swizzle} X → {GridSwizzle} O )");
        //                        }
        //#endif
        //                        result = false;
        //                    }
        //                    else
        //                    {
        //                        if (obj.IsUseExternalSetting)
        //                        {
        //#if UNITY_EDITOR
        //                            if (debugMsg)
        //                            {
        //                                Debug.LogWarning($"<color=#61bd6d>[{listName}]</color> 의 <b><color=#ed5565>[{obj.name}]</color></b> 가 GridSwizzle를 이 설정값으로 변경해야 하나, 외장 설정을 사용하고있어 실패\n<color=red>({gridCompatible.CurrentSnapSetting.Swizzle} → {GridSwizzle})</color>");
        //                            }
        //#endif
        //                        }
        //                        else
        //                        {
        //#if UNITY_EDITOR
        //                            if (debugMsg)
        //                            {
        //                                Debug.Log($"<color=#61bd6d>[{listName}]</color> 의 <b><color=#ed5565>[{obj.name}]</color></b> 가 GridSwizzle를 이 설정값으로 변경됨\n({gridCompatible.CurrentSnapSetting.Swizzle} → {GridSwizzle})");
        //                            }
        //#endif
        //                            //gridCompatible.GetGridSwizzles(EOverrideFieldType.Original).GridSwizzle = GridSwizzle;
        //                            //! 250619 수정하는 코드로 바꾸기
        //                        }
        //                    }
        //                }


        //                //! #4 받아온 checkObjectSizeCondition 검사 (사이즈 검사)
        //                var size = gridCompatible.ObjectSizeTransformOriginalCurrentVector3;
        //                if (checkObjectSizeCondition != null && checkObjectSizeCondition.Invoke(size) == false)
        //                {
        //#if UNITY_EDITOR
        //                    if (debugMsg)
        //                    {
        //                        Debug.LogError($"<color=#61bd6d>[{listName}]</color> 의 <b><color=#ed5565>[{obj.name}]</color></b> 의 사이즈가 조건에 맞지않음 (<b><color=#ed5565>{size}</color></b>)");
        //                    }
        //#endif
        //                }
        //            }



        //            return result;
        //        }



        //        ///======================================================================================================================================================



        //        public void GetRoomSize(EBoundary boundary, EDimension2D dimension, bool useRoomSafeAreaSize, out RoomObject resultRoom, out float result)
        //        {
        //            resultRoom = null;


        //            switch (boundary)
        //            {
        //                case EBoundary.Min: result = Mathf.Infinity; break;
        //                case EBoundary.Max: result = Mathf.NegativeInfinity; break;
        //                default: result = 0; break;
        //            }


        //            foreach (var room in RoomPrefabList)
        //            {
        //                float currentWidth;


        //                switch (dimension)
        //                {
        //                    case EDimension2D.Width:

        //                    if (boundary == EBoundary.Max && useRoomSafeAreaSize) { currentWidth = room.TotalRoomRectMax_Maximum.size.x; }
        //                    else { currentWidth = room.GridCompatible.ObjectSizeX_Width; }

        //                    break;

        //                    case EDimension2D.Height:

        //                    if (boundary == EBoundary.Max && useRoomSafeAreaSize) { currentWidth = room.TotalRoomRectMax_Maximum.size.y; }
        //                    else { currentWidth = room.GridCompatible.ObjectSizeY_Height; }

        //                    break;

        //                    default: currentWidth = 0; break;
        //                }


        //                if ((boundary == EBoundary.Min && result > currentWidth) ||
        //                    (boundary == EBoundary.Max && result < currentWidth))
        //                {
        //                    result = currentWidth;
        //                    resultRoom = room;
        //                }
        //            }
        //        }



        //        public float GetRoomSize(EBoundary boundary, EDimension2D dimension, bool useRoomSafeAreaSize)
        //        {
        //            GetRoomSize(boundary, dimension, useRoomSafeAreaSize, out var room, out var result);
        //            return result;
        //        }



        //        public Vector2 GetRoomSize(EBoundary boundary, bool useRoomSafeAreaSize)
        //        {
        //            return new Vector2(GetRoomSize(boundary, EDimension2D.Width, useRoomSafeAreaSize), GetRoomSize(boundary, EDimension2D.Height, useRoomSafeAreaSize));
        //        }



        //        public void GetRoomSizeVector(EBoundary boundary, bool useRoomSafeAreaSize, out RoomObject resultRoom, out Vector2 result)
        //        {
        //            resultRoom = null;


        //            switch (boundary)
        //            {
        //                case EBoundary.Min: result = new Vector2(Mathf.Infinity, Mathf.Infinity); break;
        //                case EBoundary.Max: result = new Vector2(Mathf.NegativeInfinity, Mathf.NegativeInfinity); break;
        //                default: result = Vector2.zero; break;
        //            }


        //            foreach (var room in RoomPrefabList)
        //            {
        //                Vector2 currentSize;

        //                if (boundary == EBoundary.Max && useRoomSafeAreaSize) { currentSize = room.TotalRoomRectMax_Maximum.size; }
        //                else { currentSize = room.GridCompatible.ObjectSizeOriginalVector2; }


        //                if ((boundary == EBoundary.Min && result.x > currentSize.x && result.y > currentSize.y) ||
        //                    (boundary == EBoundary.Max && result.x < currentSize.x && result.y < currentSize.y))
        //                {
        //                    result = currentSize;
        //                    resultRoom = room;
        //                }
        //            }
        //        }



        //        public Vector2 GetRoomSizeVector(EBoundary boundary, bool useRoomSafeAreaSize)
        //        {
        //            GetRoomSizeVector(boundary, useRoomSafeAreaSize, out var room, out var result);
        //            return result;
        //        }



        //        ///======================================================================================================================================================



        //        public void GetMinDoorCountRoom(EDirection4 doorDirection, out RoomObject minDoorCountRoom, out int minDoorCount)
        //        {
        //            minDoorCountRoom = null;
        //            minDoorCount = int.MaxValue;

        //            foreach (var room in RoomPrefabList)
        //            {
        //                var currentDoorCount = room.RoomDoorM.GetDoorList(doorDirection).Count;
        //                if (minDoorCount > currentDoorCount)
        //                {
        //                    minDoorCount = currentDoorCount;
        //                    minDoorCountRoom = room;
        //                }
        //            }
        //        }



        //        public void GetMaxDoorCountRoom(EDirection4 doorDirection, out RoomObject maxDoorCountRoom, out int maxDoorCount)
        //        {
        //            maxDoorCountRoom = null;
        //            maxDoorCount = int.MinValue;

        //            foreach (var room in RoomPrefabList)
        //            {
        //                var currentDoorCount = room.RoomDoorM.GetDoorList(doorDirection).Count;
        //                if (maxDoorCount < currentDoorCount)
        //                {
        //                    maxDoorCount = currentDoorCount;
        //                    maxDoorCountRoom = room;
        //                }
        //            }
        //        }


        #endregion



        #region Legacy 2

        ///// <summary>
        ///// (너비/높이) 기준으로 최소·최대 길이를 가진 방을 찾는다.
        ///// </summary>
        ///// <param name="boundary">최소값을 찾을지 최대값을 찾을지 지정</param>
        ///// <param name="dimension">Width → 너비, Height → 높이</param>
        ///// <param name="resultRoom">조건을 만족하는 방</param>
        ///// <param name="resultLength">조건을 만족하는 길이값</param>
        ///// <returns>조건에 맞는 방을 찾았으면 true</returns>
        //public bool TryGetRoomGridSizeCondition(EBoundary boundary, EDimension2D dimension, out RoomObject resultRoom, out int resultLength)
        //{
        //    //! 방 목록이 비어있으면 false 반환
        //    if (RoomPrefabList == null || RoomPrefabList.Count == 0)
        //    {
        //        resultRoom = null;
        //        resultLength = 0;
        //        return false;
        //    }

        //    //. 비교 대상 초기값
        //    resultRoom = null;
        //    resultLength = boundary == EBoundary.Min ? int.MaxValue : int.MinValue;

        //    //. 길이 계산
        //    int GetLength(RoomObject r) => dimension == EDimension2D.Width
        //                                   ? r.GridCompatible.ObjectSizeX_Width   //? 가로
        //                                   : r.GridCompatible.ObjectSizeY_Height; //? 세로

        //    //. 단일 패스로 탐색
        //    foreach (var room in RoomPrefabList)
        //    {
        //        var length = GetLength(room);

        //        if ((boundary == EBoundary.Min && length < resultLength) ||
        //            (boundary == EBoundary.Max && length > resultLength))
        //        {
        //            resultRoom = room;
        //            resultLength = length;
        //        }
        //    }

        //    return resultRoom != null; //! 찾은 결과가 있으면 true
        //}



        ///// <summary>
        ///// (너비/높이) 기준으로 최소·최대 길이를 가진 방의 (너비/높이)를 구한다.
        ///// </summary>
        ///// <param name="boundary">최소값을 찾을지 최대값을 찾을지 지정</param>
        ///// <param name="dimension">Width → 너비, Height → 높이</param>
        //public int TryGetRoomGridSizeCondition(EBoundary boundary, EDimension2D dimension)
        //{
        //    TryGetRoomGridSizeCondition(boundary, dimension, out _, out var resultLength);
        //    return resultLength;
        //}



        ///// <summary>
        ///// (넓이) 기준으로 최소·최대 크기를 가진 방을 찾아 가로·세로 값을 반환한다.
        ///// </summary>
        ///// <param name="boundary">최소값을 찾을지 최대값을 찾을지 지정</param>
        ///// <param name="resultRoom">조건을 만족하는 방</param>
        ///// <param name="resultSize">조건을 만족하는 크기(가로·세로)</param>
        ///// <returns>조건에 맞는 방을 찾았으면 true</returns>
        //public bool TryGetRoomGridAreaCondition(EBoundary boundary, out RoomObject resultRoom, out Vector2Int resultSize)
        //{
        //    //! 방 목록이 비어있으면 false 반환
        //    if (RoomPrefabList == null || RoomPrefabList.Count == 0)
        //    {
        //        resultRoom = null;
        //        resultSize = Vector2Int.zero;
        //        return false;
        //    }

        //    //. 비교 대상 초기값
        //    resultRoom = null;
        //    int bestArea = boundary == EBoundary.Min ? int.MaxValue : int.MinValue;
        //    resultSize = Vector2Int.zero;

        //    //. 단일 패스로 탐색
        //    foreach (var room in RoomPrefabList)
        //    {
        //        int w = room.GridCompatible.ObjectSizeX_Width;
        //        int h = room.GridCompatible.ObjectSizeY_Height;
        //        int area = w * h;

        //        if ((boundary == EBoundary.Min && area < bestArea) ||
        //            (boundary == EBoundary.Max && area > bestArea))
        //        {
        //            resultRoom = room;
        //            bestArea = area;
        //            resultSize = new Vector2Int(w, h);
        //        }
        //    }

        //    return resultRoom != null; //! 찾은 결과가 있으면 true
        //}



        ///// <summary>
        ///// (넓이) 기준으로 최소·최대 크기를 가진 방을 찾아 가로·세로 값의 (넓이)를 구한다.
        ///// </summary>
        ///// <param name="boundary">최소값을 찾을지 최대값을 찾을지 지정</param>
        //public Vector2Int TryGetRoomGridAreaCondition(EBoundary boundary)
        //{
        //    TryGetRoomGridAreaCondition(boundary, out _, out var resultLength);
        //    return resultLength;
        //}



        #endregion


        ///======================================================================================================================================================
    }



    [CreateAssetMenu(menuName = CreateAssetMenuInfo.STAGEGEN_PREFAB_SETTING)]
    public class StageGeneratorSettingPrefabSbject : ScriptableObject, IStageGeneratorSettingPrefab
    {
#if UNITY_EDITOR

        [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Center, EnableRichText = true, Overflow = false), EnableGUI]
        [PropertyOrder(-100)]
        [PropertySpace(8, 8)]
        private string dummy_Title
        {
            get
            {
                return $"<b><size=15>스테이지 생성기 프리팹 설정 SO</size></b>\n" +
                    $"<size=11><i>(유효한 그리드 스냅 설정과, 1개이상의 방 오브젝트가 할당 되어 있어야 함)</i></size>\n" +
                    $"유효성: " + (setting.IsValid ? $"✔️ <color=#2ecc71>Valid\n</color>" : $" ❌ <color=#ed5565>Invalid\n</color>") +
                    $"가용 유효성: " + (setting.IsValidToUse ? $"✔️ <color=#2ecc71>Valid</color>" : $" ❌ <color=#ed5565>Invalid</color>");
            }
        }

#endif



        [SerializeField]
        [InlineProperty, HideLabel]
        private StageGeneratorSettingPrefab setting;

        public StageGeneratorSettingPrefab Setting => setting;

#if UNITY_EDITOR

        private void OnValidate()
        {
            //. 에디터에서는 항상 유효성 검사
            setting.RefreshValid(this);
        }

#endif

    }
}