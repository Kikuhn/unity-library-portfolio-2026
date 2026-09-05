using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine;
using Spine.Unity;
using System.Linq;
using System;
using Pan.Util;
using Pan.Util.IOB;
using Pan.Event;
using Pan.SpinePackage;
using static Pan.SpinePackage.SkelObject.AniCore.Players;
using Pan.Util.Game;
using System.Text;
using Cysharp.Threading.Tasks.Triggers;
using Sirenix.OdinInspector;



namespace Pan.SpinePackage
{
    public partial class SkelObject
    {
        /// <summary>
        /// SkelObject의 Ani 코어
        /// </summary>
        [Serializable]
        public partial class AniCore : CoreBase
        {
            ///======================================================================================================================================================



            //? AniCore 생성 및 초기화 (팩토리 메서드)



            protected AniCore(SkelObject skelObject) : base(skelObject)
            {
                InteractionManagers = new();
            }



            /// <summary>
            /// <see cref="AniCore"/> 를 생성한다
            /// </summary>
            /// <param name="skelObject"></param>
            /// <param name="skelSbject"></param>
            public static void CreateAniCore(SkelObject skelObject, SkelSbject skelSbject)
            {
                skelObject.Ani = new AniCore(skelObject);
                skelObject.Ani.WakeUp_BySkelSbject(skelSbject);
            }



            protected override void WakeUp_BySkelSbject(SkelSbject skelSbject)
            {
                Setting = new Settings(SkelObject, skelSbject);
                Player = new Players(SkelObject);

                BindAnimationStateEvents();
            }


            private Spine.AnimationState subscribedAnimationState;



            internal void BindAnimationStateEvents()
            {
                UnbindAnimationStateEvents();

                Spine.AnimationState animationState = SkelAnimation?.AnimationState;
                if (animationState == null) { return; }

                animationState.Event -= HandleSpineEvent;
                animationState.Complete -= HandleAnimationComplete;
                subscribedAnimationState = animationState;
                subscribedAnimationState.Event += HandleSpineEvent;
                subscribedAnimationState.Complete += HandleAnimationComplete;
            }



            internal void UnbindAnimationStateEvents()
            {
                if (subscribedAnimationState == null) { return; }

                subscribedAnimationState.Event -= HandleSpineEvent;
                subscribedAnimationState.Complete -= HandleAnimationComplete;
                subscribedAnimationState = null;
            }



            private void HandleSpineEvent(TrackEntry trackEntry, Spine.Event spineEvent)
            {
                SkelSbject?.SpineEvent(SkelObject, trackEntry, spineEvent);
            }



            private void HandleAnimationComplete(TrackEntry trackEntry)
            {
                CompleteAniEvent(trackEntry, Player.GetTrack(trackEntry.TrackIndex), true);
            }



            ///======================================================================================================================================================



            //? 하위 클래스들의 베이스 클래스



            public abstract class Base
            {
                public Base(SkelObject skelObject)
                {
                    SkelObject = skelObject;
                }

                protected readonly SkelObject SkelObject;

                protected AniCore AniCore => SkelObject.Ani;
            }



            ///======================================================================================================================================================



            //? 핵심 메서드



            ///<inheritdoc/>
            protected override void RefreshCore()
            {
                SkelAnimation.UpdateTiming = UpdateTiming.ManualUpdate;
                //PostUpdateEvent = null;
            }



            ///<inheritdoc/>
            public override void EnableCore()
            {
                Player?.EmptyAnimations(); //. 최초 생성시, 아직 DB 할당 되기 이전에 호출될 수도 있으므로, null체크를 한다
                RefreshCore();
            }



            ///<inheritdoc/>
            public override void DisableCore()
            {
                Player.DisableAllTrack();
                RefreshCore();
            }



            public void Update(float deltaTime, float timeScale)
            {
                //. 애니메이션 플레이어를 먼저 업데이트하여 이번 SkeletonAnimation.Update에 TimeScale이 반영되게 한다
                Player.UpdatePlayer(timeScale);


                //. 스파인 SkeletonAnimation의 업데이트를 실행한다 (수동 Manual)
                SkelAnimation.Update(deltaTime);
            }



            ///======================================================================================================================================================



            //? Static



            ///<summary>
            /// 빈 애니메이션 이름
            /// </summary>
            private const string EMPTY_ANIMATION = "<empty>";



            /// <summary>
            /// 빈(empty) 애니메이션
            /// </summary>
            private static readonly Spine.Animation EmptyAnimation = _emptyAnimation ??= _emptyAnimation = CreateEmptyAnimation();
            private static Spine.Animation _emptyAnimation;

            private static Spine.Animation CreateEmptyAnimation()
            {
                var animation = new Spine.Animation(EMPTY_ANIMATION);
                animation.SetTimelines(new ExposedList<Timeline>(0), new ExposedList<int>(0));
                animation.Duration = 0;
                return animation;
            }



            ///======================================================================================================================================================



            //? 메인 변수



            /// <summary>
            /// 설정
            /// <para><see cref="SkelSbject"/>에서 초기화된다</para>
            /// </summary>
            [LabelText("설정")]
            [ShowInInspector][HideReferenceObjectPicker] public Settings Setting { get; private set; } = null;



