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
using System.Linq;
using Pan.SpineUtil;
using UnityEngine.Audio;
using static Pan.SpinePackage.SkelObject.AniCore.Players;


namespace Pan.SpinePackage
{
    /// <summary>
    /// 스파인 애니메이션의 확장 인터페이스
    /// </summary>
    public interface ISkelAni
    {
        /// <summary>
        /// 애니메이션 트랙 인덱스<br/>
        /// 이 값과 동일한 트랙에서 애니메이션이 실행된다
        /// </summary>
        int TrackIndex { get; }

        /// <summary>
        /// 애니메이션 랭크<br/>
        /// 이 값에 따라, 재생중이던 애니메이션을 중단하고 덮어씌울지 등등..<br/>
        /// 우선순위를 결정짓는 요소중에 하나가 된다
        /// </summary>
        int RankType { get; }

        /// <summary>
        /// 해당 스파인 애니메이션
        /// </summary>
        Spine.Animation Animation { get; }

        /// <summary>
        /// 애니메이션 타임 타입<br/>
        /// 이 타입에 따라, 애니메이션이 어떤 TimeScale을 사용할지 등이 결정된다
        /// <c>Enum</c>으로 지정되는 
        /// </summary>
        int AniTimeType { get; }

        /// <summary>
        /// 애니메이션 반복 여부
        /// </summary>
        bool Loop { get; }

        /// <summary>
        /// 애니메이션 중첩 여부<br/>
        /// 재생중인 이 애니메이션 도중에 또 시작될 경우에<br/>
        /// true라면, 재생중인 애니메이션이 중단되고 다시 처음부터 재생된다<br/>
        /// false라면, 재생중인 애니메이션이 계속 재생된다 (무시)
        /// </summary>
        bool Overlap { get; }

        /// <summary>
        /// 이 애니메이션을 실행할때 MixDuration<br/>
        /// </summary>
        float MixDuration { get; }

        /// <summary>
        /// 이 애니메이션이 종료될때의 MixDuration
        /// </summary>
        float EndMixDuration { get; }

        /// <summary>
        /// 이 애니메이션이 실행될때, 곱해질 추가 속도 (기본값: 1)
        /// </summary>
        float BonusSpeed { get; }
    }



    /// <summary>
    /// 스파인 애니메이션의 확장 인터페이스 보유 인터페이스
    /// </summary>
    public interface IHoldSkelAni
    {
        ISkelAni SkelAni { get; }
    }



    /// <summary>
    /// 스파인 애니메이션의 확장
    /// </summary>
    public class SkelAni : ISkelAni, IHoldSkelAni, ICopyable<ISkelAni>
    {
        ///======================================================================================================================================================



        //? 생성자 및 팩토리 메서드



        #region 생성자 및 팩토리 메서드



        public SkelAni(int track, Spine.Animation animation, int rankType, int aniTimeType, bool loop, bool overlap, float mixduration, float endMixDuration, float bonusSpeed)
        {
            Create(track, animation, rankType, aniTimeType, loop, overlap, mixduration, endMixDuration, bonusSpeed);
            //TrackIndex = track;
            //RankType = rankType;
            //Animation = animation;
            //AniTimeType = aniTimeType;
            //Loop = loop;
            //Overlap = overlap;
            //MixDuration = mixduration;
            //BonusSpeed = bonusSpeed;
        }



        public SkelAni(ISkelAni skelAni)
        {
            Create(skelAni);
            //Copy(skelAni);
        }



        protected SkelAni() { }



        //? 내부 생성자용 메서드



        private void Create(int track, Spine.Animation animation, int rankType, int aniTimeType, bool loop, bool overlap, float mixduration, float endMixDuration, float bonusSpeed)
        {
            TrackIndex = track;
            RankType = rankType;
            Animation = animation;
            AniTimeType = aniTimeType;
            Loop = loop;
            Overlap = overlap;
            MixDuration = mixduration;
            EndMixDuration = endMixDuration;
            BonusSpeed = bonusSpeed;
        }



        private void Create(ISkelAni skelAni)
        {
            Copy(skelAni);
        }



        //? 생성: 노말



        /// <summary>
        /// 생성: <b>노말</b>
        /// </summary>
        public static SkelAni Create_Normal(int trackIndex, Spine.Animation animation, float mixDuration)
        {
            return new SkelAni(trackIndex, animation, 0, 0, false, false, mixDuration, mixDuration, 1);
        }

        /// <summary>
        /// 생성: <b>노말</b>
        /// </summary>
        public static SkelAni Create_Normal(int trackIndex, Spine.Animation animation, int rankType, float mixDuration)
        {
            return new SkelAni(trackIndex, animation, rankType, 0, false, false, mixDuration, mixDuration, 1);
        }



