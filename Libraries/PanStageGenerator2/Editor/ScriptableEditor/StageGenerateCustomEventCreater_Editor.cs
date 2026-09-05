using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.Text;
using Pan.Util;
using System.IO;
using System.Reflection;
using System.Linq;
using DG.DemiEditor;
using System.Xml.Linq;
using Cysharp.Threading.Tasks;
using System.Text.RegularExpressions;
using Pan.Util.Editors;



namespace Pan.StageGenerators.Editor
{
    [CustomEditor(typeof(StageGenerateCustomEventCreater), true)]
    public class StageGenerateCustomEventCreater_Editor : EditorExpand_InspectorGUI<StageGenerateCustomEventCreater>
    {
        ///======================================================================================================================================================



        public StageGenerateCustomEventCreater_Editor()
        {
            SU_Collection_Types.CreateTypesAssignableTo<BaseStageGenerateCustomEvent>(SU_Collection_Types.TypeSearch.ConcreteClasses, out AllStageGenerateCustomEventTypes);
            BaseStageGenerateCustomEventTypes = GetDirectSubclasses(typeof(BaseStageGenerateCustomEvent));
        }



        ///======================================================================================================================================================



        private readonly Type[] AllStageGenerateCustomEventTypes;
        private readonly Type[] BaseStageGenerateCustomEventTypes;



        private readonly StringBuilder stringBuilder_CustomEvents = new StringBuilder();



        ///======================================================================================================================================================



        protected override void OnInspectorGUI_Current()
        {
            base.OnInspectorGUI_Current();


            SU_CustomEditor.AutoLabelField_Head($"가용 가능한 커스텀 이벤트(<color=#2ecc71>{AllStageGenerateCustomEventTypes.Length}개</color>)", SU_CustomEditor.LabelHeadType.H1, () =>
            {
                stringBuilder_CustomEvents.Clear();

                foreach (var item in AllStageGenerateCustomEventTypes)
                {
                    stringBuilder_CustomEvents.AppendLine($"<b><color=white>{item.Name.ToString()}</color></b>\n<i>({item.BaseType.Name})</i> ,");
                }

                if (AllStageGenerateCustomEventTypes.Length != 0)
                {
                    stringBuilder_CustomEvents.Remove(stringBuilder_CustomEvents.Length - 3, 3);
                }

                SU_CustomEditor.LabelField_TextAutoWidthHeight(stringBuilder_CustomEvents.ToString(true));
            });


            SU_CustomEditor.AutoLabelField_Head($"Script 생성하기", SU_CustomEditor.LabelHeadType.H1, () =>
            {
                SU_CustomEditor.LabelField_TextAutoWidthHeight($"현재 폴더 경로: <color=#f7da64>{Target.CreateFolderPath}</color>");


                SU_CustomEditor.HorizontalGUI(() =>
                {
                    SU_CustomEditor.RenderField_Property_Path(this, nameof(Target.CreateFolderPath), "생성 폴더 경로:", "");
                    SU_CustomEditor.Render_Button(this, "탐색", () =>
                    {
                        string selectedPath = EditorUtility.OpenFolderPanel("폴더 선택", Target.CreateFolderPath, "");
                        if (!string.IsNullOrEmpty(selectedPath))
                        {
                            if (selectedPath.StartsWith(Application.dataPath))
                            {
                                Target.CreateFolderPath = selectedPath.Substring(Application.dataPath.Length - 6); // "Assets/" 경로 상대적으로 만들기
                            }
                            else
                            {
                                Target.CreateFolderPath = selectedPath; // "Assets/" 외부 경로 지원
                            }
                        }
                    });
                });


                SU_CustomEditor.RenderField_Property_Path(this, nameof(Target.CreateFileName), "파일 이름:", "");


                EditorGUILayout.Space();


                foreach (var type in BaseStageGenerateCustomEventTypes)
                {
                    string typeName = type.Name;
                    string typeSimpleName = typeName.Replace("StageGenerateCustomEventBase_", "");

                    SU_CustomEditor.Render_ButtonWithStyle(this, $"스크립트 생성: {typeSimpleName}", "", () =>
                    {
                        if (Target.CreateFileName.IsNullOrEmpty()) { Debug.LogWarning("파일 이름을 지정해주세요"); }
                        else
                        {
                            var file = Path.Combine(Target.CreateFolderPath, Target.CreateFileName + ".cs");

                            if (File.Exists(file))
                            {
                                // 파일이 이미 존재할 때, 확인/취소 창 표시
                                bool overwrite = EditorUtility.DisplayDialog(
                                    "스크립트 덮어쓰기",
                                    $"파일이 이미 존재합니다:\n{file}\n\n덮어씌우시겠습니까?",
                                    "확인",
                                    "취소"
                                );

                                if (overwrite)
                                {
                                    File.Delete(file);
                                    // 덮어씌우기 확인 시 스크립트 생성
                                    //AutoScriptInheritanceGenerator.GenerateScript(type, FindScriptPathOfType(type), Target.CreateFolderPath, Target.CreateFileName);
                                    _AutoScriptInheritanceGenerator.GenerateScript(type, Target.CreateFolderPath, Target.CreateFileName);
                                    Debug.Log($"스크립트가 덮어씌워졌습니다: {file}");
                                }
                                else
                                {
                                    // 취소 시 아무 작업도 하지 않음
                                    //Debug.Log("스크립트 생성이 취소되었습니다.");
                                }
                            }
                            else
                            {

                                //AutoScriptInheritanceGenerator.GenerateScript(type, FindScriptPathOfType(type), Target.CreateFolderPath, Target.CreateFileName);
                                _AutoScriptInheritanceGenerator.GenerateScript(type, Target.CreateFolderPath, Target.CreateFileName);
                                Debug.Log($"스크립트가 생성되었습니다: {file}");
                            }

                        }

                    }, SU_ColorPresetRGB.Green_Emerald(), null);
                }
            });
        }



