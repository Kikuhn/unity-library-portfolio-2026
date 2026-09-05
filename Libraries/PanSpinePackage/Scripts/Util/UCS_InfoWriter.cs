using Pan.SpineUtil;
using Pan.Util;
using Spine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;



namespace Pan.SpinePackage
{
    /// <summary>
    /// 스파인 패키지에서, 각종 정보들을 출력할때 가시성 좋게 정리하는 전역 유틸리티 클래스
    /// </summary>
    public static class SU_InfoWriter
    {
        ///======================================================================================================================================================



        //? SkelAni Value



        /// <summary>
        /// <see cref="SkelAni"/>의 정보를 <see cref="StringBuilder"/>에 출력한다
        /// </summary>
        /// <param name="targetStringBuilder">대상 StringBuilder</param>
        /// <param name="skelAni">대상 <see cref="SkelAni"/></param>
        /// <param name="skelSbject">null을 받아올시, Enum의 정보를 변환하여 출력하지 못한다</param>
        public static void WriteSkelAniValueInfo(StringBuilder targetStringBuilder, SkelAni skelAni, SkelSbject skelSbject)
        {
            void AppendFormatted(string name, string value, bool appendLine)
            {
                targetStringBuilder.Append($"{name}: {value}");
                if (appendLine) targetStringBuilder.AppendLine();
                else targetStringBuilder.Append(" | ");
            }

            if (skelAni == null) { return; }

            //? TrackIndex
            if (skelSbject != null) AppendFormatted("📼Track", $"<color=#ac92ec>{skelSbject.EnumIndex_AniTracks.GetEnumName(skelAni.TrackIndex)}</color> (<i><color=#2ecc71>{skelAni.TrackIndex}</color></i>)", false);
            else AppendFormatted($"📼{nameof(skelAni.TrackIndex)}", $"<color=#2ecc71>{skelAni.TrackIndex}</color>", false);


            //? RankType
            if (skelSbject != null) AppendFormatted("👑Rank", $"<color=#ac92ec>{skelSbject.EnumIndex_AniRanks.GetEnumName(skelAni.RankType)}</color> (<i><color=#2ecc71>{skelAni.RankType}</color></i>)", true);
            else AppendFormatted($"👑{nameof(skelAni.RankType)}", $"<color=#2ecc71>{skelAni.RankType}</color>", true);
            targetStringBuilder.AppendLine();


            //? Animation
            AppendFormatted($"🏃{nameof(skelAni.Animation)}", $"<b><color=#f7da64>{skelAni.Animation}</color></b>", true);
            targetStringBuilder.AppendLine();


            //? Loop
            AppendFormatted($"🔁{nameof(skelAni.Loop)}", skelAni.Loop ? $"<color=#4fc1e9>{skelAni.Loop}</color>" : $"<color=#ed5565>{skelAni.Loop}</color>", false);


            //? Overlap
            AppendFormatted($"📚{nameof(skelAni.Overlap)}", skelAni.Overlap ? $"<color=#4fc1e9>{skelAni.Overlap}</color>" : $"<color=#ed5565>{skelAni.Overlap}</color>", true);
            targetStringBuilder.AppendLine();


            //? AniTimeType
            if (skelSbject != null) AppendFormatted($"⌚{nameof(skelAni.AniTimeType)}", $"<color=#ac92ec>{skelSbject.EnumIndex_AniTimes.GetEnumName(skelAni.AniTimeType)}</color> (<i><color=#2ecc71>{skelAni.AniTimeType}</color></i>)", true);
            else AppendFormatted($"⌚{nameof(skelAni.AniTimeType)}", $"<i><color=#2ecc71>{skelAni.AniTimeType}</color></i>", true);


            //? MixDuration
            AppendFormatted($"⏩{nameof(skelAni.MixDuration)}", $"<color=#2ecc71>{skelAni.MixDuration}</color>", false);


            //? EndMixDuration
            AppendFormatted($"⏭️{nameof(skelAni.EndMixDuration)}", $"<color=#2ecc71>{skelAni.EndMixDuration}</color>", false);


            //? BonusSpeed
            AppendFormatted($"🔥{nameof(skelAni.BonusSpeed)}", $"<color=#2ecc71>{skelAni.BonusSpeed}</color>", false);
            targetStringBuilder.Remove(targetStringBuilder.Length - 3, 3); //. 3글자만 지워서 " | " 지우기
        }



        /// <summary>
        /// <see cref="SkelAni"/>의 정보를 <see cref="StringBuilder"/>에 출력한다
        /// <para><see cref="SkelAni"/>에 지정된 이름도 받아와, 제목처럼 출력한다</para>
        /// </summary>
        /// <param name="targetStringBuilder">대상 StringBuilder</param>
        /// <param name="skelAni">대상 <see cref="SkelAni"/></param>
        /// <param name="skelAniValueName">SkelAni의 이름</param>
        /// <param name="skelSbject">null을 받아올시, Enum의 정보를 변환하여 출력하지 못한다</param>
        public static void WriteSkelAniValueInfo(StringBuilder targetStringBuilder, SkelAni skelAni, string skelAniValueName, SkelSbject skelSbject)
        {
            targetStringBuilder.AppendLine($"🅰️ <b>{skelAniValueName}</b>");
            targetStringBuilder.AppendLine();
            WriteSkelAniValueInfo(targetStringBuilder, skelAni, skelSbject);
        }



