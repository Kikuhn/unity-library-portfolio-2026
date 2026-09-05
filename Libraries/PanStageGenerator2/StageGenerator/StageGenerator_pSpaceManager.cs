using System.Collections.Generic;
using UnityEngine;
using Pan.Util;
using System;
using System.Text;
using Cysharp.Threading.Tasks;
using Pan.GridCompatibles2;
using Pan.StageGenerators;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine.Pool;



namespace Pan.StageGenerators
{
    public partial class StageGenerator
    {
        [Serializable]
        public class SpaceManager : BaseManager
        {
            ///======================================================================================================================================================



#if UNITY_EDITOR

            [TitleGroup("공간 매니저"), BoxGroup("공간 매니저/박스", false)]
            [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Center, EnableRichText = true, Overflow = false), EnableGUI]
            [PropertyOrder(-99)]
            [PropertySpace(8, 8)]
            private string dummyTitle_GridsInfo
            {
                get
                {
                    if (spaceBinaryTree != null && spaceBinaryTree.Count > 0)
                    {
                        return $"<color=white><size=13>공간 이진트리 목록 크기: <color=#2ecc71><b>{spaceBinaryTree.Count}</b></color>";
                    }
                    else
                    {
                        return $"<color=#ed5565><size=12>공간 이진 리스트가 유효하지 않음 (생성되지않거나 크기가 0)</size></color>";
                    }
                }
            }

#endif



            ///======================================================================================================================================================



            public IReadOnlyList<Space> SpacesBinaryTree => spaceBinaryTree;
            ///<summary>
            ///공간 이진트리 리스트
            ///</summary>
            [TitleGroup("공간 매니저"), BoxGroup("공간 매니저/박스", false)]
            [FoldoutGroup("공간 매니저/박스/공간 이진 트리")]
            [LabelText("공간 이진 트리")]
            [ListDrawerSettings(HideAddButton = true, HideRemoveButton = true, DraggableItems = false, ListElementLabelName = nameof(Space.SpaceIndex), DefaultExpandedState = false)]
            [HideDuplicateReferenceBox]
            [SerializeField]
            private List<Space> spaceBinaryTree = new List<Space>();



            #region 공간 생성 통계



            [TitleGroup("공간 매니저"), BoxGroup("공간 매니저/박스", false), FoldoutGroup("공간 매니저/박스/공간 생성 통계"), DisplayAsString, ShowInInspector, EnableGUI]
            [LabelText("최소값 노드")]
            public int Generation_MinNodeCount => generation_MinNodeCount;
            [SerializeField, HideInInspector]
            private int generation_MinNodeCount;



            [TitleGroup("공간 매니저"), BoxGroup("공간 매니저/박스", false), FoldoutGroup("공간 매니저/박스/공간 생성 통계"), DisplayAsString, ShowInInspector, EnableGUI]
            [LabelText("최대값 노드")]
            public int Generation_MaxNodeCount => generation_MaxNodeCount;
            [SerializeField, HideInInspector]
            private int generation_MaxNodeCount;


            [TitleGroup("공간 매니저"), BoxGroup("공간 매니저/박스", false), FoldoutGroup("공간 매니저/박스/공간 생성 통계"), DisplayAsString, ShowInInspector, EnableGUI]
            [LabelText("최소값 [하단] 노드")]
            public int Generation_MinNodeCount_Down => generation_MinNodeCount_Down;
            [SerializeField, HideInInspector]
            private int generation_MinNodeCount_Down;

            [TitleGroup("공간 매니저"), BoxGroup("공간 매니저/박스", false), FoldoutGroup("공간 매니저/박스/공간 생성 통계"), DisplayAsString, ShowInInspector, EnableGUI]
            [LabelText("최소값 [상단] 노드")]
            public int Generation_MinNodeCount_Up => generation_MinNodeCount_Up;
            [SerializeField, HideInInspector]
            private int generation_MinNodeCount_Up;

            [TitleGroup("공간 매니저"), BoxGroup("공간 매니저/박스", false), FoldoutGroup("공간 매니저/박스/공간 생성 통계"), DisplayAsString, ShowInInspector, EnableGUI]
            [LabelText("최소값 [좌측] 노드")]
            public int Generation_MinNodeCount_Left => generation_MinNodeCount_Left;
            [SerializeField, HideInInspector]
            private int generation_MinNodeCount_Left;

            [TitleGroup("공간 매니저"), BoxGroup("공간 매니저/박스", false), FoldoutGroup("공간 매니저/박스/공간 생성 통계"), DisplayAsString, ShowInInspector, EnableGUI]
            [LabelText("최소값 [우측] 노드")]
            public int Generation_MinNodeCount_Right => generation_MinNodeCount_Right;
            [SerializeField, HideInInspector]
            private int generation_MinNodeCount_Right;



            [TitleGroup("공간 매니저"), BoxGroup("공간 매니저/박스", false), FoldoutGroup("공간 매니저/박스/공간 생성 통계"), DisplayAsString, ShowInInspector, EnableGUI]
            [LabelText("최대값 [하단] 노드")]
            public int Generation_MaxNodeCount_Down => generation_MaxNodeCount_Down;
            [SerializeField, HideInInspector]
            private int generation_MaxNodeCount_Down;

            [TitleGroup("공간 매니저"), BoxGroup("공간 매니저/박스", false), FoldoutGroup("공간 매니저/박스/공간 생성 통계"), DisplayAsString, ShowInInspector, EnableGUI]
            [LabelText("최대값 [상단] 노드")]
            public int Generation_MaxNodeCount_Up => generation_MaxNodeCount_Up;
            [SerializeField, HideInInspector]
            private int generation_MaxNodeCount_Up;

            [TitleGroup("공간 매니저"), BoxGroup("공간 매니저/박스", false), FoldoutGroup("공간 매니저/박스/공간 생성 통계"), DisplayAsString, ShowInInspector, EnableGUI]
            [LabelText("최대값 [좌측] 노드")]
            public int Generation_MaxNodeCount_Left => generation_MaxNodeCount_Left;
            [SerializeField, HideInInspector]
            private int generation_MaxNodeCount_Left;

            [TitleGroup("공간 매니저"), BoxGroup("공간 매니저/박스", false), FoldoutGroup("공간 매니저/박스/공간 생성 통계"), DisplayAsString, ShowInInspector, EnableGUI]
            [LabelText("최대값 [우측] 노드")]
            public int Generation_MaxNodeCount_Right => generation_MaxNodeCount_Right;
            [SerializeField, HideInInspector]
            private int generation_MaxNodeCount_Right;



            ///<summary>
            /// 공간을 자른 횟수
            /// </summary>
            public int DivideSpaceCount => divideSpaceCount;
            [TitleGroup("공간 매니저"), BoxGroup("공간 매니저/박스", false), FoldoutGroup("공간 매니저/박스/공간 생성 통계"), DisplayAsString, ShowInInspector, EnableGUI]
            [LabelText("공간을 자른 횟수")]
            private int divideSpaceCount = 0;



            ///<summary>
            /// 방생성이 가능한 공간이 노드로 모두 연결되어있는지 여부
            /// </summary>
            public bool SpacesWithRoomCreatibleIsAllConnected => spacesWithRoomCreatibleIsAllConnected;
            [TitleGroup("공간 매니저"), BoxGroup("공간 매니저/박스", false), FoldoutGroup("공간 매니저/박스/공간 생성 통계"), DisplayAsString, ShowInInspector, EnableGUI]
            [LabelText("방생성이 가능한 공간이 노드로 모두 연결되어있는지 여부")]
            [LabelWidth(350)]
            private bool spacesWithRoomCreatibleIsAllConnected = false;



