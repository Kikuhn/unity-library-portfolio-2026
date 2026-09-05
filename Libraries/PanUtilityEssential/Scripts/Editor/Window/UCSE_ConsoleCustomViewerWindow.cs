using UnityEditor;
using UnityEngine;
using System.Collections.Generic;



//? 콘솔 커스텀 뷰어 윈도우가 정리되어 있는 정도의 코드



/* 기본 유니티 콘솔은 로그당 최대 문자열 개수가 정해져있어,
 * 몇몇 초장문의 로그는 도중에 잘려버리는데,
 * 이를 해결하기위한 별도의 로그 매니저
 */



namespace Pan.Util.Editors
{
    using UnityEditor;
    using UnityEngine;
    using System.Collections.Generic;

    //[InitializeOnLoad]
    public static class GlobalLogCollector
    {
        /// <summary>
        /// 전역으로 수집된 로그 메시지 리스트.
        /// </summary>
        private static List<LogMessage> logs = new List<LogMessage>();

        /// <summary>
        /// 외부에서 로그 리스트를 읽을 때 사용.
        /// </summary>
        public static IReadOnlyList<LogMessage> Logs => logs;

        /// <summary>
        /// Editor가 로드되거나 스크립트가 리컴파일될 때 호출되는 정적 생성자.
        /// </summary>
        static GlobalLogCollector()
        {
            //. 전역 로그 이벤트 구독
            Application.logMessageReceived += HandleLog;
        }

        /// <summary>
        /// 전역 로그 리스트를 비우는 메서드.
        /// </summary>
        public static void ClearLogs()
        {
            logs.Clear();
        }

        /// <summary>
        /// Unity 전역 로그 이벤트 콜백.
        /// </summary>
        private static void HandleLog(string logString, string stackTrace, LogType type)
        {
            LogMessage logMessage = new LogMessage
            {
                Message = logString ?? "",
                Type = type.ToString(),
                Time = System.DateTime.Now.ToString("HH:mm:ss")
            };

            logs.Add(logMessage);
        }

        /// <summary>
        /// 로그 단일 항목을 나타내는 구조체.
        /// </summary>
        public struct LogMessage
        {
            public string Message;
            public string Type;
            public string Time;
        }
    }




    /// <summary>
    /// Unity 콘솔 로그를 커스텀 뷰어로 표시하기 위한 에디터 창 클래스입니다.
    /// <para>
    /// - 다양한 로그(Log, Warning, Error)를 필터링하여 표시합니다.
    /// - 긴 메시지가 Unity 콘솔에서 잘려 보이는 문제를 보완하고자,
    ///   "Show Only Truncated Messages" 옵션을 제공합니다.
    /// </para>
    /// </summary>
    public class ConsoleCustomViewerWindow : EditorWindow
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 스크롤 뷰의 현재 위치를 저장하는 변수입니다.
        /// </summary>
        private Vector2 scrollPosition;



        /// <summary>
        /// 수집된 로그 메시지 목록입니다.
        /// </summary>
        private List<LogMessage> logMessages = new List<LogMessage>();



        /// <summary>
        /// 일반 로그(Log)를 표시할지 여부를 나타냅니다.
        /// </summary>
        private bool showLog = true;



        /// <summary>
        /// 경고 로그(Warning)를 표시할지 여부를 나타냅니다.
        /// </summary>
        private bool showWarning = true;



        /// <summary>
        /// 에러 로그(Error)를 표시할지 여부를 나타냅니다.
        /// </summary>
        private bool showError = true;



        /// <summary>
        /// 메시지 길이가 특정 기준(TruncateLength)을 넘어서는, 
        /// 잘린 메시지만 보여줄지 여부를 나타냅니다.
        /// </summary>
        private bool showOnlyTruncatedMessages = false;



        /// <summary>
        /// 일반 로그(Log) 표시용 <see cref="GUIStyle"/>입니다.
        /// </summary>
        private GUIStyle logStyle;



        /// <summary>
        /// 경고 로그(Warning) 표시용 <see cref="GUIStyle"/>입니다.
        /// </summary>
        private GUIStyle warningStyle;



        /// <summary>
        /// 에러 로그(Error) 표시용 <see cref="GUIStyle"/>입니다.
        /// </summary>
        private GUIStyle errorStyle;



        /// <summary>
        /// 기본 유니티 로그에서 메시지가 잘리기 시작하는 값입니다.
        /// </summary>
        private const int TruncateLength = 10000;



        ///======================================================================================================================================================



        /// <summary>
        /// 단일 로그를 나타내는 구조체입니다.
        /// </summary>
        private struct LogMessage
        {
            /// <summary>
            /// 로그의 실제 메시지 텍스트.
            /// </summary>
            public string Message;

