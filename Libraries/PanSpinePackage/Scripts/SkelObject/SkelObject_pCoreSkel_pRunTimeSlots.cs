using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine;
using Spine.Unity;
using System.Linq;
using System;
using Pan.Util;
using Pan.Util.IOB;
using Pan.SpinePackage;
using Pan.Util.Game;
using System.Text;
using Pan.SpineUtil;



namespace Pan.SpinePackage
{
    public partial class SkelObject
    {
        public partial class SkelCore : CoreBase
        {
            /// <summary>
            /// 런타임에서 생성되는 <see cref="Slot"/>들을 캐싱된 <see cref="SlotData"/>와 함께 관리하는
            /// <para>런타임 슬롯 매니저</para>
            /// </summary>
            [Serializable]
            public class RunTimeSlotManager : Base
            {
                ///======================================================================================================================================================



                public RunTimeSlotManager(SkelObject skelObject, SkelSbject skelSbject, int tagSlotCapacity, int customDrawOrderExecutes_MaxBucketInitial, int customDrawOrderExecutes_Indexes_InitialCapacity) : base(skelObject)
                {
                    SkelSbject = skelSbject;

                    //. 태그슬롯 초기화
                    TagSlots = new Dictionary<string, List<Slot>>(tagSlotCapacity);


                    //. 커스텀 드로우 오더 버킷 초기화
                    CustomDrawOrderExecutes_Buckets = new List<BaseCustomDrawOrderCommand>[customDrawOrderExecutes_MaxBucketInitial + 1];


                    //. 커스텀 드로우 오더의 모든 버킷들에게 List 초기화
                    for (int i = 0; i < CustomDrawOrderExecutes_Buckets.Length; i++)
                    {
                        CustomDrawOrderExecutes_Buckets[i] = new List<BaseCustomDrawOrderCommand>();
                    }


                    //. 커스텀 드로우 오더 인덱스 딕셔너리 초기화
                    CustomDrawOrderExecutes_Indexes = new(customDrawOrderExecutes_Indexes_InitialCapacity);


                    PostUpdateEvent_ApplyCustomDrawOder = () => ApplyCustomDrawOrders();
                }



                ///======================================================================================================================================================



                //? 커스텀 드로우 오더 커맨드 패턴 클래스



                /// <summary>
                /// 커스텀 드로우 오더 커맨드 패턴의 베이스
                /// </summary>
                public abstract class BaseCustomDrawOrderCommand
                {
                    public BaseCustomDrawOrderCommand(string eventName, string targetSlotName)
                    {
                        EventName = eventName;
                        TargetSlotName = targetSlotName;
                    }


                    /// <summary>
                    /// 이벤트 이름
                    /// </summary>
                    public readonly string EventName;


                    /// <summary>
                    /// 움직일 대상 슬롯의 이름
                    /// </summary>
                    public readonly string TargetSlotName;



                    /// <summary>
                    /// 이 커맨드의 업데이트 메서드 실행
                    /// </summary>
                    /// <param name="manager"></param>
                    public abstract void ExecuteUpdate(RunTimeSlotManager manager);
                }



                /// <summary>
                /// 커스텀 드로우 오더 커맨드
                /// <para>대상 슬롯(<see cref="TargetSlotName"/>)의 DrawOrderIndex를</para>
                /// <para><see cref="TargetDrawOrderIndex"/> 로 이동</para>
                /// </summary>
                public class CustomDrawOrderCommand_DrawOrderIndex : BaseCustomDrawOrderCommand
                {
                    /// <param name="targetSlotName">움직일 대상 슬롯의 이름</param>
                    /// <param name="targetDrawOrder">대상 슬롯이 이 DrawOrderIndex에 배치</param>
                    public CustomDrawOrderCommand_DrawOrderIndex(string eventName, string targetSlotName, int targetDrawOrder) : base(eventName, targetSlotName)
                    {
                        TargetDrawOrderIndex = targetDrawOrder;
                    }



                    /// <summary>
                    /// 대상 슬롯이 이 DrawOrderIndex에 배치
                    /// </summary>
                    public readonly int TargetDrawOrderIndex;



                    /// <inheritdoc/>
                    public override void ExecuteUpdate(RunTimeSlotManager manager)
                    {
                        manager.ApplyCustomDrawOrder_DrawOrderIndex(TargetSlotName, TargetDrawOrderIndex);
                    }
                }



                /// <summary>
                /// 커스텀 드로우 오더 커맨드
                /// <para>대상 슬롯(<see cref="TargetSlotName"/>)의 DrawOrderIndex를</para>
                /// <para>기준 슬롯(<see cref="StandardSlotName"/>)의 DrawOrderIndex를 기준으로,</para>
                /// <para><see cref="StandardOffset"/>만큼 더한 값으로 이동</para>
                /// </summary>
                public class CustomDrawOrderCommand_PutOn : BaseCustomDrawOrderCommand
                {
                    /// <param name="targetSlotName">움직일 대상 슬롯의 이름</param>
                    /// <param name="standardSlotName">대상 슬롯이 이 슬롯을 기준으로 배치</param>
                    /// <param name="standardOffset">대상 슬롯의 DrawOrderIndex가 이 기준 슬롯의 DrawOrderIndex의 이 값을 더한 값 만큼 이동</param>
                    public CustomDrawOrderCommand_PutOn(string eventName, string targetSlotName, string standardSlotName, int standardOffset) : base(eventName, targetSlotName)
                    {
                        StandardSlotName = standardSlotName;
                        StandardOffset = standardOffset;
                    }



