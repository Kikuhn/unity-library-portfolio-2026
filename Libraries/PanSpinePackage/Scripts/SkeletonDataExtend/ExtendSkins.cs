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
using Pan.SpineUtil;



namespace Pan.SpinePackage
{
    ///======================================================================================================================================================



    //? 확장 스킨



    /// <summary>
    /// 확장 스킨
    /// </summary>
    public class ExtendSkin
    {
        ///======================================================================================================================================================



        //? 추가 확장



        /// <summary>
        /// 메인 <see cref="Spine.Skin"/>외에 추가로 보유하고싶을때 사용하는 확장<br/>
        /// (추가된 <see cref="Spine.Skin"/>들은 별도로 캐싱되지않는다)
        /// </summary>
        public class Multiple : ExtendSkin
        {
            /// <summary>
            /// Multiple 확장 스킨을 생성합니다
            /// </summary>
            /// <param name="mainSkin">메인 스킨</param>
            /// <param name="mainSkinName">스킨 이름 (메인스킨의 이름 대신 적용)</param>
            /// <param name="multipleSkins">추가 스킨 배열</param>
            /// <param name="skinLayerRank">스킨 LayerRank</param>
            public Multiple(Skin mainSkin, string mainSkinName, Skin[] multipleSkins, int skinLayerRank = 0) : base(mainSkin, mainSkin.Name, skinLayerRank)
            {
                MultipleSkins = multipleSkins;
            }

            /// <summary>
            /// Multiple 확장 스킨을 생성합니다
            /// </summary>
            /// <param name="mainSkin">메인 스킨</param>
            /// <param name="multipleSkins">추가 스킨 배열</param>
            /// <param name="skinLayerRank">스킨 LayerRank</param>
            public Multiple(Skin mainSkin, Skin[] multipleSkins, int skinLayerRank = 0) : this(mainSkin, mainSkin.Name, multipleSkins, skinLayerRank) { }

            /// <summary>
            /// Multiple 확장 스킨을 생성합니다
            /// </summary>
            /// <param name="getMainSkin">인터페이스로 스킨 얻어오기</param>
            /// <param name="mainSkinName">얻어올 스킨의 이름</param>
            /// <param name="multipleSkins">추가 스킨 배열</param>
            /// <param name="skinLayerRank">스킨 LayerRank</param>
            public Multiple(ICanGetSkin getMainSkin, string mainSkinName, Skin[] multipleSkins, int skinLayerRank = 0) : this(getMainSkin.GetSkin(mainSkinName), mainSkinName, multipleSkins, skinLayerRank) { }



            /// <summary>
            /// 추가 스킨들
            /// </summary>
            public readonly Skin[] MultipleSkins;
        }



        /// <summary>
        /// <see cref="Multiple"/>를 응용한 "눈" 확장<br/>
        /// 메인Skin은 양쪽 눈, <see cref="Multiple.MultipleSkins"/> 에는 좌측/우측 눈 등록
        /// </summary>
        public class Eyes : Multiple
        {
            public Eyes(Skin bothEyes, Skin leftEye, Skin rightEye, int skinLayerRank = 0) : base(bothEyes, bothEyes.Name, new Skin[2] { leftEye, rightEye }, skinLayerRank) { }



            /// <summary>
            /// 좌측 눈 얻기
            /// </summary>
            public Skin LeftEye => MultipleSkins[0];

            /// <summary>
            /// 우측 눈 얻기
            /// </summary>
            public Skin RightEye => MultipleSkins[1];



            /// <summary>
            /// 눈 얻기
            /// </summary>
            public Skin GetEye(EDirectionLRn direction)
            {
                switch (direction)
                {
                    case EDirectionLRn.None: return MainSkin;
                    case EDirectionLRn.Left: return LeftEye;
                    case EDirectionLRn.Right: return RightEye;
                    default: return MainSkin;
                }
            }
        }



        ///======================================================================================================================================================


        //? 생성자



