using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Spine;
using Spine.Unity;
using System.Text;
using Pan.Util;
using Pan.SpinePackage;
using SitraUtils;
using Pan.SpineUtil;
using Sirenix.OdinInspector;



namespace Pan.SpinePackage
{
    /// <summary>
    /// 애니메이션 MixDuration의 값을 가지고 있는 인터페이스
    /// </summary>
    public interface IAnimationMixDuration
    {
        /// <summary>
        /// 애니메이션의 MixDuration, 또는 Blend
        /// </summary>
        float AnimationMixDuration { get; }
    }



    /// <summary>
    /// 턴-뷰 8방향 열거형
    /// </summary>
    public enum ETurnViews8
    {
        /// <summary>
        /// 정면
        /// </summary>
        Front,
        /// <summary>
        /// 정면-좌측
        /// </summary>
        Front_L,
        /// <summary>
        /// 정면-우측
        /// </summary>
        Front_R,
        /// <summary>
        /// 측면-좌측
        /// </summary>
        Side_L,
        /// <summary>
        /// 측면-우측
        /// </summary>
        Side_R,
        /// <summary>
        /// 후면
        /// </summary>
        Back,
        /// <summary>
        /// 후면-좌측
        /// </summary>
        Back_L,
        /// <summary>
        /// 후면-우측
        /// </summary>
        Back_R
    }



    /// <summary>
    /// <see cref="ETurnViews8"/> 방향을 분류하기 위한 규칙(그룹) 열거형
    /// <para>턴뷰 방향이 특정 분류 규칙에 속하는지 판정할 때 사용한다</para>
    /// <para>예: 전면 계열 / 후면 계열 / 좌측 / 우측 / Front/Back Only 등</para>
    /// </summary>
    public enum ETurnViewRule
    {
        /// <summary>
        /// 전면 계열(Front, Front_L, Front_R)
        /// <para>정면을 바라보는 방향 그룹</para>
        /// <para>Front 기반 분기 처리에 사용</para>
        /// </summary>
        FrontFamily,

        /// <summary>
        /// 후면 계열(Back, Back_L, Back_R)
        /// <para>후면을 바라보는 방향 그룹</para>
        /// <para>Back 기반 분기 처리에 사용</para>
        /// </summary>
        BackFamily,

        /// <summary>
        /// 좌측 계열(Front_L, Side_L, Back_L)
        /// <para>좌측 방향 그룹</para>
        /// <para>좌우 판정(Left/Right) 또는 반전 분기에 사용</para>
        /// </summary>
        Left,

        /// <summary>
        /// 우측 계열(Front_R, Side_R, Back_R)
        /// <para>우측 방향 그룹</para>
        /// <para>좌우 판정(Left/Right) 또는 반전 분기에 사용</para>
        /// </summary>
        Right,

        /// <summary>
        /// Front 또는 Back "정확히" 일 때만 true (LR 제외)
        /// <para>Front_L/Front_R/Back_L/Back_R 는 포함하지 않는다</para>
        /// <para>정면/후면 중앙 방향만 구분해야 할 때 사용</para>
        /// </summary>
        FrontOrBackOnly,
    }



    /// <summary>
    /// 턴-뷰 관리자 클래스
    /// </summary>
    [Serializable]
    public class TurnViewManagement
    {
        ///======================================================================================================================================================



        //. 생성자



