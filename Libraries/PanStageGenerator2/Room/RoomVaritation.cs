using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Pan.GridCompatibles2;
using Pan.StageGenerators;



namespace Pan.StageGenerators
{
    //public interface IRoomVaritationManager
    //{

    //    void SetVariableIndex(int variableIndex);
    //}



    //public class RoomVaritation : RoomObject.BaseRoomChildObject, IRoomVaritationManager
    //{
    //    ///======================================================================================================================================================



    //    [field: ReadOnly][field: SerializeField] public bool IsEnable { get; private set; } = false;



    //    ///======================================================================================================================================================



    //    [field: ReadOnly][field: SerializeField] public int VariableIndex { get; private set; }



    //    ///======================================================================================================================================================



    //    [SerializeField] private UnityEvent EnableVariationEvent;
    //    [SerializeField] private UnityEvent DisableVariationEvent;



    //    ///======================================================================================================================================================



    //    /// <summary>이 바리에이션을 활성화한다 (이미 활성화된 상태라면 false 반환)</summary>
    //    public bool EnableVariation()
    //    {
    //        if (IsEnable) { return false; }

    //        IsEnable = true;
    //        EnableVariationEvent?.Invoke();
    //        return true;
    //    }



    //    /// <summary>이 바리에이션을 비활성화한다 (이미 활성화된 상태라면 false 반환)</summary>
    //    public bool DisableVariation()
    //    {
    //        if (!IsEnable) { return false; }

    //        IsEnable = false;
    //        DisableVariationEvent?.Invoke();
    //        return true;
    //    }



    //    ///======================================================================================================================================================



    //    void IRoomVaritationManager.SetVariableIndex(int variableIndex)
    //    {
    //        VariableIndex = variableIndex;
    //    }



    //    ///======================================================================================================================================================
    //}
}