            /// <summary>
            /// 플레이어
            ///<para><see cref="SkelSbject"/>에서 초기화된다</para>
            /// </summary>
            [LabelText("플레이어")]
            [ShowInInspector][HideReferenceObjectPicker] public Players Player { get; private set; } = null;



            /// <summary>
            /// 애니메이션 상호관리 총매니저 (Key : string 으로작동)
            /// </summary>
            public InteractionManager<SkelObject> InteractionManagers { get; private set; }



            ///// <summary>
            ///// 애니메이션 <see cref="UpdateAniCore"/>에서 제일 마지막에 실행되는 이벤트
            ///// </summary>
            //public event Action<SkelObject> PostUpdateEvent = null;



            ///======================================================================================================================================================



            //? [private] 내부 요소 접근



            #region 내부 요소 접근


            ///<summary>
            ///trackIndex로 <see cref="TrackEntry"/> 얻기
            /// </summary>
            private TrackEntry GetTrackEntry(int trackIndex) => SkelAnimation.AnimationState.GetTrack(trackIndex);


            #endregion



            ///======================================================================================================================================================



            //? 애니메이션 얻기



            #region 애니메이션 얻기



            /// <summary>해당 트랙의 재생중인 애니메이션 얻기 (Track)</summary>
            /// <param name="trackEnum">해당 트랙</param>
            public Track GetPlayingTrack<TEnum>(TEnum trackEnum) where TEnum : struct, Enum, IComparable, IConvertible, IFormattable
            {
                return Player.GetTrack(Setting.GetTrackIndex(trackEnum));
            }



            /// <summary>해당 트랙의 재생중인 애니메이션 얻기 (TrackIndex)</summary>
            /// <param name="trackIndex">해당 트랙</param>
            public Track GetPlayingTrack(int trackIndex)
            {
                return Player.GetTrack(trackIndex);
            }



            /// <summary>해당 SkelAni의 트랙으로 재생중인 애니메이션 얻기</summary>
            /// 
            public Track GetPlayingTrack(ISkelAni skelAni)
            {
                return Player.GetTrack(skelAni.TrackIndex);
            }



            /// <summary>애니메이션 얻기 시도</summary>
            /// <param name="skelAni">애니메이션</param>
            /// <param name="resultTrack">반환되는 Track</param>
            public bool TryGetPlayingTrack(ISkelAni skelAni, out Track resultTrack)
            {
                return Player.TryGetTrack(skelAni.TrackIndex, out resultTrack);
            }



            /// <summary>애니메이션 얻기 시도</summary>
            /// <param name="trackIndex">해당 트랙</param>
            /// <param name="resultTrack">반환되는 Track</param>
            public bool TryGetPlayingTrack(int trackIndex, out Track resultTrack)
            {
                return Player.TryGetTrack(trackIndex, out resultTrack);
            }



            #endregion



            //? 애니메이션(트랙) 재생 확인



            #region 애니메이션(트랙) 재생 확인



            /// <summary>
            /// 받아온 <paramref name="skelAni"/>가 현재 재생중인지 확인한다
            /// <para>해당 트랙이 활성화 되어있고, 그 트랙의 애니메이션의 이름이 같다면 true</para>
            /// <para><see cref="ISkelAni"/>를 기준으로 비교하기 때문에 <paramref name="skelAni"/>가 CustomTrackIndex로 실행되었다면, 인식되지 않는다</para>
            /// </summary>
            public bool CheckPlayingSkelAni(ISkelAni skelAni)
            {
                //? 해당 트랙이 활성화 되어있고, 애니메이션의 이름이 같다면 성공
                return ((Player.TryGetTrack(skelAni.TrackIndex, out var track) && (track.PlayingSkelAni.Animation == skelAni.Animation)));
            }



            /// <summary>
            /// 받아온 <paramref name="trackIndex"/>의 트랙에 <paramref name="ani"/>가 현재 재생중인지 확인한다
            /// <para>해당 트랙이 활성화 되어있고, 그 트랙의 애니메이션의 이름이 같다면 true</para>
            /// </summary>
            public bool CheckPlayingSpineAnimation(int trackIndex, Spine.Animation ani)
            {
                return Player.IsActiveTrack(trackIndex) && Player.GetSkelAnimation(trackIndex).Animation == ani;
            }



            /// <summary>
            /// <paramref name="trackIndex"/>의 트랙이 존재하고, <b>활성화</b> 되어있는지 확인한다
            /// </summary>
            public bool CheckActiveTrackIndex(int trackIndex)
            {
                return Player.IsActiveTrack(trackIndex);
            }



            #endregion



            ///======================================================================================================================================================

            //. 애니메이션 로직 순서: 애니메이션 실행(Execute) -> 애니메이션 시작(Start) -> 애니메이션 적용(Apply), * 단, 특수는 제외

            ///======================================================================================================================================================



            //? [private] 애니메이션 적용(Apply) 관련



            #region 애니메이션 적용



