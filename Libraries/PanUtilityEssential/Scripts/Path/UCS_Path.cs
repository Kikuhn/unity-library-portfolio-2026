using System.IO;
using UnityEngine;
using System;
using System.Linq;



//? 경로 관련이 저장되어있는 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    public static class SU_PathFolder
    {///======================================================================================================================================================



        public const string NAME_ASSETS = "Assets";



        ///======================================================================================================================================================



        /// <summary>
        /// 주어진 경로에서 특정 문자를 포함하는 경로 요소를 제거한 새 경로를 반환합니다.
        /// </summary>
        /// <param name="path">대상 경로 문자열</param>
        /// <param name="filterChar">제거할 문자를 포함하는 경로 요소</param>
        /// <returns>특정 문자를 포함하는 요소가 제거된 경로 문자열</returns>
        public static string RemovePathSegmentsContainingChar(this string path, char filterChar = '@')
        {
            //? 경로를 '\' 기준으로 분할하여 배열 생성
            var segments = path.Split('\\');

            //? filterChar를 포함하지 않는 경로 요소들로 새로운 경로 생성
            return string.Join("\\", segments.Where(segment => !string.IsNullOrEmpty(segment) && !segment.Contains(filterChar)));
        }



        /// <summary>
        /// 지정된 경로 문자열에서 마지막 폴더의 이름을 반환합니다.
        /// </summary>
        /// <param name="path">경로 문자열</param>
        /// <returns>마지막 폴더의 이름</returns>
        public static string GetLastDirectoryName(this string path)
        {
            //? Path.GetDirectoryName 메서드를 사용하여 상위 디렉터리 경로를 가져옵니다.
            var directory = Path.GetDirectoryName(path);

            //? 상위 디렉터리 경로가 null이 아닌 경우, 해당 경로에서 마지막 폴더 이름을 추출합니다.
            return directory != null ? Path.GetFileName(directory) : null;
        }



        /// <summary>
        /// 지정된 경로에서 마지막 폴더 이름과 파일 이름을 결합하여 반환합니다.
        /// </summary>
        /// <param name="path">파일 경로 문자열</param>
        /// <param name="includeExtension">파일 확장자를 포함할지 여부</param>
        /// <param name="separator">폴더 이름과 파일 이름 사이에 삽입할 구분자</param>
        /// <returns>마지막 폴더 이름과 파일 이름이 결합된 문자열</returns>
        public static string CombineLastDirectoryAndFileName(this string path, bool includeExtension = false, string separator = "_")
        {
            //? 경로에서 마지막 폴더 이름을 가져옵니다.
            string lastDirectoryName = Path.GetFileName(Path.GetDirectoryName(path));

            //? 파일 이름을 확장자 포함 여부에 따라 가져옵니다.
            string fileName = includeExtension ? Path.GetFileName(path) : Path.GetFileNameWithoutExtension(path);

            //? 마지막 폴더 이름과 파일 이름을 구분자로 결합하여 반환합니다.
            return $"{lastDirectoryName}{separator}{fileName}";
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================



    #region 폐기된 SU_Legacy_Folder

    //    ///<summary>(Legacy) 스태틱 유틸리티 폴더 이름</summary> 
    //    [Obsolete("폐기요망")]
    //    public static class SU_Legacy_Folder
    //    {
    //        static SU_Legacy_Folder()
    //        {
    //#if UNITY_EDITOR
    //            AssetPath = Application.streamingAssetsPath;
    //            Debug.Log("최초실행! 에디터에요");

    //#elif UNITY_ANDROID
    //        AssetPath = Application.persistentDataPath;
    //Debug.Log("최초실행! 안드로이드에요");

    //#endif


    //            AssetBD = Path.Combine(AssetPath, name_AssetBD);
    //            Debug.Log("결국 AssetPath는 " + AssetPath);
    //            Debug.Log("AssetBD는" + AssetBD);
    //        }



    //        public const string ASSETS = "Assets";


    //        public const string name_AllStarResources = "AllStar/Resources";
    //        public const string name_AllStarResourcesBREAK = "AllStar/ResourcesBreak";



    //        public const string name_AssetBD = "AssetBD";
    //        public const string name_AssetBDEncrypted = "AssetBD_Enctypted";



    //        //! 생성자에서 기기별로 다르게 설정
    //        public static readonly string AssetPath;



    //        public static readonly string AssetBD;
    //        //    {
    //        //        get
    //        //        {

    //        //#if UNITY_EDITOR
    //        //            Debug.Log("에디터에서불러와");
    //        //            return Path.Combine(Application.streamingAssetsPath, name_AssetBD);
    //        //#elif UNITY_ANDROID
    //        //            Debug.Log("안드에서불러와: " + Path.Combine(Application.persistentDataPath, name_AssetBD));
    //        //            return Path.Combine(Application.persistentDataPath, name_AssetBD);
    //        //#endif
    //        //        }
    //        //    }



    //        public static string AssetBD_Encrypted
    //        {
    //            get
    //            {
    //                return Path.Combine(Application.streamingAssetsPath, name_AssetBDEncrypted);
    //            }
    //        }


    //        public enum ERes
    //        {
    //            Auto, Resources, ResourcesBreak
    //        }



    //        public static string AllStarResources(ERes type = ERes.Auto, bool dataPath = true)
    //        {
    //            switch (type)
    //            {
    //                case ERes.Auto:

    //                if (Directory.Exists(AllStarResources_Path(true))) return AllStarResources_Path(dataPath);
    //                if (Directory.Exists(AllStarResourcesBreak_Path(true))) return AllStarResourcesBreak_Path(dataPath);

    //                break;

    //                case ERes.Resources: return AllStarResources_Path(dataPath);
    //                case ERes.ResourcesBreak: return AllStarResourcesBreak_Path(dataPath);
    //            }

    //            Debug.LogError("올스타 리소스 폴더 불러오기 실패");
    //            return null;
    //        }



    //        private static string AllStarResources_AutoPath(bool dataPath = true)
    //        {
    //            if (Directory.Exists(AllStarResources_Path(true))) return AllStarResources_Path(dataPath);
    //            if (Directory.Exists(AllStarResourcesBreak_Path(true))) return AllStarResourcesBreak_Path(dataPath);

    //            Debug.LogError("올스타 리소스 폴더 불러오기 실패");
    //            return null;
    //        }


    //        private static string AllStarResources_Path(bool dataPath = true)
    //        {
    //            if (dataPath) return Path.Combine(Application.dataPath, name_AllStarResources);
    //            else return name_AllStarResources;
    //        }


    //        private static string AllStarResourcesBreak_Path(bool dataPath = true)
    //        {
    //            if (dataPath) return Path.Combine(Application.dataPath, name_AllStarResourcesBREAK);
    //            else return name_AllStarResources;
    //        }
    //    } 

    #endregion



    ///======================================================================================================================================================
}