            #endregion



            ///======================================================================================================================================================



            //? Generate



            ///<summary>
            ///연산하여 <see cref="spaceBinaryTreeList"/>를 반환한다
            ///</summary>
            public bool Generate_SpaceList(out IReadOnlyList<Space> spaceBinaryTreeList)
            {
                Main.GenerateM.GeneratingCurrentSteps = GenerateManager.GenerateSteps.GenStep2_Spaces;

                //. 공간 이진트리 리스트를 초기화한다
                ClearSpaceManager();


                //. 최대 스테이지 크기를 기준으로, 공간을 분할한다
                Generate1_DivideSpaceRecursion();


                //. 공간이 모두 생성된 뒤에, 후작업을 해준다 (공간 Rect에비례한 방향당 최대노드 설정)
                Generate2_PostSettingSpaceList_Parallel(spaceBinaryTree); //. 병렬
                //Generate2_PostSettingSpaceList(Space_BinaryTreeList);


                //. 공간 이진트리 리스트로, 공간들을 노드로 이어준다
                Generate3_ConnectSpaceList(spaceBinaryTree);


                //. 생성이 끝난 공간들의 정보를 토대로 생성 정보를 기록한다
                Generate4_RecordGeneratedSpacesInfo();


                //. 공간 이진트리 리스트를 반환한다
                spaceBinaryTreeList = spaceBinaryTree;


                return true;
            }



            ///<summary>
            ///[비동기] 연산하여 <see cref="spaceBinaryTree"/>를 반환한다
            ///</summary>
            public async UniTask<IReadOnlyList<Space>> Generate_SpaceListAsync()
            {
                Main.GenerateM.GeneratingCurrentSteps = GenerateManager.GenerateSteps.GenStep2_Spaces;

                //. 공간 이진트리 리스트를 초기화한다
                ClearSpaceManager();


                //. 최대 스테이지 크기를 기준으로, 공간을 분할한다
                //. [비동기]
                await Generate1_DivideSpaceRecursionAsync();


                //. 공간이 모두 생성된 뒤에, 후작업을 해준다 (공간 Rect에비례한 방향당 최대노드 설정)
                await Generate2_PostSettingSpaceList_ParallelAsync(spaceBinaryTree);


                //. 공간 이진트리 리스트로, 공간들을 노드로 이어준다
                await Generate3_ConnectSpaceListAsync(spaceBinaryTree);


                //. 생성이 끝난 공간들의 정보를 토대로 생성 정보를 기록한다
                Generate4_RecordGeneratedSpacesInfo();


                //. 공간 이진트리 리스트를 반환한다
                return spaceBinaryTree;
            }



            ///======================================================================================================================================================



            //? SpaceList 제어



            ///<summary>
            ///<paramref name="spaceList"/>에 <paramref name="rect"/>를 추가
            ///</summary>
            private void AddSpaceList(List<Space> spaceList, in Rect rect)
            {
                spaceList.Add(new Space(Setting, rect, (Vector2Int)Main.TransformM.StageParentSnappedPositionCurrentCache));
            }



            ///======================================================================================================================================================



            /// <summary>
            /// SpaceList 초기화
            /// </summary>
            [TitleGroup("공간 매니저"), BoxGroup("공간 매니저/박스", false)]
            [Button("공간 매니저 초기화", Icon = SdfIconType.Trash), GUIColor(0.67f, 0.57f, 0.93f)]
            public void ClearSpaceManager()
            {
                generation_MinNodeCount = -1;
                generation_MaxNodeCount = -1;

                generation_MinNodeCount_Down = -1;
                generation_MinNodeCount_Up = -1;
                generation_MinNodeCount_Left = -1;
                generation_MinNodeCount_Right = -1;

                generation_MaxNodeCount_Down = -1;
                generation_MaxNodeCount_Up = -1;
                generation_MaxNodeCount_Left = -1;
                generation_MaxNodeCount_Right = -1;

                divideSpaceCount = -1;

                spacesWithRoomCreatibleIsAllConnected = false;

                spaceBinaryTree.Clear();
            }



            ///======================================================================================================================================================



            //? #1 공간 자르기



            ///<summary>
            /// 공간을 계속 자르는 재귀함수를 실행시킨다
            ///</summary>
            private void Generate1_DivideSpaceRecursion()
            {
                int divideSpaceWidthMin = Setting.Space.SpaceWidthMin;
                int divideSpaceWidthMax = Setting.Space.SpaceWidthMax;
                int divideSpaceHeightMin = Setting.Space.SpaceHeightMin;
                int divideSpaceHeightMax = Setting.Space.SpaceHeightMax;
                int divideSpaceUnitX = Setting.Space.SpaceDivideUnit;
                int divideSpaceUnitY = Setting.Space.SpaceDivideUnit;


                Rect stageRect = Setting.StageVector.StageRect;
                //stageRect.position += (Vector2)Main.TransformM.TEMPPPPPPPPP(false);


                divideSpaceCount = 0;


                //. 최대 스테이지 크기를 기준으로, 공간을 분할한다
                DivideSpaceRecursion(in stageRect, spaceBinaryTree
                    , divideSpaceWidthMin,
                    divideSpaceWidthMax,
                    divideSpaceHeightMin,
                    divideSpaceHeightMax,
                    divideSpaceUnitX,
                    divideSpaceUnitY);


                //. Rect Position: StageOrigin, Size: StageSize
                //. [이진트리 재귀함수]로 조건에 맞게 계속 공간이 분할되어 리스트에 각각 추가된다


                ApplySpaceIndex();
            }