        ///======================================================================================================================================================



        public static class _AutoScriptInheritanceGenerator
        {
            // C# 기본 타입 대비 별칭 매핑 딕셔너리
            private static readonly Dictionary<Type, string> s_TypeAliasMap = new Dictionary<Type, string>
    {
        { typeof(bool), "bool" },
        { typeof(int), "int" },
        { typeof(float), "float" },
        { typeof(double), "double" },
        { typeof(string), "string" },
        { typeof(object), "object" },
        { typeof(void), "void" },
        { typeof(decimal), "decimal" },
        { typeof(long), "long" },
        { typeof(short), "short" },
        { typeof(byte), "byte" },
        { typeof(char), "char" },
    };

            // 줄 끝을 CRLF("\r\n")로 강제 정규화하기 위한 상수
            private const string NL = "\r\n";

            public const string CREATE_ASSET_MENU_NAME = "CreateAssetMenuInfo.STAGEGEN_CUSTOMEVENTFOLDER";

            /// <summary>
            /// 예: 특정 추상 클래스 T 를 상속받는 새 파일 생성
            /// </summary>
            public static void GenerateScript<T>(string filePath, string scriptName) where T : BaseStageGenerateCustomEvent
            {
                GenerateScript(typeof(T), filePath, scriptName);
            }

            /// <summary>
            /// 추상 클래스 baseType 을 상속하는 클래스를 filePath 경로에 scriptName.cs 로 생성
            /// </summary>
            public static void GenerateScript(Type baseType, string filePath, string scriptName)
            {
                if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(scriptName))
                {
                    Debug.LogError("파일 경로와 스크립트 이름은 비워둘 수 없습니다.");
                    return;
                }

                if (!baseType.IsAbstract)
                {
                    Debug.LogError($"제공된 타입 {baseType.Name}은(는) 추상 클래스가 아닙니다.");
                    return;
                }

                string namespaceName = baseType.Namespace ?? "Pan.StageGenerators";
                string scriptPath = Path.Combine(filePath, scriptName + ".cs");

                // 최종 코드 생성
                string scriptContent = BuildScriptContent(baseType, namespaceName, scriptName);

                // 저장 및 갱신
                SaveScriptToFile(filePath, scriptName, scriptContent);
                AssetDatabase.Refresh();

                Debug.Log($"클래스 '{scriptName}'가 '{scriptPath}' 에 성공적으로 생성되었습니다.");
            }

