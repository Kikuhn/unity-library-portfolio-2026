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
    [CustomEditor(typeof(PanEventValueInitializeSettingSbject), true)]
    public class PanEventValueInitializeSettingSbjectEditor : EditorExpand_InspectorGUI<PanEventValueInitializeSettingSbject>
    {
        ///======================================================================================================================================================



        protected override EDrawDefaultInspectorMode? CurrentMode_DrawDefaultInspector_Fixed => EDrawDefaultInspectorMode.OdinVisible;



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
                        sb.AppendLine($"<b><color=#f7da64>Missing</color><b>인 <b>PanEventValue</b>가 <color=#2ecc71><b>{invalidSettings.Count}</b></color>개 존재");

                        SU_CustomEditor.LabelField_TextAutoWidthHeight(sb.ToString(true));
                    });
                });
            }
        }



        ///======================================================================================================================================================
    }
}