            ///<summary>
            ///(재귀함수) 조건에 맞게 공간을 계속 잘라, <paramref name="spaceList"/>에 추가한다
            ///</summary>
            private void DivideSpaceRecursion(in Rect spaceRect, List<Space> spaceList, int divideSpaceWidthMin, int divideSpaceWidthMax, int divideSpaceHeightMin, int divideSpaceHeightMax, int divideSpaceUnitX, int divideSpaceUnitY)
            {
                divideSpaceCount++;


                //! 재귀종료 #1
                //. "공간의 총 개수가 최대 공간 수에 도달한 경우 (최대 공간수과 개수가 같거나 더 많을경우)"
                //! 재귀 종료
                if (spaceList.Count >= Setting.Space.SpaceCountMax) //나눌때마다 1개씩 늘어나기때문에 +1? 해야하나?
                {
                    AddSpaceList(spaceList, spaceRect);
                    return;
                }


                //! 확률적 재귀 종료
                //. @1 "재귀 중단 확률"을 사용중이고,
                //. @2 현재 생성된 공간이 최소 공간보다 많고,
                //. @3 현재 공간의 크기가 최대 공간 크기보다 같거나 작고 (공간을 더 분할할 필요가 없는지 확인, 최대 공간 크기보다 큰 값으로 분할되면 안되기떄문)
                //. @4 "재귀 중단 확률"에 성공했다면
                //! 재귀 종료
                if (Main.Setting.Space.StopDivideProbability != 0 && //? @1
                    Setting.Space.SpaceCountMin <= spaceList.Count && //? @2
                     (spaceRect.width <= divideSpaceWidthMax && spaceRect.height <= divideSpaceHeightMax && //? @3
                     Main.GenerateInfoM.Random.ValueFloat() < Main.Setting.Space.StopDivideProbability) //? @4
                    )
                {
                    //Debug.Log($"재귀중단#? {spaceRect.size}");
                    //Debug.Log($"확률적 재귀 종료 실행됨: {spaceRect.size} 최대너비보다 작은가? {spaceRect.width > divideSpaceWidthMax} 최대 높이보다 작은가?{spaceRect.height > divideSpaceHeightMax}");
                    AddSpaceList(spaceList, spaceRect);
                    return;
                }


                //! 재귀 종료 #2
                //. 현재 공간의 너비가 "최소 공간 너비" 보다 같거나 작고,
                //. 현재 공간의 높이가 "최소 공간 높이" 보다 같다면
                //! 재귀 종료
                //. 더 자르면 최소치보다 더 작아지기 때문에 종료한다
                if ((spaceRect.width <= divideSpaceWidthMin) && (spaceRect.height <= divideSpaceHeightMin))
                {
                    AddSpaceList(spaceList, spaceRect);
                    return;
                }


                //. 분할 방향 결정 (모드에 따라)
                bool splitHorizontally = DivideHorizontal_DecideByMode(spaceRect);

                //. 분할 지점 결정 (가로 or 세로)
                int splitPoint = splitHorizontally ?
                    CalculateSplitPoint(spaceRect.width, divideSpaceWidthMin, divideSpaceWidthMax, divideSpaceUnitX) :
                    CalculateSplitPoint(spaceRect.height, divideSpaceHeightMin, divideSpaceHeightMax, divideSpaceUnitY);


                //! 재귀종료 #3
                //. 유효한 분할 지점을 찾지 못한 경우,
                //! 재귀 종료
                //. 분할 단위를 이상하게 잡지 않는 이상, 대부분 #2에서 안전하게 걸러질것으로 추정
                if (splitPoint < 0)
                {
                    AddSpaceList(spaceList, spaceRect);
                    return;
                }


                //. 공간 A, B를 각각 누는다
                Rect spaceA, spaceB;
                if (splitHorizontally)
                {
                    spaceA = new Rect(spaceRect.x, spaceRect.y, splitPoint, spaceRect.height);
                    spaceB = new Rect(spaceRect.x + splitPoint, spaceRect.y, spaceRect.width - splitPoint, spaceRect.height);
                }
                else
                {
                    spaceA = new Rect(spaceRect.x, spaceRect.y, spaceRect.width, splitPoint);
                    spaceB = new Rect(spaceRect.x, spaceRect.y + splitPoint, spaceRect.width, spaceRect.height - splitPoint);
                }

                //? 최대 공간 개수가 허용하는 한, 공간A,B를 각각 재귀적으로 나눈다
                if (spaceList.Count < Setting.Space.SpaceCountMax) { DivideSpaceRecursion(spaceA, spaceList, divideSpaceWidthMin, divideSpaceWidthMax, divideSpaceHeightMin, divideSpaceHeightMax, divideSpaceUnitX, divideSpaceUnitY); }
                if (spaceList.Count < Setting.Space.SpaceCountMax) { DivideSpaceRecursion(spaceB, spaceList, divideSpaceWidthMin, divideSpaceWidthMax, divideSpaceHeightMin, divideSpaceHeightMax, divideSpaceUnitX, divideSpaceUnitY); }
            }



            ///<summary>
            /// [비동기] 공간을 계속 자르는 재귀함수를 실행시킨다
            ///</summary>
            private async UniTask Generate1_DivideSpaceRecursionAsync()
            {
                int divideSpaceWidthMin = Setting.Space.SpaceWidthMin;
                int divideSpaceWidthMax = Setting.Space.SpaceWidthMax;
                int divideSpaceHeightMin = Setting.Space.SpaceHeightMin;
                int divideSpaceHeightMax = Setting.Space.SpaceHeightMax;
                int divideSpaceUnitX = Setting.Space.SpaceDivideUnit;
                int divideSpaceUnitY = Setting.Space.SpaceDivideUnit;


                //Rect stageRect = new Rect(0, 0, Setting.StageVector.StageWidth, Setting.StageVector.StageHeight);
                Rect stageRect = Setting.StageVector.StageRect;
                divideSpaceCount = 0;


                //. 최대 스테이지 크기를 기준으로, 공간을 분할한다
                await DivideSpaceRecursionAsync(stageRect, spaceBinaryTree
                    , divideSpaceWidthMin,
                    divideSpaceWidthMax,
                    divideSpaceHeightMin,
                    divideSpaceHeightMax,
                    divideSpaceUnitX,
                    divideSpaceUnitY);


                //. Rect Position: StageOrigin, Size: StageSize
                //. [이진트리 재귀함수]로 조건에 맞게 계속 공간이 분할되어 리스트에 각각 추가된다


                ApplySpaceIndex();
            }



            ///<summary>
            ///(재귀함수) 조건에 맞게 공간을 계속 잘라, <paramref name="spaceList"/>에 추가한다
            ///</summary>
            private async UniTask DivideSpaceRecursionAsync(Rect spaceRect, List<Space> spaceList, int divideSpaceWidthMin, int divideSpaceWidthMax, int divideSpaceHeightMin, int divideSpaceHeightMax, int divideSpaceUnitX, int divideSpaceUnitY)
            {
                divideSpaceCount++;


                //? 프레임 양보
                if (divideSpaceCount % CalculateSetting.DivideSpaces_YieldUnitAsync == 0) { await UniTask.Yield(); }


                //! 재귀종료 #1
                //. "공간의 총 개수가 최대 공간 수에 도달한 경우 (최대 공간수과 개수가 같거나 더 많을경우)"
                //! 재귀 종료
                if (spaceList.Count >= Setting.Space.SpaceCountMax) //나눌때마다 1개씩 늘어나기때문에 +1? 해야하나?
                {
                    AddSpaceList(spaceList, spaceRect);
                    return;
                }


                //! 확률적 재귀 종료
                //. @1 "재귀 중단 확률"을 사용중이고,
                //. @2 현재 생성된 공간이 최소 공간보다 많고,
                //. @3 현재 공간의 크기가 최대 공간 크기보다 같거나 작고 (공간을 더 분할할 필요가 없는지 확인, 최대 공간 크기보다 큰 값으로 분할되면 안되기떄문)
                //. @4 "재귀 중단 확률"에 성공했다면
                //! 재귀 종료
                if (Main.Setting.Space.StopDivideProbability != 0 && //? @1
                    Setting.Space.SpaceCountMin <= spaceList.Count && //? @2
                     (spaceRect.width <= divideSpaceWidthMax && spaceRect.height <= divideSpaceHeightMax && //? @3
                     Main.GenerateInfoM.Random.ValueFloat() < Main.Setting.Space.StopDivideProbability) //? @4
                    )
                {
                    //Debug.Log($"재귀중단#? {spaceRect.size}");
                    //Debug.Log($"확률적 재귀 종료 실행됨: {spaceRect.size} 최대너비보다 작은가? {spaceRect.width > divideSpaceWidthMax} 최대 높이보다 작은가?{spaceRect.height > divideSpaceHeightMax}");
                    AddSpaceList(spaceList, spaceRect);
                    return;
                }


                //! 재귀 종료 #2
                //. 현재 공간의 너비가 "최소 공간 너비" 보다 같거나 작고,
                //. 현재 공간의 높이가 "최소 공간 높이" 보다 같다면
                //! 재귀 종료
                //. 더 자르면 최소치보다 더 작아지기 때문에 종료한다
                if ((spaceRect.width <= divideSpaceWidthMin) && (spaceRect.height <= divideSpaceHeightMin))
                {
                    AddSpaceList(spaceList, spaceRect);
                    return;
                }


                //. 분할 방향 결정 (모드에 따라)
                bool splitHorizontally = DivideHorizontal_DecideByMode(spaceRect);

                //. 분할 지점 결정 (가로 or 세로)
                int splitPoint = splitHorizontally ?
                    CalculateSplitPoint(spaceRect.width, divideSpaceWidthMin, divideSpaceWidthMax, divideSpaceUnitX) :
                    CalculateSplitPoint(spaceRect.height, divideSpaceHeightMin, divideSpaceHeightMax, divideSpaceUnitY);


                //! 재귀종료 #3
                //. 유효한 분할 지점을 찾지 못한 경우,
                //! 재귀 종료
                //. 분할 단위를 이상하게 잡지 않는 이상, 대부분 #2에서 안전하게 걸러질것으로 추정
                if (splitPoint < 0)
                {
                    AddSpaceList(spaceList, spaceRect);
                    return;
                }


                //. 공간 A, B를 각각 누는다
                Rect spaceA, spaceB;
                if (splitHorizontally)
                {
                    spaceA = new Rect(spaceRect.x, spaceRect.y, splitPoint, spaceRect.height);
                    spaceB = new Rect(spaceRect.x + splitPoint, spaceRect.y, spaceRect.width - splitPoint, spaceRect.height);
                }
                else
                {
                    spaceA = new Rect(spaceRect.x, spaceRect.y, spaceRect.width, splitPoint);
                    spaceB = new Rect(spaceRect.x, spaceRect.y + splitPoint, spaceRect.width, spaceRect.height - splitPoint);
                }

                //? 최대 공간 개수가 허용하는 한, 공간A,B를 각각 재귀적으로 나눈다
                if (spaceList.Count < Setting.Space.SpaceCountMax) { await DivideSpaceRecursionAsync(spaceA, spaceList, divideSpaceWidthMin, divideSpaceWidthMax, divideSpaceHeightMin, divideSpaceHeightMax, divideSpaceUnitX, divideSpaceUnitY); }
                if (spaceList.Count < Setting.Space.SpaceCountMax) { await DivideSpaceRecursionAsync(spaceB, spaceList, divideSpaceWidthMin, divideSpaceWidthMax, divideSpaceHeightMin, divideSpaceHeightMax, divideSpaceUnitX, divideSpaceUnitY); }
            }



