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



//? 열거형-인덱스 저장소가 들어있는 정도의 코드



namespace Pan.SpinePackage
{
    public abstract partial class SkelSbject
    {
        ///======================================================================================================================================================



        //? 인터페이스 풀세트



        /// <summary>
        /// 모든 열거형-인덱스 매니저 인터페이스가 구현되어있는 인터페이스
        /// </summary>
        public interface IEnumIndexs_All<TSkinLayer, TAniTrack, TAniRank, TAniTime> :
            IHoldEnumIndex_SkinLayer<TSkinLayer>,
            IHoldEnumIndex_AniTrack<TAniTrack>,
            IHoldEnumIndex_AniRank<TAniRank>,
            IHoldEnumIndex_AniTime<TAniTime>
            where TSkinLayer : struct, Enum
            where TAniTrack : struct, Enum
            where TAniRank : struct, Enum
            where TAniTime : struct, Enum
        { }



        ///======================================================================================================================================================



        //? 스킨 레이어



        /// <summary>
        /// <b>스킨 레이어</b>의 열거형-인덱스 매니저 인터페이스
        /// <para>제네릭 없이 <see cref="IBaseEnumIndexesManager"/>를 사용</para>
        /// </summary>
        public interface IHoldEnumIndex_SkinLayer
        {
            IEnumIndex_SkinLayer EnumIndex_SkinLayers { get; }
        }



        /// <summary>
        /// <b>스킨 레이어</b>의 열거형-인덱스 매니저 인터페이스
        /// </summary>
        /// <typeparam name="TSkinLayer">대상 열거형 스킨 레이어</typeparam>
        public interface IHoldEnumIndex_SkinLayer<TSkinLayer> where TSkinLayer : struct, Enum
        {
            EnumIndex_SkinLayer<TSkinLayer> EnumIndex_SkinLayer { get; }
        }



        /// <summary>
        /// <b>스킨 레이어</b>의 열거형-인덱스 매니저 인터페이스 (제네릭 없이 사용)
        /// </summary>
        public interface IEnumIndex_SkinLayer : IBaseEnumIndexesManager { }



        /// <summary>
        /// <b>스킨 레이어</b>의 열거형-인덱스 매니저
        /// </summary>
        /// <typeparam name="TSkinLayer">대상 열거형 스킨 레이어</typeparam>
        public class EnumIndex_SkinLayer<TSkinLayer> : BaseEnumIndexesManager<TSkinLayer>, IEnumIndex_SkinLayer, IHoldEnumIndex_SkinLayer<TSkinLayer> where TSkinLayer : struct, Enum
        {
            public EnumIndex_SkinLayer(bool autoInitialize) : base(autoInitialize) { }

            EnumIndex_SkinLayer<TSkinLayer> IHoldEnumIndex_SkinLayer<TSkinLayer>.EnumIndex_SkinLayer => this;
        }



        ///======================================================================================================================================================



        //? 애니메이션 트랙



        /// <summary>
        /// <b>애니메이션 트랙</b>의 열거형-인덱스 매니저 인터페이스
        /// <para>제네릭 없이 <see cref="IBaseEnumIndexesManager"/>를 사용</para>
        /// </summary>
        public interface IHoldEnumIndex_AniTrack
        {
            IEnumIndex_AniTrack EnumIndex_AniTracks { get; }
        }




        /// <summary>
        /// <b>애니메이션 트랙</b>의 열거형-인덱스 매니저 인터페이스
        /// </summary>
        /// <typeparam name="TSkinLayer">대상 열거형 애니메이션 트랙</typeparam>
        public interface IHoldEnumIndex_AniTrack<TAniTrack> where TAniTrack : struct, Enum
        {
            EnumIndex_AniTrack<TAniTrack> EnumIndex_AniTrack { get; }
        }



        /// <summary>
        /// <b>애니메이션 트랙</b>의 열거형-인덱스 매니저 인터페이스 (제네릭 없이 사용)
        /// </summary>
        public interface IEnumIndex_AniTrack : IBaseEnumIndexesManager { }



        /// <summary>
        /// <b>애니메이션 트랙</b>의 열거형-인덱스 매니저
        /// </summary>
        /// <typeparam name="TAniTrack">대상 열거형 애니메이션 트랙</typeparam>
        public class EnumIndex_AniTrack<TAniTrack> : BaseEnumIndexesManager<TAniTrack>, IEnumIndex_AniTrack, IHoldEnumIndex_AniTrack<TAniTrack> where TAniTrack : struct, Enum
        {
            public EnumIndex_AniTrack(bool autoInitialize) : base(autoInitialize) { }

