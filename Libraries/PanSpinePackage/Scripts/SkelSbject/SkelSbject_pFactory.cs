using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Spine;
using Spine.Unity;
using System.Text;
using Pan.Util;
using SitraUtils;
using System.Linq;
using Pan.SpinePackage;
using static Pan.SpinePackage.SkelObject.AniCore.Players;



//? 필요한 객체를 적절하게 제약된 조건 내에서 생성이 가능하게 해주는 공장 클래스 있는 정도의 코드



namespace Pan.SpinePackage
{
    public abstract partial class SkelSbject
    {
        ///======================================================================================================================================================



        //? 베이스



        public abstract class BaseFactory<TSkelSbject> where TSkelSbject : SkelSbject, new()
        {
            public BaseFactory(TSkelSbject skelSbject)
            {
                SkelSbject = skelSbject;
            }

            protected readonly TSkelSbject SkelSbject;
        }



        ///======================================================================================================================================================



        //? 확장 스킨 공장



        /// <summary>
        /// <see cref="ExtendSkin"/> 생성 공장
        /// </summary>
        /// <typeparam name="TSkelSbject">
        /// 대상 <see cref="SkelSbject"/><br/>
        /// 반드시 <see cref="IHoldEnumIndex_SkinLayer{TSkelSbject}"/>
        /// 가 구현 비추상 클래스여야함!
        /// </typeparam>
        /// <typeparam name="TSkinLayer">대상 스킨 레이어 열거형 타입</typeparam>
        public class ExtendSkinFactory<TSkelSbject, TSkinLayer> : BaseFactory<TSkelSbject>
          where TSkelSbject : SkelSbject, IHoldEnumIndex_SkinLayer<TSkinLayer>, new()
          where TSkinLayer : struct, Enum
        {
            ///======================================================================================================================================================



            public ExtendSkinFactory(TSkelSbject skelSbject) : base(skelSbject) { }



            ///======================================================================================================================================================



            /// <summary>
            /// <see cref="ExtendSkin"/> 을 생성하여 반환
            /// </summary>
            /// <param name="skinName">스킨 이름</param>
            /// <param name="skinLayer">스킨 레이어 등급</param>
            /// <returns></returns>
            public ExtendSkin NewExtendSkin(string skinName, TSkinLayer skinLayer)
            {
                return new ExtendSkin(SkelSbject, skinName, SkelSbject.EnumIndex_SkinLayer.GetIndex(skinLayer));
            }



            /// <summary>
            /// <see cref="ExtendSkin.Eyes"/> 을 생성하여 반환
            /// </summary>
            /// <param name="skinName">스킨 이름</param>
            /// <param name="skinLayerRank">스킨 레이어 등급</param>
            public ExtendSkin.Eyes NewExtendSkin_Eyes(string skinName, TSkinLayer skinLayer, string leftMark = "_L", string rightMark = "_R")
            {
                return new ExtendSkin.Eyes(SkelSbject.GetSkin(skinName),
                    SkelSbject.GetSkin(string.Concat(skinName, leftMark)),
                    SkelSbject.GetSkin(string.Concat(skinName, rightMark)),
                    SkelSbject.EnumIndex_SkinLayer.GetIndex(skinLayer));
            }



            ///======================================================================================================================================================
        }



        /// <summary>
        /// <see cref="ExtendSkin"/> 생성 공장 + 폴더 확장
        /// </summary>
        /// <typeparam name="TSkelSbject">
        /// 대상 <see cref="SkelSbject"/><br/>
        /// 반드시 <see cref="IHoldEnumIndex_SkinLayer{TSkelSbject}"/>,<br/>
        /// <see cref="ISkinFolderMappingManager{TSkinFolder}"/><br/>
        /// 가 구현 비추상 클래스여야함!
        /// </typeparam>
        /// <typeparam name="TSkinLayer">대상 스킨 레이어 열거형 타입</typeparam>
        public class ExtendSkinFactoryExtend<TSkelSbject, TSkinFolder, TSkinLayer> : ExtendSkinFactory<TSkelSbject, TSkinLayer>
            where TSkelSbject : SkelSbject, ISkinFolderMappingManager<TSkinFolder>, IHoldEnumIndex_SkinLayer<TSkinLayer>, new()
            where TSkinFolder : struct, Enum
            where TSkinLayer : struct, Enum
        {
            ///======================================================================================================================================================



            public ExtendSkinFactoryExtend(TSkelSbject skelSbject) : base(skelSbject) { }



            ///======================================================================================================================================================



            /// <summary>
            /// <see cref="ExtendSkin"/> 을 생성하여 반환
            /// </summary>
            /// <param name="skinName">스킨 이름</param>
            /// <param name="skinLayer">스킨 레이어 등급</param>
            /// <returns></returns>
            public ExtendSkin NewExtendSkin(TSkinFolder skinFolder, string skinName, TSkinLayer skinLayer)
            {
                return NewExtendSkin(SkelSbject.SkinFolder.ConvertFolder(skinFolder, skinName), skinLayer);
            }



            /// <summary>
            /// <see cref="ExtendSkin.Eyes"/> 을 생성하여 반환
            /// </summary>
            /// <param name="skinName">스킨 이름</param>
            /// <param name="skinLayerRank">스킨 레이어 등급</param>
            public ExtendSkin.Eyes NewExtendSkin_Eyes(TSkinFolder skinFolder, string skinName, TSkinLayer skinLayer, string leftMark = "_L", string rightMark = "_R")
            {
                return NewExtendSkin_Eyes(SkelSbject.SkinFolder.ConvertFolder(skinFolder, skinName), skinLayer, leftMark, rightMark);
            }



            ///======================================================================================================================================================
        }



