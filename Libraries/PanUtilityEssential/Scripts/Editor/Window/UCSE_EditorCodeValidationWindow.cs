using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;



//? Editor, EditorOnly 폴더 내부의 스크립트의 전처리기를 검사하고, 없다면 수정도 시켜주는 매니저가 있는 정도의 코드



namespace Pan.Util.Editors
{
    /// <summary>
    /// "Editor 코드 유효성 검사" 통합 윈도우입니다.
    /// 1) Editor API가 빌드에 포함되지 않도록 전처리기(#if UNITY_EDITOR) 검사를 수행
    /// 2) 'EditorOnly' 폴더 내 파일이 통째로 #if UNITY_EDITOR ~ #endif로 감싸져 있는지 확인 및 자동 수정
    /// </summary>
    public class EditorCodeValidationWindow : EditorWindow
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 검사 모드를 구분하기 위한 열거형
        /// </summary>
        private enum CodeValidationMode
        {
            EditorUsage,     // 'using UnityEditor' / 'Editor' 전처리기 검사
            EditorOnlyFolder // EditorOnly 폴더 전처리기 검사
        }



        //? 현재 탭(툴바)
        private CodeValidationMode currentMode = CodeValidationMode.EditorUsage;



        //? 공통 스크롤 위치
        private Vector2 scrollPos;



        ///======================================================================================================================================================



        //? 1) Editor Usage 검사 관련 데이터
        private string editorUsagePath = "Assets/";
        private static List<string> editorUsageResults = new List<string>();



        //? 2) EditorOnly 폴더 검사 관련 데이터
        private string editorOnlyPath = "Assets/";
        private static List<string> editorOnlyResults = new List<string>();



        ///======================================================================================================================================================



        /// <summary>
        /// 메뉴 아이템 등록 - "판/Editor 코드 유효성 검사"
        /// </summary>
        [MenuItem("판/Editor 코드 유효성 검사")]
        private static void ShowWindow()
        {
            var window = GetWindow<EditorCodeValidationWindow>();
            window.titleContent = new GUIContent("Editor 코드 유효성 검사", EditorGUIUtility.IconContent("d_Search Icon").image);
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 메인 GUI
        /// </summary>
        private void OnGUI()
        {
            // 상단 툴바
            DrawToolbar();

            GUILayout.Space(10);

            // 탭 모드별 UI
            switch (currentMode)
            {
                case CodeValidationMode.EditorUsage:
                DrawEditorUsageTab();
                break;
                case CodeValidationMode.EditorOnlyFolder:
                DrawEditorOnlyTab();
                break;
            }
        }



        /// <summary>
        /// 툴바(탭) 표시
        /// </summary>
        private void DrawToolbar()
        {
            string[] tabLabels = { "Editor Usage 검사", "EditorOnly 폴더 검사" };
            currentMode = (CodeValidationMode)GUILayout.Toolbar((int)currentMode, tabLabels);
        }



        ///======================================================================================================================================================



        //? (1) Editor Usage 검사 탭



        /// <summary>
        /// Editor API 사용을 전처리기 없이 쓰는 곳이 있는지 검사하는 탭
        /// </summary>
        private void DrawEditorUsageTab()
        {
            GUILayout.Label("Editor Usage 검사", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "이 탭은 'Editor' 폴더 밖에서 'using UnityEditor'나 'Editor' 키워드를\n" +
                "전처리기(#if UNITY_EDITOR) 없이 사용하는 코드를 찾아서 빌드 에러를 예방합니다.",
                MessageType.Info
            );

            GUILayout.Space(10);

            // 폴더 경로 입력
            EditorGUILayout.BeginHorizontal();
            editorUsagePath = EditorGUILayout.TextField("검사할 폴더 경로:", editorUsagePath);
            if (GUILayout.Button("탐색", GUILayout.Width(75)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("폴더 선택", editorUsagePath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        editorUsagePath = selectedPath.Substring(Application.dataPath.Length - 6);
                    }
                    else
                    {
                        editorUsagePath = selectedPath;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 분석 버튼
            if (GUILayout.Button("분석 시작", GUILayout.Height(25)))
            {
                AnalyzeEditorUsage(editorUsagePath);
            }

            // 결과 표시
            if (editorUsageResults.Count > 0)
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox("분석 결과:", MessageType.None);

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));
                foreach (var log in editorUsageResults)
                {
                    GUILayout.Label(log, new GUIStyle(EditorStyles.label) { richText = true });
                }
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("결과 지우기", GUILayout.Height(25)))
                {
                    editorUsageResults.Clear();
                }
            }
        }



