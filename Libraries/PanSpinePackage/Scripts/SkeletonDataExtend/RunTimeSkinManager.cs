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
using Spine.Unity.AnimationTools;
using static Pan.SpinePackage.SkelObject.AniCore.Players;
using static Pan.SpinePackage.SkelSbject;
using JetBrains.Annotations;
using Pan.SpineUtil;
using ZLinq;



namespace Pan.SpinePackage
{
    /// <summary>
    /// 런타임 스킨 매니저 Base
    /// </summary>
    /// <typeparam name="TSkin">반드시 특정 허용된 스킨만 사용해야함</typeparam>
    [Serializable]
    public abstract class BaseRunTimeSkinManager<TSkin>
    {
        ///======================================================================================================================================================



        public BaseRunTimeSkinManager(SkeletonAnimation skeletonAnimation, string huskSkinName, int mixDictionaryCapacity = 0)
        {
            SkeletonAnimation = skeletonAnimation;
            HuskSkin = new Skin(huskSkinName);
            CurrentMixSkins = new Dictionary<int, TSkin>(mixDictionaryCapacity);
            CurrentMixSkinAttachmentOnlyModes = new Dictionary<int, bool>(mixDictionaryCapacity);
            CurrentMixSkinSortBuffer = new List<KeyValuePair<int, TSkin>>(mixDictionaryCapacity);
        }



        ///======================================================================================================================================================



        //? 런타임 변수



        ///<summary>
        ///대상 SkeletonAnimation
        /// </summary>
        protected readonly SkeletonAnimation SkeletonAnimation;



        ///<summary>
        ///대상 Skeleton
        /// </summary>
        protected Skeleton RunTimeSkeleton => SkeletonAnimation != null ? SkeletonAnimation.Skeleton : null;



        ///======================================================================================================================================================



        ///<summary>
        /// 스킨의 기반이 되는 메인 스킨.<br/>
        /// 껍질 스킨의 일부를 내려놓을때, 기존 상태를 비교하기위해 존재한다, 그래서 이 메인 스킨이 기반이 된다.<br/>
        /// 이 메인스킨은 참조로 관리된다
        ///</summary>
        public Skin MainSkin { get; protected set; }



        /// <summary>
        /// 껍데기 스킨<br/>
        /// 참조로 할당되는것이아닌, 이 매니저에 생성되어 존재하는 껍데기 스킨<br/>
        /// 이 스킨에 스킨들을 올려놓고, 내려놓은뒤,<br/>
        /// 이 스킨을 런타임에 통째로 할당 하는 방식으로 구현된다
        /// </summary>
        protected readonly Skin HuskSkin;



        /// <summary>
        /// 섞여잇는 스킨을 각각 보관하는 딕셔너리
        /// </summary>
        protected readonly Dictionary<int, TSkin> CurrentMixSkins;



        /// <summary>
        /// 각 레이어가 attachment-only 방식으로 합성되었는지 보관합니다.
        /// </summary>
        private readonly Dictionary<int, bool> CurrentMixSkinAttachmentOnlyModes;



        private readonly List<KeyValuePair<int, TSkin>> CurrentMixSkinSortBuffer;



        private static readonly Comparison<KeyValuePair<int, TSkin>> CompareMixSkinRank =
            static (left, right) => left.Key.CompareTo(right.Key);



        /// <summary>
        /// 섞여잇는 스킨을 각각 보관하는 딕셔너리 얻기
        /// </summary>
        public IReadOnlyDictionary<int, TSkin> GetCurrentMixSkins => CurrentMixSkins;



        /// <summary>
        /// 레이어 상태를 추가하거나 교체합니다.
        /// </summary>
        internal bool TrySetMixSkin(
            TSkin skin,
            int skinLayerRank,
            bool overlap,
            bool putOnlyAttachment)
        {
            if (ReferenceEquals(skin, null)) { return false; }

            bool isOverlapped = CurrentMixSkins.TryGetValue(skinLayerRank, out TSkin currentSkin);
            if (isOverlapped &&
                (!overlap || EqualityComparer<TSkin>.Default.Equals(currentSkin, skin)))
            {
                return false;
            }

            CurrentMixSkins[skinLayerRank] = skin;
            CurrentMixSkinAttachmentOnlyModes[skinLayerRank] = putOnlyAttachment;
            return true;
        }