                    /// <summary>
                    /// 대상 슬롯이 이 슬롯을 기준으로 배치
                    /// </summary>
                    public readonly string StandardSlotName;



                    /// <summary>
                    /// 대상 슬롯의 DrawOrderIndex가 이 기준 슬롯의 DrawOrderIndex의 이 값을 더한 값 만큼 이동
                    /// </summary>
                    public readonly int StandardOffset;



                    /// <inheritdoc/>
                    public override void ExecuteUpdate(RunTimeSlotManager manager)
                    {
                        manager.ApplyCustomDrawOrder_PutOn(TargetSlotName, StandardOffset, StandardSlotName);
                    }
                }



                /// <summary>
                /// 커스텀 드로우 오더 커맨드
                /// <para>대상 슬롯(<see cref="TargetSlotName"/>)의 DrawOrderIndex를</para>
                /// <para>기준 슬롯들(<see cref="StandardSlotNames"/>) 중에서, 가장 높거나 낮은 (<see cref="IsHigher_StandardSlots"/>)DrawOrderIndex를 기준으로,</para>
                /// <para><see cref="StandardOffset"/>만큼 더한 값으로 이동</para>
                /// </summary>
                public class CustomDrawOrderCommand_PutOnMultiple : BaseCustomDrawOrderCommand
                {
                    /// <param name="targetSlotName">움직일 대상 슬롯의 이름</param>
                    /// <param name="standardSlotNames">대상 슬롯이 이 슬롯들을 기준으로 배치</param>
                    /// <param name="isHigher_StandardSlots">기준 슬롯(들) 중에서, 가장 높은 DrawOrderIndex가 기준이 될지 여부</param>
                    /// <param name="standardOffset">대상 슬롯의 DrawOrderIndex가 이 기준 슬롯의 DrawOrderIndex의 이 값을 더한 값 만큼 이동</param>
                    public CustomDrawOrderCommand_PutOnMultiple(string eventName, string targetSlotName, string[] standardSlotNames, bool isHigher_StandardSlots, int standardOffset) : base(eventName, targetSlotName)
                    {
                        StandardSlotNames = standardSlotNames;
                        IsHigher_StandardSlots = isHigher_StandardSlots;
                        StandardOffset = standardOffset;
                    }



                    /// <summary>
                    /// 대상 슬롯이 이 슬롯들을 기준으로 배치
                    /// </summary>
                    public readonly string[] StandardSlotNames;



                    /// <summary>
                    /// 기준 슬롯(들) 중에서, 가장 높은 DrawOrderIndex가 기준이 될지 여부
                    /// </summary>
                    public readonly bool IsHigher_StandardSlots;



                    /// <summary>
                    /// 대상 슬롯의 DrawOrderIndex가 이 기준 슬롯의 DrawOrderIndex의 이 값을 더한 값 만큼 이동
                    /// </summary>
                    public readonly int StandardOffset;



                    /// <inheritdoc/>
                    public override void ExecuteUpdate(RunTimeSlotManager manager)
                    {
                        manager.ApplyCustomDrawOrder_PutOnMultiple(TargetSlotName, StandardOffset, IsHigher_StandardSlots, StandardSlotNames);
                    }
                }



                /// <summary>
                /// 커스텀 드로우 오더 커맨드
                /// <para>대상 슬롯(<see cref="TargetSlotName"/>)의 DrawOrderIndex를</para>
                /// <para>기준 슬롯 태그 이름(<see cref="StandardSlotName"/>)이 속해있는 슬롯(들)을 기준으로,</para>
                /// <para>그 슬롯(들) 중에서 DrawOrderIndex가 가장 낮거나 높은 슬롯을 기준으로 (<see cref="IsHigher_StandardSlots"/>)</para>
                /// <para><see cref="StandardOffset"/>만큼 더한 값으로 이동</para>
                /// </summary>
                public class CustomDrawOrderCommand_PutOnSlotTag : BaseCustomDrawOrderCommand
                {
                    /// <param name="targetSlotName">움직일 대상 슬롯의 이름</param>
                    /// <param name="standardSlotTag">대상 슬롯이 이 슬롯태그를 가지고있는 슬롯(들)을 기준으로 배치</param>
                    /// <param name="isHigher_StandardSlots">기준 슬롯(들) 중에서, 가장 높은 DrawOrderIndex가 기준이 될지 여부</param>
                    /// <param name="standardOffset">대상 슬롯의 DrawOrderIndex가 정해진 기준 슬롯을 기준으로, 이 슬롯의 DrawOrderIndex의 이 값을 더한 값 만큼 이동</param>
                    public CustomDrawOrderCommand_PutOnSlotTag(string eventName, string targetSlotName, string standardSlotTag, bool isHigher_StandardSlots, int standardOffset) : base(eventName, targetSlotName)
                    {
                        StandardSlotTag = standardSlotTag;
                        IsHigher_StandardSlots = isHigher_StandardSlots;
                        StandardOffset = standardOffset;
                    }