            //? 공간 이진 트리의 Index 그대로, 각 공간의 SpaceIndex를 지정한다
            private void ApplySpaceIndex()
            {
                for (int i = 0; i < spaceBinaryTree.Count; i++)
                {
                    spaceBinaryTree[i].SpaceIndex = i;
                }
            }



            ///======================================================================================================================================================



            //? 공간 자르기 메서드



            /// <summary>
            /// 공간을 나눌 지점을 연산하여 반환, 자르기에 실패시 -1를 반환한다
            /// </summary>
            /// <param name="currentSize">공간의 현재 크기</param>
            /// <param name="minSize">공간의 최소 크기</param>
            /// <param name="maxSize">공간의 최대 크기</param>
            /// <param name="unit">분할 지점의 단위</param>
            /// <returns>계산된 분할 지점 또는 실패시 -1</returns>
            private int CalculateSplitPoint(float currentSize, int minSize, int maxSize, int unit)
            {
                // 최소 크기가 최대 크기보다 크거나 같은 경우, 또는 현재 크기가 최소 크기의 2배보다 작은 경우 분할 불가
                if (minSize > maxSize || currentSize < 2 * minSize)
                {
                    //Debug.LogWarning("분할 실패, 최소 크기가 최대 크기보다 크거나, 현재 크기의 보다 최소 크기의 2배가 큼");
                    return -1;
                }


                using (var pooled = ListPool<int>.Get(out var list)) //! 풀링 사용
                {
                    // 유효한 분할 범위를 구합니다
                    for (int i = minSize; i <= currentSize - minSize; i += unit)
                    {
                        float leftSize = i;
                        float rightSize = currentSize - i;

                        if (leftSize >= minSize && rightSize >= minSize)
                        {
                            list.Add(i);
                        }
                    }


                    // 유효한 분할 지점이 없는 경우 -1 반환
                    if (list.Count == 0)
                    {
                        return -1;
                    }


                    if (Setting.Space.DivideSmallASAPRandomLength != 0 && Main.GenerateInfoM.Random.ValueFloat() < Setting.Space.DivideSmallASAPRandomLength)
                    {
                        return list[0];
                    }


                    // 유효한 분할 지점 중 하나를 무작위로 선택
                    int result = list[Main.GenerateInfoM.Random.Range(0, list.Count)];

                    return result;
                }
            }



            /// <summary>
            /// 공간을 수평으로 분할할지 여부룰 반환한다 (모드에 따라서 연산방식이결정됨)
            /// </summary>
            private bool DivideHorizontal_DecideByMode(in Rect spaceRect)
            {
                if (spaceRect.width != spaceRect.height) { return spaceRect.width > spaceRect.height; }

                switch (Setting.Space.SpaceDivideMode)
                {
                    case StageGeneratorSetting.Spaces.EDivideMode.Priority_Horizontal:

                    return spaceRect.width > spaceRect.height;



                    case StageGeneratorSetting.Spaces.EDivideMode.Priority_Vertical:

                    return spaceRect.width <= spaceRect.height;



                    case StageGeneratorSetting.Spaces.EDivideMode.Random:

                    if (Main.GenerateInfoM.Random.ValueBool())
                    {
                        return spaceRect.width > spaceRect.height;
                    }
                    else
                    {
                        return spaceRect.width <= spaceRect.height;
                    }
                }

                return false;
            }



            ///======================================================================================================================================================



            //? #2 공간이 분할된 이후 후작업



