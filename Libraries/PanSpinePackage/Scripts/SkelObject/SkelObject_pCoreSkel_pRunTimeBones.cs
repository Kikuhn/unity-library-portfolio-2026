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



namespace Pan.SpinePackage
{
    public partial class SkelObject
    {
        public partial class SkelCore : CoreBase
        {
            /// <summary>
            /// <see cref="UpdateRunTimeBone"/>만 존재하는 인터페이스
            /// </summary>
            public interface IRunTimeBone
            {
                void UpdateRunTimeBone();
            }



            /// <summary>
            /// <see cref="Bone"/>, <see cref="RunTimeBone"/> 을 얻을 수 있는 인터페이스
            /// <para> <see cref="RunTimeBoneManager"/>, <see cref="SkelCore"/>, <see cref="SkelObject"/> 에 추가하여, 쉽게 인터페이스를 활용할수 있게 설계</para>
            /// </summary>
            public interface IGetBonesRunTime
            {

                ///<summary>
                /// <see cref="RunTimeBone"/>을 얻는다
                ///</summary>
                RunTimeBone GetRunTimeBone(string boneName);

                ///<summary>
                /// <see cref="RunTimeBone"/>을 얻는다
                ///</summary>
                public RunTimeBone GetRunTimeBone(Bone baseBone)
                {
                    return GetRunTimeBone(baseBone.Data.Name);
                }

                ///<summary>
                /// <see cref="Bone"/>을 얻는다
                ///</summary>
                Bone GetBone(string boneName);
            }



            /// <summary>
            /// 런타임에 사용되는 Bone
            /// <para>애니메이션을 무시하고 <see cref="Bone"/>을 재정의 할 수 있는</para>
            /// <para>Override 기능들이 들어가있다</para>
            /// </summary>
            public class RunTimeBone : IRunTimeBone
            {
                ///======================================================================================================================================================



                public RunTimeBone(Bone baseBone, RunTimeBoneManager runtimeBoneManager)
                {
                    BaseBone = baseBone;
                    RunTimeBoneManager = runtimeBoneManager;
                }



                ///======================================================================================================================================================



                ///<summary>
                /// 원본 <see cref="Bone"/>
                /// </summary>
                public readonly Bone BaseBone;



                /// <summary>
                /// 원본 <see cref="Bone"/>의 이름
                /// </summary>
                public string Name => BaseBone.Data.Name;



                ///======================================================================================================================================================



                private readonly RunTimeBoneManager RunTimeBoneManager;



                ///======================================================================================================================================================



                //? 회전 정의 / 재정의



                ///<summary>
                ///<see cref="Bone"/>의 회전값 얻기
                /// </summary>
                public float Rotation => BaseBone.Pose.Rotation;



                ///<summary>
                ///<see cref="Bone"/>의 회전값 을 <b>Override</b> 할지 여부
                ///<para>직접 지정하지 않아도, <see cref="Rotation_Override"/>를 setter할때, true가 할당된다</para>
                /// </summary>
                public bool UseOverride_Rotation
                {
                    get => useOverride_Rotation; private set
                    {
                        if (useOverride_Rotation == false && value == true)
                        {
                            RunTimeBoneManager.AddOverridden_RunTimeBoneList(this);
                        }
                        useOverride_Rotation = value;
                    }
                }
                private bool useOverride_Rotation;



                ///<summary>
                ///<see cref="Bone"/>의 회전값 을 <b>Override</b> 할 값
                ///<para><see cref="UseOverride_Rotation"/>를 따로 지정하지 않아도, setter 프로퍼티에서 <see cref="UseOverride_Rotation"/>에 true가 할당된다</para>
                /// </summary>
                public float Rotation_Override
                {
                    get => rotation_Override;
                    set
                    {
                        UseOverride_Rotation = true;
                        rotation_Override = value;
                    }
                }
                private float rotation_Override;



                ///<summary>
                ///<see cref="Bone"/>의 회전값 을 <b>조작</b>한다
                ///<para><paramref name="useOverride"/>에 따라서, 애니메이션에서 지정되는 회전값 을 무시하고 지정할지 결정 할 수 있다</para>
                /// </summary>
                public float SetRotation(float rotation, bool useOverride)
                {
                    if (useOverride) { Rotation_Override = rotation; }
                    else { BaseBone.Pose.Rotation = rotation; }


                    return rotation;
                }



                ///======================================================================================================================================================



                //? 위치좌표 정의 / 재정의



                ///<summary>
                ///<see cref="Bone"/>의 위치좌표값 얻기
                /// </summary>
                public Vector2 PositionSkeletonSpace() => new Vector2(BaseBone.Pose.X, BaseBone.Pose.Y);



