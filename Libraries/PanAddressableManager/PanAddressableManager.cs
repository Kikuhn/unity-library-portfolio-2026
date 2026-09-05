using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;
using System;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;
using System.Collections;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.AddressableAssets.ResourceLocators;
using System.Threading;
using Pan.AddressableManagers;
using System.IO;
using Sirenix.OdinInspector;



namespace Pan.AddressableManagers
{
    public interface IHoldIPanAddressableManager
    {
        PanAddressableManager AddressableManager { get; }
    }



    public interface IPanAddressableManager
    {
        PanAddressableManager.EventManager Event { get; }
        PanAddressableManager.CachingManager Caching { get; }
    }



    [Serializable]
    public class PanAddressableManager : IPanAddressableManager, IHoldIPanAddressableManager
    {
        ///======================================================================================================================================================



        public PanAddressableManager(int cacheCapacity = 0)
        {
            Event = new EventManager(this);
            Caching = new CachingManager(this, cacheCapacity);
            Loader = new Loaders(this);
            Downloader = new Downloaders(this);
            Initialized = true;
        }



        ///======================================================================================================================================================



        PanAddressableManager IHoldIPanAddressableManager.AddressableManager => this;



        ///======================================================================================================================================================



        [LabelText("이벤트 매니저")]
        [ShowInInspector, HideReferenceObjectPicker] protected readonly EventManager Event;

        [LabelText("캐싱 매니저")]
        [ShowInInspector, HideReferenceObjectPicker] protected readonly CachingManager Caching;



        EventManager IPanAddressableManager.Event => Event;

        CachingManager IPanAddressableManager.Caching => Caching;



        [LabelText("로더 매니저")]
        [ShowInInspector, HideReferenceObjectPicker] public readonly Loaders Loader;

        [LabelText("다운로더 매니저")]
        [ShowInInspector, HideReferenceObjectPicker] public readonly Downloaders Downloader;



        [LabelText("초기화 여부"), ShowInInspector, ReadOnly]
        public readonly bool Initialized;



        ///======================================================================================================================================================



        ///<summary>
        ///  내부 클래스의 베이스
        /// </summary>
        [Serializable]
        public abstract class Base
        {
            public Base(PanAddressableManager manager)
            {
                Manager = manager;
            }



            protected readonly PanAddressableManager Manager;
        }



        /// <summary>
        /// 에셋들을 불러오기 전/후 에서 실행되는 이벤트들을 관리하는 매니저
        /// </summary>
        [Serializable]
        public class EventManager : Base
        {
            ///======================================================================================================================================================



            public EventManager(PanAddressableManager manager) : base(manager)
            {

            }



            ///======================================================================================================================================================



            /// <summary>
            /// 단일 어드레서블 에셋을 불러오기 <b>전</b> 실행되는 이벤트 (어드레서블 이름)
            /// </summary>
            public event Action<string> LoadSingleAssetBeforeEvent;

            /// <summary>
            /// 복수 어드레서블 에셋들을 불러오기 <b>전</b> 실행되는 이벤트 (어드레서블 라벨)
            /// </summary>
            public event Action<string> LoadMultipleAssetBeforeEvent;

            /// <summary>
            /// 복수 어드레서블 에셋들을 불러오기 <b>전</b> 실행되는 이벤트 (어드레서블 라벨들)
            /// </summary>
            public event Action<IEnumerable<string>> LoadMultipleLabelAssetBeforeEvent;



            /// <summary>
            /// 어드레서블 에셋을 성공적으로 불러온 <b>후</b> 실행되는 이벤트
            /// </summary>
            public event Action<AsyncOperationHandle, Object> LoadAssetAfterEvent;



            ///======================================================================================================================================================



            ///<summary>
            /// 단일 에셋을 불러오기 <b>전</b> 실행되는 이벤트
            ///</summary>
            ///<param name="addressableName">어드레서블 이름</param>
            public void LoadAssetBefore(string addressableName)
            {
                LoadSingleAssetBeforeEvent?.Invoke(addressableName);
            }



            ///<summary>
            /// 복수 에셋을 불러오기 <b>전</b> 실행되는 이벤트
            ///</summary>
            ///<param name="addressableLabel">어드레서블 라벨</param>
            public void LoadAssetsBefore(string addressableLabel)
            {
                LoadMultipleAssetBeforeEvent?.Invoke(addressableLabel);
            }



            ///<summary>
            /// 복수 에셋을 불러오기 <b>전</b> 실행되는 이벤트
            ///</summary>
            ///<param name="addressableLabels">어드레서블 라벨들</param>
            public void LoadAssetBefore(IEnumerable<string> addressableLabels)
            {
                LoadMultipleLabelAssetBeforeEvent?.Invoke(addressableLabels);
            }



            ///<summary>
            /// 단일 에셋을 성공적으로 불러온 <b>후</b> 실행되는 이벤트
            ///</summary>
            ///<param name="addressableHandle">성공한 어드레서블 핸들</param>
            public void LoadAssetAfter<T>(in AsyncOperationHandle<T> addressableHandle) where T : Object
            {
                LoadAssetAfterEvent?.Invoke(addressableHandle, addressableHandle.Result);
            }



            ///<summary>
            /// 복수 에셋들을 성공적으로 불러온 <b>후</b> 실행되는 이벤트
            ///</summary>
            public void LoadAssetsAfterEvent<T>(in AsyncOperationHandle<IList<T>> addressableHandle) where T : Object
            {
                if (LoadAssetAfterEvent != null)
                {
                    for (int i = 0; i < addressableHandle.Result.Count; i++)
                    {
                        LoadAssetAfterEvent.Invoke(addressableHandle, addressableHandle.Result[i]);
                    }
                }
            }



            ///======================================================================================================================================================
        }



        /// <summary>
        /// 캐싱 매니저
        /// <para>여기서 캐싱되는 에셋들은, 실제 어드레서블 코드와 연계되지 않음</para>
        /// <para>그냥 저장과 관리만 할 뿐, 어드레서블에서의 해제는 <b>별개로 같이 해줘야 함</b></para>
        /// <para>언제까지나 <b>직접</b> 불러온 에셋들만 관리되며, 의존성에 의해 불러와진 에셋은 별도로 이곳에 저장되지않는다</para>
        /// </summary>
        [Serializable]
        public class CachingManager : Base
        {
            ///======================================================================================================================================================



            public CachingManager(PanAddressableManager manager, int capacity) : base(manager)
            {
                CachingAssets = new Dictionary<Object, int>(capacity);
                ReleaseOrigins = new Dictionary<object, int>(capacity);
            }



            public enum AddCacheStatus { Success, Failed, ReferenceCountAdded }



            ///======================================================================================================================================================



            //? 에셋 / 에셋 리스트를 Key로 캐싱하는 딕셔너리



            ///<summary>
            /// 캐싱된 에셋들을 Key로 사용하고, ReferenceCount를 Value로 캐싱하는 딕셔너리
            ///</summary>
            [ShowInInspector]
            [LabelText("캐시 딕셔너리")]
            [DictionaryDrawerSettings(DisplayMode = DictionaryDisplayOptions.OneLine, IsReadOnly = true, KeyColumnWidth = 200f, KeyLabel = "캐싱 오브젝트", ValueLabel = "레퍼런스 카운트")]
            [Searchable]
            private readonly Dictionary<Object, int> CachingAssets;

            // Addressables releases a multi-load by its IList result, not by each item.
            internal readonly Dictionary<object, int> ReleaseOrigins;



            ///<summary>
            /// 캐싱된 에셋들을 Key로 사용하고, ReferenceCount를 Value로 캐싱하는 딕셔너리 를 얻기
            ///</summary>
            public IReadOnlyDictionary<Object, int> GetCachingAssets => CachingAssets;



            public readonly object lockobj = new object();



            ///======================================================================================================================================================



            //? 캐싱 추가



            private AddCacheStatus AddCacheInternal<T>(T asset) where T : Object
            {
                //! 에셋이 null이라면, 캐싱 추가에 실패한다
                if (asset == null) { return AddCacheStatus.Failed; }


                //? 이미 해당 에셋이 캐싱이 되어있을경우, ReferenceCount만 증가시키고 반환한다
                if (CachingAssets.TryGetValue(asset, out var referenceCount))
                {
                    CachingAssets[asset] = referenceCount + 1;
                    return AddCacheStatus.ReferenceCountAdded;
                }


                //? 새롭게 에셋을 캐싱한다, ReferenceCount는 1로 시작한다
                CachingAssets.Add(asset, 1);
                return AddCacheStatus.Success;
            }

            private void AddReleaseOrigin(object origin)
            {
                if (origin == null) { return; }
                ReleaseOrigins[origin] = ReleaseOrigins.TryGetValue(origin, out var count) ? count + 1 : 1;
            }

            private void RemoveReleaseOrigin(object origin)
            {
                if (origin == null || !ReleaseOrigins.TryGetValue(origin, out var count)) { return; }
                if (count <= 1) { ReleaseOrigins.Remove(origin); }
                else { ReleaseOrigins[origin] = count - 1; }
            }



            ///<summary>
            /// 에셋을 캐싱에 추가한다
            ///</summary>
            public AddCacheStatus AddCache<T>(T asset) where T : Object
            {
                lock (lockobj)
                {
                    var status = AddCacheInternal(asset);
                    if (status != AddCacheStatus.Failed) { AddReleaseOrigin(asset); }
                    return status;
                }
            }



            ///<summary>
            /// 에셋 리스트를 캐싱에 추가한다
            /// <para><see cref="AddCache{T}(T)"/>를 여러번 호출하는 방식이기에, 반환값은 존재하지 않는다</para>
            ///</summary>
            public void AddCache<T>(IList<T> assetList) where T : Object
            {
                lock (lockobj)
                {
                    //! 에셋 리스트가 null이라면, 캐싱 추가에 실패한다
                    if (assetList == null) { return; }


                    //? 에셋 리스트를 순회하면서 에셋들을 각각 캐싱에 추가한다
                    for (int i = 0; i < assetList.Count; i++)
                    {
                        AddCacheInternal(assetList[i]);
                    }

                    AddReleaseOrigin(assetList);
                }
            }



            ///<summary>
            /// 에셋을 캐싱에 추가한다 (어드레서블 핸들)
            ///</summary>
            public AddCacheStatus AddCache<T>(AsyncOperationHandle<T> addressableHandle) where T : Object
            {
                lock (lockobj)
                {
                    //! 핸들이 유효하지않으면, 캐싱 추가에 실패한다
                    if (!addressableHandle.IsValid() || addressableHandle.Status != AsyncOperationStatus.Succeeded || addressableHandle.Result == null) { return AddCacheStatus.Failed; }


                    //. 핸들의 Result를 사용하여 캐싱에 추가한다
                    return AddCache(addressableHandle.Result);
                }
            }



            ///<summary>
            /// 에셋 리스트를 캐싱에 추가한다 (어드레서블 핸들)
            /// <para><see cref="AddCache{T}(T)"/>를 여러번 호출하는 방식이기에, 반환값은 존재하지 않는다</para>
            ///</summary>
            public void AddCache<T>(AsyncOperationHandle<IList<T>> addressableHandle) where T : Object
            {
                lock (lockobj)
                {
                    //! 핸들이 유효하지않으면, 캐싱 추가에 실패한다
                    if (!addressableHandle.IsValid() || addressableHandle.Status != AsyncOperationStatus.Succeeded || addressableHandle.Result == null) { return; }


                    //. 핸들의 Result를 사용하여 캐싱에 추가한다
                    AddCache(addressableHandle.Result);
                }
            }



            ///======================================================================================================================================================



            //? 캐싱 제거



            ///<summary>
            /// 에셋을 캐싱에서 제거한다
            ///</summary>
            ///<returns>
            /// <paramref name="asset"/>이 유효하다면 ReferenceCount를 1 감소시키며, 그로인해 캐싱에서 제거되었는지의 여부를 반환한다
            /// <para>제거가 되었다면 <c>true</c>, 제거가 되지 않았다면 <c>false</c></para>
            /// </returns>
            public bool RemoveCache<T>(T asset) where T : Object
            {
                lock (lockobj)
                {
                    //! 에셋이 null이거나, 해당 에셋이 캐싱되어있지 않다면 실패한다
                    if (asset == null || !CachingAssets.ContainsKey(asset)) { return false; }


                    //. ReferenceCount를 1 감소시키며, 그로인해 캐싱에서 제거되었는지의 여부를 반환한다
                    //. 제거가 되었다면 true, 제거가 되지 않았다면 false
                    bool removed = TryMinusAssetReferenceCount(asset);
                    RemoveReleaseOrigin(asset);
                    return removed;
                }
            }



            ///<summary>
            /// 에셋 리스트를 캐싱에서 제거한다
            ///<para>에셋들중에서 하나라도 제거를 실패할경우, false를 반환하되, 순회를 멈추지는 않는다</para>
            ///</summary>
            ///<returns>
            /// 모든 에셋의 ReferenceCount를 1 감소시키며, 그로인해 캐싱에서 모두 제거되었는지의 여부를 반환한다
            /// <para>모두 제거가 되었다면 <c>true</c>, 하나라도 제거가 되지 않았다면 <c>false</c></para>
            /// </returns>
            public bool RemoveCache<T>(IList<T> assetList) where T : Object
            {
                lock (lockobj)
                {
                    bool successRemove = true; //. 에셋리스트의 모든 에셋들을 제거에 성공했는지 여부


                    //? 에셋 리스트를 순회하며 에셋을 각각 캐싱에서 제거한다
                    for (int i = 0; i < assetList.Count; i++)
                    {
                        //. 하나라도 에셋 제거에 실패할경우, 최종적으로 실패를 반환시키되 순회를 멈추지는 않는다
                        if (assetList[i] == null || !CachingAssets.ContainsKey(assetList[i]))
                        {
                            successRemove = false;
                            continue;
                        }

                        if (TryMinusAssetReferenceCount(assetList[i]) == false) { successRemove = false; }
                    }

                    RemoveReleaseOrigin(assetList);

                    return successRemove;
                }
            }