                    /// <summary>
                    /// 대상 슬롯이 이 슬롯태그를 가지고있는 슬롯(들)을 기준으로 배치
                    /// </summary>
                    public readonly string StandardSlotTag;



                    /// <summary>
                    /// 기준 슬롯(들) 중에서, 가장 높은 DrawOrderIndex가 기준이 될지 여부
                    /// </summary>
                    public readonly bool IsHigher_StandardSlots;



                    /// <summary>
                    /// 대상 슬롯의 DrawOrderIndex가 정해진 기준 슬롯을 기준으로, 이 슬롯의 DrawOrderIndex의 이 값을 더한 값 만큼 이동
                    /// </summary>
                    public readonly int StandardOffset;



                    /// <inheritdoc/>
                    public override void ExecuteUpdate(RunTimeSlotManager manager)
                    {
                        manager.ApplyCustomDrawOrderCommand_PutOnSlotTag(TargetSlotName, StandardOffset, IsHigher_StandardSlots, StandardSlotTag);
                    }
                }



                ///======================================================================================================================================================



                private readonly SkelSbject SkelSbject;



                private Skeleton Skeleton => SkelCore.Skeleton;



                /// <summary>
                /// <see cref="Spine.Skeleton"/>의 <see cref="Slot"/>들 얻기 (런타임)
                /// </summary>
                public Slot[] GetSlots => Skeleton.Slots.Items;



                /// <summary>
                /// <see cref="Spine.Skeleton"/>의 DrawOrder에 의해 정렬된 <see cref="Slot"/>들 얻기 (런타임)
                /// </summary>
                public Slot[] GetDrawOrderSlots => Skeleton.DrawOrder.Pose.Items;

                /// <summary>
                /// <see cref="Spine.Skeleton"/>의 DrawOrder에 의해 정렬된 <see cref="Slot"/>들의 유효 개수 얻기 (런타임)
                /// </summary>
                public int GetDrawOrderSlotCount => Skeleton.DrawOrder.Pose.Count;



                ///======================================================================================================================================================



                //? 태그 슬롯, 슬롯들의 집합을 문자열 string 태그로 한번에 묶어, 호출할수있게 정리



                /// <summary>
                /// <see cref="Slot"/>의 리스트를 문자열 Tag로 관리하는 딕셔너리
                /// </summary>
                private readonly Dictionary<string, List<Slot>> TagSlots;



                /// <summary>
                /// <see cref="Slot"/>의 리스트를 문자열 Tag로 관리하는 딕셔너리 얻기
                /// </summary>
                public IReadOnlyDictionary<string, List<Slot>> GetTagSlots => TagSlots;



                ///======================================================================================================================================================



                //? 커스텀 드로우 오더 



                ///<summary>
                /// 커스텀 드로우 오더가 들어있는 버킷 리스트
                /// <para>배열의 Index를 순서로사용하여 구현 (때문에 최소값 0)</para>
                /// <para>배열의 Length를 넘는 값을 접근하려고 할때, <see cref="TryExpand_CustomDrawOrderExecutes_Buckets"/>가 호출되어 유연하게 확장</para>
                /// </summary>
                private List<BaseCustomDrawOrderCommand>[] CustomDrawOrderExecutes_Buckets;



                ///<summary>
                /// 커스텀 드로우 오더가 들어있는 버킷 리스트 얻기
                /// </summary>
                public IReadOnlyList<IReadOnlyList<BaseCustomDrawOrderCommand>> GetCustomDrawOrderExecutes_Buckets => CustomDrawOrderExecutes_Buckets;



                /// <summary>
                /// 커스텀 드로우 오더를 빠르게 접근하게 해주는 인덱스 딕셔너리
                /// </summary>
                private readonly Dictionary<string, (int priorityIndex, int indexInList)> CustomDrawOrderExecutes_Indexes;



                /// <summary>
                /// <see cref="Spine.Skeleton"/>의 업데이트 이후, 커스텀 드로우오더를 적용하기 위한 델리게이트
                /// </summary>
                public readonly Action PostUpdateEvent_ApplyCustomDrawOder;



                ///======================================================================================================================================================



                //. 커스텀 드로우 오더 버퍼



                public Slot[] slotbuffer;
                public byte[] slotMarksBufferbuffer;
                public int[] touchedIndicesBuffer;



                ///======================================================================================================================================================



                ///<summary>
                /// 태그Slot 추가
                /// </summary>
                public void AddTagSlot(string slotTag, IEnumerable<string> slotNames)
                {
                    TagSlots[slotTag] = slotNames.Select(x => GetSlots[SkelSbject.GetSlotData(x).Index]).ToList();
                }



                ///<summary>
                /// 태그Slot 제거
                /// </summary>
                public bool RemoveTagSlot(string slotTag)
                {
                    return TagSlots.Remove(slotTag);
                }



