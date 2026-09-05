using System.Collections.Generic;
using UnityEngine;
using Pan.Util;
using System;
using Pan.GridCompatibles2;
using Pan.StageGenerators;
using System.Text;
using Sirenix.OdinInspector;
using System.IO;
using System.Reflection;
using System.Collections;



namespace Pan.StageGenerators
{
    public partial class StageGenerator
    {
        [Serializable]
        public class DebugLogger : BaseManager
        {
            ///======================================================================================================================================================





            [TitleGroup("디버그 로거"), BoxGroup("디버그 로거/박스", false)]
            [LabelText("생성 시간 기록하기")]
            [SerializeField]
            public bool UseRecordTime;

            [TitleGroup("디버그 로거"), BoxGroup("디버그 로거/박스", false)]
            [LabelText("설정 정보 기록하기")]
            [SerializeField]
            public bool UseLog_SettingInfo;

            [TitleGroup("디버그 로거"), BoxGroup("디버그 로거/박스", false)]
            [LabelText("Debug.Log(...) 출력 배제하기")]
            [SerializeField]
            public bool NotUseUnityDebugLogs;



            public CustomDebugLogger Logger => logger;
            [TitleGroup("디버그 로거"), BoxGroup("디버그 로거/박스", false)]
            [LabelText("스테이지 생성기 디버그 로거")]
            [SerializeField]
            private CustomDebugLogger logger = new CustomDebugLogger();




            private readonly StringBuilder stringBuilder = new StringBuilder();
            private static int sBuilderFixedWidth_15 = 15;
            private static int sBuilderFixedWidth_20 = 20;
            private static int sBuilderFixedWidth_25 = 25;
            private static int sBuilderFixedWidth_30 = 30;
            private static int sBuilderFixedWidth_40 = 40;



            private readonly System.Diagnostics.Stopwatch StopWatch = new System.Diagnostics.Stopwatch();



            ///======================================================================================================================================================



            private void AddLine_A()
            {
                stringBuilder.AppendLine("======================================================");
            }



            private void AddLine_B()
            {
                stringBuilder.AppendLine("------------------------------------------------------");
            }



            ///======================================================================================================================================================



            public void StartLog(int? isRegenerateCount = null)
            {
                if (!logger.CurrentDebugMode) return;

                if (UseRecordTime)
                {
                    StopWatch.Reset();
                    StopWatch.Start();
                }

                logger.Initialize(); //. 디버그로거 초기화하고 시작

                stringBuilder.Clear();

                stringBuilder.AppendLine();
                AddLine_A();
                if (!isRegenerateCount.HasValue)
                {
                    stringBuilder.AppendLine($"{"START LOGGING".PadRight(sBuilderFixedWidth_15)}[{DateTime.Now:yyyy-MM-dd.HH:mm:ss}]");
                }
                else
                {
                    stringBuilder.AppendLine($"{$"START LOGGING (ReGenerate:{isRegenerateCount})".PadRight(sBuilderFixedWidth_15)} [{DateTime.Now:yyyy-MM-dd.HH:mm:ss}]");
                }
                AddLine_A();
                stringBuilder.AppendLine();

                stringBuilder.AppendLine($"{"SETTING:".PadRight(sBuilderFixedWidth_15)}[{Main.SettingSbject.name}]");

                if (UseLog_SettingInfo)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine($"SETTING INFO:");
                    SU_String.AppendFieldsAsString(Main.Setting, stringBuilder, ",");
                }

                stringBuilder.AppendLine();
            }



            public void ErrorLog(Exception ex)
            {
                if (!logger.CurrentDebugMode) return;

                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"Error Detected!");
                sb.AppendLine($"\t{ex}");

                stringBuilder.AppendLine(sb.RemoveHtmlTags().ToString(true));
            }



            ///======================================================================================================================================================



            //? 통합 에러 메시지



            private void LogErrorCombine(Action<StringBuilder> action, bool printOnlyDebugMessage)
            {
#if !UNITY_EDITOR
                if (!logger.CurrentDebugMode) return;
#endif
                var sb = new StringBuilder();

                action?.Invoke(sb);

#if UNITY_EDITOR
                if (!NotUseUnityDebugLogs)
                {
                    Debug.LogError(sb.ToString(false).Replace("\t", ""));
                }
#endif
                if (logger.CurrentDebugMode && !printOnlyDebugMessage)
                {
                    stringBuilder.AppendLine(sb.RemoveHtmlTags().ToString(false));
                }
            }



            private void LogErrorCombine_FailedPathFindingOrHallway(StageGenerator main, Action<StringBuilder> action, bool onlyDebugMessage)
            {
                if (main.placeM.EnableDebugLog_FailedPathFindingOrHallway)
                {
                    if (!logger.CurrentDebugMode) return;

                    StringBuilder sb = new StringBuilder();
                    action.Invoke(sb);
                    stringBuilder.AppendLine(sb.RemoveHtmlTags().ToString(false));
                }
                else
                {
                    LogErrorCombine(action, onlyDebugMessage);
                }
            }



