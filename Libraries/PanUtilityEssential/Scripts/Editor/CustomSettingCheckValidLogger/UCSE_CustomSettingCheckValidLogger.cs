using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Text;
using Pan.Util.Editors;
using System.Reflection;



//? 유효성 검사 기록기가 있는 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    /// <summary>
    ///유효성 검사 기록기 에디터 인터페이스
    /// </summary>
    public interface ICustomSettingCheckValidLoggerEditor
    {
        CustomSettingCheckValidLoggerEditor CheckValidLoggerEditor { get; }
    }



    /// <summary>
    ///유효성 검사 기록기 에디터
    /// </summary>
    public class CustomSettingCheckValidLoggerEditor
    {
        /// <summary>
        /// 에디터상에서 변경사항이 감지가 되었다면 이 값을 true로 만들면 된다
        /// </summary>
        public bool Detected_Changed_BeforeCheckValid = false;



        /// <summary>
        ///  이 값을 true로 만든 프레임에서는, 변경사항 감지가 무시된다
        /// </summary>
        public bool Detected_Changed_BeforeCheckValid_IgnoreThisFrame = false;



        /// <summary>
        /// 마지막으로 실행된 유효성 검사 이후에, 변경사항이 감지된 시간
        /// </summary>
        public DateTime? DetectedTime_Changed_BeforeCheckValid = null;



        public static void DrawGUI_CheckValid<TEditor, TObject>(
            TEditor editor, TObject target,
            Func<StringBuilder, bool> firstCondition,
            StringBuilder stringBuilder_ForCheck)
            where TEditor : EditorExpand<TObject>, ICustomSettingCheckValidLoggerEditor
            where TObject : UnityEngine.Object, ICheckValidLogger
        {
            stringBuilder_ForCheck ??= new StringBuilder();
            stringBuilder_ForCheck.Clear();

            if (firstCondition == null || firstCondition.Invoke(stringBuilder_ForCheck))
            {
                if (editor.CheckValidLoggerEditor.Detected_Changed_BeforeCheckValid)
                {
                    stringBuilder_ForCheck.AppendLine($"<size=14><b><color=#ed5565>※ 유효성 검사 실행 권장  ※</color></b></size>");
                    stringBuilder_ForCheck.AppendLine($"<b><color=#f7da64>변경사항 감지</color></b>: {editor.CheckValidLoggerEditor.DetectedTime_Changed_BeforeCheckValid}");
                    stringBuilder_ForCheck.AppendLine();
                }

                stringBuilder_ForCheck.Append($"마지막 유효성 검사: ");
                stringBuilder_ForCheck.Append($"{((target.CheckValidManager.LastCheckValidResult.HasValue) ? ((target.CheckValidManager.LastCheckValidResult.Value) ? "<b><color=#4fc1e9>성공</color></b>" : "<b><color=#ed5565>실패</color></b>") : "<b><color=#ed5565>시행된적 없음</color></b>")}, ");
                if (target.CheckValidManager.LastCheckValidResult.HasValue)
                {
                    stringBuilder_ForCheck.AppendLine($"{((target.CheckValidManager.LastCheckValidTime.HasValue) ? (target.CheckValidManager.LastCheckValidTime) : "<b><color=#ed5565>시행된적 없음</color></b>")}");
                }
                else
                {
                    stringBuilder_ForCheck.AppendLine();
                }


                stringBuilder_ForCheck.AppendLine();
            }


            SU_CustomEditor.DrawHighlightedBox(() =>
            {
                SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder_ForCheck.ToString(true));


                SU_CustomEditor.Render_ButtonWithStyle(editor, "유효성 검사 기록 초기화", "", (x) =>
                {
                    target.CheckValidManager.LastCheckValidTime = null;
                    target.CheckValidManager.LastCheckValidResult = null;
                    editor.CheckValidLoggerEditor.Detected_Changed_BeforeCheckValid_IgnoreThisFrame = true;
                }, SU_ColorPresetRGB.Red_GrapeFruit1(), null, true);
            });
        }


        /// <summary>
        /// 유효성 검사를 실행한 뒤에 이 메서드를 실행해야함
        /// </summary>
        public void AfterCheckValidEvent()
        {
            Detected_Changed_BeforeCheckValid = false;
            DetectedTime_Changed_BeforeCheckValid = null;
            Detected_Changed_BeforeCheckValid_IgnoreThisFrame = true; //유효성 검사를 실행한 행동이 감지되어 변경사항이 감지되는것을 방지
        }
    }



    ///======================================================================================================================================================



    //    [InitializeOnLoad]
    //    public static class PlayModeTimeChecker
    //    {
    //        private const string StartTimeKey = "PlayModeStartTime";

    //        static PlayModeTimeChecker()
    //        {
    //            //? 플레이모드 진입 직전 시간 저장
    //            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    //        }

    //        private static void OnPlayModeStateChanged(PlayModeStateChange state)
    //        {
    //            if (state == PlayModeStateChange.ExitingEditMode)
    //            {
    //                //. 플레이모드 진입 시작 시간 기록
    //                float startTime = (float)EditorApplication.timeSinceStartup;
    //                EditorPrefs.SetFloat(StartTimeKey, startTime);
    //                Debug.Log("[PlayMode Checker] Exiting Edit Mode... 측정 시작!");
    //            }
    //        }

    //        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    //        private static void OnPlayModeStarted()
    //        {
    //#if UNITY_EDITOR
    //            if (EditorPrefs.HasKey(StartTimeKey))
    //            {
    //                float start = EditorPrefs.GetFloat(StartTimeKey);
    //                float end = (float)EditorApplication.timeSinceStartup;
    //                float duration = end - start;

    //                Debug.Log($"[PlayMode Checker] 플레이 모드 진입 완료! 소요 시간: {duration:F2}초");
    //                EditorPrefs.DeleteKey(StartTimeKey); //. 중복 방지
    //            }
    //#endif
    //        }
    //    }



    ///======================================================================================================================================================
}