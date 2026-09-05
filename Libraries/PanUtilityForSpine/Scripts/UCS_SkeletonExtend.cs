using UnityEngine;
using Spine;
using System.Collections.Generic;



//? 스파인 스켈레톤을 확장할수있는 요소가 정리되어있는 정도의 코드



namespace Pan.SpineUtil
{
    /// <summary>
    /// <see cref="Spine.Animation"/>을 확장하는 구조체
    /// </summary>
    public readonly struct AnimationEX
    {
        ///======================================================================================================================================================

        public AnimationEX(Spine.Animation animation)
        {
            Animation = animation;
            Animation.GetEventDictionaryFromAnimation(out SpineEvents);
        }

        public static implicit operator Spine.Animation(AnimationEX value) => value.Animation;

        ///======================================================================================================================================================

        public readonly Spine.Animation Animation;

        public readonly Dictionary<string, int> SpineEvents;

        ///======================================================================================================================================================
    }



    /// <summary>
    /// <see cref="Spine.BoneData"/>을 확장하는 구조체<br/>
    /// (Index 추가)
    /// </summary>
    public readonly struct BoneDataEX
    {
        ///======================================================================================================================================================

        public BoneDataEX(BoneData boneData, int index, Skin[] skinRequireds)
        {
            BoneData = boneData;
            Index = index;
            SkinRequireds = skinRequireds;
            BoneFullPath = BoneData.GetBonePath();
        }

        public BoneDataEX(BoneData boneData, int index)
        {
            BoneData = boneData;
            Index = index;
            SkinRequireds = null;
            BoneFullPath = BoneData.GetBonePath();
        }

        public static implicit operator BoneData(BoneDataEX value) => value.BoneData;

        ///======================================================================================================================================================

        public readonly BoneData BoneData;

        public readonly int Index;

        public readonly Skin[] SkinRequireds;

        public readonly string BoneFullPath;

        ///======================================================================================================================================================

        public string Name => BoneData.Name;

        public bool UseSkinRequired => SkinRequireds != null && SkinRequireds.Length != 0;

        ///======================================================================================================================================================
    }



    /// <summary>
    /// <see cref="Spine.TransformConstraintData"/>을 확장하는 구조체<br/>
    /// (Index 추가)
    /// </summary>
    public readonly struct TransformConstraintDataEX
    {
        ///======================================================================================================================================================

        public TransformConstraintDataEX(TransformConstraintData transformConstraintData, int index, Skin[] skinRequireds)
        {
            TransformConstraintData = transformConstraintData;
            Index = index;
            SkinRequireds = skinRequireds;
        }

        public TransformConstraintDataEX(TransformConstraintData transformConstraintData, int index)
        {
            TransformConstraintData = transformConstraintData;
            Index = index;
            SkinRequireds = null;
        }

        public static implicit operator TransformConstraintData(TransformConstraintDataEX value) => value.TransformConstraintData;

        ///======================================================================================================================================================

        public readonly TransformConstraintData TransformConstraintData;

        public readonly int Index;

        public readonly Skin[] SkinRequireds;

        ///======================================================================================================================================================

        public string Name => TransformConstraintData.Name;

        public bool UseSkinRequired => SkinRequireds != null && SkinRequireds.Length != 0;

        ///======================================================================================================================================================
    }



    /// <summary>
    /// <see cref="Spine.IkConstraintData"/>을 확장하는 구조체<br/>
    /// (Index 추가)
    /// </summary>
    public readonly struct IkConstraintDataEX
    {
        ///======================================================================================================================================================

        public IkConstraintDataEX(IkConstraintData ikConstraintData, int index, Skin[] skinRequireds)
        {
            IkConstraintData = ikConstraintData;
            Index = index;
            SkinRequireds = skinRequireds;
        }

        public IkConstraintDataEX(IkConstraintData ikConstraintData, int index)
        {
            IkConstraintData = ikConstraintData;
            Index = index;
            SkinRequireds = null;
        }

        public static implicit operator IkConstraintData(IkConstraintDataEX value) => value.IkConstraintData;

        ///======================================================================================================================================================

        public readonly IkConstraintData IkConstraintData;

        public readonly int Index;

        public readonly Skin[] SkinRequireds;

        ///======================================================================================================================================================

        public string Name => IkConstraintData.Name;

        public bool UseSkinRequired => SkinRequireds != null && SkinRequireds.Length != 0;

        ///======================================================================================================================================================
    }



    /// <summary>
    /// <see cref="Spine.PathConstraintData"/>을 확장하는 구조체<br/>
    /// (Index 추가)
    /// </summary>
    public readonly struct PathConstraintDataEX
    {
        ///======================================================================================================================================================

        public PathConstraintDataEX(PathConstraintData pathConstraintData, int index, Skin[] skinRequireds)
        {
            PathConstraintData = pathConstraintData;
            Index = index;
            SkinRequireds = skinRequireds;
        }

        public PathConstraintDataEX(PathConstraintData pathConstraintData, int index)
        {
            PathConstraintData = pathConstraintData;
            Index = index;
            SkinRequireds = null;
        }

        public static implicit operator PathConstraintData(PathConstraintDataEX value) => value.PathConstraintData;

        ///======================================================================================================================================================

        public readonly PathConstraintData PathConstraintData;

        public readonly int Index;

        public readonly Skin[] SkinRequireds;

        ///======================================================================================================================================================

        public string Name => PathConstraintData.Name;

        public bool UseSkinRequired => SkinRequireds != null && SkinRequireds.Length != 0;

        ///======================================================================================================================================================
    }



    /// <summary>
    /// <see cref="Spine.PhysicsConstraintData"/>을 확장하는 구조체<br/>
    /// (Index 추가)
    /// </summary>
    public readonly struct PhysicsConstraintDataEX
    {
        ///======================================================================================================================================================

        public PhysicsConstraintDataEX(PhysicsConstraintData physicsConstraintData, int index, Skin[] skinRequireds)
        {
            PhysicsConstraintData = physicsConstraintData;
            Index = index;
            SkinRequireds = skinRequireds;
        }

        public PhysicsConstraintDataEX(PhysicsConstraintData physicsConstraintData, int index)
        {
            PhysicsConstraintData = physicsConstraintData;
            Index = index;
            SkinRequireds = null;
        }

        public static implicit operator PhysicsConstraintData(PhysicsConstraintDataEX value) => value.PhysicsConstraintData;

        ///======================================================================================================================================================

        public readonly PhysicsConstraintData PhysicsConstraintData;

        public readonly int Index;

        public readonly Skin[] SkinRequireds;

        ///======================================================================================================================================================

        public string Name => PhysicsConstraintData.Name;

        public bool UseSkinRequired => SkinRequireds != null && SkinRequireds.Length != 0;

        ///======================================================================================================================================================
    }
}