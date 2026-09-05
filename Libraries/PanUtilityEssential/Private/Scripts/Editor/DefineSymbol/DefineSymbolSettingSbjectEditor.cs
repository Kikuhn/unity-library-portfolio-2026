using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

/// <summary>
/// DefineSymbolsManager 전용 CustomEditor
/// </summary>
[CustomEditor(typeof(DefineSymbolSettingSbject))]
public class DefineSymbolSettingSbjectEditor : Editor
{
    private DefineSymbolSettingSbject manager;
    private ReorderableList reorderableList;

    private void OnEnable()
    {
        manager = (DefineSymbolSettingSbject)target;
        InitializeReorderableList();
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("[ Define 심볼 매니저 ]", EditorStyles.boldLabel);

        // ReorderableList 표시
        reorderableList.DoLayoutList();

        EditorGUILayout.Space();

        //. 적용 버튼
        EditorGUILayout.HelpBox(
            "위 목록에 있는 심볼이 기존 프로젝트 정의 심볼과 합쳐져서 PlayerSettings에 적용됩니다.",
            MessageType.Info
        );

        if (GUILayout.Button("프로젝트에 심볼 적용하기", GUILayout.Height(30)))
        {
            DefineSymbolsUtility.ApplySymbolsToProject(manager.DefineSymbols,false);
        }
        if (GUILayout.Button("프로젝트에 심볼 제거하기", GUILayout.Height(30)))
        {
            DefineSymbolsUtility.ApplySymbolsToProject(manager.DefineSymbols, true);
        }

        // 변경사항이 있을 시, ScriptableObject에 저장
        if (GUI.changed)
        {
            EditorUtility.SetDirty(manager);
        }
    }

    /// <summary>
    /// defineSymbols 리스트를 ReorderableList 형태로 표시
    /// </summary>
    private void InitializeReorderableList()
    {
        reorderableList = new ReorderableList(
            manager.DefineSymbols, // 실제 리스트 참조
            typeof(string),
            true,  // 드래그(순서 변경)
            true,  // 헤더 표시
            true,  // + 버튼 표시
            true   // - 버튼 표시
        );

        reorderableList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Define Symbols");
        };

        reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            rect.y += 2;
            manager.DefineSymbols[index] = EditorGUI.TextField(
                new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                manager.DefineSymbols[index]
            );
        };

        reorderableList.onAddCallback = (ReorderableList list) =>
        {
            manager.DefineSymbols.Add("NEW_SYMBOL");
        };

        reorderableList.onRemoveCallback = (ReorderableList list) =>
        {
            manager.DefineSymbols.RemoveAt(list.index);
        };
    }
}