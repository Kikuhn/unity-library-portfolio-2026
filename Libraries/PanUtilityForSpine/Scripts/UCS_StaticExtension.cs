using Spine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System;



//? 스파인 전역 확장 메서드가 정리되어있는 정도의 코드



namespace Pan.SpineUtil
{
    public static class SpineExtension
    {
        ///======================================================================================================================================================



        //? Skin 정보 조회 확장



        /// <summary>
        /// 지정된 <see cref="Skin"/>이 필수적으로 포함해야 하는 요소(뼈대 또는 제약 조건)를 가지고 있는지 확인합니다.
        /// </summary>
        /// <param name="skin">검사할 <see cref="Skin"/> 객체</param>
        /// <returns>
        /// <c>true</c>이면 <paramref name="skin"/>이 하나 이상의 뼈대(Bones) 또는 제약 조건(Constraints)을 포함하고 있음을 의미합니다.
        /// 그렇지 않으면 <c>false</c>를 반환합니다.
        /// </returns>
        public static bool HasRequiredElements(this Skin skin)
        {
            return (skin.Bones != null && skin.Bones.Count != 0) || (skin.Constraints != null && skin.Constraints.Count != 0);
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 해당 <see cref="Spine.Animation"/>에서 특정 이벤트가 있는지 검사하기 (최적화 버전)
        /// </summary>
        public static bool HasEventFromAnimation(this Spine.Animation animation, string eventName)
        {
            int timelineCount = animation.Timelines.Count; // 리스트 길이 캐싱 (성능 최적화)

            for (int i = 0; i < timelineCount; i++)
            {
                if (animation.Timelines.Items[i] is EventTimeline eventTimeline) // 직접 인덱스 접근 (성능 최적화)
                {
                    int eventCount = eventTimeline.Events.Length;

                    for (int j = 0; j < eventCount; j++)
                    {
                        //! 이벤트 발견 즉시 true 반환 (최대 성능)
                        if (eventTimeline.Events[j].Data.Name == eventName)
                            return true;
                    }
                }
            }

            //! 이벤트가 없으면 false 반환
            return false;
        }



        /// <summary>
        /// 해당 <see cref="Spine.Animation"/>에서 존재하는 모든 이벤트를 딕셔너리로 반환
        /// <para>key: 이벤트 이름, value: 그 이벤트가 호출된 횟수</para>
        /// </summary>
        public static void GetEventDictionaryFromAnimation(this Spine.Animation animation, out Dictionary<string, int> resultDictionary)
        {
            int timelineCount = animation.Timelines.Count;
            resultDictionary = new Dictionary<string, int>();

            for (int i = 0; i < timelineCount; i++)
            {
                if (animation.Timelines.Items[i] is EventTimeline eventTimeline)
                {
                    int eventCount = eventTimeline.Events.Length;

                    for (int j = 0; j < eventCount; j++)
                    {
                        var currentEvent = eventTimeline.Events[j];
                        string eventName = currentEvent.Data.Name; // 이벤트의 고유 이름 사용
                        if (resultDictionary.ContainsKey(eventName))
                        {
                            resultDictionary[eventName]++;
                        }
                        else
                        {
                            resultDictionary.Add(eventName, 1);
                        }
                    }
                }
            }

            resultDictionary.TrimExcess();
        }



        /// <summary>
        /// 해당 <see cref="Spine.Animation"/>에서 지정한 타입의 타임라인(<typeparamref name="TTimeline"/>)을 찾아 반환합니다.
        /// </summary>
        /// <typeparam name="TTimeline">
        /// 찾고자 하는 타임라인 타입입니다.
        /// </typeparam>
        /// <param name="animation">
        /// 대상 애니메이션입니다.
        /// </param>
        /// <returns>
        /// 해당 타입의 타임라인이 존재한다면 그 인스턴스를 반환합니다.
        /// 존재하지 않으면 null 을 반환합니다.
        /// </returns>
        public static TTimeline GetTimeLineFromAnimation<TTimeline>(this Spine.Animation animation) where TTimeline : Timeline
        {
            //. 타임라인 리스트의 총 개수
            int timelineCount = animation.Timelines.Count;

            for (int i = 0; i < timelineCount; i++)
            {
                //? Spine 런타임의 ExposedList<T>는 Items 배열을 직접 접근하는 편이 일반적으로 오버헤드가 적습니다.
                if (animation.Timelines.Items[i] is TTimeline timeLine)
                    return timeLine;
            }

            //! 못 찾으면 null
            return null;
        }



        /// <summary>
        /// 해당 <see cref="Spine.Animation"/>에서 지정한 타입의 타임라인(<typeparamref name="TTimeline"/>)을 찾고,
        /// 존재 여부에 따라 결과를 반환합니다.
        /// </summary>
        /// <typeparam name="TTimeline">
        /// 찾고자 하는 타임라인 타입입니다.
        /// </typeparam>
        /// <param name="animation">
        /// 대상 애니메이션입니다.
        /// </param>
        /// <param name="timeLine">
        /// 성공 시: 찾은 타임라인 인스턴스가 할당됩니다.
        /// 실패 시: null 이 할당됩니다.
        /// </param>
        /// <returns>
        /// 타임라인을 찾았다면 true, 찾지 못했다면 false 를 반환합니다.
        /// </returns>
        public static bool TryGetTimeLineFromAnimation<TTimeline>(this Spine.Animation animation, out TTimeline timeLine) where TTimeline : Timeline
        {
            //. out 초기화
            timeLine = null;

            //. 타임라인 리스트의 총 개수
            int timelineCount = animation.Timelines.Count;

            for (int i = 0; i < timelineCount; i++)
            {
                //? 첫 매칭을 찾는 즉시 반환 (최대 성능)
                if (animation.Timelines.Items[i] is TTimeline found)
                {
                    timeLine = found;
                    return true;
                }
            }

            //! 못 찾으면 false (timeLine은 null)
            return false;
        }



        public static ExposedList<int> GetBoneIndexesFromTimelines(this ExposedList<Timeline> timelines)
        {
            ExposedList<int> result = new();
            HashSet<int> addedBoneIndexes = new();

            int timelineCount = timelines.Count;
            for (int i = 0; i < timelineCount; i++)
            {
                if (timelines.Items[i] is IBoneTimeline boneTimeline && addedBoneIndexes.Add(boneTimeline.BoneIndex))
                {
                    result.Add(boneTimeline.BoneIndex);
                }
            }

            result.TrimExcess();
            return result;
        }


        public static void SetTimelinesWithCollectedBones(this Spine.Animation animation, ExposedList<Timeline> timelines)
        {
            animation.SetTimelines(timelines, timelines.GetBoneIndexesFromTimelines());
        }



        /// <summary>
        /// <see cref="DrawOrderTimeline"/>을 완전히 Deep Copy 합니다.
        /// <para>
        /// Frames, DrawOrders(int[][]) 모두 새 인스턴스로 복제되며,
        /// 원본 Timeline과의 참조 공유는 발생하지 않습니다.
        /// </para>
        /// </summary>
        public static DrawOrderTimeline DeepCopy(this DrawOrderTimeline source)
        {
            //! source가 null이면 복제 불가
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            int frameCount = source.FrameCount;

            //. 새 DrawOrderTimeline 생성
            var copy = new DrawOrderTimeline(frameCount);

            //? Frames 복사 (시간)
            var srcFrames = source.Frames;
            var dstFrames = copy.Frames;

            for (int i = 0; i < frameCount; i++)
            {
                dstFrames[i] = srcFrames[i];
            }

            //? DrawOrders Deep Copy (setter 없으므로 내부 배열에 직접 채움)
            var srcOrders = source.DrawOrders;
            var dstOrders = copy.DrawOrders;

            for (int i = 0; i < frameCount; i++)
            {
                var order = srcOrders[i];

                if (order == null)
                {
                    //! null 은 기본 draw order 의미 → 그대로 유지
                    dstOrders[i] = null;
                }
                else
                {
                    //. 슬롯 인덱스 배열 Deep Copy
                    int len = order.Length;
                    var copied = new int[len];
                    Array.Copy(order, copied, len);
                    dstOrders[i] = copied;
                }
            }

            return copy;
        }


        /// <summary>
        /// <see cref="DrawOrderFolderTimeline"/>을 완전히 Deep Copy 합니다.
        /// <para>Spine 4.3의 folder draw order timeline은 생성 시 skeleton slot count가 필요합니다.</para>
        /// </summary>
        public static DrawOrderFolderTimeline DeepCopy(this DrawOrderFolderTimeline source, int slotCount)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            int[] sourceSlots = source.Slots;
            if (sourceSlots == null)
                throw new ArgumentException("DrawOrderFolderTimeline Slots is null.", nameof(source));

            int[] copiedSlots = new int[sourceSlots.Length];
            for (int i = 0; i < sourceSlots.Length; i++)
            {
                int slotIndex = sourceSlots[i];
                if (slotIndex < 0 || slotIndex >= slotCount)
                    throw new ArgumentOutOfRangeException(nameof(slotCount), slotCount, $"Slot index {slotIndex} is outside slotCount.");

                copiedSlots[i] = slotIndex;
            }

            int frameCount = source.FrameCount;
            var copy = new DrawOrderFolderTimeline(frameCount, copiedSlots, slotCount);

            var srcFrames = source.Frames;
            var srcOrders = source.DrawOrders;
            for (int i = 0; i < frameCount; i++)
            {
                int[] sourceOrder = srcOrders[i];
                int[] copiedOrder = null;
                if (sourceOrder != null)
                {
                    copiedOrder = new int[sourceOrder.Length];
                    Array.Copy(sourceOrder, copiedOrder, sourceOrder.Length);
                }

                copy.SetFrame(i, srcFrames[i], copiedOrder);
            }

            return copy;
        }


        public static DrawOrderFolderTimeline DeepCopy(this DrawOrderFolderTimeline source, SkeletonData skeletonData)
        {
            if (skeletonData == null)
                throw new ArgumentNullException(nameof(skeletonData));

            return source.DeepCopy(skeletonData.Slots.Count);
        }



        ///======================================================================================================================================================




        //? SetSkin 확장



        ///<summary>
        ///스킨 설정하기 (<b><i>Attachment만!</i></b>)<br/>
        ///(눈, 입, 표정 등등 단순히 Attachment 추가/교환만 이루어지는 스킨 전용)
        ///</summary>
        /// <param name="baseSkin">적용될 기존 스킨</param>
        /// <param name="willAddskin">추가할 스킨</param>
        public static void SetSkin_OnlyAttachment(this Skin baseSkin, Skin willAddskin)
        {
            foreach (var skinEntry in willAddskin.Attachments)
            {
                baseSkin.SetAttachment(skinEntry.SlotIndex, skinEntry.Placeholder, skinEntry.Attachment);
            }
        }



