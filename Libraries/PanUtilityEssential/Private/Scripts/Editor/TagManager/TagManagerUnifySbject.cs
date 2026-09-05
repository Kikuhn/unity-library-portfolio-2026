using UnityEngine;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor.Rendering;
using UnityEditor;
using Pan.Util;
using UnityEditor.PackageManager;



/// <summary>
/// TagManagerUnifySbject 전용 커스텀 에디터
/// </summary>
[CustomEditor(typeof(TagManagerUnifySbject))]
public class TagManagerUnifySbjectEditor : OdinEditor
{
    ///======================================================================================================================================================



    protected TagManagerUnifySbject Target => target as TagManagerUnifySbject;



    protected override void OnEnable()
    {
        base.OnEnable();
        UpdateMain();
    }



    private void UpdateMain()
    {
        foreach (var item in Target.TagManagerUnifyList)
        {
            item.Main = Target;
        }
    }



    ///======================================================================================================================================================



    public override void OnInspectorGUI()
    {
        UpdateMain();
        base.OnInspectorGUI();
    }



    ///======================================================================================================================================================
}



/// <summary>
/// 여러 SO(Tag,Layer 등)를 한꺼번에 Apply, 그리고 TagLayerSettings.cs 파일의
/// TagLayerSettings 클래스 내부에 'Chunk'를 삽입·갱신하도록 설계한 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "TagManagerUnify", menuName = CreateAssetMenuInfo.TAGMANAGER_UNIFY_SETTINGS, order = CreateAssetMenuInfo.TAGMANAGER_UNIFY_SETTINGS_ORDER)]
public class TagManagerUnifySbject : ScriptableObject
{
    ///======================================================================================================================================================



    /// <summary>
    /// TagManagerUnify 항목 하나를 의미
    /// </summary>
    [Serializable]
    public class TagManagerUnify
    {
        ///======================================================================================================================================================



        [HideInInspector]
        public TagManagerUnifySbject Main;



        [LabelText("이름")]
        [InfoBox("기본 유니티 설정\n이설정으로 유니티 기본설정으로 적용이 가능", VisibleIf = nameof(isUnityDefault))]
        public string Name = "New TagManager Unify";



        [LabelText("Define 심볼")]
        [EnableIf(nameof(isNotUnityDefault))]
        public DefineSymbolSettingSbject DefineSymbolSetting;



        [LabelText("Tag 설정")]
        public TagManager_Tag_SettingSbject TagSetting;



        [LabelText("Layer 설정")]
        public TagManager_Layer_SettingSbject LayerSetting;



        [LabelText("SortingLayer 설정")]
        public TagManager_SortingLayer_SettingSbject SortingLayerSetting;



        [LabelText("RenderingLayer 설정")]
        public TagManager_RenderingLayer_SettingSbject RenderingLayerSetting;



        private bool isUnityDefault => Name == UnityDefaultUnifySettingName;
        private bool isNotUnityDefault => !isUnityDefault;



        ///======================================================================================================================================================



        private string name_Layer => nameof(SU_Layers);
        private string name_SortingLayer => nameof(SU_SortingLayers);
        private string name_RenderingLayer => nameof(SU_RenderingLayers);
        private string name_Tag => nameof(SU_Tags);

        private const string END_OF_TAGLAYERSETTINGS = "[END OF TagLayerSettings]";



        ///======================================================================================================================================================



        [Button("설정 적용")]
        [ButtonGroup("ButtonGroup")]
        public void Execute_Apply()
        {
            if (EditorUtility.DisplayDialog("경고", "설정을 프로젝트에 적용하시겠습니까?\n이 설정에 Define 심볼이 있을경우,\n리스트에 등록되어있는 다른 요소들의 Define 심볼은 컴파일 에러 방지를 위해 모두 프로젝트에서 제거됩니다\n(유니티 기본설정값은 이와 상관없이 모든 Define 심볼이 제거됩니다)", "적용", "취소"))
            {
                ApplySetting();
            }
        }



