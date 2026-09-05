using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Spine;
using Spine.Unity;
using System.Runtime.CompilerServices;
using Pan.Util;
using Pan.SpinePackage;
using Pan.SpineUtil;
using Sirenix.OdinInspector;


[assembly: InternalsVisibleTo("Pan.SpinePackages.Tests.Editor")]



//? SkelSbject의 클래스 기반, 카테고리별로 정리되어있는 Box들이 정리되어있는 정도의 코드



namespace Pan.SpinePackage
{
    public abstract partial class SkelSbject
    {
        ///======================================================================================================================================================



        //? 애니메이션 박스



        /// <summary>
        /// 애니메이션 박스를 보유중인 <see cref="SkelSbject"/>
        /// </summary>
        public interface IHoldAniBox
        {
            BaseBox AniBox { get; }
        }

        /// <summary>
        /// 애니메이션 박스를 보유중인 <see cref="TSkelSbject"/>
        /// </summary>
        public interface IHoldAniBox<TSkelSbject, TAniBox> : IHoldAniBox
            where TSkelSbject : SkelSbject, new()
            where TAniBox : BaseBox<TSkelSbject>
        {
            new TAniBox AniBox { get; }
            BaseBox IHoldAniBox.AniBox => AniBox;
        }



        //? 본 박스



        /// <summary>
        /// 본 박스를 보유중인 <see cref="SkelSbject"/>
        /// </summary>
        public interface IHoldBoneBox
        {
            BaseBoneBox BoneBox { get; }
        }

        /// <summary>
        /// 본 박스를 보유중인 <see cref="TSkelSbject"/>
        /// </summary>
        public interface IHoldBoneBox<TSkelSbject, TBoneBox> : IHoldBoneBox
            where TSkelSbject : SkelSbject, new()
            where TBoneBox : BaseBoneBox<TSkelSbject>
        {
            new TBoneBox BoneBox { get; }

            BaseBoneBox IHoldBoneBox.BoneBox => BoneBox;
        }



        //? 스킨 어태치 박스



        /// <summary>
        /// 스킨 어태치 박스를 보유중인 <see cref="SkelSbject"/>
        /// </summary>
        public interface IHoldSkinAtchBox
        {
            BaseBox SkinAtchBox { get; }
        }

        /// <summary>
        /// 스킨 어태치 박스를 보유중인 <see cref="TSkelSbject"/>
        /// </summary>
        public interface IHoldSkinAtchBox<TSkelSbject, TSkinAtchBox> : IHoldSkinAtchBox
            where TSkelSbject : SkelSbject, new()
            where TSkinAtchBox : BaseBox<TSkelSbject>
        {
            new TSkinAtchBox SkinAtchBox { get; }

            BaseBox IHoldSkinAtchBox.SkinAtchBox => SkinAtchBox;
        }



        //? 스파인 이벤트 박스



        /// <summary>
        /// 스파인 이벤트 박스를 보유중인 <see cref="SkelSbject"/>
        /// </summary>
        public interface IHoldSpineEventBox
        {
            BaseBox SpineEventBox { get; }
        }

        /// <summary>
        /// 스파인 이벤트 박스를 보유중인 <see cref="TSkelSbject"/>
        /// </summary>
        public interface IHoldSpineEventBox<TSkelSbject, TSpineEventBox> : IHoldSpineEventBox
            where TSkelSbject : SkelSbject, new()
            where TSpineEventBox : BaseBox<TSkelSbject>
        {
            new TSpineEventBox SpineEventBox { get; }

            BaseBox IHoldSpineEventBox.SpineEventBox => SpineEventBox;
        }



        //? 상호작용 박스



        /// <summary>
        /// 상호작용 박스를 보유중인 <see cref="SkelSbject"/>
        /// </summary>
        public interface IHoldInteractionBox
        {
            BaseInteractionBox InteractionBox { get; }
        }

        /// <summary>
        /// 상호작용 박스를 보유중인 <see cref="TSkelSbject"/>
        /// </summary>
        public interface IHoldInteractionBox<TSkelSbject, TInteractionBox> : IHoldInteractionBox
            where TSkelSbject : SkelSbject, new()
            where TInteractionBox : BaseInteractionBox<TSkelSbject>
        {
            new TInteractionBox InteractionBox { get; }

            BaseInteractionBox IHoldInteractionBox.InteractionBox => InteractionBox;
        }



        //? 드로우오더 박스



        /// <summary>
        /// 드로우오더 박스를 보유중인 <see cref="SkelSbject"/>
        /// </summary>
        public interface IHoldDrawOrderBox
        {
            BaseBox DrawOrderBox { get; }
        }

        /// <summary>
        /// 드로우오더 박스를 보유중인 <see cref="TSkelSbject"/>
        /// </summary>
        public interface IHoldDrawOrderBox<TSkelSbject, TDrawOrderBox> : IHoldDrawOrderBox
            where TSkelSbject : SkelSbject, new()
            where TDrawOrderBox : BaseBox<TSkelSbject>
        {
            new TDrawOrderBox DrawOrderBox { get; }

            BaseBox IHoldDrawOrderBox.DrawOrderBox => DrawOrderBox;
        }



        ///======================================================================================================================================================



        //? 박스의 베이스



        /// <summary>
        /// 베이스가 되는 박스
        /// </summary>
        public abstract class BaseBox
        {
            //? 박스 확장 인터페이스



            /// <summary>
            /// 이 인터페이스를 보유중이라면
            /// <para><see cref="SkelSbject.WakeUp_SkelObjectEvent"/>에 자동으로  <see cref="IWakeUpSkelObject.WakeUp_SkelObject(SkelObject)"/> 이벤트가 등록된다</para>
            /// </summary>
            public interface IWakeUpSkelObject
            {
                /// <summary>
                /// <see cref="SkelSbject.WakeUp_SkelObjectEvent"/>에 이 메서드가 등록된다
                /// </summary>
                void WakeUp_SkelObject(SkelObject skelObject);
            }

            /// <summary>
            /// 이 인터페이스를 보유중이라면
            /// <para><see cref="SkelSbject.Enable_SkelObjectEvent"/>에 자동으로  <see cref="IWakeUpSkelObject.Enable_SkelObject(SkelObject)"/> 이벤트가 등록된다</para>
            /// </summary>
            public interface IEnableSkelObject
            {
                /// <summary>
                /// <see cref="SkelSbject.Enable_SkelObjectEvent"/>에 이 메서드가 등록된다
                /// </summary>
                void Enable_SkelObject(SkelObject skelObject);
            }

            /// <summary>
            /// 이 인터페이스를 보유중이라면
            /// <para><see cref="SkelSbject.Disable_SkelObjectEvent"/>에 자동으로  <see cref="IWakeUpSkelObject.Disable_SkelObject(SkelObject)"/> 이벤트가 등록된다</para>
            /// </summary>
            public interface IDisableSkelObject
            {
                /// <summary>
                /// <see cref="SkelSbject.Disable_SkelObjectEvent"/>에 이 메서드가 등록된다
                /// </summary>
                void Disable_SkelObject(SkelObject skelObject);
            }



            /// <summary>
            /// 초기화 메서드에서 다른 Box를 의존 하지 않는 것이 원칙
            /// <para>Box들의 호출 순서가 무작위더라도, 정상적으로 작동해야함</para>
            /// </summary>
            public BaseBox(SkelSbject skelSbject)
            {
                SkelSbject = skelSbject;

                //? 인터페이스 확인 이후, 델리게이트 자동 등록
                if (this is IWakeUpSkelObject wakeUpSkelObject) { SkelSbject.WakeUp_SkelObjectEvent += wakeUpSkelObject.WakeUp_SkelObject; }
                if (this is IEnableSkelObject enableSkelObject) { SkelSbject.Enable_SkelObjectEvent += enableSkelObject.Enable_SkelObject; }
                if (this is IDisableSkelObject disableSkelObject) { SkelSbject.Disable_SkelObjectEvent += disableSkelObject.Disable_SkelObject; }
            }



            protected readonly SkelSbject SkelSbject;
        }



        /// <summary>
        /// 베이스가 되는 본 박스
        /// </summary>
        public abstract class BaseBoneBox : BaseBox
        {
            protected BaseBoneBox(SkelSbject skelSbject) : base(skelSbject) { }



            ///<summary>
            /// <see cref="Bone"/>을 얻는다
            ///</summary>
            public Bone GetBone(SkelObject.SkelCore.IGetBonesRunTime getBonesRunTime, string boneName) => getBonesRunTime.GetBone(boneName);



            ///<summary>
            /// <see cref="RunTimeBone"/>을 얻는다
            ///</summary>
            public SkelObject.SkelCore.RunTimeBone GetRunTimeBone(SkelObject.SkelCore.IGetBonesRunTime getBonesRunTime, string boneName) => getBonesRunTime.GetRunTimeBone(boneName);



            /// <summary>
            /// <see cref="Bone"/>을 선언하고싶을경우, 변수 이름에 이 문자열이 포함되어있어야한다 (리플렉션에서 사용)
            /// </summary>
            public const string BONE_VALUES_NAME = "Bone_";
        }



        /// <summary>
        /// 베이스가 되는 상호작용 박스
        /// </summary>
        public abstract class BaseInteractionBox : BaseBox
        {
            protected BaseInteractionBox(SkelSbject skelSbject) : base(skelSbject) { }

            public abstract AnimationInteractionManagerBase[] GetInteractionManagers { get; }
        }



        /// <summary>
        /// 베이스가 되는 박스
        /// </summary>
        /// <typeparam name="TSkelSbject"></typeparam>
        public abstract class BaseBox<TSkelSbject> : BaseBox where TSkelSbject : SkelSbject, new()
        {
            /// <summary>
            /// 초기화 메서드에서 다른 Box를 의존 하지 않는 것이 원칙
            /// <para>Box들의 호출 순서가 무작위더라도, 정상적으로 작동해야함</para>
            /// </summary>
            public BaseBox(TSkelSbject skelSbject) : base(skelSbject) { SkelSbject = skelSbject; }

            protected readonly new TSkelSbject SkelSbject;
        }



        public abstract class BaseBoneBox<TSkelSbject> : BaseBoneBox where TSkelSbject : SkelSbject, new()
        {
            /// <summary>
            /// 초기화 메서드에서 다른 Box를 의존 하지 않는 것이 원칙
            /// <para>Box들의 호출 순서가 무작위더라도, 정상적으로 작동해야함</para>
            /// </summary>
            public BaseBoneBox(TSkelSbject skelSbject) : base(skelSbject) { }

            protected readonly new TSkelSbject SkelSbject;
        }



        /// <summary>
        /// 베이스가 되는 상호작용 박스
        /// </summary>
        public abstract class BaseInteractionBox<TSkelSbject> : BaseInteractionBox where TSkelSbject : SkelSbject, new()
        {
            protected BaseInteractionBox(SkelSbject skelSbject) : base(skelSbject) { }

            public sealed override AnimationInteractionManagerBase[] GetInteractionManagers => InteractionManagers;

            protected AnimationInteractionManagerBase<TSkelSbject>[] InteractionManagers;
        }



        ///======================================================================================================================================================



        //? 상호작용