        ///<summary>
        ///스킨 되돌리기(<b><i>Attachment만!</i></b>)<br/>
        ///(최적화를 위해 바뀌었던 Attachment만 원본의 스킨으로 되돌린다)
        ///</summary>
        /// <param name="baseSkin">적용될 기존 스킨</param>
        /// <param name="originalSkin">바꿀 원본의 스킨</param>
        /// <param name="changedAttachments">바뀌었던 스킨의 Attachment들</param>
        public static void SetSkin_UndoAttachment(this Skin baseSkin, Skin originalSkin, Skin.SkinEntry[] changedAttachments)
        {
            for (int i = 0; i < changedAttachments.Length; i++)
            {
                Skin.SkinEntry skinEntry = changedAttachments[i];
                Attachment attachment = originalSkin.GetAttachment(skinEntry.SlotIndex, skinEntry.Placeholder);


                //! 기존 스킨에 해당 어태치먼트가 존재할경우, 기존 어태치먼트로 다시 덮어 씌운다
                if (attachment != null)
                {
                    baseSkin.SetAttachment(skinEntry.SlotIndex, skinEntry.Placeholder, attachment);
                }
                //! 기존 스킨에 해당 어태치먼트가 존재하지 않을경우, 해당 어태치먼트를 제거시킨다
                //.     (어태치먼트가 기존 스킨을 덮어씌운 형태가 아닌, 기존 스킨에 없었던 요소를 추가시켰을 경우 이 방법이 사용됨)
                else
                {
                    baseSkin.RemoveAttachment(skinEntry.SlotIndex, skinEntry.Placeholder);
                }
            }
        }

        ///<summary>
        ///스킨 되돌리기(<b><i>Attachment만!</i></b>)<br/>
        ///(최적화를 위해 바뀌었던 Attachment만 원본의 스킨으로 되돌린다)
        ///</summary>
        /// <param name="baseSkin">적용될 기존 스킨</param>
        /// <param name="originalSkin">바꿀 원본의 스킨</param>
        /// <param name="changedAttachments">바뀌었던 스킨의 Attachment들</param>
        public static void SetSkin_UndoAttachment(this Skin baseSkin, Skin originalSkin, IReadOnlyList<Skin.SkinEntry> changedAttachments)
        {
            for (int i = 0; i < changedAttachments.Count; i++)
            {
                Skin.SkinEntry skinEntry = changedAttachments[i];
                Attachment attachment = originalSkin.GetAttachment(skinEntry.SlotIndex, skinEntry.Placeholder);

                //! 기존 스킨에 해당 어태치먼트가 존재할경우, 기존 어태치먼트로 다시 덮어 씌운다
                if (attachment != null)
                {
                    baseSkin.SetAttachment(skinEntry.SlotIndex, skinEntry.Placeholder, attachment);
                }
                //! 기존 스킨에 해당 어태치먼트가 존재하지 않을경우, 해당 어태치먼트를 제거시킨다
                //.     (어태치먼트가 기존 스킨을 덮어씌운 형태가 아닌, 기존 스킨에 없었던 요소를 추가시켰을 경우 이 방법이 사용됨)
                else
                {
                    baseSkin.RemoveAttachment(skinEntry.SlotIndex, skinEntry.Placeholder);
                }
            }
        }

        ///<summary>
        ///스킨 되돌리기(<b><i>Attachment만!</i></b>)<br/>
        ///(최적화를 위해 바뀌었던 Attachment만 원본의 스킨으로 되돌린다)
        ///</summary>
        /// <param name="baseSkin">적용될 기존 스킨</param>
        /// <param name="originalSkin">바꿀 원본의 스킨</param>
        /// <param name="changedAttachments">바뀌었던 스킨의 Attachment들</param>
        public static void SetSkin_UndoAttachment(this Skin baseSkin, Skin originalSkin, ICollection<Skin.SkinEntry> changedAttachments)
        {
            foreach (Skin.SkinEntry skinEntry in changedAttachments)
            {
                Attachment attachment = originalSkin.GetAttachment(skinEntry.SlotIndex, skinEntry.Placeholder);

                //! 기존 스킨에 해당 어태치먼트가 존재할경우, 기존 어태치먼트로 다시 덮어 씌운다
                if (attachment != null)
                {
                    baseSkin.SetAttachment(skinEntry.SlotIndex, skinEntry.Placeholder, attachment);
                }
                //! 기존 스킨에 해당 어태치먼트가 존재하지 않을경우, 해당 어태치먼트를 제거시킨다
                //.     (어태치먼트가 기존 스킨을 덮어씌운 형태가 아닌, 기존 스킨에 없었던 요소를 추가시켰을 경우 이 방법이 사용됨)
                else
                {
                    baseSkin.RemoveAttachment(skinEntry.SlotIndex, skinEntry.Placeholder);
                }
            }
        }

        ///<summary>
        ///스킨 되돌리기(<b><i>Attachment만!</i></b>)<br/>
        ///(최적화를 위해 바뀌었던 Attachment만 원본의 스킨으로 되돌린다)
        ///</summary>
        /// <param name="baseSkin">적용될 기존 스킨</param>
        /// <param name="originalSkin">바꿀 원본의 스킨</param>
        /// <param name="changedSkin">바뀌었던 스킨 (Attachment을 여기서 추출함)</param>
        public static void SetSkin_UndoAttachment(this Skin baseSkin, Skin originalSkin, Skin changedSkin)
        {
            SetSkin_UndoAttachment(baseSkin, originalSkin, changedSkin.Attachments);
        }



        ///======================================================================================================================================================



        //? Slot의 DrawOrder 이동



        /// <summary>
        /// Slot[] 배열에서 <paramref name="targetSlot"/>의 DrawOrder를 변경합니다.
        /// </summary>
        /// <param name="currentSlots">순서를 변경할 대상이 되는 Slot[] 배열</param>
        /// <param name="targetSlot">옮길 슬롯</param>
        /// <param name="targetSlotIndex">현재 <paramref name="targetSlot"/>이 <paramref name="currentSlots"/> 내에 위치한 인덱스</param>
        /// <param name="targetDrawOrderIndex">옮길 새로운 DrawOrderIndex</param>
        public static void ChangeSlotDrawOrder(this Slot[] currentSlots, Slot targetSlot, int targetSlotIndex, int targetDrawOrderIndex)
        {
            ChangeSlotDrawOrder(currentSlots, currentSlots?.Length ?? 0, targetSlot, targetSlotIndex, targetDrawOrderIndex);
        }

        private static void ChangeSlotDrawOrder(Slot[] currentSlots, int currentSlotCount, Slot targetSlot, int targetSlotIndex, int targetDrawOrderIndex)
        {
            //! 파라미터 유효성 검사
            if (targetSlot == null || currentSlots == null || currentSlotCount == 0) return;
            currentSlotCount = Math.Min(currentSlotCount, currentSlots.Length);
            if (targetSlotIndex < 0 || targetSlotIndex >= currentSlotCount) return;

            //. 이동하려는 인덱스를 유효 범위 내로 보정
            if (targetDrawOrderIndex < 0)
            {
                targetDrawOrderIndex = 0;
            }
            else if (targetDrawOrderIndex >= currentSlotCount)
            {
                targetDrawOrderIndex = currentSlotCount - 1;
            }

            //. 이미 원하는 위치에 있다면 종료
            if (targetSlotIndex == targetDrawOrderIndex) return;

            //. 요소를 이동해야 하는 거리
            int distance = Math.Abs(targetDrawOrderIndex - targetSlotIndex);

            //. targetSlotIndex < targetDrawOrderIndex => 앞으로 당기는 경우
            //. targetSlotIndex > targetDrawOrderIndex => 뒤로 미는 경우
            if (targetSlotIndex < targetDrawOrderIndex)
            {
                //? targetSlotIndex+1부터 targetDrawOrderIndex까지를 왼쪽으로 한 칸씩 당김
                Array.Copy(currentSlots, targetSlotIndex + 1, currentSlots, targetSlotIndex, distance);
            }
            else
            {
                //? targetDrawOrderIndex부터 targetSlotIndex-1까지를 오른쪽으로 한 칸씩 미룸
                Array.Copy(currentSlots, targetDrawOrderIndex, currentSlots, targetDrawOrderIndex + 1, distance);
            }

            //. 최종 위치에 targetSlot 배치
            currentSlots[targetDrawOrderIndex] = targetSlot;
        }



        /// <summary>
        /// <see cref="Slot"/> 의 DrawOrder를 변경합니다.
        /// </summary>
        /// <param name="currentSkeleton">DrawOrder를 갖는 <see cref="Skeleton"/> 객체</param>
        /// <param name="targetSlot">옮길 슬롯</param>
        /// <param name="targetSlotIndex">현재 <paramref name="targetSlot"/>이 <paramref name="currentSkeleton"/>의 DrawOrder 내에 위치한 인덱스</param>
        /// <param name="targetDrawOrderIndex">옮길 새로운 DrawOrderIndex</param>
        public static void ChangeSlotDrawOrder(this Skeleton currentSkeleton, Slot targetSlot, int targetSlotIndex, int targetDrawOrderIndex)
        {
            //. 실제 작업은 Slot[] 확장 메서드를 이용
            ChangeSlotDrawOrder(currentSkeleton.DrawOrder.Pose.Items, currentSkeleton.DrawOrder.Pose.Count, targetSlot, targetSlotIndex, targetDrawOrderIndex);
        }

        /// <summary>
        /// <see cref="Slot"/> 의 DrawOrder를 변경합니다.
        /// </summary>
        /// <param name="currentSkeleton">DrawOrder를 갖는 <see cref="Skeleton"/> 객체</param>
        /// <param name="targetSlot">옮길 슬롯</param>
        /// <param name="targetDrawOrderIndex">옮길 새로운 DrawOrderIndex</param>
        public static void ChangeSlotDrawOrder(this Skeleton currentSkeleton, Slot targetSlot, int targetDrawOrderIndex)
        {
            //. targetSlotIndex를 currentSkeleton에서 조회 후 변경
            int targetSlotIndex = currentSkeleton.DrawOrder.Pose.IndexOf(targetSlot);
            ChangeSlotDrawOrder(currentSkeleton.DrawOrder.Pose.Items, currentSkeleton.DrawOrder.Pose.Count, targetSlot, targetSlotIndex, targetDrawOrderIndex);
        }



