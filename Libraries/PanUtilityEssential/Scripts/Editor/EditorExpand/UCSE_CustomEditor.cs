using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;
using Object = UnityEngine.Object;
using System.Linq.Expressions;
using System.Linq;
using Pan.Util;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using System.Reflection;

using UnityEditor;
using UnityEditorInternal;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using System.Text.RegularExpressions;



namespace Pan.Util.Editors
{
    ///======================================================================================================================================================



    /// <summary>
    /// 유니티 커스텀 에디터를 위한 Static 유틸리티 메서드들
    /// </summary>
    public static class SU_CustomEditor
    {
        ///======================================================================================================================================================



        //? GUI 스타일 저장소



        #region GUI 스타일 저장소



        private struct GUIStyleKey
        {
            public GUIStyleKey(Color? textColor, FontStyle? fontStyle, bool richText = false)
            {
                TextColor = textColor;
                FontStyle = fontStyle;
                RichText = richText;
            }

            public Color? TextColor;
            public FontStyle? FontStyle;
            public bool RichText;
        }



        /// <summary>
        /// 텍스트 라벨 타입
        /// </summary>
        public enum LabelHeadType { H1, H2, H3 }



        //? GUI 스타일 캐싱 딕셔너리
        private static readonly Dictionary<GUIStyleKey, GUIStyle> GUIStyle_Caching = new Dictionary<GUIStyleKey, GUIStyle>();


        /// <summary>
        /// 헤더1 GUIStyle
        /// </summary>
        public static readonly GUIStyle LabelStyle_Head1 = new GUIStyle(EditorStyles.largeLabel)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
            richText = true,
            fixedHeight = 24
        };