            ///<summary>
            /// 에셋 리스트를 캐싱에서 제거한다 (어드레서블 핸들)
            ///<para>에셋들중에서 하나라도 제거를 실패할경우, false를 반환하되, 순회를 멈추지는 않는다</para>
            ///<para>무명의 <see cref="AsyncOperationHandle"/>를 사용하기에, 별도로 반환값을 지정하지 않는다 (사유가 너무 많음, 핸들 유효, 캐스팅 실패, 에셋 리스트 각각 실패 여부 등등)</para>
            ///</summary>
            public void RemoveCache(in AsyncOperationHandle addressableHandle)
            {
                lock (lockobj)
                {
                    //! 어드베서블 핸들의 캐싱 유무를 떠나 핸들 자체가 유효하지않으면, 실패한다
                    if (!addressableHandle.IsValid() || addressableHandle.Status != AsyncOperationStatus.Succeeded || addressableHandle.Result == null) { return; }


                    //. 핸들의 반환 타입이 단일 (Object) 이라면 단일 에셋으로 제거한다
                    if (addressableHandle.Result is Object assetObject1) { RemoveCache(assetObject1); }


                    //. 핸들의 반환 타입이 복수 이라면 복수 에셋으로 제거한다, 이곳에서 직접 순회하여 캐스팅한뒤 각각 제거한다
                    else if (addressableHandle.Result is IEnumerable assetList)
                    {
                        //? 에셋 리스트를 순회한다
                        foreach (var asset in assetList)
                        {
                            //. 각각의 에셋을 Object로 캐스팅한뒤, 단일 에셋으로 각각 제거한다
                            if (asset is Object assetObject2 && CachingAssets.ContainsKey(assetObject2))
                            {
                                TryMinusAssetReferenceCount(assetObject2);
                            }
                        }

                        RemoveReleaseOrigin(addressableHandle.Result);
                    }
                }
            }



            ///<summary>
            /// 에셋 리스트를 캐싱에서 제거한다 (어드레서블 핸들)
            ///<para>에셋들중에서 하나라도 제거를 실패할경우, false를 반환하되, 순회를 멈추지는 않는다</para>
            ///</summary>
            ///<returns>
            /// 모든 에셋의 ReferenceCount를 1 감소시키며, 그로인해 캐싱에서 모두 제거되었는지의 여부를 반환한다
            /// <para>모두 제거가 되었다면 <c>true</c>, 하나라도 제거가 되지 않았다면 <c>false</c></para>
            /// </returns>
            public bool RemoveCache<T>(in AsyncOperationHandle<T> addressableHandle) where T : Object
            {
                lock (lockobj)
                {
                    //! 어드베서블 핸들의 캐싱 유무를 떠나 핸들 자체가 유효하지않으면, 실패한다
                    if (!addressableHandle.IsValid() || addressableHandle.Status != AsyncOperationStatus.Succeeded || addressableHandle.Result == null) { return false; }


                    //. 핸들의 Result를 사용하여 캐싱에서 제거한다
                    return RemoveCache(addressableHandle.Result);
                }
            }



            ///<summary>
            ///캐싱 제거 : [by 핸들] (절대 어드레서블과 같이 호출해야함)
            ///</summary>
            public bool RemoveCache<T>(in AsyncOperationHandle<IList<T>> addressableHandle) where T : Object
            {
                lock (lockobj)
                {
                    //! 어드베서블 핸들의 캐싱 유무를 떠나 핸들 자체가 유효하지않으면, 실패한다
                    if (!addressableHandle.IsValid() || addressableHandle.Status != AsyncOperationStatus.Succeeded || addressableHandle.Result == null) { return false; }


                    //. 핸들의 Result를 사용하여 캐싱에서 제거한다
                    return RemoveCache(addressableHandle.Result);
                }
            }



            /// <summary>
            /// 캐싱 되어있는 <paramref name="asset"/>의 ReferenceCount를 1 감소시킨다
            /// <para>감소되어 0 이하가 될 경우, 캐싱에서 제거하며 <c>true</c>를 반환한다</para>
            /// <para>그 외에는 <c>false</c>를 반환한다</para>
            /// </summary>
            private bool TryMinusAssetReferenceCount<T>(T asset) where T : Object
            {
                //. 에셋이 캐싱이 되어있다는것을 전제로 실행되어있음


                //? 해당 에셋의 ReferenceCount를 감소시킨다
                CachingAssets[asset]--;


                //? 감소된 ReferenceCount가 0 이하일경우, 캐싱에서 제거한뒤 true를 반환한다
                if (CachingAssets[asset] <= 0)
                {
                    CachingAssets.Remove(asset);
                    return true;
                }


                //? 감소되었음에도 ReferenceCount가 0 초과라면 false를 반환한다
                return false;
            }



            ///======================================================================================================================================================



            //? 캐싱 딕셔너리 초기화



            ///<summary>
            /// <see cref="CachingAssets"/>를 Clear 한다
            ///</summary>
            public void ClearCachingAssets()
            {
                lock (lockobj)
                {
                    CachingAssets.Clear();
                    ReleaseOrigins.Clear();
                }
            }



            ///======================================================================================================================================================
        }



        /// <summary>
        /// 로드 매니저
        /// <para>어드레서블 에셋들을 불러오고, 해제하는 메서드들 모음</para>
        /// <para>가장 높은 레벨에서 동작하며 다른 낮은 레벨의 매니저들을 의존하여 어드레서블들을 관리</para>
        /// </summary>
        [Serializable]
        public class Loaders : Base
        {
            ///======================================================================================================================================================



            public Loaders(PanAddressableManager manager) : base(manager) { }



            ///======================================================================================================================================================



            //? 디버깅



            [LabelText("디버그 모드")]
            public bool DebugMode = false;



            [LabelText("불러오기에 실패한 횟수"), Sirenix.OdinInspector.ReadOnly, ShowInInspector, GUIColor(1, 0, 0)]
            public int ErrorCount { get; private set; } = 0;



            private void FailedLoadSingleAsset(string addressableName, bool autoReleaseHandleWhenFailedResult)
            {
                ErrorCount++;
                if (!DebugMode) { return; }
                string debugText = $"[어드레서블] 단일 에셋 불러오기 실패: {addressableName}";
                if (autoReleaseHandleWhenFailedResult) { debugText += ", 어드레서블 핸들 자동 해제됨"; }
                Debug.LogError(debugText);
            }



            private void FailedLoadMultiAssets(string addressableLabel, bool autoReleaseHandleWhenFailedResult)
            {
                ErrorCount++;
                if (!DebugMode) { return; }
                string debugText = $"[어드레서블] 복수 에셋 라벨로 불러오기 실패: {addressableLabel}";
                if (autoReleaseHandleWhenFailedResult) { debugText += ", 어드레서블 핸들 자동 해제됨"; }
                Debug.LogError(debugText);
            }



            private void FailedLoadMultiAssets(IEnumerable<string> addressableLabels, bool autoReleaseHandleWhenFailedResult)
            {
                ErrorCount++;
                if (!DebugMode) { return; }
                string debugText = $"[어드레서블] 복수 에셋 라벨들로 불러오기 실패: {string.Join(", ", addressableLabels)}";
                if (autoReleaseHandleWhenFailedResult) { debugText += ", 어드레서블 핸들 자동 해제됨"; }
                Debug.LogError(debugText);
            }



            private void FailedGetComponent<TComponent>(GameObject addressableGameObject, bool autoReleaseHandleWhenFailedResult) where TComponent : Component
            {
                ErrorCount++;
                if (!DebugMode) { return; }
                string debugText = $"[어드레서블] GameObject 에셋의 컴포넌트 얻기 실패: {addressableGameObject.name}, {typeof(TComponent).Name}";
                if (autoReleaseHandleWhenFailedResult) { debugText += ", 어드레서블 핸들 자동 해제됨"; }
                Debug.LogError(debugText);
            }



            ///======================================================================================================================================================



            //? 불러오기 유틸리티

            /// <summary>
            /// Create a strict-load exception for Required Addressable APIs.
            /// </summary>
            private static InvalidOperationException CreateRequiredLoadException<T>(string addressableName)
            {
                return new InvalidOperationException($"Required Addressable load failed. Key='{addressableName}', Type='{typeof(T).FullName}'.");
            }



            private static InvalidOperationException CreateRequiredComponentLoadException<TComponent>(string addressableName) where TComponent : Component
            {
                return new InvalidOperationException($"Required Addressable component load failed. Key='{addressableName}', ComponentType='{typeof(TComponent).FullName}'.");
            }



            /// <summary>
            /// <see cref="GameObject"/>로 불러온 에셋의 <typeparamref name="TComponent"/> 컴포넌트를 얻어 반환한다
            /// <para>컴포넌트를 얻는데에 실패했다면, 디버깅과 함께 실패한다</para>
            /// </summary>
            /// <param name="addressableHandle"><see cref="GameObject"/>를 반환한 어드레서블 핸들</param>
            /// <param name="assetComponent">반환할 컴포넌트 에셋</param>
            /// <param name="autoReleaseHandleWhenFailedResult">변환에 실패시, <paramref name="addressableHandle"/>를 자동으로 해제될지의 여부</param>
            private bool ConvertAsset_GameObjectToComponent<TComponent>(in AsyncOperationHandle<GameObject> addressableHandle, out TComponent assetComponent, bool autoReleaseHandleWhenFailedResult) where TComponent : Component
            {
                var assetGameObject = addressableHandle.Result; //. 핸들의 결과값인 게임오브젝트 에셋


                //? 해당 GameObject에서 컴포넌트를 얻어 반환한다
                if (assetGameObject.TryGetComponent(out assetComponent)) { return true; }


                //! 해당 GameObject에서 컴포넌트를 얻는데에 실패
                FailedGetComponent<TComponent>(assetGameObject, autoReleaseHandleWhenFailedResult);
                if (autoReleaseHandleWhenFailedResult) { Release(addressableHandle); } //. Component 변환 실패시, 핸들을 자동으로 해제 (캐싱에서도 제거)
                return false;
            }



            /// <summary>
            /// <see cref="GameObject"/>로 불러온 에셋 리스트의 <typeparamref name="TComponent"/> 컴포넌트를 얻어 반환한다
            /// <para>단 하나라도 컴포넌트를 얻는데에 실패했다면, 디버깅과 함께 실패한다</para>
            /// </summary>
            /// <param name="addressableHandle"><see cref="IList"/>(<see cref="GameObject"/>)를 반환한 어드레서블 핸들</param>
            /// <param name="assetComponentList">반환할 컴포넌트 에셋 리스트</param>
            /// <param name="autoReleaseHandleWhenFailedResult">변환에 실패시, <paramref name="addressableHandle"/>를 자동으로 해제될지의 여부</param>
            private bool ConvertAssetList_GameObjectToComponent<TComponent>(in AsyncOperationHandle<IList<GameObject>> addressableHandle, in List<TComponent> assetComponentList, bool autoReleaseHandleWhenFailedResult) where TComponent : Component
            {
                bool success = true;
                var assetGameObjectList = addressableHandle.Result; //. 핸들의 결과값인 게임오브젝트 에셋리스트


                //? GameObject 에셋 리스트를 순회하면서, 컴포넌트를 얻어본다
                for (int i = 0; i < assetGameObjectList.Count; i++)
                {
                    var asset = assetGameObjectList[i];


                    //? 컴포넌트를 얻는데에 성공, 리스트에 추가한다
                    if (asset.TryGetComponent<TComponent>(out var assetComponent))
                    {
                        assetComponentList.Add(assetComponent);
                    }


                    //! 컴포넌트를 얻는데에 실패했다면, 즉시 순회를 중단하고 실패한다
                    else
                    {
                        FailedGetComponent<TComponent>(asset, autoReleaseHandleWhenFailedResult);
                        success = false;
                        break;
                    }
                }


                //! 단 하나라도 컴포넌트를 얻는데에 실패했다면
                if (!success)
                {
                    if (autoReleaseHandleWhenFailedResult) { Release(addressableHandle); } //. Component 변환 실패시, 핸들을 자동으로 해제 (캐싱에서도 제거)
                }


                return success; //. 성공여부를 반환한다
            }



            ///======================================================================================================================================================



            //? 단일 에셋 불러오기



            #region [단일 에셋] 불러오기/불러보기



            //? [단일 에셋] 불러오기 (동기)

            ///<summary>
            /// [동기] 단일 어드레서블 에셋을 볼러와 반환한다
            /// <para>불러오기에 실패시 실패한 Handle과 null을 반환하며, <paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            ///</summary>
            ///<param name="addressableName">어드레서블 이름</param>
            ///<param name="addressableHandle">반환되는 어드레서블 핸들</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public T Load<T>(string addressableName, out AsyncOperationHandle<T> addressableHandle, bool autoReleaseHandleWhenFailedResult = true) where T : Object
            {
                Manager.Event.LoadAssetBefore(addressableName); //. 에셋을 불러오기 "이전" 이벤트 실행


                //? 에셋을 어드레서블로 불러온다
                T resultAsset = PanAddressableNative.LoadAsset(addressableName, out addressableHandle);


                //! 에셋 불러오기 실패
                if (resultAsset == null)
                {
                    if (autoReleaseHandleWhenFailedResult) { addressableHandle.Release(); } //. 불러오기 실패시, 핸들을 자동으로 해제
                    FailedLoadSingleAsset(addressableName, autoReleaseHandleWhenFailedResult);
                    return null;
                }


                //. 에셋 불러오기 성공
                Manager.Caching.AddCache(resultAsset); //. 불러온 에셋을 캐싱한다
                Manager.Event.LoadAssetAfter(in addressableHandle); //. 에셋을 불러오기 "이후" 이벤트 실행
                return resultAsset; //. 어드레서블 반환
            }