        /// <summary>
        /// <see cref="Slot"/> 의 DrawOrder를 변경합니다.
        /// </summary>
        /// <param name="currentSkeleton">DrawOrder를 갖는 <see cref="Skeleton"/> 객체</param>
        /// <param name="targetSlotName">옮길 슬롯의 이름</param>
        /// <param name="targetSlotIndex">현재 슬롯이 <paramref name="currentSkeleton"/>의 DrawOrder 내에 위치한 인덱스</param>
        /// <param name="targetDrawOrderIndex">옮길 새로운 DrawOrderIndex</param>
        public static void ChangeSlotDrawOrder(this Skeleton currentSkeleton, string targetSlotName, int targetSlotIndex, int targetDrawOrderIndex)
        {
            //. targetSlot 찾기
            Slot targetSlot = currentSkeleton.FindSlot(targetSlotName);
            ChangeSlotDrawOrder(currentSkeleton, targetSlot, targetSlotIndex, targetDrawOrderIndex);
        }

        /// <summary>
        /// <see cref="Slot"/> 의 DrawOrder를 변경합니다.
        /// </summary>
        /// <param name="currentSkeleton">DrawOrder를 갖는 <see cref="Skeleton"/> 객체</param>
        /// <param name="targetSlotName">옮길 슬롯의 이름</param>
        /// <param name="targetDrawOrderIndex">옮길 새로운 DrawOrderIndex</param>
        public static void ChangeSlotDrawOrder(this Skeleton currentSkeleton, string targetSlotName, int targetDrawOrderIndex)
        {
            //. targetSlot을 찾고, 현재 DrawOrder 상에서 인덱스를 구한 뒤 변경
            Slot targetSlot = currentSkeleton.FindSlot(targetSlotName);
            int targetSlotIndex = currentSkeleton.DrawOrder.Pose.IndexOf(targetSlot);
            ChangeSlotDrawOrder(currentSkeleton.DrawOrder.Pose.Items, currentSkeleton.DrawOrder.Pose.Count, targetSlot, targetSlotIndex, targetDrawOrderIndex);
        }



        ///======================================================================================================================================================



        /// <summary>
        /// Slot[] 배열에서 특정 Slot(<paramref name="targetSlot"/>)의 DrawOrder를,
        /// <paramref name="standardSlotIndex"/> + <paramref name="standardOffset"/> 위치로 옮깁니다.
        /// </summary>
        /// <param name="currentSlots">순서를 변경할 대상이 되는 Slot[] 배열</param>
        /// <param name="targetSlot">이동할 슬롯</param>
        /// <param name="targetSlotIndex">
        /// <paramref name="targetSlot"/>이 <paramref name="currentSlots"/> 내에서 현재 위치한 인덱스
        /// </param>
        /// <param name="standardSlot">DrawOrder의 기준이 되는 슬롯</param>
        /// <param name="standardSlotIndex">
        /// <paramref name="standardSlot"/>이 <paramref name="currentSlots"/> 내에서 위치한 인덱스
        /// </param>
        /// <param name="standardOffset">
        /// <paramref name="standardSlotIndex"/>에 더해질 오프셋. 이 값만큼 떨어진 위치로 <paramref name="targetSlot"/>을 이동합니다.
        /// </param>
        public static void ChangeSlotDrawOrder_PutOn(this Slot[] currentSlots, Slot targetSlot, int targetSlotIndex, Slot standardSlot, int standardSlotIndex, int standardOffset)
        {
            ChangeSlotDrawOrder_PutOn(currentSlots, currentSlots?.Length ?? 0, targetSlot, targetSlotIndex, standardSlot, standardSlotIndex, standardOffset);
        }

        private static void ChangeSlotDrawOrder_PutOn(Slot[] currentSlots, int currentSlotCount, Slot targetSlot, int targetSlotIndex, Slot standardSlot, int standardSlotIndex, int standardOffset)
        {
            //! 파라미터가 유효하지 않다면 작업을 수행하지 않고 종료
            if (currentSlots == null || currentSlotCount == 0) return;
            currentSlotCount = Math.Min(currentSlotCount, currentSlots.Length);
            if (standardSlot == null || targetSlot == null) return;

            //! index 범위를 벗어난 경우 작업을 수행하지 않고 종료
            if (standardSlotIndex < 0 || standardSlotIndex >= currentSlotCount ||
                targetSlotIndex < 0 || targetSlotIndex >= currentSlotCount) return;

            //. standardSlotIndex + standardOffset로 최종 이동 위치 계산
            int newIndex = standardSlotIndex + standardOffset;

            //. 이동하려는 인덱스를 범위 내로 보정
            if (newIndex < 0)
            {
                newIndex = 0;
            }
            else if (newIndex >= currentSlotCount)
            {
                newIndex = currentSlotCount - 1;
            }

            //. 이미 목표 위치에 있으면 종료
            if (targetSlotIndex == newIndex) return;

            //. 옮길 거리
            int distance = Math.Abs(newIndex - targetSlotIndex);

            //. targetSlotIndex < newIndex => 왼쪽으로 당기는 로직
            //. targetSlotIndex > newIndex => 오른쪽으로 미는 로직
            if (targetSlotIndex < newIndex)
            {
                //? targetSlotIndex+1부터 newIndex까지 왼쪽으로 한 칸씩 이동
                Array.Copy(currentSlots, targetSlotIndex + 1, currentSlots, targetSlotIndex, distance);
            }
            else
            {
                //? newIndex부터 targetSlotIndex-1까지 오른쪽으로 한 칸씩 이동
                Array.Copy(currentSlots, newIndex, currentSlots, newIndex + 1, distance);
            }

            //. 최종 위치에 targetSlot 배치
            currentSlots[newIndex] = targetSlot;
        }



        /// <summary>
        /// <see cref="Slot"/> 의 DrawOrder를 특정 <see cref="Slot"/>의 DrawOrderIndex에
        /// <paramref name="standardOffset"/> 만큼 더한 값으로 옮깁니다.
        /// </summary>
        /// <param name="currentSkeleton">DrawOrder를 갖는 <see cref="Skeleton"/> 객체</param>
        /// <param name="targetSlot">이동할 슬롯</param>
        /// <param name="targetSlotIndex">
        /// <paramref name="targetSlot"/>이 <paramref name="currentSkeleton"/>의 DrawOrder 내에서 위치한 인덱스
        /// </param>
        /// <param name="standardSlot">DrawOrder 기준 슬롯</param>
        /// <param name="standardSlotIndex">
        /// <paramref name="standardSlot"/>이 <paramref name="currentSkeleton"/>의 DrawOrder 내에서 위치한 인덱스
        /// </param>
        /// <param name="standardOffset">
        /// <paramref name="standardSlotIndex"/>에 더해질 오프셋. 이 값만큼 떨어진 위치로 <paramref name="targetSlot"/>을 이동합니다.
        /// </param>
        public static void ChangeSlotDrawOrder_PutOn(this Skeleton currentSkeleton, Slot targetSlot, int targetSlotIndex, Slot standardSlot, int standardSlotIndex, int standardOffset)
        {
            //. 실제 작업은 Slot[] 확장 메서드 사용
            ChangeSlotDrawOrder_PutOn(
                currentSkeleton.DrawOrder.Pose.Items,
                currentSkeleton.DrawOrder.Pose.Count,
                targetSlot,
                targetSlotIndex,
                standardSlot,
                standardSlotIndex,
                standardOffset
            );
        }

        /// <summary>
        /// <see cref="Slot"/> 의 DrawOrder를 특정 <see cref="Slot"/>의 DrawOrderIndex에
        /// <paramref name="standardOffset"/> 만큼 더한 값으로 옮깁니다.
        /// </summary>
        /// <param name="currentSkeleton">DrawOrder를 갖는 <see cref="Skeleton"/> 객체</param>
        /// <param name="targetSlot">이동할 슬롯</param>
        /// <param name="standardSlot">DrawOrder 기준 슬롯</param>
        /// <param name="standardOffset">
        /// <paramref name="standardSlot"/>의 인덱스에 더해질 오프셋.
        /// 이 값만큼 떨어진 위치로 <paramref name="targetSlot"/>을 이동합니다.
        /// </param>
        public static void ChangeSlotDrawOrder_PutOn(this Skeleton currentSkeleton, Slot targetSlot, Slot standardSlot, int standardOffset)
        {
            int targetSlotIndex = currentSkeleton.DrawOrder.Pose.IndexOf(targetSlot);
            int standardSlotIndex = currentSkeleton.DrawOrder.Pose.IndexOf(standardSlot);

            ChangeSlotDrawOrder_PutOn(
                currentSkeleton.DrawOrder.Pose.Items,
                currentSkeleton.DrawOrder.Pose.Count,
                targetSlot,
                targetSlotIndex,
                standardSlot,
                standardSlotIndex,
                standardOffset
            );
        }



        /// <summary>
        /// <see cref="Slot"/> 의 DrawOrder를 특정 <see cref="Slot"/>의 DrawOrderIndex에
        /// <paramref name="standardOffset"/> 만큼 더한 값으로 옮깁니다.
        /// </summary>
        /// <param name="currentSkeleton">DrawOrder를 갖는 <see cref="Skeleton"/> 객체</param>
        /// <param name="targetSlotName">이동할 슬롯의 이름</param>
        /// <param name="targetSlotIndex">
        /// <paramref name="targetSlotName"/>에 해당하는 슬롯이 DrawOrder에서 위치한 인덱스
        /// </param>
        /// <param name="standardSlotName">DrawOrder 기준 슬롯의 이름</param>
        /// <param name="standardSlotIndex">
        /// <paramref name="standardSlotName"/>에 해당하는 슬롯이 DrawOrder에서 위치한 인덱스
        /// </param>
        /// <param name="standardOffset">
        /// <paramref name="standardSlotIndex"/>에 더해질 오프셋. 이 값만큼 떨어진 위치로 옮깁니다.
        /// </param>
        public static void ChangeSlotDrawOrder_PutOn(this Skeleton currentSkeleton, string targetSlotName, int targetSlotIndex, string standardSlotName, int standardSlotIndex, int standardOffset)
        {
            var standardSlot = currentSkeleton.FindSlot(standardSlotName);
            var targetSlot = currentSkeleton.FindSlot(targetSlotName);

            ChangeSlotDrawOrder_PutOn(
                currentSkeleton.DrawOrder.Pose.Items,
                currentSkeleton.DrawOrder.Pose.Count,
                targetSlot,
                targetSlotIndex,
                standardSlot,
                standardSlotIndex,
                standardOffset
            );
        }