        /// <summary>
        /// 실제 분석 로직 (Editor Usage)
        /// </summary>
        private void AnalyzeEditorUsage(string path)
        {
            editorUsageResults.Clear();

            // 1) "Editor" 폴더 제외한 .cs 파일 목록 수집
            var files = GetNonEditorCsFiles(path);

            // 2) 각 파일에서 전처리기(#if UNITY_EDITOR) 블록 밖의 "using UnityEditor" / "Editor" 키워드 검사
            foreach (var filePath in files)
            {
                AnalyzeEditorUsageCsFile(filePath);
            }
        }



        /// <summary>
        /// 주어진 폴더에서 Editor 폴더를 제외한 모든 .cs 파일을 재귀적으로 수집
        /// </summary>
        private List<string> GetNonEditorCsFiles(string rootPath)
        {
            var result = new List<string>();

            if (!IsEditorFolder(rootPath))
            {
                // 현재 폴더의 .cs
                result.AddRange(Directory.GetFiles(rootPath, "*.cs", SearchOption.TopDirectoryOnly));
            }

            // 하위 폴더들
            foreach (var dir in Directory.GetDirectories(rootPath, "*", SearchOption.TopDirectoryOnly))
            {
                if (!IsEditorFolder(dir))
                {
                    result.AddRange(GetNonEditorCsFiles(dir));
                }
            }
            return result;
        }



        /// <summary>
        /// 폴더 이름이 "Editor" 인지 확인
        /// </summary>
        private bool IsEditorFolder(string path)
        {
            string folderName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            return folderName.Equals("Editor", System.StringComparison.OrdinalIgnoreCase);
        }



        /// <summary>
        /// .cs 파일에서 전처리기 블록 밖의 Editor API 사용을 검사
        /// </summary>
        private void AnalyzeEditorUsageCsFile(string filePath)
        {
            foreach (var (line, lineNumber) in GetAnalyzableLines(filePath))
            {
                var trimmed = line.Trim();

                // using UnityEditor 감지
                if (trimmed.StartsWith("using UnityEditor"))
                {
                    string msg = $"<color=yellow>파일 '{filePath}'</color> <color=red>라인 {lineNumber}: 'using UnityEditor' 발견</color>";
                    editorUsageResults.Add(msg);
                    Debug.LogWarning(msg);
                }
                // Editor라는 단어 감지
                else if (trimmed.Contains("Editor"))
                {
                    string msg = $"<color=yellow>파일 '{filePath}'</color> <color=red>라인 {lineNumber}: 'Editor' 관련 사용</color>";
                    editorUsageResults.Add(msg);
                    Debug.LogWarning(msg);
                }
            }
        }



        ///======================================================================================================================================================



        //? (2) EditorOnly 폴더 검사 탭



