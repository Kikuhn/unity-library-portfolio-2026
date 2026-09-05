using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.IO;
using UnityEngine.Events;
using DG.Tweening;
using Pan.Util;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using JetBrains.Annotations;
using UnityEngine.PlayerLoop;
using System.Text;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;



//? 디버그 로그를 텍스트파일에 기록하는걸 관리하는 매니저, 런타임/에디터 모두 작동 하는 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    [Serializable]
    public class CustomDebugLogger
    {
        ///======================================================================================================================================================



        //? 디버그 모드



        /// <summary>
        /// 디버그 모드 활성화 여부
        /// </summary>
        [ShowInInspector]
        [LabelText("디버그 모드 사용")]
        [PropertyOrder(10)]
        public bool UseDebugMode
        {
            get => useDebugMode;
            set
            {
                useDebugMode = value;
                if (!useDebugMode)
                {
                    UseDebugModeEditorOnly = false;
                }
            }
        }
        [SerializeField, HideInInspector] private bool useDebugMode = true;



        /// <summary>
        /// 에디터 전용 디버그 모드 활성화 여부
        /// </summary>
        [ShowInInspector]
        [LabelText("에디터에서만 작동")]
        [EnableIf(nameof(UseDebugMode))]
        [Indent(1)]
        [PropertyOrder(10)]
        public bool UseDebugModeEditorOnly
        {
            get => useDebugModeEditorOnly;
            set
            {
                useDebugModeEditorOnly = value;
            }
        }
        [SerializeField, HideInInspector] private bool useDebugModeEditorOnly = false;



        [ShowInInspector]
        [LabelText("현재 디버그 모드")]
        [Indent(1)]
        [PropertyOrder(10)]
        public bool CurrentDebugMode => UseDebugMode && (!UseDebugModeEditorOnly || Application.isEditor);



        ///======================================================================================================================================================



        //? 디버그 파일



        /// <summary>
        /// 기본 로그 파일 이름
        /// </summary>
        [ShowInInspector]
        [LabelText("로그 파일 경로 및 이름")]
        [PropertyTooltip("")]
        [Indent(1)]
        [PropertyOrder(11)]
        public string LogPathFileName
        {
            get => logPathFileName;
            set
            {
                logPathFileName = value;
                Initialize();
            }
        }
        [SerializeField, HideInInspector] private string logPathFileName = "CustomLogSetting.txt";



        /// <summary>
        /// 런타임에서 로그 파일 경로
        /// </summary>
        [ShowInInspector]
        [LabelText("런타임 로그 파일 경로")]
        [PropertyTooltip("Application.persistentDataPath 내부에 생성된다")]
        [Indent(1)]
        [PropertyOrder(11)]
        public string RuntimeLogFilePath
        {
            get => runtimeLogFilePath;
            set => runtimeLogFilePath = value;
        }
        [SerializeField, HideInInspector] private string runtimeLogFilePath;



        /// <summary>
        /// 에디터에서 사용하는 TextAsset
        /// </summary>
        [ShowInInspector]
        [LabelText("에디터 로그 TextAsset")]
        [Indent(1)]
        [PropertyOrder(11)]
        public TextAsset EditorDebugLogText
        {
            get => editorDebugLogText;
            set
            {
                editorDebugLogText = value;
                Initialize(); // 할당 시 초기화
            }
        }
        [SerializeField, HideInInspector] private TextAsset editorDebugLogText;



#if UNITY_EDITOR



        private bool dummy_IsValid_EditorDebugLogText => editorDebugLogText != null;



        [ShowIf(nameof(dummy_IsValid_EditorDebugLogText))]
        [Button("로그 파일 초기화", ButtonAlignment = 1f, Stretch = false), GUIColor(0.93f, 0.33f, 0.40f)]
        [PropertyOrder(12)]
        private void Button_ClearEditorLogFile()
        {
            if (EditorDebugLogText == null) { return; }

            var logTextPath = UnityEditor.AssetDatabase.GetAssetPath(EditorDebugLogText);

            if (UnityEditor.EditorUtility.DisplayDialog("로그 파일 초기화", $"{logTextPath} 파일을 초기화합니다", "초기화 하기", "잘못눌렀어 취소할게"))
            {
                File.WriteAllText(logTextPath, string.Empty);
                UnityEditor.AssetDatabase.Refresh();
            }
        }



        [ShowInInspector, HideLabel, DisplayAsString(EnableRichText = true, Overflow = false), EnableGUI]
        [ShowIf(nameof(dummy_IsValid_EditorDebugLogText))]
        [PropertyOrder(13)]
        private string dummy_EditorDebugLogTextDataSize
        {
            get
            {
                if (!dummy_IsValid_EditorDebugLogText) { return ""; }
                return $"에디터 로그 파일 크기: {editorDebugLogText.dataSize.ToByteSizeString()}";
            }
        }



#endif



        private readonly object _lock = new object();



        ///======================================================================================================================================================



        /// <summary>
        /// 초기화
        /// </summary>
        public void Initialize()
        {
            if (!useDebugMode) return;

#if UNITY_EDITOR
            if (editorDebugLogText == null) // TextAsset이 수동으로 할당되지 않은 경우에만 자동 생성
            {
                SetupEditorFile();
            }
#else
        if (!useDebugModeEditorOnly)
        {
            SetupRuntimeFile();
        }
#endif
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 로그 추가
        /// </summary>
        public void Append(string log)
        {
            if (!useDebugMode) return;

#if UNITY_EDITOR
            if (editorDebugLogText != null)
            {
                string filePath = UnityEditor.AssetDatabase.GetAssetPath(editorDebugLogText);
                AppendToFile(filePath, log);
            }
#else
        if (!useDebugModeEditorOnly && !string.IsNullOrEmpty(runtimeLogFilePath))
        {
            AppendToFile(runtimeLogFilePath, log);
        }
#endif
        }



        /// <summary>
        /// 로그 읽기
        /// </summary>
        public void OverWrite(string log)
        {
            if (!useDebugMode) return;

#if UNITY_EDITOR
            if (editorDebugLogText != null)
            {
                string filePath = UnityEditor.AssetDatabase.GetAssetPath(editorDebugLogText);
                OverwriteToFile(filePath, log);
            }
#else
        if (!useDebugModeEditorOnly && !string.IsNullOrEmpty(runtimeLogFilePath))
        {
            OverwriteToFile(runtimeLogFilePath, log);
        }
#endif
        }



        /// <summary>
        /// 로그 초기화
        /// </summary>
        public void Clear(string log)
        {
            if (!useDebugMode) return;

#if UNITY_EDITOR
            if (editorDebugLogText != null)
            {
                string filePath = UnityEditor.AssetDatabase.GetAssetPath(editorDebugLogText);
                OverwriteToFile(filePath, "");
            }
#else
        if (!useDebugModeEditorOnly && !string.IsNullOrEmpty(runtimeLogFilePath))
        {
            OverwriteToFile(runtimeLogFilePath, log);
        }
#endif
        }



        /// <summary>
        /// 로그 읽기
        /// </summary>
        public string Read()
        {
            if (!useDebugMode) return string.Empty;

#if UNITY_EDITOR
            if (editorDebugLogText != null)
            {
                string filePath = UnityEditor.AssetDatabase.GetAssetPath(editorDebugLogText);
                return File.ReadAllText(filePath);
            }
#else
        if (!useDebugModeEditorOnly && !string.IsNullOrEmpty(runtimeLogFilePath))
        {
            return File.ReadAllText(runtimeLogFilePath);
        }
#endif

            return string.Empty;
        }



        /// <summary>
        /// 로그를 UniTask로 추가
        /// </summary>
        public async UniTask AppendAsync(string log)
        {
            if (!useDebugMode) return;

#if UNITY_EDITOR
            if (editorDebugLogText != null)
            {
                string filePath = UnityEditor.AssetDatabase.GetAssetPath(editorDebugLogText);
                await AppendToFileAsync(filePath, log);
            }
#else
    if (!useDebugModeEditorOnly && !string.IsNullOrEmpty(runtimeLogFilePath))
    {
        await AppendToFileAsync(runtimeLogFilePath, log);
    }
#endif
        }



        /// <summary>
        /// 로그를 UniTask로 덮어쓰기
        /// </summary>
        public async UniTask OverwriteAsync(string log)
        {
            if (!useDebugMode) return;

#if UNITY_EDITOR
            if (editorDebugLogText != null)
            {
                string filePath = UnityEditor.AssetDatabase.GetAssetPath(editorDebugLogText);
                await OverwriteToFileAsync(filePath, log);
            }
#else
    if (!useDebugModeEditorOnly && !string.IsNullOrEmpty(runtimeLogFilePath))
    {
        await OverwriteToFileAsync(runtimeLogFilePath, log);
    }
#endif
        }



        /// <summary>
        /// 로그를 UniTask로 초기화
        /// </summary>
        public async UniTask ClearAsync()
        {
            if (!useDebugMode) return;

#if UNITY_EDITOR
            if (editorDebugLogText != null)
            {
                string filePath = UnityEditor.AssetDatabase.GetAssetPath(editorDebugLogText);
                await OverwriteToFileAsync(filePath, string.Empty);
            }
#else
    if (!useDebugModeEditorOnly && !string.IsNullOrEmpty(runtimeLogFilePath))
    {
        await OverwriteToFileAsync(runtimeLogFilePath, string.Empty);
    }
#endif
        }



        /// <summary>
        /// 로그를 Fire-and-Forget 방식으로 추가 (UniTask)
        /// </summary>
        public void AppendFireAndForget(string log)
        {
            AppendAsync(log).Forget();
        }



        /// <summary>
        /// 로그를 Fire-and-Forget 방식으로 덮어쓰기 (UniTask)
        /// </summary>
        public void OverwriteFireAndForget(string log)
        {
            OverwriteAsync(log).Forget();
        }



        /// <summary>
        /// 로그를 Fire-and-Forget 방식으로 초기화 (UniTask)
        /// </summary>
        public void ClearFireAndForget()
        {
            ClearAsync().Forget();
        }



        ///======================================================================================================================================================



#if UNITY_EDITOR
        /// <summary>
        /// 에디터 환경에서 로그 파일 설정
        /// </summary>
        private void SetupEditorFile()
        {
            string filePath = Path.Combine(Application.dataPath, logPathFileName);
            string relativePath = $"Assets/{logPathFileName}";

            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, string.Empty);
                Debug.Log($"에디터용 로그 파일 생성됨: {filePath}");
            }

            UnityEditor.AssetDatabase.Refresh();
            editorDebugLogText = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(relativePath);
        }