        /// <summary>
        /// <see cref="Slot"/> 의 DrawOrder를 특정 <see cref="Slot"/>의 DrawOrderIndex에
        /// <paramref name="standardOffset"/> 만큼 더한 값으로 옮깁니다.
        /// </summary>
        /// <param name="currentSkeleton">DrawOrder를 갖는 <see cref="Skeleton"/> 객체</param>
        /// <param name="targetSlotName">이동할 슬롯의 이름</param>
        /// <param name="standardSlotName">DrawOrder 기준 슬롯의 이름</param>
        /// <param name="standardOffset">
        /// <paramref name="standardSlotName"/>에 해당하는 슬롯 인덱스 + <paramref name="standardOffset"/> 위치로
        /// <paramref name="targetSlotName"/>을 이동합니다.
        /// </param>
        public static void ChangeSlotDrawOrder_PutOn(this Skeleton currentSkeleton, string targetSlotName, string standardSlotName, int standardOffset)
        {
            var standardSlot = currentSkeleton.FindSlot(standardSlotName);
            var targetSlot = currentSkeleton.FindSlot(targetSlotName);

            ChangeSlotDrawOrder_PutOn(
                currentSkeleton.DrawOrder.Pose.Items,
                currentSkeleton.DrawOrder.Pose.Count,
                targetSlot,
                currentSkeleton.DrawOrder.Pose.IndexOf(targetSlot),
                standardSlot,
                currentSkeleton.DrawOrder.Pose.IndexOf(standardSlot),
                standardOffset
            );
        }



        ///======================================================================================================================================================



        /// <summary>
        /// Slot[] 배열에서 <paramref name="targetSlot"/>의 DrawOrder를,
        /// <paramref name="standardSlots"/>에 속한 슬롯들의 DrawOrderIndex 중
        /// 가장 작은 혹은 가장 큰 값(<paramref name="isHigher_StandardSlots"/>),
        /// 그리고 <paramref name="standardOffset"/>만큼 떨어진 위치로 옮깁니다.
        /// </summary>
        /// <param name="currentSlots">DrawOrder를 변경할 Slot[] 배열</param>
        /// <param name="targetSlot">실제로 이동될 슬롯</param>
        /// <param name="targetSlotIndex">
        /// <paramref name="targetSlot"/>이 <paramref name="currentSlots"/>에서 현재 위치한 인덱스
        /// </param>
        /// <param name="standardSlots">
        /// 위치의 기준이 될 슬롯들의 집합.  
        /// 이들 중 유효한 인덱스 값들의 최소/최대를 구해 기준(anchor)을 삼습니다.
        /// </param>
        /// <param name="isHigher_StandardSlots">
        /// true면 <paramref name="standardSlots"/> 중 가장 큰 인덱스를,  
        /// false면 가장 작은 인덱스를 기준(anchor)으로 사용합니다.
        /// </param>
        /// <param name="standardOffset">
        /// 기준 인덱스에 더해 줄 오프셋. 예: 기준 인덱스가 5이고 offset이 3이면 결과는 8이 됩니다.
        /// </param>
        public static void ChangeSlotDrawOrder_PutOnExtend(this Slot[] currentSlots, Slot targetSlot, int targetSlotIndex, IList<Slot> standardSlots, bool isHigher_StandardSlots, int standardOffset)
        {
            ChangeSlotDrawOrder_PutOnExtend(currentSlots, currentSlots?.Length ?? 0, targetSlot, targetSlotIndex, standardSlots, isHigher_StandardSlots, standardOffset);
        }

        private static void ChangeSlotDrawOrder_PutOnExtend(Slot[] currentSlots, int currentSlotCount, Slot targetSlot, int targetSlotIndex, IList<Slot> standardSlots, bool isHigher_StandardSlots, int standardOffset)
        {
            //! 파라미터가 유효하지 않으면 종료
            if (currentSlots == null || currentSlotCount == 0) return;
            currentSlotCount = Math.Min(currentSlotCount, currentSlots.Length);
            if (targetSlot == null) return;
            if (standardSlots == null || standardSlots.Count == 0) return;

            //? standardSlots 내 슬롯들의 인덱스 중 최소/최대값을 찾기
            int minIndex = int.MaxValue;
            int maxIndex = int.MinValue;

            //. standardSlots를 순회하면서 currentSlots 내 인덱스를 검색
            for (int i = 0; i < standardSlots.Count; i++)
            {
                Slot baseSlot = standardSlots[i];
                if (baseSlot == null) continue;

                int idx = System.Array.IndexOf(currentSlots, baseSlot);
                if (idx >= 0 && idx < currentSlotCount)
                {
                    if (idx < minIndex) minIndex = idx;
                    if (idx > maxIndex) maxIndex = idx;
                }
            }

            //! 유효한 인덱스를 하나도 찾지 못했다면 종료
            if (minIndex == int.MaxValue) return;

            //. 기준 인덱스(anchorIndex) 결정
            int anchorIndex = isHigher_StandardSlots ? maxIndex : minIndex;

            //. anchorIndex + standardOffset가 최종 이동 위치
            int newIndex = anchorIndex + standardOffset;

            //. 범위를 벗어나지 않도록 보정
            if (newIndex < 0)
            {
                newIndex = 0;
            }
            else if (newIndex >= currentSlotCount)
            {
                newIndex = currentSlotCount - 1;
            }

            //! targetSlotIndex가 범위를 벗어나면 종료
            if (targetSlotIndex < 0 || targetSlotIndex >= currentSlotCount) return;

            //. 이미 원하는 위치면 종료
            if (targetSlotIndex == newIndex) return;

            //. Array.Copy를 사용해 targetSlot을 제거 후, newIndex 위치로 재배치
            int distance = System.Math.Abs(newIndex - targetSlotIndex);

            if (targetSlotIndex < newIndex)
            {
                //? targetSlotIndex+1부터 newIndex까지 왼쪽으로 당기는 로직
                System.Array.Copy(currentSlots, targetSlotIndex + 1, currentSlots, targetSlotIndex, distance);
            }
            else
            {
                //? newIndex부터 targetSlotIndex-1까지 오른쪽으로 미는 로직
                System.Array.Copy(currentSlots, newIndex, currentSlots, newIndex + 1, distance);
            }

            //. 최종 위치에 targetSlot 배치
            currentSlots[newIndex] = targetSlot;
        }



        /// <summary>
        /// <see cref="Slot"/>의 DrawOrder를 <paramref name="standardSlots"/> 중
        /// 가장 작은/큰 인덱스를 기준으로(<paramref name="isHigher_StandardSlots"/>),
        /// <paramref name="standardOffset"/> 만큼 떨어진 위치로 옮깁니다.
        /// </summary>
        /// <param name="currentSkeleton">DrawOrder를 갖는 <see cref="Skeleton"/> 객체</param>
        /// <param name="targetSlot">옮길 슬롯</param>
        /// <param name="targetSlotIndex">
        /// <paramref name="targetSlot"/>이 <paramref name="currentSkeleton"/>의 DrawOrder에서 위치한 인덱스
        /// </param>
        /// <param name="standardSlots">기준 인덱스를 찾을 슬롯들의 집합</param>
        /// <param name="isHigher_StandardSlots">true면 최대값, false면 최소값을 기준으로 삼습니다</param>
        /// <param name="standardOffset">
        /// 기준 인덱스에 더해 줄 오프셋. 음수이면 기준 인덱스보다 앞으로,
        /// 양수이면 뒤로 이동합니다.
        /// </param>
        public static void ChangeSlotDrawOrder_PutOnExtend(this Skeleton currentSkeleton, Slot targetSlot, int targetSlotIndex, IList<Slot> standardSlots, bool isHigher_StandardSlots, int standardOffset)
        {
            ChangeSlotDrawOrder_PutOnExtend(
                currentSkeleton.DrawOrder.Pose.Items,
                currentSkeleton.DrawOrder.Pose.Count,
                targetSlot,
                targetSlotIndex,
                standardSlots,
                isHigher_StandardSlots,
                standardOffset
            );
        }

        /// <summary>
        /// <see cref="Slot"/>의 DrawOrder를 <paramref name="standardSlots"/> 중
        /// 가장 작은/큰 인덱스를 기준으로(<paramref name="isHigher_StandardSlots"/>),
        /// <paramref name="standardOffset"/> 만큼 떨어진 위치로 옮깁니다.
        /// </summary>
        /// <param name="currentSkeleton">DrawOrder를 갖는 <see cref="Skeleton"/> 객체</param>
        /// <param name="targetSlot">옮길 슬롯</param>
        /// <param name="standardSlots">기준 인덱스를 찾을 슬롯들의 집합</param>
        /// <param name="isHigher_StandardSlots">true면 최대값, false면 최소값을 기준으로 삼습니다</param>
        /// <param name="standardOffset">
        /// 기준 인덱스에 더해 줄 오프셋. 음수이면 기준 인덱스보다 앞으로,
        /// 양수이면 뒤로 이동합니다.
        /// </param>
        public static void ChangeSlotDrawOrder_PutOnExtend(this Skeleton currentSkeleton, Slot targetSlot, IList<Slot> standardSlots, bool isHigher_StandardSlots, int standardOffset)
        {
            int slotIndex = currentSkeleton.DrawOrder.Pose.IndexOf(targetSlot);
            ChangeSlotDrawOrder_PutOnExtend(
                currentSkeleton.DrawOrder.Pose.Items,
                currentSkeleton.DrawOrder.Pose.Count,
                targetSlot,
                slotIndex,
                standardSlots,
                isHigher_StandardSlots,
                standardOffset
            );
        }