            ///<summary>
            /// [동기] 단일 어드레서블 에셋을 볼러와 반환한다
            /// <para>불러오기에 실패시 null을 반환하며, Handle을 반환하지 않기에 자동으로 Handle을 Release한다</para>
            ///</summary>
            ///<param name="addressableName">어드레서블 이름</param>
            public T Load<T>(string addressableName) where T : Object => Load<T>(addressableName, out var addressableHandle, true);

            /// <summary>
            /// Load a single Addressable asset and throw if the asset cannot be loaded.
            /// Existing Load/TryLoad APIs keep their null-return contract; use this only when missing data is exceptional.
            /// </summary>
            public T LoadRequired<T>(string addressableName, out AsyncOperationHandle<T> addressableHandle, bool autoReleaseHandleWhenFailedResult = true) where T : Object
            {
                var asset = Load<T>(addressableName, out addressableHandle, autoReleaseHandleWhenFailedResult);
                if (asset == null) { throw CreateRequiredLoadException<T>(addressableName); }
                return asset;
            }

            /// <summary>
            /// Load a single Addressable asset and throw if the asset cannot be loaded.
            /// </summary>
            public T LoadRequired<T>(string addressableName) where T : Object => LoadRequired<T>(addressableName, out _, true);



            //? [단일 에셋] 불러오기 (비동기)

            ///<summary>
            /// [비동기] 단일 어드레서블 에셋을 볼러와 Handle을 반환한다
            /// <para>불러오기에 실패시 실패한 Handle을 반환하며, <paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            ///</summary>
            ///<param name="addressableName">어드레서블 이름</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public async UniTask<AsyncOperationHandle<T>> LoadAsyncHandle<T>(string addressableName, bool autoReleaseHandleWhenFailedResult = true) where T : Object
            {
                Manager.Event.LoadAssetBefore(addressableName); //. 에셋을 불러오기 "이전" 이벤트 실행


                //? 에셋을 어드레서블로 불러온다
                var addressableHandle = PanAddressableNative.LoadAssetAsyncHandle<T>(addressableName);
                await addressableHandle.Task; //. 비동기 대기


                //! 에셋 불러오기 실패, 실패하더라도 Handle를 반환한다
                if (addressableHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    if (autoReleaseHandleWhenFailedResult) { addressableHandle.Release(); } //. 불러오기 실패시, 핸들을 자동으로 해제
                    FailedLoadSingleAsset(addressableName, autoReleaseHandleWhenFailedResult);
                    return addressableHandle;
                }


                //. 에셋 불러오기 성공
                Manager.Caching.AddCache(addressableHandle.Result); //. 불러온 에셋을 캐싱한다
                Manager.Event.LoadAssetAfter(addressableHandle); //. 에셋을 불러오기 "이후" 이벤트 실행
                return addressableHandle; //. 어드레서블 핸들 반환
            }

            ///<summary>
            /// [비동기] 단일 어드레서블 에셋을 볼러와 반환한다
            /// <para>불러오기에 실패시 null을 반환하며, Handle을 반환하지 않기에 자동으로 Handle을 Release한다</para>
            ///</summary>
            ///<param name="addressableName">어드레서블 이름</param>
            public async UniTask<T> LoadAsync<T>(string addressableName) where T : Object
            {
                var addressableHandle = await LoadAsyncHandle<T>(addressableName, true);
                return (addressableHandle.IsValid() && addressableHandle.Status == AsyncOperationStatus.Succeeded && addressableHandle.Result != null) ? addressableHandle.Result : null;
            }

            /// <summary>
            /// Load a single Addressable asset asynchronously and throw if the asset cannot be loaded.
            /// Existing LoadAsync/TryLoadAsync APIs keep their null-return contract.
            /// </summary>
            public async UniTask<T> LoadRequiredAsync<T>(string addressableName) where T : Object
            {
                var asset = await LoadAsync<T>(addressableName);
                if (asset == null) { throw CreateRequiredLoadException<T>(addressableName); }
                return asset;
            }



            //? [단일 에셋] 불러보기 (동기)

            ///<summary>
            /// [동기] 단일 어드레서블 에셋을 볼러와 반환해본다
            /// <para>불러오기에 실패시 실패한 Handle과 null을 반환하며, <paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            ///</summary>
            ///<param name="addressableName">어드레서블 이름</param>
            ///<param name="asset">반환되는 에셋</param>
            ///<param name="addressableHandle">반환되는 어드레서블 핸들</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public bool TryLoad<T>(string addressableName, out T asset, out AsyncOperationHandle<T> addressableHandle, bool autoReleaseHandleWhenFailedResult = true) where T : Object
            {
                asset = Load(addressableName, out addressableHandle, autoReleaseHandleWhenFailedResult);
                return asset != null;
            }

            ///<summary>
            /// [동기] 단일 어드레서블 에셋을 볼러와 반환해본다
            /// <para>불러오기에 실패시 실패한 Handle과 null을 반환하며, Handle을 반환하지 않기에 자동으로 Handle을 Release한다</para>
            ///</summary>
            ///<param name="addressableName">어드레서블 이름</param>
            ///<param name="asset">반환되는 에셋</param>
            public bool TryLoad<T>(string addressableName, out T asset) where T : Object => TryLoad(addressableName, out asset, out _, true);



            //? [단일 에셋] 불러보기 (비동기)

            ///<summary>
            /// [비동기] 단일 어드레서블 에셋을 볼러와 Handle을 반환해본다
            /// <para>불러오기 성공 여부와 Handle을 반환하며, 불러오기에 실패시 <paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            ///</summary>
            ///<param name="addressableName">어드레서블 이름</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public async UniTask<(bool success, AsyncOperationHandle<T> addressableHandle)> TryLoadAsyncHandle<T>(string addressableName, bool autoReleaseHandleWhenFailedResult = true) where T : Object
            {
                var handle = await LoadAsyncHandle<T>(addressableName, autoReleaseHandleWhenFailedResult);
                return new(handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded, handle);
            }

            ///<summary>
            /// [비동기] 단일 어드레서블 에셋을 볼러와 반환해본다
            /// <para>불러오기 성공 여부와 에셋을 반환하며, 불러오기에 Handle을 반환하지 않기에 자동으로 Handle을 Release한다</para>
            ///</summary>
            ///<param name="addressableName">어드레서블 이름</param>
            public async UniTask<(bool success, T asset)> TryLoadAsync<T>(string addressableName) where T : Object
            {
                var handle = await LoadAsyncHandle<T>(addressableName, true);
                if (!handle.IsValid() || handle.Status != AsyncOperationStatus.Succeeded)
                {
                    return new(false, null);
                }

                return new(true, handle.Result);
            }



            #endregion



            #region [단일 에셋] Component로 불러오기/불러보기



            //? [단일 에셋] Component로 불러오기 (동기)

            ///<summary>
            /// [동기] 단일 <see cref="GameObject"/> 어드레서블 에셋을 볼러와 <typeparamref name="TComponent"/>의 컴포넌트를 얻어 반환한다
            /// <para>불러오기에 실패하거나 컴포넌트 얻기에 실패시 null을 반환하며, <paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            ///</summary>
            ///<param name="addressableName">어드레서블 이름</param>
            ///<param name="addressableHandle">반환되는 어드레서블 핸들 (어드레서블 에셋 자체는 <see cref="GameObject"/> 로 불러오기에 핸들의 Result는 <typeparamref name="TComponent"/>가 되지 못한다</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public TComponent LoadComponent<TComponent>(string addressableName, out AsyncOperationHandle<GameObject> addressableHandle, bool autoReleaseHandleWhenFailedResult = true) where TComponent : Component
            {
                //? GameObject 로 에셋을 얻어본다
                if (TryLoad(addressableName, out var assetGameObject, out addressableHandle, autoReleaseHandleWhenFailedResult))
                {
                    //? Component로 변환해 반환한다 (변환에 실패시 디버깅과 함께 null이 반환된다, 핸들을 자동으로 해제하기도 한다)
                    if (ConvertAsset_GameObjectToComponent<TComponent>(addressableHandle, out var assetComponent, autoReleaseHandleWhenFailedResult))
                    {
                        return assetComponent;
                    }
                }


                //! 실패, null과 실패한 핸들을 반환한다
                return null;
            }
            /// <summary>
            /// Load a GameObject Addressable, extract the requested component, and throw if either step fails.
            /// Existing LoadComponent/TryLoadComponent APIs keep their null-return contract.
            /// </summary>
            public TComponent LoadRequiredComponent<TComponent>(string addressableName, out AsyncOperationHandle<GameObject> addressableHandle, bool autoReleaseHandleWhenFailedResult = true) where TComponent : Component
            {
                var component = LoadComponent<TComponent>(addressableName, out addressableHandle, autoReleaseHandleWhenFailedResult);
                if (component == null) { throw CreateRequiredComponentLoadException<TComponent>(addressableName); }
                return component;
            }

            /// <summary>
            /// Load a GameObject Addressable, extract the requested component, and throw if either step fails.
            /// </summary>
            public TComponent LoadRequiredComponent<TComponent>(string addressableName) where TComponent : Component => LoadRequiredComponent<TComponent>(addressableName, out _, true);




            //? [단일 에셋] Component로 불러오기 (비동기)

            ///<summary>
            /// [비동기] 단일 <see cref="GameObject"/> 어드레서블 에셋을 볼러와 <typeparamref name="TComponent"/>의 컴포넌트를 얻어 Handle과 함께 반환한다
            /// <para>불러오기에 실패한다면 null과 <see cref="AsyncOperationHandle{T}"/>를 반환하며, <paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            ///</summary>
            ///<param name="addressableName">어드레서블 이름</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public async UniTask<(TComponent asset, AsyncOperationHandle<GameObject> addressableHandle)> LoadComponentAsyncHandle<TComponent>(string addressableName, bool autoReleaseHandleWhenFailedResult = true) where TComponent : Component
            {
                //? GameObject 로 에셋을 얻어본다
                var addressableHandle = await LoadAsyncHandle<GameObject>(addressableName, autoReleaseHandleWhenFailedResult);


                //? GameObject로 에셋을 얻는데에 성공
                if (addressableHandle.IsValid() && addressableHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    //? Component로 변환해 반환한다 (변환에 실패시 디버깅과 함께 null이 반환된다, 핸들을 자동으로 해제하기도 한다)
                    if (ConvertAsset_GameObjectToComponent<TComponent>(addressableHandle, out var assetComponent, autoReleaseHandleWhenFailedResult))
                    {
                        return new(assetComponent, addressableHandle);
                    }

                }


                //! 실패, null과 실패한 핸들을 반환한다
                return new(null, addressableHandle);
            }

            /// <summary>
            /// Load a GameObject Addressable asynchronously, extract the requested component, and throw if either step fails.
            /// Existing LoadComponentAsyncHandle/TryLoadComponent APIs keep their null-return contract.
            /// 성공한 GameObject origin은 manager cache에 유지되며 ReleaseAllCache로 해제할 수 있습니다.
            /// </summary>
            /// <param name="autoReleaseHandleWhenFailedResult">호환성 유지용입니다. Handle을 반환하지 않는 이 overload는 실패 handle을 항상 해제합니다.</param>
            public async UniTask<TComponent> LoadRequiredComponentAsync<TComponent>(string addressableName, bool autoReleaseHandleWhenFailedResult = true) where TComponent : Component
            {
                var (component, _) = await LoadComponentAsyncHandle<TComponent>(addressableName, true);
                if (component == null) { throw CreateRequiredComponentLoadException<TComponent>(addressableName); }
                return component;
            }



            //? [단일 에셋] Component로 불러보기 (동기)

            ///<summary>
            /// [동기] 단일 <see cref="GameObject"/> 어드레서블 에셋을 볼러와 <typeparamref name="TComponent"/>의 컴포넌트를 얻어 반환해본다
            /// <para>불러오기에 실패하거나 컴포넌트 얻기에 실패시 실패한 Handle과 null을 반환하며, <paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            ///</summary>
            ///<param name="addressableName">어드레서블 이름</param>
            ///<param name="assetComponent">반환되는 <typeparamref name="TComponent"/>의 에셋</param>
            ///<param name="addressableHandle">반환되는 어드레서블 핸들 (어드레서블 에셋 자체는 <see cref="GameObject"/> 로 불러오기에 핸들의 Result는 <typeparamref name="TComponent"/>가 되지 못한다</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public bool TryLoadComponent<TComponent>(string addressableName, out TComponent assetComponent, out AsyncOperationHandle<GameObject> addressableHandle, bool autoReleaseHandleWhenFailedResult = true) where TComponent : Component
            {
                assetComponent = LoadComponent<TComponent>(addressableName, out addressableHandle, autoReleaseHandleWhenFailedResult);
                return assetComponent != null;
            }



            #endregion



            #region [단일 에셋+] SubAsset 리스트로 불러오기/불러보기



            //? [단일 에셋+] SubAsset 리스트로 불러오기 (동기)

