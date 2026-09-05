using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;



public abstract class TagManager_SettingSbjectBaseEditor<T> : Editor where T : TagManager_SettingSbjectBase
{
    ///======================================================================================================================================================



    protected GUIStyle descriptionStyle;



    public abstract string TitleName { get; }



    protected SerializedObject TagManagerSO { get; private set; }



    ///======================================================================================================================================================



    protected virtual void Awake()
    {
        T targetObj = target as T;
        TagManagerSO = targetObj.LoadTagManagerSerializedObject();
        descriptionStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
    }



    ///======================================================================================================================================================



    public override void OnInspectorGUI()
    {
        T targetObj = target as T;


        GUILayout.Label(TitleName, EditorStyles.boldLabel);
        EditorGUILayout.Space();


        DrawApplyButtons(targetObj);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();


        OnInspectorGUI_Current(targetObj);


        GUILayout.Label("설명", EditorStyles.boldLabel);
        targetObj.Description = EditorGUILayout.TextArea(targetObj.Description, descriptionStyle, GUILayout.Height(60));
        if (GUI.changed)
        {
            EditorUtility.SetDirty(targetObj);
        }
    }



    protected abstract void OnInspectorGUI_Current(T targetObj);



    ///======================================================================================================================================================



    protected void Button_ApplyToProject(T targetObj)
    {
        if (GUILayout.Button("Project에 이 설정 적용"))
        {
            if (EditorUtility.DisplayDialog("설정 적용", "설정을 프로젝트에 적용하시겠습니까?", "적용", "취소"))
            {
                targetObj.ApplyToProject();
                Debug.Log("프로젝트에 설정이 적용되었습니다.");
            }
        }
    }



    protected void Button_LoadFromProject(T targetObj)
    {
        if (GUILayout.Button("Project로부터 설정 가져오기"))
        {
            if (EditorUtility.DisplayDialog("설정 가져오기", "현재 프로젝트 설정을 불러오겠습니까?", "복사", "취소"))
            {
                targetObj.LoadFromProject();
                Debug.Log("프로젝트 설정에서 가져왔습니다.");
            }
        }
    }



    protected void DrawApplyButtons(T targetObj)
    {
        GUILayout.BeginHorizontal();

        Button_ApplyToProject(targetObj);
        Button_LoadFromProject(targetObj);

        GUILayout.EndHorizontal();
    }



    ///======================================================================================================================================================
}



public abstract class TagManager_OtherLayer_SettingSbjectBaseEditor<T> : TagManager_SettingSbjectBaseEditor<T> where T : TagManager_OtherLayer_SettingSbjectBase
{
    ///======================================================================================================================================================



    protected ReorderableList reorderableList;
    protected ReorderableList projectOtherLayerList;


    ///======================================================================================================================================================    



    protected override void OnInspectorGUI_Current(T targetObj)
    {
        //. ScriptableObject(사용자 설정) 리스트
        if (reorderableList == null)
        {
            InitializeReorderableList(targetObj);
        }
        reorderableList.DoLayoutList();


        EditorGUILayout.Space();
        if (targetObj.OtherLayerType == TagManager_SettingSbjectBase.EType.SortingLayers)
        {
            GUILayout.Label("Project의 SortingLayers", EditorStyles.boldLabel);
        }
        if (targetObj.OtherLayerType == TagManager_SettingSbjectBase.EType.RenderingLayers)
        {
            GUILayout.Label("Project의 RenderingLayers", EditorStyles.boldLabel);
        }


        //. Project(실제 프로젝트 세팅) 리스트
        if (projectOtherLayerList == null)
        {
            InitializeProjectLayerList(targetObj);
        }


        TagManagerSO.Update();
        projectOtherLayerList.DoLayoutList();
        TagManagerSO.ApplyModifiedProperties();
    }