        /// <summary>
        /// <paramref name="turnDirection"/>이 <see cref="ETurnViews8.Front"/> 일 경우, <paramref name="turnViewAnimation"/> 내의 <see cref="DrawOrderTimeline"/> 을 사용하지 않고, <see cref="Skeleton"/>의 기본 슬롯 순서를 사용한다<br/>
        /// </summary>
        public TurnViewManagement(SkelSbject skelSbject, ETurnViews8 turnDirection, Dictionary<ETurnViews8, Spine.Animation> standardTurnViewSpineAnimations, int slotBundleCapacity)
        {
            int slotCount = skelSbject.Slot_Dictionary.Count;


            CurrentTurnDirection = turnDirection;
            SlotBundleListDictionary = new Dictionary<string, List<SlotData>>(slotBundleCapacity);
            SlotsSlotBundleCachingDictionary = new Dictionary<SlotData, string>(slotCount);
            SlotIndexDrawOrderArray_Default = skelSbject.SlotIndexesDrawOrder_Array;

            //. 현재 턴뷰 방향의 스파인 기준이 되는 턴뷰 Spine.Animation
            var currentTurnDirection_StandardSpineAnimation = standardTurnViewSpineAnimations[turnDirection];


            #region 슬롯번들 작업 (겸사겸사 우선적으로 수행해야하는 다른 작업도 함)

            //? "정면" 턴뷰 매니지먼트를 만드는 경우, "정면" Spine.Animation의 DrawOrderTimeline을 사용 하는 것이 아닌, 기본 슬롯 드로우오더 배열을 그대로 사용한다
            //.     "정면" 이 기준이기에, "정면"의 DrawOrderTimeline과 기본 슬롯 드로우오더 배열이 똑같은 탓에, DrawOrderTimeline 내부에는 아무 정보도 들어 있지 않아 사용이 불가능하기 때문
            if (turnDirection == ETurnViews8.Front)
            {
                //. (SkeletonData 내에 있는 슬롯 데이터 순서대로) 번들 딕셔너리를 만든다
                foreach (var slotData in skelSbject.Slot_Dictionary.dataArray)
                {
                    AddSlotBundleInfo_ToDictionary(slotData);
                }

                SlotIndexDrawOrderArray_Current = skelSbject.SlotIndexesDrawOrder_Array;
            }

            //? "정면" 이외의 턴뷰 매니지먼트를 만드는 경우, 해당 턴뷰 Spine.Animation의 DrawOrderTimeline을 사용한다
            //! 단, 해당 턴뷰 애니메이션에 DrawOrderTimeline이 존재하지 않거나, 비정상적인 경우 예외를 발생시킨다
            else if (TryGetSlotIndexDrawOrder(currentTurnDirection_StandardSpineAnimation, slotCount, out var slotIndexdrawOrderArray))
            {
                //. (받아온 슬롯 인덱스 드로우오더 배열 순서대로) 번들 딕셔너리를 만든다
                for (int i = 0; i < slotIndexdrawOrderArray.Length; i++)
                {
                    AddSlotBundleInfo_ToDictionary(skelSbject.GetSlotData(slotIndexdrawOrderArray[i]));
                }

                SlotIndexDrawOrderArray_Current = slotIndexdrawOrderArray;
            }

            //! 예외 처리
            else
            {
                throw new Exception($"{currentTurnDirection_StandardSpineAnimation.Name} 애니메이션의 DrawOrderTimeline 이상 발생");
            }


            //. 번들 딕셔너리들 마무리 정리
            SlotsSlotBundleCachingDictionary.TrimExcess();
            SlotBundleListDictionary.TrimExcess();
            foreach (var item in SlotBundleListDictionary) { item.Value.TrimExcess(); }


            //! 에디터 전용 번들 네임 딕셔너리 생성
#if UNITY_EDITOR
            Editor_BundleNameDictionary = new Dictionary<string, List<string>>(SlotBundleListDictionary.Count);
            foreach (KeyValuePair<string, List<SlotData>> bundle in SlotBundleListDictionary)
            {
                var slotNames = new List<string>(bundle.Value.Count);
                for (int i = 0; i < bundle.Value.Count; i++) { slotNames.Add(bundle.Value[i].Name); }
                Editor_BundleNameDictionary.Add(bundle.Key, slotNames);
            }
#endif

            #endregion


            #region 슬롯 DrawOrder 작업

            //? SlotIndex 배열을 순회하며, 슬롯DrawOrder 리스트를 생성한다
            CurrentSlotDrawOrderList = new List<SlotData>(slotCount);
            for (int i = 0; i < SlotIndexDrawOrderArray_Current.Length; i++)
            {
                var slotIndex = SlotIndexDrawOrderArray_Current[i];
                CurrentSlotDrawOrderList.Add(skelSbject.GetSlotData(slotIndex));
            }


            //! 에디터 전용으로 디버깅을 위해SlotName을 역순으로 정리한 리스트도 생성한다
#if UNITY_EDITOR
            Editor_CurrentSlotNameDrawOrderList = new List<string>(CurrentSlotDrawOrderList.Count);
            for (int i = CurrentSlotDrawOrderList.Count - 1; i >= 0; i--)
            {
                Editor_CurrentSlotNameDrawOrderList.Add(CurrentSlotDrawOrderList[i].Name);
            }
#endif

            #endregion
        }



        private static bool TryGetSlotIndexDrawOrder(Spine.Animation animation, int slotCount, out int[] slotIndexDrawOrder)
        {
            slotIndexDrawOrder = null;

            if (animation.TryGetTimeLineFromAnimation<DrawOrderTimeline>(out var drawOrderTimeline) &&
                TryGetFirstDrawOrder(drawOrderTimeline.DrawOrders, out var drawOrder))
            {
                slotIndexDrawOrder = (int[])drawOrder.Clone();
                return true;
            }

            return TryBuildSlotIndexDrawOrderFromFolderTimelines(animation, slotCount, out slotIndexDrawOrder);
        }


        private static int[] GetSlotIndexDrawOrder(Spine.Animation animation, int slotCount)
        {
            if (!TryGetSlotIndexDrawOrder(animation, slotCount, out var slotIndexDrawOrder))
            {
                throw new Exception($"{animation.Name} 애니메이션의 DrawOrderTimeline 이상 발생");
            }

            return slotIndexDrawOrder;
        }


        private static bool TryGetFirstDrawOrder(int[][] drawOrders, out int[] drawOrder)
        {
            if (drawOrders != null &&
                drawOrders.Length > 0 &&
                drawOrders[0] != null &&
                drawOrders[0].Length > 0)
            {
                drawOrder = drawOrders[0];
                return true;
            }

            drawOrder = null;
            return false;
        }


        private static bool TryBuildSlotIndexDrawOrderFromFolderTimelines(Spine.Animation animation, int slotCount, out int[] slotIndexDrawOrder)
        {
            slotIndexDrawOrder = null;

            bool hasFolderTimeline = false;
            int[] workingDrawOrder = new int[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                workingDrawOrder[i] = i;
            }

            int timelineCount = animation.Timelines.Count;
            for (int i = 0; i < timelineCount; i++)
            {
                if (animation.Timelines.Items[i] is DrawOrderFolderTimeline folderTimeline)
                {
                    hasFolderTimeline = true;
                    ApplyFolderTimelineToSlotIndexDrawOrder(workingDrawOrder, slotCount, folderTimeline);
                }
            }

            if (!hasFolderTimeline) { return false; }

            slotIndexDrawOrder = workingDrawOrder;
            return true;
        }


        private static void ApplyFolderTimelineToSlotIndexDrawOrder(int[] slotIndexDrawOrder, int slotCount, DrawOrderFolderTimeline folderTimeline)
        {
            int[] folderSlots = folderTimeline.Slots;
            if (folderSlots == null || folderSlots.Length == 0) { return; }

            TryGetFirstDrawOrder(folderTimeline.DrawOrders, out var folderDrawOrder);
            if (folderDrawOrder != null && folderDrawOrder.Length < folderSlots.Length) { return; }

            bool[] inFolder = new bool[slotCount];
            for (int i = 0; i < folderSlots.Length; i++)
            {
                int slotIndex = folderSlots[i];
                if (slotIndex >= 0 && slotIndex < slotCount)
                {
                    inFolder[slotIndex] = true;
                }
            }

            for (int i = 0, found = 0; i < slotIndexDrawOrder.Length && found < folderSlots.Length; i++)
            {
                int slotIndex = slotIndexDrawOrder[i];
                if (slotIndex < 0 || slotIndex >= slotCount || !inFolder[slotIndex]) { continue; }

                int folderSlotIndex = folderDrawOrder == null ? found : folderDrawOrder[found];
                if (folderSlotIndex < 0 || folderSlotIndex >= folderSlots.Length) { return; }

                slotIndexDrawOrder[i] = folderSlots[folderSlotIndex];
                found++;
            }
        }



