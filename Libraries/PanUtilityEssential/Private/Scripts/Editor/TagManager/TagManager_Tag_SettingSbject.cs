using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;



[CustomEditor(typeof(TagManager_Tag_SettingSbject))]
public class TagManager_Tag_SettingSbjectEditor : TagManager_SettingSbjectBaseEditor<TagManager_Tag_SettingSbject>
{
    ///======================================================================================================================================================



    private ReorderableList soTagList;     //. ScriptableObject 태그 목록
    private ReorderableList projectTagList; //. 프로젝트 태그 목록 (읽기 전용)



    public override string TitleName => "Tag 설정";



    ///======================================================================================================================================================



    protected override void OnInspectorGUI_Current(TagManager_Tag_SettingSbject targetObj)
    {
        //? ScriptableObject 의 Tag 리스트 표시
        if (soTagList == null)
        {
            InitializeSoTagList(targetObj);
        }
        soTagList.DoLayoutList();


        EditorGUILayout.Space();
        GUILayout.Label("Project의 Tags", EditorStyles.boldLabel);


        //? Project(실제 프로젝트) Tag 목록 표시
        if (projectTagList == null)
        {
            InitializeProjectTagList(targetObj);
        }


        TagManagerSO.Update();
        projectTagList.DoLayoutList();
        TagManagerSO.ApplyModifiedProperties();
    }



    //? ScriptableObject (TagSettingSbject.Tags) 를 표시하는 ReorderableList
    private void InitializeSoTagList(TagManager_Tag_SettingSbject targetObj)
    {
        soTagList = new ReorderableList(
            targetObj.Tags,
            typeof(TagManager_Tag_SettingSbject.TagInfo),
            true,  // 드래그 가능
            true,  // 헤더 표시
            true,  // 추가 버튼
            true   // 삭제 버튼
        );

        soTagList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "ScriptableObject Tag 목록");
        };

        soTagList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            var tagInfo = targetObj.Tags[index];
            rect.y += 2;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, 40, EditorGUIUtility.singleLineHeight), $"Tag {index}");
            tagInfo.TagName = EditorGUI.TextField(
                new Rect(rect.x + 50, rect.y, rect.width - 50, EditorGUIUtility.singleLineHeight),
                tagInfo.TagName
            );
        };

        //. + 버튼 눌렀을 때
        soTagList.onAddCallback = (ReorderableList list) =>
        {
            targetObj.Tags.Add(new TagManager_Tag_SettingSbject.TagInfo("NewTag"));
        };

        //. - 버튼 눌렀을 때
        soTagList.onRemoveCallback = (ReorderableList list) =>
        {
            // Default, Untagged 같은 특정 태그를 삭제 못하게 하려면 검사 로직 추가
            // if (tagSetting.Tags[list.index].TagName == "Untagged") { ... }
            targetObj.Tags.RemoveAt(list.index);
        };
    }



    //? Project Tag 목록 (m_Tags) 을 읽기 전용으로 보여주는 ReorderableList
    private void InitializeProjectTagList(TagManager_Tag_SettingSbject targetObj)
    {
        var tagsProp = targetObj.LoadTagManagerProperty(TagManager_SettingSbjectBase.EType.Tags, TagManagerSO);

        projectTagList = new ReorderableList(
            TagManagerSO,
            tagsProp,
            false, // 드래그 비활성화
            false,  // 헤더 표시
            false, // + 버튼 비활성화
            false  // - 버튼 비활성화
        );

        projectTagList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            if (index < 0 || index >= tagsProp.arraySize) return;

            SerializedProperty sp = tagsProp.GetArrayElementAtIndex(index);

            rect.y += 2;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, 40, EditorGUIUtility.singleLineHeight), $"Tag {index}");

            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.TextField(
                new Rect(rect.x + 50, rect.y, rect.width - 50, EditorGUIUtility.singleLineHeight),
                sp.stringValue
            );
            EditorGUI.EndDisabledGroup();
        };
    }



    ///======================================================================================================================================================
}



[CreateAssetMenu(fileName = "TagSetting", menuName = CreateAssetMenuInfo.TAG_SETTINGS, order = CreateAssetMenuInfo.TAG_SETTINGS_ORDER)]
public class TagManager_Tag_SettingSbject : TagManager_SettingSbjectBase
{
    ///======================================================================================================================================================



    [Serializable]
    public class TagInfo
    {
        public string TagName;

        public TagInfo(string name)
        {
            TagName = name;
        }
    }



    [SerializeField] public List<TagInfo> Tags = new List<TagInfo>();



    ///======================================================================================================================================================



    /// <summary>
    /// Project에 Tag 설정 적용하기
    /// </summary>
    public override void ApplyToProject()
    {
        ApplysToProject(EType.Tags, (tagsProp) =>
        {
            //. 기존 Tag 목록 초기화
            tagsProp.ClearArray();

            //. ScriptableObject 내 Tags 목록을 모두 삽입
            for (int i = 0; i < Tags.Count; i++)
            {
                tagsProp.InsertArrayElementAtIndex(i);
                SerializedProperty element = tagsProp.GetArrayElementAtIndex(i);
                element.stringValue = Tags[i].TagName;
            }
        });
    }



    /// <summary>
    /// Project로부터 Tag 설정 불러오기
    /// </summary>
    public override void LoadFromProject()
    {
        Tags.Clear();


        LoadsFromProject(EType.Tags, tagsProp =>
        {
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty element = tagsProp.GetArrayElementAtIndex(i);
                //. string 배열이므로 바로 element.stringValue 사용
                string tagStr = element.stringValue;
                Tags.Add(new TagInfo(tagStr));
            }
        });
    }



    ///======================================================================================================================================================
}