        /// <summary>
        /// 확장 스킨을 생성합니다
        /// </summary>
        /// <param name="mainSkin">메인 스킨</param>
        /// <param name="mainSkinName">스킨 이름 (메인스킨의 이름 대신 적용)</param>
        /// <param name="skinLayerRank">스킨 LayerRank</param>
        public ExtendSkin(Skin mainSkin, string mainSkinName, int skinLayerRank = 0)
        {
            if (mainSkin == null)
            {
                Debug.LogError($"<b>ExtendSkin 생성 실패!</b> (mainSkin이 비어있음, <color=cyan>{mainSkinName}</color>)");
                return;
            }

            MainSkin = mainSkin;
            Name = mainSkinName;
            LayerRank = skinLayerRank;
            SkinEntrys = (MainSkin.Attachments == null || MainSkin.Attachments.Count == 0) ? null : MainSkin.Attachments.ToArray();
            BoneDatas = (MainSkin.Bones == null || MainSkin.Bones.Count == 0) ? null : MainSkin.Bones.ToArray();
            ConstraintDatas = (MainSkin.Constraints == null || MainSkin.Constraints.Count == 0) ? null : MainSkin.Constraints.ToArray();

            //StringBuilder sb = new StringBuilder();


            //sb.AppendLine($"{skin.Name} ({skinName})");
            //sb.AppendLine("-- SkinEntrys --");
            //foreach (var item in SkinEntrys)
            //{
            //    sb.AppendLine($"{item.Name}, {item.SlotIndex}, {item.Attachment}, {item.Attachment.Name}");
            //}
            //sb.AppendLine("-- BoneDatas --");
            //foreach (var item in BoneDatas)
            //{
            //    sb.AppendLine($"{item.Name}, {item.Index}");
            //}
            //sb.AppendLine("-- ConstraintDatas --");
            //foreach (var item in ConstraintDatas)
            //{
            //    sb.AppendLine($"{item.Name}, {item.Order}, {item.SkinRequired}");
            //}

            //Debug.Log(sb.ToString());
        }



        /// <summary>
        /// 확장 스킨을 생성합니다
        /// </summary>
        /// <param name="mainSkin">메인 스킨 (스킨 이름도 이 스킨의 이름으로 결정)</param>
        /// <param name="skinLayerRank">스킨 LayerRank</param>
        public ExtendSkin(Skin mainSkin, int skinLayerRank = 0) : this(mainSkin, mainSkin.Name, skinLayerRank) { }



        /// <summary>
        /// 확장 스킨을 생성합니다
        /// </summary>
        /// <param name="getMainSkin">인터페이스로 스킨 얻어오기</param>
        /// <param name="mainSkinName">얻어올 스킨의 이름</param>
        /// <param name="skinLayerRank">스킨 LayerRank</param>
        public ExtendSkin(ICanGetSkin getMainSkin, string mainSkinName, int skinLayerRank = 0) : this(getMainSkin.GetSkin(mainSkinName), mainSkinName, skinLayerRank) { }



        public static implicit operator Skin(ExtendSkin value) => value.MainSkin;



        ///======================================================================================================================================================



        //? 핵심 스킨 정보



        ///<summary>
        ///메인 스킨
        /// </summary>
        public readonly Skin MainSkin;

        ///<summary>
        ///스킨 이름
        /// </summary>
        public readonly string Name;

        ///<summary>
        ///스킨 LayerRank
        /// </summary>
        public int LayerRank;



        ///======================================================================================================================================================



        //? 스킨 캐싱



        ///<summary>
        ///캐싱된 SkinEntrys (Attachments)
        /// </summary>
        public readonly Skin.SkinEntry[] SkinEntrys;

        ///<summary>
        ///캐싱된 BoneDatas
        /// </summary>
        public readonly BoneData[] BoneDatas;

        ///<summary>
        ///캐싱된 ConstraintDatas
        /// </summary>
        public readonly IConstraintData[] ConstraintDatas;



        /// <summary>
        /// SkinEntrys가 유효한가?
        /// </summary>
        public bool IsValid_SkinEntrys => SkinEntrys != null && SkinEntrys.Length > 0;

        /// <summary>
        /// BoneDatas가 유효한가?
        /// </summary>
        public bool IsValid_BoneDatas => BoneDatas != null && BoneDatas.Length > 0;

        /// <summary>
        /// ConstraintDatas가 유효한가?
        /// </summary>
        public bool IsValid_ConstraintDatas => ConstraintDatas != null && ConstraintDatas.Length > 0;



        ///======================================================================================================================================================



        //? 이벤트



        /// <summary>
        /// 스킨이 적용될때의 액션
        /// </summary>
        public event Action<SkelObject> ApplyEvents = null;



        /// <summary>
        /// 스킨이 제거될때의 액션
        /// </summary>
        public event Action<SkelObject> RemoveEvents = null;



        ///======================================================================================================================================================



        //? 이벤트 실행



        /// <summary>
        /// 이 스킨이 적용될때 이 메서드가 실행되야함
        /// </summary>
        public void ApplyEvent(SkelObject skelObject)
        {
            ApplyEvents?.Invoke(skelObject);
        }