        /// <summary>
        /// 생성: <b>노말</b>
        /// </summary>
        /// <param name="mixDuration"><see cref="IAnimationMixDuration.AnimationMixDuration"/>이 구현된 객체를 넣어 지정한다</param>
        public static SkelAni Create_Normal(int trackIndex, Spine.Animation animation, IAnimationMixDuration mixDuration)
        {
            return new SkelAni(trackIndex, animation, 0, 0, false, false, mixDuration.AnimationMixDuration, mixDuration.AnimationMixDuration, 1);
        }

        /// <summary>
        /// 생성: <b>노말</b>
        /// </summary>
        /// <param name="mixDuration"><see cref="IAnimationMixDuration.AnimationMixDuration"/>이 구현된 객체를 넣어 지정한다</param>
        public static SkelAni Create_Normal(int trackIndex, Spine.Animation animation, int rankType, IAnimationMixDuration mixDuration)
        {
            return new SkelAni(trackIndex, animation, rankType, 0, false, false, mixDuration.AnimationMixDuration, mixDuration.AnimationMixDuration, 1);
        }



        /// <summary>
        /// 생성: <b>노말</b>
        /// </summary>
        public static TSkelAni Create_Normal<TSkelAni>(int trackIndex, Spine.Animation animation, float mixDuration) where TSkelAni : SkelAni, new()
        {
            var result = new TSkelAni();
            result.Create(trackIndex, animation, 0, 0, false, false, mixDuration, mixDuration, 1);
            return result;
        }

        /// <summary>
        /// 생성: <b>노말</b>
        /// </summary>
        public static TSkelAni Create_Normal<TSkelAni>(int trackIndex, Spine.Animation animation, int rankType, float mixDuration) where TSkelAni : SkelAni, new()
        {
            var result = new TSkelAni();
            result.Create(trackIndex, animation, rankType, 0, false, false, mixDuration, mixDuration, 1);
            return result;
        }



        /// <summary>
        /// 생성: <b>노말</b>
        /// </summary>
        /// <param name="mixDuration"><see cref="IAnimationMixDuration.AnimationMixDuration"/>이 구현된 객체를 넣어 지정한다</param>
        public static TSkelAni Create_Normal<TSkelAni>(int trackIndex, Spine.Animation animation, IAnimationMixDuration mixDuration) where TSkelAni : SkelAni, new()
        {
            var result = new TSkelAni();
            result.Create(trackIndex, animation, 0, 0, false, false, mixDuration.AnimationMixDuration, mixDuration.AnimationMixDuration, 1);
            return result;
        }

        /// <summary>
        /// 생성: <b>노말</b>
        /// </summary>
        /// <param name="mixDuration"><see cref="IAnimationMixDuration.AnimationMixDuration"/>이 구현된 객체를 넣어 지정한다</param>
        public static TSkelAni Create_Normal<TSkelAni>(int trackIndex, Spine.Animation animation, int rankType, IAnimationMixDuration mixDuration) where TSkelAni : SkelAni, new()
        {
            var result = new TSkelAni();
            result.Create(trackIndex, animation, rankType, 0, false, false, mixDuration.AnimationMixDuration, mixDuration.AnimationMixDuration, 1);
            return result;
        }



        //? 생성: 노말 중첩



        /// <summary>
        /// 생성: <b>노말 중첩</b>
        /// </summary>
        public static SkelAni Create_NormalOverlap(int trackIndex, Spine.Animation animation, float mixDuration)
        {
            return new SkelAni(trackIndex, animation, 0, 0, false, true, mixDuration, mixDuration, 1);
        }

        /// <summary>
        /// 생성: <b>노말 중첩</b>
        /// </summary>
        public static SkelAni Create_NormalOverlap(int trackIndex, Spine.Animation animation, int rankType, float mixDuration)
        {
            return new SkelAni(trackIndex, animation, rankType, 0, false, true, mixDuration, mixDuration, 1);
        }



        /// <summary>
        /// 생성: <b>노말 중첩</b>
        /// </summary>
        /// <param name="mixDuration"><see cref="IAnimationMixDuration.AnimationMixDuration"/>이 구현된 객체를 넣어 지정한다</param>
        public static SkelAni Create_NormalOverlap(int trackIndex, Spine.Animation animation, IAnimationMixDuration mixDuration)
        {
            return new SkelAni(trackIndex, animation, 0, 0, false, true, mixDuration.AnimationMixDuration, mixDuration.AnimationMixDuration, 1);
        }

        /// <summary>
        /// 생성: <b>노말 중첩</b>
        /// </summary>
        /// <param name="mixDuration"><see cref="IAnimationMixDuration.AnimationMixDuration"/>이 구현된 객체를 넣어 지정한다</param>
        public static SkelAni Create_NormalOverlap(int trackIndex, Spine.Animation animation, int rankType, IAnimationMixDuration mixDuration)
        {
            return new SkelAni(trackIndex, animation, rankType, 0, false, true, mixDuration.AnimationMixDuration, mixDuration.AnimationMixDuration, 1);
        }