            ///======================================================================================================================================================



            //? 통합 Head1 메서드 실행의 준비, 성공, 실패



            ///<summary>
            ///Head1 메서드 준비
            /// </summary>
            public void Log_Head1_Ready(string title)
            {
                if (!logger.CurrentDebugMode) return;

                AddLine_B();
                stringBuilder.AppendLine($"■ Ready {title}");
                //AddLine_B();
                stringBuilder.AppendLine();
            }



            ///<summary>
            ///Head1 메서드 성공
            /// </summary>
            public void Log_Head1_Success(string title)
            {
                if (!logger.CurrentDebugMode) return;

                //AddLine_B();
                stringBuilder.AppendLine($"▶ Success {title}!");
                AddLine_B();
                stringBuilder.AppendLine();
            }



            ///<summary>
            ///Head1 메서드 실패
            /// </summary>
            public void Log_Head1_Failure(string title)
            {
                if (!logger.CurrentDebugMode) return;

                //AddLine_B();
                stringBuilder.AppendLine($"▷ Failure {title}");
                AddLine_B();
                stringBuilder.AppendLine();
                stringBuilder.AppendLine();
            }



            ///======================================================================================================================================================



            public void Log_RandomSeedInfo()
            {
                if (!logger.CurrentDebugMode) return;
                stringBuilder.AppendLine($"{"\tSeed:".PadRight(sBuilderFixedWidth_15)}{Main.GenerateInfoM.Seed_LastApplied}");
                stringBuilder.AppendLine();
            }



            public void Log_SpaceInfo()
            {
                if (!logger.CurrentDebugMode) return;
                stringBuilder.AppendLine($"{"\tSpace Count:".PadRight(sBuilderFixedWidth_30)}{Main.spaceM.SpacesBinaryTree.Count}");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine($"{"\tMin Space Node Count:".PadRight(sBuilderFixedWidth_30)}{Main.spaceM.Generation_MinNodeCount}");
                stringBuilder.AppendLine($"{"\tMax Space Node Count:".PadRight(sBuilderFixedWidth_30)}{Main.spaceM.Generation_MaxNodeCount}");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine($"{"\tMin Space Node Down Count:".PadRight(sBuilderFixedWidth_30)}{Main.spaceM.Generation_MinNodeCount_Down}");
                stringBuilder.AppendLine($"{"\tMin Space Node Up Count:".PadRight(sBuilderFixedWidth_30)}{Main.spaceM.Generation_MinNodeCount_Up}");
                stringBuilder.AppendLine($"{"\tMin Space Node Left Count:".PadRight(sBuilderFixedWidth_30)}{Main.spaceM.Generation_MinNodeCount_Left}");
                stringBuilder.AppendLine($"{"\tMin Space Node Right Count:".PadRight(sBuilderFixedWidth_30)}{Main.spaceM.Generation_MinNodeCount_Right}");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine($"{"\tMax Space Node Down Count:".PadRight(sBuilderFixedWidth_30)}{Main.spaceM.Generation_MaxNodeCount_Down}");
                stringBuilder.AppendLine($"{"\tMax Space Node Up Count:".PadRight(sBuilderFixedWidth_30)}{Main.spaceM.Generation_MaxNodeCount_Up}");
                stringBuilder.AppendLine($"{"\tMax Space Node Left Count:".PadRight(sBuilderFixedWidth_30)}{Main.spaceM.Generation_MaxNodeCount_Left}");
                stringBuilder.AppendLine($"{"\tMax Space Node Right Count:".PadRight(sBuilderFixedWidth_30)}{Main.spaceM.Generation_MaxNodeCount_Right}");
                stringBuilder.AppendLine();
            }



            ///======================================================================================================================================================



            //? Place_Gen1 (공간 안에 방들을 소환)



            /// <summary>
            ///  Place Gen1 준비
            /// </summary>
            public void Log_Place_Gen1_Ready()
            {
                if (!logger.CurrentDebugMode) return;


                stringBuilder.AppendLine($"# Ready Gen1_RoomCreater");
                stringBuilder.AppendLine();
            }



            /// <summary>
            ///  Place Gen1 성공
            /// </summary>
            /// <param name="isAfter"></param>
            public void Log_Place_Gen1_Success()
            {
                if (!logger.CurrentDebugMode) return;

                stringBuilder.AppendLine($"\t# Success Gen1_RoomCreater!");
                stringBuilder.AppendLine();
            }



            /// <summary>
            ///  Place Gen1 실패
            /// </summary>
            /// <param name="isAfter"></param>
            public void Log_Place_Gen1_Failure()
            {
                if (!logger.CurrentDebugMode) return;

                stringBuilder.AppendLine($"\t# Failure Gen1_RoomCreater");
                stringBuilder.AppendLine();
            }



