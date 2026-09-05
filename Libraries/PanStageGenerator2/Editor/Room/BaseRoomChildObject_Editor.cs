using System.Collections;
using UnityEngine;
using UnityEditor;
using Pan.Util;
using System.Text;
using Pan.GridCompatibles2;

using Pan.StageGenerators;
using Pan.StageGenerators.Editor;
using Pan.Util.Editors;



namespace Pan.StageGenerators.Editor
{
    //public abstract class BaseRoomChildObject_Editor<T> : EditorExpand_InspectorGUI<T> where T : RoomObject.BaseRoomChildObject, new()
    //{
    //    ///======================================================================================================================================================



    //    protected RoomObject.IBaseRoomChildObject TargetRoomChild => Target;



    //    protected RoomObject.IBaseRoomChildObject GetTargetRoomChild(RoomObject.BaseRoomChildObject target) => target;



    //    ///======================================================================================================================================================



    //    protected override void Awake()
    //    {
    //        base.Awake();
    //        Find_ParentRoomObject(false, false);
    //    }



    //    protected override void OnEnable()
    //    {
    //        base.OnEnable();
    //        Find_ParentRoomObject(false, false);
    //    }



    //    ///======================================================================================================================================================



    //    protected void Find_ParentRoomObject(bool overlap, bool debugMsg)
    //    {
    //        bool parentRoom_IsNull = Target.ParentRoom == null;

    //        if (parentRoom_IsNull || overlap)
    //        {
    //            TargetRoomChild.AutoFindAndFillParentRoom(Target);

    //            if (parentRoom_IsNull && debugMsg)
    //            {
    //                Debug.LogWarning($"{Target.name}의 부모 RoomObject가 Missing");
    //            }
    //            else if (!Target.ParentRoom.RoomObjectIsInstanced)
    //            {
    //                Target.ParentRoom.FindChilds(debugMsg);
    //            }
    //        }
    //    }



    //    ///======================================================================================================================================================
    //}
}