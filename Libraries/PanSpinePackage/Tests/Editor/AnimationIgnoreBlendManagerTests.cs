using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Spine;
using UnityEngine;

namespace Pan.SpinePackage.Tests
{
    public class AnimationIgnoreBlendManagerTests
    {
        [Test]
        public void Manager_ObservesAnimationStartAndEnd()
        {
            System.Type managerType =
                typeof(SkelSbject.AnimationIgnoreBlendManager);

            Assert.That(
                typeof(SkelObject.Observers.IS.IAniStart).IsAssignableFrom(managerType),
                Is.True);
            Assert.That(
                typeof(SkelObject.Observers.IS.IAniEnd).IsAssignableFrom(managerType),
                Is.True);
        }

        [Test]
        public void PoseRetargetDetection_IncludesBonesAndConstraints()
        {
            var empty = new Spine.Animation("empty");
            var boneAnimation = new Spine.Animation("bone");
            boneAnimation.SetTimelines(
                new ExposedList<Timeline>(
                    new Timeline[] { new ScaleTimeline(1, 0, 0) }),
                new ExposedList<int>());
            var constraintAnimation = new Spine.Animation("constraint");
            constraintAnimation.SetTimelines(
                new ExposedList<Timeline>(
                    new Timeline[] { new TransformConstraintTimeline(1, 0, 0) }),
                new ExposedList<int>());

            Assert.That(
                SkelSbject.AnimationIgnoreBlendManager
                    .AnimationChangesTransformPose(empty),
                Is.False);
            Assert.That(
                SkelSbject.AnimationIgnoreBlendManager
                    .AnimationChangesTransformPose(boneAnimation),
                Is.True);
            Assert.That(
                SkelSbject.AnimationIgnoreBlendManager
                    .AnimationChangesTransformPose(constraintAnimation),
                Is.True);
        }

        [Test]
        public void PolicyConstraintDiscovery_CollectsNestedSlidersAndStopsCycles()
        {
            var data = new SkeletonData();
            var rootSlider = new SliderData("root");
            var nestedSlider = new SliderData("nested");

            data.Constraints.Add(rootSlider);
            data.Constraints.Add(new TransformConstraintData("child"));
            data.Constraints.Add(nestedSlider);
            data.Constraints.Add(new TransformConstraintData("grandchild"));

            SetSliderAnimation(
                rootSlider,
                new TransformConstraintTimeline(1, 0, 1),
                new SliderTimeline(1, 0, 2));
            SetSliderAnimation(
                nestedSlider,
                new TransformConstraintTimeline(1, 0, 3),
                new SliderTimeline(1, 0, 0));

            int[] result = SkelSbject.AnimationIgnoreBlendManager
                .CollectPolicyConstraintIndexes(data, new[] { 0 });

            Assert.That(result, Is.EqualTo(new[] { 0, 1, 2, 3 }));
        }

        [Test]
        public void PolicyExtraction_PreservesSliderOwnedAnimationAsAtomicGraph()
        {
            var data = new SkeletonData();
            var rootSlider = new SliderData("root");
            var nestedSlider = new SliderData("nested");
            data.Constraints.Add(rootSlider);
            data.Constraints.Add(nestedSlider);
            SetSliderAnimation(rootSlider, new SliderTimeline(1, 0, 1));
            SetSliderAnimation(nestedSlider, new ScaleTimeline(1, 0, 0));

            HashSet<Spine.Animation> sliderOwnedAnimations =
                SkelSbject.AnimationIgnoreBlendManager
                    .CollectSliderOwnedAnimations(data);
            ExposedList<Timeline> nestedPolicy =
                SkelSbject.AnimationIgnoreBlendManager.ExtractPolicyTimelines(
                    rootSlider.Animation,
                    new[] { 0, 1 },
                    sliderOwnedAnimations);

            Assert.That(nestedPolicy.Count, Is.Zero);
            Assert.That(rootSlider.Animation.Timelines.Count, Is.EqualTo(1));

            var rootAnimation = new Spine.Animation("playable");
            rootAnimation.SetTimelines(
                new ExposedList<Timeline>(
                    new Timeline[] { new SliderTimeline(1, 0, 0) }),
                new ExposedList<int>());

            ExposedList<Timeline> rootPolicy =
                SkelSbject.AnimationIgnoreBlendManager.ExtractPolicyTimelines(
                    rootAnimation,
                    new[] { 0, 1 },
                    sliderOwnedAnimations);

            Assert.That(rootPolicy.Count, Is.EqualTo(1));
            Assert.That(rootAnimation.Timelines.Count, Is.Zero);
        }

