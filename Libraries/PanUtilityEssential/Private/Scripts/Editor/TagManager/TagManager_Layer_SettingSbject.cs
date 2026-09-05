using System;
using System.Text;
using UnityEngine;
using UnityEditor;



[CustomEditor(typeof(TagManager_Layer_SettingSbject))]
public class TagManager_Layer_SettingSbjectEditor : TagManager_SettingSbjectBaseEditor<TagManager_Layer_SettingSbject>
{
    ///======================================================================================================================================================



    private GUIStyle layerStyle; // Layer 이름 표시 스타일



    ///======================================================================================================================================================



    public override string TitleName => "Layer 설정";



    ///======================================================================================================================================================



    protected override void Awake()
    {
        base.Awake();

        layerStyle = new GUIStyle(EditorStyles.label)
        {
            richText = true,
        };
    }



    ///======================================================================================================================================================



    protected override void OnInspectorGUI_Current(TagManager_Layer_SettingSbject targetObj)
    {
        for (int i = 0; i < 32; i++)
        {
            var layer = targetObj.Layers[i];
            string projectLayerName = LayerMask.LayerToName(i);

            //? 비어 있는 Layer를 공백으로 처리
            string scriptableLayerName = layer.LayerName;

            //? Layer가 다르면 초록색으로 표시하고 `*` 추가
            bool isDifferent = scriptableLayerName != projectLayerName;
            string layerTextColor = isDifferent ? "green" : "grey";
            string layerTextSuffix = isDifferent ? "* " : "";

            EditorGUILayout.BeginHorizontal();

            //? Layer 번호 표시
            GUILayout.Label($"<color={layerTextColor}>{layerTextSuffix}Layer {i,-2}</color>", layerStyle, GUILayout.Width(80));

            //? ScriptableObject Layer 표시 (수정 제한 적용)
            if (layer.IsLocked)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField(scriptableLayerName, GUILayout.MinWidth(150), GUILayout.ExpandWidth(true));
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                layer.LayerName = EditorGUILayout.TextField(scriptableLayerName, GUILayout.MinWidth(150), GUILayout.ExpandWidth(true));
            }

            //? 현재 프로젝트 Layer 표시 (읽기 전용)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(projectLayerName, GUILayout.MinWidth(150), GUILayout.ExpandWidth(true));
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(targetObj);
        }
    }



    ///======================================================================================================================================================
}



[CreateAssetMenu(fileName = "LayerSetting", menuName = CreateAssetMenuInfo.LAYER_SETTINGS, order = CreateAssetMenuInfo.LAYER_SETTINGS_ORDER)]
public class TagManager_Layer_SettingSbject : TagManager_SettingSbjectBase
{
    ///======================================================================================================================================================



    [Serializable]
    public class LayerInfo
    {
        public string LayerName;
        public bool IsLocked;

        public LayerInfo(string name, bool isLocked)
        {
            LayerName = name;
            IsLocked = isLocked;
        }
    }



    [SerializeField]
    public LayerInfo[] Layers = new LayerInfo[32]; //. 고정된 32개의 Layer 배열



    ///======================================================================================================================================================



    /// <summary>
    /// Project에 Layer 설정 적용하기
    /// </summary>
    public override void ApplyToProject()
    {
        ApplysToProject(EType.Layers, (layersProp) =>
        {
            for (int i = 0; i < 32; i++)
            {
                if (!Layers[i].IsLocked)
                {
                    layersProp.GetArrayElementAtIndex(i).stringValue = Layers[i].LayerName;
                }
            }
        });
    }



    /// <summary>
    /// Project로부터 Layer 설정 불러오기
    /// </summary>
    public override void LoadFromProject()
    {
        for (int i = 0; i < 32; i++)
        {
            string projectLayerName = LayerMask.LayerToName(i);
            Layers[i].LayerName = string.IsNullOrEmpty(projectLayerName) ? string.Empty : projectLayerName;
        }

        EditorUtility.SetDirty(this);
    }



    ///======================================================================================================================================================



    private void OnEnable()
    {
        InitializeLayers();
    }



    private void InitializeLayers()
    {
        // Unity에서 고정된 Layer 설정
        Layers[0] = new LayerInfo("Default", true);
        Layers[1] = new LayerInfo("TransparentFX", true);
        Layers[2] = new LayerInfo("Ignore Raycast", true);
        Layers[4] = new LayerInfo("Water", true);
        Layers[5] = new LayerInfo("UI", true);

        // 나머지 Layer 초기화
        for (int i = 0; i < Layers.Length; i++)
        {
            if (Layers[i] == null)
            {
                Layers[i] = new LayerInfo(string.Empty, false);
            }
        }
    }



    ///======================================================================================================================================================
}