    private void InitializeReorderableList(T targetObj)
    {
        var layerList = targetObj.OtherLayers;


        reorderableList = new ReorderableList(
            layerList,
            typeof(TagManager_OtherLayer_SettingSbjectBase.OtherLayerInfo),
            true, // 드래그 가능
            false, // 헤더 표시
            true,  // 추가 버튼
            true   // 삭제 버튼
        );

        //. 요소 렌더링
        reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            var layer = layerList[index];
            rect.y += 2;

            //? "Layer X" 표시
            EditorGUI.LabelField(new Rect(rect.x, rect.y, 60, EditorGUIUtility.singleLineHeight), $"Layer {index}");

            //. Name 필드
            layer.Name = EditorGUI.TextField(
                new Rect(rect.x + 70, rect.y, rect.width - 70, EditorGUIUtility.singleLineHeight),
                layer.Name
            );
        };

        //. 요소 추가
        reorderableList.onAddCallback = (ReorderableList list) =>
        {
            targetObj.OtherLayers.Add(new TagManager_OtherLayer_SettingSbjectBase.OtherLayerInfo("New Layer"));
        };

        //. 요소 삭제
        reorderableList.onRemoveCallback = (ReorderableList list) =>
        {
            //! 'Default' 레이어는 삭제 불가
            if (targetObj.OtherLayers[list.index].Name == "Default")
            {
                EditorUtility.DisplayDialog("삭제 불가", "'Default' 레이어는 삭제할 수 없습니다.", "확인");
                return;
            }
            targetObj.OtherLayers.RemoveAt(list.index);
        };
    }



    private void InitializeProjectLayerList(T targetObj)
    {
        var layersProp = targetObj.LoadTagManagerProperty(targetObj.OtherLayerType, TagManagerSO);


        projectOtherLayerList = new ReorderableList(
            TagManagerSO,
            layersProp,
            false, // 드래그 비활성화
            false,  // 헤더 표시
            false, // 추가 버튼 비활성화
            false  // 삭제 버튼 비활성화
        );

        //. 요소 렌더링
        projectOtherLayerList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            if (index < 0 || index >= layersProp.arraySize) return;

            //? otherLayersProp 배열 내 해당 요소
            SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(index);

            //. 실제 레이어 이름은 "name" 하위 프로퍼티에 있음
            SerializedProperty layerNameProp = (targetObj.OtherLayerType == TagManager_SettingSbjectBase.EType.SortingLayers) ? layerProp.FindPropertyRelative("name") : null;

            rect.y += 2;

            EditorGUI.LabelField(new Rect(rect.x, rect.y, 60, EditorGUIUtility.singleLineHeight), $"Layer {index}");

            //? 읽기 전용
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.TextField(
                new Rect(rect.x + 70, rect.y, rect.width - 70, EditorGUIUtility.singleLineHeight),
                (targetObj.OtherLayerType == TagManager_SettingSbjectBase.EType.SortingLayers) ? layerNameProp.stringValue : layerProp.stringValue
            );
            EditorGUI.EndDisabledGroup();
        };

        //. (필요하다면) 삭제 불가 처리
        projectOtherLayerList.onCanRemoveCallback = (ReorderableList list) =>
        {
            return false; // 삭제 불가
        };
    }



    ///======================================================================================================================================================    
}


public abstract class TagManager_SettingSbjectBase : ScriptableObject
{
    public SerializedObject LoadTagManagerSerializedObject()
    {
        return new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
    }



    public enum EType
    {
        Tags,
        SortingLayers,
        Layers,
        RenderingLayers
    }



    public SerializedProperty LoadTagManagerProperty(EType type, SerializedObject tagManager)
    {
        if (tagManager == null) { tagManager = LoadTagManagerSerializedObject(); }

        switch (type)
        {
            case EType.Tags: return tagManager.FindProperty("tags");
            case EType.SortingLayers: return tagManager.FindProperty("m_SortingLayers");
            case EType.Layers: return tagManager.FindProperty("layers");
            case EType.RenderingLayers: return tagManager.FindProperty("m_RenderingLayers");
            default: return null;
        }
    }