            /// <summary>
            /// <see cref="ISkelAni"/>로 애니메이션을 적용한다
            /// </summary>
            private void ApplyAnimation(int trackIndex, ISkelAni skelAni)
            {
                ApplyAnimation(trackIndex, skelAni.Animation, skelAni.Loop, skelAni.MixDuration);
            }



            /// <summary>
            /// <see cref="Spine.Animation"/>으로 애니메이션을 적용한다
            /// </summary>
            private void ApplyAnimation(int trackIndex, Spine.Animation animation, bool loop, float mixDuration)
            {
                var trackEntry = SkelAnimation.AnimationState.SetAnimation(trackIndex, animation, loop);
                trackEntry.MixDuration = mixDuration;
            }



            /// <summary>
            /// 애니메이션 이름 문자열 (비권장)으로 애니메이션을 적용한다
            /// </summary>
            private void ApplyAnimation(int trackIndex, string animationName, bool loop, float mixDuration)
            {
                var trackEntry = SkelAnimation.AnimationState.SetAnimation(trackIndex, animationName, loop);
                trackEntry.MixDuration = mixDuration;
            }



            #endregion



            ///======================================================================================================================================================



            //? [private] 애니메이션 시작(Start)  및 종료 관련



            #region 애니메이션 시작



            /// <summary>
            /// 애니메이션을 시작한다
            /// <para>실패할수도 있음</para>
            /// </summary>
            /// <param name="skelAni">재생하려는 <see cref="ISkelAni"/> 애니메이션</param>
            /// <param name="playTrackIndex">반환되는 재생하려고 하는 트랙</param>
            /// <param name="customTrackIndex">null이 아닐경우, <see cref="ISkelAni"/>의 트랙 설정을 무시하고 이 트랙이 적용된다</param>
            /// <returns></returns>
            private bool StartAni(ISkelAni skelAni, out int playTrackIndex, int? customTrackIndex)
            {
                playTrackIndex = customTrackIndex ?? skelAni.TrackIndex;


                //. ===== 애니메이션 실행하기 =====


                //! #1 : 플레이어에 트랙 자체가 없을거나 비활성화 되어있을 경우, 새롭게 활성화 한 뒤 실행한다
                //. 트랙 자체가 없는 경우는 일반적이라면, 모든 트랙에 빈 애니메이션이 재생되기에 이럴일이 없을 것 같지만,
                //. 커스텀 트랙을 사용할 경우에는 어쩔수 없이 새롭게 활성화한뒤 사용한다
                if (Player.IsActiveTrack(playTrackIndex) == false)
                {
                    TrackEntry currentEntry = SkelAnimation.AnimationState.GetTrack(playTrackIndex);
                    float mixDuration = skelAni.MixDuration;

                    //? 같은 프레임에 EndAni 후 새 애니메이션이 시작되면, 남아 있는 Empty Mix보다
                    //? 새 Mix가 먼저 끝나면서 이전 포즈가 한 번에 제거되지 않도록 남은 시간을 이어받는다
                    if (currentEntry?.Animation.Name == EMPTY_ANIMATION && currentEntry.MixingFrom != null)
                    {
                        float remainingEndMix = Mathf.Max(0, currentEntry.MixDuration - currentEntry.MixTime);
                        mixDuration = ResolveReplacementMixDuration(remainingEndMix, mixDuration);
                    }

                    //. 트랙이 없다면 추가하는 기능도 포함되어있음
                    Player.EnableTrack(playTrackIndex, skelAni);

                    ApplyAnimation(
                        playTrackIndex,
                        skelAni.Animation,
                        skelAni.Loop,
                        mixDuration);
                    return true;
                }


                //! #2 : 트랙에 이미 재생중인 애니메이션이 있지만, 재생 하려고 하는 것의 애니메이션의 랭크가 더 높다면 실행한다
                if (Player.GetSkelAnimation(playTrackIndex).RankType < skelAni.RankType)
                {
                    return ReplaceAnimation(playTrackIndex, skelAni);
                }


                //! #3 : 랭크는 양측 동일하지만, 실행하려는 애니메이션이 다르다면 실행한다
                if (Player.GetSkelAnimation(playTrackIndex).Animation != skelAni.Animation &&
                    Player.GetSkelAnimation(playTrackIndex).RankType == skelAni.RankType)
                {
                    return ReplaceAnimation(playTrackIndex, skelAni);
                }


                //! #4 : 애니메이션 이름 까지 동일하고, 받아온 애니메이션이 "중첩"이 가능할경우, 실행한다
                if (Player.GetSkelAnimation(playTrackIndex).Animation == skelAni.Animation && skelAni.Overlap)
                {
                    return ReplaceAnimation(playTrackIndex, skelAni);
                }


                return false;
            }



            /// <summary>
            /// 트랙을 추가한다
            /// <para>일반적일때는 사용할 일이 없고,</para>
            /// <para>특수하게 애니메이션을 특정 트랙에 실행하고싶을때 사용한다</para>
            /// </summary>
            /// <param name="trackIndex"></param>
            /// <returns></returns>
            private bool AddNewTrack(int trackIndex)
            {
                return Player.AddNewTrack(trackIndex);
            }