        /// <summary>
        /// 같은 레이어에 실제로 올라간 같은 스킨일 때만 상태를 제거합니다.
        /// </summary>
        internal bool TryRemoveMixSkin(TSkin skin, int skinLayerRank)
        {
            if (!CurrentMixSkins.TryGetValue(skinLayerRank, out TSkin currentSkin) ||
                ReferenceEquals(currentSkin, null) ||
                !EqualityComparer<TSkin>.Default.Equals(currentSkin, skin))
            {
                return false;
            }

            return RemoveMixSkinState(skinLayerRank);
        }



        /// <summary>
        /// 레이어 식별자로 상태를 제거합니다.
        /// </summary>
        internal bool TryRemoveMixSkin(int skinLayerRank)
        {
            return CurrentMixSkins.ContainsKey(skinLayerRank) &&
                   RemoveMixSkinState(skinLayerRank);
        }



        private bool RemoveMixSkinState(int skinLayerRank)
        {
            CurrentMixSkinAttachmentOnlyModes.Remove(skinLayerRank);
            return CurrentMixSkins.Remove(skinLayerRank);
        }



        /// <summary>
        /// MainSkin과 현재 mix skin을 rank 오름차순으로 다시 합성합니다.
        /// 높은 rank는 나중에 적용되어 같은 slot/placeholder를 덮어씁니다.
        /// </summary>
        internal void RebuildHuskSkin()
        {
            HuskSkin.Clear();

            if (MainSkin != null)
            {
                HuskSkin.CopySkin(MainSkin);
            }

            CurrentMixSkins.AsValueEnumerable().CopyTo(CurrentMixSkinSortBuffer);
            CurrentMixSkinSortBuffer.Sort(CompareMixSkinRank);

            foreach (KeyValuePair<int, TSkin> mixSkin in CurrentMixSkinSortBuffer)
            {
                Skin spineSkin = SU_SpinePackage.GetSkin_UnknownTypeSkin(mixSkin.Value);
                if (spineSkin == null) { continue; }

                bool putOnlyAttachment =
                    CurrentMixSkinAttachmentOnlyModes.TryGetValue(mixSkin.Key, out bool attachmentOnly) &&
                    attachmentOnly;

                if (putOnlyAttachment)
                {
                    HuskSkin.SetSkin_OnlyAttachment(spineSkin);
                }
                else
                {
                    HuskSkin.AddSkin(spineSkin);
                }
            }
        }



        ///======================================================================================================================================================



        ///<summary>
        /// 껍질 스킨을 런타임 스킨에 적용(SetSkin)한다
        /// </summary>
        protected void ApplyHuskSkinToRunTimeSkin(bool isMixSkin = false)
        {
            Skeleton runtimeSkeleton = RunTimeSkeleton;
            if (runtimeSkeleton == null) { return; }

            //NullRuntimeSkin();
            //! 250309 비활성화, 활성화시 어태치먼트만을 바꾸는 스킨을 얹을때 잠깐 1프레임동안 스킨이 깜?빡이듯이 적용 될 때가 간헐적으로 발생했음
            //! 250320 비활성화로 인한 문제 발견, 제약조건이 포함된 스킨이 적용될때, 제약조건이 제대로 적용되지 않음
            //? 250320 따라서 스킨이 적용 된 이후, 수동으로 즉시 갱신되게끔 하여 깜빡임 문제 해결

            NullRuntimeSkin();


            runtimeSkeleton.SetSkin(HuskSkin);

            //SkeletonAnimation.AnimationState.Apply(RunTimeSkeleton); //. 애니메이션에 즉시 적용하여 깜빡임을 제거
            //? 250321 위 Apply로직을 사용할경우, 이 메서드의 호출과 동시에 애니메이션이 종료될때, 애니메이션의 Blend/MixDuration이 적용되지 않는 문제 발생
            //! 아래와 같이 Update 로직으로 대체하여, 문제를 해결

            RefreshSkeletonAfterSkinChanged();


            //! 런타임 Skin을 null 시키지 않고 SetSkin을 하게 되면, 제대로 적용되지 않음 (제약조건 등등 미적용)
            //! 따라서 null 시킨 이후에 적용해야한다

            #region 테스트 디버깅

            //StringBuilder sb = new StringBuilder();

            //sb.AppendLine($"HuskSkin을 런타임에 SetSkin");
            //sb.AppendLine($"Husk Name: {HuskSkin.Name}");
            //sb.AppendLine($"<color=red>Bones</color>: {string.Join(",\n", HuskSkin.Bones.Select(x => x.Name))}");
            //sb.AppendLine($"<color=red>Cons</color>: {string.Join(",\n", HuskSkin.Constraints.Select(x => x.Name))}");
            ////sb.AppendLine($"<color=red>Att</color>: {string.Join(",\n", HuskSkin.Attachments.Select(x => x.Name))}");
            //sb.AppendLine($"Main Name: {RunTimeSkeleton.Skin.Name}");
            //sb.AppendLine($"<color=blue>Bones</color>: {string.Join(",\n", RunTimeSkeleton.Skin.Bones.Select(x => x.Name))}");
            //sb.AppendLine($"<color=blue>Cons</color>: {string.Join(",\n", RunTimeSkeleton.Skin.Constraints.Select(x => x.Name))}");
            ////sb.AppendLine($"<color=blue>Att</color>: {string.Join(",\n", RunTimeSkeleton.Skin.Attachments.Select(x => x.Name))}");

            //foreach (var item in RunTimeSkeleton.Skin.Constraints)
            //{
            //    sb.AppendLine($"런타임 제약조건 상태 {item.Name} : {item.SkinRequired}");
            //}
            //Debug.Log(sb.ToString());

            #endregion
        }