        ///======================================================================================================================================================



        //? 애니메이션 공장



        /// <summary>
        /// <see cref="SkelAni"/> 생성 공장
        /// </summary>
        /// <typeparam name="TSkelSbject">
        /// 대상 <see cref="SkelSbject"/><br/>
        /// 반드시 <see cref="IHoldEnumIndex_AniTrack{TSkelSbject}"/>, <br/>
        /// <see cref="IHoldEnumIndex_AniRank{TAniRank}"/>,<br/>
        /// <see cref="IHoldEnumIndex_AniTime{TAniTime}"/><br/>
        /// 가 구현 비추상 클래스여야함!
        /// </typeparam>
        /// <typeparam name="TEnumTrack">대상 애니메이션 트랙 열거형 타입</typeparam>
        /// <typeparam name="TEnumRank">대상 애니메이션 랭크 열거형 타입</typeparam>
        /// <typeparam name="TEnumTime">대상 애니메이션 타임 열거형 타입</typeparam>
        public class SkelAniFactory<TSkelSbject, TEnumTrack, TEnumRank, TEnumTime> : BaseFactory<TSkelSbject>
            where TSkelSbject : SkelSbject, IHoldEnumIndex_AniTrack<TEnumTrack>, IHoldEnumIndex_AniRank<TEnumRank>, IHoldEnumIndex_AniTime<TEnumTime>, new()
            where TEnumTrack : struct, Enum
            where TEnumRank : struct, Enum
            where TEnumTime : struct, Enum
        {
            ///======================================================================================================================================================



            public SkelAniFactory(TSkelSbject skelSbject) : base(skelSbject) { }



            ///======================================================================================================================================================



            //? 생성: 노말



            #region 생성: 노말



            /// <summary>
            /// 생성: <b>노말</b>
            /// </summary>
            public SkelAni Create_Normal(TEnumTrack track, Spine.Animation animation, float mixDuration)
            {
                return SkelAni.Create_Normal(SkelSbject.EnumIndex_AniTracks.GetIndex(track), animation, mixDuration);
            }

            /// <summary>
            /// 생성: <b>노말</b>
            /// </summary>
            public SkelAni Create_Normal(TEnumTrack track, Spine.Animation animation, TEnumRank aniRankEnum, float mixDuration)
            {
                return SkelAni.Create_Normal(SkelSbject.EnumIndex_AniTracks.GetIndex(track), animation, SkelSbject.EnumIndex_AniRanks.GetIndex(aniRankEnum), mixDuration);
            }



            /// <summary>
            /// 생성: <b>노말</b>
            /// </summary>
            public SkelAni Create_Normal(TEnumTrack track, Spine.Animation animation)
            {
                return SkelAni.Create_Normal(SkelSbject.EnumIndex_AniTracks.GetIndex(track), animation, SkelSbject.AnimationMixDuration);
            }

            /// <summary>
            /// 생성: <b>노말</b>
            /// </summary>
            public SkelAni Create_Normal(TEnumTrack track, Spine.Animation animation, TEnumRank aniRankEnum)
            {
                return SkelAni.Create_Normal(SkelSbject.EnumIndex_AniTracks.GetIndex(track), animation, SkelSbject.EnumIndex_AniRanks.GetIndex(aniRankEnum), SkelSbject.AnimationMixDuration);
            }



            /// <summary>
            /// 생성: <b>노말</b>
            /// </summary>
            public SkelAni Create_Normal(TEnumTrack track, string animationName, float mixDuration)
            {
                return Create_Normal(track, SkelSbject.GetAnimation(animationName), mixDuration);
            }

            /// <summary>
            /// 생성: <b>노말</b>
            /// </summary>
            public SkelAni Create_Normal(TEnumTrack track, string animationName, TEnumRank aniRankEnum, float mixDuration)
            {
                return Create_Normal(track, SkelSbject.GetAnimation(animationName), aniRankEnum, mixDuration);
            }



            /// <summary>
            /// 생성: <b>노말</b>
            /// </summary>
            public SkelAni Create_Normal(TEnumTrack track, string animationName)
            {
                return Create_Normal(track, SkelSbject.GetAnimation(animationName), SkelSbject.AnimationMixDuration);
            }

            /// <summary>
            /// 생성: <b>노말</b>
            /// </summary>
            public SkelAni Create_Normal(TEnumTrack track, string animationName, TEnumRank aniRankEnum)
            {
                return Create_Normal(track, SkelSbject.GetAnimation(animationName), aniRankEnum, SkelSbject.AnimationMixDuration);
            }



            #endregion



            #region 생성: 노말



            /// <summary>
            /// 생성: <b>노말</b>
            /// </summary>
            public TSkelAni Create_Normal<TSkelAni>(TEnumTrack track, Spine.Animation animation, float mixDuration) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_Normal<TSkelAni>(SkelSbject.EnumIndex_AniTracks.GetIndex(track), animation, mixDuration);
            }

            /// <summary>
            /// 생성: <b>노말</b>
            /// </summary>
            public TSkelAni Create_Normal<TSkelAni>(TEnumTrack track, Spine.Animation animation, TEnumRank aniRankEnum, float mixDuration) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_Normal<TSkelAni>(SkelSbject.EnumIndex_AniTracks.GetIndex(track), animation, SkelSbject.EnumIndex_AniRanks.GetIndex(aniRankEnum), mixDuration);
            }