        /// <summary>
        /// 이 스킨이 제거될때 이 메서드가 실행되야함
        /// </summary>
        public void RemoveEvent(SkelObject skelObject)
        {
            RemoveEvents?.Invoke(skelObject);
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 다수의 ExtendSkin 를 섞어주는 믹서, int를 Key로 쓰는 Dictionary로 관리
    /// </summary>
    public class ExtendSkinMixManager
    {
        ///======================================================================================================================================================

        public ExtendSkinMixManager(int capacity = 0)
        {
            MixExtendSkins = new Dictionary<int, ExtendSkin>(capacity);
        }

        ///======================================================================================================================================================

        private readonly Dictionary<int, ExtendSkin> MixExtendSkins;

        ///======================================================================================================================================================

        public void AddSkin(ExtendSkin extendSkin, bool overlap)
        {
            //받아온 ExtendSkin의 LayerRank가 이미 존재하면 그냥 return, 단 overlap일경우 무시하고 덮어씌움
            if (MixExtendSkins.ContainsKey(extendSkin.LayerRank))
            {
                if (overlap) { MixExtendSkins[extendSkin.LayerRank] = extendSkin; }
                return;
            }

            MixExtendSkins.Add(extendSkin.LayerRank, extendSkin);
        }

        /// <summary>
        /// ExtendSkin 로 제거, 받아온 Skin의 LayerRank 가 있고, 받아온거랑 똑같으면 삭제
        /// </summary>
        /// <param name="wasAddedExtendSkin">추가했던 스킨</param>
        public bool RemoveSkin(ExtendSkin wasAddedExtendSkin)
        {
            if (MixExtendSkins.TryGetValue(wasAddedExtendSkin.LayerRank, out var result) && result == wasAddedExtendSkin)
            {
                MixExtendSkins.Remove(wasAddedExtendSkin.LayerRank);
                return true;
            }

            return false;
        }

        public bool RemoveSkin(int layerRank)
        {
            return MixExtendSkins.Remove(layerRank);
        }

        public ExtendSkin GetSkin(int layerRank)
        {
            if (MixExtendSkins.ContainsKey(layerRank))
            {
                return MixExtendSkins[layerRank];
            }
            return null;
        }

        public bool TryGetSkin(int layerRank, out ExtendSkin resultExtendSkin)
        {
            return MixExtendSkins.TryGetValue(layerRank, out resultExtendSkin);
        }

        public bool TryGetSkin(ExtendSkin skin, out ExtendSkin resultExtendSkin)
        {
            return MixExtendSkins.TryGetValue(skin.LayerRank, out resultExtendSkin);
        }

        public bool Contains(int layerRank) => MixExtendSkins.ContainsKey(layerRank);

        public bool Contains(ExtendSkin extendSkin)
        {
            if (TryGetSkin(extendSkin.LayerRank, out var result))
            {
                return result == extendSkin;
            }
            return false;
        }

        public void Clear()
        {
            MixExtendSkins.Clear();
        }

        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================



    ///<summary>
    ///조건에 충족하는 <see cref="Skin.SkinEntry"/>를 캐싱하는 클래스
    /// </summary>
    [Serializable]
    public class SkinEntryHoldCaching
    {
        ///======================================================================================================================================================



        ///<summary>
        /// 직접 <see cref="Skin.SkinEntry"/>를 넣어 캐싱
        /// </summary>
        public SkinEntryHoldCaching(Skin.SkinEntry[] skinEntrys)
        {
            SkinEntrys = skinEntrys;
        }



        ///<summary>
        /// 직접 <see cref="Skin.SkinEntry"/>를 넣어 캐싱
        /// </summary>
        public SkinEntryHoldCaching(IEnumerable<Skin.SkinEntry> skinEntrys)
        {
            SkinEntrys = skinEntrys.ToArray();
        }



        /// <summary>
        /// <see cref="Skin"/>의 Attachments에 직접 접근하여, 조건에 맞는 <see cref="Skin.SkinEntry"/>를 캐싱
        /// </summary>
        /// <param name="skin">대상 스킨</param>
        /// <param name="condition">캐싱 조건</param>
        public SkinEntryHoldCaching(Skin skin, Func<Skin.SkinEntry, bool> condition)
        {
            List<Skin.SkinEntry> skinEntryList = new List<Skin.SkinEntry>(skin.Attachments);

            foreach (var skinEntry in skin.Attachments)
            {
                if (condition.Invoke(skinEntry)) skinEntryList.Add(skinEntry);
            }

            SkinEntrys = skinEntryList.ToArray();
        }



        /// <summary>
        /// <see cref="ExtendSkin "/>의 SkinEntry에 접근하여, 조건에 맞는 <see cref="Skin.SkinEntry"/>를 캐싱
        /// </summary>
        /// <param name="extendSkin">대상 확장 스킨</param>
        /// <param name="condition">캐싱 조건</param>
        public SkinEntryHoldCaching(ExtendSkin extendSkin, Func<Skin.SkinEntry, bool> condition)
        {
            List<Skin.SkinEntry> skinEntryList = new List<Skin.SkinEntry>(extendSkin.SkinEntrys);

            foreach (var skinEntry in extendSkin.SkinEntrys)
            {
                if (condition.Invoke(skinEntry)) skinEntryList.Add(skinEntry);
            }

            SkinEntrys = skinEntryList.ToArray();
        }



        ///======================================================================================================================================================



        public static implicit operator Skin.SkinEntry[](SkinEntryHoldCaching value) => value.SkinEntrys;



        ///======================================================================================================================================================



        ///<summary>
        ///SkinEntry(Attachment) 저장소
        /// </summary>
        public readonly Skin.SkinEntry[] SkinEntrys;



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================
}