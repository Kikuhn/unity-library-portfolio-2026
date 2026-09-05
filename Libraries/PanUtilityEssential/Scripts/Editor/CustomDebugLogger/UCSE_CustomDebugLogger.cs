using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Text;
using Pan.Util.Editors;



//? "CustomDebugLogger" 를 에디터에서 쉽게 띄우기 위한 인터페이스 등이 정리되어있는 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    ///<summary>
    ///커스텀 디버그 로거 에디터<br/>
    ///(전역 변수/메서드가 포함되어있어 <see cref="OnInspectorGUI{TEditor, TObject}(TEditor, TObject, CustomDebugLogger)"/>를 호출하여 사용가능
    /// </summary>
    public interface ICustomDebugLoggerEditor
    {
        /// <summary>
        /// 커스텀 디버그 로거의 코드 위치<br/>
        /// <b>(파일 경로 같은것이 아닌 코드 흐름 경로)</b>
        /// </summary>
        string[] CustomDebugLoggerFieldPath { get; }



        /// <summary>
        /// 로그 파일 접기/펴기 스위치
        /// </summary>
        public static bool FoldSwitch_Log;


        /// <summary>
        /// 대상 <paramref name="customDebugLogger"/>을 에디터에 그리기
        /// </summary>
        /// <typeparam name="TEditor"></typeparam>
        /// <typeparam name="TObject"></typeparam>
        /// <param name="editor"></param>
        /// <param name="target"></param>
        /// <param name="customDebugLogger"></param>
        public static void OnInspectorGUI<TEditor, TObject>(TEditor editor, TObject target, CustomDebugLogger customDebugLogger) where TEditor : EditorExpand<TObject>, ICustomDebugLoggerEditor where TObject : UnityEngine.Object
        {
            SU_CustomEditor.DrawHighlightedBox(() =>
            {
                var path_LogM = editor.CustomDebugLoggerFieldPath;


                SU_CustomEditor.CheckChangeAction(editor, () =>
                {
                    SU_CustomEditor.RenderField_Property_Path(editor, nameof(customDebugLogger.UseDebugMode)._LowerFirst(), "디버깅 모드 활성화", "", path_LogM);
                }, x => customDebugLogger.UseDebugMode = customDebugLogger.UseDebugMode);


                if (customDebugLogger.UseDebugMode)
                {
                    SU_CustomEditor.Format_IndentLevel_Plus();

                    SU_CustomEditor.RenderField_Property_Path(editor, nameof(customDebugLogger.UseDebugModeEditorOnly)._LowerFirst(), "에디터에서만 작동", "", path_LogM);

                    SU_CustomEditor.CheckChangeAction(editor, () =>
                    {
                        SU_CustomEditor.RenderField_Property_Path(editor, nameof(customDebugLogger.EditorDebugLogText)._LowerFirst(), "로그 파일", "", path_LogM);
                    }, x =>
                    {
                        customDebugLogger.EditorDebugLogText = customDebugLogger.EditorDebugLogText;
                    });

                    if (customDebugLogger.EditorDebugLogText != null)
                    {
                        SU_CustomEditor.Format_IndentLevel_PlusMinusEvent(() =>
                        {
                            var size = customDebugLogger.EditorDebugLogText.dataSize.ToByteSizeString();
                            SU_CustomEditor.LabelField_Text(size);
                        });
                    }



                    if (customDebugLogger.EditorDebugLogText != null)
                    {
                        var logTextPath = AssetDatabase.GetAssetPath(customDebugLogger.EditorDebugLogText);

                        SU_CustomEditor.Render_ButtonWithStyle(editor, "로그 파일 초기화", "", () =>
                        {
                            if (EditorUtility.DisplayDialog("로그 파일 초기화", $"{logTextPath} 파일을 초기화합니다", "초기화 하기", "잘못눌렀어 취소할게"))
                            {
                                File.WriteAllText(logTextPath, string.Empty);
                                AssetDatabase.Refresh();
                            }
                        }, SU_ColorPresetRGB.Red_GrapeFruit1(), null);


                        SU_CustomEditor.FoldOut(ref FoldSwitch_Log, "로그 텍스트 파일 전체 보기", () =>
                        {
                            SU_CustomEditor.LabelField_TextAutoWidthHeight(customDebugLogger.EditorDebugLogText.dataSize.ToString());
                        });
                    }

                    SU_CustomEditor.LabelField_TextAutoWidthHeight($"런타임 로그 파일 경로: {customDebugLogger.RuntimeLogFilePath}");

                    SU_CustomEditor.Format_IndentLevel_Minus();
                }


                SU_CustomEditor.RenderField_Property_Path(editor, nameof(customDebugLogger.LogPathFileName)._LowerFirst(), "기본 로그 파일 이름", "", path_LogM);
            });
        }
    }



    ///======================================================================================================================================================
}