        /// <summary>
        /// 생성: <b>노말 중첩</b>
        /// </summary>
        public static TSkelAni Create_NormalOverlap<TSkelAni>(int trackIndex, Spine.Animation animation, float mixDuration) where TSkelAni : SkelAni, new()
        {
            var result = new TSkelAni();
            result.Create(trackIndex, animation, 0, 0, false, true, mixDuration, mixDuration, 1);
            return result;
        }

        /// <summary>
        /// 생성: <b>노말 중첩</b>
        /// </summary>
        public static TSkelAni Create_NormalOverlap<TSkelAni>(int trackIndex, Spine.Animation animation, int rankType, float mixDuration) where TSkelAni : SkelAni, new()
        {
            var result = new TSkelAni();
            result.Create(trackIndex, animation, rankType, 0, false, true, mixDuration, mixDuration, 1);
            return result;
        }



        /// <summary>
        /// 생성: <b>노말 중첩</b>
        /// </summary>
        /// <param name="mixDuration"><see cref="IAnimationMixDuration.AnimationMixDuration"/>이 구현된 객체를 넣어 지정한다</param>
        public static TSkelAni Create_NormalOverlap<TSkelAni>(int trackIndex, Spine.Animation animation, IAnimationMixDuration mixDuration) where TSkelAni : SkelAni, new()
        {
            var result = new TSkelAni();
            result.Create(trackIndex, animation, 0, 0, false, true, mixDuration.AnimationMixDuration, mixDuration.AnimationMixDuration, 1);
            return result;
        }

        /// <summary>
        /// 생성: <b>노말 중첩</b>
        /// </summary>
        /// <param name="mixDuration"><see cref="IAnimationMixDuration.AnimationMixDuration"/>이 구현된 객체를 넣어 지정한다</param>
        public static TSkelAni Create_NormalOverlap<TSkelAni>(int trackIndex, Spine.Animation animation, int rankType, IAnimationMixDuration mixDuration) where TSkelAni : SkelAni, new()
        {
            var result = new TSkelAni();
            result.Create(trackIndex, animation, rankType, 0, false, true, mixDuration.AnimationMixDuration, mixDuration.AnimationMixDuration, 1);
            return result;
        }



        //? 생성: 반복



        /// <summary>
        /// 생성: <b>반복</b>
        /// </summary>
        public static SkelAni Create_Loop(int trackIndex, Spine.Animation animation, float mixDuration)
        {
            return new SkelAni(trackIndex, animation, 0, 0, true, false, mixDuration, mixDuration, 1);
        }

        /// <summary>
        /// 생성: <b>반복</b>
        /// </summary>
        public static SkelAni Create_Loop(int trackIndex, Spine.Animation animation, int rankType, float mixDuration)
        {
            return new SkelAni(trackIndex, animation, rankType, 0, true, false, mixDuration, mixDuration, 1);
        }



        /// <summary>
        /// 생성: <b>반복</b>
        /// </summary>
        /// <param name="mixDuration"><see cref="IAnimationMixDuration.AnimationMixDuration"/>이 구현된 객체를 넣어 지정한다</param>
        public static SkelAni Create_Loop(int trackIndex, Spine.Animation animation, IAnimationMixDuration mixDuration)
        {
            return new SkelAni(trackIndex, animation, 0, 0, true, false, mixDuration.AnimationMixDuration, mixDuration.AnimationMixDuration, 1);
        }

        /// <summary>
        /// 생성: <b>반복</b>
        /// </summary>
        /// <param name="mixDuration"><see cref="IAnimationMixDuration.AnimationMixDuration"/>이 구현된 객체를 넣어 지정한다</param>
        public static SkelAni Create_Loop(int trackIndex, Spine.Animation animation, int rankType, IAnimationMixDuration mixDuration)
        {
            return new SkelAni(trackIndex, animation, rankType, 0, true, false, mixDuration.AnimationMixDuration, mixDuration.AnimationMixDuration, 1);
        }



        /// <summary>
        /// 생성: <b>반복</b>
        /// </summary>
        public static TSkelAni Create_Loop<TSkelAni>(int trackIndex, Spine.Animation animation, float mixDuration) where TSkelAni : SkelAni, new()
        {
            var result = new TSkelAni();
            result.Create(trackIndex, animation, 0, 0, true, false, mixDuration, mixDuration, 1);
            return result;
        }

        /// <summary>
        /// 생성: <b>반복</b>
        /// </summary>
        public static TSkelAni Create_Loop<TSkelAni>(int trackIndex, Spine.Animation animation, int rankType, float mixDuration) where TSkelAni : SkelAni, new()
        {
            var result = new TSkelAni();
            result.Create(trackIndex, animation, rankType, 0, true, false, mixDuration, mixDuration, 1);
            return result;
        }