        [Test]
        public void ScalePolicyClassification_DetectsPureReflection()
        {
            var slider = new SliderData("graphic flip");
            var scale = new ScaleTimeline(2, 0, 0);
            scale.SetFrame(0, 0, 1, 1);
            scale.SetFrame(1, 0.02f, 1, -1);
            SetSliderAnimation(slider, scale);

            Assert.That(
                SkelSbject.AnimationIgnoreBlendManager
                    .ClassifyScalePolicy(slider),
                Is.EqualTo(
                    SkelSbject.AnimationIgnoreBlendManager
                        .ScalePolicyKind.PureReflection));
        }

        [Test]
        public void ScalePolicyClassification_DoesNotTreatDoubleAxisFlipAsReflection()
        {
            var slider = new SliderData("half turn");
            var scale = new ScaleTimeline(2, 0, 0);
            scale.SetFrame(0, 0, 1, 1);
            scale.SetFrame(1, 0.02f, -1, -1);
            SetSliderAnimation(slider, scale);

            Assert.That(
                SkelSbject.AnimationIgnoreBlendManager
                    .ClassifyScalePolicy(slider),
                Is.EqualTo(
                    SkelSbject.AnimationIgnoreBlendManager
                        .ScalePolicyKind.None));
        }

        [Test]
        public void ScalePolicyClassification_RejectsMixedOrSingularScalePolicies()
        {
            var mixed = new SliderData("mixed");
            var mixedScale = new ScaleTimeline(2, 0, 0);
            mixedScale.SetFrame(0, 0, 1, 1);
            mixedScale.SetFrame(1, 0.02f, 1, -1);
            SetSliderAnimation(
                mixed,
                new RotateTimeline(1, 0, 0),
                mixedScale);

            var singular = new SliderData("singular");
            var singularScale = new ScaleTimeline(1, 0, 0);
            singularScale.SetFrame(0, 0, 1, 0);
            SetSliderAnimation(singular, singularScale);

            Assert.That(
                SkelSbject.AnimationIgnoreBlendManager
                    .ClassifyScalePolicy(mixed),
                Is.EqualTo(
                    SkelSbject.AnimationIgnoreBlendManager
                        .ScalePolicyKind.Ambiguous));
            Assert.That(
                SkelSbject.AnimationIgnoreBlendManager
                    .ClassifyScalePolicy(singular),
                Is.EqualTo(
                    SkelSbject.AnimationIgnoreBlendManager
                        .ScalePolicyKind.Ambiguous));
        }

        [Test]
        public void AutomaticScalePolicy_RequiresAControlDrivenSlider()
        {
            var controlKey = new SliderData("control key");
            var renderedPolicy = new SliderData("rendered policy")
            {
                Bone = new BoneData(0, "driver", null)
            };

            Assert.That(
                SkelSbject.AnimationIgnoreBlendManager
                    .IsAutoScalePolicyCandidate(controlKey),
                Is.False);
            Assert.That(
                SkelSbject.AnimationIgnoreBlendManager
                    .IsAutoScalePolicyCandidate(renderedPolicy),
                Is.True);
        }

        [Test]
        public void RotationPolicyRisk_DetectsOffsetAndNegativeMappingsOnly()
        {
            var data = new SkeletonData();
            var source = new BoneData(0, "source", null);
            var output = new BoneData(1, "output", null);
            data.Bones.Add(source);
            data.Bones.Add(output);

            var slider = new SliderData("policy");
            data.Constraints.Add(slider);
            TransformConstraintData transform = CreateRotationConstraint(
                "mapping",
                source,
                output);
            data.Constraints.Add(transform);
            SetSliderAnimation(
                slider,
                new TransformConstraintTimeline(1, 0, 1));

            Assert.That(
                SkelSbject.AnimationIgnoreBlendManager
                    .IsPotentialDiscontinuousRotationPolicy(data, slider),
                Is.False);

            transform.OffsetRotation = 180;
            Assert.That(
                SkelSbject.AnimationIgnoreBlendManager
                    .IsPotentialDiscontinuousRotationPolicy(data, slider),
                Is.True);

            transform.OffsetRotation = 0;
            var fromRotate = (TransformConstraintData.FromRotate)
                transform.Properties.Items[0];
            ((TransformConstraintData.ToRotate)fromRotate.to.Items[0]).scale = -1;
            Assert.That(
                SkelSbject.AnimationIgnoreBlendManager
                    .IsPotentialDiscontinuousRotationPolicy(data, slider),
                Is.True);
        }

