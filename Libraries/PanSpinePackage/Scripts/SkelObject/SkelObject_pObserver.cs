using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine;
using Spine.Unity;
using System.Linq;
using SitraUtils;
using System;
using Pan.Util;
using Pan.Util.IOB;
using Pan.SpinePackage;
using Sirenix.OdinInspector;
using Sirenix.Serialization;


namespace Pan.SpinePackage
{
    public partial class SkelObject
    {

        /// <summary>
        /// <see cref="SkelObject"/> 의 전용 옵저버
        /// </summary>
        [Serializable]
        public class Observers : Observer_Main<Observers.IS>
        {
            ///======================================================================================================================================================



            public Observers()
            {
                Alarm_AniStart = new Subscribe_Alarm<IS, IS.IAniStart, SkelObject, AniCore.Track>(this, static (skelObject, track, ob) =>
                {
                    ob.AlarmThe_AniStart(skelObject, track);
                });

                Alarm_AniEnd = new Subscribe_Alarm<IS, IS.IAniEnd, AniCore.Track, TrackEntry, bool>(this, static (track, trackEntry, fromAnimationStateComplete, ob) =>
                {
                    ob.AlarmThe_AniEnd(track, trackEntry, fromAnimationStateComplete);
                });

                Alarm_FlipX = new Subscribe_Alarm<IS, IS.IFlipX, SkelObject, bool>(this, static (skelObject, flipX, ob) =>
                {
                    ob.AlarmThe_FlipX(skelObject, flipX);
                });

                Alarm_FlipY = new Subscribe_Alarm<IS, IS.IFlipY, SkelObject, bool>(this, static (skelObject, flipY, ob) =>
                {
                    ob.AlarmThe_FlipY(skelObject, flipY);
                });
            }



            public interface IS : IObserver
            {
                /// <summary>
                /// 애니메이션이 <b>시작(Start)</b> 될 때, 알람
                /// </summary>
                public interface IAniStart : IS
                {
                    /// <summary>
                    /// 애니메이션이 <b>시작(Start)</b> 될 때, 알람
                    /// </summary>
                    void AlarmThe_AniStart(SkelObject skelObject, AniCore.Track track);
                }



                /// <summary>
                /// 애니메이션이 <b>종료</b> 될 때, 알람
                /// </summary>
                public interface IAniEnd : IS
                {
                    /// <summary>
                    /// 애니메이션이 <b>종료</b> 될 때, 알람
                    /// </summary>
                    /// <param name="fromAnimationStateComplete"><see cref="Spine.AnimationState.Complete"/>에서 호출될경우 true를 사용한다</param>
                    void AlarmThe_AniEnd(AniCore.Track track, TrackEntry trackEntry, bool fromAnimationStateComplete);
                }



                /// <summary>
                /// 플립X (FlipX) 될 때, 알람
                /// </summary>
                public interface IFlipX : IS
                {
                    /// <summary>
                    /// 플립X (FlipX) 될 때, 알람
                    /// </summary>
                    /// <param name="skelObject"></param>
                    /// <param name="flipX"></param>
                    void AlarmThe_FlipX(SkelObject skelObject, bool flipX);
                }



                /// <summary>
                /// 플립Y (FlipY) 될 때, 알람
                /// </summary>
                public interface IFlipY : IS
                {
                    /// <summary>
                    /// 플립X (FlipX) 될 때, 알람
                    /// </summary>
                    /// <param name="skelObject"></param>
                    /// <param name="flipY"></param>
                    void AlarmThe_FlipY(SkelObject skelObject, bool flipY);
                }
            }



            ///======================================================================================================================================================



            /// <summary>
            /// 애니메이션이 <b>시작(Start)</b> 했을때, 알람
            /// </summary>
            [ShowInInspector, ReadOnly] public readonly Subscribe_Alarm<IS, IS.IAniStart, SkelObject, AniCore.Track> Alarm_AniStart;



            /// <summary>
            /// 애니메이션이 <b>종료</b> 될 때, 알람
            /// </summary>
            [ShowInInspector, ReadOnly] public readonly Subscribe_Alarm<IS, IS.IAniEnd, AniCore.Track, TrackEntry, bool> Alarm_AniEnd;



            /// <summary>
            /// 플립X (FlipX) 될 때, 알람
            /// </summary>
            [ShowInInspector, ReadOnly] public readonly Subscribe_Alarm<IS, IS.IFlipX, SkelObject, bool> Alarm_FlipX;



            /// <summary>
            /// 플립Y (FlipY) 될 때, 알람
            /// </summary>
            [ShowInInspector, ReadOnly] public readonly Subscribe_Alarm<IS, IS.IFlipY, SkelObject, bool> Alarm_FlipY;



            ///======================================================================================================================================================
        }
    }
}