        /// <summary>
        /// 생성: <b>반복</b>
        /// </summary>
        /// <param name="mixDuration"><see cref="IAnimationMixDuration.AnimationMixDuration"/>이 구현된 객체를 넣어 지정한다</param>
        public static TSkelAni Create_Loop<TSkelAni>(int trackIndex, Spine.Animation animation, IAnimationMixDuration mixDuration) where TSkelAni : SkelAni, new()
        {
            var result = new TSkelAni();
            result.Create(trackIndex, animation, 0, 0, true, false, mixDuration.AnimationMixDuration, mixDuration.AnimationMixDuration, 1);
            return result;
        }

        /// <summary>
        /// 생성: <b>반복</b>
        /// </summary>
        /// <param name="mixDuration"><see cref="IAnimationMixDuration.AnimationMixDuration"/>이 구현된 객체를 넣어 지정한다</param>
        public static TSkelAni Create_Loop<TSkelAni>(int trackIndex, Spine.Animation animation, int rankType, IAnimationMixDuration mixDuration) where TSkelAni : SkelAni, new()
        {
            var result = new TSkelAni();
            result.Create(trackIndex, animation, rankType, 0, true, false, mixDuration.AnimationMixDuration, mixDuration.AnimationMixDuration, 1);
            return result;
        }



        //? 생성: 복제



        /// <summary>
        /// 생성: <b>복제 생성</b><br/>
        /// 받아온<paramref name="target"/>을 복제(Deep)하여 생성한다
        /// </summary>
        public static SkelAni Create_Copy(ISkelAni target)
        {
            return new SkelAni(target);
        }



        /// <summary>
        /// 생성: <b>복제 생성</b><br/>
        /// 받아온<paramref name="target"/>을 복제(Deep)하여 생성한다<br/>
        /// + <see cref="Animation"/> 을 수정한다
        /// </summary>
        public static SkelAni Create_Copy(ISkelAni target, Spine.Animation animation)
        {
            var result = new SkelAni(target);
            result.Animation = animation;
            return result;
        }



        /// <summary>
        /// 생성: <b>복제 생성</b><br/>
        /// 받아온<paramref name="target"/>을 복제(Deep)하여 생성한다<br/>
        /// + <see cref="TrackIndex"/>을 수정한다
        /// </summary>
        public static SkelAni Create_Copy(ISkelAni target, int trackIndex)
        {
            var result = new SkelAni(target);
            result.TrackIndex = trackIndex;
            return result;
        }



        /// <summary>
        /// 생성: <b>복제 생성</b><br/>
        /// 받아온<paramref name="target"/>을 복제(Deep)하여 생성한다
        /// </summary>
        public static TSkelAni Create_Copy<TSkelAni>(ISkelAni target) where TSkelAni : SkelAni, new()
        {
            var result = new TSkelAni();
            result.Create(target);
            return result;
        }



        /// <summary>
        /// 생성: <b>복제 생성</b><br/>
        /// 받아온<paramref name="target"/>을 복제(Deep)하여 생성한다<br/>
        /// + <see cref="Animation"/> 을 수정한다
        /// </summary>
        public static TSkelAni Create_Copy<TSkelAni>(ISkelAni target, Spine.Animation animation) where TSkelAni : SkelAni, new()
        {
            var result = new TSkelAni();
            result.Create(target);
            result.Animation = animation;
            return result;
        }



        /// <summary>
        /// 생성: <b>복제 생성</b><br/>
        /// 받아온<paramref name="target"/>을 복제(Deep)하여 생성한다<br/>
        /// + <see cref="TrackIndex"/>을 수정한다
        /// </summary>
        public static TSkelAni Create_Copy<TSkelAni>(ISkelAni target, int trackIndex) where TSkelAni : SkelAni, new()
        {
            var result = new TSkelAni();
            result.Create(target);
            result.TrackIndex = trackIndex;
            return result;
        }



        //? 설정: 껍질



        /// <summary>
        /// 생성: <b>껍질 생성</b>
        ///  <para><see cref="Copy(ISkelAni)"/>로 다른 애니메이션들을 복사해오는 껍질 애니메이션으로 생성한다</para>
        /// </summary>
        public static SkelAni Create_Husk()
        {
            var result = new SkelAni();
            result.IsHuskSkelAni = true;
            return result;
        }



        #endregion



        ///======================================================================================================================================================



        //? ISkelAni 필드 (추가시 Copy, Reset 메세도 갱신 요망)



        #region ISkelAni 필드

        ///<inheritdoc/>
        public int TrackIndex { get; set; }

        ///<inheritdoc/>
        public int RankType { get; set; }

        ///<inheritdoc/>
        public Spine.Animation Animation { get; set; }

        ///<inheritdoc/>
        public int AniTimeType { get; set; }

        ///<inheritdoc/>
        public bool Loop { get; set; }

        ///<inheritdoc/>
        public bool Overlap { get; set; }

        ///<inheritdoc/>
        public float MixDuration { get; set; }

        ///<inheritdoc/>
        public float EndMixDuration { get; set; }

        ///<inheritdoc/>
        public float BonusSpeed { get; set; }

