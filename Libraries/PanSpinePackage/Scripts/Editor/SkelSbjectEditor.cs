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
using Cysharp.Threading.Tasks;
using System.Text;
using UnityEditor;
using Pan.Util.Editors;
using Pan.SpineUtil;
using Sirenix.Utilities.Editor;
using System.Reflection;
using static Pan.SpinePackage.SkelSbject;
using System.Reflection.Emit;



namespace Pan.SpinePackage.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SkelSbject), true)]
    public class SkelSbjectEditor : EditorExpand_InspectorGUI<SkelSbject>
    {
        ///======================================================================================================================================================



        public SkelSbject.ISkeletonDataAsset SkeletonDataAsset => Target;



        ///======================================================================================================================================================



        [NonSerialized] private bool isInitialed_SkeletonDataAsset;



        private static readonly StringBuilder stringBuilder = new StringBuilder();



        ///======================================================================================================================================================



        private bool SkelSbject_IsHoldInterface;
        private string SkelSbject_HoldInterfaceInfo;



        public abstract class SkelSbjectInfo_EnumIndex_Base : MainSlave<SkelSbjectEditor>
        {
            public SkelSbjectInfo_EnumIndex_Base(SkelSbjectEditor main) : base(main) { }

            [SerializeField] public bool Fold;
            [SerializeField] public Vector2 ScrollPos;
            [SerializeField] public float ScrollHeight = 100;
            [SerializeField] public string SearchQuery;
            [SerializeField] public bool OrderMode;
        }



        public class SkelSbjectInfo_EnumIndex_SkinLayers : SkelSbjectInfo_EnumIndex_Base
        {
            public SkelSbjectInfo_EnumIndex_SkinLayers(SkelSbjectEditor main) : base(main) { }
        }
        public class SkelSbjectInfo_EnumIndex_AniTracks : SkelSbjectInfo_EnumIndex_Base
        {
            public SkelSbjectInfo_EnumIndex_AniTracks(SkelSbjectEditor main) : base(main) { }
        }
        public class SkelSbjectInfo_EnumIndex_AniRanks : SkelSbjectInfo_EnumIndex_Base
        {
            public SkelSbjectInfo_EnumIndex_AniRanks(SkelSbjectEditor main) : base(main) { }
        }
        public class SkelSbjectInfo_EnumIndex_AniTimes : SkelSbjectInfo_EnumIndex_Base
        {
            public SkelSbjectInfo_EnumIndex_AniTimes(SkelSbjectEditor main) : base(main) { }
        }



        public SkelSbjectInfo_EnumIndex_SkinLayers SkelSbjectInfo_EnumIndex_SkinLayer;
        public SkelSbjectInfo_EnumIndex_AniTracks SkelSbjectInfo_EnumIndex_AniTrack;
        public SkelSbjectInfo_EnumIndex_AniRanks SkelSbjectInfo_EnumIndex_AniRank;
        public SkelSbjectInfo_EnumIndex_AniTimes SkelSbjectInfo_EnumIndex_AniTime;



        ///======================================================================================================================================================



        //? Box 



        public abstract class BoxInfoBase : MainSlave<SkelSbjectEditor>
        {
            public BoxInfoBase(SkelSbjectEditor main) : base(main) { }

            protected SkelSbject Target => Main.Target;

            public abstract bool HasHoldBox { get; protected set; }

            [SerializeField] public bool Fold_HoldInterfaceInfo;
            [SerializeField] public string HoldInterfaceInfoText;
        }



        public abstract class BoxInfoBase<TDataListType> : BoxInfoBase
        {
            public BoxInfoBase(SkelSbjectEditor main) : base(main) { }

            public List<(string Name, TDataListType Value)> DataList { get; protected set; } = null;

            public bool IsValidData => DataList != null && DataList.Count != 0;

            [SerializeField] public bool Fold_DataList;
            [SerializeField] public Vector2 ScrollPos_DataList;
            [SerializeField] public float ScrollHeight_DataList;
            [SerializeField] public string SearchQuery_DataList;
        }



        public class AniBoxInfos : BoxInfoBase<SkelAni>
        {
            public AniBoxInfos(SkelSbjectEditor main) : base(main)
            {
                ScrollHeight_DataList = 355;

                if (Target is SkelSbject.IHoldAniBox aniBox)
                {
                    HasHoldBox = true;

                    stringBuilder.Clear();
                    SU_InfoWriter.WriteSkelSbjectHoldSomethingInterfaceInfo(stringBuilder, aniBox.AniBox, $"AniBox");
                    HoldInterfaceInfoText = stringBuilder.GetTemporaryString(true);

                    //DataList = SU_Reflection.GetMembersWithOriginalNames<SkelAni>(aniBox.AniBox, false, true, true, true, SU_Reflection.MemberType.Fields, SU_Reflection.MemberType.Properties);
                    DataList = GetSkelAniReflections(aniBox.AniBox);
                }
            }

            public override bool HasHoldBox { get; protected set; }

            [SerializeField] public bool SearchQueryMode_AniBox;



            /// <summary>
            /// 받아온 객체에 있는 모든 SkelAni들을 리플렉션을 사용하여  정보 리스트로 얻기
            /// </summary>
            /// <param name="target"></param>
            /// <returns></returns>
            public static List<(string Name, SkelAni Value)> GetSkelAniReflections(object target)
            {
                return SU_Reflection.GetMembersWithOriginalNames<SkelAni>(target, false, true, true, true, SU_Reflection.MemberType.Fields, SU_Reflection.MemberType.Properties);
            }
        }



        public class BoneBoxInfos : BoxInfoBase<string>
        {
            public BoneBoxInfos(SkelSbjectEditor main) : base(main)
            {
                ScrollHeight_DataList = 200;

                if (Target is SkelSbject.IHoldBoneBox boneBox)
                {
                    HasHoldBox = true;

                    stringBuilder.Clear();
                    SU_InfoWriter.WriteSkelSbjectHoldSomethingInterfaceInfo(stringBuilder, boneBox.BoneBox, $"BoneBox");
                    HoldInterfaceInfoText = stringBuilder.GetTemporaryString(true);

                    //. BONE_VALUES_NAME 이 포함된 변수들만 가져온다
                    DataList = SU_Reflection.GetMembersWithOriginalNames<string>(boneBox.BoneBox, false, true, true, true, SU_Reflection.MemberType.Fields, SU_Reflection.MemberType.Properties)
                        .Where(x => x.Name.Contains(SkelSbject.BaseBoneBox.BONE_VALUES_NAME))
                        .ToList();
                }
            }

            public override bool HasHoldBox { get; protected set; }
        }



        public class SkinAtchBoxInfos : BoxInfoBase<ExtendSkin>
        {
            public SkinAtchBoxInfos(SkelSbjectEditor main) : base(main)
            {
                ScrollHeight_DataList = 200;

                if (Target is SkelSbject.IHoldSkinAtchBox skinAtchBox)
                {
                    HasHoldBox = true;

                    stringBuilder.Clear();
                    SU_InfoWriter.WriteSkelSbjectHoldSomethingInterfaceInfo(stringBuilder, skinAtchBox.SkinAtchBox, $"SkinAtchBox");
                    HoldInterfaceInfoText = stringBuilder.GetTemporaryString(true);

                    DataList = SU_Reflection.GetMembersWithOriginalNames<ExtendSkin>(skinAtchBox.SkinAtchBox, false, true, true, true, SU_Reflection.MemberType.Fields, SU_Reflection.MemberType.Properties);

                    SpineSkinList = SU_Reflection.GetMembersWithOriginalNames<Spine.Skin>(skinAtchBox.SkinAtchBox, false, true, true, true, SU_Reflection.MemberType.Fields, SU_Reflection.MemberType.Properties);
                }
            }

            public override bool HasHoldBox { get; protected set; }

            [SerializeField] public bool UseDetail;
            [SerializeField] public bool UseDetail_SpineSkin;

            public List<(string Name, Spine.Skin Value)> SpineSkinList { get; protected set; } = null;

            public bool IsValidSpineSkinData => SpineSkinList != null && SpineSkinList.Count != 0;

            [SerializeField] public bool Fold_SpineSkinList;
            [SerializeField] public Vector2 ScrollPos_SpineSkinList;
            [SerializeField] public float ScrollHeight_SpineSkinList;
            [SerializeField] public string SearchQuery_SpineSkinList;
        }



        public class SpineEventBoxInfos : BoxInfoBase<string>
        {
            public SpineEventBoxInfos(SkelSbjectEditor main) : base(main)
            {
                ScrollHeight_DataList = 200;

                if (Target is SkelSbject.IHoldSpineEventBox spineEventBox)
                {
                    HasHoldBox = true;

                    stringBuilder.Clear();
                    SU_InfoWriter.WriteSkelSbjectHoldSomethingInterfaceInfo(stringBuilder, spineEventBox.SpineEventBox, $"SpineEventBox");
                    HoldInterfaceInfoText = stringBuilder.GetTemporaryString(true);

                    DataList = SU_Reflection.GetMembersWithOriginalNames<string>(spineEventBox.SpineEventBox, true, true, true, true, SU_Reflection.MemberType.Fields, SU_Reflection.MemberType.Properties);
                }
            }

            public override bool HasHoldBox { get; protected set; }
        }



        public class InteractionBoxInfos : BoxInfoBase
        {
            public InteractionBoxInfos(SkelSbjectEditor main) : base(main)
            {
                if (Target is SkelSbject.IHoldInteractionBox interactionBox)
                {
                    HasHoldBox = true;

                    stringBuilder.Clear();
                    SU_InfoWriter.WriteSkelSbjectHoldSomethingInterfaceInfo(stringBuilder, interactionBox.InteractionBox, $"InteractionBox");
                    HoldInterfaceInfoText = stringBuilder.GetTemporaryString(true);
                }
            }

            public override bool HasHoldBox { get; protected set; }

            [SerializeField] public bool Fold_InteractionInfo;
        }



        public class DrawOrderBoxInfos : BoxInfoBase<BaseDrawOrderSet>
        {
            public DrawOrderBoxInfos(SkelSbjectEditor main) : base(main)
            {
                if (Target is SkelSbject.IHoldDrawOrderBox drawOrderBox)
                {
                    HasHoldBox = true;

                    stringBuilder.Clear();
                    SU_InfoWriter.WriteSkelSbjectHoldSomethingInterfaceInfo(stringBuilder, drawOrderBox.DrawOrderBox, $"DrawOrderBox");
                    HoldInterfaceInfoText = stringBuilder.GetTemporaryString(true);

                    DataList = SU_Reflection.GetMembersWithOriginalNames<BaseDrawOrderSet>(drawOrderBox.DrawOrderBox, false, true, true, true, SU_Reflection.MemberType.Fields, SU_Reflection.MemberType.Properties);
                }
            }

            public override bool HasHoldBox { get; protected set; }
        }



        private AniBoxInfos AniBoxInfo;
        private BoneBoxInfos BoneBoxInfo;
        private SkinAtchBoxInfos SkinAtchBoxInfo;
        private SpineEventBoxInfos SpineEventBoxInfo;
        private InteractionBoxInfos InteractionBoxInfo;
        private DrawOrderBoxInfos DrawOrderBoxInfo;



        ///======================================================================================================================================================



        //? 어드밴스드 스킨



        public class AdvancedSkinInfoChip
        {
            public AdvancedSkinInfoChip(BaseAdvancedSkin advancedSkin)
            {
                AdvancedSkinName = advancedSkin.GetType().Name;

                stringBuilder.Clear();
                SU_InfoWriter.WriteSkelSbjectHoldSomethingInterfaceInfo(stringBuilder, advancedSkin, AdvancedSkinName);
                HoldInterfaceInfoText = stringBuilder.ToString(true);
                stringBuilder.Clear();


                DataList_ExtendSkin = new DataList_ExtendSkins();
                DataList_ExtendSkin.Datas = SU_Reflection.GetMembersWithOriginalNames<ExtendSkin>(advancedSkin, false, true, true, true, SU_Reflection.MemberType.Fields, SU_Reflection.MemberType.Properties);

                DataList_SpineSkin = new DataList_SpineSkins();
                DataList_SpineSkin.Datas = SU_Reflection.GetMembersWithOriginalNames<Spine.Skin>(advancedSkin, false, true, true, true, SU_Reflection.MemberType.Fields, SU_Reflection.MemberType.Properties);

                DataList_SkelAni = new DataList_SkelAnis();
                //DataList_SkelAni.Datas = SU_Reflection.GetMembersWithOriginalNames<SkelAni>(advancedSkin, false, true, true, true, SU_Reflection.MemberType.Fields, SU_Reflection.MemberType.Properties);
                DataList_SkelAni.Datas = AniBoxInfos.GetSkelAniReflections(advancedSkin);


                DataList_BoneName = new DataList_BoneNames();
                //. BONE_VALUES_NAME 이 포함된 변수들만 가져온다
                DataList_BoneName.Datas = SU_Reflection.GetMembersWithOriginalNames<string>(advancedSkin, false, true, true, true, SU_Reflection.MemberType.Fields, SU_Reflection.MemberType.Properties)
                    .Where(x => x.Name.Contains(SkelSbject.BaseBoneBox.BONE_VALUES_NAME))
                    .ToList();
            }


            public bool Fold;
            public readonly string AdvancedSkinName;


            public string HoldInterfaceInfoText;


            [SerializeField] public bool Fold_HoldInterfaceInfo;


            public readonly DataList_ExtendSkins DataList_ExtendSkin;
            public readonly DataList_SpineSkins DataList_SpineSkin;
            public readonly DataList_SkelAnis DataList_SkelAni;
            public readonly DataList_BoneNames DataList_BoneName;



            public class BaseDataList
            {
                [SerializeField] public bool Fold_DataList;
                [SerializeField] public Vector2 ScrollPos_DataList;
                [SerializeField] public float ScrollHeight_DataList = 200;
                [SerializeField] public string SearchQuery_DataList;
            }



            public class DataList_ExtendSkins : BaseDataList
            {
                public List<(string Name, ExtendSkin Value)> Datas;
                public bool UseDetail;
            }



            public class DataList_SpineSkins : BaseDataList
            {
                public List<(string Name, Skin Value)> Datas;
                public bool UseDetail;
            }



            public class DataList_SkelAnis : BaseDataList
            {
                public DataList_SkelAnis()
                {
                    ScrollHeight_DataList = 400;
                }

                public List<(string Name, SkelAni Value)> Datas;

                [SerializeField] public bool SearchQueryMode;
            }



            public class DataList_BoneNames : BaseDataList
            {
                public List<(string Name, string Value)> Datas;
            }
        }



        private List<AdvancedSkinInfoChip> AdvancedSkinList;



        ///======================================================================================================================================================



        //? Data



        public class DataInfoBase : MainSlave<SkelSbjectEditor>
        {
            public DataInfoBase(SkelSbjectEditor main) : base(main) { }

            protected SkelSbject Target => Main.Target;

            [SerializeField] public bool Fold;
            [SerializeField] public Vector2 ScrollPos;
            [SerializeField] public float ScrollHeight = 200;
            [SerializeField] public string SearchQuery;
        }

        public class SkinDataInfos : DataInfoBase
        {
            public SkinDataInfos(SkelSbjectEditor main) : base(main) { }
            [SerializeField] public bool UseDetail;
            [SerializeField] public bool FilteringSkinRequireds;
        }

        public class AnimationDataInfos : DataInfoBase
        {
            public AnimationDataInfos(SkelSbjectEditor main) : base(main) { }
            [SerializeField] public bool UseDetail;
        }

        public class BoneDataInfos : DataInfoBase
        {
            public BoneDataInfos(SkelSbjectEditor main) : base(main)
            {
                BoneDataEX_CachedList = main.Target.BoneDataEX_Dictionary.dataArray.ToList();
                Refresh_BoneDataEX_CachedList_InfoText();
            }

            [SerializeField] public bool UseDetail;
            [SerializeField] public bool FilteringSkinRequireds;
            [SerializeField] public bool UseTreeView;
            [SerializeField] public List<BoneDataEX> BoneDataEX_CachedList;
            [SerializeField] public Dictionary<BoneDataEX, string> BoneDataEX_CachedList_InfoText = new Dictionary<BoneDataEX, string>();


            public void Changed_FilteringSkinRequireds()
            {
                if (FilteringSkinRequireds)
                {
                    BoneDataEX_CachedList = BoneDataEX_CachedList.Where(x => x.UseSkinRequired).ToList();

                    if (UseTreeView)
                    {
                        Changed_UseTreeView();
                        return;
                    }
                }
                else
                {
                    BoneDataEX_CachedList = Main.Target.BoneDataEX_Dictionary.dataArray.ToList();
                }

                Refresh_BoneDataEX_CachedList_InfoText();
            }


            public void Changed_UseTreeView()
            {
                //BoneDataEX_CachedList = BoneDataEX_CachedList.OrderBy(info => info.BoneFullPath.Split('/'), HierarchicalPathComparer.Instance).ToList();
                BoneDataEX_CachedList.Sort(new HierarchicalFieldComparer<BoneDataEX>(x => x.BoneFullPath));
                Refresh_BoneDataEX_CachedList_InfoText();
            }



            public void Refresh_BoneDataEX_CachedList_InfoText()
            {
                BoneDataEX_CachedList_InfoText.Clear();
                foreach (var boneDataEX in BoneDataEX_CachedList)
                {
                    stringBuilder.Clear();
                    SU_InfoWriter.WriteBoneDataEXInfo(stringBuilder, boneDataEX, UseDetail, UseTreeView);
                    BoneDataEX_CachedList_InfoText.Add(boneDataEX, stringBuilder.ToString(true));
                    stringBuilder.Clear();
                }
            }

        }

        public class SlotDataInfos : DataInfoBase
        {
            public SlotDataInfos(SkelSbjectEditor main) : base(main) { }
        }

        public class TransformConstraintDataInfos : DataInfoBase
        {
            public TransformConstraintDataInfos(SkelSbjectEditor main) : base(main) { }
            [SerializeField] public bool UseDetail;
            [SerializeField] public bool FilteringSkinRequireds;

        }

        public class IkConstraintDataInfos : DataInfoBase
        {
            public IkConstraintDataInfos(SkelSbjectEditor main) : base(main) { }
            [SerializeField] public bool UseDetail;
            [SerializeField] public bool FilteringSkinRequireds;

        }

        public class PathConstraintDataInfos : DataInfoBase
        {
            public PathConstraintDataInfos(SkelSbjectEditor main) : base(main) { }
            [SerializeField] public bool UseDetail;
            [SerializeField] public bool FilteringSkinRequireds;


        }

        public class PhysicsConstraintDataInfos : DataInfoBase
        {
            public PhysicsConstraintDataInfos(SkelSbjectEditor main) : base(main) { }
            [SerializeField] public bool UseDetail;
            [SerializeField] public bool FilteringSkinRequireds;


        }

        public class EventConstraintDataInfos : DataInfoBase
        {
            public EventConstraintDataInfos(SkelSbjectEditor main) : base(main) { }
            [SerializeField] public bool UseDetail;
        }



        private SkinDataInfos SkinDataInfo { get; set; }
        private AnimationDataInfos AnimationDataInfo { get; set; }
        private BoneDataInfos BoneDataInfo { get; set; }
        private SlotDataInfos SlotDataInfo { get; set; }
        private TransformConstraintDataInfos TransformConstraintDataInfo { get; set; }
        private IkConstraintDataInfos IkConstraintDataInfo { get; set; }
        private PathConstraintDataInfos PathConstraintDataInfo { get; set; }
        private PhysicsConstraintDataInfos PhysicsConstraintDataInfo { get; set; }
        private EventConstraintDataInfos EventConstraintDataInfo { get; set; }



        public ImmutableDataManager<string, Skin> Skin_Dictionary => Target.Skin_Dictioinary;
        public ImmutableDataManager<string, AnimationEX> AnimationEX_Dictionary => Target.AnimationEX_Dictionary;
        public ImmutableDataManager2<string, int, BoneDataEX> BoneDataEX_Dictionary => Target.BoneDataEX_Dictionary;
        public ImmutableDataManager2<string, int, SlotData> Slot_Dictionary => Target.Slot_Dictionary;
        public ImmutableDataManager2<string, int, TransformConstraintDataEX> TFConsDataEX_Dictionary => Target.TFConsDataEX_Dictionary;
        public ImmutableDataManager2<string, int, IkConstraintDataEX> IKConsDataEX_Dictionary => Target.IKConsDataEX_Dictionary;
        public ImmutableDataManager2<string, int, PathConstraintDataEX> PathConsDataEX_Dictionary => Target.PathConsDataEX_Dictionary;
        public ImmutableDataManager2<string, int, PhysicsConstraintDataEX> PhysicsConsDataEX_Dictionary => Target.PhysicsConsDataEX_Dictionary;
        public ImmutableDataManager<string, Spine.EventData> SpineEvent_Dictionary => Target.SpineEvent_Dictionary;



        ///======================================================================================================================================================



        protected override void Awake()
        {
            base.Awake();

            Target.WakeUp_ScriptableObject();

            Initial();
        }



        protected override void OnEnable()
        {
            base.OnEnable();

            Target.WakeUp_ScriptableObject();

            Initial();
        }



        private void Initial()
        {
            if (isInitialed_SkeletonDataAsset || SkeletonDataAsset.Skeleton_DataAsset == null) { return; }

            stringBuilder.Clear();
            SkelSbject_IsHoldInterface = SU_InfoWriter.WriteSkelSbjectHoldSomethingInterfaceInfo(stringBuilder, Target, $"{Target.GetType().Name}");
            SkelSbject_HoldInterfaceInfo = stringBuilder.GetTemporaryString(true);


            SkelSbjectInfo_EnumIndex_SkinLayer = new(this);
            SkelSbjectInfo_EnumIndex_AniTrack = new(this);
            SkelSbjectInfo_EnumIndex_AniRank = new(this);
            SkelSbjectInfo_EnumIndex_AniTime = new(this);


            //? AniBox 가져오기
            AniBoxInfo = new(this);

            //? BoneBox 가져오기
            BoneBoxInfo = new(this);

            //? SkinAtchBox 가져오기
            SkinAtchBoxInfo = new(this);

            //? SpineEventBox 가져오기
            SpineEventBoxInfo = new(this);

            //? InteractionBox 가져오기
            InteractionBoxInfo = new(this);



            if (Target is IHoldAdvancedSkinManager holdAdvancedSkinManager)
            {
                AdvancedSkinList = new List<AdvancedSkinInfoChip>(holdAdvancedSkinManager.GetAdvancedSkinArray.Length);

                foreach (var obj_AdvancedSkin in holdAdvancedSkinManager.GetAdvancedSkinArray)
                {
                    if (obj_AdvancedSkin is BaseAdvancedSkin advancedSkin)
                    {
                        AdvancedSkinList.Add(new AdvancedSkinInfoChip(advancedSkin));
                    }
                }
            }



            SkinDataInfo = new(this);
            AnimationDataInfo = new(this);
            BoneDataInfo = new(this);
            SlotDataInfo = new(this);
            TransformConstraintDataInfo = new(this);
            IkConstraintDataInfo = new(this);
            PathConstraintDataInfo = new(this);
            PhysicsConstraintDataInfo = new(this);
            EventConstraintDataInfo = new(this);
            DrawOrderBoxInfo = new(this);

            isInitialed_SkeletonDataAsset = true;
        }



        ///======================================================================================================================================================



        protected override void OnInspectorGUI_Current()
        {
            base.OnInspectorGUI_Current();


            Draw_SkeletonDataAsset();


            if (isInitialed_SkeletonDataAsset)
            {
                Draw_SkelSbjectInfo();
                Draw_Boxes();
                Draw_AdvancedSkin();
                Draw_SkeletonDataAssetCached();
            }
        }



        protected void Draw_SkeletonDataAsset()
        {
            SU_CustomEditor.AutoLabelField_Head("💽 Skeleton Data Asset", SU_CustomEditor.LabelHeadType.H1, () =>
            {
                if (Target.Skeleton_DataAsset == null)
                {
                    SirenixEditorGUI.ErrorMessageBox("스켈레톤 데이터 에셋이 비어있음, 추가 요망");
                }

                SU_CustomEditor.CheckChangeAction(this, () =>
                {
                    SU_CustomEditor.RenderField_PropertyOdinInspector(this, nameof(Target.Skeleton_DataAsset)._LowerFirst(), "스켈레톤 데이터 에셋");
                }, x =>
                {
                    SkeletonDataAsset.Skeleton_DataAsset = SkeletonDataAsset.Skeleton_DataAsset;
                    isInitialed_SkeletonDataAsset = false;
                    Initial();
                });

                if (Target.Skeleton_DataAsset != null)
                {
                    SU_CustomEditor.RenderField_Property(this, nameof(Target.CreateCount)._LowerFirst(), "기본 생성 개수(풀링)");
                    SU_CustomEditor.RenderField_Property(this, nameof(Target.CenterLocalPosition)._LowerFirst(), "로컬 중심 좌표");
                    SU_CustomEditor.RenderField_Property(this, nameof(Target.AnimationMixDuration)._LowerFirst(), "기본 애니메이션 MixDuration");
                }
            });
        }



        protected void Draw_SkelSbjectInfo()
        {
            SU_CustomEditor.AutoLabelField_Head($"💀 SkelSbject 정보", SU_CustomEditor.LabelHeadType.H1, () =>
            {
                SU_CustomEditor.AutoLabelField_Head($"⚡ Hold 인터페이스", SU_CustomEditor.LabelHeadType.H2, () =>
                    {
                        SU_CustomEditor.LabelField_TextAutoWidthHeight(SkelSbject_HoldInterfaceInfo);
                    }, false);


                SU_CustomEditor.AutoLabelField_Head($"🏷️ 열거형 인덱스", SU_CustomEditor.LabelHeadType.H2, () =>
                {
                    SU_CustomEditor.AutoLabelFoldOut_Head($"🛍️👑 스킨 레이어", SU_CustomEditor.LabelHeadType.H3, ref SkelSbjectInfo_EnumIndex_SkinLayer.Fold, () =>
                    {
                        if (!Target.IsDefault_EnumIndex_SkinLayers)
                        {
                            var skinLayers = Target.EnumIndex_SkinLayers.GetAssignedIndexes.ToList();

                            if (SkelSbjectInfo_EnumIndex_SkinLayer.OrderMode)
                            {
                                skinLayers.Reverse();
                            }

                            SU_CustomEditor.RenderField_Bool(this, ref SkelSbjectInfo_EnumIndex_SkinLayer.OrderMode, "👆 역순으로 정렬");

                            SU_CustomEditor.ScrollViewResizable_Collection(ref SkelSbjectInfo_EnumIndex_SkinLayer.ScrollPos, ref SkelSbjectInfo_EnumIndex_SkinLayer.ScrollHeight, 100, 1000, ref SkelSbjectInfo_EnumIndex_SkinLayer.SearchQuery, out int searchCount, skinLayers,
                                index =>
                                {
                                    stringBuilder.Clear();

                                    SU_InfoWriter.WriteEnumIndexes_SkinLayerInfo(stringBuilder, index, Target.EnumIndex_SkinLayers.GetEnumName(index));

                                    SU_CustomEditor.VerticalHelpBox(() =>
                                    {
                                        SU_CustomEditor.HorizontalGUI(() =>
                                        {
                                            SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));
                                        });
                                    });

                                }, x =>
                                {
                                    return Target.EnumIndex_SkinLayers.GetEnumName(x);
                                }, true, GUILayout.Height(100));
                        }
                        else
                        {
                            SU_CustomEditor.VerticalHelpBox(() =>
                            {
                                GUILayout.Space(6);
                                SU_CustomEditor.LabelField_TextAutoWidthHeight($"ℹ️ <b>스킨 레이어 열거형 인덱스</b>가 <color=#ed5565><b>override</b></color> 되지 않은 기본 형태");
                                GUILayout.Space(6);
                            });
                        }
                    }, false);
                    EditorGUILayout.Space();



                    SU_CustomEditor.AutoLabelFoldOut_Head($"🅰️📼 애니메이션 트랙", SU_CustomEditor.LabelHeadType.H3, ref SkelSbjectInfo_EnumIndex_AniTrack.Fold, () =>
                    {
                        if (!Target.IsDefault_EnumIndex_AniTracks)
                        {
                            var aniTracks = Target.EnumIndex_AniTracks.GetAssignedIndexes.ToList();

                            if (SkelSbjectInfo_EnumIndex_AniTrack.OrderMode)
                            {
                                aniTracks.Reverse();
                            }

                            SU_CustomEditor.RenderField_Bool(this, ref SkelSbjectInfo_EnumIndex_AniTrack.OrderMode, "👆 역순으로 정렬");

                            SU_CustomEditor.ScrollViewResizable_Collection(ref SkelSbjectInfo_EnumIndex_AniTrack.ScrollPos, ref SkelSbjectInfo_EnumIndex_AniTrack.ScrollHeight, 100, 1000, ref SkelSbjectInfo_EnumIndex_AniTrack.SearchQuery, out int searchCount, aniTracks,
                                index =>
                                {
                                    stringBuilder.Clear();

                                    SU_InfoWriter.WriteEnumIndexes_AnimationTrackInfo(stringBuilder, index, Target.EnumIndex_AniTracks.GetEnumName(index));

                                    SU_CustomEditor.VerticalHelpBox(() =>
                                    {
                                        SU_CustomEditor.HorizontalGUI(() =>
                                        {
                                            SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));
                                        });
                                    });

                                }, x =>
                                {
                                    return Target.EnumIndex_AniTracks.GetEnumName(x);
                                }, true, GUILayout.Height(100));
                        }
                        else
                        {
                            SU_CustomEditor.VerticalHelpBox(() =>
                            {
                                GUILayout.Space(6);
                                SU_CustomEditor.LabelField_TextAutoWidthHeight($"ℹ️ <b>애니메이션 트랙 열거형 인덱스</b>가 <color=#ed5565><b>override</b></color> 되지 않은 기본 형태");
                                GUILayout.Space(6);
                            });
                        }
                    }, false);
                    EditorGUILayout.Space();



                    SU_CustomEditor.AutoLabelFoldOut_Head($"🅰️👑 애니메이션 랭크", SU_CustomEditor.LabelHeadType.H3, ref SkelSbjectInfo_EnumIndex_AniRank.Fold, () =>
                    {
                        if (!Target.IsDefault_EnumIndex_AniRanks)
                        {
                            var aniRanks = Target.EnumIndex_AniRanks.GetAssignedIndexes.ToList();

                            if (SkelSbjectInfo_EnumIndex_AniRank.OrderMode)
                            {
                                aniRanks.Reverse();
                            }

                            SU_CustomEditor.RenderField_Bool(this, ref SkelSbjectInfo_EnumIndex_AniRank.OrderMode, "👆 역순으로 정렬");

                            SU_CustomEditor.ScrollViewResizable_Collection(ref SkelSbjectInfo_EnumIndex_AniRank.ScrollPos, ref SkelSbjectInfo_EnumIndex_AniRank.ScrollHeight, 100, 1000, ref SkelSbjectInfo_EnumIndex_AniRank.SearchQuery, out int searchCount, aniRanks,
                                index =>
                                {
                                    stringBuilder.Clear();

                                    SU_InfoWriter.WriteEnumIndexes_AnimationRankInfo(stringBuilder, index, Target.EnumIndex_AniRanks.GetEnumName(index));

                                    SU_CustomEditor.VerticalHelpBox(() =>
                                    {
                                        SU_CustomEditor.HorizontalGUI(() =>
                                        {
                                            SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));
                                        });
                                    });

                                }, x =>
                                {
                                    return Target.EnumIndex_AniRanks.GetEnumName(x);
                                }, true, GUILayout.Height(100));
                        }
                        else
                        {
                            SU_CustomEditor.VerticalHelpBox(() =>
                            {
                                GUILayout.Space(6);
                                SU_CustomEditor.LabelField_TextAutoWidthHeight($"ℹ️ <b>애니메이션 트랙 열거형 인덱스</b>가 <color=#ed5565><b>override</b></color> 되지 않은 기본 형태");
                                GUILayout.Space(6);
                            });
                        }
                    }, false);
                    EditorGUILayout.Space();



                    SU_CustomEditor.AutoLabelFoldOut_Head($"🅰️⌚ 애니메이션 타임", SU_CustomEditor.LabelHeadType.H3, ref SkelSbjectInfo_EnumIndex_AniTime.Fold, () =>
                    {
                        if (!Target.IsDefault_EnumIndex_AniTimes)
                        {
                            var aniTimes = Target.EnumIndex_AniTimes.GetAssignedIndexes.ToList();

                            if (SkelSbjectInfo_EnumIndex_AniTime.OrderMode)
                            {
                                aniTimes.Reverse();
                            }

                            SU_CustomEditor.RenderField_Bool(this, ref SkelSbjectInfo_EnumIndex_AniTime.OrderMode, "👆 역순으로 정렬");

                            SU_CustomEditor.ScrollViewResizable_Collection(ref SkelSbjectInfo_EnumIndex_AniTime.ScrollPos, ref SkelSbjectInfo_EnumIndex_AniTime.ScrollHeight, 100, 1000, ref SkelSbjectInfo_EnumIndex_AniTime.SearchQuery, out int searchCount, aniTimes,
                                index =>
                                {
                                    stringBuilder.Clear();

                                    SU_InfoWriter.WriteEnumIndexes_AnimationTimeInfo(stringBuilder, index, Target.EnumIndex_AniTimes.GetEnumName(index));

                                    SU_CustomEditor.VerticalHelpBox(() =>
                                    {
                                        SU_CustomEditor.HorizontalGUI(() =>
                                        {
                                            SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));
                                        });
                                    });

                                }, x =>
                                {
                                    return Target.EnumIndex_AniTimes.GetEnumName(x);
                                }, true, GUILayout.Height(100));
                        }
                        else
                        {
                            SU_CustomEditor.VerticalHelpBox(() =>
                            {
                                GUILayout.Space(6);
                                SU_CustomEditor.LabelField_TextAutoWidthHeight($"ℹ️ <b>애니메이션 트랙 열거형 인덱스</b>가 <color=#ed5565><b>override</b></color> 되지 않은 기본 형태");
                                GUILayout.Space(6);
                            });
                        }
                    }, false);
                    EditorGUILayout.Space();



                }, false);
            });
        }



        protected void Draw_Boxes()
        {
            SU_CustomEditor.AutoLabelField_Head("📦 Boxes", SU_CustomEditor.LabelHeadType.H1, () =>
            {
                SU_CustomEditor.DrawHighlightedBox(() =>
                {
                    if (AniBoxInfo.HasHoldBox)
                    {
                        SU_CustomEditor.AutoLabelField_Head("🅰️ AniBox", SU_CustomEditor.LabelHeadType.H2, () =>
                        {
                            if (AniBoxInfo.IsValidData)
                            {
                                SU_CustomEditor.FoldOut(ref AniBoxInfo.Fold_HoldInterfaceInfo, "⚡ Hold 인터페이스 보기", () =>
                                {
                                    SU_CustomEditor.LabelField_TextAutoWidthHeight(AniBoxInfo.HoldInterfaceInfoText);
                                });
                                SU_CustomEditor.FoldOut(ref AniBoxInfo.Fold_DataList, $"🅰️ {nameof(SkelAni)} 목록 보기", () =>
                                {
                                    SU_CustomEditor.HorizontalGUI(() =>
                                    {
                                        SU_CustomEditor.LabelField_TextAutoWidthHeight("🔍 검색 모드 변경");
                                        SU_CustomEditor.RenderField_Bool(this, ref AniBoxInfo.SearchQueryMode_AniBox);
                                    });


                                    SU_CustomEditor.LabelField_TextAutoWidthHeight(!AniBoxInfo.SearchQueryMode_AniBox ?
                                        $"현재 검색모드: <b><color=#2ecc71>[{nameof(SkelAni)}]</color></b> | {nameof(Spine)}{nameof(Spine.Animation)}.{nameof(Spine.Animation.Name)}" :
                                        $"현재 검색모드: <b>{nameof(SkelAni)}</b> | <b><color=#2ecc71>[{nameof(Spine)}{nameof(Spine.Animation)}.{nameof(Spine.Animation.Name)}]</color></b>");


                                    SU_CustomEditor.ScrollViewResizable_Collection(ref AniBoxInfo.ScrollPos_DataList, ref AniBoxInfo.ScrollHeight_DataList, 355, 800, ref AniBoxInfo.SearchQuery_DataList, out var serachCount, AniBoxInfo.DataList,
                                            x =>
                                            {
                                                SU_CustomEditor.VerticalHelpBox(() =>
                                                {
                                                    stringBuilder.Clear();
                                                    SU_InfoWriter.WriteSkelAniValueInfo(stringBuilder, x.Value, x.Name, Target);
                                                    SU_CustomEditor.LabelField_TextAutoWidthHeight($"{stringBuilder.ToString(true)}");
                                                });

                                            }, x =>
                                            {
                                                if (AniBoxInfo.SearchQueryMode_AniBox)
                                                {
                                                    return x.Value.Animation.Name;
                                                }
                                                else
                                                {
                                                    return x.Name;
                                                }
                                            }, true, GUILayout.Height(355), GUILayout.ExpandWidth(false));
                                });
                            }
                        });
                    }


                    if (BoneBoxInfo.HasHoldBox)
                    {
                        SU_CustomEditor.AutoLabelField_Head("🦴 BoneBox", SU_CustomEditor.LabelHeadType.H2, () =>
                        {
                            SU_CustomEditor.FoldOut(ref BoneBoxInfo.Fold_HoldInterfaceInfo, "⚡ Hold 인터페이스 보기", () =>
                            {
                                SU_CustomEditor.LabelField_TextAutoWidthHeight(BoneBoxInfo.HoldInterfaceInfoText);
                            });
                            SU_CustomEditor.FoldOut(ref BoneBoxInfo.Fold_DataList, $"🦴 Bone(이름) 목록 보기", () =>
                            {
                                SU_CustomEditor.ScrollViewResizable_Collection(ref BoneBoxInfo.ScrollPos_DataList, ref BoneBoxInfo.ScrollHeight_DataList, 200, 800, ref BoneBoxInfo.SearchQuery_DataList, out var serachCount, BoneBoxInfo.DataList,
                                        x =>
                                        {
                                            SU_CustomEditor.VerticalHelpBox(() =>
                                            {
                                                stringBuilder.Clear();
                                                //stringBuilder.AppendLine($"🦴 {x.Name}");
                                                SU_InfoWriter.WriteBoneValueInfo(stringBuilder, x.Value, x.Name);
                                                SU_CustomEditor.LabelField_TextAutoWidthHeight($"{stringBuilder.ToString(true)}");
                                            });

                                        }, x =>
                                        {
                                            return x.Name;
                                        }, true, GUILayout.Height(200));
                            });
                        });
                    }


                    if (SkinAtchBoxInfo.HasHoldBox)
                    {
                        SU_CustomEditor.AutoLabelField_Head("🛍️ SkinAtchBox", SU_CustomEditor.LabelHeadType.H2, () =>
                        {
                            if (SkinAtchBoxInfo.IsValidData)
                            {
                                SU_CustomEditor.FoldOut(ref SkinAtchBoxInfo.Fold_HoldInterfaceInfo, "⚡ Hold 인터페이스 보기", () =>
                                {
                                    SU_CustomEditor.LabelField_TextAutoWidthHeight(SkinAtchBoxInfo.HoldInterfaceInfoText);
                                });
                                SU_CustomEditor.FoldOut(ref SkinAtchBoxInfo.Fold_DataList, $"🛍️ ExtendSkin 목록 보기", () =>
                                {
                                    SU_CustomEditor.HorizontalGUI(() =>
                                    {
                                        SU_CustomEditor.LabelField_TextAutoWidthHeight("ℹ️ 심화 정보 보기");
                                        SU_CustomEditor.RenderField_Bool(this, ref SkinAtchBoxInfo.UseDetail);
                                    });


                                    SU_CustomEditor.ScrollViewResizable_Collection(ref SkinAtchBoxInfo.ScrollPos_DataList, ref SkinAtchBoxInfo.ScrollHeight_DataList, 200, 800, ref SkinAtchBoxInfo.SearchQuery_DataList, out var serachCount, SkinAtchBoxInfo.DataList,
                                            x =>
                                            {
                                                SU_CustomEditor.VerticalHelpBox(() =>
                                                {
                                                    stringBuilder.Clear();
                                                    SU_InfoWriter.WriteExtendSkinValueInfo(stringBuilder, SkinAtchBoxInfo.UseDetail, x.Value, x.Name, Target);
                                                    SU_CustomEditor.LabelField_TextAutoWidthHeight($"{stringBuilder.ToString(true)}");
                                                });

                                            }, x =>
                                            {
                                                return x.Name;
                                            }, true, GUILayout.Height(200));
                                });
                            }


                            if (SkinAtchBoxInfo.IsValidSpineSkinData)
                            {
                                SU_CustomEditor.FoldOut(ref SkinAtchBoxInfo.Fold_SpineSkinList, $"👕 Spine.Skin 목록 보기", () =>
                                {
                                    SU_CustomEditor.HorizontalGUI(() =>
                                    {
                                        SU_CustomEditor.LabelField_TextAutoWidthHeight("ℹ️ 심화 정보 보기");
                                        SU_CustomEditor.RenderField_Bool(this, ref SkinAtchBoxInfo.UseDetail_SpineSkin);
                                    });

                                    SU_CustomEditor.ScrollViewResizable_Collection(ref SkinAtchBoxInfo.ScrollPos_SpineSkinList, ref SkinAtchBoxInfo.ScrollHeight_SpineSkinList, 200, 800, ref SkinAtchBoxInfo.SearchQuery_SpineSkinList, out var serachCount, SkinAtchBoxInfo.SpineSkinList,
                                            x =>
                                            {
                                                SU_CustomEditor.VerticalHelpBox(() =>
                                                {
                                                    stringBuilder.Clear();
                                                    SU_InfoWriter.WriteSpineSkinDataInfo(stringBuilder, Target, x.Value, SkinAtchBoxInfo.UseDetail_SpineSkin);
                                                    SU_CustomEditor.LabelField_TextAutoWidthHeight($"{stringBuilder.ToString(true)}");
                                                });

                                            }, x =>
                                            {
                                                return x.Name;
                                            }, true, GUILayout.Height(200));
                                });
                            }
                        });
                    }


                    if (SpineEventBoxInfo.HasHoldBox)
                    {
                        SU_CustomEditor.AutoLabelField_Head("🍆 SpineEventBox", SU_CustomEditor.LabelHeadType.H2, () =>
                        {
                            if (SpineEventBoxInfo.IsValidData)
                            {
                                SU_CustomEditor.FoldOut(ref SpineEventBoxInfo.Fold_HoldInterfaceInfo, "⚡ Hold 인터페이스 보기", () =>
                                {
                                    SU_CustomEditor.LabelField_TextAutoWidthHeight(SpineEventBoxInfo.HoldInterfaceInfoText);
                                });
                                SU_CustomEditor.FoldOut(ref SpineEventBoxInfo.Fold_DataList, $"🍆 SpineEvent(이름) 목록 보기", () =>
                                {
                                    SU_CustomEditor.ScrollViewResizable_Collection(ref SpineEventBoxInfo.ScrollPos_DataList, ref SpineEventBoxInfo.ScrollHeight_DataList, 200, 800, ref SpineEventBoxInfo.SearchQuery_DataList, out var serachCount, SpineEventBoxInfo.DataList,
                                                x =>
                                                {
                                                    SU_CustomEditor.VerticalHelpBox(() =>
                                                    {
                                                        stringBuilder.Clear();
                                                        SU_InfoWriter.WriteSpineEventValueInfo(stringBuilder, x.Value, x.Name);
                                                        SU_CustomEditor.LabelField_TextAutoWidthHeight($"{stringBuilder.ToString(true)}");
                                                    });

                                                }, x =>
                                                {
                                                    return x.Name;
                                                }, true, GUILayout.Height(200));
                                });
                            }

                        });
                    }


                    if (InteractionBoxInfo.HasHoldBox)
                    {
                        SU_CustomEditor.AutoLabelField_Head("🏅 InteractionBox", SU_CustomEditor.LabelHeadType.H2, () =>
                        {
                            var interactionBox = (Target as SkelSbject.IHoldInteractionBox);

                            SU_CustomEditor.FoldOut(ref InteractionBoxInfo.Fold_HoldInterfaceInfo, "⚡ Hold 인터페이스 보기", () =>
                            {
                                SU_CustomEditor.LabelField_TextAutoWidthHeight(InteractionBoxInfo.HoldInterfaceInfoText);
                            });
                            SU_CustomEditor.FoldOut(ref InteractionBoxInfo.Fold_InteractionInfo, $"🏅 상호작용 정보 보기", () =>
                            {
                                if (interactionBox.InteractionBox.GetInteractionManagers != null && interactionBox.InteractionBox.GetInteractionManagers.Length != 0)
                                {
                                    stringBuilder.Clear();
                                    SU_InfoWriter.WriteInteractionBoxInfo(stringBuilder, interactionBox);
                                    SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));
                                }
                            });
                        });
                    }


                    if (DrawOrderBoxInfo.HasHoldBox)
                    {
                        SU_CustomEditor.AutoLabelField_Head("🖼️ DrawOrderBox", SU_CustomEditor.LabelHeadType.H2, () =>
                        {
                            var drawOrderBox = (Target as SkelSbject.IHoldDrawOrderBox);

                            SU_CustomEditor.FoldOut(ref DrawOrderBoxInfo.Fold_HoldInterfaceInfo, "⚡ Hold 인터페이스 보기", () =>
                            {
                                SU_CustomEditor.LabelField_TextAutoWidthHeight(DrawOrderBoxInfo.HoldInterfaceInfoText);
                            });


                            SU_CustomEditor.LabelField_TextAutoWidthHeight($"📜 보유중인 드로우오더 세트 클래스");


                            SU_CustomEditor.FoldOut(ref DrawOrderBoxInfo.Fold_DataList, $"🖼️ {nameof(BaseDrawOrderSet)} 목록 보기", () =>
                            {
                                SU_CustomEditor.ScrollViewResizable_Collection(ref DrawOrderBoxInfo.ScrollPos_DataList, ref DrawOrderBoxInfo.ScrollHeight_DataList, 200, 800, DrawOrderBoxInfo.DataList,
                                        x =>
                                        {
                                            SU_CustomEditor.VerticalHelpBox(() =>
                                            {
                                                stringBuilder.Clear();
                                                SU_InfoWriter.WriteInteractionBoxInfo(stringBuilder, x.Name, x.Value);
                                                SU_CustomEditor.LabelField_TextAutoWidthHeight($"{stringBuilder.ToString(true)}");
                                            });

                                        }, GUILayout.Height(200), GUILayout.ExpandWidth(false));
                            });
                        });
                    }
                });
            });
        }



        protected virtual void Draw_AdvancedSkin()
        {
            SU_CustomEditor.AutoLabelField_Head("👕➕ Advanced 스킨", SU_CustomEditor.LabelHeadType.H1, () =>
            {
                EditorGUILayout.Space(10);
                if (AdvancedSkinList != null && AdvancedSkinList.Count != 0)
                {
                    foreach (var advancedSkin in AdvancedSkinList)
                    {
                        SU_CustomEditor.AutoLabelFoldOut_Head($"{advancedSkin.AdvancedSkinName}", SU_CustomEditor.LabelHeadType.H2, ref advancedSkin.Fold,
                            () =>
                            {
                                SU_CustomEditor.DrawHighlightedBox(() =>
                                {
                                    SU_CustomEditor.FoldOut(ref advancedSkin.Fold_HoldInterfaceInfo, "⚡ Hold 인터페이스 보기", () =>
                                    {
                                        SU_CustomEditor.LabelField_TextAutoWidthHeight(advancedSkin.HoldInterfaceInfoText);
                                    });
                                    EditorGUILayout.Space();



                                    //? ExtendSkins
                                    if (advancedSkin.DataList_ExtendSkin.Datas != null && advancedSkin.DataList_ExtendSkin.Datas.Count != 0)
                                    {
                                        SU_CustomEditor.FoldOut(ref advancedSkin.DataList_ExtendSkin.Fold_DataList, $"🛍️ ExtendSkins",
                                            () =>
                                            {
                                                SU_CustomEditor.HorizontalGUI(() =>
                                                {
                                                    SU_CustomEditor.LabelField_TextAutoWidthHeight("ℹ️ 심화 정보 보기");
                                                    SU_CustomEditor.RenderField_Bool(this, ref advancedSkin.DataList_ExtendSkin.UseDetail);
                                                });

                                                SU_CustomEditor.ScrollViewResizable_Collection(ref advancedSkin.DataList_ExtendSkin.ScrollPos_DataList, ref advancedSkin.DataList_ExtendSkin.ScrollHeight_DataList, 200, 400, ref advancedSkin.DataList_ExtendSkin.SearchQuery_DataList, out int searchCount_ExtendSkin, advancedSkin.DataList_ExtendSkin.Datas,
                                                    x =>
                                                    {
                                                        SU_CustomEditor.VerticalHelpBox(() =>
                                                        {
                                                            stringBuilder.Clear();
                                                            SU_InfoWriter.WriteExtendSkinValueInfo(stringBuilder, advancedSkin.DataList_ExtendSkin.UseDetail, x.Value, x.Name, Target);
                                                            SU_CustomEditor.LabelField_TextAutoWidthHeight($"{stringBuilder.ToString(true)}");
                                                            stringBuilder.Clear();
                                                        });
                                                    }, x =>
                                                    {
                                                        return x.Name;
                                                    }, true, GUILayout.Height(200));
                                            });
                                        EditorGUILayout.Space();
                                    }



                                    //? Spine.Skins
                                    if (advancedSkin.DataList_SpineSkin.Datas != null && advancedSkin.DataList_SpineSkin.Datas.Count != 0)
                                    {
                                        SU_CustomEditor.FoldOut(ref advancedSkin.DataList_SpineSkin.Fold_DataList, $"👕 Spine.Skin",
                                            () =>
                                            {
                                                SU_CustomEditor.HorizontalGUI(() =>
                                                {
                                                    SU_CustomEditor.LabelField_TextAutoWidthHeight("ℹ️ 심화 정보 보기");
                                                    SU_CustomEditor.RenderField_Bool(this, ref advancedSkin.DataList_SpineSkin.UseDetail);
                                                });

                                                SU_CustomEditor.ScrollViewResizable_Collection(ref advancedSkin.DataList_SpineSkin.ScrollPos_DataList, ref advancedSkin.DataList_SpineSkin.ScrollHeight_DataList, 200, 400, ref advancedSkin.DataList_SpineSkin.SearchQuery_DataList, out int searchCount_ExtendSkin, advancedSkin.DataList_SpineSkin.Datas,
                                                x =>
                                                {
                                                    SU_CustomEditor.VerticalHelpBox(() =>
                                                    {
                                                        stringBuilder.Clear();
                                                        SU_InfoWriter.WriteSpineSkinDataInfo(stringBuilder, Target, x.Value, advancedSkin.DataList_SpineSkin.UseDetail);
                                                        SU_CustomEditor.LabelField_TextAutoWidthHeight($"{stringBuilder.ToString(true)}");
                                                        stringBuilder.Clear();
                                                    });
                                                }, x =>
                                                {
                                                    return x.Name;
                                                }, true, GUILayout.Height(200));
                                            });
                                        EditorGUILayout.Space();
                                    }



                                    //? SkelAni
                                    if (advancedSkin.DataList_SkelAni.Datas != null && advancedSkin.DataList_SkelAni.Datas.Count != 0)
                                    {
                                        SU_CustomEditor.FoldOut(ref advancedSkin.DataList_SkelAni.Fold_DataList, $"🅰️ SkelAni",
                                            () =>
                                            {
                                                SU_CustomEditor.RenderField_Bool(this, ref advancedSkin.DataList_SkelAni.SearchQueryMode, "검색 모드 변경");

                                                SU_CustomEditor.LabelField_TextAutoWidthHeight(!advancedSkin.DataList_SkelAni.SearchQueryMode ?
                                                    $"현재 검색모드: <b><color=#2ecc71>[{nameof(SkelAni)}]</color></b> | {nameof(Spine)}{nameof(Spine.Animation)}.{nameof(Spine.Animation.Name)}" :
                                                    $"현재 검색모드: <b>{nameof(SkelAni)}</b> | <b><color=#2ecc71>[{nameof(Spine)}{nameof(Spine.Animation)}.{nameof(Spine.Animation.Name)}]</color></b>");

                                                SU_CustomEditor.ScrollViewResizable_Collection(ref advancedSkin.DataList_SkelAni.ScrollPos_DataList, ref advancedSkin.DataList_SkelAni.ScrollHeight_DataList, 400, 800, ref advancedSkin.DataList_SkelAni.SearchQuery_DataList, out var serachCount, advancedSkin.DataList_SkelAni.Datas,
                                                        x =>
                                                        {
                                                            SU_CustomEditor.VerticalHelpBox(() =>
                                                            {
                                                                stringBuilder.Clear();
                                                                SU_InfoWriter.WriteSkelAniValueInfo(stringBuilder, x.Value, x.Name, Target);
                                                                SU_CustomEditor.LabelField_TextAutoWidthHeight($"{stringBuilder.ToString(true)}");
                                                            });

                                                        }, x =>
                                                        {
                                                            if (advancedSkin.DataList_SkelAni.SearchQueryMode)
                                                            {
                                                                return x.Value.Animation.Name;
                                                            }
                                                            else
                                                            {
                                                                return x.Name;
                                                            }
                                                        }, true, GUILayout.Height(400), GUILayout.ExpandWidth(true));
                                            });
                                        EditorGUILayout.Space();
                                    }



                                    //? Bone (Name)
                                    if (advancedSkin.DataList_BoneName.Datas != null && advancedSkin.DataList_BoneName.Datas.Count != 0)
                                    {
                                        SU_CustomEditor.FoldOut(ref advancedSkin.DataList_BoneName.Fold_DataList, $"🦴 Bone (이름)",
                                            () =>
                                            {
                                                SU_CustomEditor.ScrollViewResizable_Collection(ref advancedSkin.DataList_BoneName.ScrollPos_DataList, ref advancedSkin.DataList_BoneName.ScrollHeight_DataList, 200, 400, ref advancedSkin.DataList_BoneName.SearchQuery_DataList, out int searchCount_ExtendSkin, advancedSkin.DataList_BoneName.Datas,
                                                x =>
                                                {
                                                    SU_CustomEditor.VerticalHelpBox(() =>
                                                    {
                                                        stringBuilder.Clear();
                                                        SU_InfoWriter.WriteBoneValueInfo(stringBuilder, x.Value, x.Name);
                                                        SU_CustomEditor.LabelField_TextAutoWidthHeight($"{stringBuilder.ToString(true)}");
                                                        stringBuilder.Clear();
                                                    });
                                                }, x =>
                                                {
                                                    return x.Name;
                                                }, true, GUILayout.Height(200));
                                            });
                                        EditorGUILayout.Space();
                                    }
                                });
                            }, false);

                        EditorGUILayout.Space(10);
                    }
                }
            });
        }




        protected void Draw_SkeletonDataAssetCached()
        {
            SU_CustomEditor.AutoLabelField_Head("💽 DataAsset 기반 캐싱 요소 보기", SU_CustomEditor.LabelHeadType.H1, () =>
            {
                SU_CustomEditor.DrawHighlightedBox(() =>
                {
                    //? 스킨



                    #region 스킨



                    SU_CustomEditor.AutoLabelFoldOut_Head("👕 스킨 <size=11>(<i>Skins)</size>", SU_CustomEditor.LabelHeadType.H2, ref SkinDataInfo.Fold, () =>
                    {
                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.LabelField_TextAutoWidthHeight("ℹ️ 심화 정보 보기");
                            SU_CustomEditor.RenderField_Bool(this, ref SkinDataInfo.UseDetail);
                        });

                        //. 스킨 표시 함
                        if (Skin_Dictionary.Count != 0)
                        {
                            var skin_DataArray = Skin_Dictionary.dataArray;
                            if (SkinDataInfo.FilteringSkinRequireds) { skin_DataArray = skin_DataArray.Where(skin => skin.HasRequiredElements()).ToArray(); }

                            //? 필터링 옵션
                            SU_CustomEditor.HorizontalGUI(() =>
                            {
                                SU_CustomEditor.LabelField_TextAutoWidthHeight("📌 스킨 전용 요소가 포함된 스킨");
                                SU_CustomEditor.RenderField_Bool(this, ref SkinDataInfo.FilteringSkinRequireds);
                            });


                            //. 요소를 각각 스크롤뷰로 출력
                            SU_CustomEditor.ScrollViewResizable_Collection(ref SkinDataInfo.ScrollPos, ref SkinDataInfo.ScrollHeight, 100, 400, ref SkinDataInfo.SearchQuery, out var searchCount, skin_DataArray,
                                (skin) =>
                                {
                                    SU_CustomEditor.VerticalHelpBox(() =>
                                    {
                                        stringBuilder.Clear();
                                        SU_InfoWriter.WriteSpineSkinDataInfo(stringBuilder, Target, skin, SkinDataInfo.UseDetail);
                                        SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));
                                    });
                                },
                            x => x.Name, true, GUILayout.Height(200));
                        }

                        //. 스킨 표시 안함
                        else
                        {
                            SU_CustomEditor.HelpBox(MessageType.Info, "비어있음");
                        }
                    }, false);
                    EditorGUILayout.Space(15);



                    #endregion



                    //? 애니메이션



                    #region 애니메이션



                    SU_CustomEditor.AutoLabelFoldOut_Head("🏃 애니메이션 <size=11>(<i>Animations)</size>", SU_CustomEditor.LabelHeadType.H2, ref AnimationDataInfo.Fold, () =>
                    {
                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.LabelField_TextAutoWidthHeight("ℹ️ 심화 정보 보기");
                            SU_CustomEditor.RenderField_Bool(this, ref AnimationDataInfo.UseDetail);
                        });

                        //. 애니메이션 표시 함
                        if (AnimationEX_Dictionary.Count != 0)
                        {
                            //. 요소를 각각 스크롤뷰로 출력
                            SU_CustomEditor.ScrollViewResizable_Collection(ref AnimationDataInfo.ScrollPos, ref AnimationDataInfo.ScrollHeight, 100, 400, ref AnimationDataInfo.SearchQuery, out var searchCount, AnimationEX_Dictionary.dataArray,
                                (animation) =>
                                {
                                    SU_CustomEditor.VerticalHelpBox(() =>
                                    {
                                        stringBuilder.Clear();
                                        SU_InfoWriter.WriteSpineAnimationDataInfo(stringBuilder, animation, AnimationDataInfo.UseDetail);
                                        SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));
                                    });
                                },
                            x => x.Animation.Name, true, GUILayout.Height(200));
                        }

                        //. 애니메이션 표시 안함
                        else
                        {
                            SU_CustomEditor.HelpBox(MessageType.Info, "비어있음");
                        }
                    }, false);
                    EditorGUILayout.Space(15);



                    #endregion



                    //? 본 데이터



                    #region 본 데이터



                    SU_CustomEditor.AutoLabelFoldOut_Head("🦴 본 <size=11>(<i>Bones)</size>", SU_CustomEditor.LabelHeadType.H2, ref BoneDataInfo.Fold, () =>
                    {
                        //. 본 표시 함
                        if (BoneDataEX_Dictionary.Count != 0)
                        {
                            //? 스킨 전용 요소의 포함된 본
                            SU_CustomEditor.HorizontalGUI(() =>
                            {
                                SU_CustomEditor.LabelField_TextAutoWidthHeight("📌 스킨 전용 요소의 포함된 본");
                                SU_CustomEditor.CheckChangeAction(this, () =>
                                {
                                    SU_CustomEditor.RenderField_Bool(this, ref BoneDataInfo.FilteringSkinRequireds);
                                }, x =>
                                {
                                    BoneDataInfo.Changed_FilteringSkinRequireds();
                                });
                            });

                            //? 심화 정보 보기
                            SU_CustomEditor.HorizontalGUI(() =>
                            {
                                SU_CustomEditor.LabelField_TextAutoWidthHeight("ℹ️ 심화 정보 보기");
                                SU_CustomEditor.CheckChangeAction(this, () =>
                                {
                                    SU_CustomEditor.RenderField_Bool(this, ref BoneDataInfo.UseDetail);
                                }, x =>
                                {
                                    BoneDataInfo.Refresh_BoneDataEX_CachedList_InfoText();
                                });
                            });


                            //? 트리뷰로 보기
                            SU_CustomEditor.HorizontalGUI(() =>
                            {
                                SU_CustomEditor.LabelField_TextAutoWidthHeight("🌲 트리뷰로 보기");
                                SU_CustomEditor.CheckChangeAction(this, () =>
                                {
                                    SU_CustomEditor.RenderField_Bool(this, ref BoneDataInfo.UseTreeView);
                                }, x =>
                                {
                                    BoneDataInfo.Changed_UseTreeView();
                                });
                            });


                            //var boneData_DataList = BoneDataEX_Dictionary.dataArray.ToList();
                            var boneData_DataList = BoneDataInfo.BoneDataEX_CachedList;

                            //. 요소를 각각 스크롤뷰로 출력
                            SU_CustomEditor.ScrollViewResizable_Collection(ref BoneDataInfo.ScrollPos, ref BoneDataInfo.ScrollHeight, 100, 400, ref BoneDataInfo.SearchQuery, out var searchCount, BoneDataInfo.BoneDataEX_CachedList_InfoText,
                                (boneDataEX_KV) =>
                                {
                                    SU_CustomEditor.VerticalHelpBox(() =>
                                    {
                                        SU_CustomEditor.LabelField_TextAutoWidthHeight(boneDataEX_KV.Value);
                                    });
                                },
                            x =>
                            {
                                return x.Key.Name;
                            }
                            , true, GUILayout.Height(200));


                            ////. 요소를 각각 스크롤뷰로 출력
                            //SU_CustomEditor.ScrollViewResizable_Collection(ref BoneDataInfo.ScrollPos, ref BoneDataInfo.ScrollHeight, 100, 400, ref BoneDataInfo.SearchQuery, out var searchCount, boneData_DataList,
                            //    (boneDataEX) =>
                            //    {
                            //        SU_CustomEditor.VerticalHelpBox(() =>
                            //        {
                            //            stringBuilder.Clear();
                            //            SU_InfoWriter.WriteBoneDataEXInfo(stringBuilder, boneDataEX, BoneDataInfo.UseDetail, BoneDataInfo.UseTreeView);
                            //            SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));
                            //        });
                            //    },
                            //x => x.Name, true, GUILayout.Height(200));
                        }

                        //. 본 표시 안함
                        else
                        {
                            SU_CustomEditor.HelpBox(MessageType.Info, "비어있음");
                        }
                    }, false);
                    EditorGUILayout.Space(15);



                    #endregion



                    //? 슬롯



                    #region 슬롯



                    SU_CustomEditor.AutoLabelFoldOut_Head("⚪ 슬롯 <size=11>(<i>SlotDatas)</size>", SU_CustomEditor.LabelHeadType.H2, ref SlotDataInfo.Fold, () =>
                    {
                        //. 슬롯 표시 함
                        if (Slot_Dictionary.Count != 0)
                        {
                            //. 요소를 각각 스크롤뷰로 출력
                            //! 역순으로 정렬하여, 스파인 DrawOrder와 동일하게 보이게끔 한다
                            SU_CustomEditor.ScrollViewResizable_Collection(ref SlotDataInfo.ScrollPos, ref SlotDataInfo.ScrollHeight, 100, 400, ref SlotDataInfo.SearchQuery, out var searchCount, Slot_Dictionary.dataArray.Reverse(),
                                (slotData) =>
                                {
                                    SU_CustomEditor.VerticalHelpBox(() =>
                                    {
                                        stringBuilder.Clear();
                                        SU_InfoWriter.WriteSpineSlotDataInfo(stringBuilder, slotData);
                                        SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));
                                    });
                                },
                            x => x.Name, true, GUILayout.Height(200));
                        }

                        //. 슬롯 표시 안함
                        else
                        {
                            SU_CustomEditor.HelpBox(MessageType.Info, "비어있음");
                        }
                    }, false);
                    EditorGUILayout.Space(15);



                    #endregion



                    //? 트랜스폼 제약조건



                    #region 트랜스폼 제약조건



                    SU_CustomEditor.AutoLabelFoldOut_Head("🦾 트랜스폼 제약조건 <size=11>(<i>TransformConstraintDatas)</size>", SU_CustomEditor.LabelHeadType.H2, ref TransformConstraintDataInfo.Fold, () =>
                    {
                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.LabelField_TextAutoWidthHeight("ℹ️ 심화 정보 보기");
                            SU_CustomEditor.RenderField_Bool(this, ref TransformConstraintDataInfo.UseDetail);
                        });

                        //. 트랜스폼 제약조건 표시 함
                        if (TFConsDataEX_Dictionary.Count != 0)
                        {
                            var tfConsData_DataArray = TFConsDataEX_Dictionary.dataArray;
                            if (TransformConstraintDataInfo.FilteringSkinRequireds) { tfConsData_DataArray = tfConsData_DataArray.Where(tfCons => tfCons.TransformConstraintData.SkinRequired).ToArray(); }

                            //? 필터링 옵션
                            SU_CustomEditor.HorizontalGUI(() =>
                            {
                                SU_CustomEditor.LabelField_TextAutoWidthHeight("📌 스킨 전용 요소의 포함된 제약조건");
                                SU_CustomEditor.RenderField_Bool(this, ref TransformConstraintDataInfo.FilteringSkinRequireds);
                            });

                            //. 요소를 각각 스크롤뷰로 출력
                            SU_CustomEditor.ScrollViewResizable_Collection(ref TransformConstraintDataInfo.ScrollPos, ref TransformConstraintDataInfo.ScrollHeight, 100, 400, ref TransformConstraintDataInfo.SearchQuery, out var searchCount, tfConsData_DataArray,
                                (tf) =>
                                {
                                    SU_CustomEditor.VerticalHelpBox(() =>
                                    {
                                        stringBuilder.Clear();
                                        SU_InfoWriter.WriteTransformConstraintDataEXInfo(stringBuilder, tf, TransformConstraintDataInfo.UseDetail);
                                        SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));
                                    });
                                },
                            x => x.Name, true, GUILayout.Height(200));
                        }

                        //. 트랜스폼 제약조건 표시 안함
                        else
                        {
                            SU_CustomEditor.HelpBox(MessageType.Info, "비어있음");
                        }

                    }, false);
                    EditorGUILayout.Space(15);



                    #endregion



                    //? IK 제약조건



                    #region IK 제약조건



                    SU_CustomEditor.AutoLabelFoldOut_Head("🦿 IK 제약조건 <size=11>(<i>IKConstraintDatas)</size>", SU_CustomEditor.LabelHeadType.H2, ref IkConstraintDataInfo.Fold, () =>
                    {
                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.LabelField_TextAutoWidthHeight("ℹ️ 심화 정보 보기");
                            SU_CustomEditor.RenderField_Bool(this, ref IkConstraintDataInfo.UseDetail);
                        });

                        //. IK 제약조건 표시 함
                        if (IKConsDataEX_Dictionary.Count != 0)
                        {
                            var ikConsData_DataArray = IKConsDataEX_Dictionary.dataArray;
                            if (IkConstraintDataInfo.FilteringSkinRequireds) { ikConsData_DataArray = ikConsData_DataArray.Where(ikCons => ikCons.IkConstraintData.SkinRequired).ToArray(); }

                            //? 필터링 옵션
                            SU_CustomEditor.HorizontalGUI(() =>
                            {
                                SU_CustomEditor.LabelField_TextAutoWidthHeight("📌 스킨 전용 요소의 포함된 제약조건");
                                SU_CustomEditor.RenderField_Bool(this, ref IkConstraintDataInfo.FilteringSkinRequireds);
                            });

                            //. 요소를 각각 스크롤뷰로 출력
                            SU_CustomEditor.ScrollViewResizable_Collection(ref IkConstraintDataInfo.ScrollPos, ref IkConstraintDataInfo.ScrollHeight, 100, 400, ref IkConstraintDataInfo.SearchQuery, out var searchCount, ikConsData_DataArray,
                                (ik) =>
                                {
                                    SU_CustomEditor.VerticalHelpBox(() =>
                                    {
                                        stringBuilder.Clear();
                                        SU_InfoWriter.WriteIkConstraintDataEXInfo(stringBuilder, ik, IkConstraintDataInfo.UseDetail);
                                        SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));
                                    });
                                },
                            x => x.Name, true, GUILayout.Height(200));
                        }

                        //. IK 제약조건 표시 안함
                        else
                        {
                            SU_CustomEditor.HelpBox(MessageType.Info, "비어있음");
                        }
                    }, false);
                    EditorGUILayout.Space(15);



                    #endregion



                    //? Path 제약조건



                    #region Path 제약조건



                    SU_CustomEditor.AutoLabelFoldOut_Head("🚲 Path 제약조건 <size=11>(<i>TransformConstraintDatas)</size>", SU_CustomEditor.LabelHeadType.H2, ref PathConstraintDataInfo.Fold, () =>
                    {
                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.LabelField_TextAutoWidthHeight("ℹ️ 심화 정보 보기");
                            SU_CustomEditor.RenderField_Bool(this, ref PathConstraintDataInfo.UseDetail);
                        });

                        //. Path 제약조건 표시 함
                        if (PathConsDataEX_Dictionary.Count != 0)
                        {
                            var pathConsData_DataArray = PathConsDataEX_Dictionary.dataArray;
                            if (PathConstraintDataInfo.FilteringSkinRequireds) { pathConsData_DataArray = pathConsData_DataArray.Where(pathCons => pathCons.PathConstraintData.SkinRequired).ToArray(); }

                            //? 필터링 옵션
                            SU_CustomEditor.HorizontalGUI(() =>
                            {
                                SU_CustomEditor.LabelField_TextAutoWidthHeight("📌 스킨 전용 요소의 포함된 제약조건");
                                SU_CustomEditor.RenderField_Bool(this, ref PathConstraintDataInfo.FilteringSkinRequireds);
                            });

                            //. 요소를 각각 스크롤뷰로 출력
                            SU_CustomEditor.ScrollViewResizable_Collection(ref PathConstraintDataInfo.ScrollPos, ref PathConstraintDataInfo.ScrollHeight, 100, 400, ref PathConstraintDataInfo.SearchQuery, out var searchCount, pathConsData_DataArray,
                                (path) =>
                                {
                                    SU_CustomEditor.VerticalHelpBox(() =>
                                    {
                                        stringBuilder.Clear();
                                        SU_InfoWriter.WritePathConstraintDataEXInfo(stringBuilder, path, PathConstraintDataInfo.UseDetail);
                                        SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));
                                    });
                                },
                            x => x.Name, true, GUILayout.Height(200));
                        }

                        //. Path 제약조건 표시 안함
                        else
                        {
                            SU_CustomEditor.HelpBox(MessageType.Info, "비어있음");
                        }
                    }, false);
                    EditorGUILayout.Space(15);



                    #endregion



                    //? Physics 제약조건



                    #region Physics 제약조건



                    SU_CustomEditor.AutoLabelFoldOut_Head("🍎 Physics 제약조건 <size=11>(<i>TransformConstraintDatas)</size>", SU_CustomEditor.LabelHeadType.H2, ref PhysicsConstraintDataInfo.Fold, () =>
                    {
                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.LabelField_TextAutoWidthHeight("ℹ️ 심화 정보 보기");
                            SU_CustomEditor.RenderField_Bool(this, ref PhysicsConstraintDataInfo.UseDetail);
                        });

                        //. Physics 제약조건 표시 함
                        if (PhysicsConsDataEX_Dictionary.Count != 0)
                        {
                            var physicsConsData_DataArray = PhysicsConsDataEX_Dictionary.dataArray;
                            if (PhysicsConstraintDataInfo.FilteringSkinRequireds) { physicsConsData_DataArray = physicsConsData_DataArray.Where(phayicsCons => phayicsCons.PhysicsConstraintData.SkinRequired).ToArray(); }

                            //? 필터링 옵션
                            SU_CustomEditor.HorizontalGUI(() =>
                            {
                                SU_CustomEditor.LabelField_TextAutoWidthHeight("📌 스킨 전용 요소의 포함된 제약조건");
                                SU_CustomEditor.RenderField_Bool(this, ref PhysicsConstraintDataInfo.FilteringSkinRequireds);
                            });

                            //. 요소를 각각 스크롤뷰로 출력
                            SU_CustomEditor.ScrollViewResizable_Collection(ref PhysicsConstraintDataInfo.ScrollPos, ref PhysicsConstraintDataInfo.ScrollHeight, 100, 400, ref PhysicsConstraintDataInfo.SearchQuery, out var searchCount, physicsConsData_DataArray,
                                (physics) =>
                                {
                                    SU_CustomEditor.VerticalHelpBox(() =>
                                    {
                                        stringBuilder.Clear();
                                        SU_InfoWriter.WritePhysicsConstraintDataEXInfo(stringBuilder, physics, PhysicsConstraintDataInfo.UseDetail);
                                        SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));
                                    });
                                },
                            x => x.Name, true, GUILayout.Height(200));
                        }

                        //. Physics 제약조건 표시 안함
                        else
                        {
                            SU_CustomEditor.HelpBox(MessageType.Info, "비어있음");
                        }
                    }, false);
                    EditorGUILayout.Space(15);



                    #endregion



                    //? 스파인 이벤트



                    #region SpineEvent 스파인 이벤트



                    SU_CustomEditor.AutoLabelFoldOut_Head("🍆 스파인 이벤트 <size=11>(<i>SpineEvents)</size>", SU_CustomEditor.LabelHeadType.H2, ref EventConstraintDataInfo.Fold, () =>
                    {
                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.LabelField_TextAutoWidthHeight("ℹ️ 심화 정보 보기");
                            SU_CustomEditor.RenderField_Bool(this, ref EventConstraintDataInfo.UseDetail);
                        });

                        //. 스파인 이벤트 표시 함
                        if (SpineEvent_Dictionary.Count != 0)
                        {
                            //. 요소를 각각 스크롤뷰로 출력
                            SU_CustomEditor.ScrollViewResizable_Collection(ref EventConstraintDataInfo.ScrollPos, ref EventConstraintDataInfo.ScrollHeight, 100, 400, ref EventConstraintDataInfo.SearchQuery, out var searchCount, SpineEvent_Dictionary.dataArray,
                                (spineEvent) =>
                                {
                                    SU_CustomEditor.VerticalHelpBox(() =>
                                    {
                                        stringBuilder.Clear();
                                        SU_InfoWriter.WriteSpineEventDataInfo(stringBuilder, spineEvent, EventConstraintDataInfo.UseDetail, Target);
                                        SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));
                                    });
                                },
                            x => x.Name, true, GUILayout.Height(200));
                        }

                        //. 스파인 이벤트 표시 안함
                        else
                        {
                            SU_CustomEditor.HelpBox(MessageType.Info, "비어있음");
                        }
                    }, false);
                    EditorGUILayout.Space(15);



                    #endregion

                });
            });
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 계층적 경로를 정렬하는 비교자입니다. (Spine의 BonePath 정렬 방식)
        /// </summary>
        public sealed class HierarchicalPathComparer : IComparer<string[]>
        {
            /// <summary>
            /// 계층적 경로를 정렬하는 비교 로직
            /// </summary>
            public int Compare(string[] x, string[] y)
            {
                //! null 체크
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;

                //? 두 경로 중 더 짧은 깊이까지만 비교
                int minDepth = Math.Min(x.Length, y.Length);
                for (int i = 0; i < minDepth; i++)
                {
                    int cmp = string.Compare(x[i], y[i], StringComparison.Ordinal);
                    if (cmp != 0)
                        return cmp; //? 해당 계층에서 정렬이 결정됨
                }

                //! 동일한 경로일 경우, 더 짧은 경로가 앞에 오도록 정렬
                return x.Length.CompareTo(y.Length);
            }

            /// <summary>
            /// 싱글톤 인스턴스 (멀티스레드 안전)
            /// </summary>
            public static HierarchicalPathComparer Instance => _instance ??= new();
            private static HierarchicalPathComparer _instance;
        }



        /// <summary>
        /// 특정 타입 T의 문자열 필드를 계층적으로 정렬하는 비교자입니다.
        /// </summary>
        /// <typeparam name="T">비교할 객체의 타입</typeparam>
        public sealed class HierarchicalFieldComparer<T> : IComparer<T>
        {
            private readonly Func<T, string> _keySelector;

            /// <summary>
            /// 생성자: 비교할 문자열 필드를 지정합니다.
            /// </summary>
            /// <param name="keySelector">정렬 기준이 될 문자열을 반환하는 함수</param>
            public HierarchicalFieldComparer(Func<T, string> keySelector)
            {
                _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
            }

            /// <summary>
            /// 객체 T의 특정 필드를 계층적으로 정렬합니다.
            /// </summary>
            public int Compare(T x, T y)
            {
                //! null 체크
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;

                string xValue = _keySelector(x) ?? string.Empty;
                string yValue = _keySelector(y) ?? string.Empty;

                //! string[]로 변환하여 계층 비교
                string[] xTokens = xValue.Split('/');
                string[] yTokens = yValue.Split('/');

                return HierarchicalPathComparer.Instance.Compare(xTokens, yTokens);
            }
        }



        ///======================================================================================================================================================
    }


    [CanEditMultipleObjects]
    [CustomEditor(typeof(SkelSbject), true)]
    public class SkelSbjectEditor<TSkelSbject> : SkelSbjectEditor where TSkelSbject : SkelSbject, new()
    {
        protected override void Draw_AdvancedSkin()
        {
            base.Draw_AdvancedSkin();
        }
    }
}