            /// <summary>
            /// Place Gen1 적용할 최대 방 인접 반복 연산 횟수 출력
            /// </summary>
            /// <param name="nearLoopCount"></param>
            public void Log_Place_Gen1_PrintRoomNearCalculateCount(int nearLoopCount)
            {
                if (!logger.CurrentDebugMode) return;

                stringBuilder.AppendLine($"\tSetting's Room Near Loop Calculate Count: {nearLoopCount}");
                stringBuilder.AppendLine();
            }



            /// <summary>
            /// Place Gen1 방 인접 반복 연산 루프가 시행된 횟수 출력
            /// </summary>
            /// <param name="nearLoopCount"></param>
            public void Log_Place_Gen1_PrintApplyedRoomNearCalculateCount(int nearLoopCount)
            {
                if (!logger.CurrentDebugMode) return;
                stringBuilder.AppendLine($"\tApplyed Room Near Loop Calculate Count: {nearLoopCount}");
                stringBuilder.AppendLine();
            }



            /// <summary>
            /// Place Gen1 인접 크로노 브레이크 실행 정보 출력
            /// </summary>
            public void Log_Place_Gen1_PrintExecutedChronoBreak_AdjacentNearly(StageGenerator main, int loopNumber, Space currentSpace, in RectWithOffset currentRoomRect, in RectWithOffset targetRoomRect, float hiatus_1, float hiatus_2, float hiatusMain, float rectDistance, in IEnumerable<Rect> allRoomRects, in Rect boundingRect)
            {
                if (!logger.CurrentDebugMode) return;


                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"\tChronoBreak_AdjacentNearly is Executed!");
                sb.AppendLine();
                sb.AppendLine($"\t\t{"Calculated Count:".PadRight(sBuilderFixedWidth_25)} <color=#2ecc71><b>{loopNumber + 1}</b></color>");
                sb.AppendLine($"\t\t{"Executed Space Number:".PadRight(sBuilderFixedWidth_25)} <color=#2ecc71><b>{currentSpace.OrderIndex_RoomNearest}</b></color>");
                sb.AppendLine($"\t\t{"ComparedRects:".PadRight(sBuilderFixedWidth_25)} <color=#2ecc71><b>{currentRoomRect.Rect}</b></color>, <color=#2ecc71><b>{targetRoomRect.Rect}</b></color>");
                sb.AppendLine($"\t\t{"Hiatus_1:".PadRight(sBuilderFixedWidth_25)} <color=#2ecc71><b>{hiatus_1}</b></color>");
                sb.AppendLine($"\t\t{"Hiatus_2:".PadRight(sBuilderFixedWidth_25)} <color=#2ecc71><b>{hiatus_2}</b></color>");
                sb.AppendLine($"\t\t{"Main Hiatus:".PadRight(sBuilderFixedWidth_25)} <color=#2ecc71><b>{hiatusMain}</b></color>");
                sb.AppendLine($"\t\t{"Rect Distance:".PadRight(sBuilderFixedWidth_25)} <color=#2ecc71><b>{rectDistance}</b></color>");

                //#if UNITY_EDITOR
                //                Debug.LogWarning(sb.ToStringCustom(false));
                //#endif

