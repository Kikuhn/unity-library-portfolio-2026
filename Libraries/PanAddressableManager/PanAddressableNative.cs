using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;



namespace Pan.AddressableManagers
{
    /// <summary>
    /// 어드레서블 코드에 직접 접근하는 메서드들을 래핑한 전역 클래스
    /// </summary>
    [Serializable]
    public static class PanAddressableNative
    {
        ///======================================================================================================================================================



        //? 단일 에셋 불러오기



        ///<summary>
        /// [비동기 핸들 반환] 어드레서블 에셋을 불러와 <see cref="AsyncOperationHandle{T}"/> 를 반환
        ///</summary>
        public static AsyncOperationHandle<T> LoadAssetAsyncHandle<T>(string key) where T : Object
        {
            return Addressables.LoadAssetAsync<T>(key);
        }



        ///<summary>
        /// [비동기 핸들 OnCompleted] 어드레서블 에셋을 비동기로 불러온뒤 <paramref name="onCompleted"/>로 확인
        ///</summary>
        public static void LoadAssetAsyncCallback<T>(string key, Action<AsyncOperationHandle<T>> onCompleted) where T : Object
        {
            var addressableHandle = LoadAssetAsyncHandle<T>(key);
            addressableHandle.Completed += onCompleted;
        }



        ///<summary>
        /// [동기 에셋 반환] 어드레서블 에셋을 불러와 Result를 즉시 반환
        ///</summary>
        public static T LoadAsset<T>(string key, out AsyncOperationHandle<T> addressableHandle) where T : Object
        {
            addressableHandle = LoadAssetAsyncHandle<T>(key);
            addressableHandle.WaitForCompletion();
            return addressableHandle.Result;
        }



        ///======================================================================================================================================================



        //? 단일 에셋 SubAssetList로 불러오기



        ///<summary>
        /// [비동기 핸들 반환] 어드레서블 에셋을 SubAssetList로 불러와 <see cref="AsyncOperationHandle{T}"/> 를 반환
        ///</summary>
        public static AsyncOperationHandle<IList<T>> LoadSubAssetListAsyncHandle<T>(string key) where T : Object
        {
            return Addressables.LoadAssetAsync<IList<T>>(key);
        }



        ///<summary>
        /// [비동기 핸들 OnCompleted] 어드레서블 SubAsset으로 불러와 <see cref="IList{T}"/>로 <paramref name="onCompleted"/>로 확인
        ///</summary>
        public static void LoadSubAssetListAsyncCallback<T>(string key, Action<AsyncOperationHandle<IList<T>>> onCompleted) where T : Object
        {
            var addressableHandle = LoadSubAssetListAsyncHandle<T>(key);
            addressableHandle.Completed += onCompleted;
        }



        ///<summary>
        /// [동기 에셋 반환] 어드레서블 에셋을 SubAssetList로 불러와 Result를 즉시 반환
        ///</summary>
        public static IList<T> LoadSubAssetList<T>(string key, out AsyncOperationHandle<IList<T>> addressableHandle) where T : Object
        {
            addressableHandle = LoadSubAssetListAsyncHandle<T>(key);
            addressableHandle.WaitForCompletion();
            return addressableHandle.Result;
        }



        ///======================================================================================================================================================



        //? 복수 에셋 불러오기 (단일 키)



        ///<summary>
        /// [비동기 핸들 반환] 어드레서블 에셋들을 불러와 <see cref="AsyncOperationHandle{IList{T}}"/> 를 반환
        ///</summary>
        public static AsyncOperationHandle<IList<T>> LoadAssetsAsyncHandle<T>(string key, Action<T> callback) where T : Object
        {
            return Addressables.LoadAssetsAsync(key, callback);
        }



        ///<summary>
        /// [비동기 핸들 OnCompleted] 어드레서블 에셋들을 비동기로 불러온뒤 <paramref name="onCompleted"/>로 확인
        ///</summary>
        public static void LoadAssetsAsyncCallback<T>(string key, Action<T> callback, Action<AsyncOperationHandle<IList<T>>> onCompleted) where T : Object
        {
            var addressableHandle = LoadAssetsAsyncHandle(key, callback);
            addressableHandle.Completed += onCompleted;
        }



        ///<summary>
        /// [동기 에셋 반환] 어드레서블 에셋들을 불러와 Result를 즉시 반환
        ///</summary>
        public static IList<T> LoadAssets<T>(string key, Action<T> callback, out AsyncOperationHandle<IList<T>> addressableHandle) where T : Object
        {
            addressableHandle = LoadAssetsAsyncHandle(key, callback);
            addressableHandle.WaitForCompletion();
            return addressableHandle.Result;
        }




        //? 복수 에셋 불러오기 (멀티 키)