            /// <summary>
            /// 최종 스크립트 코드(문자열)를 생성
            /// </summary>
            private static string BuildScriptContent(Type baseType, string namespaceName, string className)
            {
                // using/namespace 헤더
                string header =
        $@"using UnityEngine;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
{NL}namespace {namespaceName}{NL}{{{NL}";

                // class 선언부
                string classDefinition =
        $@"    [CreateAssetMenu(fileName = nameof({className}), menuName = {CREATE_ASSET_MENU_NAME} + ""/{className}"")]
    public class {className} : {baseType.Name}
    {{{NL}";

                // 추상 멤버(메서드/프로퍼티/이벤트) 찾기
                var abstractMethods = baseType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(m => m.IsAbstract && !IsAccessorMethod(m));

                var abstractProperties = baseType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(p => (p.GetGetMethod(true)?.IsAbstract ?? false) || (p.GetSetMethod(true)?.IsAbstract ?? false));

                var abstractEvents = baseType.GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(e => (e.GetAddMethod(true)?.IsAbstract ?? false) || (e.GetRemoveMethod(true)?.IsAbstract ?? false));

                // Stub 생성
                var methodStubs = abstractMethods.Select(GenerateMethodStub);
                var propertyStubs = abstractProperties.Select(p => GeneratePropertyStub(p, namespaceName));
                var eventStubs = abstractEvents.Select(e => GenerateEventStub(e, namespaceName));

                // 모두 합침
                string allMemberStubs = string.Join(NL + NL,
                    methodStubs.Concat(propertyStubs).Concat(eventStubs));

                // class/namespace 마무리
                string footer = $@"    }}{NL}}}";

                return header + classDefinition + allMemberStubs + NL + footer;
            }

            /// <summary>
            /// 추상 메서드 -> override 스텁 생성
            /// </summary>
            private static string GenerateMethodStub(MethodInfo method)
            {
                // 반환 타입 문자열
                string returnType = GetTypeName(method.ReturnType, method.DeclaringType.Namespace);

                // 파라미터 문자열
                string parameters = string.Join(", ", method.GetParameters()
                    .Select(p => $"{GetTypeName(p.ParameterType, method.DeclaringType.Namespace)} {p.Name}"));

                // (1) async 여부 판단
                //  => UniTask 또는 UniTask<> 이면 async
                bool isUniTask =
                    (method.ReturnType == typeof(UniTask)) ||
                    (method.ReturnType.IsGenericType &&
                     method.ReturnType.GetGenericTypeDefinition() == typeof(UniTask<>));

                // (2) bool(또는 UniTask<bool>) 여부 판단
                bool isBool =
                    (method.ReturnType == typeof(bool)) ||
                    (
                        method.ReturnType.IsGenericType &&
                        method.ReturnType.GetGenericTypeDefinition() == typeof(UniTask<>) &&
                        method.ReturnType.GetGenericArguments()[0] == typeof(bool)
                    );

                // (3) void(또는 반환 없는 UniTask) 여부 판단
                bool isVoid = (method.ReturnType == typeof(void));
                bool isUniTaskNoGeneric = (method.ReturnType == typeof(UniTask)); // 즉 UniTask 자체

                // 이제 메서드 본문을 결정
                string methodBody;
                if (isBool)
                {
                    // bool / UniTask<bool> => return false;
                    methodBody = "            return false;";
                }
                else if (isVoid || isUniTaskNoGeneric)
                {
                    // void / UniTask => 본문 비움
                    methodBody = "";
                }
                else
                {
                    // 그 외(int, float, UniTask<int> 등) => throw new NotImplementedException();
                    methodBody = "            throw new NotImplementedException();";
                }

                // async 키워드 붙일지 여부
                string asyncKeyword = isUniTask ? "async " : "";

                return
            $@"        public override {asyncKeyword}{returnType} {method.Name}({parameters})
        {{
{methodBody}
        }}";
            }



            /// <summary>
            /// 추상 프로퍼티 -> override 스텁 생성
            /// </summary>
            private static string GeneratePropertyStub(PropertyInfo property, string currentNamespace)
            {
                bool isIndexer = property.GetIndexParameters().Length > 0;
                string propName = isIndexer
                    ? $"this[{string.Join(", ", property.GetIndexParameters().Select(p => $"{GetTypeName(p.ParameterType, currentNamespace)} {p.Name}"))}]"
                    : property.Name;

                string returnType = GetTypeName(property.PropertyType, currentNamespace);
                bool hasGetter = property.GetGetMethod(true) != null;
                bool hasSetter = property.GetSetMethod(true) != null;

                string getter = hasGetter ?
        $@"            get
            {{
                throw new NotImplementedException();
            }}" : "";

                string setter = hasSetter ?
        $@"            set
            {{
                throw new NotImplementedException();
            }}" : "";

                return
        $@"        public override {returnType} {propName}
        {{
{(string.IsNullOrEmpty(getter) ? "" : getter + NL)}
{(string.IsNullOrEmpty(setter) ? "" : setter + NL)}        }}";
            }

            /// <summary>
            /// 추상 이벤트 -> override event 스텁 생성
            /// </summary>
            private static string GenerateEventStub(EventInfo eventInfo, string currentNamespace)
            {
                string eventHandlerType = GetTypeName(eventInfo.EventHandlerType, currentNamespace);

                return
        $@"        public override event {eventHandlerType} {eventInfo.Name}
        {{
            add {{ throw new NotImplementedException(); }}
            remove {{ throw new NotImplementedException(); }}
        }}";
            }

            /// <summary>
            /// C# Type 을 해당 namespace 컨텍스트에서 적절히 string 변환
            /// </summary>
            private static string GetTypeName(Type type, string currentNamespace)
            {
                if (type.IsArray)
                {
                    int rank = type.GetArrayRank(); // 배열 차원
                    string arrayDims = rank > 1 ? "[" + new string(',', rank - 1) + "]" : "[]";
                    return GetTypeName(type.GetElementType(), currentNamespace) + arrayDims;
                }

                if (type.IsGenericType)
                {
                    string genericName = type.GetGenericTypeDefinition().Name.Split('`')[0];
                    // 흔히 쓰이는 네임스페이스면 prefix 생략
                    string nsPrefix = (type.Namespace == currentNamespace
                                       || type.Namespace == "System.Collections.Generic"
                                       || type.Namespace == "Cysharp.Threading.Tasks")
                                     ? ""
                                     : (type.Namespace + ".");

                    string genericArgs = string.Join(", ",
                        type.GetGenericArguments().Select(t => GetTypeName(t, currentNamespace)));
                    return $"{nsPrefix}{genericName}<{genericArgs}>";
                }

                // bool, int, float 등 별칭
                if (s_TypeAliasMap.TryGetValue(type, out string alias))
                {
                    return alias;
                }

                // Nested 타입인지 (중첩 클래스)
                string typeName = (type.DeclaringType != null)
                    ? $"{type.DeclaringType.Name}.{type.Name}"
                    : type.Name;

                // 같은 네임스페이스 or 자주 쓰이는 곳이면 prefix 생략
                if (type.Namespace == currentNamespace
                    || type.Namespace == "System.Collections.Generic"
                    || type.Namespace == "Cysharp.Threading.Tasks")
                {
                    return typeName;
                }
                return $"{type.Namespace}.{typeName}";
            }

            /// <summary>
            /// 메서드가 get/set 등 프로퍼티 접근자 용인지
            /// </summary>
            private static bool IsAccessorMethod(MethodInfo methodInfo)
            {
                return methodInfo.IsSpecialName;
            }

            /// <summary>
            /// 스크립트 파일로 저장 (CRLF로 통일)
            /// </summary>
            private static void SaveScriptToFile(string folderPath, string scriptName, string scriptContent)
            {
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string fullPath = Path.Combine(folderPath, scriptName + ".cs");

                // CRLF로 통일
                string normalized = scriptContent.Replace("\r\n", "\n").Replace("\n", NL);
                File.WriteAllText(fullPath, normalized);
            }
        }



        ///======================================================================================================================================================



        /// <summary>
        /// Unity 프로젝트 내에서, 주어진 Type이 정의된 .cs 파일 경로를 찾는다.
        /// 여러 개가 있을 경우, 첫 번째만 반환한다. (없으면 null 반환)
        /// </summary>
        public static string FindScriptPathOfType(Type baseType)
        {
#if UNITY_EDITOR
            // 1) baseType.Name 으로 MonoScript 검색
            //    (이름이 동일한 클래스가 여러 개 있을 수 있으므로, 100% 신뢰하긴 어렵고 필터링 필요)
            string[] guids = AssetDatabase.FindAssets($"{baseType.Name} t:MonoScript");
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                // 2) 해당 MonoScript 로부터 Class 가져오기
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
                if (script == null) continue; // 혹시 로드 실패 시 패스

                Type scriptClass = script.GetClass();
                if (scriptClass == baseType)
                {
                    // 정확히 baseType과 일치하면 찾은 것
                    return assetPath;
                }
            }
#endif

            // 에디터가 아니거나, 찾지 못했으면 null
            return null;
        }



        /// <summary>
        /// (예시) baseType이 Abstract가 아니면서 ScriptableObject를 상속한다면,
        /// MonoScript.FromScriptableObject() 로 간단히 구할 수 있다.
        /// </summary>
        public static string FindScriptPath_ScriptableObject(Type scriptableType)
        {
#if UNITY_EDITOR
            if (scriptableType.IsSubclassOf(typeof(ScriptableObject)) && !scriptableType.IsAbstract)
            {
                ScriptableObject temp = ScriptableObject.CreateInstance(scriptableType);
                if (temp != null)
                {
                    MonoScript ms = MonoScript.FromScriptableObject(temp);
                    string path = AssetDatabase.GetAssetPath(ms);
                    UnityEngine.Object.DestroyImmediate(temp); // 임시 객체 정리
                    return path;
                }
            }
#endif
            return null;
        }



        /// <summary>
        /// (예시) baseType이 Abstract가 아니면서 MonoBehaviour를 상속한다면,
        /// MonoScript.FromMonoBehaviour() 로 간단히 구할 수 있다.
        /// </summary>
        public static string FindScriptPath_MonoBehaviour(Type monoType)
        {
#if UNITY_EDITOR
            if (monoType.IsSubclassOf(typeof(MonoBehaviour)) && !monoType.IsAbstract)
            {
                var go = new GameObject("__temp");
                var comp = go.AddComponent(monoType);
                MonoScript ms = MonoScript.FromMonoBehaviour(comp as MonoBehaviour);
                string path = AssetDatabase.GetAssetPath(ms);
                GameObject.DestroyImmediate(go);
                return path;
            }
#endif
            return null;
        }



        /// <summary>
        /// 추상 클래스에서 상속된 스크립트(ScriptableObject) 코드를 자동 생성하는 유틸리티.
        /// </summary>
        public static class AutoScriptInheritanceGenerator
        {
            // C# 기본 타입 대비 별칭 매핑 딕셔너리
            private static readonly Dictionary<Type, string> s_TypeAliasMap = new Dictionary<Type, string>
        {
            { typeof(bool), "bool" },
            { typeof(int), "int" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(string), "string" },
            { typeof(object), "object" },
            { typeof(void), "void" },
            { typeof(decimal), "decimal" },
            { typeof(long), "long" },
            { typeof(short), "short" },
            { typeof(byte), "byte" },
            { typeof(char), "char" },
        };

            // 줄 끝을 CRLF("\r\n")로 강제 정규화하기 위한 상수
            private const string NL = "\r\n";

            public const string CREATE_ASSET_MENU_NAME = "CreateAssetMenuInfo.STAGEGEN_CUSTOMEVENTFOLDER";


            /// <summary>
            /// (예시) 제네릭 버전: T 타입 추상 클래스를 기반으로 파일을 생성
            /// </summary>
            public static void GenerateScript<T>(string originalFilePath, string destinationFolder, string scriptName)
                where T : BaseStageGenerateCustomEvent
            {
                GenerateScript(typeof(T), originalFilePath, destinationFolder, scriptName);
            }


            /// <summary>
            /// 추상 클래스( baseType )를 상속받는 새 클래스를 destinationFolder 경로에 생성.
            /// originalFilePath는 baseType 이 정의된 '원본 .cs 파일 경로'이다.
            /// </summary>
            public static void GenerateScript(Type baseType, string originalFilePath, string destinationFolder, string scriptName)
            {
                if (string.IsNullOrEmpty(originalFilePath))
                {
                    Debug.LogError("원본 .cs 파일 경로(originalFilePath)가 비어 있습니다. 해당 추상 클래스가 정의된 파일 경로를 지정해야 합니다.");
                    return;
                }

                if (string.IsNullOrEmpty(destinationFolder) || string.IsNullOrEmpty(scriptName))
                {
                    Debug.LogError("출력 폴더(destinationFolder)와 스크립트 이름(scriptName)은 비워둘 수 없습니다.");
                    return;
                }

                if (!baseType.IsAbstract)
                {
                    Debug.LogError($"제공된 타입 {baseType.Name}은(는) 추상 클래스가 아닙니다.");
                    return;
                }

                // nameSpace: 추상 클래스 baseType 의 Namespace 또는 기본값
                string namespaceName = baseType.Namespace ?? "Pan.StageGenerators";

                // 생성될 파일의 전체 경로
                string scriptPath = Path.Combine(destinationFolder, scriptName + ".cs");

                // 실제로 새 파일 내용을 빌드하되, 원본 경로를 넘겨준다
                string scriptContent = BuildScriptContent(baseType, namespaceName, scriptName, originalFilePath);

                // 파일로 저장
                SaveScriptToFile(destinationFolder, scriptName, scriptContent);

                // 에셋 리프레시
                AssetDatabase.Refresh();

                Debug.Log($"클래스 '{scriptName}'가 '{scriptPath}' 에 성공적으로 생성되었습니다.");
            }


            /// <summary>
            /// 최종 스크립트(.cs) 내용을 빌드해서 문자열로 반환
            /// </summary>
            private static string BuildScriptContent(Type baseType, string namespaceName, string className, string originalFilePath)
            {
                // using/namespace 선언부
                string header =
    $@"using UnityEngine;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
{NL}namespace {namespaceName}{NL}{{{NL}";

                // class 선언부
                string classDefinition =
    $@"    [CreateAssetMenu(fileName = nameof({className}), menuName = {CREATE_ASSET_MENU_NAME} + ""/{className}"")]
    public class {className} : {baseType.Name}
    {{{NL}";

                // 추상 메서드, 추상 프로퍼티, 추상 이벤트 분리 수집
                var abstractMethods = baseType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(m => m.IsAbstract && !IsAccessorMethod(m));

                var abstractProperties = baseType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(p => (p.GetGetMethod(true)?.IsAbstract ?? false) || (p.GetSetMethod(true)?.IsAbstract ?? false));

                var abstractEvents = baseType.GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(e => (e.GetAddMethod(true)?.IsAbstract ?? false) || (e.GetRemoveMethod(true)?.IsAbstract ?? false));

                // 각각의 스텁 코드(메서드, 프로퍼티, 이벤트) 생성
                var methodStubs = abstractMethods.Select(m => GenerateMethodStub(m, originalFilePath, namespaceName));
                var propertyStubs = abstractProperties.Select(p => GeneratePropertyStub(p, namespaceName));
                var eventStubs = abstractEvents.Select(e => GenerateEventStub(e, namespaceName));

                // 모든 멤버 스텁을 합침
                string allMemberStubs = string.Join(NL + NL, methodStubs.Concat(propertyStubs).Concat(eventStubs));

                // class 마무리
                string footer =
    $@"    }}{NL}}}";

                return header + classDefinition + allMemberStubs + NL + footer;
            }


            /// <summary>
            /// 추상 메서드 스텁(override ...) 코드를 생성
            /// </summary>
            private static string GenerateMethodStub(MethodInfo method, string originalFilePath, string currentNamespace)
            {
                string returnType = GetTypeName(method.ReturnType, currentNamespace);
                string parameters = string.Join(", ",
                    method.GetParameters().Select(p => $"{GetTypeName(p.ParameterType, currentNamespace)} {p.Name}"));

                // 원본 파일에서 XML 주석 읽기
                string methodSummary = ExtractXmlSummaryFromSourceFile(originalFilePath, method);

                // 반환 타입 필요 여부
                bool isReturnTypeNeeded = method.ReturnType != typeof(void);

                // 반환 타입이 bool 또는 UniTask<bool>인지 확인
                bool isBoolReturnType =
                    method.ReturnType == typeof(bool) ||
                    (method.ReturnType.IsGenericType &&
                     method.ReturnType.GetGenericTypeDefinition() == typeof(UniTask<>) &&
                     method.ReturnType.GetGenericArguments()[0] == typeof(bool));

                // 메서드 내부 주석
                string internalComment = string.IsNullOrWhiteSpace(methodSummary)
                    ? string.Empty
                    : $"        // {methodSummary}";

                // 메서드 본문 생성
                string methodBody;
                if (isReturnTypeNeeded)
                {
                    if (isBoolReturnType)
                    {
                        // bool or UniTask<bool> 의 경우 임시로 false 반환
                        methodBody =
    $@"{internalComment}
            return false;";
                    }
                    else
                    {
                        // 그 외에는 throw new NotImplementedException()
                        methodBody =
    $@"{internalComment}
            throw new NotImplementedException();";
                    }
                }
                else
                {
                    // void 반환
                    methodBody = string.IsNullOrWhiteSpace(internalComment)
                        ? string.Empty
                        : internalComment;
                }

                // 최종 오버라이드 메서드 코드
                return
    $@"        /// <summary>
        /// {methodSummary}
        /// </summary>
        public override {returnType} {method.Name}({parameters})
        {{
{methodBody}
        }}";
            }



            /// <summary>
            /// 원본 파일에서 MethodInfo에 해당하는 추상 메서드의 XML 주석을 찾아서 반환
            /// </summary>
            private static string ExtractXmlSummaryFromSourceFile(string filePath, MethodInfo methodInfo)
            {
                if (!File.Exists(filePath))
                    return string.Empty;

                var lines = File.ReadAllLines(filePath);

                // 1) 메서드 선언 라인 찾기
                int methodLineIndex = FindMethodLineByNameAndParamCount(lines, methodInfo);
                if (methodLineIndex == -1)
                    return string.Empty;

                // 2) 위로 올라가면서 "///" 로 시작하는 라인만 모으되,
                //    중간에 { 나 빈 줄, 다른 코드 만나면 중지.
                var summaryLines = new List<string>();
                for (int i = methodLineIndex - 1; i >= 0; i--)
                {
                    string trimmed = lines[i].Trim();

                    // (a) 빈 줄이면 멈춘다.
                    if (string.IsNullOrEmpty(trimmed))
                        break;

                    // (b) 중괄호 {  만났으면 본문 시작으로 간주 -> 멈춘다.
                    //     "}" 를 만나도 확실히 멈추는 것이 좋음 (중괄호 닫힘 후는 다른 영역)
                    if (trimmed.EndsWith("{") || trimmed.EndsWith("}"))
                        break;

                    // (c) 주석이 아닌 줄 만나면 멈춘다.
                    if (!trimmed.StartsWith("///"))
                        break;

                    // (d) XML 태그가 전혀 없는 줄(예: ///============== )은 스킵할 수도 있음
                    //     필요하다면 아래 정규식 조건을 빼도 된다.
                    if (!Regex.IsMatch(trimmed, @"///\s*\<(summary|param|returns|remarks|typeparam)\b"))
                        continue;

                    // "///" 제거 후 맨 앞에 삽입(역순)
                    summaryLines.Insert(0, trimmed.Substring(3).Trim());
                }

                // 3) 최종 연결
                return summaryLines.Count > 0 ? string.Join(Environment.NewLine, summaryLines) : string.Empty;
            }



            /// <summary>
            /// 메서드 이름/파라미터 개수만 체크해서 "후보 라인"을 찾는다.
            /// </summary>
            private static int FindMethodLineByNameAndParamCount(string[] lines, MethodInfo methodInfo)
            {
                string methodName = methodInfo.Name;
                int paramCount = methodInfo.GetParameters().Length;

                for (int i = 0; i < lines.Length; i++)
                {
                    string trimmed = lines[i].Trim();

                    // 메서드 이름이 들어있는지 체크
                    if (trimmed.Contains(methodName))
                    {
                        // 정규식으로 괄호 안 파라미터 개수 추정
                        var match = Regex.Match(trimmed, $@"\b{methodName}\s*\(([^)]*)\)");
                        if (match.Success)
                        {
                            string insideParenthesis = match.Groups[1].Value.Trim();

                            // 파라미터가 없는 경우
                            if (string.IsNullOrEmpty(insideParenthesis))
                            {
                                if (paramCount == 0)
                                    return i;
                            }
                            else
                            {
                                // 쉼표(,) 개수 + 1 == paramCount
                                int actualParamCount = insideParenthesis.Split(',').Length;
                                if (actualParamCount == paramCount)
                                    return i;
                            }
                        }
                    }
                }

                return -1; // 찾지 못함
            }


            /// <summary>
            /// 추상 프로퍼티 스텁(override ...) 코드를 생성
            /// </summary>
            private static string GeneratePropertyStub(PropertyInfo property, string currentNamespace)
            {
                bool isIndexer = property.GetIndexParameters().Length > 0;
                string propName = isIndexer
                    ? $"this[{string.Join(", ", property.GetIndexParameters().Select(p => $"{GetTypeName(p.ParameterType, currentNamespace)} {p.Name}"))}]"
                    : property.Name;

                string returnType = GetTypeName(property.PropertyType, currentNamespace);
                bool hasGetter = property.GetGetMethod(true) != null;
                bool hasSetter = property.GetSetMethod(true) != null;

                // get / set 블록은 한 단계만 들여쓰기
                string getter = hasGetter ?
    $@"            get
            {{
                throw new NotImplementedException();
            }}" : "";

                string setter = hasSetter ?
    $@"            set
            {{
                throw new NotImplementedException();
            }}" : "";

                return
    $@"        public override {returnType} {propName}
        {{
{(string.IsNullOrEmpty(getter) ? "" : getter + NL)}
{(string.IsNullOrEmpty(setter) ? "" : setter + NL)}        }}";
            }


            /// <summary>
            /// 추상 이벤트 스텁(override event ...) 코드를 생성
            /// </summary>
            private static string GenerateEventStub(EventInfo eventInfo, string currentNamespace)
            {
                string eventHandlerType = GetTypeName(eventInfo.EventHandlerType, currentNamespace);

                return
    $@"        public override event {eventHandlerType} {eventInfo.Name}
        {{
            add {{ throw new NotImplementedException(); }}
            remove {{ throw new NotImplementedException(); }}
        }}";
            }


            /// <summary>
            /// 현재 스크립트를 파일로 저장하고, CRLF 개행으로 통일
            /// </summary>
            private static void SaveScriptToFile(string filePath, string scriptName, string scriptContent)
            {
                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }

                string fullPath = Path.Combine(filePath, scriptName + ".cs");

                // CRLF Line Ending 통일
                string normalizedContent = scriptContent.Replace("\r\n", "\n").Replace("\n", NL);

                File.WriteAllText(fullPath, normalizedContent);

                Debug.Log($"Saved script at: {fullPath}");
            }


            /// <summary>
            /// true라면 Getter/Setter 같은 접근자(프로퍼티용) 메서드인지 확인
            /// </summary>
            private static bool IsAccessorMethod(MethodInfo methodInfo)
            {
                return methodInfo.IsSpecialName;
            }


            /// <summary>
            /// C#의 Type을 적절히 string으로 변환
            /// </summary>
            private static string GetTypeName(Type type, string currentNamespace)
            {
                if (type.IsArray)
                {
                    // 배열 차원을 계산하여 쉼표 추가 (2차원 이상)
                    int rank = type.GetArrayRank();
                    string arrayDimensions = rank > 1 ? "[" + new string(',', rank - 1) + "]" : "[]";
                    return GetTypeName(type.GetElementType(), currentNamespace) + arrayDimensions;
                }

                if (type.IsGenericType)
                {
                    string genericTypeName = type.GetGenericTypeDefinition().Name.Split('`')[0];
                    string nsPrefix = (type.Namespace == currentNamespace
                                       || type.Namespace == "System.Collections.Generic"
                                       || type.Namespace == "Cysharp.Threading.Tasks")
                                     ? ""
                                     : (type.Namespace + ".");

                    string genericArgs = string.Join(", ", type.GetGenericArguments().Select(t => GetTypeName(t, currentNamespace)));
                    return $"{nsPrefix}{genericTypeName}<{genericArgs}>";
                }

                // 기본 타입(bool, int, string 등) 별칭 매핑 처리
                if (s_TypeAliasMap.TryGetValue(type, out string alias))
                {
                    return alias;
                }

                // Nested 타입인지 검사
                string typeName = type.DeclaringType != null
                    ? $"{type.DeclaringType.Name}.{type.Name}"
                    : type.Name;

                // 같은 네임스페이스거나, 흔히 쓰이는 System.Collections.Generic, Cysharp.Threading.Tasks는 nsPrefix 생략
                if (type.Namespace == currentNamespace
                    || type.Namespace == "System.Collections.Generic"
                    || type.Namespace == "Cysharp.Threading.Tasks")
                {
                    return typeName;
                }
                else
                {
                    return $"{type.Namespace}.{typeName}";
                }
            }
        }



        ///======================================================================================================================================================


        /// <summary>
        /// 특정 타입을 직접 상속받는 클래스들을 반환합니다.
        /// 추상 클래스와 구체 클래스 모두 포함됩니다.
        /// </summary>
        /// <param name="baseType">기준이 되는 원본 클래스 타입.</param>
        /// <returns>기준 클래스를 직접 상속받는 클래스들의 배열.</returns>
        public static Type[] GetDirectSubclasses(Type baseType)
        {
            if (baseType == null)
            {
                throw new ArgumentNullException(nameof(baseType), "기준 타입은 null일 수 없습니다.");
            }

            if (!baseType.IsClass || baseType.IsSealed)
            {
                throw new ArgumentException("기준 타입은 추상 클래스 또는 상속 가능한 클래스여야 합니다.", nameof(baseType));
            }

            // 현재 어셈블리와 참조된 모든 어셈블리에서 타입 검색
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var subclasses = new List<Type>();

            foreach (var assembly in assemblies)
            {
                try
                {
                    subclasses.AddRange(assembly.GetTypes()
                        .Where(type => type.BaseType == baseType)); // 직접 상속 여부 확인
                }
                catch (ReflectionTypeLoadException)
                {
                    // 어셈블리의 타입을 로드하지 못한 경우 무시
                    continue;
                }
            }

            return subclasses.ToArray();
        }



        ///======================================================================================================================================================
    }
}