            /// <summary>
            /// 애니메이션이 <b>종료</b> 될때 실행되는 메서드
            /// <para>기본적으로 <see cref="Spine.AnimationState.Complete"/>에 추가되어</para>
            /// <para>애니메이션의 Loop 여부와 상태와 관계없이, 호출된다</para>
            /// <para>이때, <paramref name="fromAnimationStateComplete"/>가 true인 채로 받게되기 때문에</para>
            /// <para>Loop 애니메이션이 Loop 될 때 마다 실행되는것이 제약되어있다</para>
            /// <para>그 <b>외에 특정 애니메이션의 종료에는 반드시 이 메서드와 함께 호출하여 사용해야 한다</b></para>
            /// </summary>
            /// <param name="trackEntry"></param>
            /// <param name="track"></param>
            /// <param name="fromAnimationStateComplete"><see cref="Spine.AnimationState.Complete"/>에서 호출될경우 true를 사용한다</param>
            private void CompleteAniEvent(
                TrackEntry trackEntry,
                Track track,
                bool fromAnimationStateComplete,
                bool executeEmptyAnimation = true)
            {
                if (trackEntry == null || track == null) { return; }

                //! 해당 애니메이션이 empty 이거나,
                //! 애니메이션이 Loop 이고, 안전하게 종료된 것이라면 return
                if ((trackEntry.Animation.Name == EMPTY_ANIMATION) || (trackEntry.Loop == true && fromAnimationStateComplete == true)) { return; }


                //. 실제 적용된 TrackIndex를 가져온다
                int trackIndex = trackEntry.TrackIndex;
                float endMixDuration = track.PlayingSkelAni.EndMixDuration;


                //? 먼저 SkelAni가 EndEvent가 존재한다면, 실행한다
                SkelAni.TryExecute_EndEvent(track.PlayingSkelAni_Reference, SkelObject, track);

                //? 트랙의 EndEvent를 실행한다
                Player.GetTrack(trackIndex).ExecuteEndEvent(SkelObject, track);

                //? 옵저버에 애니메이션의 종료를 알린다
                SkelObject.Observer.Alarm_AniEnd.Alarm(track, trackEntry, fromAnimationStateComplete);

                //? 플레이어를 비활성화한다
                Player.DisableTrack(trackIndex);

                if (!executeEmptyAnimation) { return; }


                //? 애니메이션을 종료하는 로직이니까 empty 애니메이션을 실행한다

                //. 빈 애니메이션을 실행함에 있어, 특수한 MixDuration을 사용할 경우,
                //. 그 MixDuration으로 empty 애니메이션을 실행한다
                if (Setting.TryGet_EmptyAnimationMixduration(trackIndex, out var endMixDuration1))
                {
                    ExecuteEmptyAnimation(trackIndex, endMixDuration1);
                }
                //. 일반적인 경우는 기본 MixDuration으로 empty 애니메이션을 실행한다
                else
                {
                    ExecuteEmptyAnimation(trackIndex, endMixDuration);
                }
            }



            private bool ReplaceAnimation(int trackIndex, ISkelAni skelAni)
            {
                Track previousTrack = Player.GetTrack(trackIndex);
                float endMixDuration = previousTrack.PlayingSkelAni.EndMixDuration;
                if (Setting.TryGet_EmptyAnimationMixduration(trackIndex, out var customEndMixDuration))
                {
                    endMixDuration = customEndMixDuration;
                }

                float mixDuration = ResolveReplacementMixDuration(
                    endMixDuration,
                    skelAni.MixDuration);

                CompleteAniEvent(
                    SkelAnimation.AnimationState.GetTrack(trackIndex),
                    previousTrack,
                    false,
                    false);
                Player.EnableTrack(trackIndex, skelAni);
                ApplyAnimation(
                    trackIndex,
                    skelAni.Animation,
                    skelAni.Loop,
                    mixDuration);
                return true;
            }



            internal static float ResolveReplacementMixDuration(
                float endMixDuration,
                float startMixDuration)
            {
                return Mathf.Max(endMixDuration, startMixDuration);
            }



            #endregion



            ///======================================================================================================================================================



            //? 애니메이션 실행(Execute) 관련



            #region 애니메이션 실행



            //? 애니메이션 실행



            ///<summary>
            ///애니메이션 실행 (Execute)
            ///</summary>
            ///<param name="skelAni">실행할 애니메이션</param>
            ///<param name="customTrackIndex"><paramref name="skelAni"/>의 <see cref="ISkelAni.TrackIndex"/>와는 다른 별개의 TrackIndex를 실행하고싶을경우, 할당</param>
            public bool ExecuteAni(ISkelAni skelAni, int? customTrackIndex)
            {
                var aniStart = StartAni(skelAni, out int playingTrackIndex, customTrackIndex);

                if (aniStart == true)
                {
                    Track playingTrack = Player.GetTrack(playingTrackIndex);

                    SkelObject.Observer.Alarm_AniStart.Alarm(SkelObject, playingTrack);
                    SkelAni.TryExecute_StartEvent(skelAni, SkelObject, playingTrack);
                }

                return aniStart;
            }

