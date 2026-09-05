using Pan.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;


//? 에디터에서 사용하는 경로 관련 코드들이 저장되어있는 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    public static class SU_EditorPath
    {
        public struct AssetWithPath<T> where T : Object
        {
            public T Asset;
            public string Path;

            public AssetWithPath(T asset, string path)
            {
                Asset = asset;
                Path = path;
            }
        }



        /// <summary>
        /// 프로젝트 내에서 이름으로 단일 에셋 인스턴스를 찾습니다.
        /// </summary>
        /// <typeparam name="T">찾고자 하는 에셋의 타입</typeparam>
        /// <param name="name">찾고자 하는 에셋의 이름</param>
        /// <param name="searchReverse">역순으로 찾을지 여부 (기본값: false)</param>
        /// <returns>찾은 에셋 인스턴스, 찾지 못하면 null</returns>
        public static T FindAsset<T>(string name, bool searchReverse = false) where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (searchReverse)
            {
                for (int i = guids.Length - 1; i >= 0; i--)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                    if (asset != null && asset.name == name)
                    {
                        return asset;
                    }
                }
            }
            else
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                    if (asset != null && asset.name == name)
                    {
                        return asset;
                    }
                }
            }
            Debug.LogWarning($"Asset of type {typeof(T).Name} with name {name} not found.");
            return null;
        }



        /// <summary>
        /// 프로젝트 내에서 이름으로 단일 에셋 인스턴스와 경로를 찾습니다.
        /// </summary>
        /// <typeparam name="T">찾고자 하는 에셋의 타입</typeparam>
        /// <param name="name">찾고자 하는 에셋의 이름</param>
        /// <param name="searchReverse">역순으로 찾을지 여부 (기본값: false)</param>
        /// <returns>찾은 에셋 인스턴스와 경로, 찾지 못하면 null</returns>
        public static AssetWithPath<T>? FindAssetWithPath<T>(string name, bool searchReverse = false) where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (searchReverse)
            {
                for (int i = guids.Length - 1; i >= 0; i--)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                    if (asset != null && asset.name == name)
                    {
                        return new AssetWithPath<T>(asset, path);
                    }
                }
            }
            else
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                    if (asset != null && asset.name == name)
                    {
                        return new AssetWithPath<T>(asset, path);
                    }
                }
            }
            Debug.LogWarning($"Asset of type {typeof(T).Name} with name {name} not found.");
            return null;
        }



        /// <summary>
        /// 프로젝트 내에서 모든 에셋 인스턴스를 찾습니다.
        /// </summary>
        /// <typeparam name="T">찾고자 하는 에셋의 타입</typeparam>
        /// <returns>찾은 에셋 인스턴스들의 리스트</returns>
        public static List<T> FindAllAssets<T>() where T : Object
        {
            List<T> assets = new List<T>();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }
            return assets;
        }



        /// <summary>
        /// 프로젝트 내에서 모든 에셋 인스턴스와 경로를 찾습니다.
        /// </summary>
        /// <typeparam name="T">찾고자 하는 에셋의 타입</typeparam>
        /// <returns>찾은 에셋 인스턴스와 경로의 리스트</returns>
        public static List<AssetWithPath<T>> FindAllAssetsWithPath<T>() where T : Object
        {
            List<AssetWithPath<T>> assets = new List<AssetWithPath<T>>();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    assets.Add(new AssetWithPath<T>(asset, path));
                }
            }
            return assets;
        }



        /// <summary>
        /// 프로젝트 내에서 모든 에셋 인스턴스를 배열로 찾습니다.
        /// </summary>
        /// <typeparam name="T">찾고자 하는 에셋의 타입</typeparam>
        /// <returns>찾은 에셋 인스턴스들의 배열</returns>
        public static T[] FindAllAssetsAsArray<T>() where T : Object
        {
            List<T> assets = FindAllAssets<T>();
            return assets.ToArray();
        }



        /// <summary>
        /// 프로젝트 내에서 모든 에셋 인스턴스와 경로를 배열로 찾습니다.
        /// </summary>
        /// <typeparam name="T">찾고자 하는 에셋의 타입</typeparam>
        /// <returns>찾은 에셋 인스턴스와 경로의 배열</returns>
        public static AssetWithPath<T>[] FindAllAssetsWithPathAsArray<T>() where T : Object
        {
            List<AssetWithPath<T>> assets = FindAllAssetsWithPath<T>();
            return assets.ToArray();
        }



        /// <summary>
        /// 프로젝트 내에서 이름 패턴으로 여러 에셋 인스턴스를 찾습니다.
        /// </summary>
        /// <typeparam name="T">찾고자 하는 에셋의 타입</typeparam>
        /// <param name="namePattern">이름 패턴 (예: "Player*")</param>
        /// <returns>찾은 에셋 인스턴스들의 리스트</returns>
        public static List<T> FindAssetsByNamePattern<T>(string namePattern) where T : Object
        {
            List<T> assets = new List<T>();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null && asset.name.Contains(namePattern))
                {
                    assets.Add(asset);
                }
            }
            return assets;
        }



        /// <summary>
        /// 프로젝트 내에서 이름 패턴으로 여러 에셋 인스턴스와 경로를 찾습니다.
        /// </summary>
        /// <typeparam name="T">찾고자 하는 에셋의 타입</typeparam>
        /// <param name="namePattern">이름 패턴 (예: "Player*")</param>
        /// <returns>찾은 에셋 인스턴스와 경로의 리스트</returns>
        public static List<AssetWithPath<T>> FindAssetsWithPathByNamePattern<T>(string namePattern) where T : Object
        {
            List<AssetWithPath<T>> assets = new List<AssetWithPath<T>>();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null && asset.name.Contains(namePattern))
                {
                    assets.Add(new AssetWithPath<T>(asset, path));
                }
            }
            return assets;
        }



        /// <summary>
        /// 프로젝트 내에서 이름 패턴으로 여러 에셋 인스턴스를 배열로 찾습니다.
        /// </summary>
        /// <typeparam name="T">찾고자 하는 에셋의 타입</typeparam>
        /// <param name="namePattern">이름 패턴 (예: "Player*")</param>
        /// <returns>찾은 에셋 인스턴스들의 배열</returns>
        public static T[] FindAssetsByNamePatternAsArray<T>(string namePattern) where T : Object
        {
            List<T> assets = FindAssetsByNamePattern<T>(namePattern);
            return assets.ToArray();
        }



        /// <summary>
        /// 프로젝트 내에서 이름 패턴으로 여러 에셋 인스턴스와 경로를 배열로 찾습니다.
        /// </summary>
        /// <typeparam name="T">찾고자 하는 에셋의 타입</typeparam>
        /// <param name="namePattern">이름 패턴 (예: "Player*")</param>
        /// <returns>찾은 에셋 인스턴스와 경로의 배열</returns>
        public static AssetWithPath<T>[] FindAssetsWithPathByNamePatternAsArray<T>(string namePattern) where T : Object
        {
            List<AssetWithPath<T>> assets = FindAssetsWithPathByNamePattern<T>(namePattern);
            return assets.ToArray();
        }



        /// <summary>
        /// 프로젝트 내에서 모든 에셋 인스턴스를 지정된 컬렉션에 추가합니다.
        /// </summary>
        /// <typeparam name="T">찾고자 하는 에셋의 타입</typeparam>
        /// <param name="collection">에셋을 추가할 컬렉션</param>
        public static void FindAllAssetsToCollection<T>(ICollection<T> collection) where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    collection.Add(asset);
                }
            }
        }



        /// <summary>
        /// 프로젝트 내에서 모든 에셋 인스턴스와 경로를 지정된 컬렉션에 추가합니다.
        /// </summary>
        /// <typeparam name="T">찾고자 하는 에셋의 타입</typeparam>
        /// <param name="collection">에셋과 경로를 추가할 컬렉션</param>
        public static void FindAllAssetsWithPathToCollection<T>(ICollection<AssetWithPath<T>> collection) where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    collection.Add(new AssetWithPath<T>(asset, path));
                }
            }
        }



        /// <summary>
        /// 프로젝트 내에서 이름 패턴으로 여러 에셋 인스턴스를 지정된 컬렉션에 추가합니다.
        /// </summary>
        /// <typeparam name="T">찾고자 하는 에셋의 타입</typeparam>
        /// <param name="namePattern">이름 패턴 (예: "Player*")</param>
        /// <param name="collection">에셋을 추가할 컬렉션</param>
        public static void FindAssetsByNamePatternToCollection<T>(string namePattern, ICollection<T> collection) where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null && asset.name.Contains(namePattern))
                {
                    collection.Add(asset);
                }
            }
        }



        /// <summary>
        /// 프로젝트 내에서 이름 패턴으로 여러 에셋 인스턴스와 경로를 지정된 컬렉션에 추가합니다.
        /// </summary>
        /// <typeparam name="T">찾고자 하는 에셋의 타입</typeparam>
        /// <param name="namePattern">이름 패턴 (예: "Player*")</param>
        /// <param name="collection">에셋과 경로를 추가할 컬렉션</param>
        public static void FindAssetsWithPathByNamePatternToCollection<T>(string namePattern, ICollection<AssetWithPath<T>> collection) where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null && asset.name.Contains(namePattern))
                {
                    collection.Add(new AssetWithPath<T>(asset, path));
                }
            }
        }
    }



    ///======================================================================================================================================================
}