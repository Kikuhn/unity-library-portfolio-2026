using Pan.Event;
using Pan.Util;
using Pan.Util.Editors;
using System;
using System.Reflection.Emit;
using System.Text;
using UnityEditor;
using UnityEngine;



namespace Pan.Event.Editor
{
    [CustomEditor(typeof(PanEventInitializeSettingSbject), true)]
    public class PanEventInitializeSettingSbjectEditor : EditorExpand_InspectorGUI<PanEventInitializeSettingSbject>
    {
        ///======================================================================================================================================================



        protected override EDrawDefaultInspectorMode? CurrentMode_DrawDefaultInspector_Fixed =>  EDrawDefaultInspectorMode.OdinVisible;

        

        ///======================================================================================================================================================



        protected override void Awake()
        {
            base.Awake();

            if (Target != null) Target.InitializeSettings();
        }



        protected override void OnEnable()
        {
            base.OnEnable();

            if (Target != null) Target.InitializeSettings();
        }



        ///======================================================================================================================================================



        protected override void OnInspectorGUI_Current()
        {
            if (!Target.CheckInitializeSettings_InValid(out var invalidSettings))
            {
                SU_CustomEditor.VerticalHelpBox(() =>
                {
                    SU_CustomEditor.UsingStringBuilder(sb =>
                    {
                        sb.AppendLine($"<b><color=#f7da64>Missing</color><b>인 <b>PanEvent</b>가 <color=#2ecc71><b>{invalidSettings.Count}</b></color>개 존재");

                        SU_CustomEditor.LabelField_TextAutoWidthHeight(sb.ToString(true));
                    });
                });
            }
        }



        ///======================================================================================================================================================
    }



    #region Legacy

    //[CustomEditor(typeof(PanEventInitializeSettingSbject), true)]
    //public class PanEventInitializeSettingSbjectEditor : EditorExpand_InspectorGUI<PanEventInitializeSettingSbject>
    //{
    //    ///======================================================================================================================================================



    //    protected override void Awake()
    //    {
    //        base.Awake();

    //        Target.InitializeSettings();
    //    }



    //    protected override void OnEnable()
    //    {
    //        base.OnEnable();

    //        Target.InitializeSettings();
    //    }



    //    ///======================================================================================================================================================



    //    private bool Fold_LeftTypes;



    //    private string PanBaseEventName => typeof(PanBaseEvent).Name;



    //    protected override void OnInspectorGUI_Current()
    //    {
    //        if (!Target.CheckInitializeSettings_InValid(out var invalidSettings))
    //        {
    //            SU_CustomEditor.VerticalHelpBox(() =>
    //            {
    //                SU_CustomEditor.UsingStringBuilder(sb =>
    //                {
    //                    sb.AppendLine($"유효하지 않은 요소가 {invalidSettings.Count()} 존재");

    //                    SU_CustomEditor.LabelField_TextAutoWidthHeight(sb.ToString(true));
    //                });
    //            });
    //        }




    //        var allTypes = PanEventInitializeSettingSbject.GetCachingTypes();
    //        var initializeSettings = Target.GetPanEventInitializeSettings;

    //        int allTypesCount = allTypes.Length;
    //        int listTypesCount = Target.GetPanEventInitializeSettings.Count;

    //        bool allTypesarSetted = listTypesCount >= allTypesCount;


    //        SU_CustomEditor.VerticalHelpBox(() =>
    //        {
    //            SU_CustomEditor.UsingStringBuilder(sb =>
    //            {
    //                sb.Append($"설정값에 존재하는 {PanBaseEventName} 개수: <color=#2ecc71><b>{listTypesCount}</b></color> / <color=#4fc1e9><b>{allTypes.Length}</b></color>");

    //                if (allTypesarSetted)
    //                {
    //                    sb.AppendLine($" (리스트에 모든 {PanBaseEventName}가 존재함)");
    //                }
    //                else
    //                {
    //                    sb.AppendLine($" (남은 {PanBaseEventName} <color=#ed5565><b>{allTypesCount - listTypesCount}</b></color>)");
    //                }

    //                SU_CustomEditor.LabelField_TextAutoWidthHeight(sb.ToString(true));
    //            });
    //        });

    //        if (!allTypesarSetted)
    //        {
    //            SU_CustomEditor.FoldOut(ref Fold_LeftTypes, $"남겨진 {PanBaseEventName} 목록 보기 ({allTypesCount - listTypesCount})", () =>
    //            {
    //                var filteredTypes = allTypes.Except(
    //                    initializeSettings
    //                    .Where(x => x.GetPanBaseEventType != null)
    //                    .Select(x => x.GetPanBaseEventType)
    //                    ).ToArray();


    //                SU_CustomEditor.UsingStringBuilder(sb =>
    //                {
    //                    int count = 1;
    //                    for (int i = 0; i < filteredTypes.Length; i++)
    //                    {
    //                        var currentType = filteredTypes[i];
    //                        sb.AppendLine($"{count}\t{currentType.Name}");
    //                        count++;
    //                    }

    //                    SU_CustomEditor.LabelField_TextAutoWidthHeight(sb.ToString(true));
    //                });
    //            });
    //        }




    //        //SU_CustomEditor.ScrollViewResizable_Collection(ref ScrollPos_Events, ref Height_Events, MinHeight_Events, MaxHeight_Events, Target.List1, (Action<PanBaseEventInitializeSetting>)((setting) =>
    //        //{
    //        //    SU_CustomEditor.VerticalHelpBox((Action)(() =>
    //        //    {
    //        //        SU_CustomEditor.LabelField_TextAutoWidthHeight(setting.GetCurrentTypeName);

    //        //        SU_CustomEditor.Render_Button(this, "타입 지정", (Action)(() =>
    //        //        {
    //        //            SU_CustomEditor.DropDownMenuEvent(GetCachingTypeNames(), (Action<string>)((selected) =>
    //        //            {
    //        //                var type = GetCachingTypeFromTypeName(selected);
    //        //                setting.CurrentType = type;
    //        //            }));
    //        //        }));

    //        //        //SU_CustomEditor.LabelField_TextAutoWidthHeight(setting.Initialize);
    //        //        SU_CustomEditor.RenderField_Bool(this, ref setting.Initialize);
    //        //    }));
    //        //}));



    //        //SU_CustomEditor.Render_Button(this, "테스트222", () =>
    //        //{
    //        //    SU_CustomEditor.DropDownMenuEvent(PanEventInitializeSettingSbject.GetCachingTypeNames(), (selected) =>
    //        //    {

    //        //    });
    //        //});
    //    }



    //    ///======================================================================================================================================================
    //} 

    #endregion
}