            /// <summary>
            /// 로그 유형(Log, Warning, Error 등).
            /// </summary>
            public string Type;

            /// <summary>
            /// 로그가 기록된 시각("HH:mm:ss" 형식).
            /// </summary>
            public string Time;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// "판/Console 커스텀 뷰어" 메뉴에서 이 윈도우를 열 수 있도록 하는 메서드입니다.
        /// </summary>
        [MenuItem("판/Console 커스텀 뷰어")]
        public static void ShowWindow()
        {
            //. 에디터 윈도우 인스턴스 생성
            var window = GetWindow<ConsoleCustomViewerWindow>();

            //. 타이틀 아이콘 설정
            window.titleContent = new GUIContent(
                "Console 커스텀 뷰어",
                EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow").image
            );
        }



        ///======================================================================================================================================================
        


        private void OnEnable()
        {
            //. 스타일 초기화
            InitializeStyles();
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 로그 스타일(일반/경고/에러)에 대한 <see cref="GUIStyle"/>을 설정합니다.
        /// </summary>
        private void InitializeStyles()
        {
            //. 일반 로그 스타일
            logStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                wordWrap = true,
                //normal = { textColor = Color.white } // 필요하다면 색상 지정
            };

            //. 경고 스타일
            warningStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                wordWrap = true,
                //normal = { textColor = Color.yellow }
            };

            //. 에러 스타일
            errorStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                wordWrap = true,
                //normal = { textColor = Color.red }
            };
        }



        ///======================================================================================================================================================수집 로직



        /// <summary>
        /// 윈도우가 그려질 때마다 호출되며,
        /// 로그 필터 토글 및 로그 목록 스크롤, Copy 기능 등을 처리합니다.
        /// </summary>
        private void OnGUI()
        {
            //. 필요하다면 스타일 재초기화
            InitializeStyles();

            //? 상단 필터 토글들
            GUILayout.BeginHorizontal();
            showLog = GUILayout.Toggle(showLog, "Log");
            showWarning = GUILayout.Toggle(showWarning, "Warning");
            showError = GUILayout.Toggle(showError, "Error");
            showOnlyTruncatedMessages = GUILayout.Toggle(showOnlyTruncatedMessages, "Show Only Truncated Messages");

            //. Clear 버튼
            if (GUILayout.Button("Clear"))
            {
                // GlobalLogCollector의 로그 리스트 비우기
                GlobalLogCollector.ClearLogs();
            }

            //. Refresh 버튼 (사실상 필요 없음이지만, UI 유지용)
            if (GUILayout.Button("Refresh"))
            {
                // 전역 구독 방식이므로 이벤트 재등록은 필요 X
                // 굳이 기능을 넣고 싶다면, 이 버튼에서 아무것도 하지 않아도 됨
            }
            GUILayout.EndHorizontal();

            //? 로그 출력 스크롤 영역
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            //. 전역 로그 리스트 가져오기
            IReadOnlyList<GlobalLogCollector.LogMessage> allLogs = GlobalLogCollector.Logs;

            //. 필터 및 표시
            foreach (var logMessage in allLogs)
            {
                //? 긴 메시지 표시 옵션
                if (showOnlyTruncatedMessages && logMessage.Message.Length < TruncateLength)
                {
                    continue;
                }

                //? 로그 타입 필터
                if ((logMessage.Type == "Log" && !showLog) ||
                    (logMessage.Type == "Warning" && !showWarning) ||
                    (logMessage.Type == "Error" && !showError))
                {
                    continue;
                }

                //. UI 출력
                GUILayout.BeginHorizontal();
                GUILayout.Label($"[{logMessage.Time}] [{logMessage.Type}]:", GUILayout.Width(120));

                GUIStyle styleToUse = logStyle;
                if (logMessage.Type == "Warning")
                    styleToUse = warningStyle;
                else if (logMessage.Type == "Error")
                    styleToUse = errorStyle;

                GUILayout.Label(logMessage.Message, styleToUse);
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            //? Copy Selected 버튼 (화면에 보이는 로그만 복사)
            if (GUILayout.Button("Copy Selected"))
            {
                string selectedText = "";
                foreach (var logMessage in allLogs)
                {
                    // 필터 적용
                    if (showOnlyTruncatedMessages && logMessage.Message.Length < TruncateLength)
                        continue;

                    if ((logMessage.Type == "Log" && !showLog) ||
                        (logMessage.Type == "Warning" && !showWarning) ||
                        (logMessage.Type == "Error" && !showError))
                    {
                        continue;
                    }

                    selectedText += $"[{logMessage.Time}] [{logMessage.Type}]: {logMessage.Message}\n";
                }

                //. 클립보드에 저장
                EditorGUIUtility.systemCopyBuffer = selectedText;
            }
        }



        ///======================================================================================================================================================
    }
}