            ///<summary>
            /// [동기] 단일 어드레서블 에셋을 SubAsset 리스트로 볼러와 반환한다
            /// <para>Multiple-Sprite와 같은 에셋 안에 또 에셋이 있는것들을 불러온다</para>
            /// <para>불러오기에 실패시 실패한 Handle과 null을 반환하며, <paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            ///</summary>
            ///<param name="addressableName">어드레서블 이름</param>
            ///<param name="addressableHandle">반환되는 어드레서블 핸들</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public IList<T> LoadSubAssetList<T>(string addressableName, out AsyncOperationHandle<IList<T>> addressableHandle, bool autoReleaseHandleWhenFailedResult = true) where T : Object
            {
                Manager.Event.LoadAssetBefore(addressableName); //. SubAsset 리스트를 불러오기 "이전" 이벤트 실행


                //? SubAsset 리스트를 어드레서블로 불러온다
                var resultAssetList = PanAddressableNative.LoadSubAssetList(addressableName, out addressableHandle);


                //! SubAsset 리스트 불러오기 실패, Handle을 해제한뒤 null을 반환한다
                if (resultAssetList == null)
                {
                    if (autoReleaseHandleWhenFailedResult) { addressableHandle.Release(); } //. 불러오기 실패시, 핸들을 자동으로 해제
                    FailedLoadSingleAsset(addressableName, autoReleaseHandleWhenFailedResult);
                    return null;
                }


                //. SubAsset 리스트 불러오기 성공
                Manager.Caching.AddCache(resultAssetList); //. 불러온 SubAsset리스트를 캐싱한다
                Manager.Event.LoadAssetsAfterEvent(in addressableHandle); //. SubAsset 리스트를 불러오기 "이후" 이벤트 실행
                return resultAssetList; //. 어드레서블 반환
            }

            ///<summary>
            /// [동기] 단일 어드레서블 에셋을 SubAsset 리스트로 볼러와 반환한다
            /// <para>Multiple-Sprite와 같은 에셋 안에 또 에셋이 있는것들을 불러온다</para>
            /// <para>불러오기에 실패시 null을 반환하며, Handle을 반환하지 않기에 자동으로 Handle을 Release한다</para>
            ///</summary>
            ///<param name="addressableName">어드레서블 이름</param>
            [Obsolete("복수 에셋은 되도록 addressableHandle을 반환받고 관리하는 구조로 변경 권장", false)]
            public IList<T> LoadSubAssetList<T>(string addressableName) where T : Object => LoadSubAssetList<T>(addressableName, out _, true);



            //? [단일 에셋] SubAsset 리스트로 불러오기 (비동기)

            ///<summary>
            /// [비동기] 단일 어드레서블 에셋을 SubAsset 리스트로 볼러와 Handle을 반환한다
            /// <para>Multiple-Sprite와 같은 에셋 안에 또 에셋이 있는것들을 불러온다</para>
            /// <para>불러오기에 실패시 실패한 Handle을 반환하며, <paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            ///</summary>
            ///<param name="addressableName">어드레서블 이름</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public async UniTask<AsyncOperationHandle<IList<T>>> LoadSubAssetsAsyncHandle<T>(string addressableName, bool autoReleaseHandleWhenFailedResult = true) where T : Object
            {
                Manager.Event.LoadAssetBefore(addressableName); //. SubAsset 리스트를 불러오기 "이전" 이벤트 실행


                //? SubAsset 리스트를 어드레서블로 불러온다
                var addressableHandle = PanAddressableNative.LoadSubAssetListAsyncHandle<T>(addressableName);
                await addressableHandle.Task; //. 비동기 대기


                //! SubAsset 리스트 불러오기 실패, Handle을 해제한뒤 실패한 핸들을 반환한다
                if (addressableHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    if (autoReleaseHandleWhenFailedResult) { addressableHandle.Release(); } //. 불러오기 실패시, 핸들을 자동으로 해제
                    FailedLoadSingleAsset(addressableName, autoReleaseHandleWhenFailedResult);
                    return addressableHandle;
                }


                //. SubAsset 리스트 불러오기 성공
                Manager.Caching.AddCache(addressableHandle.Result); //. 불러온 SubAsset리스트를 캐싱한다
                Manager.Event.LoadAssetsAfterEvent(addressableHandle); //. SubAsset 리스트를 불러오기 "이후" 이벤트 실행
                return addressableHandle; //. 어드레서블 핸들 반환
            }

            ///<summary>
            /// [비동기] 단일 어드레서블 에셋을 SubAsset 리스트로 볼러와 반환한다
            /// <para>Multiple-Sprite와 같은 에셋 안에 또 에셋이 있는것들을 불러온다</para>
            /// <para>불러오기에 실패시 실패한 Handle을 반환하며, Handle을 반환하지 않기에 자동으로 Handle을 Release한다</para>
            ///</summary>
            ///<param name="addressableName">어드레서블 이름</param>
            [Obsolete("복수 에셋은 되도록 addressableHandle을 반환받고 관리하는 구조로 변경 권장", false)]
            public async UniTask<IList<T>> LoadSubAssetsAsync<T>(string addressableName) where T : Object
            {
                var handle = await LoadSubAssetsAsyncHandle<T>(addressableName, true);
                return handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null;
            }



            //? [단일 에셋+] SubAsset 리스트로 불러보기 (동기)

            ///<summary>
            /// [동기] 단일 어드레서블 에셋을 SubAsset 리스트로 볼러와 반환해본다
            /// <para>Multiple-Sprite와 같은 에셋 안에 또 에셋이 있는것들을 불러온다</para>
            /// <para>불러오기에 실패시 실패한 Handle과 null을 반환하며, <paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            ///</summary>
            ///<param name="addressableName">어드레서블 이름</param>
            ///<param name="subAssetList">반환되는 SubAsset 리스트</param>
            ///<param name="addressableHandle">반환되는 어드레서블 핸들</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public bool TryLoadSubAssetList<T>(string addressableName, out IList<T> subAssetList, out AsyncOperationHandle<IList<T>> addressableHandle, bool autoReleaseHandleWhenFailedResult = true) where T : Object
            {
                subAssetList = LoadSubAssetList(addressableName, out addressableHandle, autoReleaseHandleWhenFailedResult);
                return subAssetList != null;
            }

            ///<summary>
            /// [동기] 단일 어드레서블 에셋을 SubAsset 리스트로 볼러와 반환해본다
            /// <para>Multiple-Sprite와 같은 에셋 안에 또 에셋이 있는것들을 불러온다</para>
            /// <para>불러오기에 실패시 null을 반환하며, Handle을 반환하지 않기에 자동으로 Handle을 Release한다</para>
            ///</summary>
            ///<param name="addressableName">어드레서블 이름</param>
            ///<param name="subAssetList">반환되는 SubAsset 리스트</param>
            [Obsolete("복수 에셋은 되도록 addressableHandle을 반환받고 관리하는 구조로 변경 권장", false)]
            public bool TryLoadSubAssetList<T>(string addressableName, out IList<T> subAssetList) where T : Object => TryLoadSubAssetList(addressableName, out subAssetList, out _, true);



            #endregion



            #region [단일에셋++] 비동기 Lazy로 불러오는 메서드 모음



            //? [단일 에셋++] 불러오기 (비동기 Lazy)

            ///<summary>
            /// [비동기 Lazy] 단일 어드레서블 에셋을 비동기로 볼러와 <paramref name="onSucceeded"/> 또는 <paramref name="onFailed"/>를 Callback 한다
            /// <para>불러오기에 실패시,<paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            ///</summary>
            ///<param name="addressableName">어드레서블 이름</param>
            ///<param name="onSucceeded">성공할때 실행되는 콜백</param>
            ///<param name="onFailed">실패할때 실행되는 콜백</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public void LoadLazyAsync<T>(string addressableName, Action<AsyncOperationHandle<T>> onSucceeded, Action<AsyncOperationHandle<T>> onFailed, bool autoReleaseHandleWhenFailedResult = true) where T : Object
            {
                Manager.Event.LoadAssetBefore(addressableName); //. 에셋을 불러오기 "이전" 이벤트 실행


                //? 어드레서블로 에셋을 불러오되, 비동기로 불러오며 완료시 Callback을 실행한다
                PanAddressableNative.LoadAssetAsyncCallback<T>(addressableName, addressableHandle =>
                {
                    //? 에셋 불러오기에 성공
                    if (addressableHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        Manager.Caching.AddCache(addressableHandle.Result); //. 불러온 에셋을 캐싱한다
                        Manager.Event.LoadAssetAfter(addressableHandle); //. 에셋을 불러오기 "이후" 이벤트 실행
                        onSucceeded?.Invoke(addressableHandle); //? 성공 콜백 실행
                    }

                    //! 에셋 불러오기에 실패
                    else
                    {
                        if (autoReleaseHandleWhenFailedResult) { addressableHandle.Release(); } //. 불러오기 실패시, 핸들을 자동으로 해제
                        FailedLoadSingleAsset(addressableName, autoReleaseHandleWhenFailedResult);
                        onFailed?.Invoke(addressableHandle); //! 실패 콜백 실행
                    }
                });
            }



            //? [단일 에셋++] Component 불러오기 (비동기 Lazy)

            ///<summary>
            /// [비동기 Lazy] 단일 <see cref="GameObject"/> 어드레서블 에셋을 비동기로 볼러와 <typeparamref name="TComponent"/>의 컴포넌트를 얻어 <paramref name="onSucceeded"/> 또는 <paramref name="onFailed"/>를 Callback 한다
            /// <para>불러오기에 실패시,<paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            ///</summary>
            ///<param name="addressableName">어드레서블 이름</param>
            ///<param name="onSucceeded">성공할때 실행되는 콜백</param>
            ///<param name="onFailed">실패할때 실행되는 콜백</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public void LoadComponentLazyAsync<TComponent>(string addressableName, Action<TComponent, AsyncOperationHandle<GameObject>> onSucceeded, Action<AsyncOperationHandle<GameObject>> onFailed, bool autoReleaseHandleWhenFailedResult = true) where TComponent : Component
            {
                Manager.Event.LoadAssetBefore(addressableName); //. 에셋을 불러오기 "이전" 이벤트 실행


                //? 어드레서블로 에셋을 불러오되, GameObject로 불러오며 얻어올때까지 대기하며 (비동기), 완료를 콜백 메서드로 받는다
                PanAddressableNative.LoadAssetAsyncCallback<GameObject>(addressableName, addressableHandle =>
                {
                    //? 에셋 불러오기에 성공
                    if (addressableHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        //? Component로 변환해 반환한다 (변환에 실패시 디버깅과 함께 null이 반환된다, 핸들을 자동으로 해제하기도 한다)
                        if (ConvertAsset_GameObjectToComponent<TComponent>(addressableHandle, out var assetComponent, autoReleaseHandleWhenFailedResult))
                        {
                            Manager.Caching.AddCache(addressableHandle.Result); //. 불러온 에셋을 캐싱한다
                            Manager.Event.LoadAssetAfter(addressableHandle); //. 에셋을 불러오기 "이후" 이벤트 실행
                            onSucceeded?.Invoke(assetComponent, addressableHandle); //? 성공 콜백 실행
                        }

                        //! Component로 변환에 실패
                        else
                        {
                            FailedLoadSingleAsset(addressableName, autoReleaseHandleWhenFailedResult);
                            onFailed?.Invoke(addressableHandle); //! 실패 콜백 실행
                        }
                    }

                    //! 에셋 불러오기에 실패
                    else
                    {
                        if (autoReleaseHandleWhenFailedResult) { addressableHandle.Release(); } //. 불러오기 실패시, 핸들을 자동으로 해제
                        FailedLoadSingleAsset(addressableName, autoReleaseHandleWhenFailedResult);
                        onFailed?.Invoke(addressableHandle); //! 실패 콜백 실행
                    }
                });
            }



            //? [단일 에셋++] SubAsset 리스트로 불러오기 (비동기 Lazy)

            ///<summary>
            /// [비동기 Lazy] 단일 어드레서블 에셋을 SubAsset 리스트로 비동기로 볼러와 <paramref name="onSucceeded"/> 또는 <paramref name="onFailed"/>를 Callback 한다
            /// <para>Multiple-Sprite와 같은 에셋 안에 또 에셋이 있는것들을 불러온다</para>
            /// <para>불러오기에 실패시,<paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            ///</summary>
            ///<param name="addressableName">어드레서블 이름</param>
            ///<param name="onSucceeded">성공할때 실행되는 콜백</param>
            ///<param name="onFailed">실패할때 실행되는 콜백</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public void LoadSubAssetsLazyAsync<T>(string addressableName, Action<IList<T>, AsyncOperationHandle> onSucceeded, Action<AsyncOperationHandle> onFailed, bool autoReleaseHandleWhenFailedResult = true) where T : Object
            {
                Manager.Event.LoadAssetBefore(addressableName); //. 에셋을 불러오기 "이전" 이벤트 실행


                //? 에셋을 어드레서블로 불러오되, 얻어올때까지 대기하며 (비동기), 완료를 콜백 메서드로 받는다
                PanAddressableNative.LoadSubAssetListAsyncCallback<T>(addressableName, (Action<AsyncOperationHandle<IList<T>>>)(addressableHandle =>
                {
                    //? 에셋 불러오기에 성공
                    if (addressableHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        Manager.Caching.AddCache(addressableHandle.Result); //. 불러온 SubAsset리스트를 캐싱한다
                        Manager.Event.LoadAssetsAfterEvent<T>(addressableHandle); //. 에셋을 불러오기 "이후" 이벤트 실행
                        onSucceeded?.Invoke(addressableHandle.Result, addressableHandle); //? 성공 콜백 실행
                    }

                    //! 에셋 불러오기에 실패
                    else
                    {
                        if (autoReleaseHandleWhenFailedResult) { addressableHandle.Release(); } //. 불러오기 실패시, 핸들을 자동으로 해제
                        FailedLoadSingleAsset(addressableName, autoReleaseHandleWhenFailedResult);
                        onFailed?.Invoke(addressableHandle); //! 실패 콜백 실행
                    }
                }));
            }



            #endregion



            ///======================================================================================================================================================



            //? 복수 에셋들 불러오기



            #region [복수 에셋들] "라벨"로 불러오기/불러보기 (+대상 리스트에 추가)



