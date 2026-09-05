using Sirenix.OdinInspector.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;



namespace Pan.Util.Editors
{
    /// <summary>
    /// 타입 인스턴스 생성기의 검증 결과와 생성 대상 미리보기를 표시합니다.
    /// </summary>
    [CustomEditor(typeof(TypeInstanceGenerator))]
    public sealed class TypeInstanceGeneratorEditor : OdinEditor
    {
        private List<Type> cachedDerivedTypes;
        private bool showDerivedPreview;
        private string previousBaseTypeName = string.Empty;


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var generator = (TypeInstanceGenerator)target;
            generator.ValidateBaseType();

            DrawDerivedTypePreviewButton(generator);
            ResetPreviewWhenBaseTypeChanged(generator);
            DrawUncheckedValueTypeWarning(generator);
            DrawBaseTypeInformation(generator);
        }


        private void DrawDerivedTypePreviewButton(TypeInstanceGenerator generator)
        {
            if (generator.validatedBaseType == null || !GUILayout.Button("하위 타입 미리보기"))
            {
                return;
            }

            cachedDerivedTypes = SU_Collection_Types
                .GetTypesAssignableTo(generator.validatedBaseType, generator.searchOption)
                .OrderBy(type => type.Name)
                .ToList();
            showDerivedPreview = true;
        }


        private void ResetPreviewWhenBaseTypeChanged(TypeInstanceGenerator generator)
        {
            if (previousBaseTypeName == generator.baseTypeName)
            {
                return;
            }

            cachedDerivedTypes = null;
            showDerivedPreview = false;
            previousBaseTypeName = generator.baseTypeName;
        }


        private static void DrawUncheckedValueTypeWarning(TypeInstanceGenerator generator)
        {
            if (!string.IsNullOrEmpty(generator.valueTypeName))
            {
                EditorGUILayout.HelpBox("Value Type은 유효성 검증 없이 그대로 사용됩니다.", MessageType.Warning);
            }
        }


        private void DrawBaseTypeInformation(TypeInstanceGenerator generator)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Base Type 정보", EditorStyles.boldLabel);

            var text = new StringBuilder();
            AppendBaseTypeInformation(text, generator);

            var richStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                wordWrap = true
            };
            EditorGUILayout.LabelField(text.ToString(), richStyle);
        }


        private void AppendBaseTypeInformation(StringBuilder text, TypeInstanceGenerator generator)
        {
            if (string.IsNullOrEmpty(generator.baseTypeName))
            {
                text.Append("Base Type 이름을 입력하세요.");
                return;
            }

            if (generator.validatedBaseType == null)
            {
                text.Append("<color=red>입력된 '").Append(generator.baseTypeName)
                    .Append("' 은(는) 유효한 타입이 아닙니다.</color>");
                return;
            }

            Type validatedType = generator.validatedBaseType;
            text.Append("<b>유효한 타입:</b> ").Append(validatedType.FullName).Append('\n');
            text.Append("<b>어셈블리:</b> ").Append(validatedType.Assembly.GetName().Name).Append('\n');

            if (!showDerivedPreview || cachedDerivedTypes == null)
            {
                return;
            }

            text.Append("<b>하위 타입 미리보기:</b>\n");
            if (cachedDerivedTypes.Count == 0)
            {
                text.Append("<color=red>하위 타입 없음</color>");
                return;
            }

            for (int i = 0; i < cachedDerivedTypes.Count; i++)
            {
                text.AppendFormat(
                    "<color=cyan>{0}/{1}:</color> {2}\n",
                    i + 1,
                    cachedDerivedTypes.Count,
                    cachedDerivedTypes[i].Name);
            }
        }
    }
}