        /// <summary>
        /// (IGNORE_BLEND) 제약조건 정책은 즉시 적용하고, 불연속 Plane 전환의 입력 Bone 회전만 보정한다.
        /// </summary>
        public class AnimationIgnoreBlendManager :
            SkelObject.Observers.IS.IAniStart,
            SkelObject.Observers.IS.IAniEnd
        {
            public AnimationIgnoreBlendManager(SkelSbject skelSbject)
            {
                SkelSbject = skelSbject;
                RuntimePosePreparedCallback = ApplyPoliciesBeforeWorld;
                BoneDepths = new int[skelSbject.Skeleton_Data.Bones.Count];
                for (int i = 0; i < BoneDepths.Length; i++)
                {
                    BoneDepths[i] = GetBoneDepth(skelSbject.Skeleton_Data.Bones.Items[i]);
                }
                CompareRotationRouteDepth = (left, right) =>
                    BoneDepths[left.OutputBoneIndex].CompareTo(BoneDepths[right.OutputBoneIndex]);
                skelSbject.Enable_SkelObjectEvent += SkelSbject_Enable_SkelObjectEvent;
                skelSbject.Disable_SkelObjectEvent += SkelSbject_Disable_SkelObjectEvent;

                HashSet<int> taggedConstraintIndexes = new();
                for (int i = 0; i < skelSbject.Skeleton_Data.Constraints.Count; i++)
                {
                    if (skelSbject.Skeleton_Data.Constraints.Items[i].Name.Contains(NAME_IGNORE_BLEND))
                    {
                        taggedConstraintIndexes.Add(i);
                    }
                }

                HashSet<int> taggedPolicyGraph = new(
                    CollectPolicyConstraintIndexes(
                        skelSbject.Skeleton_Data,
                        taggedConstraintIndexes));
                HashSet<int> discreteConstraintIndexes = new(taggedConstraintIndexes);
                for (int i = 0; i < skelSbject.Skeleton_Data.Constraints.Count; i++)
                {
                    if (skelSbject.Skeleton_Data.Constraints.Items[i] is not SliderData sliderData)
                    {
                        continue;
                    }

                    //. 입력 Bone이 없는 Slider는 렌더 반전이 아니라 다른 정책을 구동하는 제어 키일 수 있다.
                    if (!IsAutoScalePolicyCandidate(sliderData)) { continue; }

                    ScalePolicyKind scalePolicyKind = ClassifyScalePolicy(sliderData);
                    if (scalePolicyKind == ScalePolicyKind.PureReflection)
                    {
                        if (!taggedConstraintIndexes.Contains(i))
                        {
                            WarnOnce(
                                "untagged-reflection-" + sliderData.Name,
                                "[IGNORE_BLEND] Scale 반전 Slider가 정책 태그 없이 사용됩니다. " +
                                "원본 Spine 제약조건에 (IGNORE_BLEND)를 추가하세요: " +
                                sliderData.Name);
                        }
                    }
                    else if (scalePolicyKind == ScalePolicyKind.Ambiguous)
                    {
                        WarnOnce(
                            "ambiguous-scale-" + sliderData.Name,
                            "[IGNORE_BLEND] Scale과 다른 속성이 섞였거나 0 Scale 키가 있는 Slider는 " +
                            "자동 분류하지 않습니다: " + sliderData.Name);
                    }

                    if (!taggedPolicyGraph.Contains(i) &&
                        IsPotentialDiscontinuousRotationPolicy(
                            skelSbject.Skeleton_Data,
                            sliderData))
                    {
                        WarnOnce(
                            "untagged-rotation-policy-" + sliderData.Name,
                            "[IGNORE_BLEND] 180도 또는 반대 방향 회전 매핑을 전환하는 Slider가 " +
                            "정책 태그 그래프 밖에 있습니다. (IGNORE_BLEND) 적용 여부를 검토하세요: " +
                            sliderData.Name);
                    }
                }

                HashSet<Spine.Animation> sliderOwnedAnimations =
                    CollectSliderOwnedAnimations(skelSbject.Skeleton_Data);
                PolicyConstraintIndexes = CollectPolicyConstraintIndexes(
                    skelSbject.Skeleton_Data,
                    discreteConstraintIndexes);
                AnimationProfiles = new Dictionary<string, AnimationProfile>();

                for (int i = 0; i < skelSbject.AnimationEX_Dictionary.dataArray.Length; i++)
                {
                    CreateAnimationProfile(
                        skelSbject.AnimationEX_Dictionary.dataArray[i],
                        discreteConstraintIndexes,
                        sliderOwnedAnimations);
                }

                AnimationProfiles.TrimExcess();
            }



            private void CreateAnimationProfile(
                Spine.Animation sourceAnimation,
                IReadOnlyCollection<int> discreteConstraintIndexes,
                HashSet<Spine.Animation> sliderOwnedAnimations)
            {
                ExposedList<Timeline> policyTimelines = ExtractPolicyTimelines(
                    sourceAnimation,
                    discreteConstraintIndexes,
                    sliderOwnedAnimations);

                if (policyTimelines.Count == 0) { return; }

                Spine.Animation policyAnimation = new(sourceAnimation.Name + NAME_IGNORE_BLEND);
                policyAnimation.SetTimelines(policyTimelines, new ExposedList<int>());
                policyAnimation.Duration = sourceAnimation.Duration;

                HashSet<int> profileConstraintIndexes = new();
                for (int i = 0; i < policyTimelines.Count; i++)
                {
                    profileConstraintIndexes.Add(
                        ((IConstraintTimeline)policyTimelines.Items[i]).ConstraintIndex);
                }

                AnimationProfiles.Add(
                    sourceAnimation.Name,
                    new AnimationProfile(
                        sourceAnimation,
                        policyAnimation,
                        CollectRotationCompensationRoutes(
                            SkelSbject.Skeleton_Data,
                            profileConstraintIndexes),
                        CollectAnimationBoneRotationRoutes(
                            SkelSbject.Skeleton_Data,
                            sourceAnimation,
                            ((IConstraintTimeline)policyTimelines.Items[0])
                                .ConstraintIndex)));
            }



            internal static ExposedList<Timeline> ExtractPolicyTimelines(
                Spine.Animation sourceAnimation,
                IReadOnlyCollection<int> discreteConstraintIndexes,
                HashSet<Spine.Animation> sliderOwnedAnimations)
            {
                ExposedList<Timeline> policyTimelines = new();
                if (sourceAnimation == null ||
                    sliderOwnedAnimations.Contains(sourceAnimation))
                {
                    return policyTimelines;
                }

                List<Timeline> removeTimelines = new();
                foreach (Timeline timeline in sourceAnimation.Timelines)
                {
                    if (timeline is not IConstraintTimeline constraintTimeline ||
                        !ContainsConstraintIndex(
                            discreteConstraintIndexes,
                            constraintTimeline.ConstraintIndex))
                    {
                        continue;
                    }

                    policyTimelines.Add(timeline);
                    removeTimelines.Add(timeline);
                }

                for (int i = 0; i < removeTimelines.Count; i++)
                {
                    sourceAnimation.Timelines.Remove(removeTimelines[i]);
                }
                if (removeTimelines.Count > 0)
                {
                    sourceAnimation.SetTimelines(
                        sourceAnimation.Timelines,
                        sourceAnimation.Bones);
                }

                return policyTimelines;
            }



            internal static HashSet<Spine.Animation> CollectSliderOwnedAnimations(
                SkeletonData skeletonData)
            {
                HashSet<Spine.Animation> result = new();
                for (int i = 0; i < skeletonData.Constraints.Count; i++)
                {
                    if (skeletonData.Constraints.Items[i] is SliderData sliderData &&
                        sliderData.Animation != null)
                    {
                        result.Add(sliderData.Animation);
                    }
                }

                return result;
            }



            internal static ScalePolicyKind ClassifyScalePolicy(
                SliderData sliderData)
            {
                if (sliderData?.Animation == null ||
                    sliderData.Animation.Timelines.Count == 0)
                {
                    return ScalePolicyKind.None;
                }

                bool hasScaleTimeline = false;
                bool hasUnsupportedTimeline = false;
                Dictionary<int, List<Timeline>> timelinesByBone = new();
                SortedSet<float> sampleTimes = new() { 0 };

                foreach (Timeline timeline in sliderData.Animation.Timelines)
                {
                    if (timeline is not ScaleTimeline &&
                        timeline is not ScaleXTimeline &&
                        timeline is not ScaleYTimeline)
                    {
                        hasUnsupportedTimeline = true;
                        continue;
                    }

                    hasScaleTimeline = true;
                    int boneIndex = ((IBoneTimeline)timeline).BoneIndex;
                    if (!timelinesByBone.TryGetValue(
                        boneIndex,
                        out List<Timeline> boneTimelines))
                    {
                        boneTimelines = new List<Timeline>();
                        timelinesByBone.Add(boneIndex, boneTimelines);
                    }
                    boneTimelines.Add(timeline);

                    float[] frames = timeline.Frames;
                    for (int frame = 0; frame < frames.Length; frame += timeline.FrameEntries)
                    {
                        sampleTimes.Add(frames[frame]);
                    }
                }

                if (!hasScaleTimeline)
                {
                    return ScalePolicyKind.None;
                }
                if (hasUnsupportedTimeline || sliderData.Additive)
                {
                    return ScalePolicyKind.Ambiguous;
                }

                foreach (KeyValuePair<int, List<Timeline>> pair in timelinesByBone)
                {
                    foreach (float time in sampleTimes)
                    {
                        float scaleX = 1;
                        float scaleY = 1;

                        for (int i = 0; i < pair.Value.Count; i++)
                        {
                            Timeline timeline = pair.Value[i];
                            if (time < timeline.Frames[0]) { continue; }

                            switch (timeline)
                            {
                                case ScaleTimeline scaleTimeline:
                                    GetScaleTimelineValues(
                                        scaleTimeline,
                                        time,
                                        out scaleX,
                                        out scaleY);
                                    break;
                                case ScaleXTimeline scaleXTimeline:
                                    scaleX = scaleXTimeline.GetCurveValue(time);
                                    break;
                                case ScaleYTimeline scaleYTimeline:
                                    scaleY = scaleYTimeline.GetCurveValue(time);
                                    break;
                            }
                        }

                        float determinantScale = scaleX * scaleY;
                        if (Mathf.Abs(determinantScale) <= MATRIX_DETERMINANT_EPSILON)
                        {
                            return ScalePolicyKind.Ambiguous;
                        }
                        if (determinantScale < 0)
                        {
                            return ScalePolicyKind.PureReflection;
                        }
                    }
                }

                return ScalePolicyKind.None;
            }



            internal static bool IsAutoScalePolicyCandidate(
                SliderData sliderData)
            {
                return sliderData?.Bone != null;
            }



            internal static bool IsPotentialDiscontinuousRotationPolicy(
                SkeletonData skeletonData,
                SliderData sliderData)
            {
                if (sliderData?.Animation == null) { return false; }

                int sliderIndex = -1;
                for (int i = 0; i < skeletonData.Constraints.Count; i++)
                {
                    if (ReferenceEquals(
                        skeletonData.Constraints.Items[i],
                        sliderData))
                    {
                        sliderIndex = i;
                        break;
                    }
                }
                if (sliderIndex < 0) { return false; }

                int[] policyConstraintIndexes = CollectPolicyConstraintIndexes(
                    skeletonData,
                    new[] { sliderIndex });
                foreach (int constraintIndex in policyConstraintIndexes)
                {
                    if ((uint)constraintIndex >=
                            (uint)skeletonData.Constraints.Count ||
                        skeletonData.Constraints.Items[constraintIndex]
                            is not TransformConstraintData transform ||
                        !HasRotationMapping(transform))
                    {
                        continue;
                    }

                    if (Mathf.Abs(transform.OffsetRotation) >=
                        ROTATION_POLICY_EPSILON)
                    {
                        return true;
                    }

                    for (int propertyIndex = 0;
                        propertyIndex < transform.Properties.Count;
                        propertyIndex++)
                    {
                        if (transform.Properties.Items[propertyIndex]
                            is not TransformConstraintData.FromRotate fromRotate)
                        {
                            continue;
                        }

                        if (Mathf.Abs(fromRotate.offset) >=
                            ROTATION_POLICY_EPSILON)
                        {
                            return true;
                        }

                        for (int toIndex = 0;
                            toIndex < fromRotate.to.Count;
                            toIndex++)
                        {
                            if (fromRotate.to.Items[toIndex]
                                is TransformConstraintData.ToRotate toRotate &&
                                (Mathf.Abs(toRotate.offset) >=
                                    ROTATION_POLICY_EPSILON ||
                                    toRotate.scale < -ROTATION_APPLY_EPSILON))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }



            private static void GetScaleTimelineValues(
                ScaleTimeline timeline,
                float time,
                out float scaleX,
                out float scaleY)
            {
                float[] frames = timeline.Frames;
                int frameIndex = 0;
                for (int i = timeline.FrameEntries;
                    i < frames.Length && frames[i] <= time;
                    i += timeline.FrameEntries)
                {
                    frameIndex = i;
                }

                scaleX = frames[frameIndex + 1];
                scaleY = frames[frameIndex + 2];
            }



            private static bool ContainsConstraintIndex(
                IReadOnlyCollection<int> constraintIndexes,
                int targetIndex)
            {
                if (constraintIndexes is HashSet<int> hashSet)
                {
                    return hashSet.Contains(targetIndex);
                }

                foreach (int constraintIndex in constraintIndexes)
                {
                    if (constraintIndex == targetIndex) { return true; }
                }

                return false;
            }



            internal static int[] CollectPolicyConstraintIndexes(
                SkeletonData skeletonData,
                IReadOnlyCollection<int> taggedConstraintIndexes)
            {
                List<int> policyConstraintIndexes = new(taggedConstraintIndexes);
                HashSet<int> knownIndexes = new(taggedConstraintIndexes);

                //. Slider 내부 애니메이션이 다시 Slider를 키잉할 수 있으므로 순환을 막으며 재귀적으로 확장한다.
                for (int cursor = 0; cursor < policyConstraintIndexes.Count; cursor++)
                {
                    int constraintIndex = policyConstraintIndexes[cursor];
                    if ((uint)constraintIndex >= (uint)skeletonData.Constraints.Count ||
                        skeletonData.Constraints.Items[constraintIndex] is not SliderData sliderData ||
                        sliderData.Animation == null)
                    {
                        continue;
                    }

                    foreach (Timeline timeline in sliderData.Animation.Timelines)
                    {
                        if (timeline is IConstraintTimeline constraintTimeline &&
                            knownIndexes.Add(constraintTimeline.ConstraintIndex))
                        {
                            policyConstraintIndexes.Add(constraintTimeline.ConstraintIndex);
                        }
                    }
                }

                return policyConstraintIndexes.ToArray();
            }



            internal static RotationCompensationRoute[] CollectRotationCompensationRoutes(
                SkeletonData skeletonData,
                IReadOnlyCollection<int> constraintIndexes)
            {
                List<RotationCompensationRoute> routes = new();
                HashSet<string> knownRoutes = new();
                HashSet<int> activeRotationConstraintIndexes =
                    CollectPotentialRotationConstraintIndexes(skeletonData);
                int[] policyConstraintIndexes = CollectPolicyConstraintIndexes(
                    skeletonData,
                    constraintIndexes);
                HashSet<int> policyGraphIndexes = new(policyConstraintIndexes);

                foreach (int constraintIndex in policyConstraintIndexes)
                {
                    if ((uint)constraintIndex >= (uint)skeletonData.Constraints.Count ||
                        skeletonData.Constraints.Items[constraintIndex] is not SliderData sliderData ||
                        sliderData.Animation == null)
                    {
                        continue;
                    }

                    foreach (Timeline timeline in sliderData.Animation.Timelines)
                    {
                        if (timeline is not TransformConstraintTimeline transformTimeline ||
                            (uint)transformTimeline.ConstraintIndex >= (uint)skeletonData.Constraints.Count ||
                            skeletonData.Constraints.Items[transformTimeline.ConstraintIndex]
                                is not TransformConstraintData planeConstraint ||
                            planeConstraint.Source == null ||
                            !HasRotationMapping(planeConstraint))
                        {
                            continue;
                        }

                        HashSet<int> inputBoneIndexes = new();
                        CollectUpstreamRotationInputs(
                            skeletonData,
                            planeConstraint.Source.Index,
                            transformTimeline.ConstraintIndex,
                            activeRotationConstraintIndexes,
                            policyGraphIndexes,
                            new HashSet<long>(),
                            inputBoneIndexes);

                        if (inputBoneIndexes.Count == 0 || planeConstraint.Bones.Count == 0)
                        {
                            continue;
                        }

                        int[] inputs = new int[inputBoneIndexes.Count];
                        inputBoneIndexes.CopyTo(inputs);
                        Array.Sort(inputs);

                        HashSet<int> outputBoneIndexes = new();
                        for (int i = 0; i < planeConstraint.Bones.Count; i++)
                        {
                            CollectDownstreamRotationOutputs(
                                skeletonData,
                                planeConstraint.Bones.Items[i].Index,
                                transformTimeline.ConstraintIndex,
                                activeRotationConstraintIndexes,
                                new HashSet<long>(),
                                outputBoneIndexes);
                        }

                        foreach (int outputBoneIndex in outputBoneIndexes)
                        {
                            string routeKey = outputBoneIndex + ":" +
                                string.Join(",", inputs);
                            if (!knownRoutes.Add(routeKey)) { continue; }

                            routes.Add(new RotationCompensationRoute(
                                constraintIndex,
                                inputs,
                                outputBoneIndex));
                        }
                    }
                }

                return routes.ToArray();
            }



            internal static RotationCompensationRoute[] CollectAnimationBoneRotationRoutes(
                SkeletonData skeletonData,
                Spine.Animation animation,
                int policyConstraintIndex)
            {
                if (animation?.Bones == null || animation.Bones.Count == 0)
                {
                    return Array.Empty<RotationCompensationRoute>();
                }

                HashSet<int> activeRotationConstraintIndexes =
                    CollectPotentialRotationConstraintIndexes(skeletonData);
                List<RotationCompensationRoute> routes = new();
                HashSet<string> knownRoutes = new();

                for (int i = 0; i < animation.Bones.Count; i++)
                {
                    int inputBoneIndex = animation.Bones.Items[i];
                    HashSet<int> outputBoneIndexes = new();
                    CollectDownstreamRotationOutputs(
                        skeletonData,
                        inputBoneIndex,
                        -1,
                        activeRotationConstraintIndexes,
                        new HashSet<long>(),
                        outputBoneIndexes);

                    foreach (int outputBoneIndex in outputBoneIndexes)
                    {
                        string routeKey = inputBoneIndex + ":" + outputBoneIndex;
                        if (!knownRoutes.Add(routeKey)) { continue; }

                        routes.Add(new RotationCompensationRoute(
                            policyConstraintIndex,
                            new[] { inputBoneIndex },
                            outputBoneIndex));
                    }
                }

                return routes.ToArray();
            }



            private static void CollectDownstreamRotationOutputs(
                SkeletonData skeletonData,
                int boneIndex,
                int afterConstraintIndex,
                HashSet<int> activeRotationConstraintIndexes,
                HashSet<long> visited,
                HashSet<int> destination)
            {
                long visitKey = ((long)afterConstraintIndex << 32) |
                    (uint)boneIndex;
                if (!visited.Add(visitKey)) { return; }

                bool hasReader = false;
                for (int i = afterConstraintIndex + 1;
                    i < skeletonData.Constraints.Count;
                    i++)
                {
                    if (skeletonData.Constraints.Items[i]
                            is not TransformConstraintData transform ||
                        transform.Source == null ||
                        transform.Source.Index != boneIndex ||
                        !activeRotationConstraintIndexes.Contains(i))
                    {
                        continue;
                    }

                    hasReader = true;
                    for (int bone = 0; bone < transform.Bones.Count; bone++)
                    {
                        CollectDownstreamRotationOutputs(
                            skeletonData,
                            transform.Bones.Items[bone].Index,
                            i,
                            activeRotationConstraintIndexes,
                            visited,
                            destination);
                    }
                }

                if (!hasReader) { destination.Add(boneIndex); }
            }



            private static void CollectUpstreamRotationInputs(
                SkeletonData skeletonData,
                int boneIndex,
                int beforeConstraintIndex,
                HashSet<int> activeRotationConstraintIndexes,
                HashSet<int> policyGraphIndexes,
                HashSet<long> visited,
                HashSet<int> destination)
            {
                long visitKey = ((long)beforeConstraintIndex << 32) | (uint)boneIndex;
                if (!visited.Add(visitKey)) { return; }

                bool hasWriter = false;
                bool hasPolicyWriter = false;
                for (int i = 0; i < beforeConstraintIndex; i++)
                {
                    if (skeletonData.Constraints.Items[i]
                            is TransformConstraintData transform &&
                        transform.Source != null &&
                        activeRotationConstraintIndexes.Contains(i) &&
                        policyGraphIndexes.Contains(i) &&
                        ContainsBone(transform.Bones, boneIndex))
                    {
                        hasPolicyWriter = true;
                        break;
                    }
                }

                for (int i = 0; i < beforeConstraintIndex; i++)
                {
                    if (skeletonData.Constraints.Items[i] is not TransformConstraintData transform ||
                        transform.Source == null ||
                        !activeRotationConstraintIndexes.Contains(i) ||
                        (hasPolicyWriter && !policyGraphIndexes.Contains(i)) ||
                        !ContainsBone(transform.Bones, boneIndex))
                    {
                        continue;
                    }

                    hasWriter = true;
                    CollectUpstreamRotationInputs(
                        skeletonData,
                        transform.Source.Index,
                        i,
                        activeRotationConstraintIndexes,
                        policyGraphIndexes,
                        visited,
                        destination);
                }

                if (!hasWriter)
                {
                    destination.Add(boneIndex);
                }
            }



            private static bool HasRotationMapping(TransformConstraintData transform)
            {
                for (int i = 0; i < transform.Properties.Count; i++)
                {
                    if (transform.Properties.Items[i] is not TransformConstraintData.FromRotate rotate)
                    {
                        continue;
                    }

                    for (int toIndex = 0; toIndex < rotate.to.Count; toIndex++)
                    {
                        if (rotate.to.Items[toIndex] is TransformConstraintData.ToRotate)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }



            private static HashSet<int> CollectPotentialRotationConstraintIndexes(
                SkeletonData skeletonData)
            {
                HashSet<int> result = new();

                for (int i = 0; i < skeletonData.Constraints.Count; i++)
                {
                    if (skeletonData.Constraints.Items[i] is TransformConstraintData transform &&
                        HasRotationMapping(transform) &&
                        Mathf.Abs(transform.GetSetupPose().MixRotate) >=
                            ROTATION_APPLY_EPSILON)
                    {
                        result.Add(i);
                    }
                }

                for (int i = 0; i < skeletonData.Animations.Count; i++)
                {
                    AddAnimatedRotationConstraintIndexes(
                        skeletonData,
                        skeletonData.Animations.Items[i],
                        result);
                }

                for (int i = 0; i < skeletonData.Constraints.Count; i++)
                {
                    if (skeletonData.Constraints.Items[i] is SliderData slider &&
                        slider.Animation != null)
                    {
                        AddAnimatedRotationConstraintIndexes(
                            skeletonData,
                            slider.Animation,
                            result);
                    }
                }

                return result;
            }



            private static void AddAnimatedRotationConstraintIndexes(
                SkeletonData skeletonData,
                Spine.Animation animation,
                HashSet<int> destination)
            {
                foreach (Timeline timeline in animation.Timelines)
                {
                    if (timeline is TransformConstraintTimeline transformTimeline &&
                        (uint)transformTimeline.ConstraintIndex <
                            (uint)skeletonData.Constraints.Count &&
                        skeletonData.Constraints.Items[transformTimeline.ConstraintIndex]
                            is TransformConstraintData transform &&
                        HasRotationMapping(transform))
                    {
                        destination.Add(transformTimeline.ConstraintIndex);
                    }
                }
            }



            private static bool ContainsBone(
                ExposedList<BoneData> bones,
                int boneIndex)
            {
                for (int i = 0; i < bones.Count; i++)
                {
                    if (bones.Items[i].Index == boneIndex) { return true; }
                }

                return false;
            }



            private void SkelSbject_Enable_SkelObjectEvent(SkelObject skelObject)
            {
                //! 재활성화 뒤에도 Observer와 업데이트 콜백은 각각 한 번만 유지한다.
                skelObject.Observer.RemoveOB(this);
                skelObject.Observer.AddOB(this);

                ObjectState objectState = GetObjectState(skelObject);
                //. OnEnable의 Refresh_SkelObject가 이벤트를 비우므로 기존 상태도 다시 연결한다.
                objectState.CallbackSubscribed = false;
                EnsureSkeleton(skelObject, objectState);
            }



            private void SkelSbject_Disable_SkelObjectEvent(SkelObject skelObject)
            {
                skelObject.Observer.RemoveOB(this);

                if (!ObjectStates.TryGetValue(skelObject, out ObjectState objectState)) { return; }

                UnsubscribeCallbacks(skelObject, objectState);
                ObjectStates.Remove(skelObject);
            }



            /// <summary>
            /// 분리된 정책 애니메이션의 기존 공개 메모 키. 소스 호환성을 위해 유지한다.
            /// </summary>
            public const string MEMOEX_IGNORE_BLEND_ANI_TRACKINDEX = "IGB_TRACKINDEX";



            /// <summary>
            /// 기존 별도 정책 트랙 오프셋. 현재 직접 정책 적용 방식에서는 런타임 트랙을 만들지 않는다.
            /// </summary>
            public int IgnoreBlend_AddedTrackIndex = 1000;



            private const string NAME_IGNORE_BLEND = "(IGNORE_BLEND)";
            private const float ROTATION_POLICY_EPSILON = 0.1f;
            private const float ROTATION_SENSITIVITY_EPSILON = 0.05f;
            private const float ROTATION_PERTURBATION = 1f;
            private const float ROTATION_APPLY_EPSILON = 0.001f;
            private const float MATRIX_DETERMINANT_EPSILON = 0.000001f;



            internal enum ScalePolicyKind
            {
                None,
                PureReflection,
                Ambiguous
            }



            private readonly SkelSbject SkelSbject;
            private readonly Action<SkelObject> RuntimePosePreparedCallback;
            private readonly int[] BoneDepths;
            private readonly Comparison<RotationCompensationRoute> CompareRotationRouteDepth;
            /// <summary>
            /// 고정 프로필의 이전/현재 정책과 턴뷰 재지정 여부로 만들어진 경로 쌍만 공유한다.
            /// 매번 고정 프로필 경로부터 병합하므로 중간 결과도 유한한 조합에 한정되며,
            /// 같은 전환을 반복해도 이전 전환 결과를 이어 붙이는 캐시 체인은 생기지 않는다.
            /// 포즈·캐릭터 참조는 저장하지 않으며 SkeletonData 재구성 시 매니저와 함께 폐기한다.
            /// </summary>
            private readonly Dictionary<(RotationCompensationRoute, RotationCompensationRoute),
                RotationCompensationRoute> MergedRotationRoutes = new();
            private readonly int[] PolicyConstraintIndexes;
            private readonly Dictionary<string, AnimationProfile> AnimationProfiles;
            private readonly Dictionary<SkelObject, ObjectState> ObjectStates = new();
            private readonly HashSet<string> LoggedWarnings = new();



            public void AlarmThe_AniStart(
                SkelObject skelObject,
                SkelObject.AniCore.Track track)
            {
                ObjectState objectState = GetObjectState(skelObject);
                EnsureSkeleton(skelObject, objectState);

                int sourceTrackIndex = ResolveTrackIndex(skelObject, track);
                AnimationProfiles.TryGetValue(
                    track.PlayingSkelAni.Animation.Name,
                    out AnimationProfile targetProfile);

                objectState.ActivePolicies.TryGetValue(
                    sourceTrackIndex,
                    out ActivePolicy currentPolicy);

                TrackEntry sourceEntry =
                    skelObject.Skel?.GetSkeletonAnimation()?.AnimationState.GetTrack(sourceTrackIndex);

                bool policyChanged = currentPolicy != null || targetProfile != null;
                if (!policyChanged)
                {
                    RequestPoseRetarget(
                        objectState,
                        track.PlayingSkelAni.Animation,
                        sourceTrackIndex);
                    return;
                }

                PrepareTransitionSource(objectState);

                int generation = objectState.NextGeneration(sourceTrackIndex);
                if (targetProfile == null)
                {
                    objectState.ActivePolicies.Remove(sourceTrackIndex);
                }
                else
                {
                    objectState.ActivePolicies[sourceTrackIndex] = new ActivePolicy(
                        sourceTrackIndex,
                        generation,
                        targetProfile,
                        sourceEntry);
                }
                objectState.RebuildSortedPolicies();

                StartTransition(
                    objectState,
                    sourceTrackIndex,
                    sourceEntry?.MixDuration ?? track.PlayingSkelAni.MixDuration);
            }



            public void AlarmThe_AniEnd(
                SkelObject.AniCore.Track track,
                TrackEntry trackEntry,
                bool fromAnimationStateComplete)
            {
                if (trackEntry == null ||
                    !TryResolveSkelObject(track, trackEntry, out SkelObject skelObject) ||
                    !ObjectStates.TryGetValue(skelObject, out ObjectState objectState))
                {
                    return;
                }

                if (!objectState.ActivePolicies.TryGetValue(
                    trackEntry.TrackIndex,
                    out ActivePolicy activePolicy))
                {
                    RequestPoseRetarget(objectState, trackEntry.Animation, trackEntry.TrackIndex);
                    return;
                }

                EndAnimation(
                    skelObject,
                    trackEntry.TrackIndex,
                    activePolicy.Generation,
                    track.PlayingSkelAni.EndMixDuration);
            }



            private void RequestPoseRetarget(
                ObjectState objectState,
                Spine.Animation animation,
                int trackIndex)
            {
                if (!AnimationChangesTransformPose(animation) ||
                    !objectState.Transition.Active ||
                    trackIndex == objectState.Transition.SourceTrackIndex)
                {
                    return;
                }

                if (!objectState.PoseRetargetTrackIndexes.Contains(trackIndex))
                {
                    objectState.PoseRetargetTrackIndexes.Add(trackIndex);
                }
            }


            internal static bool AnimationChangesTransformPose(
                Spine.Animation animation)
            {
                if (animation?.Timelines == null) { return false; }

                foreach (Timeline timeline in animation.Timelines)
                {
                    if (timeline is IBoneTimeline ||
                        timeline is IConstraintTimeline)
                    {
                        return true;
                    }
                }

                return false;
            }



            private bool TryResolveSkelObject(
                SkelObject.AniCore.Track track,
                TrackEntry trackEntry,
                out SkelObject result)
            {
                foreach (KeyValuePair<SkelObject, ObjectState> pair in ObjectStates)
                {
                    if (ReferenceEquals(
                        pair.Value.AnimationState?.GetTrack(trackEntry.TrackIndex),
                        trackEntry) ||
                        pair.Key.Ani.Player.GetPlayingAnimationTracks.TryGetValue(
                            trackEntry.TrackIndex,
                            out SkelObject.AniCore.Track candidate) &&
                        ReferenceEquals(candidate, track))
                    {
                        result = pair.Key;
                        return true;
                    }
                }

                result = null;
                return false;
            }



            private void EndAnimation(
                SkelObject skelObject,
                int sourceTrackIndex,
                int generation,
                float endMixDuration)
            {
                if (!ObjectStates.TryGetValue(skelObject, out ObjectState objectState) ||
                    !objectState.Generations.TryGetValue(sourceTrackIndex, out int currentGeneration) ||
                    currentGeneration != generation)
                {
                    return;
                }

                EnsureSkeleton(skelObject, objectState);
                PrepareTransitionSource(objectState);

                if (objectState.ActivePolicies.TryGetValue(sourceTrackIndex, out ActivePolicy activePolicy) &&
                    activePolicy.Generation == generation)
                {
                    objectState.ActivePolicies.Remove(sourceTrackIndex);
                    objectState.RebuildSortedPolicies();
                }

                StartTransition(
                    objectState,
                    sourceTrackIndex,
                    endMixDuration);
            }



            private void PrepareTransitionSource(ObjectState objectState)
            {
                bool hasRenderedPose = objectState.Skeleton != null;
                if (hasRenderedPose)
                {
                    EnsureRotationBuffers(objectState);
                    CaptureAllBoneRotations(
                        objectState.Skeleton,
                        objectState.FrozenSourceRotations,
                        objectState.FrozenSourceLocalRotations);
                }

                //. 새 전환은 직전 렌더 회전을 시작점으로 다시 분기를 선택한다.
                objectState.PersistentWindingOffsets.Clear();
                objectState.PersistentWindingRoutes.Clear();
                objectState.PersistentWindingSamples.Clear();
                objectState.WindingNormalizationPending = false;
                objectState.LastAppliedRotations.Clear();

                //. End 직후 같은 프레임에 다음 애니메이션이 시작되면,
                //. 아직 평가되지 않은 이전 정책 스택을 새 전환의 시작점으로 이어간다.
                bool preservePendingSourcePolicies =
                    ShouldPreservePendingSourcePolicies(
                    objectState.Transition.Active,
                    objectState.Transition.FirstFrame,
                    objectState.Transition.PreparedFrame,
                    objectState.Transition.PreviousPolicies.Count);
                objectState.Transition.Reset(preservePendingSourcePolicies);
                //. 정책이 바뀌기 전 마지막 렌더 회전을 새 Plane의 입력 회전 보정 목표로 사용한다.
                objectState.Transition.HasFrozenSource = hasRenderedPose;
                if (preservePendingSourcePolicies) { return; }

                //? 종료 자세의 입력과 정책을 함께 보관한다. 턴뷰가 바뀔 때만 이 포즈를 다시 평가한다.
                objectState.PoseRetargetTrackIndexes.Clear();
                objectState.SourcePoseTracks.Clear();
                if (hasRenderedPose)
                {
                    objectState.SourcePoseSkeleton ??= new Skeleton(objectState.Skeleton);
                    CopyCurrentPose(objectState.Skeleton, objectState.SourcePoseSkeleton);
                    ExposedList<TrackEntry> tracks = objectState.AnimationState.Tracks;
                    for (int i = 0; i < tracks.Count; i++)
                    {
                        if (tracks.Items[i] != null)
                        {
                            objectState.SourcePoseTracks[i] = tracks.Items[i].Animation;
                        }
                    }
                }

                for (int i = 0; i < objectState.SortedPolicies.Count; i++)
                {
                    objectState.Transition.PreviousPolicies.Add(
                        objectState.SortedPolicies[i].CreateSample());
                }
            }



            internal static bool ShouldPreservePendingSourcePolicies(
                bool active,
                bool firstFrame,
                int preparedFrame,
                int previousPolicyCount)
            {
                return active &&
                    firstFrame &&
                    preparedFrame < 0 &&
                    previousPolicyCount > 0;
            }



            private void StartTransition(
                ObjectState objectState,
                int sourceTrackIndex,
                float duration)
            {
                if (duration <= 0 ||
                    objectState.Skeleton == null)
                {
                    objectState.Transition.Reset();
                    return;
                }

                EnsureComparisonSkeleton(objectState);
                objectState.Transition.Active = true;
                objectState.Transition.FirstFrame = true;
                objectState.Transition.SourceTrackIndex = sourceTrackIndex;
                objectState.Transition.Duration = duration;
                objectState.Transition.Elapsed = 0;
                objectState.Transition.Alpha = 0;
                RebuildRotationRoutes(objectState);

                if (objectState.Transition.Routes.Count == 0)
                {
                    objectState.Transition.Reset();
                    return;
                }
            }



            /// <summary>
            /// 정책 전환 또는 턴뷰 재지정 때만 경로와 입력 연결을 갱신한다.
            /// 뼈 깊이와 병합 경로는 공유하지만 전환 버퍼는 캐릭터별로 재사용한다.
            /// </summary>
            private void RebuildRotationRoutes(ObjectState objectState)
            {
                TransitionState transition = objectState.Transition;
                transition.Routes.Clear();

                for (int i = 0; i < transition.PreviousPolicies.Count; i++)
                {
                    if (transition.PreviousPolicies[i].TrackIndex !=
                        transition.SourceTrackIndex)
                    {
                        continue;
                    }

                    AddRotationRoutes(
                        transition.PreviousPolicies[i].Profile.RotationRoutes,
                        transition.Routes);
                    if (transition.UsePoseRetargetRoutes)
                    {
                        AddRotationRoutes(
                            transition.PreviousPolicies[i].Profile.PoseRetargetRoutes,
                            transition.Routes);
                    }
                }
                for (int i = 0; i < objectState.SortedPolicies.Count; i++)
                {
                    if (objectState.SortedPolicies[i].TrackIndex !=
                        transition.SourceTrackIndex)
                    {
                        continue;
                    }

                    AddRotationRoutes(
                        objectState.SortedPolicies[i].Profile.RotationRoutes,
                        transition.Routes);
                    if (transition.UsePoseRetargetRoutes)
                    {
                        AddRotationRoutes(
                            objectState.SortedPolicies[i].Profile.PoseRetargetRoutes,
                            transition.Routes);
                    }
                }

                transition.Routes.Sort(CompareRotationRouteDepth);
                transition.HasSharedInputs = HasSharedRotationInputs(transition.Routes);
                List<int> inputBoneIndexes = transition.CoupledInputBoneIndexes;
                inputBoneIndexes.Clear();
                for (int i = 0; i < transition.Routes.Count; i++)
                {
                    int[] inputs = transition.Routes[i].InputBoneIndexes;
                    for (int j = 0; j < inputs.Length; j++)
                    {
                        if (!inputBoneIndexes.Contains(inputs[j]))
                        {
                            inputBoneIndexes.Add(inputs[j]);
                        }
                    }
                }
                inputBoneIndexes.Sort();
            }



            private static int GetBoneDepth(BoneData bone)
            {
                int depth = 0;
                while (bone.Parent != null)
                {
                    depth++;
                    bone = bone.Parent;
                }
                return depth;
            }



            private void AddRotationRoutes(
                RotationCompensationRoute[] source,
                List<RotationCompensationRoute> destination)
            {
                for (int i = 0; i < source.Length; i++)
                {
                    RotationCompensationRoute route = source[i];
                    int existingIndex = -1;
                    for (int destinationIndex = 0;
                        destinationIndex < destination.Count;
                        destinationIndex++)
                    {
                        if (destination[destinationIndex].OutputBoneIndex != route.OutputBoneIndex)
                        {
                            continue;
                        }

                        existingIndex = destinationIndex;
                        break;
                    }

                    if (existingIndex < 0)
                    {
                        destination.Add(route);
                        continue;
                    }

                    RotationCompensationRoute existing = destination[existingIndex];
                    if (!MergedRotationRoutes.TryGetValue((existing, route), out RotationCompensationRoute merged))
                    {
                        merged = RotationCompensationRoute.Merge(existing, route);
                        MergedRotationRoutes.Add((existing, route), merged);
                    }
                    destination[existingIndex] = merged;
                }
            }



            private void ApplyPoliciesBeforeWorld(SkelObject skelObject)
            {
                if (!ObjectStates.TryGetValue(skelObject, out ObjectState objectState))
                {
                    return;
                }

                EnsureSkeleton(skelObject, objectState);
                Skeleton skeleton = objectState.Skeleton;
                if (skeleton == null) { return; }

                RestoreAppliedRotationCorrections(objectState);
                RetargetTransitionAfterPoseChange(objectState);

                if (objectState.Transition.Active)
                {
                    if (objectState.Transition.PreparedFrame != Time.frameCount)
                    {
                        objectState.Transition.PreparedFrame = Time.frameCount;
                        objectState.Transition.Alpha =
                            ResolveTransitionAlpha(objectState);
                    }

                    SolveRotationCorrections(objectState);
                }
                else if (objectState.WindingNormalizationPending)
                {
                    TryNormalizePersistentWindings(objectState);
                }

                ResetPolicyConstraintPoses(skeleton);
                ApplyActivePolicies(skeleton, objectState.SortedPolicies);
                if (objectState.Transition.Active)
                {
                    ApplyRotationCorrections(objectState);
                }
                else
                {
                    ApplyPersistentWindingOffsets(objectState);
                }
                AdvanceTransition(objectState);
            }



            private void RetargetTransitionAfterPoseChange(ObjectState objectState)
            {
                if (objectState.PoseRetargetTrackIndexes.Count == 0) { return; }

                TransitionState transition = objectState.Transition;
                Skeleton source = objectState.SourcePoseSkeleton;
                if (transition.Active && source != null)
                {
                    objectState.PoseRetargetTrackIndexes.Sort();
                    foreach (int trackIndex in objectState.PoseRetargetTrackIndexes)
                    {
                        objectState.SourcePoseTracks.TryGetValue(trackIndex, out Spine.Animation previous);
                        TrackEntry entry = objectState.AnimationState.GetTrack(trackIndex);
                        ApplyPoseRetargetTrack(source, previous, entry);
                        if (entry == null)
                        {
                            objectState.SourcePoseTracks.Remove(trackIndex);
                            continue;
                        }
                        objectState.SourcePoseTracks[trackIndex] = entry.Animation;
                    }

                    //. 출발 포즈에는 종료 모션의 관절별 정책이 남아 있다.
                    //. 도착 포즈는 기존 solver가 현재 정책으로 평가하므로 두 정책을 섞어 덮어쓰지 않는다.
                    source.UpdateWorldTransform(Spine.Physics.Pose);
                    CaptureAllBoneRotations(source,
                        objectState.FrozenSourceRotations, objectState.FrozenSourceLocalRotations);
                    objectState.LastAppliedRotations.Clear();
                    transition.ResetRotationState();
                    transition.UsePoseRetargetRoutes = true;
                    RebuildRotationRoutes(objectState);
                }
                objectState.PoseRetargetTrackIndexes.Clear();
            }


            internal static void ApplyPoseRetargetTrack(
                Skeleton source,
                Spine.Animation previous,
                TrackEntry entry)
            {
                //? 이전 트랙에서만 키잉된 속성도 해제한다. 다른 관절의 Plane 정책은 그대로 둔다.
                previous?.Apply(source, 0, -1, false, null, 1, MixFrom.Setup, false, false, false);
                if (entry == null) { return; }
                float time = entry.Reverse
                    ? entry.Animation.Duration - entry.AnimationTime
                    : entry.AnimationTime;
                entry.Animation.Apply(source, time, time, entry.Loop, null,
                    entry.Alpha, MixFrom.Current, entry.Additive, false, false);
            }


            private void SolveRotationCorrections(ObjectState objectState)
            {
                TransitionState transition = objectState.Transition;
                transition.Corrections.Clear();
                transition.RejectedInputBones.Clear();

                if (!transition.HasFrozenSource ||
                    transition.Routes.Count == 0 ||
                    objectState.Skeleton == null)
                {
                    transition.Reset();
                    return;
                }

                EnsureRouteStateBuffers(transition);
                if (!transition.BranchesInitialized)
                {
                    InitializeDestinationBranches(objectState);
                    transition.BranchesInitialized = true;
                }

                //. 턴뷰 변경처럼 좌우 입력 매핑이 동시에 바뀌면 여러 출력이 같은 입력 Bone을 공유한다.
                //. 이 경우 출력별 탐욕 해법은 서로의 보정을 덮으므로 한 번에 최소 회전 해를 구한다.
                if ((transition.UsePoseRetargetRoutes ||
                        transition.HasSharedInputs) &&
                    TrySolveCoupledRotationCorrections(objectState))
                {
                    return;
                }

                //? 모든 목적지는 보정 전의 같은 포즈에서 읽는다. 부모 보정이 자식 목적지에 섞이면 회전 분기가 달라진다.
                EvaluateActivePolicyPose(objectState);
                for (int routeIndex = 0; routeIndex < transition.Routes.Count; routeIndex++)
                {
                    int outputBoneIndex = transition.Routes[routeIndex].OutputBoneIndex;
                    if (TryCreateRotationSample(
                        objectState.ComparisonSkeleton.Bones.Items[outputBoneIndex],
                        out RotationSample destinationSample))
                    {
                        transition.DynamicDestinationRotations[routeIndex] = GetContinuousDestinationRotation(
                            objectState.FrozenSourceRotations[outputBoneIndex].Rotation,
                            transition.DynamicDestinationRotations[routeIndex],
                            destinationSample.Rotation);
                    }
                }

                for (int routeIndex = 0; routeIndex < transition.Routes.Count; routeIndex++)
                {
                    RotationCompensationRoute route = transition.Routes[routeIndex];
                    //. 첫 출력은 바로 위에서 평가한 보정 전 포즈를 그대로 읽는다.
                    if (routeIndex != 0) { EvaluateActivePolicyPose(objectState); }
                    if (!TryCreateRotationSample(
                        objectState.ComparisonSkeleton.Bones
                            .Items[route.OutputBoneIndex],
                        out RotationSample activePolicySample) ||
                        !objectState.FrozenSourceRotations[route.OutputBoneIndex].Valid)
                    {
                        WarnOnce(
                            "rotation-singular-" + route.PolicyConstraintIndex + "-" +
                            route.OutputBoneIndex,
                            "[IGNORE_BLEND] Plane 정책 출력 행렬이 특이 상태라 회전 보정을 생략합니다: " +
                            SkelSbject.Skeleton_Data.Constraints.Items[route.PolicyConstraintIndex].Name);
                        continue;
                    }

                    float sourceRotation = objectState
                        .FrozenSourceRotations[route.OutputBoneIndex].Rotation;
                    float destinationRotation = transition.DynamicDestinationRotations[routeIndex];
                    if (float.IsNaN(destinationRotation)) { continue; }
                    float sourceBranch = GetStableSourceBranch(
                        sourceRotation,
                        destinationRotation,
                        transition.SourceBranchRotations[routeIndex]);
                    transition.SourceBranchRotations[routeIndex] = sourceBranch;
                    RotationBlendState blendState = transition.RotationBlendStates[routeIndex];
                    float targetRotation = GetContinuousRotationTarget(
                        sourceBranch,
                        destinationRotation,
                        transition.Alpha,
                        ref blendState);
                    transition.RotationBlendStates[routeIndex] = blendState;
                    float currentRotation = UnwrapRotationNear(
                        targetRotation,
                        activePolicySample.Rotation);
                    if (Mathf.Abs(currentRotation - targetRotation) <=
                        ROTATION_POLICY_EPSILON)
                    {
                        continue;
                    }

                    int selectedInputBoneIndex =
                        transition.SelectedInputBoneIndexes[routeIndex];
                    bool hasFixedWindingOffset = selectedInputBoneIndex >= 0;

                    if (!TrySolveRotationCorrection(
                        objectState,
                        route,
                        targetRotation,
                        destinationRotation,
                        selectedInputBoneIndex,
                        hasFixedWindingOffset,
                        transition.WindingOffsets[routeIndex],
                        out RotationCorrection correction))
                    {
                        WarnOnce(
                            "rotation-unsolved-" + route.PolicyConstraintIndex + "-" +
                            route.OutputBoneIndex,
                            "[IGNORE_BLEND] Plane 정책의 입력 회전 보정 경로를 풀 수 없어 정책만 즉시 적용합니다: " +
                            SkelSbject.Skeleton_Data.Constraints.Items[route.PolicyConstraintIndex].Name);
                        continue;
                    }

                    transition.SelectedInputBoneIndexes[routeIndex] =
                        correction.InputBoneIndex;
                    if (!hasFixedWindingOffset)
                    {
                        transition.WindingOffsets[routeIndex] =
                            correction.WindingOffset;
                    }
                    AddRotationCorrection(transition, route, correction);
                }
            }



            private void InitializeDestinationBranches(ObjectState objectState)
            {
                TransitionState transition = objectState.Transition;
                Skeleton destination = objectState.ComparisonSkeleton;
                CopyCurrentPose(objectState.Skeleton, destination);
                destination.SetupPose();
                ExposedList<TrackEntry> tracks = objectState.AnimationState.Tracks;
                for (int trackIndex = 0; trackIndex < tracks.Count; trackIndex++)
                {
                    TrackEntry entry = tracks.Items[trackIndex];
                    if (entry == null || entry.Delay > 0) { continue; }
                    float animationTime = entry.Reverse
                        ? entry.Animation.Duration - entry.AnimationTime
                        : entry.AnimationTime;
                    entry.Animation.Apply(
                        destination, animationTime, animationTime,
                        entry.Loop, null, entry.Alpha,
                        trackIndex == 0 ? MixFrom.Setup : MixFrom.Current,
                        entry.Additive, false, false);
                }
                ResetPolicyConstraintPoses(destination);
                ApplyActivePolicies(destination, objectState.SortedPolicies);
                destination.UpdateWorldTransform(Spine.Physics.Pose);

                //? 현재 Mix 포즈는 Plane 반전 때문에 반대쪽 반원에서 출발할 수 있다.
                //? 실제 도착 애니메이션으로 분기만 정하고, 이후에는 매 프레임의 연속 목적지를 그대로 따른다.
                for (int i = 0; i < transition.Routes.Count; i++)
                {
                    int outputIndex = transition.Routes[i].OutputBoneIndex;
                    if (!TryCreateRotationSample(destination.Bones.Items[outputIndex], out RotationSample sample))
                    {
                        continue;
                    }
                    transition.DynamicDestinationRotations[i] = sample.Rotation;
                    transition.SourceBranchRotations[i] = UnwrapRotationNear(
                        sample.Rotation, objectState.FrozenSourceRotations[outputIndex].Rotation);
                }
            }



            private bool TrySolveCoupledRotationCorrections(
                ObjectState objectState)
            {
                TransitionState transition = objectState.Transition;
                List<int> inputBoneIndexes = transition.CoupledInputBoneIndexes;
                if (inputBoneIndexes.Count == 0) { return false; }

                int outputCount = transition.Routes.Count;
                EnsureCoupledBuffers(
                    transition,
                    outputCount,
                    inputBoneIndexes.Count);
                float[] activeRotations = transition.CoupledActiveRotations;
                float[] targetRotations = transition.CoupledTargetRotations;
                float[] requiredDeltas = transition.CoupledRequiredDeltas;
                bool[] validOutputs = transition.CoupledValidOutputs;
                float[,] sensitivities = transition.CoupledSensitivities;
                Array.Clear(validOutputs, 0, outputCount);
                Array.Clear(
                    sensitivities,
                    0,
                    sensitivities.Length);

                transition.Corrections.Clear();
                EvaluateActivePolicyPose(objectState);
                for (int routeIndex = 0; routeIndex < outputCount; routeIndex++)
                {
                    RotationCompensationRoute route = transition.Routes[routeIndex];
                    if (!TryCreateRotationSample(
                            objectState.ComparisonSkeleton.Bones
                                .Items[route.OutputBoneIndex],
                            out RotationSample destinationSample) ||
                        !objectState.FrozenSourceRotations[route.OutputBoneIndex].Valid)
                    {
                        continue;
                    }

                    float sourceRotation = objectState
                        .FrozenSourceRotations[route.OutputBoneIndex].Rotation;
                    float destinationRotation = GetContinuousDestinationRotation(
                        sourceRotation,
                        transition.DynamicDestinationRotations[routeIndex],
                        destinationSample.Rotation);
                    transition.DynamicDestinationRotations[routeIndex] =
                        destinationRotation;
                    float sourceBranch = GetStableSourceBranch(
                        sourceRotation,
                        destinationRotation,
                        transition.SourceBranchRotations[routeIndex]);
                    transition.SourceBranchRotations[routeIndex] = sourceBranch;
                    RotationBlendState blendState = transition.RotationBlendStates[routeIndex];
                    float targetRotation = GetContinuousRotationTarget(
                        sourceBranch,
                        destinationRotation,
                        transition.Alpha,
                        ref blendState);
                    transition.RotationBlendStates[routeIndex] = blendState;

                    targetRotations[routeIndex] = targetRotation;
                    validOutputs[routeIndex] = true;
                }

                SetPreviousCoupledRotationCorrections(
                    transition,
                    inputBoneIndexes);
                //. 이전 보정이 없으면 목적지 평가와 입력이 동일하다. 보정이 있으면 반드시 재평가한다.
                if (transition.Corrections.Count != 0) { EvaluateActivePolicyPose(objectState); }
                for (int routeIndex = 0; routeIndex < outputCount; routeIndex++)
                {
                    if (!validOutputs[routeIndex] ||
                        !TryCreateRotationSample(
                            objectState.ComparisonSkeleton.Bones.Items[
                                transition.Routes[routeIndex].OutputBoneIndex],
                            out RotationSample activeSample))
                    {
                        validOutputs[routeIndex] = false;
                        continue;
                    }

                    activeRotations[routeIndex] = activeSample.Rotation;
                    requiredDeltas[routeIndex] = Mathf.DeltaAngle(
                        activeSample.Rotation,
                        targetRotations[routeIndex]);
                }

                for (int inputIndex = 0; inputIndex < inputBoneIndexes.Count; inputIndex++)
                {
                    EvaluateActivePolicyPose(
                        objectState,
                        inputBoneIndexes[inputIndex],
                        ROTATION_PERTURBATION);
                    for (int routeIndex = 0; routeIndex < outputCount; routeIndex++)
                    {
                        if (!validOutputs[routeIndex] ||
                            !TryCreateRotationSample(
                                objectState.ComparisonSkeleton.Bones.Items[
                                    transition.Routes[routeIndex].OutputBoneIndex],
                                out RotationSample perturbedSample))
                        {
                            continue;
                        }

                        sensitivities[routeIndex, inputIndex] =
                            Mathf.DeltaAngle(
                                activeRotations[routeIndex],
                                perturbedSample.Rotation) /
                            ROTATION_PERTURBATION;
                    }
                }

                if (!TrySolveLeastSquares(
                    sensitivities,
                    requiredDeltas,
                    outputCount,
                    inputBoneIndexes.Count,
                    transition.CoupledOffsets,
                    transition.CoupledNormalMatrix))
                {
                    return false;
                }
                float[] offsets = transition.CoupledOffsets;
                for (int i = 0; i < inputBoneIndexes.Count; i++)
                {
                    if (transition.CoupledOffsetBranches.TryGetValue(
                        inputBoneIndexes[i],
                        out float previousOffset))
                    {
                        offsets[i] = AccumulateCoupledRotationOffset(
                            previousOffset,
                            offsets[i]);
                    }
                }

                SetCoupledRotationCorrections(
                    transition,
                    inputBoneIndexes,
                    offsets);
                EvaluateActivePolicyPose(objectState);

                float[] residuals = transition.CoupledResiduals;
                bool needsRefinement = false;
                for (int routeIndex = 0; routeIndex < outputCount; routeIndex++)
                {
                    if (!validOutputs[routeIndex] ||
                        !TryCreateRotationSample(
                            objectState.ComparisonSkeleton.Bones.Items[
                                transition.Routes[routeIndex].OutputBoneIndex],
                            out RotationSample solvedSample))
                    {
                        continue;
                    }

                    residuals[routeIndex] = Mathf.DeltaAngle(
                        solvedSample.Rotation,
                        targetRotations[routeIndex]);
                    needsRefinement |= Mathf.Abs(residuals[routeIndex]) >
                        ROTATION_POLICY_EPSILON;
                }

                if (needsRefinement &&
                    TrySolveLeastSquares(
                        sensitivities,
                        residuals,
                        outputCount,
                        inputBoneIndexes.Count,
                        transition.CoupledRefinement,
                        transition.CoupledNormalMatrix))
                {
                    float[] refinement = transition.CoupledRefinement;
                    for (int i = 0; i < inputBoneIndexes.Count; i++)
                    {
                        offsets[i] += refinement[i];
                    }
                    SetCoupledRotationCorrections(
                        transition,
                        inputBoneIndexes,
                        offsets);
                    EvaluateActivePolicyPose(objectState);
                }

                SetContinuousCoupledRotationCorrections(
                    transition,
                    inputBoneIndexes,
                    offsets);
                return true;
            }



            private static bool HasSharedRotationInputs(
                IReadOnlyList<RotationCompensationRoute> routes)
            {
                for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
                {
                    int[] inputs = routes[routeIndex].InputBoneIndexes;
                    for (int inputIndex = 0; inputIndex < inputs.Length; inputIndex++)
                    {
                        for (int previousRoute = 0;
                            previousRoute < routeIndex;
                            previousRoute++)
                        {
                            if (Array.IndexOf(
                                routes[previousRoute].InputBoneIndexes,
                                inputs[inputIndex]) >= 0)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }



            private static void EnsureCoupledBuffers(
                TransitionState transition,
                int outputCount,
                int inputCount)
            {
                if (transition.CoupledActiveRotations.Length < outputCount)
                {
                    transition.CoupledActiveRotations = new float[outputCount];
                    transition.CoupledTargetRotations = new float[outputCount];
                    transition.CoupledRequiredDeltas = new float[outputCount];
                    transition.CoupledResiduals = new float[outputCount];
                    transition.CoupledValidOutputs = new bool[outputCount];
                }
                if (transition.CoupledOffsets.Length < inputCount)
                {
                    transition.CoupledOffsets = new float[inputCount];
                    transition.CoupledRefinement = new float[inputCount];
                }
                if (transition.CoupledSensitivities.GetLength(0) < outputCount ||
                    transition.CoupledSensitivities.GetLength(1) < inputCount)
                {
                    transition.CoupledSensitivities =
                        new float[outputCount, inputCount];
                }
                if (transition.CoupledNormalMatrix.GetLength(0) < inputCount ||
                    transition.CoupledNormalMatrix.GetLength(1) < inputCount + 1)
                {
                    transition.CoupledNormalMatrix =
                        new double[inputCount, inputCount + 1];
                }
            }



            private static void SetCoupledRotationCorrections(
                TransitionState transition,
                IReadOnlyList<int> inputBoneIndexes,
                IReadOnlyList<float> offsets)
            {
                transition.Corrections.Clear();
                for (int i = 0; i < inputBoneIndexes.Count; i++)
                {
                    if (Mathf.Abs(offsets[i]) <= ROTATION_APPLY_EPSILON)
                    {
                        continue;
                    }

                    transition.Corrections.Add(new RotationCorrection(
                        inputBoneIndexes[i],
                        0,
                        offsets[i]));
                }
            }



            private static void SetPreviousCoupledRotationCorrections(
                TransitionState transition,
                IReadOnlyList<int> inputBoneIndexes)
            {
                transition.Corrections.Clear();
                for (int i = 0; i < inputBoneIndexes.Count; i++)
                {
                    int inputBoneIndex = inputBoneIndexes[i];
                    if (!transition.CoupledOffsetBranches.TryGetValue(
                            inputBoneIndex,
                            out float offset) ||
                        Mathf.Abs(offset) <= ROTATION_APPLY_EPSILON)
                    {
                        continue;
                    }

                    transition.Corrections.Add(new RotationCorrection(
                        inputBoneIndex,
                        0,
                        offset));
                }
            }



            private static void SetContinuousCoupledRotationCorrections(
                TransitionState transition,
                IReadOnlyList<int> inputBoneIndexes,
                IReadOnlyList<float> offsets)
            {
                transition.Corrections.Clear();
                for (int i = 0; i < inputBoneIndexes.Count; i++)
                {
                    int inputBoneIndex = inputBoneIndexes[i];
                    float previousOffset = transition.CoupledOffsetBranches
                        .TryGetValue(inputBoneIndex, out float value)
                            ? value
                            : float.NaN;
                    float offset = GetContinuousRotationOffset(
                        previousOffset,
                        offsets[i]);
                    transition.CoupledOffsetBranches[inputBoneIndex] = offset;
                    if (Mathf.Abs(offset) <= ROTATION_APPLY_EPSILON)
                    {
                        continue;
                    }

                    transition.Corrections.Add(new RotationCorrection(
                        inputBoneIndex,
                        0,
                        offset));
                }
            }



            internal static bool TrySolveLeastSquares(
                float[,] coefficients,
                float[] values,
                out float[] result)
            {
                int rowCount = coefficients.GetLength(0);
                int columnCount = coefficients.GetLength(1);
                result = new float[columnCount];
                var normal = new double[columnCount, columnCount + 1];
                return TrySolveLeastSquares(
                    coefficients,
                    values,
                    rowCount,
                    columnCount,
                    result,
                    normal);
            }



            private static bool TrySolveLeastSquares(
                float[,] coefficients,
                float[] values,
                int rowCount,
                int columnCount,
                float[] result,
                double[,] normal)
            {
                if (rowCount > coefficients.GetLength(0) ||
                    columnCount > coefficients.GetLength(1) ||
                    rowCount > values.Length ||
                    columnCount > result.Length ||
                    columnCount == 0)
                {
                    return false;
                }

                Array.Clear(normal, 0, normal.Length);
                for (int column = 0; column < columnCount; column++)
                {
                    for (int otherColumn = 0;
                        otherColumn < columnCount;
                        otherColumn++)
                    {
                        double value = column == otherColumn ? 0.0001 : 0;
                        for (int row = 0; row < rowCount; row++)
                        {
                            value += coefficients[row, column] *
                                coefficients[row, otherColumn];
                        }
                        normal[column, otherColumn] = value;
                    }

                    for (int row = 0; row < rowCount; row++)
                    {
                        normal[column, columnCount] +=
                            coefficients[row, column] * values[row];
                    }
                }

                for (int pivot = 0; pivot < columnCount; pivot++)
                {
                    int bestRow = pivot;
                    for (int row = pivot + 1; row < columnCount; row++)
                    {
                        if (Math.Abs(normal[row, pivot]) >
                            Math.Abs(normal[bestRow, pivot]))
                        {
                            bestRow = row;
                        }
                    }
                    if (Math.Abs(normal[bestRow, pivot]) <= 0.00000001)
                    {
                        return false;
                    }

                    if (bestRow != pivot)
                    {
                        for (int column = pivot;
                            column <= columnCount;
                            column++)
                        {
                            (normal[pivot, column], normal[bestRow, column]) =
                                (normal[bestRow, column], normal[pivot, column]);
                        }
                    }

                    double divisor = normal[pivot, pivot];
                    for (int column = pivot; column <= columnCount; column++)
                    {
                        normal[pivot, column] /= divisor;
                    }

                    for (int row = 0; row < columnCount; row++)
                    {
                        if (row == pivot) { continue; }
                        double factor = normal[row, pivot];
                        for (int column = pivot; column <= columnCount; column++)
                        {
                            normal[row, column] -= factor * normal[pivot, column];
                        }
                    }
                }

                for (int i = 0; i < columnCount; i++)
                {
                    result[i] = (float)normal[i, columnCount];
                    if (float.IsNaN(result[i]) || float.IsInfinity(result[i]))
                    {
                        return false;
                    }
                }
                return true;
            }



            private bool TrySolveRotationCorrection(
                ObjectState objectState,
                RotationCompensationRoute route,
                float targetRotation,
                float branchReference,
                int selectedInputBoneIndex,
                bool hasFixedWindingOffset,
                float fixedWindingOffset,
                out RotationCorrection correction)
            {
                correction = default;
                float bestResidual = float.PositiveInfinity;
                float bestMagnitude = float.PositiveInfinity;
                bool solved = false;

                for (int i = 0; i < route.InputBoneIndexes.Length; i++)
                {
                    int inputBoneIndex = route.InputBoneIndexes[i];
                    if (selectedInputBoneIndex >= 0 &&
                        inputBoneIndex != selectedInputBoneIndex)
                    {
                        continue;
                    }

                    bool hasExistingCorrection = TryGetExistingRotationCorrection(
                        objectState.Transition,
                        inputBoneIndex,
                        out RotationCorrection existingCorrection);
                    float windingOffset;
                    float evaluationOffset;
                    float candidateBlendOffset;

                    if (hasExistingCorrection)
                    {
                        windingOffset = existingCorrection.WindingOffset;
                        evaluationOffset = 0;
                        candidateBlendOffset = existingCorrection.BlendOffset;
                    }
                    else
                    {
                        if (hasFixedWindingOffset)
                        {
                            windingOffset = fixedWindingOffset;
                        }
                        else
                        {
                            float baseRotation = objectState.Skeleton
                                .Bones.Items[inputBoneIndex].Pose.Rotation;
                            float previousAppliedRotation = objectState.LastAppliedRotations
                                .TryGetValue(inputBoneIndex, out float lastAppliedRotation)
                                    ? lastAppliedRotation
                                    : objectState.FrozenSourceLocalRotations[inputBoneIndex];
                            float rebasedRotation = RebaseRotation(
                                baseRotation,
                                previousAppliedRotation);
                            windingOffset = rebasedRotation - baseRotation;
                        }
                        evaluationOffset = windingOffset;
                        candidateBlendOffset = 0;
                    }

                    EvaluateActivePolicyPose(
                        objectState,
                        inputBoneIndex,
                        evaluationOffset);
                    if (!TryCreateRotationSample(
                        objectState.ComparisonSkeleton.Bones
                            .Items[route.OutputBoneIndex],
                        out RotationSample activePolicySample))
                    {
                        continue;
                    }

                    float activePolicyRotation = branchReference +
                        Mathf.DeltaAngle(
                            branchReference,
                            activePolicySample.Rotation);
                    EvaluateActivePolicyPose(
                        objectState,
                        inputBoneIndex,
                        evaluationOffset + ROTATION_PERTURBATION);

                    if (!TryCreateRotationSample(
                        objectState.ComparisonSkeleton.Bones
                            .Items[route.OutputBoneIndex],
                        out RotationSample perturbedSample))
                    {
                        continue;
                    }

                    float perturbedRotation = perturbedSample.Rotation;
                    float sensitivity = Mathf.DeltaAngle(
                        activePolicyRotation,
                        perturbedRotation) / ROTATION_PERTURBATION;
                    if (Mathf.Abs(sensitivity) < ROTATION_SENSITIVITY_EPSILON)
                    {
                        continue;
                    }

                    float additionalOffset = (
                        targetRotation - activePolicyRotation) / sensitivity;
                    candidateBlendOffset += additionalOffset;

                    EvaluateActivePolicyPose(
                        objectState,
                        inputBoneIndex,
                        evaluationOffset + additionalOffset);
                    if (!TryCreateRotationSample(
                        objectState.ComparisonSkeleton.Bones
                            .Items[route.OutputBoneIndex],
                        out RotationSample solvedSample))
                    {
                        continue;
                    }

                    float solvedRotation = solvedSample.Rotation;
                    float signedResidual = Mathf.DeltaAngle(
                        solvedRotation,
                        targetRotation);

                    if (Mathf.Abs(signedResidual) >= ROTATION_POLICY_EPSILON)
                    {
                        additionalOffset += signedResidual / sensitivity;
                        candidateBlendOffset += signedResidual / sensitivity;

                        EvaluateActivePolicyPose(
                            objectState,
                            inputBoneIndex,
                            evaluationOffset + additionalOffset);
                        if (!TryCreateRotationSample(
                            objectState.ComparisonSkeleton.Bones
                                .Items[route.OutputBoneIndex],
                            out solvedSample))
                        {
                            continue;
                        }

                        solvedRotation = solvedSample.Rotation;
                        signedResidual = Mathf.DeltaAngle(
                            solvedRotation,
                            targetRotation);
                    }

                    float residual = Mathf.Abs(signedResidual);
                    float magnitude = Mathf.Abs(candidateBlendOffset);
                    if (residual > ROTATION_POLICY_EPSILON ||
                        residual > bestResidual + ROTATION_APPLY_EPSILON ||
                        (Mathf.Abs(residual - bestResidual) <= ROTATION_APPLY_EPSILON &&
                            magnitude >= bestMagnitude))
                    {
                        continue;
                    }

                    bestResidual = residual;
                    bestMagnitude = magnitude;
                    correction = new RotationCorrection(
                        inputBoneIndex,
                        windingOffset,
                        candidateBlendOffset);
                    solved = true;
                }

                return solved;
            }



            private static bool TryGetExistingRotationCorrection(
                TransitionState transition,
                int inputBoneIndex,
                out RotationCorrection result)
            {
                for (int i = 0; i < transition.Corrections.Count; i++)
                {
                    if (transition.Corrections[i].InputBoneIndex == inputBoneIndex)
                    {
                        result = transition.Corrections[i];
                        return true;
                    }
                }

                result = default;
                return false;
            }



            private void AddRotationCorrection(
                TransitionState transition,
                RotationCompensationRoute route,
                RotationCorrection correction)
            {
                if (transition.RejectedInputBones.Contains(correction.InputBoneIndex))
                {
                    return;
                }

                for (int i = 0; i < transition.Corrections.Count; i++)
                {
                    RotationCorrection existing = transition.Corrections[i];
                    if (existing.InputBoneIndex != correction.InputBoneIndex) { continue; }

                    if (Mathf.Abs(
                            existing.WindingOffset - correction.WindingOffset) <=
                            ROTATION_APPLY_EPSILON &&
                        Mathf.Abs(
                            existing.BlendOffset - correction.BlendOffset) <=
                            ROTATION_POLICY_EPSILON)
                    {
                        transition.Corrections[i] = new RotationCorrection(
                            existing.InputBoneIndex,
                            existing.WindingOffset,
                            Mathf.Lerp(
                                existing.BlendOffset,
                                correction.BlendOffset,
                                0.5f));
                        return;
                    }

                    transition.Corrections.RemoveAt(i);
                    transition.RejectedInputBones.Add(correction.InputBoneIndex);
                    WarnOnce(
                        "rotation-conflict-" + correction.InputBoneIndex,
                        "[IGNORE_BLEND] 하나의 입력 Bone에 서로 다른 Plane 회전 보정이 요구되어 정책만 즉시 적용합니다: " +
                        SkelSbject.Skeleton_Data.Bones.Items[correction.InputBoneIndex].Name +
                        " / " + SkelSbject.Skeleton_Data.Constraints.Items[route.PolicyConstraintIndex].Name);
                    return;
                }

                transition.Corrections.Add(correction);
            }



            private static void EnsureRouteStateBuffers(TransitionState transition)
            {
                while (transition.SelectedInputBoneIndexes.Count < transition.Routes.Count)
                {
                    transition.SelectedInputBoneIndexes.Add(-1);
                    transition.WindingOffsets.Add(0);
                    transition.DynamicDestinationRotations.Add(float.NaN);
                    transition.SourceBranchRotations.Add(float.NaN);
                    transition.RotationBlendStates.Add(new RotationBlendState { Rotation = float.NaN });
                }
            }



            private void EvaluateActivePolicyPose(
                ObjectState objectState,
                int inputBoneIndex = -1,
                float rotationOffset = 0)
            {
                EnsureComparisonSkeleton(objectState);
                Skeleton comparisonSkeleton = objectState.ComparisonSkeleton;
                CopyCurrentPose(objectState.Skeleton, comparisonSkeleton);

                for (int i = 0; i < objectState.Transition.Corrections.Count; i++)
                {
                    RotationCorrection correction =
                        objectState.Transition.Corrections[i];
                    comparisonSkeleton.Bones.Items[correction.InputBoneIndex]
                        .Pose.Rotation += correction.TotalOffset;
                }

                if (inputBoneIndex >= 0)
                {
                    comparisonSkeleton.Bones.Items[inputBoneIndex].Pose.Rotation +=
                        rotationOffset;
                }

                ResetPolicyConstraintPoses(comparisonSkeleton);
                ApplyActivePolicies(comparisonSkeleton, objectState.SortedPolicies);
                comparisonSkeleton.UpdateWorldTransform(Spine.Physics.Pose);
            }



            private static void RestoreAppliedRotationCorrections(ObjectState objectState)
            {
                if (objectState.Skeleton == null) { return; }

                foreach (KeyValuePair<int, FrameRotationApplication> pair
                    in objectState.FrameRotationApplications)
                {
                    FrameRotationApplication application = pair.Value;
                    BonePose pose = objectState.Skeleton
                        .Bones.Items[pair.Key].Pose;
                    if (ShouldRestoreAppliedRotation(
                        pose.Rotation,
                        application.BaseRotation,
                        application.AppliedOffset))
                    {
                        pose.Rotation = application.BaseRotation;
                    }
                }
                objectState.FrameRotationApplications.Clear();
            }



            internal static bool ShouldRestoreAppliedRotation(
                float currentRotation,
                float baseRotation,
                float appliedOffset)
            {
                return Mathf.Abs(
                    currentRotation - (baseRotation + appliedOffset)) <=
                    ROTATION_APPLY_EPSILON;
            }



            private static void ApplyRotationCorrections(ObjectState objectState)
            {
                TransitionState transition = objectState.Transition;
                if (!transition.Active ||
                    objectState.Skeleton == null)
                {
                    return;
                }

                for (int i = 0; i < transition.Corrections.Count; i++)
                {
                    RotationCorrection correction = transition.Corrections[i];
                    ApplyRotationOffset(
                        objectState,
                        correction.InputBoneIndex,
                        correction.WindingOffset,
                        correction.BlendOffset);
                }
            }



            private static void ApplyPersistentWindingOffsets(
                ObjectState objectState)
            {
                if (objectState.Skeleton == null) { return; }

                foreach (KeyValuePair<int, float> pair
                    in objectState.PersistentWindingOffsets)
                {
                    ApplyRotationOffset(
                        objectState,
                        pair.Key,
                        pair.Value,
                        0);
                }
            }



            private static void ApplyRotationOffset(
                ObjectState objectState,
                int inputBoneIndex,
                float windingOffset,
                float blendOffset)
            {
                BonePose pose = objectState.Skeleton
                    .Bones.Items[inputBoneIndex].Pose;
                float baseRotation = pose.Rotation;
                pose.Rotation = baseRotation + windingOffset + blendOffset;

                objectState.LastAppliedRotations[inputBoneIndex] = pose.Rotation;
                objectState.FrameRotationApplications[inputBoneIndex] =
                    new FrameRotationApplication(
                        baseRotation,
                        windingOffset,
                        blendOffset);
            }



            private void TryNormalizePersistentWindings(ObjectState objectState)
            {
                objectState.WindingNormalizationPending = false;
                if (objectState.PersistentWindingOffsets.Count == 0 ||
                    objectState.PersistentWindingRoutes.Count == 0)
                {
                    objectState.PersistentWindingOffsets.Clear();
                    objectState.PersistentWindingRoutes.Clear();
                    return;
                }

                EnsureComparisonSkeleton(objectState);
                Skeleton comparisonSkeleton = objectState.ComparisonSkeleton;
                CopyCurrentPose(objectState.Skeleton, comparisonSkeleton);
                foreach (KeyValuePair<int, float> pair
                    in objectState.PersistentWindingOffsets)
                {
                    comparisonSkeleton.Bones.Items[pair.Key].Pose.Rotation +=
                        pair.Value;
                }
                ResetPolicyConstraintPoses(comparisonSkeleton);
                ApplyActivePolicies(comparisonSkeleton, objectState.SortedPolicies);
                comparisonSkeleton.UpdateWorldTransform(Spine.Physics.Pose);

                objectState.PersistentWindingSamples.Clear();
                for (int i = 0; i < objectState.PersistentWindingRoutes.Count; i++)
                {
                    int outputBoneIndex = objectState
                        .PersistentWindingRoutes[i].OutputBoneIndex;
                    objectState.PersistentWindingSamples.Add(
                        new WorldMatrixSample(
                            comparisonSkeleton.Bones.Items[outputBoneIndex]
                                .AppliedPose));
                }

                CopyCurrentPose(objectState.Skeleton, comparisonSkeleton);
                ResetPolicyConstraintPoses(comparisonSkeleton);
                ApplyActivePolicies(comparisonSkeleton, objectState.SortedPolicies);
                comparisonSkeleton.UpdateWorldTransform(Spine.Physics.Pose);

                for (int i = 0; i < objectState.PersistentWindingRoutes.Count; i++)
                {
                    int outputBoneIndex = objectState
                        .PersistentWindingRoutes[i].OutputBoneIndex;
                    WorldMatrixSample withoutWinding = new(
                        comparisonSkeleton.Bones.Items[outputBoneIndex]
                            .AppliedPose);
                    if (!AreEquivalent(
                        objectState.PersistentWindingSamples[i],
                        withoutWinding))
                    {
                        objectState.PersistentWindingSamples.Clear();
                        return;
                    }
                }

                objectState.PersistentWindingOffsets.Clear();
                objectState.PersistentWindingRoutes.Clear();
                objectState.PersistentWindingSamples.Clear();
            }



            private static bool AreEquivalent(
                WorldMatrixSample left,
                WorldMatrixSample right)
            {
                return Mathf.Abs(left.A - right.A) <= ROTATION_APPLY_EPSILON &&
                    Mathf.Abs(left.B - right.B) <= ROTATION_APPLY_EPSILON &&
                    Mathf.Abs(left.C - right.C) <= ROTATION_APPLY_EPSILON &&
                    Mathf.Abs(left.D - right.D) <= ROTATION_APPLY_EPSILON &&
                    Mathf.Abs(left.X - right.X) <= ROTATION_APPLY_EPSILON &&
                    Mathf.Abs(left.Y - right.Y) <= ROTATION_APPLY_EPSILON;
            }



            private static void AdvanceTransition(ObjectState objectState)
            {
                TransitionState transition = objectState.Transition;
                if (!transition.Active || transition.AdvancedFrame == Time.frameCount)
                {
                    return;
                }

                transition.AdvancedFrame = Time.frameCount;
                if (transition.Alpha >= 1)
                {
                    objectState.PersistentWindingOffsets.Clear();
                    objectState.PersistentWindingRoutes.Clear();
                    for (int i = 0; i < transition.Corrections.Count; i++)
                    {
                        RotationCorrection correction = transition.Corrections[i];
                        if (Mathf.Abs(correction.WindingOffset) <=
                            ROTATION_APPLY_EPSILON)
                        {
                            continue;
                        }

                        objectState.PersistentWindingOffsets[
                            correction.InputBoneIndex] = correction.WindingOffset;
                    }

                    if (objectState.PersistentWindingOffsets.Count > 0)
                    {
                        objectState.PersistentWindingRoutes.AddRange(
                            transition.Routes);
                        objectState.WindingNormalizationPending = true;
                    }
                    transition.Reset();
                    return;
                }

                transition.FirstFrame = false;
                transition.Elapsed += Time.unscaledDeltaTime *
                    (objectState.AnimationState?.TimeScale ?? 1);
            }



            private float ResolveTransitionAlpha(ObjectState objectState)
            {
                TransitionState transition = objectState.Transition;
                if (transition.FirstFrame) { return 0; }

                return ResolveRawTransitionAlpha(objectState);
            }



            private static float ResolveRawTransitionAlpha(ObjectState objectState)
            {
                TransitionState transition = objectState.Transition;

                TrackEntry currentEntry =
                    objectState.AnimationState?.GetTrack(
                        transition.SourceTrackIndex);

                if (currentEntry != null &&
                    currentEntry.MixDuration > 0 &&
                    Mathf.Abs(currentEntry.MixDuration - transition.Duration) <= 0.001f)
                {
                    return Mathf.Clamp01(
                        currentEntry.MixTime /
                        currentEntry.MixDuration);
                }

                return Mathf.Clamp01(
                    transition.Elapsed /
                    transition.Duration);
            }



            private void ResetPolicyConstraintPoses(Skeleton skeleton)
            {
                IConstraint[] constraints = skeleton.Constraints.Items;
                for (int i = 0; i < PolicyConstraintIndexes.Length; i++)
                {
                    int constraintIndex = PolicyConstraintIndexes[i];
                    if ((uint)constraintIndex < (uint)skeleton.Constraints.Count)
                    {
                        constraints[constraintIndex].SetupPose();
                    }
                }
            }



            private static void ApplyActivePolicies(
                Skeleton skeleton,
                List<ActivePolicy> policies)
            {
                for (int i = 0; i < policies.Count; i++)
                {
                    ActivePolicy policy = policies[i];
                    policy.RefreshPlaybackTime();
                    ApplyPolicy(
                        skeleton,
                        policy.Profile.PolicyAnimation,
                        policy.Time,
                        policy.Loop);
                }
            }



            private static void ApplyPolicy(
                Skeleton skeleton,
                Spine.Animation animation,
                float time,
                bool loop)
            {
                animation.Apply(
                    skeleton,
                    time,
                    time,
                    loop,
                    null,
                    1,
                    MixFrom.Current,
                    false,
                    false,
                    false);
            }



            private void CopyCurrentPose(
                Skeleton source,
                Skeleton destination)
            {
                if (!ReferenceEquals(source.Skin, destination.Skin))
                {
                    destination.Skin = source.Skin;
                    destination.UpdateCache();
                }

                destination.X = source.X;
                destination.Y = source.Y;
                destination.ScaleX = source.ScaleX;
                destination.ScaleY = source.ScaleY;
                destination.Time = source.Time;

                for (int i = 0; i < source.Bones.Count; i++)
                {
                    destination.Bones.Items[i].Pose.Set(
                        source.Bones.Items[i].Pose);
                }

                for (int i = 0; i < source.Slots.Count; i++)
                {
                    destination.Slots.Items[i].Pose.Set(
                        source.Slots.Items[i].Pose);
                }

                IConstraint[] sourceConstraints = source.Constraints.Items;
                IConstraint[] destinationConstraints = destination.Constraints.Items;
                for (int i = 0; i < source.Constraints.Count; i++)
                {
                    switch (sourceConstraints[i])
                    {
                        case Slider sourcePose when destinationConstraints[i] is Slider destinationPose:
                            destinationPose.Pose.Set(sourcePose.Pose);
                            break;
                        case TransformConstraint sourcePose when destinationConstraints[i] is TransformConstraint destinationPose:
                            destinationPose.Pose.Set(sourcePose.Pose);
                            break;
                        case IkConstraint sourcePose when destinationConstraints[i] is IkConstraint destinationPose:
                            destinationPose.Pose.Set(sourcePose.Pose);
                            break;
                        case PathConstraint sourcePose when destinationConstraints[i] is PathConstraint destinationPose:
                            destinationPose.Pose.Set(sourcePose.Pose);
                            break;
                        case PhysicsConstraint sourcePose when destinationConstraints[i] is PhysicsConstraint destinationPose:
                            destinationPose.Pose.Set(sourcePose.Pose);
                            break;
                        default:
                            destinationConstraints[i].SetupPose();
                            WarnOnce(
                                "unknown-constraint-" + sourceConstraints[i].GetType().FullName,
                                "[IGNORE_BLEND] 알 수 없는 Constraint 타입은 보조 Skeleton에서 Setup Pose로 평가합니다: " +
                                sourceConstraints[i].GetType().FullName);
                            break;
                    }
                }
            }



            private ObjectState GetObjectState(SkelObject skelObject)
            {
                if (ObjectStates.TryGetValue(
                    skelObject,
                    out ObjectState objectState))
                {
                    return objectState;
                }

                objectState = new ObjectState();
                ObjectStates.Add(skelObject, objectState);
                return objectState;
            }



            private void SubscribeCallbacks(
                SkelObject skelObject,
                ObjectState objectState)
            {
                if (objectState.CallbackSubscribed) { return; }
                skelObject.RuntimePosePreparedEvent -= RuntimePosePreparedCallback;
                skelObject.RuntimePosePreparedEvent += RuntimePosePreparedCallback;
                objectState.CallbackSubscribed = true;
            }



            private void UnsubscribeCallbacks(
                SkelObject skelObject,
                ObjectState objectState)
            {
                skelObject.RuntimePosePreparedEvent -= RuntimePosePreparedCallback;
                objectState.CallbackSubscribed = false;

                objectState.ResetRuntime();
            }



            private void EnsureSkeleton(
                SkelObject skelObject,
                ObjectState objectState)
            {
                SubscribeCallbacks(skelObject, objectState);

                Skeleton skeleton =
                    skelObject.Skel?.GetSkeletonAnimation()?.Skeleton;
                if (ReferenceEquals(objectState.Skeleton, skeleton)) { return; }

                objectState.Skeleton = skeleton;
                objectState.AnimationState =
                    skelObject.Skel?.GetSkeletonAnimation()?.AnimationState;
                objectState.ComparisonSkeleton = null;
                objectState.FrozenSourceRotations = null;
                objectState.FrozenSourceLocalRotations = null;
                objectState.FrameRotationApplications.Clear();
                objectState.LastAppliedRotations.Clear();
                objectState.PersistentWindingOffsets.Clear();
                objectState.PersistentWindingRoutes.Clear();
                objectState.PersistentWindingSamples.Clear();
                objectState.WindingNormalizationPending = false;
                objectState.SourcePoseSkeleton = null;
                objectState.SourcePoseTracks.Clear();
                objectState.PoseRetargetTrackIndexes.Clear();
                objectState.ActivePolicies.Clear();
                objectState.Generations.Clear();
                objectState.SortedPolicies.Clear();
                objectState.Transition.Reset();
            }



            private static void EnsureComparisonSkeleton(ObjectState objectState)
            {
                if (objectState.Skeleton == null) { return; }

                objectState.ComparisonSkeleton ??=
                    new Skeleton(objectState.Skeleton);
                EnsureRotationBuffers(objectState);
            }



            private static void EnsureRotationBuffers(ObjectState objectState)
            {
                int boneCount = objectState.Skeleton?.Bones.Count ?? 0;
                if (objectState.FrozenSourceRotations == null ||
                    objectState.FrozenSourceRotations.Length != boneCount ||
                    objectState.FrozenSourceLocalRotations == null ||
                    objectState.FrozenSourceLocalRotations.Length != boneCount)
                {
                    objectState.FrozenSourceRotations = new RotationSample[boneCount];
                    objectState.FrozenSourceLocalRotations = new float[boneCount];
                }
            }



            private static void CaptureAllBoneRotations(
                Skeleton skeleton,
                RotationSample[] worldDestination,
                float[] localDestination)
            {
                for (int i = 0; i < skeleton.Bones.Count; i++)
                {
                    Bone bone = skeleton.Bones.Items[i];
                    TryCreateRotationSample(bone, out worldDestination[i]);
                    localDestination[i] = skeleton.Bones.Items[i].Pose.Rotation;
                }
            }



            internal static float RebaseRotation(
                float baseRotation,
                float previousAppliedRotation)
            {
                return baseRotation + 360f * Mathf.Round(
                    (previousAppliedRotation - baseRotation) / 360f);
            }



            internal static float UnwrapRotationNear(
                float referenceRotation,
                float rotation)
            {
                return referenceRotation + Mathf.DeltaAngle(
                    referenceRotation,
                    rotation);
            }



            internal static float GetShortestRotationTarget(
                float sourceRotation,
                float currentTargetRotation,
                float alpha)
            {
                return Mathf.LerpUnclamped(
                    sourceRotation,
                    UnwrapRotationNear(sourceRotation, currentTargetRotation),
                    Mathf.Clamp01(alpha));
            }



            internal static float GetContinuousDestinationRotation(
                float sourceRotation,
                float previousDestinationRotation,
                float currentDestinationRotation)
            {
                float referenceRotation = float.IsNaN(previousDestinationRotation)
                    ? sourceRotation
                    : previousDestinationRotation;
                return UnwrapRotationNear(referenceRotation, currentDestinationRotation);
            }



            internal static float GetContinuousRotationTarget(
                float sourceRotation,
                float continuousDestinationRotation,
                float alpha)
            {
                var state = new RotationBlendState { Rotation = float.NaN };
                return GetContinuousRotationTarget(sourceRotation, continuousDestinationRotation, alpha, ref state);
            }



            internal struct RotationBlendState
            {
                public float Rotation;
                public float Alpha;
                public bool SmoothCancellation;
            }



            internal static float GetContinuousRotationTarget(
                float sourceRotation,
                float destinationRotation,
                float alpha,
                ref RotationBlendState state)
            {
                alpha = Mathf.Clamp01(alpha);
                float source = sourceRotation * Mathf.Deg2Rad;
                float destination = destinationRotation * Mathf.Deg2Rad;
                float sourceX = Mathf.Cos(source), sourceY = Mathf.Sin(source);
                float destinationX = Mathf.Cos(destination), destinationY = Mathf.Sin(destination);
                float x = Mathf.Lerp(sourceX, destinationX, alpha);
                float y = Mathf.Lerp(sourceY, destinationY, alpha);
                bool cancellation = x * x + y * y < MATRIX_DETERMINANT_EPSILON;
                //. 같은 방향의 +360도 표현이 불필요한 한 바퀴 경로가 되지 않도록 방향을 보간한다.
                float target = cancellation
                    ? GetShortestRotationTarget(sourceRotation, destinationRotation, alpha)
                    : Mathf.Atan2(y, x) * Mathf.Rad2Deg;
                bool hasPrevious = !float.IsNaN(state.Rotation);
                target = UnwrapRotationNear(hasPrevious ? state.Rotation : sourceRotation, target);
                //? 둔각 방향은 벡터 합이 작아지며 중간 회전이 급격히 빨라진다. 상쇄되기 전부터 남은 Mix 시간에 분산한다.
                bool opposingDirections = sourceX * destinationX + sourceY * destinationY < 0f;
                if (opposingDirections || cancellation || hasPrevious && alpha < 1f && Mathf.Abs(target - state.Rotation) > 90f)
                {
                    state.SmoothCancellation = true;
                }
                if (hasPrevious && state.SmoothCancellation)
                {
                    //? 정반대 방향이 상쇄되는 중간 지점에서도 직전 출력에서 남은 Mix 시간만큼 이어간다.
                    float step = state.Alpha >= 1f ? 1f :
                        Mathf.Clamp01((alpha - state.Alpha) / (1f - state.Alpha));
                    target = Mathf.LerpUnclamped(state.Rotation, target, step);
                }
                state.Rotation = target;
                state.Alpha = alpha;
                return target;
            }



            internal static float GetStableSourceBranch(
                float sourceRotation,
                float destinationRotation,
                float previousSourceBranch)
            {
                return float.IsNaN(previousSourceBranch)
                    ? UnwrapRotationNear(destinationRotation, sourceRotation)
                    : previousSourceBranch;
            }



            internal static float GetContinuousRotationOffset(
                float previousOffset,
                float currentOffset)
            {
                return float.IsNaN(previousOffset)
                    ? currentOffset
                    : UnwrapRotationNear(previousOffset, currentOffset);
            }



            internal static float AccumulateCoupledRotationOffset(
                float previousOffset,
                float solvedDelta)
            {
                return previousOffset + solvedDelta;
            }



            internal static bool TryGetPolarRotation(
                float a,
                float b,
                float c,
                float d,
                out float rotation,
                out int determinantSign)
            {
                float determinant = a * d - b * c;
                if (Mathf.Abs(determinant) <= MATRIX_DETERMINANT_EPSILON)
                {
                    rotation = 0;
                    determinantSign = 0;
                    return false;
                }

                determinantSign = determinant > 0 ? 1 : -1;
                float x = determinantSign > 0 ? a + d : a - d;
                float y = determinantSign > 0 ? c - b : c + b;
                if (x * x + y * y <= MATRIX_DETERMINANT_EPSILON)
                {
                    rotation = 0;
                    determinantSign = 0;
                    return false;
                }

                rotation = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
                return true;
            }



            private static bool TryCreateRotationSample(
                Bone bone,
                out RotationSample sample)
            {
                BonePose pose = bone.AppliedPose;
                float determinant = pose.A * pose.D - pose.B * pose.C;
                float axisLengthSquared = pose.A * pose.A + pose.C * pose.C;
                if (Mathf.Abs(determinant) > MATRIX_DETERMINANT_EPSILON &&
                    axisLengthSquared > MATRIX_DETERMINANT_EPSILON)
                {
                    //. 실제 Attachment의 Bone 길이 방향과 같은 월드 X축을 기준으로 한다.
                    //. 부모의 반전/회전이 바뀌는 동안 상대 회전만 맞추면 화면에서는 도약한다.
                    sample = new RotationSample(
                        Mathf.Atan2(pose.C, pose.A) * Mathf.Rad2Deg);
                    return true;
                }

                sample = default;
                return false;
            }



            internal static bool TryGetRelativePolarRotation(
                float childA,
                float childB,
                float childC,
                float childD,
                float parentA,
                float parentB,
                float parentC,
                float parentD,
                out float rotation,
                out float determinant)
            {
                float parentDeterminant = parentA * parentD - parentB * parentC;
                if (Mathf.Abs(parentDeterminant) <= MATRIX_DETERMINANT_EPSILON)
                {
                    rotation = 0;
                    determinant = 0;
                    return false;
                }

                float a = (parentD * childA - parentB * childC) /
                    parentDeterminant;
                float b = (parentD * childB - parentB * childD) /
                    parentDeterminant;
                float c = (-parentC * childA + parentA * childC) /
                    parentDeterminant;
                float d = (-parentC * childB + parentA * childD) /
                    parentDeterminant;
                determinant = a * d - b * c;
                return TryGetPolarRotation(
                    a,
                    b,
                    c,
                    d,
                    out rotation,
                    out _);
            }



            private static int ResolveTrackIndex(
                SkelObject skelObject,
                SkelObject.AniCore.Track track)
            {
                foreach (KeyValuePair<int, SkelObject.AniCore.Track> pair
                    in skelObject.Ani.Player.GetPlayingAnimationTracks)
                {
                    if (ReferenceEquals(pair.Value, track))
                    {
                        return pair.Key;
                    }
                }

                return track.PlayingSkelAni.TrackIndex;
            }



            private void WarnOnce(string key, string message)
            {
                if (LoggedWarnings.Add(key))
                {
                    Debug.LogWarning(message);
                }
            }



            internal sealed class RotationCompensationRoute
            {
                public RotationCompensationRoute(
                    int policyConstraintIndex,
                    int[] inputBoneIndexes,
                    int outputBoneIndex)
                {
                    PolicyConstraintIndex = policyConstraintIndex;
                    InputBoneIndexes = inputBoneIndexes;
                    OutputBoneIndex = outputBoneIndex;
                }

                public readonly int PolicyConstraintIndex;
                public readonly int[] InputBoneIndexes;
                public readonly int OutputBoneIndex;

                public static RotationCompensationRoute Merge(
                    RotationCompensationRoute left,
                    RotationCompensationRoute right)
                {
                    HashSet<int> inputBoneIndexes = new(left.InputBoneIndexes);
                    inputBoneIndexes.UnionWith(right.InputBoneIndexes);
                    int[] mergedInputs = new int[inputBoneIndexes.Count];
                    inputBoneIndexes.CopyTo(mergedInputs);
                    Array.Sort(mergedInputs);

                    return new RotationCompensationRoute(
                        left.PolicyConstraintIndex,
                        mergedInputs,
                        left.OutputBoneIndex);
                }
            }



            private readonly struct RotationCorrection
            {
                public RotationCorrection(
                    int inputBoneIndex,
                    float windingOffset,
                    float blendOffset)
                {
                    InputBoneIndex = inputBoneIndex;
                    WindingOffset = windingOffset;
                    BlendOffset = blendOffset;
                }

                public readonly int InputBoneIndex;
                public readonly float WindingOffset;
                public readonly float BlendOffset;
                public float TotalOffset => WindingOffset + BlendOffset;
            }



            private readonly struct FrameRotationApplication
            {
                public FrameRotationApplication(
                    float baseRotation,
                    float windingOffset,
                    float blendOffset)
                {
                    BaseRotation = baseRotation;
                    WindingOffset = windingOffset;
                    BlendOffset = blendOffset;
                }

                public readonly float BaseRotation;
                public readonly float WindingOffset;
                public readonly float BlendOffset;
                public float AppliedOffset => WindingOffset + BlendOffset;
            }



            private readonly struct RotationSample
            {
                public RotationSample(float rotation)
                {
                    Rotation = rotation;
                    Valid = true;
                }

                public readonly float Rotation;
                public readonly bool Valid;
            }



            private readonly struct WorldMatrixSample
            {
                public WorldMatrixSample(BonePose pose)
                {
                    A = pose.A;
                    B = pose.B;
                    C = pose.C;
                    D = pose.D;
                    X = pose.WorldX;
                    Y = pose.WorldY;
                }

                public readonly float A;
                public readonly float B;
                public readonly float C;
                public readonly float D;
                public readonly float X;
                public readonly float Y;
            }



            private sealed class AnimationProfile
            {
                public AnimationProfile(
                    Spine.Animation sourceAnimation,
                    Spine.Animation policyAnimation,
                    RotationCompensationRoute[] rotationRoutes,
                    RotationCompensationRoute[] poseRetargetRoutes)
                {
                    SourceAnimation = sourceAnimation;
                    PolicyAnimation = policyAnimation;
                    RotationRoutes = rotationRoutes;
                    PoseRetargetRoutes = poseRetargetRoutes;
                }

                public readonly Spine.Animation SourceAnimation;
                public readonly Spine.Animation PolicyAnimation;
                public readonly RotationCompensationRoute[] RotationRoutes;
                public readonly RotationCompensationRoute[] PoseRetargetRoutes;
            }



            private sealed class ActivePolicy
            {
                public ActivePolicy(
                    int trackIndex,
                    int generation,
                    AnimationProfile profile,
                    TrackEntry sourceEntry)
                {
                    TrackIndex = trackIndex;
                    Generation = generation;
                    Profile = profile;
                    SourceEntry = sourceEntry;
                    Time = sourceEntry?.AnimationTime ?? 0;
                    Loop = sourceEntry?.Loop ?? false;
                }

                public readonly int TrackIndex;
                public readonly int Generation;
                public readonly AnimationProfile Profile;
                public readonly TrackEntry SourceEntry;
                public float Time;
                public bool Loop;

                public void RefreshPlaybackTime()
                {
                    if (SourceEntry == null ||
                        !ReferenceEquals(
                            SourceEntry.Animation,
                            Profile.SourceAnimation))
                    {
                        return;
                    }

                    Time = SourceEntry.AnimationTime;
                    Loop = SourceEntry.Loop;
                }

                public PolicySample CreateSample()
                {
                    return new PolicySample(TrackIndex, Profile);
                }
            }



            private struct PolicySample
            {
                public PolicySample(
                    int trackIndex,
                    AnimationProfile profile)
                {
                    TrackIndex = trackIndex;
                    Profile = profile;
                }

                public readonly int TrackIndex;
                public readonly AnimationProfile Profile;
            }



            private sealed class TransitionState
            {
                public bool Active;
                public bool FirstFrame;
                public bool HasFrozenSource;
                public int SourceTrackIndex;
                public float Duration;
                public float Elapsed;
                public float Alpha;
                public int PreparedFrame = -1;
                public int AdvancedFrame = -1;
                public bool UsePoseRetargetRoutes;
                public bool BranchesInitialized;
                public bool HasSharedInputs;
                public readonly List<PolicySample> PreviousPolicies = new();
                public readonly List<RotationCompensationRoute> Routes = new();
                public readonly List<int> SelectedInputBoneIndexes = new();
                public readonly List<float> WindingOffsets = new();
                public readonly List<float> DynamicDestinationRotations = new();
                public readonly List<float> SourceBranchRotations = new();
                public readonly List<RotationBlendState> RotationBlendStates = new();
                public readonly List<RotationCorrection> Corrections = new();
                public readonly Dictionary<int, float> CoupledOffsetBranches = new();
                public readonly HashSet<int> RejectedInputBones = new();
                public readonly List<int> CoupledInputBoneIndexes = new();
                public float[] CoupledActiveRotations = Array.Empty<float>();
                public float[] CoupledTargetRotations = Array.Empty<float>();
                public float[] CoupledRequiredDeltas = Array.Empty<float>();
                public float[] CoupledResiduals = Array.Empty<float>();
                public bool[] CoupledValidOutputs = Array.Empty<bool>();
                public float[] CoupledOffsets = Array.Empty<float>();
                public float[] CoupledRefinement = Array.Empty<float>();
                public float[,] CoupledSensitivities = new float[0, 0];
                public double[,] CoupledNormalMatrix = new double[0, 0];

                public void Reset(bool preservePreviousPolicies = false)
                {
                    Active = false;
                    FirstFrame = false;
                    HasFrozenSource = false;
                    SourceTrackIndex = 0;
                    Duration = 0;
                    Elapsed = 0;
                    Alpha = 0;
                    PreparedFrame = -1;
                    AdvancedFrame = -1;
                    UsePoseRetargetRoutes = false;
                    if (!preservePreviousPolicies)
                    {
                        PreviousPolicies.Clear();
                    }
                    Routes.Clear();
                    CoupledInputBoneIndexes.Clear();
                    HasSharedInputs = false;
                    ResetRotationState();
                }

                public void ResetRotationState()
                {
                    BranchesInitialized = false;
                    SelectedInputBoneIndexes.Clear();
                    WindingOffsets.Clear();
                    DynamicDestinationRotations.Clear();
                    SourceBranchRotations.Clear();
                    RotationBlendStates.Clear();
                    Corrections.Clear();
                    CoupledOffsetBranches.Clear();
                    RejectedInputBones.Clear();
                }
            }



            /// <summary>
            /// 한 활성 SkelObject의 작업 공간. 프레임과 전환 사이에는 배열 용량을 재사용하고,
            /// Disable 시 참조를 해제한다. 감도·보정값은 현재 포즈에서 매번 다시 계산한다.
            /// </summary>
            private sealed class ObjectState
            {
                public bool CallbackSubscribed;
                public Spine.AnimationState AnimationState;
                public Skeleton Skeleton;
                public Skeleton ComparisonSkeleton;
                public Skeleton SourcePoseSkeleton;
                public readonly Dictionary<int, Spine.Animation> SourcePoseTracks = new();
                public readonly List<int> PoseRetargetTrackIndexes = new();
                public RotationSample[] FrozenSourceRotations;
                public float[] FrozenSourceLocalRotations;

                public readonly Dictionary<int, ActivePolicy> ActivePolicies = new();
                public readonly Dictionary<int, int> Generations = new();
                public readonly Dictionary<int, FrameRotationApplication>
                    FrameRotationApplications = new();
                public readonly Dictionary<int, float> LastAppliedRotations = new();
                public readonly Dictionary<int, float> PersistentWindingOffsets = new();
                public readonly List<RotationCompensationRoute> PersistentWindingRoutes = new();
                public readonly List<WorldMatrixSample> PersistentWindingSamples = new();
                public readonly List<ActivePolicy> SortedPolicies = new();
                public readonly TransitionState Transition = new();
                public bool WindingNormalizationPending;


                public int NextGeneration(int trackIndex)
                {
                    Generations.TryGetValue(
                        trackIndex,
                        out int generation);
                    generation++;
                    Generations[trackIndex] = generation;
                    return generation;
                }

                public void RebuildSortedPolicies()
                {
                    SortedPolicies.Clear();
                    foreach (ActivePolicy policy in ActivePolicies.Values)
                    {
                        SortedPolicies.Add(policy);
                    }
                    SortedPolicies.Sort(
                        static (left, right) =>
                            left.TrackIndex.CompareTo(right.TrackIndex));
                }

                public void ResetRuntime()
                {
                    AnimationState = null;
                    Skeleton = null;
                    ComparisonSkeleton = null;
                    FrozenSourceRotations = null;
                    FrozenSourceLocalRotations = null;
                    ActivePolicies.Clear();
                    Generations.Clear();
                    FrameRotationApplications.Clear();
                    LastAppliedRotations.Clear();
                    PersistentWindingOffsets.Clear();
                    PersistentWindingRoutes.Clear();
                    PersistentWindingSamples.Clear();
                    WindingNormalizationPending = false;
                    SourcePoseSkeleton = null;
                    SourcePoseTracks.Clear();
                    PoseRetargetTrackIndexes.Clear();
                    SortedPolicies.Clear();
                    Transition.Reset();
                }
            }
        }



        ///======================================================================================================================================================



        //? 드로우 오더



        public abstract class BaseDrawOrderSet
        {
            public BaseDrawOrderSet(SkelSbject skelSbject)
            {
                SkelSbject = skelSbject;
            }

            protected readonly SkelSbject SkelSbject;
        }



        public abstract class BaseDrawOrderSet<TSkelSbject> : BaseDrawOrderSet where TSkelSbject : SkelSbject, new()
        {
            public BaseDrawOrderSet(TSkelSbject skelSbject) : base(skelSbject)
            {
                SkelSbject = skelSbject;
            }

            protected readonly new TSkelSbject SkelSbject;
        }



        [Serializable]
        public class SlotBundleInfo
        {
            public SlotBundleInfo(string slotBundleName, IEnumerable<SlotData> allSlotData, int bundleIndex = -1)
            {
                SlotBundleName = slotBundleName;
                BundleIndex = bundleIndex;

                var slotDataList = new List<SlotData>();

                foreach (var slot in allSlotData)
                {
                    if (slot.Name.Contains(slotBundleName)) slotDataList.Add(slot);
                }

                SlotDataArray = new SlotData[slotDataList.Count];
                SlotNameArray = new string[slotDataList.Count];
                for (int i = 0; i < slotDataList.Count; i++)
                {
                    SlotDataArray[i] = slotDataList[i];
                    SlotNameArray[i] = slotDataList[i].Name;
                }
            }



            /// <summary>
            /// 슬롯번들 이름 
            /// </summary>
            [ShowInInspector]
            [ReadOnly]
            public readonly string SlotBundleName;

            /// <summary>
            /// 번들 번호 (중요하진않고, 구분용)
            /// </summary>
            [ShowInInspector]
            [ReadOnly]
            public readonly int BundleIndex;



            /// <summary>
            /// 슬롯번들에 해당하는 <see cref="SlotData"/>  배열
            /// </summary>
            [ShowInInspector]
            [ReadOnly]
            public readonly SlotData[] SlotDataArray;



            /// <summary>
            /// 슬롯번들에 해당하는 <see cref="SlotData"/> 슬롯 이름 배열
            /// </summary>
            [ShowInInspector]
            [ReadOnly]
            public readonly string[] SlotNameArray;
        }



        ///======================================================================================================================================================
    }
}