            EnumIndex_AniTrack<TAniTrack> IHoldEnumIndex_AniTrack<TAniTrack>.EnumIndex_AniTrack => this;

            ///======================================================================================================================================================



            //? MixDuration 확장


            /// <summary>
            /// 트랙이 비워질 때 (해당 트랙에 더이상 애니메이션이 재생되지 않음)
            /// <para>Empty 애니메이션을 실행하려고 할때, 일반적인 MixDuration이 아닌</para>
            /// <para>별도의 MixDuration을 추가하기</para>
            /// </summary>
            public void AddCustomEndMixDuration(SkelObject.AniCore.Settings aniSetting, TAniTrack trackEnum, float emptyAnimationMixDuration)
            {
                aniSetting.Add_EmptyAnimationMixduration(GetIndex(trackEnum), emptyAnimationMixDuration);
            }



            /// <summary>
            /// 트랙이 비워질 때 (해당 트랙에 더이상 애니메이션이 재생되지 않음)
            /// <para>Empty 애니메이션을 실행하려고 할때, 일반적인 MixDuration이 아닌</para>
            /// <para>별도의 MixDuration을 얻어보기</para>
            /// </summary>
            public bool TryGetCustomEndMixDuration(SkelObject.AniCore.Settings aniSetting, TAniTrack trackEnum, out float emptyAnimationMixDuration)
            {
                return aniSetting.TryGet_EmptyAnimationMixduration(GetIndex(trackEnum), out emptyAnimationMixDuration);
            }



            /// <summary>
            /// 트랙이 비워질 때 (해당 트랙에 더이상 애니메이션이 재생되지 않음)
            /// <para>Empty 애니메이션을 실행하려고 할때, 일반적인 MixDuration이 아닌</para>
            /// <para>별도의 MixDuration을 한번에 추가하기</para>
            /// </summary>
            public void QuickFullSetting_CustomEndMixDurations(SkelObject.AniCore.Settings aniSetting, params ValueTuple<TAniTrack, float>[] trackIndex_MixDurations)
            {
                aniSetting.QuickFullSetting_CustomEndMixDurations(trackIndex_MixDurations);
            }



            ///======================================================================================================================================================            



            //? 애니메이션 종료



            /// <summary>
            /// 애니메이션을 <b>종료</b>시킨다
            /// <para><paramref name="track"/>에 해당하는 <see cref="SkelObject.AniCore.Track"/>의 애니메이션을 종료시킨다</para>
            /// <para>종료에 성공하면 true를 반환한다</para>
            /// </summary>
            public bool EndAni(SkelObject skelObject, TAniTrack track) => skelObject.Ani.EndAni(GetIndex(track));




            /// <summary>
            /// 애니메이션을 <b>종료</b>시킨다
            /// <para><paramref name="track"/>에 해당하는 <see cref="SkelObject.AniCore.Track"/>의 애니메이션을 종료시킨다</para>
            /// <para>종료에 성공하면 true를 반환한다</para>
            ///<para><b><paramref name="starter"/>와 <see cref="SkelObject.AniCore.Track.AnimationStarter"/>가 같아야, 종료가 이루어진다</b></para>
            /// </summary>
            public bool EndAni(SkelObject skelObject, object starter, TAniTrack track) => skelObject.Ani.EndAni(starter, GetIndex(track));



            /// <summary>
            /// 애니메이션을 <b>종료</b>시킨다
            /// <para>(CustomTrackIndex로 실행되었을때를 가정하여, <see cref="ISkelAni.TrackIndex"/>의 TrackIndex가 아닌 받아온 <paramref name="trackIndex"/>을 사용한다)</para>
            /// <para><paramref name="track"/>에 해당하는 <see cref="Track"/>의 애니메이션이 <see cref="ISkelAni.Animation"/>과 같다면, 종료시킨다</para>
            /// <para>종료에 성공하면 true를 반환한다</para>
            /// </summary>
            public bool EndAni(SkelObject skelObject, TAniTrack track, ISkelAni skelAni) => skelObject.Ani.EndAni(GetIndex(track), skelAni);




