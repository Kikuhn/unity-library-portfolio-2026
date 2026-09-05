// SPDX-License-Identifier: MIT
#nullable enable
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.Editor;

namespace Pan.InputSystemExtensions
{
    public sealed class MultiPressOnDownEditor : InputParameterEditor<MultiPressOnDown>
    {
        private static readonly GUIContent GC_TapCount = new("Tap Count");
        private static readonly GUIContent GC_Spacing = new("Max Tap Spacing");
        private static readonly GUIContent GC_Duration = new("Max Tap Duration");
        private static readonly GUIContent GC_PressPoint = new("Press Point");
        private static readonly GUIContent GC_Default = new("Default");
        private static readonly GUIContent GC_OpenSettings = new("Open Input Settings");

        private const string TIP_Spacing = "Uses \"Default Tap Spacing\" set in project-wide input settings.";
        private const string TIP_Duration = "Uses \"Default Tap Time\" set in project-wide input settings.";
        private const string TIP_Press = "Uses \"Default Button Press Point\" set in project-wide input settings.";

        private const float LABEL_WIDTH = 140f;
        private const float BUTTON_WIDTH = 130f;
        private const float TOGGLE_WIDTH = 70f;

        // 박스 축소 스타일
        private static GUIStyle s_HelpBoxTight;
        private static GUIStyle s_HelpTipLabel;

        protected override void OnEnable()
        {
            // Tap Count 기본값만 보증
            if (target.tapCount <= 0) target.tapCount = 2;

            // ★ 사용자가 이미 값을 만졌을 가능성이 있으면 기본 토글을 '강제'로 켜지 않음
            bool looksUninitialized =
                !target.useDefaultTapSpacing &&
                !target.useDefaultTapDuration &&
                !target.useDefaultPressPoint &&
                Mathf.Approximately(target.maxTapSpacing, 0f) &&
                Mathf.Approximately(target.maxTapDuration, 0f) &&
                Mathf.Approximately(target.pressPoint, 0f);

            if (looksUninitialized)
            {
                target.useDefaultTapSpacing = true;
                target.useDefaultTapDuration = true;
                target.useDefaultPressPoint = true;
            }

            if (s_HelpBoxTight == null)
                s_HelpBoxTight = new GUIStyle(EditorStyles.helpBox)
                { padding = new RectOffset(6, 6, 2, 2), margin = new RectOffset(0, 0, 2, 0) };

            if (s_HelpTipLabel == null)
                s_HelpTipLabel = new GUIStyle(EditorStyles.miniLabel)
                { wordWrap = true, margin = new RectOffset(2, 2, 1, 1) };
        }

        public override void OnGUI()
        {
            EditorGUIUtility.labelWidth = LABEL_WIDTH;

            // Tap Count(그대로)
            target.tapCount = Mathf.Max(1, EditorGUILayout.IntField(GC_TapCount, target.tapCount));
            EditorGUILayout.Space(3);

            DrawRow_WithDefaultAndSettings(GC_Spacing, ref target.maxTapSpacing, ref target.useDefaultTapSpacing, GetDefaultTapSpacing(), TIP_Spacing);
            DrawRow_WithDefaultAndSettings(GC_Duration, ref target.maxTapDuration, ref target.useDefaultTapDuration, GetDefaultTapDuration(), TIP_Duration);
            DrawRow_WithDefaultAndSettings(GC_PressPoint, ref target.pressPoint, ref target.useDefaultPressPoint, GetDefaultPressPoint(), TIP_Press);
        }

        private void DrawRow_WithDefaultAndSettings(
            GUIContent label,
            ref float valueRef,
            ref bool useDefaultRef,
            float defaultValue,
            string tipText)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                // Default 켜져 있으면 미리보기로 defaultValue를 보여주되, 값은 보존
                using (new EditorGUI.DisabledScope(useDefaultRef))
                {
                    float shown = useDefaultRef ? defaultValue : valueRef;
                    float edited = EditorGUILayout.FloatField(label, shown);
                    if (!useDefaultRef) valueRef = edited; // Default 해제 상태에서만 실제 값 갱신
                }

                // Default 토글
                bool prev = useDefaultRef;
                useDefaultRef = GUILayout.Toggle(useDefaultRef, GC_Default, GUILayout.Width(TOGGLE_WIDTH));

                // 사용자가 방금 Default를 껐다면, 편집 편의상 현재 표시값(defaultValue)을 초기값으로 복사
                if (prev && !useDefaultRef && Mathf.Approximately(valueRef, 0f))
                    valueRef = defaultValue;

                // 오른쪽 설정 버튼
                if (GUILayout.Button(GC_OpenSettings, GUILayout.Width(BUTTON_WIDTH)))
                    SettingsService.OpenProjectSettings("Project/Input System Package");
            }

            using (new EditorGUILayout.VerticalScope(s_HelpBoxTight))
                GUILayout.Label(tipText, s_HelpTipLabel, GUILayout.MinHeight(EditorGUIUtility.singleLineHeight));

            EditorGUILayout.Space(2);
        }

        private static float GetDefaultTapSpacing()
        {
            var s = UnityEngine.InputSystem.InputSystem.settings;
            if (s == null) return 0.75f;
            var t = s.GetType();
            foreach (var n in new[] { "multiTapDelayTime", "defaultTapSpacing", "multiTapTime", "tapSpacing", "defaultMultiTapDelay" })
            {
                var p = t.GetProperty(n); if (p?.PropertyType == typeof(float)) return (float)p.GetValue(s);
                var f = t.GetField(n); if (f?.FieldType == typeof(float)) return (float)f.GetValue(s);
            }
            return 2f * GetDefaultTapDuration();
        }

        private static float GetDefaultTapDuration()
        {
            var s = UnityEngine.InputSystem.InputSystem.settings;
            if (s == null) return 0.2f;
            var t = s.GetType();
            foreach (var n in new[] { "defaultTapTime", "tapTime", "defaultTapDuration" })
            {
                var p = t.GetProperty(n); if (p?.PropertyType == typeof(float)) return (float)p.GetValue(s);
                var f = t.GetField(n); if (f?.FieldType == typeof(float)) return (float)f.GetValue(s);
            }
            return 0.2f;
        }

        private static float GetDefaultPressPoint()
            => UnityEngine.InputSystem.InputSystem.settings?.defaultButtonPressPoint ?? 0.5f;
    }
}
#endif