                ///<summary>
                ///<see cref="Bone"/>의 위치좌표값 을 <b>Override</b> 할지 여부
                ///<para>직접 지정하지 않아도, <see cref="PositionSkeletonSpace_Override"/>를 setter할때, true가 할당된다</para>
                /// </summary>
                public bool UseOverride_PositionSkeletonSpace
                {
                    get => useOverride_PositionSkeletonSpace; private set
                    {
                        if (useOverride_PositionSkeletonSpace == false && value == true)
                        {
                            RunTimeBoneManager.AddOverridden_RunTimeBoneList(this);
                        }
                        useOverride_PositionSkeletonSpace = value;
                    }
                }
                private bool useOverride_PositionSkeletonSpace;



                ///<summary>
                ///<see cref="Bone"/>의 위치좌표값 을 <b>Override</b> 할 값
                ///<para><see cref="UseOverride_PositionSkeletonSpace"/>를 따로 지정하지 않아도, setter 프로퍼티에서 <see cref="UseOverride_PositionSkeletonSpace"/>에 true가 할당된다</para>
                /// </summary>
                public Vector2 PositionSkeletonSpace_Override
                {
                    get => positionSkeletonSpace_Override;
                    set
                    {
                        UseOverride_PositionSkeletonSpace = true;
                        positionSkeletonSpace_Override = value;
                    }
                }
                private Vector2 positionSkeletonSpace_Override;



                ///<summary>
                ///<see cref="Bone"/>의 위치좌표값 을 <b>조작</b>한다
                ///<para><paramref name="useOverride"/>에 따라서, 애니메이션에서 지정되는 위치좌표값 을 무시하고 지정할지 결정 할 수 있다</para>
                /// </summary>
                public Vector2 SetSkeletonSpacePositionOverride(Vector2 positionSkeletonSpace, bool @override)
                {
                    if (@override)
                    {
                        PositionSkeletonSpace_Override = positionSkeletonSpace;
                    }
                    else
                    {
                        BaseBone.Pose.SetPosition(positionSkeletonSpace.x, positionSkeletonSpace.y);
                    }

                    return positionSkeletonSpace;
                }



                ///======================================================================================================================================================



                //? 업데이트



                ///<summary>
                /// 이 <see cref="RunTimeBone"/>을 Update한다
                /// </summary>
                public void UpdateRunTimeBone()
                {
                    //. 뼈의 회전값이 Override되어있다면, 원본 뼈의 회전값에 Override된 값을 지금 적용한다
                    if (UseOverride_Rotation)
                    {
                        BaseBone.Pose.Rotation = Rotation_Override;
                        UseOverride_Rotation = false;
                    }

                    //. 뼈의 위치좌표값이 Override되어있다면, 원본 뼈의 위치좌표값에 Override된 값을 지금 적용한다
                    if (UseOverride_PositionSkeletonSpace)
                    {
                        BaseBone.Pose.SetPosition(PositionSkeletonSpace_Override.x, PositionSkeletonSpace_Override.y);
                        UseOverride_PositionSkeletonSpace = false;
                    }
                }



                ///======================================================================================================================================================
            }



            /// <summary>
            /// 런타임에 사용되는 Bone을 관리하는 매니저
            /// </summary>
            [Serializable]
            public class RunTimeBoneManager : Base, IGetBonesRunTime
            {
                ///======================================================================================================================================================



                public RunTimeBoneManager(SkelObject skelObject, SkelSbject skelSbject) : base(skelObject)
                {
                    //? RunTimeBone 초기화
                    RunTimeBones = new Dictionary<string, RunTimeBone>(skelSbject.BoneDataEX_Dictionary.Count);
                    Overridden_RunTimeBoneList_ThisFrame = new List<IRunTimeBone>(skelSbject.BoneDataEX_Dictionary.Count / 4); //. Bone 크기의 1/4 만큼 Capacity를 지정
                    for (int i = 0; i < SkelCore.SkelAnimation.Skeleton.Bones.Count; i++)
                    {
                        var bone = SkelCore.SkelAnimation.Skeleton.Bones.Items[i];
                        RunTimeBones.Add(bone.Data.Name, new RunTimeBone(bone, this));
                    }
                    RunTimeBones.TrimExcess();
                }



                ///======================================================================================================================================================



                ///<summary>
                /// <see cref="RunTimeBone"/> 타입으로 확장되어 관리되는 <see cref="Bone"/>의 딕셔너리
                /// </summary>
                private readonly Dictionary<string, RunTimeBone> RunTimeBones;