            /// <summary>
            /// [병렬처리] 공간이 모두 생성된 뒤의 후작업 (공간 Rect에비례한 방향당 최대노드 설정)
            /// </summary>
            private void Generate2_PostSettingSpaceList_Parallel(List<Space> spaceList)
            {
                //. spaceList를 병렬로 순회
                spaceList.ParallelProcess(CalculateSetting.SpacePostSettingNode_BatchSize, space =>
                {
                    //. "노드 생성에 필요한 최소 공간 너비" 를 사용중이고, "공간 너비"가 이보다 작다면 (충족하지 않다면)
                    //.     하/상 최대 노드 개수는 0개가 된다
                    if (Setting.SpaceNodeConnect.UseNodeLimit_MinimumSpaceWidth && space.SpaceRect.width < Setting.SpaceNodeConnect.NodeLimit_MinimumSpaceWidth)
                    {
                        space.MaxNodeCount_Down = 0;
                        space.MaxNodeCount_Up = 0;
                    }
                    //. 별도의 생성 제약이 없다면,
                    //.     하/상 최대 노드 개수를 설정에 맞게 지정한다
                    else
                    {
                        space.MaxNodeCount_Down = Setting.SpaceNodeConnect.NodeMaxCount_Down;
                        space.MaxNodeCount_Up = Setting.SpaceNodeConnect.NodeMaxCount_Up;
                    }


                    //. "노드 생성에 필요한 최소 공간 높이" 를 사용중이고, "공간 높이"가 이보다 작다면 (충족하지 않다면)
                    //.     좌/우 최대 노드 개수는 0개가 된다
                    if (Setting.SpaceNodeConnect.UseNodeLimit_MinimumSpaceHeight && space.SpaceRect.height < Setting.SpaceNodeConnect.NodeLimit_MinimumSpaceHeight)
                    {
                        space.MaxNodeCount_Left = 0;
                        space.MaxNodeCount_Right = 0;
                    }
                    //. 별도의 생성 제약이 없다면,
                    //.     좌/우 최대 노드 개수를 설정에 맞게 지정한다
                    else
                    {
                        space.MaxNodeCount_Left = Setting.SpaceNodeConnect.NodeMaxCount_Left;
                        space.MaxNodeCount_Right = Setting.SpaceNodeConnect.NodeMaxCount_Right;
                    }


                    //. "공간의 크기로 노드 제약" 을 사용중일 경우
                    if (Setting.SpaceNodeConnect.UseNodeLimit_SpaceSize)
                    {
                        //. "공간의 너비로 하/상 노드 제약"을 사용할 경우
                        if (Setting.SpaceNodeConnect.UseNodeLimit_SpaceSize_Width)
                        {
                            //. "공간의 너비로 하/상 노드 제약"에 "추가 조건"이 활성화 되어있는지 확인한다
                            //.     "추가 높이 최소 크기" 조건에 불충족하거나, 충족 하더라도 공간의 크기가 해당 크기보다 같거나 크다면,
                            //.         노드 제약을 그대로 적용한다 ("공간 너비 / 노드 단위" 를 적용)
                            if (!Setting.SpaceNodeConnect.UseNodeLimit_SpaceSize_Width_ExtendLimitHeight ||
                            space.SpaceRect.height >= Setting.SpaceNodeConnect.NodeLimit_SpaceSize_Width_ExtendLimitHeight)
                            {
                                int node = Mathf.FloorToInt(space.SpaceRect.width / Setting.SpaceNodeConnect.NodeLimit_SpaceSize_Width);
                                space.MaxNodeCount_Down = node;
                                space.MaxNodeCount_Up = node;
                            }
                        }


                        //. "공간의 높이로 좌/우 노드 제약"을 사용할 경우
                        if (Setting.SpaceNodeConnect.UseNodeLimit_SpaceSize_Height)
                        {
                            //. "공간의 높이로 좌/우 노드 제약"에 "추가 조건"이 활성화 되어있는지 확인한다
                            //.     "추가 너비 최소 크기" 조건에 불충족하거나, 충족 하더라도 공간의 크기가 해당 크기보다 같거나 크다면,
                            //.         노드 제약을 그대로 적용한다 ("공간 높이 / 노드 단위" 를 적용)
                            if (!Setting.SpaceNodeConnect.UseNodeLimit_SpaceSize_Height_ExtendLimitWidth ||
                            space.SpaceRect.width >= Setting.SpaceNodeConnect.NodeLimit_SpaceSize_Height_ExtendLimitWidth)
                            {
                                int node = Mathf.FloorToInt(space.SpaceRect.height / Setting.SpaceNodeConnect.NodeLimit_SpaceSize_Height);
                                space.MaxNodeCount_Left = node;
                                space.MaxNodeCount_Right = node;
                            }
                        }
                    }
                });
            }



            /// <summary>
            /// [비동기+병렬] 공간 후작업을 백그라운드에서 Parallel로 돌린 뒤, 완료 시점을 await
            /// </summary>
            public async UniTask Generate2_PostSettingSpaceList_ParallelAsync(List<Space> spaceList)
            {
                //. (1) 백그라운드에서 병렬 처리
                await UniTask.RunOnThreadPool(() =>
                {
                    //. 여기서 Parallel.ForEach를 호출 -> 여러 스레드가 동시에 처리
                    spaceList.ParallelProcess(CalculateSetting.SpacePostSettingNode_BatchSize, space =>
                    {
                        //. "노드 생성에 필요한 최소 공간 너비" 를 사용중이고, "공간 너비"가 이보다 작다면 (충족하지 않다면)
                        //.     하/상 최대 노드 개수는 0개가 된다
                        if (Setting.SpaceNodeConnect.UseNodeLimit_MinimumSpaceWidth && space.SpaceRect.width < Setting.SpaceNodeConnect.NodeLimit_MinimumSpaceWidth)
                        {
                            space.MaxNodeCount_Down = 0;
                            space.MaxNodeCount_Up = 0;
                        }
                        //. 별도의 생성 제약이 없다면,
                        //.     하/상 최대 노드 개수를 설정에 맞게 지정한다
                        else
                        {
                            space.MaxNodeCount_Down = Setting.SpaceNodeConnect.NodeMaxCount_Down;
                            space.MaxNodeCount_Up = Setting.SpaceNodeConnect.NodeMaxCount_Up;
                        }


                        //. "노드 생성에 필요한 최소 공간 높이" 를 사용중이고, "공간 높이"가 이보다 작다면 (충족하지 않다면)
                        //.     좌/우 최대 노드 개수는 0개가 된다
                        if (Setting.SpaceNodeConnect.UseNodeLimit_MinimumSpaceHeight && space.SpaceRect.height < Setting.SpaceNodeConnect.NodeLimit_MinimumSpaceHeight)
                        {
                            space.MaxNodeCount_Left = 0;
                            space.MaxNodeCount_Right = 0;
                        }
                        //. 별도의 생성 제약이 없다면,
                        //.     좌/우 최대 노드 개수를 설정에 맞게 지정한다
                        else
                        {
                            space.MaxNodeCount_Left = Setting.SpaceNodeConnect.NodeMaxCount_Left;
                            space.MaxNodeCount_Right = Setting.SpaceNodeConnect.NodeMaxCount_Right;
                        }


                        //. "공간의 크기로 노드 제약" 을 사용중일 경우
                        if (Setting.SpaceNodeConnect.UseNodeLimit_SpaceSize)
                        {
                            //. "공간의 너비로 하/상 노드 제약"을 사용할 경우
                            if (Setting.SpaceNodeConnect.UseNodeLimit_SpaceSize_Width)
                            {
                                //. "공간의 너비로 하/상 노드 제약"에 "추가 조건"이 활성화 되어있는지 확인한다
                                //.     "추가 높이 최소 크기" 조건에 불충족하거나, 충족 하더라도 공간의 크기가 해당 크기보다 같거나 크다면,
                                //.         노드 제약을 그대로 적용한다 ("공간 너비 / 노드 단위" 를 적용)
                                if (!Setting.SpaceNodeConnect.UseNodeLimit_SpaceSize_Width_ExtendLimitHeight ||
                                space.SpaceRect.height >= Setting.SpaceNodeConnect.NodeLimit_SpaceSize_Width_ExtendLimitHeight)
                                {
                                    int node = Mathf.FloorToInt(space.SpaceRect.width / Setting.SpaceNodeConnect.NodeLimit_SpaceSize_Width);
                                    space.MaxNodeCount_Down = node;
                                    space.MaxNodeCount_Up = node;
                                }
                            }


                            //. "공간의 높이로 좌/우 노드 제약"을 사용할 경우
                            if (Setting.SpaceNodeConnect.UseNodeLimit_SpaceSize_Height)
                            {
                                //. "공간의 높이로 좌/우 노드 제약"에 "추가 조건"이 활성화 되어있는지 확인한다
                                //.     "추가 너비 최소 크기" 조건에 불충족하거나, 충족 하더라도 공간의 크기가 해당 크기보다 같거나 크다면,
                                //.         노드 제약을 그대로 적용한다 ("공간 높이 / 노드 단위" 를 적용)
                                if (!Setting.SpaceNodeConnect.UseNodeLimit_SpaceSize_Height_ExtendLimitWidth ||
                                space.SpaceRect.width >= Setting.SpaceNodeConnect.NodeLimit_SpaceSize_Height_ExtendLimitWidth)
                                {
                                    int node = Mathf.FloorToInt(space.SpaceRect.height / Setting.SpaceNodeConnect.NodeLimit_SpaceSize_Height);
                                    space.MaxNodeCount_Left = node;
                                    space.MaxNodeCount_Right = node;
                                }
                            }
                        }
                    });
                });
            }