        ///======================================================================================================================================================



        [ShowInInspector]
        [ReadOnly]
        [LabelText("현재 턴뷰 방향")]
        public readonly ETurnViews8 CurrentTurnDirection;



        ///======================================================================================================================================================



        //. 슬롯 번들



        /// <summary>
        /// SlotData들이 슬롯번들의 문자열을 Key로 사용하여 분류되어있는 딕셔너리
        /// </summary>
        [ShowInInspector]
        [ReadOnly]
        [LabelText("k: SlotBundle v:List<SlotData> 딕셔너리")]
        private readonly Dictionary<string, List<SlotData>> SlotBundleListDictionary;



        /// <summary>
        /// SlotData의 슬롯번들 스키마를 얻을수있는 딕셔너리
        /// </summary>
        [HideInInspector]
        [ReadOnly]
        [LabelText("k:SlotData v:SlotBundleName 캐싱 딕셔너리")]
        private readonly Dictionary<SlotData, string> SlotsSlotBundleCachingDictionary;



#if UNITY_EDITOR

        [ShowInInspector]
        [ReadOnly]
        [LabelText("슬롯 번들 Name 딕셔너리 (Editor)")]
        [PropertyTooltip("가독성을 위해 역순으로 정렬 되어있음")]
        private readonly Dictionary<string, List<string>> Editor_BundleNameDictionary; //! 디버깅용아니냐 이것도
#endif



        //? SlotData를 통해, 스키마를 확인하여 번들 슬롯 딕셔너리에 각각 추가한다

        /// <summary>
        /// <see cref="SlotData"/> 를 받아와, 이름을 확인하여 번들 딕셔너리들에게 추가한다
        /// </summary>
        /// <param name="slotData"></param>
        private void AddSlotBundleInfo_ToDictionary(SlotData slotData)
        {
            var slotDataName = slotData.Name;


            //? 스키마 문자열을 확인하여, 각 슬롯들을 번들 딕셔너리에 추가한다
            if (slotDataName.Contains("{c01}")) addToSlotBundleDictionarys("c01", slotData);
            else if (slotDataName.Contains("{c02}")) addToSlotBundleDictionarys("c02", slotData);
            else if (slotDataName.Contains("{c03}")) addToSlotBundleDictionarys("c03", slotData);
            else if (slotDataName.Contains("{c04}")) addToSlotBundleDictionarys("c04", slotData);
            else if (slotDataName.Contains("{c05}")) addToSlotBundleDictionarys("c05", slotData);
            else if (slotDataName.Contains("{c06}")) addToSlotBundleDictionarys("c06", slotData);
            else if (slotDataName.Contains("{c07}")) addToSlotBundleDictionarys("c07", slotData);
            else if (slotDataName.Contains("{c08}")) addToSlotBundleDictionarys("c08", slotData);
            else if (slotDataName.Contains("{c09}")) addToSlotBundleDictionarys("c09", slotData);
            else if (slotDataName.Contains("{x} haircapfront")) addToSlotBundleDictionarys("haircapfront", slotData);
            else if (slotDataName.Contains("{x} haircapback")) addToSlotBundleDictionarys("haircapback", slotData);
            else if (slotDataName.Contains("{x} subshoulderL")) addToSlotBundleDictionarys("subshoulderL", slotData);
            else if (slotDataName.Contains("{x} subshoulderR")) addToSlotBundleDictionarys("subshoulderR", slotData);
            else if (slotDataName.Contains("{x} backhip")) addToSlotBundleDictionarys("backhip", slotData);
            else if (slotDataName.Contains("{x} wingL")) addToSlotBundleDictionarys("wingL", slotData);
            else if (slotDataName.Contains("{x} wingR")) addToSlotBundleDictionarys("wingR", slotData);
            else if (slotDataName.Contains("{x} backhair")) addToSlotBundleDictionarys("backhair", slotData);
            //! 스키마에 속하지 않는 슬롯들은 etc로 분류한다!
            else { addToSlotBundleDictionarys("etc", slotData); }


            void addToSlotBundleDictionarys(string slotBundleName, SlotData slotData)
            {
                if (!SlotBundleListDictionary.ContainsKey(slotBundleName))
                {
                    SlotBundleListDictionary.Add(slotBundleName, new List<SlotData>());
                }
                SlotBundleListDictionary[slotBundleName].Add(slotData);
                SlotsSlotBundleCachingDictionary.Add(slotData, slotBundleName);
            }
        }



        /// <summary>
        /// SlotData의 슬롯번들 스키마 이름을 반환한다 
        /// </summary>
        /// <param name="slotData"></param>
        /// <returns></returns>
        public string GetSlotBundleSchemaName(SlotData slotData)
        {
            return SlotsSlotBundleCachingDictionary[slotData];
        }



        ///======================================================================================================================================================



        ///<summary>
        /// 현재 <see cref="CurrentTurnDirection"/>  턴뷰 내에 SlotData들의 드로우오더 리스트
        /// </summary>
        [HideInInspector]
        [ReadOnly]
        [LabelText("현재 턴뷰 SlotData DrawOrder 리스트")]
        private readonly List<SlotData> CurrentSlotDrawOrderList;



#if UNITY_EDITOR

        ///<summary>
        /// 현재 <see cref="CurrentTurnDirection"/>  턴뷰 내에 SlotData들의 드로우오더 리스트 (슬롯 이름 버전)
        /// </summary>
        [ShowInInspector]
        [ReadOnly]
        [LabelText("슬롯 번들 Name DrawOrder 리스트 (Editor)")]
        [PropertyTooltip("가독성을 위해 역순으로 정렬 되어있음")]
        private List<string> Editor_CurrentSlotNameDrawOrderList;

#endif