        [Test]
        public void NestedRotationPolicy_PropagatesRiskAndCompensationRoute()
        {
            var data = new SkeletonData();
            var source = new BoneData(0, "source", null);
            var output = new BoneData(1, "output", null);
            data.Bones.Add(source);
            data.Bones.Add(output);

            var rootSlider = new SliderData("root policy");
            var nestedSlider = new SliderData("nested policy");
            data.Constraints.Add(rootSlider);
            data.Constraints.Add(nestedSlider);
            TransformConstraintData transform = CreateRotationConstraint(
                "nested mapping",
                source,
                output);
            transform.OffsetRotation = 180;
            data.Constraints.Add(transform);
            SetSliderAnimation(rootSlider, new SliderTimeline(1, 0, 1));
            SetSliderAnimation(
                nestedSlider,
                new TransformConstraintTimeline(1, 0, 2));

            Assert.That(
                SkelSbject.AnimationIgnoreBlendManager
                    .IsPotentialDiscontinuousRotationPolicy(data, rootSlider),
                Is.True);

            var routes = SkelSbject.AnimationIgnoreBlendManager
                .CollectRotationCompensationRoutes(data, new[] { 0 });

            Assert.That(routes, Has.Length.EqualTo(1));
            Assert.That(routes[0].PolicyConstraintIndex, Is.EqualTo(1));
            Assert.That(routes[0].InputBoneIndexes, Is.EqualTo(new[] { 0 }));
            Assert.That(routes[0].OutputBoneIndex, Is.EqualTo(1));
        }

        [Test]
        public void RotationRoutes_FollowUpstreamWriterAndDownstreamOutput()
        {
            SkeletonData data = CreatePlaneRouteData(
                includeMirroredInput: false,
                out int sliderIndex,
                out int actionLeftIndex,
                out _,
                out int outputIndex);

            var routes = SkelSbject.AnimationIgnoreBlendManager
                .CollectRotationCompensationRoutes(data, new[] { sliderIndex });

            Assert.That(routes, Has.Length.EqualTo(1));
            Assert.That(routes[0].PolicyConstraintIndex, Is.EqualTo(sliderIndex));
            Assert.That(routes[0].InputBoneIndexes, Is.EqualTo(new[] { actionLeftIndex }));
            Assert.That(routes[0].OutputBoneIndex, Is.EqualTo(outputIndex));
        }

        [Test]
        public void RotationRoutes_CollectBothDirectAndMirroredInputs()
        {
            SkeletonData data = CreatePlaneRouteData(
                includeMirroredInput: true,
                out int sliderIndex,
                out int actionLeftIndex,
                out int actionRightIndex,
                out int outputIndex);

            var routes = SkelSbject.AnimationIgnoreBlendManager
                .CollectRotationCompensationRoutes(data, new[] { sliderIndex });

            Assert.That(routes, Has.Length.EqualTo(1));
            Assert.That(
                routes[0].InputBoneIndexes,
                Is.EqualTo(new[] { actionLeftIndex, actionRightIndex }));
            Assert.That(routes[0].OutputBoneIndex, Is.EqualTo(outputIndex));
        }

        [Test]
        public void RotationRoutes_IgnoreZeroMixOrderingConstraint()
        {
            SkeletonData data = CreatePlaneRouteData(
                includeMirroredInput: true,
                out int sliderIndex,
                out int actionLeftIndex,
                out int actionRightIndex,
                out _);

            var routes = SkelSbject.AnimationIgnoreBlendManager
                .CollectRotationCompensationRoutes(data, new[] { sliderIndex });

            Assert.That(
                routes[0].InputBoneIndexes,
                Is.EqualTo(new[] { actionLeftIndex, actionRightIndex }));
        }

        [Test]
        public void RotationRoutes_IgnoreBoneScaleSliderAndTaggedTransform()
        {
            var data = new SkeletonData();
            var root = new BoneData(0, "root", null);
            data.Bones.Add(root);

            var graphicFlip = new SliderData("graphic flip");
            var equipment = new TransformConstraintData("equipment");
            data.Constraints.Add(graphicFlip);
            data.Constraints.Add(equipment);
            SetSliderAnimation(graphicFlip, new ScaleTimeline(1, 0, root.Index));

            var routes = SkelSbject.AnimationIgnoreBlendManager
                .CollectRotationCompensationRoutes(data, new[] { 0, 1 });

            Assert.That(routes, Is.Empty);
        }