            ///======================================================================================================================================================



            //? #3 공간 이어주기



            ///<summary>
            ///공간들을 설정에 맞게 이어준다
            ///</summary>
            private void Generate3_ConnectSpaceList(List<Space> spaceList)
            {
                //. 우선 모든 공간들을 이웃 공간을 찾아주고, 그 딕셔너리를 얻는다
                CalculateSpaceWithNeighborDictionary_Parallel(spaceList, out var spaceWithNeighborDictionary);


                //. 공간 연결
                StartConnectSpaces(spaceWithNeighborDictionary, spaceList); //. 병렬처리 불가능


                //. 모든 공간들의 연결선이 하나로 이어지게하기
                if (Setting.SpaceNodeConnect.UseCombineSpaceNodes) { SetConnectToOneLine(spaceList); } //. 병렬처리 불가능


                //. 모든 공간의 방향별 노드들을 모두 재정렬 (상하는 좌우순, 좌우는 하상순)
                //SortConnectingSpace(spaceList);
                SortConnectingSpace_Parallel(spaceList);
            }



            ///<summary>
            ///[비동기] 공간들을 설정에 맞게 이어준다
            ///</summary>
            private async UniTask Generate3_ConnectSpaceListAsync(List<Space> spaceList)
            {
                //. 우선 모든 공간들을 이웃 공간을 찾아주고, 그 딕셔너리를 얻는다
                var spaceWithNeighborDictionary = await CalculateSpaceWithNeighborDictionary_ParallelAsync(spaceList);


                //. 공간 연결
                StartConnectSpaces(spaceWithNeighborDictionary, spaceList); //. 병렬처리 불가능


                //. 모든 공간들의 연결선이 하나로 이어지게하기
                if (Setting.SpaceNodeConnect.UseCombineSpaceNodes) { SetConnectToOneLine(spaceList); } //. 병렬처리 불가능


                //. 모든 공간의 방향별 노드들을 모두 재정렬 (상하는 좌우순, 좌우는 하상순)
                await SortConnectingSpace_ParallelAsync(spaceList);
            }



            /// <summary>
            /// [병렬] 공간들의 이웃 공간을 찾아, 결과 딕셔너리를 반환한다
            /// </summary>
            private void CalculateSpaceWithNeighborDictionary_Parallel(IReadOnlyList<Space> spaceList, out Dictionary<Space, List<Space>> spaceWithNeighborDictionary)
            {
                //. 임시 결과를 저장할 배열 (spaceList.Count 크기)
                //. 각 인덱스 i => (space: spaceList[i], 연결된 Space들)
                //! 병렬을 사용하기에, 나름 스레드 세이프 하게 사용할수있어 사용한다
                var spaceWithNeighbors = new (Space space, List<Space> neighborSpaces)[spaceList.Count];


                //. 병렬로 2중 루프를 돌리는 대신,
                //. 1중 루프(Parallel.For)에서 "내부 1중 루프"는 동기화
                Parallel.For(0, spaceList.Count, i =>
                {
                    //! (spaceList 중 하나의 space, index는 i)

                    var currentSpace = spaceList[i];
                    var neighborSpaces = new List<Space>(currentSpace.TotalMaxNodeCount);


                    //. 이 Space와 다른 Space들을 비교 하기 위해, 순회 한다
                    for (int j = 0; j < spaceList.Count; j++)
                    {
                        //. 다른 Space
                        var otherSpace = spaceList[j];

                        //? 다른 공간이 이 공간과 다르고, 다른 공간이 이 공간과 "인접" 해 있다면, "이웃 공간"이 된다
                        if (currentSpace != otherSpace && currentSpace.SpaceRect.CheckRectsAreClose(otherSpace.SpaceRect, true))
                        {
                            neighborSpaces.Add(otherSpace);
                        }
                    }

                    //. 스레드 안전하게, 결과를 localResults[i]에만 저장
                    spaceWithNeighbors[i] = (currentSpace, neighborSpaces);
                });


                //. 이제 병렬 처리가 모두 끝난 상태이므로,
                //. 배열을 Dictionary로 변환한다
                spaceWithNeighborDictionary = new Dictionary<Space, List<Space>>(spaceList.Count);
                foreach (var (space, neighborSpaces) in spaceWithNeighbors)
                {
                    spaceWithNeighborDictionary.Add(space, neighborSpaces);

                    //. 해당 공간에 "연산된 인접한 이웃 공간들" 을 할당한다
                    space.NeighborSpaces = neighborSpaces;
                }
            }



            /// <summary>
            /// [비동기 + 병렬] 공간들의 이웃 공간을 찾아, 결과 딕셔너리를 반환한다
            /// </summary>
            private async UniTask<Dictionary<Space, List<Space>>> CalculateSpaceWithNeighborDictionary_ParallelAsync(IReadOnlyList<Space> spaceList)
            {
                //. 병렬 작업 리스트 생성
                using (var pooled = ListPool<UniTask<(Space space, List<Space> neighborSpaces)>>.Get(out var tasks)) //. Dispose 시 자동 Clear & Release
                {
                    tasks.Capacity = spaceList.Count; //. 재할당 최소화 (EnsureCapacity 미사용)

                    for (int i = 0; i < spaceList.Count; i++)
                    {
                        int index = i; //. 캡쳐된 변수 방지
                        tasks.Add(UniTask.RunOnThreadPool(() =>
                        {
                            var currentSpace = spaceList[index];
                            var neighborSpaces = new List<Space>(); //. 소유권이 외부로 넘어가므로 풀링 금지

                            //. 이 Space와 다른 Space들을 비교 하기 위해, 순회 한다
                            for (int j = 0; j < spaceList.Count; j++)
                            {
                                //. 다른 Space
                                var otherSpace = spaceList[j];

                                //? 다른 공간이 이 공간과 다르고, 다른 공간이 이 공간과 "인접" 해 있다면, "이웃 공간"이 된다
                                if (currentSpace != otherSpace && currentSpace.SpaceRect.CheckRectsAreClose(otherSpace.SpaceRect, true))
                                {
                                    neighborSpaces.Add(otherSpace);
                                }
                            }

                            //. (currentSpace, neighborSpaces) 반환
                            return (currentSpace, neighborSpaces);
                        }));
                    }

                    //. 모든 작업 완료 대기
                    var localResults = await UniTask.WhenAll(tasks);

                    //. Dictionary 구성 (원본 유지)
                    var spaceWithNeighborDictionary = new Dictionary<Space, List<Space>>(spaceList.Count);
                    foreach (var (space, neighborSpaces) in localResults)
                    {
                        spaceWithNeighborDictionary.Add(space, neighborSpaces);

                        //. 해당 공간에 "연산된 인접한 이웃 공간들" 을 할당한다
                        space.NeighborSpaces = neighborSpaces;
                    }

                    return spaceWithNeighborDictionary;
                }
            }