            ///<summary>
            ///애니메이션 실행 (Execute)
            ///</summary>
            ///<param name="skelAni">실행할 애니메이션</param>
            public bool ExecuteAni(ISkelAni skelAni) => ExecuteAni(skelAni, null);



            //? 애니메이션 실행 + starter



            ///<summary>
            ///애니메이션 실행 (Execute)
            ///<para>+ 실행에 성공할경우, <paramref name="starter"/>를 <see cref="Track.AnimationStarter"/>에 할당한다</para>
            ///</summary>
            ///<param name="starter">애니메이션 실행에 성공하면, 이 오브젝트를 등록한다</param>
            ///<param name="skelAni">실행할 애니메이션</param>
            ///<param name="customTrackIndex"><paramref name="skelAni"/>의 <see cref="ISkelAni.TrackIndex"/>와는 다른 별개의 TrackIndex를 실행하고싶을경우, 할당</param>
            public bool ExecuteAni(object starter, ISkelAni skelAni, int? customTrackIndex)
            {
                if (ExecuteAni(skelAni, customTrackIndex))
                {
                    GetPlayingTrack(skelAni).AnimationStarter = starter;
                    return true;
                }

                return false;
            }

            ///<summary>
            ///애니메이션 실행 (Execute)
            ///<para>+ 실행에 성공할경우, <paramref name="starter"/>를 <see cref="Track.AnimationStarter"/>에 할당한다</para>
            ///</summary>
            ///<param name="starter">애니메이션 실행에 성공하면, 이 오브젝트를 등록한다</param>
            ///<param name="skelAni">실행할 애니메이션</param>
            public bool ExecuteAni(object starter, ISkelAni skelAni) => ExecuteAni(starter, skelAni, null);



            //? 애니메이션 실행(Track반환)



            ///<summary>
            ///애니메이션 실행 (Execute)
            ///<para>실행에 성공했을 경우, 실행된 애니메이션이 속한 <see cref="Track"/>을 즉시 반환한다</para>
            ///</summary>
            ///<param name="skelAni">실행할 애니메이션</param>
            ///<param name="customTrackIndex"><paramref name="skelAni"/>의 <see cref="ISkelAni.TrackIndex"/>와는 다른 별개의 TrackIndex를 실행하고싶을경우, 할당</param>
            public Track ExecuteAni_GetTrack(ISkelAni skelAni, int? customTrackIndex)
            {
                return ExecuteAni(skelAni, customTrackIndex) ? GetPlayingTrack(customTrackIndex ?? skelAni.TrackIndex) : null;
            }

            ///<summary>
            ///애니메이션 실행 (Execute)
            ///<para>실행에 성공했을 경우, 실행된 애니메이션이 속한 <see cref="Track"/>을 즉시 반환한다</para>
            ///</summary>
            ///<param name="skelAni">실행할 애니메이션</param>
            public Track ExecuteAni_GetTrack(ISkelAni skelAni) => ExecuteAni_GetTrack(skelAni, null);



            //? 애니메이션 실행(Track반환) + starter



            ///<summary>
            ///애니메이션 실행 (Execute)
            ///<para>실행에 성공했을 경우, 실행된 애니메이션이 속한 <see cref="Track"/>을 즉시 반환한다</para>
            ///<para>+ 실행에 성공할경우, <paramref name="starter"/>를 <see cref="Track.AnimationStarter"/>에 할당한다</para>
            ///</summary>
            ///<param name="starter">애니메이션 실행에 성공하면, 이 오브젝트를 등록한다</param>
            ///<param name="skelAni">실행할 애니메이션</param>
            ///<param name="customTrackIndex"><paramref name="skelAni"/>의 <see cref="ISkelAni.TrackIndex"/>와는 다른 별개의 TrackIndex를 실행하고싶을경우, 할당</param>
            public Track ExecuteAni_GetTrack(object starter, ISkelAni skelAni, int? customTrackIndex = null)
            {
                if (ExecuteAni(starter, skelAni, customTrackIndex))
                {
                    var result = GetPlayingTrack(customTrackIndex ?? skelAni.TrackIndex);
                    return result;
                }

                return null;
            }

            ///<summary>
            ///애니메이션 실행 (Execute)
            ///<para>실행에 성공했을 경우, 실행된 애니메이션이 속한 <see cref="Track"/>을 즉시 반환한다</para>
            ///<para>+ 실행에 성공할경우, <paramref name="starter"/>를 <see cref="Track.AnimationStarter"/>에 할당한다</para>
            ///</summary>
            ///<param name="starter">애니메이션 실행에 성공하면, 이 오브젝트를 등록한다</param>
            ///<param name="skelAni">실행할 애니메이션</param>
            public Track ExecuteAni_GetTrack(object starter, ISkelAni skelAni)
            {
                return ExecuteAni_GetTrack(starter, skelAni, null);
            }



            //? 애니메이션 실행(무조건 Track반환)