        [Button("스크립트 생성")]
        [ButtonGroup("ButtonGroup")]
        [EnableIf(nameof(isNotUnityDefault))]
        public void Execute_GenerateOnlyCode()
        {
            if (EditorUtility.DisplayDialog("경고", "스크립트를 생성하시겠겠습니까?", "적용", "취소"))
            {
                if (File.Exists(Main.GetFullGeneratedCodePathWithFile))
                {
                    int result = EditorUtility.DisplayDialogComplex("경고", "이미 생성된 스크립트가 있습니다\n 스크립트를 수정하시겠습니까?\n새롭게 생성하시겠습니까?", "수정하기", "취소하기", "삭제후 재생성");
                    if (result == 0)
                    {
                        GenerateCode(false);
                    }
                    else if (result == 2)
                    {
                        GenerateCode(true);
                    }
                }

            }
        }



        [Button("적용&생성")]
        [ButtonGroup("ButtonGroup")]
        [EnableIf(nameof(isNotUnityDefault))]
        public void Execute_GenerateAndApply()
        {
            if (EditorUtility.DisplayDialog("경고", "설정을 프로젝트에 적용하고, 스크립트를 생성하시겠겠습니까?", "적용", "취소"))
            {
                if (File.Exists(Main.GetFullGeneratedCodePathWithFile))
                {
                    int result = EditorUtility.DisplayDialogComplex("경고", "이미 생성된 스크립트가 있습니다\n 스크립트를 수정하시겠습니까?\n새롭게 생성하시겠습니까?", "수정하기", "취소하기", "삭제후 재생성");
                    if (result == 0)
                    {
                        ApplySettingAndGenerate(false);
                    }
                    else if (result == 2)
                    {
                        ApplySettingAndGenerate(true);
                    }
                }
            }
        }



        ///======================================================================================================================================================



        ///<summary>
        ///설정을 프로젝트에 적용하기
        /// </summary>
        public void ApplySetting()
        {
            // 1) Define 심볼 적용
            if (DefineSymbolSetting != null)
            {
                //? 리스트의 다른 Define 심볼들은 프로젝트에서 모두 제거 (충돌 방지)
                foreach (var tagManager in Main.TagManagerUnifyList)
                {
                    if (tagManager == this || tagManager.DefineSymbolSetting == null) { continue; }
                    DefineSymbolsUtility.ApplySymbolsToProject(tagManager.DefineSymbolSetting.DefineSymbols, true);
                }


                DefineSymbolsUtility.ApplySymbolsToProject(DefineSymbolSetting.DefineSymbols, false);
            }
            //! 유니티 기본 설정일경우, 그냥 모든 Define 심볼을 제거한다
            else if (isUnityDefault)
            {
                //? 리스트의 다른 Define 심볼들은 프로젝트에서 모두 제거 (충돌 방지)
                foreach (var tagManager in Main.TagManagerUnifyList)
                {
                    if (tagManager == this || tagManager.DefineSymbolSetting == null) { continue; }
                    DefineSymbolsUtility.ApplySymbolsToProject(tagManager.DefineSymbolSetting.DefineSymbols, true);
                }
            }


            // 2) 각 세팅 적용
            if (TagSetting != null) TagSetting.ApplyToProject();
            if (LayerSetting != null) LayerSetting.ApplyToProject();
            if (SortingLayerSetting != null) SortingLayerSetting.ApplyToProject();
            if (RenderingLayerSetting != null) RenderingLayerSetting.ApplyToProject();

        }



        /// <summary>
        /// 스크립트를 생성하기
        /// </summary>
        public void GenerateCode(bool createNewScript)
        {
            if (string.IsNullOrEmpty(Main.GetFullGeneratedCodePathWithFile))
            {
                Debug.LogError($"[{Name}] codeGenFilePath 가 비어있습니다! 경로를 지정해주세요.");
                return;
            }
            if (Main.DefaultUnityLayerSetting == null)
            {
                Debug.LogError($"{nameof(Main.DefaultUnityLayerSetting)}가 비어있습니다. 기본 유니티 레이어를 지정해주세요");
                return;
            }


            //? 스크립트 파일을 제거한뒤 새롭게 생성할지 여부
            if (createNewScript && File.Exists(Main.GetFullGeneratedCodePathWithFile))
            {
                File.Delete(Main.GetFullGeneratedCodePathWithFile);
            }


            InsertOrUpdateChunkInFile(Main.GetFullGeneratedCodePathWithFile, $"TagManagerUnify:{Name}", BuildChunkCode());
        }