            /// <summary>
            /// 공간들을 연결한다
            /// </summary>
            private void StartConnectSpaces(Dictionary<Space, List<Space>> spaceWithNeighborDictionary, List<Space> spaceList)
            {
                for (int i = 0; i < spaceList.Count; i++)
                {
                    Space space = spaceList[i];

                    //! 이 공간이 공간 기준에 미달이라면 스킵
                    if (!space.CanCreateRoom) { continue; }

                    //. 이웃 딕셔너리에서 이 공간의 이웃 공간 리스트를 새로 복사해온다
                    //! 연산 도중 리스트에 변화(제거)가 필요하기에 복사하여 생성한다
                    using (var pooled = ListPool<Space>.Get(out var neighborSpaces)) //? 임시 복사본 풀링
                    {
                        neighborSpaces.Capacity = spaceWithNeighborDictionary[space].Count; //. EnsureCapacity 대신
                        neighborSpaces.AddRange(spaceWithNeighborDictionary[space]);

                        //. 최대 노드 범위 개수 정하기
                        int nodeRandomLengthMax;

                        //. "최대한 모든 공간 연결" 을 사용할 경우
                        //.     "각 공간별 최대 노드 개수 설정"과 "이웃 공간 개수"중 큰 값이 최대 노드 개수가 된다
                        if (Setting.SpaceNodeConnect.UseCombineSpaceNodes)
                        {
                            nodeRandomLengthMax = Mathf.Max(Setting.SpaceNodeConnect.NodeCountMax, neighborSpaces.Count);
                        }
                        //. "최대한 모든 공간 연결" 을 사용하지 않을경우
                        //.     50% 확률로, "각 공간별 최대 노드 개수 설정" 또는 "이웃 공간 개수"가 최대 노드 개수가 된다
                        else
                        {
                            nodeRandomLengthMax = Main.GenerateInfoM.Random.ValueBool() ? Setting.SpaceNodeConnect.NodeCountMax : neighborSpaces.Count;
                        }

                        //. 이 공간에 정할 노드 개수 (while문에서 사용할수록 점점 소모됨)
                        int nodeCount = Main.GenerateInfoM.Random.Range(0, nodeRandomLengthMax);

                        //. 이 공간이 다른 공간과 연결되어있는 총 개수 < 이 공간에 정할 남은 노드 개수
                        //. and
                        //. 이웃 리스트가 남아있는한
                        //? 무한히 반복하며 공간들을 최대한 이어준다
                        while (space.TotalConnectingSpacesCount < nodeCount && neighborSpaces.Count > 0)
                        {
                            //. 남은 이웃 공간 리스트의 무작위 인덱스를 지정한다
                            int randNeighborIndex = Main.GenerateInfoM.Random.Range(0, neighborSpaces.Count);

                            //. 그 무작위 인덱스의 이웃 공간을 얻어온다
                            Space randNeighrborSpace = neighborSpaces[randNeighborIndex];

                            //! 이웃 공간이 공간 기준에 미달이라면, 연결을 못하니 리스트에서 제거한뒤 스킵한다
                            if (!randNeighrborSpace.CanCreateRoom)
                            {
                                neighborSpaces.RemoveAt(randNeighborIndex);
                                nodeCount--;
                                continue;
                            }

                            //. 이웃 공간이 다른 공간과 연결되어있는 총 개수가, 설정의 최대 노드 개수보다 작다면
                            //.     (이웃 공간에 노드 추가 가능)
                            //? 이어준다
                            if (randNeighrborSpace.TotalConnectingSpacesCount < Setting.SpaceNodeConnect.NodeCountMax)
                            {
                                Space.TryConnectTwoSpaces(space, randNeighrborSpace);
                            }

                            //. 리스트에서 이 무작위 공간을 제거한다
                            neighborSpaces.RemoveAt(randNeighborIndex);
                            nodeCount--;
                        }
                    } //. using 종료 시 자동 반환
                }
            }



            /// <summary>
            /// [병렬] 모든 공간의 방향별 노드들을 모두 재정렬 (상하는 좌우순, 좌우는 하상순)
            /// </summary>
            private void SortConnectingSpace_Parallel(List<Space> spaceList)
            {
                spaceList.ParallelProcess(CalculateSetting.SpacePostSettingSortNodes_BatchSize, space =>
                 {
                     //space.SortConnecting_Spaces();
                     space.SortConnecting_SpacesParallel();
                 });
            }



            /// <summary>
            /// [비동기 + 병렬] 모든 공간의 방향별 노드들을 재정렬 (상하는 좌우순, 좌우는 하상순)
            /// </summary>
            /// <param name="spaceList">정렬할 Space 목록</param>
            /// <returns>비동기 작업 완료를 나타내는 UniTask</returns>
            private async UniTask SortConnectingSpace_ParallelAsync(IReadOnlyList<Space> spaceList)
            {
                int batchSize = Mathf.Max(1, CalculateSetting.SpacePostSettingSortNodes_BatchSize);
                int index = 0;

                while (index < spaceList.Count)
                {
                    int end = Mathf.Min(index + batchSize, spaceList.Count);

                    // 현재 배치 작업 생성
                    using (var pooled = ListPool<UniTask>.Get(out var tasks)) //! 배치 단위 임시 작업 리스트
                    {
                        int requiredCapacity = end - index;
                        if (tasks.Capacity < requiredCapacity) { tasks.Capacity = requiredCapacity; }

                        for (int i = index; i < end; i++)
                        {
                            tasks.Add(spaceList[i].SortConnecting_SpacesParallelAsync());
                        }

                        // 현재 배치 작업 대기
                        await UniTask.WhenAll(tasks);
                    } //. 즉시 반환

                    index = end;
                }
            }



            ///======================================================================================================================================================



            //? #4 생성이 완료된 공간들의 정보를 토대로 기록하기



            ///<summary>
            ///생성이 끝난 공간들의 정보를 토대로 생성 정보를 기록한다
            /// </summary>
            private void Generate4_RecordGeneratedSpacesInfo()
            {
                generation_MinNodeCount = int.MaxValue;
                generation_MaxNodeCount = int.MinValue;

                generation_MinNodeCount_Down = int.MaxValue;
                generation_MinNodeCount_Up = int.MaxValue;
                generation_MinNodeCount_Left = int.MaxValue;
                generation_MinNodeCount_Right = int.MaxValue;

                generation_MaxNodeCount_Down = int.MinValue;
                generation_MaxNodeCount_Up = int.MinValue;
                generation_MaxNodeCount_Left = int.MinValue;
                generation_MaxNodeCount_Right = int.MinValue;


                //. 생성 정보 기록
                for (int i = 0; i < spaceBinaryTree.Count; i++)
                {
                    //. Index 지정
                    Space space = spaceBinaryTree[i];

                    int nodeCount = space.TotalConnectingSpacesCount;
                    int nodeCount_Down = space.CurrentNodeCount_Down;
                    int nodeCount_Up = space.CurrentNodeCount_Up;
                    int nodeCount_Left = space.CurrentNodeCount_Left;
                    int nodeCount_Right = space.CurrentNodeCount_Right;


                    if (generation_MinNodeCount >= nodeCount) { generation_MinNodeCount = nodeCount; }
                    if (generation_MaxNodeCount <= nodeCount) { generation_MaxNodeCount = nodeCount; }

                    if (generation_MinNodeCount_Down >= nodeCount_Down) { generation_MinNodeCount_Down = nodeCount_Down; }
                    if (generation_MinNodeCount_Up >= nodeCount_Up) { generation_MinNodeCount_Up = nodeCount_Up; }
                    if (generation_MinNodeCount_Left >= nodeCount_Left) { generation_MinNodeCount_Left = nodeCount_Left; }
                    if (generation_MinNodeCount_Right >= nodeCount_Right) { generation_MinNodeCount_Right = nodeCount_Right; }

                    if (generation_MaxNodeCount_Down <= nodeCount_Down) { generation_MaxNodeCount_Down = nodeCount_Down; }
                    if (generation_MaxNodeCount_Up <= nodeCount_Up) { generation_MaxNodeCount_Up = nodeCount_Up; }
                    if (generation_MaxNodeCount_Left <= nodeCount_Left) { generation_MaxNodeCount_Left = nodeCount_Left; }
                    if (generation_MaxNodeCount_Right <= nodeCount_Right) { generation_MaxNodeCount_Right = nodeCount_Right; }
                }

                //. 노드가 모든 방 보유가 가능한 공간과 분리되어있지 않고 연결되었는지 기록
                spacesWithRoomCreatibleIsAllConnected = CheckSpacesConnected(spaceBinaryTree);
            }