#endif



        /// <summary>
        /// 런타임 환경에서 로그 파일 설정
        /// </summary>
        private void SetupRuntimeFile()
        {
            runtimeLogFilePath = Path.Combine(Application.persistentDataPath, logPathFileName);

            if (!File.Exists(runtimeLogFilePath))
            {
                File.WriteAllText(runtimeLogFilePath, string.Empty);
            }
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 메시지를 파일에 추가
        /// </summary>
        private void OverwriteToFile(string filePath, string message)
        {
            lock (_lock)
            {
                File.WriteAllText(filePath, message, Encoding.UTF8);
            }
        }



        /// <summary>
        /// 메시지를 파일에 추가
        /// </summary>
        private void AppendToFile(string filePath, string message)
        {
            lock (_lock)
            {
                File.AppendAllText(filePath, message, Encoding.UTF8);
            }
        }



        /// <summary>
        /// 메시지를 파일에 비동기로 덮어쓰기 (UniTask)
        /// </summary>
        private async UniTask OverwriteToFileAsync(string filePath, string message)
        {
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                await writer.WriteAsync(message).ConfigureAwait(false);
            }
        }



        /// <summary>
        /// 메시지를 파일에 비동기로 추가 (UniTask)
        /// </summary>
        private async UniTask AppendToFileAsync(string filePath, string message)
        {
            using (var writer = new StreamWriter(filePath, true, Encoding.UTF8))
            {
                await writer.WriteAsync(message).ConfigureAwait(false);
            }
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================
}
