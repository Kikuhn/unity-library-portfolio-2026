using UnityEngine;
using Spine;



//? 스파인 스켈레톤으로부터 값을 얻을수 있게 하는 코드가 정리되어있는 정도의 코드



namespace Pan.SpineUtil
{
    ///======================================================================================================================================================



    //? 스파인-스켈레톤 값 얻기 인터페이스



    /// <summary>
    /// <see cref="Spine.Skin"/> 얻기가 가능한 인터페이스
    /// </summary>
    public interface ICanGetSkin
    {
        Skin GetSkin(string skinName);
    }



    /// <summary>
    /// <see cref="Spine.Animation"/> 얻기가 가능한 인터페이스
    /// </summary>
    public interface ICanGetAnimation
    {
        Spine.Animation GetAnimation(string animationName);
    }



    /// <summary>
    /// <see cref="Spine.SlotData"/> 얻기가 가능한 인터페이스
    /// </summary>
    public interface ICanGetSlotData
    {
        SlotData GetSlotData(string slotName);

        SlotData GetSlotData(int slotIndex);
    }



    /// <summary>
    /// <see cref="Spine.BoneData"/> 얻기가 가능한 인터페이스
    /// </summary>
    public interface ICanGetBoneData
    {
        BoneData GetBoneData(string boneName);

        BoneData GetBoneData(int boneIndex);
    }



    /// <summary>
    /// <see cref="BoneDataEX"/> 얻기가 가능한 인터페이스
    /// </summary>
    public interface ICanGetBoneDataEX
    {
        BoneDataEX? GetBoneDataEX(string boneName);

        BoneDataEX? GetBoneDataEX(int boneIndex);
    }



    /// <summary>
    /// <see cref="TransformConstraintData"/> 얻기가 가능한 인터페이스
    /// </summary>
    public interface ICanGetTransformConstraintData
    {
        TransformConstraintData GetTransformConstraintData(string transformConstraintDataName);

        TransformConstraintData GetTransformConstraintData(int transformConstraintDataIndex);
    }



    /// <summary>
    /// <see cref="TransformConstraintDataEX"/> 얻기가 가능한 인터페이스
    /// </summary>
    public interface ICanGetTransformConstraintDataEX
    {
        TransformConstraintDataEX? GetTransformConstraintDataEX(string transformConstraintDataName);

        TransformConstraintDataEX? GetTransformConstraintDataEX(int transformConstraintDataIndex);
    }



    ///======================================================================================================================================================
}