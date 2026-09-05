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
            /// 스파인 확장<see cref="ExtendSkin"/>, <see cref="Pan.SpinePackage.SkelObject"/>를 사용하는
            /// <para>런타임 스킨 매니저</para>
            /// </summary>
            [Serializable]
            public sealed class RunTimeSkinManager : BaseRunTimeSkinManager<ExtendSkin>
            {
                ///======================================================================================================================================================



                public RunTimeSkinManager(SkelObject skelObject, SkeletonAnimation skeletonAnimation, string huskSkinName, int mixDictionaryCapacity = 0) : base(skeletonAnimation, huskSkinName, mixDictionaryCapacity)
                {
                    SkelObject = skelObject;
                }



                ///======================================================================================================================================================



                private readonly SkelObject SkelObject;



                /// <summary>
                /// 이 매니저의 메인 Advanced 스킨<br/>
                /// 참조로 관리된다
                /// </summary>
                public SkelSbject.BaseAdvancedSkin MainAdvancedSkin { get; private set; }



                ///======================================================================================================================================================



                ///<inheritdoc/>
                public override void ResetAllSkin()
                {
                    base.ResetAllSkin();
                    RemoveMainAdvancedSkin();
                }



                ///======================================================================================================================================================



                //? 메인 스킨 지정/제거 (SkelObject)



                ///<summary>MainSkin 설정하기 (ExtendSkin)</summary>
                /// <param name="clearBeforeApply">스킨을 적용하기전에 Clear 이후 적용하기</param>
                public void SetMainSkin(ExtendSkin extendSkin, bool clearBeforeApply = false, bool setSlotsToSetupPose = false)
                {
                    SetMainSkin(extendSkin.MainSkin, clearBeforeApply, setSlotsToSetupPose);
                    extendSkin.ApplyEvent(SkelObject);
                }



                ///<summary>
                ///MainSkin 제거하기<br/>
                ///(메인 AdvancedSkin 존재시, 참조 제거됨)
                ///</summary>
                public override bool RemoveMainSkin(bool setSlotsToSetupPose = false)
                {
                    if (!base.RemoveMainSkin(setSlotsToSetupPose)) { return false; }

                    if (MainAdvancedSkin != null) { RemoveMainAdvancedSkin(); }

                    return true;
                }



                protected override void RefreshSkeletonAfterSkinChanged()
                {
                    SkelObject.RequestRuntimeSkeletonRefresh();
                }



                ///======================================================================================================================================================



                //? Advanced 스킨 지정/제거 (SkelObject)



                ///<summary>
                /// 메인 Advanced 스킨 설정하기 
                ///</summary>
                ///<param name="advancedSkin">대상 Advanced 스킨</param>
                ///<param name="overlap">중첩 여부</param>
                /// <param name="clearBeforeApply">스킨을 적용하기전에 Clear 이후 적용하기</param>
                public bool SetMainAdvancedSkin<TAdvancedSkin>(TAdvancedSkin advancedSkin, bool overlap = true, bool clearBeforeApply = true, bool setSlotsToSetupPose = true) where TAdvancedSkin : SkelSbject.BaseAdvancedSkin
                {
                    //? 이미 다른 메인 Advanced 스킨이 설정되어있다면
                    if (MainAdvancedSkin != null)
                    {
                        //! 중첩을 허용하지 않거나, 중복이라면 실패한다
                        if (!overlap || MainAdvancedSkin == advancedSkin) return false;

                        //? 중첩하기위해, 먼저 현재 메인 Advanced 스킨을 제거한다
                        RemoveMainAdvancedSkin();
                    }

                    //? AdvancedSkin의 스킨을 설정헌다
                    SetMainSkin(advancedSkin.MainExtendSkin, clearBeforeApply, setSlotsToSetupPose);

                    //?  메인 Advanced 스킨 참조 등록
                    MainAdvancedSkin = advancedSkin;

                    return true;
                }



                ///<summary>
                /// 메인 Advanced 스킨 설정하기
                ///</summary>
                ///<param name="overlap">중첩 여부</param>
                /// <param name="clearBeforeApply">스킨을 적용하기전에 Clear 이후 적용하기</param>
                public void SetMainAdvancedSkin<TSkelSbject, TAdvancedSkinBase, TAdvancedSkin>(SkelSbject.IHoldAdvancedSkinManager<TSkelSbject, TAdvancedSkinBase> target, bool overlap = true, bool clearBeforeApply = true, bool setSlotsToSetupPose = true)
                    where TSkelSbject : SkelSbject, new()
                    where TAdvancedSkinBase : SkelSbject.BaseAdvancedSkin<TSkelSbject>
                    where TAdvancedSkin : TAdvancedSkinBase, new()
                {
                    SetMainAdvancedSkin(target.AdvancedSkins.GetInstance<TAdvancedSkin>(), overlap, clearBeforeApply, setSlotsToSetupPose);
                }



                ///<summary>
                /// 메인 Advanced 스킨 설정하기
                ///</summary>
                ///<param name="overlap">중첩 여부</param>
                /// <param name="clearBeforeApply">스킨을 적용하기전에 Clear 이후 적용하기</param>
                public void SetMainAdvancedSkin<TSkelSbject, TAdvancedSkin>(TSkelSbject target, bool overlap = true, bool clearBeforeApply = true, bool setSlotsToSetupPose = true)
                    where TSkelSbject : SkelSbject_AdvancedSkin<TSkelSbject, SkelSbject.BaseAdvancedSkin<TSkelSbject>>, new()
                    where TAdvancedSkin : SkelSbject.BaseAdvancedSkin<TSkelSbject>, new()
                {
                    SetMainAdvancedSkin(target.AdvancedSkins.GetInstance<TAdvancedSkin>(), overlap, clearBeforeApply, setSlotsToSetupPose);
                }



                /// <summary>
                /// 메인 Advanced 스킨 제거하기
                /// </summary>
                public bool RemoveMainAdvancedSkin()
                {
                    if (MainAdvancedSkin == null) { return false; }

                    MainAdvancedSkin.MainExtendSkin.RemoveEvent(SkelObject);
                    MainAdvancedSkin = null;
                    return true;
                }



                ///======================================================================================================================================================



                //? 믹스 스킨



                #region override 메서드 (비권장)

                [Obsolete("이 방식을 사용해도 되지만, 굳이 사용할 필요는 없음")]
                public override bool PutUpSkin(ExtendSkin extendSkin, int skinLayerRank, bool overlap = false, bool isOnlyAttachment = false, bool setSlotsToSetupPose = false)
                {
                    return PutUpSkinInternal(
                        extendSkin,
                        skinLayerRank,
                        overlap,
                        isOnlyAttachment,
                        setSlotsToSetupPose);
                }



                [Obsolete("이 방식을 사용해도 되지만, 굳이 사용할 필요는 없음")]
                public override bool PutDownSkin(ExtendSkin wasAddedSkin, int wasAddedSkinLayerRank, bool setSlotsToSetupPose = false)
                {
                    return PutDownSkinInternal(
                        wasAddedSkin,
                        wasAddedSkinLayerRank,
                        setSlotsToSetupPose);
                }

                #endregion



                private bool PutUpSkinInternal(
                    ExtendSkin extendSkin,
                    int skinLayerRank,
                    bool overlap,
                    bool putOnlyAttachment,
                    bool setSlotsToSetupPose)
                {
                    if (!TrySetMixSkin(extendSkin, skinLayerRank, overlap, putOnlyAttachment)) { return false; }

                    RebuildHuskSkin();
                    ApplyHuskSkinToRunTimeSkin(true);

                    if (setSlotsToSetupPose) { SetSlotsToSetupPose(); }

                    return true;
                }



                private bool PutDownSkinInternal(
                    ExtendSkin extendSkin,
                    int skinLayerRank,
                    bool setSlotsToSetupPose)
                {
                    if (!TryRemoveMixSkin(extendSkin, skinLayerRank)) { return false; }

                    RebuildHuskSkin();
                    ApplyHuskSkinToRunTimeSkin(true);

                    if (setSlotsToSetupPose) { SetSlotsToSetupPose(); }

                    return true;
                }



                ///<summary>
                /// 믹스 스킨을 가져와 올려놓는다
                /// </summary>
                /// <param name="extendSkin"></param>
                /// <param name="overlap"></param>
                /// <param name="putOnlyAttachment">어태치먼트만 적용하기</param>
                /// <param name="setSlotsToSetupPose"></param>
                public bool PutUpSkin(ExtendSkin extendSkin, bool overlap = false, bool putOnlyAttachment = false, bool setSlotsToSetupPose = false)
                {
                    return PutUpSkinInternal(
                        extendSkin,
                        extendSkin.LayerRank,
                        overlap,
                        putOnlyAttachment,
                        setSlotsToSetupPose);
                }



                /// <summary>
                /// 믹스 스킨을 꺼내 내려놓는다
                /// </summary>
                /// <param name="wasAddedExtendSkin"></param>
                /// <param name="setSlotsToSetupPose"></param>
                /// <returns></returns>
                public bool PutDownSkin(ExtendSkin wasAddedExtendSkin, bool setSlotsToSetupPose = false)
                {
                    return PutDownSkinInternal(
                        wasAddedExtendSkin,
                        wasAddedExtendSkin.LayerRank,
                        setSlotsToSetupPose);
                }



                ///======================================================================================================================================================
            }
        }
    }
}
