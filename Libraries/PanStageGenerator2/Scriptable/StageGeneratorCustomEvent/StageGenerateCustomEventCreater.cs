using Pan.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace Pan.StageGenerators
{
    public abstract class BaseStageGenerateCustomEvent : ScriptableObject { }



    [CreateAssetMenu(fileName = "StageGenerateCustomEventCreater", menuName = CreateAssetMenuInfo.STAGEGEN_CUSTOMEVENTCREATER)]
    public class StageGenerateCustomEventCreater : SingleTon_ScriptableObject<StageGenerateCustomEventCreater>
    {
        public string CreateFolderPath;
        public string CreateFileName;
    }
}