        /// <summary>
        /// 설정을 프로젝트에 적용하고 스크립트를 생성
        /// </summary>
        public void ApplySettingAndGenerate(bool createNewScript)
        {
            ApplySetting();


            // 3) 코드 생성 & 삽입
            GenerateCode(createNewScript);
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 이 SO가 생성할 코드 Chunk (전처리기가 있으면 #if ... #else ... #endif)
        /// 현재는 Layer만 예시지만, Tag/SortingLayer 등도 확장 가능
        /// </summary>
        private string BuildChunkCode()
        {
            // 첫 번째 심볼 사용
            string defineSymbol = null;
            if (DefineSymbolSetting != null && DefineSymbolSetting.DefineSymbols.Count > 0)
            {
                defineSymbol = DefineSymbolSetting.DefineSymbols[0];
            }

            var sb = new StringBuilder();
            sb.AppendLine("//----------------------------------------");
            sb.AppendLine($"// Chunk generated by {Main.name}: {Name}");
            sb.AppendLine($"// Date: {System.DateTime.Now}");
            sb.AppendLine("//----------------------------------------");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(defineSymbol))
            {
                sb.AppendLine($"#if {defineSymbol}");
                sb.Append(BuildTagCode());
                sb.AppendLine();
                sb.Append(BuildLayerCode());
                sb.AppendLine();
                sb.Append(BuildSortingLayerCode());
                sb.AppendLine();
                sb.Append(BuildRenderingLayerCode());
                sb.AppendLine($"#endif");
            }
            else
            {
                // 전처리기 없이 그냥 코드
                sb.Append(BuildTagCode());
                sb.AppendLine();
                sb.Append(BuildLayerCode());
                sb.AppendLine();
                sb.Append(BuildSortingLayerCode());
                sb.AppendLine();
                sb.Append(BuildRenderingLayerCode());
            }

            return sb.ToString();
        }



        private string BuildSortingLayerCode()
        {
            var sb = new StringBuilder();

            const string DIC_NAME = "cachingSortingLayerNames";

            sb.AppendLine($"public static partial class {name_SortingLayer}");
            sb.AppendLine("{");

            sb.AppendLine($"static {name_SortingLayer}()");
            sb.AppendLine("{");
            sb.AppendLine("    EL[] enums = Enum.GetValues(typeof(EL)) as EL[];");
            sb.AppendLine($"    {DIC_NAME} = new Dictionary<EL, string>(enums.Length);");
            sb.AppendLine("    foreach (EL value in Enum.GetValues(typeof(EL)))");
            sb.AppendLine("    {");
            sb.AppendLine($"        {DIC_NAME}[value] = value.ToString();");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("public enum EL");
            sb.AppendLine("{");

            int index = 0;
            foreach (var sortingLayer in SortingLayerSetting.OtherLayers)
            {
                string comma = (index < SortingLayerSetting.OtherLayers.Count - 1) ? "," : "";
                sb.AppendLine($"    {sortingLayer.Name} = {index}{comma}");
                index++;
            }
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine($"private static readonly Dictionary<EL, string> {DIC_NAME};");
            sb.AppendLine();

            sb.AppendLine($"public static string GetName(EL sortingLayer) => {DIC_NAME}[sortingLayer];");
            sb.AppendLine();

            sb.AppendLine($"public static Renderer SetSortingLayer(Renderer var, EL sortingLayer)");
            sb.AppendLine("{");
            sb.AppendLine("    var.sortingLayerName = GetName(sortingLayer);");
            sb.AppendLine("    return var;");
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine($" public static Renderer SetSortingLayerExt(this Renderer var, EL sortingLayer) => SetSortingLayer(var, sortingLayer);");


            sb.AppendLine("}");

            return BuildLayerCode_CurtainCall(sb);
        }



        private string BuildRenderingLayerCode()
        {
            var sb = new StringBuilder();

            const string DIC_NAME = "cachingRenderingLayerNames";

            sb.AppendLine($"public static partial class {name_RenderingLayer}");
            sb.AppendLine("{");

            sb.AppendLine($"static {name_RenderingLayer}()");
            sb.AppendLine("{");
            sb.AppendLine("    EL[] enums = Enum.GetValues(typeof(EL)) as EL[];");
            sb.AppendLine($"    {DIC_NAME} = new Dictionary<EL, string>(enums.Length);");
            sb.AppendLine("    foreach (EL value in Enum.GetValues(typeof(EL)))");
            sb.AppendLine("    {");
            sb.AppendLine($"        {DIC_NAME}[value] = value.ToString();");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("public enum EL");
            sb.AppendLine("{");

            int index = 0;
            foreach (var renderingLayer in RenderingLayerSetting.OtherLayers)
            {
                string comma = (index < RenderingLayerSetting.OtherLayers.Count - 1) ? "," : "";
                sb.AppendLine($"    {renderingLayer.Name} = {index}{comma}");
                index++;
            }
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine($"private static readonly Dictionary<EL, string> {DIC_NAME};");
            sb.AppendLine();

            sb.AppendLine($"public static string GetName(EL renderingLayer) => {DIC_NAME}[renderingLayer];");

            sb.AppendLine("}");

            return BuildLayerCode_CurtainCall(sb);
        }



        private string BuildTagCode()
        {
            var sb = new StringBuilder();

            const string DIC_NAME = "cachingTagNames";

            sb.AppendLine($"public static partial class {name_Tag}");
            sb.AppendLine("{");

            sb.AppendLine($"static {name_Tag}()");
            sb.AppendLine("{");
            sb.AppendLine("    ETag[] enums = Enum.GetValues(typeof(ETag)) as ETag[];");
            sb.AppendLine("    if (enums == null || enums.Length == 0) { return; }");
            sb.AppendLine($"    {DIC_NAME} = new Dictionary<ETag, string>(enums.Length);");
            sb.AppendLine("    foreach (ETag value in Enum.GetValues(typeof(ETag)))");
            sb.AppendLine("    {");
            sb.AppendLine($"        {DIC_NAME}[value] = value.ToString();");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("public enum ETag");
            sb.AppendLine("{");

            int index = 0;
            foreach (var tag in TagSetting.Tags)
            {
                string comma = (index < TagSetting.Tags.Count - 1) ? "," : "";
                sb.AppendLine($"    {tag.TagName} = {index}{comma}");
                index++;
            }
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine($"private static readonly Dictionary<ETag, string> {DIC_NAME};");
            sb.AppendLine();

            sb.AppendLine($"public static string GetTag(ETag tag) => {DIC_NAME}[tag];");
            sb.AppendLine();

            sb.AppendLine($"public static GameObject SetTag(GameObject gameObject, ETag tag)");
            sb.AppendLine("{");
            sb.AppendLine("    gameObject.tag = GetTag(tag);");
            sb.AppendLine("    return gameObject;");
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine($"public static MonoBehaviour SetTag(MonoBehaviour mono, ETag tag)");
            sb.AppendLine("{");
            sb.AppendLine("    mono.tag = GetTag(tag);");
            sb.AppendLine("    return mono;");
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine($"public static GameObject SetTagExt(this GameObject var, ETag tag)");
            sb.AppendLine("{");
            sb.AppendLine("    var.tag = GetTag(tag);");
            sb.AppendLine("    return var;");
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine($"public static MonoBehaviour SetTagExt(this MonoBehaviour var, ETag tag)");
            sb.AppendLine("{");
            sb.AppendLine("    var.tag = GetTag(tag);");
            sb.AppendLine("    return var;");
            sb.AppendLine("}");
            sb.AppendLine();


            sb.AppendLine("}");

            return BuildLayerCode_CurtainCall(sb);
        }



        /// <summary>
        /// 실제로 LayerEnum, 상수, 메서드 등 멤버를 만드는 예시
        /// (클래스나 namespace 선언은 포함 X -> 상위 스켈레톤 내부에 들어가야 함)
        /// </summary>
        private string BuildLayerCode()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"public static partial class {name_Layer}");
            sb.AppendLine("{");