        /// <summary>
        /// 껍데기 스킨을 Clear하고, MainSkin을 null 시킨다
        /// </summary>
        protected void ClearSkins()
        {
            HuskSkin.Clear();
            MainSkin = null;
        }



        /// <summary>
        /// 런타임 스킨을 null시킨다
        /// </summary>
        protected void NullRuntimeSkin()
        {
            //? 생성자에서 이걸 호출해버리면, RunTimeSkeleton이 null일수도있어서 검사함
            Skeleton runtimeSkeleton = RunTimeSkeleton;
            if (runtimeSkeleton != null) runtimeSkeleton.Skin = null;
        }



        protected virtual void RefreshSkeletonAfterSkinChanged()
        {
            SkeletonAnimation.Update(0f);
            var renderer = SkeletonAnimation.Renderer;
            if (renderer != null && renderer.IsValid)
            {
                renderer.UpdateMesh(); //. 메쉬를 즉시 갱신하여 깜빡임을 제거
            }
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 모든 스킨들 초기화
        /// </summary>
        public virtual void ResetAllSkin()
        {
            ClearSkins();
            NullRuntimeSkin();
            CurrentMixSkins.Clear();
            CurrentMixSkinAttachmentOnlyModes.Clear();
            CurrentMixSkinSortBuffer.Clear();
        }



        ///======================================================================================================================================================



        //? 메인 스킨 지정/제거



        ///<summary>
        ///기반이 되는 <b>MainSkin</b>을 설정한다.<br/>
        ///<i>런타임에도 적용됨</i>
        ///</summary>
        /// <param name="clearBeforeApply">스킨을 적용하기전에 Clear 이후 적용하기</param>
        public void SetMainSkin(Skin mainSkin, bool clearBeforeApply = false, bool setSlotsToSetupPose = false)
        {
            if (mainSkin == null) { throw new ArgumentNullException(nameof(mainSkin)); }
            if (clearBeforeApply)
            {
                CurrentMixSkins.Clear();
                CurrentMixSkinAttachmentOnlyModes.Clear();
            }

            MainSkin = mainSkin; //. 메인 스킨 지정
            RebuildHuskSkin();

            ApplyHuskSkinToRunTimeSkin(); //? 런타임 스킨에 적용한다

            if (setSlotsToSetupPose) { SetSlotsToSetupPose(); }
        }



        ///<summary>
        ///기반이 되는 <b>MainSkin</b>을 제거한다.<br/>
        ///<i>런타임에도 적용됨</i>
        ///</summary>
        public virtual bool RemoveMainSkin(bool setSlotsToSetupPose = false)
        {
            if (MainSkin == null) { return false; }

            MainSkin = null;
            RebuildHuskSkin();
            ApplyHuskSkinToRunTimeSkin();

            if (setSlotsToSetupPose) { SetSlotsToSetupPose(); }

            return true;
        }



        ///======================================================================================================================================================



        ///<summary>
        /// 믹스 스킨을 가져와 올려놓는다
        /// </summary>
        /// <param name="skin"></param>
        /// <param name="overlap"></param>
        /// <param name="putOnlyAttachment">어태치먼트만 적용하기</param>
        /// <param name="setSlotsToSetupPose"></param>
        public abstract bool PutUpSkin(TSkin skin, int skinLayerRank, bool overlap = false, bool putOnlyAttachment = false, bool setSlotsToSetupPose = false);



        /// <summary>
        /// 믹스 스킨을 꺼내 내려놓는다
        /// </summary>
        /// <param name="wasAddedSkin"></param>
        /// <param name="setSlotsToSetupPose"></param>
        /// <returns></returns>
        public abstract bool PutDownSkin(TSkin wasAddedSkin, int wasAddedSkinLayerRank, bool setSlotsToSetupPose = false);



        /// <summary>기존 스킨에 추가했던 스킨 빼기 (레이어)</summary>
        /// <param name="layerRank">레이어 랭크</param>
        /// <param name="changedAttachments">바뀌었던 어태치먼트 배열</param>
        public virtual bool PutDownSkin(IReadOnlyList<Skin.SkinEntry> changedAttachments, int layerRank, bool setSlotsToSetupPose = false)
        {
            if (!TryRemoveMixSkin(layerRank)) { return false; }

            RebuildHuskSkin();
            ApplyHuskSkinToRunTimeSkin();

            if (setSlotsToSetupPose) { SetSlotsToSetupPose(); }

            return true;
        }



        /// <summary>
        /// 보통 테스트용으로 사용하는, 강제스킨 Mix
        /// <para>런타임 스켈레톤의 <see cref="Spine.Skin"/>에 즉시 <see cref="Spine.Skin.AddSkin(Skin)"/>한다</para>
        /// </summary>
        /// <param name="skin"></param>
        public void AddMixSkinRunTime_Force(Skin skin)
        {
            RunTimeSkeleton.Skin.AddSkin(skin);
        }



        ///======================================================================================================================================================



        protected void SetSlotsToSetupPose()
        {
            RunTimeSkeleton?.SetupPoseSlots();
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 스파인 기본 <see cref="Spine.Skeleton"/>을 사용하는 런타임 스킨 매니저
    /// </summary>
    [Serializable]
    public sealed class RunTimeSkinManager_Skin : BaseRunTimeSkinManager<Skin>
    {
        ///======================================================================================================================================================



        public RunTimeSkinManager_Skin(SkeletonAnimation skeletonAnimation, string huskSkinName, int mixDictionaryCapacity = 0) : base(skeletonAnimation, huskSkinName, mixDictionaryCapacity)
        {

        }



        ///======================================================================================================================================================



        //? 믹스 스킨



        ///<summary>
        /// 믹스 스킨을 가져와 올려놓는다
        /// </summary>
        /// <param name="skin"></param>
        /// <param name="overlap"></param>
        /// <param name="putOnlyAttachment">어태치먼트만 적용하기</param>
        /// <param name="setSlotsToSetupPose"></param>
        public override bool PutUpSkin(Skin skin, int skinLayerRank, bool overlap = false, bool putOnlyAttachment = false, bool setSlotsToSetupPose = false)
        {
            if (!TrySetMixSkin(skin, skinLayerRank, overlap, putOnlyAttachment)) { return false; }

            RebuildHuskSkin();
            ApplyHuskSkinToRunTimeSkin(); //? 런타임 스킨에 적용한다

            if (setSlotsToSetupPose) { SetSlotsToSetupPose(); }

            return true;
        }



        /// <summary>
        /// 믹스 스킨을 꺼내 내려놓는다
        /// </summary>
        /// <param name="wasAddedSkin"></param>
        /// <param name="setSlotsToSetupPose"></param>
        /// <returns></returns>
        public override bool PutDownSkin(Skin wasAddedSkin, int wasAddedSkinLayerRank, bool setSlotsToSetupPose = false)
        {
            if (!TryRemoveMixSkin(wasAddedSkin, wasAddedSkinLayerRank)) { return false; }

            RebuildHuskSkin();
            ApplyHuskSkinToRunTimeSkin(); //? 런타임 스킨에 적용한다


            if (setSlotsToSetupPose) { SetSlotsToSetupPose(); }


            return true;
        }



        ///======================================================================================================================================================
    }
}
