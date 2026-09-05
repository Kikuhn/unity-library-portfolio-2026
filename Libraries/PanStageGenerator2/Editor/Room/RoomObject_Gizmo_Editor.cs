//using System.Collections;
//using UnityEngine;
//using UnityEditor;
//using System.Text;
//using Pan.Util;
//using System.Linq;
//using DG.Tweening;
//using Pan.GridCompatibles2;
//
//using Pan.StageGenerators;
//using Pan.StageGenerators.Editor;
//using UnityEditor.SceneManagement;

//[CanEditMultipleObjects]
//[CustomEditor(typeof(RoomObject_Gizmo), true)]
//public class RoomObject_Gizmo_Editor : BaseGizmoScript_Editor
//{
//    protected new RoomObject_Gizmo Target => base.Target as RoomObject_Gizmo;

//    protected override void OnInspectorGUI_Current()
//    {
//        SU_CustomEditor.AutoLabelField_Head("기즈모 SO", SU_CustomEditor.LabelHeadType.H2, () =>
//        {
//            SU_CustomEditor.RenderField_Property(this, nameof(Target.CurrentGizmoSetting).LowerFirst(), "기즈모 SO");
//        });

//        base.OnInspectorGUI_Current();
//    }
//}