            // 0~31 레이어 스캔
            List<(int index, string layerName, string safeName)> layerList = new List<(int, string, string)>(32);
            for (int i = 0; i < 32; i++)
            {
                string n = LayerSetting.Layers[i].LayerName;
                if (!string.IsNullOrEmpty(n))
                {
                    string safe = ConvertToSafeIdentifier(n);
                    layerList.Add((i, n, safe));
                }
            }

            // enum
            sb.AppendLine("public enum EL");
            sb.AppendLine("{");
            for (int i = 0; i < layerList.Count; i++)
            {
                var (idx, _, safeName) = layerList[i];
                string comma = (i < layerList.Count - 1) ? "," : "";
                sb.AppendLine($"    {safeName} = {idx}{comma}");
            }
            sb.AppendLine("}");
            sb.AppendLine();

            // 상수 & Shifted 상수
            foreach (var (idx, layerName, safeName) in layerList)
            {
                //! 유니티 기본 레이어는 상수로 생성 안함               
                if (Main.DefaultUnityLayerSetting.Layers[idx].LayerName == layerName)
                {
                    Debug.Log($"상수 생성 스킵: {safeName}");
                    continue;
                }

                sb.AppendLine($"public const int {safeName} = {idx};");
                sb.AppendLine($"public static int _{safeName} = 1 << {safeName};");
                sb.AppendLine();
            }

