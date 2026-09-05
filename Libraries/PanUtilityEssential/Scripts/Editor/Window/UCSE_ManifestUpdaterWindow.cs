using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;



//? 로컬 GitPackage의 경로를 환경변수를 사용하여, 비정상적인 경로를 해당 환경변수 경로로 고치는 매니저가 있는 정도의 코드



public class ManifestUpdaterWindow : EditorWindow
{
    //? 잠금 상태 (true이면 수정 불가)
    private bool isVariableLocked = true;

    //? 환경변수 이름 (초기 기본값: "PAN_GIT_FOLDER")
    //? EditorPrefs 키 값 - 리로드 등 에디터가 다시 열려도 유지하기 위함
    private const string EnvVarNameEditorPrefsKey = "ManifestUpdater_EnvironmentVarName";
    private string environmentVariableName = "PAN_GIT_FOLDER";

    //. 현재 환경변수 경로 (읽어온 값)
    private string environmentVariableValue = "";

    [MenuItem("판/Mainfest 업데이터")]
    private static void ShowWindow()
    {
        //. "Mainfest 업데이터"라는 타이틀로 창 생성
        var window = GetWindow<ManifestUpdaterWindow>();
        window.titleContent = new GUIContent("Mainfest 업데이터", EditorGUIUtility.IconContent("d_BuildSettings.Standalone.Small").image);
        window.minSize = new Vector2(400, 300);

        //. 에디터 재시작 후에도 사용자가 변경해둔 환경변수 이름 유지
        window.environmentVariableName = EditorPrefs.GetString(EnvVarNameEditorPrefsKey, "PAN_GIT_FOLDER");
        window.environmentVariableValue = Environment.GetEnvironmentVariable(window.environmentVariableName) ?? "";

        window.Show();
    }