            //? [복수 에셋들] "라벨"로 불러오기 (동기)



            ///<summary>
            /// [동기] 복수 어드레서블 에셋들을 <b>라벨</b>로 볼러와 반환한다
            /// <para>불러오기에 실패시 실패한 Handle과 null을 반환하며, <paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            ///</summary>
            ///<param name="addressableLabel">어드레서블 라벨</param>
            ///<param name="addressableHandle">반환되는 어드레서블 핸들</param>
            ///<param name="callback">각 에셋들이 로드될때마다 각각 실행되는 콜백</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public IList<T> Loads<T>(string addressableLabel, out AsyncOperationHandle<IList<T>> addressableHandle, Action<T> callback = null, bool autoReleaseHandleWhenFailedResult = true) where T : Object
            {
                Manager.Event.LoadAssetsBefore(addressableLabel); //. 에셋들을 불러오기 "이전" 이벤트 실행


                //? 에셋들을 어드레서블로 불러온다
                var resultAssetList = PanAddressableNative.LoadAssets(addressableLabel, callback, out addressableHandle);


                //! 에셋들 불러오기 실패
                if (resultAssetList == null)
                {
                    if (autoReleaseHandleWhenFailedResult) { addressableHandle.Release(); } //. 불러오기 실패시, 핸들을 자동으로 해제
                    FailedLoadMultiAssets(addressableLabel, autoReleaseHandleWhenFailedResult);
                    return null;
                }


                //. 에셋들 불러오기 성공
                Manager.Caching.AddCache(resultAssetList); //. 불러온 에셋들을 캐싱한다
                Manager.Event.LoadAssetsAfterEvent(in addressableHandle); //. 에셋들을 불러오기 "이후" 이벤트 실행
                return resultAssetList; //. 어드레서블 리스트 반환
            }

            ///<summary>
            /// [동기] 복수 어드레서블 에셋들을 <b>라벨</b>로 볼러와 반환한다
            /// <para>불러오기에 실패시 실패한 Handle과 null을 반환하며, Handle을 반환하지 않기에 자동으로 Handle을 Release한다</para>
            ///</summary>
            ///<param name="addressableLabel">어드레서블 라벨</param>
            ///<param name="callback">각 에셋들이 로드될때마다 각각 실행되는 콜백</param>
            public IList<T> Loads<T>(string addressableLabel, Action<T> callback = null) where T : Object => Loads(addressableLabel, out _, callback, true);



            //? [복수 에셋들] "라벨"로 불러오기 (비동기)



            ///<summary>
            /// [비동기] 복수 어드레서블 에셋들을 <b>라벨</b>로 볼러와 Handle을 반환한다
            /// <para>불러오기에 실패시 실패한 Handle과 null을 반환하며, <paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            ///</summary>
            ///<param name="addressableLabel">어드레서블 라벨</param>
            ///<param name="callback">각 에셋들이 로드될때마다 각각 실행되는 콜백</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public async UniTask<AsyncOperationHandle<IList<T>>> LoadsAsyncHandle<T>(string addressableLabel, Action<T> callback = null, bool autoReleaseHandleWhenFailedResult = true) where T : Object
            {
                Manager.Event.LoadAssetsBefore(addressableLabel); //. 에셋들을 불러오기 "이전" 이벤트 실행


                //? 에셋들을 어드레서블로 불러온다
                AsyncOperationHandle<IList<T>> addressableHandle = PanAddressableNative.LoadAssetsAsyncHandle(addressableLabel, callback);
                await addressableHandle.Task; //. 비동기 대기


                //! 에셋 불러오기 실패, 실패하더라도 Handle를 반환한다
                if (addressableHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    if (autoReleaseHandleWhenFailedResult) { addressableHandle.Release(); } //. 불러오기 실패시, 핸들을 자동으로 해제
                    FailedLoadMultiAssets(addressableLabel, autoReleaseHandleWhenFailedResult);
                    return addressableHandle;
                }


                //. 에셋들 불러오기 성공
                Manager.Caching.AddCache(addressableHandle.Result); //. 불러온 에셋들을 캐싱한다
                Manager.Event.LoadAssetsAfterEvent(addressableHandle); //. 에셋들을 불러오기 "이후" 이벤트 실행
                return addressableHandle; //. 어드레서블 핸들 반환
            }

            ///<summary>
            /// [비동기] 복수 어드레서블 에셋들을 <b>라벨</b>로 볼러와 반환한다
            /// <para>불러오기에 실패시 null을 반환하며, Handle을 반환하지 않기에 자동으로 Handle을 Release한다</para>
            ///</summary>
            ///<param name="addressableLabel">어드레서블 라벨</param>
            public async UniTask<IList<T>> LoadsAsync<T>(string addressableLabel, Action<T> callback = null) where T : Object
            {
                var handle = await LoadsAsyncHandle(addressableLabel, callback, true);
                return handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null;
            }



            //? [복수 에셋들] "라벨"로 불러보기 (동기)

            ///<summary>
            /// [동기] 복수 어드레서블 에셋들을 <b>라벨</b>로 볼러와 반환해본다
            /// <para>불러오기에 실패시 실패한 Handle과 null을 반환하며, <paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            ///</summary>
            ///<param name="addressableLabel">어드레서블 라벨</param>
            ///<param name="assetList">반환되는 에셋 리스트</param>
            ///<param name="addressableHandle">반환되는 어드레서블 핸들</param>
            ///<param name="callback">각 에셋들이 로드될때마다 각각 실행되는 콜백</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public bool TryLoads<T>(string addressableLabel, out IList<T> assetList, out AsyncOperationHandle<IList<T>> addressableHandle, Action<T> callback = null, bool autoReleaseHandleWhenFailedResult = true) where T : Object
            {
                assetList = Loads(addressableLabel, out addressableHandle, callback, autoReleaseHandleWhenFailedResult);
                return assetList != null;
            }

            ///<summary>
            /// [동기] 복수 어드레서블 에셋들을 <b>라벨</b>로 볼러와 반환해본다
            /// <para>불러오기에 실패시 실패한 Handle과 null을 반환하며, Handle을 반환하지 않기에 자동으로 Handle을 Release한다</para>
            ///</summary>
            ///<param name="addressableLabel">어드레서블 라벨</param>
            ///<param name="callback">각 에셋들이 로드될때마다 각각 실행되는 콜백</param>
            ///<param name="assetList">반환되는 에셋 리스트</param>
            public bool TryLoads<T>(string addressableLabel, out IList<T> assetList, Action<T> callback = null) where T : Object => TryLoads(addressableLabel, out assetList, out _, callback, true);



            //? [복수 에셋들] "라벨"로 불러와 대상 리스트에 추가하기 (동기)

            ///<summary>
            /// [동기] 복수 어드레서블 에셋들을 <b>라벨</b>로 볼러와, <paramref name="targetAssetList"/>에 불러온 에셋들을 각각 추가한다
            /// <para>불러오기에 성공했다면 각 에셋들의 개수와 Handle을 반환한다</para>
            /// <para>불러오기에 실패시 <c>-1</c>와 실패한 Handle을 반환하며, <paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            /// <para>반드시 <paramref name="addressableHandle"/>을 별도로 보관하여 추후 해제할수있게 해야한다 (원본 IList는 소실되어 Handle로만 해제할수 있다)</para>
            ///</summary>
            ///<param name="addressableLabel">어드레서블 라벨</param>
            ///<param name="targetAssetList">불러온 에셋들이 추가되는 리스트</param>
            ///<param name="addressableHandle">반환되는 어드레서블 핸들</param>
            ///<param name="callback">각 에셋들이 로드될때마다 각각 실행되는 콜백</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public int TryLoadsToTargetList<T>(string addressableLabel, in IList<T> targetAssetList, out AsyncOperationHandle<IList<T>> addressableHandle, Action<T> callback = null, bool autoReleaseHandleWhenFailedResult = true) where T : Object
            {
                //. 에셋들을 불러와본다
                if (TryLoads(addressableLabel, out var assets, out addressableHandle, callback, autoReleaseHandleWhenFailedResult))
                {
                    //. 불러온 에셋들을 각각 리스트에 추가한다
                    for (int i = 0; i < assets.Count; i++) { targetAssetList.Add(assets[i]); }
                    return assets.Count;
                }

                //! 에셋들을 불러오는데 실패했다면 -1을 반환한다
                return -1;
            }



            #endregion



            #region [복수 에셋들] "멀티 라벨"로 불러오기/불러보기



            //? [복수 에셋들] "멀티 라벨"로 불러오기 (동기)

            ///<summary>
            /// [동기] 복수 어드레서블 에셋들을 <b>라벨들</b>로 볼러와 반환한다
            /// <para>불러오기에 실패시 실패한 Handle과 null을 반환하며, <paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            ///</summary>
            ///<param name="addressableLabels">어드레서블 라벨들</param>
            ///<param name="mergeMode">라벨들의 머지 모드</param>
            ///<param name="addressableHandle">반환되는 어드레서블 핸들</param>
            ///<param name="callback">각 에셋들이 로드될때마다 각각 실행되는 콜백</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public IList<T> Loads<T>(IEnumerable<string> addressableLabels, Addressables.MergeMode mergeMode, out AsyncOperationHandle<IList<T>> addressableHandle, Action<T> callback = null, bool autoReleaseHandleWhenFailedResult = true) where T : Object
            {
                Manager.Event.LoadAssetBefore(addressableLabels); //. 에셋들을 불러오기 "이전" 이벤트 실행


                //? 에셋들을 어드레서블로 불러온다
                var resultList = PanAddressableNative.LoadAssets(addressableLabels, callback, mergeMode, out addressableHandle);


                //! 에셋들 불러오기 실패
                if (resultList == null)
                {
                    if (autoReleaseHandleWhenFailedResult) { addressableHandle.Release(); } //. 불러오기 실패시, 핸들을 자동으로 해제
                    FailedLoadMultiAssets(addressableLabels, autoReleaseHandleWhenFailedResult);
                    return null;
                }


                //. 에셋들 불러오기 성공
                Manager.Caching.AddCache(resultList); //. 불러온 에셋들을 캐싱한다
                Manager.Event.LoadAssetsAfterEvent(addressableHandle); //. 에셋들을 불러오기 "이후" 이벤트 실행
                return resultList;
            }

            ///<summary>
            /// [동기] 복수 어드레서블 에셋들을 <b>라벨들</b>로 볼러와 반환한다
            /// <para>불러오기에 실패시 null을 반환하며, Handle을 반환하지 않기에 자동으로 Handle을 Release한다</para>
            ///</summary>
            ///<param name="addressableLabels">어드레서블 라벨들</param>
            ///<param name="mergeMode">라벨들의 머지 모드</param>
            ///<param name="callback">각 에셋들이 로드될때마다 각각 실행되는 콜백</param>
            public IList<T> Loads<T>(IEnumerable<string> addressableLabels, Addressables.MergeMode mergeMode, Action<T> callback = null) where T : Object => Loads<T>(addressableLabels, mergeMode, out _, callback, true);



            //? [복수 에셋들] "멀티 라벨"로 불러오기 (비동기)

            ///<summary>
            /// [비동기] 복수 어드레서블 에셋들을 <b>라벨들</b>로 볼러와 Handle을 반환한다
            /// <para>불러오기에 실패시 실패한 Handle을 반환하며, <paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            ///</summary>
            ///<param name="addressableLabels">어드레서블 라벨들</param>
            ///<param name="mergeMode">라벨들의 머지 모드</param>
            ///<param name="callback">각 에셋들이 로드될때마다 각각 실행되는 콜백</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public async UniTask<AsyncOperationHandle<IList<T>>> LoadsAsyncHandle<T>(IEnumerable<string> addressableLabels, Addressables.MergeMode mergeMode, Action<T> callback = null, bool autoReleaseHandleWhenFailedResult = true) where T : Object
            {
                Manager.Event.LoadAssetBefore(addressableLabels); //. 에셋들을 불러오기 "이전" 이벤트 실행


                //? 에셋들을 어드레서블로 불러온다
                var addressableHandle = PanAddressableNative.LoadAssetsAsyncHandle<T>(addressableLabels, callback, mergeMode);
                await addressableHandle.Task; //. 비동기 대기


                //! 에셋들 불러오기 실패
                if (addressableHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    if (autoReleaseHandleWhenFailedResult) { addressableHandle.Release(); } //. 불러오기 실패시, 핸들을 자동으로 해제
                    FailedLoadMultiAssets(addressableLabels, autoReleaseHandleWhenFailedResult);
                    return addressableHandle;
                }


                //. 에셋들 불러오기 성공
                Manager.Caching.AddCache(addressableHandle.Result); //. 불러온 에셋을 캐싱한다
                Manager.Event.LoadAssetsAfterEvent(addressableHandle); //. 에셋들을 불러오기 "이후" 이벤트 실행
                return addressableHandle; //. 어드레서블 핸들 반환
            }

            ///<summary>
            /// [비동기] 복수 어드레서블 에셋들을 <b>라벨들</b>로 볼러와 반환해보기
            /// <para>불러오기에 실패시 실패한 null을 반환하며, Handle을 반환하지 않기에 자동으로 Handle을 Release한다</para>
            ///</summary>
            ///<param name="addressableLabels">어드레서블 라벨들</param>
            ///<param name="mergeMode">라벨들의 머지 모드</param>
            ///<param name="callback">각 에셋들이 로드될때마다 각각 실행되는 콜백</param>
            ///<param name="autoReleaseHandleWhenFailedResult">호환성 유지용입니다. Handle을 반환하지 않는 이 overload는 실패 handle을 항상 해제합니다.</param>
            public async UniTask<IList<T>> LoadsAsync<T>(IEnumerable<string> addressableLabels, Addressables.MergeMode mergeMode, Action<T> callback = null, bool autoReleaseHandleWhenFailedResult = true) where T : Object
            {
                //! Handle을 반환하지 않는 overload이므로 실패 handle은 항상 해제한다.
                var handle = await LoadsAsyncHandle<T>(addressableLabels, mergeMode, callback, true);
                return handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null;
            }