        [Test]
        public void RotationRoutes_KeepDirectPolicyOutputWhenRenderedDescendantExists()
        {
            SkeletonData data = CreatePlaneRouteData(
                includeMirroredInput: false,
                out int sliderIndex,
                out _,
                out _,
                out int outputIndex);
            var rendered = new BoneData(
                data.Bones.Count,
                "rendered",
                data.Bones.Items[outputIndex]);
            data.Bones.Add(rendered);
            data.Slots.Add(new SlotData(0, "rendered slot", rendered));

            var routes = SkelSbject.AnimationIgnoreBlendManager
                .CollectRotationCompensationRoutes(data, new[] { sliderIndex });

            Assert.That(routes, Has.Length.EqualTo(1));
            Assert.That(routes[0].OutputBoneIndex, Is.EqualTo(outputIndex));
        }

        [Test]
        public void RotationRoutes_PreferCurrentPolicyWriterForSharedProxy()
        {
            var data = new SkeletonData();
            var policyInput = new BoneData(0, "policy input", null);
            var unrelatedInput = new BoneData(1, "unrelated input", null);
            var proxy = new BoneData(2, "proxy", null);
            var output = new BoneData(3, "output", null);
            data.Bones.Add(policyInput);
            data.Bones.Add(unrelatedInput);
            data.Bones.Add(proxy);
            data.Bones.Add(output);

            int unrelatedWriterIndex = data.Constraints.Count;
            TransformConstraintData unrelatedWriter = CreateRotationConstraint(
                "unrelated writer",
                unrelatedInput,
                proxy);
            unrelatedWriter.GetSetupPose().MixRotate = 0;
            data.Constraints.Add(unrelatedWriter);
            var unrelatedAnimation = new Spine.Animation("unrelated animation");
            unrelatedAnimation.SetTimelines(
                new ExposedList<Timeline>(new Timeline[]
                {
                    new TransformConstraintTimeline(1, 0, unrelatedWriterIndex)
                }),
                new ExposedList<int>());
            data.Animations.Add(unrelatedAnimation);

            int sliderIndex = data.Constraints.Count;
            var slider = new SliderData("policy");
            data.Constraints.Add(slider);
            int policyWriterIndex = data.Constraints.Count;
            data.Constraints.Add(CreateRotationConstraint(
                "policy writer",
                policyInput,
                proxy));
            int applyIndex = data.Constraints.Count;
            data.Constraints.Add(CreateRotationConstraint(
                "apply",
                proxy,
                output));
            SetSliderAnimation(
                slider,
                new TransformConstraintTimeline(1, 0, policyWriterIndex),
                new TransformConstraintTimeline(1, 0, applyIndex));

            var routes = SkelSbject.AnimationIgnoreBlendManager
                .CollectRotationCompensationRoutes(data, new[] { sliderIndex });

            Assert.That(routes, Has.Length.EqualTo(1));
            Assert.That(routes[0].InputBoneIndexes, Is.EqualTo(new[] { policyInput.Index }));
            Assert.That(routes[0].OutputBoneIndex, Is.EqualTo(output.Index));
        }

        [Test]
        public void PoseRetargetRoutes_FollowAnimatedInputToFinalOutput()
        {
            SkeletonData data = CreatePlaneRouteData(
                includeMirroredInput: false,
                out int sliderIndex,
                out int actionLeftIndex,
                out _,
                out int outputIndex);
            var animation = new Spine.Animation("motion");
            var animatedBones = new ExposedList<int>();
            animatedBones.Add(actionLeftIndex);
            animation.SetTimelines(
                new ExposedList<Timeline>(),
                animatedBones);

            var routes = SkelSbject.AnimationIgnoreBlendManager
                .CollectAnimationBoneRotationRoutes(
                    data,
                    animation,
                    sliderIndex);

            Assert.That(routes, Has.Length.EqualTo(1));
            Assert.That(routes[0].InputBoneIndexes, Is.EqualTo(new[] { actionLeftIndex }));
            Assert.That(routes[0].OutputBoneIndex, Is.EqualTo(outputIndex));
        }