        #endregion



        #region 체이닝 설정 메서드

        public SkelAni SetTrackIndex(int trackIndex)
        {
            TrackIndex = trackIndex;
            return this;
        }

        public SkelAni SetRank(int rank)
        {
            RankType = rank;
            return this;
        }

        public SkelAni SetAnimation(Spine.Animation animation)
        {
            Animation = animation;
            return this;
        }

        public SkelAni SetAniTimeEType(int aniTimeEType)
        {
            AniTimeType = aniTimeEType;
            return this;
        }

        public SkelAni SetLoop(bool loop)
        {
            Loop = loop;
            return this;
        }

        public SkelAni SetOverlap(bool overlap)
        {
            Overlap = overlap;
            return this;
        }

        public SkelAni SetMixDuration(float mixDuration)
        {
            MixDuration = mixDuration;
            return this;
        }

        public SkelAni SetEndMixDuration(float endMixDuration)
        {
            EndMixDuration = endMixDuration;
            return this;
        }

        public SkelAni SetBonusSpeed(float bonusSpeed)
        {
            BonusSpeed = bonusSpeed;
            return this;
        }

        #endregion



        ///======================================================================================================================================================



        //? 정보 필드



        /// <summary>
        /// 껍질  <see cref="SkelAni"/> 여부
        /// </summary>
        public bool IsHuskSkelAni { get; private set; }



        /// <summary>
        /// 커스텀 트랙을 사용중인지 여부
        /// <para><see cref="TrackIndex"/>가 <c>-1</c>일경우</para>
        /// <para>별도로 실행시 트랙이 정해지는 커스텀 트랙이다</para>
        /// </summary>
        public bool IsCustomTrackIndex => TrackIndex == -1;


        ISkelAni IHoldSkelAni.SkelAni => this;



        ///======================================================================================================================================================



        public void Copy(ISkelAni original)
        {
            TrackIndex = original.TrackIndex;
            RankType = original.RankType;
            Animation = original.Animation;
            AniTimeType = original.AniTimeType;
            Loop = original.Loop;
            Overlap = original.Overlap;
            MixDuration = original.MixDuration;
            EndMixDuration = original.EndMixDuration;
            BonusSpeed = original.BonusSpeed;
        }



        /// <summary>
        /// 설정값 초기화,
        /// <para><see cref="IsHuskSkelAni"/>일때 주로 사용하여, 사용되지않을때 초기화</para>
        /// </summary>
        public virtual void ResetValues()
        {
            TrackIndex = 0;
            RankType = 0;
            Animation = null;
            AniTimeType = 0;
            Loop = false;
            Overlap = false;
            MixDuration = 0;
            EndMixDuration = 0;
            BonusSpeed = 0;
        }



        ///======================================================================================================================================================



        //? 이 애니메이션 실행(Execute) 관련 (SkelObject를 받아와서 실행)



        #region 이 애니메이션 실행(Execute) 관련



        //? 애니메이션 실행



        ///<summary>
        ///이 애니메이션 실행 (Execute)
        ///</summary>
        ///<param name="skelObject">대상 <see cref="SkelObject"/></param>
        ///<param name="customTrackIndex"><paramref name="skelAni"/>의 <see cref="ISkelAni.TrackIndex"/>와는 다른 별개의 TrackIndex를 실행하고싶을경우, 할당</param>
        public bool ExecuteAni(SkelObject skelObject, int? customTrackIndex)
        {
            return skelObject.Ani.ExecuteAni(this, customTrackIndex);
        }



        ///<summary>
        ///이 애니메이션 실행 (Execute)
        ///</summary>
        ///<param name="skelObject">대상 <see cref="SkelObject"/></param>
        public bool ExecuteAni(SkelObject skelObject)
        {
            return skelObject.Ani.ExecuteAni(this);
        }



        //? 애니메이션 실행 + starter



        ///<summary>
        ///애니메이션 실행 (Execute)
        ///<para>+ 실행에 성공할경우, <paramref name="starter"/>를 <see cref="SkelObject.AniCore.Track.AnimationStarter"/>에 할당한다</para>
        ///</summary>
        ///<param name="skelObject">대상 <see cref="SkelObject"/></param>
        ///<param name="starter">애니메이션 실행에 성공하면, 이 오브젝트를 등록한다</param>
        ///<param name="customTrackIndex"><paramref name="skelAni"/>의 <see cref="ISkelAni.TrackIndex"/>와는 다른 별개의 TrackIndex를 실행하고싶을경우, 할당</param>
        public bool ExecuteAni(SkelObject skelObject, object starter, int? customTrackIndex)
        {
            return skelObject.Ani.ExecuteAni(starter, this, customTrackIndex);
        }