            /// <summary>
            /// 생성: <b>노말</b>
            /// </summary>
            public TSkelAni Create_Normal<TSkelAni>(TEnumTrack track, Spine.Animation animation) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_Normal<TSkelAni>(SkelSbject.EnumIndex_AniTracks.GetIndex(track), animation, SkelSbject.AnimationMixDuration);
            }

            /// <summary>
            /// 생성: <b>노말</b>
            /// </summary>
            public TSkelAni Create_Normal<TSkelAni>(TEnumTrack track, Spine.Animation animation, TEnumRank aniRankEnum) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_Normal<TSkelAni>(SkelSbject.EnumIndex_AniTracks.GetIndex(track), animation, SkelSbject.EnumIndex_AniRanks.GetIndex(aniRankEnum), SkelSbject.AnimationMixDuration);
            }



            /// <summary>
            /// 생성: <b>노말</b>
            /// </summary>
            public TSkelAni Create_Normal<TSkelAni>(TEnumTrack track, string animationName, float mixDuration) where TSkelAni : SkelAni, new()
            {
                return Create_Normal<TSkelAni>(track, SkelSbject.GetAnimation(animationName), mixDuration);
            }

            /// <summary>
            /// 생성: <b>노말</b>
            /// </summary>
            public TSkelAni Create_Normal<TSkelAni>(TEnumTrack track, string animationName, TEnumRank aniRankEnum, float mixDuration) where TSkelAni : SkelAni, new()
            {
                return Create_Normal<TSkelAni>(track, SkelSbject.GetAnimation(animationName), aniRankEnum, mixDuration);
            }



            /// <summary>
            /// 생성: <b>노말</b>
            /// </summary>
            public TSkelAni Create_Normal<TSkelAni>(TEnumTrack track, string animationName) where TSkelAni : SkelAni, new()
            {
                return Create_Normal<TSkelAni>(track, SkelSbject.GetAnimation(animationName), SkelSbject.AnimationMixDuration);
            }

            /// <summary>
            /// 생성: <b>노말</b>
            /// </summary>
            public TSkelAni Create_Normal<TSkelAni>(TEnumTrack track, string animationName, TEnumRank aniRankEnum) where TSkelAni : SkelAni, new()
            {
                return Create_Normal<TSkelAni>(track, SkelSbject.GetAnimation(animationName), aniRankEnum, SkelSbject.AnimationMixDuration);
            }



            #endregion



            //? 생성: 노말 중첩



            #region 생성: 노말 중첩



            /// <summary>
            /// 생성: <b>노말 중첩</b>
            /// </summary>
            public SkelAni Create_NormalOverlap(TEnumTrack track, Spine.Animation animation, float mixDuration)
            {
                return SkelAni.Create_NormalOverlap(SkelSbject.EnumIndex_AniTracks.GetIndex(track), animation, mixDuration);
            }

            /// <summary>
            /// 생성: <b>노말 중첩</b>
            /// </summary>
            public SkelAni Create_NormalOverlap(TEnumTrack track, Spine.Animation animation, TEnumRank aniRankEnum, float mixDuration)
            {
                return SkelAni.Create_NormalOverlap(SkelSbject.EnumIndex_AniTracks.GetIndex(track), animation, SkelSbject.EnumIndex_AniRanks.GetIndex(aniRankEnum), mixDuration);
            }



            /// <summary>
            /// 생성: <b>노말 중첩</b>
            /// </summary>
            public SkelAni Create_NormalOverlap(TEnumTrack track, Spine.Animation animation)
            {
                return SkelAni.Create_NormalOverlap(SkelSbject.EnumIndex_AniTracks.GetIndex(track), animation, SkelSbject.AnimationMixDuration);
            }

            /// <summary>
            /// 생성: <b>노말 중첩</b>
            /// </summary>
            public SkelAni Create_NormalOverlap(TEnumTrack track, Spine.Animation animation, TEnumRank aniRankEnum)
            {
                return SkelAni.Create_NormalOverlap(SkelSbject.EnumIndex_AniTracks.GetIndex(track), animation, SkelSbject.EnumIndex_AniRanks.GetIndex(aniRankEnum), SkelSbject.AnimationMixDuration);
            }



            /// <summary>
            /// 생성: <b>노말 중첩</b>
            /// </summary>
            public SkelAni Create_NormalOverlap(TEnumTrack track, string animationName, float mixDuration)
            {
                return Create_NormalOverlap(track, SkelSbject.GetAnimation(animationName), mixDuration);
            }

            /// <summary>
            /// 생성: <b>노말 중첩</b>
            /// </summary>
            public SkelAni Create_NormalOverlap(TEnumTrack track, string animationName, TEnumRank aniRankEnum, float mixDuration)
            {
                return Create_NormalOverlap(track, SkelSbject.GetAnimation(animationName), aniRankEnum, mixDuration);
            }



            /// <summary>
            /// 생성: <b>노말 중첩</b>
            /// </summary>
            public SkelAni Create_NormalOverlap(TEnumTrack track, string animationName)
            {
                return Create_NormalOverlap(track, SkelSbject.GetAnimation(animationName), SkelSbject.AnimationMixDuration);
            }

            /// <summary>
            /// 생성: <b>노말 중첩</b>
            /// </summary>
            public SkelAni Create_NormalOverlap(TEnumTrack track, string animationName, TEnumRank aniRankEnum)
            {
                return Create_NormalOverlap(track, SkelSbject.GetAnimation(animationName), aniRankEnum, SkelSbject.AnimationMixDuration);
            }



            #endregion



            #region 생성: 노말 중첩



            /// <summary>
            /// 생성: <b>노말 중첩</b>
            /// </summary>
            public TSkelAni Create_NormalOverlap<TSkelAni>(TEnumTrack track, Spine.Animation animation, float mixDuration) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_NormalOverlap<TSkelAni>(SkelSbject.EnumIndex_AniTracks.GetIndex(track), animation, mixDuration);
            }

            /// <summary>
            /// 생성: <b>노말 중첩</b>
            /// </summary>
            public TSkelAni Create_NormalOverlap<TSkelAni>(TEnumTrack track, Spine.Animation animation, TEnumRank aniRankEnum, float mixDuration) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_NormalOverlap<TSkelAni>(SkelSbject.EnumIndex_AniTracks.GetIndex(track), animation, SkelSbject.EnumIndex_AniRanks.GetIndex(aniRankEnum), mixDuration);
            }



            /// <summary>
            /// 생성: <b>노말 중첩</b>
            /// </summary>
            public TSkelAni Create_NormalOverlap<TSkelAni>(TEnumTrack track, Spine.Animation animation) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_NormalOverlap<TSkelAni>(SkelSbject.EnumIndex_AniTracks.GetIndex(track), animation, SkelSbject.AnimationMixDuration);
            }

            /// <summary>
            /// 생성: <b>노말 중첩</b>
            /// </summary>
            public TSkelAni Create_NormalOverlap<TSkelAni>(TEnumTrack track, Spine.Animation animation, TEnumRank aniRankEnum) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_NormalOverlap<TSkelAni>(SkelSbject.EnumIndex_AniTracks.GetIndex(track), animation, SkelSbject.EnumIndex_AniRanks.GetIndex(aniRankEnum), SkelSbject.AnimationMixDuration);
            }



            /// <summary>
            /// 생성: <b>노말 중첩</b>
            /// </summary>
            public TSkelAni Create_NormalOverlap<TSkelAni>(TEnumTrack track, string animationName, float mixDuration) where TSkelAni : SkelAni, new()
            {
                return Create_NormalOverlap<TSkelAni>(track, SkelSbject.GetAnimation(animationName), mixDuration);
            }

            /// <summary>
            /// 생성: <b>노말 중첩</b>
            /// </summary>
            public TSkelAni Create_NormalOverlap<TSkelAni>(TEnumTrack track, string animationName, TEnumRank aniRankEnum, float mixDuration) where TSkelAni : SkelAni, new()
            {
                return Create_NormalOverlap<TSkelAni>(track, SkelSbject.GetAnimation(animationName), aniRankEnum, mixDuration);
            }



            /// <summary>
            /// 생성: <b>노말 중첩</b>
            /// </summary>
            public TSkelAni Create_NormalOverlap<TSkelAni>(TEnumTrack track, string animationName) where TSkelAni : SkelAni, new()
            {
                return Create_NormalOverlap<TSkelAni>(track, SkelSbject.GetAnimation(animationName), SkelSbject.AnimationMixDuration);
            }

            /// <summary>
            /// 생성: <b>노말 중첩</b>
            /// </summary>
            public TSkelAni Create_NormalOverlap<TSkelAni>(TEnumTrack track, string animationName, TEnumRank aniRankEnum) where TSkelAni : SkelAni, new()
            {
                return Create_NormalOverlap<TSkelAni>(track, SkelSbject.GetAnimation(animationName), aniRankEnum, SkelSbject.AnimationMixDuration);
            }



            #endregion



            //? 생성: 반복



            #region 생성: 반복



            /// <summary>
            /// 생성: <b>반복</b>
            /// </summary>
            public SkelAni Create_Loop(TEnumTrack track, Spine.Animation animation, float mixDuration)
            {
                return SkelAni.Create_Loop(SkelSbject.EnumIndex_AniTracks.GetIndex(track), animation, mixDuration);
            }

            /// <summary>
            /// 생성: <b>반복</b>
            /// </summary>
            public SkelAni Create_Loop(TEnumTrack track, Spine.Animation animation, TEnumRank aniRankEnum, float mixDuration)
            {
                return SkelAni.Create_Loop(SkelSbject.EnumIndex_AniTracks.GetIndex(track), animation, SkelSbject.EnumIndex_AniRanks.GetIndex(aniRankEnum), mixDuration);
            }



            /// <summary>
            /// 생성: <b>반복</b>
            /// </summary>
            public SkelAni Create_Loop(TEnumTrack track, Spine.Animation animation)
            {
                return SkelAni.Create_Loop(SkelSbject.EnumIndex_AniTracks.GetIndex(track), animation, SkelSbject.AnimationMixDuration);
            }

            /// <summary>
            /// 생성: <b>반복</b>
            /// </summary>
            public SkelAni Create_Loop(TEnumTrack track, Spine.Animation animation, TEnumRank aniRankEnum)
            {
                return SkelAni.Create_Loop(SkelSbject.EnumIndex_AniTracks.GetIndex(track), animation, SkelSbject.EnumIndex_AniRanks.GetIndex(aniRankEnum), SkelSbject.AnimationMixDuration);
            }



            /// <summary>
            /// 생성: <b>반복</b>
            /// </summary>
            public SkelAni Create_Loop(TEnumTrack track, string animationName, float mixDuration)
            {
                return Create_Loop(track, SkelSbject.GetAnimation(animationName), mixDuration);
            }

            /// <summary>
            /// 생성: <b>반복</b>
            /// </summary>
            public SkelAni Create_Loop(TEnumTrack track, string animationName, TEnumRank aniRankEnum, float mixDuration)
            {
                return Create_Loop(track, SkelSbject.GetAnimation(animationName), aniRankEnum, mixDuration);
            }



            /// <summary>
            /// 생성: <b>반복</b>
            /// </summary>
            public SkelAni Create_Loop(TEnumTrack track, string animationName)
            {
                return Create_Loop(track, SkelSbject.GetAnimation(animationName), SkelSbject.AnimationMixDuration);
            }

            /// <summary>
            /// 생성: <b>반복</b>
            /// </summary>
            public SkelAni Create_Loop(TEnumTrack track, string animationName, TEnumRank aniRankEnum)
            {
                return Create_Loop(track, SkelSbject.GetAnimation(animationName), aniRankEnum, SkelSbject.AnimationMixDuration);
            }


            #endregion



            #region 생성: 반복



            /// <summary>
            /// 생성: <b>반복</b>
            /// </summary>
            public TSkelAni Create_Loop<TSkelAni>(TEnumTrack track, Spine.Animation animation, float mixDuration) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_Loop<TSkelAni>(SkelSbject.EnumIndex_AniTracks.GetIndex(track), animation, mixDuration);
            }

            /// <summary>
            /// 생성: <b>반복</b>
            /// </summary>
            public TSkelAni Create_Loop<TSkelAni>(TEnumTrack track, Spine.Animation animation, TEnumRank aniRankEnum, float mixDuration) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_Loop<TSkelAni>(SkelSbject.EnumIndex_AniTracks.GetIndex(track), animation, SkelSbject.EnumIndex_AniRanks.GetIndex(aniRankEnum), mixDuration);
            }



            /// <summary>
            /// 생성: <b>반복</b>
            /// </summary>
            public TSkelAni Create_Loop<TSkelAni>(TEnumTrack track, Spine.Animation animation) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_Loop<TSkelAni>(SkelSbject.EnumIndex_AniTracks.GetIndex(track), animation, SkelSbject.AnimationMixDuration);
            }

            /// <summary>
            /// 생성: <b>반복</b>
            /// </summary>
            public TSkelAni Create_Loop<TSkelAni>(TEnumTrack track, Spine.Animation animation, TEnumRank aniRankEnum) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_Loop<TSkelAni>(SkelSbject.EnumIndex_AniTracks.GetIndex(track), animation, SkelSbject.EnumIndex_AniRanks.GetIndex(aniRankEnum), SkelSbject.AnimationMixDuration);
            }



            /// <summary>
            /// 생성: <b>반복</b>
            /// </summary>
            public TSkelAni Create_Loop<TSkelAni>(TEnumTrack track, string animationName, float mixDuration) where TSkelAni : SkelAni, new()
            {
                return Create_Loop<TSkelAni>(track, SkelSbject.GetAnimation(animationName), mixDuration);
            }

            /// <summary>
            /// 생성: <b>반복</b>
            /// </summary>
            public TSkelAni Create_Loop<TSkelAni>(TEnumTrack track, string animationName, TEnumRank aniRankEnum, float mixDuration) where TSkelAni : SkelAni, new()
            {
                return Create_Loop<TSkelAni>(track, SkelSbject.GetAnimation(animationName), aniRankEnum, mixDuration);
            }



            /// <summary>
            /// 생성: <b>반복</b>
            /// </summary>
            public TSkelAni Create_Loop<TSkelAni>(TEnumTrack track, string animationName) where TSkelAni : SkelAni, new()
            {
                return Create_Loop<TSkelAni>(track, SkelSbject.GetAnimation(animationName), SkelSbject.AnimationMixDuration);
            }

            /// <summary>
            /// 생성: <b>반복</b>
            /// </summary>
            public TSkelAni Create_Loop<TSkelAni>(TEnumTrack track, string animationName, TEnumRank aniRankEnum) where TSkelAni : SkelAni, new()
            {
                return Create_Loop<TSkelAni>(track, SkelSbject.GetAnimation(animationName), aniRankEnum, SkelSbject.AnimationMixDuration);
            }


            #endregion



            //? 생성: 복제



            #region 생성: 복제



            /// <summary>
            /// 생성: <b>복제 생성</b><br/>
            /// 받아온<paramref name="target"/>을 복제(Deep)하여 생성한다
            /// </summary>
            public SkelAni Create_Copy(ISkelAni target)
            {
                return SkelAni.Create_Copy(target);
            }



            /// <summary>
            /// 생성: <b>복제 생성</b><br/>
            /// 받아온<paramref name="target"/>을 복제(Deep)하여 생성한다<br/>
            /// + Animation 을 수정한다
            /// </summary>
            public SkelAni Create_Copy(ISkelAni target, string animationName)
            {
                return SkelAni.Create_Copy(target, SkelSbject.GetAnimation(animationName));
            }



            /// <summary>
            /// 생성: <b>복제 생성</b><br/>
            /// 받아온<paramref name="target"/>을 복제(Deep)하여 생성한다<br/>
            /// + TrackIndex 를 수정한다
            /// </summary>
            public SkelAni Create_Copy(ISkelAni target, TEnumTrack track)
            {
                return SkelAni.Create_Copy(target, SkelSbject.EnumIndex_AniTracks.GetIndex(track));
            }



            #endregion



            #region 생성: 복제



            /// <summary>
            /// 생성: <b>복제 생성</b><br/>
            /// 받아온<paramref name="target"/>을 복제(Deep)하여 생성한다
            /// </summary>
            public TSkelAni Create_Copy<TSkelAni>(ISkelAni target) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_Copy<TSkelAni>(target);
            }



            /// <summary>
            /// 생성: <b>복제 생성</b><br/>
            /// 받아온<paramref name="target"/>을 복제(Deep)하여 생성한다<br/>
            /// + Animation 을 수정한다
            /// </summary>
            public TSkelAni Create_Copy<TSkelAni>(ISkelAni target, string animationName) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_Copy<TSkelAni>(target, SkelSbject.GetAnimation(animationName));
            }



            /// <summary>
            /// 생성: <b>복제 생성</b><br/>
            /// 받아온<paramref name="target"/>을 복제(Deep)하여 생성한다<br/>
            /// + TrackIndex 를 수정한다
            /// </summary>
            public TSkelAni Create_Copy<TSkelAni>(ISkelAni target, TEnumTrack track) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_Copy<TSkelAni>(target, SkelSbject.EnumIndex_AniTracks.GetIndex(track));
            }



            #endregion



            ///======================================================================================================================================================
        }



        /// <summary>
        /// <see cref="SkelAni"/> 생성 공장 + 폴더 확장
        /// </summary>
        /// <typeparam name="TSkelSbject">
        /// 대상 <see cref="SkelSbject"/><br/>
        /// 반드시 <see cref="IHoldEnumIndex_AniTrack{TSkelSbject}"/>, <br/>
        /// <see cref="IHoldEnumIndex_AniRank{TAniRank}"/>,<br/>
        /// <see cref="IHoldEnumIndex_AniTime{TAniTime}"/><br/>
        /// <see cref="IAnimationFolderMappingManager{TEnumFolder}"/><br/>
        /// 가 구현 비추상 클래스여야함!
        /// </typeparam>
        /// <typeparam name="TEnumTrack">대상 애니메이션 트랙 열거형 타입</typeparam>
        /// <typeparam name="TEnumRank">대상 애니메이션 랭크 열거형 타입</typeparam>
        /// <typeparam name="TEnumTime">대상 애니메이션 타임 열거형 타입</typeparam>
        public class SkelAniFactoryExtend<TSkelSbject, TEnumFolder, TEnumTrack, TEnumRank, TEnumTime> : SkelAniFactory<TSkelSbject, TEnumTrack, TEnumRank, TEnumTime>
    where TSkelSbject : SkelSbject, IAnimationFolderMappingManager<TEnumFolder>, IHoldEnumIndex_AniTrack<TEnumTrack>, IHoldEnumIndex_AniRank<TEnumRank>, IHoldEnumIndex_AniTime<TEnumTime>, new()
    where TEnumFolder : struct, Enum
    where TEnumTrack : struct, Enum
    where TEnumRank : struct, Enum
    where TEnumTime : struct, Enum
        {
            ///======================================================================================================================================================



            public SkelAniFactoryExtend(TSkelSbject skelSbject) : base(skelSbject) { }



            ///======================================================================================================================================================



            //? 생성: 노말



            /// <summary>
            /// 생성: <b>노말</b>
            /// </summary>
            public SkelAni Create_Normal(TEnumTrack track, TEnumFolder aniFolder, string animationName, float mixDuration)
            {

                return SkelAni.Create_Normal(SkelSbject.EnumIndex_AniTracks.GetIndex(track), SkelSbject.GetAnimation(aniFolder, animationName), mixDuration);
            }

            /// <summary>
            /// 생성: <b>노말</b>
            /// </summary>
            public SkelAni Create_Normal(TEnumTrack track, TEnumFolder aniFolder, string animationName, TEnumRank aniRankEnum, float mixDuration)
            {

                return SkelAni.Create_Normal(SkelSbject.EnumIndex_AniTracks.GetIndex(track), SkelSbject.GetAnimation(aniFolder, animationName), SkelSbject.EnumIndex_AniRanks.GetIndex(aniRankEnum), mixDuration);
            }



            /// <summary>
            /// 생성: <b>노말</b>
            /// </summary>
            public SkelAni Create_Normal(TEnumTrack track, TEnumFolder aniFolder, string animationName)
            {
                return SkelAni.Create_Normal(SkelSbject.EnumIndex_AniTracks.GetIndex(track), SkelSbject.GetAnimation(aniFolder, animationName), SkelSbject.AnimationMixDuration);
            }

            /// <summary>
            /// 생성: <b>노말</b>
            /// </summary>
            public SkelAni Create_Normal(TEnumTrack track, TEnumFolder aniFolder, string animationName, TEnumRank aniRankEnum)
            {
                return SkelAni.Create_Normal(SkelSbject.EnumIndex_AniTracks.GetIndex(track), SkelSbject.GetAnimation(aniFolder, animationName), SkelSbject.EnumIndex_AniRanks.GetIndex(aniRankEnum), SkelSbject.AnimationMixDuration);
            }



            /// <summary>
            /// 생성: <b>노말</b>
            /// </summary>
            public TSkelAni Create_Normal<TSkelAni>(TEnumTrack track, TEnumFolder aniFolder, string animationName, float mixDuration) where TSkelAni : SkelAni, new()
            {

                return SkelAni.Create_Normal<TSkelAni>(SkelSbject.EnumIndex_AniTracks.GetIndex(track), SkelSbject.GetAnimation(aniFolder, animationName), mixDuration);
            }

            /// <summary>
            /// 생성: <b>노말</b>
            /// </summary>
            public TSkelAni Create_Normal<TSkelAni>(TEnumTrack track, TEnumFolder aniFolder, string animationName, TEnumRank aniRankEnum, float mixDuration) where TSkelAni : SkelAni, new()
            {

                return SkelAni.Create_Normal<TSkelAni>(SkelSbject.EnumIndex_AniTracks.GetIndex(track), SkelSbject.GetAnimation(aniFolder, animationName), SkelSbject.EnumIndex_AniRanks.GetIndex(aniRankEnum), mixDuration);
            }



            /// <summary>
            /// 생성: <b>노말</b>
            /// </summary>
            public TSkelAni Create_Normal<TSkelAni>(TEnumTrack track, TEnumFolder aniFolder, string animationName) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_Normal<TSkelAni>(SkelSbject.EnumIndex_AniTracks.GetIndex(track), SkelSbject.GetAnimation(aniFolder, animationName), SkelSbject.AnimationMixDuration);
            }

            /// <summary>
            /// 생성: <b>노말</b>
            /// </summary>
            public TSkelAni Create_Normal<TSkelAni>(TEnumTrack track, TEnumFolder aniFolder, string animationName, TEnumRank aniRankEnum) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_Normal<TSkelAni>(SkelSbject.EnumIndex_AniTracks.GetIndex(track), SkelSbject.GetAnimation(aniFolder, animationName), SkelSbject.EnumIndex_AniRanks.GetIndex(aniRankEnum), SkelSbject.AnimationMixDuration);
            }



            //? 생성: 노말 중첩



            /// <summary>
            /// 생성: <b>노말 중첩</b>
            /// </summary>
            public SkelAni Create_NormalOverlap(TEnumTrack track, TEnumFolder aniFolder, string animationName, float mixDuration)
            {
                return SkelAni.Create_NormalOverlap(SkelSbject.EnumIndex_AniTracks.GetIndex(track), SkelSbject.AniFolder.GetAnimation(aniFolder, animationName), mixDuration);
            }

            /// <summary>
            /// 생성: <b>노말 중첩</b>
            /// </summary>
            public SkelAni Create_NormalOverlap(TEnumTrack track, TEnumFolder aniFolder, string animationName, TEnumRank aniRankEnum, float mixDuration)
            {
                return SkelAni.Create_NormalOverlap(SkelSbject.EnumIndex_AniTracks.GetIndex(track), SkelSbject.AniFolder.GetAnimation(aniFolder, animationName), SkelSbject.EnumIndex_AniRanks.GetIndex(aniRankEnum), mixDuration);
            }



            /// <summary>
            /// 생성: <b>노말 중첩</b>
            /// </summary>
            public SkelAni Create_NormalOverlap(TEnumTrack track, TEnumFolder aniFolder, string animationName)
            {
                return SkelAni.Create_NormalOverlap(SkelSbject.EnumIndex_AniTracks.GetIndex(track), SkelSbject.AniFolder.GetAnimation(aniFolder, animationName), SkelSbject.AnimationMixDuration);
            }

            /// <summary>
            /// 생성: <b>노말 중첩</b>
            /// </summary>
            public SkelAni Create_NormalOverlap(TEnumTrack track, TEnumFolder aniFolder, string animationName, TEnumRank aniRankEnum)
            {
                return SkelAni.Create_NormalOverlap(SkelSbject.EnumIndex_AniTracks.GetIndex(track), SkelSbject.AniFolder.GetAnimation(aniFolder, animationName), SkelSbject.EnumIndex_AniRanks.GetIndex(aniRankEnum), SkelSbject.AnimationMixDuration);
            }



            /// <summary>
            /// 생성: <b>노말 중첩</b>
            /// </summary>
            public TSkelAni Create_NormalOverlap<TSkelAni>(TEnumTrack track, TEnumFolder aniFolder, string animationName, float mixDuration) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_NormalOverlap<TSkelAni>(SkelSbject.EnumIndex_AniTracks.GetIndex(track), SkelSbject.AniFolder.GetAnimation(aniFolder, animationName), mixDuration);
            }

            /// <summary>
            /// 생성: <b>노말 중첩</b>
            /// </summary>
            public TSkelAni Create_NormalOverlap<TSkelAni>(TEnumTrack track, TEnumFolder aniFolder, string animationName, TEnumRank aniRankEnum, float mixDuration) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_NormalOverlap<TSkelAni>(SkelSbject.EnumIndex_AniTracks.GetIndex(track), SkelSbject.AniFolder.GetAnimation(aniFolder, animationName), SkelSbject.EnumIndex_AniRanks.GetIndex(aniRankEnum), mixDuration);
            }



            /// <summary>
            /// 생성: <b>노말 중첩</b>
            /// </summary>
            public TSkelAni Create_NormalOverlap<TSkelAni>(TEnumTrack track, TEnumFolder aniFolder, string animationName) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_NormalOverlap<TSkelAni>(SkelSbject.EnumIndex_AniTracks.GetIndex(track), SkelSbject.AniFolder.GetAnimation(aniFolder, animationName), SkelSbject.AnimationMixDuration);
            }

            /// <summary>
            /// 생성: <b>노말 중첩</b>
            /// </summary>
            public TSkelAni Create_NormalOverlap<TSkelAni>(TEnumTrack track, TEnumFolder aniFolder, string animationName, TEnumRank aniRankEnum) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_NormalOverlap<TSkelAni>(SkelSbject.EnumIndex_AniTracks.GetIndex(track), SkelSbject.AniFolder.GetAnimation(aniFolder, animationName), SkelSbject.EnumIndex_AniRanks.GetIndex(aniRankEnum), SkelSbject.AnimationMixDuration);
            }



            //? 생성: 반복



            /// <summary>
            /// 생성: <b>반복</b>
            /// </summary>
            public SkelAni Create_Loop(TEnumTrack track, TEnumFolder aniFolder, string animationName, float mixDuration)
            {
                return SkelAni.Create_Loop(SkelSbject.EnumIndex_AniTracks.GetIndex(track), SkelSbject.AniFolder.GetAnimation(aniFolder, animationName), mixDuration);
            }

            /// <summary>
            /// 생성: <b>반복</b>
            /// </summary>
            public SkelAni Create_Loop(TEnumTrack track, TEnumFolder aniFolder, string animationName, TEnumRank aniRankEnum, float mixDuration)
            {
                return SkelAni.Create_Loop(SkelSbject.EnumIndex_AniTracks.GetIndex(track), SkelSbject.AniFolder.GetAnimation(aniFolder, animationName), SkelSbject.EnumIndex_AniRanks.GetIndex(aniRankEnum), mixDuration);
            }



            /// <summary>
            /// 생성: <b>반복</b>
            /// </summary>
            public SkelAni Create_Loop(TEnumTrack track, TEnumFolder aniFolder, string animationName)
            {
                return SkelAni.Create_Loop(SkelSbject.EnumIndex_AniTracks.GetIndex(track), SkelSbject.AniFolder.GetAnimation(aniFolder, animationName), SkelSbject.AnimationMixDuration);
            }

            /// <summary>
            /// 생성: <b>반복</b>
            /// </summary>
            public SkelAni Create_Loop(TEnumTrack track, TEnumFolder aniFolder, string animationName, TEnumRank aniRankEnum)
            {
                return SkelAni.Create_Loop(SkelSbject.EnumIndex_AniTracks.GetIndex(track), SkelSbject.AniFolder.GetAnimation(aniFolder, animationName), SkelSbject.EnumIndex_AniRanks.GetIndex(aniRankEnum), SkelSbject.AnimationMixDuration);
            }



            /// <summary>
            /// 생성: <b>반복</b>
            /// </summary>
            public TSkelAni Create_Loop<TSkelAni>(TEnumTrack track, TEnumFolder aniFolder, string animationName, float mixDuration) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_Loop<TSkelAni>(SkelSbject.EnumIndex_AniTracks.GetIndex(track), SkelSbject.AniFolder.GetAnimation(aniFolder, animationName), mixDuration);
            }

            /// <summary>
            /// 생성: <b>반복</b>
            /// </summary>
            public TSkelAni Create_Loop<TSkelAni>(TEnumTrack track, TEnumFolder aniFolder, string animationName, TEnumRank aniRankEnum, float mixDuration) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_Loop<TSkelAni>(SkelSbject.EnumIndex_AniTracks.GetIndex(track), SkelSbject.AniFolder.GetAnimation(aniFolder, animationName), SkelSbject.EnumIndex_AniRanks.GetIndex(aniRankEnum), mixDuration);
            }



            /// <summary>
            /// 생성: <b>반복</b>
            /// </summary>
            public TSkelAni Create_Loop<TSkelAni>(TEnumTrack track, TEnumFolder aniFolder, string animationName) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_Loop<TSkelAni>(SkelSbject.EnumIndex_AniTracks.GetIndex(track), SkelSbject.AniFolder.GetAnimation(aniFolder, animationName), SkelSbject.AnimationMixDuration);
            }

            /// <summary>
            /// 생성: <b>반복</b>
            /// </summary>
            public TSkelAni Create_Loop<TSkelAni>(TEnumTrack track, TEnumFolder aniFolder, string animationName, TEnumRank aniRankEnum) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_Loop<TSkelAni>(SkelSbject.EnumIndex_AniTracks.GetIndex(track), SkelSbject.AniFolder.GetAnimation(aniFolder, animationName), SkelSbject.EnumIndex_AniRanks.GetIndex(aniRankEnum), SkelSbject.AnimationMixDuration);
            }



            //? 생성: 복제



            /// <summary>
            /// 생성: <b>복제 생성</b><br/>
            /// 받아온<paramref name="target"/>을 복제(Deep)하여 생성한다<br/>
            /// + Animation 을 수정한다
            /// </summary>
            public SkelAni Create_Copy(ISkelAni target, TEnumFolder aniFolder, string animationName)
            {
                return SkelAni.Create_Copy(target, SkelSbject.AniFolder.GetAnimation(aniFolder, animationName));
            }



            /// <summary>
            /// 생성: <b>복제 생성</b><br/>
            /// 받아온<paramref name="target"/>을 복제(Deep)하여 생성한다<br/>
            /// + Animation 을 수정한다
            /// </summary>
            public TSkelAni Create_Copy<TSkelAni>(ISkelAni target, TEnumFolder aniFolder, string animationName) where TSkelAni : SkelAni, new()
            {
                return SkelAni.Create_Copy<TSkelAni>(target, SkelSbject.AniFolder.GetAnimation(aniFolder, animationName));
            }



            ///======================================================================================================================================================
        }



        ///======================================================================================================================================================
    }
}