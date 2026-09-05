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
using Spine.Unity.AnimationTools;
using JetBrains.Annotations;
using static Pan.SpinePackage.SkelSbject;
using static Pan.SpinePackage.SkelObject.AniCore.Players;
using Pan.Event;
using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using Pan.SpineUtil;



//! SkelObject에 사용되는 스파인의 스켈레톤 정보들을 저장, 관리하는 스크립터블 오브젝트



namespace Pan.SpinePackage
{
    [Serializable]
    public abstract partial class SkelSbject :
        CustomScriptableObject,
        ISkeletonDataAsset,
        ICreateCount,  //. DB 사용을 위한 인터페이스
        ICanGetSkin, ICanGetAnimation, ICanGetSlotData, ICanGetBoneData, ICanGetBoneDataEX, ICanGetTransformConstraintData, ICanGetTransformConstraintDataEX, //. Get류 인터페이스
        IAnimationMixDuration, //. 기본적으로 애니메이션 MixDuration 값 구현 (인스펙터에서 보고 수정)
        IHoldEnumIndex_SkinLayer, IHoldEnumIndex_AniTrack, IHoldEnumIndex_AniRank, IHoldEnumIndex_AniTime //. 기본적으로 애니메이션 트랙, 애니메이션 랭크, 애니타임, 스킨레이어 Enum 구현
    {
        ///======================================================================================================================================================



        //? 인터페이스


        public interface ISkeletonDataAsset
        {
            SkeletonDataAsset Skeleton_DataAsset { get; set; }
        }



        /// <summary>
        /// <see cref="SkelSbject"/> 또는 <see cref="SkelSbject.BaseBox"/>들에게 사용되는
        /// <para>보유 정보 관리 인터페이스 의 베이스의 베이스</para>
        /// </summary>
        public interface IHoldSomethingBase
        {

        }



        ///<summary>
        /// <see cref="SkelAni"/> 애니메이션의 보유 정보를 저장하는 인터페이스의 베이스
        /// </summary>
        public interface IHoldAniBase : IHoldSomethingBase { }



        ///<summary>
        /// 뼈의 보유 정보를 저장하는 인터페이스의 베이스
        /// <para><see cref="SkelObject.SkelCore.IGetBonesRunTime"/> 을 활용해서 지정 추천</para>
        /// </summary>
        public interface IHoldBoneBase : IHoldSomethingBase
        {
            public Bone GetBone(SkelObject.SkelCore.IGetBonesRunTime getBonesRunTime, string boneName) => getBonesRunTime.GetBone(boneName);

            public SkelObject.SkelCore.RunTimeBone GetRunTimeBone(SkelObject.SkelCore.IGetBonesRunTime getBonesRunTime, string boneName) => getBonesRunTime.GetRunTimeBone(boneName);
        }



        ///======================================================================================================================================================



        //? 인터페이스 변수



        #region 인터페이스 변수



        //? 생성 개수


        /// <summary>
        /// 이 SkelSbject를 기반으로 생성되는 오브젝트의 생성개수 (풀링)
        /// </summary>
        public int CreateCount { get => createCount; set => createCount = value; }
        [SerializeField] private int createCount;



        #endregion



        ///======================================================================================================================================================



        //? 핵심 SkeletonDataAsset



        ///<summary>
        /// <see cref="SkeletonDataAsset"/> 얻기
        /// </summary>
        public SkeletonDataAsset Skeleton_DataAsset => skeleton_DataAsset;
        [SerializeField] private SkeletonDataAsset skeleton_DataAsset;


        SkeletonDataAsset ISkeletonDataAsset.Skeleton_DataAsset
        {
            get => skeleton_DataAsset;
            set
            {
                skeleton_DataAsset = value;
                WakeUps();
            }
        }



        /// <summary>
        /// 이 설정의 <see cref="SkeletonData"/><br/>
        /// <i>(WakeUp에서 초기화)</i>
        /// </summary>
        public SkeletonData Skeleton_Data { get; private set; }



        ///======================================================================================================================================================



        //? Data 캐싱 (WakeUp에서 초기화)



        /// <summary>
        /// 이 설정의 Skin 데이터 딕셔너리<br/>
        /// <i>(WakeUp에서 초기화)</i>
        /// </summary>
        public ImmutableDataManager<string, Skin> Skin_Dictioinary { get; private set; }

        /// <summary>
        /// 이 설정의 Animation 데이터 딕셔너리<br/>
        /// <i>(WakeUp에서 초기화)</i>
        /// </summary>
        public ImmutableDataManager<string, AnimationEX> AnimationEX_Dictionary { get; private set; }

        /// <summary>
        /// 이 설정의 Bone 데이터 딕셔너리<br/>
        /// <i>(WakeUp에서 초기화)</i>
        /// </summary>
        public ImmutableDataManager2<string, int, BoneDataEX> BoneDataEX_Dictionary { get; private set; }

        /// <summary>
        /// 이 설정의 Slot 데이터 딕셔너리<br/>
        /// <i>(WakeUp에서 초기화)</i>
        /// </summary>
        public ImmutableDataManager2<string, int, SlotData> Slot_Dictionary { get; private set; }

        /// <summary>
        /// 이 설정의 TransformConstraint 데이터 딕셔너리<br/>
        /// <i>(WakeUp에서 초기화)</i>
        /// </summary>
        public ImmutableDataManager2<string, int, TransformConstraintDataEX> TFConsDataEX_Dictionary { get; private set; }

        /// <summary>
        /// 이 설정의 IkConstraintData 데이터 딕셔너리<br/>
        /// <i>(WakeUp에서 초기화)</i>
        /// </summary>
        public ImmutableDataManager2<string, int, IkConstraintDataEX> IKConsDataEX_Dictionary { get; private set; }

        /// <summary>
        /// 이 설정의 PathConstraintData 데이터 딕셔너리<br/>
        /// <i>(WakeUp에서 초기화)</i>
        /// </summary>
        public ImmutableDataManager2<string, int, PathConstraintDataEX> PathConsDataEX_Dictionary { get; private set; }

        /// <summary>
        /// 이 설정의 PhysicsConstraintData 데이터 딕셔너리<br/>
        /// <i>(WakeUp에서 초기화)</i>
        /// </summary>
        public ImmutableDataManager2<string, int, PhysicsConstraintDataEX> PhysicsConsDataEX_Dictionary { get; private set; }

        /// <summary>
        /// 이 설정의 Spine.Event 데이터 딕셔너리<br/>
        /// <i>(WakeUp에서 초기화)</i>
        /// </summary>
        public ImmutableDataManager<string, Spine.EventData> SpineEvent_Dictionary { get; private set; }

        /// <summary>
        /// 이 설정의 슬롯 인덱스 드로우 오더 배열 (int 배열)
        /// </summary>
        public int[] SlotIndexesDrawOrder_Array { get; private set; }



        ///======================================================================================================================================================



        //? Data 얻기



        #region Data 얻기 메서드



        //? Skin 



        ///<summary>
        ///<see cref="Skin"/> 얻기
        /// </summary>
        public Skin GetSkin(string skinName)
        {
            try
            {
                return Skin_Dictioinary[skinName];
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>Skin</b> 얻기 실패 <color=cyan>{skinName}</color>");
                return null;
            }
        }

        ///<summary>
        ///<see cref="Skin"/> 얻어보기
        /// </summary>
        public bool TryGetSkin(string skinName, out Skin resultSkin)
        {
            if (Skin_Dictioinary.TryGetValue(skinName, out resultSkin))
            {
                return true;
            }

            resultSkin = null;
            return false;
        }



        ///<summary>
        ///<see cref="Skin"/> 얻지만, Skin을 생성하여 그 Skin에 복사(Deep)하여 반환한다
        /// </summary>
        public Skin GetSkinCreateCopied(string skinName)
        {
            var targetSkin = GetSkin(skinName);
            Skin skin = new Skin(skinName);
            skin.CopySkin(targetSkin);
            return skin;
        }

        ///<summary>
        ///<see cref="Skin"/> 얻어보지만, Skin을 생성하여 그 Skin에 복사(Deep)하여 반환한다
        /// </summary>
        public bool TryGetSkinCreateCopied(string skinName, out Skin resultSkin)
        {
            var skin = GetSkinCreateCopied(skinName);
            if (skin != null)
            {
                resultSkin = skin;
                return true;
            }

            resultSkin = null;
            return false;
        }

        public Skin GetRequiredSkinCreateCopied(string skinName)
        {
            var targetSkin = GetRequiredSkin(skinName);
            var skin = new Skin(skinName);
            skin.CopySkin(targetSkin);
            return skin;
        }



        //? Animation



        ///<summary>
        ///<see cref="Spine.Animation"/> 얻기
        /// </summary>
        public Spine.Animation GetAnimation(string animationName)
        {
            try
            {
                return AnimationEX_Dictionary[animationName];
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>Animation</b> 얻기 실패 <color=cyan>{animationName}</color>");
                return null;
            }
        }

        ///<summary>
        ///<see cref="Spine.Animation"/> 얻어보기
        /// </summary>
        public bool TryGetAnimation(string animationName, out Spine.Animation resultAnimation)
        {
            if (AnimationEX_Dictionary.TryGetValue(animationName, out var animationEX))
            {
                resultAnimation = animationEX;
                return true;
            }

            resultAnimation = null;
            return false;
        }



        ///<summary>
        ///<see cref="AnimationEX"/> 얻기
        /// </summary>
        public AnimationEX? GetAnimationEX(string animationName)
        {
            try
            {
                return AnimationEX_Dictionary[animationName];
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>AnimationEX</b> 얻기 실패 <color=cyan>{animationName}</color>");
                return null;
            }
        }

        ///<summary>
        ///<see cref="AnimationEX"/> 얻어보기
        /// </summary>
        public bool TryGetAnimationEX(string animationName, out AnimationEX resultAnimationEX)
        {
            if (AnimationEX_Dictionary.TryGetValue(animationName, out resultAnimationEX))
            {
                return true;
            }

            return false;
        }



        //? Slot



        ///<summary>
        ///<see cref="SlotData"/>을 얻기
        /// </summary>
        public SlotData GetSlotData(string slotName)
        {
            try
            {
                return Slot_Dictionary[slotName];
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>SlotData</b> 얻기 실패 <color=cyan>{slotName}</color>");
                return null;
            }
        }

        ///<summary>
        ///<see cref="SlotData"/>을 얻어보기
        /// </summary>
        public bool TryGetSlotData(string slotName, out SlotData resultSlotData)
        {
            if (Slot_Dictionary.TryGetValue1(slotName, out resultSlotData))
            {
                return true;
            }

            resultSlotData = null;
            return false;
        }



        ///<summary>
        ///<see cref="SlotData"/>을 얻기 (<paramref name="slotIndex"/>)
        /// </summary>
        public SlotData GetSlotData(int slotIndex)
        {
            try
            {
                return Slot_Dictionary.GetValue2(slotIndex);
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>SlotData</b> 얻기 실패 <color=cyan>{slotIndex}</color>");
                return null;
            }
        }

        ///<summary>
        ///<see cref="SlotData"/>을 얻어보기 (<paramref name="slotIndex"/>)
        /// </summary>
        public bool TryGetSlotData(int slotIndex, out SlotData resultSlotData)
        {
            return Slot_Dictionary.TryGetValue2(slotIndex, out resultSlotData);
        }



        //? BoneData



        /// <summary>
        /// <see cref="BoneData"/> 얻기
        /// </summary>
        public BoneData GetBoneData(string boneName)
        {
            try
            {
                return BoneDataEX_Dictionary[boneName];
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>BoneData</b> 얻기 실패 <color=cyan>{boneName}</color>");
                return null;
            }
        }

        ///<summary>
        ///<see cref="BoneData"/>을 얻어보기
        /// </summary>
        public bool TryGetBoneData(string boneName, out BoneData resultBoneData)
        {
            if (TryGetBoneDataEX(boneName, out var result))
            {
                resultBoneData = result.Value.BoneData;
                return true;
            }
            resultBoneData = null;
            return false;
        }



        /// <summary>
        /// <see cref="BoneData"/> 얻기 (<paramref name="boneIndex"/>)
        /// </summary>
        public BoneData GetBoneData(int boneIndex)
        {
            try
            {
                return BoneDataEX_Dictionary[boneIndex];
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>BoneData</b> 얻기 실패 <color=cyan>{boneIndex}</color>");
                return null;
            }
        }

        ///<summary>
        ///<see cref="BoneData"/>을 얻어보기 (<paramref name="boneIndex"/>)
        /// </summary>
        public bool TryGetBoneData(int boneIndex, out BoneData resultBoneData)
        {
            if (TryGetBoneDataEX(boneIndex, out var result))
            {
                resultBoneData = result.Value.BoneData;
                return true;
            }
            resultBoneData = null;
            return false;
        }



        /// <summary>
        /// <see cref="BoneDataEX"/> 얻기
        /// </summary>
        public BoneDataEX? GetBoneDataEX(string boneName)
        {
            try
            {
                return BoneDataEX_Dictionary[boneName];
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>BoneDataEX</b> 얻기 실패 <color=cyan>{boneName}</color>");
                return null;
            }
        }

        /// <summary>
        /// <see cref="BoneDataEX"/> 얻어보기
        /// </summary>
        /// 
        public bool TryGetBoneDataEX(string boneName, out BoneDataEX? resultBoneDataEX)
        {
            if (BoneDataEX_Dictionary.TryGetValue1(boneName, out var boneDataEX))
            {
                resultBoneDataEX = boneDataEX;
                return true;
            }

            resultBoneDataEX = null;
            return false;
        }



        /// <summary>
        /// <see cref="BoneDataEX"/> 얻기 (<paramref name="boneIndex"/>)
        /// </summary>
        public BoneDataEX? GetBoneDataEX(int boneIndex)
        {
            try
            {
                return BoneDataEX_Dictionary[boneIndex];
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>BoneDataEX</b> 얻기 실패 <color=cyan>{boneIndex}</color>");
                return null;
            }
        }

        /// <summary>
        /// <see cref="BoneDataEX"/> 얻어보기 (<paramref name="boneIndex"/>)
        /// </summary>
        /// 
        public bool TryGetBoneDataEX(int boneIndex, out BoneDataEX? resultBoneDataEX)
        {
            if (BoneDataEX_Dictionary.TryGetValue2(boneIndex, out var boneDataEX))
            {
                resultBoneDataEX = boneDataEX;
                return true;
            }

            resultBoneDataEX = null;
            return false;
        }



        //? Transform Constraint



        /// <summary>
        /// <see cref="TransformConstraintData"/> 얻기
        /// </summary>
        public TransformConstraintData GetTransformConstraintData(string transformConstraintDataName)
        {
            try
            {
                return TFConsDataEX_Dictionary[transformConstraintDataName];
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>TransformConstraintData</b> 얻기 실패 <color=cyan>{transformConstraintDataName}</color>");
                return null;
            }
        }

        /// <summary>
        /// <see cref="TransformConstraintData"/> 얻기
        /// </summary>
        public bool TryGetTransformConstraintData(string transformConstraintDataName, out TransformConstraintData resultTransformConstraintData)
        {
            if (TFConsDataEX_Dictionary.TryGetValue1(transformConstraintDataName, out var transformConstraintDataEX))
            {
                resultTransformConstraintData = transformConstraintDataEX.TransformConstraintData;
                return true;
            }
            resultTransformConstraintData = null;
            return false;
        }



        /// <summary>
        /// <see cref="TransformConstraintData"/> 얻기 (<paramref name="transformConstraintDataIndex"/>)
        /// </summary>
        public TransformConstraintData GetTransformConstraintData(int transformConstraintDataIndex)
        {
            try
            {
                return TFConsDataEX_Dictionary[transformConstraintDataIndex];
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>TransformConstraintData</b> 얻기 실패 <color=cyan>{transformConstraintDataIndex}</color>");
                return null;
            }
        }

        /// <summary>
        /// <see cref="TransformConstraintData"/> 얻어보기 (<paramref name="transformConstraintDataIndex"/>)
        /// </summary>
        public bool TryGetTransformConstraintData(int transformConstraintDataIndex, out TransformConstraintData resultTransformConstraintData)
        {
            if (TFConsDataEX_Dictionary.TryGetValue2(transformConstraintDataIndex, out var transformConstraintDataEX))
            {
                resultTransformConstraintData = transformConstraintDataEX.TransformConstraintData;
                return true;
            }
            resultTransformConstraintData = null;
            return false;
        }



        /// <summary>
        /// <see cref="TransformConstraintDataEX"/> 얻기
        /// </summary>
        public TransformConstraintDataEX? GetTransformConstraintDataEX(string transformConstraintDataName)
        {
            try
            {
                return TFConsDataEX_Dictionary[transformConstraintDataName];
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>TransformConstraintDataEX</b> 얻기 실패 <color=cyan>{transformConstraintDataName}</color>");
                return null;
            }
        }

        /// <summary>
        /// <see cref="TransformConstraintDataEX"/> 얻어보기
        /// </summary>
        public bool TryGetTransformConstraintDataEX(string transformConstraintDataName, out TransformConstraintDataEX resultTransformConstraintDataEX)
        {
            return TFConsDataEX_Dictionary.TryGetValue1(transformConstraintDataName, out resultTransformConstraintDataEX);
        }



        /// <summary>
        /// <see cref="TransformConstraintDataEX"/> 얻기 (<paramref name="transformConstraintDataIndex"/>)
        /// </summary>
        public TransformConstraintDataEX? GetTransformConstraintDataEX(int transformConstraintDataIndex)
        {
            try
            {
                return TFConsDataEX_Dictionary[transformConstraintDataIndex];
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>TransformConstraintDataEX</b> 얻기 실패 <color=cyan>{transformConstraintDataIndex}</color>");
                return null;
            }
        }

        /// <summary>
        /// <see cref="TransformConstraintDataEX"/> 얻어보기 (<paramref name="transformConstraintDataIndex"/>)
        /// </summary>
        public bool TryGetTransformConstraintDataEX(int transformConstraintDataIndex, out TransformConstraintDataEX resultTransformConstraintDataEX)
        {
            return TFConsDataEX_Dictionary.TryGetValue2(transformConstraintDataIndex, out resultTransformConstraintDataEX);
        }



        //? IK Constraint



        /// <summary>
        /// <see cref="IkConstraintData"/> 얻기
        /// </summary>
        public IkConstraintData GetIkConstraintData(string ikConstraintDataName)
        {
            try
            {
                return IKConsDataEX_Dictionary[ikConstraintDataName];
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>IkConstraintData</b> 얻기 실패 <color=cyan>{ikConstraintDataName}</color>");
                return null;
            }
        }

        /// <summary>
        /// <see cref="IkConstraintData"/> 얻기
        /// </summary>
        public bool TryGetIkConstraintData(string ikConstraintDataName, out IkConstraintData resultIkConstraintData)
        {
            if (IKConsDataEX_Dictionary.TryGetValue1(ikConstraintDataName, out var ikConstraintDataEX))
            {
                resultIkConstraintData = ikConstraintDataEX.IkConstraintData;
                return true;
            }
            resultIkConstraintData = null;
            return false;
        }



        /// <summary>
        /// <see cref="IkConstraintData"/> 얻기 (<paramref name="ikConstraintDataIndex"/>)
        /// </summary>
        public IkConstraintData GetIkConstraintData(int ikConstraintDataIndex)
        {
            try
            {
                return IKConsDataEX_Dictionary[ikConstraintDataIndex];
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>IkConstraintData</b> 얻기 실패 <color=cyan>{ikConstraintDataIndex}</color>");
                return null;
            }
        }

        /// <summary>
        /// <see cref="IkConstraintData"/> 얻어보기 (<paramref name="ikConstraintDataIndex"/>)
        /// </summary>
        public bool TryGetIkConstraintData(int ikConstraintDataIndex, out IkConstraintData resultIkConstraintData)
        {
            if (IKConsDataEX_Dictionary.TryGetValue2(ikConstraintDataIndex, out var ikConstraintDataEX))
            {
                resultIkConstraintData = ikConstraintDataEX.IkConstraintData;
                return true;
            }
            resultIkConstraintData = null;
            return false;
        }



        /// <summary>
        /// <see cref="IkConstraintDataEX"/> 얻기
        /// </summary>
        public IkConstraintDataEX? GetIkConstraintDataEX(string ikConstraintDataName)
        {
            try
            {
                return IKConsDataEX_Dictionary[ikConstraintDataName];
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>IkConstraintDataEX</b> 얻기 실패 <color=cyan>{ikConstraintDataName}</color>");
                return null;
            }
        }

        /// <summary>
        /// <see cref="IkConstraintDataEX"/> 얻어보기
        /// </summary>
        public bool TryGetIkConstraintDataEX(string ikConstraintDataName, out IkConstraintDataEX resultIkConstraintDataEX)
        {
            return IKConsDataEX_Dictionary.TryGetValue1(ikConstraintDataName, out resultIkConstraintDataEX);
        }



        /// <summary>
        /// <see cref="IkConstraintDataEX"/> 얻기 (<paramref name="ikConstraintDataIndex"/>)
        /// </summary>
        public IkConstraintDataEX? GetIkConstraintDataEX(int ikConstraintDataIndex)
        {
            try
            {
                return IKConsDataEX_Dictionary[ikConstraintDataIndex];
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>IkConstraintDataEX</b> 얻기 실패 <color=cyan>{ikConstraintDataIndex}</color>");
                return null;
            }
        }

        /// <summary>
        /// <see cref="IkConstraintDataEX"/> 얻어보기 (<paramref name="ikConstraintDataIndex"/>)
        /// </summary>
        public bool TryGetIkConstraintDataEX(int ikConstraintDataIndex, out IkConstraintDataEX resultIkConstraintDataEX)
        {
            return IKConsDataEX_Dictionary.TryGetValue2(ikConstraintDataIndex, out resultIkConstraintDataEX);
        }



        //? Path Constraint



        /// <summary>
        /// <see cref="PathConstraintData"/> 얻기
        /// </summary>
        public PathConstraintData GetPathConstraintData(string pathConstraintDataName)
        {
            try
            {
                return PathConsDataEX_Dictionary[pathConstraintDataName];
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>PathConstraintData</b> 얻기 실패 <color=cyan>{pathConstraintDataName}</color>");
                return null;
            }
        }

        /// <summary>
        /// <see cref="PathConstraintData"/> 얻기
        /// </summary>
        public bool TryGetPathConstraintData(string pathConstraintDataName, out PathConstraintData resultPathConstraintData)
        {
            if (PathConsDataEX_Dictionary.TryGetValue1(pathConstraintDataName, out var pathConstraintDataEX))
            {
                resultPathConstraintData = pathConstraintDataEX.PathConstraintData;
                return true;
            }
            resultPathConstraintData = null;
            return false;
        }



        /// <summary>
        /// <see cref="PathConstraintData"/> 얻기 (<paramref name="pathConstraintDataIndex"/>)
        /// </summary>
        public PathConstraintData GetPathConstraintData(int pathConstraintDataIndex)
        {
            try
            {
                return PathConsDataEX_Dictionary[pathConstraintDataIndex];
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>PathConstraintData</b> 얻기 실패 <color=cyan>{pathConstraintDataIndex}</color>");
                return null;
            }
        }

        /// <summary>
        /// <see cref="PathConstraintData"/> 얻어보기 (<paramref name="pathConstraintDataIndex"/>)
        /// </summary>
        public bool TryGetPathConstraintData(int pathConstraintDataIndex, out PathConstraintData resultPathConstraintData)
        {
            if (PathConsDataEX_Dictionary.TryGetValue2(pathConstraintDataIndex, out var pathConstraintDataEX))
            {
                resultPathConstraintData = pathConstraintDataEX.PathConstraintData;
                return true;
            }
            resultPathConstraintData = null;
            return false;
        }



        /// <summary>
        /// <see cref="PathConstraintDataEX"/> 얻기
        /// </summary>
        public PathConstraintDataEX? GetPathConstraintDataEX(string pathConstraintDataName)
        {
            try
            {
                return PathConsDataEX_Dictionary[pathConstraintDataName];
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>PathConstraintDataEX</b> 얻기 실패 <color=cyan>{pathConstraintDataName}</color>");
                return null;
            }
        }

        /// <summary>
        /// <see cref="PathConstraintDataEX"/> 얻어보기
        /// </summary>
        public bool TryGetPathConstraintDataEX(string pathConstraintDataName, out PathConstraintDataEX resultPathConstraintDataEX)
        {
            return PathConsDataEX_Dictionary.TryGetValue1(pathConstraintDataName, out resultPathConstraintDataEX);
        }



        /// <summary>
        /// <see cref="PathConstraintDataEX"/> 얻기 (<paramref name="pathConstraintDataIndex"/>)
        /// </summary>
        public PathConstraintDataEX? GetPathConstraintDataEX(int pathConstraintDataIndex)
        {
            try
            {
                return PathConsDataEX_Dictionary[pathConstraintDataIndex];
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>PathConstraintDataEX</b> 얻기 실패 <color=cyan>{pathConstraintDataIndex}</color>");
                return null;
            }
        }

        /// <summary>
        /// <see cref="PathConstraintDataEX"/> 얻어보기 (<paramref name="pathConstraintDataIndex"/>)
        /// </summary>
        public bool TryGetPathConstraintDataEX(int pathConstraintDataIndex, out PathConstraintDataEX resultPathConstraintDataEX)
        {
            return PathConsDataEX_Dictionary.TryGetValue2(pathConstraintDataIndex, out resultPathConstraintDataEX);
        }



        //? Physics Constraint



        /// <summary>
        /// <see cref="PhysicsConstraintData"/> 얻기
        /// </summary>
        public PhysicsConstraintData GetPhysicsConstraintData(string physicsConstraintDataName)
        {
            try
            {
                return PhysicsConsDataEX_Dictionary[physicsConstraintDataName];
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>PhysicsConstraintData</b> 얻기 실패 <color=cyan>{physicsConstraintDataName}</color>");
                return null;
            }
        }

        /// <summary>
        /// <see cref="PhysicsConstraintData"/> 얻기
        /// </summary>
        public bool TryGetPhysicsConstraintData(string physicsConstraintDataName, out PhysicsConstraintData resultPhysicsConstraintData)
        {
            if (PhysicsConsDataEX_Dictionary.TryGetValue1(physicsConstraintDataName, out var physicsConstraintDataEX))
            {
                resultPhysicsConstraintData = physicsConstraintDataEX.PhysicsConstraintData;
                return true;
            }
            resultPhysicsConstraintData = null;
            return false;
        }



        /// <summary>
        /// <see cref="PhysicsConstraintData"/> 얻기 (<paramref name="physicsConstraintDataIndex"/>)
        /// </summary>
        public PhysicsConstraintData GetPhysicsConstraintData(int physicsConstraintDataIndex)
        {
            try
            {
                return PhysicsConsDataEX_Dictionary[physicsConstraintDataIndex];
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>PhysicsConstraintData</b> 얻기 실패 <color=cyan>{physicsConstraintDataIndex}</color>");
                return null;
            }
        }

        /// <summary>
        /// <see cref="PhysicsConstraintData"/> 얻어보기 (<paramref name="physicsConstraintDataIndex"/>)
        /// </summary>
        public bool TryGetPhysicsConstraintData(int physicsConstraintDataIndex, out PhysicsConstraintData resultPhysicsConstraintData)
        {
            if (PhysicsConsDataEX_Dictionary.TryGetValue2(physicsConstraintDataIndex, out var physicsConstraintDataEX))
            {
                resultPhysicsConstraintData = physicsConstraintDataEX.PhysicsConstraintData;
                return true;
            }
            resultPhysicsConstraintData = null;
            return false;
        }



        /// <summary>
        /// <see cref="PhysicsConstraintDataEX"/> 얻기
        /// </summary>
        public PhysicsConstraintDataEX? GetPhysicsConstraintDataEX(string physicsConstraintDataName)
        {
            try
            {
                return PhysicsConsDataEX_Dictionary[physicsConstraintDataName];
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>PhysicsConstraintDataEX</b> 얻기 실패 <color=cyan>{physicsConstraintDataName}</color>");
                return null;
            }
        }

        /// <summary>
        /// <see cref="PhysicsConstraintDataEX"/> 얻어보기
        /// </summary>
        public bool TryGetPhysicsConstraintDataEX(string physicsConstraintDataName, out PhysicsConstraintDataEX resultPhysicsConstraintDataEX)
        {
            return PhysicsConsDataEX_Dictionary.TryGetValue1(physicsConstraintDataName, out resultPhysicsConstraintDataEX);
        }



        /// <summary>
        /// <see cref="PhysicsConstraintDataEX"/> 얻기 (<paramref name="physicsConstraintDataIndex"/>)
        /// </summary>
        public PhysicsConstraintDataEX? GetPhysicsConstraintDataEX(int physicsConstraintDataIndex)
        {
            try
            {
                return PhysicsConsDataEX_Dictionary[physicsConstraintDataIndex];
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>PhysicsConstraintDataEX</b> 얻기 실패 <color=cyan>{physicsConstraintDataIndex}</color>");
                return null;
            }
        }

        /// <summary>
        /// <see cref="PhysicsConstraintDataEX"/> 얻어보기 (<paramref name="physicsConstraintDataIndex"/>)
        /// </summary>
        public bool TryGetPhysicsConstraintDataEX(int physicsConstraintDataIndex, out PhysicsConstraintDataEX resultPhysicsConstraintDataEX)
        {
            return PhysicsConsDataEX_Dictionary.TryGetValue2(physicsConstraintDataIndex, out resultPhysicsConstraintDataEX);
        }



        //? SpineEvent



        ///<summary>
        ///<see cref="EventData"/>을 얻기
        /// </summary>
        public EventData GetSpineEventData(string slotName)
        {
            try
            {
                return SpineEvent_Dictionary[slotName];
            }
            catch
            {
                Debug.LogError($"{name} SkelSbject의 <b>Spine.EventData</b> 얻기 실패 <color=cyan>{slotName}</color>");
                return null;
            }
        }

        ///<summary>
        ///<see cref="EventData"/>을 얻어보기
        /// </summary>
        public bool TryGetSpineEventData(string slotName, out EventData resultEventData)
        {
            return SpineEvent_Dictionary.TryGetValue(slotName, out resultEventData);
        }



        private InvalidOperationException CreateRequiredDataException(string dataType, object key)
        {
            return new InvalidOperationException($"{name} SkelSbject required {dataType} not found. Key='{key}'.");
        }

        /// <summary>
        /// 기존 Get/TryGet API의 null 반환 계약은 유지하고, 필수 데이터 누락을 예외로 다룰 때 사용하는 Required API.
        /// </summary>
        public Skin GetRequiredSkin(string skinName)
        {
            if (TryGetSkin(skinName, out var result)) { return result; }
            throw CreateRequiredDataException(nameof(Skin), skinName);
        }

        public Spine.Animation GetRequiredAnimation(string animationName)
        {
            if (TryGetAnimation(animationName, out var result)) { return result; }
            throw CreateRequiredDataException(nameof(Spine.Animation), animationName);
        }

        public AnimationEX GetRequiredAnimationEX(string animationName)
        {
            if (TryGetAnimationEX(animationName, out var result)) { return result; }
            throw CreateRequiredDataException(nameof(AnimationEX), animationName);
        }

        public SlotData GetRequiredSlotData(string slotName)
        {
            if (TryGetSlotData(slotName, out var result)) { return result; }
            throw CreateRequiredDataException(nameof(SlotData), slotName);
        }

        public SlotData GetRequiredSlotData(int slotIndex)
        {
            if (TryGetSlotData(slotIndex, out var result)) { return result; }
            throw CreateRequiredDataException(nameof(SlotData), slotIndex);
        }

        public BoneData GetRequiredBoneData(string boneName)
        {
            if (TryGetBoneData(boneName, out var result)) { return result; }
            throw CreateRequiredDataException(nameof(BoneData), boneName);
        }

        public BoneData GetRequiredBoneData(int boneIndex)
        {
            if (TryGetBoneData(boneIndex, out var result)) { return result; }
            throw CreateRequiredDataException(nameof(BoneData), boneIndex);
        }

        public BoneDataEX GetRequiredBoneDataEX(string boneName)
        {
            if (TryGetBoneDataEX(boneName, out var result) && result.HasValue) { return result.Value; }
            throw CreateRequiredDataException(nameof(BoneDataEX), boneName);
        }

        public BoneDataEX GetRequiredBoneDataEX(int boneIndex)
        {
            if (TryGetBoneDataEX(boneIndex, out var result) && result.HasValue) { return result.Value; }
            throw CreateRequiredDataException(nameof(BoneDataEX), boneIndex);
        }

        public TransformConstraintData GetRequiredTransformConstraintData(string transformConstraintDataName)
        {
            if (TryGetTransformConstraintData(transformConstraintDataName, out var result)) { return result; }
            throw CreateRequiredDataException(nameof(TransformConstraintData), transformConstraintDataName);
        }

        public TransformConstraintData GetRequiredTransformConstraintData(int transformConstraintDataIndex)
        {
            if (TryGetTransformConstraintData(transformConstraintDataIndex, out var result)) { return result; }
            throw CreateRequiredDataException(nameof(TransformConstraintData), transformConstraintDataIndex);
        }

        public TransformConstraintDataEX GetRequiredTransformConstraintDataEX(string transformConstraintDataName)
        {
            if (TryGetTransformConstraintDataEX(transformConstraintDataName, out var result)) { return result; }
            throw CreateRequiredDataException(nameof(TransformConstraintDataEX), transformConstraintDataName);
        }

        public TransformConstraintDataEX GetRequiredTransformConstraintDataEX(int transformConstraintDataIndex)
        {
            if (TryGetTransformConstraintDataEX(transformConstraintDataIndex, out var result)) { return result; }
            throw CreateRequiredDataException(nameof(TransformConstraintDataEX), transformConstraintDataIndex);
        }

        public IkConstraintData GetRequiredIkConstraintData(string ikConstraintDataName)
        {
            if (TryGetIkConstraintData(ikConstraintDataName, out var result)) { return result; }
            throw CreateRequiredDataException(nameof(IkConstraintData), ikConstraintDataName);
        }

        public IkConstraintData GetRequiredIkConstraintData(int ikConstraintDataIndex)
        {
            if (TryGetIkConstraintData(ikConstraintDataIndex, out var result)) { return result; }
            throw CreateRequiredDataException(nameof(IkConstraintData), ikConstraintDataIndex);
        }

        public IkConstraintDataEX GetRequiredIkConstraintDataEX(string ikConstraintDataName)
        {
            if (TryGetIkConstraintDataEX(ikConstraintDataName, out var result)) { return result; }
            throw CreateRequiredDataException(nameof(IkConstraintDataEX), ikConstraintDataName);
        }

        public IkConstraintDataEX GetRequiredIkConstraintDataEX(int ikConstraintDataIndex)
        {
            if (TryGetIkConstraintDataEX(ikConstraintDataIndex, out var result)) { return result; }
            throw CreateRequiredDataException(nameof(IkConstraintDataEX), ikConstraintDataIndex);
        }

        public PathConstraintData GetRequiredPathConstraintData(string pathConstraintDataName)
        {
            if (TryGetPathConstraintData(pathConstraintDataName, out var result)) { return result; }
            throw CreateRequiredDataException(nameof(PathConstraintData), pathConstraintDataName);
        }

        public PathConstraintData GetRequiredPathConstraintData(int pathConstraintDataIndex)
        {
            if (TryGetPathConstraintData(pathConstraintDataIndex, out var result)) { return result; }
            throw CreateRequiredDataException(nameof(PathConstraintData), pathConstraintDataIndex);
        }

        public PathConstraintDataEX GetRequiredPathConstraintDataEX(string pathConstraintDataName)
        {
            if (TryGetPathConstraintDataEX(pathConstraintDataName, out var result)) { return result; }
            throw CreateRequiredDataException(nameof(PathConstraintDataEX), pathConstraintDataName);
        }

        public PathConstraintDataEX GetRequiredPathConstraintDataEX(int pathConstraintDataIndex)
        {
            if (TryGetPathConstraintDataEX(pathConstraintDataIndex, out var result)) { return result; }
            throw CreateRequiredDataException(nameof(PathConstraintDataEX), pathConstraintDataIndex);
        }

        public PhysicsConstraintData GetRequiredPhysicsConstraintData(string physicsConstraintDataName)
        {
            if (TryGetPhysicsConstraintData(physicsConstraintDataName, out var result)) { return result; }
            throw CreateRequiredDataException(nameof(PhysicsConstraintData), physicsConstraintDataName);
        }

        public PhysicsConstraintData GetRequiredPhysicsConstraintData(int physicsConstraintDataIndex)
        {
            if (TryGetPhysicsConstraintData(physicsConstraintDataIndex, out var result)) { return result; }
            throw CreateRequiredDataException(nameof(PhysicsConstraintData), physicsConstraintDataIndex);
        }

        public PhysicsConstraintDataEX GetRequiredPhysicsConstraintDataEX(string physicsConstraintDataName)
        {
            if (TryGetPhysicsConstraintDataEX(physicsConstraintDataName, out var result)) { return result; }
            throw CreateRequiredDataException(nameof(PhysicsConstraintDataEX), physicsConstraintDataName);
        }

        public PhysicsConstraintDataEX GetRequiredPhysicsConstraintDataEX(int physicsConstraintDataIndex)
        {
            if (TryGetPhysicsConstraintDataEX(physicsConstraintDataIndex, out var result)) { return result; }
            throw CreateRequiredDataException(nameof(PhysicsConstraintDataEX), physicsConstraintDataIndex);
        }

        public EventData GetRequiredSpineEventData(string eventName)
        {
            if (TryGetSpineEventData(eventName, out var result)) { return result; }
            throw CreateRequiredDataException(nameof(EventData), eventName);
        }



        #endregion



        ///======================================================================================================================================================



        /// <summary>
        /// 소환될 <see cref="SkelObject"/>의 로컬 중심 좌표
        /// </summary>
        public Vector2 CenterLocalPosition { get => centerLocalPosition; private set => centerLocalPosition = value; }
        [SerializeField] private Vector2 centerLocalPosition;



        /// <summary>
        /// 기본 애니메이션 MixDuration
        /// </summary>
        public float AnimationMixDuration { get => animationMixDuration; private set => animationMixDuration = value; }
        [SerializeField] private float animationMixDuration = 0.2f;



        ///======================================================================================================================================================



        //? 열거형-인덱스 매니저



        /// <summary>
        /// 스킨 레이어 열거형-인덱스 매니저 (제네릭 미사용)
        /// </summary>
        public virtual IEnumIndex_SkinLayer EnumIndex_SkinLayers { get; private set; }

        /// <summary>
        /// 애니메이션 트랙 열거형-인덱스 매니저 (제네릭 미사용)
        /// </summary>
        public virtual IEnumIndex_AniTrack EnumIndex_AniTracks { get; private set; }

        /// <summary>
        /// 애니메이션 랭크 열거형-인덱스 매니저 (제네릭 미사용)
        /// </summary>
        public virtual IEnumIndex_AniRank EnumIndex_AniRanks { get; private set; }

        /// <summary>
        /// 애니메이션 타임 열거형-인덱스 매니저 (제네릭 미사용)
        /// </summary>
        public virtual IEnumIndex_AniTime EnumIndex_AniTimes { get; private set; }



        /// <summary>
        /// 스킨 레이어 열거형-인덱스 매니저가 별도의 Enum을 사용하지않아,
        /// <para>기본 열거형인 <see cref="ESkinLayerDefault"/>를 사용 중인지 확인</para>
        /// <para>별개의 열거형을 사용하고싶다면, <see cref="VirtualWakeUp_EnumIndex_SkinLayers"/>을 완전히 재정의하여 그곳에서 <see cref="EnumIndex_SkinLayers"/> 를 선언해야함</para>
        /// </summary>
        [field: SerializeField][field: ReadOnly] public bool IsDefault_EnumIndex_SkinLayers { get; private set; }

        /// <summary>
        /// 애니메이션 트랙 열거형-인덱스 매니저가 별도의 Enum을 사용하지않아,
        /// <para>기본 열거형인 <see cref="EAniTrackDefault"/>를 사용 중인지 확인</para>
        /// <para>별개의 열거형을 사용하고싶다면, <see cref="VirtualWakeUp_EnumIndex_AniTracks"/>을 완전히 재정의하여 그곳에서 <see cref="EnumIndex_AniTracks"/> 를 선언해야함</para>
        /// </summary>
        [field: SerializeField][field: ReadOnly] public bool IsDefault_EnumIndex_AniTracks { get; private set; }

        /// <summary>
        /// 애니메이션 랭크열거형-인덱스 매니저가 별도의 Enum을 사용하지않아,
        /// <para>기본 열거형인 <see cref="EAniRankDefault"/>를 사용 중인지 확인</para>
        /// <para>별개의 열거형을 사용하고싶다면, <see cref="VirtualWakeUp_EnumIndex_AniRanks"/>을 완전히 재정의하여 그곳에서 <see cref="EnumIndex_AniRanks"/> 를 선언해야함</para>
        /// </summary>
        [field: SerializeField][field: ReadOnly] public bool IsDefault_EnumIndex_AniRanks { get; private set; }

        /// <summary>
        /// 애니메이션 타입 열거형-인덱스 매니저가 별도의 Enum을 사용하지않아,
        /// <para>기본 열거형인 <see cref="EAniTimeDefault"/>를 사용 중인지 확인</para>
        /// <para>별개의 열거형을 사용하고싶다면, <see cref="VirtualWakeUp_EnumIndex_AniTimes"/>을 완전히 재정의하여 그곳에서 <see cref="EnumIndex_AniTimes"/> 를 선언해야함</para>
        /// </summary>
        [field: SerializeField][field: ReadOnly] public bool IsDefault_EnumIndex_AniTimes { get; private set; }



        //? 열거형-인덱스 매니저의 초기화 메서드를 별도로 override 하지 않으면, 지정되는 빈 열거형
        public enum ESkinLayerDefault { }
        public enum EAniTrackDefault { }
        public enum EAniRankDefault { }
        public enum EAniTimeDefault { }



        ///======================================================================================================================================================



        //? Sbject 불러오기 (캐스팅)



        #region Sbject 불러오기 (캐스팅)

        /// <summary>
        /// 이 Sbject 의 캐스팅 불러오기
        /// </summary>
        public TSkelSbject LoadSbject<TSkelSbject>() where TSkelSbject : SkelSbject, new()
        {
            if (this is TSkelSbject result)
            {
                return result;
            }

            return null;
        }

        public TSkelSbject LoadRequiredSbject<TSkelSbject>() where TSkelSbject : SkelSbject, new()
        {
            var result = LoadSbject<TSkelSbject>();
            if (result != null) { return result; }
            throw CreateRequiredDataException(typeof(TSkelSbject).Name, name);
        }



        /// <summary>
        /// 이 Sbject 의 캐스팅 여부를 불러오기
        /// </summary>
        public bool ContainsSbject<TSkelSbject>() where TSkelSbject : SkelSbject, new()
        {
            return LoadSbject<TSkelSbject>() != null;
        }



        /// <summary>
        /// 이 Sbject 의 캐스팅과 여부를 불러오기
        /// </summary>
        public bool TryGetSbject<TSkelSbject>(out TSkelSbject resultSkelSbject) where TSkelSbject : SkelSbject, new()
        {
            resultSkelSbject = LoadSbject<TSkelSbject>();
            return resultSkelSbject != null;
        }

        #endregion



        //? Animation 불러오기 (캐스팅)



        #region Animation 불러오기 (캐스팅)

        /// <summary>
        /// 이 Sbject가 해당 <typeparamref name="TAnimation"/>의 인터페이스를 상속받는 <see cref="SkelSbject"/> 라면, 캐스팅 하여 반환<br/>
        /// <paramref name="IgnoreAdvancedSkin"/>가 false일경우, AdvancedSkin을 우선으로 캐스팅을 시도한다<br/>
        /// <i>(<paramref name="skelObject"/>의 스킨이 해당 AdvancedSkin 상태여야 불러온다)</i><br/>
        /// 그 외에는 <see cref="SkelSbject"/>, <see cref="AniBox"/> 순으로 캐스팅하며 반환한다
        /// </summary>
        /// <param name="IgnoreAdvancedSkin">활성화시, AdvancedSkin을 무시하고 불러옴</param>
        public TAnimation LoadAnimation<TAnimation>(SkelObject skelObject, bool IgnoreAdvancedSkin = false) where TAnimation : class, IHoldAniBase
        {
            //? #1 AdvancedSkin 무시가 비활성화, RunTimeSkinCenter의 현재 AdvancedSkin이 TAni를 보유하고있을 경우
            if (!IgnoreAdvancedSkin && skelObject.Skel.RunTimeSkins.MainAdvancedSkin is TAnimation advancedAni)
            {
                return advancedAni;
            }

            //? #2 이 Sbject가 TAni를 보유하고 있을 경우
            if (this is TAnimation sbjectAni)
            {
                return sbjectAni;
            }

            //? #3 AniBox가 TAni를 보유하고 있을경우
            if (this is IHoldAniBox aniBox && aniBox.AniBox is TAnimation aniBoxAni)
            {
                return aniBoxAni;
            }

            return null;
        }

        public TAnimation LoadRequiredAnimation<TAnimation>(SkelObject skelObject, bool IgnoreAdvancedSkin = false) where TAnimation : class, IHoldAniBase
        {
            var result = LoadAnimation<TAnimation>(skelObject, IgnoreAdvancedSkin);
            if (result != null) { return result; }
            throw CreateRequiredDataException(typeof(TAnimation).Name, skelObject != null ? skelObject.name : "null");
        }



        /// <summary>
        /// 이 Sbject가 해당 <typeparamref name="TAnimation"/>의 인터페이스를 상속받는 <see cref="SkelSbject"/> 라면, 캐스팅 여부를 반환<br/>
        /// <paramref name="IgnoreAdvancedSkin"/>가 false일경우, AdvancedSkin을 우선으로 캐스팅을 시도한다<br/>
        /// <i>(<paramref name="skelObject"/>의 스킨이 해당 AdvancedSkin 상태여야 불러온다)</i><br/>
        /// 그 외에는 <see cref="SkelSbject"/>, <see cref="AniBox"/> 순으로 캐스팅 여부를 반환한다
        /// </summary>
        /// <param name="IgnoreAdvancedSkin">활성화시, AdvancedSkin을 무시하고 불러옴</param>
        public bool ContainsAnimation<TAnimation>(SkelObject skelObject, bool IgnoreAdvancedSkin = false) where TAnimation : class, IHoldAniBase
        {
            return LoadAnimation<TAnimation>(skelObject, IgnoreAdvancedSkin) != null;
        }



        /// <summary>
        /// 이 Sbject가 해당 <typeparamref name="TAnimation"/>의 인터페이스를 상속받는 <see cref="SkelSbject"/> 라면, 캐스팅과 여부를 반환<br/>
        /// <paramref name="IgnoreAdvancedSkin"/>가 false일경우, AdvancedSkin을 우선으로 캐스팅을 시도한다<br/>
        /// <i>(<paramref name="skelObject"/>의 스킨이 해당 AdvancedSkin 상태여야 불러온다)</i><br/>
        /// 그 외에는 <see cref="SkelSbject"/>, <see cref="AniBox"/> 순으로 캐스팅과 여부를 반환한다
        /// </summary>
        /// <param name="IgnoreAdvancedSkin">활성화시, AdvancedSkin을 무시하고 불러옴</param>
        public bool TryLoadAnimation<TAnimation>(SkelObject skelObject, out TAnimation result, bool IgnoreAdvancedSkin = false) where TAnimation : class, IHoldAniBase
        {
            result = LoadAnimation<TAnimation>(skelObject, IgnoreAdvancedSkin);
            return result != null;
        }

        #endregion



        //? Bone 불러오기 (캐스팅)



        #region Bone 불러오기 (캐스팅)

        /// <summary>
        /// 이 Sbject가 해당 <typeparamref name="TBone"/>의 인터페이스를 상속받는 <see cref="SkelSbject"/> 라면, 캐스팅 하여 반환<br/>
        /// 실패한다면, <see cref="BoneBox"/> 를 캐스팅하며 반환한다
        /// </summary>
        public TBone LoadBone<TBone>() where TBone : class, IHoldBoneBase
        {
            if (this is TBone resultBone)
            {
                return resultBone;
            }

            if (this is IHoldBoneBox boneBox && boneBox.BoneBox is TBone boneBoxBone)
            {
                return boneBoxBone;
            }

            return null;
        }

        public TBone LoadRequiredBone<TBone>() where TBone : class, IHoldBoneBase
        {
            var result = LoadBone<TBone>();
            if (result != null) { return result; }
            throw CreateRequiredDataException(typeof(TBone).Name, name);
        }



        /// <summary>
        /// 이 Sbject가 해당 <typeparamref name="TBone"/>의 인터페이스를 상속받는 <see cref="SkelSbject"/> 라면, 캐스팅 여부를 반환<br/>
        /// 실패한다면, <see cref="BoneBox"/> 를 캐스팅 여부를 반환 한다
        /// </summary>
        public bool TryLoadBone<TBone>() where TBone : class, IHoldBoneBase
        {
            return LoadBone<TBone>() != null;
        }



        /// <summary>
        /// 이 Sbject가 해당 <typeparamref name="TBone"/>의 인터페이스를 상속받는 <see cref="SkelSbject"/> 라면, 캐스팅 하여 반환<br/>
        /// 실패한다면, <see cref="BoneBox"/> 를 캐스팅하며 반환한다
        /// </summary>
        public bool TryLoadBone<TBone>(out TBone resultBone) where TBone : class, IHoldBoneBase
        {
            resultBone = LoadBone<TBone>();
            return resultBone != null;
        }

        #endregion



        ///======================================================================================================================================================



        //? 내부 델리게이트 이벤트



        ///<summary>
        ///이 <see cref="SkelSbject"/>가 들어있는 <see cref="SkelObject"/>가 <b>맨 처음 등록되어 WakeUp</b> 되었을때 실행되는 이벤트
        ///<para> <see cref="WakeUp_SkelObject_Current"/> 이전에 실행된다</para>
        /// </summary>
        protected event Action<SkelObject> WakeUp_SkelObjectEvent;



        ///<summary>
        ///이 <see cref="SkelSbject"/>가 들어있는 <see cref="SkelObject"/>가 <b>활성화</b> 되었을때 실행되는 이벤트
        ///<para> <see cref="Enable_SkelObject_Current"/> 이전에 실행된다</para>
        /// </summary>
        protected event Action<SkelObject> Enable_SkelObjectEvent;



        ///<summary>
        ///이 <see cref="SkelSbject"/>가 들어있는 <see cref="SkelObject"/>가 <b>비활성화</b> 되었을때 실행되는 이벤트
        ///<para> <see cref="Disable_SkelObject_Current"/> 이전에 실행된다</para>
        /// </summary>
        protected event Action<SkelObject> Disable_SkelObjectEvent;



        ///======================================================================================================================================================



        //? 초기화



        protected sealed override void WakeUp()
        {
            WakeUp_SkelSbject();
        }



        ///<summary>
        /// 기본 SkelSbject 초기화
        /// </summary>
        [Button("WakeUp_SkelSbject 강제호출"), GUIColor(1f, 0.2f, 0)]
        private void WakeUp_SkelSbject()
        {
            #region 기본 SkelSbject 초기화


            //? 데이터에셋 null체크 + 디버깅
            if (Skeleton_DataAsset == null)
            {
                Debug.LogError($"{name}의 SkeletonDataAsset을 찾을수 없음, WakeUp 실패");
                m_IsWakeUp = false;
                return;
            }


            //? 데이터 캐싱
            Skeleton_Data = skeleton_DataAsset.GetSkeletonData(true);
            if (Skeleton_Data == null)
            {
                Debug.LogError($"{name}의 SkeletonData를 찾을수 없음, WakeUp 실패");
                m_IsWakeUp = false;
                return;
            }


            //? 스킨 캐싱
            Skin_Dictioinary = new ImmutableDataManager<string, Skin>(Skeleton_Data.Skins, skin => skin.Name);


            //? 제약조건 캐싱 (Spine 4.3부터 SkeletonData.Constraints로 통합)
            List<KeyValuePair<TransformConstraintData, int>> transformConstraintDatas = new(Skeleton_Data.Constraints.Count);
            List<KeyValuePair<IkConstraintData, int>> ikConstraintDatas = new(Skeleton_Data.Constraints.Count);
            List<KeyValuePair<PathConstraintData, int>> pathConstraintDatas = new(Skeleton_Data.Constraints.Count);
            List<KeyValuePair<PhysicsConstraintData, int>> physicsConstraintDatas = new(Skeleton_Data.Constraints.Count);
            for (int constraintIndex = 0; constraintIndex < Skeleton_Data.Constraints.Count; constraintIndex++)
            {
                switch (Skeleton_Data.Constraints.Items[constraintIndex])
                {
                    case TransformConstraintData tfc:
                    transformConstraintDatas.Add(new KeyValuePair<TransformConstraintData, int>(tfc, constraintIndex));
                    break;

                    case IkConstraintData ikc:
                    ikConstraintDatas.Add(new KeyValuePair<IkConstraintData, int>(ikc, constraintIndex));
                    break;

                    case PathConstraintData pac:
                    pathConstraintDatas.Add(new KeyValuePair<PathConstraintData, int>(pac, constraintIndex));
                    break;

                    case PhysicsConstraintData phc:
                    physicsConstraintDatas.Add(new KeyValuePair<PhysicsConstraintData, int>(phc, constraintIndex));
                    break;
                }
            }


            //? 스킨전용
            Dictionary<string, List<Skin>> skinRequired_Bones_Skins = new(Skeleton_Data.Bones.Count);
            Dictionary<string, List<Skin>> skinRequired_TFCons_Skins = new(transformConstraintDatas.Count);
            Dictionary<string, List<Skin>> skinRequired_IKCons_Skins = new(ikConstraintDatas.Count);
            Dictionary<string, List<Skin>> skinRequired_PathCons_skins = new(pathConstraintDatas.Count);
            Dictionary<string, List<Skin>> skinRequired_PhysicsCons_skins = new(physicsConstraintDatas.Count);
            //. 스킨들을 순회하면서, SkinRequired를 찾는다
            for (int i = 0; i < Skin_Dictioinary.dataArray.Length; i++)
            {
                var skin = Skin_Dictioinary.dataArray[i];

                //! SkinRequired가 아니라면 continue
                if (!skin.HasRequiredElements()) { continue; }


                //. SkinRequired Bone 순회
                if (skin.Bones.Count != 0)
                {
                    for (int j = 0; j < skin.Bones.Count; j++)
                    {
                        //. 본 이름을 Key로, Skin들의 리스트를 Value로 딕셔너리에 추가한다
                        var boneData_SkinRequired = skin.Bones.Items[j];
                        if (skinRequired_Bones_Skins.TryGetValue(boneData_SkinRequired.Name, out var skinList)) skinList.Add(skin);
                        else skinRequired_Bones_Skins.Add(boneData_SkinRequired.Name, new List<Skin> { skin });
                    }
                }

                //. SkinRequired 제약조건 순회
                if (skin.Constraints.Count != 0)
                {
                    for (int j = 0; j < skin.Constraints.Count; j++)
                    {
                        var constraint_SkinRequired = skin.Constraints.Items[j];

                        //. 제약조건의 타입에따라, 어떤 딕셔너리에 추가할지 결정
                        Dictionary<string, List<Skin>> targetDictionary;
                        switch (constraint_SkinRequired)
                        {
                            case TransformConstraintData tfCons:
                            targetDictionary = skinRequired_TFCons_Skins;
                            break;

                            case IkConstraintData ikCons:
                            targetDictionary = skinRequired_IKCons_Skins;
                            break;

                            case PathConstraintData pathCons:
                            targetDictionary = skinRequired_PathCons_skins;
                            break;

                            case PhysicsConstraintData physicsCons:
                            targetDictionary = skinRequired_PhysicsCons_skins;
                            break;

                            default: targetDictionary = null; break;
                        }

                        if (targetDictionary == null) { continue; }

                        //. 제약조건 이름을 Key로, Skin들의 리스트를 Value로 딕셔너리에 추가한다
                        if (targetDictionary.TryGetValue(constraint_SkinRequired.Name, out var skinList)) skinList.Add(skin);
                        else targetDictionary.Add(constraint_SkinRequired.Name, new List<Skin> { skin });
                    }
                }
            }


            //? 애니메이션 캐싱
            AnimationEX[] animations = new AnimationEX[Skeleton_Data.Animations.Count];
            int animationIndex = 0;
            foreach (var animation in Skeleton_Data.Animations)
            {
                animations[animationIndex] = new AnimationEX(animation);
                animationIndex++;
            }
            AnimationEX_Dictionary = new ImmutableDataManager<string, AnimationEX>(animations, animation => animation.Animation.Name);


            //? 슬롯 캐싱
            Slot_Dictionary = new ImmutableDataManager2<string, int, SlotData>(
                Skeleton_Data.Slots, //x Reverse하고 넣어야, 순서가 DrawOrder에 준수하여 들어간다 (Index가 0인 슬롯은 최상단)
                slotData => slotData.Name,
                slotData => slotData.Index);


            //? 본 캐싱
            BoneDataEX[] bones = new BoneDataEX[Skeleton_Data.Bones.Count];
            int boneIndex = 0;
            foreach (var bone in Skeleton_Data.Bones)
            {
                bones[boneIndex] = new BoneDataEX(bone, boneIndex, skinRequired_Bones_Skins.TryGetValue(bone.Name, out var skinList) ? skinList.ToArray() : null);
                boneIndex++;
            }
            BoneDataEX_Dictionary = new ImmutableDataManager2<string, int, BoneDataEX>(bones,
                boneDataEX => boneDataEX.Name,
                boneDataEX => boneDataEX.Index
            );


            //? 트랜스폼 제약조건 캐싱
            TransformConstraintDataEX[] tfConsDatas = new TransformConstraintDataEX[transformConstraintDatas.Count];
            List<TransformConstraintDataEX> tfConsDatas_SkinRequired = new List<TransformConstraintDataEX>(transformConstraintDatas.Count);
            int transformIndex = 0;
            foreach (var constraintPair in transformConstraintDatas)
            {
                var tfc = constraintPair.Key;
                var item = new TransformConstraintDataEX(tfc, transformIndex, skinRequired_TFCons_Skins.TryGetValue(tfc.Name, out var skinList) ? skinList.ToArray() : null);
                tfConsDatas[transformIndex] = item;
                if (tfc.SkinRequired) tfConsDatas_SkinRequired.Add(item);
                transformIndex++;
            }
            TFConsDataEX_Dictionary = new ImmutableDataManager2<string, int, TransformConstraintDataEX>(tfConsDatas,
                tfcEX => tfcEX.Name,
                tfcEX => tfcEX.Index
            );


            //? IK 제약조건 캐싱
            IkConstraintDataEX[] ikConsDatas = new IkConstraintDataEX[ikConstraintDatas.Count];
            List<IkConstraintDataEX> ikConsDatas_SkinRequired = new List<IkConstraintDataEX>(ikConstraintDatas.Count);
            int ikIndex = 0;
            foreach (var constraintPair in ikConstraintDatas)
            {
                var ikc = constraintPair.Key;
                var item = new IkConstraintDataEX(ikc, ikIndex, skinRequired_IKCons_Skins.TryGetValue(ikc.Name, out var skinList) ? skinList.ToArray() : null);
                ikConsDatas[ikIndex] = item;
                if (ikc.SkinRequired) ikConsDatas_SkinRequired.Add(item);
                ikIndex++;
            }
            IKConsDataEX_Dictionary = new ImmutableDataManager2<string, int, IkConstraintDataEX>(ikConsDatas,
                    ikEX => ikEX.Name,
                    ikEX => ikEX.Index
                );


            //? Path 제약조건 캐싱
            PathConstraintDataEX[] pathConsDatas = new PathConstraintDataEX[pathConstraintDatas.Count];
            List<PathConstraintDataEX> pathConsDatas_SkinRequired = new List<PathConstraintDataEX>(pathConstraintDatas.Count);
            int pathIndex = 0;
            foreach (var constraintPair in pathConstraintDatas)
            {
                var pac = constraintPair.Key;
                var item = new PathConstraintDataEX(pac, pathIndex, skinRequired_PathCons_skins.TryGetValue(pac.Name, out var skinList) ? skinList.ToArray() : null);
                pathConsDatas[pathIndex] = item;
                if (pac.SkinRequired) pathConsDatas_SkinRequired.Add(item);
                pathIndex++;
            }
            PathConsDataEX_Dictionary = new ImmutableDataManager2<string, int, PathConstraintDataEX>(pathConsDatas,
                    pacEX => pacEX.Name,
                    pacEX => pacEX.Index
                );


            //? Physics 제약조건 캐싱
            PhysicsConstraintDataEX[] PhysicsConsDatas = new PhysicsConstraintDataEX[physicsConstraintDatas.Count];
            List<PhysicsConstraintDataEX> PhysicsConsDatas_SkinRequired = new List<PhysicsConstraintDataEX>(physicsConstraintDatas.Count);
            int physicsIndex = 0;
            foreach (var constraintPair in physicsConstraintDatas)
            {
                var phc = constraintPair.Key;
                var item = new PhysicsConstraintDataEX(phc, physicsIndex, skinRequired_PhysicsCons_skins.TryGetValue(phc.Name, out var skinList) ? skinList.ToArray() : null);
                PhysicsConsDatas[physicsIndex] = item;
                if (phc.SkinRequired) PhysicsConsDatas_SkinRequired.Add(item);
                physicsIndex++;
            }
            PhysicsConsDataEX_Dictionary = new ImmutableDataManager2<string, int, PhysicsConstraintDataEX>(PhysicsConsDatas,
                    phcEX => phcEX.Name,
                    phcEX => phcEX.Index
                );


            //? Event 캐싱
            //Event_Dictionary= new ImmutableDataManager<string, Spine.Animation>(Skeleton_Data.Animations, animation => animation.Name);
            SpineEvent_Dictionary = new ImmutableDataManager<string, Spine.EventData>(Skeleton_Data.Events.Items, evnt => evnt.Name);


            //? 슬롯 인덱스 드로우오더 배열 캐싱
            SlotIndexesDrawOrder_Array = new int[Skeleton_Data.Slots.Count];
            for (int i = 0; i < Skeleton_Data.Slots.Count; i++)
            {
                SlotIndexesDrawOrder_Array[i] = Skeleton_Data.Slots.Items[i].Index;
            }



            #endregion


            WakeUp_Current();
            VirtualWakeUp_EnumIndex_SkinLayers();
            VirtualWakeUp_EnumIndex_AniTracks();
            VirtualWakeUp_EnumIndex_AniRanks();
            VirtualWakeUp_EnumIndex_AniTimes();
        }



        /// <summary>
        /// <see cref="SkelSbject"/>을 상속받은 객체에서 초기화
        /// </summary>
        protected abstract void WakeUp_Current();



        /// <summary>
        /// 열거형-인덱스 매니저를 초기화한다 : 스킨 레이어
        /// <para><b>이 메서드를 override하지 않으면, 열거형-인덱스 매니저는 기본값으로 초기화되어 사용된다</b></para>
        /// </summary>
        protected virtual void VirtualWakeUp_EnumIndex_SkinLayers()
        {
            EnumIndex_SkinLayers = new EnumIndex_SkinLayer<ESkinLayerDefault>(true);
            IsDefault_EnumIndex_SkinLayers = true;
        }



        /// <summary>
        /// 열거형-인덱스 매니저를 초기화한다 : 애니메이션 트랙
        /// <para><b>이 메서드를 override하지 않으면, 열거형-인덱스 매니저는 기본값으로 초기화되어 사용된다</b></para>
        /// </summary>
        protected virtual void VirtualWakeUp_EnumIndex_AniTracks()
        {
            EnumIndex_AniTracks = new EnumIndex_AniTrack<EAniTrackDefault>(true);
            IsDefault_EnumIndex_AniTracks = true;
        }



        /// <summary>
        /// 열거형-인덱스 매니저를 초기화한다 : 애니메이션 랭크
        /// <para><b>이 메서드를 override하지 않으면, 열거형-인덱스 매니저는 기본값으로 초기화되어 사용된다</b></para>
        /// </summary>
        protected virtual void VirtualWakeUp_EnumIndex_AniRanks()
        {
            EnumIndex_AniRanks = new EnumIndex_AniRank<EAniRankDefault>(true);
            IsDefault_EnumIndex_AniRanks = true;
        }



        /// <summary>
        /// 열거형-인덱스 매니저를 초기화한다 : 애니메이션 타임
        /// <para><b>이 메서드를 override하지 않으면, 열거형-인덱스 매니저는 기본값으로 초기화되어 사용된다</b></para>
        /// </summary>
        protected virtual void VirtualWakeUp_EnumIndex_AniTimes()
        {
            EnumIndex_AniTimes = new EnumIndex_AniTime<EAniTimeDefault>(true);
            IsDefault_EnumIndex_AniTimes = true;
        }



        ///======================================================================================================================================================



        //? 초기화 (SkelObject에 의한)



        ///<summary>
        ///<see cref="SkelObject"/>에 이 <see cref="SkelSbject"/>가 등록될때 실행
        /// </summary>
        public void WakeUp_SkelObject(SkelObject skelObject)
        {
            WakeUp_SkelObjectEvent?.Invoke(skelObject);
            WakeUp_SkelObject_Current(skelObject);
        }



        ///<summary>
        /// (내부 재정의용 메서드) <see cref="SkelObject"/>에 이 <see cref="SkelSbject"/>가 등록될때 실행
        /// </summary>
        protected virtual void WakeUp_SkelObject_Current(SkelObject skelObject) { }



        /// <summary>
        /// 이 <see cref="SkelSbject"/>를 보유하고있는 <see cref="SkelObject"/> 객체가 <b>활성화</b> 되었을때 실행
        /// </summary>
        /// <param name="skelObject"></param>
        public void Enable_SkelObject(SkelObject skelObject)
        {
            Enable_SkelObjectEvent?.Invoke(skelObject);
            Enable_SkelObject_Current(skelObject);
        }



        /// <summary>
        /// (내부 재정의용 메서드)  이 <see cref="SkelSbject"/>를 보유하고있는 <see cref="SkelObject"/> 객체가 <b>활성화</b> 되었을때 실행
        /// </summary>
        /// <param name="skelObject"></param>
        protected virtual void Enable_SkelObject_Current(SkelObject skelObject) { }



        /// <summary>
        /// 이 <see cref="SkelSbject"/>를 보유하고있는 <see cref="SkelObject"/> 객체가 <b>비활성화</b> 되었을때 실행
        /// </summary>
        /// <param name="skelObject"></param>
        public void Disable_SkelObject(SkelObject skelObject)
        {
            Disable_SkelObjectEvent?.Invoke(skelObject);
            Disable_SkelObject_Current(skelObject);
        }



        /// <summary>
        /// (내부 재정의용 메서드)  이 <see cref="SkelSbject"/>를 보유하고있는 <see cref="SkelObject"/> 객체가 <b>활성화</b> 되었을때 실행
        /// </summary>
        /// <param name="skelObject"></param>
        protected virtual void Disable_SkelObject_Current(SkelObject skelObject) { }



        ///======================================================================================================================================================



        /// <summary>
        /// SpineEvent 함수, 이 SkelSbject를 기반
        /// </summary>
        public virtual void SpineEvent(SkelObject skelObject, TrackEntry trackEntry, Spine.Event spineEvent) { }



        /// <summary>
        /// Flip될때의 이벤트, 이 SkelSbject를 기반
        /// </summary>
        public virtual void FlipEvent(SkelObject skelObject, EDirectionLR lr) { }



        ///======================================================================================================================================================



        //정체불명


        private void TestManualDrawOrdering(SkelObject skelObject)
        {
            ExposedList<Timeline> timeLineList = new ExposedList<Timeline>();

            DrawOrderTimeline time = new DrawOrderTimeline(1);

            //var f = skelObject.Skel.IndexSlots.









            int[] applyArray = null;

            time.SetFrame(1, 1, applyArray);

            var spineAnimation = new Spine.Animation("Test");
            spineAnimation.SetTimelines(timeLineList, new ExposedList<int>());
            spineAnimation.Duration = 1;
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================



    //? 어드밴스드스킨 필수 보유 SkelSbject



    /// <summary>
    /// <b>어드밴스드스킨</b> 을 반드시 보유하는 <see cref="SkelSbject"/>
    /// </summary>
    /// <typeparam name="TSkelSbject">
    /// 이 <see cref="SkelSbject"/>을 넣기
    /// </typeparam>
    /// <typeparam name="TAdvancedSkin">
    /// 대상 어드밴스드 스킨<br/>
    /// (해당 클래스 내부에 <see cref="SkelSbject.BaseAdvancedSkin"/>&lt;<see cref="TSkelSbject"/>&gt; 사용 권장함)
    /// </typeparam>
    public abstract class SkelSbject_AdvancedSkin<TSkelSbject, TAdvancedSkin> : SkelSbject, IHoldAdvancedSkinManager<TSkelSbject, TAdvancedSkin>
        where TSkelSbject : SkelSbject, new()
        where TAdvancedSkin : BaseAdvancedSkin<TSkelSbject>
    {
        /// <summary>
        /// 어드밴스드 스킨들
        /// </summary>
        public TypeInstancesFactoryManager<TAdvancedSkin, TSkelSbject> AdvancedSkins { get; protected set; }

        public BaseAdvancedSkin[] GetAdvancedSkinArray => AdvancedSkins.TypeInstancesFactory.TypeInstanceDictionary.Values.ToArray();

        protected override void LateWakeUp()
        {
            //. 어드밴스드 스킨의 WakeUp 내부에서, AniBox 등 SkelSbject의 초기화가 필요할수 있기 때문에,
            //. LateWakeUp에서 AdvancedSkins를 초기화한다
            if (!IsWakeUp || Skeleton_Data == null) { return; }

            base.LateWakeUp();
            LateWakeUp_SetAdvancedSkins();
        }



        /// <summary>
        /// <see cref="LateWakeUp"/>에서 호출되는, 어드밴스드 스킨을 지정 메서드<br/>
        /// 이 메서드를 <c>override</c> 하여 별도의 <see cref="SetAdvancedSkins(TypeInstancesFactoryManager{TAdvancedSkin, SkelSbject})"/>을 호출하여 사용할수 있다<br/>
        /// 생성된 <see cref="TypeInstancesFactoryManager{TClass, TParam}"/>를 사용하고싶을때 유용하다
        /// </summary>
        protected virtual void LateWakeUp_SetAdvancedSkins()
        {
            if (this is TSkelSbject tSkelSbject)
            {
                AdvancedSkins = TypeInstancesFactoryManager<TAdvancedSkin, TSkelSbject>.Create(tSkelSbject);
            }
            else
            {
                throw new Exception($"{this.name}의 타입캐스팅 실패 {typeof(TSkelSbject).Name}");
            }
        }
    }



    ///======================================================================================================================================================
}