        /// <summary>
        /// 헤더2 GUIStyle
        /// </summary>
        public static readonly GUIStyle LabelStyle_Head2 = new GUIStyle(EditorStyles.largeLabel)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
            richText = true,
            fixedHeight = 26
        };

        /// <summary>
        /// 헤더3 GUIStyle
        /// </summary>
        public static readonly GUIStyle LabelStyle_Head3 = new GUIStyle(EditorStyles.largeLabel)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
            richText = true,
            fixedHeight = 30
        };

        /// <summary>
        /// 폴드아웃용 헤더1 GUIStyle
        /// </summary>
        public static readonly GUIStyle LabelStyle_FoldOutHead1 = new GUIStyle(EditorStyles.foldout)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
            richText = true,
            fixedHeight = 14
        };

        /// <summary>
        /// 폴드아웃용 헤더2 GUIStyle
        /// </summary>
        public static readonly GUIStyle LabelStyle_FoldOutHead2 = new GUIStyle(EditorStyles.foldout)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
            richText = true,
            fixedHeight = 16
        };

        /// <summary>
        /// 폴드아웃용 헤더3 GUIStyle
        /// </summary>
        public static readonly GUIStyle LabelStyle_FoldOutHead3 = new GUIStyle(EditorStyles.foldout)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
            richText = true,
            fixedHeight = 20
        };



        #endregion



        //? GUI 스타일 만들기



        #region GUI 스타일 만들기



        /// <summary>
        /// 전달된 <see cref="GUIStyle"/>을 기반으로 텍스트 색상과 폰트 스타일, 그리고 RichText 옵션을 적용한 새 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// 캐싱 딕셔너리(<see cref="GUIStyle_Caching"/>)를 사용해 동일한 설정이 이미 존재한다면 재사용합니다.
        /// </summary>
        /// <param name="guiStyle">원본 GUIStyle</param>
        /// <param name="textColor">텍스트 색상 (null이면 적용 안 함)</param>
        /// <param name="fontStyle">폰트 스타일 (null이면 적용 안 함)</param>
        /// <param name="richText">리치 텍스트 사용 여부</param>
        /// <returns>생성 또는 재사용된 GUIStyle</returns>
        public static GUIStyle CustomLabelStyleNew(GUIStyle guiStyle, Color? textColor, FontStyle? fontStyle, bool richText = true)
        {
            var key = new GUIStyleKey(textColor, fontStyle, richText);

            if (!GUIStyle_Caching.TryGetValue(key, out var cachedStyle))
            {
                cachedStyle = new GUIStyle(guiStyle);

                if (textColor.HasValue)
                {
                    cachedStyle.normal.textColor = textColor.Value;
                }

                if (fontStyle.HasValue)
                {
                    cachedStyle.fontStyle = fontStyle.Value;
                }

                cachedStyle.richText = richText;
                GUIStyle_Caching[key] = cachedStyle;
            }

            return cachedStyle;
        }



        /// <summary>
        /// 텍스트 색상을 지정하여 새 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <param name="textColor">텍스트 색상</param>
        /// <returns>생성 또는 재사용된 GUIStyle</returns>
        public static GUIStyle NewGUIStyle(Color textColor)
        {
            var key = new GUIStyleKey(textColor, null);

            if (!GUIStyle_Caching.TryGetValue(key, out var cachedStyle))
            {
                cachedStyle = new GUIStyle(GUIStyle.none)
                {
                    normal = { textColor = textColor }
                };

                GUIStyle_Caching[key] = cachedStyle;
            }

            return cachedStyle;
        }



        /// <summary>
        /// 폰트 스타일을 지정하여 새 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <param name="fontStyle">폰트 스타일</param>
        /// <returns>생성 또는 재사용된 GUIStyle</returns>
        public static GUIStyle NewGUIStyle(FontStyle fontStyle)
        {
            var key = new GUIStyleKey(null, fontStyle);

            if (!GUIStyle_Caching.TryGetValue(key, out var cachedStyle))
            {
                cachedStyle = new GUIStyle(GUIStyle.none)
                {
                    fontStyle = fontStyle
                };

                GUIStyle_Caching[key] = cachedStyle;
            }

            return cachedStyle;
        }



        /// <summary>
        /// 텍스트 색상과 폰트 스타일을 지정하여 새 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <param name="textColor">텍스트 색상</param>
        /// <param name="fontStyle">폰트 스타일</param>
        /// <returns>생성 또는 재사용된 GUIStyle</returns>
        public static GUIStyle NewGUIStyle(Color textColor, FontStyle fontStyle)
        {
            var key = new GUIStyleKey(textColor, fontStyle);

            if (!GUIStyle_Caching.TryGetValue(key, out var cachedStyle))
            {
                cachedStyle = new GUIStyle(GUIStyle.none)
                {
                    normal = { textColor = textColor },
                    fontStyle = fontStyle
                };

                GUIStyle_Caching[key] = cachedStyle;
            }

            return cachedStyle;
        }



        /// <summary>
        /// <see cref="GUI.skin.label"/>을 기반으로 리치 텍스트가 활성화된 새 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <returns>생성 또는 재사용된 GUIStyle</returns>
        public static GUIStyle NewGUIStyle_Label()
        {
            var key = new GUIStyleKey(null, null, true);

            if (!GUIStyle_Caching.TryGetValue(key, out var cachedStyle))
            {
                cachedStyle = new GUIStyle(GUI.skin.label)
                {
                    richText = true
                };

                GUIStyle_Caching[key] = cachedStyle;
            }

            return cachedStyle;
        }



        /// <summary>
        /// 헤더1 스타일을 복제하여 새로운 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <returns>새로운 헤더1 GUIStyle</returns>
        public static GUIStyle LabelStyle_Head1New() => CustomLabelStyleNew(LabelStyle_Head1, null, null, LabelStyle_Head1.richText);

        /// <summary>
        /// 헤더2 스타일을 복제하여 새로운 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <returns>새로운 헤더2 GUIStyle</returns>
        public static GUIStyle LabelStyle_Head2New() => CustomLabelStyleNew(LabelStyle_Head2, null, null, LabelStyle_Head2.richText);

        /// <summary>
        /// 헤더3 스타일을 복제하여 새로운 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <returns>새로운 헤더3 GUIStyle</returns>
        public static GUIStyle LabelStyle_Head3New() => CustomLabelStyleNew(LabelStyle_Head3, null, null, LabelStyle_Head3.richText);



        /// <summary>
        /// 헤더1 스타일을 복제하여 텍스트 색상을 변경한 새로운 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <param name="textColor">변경할 텍스트 색상</param>
        /// <returns>새로운 헤더1 GUIStyle</returns>
        public static GUIStyle LabelStyle_Head1New(Color textColor) => CustomLabelStyleNew(LabelStyle_Head1, textColor, null, LabelStyle_Head1.richText);

        /// <summary>
        /// 헤더2 스타일을 복제하여 텍스트 색상을 변경한 새로운 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <param name="textColor">변경할 텍스트 색상</param>
        /// <returns>새로운 헤더2 GUIStyle</returns>
        public static GUIStyle LabelStyle_Head2New(Color textColor) => CustomLabelStyleNew(LabelStyle_Head2, textColor, null, LabelStyle_Head2.richText);

        /// <summary>
        /// 헤더3 스타일을 복제하여 텍스트 색상을 변경한 새로운 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <param name="textColor">변경할 텍스트 색상</param>
        /// <returns>새로운 헤더3 GUIStyle</returns>
        public static GUIStyle LabelStyle_Head3New(Color textColor) => CustomLabelStyleNew(LabelStyle_Head3, textColor, null, LabelStyle_Head3.richText);



        /// <summary>
        /// 헤더1 스타일을 복제하여 폰트 스타일을 변경한 새로운 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <param name="fontStyle">적용할 폰트 스타일</param>
        /// <returns>새로운 헤더1 GUIStyle</returns>
        public static GUIStyle LabelStyle_Head1New(FontStyle fontStyle) => CustomLabelStyleNew(LabelStyle_Head1, null, fontStyle, LabelStyle_Head1.richText);

        /// <summary>
        /// 헤더2 스타일을 복제하여 폰트 스타일을 변경한 새로운 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <param name="fontStyle">적용할 폰트 스타일</param>
        /// <returns>새로운 헤더2 GUIStyle</returns>
        public static GUIStyle LabelStyle_Head2New(FontStyle fontStyle) => CustomLabelStyleNew(LabelStyle_Head2, null, fontStyle, LabelStyle_Head2.richText);

        /// <summary>
        /// 헤더3 스타일을 복제하여 폰트 스타일을 변경한 새로운 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <param name="fontStyle">적용할 폰트 스타일</param>
        /// <returns>새로운 헤더3 GUIStyle</returns>
        public static GUIStyle LabelStyle_Head3New(FontStyle fontStyle) => CustomLabelStyleNew(LabelStyle_Head3, null, fontStyle, LabelStyle_Head3.richText);



        /// <summary>
        /// 헤더1 스타일을 복제하여 텍스트 색상과 폰트 스타일을 모두 변경한 새로운 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <param name="textColor">변경할 텍스트 색상</param>
        /// <param name="fontStyle">적용할 폰트 스타일</param>
        /// <returns>새로운 헤더1 GUIStyle</returns>
        public static GUIStyle LabelStyle_Head1New(Color textColor, FontStyle fontStyle) => CustomLabelStyleNew(LabelStyle_Head1, textColor, fontStyle, LabelStyle_Head1.richText);

        /// <summary>
        /// 헤더2 스타일을 복제하여 텍스트 색상과 폰트 스타일을 모두 변경한 새로운 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <param name="textColor">변경할 텍스트 색상</param>
        /// <param name="fontStyle">적용할 폰트 스타일</param>
        /// <returns>새로운 헤더2 GUIStyle</returns>
        public static GUIStyle LabelStyle_Head2New(Color textColor, FontStyle fontStyle) => CustomLabelStyleNew(LabelStyle_Head2, textColor, fontStyle, LabelStyle_Head2.richText);

        /// <summary>
        /// 헤더3 스타일을 복제하여 텍스트 색상과 폰트 스타일을 모두 변경한 새로운 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <param name="textColor">변경할 텍스트 색상</param>
        /// <param name="fontStyle">적용할 폰트 스타일</param>
        /// <returns>새로운 헤더3 GUIStyle</returns>
        public static GUIStyle LabelStyle_Head3New(Color textColor, FontStyle fontStyle) => CustomLabelStyleNew(LabelStyle_Head3, textColor, fontStyle, LabelStyle_Head3.richText);



        /// <summary>
        /// 폴드아웃용 헤더1 스타일을 복제하여 새로운 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <returns>새로운 FoldOut 헤더1 GUIStyle</returns>
        public static GUIStyle LabelStyle_FoldOutHead1New() => CustomLabelStyleNew(LabelStyle_FoldOutHead1, null, null, LabelStyle_FoldOutHead1.richText);

        /// <summary>
        /// 폴드아웃용 헤더2 스타일을 복제하여 새로운 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <returns>새로운 FoldOut 헤더2 GUIStyle</returns>
        public static GUIStyle LabelStyle_FoldOutHead2New() => CustomLabelStyleNew(LabelStyle_FoldOutHead2, null, null, LabelStyle_FoldOutHead2.richText);

        /// <summary>
        /// 폴드아웃용 헤더3 스타일을 복제하여 새로운 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <returns>새로운 FoldOut 헤더3 GUIStyle</returns>
        public static GUIStyle LabelStyle_FoldOutHead3New() => CustomLabelStyleNew(LabelStyle_FoldOutHead3, null, null, LabelStyle_FoldOutHead3.richText);



        /// <summary>
        /// 폴드아웃용 헤더1 스타일을 복제하여 텍스트 색상을 변경한 새로운 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <param name="textColor">변경할 텍스트 색상</param>
        /// <returns>새로운 FoldOut 헤더1 GUIStyle</returns>
        public static GUIStyle LabelStyle_FoldOutHead1New(Color textColor) => CustomLabelStyleNew(LabelStyle_FoldOutHead1, textColor, null, LabelStyle_FoldOutHead1.richText);

        /// <summary>
        /// 폴드아웃용 헤더2 스타일을 복제하여 텍스트 색상을 변경한 새로운 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <param name="textColor">변경할 텍스트 색상</param>
        /// <returns>새로운 FoldOut 헤더2 GUIStyle</returns>
        public static GUIStyle LabelStyle_FoldOutHead2New(Color textColor) => CustomLabelStyleNew(LabelStyle_FoldOutHead2, textColor, null, LabelStyle_FoldOutHead2.richText);

        /// <summary>
        /// 폴드아웃용 헤더3 스타일을 복제하여 텍스트 색상을 변경한 새로운 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <param name="textColor">변경할 텍스트 색상</param>
        /// <returns>새로운 FoldOut 헤더3 GUIStyle</returns>
        public static GUIStyle LabelStyle_FoldOutHead3New(Color textColor) => CustomLabelStyleNew(LabelStyle_FoldOutHead3, textColor, null, LabelStyle_FoldOutHead3.richText);



        /// <summary>
        /// 폴드아웃용 헤더1 스타일을 복제하여 폰트 스타일을 변경한 새로운 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <param name="fontStyle">적용할 폰트 스타일</param>
        /// <returns>새로운 FoldOut 헤더1 GUIStyle</returns>
        public static GUIStyle LabelStyle_FoldOutHead1New(FontStyle fontStyle) => CustomLabelStyleNew(LabelStyle_FoldOutHead1, null, fontStyle, LabelStyle_FoldOutHead1.richText);

        /// <summary>
        /// 폴드아웃용 헤더2 스타일을 복제하여 폰트 스타일을 변경한 새로운 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <param name="fontStyle">적용할 폰트 스타일</param>
        /// <returns>새로운 FoldOut 헤더2 GUIStyle</returns>
        public static GUIStyle LabelStyle_FoldOutHead2New(FontStyle fontStyle) => CustomLabelStyleNew(LabelStyle_FoldOutHead2, null, fontStyle, LabelStyle_FoldOutHead2.richText);

        /// <summary>
        /// 폴드아웃용 헤더3 스타일을 복제하여 폰트 스타일을 변경한 새로운 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <param name="fontStyle">적용할 폰트 스타일</param>
        /// <returns>새로운 FoldOut 헤더3 GUIStyle</returns>
        public static GUIStyle LabelStyle_FoldOutHead3New(FontStyle fontStyle) => CustomLabelStyleNew(LabelStyle_FoldOutHead3, null, fontStyle, LabelStyle_FoldOutHead3.richText);



        /// <summary>
        /// 폴드아웃용 헤더1 스타일을 복제하여 텍스트 색상과 폰트 스타일을 모두 변경한 새로운 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <param name="textColor">변경할 텍스트 색상</param>
        /// <param name="fontStyle">적용할 폰트 스타일</param>
        /// <returns>새로운 FoldOut 헤더1 GUIStyle</returns>
        public static GUIStyle LabelStyle_FoldOutHead1New(Color textColor, FontStyle fontStyle) => CustomLabelStyleNew(LabelStyle_FoldOutHead1, textColor, fontStyle, LabelStyle_FoldOutHead1.richText);

        /// <summary>
        /// 폴드아웃용 헤더2 스타일을 복제하여 텍스트 색상과 폰트 스타일을 모두 변경한 새로운 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <param name="textColor">변경할 텍스트 색상</param>
        /// <param name="fontStyle">적용할 폰트 스타일</param>
        /// <returns>새로운 FoldOut 헤더2 GUIStyle</returns>
        public static GUIStyle LabelStyle_FoldOutHead2New(Color textColor, FontStyle fontStyle) => CustomLabelStyleNew(LabelStyle_FoldOutHead2, textColor, fontStyle, LabelStyle_FoldOutHead2.richText);

        /// <summary>
        /// 폴드아웃용 헤더3 스타일을 복제하여 텍스트 색상과 폰트 스타일을 모두 변경한 새로운 <see cref="GUIStyle"/>을 생성해 반환합니다.
        /// </summary>
        /// <param name="textColor">변경할 텍스트 색상</param>
        /// <param name="fontStyle">적용할 폰트 스타일</param>
        /// <returns>새로운 FoldOut 헤더3 GUIStyle</returns>
        public static GUIStyle LabelStyle_FoldOutHead3New(Color textColor, FontStyle fontStyle) => CustomLabelStyleNew(LabelStyle_FoldOutHead3, textColor, fontStyle, LabelStyle_FoldOutHead3.richText);



        #endregion



        ///======================================================================================================================================================



        //? GUIContent 



        #region GUI Conetent 저장소



        private struct GUIContentKey
        {
            public GUIContentKey(string label, string tooltip)
            {
                Label = label;
                Tooltip = tooltip;
            }

            public string Label;
            public string Tooltip;
        }

        /// <summary>
        /// GUIContent 캐싱용 전역 딕셔너리
        /// </summary>
        private static readonly Dictionary<GUIContentKey, GUIContent> GUIContent_Caching = new Dictionary<GUIContentKey, GUIContent>();



        #endregion



        //? GUIContent 만들기



        #region GUI Conetent 저장소



        /// <summary>라벨과 툴팁을 받아와 GUIContent를 생성해 반환</summary>
        public static GUIContent NewGUIContent_Label(string label, string tooltip = "")
        {
            if (string.IsNullOrEmpty(label))
            {
                return GUIContent.none;
            }

            var key = new GUIContentKey(label, tooltip);

            if (!GUIContent_Caching.TryGetValue(key, out var guicontent))
            {
                guicontent = new GUIContent(label, tooltip);
                GUIContent_Caching[key] = guicontent;
            }

            return guicontent;
        }



        #endregion



        ///======================================================================================================================================================



        //? GUI 레이아웃 메서드



        #region GUI 레이아웃 메서드



        /// <summary>
        /// <paramref name="action"/> 안에 정의된 GUI 요소들을 가로 방향(Horizontal)으로 배치하여 그립니다.
        /// </summary>
        /// <param name="action">가로로 배치할 GUI 요소들을 그리는 액션</param>
        public static void HorizontalGUI(Action action)
        {
            GUILayout.BeginHorizontal();
            action?.Invoke();
            GUILayout.EndHorizontal();
        }



        /// <summary>
        /// <paramref name="action"/> 안에 정의된 GUI 요소들을 세로방향+helpBox 로 배치하여 그립니다.
        /// </summary>
        /// <param name="action">세로로 배치할 GUI 요소들을 그리는 액션</param>
        public static void VerticalHelpBox(Action action)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15); // HelpBox 자체의 들여쓰기 적용

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                int previousIndent = EditorGUI.indentLevel; // 현재 indentLevel 저장
                EditorGUI.indentLevel = 0; // 내부 요소가 indent 영향 받지 않도록 설정

                GUILayout.BeginHorizontal();
                GUILayout.Space(5); // 내부 요소 왼쪽 여백 추가
                GUILayout.BeginVertical();

                action?.Invoke();

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

                EditorGUI.indentLevel = previousIndent; // 원래 indentLevel 복원
            }
            //GUILayout.Space(EditorGUI.indentLevel * -15);
            EditorGUILayout.EndHorizontal();
        }





        /// <summary>
        /// 폴드아웃(Foldout) UI를 생성하고, 펼쳐진 상태면 <paramref name="action"/>을 실행합니다.
        /// </summary>
        /// <param name="fold">접힘/펼침 여부를 나타내는 bool 참조</param>
        /// <param name="guiContent">폴드아웃 라벨에 표시될 <see cref="GUIContent"/> (텍스트, 툴팁, 이미지 등)</param>
        /// <param name="guiStyle">폴드아웃 라벨에 적용할 <see cref="GUIStyle"/>. null이면 기본 <see cref="EditorStyles.foldout"/> 사용</param>
        /// <param name="color">폴드아웃 라벨의 텍스트 색상 (null이면 적용 안 함)</param>
        /// <param name="action">폴드아웃이 펼쳐졌을 때 그릴 GUI 요소들</param>
        public static void FoldOut(ref bool fold, GUIContent guiContent, GUIStyle guiStyle, Color? color, Action action, bool useIndentLevel = true)
        {
            GUIStyle applyGUIStyle = (guiStyle != null) ? new GUIStyle(guiStyle) : new GUIStyle(EditorStyles.foldout);

            if (color.HasValue)
            {
                applyGUIStyle.normal.textColor = color.Value;
                applyGUIStyle.onNormal.textColor = color.Value;
            }

            applyGUIStyle.richText = true;

            fold = EditorGUILayout.Foldout(fold, guiContent, applyGUIStyle);

            if (fold)
            {
                if (useIndentLevel) { Format_IndentLevel_Plus(); }
                action?.Invoke();
                if (useIndentLevel) { Format_IndentLevel_Minus(); }
            }
        }

        /// <summary>
        /// 폴드아웃(Foldout) UI를 생성하고, 펼쳐진 상태면 <paramref name="action"/>을 실행합니다.
        /// (간단히 문자열과 툴팁만으로 라벨을 구성할 때 사용)
        /// </summary>
        /// <param name="fold">접힘/펼침 여부를 나타내는 bool 참조</param>
        /// <param name="label">라벨에 표시될 텍스트</param>
        /// <param name="tooltip">라벨에 표시될 툴팁</param>
        /// <param name="action">폴드아웃이 펼쳐졌을 때 그릴 GUI 요소들</param>
        public static void FoldOut(ref bool fold, string label, string tooltip, Action action, bool useIndentLevel = true)
        {
            FoldOut(ref fold, NewGUIContent_Label(label, tooltip), null, null, action, useIndentLevel);
        }

        /// <summary>
        /// 폴드아웃(Foldout) UI를 생성하고, 펼쳐진 상태면 <paramref name="action"/>을 실행합니다.
        /// (라벨 텍스트만 지정하는 간단한 버전)
        /// </summary>
        /// <param name="fold">접힘/펼침 여부를 나타내는 bool 참조</param>
        /// <param name="label">라벨에 표시될 텍스트</param>
        /// <param name="action">폴드아웃이 펼쳐졌을 때 그릴 GUI 요소들</param>
        public static void FoldOut(ref bool fold, string label, Action action, bool useIndentLevel = true)
        {
            FoldOut(ref fold, NewGUIContent_Label(label), null, null, action, useIndentLevel);
        }

        /// <summary>
        /// <see cref="LabelHeadType"/>에 따라 자동으로 폴드아웃 헤더를 생성하고, 펼쳐진 상태면 <paramref name="action"/>을 실행합니다.
        /// </summary>
        /// <param name="label">라벨에 표시될 텍스트</param>
        /// <param name="headType">헤더의 폰트 크기/스타일을 결정할 <see cref="LabelHeadType"/></param>
        /// <param name="fold">접힘/펼침 여부를 나타내는 bool 참조</param>
        /// <param name="color">헤더 라벨의 색상 (null이면 적용 안 함)</param>
        /// <param name="action">폴드아웃이 펼쳐졌을 때 그릴 GUI 요소들</param>
        /// <param name="endHorizontalLine">true면 폴드아웃 후에 수평 라인을 그립니다. false면 간단히 여백 처리</param>
        public static void AutoLabelFoldOut_Head(string label, LabelHeadType headType, ref bool fold, Color? color, Action action, bool endHorizontalLine = true, bool useIndentLevel = true)
        {
            GUIStyle guiStyle;

            switch (headType)
            {
                case LabelHeadType.H1:
                guiStyle = LabelStyle_FoldOutHead1;
                break;
                case LabelHeadType.H2:
                guiStyle = LabelStyle_FoldOutHead2;
                break;
                case LabelHeadType.H3:
                guiStyle = LabelStyle_FoldOutHead3;
                break;
                default:
                guiStyle = null;
                break;
            }

            FoldOut(ref fold, NewGUIContent_Label(label), guiStyle, color, action, useIndentLevel);

            if (endHorizontalLine)
            {
                Format_HorizontalLine();
            }
            else
            {
                EditorGUILayout.Space();
            }
        }

        /// <summary>
        /// <see cref="LabelHeadType"/>에 따라 자동으로 폴드아웃 헤더를 생성하고, 펼쳐진 상태면 <paramref name="action"/>을 실행합니다.
        /// (텍스트 색상 지정이 필요 없는 간단한 버전)
        /// </summary>
        /// <param name="label">라벨에 표시될 텍스트</param>
        /// <param name="headType">헤더의 폰트 크기/스타일을 결정할 <see cref="LabelHeadType"/></param>
        /// <param name="fold">접힘/펼침 여부를 나타내는 bool 참조</param>
        /// <param name="action">폴드아웃이 펼쳐졌을 때 그릴 GUI 요소들</param>
        /// <param name="endHorizontalLine">true면 폴드아웃 후에 수평 라인을 그립니다. false면 간단히 여백 처리</param>
        public static void AutoLabelFoldOut_Head(string label, LabelHeadType headType, ref bool fold, Action action, bool endHorizontalLine = true, bool useIndentLevel = true)
        {
            GUIStyle guiStyle;

            switch (headType)
            {
                case LabelHeadType.H1:
                guiStyle = LabelStyle_FoldOutHead1;
                break;
                case LabelHeadType.H2:
                guiStyle = LabelStyle_FoldOutHead2;
                break;
                case LabelHeadType.H3:
                guiStyle = LabelStyle_FoldOutHead3;
                break;
                default:
                guiStyle = null;
                break;
            }

            FoldOut(ref fold, NewGUIContent_Label(label), guiStyle, null, action, useIndentLevel);

            if (endHorizontalLine)
            {
                Format_HorizontalLine();
            }
            else
            {
                EditorGUILayout.Space();
            }
        }



        #endregion



        ///======================================================================================================================================================



        //? 스크롤 뷰 (250311 재사용이 안되는 비효율적인 상태, 정리 미완)



        /// <summary>
        /// 일반 스크롤 뷰를 그린다.
        /// </summary>
        /// <param name="scrollPos">스크롤 좌표</param>
        /// <param name="action">내부 UI를 렌더링할 액션</param>
        /// <param name="options">추가적인 GUILayout 옵션</param>
        public static void ScrollView(ref Vector2 scrollPos, Action action, params GUILayoutOption[] options)
        {
            //. 스크롤 뷰 시작
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, options);
            action?.Invoke(); // 내부 요소 렌더링
            EditorGUILayout.EndScrollView(); // 스크롤 뷰 종료
        }



        /// <summary>
        /// 컬렉션을 출력하는 스크롤 뷰
        /// </summary>
        /// <typeparam name="T">컬렉션의 요소 타입</typeparam>
        /// <param name="scrollPos">스크롤 좌표</param>
        /// <param name="collection">출력할 데이터 컬렉션</param>
        /// <param name="elementsAction">각 요소를 출력하는 액션</param>
        /// <param name="options">추가적인 GUILayout 옵션</param>
        public static void ScrollView_Collection<T>(ref Vector2 scrollPos, IEnumerable<T> collection, Action<T> elementsAction, params GUILayoutOption[] options)
        {
            //. 스크롤 뷰 시작
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, options);

            if (elementsAction != null)
            {
                foreach (var item in collection)
                {
                    elementsAction.Invoke(item); // 각 요소를 개별적으로 렌더링
                }
            }

            EditorGUILayout.EndScrollView(); // 스크롤 뷰 종료
        }



        /// <summary>
        /// 컬렉션을 출력하는 스크롤 뷰 (검색 필터 포함)
        /// </summary>
        /// <typeparam name="T">컬렉션 요소 타입</typeparam>
        /// <param name="scrollPos">스크롤 좌표</param>
        /// <param name="searchQuery">검색 문자열 (ref)</param>
        /// <param name="collection">출력할 데이터 컬렉션</param>
        /// <param name="elementsAction">각 요소를 출력하는 액션</param>
        /// <param name="searchField">검색할 필드를 반환하는 함수</param>
        /// <param name="options">추가적인 GUILayout 옵션</param>
        public static void ScrollView_Collection<T>(ref Vector2 scrollPos, ref string searchQuery, out int searchCount, IEnumerable<T> collection, Action<T> elementsAction, Func<T, string> searchField, bool drawElementsInfo, params GUILayoutOption[] options)
        {
            //? 검색 필터
            List<T> tempList = new List<T>(collection);
            for (int i = tempList.Count - 1; i >= 0; i--)
            {
                T item = tempList[i];
                if (!string.IsNullOrEmpty(searchQuery) &&
                    !searchField(item).Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                {
                    tempList.RemoveAt(i);
                }
            }
            searchCount = tempList.Count;


            //. 요소 카운트 라벨
            if (drawElementsInfo)
            {
                string text = (string.IsNullOrEmpty(searchQuery)) ?
                    $"Elements: <b><i><color=#2ecc71>{collection.Count()}</color></i></b>" :
                    $"Elements: <b><i><color=#4fc1e9>{tempList.Count}</color></i></b> <b>/</b> <b><i><color=#2ecc71>{collection.Count()}</color></i></b>";
                LabelField_TextAutoWidthHeight(text);
            }


            //. 검색창 UI
            DrawSearchField(ref searchQuery);


            //. 스크롤 뷰
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, options);
            foreach (var item in tempList)
            {
                elementsAction.Invoke(item);
            }
            EditorGUILayout.EndScrollView();
        }



        /// <summary>
        /// (공통) 드래그로 높이 조절하는 핸들을 그리는 메서드
        /// </summary>
        /// <param name="height">현재 높이</param>
        /// <param name="minHeight">최소 높이</param>
        /// <param name="maxHeight">최대 높이</param>
        private static void DrawResizeHandle(ref float height, float minHeight, float maxHeight)
        {
            //. 스크롤 뷰의 마지막 Rect를 기반으로 핸들 위치를 설정
            Rect resizeHandle = GUILayoutUtility.GetLastRect();
            resizeHandle.y += resizeHandle.height - 3;
            resizeHandle.height = 6;
            resizeHandle.x += EditorGUI.indentLevel * 15;

            //. 마우스 오버 시 색상 변경
            bool mouseHover = resizeHandle.Contains(Event.current.mousePosition);
            Color handleColor = mouseHover
                ? new Color(0.5f, 0.5f, 0.5f, 0.8f)
                : new Color(0.3f, 0.3f, 0.3f, 0.6f);
            EditorGUI.DrawRect(resizeHandle, handleColor);

            //. 커서 모양 변경을 위해 컨트롤 ID를 얻어옴
            int resizeControlID = GUIUtility.GetControlID(FocusType.Passive);
            EditorGUIUtility.AddCursorRect(resizeHandle, MouseCursor.ResizeVertical, resizeControlID);

            //. 이벤트 처리
            Event e = Event.current;
            EventType eventType = e.GetTypeForControl(resizeControlID);

            switch (eventType)
            {
                case EventType.MouseDown:
                //! 핸들 영역 클릭 시 드래그 독점
                if (e.button == 0 && resizeHandle.Contains(e.mousePosition))
                {
                    GUIUtility.hotControl = resizeControlID;
                    e.Use();
                }
                break;

                case EventType.MouseDrag:
                //! 드래그 중이고 핫컨트롤이 내 것인 경우에만 높이 조절
                if (GUIUtility.hotControl == resizeControlID)
                {
                    height += e.delta.y;
                    height = Mathf.Clamp(height, minHeight, maxHeight);
                    e.Use();
                }
                break;

                case EventType.MouseUp:
                //! 마우스 업 시 컨트롤 해제
                if (GUIUtility.hotControl == resizeControlID)
                {
                    GUIUtility.hotControl = 0;
                    e.Use();
                }
                break;
            }
        }



        /// <summary>
        /// 높이 조절이 가능한 스크롤 뷰 (드래그 핸들 포함)
        /// </summary>
        /// <param name="scrollPos">스크롤 좌표</param>
        /// <param name="height">현재 스크롤 뷰의 높이 (ref)</param>
        /// <param name="minHeight">최소 높이</param>
        /// <param name="maxHeight">최대 높이</param>
        /// <param name="action">렌더링할 UI 액션</param>
        /// <param name="options">추가적인 GUILayout 옵션</param>
        public static void ScrollViewResizable(ref Vector2 scrollPos, ref float height, float minHeight, float maxHeight, Action action, params GUILayoutOption[] options)
        {
            //? 원하는 높이를 지정하기 위해 options에 Height 추가
            List<GUILayoutOption> layoutOptions = new(options ?? Array.Empty<GUILayoutOption>())
        {
            GUILayout.Height(height)
        };

            GUILayout.BeginVertical();

            //. 스크롤 뷰
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, layoutOptions.ToArray());
            action?.Invoke();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(3);

            //? 높이 조절 핸들 호출
            DrawResizeHandle(ref height, minHeight, maxHeight);

            GUILayout.Space(6);
            GUILayout.EndVertical();
        }



        /// <summary>
        /// 높이 조절이 가능한 컬렉션 스크롤 뷰 (드래그 핸들 포함)
        /// </summary>
        /// <typeparam name="T">컬렉션 요소 타입</typeparam>
        /// <param name="scrollPos">스크롤 좌표</param>
        /// <param name="height">현재 스크롤 뷰의 높이 (ref)</param>
        /// <param name="minHeight">최소 높이</param>
        /// <param name="maxHeight">최대 높이</param>
        /// <param name="collection">출력할 데이터 컬렉션</param>
        /// <param name="elementsAction">각 요소를 출력하는 액션</param>
        /// <param name="options">추가적인 GUILayout 옵션</param>
        public static void ScrollViewResizable_Collection<T>(ref Vector2 scrollPos, ref float height, float minHeight, float maxHeight, IEnumerable<T> collection, Action<T> elementsAction, params GUILayoutOption[] options)
        {
            //? 원하는 높이를 지정하기 위해 options에 Height를 추가
            List<GUILayoutOption> layoutOptions = new(options ?? Array.Empty<GUILayoutOption>())
        {
            GUILayout.Height(height)
        };


            GUILayout.BeginVertical();


            //. 스크롤 뷰
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, layoutOptions.ToArray());
            if (elementsAction != null)
            {
                foreach (var item in collection)
                {
                    elementsAction.Invoke(item);
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(3);


            //? 높이 조절 핸들 호출
            DrawResizeHandle(ref height, minHeight, maxHeight);


            GUILayout.Space(6);
            GUILayout.EndVertical();
        }



        /// <summary>
        /// 높이 조절이 가능한 컬렉션 스크롤 뷰 (검색 필터 포함, 드래그 핸들 포함)
        /// </summary>
        /// <typeparam name="T">컬렉션 요소 타입</typeparam>
        /// <param name="scrollPos">스크롤 좌표</param>
        /// <param name="height">현재 스크롤 뷰의 높이 (ref)</param>
        /// <param name="minHeight">최소 높이</param>
        /// <param name="maxHeight">최대 높이</param>
        /// <param name="searchQuery">검색 문자열 (ref)</param>
        /// <param name="collection">출력할 데이터 컬렉션</param>
        /// <param name="elementsAction">각 요소를 출력하는 액션</param>
        /// <param name="searchField">검색할 필드를 반환하는 함수</param>
        /// <param name="options">추가적인 GUILayout 옵션</param>
        public static void ScrollViewResizable_Collection<T>(ref Vector2 scrollPos, ref float height, float minHeight, float maxHeight, ref string searchQuery, out int searchCount, IEnumerable<T> collection, Action<T> elementsAction, Func<T, string> searchField, bool drawElementsInfo, params GUILayoutOption[] options)
        {
            //. (1) 검색 필터
            List<T> tempList = new List<T>(collection);
            for (int i = tempList.Count - 1; i >= 0; i--)
            {
                T item = tempList[i];
                if (!string.IsNullOrEmpty(searchQuery) && !searchField(item).Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) { tempList.RemoveAt(i); }
            }
            searchCount = tempList.Count;


            //. (2) 요소 카운트 라벨
            if (drawElementsInfo)
            {
                string text = (string.IsNullOrEmpty(searchQuery)) ?
                    $"Elements: <b><i><color=#2ecc71>{collection.Count()}</color></i></b>" :
                    $"Elements: <b><i><color=#4fc1e9>{tempList.Count}</color></i></b> <b>/</b> <b><i><color=#2ecc71>{collection.Count()}</color></i></b>";
                LabelField_TextAutoWidthHeight(text);
            }


            //. (3) 검색창 UI
            DrawSearchField(ref searchQuery);


            //. (4) 스크롤 뷰
            List<GUILayoutOption> layoutOptions = new(options ?? Array.Empty<GUILayoutOption>())
        {
            GUILayout.Height(height)
        };


            GUILayout.BeginVertical();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, layoutOptions.ToArray());
            foreach (var item in tempList)
            {
                elementsAction.Invoke(item);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(3);


            //. (5) 높이 조절 핸들 호출
            DrawResizeHandle(ref height, minHeight, maxHeight);


            GUILayout.Space(6);
            GUILayout.EndVertical();
        }



        //? 검색창



        /// <summary>
        /// EditorGUI.indentLevel이 높아도 검색창이 어긋나지 않도록 하는 메서드.
        /// 아이콘/텍스트필드/닫기버튼을 일관된 Rect와 스타일로 그려,
        /// 포커스 상태 변화에도 위치가 움직이지 않도록 합니다.
        /// </summary>
        /// <param name="searchText">검색어 (ref)</param>
        /// <param name="placeholder">입력 전 표시될 플레이스홀더 텍스트</param>
        /// <param name="showClearButton">true면, 검색어가 있을 시 오른쪽에 닫기 아이콘 표시</param>
        /// <param name="options">GUILayout 옵션 (예: ExpandWidth 등)</param>
        /// <returns>최종 검색어</returns>
        public static string DrawSearchField(ref string searchText, string placeholder = "Search", bool showClearButton = true, params GUILayoutOption[] options)
        {
            //. 한 줄짜리 Rect를 얻되, 라벨은 사용 안 하므로 false, 높이는 singleLineHeight로 맞춘다.
            Rect totalRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, options);

            //. indentLevel 적용: indentLevel이 2,3이면 그만큼 x가 오른쪽으로 이동
            totalRect = EditorGUI.IndentedRect(totalRect);

            //. 아이콘과 버튼의 폭(높이). singleLineHeight(약 18~20)으로 통일하면
            //. 포커스 전후에도 레이아웃이 동일하게 유지된다.
            float iconWidth = EditorGUIUtility.singleLineHeight;
            float closeWidth = 0f;
            if (showClearButton && !string.IsNullOrEmpty(searchText))
            {
                closeWidth = EditorGUIUtility.singleLineHeight;
            }

            //. 좌측 아이콘 Rect
            Rect iconRect = new Rect(totalRect.x, totalRect.y, iconWidth, totalRect.height);

            //. 텍스트 필드 Rect(아이콘 폭 + 닫기버튼 폭 제외)
            float spacing = 2f;
            float textFieldWidth = totalRect.width - iconWidth - closeWidth - spacing;
            Rect textFieldRect = new Rect(iconRect.xMax + spacing, totalRect.y, textFieldWidth, totalRect.height);

            //. 닫기 버튼 Rect
            Rect closeRect = new Rect(textFieldRect.xMax, totalRect.y, closeWidth, totalRect.height);


            // (A) 돋보기 아이콘
            GUIContent searchIcon = EditorGUIUtility.IconContent("d_Search Icon");
            GUI.Label(iconRect, searchIcon);

            // (B) 텍스트 필드 + 플레이스홀더
            //     *중요*: EditorStyles.textField 스타일로 통일 → 포커스 전후 위치가 달라지지 않음

            //. TextField에만 IndentLevel이 이중 적용되지 않도록, 일시적으로 0으로 세팅
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            searchText = EditorGUI.TextField(
                textFieldRect,
                GUIContent.none,
                searchText,
                EditorStyles.textField
            );

            EditorGUI.indentLevel = oldIndent; //? 원상복귀

            //. 플레이스홀더: 포커스가 없고 문자열이 비어있을 때만
            //. 텍스트필드 위치/스타일 그대로 회색 텍스트를 오버레이
            bool isFocused = (GUI.GetNameOfFocusedControl() == "SearchField");
            if (!isFocused && string.IsNullOrEmpty(searchText))
            {
                GUIStyle placeholderStyle = new GUIStyle(EditorStyles.textField)
                {
                    normal = { textColor = Color.gray },
                    alignment = TextAnchor.MiddleLeft
                };
                GUI.Label(textFieldRect, placeholder, placeholderStyle);
            }

            // (C) 닫기 버튼
            if (closeWidth > 0f) // (검색어가 있고 showClearButton=true일 때만)
            {
                GUIStyle clearBtnStyle = new GUIStyle(GUIStyle.none)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = EditorStyles.label.normal.textColor }
                };
                GUIContent closeIcon = EditorGUIUtility.IconContent("d_close");
                // or "d_tab_close"

                if (GUI.Button(closeRect, closeIcon, clearBtnStyle))
                {
                    searchText = string.Empty;
                    GUI.FocusControl(null);
                }
            }

            return searchText;
        }



        ///======================================================================================================================================================



        //? 오딘 인스펙터 활용 확장 메서드



        /// <summary>
        ///  오딘 인스펙터를 활용한, 드롭 다운 메뉴 이벤트
        /// </summary>
        /// <param name="dropDownMenuNames">그려질 드롭다운 목록의 이름이 담긴 배열</param>
        /// <param name="selectEvent">목록의 요소를 선택했을때, 실행되는 이벤트</param>
        /// <param name="selectionTitleName"></param>
        public static GenericSelector<string> DropDownMenuEvent(IEnumerable<string> dropDownMenuNames, Action<string> selectEvent, string selectionTitleName = "Select")
        {
            var odinSelector = new GenericSelector<string>(dropDownMenuNames);


            odinSelector.SetSelection(selectionTitleName);
            odinSelector.EnableSingleClickToSelect();

            odinSelector.SelectionConfirmed += selection =>
            {
                var selectedMenuName = selection.First();

                if (!string.IsNullOrEmpty(selectedMenuName))
                {
                    selectEvent?.Invoke(selectedMenuName);
                }
            };


            odinSelector.ShowInPopup(); // 팝업 형태로 표시


            return odinSelector;
        }



        ///======================================================================================================================================================



        //? 필드 렌더링 모음



        #region 일반 필드 렌더링 모음


        #region 필드 렌더링 : 오브젝트형 (Generic)

        /// <summary>
        /// 제네릭 UnityEngine.Object 타입 필드를 렌더링합니다.
        /// 변경을 감지하면 Undo 기록 후, <paramref name="field"/> 값을 갱신합니다.
        /// </summary>
        /// <typeparam name="T">UnityEngine.Object를 상속받는 타입</typeparam>
        /// <param name="editor">Undo 기록을 위한 에디터 인스턴스</param>
        /// <param name="field">렌더링 및 변경할 필드(참조)</param>
        /// <param name="label">필드 라벨 텍스트</param>
        /// <param name="tooltip">필드 라벨 툴팁</param>
        /// <returns>변경된 필드 값</returns>
        public static T RenderField_Object<T>(Editor editor, ref T field, string label = "", string tooltip = "", params GUILayoutOption[] options) where T : UnityEngine.Object
        {
            var guiContent = NewGUIContent_Label(label, tooltip);

            EditorGUI.BeginChangeCheck();
            var newField = EditorGUILayout.ObjectField(guiContent, field, typeof(T), true, options) as T;
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject(editor, label);
                field = newField;
            }
            return field;
        }

        /// <summary>
        /// 제네릭 UnityEngine.Object 타입 필드를 렌더링합니다.
        /// (Non-Ref 버전) 변경을 감지하면 Undo 기록 후, 새 필드 값을 반환합니다.
        /// </summary>
        /// <typeparam name="T">UnityEngine.Object를 상속받는 타입</typeparam>
        /// <param name="editor">Undo 기록을 위한 에디터 인스턴스</param>
        /// <param name="field">렌더링 및 변경할 필드(값)</param>
        /// <param name="label">필드 라벨 텍스트</param>
        /// <param name="tooltip">필드 라벨 툴팁</param>
        /// <returns>변경된 필드 값</returns>
        public static T RenderField_Object<T>(Editor editor, T field, string label = "", string tooltip = "", params GUILayoutOption[] options) where T : UnityEngine.Object
        {
            var guiContent = NewGUIContent_Label(label, tooltip);

            EditorGUI.BeginChangeCheck();
            var newField = EditorGUILayout.ObjectField(guiContent, field, typeof(T), true, options) as T;
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject(editor, label);
                field = newField;
            }
            return field;
        }

        #endregion



        #region 필드 렌더링 : String

        /// <summary>
        /// String 필드를 렌더링합니다. 변경이 감지되면 Undo 기록 후 <paramref name="field"/>를 갱신합니다.
        /// </summary>
        /// <param name="editor">Undo 기록을 위한 에디터 인스턴스</param>
        /// <param name="field">렌더링 및 변경할 문자열(참조)</param>
        /// <param name="guiContent">라벨/툴팁 표시에 사용할 GUIContent</param>
        /// <returns>변경된 문자열</returns>
        public static string RenderField_String(Editor editor, ref string field, GUIContent guiContent, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            var newField = EditorGUILayout.TextField(guiContent, field, options);
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject(editor, guiContent.text);
                field = newField;
            }
            return field;
        }

        /// <summary>
        /// String 필드를 렌더링합니다. 
        /// (오버로드: 라벨과 툴팁을 파라미터로 받음)
        /// </summary>
        /// <param name="editor">Undo 기록을 위한 에디터 인스턴스</param>
        /// <param name="field">렌더링 및 변경할 문자열(참조)</param>
        /// <param name="label">필드 라벨 텍스트</param>
        /// <param name="tooltip">필드 라벨 툴팁</param>
        /// <returns>변경된 문자열</returns>
        public static string RenderField_String(Editor editor, ref string field, string label = "", string tooltip = "", params GUILayoutOption[] options)
        {
            return RenderField_String(editor, ref field, NewGUIContent_Label(label, tooltip), options);
        }

        /// <summary>
        /// String 필드를 렌더링합니다. (Non-Ref 버전)
        /// 변경이 감지되면 Undo 기록 후, 새 문자열을 반환합니다.
        /// </summary>
        /// <param name="editor">Undo 기록을 위한 에디터 인스턴스</param>
        /// <param name="field">렌더링 및 변경할 문자열(값)</param>
        /// <param name="guiContent">라벨/툴팁 표시에 사용할 GUIContent</param>
        /// <returns>변경된 문자열</returns>
        public static string RenderField_String(Editor editor, string field, GUIContent guiContent, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            var newField = EditorGUILayout.TextField(guiContent, field, options);
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject(editor, guiContent.text);
                field = newField;
            }
            return field;
        }

        /// <summary>
        /// String 필드를 렌더링합니다. 
        /// (오버로드: 라벨과 툴팁을 파라미터로 받는 Non-Ref 버전)
        /// </summary>
        /// <param name="editor">Undo 기록을 위한 에디터 인스턴스</param>
        /// <param name="field">렌더링 및 변경할 문자열(값)</param>
        /// <param name="label">필드 라벨 텍스트</param>
        /// <param name="tooltip">필드 라벨 툴팁</param>
        /// <returns>변경된 문자열</returns>
        public static string RenderField_String(Editor editor, string field, string label = "", string tooltip = "", params GUILayoutOption[] options)
        {
            return RenderField_String(editor, field, NewGUIContent_Label(label, tooltip), options);
        }

        #endregion



        #region 필드 렌더링 : Int

        /// <summary>
        /// int 필드를 렌더링합니다. 변경이 감지되면 Undo 기록 후 <paramref name="field"/>를 갱신합니다.
        /// </summary>
        /// <param name="editor">Undo 기록을 위한 에디터 인스턴스</param>
        /// <param name="field">렌더링 및 변경할 정수(참조)</param>
        /// <param name="guiContent">라벨/툴팁 표시에 사용할 GUIContent</param>
        /// <returns>변경된 정수 값</returns>
        public static int RenderField_Int(Editor editor, ref int field, GUIContent guiContent, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            var newField = EditorGUILayout.IntField(guiContent, field, options);
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject(editor, guiContent.text);
                field = newField;
            }
            return field;
        }

        /// <summary>
        /// int 필드를 렌더링합니다. 
        /// (오버로드: 라벨과 툴팁을 파라미터로 받음)
        /// </summary>
        /// <param name="editor">Undo 기록을 위한 에디터 인스턴스</param>
        /// <param name="field">렌더링 및 변경할 정수(참조)</param>
        /// <param name="label">필드 라벨 텍스트</param>
        /// <param name="tooltip">필드 라벨 툴팁</param>
        /// <returns>변경된 정수 값</returns>
        public static int RenderField_Int(Editor editor, ref int field, string label = "", string tooltip = "", params GUILayoutOption[] options)
        {
            return RenderField_Int(editor, ref field, NewGUIContent_Label(label, tooltip), options);
        }

        /// <summary>
        /// int 필드를 렌더링합니다. (Non-Ref 버전)
        /// 변경이 감지되면 Undo 기록 후, 새 정수 값을 반환합니다.
        /// </summary>
        /// <param name="editor">Undo 기록을 위한 에디터 인스턴스</param>
        /// <param name="field">렌더링 및 변경할 정수(값)</param>
        /// <param name="guiContent">라벨/툴팁 표시에 사용할 GUIContent</param>
        /// <returns>변경된 정수 값</returns>
        public static int RenderField_Int(Editor editor, int field, GUIContent guiContent, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            var newField = EditorGUILayout.IntField(guiContent, field, options);
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject(editor, guiContent.text);
                field = newField;
            }
            return field;
        }

        /// <summary>
        /// int 필드를 렌더링합니다. 
        /// (오버로드: 라벨과 툴팁을 파라미터로 받는 Non-Ref 버전)
        /// </summary>
        /// <param name="editor">Undo 기록을 위한 에디터 인스턴스</param>
        /// <param name="field">렌더링 및 변경할 정수(값)</param>
        /// <param name="label">필드 라벨 텍스트</param>
        /// <param name="tooltip">필드 라벨 툴팁</param>
        /// <returns>변경된 정수 값</returns>
        public static int RenderField_Int(Editor editor, int field, string label = "", string tooltip = "", params GUILayoutOption[] options)
        {
            return RenderField_Int(editor, field, NewGUIContent_Label(label, tooltip), options);
        }

        #endregion



        #region 필드 렌더링 : Float

        /// <summary>
        /// float 필드를 렌더링합니다. 변경이 감지되면 Undo 기록 후 <paramref name="field"/>를 갱신합니다.
        /// </summary>
        /// <param name="editor">Undo 기록을 위한 에디터 인스턴스</param>
        /// <param name="field">렌더링 및 변경할 실수(참조)</param>
        /// <param name="guiContent">라벨/툴팁 표시에 사용할 GUIContent</param>
        /// <returns>변경된 float 값</returns>
        public static float RenderField_Float(Editor editor, ref float field, GUIContent guiContent, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            var newField = EditorGUILayout.FloatField(guiContent, field, options);
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject(editor, guiContent.text);
                field = newField;
            }
            return field;
        }

        /// <summary>
        /// float 필드를 렌더링합니다. 
        /// (오버로드: 라벨과 툴팁을 파라미터로 받음)
        /// </summary>
        /// <param name="editor">Undo 기록을 위한 에디터 인스턴스</param>
        /// <param name="field">렌더링 및 변경할 실수(참조)</param>
        /// <param name="label">필드 라벨 텍스트</param>
        /// <param name="tooltip">필드 라벨 툴팁</param>
        /// <returns>변경된 float 값</returns>
        public static float RenderField_Float(Editor editor, ref float field, string label = "", string tooltip = "", params GUILayoutOption[] options)
        {
            return RenderField_Float(editor, ref field, NewGUIContent_Label(label, tooltip), options);
        }

        /// <summary>
        /// float 필드를 렌더링합니다. (Non-Ref 버전)
        /// 변경이 감지되면 Undo 기록 후, 새 실수 값을 반환합니다.
        /// </summary>
        /// <param name="editor">Undo 기록을 위한 에디터 인스턴스</param>
        /// <param name="field">렌더링 및 변경할 실수(값)</param>
        /// <param name="guiContent">라벨/툴팁 표시에 사용할 GUIContent</param>
        /// <returns>변경된 float 값</returns>
        public static float RenderField_Float(Editor editor, float field, GUIContent guiContent, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            var newField = EditorGUILayout.FloatField(guiContent, field, options);
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject(editor, guiContent.text);
                field = newField;
            }
            return field;
        }

        /// <summary>
        /// float 필드를 렌더링합니다.
        /// (오버로드: 라벨과 툴팁을 파라미터로 받는 Non-Ref 버전)
        /// </summary>
        /// <param name="editor">Undo 기록을 위한 에디터 인스턴스</param>
        /// <param name="field">렌더링 및 변경할 실수(값)</param>
        /// <param name="label">필드 라벨 텍스트</param>
        /// <param name="tooltip">필드 라벨 툴팁</param>
        /// <returns>변경된 float 값</returns>
        public static float RenderField_Float(Editor editor, float field, string label = "", string tooltip = "", params GUILayoutOption[] options)
        {
            return RenderField_Float(editor, field, NewGUIContent_Label(label, tooltip), options);
        }

        #endregion



        #region 필드 렌더링 : Vector2, Vector2Int

        /// <summary>
        /// Vector2 필드를 렌더링합니다. 변경이 감지되면 Undo 기록 후 <paramref name="field"/>를 갱신합니다.
        /// </summary>
        /// <param name="editor">Undo 기록을 위한 에디터 인스턴스</param>
        /// <param name="field">렌더링 및 변경할 Vector2(참조)</param>
        /// <param name="guiContent">라벨/툴팁 표시에 사용할 GUIContent</param>
        /// <returns>변경된 Vector2 값</returns>
        public static Vector2 RenderField_Vector2(Editor editor, ref Vector2 field, GUIContent guiContent, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            var newField = EditorGUILayout.Vector2Field(guiContent, field, options);
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject(editor, guiContent.text);
                field = newField;
            }
            return field;
        }

        /// <summary>
        /// Vector2 필드를 렌더링합니다.
        /// (오버로드: 라벨과 툴팁을 파라미터로 받음)
        /// </summary>
        /// <param name="editor">Undo 기록을 위한 에디터 인스턴스</param>
        /// <param name="field">렌더링 및 변경할 Vector2(참조)</param>
        /// <param name="label">필드 라벨 텍스트</param>
        /// <param name="tooltip">필드 라벨 툴팁</param>
        /// <returns>변경된 Vector2 값</returns>
        public static Vector2 RenderField_Vector2(Editor editor, ref Vector2 field, string label = "", string tooltip = "", params GUILayoutOption[] options)
        {
            return RenderField_Vector2(editor, ref field, NewGUIContent_Label(label, tooltip), options);
        }

        /// <summary>
        /// Vector2Int 필드를 렌더링합니다. 변경이 감지되면 Undo 기록 후 <paramref name="field"/>를 갱신합니다.
        /// </summary>
        /// <param name="editor">Undo 기록을 위한 에디터 인스턴스</param>
        /// <param name="field">렌더링 및 변경할 Vector2Int(참조)</param>
        /// <param name="guiContent">라벨/툴팁 표시에 사용할 GUIContent</param>
        /// <returns>변경된 Vector2Int 값</returns>
        public static Vector2Int RenderField_Vector2Int(Editor editor, ref Vector2Int field, GUIContent guiContent, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            var newField = EditorGUILayout.Vector2IntField(guiContent, field, options);
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject(editor, guiContent.text);
                field = newField;
            }
            return field;
        }

        /// <summary>
        /// Vector2Int 필드를 렌더링합니다.
        /// (오버로드: 라벨과 툴팁을 파라미터로 받음)
        /// </summary>
        /// <param name="editor">Undo 기록을 위한 에디터 인스턴스</param>
        /// <param name="field">렌더링 및 변경할 Vector2Int(참조)</param>
        /// <param name="label">필드 라벨 텍스트</param>
        /// <param name="tooltip">필드 라벨 툴팁</param>
        /// <returns>변경된 Vector2Int 값</returns>
        public static Vector2Int RenderField_Vector2Int(Editor editor, ref Vector2Int field, string label = "", string tooltip = "", params GUILayoutOption[] options)
        {
            return RenderField_Vector2Int(editor, ref field, NewGUIContent_Label(label, tooltip), options);
        }

        /// <summary>
        /// Vector2 필드를 렌더링합니다. (Non-Ref 버전)
        /// 변경이 감지되면 Undo 기록 후, 새 Vector2 값을 반환합니다.
        /// </summary>
        public static Vector2 RenderField_Vector2(Editor editor, Vector2 field, GUIContent guiContent, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            var newField = EditorGUILayout.Vector2Field(guiContent, field, options);
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject(editor, guiContent.text);
                field = newField;
            }
            return field;
        }

        /// <summary>
        /// Vector2 필드를 렌더링합니다.
        /// (오버로드: 라벨, 툴팁을 파라미터로 받는 Non-Ref 버전)
        /// </summary>
        public static Vector2 RenderField_Vector2(Editor editor, Vector2 field, string label = "", string tooltip = "", params GUILayoutOption[] options)
        {
            return RenderField_Vector2(editor, field, NewGUIContent_Label(label, tooltip), options);
        }

        /// <summary>
        /// Vector2Int 필드를 렌더링합니다. (Non-Ref 버전)
        /// 변경이 감지되면 Undo 기록 후, 새 Vector2Int 값을 반환합니다.
        /// </summary>
        public static Vector2Int RenderField_Vector2Int(Editor editor, Vector2Int field, GUIContent guiContent, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            var newField = EditorGUILayout.Vector2IntField(guiContent, field, options);
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject(editor, guiContent.text);
                field = newField;
            }
            return field;
        }

        /// <summary>
        /// Vector2Int 필드를 렌더링합니다.
        /// (오버로드: 라벨, 툴팁을 파라미터로 받는 Non-Ref 버전)
        /// </summary>
        public static Vector2Int RenderField_Vector2Int(Editor editor, Vector2Int field, string label = "", string tooltip = "", params GUILayoutOption[] options)
        {
            return RenderField_Vector2Int(editor, field, NewGUIContent_Label(label, tooltip), options);
        }

        #endregion



        #region 필드 렌더링 : Vector3, Vector3Int

        /// <summary>
        /// Vector3 필드를 렌더링합니다. 변경이 감지되면 Undo 기록 후 <paramref name="field"/>를 갱신합니다.
        /// </summary>
        public static Vector3 RenderField_Vector3(Editor editor, ref Vector3 field, GUIContent guiContent, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            var newField = EditorGUILayout.Vector3Field(guiContent, field, options);
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject(editor, guiContent.text);
                field = newField;
            }
            return field;
        }

        /// <summary>
        /// Vector3 필드를 렌더링합니다.
        /// (오버로드: 라벨과 툴팁을 파라미터로 받음)
        /// </summary>
        public static Vector3 RenderField_Vector3(Editor editor, ref Vector3 field, string label = "", string tooltip = "", params GUILayoutOption[] options)
        {
            return RenderField_Vector3(editor, ref field, NewGUIContent_Label(label, tooltip), options);
        }

        /// <summary>
        /// Vector3Int 필드를 렌더링합니다. 변경이 감지되면 Undo 기록 후 <paramref name="field"/>를 갱신합니다.
        /// </summary>
        public static Vector3Int RenderField_Vector3Int(Editor editor, ref Vector3Int field, GUIContent guiContent, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            var newField = EditorGUILayout.Vector3IntField(guiContent, field, options);
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject(editor, guiContent.text);
                field = newField;
            }
            return field;
        }

        /// <summary>
        /// Vector3Int 필드를 렌더링합니다.
        /// (오버로드: 라벨과 툴팁을 파라미터로 받음)
        /// </summary>
        public static Vector3Int RenderField_Vector3Int(Editor editor, ref Vector3Int field, string label = "", string tooltip = "", params GUILayoutOption[] options)
        {
            return RenderField_Vector3Int(editor, ref field, NewGUIContent_Label(label, tooltip), options);
        }

        /// <summary>
        /// Vector3 필드를 렌더링합니다. (Non-Ref 버전)
        /// 변경이 감지되면 Undo 기록 후, 새 Vector3 값을 반환합니다.
        /// </summary>
        public static Vector3 RenderField_Vector3(Editor editor, Vector3 field, GUIContent guiContent, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            var newField = EditorGUILayout.Vector3Field(guiContent, field, options);
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject(editor, guiContent.text);
                field = newField;
            }
            return field;
        }

        /// <summary>
        /// Vector3 필드를 렌더링합니다.
        /// (오버로드: 라벨과 툴팁을 파라미터로 받는 Non-Ref 버전)
        /// </summary>
        public static Vector3 RenderField_Vector3(Editor editor, Vector3 field, string label = "", string tooltip = "", params GUILayoutOption[] options)
        {
            return RenderField_Vector3(editor, field, NewGUIContent_Label(label, tooltip), options);
        }

        /// <summary>
        /// Vector3Int 필드를 렌더링합니다. (Non-Ref 버전)
        /// 변경이 감지되면 Undo 기록 후, 새 Vector3Int 값을 반환합니다.
        /// </summary>
        public static Vector3Int RenderField_Vector3Int(Editor editor, Vector3Int field, GUIContent guiContent, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            var newField = EditorGUILayout.Vector3IntField(guiContent, field, options);
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject(editor, guiContent.text);
                field = newField;
            }
            return field;
        }

        /// <summary>
        /// Vector3Int 필드를 렌더링합니다.
        /// (오버로드: 라벨과 툴팁을 파라미터로 받는 Non-Ref 버전)
        /// </summary>
        public static Vector3Int RenderField_Vector3Int(Editor editor, Vector3Int field, string label = "", string tooltip = "", params GUILayoutOption[] options)
        {
            return RenderField_Vector3Int(editor, field, NewGUIContent_Label(label, tooltip), options);
        }

        #endregion



        #region 필드 렌더링 : Bool

        /// <summary>
        /// bool 필드를 렌더링합니다. 변경이 감지되면 Undo 기록 후 <paramref name="field"/>를 갱신합니다.
        /// </summary>
        public static bool RenderField_Bool(Editor editor, ref bool field, GUIContent guiContent, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            var newField = EditorGUILayout.Toggle(guiContent, field, options);
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject(editor, guiContent.text);
                field = newField;
            }
            return field;
        }

        /// <summary>
        /// bool 필드를 렌더링합니다.
        /// (오버로드: 라벨과 툴팁을 파라미터로 받음)
        /// </summary>
        public static bool RenderField_Bool(Editor editor, ref bool field, string label = "", string tooltip = "", params GUILayoutOption[] options)
        {
            return RenderField_Bool(editor, ref field, NewGUIContent_Label(label, tooltip), options);
        }

        /// <summary>
        /// bool 필드를 렌더링합니다. (Non-Ref 버전)
        /// 변경이 감지되면 Undo 기록 후, 새 bool 값을 반환합니다.
        /// </summary>
        public static bool RenderField_Bool(Editor editor, bool field, GUIContent guiContent, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            var newField = EditorGUILayout.Toggle(guiContent, field, options);
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject(editor, guiContent.text);
                field = newField;
            }
            return field;
        }

        /// <summary>
        /// bool 필드를 렌더링합니다.
        /// (오버로드: 라벨과 툴팁을 파라미터로 받는 Non-Ref 버전)
        /// </summary>
        public static bool RenderField_Bool(Editor editor, bool field, string label = "", string tooltip = "", params GUILayoutOption[] options)
        {
            return RenderField_Bool(editor, field, NewGUIContent_Label(label, tooltip), options);
        }

        #endregion



        #region 필드 렌더링 : Color

        /// <summary>
        /// Color 필드를 렌더링합니다. 변경이 감지되면 Undo 기록 후 <paramref name="field"/>를 갱신합니다.
        /// </summary>
        public static Color RenderField_Color(Editor editor, ref Color field, GUIContent guiContent, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            var newField = EditorGUILayout.ColorField(guiContent, field, options);
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject(editor, guiContent.text);
                field = newField;
            }
            return field;
        }

        /// <summary>
        /// Color 필드를 렌더링합니다.
        /// (오버로드: 라벨과 툴팁을 파라미터로 받음)
        /// </summary>
        public static Color RenderField_Color(Editor editor, ref Color field, string label = "", string tooltip = "", params GUILayoutOption[] options)
        {
            return RenderField_Color(editor, ref field, NewGUIContent_Label(label, tooltip), options);
        }

        /// <summary>
        /// Color 필드를 렌더링합니다. (Non-Ref 버전)
        /// 변경이 감지되면 Undo 기록 후, 새 Color 값을 반환합니다.
        /// </summary>
        public static Color RenderField_Color(Editor editor, Color field, GUIContent guiContent, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            var newField = EditorGUILayout.ColorField(guiContent, field, options);
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject(editor, guiContent.text);
                field = newField;
            }
            return field;
        }

        /// <summary>
        /// Color 필드를 렌더링합니다.
        /// (오버로드: 라벨과 툴팁을 파라미터로 받는 Non-Ref 버전)
        /// </summary>
        public static Color RenderField_Color(Editor editor, Color field, string label = "", string tooltip = "", params GUILayoutOption[] options)
        {
            return RenderField_Color(editor, field, NewGUIContent_Label(label, tooltip), options);
        }

        #endregion



        #region 필드 렌더링 : Material

        /// <summary>
        /// Material 필드를 렌더링합니다. 변경이 감지되면 Undo 기록 후 <paramref name="field"/>를 갱신합니다.
        /// </summary>
        public static Material RenderField_Material(Editor editor, ref Material field, GUIContent guiContent, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            var newField = (Material)EditorGUILayout.ObjectField(guiContent, field, typeof(Material), false, options);
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject(editor, guiContent.text);
                field = newField;
            }
            return field;
        }

        /// <summary>
        /// Material 필드를 렌더링합니다.
        /// (오버로드: 라벨과 툴팁을 파라미터로 받음)
        /// </summary>
        public static Material RenderField_Material(Editor editor, ref Material field, string label = "", string tooltip = "", params GUILayoutOption[] options)
        {
            return RenderField_Material(editor, ref field, NewGUIContent_Label(label, tooltip), options);
        }

        /// <summary>
        /// Material 필드를 렌더링합니다. (Non-Ref 버전)
        /// 변경이 감지되면 Undo 기록 후, 새 Material 값을 반환합니다.
        /// </summary>
        public static Material RenderField_Material(Editor editor, Material field, GUIContent guiContent, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            var newField = (Material)EditorGUILayout.ObjectField(guiContent, field, typeof(Material), false, options);
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject(editor, guiContent.text);
                field = newField;
            }
            return field;
        }

        /// <summary>
        /// Material 필드를 렌더링합니다.
        /// (오버로드: 라벨과 툴팁을 파라미터로 받는 Non-Ref 버전)
        /// </summary>
        public static Material RenderField_Material(Editor editor, Material field, string label = "", string tooltip = "", params GUILayoutOption[] options)
        {
            return RenderField_Material(editor, field, NewGUIContent_Label(label, tooltip), options);
        }

        #endregion



        #region 필드 렌더링 : Enum

        /// <summary>
        /// Enum 필드를 렌더링합니다. 변경이 감지되면 Undo 기록 후 <paramref name="field"/>를 갱신합니다.
        /// </summary>
        /// <typeparam name="T">Enum 타입</typeparam>
        /// <param name="editor">Undo 기록을 위한 에디터 인스턴스</param>
        /// <param name="field">렌더링 및 변경할 enum(참조)</param>
        /// <param name="guiContent">라벨/툴팁 표시에 사용할 GUIContent</param>
        /// <returns>변경된 enum 값</returns>
        public static T RenderField_Enum<T>(Editor editor, ref T field, GUIContent guiContent, params GUILayoutOption[] options) where T : Enum
        {
            EditorGUI.BeginChangeCheck();
            T newField = (T)EditorGUILayout.EnumPopup(guiContent, field, options);
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject(editor, guiContent.text);
                field = newField;
            }
            return field;
        }

        /// <summary>
        /// Enum 필드를 렌더링합니다.
        /// (오버로드: 라벨과 툴팁을 파라미터로 받음)
        /// </summary>
        /// <typeparam name="T">Enum 타입</typeparam>
        public static T RenderField_Enum<T>(Editor editor, ref T field, string label = "", string tooltip = "", params GUILayoutOption[] options) where T : Enum
        {
            return RenderField_Enum(editor, ref field, NewGUIContent_Label(label, tooltip), options);
        }

        /// <summary>
        /// Enum 필드를 렌더링합니다. (Non-Ref 버전)
        /// 변경이 감지되면 Undo 기록 후, 새 enum 값을 반환합니다.
        /// </summary>
        /// <typeparam name="T">Enum 타입</typeparam>
        public static T RenderField_Enum<T>(Editor editor, T field, GUIContent guiContent, params GUILayoutOption[] options) where T : Enum
        {
            EditorGUI.BeginChangeCheck();
            T newField = (T)EditorGUILayout.EnumPopup(guiContent, field, options);
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject(editor, guiContent.text);
                field = newField;
            }
            return field;
        }

        /// <summary>
        /// Enum 필드를 렌더링합니다.
        /// (오버로드: 라벨과 툴팁을 파라미터로 받는 Non-Ref 버전)
        /// </summary>
        /// <typeparam name="T">Enum 타입</typeparam>
        public static T RenderField_Enum<T>(Editor editor, T field, string label = "", string tooltip = "", params GUILayoutOption[] options) where T : Enum
        {
            return RenderField_Enum(editor, field, NewGUIContent_Label(label, tooltip), options);
        }

        #endregion



        ///<summary>
        ///라벨 필드: 텍스트 Area
        ///</summary>
        public static void Render_TextArea(string text, params GUILayoutOption[] options)
        {
            EditorGUILayout.TextArea(text, options);
        }


        #endregion



        ///======================================================================================================================================================



        //? 프로퍼티 필드 렌더링 모음



        #region 프로퍼티 필드 렌더링 모음



        private static class PropertyFieldHelper
        {
            private static readonly Dictionary<SerializedObject, Dictionary<string, SerializedProperty>> caching
                = new Dictionary<SerializedObject, Dictionary<string, SerializedProperty>>();

            private static readonly Dictionary<SerializedObject, Dictionary<string, InspectorProperty>> caching_InspectorOdin
                = new Dictionary<SerializedObject, Dictionary<string, InspectorProperty>>();

            /// <summary>
            /// 주어진 경로에 해당하는 <see cref="SerializedProperty"/>를 캐시에서 가져오거나,
            /// 캐시에 없다면 새로 탐색 후 캐싱하여 반환합니다.
            /// </summary>
            /// <param name="editor"><see cref="SerializedProperty"/>를 가져올 대상 <see cref="Editor"/> 인스턴스</param>
            /// <param name="propertyFieldPath">필드 경로(예: "someField" 또는 "someStruct.someSubField")</param>
            /// <returns>찾은 <see cref="SerializedProperty"/>, 찾지 못하면 <c>null</c></returns>
            public static SerializedProperty GetCachedSerilizedProperty(Editor editor, string propertyFieldPath)
            {
                SerializedProperty result;
                bool tryGet_SerializedObject = caching.TryGetValue(editor.serializedObject, out var dictionary);

                if (tryGet_SerializedObject && dictionary.TryGetValue(propertyFieldPath, out result))
                {
                    // 이미 캐시에 존재하는 경우
                }
                else
                {
                    // 캐시에 존재하지 않으면 새로 탐색 후 캐싱
                    if (!tryGet_SerializedObject)
                    {
                        caching.Add(editor.serializedObject, new Dictionary<string, SerializedProperty>());
                    }

                    try
                    {
                        result = GetSerializedProperty(editor, propertyFieldPath);
                        caching[editor.serializedObject].Add(propertyFieldPath, result);
                    }
                    catch
                    {
                        return null;
                    }
                }

                return result;
            }

            /// <summary>
            /// 주어진 경로에 해당하는 <see cref="SerializedProperty"/>를 찾습니다. 
            /// 내부적으로 Unity의 <see cref="SerializedObject.FindProperty(string)"/>를 사용하여 탐색하며,
            /// 경로가 잘못되었거나 직렬화되지 않은 필드라면 예외를 던질 수 있습니다.
            /// </summary>
            /// <param name="editor"><see cref="SerializedProperty"/>를 찾을 대상 <see cref="Editor"/> 인스턴스</param>
            /// <param name="propertyFieldPath">필드 경로(예: "someField" 또는 "someStruct.someSubField")</param>
            /// <returns>찾은 <see cref="SerializedProperty"/>, 찾지 못하면 <c>null</c></returns>
            private static SerializedProperty GetSerializedProperty(Editor editor, string propertyFieldPath)
            {
                SerializedProperty result = editor.serializedObject.FindProperty(propertyFieldPath);

                if (result == null)
                {
                    string errorMsg =
                        $"PropertyField를 찾을 수 없습니다! <b><i>{propertyFieldPath}</i></b> 경로가 정확한지, " +
                        $"또는 직렬화 속성이 맞는지(<i>private</i>인데 <i>[SerializeField]</i>가 누락되지 않았는지) 확인하세요.";

                    // 경로가 여러 단계일 가능성을 체크
                    if (propertyFieldPath.Contains('.'))
                    {
                        string[] pathParts = propertyFieldPath.Split('.');
                        string currentPath = "";
                        string lastSuccessfulPath = "";

                        try
                        {
                            for (int i = 0; i < pathParts.Length; i++)
                            {
                                currentPath = string.IsNullOrEmpty(currentPath)
                                    ? pathParts[i]
                                    : $"{currentPath}.{pathParts[i]}";

                                SerializedProperty property = editor.serializedObject.FindProperty(currentPath);

                                if (property == null)
                                {
                                    string successfulPath = string.IsNullOrEmpty(lastSuccessfulPath)
                                        ? "루트 객체"
                                        : lastSuccessfulPath;

                                    throw new Exception(
                                        $"{errorMsg}\n" +
                                        $"<b><i>{successfulPath}</i></b>까지 접근 성공, " +
                                        $"<b><i>{pathParts[i]}</i></b>부터 접근 불가\n전체 경로: <b><i>{propertyFieldPath}</i></b>");
                                }
                                else
                                {
                                    lastSuccessfulPath = currentPath;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                            return null;
                        }
                    }
                    else
                    {
                        try
                        {
                            throw new Exception(errorMsg);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                            return null;
                        }
                    }
                }

                return result;
            }

            /// <summary>
            /// 주어진 필드 경로와 마지막 필드 이름을 결합하여 <see cref="SerializedProperty"/>를 찾습니다.
            /// </summary>
            /// <param name="editor"><see cref="SerializedProperty"/>를 찾을 대상 <see cref="Editor"/> 인스턴스</param>
            /// <param name="fieldPath">최상위부터 마지막 속성 전까지의 경로(배열)</param>
            /// <param name="finalFieldName">최종으로 찾을 필드 이름</param>
            /// <returns>찾은 <see cref="SerializedProperty"/> 객체. 찾지 못하면 <c>null</c></returns>
            public static SerializedProperty GetSerializedProperty(Editor editor, string[] fieldPath, string finalFieldName)
            {
                string propertyFieldPath = (fieldPath == null || fieldPath.Length == 0)
                    ? finalFieldName
                    : $"{string.Join('.', fieldPath)}.{finalFieldName}";

                return GetCachedSerilizedProperty(editor, propertyFieldPath);
            }

            /// <summary>
            /// 주어진 경로에 해당하는 <see cref="InspectorProperty"/>를 캐시에서 가져오거나,
            /// 캐시에 없다면 새로 탐색 후 캐싱하여 반환합니다. (Odin Inspector 전용)
            /// </summary>
            /// <param name="editor"><see cref="InspectorProperty"/>를 가져올 대상 <see cref="Editor"/> 인스턴스</param>
            /// <param name="propertyTree">Odin Inspector의 <see cref="PropertyTree"/> 인스턴스</param>
            /// <param name="propertyFieldPath">필드 경로(예: "someField" 또는 "someStruct.someSubField")</param>
            /// <returns>찾은 <see cref="InspectorProperty"/>, 찾지 못하면 <c>null</c></returns>
            public static InspectorProperty GetCachedOdinSerilizedProperty(Editor editor, PropertyTree propertyTree, string propertyFieldPath)
            {
                InspectorProperty result;
                bool tryGet_SerializedObject = caching_InspectorOdin.TryGetValue(editor.serializedObject, out var dictionary);

                if (tryGet_SerializedObject && dictionary.TryGetValue(propertyFieldPath, out result))
                {
                    // 이미 캐시에 존재하는 경우
                }
                else
                {
                    // 캐시에 존재하지 않으면 새로 탐색 후 캐싱
                    if (!tryGet_SerializedObject)
                    {
                        caching_InspectorOdin.Add(editor.serializedObject, new Dictionary<string, InspectorProperty>());
                    }

                    try
                    {
                        result = GetSerializedOdinProperty(editor, propertyTree, propertyFieldPath);
                        caching_InspectorOdin[editor.serializedObject].Add(propertyFieldPath, result);
                    }
                    catch
                    {
                        return null;
                    }
                }

                return result;
            }

            /// <summary>
            /// 주어진 경로에 해당하는 <see cref="InspectorProperty"/>를 찾습니다. (Odin Inspector 전용)
            /// 내부적으로 <see cref="PropertyTree.GetPropertyAtUnityPath(string)"/>를 사용하여 탐색합니다.
            /// 경로가 잘못되었거나 직렬화되지 않은 필드라면 예외를 던질 수 있습니다.
            /// </summary>
            /// <param name="editor"><see cref="InspectorProperty"/>를 찾을 대상 <see cref="Editor"/> 인스턴스</param>
            /// <param name="propertyTree">Odin Inspector의 <see cref="PropertyTree"/> 인스턴스</param>
            /// <param name="propertyFieldPath">필드 경로(예: "someField" 또는 "someStruct.someSubField")</param>
            /// <returns>찾은 <see cref="InspectorProperty"/>, 찾지 못하면 <c>null</c></returns>
            private static InspectorProperty GetSerializedOdinProperty(Editor editor, PropertyTree propertyTree, string propertyFieldPath)
            {
                InspectorProperty result = propertyTree.GetPropertyAtUnityPath(propertyFieldPath);

                if (result == null)
                {
                    string errorMsg =
                        $"PropertyField를 찾을 수 없습니다! <b><i>{propertyFieldPath}</i></b> 경로가 정확한지, " +
                        $"또는 직렬화 속성이 맞는지(<i>private</i>인데 <i>[SerializeField]</i>가 누락되지 않았는지) 확인하세요.";

                    // 경로가 여러 단계일 가능성을 체크
                    if (propertyFieldPath.Contains('.'))
                    {
                        string[] pathParts = propertyFieldPath.Split('.');
                        string currentPath = "";
                        string lastSuccessfulPath = "";

                        try
                        {
                            for (int i = 0; i < pathParts.Length; i++)
                            {
                                currentPath = string.IsNullOrEmpty(currentPath)
                                    ? pathParts[i]
                                    : $"{currentPath}.{pathParts[i]}";

                                SerializedProperty property = editor.serializedObject.FindProperty(currentPath);

                                if (property == null)
                                {
                                    string successfulPath = string.IsNullOrEmpty(lastSuccessfulPath)
                                        ? "루트 객체"
                                        : lastSuccessfulPath;

                                    throw new Exception(
                                        $"{errorMsg}\n" +
                                        $"<b><i>{successfulPath}</i></b>까지 접근 성공, " +
                                        $"<b><i>{pathParts[i]}</i></b>부터 접근 불가\n전체 경로: <b><i>{propertyFieldPath}</i></b>");
                                }
                                else
                                {
                                    lastSuccessfulPath = currentPath;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                            return null;
                        }
                    }
                    else
                    {
                        try
                        {
                            throw new Exception(errorMsg);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                            return null;
                        }
                    }
                }

                return result;
            }

            /// <summary>
            /// 주어진 경로에 해당하는 <see cref="InspectorProperty"/>를 찾습니다. (Odin Inspector 전용)
            /// 가능한 모든 메서드를 최대한 동원해, null 반환을 최소화합니다.
            /// </summary>
            /// <param name="editor"><see cref="InspectorProperty"/>를 찾을 대상 <see cref="Editor"/> 인스턴스</param>
            /// <param name="propertyTree">Odin Inspector의 <see cref="PropertyTree"/> 인스턴스</param>
            /// <param name="propertyFieldPath">필드 경로(예: "someField" 또는 "someStruct.someSubField")</param>
            /// <returns>찾은 <see cref="InspectorProperty"/>, 찾지 못하면 <c>null</c></returns>
            [Obsolete]
            private static InspectorProperty _GetSerializedOdinProperty(Editor editor, PropertyTree propertyTree, string propertyFieldPath)
            {
                // 1) 가장 먼저, Odin 내부 'Path' 기반으로 탐색
                InspectorProperty odinProp = propertyTree.GetPropertyAtPath(propertyFieldPath);

                // 2) UnityPath 경로로 재시도
                if (odinProp == null)
                {
                    odinProp = propertyTree.GetPropertyAtUnityPath(propertyFieldPath);
                }

                // 4) PrefabModificationPath 이용
                if (odinProp == null)
                {
                    odinProp = propertyTree.GetPropertyAtPrefabModificationPath(propertyFieldPath);
                }


                // 6) 여기까지 시도 후 성공했다면 바로 반환
                if (odinProp != null)
                {
                    return odinProp;
                }


                //--- [여기서부터는 Odin이 아닌 Unity의 SerializedProperty를 통해 경로를 검증해보고, 다시 OdinProperty를 얻는 시도] ---
                string errorMsg =
                    $"PropertyField를 찾을 수 없습니다! <b><i>{propertyFieldPath}</i></b> 경로가 정확한지, " +
                    $"또는 직렬화 속성이 맞는지(<i>private</i>인데 <i>[SerializeField]</i>가 누락되지 않았는지) 확인하세요.";

                // 경로에 '.'이 포함되어 있으면, 다단계로 접근해보면서 하나라도 실패하면 예외 처리
                if (propertyFieldPath.Contains('.'))
                {
                    string[] pathParts = propertyFieldPath.Split('.');
                    string currentPath = "";
                    string lastSuccessfulPath = "";

                    try
                    {
                        for (int i = 0; i < pathParts.Length; i++)
                        {
                            currentPath = string.IsNullOrEmpty(currentPath)
                                ? pathParts[i]
                                : $"{currentPath}.{pathParts[i]}";

                            SerializedProperty sp = editor.serializedObject.FindProperty(currentPath);
                            if (sp == null)
                            {
                                string successfulPath = string.IsNullOrEmpty(lastSuccessfulPath)
                                    ? "루트 객체"
                                    : lastSuccessfulPath;

                                throw new System.Exception(
                                    $"{errorMsg}\n" +
                                    $"<b><i>{successfulPath}</i></b>까지 접근 성공, " +
                                    $"<b><i>{pathParts[i]}</i></b>부터 접근 불가\n전체 경로: <b><i>{propertyFieldPath}</i></b>");
                            }
                            else
                            {
                                lastSuccessfulPath = currentPath;
                            }
                        }

                        // 전체 경로를 SerializedProperty에서 찾았다면, 다시 OdinProperty 재확인
                        SerializedProperty foundSP = editor.serializedObject.FindProperty(propertyFieldPath);
                        if (foundSP != null)
                        {
                            // Unity 경로 기반으로 다시 OdinProperty 시도
                            odinProp = propertyTree.GetPropertyAtUnityPath(foundSP.propertyPath);
                            if (odinProp == null)
                            {
                                odinProp = propertyTree.GetPropertyAtPath(foundSP.propertyPath);
                            }
                            if (odinProp == null)
                            {
                                odinProp = propertyTree.GetPropertyAtPrefabModificationPath(foundSP.propertyPath);
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogException(ex);
                        return null;
                    }
                }
                else
                {
                    // '.'이 없는 경로는 그대로 SerializedProperty 시도
                    try
                    {
                        SerializedProperty sp = editor.serializedObject.FindProperty(propertyFieldPath);
                        if (sp != null)
                        {
                            // 다시 OdinProperty로 역매핑 시도
                            odinProp = propertyTree.GetPropertyAtUnityPath(sp.propertyPath);
                            if (odinProp == null)
                            {
                                odinProp = propertyTree.GetPropertyAtPath(sp.propertyPath);
                            }
                            if (odinProp == null)
                            {
                                odinProp = propertyTree.GetPropertyAtPrefabModificationPath(sp.propertyPath);
                            }
                        }
                        else
                        {
                            throw new System.Exception(errorMsg);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogException(ex);
                        return null;
                    }
                }

                // 모든 방법을 시도한 뒤에 찾아졌으면 odinProp이 null이 아님
                return odinProp;
            }

            /// <summary>
            /// 주어진 필드 경로와 마지막 필드 이름을 결합하여 <see cref="InspectorProperty"/>를 찾습니다. (Odin Inspector 전용)
            /// </summary>
            /// <param name="editor"><see cref="InspectorProperty"/>를 찾을 대상 <see cref="Editor"/> 인스턴스</param>
            /// <param name="propertyTree">Odin Inspector의 <see cref="PropertyTree"/> 인스턴스</param>
            /// <param name="fieldPath">최상위부터 마지막 속성 전까지의 경로(배열)</param>
            /// <param name="finalFieldName">최종으로 찾을 필드 이름</param>
            /// <returns>찾은 <see cref="InspectorProperty"/> 객체. 찾지 못하면 <c>null</c></returns>
            public static InspectorProperty GetSerializedOdinProperty(Editor editor, PropertyTree propertyTree, string[] fieldPath, string finalFieldName)
            {
                string propertyFieldPath = (fieldPath == null || fieldPath.Length == 0)
                    ? finalFieldName
                    : $"{string.Join('.', fieldPath)}.{finalFieldName}";

                return GetCachedOdinSerilizedProperty(editor, propertyTree, propertyFieldPath);
            }
        }



        //? SerializedProperty기반 



        /// <summary>
        /// <see cref="SerializedProperty"/>를 기반으로 Unity Editor GUI에 필드를 렌더링합니다.
        /// </summary>
        /// <param name="editor">렌더링 대상 <see cref="Editor"/> 인스턴스</param>
        /// <param name="serializedProperty">그릴 <see cref="SerializedProperty"/> 객체</param>
        /// <param name="guiContent">라벨/툴팁을 나타내는 <see cref="GUIContent"/> (없으면 <see cref="GUIContent.none"/>)</param>
        /// <returns>렌더링에 사용된 <see cref="SerializedProperty"/></returns>
        public static SerializedProperty RenderField_Property(Editor editor, SerializedProperty serializedProperty, GUIContent guiContent, params GUILayoutOption[] options)
        {
            if (serializedProperty == null)
            {
                Debug.LogError("SerializedProperty가 null입니다.");
                return null;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedProperty, guiContent, true, options);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(editor.target, $"변경됨: {editor.target.name} | {guiContent.text}");
                editor.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(editor.target);
            }

            return serializedProperty;
        }



        /// <summary>
        /// <see cref="SerializedProperty"/>를 기반으로 Unity Editor GUI에 필드를 렌더링합니다.
        /// </summary>
        /// <param name="label">필드 레이블 텍스트</param>
        /// <param name="tooltip">필드 툴팁 텍스트</param>

        public static SerializedProperty RenderField_Property(Editor editor, SerializedProperty serializedProperty, string label, string tooltip, params GUILayoutOption[] options) => RenderField_Property(editor, serializedProperty, NewGUIContent_Label(label, tooltip), options);



        /// <summary>
        /// <see cref="SerializedProperty"/>를 기반으로 Unity Editor GUI에 필드를 렌더링합니다.
        /// </summary>
        /// <param name="label">필드 레이블 텍스트</param>
        public static SerializedProperty RenderField_Property(Editor editor, SerializedProperty serializedProperty, string label, params GUILayoutOption[] options) => RenderField_Property(editor, serializedProperty, label, "", options);



        /// <summary>
        /// <see cref="SerializedProperty"/>를 기반으로 Unity Editor GUI에 필드를 렌더링합니다.
        /// (라벨/툴팁 없이 기본 GUIContent 사용)
        /// </summary>
        public static SerializedProperty RenderField_Property(Editor editor, SerializedProperty serializedProperty, params GUILayoutOption[] options) => RenderField_Property(editor, serializedProperty, "", "", options);



        //? 문자열 propertyFieldPath 기반 



        /// <summary>
        /// 필드 경로(<c>propertyFieldPath</c>)를 통해 <see cref="SerializedProperty"/>를 가져온 뒤,
        /// Unity Editor GUI에 필드를 렌더링합니다.
        /// </summary>
        /// <param name="propertyFieldPath">필드 경로(예: "someField" 또는 "someStruct.someSubField")</param>
        public static SerializedProperty RenderField_Property(Editor editor, string propertyFieldPath, GUIContent guiContent, params GUILayoutOption[] options)
        {
            var sp = PropertyFieldHelper.GetCachedSerilizedProperty(editor, propertyFieldPath);
            return RenderField_Property(editor, sp, guiContent, options);
        }



        /// <summary>
        /// 필드 경로(<c>propertyFieldPath</c>)를 통해 <see cref="SerializedProperty"/>를 가져온 뒤,
        /// Unity Editor GUI에 필드를 렌더링합니다.
        /// </summary>
        /// <param name="propertyFieldPath">필드 경로(예: "someField" 또는 "someStruct.someSubField")</param>
        public static SerializedProperty RenderField_Property(Editor editor, string propertyFieldPath, string label, string tooltip, params GUILayoutOption[] options) => RenderField_Property(editor, propertyFieldPath, NewGUIContent_Label(label, tooltip), options);



        /// <summary>
        /// 필드 경로(<c>propertyFieldPath</c>)를 통해 <see cref="SerializedProperty"/>를 가져온 뒤,
        /// Unity Editor GUI에 필드를 렌더링합니다.
        /// </summary>
        /// <param name="propertyFieldPath">필드 경로(예: "someField" 또는 "someStruct.someSubField")</param>
        public static SerializedProperty RenderField_Property(Editor editor, string propertyFieldPath, string label, params GUILayoutOption[] options) => RenderField_Property(editor, propertyFieldPath, label, "", options);



        /// <summary>
        /// 필드 경로(<c>propertyFieldPath</c>)를 통해 <see cref="SerializedProperty"/>를 가져온 뒤,
        /// Unity Editor GUI에 필드를 렌더링합니다.
        /// </summary>
        /// <param name="propertyFieldPath">필드 경로(예: "someField" 또는 "someStruct.someSubField")</param>
        public static SerializedProperty RenderField_Property(Editor editor, string propertyFieldPath, params GUILayoutOption[] options) => RenderField_Property(editor, propertyFieldPath, "", "", options);



        //? Path



        /// <summary>
        /// 배열 형태의 <paramref name="fieldPath"/>와 <paramref name="fieldName"/>을 결합하여 <see cref="SerializedProperty"/>를 찾은 뒤,
        /// Unity Editor GUI에 필드를 렌더링합니다.
        /// </summary>
        /// <param name="fieldPath">필드 경로를 구성하는 문자열 배열</param>
        /// <param name="fieldName">마지막 필드 이름</param>
        public static SerializedProperty RenderField_Property_Path(Editor editor, string[] fieldPath, string fieldName, string label, string tooltip, params GUILayoutOption[] options) => RenderField_Property(editor, PropertyFieldHelper.GetSerializedProperty(editor, fieldPath, fieldName), label, tooltip, options);



        /// <summary>
        /// 배열 형태의 <paramref name="fieldPath"/>와 <paramref name="fieldName"/>을 결합하여 <see cref="SerializedProperty"/>를 찾은 뒤,
        /// Unity Editor GUI에 필드를 렌더링합니다.
        /// </summary>
        /// <param name="fieldPath">필드 경로를 구성하는 문자열 배열</param>
        /// <param name="fieldName">마지막 필드 이름</param>
        public static SerializedProperty RenderField_Property_Path(Editor editor, string[] fieldPath, string fieldName, string label, params GUILayoutOption[] options) => RenderField_Property_Path(editor, fieldPath, fieldName, label, "", options);



        /// <summary>
        /// 배열 형태의 <paramref name="fieldPath"/>와 <paramref name="fieldName"/>을 결합하여 <see cref="SerializedProperty"/>를 찾은 뒤,
        /// Unity Editor GUI에 필드를 렌더링합니다.
        /// </summary>
        /// <param name="fieldPath">필드 경로를 구성하는 문자열 배열</param>
        /// <param name="fieldName">마지막 필드 이름</param>
        public static SerializedProperty RenderField_Property_Path(Editor editor, string[] fieldPath, string fieldName, params GUILayoutOption[] options) => RenderField_Property_Path(editor, fieldPath, fieldName, "", "", options);



        /// <summary>
        /// <paramref name="fieldPath"/> (params 배열)과 <paramref name="fieldName"/>을 결합하여 <see cref="SerializedProperty"/>를 찾은 뒤,
        /// Unity Editor GUI에 필드를 렌더링합니다.
        /// </summary>
        public static SerializedProperty RenderField_Property_Path(Editor editor, string fieldName, string label, string tooltip, params string[] fieldPath) => RenderField_Property(editor, PropertyFieldHelper.GetSerializedProperty(editor, fieldPath, fieldName), label, tooltip);



        /// <summary>
        /// <paramref name="fieldPath"/> (params 배열)과 <paramref name="fieldName"/>을 결합하여 <see cref="SerializedProperty"/>를 찾은 뒤,
        /// Unity Editor GUI에 필드를 렌더링합니다.
        /// </summary>
        public static SerializedProperty RenderField_Property_Path_Mini(Editor editor, string fieldName, params string[] fieldPath) => RenderField_Property_Path(editor, fieldName, "", "", fieldPath);



        ///======================================================================================================================================================



        //? Odin



        /// <summary>
        /// Odin Inspector가 사용하는 <see cref="InspectorProperty"/>를 기반으로 GUI 필드를 렌더링합니다.
        /// </summary>
        /// <param name="editor">렌더링 대상 <see cref="Editor"/> 인스턴스</param>
        /// <param name="propertyTree">Odin Inspector에서 사용하는 <see cref="PropertyTree"/></param>
        /// <param name="inspectorProperty">렌더링할 <see cref="InspectorProperty"/></param>
        /// <param name="guiContent">라벨/툴팁을 나타내는 <see cref="GUIContent"/> (없으면 <see cref="GUIContent.none"/>)</param>
        /// <returns>렌더링에 사용된 <see cref="InspectorProperty"/></returns>
        public static InspectorProperty RenderField_PropertyOdinInspector(Editor editor, PropertyTree propertyTree, InspectorProperty inspectorProperty, GUIContent guiContent)
        {
            if (inspectorProperty == null)
            {
                return null;
            }

            EditorGUI.BeginChangeCheck();
            propertyTree.BeginDraw(true);  // 리스트 추가/제거 등의 기능을 위해 BeginDraw/EndDraw 필요
            inspectorProperty.Draw(guiContent);
            propertyTree.EndDraw();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(editor.target, $"변경됨: {editor.target.name} | {inspectorProperty.Name}");
                editor.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(editor.target);
            }

            return inspectorProperty;
        }



        /// <summary>
        /// Odin Inspector가 사용하는 <see cref="InspectorProperty"/>를 기반으로 GUI 필드를 렌더링합니다.
        /// (라벨/툴팁 없이 기본 GUIContent 사용)
        /// </summary>
        public static InspectorProperty RenderField_PropertyOdinInspector(Editor editor, PropertyTree propertyTree, InspectorProperty inspectorProperty) => RenderField_PropertyOdinInspector(editor, propertyTree, inspectorProperty, GUIContent.none);




        /// <summary>
        /// Odin Inspector가 사용하는 <see cref="InspectorProperty"/>를 기반으로 GUI 필드를 렌더링합니다.
        /// </summary>
        /// <param name="label">필드 레이블 텍스트</param>
        /// <param name="tooltip">필드 툴팁 텍스트</param>
        public static InspectorProperty RenderField_PropertyOdinInspector(Editor editor, PropertyTree propertyTree, InspectorProperty inspectorProperty, string label, string tooltip) => RenderField_PropertyOdinInspector(editor, propertyTree, inspectorProperty, NewGUIContent_Label(label, tooltip));



        /// <summary>
        /// Odin Inspector가 사용하는 <see cref="InspectorProperty"/>를 기반으로 GUI 필드를 렌더링합니다.
        /// </summary>
        /// <param name="label">필드 레이블 텍스트</param>
        /// <param name="tooltip">필드 툴팁 텍스트</param>
        public static InspectorProperty RenderField_PropertyOdinInspector(Editor editor, PropertyTree propertyTree, InspectorProperty inspectorProperty, string label) => RenderField_PropertyOdinInspector(editor, propertyTree, inspectorProperty, label);



        /// <summary>
        /// 경로(<paramref name="propertyFieldPath"/>)를 통해 <see cref="InspectorProperty"/>를 찾은 뒤,
        /// Odin Inspector GUI 필드를 렌더링합니다.
        /// </summary>
        public static InspectorProperty RenderField_PropertyOdinInspector(Editor editor, PropertyTree propertyTree, string propertyFieldPath, string label, string tooltip)
        {
            var ip = PropertyFieldHelper.GetCachedOdinSerilizedProperty(editor, propertyTree, propertyFieldPath);
            return RenderField_PropertyOdinInspector(editor, propertyTree, ip, NewGUIContent_Label(label, tooltip));
        }



        /// <summary>
        /// 경로(<paramref name="propertyFieldPath"/>)를 통해 <see cref="InspectorProperty"/>를 찾은 뒤,
        /// Odin Inspector GUI 필드를 렌더링합니다.
        /// </summary>
        public static InspectorProperty RenderField_PropertyOdinInspector(Editor editor, PropertyTree propertyTree, string propertyFieldPath, string label) => RenderField_PropertyOdinInspector(editor, propertyTree, propertyFieldPath, label, "");



        /// <summary>
        /// 경로(<paramref name="propertyFieldPath"/>)를 통해 <see cref="InspectorProperty"/>를 찾은 뒤,
        /// Odin Inspector GUI 필드를 렌더링합니다.
        /// </summary>
        public static InspectorProperty RenderField_PropertyOdinInspector(Editor editor, PropertyTree propertyTree, string propertyFieldPath) => RenderField_PropertyOdinInspector(editor, propertyTree, propertyFieldPath, "", "");



        #region 오딘 PropertyTree 확장 에디터 제네릭 메서드



        /// <summary>
        /// Odin Inspector의 <see cref="IOdinPropertyTree"/>를 구현한 제네릭 에디터를 대상으로,
        /// <see cref="InspectorProperty"/> GUI 필드를 렌더링합니다.
        /// </summary>
        /// <typeparam name="TEditorOdinPropertyTree">Odin PropertyTree를 사용하는 에디터 타입</typeparam>
        /// <param name="editor">렌더링 대상 에디터 인스턴스</param>
        /// <param name="inspectorProperty">렌더링할 <see cref="InspectorProperty"/></param>
        /// <param name="guiContent">라벨/툴팁을 나타내는 <see cref="GUIContent"/></param>
        /// <returns>렌더링에 사용된 <see cref="InspectorProperty"/></returns>
        public static InspectorProperty RenderField_PropertyOdinInspector<TEditorOdinPropertyTree>(
            TEditorOdinPropertyTree editor,
            InspectorProperty inspectorProperty,
            GUIContent guiContent)
            where TEditorOdinPropertyTree : Editor, IOdinPropertyTree
        {
            if (inspectorProperty == null)
            {
                return null;
            }

            EditorGUI.BeginChangeCheck();

            editor.OdinPropertyTree.BeginDraw(true);

            if (guiContent.text == "" && guiContent.tooltip == "") inspectorProperty.Draw();
            else inspectorProperty.Draw(guiContent);

            editor.OdinPropertyTree.EndDraw();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(editor.target, $"변경됨: {editor.target.name} | {inspectorProperty.Name}");
                editor.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(editor.target);
            }

            return inspectorProperty;
        }



        /// <summary>
        /// Odin Inspector의 <see cref="IOdinPropertyTree"/>를 구현한 제네릭 에디터를 대상으로,
        /// <see cref="InspectorProperty"/> GUI 필드를 렌더링합니다.
        /// </summary>
        public static InspectorProperty RenderField_PropertyOdinInspector<TEditorOdinPropertyTree>(
            TEditorOdinPropertyTree editor,
            InspectorProperty inspectorProperty,
            string label,
            string tooltip)
            where TEditorOdinPropertyTree : Editor, IOdinPropertyTree => RenderField_PropertyOdinInspector(editor, inspectorProperty, NewGUIContent_Label(label, tooltip));



        /// <summary>
        /// Odin Inspector의 <see cref="IOdinPropertyTree"/>를 구현한 제네릭 에디터를 대상으로,
        /// <see cref="InspectorProperty"/> GUI 필드를 렌더링합니다.
        /// </summary>
        public static InspectorProperty RenderField_PropertyOdinInspector<TEditorOdinPropertyTree>(
            TEditorOdinPropertyTree editor,
            InspectorProperty inspectorProperty,
            string label)
            where TEditorOdinPropertyTree : Editor, IOdinPropertyTree => RenderField_PropertyOdinInspector(editor, inspectorProperty, label);



        /// <summary>
        /// Odin Inspector의 <see cref="IOdinPropertyTree"/>를 구현한 제네릭 에디터를 대상으로,
        /// <see cref="InspectorProperty"/> GUI 필드를 렌더링합니다.
        /// (라벨/툴팁 없이 기본 GUIContent 사용)
        /// </summary>
        public static InspectorProperty RenderField_PropertyOdinInspector<TEditorOdinPropertyTree>(
            TEditorOdinPropertyTree editor,
            InspectorProperty inspectorProperty)
            where TEditorOdinPropertyTree : Editor, IOdinPropertyTree => RenderField_PropertyOdinInspector(editor, inspectorProperty, "", "");



        //? propertyFieldPath



        /// <summary>
        /// 경로(<paramref name="propertyFieldPath"/>)를 통해 <see cref="InspectorProperty"/>를 찾은 뒤,
        /// Odin Inspector GUI 필드를 렌더링합니다.
        /// </summary>
        public static InspectorProperty RenderField_PropertyOdinInspector<TEditorOdinPropertyTree>(
            TEditorOdinPropertyTree editor,
            string propertyFieldPath,
            string label,
            string tooltip)
            where TEditorOdinPropertyTree : Editor, IOdinPropertyTree
        {
            var ip = PropertyFieldHelper.GetCachedOdinSerilizedProperty(editor, editor.OdinPropertyTree, propertyFieldPath);
            return RenderField_PropertyOdinInspector(editor, ip, NewGUIContent_Label(label, tooltip));
        }



        /// <summary>
        /// 경로(<paramref name="propertyFieldPath"/>)를 통해 <see cref="InspectorProperty"/>를 찾은 뒤,
        /// Odin Inspector GUI 필드를 렌더링합니다.
        /// </summary>
        public static InspectorProperty RenderField_PropertyOdinInspector<TEditorOdinPropertyTree>(
            TEditorOdinPropertyTree editor,
            string propertyFieldPath,
            string label)
            where TEditorOdinPropertyTree : Editor, IOdinPropertyTree => RenderField_PropertyOdinInspector(editor, propertyFieldPath, label, "");



        /// <summary>
        /// 경로(<paramref name="propertyFieldPath"/>)를 통해 <see cref="InspectorProperty"/>를 찾은 뒤,
        /// Odin Inspector GUI 필드를 렌더링합니다.
        /// </summary>
        public static InspectorProperty RenderField_PropertyOdinInspector<TEditorOdinPropertyTree>(
            TEditorOdinPropertyTree editor,
            string propertyFieldPath)
            where TEditorOdinPropertyTree : Editor, IOdinPropertyTree => RenderField_PropertyOdinInspector(editor, propertyFieldPath, "", "");




        #endregion



        //? Path



        /// <summary>
        /// 배열 형태의 <paramref name="fieldPath"/>와 <paramref name="fieldName"/>을 결합하여
        /// <see cref="InspectorProperty"/>를 찾은 뒤 Odin Inspector GUI 필드를 렌더링합니다.
        /// </summary>
        public static InspectorProperty RenderField_PropertyOdinInspector_Path(
            Editor editor,
            PropertyTree propertyTree,
            string[] fieldPath,
            string fieldName,
            string label,
            string tooltip)
        {
            return RenderField_PropertyOdinInspector(
                editor,
                propertyTree,
                PropertyFieldHelper.GetSerializedOdinProperty(editor, propertyTree, fieldPath, fieldName),
                label,
                tooltip
            );
        }



        /// <summary>
        /// 배열 형태의 <paramref name="fieldPath"/>와 <paramref name="fieldName"/>을 결합하여
        /// <see cref="InspectorProperty"/>를 찾은 뒤 Odin Inspector GUI 필드를 렌더링합니다.
        /// </summary>
        public static InspectorProperty RenderField_PropertyOdinInspector_Path(
            Editor editor,
            PropertyTree propertyTree,
            string[] fieldPath,
            string fieldName,
            string label) => RenderField_PropertyOdinInspector_Path(editor, propertyTree, fieldPath, fieldName, label, "");



        /// <summary>
        /// 배열 형태의 <paramref name="fieldPath"/>와 <paramref name="fieldName"/>을 결합하여
        /// <see cref="InspectorProperty"/>를 찾은 뒤 Odin Inspector GUI 필드를 렌더링합니다.
        /// </summary>
        public static InspectorProperty RenderField_PropertyOdinInspector_Path(
            Editor editor,
            PropertyTree propertyTree,
            string[] fieldPath,
            string fieldName) => RenderField_PropertyOdinInspector_Path(editor, propertyTree, fieldPath, fieldName, "", "");



        /// <summary>
        /// <paramref name="fieldPath"/> (params 배열)과 <paramref name="fieldName"/>을 결합하여
        /// <see cref="InspectorProperty"/>를 찾은 뒤 Odin Inspector GUI 필드를 렌더링합니다. (툴팁 포함)
        /// </summary>
        public static InspectorProperty RenderField_PropertyOdinInspector_Path_Tooltip(
            Editor editor,
            PropertyTree propertyTree,
            string fieldName,
            string label,
            string tooltip,
            params string[] fieldPath)
        {
            return RenderField_PropertyOdinInspector(
                editor,
                propertyTree,
                PropertyFieldHelper.GetSerializedOdinProperty(editor, propertyTree, fieldPath, fieldName),
                label,
                tooltip
            );
        }



        /// <summary>
        /// <paramref name="fieldPath"/> (params 배열)과 <paramref name="fieldName"/>을 결합하여
        /// <see cref="InspectorProperty"/>를 찾은 뒤 Odin Inspector GUI 필드를 렌더링합니다.
        /// </summary>
        public static InspectorProperty RenderField_PropertyOdinInspector_Path(
            Editor editor,
            PropertyTree propertyTree,
            string fieldName,
            string label,
            params string[] fieldPath) => RenderField_PropertyOdinInspector_Path_Tooltip(editor, propertyTree, fieldName, label, "", fieldPath);



        /// <summary>
        /// <paramref name="fieldPath"/> (params 배열)과 <paramref name="fieldName"/>을 결합하여
        /// <see cref="InspectorProperty"/>를 찾은 뒤 Odin Inspector GUI 필드를 렌더링합니다.
        /// </summary>
        public static InspectorProperty RenderField_PropertyOdinInspector_Path(
            Editor editor,
            PropertyTree propertyTree,
            string fieldName,
            params string[] fieldPath) => RenderField_PropertyOdinInspector_Path_Tooltip(editor, propertyTree, fieldName, "", "", fieldPath);



        #region 오딘 PropertyTree 확장 에디터 제네릭 메서드



        /// <summary>
        /// 배열 형태의 <paramref name="fieldPath"/>와 <paramref name="fieldName"/>을 결합하여
        /// <see cref="InspectorProperty"/>를 찾은 뒤 Odin Inspector GUI 필드를 렌더링합니다.
        /// </summary>
        public static InspectorProperty RenderField_PropertyOdinInspector_Path<TEditorOdinPropertyTree>(
            TEditorOdinPropertyTree editor,
            string[] fieldPath,
            string fieldName,
            string label,
            string tooltip)
            where TEditorOdinPropertyTree : Editor, IOdinPropertyTree
        {
            return RenderField_PropertyOdinInspector(
                editor,
                PropertyFieldHelper.GetSerializedOdinProperty(editor, editor.OdinPropertyTree, fieldPath, fieldName),
                label,
                tooltip
            );
        }



        /// <summary>
        /// 배열 형태의 <paramref name="fieldPath"/>와 <paramref name="fieldName"/>을 결합하여
        /// <see cref="InspectorProperty"/>를 찾은 뒤 Odin Inspector GUI 필드를 렌더링합니다.
        /// </summary>
        public static InspectorProperty RenderField_PropertyOdinInspector_Path<TEditorOdinPropertyTree>(
            TEditorOdinPropertyTree editor,
            string[] fieldPath,
            string fieldName,
            string label)
            where TEditorOdinPropertyTree : Editor, IOdinPropertyTree => RenderField_PropertyOdinInspector_Path(editor, fieldPath, fieldName, label, "");



        /// <summary>
        /// 배열 형태의 <paramref name="fieldPath"/>와 <paramref name="fieldName"/>을 결합하여
        /// <see cref="InspectorProperty"/>를 찾은 뒤 Odin Inspector GUI 필드를 렌더링합니다.
        /// </summary>
        public static InspectorProperty RenderField_PropertyOdinInspector_Path<TEditorOdinPropertyTree>(
            TEditorOdinPropertyTree editor,
            string[] fieldPath,
            string fieldName)
            where TEditorOdinPropertyTree : Editor, IOdinPropertyTree => RenderField_PropertyOdinInspector_Path(editor, fieldPath, fieldName, "", "");



        /// <summary>
        /// <paramref name="fieldPath"/> (params 배열)과 <paramref name="fieldName"/>을 결합하여
        /// <see cref="InspectorProperty"/>를 찾은 뒤 Odin Inspector GUI 필드를 렌더링합니다. (툴팁 포함)
        /// </summary>
        public static InspectorProperty RenderField_PropertyOdinInspector_Path<TEditorOdinPropertyTree>(
            TEditorOdinPropertyTree editor,
            string fieldName,
            string label,
            string tooltip,
            params string[] fieldPath)
            where TEditorOdinPropertyTree : Editor, IOdinPropertyTree
        {
            return RenderField_PropertyOdinInspector(editor, PropertyFieldHelper.GetSerializedOdinProperty(editor, editor.OdinPropertyTree, fieldPath, fieldName), label, tooltip);
        }



        /// <summary>
        /// <paramref name="fieldPath"/> (params 배열)과 <paramref name="fieldName"/>을 결합하여
        /// <see cref="InspectorProperty"/>를 찾은 뒤 Odin Inspector GUI 필드를 렌더링합니다.
        /// </summary>
        public static InspectorProperty RenderField_PropertyOdinInspector_Path<TEditorOdinPropertyTree>(
            TEditorOdinPropertyTree editor,
            string fieldName,
            string label,
            params string[] fieldPath)
            where TEditorOdinPropertyTree : Editor, IOdinPropertyTree => RenderField_PropertyOdinInspector_Path(editor, fieldName, label, "", fieldPath);



        /// <summary>
        /// <paramref name="fieldPath"/> (params 배열)과 <paramref name="fieldName"/>을 결합하여
        /// <see cref="InspectorProperty"/>를 찾은 뒤 Odin Inspector GUI 필드를 렌더링합니다.
        /// </summary>
        public static InspectorProperty RenderField_PropertyOdinInspector_Path<TEditorOdinPropertyTree>(
            TEditorOdinPropertyTree editor,
            string fieldName,
            params string[] fieldPath)
            where TEditorOdinPropertyTree : Editor, IOdinPropertyTree => RenderField_PropertyOdinInspector_Path(editor, fieldName, "", "", fieldPath);



        #endregion



        ///======================================================================================================================================================



        #region RenderField_PropertyEX (사용 자제)

        /// <summary>
        /// <see cref="Expression"/>을 활용해 특정 프로퍼티/필드의 <see cref="SerializedProperty"/>를 자동으로 추출하고,
        /// Unity Editor GUI에 필드를 렌더링합니다. (사용 자제)
        /// </summary>
        /// <typeparam name="T">프로퍼티/필드의 타입</typeparam>
        /// <param name="editor">렌더링 대상 <see cref="Editor"/> 인스턴스</param>
        /// <param name="property">프로퍼티를 나타내는 람다 표현식(예: <c>() => target.someField</c>)</param>
        /// <param name="usePath">true면 전체 경로를 사용, false면 마지막 속성 이름만 사용</param>
        /// <param name="convertFirstLetterToLower">
        /// true면 추출한 경로/이름의 첫 알파벳을 소문자로 변환하여 적용
        /// </param>
        /// <param name="label">필드 레이블</param>
        /// <param name="tooltip">필드 툴팁</param>
        [Obsolete("사용 자제")]
        public static SerializedProperty RenderField_PropertyEX<T>(
            Editor editor,
            Expression<Func<T>> property,
            bool usePath,
            bool convertFirstLetterToLower,
            string label = "",
            string tooltip = "")
        {
            string propertyName = GetPropertyEX_PathName(property, usePath);

            if (convertFirstLetterToLower)
            {
                propertyName = SU_String.LowerFirst(propertyName); // SU_String은 사용처에 맞게 구현
            }

            var sp = editor.serializedObject.FindProperty(propertyName);
            return RenderField_Property(editor, sp, NewGUIContent_Label(label, tooltip));
        }

        /// <summary>
        /// <see cref="Expression"/>을 활용해 특정 프로퍼티/필드의 <see cref="SerializedProperty"/> 경로를 추출하고,
        /// Unity Editor GUI에 필드를 렌더링합니다. (경로 사용, 사용 자제)
        /// </summary>
        [Obsolete("사용 자제")]
        public static SerializedProperty RenderField_PropertyEX_Path<T>(
            Editor editor,
            Expression<Func<T>> property,
            bool convertFirstLetterToLower,
            string label = "",
            string tooltip = "")
        {
            return RenderField_PropertyEX(editor, property, true, convertFirstLetterToLower, label, tooltip);
        }

        /// <summary>
        /// <see cref="Expression"/>을 활용해 특정 프로퍼티/필드의 <see cref="SerializedProperty"/> 경로를 추출하고,
        /// 경로의 첫 글자를 소문자로 변환 후 Unity Editor GUI에 필드를 렌더링합니다. (사용 자제)
        /// </summary>
        [Obsolete("사용 자제")]
        public static SerializedProperty RenderField_PropertyEX_PathFLF<T>(
            Editor editor,
            Expression<Func<T>> property,
            string label = "",
            string tooltip = "")
        {
            return RenderField_PropertyEX(editor, property, true, true, label, tooltip);
        }

        /// <summary>
        /// 람다 표현식에서 프로퍼티/필드의 경로를 추출합니다.
        /// </summary>
        /// <typeparam name="T">프로퍼티/필드의 타입</typeparam>
        /// <param name="property">프로퍼티를 지정하는 람다 표현식 (예: <c>() => target.someField</c>)</param>
        /// <param name="fullPath">true면 전체 경로, false면 마지막 필드/프로퍼티 이름만 반환</param>
        /// <returns>경로 문자열(예: "someStruct.someSubField") 또는 마지막 필드 이름(예: "someSubField")</returns>
        /// <exception cref="ArgumentException">유효한 멤버 액세스 표현식이 아닐 경우</exception>
        [Obsolete("사용 자제")]
        public static string GetPropertyEX_PathName<T>(Expression<Func<T>> property, bool fullPath)
        {
            Expression body = property.Body;
            while (body is UnaryExpression unaryExpr && unaryExpr.NodeType == ExpressionType.Convert)
            {
                body = unaryExpr.Operand;
            }

            var memberNames = new List<string>();
            var memberExpr = body as MemberExpression;

            while (memberExpr != null)
            {
                memberNames.Add(memberExpr.Member.Name);
                memberExpr = memberExpr.Expression as MemberExpression;
            }

            if (memberNames.Count == 0)
            {
                throw new ArgumentException("유효한 멤버 액세스 표현식이어야 합니다.", nameof(property));
            }

            return fullPath
                ? string.Join(".", memberNames.AsEnumerable().Reverse())
                : memberNames[0];
        }

        #endregion



        #endregion



        ///======================================================================================================================================================



        //? 라벨필드



        #region 라벨 필드



        //? 헤드 라벨필드



        ///<summary>라벨 필드: 헤더</summary>
        public static void LabelField_Head(string header, LabelHeadType headType, Color? color, bool endSpace = true)
        {
            GUIStyle currentGUIStyle;


            switch (headType)
            {
                case LabelHeadType.H1:
                currentGUIStyle = new GUIStyle(LabelStyle_Head1);
                break;

                case LabelHeadType.H2:
                currentGUIStyle = new GUIStyle(LabelStyle_Head2);
                break;

                case LabelHeadType.H3:
                currentGUIStyle = new GUIStyle(LabelStyle_Head3);
                break;
                default: currentGUIStyle = null; break;
            }


            if (color.HasValue)
            {
                currentGUIStyle.normal.textColor = color.Value;
            }


            EditorGUILayout.LabelField(header, currentGUIStyle);


            if (endSpace) { EditorGUILayout.Space(); }
        }



        ///<summary>라벨 필드: 헤더</summary>
        public static void LabelField_Head(string header, GUIStyle guiStyle, Color? color, bool endSpace = true)
        {
            GUIStyle currentStyle = new GUIStyle(guiStyle);


            if (color.HasValue)
            {
                currentStyle.normal.textColor = color.Value;
            }


            EditorGUILayout.LabelField(header, currentStyle);


            if (endSpace) { EditorGUILayout.Space(); }
        }



        ///======================================================================================================================================================



        //? 헤드 자동쓰기



        /// <summary>
        /// 헤더 자동쓰기
        /// </summary>
        /// <param name="header">헤더 텍스트</param>
        /// <param name="headType">헤더 타입</param>
        /// <param name="action">헤더 안에서 실행할 이벤트</param>
        /// <param name="endHorizontalLine">끝부분에 구분선 긋기</param>
        public static void AutoLabelField_Head(string header, LabelHeadType headType, Action action, bool endHorizontalLine = true, bool useIndentLevel = true, bool endHeadSpace = true)
        {
            LabelField_Head(header, headType, null, endHeadSpace);
            if (useIndentLevel) { Format_IndentLevel_Plus(); }
            action?.Invoke();
            if (useIndentLevel) { Format_IndentLevel_Minus(); }
            if (endHorizontalLine) { Format_HorizontalLine(); } else { EditorGUILayout.Space(); }
        }



        /// <summary>
        /// 헤더 자동쓰기
        /// </summary>
        /// <param name="header">헤더 텍스트</param>
        /// <param name="guiStyle">헤더 스타일</param>
        /// <param name="action">헤더 안에서 실행할 이벤트</param>
        /// <param name="endHorizontalLine">끝부분에 구분선 긋기</param>
        public static void AutoLabelField_Head(string header, GUIStyle guiStyle, Action action, bool endHorizontalLine = true, bool useIndentLevel = true, bool endHeadSpace = true)
        {
            LabelField_Head(header, guiStyle, null, endHeadSpace);
            if (useIndentLevel) { Format_IndentLevel_Plus(); }
            action?.Invoke();
            if (useIndentLevel) { Format_IndentLevel_Minus(); }
            if (endHorizontalLine) { Format_HorizontalLine(); } else { EditorGUILayout.Space(); }
        }



        /// <summary>
        /// 헤더 자동쓰기
        /// </summary>
        /// <param name="header">헤더 텍스트</param>
        /// <param name="headType">헤더 타입</param>
        /// <param name="action">헤더 안에서 실행할 이벤트</param>
        /// <param name="endHorizontalLine">끝부분에 구분선 긋기</param>
        public static void AutoLabelField_Head(string header, LabelHeadType headType, Color color, Action action, bool endHorizontalLine = true, bool useIndentLevel = true, bool endHeadSpace = true)
        {
            LabelField_Head(header, headType, color, endHeadSpace);
            if (useIndentLevel) { Format_IndentLevel_Plus(); }
            action?.Invoke();
            if (useIndentLevel) { Format_IndentLevel_Minus(); }
            if (endHorizontalLine) { Format_HorizontalLine(); } else { EditorGUILayout.Space(); }
        }



        /// <summary>
        /// 헤더 자동쓰기
        /// </summary>
        /// <param name="header">헤더 텍스트</param>
        /// <param name="guiStyle">헤더 스타일</param>
        /// <param name="action">헤더 안에서 실행할 이벤트</param>
        /// <param name="endHorizontalLine">끝부분에 구분선 긋기</param>
        public static void AutoLabelField_Head(string header, GUIStyle guiStyle, Color color, Action action, bool endHorizontalLine = true, bool useIndentLevel = true, bool endHeadSpace = true)
        {
            LabelField_Head(header, guiStyle, null, endHeadSpace);
            if (useIndentLevel) { Format_IndentLevel_Plus(); }
            action?.Invoke();
            if (useIndentLevel) { Format_IndentLevel_Minus(); }
            if (endHorizontalLine) { Format_HorizontalLine(); } else { EditorGUILayout.Space(); }
        }



        ///======================================================================================================================================================



        //? 텍스트 라벨필드



        ///<summary>
        ///라벨 필드: 텍스트
        ///</summary>
        public static void LabelField_Text(string text, GUIStyle style, params GUILayoutOption[] options)
        {
            EditorGUILayout.LabelField(text, style, options);
        }



        ///<summary>
        ///라벨 필드: 텍스트
        ///</summary>
        public static void LabelField_Text(string text, params GUILayoutOption[] options)
        {
            LabelField_Text(text, NewGUIStyle_Label(), options);
        }



        ///<summary>
        ///라벨 필드: 텍스트
        ///</summary>
        public static void LabelField_Text(string text, Color textColor, params GUILayoutOption[] options)
        {
            LabelField_Text(text, NewGUIStyle(textColor), options);
        }



        ///<summary>
        ///라벨 필드: 텍스트
        ///</summary>
        public static void LabelField_Text(string text, FontStyle fontStyle, params GUILayoutOption[] options)
        {
            LabelField_Text(text, NewGUIStyle(fontStyle), options);
        }



        ///<summary>
        ///라벨 필드: 텍스트
        ///</summary>
        public static void LabelField_Text(string text, Color textColor, FontStyle fontStyle, params GUILayoutOption[] options)
        {
            LabelField_Text(text, NewGUIStyle(textColor, fontStyle), options);
        }



        //? 자동 크기 조정 라벨 필드



        ///<summary>
        /// 라벨 필드: 텍스트 (자동 폭 및 높이 조정)
        /// 공통 로직을 처리하는 내부 함수
        /// HTML 태그를 제거한 순수 텍스트를 기준으로 사이즈를 계산합니다.
        ///</summary>
        private static GUILayoutOption[] GetAdjustedOptions(string text, GUIStyle style, int correctionWidth, GUILayoutOption[] existingOptions)
        {
            // HTML 태그 제거 (예: <b>, <i> 등)
            string textWithoutTags = Regex.Replace(text, "<.*?>", "");
            GUIContent content = new GUIContent(textWithoutTags);

            // 현재 인덴트로 인해 좌측에 추가로 들어가는 픽셀 오프셋
            float indentOffset = EditorGUI.indentLevel * 15f;


            // 높이 계산시, 인덴트로 인해 유효 폭이 줄어들 수 있으므로 이를 반영
            float availableWidth = EditorGUIUtility.currentViewWidth - indentOffset;
            float calculatedHeight = style.CalcHeight(content, availableWidth);

            GUILayoutOption[] finalOptions = new GUILayoutOption[existingOptions.Length + 1];
            existingOptions.CopyTo(finalOptions, 0);
            finalOptions[existingOptions.Length] = GUILayout.Height(calculatedHeight);

            return finalOptions;
        }



        #region IndentLevel를 고려안하던거

        ///<summary>
        /// 라벨 필드: 텍스트 (자동 폭 및 높이 조정)
        /// 공통 로직을 처리하는 내부 함수
        ///</summary>
        private static GUILayoutOption[] GetAdjustedOptions_Legacy(string text, GUIStyle style, int correctionWidth, GUILayoutOption[] existingOptions)
        {
            GUIContent content = new GUIContent(text);
            float calculatedWidth = GUI.skin.label.CalcSize(content).x + correctionWidth;
            float calculatedHeight = style.CalcHeight(content, EditorGUIUtility.currentViewWidth);

            GUILayoutOption[] finalOptions = new GUILayoutOption[existingOptions.Length + 2];
            existingOptions.CopyTo(finalOptions, 0);
            finalOptions[existingOptions.Length] = GUILayout.Width(calculatedWidth);
            finalOptions[existingOptions.Length + 1] = GUILayout.Height(calculatedHeight);

            return finalOptions;
        }

        #endregion



        #region HTML 태그 제거 안하던거

        ///<summary>
        /// 라벨 필드: 텍스트 (자동 폭 및 높이 조정)
        /// 공통 로직을 처리하는 내부 함수
        ///</summary>
        private static GUILayoutOption[] GetAdjustedOptions_Legacy2(string text, GUIStyle style, int correctionWidth, GUILayoutOption[] existingOptions)
        {
            GUIContent content = new GUIContent(text);

            //. 현재 인덴트로 인해 좌측에 추가로 들어가는 픽셀 오프셋
            float indentOffset = EditorGUI.indentLevel * 5f;

            //. 기본 라벨 너비 + 보정값 + 인덴트 오프셋
            float labelWidth = GUI.skin.label.CalcSize(content).x;
            float calculatedWidth = labelWidth + correctionWidth + indentOffset;

            //. 높이 계산시, 인덴트로 인해 유효 폭이 줄어들 수 있으므로 이를 반영
            float availableWidth = EditorGUIUtility.currentViewWidth - indentOffset;
            float calculatedHeight = style.CalcHeight(content, availableWidth);

            GUILayoutOption[] finalOptions = new GUILayoutOption[existingOptions.Length + 2];
            existingOptions.CopyTo(finalOptions, 0);
            finalOptions[existingOptions.Length] = GUILayout.Width(calculatedWidth);
            finalOptions[existingOptions.Length + 1] = GUILayout.Height(calculatedHeight);

            return finalOptions;
        }

        #endregion

        #region 너비 길이 계산하던거 (할필요없는걸로 판명)



        ///<summary>
        /// 라벨 필드: 텍스트 (자동 폭 및 높이 조정)
        /// 공통 로직을 처리하는 내부 함수
        /// HTML 태그를 제거한 순수 텍스트를 기준으로 사이즈를 계산합니다.
        ///</summary>
        private static GUILayoutOption[] GetAdjustedOptions_Legacy3(string text, GUIStyle style, int correctionWidth, GUILayoutOption[] existingOptions)
        {
            // HTML 태그 제거 (예: <b>, <i> 등)
            string textWithoutTags = Regex.Replace(text, "<.*?>", "");
            GUIContent content = new GUIContent(textWithoutTags);

            // 현재 인덴트로 인해 좌측에 추가로 들어가는 픽셀 오프셋
            float indentOffset = EditorGUI.indentLevel * 15f;

            // 기본 라벨 너비 + 보정값 + 인덴트 오프셋 (HTML 태그를 제외한 텍스트의 크기를 기준으로 계산)
            float labelWidth = GUI.skin.label.CalcSize(content).x;
            float calculatedWidth = labelWidth + correctionWidth + indentOffset;

            // 높이 계산시, 인덴트로 인해 유효 폭이 줄어들 수 있으므로 이를 반영
            float availableWidth = EditorGUIUtility.currentViewWidth - indentOffset;
            float calculatedHeight = style.CalcHeight(content, availableWidth);

            GUILayoutOption[] finalOptions = new GUILayoutOption[existingOptions.Length + 2];
            existingOptions.CopyTo(finalOptions, 0);
            finalOptions[existingOptions.Length] = GUILayout.Width(calculatedWidth);
            finalOptions[existingOptions.Length + 1] = GUILayout.Height(calculatedHeight);

            return finalOptions;
        }



        #endregion


        ///<summary>
        ///라벨 필드: 텍스트 (자동 폭 및 높이 조정)
        ///</summary>
        public static void LabelField_TextAutoWidthHeight(string text, GUIStyle style, int correctionWidth = 30, params GUILayoutOption[] options)
        {
            EditorGUILayout.LabelField(new GUIContent(text), style, GetAdjustedOptions(text, style, correctionWidth, options));
        }



        ///<summary>
        ///라벨 필드: 텍스트 (자동 폭 및 높이 조정)
        ///</summary>
        public static void LabelField_TextAutoWidthHeight(string text, int correctionWidth = 30, params GUILayoutOption[] options)
        {
            GUIStyle guiStyle = NewGUIStyle_Label();
            EditorGUILayout.LabelField(new GUIContent(text), guiStyle, GetAdjustedOptions(text, guiStyle, correctionWidth, options));
        }



        ///<summary>
        ///라벨 필드: 텍스트 (자동 폭 및 높이 조정)
        ///</summary>
        public static void LabelField_TextAutoWidthHeight(string text, Color textColor, int correctionWidth = 30, params GUILayoutOption[] options)
        {
            LabelField_TextAutoWidthHeight(text, NewGUIStyle(textColor), correctionWidth, options);
        }



        ///<summary>
        ///라벨 필드: 텍스트 (자동 폭 및 높이 조정)
        ///</summary>
        public static void LabelField_TextAutoWidthHeight(string text, FontStyle fontStyle, int correctionWidth = 30, params GUILayoutOption[] options)
        {
            LabelField_TextAutoWidthHeight(text, NewGUIStyle(fontStyle), correctionWidth, options);
        }



        ///<summary>
        ///라벨 필드: 텍스트 (자동 폭 및 높이 조정)
        ///</summary>
        public static void LabelField_TextAutoWidthHeight(string text, Color textColor, FontStyle fontStyle, int correctionWidth = 30, params GUILayoutOption[] options)
        {
            LabelField_TextAutoWidthHeight(text, NewGUIStyle(textColor, fontStyle), correctionWidth, options);
        }



        #endregion



        ///======================================================================================================================================================



        //? Button 렌더링



        #region Button 렌더링

        /// <summary>
        /// Button 렌더링 시 폭을 보정하기 위해 사용하는 기본 보정값입니다.
        /// </summary>
        public static int ButtonCorrectionWidth = 40;



        /// <summary>
        /// 특정 <see cref="GUIStyle"/>와 <paramref name="guiContent"/>로 버튼을 그린 뒤, 클릭 시 <paramref name="action"/>을 실행합니다.
        /// </summary>
        /// <param name="editor">Undo 기록을 위해 필요한 대상 <see cref="Editor"/> 인스턴스</param>
        /// <param name="guiContent">버튼에 표시할 텍스트 및 툴팁 정보</param>
        /// <param name="guiStyle">버튼에 적용할 <see cref="GUIStyle"/></param>
        /// <param name="action">버튼 클릭 시 실행할 액션</param>
        /// <param name="enableRecord">
        /// true면 <see cref="EditorGUI.BeginChangeCheck()"/> / <see cref="EditorGUI.EndChangeCheck()"/>를 사용하여 Undo 기록 및
        /// Dirty 마크를 설정합니다.
        /// </param>
        /// <param name="autoSize">
        /// true면 <see cref="GUILayoutOption"/> 등을 기준으로 버튼 폭을 자동 조정합니다
        /// (<see cref="ButtonCorrectionWidth"/>를 사용하여 텍스트 길이에 맞춰 크기를 조정).
        /// </param>
        /// <param name="options">추가적인 <see cref="GUILayoutOption"/> 배열</param>
        /// <returns>버튼이 클릭되었으면 true, 아니면 false</returns>
        public static bool Render_Button(
            Editor editor,
            GUIContent guiContent,
            GUIStyle guiStyle,
            Action action,
            bool enableRecord,
            bool autoSize,
            params GUILayoutOption[] options
        )
        {
            bool buttonClicked = false;

            // autoSize가 true이면, HorizontalGUI를 사용해 버튼 폭을 텍스트 길이에 맞춤
            if (autoSize)
            {
                HorizontalGUI(() =>
                    buttonClicked = GUILayout.Button(
                        guiContent,
                        guiStyle,
                        GetAdjustedOptions(guiContent.text, guiStyle, ButtonCorrectionWidth, options)
                    )
                );
            }
            else
            {
                buttonClicked = GUILayout.Button(guiContent, guiStyle, options);
            }

            if (buttonClicked)
            {
                bool hasChanged = false;
                if (enableRecord)
                {
                    EditorGUI.BeginChangeCheck();
                }

                action?.Invoke();

                if (enableRecord)
                {
                    hasChanged = EditorGUI.EndChangeCheck();
                    if (hasChanged)
                    {
                        Undo.RecordObject(editor.target, $"\"{guiContent.text}\" 버튼 Clicked");
                        EditorUtility.SetDirty(editor.target);
                    }
                }
            }

            return buttonClicked;
        }



        /// <summary>
        /// 기본 <see cref="GUI.skin.button"/> 스타일로 버튼을 렌더링합니다.
        /// </summary>
        /// <param name="editor">Undo 기록을 위해 필요한 대상 <see cref="Editor"/> 인스턴스</param>
        /// <param name="guiContent">버튼에 표시할 텍스트 및 툴팁 정보</param>
        /// <param name="action">버튼 클릭 시 실행할 액션</param>
        /// <param name="enableRecord">true면 Undo 기록이 활성화됩니다.</param>
        /// <param name="autoSize">true면 버튼 폭을 자동 조정합니다.</param>
        /// <param name="options">추가적인 <see cref="GUILayoutOption"/> 배열</param>
        /// <returns>버튼이 클릭되었으면 true, 아니면 false</returns>
        public static bool Render_Button(
            Editor editor,
            GUIContent guiContent,
            Action action,
            bool enableRecord = true,
            bool autoSize = false,
            params GUILayoutOption[] options
        )
        {
            var button = GUI.skin.button;
            button.richText = true;
            return Render_Button(editor, guiContent, button, action, enableRecord, autoSize, options);
        }



        /// <summary>
        /// 기본 <see cref="GUI.skin.button"/> 스타일로 버튼을 렌더링합니다.
        /// </summary>
        /// <param name="editor">Undo 기록을 위해 필요한 대상 <see cref="Editor"/> 인스턴스</param>
        /// <param name="label">버튼에 표시할 텍스트</param>
        /// <param name="action">버튼 클릭 시 실행할 액션</param>
        /// <param name="tooltip">버튼 툴팁</param>
        /// <param name="enableRecord">true면 Undo 기록이 활성화됩니다.</param>
        /// <param name="autoSize">true면 버튼 폭을 자동 조정합니다.</param>
        /// <param name="options">추가적인 <see cref="GUILayoutOption"/> 배열</param>
        /// <returns>버튼이 클릭되었으면 true, 아니면 false</returns>
        public static bool Render_Button(
            Editor editor,
            string label,
            Action action,
            string tooltip = "",
            bool enableRecord = true,
            bool autoSize = false,
            params GUILayoutOption[] options
        )
        {
            return Render_Button(editor, NewGUIContent_Label(label, tooltip), action, enableRecord, autoSize, options);
        }



        /// <summary>
        /// 커스텀 색상(배경/텍스트)을 적용해 버튼을 렌더링합니다.
        /// </summary>
        /// <param name="editor">Undo 기록을 위해 필요한 대상 <see cref="Editor"/> 인스턴스</param>
        /// <param name="guiContent">버튼에 표시할 텍스트 및 툴팁 정보</param>
        /// <param name="action">버튼 클릭 시 실행할 액션</param>
        /// <param name="backgroundColor">버튼 배경색 (null이면 적용 안 함)</param>
        /// <param name="textColor">버튼 텍스트 색상 (null이면 적용 안 함)</param>
        /// <param name="enableRecord">true면 Undo 기록이 활성화됩니다.</param>
        /// <param name="autoSize">true면 버튼 폭을 자동 조정합니다.</param>
        /// <param name="options">추가적인 <see cref="GUILayoutOption"/> 배열</param>
        /// <returns>버튼이 클릭되었으면 true, 아니면 false</returns>
        public static bool Render_ButtonWithStyle(
            Editor editor,
            GUIContent guiContent,
            Action action,
            Color? backgroundColor = null,
            Color? textColor = null,
            bool enableRecord = true,
            bool autoSize = false,
            params GUILayoutOption[] options
        )
        {
            bool buttonClicked;

            // 원래 GUI 색상 저장
            var originalBackgroundColor = GUI.backgroundColor;
            var originalContentColor = GUI.contentColor;

            // 지정된 색상 적용
            if (backgroundColor.HasValue)
                GUI.backgroundColor = backgroundColor.Value;

            if (textColor.HasValue)
            {
                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.normal.textColor = textColor.Value;
                buttonStyle.hover.textColor = textColor.Value * 0.9f;   // 옵션: hover 시 약간 어둡게
                buttonStyle.active.textColor = textColor.Value * 0.8f;  // 옵션: 클릭 시 더 어둡게

                buttonClicked = Render_Button(editor, guiContent, buttonStyle, action, enableRecord, autoSize, options);
            }
            else
            {
                buttonClicked = Render_Button(editor, guiContent, action, enableRecord, autoSize, options);
            }

            // 원래 GUI 색상 복원
            GUI.backgroundColor = originalBackgroundColor;
            GUI.contentColor = originalContentColor;

            return buttonClicked;
        }



        /// <summary>
        /// 커스텀 색상(배경/텍스트)을 적용해 버튼을 렌더링합니다.
        /// </summary>
        /// <param name="label">버튼에 표시할 텍스트</param>
        /// <param name="tooltip">버튼 툴팁</param>
        /// <param name="backgroundColor">버튼 배경색 (null이면 적용 안 함)</param>
        /// <param name="textColor">버튼 텍스트 색상 (null이면 적용 안 함)</param>
        public static bool Render_ButtonWithStyle(
            Editor editor,
            string label,
            string tooltip,
            Action action,
            Color? backgroundColor,
            Color? textColor,
            bool enableRecord = true,
            bool autoSize = false,
            params GUILayoutOption[] options
        )
        {
            var guiContent = NewGUIContent_Label(label, tooltip);
            return Render_ButtonWithStyle(editor, guiContent, action, backgroundColor, textColor, enableRecord, autoSize, options);
        }



        /// <summary>
        /// 여러 <see cref="Object"/>를 편집할 수 있는 <see cref="EditorExpand{T}"/>를 대상으로,
        /// 특정 <see cref="GUIStyle"/>와 <paramref name="guiContent"/>로 버튼을 그린 뒤, 클릭 시 <paramref name="action"/>을 실행합니다.
        /// </summary>
        /// <typeparam name="T"><see cref="Object"/>를 상속받는 타입</typeparam>
        /// <param name="editor">Targets를 통해 여러 오브젝트를 참조하는 <see cref="EditorExpand{T}"/> 인스턴스</param>
        /// <param name="guiContent">버튼에 표시할 텍스트 및 툴팁 정보</param>
        /// <param name="guiStyle">버튼에 적용할 <see cref="GUIStyle"/></param>
        /// <param name="action">버튼 클릭 시, 모든 대상 오브젝트(<see cref="EditorExpand{T}.Targets"/>)에 대해 실행할 액션</param>
        /// <param name="enableRecord">true면 Undo 기록이 활성화됩니다.</param>
        /// <param name="autoSize">true면 버튼 폭을 자동 조정합니다.</param>
        /// <param name="options">추가적인 <see cref="GUILayoutOption"/> 배열</param>
        /// <returns>버튼이 클릭되었으면 true, 아니면 false</returns>
        public static bool Render_Button<T>(
            EditorExpand<T> editor,
            GUIContent guiContent,
            GUIStyle guiStyle,
            Action<T> action,
            bool enableRecord = true,
            bool autoSize = false,
            params GUILayoutOption[] options
        ) where T : Object
        {
            bool buttonClicked = false;

            if (autoSize)
            {
                HorizontalGUI(() =>
                    buttonClicked = GUILayout.Button(
                        guiContent,
                        guiStyle,
                        GetAdjustedOptions(guiContent.text, guiStyle, ButtonCorrectionWidth, options)
                    )
                );
            }
            else
            {
                buttonClicked = GUILayout.Button(guiContent, guiStyle, options);
            }

            if (buttonClicked)
            {
                bool hasChanged = false;
                if (enableRecord)
                {
                    EditorGUI.BeginChangeCheck();
                }

                // 모든 대상 오브젝트에 대해 액션 실행
                foreach (var target in editor.Targets)
                {
                    action?.Invoke(target);
                }

                if (enableRecord)
                {
                    hasChanged = EditorGUI.EndChangeCheck();
                    if (hasChanged)
                    {
                        Undo.RecordObjects(editor.Targets.ToArray(), $"\"{guiContent.text}\" 버튼 Clicked");
                        foreach (var target in editor.Targets)
                        {
                            EditorUtility.SetDirty(target);
                        }
                    }
                }
            }

            return buttonClicked;
        }



        /// <summary>
        /// 여러 <see cref="Object"/>를 편집할 수 있는 <see cref="EditorExpand{T}"/>를 대상으로,
        /// 기본 <see cref="GUI.skin.button"/> 스타일로 버튼을 렌더링합니다.
        /// </summary>
        /// <typeparam name="T"><see cref="Object"/>를 상속받는 타입</typeparam>
        public static bool Render_Button<T>(
            EditorExpand<T> editor,
            GUIContent guiContent,
            Action<T> action,
            bool enableRecord = true,
            bool autoSize = false,
            params GUILayoutOption[] options
        ) where T : Object
        {
            return Render_Button(editor, guiContent, GUI.skin.button, action, enableRecord, autoSize, options);
        }



        /// <summary>
        /// 여러 <see cref="Object"/>를 편집할 수 있는 <see cref="EditorExpand{T}"/>를 대상으로,
        /// 기본 <see cref="GUI.skin.button"/> 스타일로 버튼을 렌더링합니다.
        /// </summary>
        /// <typeparam name="T"><see cref="Object"/>를 상속받는 타입</typeparam>
        public static bool Render_Button<T>(
            EditorExpand<T> editor,
            string label,
            Action<T> action,
            string tooltip = "",
            bool enableRecord = true,
            bool autoSize = false,
            params GUILayoutOption[] options
        ) where T : Object
        {
            return Render_Button(editor, NewGUIContent_Label(label, tooltip), action, enableRecord, autoSize, options);
        }



        /// <summary>
        /// 여러 <see cref="Object"/>를 편집할 수 있는 <see cref="EditorExpand{T}"/>를 대상으로,
        /// 커스텀 색상(배경/텍스트)을 적용해 버튼을 렌더링합니다.
        /// </summary>
        /// <typeparam name="T"><see cref="Object"/>를 상속받는 타입</typeparam>
        /// <param name="editor"><see cref="EditorExpand{T}"/> 인스턴스</param>
        /// <param name="guiContent">버튼에 표시할 텍스트 및 툴팁 정보</param>
        /// <param name="action">버튼 클릭 시, 모든 대상 오브젝트에 대해 실행할 액션</param>
        /// <param name="backgroundColor">버튼 배경색 (null이면 적용 안 함)</param>
        /// <param name="textColor">버튼 텍스트 색상 (null이면 적용 안 함)</param>
        /// <param name="enableRecord">true면 Undo 기록이 활성화됩니다.</param>
        /// <param name="autoSize">true면 버튼 폭을 자동 조정합니다.</param>
        /// <param name="options">추가적인 <see cref="GUILayoutOption"/> 배열</param>
        /// <returns>버튼이 클릭되었으면 true, 아니면 false</returns>
        public static bool Render_ButtonWithStyle<T>(
            EditorExpand<T> editor,
            GUIContent guiContent,
            Action<T> action,
            Color? backgroundColor = null,
            Color? textColor = null,
            bool enableRecord = true,
            bool autoSize = false,
            params GUILayoutOption[] options
        ) where T : Object
        {
            bool buttonClicked;

            var originalBackgroundColor = GUI.backgroundColor;
            var originalContentColor = GUI.contentColor;

            if (backgroundColor.HasValue)
                GUI.backgroundColor = backgroundColor.Value;

            if (textColor.HasValue)
            {
                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.normal.textColor = textColor.Value;
                buttonStyle.hover.textColor = textColor.Value * 0.9f;
                buttonStyle.active.textColor = textColor.Value * 0.8f;

                buttonClicked = Render_Button(editor, guiContent, buttonStyle, action, enableRecord, autoSize, options);
            }
            else
            {
                buttonClicked = Render_Button(editor, guiContent, action, enableRecord, autoSize, options);
            }

            GUI.backgroundColor = originalBackgroundColor;
            GUI.contentColor = originalContentColor;

            return buttonClicked;
        }



        /// <summary>
        /// 여러 <see cref="Object"/>를 편집할 수 있는 <see cref="EditorExpand{T}"/>를 대상으로,
        /// 커스텀 색상(배경/텍스트)을 적용해 버튼을 렌더링합니다.
        /// </summary>
        public static bool Render_ButtonWithStyle<T>(
            EditorExpand<T> editor,
            string label,
            string tooltip,
            Action<T> action,
            Color? backgroundColor,
            Color? textColor,
            bool enableRecord = true,
            bool autoSize = false,
            params GUILayoutOption[] options
        ) where T : Object
        {
            var guiContent = NewGUIContent_Label(label, tooltip);
            return Render_ButtonWithStyle(editor, guiContent, action, backgroundColor, textColor, enableRecord, autoSize, options);
        }



        #endregion



        ///======================================================================================================================================================



        //? 서식 메서드



        #region 서식 메서드



        ///<summary>
        ///구분선 추가
        ///</summary>
        public static void Format_HorizontalLine(bool space_Start = true, bool space_End = true)
        {
            if (space_Start) EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            if (space_End) EditorGUILayout.Space();
        }



        /// <summary>
        /// 들여쓰기 (+추가)
        /// </summary>
        public static void Format_IndentLevel_Plus()
        {
            EditorGUI.indentLevel++;
        }



        /// <summary>
        /// 들여쓰기 (-감소)
        /// </summary>
        public static void Format_IndentLevel_Minus()
        {
            EditorGUI.indentLevel--;
        }



        /// <summary>
        /// 들여쓰기 이벤트
        /// </summary>
        public static void Format_IndentLevel_PlusMinusEvent(Action action)
        {
            Format_IndentLevel_Plus();
            action?.Invoke();
            Format_IndentLevel_Minus();
        }



        #endregion



        ///======================================================================================================================================================



        //? 이벤트



        #region 이벤트



        /// <summary>
        /// <paramref name="editor"/>가 편집하는 대상 오브젝트에 대해 Undo 레코드를 생성하고
        /// Dirty 플래그를 설정하는 래핑 메서드입니다.
        /// </summary>
        /// <param name="editor">대상 오브젝트를 가진 <see cref="Editor"/> 인스턴스</param>
        /// <param name="label">Undo 레코드에 표시될 문자열</param>
        public static void UndoRecordObject(Editor editor, string label)
        {
            Undo.RecordObject(editor.target, $"변경됨: {editor.target.name} | {label}");
            EditorUtility.SetDirty(editor.target);
        }

        /// <summary>
        /// 지정된 <paramref name="action"/>을 실행하기 전후에 변경 사항을 감지하고,
        /// 실제로 변경이 발생하면 Undo 레코드를 생성하는 메서드입니다.
        /// </summary>
        /// <param name="target">Undo 레코드를 남길 <see cref="Object"/></param>
        /// <param name="action">실행할 액션</param>
        /// <param name="recordText">Undo 레코드에 표시될 문자열(없으면 <c>null</c>)</param>
        public static void UndoRecordObject(Object target, Action action, string recordText)
        {
            EditorGUI.BeginChangeCheck();
            action?.Invoke();
            bool hasChanged = EditorGUI.EndChangeCheck();

            if (hasChanged)
            {
                // 변경이 실제로 발생했을 때만 Undo 레코드 생성
                Undo.RecordObject(target, recordText ?? $"{target.name} | {action}");
                EditorUtility.SetDirty(target);

                // Prefab 인스턴스인 경우, Prefab 수정 사항도 기록
                if (PrefabUtility.IsPartOfPrefabInstance(target))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                }
            }
        }

        /// <summary>
        /// <see cref="UndoRecordObject(Object, Action, string)"/>와 동일한 동작을 수행하되,
        /// <paramref name="editor"/>의 <see cref="Editor.target"/>을 대상으로 실행하는 메서드입니다.
        /// </summary>
        /// <param name="editor">Undo 레코드를 남길 대상 <see cref="Editor"/> 인스턴스</param>
        /// <param name="action">실행할 액션</param>
        /// <param name="recordText">Undo 레코드에 표시될 문자열</param>
        public static void UndoRecordObject(Editor editor, Action action, string recordText)
        {
            UndoRecordObject(editor.target, action, recordText);
        }

        /// <summary>
        /// 에디터에서 특정 작업(<paramref name="action"/>)을 실행하기 전후로 변경 사항을 감지하고,
        /// 실제로 변경이 발생하면 각 <see cref="EditorExpand{T}.Targets"/>에 대해
        /// Undo 및 Dirty 플래그를 설정하거나 추가 액션(<paramref name="changeAction"/>)을 실행합니다.
        /// </summary>
        /// <typeparam name="T">에디터에서 편집하는 오브젝트의 타입 (<see cref="UnityEngine.Object"/> 상속)</typeparam>
        /// <param name="editor">여러 대상 오브젝트를 관리하는 <see cref="EditorExpand{T}"/> 인스턴스</param>
        /// <param name="action">기본적으로 실행할 에디터 작업</param>
        /// <param name="changeAction">변경 사항이 발생했을 때 각 오브젝트에 대해 추가로 실행할 작업</param>
        /// <param name="defaultApplyAction">
        /// true면 기본 Undo 및 Dirty 처리(<see cref="SerializedObject.ApplyModifiedProperties"/> 포함)를 수행합니다.
        /// false면 <paramref name="changeAction"/>만 실행하고 기본 처리는 생략됩니다.
        /// </param>
        /// <param name="ignoredChangeConditions">
        /// 변경 사항을 무시해야 하는 조건(람다). 이 조건을 만족하면 변경 사항이 있어도 무시됩니다.
        /// </param>
        public static void CheckChangeAction<T>(
            EditorExpand<T> editor,
            Action action,
            Action<T> changeAction,
            bool defaultApplyAction = true,
            Func<bool> ignoredChangeConditions = null
        ) where T : UnityEngine.Object
        {
            // 에디터의 직렬화된 객체를 업데이트 (필요 시)
            // editor.serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            action?.Invoke(); // 지정된 에디터 작업 실행

            if (EditorGUI.EndChangeCheck())
            {
                // 무시할 조건이 지정되어 있고, 조건을 만족하면 변경 사항을 무시
                if (ignoredChangeConditions != null && ignoredChangeConditions.Invoke())
                {
                    return;
                }

                foreach (var target in editor.Targets)
                {
                    SerializedObject serializedObject = new SerializedObject(target);

                    if (defaultApplyAction)
                    {
                        Undo.RecordObject(target, "커스텀 에디터 수정됨");
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(target);
                    }

                    changeAction?.Invoke(target);
                }
            }
        }

        /// <summary>
        /// Foldout 상태 변화(FoldOut 열림/접힘)를 변경 사항에서 제외하고,
        /// 나머지 변경만 감지하여 적용하는 메서드입니다.
        /// </summary>
        /// <typeparam name="T">에디터에서 편집하는 오브젝트의 타입 (<see cref="UnityEngine.Object"/> 상속)</typeparam>
        /// <param name="editor">여러 대상 오브젝트를 관리하는 <see cref="EditorExpand{T}"/> 인스턴스</param>
        /// <param name="action">기본적으로 실행할 에디터 작업</param>
        /// <param name="changeAction">변경 사항이 발생했을 때 각 오브젝트에 대해 추가로 실행할 작업</param>
        /// <param name="currentStateProvider">
        /// 현재 Foldout 상태 배열을 반환하는 함수. 예: <c>() => new[] { fold1, fold2 }</c>
        /// </param>
        /// <param name="defaultApplyAction">
        /// true면 기본 Undo 및 Dirty 처리(<see cref="SerializedObject.ApplyModifiedProperties"/> 등)를 수행합니다.
        /// </param>
        public static void CheckFoldoutChangeAction<T>(
            EditorExpand<T> editor,
            Action action,
            Action<T> changeAction,
            Func<bool[]> currentStateProvider,
            bool defaultApplyAction = true
        ) where T : UnityEngine.Object
        {
            // 현재 Foldout 상태를 미리 저장
            bool[] previousFoldoutState = currentStateProvider.Invoke().ToArray();

            // 변경 사항을 감지하되, Foldout 상태 변화만 무시하기 위해 CheckChangeAction을 사용
            CheckChangeAction(
                editor,
                action,
                changeAction,
                defaultApplyAction,
                () =>
                {
                    // 현재 Foldout 상태를 다시 가져옴
                    bool[] currentState = currentStateProvider.Invoke();

                    // Foldout 상태가 바뀌었는지 확인
                    bool foldoutChanged = IsFoldoutChanged(previousFoldoutState, currentState);

                    if (foldoutChanged)
                    {
                        Debug.Log("Foldout 상태 변화 감지됨. 무시 처리 중.");
                    }

                    // Foldout 상태가 변경되었으면 무시 처리(true 반환),
                    // 그렇지 않으면 false를 반환하여 변경 사항을 적용하도록 함
                    return GUI.changed && foldoutChanged;
                }
            );

            /// <summary>
            /// 이전/현재 Foldout 상태 배열을 비교하여, 하나라도 달라졌는지 판별합니다.
            /// </summary>
            /// <param name="previousState">이전 Foldout 상태 배열</param>
            /// <param name="currentState">현재 Foldout 상태 배열</param>
            /// <returns>Foldout 상태가 변경되었으면 true, 아니면 false</returns>
            static bool IsFoldoutChanged(bool[] previousState, bool[] currentState)
            {
                for (int i = 0; i < previousState.Length; i++)
                {
                    if (previousState[i] != currentState[i])
                    {
                        return true;
                    }
                }
                return false;
            }
        }



        #endregion



        ///======================================================================================================================================================



        //? GUI 요소 그리기 (확장)



        #region GUI 요소 그리기 (확장)



        ///<summary>
        ///받아온 조건의 여부에 따라 액션 안의 GUI가 활성/비활성화
        ///</summary>
        public static void ActiveGUICondition(bool condition, Action action)
        {
            // GUI.enabled의 현재 상태를 저장
            var previousState = GUI.enabled;

            // 조건에 따라 GUI 활성/비활성화 설정
            GUI.enabled &= condition;

            // 액션 실행
            action?.Invoke();

            // GUI.enabled를 이전 상태로 복원
            GUI.enabled = previousState;
        }



        ///<summary>
        ///툴팁 박스
        ///</summary>
        public static void HelpBox(MessageType messageType, string tooltip, bool? wide = null)
        {
            EditorGUILayout.Space();

            if (wide.HasValue)
            {
                EditorGUILayout.HelpBox(tooltip, messageType, wide.Value);
                return;
            }
            EditorGUILayout.HelpBox(tooltip, messageType);
        }



        /// <summary>
        /// 유니티 기본 Script를 그리는 것처럼 그리기
        /// </summary>
        public static void DrawDefaultScriptField(UnityEngine.Object target, string label = "Script", params GUILayoutOption[] options)
        {
            MonoScript script = null;

            if (target is MonoBehaviour monoBehaviour)
            {
                script = MonoScript.FromMonoBehaviour(monoBehaviour);
            }
            else if (target is ScriptableObject scriptableObject)
            {
                script = MonoScript.FromScriptableObject(scriptableObject);
            }

            if (script != null)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(label, script, typeof(MonoScript), false, options);
                EditorGUI.EndDisabledGroup();
            }
        }



        /// <summary>
        /// 유니티 기본 Script를 그리는 것처럼 그리기 (2개)
        /// </summary>
        public static void DrawDefaultScriptFieldDouble(UnityEngine.Object target1, UnityEngine.Object target2, string label = "Script")
        {
            HorizontalGUI(() =>
            {
                LabelField_TextAutoWidthHeight(label, 30, GUILayout.MinWidth(Screen.width / 3f));
                DrawDefaultScriptField(target1, "", GUILayout.MinWidth(Screen.width / 3f));
                DrawDefaultScriptField(target2, "", GUILayout.MinWidth(Screen.width / 3f));
            });
        }



        ///<summary>
        ///<see cref="Editor.DrawDefaultInspector"/>를 헤더lv3로 그리기
        ///</summary>
        public static void DrawDefaultInspector_WithHead(Editor editor, Color? color, bool endHorizontalLine, string headText = "Default Inspector")
        {
            var headGUIStyle = (color.HasValue) ? CustomLabelStyleNew(LabelStyle_Head3, color.Value, null) : LabelStyle_Head3;

            AutoLabelField_Head(headText, headGUIStyle, () =>
            {
                editor.DrawDefaultInspector();
            }, endHorizontalLine);
        }



        ///<summary>
        ///Odin 기본 인스펙터를 헤더lv3로 그리기
        ///</summary>
        public static void DrawOdinInspector_WithHead(PropertyTree odinPropertyTree, Color? color, bool endHorizontalLine, string headText = "Odin Inspector")
        {
            var headGUIStyle = (color.HasValue) ? CustomLabelStyleNew(LabelStyle_Head3, color.Value, null) : LabelStyle_Head3;

            AutoLabelField_Head(headText, headGUIStyle, () =>
            {
                odinPropertyTree.Draw();
                odinPropertyTree.ApplyChanges();
            }, endHorizontalLine);
        }



        ///<summary>
        ///<see cref="Editor.DrawDefaultInspector"/>를 폴드로 그리기
        ///</summary>
        public static void DrawDefaultInspector_WithFold(Editor editor, ref bool fold, Color? color, string text = "Fold | Inspector", bool useIndentLevel = false)
        {
            FoldOut(ref fold, NewGUIContent_Label(text), null, color, () =>
            {
                editor.DrawDefaultInspector();
            }, useIndentLevel);
        }



        ///<summary>
        ///Odin 기본 인스펙터를 폴드로 그리기
        ///</summary>
        public static void DrawOdinInspector_WithFold(PropertyTree odinPropertyTree, ref bool fold, Color? color, string text = "Fold | Odin Inspector", bool useIndentLevel = false)
        {
            FoldOut(ref fold, NewGUIContent_Label(text), null, color, () =>
            {
                odinPropertyTree.Draw();
                odinPropertyTree.ApplyChanges();
            }, useIndentLevel);
        }



        /// <summary>
        /// 강조된 배경 박스를 생성하고, 내부에 UI 요소를 배치합니다.
        /// </summary>
        /// <param name="color">박스 배경 색상</param>
        /// <param name="action">박스 내부에서 실행할 UI 요소</param>
        public static void DrawHighlightedBox(Color color, Action action)
        {
            if (action == null) return;

            //. GUIStyle 설정
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = CreateColorTexture(color);

            //! EditorGUI.indentLevel 보정
            //? margin만 조정하여 박스 내부 내용은 그대로 유지
            //. margin.left의 기본값에 indentLevel * 15을 더해준다
            boxStyle.margin.left += EditorGUI.indentLevel * 15;

            GUILayout.BeginVertical(boxStyle);
            {
                action.Invoke();
            }
            GUILayout.EndVertical();

            /// <summary>
            /// 단색 텍스처 생성
            /// </summary>
            /// <param name="color">텍스처의 색상</param>
            /// <returns>단색 텍스처</returns>
            static Texture2D CreateColorTexture(Color color)
            {
                Texture2D texture = new Texture2D(1, 1);
                texture.SetPixel(0, 0, color);
                texture.Apply();
                return texture;
            }
        }




        /// <summary>
        /// 강조된 배경 박스를 생성하고, 내부에 UI 요소를 배치합니다.
        /// 기본 배경보다 더 어두운 색으로 설정됩니다.
        /// </summary>
        /// <param name="action">박스 내부에서 실행할 UI 요소</param>
        public static void DrawHighlightedBox(Action action, float darkeningFactor = 0.4f)
        {
            if (action == null) return;

            // 기본 박스 배경 색 가져오기
            Color defaultBackgroundColor = GetDefaultBoxBackgroundColor();

            // 기본 색상에서 어두운 색 계산
            Color darkerColor = GetDarkerColor(defaultBackgroundColor, darkeningFactor);

            // 어두운 색으로 박스 그리기
            DrawHighlightedBox(darkerColor, action);

            /// <summary>
            /// 기본 GUI 박스의 배경 색상을 가져옵니다.
            /// </summary>
            /// <returns>박스 배경 색상</returns>
            static Color GetDefaultBoxBackgroundColor()
            {
                // 기본 GUIStyle의 박스 배경 텍스처 가져오기
                Texture2D texture = GUI.skin.box.normal.background as Texture2D;

                // 텍스처에서 색상 추출 (첫 번째 픽셀 사용)
                if (texture != null && texture.width > 0 && texture.height > 0)
                {
                    return texture.GetPixel(0, 0);
                }

                // 텍스처가 없는 경우 기본 색상 반환
                return Color.gray;
            }

            /// <summary>
            /// 현재 색상보다 더 어두운 색을 계산합니다.
            /// </summary>
            /// <param name="color">기준 색상</param>
            /// <returns>더 어두운 색상</returns>
            static Color GetDarkerColor(Color color, float darkeningFactor)
            {
                return new Color(
                    Mathf.Clamp01(color.r * darkeningFactor),
                    Mathf.Clamp01(color.g * darkeningFactor),
                    Mathf.Clamp01(color.b * darkeningFactor),
                    color.a // 투명도 유지
                );
            }
        }



        #endregion



        ///======================================================================================================================================================



        //? 커스텀 에디터 전용 StringBuilder



        ///<summary>
        /// 커스텀 에디터에서 공용으로 사용할 전역 StringBuilder
        /// </summary>
        public static readonly StringBuilder StringBuilder = new StringBuilder();



        ///<summary>
        /// 커스텀 에디터에서 공용으로 사용할 전역 StringBuilder을 using
        /// <para>사용 전에 Clear되며, 사용후에 Clear된다</para>
        /// </summary>
        public static void UsingStringBuilder(Action<StringBuilder> action)
        {
            StringBuilder.Clear();
            action?.Invoke(StringBuilder);
            StringBuilder.Clear();
        }



        ///======================================================================================================================================================
    }



    namespace Legacy
    {
        /// <summary>
        /// Property 캐싱 클래스
        /// </summary>
        [Obsolete]
        public class CachedSerializedProperty
        {
            ///======================================================================================================================================================



            public CachedSerializedProperty(string propertyPath)
            {
                PropertyPath = propertyPath;
            }



            /// <summary> 문자열 배열로 가져와, 사이에 .을 찍으며 문자열을 결합 </summary>
            public CachedSerializedProperty(params string[] propertyPaths)
            {
                PropertyPath = SU_String.JoinWithSeparator(propertyPaths, '.');
            }



            public string PropertyPath { get; set; }



            private SerializedProperty property;



            public SerializedProperty GetProperty(SerializedObject serializedObject)
            {
                if (property != null) { return property; }
                property = serializedObject.FindProperty(PropertyPath);
                return property;
            }



            public SerializedProperty GetProperty(UnityEditor.Editor editor)
            {
                return GetProperty(editor.serializedObject);
            }



            ///======================================================================================================================================================



            /// <summary> 문자열 배열로 가져와, 사이에 .을 찍으며 문자열을 결합 </summary>
            public static SerializedProperty GetProperty(SerializedObject serializedObject, params string[] propertyPaths)
            {
                return serializedObject.FindProperty(SU_String.JoinWithSeparator(propertyPaths, '.'));
            }



            /// <summary> 문자열 배열로 가져와, 사이에 .을 찍으며 문자열을 결합 </summary>
            public static SerializedProperty GetProperty(UnityEditor.Editor editor, params string[] propertyPaths)
            {
                return GetProperty(editor.serializedObject, propertyPaths);
            }



            ///======================================================================================================================================================
        }
    }



    ///======================================================================================================================================================
}


