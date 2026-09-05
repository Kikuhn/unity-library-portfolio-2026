using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization; // OdinSerialize를 위해 필요
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Pan.Util; // 필요에 따라 사용

namespace Pan.Util.Editors
{
    /// <summary>
    /// AutoCacheTypeGenerator : 입력한 문자열을 통해 Base Type을 지정하고, 
    /// 해당 타입의 하위 타입들을 검색하여 코드를 자동 생성하는 ScriptableObject입니다.
    /// 
    /// [요구사항]
    /// - baseTypeName : 실제로 유효한 Type인지 검증(ValidateBaseType)
    /// - valueTypeName : 별도 유효성 검증 없이, 비어있지 않으면 그대로 BaseTypeInstanceAuto 제네릭에 사용
    /// </summary>
    [CreateAssetMenu(
        fileName = "TypeInstanceGenerator",
        menuName = "판/자동 타입 인스턴스 생성기",
        order = 0)]
    public class TypeInstanceGenerator : ScriptableObject
    {
        #region 코드 생성 설정
        [TitleGroup("코드 생성 설정")]
        [LabelText("생성될 CS 파일 이름")]
        [InfoBox("파일 이름에 {0}를 넣으면 'BaseClass' 등의 이름으로 치환됩니다.\n예) TypeInstance_{0}.cs -> TypeInstance_BaseClass.cs")]
        public string fileName = "TypeInstanceGenerated_{0}.cs";

        [TitleGroup("코드 생성 설정")]
        [LabelText("생성될 클래스 이름")]
        [InfoBox("비워둘경우, 해당 'TypeInstanceGenerated_TypeName'으로 사용됩니다.")]
        public string generateClassName = "";

        [FolderPath(AbsolutePath = true)]
        [LabelText("생성할 폴더 경로")]
        public string filePath;

        [LabelText("네임스페이스 (없으면 비움)")]
        public string targetNamespace;

        [LabelText("검색 옵션")]
        public SU_Collection_Types.TypeSearch searchOption = SU_Collection_Types.TypeSearch.ConcreteClasses;
        #endregion



        #region Base Type 지정 (문자열 입력 및 내부 검증)
        [TitleGroup("Base Type 지정")]
        [InfoBox("BaseClass가 될 클래스의 전체 이름(네임스페이스 포함)을 입력하세요.\n입력과 동시에 내부적으로 타입 유효성이 갱신됩니다.")]
        [OnValueChanged("ValidateBaseType", true)]
        [OdinSerialize]
        [LabelText("Base Type 이름")]
        public string baseTypeName;

        [OdinSerialize]
        internal Type validatedBaseType;

        // 정적 캐시: 모든 어셈블리의 타입 정보를 한 번만 수집합니다.
        internal static Dictionary<string, Type> typeLookup;
        internal static void InitializeTypeLookup()
        {
            if (typeLookup != null) return;
            typeLookup = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.IsClass)
                        {
                            // 풀네임 Key 등록
                            if (!typeLookup.ContainsKey(type.FullName))
                                typeLookup[type.FullName] = type;
                            // 짧은 이름 Key 등록
                            if (!typeLookup.ContainsKey(type.Name))
                                typeLookup[type.Name] = type;
                        }
                    }
                }
                catch { }
            }
        }

        internal void ValidateBaseType()
        {
            InitializeTypeLookup();
            if (string.IsNullOrEmpty(baseTypeName))
            {
                validatedBaseType = null;
                return;
            }

            // 우선 Type.GetType으로 직접 시도
            var t = Type.GetType(baseTypeName, throwOnError: false);
            if (t != null)
            {
                validatedBaseType = t;
                return;
            }

            // Dictionary 매칭
            if (typeLookup.TryGetValue(baseTypeName, out Type found))
                validatedBaseType = found;
            else
                validatedBaseType = null;
        }


        /// <summary>
        /// BaseType의 스크립트 경로를 반환합니다.
        /// 주의: 한 파일에 여러 클래스가 포함되거나, 파일명과 클래스명이 다를 수 있으므로 정확한 결과를 보장할 수 없습니다.
        /// </summary>
        internal string GetScriptPath(Type type)
        {
#if UNITY_EDITOR
            // AssetDatabase.FindAssets를 우선적으로 사용 (에디터 환경 전제)
            string[] guids = AssetDatabase.FindAssets("t:MonoScript " + type.Name);
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() == type)
                    return path;
            }
