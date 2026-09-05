using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;



[CustomEditor(typeof(TagManager_SortingLayer_SettingSbject))]
public class TagManager_SortingLayer_SettingSbjectEditor : TagManager_OtherLayer_SettingSbjectBaseEditor<TagManager_SortingLayer_SettingSbject>
{
    ///======================================================================================================================================================



    public override string TitleName => "SortingLayer 설정";



    ///======================================================================================================================================================
}



[CreateAssetMenu(fileName = "SortingLayerSetting", menuName = CreateAssetMenuInfo.SORTINGLAYER_SETTINGS, order =CreateAssetMenuInfo.SORTINGLAYER_SETTINGS_ORDER)]
public class TagManager_SortingLayer_SettingSbject : TagManager_OtherLayer_SettingSbjectBase
{
    public override EType OtherLayerType => EType.SortingLayers;
}