        ///<summary>
        ///애니메이션 실행 (Execute)
        ///<para>+ 실행에 성공할경우, <paramref name="starter"/>를 <see cref="SkelObject.AniCore.Track.AnimationStarter"/>에 할당한다</para>
        ///</summary>
        ///<param name="skelObject">대상 <see cref="SkelObject"/></param>
        ///<param name="starter">애니메이션 실행에 성공하면, 이 오브젝트를 등록한다</param>
        public bool ExecuteAni(SkelObject skelObject, object starter)
        {
            return skelObject.Ani.ExecuteAni(starter, this);
        }



        //? 애니메이션 실행(Track반환)



        ///<summary>
        ///애니메이션 실행 (Execute)
        ///<para>실행에 성공했을 경우, 실행된 애니메이션이 속한 <see cref="Track"/>을 즉시 반환한다</para>
        ///</summary>
        ///<param name="skelObject">대상 <see cref="SkelObject"/></param>
        ///<param name="customTrackIndex"><paramref name="skelAni"/>의 <see cref="ISkelAni.TrackIndex"/>와는 다른 별개의 TrackIndex를 실행하고싶을경우, 할당</param>
        public SkelObject.AniCore.Track ExecuteAni_GetTrack(SkelObject skelObject, int? customTrackIndex)
        {
            return skelObject.Ani.ExecuteAni_GetTrack(this, customTrackIndex);
        }



        ///<summary>
        ///애니메이션 실행 (Execute)
        ///<para>실행에 성공했을 경우, 실행된 애니메이션이 속한 <see cref="Track"/>을 즉시 반환한다</para>
        ///</summary>
        ///<param name="skelObject">대상 <see cref="SkelObject"/></param>
        public SkelObject.AniCore.Track ExecuteAni_GetTrack(SkelObject skelObject)
        {
            return skelObject.Ani.ExecuteAni_GetTrack(this);
        }



        //? 애니메이션 실행(Track반환) + starter



        ///<summary>
        ///애니메이션 실행 (Execute)
        ///<para>실행에 성공했을 경우, 실행된 애니메이션이 속한 <see cref="Track"/>을 즉시 반환한다</para>
        ///<para>+ 실행에 성공할경우, <paramref name="starter"/>를 <see cref="Track.AnimationStarter"/>에 할당한다</para>
        ///</summary>
        ///<param name="starter">애니메이션 실행에 성공하면, 이 오브젝트를 등록한다</param>
        ///<param name="skelObject">대상 <see cref="SkelObject"/></param>
        ///<param name="customTrackIndex"><paramref name="skelAni"/>의 <see cref="ISkelAni.TrackIndex"/>와는 다른 별개의 TrackIndex를 실행하고싶을경우, 할당</param>
        public SkelObject.AniCore.Track ExecuteAni_GetTrack(SkelObject skelObject, object starter, int? customTrackIndex)
        {
            return skelObject.Ani.ExecuteAni_GetTrack(starter, this, customTrackIndex);
        }



        ///<summary>
        ///애니메이션 실행 (Execute)
        ///<para>실행에 성공했을 경우, 실행된 애니메이션이 속한 <see cref="Track"/>을 즉시 반환한다</para>
        ///<para>+ 실행에 성공할경우, <paramref name="starter"/>를 <see cref="Track.AnimationStarter"/>에 할당한다</para>
        ///</summary>
        ///<param name="starter">애니메이션 실행에 성공하면, 이 오브젝트를 등록한다</param>
        ///<param name="skelObject">대상 <see cref="SkelObject"/></param>
        public SkelObject.AniCore.Track ExecuteAni_GetTrack(SkelObject skelObject, object starter)
        {
            return skelObject.Ani.ExecuteAni_GetTrack(starter, this);
        }



        //? 애니메이션 실행(무조건 Track반환)



        ///<summary>
        ///애니메이션 실행 (Execute)
        ///<para>실행 성공 여부와 관계없이 , 실행될 애니메이션이 속한 <see cref="Track"/>을 즉시 반환한다</para>
        ///</summary>
        ///<param name="skelObject">대상 <see cref="SkelObject"/></param>
        ///<param name="customTrackIndex"><paramref name="skelAni"/>의 <see cref="ISkelAni.TrackIndex"/>와는 다른 별개의 TrackIndex를 실행하고싶을경우, 할당</param>
        public SkelObject.AniCore.Track ExecuteAni_GetTrackAbsolute(SkelObject skelObject, int? customTrackIndex)
        {
            return skelObject.Ani.ExecuteAni_GetTrackAbsolute(this, customTrackIndex);
        }



        ///<summary>
        ///애니메이션 실행 (Execute)
        ///<para>실행 성공 여부와 관계없이 , 실행될 애니메이션이 속한 <see cref="Track"/>을 즉시 반환한다</para>
        ///</summary>
        ///<param name="skelObject">대상 <see cref="SkelObject"/></param>
        public SkelObject.AniCore.Track ExecuteAni_GetTrackAbsolute(SkelObject skelObject)
        {
            return skelObject.Ani.ExecuteAni_GetTrackAbsolute(this);
        }