                ///<summary>
                /// 태그Slot 얻기
                /// </summary>
                public List<Slot> GetTagSlot(string slotTag)
                {
                    return TagSlots[slotTag];
                }



                ///<summary>
                /// 태그Slot 얻어보기
                /// </summary>
                public bool TryGetTagSlot(string slotTag, out List<Slot> result)
                {
                    return TagSlots.TryGetValue(slotTag, out result);
                }



                ///======================================================================================================================================================



                ///<summary>
                /// <see cref="SkelSbject"/>에 캐싱되어있는 <see cref="SlotData"/>를 기반으로,
                /// <para> 런타임 <see cref="Skeleton"/>의 <see cref="Slot"/>을 빠르게 얻어 반환한다</para>
                /// </summary>
                public Slot GetSlot(string slotName)
                {
                    return Skeleton.Slots.Items[SkelSbject.GetSlotData(slotName).Index];
                }



                /// <summary>
                /// 여러 SlotName들을 받아, 캐싱된 SlotData를 통해 해당하는 실제 Slot들을 배열로 반환한다.
                /// </summary>
                public Slot[] GetSlots_Cached(params string[] slotNames)
                {
                    //. 반환할 슬롯 배열
                    var result = new Slot[slotNames.Length];

                    for (int i = 0; i < slotNames.Length; i++)
                    {
                        result[i] = Skeleton.Slots.Items[SkelSbject.GetSlotData(slotNames[i]).Index];
                    }

                    return result;
                }



                ///<summary>
                /// <see cref="SkelSbject"/>에 캐싱되어있는 <see cref="SlotData"/>를 기반으로,
                /// <para> 런타임 <see cref="Skeleton"/>의 <see cref="Slot"/>을 빠르게 얻어 반환한다</para>
                /// </summary>
                public bool TryGetSlot(string slotName, out Slot resultSlot)
                {
                    if (SkelSbject.TryGetSlotData(slotName, out var slotData))
                    {
                        resultSlot = Skeleton.Slots.Items[slotData.Index];
                        return true;
                    }

                    resultSlot = null;
                    return false;
                }



                ///======================================================================================================================================================



                //? 기본 커스텀 드로우 오더 적용 메서드



                public bool ApplyCustomDrawOrders()
                {
                    bool isApplied = false;

                    for (int i = 0; i < CustomDrawOrderExecutes_Buckets.Length; i++)
                    {
                        if (CustomDrawOrderExecutes_Buckets[i].Count == 0) { continue; }

                        for (int j = 0; j < CustomDrawOrderExecutes_Buckets[i].Count; j++)
                        {
                            CustomDrawOrderExecutes_Buckets[i][j].ExecuteUpdate(this);
                            isApplied = true;
                        }
                    }

                    return isApplied;
                }



                /// <summary>
                /// 대상 <see cref="Slot"/>의 DrawOrder 이동
                /// <para><b>이 메서드가 <see cref="Spine.Skeleton"/>의 Update가 끝난 뒤 실행되어야, 적용된다.</b></para>
                /// <para><b>그렇지 않으면 <see cref="Spine.Skeleton"/> 자체의 DrawOrderIndex에 묻혀 적용이 되지 않는다</b></para>
                /// </summary>
                /// <param name="targetSlotName">옮길 대상 슬롯의 이름</param>
                /// <param name="targetDrawOrderIndex">대상슬롯이 이 DrawOrderIndex에 배치</param>
                public void ApplyCustomDrawOrder_DrawOrderIndex(string targetSlotName, int targetDrawOrderIndex)
                {
                    Skeleton.ChangeSlotDrawOrder(GetSlot(targetSlotName), targetDrawOrderIndex);
                }



                /// <summary>
                /// 대상 <see cref="Slot"/>의 DrawOrderIndex를 특정 <see cref="Slot"/>의 DrawOrderIndex에 <paramref name="standardOffset"/> 를 더한 값으로 이동
                /// <para><b>이 메서드가 <see cref="Spine.Skeleton"/>의 Update가 끝난 뒤 실행되어야, 적용된다.</b></para>
                /// <para><b>그렇지 않으면 <see cref="Spine.Skeleton"/> 자체의 DrawOrderIndex에 묻혀 적용이 되지 않는다</b></para>
                /// </summary>
                /// <param name="targetSlotName">옮길 대상 슬롯의 이름</param>
                /// <param name="standardOffset">대상 슬롯의 DrawOrderIndex가 기준 슬롯의 DrawOrderIndex의 이 값을 더한 값 만큼 이동</param>
                /// <param name="standardSlotName">기준이 되는 슬롯 이름</param>
                public void ApplyCustomDrawOrder_PutOn(string targetSlotName, int standardOffset, string standardSlotName)
                {
                    Skeleton.ChangeSlotDrawOrder_PutOn(GetSlot(targetSlotName), GetSlot(standardSlotName), standardOffset);
                }