                ///<summary>
                /// <see cref="RunTimeBone"/> 타입으로 확장되어 관리되는 <see cref="Bone"/>의 딕셔너리 얻기
                /// </summary>
                public IReadOnlyDictionary<string, RunTimeBone> GetRunTimeBones => RunTimeBones;



                ///<summary>
                /// 이번 프레임에서, <see cref="RunTimeBone"/>가 이번 프레임에 Override되어,
                /// <para>그 값을 적용할 필요가 있을 때 이 리스트에 추가된다</para>
                /// </summary>
                private readonly List<IRunTimeBone> Overridden_RunTimeBoneList_ThisFrame;



                ///======================================================================================================================================================



                /// <summary>
                /// <see cref="Overridden_RunTimeBoneList_ThisFrame"/> 에 <see cref="IRunTimeBone"/> 추가
                /// </summary>
                /// <param name="runtimeBone"></param>
                public void AddOverridden_RunTimeBoneList(IRunTimeBone runtimeBone)
                {
                    Overridden_RunTimeBoneList_ThisFrame.Add(runtimeBone);
                }



                public bool HasOverriddenRunTimeBones => Overridden_RunTimeBoneList_ThisFrame.Count != 0;



                ///======================================================================================================================================================



                //? 런타임 Bone 얻기



                ///<summary>
                /// <see cref="RunTimeBone"/>을 얻는다
                ///</summary>
                public RunTimeBone GetRunTimeBone(string boneName)
                {
                    return RunTimeBones[boneName];
                }



                ///<summary>
                /// <see cref="RunTimeBone"/>을 얻어본다
                ///</summary>
                public bool TryGetRunTimeBone(string boneName, out RunTimeBone resultRunTimeBone)
                {
                    return RunTimeBones.TryGetValue(boneName, out resultRunTimeBone);
                }



                ///<summary>
                /// <see cref="RunTimeBone"/>을 얻는다
                ///</summary>
                public RunTimeBone GetRunTimeBone(Bone baseBone)
                {
                    return GetRunTimeBone(baseBone.Data.Name);
                }



                ///<summary>
                /// <see cref="RunTimeBone"/>을 얻어본다
                ///</summary>
                public bool TryGetRunTimeBone(Bone baseBone, out RunTimeBone resultRunTimeBone)
                {
                    return TryGetRunTimeBone(baseBone.Data.Name, out resultRunTimeBone);
                }



                ///<summary>
                /// <see cref="Bone"/>을 얻는다
                ///</summary>
                public Bone GetBone(string boneName)
                {
                    return GetRunTimeBone(boneName).BaseBone;
                }



                ///<summary>
                /// <see cref="Bone"/>을 얻어본다
                ///</summary>
                public bool TryGetBone(string boneName, out Bone resultBone)
                {
                    resultBone = TryGetRunTimeBone(boneName, out var runtimeBone) ? runtimeBone.BaseBone : null;
                    return resultBone != null;
                }



                ///======================================================================================================================================================



                //? Update



                /// <summary>
                /// 런타임 Bone을 업데이트한다
                /// <para>이번 프레임에 Override된 <see cref="Bone"/>이 있을 경우에만 실행된다</para>
                /// <para><see cref="Overridden_RunTimeBoneList_ThisFrame"/>를 순회하면서 <see cref="IRunTimeBone.UpdateRunTimeBone"/>를 실행시켜 적용한뒤,</para>
                /// <para>순회가 종료되면 <see cref="Overridden_RunTimeBoneList_ThisFrame"/>를 Clear 한다</para>
                /// </summary>
                public bool Update_RunTimeBones()
                {
                    //. Override된 Bone이 전혀 없다면, 그냥 return
                    if (Overridden_RunTimeBoneList_ThisFrame.Count == 0) { return false; }

                    for (int i = 0; i < Overridden_RunTimeBoneList_ThisFrame.Count; i++)
                    {
                        Overridden_RunTimeBoneList_ThisFrame[i].UpdateRunTimeBone();
                    }

                    Overridden_RunTimeBoneList_ThisFrame.Clear();
                    return true;

                    //GetSkeleton().UpdateWorldTransform(Skeleton.Physics.Update); //! 240103 스파인 4.1 -> .4.2 버전으로 업데이트
                }



                ///======================================================================================================================================================



                //? 리셋


                ///<summary>
                /// 런타임 본 매니저 초기화
                ///<para><see cref="Overridden_RunTimeBoneList_ThisFrame"/>만 Clear 된다</para>
                /// </summary>
                public void Reset()
                {
                    Overridden_RunTimeBoneList_ThisFrame.Clear();
                }



                ///======================================================================================================================================================
            }
        }
    }
}
