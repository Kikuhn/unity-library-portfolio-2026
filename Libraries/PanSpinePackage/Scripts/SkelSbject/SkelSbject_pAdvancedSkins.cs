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
using System.Runtime.CompilerServices;



//? SkelSbject의 클래스 기반 스킨 세트, AdvancedSkin이 정리되어있는 정도의 코드



namespace Pan.SpinePackage
{
    ///======================================================================================================================================================



    public partial class SkelSbject
    {
        ///======================================================================================================================================================



        //? 어드밴스드 스킨 (베이스)



        /// <summary>
        /// <see cref="SkelSbject"/>의 귀속되는 어드밴스드 스킨의 베이스 (클래스로 이루어짐)
        /// </summary>
        public abstract class BaseAdvancedSkin
        {
            /// <summary>
            /// 스킨이 적용/제거 될때 실행되는 이벤트<br/>
            /// 이 인터페이스를 상속받을경우,<br/>
            /// <see cref="MainExtendSkin"/>에 자동으로 <see cref="ExtendSkin.ApplyEvents"/> <see cref="ExtendSkin.RemoveEvents"/> 가 적용된다<br/>
            /// <b>이 인터페이스를 상속받은 <see cref="BaseAdvancedSkin"/>은 별도로 <see cref="ExtendSkin"/>에 이벤트를 추가할필요가 없음!</b>
            /// </summary>
            public interface ISkinEventApplyRemove
            {
                void ApplyAdvancedSkin(SkelObject skelObject);
                void RemoveAdvancedSkinEvent(SkelObject skelObject);
            }



            /// <summary>
            /// 메인 <see cref="ExtendSkin"/><br/>
            /// setter 호출시:이 클래스가 <see cref="ISkinEventApplyRemove"/>를 상속받고있다면<br/>
            /// <see cref="ExtendSkin.ApplyEvents"/>, <see cref="ExtendSkin.RemoveEvents"/> 에<br/>
            /// <see cref="ISkinEventApplyRemove.ApplyAdvancedSkin(SkelObject)"/>와 <see cref="ISkinEventApplyRemove.RemoveAdvancedSkinEvent(SkelObject)"/> 가 등록된다
            /// </summary>
            public ExtendSkin MainExtendSkin
            {
                get => mainExtendSkin;
                protected set
                {
                    //? null을 넣어 기존 메인스킨을 제거하려는 경우, 구독 취소
                    if (mainExtendSkin != null && value == null && this is ISkinEventApplyRemove skinEventApplyRemove1)
                    {
                        MainExtendSkin.ApplyEvents -= skinEventApplyRemove1.ApplyAdvancedSkin;
                        MainExtendSkin.RemoveEvents -= skinEventApplyRemove1.RemoveAdvancedSkinEvent;
                    }

                    mainExtendSkin = value;


                    //? 새로운 값을 넣어 기존 메인스킨을 추가하려는 경우, 구독 시작
                    if (mainExtendSkin != null && this is ISkinEventApplyRemove skinEventApplyRemove2)
                    {
                        MainExtendSkin.ApplyEvents += skinEventApplyRemove2.ApplyAdvancedSkin;
                        MainExtendSkin.RemoveEvents += skinEventApplyRemove2.RemoveAdvancedSkinEvent;
                    }
                }
            }
            private ExtendSkin mainExtendSkin;



            public static implicit operator ExtendSkin(BaseAdvancedSkin value) => value.MainExtendSkin;
        }



        ///<inheritdoc/>
        public abstract class BaseAdvancedSkin<TSkelSbject> : BaseAdvancedSkin, IWakeUp<TSkelSbject> where TSkelSbject : SkelSbject, new()
        {
            /// <summary>
            /// SkelSbject (제네릭)
            /// </summary>
            protected TSkelSbject SkelSbject { get; private set; }



            public void WakeUp(TSkelSbject skelSbject)
            {
                SkelSbject = skelSbject;
                WakeUp_AdvancedSkin(skelSbject);
            }



            protected abstract void WakeUp_AdvancedSkin(TSkelSbject skelSbject);
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 어드밴스드 스킨 매니저 보유 인터페이스
        /// </summary>
        public interface IHoldAdvancedSkinManager
        {
            BaseAdvancedSkin[] GetAdvancedSkinArray { get; }
        }



        ///<inheritdoc/>
        public interface IHoldAdvancedSkinManager<TSkelSbject, TAdvancedSkin> : IHoldAdvancedSkinManager
            where TSkelSbject : SkelSbject, new()
            where TAdvancedSkin : BaseAdvancedSkin, IWakeUp<TSkelSbject>
        {
            TypeInstancesFactoryManager<TAdvancedSkin, TSkelSbject> AdvancedSkins { get; }
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================
}