        /// <summary>
        /// SlotIndex DrawOrder 배열 (기본 스켈레톤 기준) 
        /// </summary>
        [ShowInInspector]
        [ReadOnly]
        [LabelText("기본 스켈레톤 SlotIndex DrawOrder 배열")]
        public readonly int[] SlotIndexDrawOrderArray_Default;



        /// <summary>
        /// <see cref="CurrentTurnDirection"/> 턴뷰 기준 애니메이션 내의 SlotIndex DrawOrder 배열 
        /// </summary>
        [ShowInInspector]
        [ReadOnly]
        [LabelText("현재 턴뷰 SlotIndex DrawOrder 배열")]
        public readonly int[] SlotIndexDrawOrderArray_Current;



        ///======================================================================================================================================================



        [Serializable]
        public abstract class Extend { }



        [Serializable]
        public class HeadExtend : Extend
        {
            public HeadExtend(SkelSbject skelSbject, TurnViewManagement turnViewManagement, Dictionary<ETurnViews8, Spine.Animation> standardTurnViewSpineAnimations, int customDrawOrderTrackIndex, string attachmentTag_Head)
            {
                #region 현재 턴뷰 방향의 머리 턴뷰 커스텀 드로우오더 SkelAni 딕셔너리 제작

                int turnViewCapacity = 8;

                //. 현재 턴뷰 방향의 머리 턴뷰 커스텀 드로우오더 SkelAni 딕셔너리 제작
                HeadTurnView_CustomDrawOrderSkelAniDictionary = new Dictionary<ETurnViews8, SkelAni>(turnViewCapacity);


                makeHeadTurnViews(ETurnViews8.Front);
                makeHeadTurnViews(ETurnViews8.Front_L);
                makeHeadTurnViews(ETurnViews8.Front_R);
                makeHeadTurnViews(ETurnViews8.Side_L);
                makeHeadTurnViews(ETurnViews8.Side_R);
                makeHeadTurnViews(ETurnViews8.Back);
                makeHeadTurnViews(ETurnViews8.Back_L);
                makeHeadTurnViews(ETurnViews8.Back_R);


                //! 만들어진 머리 턴뷰 커스텀 드로우오더가 어떤 순서로 만들어졌는지 확인하는 에디터 전용 딕셔너리 생성
#if UNITY_EDITOR
                Editor_HeadTurnView_CustomDrawOrderSkelAniDrawOrderSlotNameDictionary = new Dictionary<ETurnViews8, List<string>>(turnViewCapacity);
                foreach (var item in HeadTurnView_CustomDrawOrderSkelAniDictionary)
                {
                    int[] slotIndexes = GetSlotIndexDrawOrder(item.Value.Animation, skelSbject.Slot_Dictionary.Count);
                    var slotNameList = new List<string>(slotIndexes.Length);
                    for (int i = slotIndexes.Length - 1; i >= 0; i--)
                    {
                        slotNameList.Add(skelSbject.GetSlotData(slotIndexes[i]).Name);
                    }

                    Editor_HeadTurnView_CustomDrawOrderSkelAniDrawOrderSlotNameDictionary.Add(item.Key, slotNameList);
                }
#endif


                //. 현재 방향을 기준으로 머리만 회전했을때, 그 DrawOrder가 적용된 애니메이션을 제작하는 메서드
                void makeHeadTurnView_CustomDrawOrderSpineAnimation(ETurnViews8 headTurnViewDirection, out Spine.Animation createdAnimation)
                {
                    string aniName = $"HeadTurnViewCustomDrawOrder_{turnViewManagement.CurrentTurnDirection}_{headTurnViewDirection}";


                    //. 타임라인ExposedList에 추가될 DrawOrderTimeLine
                    DrawOrderTimeline createdDrawOrderTimeline = new DrawOrderTimeline(1);


                    //. DrawOrderTimeLine에 설정될 DrawOrder 배열
                    int[] createdDrawOrders = new int[skelSbject.Slot_Dictionary.Count];


                    //? Front면 애니메이션에서 드로우오더 따오는게 아니라 기본에서 따오기
                    if (headTurnViewDirection == ETurnViews8.Front)
                    {
                        createdDrawOrders = SU_SpinePackage.RebuildDrawOrderByReferenceBundles(turnViewManagement.SlotIndexDrawOrderArray_Current, turnViewManagement.SlotIndexDrawOrderArray_Default, skelSbject.GetSlotData, (slotData) => turnViewManagement.GetSlotBundleSchemaName(slotData), (slotData) => slotData.Name.Contains(attachmentTag_Head));
                    }

                    //? 받아온 애니메이션의 DrawOrderTimeline에서 순서를 따옴
                    else
                    {
                        var headTurnViewSpineAnimationDrawOrder = GetSlotIndexDrawOrder(standardTurnViewSpineAnimations[headTurnViewDirection], skelSbject.Slot_Dictionary.Count);
                        createdDrawOrders = SU_SpinePackage.RebuildDrawOrderByReferenceBundles(turnViewManagement.SlotIndexDrawOrderArray_Current, headTurnViewSpineAnimationDrawOrder, skelSbject.GetSlotData, (slotData) => turnViewManagement.GetSlotBundleSchemaName(slotData), (slotData) => slotData.Name.Contains(attachmentTag_Head));
                    }


                    #region 기존 기본/턴뷰 드로우오더를 별도 처리 없이 그대로 사용하던 버전 (이걸 사용하면 Head 외의 다른 슬롯들의 드로우 오더도 의도와 다르게 조정 되어 버림 260203



                    ////? Front면 애니메이션에서 드로우오더 따오는게 아니라 기본에서 따오기
                    //if (headTurnViewDirection == ETurnViews8.Front)
                    //{
                    //    //. 기본 드로우오더 배열 설정
                    //    for (int i = 0; i < DefaultSlotIndexDrawOrderArray.Length; i++)
                    //    {
                    //        createdDrawOrders[i] = DefaultSlotIndexDrawOrderArray[i];
                    //    }
                    //}

                    ////? 받아온 애니메이션의 DrawOrderTimeline에서 순서를 따옴
                    //else
                    //{
                    //    var baseAniDrawOrders = TurnViewSpineAnimations[headTurnViewDirection].GetTimeLineFromAnimation<DrawOrderTimeline>().DrawOrders[0];

                    //    for (int i = 0; i < baseAniDrawOrders.Length; i++)
                    //    {
                    //        createdDrawOrders[i] = baseAniDrawOrders[i];
                    //    }
                    //    //! 이거 합쳐서 Array.Copy로하면 더 좋을듯
                    //}


                    #endregion


                    //. 설정된 DrawOrder 배열 적용시고, Spine.Animation 생성
                    createdDrawOrderTimeline.SetFrame(0, 0, createdDrawOrders);
                    createdAnimation = new Spine.Animation(aniName);
                    createdAnimation.SetTimelines(new ExposedList<Timeline>() { createdDrawOrderTimeline }, new ExposedList<int>());
                    createdAnimation.Duration = 0;
                }

                //. DrawOrderTimeline이 적용된 Spine.Animation을 CustomDrawOrderTrack 트랙을 사용하는 SkelAni로 제작하여 딕셔너리에 추가하는 메서드
                void makeHeadTurnViewCustomDrawOrderSpineAnimation_SkelAni_AndAddToDictionary(ETurnViews8 headTurnViewDirection, Spine.Animation headTurnViewCustomDrawOrderSpineAnimation)
                {
                    HeadTurnView_CustomDrawOrderSkelAniDictionary.Add(headTurnViewDirection, new SkelAni(customDrawOrderTrackIndex, headTurnViewCustomDrawOrderSpineAnimation, 0, 0, true, false, 0, 0, 1));
                }

                //. 위 2개 메서드 통합
                void makeHeadTurnViews(ETurnViews8 headTurnViewDirection)
                {
                    makeHeadTurnView_CustomDrawOrderSpineAnimation(headTurnViewDirection, out var customDrawOrderSpineAnimation);
                    makeHeadTurnViewCustomDrawOrderSpineAnimation_SkelAni_AndAddToDictionary(headTurnViewDirection, customDrawOrderSpineAnimation);
                }

                #endregion
            }



