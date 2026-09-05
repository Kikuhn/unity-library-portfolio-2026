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
using static Pan.SpinePackage.SkelObject.AniCore.Players;
using Pan.Util.Game;



namespace Pan.SpinePackage
{

    public partial class SkelObject
    {
        /// <summary>
        ///<see cref="Pan.SpinePackage.SkelObject"/> 의 기능별로 분류된 "코어"의 베이스 클래스
        /// </summary>
        public abstract class CoreBase
        {
            ///======================================================================================================================================================



            protected CoreBase(SkelObject skelObject)
            {
                SkelObject = skelObject;
                CoreEnabled = true;
            }



            ///======================================================================================================================================================



            protected readonly SkelObject SkelObject;

            protected SkeletonAnimation SkelAnimation => SkelObject.m_SkeletonAnimation;

            protected SkelSbject SkelSbject => SkelObject.currentDB;

            protected Skeleton Skeleton => SkelAnimation.Skeleton;



            /// <summary>
            /// 이 코어가 생성자에서 초기화 되었는지 여부
            /// </summary>
            public readonly bool CoreEnabled;



            ///======================================================================================================================================================



            ///<summary>
            ///<see cref="Pan.SpinePackage.SkelSbject"/>에서 실행되는 초기화
            /// </summary>
            protected abstract void WakeUp_BySkelSbject(SkelSbject skelSbject);



            ///<summary>
            ///코어가 <b>활성화</b> 될 때 실행
            /// </summary>
            public abstract void EnableCore();



            ///<summary>
            ///코어가 <b>비활성화</b> 될 때 실행
            /// </summary>
            public abstract void DisableCore();



            ///<summary>
            ///코어가 요소를 <b>갱신</b> 할 필요가 있을때 실행
            /// </summary>
            protected abstract void RefreshCore();



            ///======================================================================================================================================================
        }
    }

}