        ///<summary>
        /// [비동기 핸들 반환] 어드레서블 에셋들을 불러와 <see cref="AsyncOperationHandle{IList{T}}"/> 를 반환
        ///</summary>
        public static AsyncOperationHandle<IList<T>> LoadAssetsAsyncHandle<T>(IEnumerable<string> keys, Action<T> callback, Addressables.MergeMode mergeMode) where T : Object
        {
            return Addressables.LoadAssetsAsync(keys, callback, mergeMode);
        }



        ///<summary>
        /// [비동기 핸들 OnCompleted] 어드레서블 에셋들을 비동기로 불러온뒤 <paramref name="onCompleted"/>로 확인
        ///</summary>
        public static void LoadAssetsAsyncCallback<T>(IEnumerable<string> keys, Action<T> callback, Addressables.MergeMode mergeMode, Action<AsyncOperationHandle<IList<T>>> onCompleted) where T : Object
        {
            var addressableHandle = LoadAssetsAsyncHandle(keys, callback, mergeMode);
            addressableHandle.Completed += onCompleted;
        }



        ///<summary>
        /// [동기 에셋 반환] 어드레서블 에셋들을 불러와 Result를 즉시 반환
        ///</summary>
        public static IList<T> LoadAssets<T>(IEnumerable<string> keys, Action<T> callback, Addressables.MergeMode mergeMode, out AsyncOperationHandle<IList<T>> addressableHandle) where T : Object
        {
            addressableHandle = LoadAssetsAsyncHandle<T>(keys, callback, mergeMode);
            addressableHandle.WaitForCompletion();
            return addressableHandle.Result;
        }



        ///======================================================================================================================================================



        #region 폐기된 복수 에셋 SubAssetList로 불러오기

        ////? 폐기된 복수 에셋 SubAssetList로 불러오기



        //[Obsolete]
        //public AsyncOperationHandle<IList<IList<T>>> LoadSubAssetsListAsyncHandle<T>(IEnumerable<string> keys, Action<IList<T>> callback, Addressables.MergeMode mergeMode) where T : Object
        //{
        //    return Addressables.LoadAssetsAsync(keys, callback, mergeMode);
        //}

        //[Obsolete]
        //public void LoadSubAssetsListCallback<T>(IEnumerable<string> keys, Action<IList<T>> callback, Addressables.MergeMode mergeMode, Action<AsyncOperationHandle<IList<IList<T>>>> onComplete) where T : Object
        //{
        //    var addressableHandle = LoadSubAssetsListAsyncHandle(keys, callback, mergeMode);
        //    addressableHandle.Completed += onComplete;
        //}

        //[Obsolete]
        //public IList<IList<T>> LoadSubAssetsList<T>(IEnumerable<string> keys, Action<IList<T>> callback, Addressables.MergeMode mergeMode, out AsyncOperationHandle<IList<IList<T>>> addressableHandle) where T : Object
        //{
        //    addressableHandle = LoadSubAssetsListAsyncHandle(keys, callback, mergeMode);
        //    addressableHandle.WaitForCompletion();
        //    return addressableHandle.Result;
        //}




        //public async UniTask<IList<T>> LoadSubAssetsListAsyncHandle000_<T>(IEnumerable<string> keys, Action<IList<T>> callback, Addressables.MergeMode mergeMode) where T : Object
        //{
        //    var locationHandlee = Addressables.LoadResourceLocationsAsync(keys, mergeMode, typeof(T));

        //    await locationHandlee.Task;

        //    if (locationHandlee.Status != AsyncOperationStatus.Succeeded)
        //    {
        //        return null;
        //    }

        //    var r = locationHandlee.Result;


        //    List<T> resultList = new List<T>(r.Count); //! 실제론 더커질듯

        //    for (int i = 0; i < r.Count; i++)
        //    {
        //        var current = r[i];
        //        var tt = await Addressables.LoadAssetAsync<IList<T>>(current);

        //        for (int j = 0; j < tt.Count; j++)
        //        {

        //            resultList.Add(tt[j]);
        //        }
        //    }

        //    return resultList;
        //}


        ///// <summary>
        ///// 여러 Key에 포함된 모든 서브 에셋(T)을 한 번에 로드하여 리스트로 반환
        ///// </summary>
        //public async UniTask<IList<T>> LoadSubAssetsListAsyncHandle000<T>(
        //    IEnumerable<string> keys,
        //    Action<IList<T>> callback,
        //    Addressables.MergeMode mergeMode) where T : Object
        //{
        //    //? 1) Key → IResourceLocation 조회
        //    var locHandle = Addressables.LoadResourceLocationsAsync(keys, mergeMode, typeof(T));
        //    await locHandle.Task;

        //    if (locHandle.Status != AsyncOperationStatus.Succeeded)
        //    {
        //        Addressables.Release(locHandle);   //! 위치 핸들 해제
        //        return null;
        //    }

        //    //? 2) 조회된 위치들을 한 번에 로드 (내부적으로 병렬 처리)
        //    var assetsHandle = Addressables.LoadAssetsAsync<T>(locHandle.Result, null, true);
        //    await assetsHandle.Task;