            ///<summary>
            ///애니메이션 실행 (Execute)
            ///<para>실행 성공 여부와 관계없이 , 실행될 애니메이션이 속한 <see cref="Track"/>을 즉시 반환한다</para>
            ///</summary>
            ///<param name="skelAni">실행할 애니메이션</param>
            ///<param name="customTrackIndex"><paramref name="skelAni"/>의 <see cref="ISkelAni.TrackIndex"/>와는 다른 별개의 TrackIndex를 실행하고싶을경우, 할당</param>
            public Track ExecuteAni_GetTrackAbsolute(ISkelAni skelAni, int? customTrackIndex = null)
            {
                ExecuteAni(skelAni, customTrackIndex);
                return GetPlayingTrack(customTrackIndex ?? skelAni.TrackIndex);
            }

            ///<summary>
            ///애니메이션 실행 (Execute)
            ///<para>실행 성공 여부와 관계없이 , 실행될 애니메이션이 속한 <see cref="Track"/>을 즉시 반환한다</para>
            ///</summary>
            ///<param name="skelAni">실행할 애니메이션</param>
            public Track ExecuteAni_GetTrackAbsolute(ISkelAni skelAni)
            {
                return ExecuteAni_GetTrackAbsolute(skelAni, null);
            }



            //? 애니메이션 실행(무조건 Track반환) + starter



            ///<summary>
            ///애니메이션 실행 (Execute)
            ///<para>실행 성공 여부와 관계없이 , 실행될 애니메이션이 속한 <see cref="Track"/>을 즉시 반환한다</para>
            ///<para>+ 실행에 성공할경우, <paramref name="starter"/>를 <see cref="Track.AnimationStarter"/>에 할당한다</para>
            ///</summary>
            ///<param name="starter">애니메이션 실행에 성공하면, 이 오브젝트를 등록한다</param>
            ///<param name="skelAni">실행할 애니메이션</param>
            ///<param name="customTrackIndex"><paramref name="skelAni"/>의 <see cref="ISkelAni.TrackIndex"/>와는 다른 별개의 TrackIndex를 실행하고싶을경우, 할당</param>
            public Track ExecuteAni_GetTrackAbsolute(object starter, ISkelAni skelAni, int? customTrackIndex = null)
            {
                if (ExecuteAni(skelAni, customTrackIndex))
                {
                    var result = GetPlayingTrack(customTrackIndex ?? skelAni.TrackIndex);
                    result.AnimationStarter = starter;
                    return result;
                }
                return GetPlayingTrack(customTrackIndex ?? skelAni.TrackIndex);
            }

            ///<summary>
            ///애니메이션 실행 (Execute)
            ///<para>실행 성공 여부와 관계없이 , 실행될 애니메이션이 속한 <see cref="Track"/>을 즉시 반환한다</para>
            ///<para>+ 실행에 성공할경우, <paramref name="starter"/>를 <see cref="Track.AnimationStarter"/>에 할당한다</para>
            ///</summary>
            ///<param name="starter">애니메이션 실행에 성공하면, 이 오브젝트를 등록한다</param>
            ///<param name="skelAni">실행할 애니메이션</param>
            public Track ExecuteAni_GetTrackAbsolute(object starter, ISkelAni skelAni)
            {
                return ExecuteAni_GetTrackAbsolute(starter, skelAni, null);
            }



            #endregion



            ///======================================================================================================================================================



            //? 애니메이션 특수 실행(Execute)



            #region 애니메이션 특수 실행(Execute)



            ///<summary>
            ///특수 애니메이션 실행
            ///<para>직접 <see cref="Spine.Animation"/>과 파라미터들을 받아와, 애니메이션을 실행한다</para>
            ///</summary>
            public void ExecuteCustomAnimation(int trackIndex, Spine.Animation ani, bool loop, float mixDuration)
            {
                //AddNewTrack(trackIndex);
                ApplyAnimation(trackIndex, ani, loop, mixDuration);
            }



            ///<summary>
            ///특수 애니메이션 실행
            ///<para>직접 애니메이션 이름과 파라미터들을 받아와, 애니메이션을 실행한다</para>
            ///</summary>
            public void ExecuteCustomAnimation(int trackIndex, string aniName, bool loop, float mixDuration)
            {
                //AddNewTrack(trackIndex);
                ApplyAnimation(trackIndex, aniName, loop, mixDuration);
            }



            ///<summary>
            ///empty 애니메이션(<see cref="EmptyAnimation"/>) 실행
            ///</summary>
            public void ExecuteEmptyAnimation(int trackIndex, float mixDuration)
            {
                ApplyAnimation(trackIndex, EmptyAnimation, false, mixDuration);
            }



            #endregion



            ///======================================================================================================================================================



            //? 애니메이션 종료



            #region 애니메이션 종료



            //? TrackIndex로 종료



            /// <summary>
            /// 애니메이션을 <b>종료</b>시킨다
            /// <para><paramref name="trackIndex"/>에 해당하는 <see cref="Track"/>의 애니메이션을 종료시킨다</para>
            /// <para>종료에 성공하면 true를 반환한다</para>
            /// </summary>
            public bool EndAni(int trackIndex)
            {
                //? 해당 트랙이 재생중이라면 종료
                if (Player.TryGetTrack(trackIndex, out var track))
                {
                    CompleteAniEvent(GetTrackEntry(trackIndex), track, false);
                    return true;
                }

                return false;
            }