            //? [복수 에셋들] "멀티 라벨"로 불러오기 (동기)

            ///<summary>
            /// [동기] 복수 어드레서블 에셋들을 <b>라벨들</b>로 볼러와 반환해본다
            /// <para>불러오기에 실패시 실패한 Handle과 null을 반환하며, <paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            ///</summary>
            ///<param name="addressableLabels">어드레서블 라벨들</param>
            ///<param name="mergeMode">라벨들의 머지 모드</param>
            ///<param name="assetList">반환되는 에셋 리스트</param>
            ///<param name="addressableHandle">반환되는 어드레서블 핸들</param>
            ///<param name="callback">각 에셋들이 로드될때마다 각각 실행되는 콜백</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public bool TryLoads<T>(IEnumerable<string> addressableLabels, Addressables.MergeMode mergeMode, out IList<T> assetList, out AsyncOperationHandle<IList<T>> addressableHandle, Action<T> callback = null, bool autoReleaseHandleWhenFailedResult = true) where T : Object
            {
                assetList = Loads(addressableLabels, mergeMode, out addressableHandle, callback, autoReleaseHandleWhenFailedResult);
                return assetList != null;
            }

            ///<summary>
            /// [동기] 복수 어드레서블 에셋들을 <b>라벨들</b>로 볼러와 반환해본다
            /// <para>불러오기에 실패시 실패한 Handle과 null을 반환하며, Handle을 반환하지 않기에 자동으로 Handle을 Release한다</para>
            ///</summary>
            ///<param name="addressableLabels">어드레서블 라벨들</param>
            ///<param name="mergeMode">라벨들의 머지 모드</param>
            ///<param name="assetList">반환되는 에셋 리스트</param>
            ///<param name="callback">각 에셋들이 로드될때마다 각각 실행되는 콜백</param>
            public bool TryLoads<T>(IEnumerable<string> addressableLabels, Addressables.MergeMode mergeMode, out IList<T> assetList, Action<T> callback = null) where T : Object => TryLoads(addressableLabels, mergeMode, out assetList, out _, callback, true);



            //? [복수 에셋들] "멀티 라벨"로 불러와 대상 리스트에 추가해보기 (동기)

            ///<summary>
            /// [동기] 복수 어드레서블 에셋들을 <b>라벨들</b>로 볼러와, <paramref name="targetAssetList"/>에 불러온 에셋들을 각각 추가한다
            /// <para>불러오기에 성공했다면 각 에셋들의 개수와 Handle을 반환한다</para>
            /// <para>불러오기에 실패시 <c>-1</c>와 실패한 Handle을 반환하며, <paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            /// <para>반드시 <paramref name="addressableHandle"/>을 별도로 보관하여 추후 해제할수있게 해야한다 (원본 IList는 소실되어 Handle로만 해제할수 있다)</para>
            ///</summary>
            ///<param name="addressableLabels">어드레서블 라벨들</param>
            ///<param name="targetAssetList">불러온 에셋들이 추가되는 리스트</param>
            ///<param name="addressableHandle">반환되는 어드레서블 핸들</param>
            ///<param name="callback">각 에셋들이 로드될때마다 각각 실행되는 콜백</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public int TryLoadsToTargetList<T>(IEnumerable<string> addressableLabels, Addressables.MergeMode mergeMode, in IList<T> targetAssetList, out AsyncOperationHandle<IList<T>> addressableHandle, Action<T> callback = null, bool autoReleaseHandleWhenFailedResult = true) where T : Object
            {
                //. 에셋들을 불러와본다
                if (TryLoads(addressableLabels, mergeMode, out var assets, out addressableHandle, callback, autoReleaseHandleWhenFailedResult))
                {
                    //. 불러온 에셋들을 각각 리스트에 추가한다
                    for (int i = 0; i < assets.Count; i++) { targetAssetList.Add(assets[i]); }
                    return assets.Count;
                }

                //! 에셋들을 불러오는데 실패했다면 -1을 반환한다
                return -1;
            }




            #endregion



            #region [복수 에셋들+] "라벨"로 Component로 불러오기/불러보기



            //? [복수 에셋들] "라벨"로 Component로 불러오기 (동기)

            ///<summary>
            /// [동기] 복수 어드레서블 에셋들을 <b>라벨</b>로 볼러와 반환한다
            /// <para>불러오기에 실패시 실패한 Handle과 null을 반환하며, <paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            /// <para>반드시 <paramref name="addressableHandle"/>을 별도로 보관하여 추후 해제할수있게 해야한다 (원본 IList는 소실되어 Handle로만 해제할수 있다)</para>
            ///</summary>
            ///<param name="addressableLabel">어드레서블 라벨</param>
            ///<param name="addressableHandle">반환되는 어드레서블 핸들</param>
            ///<param name="callback">각 에셋들이 로드될때마다 각각 실행되는 콜백</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public IList<TComponent> LoadsComponent<TComponent>(string addressableLabel, out AsyncOperationHandle<IList<GameObject>> addressableHandle, Action<GameObject> callback = null, bool autoReleaseHandleWhenFailedResult = true) where TComponent : Component
            {
                //? GameObject 로 에셋들을 얻어본다
                if (TryLoads(addressableLabel, out var assetGameObjectList, out addressableHandle, callback, autoReleaseHandleWhenFailedResult))
                {
                    //. 반환해야하는 Component 리스트를 직접 초기화한다
                    var assetComponentlist = new List<TComponent>(assetGameObjectList.Count);

                    //? GameObject 에셋들을 각각 Component로 변환하여 Component 리스트에 추가한다 (하나라도 변환에 실패시 디버깅과 함께 null이 반환된다, 핸들을 자동으로 해제하기도 한다)
                    if (ConvertAssetList_GameObjectToComponent(addressableHandle, in assetComponentlist, autoReleaseHandleWhenFailedResult))
                    {
                        return assetComponentlist;
                    }
                }


                //! 실패, null과 실패한 핸들을 반환한다
                return null;
            }



            //? [복수 에셋들] "라벨"로 Component로 불러보기 (동기)

            ///<summary>
            /// [동기] 복수 어드레서블 에셋들을 <b>라벨</b>로 볼러와 반환해본다
            /// <para>불러오기에 실패시 실패한 Handle과 null을 반환하며, <paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            /// <para>반드시 <paramref name="addressableHandle"/>을 별도로 보관하여 추후 해제할수있게 해야한다 (원본 IList는 소실되어 Handle로만 해제할수 있다)</para>
            ///</summary>
            ///<param name="addressableLabel">어드레서블 라벨</param>
            ///<param name="addressableHandle">반환되는 어드레서블 핸들</param>
            ///<param name="callback">각 에셋들이 로드될때마다 각각 실행되는 콜백</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public bool TryLoadsComponent<TComponent>(string addressableLabel, out IList<TComponent> assetList, out AsyncOperationHandle<IList<GameObject>> addressableHandle, Action<GameObject> callback = null, bool autoReleaseHandleWhenFailedResult = true) where TComponent : Component
            {
                assetList = LoadsComponent<TComponent>(addressableLabel, out addressableHandle, callback, autoReleaseHandleWhenFailedResult);
                return assetList != null;
            }



            //? [복수 에셋들] "라벨"로 Component로 불러오기 (비동기)

            ///<summary>
            /// [비동기] 복수 <see cref="GameObject"/> 어드레서블 에셋들을 <b>라벨</b>로 볼러와 <typeparamref name="TComponent"/>의 컴포넌트를 얻어 Handle과 함께 반환한다
            /// <para>불러오기에 실패한다면 null과 <see cref="AsyncOperationHandle{T}"/>를 반환하며, <paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            ///</summary>
            ///<param name="addressableLabel">어드레서블 라벨</param>
            ///<param name="callback">각 에셋들이 로드될때마다 각각 실행되는 콜백</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            ///
            public async UniTask<(IList<TComponent> assetList, AsyncOperationHandle<IList<GameObject>> addressableHandle)> LoadsComponentAsyncHandle<TComponent>(string addressableLabel, Action<GameObject> callback = null, bool autoReleaseHandleWhenFailedResult = true) where TComponent : Component
            {
                //? GameObject 로 에셋리스트를 얻어본다
                var addressableHandle = await LoadsAsyncHandle(addressableLabel, callback, autoReleaseHandleWhenFailedResult);


                //? GameObject로 에셋리스트를 얻는데에 성공
                if (addressableHandle.IsValid() && addressableHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    //. 반환해야하는 Component 리스트를 직접 초기화한다
                    var assetComponentlist = new List<TComponent>(addressableHandle.Result.Count);

                    //? GameObject 에셋들을 각각 Component로 변환하여 Component 리스트에 추가한다 (하나라도 변환에 실패시 디버깅과 함께 null이 반환된다, 핸들을 자동으로 해제하기도 한다)
                    if (ConvertAssetList_GameObjectToComponent(addressableHandle, in assetComponentlist, autoReleaseHandleWhenFailedResult))
                    {
                        return new(assetComponentlist, addressableHandle);
                    }
                }


                //! 실패, null과 실패한 핸들을 반환한다
                return new(null, addressableHandle);
            }



            #endregion



            #region [복수 에셋들+] "멀티 라벨"로 Component로 불러오기/불러보기



            //? [복수 에셋들] "멀티 라벨"로 Component로 불러오기 (동기)

            ///<summary>
            /// [동기] 복수 어드레서블 에셋들을 <b>라벨들</b>로 볼러와 반환한다
            /// <para>불러오기에 실패시 실패한 Handle과 null을 반환하며, <paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            /// <para>반드시 <paramref name="addressableHandle"/>을 별도로 보관하여 추후 해제할수있게 해야한다 (원본 IList는 소실되어 Handle로만 해제할수 있다)</para>
            ///</summary>
            ///<param name="addressableLabels">어드레서블 라벨들</param>
            ///<param name="mergeMode">라벨들의 머지 모드</param>
            ///<param name="addressableHandle">반환되는 어드레서블 핸들</param>
            ///<param name="callback">각 에셋들이 로드될때마다 각각 실행되는 콜백</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public IList<TComponent> LoadsComponent<TComponent>(IEnumerable<string> addressableLabels, Addressables.MergeMode mergeMode, out AsyncOperationHandle<IList<GameObject>> addressableHandle, Action<GameObject> callback = null, bool autoReleaseHandleWhenFailedResult = true) where TComponent : Component
            {
                //? GameObject 로 에셋들을 얻어본다
                if (TryLoads(addressableLabels, mergeMode, out var assetGameObjectList, out addressableHandle, callback, autoReleaseHandleWhenFailedResult))
                {
                    //. 반환해야하는 Component 리스트를 직접 초기화한다
                    var assetComponentlist = new List<TComponent>(assetGameObjectList.Count);

                    //? GameObject 에셋들을 각각 Component로 변환하여 Component 리스트에 추가한다 (하나라도 변환에 실패시 디버깅과 함께 null이 반환된다, 핸들을 자동으로 해제하기도 한다)
                    if (ConvertAssetList_GameObjectToComponent(addressableHandle, in assetComponentlist, autoReleaseHandleWhenFailedResult))
                    {
                        return assetComponentlist;
                    }
                }


                //! 에셋을 얻는것 자체에서 실패
                return null;
            }



            //? [복수 에셋들] "멀티 라벨"로 Component로 불러보기 (동기)

            ///<summary>
            /// [동기] 복수 어드레서블 에셋들을 <b>라벨들</b>로 볼러와 반환해본다
            /// <para>불러오기에 실패시 실패한 Handle과 null을 반환하며, <paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            /// <para>반드시 <paramref name="addressableHandle"/>을 별도로 보관하여 추후 해제할수있게 해야한다 (원본 IList는 소실되어 Handle로만 해제할수 있다)</para>
            ///</summary>
            ///<param name="addressableLabels">어드레서블 라벨들</param>
            ///<param name="mergeMode">라벨들의 머지 모드</param>
            ///<param name="addressableHandle">반환되는 어드레서블 핸들</param>
            ///<param name="callback">각 에셋들이 로드될때마다 각각 실행되는 콜백</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public bool TryLoadsComponent<TComponent>(IEnumerable<string> addressableLabels, Addressables.MergeMode mergeMode, out IList<TComponent> assets, out AsyncOperationHandle<IList<GameObject>> addressableHandle, Action<GameObject> callback = null, bool autoReleaseHandleWhenFailedResult = true) where TComponent : Component
            {
                assets = LoadsComponent<TComponent>(addressableLabels, mergeMode, out addressableHandle, callback, autoReleaseHandleWhenFailedResult);
                return assets != null;
            }



            //? [복수 에셋들] "멀티 라벨"로 Component로 불러오기 (비동기)