            // GetLayer
            sb.AppendLine("public static int GetLayer(EL layer)");
            sb.AppendLine("{");
            sb.AppendLine("    switch (layer)");
            sb.AppendLine("    {");
            foreach (var (idx, _, safeName) in layerList)
            {
                sb.AppendLine($"        case EL.{safeName}: return {safeName};");
            }
            sb.AppendLine("        default: return 0;");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();

            // GetBitLayer
            sb.AppendLine("public static int GetBitLayer(EL layer)");
            sb.AppendLine("{");
            sb.AppendLine("    switch (layer)");
            sb.AppendLine("    {");
            foreach (var (idx, _, safeName) in layerList)
            {
                sb.AppendLine($"        case EL.{safeName}: return _{safeName};");
            }
            sb.AppendLine("        default: return 0;");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();

            // GetBitLayer(params EL[])
            sb.AppendLine("public static int GetBitLayer(params EL[] layers)");
            sb.AppendLine("{");
            sb.AppendLine("    int shifts = 0;");
            sb.AppendLine("    foreach (EL layer in layers)");
            sb.AppendLine("    {");
            sb.AppendLine("        shifts |= GetBitLayer(layer);");
            sb.AppendLine("    }");
            sb.AppendLine("    return shifts;");
            sb.AppendLine("}");
            sb.AppendLine();

            // IsLayer
            sb.AppendLine("public static bool IsLayer(int layerInt, EL layer)");
            sb.AppendLine("{");
            sb.AppendLine("    int layerShift = GetLayer(layer);");
            sb.AppendLine("    return (layerInt & (1 << layerShift)) != 0;");
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine("}");


            return BuildLayerCode_CurtainCall(sb);
        }



        /// <summary>
        /// 코드생성 마무리 작업 (줄바꿈 등)
        /// </summary>
        /// <param name="sb"></param>
        /// <returns></returns>
        private string BuildLayerCode_CurtainCall(StringBuilder sb)
        {
            // 기존 내용을 줄 단위로 분리하기 전에, 끝부분의 불필요한 개행·공백 제거
            var lines = sb.ToString().TrimEnd('\n', '\r').Split('\n');

            sb.Clear(); // StringBuilder 초기화

            int index = 0;
            foreach (var line in lines)
            {
                // 첫 번째 줄(index == 0), 두 번째 줄(index == 1), 마지막 줄(index == lines.Length - 1)에는 공백 추가 X
                if (!(index == 0 || index == 1 || index == lines.Length - 1))
                {
                    sb.Append("    ");
                }

                sb.AppendLine(line.TrimEnd('\r')); // 줄 끝의 캐리지리턴(\r) 제거 후 추가
                index++;
            }

            return sb.ToString();
        }



        private string BuildLayerCode_UnityDefault()
        {
            var sb = new StringBuilder();


            //? 유니티 기본 레이어 스캔
            List<(int index, string layerName, string safeName)> layerList = new List<(int, string, string)>();
            for (int i = 0; i < 32; i++)
            {
                string n = Main.DefaultUnityLayerSetting.Layers[i].LayerName;
                if (!string.IsNullOrEmpty(n))
                {
                    string safe = ConvertToSafeIdentifier(n);
                    layerList.Add((i, n, safe));
                }
            }


            //? 상수 & Shifted 상수
            foreach (var (idx, _, safeName) in layerList)
            {
                sb.AppendLine($"        public const int {safeName} = {idx};");
                sb.AppendLine($"        public static int _{safeName} = 1 << {safeName};");
                sb.AppendLine();
            }


            //! 뒷부분의 모든 줄바꿈 제거
            while (sb.Length > 0 && (sb[sb.Length - 1] == '\n' || sb[sb.Length - 1] == '\r')) { sb.Length--; }


            return sb.ToString();
        }