            /// <summary>
            /// 애니메이션을 <b>종료</b>시킨다
            /// <para><paramref name="trackIndex"/>에 해당하는 <see cref="Track"/>의 애니메이션을 종료시킨다</para>
            /// <para>종료에 성공하면 true를 반환한다</para>
            ///<para><b><paramref name="starter"/>와 <see cref="Track.AnimationStarter"/>가 같아야, 종료가 이루어진다</b></para>
            /// </summary>
            public bool EndAni(object starter, int trackIndex)
            {
                //? 해당 트랙이 재생중이고, starter가 같다면 종료
                if (Player.TryGetTrack(trackIndex, out var track) && track.AnimationStarter == starter)
                {
                    CompleteAniEvent(GetTrackEntry(trackIndex), Player.GetTrack(trackIndex), false);
                    return true;
                }

                return false;
            }



            //? ISkelAni로 종료



            /// <summary>
            /// 애니메이션을 <b>종료</b>시킨다
            /// <para><paramref name="skelAni"/>의 <see cref="ISkelAni.TrackIndex"/>에 해당하는 <see cref="Track"/>의 애니메이션이 <see cref="ISkelAni.Animation"/>과 같다면, 종료시킨다</para>
            /// <para>종료에 성공하면 true를 반환한다</para>
            /// <para><see cref="ISkelAni"/>를 기준으로 비교하기 때문에 <paramref name="skelAni"/>가 CustomTrackIndex로 실행되었다면, 인식되지 않는다</para>
            /// </summary>
            public bool EndAni(ISkelAni skelAni)
            {
                //? 해당 트랙이 재생중이고, 받아온 애니메이션과 같다면 종료
                if (Player.TryGetTrack(skelAni.TrackIndex, out var track) && track.PlayingSkelAni.Animation == skelAni.Animation)
                {
                    CompleteAniEvent(GetTrackEntry(skelAni.TrackIndex), track, false);
                    return true;
                }

                return false;
            }



            /// <summary>
            /// 애니메이션을 <b>종료</b>시킨다
            /// <para><paramref name="skelAni"/>의 <see cref="ISkelAni.TrackIndex"/>에 해당하는 <see cref="Track"/>의 애니메이션이 <see cref="ISkelAni.Animation"/>과 같다면, 종료시킨다</para>
            /// <para>종료에 성공하면 true를 반환한다</para>
            /// <para><see cref="ISkelAni"/>를 기준으로 비교하기 때문에 <paramref name="skelAni"/>가 CustomTrackIndex로 실행되었다면, 인식되지 않는다</para>
            ///<para><b><paramref name="starter"/>와 <see cref="Track.AnimationStarter"/>가 같아야, 종료가 이루어진다</b></para>
            /// </summary>
            public bool EndAni(object starter, ISkelAni skelAni)
            {
                //? 해당 트랙이 재생중이고, 받아온 애니메이션과 같고,  starter가 같다면 종료
                if (Player.TryGetTrack(skelAni.TrackIndex, out var track) && track.PlayingSkelAni.Animation == skelAni.Animation && track.AnimationStarter == starter)
                {
                    CompleteAniEvent(GetTrackEntry(skelAni.TrackIndex), track, false);
                    return true;
                }

                return false;
            }



            /// <summary>
            /// 애니메이션을 <b>종료</b>시킨다
            /// <para>(CustomTrackIndex로 실행되었을때를 가정하여, <see cref="ISkelAni.TrackIndex"/>의 TrackIndex가 아닌 받아온 <paramref name="trackIndex"/>을 사용한다)</para>
            /// <para><paramref name="trackIndex"/>에 해당하는 <see cref="Track"/>의 애니메이션이 <see cref="ISkelAni.Animation"/>과 같다면, 종료시킨다</para>
            /// <para>종료에 성공하면 true를 반환한다</para>
            /// </summary>
            public bool EndAni(int trackIndex, ISkelAni skelAni)
            {
                //? 해당 트랙(별도의 trackIndex)이 재생중이고, 받아온 애니메이션과 같다면 종료
                if (Player.TryGetTrack(trackIndex, out var track) && track.PlayingSkelAni.Animation == skelAni.Animation)
                {
                    CompleteAniEvent(GetTrackEntry(trackIndex), track, false);
                    return true;
                }

                return false;
            }