        //? 애니메이션 실행(무조건 Track반환) + starter



        ///<summary>
        ///애니메이션 실행 (Execute)
        ///<para>실행 성공 여부와 관계없이 , 실행될 애니메이션이 속한 <see cref="Track"/>을 즉시 반환한다</para>
        ///<para>+ 실행에 성공할경우, <paramref name="starter"/>를 <see cref="Track.AnimationStarter"/>에 할당한다</para>
        ///</summary>
        ///<param name="skelObject">대상 <see cref="SkelObject"/></param>
        ///<param name="starter">애니메이션 실행에 성공하면, 이 오브젝트를 등록한다</param>
        ///<param name="customTrackIndex"><paramref name="skelAni"/>의 <see cref="ISkelAni.TrackIndex"/>와는 다른 별개의 TrackIndex를 실행하고싶을경우, 할당</param>
        public SkelObject.AniCore.Track ExecuteAni_GetTrackAbsolute(SkelObject skelObject, object starter, int? customTrackIndex)
        {
            return skelObject.Ani.ExecuteAni_GetTrackAbsolute(starter, this, customTrackIndex);
        }



        ///<summary>
        ///애니메이션 실행 (Execute)
        ///<para>실행 성공 여부와 관계없이 , 실행될 애니메이션이 속한 <see cref="Track"/>을 즉시 반환한다</para>
        ///<para>+ 실행에 성공할경우, <paramref name="starter"/>를 <see cref="Track.AnimationStarter"/>에 할당한다</para>
        ///</summary>
        ///<param name="starter">애니메이션 실행에 성공하면, 이 오브젝트를 등록한다</param>
        ///<param name="skelObject">대상 <see cref="SkelObject"/></param>
        public SkelObject.AniCore.Track ExecuteAni_GetTrackAbsolute(SkelObject skelObject, object starter)
        {
            return skelObject.Ani.ExecuteAni_GetTrackAbsolute(starter, this);
        }



        #endregion



        //? 이 애니메이션 종료 관련



        #region 이 애니메이션 종료 관련



        /// <summary>
        /// 애니메이션을 <b>종료</b>시킨다
        /// <para>이 <see cref="SkelAni"/>의 <see cref="SkelAni.TrackIndex"/>에 해당하는 <see cref="SkelObject.AniCore.Track"/>의 애니메이션이 <see cref="SkelAni.Animation"/>과 같다면, 종료시킨다</para>
        /// <para>종료에 성공하면 true를 반환한다</para>
        /// <para><see cref="SkelAni"/>를 기준으로 비교하기 때문에 이 <see cref="SkelAni"/>가 CustomTrackIndex로 실행되었다면, 인식되지 않는다</para>
        /// </summary>
        public bool EndAni(SkelObject skelObject)
        {
            return skelObject.Ani.EndAni(this);
        }


        /// <summary>
        /// 애니메이션을 <b>종료</b>시킨다
        /// <para>이 <see cref="SkelAni"/>의 <see cref="SkelAni.TrackIndex"/>에 해당하는 <see cref="SkelObject.AniCore.Track"/>의 애니메이션이 <see cref="SkelAni.Animation"/>과 같다면, 종료시킨다</para>
        /// <para>종료에 성공하면 true를 반환한다</para>
        /// <para><see cref="SkelAni"/>를 기준으로 비교하기 때문에 이 <see cref="SkelAni"/>가 CustomTrackIndex로 실행되었다면, 인식되지 않는다</para>
        ///<para><b><paramref name="starter"/>와 <see cref="SkelObject.AniCore.Track.AnimationStarter"/>가 같아야, 종료가 이루어진다</b></para>
        /// </summary>
        public bool EndAni(SkelObject skelObject, object starter)
        {
            return skelObject.Ani.EndAni(starter, this);
        }



        #endregion



        ///======================================================================================================================================================



        //? SkelAni 확장 클래스 / 인터페이스



        #region StartEvent / EndEvent



        /// <summary>
        /// 이 애니메이션이 <b>시작</b> 될때
        /// <para>이벤트 실행이 가능한 인터페이스</para>
        /// </summary>
        public interface IStartEvents
        {
            /// <summary>
            /// 애니메이션이 <b>시작</b> 될때 실행되는 이벤트
            /// </summary>
            Action<SkelObject, SkelObject.AniCore.Track> StartEvent { get; set; }

            /// <summary>
            /// 애니메이션이 <b>시작</b> 될때 실행되는 이벤트 지정하기
            /// </summary>
            public IStartEvents SetStartEvent(Action<SkelObject, SkelObject.AniCore.Track> startEvent)
            {
                StartEvent = startEvent;
                return this;
            }
        }



        /// <summary>
        /// 이 애니메이션이 <b>종료</b> 될때
        /// <para>이벤트 실행이 가능한 인터페이스</para>
        /// </summary>
        public interface IEndEvents
        {
            /// <summary>
            /// 애니메이션이 <b>종료</b> 될때 실행되는 이벤트
            /// </summary>
            Action<SkelObject, SkelObject.AniCore.Track> EndEvent { get; set; }