#endif
            return "";
        }
        #endregion



        #region Value Type 지정 (검증 없이 단순 사용)
        [TitleGroup("Value Type 지정")]
        [InfoBox("ValueType가 될 클래스(네임스페이스 포함) 이름을 입력하세요.\n" +
                 "이 값은 유효성 검사 없이 그대로 사용됩니다.\n" +
                 "비어있지 않으면 BaseTypeInstanceAuto 제네릭 및 Dictionary Value 타입에 적용됩니다.")]
        [OdinSerialize]
        [LabelText("Value Type 이름 (검증 안 함)")]
        public string valueTypeName;
        #endregion



        private bool IsValidBaseType()
        {
            return validatedBaseType != null;
        }



        #region 코드 생성하기
        [TitleGroup("코드 생성")]
        [ShowIf("IsValidBaseType")]
        [Button(ButtonSizes.Large), GUIColor(0.3f, 0.8f, 0.3f)]
        [LabelText("코드 생성하기!")]
        public void GenerateCode()
        {
            if (validatedBaseType == null)
            {
                Debug.LogError("[TypeInstanceGenerator] 유효한 Base Type이 지정되지 않았습니다.");
                return;
            }

            // baseTypeName으로부터 하위 타입 검색
            var types = SU_Collection_Types.GetTypesAssignableTo(validatedBaseType, searchOption);
            var nestedClass = validatedBaseType.GetNestedClassChainExceptLast();

            // 클래스 이름 결정
            string className;
            if (string.IsNullOrEmpty(generateClassName))
            {
                if (nestedClass != null)
                {
                    className = $"TypeInstanceGenerated_{string.Join('_', nestedClass)}_{validatedBaseType.Name}";
                }
                else
                {
                    className = $"TypeInstanceGenerated_{validatedBaseType.Name}";
                }
            }
            else
            {
                className = generateClassName;
            }

            // 코드 생성
            string generatedCode = GenerateClassCode(className, types, nestedClass);

            // 파일 저장
            string finalFileName = string.Format(fileName, validatedBaseType.Name);
            string fullPath = Path.Combine(filePath, finalFileName);
            GeneratedCSharpFileWriter.Write(fullPath, generatedCode);

#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
            Debug.Log($"[AutoCacheTypeGenerator] 코드 생성 완료! -> {fullPath}");
        }
        #endregion



        #region 코드 생성 메서드
        /// <summary>
        /// valueTypeName이 비어있지 않으면 그대로, 비어있으면 validatedBaseType.Name을 사용
        /// </summary>
        private string GenerateClassCode(string className, Type[] derivedTypes, string[] nestedClass)
        {
            // 값이 있으면 그대로 사용, 없으면 validatedBaseType의 Name
            string finalValueTypeString = !string.IsNullOrEmpty(valueTypeName)
                ? valueTypeName
                : validatedBaseType.Name; // 그래도 적어도 baseType의 짧은 이름은 있음

            bool hasNamespace = !string.IsNullOrWhiteSpace(targetNamespace);
            var sb = new StringBuilder();
            sb.AppendLine("///======================================================================================================================================================");
            sb.AppendLine("//?     이 코드는 자동 생성되었습니다.");
            sb.AppendLine("//!     수동 편집 금지: 생성기 설정 또는 템플릿을 수정한 뒤 다시 생성해야 합니다.");
            sb.AppendLine("///======================================================================================================================================================");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine($"using {nameof(Pan)}.{nameof(Pan.Util)};");
            if (nestedClass != null)
            {
                foreach (var nestClass in nestedClass)
                {
                    sb.AppendLine($"using static {nestClass}; //. 내부 클래스를 감지하여 자동으로 선언됨");
                }
            }

            sb.AppendLine();
            if (hasNamespace)
            {
                sb.AppendLine($"namespace {targetNamespace}");
                sb.AppendLine("{");
            }

            string indent = hasNamespace ? "    " : "";

            // BaseTypeInstanceAuto<T> 여기서 T를 finalValueTypeString으로 사용 (검증 없이)
            sb.AppendLine($"{indent}public class {className} : {typeof(BaseTypeInstanceAuto<>).GetPureClassName()}<{finalValueTypeString}>");
            sb.AppendLine($"{indent}{{");

            // 생성자
            sb.AppendLine($"{indent}    public {className}() : base(new Dictionary<Type, {finalValueTypeString}>({derivedTypes.Length}))");
            sb.AppendLine($"{indent}    {{");
            foreach (var t in derivedTypes)
            {
                if (t.IsAbstract || t.IsInterface || t.IsValueType)
                    continue;  //! 추상 클래스, 인터페이스, 값 타입은 무시
                sb.AppendLine($"{indent}        GeneratedTypeInstances.Add(typeof({t.Name}), new {t.Name}());");
            }
            sb.AppendLine($"{indent}    }}");

            sb.AppendLine($"{indent}}}");
            if (hasNamespace)
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }
        #endregion
    }

}