            ///<summary>
            /// [비동기] 복수 <see cref="GameObject"/> 어드레서블 에셋들을 <b>라벨들</b>로 볼러와 <typeparamref name="TComponent"/>의 컴포넌트를 얻어 Handle과 함께 반환한다
            /// <para>불러오기에 실패한다면 null과 <see cref="AsyncOperationHandle{T}"/>를 반환하며, <paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            ///</summary>
            ///<param name="addressableLabels">어드레서블 라벨들</param>
            ///<param name="mergeMode">라벨들의 머지 모드</param>
            ///<param name="callback">각 에셋들이 로드될때마다 각각 실행되는 콜백</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            ///
            public async UniTask<(IList<TComponent> assets, AsyncOperationHandle<IList<GameObject>> addressableHandle)> LoadsComponentAsyncHandle<TComponent>(IEnumerable<string> addressableLabels, Addressables.MergeMode mergeMode, Action<GameObject> callback = null, bool autoReleaseHandleWhenFailedResult = true) where TComponent : Component
            {
                //? GameObject 로 에셋리스트를 얻어본다
                var addressableHandle = await LoadsAsyncHandle(addressableLabels, mergeMode, callback, autoReleaseHandleWhenFailedResult);


                //? GameObject로 에셋리스트를 얻는데에 성공
                if (addressableHandle.IsValid() && addressableHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    //. 반환해야하는 Component 리스트를 직접 초기화한다
                    var assetComponentlist = new List<TComponent>(addressableHandle.Result.Count);

                    //? GameObject 에셋들을 각각 Component로 변환하여 Component 리스트에 추가한다 (하나라도 변환에 실패시 디버깅과 함께 null이 반환된다, 핸들을 자동으로 해제하기도 한다)
                    if (ConvertAssetList_GameObjectToComponent(addressableHandle, in assetComponentlist, autoReleaseHandleWhenFailedResult))
                    {
                        return new(assetComponentlist, addressableHandle);
                    }
                }


                //! 실패, null과 실패한 핸들을 반환한다
                return new(null, addressableHandle);
            }



            #endregion



            #region [복수 에셋들++] 비동기 Lazy로 불러오는 메서드 모음



            //? [복수 에셋들++] "라벨"로 불러오기 (비동기 Lazy)

            ///<summary>
            /// [비동기 Lazy] 복수 어드레서블 에셋들을 <b>라벨</b>로 비동기로 볼러와 <paramref name="onSucceeded"/> 또는 <paramref name="onFailed"/>를 Callback 한다
            /// <para>불러오기에 실패시,<paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            ///</summary>
            ///<param name="addressableLabel">어드레서블 라벨</param>
            ///<param name="onSucceeded">성공할때 실행되는 콜백</param>
            ///<param name="onFailed">실패할때 실행되는 콜백</param>
            ///<param name="callback">각 에셋들이 로드될때마다 각각 실행되는 콜백</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public void LoadsLazyAsync<T>(string addressableLabel, Action<IList<T>, AsyncOperationHandle> onSucceeded, Action<AsyncOperationHandle> onFailed, Action<T> callback, bool autoReleaseHandleWhenFailedResult = true) where T : Object
            {
                Manager.Event.LoadAssetsBefore(addressableLabel); //. 에셋들을 불러오기 "이전" 이벤트 실행


                //? 어드레서블로 에셋들을 불러오되, 얻어올때까지 대기하며 (비동기), 완료를 콜백 메서드로 받는다
                PanAddressableNative.LoadAssetsAsyncCallback(addressableLabel, callback, (addressableHandle =>
                {
                    //? 에셋들 불러오기에 성공
                    if (addressableHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        Manager.Caching.AddCache(addressableHandle.Result); //. 불러온 에셋들을 캐싱한다
                        Manager.Event.LoadAssetsAfterEvent(addressableHandle); //. 에셋들을 불러오기 "이후" 이벤트 실행
                        onSucceeded?.Invoke(addressableHandle.Result, addressableHandle); //? 성공 콜백 실행
                    }

                    //! 에셋들 불러오기에 실패
                    else
                    {
                        if (autoReleaseHandleWhenFailedResult) { addressableHandle.Release(); } //. 불러오기 실패시, 핸들을 자동으로 해제
                        FailedLoadSingleAsset(addressableLabel, autoReleaseHandleWhenFailedResult);
                        onFailed?.Invoke(addressableHandle); //! 실패 콜백 실행
                    }
                }));
            }



            //? [복수 에셋들++] "라벨"로 Component로 불러오기 (비동기 Lazy)

            ///<summary>
            /// [비동기 Lazy] 복수 <see cref="GameObject"/> 어드레서블 에셋들을 <b>라벨</b>로 비동기로 볼러와 <typeparamref name="TComponent"/>의 컴포넌트를 얻어 <paramref name="onSucceeded"/> 또는 <paramref name="onFailed"/>를 Callback 한다
            /// <para>불러오기에 실패시,<paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            ///</summary>
            ///<param name="addressableLabel">어드레서블 라벨</param>
            ///<param name="onSucceeded">성공할때 실행되는 콜백</param>
            ///<param name="onFailed">실패할때 실행되는 콜백</param>
            ///<param name="callback">각 에셋들이 로드될때마다 각각 실행되는 콜백</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public void LoadsComponentLazyAsync<TComponent>(string addressableLabel, Action<IList<TComponent>, AsyncOperationHandle> onSucceeded, Action<AsyncOperationHandle> onFailed, Action<GameObject> callback, bool autoReleaseHandleWhenFailedResult = true) where TComponent : Component
            {
                Manager.Event.LoadAssetsBefore(addressableLabel); //. 에셋들을 불러오기 "이전" 이벤트 실행


                //? 어드레서블로 에셋들을 불러오되, GameObject로 불러오며 얻어올때까지 대기하며 (비동기), 완료를 콜백 메서드로 받는다
                PanAddressableNative.LoadAssetsAsyncCallback(addressableLabel, callback, (addressableHandle =>
                {
                    //? 에셋들 불러오기에 성공
                    if (addressableHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        //. 반환해야하는 Component 리스트를 직접 초기화한다
                        var assetComponentlist = new List<TComponent>(addressableHandle.Result.Count);

                        //? GameObject 에셋들을 각각 Component로 변환하여 Component 리스트에 추가한다 (하나라도 변환에 실패시 디버깅과 함께 null이 반환된다, 핸들을 자동으로 해제하기도 한다)
                        if (ConvertAssetList_GameObjectToComponent(addressableHandle, in assetComponentlist, autoReleaseHandleWhenFailedResult))
                        {
                            Manager.Caching.AddCache(addressableHandle.Result); //. 불러온 에셋을 캐싱한다
                            Manager.Event.LoadAssetsAfterEvent(addressableHandle); //. 에셋들을 불러오기 "이후" 이벤트 실행
                            onSucceeded?.Invoke(assetComponentlist, addressableHandle); //? 성공 콜백 실행
                        }

                        //! Component 변환에 실패
                        else
                        {
                            FailedLoadMultiAssets(addressableLabel, autoReleaseHandleWhenFailedResult);
                            onFailed?.Invoke(addressableHandle); //! 실패 콜백 실행
                        }
                    }

                    //! 에셋들 불러오기에 실패
                    else
                    {
                        if (autoReleaseHandleWhenFailedResult) { addressableHandle.Release(); } //. 불러오기 실패시, 핸들을 자동으로 해제
                        FailedLoadSingleAsset(addressableLabel, autoReleaseHandleWhenFailedResult);
                        onFailed?.Invoke(addressableHandle); //! 실패 콜백 실행
                    }
                }));
            }



            //? [복수 에셋들++] "멀티 라벨"로 불러오기 (비동기 Lazy)

            ///<summary>
            /// [비동기 Lazy] 복수 어드레서블 에셋들을 <b>라벨들</b>로 비동기로 볼러와 <paramref name="onSucceeded"/> 또는 <paramref name="onFailed"/>를 Callback 한다
            /// <para>불러오기에 실패시,<paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            ///</summary>
            ///<param name="addressableLabels">어드레서블 라벨들</param>
            ///<param name="onSucceeded">성공할때 실행되는 콜백</param>
            ///<param name="onFailed">실패할때 실행되는 콜백</param>
            ///<param name="callback">각 에셋들이 로드될때마다 각각 실행되는 콜백</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public void LoadsLazyAsync<T>(IEnumerable<string> addressableLabels, Addressables.MergeMode mergeMode, Action<IList<T>, AsyncOperationHandle> onSucceeded, Action<AsyncOperationHandle> onFailed, Action<T> callback, bool autoReleaseHandleWhenFailedResult = true) where T : Object
            {
                Manager.Event.LoadAssetBefore(addressableLabels); //. 에셋을 불러오기 "이전" 이벤트 실행


                //? 에셋들을 어드레서블로 불러오되, 얻어올때까지 대기하며 (비동기), 완료를 콜백 메서드로 받는다
                PanAddressableNative.LoadAssetsAsyncCallback(addressableLabels, callback, mergeMode, (addressableHandle =>
                {
                    //? 에셋들 불러오기에 성공
                    if (addressableHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        Manager.Caching.AddCache(addressableHandle.Result); //. 불러온 에셋들을 캐싱한다
                        Manager.Event.LoadAssetsAfterEvent(addressableHandle); //. 에셋들을 불러오기 "이후" 이벤트 실행
                        onSucceeded?.Invoke(addressableHandle.Result, addressableHandle); //? 성공 콜백 실행
                    }

                    //! 에셋들 불러오기에 실패
                    else
                    {
                        if (autoReleaseHandleWhenFailedResult) { addressableHandle.Release(); } //. 불러오기 실패시, 핸들을 자동으로 해제
                        FailedLoadMultiAssets(addressableLabels, autoReleaseHandleWhenFailedResult);
                        onFailed?.Invoke(addressableHandle); //! 실패 콜백 실행
                    }
                }));
            }



            //? [복수 에셋들++] "멀티 라벨"로 Component로 불러오기 (비동기 Lazy)

            ///<summary>
            /// [비동기 Lazy] 복수 <see cref="GameObject"/> 어드레서블 에셋들을 <b>라벨</b>로 비동기로 볼러와 <typeparamref name="TComponent"/>의 컴포넌트를 얻어 <paramref name="onSucceeded"/> 또는 <paramref name="onFailed"/>를 Callback 한다
            /// <para>불러오기에 실패시,<paramref name="autoReleaseHandleWhenFailedResult"/>의 여부에 따라 자동으로 Release된다</para>
            ///</summary>
            ///<param name="addressableLabels">어드레서블 라벨</param>
            ///<param name="onSucceeded">성공할때 실행되는 콜백</param>
            ///<param name="onFailed">실패할때 실행되는 콜백</param>
            ///<param name="callback">각 에셋들이 로드될때마다 각각 실행되는 콜백</param>
            ///<param name="autoReleaseHandleWhenFailedResult">반환에 실패시, 어드레서블 핸들을 자동으로 해제될지의 여부</param>
            public void LoadsComponentLazyAsync<TComponent>(IEnumerable<string> addressableLabels, Addressables.MergeMode mergeMode, Action<IList<TComponent>, AsyncOperationHandle> onSucceeded, Action<AsyncOperationHandle> onFailed, Action<GameObject> callback, bool autoReleaseHandleWhenFailedResult = true) where TComponent : Component
            {
                Manager.Event.LoadAssetBefore(addressableLabels); //. 에셋들을 불러오기 "이전" 이벤트 실행


                //? 에셋들을 GameObject로 어드레서블로 불러오되, 얻어올때까지 대기하며 (비동기), 완료를 콜백 메서드로 받는다
                PanAddressableNative.LoadAssetsAsyncCallback(addressableLabels, callback, mergeMode, (addressableHandle =>
                {
                    //? 에셋들 불러오기에 성공
                    if (addressableHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        //. 반환해야하는 Component 리스트를 직접 초기화한다
                        var assetComponentlist = new List<TComponent>(addressableHandle.Result.Count);

                        //? GameObject 에셋들을 각각 Component로 변환하여 Component 리스트에 추가한다 (하나라도 변환에 실패시 디버깅과 함께 null이 반환된다, 핸들을 자동으로 해제하기도 한다)
                        if (ConvertAssetList_GameObjectToComponent(addressableHandle, in assetComponentlist, autoReleaseHandleWhenFailedResult))
                        {
                            Manager.Caching.AddCache(addressableHandle.Result); //. 불러온 에셋을 캐싱한다
                            Manager.Event.LoadAssetsAfterEvent(addressableHandle); //. 에셋을 불러오기 "이후" 이벤트 실행
                            onSucceeded?.Invoke(assetComponentlist, addressableHandle); //? 성공 콜백 실행
                        }

                        //! Component 변환에 실패
                        else
                        {
                            FailedLoadMultiAssets(addressableLabels, autoReleaseHandleWhenFailedResult);
                            onFailed?.Invoke(addressableHandle); //! 실패 콜백 실행
                        }
                    }

                    //! 에셋들 불러오기에 실패
                    else
                    {
                        if (autoReleaseHandleWhenFailedResult) { addressableHandle.Release(); } //. 불러오기 실패시, 핸들을 자동으로 해제
                        FailedLoadMultiAssets(addressableLabels, autoReleaseHandleWhenFailedResult);
                        onFailed?.Invoke(addressableHandle); //! 실패 콜백 실행
                    }
                }));
            }



            #endregion



            ///======================================================================================================================================================



            //? 어드레서블 해제하기



            ///<summary>
            ///어드레서블 에셋을 해제한다 (범용 <see cref="AsyncOperationHandle"/>)
            ///</summary>
            ///<param name="handle">대상 어드레서블 핸들</param>
            ///<param name="removeCache">캐싱에서 제거할지 여부</param>
            public void Release(AsyncOperationHandle handle, bool removeCache = true)
            {
                if (!handle.IsValid()) { return; } //! 핸들이 유효해야한다
                if (removeCache) { Manager.Caching.RemoveCache(handle); } //. 캐싱에서 제거한다
                PanAddressableNative.Release(handle); //. 어드레서블 에셋에서 제거한다
            }



            ///<summary>
            ///어드레서블 에셋을 해제한다 (단일 <see cref="AsyncOperationHandle{T}"/>)
            ///</summary>
            ///<param name="handle">대상 어드레서블 핸들</param>
            ///<param name="removeCache">캐싱에서 제거할지 여부</param>
            public void Release<T>(AsyncOperationHandle<T> handle, bool removeCache = true) where T : Object
            {
                if (!handle.IsValid()) { return; } //! 핸들이 유효해야한다
                if (removeCache) { Manager.Caching.RemoveCache(handle); } //. 캐싱에서 제거한다
                PanAddressableNative.Release(handle); //. 어드레서블 에셋에서 제거한다
            }



