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
using Sirenix.OdinInspector.Editor;
using System.Runtime.CompilerServices;
using Sirenix.Utilities.Editor;
using Sirenix.Utilities;
using System.Net;
using JetBrains.Annotations;


namespace Pan.SpinePackage.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SkelObject), true)]
    public partial class SkelObjectEditor : EditorExpand_InspectorGUI<SkelObject>, SkelObject.Observers.IS.IAniStart
    {
        ///======================================================================================================================================================



        private SkeletonAnimation skeletonAnimation;



        private static StringBuilder stringBuilder => _stringBuilder ??= new StringBuilder();
        private static StringBuilder _stringBuilder;



        protected override EDrawDefaultInspectorMode? CurrentMode_DrawDefaultInspector_Fixed => EDrawDefaultInspectorMode.OdinVisible;



        ///======================================================================================================================================================



        protected override void Awake()
        {
            base.Awake();
            skeletonAnimation = Target.GetComponent<SkeletonAnimation>();
        }



        protected override void Refresh_OnEditorAllListChanged(List<EditorExpand<SkelObject>> editors)
        {
            foreach (var item in editors)
            {
                Target.Observer.RemoveOB(item as SkelObject.Observers.IS);
            }

            Target.Observer?.AddOB(this);
        }



        private void OnDestroy()
        {
            Target.Observer?.RemoveOB(this);
        }



        protected override void OnEnable()
        {
            base.OnEnable();
            skeletonAnimation = Target.GetComponent<SkeletonAnimation>();
        }



        protected override void OnDisable()
        {
            base.OnDisable();
        }



        protected override void OnInspectorGUI_Current()
        {
            base.OnInspectorGUI_Current();


            if (Target.Skel != null && Target.Skel.CoreEnabled)
            {
                OnInspectorGUI_SkelCore(Target.Skel);
            }


            if (Target.Ani != null && Target.Ani.CoreEnabled)
            {
                OnInspectorGUI_AniCore(Target.Ani);
            }
        }



        ///======================================================================================================================================================



        //? SkelCore 그리기
        private void OnInspectorGUI_SkelCore(SkelObject.SkelCore skelCore)
        {
            SU_CustomEditor.AutoLabelField_Head("💀 Skel Core", SU_CustomEditor.LabelHeadType.H1, () =>
            {
                if (skelCore.RunTimeSkins != null) DrawRuntimeSkinManager(this, skelCore.RunTimeSkins, Target.CurrentDB, Target.CurrentDB);
                if (skelCore.RunTimeBones != null) DrawRunTimeBoneManager(skelCore.RunTimeBones);
                if (skelCore.RunTimeSlots != null) DrawRunTimeSlotManager(skelCore.RunTimeSlots, Target.CurrentDB);
            });
        }



        //? 런타임 스킨 매니저
        public static void DrawRuntimeSkinManager<TEditor, TSkin>(TEditor editor, BaseRunTimeSkinManager<TSkin> runTimeSkinManager, SkelSbject.IHoldEnumIndex_SkinLayer holdEnumIndexSkinLayer, SkelSbject skelSbject = null) where TEditor : UnityEditor.Editor
        {
            SU_CustomEditor.AutoLabelField_Head("👕 런타임 스킨 매니저", SU_CustomEditor.LabelHeadType.H2, () =>
            {
                SkelObject.SkelCore.RunTimeSkinManager runTimeSkinManager_SkelObject = runTimeSkinManager as SkelObject.SkelCore.RunTimeSkinManager;


                //? SkelObject 런타임 스킨 매니저이고, SkelSbject를 받아왔다면 어드밴스드 스킨 강제적용 요소들을 그린다
                if (runTimeSkinManager_SkelObject != null && skelSbject != null && skelSbject is SkelSbject.IHoldAdvancedSkinManager skelSbject_HoldAdvancedSkinManger)
                {
                    SU_CustomEditor.VerticalHelpBox(() =>
                    {
                        SU_CustomEditor.AutoLabelField_Head("👕➕ Advanced 스킨 강제 적용", SU_CustomEditor.LabelHeadType.H3, () =>
                        {
                            SU_CustomEditor.Render_Button(editor, "👕➕ Advanced 스킨 선택", () =>
                            {
                                var advancedSkinNames = skelSbject_HoldAdvancedSkinManger.GetAdvancedSkinArray.Select(x => x.GetType().Name).ToList();
                                SU_CustomEditor.DropDownMenuEvent(advancedSkinNames, (selectAdvancedSkinName) =>
                                {
                                    var selectedOneIndex = advancedSkinNames.IndexOf(selectAdvancedSkinName);
                                    var advancedSkin = skelSbject_HoldAdvancedSkinManger.GetAdvancedSkinArray[selectedOneIndex];

                                    Debug.Log($"<b>{editor.target.name}</b>의 Advanced 스킨 강제적용: <color=#4fc1e9>{advancedSkin.GetType().Name}</color>");
                                    runTimeSkinManager_SkelObject.SetMainAdvancedSkin(advancedSkin);
                                }, "👕➕ Advanced 스킨 선택");
                            });
                        }, false);
                    });
                }



                //? 스킨 강제 Mix 적용
                SU_CustomEditor.VerticalHelpBox(() =>
                {
                    SU_CustomEditor.AutoLabelField_Head("👕🔓 Spine.Skin 강제 Mix 적용", SU_CustomEditor.LabelHeadType.H3, () =>
                    {
                        SU_CustomEditor.Render_Button(editor, "👕🔓 Spine.Skin 선택", () =>
                        {
                            var skinNames = skelSbject.Skin_Dictioinary.dataArray.Select(x => x.Name).ToList();
                            SU_CustomEditor.DropDownMenuEvent(skinNames, (selectSkinName) =>
                            {
                                var skin = skelSbject.Skin_Dictioinary[selectSkinName];
                                Debug.Log($"<b>{editor.target.name}</b>의 Skin 강제 Mix 적용: <color=#4fc1e9>{selectSkinName}</color>");
                                runTimeSkinManager.AddMixSkinRunTime_Force(skin);
                            }, "👕🔓 Spine.Skin 선택");
                        });

                    }, false);
                });



                //? MainSkin 정보 띄우기
                SU_CustomEditor.VerticalHelpBox(() =>
                {
                    EditorGUILayout.Space();
                    if (runTimeSkinManager.MainSkin != null)
                    {
                        SU_CustomEditor.LabelField_TextAutoWidthHeight($"👕 <b>메인 스킨</b>: <color=#ac92ec>{runTimeSkinManager.MainSkin.Name}</color>");
                    }
                    else
                    {
                        SU_CustomEditor.LabelField_TextAutoWidthHeight($"👕 <b>메인 스킨</b>: <color=#ed5565>null</color>");
                    }
                    EditorGUILayout.Space();
                });


                //? SkelObject 런타임 스킨 매니저라면, Advanced 스킨 정보 띄우기
                if (runTimeSkinManager_SkelObject != null)
                {
                    SU_CustomEditor.VerticalHelpBox(() =>
                    {
                        EditorGUILayout.Space();

                        if (runTimeSkinManager_SkelObject.MainAdvancedSkin != null)
                        {
                            SU_CustomEditor.LabelField_TextAutoWidthHeight($"👕➕ <b>Advanced 스킨</b>: <color=#ac92ec>{runTimeSkinManager_SkelObject.MainAdvancedSkin.GetType().Name}</color>");
                        }
                        else
                        {
                            SU_CustomEditor.LabelField_TextAutoWidthHeight($"👕➕ <b>Advanced 스킨</b>: <color=#ed5565>null</color>");
                        }

                        EditorGUILayout.Space();
                    });
                }


                //? Mix 스킨 정보 띄우기
                SU_CustomEditor.VerticalHelpBox(() =>
                {
                    SU_CustomEditor.AutoLabelField_Head("🥞Mix 스킨 정보", SU_CustomEditor.LabelHeadType.H3, () =>
                    {
                        if (runTimeSkinManager.GetCurrentMixSkins.Count != 0)
                        {
                            SU_CustomEditor.LabelField_TextAutoWidthHeight($"Mix 스킨 개수: <color=#2ecc71><b>{runTimeSkinManager.GetCurrentMixSkins.Count}</b></color>");

                            EditorGUILayout.Space();

                            //? 믹스스킨들의 내림차순 정렬
                            var mixSkins = runTimeSkinManager.GetCurrentMixSkins.OrderByDescending(x => x.Key);

                            foreach (var item in mixSkins)
                            {
                                var skin = SU_SpinePackage.GetSkin_UnknownTypeSkin(item.Value);

                                string customSkinIndexName = "";

                                if (holdEnumIndexSkinLayer != null)
                                {
                                    customSkinIndexName = holdEnumIndexSkinLayer.EnumIndex_SkinLayers.GetEnumName(item.Key);
                                }

                                SU_CustomEditor.VerticalHelpBox(() =>
                                {
                                    SU_CustomEditor.HorizontalGUI(() =>
                                    {
                                        if (customSkinIndexName == "")
                                        {
                                            SU_CustomEditor.LabelField_Text($"🥞 <b><color=#2ecc71>{item.Key:000}</color></b>:", GUILayout.Width(125));
                                        }
                                        else
                                        {
                                            SU_CustomEditor.LabelField_Text($"{$"🥞 <b><color=#ac92ec>{customSkinIndexName}</color></b>"} (<b><color=#2ecc71>{item.Key:000}</color></b>):", GUILayout.Width(125));
                                        }

                                        SU_CustomEditor.LabelField_TextAutoWidthHeight($"{skin.Name}");
                                    });


                                });
                            }
                        }
                        else
                        {
                            SU_CustomEditor.VerticalHelpBox(() =>
                            {
                                EditorGUILayout.Space();
                                SU_CustomEditor.LabelField_TextAutoWidthHeight($"<color=#ed5565><b>Mix 스킨이 존재하지않음</b></color>");
                                EditorGUILayout.Space();
                            });
                        }
                    }, false);
                });
            }, true);
        }



        protected bool RunTimeBones_Fold;
        protected ESelectActivesMode RunTimeBones_FilteringMode;
        protected bool RunTimeBones_ViewOnlyOverrideBone;
        protected bool RunTimeBones_ViewDetail;



        //? 런타임 본 매니저
        private void DrawRunTimeBoneManager(SkelObject.SkelCore.RunTimeBoneManager runTimeBoneManager)
        {
            SU_CustomEditor.AutoLabelField_Head("🦴 런타임 Bone 매니저", SU_CustomEditor.LabelHeadType.H2, () =>
            {
                var runTimeBones = runTimeBoneManager.GetRunTimeBones;
                var runTimeBones_OnlyActive = runTimeBones.Where(x => x.Value.BaseBone.Active);
                int runTimeBones_OnlyActive_Count = runTimeBones_OnlyActive.Count();


                SU_CustomEditor.HorizontalGUI(() =>
                {
                    SU_CustomEditor.LabelField_TextAutoWidthHeight($"<b>총 Bone 개수</b>:\t<color=#2ecc71><b>{runTimeBones.Count}</b></color> <b>|</b> <color=#4fc1e9><b>{runTimeBones_OnlyActive_Count}</b></color> <b>/</b> <color=#ed5565><b>{runTimeBones.Count - runTimeBones_OnlyActive_Count}</b></color>");
                    //SU_CustomEditor.LabelField_Text($"<color=#2ecc71><b>{runTimeBones.Count}</b></color> <b>|</b> <color=#4fc1e9><b>{runTimeBones_OnlyActive_Count}</b></color> <b>/</b> <color=#ed5565><b>{runTimeBones.Count - runTimeBones_OnlyActive_Count}</b></color>");
                });


                SU_CustomEditor.AutoLabelFoldOut_Head("🦴 런타임 Bone 보기", SU_CustomEditor.LabelHeadType.H3, ref RunTimeBones_Fold, () =>
                {
                    SU_CustomEditor.RenderField_Enum(this, ref RunTimeBones_FilteringMode, "📌 필터 모드");
                    SU_CustomEditor.RenderField_Bool(this, ref RunTimeBones_ViewOnlyOverrideBone, "🛠️ 재정의된 본만 보기");
                    SU_CustomEditor.RenderField_Bool(this, ref RunTimeBones_ViewDetail, "ℹ️ 심화 보기");

                    EditorGUILayout.Space();
                    SU_CustomEditor.VerticalHelpBox(() =>
                    {
                        stringBuilder.Clear();


                        foreach (var item in runTimeBones)
                        {
                            //? 재정의된 본만 보기
                            if (RunTimeBones_ViewOnlyOverrideBone && (!item.Value.UseOverride_Rotation && !item.Value.UseOverride_PositionSkeletonSpace))
                            {
                                continue;
                            }


                            //? 본 기본 출력
                            switch (RunTimeBones_FilteringMode)
                            {
                                case ESelectActivesMode.All:

                                if (item.Value.BaseBone.Active)
                                {
                                    stringBuilder.AppendLine($"🦴 <color=#4fc1e9>{item.Key}</color>");
                                }
                                else
                                {
                                    stringBuilder.AppendLine($"🦴 <color=#ed5565>{item.Key}</color>");
                                }

                                break;

                                case ESelectActivesMode.OnlyEnable:

                                if (item.Value.BaseBone.Active)
                                {
                                    stringBuilder.AppendLine($"🦴 <color=#4fc1e9>{item.Key}</color>");
                                }

                                break;

                                case ESelectActivesMode.OnlyDisable:

                                if (!item.Value.BaseBone.Active)
                                {
                                    stringBuilder.AppendLine($"🦴 <color=#ed5565>{item.Key}</color>");
                                }

                                break;
                            }


                            //? 본 상세정보 보기
                            if (item.Value.BaseBone.Active && RunTimeBones_ViewDetail)
                            {
                                if (!item.Value.UseOverride_Rotation)
                                {
                                    stringBuilder.AppendLine($"\tRotation: <color=#2ecc71>{item.Value.Rotation}</color>");
                                }
                                else
                                {
                                    stringBuilder.AppendLine($"\t🛠️ <color=#ed5565>Rotation</color>: <color=#2ecc71><b>{item.Value.Rotation_Override}</b></color> | 원본: (<color=#2ecc71><i>{item.Value.Rotation}</i></color>)");
                                }

                                if (!item.Value.UseOverride_PositionSkeletonSpace)
                                {
                                    stringBuilder.AppendLine($"\tPositionSkeletonSpace: <color=#2ecc71>{item.Value.PositionSkeletonSpace()}</color>");
                                }
                                else
                                {
                                    stringBuilder.AppendLine($"\t🛠️ <color=#ed5565>PositionSkeletonSpace</color>: <color=#2ecc71><b>{item.Value.PositionSkeletonSpace_Override}</b></color> | 원본: (<color=#2ecc71><i>{item.Value.PositionSkeletonSpace()}</i></color>)");
                                }
                            }


                        }


                        SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));

                    });
                }, false);


            }, true
            );
        }



        protected bool RunTimeSlot_TagSlots_Fold;
        protected bool RunTimeSlot_Slot_Fold;
        protected bool RunTimeSlot_CustomDrawOrder_Fold;
        protected bool RunTimeSlot_ViewDrawOrderSlot_DefaultSlotOrder;
        protected List<SkelObject.SkelCore.RunTimeSlotManager.BaseCustomDrawOrderCommand> RunTimeSlot_CachingLists = new List<SkelObject.SkelCore.RunTimeSlotManager.BaseCustomDrawOrderCommand>();



        //? 런타임 슬롯 매니저
        private void DrawRunTimeSlotManager(SkelObject.SkelCore.RunTimeSlotManager runTimeSlotManager, SkelSbject skelSbject)
        {
            SU_CustomEditor.AutoLabelField_Head("🔘 런타임 슬롯 매니저", SU_CustomEditor.LabelHeadType.H2, () =>
            {
                SU_CustomEditor.LabelField_TextAutoWidthHeight($"🔘 <b>슬롯 총 개수</b>:\t<color=#2ecc71><b>{runTimeSlotManager.GetSlots.Length}</b></color>");
                EditorGUILayout.Space();
                SU_CustomEditor.AutoLabelFoldOut_Head("🔘 런타임 드로우 오더 슬롯 보기", SU_CustomEditor.LabelHeadType.H3, ref RunTimeSlot_Slot_Fold, () =>
                {
                    EditorGUILayout.Space();
                    SU_CustomEditor.HorizontalGUI(() =>
                    {
                        SU_CustomEditor.LabelField_Text("📌 기본 슬롯 순서로 보기", GUILayout.Width(200));
                        SU_CustomEditor.RenderField_Bool(this, ref RunTimeSlot_ViewDrawOrderSlot_DefaultSlotOrder);
                    });
                    EditorGUILayout.Space();

                    stringBuilder.Clear();


                    //? 런타임 슬롯을 드로우 오더로 보기
                    if (!RunTimeSlot_ViewDrawOrderSlot_DefaultSlotOrder)
                    {
                        for (int i = runTimeSlotManager.GetSlots.Length - 1; i >= 0; i--)
                        {
                            var currentSlot_DrawOrdered = runTimeSlotManager.GetDrawOrderSlots[i];
                            var currentSlotData = skelSbject.GetSlotData(currentSlot_DrawOrdered.Data.Name);

                            //stringBuilder.AppendLine($"🔖\t<color=#2ecc71><b>{i:000}</b></color>\t(<color=#f7da64>{currentSlotData.Index:000}</color>)\t🔘 {currentSlot_DrawOrdered.Data.Name}");

                            RunTimeSlot_CachingLists.Clear();
                            if (runTimeSlotManager.CustomDrawOrderFindAndGet_Commands(currentSlot_DrawOrdered.Data.Name, RunTimeSlot_CachingLists))
                            {
                                //stringBuilder.AppendLine($"🔖\t<color=#2ecc71><b>{i:000}</b></color>\t(<color=#f7da64>{currentSlotData.Index:000}</color>)\t🔘 <b><color=#ed5565>{currentSlot_DrawOrdered.Data.Name}</color></b> (커스텀 드로우오더 적용됨: <color=#2ecc71>{RunTimeSlot_CachingLists.Count}</color></b>)");
                                stringBuilder.AppendLine($"🔖\t<color=#2ecc71><b>{i:000}</b></color>\t(<color=#f7da64>{currentSlotData.Index:000}</color>)\t🔘 <b><color=#ed5565>{currentSlot_DrawOrdered.Data.Name}</color></b>");
                                stringBuilder.AppendLine($"\t\t\t<i>(커스텀 드로우오더 적용됨</i>: <color=#2ecc71><b>{RunTimeSlot_CachingLists.Count}</b></color>)");

                                //stringBuilder.AppendLine($"\t\t\t🖼️ <b><color=#ed5565>Used CustomDrawOrder</color>: <color=#2ecc71>{RunTimeSlot_CachingLists.Count}</color></b>");
                                for (int j = 0; j < RunTimeSlot_CachingLists.Count; j++)
                                {
                                    var item = RunTimeSlot_CachingLists[j];
                                    stringBuilder.AppendLine($"\t\t\t\t<color=#2ecc71><b>{j + 1}</b></color> | <color=#ac92ec>{item.EventName}</color>");
                                }
                            }
                            else
                            {
                                stringBuilder.AppendLine($"🔖\t<color=#2ecc71><b>{i:000}</b></color>\t(<color=#f7da64>{currentSlotData.Index:000}</color>)\t🔘 {currentSlot_DrawOrdered.Data.Name}");
                            }
                        }
                    }


                    //? 런타임 슬롯을 기본 슬롯 순서로 보기
                    else
                    {
                        for (int i = runTimeSlotManager.GetSlots.Length - 1; i >= 0; i--)
                        {
                            var currentSlot = runTimeSlotManager.GetSlots[i];

                            stringBuilder.AppendLine($"🔖\t<color=#2ecc71><b>{i:000}</b></color>\t⚪ {currentSlot.Data.Name}");
                        }
                    }


                    SU_CustomEditor.VerticalHelpBox(() =>
                    {
                        SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));
                    });


                }, false);


                SU_CustomEditor.AutoLabelFoldOut_Head("🖼️ 커스텀 드로우 오더 이벤트", SU_CustomEditor.LabelHeadType.H3, ref RunTimeSlot_CustomDrawOrder_Fold, () =>
                {
                    SU_CustomEditor.VerticalHelpBox(() =>
                    {
                        EditorGUILayout.Space();

                        stringBuilder.Clear();

                        SU_CustomEditor.LabelField_TextAutoWidthHeight($"<b>적용중인 커스텀 드로우 오더 버킷 개수: <color=#2ecc71>{runTimeSlotManager.GetCustomDrawOrderExecutes_Buckets.Count}</color></b>");

                        if (runTimeSlotManager.GetCustomDrawOrderExecutes_Buckets.Count != 0)
                        {
                            //stringBuilder.AppendLine($"<b>적용중인 커스텀 드로우 오더 버킷 개수: <color=#2ecc71>{runTimeSlotManager.GetCustomDrawOrderExecutes_Buckets.Count}</color></b>");

                            for (int i = 0; i < runTimeSlotManager.GetCustomDrawOrderExecutes_Buckets.Count; i++)
                            {
                                IReadOnlyList<SkelObject.SkelCore.RunTimeSlotManager.BaseCustomDrawOrderCommand> item = runTimeSlotManager.GetCustomDrawOrderExecutes_Buckets[i];

                                stringBuilder.AppendLine($"\t<b><color=#2ecc71>{i}</color>번 버킷</b>");

                                for (int j = 0; j < item.Count; j++)
                                {
                                    stringBuilder.AppendLine($"\t\t<color=#2ecc71><b>{i + 1}</b></color>\t이벤트 이름: <color=#f7da64><b>{item[j].EventName}</b></color>");
                                    stringBuilder.AppendLine($"\t\t\t대상 슬롯: <color=#ac92ec>{item[j].TargetSlotName}</color>");
                                }
                            }
                        }
                        else
                        {
                            stringBuilder.AppendLine("적용중인 커스텀 드로우 오더 이벤트가 없음");
                        }

                        SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));
                        EditorGUILayout.Space();
                    });
                }, false);


                SU_CustomEditor.AutoLabelFoldOut_Head("📑 태그 슬롯 보기", SU_CustomEditor.LabelHeadType.H3, ref RunTimeSlot_TagSlots_Fold, () =>
                {
                    if (runTimeSlotManager.GetTagSlots != null && runTimeSlotManager.GetTagSlots.Count > 0)
                    {
                        EditorGUILayout.Space();

                        SU_CustomEditor.LabelField_TextAutoWidthHeight($"총 태그 개수: <color=#2ecc71><b>{runTimeSlotManager.GetTagSlots.Count}</b></color>");

                        EditorGUILayout.Space();

                        SU_CustomEditor.VerticalHelpBox(() =>
                        {
                            stringBuilder.Clear();

                            foreach (var item in runTimeSlotManager.GetTagSlots)
                            {
                                stringBuilder.AppendLine($"📑 <b><color=#ac92ec>{item.Key}</color></b> | {((item.Value != null) ? $"<color=#2ecc71>{item.Value.Count.ToString()}</color>" : "<color=red>null??</color>")}");
                                stringBuilder.AppendLine();
                                for (int i = 0; i < item.Value.Count; i++)
                                {
                                    Slot slot = item.Value[i];
                                    stringBuilder.AppendLine($"<color=#2ecc71><b>{i}</b></color>\t⚪ {slot.Data.Name}");

                                    //if (i < item.Value.Count - 1) stringBuilder.AppendLine(",");
                                    //else stringBuilder.AppendLine();
                                }
                            }

                            SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));

                            stringBuilder.Clear();
                        });
                    }
                }, false);
            }, false
            );
        }



        ///======================================================================================================================================================



        //? AniCore 그리기
        private void OnInspectorGUI_AniCore(SkelObject.AniCore aniCore)
        {
            SU_CustomEditor.AutoLabelField_Head("🅰️ Ani Core", SU_CustomEditor.LabelHeadType.H1, () =>
            {
                if (aniCore.Setting != null) DrawAniSetting(aniCore.Setting);
                if (aniCore.Player != null) DrawAniPlayer(aniCore, aniCore.Player, Target.CurrentDB);
            });
        }



        //? 애니 설정
        private void DrawAniSetting(SkelObject.AniCore.Settings aniCoreSetting)
        {
            var enumIndex_AniTrack = aniCoreSetting.EnumIndex_AniTrack;
            var enumIndex_AniRank = aniCoreSetting.EnumIndex_AniRank;


            SU_CustomEditor.AutoLabelField_Head("⚙️ 애니 설정", SU_CustomEditor.LabelHeadType.H2, () =>
            {
                SU_CustomEditor.VerticalHelpBox(() =>
                {
                    SU_CustomEditor.LabelField_TextAutoWidthHeight($"기본 애니메이션 MixDuration: <color=#2ecc71><b>{aniCoreSetting.DefaultAnimationMixDuration}</b></color>");
                });


                SU_CustomEditor.VerticalHelpBox(() =>
                {
                    SU_CustomEditor.LabelField_TextAutoWidthHeight("트랙이 비워져 <b>EmptyAnimation</b>이 실행될때,\n<b>별도의 MixDuration를 사용하는 트랙 목록</b>");

                    if (aniCoreSetting.GetEmptyAnimation_MixDurationDictionary.Count != 0)
                    {
                        stringBuilder.Clear();

                        stringBuilder.AppendLine($"총 개수: <color=#2ecc71>{aniCoreSetting.GetEmptyAnimation_MixDurationDictionary.Count}</color>");

                        foreach (var item in aniCoreSetting.GetEmptyAnimation_MixDurationDictionary)
                        {
                            stringBuilder.AppendLine($"📼\t<color=#f7da64>{enumIndex_AniTrack.GetEnumName(item.Key)}</color>\t(<color=#2ecc71>{item.Key}</color>):\t<color=#2ecc71><b>{item.Value}</b></color>");
                        }

                        SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));
                    }
                    else
                    {
                        SU_CustomEditor.LabelField_TextAutoWidthHeight("\t사용하지 않음");
                    }
                });


            }, true);
        }



        protected bool ExecuteSkelAnimationForce_UseCustomTrackIndex;
        protected int ExecuteSkelAnimationForce_CustomTrackIndex;
        protected bool ExecuteSkelAnimationForce_UseAbsoluteOverlap;
        protected bool ExecuteSkelAnimationForce_UseCustomLoop;
        protected bool ExecuteSkelAnimationForce_CustomLoop;
        protected float EndSkelAnimationForce_MixDuration;


        protected int ExecuteSpineAnimationForce_TrackIndex;
        protected bool ExecuteSpineAnimationForce_Loop;
        protected float ExecuteSpineAnimationForce_MixDuration;
        protected float EndSpineAnimationForce_MixDuration;


        protected  bool AniPlayer_PlayingSpineAnimations_Fold;
        protected  bool AniPlayer_PlayingSpineAnimations_ViewNullTrackEntrys;
        protected  bool AniPlayer_PlayingSpineAnimations_ViewTrackEnumName;
        protected  bool AniPlayer_PlayingTracksInfo_Fold;
        protected  bool AniPlayer_PlayingTracks_Fold;
        protected  bool AniPlayer_PlayingTracksFiltering_AllTracks;
        protected  string AniPlayer_PlayingTracksFiltering_TrackIndex;
        protected readonly Dictionary<int, bool> PlayingAniTrack_Fold_Dictionary = new Dictionary<int, bool>();
        protected readonly Dictionary<int, bool> PlayingAniTrack_Memo_Fold_Dictionary = new Dictionary<int, bool>();
        protected readonly Dictionary<int, bool> PlayingAniTrack_MemoEX_Fold_Dictionary = new Dictionary<int, bool>();


        protected bool ViewRunTimePlayingSkelAni_Fold;



        //? 애니 플레이어
        private void DrawAniPlayer(SkelObject.AniCore aniCore, SkelObject.AniCore.Players aniCorePlayer, SkelSbject skelSbject)
        {
            SU_CustomEditor.AutoLabelField_Head("📺 애니 플레이어", SU_CustomEditor.LabelHeadType.H2, () =>
            {
                stringBuilder.Clear();


                //? SkelAni 강제 실행
                SU_CustomEditor.VerticalHelpBox(() =>
                {
                    SU_CustomEditor.AutoLabelField_Head("💣🅰️ SkelAni 강제 실행", SU_CustomEditor.LabelHeadType.H3, () =>
                    {
                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.RenderField_Bool(this, ref ExecuteSkelAnimationForce_UseCustomTrackIndex, "💽 CustomTrackIndex 사용", "");
                            GUILayout.FlexibleSpace();
                            SU_CustomEditor.ActiveGUICondition(ExecuteSkelAnimationForce_UseCustomTrackIndex, () =>
                            {
                                SU_CustomEditor.RenderField_Int(this, ref ExecuteSkelAnimationForce_CustomTrackIndex, "", "", GUILayout.MinWidth(30));
                            });
                        });


                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.RenderField_Bool(this, ref ExecuteSkelAnimationForce_UseAbsoluteOverlap, "📚 강제 종료 후 실행");
                        });


                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            SU_CustomEditor.RenderField_Bool(this, ref ExecuteSkelAnimationForce_UseCustomLoop, "🔁 강제 Loop 조작");
                            SU_CustomEditor.ActiveGUICondition(ExecuteSkelAnimationForce_UseCustomLoop, () =>
                            {
                                SU_CustomEditor.RenderField_Bool(this, ref ExecuteSkelAnimationForce_CustomLoop, "🔁 강제 Loop");
                            });
                        });


                        SU_CustomEditor.Render_Button(this, "🅰️ SkelAni 선택", () =>
                        {
                            List<string> skelAniNameList_All = new List<string>(); //. 드롭다운에 표시될 스킨들의 이름 리스트


                            List<(string Name, SkelAni SkelAni)> skelAniList_AniBox = null; //. AniBox에서 추출한 SkelAni들


                            Dictionary<string, SkelAni> cachedSkelAnis_AniBox = null; //. AniBox에서 추출한 SkelAni들의 캐싱
                            Dictionary<string, Dictionary<string, SkelAni>> cachedSkelAnis_AdvancedSkins = null; //. AdvancedSkins들에서 추출한 SkelAni들의 캐싱
                                                                                                                 //. Key: 어드밴스드 스킨 타입 / Value-Key: SkelAni 애니메이션 이름 / Value-Value: SkelAni 객체


                            //? AniBox 등록
                            if (skelSbject is SkelSbject.IHoldAniBox holdAniBox)
                            {
                                skelAniList_AniBox = SkelSbjectEditor.AniBoxInfos.GetSkelAniReflections(holdAniBox.AniBox);
                                cachedSkelAnis_AniBox = new Dictionary<string, SkelAni>();

                                for (int i = 0; i < skelAniList_AniBox.Count; i++)
                                {
                                    //? SkelAni이름, SkelAni를 캐싱한다
                                    cachedSkelAnis_AniBox.Add(skelAniList_AniBox[i].Name, skelAniList_AniBox[i].SkelAni);
                                }
                            }


                            //? AdvancedSkins 등록
                            if (skelSbject is SkelSbject.IHoldAdvancedSkinManager holdAdvancedSkinManager)
                            {
                                var advancedSkinManager = holdAdvancedSkinManager.GetAdvancedSkinArray;

                                cachedSkelAnis_AdvancedSkins = new Dictionary<string, Dictionary<string, SkelAni>>(advancedSkinManager.Length);


                                for (int i = 0; i < advancedSkinManager.Length; i++)
                                {
                                    var currentAdvancedSkinManager = advancedSkinManager[i];
                                    var currentAdvancedSkinName = currentAdvancedSkinManager.GetType().Name; //. 어드밴스드스킨의 타입의 이름 문자열

                                    //. 현재 순회의 어드밴스드 스킨의 SkelAni들
                                    List<(string Name, SkelAni SkelAni)> skelAniList_AdvancedSkin = SkelSbjectEditor.AniBoxInfos.GetSkelAniReflections(currentAdvancedSkinManager);


                                    cachedSkelAnis_AdvancedSkins.Add(currentAdvancedSkinName, new Dictionary<string, SkelAni>(skelAniList_AdvancedSkin.Count));


                                    for (int j = 0; j < skelAniList_AdvancedSkin.Count; j++)
                                    {
                                        var currnetSkelAni_AdvancedSkin = skelAniList_AdvancedSkin[j];

                                        //? "어드밴스드스킨/SkelAni이름" 방식으로 드롭다운에 띄운다
                                        skelAniNameList_All.Add($"{currentAdvancedSkinName}/{currnetSkelAni_AdvancedSkin.Name}");

                                        //? SkelAni이름, SkelAni를 캐싱한다
                                        cachedSkelAnis_AdvancedSkins[currentAdvancedSkinName].Add(currnetSkelAni_AdvancedSkin.Name, currnetSkelAni_AdvancedSkin.SkelAni);
                                    }
                                }
                            }


                            //? AniBox들의 드롭다운 이름들을 나중에 추가하여, 어드밴스드스킨이 최상단에 오게끔 한다
                            if (skelAniList_AniBox != null)
                            {
                                skelAniNameList_All.AddRange(skelAniList_AniBox.Select(x => x.Name));
                            }


                            SU_CustomEditor.DropDownMenuEvent(skelAniNameList_All, (selectSkelAniName) =>
                            {
                                //? 슬래시가들어간, 어드밴스드스킨에 있는 SkelAni를 실행
                                if (selectSkelAniName.Contains("/") && (skelSbject is SkelSbject.IHoldAdvancedSkinManager holdAdvancedSkinManager))
                                {
                                    var skelAniNameSplit = selectSkelAniName.Split("/");

                                    string advancedSkinName = skelAniNameSplit[0];
                                    string skelAniName = skelAniNameSplit[1];

                                    if (cachedSkelAnis_AdvancedSkins.TryGetValue(advancedSkinName, out var resultDic))
                                    {
                                        if (resultDic.TryGetValue(skelAniName, out var skelAni))
                                        {
                                            executeSkelAni(skelAniName, skelAni);
                                        }
                                    }

                                    return;
                                }


                                //? AniBox에 있는 SkelAni를 실행
                                if (skelAniList_AniBox != null)
                                {
                                    var selectSkelAni = skelAniList_AniBox.Find(x => x.Name == selectSkelAniName);
                                    executeSkelAni(selectSkelAni.Name, selectSkelAni.SkelAni);
                                }


                                void executeSkelAni(string skelAniName, SkelAni skelAni)
                                {
                                    if (skelAni != null)
                                    {
                                        SkelObject.AniCore.Track played_Track;


                                        if (!ExecuteSkelAnimationForce_UseCustomTrackIndex)
                                        {
                                            Debug.Log($"<b>{target.name}</b>의 SkelAni 강제 실행: <color=#4fc1e9>{skelAniName}</color>");

                                            if (ExecuteSkelAnimationForce_UseAbsoluteOverlap && aniCore.TryGetPlayingTrack(skelAni.TrackIndex, out var playingTrack))
                                            {
                                                aniCore.EndAni(skelAni.TrackIndex);
                                            }

                                            played_Track = aniCore.ExecuteAni_GetTrack(skelAni);
                                        }
                                        else
                                        {
                                            Debug.Log($"<b>{target.name}</b>의 <color=red>CustomTrackIndex </color>SkelAni 강제 실행: <color=#4fc1e9>{skelAniName}</color>");

                                            if (ExecuteSkelAnimationForce_UseAbsoluteOverlap && aniCore.TryGetPlayingTrack(ExecuteSkelAnimationForce_CustomTrackIndex, out var playingTrack))
                                            {
                                                aniCore.EndAni(skelAni.TrackIndex);
                                            }

                                            played_Track = aniCore.ExecuteAni_GetTrack(skelAni, ExecuteSkelAnimationForce_CustomTrackIndex);
                                        }


                                        if (played_Track != null && ExecuteSkelAnimationForce_UseCustomLoop)
                                        {
                                            //. PlayingSkelAni와 재생중인 TrackEntry 모두 적용
                                            played_Track.PlayingSkelAni.Loop = ExecuteSkelAnimationForce_CustomLoop;
                                            skeletonAnimation.AnimationState.GetTrack(skelAni.TrackIndex).Loop = ExecuteSkelAnimationForce_CustomLoop;
                                            Debug.Log($"강제 Loop 적용: <b>{ExecuteSkelAnimationForce_CustomLoop}</b>");
                                        }
                                    }
                                    else
                                    {
                                        Debug.LogWarning($"<b>{target.name}</b>의 SkelAni 강제 실행 실패");
                                    }
                                }


                            }, "🅰️ SkelAni 선택");


                        });


                    }, false);


                    SU_CustomEditor.AutoLabelField_Head("💣🅰️ SkelAni 강제 종료", SU_CustomEditor.LabelHeadType.H3, () =>
                    {
                        SU_CustomEditor.RenderField_Float(this, ref EndSkelAnimationForce_MixDuration, "MixDuration");


                        SU_CustomEditor.Render_ButtonWithStyle(this, "🅰️ <color=#ed5565>강제 종료</color> SkelAni 트랙 선택", "", () =>
                        {
                            var copied_PlayingTracks = new List<int>(aniCorePlayer.GetEnablePlayingAnimationTrackIndexes);


                            //. Key: Track 이름의 드롭다운 메뉴 이름
                            //. Value: TrackIndex
                            Dictionary<string, int> tempDictionary = new(aniCorePlayer.GetEnablePlayingAnimationTrackIndexes.Count);


                            for (int i = 0; i < aniCorePlayer.GetEnablePlayingAnimationTrackIndexes.Count; i++)
                            {
                                var currentTrackIndex = aniCorePlayer.GetEnablePlayingAnimationTrackIndexes[i];
                                tempDictionary.Add(GetTrackEnumName(currentTrackIndex, skelSbject), currentTrackIndex);
                            }


                            var playingTrackIndexNames = tempDictionary.Keys.ToList();
                            playingTrackIndexNames.Sort((a, b) =>
                            {
                                return tempDictionary[a].CompareTo(tempDictionary[b]);
                            });


                            SU_CustomEditor.DropDownMenuEvent(playingTrackIndexNames, (selectTrackIndexName) =>
                            {
                                var trackIndex = tempDictionary[selectTrackIndexName];

                                if (aniCorePlayer.TryGetTrack(trackIndex, out var playingTrack) && playingTrack.IsEnable)
                                {
                                    Debug.Log($"<b>{target.name}</b>의 TrackIndex로 <b>{playingTrack.PlayingSkelAni.Animation.Name}</b> 강제 종료: <color=#2ecc71>{selectTrackIndexName}</color>");
                                    aniCore.EndAni(trackIndex);
                                }
                                else
                                {
                                    Debug.LogWarning($"<b>{target.name}</b>의 TrackIndex로 강제 종료 실패, 해당 트랙이 활성화 되어있지 않음");
                                }

                            }, "🅰️ SkelAni 선택");
                        }, SU_ColorPresetRGB.Red_GrapeFruit1(), null);
                    }, false);
                });


                //? Spine.Animation 강제 실행
                SU_CustomEditor.VerticalHelpBox(() =>
                {
                    SU_CustomEditor.AutoLabelField_Head("💣🏃 Spine.Animation 강제 실행", SU_CustomEditor.LabelHeadType.H3, () =>
                    {
                        SU_CustomEditor.VerticalHelpBox(() =>
                        {
                            SU_CustomEditor.RenderField_Int(this, ref ExecuteSpineAnimationForce_TrackIndex, "TrackIndex");
                            SU_CustomEditor.RenderField_Bool(this, ref ExecuteSpineAnimationForce_Loop, "Loop");
                            SU_CustomEditor.RenderField_Float(this, ref ExecuteSpineAnimationForce_MixDuration, "MixDuration");

                            SU_CustomEditor.Render_Button(this, "🏃 강제 실행 Spine.Animation 선택", () =>
                            {
                                var animationNames = skelSbject.AnimationEX_Dictionary.dataArray.Select(x => x.Animation.Name).ToList();

                                SU_CustomEditor.DropDownMenuEvent(animationNames, (selectAnimationName) =>
                                {
                                    var animation = skelSbject.AnimationEX_Dictionary[selectAnimationName];

                                    Debug.Log($"<b>{target.name}</b>의 Spine.Animation 강제 실행: <color=#4fc1e9>{selectAnimationName}</color>");

                                    aniCore.ExecuteCustomAnimation(ExecuteSpineAnimationForce_TrackIndex, animation.Animation, ExecuteSpineAnimationForce_Loop, ExecuteSpineAnimationForce_MixDuration);

                                }, "🏃 Spine.Animation 선택");
                            });
                        });

                    }, false);


                    SU_CustomEditor.AutoLabelField_Head("💣🏃 Spine.Animation 강제 종료", SU_CustomEditor.LabelHeadType.H3, () =>
                    {
                        SU_CustomEditor.RenderField_Float(this, ref EndSpineAnimationForce_MixDuration, "MixDuration");


                        SU_CustomEditor.Render_ButtonWithStyle(this, "🏃 <color=#ed5565>강제 종료</color> Spine.Animation 트랙 선택", "", () =>
                        {
                            var playingTrackIndexNames = skeletonAnimation.AnimationState.Tracks.Where(x => x != null).Select(x => x.TrackIndex.ToString()).ToList();

                            SU_CustomEditor.DropDownMenuEvent(playingTrackIndexNames, (selectTrackIndexName) =>
                            {
                                if (int.TryParse(selectTrackIndexName, out var playingTrackIndex))
                                {
                                    if (skeletonAnimation.AnimationState.GetTrack(playingTrackIndex).Animation != null)
                                    {
                                        Debug.Log($"<b>{target.name}</b>의 TrackIndex로 <b>{skeletonAnimation.AnimationState.GetTrack(playingTrackIndex).Animation.Name}</b> 강제 종료: <color=#2ecc71>{selectTrackIndexName}</color>");
                                        skeletonAnimation.AnimationState.SetEmptyAnimation(playingTrackIndex, EndSpineAnimationForce_MixDuration);
                                    }
                                    else
                                    {
                                        Debug.LogWarning($"<b>{target.name}</b>의 TrackIndex로 Spine.Animation 강제 종료 실패, 재생중인 애니메이션이 없음");
                                    }
                                }
                            }, "🏃 Spine.Animation 선택");
                        }, SU_ColorPresetRGB.Red_GrapeFruit1(), null);

                    }, false);
                });


                //? 재생중인 Spine.Animation 보기
                SU_CustomEditor.AutoLabelFoldOut_Head("🏃 재생중인 Spine.Animation 보기", SU_CustomEditor.LabelHeadType.H3, ref AniPlayer_PlayingSpineAnimations_Fold, () =>
                {
                    EditorGUILayout.Space();

                    SU_CustomEditor.HorizontalGUI(() =>
                    {
                        SU_CustomEditor.LabelField_TextAutoWidthHeight("📌 빈 TrackEntry도 보기");
                        SU_CustomEditor.RenderField_Bool(this, ref AniPlayer_PlayingSpineAnimations_ViewNullTrackEntrys);
                    });
                    SU_CustomEditor.HorizontalGUI(() =>
                    {
                        SU_CustomEditor.LabelField_TextAutoWidthHeight("📌 트랙 열거형 이름 보기");
                        SU_CustomEditor.RenderField_Bool(this, ref AniPlayer_PlayingSpineAnimations_ViewTrackEnumName);
                    });

                    EditorGUILayout.Space();

                    SU_CustomEditor.VerticalHelpBox(() =>
                    {
                        stringBuilder.Clear();
                        for (int i = 0; i < skeletonAnimation.AnimationState.Tracks.Items.Length; i++)
                        {
                            TrackEntry item = skeletonAnimation.AnimationState.Tracks.Items[i];
                            //if (item == null) { continue; }

                            if (item != null)
                            {
                                if (!AniPlayer_PlayingSpineAnimations_ViewTrackEnumName)
                                {
                                    stringBuilder.AppendLine($"🏃 <color=#2ecc71>{item.TrackIndex:000}</color>\t{item.Animation.Name}");
                                }
                                else
                                {
                                    stringBuilder.AppendLine($"🏃 <color=#2ecc71>{item.TrackIndex:000}</color>\t<color=#f7da64>{GetTrackEnumName(item.TrackIndex, skelSbject)}</color>\t{item.Animation.Name}");
                                }
                            }
                            else if (AniPlayer_PlayingSpineAnimations_ViewNullTrackEntrys)
                            {
                                stringBuilder.AppendLine($"❓ {i:000}\t <b><color=#ed5565>NULL</color></b>");
                            }
                        }
                        SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));
                    });

                });


                //? 트랙 개수 출력
                SU_CustomEditor.VerticalHelpBox(() =>
                {
                    EditorGUILayout.Space();
                    SU_CustomEditor.LabelField_TextAutoWidthHeight($"등록된 트랙 총 개수: <b><color=#2ecc71>{aniCorePlayer.GetPlayingAnimationTracks.Count}</color><b> (<b><color=#4fc1e9>{aniCorePlayer.GetEnablePlayingAnimationTrackIndexes.Count}</color><b> / <b><color=#ed5565>{aniCorePlayer.GetPlayingAnimationTracks.Count - aniCorePlayer.GetEnablePlayingAnimationTrackIndexes.Count}</color><b>)");

                    if (aniCorePlayer.GetPlayingAnimationTracks.Count != skeletonAnimation.AnimationState.Tracks.Count)
                    {
                        SU_CustomEditor.LabelField_TextAutoWidthHeight($"미등록된 트랙에서 재생중인 AnimationState 트랙 개수: <b><color=#2ecc71>{aniCorePlayer.GetPlayingAnimationTracks.Count - skeletonAnimation.AnimationState.Tracks.Count}</color><b>");
                    }

                    EditorGUILayout.Space();
                });


                //? 재생중인 트랙 간단하게 보기
                SU_CustomEditor.AutoLabelFoldOut_Head("ℹ️ 등록된 트랙 간단하게 보기", SU_CustomEditor.LabelHeadType.H3, ref AniPlayer_PlayingTracksInfo_Fold, () =>
                {
                    SU_CustomEditor.VerticalHelpBox(() =>
                    {
                        stringBuilder.Clear();

                        foreach (var item in aniCorePlayer.GetPlayingAnimationTracks)
                        {
                            if (item.Value.IsEnable)
                            {
                                stringBuilder.Append($"🔵");
                                stringBuilder.AppendLine($" <b><color=#4fc1e9>{item.Key:000}</b>\t({GetTrackEnumName(item.Key, skelSbject)})</color>\t<i>{item.Value.PlayingSkelAni.Animation.Name}</i>");
                            }
                            else
                            {
                                stringBuilder.Append($"🔴");
                                stringBuilder.AppendLine($" <b><color=#ed5565>{item.Key:000}</b>\t({GetTrackEnumName(item.Key, skelSbject)})</color>");
                            }
                        }
                        SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));
                    });
                }, false);


                //? 재생중인 트랙 보기
                SU_CustomEditor.AutoLabelFoldOut_Head("📼 재생중인 트랙 보기", SU_CustomEditor.LabelHeadType.H3, ref AniPlayer_PlayingTracks_Fold, () =>
                {
                    SU_CustomEditor.Render_Button(this, "📕 트랙 상세정보 한번에 접기", () =>
                    {
                        foreach (var item in PlayingAniTrack_Fold_Dictionary.Keys.ToList())
                        {
                            PlayingAniTrack_Fold_Dictionary[item] = false;
                        }

                        foreach (var item in PlayingAniTrack_Memo_Fold_Dictionary.Keys.ToList())
                        {
                            PlayingAniTrack_Memo_Fold_Dictionary[item] = false;
                        }

                        foreach (var item in PlayingAniTrack_MemoEX_Fold_Dictionary.Keys.ToList())
                        {
                            PlayingAniTrack_MemoEX_Fold_Dictionary[item] = false;
                        }
                    });
                    //. 오딘인스펙터 드롭다운 메서드 활용/캡슐화해서, 만들어보기


                    EditorGUILayout.Space();
                    SU_CustomEditor.RenderField_Bool(this, ref AniPlayer_PlayingTracksFiltering_AllTracks, "📌 모든 트랙 보기");

                    SU_CustomEditor.HorizontalGUI(() =>
                    {
                        SU_CustomEditor.LabelField_TextAutoWidthHeight($"📌 특정 TrackIndex 필터링");
                        SU_CustomEditor.DrawSearchField(ref AniPlayer_PlayingTracksFiltering_TrackIndex);

                        //. 문자열 int형만 허용
                        //if (AniPlayer_PlayingTracksFiltering_TrackIndex != null && AniPlayer_PlayingTracksFiltering_TrackIndex != "" && !int.TryParse(AniPlayer_PlayingTracksFiltering_TrackIndex, out var tempInt)) { AniPlayer_PlayingTracksFiltering_TrackIndex = ""; }
                    });

                    EditorGUILayout.Space();


                    foreach (var item in aniCorePlayer.GetPlayingAnimationTracks)
                    {
                        var trackIndex = item.Key;
                        var track = item.Value;


                        //? 트랙이 비활성화되어있고, 전체 트랙 보기도 비활성화되어있을경우 스킵, continue
                        if (!track.IsEnable && !AniPlayer_PlayingTracksFiltering_AllTracks) { continue; }


                        //? TrackIndex 필터링
                        if (AniPlayer_PlayingTracksFiltering_TrackIndex != null &&
                        AniPlayer_PlayingTracksFiltering_TrackIndex != "" &&
                        int.TryParse(AniPlayer_PlayingTracksFiltering_TrackIndex, out int filtered_TrackIndex) &&
                        filtered_TrackIndex != trackIndex
                        ) { continue; }


                        SU_CustomEditor.VerticalHelpBox(() =>
                        {
                            //. 트랙 이름 지정
                            string trackEnumName = GetTrackEnumName(trackIndex, skelSbject);


                            //? 최상단 트랙 정보 출력
                            if (track.IsEnable)
                            {
                                SU_CustomEditor.LabelField_TextAutoWidthHeight($"<size=13>📼 트랙: <b><color=#2ecc71>{trackIndex}</color> (<color=#f7da64>{trackEnumName}</color>)</b></size>");
                                EditorGUILayout.Space();
                            }
                            else
                            {
                                SU_CustomEditor.LabelField_TextAutoWidthHeight($"<color=#ed5565><b>(DISABLE)</b></color>\t📼 트랙: <b><color=#2ecc71>{trackIndex}</color> (<color=#f7da64>{trackEnumName}</color>)</b></size>");
                                return;
                            }


                            //? 하위 요소 출력


                            stringBuilder.Clear();

                            //? SkelAni 상세 정보 폴드 출력
                            if (!PlayingAniTrack_Fold_Dictionary.TryGetValue(trackIndex, out bool fold_SkelAniInfo)) { PlayingAniTrack_Fold_Dictionary.Add(trackIndex, false); }
                            SU_CustomEditor.VerticalHelpBox(() =>
                            {
                                SU_CustomEditor.Format_IndentLevel_PlusMinusEvent(() =>
                                {
                                    SU_CustomEditor.FoldOut(ref fold_SkelAniInfo, $"🅰️ <b><color=#ac92ec>{track.PlayingSkelAni.Animation.Name}</color></b>", () =>
                                    {
                                        SU_CustomEditor.Format_IndentLevel_Minus();

                                        stringBuilder.Clear();
                                        SU_InfoWriter.WriteSkelAniValueInfo(stringBuilder, track.PlayingSkelAni, skelSbject);

                                        EditorGUILayout.Space();
                                        SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));
                                        EditorGUILayout.Space();

                                        SU_CustomEditor.Format_IndentLevel_Plus();

                                    }, true);
                                });
                                PlayingAniTrack_Fold_Dictionary[trackIndex] = fold_SkelAniInfo;
                            });


                            //? 재생중인 정보 출력


                            SU_CustomEditor.VerticalHelpBox(() =>
                            {
                                Repaint(); //. 이거 안넣으면, 가끔 갱신이 툭툭 끊김


                                var currentAnimationState = skeletonAnimation.AnimationState.GetTrack(trackIndex);


                                //? 애니메이션 재생 시간 출력
                                SU_CustomEditor.LabelField_TextAutoWidthHeight($"🅰️⏱️ <b>애니 재생 시간</b><size=9>(sec)</size>: <b><color=#2ecc71>{currentAnimationState.AnimationTime:00.000}</color></b>");
                                SU_CustomEditor.LabelField_TextAutoWidthHeight($"📼⏱️ <b>트랙 재생 시간</b><size=9>(sec)</size>: <b><color=#2ecc71>{currentAnimationState.TrackTime:00.000}</color></b>");

                                //? 애니메이션 타임라인 진행도 출력 (타임라인이 1개 초과 일때)
                                if (currentAnimationState.Animation.Duration > 0)
                                {
                                    //. 진행도 계산 (Loop 여부 상관없이 AnimationTime 사용)
                                    float trackProgress = currentAnimationState.AnimationTime / currentAnimationState.Animation.Duration;
                                    trackProgress = Mathf.Clamp01(trackProgress); // 안전을 위한 보정

                                    //SU_CustomEditor.LabelField_Text(trackProgress.ToString("0.000"));

                                    //. 게이지 바 출력
                                    var config = ProgressBarConfig.Default;
                                    config.DrawValueLabel = true;
                                    SirenixEditorFields.ProgressBarField("⏳ 애니메이션 진행도", trackProgress, 0f, 1f, config);
                                }
                            });


                            stringBuilder.Clear();


                            //? MultipleTime 출력
                            stringBuilder.AppendLine($"⌛ <b>MultipleTime: <color=#2ecc71>{track.GetMultipleTime().ToString()}</color>");


                            //? 애니메이션 스타터 출력
                            if (track.AnimationStarter != null)
                            {
                                stringBuilder.AppendLine($"💊 <b>AnimationStarter:\t<color=#4fc1e9>{track.AnimationStarter}</color>");
                            }


                            //? EndEvent 출력
                            if (track.Contains_EndEvent)
                            {
                                stringBuilder.AppendLine($"🍆 <b>EndEvent:\t<color=#4fc1e9>존재</color>");
                            }


                            //? 무명메서드 출력 1~9
                            if (track.SpineEventCustom_1 != null)
                            {
                                stringBuilder.AppendLine($"🍆 <b>SpineEventCustom1:\t<color=#4fc1e9>존재</color>");
                            }
                            if (track.SpineEventCustom_2 != null)
                            {
                                stringBuilder.AppendLine($"🍆 <b>SpineEventCustom2:\t<color=#4fc1e9>존재</color>");
                            }
                            if (track.SpineEventCustom_3 != null)
                            {
                                stringBuilder.AppendLine($"🍆 <b>SpineEventCustom3:\t<color=#4fc1e9>존재</color>");
                            }
                            if (track.SpineEventCustom_4 != null)
                            {
                                stringBuilder.AppendLine($"🍆 <b>SpineEventCustom4:\t<color=#4fc1e9>존재</color>");
                            }
                            if (track.SpineEventCustom_5 != null)
                            {
                                stringBuilder.AppendLine($"🍆 <b>SpineEventCustom5:\t<color=#4fc1e9>존재</color>");
                            }
                            if (track.SpineEventCustom_6 != null)
                            {
                                stringBuilder.AppendLine($"🍆 <b>SpineEventCustom6:\t<color=#4fc1e9>존재</color>");
                            }
                            if (track.SpineEventCustom_7 != null)
                            {
                                stringBuilder.AppendLine($"🍆 <b>SpineEventCustom7:\t<color=#4fc1e9>존재</color>");
                            }
                            if (track.SpineEventCustom_8 != null)
                            {
                                stringBuilder.AppendLine($"🍆 <b>SpineEventCustom8:\t<color=#4fc1e9>존재</color>");
                            }
                            if (track.SpineEventCustom_9 != null)
                            {
                                stringBuilder.AppendLine($"🍆 <b>SpineEventCustom9:\t<color=#4fc1e9>존재</color>");
                            }


                            //? 위 요소들 출력
                            SU_CustomEditor.VerticalHelpBox(() =>
                            {
                                SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));
                            });


                            //? 메모 리스트 출력
                            if (track.GetMemoList.Count != 0)
                            {
                                SU_CustomEditor.VerticalHelpBox(() =>
                                {
                                    //? Memo 상세 정보 폴드 출력
                                    if (!PlayingAniTrack_Memo_Fold_Dictionary.TryGetValue(trackIndex, out bool fold_memo)) { PlayingAniTrack_Memo_Fold_Dictionary.Add(trackIndex, false); }

                                    SU_CustomEditor.Format_IndentLevel_PlusMinusEvent(() =>
                                    {
                                        SU_CustomEditor.FoldOut(ref fold_memo, $"🗒️ <b>Memo</b> (<color=#2ecc71><b>{track.GetMemoList.Count}</b></color>)", () =>
                                        {
                                            SU_CustomEditor.Format_IndentLevel_Minus();

                                            stringBuilder.Clear();

                                            for (int i = 0; i < track.GetMemoList.Count; i++)
                                            {
                                                string memo = track.GetMemoList[i];
                                                stringBuilder.AppendLine($"\t🗒️ <color=#f7da64><b>{memo}</b></color> (<color=#2ecc71><b>{i}</b></color>)");
                                            }

                                            SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));

                                            SU_CustomEditor.Format_IndentLevel_Plus();
                                        });

                                        PlayingAniTrack_Memo_Fold_Dictionary[trackIndex] = fold_memo;
                                    });
                                });
                            }


                            //? 메모EX 리스트 출력
                            if (track.GetMemoExtendList.Count != 0)
                            {
                                SU_CustomEditor.VerticalHelpBox(() =>
                                {
                                    //? MemoEX 상세 정보 폴드 출력
                                    if (!PlayingAniTrack_MemoEX_Fold_Dictionary.TryGetValue(trackIndex, out bool fold_memoEX)) { PlayingAniTrack_MemoEX_Fold_Dictionary.Add(trackIndex, false); }
                                    SU_CustomEditor.Format_IndentLevel_PlusMinusEvent(() =>
                                    {
                                        SU_CustomEditor.FoldOut(ref fold_memoEX, $"📒 <b>MemoEX</b> (<color=#2ecc71><b>{track.GetMemoExtendList.Count}</b></color>)", () =>
                                        {
                                            SU_CustomEditor.Format_IndentLevel_Minus();

                                            stringBuilder.Clear();

                                            for (int i = 0; i < track.GetMemoExtendList.Count; i++)
                                            {
                                                SkelObject.AniCore.TrackMemoExtend memoExtend = track.GetMemoExtendList[i];
                                                stringBuilder.AppendLine($"\t📒 <color=#f7da64><b>{memoExtend.Memo}</b></color> (<color=#2ecc71><b>{i}</b></color>)");
                                                stringBuilder.AppendLine($"\t\t📼 <b>TrackIndex</b>: <color=#2ecc71>{memoExtend.TrackIndex}</color>");
                                                stringBuilder.AppendLine($"\t\t🅰️ <b>Animation</b>: <color=#ac92ec>{memoExtend.SkelAni.Animation.Name}</color>");
                                            }

                                            SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));

                                            SU_CustomEditor.Format_IndentLevel_Plus();
                                        });
                                        PlayingAniTrack_MemoEX_Fold_Dictionary[trackIndex] = fold_memoEX;
                                    });
                                });
                            }


                            stringBuilder.Clear();

                        });

                        EditorGUILayout.Space(1);
                    }

                });


                //? 실시간 재생된 애니메이션 보기
                SU_CustomEditor.AutoLabelFoldOut_Head("💥 실시간 재생된 애니메이션 보기", SU_CustomEditor.LabelHeadType.H3, ref ViewRunTimePlayingSkelAni_Fold, () =>
                {
                    if (ToastMessages_SkelAniExecuted.Count != 0 && !IsRunning_ToastMessages_SkelAniExecuted)
                    {
                        IsRunning_ToastMessages_SkelAniExecuted = true;

                        UniTask.RunOnThreadPool(async () =>
                        {
                            await UniTask.SwitchToMainThread();

                            while (ToastMessages_SkelAniExecuted.Count != 0)
                            {
                                var removeList = new List<SkelObject.AniCore.Track>();


                                foreach (var msg in ToastMessages_SkelAniExecuted.Keys.ToList())
                                {
                                    ToastMessages_SkelAniExecuted[msg] += Time.unscaledDeltaTime;
                                    if (ToastMessages_SkelAniExecuted[msg] >= ToastMessages_SkelAniExecuted_MaxTime)
                                    {
                                        removeList.Add(msg);
                                    }
                                }


                                if (removeList.Count != 0)
                                {
                                    for (int i = 0; i < removeList.Count; i++)
                                    {
                                        ToastMessages_SkelAniExecuted.Remove(removeList[i]);
                                    }
                                }

                                await UniTask.Yield();
                            }

                            IsRunning_ToastMessages_SkelAniExecuted = false;

                        }).Forget();
                    }


                    var ToastMessages_SkelAniExecutedList = ToastMessages_SkelAniExecuted.Keys.ToArray();


                    for (int i = ToastMessages_SkelAniExecutedList.Length - 1; i >= 0; i--)
                    {
                        var playingTrack = ToastMessages_SkelAniExecutedList[i];

                        if (playingTrack == null || !playingTrack.IsEnable) { continue; }

                        SU_CustomEditor.VerticalHelpBox(() =>
                        {
                            //ColorUtility.TryParseHtmlString("#f7da64", out var currentColor);
                            var currentColor = Color.white;
                            currentColor.SetAlpha(0.5f);

                            var color = Color.Lerp(currentColor, currentColor.SetAlpha(0), ToastMessages_SkelAniExecuted[playingTrack] / ToastMessages_SkelAniExecuted_MaxTime);

                            SU_CustomEditor.DrawHighlightedBox(color, () =>
                            {
                                //SU_CustomEditor.LabelField_Text("애니메이션 재생!");
                                stringBuilder.Clear();
                                SU_InfoWriter.WriteSkelAniValueInfo(stringBuilder, playingTrack.PlayingSkelAni, skelSbject);
                                SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder.ToString(true));
                                stringBuilder.Clear();
                            });

                        });
                    }
                }, false);


            }, false);
        }



        ///======================================================================================================================================================



        public Dictionary<SkelObject.AniCore.Track, float> ToastMessages_SkelAniExecuted = new();
        public bool IsRunning_ToastMessages_SkelAniExecuted;
        public float ToastMessages_SkelAniExecuted_MaxTime = 1f;



        public void AlarmThe_AniStart(SkelObject skelObject, SkelObject.AniCore.Track track)
        {
            ToastMessages_SkelAniExecuted[track] = 0;
            //Debug.Log($"{track.PlayingSkelAni.Animation.Name} 이 실행됨");
        }



        ///======================================================================================================================================================



        //? CustomTrackIndex는 Enum이없으므로, 이를 고려하여 TrackIndex의 열거형 문자열을 반환하는 메서드
        private static string GetTrackEnumName(int trackIndex, SkelSbject skelSbject)
        {
            //. 트랙 이름 지정
            string trackEnumName = skelSbject.EnumIndex_AniTracks.GetEnumName(trackIndex);
            if (trackEnumName == null || trackEnumName == "") { trackEnumName = $"CustomTrackIndex ({trackIndex})"; }
            return trackEnumName;
        }



        ///======================================================================================================================================================
    }
}