        [Test]
        public void CoupledRotationSolver_UsesSharedInputsWithoutGreedyConflicts()
        {
            float[,] sensitivities =
            {
                { 1f, 1f },
                { 1f, -1f }
            };

            bool solved = SkelSbject.AnimationIgnoreBlendManager.TrySolveLeastSquares(
                sensitivities,
                new[] { 30f, 10f },
                out float[] offsets);

            Assert.That(solved, Is.True);
            Assert.That(offsets[0], Is.EqualTo(20f).Within(0.01f));
            Assert.That(offsets[1], Is.EqualTo(10f).Within(0.01f));
        }

        [TestCase(0, 350, 360)]
        [TestCase(0, 710, 720)]
        [TestCase(90, -260, -270)]
        public void RotationRebase_KeepsNearestEquivalentWinding(
            float baseRotation,
            float previousAppliedRotation,
            float expected)
        {
            float result = SkelSbject.AnimationIgnoreBlendManager
                .RebaseRotation(baseRotation, previousAppliedRotation);

            Assert.That(result, Is.EqualTo(expected).Within(0.0001f));
        }

        [TestCase(0, -104, -104)]
        [TestCase(-170, 170, -190)]
        public void RotationUnwrap_ChoosesSourceBranchNearestDestination(
            float destination,
            float source,
            float expected)
        {
            float result = SkelSbject.AnimationIgnoreBlendManager
                .UnwrapRotationNear(destination, source);

            Assert.That(result, Is.EqualTo(expected).Within(0.0001f));
        }