            ///<summary>
            ///어드레서블 에셋을 해제한다 (복수 <see cref="AsyncOperationHandle{T}"/>)
            ///</summary>
            ///<param name="handle">대상 어드레서블 핸들</param>
            ///<param name="removeCache">캐싱에서 제거할지 여부</param>
            public void Release<T>(AsyncOperationHandle<IList<T>> handle, bool removeCache = true) where T : Object
            {
                if (!handle.IsValid()) { return; } //! 핸들이 유효해야한다
                if (removeCache) { Manager.Caching.RemoveCache(handle); } //. 캐싱에서 제거한다
                PanAddressableNative.Release(handle); //. 어드레서블 에셋에서 제거한다
            }



            ///<summary>
            ///어드레서블 에셋을 해제한다 (단일 에셋)
            ///</summary>
            ///<param name="asset">대상 어드레서블 에셋</param>
            ///<param name="removeCache">캐싱에서 제거할지 여부</param>
            public void Release<T>(T asset, bool removeCache = true) where T : Object
            {
                if (asset == null) { return; } //! 에셋이 유효해야한다
                if (removeCache) { Manager.Caching.RemoveCache(asset); } //. 캐싱에서 제거한다
                PanAddressableNative.Release(asset); //. 어드레서블 에셋에서 제거한다
            }



            ///<summary>
            ///어드레서블 에셋을 해제한다 (복수 에셋 리스트)
            ///</summary>
            ///<param name="assetList">대상 어드레서블 에셋 리스트</param>
            ///<param name="removeCache">캐싱에서 제거할지 여부</param>
            public void Release<T>(IList<T> assetList, bool removeCache = true) where T : Object
            {
                if (assetList == null) { return; } //! 에셋리스트가 유효해야한다
                if (removeCache) { Manager.Caching.RemoveCache(assetList); } //. 캐싱에서 제거한다
                PanAddressableNative.Release(assetList); //. 어드레서블 에셋에서 제거한다
            }



            ///<summary>
            ///캐싱되어있는 어드레서블 에셋들을 기준으로, 모든 어드레서블 에셋을 해제한다
            ///</summary>
            public void ReleaseAllCache()
            {
                lock (Manager.Caching.lockobj)
                {
                    foreach (var origin in Manager.Caching.ReleaseOrigins)
                    {
                        for (int i = 0; i < origin.Value; i++)
                        {
                            //. foreach 도중이니 캐시에서 제거하지않고 Release 될수 있도록 한다
                            PanAddressableNative.Release(origin.Key);
                        }
                    }

                    //. 캐시되어있는 모든 에셋들을 Release 해줬다면, 남겨진 모든 캐시를 초기화한다
                    Manager.Caching.ClearCachingAssets();
                }
            }



            ///======================================================================================================================================================



            //? 에디터 테스트



#if UNITY_EDITOR



            [FoldoutGroup("어드레서블 테스트"), BoxGroup("어드레서블 테스트/박스", ShowLabel = false)]
            [Button("단일 에셋 불러오기 테스트", ButtonStyle.Box, Expanded = true)]
            [GUIColor(0.8f, 1f, 0.8f)]
            private void EditorTest_LoadSingleAsset(
                [LabelText("🛑 비동기")] bool isAsync,
                [LabelText("🏷️ 어드레서블 이름")] string addressableName
                )
            {
                if (!isAsync)
                {
                    if (TryLoad<Object>(addressableName, out var result)) { Debug.Log($"[어드레서블] 단일 에셋 동기 불러오기 테스트 성공 {addressableName}"); }
                    else { Debug.LogError($"[어드레서블] 단일 에셋 동기 불러오기 테스트 실패 {addressableName}"); }
                }
                else
                {
                    EditorTest_LoadSingleAssetAsync(addressableName).Forget();
                }
            }

            private async UniTask EditorTest_LoadSingleAssetAsync(string addressableName)
            {
                var result = await TryLoadAsyncHandle<Object>(addressableName);

                if (result.success) { Debug.Log($"[어드레서블] 단일 에셋 비동기 불러오기 테스트 성공 {addressableName}"); }
                else { Debug.LogError($"[어드레서블] 단일 에셋 비동기 불러오기 테스트 실패 {addressableName}"); }
            }



            [FoldoutGroup("어드레서블 테스트"), BoxGroup("어드레서블 테스트/박스", ShowLabel = false)]
            [Button("복수 에셋들 불러오기 테스트", ButtonStyle.Box, Expanded = true)]
            [GUIColor(0.8f, 1f, 0.8f)]
            private void EditorTest_LoadMultiAssets(
                [LabelText("🛑 비동기")] bool isAsync,
                [LabelText("🔖 어드레서블 라벨")] string addressableLabel
                )
            {
                if (!isAsync)
                {
                    if (TryLoads<Object>(addressableLabel, out var resultList)) { Debug.Log($"[어드레서블] 복수 에셋 동기 불러오기 테스트 성공 {addressableLabel}"); }
                    else { Debug.LogError($"[어드레서블] 복수 에셋 동기 불러오기 테스트 실패 {addressableLabel}"); }
                }
                else
                {
                    EditorTest_LoadMultiAssetsAsync(addressableLabel).Forget();
                }
            }

            private async UniTask EditorTest_LoadMultiAssetsAsync(string addressableLabel)
            {
                var result = await LoadsAsyncHandle<Object>(addressableLabel);
                if (result.IsValid() && result.Status == AsyncOperationStatus.Succeeded) { Debug.Log($"[어드레서블] 단일 에셋 비동기 불러오기 테스트 성공 {addressableLabel}"); }
                else { Debug.LogError($"[어드레서블] 단일 에셋 비동기 불러오기 테스트 실패 {addressableLabel}"); }
            }



            [FoldoutGroup("어드레서블 테스트"), BoxGroup("어드레서블 테스트/박스", ShowLabel = false)]
            [Button("에셋 해제 테스트", ButtonStyle.Box, Expanded = true)]
            [GUIColor(0.8f, 1f, 0.8f)]
            private void EditorTest_LoadSingleAsset(
          [LabelText("🏷️ 어드레서블 이름")] string addressableName
          )
            {
                var findCache = Manager.Caching.GetCachingAssets.First(x => x.Key.name == addressableName).Key;
                if (findCache != null)
                {
                    Release(findCache);
                }
                else
                {
                    Debug.LogError($"[어드레서블] 캐싱되어있는 {addressableName} 을 찾지 못함");
                }
            }



#endif



            ///======================================================================================================================================================
        }



        /// <summary>
        ///     Addressables 관련 <b>의존성(Dependencies) 다운로드</b> 및
        ///     <b>예상 다운로드 용량 조회</b> 기능을 제공하는 매니저.
        ///     <para>
        ///     실제 로딩을 담당하는 <see cref="Loaders"/> 와는 분리하여,
        ///     다운로드(캐시 프리패칭) 단계의 책임만을 갖는다.
        ///     </para>
        /// </summary>
        [Serializable]
        public class Downloaders : Base
        {
            ///======================================================================================================================================================



            public Downloaders(PanAddressableManager manager) : base(manager) { }



            ///======================================================================================================================================================



            #region ► Dependencies Download

            ///<summary>
            ///  단일 <paramref name="addressableKey"/> 에 대한 <b>Dependencies</b> 를 동기 다운로드한다.
            ///  <para>이미 캐시돼 있다면 즉시 완료된다.</para>
            ///</summary>
            ///<param name="addressableKey">어드레서블 키 (주소, 라벨, AssetReference 등)</param>
            ///<param name="autoReleaseHandle">다운로드 완료 후 핸들을 자동 Release 할지 여부</param>
            ///<returns>다운로드 작업 결과 핸들(<see cref="AsyncOperationHandle"/>)</returns>
            public object DownloadDependencies(object addressableKey, bool autoReleaseHandle = false)
            {
                // Addressables API 는 비동기이므로 WaitForCompletion() 으로 동기 블로킹 처리
                return Addressables.DownloadDependenciesAsync(addressableKey, autoReleaseHandle)
                                    .WaitForCompletion();
            }

            ///<summary>
            ///  <paramref name="labelKeys"/> 가 가리키는 <b>여러 리소스</b> 의 Dependencies 를 동기 다운로드한다.
            ///</summary>
            ///<param name="labelKeys">라벨 키 컬렉션 (IEnumerable)</param>
            ///<param name="mergeMode">다중 라벨 머지 모드</param>
            ///<param name="autoReleaseHandle">다운로드 완료 후 핸들을 자동 Release 할지 여부</param>
            public object DownloadDependencies(IEnumerable labelKeys, Addressables.MergeMode mergeMode,
                                               bool autoReleaseHandle = false)
            {
                return Addressables.DownloadDependenciesAsync(labelKeys, mergeMode, autoReleaseHandle)
                                    .WaitForCompletion();
            }

            ///<summary>
            ///  사전 조회한 <see cref="IResourceLocation"/> 목록에 대한 Dependencies 를 동기 다운로드한다.
            ///</summary>
            public object DownloadDependencies(IList<IResourceLocation> resourceLocations,
                                               Addressables.MergeMode mergeMode,
                                               bool autoReleaseHandle = false)
            {
                return Addressables.DownloadDependenciesAsync(resourceLocations, mergeMode, autoReleaseHandle)
                                    .WaitForCompletion();
            }

            //--------------------------------------------- Async counterparts --------------------------------------------------

            ///<summary>
            ///  단일 <paramref name="addressableKey"/> 에 대한 Dependencies 를 <b>비동기</b> 다운로드한다.
            ///</summary>
            public async UniTask<object> DownloadDependenciesAsync(object addressableKey, bool autoReleaseHandle = false)
            {
                // 비동기 핸들을 얻고, UniTask 로 await
                var handle = Addressables.DownloadDependenciesAsync(addressableKey, autoReleaseHandle);
                await handle.Task; // 완료 대기
                return handle;
            }

            ///<summary>
            ///  여러 <paramref name="labelKeys"/> 의 Dependencies 를 <b>비동기</b> 다운로드한다.
            ///</summary>
            public async UniTask<object> DownloadDependenciesAsync(IEnumerable labelKeys,
                                                                   Addressables.MergeMode mergeMode,
                                                                   bool autoReleaseHandle = false)
            {
                var handle = Addressables.DownloadDependenciesAsync(labelKeys, mergeMode, autoReleaseHandle);
                await handle.Task;
                return handle;
            }

            ///<summary>
            ///  <see cref="IResourceLocation"/> 목록의 Dependencies 를 <b>비동기</b> 다운로드한다.
            ///</summary>
            public async UniTask<object> DownloadDependenciesAsync(IList<IResourceLocation> resourceLocations,
                                                                   Addressables.MergeMode mergeMode,
                                                                   bool autoReleaseHandle = false)
            {
                var handle = Addressables.DownloadDependenciesAsync(resourceLocations, mergeMode, autoReleaseHandle);
                await handle.Task;
                return handle;
            }

            #endregion



            ///======================================================================================================================================================



            #region ► Size Query

            ///<summary>
            ///  단일 <paramref name="addressableKey"/> 를 다운로드할 때 필요한 <b>바이트 수</b> 를 동기적으로 반환한다.
            ///</summary>
            public long GetDownloadSize(object addressableKey)
            {
                var handle = Addressables.GetDownloadSizeAsync(addressableKey);
                try
                {
                    return handle.WaitForCompletion();
                }
                finally
                {
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }
                }
            }

            ///<summary>
            ///  <paramref name="addressablePath"/> 문자열 키 하나에 대한 다운로드 용량을 반환한다.
            ///</summary>
            public long GetDownloadSize(string addressablePath)
            {
                var handle = Addressables.GetDownloadSizeAsync(addressablePath);
                try
                {
                    return handle.WaitForCompletion();
                }
                finally
                {
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }
                }
            }

            ///<summary>
            ///  <paramref name="labelKeys"/> 컬렉션 전체의 예상 다운로드 용량을 반환한다.
            ///</summary>
            public long GetDownloadSize(IEnumerable labelKeys)
            {
                var handle = Addressables.GetDownloadSizeAsync(labelKeys);
                try
                {
                    return handle.WaitForCompletion();
                }
                finally
                {
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }
                }
            }

            //--------------------------------------------- Async counterparts --------------------------------------------------

            ///<summary>
            ///  단일 <paramref name="addressableKey"/> 의 예상 다운로드 용량을 <b>비동기</b>로 조회한다.
            ///</summary>
            public async UniTask<long> GetDownloadSizeAsync(object addressableKey)
            {
                var handle = Addressables.GetDownloadSizeAsync(addressableKey);
                try
                {
                    return await handle.Task;
                }
                finally
                {
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }
                }
            }

            ///<summary>
            ///  문자열 키(<paramref name="addressablePath"/>) 의 예상 다운로드 용량을 <b>비동기</b>로 조회한다.
            ///</summary>
            public async UniTask<long> GetDownloadSizeAsync(string addressablePath)
            {
                var handle = Addressables.GetDownloadSizeAsync(addressablePath);
                try
                {
                    return await handle.Task;
                }
                finally
                {
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }
                }
            }

            ///<summary>
            ///  다수 <paramref name="labelKeys"/> 의 예상 다운로드 용량을 <b>비동기</b>로 조회한다.
            ///</summary>
            public async UniTask<long> GetDownloadSizeAsync(IEnumerable labelKeys)
            {
                var handle = Addressables.GetDownloadSizeAsync(labelKeys);
                try
                {
                    return await handle.Task;
                }
                finally
                {
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }
                }
            }

            #endregion



            ///======================================================================================================================================================
        }



        ///======================================================================================================================================================



    }
}