            /// <summary>
            /// 애니메이션을 <b>종료</b>시킨다
            /// <para>(CustomTrackIndex로 실행되었을때를 가정하여, <see cref="ISkelAni.TrackIndex"/>의 TrackIndex가 아닌 받아온 <paramref name="trackIndex"/>을 사용한다)</para>
            /// <para><paramref name="trackIndex"/>에 해당하는 <see cref="Track"/>의 애니메이션이 <see cref="ISkelAni.Animation"/>과 같다면, 종료시킨다</para>
            /// <para>종료에 성공하면 true를 반환한다</para>
            ///<para><b><paramref name="starter"/>와 <see cref="Track.AnimationStarter"/>가 같아야, 종료가 이루어진다</b></para>
            /// </summary>
            public bool EndAni(SkelObject skelObject, object starter, TAniTrack track, ISkelAni skelAni) => skelObject.Ani.EndAni(starter, GetIndex(track), skelAni);




            ///======================================================================================================================================================

        }



        ///======================================================================================================================================================



        //? 애니메이션 랭크



        /// <summary>
        /// <b>애니메이션 랭크</b>의 열거형-인덱스 매니저 인터페이스
        /// <para>제네릭 없이 <see cref="IBaseEnumIndexesManager"/>를 사용</para>
        /// </summary>
        public interface IHoldEnumIndex_AniRank
        {
            IEnumIndex_AniRank EnumIndex_AniRanks { get; }
        }



        /// <summary>
        /// <b>애니메이션 랭크</b>의 열거형-인덱스 매니저 인터페이스
        /// </summary>
        /// <typeparam name="TSkinLayer">대상 열거형 애니메이션 랭크</typeparam>
        public interface IHoldEnumIndex_AniRank<TAniRank> where TAniRank : struct, Enum
        {
            EnumIndex_AniRank<TAniRank> EnumIndex_AniRank { get; }
        }



        /// <summary>
        /// <b>애니메이션 랭크</b>의 열거형-인덱스 매니저 인터페이스 (제네릭 없이 사용)
        /// </summary>
        public interface IEnumIndex_AniRank : IBaseEnumIndexesManager { }



        /// <summary>
        /// <b>애니메이션 랭크</b>의 열거형-인덱스 매니저
        /// </summary>
        /// <typeparam name="TAniRank">대상 열거형 애니메이션 랭크</typeparam>
        public class EnumIndex_AniRank<TAniRank> : BaseEnumIndexesManager<TAniRank>, IEnumIndex_AniRank, IHoldEnumIndex_AniRank<TAniRank> where TAniRank : struct, Enum
        {
            public EnumIndex_AniRank(bool autoInitialize) : base(autoInitialize) { }

            EnumIndex_AniRank<TAniRank> IHoldEnumIndex_AniRank<TAniRank>.EnumIndex_AniRank => this;
        }



        ///======================================================================================================================================================



        //? 애니메이션 타임



        /// <summary>
        /// <b>애니메이션 타임</b>의 열거형-인덱스 매니저 인터페이스
        /// <para>제네릭 없이 <see cref="IBaseEnumIndexesManager"/>를 사용</para>
        /// </summary>
        public interface IHoldEnumIndex_AniTime
        {
            IEnumIndex_AniTime EnumIndex_AniTimes { get; }
        }



        /// <summary>
        /// <b>애니메이션 타임</b>의 열거형-인덱스 매니저 인터페이스
        /// </summary>
        /// <typeparam name="TSkinLayer">대상 열거형 애니메이션 타임</typeparam>
        public interface IHoldEnumIndex_AniTime<TAniTime> where TAniTime : struct, Enum
        {
            EnumIndex_AniTime<TAniTime> EnumIndex_AniTime { get; }
        }


        /// <summary>
        /// <b>애니메이션 타임</b>의 열거형-인덱스 매니저 인터페이스 (제네릭 없이 사용)
        /// </summary>
        public interface IEnumIndex_AniTime : IBaseEnumIndexesManager { }



        /// <summary>
        /// <b>애니메이션 타임</b>의 열거형-인덱스 매니저
        /// </summary>
        /// <typeparam name="TAniTime">대상 열거형 애니메이션 타임</typeparam>
        public class EnumIndex_AniTime<TAniTime> : BaseEnumIndexesManager<TAniTime>, IEnumIndex_AniTime, IHoldEnumIndex_AniTime<TAniTime> where TAniTime : struct, Enum
        {
            public EnumIndex_AniTime(bool autoInitialize) : base(autoInitialize) { }

            EnumIndex_AniTime<TAniTime> IHoldEnumIndex_AniTime<TAniTime>.EnumIndex_AniTime => this;
        }



        ///======================================================================================================================================================
    }
}
