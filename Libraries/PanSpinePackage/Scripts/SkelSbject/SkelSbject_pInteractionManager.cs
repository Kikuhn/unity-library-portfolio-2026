using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Spine;
using Spine.Unity;
using System.Text;
using Pan.Util;
using System.Linq;
using Pan.SpinePackage;
using static Pan.SpinePackage.SkelObject.AniCore.Players;
using Pan.SpineUtil;



namespace Pan.SpinePackage
{



    public abstract partial class SkelSbject
    {
        /// <summary>
        /// 애니메이션 상호관리매니저의 베이스
        /// <para>논-제네릭 버전</para>
        /// </summary>
        public abstract class AnimationInteractionManagerBase
        {

        }



        /// <summary>
        /// 애니메이션 상호관리매니저의 베이스
        /// <para>Animation Name을 수집하여 HashSet으로 저장한다</para>
        /// </summary>
        public abstract class AnimationInteractionManagerBase<TSkelSbject> : AnimationInteractionManagerBase where TSkelSbject : SkelSbject, new()
        {
            ///======================================================================================================================================================



            public AnimationInteractionManagerBase(TSkelSbject skelSbject)
            {
                SkelSbject = skelSbject;
            }



            ///======================================================================================================================================================



            protected readonly TSkelSbject SkelSbject;



            ///<summary>
            /// 이 상호작용 매니저에 해당하는 스파인 애니메이션의 이름들
            /// </summary>
            private readonly HashSet<string> SpineAnimationNames = new();



            /// <summary>
            /// 이 상호작용 매니저에 더이상 애니메이션을 추가할 필요 없다면 활성화된다
            /// </summary>
            public bool IsLock { get; private set; }



            ///<summary>
            /// 이 상호작용 매니저가 활성화 되어있는지 확인
            /// <para>애니메이션이 1개 이상 보유중이여야 함</para>
            /// </summary>
            public bool IsEnable => SpineAnimationNames.Count > 0;



            /// <summary>
            /// 이 상호작용 매니저가 적용되어야하는 애니메이션이
            /// <para>포함 하고 있어야 하는 스파인 이벤트 이름</para>
            /// </summary>
            public abstract string SpineEventName { get; }



            ///======================================================================================================================================================



            //? 애니메이션 지정 설정



            ///<summary>
            ///이 상호작용 매니저에 애니메이션 추가를 시도한다
            ///<para><paramref name="spineAnimation"/> 애니메이션에 <see cref="SpineEventName"/> 이벤트가 존재한다면, 추가된다</para>
            /// </summary>
            public bool TrySettingSpineAnimation(Spine.Animation spineAnimation)
            {
                //. 잠겨있다면, 실패한다
                if (IsLock) { return false; }

                if (spineAnimation.HasEventFromAnimation(SpineEventName))
                {
                    SpineAnimationNames.Add(spineAnimation.Name);
                    return true;
                }
                return false;
            }



            ///<summary>
            ///이 상호작용 매니저에 추가할 애니메이션들을 모두 추가했다면
            ///<para>이 메서드를 실행하여 더 추가 되지 않게 잠근다</para>
            /// </summary>
            public void LockSettingSpineAnimation()
            {
                if (IsLock) { return; }

                IsLock = true;
                SpineAnimationNames.TrimExcess();
            }



            ///======================================================================================================================================================



            //? 애니메이션 확인



            ///<summary>
            /// 애니메이션 이름이 이 상호작용 매니저에 존재하는지 확인
            /// </summary>
            protected bool ContainsAnimation(string animationName) => SpineAnimationNames.Contains(animationName);



            ///<summary>
            /// 애니메이션 이름이 이 상호작용 매니저에 존재하는지 확인
            /// <para><see cref="SkelObject.AniCore.Track"/>으로 확인</para>
            /// </summary>
            protected bool ContainsAnimation(SkelObject.AniCore.Track track) => SpineAnimationNames.Contains(track.PlayingSkelAni.Animation.Name);



            ///======================================================================================================================================================



            //? 상호작용 실행/종료 +제거



            ///<summary>
            /// 이 상호작용이 <b>Execute(실행)</b> 될때 실행되는 메서드
            /// <para>직접 사용하면 절대 안되며, 상호작용에 의해 실행되어야한다</para>
            /// </summary>
            protected abstract void ExecuteEvent(SkelObject skelObject);



            ///<summary>
            /// 이 상호작용이 <b>Quit(종료)</b> 될때 실행되는 메서드
            /// <para>직접 사용하면 절대 안되며, 상호작용에 의해 실행되어야한다</para>
            /// </summary>
            protected abstract void QuitEvent(SkelObject skelObject);



            ///<summary>
            /// 이 상호작용 매니저에 의해 추가한 "노말 트랙"을 제거하고, "QuitEvent"를 호출하는 메서드
            /// </summary>
            protected void Remove_NormalTrack_TryQuitEvent(SkelObject skelObject, SkelObject.AniCore.Track track)
            {
                skelObject.Ani.InteractionManagers.GetManager(SpineEventName).Remove_NormalTrack_TryQuitEvent(track.PlayingSkelAni.TrackIndex, skelObject);
            }



