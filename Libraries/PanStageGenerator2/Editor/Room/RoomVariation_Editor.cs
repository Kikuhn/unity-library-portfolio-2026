using System.Collections;
using UnityEngine;
using UnityEditor;
using System.Text;
using Pan.Util;
using System.Linq;
using DG.Tweening;
using Pan.GridCompatibles2;

using Pan.StageGenerators;
using Pan.StageGenerators.Editor;



namespace Pan.StageGenerators.Editor
{
    //[CanEditMultipleObjects]
    //[CustomEditor(typeof(RoomVaritation), true)]
    //public class RoomVaritation_Editor : BaseRoomChildObject_Editor<RoomVaritation>
    //{
    //    protected override void OnInspectorGUI_Current()
    //    {
    //        SU_CustomEditor.Render_Button(this, "부모 방 찾기!", x =>
    //        {
    //            GetTargetRoomChild(x).AutoFindAndFillParentRoom(x);
    //        });

    //        SU_CustomEditor.HorizontalGUI(() =>
    //        {
    //            SU_CustomEditor.Render_ButtonWithStyle(this, "이 바리에이션 활성화", "", x =>
    //            {
    //                x.EnableVariation();
    //            }, SU_Color.Green_Emerald(), Color.white);

    //            SU_CustomEditor.Render_ButtonWithStyle(this, "이 바리에이션 비활성화", "", x =>
    //            {
    //                x.DisableVariation();
    //            }, SU_Color.Red_GrapeFruit1(), Color.white);
    //        });
    //    }
    //}
}