        ///======================================================================================================================================================



        //? Bone (Name) Value



        /// <summary>
        /// Bone의 정보를 <see cref="StringBuilder"/>에 출력한다
        /// </summary>
        /// <param name="targetStringBuilder">대상 StringBuilder</param>
        /// <param name="boneName">본 이름</param>
        /// <param name="boneValueName">본 변수 이름</param>
        public static void WriteBoneValueInfo(StringBuilder targetStringBuilder, string boneName, string boneValueName = null)
        {
            if (boneValueName != null) targetStringBuilder.AppendLine($"🦴 {boneValueName} (<color=#f7da64><b>{boneName}</b></color>)");
            else targetStringBuilder.AppendLine($"🦴 <color=#f7da64><b>{boneName}</b></color>");
        }



        ///======================================================================================================================================================



        //? ExtendSkin Value



        /// <summary>
        /// <see cref="ExtendSkin"/>의 정보를 <see cref="StringBuilder"/>에 출력한다
        /// </summary>
        /// <param name="targetStringBuilder">대상 StringBuilder</param>
        /// <param name="useDetail">심화 표시 여부</param>
        /// <param name="extendSkin">확장 스킨</param>
        /// <param name="extendSkinValueName">확장 스킨 변수 이름</param>
        /// <param name="skelSbject">스킨레이어 Enum 변환용 <see cref="SkelSbject"/></param>
        public static void WriteExtendSkinValueInfo(StringBuilder targetStringBuilder, bool useDetail, ExtendSkin extendSkin, string extendSkinValueName, SkelSbject skelSbject)
        {
            if (extendSkin == null) { return; }

            switch (extendSkin)
            {
                case ExtendSkin.Eyes: targetStringBuilder.Append($"👁️ <b>{extendSkinValueName}</b>"); break;
                case ExtendSkin.Multiple: targetStringBuilder.Append($"🧥 <b>{extendSkinValueName}</b>"); break;
                default: targetStringBuilder.Append($"🛍️ <b>{extendSkinValueName}</b>"); break;
            }
            if (skelSbject != null) targetStringBuilder.AppendLine($" | 👑 LayerRank: <color=#ac92ec>{skelSbject.EnumIndex_SkinLayers.GetEnumName(extendSkin.LayerRank)}</color> (<color=#2ecc71><b>{extendSkin.LayerRank}</b></color>)");
            else targetStringBuilder.AppendLine($" | 👑 LayerRank: <color=#2ecc71><b>{extendSkin.LayerRank}</b></color>");


            if (useDetail)
            {
                targetStringBuilder.AppendLine($"   👕 {nameof(ExtendSkin)} 이름: <color=#f7da64>{extendSkin.Name}</color>");
                targetStringBuilder.AppendLine($"   👕 {nameof(Spine.Skin)} 이름: <color=#f7da64>{extendSkin.MainSkin.Name}</color>");
                targetStringBuilder.AppendLine();
                if (extendSkin.SkinEntrys != null && extendSkin.SkinEntrys.Length != 0)
                {
                    targetStringBuilder.AppendLine($"\t⚪ SkinEntry Count: <color=#2ecc71><b>{extendSkin.SkinEntrys.Length}</b></color>");
                    for (int i = 0; i < extendSkin.SkinEntrys.Length; i++)
                    {
                        var skinEntry = extendSkin.SkinEntrys[i];
                        targetStringBuilder.AppendLine($"\t   ⚪ <color=#f7da64>{skinEntry.Placeholder}</color>");
                    }
                    targetStringBuilder.AppendLine();
                }


                if (extendSkin.BoneDatas != null && extendSkin.BoneDatas.Length != 0)
                {
                    targetStringBuilder.AppendLine($"\t🦴 Bone Count: <color=#2ecc71><b>{extendSkin.BoneDatas.Length}</b></color>");
                    for (int i = 0; i < extendSkin.BoneDatas.Length; i++)
                    {
                        var bone = extendSkin.BoneDatas[i];
                        targetStringBuilder.AppendLine($"\t   🦴 Bone: <color=#f7da64>{bone.Name}</color>");
                    }
                    targetStringBuilder.AppendLine();
                }


                if (extendSkin.ConstraintDatas != null && extendSkin.ConstraintDatas.Length != 0)
                {
                    targetStringBuilder.AppendLine($"\t⛓️ Constraint Count: <color=#2ecc71><b>{extendSkin.ConstraintDatas.Length}</b></color>");
                    for (int i = 0; i < extendSkin.ConstraintDatas.Length; i++)
                    {
                        var constraint = extendSkin.ConstraintDatas[i];
                        targetStringBuilder.AppendLine($"\t   ⛓️ Constraint: <color=#f7da64>{constraint.Name}</color>");
                    }
                    targetStringBuilder.AppendLine();
                }
            }
        }