                /// <summary>
                /// 대상 <see cref="Slot"/>의 DrawOrderIndex를 특정 <see cref="Slot"/>들의 DrawOrderIndex에 <paramref name="standardOffset"/> 를 더한 값으로 이동
                /// <para><b>이 메서드가 <see cref="Spine.Skeleton"/>의 Update가 끝난 뒤 실행되어야, 적용된다.</b></para>
                /// <para><b>그렇지 않으면 <see cref="Spine.Skeleton"/> 자체의 DrawOrderIndex에 묻혀 적용이 되지 않는다</b></para>
                /// </summary>
                /// <param name="targetSlotName">옮길 대상 슬롯의 이름</param>
                /// <param name="standardOffset">대상 슬롯의 DrawOrderIndex가 기준 슬롯의 DrawOrderIndex의 이 값을 더한 값 만큼 이동</param>
                /// <param name="isHigher_StandardSlots">기준 슬롯들 중에서, 가장 높은 DrawOrderIndex가 기준 슬롯이 될지 여부</param>
                /// <param name="standardSlotNames">기준이 되는 슬롯들의 이름들</param>
                public void ApplyCustomDrawOrder_PutOnMultiple(string targetSlotName, int standardOffset, bool isHigher_StandardSlots, params string[] standardSlotNames)
                {
                    Skeleton.ChangeSlotDrawOrder_PutOnExtend(GetSlot(targetSlotName), GetSlots_Cached(standardSlotNames), isHigher_StandardSlots, standardOffset);
                }



                /// <summary>
                /// 대상 <see cref="Slot"/>의 DrawOrderIndex를 받아온 SlotTag에 해당하는 <see cref="Slot"/>(들)의 DrawOrderIndex에 <paramref name="standardOffset"/> 를 더한 값으로 이동
                /// <para><b>이 메서드가 <see cref="Spine.Skeleton"/>의 Update가 끝난 뒤 실행되어야, 적용된다.</b></para>
                /// <para><b>그렇지 않으면 <see cref="Spine.Skeleton"/> 자체의 DrawOrderIndex에 묻혀 적용이 되지 않는다</b></para>
                /// </summary>
                /// <param name="targetSlotName">옮길 대상 슬롯의 이름</param>
                /// <param name="standardOffset">대상 슬롯의 DrawOrderIndex가 기준 슬롯의 DrawOrderIndex의 이 값을 더한 값 만큼 이동</param>
                /// <param name="isHigher_StandardSlots">기준 슬롯들 중에서, 가장 높은 DrawOrderIndex가 기준 슬롯이 될지 여부</param>
                /// <param name="standardSlotTag">기준이 되는 슬롯(들)의 슬롯태그</param>
                public void ApplyCustomDrawOrderCommand_PutOnSlotTag(string targetSlotName, int standardOffset, bool isHigher_StandardSlots, string standardSlotTag)
                {
                    Skeleton.ChangeSlotDrawOrder_PutOnExtend(GetSlot(targetSlotName), GetTagSlot(standardSlotTag), isHigher_StandardSlots, standardOffset);
                }



                /// <summary>
                /// <see cref="Skeleton"/>의 DrawOrder에서 <paramref name="targetSlots"/> 그룹을 블록으로 묶어,
                /// <paramref name="standardSlots"/> 그룹의 "최상단(가장 앞)" 바로 위(앞)에 배치합니다.
                /// <para>그룹 내부 순서는 "현재 DrawOrder에 존재하던 순서"를 유지합니다.</para>
                /// </summary>
                /// <param name="currentSkeleton">
                /// 대상 스켈레톤입니다.
                /// </param>
                /// <param name="targetSlots">
                /// 이동할 슬롯 그룹입니다. 모든 슬롯은 <paramref name="currentSkeleton"/> 소속이며 DrawOrder 내에 존재해야 합니다.
                /// </param>
                /// <param name="standardSlots">
                /// 기준 슬롯 그룹입니다. 모든 슬롯은 <paramref name="currentSkeleton"/> 소속이며 DrawOrder 내에 존재해야 합니다.
                /// </param>
                /// <param name="blockBuffer">
                /// 타겟 슬롯 블록을 임시로 담는 버퍼입니다. null이거나 부족하면 내부에서 확보됩니다.
                /// </param>
                /// <param name="slotMarks">
                /// 슬롯 멤버십 마킹 버퍼입니다(0=없음, 1=타겟, 2=기준). null이거나 부족하면 내부에서 확보됩니다.
                /// </param>
                /// <param name="touchedIndices">
                /// slotMarks를 건드린 dataIndex 기록용 버퍼입니다. null이거나 부족하면 내부에서 확보됩니다.
                /// </param>
                public void ApplyCustomDrawOrderCommand_PutGroupAbove(string targetSlotTag, string standardSlotTag)
                {
                    Skeleton.ChangeSlotDrawOrder_PutGroupAbove(GetTagSlot(targetSlotTag), GetTagSlot(standardSlotTag), ref slotbuffer, ref slotMarksBufferbuffer, ref touchedIndicesBuffer);
                }