            ///<summary>
            /// <see cref="CurrentTurnDirection"/>을 기준으로, <b>머리</b>만 각각 방향으로 회전한 CustomDrawOrder가 적용된 <see cref="SkelAni"/> 가 저장되어있는 딕셔너리
            /// </summary>
            [HideInInspector]
            [ReadOnly]
            [LabelText("머리 턴뷰 CustomDrawOrder SkelAni 딕셔너리")]
            private readonly Dictionary<ETurnViews8, SkelAni> HeadTurnView_CustomDrawOrderSkelAniDictionary;



#if UNITY_EDITOR
            ///<summary>
            /// <see cref="CurrentTurnDirection"/>을 기준으로, <b>머리</b>만 각각 방향으로 회전한 CustomDrawOrder가 적용된 <see cref="SkelAni"/>의 DrawOrder가 SlotName으로 저장되어있는 딕셔너리
            /// </summary>
            [ShowInInspector]
            [ReadOnly]
            [LabelText("머리 턴뷰 CustomDrawOrder SkelAni의 슬롯 DrawOrder 딕셔너리 (Editor)")]
            private readonly Dictionary<ETurnViews8, List<string>> Editor_HeadTurnView_CustomDrawOrderSkelAniDrawOrderSlotNameDictionary;
#endif



            /// <summary>
            /// <see cref="CurrentTurnDirection"/>을 기준으로, <b>머리</b>만 <paramref name="headTurnViewDirection"/> 방향으로 회전한 CustomDrawOrder가 적용된 <see cref="SkelAni"/> 를 반환
            /// </summary>
            /// <param name="headTurnViewDirection">머리 턴뷰 방향</param>
            /// <returns></returns>
            public SkelAni GetTurnHead_CustomDrawOrderSkelAni(ETurnViews8 headTurnViewDirection)
            {
                return HeadTurnView_CustomDrawOrderSkelAniDictionary[headTurnViewDirection];
            }



        }



        [ShowInInspector, ReadOnly]
        [ShowIf(nameof(UseExtends))]
        ///<summary>
        /// <see cref="TurnViewManagement"/>를 더 확장할수 있는 <see cref="Extend"/>들을 관리하는 딕셔너리
        /// </summary>
        private Dictionary<Type, Extend> Extends = null;



        public bool UseExtends => Extends != null && Extends.Count > 0;



        /// <summary>
        ///  <typeparamref name="TExtend"/> 확장 모듈을 추가한다
        /// </summary>
        /// <typeparam name="TExtend"></typeparam>
        /// <param name="extend"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void AddExtend<TExtend>(TExtend extend) where TExtend : Extend
        {
            Extends ??= new Dictionary<Type, Extend>();
            if (extend == null) { throw new ArgumentNullException(nameof(extend), "Extend 인스턴스는 null일 수 없습니다."); }
            if (!Extends.TryAdd(typeof(TExtend), extend))
            {
                throw new ArgumentException($"이미 {typeof(TExtend).Name} 타입의 Extend가 추가되어 있습니다.", nameof(extend));
            }
        }



        /// <summary>
        ///  <typeparamref name="TExtend"/> 확장 모듈을 얻어본다
        /// </summary>
        /// <typeparam name="TExtend"></typeparam>
        /// <param name="extend"></param>
        /// <returns></returns>
        public bool TryGetExtend<TExtend>(out TExtend extend) where TExtend : Extend
        {
            if (Extends != null && Extends.TryGetValue(typeof(TExtend), out var foundExtend))
            {
                extend = (TExtend)foundExtend;
                return true;
            }
            extend = null;
            return false;
        }



