using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Threading;
using Cysharp.Threading.Tasks;
using Pan.AddressableManagers;
using System.Linq;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System.Text;
using System.IO;
using System.Data.SqlTypes;
using System.Text.RegularExpressions;
using System;
using UnityEngine.UI;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;
using UnityEditor.Callbacks;



namespace Pan.AddressableManagers.Editor
{
    [InitializeOnLoad] //. 에디터가 켜지거나 도메인이 리로드될 때 static 생성자 실행
    public class PanAddressableEditorWindow : OdinEditorWindow
    {
        ///======================================================================================================================================================



        [MenuItem("판/Addressable 윈도우 소환")]
        private static void Open()
        {
            GetWindow<PanAddressableEditorWindow>().ShowUtility();
        }



        ///======================================================================================================================================================



        /// <summary>
        ///   Addressables Settings 자산이 존재하는 폴더 경로를 얻는다.  
        ///   <para>(Settings 가 아직 없으면 <c>null</c> 반환)</para>
        /// </summary>
        /// <param name="absolutePath">
        ///   <b>true</b> → 프로젝트 루트 기준 **절대 경로**  
        ///   <b>false</b> (기본값) → “Assets/…” 형태의 **상대 경로**
        /// </param>
        public static string GetAddressablesConfigFolder(bool absolutePath = false)
        {
            //. ① Settings 인스턴스를 메모리/캐시에서 즉시 가져옴 (비어 있으면 null)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return null;          //. Addressables 미사용 프로젝트

            //. ② Settings 자산 파일의 **상대** 경로 (예: "Assets/AddressableAssetsData/…")
            string relativeAssetPath = AssetDatabase.GetAssetPath(settings);

            //. ③ 폴더 경로로 변환
            string relativeFolderPath = Path.GetDirectoryName(relativeAssetPath);

            //. ④ 호출 옵션에 따라 상대 ↔ 절대 변환
            if (!absolutePath)
            {
                return relativeFolderPath;              //. 상대 경로 그대로 반환
            }

            //. ── 절대 경로로 변환 ───────────────────────────────────────────────
            // Application.dataPath  : "<프로젝트 루트>/Assets"
            // 프로젝트 루트 구하기    : Assets 상위 폴더 1단계
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string absoluteFolderPath = Path.GetFullPath(Path.Combine(projectRoot, relativeFolderPath));
            return absoluteFolderPath;
        }



        ///======================================================================================================================================================



        [Title("판 어드레서블 에디터 윈도우")]
        [HideLabel]
        [DisplayAsString]
        [ShowInInspector]
        [EnableGUI]
        [PropertyOrder(-1000)]
        private string dummyTitle => "";



        [ShowInInspector]
        [LabelText("어드레서블 에디터 설정 에셋 이름")]
        private static string PanAddressableEditorSettingName => "PanAddressableEditorSetting.asset";



        /// <summary>
        ///   현재 Addressables 설정 경로 기준으로 PanAddressableSettingSbject 자산을 반환  
        ///   존재하지 않으면 자동 생성
        /// </summary>
        public static bool CreatePanAddressableEditorSettingSbject()
        {
            string addressableConfigFolder = GetAddressablesConfigFolder();


            if (addressableConfigFolder == null)
            {
                Debug.LogError("[판 어드레서블 에디터 설정 생성] Addressables 설정 경로를 찾을 수 없습니다.");
                return false;
            }


            string fullPath = Path.Combine(addressableConfigFolder, PanAddressableEditorSettingName);
            string assetPath = fullPath.Replace(Application.dataPath, "Assets"); //. 절대→상대 경로 변환


            //. 이미 존재하면 성공
            var existing = AssetDatabase.LoadAssetAtPath<PanAddressableEditorSettingSbject>(assetPath);
            if (existing != null) return true;


            //. 폴더가 없다면 생성
            if (!Directory.Exists(addressableConfigFolder))
            {
                Directory.CreateDirectory(addressableConfigFolder);
                AssetDatabase.Refresh();
            }


            //. 새로 생성
            var instance = ScriptableObject.CreateInstance<PanAddressableEditorSettingSbject>();
            AssetDatabase.CreateAsset(instance, assetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[판 어드레서블 에디터 설정 생성] ScriptableObject 자동 생성됨: {assetPath}");
            return true;
        }



        [Button("판 어드레서블 에디터 설정 생성하기", Icon = SdfIconType.LightningFill), GUIColor(0.97f, 0.85f, 0.39f)]
        [InfoBox("유니티 AddressableAssetSettingsDefaultObject의 폴더에 생성됨")]
        public static void ExecuteCreatePanAddressableEditorSettingSbject()
        {
            Debug.Log($"[판 어드레서블 에디터 설정 생성] 준비...");
            bool result = CreatePanAddressableEditorSettingSbject();

            string text = $"[판 어드레서블 에디터 설정 생성] 결과: {result}";

            if (result) { Debug.Log(text); }
            else { Debug.LogError(text); }
        }



        [InitializeOnLoadMethod]
        private static void AutoLoad_ExecuteCreatePanAddressableEditorSettingSbject()
        {
            EditorApplication.delayCall -= AutoLoad_CreatePanAddressableEditorSettingSbject;
            EditorApplication.delayCall += AutoLoad_CreatePanAddressableEditorSettingSbject;
        }



        private static void AutoLoad_CreatePanAddressableEditorSettingSbject()
        {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling)
            {
                EditorApplication.delayCall -= AutoLoad_CreatePanAddressableEditorSettingSbject;
                EditorApplication.delayCall += AutoLoad_CreatePanAddressableEditorSettingSbject;
                return;
            }

            if (!AddressableAssetSettingsDefaultObject.SettingsExists) { return; }
            CreatePanAddressableEditorSettingSbject();
        }



        ///======================================================================================================================================================
    }
}