            ///<summary>
            /// 이 상호작용 매니저에 의해 추가한 "방해 트랙"을 제거하고, "ExecuteEvent"를 호출하는 메서드
            /// </summary>
            protected void Remove_HindranceTrack_TryExecuteEvent(SkelObject skelObject, SkelObject.AniCore.Track track)
            {
                skelObject.Ani.InteractionManagers.GetManager(SpineEventName).Remove_HindranceTrack_TryExecuteEVent(track.PlayingSkelAni.TrackIndex, skelObject);
            }



            ///======================================================================================================================================================



            //? 초기화



            /// <summary>
            /// <paramref name="skelObject"/>에 이 상호작용 매니저를 등록한다
            /// </summary>
            /// <param name="skelObject"></param>
            public void WakeUp(SkelObject skelObject)
            {
                //. skelObject의 애니메이션 상호관리 매니저에,
                //. "SpineEventName" 이름으로 매니저를 추가한뒤
                //. 그 상호관리 매니저가 Execute/Quit 될때의 이벤트를 지정한다
                skelObject.Ani.InteractionManagers.AddManger(SpineEventName).SettingEvents(ExecuteEvent, QuitEvent);
            }



            ///======================================================================================================================================================



            //? 애니메이션 시작 알람


            ///<summary>
            ///<b>애니메이션 시작</b>을 알람 받았을때,
            ///<para>이 조건 까지 충족해야 <see cref="AlarmThe_AniStartCurrent(SkelObject, InteractionManager{SkelObject}.Interaction, SkelObject.AniCore.Track)"/> 가 호출된다</para>
            ///<para>null일경우, 무조건 true를 반환하는 것으로 간주한다</para>
            ///<para>override 하지 않으면, 기본 값은 null을 반환한다</para>
            /// </summary>
            protected virtual Func<SkelObject, SkelObject.AniCore.Track, bool> Condition_AlarmThe_AniStart { get => null; }



            /// <summary>
            /// 애니메이션이 시작 될때 알람
            /// </summary>
            /// <param name="skelObject"></param>
            /// <param name="track"></param>
            public void AlarmThe_AniStart(SkelObject skelObject, SkelObject.AniCore.Track track)
            {
                //? 이 상호작용 매니저가 활성화가 되어있고,
                //? 조건이 비어있거나 (무조건), 비어있지 않지만 조건을 충족했을경우, 애니메이션 시작 알람 메서드를 호출한다
                if (IsEnable && (Condition_AlarmThe_AniStart == null || Condition_AlarmThe_AniStart.Invoke(skelObject, track)))
                {
                    //. 실행된 애니메이션 -> 의 SkelObject -> 의 상호작용 매니저 -> 의 이 객체를 담당한 "상호작용"을 가져온다
                    //! 없다면 매니저를 SkelObject에 추가하는 부분에서 코드를 잘못짠것
                    AlarmThe_AniStartCurrent(skelObject, skelObject.Ani.InteractionManagers.GetManager(SpineEventName), track);
                }
            }



            /// <summary>
            /// 조건에 맞는 애니메이션이 시작 될때의 알람
            /// </summary>
            /// <param name="skelObject"></param>
            /// <param name="interaction">대상 상호작용</param>
            /// <param name="track"></param>
            protected virtual void AlarmThe_AniStartCurrent(SkelObject skelObject, InteractionManager<SkelObject>.Interaction interaction, SkelObject.AniCore.Track track)
            {
                //? 실행된 애니메이션이, 이 상호작용 매니저에 존재 한다면...
                if (ContainsAnimation(track))
                {
                    //? 실행된 애니메이션의 TrackIndex를 "노말 트랙"으로, "상호작용" 에 추가한다
                    //. 추가 한 뒤, Execute 조건에 맞는다면, "ExecuteEvent" 가 실행된다
                    interaction.Add_NormalTrack_TryExecuteEvent(track.PlayingSkelAni.TrackIndex, skelObject);

                    //. "ExecuteEvent" 실행 여부와 관계없이, 그 애니메이션이 종료될때 "상호작용"에 TrackIndex를 "노말 트랙" 으로 제거한다, "QuitEvent"가 실행된다
                    track.EndEvent += Remove_NormalTrack_TryQuitEvent;

                    return;
                }

                //? 실행된 애니메이션이, 이 상호작용 매니저에 존재 하지 않는다면...
                else
                {
                    //? 실행된 애니메이션의 TrackIndex를 "방해 트랙"으로 "상호작용"에 추가한다
                    //. 추가 한 뒤, Quit 조건에 맞는다면, "QuitEvent" 가 실행된다
                    interaction.Add_HindranceTrack_TryQuitEvent(track.PlayingSkelAni.TrackIndex, skelObject);

                    //. "QuitEvent" 실행 여부와 관계없이, 그 애니메이션이 종료될때 "상호작용"에 TrackIndex를 "방해 트랙" 으로 제거한다, "ExecuteEvent"가 실행된다
                    track.EndEvent += Remove_HindranceTrack_TryExecuteEvent;
                }
            }



            ///======================================================================================================================================================
        }

    }
}