        //    Addressables.Release(locHandle);       //. 더 이상 필요 없는 위치 핸들 해제

        //    if (assetsHandle.Status != AsyncOperationStatus.Succeeded)
        //    {
        //        Addressables.Release(assetsHandle);
        //        return null;
        //    }

        //    var resultList = assetsHandle.Result;   //. 최종 결과

        //    callback?.Invoke(resultList);           //? 외부 콜백 실행

        //    //! 호출 측에서 사용이 끝나면 반드시 Addressables.Release(assetsHandle) 할 것
        //    return resultList;
        //}



        ///// <summary>
        ///// 여러 Key 에 포함된 모든 Sub-Asset(T)을 병렬로 로드하여 하나의 리스트로 병합<br/>
        ///// – Slice Sprite 전부 / FBX AnimationClip 전부 등 지원
        ///// </summary>
        //public async UniTask<IList<T>> LoadSubAssetsListAsyncHandle0000<T>(
        //    IEnumerable<string> keys,
        //    Action<IList<T>> callback,
        //    Addressables.MergeMode mergeMode) where T : Object
        //{
        //    //? 1) Key → IResourceLocation 조회 ─ 실패 시 null 반환
        //    var locHandle = Addressables.LoadResourceLocationsAsync(keys, mergeMode, typeof(T));
        //    await locHandle.Task;
        //    if (locHandle.Status != AsyncOperationStatus.Succeeded) return null;

        //    //. 2) 위치마다 LoadAssetAsync<IList<T>> 생성 (아직 Await 안 함)
        //    var locations = locHandle.Result;
        //    var loadHandles = new AsyncOperationHandle<IList<T>>[locations.Count];
        //    for (int i = 0; i < locations.Count; ++i)
        //        loadHandles[i] = Addressables.LoadAssetAsync<IList<T>>(locations[i]);

        //    //. 3) 전부 병렬 Await – 디스크/네트워크 IO 동시 처리
        //    await UniTask.WhenAll(loadHandles.Select(h => h.ToUniTask()));

        //    //. 4) 최종 리스트 용량 미리 산출 → 할당 최소화
        //    int capacity = 0;
        //    for (int i = 0; i < loadHandles.Length; ++i)
        //    {
        //        var h = loadHandles[i];
        //        if (h.Status == AsyncOperationStatus.Succeeded && h.Result != null)
        //            capacity += h.Result.Count;
        //    }

        //    List<T> resultList = new List<T>(capacity);

        //    //. 5) 결과 병합 (AddRange → 중간 GC 없음)
        //    for (int i = 0; i < loadHandles.Length; ++i)
        //    {
        //        var list = loadHandles[i].Result;
        //        if (list != null) resultList.AddRange(list);
        //    }

        //    //. 6) 외부 콜백
        //    callback?.Invoke(resultList);

        //    //! 원본 구현과 동일하게, 핸들은 호출-측에서 Release 하도록 남겨둡니다
        //    //    필요하면 여기서 foreach(Addressables.Release(handle)) 해제 가능
        //    return resultList;
        //} 

        #endregion



        ///======================================================================================================================================================



        //? 에셋 해제하기 (Release)



        ///<summary>
        /// 에셋 해제 (Release)
        ///</summary>
        public static void Release(AsyncOperationHandle addressableHandle)
        {
            Addressables.Release(addressableHandle);
        }



        ///<summary>
        /// 에셋 해제 (Release)
        ///</summary>
        public static void Release<T>(AsyncOperationHandle<T> addressableHandle) where T : class
        {
            Addressables.Release(addressableHandle);
        }



        ///<summary>
        /// 에셋 해제 (Release)
        ///</summary>
        public static void Release<T>(T obj) where T : class
        {
            Addressables.Release(obj);
        }



        ///======================================================================================================================================================



        //? 어드레서블을 통해 직접 생성된 게임오브젝트 에셋 해제하기 (ReleaseInstance)



        ///<summary>
        /// <see cref="Addressables.InstantiateAsync"/>로 생성한 <see cref="GameObject"/>를  해제하기
        ///</summary>
        public static bool ReleaseGameObjectInstance(AsyncOperationHandle handle)
        {
            return Addressables.ReleaseInstance(handle);
        }



        ///<summary>
        /// <see cref="Addressables.InstantiateAsync"/>로 생성한 <see cref="GameObject"/>를  해제하기
        ///</summary>
        public static bool ReleaseGameObjectInstance(AsyncOperationHandle<GameObject> handle)
        {
            return Addressables.ReleaseInstance(handle);
        }



        ///<summary>
        /// <see cref="Addressables.InstantiateAsync"/>로 생성한 <see cref="GameObject"/>를  해제하기
        ///</summary>
        public static bool ReleaseGameObjectInstance(GameObject gameObject)
        {
            return Addressables.ReleaseInstance(gameObject);
        }



        ///======================================================================================================================================================
    }

}