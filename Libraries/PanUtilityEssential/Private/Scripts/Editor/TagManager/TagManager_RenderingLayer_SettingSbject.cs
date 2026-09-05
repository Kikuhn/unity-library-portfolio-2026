using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;



[CustomEditor(typeof(TagManager_RenderingLayer_SettingSbject))]
public class TagManager_RenderingLayer_SettingSbjectEditor : TagManager_OtherLayer_SettingSbjectBaseEditor<TagManager_RenderingLayer_SettingSbject>
{
    ///======================================================================================================================================================



    public override string TitleName => "RenderingLayer 설정";



    ///======================================================================================================================================================
}



[CreateAssetMenu(fileName = "RenderingLayerSetting", menuName = CreateAssetMenuInfo.RENDRINGLAYER_SETTINGS, order = CreateAssetMenuInfo.RENDRINGLAYER_SETTINGS_ORDER)]
public class TagManager_RenderingLayer_SettingSbject : TagManager_OtherLayer_SettingSbjectBase
{
    public override EType OtherLayerType => EType.RenderingLayers;
}