        /// <summary>
        /// <typeparamref name="TExtend"/> 확장 모듈을 얻어온다 
        /// </summary>
        /// <typeparam name="TExtend"></typeparam>
        /// <returns></returns>
        public TExtend GetExtend<TExtend>() where TExtend : Extend
        {
            return Extends[typeof(TExtend)] as TExtend;
        }



        ///======================================================================================================================================================
    }



    public abstract class TurnView8Manager
    {
        ///======================================================================================================================================================



        [ShowInInspector, ReadOnly]
        [ShowIf(nameof(UseExtends))]
        ///<summary>
        /// <see cref="TurnView8Manager"/>를 더 확장할수 있는 <see cref="Extend"/>들을 관리하는 딕셔너리
        /// </summary>
        private Dictionary<Type, Extend> Extends = null;



        public bool UseExtends => Extends != null && Extends.Count > 0;



        /// <summary>
        ///  <typeparamref name="TExtend"/> 확장 모듈을 추가한다
        /// </summary>
        /// <typeparam name="TExtend"></typeparam>
        /// <param name="extend"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void AddExtend<TExtend>(TExtend extend) where TExtend : Extend
        {
            Extends ??= new Dictionary<Type, Extend>();
            if (extend == null) { throw new ArgumentNullException(nameof(extend), "Extend 인스턴스는 null일 수 없습니다."); }
            if (!Extends.TryAdd(typeof(TExtend), extend))
            {
                throw new ArgumentException($"이미 {typeof(TExtend).Name} 타입의 Extend가 추가되어 있습니다.", nameof(extend));
            }
        }



        /// <summary>
        ///  <typeparamref name="TExtend"/> 확장 모듈을 얻어본다
        /// </summary>
        /// <typeparam name="TExtend"></typeparam>
        /// <param name="extend"></param>
        /// <returns></returns>
        public bool TryGetExtend<TExtend>(out TExtend extend) where TExtend : Extend
        {
            if (Extends != null && Extends.TryGetValue(typeof(TExtend), out var foundExtend))
            {
                extend = (TExtend)foundExtend;
                return true;
            }
            extend = null;
            return false;
        }



        /// <summary>
        /// <typeparamref name="TExtend"/> 확장 모듈을 얻어온다 
        /// </summary>
        /// <typeparam name="TExtend"></typeparam>
        /// <returns></returns>
        public TExtend GetExtend<TExtend>() where TExtend : Extend
        {
            return Extends[typeof(TExtend)] as TExtend;
        }



        ///======================================================================================================================================================



        [Serializable]
        public abstract class Extend { }



        [Serializable]
        public class TurnViewManagementExtend : Extend
        {
            public TurnViewManagementExtend(Dictionary<ETurnViews8, TurnViewManagement> turnViewManagementDictionary)
            {
                TurnViewManagementDictionary = turnViewManagementDictionary;
            }



            /// <summary>
            /// 턴뷰의 <see cref="TurnViewManagement"/> 들을 보관하는 딕셔너리
            /// </summary>
            [ShowInInspector, ReadOnly]
            protected readonly Dictionary<ETurnViews8, TurnViewManagement> TurnViewManagementDictionary;



            /// <summary>
            /// <paramref name="turnViewDirection"/> 에 해당하는 턴뷰의 <see cref="TurnViewManagement"/>을 반환한다
            /// </summary>
            /// <param name="turnViewDirection"></param>
            /// <returns></returns>
            public TurnViewManagement GetTurnViewManagement(ETurnViews8 turnViewDirection)
            {
                return TurnViewManagementDictionary[turnViewDirection];
            }
        }



        [Serializable]
        public class HeadExtend : Extend
        {
            public HeadExtend(Dictionary<ETurnViews8, SkelAni> turnViewMainHead_SkelAniDictionary, Dictionary<ETurnViews8, SkelAni> turnViewPlusHead_SkelAniDictionary)
            {
                TurnViewMainHead_SkelAniDictionary = turnViewMainHead_SkelAniDictionary;
                TurnViewPlusHead_SkelAniDictionary = turnViewPlusHead_SkelAniDictionary;
            }



            /// <summary>
            /// <b>턴뮤 메인 헤드 트랙</b> 의 <see cref="SkelAni"/> 들을 보관하는 딕셔너리
            /// </summary>
            [ShowInInspector, ReadOnly]
            protected readonly Dictionary<ETurnViews8, SkelAni> TurnViewMainHead_SkelAniDictionary;

            /// <summary>
            /// <b>턴뷰 플러스 헤드 트랙</b> 의 <see cref="SkelAni"/> 들을 보관하는 딕셔너리 
            /// </summary>
            [ShowInInspector, ReadOnly]
            protected readonly Dictionary<ETurnViews8, SkelAni> TurnViewPlusHead_SkelAniDictionary;



            /// <summary>
            ///  <paramref name="turnViewDirection"/> 에 해당하는 <b>턴뷰 메인 헤드 트랙</b>의 <see cref="SkelAni"/>을 반환한다 
            /// </summary>
            /// <param name="turnViewDirection"></param>
            /// <returns></returns>
            public SkelAni GetTurnViewMainHeadSkelAni(ETurnViews8 turnViewDirection)
            {
                return TurnViewMainHead_SkelAniDictionary[turnViewDirection];
            }

            /// <summary>
            ///  <paramref name="turnViewDirection"/> 에 해당하는 <b>턴뷰 플러스 헤드 트랙</b>의 <see cref="SkelAni"/>을 반환한다 
            /// </summary>
            /// <param name="turnViewDirection"></param>
            /// <returns></returns>
            public SkelAni GetTurnViewPlusHeadSkelAni(ETurnViews8 turnViewDirection)
            {
                return TurnViewPlusHead_SkelAniDictionary[turnViewDirection];
            }

        }



