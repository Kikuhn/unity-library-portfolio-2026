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
using Pan.SpineUtil;
using Sirenix.OdinInspector;
using Sirenix.Serialization;



namespace Pan.SpinePackage
{
    public partial class SkelObject
    {
        /// <summary>
        /// SkelObject의 Skel 코어
        /// </summary>
        [Serializable]
        public partial class SkelCore : CoreBase, SkelCore.IGetBonesRunTime
        {
            ///======================================================================================================================================================



            //? SkelCore 생성 및 초기화 (팩토리 메서드)



            protected SkelCore(SkelObject skelObject) : base(skelObject)
            {
                //. 런타임 스킨 매니저 초기화
                RunTimeSkins = new RunTimeSkinManager(skelObject, skelObject.m_SkeletonAnimation, "SkelObjectHuskSkin");


                //. 플립 매니저 초기화
                FlipsX = new FlipManager(skelObject, Exy.X);
                FlipsY = new FlipManager(skelObject, Exy.Y);
            }



            /// <summary>
            /// <see cref="SkelCore"/> 를 생성한다
            /// </summary>
            /// <param name="skelObject"></param>
            /// <param name="skelSbject"></param>
            public static void CreateSkelCore(SkelObject skelObject, SkelSbject skelSbject)
            {
                skelObject.Skel = new SkelCore(skelObject);
                skelObject.Skel.WakeUp_BySkelSbject(skelSbject);
            }



            protected override void WakeUp_BySkelSbject(SkelSbject skelSbject)
            {
                //. 기본 Spine의 SkeletonAnimation의 정보를 할당한다
                SkelAnimation.skeletonDataAsset = skelSbject.Skeleton_DataAsset;


                //. SkeletonAnimation의 기본 설정값을 지정한다
                if (SkelAnimation.Renderer is SkeletonRenderer skeletonRenderer)
                {
                    skeletonRenderer.clearStateOnDisable = true;
                }
                //SkelAnimation.Skeleton.Data.DefaultSkin = skelSbject.DefaultSkinData;
                //SkelAnimation.Skeleton.SetSkin(skelSbject.DefaultSkinData);
                SkelAnimation.Initialize(true);
                //SkelAnimation.zSpacing = -0.0001f;
                SkelAnimation.Renderer.MeshSettings.zSpacing = 0;


                //. 런타임 본 매니저 초기화
                RunTimeBones = new RunTimeBoneManager(SkelObject, skelSbject);

                RunTimeSlots = new RunTimeSlotManager(SkelObject,
                    skelSbject, 
                    SkelObject.RunTimeTagSlotInitialCapacity,
                    SkelObject.CustomDrawOrderExecutes_InitialMaxBucketLength,
                    SkelObject.CustomDrawOrderExecutes_Indexes_InitialCapacity);


                SetSlotsToSetupPose();
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

                protected SkelCore SkelCore => SkelObject.Skel;
            }



            ///======================================================================================================================================================



            //? 핵심 메서드



            protected override void RefreshCore()
            {
                //. 모든 런타임 스킨 초기화
                RunTimeSkins.ResetAllSkin();


                //. 런타임 본 초기화
                RunTimeBones.Reset();


                //. 런타임 슬롯 초기화
                RunTimeSlots.Reset();


                //. 플립 매니저 초기화
                FlipsX.Reset();
                FlipsY.Reset();
            }



            public override void EnableCore()
            {
                RefreshCore();
            }



            public override void DisableCore()
            {
                RefreshCore();
            }



            ///======================================================================================================================================================



            //? 매니저



            ///<summary>
            ///RunTime-스킨 매니저
            ///</summary>
            [LabelText("런타임 스킨 매니저"), PropertyOrder(1)]
            [HideReferenceObjectPicker]
            [ShowInInspector] public readonly RunTimeSkinManager RunTimeSkins;



            /// <summary>
            /// Runtime-본 매니저
            /// <para><see cref="WakeUp_BySkelSbject(SkelSbject)"/> 에서 초기화된다</para>
            /// </summary>
            [ LabelText("런타임 Bone 매니저"), PropertyOrder(2)]
            [HideReferenceObjectPicker]
            [ShowInInspector] public RunTimeBoneManager RunTimeBones { get; private set; }



            /// <summary>
            /// Runtime-슬롯 매니저
            /// <para><see cref="WakeUp_BySkelSbject(SkelSbject)"/> 에서 초기화된다</para>
            /// </summary>
            [ LabelText("런타임 슬롯 매니저"), PropertyOrder(3)]
            [HideReferenceObjectPicker]
            [ShowInInspector] public RunTimeSlotManager RunTimeSlots { get; private set; }



            /// <summary>
            /// Flip-X 매니저
            /// </summary>
            [ LabelText("Flip X 매니저"), PropertyOrder(4)]
            [HideReferenceObjectPicker]
            [ShowInInspector] public readonly FlipManager FlipsX;

            /// <summary>
            /// Flip-Y 매니저
            /// </summary>
            [ LabelText("Flip Y 매니저"), PropertyOrder(5)]
            [HideReferenceObjectPicker]
            [ShowInInspector] public readonly FlipManager FlipsY;



            ///======================================================================================================================================================



            //? 조회 변수



            /// <summary>
            /// <see cref="SkeletonAnimation"/>을 얻는다
            /// <para><b>디버깅용, 사용 자제 권장</b></para>
            /// </summary>
            public SkeletonAnimation GetSkeletonAnimation() => SkelAnimation;



            /// <summary>
            /// <see cref="Skin"/> 얻기 (<see cref="Skeleton.Skin"/>)
            /// </summary>
            public Skin GetSkin() => Skeleton.Skin;



            ///======================================================================================================================================================



            //? 공용 유틸리티 메서드



            ///<summary>
            ///<see cref="Skeleton"/>의 <see cref="Skeleton.SetupPoseSlots"/> 호출
            ///</summary>
            public void SetSlotsToSetupPose()
            {
                Skeleton.SetupPoseSlots();
            }



            ///======================================================================================================================================================



            //? 인터페이스 구현



            ///<inheritdoc/>
            public RunTimeBone GetRunTimeBone(string boneName) => RunTimeBones.GetRunTimeBone(boneName);




            ///<inheritdoc/>
            public Bone GetBone(string boneName) => RunTimeBones.GetBone(boneName);



            ///======================================================================================================================================================
        }
    }
}