        /// <summary>
        /// <see cref="Slot"/>의 DrawOrder를 <paramref name="standardSlots"/> 중
        /// 가장 작은/큰 인덱스를 기준으로(<paramref name="isHigher_StandardSlots"/>),
        /// <paramref name="standardOffset"/> 만큼 떨어진 위치로 옮깁니다.
        /// <para>
        /// <see cref="params Slot[]"/>를 통해 여러 Slot을 편리하게 전달받습니다.
        /// </para>
        /// </summary>
        /// <param name="currentSkeleton">DrawOrder를 갖는 <see cref="Skeleton"/> 객체</param>
        /// <param name="targetSlot">옮길 슬롯</param>
        /// <param name="targetSlotIndex">
        /// <paramref name="targetSlot"/>이 <paramref name="currentSkeleton"/>의 DrawOrder에서 위치한 인덱스
        /// </param>
        /// <param name="isHigher_StandardSlots">true면 최대값, false면 최소값을 기준으로 삼습니다</param>
        /// <param name="standardOffset">기준 인덱스에 더해 줄 오프셋</param>
        /// <param name="standardSlots">기준 인덱스를 찾을 슬롯들의 집합 (가변 인자)</param>
        public static void ChangeSlotDrawOrder_PutOnExtend(this Skeleton currentSkeleton, Slot targetSlot, int targetSlotIndex, bool isHigher_StandardSlots, int standardOffset, params Slot[] standardSlots)
        {
            ChangeSlotDrawOrder_PutOnExtend(
                currentSkeleton,
                targetSlot,
                targetSlotIndex,
                (IList<Slot>)standardSlots,
                isHigher_StandardSlots,
                standardOffset
            );
        }

        /// <summary>
        /// <see cref="Slot"/>의 DrawOrder를 <paramref name="standardSlots"/> 중
        /// 가장 작은/큰 인덱스를 기준으로(<paramref name="isHigher_StandardSlots"/>),
        /// <paramref name="standardOffset"/> 만큼 떨어진 위치로 옮깁니다.
        /// <para>
        /// <see cref="params Slot[]"/>를 통해 여러 Slot을 편리하게 전달받습니다.
        /// </para>
        /// </summary>
        /// <param name="currentSkeleton">DrawOrder를 갖는 <see cref="Skeleton"/> 객체</param>
        /// <param name="targetSlot">옮길 슬롯</param>
        /// <param name="isHigher_StandardSlots">true면 최대값, false면 최소값을 기준으로 삼습니다</param>
        /// <param name="standardOffset">기준 인덱스에 더해 줄 오프셋</param>
        /// <param name="standardSlots">기준 인덱스를 찾을 슬롯들의 집합 (가변 인자)</param>
        public static void ChangeSlotDrawOrder_PutOnExtend(this Skeleton currentSkeleton, Slot targetSlot, bool isHigher_StandardSlots, int standardOffset, params Slot[] standardSlots)
        {
            ChangeSlotDrawOrder_PutOnExtend(
                currentSkeleton,
                targetSlot,
                currentSkeleton.DrawOrder.Pose.IndexOf(targetSlot),
                isHigher_StandardSlots,
                standardOffset,
                standardSlots
            );
        }



        ///======================================================================================================================================================



        #region 슬롯 그룹을 이동

        //? Slot "그룹"의 DrawOrder 이동 (블록 이동) - Update 전제 최적화 버전 (IList 입력)
        #region 설명
        //. - IList로 슬롯을 받되, 멤버십 체크는 Slot.Data.Index 기반 byte 마킹(=Contains/IndexOf 없음)
        //. - 버퍼는 ref로 받아오며, null/부족하면 메서드 내에서 확보/확장
        //. - 실패는 조용히 종료하지 않고 예외로 터짐 (요구사항) 
        #endregion



        /// <summary>
        /// <see cref="Skeleton"/>의 DrawOrder에서 <paramref name="targetSlots"/> 그룹을 블록으로 묶어,
        /// <paramref name="standardSlot"/> 기준 "아래(뒤)" 방향으로 <paramref name="distance"/>칸 떨어진 위치에 배치합니다.
        /// <para>그룹 내부 순서는 "현재 DrawOrder에 존재하던 순서"를 유지합니다.</para>
        /// </summary>
        /// <param name="currentSkeleton">
        /// 대상 스켈레톤입니다.
        /// </param>
        /// <param name="targetSlots">
        /// 이동할 슬롯 그룹입니다. 모든 슬롯은 <paramref name="currentSkeleton"/> 소속이며 DrawOrder 내에 존재해야 합니다.
        /// </param>
        /// <param name="standardSlot">
        /// 기준 슬롯입니다. <paramref name="currentSkeleton"/> 소속이며 DrawOrder 내에 존재해야 합니다.
        /// </param>
        /// <param name="distance">
        /// 1이면 바로 아래(뒤)입니다. 0 이하이면 1로 보정됩니다.
        /// </param>
        /// <param name="blockBuffer">
        /// 타겟 슬롯 블록을 임시로 담는 버퍼입니다. null이거나 부족하면 내부에서 확보됩니다.
        /// </param>
        /// <param name="slotMarks">
        /// 슬롯 멤버십 마킹 버퍼입니다(0=없음, 1=타겟). null이거나 부족하면 내부에서 확보됩니다.
        /// </param>
        /// <param name="touchedIndices">
        /// slotMarks를 건드린 dataIndex 기록용 버퍼입니다. null이거나 부족하면 내부에서 확보됩니다.
        /// </param>
        public static void ChangeSlotDrawOrder_PutGroupBelow(
            this Skeleton currentSkeleton,
            IList<Slot> targetSlots,
            Slot standardSlot,
            int distance,
            ref Slot[] blockBuffer,
            ref byte[] slotMarks,
            ref int[] touchedIndices
        )
        {
            if (currentSkeleton == null) throw new ArgumentNullException(nameof(currentSkeleton));

            int drawCount = currentSkeleton.DrawOrder.Pose.Count;
            //! DrawOrder가 비어있으면 에러 (요구사항)
            if (drawCount <= 0) throw new InvalidOperationException("DrawOrder.Count is 0.");

            int targetCount = targetSlots?.Count ?? 0;
            int slotsCount = currentSkeleton.Slots.Count;

            //! 필요한 버퍼 확보/확장
            EnsureBuffer(ref blockBuffer, targetCount);
            EnsureBuffer(ref slotMarks, slotsCount);
            EnsureBuffer(ref touchedIndices, targetCount);

            MoveGroup_StandardSlot_Core(
                drawItems: currentSkeleton.DrawOrder.Pose.Items,
                drawCount: drawCount,
                skeletonSlotsItems: currentSkeleton.Slots.Items,
                skeletonSlotsCount: slotsCount,
                targetSlots: targetSlots,
                standardSlot: standardSlot,
                distance: distance,
                isAbove: false,
                blockBuffer: blockBuffer,
                slotMarks: slotMarks,
                touchedIndices: touchedIndices
            );
        }

        /// <summary>
        /// <see cref="Skeleton"/>의 DrawOrder에서 <paramref name="targetSlots"/> 그룹을 블록으로 묶어,
        /// <paramref name="standardSlot"/> 기준 "위(앞)" 방향으로 <paramref name="distance"/>칸 떨어진 위치에 배치합니다.
        /// <para>그룹 내부 순서는 "현재 DrawOrder에 존재하던 순서"를 유지합니다.</para>
        /// </summary>
        /// <param name="currentSkeleton">
        /// 대상 스켈레톤입니다.
        /// </param>
        /// <param name="targetSlots">
        /// 이동할 슬롯 그룹입니다. 모든 슬롯은 <paramref name="currentSkeleton"/> 소속이며 DrawOrder 내에 존재해야 합니다.
        /// </param>
        /// <param name="standardSlot">
        /// 기준 슬롯입니다. <paramref name="currentSkeleton"/> 소속이며 DrawOrder 내에 존재해야 합니다.
        /// </param>
        /// <param name="distance">
        /// 1이면 바로 위(앞)입니다. 0 이하이면 1로 보정됩니다.
        /// </param>
        /// <param name="blockBuffer">
        /// 타겟 슬롯 블록을 임시로 담는 버퍼입니다. null이거나 부족하면 내부에서 확보됩니다.
        /// </param>
        /// <param name="slotMarks">
        /// 슬롯 멤버십 마킹 버퍼입니다(0=없음, 1=타겟). null이거나 부족하면 내부에서 확보됩니다.
        /// </param>
        /// <param name="touchedIndices">
        /// slotMarks를 건드린 dataIndex 기록용 버퍼입니다. null이거나 부족하면 내부에서 확보됩니다.
        /// </param>
        public static void ChangeSlotDrawOrder_PutGroupAbove(
            this Skeleton currentSkeleton,
            IList<Slot> targetSlots,
            Slot standardSlot,
            int distance,
            ref Slot[] blockBuffer,
            ref byte[] slotMarks,
            ref int[] touchedIndices
        )
        {
            if (currentSkeleton == null) throw new ArgumentNullException(nameof(currentSkeleton));

            int drawCount = currentSkeleton.DrawOrder.Pose.Count;
            //! DrawOrder가 비어있으면 에러 (요구사항)
            if (drawCount <= 0) throw new InvalidOperationException("DrawOrder.Count is 0.");

            int targetCount = targetSlots?.Count ?? 0;
            int slotsCount = currentSkeleton.Slots.Count;

            //! 필요한 버퍼 확보/확장
            EnsureBuffer(ref blockBuffer, targetCount);
            EnsureBuffer(ref slotMarks, slotsCount);
            EnsureBuffer(ref touchedIndices, targetCount);

            MoveGroup_StandardSlot_Core(
                drawItems: currentSkeleton.DrawOrder.Pose.Items,
                drawCount: drawCount,
                skeletonSlotsItems: currentSkeleton.Slots.Items,
                skeletonSlotsCount: slotsCount,
                targetSlots: targetSlots,
                standardSlot: standardSlot,
                distance: distance,
                isAbove: true,
                blockBuffer: blockBuffer,
                slotMarks: slotMarks,
                touchedIndices: touchedIndices
            );
        }