            /// <summary>
            /// 애니메이션이 <b>종료</b> 될때 실행되는 이벤트 지정하기
            /// </summary>
            public IEndEvents SetEndEvent(Action<SkelObject, SkelObject.AniCore.Track> endEvent)
            {
                EndEvent = endEvent;
                return this;
            }
        }



        /// <summary>
        /// 이 애니메이션이 <b>시작</b> 될때
        /// <para>이벤트 실행이 가능한 애니메이션</para>
        /// </summary>
        public sealed class StartEvents : SkelAni, IStartEvents
        {
            /// <inheritdoc/>
            public Action<SkelObject, SkelObject.AniCore.Track> StartEvent { get; set; }

            public override void ResetValues()
            {
                base.ResetValues();
                StartEvent = null;
            }
        }



        /// <summary>
        /// 이 애니메이션이 <b>종료</b> 될때
        /// <para>이벤트 실행이 가능한 애니메이션</para>
        /// </summary>
        public sealed class EndEvents : SkelAni, IEndEvents
        {
            /// <inheritdoc/>
            public Action<SkelObject, SkelObject.AniCore.Track> EndEvent { get; set; }

            public override void ResetValues()
            {
                base.ResetValues();
                EndEvent = null;
            }
        }



        /// <summary>
        /// 이 애니메이션이 <b>시작 + 종료</b> 될때
        /// <para>이벤트 실행이 가능한 애니메이션</para>
        /// </summary>
        public sealed class StartEndEvents : SkelAni, IStartEvents, IEndEvents
        {
            /// <inheritdoc/>
            public Action<SkelObject, SkelObject.AniCore.Track> StartEvent { get; set; }
            /// <inheritdoc/>
            public Action<SkelObject, SkelObject.AniCore.Track> EndEvent { get; set; }

            public override void ResetValues()
            {
                base.ResetValues();
                StartEvent = null;
                EndEvent = null;
            }
        }



        /// <summary>
        /// 이 애니메이션이 <b>시작</b> 될때
        /// <para>이벤트 실행이 가능한 애니메이션</para>
        /// <para>을 Casting하여 지정하기</para>
        /// </summary>
        public SkelAni Casting_StartEvent(Action<SkelObject, SkelObject.AniCore.Track> startEvent)
        {
            if (this is IStartEvents casting)
            {
                casting.SetStartEvent(startEvent);
            }
            else
            {
                Debug.LogError($"{GetType().Name}은(는) {nameof(IStartEvents)} 인터페이스를 구현하지 않습니다.");
            }

            return this;
        }



        /// <summary>
        /// <b>애니메이션 시작 이벤트</b>
        ///<para>가 존재할경우, 이벤트 실행</para>
        /// </summary>
        /// <param name="skelObject"></param>
        /// <param name="track"></param>
        public static void TryExecute_StartEvent(ISkelAni target, SkelObject skelObject, SkelObject.AniCore.Track track)
        {
            if (target is IStartEvents casting)
            {
                casting.StartEvent?.Invoke(skelObject, track);
            }
        }



        /// <summary>
        /// 이 애니메이션이 <b>종료</b> 될때
        /// <para>이벤트 실행이 가능한 애니메이션</para>
        /// <para>을 Casting하여 지정하기</para>
        /// </summary>
        public SkelAni Casting_EndEvent(Action<SkelObject, SkelObject.AniCore.Track> endEvent)
        {
            if (this is IEndEvents casting)
            {
                casting.SetEndEvent(endEvent);
            }
            else
            {
                Debug.LogError($"{GetType().Name}은(는) {nameof(IEndEvents)} 인터페이스를 구현하지 않습니다.");
            }

            return this;
        }



        /// <summary>
        /// <b>애니메이션 종료 이벤트</b>
        ///<para>가 존재할경우, 이벤트 실행</para>
        /// </summary>
        /// <param name="skelObject"></param>
        /// <param name="track"></param>
        public static void TryExecute_EndEvent(ISkelAni target, SkelObject skelObject, SkelObject.AniCore.Track track)
        {
            if (target is IEndEvents casting)
            {
                casting.EndEvent?.Invoke(skelObject, track);
            }
        }



        /// <summary>
        /// 이 애니메이션이 <b>시작 + 종료</b> 될때
        /// <para>이벤트 실행이 가능한 애니메이션</para>
        /// <para>을 Casting하여 지정하기</para>
        /// </summary>
        public SkelAni Casting_StartEndEvent(Action<SkelObject, SkelObject.AniCore.Track> startEvent, Action<SkelObject, SkelObject.AniCore.Track> endEvent)
        {
            Casting_StartEvent(startEvent);
            Casting_EndEvent(endEvent);
            return this;
        }



        #endregion



        ///======================================================================================================================================================
    }
}