        ///======================================================================================================================================================
    }



    public abstract class TurnView8Manager<TSkelSbject> : TurnView8Manager where TSkelSbject : SkelSbject, new()
    {
        ///======================================================================================================================================================



        public TurnView8Manager(TSkelSbject skelSbject,
            Dictionary<ETurnViews8, Spine.Animation> turnViewSpineAnimationDictionary,
            Dictionary<ETurnViews8, SkelAni> turnViewMain_SkelAniDictionary,
            Dictionary<ETurnViews8, SkelAni> turnViewPlus_SkelAniDictionary
            )
        {
            SkelSbject = skelSbject;
            TurnViewMainTrackIndex = GetTurnViewMainTrackIndex();
            TurnViewPlusTrackIndex = GetTurnViewPlusTrackIndex();
            //. SkelSbject 할당이 끝난 후에 해야함


            TurnViewSpineAnimationDictionary = turnViewSpineAnimationDictionary;
            TurnViewMain_SkelAniDictionary = turnViewMain_SkelAniDictionary;
            TurnViewPlus_SkelAniDictionary = turnViewPlus_SkelAniDictionary;
        }



        ///======================================================================================================================================================



        protected readonly TSkelSbject SkelSbject;



        /// <summary>
        /// 턴뷰 메인 트랙 인덱스 
        /// </summary>
        public readonly int TurnViewMainTrackIndex;

        /// <summary>
        /// 턴뷰 플러스 트랙 인덱스 
        /// </summary>
        public readonly int TurnViewPlusTrackIndex;



        ///======================================================================================================================================================



        #region 턴뷰 Spine.Animation

        ///<summary>
        /// 턴뷰의 <see cref="Spine.Animation"/> 들을 보관하는 딕셔너리
        /// </summary>
        [ShowInInspector, ReadOnly]
        protected readonly Dictionary<ETurnViews8, Spine.Animation> TurnViewSpineAnimationDictionary;



        /// <summary>
        ///  <paramref name="turnViewDirection"/> 에 해당하는 턴뷰의 <see cref="Spine.Animation"/>을 반환한다 
        /// </summary>
        /// <param name="turnViewDirection"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Spine.Animation GetTurnViewSpineAnimation(ETurnViews8 turnViewDirection)
        {
            if (TurnViewSpineAnimationDictionary.TryGetValue(turnViewDirection, out var animation)) { return animation; }
            throw new Exception($"{turnViewDirection} 턴뷰에 해당하는 Spine.Animation을 찾을 수 없음");
        }

        #endregion



        ///======================================================================================================================================================



        /// <summary>
        /// <b>턴뷰 메인 트랙</b>의 <see cref="SkelAni"/> 들을 보관하는 딕셔너리
        /// </summary>
        [ShowInInspector, ReadOnly]
        protected readonly Dictionary<ETurnViews8, SkelAni> TurnViewMain_SkelAniDictionary;

        /// <summary>
        /// <b>턴뷰 플러스 트랙</b>의 <see cref="SkelAni"/> 들을 보관하는 딕셔너리 
        /// </summary>
        [ShowInInspector, ReadOnly]
        protected readonly Dictionary<ETurnViews8, SkelAni> TurnViewPlus_SkelAniDictionary;



        /// <summary>
        ///  <paramref name="turnViewDirection"/> 에 해당하는 <b>턴뷰 메인 트랙</b>의 <see cref="SkelAni"/>을 반환한다 
        /// </summary>
        /// <param name="turnViewDirection"></param>
        /// <returns></returns>
        public SkelAni GetTurnViewMainSkelAni(ETurnViews8 turnViewDirection)
        {
            return TurnViewMain_SkelAniDictionary[turnViewDirection];
        }

        /// <summary>
        ///  <paramref name="turnViewDirection"/> 에 해당하는 <b>턴뷰 플러스 트랙</b>의 <see cref="SkelAni"/>을 반환한다 
        /// </summary>
        /// <param name="turnViewDirection"></param>
        /// <returns></returns>
        public SkelAni GetTurnViewPlusSkelAni(ETurnViews8 turnViewDirection)
        {
            return TurnViewPlus_SkelAniDictionary[turnViewDirection];
        }




        /// <summary>
        /// 턴뷰 메인 트랙 인덱스를 반환한다 (생성자에서 1회 사용되어 <see cref="TurnViewMainTrackIndex"/>에 할당된다)
        /// </summary>
        /// <returns></returns>
        protected abstract int GetTurnViewMainTrackIndex();

        /// <summary>
        /// 턴뷰 플러스 트랙 인덱스를 반환한다 (생성자에서 1회 사용되어 <see cref="TurnViewPlusTrackIndex"/>에 할당된다)
        /// </summary>
        /// <returns></returns>
        protected abstract int GetTurnViewPlusTrackIndex();



        //? 재생중인 턴뷰 방향 얻기 시도



        /// <summary>
        ///  <paramref name="turnViewTrackIndex"/> 에서 재생중인 턴뷰 모션의 방향을 반환한다<br/>
        ///  트랙이 재생중이지 않거나, 재생중인 모션이 턴뷰 모션이 아닐경우 실패한다
        /// </summary>
        /// <param name="skelObject"></param>
        /// <param name="turnViewTrackIndex"></param>
        /// <param name="turnViewSkelAniDictionary"></param>
        /// <param name="turnViewDirection"></param>
        /// <returns></returns>
        protected static bool TryGet_TurnViewSomethingTrack_Direction(SkelObject skelObject, int turnViewTrackIndex, Dictionary<ETurnViews8, SkelAni> turnViewSkelAniDictionary, out ETurnViews8? turnViewDirection)
        {
            if (skelObject.Ani.TryGetPlayingTrack(turnViewTrackIndex, out var playingTrack) && playingTrack.IsEnable)
            {
                var playingSkelAni = playingTrack.PlayingSkelAni_Reference;
                foreach (var item in turnViewSkelAniDictionary)
                {
                    if (item.Value == playingSkelAni)
                    {
                        turnViewDirection = item.Key;
                        return true;
                    }
                }
            }
            turnViewDirection = null;
            return false;
        }