                /// <summary>
                /// <see cref="Skeleton"/>의 DrawOrder에서 <paramref name="targetSlots"/> 그룹을 블록으로 묶어,
                /// <paramref name="standardSlots"/> 그룹의 "최상단(가장 앞)" 바로 위(앞)에 배치합니다.
                /// <para>그룹 내부 순서는 "현재 DrawOrder에 존재하던 순서"를 유지합니다.</para>
                /// </summary>
                /// <param name="currentSkeleton">
                /// 대상 스켈레톤입니다.
                /// </param>
                /// <param name="targetSlots">
                /// 이동할 슬롯 그룹입니다. 모든 슬롯은 <paramref name="currentSkeleton"/> 소속이며 DrawOrder 내에 존재해야 합니다.
                /// </param>
                /// <param name="standardSlots">
                /// 기준 슬롯 그룹입니다. 모든 슬롯은 <paramref name="currentSkeleton"/> 소속이며 DrawOrder 내에 존재해야 합니다.
                /// </param>
                /// <param name="blockBuffer">
                /// 타겟 슬롯 블록을 임시로 담는 버퍼입니다. null이거나 부족하면 내부에서 확보됩니다.
                /// </param>
                /// <param name="slotMarks">
                /// 슬롯 멤버십 마킹 버퍼입니다(0=없음, 1=타겟, 2=기준). null이거나 부족하면 내부에서 확보됩니다.
                /// </param>
                /// <param name="touchedIndices">
                /// slotMarks를 건드린 dataIndex 기록용 버퍼입니다. null이거나 부족하면 내부에서 확보됩니다.
                /// </param>
                public void ApplyCustomDrawOrderCommand_PutGroupBelow(string targetSlotTag, string standardSlotTag)
                {
                    Skeleton.ChangeSlotDrawOrder_PutGroupBelow(GetTagSlot(targetSlotTag), GetTagSlot(standardSlotTag), ref slotbuffer, ref slotMarksBufferbuffer, ref touchedIndicesBuffer);
                }



                public void ApplyCustomDrawOrderCommand_PutGroups(string targetSlotTag, string standardSlotTag, bool isHigher_StandardSlots)
                {
                    if (isHigher_StandardSlots)
                    {
                        ApplyCustomDrawOrderCommand_PutGroupBelow(targetSlotTag, standardSlotTag);
                    }
                    else
                    {
                        ApplyCustomDrawOrderCommand_PutGroupAbove(targetSlotTag, standardSlotTag);
                    }
                }



                ///======================================================================================================================================================



                //? 커스텀 드로우 오더 실행 메서드



                /// <summary>
                /// 대상 <see cref="Slot"/>의 DrawOrder 이동
                /// <para><b>이 메서드는 <see cref="Spine.Skeleton"/>의 Update가 끝난 뒤 실행되며, <paramref name="customDrawOrder_Order"/>의 올림차순으로 실행된다</b></para>
                /// </summary>
                /// <typeparam name="TCommand"></typeparam>
                /// <param name="customDrawOrderEventName">커스텀 드로우 오더 이벤트 이름</param>
                /// <param name="customDrawOrder_Order">커스텀 드로우 오더 이벤트 순서, 최소값 0, 작은순으로 실행</param>
                /// <param name="command">대상 커맨드 클래스</param>
                public void ExecuteSetCustomDrawOrder<TCommand>(int customDrawOrder_Order, TCommand command) where TCommand : BaseCustomDrawOrderCommand
                {
                    //? 사전작업
                    if (!WhenBefore_SetCustomDrawOrderCommand(command.EventName, customDrawOrder_Order)) { return; }

                    WhenAfter_SetCustomDrawOrderCommand(command.EventName, customDrawOrder_Order, command);
                }



                /// <summary>
                /// 대상 <see cref="Slot"/>의 DrawOrder 이동
                /// <para><b>이 메서드는 <see cref="Spine.Skeleton"/>의 Update가 끝난 뒤 실행되며, <paramref name="customDrawOrder_Order"/>의 올림차순으로 실행된다</b></para>
                /// </summary>
                /// <param name="customDrawOrderEventName">커스텀 드로우 오더 이벤트 이름</param>
                /// <param name="customDrawOrder_Order">커스텀 드로우 오더 이벤트 순서, 최소값 0, 작은순으로 실행</param>
                /// <param name="targetSlotName">옮길 대상 슬롯의 이름</param>
                /// <param name="targetDrawOrderIndex">대상슬롯이 이 DrawOrderIndex에 배치</param>
                public void ExecuteSetCustomDrawOrder_DrawOrderIndex(string customDrawOrderEventName, int customDrawOrder_Order, string targetSlotName, int targetDrawOrderIndex)
                {
                    ExecuteSetCustomDrawOrder(customDrawOrder_Order, new CustomDrawOrderCommand_DrawOrderIndex(customDrawOrderEventName, targetSlotName, targetDrawOrderIndex));
                }