                stringBuilder.AppendLine(sb.RemoveHtmlTags().ToString(false));
            }



            /// <summary>
            ///  Place Gen1 실패, 공간 위에 방 배치 실패
            /// </summary>
            /// <param name="main"></param>
            /// <param name="space"></param>
            public void LogError_Place_Gen1_NotFoundRoomFromSpace(StageGenerator main, Space space)
            {
                LogErrorCombine((sb) =>
                {
                    sb.AppendLine("<b><color=red>Place_Gen1_RoomCreate Failed!</color></b>");
                    sb.AppendLine("<b><color=red>공간 위에 방 배치 실패, 맞는 방을 찾을수 없음</color></b>");
                    sb.AppendLine();


                    Vector2Int maxSizeRoom = main.PrefabSetting.GetExtremeRoomGridDimensions(EBoundary.Max);
                    Vector2Int roomSizeCorrection = main.Setting.Room.RoomPlacementSizeCorrection;
                    Vector2 maxSizeRoom_Correction = maxSizeRoom + roomSizeCorrection;


                    sb.AppendLine($"<b>해당 공간의 크기</b> : <b><i><color=#ed5565>{space.SpaceRect.size}</color></i></b>");
                    sb.AppendLine($"<b>하단 노드</b>: <b><i><color=#ed5565>{space.CurrentNodeCount_Down}</color></i></b>\t<b>상단 노드</b>: <b><i><color=#ed5565>{space.CurrentNodeCount_Up}</color></i></b>");
                    sb.AppendLine($"<b>좌측 노드</b>: <b><i><color=#ed5565>{space.CurrentNodeCount_Left}</color></i></b>\t<b>우측 노드</b>: <b><i><color=#ed5565>{space.CurrentNodeCount_Right}</color></i></b>");
                    sb.AppendLine();

                    sb.AppendLine($"<b>목록의 방 중 최대 크기</b> : <b><i><color=#ed5565>{maxSizeRoom}</color></i></b>");
                    sb.AppendLine($"<b>목록의 방 중 최대 크기(보정)</b>: <b><i><color=#ed5565>{maxSizeRoom_Correction}</color></i></b>");
                    sb.AppendLine();


                    if (space.SpaceRect.x < maxSizeRoom_Correction.x || space.SpaceRect.y < maxSizeRoom_Correction.y)
                    {
                        if (maxSizeRoom_Correction == Vector2Int.zero)
                        {
                            sb.AppendLine($"원인: <b>최대 방 영역보다 공간이 작음!</b>");
                        }
                        else
                        {
                            sb.AppendLine($"원인: <b>최대 방 영역보다 공간이 작음!</b>\t <i>(추가로 보정된 방 크기: <b><color=#61bd6d>{roomSizeCorrection.x}</color></b> x <b><color=#61bd6d>{roomSizeCorrection.y}</color></b>)</i>");
                        }

                        if (space.SpaceRect.size.x < maxSizeRoom_Correction.x)
                        {
                            sb.AppendLine($"\t현재 설정에서 최소 공간 너비X를 <b><color=#61bd6d>{maxSizeRoom_Correction.x - space.SpaceRect.size.x}</color></b> 만큼 늘려야함");
                            sb.AppendLine($"\t아니면 방의 추가 보정 너비를 조정해야함");
                            sb.AppendLine($"\t최소 공간 너비X의 최소값을 <b><color=#ed5565>{maxSizeRoom_Correction.x}</color></b> 로 해야함");
                            sb.AppendLine();
                        }
                        if (space.SpaceRect.size.y < maxSizeRoom_Correction.y)
                        {
                            sb.AppendLine($"\t현재 설정에서 최소 공간 높이Y를 <b><color=#61bd6d>{maxSizeRoom_Correction.y - space.SpaceRect.size.y}</color></b> 만큼 늘려야함");
                            sb.AppendLine($"\t아니면 방의 추가 보정 높이를 조정해야함");
                            sb.AppendLine($"\t최소 공간 높이Y의 최소값을 <b><color=#ed5565>{maxSizeRoom_Correction.y}</color></b> 로 해야함");
                            sb.AppendLine();
                        }
                    }
                    sb.AppendLine();
                }, true);

                if (logger.CurrentDebugMode)
                {
                    stringBuilder.AppendLine($"\t!Error! Cannot find a room that can be created in space\tsize: ({space.SpaceRect.size.x}, {space.SpaceRect.size.y})");
                    stringBuilder.AppendLine();
                }
            }



            ///======================================================================================================================================================



            //? Place_Gen3 (복도 생성)



            /// <summary>
            ///  Place Gen3 시작
            /// </summary>
            public void Log_Place_Gen3_Ready()
            {
                if (!logger.CurrentDebugMode) return;

                stringBuilder.AppendLine($"# Ready Gen3_ConnectDoorsHallways");
                stringBuilder.AppendLine();
            }



            /// <summary>
            ///  Place Gen3 성공
            /// </summary>
            public void Log_Place_Gen3_Success()
            {
                if (!logger.CurrentDebugMode) return;

                stringBuilder.AppendLine($"\t# Success Gen3_ConnectDoorsHallways!");
                stringBuilder.AppendLine();
            }



            /// <summary>
            ///  Place Gen3 실패
            /// </summary>
            public void Log_Place_Gen3_Failure()
            {
                if (!logger.CurrentDebugMode) return;

                stringBuilder.AppendLine($"\t# Failure Gen3_ConnectDoorsHallways");
                stringBuilder.AppendLine();
            }



            /// <summary>
            /// Place Gen3 실패, 도어Start~도어End 사이 복도 생성 실패
            /// </summary>
            /// <param name="door"></param>
            public void LogError_Place_Gen3_DoorStartToDoorEnd(StageGenerator main, RoomObject.Door door)
            {
                LogErrorCombine_FailedPathFindingOrHallway(main, sb =>
                {
                    sb.AppendLine();
                    sb.AppendLine("!! <b><color=red>Place_Gen3_DoorStartToEnd Failed!</color></b>");
                    sb.AppendLine($"<b>Door</b>: {door.DoorDirection} Door {((door.ParentRoomObject != null) ? $"Parent: {door.ParentRoomObject.name}" : "")}");
                    sb.AppendLine($"<b>Door Position</b>:<color=#2ecc71><b>{door.DoorStartTransformPositionCurrent}</b></color>");
                    if (door.ParentRoomObject != null)
                    {
                        door.CenterGridPosition_Start(out var isDoubleCenter, out var center1, out var center2);
                        sb.AppendLine($"<b>Door Start Grid Position</b>: <color=#2ecc71><b>{center1}</b></color>, <color=#2ecc71><b>{center1}</b></color>");
                    }

                }, false);
            }



            /// <summary>
            /// Place Gen3 실패, 도어End~연결도어End 사이 복도 생성 실패
            /// </summary>
            /// <param name="doorA"></param>
            /// <param name="doorB"></param>
            public void LogError_Place_Gen3_DoorEndToDoorEnd(StageGenerator main, RoomObject.Door doorA, RoomObject.Door doorB)
            {
                LogErrorCombine_FailedPathFindingOrHallway(main, sb =>
                {
                    sb.AppendLine();
                    sb.AppendLine("!! <b><color=red>Place_Gen3_DoorEndToEnd Failed!</color></b>");
                    sb.AppendLine($"<b>DoorA</b>: {doorA.DoorDirection} Door {((doorA.ParentRoomObject != null) ? $"Parent: {doorA.ParentRoomObject.name}" : "")}");
                    sb.AppendLine($"<b>DoorA Position</b>: <color=#2ecc71><b>{doorA.DoorStartTransformPositionCurrent}</b></color>");
                    if (doorA.ParentRoomObject != null)
                    {
                        doorA.CenterGridPosition_End(out var isDoubleCenter, out var center1, out var center2);
                        sb.AppendLine($"<b>DoorA End Grid Position</b>: <color=#2ecc71><b>{center1}</b></color>, <color=#2ecc71><b>{center1}</b></color>");
                    }

                    sb.AppendLine($"<b>DoorB</b>: {doorB.DoorDirection} Door {((doorB.ParentRoomObject != null) ? $"Parent: {doorB.ParentRoomObject.name}" : "")}");
                    sb.AppendLine($"<b>DoorB Position</b>: <color=#2ecc71><b>{doorB.DoorStartTransformPositionCurrent}</b></color>");
                    if (doorB.ParentRoomObject != null)
                    {
                        doorB.CenterGridPosition_End(out var isDoubleCenter, out var center1, out var center2);
                        sb.AppendLine($"<b>DoorB End Grid Position</b>: <color=#2ecc71><b>{center1}</b></color>, <color=#2ecc71><b>{center1}</b></color>");
                    }

                }, false);
            }



            //? Place_Gen3 서브: 복도 재구성



            /// <summary>
            ///  복도 재구성 준비
            /// </summary>
            /// <param name="doorA"></param>
            /// <param name="doorB"></param>
            public void Log_Place_Gen3_HallwayReconstruction_Ready(RoomObject.Door doorA, RoomObject.Door doorB, int? reconstructionCount)
            {
                if (!logger.CurrentDebugMode) return;

                stringBuilder.Append($"\t# Ready Hallway Reconstrction");
                if (reconstructionCount.HasValue)
                {
                    stringBuilder.AppendLine($", ReconstructionCount: {reconstructionCount}");
                }
                else
                {
                    stringBuilder.AppendLine();
                }

                stringBuilder.AppendLine($"\t\t{"DoorA:".PadRight(sBuilderFixedWidth_30)} {doorA.DoorDirection} Door {((doorA.ParentRoomObject != null) ? $"\tParent: {doorA.ParentRoomObject.name}" : "")}");
                stringBuilder.AppendLine($"\t\t{"DoorA Position:".PadRight(sBuilderFixedWidth_30)}{doorA.DoorStartTransformPositionCurrent}");
                if (doorA.ParentRoomObject != null)
                {
                    doorA.CenterGridPosition_End(out var isDoubleCenter, out var center1, out var center2);
                    stringBuilder.AppendLine($"\t\t{"DoorA End Grid Position:".PadRight(sBuilderFixedWidth_30)}{center1}, {center1}");
                }

                stringBuilder.AppendLine($"\t\t{"DoorB:".PadRight(sBuilderFixedWidth_30)} {doorB.DoorDirection} Door {((doorB.ParentRoomObject != null) ? $"\tParent: {doorB.ParentRoomObject.name}" : "")}");
                stringBuilder.AppendLine($"\t\t{"DoorB Position:".PadRight(sBuilderFixedWidth_30)}{doorB.DoorStartTransformPositionCurrent}");
                if (doorB.ParentRoomObject != null)
                {
                    doorB.CenterGridPosition_End(out var isDoubleCenter, out var center1, out var center2);
                    stringBuilder.AppendLine($"\t\t{"DoorB End Grid Position:".PadRight(sBuilderFixedWidth_30)} {center1}, {center1}");
                }
                stringBuilder.AppendLine();
            }



            /// <summary>
            /// 복도 재구성 성공
            /// </summary>
            public void Log_Place_Gen3_HallwayReconstruction_Success()
            {
                if (!logger.CurrentDebugMode) return;

                stringBuilder.AppendLine($"\t\t# Success Hallway Reconstrction!");
                stringBuilder.AppendLine();
            }



            /// <summary>
            /// 복도 재구성 실패, (금지 그리드에 해당되는 도어로부터 생성된 복도를 찾지못함)
            /// </summary>
            public void LogFailed_Place_Gen3_HallwayReconstruction_Failure_NotFoundCollisionHallways()
            {
                if (!logger.CurrentDebugMode) return;

                stringBuilder.AppendLine($"\t\t# Failed To Found Collision Hallways");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine($"\t# Failure Hallway Reconstrction");
                stringBuilder.AppendLine();
            }



            /// <summary>
            /// 복도 재구성 실패, (금지 그리드에 포함되어있는 복도를 해제하고, 금지 그리드를 점유시키고, 복도를 재생성 시켰으나 실패)
            /// </summary>
            public void LogFailed_Place_Gen3_HallwayReconstruction_Failure_ReconstructionCollisionNewHallways(RoomObject.Door doorA, RoomObject.Door doorB)
            {
                if (!logger.CurrentDebugMode) return;

                stringBuilder.AppendLine($"\t\t# Failed To Create Collision New Hallways");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine($"\t# Failure Hallway Reconstrction");
                stringBuilder.AppendLine();
            }



            /// <summary>
            /// 복도 재구성 실패, (복도 재구성 도중 또 복도 재구성이 시도되어 실패)
            /// </summary>
            /// <param name="door"></param>
            /// <param name="connectDoor"></param>
            public void LogFailed_Place_Gen3_HallwayReconstruction_IsOverlapedReconstruction()
            {
                if (!logger.CurrentDebugMode) return;

                stringBuilder.AppendLine($"\t\t# Failed Because Detected Overlapped Reconstrctuion");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine($"\t# Failure Hallway Reconstrction");
                stringBuilder.AppendLine();
            }



            /// <summary>
            /// 복도 재구성 디버깅: 금지 그리드 출력
            /// </summary>
            /// <param name="tempBannedGrids"></param>
            public void Log_Place_Gen3_HallwayReconstruction_PrintBannedGrids(ICollection<Grid> tempBannedGrids)
            {
                if (!logger.CurrentDebugMode) return;

                stringBuilder.Append($"\t\t{$"Temp Banned Grids [{tempBannedGrids.Count}]:".PadRight(sBuilderFixedWidth_40)}");
                foreach (var grid in tempBannedGrids)
                {
                    stringBuilder.Append($"{grid.GridPositionFixed}, ");
                }

                stringBuilder.AppendLine();
                stringBuilder.AppendLine();
            }



            /// <summary>
            /// 복도 재구성 디버깅: 금지 그리드에 포함되어있는 복도의 주인 도어 출력
            /// </summary>
            /// <param name="tempBannedGrids"></param>
            public void Log_Place_Gen3_HallwayReconstruction_PrintDoorsHaveCollisionHallways(ICollection<RoomObject.Door> collisionHallwayDoors)
            {
                if (!logger.CurrentDebugMode) return;

                stringBuilder.Append($"\t\t{$"Found Door With Collision Hallways [{collisionHallwayDoors.Count}]:".PadRight(sBuilderFixedWidth_40)}");
                foreach (var door in collisionHallwayDoors)
                {
                    stringBuilder.Append($"[{door.DoorDirection} Door");
                    if (door.ParentRoomObject != null)
                    {
                        door.CenterGridPosition_Start(out var isDoubleCenter, out var center1, out var center2);

                        if (isDoubleCenter)
                        {
                            stringBuilder.Append($"{center1} / {center2} ");
                        }
                        else
                        {
                            stringBuilder.Append($"{center1} ");
                        }

                        stringBuilder.Append($"[Parent: {door.ParentRoomObject.name} | {door.ParentRoomObject.GridPosition}]");
                    }
                    stringBuilder.Append("], ");
                }

                stringBuilder.AppendLine();
                stringBuilder.AppendLine();
            }



            /// <summary>
            /// 복도 재구성 디버깅: 금지 그리드에 포함되어있는 복도의 주인 도어의 새로운 복도 이어주기 정보 출력
            /// </summary>
            public void Log_Place_Gen3_HallwayReconstruction_PrintNewHallwaysByDoor(RoomObject.Door door, RoomObject.Door connectDoor)
            {
                if (!logger.CurrentDebugMode) return;

                stringBuilder.AppendLine($"\t\tWait to Create New Hallways, Avoid Temp Occupied Area");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine($"\t\t\t{"Door:".PadRight(sBuilderFixedWidth_30)} {door.DoorDirection} Door {((door.ParentRoomObject != null) ? $"\tParent: {door.ParentRoomObject.name}" : "")}");

                stringBuilder.AppendLine($"\t\t\t{"Door Position:".PadRight(sBuilderFixedWidth_30)}{door.DoorStartTransformPositionCurrent}");
                if (door.ParentRoomObject != null)
                {
                    door.CenterGridPosition_End(out var isDoubleCenter, out var center1, out var center2);
                    stringBuilder.AppendLine($"\t\t\t{"Door End Grid Position:".PadRight(sBuilderFixedWidth_30)}{center1}, {center1}");
                }

                stringBuilder.AppendLine($"\t\t\t{"ConnectDoor:".PadRight(sBuilderFixedWidth_30)} {connectDoor.DoorDirection} Door {((connectDoor.ParentRoomObject != null) ? $"\tParent: {connectDoor.ParentRoomObject.name}" : "")}");
                stringBuilder.AppendLine($"\t\t\t{"ConnectDoor Position:".PadRight(sBuilderFixedWidth_30)}{connectDoor.DoorStartTransformPositionCurrent}");
                if (connectDoor.ParentRoomObject != null)
                {
                    connectDoor.CenterGridPosition_End(out var isDoubleCenter, out var center1, out var center2);
                    stringBuilder.AppendLine($"\t\t\t{"ConnectDoor End Grid Position:".PadRight(sBuilderFixedWidth_30)} {center1}, {center1}");
                }

                stringBuilder.AppendLine();
            }



            //? Place_Gen3 서브: 복도 크로노 브레이크



            /// <summary>
            ///  복도 인접 크로노브레이크 준비
            /// </summary>
            public void Log_Place_Gen3_ChoronoBreak_ByHallways_ReGenerate()
            {
                if (!logger.CurrentDebugMode) return;

                stringBuilder.AppendLine($"\t# Ready ChoronoBreak By Hallways");
                stringBuilder.AppendLine();
            }



            /// <summary>
            ///  복도 인접 크로노브레이크 성공
            /// </summary>
            public void Log_Place_Gen3_ChoronoBreak_ByHallways_ReGenerate_Success(int choronoBreakAdditiion)
            {
                if (!logger.CurrentDebugMode) return;

                stringBuilder.AppendLine($"\t\t# Success ChoronoBreak By Hallways! [loopBackCount: {choronoBreakAdditiion * (choronoBreakAdditiion + 1) / 2}]");
                stringBuilder.AppendLine();
            }



            /// <summary>
            ///  복도 인접 크로노브레이크 에러
            /// </summary>
            public void LogError_Place_Gen3_ChoronoBreak_ByHallways_ReGenerate_Failure()
            {
                LogErrorCombine(sb =>
                {
                    sb.AppendLine($"\t\t# Failure ChoronoBreak By Hallways");
                    stringBuilder.AppendLine();
                }, false);
            }



            ///======================================================================================================================================================



            ///<summary>
            /// 패스파인딩 경로 찾기 실패
            /// </summary>
            public void LogError_Gen_PathFinding(StageGenerator main, Vector2Int start, Vector2Int end, int pathWidth, int pathHeight, bool expandPositiveHorizontal, bool expandPositiveVertical, PlaceManger.Generator_ASharpPathFinder.HeuristicType pathHeuristicType = PlaceManger.Generator_ASharpPathFinder.HeuristicType.Manhattan, float? pathHeuristicFactor = null, Vector2Int? clampBottomLeft = null, Vector2Int? clampTopRight = null, PlaceManger.Generator_ASharpPathFinder.FailedPathFinding_Retry failed_RetryAction = null, IReadOnlyCollection<Grid> IgnoreWhenExpandGrids_A = null, IReadOnlyCollection<Grid> IgnoreWhenExpandGrids_B = null)
            {
                LogErrorCombine_FailedPathFindingOrHallway(main, sb =>
                {
                    sb.AppendLine("!! <b><color=red>Pathfinding Error!</color></b>");
                    sb.AppendLine($"<b>Start,End</b>: <b><color=#2ecc71>{start}</color></b>~<b><color=#2ecc71>{end}</color></b>");
                    sb.AppendLine($"<b>pathWidth</b>: <b><color=#2ecc71>{pathWidth}</color></b>");
                    sb.AppendLine($"<b>pathHeight</b>: <b><color=#2ecc71>{pathHeight}</color></b>");
                    sb.AppendLine($"<b>expandPositiveHorizontal</b>: <b><color=#2ecc71>{expandPositiveHorizontal}</color></b>");
                    sb.AppendLine($"<b>expandPositiveVertical</b>: <b><color=#2ecc71>{expandPositiveVertical}</color></b>");
                    sb.AppendLine($"<b>pathHeuristicType</b>: <b><color=#2ecc71>{pathHeuristicType}</color></b>");
                    sb.AppendLine($"<b>pathHeuristicFactor</b>: <b><color=#2ecc71>{pathHeuristicFactor}</color></b>");
                    sb.AppendLine($"<b>clampBottomLeft</b>: <b><color=#2ecc71>{clampBottomLeft}</color></b>");
                    sb.AppendLine($"<b>clampTopRight</b>: <b><color=#2ecc71>{clampTopRight}</color></b>");
                    sb.AppendLine($"<b>failed_RetryAction</b>: <b><color=#2ecc71>{((failed_RetryAction != null) ? "used" : "not used")}</color></b>");
                    sb.AppendLine($"<b>IgnoreWhenExpandGrids_A</b>: <b><color=#2ecc71>{((IgnoreWhenExpandGrids_A != null) ? $"have {IgnoreWhenExpandGrids_A.Count} grids" : "null")}</color></b>");
                    sb.AppendLine($"<b>IgnoreWhenExpandGrids_B</b>: <b><color=#2ecc71>{((IgnoreWhenExpandGrids_B != null) ? $"have {IgnoreWhenExpandGrids_B.Count} grids" : "null")}</color></b>");
                }, false);
            }



            ///<summary>
            /// 비동기 패스파인딩 경로 찾기 실패
            /// </summary>
            public void LogError_Gen_PathFindingAsync(StageGenerator main, Vector2Int start, Vector2Int end, int pathWidth, int pathHeight, bool expandPositiveHorizontal, bool expandPositiveVertical, PlaceManger.Generator_ASharpPathFinder.HeuristicType pathHeuristicType = PlaceManger.Generator_ASharpPathFinder.HeuristicType.Manhattan, float? pathHeuristicFactor = null, Vector2Int? clampBottomLeft = null, Vector2Int? clampTopRight = null, PlaceManger.Generator_ASharpPathFinder.FailedPathFinding_RetryAsync failed_RetryAction = null, IReadOnlyCollection<Grid> IgnoreWhenExpandGrids_A = null, IReadOnlyCollection<Grid> IgnoreWhenExpandGrids_B = null)
            {
                LogErrorCombine_FailedPathFindingOrHallway(main, sb =>
                {
                    sb.AppendLine("!! <b><color=red>Pathfinding Error!</color></b>");
                    sb.AppendLine($"<b>Start,End</b>: <b><color=#2ecc71>{start}</color></b>~<b><color=#2ecc71>{end}</color></b>");
                    sb.AppendLine($"<b>pathWidth</b>: <b><color=#2ecc71>{pathWidth}</color></b>");
                    sb.AppendLine($"<b>pathHeight</b>: <b><color=#2ecc71>{pathHeight}</color></b>");
                    sb.AppendLine($"<b>expandPositiveHorizontal</b>: <b><color=#2ecc71>{expandPositiveHorizontal}</color></b>");
                    sb.AppendLine($"<b>expandPositiveVertical</b>: <b><color=#2ecc71>{expandPositiveVertical}</color></b>");
                    sb.AppendLine($"<b>pathHeuristicType</b>: <b><color=#2ecc71>{pathHeuristicType}</color></b>");
                    sb.AppendLine($"<b>pathHeuristicFactor</b>: <b><color=#2ecc71>{pathHeuristicFactor}</color></b>");
                    sb.AppendLine($"<b>clampBottomLeft</b>: <b><color=#2ecc71>{clampBottomLeft}</color></b>");
                    sb.AppendLine($"<b>clampTopRight</b>: <b><color=#2ecc71>{clampTopRight}</color></b>");
                    sb.AppendLine($"<b>failed_RetryAction</b>: <b><color=#2ecc71>{((failed_RetryAction != null) ? "used" : "not used")}</color></b>");
                    sb.AppendLine($"<b>IgnoreWhenExpandGrids_A</b>: <b><color=#2ecc71>{((IgnoreWhenExpandGrids_A != null) ? $"have {IgnoreWhenExpandGrids_A.Count} grids" : "null")}</color></b>");
                    sb.AppendLine($"<b>IgnoreWhenExpandGrids_B</b>: <b><color=#2ecc71>{((IgnoreWhenExpandGrids_B != null) ? $"have {IgnoreWhenExpandGrids_B.Count} grids" : "null")}</color></b>");
                }, false);
            }



            /// <summary>
            /// 패스파인딩 경로 너비 확장 재연산 횟수 초과
            /// </summary>
            public void LogError_Gen_PathFinding_InfinityLoop(StageGenerator main, int count)
            {
                LogErrorCombine_FailedPathFindingOrHallway(main, sb =>
                {
                    sb.AppendLine("<b><color=red>Failed Gen_PathFhiding!</color></b>");
                    sb.AppendLine($"<b><color=red>Detected Infinity ReCaculate:</color></b> <b><color=#2ecc71>{count}</color></b>");
                }, false);
            }



            ///======================================================================================================================================================



            public void EndLog()
            {
                if (!logger.CurrentDebugMode) return;

                if (UseRecordTime)
                {
                    StopWatch.Stop();
                }

                stringBuilder.AppendLine($"{"Reconstruction ConnectHallways Count:".PadRight(sBuilderFixedWidth_40)} [{Main.placeM.Reconstruction_ConnectHallways_ReportCount}]");
                //stringBuilder.AppendLine($"{"ChoronoBreak LoopBack ByHallways Count:".PadRight(sBuilderFixedWidth_40)} [{Main.placeM.Count_ChoronoBreakLoopBack_ByHallways}]");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine($"{"TOTAL TIME:".PadRight(sBuilderFixedWidth_15)}[{StopWatch.ElapsedMilliseconds}.ms]");
                stringBuilder.AppendLine($"{"END LOGGING".PadRight(sBuilderFixedWidth_15)}[{DateTime.Now:yyyy-MM-dd.HH:mm:ss}]");
                stringBuilder.AppendLine($"======================================================");

                ApplyExecute_Log();
            }



            public void ApplyExecute_Log()
            {
                if (!logger.CurrentDebugMode) return;

                logger.AppendFireAndForget(stringBuilder.ToString(true));
            }



            ///======================================================================================================================================================
        }
    }
}