            /// <summary>
            /// 애니메이션을 <b>종료</b>시킨다
            /// <para>(CustomTrackIndex로 실행되었을때를 가정하여, <see cref="ISkelAni.TrackIndex"/>의 TrackIndex가 아닌 받아온 <paramref name="trackIndex"/>을 사용한다)</para>
            /// <para><paramref name="trackIndex"/>에 해당하는 <see cref="Track"/>의 애니메이션이 <see cref="ISkelAni.Animation"/>과 같다면, 종료시킨다</para>
            /// <para>종료에 성공하면 true를 반환한다</para>
            ///<para><b><paramref name="starter"/>와 <see cref="Track.AnimationStarter"/>가 같아야, 종료가 이루어진다</b></para>
            /// </summary>
            public bool EndAni(object starter, int trackIndex, ISkelAni skelAni)
            {
                //? 해당 트랙(별도의 trackIndex)이 재생중이고, 받아온 애니메이션과 같고,  starter가 같다면 종료
                if (Player.TryGetTrack(trackIndex, out var track) && track.PlayingSkelAni.Animation == skelAni.Animation && track.AnimationStarter == starter)
                {
                    CompleteAniEvent(GetTrackEntry(trackIndex), track, false);
                    return true;
                }

                return false;
            }



            //? 특수 종료



            /// <summary>
            /// 애니메이션을 <b>종료</b>시킨다
            /// <para><paramref name="trackIndex"/>에 해당하는 <see cref="Track"/>의 <see cref="Spine.Animation"/>가 재생중이라면, 종료시킨다</para>
            /// <para>종료에 성공하면 true를 반환한다</para>
            /// </summary>
            public bool EndAni(int trackIndex, Spine.Animation spineAnimation)
            {
                //? 해당 트랙이 재생중이고, 받아온 애니메이션 이름과 같으면 종료
                if (Player.TryGetTrack(trackIndex, out var track) && track.PlayingSkelAni.Animation == spineAnimation)
                {
                    CompleteAniEvent(GetTrackEntry(trackIndex), track, false);
                    return true;
                }

                return false;
            }



            /// <summary>
            /// 애니메이션을 <b>종료</b>시킨다
            /// <para><paramref name="trackIndex"/>에 해당하는 <see cref="Track"/>의 <see cref="Spine.Animation"/>가 재생중이라면, 종료시킨다</para>
            /// <para>종료에 성공하면 true를 반환한다</para>
            ///<para><b><paramref name="starter"/>와 <see cref="Track.AnimationStarter"/>가 같아야, 종료가 이루어진다</b></para>
            /// </summary>
            public bool EndAni(object starter, int trackIndex, Spine.Animation spineAnimation)
            {
                //? 해당 트랙이 재생중이고, 받아온 애니메이션 이름과 같고, starter가 같다면 종료
                if (Player.TryGetTrack(trackIndex, out var track) && track.PlayingSkelAni.Animation == spineAnimation && track.AnimationStarter == starter)
                {
                    CompleteAniEvent(GetTrackEntry(trackIndex), track, false);
                    return true;
                }

                return false;
            }



            /// <summary>
            /// 애니메이션을 <b>종료</b>시킨다
            /// <para><paramref name="trackIndex"/>에 해당하는 <see cref="Track"/>의 <paramref name="spineAnimationName"/> 이름의 애니메이션이 재생중이라면, 종료시킨다</para>
            /// <para>종료에 성공하면 true를 반환한다</para>
            /// </summary>
            public bool EndAni(int trackIndex, string spineAnimationName)
            {
                //? 해당 트랙이 재생중이고, 받아온 애니메이션 이름과 같으면 종료
                if (Player.TryGetTrack(trackIndex, out var track) && track.PlayingSkelAni.Animation.Name == spineAnimationName)
                {
                    CompleteAniEvent(GetTrackEntry(trackIndex), track, false);
                    return true;
                }

                return false;
            }



            /// <summary>
            /// 애니메이션을 <b>종료</b>시킨다
            /// <para><paramref name="trackIndex"/>에 해당하는 <see cref="Track"/>의 <paramref name="spineAnimationName"/> 이름의 애니메이션이 재생중이라면, 종료시킨다</para>
            /// <para>종료에 성공하면 true를 반환한다</para>
            ///<para><b><paramref name="starter"/>와 <see cref="Track.AnimationStarter"/>가 같아야, 종료가 이루어진다</b></para>
            /// </summary>
            public bool EndAni(object starter, int trackIndex, string spineAnimationName)
            {
                //? 해당 트랙이 재생중이고, 받아온 애니메이션 이름과 같고, starter가 같다면 종료
                if (Player.TryGetTrack(trackIndex, out var track) && track.PlayingSkelAni.Animation.Name == spineAnimationName && track.AnimationStarter == starter)
                {
                    CompleteAniEvent(GetTrackEntry(trackIndex), track, false);
                    return true;
                }

                return false;
            }



            #endregion



            ///======================================================================================================================================================



            //? 확장 메서드



            /// <summary>
            /// 받아온 <paramref name="playingTrack"/> 애니메이션이 재생되는동안,
            /// <para><see cref="PostUpdateEvent"/>에 할당되어, Update 될때마다</para>
            /// <para><paramref name="updateEvent"/>이 실행되게끔 한다</para>
            /// </summary>
            public void AddPostUpdate_DuringPlayingAnimation(Track playingTrack, Action<SkelObject> updateEvent)
            {
                SkelObject.PostUpdateEvent += updateEvent;
                playingTrack.EndEvent += (skelObject, track) =>
                {
                    skelObject.PostUpdateEvent -= updateEvent;
                };
            }



            ///======================================================================================================================================================
        }
    }
}