        /// <summary>
        ///  <see cref="AniTrack.TurnViewMain"/> 에서 재생중인 턴뷰 모션의 방향을 반환한다<br/> 
        ///  트랙이 재생중이지 않거나, 재생중인 모션이 턴뷰 모션이 아닐경우 실패한다
        /// </summary>
        /// <param name="skelObject"></param>
        /// <param name="turnViewDirection"></param>
        /// <returns></returns>
        public bool TryGet_TurnViewMain_Direction(SkelObject skelObject, out ETurnViews8? turnViewDirection)
        {
            return TryGet_TurnViewSomethingTrack_Direction(skelObject, GetTurnViewMainTrackIndex(), TurnViewMain_SkelAniDictionary, out turnViewDirection);
        }

        /// <summary>
        /// <see cref="AniTrack.TurnViewPlus"/> 에서 재생중인 턴뷰 모션의 방향을 반환한다<br/>
        /// 트랙이 재생중이지 않거나, 재생중인 모션이 턴뷰 모션이 아닐경우 실패한다
        /// </summary>
        /// <param name="skelObject"></param>
        /// <param name="turnViewDirection"></param>
        /// <returns></returns>
        public bool TryGet_TurnViewPlus_Direction(SkelObject skelObject, out ETurnViews8? turnViewDirection)
        {
            return TryGet_TurnViewSomethingTrack_Direction(skelObject, GetTurnViewPlusTrackIndex(), TurnViewPlus_SkelAniDictionary, out turnViewDirection);
        }



        //? 턴뷰가 특정 방향으로 재생중인지 확인



        /// <summary>
        ///  <see cref="AniTrack.TurnViewMain"/> 에서 <paramref name="turnViewDirection"/> 에 해당하는 턴뷰 모션이 재생중인지 확인한다
        /// </summary>
        /// <param name="skelObject"></param>
        /// <param name="turnViewDirection"></param>
        /// <returns></returns>
        public bool Check_TurnViewMain_IsPlaying(SkelObject skelObject, ETurnViews8 turnViewDirection)
        {
            return (TryGet_TurnViewMain_Direction(skelObject, out var currentDirection) && currentDirection.Value == turnViewDirection);
        }

        /// <summary>
        ///  <see cref="AniTrack.TurnViewPlus"/> 에서 <paramref name="turnViewDirection"/> 에 해당하는 턴뷰 모션이 재생중인지 확인한다
        /// </summary>
        /// <param name="skelObject"></param>
        /// <param name="turnViewDirection"></param>
        /// <returns></returns>
        public bool Check_TurnViewPlus_IsPlaying(SkelObject skelObject, ETurnViews8 turnViewDirection)
        {
            return (TryGet_TurnViewPlus_Direction(skelObject, out var currentDirection) && currentDirection.Value == turnViewDirection);
        }



        /// <summary>
        ///  <see cref="AniTrack.TurnViewMain"/> 또는 <see cref="AniTrack.TurnViewPlus"/> 에서 <paramref name="turnViewDirection"/> 에 해당하는 턴뷰 모션이 재생중인지 확인한다
        /// </summary>
        /// <param name="skelObject"></param>
        /// <param name="turnViewDirection"></param>
        /// <returns></returns>
        public bool Check_TurnViewTracks_IsPlaying(SkelObject skelObject, ETurnViews8 turnViewDirection)
        {
            return Check_TurnViewMain_IsPlaying(skelObject, turnViewDirection) || Check_TurnViewPlus_IsPlaying(skelObject, turnViewDirection);
        }

        /// <summary>
        ///  <see cref="AniTrack.TurnViewMain"/> 에서 <paramref name="turnViewRule"/> 규칙에 해당하는 턴뷰 모션이 재생중인지 확인한다 
        /// </summary>
        /// <param name="skelObject"></param>
        /// <param name="turnViewRule"></param>
        /// <returns></returns>
        public bool Check_TurnViewMain_IsPlayingRule(SkelObject skelObject, ETurnViewRule turnViewRule)
        {
            if (TryGet_TurnViewMain_Direction(skelObject, out var currentDirection)) { return currentDirection.Value.Is(turnViewRule); }
            return false;
        }



        /// <summary>
        ///  <see cref="AniTrack.TurnViewPlus"/> 에서 <paramref name="turnViewRule"/> 규칙에 해당하는 턴뷰 모션이 재생중인지 확인한다 
        /// </summary>
        /// <param name="skelObject"></param>
        /// <param name="turnViewRule"></param>
        /// <returns></returns>
        public bool Check_TurnViewPlus_IsPlayingRule(SkelObject skelObject, ETurnViewRule turnViewRule)
        {
            if (TryGet_TurnViewPlus_Direction(skelObject, out var currentDirection)) { return currentDirection.Value.Is(turnViewRule); }
            return false;
        }

        /// <summary>
        /// 
        /// 
        ///  <see cref="AniTrack.TurnViewMain"/> 또는 <see cref="AniTrack.TurnViewPlus"/> 에서 <paramref name="turnViewRule"/> 규칙에 해당하는 턴뷰 모션이 재생중인지 확인한다 
        /// </summary>
        /// <param name="skelObject"></param>
        /// <param name="turnViewRule"></param>
        /// <returns></returns>
        public bool Check_TurnViewTracks_IsPlayingRule(SkelObject skelObject, ETurnViewRule turnViewRule)
        {
            return Check_TurnViewMain_IsPlayingRule(skelObject, turnViewRule) || Check_TurnViewPlus_IsPlayingRule(skelObject, turnViewRule);
        }



        ///======================================================================================================================================================
    }
}