    public SerializedProperty LoadTagManagerProperty(EType type, out SerializedObject tagManager)
    {
        tagManager = LoadTagManagerSerializedObject();
        return LoadTagManagerProperty(type, tagManager);
    }



    protected void ApplysToProject(EType type, Action<SerializedProperty> action)
    {
        var property = LoadTagManagerProperty(type, out var tagManager);

        action?.Invoke(property);

        tagManager.ApplyModifiedProperties();
        EditorUtility.SetDirty(this);
    }


    protected void LoadsFromProject(EType type, Action<SerializedProperty> action)
    {
        var property = LoadTagManagerProperty(type, out var tagManager);

        action?.Invoke(property);

        tagManager.ApplyModifiedProperties();
        EditorUtility.SetDirty(this);
    }



    ///======================================================================================================================================================



    ///<summary>
    ///설명
    ///</summary>
    [TextArea] public string Description;



    ///======================================================================================================================================================



    /// <summary>
    /// Project에 설정 적용하기
    /// </summary>
    public abstract void ApplyToProject();



    /// <summary>
    /// Project로부터 설정 불러오기
    /// </summary>
    public abstract void LoadFromProject();



    ///======================================================================================================================================================
}




public abstract class TagManager_OtherLayer_SettingSbjectBase : TagManager_SettingSbjectBase
{
    ///======================================================================================================================================================



    [Serializable]
    public class OtherLayerInfo
    {
        public string Name;

        public OtherLayerInfo(string name)
        {
            Name = name;
        }
    }



    [SerializeField] public List<OtherLayerInfo> OtherLayers = new List<OtherLayerInfo>();



    ///======================================================================================================================================================



    public abstract EType OtherLayerType { get; }



    ///======================================================================================================================================================



    /// <summary>
    /// Project에 SortingLayer 설정 적용하기
    /// </summary>
    public override void ApplyToProject()
    {
        ApplysToProject(OtherLayerType, (sortingLayersProp) =>
        {
            sortingLayersProp.ClearArray();

            for (int i = 0; i < OtherLayers.Count; i++)
            {
                sortingLayersProp.InsertArrayElementAtIndex(i);
                SerializedProperty element = sortingLayersProp.GetArrayElementAtIndex(i);
                if (OtherLayerType == EType.SortingLayers)
                {
                    element.FindPropertyRelative("name").stringValue = OtherLayers[i].Name;
                }
                else if (OtherLayerType == EType.RenderingLayers)
                {
                    element.stringValue = OtherLayers[i].Name;
                }
            }
        });
    }



    /// <summary>
    /// Project로부터 SortingLayer 설정 불러오기
    /// </summary>
    public override void LoadFromProject()
    {
        OtherLayers.Clear();

        LoadsFromProject(OtherLayerType, sortingLayersProp =>
        {
            // 실제 Sorting Layer 이름은 각 요소의 "name" 하위 프로퍼티에 들어있음
            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                SerializedProperty layerProp = sortingLayersProp.GetArrayElementAtIndex(i);
                if (OtherLayerType == EType.SortingLayers)
                {
                    SerializedProperty layerNameProp = layerProp.FindPropertyRelative("name");
                    OtherLayers.Add(new OtherLayerInfo(layerNameProp.stringValue));
                }
                else if (OtherLayerType == EType.RenderingLayers)
                {
                    OtherLayers.Add(new OtherLayerInfo(layerProp.stringValue));
                }
            }
        });
    }



    ///======================================================================================================================================================



    private void OnValidate()
    {
        //? "Default"가 리스트에 없으면 추가
        if (OtherLayers.Find(layer => layer.Name == "Default") == null)
        {
            OtherLayers.Insert(0, new OtherLayerInfo("Default"));
        }
    }



    ///======================================================================================================================================================
}