                /// <summary>
                /// 대상 <see cref="Slot"/>의 DrawOrderIndex를 특정 <see cref="Slot"/>의 DrawOrderIndex에 <paramref name="standardOffset"/> 를 더한 값으로 이동
                /// <para><b>이 메서드는 <see cref="Spine.Skeleton"/>의 Update가 끝난 뒤 실행되며, <paramref name="customDrawOrder_Order"/>의 올림차순으로 실행된다</b></para>
                /// </summary>
                /// <param name="customDrawOrderEventName">커스텀 드로우 오더 이벤트 이름</param>
                /// <param name="customDrawOrder_Order">커스텀 드로우 오더 이벤트 순서, 최소값 0, 작은순으로 실행</param>
                /// <param name="targetSlotName">옮길 대상 슬롯의 이름</param>
                /// <param name="standardOffset">대상 슬롯의 DrawOrderIndex가 기준 슬롯의 DrawOrderIndex의 이 값을 더한 값 만큼 이동</param>
                /// <param name="standardSlotName">기준이 되는 슬롯 이름</param>
                public void ExecuteSetCustomDrawOrder_PutOn(string customDrawOrderEventName, int customDrawOrder_Order, string targetSlotName, string standardSlotName, int standardOffset)
                {
                    ExecuteSetCustomDrawOrder(customDrawOrder_Order, new CustomDrawOrderCommand_PutOn(customDrawOrderEventName, targetSlotName, standardSlotName, standardOffset));
                }



                /// <summary>
                /// 대상 <see cref="Slot"/>의 DrawOrderIndex를 특정 <see cref="Slot"/>들의 DrawOrderIndex에 <paramref name="standardOffset"/> 를 더한 값으로 이동
                /// <para><b>이 메서드는 <see cref="Spine.Skeleton"/>의 Update가 끝난 뒤 실행되며, <paramref name="customDrawOrder_Order"/>의 올림차순으로 실행된다</b></para>
                /// </summary>
                /// <param name="customDrawOrderEventName">커스텀 드로우 오더 이벤트 이름</param>
                /// <param name="customDrawOrder_Order">커스텀 드로우 오더 이벤트 순서, 최소값 0, 작은순으로 실행</param>
                /// <param name="targetSlotName">옮길 대상 슬롯의 이름</param>
                /// <param name="standardOffset">대상 슬롯의 DrawOrderIndex가 기준 슬롯의 DrawOrderIndex의 이 값을 더한 값 만큼 이동</param>
                /// <param name="isHigher_StandardSlots">기준 슬롯들 중에서, 가장 높은 DrawOrderIndex가 기준 슬롯이 될지 여부</param>
                /// <param name="standardSlotNames">기준이 되는 슬롯들의 이름들</param>
                public void SetCustomDrawOrder_PutOnMultiple(string customDrawOrderEventName, int customDrawOrder_Order, string targetSlotName, int standardOffset, bool isHigher_StandardSlots, params string[] standardSlotNames)
                {
                    ExecuteSetCustomDrawOrder(customDrawOrder_Order, new CustomDrawOrderCommand_PutOnMultiple(customDrawOrderEventName, targetSlotName, standardSlotNames, isHigher_StandardSlots, standardOffset));
                }



                /// <summary>
                /// 대상 <see cref="Slot"/>의 DrawOrderIndex를 받아온 SlotTag에 해당하는 <see cref="Slot"/>(들)의 DrawOrderIndex에 <paramref name="standardOffset"/> 를 더한 값으로 이동
                /// <para><b>이 메서드는 <see cref="Spine.Skeleton"/>의 Update가 끝난 뒤 실행되며, <paramref name="customDrawOrder_Order"/>의 올림차순으로 실행된다</b></para>
                /// </summary>
                /// <param name="customDrawOrderEventName">커스텀 드로우 오더 이벤트 이름</param>
                /// <param name="customDrawOrder_Order">커스텀 드로우 오더 이벤트 순서, 최소값 0, 작은순으로 실행</param>
                /// <param name="targetSlotName">옮길 대상 슬롯의 이름</param>
                /// <param name="standardOffset">대상 슬롯의 DrawOrderIndex가 기준 슬롯의 DrawOrderIndex의 이 값을 더한 값 만큼 이동</param>
                /// <param name="isHigher_StandardSlots">기준 슬롯들 중에서, 가장 높은 DrawOrderIndex가 기준 슬롯이 될지 여부</param>
                /// <param name="standardSlotTag">기준이 되는 슬롯(들)의 슬롯태그</param>
                public void SetCustomDrawOrderCommand_PutOnSlotTag(string customDrawOrderEventName, int customDrawOrder_Order, string targetSlotName, int standardOffset, bool isHigher_StandardSlots, string standardSlotTag)
                {
                    ExecuteSetCustomDrawOrder(customDrawOrder_Order, new CustomDrawOrderCommand_PutOnSlotTag(customDrawOrderEventName, targetSlotName, standardSlotTag, isHigher_StandardSlots, standardOffset));
                }



                //? 커스텀 드로우 오더 메서드 제거 메서드