        /// <summary>
        /// filePath에 있는 TagLayerSettings 클래스 내부,
        /// [BEGIN: chunkID] ~ [END: chunkID] 범위를 chunkCode로 교체 or 삽입.
        /// 없으면 skeleton 내부에 새로 삽입.
        /// </summary>
        private void InsertOrUpdateChunkInFile(string filePath, string chunkID, string chunkCode)
        {
            // 파일 없으면 스켈레톤 생성
            if (!File.Exists(filePath))
            {
                CreateInitialSkeleton(filePath);
            }

            var allLines = new List<string>(File.ReadAllLines(filePath));

            // 마커
            string beginMarker = $"    // [BEGIN: {chunkID}]";
            string endMarker = $"    // [END: {chunkID}]";

            bool foundBegin = false;
            bool foundEnd = false;
            int beginIndex = -1;
            int endIndex = -1;

            // 기존 chunk 검색
            for (int i = 0; i < allLines.Count; i++)
            {
                string line = allLines[i].Trim();
                if (line == beginMarker.Trim())
                {
                    foundBegin = true;
                    beginIndex = i;
                }
                if (line == endMarker.Trim())
                {
                    foundEnd = true;
                    endIndex = i;
                }
            }

            // 새 chunk 텍스트
            var chunkBuilder = new StringBuilder();
            chunkBuilder.AppendLine(beginMarker);

            // 1) 먼저 chunkCode 의 모든 줄바꿈을 단일 형태(\n)로 통일
            string normalized = chunkCode
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .TrimEnd('\n');

            // 2) 라인 분할
            var chunkLines = normalized.Split('\n');
            foreach (var rawLine in chunkLines)
            {
                // 맨 뒤의 공백 제거
                string line = rawLine.TrimEnd();


                // indent + line + '\n'
                if (!rawLine.StartsWith("#"))
                {
                    chunkBuilder.Append("    "); // indent
                }


                chunkBuilder.Append(line);
                chunkBuilder.Append("\n"); // single line break
            }

            chunkBuilder.AppendLine(endMarker);

            string finalChunk = chunkBuilder.ToString().TrimEnd();

            if (foundBegin && foundEnd && beginIndex < endIndex)
            {
                // 기존 영역 교체
                var newLines = new List<string>();

                // 위쪽 그대로
                for (int i = 0; i < beginIndex; i++)
                {
                    newLines.Add(allLines[i]);
                }
                // 새 chunk
                newLines.AddRange(finalChunk.Split('\n'));
                // 아래쪽
                for (int i = endIndex + 1; i < allLines.Count; i++)
                {
                    newLines.Add(allLines[i]);
                }
                allLines = newLines;
            }
            else
            {
                // 없으면 삽입
                int insertPos = FindInsertPosition(allLines);
                if (insertPos < 0) insertPos = allLines.Count;
                var linesToInsert = finalChunk.Split('\n');
                allLines.InsertRange(insertPos, linesToInsert);
            }

            File.WriteAllLines(filePath, allLines.Select(NormalizeLineEndings), Encoding.UTF8);
            AssetDatabase.Refresh();

            Debug.Log($"[{nameof(TagManagerUnifySbject)}] Chunk '{chunkID}' updated in: {filePath}");
        }