    // ! 스크립트가 리로드될 때마다 자동 실행 (프로젝트 처음 열 때, 코드 컴파일 후 등)
    //[DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        //. 리로드 시점에도 EditorPrefs에서 환경변수 이름을 읽어옵니다.
        string savedEnvVarName = EditorPrefs.GetString(EnvVarNameEditorPrefsKey, "PAN_GIT_FOLDER");
        UpdateManifest(savedEnvVarName);
    }

    //[InitializeOnLoadMethod]
    private static void OnScriptsReloaded2()
    {
        //. 리로드 시점에도 EditorPrefs에서 환경변수 이름을 읽어옵니다.
        string savedEnvVarName = EditorPrefs.GetString(EnvVarNameEditorPrefsKey, "PAN_GIT_FOLDER");
        UpdateManifest(savedEnvVarName);
    }

    private void OnEnable()
    {
        //. 창이 다시 열릴 때나, 재컴파일 후에도 사용자 지정값 유지
        environmentVariableName = EditorPrefs.GetString(EnvVarNameEditorPrefsKey, "PAN_GIT_FOLDER");
        environmentVariableValue = Environment.GetEnvironmentVariable(environmentVariableName) ?? "";
    }

    private void OnGUI()
    {
        //. 상단 라벨
        EditorGUILayout.LabelField("Mainfest 패키지 경로 자동/수동 업데이터", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        //. 자동 실행 설명 HelpBox
        EditorGUILayout.HelpBox(
            "프로젝트를 처음 열거나, 스크립트가 리로드될 때마다 " +
            "자동으로 manifest.json을 갱신합니다. (이제 아님 수동으로 해야함)",
            MessageType.Info
        );

        EditorGUILayout.Space();

        //? 환경변수 이름 + 잠금/해제 UI
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("환경변수 이름", GUILayout.Width(90));

        EditorGUI.BeginDisabledGroup(isVariableLocked);
        string newEnvName = EditorGUILayout.TextField(environmentVariableName);
        EditorGUI.EndDisabledGroup();

        //. "잠금 / 해제" 버튼
        isVariableLocked = !GUILayout.Toggle(!isVariableLocked, isVariableLocked ? "잠금 해제" : "잠금", "button", GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        //. 만약 텍스트필드 값이 바뀌었다면 환경변수 이름 교체 & EditorPrefs 반영
        if (!isVariableLocked && newEnvName != environmentVariableName)
        {
            environmentVariableName = newEnvName;
            EditorPrefs.SetString(EnvVarNameEditorPrefsKey, environmentVariableName);

            //. 변경된 이름으로 환경변수 다시 읽어오기
            environmentVariableValue = Environment.GetEnvironmentVariable(environmentVariableName) ?? "";
        }

        EditorGUILayout.Space();

        //. 현재 환경변수 값 표시 (굵은 폰트)
        EditorGUILayout.HelpBox("현재 환경변수 경로:", MessageType.None);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("", GUILayout.Width(5)); // 들여쓰기용
        GUIStyle boldStyle = new GUIStyle(EditorStyles.boldLabel);
        EditorGUILayout.LabelField(environmentVariableValue, boldStyle);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        //. 수동 실행 관련 안내
        EditorGUILayout.HelpBox(
            "아래 버튼을 누르면, 현재 환경변수(" + environmentVariableName + ")에 설정된 경로에 " +
            "맞춰서 manifest.json이 갱신됩니다.",
            MessageType.Info
        );

        //. 수동 갱신 버튼
        if (GUILayout.Button("수동으로 Manifest 갱신하기", GUILayout.Height(30)))
        {
            UpdateManifest(environmentVariableName);
        }
    }

    /// <summary>
    /// 실제 Manifest 갱신 로직
    /// </summary>
    private static void UpdateManifest(string envVarName)
    {
        //. 환경변수 값 읽기
        string envPath = Environment.GetEnvironmentVariable(envVarName);

        //! 환경변수가 비어있으면 중단
        if (string.IsNullOrEmpty(envPath))
        {
            Debug.LogWarning($"[ManifestUpdater] '{envVarName}' 환경변수가 설정되지 않았습니다.");
            return;
        }

        //. manifest.json 경로
        string projectPath = Application.dataPath;
        string manifestPath = Path.Combine(projectPath, "../Packages/manifest.json");

        if (!File.Exists(manifestPath))
        {
            Debug.LogError($"[ManifestUpdater] manifest.json 파일을 찾을 수 없습니다: {manifestPath}");
            return;
        }

        //. JSON 로드
        string jsonContent = File.ReadAllText(manifestPath);
        JObject manifestObj = JObject.Parse(jsonContent);
        JObject dependencies = (JObject)manifestObj["dependencies"];

        bool changesMade = false;

        //. dependencies 안의 각 패키지를 순회
        foreach (var property in dependencies.Properties())
        {
            string packageName = property.Name;
            string packagePath = property.Value.ToString();

            //? file:로 시작하면 로컬 패키지 경로
            if (packagePath.StartsWith("file:"))
            {
                //. "file:" 제거
                string localPath = packagePath.Substring("file:".Length);

                //. 경로가 절대경로인지 아닌지 확인 (또는 유효 디렉토리 여부)
                if (!Path.IsPathRooted(localPath) || !Directory.Exists(localPath))
                {
                    //. 새 경로: envPath + 기존 폴더 이름
                    string newPath = $"file:{Path.Combine(envPath, Path.GetFileName(localPath))}";

                    Debug.Log($"[ManifestUpdater] '{packageName}' 경로 변경: '{packagePath}' → '{newPath}'");
                    dependencies[packageName] = newPath;
                    changesMade = true;
                }
            }
        }

        //. 변경사항이 있으면 파일 반영
        if (changesMade)
        {
            string updatedJson = JsonConvert.SerializeObject(manifestObj, Formatting.Indented);
            File.WriteAllText(manifestPath, updatedJson);
            Debug.Log("[ManifestUpdater] manifest.json 업데이트 완료!");
        }
        else
        {
            //Debug.Log("[ManifestUpdater] 변경할 항목이 없습니다. 이미 모든 경로가 유효하거나 file: 경로가 없을 수 있습니다.");
        }
    }
}