                /// <summary>
                /// 커스텀 드로우 오더 제거
                /// </summary>
                /// <param name="customDrawOrderEventName"></param>
                public void RemoveCustomDrawOrderCommand(string customDrawOrderEventName)
                {
                    //! 해당 이벤트 이름이 없을경우, 실패
                    if (!CustomDrawOrderExecutes_Indexes.TryGetValue(customDrawOrderEventName, out var value)) { return; }

                    var targetList = CustomDrawOrderExecutes_Buckets[value.priorityIndex];

                    //. Swap-Remove로 제거 최적화
                    int last = targetList.Count - 1;
                    if (value.indexInList < last)
                    {
                        // swap
                        targetList[value.indexInList] = targetList[last];
                        // 그리고 Dictionary에서 “eventName → indexInList” 업데이트
                    }
                    targetList.RemoveAt(last);

                    //CustomDrawOrderExecutes_Buckets[value.priorityIndex].RemoveAt(value.indexInList);

                    CustomDrawOrderExecutes_Indexes.Remove(customDrawOrderEventName);
                }



                //? 커스텀 드로우 오더 메서드 관련 유틸리티 메서드



                /// <summary>
                /// 특정 슬롯을 대상으로, 커스텀 드로우 오더가 동작중인지 확인하고,
                /// <para>작동중이라면, true를 반환함과 동시에 그 슬롯이 속한 <see cref="BaseCustomDrawOrderCommand"/>들의 목록을 얻어온다</para>
                /// </summary>
                /// <param name="targetSlotName"></param>
                /// <param name="resultDrawOrderCommands"></param>
                public bool CustomDrawOrderFindAndGet_Commands(string targetSlotName, ICollection<BaseCustomDrawOrderCommand> resultDrawOrderCommands)
                {
                    bool result = false;

                    for (int i = 0; i < CustomDrawOrderExecutes_Buckets.Length; i++)
                    {
                        for (int j = 0; j < CustomDrawOrderExecutes_Buckets[i].Count; j++)
                        {
                            if (CustomDrawOrderExecutes_Buckets[i][j].TargetSlotName == targetSlotName)
                            {
                                resultDrawOrderCommands.Add(CustomDrawOrderExecutes_Buckets[i][j]);
                                result = true;
                            }
                        }
                    }

                    return result;
                }



                //? 커스텀 드로우 오더 메서드 관련 내부 메서드



                /// <summary>
                /// 커스텀 드로우 오더 를 실행할때 검사
                /// <para>이미 해당 이벤트 이름이 실행중인경우 실패하며,</para>
                /// <para>성공시 순서 크기 제약(0)을 수행 한 후, 버킷의 범위보다 크다면 확장 작업 수행</para>
                /// </summary>
                /// <param name="customDrawOrderEventName"></param>
                /// <param name="customDrawOrder_Order"></param>
                /// <param name=""></param>
                /// <returns></returns>
                private bool WhenBefore_SetCustomDrawOrderCommand(string customDrawOrderEventName, int customDrawOrder_Order)
                {
                    //! 이미 해당 이벤트 이름이 실행중일경우, 실패
                    if (CustomDrawOrderExecutes_Indexes.ContainsKey(customDrawOrderEventName)) { return false; }

                    //. 순서 크기 제약 & 버킷의 범위보다 크다면 확장 작업 수행
                    customDrawOrder_Order.SetClamp0();
                    TryExpand_CustomDrawOrderExecutes_Buckets(customDrawOrder_Order);

                    return true;
                }



                /// <summary>
                /// 커스텀 드로우 오더가 성공적으로 실행된 이후 실행
                /// </summary>
                /// <param name="customDrawOrderEventName"></param>
                /// <param name="customDrawOrder_Order"></param>
                private void WhenAfter_SetCustomDrawOrderCommand(string customDrawOrderEventName, int customDrawOrder_Order, BaseCustomDrawOrderCommand command)
                {
                    CustomDrawOrderExecutes_Buckets[customDrawOrder_Order].Add(command);
                    CustomDrawOrderExecutes_Indexes.Add(customDrawOrderEventName, (customDrawOrder_Order, CustomDrawOrderExecutes_Buckets[customDrawOrder_Order].Count - 1));
                }



                ///<summary>
                /// 범위를 벗어난 유효하지 않은 버킷 인덱스일경우, 확장 및 초기화 작업을 하는 메서드
                /// </summary>
                private bool TryExpand_CustomDrawOrderExecutes_Buckets(int requiredIndex)
                {
                    if (requiredIndex < CustomDrawOrderExecutes_Buckets.Length)
                        return false;


                    int oldSize = CustomDrawOrderExecutes_Buckets.Length;
                    int newSize = requiredIndex + 1;

                    //. Array.Resize 로 배열 길이 늘리기
                    Array.Resize(ref CustomDrawOrderExecutes_Buckets, newSize);

                    //. 새로 늘어난 부분 초기화
                    for (int i = oldSize; i < newSize; i++)
                    {
                        CustomDrawOrderExecutes_Buckets[i] = new List<BaseCustomDrawOrderCommand>();
                    }

                    return true;
                }



                ///======================================================================================================================================================



                //? 리셋



                ///<summary>
                /// 런타임 슬롯 매니저 초기화
                /// </summary>
                public void Reset()
                {
                    TagSlots.Clear();

                    for (int i = 0; i < CustomDrawOrderExecutes_Buckets.Length; i++)
                    {
                        CustomDrawOrderExecutes_Buckets[i].Clear();
                    }

                    CustomDrawOrderExecutes_Indexes.Clear();
                }



                ///======================================================================================================================================================
            }



        }
    }
}