        ///======================================================================================================================================================



        //? SpineEvent Value



        /// <summary>
        /// <see cref="Spine.EventData"/>(이름)의 정보를 <see cref="StringBuilder"/>에 출력한다
        /// </summary>
        /// <param name="targetStringBuilder">대상 StringBuilder</param>
        /// <param name="spineEvent">스파인 이벤트 이름</param>
        /// <param name="spineEventValueName">스파인 이벤트  변수 이름</param>
        public static void WriteSpineEventValueInfo(StringBuilder targetStringBuilder, string spineEvent, string spineEventValueName)
        {
            targetStringBuilder.AppendLine($"🍆 <b>{spineEventValueName}</b>");
            targetStringBuilder.AppendLine($"<color=#f7da64>{spineEvent.WrapTargetSubstring("/", "<color=#ed5565><b>", "</b></color>")}</color>");
        }



        ///======================================================================================================================================================



        //? InteractionBox


        /// <summary>
        /// <see cref="SkelSbject.BaseInteractionBox"/>의 정보를 <see cref="StringBuilder"/>에 출력한다
        /// </summary>
        /// <param name="targetStringBuilder">대상 StringBuilder</param>
        public static void WriteInteractionBoxInfo(StringBuilder targetStringBuilder, SkelSbject.IHoldInteractionBox holdInteractionBox)
        {
            if (holdInteractionBox == null) { return; }

            targetStringBuilder.AppendLine($"📜 보유중인 애니메이션 상호작용 매니저 클래스");
            for (int i = 0; i < holdInteractionBox.InteractionBox.GetInteractionManagers.Length; i++)
            {
                var interactionManager = holdInteractionBox.InteractionBox.GetInteractionManagers[i];
                targetStringBuilder.Append($"\t<color=#f7da64>{interactionManager.GetType().Name}</color>");

                if (i < holdInteractionBox.InteractionBox.GetInteractionManagers.Length - 1) { targetStringBuilder.AppendLine(","); }
                else { targetStringBuilder.AppendLine(); }
            }
        }




        ///======================================================================================================================================================



        //? DrawOrderBox



        /// <summary>
        /// <see cref="SkelSbject.IHoldDrawOrderBox"/>의 정보를 <see cref="StringBuilder"/>에 출력한다
        /// </summary>
        /// <param name="targetStringBuilder">대상 StringBuilder</param>
        public static void WriteInteractionBoxInfo(StringBuilder targetStringBuilder, string baseDrawOrderSetValueName, SkelSbject.BaseDrawOrderSet baseDrawOrderSet)
        {
            targetStringBuilder.AppendLine($"🖼️ <b>{baseDrawOrderSetValueName}</b>");
        }




        ///======================================================================================================================================================



        //? 인터페이스



        /// <summary>
        /// <see cref="SkelSbject.IHoldSomethingBase"/>의 인터페이스 보유 정보를 출력한다
        /// </summary>
        /// <param name="targetStringBuilder"></param>
        /// <param name="holdTarget"></param>
        /// <param name="holdTargetName"></param>
        public static bool WriteSkelSbjectHoldSomethingInterfaceInfo(StringBuilder targetStringBuilder, object holdTarget, string holdTargetName)
        {
            if (holdTarget is SkelSbject.IHoldSomethingBase)
            {
                //? 애니 베이스 보유중
                if (holdTarget is SkelSbject.IHoldAniBase)
                {
                    var types = SU_Reflection.GetImplementedInterfacesOfType<SkelSbject.IHoldAniBase>(holdTarget).ToArray();

                    targetStringBuilder.AppendLine($"⚡🅰️ <b>{holdTargetName}</b>: <color=#f7da64>{nameof(SkelSbject.IHoldAniBase)}</color> (<color=#2ecc71><b><i>{types.Count()}</i></b></color>)");

                    for (int i = 0; i < types.Length; i++)
                    {
                        var type = types[i];
                        if (type == typeof(SkelSbject.IHoldAniBase)) { continue; }

                        targetStringBuilder.Append($"\t<color=#f7da64>{type.Name}</color>");
                        if (i < types.Length - 1) { targetStringBuilder.AppendLine(","); }
                        else { targetStringBuilder.AppendLine(); }

                    }

                    targetStringBuilder.Remove(targetStringBuilder.Length - 3, 3);
                }


                //? 본 베이스 보유중
                if (holdTarget is SkelSbject.IHoldBoneBase)
                {
                    var types = SU_Reflection.GetImplementedInterfacesOfType<SkelSbject.IHoldBoneBase>(holdTarget).ToArray();

                    targetStringBuilder.AppendLine($"⚡🦴 <b>{holdTargetName}</b>: <color=#f7da64>{nameof(SkelSbject.IHoldBoneBase)}</color> (<color=#2ecc71><b><i>{types.Count()}</i></b></color>)");

                    for (int i = 0; i < types.Length; i++)
                    {
                        System.Type type = types[i];
                        if (type == typeof(SkelSbject.IHoldBoneBase)) { continue; }

                        targetStringBuilder.Append($"\t<color=#f7da64>{type.Name}</color>");
                        if (i < types.Length - 1) { targetStringBuilder.AppendLine(","); }
                        else { targetStringBuilder.AppendLine(); }
                    }
                    targetStringBuilder.Remove(targetStringBuilder.Length - 3, 3);
                }


                //? 스킨어태치 베이스 보유중
                if (holdTarget is SkelSbject.IHoldSkinAtchBox)
                {
                    var types = SU_Reflection.GetImplementedInterfacesOfType<SkelSbject.IHoldSkinAtchBox>(holdTarget).ToArray();

                    targetStringBuilder.AppendLine($"⚡🛍️ <b>{holdTargetName}</b>: <color=#f7da64>{nameof(SkelSbject.IHoldSkinAtchBox)}</color> (<color=#2ecc71><b><i>{types.Count()}</i></b></color>)");

                    for (int i = 0; i < types.Length; i++)
                    {
                        System.Type type = types[i];
                        if (type == typeof(SkelSbject.IHoldSkinAtchBox)) { continue; }

                        targetStringBuilder.Append($"\t<color=#f7da64>{type.Name}</color>");
                        if (i < types.Length - 1) { targetStringBuilder.AppendLine(","); }
                        else { targetStringBuilder.AppendLine(); }
                    }
                    targetStringBuilder.Remove(targetStringBuilder.Length - 3, 3);
                }


                return true;
            }


            else
            {
                targetStringBuilder.AppendLine($"⚡ <b>{holdTargetName}</b>: <color=#f7da64>{nameof(SkelSbject.IHoldSomethingBase)}</color> 를 보유하고 있지 않음");
                return false;
            }
        }



        ///======================================================================================================================================================



        //? 열거형 인덱스



        /// <summary>
        /// 열거형 인덱스 - 스킨레이어 출력하기
        /// </summary>
        /// <param name="targetStringBuilder"></param>
        public static void WriteEnumIndexes_SkinLayerInfo(StringBuilder targetStringBuilder, int index, string skinLayerEnumName)
        {
            targetStringBuilder.Append($"👑 <color=#2ecc71><b>{index:00}</b></color>\t<color=#ac92ec>{skinLayerEnumName}</color>");
        }



        /// <summary>
        /// 열거형 인덱스 - 애니메이션 트랙 출력하기
        /// </summary>
        /// <param name="targetStringBuilder"></param>
        public static void WriteEnumIndexes_AnimationTrackInfo(StringBuilder targetStringBuilder, int index, string aniTrackEnumName)
        {
            targetStringBuilder.Append($"📼 <color=#2ecc71><b>{index:000}</b></color>\t<color=#ac92ec>{aniTrackEnumName}</color>");
        }



        /// <summary>
        /// 열거형 인덱스 - 애니메이션 랭크 출력하기
        /// </summary>
        /// <param name="targetStringBuilder"></param>
        public static void WriteEnumIndexes_AnimationRankInfo(StringBuilder targetStringBuilder, int index, string aniRankEnumName)
        {
            targetStringBuilder.Append($"👑 <color=#2ecc71><b>{index:00}</b></color>\t<color=#ac92ec>{aniRankEnumName}</color>");
        }



        /// <summary>
        /// 열거형 인덱스 - 애니메이션 타임 출력하기
        /// </summary>
        /// <param name="targetStringBuilder"></param>
        public static void WriteEnumIndexes_AnimationTimeInfo(StringBuilder targetStringBuilder, int index, string aniTimeEnumName)
        {
            targetStringBuilder.Append($"⌚ <color=#2ecc71><b>{index:00}</b></color>\t<color=#ac92ec>{aniTimeEnumName}</color>");
        }



        ///======================================================================================================================================================



        //? Spine.Skin Data



        /// <summary>
        /// <see cref="Spine.Skin"/> 출력하기
        /// </summary>
        /// <param name="targetStringBuilder"></param>
        /// <param name="skelSbject"></param>
        /// <param name="skin"></param>
        /// <param name="useDetail"></param>
        public static void WriteSpineSkinDataInfo(StringBuilder targetStringBuilder, SkelSbject skelSbject, Spine.Skin skin, bool useDetail)
        {
            if (useDetail)
            {
                if (skin.HasRequiredElements()) targetStringBuilder.AppendLine($"👔 <color=#2ecc71><b>*</b></color> <b>{skin.Name}</b>");
                else targetStringBuilder.AppendLine($"👕 <b>{skin.Name}</b>");
            }
            //? 심화 정보 보기가 아닐경우, 볼드를 해제한다
            else
            {
                if (skin.HasRequiredElements()) targetStringBuilder.AppendLine($"👔 <color=#2ecc71><b>*</b></color> {skin.Name}");
                else targetStringBuilder.AppendLine($"👕 {skin.Name}");
            }


            //? 심화 정보 보기
            if (useDetail)
            {
                //. 본
                if (skin.Bones.Items.Length != 0)
                {
                    targetStringBuilder.AppendLine($"\t🍖 <b>본</b>: <b><i><color=#2ecc71>{skin.Bones.Items.Length}</color></i></b>");

                    for (int i = 0; i < skin.Bones.Items.Length; i++)
                    {
                        var skinBoneData = skelSbject.BoneDataEX_Dictionary.GetValue1(skin.Bones.Items[i].Name);
                        targetStringBuilder.AppendLine($"\t\t🍖 {skinBoneData.Name}");
                    }
                }


                //. "트랜스폼 제약조건" 만 필터링
                TransformConstraintData[] skin_TFConsItems = skin.Constraints.Items.Where(x => x is TransformConstraintData).Select(x => (TransformConstraintData)x).ToArray();

                //. "IK 제약조건" 만 필터링
                IkConstraintData[] skin_IKConsItems = skin.Constraints.Items.Where(x => x is IkConstraintData).Select(x => (IkConstraintData)x).ToArray();

                //. "Path 제약조건" 만 필터링
                PathConstraintData[] skin_PathConsItems = skin.Constraints.Items.Where(x => x is PathConstraintData).Select(x => (PathConstraintData)x).ToArray();

                //. "Physics 제약조건" 만 필터링
                PhysicsConstraintData[] skin_PhysicsConsItems = skin.Constraints.Items.Where(x => x is PhysicsConstraintData).Select(x => (PhysicsConstraintData)x).ToArray();


                //. "트랜스폼 제약조건" 
                if (skin_TFConsItems.Length != 0)
                {
                    targetStringBuilder.AppendLine($"\t💪 <b>트랜스폼 제약조건</b>: <b><i><color=#2ecc71>{skin_TFConsItems.Length}</color></i></b>");

                    for (int i = 0; i < skin_TFConsItems.Length; i++)
                    {
                        var skin_TFCons = skelSbject.TFConsDataEX_Dictionary[skin_TFConsItems[i].Name];
                        targetStringBuilder.AppendLine($"\t\t💪 {skin_TFCons.Name}");
                    }
                }

                //. "IK 제약조건"
                if (skin_IKConsItems.Length != 0)
                {
                    targetStringBuilder.AppendLine($"\t🦿 <b>IK 제약조건</b>: <b><i><color=#2ecc71>{skin_IKConsItems.Length}</color></i></b>");

                    for (int i = 0; i < skin_IKConsItems.Length; i++)
                    {
                        var skin_IKCons = skelSbject.IKConsDataEX_Dictionary[skin_IKConsItems[i].Name];
                        targetStringBuilder.AppendLine($"\t\t🦵 {skin_IKCons.Name}");
                    }
                }

                //. "Path 제약조건"
                if (skin_PathConsItems.Length != 0)
                {
                    targetStringBuilder.AppendLine($"\t🚲 <b>Path 제약조건</b>: <b><i><color=#2ecc71>{skin_PathConsItems.Length}</color></i></b>");

                    for (int i = 0; i < skin_PathConsItems.Length; i++)
                    {
                        var skin_PathCons = skelSbject.PathConsDataEX_Dictionary[skin_PathConsItems[i].Name];
                        targetStringBuilder.AppendLine($"\t\t🚴‍ {skin_PathCons.Name}");
                    }
                }

                //. "Physics 제약조건"
                if (skin_PhysicsConsItems.Length != 0)
                {
                    targetStringBuilder.AppendLine($"\t🍎 <b>Physics 제약조건</b>: <b><i><color=#2ecc71>{skin_PhysicsConsItems.Length}</color></i></b>");

                    for (int i = 0; i < skin_PhysicsConsItems.Length; i++)
                    {
                        var skin_PhysicsCons = skelSbject.PhysicsConsDataEX_Dictionary[skin_PhysicsConsItems[i].Name];
                        targetStringBuilder.AppendLine($"\t\t🍏 {skin_PhysicsCons.Name}");
                    }
                }
            }
        }



        //? Spine.Animation Data



        /// <summary>
        /// <see cref="Spine.Animation"/> 출력하기
        /// </summary>
        /// <param name="targetStringBuilder"></param>
        /// <param name="spineAnimationEX"></param>
        public static void WriteSpineAnimationDataInfo(StringBuilder targetStringBuilder, AnimationEX spineAnimationEX, bool useDetail)
        {
            if (!useDetail)
            {
                targetStringBuilder.AppendLine($"🏃 {spineAnimationEX.Animation.Name.WrapTargetSubstring("/", "<color=#ed5565><b>", "</b></color>")}");
            }
            else
            {
                targetStringBuilder.AppendLine($"🏃 <b><color=#f7da64>{spineAnimationEX.Animation.Name.WrapTargetSubstring("/", "<color=#ed5565><b>", "</b></color>")}</color></b>");
            }


            if (useDetail)
            {
                if (spineAnimationEX.SpineEvents.Count != 0)
                {
                    targetStringBuilder.AppendLine($"\t🍆 <b>보유 이벤트</b>: <color=#2ecc71><b><i>{spineAnimationEX.SpineEvents.Count}</i></b></color>");
                    foreach (var item in spineAnimationEX.SpineEvents)
                    {
                        targetStringBuilder.AppendLine($"\t\t🍆{item.Key.WrapTargetSubstring("/", "<color=#ed5565><b>", "</b></color>")} (<color=#2ecc71><b>{item.Value}</b></color>)");
                    }
                }
            }
        }



        //? BoneEX Data



        /// <summary>
        /// <see cref="BoneDataEX"/> 출력하기
        /// </summary>
        /// <param name="targetStringBuilder"></param>
        public static void WriteBoneDataEXInfo(StringBuilder targetStringBuilder, BoneDataEX boneDataEX, bool useDetail, bool useTreeView)
        {
            string slashSpace = "";
            string slashSpace_Second = "";
            if (useTreeView)
            {
                int slashCount = boneDataEX.BoneFullPath.Count(x => x == '/');
                slashSpace += string.Concat(Enumerable.Repeat(" ", slashCount));
                slashSpace_Second = slashSpace;
                if (slashCount > 0)
                {
                    slashSpace += "└";
                }
            }


            if (useDetail)
            {
                if (!boneDataEX.BoneData.SkinRequired) targetStringBuilder.AppendLine($"{slashSpace}🦴 <b>{boneDataEX.Name}</b> [<i><color=#2ecc71>{boneDataEX.Index}</color></i>]");
                else targetStringBuilder.AppendLine($"{slashSpace}🍖 <color=#2ecc71><b>*</b></color> <b>{boneDataEX.Name}</b> [<i><color=#2ecc71>{boneDataEX.Index}</color></i>]");
            }
            //? 심화 정보 보기가 아닐경우, 볼드를 해제한다
            else
            {
                if (!boneDataEX.BoneData.SkinRequired) targetStringBuilder.AppendLine($"{slashSpace}🦴 {boneDataEX.Name} [<i><color=#2ecc71>{boneDataEX.Index}</color></i>]");
                else targetStringBuilder.AppendLine($"{slashSpace}🍖 <color=#2ecc71><b>*</b></color> {boneDataEX.Name} [<i><color=#2ecc71>{boneDataEX.Index}</color></i>]");
            }


            if (useDetail)
            {
                targetStringBuilder.AppendLine($"{slashSpace_Second}\t📁 {boneDataEX.BoneFullPath.WrapTargetSubstring("/", "<color=#ed5565><b>", "</b></color>")}");
                if (boneDataEX.UseSkinRequired)
                {
                    targetStringBuilder.AppendLine($"{slashSpace_Second}\t👔 이 본의 전용 스킨: <b><i><color=#2ecc71>{boneDataEX.SkinRequireds.Length}</color></b></i>");

                    for (int i = 0; i < boneDataEX.SkinRequireds.Length; i++)
                    {
                        targetStringBuilder.AppendLine($"{slashSpace_Second}\t\t👔 {boneDataEX.SkinRequireds[i].Name}");
                    }
                }
            }
        }



        //? Spine.SlotData Data



        /// <summary>
        /// <see cref="Spine.SlotData"/> 출력하기
        /// </summary>
        /// <param name="targetStringBuilder"></param>
        public static void WriteSpineSlotDataInfo(StringBuilder targetStringBuilder, Spine.SlotData slotData)
        {
            targetStringBuilder.AppendLine($"⚪ <color=#2ecc71><b>{slotData.Index.ToString("000")}</b></color> | {slotData.Name}");
        }



        //? TransformConstraintDataEX Data



        /// <summary>
        /// <see cref="TransformConstraintDataEX"/> 출력하기
        /// </summary>
        /// <param name="targetStringBuilder"></param>
        public static void WriteTransformConstraintDataEXInfo(StringBuilder targetStringBuilder, TransformConstraintDataEX transformConstraintDataEX, bool useDetail)
        {
            if (useDetail)
            {
                if (!transformConstraintDataEX.TransformConstraintData.SkinRequired) targetStringBuilder.AppendLine($"🦾 <b>{transformConstraintDataEX.Name.WrapTargetSubstring("/", "<color=#ed5565><b> ", " </b></color>")}</b> [<i><color=#2ecc71>{transformConstraintDataEX.Index}</color>]");
                else targetStringBuilder.AppendLine($"💪 <b>{transformConstraintDataEX.Name.WrapTargetSubstring("/", "<color=#ed5565><b> ", " </b></color>")}</b> [<i><color=#2ecc71>{transformConstraintDataEX.Index}</color></i>]");
            }
            //? 심화 정보 보기가 아닐경우, 볼드를 해제한다
            else
            {
                if (!transformConstraintDataEX.TransformConstraintData.SkinRequired) targetStringBuilder.AppendLine($"🦾 {transformConstraintDataEX.Name.WrapTargetSubstring("/", "<color=#ed5565><b> ", " </b></color>")} [<i><color=#2ecc71>{transformConstraintDataEX.Index}</color>]");
                else targetStringBuilder.AppendLine($"💪 {transformConstraintDataEX.Name.WrapTargetSubstring("/", "<color=#ed5565><b> ", " </b></color>")} [<i><color=#2ecc71>{transformConstraintDataEX.Index}</color></i>]");
            }



            if (useDetail)
            {
                if (transformConstraintDataEX.UseSkinRequired)
                {
                    targetStringBuilder.AppendLine($"\t👔 이 제약조건의 전용 스킨: <b><i><color=#2ecc71>{transformConstraintDataEX.SkinRequireds.Length}</color></i></b>");

                    for (int i = 0; i < transformConstraintDataEX.SkinRequireds.Length; i++)
                    {
                        targetStringBuilder.AppendLine($"\t\t👔 {transformConstraintDataEX.SkinRequireds[i].Name}");
                    }
                }
            }
        }



        //? IkConstraintDataEX Data



        /// <summary>
        /// <see cref="IkConstraintDataEX"/> 출력하기
        /// </summary>
        /// <param name="targetStringBuilder"></param>
        public static void WriteIkConstraintDataEXInfo(StringBuilder targetStringBuilder, IkConstraintDataEX ikConstraintDataEX, bool useDetail)
        {
            if (useDetail)
            {
                if (!ikConstraintDataEX.IkConstraintData.SkinRequired) targetStringBuilder.AppendLine($"🦿 <b>{ikConstraintDataEX.Name.WrapTargetSubstring("/", "<color=#ed5565><b> ", " </b></color>")}</b> [<i><color=#2ecc71>{ikConstraintDataEX.Index}</color>]");
                else targetStringBuilder.AppendLine($"🦵 <b>{ikConstraintDataEX.Name.WrapTargetSubstring("/", "<color=#ed5565><b> ", " </b></color>")}</b> [<i><color=#2ecc71>{ikConstraintDataEX.Index}</color></i>]");
            }
            //? 심화 정보 보기가 아닐경우, 볼드를 해제한다
            else
            {
                if (!ikConstraintDataEX.IkConstraintData.SkinRequired) targetStringBuilder.AppendLine($"🦿 {ikConstraintDataEX.Name.WrapTargetSubstring("/", "<color=#ed5565><b> ", " </b></color>")} [<i><color=#2ecc71>{ikConstraintDataEX.Index}</color>]");
                else targetStringBuilder.AppendLine($"🦵 {ikConstraintDataEX.Name.WrapTargetSubstring("/", "<color=#ed5565><b> ", " </b></color>")} [<i><color=#2ecc71>{ikConstraintDataEX.Index}</color></i>]");
            }



            if (useDetail)
            {
                if (ikConstraintDataEX.UseSkinRequired)
                {
                    targetStringBuilder.AppendLine($"\t👔 이 제약조건의 전용 스킨: <b><i><color=#2ecc71>{ikConstraintDataEX.SkinRequireds.Length}</color></i></b>");

                    for (int i = 0; i < ikConstraintDataEX.SkinRequireds.Length; i++)
                    {
                        targetStringBuilder.AppendLine($"\t\t👔 {ikConstraintDataEX.SkinRequireds[i].Name}");
                    }
                }
            }
        }



        //? PathConstraintDataEX Data



        /// <summary>
        /// <see cref="PathConstraintDataEX"/> 출력하기
        /// </summary>
        /// <param name="targetStringBuilder"></param>
        public static void WritePathConstraintDataEXInfo(StringBuilder targetStringBuilder, PathConstraintDataEX pathConstraintDataEX, bool useDetail)
        {
            if (useDetail)
            {
                if (!pathConstraintDataEX.PathConstraintData.SkinRequired) targetStringBuilder.AppendLine($"🚲 <b>{pathConstraintDataEX.Name.WrapTargetSubstring("/", "<color=#ed5565><b> ", " </b></color>")}</b> [<i><color=#2ecc71>{pathConstraintDataEX.Index}</color>]");
                else targetStringBuilder.AppendLine($"🚴 <b>{pathConstraintDataEX.Name.WrapTargetSubstring("/", "<color=#ed5565><b> ", " </b></color>")}</b> [<i><color=#2ecc71>{pathConstraintDataEX.Index}</color></i>]");
            }
            //? 심화 정보 보기가 아닐경우, 볼드를 해제한다
            else
            {
                if (!pathConstraintDataEX.PathConstraintData.SkinRequired) targetStringBuilder.AppendLine($"🚲 {pathConstraintDataEX.Name.WrapTargetSubstring("/", "<color=#ed5565><b> ", " </b></color>")} [<i><color=#2ecc71>{pathConstraintDataEX.Index}</color>]");
                else targetStringBuilder.AppendLine($"🚴 {pathConstraintDataEX.Name.WrapTargetSubstring("/", "<color=#ed5565><b> ", " </b></color>")} [<i><color=#2ecc71>{pathConstraintDataEX.Index}</color></i>]");
            }



            if (useDetail)
            {
                if (pathConstraintDataEX.UseSkinRequired)
                {
                    targetStringBuilder.AppendLine($"\t👔 이 제약조건의 전용 스킨: <b><i><color=#2ecc71>{pathConstraintDataEX.SkinRequireds.Length}</color></i></b>");

                    for (int i = 0; i < pathConstraintDataEX.SkinRequireds.Length; i++)
                    {
                        targetStringBuilder.AppendLine($"\t\t👔 {pathConstraintDataEX.SkinRequireds[i].Name}");
                    }
                }
            }
        }



        //? PhysicsConstraintDataEX Data



        /// <summary>
        /// <see cref="PhysicsConstraintDataEX"/> 출력하기
        /// </summary>
        /// <param name="targetStringBuilder"></param>
        public static void WritePhysicsConstraintDataEXInfo(StringBuilder targetStringBuilder, PhysicsConstraintDataEX physicsConstraintDataEX, bool useDetail)
        {
            if (useDetail)
            {
                if (!physicsConstraintDataEX.PhysicsConstraintData.SkinRequired) targetStringBuilder.AppendLine($"🍎 <b>{physicsConstraintDataEX.Name.WrapTargetSubstring("/", "<color=#ed5565><b> ", " </b></color>")}</b> [<i><color=#2ecc71>{physicsConstraintDataEX.Index}</color>]");
                else targetStringBuilder.AppendLine($"🍏 <b>{physicsConstraintDataEX.Name.WrapTargetSubstring("/", "<color=#ed5565><b> ", " </b></color>")}</b> [<i><color=#2ecc71>{physicsConstraintDataEX.Index}</color></i>]");
            }
            //? 심화 정보 보기가 아닐경우, 볼드를 해제한다
            else
            {
                if (!physicsConstraintDataEX.PhysicsConstraintData.SkinRequired) targetStringBuilder.AppendLine($"🍎 {physicsConstraintDataEX.Name.WrapTargetSubstring("/", "<color=#ed5565><b> ", " </b></color>")} [<i><color=#2ecc71>{physicsConstraintDataEX.Index}</color>]");
                else targetStringBuilder.AppendLine($"🍏 {physicsConstraintDataEX.Name.WrapTargetSubstring("/", "<color=#ed5565><b> ", " </b></color>")} [<i><color=#2ecc71>{physicsConstraintDataEX.Index}</color></i>]");
            }



            if (useDetail)
            {
                if (physicsConstraintDataEX.UseSkinRequired)
                {
                    targetStringBuilder.AppendLine($"\t👔 이 제약조건의 전용 스킨: <b><i><color=#2ecc71>{physicsConstraintDataEX.SkinRequireds.Length}</color></i></b>");

                    for (int i = 0; i < physicsConstraintDataEX.SkinRequireds.Length; i++)
                    {
                        targetStringBuilder.AppendLine($"\t\t👔 {physicsConstraintDataEX.SkinRequireds[i].Name}");
                    }
                }
            }
        }



        //? Spine.EventData Data



        /// <summary>
        /// <see cref="Spine.EventData"/> 출력하기
        /// </summary>
        /// <param name="targetStringBuilder"></param>
        public static void WriteSpineEventDataInfo(StringBuilder targetStringBuilder, Spine.EventData spineEvent, bool useDetail, SkelSbject skelSbject)
        {
            if (!useDetail)
            {
                targetStringBuilder.AppendLine($"🍆 {spineEvent.Name.WrapTargetSubstring("/", "<color=#ed5565><b>", "</b></color>")}");
            }
            else
            {
                targetStringBuilder.AppendLine($"🍆 <b><color=#f7da64>{spineEvent.Name.WrapTargetSubstring("/", "<color=#ed5565><b>", "</b></color>")}</color></b>");
            }


            if (useDetail)
            {
                List<Spine.Animation> animations = new List<Spine.Animation>();

                for (int i = 0; i < skelSbject.AnimationEX_Dictionary.dataArray.Length; i++)
                {
                    var animation = skelSbject.AnimationEX_Dictionary.dataArray[i];

                    foreach (var currentSpineEventName in animation.SpineEvents.Keys)
                    {
                        if (currentSpineEventName == spineEvent.Name)
                        {
                            animations.Add(animation.Animation);
                        }
                    }
                }

                if (animations.Count != 0)
                {
                    targetStringBuilder.AppendLine($"\t🏃 <b>보유 애니메이션</b>: <color=#2ecc71><b><i>{animations.Count}</i></b></color>");

                    for (int i = 0; i < animations.Count; i++)
                    {
                        targetStringBuilder.AppendLine($"\t\t🏃{animations[i].Name.WrapTargetSubstring("/", "<color=#ed5565><b>", "</b></color>")}");
                    }
                }
            }
        }



        ///======================================================================================================================================================
    }
}