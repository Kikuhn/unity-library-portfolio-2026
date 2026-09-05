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
using Sirenix.OdinInspector;



namespace Pan.SpinePackage
{
    public partial class SkelObject
    {
        public partial class SkelCore : CoreBase
        {
            /// <summary>
            /// Flip 작동 모드
            /// </summary>
            public enum FlipMode
            {
                /// <summary>
                /// 기본 Flip 모드
                /// <para><see cref="Skeleton.scaleX"/>를 뒤집는 방식으로 적용된다</para>
                /// </summary>
                Default,
                /// <summary>
                /// 수동 Flip 모드
                /// <para>직접 FlipX에 따라, 어떻게 적용할지 결정된다</para>
                /// <para>옵저버에 의해 작동하는것이 정석 (<see cref="SkelObject.Observers.Alarm_FlipX"/>)</para>
                /// <para>아무것도 알람되지 않으면, 아무 일도 일어 나지 않는다</para>
                /// </summary>
                Another
            }



            /// <summary>
            /// Flip 매니저
            /// </summary>
            [Serializable]
            public class FlipManager : Base
            {
                ///======================================================================================================================================================



                public FlipManager(SkelObject skelObject, Exy flipAxis) : base(skelObject)
                {
                    FlipAxis = flipAxis;


                    //. 밸류-컨트롤 매니저인, Fliper 설정하기
                    Fliper = new ValueControlManager<bool>((oldValue, newValue) =>
                    {
                        //! 처음 적용되는 값이 아니고, 이전값과 현재 값이 같다면 return
                        if (oldValue is not null && oldValue == newValue) { return; }

                        switch (FlipAxis)
                        {
                            case Exy.X:

                            //. Skel 옵저버에 FlipX을 알림
                            SkelObject.Observer.Alarm_FlipX.Alarm(SkelObject, newValue);


                            //? Flip 모드가 기본 모드라면, ScaleX를 뒤집는다
                            if (FlipMode == FlipMode.Default)
                            {
                                skelObject.Skel.SkelAnimation.Skeleton.ScaleX = newValue ? -1f : 1f;
                            }

                            break;

                            case Exy.Y:

                            //. Skel 옵저버에 FlipY을 알림
                            SkelObject.Observer.Alarm_FlipY.Alarm(SkelObject, newValue);


                            //? Flip 모드가 기본 모드라면, ScaleY를 뒤집는다
                            if (FlipMode == FlipMode.Default)
                            {
                                skelObject.Skel.SkelAnimation.Skeleton.ScaleY = newValue ? -1f : 1f;
                            }

                            break;
                        }

                    }, false);
                }



                ///======================================================================================================================================================



                /// <summary>
                /// Flip의 축 (고정)
                /// </summary>
                [ShowInInspector][ReadOnly][LabelText("Flip 방향")] public readonly Exy FlipAxis;



                /// <summary>
                /// 내부에서 Flip의 사용되는 밸류-컨트롤 매니저
                /// </summary>
                private readonly ValueControlManager<bool> Fliper;



                ///======================================================================================================================================================



                /// <summary>
                /// 현재 Flip 작동 모드
                /// </summary>
                [ShowInInspector][LabelText("Flip 모드")] public FlipMode FlipMode { get; set; } = FlipMode.Default;



                ///======================================================================================================================================================



                /// <summary>
                /// 현재 Flip 얻기
                /// <para>X- 좌측: true / 우측: false</para>
                /// <para>Y- 하단: true / 상단: false</para>
                /// </summary>
                [ShowInInspector][LabelText("현재 Flip 상태")] public bool Flip => Fliper.GetValue();



                ///======================================================================================================================================================



                ///<summary>
                /// Flip 지정하기
                ///</summary>
                public void SetFlip(int flipRank, bool value, bool overlap = false)
                {
                    Fliper.SetValue(this, flipRank, value, overlap);
                }



                ///<summary>
                /// Flip 제거하기
                ///</summary>
                public void RemoveFlip(int flipRank)
                {
                    Fliper.RemoveValue(this, flipRank);
                }



                ///======================================================================================================================================================



                //? 리셋



                ///<summary>
                ///플립 매니저 리셋
                ///<para> <see cref="FlipMode"/>가 기본값(<see cref="FlipMode.Default"/>)으로 지정</para>
                ///<para>Fliper가 Reset</para>
                ///</summary>
                public void Reset()
                {
                    FlipMode = FlipMode.Default;
                    Fliper.Reset(false);
                }



                ///======================================================================================================================================================
            }
        }
    }
}