        [TestCase(90, 0, 0.25f, 67.5f)]
        [TestCase(-170, 170, 0.5f, -180f)]
        [TestCase(170, -170, 0.5f, 180f)]
        public void RotationTarget_FollowsFixedDestinationOnShortestSourceBranch(
            float source,
            float currentTarget,
            float alpha,
            float expected)
        {
            float result = SkelSbject.AnimationIgnoreBlendManager
                .GetShortestRotationTarget(source, currentTarget, alpha);

            Assert.That(result, Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void DynamicRotationTarget_EndsAtTheCurrentRenderedBranch()
        {
            float destination = SkelSbject.AnimationIgnoreBlendManager
                .GetContinuousDestinationRotation(90, 170, -179);
            float target = SkelSbject.AnimationIgnoreBlendManager
                .GetShortestRotationTarget(90, destination, 1);

            Assert.That(destination, Is.EqualTo(181).Within(0.0001f));
            Assert.That(Mathf.DeltaAngle(-179, target), Is.Zero.Within(0.0001f));
        }

        [Test]
        public void DynamicRotationTarget_KeepsTheInitialSourceBranch()
        {
            float sourceBranch = SkelSbject.AnimationIgnoreBlendManager
                .GetStableSourceBranch(90, -80, float.NaN);
            float stableBranch = SkelSbject.AnimationIgnoreBlendManager
                .GetStableSourceBranch(90, -100, sourceBranch);

            Assert.That(sourceBranch, Is.EqualTo(90).Within(0.0001f));
            Assert.That(stableBranch, Is.EqualTo(90).Within(0.0001f));
        }

        [Test]
        public void DynamicRotationTarget_DoesNotSwitchAContinuousDestinationBranch()
        {
            float destination = SkelSbject.AnimationIgnoreBlendManager
                .GetContinuousDestinationRotation(90, -80, -100);
            float target = SkelSbject.AnimationIgnoreBlendManager
                .GetContinuousRotationTarget(90, destination, 0.25f);

            Assert.That(destination, Is.EqualTo(-100).Within(0.0001f));
            Assert.That(target, Is.EqualTo(94.925f).Within(0.001f));
        }

        [TestCase(227.9489f, -100.5682f)]
        [TestCase(-227.9489f, 100.5682f)]
        public void InterruptedRotation_IgnoresEquivalentFullTurns(float source, float destination)
        {
            var state = new SkelSbject.AnimationIgnoreBlendManager.RotationBlendState { Rotation = float.NaN };
            float previous = source;
            float travel = 0;
            for (int frame = 0; frame <= 60; frame++)
            {
                float alpha = frame / 60f;
                float target = SkelSbject.AnimationIgnoreBlendManager.GetContinuousRotationTarget(
                    source, destination, alpha, ref state);
                travel += Mathf.DeltaAngle(previous, target);
                previous = target;
            }
            Assert.That(travel, Is.EqualTo(Mathf.DeltaAngle(source, destination)).Within(0.001f));
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void NearOppositeDirections_DoNotConcentrateRotationAtHalfMix(bool moving, bool mirrored)
        {
            var state = new SkelSbject.AnimationIgnoreBlendManager.RotationBlendState { Rotation = float.NaN };
            float sign = mirrored ? -1f : 1f;
            float source = 30f * sign;
            float previous = source;
            for (int frame = 0; frame <= 100; frame++)
            {
                float alpha = frame / 100f;
                float destination = (moving ? Mathf.Lerp(-235f, -69f, alpha) : 209f) * sign;
                float target = SkelSbject.AnimationIgnoreBlendManager.GetContinuousRotationTarget(
                    source, destination, alpha, ref state);
                //? 남은 반 구간의 180도 회전과 목적지 자체의 프레임 이동량을 함께 허용한다.
                float maxStep = 3.6f + (moving ? 1.66f : 0f);
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(previous, target)), Is.LessThan(maxStep),
                    "Rotation concentrated in one percent of the mix at frame " + frame);
                previous = target;
            }
            Assert.That(Mathf.DeltaAngle(previous, (moving ? -69f : 209f) * sign), Is.Zero.Within(0.001f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OppositeDirections_DoNotSnapAtHalfMix(bool moving)
        {
            var state = new SkelSbject.AnimationIgnoreBlendManager.RotationBlendState { Rotation = float.NaN };
            float previous = 0;
            for (int frame = 0; frame <= 100; frame++)
            {
                float alpha = frame / 100f;
                float destination = moving ? Mathf.Lerp(170, 190, alpha) : 180;
                float target = SkelSbject.AnimationIgnoreBlendManager.GetContinuousRotationTarget(
                    0, destination, alpha, ref state);
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(previous, target)), Is.LessThan(15f), "frame " + frame);
                previous = target;
            }
            Assert.That(Mathf.DeltaAngle(previous, moving ? 190 : 180), Is.Zero.Within(0.001f));
        }

        [Test]
        public void CoupledRotationOffset_KeepsTheNearestEquivalentBranch()
        {
            float result = SkelSbject.AnimationIgnoreBlendManager
                .GetContinuousRotationOffset(170, -170);

            Assert.That(result, Is.EqualTo(190).Within(0.0001f));
        }

        [Test]
        public void CoupledRotationOffset_AccumulatesFromThePreviousFrame()
        {
            float result = SkelSbject.AnimationIgnoreBlendManager
                .AccumulateCoupledRotationOffset(200, -10);

            Assert.That(result, Is.EqualTo(190).Within(0.0001f));
        }

        [Test]
        public void AppliedRotation_IsRestoredOnlyWhileTheOffsetStillRemains()
        {
            Assert.IsTrue(SkelSbject.AnimationIgnoreBlendManager
                .ShouldRestoreAppliedRotation(210, 10, 200));
            Assert.IsFalse(SkelSbject.AnimationIgnoreBlendManager
                .ShouldRestoreAppliedRotation(15, 10, 200));
        }

        [Test]
        public void PolarRotation_TreatsScaleYReflectionAsDiscretePolicy()
        {
            bool positiveValid = SkelSbject.AnimationIgnoreBlendManager
                .TryGetPolarRotation(
                    0,
                    -1,
                    1,
                    0,
                    out float positiveRotation,
                    out int positiveSign);
            bool reflectedValid = SkelSbject.AnimationIgnoreBlendManager
                .TryGetPolarRotation(
                    0,
                    1,
                    1,
                    0,
                    out float reflectedRotation,
                    out int reflectedSign);

            Assert.That(positiveValid, Is.True);
            Assert.That(reflectedValid, Is.True);
            Assert.That(positiveSign, Is.EqualTo(1));
            Assert.That(reflectedSign, Is.EqualTo(-1));
            Assert.That(positiveRotation, Is.EqualTo(90).Within(0.0001f));
            Assert.That(reflectedRotation, Is.EqualTo(90).Within(0.0001f));
        }

        [Test]
        public void PolarRotation_RejectsSingularScale()
        {
            bool valid = SkelSbject.AnimationIgnoreBlendManager
                .TryGetPolarRotation(
                    1,
                    0,
                    0,
                    0,
                    out _,
                    out int determinantSign);

            Assert.That(valid, Is.False);
            Assert.That(determinantSign, Is.Zero);
        }

        [Test]
        public void RelativePolarRotation_RemovesParentReflectionBeforeChoosingJointPath()
        {
            bool valid = SkelSbject.AnimationIgnoreBlendManager
                .TryGetRelativePolarRotation(
                    0, -1, -1, 0,
                    1, 0, 0, -1,
                    out float rotation,
                    out float determinant);

            Assert.That(valid, Is.True);
            Assert.That(rotation, Is.EqualTo(90).Within(0.0001f));
            Assert.That(determinant, Is.EqualTo(1).Within(0.0001f));
        }

        [Test]
        public void PendingExitTransition_IsPreservedForSameFrameReplacement()
        {
            Assert.That(
                SkelSbject.AnimationIgnoreBlendManager
                    .ShouldPreservePendingSourcePolicies(true, true, -1, 1),
                Is.True);
            Assert.That(
                SkelSbject.AnimationIgnoreBlendManager
                    .ShouldPreservePendingSourcePolicies(true, false, -1, 1),
                Is.False);
            Assert.That(
                SkelSbject.AnimationIgnoreBlendManager
                    .ShouldPreservePendingSourcePolicies(true, true, 10, 1),
                Is.False);
        }

        [TestCase(0.5f, 0.15f, 0.5f)]
        [TestCase(0.1f, 0.3f, 0.3f)]
        public void ReplacementMix_UsesLongerExitOrEntryDuration(
            float endMixDuration,
            float startMixDuration,
            float expected)
        {
            Assert.That(
                SkelObject.AniCore.ResolveReplacementMixDuration(
                    endMixDuration,
                    startMixDuration),
                Is.EqualTo(expected));
        }

        private static SkeletonData CreatePlaneRouteData(
            bool includeMirroredInput,
            out int sliderIndex,
            out int actionLeftIndex,
            out int actionRightIndex,
            out int outputIndex)
        {
            var data = new SkeletonData();
            var orderingRoot = new BoneData(0, "ordering root", null);
            var actionLeft = new BoneData(1, "action left", null);
            var actionRight = new BoneData(2, "action right", null);
            var motionInput = new BoneData(3, "motion input", null);
            var proxy = new BoneData(4, "proxy", null);
            var output = new BoneData(5, "output", null);
            data.Bones.Add(orderingRoot);
            data.Bones.Add(actionLeft);
            data.Bones.Add(actionRight);
            data.Bones.Add(motionInput);
            data.Bones.Add(proxy);
            data.Bones.Add(output);

            TransformConstraintData ordering = CreateRotationConstraint(
                "ordering only",
                orderingRoot,
                actionLeft);
            ordering.Bones.Add(actionRight);
            ordering.GetSetupPose().MixRotate = 0;
            data.Constraints.Add(ordering);

            data.Constraints.Add(CreateRotationConstraint(
                "direct",
                actionLeft,
                motionInput));

            if (includeMirroredInput)
            {
                data.Constraints.Add(CreateRotationConstraint(
                    "mirror",
                    actionRight,
                    motionInput));
            }

            sliderIndex = data.Constraints.Count;
            var slider = new SliderData("plane slider");
            data.Constraints.Add(slider);

            int planeIndex = data.Constraints.Count;
            data.Constraints.Add(CreateRotationConstraint(
                "plane",
                motionInput,
                proxy));
            data.Constraints.Add(CreateRotationConstraint(
                "apply",
                proxy,
                output));
            SetSliderAnimation(
                slider,
                new TransformConstraintTimeline(1, 0, planeIndex));

            actionLeftIndex = actionLeft.Index;
            actionRightIndex = actionRight.Index;
            outputIndex = output.Index;
            return data;
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void PoseRetarget_PreservesEachEndpointsJointPolicies(bool sourceDepth, bool destinationDepth)
        {
            var data = new SkeletonData();
            var root = new BoneData(0, "view", null);
            var input = new BoneData(1, "input", root);
            var outputA = new BoneData(2, "joint A", null);
            var outputB = new BoneData(3, "joint B", null);
            data.Bones.Add(root);
            data.Bones.Add(input);
            data.Bones.Add(outputA);
            data.Bones.Add(outputB);
            foreach (BoneData bone in data.Bones)
            {
                bone.GetSetupPose().ScaleX = 1;
                bone.GetSetupPose().ScaleY = 1;
            }
            input.GetSetupPose().Rotation = 30;
            foreach (BoneData output in new[] { outputA, outputB })
            {
                data.Constraints.Add(CreateRotationConstraint("world " + output.Name, input, output));
                TransformConstraintData screen = CreateRotationConstraint("local " + output.Name, input, output);
                screen.LocalSource = true;
                data.Constraints.Add(screen);
            }

            foreach (TransformConstraintData constraint in data.Constraints)
            {
                var mapping = (TransformConstraintData.FromRotate)constraint.Properties.Items[0];
                ((TransformConstraintData.ToRotate)mapping.to.Items[0]).scale = 1;
            }

            var rightTimeline = new ScaleTimeline(1, 0, root.Index);
            rightTimeline.SetFrame(0, 0, 1, 1);
            var leftTimeline = new ScaleTimeline(1, 0, root.Index);
            leftTimeline.SetFrame(0, 0, -1, 1);
            var right = new Spine.Animation("view positive");
            right.SetTimelines(new ExposedList<Timeline>(new Timeline[] { rightTimeline }), new ExposedList<int>());
            var left = new Spine.Animation("view negative");
            left.SetTimelines(new ExposedList<Timeline>(new Timeline[] { leftTimeline }), new ExposedList<int>());
            var animationState = new Spine.AnimationState(new AnimationStateData(data));

            foreach (bool depth in new[] { sourceDepth, destinationDepth })
            {
                var pose = new Skeleton(data);
                pose.SetupPose();
                for (int i = 0; i < 4; i++)
                {
                    bool enabled = i < 2 ? (i == 0) == depth : (i == 2) != depth;
                    ((TransformConstraint)pose.Constraints.Items[i]).Pose.MixRotate = enabled ? 1 : 0;
                }

                //? 같은 입력이어도 관절별 정책은 다르다. 출발·도착 양쪽에 같은 처리를 사용한다.
                TrackEntry entry = animationState.SetAnimation(1, left, false);
                SkelSbject.AnimationIgnoreBlendManager.ApplyPoseRetargetTrack(pose, right, entry);
                pose.UpdateWorldTransform(Spine.Physics.Pose);
                float angleA = Mathf.Atan2(pose.Bones.Items[2].AppliedPose.C, pose.Bones.Items[2].AppliedPose.A) * Mathf.Rad2Deg;
                float angleB = Mathf.Atan2(pose.Bones.Items[3].AppliedPose.C, pose.Bones.Items[3].AppliedPose.A) * Mathf.Rad2Deg;
                Assert.That(Mathf.DeltaAngle(depth ? 150 : 30, angleA), Is.EqualTo(0).Within(0.01f));
                Assert.That(Mathf.DeltaAngle(depth ? 30 : 150, angleB), Is.EqualTo(0).Within(0.01f));

                //? 이전 트랙만 가진 키를 지우며, 반복 전환에서도 반전을 누적하지 않는다.
                SkelSbject.AnimationIgnoreBlendManager.ApplyPoseRetargetTrack(pose, left, null);
                pose.UpdateWorldTransform(Spine.Physics.Pose);
                Assert.That(pose.Bones.Items[0].Pose.ScaleX, Is.EqualTo(1));
                Assert.That(pose.Bones.Items[1].Pose.Rotation, Is.EqualTo(30));
                Assert.That(((TransformConstraint)pose.Constraints.Items[0]).Pose.MixRotate, Is.EqualTo(depth ? 1 : 0));
            }
        }



        private static TransformConstraintData CreateRotationConstraint(
            string name,
            BoneData source,
            BoneData output)
        {
            var constraint = new TransformConstraintData(name);
            SetTransformSource(constraint, source);
            constraint.Bones.Add(output);
            constraint.GetSetupPose().MixRotate = 1;

            var from = new TransformConstraintData.FromRotate();
            from.to.Add(new TransformConstraintData.ToRotate());
            constraint.Properties.Add(from);
            return constraint;
        }

        private static void SetTransformSource(
            TransformConstraintData constraint,
            BoneData source)
        {
            typeof(TransformConstraintData)
                .GetField("source", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(constraint, source);
        }

        private static void SetSliderAnimation(
            SliderData sliderData,
            params Timeline[] timelines)
        {
            var animation = new Spine.Animation(sliderData.Name + " animation");
            animation.SetTimelines(
                new ExposedList<Timeline>(timelines),
                new ExposedList<int>());

            typeof(SliderData)
                .GetField("animation", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(sliderData, animation);
        }
    }
}