            /// <summary>
            ///  모든 공간이 노드를 통해 하나로 이어져 있는지 BFS 로 확인한다
            ///  (연결 조건에 미달한 Space 는 검사 대상에서 제외)
            /// </summary>
            private bool CheckSpacesConnected(IReadOnlyList<Space> spaceList)
            {
                //. 연결 대상 필터링
                using (var pooledValid = ListPool<Space>.Get(out var validSpaces))
                using (var pooledVisited = HashSetPool<Space>.Get(out var visited))
                {
                    foreach (var s in spaceList) { if (s.CanCreateRoom) validSpaces.Add(s); }

                    if (validSpaces.Count <= 1) { return true; }  //. 0·1개면 당연히 연결

                    var q = new Queue<Space>(); //. Queue<T>는 기본 풀 없음(필요시 ObjectPool<Queue<T>>로 커스텀 가능)

                    q.Enqueue(validSpaces[0]);
                    visited.Add(validSpaces[0]);

                    while (q.Count > 0)
                    {
                        var cur = q.Dequeue();

                        foreach (var next in cur.TotalConnectingSpaces)     //. Space 가 보유한 실제 연결 리스트
                        {
                            //! 연결 대상이 아니거나 이미 방문
                            if (!next.CanCreateRoom || !visited.Add(next)) { continue; }

                            q.Enqueue(next);
                        }
                    }

                    return visited.Count == validSpaces.Count;         //? 모두 방문했는가
                }
            }



            ///======================================================================================================================================================



            //? 공간 노드 하나로 이어주기



            /// <summary>
            /// 공간들의 노드를 하나로 이어준다
            /// </summary>
            private void SetConnectToOneLine(List<Space> spaceList)
            {
                // MST + UnionFind 방식
                SetConnectToOneLine_MST_UnionFind(spaceList);
            }



            /// <summary>
            /// 최소 스패닝 트리(Kruskal)용 Union-Find(Disjoint Set) 예시.
            /// 필요에 따라 별도 파일/클래스로 분리해도 됩니다.
            /// </summary>
            public class UnionFind
            {
                private readonly int[] parent;
                private readonly int[] rank;

                public UnionFind(int n)
                {
                    parent = new int[n];
                    rank = new int[n];
                    for (int i = 0; i < n; i++)
                    {
                        parent[i] = i;
                        rank[i] = 0;
                    }
                }

                /// <summary> x의 루트(대표) 노드 인덱스를 찾음 (경로 압축)</summary>
                public int Find(int x)
                {
                    if (parent[x] == x) return x;
                    return parent[x] = Find(parent[x]);
                }

                /// <summary> x와 y가 속한 집합을 합침(Union by rank)</summary>
                /// <returns>합쳐졌으면 true, 이미 같은 집합이면 false</returns>
                public bool Union(int x, int y)
                {
                    int rx = Find(x);
                    int ry = Find(y);
                    if (rx == ry) return false; // 이미 같은 그룹

                    // 낮은 rank를 높은 rank 밑으로
                    if (rank[rx] < rank[ry])
                    {
                        parent[rx] = ry;
                    }
                    else if (rank[rx] > rank[ry])
                    {
                        parent[ry] = rx;
                    }
                    else
                    {
                        parent[ry] = rx;
                        rank[rx]++;
                    }

                    return true;
                }
            }



            /// <summary>
            /// MST(크루스칼) + Union-Find로 모든 Space를 하나로 이음.
            /// 기존 "SetConnectToOneLine"를 대체할 수 있는 방식.
            /// </summary>
            private void SetConnectToOneLine_MST_UnionFind(List<Space> spaceList)
            {
                // 1) 연결 가능한 Space만 추려서 인덱스 부여
                var validSpaces = ListPool<Space>.Get();
                var indexOf = DictionaryPool<Space, int>.Get();

                for (int i = 0; i < spaceList.Count; i++)
                {
                    Space sp = spaceList[i];
                    if (sp.CanCreateRoom)
                    {
                        indexOf[sp] = validSpaces.Count;
                        validSpaces.Add(sp);
                    }
                }

                int n = validSpaces.Count;
                if (n <= 1)
                {
                    // 유효한 공간이 0개 또는 1개면 연결할 필요가 없음
                    return;
                }

                // 2) 간선 후보 만들기
                using (var pooledEdges = ListPool<(float dist, Space A, Space B)>.Get(out var edges))
                {
                    foreach (var space in validSpaces)
                    {
                        var nearSpaces = space.NeighborSpaces;
                        if (nearSpaces == null)
                            continue;

                        foreach (var other in nearSpaces)
                        {
                            if (!other.CanCreateRoom)
                                continue;
                            if (space == other)
                                continue;

                            // 거리 계산 (sqrMagnitude로 충분)
                            float dist = (space.SpaceRect.center - other.SpaceRect.center).sqrMagnitude;
                            edges.Add((dist, space, other));
                        }
                    }

                    // 3) 거리 기준 오름차순 정렬
                    edges.Sort((x, y) => x.dist.CompareTo(y.dist));

                    // 4) Union-Find 초기화
                    var uf = new UnionFind(n);

                    // 5) 크루스칼 알고리즘: 간선 순회하며 연결 시도
                    int usedEdges = 0;

                    foreach (var (dist, A, B) in edges)
                    {
                        int idxA = indexOf[A];
                        int idxB = indexOf[B];

                        // 이미 같은 그룹이면 패스
                        if (uf.Find(idxA) == uf.Find(idxB))
                            continue;

                        // "연결 가능 횟수" 제한 체크
                        if (A.TotalConnectingSpacesCount >= Setting.SpaceNodeConnect.NodeCountMax)
                            continue;
                        if (B.TotalConnectingSpacesCount >= Setting.SpaceNodeConnect.NodeCountMax)
                            continue;

                        // 그룹 병합 시도
                        bool merged = uf.Union(idxA, idxB);
                        if (!merged)
                            continue; // 이미 같은 그룹 or 오류

                        // 실제 연결 (기존 코드의 연결 로직 사용)
                        Space.TryConnectTwoSpaces(A, B);
                        usedEdges++;
                    }
                }

                validSpaces.Clear();
                indexOf.Clear();
                ListPool<Space>.Release(validSpaces);
                DictionaryPool<Space, int>.Release(indexOf);
            }



            ///======================================================================================================================================================
        }
    }
}
