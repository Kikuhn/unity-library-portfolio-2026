using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;



namespace Pan.AddressableManagers.Editor
{


    public class PanAddressableEditorSettingSbject : ScriptableObject
    {
        ///======================================================================================================================================================



        [Title("판 어드레서블 에디터 설정")]
        [InfoBox("절대 이 ScriptableObject를 옮기거나 제거해서는안됨\n에디터에서만 동작")]
        [HideLabel]
        [DisplayAsString]
        [ShowInInspector]
        [EnableGUI]
        [PropertyOrder(-1000)]
        [PropertySpace(SpaceAfter = -8f)]
        private string dummyTitle => "";



        ///======================================================================================================================================================



        [ShowInInspector, Sirenix.OdinInspector.ReadOnly, LabelText("유니티 어드레서블 설정"), PropertyOrder(-10)]
        private AddressableAssetSettings AddressableAssetSettings => AddressableAssetSettingsDefaultObject.Settings;



        private bool IsNull_autoLabelScriptFolder => AutoScriptFolder == null || AutoScriptFolder == "";



        ///======================================================================================================================================================



        [TitleGroup("스크립트 자동 생성")]
        [LabelText("스크립트 생성 경로")]
        [FolderPath(AbsolutePath = false, RequireExistingPath = true, ParentFolder = "Assets")]
        [InfoBox("Auto-Label을 지정할 폴더를 지정해주세요", InfoMessageType = InfoMessageType.Error, VisibleIf = nameof(IsNull_autoLabelScriptFolder))]
        [SerializeField]
        private string AutoScriptFolder;



        ///======================================================================================================================================================



        //? 스크립트 생성: Auto-Label 생성



        [TitleGroup("스크립트 자동 생성"), BoxGroup("스크립트 자동 생성/Auto-Label 생성")]
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly, LabelText("Auto-Label 스크립트 이름")]
        private string AddressableAutoLabelScriptName => "AddressableAutoLabels_Generated";