        /// <summary>
        /// EditorOnly 폴더가 전처리기로 올바르게 감싸져 있는지 확인하는 탭
        /// </summary>
        private void DrawEditorOnlyTab()
        {
            GUILayout.Label("EditorOnly 폴더 검사", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "이 탭은 'EditorOnly' 폴더에 있는 스크립트가\n" +
                "#if UNITY_EDITOR ~ #endif 로 전체를 감싸고 있는지 검사하고,\n" +
                "감싸져 있지 않으면 자동으로 수정합니다.",
                MessageType.Info
            );

            GUILayout.Space(10);

            // 폴더 경로
            EditorGUILayout.BeginHorizontal();
            editorOnlyPath = EditorGUILayout.TextField("EditorOnly 폴더 경로:", editorOnlyPath);
            if (GUILayout.Button("탐색", GUILayout.Width(75)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("폴더 선택", editorOnlyPath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        editorOnlyPath = selectedPath.Substring(Application.dataPath.Length - 6);
                    }
                    else
                    {
                        editorOnlyPath = selectedPath;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 분석 버튼
            if (GUILayout.Button("분석", GUILayout.Height(25)))
            {
                AnalyzeEditorOnlyFolder(editorOnlyPath);
            }

            // 결과 표시
            if (editorOnlyResults.Count > 0)
            {
                GUILayout.Space(10);

                if (GUILayout.Button("고치기", GUILayout.Height(25)))
                {
                    FixEditorOnlyFiles();
                }

                if (GUILayout.Button("결과 지우기", GUILayout.Height(25)))
                {
                    editorOnlyResults.Clear();
                }

                GUILayout.Space(10);
                GUILayout.Label("분석 결과:", EditorStyles.boldLabel);

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));
                foreach (var log in editorOnlyResults)
                {
                    GUILayout.Label(log, new GUIStyle(EditorStyles.label) { richText = true });
                }
                EditorGUILayout.EndScrollView();
            }
        }



        /// <summary>
        /// EditorOnly 폴더 내 .cs 파일을 검사
        /// </summary>
        private void AnalyzeEditorOnlyFolder(string path)
        {
            editorOnlyResults.Clear();

            // EditorOnly 폴더 검색 (Assets, Packages 등)
            var targetDirs = Directory.GetDirectories(path, "EditorOnly", SearchOption.AllDirectories)
                .Concat(Directory.GetDirectories("Packages", "EditorOnly", SearchOption.AllDirectories))
                .Distinct();

            foreach (var dir in targetDirs)
            {
                var csFiles = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
                foreach (var csFile in csFiles)
                {
                    CheckEditorOnlyPreprocessor(csFile);
                }
            }
        }



        /// <summary>
        /// 파일 전체가 #if UNITY_EDITOR ~ #endif로 감싸져 있는지 확인
        /// </summary>
        private void CheckEditorOnlyPreprocessor(string filePath)
        {
            var lines = File.ReadAllLines(filePath, new UTF8Encoding(true));
            if (lines.Length < 2 ||
                !lines.First().Trim().Equals("#if UNITY_EDITOR") ||
                !lines.Last().Trim().Equals("#endif"))
            {
                string warningMessage = $"<color=yellow>파일 '{filePath}'</color> <color=red>전처리기가 올바르지 않습니다.</color>";
                editorOnlyResults.Add(warningMessage);
                Debug.LogWarning(warningMessage);
            }
            else
            {
                string successMessage = $"<color=yellow>파일 '{filePath}'</color> <color=green>올바르게 전처리기가 감싸져 있습니다.</color>";
                editorOnlyResults.Add(successMessage);
                Debug.Log(successMessage);
            }
        }



        /// <summary>
        /// 전처리기가 올바르지 않은 파일에 #if UNITY_EDITOR ~ #endif를 추가
        /// </summary>
        private void FixEditorOnlyFiles()
        {
            var wrongFiles = editorOnlyResults
                .Where(log => log.Contains("전처리기가 올바르지 않습니다."))
                .Select(log => log.Split('\'')[1])
                .Distinct()
                .ToList();

            foreach (var filePath in wrongFiles)
            {
                var lines = File.ReadAllLines(filePath, new UTF8Encoding(true)).ToList();

                if (lines.Count == 0 || !lines.First().Trim().Equals("#if UNITY_EDITOR"))
                {
                    lines.Insert(0, "#if UNITY_EDITOR");
                }

                if (lines.Count == 0 || !lines.Last().Trim().Equals("#endif"))
                {
                    lines.Add("#endif");
                }

                File.WriteAllLines(filePath, lines, new UTF8Encoding(true));
                string fixMessage = $"<color=yellow>파일 '{filePath}'</color> <color=blue>수정되었습니다.</color>";
                Debug.Log(fixMessage);
            }

            // 수정 후 다시 분석
            editorOnlyResults.Clear();
            AnalyzeEditorOnlyFolder(editorOnlyPath);
        }



        ///======================================================================================================================================================



        //? 공통: 전처리기 블록 밖의 라인을 추출


        /// <summary>
        /// 전처리기(#if UNITY_EDITOR) 안에 있는 라인을 제외하고, 실제 빌드 대상인 라인만 반환
        /// 블록 주석(/* */)와 라인 주석(//)도 무시
        /// </summary>
        private IEnumerable<(string line, int lineNumber)> GetAnalyzableLines(string filePath)
        {
            var lines = File.ReadAllLines(filePath, new UTF8Encoding(true));

            bool inCommentBlock = false;
            int unityEditorBlockDepth = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                // 블록 주석 시작
                if (trimmed.StartsWith("/*"))
                {
                    inCommentBlock = true;
                }
                // 블록 주석 종료
                if (trimmed.EndsWith("*/"))
                {
                    inCommentBlock = false;
                    continue;
                }
                // 블록 주석 내
                if (inCommentBlock)
                    continue;

                // 라인 주석
                if (trimmed.StartsWith("//"))
                    continue;

                // 전처리기 시작(#if, #elif)
                if (trimmed.StartsWith("#if") || trimmed.StartsWith("#elif"))
                {
                    if (trimmed.Contains("UNITY_EDITOR"))
                    {
                        unityEditorBlockDepth++;
                    }
                    continue;
                }
                // 전처리기 종료(#endif)
                else if (trimmed.StartsWith("#endif"))
                {
                    if (unityEditorBlockDepth > 0)
                        unityEditorBlockDepth--;
                    continue;
                }

                // 전처리기 블록 밖
                if (unityEditorBlockDepth == 0)
                {
                    yield return (line, i + 1);
                }
            }
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 스크립트 리로드 시, 각 탭의 결과 리스트 초기화
        /// </summary>
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            //var window = GetWindow<EditorCodeValidationWindow>(false, null, false);
            //if (window != null)
            //{
            //    window.editorUsageResults.Clear();
            //    window.editorOnlyResults.Clear();
            //}

            editorUsageResults.Clear();
            editorOnlyResults.Clear();
        }



        ///======================================================================================================================================================
    }
}