        /// <summary>
        /// <see cref="Skeleton"/>의 DrawOrder에서 <paramref name="targetSlots"/> 그룹을 블록으로 묶어,
        /// <paramref name="standardSlots"/> 그룹의 "최하단(가장 뒤)" 바로 아래(뒤)에 배치합니다.
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
        public static void ChangeSlotDrawOrder_PutGroupBelow(
            this Skeleton currentSkeleton,
            IList<Slot> targetSlots,
            IList<Slot> standardSlots,
            ref Slot[] blockBuffer,
            ref byte[] slotMarks,
            ref int[] touchedIndices
        )
        {
            if (currentSkeleton == null) throw new ArgumentNullException(nameof(currentSkeleton));

            int drawCount = currentSkeleton.DrawOrder.Pose.Count;
            //! DrawOrder가 비어있으면 에러 (요구사항)
            if (drawCount <= 0) throw new InvalidOperationException("DrawOrder.Count is 0.");

            int targetCount = targetSlots?.Count ?? 0;
            int standardCount = standardSlots?.Count ?? 0;
            int slotsCount = currentSkeleton.Slots.Count;

            //! 필요한 버퍼 확보/확장
            EnsureBuffer(ref blockBuffer, targetCount);
            EnsureBuffer(ref slotMarks, slotsCount);
            EnsureBuffer(ref touchedIndices, targetCount + standardCount);

            MoveGroup_StandardGroup_Core(
                drawItems: currentSkeleton.DrawOrder.Pose.Items,
                drawCount: drawCount,
                skeletonSlotsItems: currentSkeleton.Slots.Items,
                skeletonSlotsCount: slotsCount,
                targetSlots: targetSlots,
                standardSlots: standardSlots,
                isAbove: false,
                blockBuffer: blockBuffer,
                slotMarks: slotMarks,
                touchedIndices: touchedIndices
            );
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
        public static void ChangeSlotDrawOrder_PutGroupAbove(
            this Skeleton currentSkeleton,
            IList<Slot> targetSlots,
            IList<Slot> standardSlots,
            ref Slot[] blockBuffer,
            ref byte[] slotMarks,
            ref int[] touchedIndices
        )
        {
            if (currentSkeleton == null) throw new ArgumentNullException(nameof(currentSkeleton));

            int drawCount = currentSkeleton.DrawOrder.Pose.Count;
            //! DrawOrder가 비어있으면 에러 (요구사항)
            if (drawCount <= 0) throw new InvalidOperationException("DrawOrder.Count is 0.");

            int targetCount = targetSlots?.Count ?? 0;
            int standardCount = standardSlots?.Count ?? 0;
            int slotsCount = currentSkeleton.Slots.Count;

            //! 필요한 버퍼 확보/확장
            EnsureBuffer(ref blockBuffer, targetCount);
            EnsureBuffer(ref slotMarks, slotsCount);
            EnsureBuffer(ref touchedIndices, targetCount + standardCount);

            MoveGroup_StandardGroup_Core(
                drawItems: currentSkeleton.DrawOrder.Pose.Items,
                drawCount: drawCount,
                skeletonSlotsItems: currentSkeleton.Slots.Items,
                skeletonSlotsCount: slotsCount,
                targetSlots: targetSlots,
                standardSlots: standardSlots,
                isAbove: true,
                blockBuffer: blockBuffer,
                slotMarks: slotMarks,
                touchedIndices: touchedIndices
            );
        }



        //. Core (standardSlot 버전)


        private static void MoveGroup_StandardSlot_Core(
            Slot[] drawItems,
            int drawCount,
            Slot[] skeletonSlotsItems,
            int skeletonSlotsCount,
            IList<Slot> targetSlots,
            Slot standardSlot,
            int distance,
            bool isAbove,
            Slot[] blockBuffer,
            byte[] slotMarks,
            int[] touchedIndices
        )
        {
            if (drawItems == null) throw new ArgumentNullException(nameof(drawItems));
            if (skeletonSlotsItems == null) throw new ArgumentNullException(nameof(skeletonSlotsItems));

            if (targetSlots == null) throw new ArgumentNullException(nameof(targetSlots));
            if (targetSlots.Count <= 0) throw new ArgumentException("targetSlots.Count must be > 0.", nameof(targetSlots));

            if (standardSlot == null) throw new ArgumentNullException(nameof(standardSlot));

            if (drawCount > drawItems.Length) drawCount = drawItems.Length;
            if (drawCount <= 0) throw new InvalidOperationException("DrawOrder.Count is 0.");

            if (skeletonSlotsCount <= 0) throw new ArgumentException("skeletonSlotsCount must be > 0.", nameof(skeletonSlotsCount));

            //! 버퍼는 ref에서 이미 확보했어도, 혹시라도 외부에서 깨진 값을 넣을 수 있으니 한 번 더 방어
            if (slotMarks == null || slotMarks.Length < skeletonSlotsCount) throw new ArgumentException("slotMarks is invalid.", nameof(slotMarks));
            if (blockBuffer == null || blockBuffer.Length < targetSlots.Count) throw new ArgumentException("blockBuffer is invalid.", nameof(blockBuffer));
            if (touchedIndices == null || touchedIndices.Length < targetSlots.Count) throw new ArgumentException("touchedIndices is invalid.", nameof(touchedIndices));

            if (distance <= 0) distance = 1;

            int touchedCount = 0;

            try
            {
                MarkTargetSlots(
                    slots: targetSlots,
                    skeletonSlotsItems: skeletonSlotsItems,
                    skeletonSlotsCount: skeletonSlotsCount,
                    slotMarks: slotMarks,
                    touchedIndices: touchedIndices,
                    ref touchedCount
                );

                ValidateSlotBelongsToSkeleton(standardSlot, skeletonSlotsItems, skeletonSlotsCount, nameof(standardSlot));

                //! 기준 슬롯이 타겟에 포함되면 에러
                if ((slotMarks[standardSlot.Data.Index] & 1) != 0) throw new InvalidOperationException("standardSlot is contained in targetSlots.");

                int blockCount;
                int standardIndex;
                int currentStart;
                bool scattered;

                Scan_DrawOrder_StandardSlot(
                    drawItems: drawItems,
                    drawCount: drawCount,
                    slotMarks: slotMarks,
                    standardSlot: standardSlot,
                    out blockCount,
                    out standardIndex,
                    out currentStart,
                    out scattered
                );

                //! DrawOrder 내 존재 보장: 불일치면 에러
                if (blockCount != targetSlots.Count) throw new InvalidOperationException("Some targetSlots are not in DrawOrder.");
                if (standardIndex < 0) throw new InvalidOperationException("standardSlot is not in DrawOrder.");

                int remainingCount = drawCount - blockCount;

                //. insertIndex 계산
                int insertIndex = isAbove
                    ? (standardIndex - (distance - 1))
                    : (standardIndex + distance);

                if (insertIndex < 0) insertIndex = 0;
                else if (insertIndex > remainingCount) insertIndex = remainingCount;

                //. 이미 연속 블록이고 목표 위치면 종료
                if (!scattered && currentStart == insertIndex) return;

                Compact_And_Insert(
                    drawItems: drawItems,
                    drawCount: drawCount,
                    slotMarks: slotMarks,
                    blockBuffer: blockBuffer,
                    blockCount: blockCount,
                    insertIndex: insertIndex
                );
            }
            finally
            {
                //? touchedIndices만큼만 원복 (전체 Clear 금지)
                UnmarkTouched(slotMarks, touchedIndices, touchedCount);
            }
        }



        //. Core (standardSlots 그룹 버전)
        private static void MoveGroup_StandardGroup_Core(
            Slot[] drawItems,
            int drawCount,
            Slot[] skeletonSlotsItems,
            int skeletonSlotsCount,
            IList<Slot> targetSlots,
            IList<Slot> standardSlots,
            bool isAbove,
            Slot[] blockBuffer,
            byte[] slotMarks,
            int[] touchedIndices
        )
        {
            if (drawItems == null) throw new ArgumentNullException(nameof(drawItems));
            if (skeletonSlotsItems == null) throw new ArgumentNullException(nameof(skeletonSlotsItems));

            if (targetSlots == null) throw new ArgumentNullException(nameof(targetSlots));
            if (targetSlots.Count <= 0) throw new ArgumentException("targetSlots.Count must be > 0.", nameof(targetSlots));

            if (standardSlots == null) throw new ArgumentNullException(nameof(standardSlots));
            if (standardSlots.Count <= 0) throw new ArgumentException("standardSlots.Count must be > 0.", nameof(standardSlots));

            if (drawCount > drawItems.Length) drawCount = drawItems.Length;
            if (drawCount <= 0) throw new InvalidOperationException("DrawOrder.Count is 0.");

            if (skeletonSlotsCount <= 0) throw new ArgumentException("skeletonSlotsCount must be > 0.", nameof(skeletonSlotsCount));

            int needTouched = targetSlots.Count + standardSlots.Count;

            //! 버퍼 방어
            if (slotMarks == null || slotMarks.Length < skeletonSlotsCount) throw new ArgumentException("slotMarks is invalid.", nameof(slotMarks));
            if (blockBuffer == null || blockBuffer.Length < targetSlots.Count) throw new ArgumentException("blockBuffer is invalid.", nameof(blockBuffer));
            if (touchedIndices == null || touchedIndices.Length < needTouched) throw new ArgumentException("touchedIndices is invalid.", nameof(touchedIndices));

            int touchedCount = 0;

            try
            {
                MarkTargetSlots(
                    slots: targetSlots,
                    skeletonSlotsItems: skeletonSlotsItems,
                    skeletonSlotsCount: skeletonSlotsCount,
                    slotMarks: slotMarks,
                    touchedIndices: touchedIndices,
                    ref touchedCount
                );

                MarkStandardSlots(
                    slots: standardSlots,
                    skeletonSlotsItems: skeletonSlotsItems,
                    skeletonSlotsCount: skeletonSlotsCount,
                    slotMarks: slotMarks,
                    touchedIndices: touchedIndices,
                    ref touchedCount
                );

                int blockCount;
                int minStandard;
                int maxStandard;
                int currentStart;
                bool scattered;

                Scan_DrawOrder_StandardGroup(
                    drawItems: drawItems,
                    drawCount: drawCount,
                    slotMarks: slotMarks,
                    out blockCount,
                    out minStandard,
                    out maxStandard,
                    out currentStart,
                    out scattered
                );

                if (blockCount != targetSlots.Count) throw new InvalidOperationException("Some targetSlots are not in DrawOrder.");
                if (minStandard == int.MaxValue) throw new InvalidOperationException("Some standardSlots are not in DrawOrder.");

                int remainingCount = drawCount - blockCount;

                int insertIndex = isAbove ? minStandard : (maxStandard + 1);

                if (insertIndex < 0) insertIndex = 0;
                else if (insertIndex > remainingCount) insertIndex = remainingCount;

                if (!scattered && currentStart == insertIndex) return;

                Compact_And_Insert(
                    drawItems: drawItems,
                    drawCount: drawCount,
                    slotMarks: slotMarks,
                    blockBuffer: blockBuffer,
                    blockCount: blockCount,
                    insertIndex: insertIndex
                );
            }
            finally
            {
                UnmarkTouched(slotMarks, touchedIndices, touchedCount);
            }
        }



        //. PASS 1 Scans  
        private static void Scan_DrawOrder_StandardSlot(
            Slot[] drawItems,
            int drawCount,
            byte[] slotMarks,
            Slot standardSlot,
            out int blockCount,
            out int standardIndex,
            out int currentStart,
            out bool scattered
        )
        {
            blockCount = 0;
            standardIndex = -1;
            currentStart = -1;

            bool seenTarget = false;
            bool seenNonTargetAfterTarget = false;
            scattered = false;

            int remainingIndex = 0;

            for (int i = 0; i < drawCount; i++)
            {
                Slot s = drawItems[i];
                int di = s.Data.Index;

                bool isTarget = (slotMarks[di] & 1) != 0;

                if (isTarget)
                {
                    if (currentStart < 0) currentStart = remainingIndex;

                    blockCount++;
                    seenTarget = true;

                    if (seenNonTargetAfterTarget) scattered = true;
                    continue;
                }

                if (seenTarget) seenNonTargetAfterTarget = true;

                if (ReferenceEquals(s, standardSlot))
                {
                    standardIndex = remainingIndex;
                }

                remainingIndex++;
            }
        }

        private static void Scan_DrawOrder_StandardGroup(
            Slot[] drawItems,
            int drawCount,
            byte[] slotMarks,
            out int blockCount,
            out int minStandard,
            out int maxStandard,
            out int currentStart,
            out bool scattered
        )
        {
            blockCount = 0;
            currentStart = -1;

            bool seenTarget = false;
            bool seenNonTargetAfterTarget = false;
            scattered = false;

            minStandard = int.MaxValue;
            maxStandard = int.MinValue;

            int remainingIndex = 0;

            for (int i = 0; i < drawCount; i++)
            {
                Slot s = drawItems[i];
                int di = s.Data.Index;

                bool isTarget = (slotMarks[di] & 1) != 0;

                if (isTarget)
                {
                    if (currentStart < 0) currentStart = remainingIndex;

                    blockCount++;
                    seenTarget = true;

                    if (seenNonTargetAfterTarget) scattered = true;
                    continue;
                }

                if (seenTarget) seenNonTargetAfterTarget = true;

                if ((slotMarks[di] & 2) != 0)
                {
                    if (remainingIndex < minStandard) minStandard = remainingIndex;
                    if (remainingIndex > maxStandard) maxStandard = remainingIndex;
                }

                remainingIndex++;
            }
        }



        //. PASS 2 (Compaction + Insert)
        private static void Compact_And_Insert(
            Slot[] drawItems,
            int drawCount,
            byte[] slotMarks,
            Slot[] blockBuffer,
            int blockCount,
            int insertIndex
        )
        {
            int write = 0;
            int blockWrite = 0;

            for (int i = 0; i < drawCount; i++)
            {
                Slot s = drawItems[i];
                int di = s.Data.Index;

                if ((slotMarks[di] & 1) != 0)
                {
                    blockBuffer[blockWrite++] = s;
                }
                else
                {
                    drawItems[write++] = s;
                }
            }

            if (blockWrite != blockCount) throw new InvalidOperationException("Internal error: blockWrite mismatch.");

            int remainingCount = drawCount - blockCount;

            int tailCount = remainingCount - insertIndex;
            if (tailCount > 0)
            {
                Array.Copy(drawItems, insertIndex, drawItems, insertIndex + blockCount, tailCount);
            }

            Array.Copy(blockBuffer, 0, drawItems, insertIndex, blockCount);
        }



        //. Mark / Unmark
        private static void MarkTargetSlots(
            IList<Slot> slots,
            Slot[] skeletonSlotsItems,
            int skeletonSlotsCount,
            byte[] slotMarks,
            int[] touchedIndices,
            ref int touchedCount
        )
        {
            for (int i = 0; i < slots.Count; i++)
            {
                Slot s = slots[i];
                if (s == null) throw new ArgumentNullException(nameof(slots), "slots contains null.");

                ValidateSlotBelongsToSkeleton(s, skeletonSlotsItems, skeletonSlotsCount, nameof(slots));

                int di = s.Data.Index;

                //! 중복 타겟 검출
                if ((slotMarks[di] & 1) != 0) throw new InvalidOperationException("targetSlots has duplicate slots.");

                slotMarks[di] = (byte)(slotMarks[di] | 1);
                touchedIndices[touchedCount++] = di;
            }
        }

        private static void MarkStandardSlots(
            IList<Slot> slots,
            Slot[] skeletonSlotsItems,
            int skeletonSlotsCount,
            byte[] slotMarks,
            int[] touchedIndices,
            ref int touchedCount
        )
        {
            for (int i = 0; i < slots.Count; i++)
            {
                Slot s = slots[i];
                if (s == null) throw new ArgumentNullException(nameof(slots), "slots contains null.");

                ValidateSlotBelongsToSkeleton(s, skeletonSlotsItems, skeletonSlotsCount, nameof(slots));

                int di = s.Data.Index;
                byte m = slotMarks[di];

                //! 기준 중복/겹침 검출
                if ((m & 2) != 0) throw new InvalidOperationException("standardSlots has duplicate slots.");
                if ((m & 1) != 0) throw new InvalidOperationException("standardSlots overlaps targetSlots.");

                slotMarks[di] = (byte)(m | 2);
                touchedIndices[touchedCount++] = di;
            }
        }

        private static void UnmarkTouched(byte[] slotMarks, int[] touchedIndices, int touchedCount)
        {
            for (int i = 0; i < touchedCount; i++)
            {
                slotMarks[touchedIndices[i]] = 0;
            }
        }



        //. Validation
        /// <summary>
        /// <paramref name="slot"/>이 현재 <see cref="Skeleton"/>의 Slot 컬렉션에 소속된 슬롯인지 O(1)로 검증합니다.
        /// </summary>
        /// <param name="slot">
        /// 검증할 슬롯입니다.
        /// </param>
        /// <param name="skeletonSlotsItems">
        /// <see cref="Skeleton.Slots"/>의 내부 배열입니다.
        /// </param>
        /// <param name="skeletonSlotsCount">
        /// <see cref="Skeleton.Slots.Count"/> 값입니다.
        /// </param>
        /// <param name="paramName">
        /// 예외 메시지에 사용할 파라미터 이름입니다.
        /// </param>
        private static void ValidateSlotBelongsToSkeleton(Slot slot, Slot[] skeletonSlotsItems, int skeletonSlotsCount, string paramName)
        {
            if (slot == null) throw new ArgumentNullException(paramName);

            var data = slot.Data;
            if (data == null) throw new ArgumentException("Slot.Data is null.", paramName);

            int di = data.Index;

            //! 스켈레톤 소속 슬롯인지 O(1)로 검증
            if ((uint)di >= (uint)skeletonSlotsCount) throw new ArgumentException("Slot does not belong to this Skeleton.", paramName);
            if (!ReferenceEquals(skeletonSlotsItems[di], slot)) throw new ArgumentException("Slot does not belong to this Skeleton.", paramName);
        }


        //. Buffer Helpers
        /// <summary>
        /// <paramref name="buffer"/>가 null이거나 길이가 부족하면 확장/초기화합니다.
        /// </summary>
        /// <param name="buffer">
        /// 확보/확장할 버퍼입니다.
        /// </param>
        /// <param name="required">
        /// 필요한 최소 길이입니다.
        /// </param>
        private static void EnsureBuffer(ref Slot[] buffer, int required)
        {
            if (required <= 0) return;

            if (buffer == null)
            {
                buffer = new Slot[required];
                return;
            }

            if (buffer.Length < required)
            {
                buffer = new Slot[GrowSize(buffer.Length, required)];
            }
        }

        /// <summary>
        /// <paramref name="buffer"/>가 null이거나 길이가 부족하면 확장/초기화합니다.
        /// </summary>
        /// <param name="buffer">
        /// 확보/확장할 버퍼입니다.
        /// </param>
        /// <param name="required">
        /// 필요한 최소 길이입니다.
        /// </param>
        private static void EnsureBuffer(ref byte[] buffer, int required)
        {
            if (required <= 0) return;

            if (buffer == null)
            {
                buffer = new byte[required];
                return;
            }

            if (buffer.Length < required)
            {
                buffer = new byte[GrowSize(buffer.Length, required)];
            }
        }

        /// <summary>
        /// <paramref name="buffer"/>가 null이거나 길이가 부족하면 확장/초기화합니다.
        /// </summary>
        /// <param name="buffer">
        /// 확보/확장할 버퍼입니다.
        /// </param>
        /// <param name="required">
        /// 필요한 최소 길이입니다.
        /// </param>
        private static void EnsureBuffer(ref int[] buffer, int required)
        {
            if (required <= 0) return;

            if (buffer == null)
            {
                buffer = new int[required];
                return;
            }

            if (buffer.Length < required)
            {
                buffer = new int[GrowSize(buffer.Length, required)];
            }
        }

        private static int GrowSize(int current, int required)
        {
            //. 너무 자주 재할당되지 않도록 완만한 성장
            int newLen = current > 0 ? (current << 1) : 4;

            if (newLen < required) newLen = required;

            //! int 오버플로우 방어
            if (newLen < 0) newLen = required;

            return newLen;
        }

        #endregion



        ///======================================================================================================================================================



        #region 이전 ChangeSlotDrawOrder들 백업

        ///// <summary>
        ///// <see cref="Slot"/>의 DrawOrder 이동
        ///// </summary>
        ///// <param name="slot">옮길 슬롯</param>
        ///// <param name="drawOrderIndex">옮길 새로운 DrawOrderIndex</param>
        //public static void ChangeSlotDrawOrder(this Skeleton skeleton, Slot slot, int drawOrderIndex)
        //{
        //    if (slot == null) return;

        //    var drawOrder = skeleton.DrawOrder;

        //    int oldIndex = drawOrder.IndexOf(slot);

        //    if (drawOrderIndex < 0)
        //    {
        //        drawOrderIndex = 0;
        //    }
        //    else if (drawOrderIndex > drawOrder.Count - 1)
        //    {
        //        drawOrderIndex = drawOrder.Count - 1;
        //    }

        //    drawOrder.RemoveAt(oldIndex);
        //    drawOrder.Insert(drawOrderIndex, slot);
        //}



        ///// <summary>
        ///// <see cref="Slot"/>의 DrawOrder 이동
        ///// </summary>
        ///// <param name="slotName">옮길 슬롯의 이름</param>
        ///// <param name="drawOrderIndex">옮길 새로운 DrawOrderIndex</param>
        //public static void ChangeSlotDrawOrder(this Skeleton skeleton, string slotName, int drawOrderIndex)
        //{
        //    ChangeSlotDrawOrder(skeleton, skeleton.FindSlot(slotName), drawOrderIndex);
        //}



        ///// <summary>
        ///// <see cref="Slot"/>의 DrawOrder 를 특정 <see cref="Slot"/>의 DrawOrderIndex에 <paramref name="plusDrawOrderIndex"/> 만큼 더한 값으로 옮기기
        ///// </summary>
        ///// <param name="targetSlot">옮길 슬롯 대상</param>
        ///// <param name="baseSlot">이 슬롯의 DrawOrderIndex를 기준으로 계산</param>
        ///// <param name="plusDrawOrderIndex">계산되는 DrawOrderIndex</param>
        //public static void ChangeSlotDrawOrder_PutOn(this Skeleton skeleton, Slot targetSlot, Slot baseSlot, int plusDrawOrderIndex)
        //{
        //    if (targetSlot == null || baseSlot == null) return;

        //    var drawOrder = skeleton.DrawOrder;

        //    int baseIndex = drawOrder.IndexOf(targetSlot);
        //    int targetIndex = drawOrder.IndexOf(baseSlot);

        //    int newIndex = baseIndex + plusDrawOrderIndex;

        //    if (newIndex < 0)
        //    {
        //        newIndex = 0;
        //    }
        //    else if (newIndex > drawOrder.Count - 1)
        //    {
        //        newIndex = drawOrder.Count - 1;
        //    }

        //    drawOrder.RemoveAt(targetIndex);
        //    drawOrder.Insert(newIndex, baseSlot);
        //}



        ///// <summary>
        ///// <see cref="Slot"/>의 DrawOrder 를 특정 <see cref="Slot"/>의 DrawOrderIndex에 <paramref name="plusDrawOrderIndex"/> 만큼 더한 값으로 옮기기
        ///// </summary>
        ///// <param name="targetSlotName">옮길 슬롯 대상 (의 이름)</param>
        ///// <param name="baseSlotName">이 슬롯의 DrawOrderIndex를 기준으로 계산 (의 이름)</param>
        ///// <param name="plusDrawOrderIndex">계산되는 DrawOrderIndex</param>
        //public static void ChangeSlotDrawOrder_PutOn(this Skeleton skeleton, string targetSlotName, string baseSlotName, int plusDrawOrderIndex)
        //{
        //    ChangeSlotDrawOrder_PutOn(skeleton, skeleton.FindSlot(targetSlotName), skeleton.FindSlot(baseSlotName), plusDrawOrderIndex);
        //}



        ///// <summary>
        ///// <see cref="Slot"/>의 DrawOrder 를 특정 <see cref="Slot"/>들의 집합중,
        ///// <para>가장 작은/큰 값 (<paramref name="isHigher"/>)의 DrawOrderIndex에,</para>
        ///// <para><paramref name="plusDrawOrderIndex"/> 만큼 더한 값으로 옮기기</para>
        ///// </summary>
        ///// <param name="targetSlot">옮길 슬롯 대상</param>
        ///// <param name="baseSlots">이 슬롯들의 DrawOrderIndex를 기준으로 계산</param>
        ///// <param name="isHigher">false라면, <paramref name="baseSlots"/>들 중 가장 작은값이 대상, true라면 가장 높은 값이 대상</param>
        ///// <param name="plusDrawOrderIndex">계산되는 DrawOrderIndex</param>
        //public static void ChangeSlotDrawOrder_PutOnExtend(this Skeleton skeleton, Slot targetSlot, IList<Slot> baseSlots, bool isHigher, int plusDrawOrderIndex)
        //{
        //    //! skeleton이나 baseSlot이 null이면 종료
        //    if (skeleton == null || targetSlot == null) return;

        //    //. skeleton의 DrawOrder 참조
        //    var drawOrder = skeleton.DrawOrder;

        //    //? 유효한 Slot 인덱스의 최소/최대값 찾기 위한 변수
        //    int minIndex = int.MaxValue;
        //    int maxIndex = int.MinValue;

        //    //. targetSlots 반복하며 유효한 인덱스를 판별
        //    for (int i = 0; i < baseSlots.Count; i++)
        //    {
        //        Slot slot = baseSlots[i];
        //        if (slot == null) continue;

        //        int index = drawOrder.IndexOf(slot);
        //        if (index >= 0)
        //        {
        //            if (index < minIndex) minIndex = index;
        //            if (index > maxIndex) maxIndex = index;
        //        }
        //    }

        //    //! 유효한 인덱스를 하나도 찾지 못했다면 종료
        //    if (minIndex == int.MaxValue) return;

        //    //. isHigher가 true면 최대 인덱스(maxIndex), false면 최소 인덱스(minIndex)를 기준으로 삼음
        //    int anchorIndex = isHigher ? maxIndex : minIndex;

        //    //? anchorIndex에 plusIndex를 더해서 최종 이동 위치 계산
        //    int newIndex = anchorIndex + plusDrawOrderIndex;

        //    //! 유효 범위를 벗어나지 않도록 보정
        //    if (newIndex < 0)
        //    {
        //        newIndex = 0;
        //    }
        //    else if (newIndex >= drawOrder.Count)
        //    {
        //        newIndex = drawOrder.Count - 1;
        //    }

        //    //. baseSlot을 DrawOrder에서 제거하고, 결정된 위치로 삽입
        //    drawOrder.Remove(targetSlot);
        //    drawOrder.Insert(newIndex, targetSlot);
        //}



        ///// <summary>
        ///// <see cref="Slot"/>의 DrawOrder 를 특정 <see cref="Slot"/>들의 집합중,
        ///// <para>가장 작은/큰 값 (<paramref name="isHigher"/>)의 DrawOrderIndex에,</para>
        ///// <para><paramref name="plusDrawOrderIndex"/> 만큼 더한 값으로 옮기기</para>
        ///// </summary>
        ///// <param name="targetSlot">옮길 슬롯 대상</param>
        ///// <param name="isHigher">false라면, <paramref name="baseSlots"/>들 중 가장 작은값이 대상, true라면 가장 높은 값이 대상</param>
        ///// <param name="plusDrawOrderIndex">계산되는 DrawOrderIndex</param>
        ///// <param name="baseSlots">이 슬롯들의 DrawOrderIndex를 기준으로 계산</param>
        //public static void ChangeSlotDrawOrder_PutOnExtend(this Skeleton skeleton, Slot targetSlot, bool isHigher, int plusDrawOrderIndex, params Slot[] baseSlots)
        //{
        //    ChangeSlotDrawOrder_PutOnExtend(skeleton, targetSlot, baseSlots, isHigher, plusDrawOrderIndex);
        //} 

        #endregion



        ///======================================================================================================================================================



        //? Bone



        /// <summary>
        /// BoneData의 전체 경로를 반환합니다. (예: "root/Arm/Hand")
        /// </summary>
        public static string GetBonePath(this BoneData boneData)
        {
            if (boneData == null) return string.Empty;

            StringBuilder pathBuilder = new StringBuilder();
            BoneData current = boneData;

            while (current != null)
            {
                if (pathBuilder.Length > 0)
                    pathBuilder.Insert(0, '/');

                pathBuilder.Insert(0, current.Name);
                current = current.Parent;
            }

            return pathBuilder.ToString();
        }



        /// <summary>
        /// Bone의 전체 경로를 반환합니다. (예: "root/Arm/Hand")
        /// </summary>
        public static string GetBonePath(this Bone bone)
        {
            if (bone == null) return string.Empty;

            StringBuilder pathBuilder = new StringBuilder();
            Bone current = bone;

            while (current != null)
            {
                if (pathBuilder.Length > 0)
                    pathBuilder.Insert(0, '/');

                pathBuilder.Insert(0, current.Data.Name);
                current = current.Parent;
            }

            return pathBuilder.ToString();
        }



        /// <summary>
        /// <paramref name="boneData"/>부터 부모 체인을 따라 올라가며,
        /// 어떤 조상(자기 자신 포함)의 <see cref="BoneData.Name"/>에 <paramref name="tag"/>가 포함되는지 검사한다.
        /// </summary>
        /// <param name="boneData">검사를 시작할 <see cref="BoneData"/> (null이면 false).</param>
        /// <param name="tag">이름에 포함 여부를 확인할 문자열 (null/empty/whitespace면 false).</param>
        /// <returns>
        /// 부모 체인(자기 자신 포함) 중 하나라도 tag를 포함하면 true,
        /// 끝까지 없으면 false.
        /// </returns>
        public static bool HasTagInSelfOrParents(this BoneData boneData, string tag)
        {
            //! tag가 비어있으면 "포함"의 의미가 모호하므로 실패로 간주
            if (string.IsNullOrWhiteSpace(tag))
                return false;

            //? 부모 체인을 따라 순회하며 Name에 tag가 포함되는지 검사
            for (BoneData current = boneData; current != null; current = current.Parent)
            {
                //. 현재 본의 이름
                string name = current.Name;

                //! null 방어 (Spine 쪽에서 Name이 null일 가능성은 낮지만 안전하게 처리)
                if (name != null && name.IndexOf(tag, StringComparison.Ordinal) >= 0)
                    return true;
            }

            //! 끝까지 못 찾으면 false
            return false;

            //while (true)
            //{
            //    if (boneData == null) { return false; }
            //    if (boneData.Name.Contains(tag)) { return true; }
            //    boneData = boneData.Parent;
            //}
        }



        ///======================================================================================================================================================



        #region Legacy?

        ///// <summary>
        ///// AddSkin 체이닝
        ///// </summary>
        ///// <param name="var"></param>
        ///// <param name="skin">추가할 스킨</param>
        ///// <returns></returns>
        //public static Skin GetAddSkin(this Skin var, Skin skin)
        //{
        //    var.AddSkin(skin);
        //    return var;
        //}


        ///// <summary>
        ///// CopySkin 체이닝
        ///// </summary>
        ///// <param name="var"></param>
        ///// <param name="skin">복사할 스킨</param>
        ///// <returns></returns>
        //public static Skin GetCopySkin(this Skin var, Skin skin)
        //{
        //    var.CopySkin(skin);
        //    return var;
        //}



        /////<summary>이 스킨을 새로 복사(deep copy) 해서 반환 (new 사용)</summary>
        //public static Skin CopyThisSkin(this Skin var)
        //{
        //    return new Skin(var.Name).GetCopySkin(var);
        //} 

        #endregion



        ///======================================================================================================================================================
    }
}