        [TitleGroup("스크립트 자동 생성"), BoxGroup("스크립트 자동 생성/Auto-Label 생성")]
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly, LabelText("Auto-Label 스크립트 Enum 이름")]
        private string AddressableAutoLabelEnumName => "EAddressableLabel";



        [TitleGroup("스크립트 자동 생성"), BoxGroup("스크립트 자동 생성/Auto-Label 생성")]
        [Button("Auto-Label 스크립트 생성", Icon = SdfIconType.LightningFill), GUIColor(0.97f, 0.85f, 0.39f)]
        private void GenerateAutoLabel()
        {
            Debug.Log("[어드레서블 AutoLabel 스크립트 생성] 을 시작합니다");

            if (AddressableAssetSettings == null)
            {
                Debug.LogWarning("[어드레서블 AutoLabel 스크립트 생성] 어드레서블 설정을 찾을 수 없음");
                return;
            }


            if (IsNull_autoLabelScriptFolder)
            {
                Debug.LogWarning("[어드레서블 AutoLabel 스크립트 생성] 저장 경로가 지정되지 않음");
                return;
            }


            string folderPath = Path.Combine(Application.dataPath, AutoScriptFolder);


            if (!Directory.Exists(folderPath))
            {
                Debug.LogError($"[어드레서블 AutoLabel 스크립트 생성] 저장 경로가 유효하지 않음: {folderPath}");
                return;
            }

            //. Addressables 설정의 현재 label 순서를 그대로 보존해 기존 enum 숫자 값이 불필요하게 바뀌지 않게 합니다.
            List<string> labels = AddressableAssetSettings.GetLabels();
            string scriptPath = Path.Combine(folderPath, $"{AddressableAutoLabelScriptName}.cs");

            GeneratedCSharpFile.Write(scriptPath, BuildAutoLabelSource(labels));

            AssetDatabase.Refresh();

            Debug.Log("[어드레서블 AutoLabel 스크립트 생성] 완료");
        }


        private string BuildAutoLabelSource(IReadOnlyList<string> labels)
        {
            var source = new StringBuilder(256 + labels.Count * 24);
            GeneratedCSharpFile.AppendHeader(
                source,
                "Addressables 설정의 label 목록으로부터 생성되는 enum입니다.");

            source.Append("public enum ").Append(AddressableAutoLabelEnumName).AppendLine();
            source.AppendLine("{");

            for (int i = 0; i < labels.Count; i++)
            {
                source.Append("    ").Append(labels[i]);
                if (i < labels.Count - 1)
                {
                    source.Append(',');
                }

                source.AppendLine();
            }

            source.AppendLine("}");
            return source.ToString();
        }



        ///======================================================================================================================================================



        //? 스크립트 생성: 어드레서블 패키지 스크립트, SO 생성



        [TitleGroup("스크립트 자동 생성"), BoxGroup("스크립트 자동 생성/어드레서블 패키지 생성")]
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly, LabelText("어드레서블 패키지 SO 스크립트 이름")]
        private string PanAddressablePackageSbjectName => "PanAddressablePackageSbject";



        [TitleGroup("스크립트 자동 생성"), BoxGroup("스크립트 자동 생성/어드레서블 패키지 생성")]
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly, LabelText("생성할 어드레서블 패키지 SO 이름")]
        private string PanAddressablePackName => "PanAddressablePack.asset";



        [TitleGroup("스크립트 자동 생성"), BoxGroup("스크립트 자동 생성/어드레서블 패키지 생성")]
        [Button("어드레서블 패키지 스크립트,SO 생성", Icon = SdfIconType.LightningFill), GUIColor(0.97f, 0.85f, 0.39f)]
        private void GenerateAddressablePackageScriptAndSbject()
        {
            Debug.Log("[어드레서블 Package 스크립트 생성] 을 시작합니다");

            if (AddressableAssetSettings == null)
            {
                Debug.LogWarning("[어드레서블 Package 스크립트 생성] 어드레서블 설정을 찾을 수 없음");
                return;
            }


            if (IsNull_autoLabelScriptFolder)
            {
                Debug.LogWarning("[어드레서블 Package 스크립트 생성] 저장 경로가 지정되지 않음");
                return;
            }


            string folderPath = Path.Combine(Application.dataPath, AutoScriptFolder);


            if (!Directory.Exists(folderPath))
            {
                Debug.LogError($"[어드레서블 Package 스크립트 생성] 저장 경로가 유효하지 않음: {folderPath}");
                return;
            }


            string scriptPath = Path.Combine(folderPath, $"{PanAddressablePackageSbjectName}.cs");


            if (!File.Exists(scriptPath))
            {
                GeneratedCSharpFile.Write(scriptPath, BuildAddressablePackageSource());

                AssetDatabase.Refresh();

                Debug.LogWarning($"[어드레서블 Package 스크립트 생성] 스크립트 생성 성공: {scriptPath}");
            }
            else
            {
                Debug.LogWarning($"[어드레서블 Package 스크립트 생성] 이미 스크립트가 존재하여, 스크립트는 생성하지 않음: {folderPath}");
            }


            string scriptableObjectPath = Path.Combine(PanAddressableEditorWindow.GetAddressablesConfigFolder(false), PanAddressablePackName);


            if (!File.Exists(scriptableObjectPath))
            {
                var instance = ScriptableObject.CreateInstance(PanAddressablePackageSbjectName);
                AssetDatabase.CreateAsset(instance, scriptableObjectPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.LogWarning($"[어드레서블 Package 스크립트 생성] SO 생성 성공: {scriptableObjectPath}");
            }
            else
            {
                Debug.LogWarning($"[어드레서블 Package 스크립트 생성] 이미 ScriptableObject가 존재하여, 스크립트는 생성하지 않음: {scriptableObjectPath}");
            }
        }


        private string BuildAddressablePackageSource()
        {
            var source = new StringBuilder(512);
            GeneratedCSharpFile.AppendHeader(
                source,
                "프로젝트의 Addressables package ScriptableObject 형식을 선언합니다.");

            source.Append("using ").Append(nameof(Pan)).Append('.').Append(nameof(Pan.AddressableManagers)).AppendLine(";");
            source.AppendLine();
            source.Append("public class ").Append(PanAddressablePackageSbjectName)
                .Append(" : PanAddressablePackageSbjectBase<")
                .Append(AddressableAutoLabelEnumName).Append(", ")
                .Append(PanAddressablePackageSbjectName).AppendLine(".Pack>");
            source.AppendLine("{");
            source.AppendLine("    public class Pack : BasePack");
            source.AppendLine("    {");
            source.AppendLine("    }");
            source.AppendLine("}");
            return source.ToString();
        }



        ///======================================================================================================================================================
    }
}