        /// <summary>
        /// TagLayerSettings 클래스 내부에 "// [END OF TagLayerSettings]" 라인이 있을 것이므로,<br/>
        /// 그 직전(index) 을 찾아서 삽입 위치로 삼음.
        /// </summary>
        private int FindInsertPosition(List<string> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains(END_OF_TAGLAYERSETTINGS))
                {
                    return i; // 그 줄 바로 앞에 삽입
                }
            }
            // 없으면 파일 끝
            return lines.Count;
        }



        /// <summary>
        /// TagLayerSettings라는 스켈레톤 생성.<br/>
        /// 클래스 내부에 "// [END OF TagLayerSettings]" 마커를 두어,<br/>
        /// 그 위에 chunk들이 들어갈 수 있게 함.<br/>
        /// </summary>
        private void CreateInitialSkeleton(string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine($"using {nameof(Pan)}.{nameof(Pan.Util)};");
            sb.AppendLine("//! DO NOT EDIT! THIS IS AUTO-GENERATED CODE 수정금지, 자동으로 생성된 코드임");

            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();

            sb.AppendLine($"namespace {nameof(Pan)}.{nameof(Pan.Util)}");
            sb.AppendLine("{");


            sb.AppendLine($"    public static partial class {name_Layer}");
            sb.AppendLine("    {");
            sb.AppendLine(BuildLayerCode_UnityDefault());
            sb.AppendLine("    }");
            sb.AppendLine($"    // {END_OF_TAGLAYERSETTINGS}");


            sb.AppendLine("}");


            File.WriteAllText(filePath, NormalizeLineEndings(sb.ToString()), Encoding.UTF8);
            Debug.Log($"[{nameof(TagManagerUnifySbject)}] Created initial skeleton: {filePath}");
        }



        /// <summary>
        /// 레이어 이름을 C# 식별자로 변환
        /// (공백/특수문자 -> '_', 첫문자가 숫자면 '_' 붙임)
        /// </summary>
        private string ConvertToSafeIdentifier(string layerName)
        {
            var safe = layerName
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace(".", "_")
                .Replace("(", "_")
                .Replace(")", "_")
                .Replace("[", "_")
                .Replace("]", "_")
                .Replace("{", "_")
                .Replace("}", "_")
                .Replace("!", "_")
                .Replace("@", "_")
                .Replace("#", "_")
                .Replace("$", "_")
                .Replace("%", "_")
                .Replace("^", "_")
                .Replace("&", "_")
                .Replace("*", "_")
                .Replace("?", "_")
                .Replace(":", "_")
                .Replace(";", "_")
                .Replace(",", "_");

            if (!string.IsNullOrEmpty(safe) && char.IsDigit(safe[0]))
            {
                safe = "_" + safe;
            }
            return safe;
        }



        private string NormalizeLineEndings(string input)
        {
            return input.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine);
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================



    /// <summary>
    /// 여러 TagManagerUnify 항목을 리스트로 관리
    /// </summary>
    public List<TagManagerUnify> TagManagerUnifyList = new List<TagManagerUnify>();



    [LabelText("유니티 기본 레이어 설정")]
    public TagManager_Layer_SettingSbject DefaultUnityLayerSetting;



    ///======================================================================================================================================================



    [Button("스크립트 생성 (전체)")]
    [ButtonGroup("ButtonGroup")]
    public void Execute_GenerateOnlyCode()
    {
        if (EditorUtility.DisplayDialog("경고", "모든 설정들의 스크립트를 생성하시겠겠습니까?\n (파일이 새롭게 생성됩니다)", "적용", "취소"))
        {
            File.Delete(GetFullGeneratedCodePathWithFile);
            foreach (var entry in TagManagerUnifyList)
            {
                entry.Main = this;
                entry.GenerateCode(false);
            }
        }
    }



    ///======================================================================================================================================================



    [LabelText("유니티 기본 통합 설정 이름")]
    [ShowInInspector, ReadOnly]
    public static string UnityDefaultUnifySettingName => "UnityDefault";



    [LabelText("스크립트 저장 경로 (상대경로)")]
    [SerializeField][ReadOnly] private string GeneratedCodePath = @"Private\TagManager\Scripts";



    [LabelText("패키지 경로 (상대경로)")]
    [SerializeField][ReadOnly] private string PackagePath = @"Packages/com.kikuhn.panutilityessential/package.json";



    [LabelText("스크립트 저장 경로")]
    [ShowInInspector, ReadOnly]
    private string GetFullGeneratedCodePath => Path.Combine(GetPanUtilityEssentialPath(), GeneratedCodePath);



    [LabelText("스크립트 저장 경로 파일")]
    [ShowInInspector, ReadOnly]
    private string GetFullGeneratedCodePathWithFile => Path.Combine(GetPanUtilityEssentialPath(), GeneratedCodePath, "TagLayerSettings_Generated.cs");





    /// <summary>
    /// “Packages/PanUtilityEssential” 폴더가 실제 어디에 있는지(=resolvedPath) 가져옵니다.
    /// </summary>
    /// <returns>패키지의 실제 물리 경로. 찾지 못하면 null</returns>
    public string GetPanUtilityEssentialPath()
    {
        //. "Packages/PanUtilityEssential/package.json" 경로를 직접 넣어준다
        var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(PackagePath);
        if (packageInfo == null)
        {
            return null;
        }
        return packageInfo.resolvedPath;
    }



    ///======================================================================================================================================================
}