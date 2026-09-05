using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using SitraUtils;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.AddressableAssets;
using ZLinq;



//? 무한스택 매니저가 정리되어있는 정도의 코드



namespace Pan.Util
{
    internal static class ZLinqEnumerableCache
    {
        internal static T[] Materialize<T>(IEnumerable<T> source)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            return source as T[] ?? source.AsValueEnumerable().ToArray();
        }
    }



    ///======================================================================================================================================================



    //? 무한스택 매니저 관련 인터페이스



    /// <summary>
    /// 무한스택 매니저에 Key를 클래스 기반으로 사용했을때 사용되는 인터페이스
    /// <para>풀딕셔너리에 접근하는 CRUD 메서드들을 제네릭으로 접근이 가능</para>
    /// <para><typeparamref name="TKey"/>와 <typeparamref name="TElement"/>를 기존 무한스택 매니저와 다르게 설정도 할수있음</para>
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TElement"></typeparam>
    public interface IInfinityStackManagerBase_ClassKey<TKey, TElement>
        where TKey : class
        where TElement : class
    {
        ///======================================================================================================================================================



        //? 풀 딕셔너리 확인



        /// <summary>
        /// <paramref name="key">에 속한 <typeparamref name="TInfinityStack"/>의  <see cref="TInfinityStack.PoolCount"/> 를 구해보기
        /// </summary>
        bool TryGetPoolCount<T>(out int value) where T : TKey;



        ///======================================================================================================================================================



        //? 요소 생성 (Create)



        /// <summary>
        /// <typeparamref name="TElement"/>를 <paramref name="createCount"/> 만큼 생성한다
        /// <para>각 풀의 <see cref="TInfinityStack.MaxElementCount"/> 를 넘으면 반드시 생성을 보장하지 않으며,</para>
        /// <para>그렇게 <typeparamref name="TElement"/>를 생성된 개수를 반환한다 (int), 하나도 생성하지 못하면 0을 반환한다</para>
        /// <para>내부에서 <see cref="TInfinityStack.CreateInternal(int, Action{TElement})"/>을 재정의하여 그 곳에서 각 <typeparamref name="TElement"/>에 맞게 생성한다</para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="createCount"><typeparamref name="TElement"/>를 생성할 개수</param>
        /// <returns></returns>
        int Create<T>(int createCount) where T : TKey;



        /// <summary>
        /// [비동기] <typeparamref name="TElement"/>를 <paramref name="createCount"/> 만큼 생성한다
        /// <para>각 풀의 <see cref="TInfinityStack.MaxElementCount"/> 를 넘으면 반드시 생성을 보장하지 않으며,</para>
        /// <para>그렇게 <typeparamref name="TElement"/>를 생성된 개수를 반환한다 (int), 하나도 생성하지 못하면 0을 반환한다</para>
        /// <para>내부에서 <see cref="TInfinityStack.CreateInternal(int, Action{TElement})"/>을 재정의하여 그 곳에서 각 <typeparamref name="TElement"/>에 맞게 생성한다</para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="createCount"><typeparamref name="TElement"/>를 생성할 개수</param>
        /// <returns></returns>
        UniTask<int> CreateAsync<T>(int createCount) where T : TKey;



        ///======================================================================================================================================================



        //? 요소 넣기 (Push)



        /// <summary>
        /// <typeparamref name="TElement"/>를 넣는다 (반환한다)
        /// <para>각 풀의 <see cref="TInfinityStack.inPool"/>에서 중복 검사를 하여, 이미 추가된 <typeparamref name="TElement"/> 일 경우 false를 반환한다</para>
        /// <para>디버그 모드에 따라 이를 메세지로 출력 하기도 한다</para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="element">넣을(반환할) 요소</param>
        void Push<T>(TElement value) where T : TKey;



        /// <summary>
        /// <typeparamref name="TElement"/>들을 "배열"로 넣는다 (반환한다)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="elements">넣을(반환할) 요소 들의 배열</param>
        void PushRange<T>(params TElement[] elements) where T : TKey;



        /// <summary>
        /// <typeparamref name="TElement"/>들을 "리스트"로 넣는다 (반환한다)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="elements">넣을(반환할) 요소 들의 배열</param>
        void PushRange<T>(IList<TElement> elements) where T : TKey;



        /// <summary>
        /// <typeparamref name="TElement"/>들을 "IEnumberable"로 넣는다 (반환한다)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="elements">넣을(반환할) 요소 들의 배열</param>
        void PushRange<T>(IEnumerable<TElement> elements) where T : TKey;



        ///======================================================================================================================================================



        //? 요소 해제하기 (Release)



        /// <summary>
        /// <see cref="TInfinityStack.poolStack"/>에 생성한 <typeparamref name="TElement"/>들을 해제(제거)하며
        /// <para>해제에 성공한 개수를 반환한다</para>
        /// <para>정말 더이상 사용 하지 않을때, 메모리를 확보 하기 위해 사용한다</para>
        /// <para><see cref="PoolDictionary"/>에 <paramref name="key"/>가 존재하지 않으면 0을 반환한다</para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="releaseCount">해제할 <typeparamref name="TElement"/>의 개수</param>
        int Release<T>(int count) where T : TKey;



        /// <summary>
        /// <see cref="TInfinityStack.poolStack"/>에 생성한 모든<typeparamref name="TElement"/>들을 해제(제거)한다
        /// <para>정말 더이상 사용 하지 않을때, 메모리를 확보 하기 위해 사용한다</para>
        /// <para><paramref name="removePoolInPoolDictionary"/>를 사용 할 경우, 해당 <typeparamref name="TInfinityStack"/> 자체를 해제하여 레퍼런스 카운트를 0 으로 만든다</para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="removePoolInPoolDictionary"><typeparamref name="TInfinityStack"/> 를 딕셔너리에서 제거 할 지 여부</param>
        void ReleaseClear<T>(bool removePoolInPoolDictionary) where T : TKey;



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 무한스택 매니저에 Key를 클래스 기반으로 사용했을때 사용되는 인터페이스
    /// <para>풀딕셔너리에 접근하는 CRUD 메서드들을 제네릭으로 접근이 가능</para>
    /// <para><typeparamref name="TKey"/>와 <typeparamref name="TElement"/>를 기존 무한스택 매니저와 다르게 설정도 할수있음</para>
    /// <para><b>Pop 메서드가 <typeparamref name="TElement"/>를 반환하게 되어있는 인터페이스</b></para>
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TElement"></typeparam>
    public interface IInfinityStackManagerBase_ClassKey_PopElement<TKey, TElement> : IInfinityStackManagerBase_ClassKey<TKey, TElement>
        where TKey : class
        where TElement : class
    {
        //? 요소 꺼내기 (Pop)



        /// <summary>
        /// <typeparamref name="TElement"/>를 꺼낸다
        /// <para>풀의 상태와 파라미터에 따라, <b>반환을 무조건 보장하지는 않으며</b>, 이때 null을 반환한다</para>
        /// </summary>
        /// <param name="autoTryCreateElementsWhenPoolNotEnough">
        /// 꺼내야 하는데, 풀의 크기가 모자랄 경우 (+풀이 존재 하지 않거나), <see cref="Create(int, Action{TElement})"/>을 한 이후 반환 할 지 여부
        /// <para>null을 받아오면, 풀의 크기가 모자랄경우 반환에 실패한다</para>
        /// <para>값을 넣더라도, <see cref="TInfinityStack.MaxElementCount"/>에 의해 반환에 실패 할 수도 있다</para>
        /// <para>기본값: 1</para>
        /// </param>
        /// <returns></returns>
        TElement Pop<T>(int? autoTryCreateElementsWhenPoolNotEnough = 0) where T : class, TKey;



        /// <summary>
        /// [비동기] <typeparamref name="TElement"/>를 꺼낸다
        /// <para>풀의 상태와 파라미터에 따라, <b>반환을 무조건 보장하지는 않으며</b>, 이때 null을 반환한다</para>
        /// </summary>
        /// <param name="autoTryCreateElementsWhenPoolNotEnough">
        /// 꺼내야 하는데, 풀의 크기가 모자랄 경우 (+풀이 존재 하지 않거나), <see cref="Create(int, Action{TElement})"/>을 한 이후 반환 할 지 여부
        /// <para>null을 받아오면, 풀의 크기가 모자랄경우 반환에 실패한다</para>
        /// <para>값을 넣더라도, <see cref="TInfinityStack.MaxElementCount"/>에 의해 반환에 실패 할 수도 있다</para>
        /// <para>기본값: 1</para>
        /// </param>
        /// <returns></returns>
        UniTask<TElement> PopAsync<T>(int? autoTryCreateElementsWhenPoolNotEnough = 1) where T : class, TKey;
    }



    /// <summary>
    /// 무한스택 매니저에 Key를 클래스 기반으로 사용했을때 사용되는 인터페이스
    /// <para>풀딕셔너리에 접근하는 CRUD 메서드들을 제네릭으로 접근이 가능</para>
    /// <para><typeparamref name="TKey"/>와 <typeparamref name="TElement"/>를 기존 무한스택 매니저와 다르게 설정도 할수있음</para>
    /// <para><b>Pop 메서드가 <typeparamref name="TKey"/>를 상속받는 제네릭을 반환하게 되어있는 인터페이스</b></para>
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TElement"></typeparam>
    public interface IInfinityStackManagerBase_ClassKey_PopGeneric<TKey, TElement> : IInfinityStackManagerBase_ClassKey<TKey, TElement>
        where TKey : class
        where TElement : class
    {
        //? 요소 꺼내기 (Pop)



        /// <summary>
        /// <typeparamref name="TElement"/>를 꺼낸다
        /// <para>풀의 상태와 파라미터에 따라, <b>반환을 무조건 보장하지는 않으며</b>, 이때 null을 반환한다</para>
        /// </summary>
        /// <param name="autoTryCreateElementsWhenPoolNotEnough">
        /// 꺼내야 하는데, 풀의 크기가 모자랄 경우 (+풀이 존재 하지 않거나), <see cref="Create(int, Action{TElement})"/>을 한 이후 반환 할 지 여부
        /// <para>null을 받아오면, 풀의 크기가 모자랄경우 반환에 실패한다</para>
        /// <para>값을 넣더라도, <see cref="TInfinityStack.MaxElementCount"/>에 의해 반환에 실패 할 수도 있다</para>
        /// <para>기본값: 1</para>
        /// </param>
        /// <returns></returns>
        T Pop<T>(int? autoTryCreateElementsWhenPoolNotEnough = 0) where T : class, TKey;



        /// <summary>
        /// [비동기] <typeparamref name="TElement"/>를 꺼낸다
        /// <para>풀의 상태와 파라미터에 따라, <b>반환을 무조건 보장하지는 않으며</b>, 이때 null을 반환한다</para>
        /// </summary>
        /// <param name="autoTryCreateElementsWhenPoolNotEnough">
        /// 꺼내야 하는데, 풀의 크기가 모자랄 경우 (+풀이 존재 하지 않거나), <see cref="Create(int, Action{TElement})"/>을 한 이후 반환 할 지 여부
        /// <para>null을 받아오면, 풀의 크기가 모자랄경우 반환에 실패한다</para>
        /// <para>값을 넣더라도, <see cref="TInfinityStack.MaxElementCount"/>에 의해 반환에 실패 할 수도 있다</para>
        /// <para>기본값: 1</para>
        /// </param>
        /// <returns></returns>
        UniTask<T> PopAsync<T>(int? autoTryCreateElementsWhenPoolNotEnough = 1) where T : class, TKey;
    }



    ///======================================================================================================================================================



    //? 무한 스택 매니저 베이스



    /// <summary>
    /// <see cref="InfinityStackBase{TElement}"/> 을 기반으로 관리하는 매니저의 베이스
    /// <para>Key (<typeparamref name="TKey"/>) Value (<typeparamref name="TInfinityStack"/>) 를 딕셔너리로 관리한다</para>
    /// <para><see cref="InitialInitialize"/>를 사용해 최초 초기화 설정 권장</para>
    /// </summary>
    /// <typeparam name="TElement">풀링할 요소</typeparam>
    /// <typeparam name="TKey">풀링할 요소들을 나눌 단위의 키</typeparam>
    /// <typeparam name="TInfinityStack">무한 스택 타입</typeparam>
    [Serializable]
    [HideReferenceObjectPicker]
    public abstract class InfinityStackManagerBase<TElement, TKey, TInfinityStack>
        where TElement : class
        where TInfinityStack : InfinityStackBase<TElement>
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 
        /// </summary>
        /// <param name="poolInitialize_DebugDuplicate">풀 초기 생성 설정: 중복 디버깅 여부</param>
        /// <param name="poolInitialize_UseMaxElementCount">풀 초기 생성 설정: 최대 요소 개수 제약 사용 여부</param>
        /// <param name="poolDictionaryCapacity"><see cref="GetPoolDictionaryInternal"/>의 초기 용량 정하기</param>
        public InfinityStackManagerBase(bool poolInitialize_DebugDuplicate, int? poolInitialize_UseMaxElementCount, int poolDictionaryCapacity)
        {
            PoolInitialize_DebugDuplicate = poolInitialize_DebugDuplicate;
            PoolInitialize_UseMaxElementCount = poolInitialize_UseMaxElementCount;
            PoolDictionary = new Dictionary<TKey, TInfinityStack>(poolDictionaryCapacity);
        }



        /// <summary>
        /// <see cref="TKey"/>에 따라 <see cref="TInfinityStack"/>을 생성 하는 메서드
        ///  <para>재정의하여 각 타입과 성질에 맞게 무한스택을 생성하면됨</para>
        ///  <para>내부에서 Create 호출 금지</para>
        /// </summary>
        /// <param name="key">키</param>
        /// <param name="pool">초기화 하여 반환 할 풀 (무한스택)</param>
        protected abstract void InitializePool(TKey key, out TInfinityStack pool);



        /// <summary>
        /// <see cref="InitialInitialize"/>가 실행 되었는지 여부
        /// </summary>
        public bool IsInitialInitialize { get; private set; }



        /// <summary>
        /// 최초 초기화
        /// <para><see cref="InitialInitialize"/>가 딱 한번 실행된다</para>
        /// </summary>
        /// <returns></returns>
        public bool InitialInitialize()
        {
            if (IsInitialInitialize) return false;

            Initialize();
            IsInitialInitialize = true;

            return true;
        }



        /// <summary>
        /// 최초 초기화
        /// </summary>
        protected abstract void Initialize();



        ///======================================================================================================================================================



        //? 초기 풀 초기화 설정



        [HideInInspector]
        public readonly bool PoolInitialize_DebugDuplicate;
        [HideInInspector]
        public readonly int? PoolInitialize_UseMaxElementCount;



        ///======================================================================================================================================================



        //? 풀 딕셔너리



        /// <summary>
        /// <see cref="TKey"/>를 Key로, <see cref="TInfinityStack"/>을 Value로 저장하는 풀 딕셔너리
        /// </summary>
        protected readonly Dictionary<TKey, TInfinityStack> PoolDictionary;



        /// <summary>
        /// <see cref="TKey"/>를 Key로, <see cref="TInfinityStack"/>을 Value로 저장하는 풀 딕셔너리 를 내부에서 얻기
        /// </summary>
        protected Dictionary<TKey, TInfinityStack> GetPoolDictionaryInternal => PoolDictionary;



        /// <summary>
        /// <see cref="GetPoolDictionaryInternal"/> 읽기 전용으로 얻기
        /// </summary>
        public IReadOnlyDictionary<TKey, TInfinityStack> GetPoolDictionary => PoolDictionary;



#if UNITY_EDITOR



        ///======================================================================================================================================================



        //? 실제 풀 딕셔너리와 에디터 표시용 문자열 키 딕셔너리를 분리하여, 런타임 접근마다 전체 변환하지 않는다
        private bool editorPoolDictionaryCacheDirty = true;



        //[ShowInInspector, ShowIf(nameof(Editor_ValidPoolDictionary))]
        //[LabelText("풀 딕셔너리 보기")]
        //[Searchable]
        //[InlineProperty,HideReferenceObjectPicker]
        private Dictionary<string, TInfinityStack> editorCachedPoolDictionary;



        private void EditorRefresh_cachedPoolDictionary()
        {
            if (!editorPoolDictionaryCacheDirty) { return; }

            if (PoolDictionary == null)
            {
                editorCachedPoolDictionary = null;
                editorPoolDictionaryCacheDirty = false;
                return;
            }

            editorCachedPoolDictionary ??= new Dictionary<string, TInfinityStack>(PoolDictionary.Count);
            editorCachedPoolDictionary.Clear();


            foreach (var kv in PoolDictionary)
            {
                string key;

                switch (kv.Key)
                {
                    case IName kv_IName: key = kv_IName.CurrentName; break;
                    case Type kv_Type: key = kv_Type.Name; break;
                    default: key = kv.GetHashCode().ToString(); break;
                }

                editorCachedPoolDictionary.Add(key, kv.Value);
            }

            editorPoolDictionaryCacheDirty = false;
        }



        private bool Editor_ValidPoolDictionary => PoolDictionary != null;



        //? 에디터에 띄우기 위한 풀 딕셔너리, 콜렉션 변화를 제외한 수정 가능
        [ShowInInspector, ShowIf(nameof(Editor_ValidPoolDictionary))]
        [LabelText("풀 딕셔너리 보기")]
        [Searchable]
        [HideReferenceObjectPicker]
        [DictionaryDrawerSettings(IsReadOnly = true)]
        public Dictionary<string, TInfinityStack> EditorCached_GetPoolStackConvertedList
        {
            get
            {
                EditorRefresh_cachedPoolDictionary();
                return editorCachedPoolDictionary;
            }
            private set { } //! 에디터에서 요소를 수정 할 수 있기 위한 억지 setter
        }



        ///======================================================================================================================================================



#endif



        /// <summary>
        /// 새 풀을 등록하고 에디터 표시용 딕셔너리가 변경되었음을 기록합니다.
        /// </summary>
        protected void AddPoolInternal(TKey key, TInfinityStack pool)
        {
            PoolDictionary.Add(key, pool);
            MarkEditorPoolDictionaryCacheDirty();
        }



        /// <summary>
        /// 등록된 풀을 제거하고 에디터 표시용 딕셔너리가 변경되었음을 기록합니다.
        /// </summary>
        protected bool RemovePoolInternal(TKey key)
        {
            bool removed = PoolDictionary.Remove(key);
            if (removed) { MarkEditorPoolDictionaryCacheDirty(); }
            return removed;
        }



        /// <summary>
        /// 등록된 풀을 모두 제거하고 에디터 표시용 딕셔너리가 변경되었음을 기록합니다.
        /// </summary>
        protected void ClearPoolsInternal()
        {
            if (PoolDictionary.Count == 0) { return; }

            PoolDictionary.Clear();
            MarkEditorPoolDictionaryCacheDirty();
        }



        /// <summary>
        /// 에디터 표시용 풀 딕셔너리를 다음 조회 시점에 한 번만 갱신하도록 표시합니다.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void MarkEditorPoolDictionaryCacheDirty()
        {
#if UNITY_EDITOR
            editorPoolDictionaryCacheDirty = true;
#endif
        }




        ///======================================================================================================================================================



        //? 풀 딕셔너리 관련 이벤트



        /// <summary>
        /// 요소가 생성 된 이후에 추가로 수행할 이벤트 를 얻기
        /// <para><see cref="GetPoolDictionaryInternal"/>이나 <typeparamref name="TInfinityStack"/>를 수정하는 이벤트는 절대 사용하면 안됨</para>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected abstract Action<TElement> GetCreateEventPost(TKey key);



        ///======================================================================================================================================================



        //? 풀 딕셔너리 확인



        /// <summary>
        /// <paramref name="key">에 속한 <typeparamref name="TInfinityStack"/>의  <see cref="TInfinityStack.PoolCount"/> 를 구해보기
        /// </summary>
        public bool TryGetPoolCount(TKey key, out int value)
        {
            if (GetPoolDictionaryInternal.TryGetValue(key, out var result))
            {
                value = result.PoolCount;
                return true;
            }

            value = 0;
            return false;
        }



        /// <summary>
        /// <see cref="GetPoolDictionaryInternal"/> 의 크기 구하기
        /// </summary>
        public int GetPoolDictionaryCount => PoolDictionary?.Count ?? 0;



        ///======================================================================================================================================================



        //? 요소 생성 (Create)



        /// <summary>
        /// <typeparamref name="TElement"/>를 <paramref name="createCount"/> 만큼 생성한다
        /// <para>각 풀의 <see cref="TInfinityStack.MaxElementCount"/> 를 넘으면 반드시 생성을 보장하지 않으며,</para>
        /// <para>그렇게 <typeparamref name="TElement"/>를 생성된 개수를 반환한다 (int), 하나도 생성하지 못하면 0을 반환한다</para>
        /// <para>내부에서 <see cref="TInfinityStack.CreateInternal(int, Action{TElement})"/>을 재정의하여 그 곳에서 각 <typeparamref name="TElement"/>에 맞게 생성한다</para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="createCount"><typeparamref name="TElement"/>를 생성할 개수</param>
        /// <returns></returns>
        public int Create(TKey key, int createCount)
        {
            int result;

            //! 풀딕셔너리에 해당 Key가 이미 존재한다면, Value인 무한 스택에서 Create를 실행
            if (GetPoolDictionaryInternal.TryGetValue(key, out var pool))
            {
                result = pool.Create(createCount, GetCreateEventPost(key));
            }

            //! 풀딕셔너리에 해당 Key가 존재하지 않는다면, Pool을 새로 생성 한 후에 Value인 무한 스택에서 Create를 실행
            else
            {
                InitializePool(key, out pool);
                result = pool.Create(createCount, GetCreateEventPost(key));
                AddPoolInternal(key, pool);
            }

            return result;
        }



        /// <summary>
        /// [비동기] <typeparamref name="TElement"/>를 <paramref name="createCount"/> 만큼 생성한다
        /// <para>각 풀의 <see cref="TInfinityStack.MaxElementCount"/> 를 넘으면 반드시 생성을 보장하지 않으며,</para>
        /// <para>그렇게 <typeparamref name="TElement"/>를 생성된 개수를 반환한다 (int), 하나도 생성하지 못하면 0을 반환한다</para>
        /// <para>내부에서 <see cref="TInfinityStack.CreateInternal(int, Action{TElement})"/>을 재정의하여 그 곳에서 각 <typeparamref name="TElement"/>에 맞게 생성한다</para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="createCount"><typeparamref name="TElement"/>를 생성할 개수</param>
        /// <returns></returns>
        public async UniTask<int> CreateAsync(TKey key, int createCount)
        {
            int result;

            //! 풀딕셔너리에 해당 Key가 이미 존재한다면, Value인 무한 스택에서 Create를 실행
            if (GetPoolDictionaryInternal.TryGetValue(key, out var pool))
            {
                result = await pool.CreateAsync(createCount, GetCreateEventPost(key));
            }

            //! 풀딕셔너리에 해당 Key가 존재하지 않는다면, Pool을 새로 생성 한 후에 Value인 무한 스택에서 Create를 실행
            else
            {
                InitializePool(key, out pool);
                result = await pool.CreateAsync(createCount, GetCreateEventPost(key));
                AddPoolInternal(key, pool);
            }

            return result;
        }



        ///======================================================================================================================================================



        //? 요소 넣기 (Push)



        /// <summary>
        /// <typeparamref name="TElement"/>를 넣는다 (반환한다)
        /// <para>각 풀의 <see cref="TInfinityStack.inPool"/>에서 중복 검사를 하여, 이미 추가된 <typeparamref name="TElement"/> 일 경우 false를 반환한다</para>
        /// <para>디버그 모드에 따라 이를 메세지로 출력 하기도 한다</para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="element">넣을(반환할) 요소</param>
        public bool Push(TKey key, TElement element)
        {
            bool result;

            //! 풀딕셔너리에 해당 Key가 이미 존재한다면, Value인 무한 스택에서 Push를 실행
            if (GetPoolDictionaryInternal.TryGetValue(key, out var pool))
            {
                result = pool.Push(element);
            }

            //! 풀딕셔너리에 해당 Key가 존재하지 않는다면, Pool을 새로 생성 한 후에 Value인 무한 스택에서 Push를 실행
            else
            {
                InitializePool(key, out pool);
                result = pool.Push(element);
                AddPoolInternal(key, pool);
            }

            return result;
        }



        /// <summary>
        /// <typeparamref name="TElement"/>들을 "배열"로 넣는다 (반환한다)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="elements">넣을(반환할) 요소 들의 배열</param>
        public void PushRange(TKey key, params TElement[] elements)
        {
            //! 풀딕셔너리에 해당 Key가 이미 존재한다면, Value인 무한 스택에서 PushRange를 실행
            if (GetPoolDictionaryInternal.TryGetValue(key, out var pool))
            {
                pool.PushRange(elements);
            }

            //! 풀딕셔너리에 해당 Key가 존재하지 않는다면, Pool을 새로 생성 한 후에 Value인 무한 스택에서 PushRange를 실행
            else
            {
                InitializePool(key, out pool);
                pool.PushRange(elements);
                AddPoolInternal(key, pool);
            }
        }



        /// <summary>
        /// <typeparamref name="TElement"/>들을 "리스트"로 넣는다 (반환한다)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="elements">넣을(반환할) 요소 들의 배열</param>
        public void PushRange(TKey key, IList<TElement> elements)
        {
            //! 풀딕셔너리에 해당 Key가 이미 존재한다면, Value인 무한 스택에서 PushRange를 실행
            if (GetPoolDictionaryInternal.TryGetValue(key, out var pool))
            {
                pool.PushRange(elements);
            }

            //! 풀딕셔너리에 해당 Key가 존재하지 않는다면, Pool을 새로 생성 한 후에 Value인 무한 스택에서 PushRange를 실행
            else
            {
                InitializePool(key, out pool);
                pool.PushRange(elements);
                AddPoolInternal(key, pool);
            }
        }



        /// <summary>
        /// <typeparamref name="TElement"/>들을 "IEnumberable"로 넣는다 (반환한다)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="elements">넣을(반환할) 요소 들의 배열</param>
        public void PushRange(TKey key, IEnumerable<TElement> elements)
        {
            //! 풀딕셔너리에 해당 Key가 이미 존재한다면, Value인 무한 스택에서 PushRange를 실행
            if (GetPoolDictionaryInternal.TryGetValue(key, out var pool))
            {
                pool.PushRange(elements);
            }

            //! 풀딕셔너리에 해당 Key가 존재하지 않는다면, Pool을 새로 생성 한 후에 Value인 무한 스택에서 PushRange를 실행
            else
            {
                InitializePool(key, out pool);
                pool.PushRange(elements);
                AddPoolInternal(key, pool);
            }
        }



        ///======================================================================================================================================================



        //? 요소 해제하기 (Release)



        /// <summary>
        /// <see cref="TInfinityStack.poolStack"/>에 생성한 <typeparamref name="TElement"/>들을 해제(제거)하며
        /// <para>해제에 성공한 개수를 반환한다</para>
        /// <para>정말 더이상 사용 하지 않을때, 메모리를 확보 하기 위해 사용한다</para>
        /// <para><see cref="GetPoolDictionaryInternal"/>에 <paramref name="key"/>가 존재하지 않으면 0을 반환한다</para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="releaseCount">해제할 <typeparamref name="TElement"/>의 개수</param>
        public int Release(TKey key, int releaseCount)
        {
            int result = 0;

            //! 풀딕셔너리에 해당 Key가 이미 존재한다면, Value인 무한 스택에서 Release를 실행
            if (GetPoolDictionaryInternal.TryGetValue(key, out var pool))
            {
                result = pool.Release(releaseCount);
            }

            //. Key가 존재하지 않는다면 기본값인 0이 반환된다
            return result;
        }



        /// <summary>
        /// <see cref="TInfinityStack.poolStack"/>에 생성한 모든<typeparamref name="TElement"/>들을 해제(제거)한다
        /// <para>정말 더이상 사용 하지 않을때, 메모리를 확보 하기 위해 사용한다</para>
        /// <para><paramref name="removePoolInPoolDictionary"/>를 사용 할 경우, 해당 <typeparamref name="TInfinityStack"/> 자체를 해제하여 레퍼런스 카운트를 0 으로 만든다</para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="removePoolInPoolDictionary"><typeparamref name="TInfinityStack"/> 를 딕셔너리에서 제거 할 지 여부</param>
        public void ReleaseClear(TKey key, bool removePoolInPoolDictionary)
        {
            //! 풀딕셔너리에 해당 Key가 이미 존재한다면, Value인 무한 스택에서 ReleaseClear를 실행
            if (GetPoolDictionaryInternal.TryGetValue(key, out var pool))
            {
                pool.ReleaseClear();

                if (removePoolInPoolDictionary)
                {
                    pool.Dispose();
                    RemovePoolInternal(key);
                }
            }
        }



        /// <summary>
        /// <see cref="GetPoolDictionaryInternal"/>의 모든 <see cref="TInfinityStack.poolStack"/>에 생성한 모든<typeparamref name="TElement"/>들을 해제(제거)한다
        /// <para>정말 더이상 사용 하지 않을때, 메모리를 확보 하기 위해 사용한다</para>
        /// <para><paramref name="clearPoolInPoolDictionary"/>를 사용 할 경우, 해당 <typeparamref name="TInfinityStack"/> 자체를 해제하여 레퍼런스 카운트를 0 으로 만든다</para>
        /// <para><see cref="GetPoolDictionaryInternal"/>를 순회하면서 실행된다</para>
        /// </summary>
        /// <param name="clearPoolInPoolDictionary">모든 <typeparamref name="TInfinityStack"/> 를 딕셔너리에서 제거 할 지 여부</param>
        public void ReleaseClearAll(bool clearPoolInPoolDictionary)
        {
            foreach (var item in GetPoolDictionaryInternal)
            {
                item.Value.ReleaseClear();
                if (clearPoolInPoolDictionary) { item.Value.Dispose(); }
            }

            if (clearPoolInPoolDictionary) { ClearPoolsInternal(); }
        }



        ///======================================================================================================================================================



        //? 요소 꺼내기 (Pop)



        /// <summary>
        /// <typeparamref name="TElement"/>를 꺼낸다
        /// <para>풀의 상태와 파라미터에 따라, <b>반환을 무조건 보장하지는 않으며</b>, 이때 null을 반환한다</para>
        /// </summary>
        /// <param name="autoTryCreateElementsWhenPoolNotEnough">
        /// 꺼내야 하는데, 풀의 크기가 모자랄 경우 (+풀이 존재 하지 않거나), <see cref="Create(int, Action{TElement})"/>을 한 이후 반환 할 지 여부
        /// <para>null을 받아오면, 풀의 크기가 모자랄경우 반환에 실패한다</para>
        /// <para>값을 넣더라도, <see cref="TInfinityStack.MaxElementCount"/>에 의해 반환에 실패 할 수도 있다</para>
        /// <para>기본값: 1</para>
        /// </param>
        /// <returns></returns>
        public TElement Pop(TKey key, int? autoTryCreateElementsWhenPoolNotEnough = 1)
        {
            //! 풀딕셔너리에 해당 Key가 이미 존재한다면, Value인 무한 스택에서 Pop를 실행
            if (GetPoolDictionaryInternal.TryGetValue(key, out var pool))
            {
                return pool.Pop(autoTryCreateElementsWhenPoolNotEnough, GetCreateEventPost(key));
            }

            //! 풀딕셔너리에 해당 Key가 존재하지 않고,
            //! 풀이 모자라거나 존재하지 않을때 생성을 원하는 상태일 경우
            //! Pool을 새로 생성 한 후에 Value인 무한 스택에서 Pop를 실행
            else if (autoTryCreateElementsWhenPoolNotEnough.HasValue && autoTryCreateElementsWhenPoolNotEnough.Value > 0)
            {
                //. 핵심은 autoTryCreateElementsWhenPoolNotEnough를 해당 풀이 아닌 이곳에서 처리하는것
                //. 물론 풀이 애초에 존재 하지 않고 조건이 맞을 때만 이 코드가 실행된다
                InitializePool(key, out pool);
                pool.Create(autoTryCreateElementsWhenPoolNotEnough.Value, GetCreateEventPost(key));
                AddPoolInternal(key, pool);
                return pool.Pop(1); //. pool.Create에서 이미 원한는 만큼 미리 생성했기에, 여기서는 1개만 보장되게한다
            }

            return null;
        }



        /// <summary>
        /// [비동기] <typeparamref name="TElement"/>를 꺼낸다
        /// <para>풀의 상태와 파라미터에 따라, <b>반환을 무조건 보장하지는 않으며</b>, 이때 null을 반환한다</para>
        /// </summary>
        /// <param name="autoTryCreateElementsWhenPoolNotEnough">
        /// 꺼내야 하는데, 풀의 크기가 모자랄 경우 (+풀이 존재 하지 않거나), <see cref="Create(int, Action{TElement})"/>을 한 이후 반환 할 지 여부
        /// <para>null을 받아오면, 풀의 크기가 모자랄경우 반환에 실패한다</para>
        /// <para>값을 넣더라도, <see cref="TInfinityStack.MaxElementCount"/>에 의해 반환에 실패 할 수도 있다</para>
        /// <para>기본값: 1</para>
        /// </param>
        /// <returns></returns>
        public async UniTask<TElement> PopAsync(TKey key, int? autoTryCreateElementsWhenPoolNotEnough = 1)
        {
            //! 풀딕셔너리에 해당 Key가 이미 존재한다면, Value인 무한 스택에서 Pop를 실행
            if (GetPoolDictionaryInternal.TryGetValue(key, out var pool))
            {
                return await pool.PopAsync(autoTryCreateElementsWhenPoolNotEnough, GetCreateEventPost(key));
            }

            //! 풀딕셔너리에 해당 Key가 존재하지 않고,
            //! 풀이 모자라거나 존재하지 않을때 생성을 원하는 상태일 경우
            //! Pool을 새로 생성 한 후에 Value인 무한 스택에서 Pop를 실행
            else if (autoTryCreateElementsWhenPoolNotEnough.HasValue && autoTryCreateElementsWhenPoolNotEnough.Value > 0)
            {
                //. 핵심은 autoTryCreateElementsWhenPoolNotEnough를 해당 풀이 아닌 이곳에서 처리하는것
                //. 물론 풀이 애초에 존재 하지 않고 조건이 맞을 때만 이 코드가 실행된다
                InitializePool(key, out pool);
                await pool.CreateAsync(autoTryCreateElementsWhenPoolNotEnough.Value, GetCreateEventPost(key));
                AddPoolInternal(key, pool);
                return await pool.PopAsync(1); //. pool.Create에서 이미 원한는 만큼 미리 생성했기에, 여기서는 1개만 보장되게한다
            }

            return null;
        }



        ///======================================================================================================================================================



        //? 풀 딕셔너리 검사



        ///<summary>
        /// 풀 딕셔너리를 검사하여, 중복된 타입이 존재 하는지 검사한다
        /// </summary>
        public void CheckPoolDictionary_IsDuplicate()
        {
            foreach (var stack in GetPoolDictionary)
            {
                var duplicates = stack.Value.GetPoolStack.GroupBy(x => x?.GetType())
                                  .Where(g => g.Count() > 1)
                                  .Select(g => g.Key?.Name)
                                  .ToArray();

                if (duplicates.Length > 0)
                {
                    throw new InvalidOperationException(
                        $"중복된 타입이 존재합니다: {string.Join(", ", duplicates)}");
                }
            }
        }



        ///======================================================================================================================================================



        //? Dispose 패턴



        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            DisposeInternal();
            _disposed = true;
        }


        protected virtual void DisposeInternal()
        {
            ReleaseClearAll(true);
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// <see cref="InfinityStackBase{TElement}"/> 을 기반으로 관리하는 매니저의 베이스
    /// <para>Key (<typeparamref name="TKey"/>) Value (<typeparamref name="TInfinityStack"/>) 를 딕셔너리로 관리한다</para>
    /// <para>각 <typeparamref name="TKey"/> 별로 <typeparamref name="TElement"/>가 각각 다르게 선언 되어 있을때 사용한다</para>
    /// <para>따라서 각각 <typeparamref name="TKey"/>와 <typeparamref name="TElement"/>가 매핑된 딕셔너리를 사용한다</para>
    /// <para><b>Key별로 고유한 프로토타입(데이터·Prefab·설정)이 필요 할 때 사용한다!</b></para>
    /// </summary>
    /// <typeparam name="TElement">풀링할 요소</typeparam>
    /// <typeparam name="TKey">풀링할 요소들을 나눌 단위의 키</typeparam>
    /// <typeparam name="TInfinityStack">무한 스택 타입</typeparam>
    [Serializable]
    [HideReferenceObjectPicker]
    public abstract class InfinityStackManagerBase_PreKey<TElement, TKey, TInfinityStack> : InfinityStackManagerBase<TElement, TKey, TInfinityStack>
        where TElement : class
        where TInfinityStack : InfinityStackBase<TElement>
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 사용할때, 각 <typeparamref name="TElement"/>들을 별도로 생성 하여 받아와야한다
        /// </summary>
        /// <param name="elements">요소들 배열, 이 요소들로 매핑 딕셔너리를 초기화한다</param>
        /// <param name="createEventPost">요소가 생성 된 이후에 추가로 수행할 이벤트</param>
        protected InfinityStackManagerBase_PreKey(IEnumerable<TElement> elements, Action<TElement> createEventPost = null, bool poolInitialize_DebugDuplicate = true, int? poolInitialize_UseMaxElementCount = null)
            : this(ZLinqEnumerableCache.Materialize(elements), createEventPost, poolInitialize_DebugDuplicate, poolInitialize_UseMaxElementCount)
        {
        }



        private InfinityStackManagerBase_PreKey(TElement[] elements, Action<TElement> createEventPost, bool poolInitialize_DebugDuplicate, int? poolInitialize_UseMaxElementCount)
            : base(poolInitialize_DebugDuplicate, poolInitialize_UseMaxElementCount, elements.Length)
        {
            CreateEventPost = createEventPost; //. 생성 이벤트 등록


            //? 매핑 딕셔너리를 초기화한다
            InitializeKeyElementMaps(elements, out KeyElementMaps);


            //? 초기화된 매핑 딕셔너리를 순회하면서, 각각의 풀을 순회한다
            foreach (var item in KeyElementMaps)
            {
                //. 무한스택(풀) 을 생성한다
                InitializePool(item.Key, out var pool);

                //. 생성한 무한스택을 풀 딕셔너리에 추가한다
                AddPoolInternal(item.Key, pool);
            }
        }



        /// <summary>
        /// 매핑 딕셔너리를 제작하는 메서드
        ///  <para>이 메서드를 재정의하여 각 요소에 맞춰 딕셔너리를 초기화 하면 됨</para>
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="keyElementsMap"></param>
        protected abstract void InitializeKeyElementMaps(IEnumerable<TElement> elements, out Dictionary<TKey, TElement> keyElementsMap);



        ///======================================================================================================================================================



        //? 키-요소 매핑 딕셔너리



        /// <summary>
        /// <see cref="TKey"/> 를 Key로, <see cref="TElement"/>를 Value가 저장 되어있는 매핑 딕셔너리
        /// </summary>
        protected readonly Dictionary<TKey, TElement> KeyElementMaps;



        ///======================================================================================================================================================



        //? 풀 딕셔너리 관련 이벤트



        /// <summary>
        /// 요소가 생성 된 이후에 추가로 수행할 이벤트
        /// <para><see cref="InfinityStackManagerBase.PoolDictionary"/>이나 <typeparamref name="TInfinityStack"/>를 수정하는 이벤트는 절대 사용하면 안됨</para>
        /// </summary>
        public event Action<TElement> CreateEventPost;



        /// <inheritdoc/>
        protected override Action<TElement> GetCreateEventPost(TKey key) => CreateEventPost;



        ///======================================================================================================================================================



        //? Dispose 패턴



        protected override void DisposeInternal()
        {
            KeyElementMaps.Clear();
            CreateEventPost = null;

            base.DisposeInternal();
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// <b>Key로 <see cref="Type"/>을 사용하는</b> <see cref="InfinityStackBase{TElement}"/> 을 기반으로 관리하는 매니저의 베이스
    /// <para>Key (<typeparamref name="TKey"/>) Value (<typeparamref name="TInfinityStack"/>) 를 딕셔너리로 관리한다</para>
    /// <para>각 <typeparamref name="TKey"/> 별로 <typeparamref name="TElement"/>가 각각 다르게 선언 되어 있을때 사용한다</para>
    /// <para>따라서 각각 <typeparamref name="TKey"/>와 <typeparamref name="TElement"/>가 매핑된 딕셔너리를 사용한다</para>
    /// <para><b>Key별로 고유한 프로토타입(데이터·Prefab·설정)이 필요 할 때 사용한다!</b></para>
    /// </summary>
    /// <typeparam name="TElement">풀링할 요소</typeparam>
    /// <typeparam name="TKey">풀링할 요소들을 나눌 단위의 키</typeparam>
    /// <typeparam name="TInfinityStack">무한 스택 타입</typeparam>
    [Serializable]
    [HideReferenceObjectPicker]
    public abstract class InfinityStackManagerBase_PreClassKey<TElement, TInfinityStack> : InfinityStackManagerBase_PreKey<TElement, Type, TInfinityStack>, IInfinityStackManagerBase_ClassKey_PopGeneric<TElement, TElement>
        where TElement : class
        where TInfinityStack : InfinityStackBase<TElement>
    {
        ///======================================================================================================================================================



        ///<inheritdoc/>
        protected InfinityStackManagerBase_PreClassKey(TElement[] elements, Action<TElement> createEventPost = null, bool poolInitialize_DebugDuplicate = true, int? poolInitialize_UseMaxElementCount = null) : base(elements, createEventPost, poolInitialize_DebugDuplicate, poolInitialize_UseMaxElementCount) { }


        ///======================================================================================================================================================



        //? 풀 딕셔너리 확인



        /// <summary>
        /// <paramref name="key">에 속한 <typeparamref name="TInfinityStack"/>의  <see cref="TInfinityStack.PoolCount"/> 를 구해보기
        /// </summary>
        public bool TryGetPoolCount<T>(out int value) where T : TElement => TryGetPoolCount(typeof(T), out value);



        ///======================================================================================================================================================



        //? 요소 생성 (Create)



        /// <summary>
        /// <typeparamref name="TKey"/>를 <paramref name="createCount"/> 만큼 생성한다
        /// <para>각 풀의 <see cref="TInfinityStack.MaxElementCount"/> 를 넘으면 반드시 생성을 보장하지 않으며,</para>
        /// <para>그렇게 <typeparamref name="TKey"/>를 생성된 개수를 반환한다 (int), 하나도 생성하지 못하면 0을 반환한다</para>
        /// <para>내부에서 <see cref="TInfinityStack.CreateInternal(int, Action{TKey})"/>을 재정의하여 그 곳에서 각 <typeparamref name="TKey"/>에 맞게 생성한다</para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="createCount"><typeparamref name="TKey"/>를 생성할 개수</param>
        /// <returns></returns>
        public int Create<T>(int createCount) where T : TElement => Create(typeof(T), createCount);



        /// <summary>
        /// [비동기] <typeparamref name="TKey"/>를 <paramref name="createCount"/> 만큼 생성한다
        /// <para>각 풀의 <see cref="TInfinityStack.MaxElementCount"/> 를 넘으면 반드시 생성을 보장하지 않으며,</para>
        /// <para>그렇게 <typeparamref name="TKey"/>를 생성된 개수를 반환한다 (int), 하나도 생성하지 못하면 0을 반환한다</para>
        /// <para>내부에서 <see cref="TInfinityStack.CreateInternal(int, Action{TKey})"/>을 재정의하여 그 곳에서 각 <typeparamref name="TKey"/>에 맞게 생성한다</para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="createCount"><typeparamref name="TKey"/>를 생성할 개수</param>
        /// <returns></returns>
        public async UniTask<int> CreateAsync<T>(int createCount) where T : TElement => await CreateAsync(typeof(T), createCount);



        ///======================================================================================================================================================



        //? 요소 넣기 (Push)



        /// <summary>
        /// <typeparamref name="TKey"/>를 넣는다 (반환한다)
        /// <para>각 풀의 <see cref="TInfinityStack.inPool"/>에서 중복 검사를 하여, 이미 추가된 <typeparamref name="TKey"/> 일 경우 false를 반환한다</para>
        /// <para>디버그 모드에 따라 이를 메세지로 출력 하기도 한다</para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="element">넣을(반환할) 요소</param>
        public void Push<T>(TElement value) where T : TElement => Push(typeof(T), value);




        /// <summary>
        /// <typeparamref name="TElement"/>를 넣는다 (반환한다)
        /// <para>각 풀의 <see cref="TInfinityStack.inPool"/>에서 중복 검사를 하여, 이미 추가된 <typeparamref name="TElement"/> 일 경우 false를 반환한다</para>
        /// <para>디버그 모드에 따라 이를 메세지로 출력 하기도 한다</para>
        /// </summary>
        /// <param name="element">넣을(반환할) 요소</param>
        public bool Push(TElement element) => Push(element.GetType(), element);




        /// <summary>
        /// <typeparamref name="TKey"/>들을 "배열"로 넣는다 (반환한다)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="elements">넣을(반환할) 요소 들의 배열</param>
        public void PushRange<T>(params TElement[] elements) where T : TElement => PushRange(typeof(T), elements);



        /// <summary>
        /// <typeparamref name="TKey"/>들을 "리스트"로 넣는다 (반환한다)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="elements">넣을(반환할) 요소 들의 배열</param>
        public void PushRange<T>(IList<TElement> elements) where T : TElement => PushRange(typeof(T), elements);



        /// <summary>
        /// <typeparamref name="TKey"/>들을 "IEnumberable"로 넣는다 (반환한다)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="elements">넣을(반환할) 요소 들의 배열</param>
        public void PushRange<T>(IEnumerable<TElement> elements) where T : TElement => PushRange(typeof(T), elements);



        ///======================================================================================================================================================



        //? 요소 해제하기 (Release)



        /// <summary>
        /// <see cref="TInfinityStack.poolStack"/>에 생성한 <typeparamref name="TKey"/>들을 해제(제거)하며
        /// <para>해제에 성공한 개수를 반환한다</para>
        /// <para>정말 더이상 사용 하지 않을때, 메모리를 확보 하기 위해 사용한다</para>
        /// <para><see cref="PoolDictionary"/>에 <paramref name="key"/>가 존재하지 않으면 0을 반환한다</para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="releaseCount">해제할 <typeparamref name="TKey"/>의 개수</param>
        public int Release<T>(int count) where T : TElement => Release(typeof(T), count);



        /// <summary>
        /// <see cref="TInfinityStack.poolStack"/>에 생성한 모든<typeparamref name="TKey"/>들을 해제(제거)한다
        /// <para>정말 더이상 사용 하지 않을때, 메모리를 확보 하기 위해 사용한다</para>
        /// <para><paramref name="removePoolInPoolDictionary"/>를 사용 할 경우, 해당 <typeparamref name="TInfinityStack"/> 자체를 해제하여 레퍼런스 카운트를 0 으로 만든다</para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="removePoolInPoolDictionary"><typeparamref name="TInfinityStack"/> 를 딕셔너리에서 제거 할 지 여부</param>
        public void ReleaseClear<T>(bool removePoolInPoolDictionary) where T : TElement => ReleaseClear(typeof(T), removePoolInPoolDictionary);




        ///======================================================================================================================================================



        //? 요소 꺼내기 (Pop)



        /// <summary>
        /// <typeparamref name="TKey"/>를 꺼낸다
        /// <para>풀의 상태와 파라미터에 따라, <b>반환을 무조건 보장하지는 않으며</b>, 이때 null을 반환한다</para>
        /// </summary>
        /// <param name="autoTryCreateElementsWhenPoolNotEnough">
        /// 꺼내야 하는데, 풀의 크기가 모자랄 경우 (+풀이 존재 하지 않거나), <see cref="Create(int, Action{TKey})"/>을 한 이후 반환 할 지 여부
        /// <para>null을 받아오면, 풀의 크기가 모자랄경우 반환에 실패한다</para>
        /// <para>값을 넣더라도, <see cref="TInfinityStack.MaxElementCount"/>에 의해 반환에 실패 할 수도 있다</para>
        /// <para>기본값: 1</para>
        /// </param>
        /// <returns></returns>
        public T Pop<T>(int? autoTryCreateElementsWhenPoolNotEnough = 1) where T : class, TElement => Pop(typeof(T), autoTryCreateElementsWhenPoolNotEnough) as T;



        /// <summary>
        /// [비동기] <typeparamref name="TKey"/>를 꺼낸다
        /// <para>풀의 상태와 파라미터에 따라, <b>반환을 무조건 보장하지는 않으며</b>, 이때 null을 반환한다</para>
        /// </summary>
        /// <param name="autoTryCreateElementsWhenPoolNotEnough">
        /// 꺼내야 하는데, 풀의 크기가 모자랄 경우 (+풀이 존재 하지 않거나), <see cref="Create(int, Action{TKey})"/>을 한 이후 반환 할 지 여부
        /// <para>null을 받아오면, 풀의 크기가 모자랄경우 반환에 실패한다</para>
        /// <para>값을 넣더라도, <see cref="TInfinityStack.MaxElementCount"/>에 의해 반환에 실패 할 수도 있다</para>
        /// <para>기본값: 1</para>
        /// </param>
        /// <returns></returns>
        public async UniTask<T> PopAsync<T>(int? autoTryCreateElementsWhenPoolNotEnough = 1) where T : class, TElement => await PopAsync(typeof(T), autoTryCreateElementsWhenPoolNotEnough) as T;



        ///======================================================================================================================================================
    }



    /// <summary>
    /// <see cref="InfinityStackBase{TElement}"/> 을 기반으로 관리하는 매니저의 베이스
    /// <para><typeparamref name="TKey"/>들의 배열을 받아와 그것들을 기반으로 딕셔너리를 생성한다</para>
    /// <para>모든 <typeparamref name="TKey"/> 가 동일한 <typeparamref name="TElement"/>를 사용할때 사용한다</para>
    /// <para><typeparamref name="TElement"/>를 하나 할당 한 뒤, 그<typeparamref name="TElement"/>로 생성한다</para>
    /// <para><b>프로토타입은 하나고, Key는 단지 분류·소속·파티션용 일 때 사용한다!</b></para>
    /// </summary>
    /// <typeparam name="TElement">풀링할 요소</typeparam>
    /// <typeparam name="TKey">풀링할 요소들을 나눌 단위의 키</typeparam>
    /// <typeparam name="TInfinityStack">무한 스택 타입</typeparam>
    [Serializable]
    [HideReferenceObjectPicker]
    public abstract class InfinityStackManagerBase_Shared<TElement, TKey, TInfinityStack> : InfinityStackManagerBase<TElement, TKey, TInfinityStack>
        where TElement : class
        where TInfinityStack : InfinityStackBase<TElement>
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 사용할때, 각 <typeparamref name="TElement"/>들을 별도로 생성 하여 받아와야한다
        /// </summary>
        /// <param name="elementStandard">생성할 요소의 기준이 되는 객체</param>
        /// <param name="keys">
        /// 키 값 배열, 받아온 Key들 만큼 딕셔너리를 생성한다
        /// <para>null이나 크기가 0인 배열을 받아와도 무방</para>
        /// </param>
        /// <param name="createEventPost">요소가 생성 된 이후에 추가로 수행할 이벤트</param>
        public InfinityStackManagerBase_Shared(TElement elementStandard, IEnumerable<TKey> keys, Action<TKey, TElement> createEventPost = null, bool poolInitialize_DebugDuplicate = true, int? poolInitialize_UseMaxElementCount = null)
            : this(elementStandard, ZLinqEnumerableCache.Materialize(keys), createEventPost, poolInitialize_DebugDuplicate, poolInitialize_UseMaxElementCount)
        {
        }



        private InfinityStackManagerBase_Shared(TElement elementStandard, TKey[] keys, Action<TKey, TElement> createEventPost, bool poolInitialize_DebugDuplicate, int? poolInitialize_UseMaxElementCount)
            : base(poolInitialize_DebugDuplicate, poolInitialize_UseMaxElementCount, keys.Length)
        {
            ElementStandard = elementStandard; //. 기준이 되는 요소의 객체를 할당한다
            CreateEventPost = createEventPost; //. 생성 이벤트 등록


            //. 키 값 배열을 기반으로 풀 딕셔너리에 풀들을 각각 만들어 추가한다
            foreach (TKey key in keys)
            {
                InitializePool(key, out var pool);
                AddPoolInternal(key, pool);
            }
        }



        ///======================================================================================================================================================



        //? 기준이 되는 요소



        /// <summary>
        ///  기준이 되는 요소
        ///  <para>이 객체를 기준으로 객체들이 생성된다</para>
        /// </summary>
        protected readonly TElement ElementStandard;



        ///======================================================================================================================================================



        //? 풀 딕셔너리 관련 이벤트



        /// <summary>
        /// 요소가 생성 된 이후에 추가로 수행할 이벤트
        /// <para><see cref="InfinityStackManagerBase.PoolDictionary"/>이나 <typeparamref name="TInfinityStack"/>를 수정하는 이벤트는 절대 사용하면 안됨</para>
        /// </summary>
        public event Action<TKey, TElement> CreateEventPost;



        /// <summary>
        /// 캡처된 생성 이벤트들을 저장하는 딕셔너리
        /// <para><see cref="GetCreateEventPost(TKey)"/> 에서 Lazy 초기화 되어 사용</para>
        /// </summary>
        private Dictionary<TKey, Action<TElement>> cached_CreateEventPost;



        /// <inheritdoc/>
        protected override Action<TElement> GetCreateEventPost(TKey key)
        {
            if (cached_CreateEventPost != null && cached_CreateEventPost.TryGetValue(key, out var result)) return result;

            cached_CreateEventPost ??= new Dictionary<TKey, Action<TElement>>();

            result = element => CreateEventPost?.Invoke(key, element);
            cached_CreateEventPost[key] = result;
            return result;
        }



        ///======================================================================================================================================================



        //? Dispose 패턴



        protected override void DisposeInternal()
        {
            cached_CreateEventPost.Clear();
            cached_CreateEventPost = null;
            CreateEventPost = null;

            base.DisposeInternal();
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// <see cref="InfinityStackBase{TElement}"/> 을 기반으로 관리하는 매니저의 베이스
    /// <para><typeparamref name="TKey"/>들의 배열을 받아와 그것들을 기반으로 딕셔너리를 생성한다</para>
    /// <para>모든 <typeparamref name="TKey"/> 가 동일한 <typeparamref name="TElement"/>를 사용할때 사용한다</para>
    /// <para><typeparamref name="TElement"/>를 하나 할당 한 뒤, 그<typeparamref name="TElement"/>로 생성한다</para>
    /// <para><b>프로토타입은 하나고, Key는 단지 분류·소속·파티션용 일 때 사용한다!</b></para>
    /// <para><typeparamref name="TKey"/>로 클래스를 사용하는 버전</para>
    /// </summary>
    /// <typeparam name="TElement">풀링할 요소</typeparam>
    /// <typeparam name="TKey">풀링할 요소들을 나눌 단위의 키</typeparam>
    /// <typeparam name="TInfinityStack">무한 스택 타입</typeparam>
    [Serializable]
    [HideReferenceObjectPicker]
    public abstract class InfinityStackManagerBase_Shared_ClassKey<TElement, TKey, TInfinityStack> : InfinityStackManagerBase_Shared<TElement, TKey, TInfinityStack>, IInfinityStackManagerBase_ClassKey_PopElement<TKey, TElement>
        where TElement : class
        where TKey : class
        where TInfinityStack : InfinityStackBase<TElement>
    {
        ///======================================================================================================================================================



        ///<inheritdoc/>
        protected InfinityStackManagerBase_Shared_ClassKey(TElement elementStandard, IEnumerable<TKey> keys, Action<TKey, TElement> createEventPost = null, bool poolInitialize_DebugDuplicate = true, int? poolInitialize_UseMaxElementCount = null)
            : this(elementStandard, ZLinqEnumerableCache.Materialize(keys), createEventPost, poolInitialize_DebugDuplicate, poolInitialize_UseMaxElementCount)
        {
        }



        private InfinityStackManagerBase_Shared_ClassKey(TElement elementStandard, TKey[] keys, Action<TKey, TElement> createEventPost, bool poolInitialize_DebugDuplicate, int? poolInitialize_UseMaxElementCount)
            : base(elementStandard, keys, createEventPost, poolInitialize_DebugDuplicate, poolInitialize_UseMaxElementCount)
        {
            KeyTypeDictionary = new Dictionary<Type, TKey>(keys.Length);

            foreach (TKey key in keys)
            {
                KeyTypeDictionary.Add(key.GetType(), key);
            }
        }



        ///======================================================================================================================================================



        ///<summary>
        ///<see cref="TKey"/>를 기반으로 만캐싱된 타입 딕셔너리
        /// </summary>
        protected readonly Dictionary<Type, TKey> KeyTypeDictionary;



        ///======================================================================================================================================================



        //? 풀 딕셔너리 확인



        /// <summary>
        /// <paramref name="key">에 속한 <typeparamref name="TInfinityStack"/>의  <see cref="TInfinityStack.PoolCount"/> 를 구해보기
        /// </summary>
        public bool TryGetPoolCount<T>(out int value) where T : TKey => TryGetPoolCount(KeyTypeDictionary[typeof(T)], out value);



        ///======================================================================================================================================================



        //? 요소 생성 (Create)



        /// <summary>
        /// <typeparamref name="TKey"/>를 <paramref name="createCount"/> 만큼 생성한다
        /// <para>각 풀의 <see cref="TInfinityStack.MaxElementCount"/> 를 넘으면 반드시 생성을 보장하지 않으며,</para>
        /// <para>그렇게 <typeparamref name="TKey"/>를 생성된 개수를 반환한다 (int), 하나도 생성하지 못하면 0을 반환한다</para>
        /// <para>내부에서 <see cref="TInfinityStack.CreateInternal(int, Action{TKey})"/>을 재정의하여 그 곳에서 각 <typeparamref name="TKey"/>에 맞게 생성한다</para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="createCount"><typeparamref name="TKey"/>를 생성할 개수</param>
        /// <returns></returns>
        public int Create<T>(int createCount) where T : TKey => Create(KeyTypeDictionary[typeof(T)], createCount);



        /// <summary>
        /// [비동기] <typeparamref name="TKey"/>를 <paramref name="createCount"/> 만큼 생성한다
        /// <para>각 풀의 <see cref="TInfinityStack.MaxElementCount"/> 를 넘으면 반드시 생성을 보장하지 않으며,</para>
        /// <para>그렇게 <typeparamref name="TKey"/>를 생성된 개수를 반환한다 (int), 하나도 생성하지 못하면 0을 반환한다</para>
        /// <para>내부에서 <see cref="TInfinityStack.CreateInternal(int, Action{TKey})"/>을 재정의하여 그 곳에서 각 <typeparamref name="TKey"/>에 맞게 생성한다</para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="createCount"><typeparamref name="TKey"/>를 생성할 개수</param>
        /// <returns></returns>
        public async UniTask<int> CreateAsync<T>(int createCount) where T : TKey => await CreateAsync(KeyTypeDictionary[typeof(T)], createCount);



        ///======================================================================================================================================================



        //? 요소 넣기 (Push)



        /// <summary>
        /// <typeparamref name="TKey"/>를 넣는다 (반환한다)
        /// <para>각 풀의 <see cref="TInfinityStack.inPool"/>에서 중복 검사를 하여, 이미 추가된 <typeparamref name="TKey"/> 일 경우 false를 반환한다</para>
        /// <para>디버그 모드에 따라 이를 메세지로 출력 하기도 한다</para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="element">넣을(반환할) 요소</param>
        public void Push<T>(TElement value) where T : TKey => Push(KeyTypeDictionary[typeof(T)], value);



        /// <summary>
        /// <typeparamref name="TKey"/>들을 "배열"로 넣는다 (반환한다)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="elements">넣을(반환할) 요소 들의 배열</param>
        public void PushRange<T>(params TElement[] elements) where T : TKey => PushRange(KeyTypeDictionary[typeof(T)], elements);



        /// <summary>
        /// <typeparamref name="TKey"/>들을 "리스트"로 넣는다 (반환한다)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="elements">넣을(반환할) 요소 들의 배열</param>
        public void PushRange<T>(IList<TElement> elements) where T : TKey => PushRange(KeyTypeDictionary[typeof(T)], elements);



        /// <summary>
        /// <typeparamref name="TKey"/>들을 "IEnumberable"로 넣는다 (반환한다)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="elements">넣을(반환할) 요소 들의 배열</param>
        public void PushRange<T>(IEnumerable<TElement> elements) where T : TKey => PushRange(KeyTypeDictionary[typeof(T)], elements);



        ///======================================================================================================================================================



        //? 요소 해제하기 (Release)



        /// <summary>
        /// <see cref="TInfinityStack.poolStack"/>에 생성한 <typeparamref name="TKey"/>들을 해제(제거)하며
        /// <para>해제에 성공한 개수를 반환한다</para>
        /// <para>정말 더이상 사용 하지 않을때, 메모리를 확보 하기 위해 사용한다</para>
        /// <para><see cref="PoolDictionary"/>에 <paramref name="key"/>가 존재하지 않으면 0을 반환한다</para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="releaseCount">해제할 <typeparamref name="TKey"/>의 개수</param>
        public int Release<T>(int count) where T : TKey => Release(KeyTypeDictionary[typeof(T)], count);



        /// <summary>
        /// <see cref="TInfinityStack.poolStack"/>에 생성한 모든<typeparamref name="TKey"/>들을 해제(제거)한다
        /// <para>정말 더이상 사용 하지 않을때, 메모리를 확보 하기 위해 사용한다</para>
        /// <para><paramref name="removePoolInPoolDictionary"/>를 사용 할 경우, 해당 <typeparamref name="TInfinityStack"/> 자체를 해제하여 레퍼런스 카운트를 0 으로 만든다</para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="removePoolInPoolDictionary"><typeparamref name="TInfinityStack"/> 를 딕셔너리에서 제거 할 지 여부</param>
        public void ReleaseClear<T>(bool removePoolInPoolDictionary) where T : TKey => ReleaseClear(KeyTypeDictionary[typeof(T)], removePoolInPoolDictionary);




        ///======================================================================================================================================================



        //? 요소 꺼내기 (Pop)



        /// <summary>
        /// <typeparamref name="TKey"/>를 꺼낸다
        /// <para>풀의 상태와 파라미터에 따라, <b>반환을 무조건 보장하지는 않으며</b>, 이때 null을 반환한다</para>
        /// </summary>
        /// <param name="autoTryCreateElementsWhenPoolNotEnough">
        /// 꺼내야 하는데, 풀의 크기가 모자랄 경우 (+풀이 존재 하지 않거나), <see cref="Create(int, Action{TKey})"/>을 한 이후 반환 할 지 여부
        /// <para>null을 받아오면, 풀의 크기가 모자랄경우 반환에 실패한다</para>
        /// <para>값을 넣더라도, <see cref="TInfinityStack.MaxElementCount"/>에 의해 반환에 실패 할 수도 있다</para>
        /// <para>기본값: 1</para>
        /// </param>
        /// <returns></returns>
        public TElement Pop<T>(int? autoTryCreateElementsWhenPoolNotEnough = 1) where T : class, TKey => Pop(KeyTypeDictionary[typeof(T)], autoTryCreateElementsWhenPoolNotEnough);



        /// <summary>
        /// [비동기] <typeparamref name="TKey"/>를 꺼낸다
        /// <para>풀의 상태와 파라미터에 따라, <b>반환을 무조건 보장하지는 않으며</b>, 이때 null을 반환한다</para>
        /// </summary>
        /// <param name="autoTryCreateElementsWhenPoolNotEnough">
        /// 꺼내야 하는데, 풀의 크기가 모자랄 경우 (+풀이 존재 하지 않거나), <see cref="Create(int, Action{TKey})"/>을 한 이후 반환 할 지 여부
        /// <para>null을 받아오면, 풀의 크기가 모자랄경우 반환에 실패한다</para>
        /// <para>값을 넣더라도, <see cref="TInfinityStack.MaxElementCount"/>에 의해 반환에 실패 할 수도 있다</para>
        /// <para>기본값: 1</para>
        /// </param>
        /// <returns></returns>
        public async UniTask<TElement> PopAsync<T>(int? autoTryCreateElementsWhenPoolNotEnough = 1) where T : class, TKey => await PopAsync(KeyTypeDictionary[typeof(T)], autoTryCreateElementsWhenPoolNotEnough);



        ///======================================================================================================================================================



        //? Dispose 패턴



        protected override void DisposeInternal()
        {
            KeyTypeDictionary.Clear();

            base.DisposeInternal();
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================



    /// <summary>
    /// <see cref="IName"/>,<see cref="ICreateCount"/> (<see cref="INamedCreateCount"/>)를 가진 <see cref="MonoBehaviour"/> 객체를  관리하는 무한 스택 매니저
    /// </summary>
    /// <typeparam name="TMonoBehaviour"><see cref="INamedCreateCount"/> 를 보유중인 <see cref="MonoBehaviour"/></typeparam>
    [Serializable]
    [HideReferenceObjectPicker]
    public class InfinityStackManager_NamedMonoBehaviour<TMonoBehaviour> : InfinityStackManagerBase_PreKey<TMonoBehaviour, string, InfinityStack_MonoBehaviour<TMonoBehaviour>>
        where TMonoBehaviour : MonoBehaviour, IName, ICreateCount
    {
        ///======================================================================================================================================================



        /// <inheritdoc/>
        public InfinityStackManager_NamedMonoBehaviour(IEnumerable<TMonoBehaviour> elements, Action<TMonoBehaviour> createEventPost = null, bool poolInitialize_DebugDuplicate = true, int? poolInitialize_UseMaxElementCount = null) : base(elements, createEventPost, poolInitialize_DebugDuplicate, poolInitialize_UseMaxElementCount) { }



        /// <inheritdoc/>
        protected override void InitializeKeyElementMaps(IEnumerable<TMonoBehaviour> elements, out Dictionary<string, TMonoBehaviour> keyElementsMap)
        {
            //. CurrentName을 Key로 사용해 매핑 디셔너리를 초기화한다
            int capacity = elements is ICollection<TMonoBehaviour> collection ? collection.Count : 0;
            keyElementsMap = new Dictionary<string, TMonoBehaviour>(capacity);

            foreach (var element in elements)
            {
                keyElementsMap.Add(element.CurrentName, element);
            }
        }



        /// <inheritdoc/>
        protected override void InitializePool(string key, out InfinityStack_MonoBehaviour<TMonoBehaviour> pool)
        {
            //. CreateCount를 사용해 그만큼 생성 한다
            pool = new InfinityStack_MonoBehaviour<TMonoBehaviour>(KeyElementMaps[key], KeyElementMaps[key].CreateCount, GetCreateEventPost(key), PoolInitialize_DebugDuplicate, PoolInitialize_UseMaxElementCount);
        }



        /// <inheritdoc/>
        protected override void Initialize()
        {
            //. 매핑 딕셔너리로, 각 객체의 CreateCount 만큼 생성시킨다
            foreach (var item in KeyElementMaps)
            {
                Create(item.Key, item.Value.CreateCount);
            }
        }



        ///======================================================================================================================================================



        /// <summary>
        /// <typeparamref name="TMonoBehaviour"/>를 넣는다 (반환한다)
        /// <para><see cref="TMonoBehaviour"/>에 Key로 사용하는 <see cref="TMonoBehaviour.CurrentName"/>이 있으니, 별도의 Key를 받아오지 않아도 Push가 가능</para>
        /// </summary>
        /// <param name="monobehaviour">넣을(반환할) 요소</param>
        public void Push(TMonoBehaviour monobehaviour) => Push(monobehaviour.CurrentName, monobehaviour);



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================



    /// <summary>
    /// 클래스 기반의 DB(Database)로부터 만들어지는 오브젝트/클래스에 공통으로 필요한 인터페이스
    /// </summary>
    /// <typeparam name="TDB">DB 역할의 클래스</typeparam>
    public interface IBaseClassDB<TDB> where TDB : class
    {
        /// <summary>
        /// 이 오브젝트가 참조하는 DB 객체
        /// </summary>
        TDB CurrentDB { get; set; }
    }



    /// <summary>
    /// <typeparamref name="TKey_Class"/>를 클래스 DB로 사용하여, <see cref="MonoBehaviour"/> 기반의 오브젝트를 관리하는 무한스택 매니저
    /// <para>클래스DB인 <typeparamref name="TKey_Class"/>에 <see cref="ICreateCount"/> 가 포함되어있어야 한다</para>
    /// <para>요소인 <typeparamref name="TElement_MonoBehaviour"/>에 <see cref="IBaseClassDB{TDB}"/> 가 포함되어있어야 한다</para>
    /// </summary>
    /// <typeparam name="TKey_Class">클래스DB</typeparam>
    /// <typeparam name="TElement_MonoBehaviour">요소</typeparam>
    [Serializable]
    [HideReferenceObjectPicker]
    public class InfinityStackManager_BaseClassDB_MonoBehaviour<TKey_Class, TElement_MonoBehaviour> : InfinityStackManagerBase_Shared_ClassKey<TElement_MonoBehaviour, TKey_Class, InfinityStack_MonoBehaviour<TElement_MonoBehaviour>>
        where TKey_Class : class, ICreateCount
        where TElement_MonoBehaviour : MonoBehaviour, IBaseClassDB<TKey_Class>
    {
        ///======================================================================================================================================================



        ///<inheritdoc/>
        public InfinityStackManager_BaseClassDB_MonoBehaviour(TElement_MonoBehaviour elementStandard, IEnumerable<TKey_Class> keys, Action<TKey_Class, TElement_MonoBehaviour> createEventPost = null, bool poolInitialize_DebugDuplicate = true, int? poolInitialize_UseMaxElementCount = null) : base(elementStandard, keys, createEventPost, poolInitialize_DebugDuplicate, poolInitialize_UseMaxElementCount) { }



        ///<inheritdoc/>
        protected override void InitializePool(TKey_Class key, out InfinityStack_MonoBehaviour<TElement_MonoBehaviour> pool)
        {
            pool = new InfinityStack_MonoBehaviour<TElement_MonoBehaviour>(ElementStandard, key.CreateCount, GetCreateEventPost(key), PoolInitialize_DebugDuplicate, PoolInitialize_UseMaxElementCount);
        }



        ///<inheritdoc/>
        protected override void Initialize()
        {
            //. Key로 타입을 사용하기 떄문에, 생성된 키타입 딕셔너리를 순회하면서 생성해준다
            foreach (var item in KeyTypeDictionary)
            {
                Create(item.Value, item.Value.CreateCount);
            }
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================



    ///// <summary>
    ///// [구판] <typeparamref name="TElement_BaseClass"/>를 상속받는 모든 클래스들을 구하여,
    ///// <para>각각의 타입 별로 <see cref="InfinityStack_ClassInstance{TClass}"/> 를 생성하여 관리하는 무한 스택 매니저</para>
    ///// </summary>
    ///// <typeparam name="TElement_BaseClass">ICreateCount를 구현하는 클래스 타입</typeparam>
    //[Serializable]
    //[HideReferenceObjectPicker]
    //[Obsolete]
    //public class InfinityStackManager_TypeBaseClassInstance_Legacy<TElement_BaseClass> : InfinityStackManagerBase_PreClassKey<TElement_BaseClass, InfinityStack_ClassInstance<TElement_BaseClass>>
    //    where TElement_BaseClass : class //, ICreateCount
    //{
    //    ///======================================================================================================================================================



    //    ///<inheritdoc/>
    //    ///<param name="ignore_CreateCountInterface">
    //    ///<see cref="TElement_BaseClass"/>에 <see cref="ICreateCount"/>가 있어도 그 생성 개수를 무시 할 지 여부
    //    ///</param>
    //    ///<param name="initializeCountDictionary">
    //    ///각 <see cref="TElement_BaseClass"/>를 최초에 생성할 개수를 정하는 타입 딕셔너리
    //    ///<para>사용하고 싶지 않으면 null을 받아와도 가능</para>
    //    ///</param>
    //    public InfinityStackManager_TypeBaseClassInstance_Legacy(bool ignore_CreateCountInterface, Dictionary<Type, int> initializeCountDictionary, Action<TElement_BaseClass> createEventPost = null, bool poolInitialize_DebugDuplicate = true, int? poolInitialize_UseMaxElementCount = null)
    //        : base(GetElementBaseClasses(), createEventPost, poolInitialize_DebugDuplicate, poolInitialize_UseMaxElementCount)
    //    {
    //        Ignore_CreateCountInterface = ignore_CreateCountInterface;
    //        InitializeCountDictionary = initializeCountDictionary;

    //        //. 인스턴스 팩토리 매니저만 새로 생성해준다
    //        InstanceFactoryManager = new InstanceFactoryManager<TElement_BaseClass>();
    //    }

    //    //! 이거 딕셔너리받아오면 거기게있는타입배열로쓰면되는데 뭐하러이걸로함?

    //    //? 부모 생성자에 사용되는, TElement_BaseClass를 상속받는 모든 유효한 객체들의 배열을 반환
    //    private static TElement_BaseClass[] GetElementBaseClasses()
    //    {
    //        SU_Collection_Types.CreateTypeArray<TElement_BaseClass>(SU_Collection_Types.TypeSearch.ConcreteClasses, out var arr);
    //        return arr;
    //    }



    //    ///<inheritdoc/>
    //    protected override void InitializeKeyElementMaps(TElement_BaseClass[] elements, out Dictionary<Type, TElement_BaseClass> keyElementsMap)
    //    {
    //        keyElementsMap = new Dictionary<Type, TElement_BaseClass>(elements.Length);

    //        for (int i = 0; i < elements.Length; i++)
    //        {
    //            keyElementsMap.Add(elements[i].GetType(), elements[i]);
    //        }
    //    }



    //    ///<inheritdoc/>
    //    protected override void InitializePool(Type key, out InfinityStack_ClassInstance<TElement_BaseClass> pool)
    //    {
    //        var element = KeyElementMaps[key];

    //        if (!Ignore_CreateCountInterface && element is ICreateCount elementCreateCount)
    //        {
    //            pool = new InfinityStack_ClassInstance<TElement_BaseClass>(key, elementCreateCount.CreateCount, InstanceFactoryManager, GetCreateEventPost(key), PoolInitialize_DebugDuplicate, PoolInitialize_UseMaxElementCount);
    //        }
    //        else
    //        {
    //            pool = new InfinityStack_ClassInstance<TElement_BaseClass>(key, 0, InstanceFactoryManager, GetCreateEventPost(key), PoolInitialize_DebugDuplicate, PoolInitialize_UseMaxElementCount);
    //        }
    //    }



    //    ///<inheritdoc/>
    //    protected override void Initialize()
    //    {
    //        //! 최초 설정딕셔너리가 존재하여 사용할수 있을경우
    //        if (InitializeCountDictionary != null)
    //        {
    //            foreach (var item in KeyElementMaps)
    //            {
    //                //. 설정 딕셔너리에 있는 값을 사용해 최초 생성 개수를 받아와 생성한다
    //                Create(item.Key, InitializeCountDictionary[item.Key]);
    //            }
    //        }

    //        //! 최초 설정딕셔너리가 존재하지 않아 사용할수 있을경우
    //        else
    //        {
    //            foreach (var item in KeyElementMaps)
    //            {
    //                //. ICreateCount 사용이 가능하게 설정이 되어있고, ICreateCount를 상속받고 있다면 그 최초 생성 개수 만큼 생성한다
    //                if (!Ignore_CreateCountInterface && item.Value is ICreateCount elementCreateCount)
    //                {
    //                    Create(item.Key, elementCreateCount.CreateCount);
    //                }
    //                //. 그 외에는 0개를 생성한다
    //                else
    //                {
    //                    Create(item.Key, 0);
    //                }
    //            }
    //        }
    //    }



    //    ///======================================================================================================================================================



    //    //? 최초 생성



    //    /// <summary>
    //    /// <see cref="TElement_BaseClass"/>에 <see cref="ICreateCount"/>가 있어도 그 생성 개수를 무시 할 지 여부
    //    /// </summary>
    //    private readonly bool Ignore_CreateCountInterface;



    //    /// <summary>
    //    /// 각 <see cref="TElement_BaseClass"/>를 최초에 생성할 개수를 정하는 타입 딕셔너리
    //    ///<para>null이라면 <see cref="Initialize"/>에서 0개를 생성하거나 <see cref="ICreateCount"/> 설정에 맞춰 생성함</para>
    //    /// </summary>
    //    private readonly Dictionary<Type, int> InitializeCountDictionary;



    //    ///======================================================================================================================================================



    //    //? 베이스 클래스를 기반으로 캐싱하여 빠르게 클래스 생성해주는 팩토리 매니저



    //    /// <summary>
    //    /// <see cref="TElement_BaseClass"/>들을 생성할때, 더 효율적이게 생성하게 해주는 인스턴스 팩토리 매니저
    //    /// </summary>
    //    private readonly InstanceFactoryManager<TElement_BaseClass> InstanceFactoryManager;



    //    ///======================================================================================================================================================



    //    //? 요소 넣기 (Push)



    //    /// <summary>
    //    /// <typeparamref name="TElement_BaseClass"/>를 넣는다 (반환한다)
    //    /// <para>각 풀의 <see cref="TInfinityStack.inPool"/>에서 중복 검사를 하여, 이미 추가된 <typeparamref name="TElement_BaseClass"/> 일 경우 false를 반환한다</para>
    //    /// <para>디버그 모드에 따라 이를 메세지로 출력 하기도 한다</para>
    //    /// </summary>
    //    /// <param name="element">넣을(반환할) 요소</param>
    //    public bool Push(TElement_BaseClass element)
    //    {
    //        return Push(element.GetType(), element);
    //    }



    //    ///======================================================================================================================================================



    //    //? Dispose 패턴



    //    protected override void DisposeInternal()
    //    {
    //        InstanceFactoryManager.Dispose();

    //        base.DisposeInternal();
    //    }



    //    ///======================================================================================================================================================
    //}



    ///======================================================================================================================================================



    /// <summary>
    /// [개선판] <typeparamref name="TElement_BaseClass"/>를 상속받는 모든 클래스들을 구하여,
    /// <para>각각의 타입 별로 <see cref="InfinityStack_ClassInstance{TClass}"/> 를 생성하여 관리하는 무한 스택 매니저</para>
    /// </summary>
    /// <typeparam name="TElement_BaseClass">ICreateCount를 구현하는 클래스 타입</typeparam>
    [Serializable]
    [HideReferenceObjectPicker]
    public class InfinityStackManager_TypeBaseClassInstance_Improved<TElement_BaseClass> : InfinityStackManagerBase<TElement_BaseClass, Type, InfinityStack_ClassInstance<TElement_BaseClass>>
        where TElement_BaseClass : class
    {
        ///======================================================================================================================================================



        ///<inheritdoc/>
        ///<param name="initializeCountDictionary">
        ///각 <see cref="TElement_BaseClass"/>를 최초에 생성할 개수를 정하는 타입 딕셔너리
        ///<para>사용하고 싶지 않으면 null을 받아와도 OK</para>
        ///<para>단 null을 받아오면 각 <typeparamref name="TElement_BaseClass"/>가 최초 초기화 개수가 0개씩 생성되는 딕셔너리를 자체적으로 생성함</para>
        ///</param>
        public InfinityStackManager_TypeBaseClassInstance_Improved(Dictionary<Type, int> initializeCountDictionary, Action<TElement_BaseClass> createEventPost = null, bool poolInitialize_DebugDuplicate = true, int? poolInitialize_UseMaxElementCount = null)
           : base(poolInitialize_DebugDuplicate, poolInitialize_UseMaxElementCount, 0)
        {
            InitializeCountDictionary = initializeCountDictionary;


            //. 초기화개수 딕셔너리를 받아오지않았을경우, 직접 만든다
            if (initializeCountDictionary == null)
            {
                //. TElement_BaseClass의 타입 배열을 만들고
                var types = SU_Collection_Types.GetTypesAssignableTo<TElement_BaseClass>(SU_Collection_Types.TypeSearch.ConcreteClasses);


                //. 그 배열의 크기만큼 딕셔너리를 초기화한다
                InitializeCountDictionary = new Dictionary<Type, int>(types.Length);

                //. 그리고 타입배열을 순회하면서, 각 타입을 추가한다
                //. Value는 0이된다
                for (int i = 0; i < types.Length; i++)
                {
                    InitializeCountDictionary.Add(types[i], 0);
                }
            }


            //. 딕셔너리 null유무와 관계없이, InitializeCountDictionary의 작업이 끝났으니 수동으로 풀딕셔너리의 최초용량을 지정해준다
            PoolDictionary.EnsureCapacity(InitializeCountDictionary.Count);


            //. 인스턴스 팩토리 매니저만 새로 생성해준다
            InstanceFactoryManager = new InstanceFactoryManager<TElement_BaseClass>();
        }



        ///<inheritdoc/>
        protected override void InitializePool(Type key, out InfinityStack_ClassInstance<TElement_BaseClass> pool)
        {
            pool = new InfinityStack_ClassInstance<TElement_BaseClass>(key, 0, InstanceFactoryManager, GetCreateEventPost(key), PoolInitialize_DebugDuplicate, PoolInitialize_UseMaxElementCount);
        }



        ///<inheritdoc/>
        protected override void Initialize()
        {
            foreach (var item in InitializeCountDictionary)
            {
                //. 설정 딕셔너리에 있는 값을 사용해 최초 생성 개수를 받아와 생성한다
                Create(item.Key, InitializeCountDictionary[item.Key]);
            }
        }



        ///======================================================================================================================================================



        //? 풀 딕셔너리 관련 이벤트



        /// <summary>
        /// 요소가 생성 된 이후에 추가로 수행할 이벤트
        /// <para><see cref="InfinityStackManagerBase.PoolDictionary"/>이나 <typeparamref name="TInfinityStack"/>를 수정하는 이벤트는 절대 사용하면 안됨</para>
        /// </summary>
        public event Action<TElement_BaseClass> CreateEventPost;



        /// <inheritdoc/>
        protected override Action<TElement_BaseClass> GetCreateEventPost(Type key) => CreateEventPost;



        ///======================================================================================================================================================



        //? 최초 생성



        /// <summary>
        /// 각 <see cref="TElement_BaseClass"/>를 최초에 생성할 개수를 정하는 타입 딕셔너리
        ///<para>null이라면 <see cref="Initialize"/>에서 0개를 생성하거나 <see cref="ICreateCount"/> 설정에 맞춰 생성함</para>
        /// </summary>
        private readonly Dictionary<Type, int> InitializeCountDictionary;



        ///======================================================================================================================================================



        //? 베이스 클래스를 기반으로 캐싱하여 빠르게 클래스 생성해주는 팩토리 매니저



        /// <summary>
        /// <see cref="TElement_BaseClass"/>들을 생성할때, 더 효율적이게 생성하게 해주는 인스턴스 팩토리 매니저
        /// </summary>
        private readonly InstanceFactoryManager<TElement_BaseClass> InstanceFactoryManager;



        ///======================================================================================================================================================



        //? 풀 딕셔너리 확인



        /// <summary>
        /// <paramref name="key">에 속한 <typeparamref name="TInfinityStack"/>의  <see cref="TInfinityStack.PoolCount"/> 를 구해보기
        /// </summary>
        public bool TryGetPoolCount<T>(out int value) where T : TElement_BaseClass => TryGetPoolCount(typeof(T), out value);



        ///======================================================================================================================================================



        //? 요소 생성 (Create)



        /// <summary>
        /// <typeparamref name="TKey"/>를 <paramref name="createCount"/> 만큼 생성한다
        /// <para>각 풀의 <see cref="TInfinityStack.MaxElementCount"/> 를 넘으면 반드시 생성을 보장하지 않으며,</para>
        /// <para>그렇게 <typeparamref name="TKey"/>를 생성된 개수를 반환한다 (int), 하나도 생성하지 못하면 0을 반환한다</para>
        /// <para>내부에서 <see cref="TInfinityStack.CreateInternal(int, Action{TKey})"/>을 재정의하여 그 곳에서 각 <typeparamref name="TKey"/>에 맞게 생성한다</para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="createCount"><typeparamref name="TKey"/>를 생성할 개수</param>
        /// <returns></returns>
        public int Create<T>(int createCount) where T : TElement_BaseClass => Create(typeof(T), createCount);



        /// <summary>
        /// [비동기] <typeparamref name="TKey"/>를 <paramref name="createCount"/> 만큼 생성한다
        /// <para>각 풀의 <see cref="TInfinityStack.MaxElementCount"/> 를 넘으면 반드시 생성을 보장하지 않으며,</para>
        /// <para>그렇게 <typeparamref name="TKey"/>를 생성된 개수를 반환한다 (int), 하나도 생성하지 못하면 0을 반환한다</para>
        /// <para>내부에서 <see cref="TInfinityStack.CreateInternal(int, Action{TKey})"/>을 재정의하여 그 곳에서 각 <typeparamref name="TKey"/>에 맞게 생성한다</para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="createCount"><typeparamref name="TKey"/>를 생성할 개수</param>
        /// <returns></returns>
        public async UniTask<int> CreateAsync<T>(int createCount) where T : TElement_BaseClass => await CreateAsync(typeof(T), createCount);



        ///======================================================================================================================================================



        //? 요소 넣기 (Push)



        /// <summary>
        /// <typeparamref name="TKey"/>를 넣는다 (반환한다)
        /// <para>각 풀의 <see cref="TInfinityStack.inPool"/>에서 중복 검사를 하여, 이미 추가된 <typeparamref name="TKey"/> 일 경우 false를 반환한다</para>
        /// <para>디버그 모드에 따라 이를 메세지로 출력 하기도 한다</para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="element">넣을(반환할) 요소</param>
        public void Push<T>(TElement_BaseClass value) where T : TElement_BaseClass => Push(typeof(T), value);




        /// <summary>
        /// <typeparamref name="TElement_BaseClass"/>를 넣는다 (반환한다)
        /// <para>각 풀의 <see cref="TInfinityStack.inPool"/>에서 중복 검사를 하여, 이미 추가된 <typeparamref name="TElement_BaseClass"/> 일 경우 false를 반환한다</para>
        /// <para>디버그 모드에 따라 이를 메세지로 출력 하기도 한다</para>
        /// </summary>
        /// <param name="element">넣을(반환할) 요소</param>
        public bool Push(TElement_BaseClass element) => Push(element.GetType(), element);




        /// <summary>
        /// <typeparamref name="TKey"/>들을 "배열"로 넣는다 (반환한다)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="elements">넣을(반환할) 요소 들의 배열</param>
        public void PushRange<T>(params TElement_BaseClass[] elements) where T : TElement_BaseClass => PushRange(typeof(T), elements);



        /// <summary>
        /// <typeparamref name="TKey"/>들을 "리스트"로 넣는다 (반환한다)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="elements">넣을(반환할) 요소 들의 배열</param>
        public void PushRange<T>(IList<TElement_BaseClass> elements) where T : TElement_BaseClass => PushRange(typeof(T), elements);



        /// <summary>
        /// <typeparamref name="TKey"/>들을 "IEnumberable"로 넣는다 (반환한다)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="elements">넣을(반환할) 요소 들의 배열</param>
        public void PushRange<T>(IEnumerable<TElement_BaseClass> elements) where T : TElement_BaseClass => PushRange(typeof(T), elements);



        ///======================================================================================================================================================



        //? 요소 해제하기 (Release)



        /// <summary>
        /// <see cref="TInfinityStack.poolStack"/>에 생성한 <typeparamref name="TKey"/>들을 해제(제거)하며
        /// <para>해제에 성공한 개수를 반환한다</para>
        /// <para>정말 더이상 사용 하지 않을때, 메모리를 확보 하기 위해 사용한다</para>
        /// <para><see cref="PoolDictionary"/>에 <paramref name="key"/>가 존재하지 않으면 0을 반환한다</para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="releaseCount">해제할 <typeparamref name="TKey"/>의 개수</param>
        public int Release<T>(int count) where T : TElement_BaseClass => Release(typeof(T), count);



        /// <summary>
        /// <see cref="TInfinityStack.poolStack"/>에 생성한 모든<typeparamref name="TKey"/>들을 해제(제거)한다
        /// <para>정말 더이상 사용 하지 않을때, 메모리를 확보 하기 위해 사용한다</para>
        /// <para><paramref name="removePoolInPoolDictionary"/>를 사용 할 경우, 해당 <typeparamref name="TInfinityStack"/> 자체를 해제하여 레퍼런스 카운트를 0 으로 만든다</para>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="removePoolInPoolDictionary"><typeparamref name="TInfinityStack"/> 를 딕셔너리에서 제거 할 지 여부</param>
        public void ReleaseClear<T>(bool removePoolInPoolDictionary) where T : TElement_BaseClass => ReleaseClear(typeof(T), removePoolInPoolDictionary);




        ///======================================================================================================================================================



        //? 요소 꺼내기 (Pop)



        /// <summary>
        /// <typeparamref name="TKey"/>를 꺼낸다
        /// <para>풀의 상태와 파라미터에 따라, <b>반환을 무조건 보장하지는 않으며</b>, 이때 null을 반환한다</para>
        /// </summary>
        /// <param name="autoTryCreateElementsWhenPoolNotEnough">
        /// 꺼내야 하는데, 풀의 크기가 모자랄 경우 (+풀이 존재 하지 않거나), <see cref="Create(int, Action{TKey})"/>을 한 이후 반환 할 지 여부
        /// <para>null을 받아오면, 풀의 크기가 모자랄경우 반환에 실패한다</para>
        /// <para>값을 넣더라도, <see cref="TInfinityStack.MaxElementCount"/>에 의해 반환에 실패 할 수도 있다</para>
        /// <para>기본값: 1</para>
        /// </param>
        /// <returns></returns>
        public T Pop<T>(int? autoTryCreateElementsWhenPoolNotEnough = 1) where T : class, TElement_BaseClass => Pop(typeof(T), autoTryCreateElementsWhenPoolNotEnough) as T;



        /// <summary>
        /// [비동기] <typeparamref name="TKey"/>를 꺼낸다
        /// <para>풀의 상태와 파라미터에 따라, <b>반환을 무조건 보장하지는 않으며</b>, 이때 null을 반환한다</para>
        /// </summary>
        /// <param name="autoTryCreateElementsWhenPoolNotEnough">
        /// 꺼내야 하는데, 풀의 크기가 모자랄 경우 (+풀이 존재 하지 않거나), <see cref="Create(int, Action{TKey})"/>을 한 이후 반환 할 지 여부
        /// <para>null을 받아오면, 풀의 크기가 모자랄경우 반환에 실패한다</para>
        /// <para>값을 넣더라도, <see cref="TInfinityStack.MaxElementCount"/>에 의해 반환에 실패 할 수도 있다</para>
        /// <para>기본값: 1</para>
        /// </param>
        /// <returns></returns>
        public async UniTask<T> PopAsync<T>(int? autoTryCreateElementsWhenPoolNotEnough = 1) where T : class, TElement_BaseClass => await PopAsync(typeof(T), autoTryCreateElementsWhenPoolNotEnough) as T;



        ///======================================================================================================================================================



        //? Dispose 패턴



        protected override void DisposeInternal()
        {
            InstanceFactoryManager.Dispose();

            base.DisposeInternal();
        }

        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================



    /// <summary>
    /// 무한스택 기반 오브젝트 풀 
    ///</summary> 
    [Serializable]
    public class InfinityObjectPool<TMono> where TMono : MonoBehaviour
    {
        public void InitializePool(TMono targetObject = null, int? initialPoolCount = null, Action<TMono> createEventPost = null, bool debugPushDuplicate = true, int? useMaxElementCount = null)
        {
            if (IsInitialized) { return; }

            if (targetObject != null) TargetObject = targetObject;
            if (initialPoolCount.HasValue) InitialPoolCount = initialPoolCount.Value;

            if (TargetObject != null)
            {
                m_Pool = new InfinityStack_MonoBehaviour<TMono>(TargetObject, InitialPoolCount, createEventPost, debugPushDuplicate, useMaxElementCount);
                m_Pool.Create(InitialPoolCount);
            }
            else
            {
                Debug.LogError($"({typeof(TMono).Name})InfinityObjectPool TargetObject가 null");
            }

            IsInitialized = true;
        }



        [ShowInInspector]
        [LabelText("초기화 여부"), DisplayAsString, EnableGUI]
        public bool IsInitialized { get; private set; }



        [PropertyOrder(-100)]
        [SerializeField, HideLabel]
        private InfinityStack_MonoBehaviour<TMono> m_Pool;
        public InfinityStack_MonoBehaviour<TMono> Pool => m_Pool;



        [PropertyOrder(0)]
        [LabelText("대상 오브젝트")]
#if UNITY_EDITOR
        [GUIColor(nameof(editorColor_TargetObject))]
#endif
        public TMono TargetObject;



        [PropertyOrder(0)]
        [LabelText("초기 풀 개수")]
        [ReadOnlyCustom(true)]
        public int InitialPoolCount;

#if UNITY_EDITOR

        private Color editorColor_TargetObject => TargetObject == null ? Color.red : Color.white;

#endif
    }



    /// <summary>
    /// 무한스택 기반 오브젝트 풀 (+어드레서블)
    ///</summary> 
    [Serializable]
    public class InfinityObjectPool_Addressable<TMono> where TMono : MonoBehaviour
    {
        public void InitializePool(int? initialPoolCount = null, Action<TMono> createEventPost = null, bool debugPushDuplicate = true, int? useMaxElementCount = null)
        {
            if (IsInitialized) { return; }

            if (initialPoolCount.HasValue) InitialPoolCount = initialPoolCount.Value;

            if (TargetObject != null)
            {
                m_Pool = new InfinityStack_MonoBehaviour<TMono>(TargetObject, InitialPoolCount, createEventPost, debugPushDuplicate, useMaxElementCount);
                m_Pool.Create(InitialPoolCount);
            }
            else
            {
                Debug.LogError($"({typeof(TMono).Name})InfinityObjectPool TargetObject가 null");
            }

            IsInitialized = true;
        }



        [ShowInInspector]
        [LabelText("초기화 여부"), DisplayAsString, EnableGUI]
        public bool IsInitialized { get; private set; }



        [PropertyOrder(-100)]
        [SerializeField, HideLabel]
        private InfinityStack_MonoBehaviour<TMono> m_Pool;
        public InfinityStack_MonoBehaviour<TMono> Pool => m_Pool;



        [PropertyOrder(-1)]
        [ShowInInspector]
        [LabelText("대상 오브젝트 (어드레서블)")]
        [PropertyTooltip("어드레서블 에셋 레퍼런스를 통해 프리팹을 할당합니다.\n어드레서블 설정이 올바르게 되어 있어야 합니다.")]
#if UNITY_EDITOR
        [OnValueChanged(nameof(editorRefresh_TargetObject_AssetReference), IncludeChildren = true)]
#endif
        private AssetReferenceGameObject TargetObject_AssetReference
        {
            get => targetObject_AssetReference;
            set => targetObject_AssetReference = value;
        }
        [SerializeField, HideInInspector]
        private AssetReferenceGameObject targetObject_AssetReference;



#if UNITY_EDITOR
        private void editorRefresh_TargetObject_AssetReference()
        {
            //? 어드레서블 참조가 유효한지 먼저 확인
            if (TargetObject_AssetReference == null || !TargetObject_AssetReference.RuntimeKeyIsValid())
            {
                TargetObject = null;
                return;
            }

            //. 에디터 전용으로 프리팹 동기 접근
            var prefab = TargetObject_AssetReference.editorAsset as GameObject;
            if (prefab == null)
            {
                TargetObject = null;
                Debug.LogError($"InfinityObjectPool_Addressable<{typeof(TMono).Name}>: 프리팹을 찾을 수 없습니다.");
                targetObject_AssetReference = null;
                return;
            }

            //. 루트에 없으면 자식에서도 검색 허용
            if (!prefab.TryGetComponent<TMono>(out var comp))
                comp = prefab.GetComponentInChildren<TMono>(true);

            if (comp != null)
            {
                TargetObject = comp; //? 성공 매핑
            }
            else
            {
                TargetObject = null;
                Debug.LogError($"InfinityObjectPool_Addressable<{typeof(TMono).Name}>: '{prefab.name}'에 {typeof(TMono).Name} 컴포넌트가 없습니다.");
                targetObject_AssetReference = null;
            }

        }
#endif



        [PropertyOrder(0)]
        [LabelText("대상 오브젝트")]
        [ReadOnly]
        public TMono TargetObject;



        [PropertyOrder(0)]
        [LabelText("초기 풀 개수")]
        [ReadOnlyCustom(true)]
        public int InitialPoolCount;
    }



    ///======================================================================================================================================================



    #region 레거시 짬통

    #region 임시백업용 InfinityStackManagerBase, InfinityStackManagerBase2222

    ////! 각각 객체가 다를때 사용해야되는걸 기반으로 설계됨 이거는
    //[Serializable]
    //public abstract class InfinityStackManagerBase<TElement, TKey, TInfinityStack>
    //    where TElement : class
    //    where TInfinityStack : InfinityStackBase<TElement>
    //{
    //    ///======================================================================================================================================================



    //    public InfinityStackManagerBase(TElement[] elements, Action<TElement> createEvent = null)
    //    {
    //        CreateEventPost = createEvent;

    //        InitializeMapping(elements, out ElementsMap);

    //        Pools = new Dictionary<TKey, TInfinityStack>(elements.Length);
    //        foreach (var item in ElementsMap)
    //        {
    //            InitializePool(item.Key, out var pool);
    //            Pools.Add(item.Key, pool);
    //        }
    //    }


    //    //! 초기 매핑 안하는버전, 자식에서 직접해라
    //    protected InfinityStackManagerBase(Action<TElement> createEvent = null)
    //    {
    //        CreateEventPost = createEvent;
    //    }




    //    protected abstract void InitializeMapping(TElement[] elements, out Dictionary<TKey, TElement> elementsMap);



    //    protected abstract void InitializePool(TKey key, out TInfinityStack pool);



    //    //. 매니저에서 스택 자체를 릴리즈하는거 추가하기, (키도 같이 날려버리게)
    //    //! 어차피 풀 딕셔너리에 접근하는 모든 메서드에 TryGet으로 검사하고, 자동으로 또 생성 할수 있게끔 구조가 짜여져있음



    //    ///======================================================================================================================================================



    //    protected Dictionary<TKey, TElement> ElementsMap; //readonly
    //    //! readonly뺀거 위험해 그런데 자식에서 이거 지정하려고 이렇게했어



    //    //? 풀 관리



    //    /// <summary>
    //    /// 무한 스택을 각 객체의 이름 (<see cref="IName"/>)을 Key로 사용하여 각각의 무한 스택을 관리하는 풀
    //    /// </summary>
    //    protected Dictionary<TKey, TInfinityStack> Pools; //readonly
    //    //! readonly뺀거 위험해 그런데 자식에서 이거 지정하려고 이렇게했어


    //    /// <summary>
    //    /// 스택들을 읽기 전용으로 반환합니다.
    //    /// </summary>
    //    public IReadOnlyDictionary<TKey, TInfinityStack> GetStacks => Pools;




    //    /// <summary>
    //    /// 특정 이름의 스택에 현재 몇 개가 있는지 반환합니다.
    //    /// </summary>
    //    /// <param name="key">스택 이름 (ThisName)</param>
    //    /// <param name="value">스택에 있는 객체 수</param>
    //    /// <returns>해당 이름의 스택이 존재하면 true, 없으면 false</returns>
    //    public bool TryGet_Count_Pool(TKey key, out int value)
    //    {
    //        if (Pools.TryGetValue(key, out var result))
    //        {
    //            value = result.PoolCount;
    //            return true;
    //        }

    //        value = 0;
    //        return false;
    //    }



    //    /// <summary>
    //    /// 특정 이름의 스택이 비어있는지 여부를 반환합니다.
    //    /// </summary>
    //    /// <param name="key">스택 이름 (ThisName)</param>
    //    /// <param name="value">비어있으면 true</param>
    //    /// <returns>해당 이름의 스택이 존재하면 true, 없으면 false</returns>
    //    public bool TryGet_IsEmpty_Pool(TKey key, out bool value)
    //    {
    //        if (Pools.TryGetValue(key, out var result))
    //        {
    //            value = (result.PoolCount == 0);
    //            return true;
    //        }
    //        value = false;
    //        return false;
    //    }



    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// 요소가 생성 된 이후에 추가로 수행할 이벤트
    //    /// <para><see cref="Pools"/>를 수정하는 이벤트는 절대 사용하면 안됨</para>
    //    /// </summary>
    //    public event Action<TElement> CreateEventPost;



    //    /// <summary>
    //    /// 요소를 Push 시킨 이후에 추가로 수행할 이벤트
    //    /// <para><see cref="Pools"/>를 수정하는 이벤트는 절대 사용하면 안됨</para>
    //    /// </summary>
    //    public event Action<TElement> PushEventPre;



    //    /// <summary>
    //    /// 요소를 Release 시킨 이후에 추가로 수행할 이벤트
    //    /// <para><see cref="Pools"/>를 수정하는 이벤트는 절대 사용하면 안됨</para>
    //    /// </summary>
    //    public event Action<TElement> ReleaseEventPost;



    //    protected void AddCreateEventPost(Action<TElement> createEventPost)
    //    {
    //        CreateEventPost += createEventPost;
    //    }



    //    protected void RemoveCreateEventPost(Action<TElement> createEventPost)
    //    {
    //        CreateEventPost -= createEventPost;
    //    }



    //    protected void ExecuteCreateEventPost(TElement element)
    //    {
    //        CreateEventPost?.Invoke(element);
    //    }



    //    ///======================================================================================================================================================



    //    //? 요소 생성 (Create)



    //    public void Create(TKey key, int createCount)
    //    {
    //        if (Pools.TryGetValue(key, out var pool))
    //        {
    //            pool.Create(createCount, CreateEventPost);
    //        }
    //        else
    //        {
    //            //pool = new TInfinityStack();
    //            InitializePool(key, out pool);
    //            pool.Create(createCount, CreateEventPost);
    //            Pools.Add(key, pool);
    //        }
    //    }



    //    public async UniTask CreateAsync(TKey key, int createCount)
    //    {
    //        if (Pools.TryGetValue(key, out var pool))
    //        {
    //            await pool.CreateAsync(createCount, CreateEventPost);
    //        }
    //        else
    //        {
    //            //pool = new TInfinityStack();
    //            InitializePool(key, out pool);
    //            await pool.CreateAsync(createCount, CreateEventPost);
    //            Pools.Add(key, pool);
    //        }
    //    }



    //    ///======================================================================================================================================================



    //    //? 요소 넣기 (Push)



    //    public void Push(TKey key, TElement element)
    //    {
    //        if (Pools.TryGetValue(key, out var pool))
    //        {
    //            pool.Push(element);
    //        }
    //        else
    //        {
    //            //pool = new TInfinityStack();
    //            InitializePool(key, out pool);
    //            pool.Push(element);
    //            Pools.Add(key, pool);
    //        }
    //    }



    //    public void PushRange(TKey key, params TElement[] elements)
    //    {
    //        if (Pools.TryGetValue(key, out var pool))
    //        {
    //            pool.PushRange(elements);
    //        }
    //        else
    //        {
    //            //pool = new TInfinityStack();
    //            InitializePool(key, out pool);
    //            pool.PushRange(elements);
    //            Pools.Add(key, pool);
    //        }
    //    }



    //    public void PushRange(TKey key, IList<TElement> elements)
    //    {
    //        if (Pools.TryGetValue(key, out var pool))
    //        {
    //            pool.PushRange(elements);
    //        }
    //        else
    //        {
    //            //pool = new TInfinityStack();
    //            InitializePool(key, out pool);
    //            pool.PushRange(elements);
    //            Pools.Add(key, pool);
    //        }
    //    }



    //    public void PushRange(TKey key, IEnumerable<TElement> elements)
    //    {
    //        if (Pools.TryGetValue(key, out var pool))
    //        {
    //            pool.PushRange(elements);
    //        }
    //        else
    //        {
    //            //pool = new TInfinityStack();
    //            InitializePool(key, out pool);
    //            pool.PushRange(elements);
    //            Pools.Add(key, pool);
    //        }
    //    }



    //    ///======================================================================================================================================================



    //    //? 요소 해제하기 (Release)



    //    public void Release(TKey key, int releaseCount)
    //    {
    //        if (Pools.TryGetValue(key, out var pool))
    //        {
    //            pool.Release(releaseCount);
    //        }
    //    }



    //    public void ReleaseClear(TKey key, bool removePools)
    //    {
    //        if (Pools.TryGetValue(key, out var pool))
    //        {
    //            pool.ReleaseClear();

    //            if (removePools) { Pools.Remove(key); }
    //        }
    //    }



    //    ///======================================================================================================================================================



    //    //? 요소 꺼내기 (Pop)



    //    /// <summary>
    //    /// <typeparamref name="TElement"/>를 꺼낸다
    //    /// <para>풀의 상태와 파라미터에 따라, <b>반환을 무조건 보장하지는 않으며</b>, 이때 null을 반환한다</para>
    //    /// </summary>
    //    /// <param name="autoTryCreateElementsWhenPoolNotEnough">
    //    /// 꺼내야 하는데, 풀의 크기가 모자랄 경우, <see cref="Create(int, Action{TElement})"/>을 한 이후 반환 할 지 여부
    //    /// <para>null을 받아오면, 풀의 크기가 모자랄경우 반환에 실패한다</para>
    //    /// <para>값을 넣더라도, <see cref="MaxElementCount"/>에 의해 반환에 실패 할 수도 있다</para>
    //    /// <para>기본값: 1</para>
    //    /// </param>
    //    /// <returns></returns>
    //    public TElement Pop(TKey key, int? autoTryCreateElementsWhenPoolNotEnough = 1)
    //    {
    //        if (Pools.TryGetValue(key, out var pool))
    //        {
    //            return pool.Pop(autoTryCreateElementsWhenPoolNotEnough, CreateEventPost);
    //        }
    //        else if (autoTryCreateElementsWhenPoolNotEnough.HasValue && autoTryCreateElementsWhenPoolNotEnough.Value > 0)
    //        {
    //            //pool = new TInfinityStack();
    //            InitializePool(key, out pool);
    //            pool.Create(autoTryCreateElementsWhenPoolNotEnough.Value, CreateEventPost);
    //            Pools.Add(key, pool);

    //            return pool.Pop(1);
    //        }

    //        return null;
    //    }



    //    /// <summary>
    //    /// [비동기] <typeparamref name="TElement"/>를 꺼낸다
    //    /// <para>풀의 상태와 파라미터에 따라, <b>반환을 무조건 보장하지는 않으며</b>, 이때 null을 반환한다</para>
    //    /// </summary>
    //    /// <param name="autoTryCreateElementsWhenPoolNotEnough">
    //    /// 꺼내야 하는데, 풀의 크기가 모자랄 경우, <see cref="Create(int, Action{TElement})"/>을 한 이후 반환 할 지 여부
    //    /// <para>null을 받아오면, 풀의 크기가 모자랄경우 반환에 실패한다</para>
    //    /// <para>값을 넣더라도, <see cref="MaxElementCount"/>에 의해 반환에 실패 할 수도 있다</para>
    //    /// <para>기본값: 1</para>
    //    /// </param>
    //    /// <returns></returns>
    //    public async UniTask<TElement> PopAsync(TKey key, int? autoTryCreateElementsWhenPoolNotEnough = 1)
    //    {
    //        if (Pools.TryGetValue(key, out var pool))
    //        {
    //            return await pool.PopAsync(autoTryCreateElementsWhenPoolNotEnough);
    //        }
    //        else if (autoTryCreateElementsWhenPoolNotEnough.HasValue && autoTryCreateElementsWhenPoolNotEnough.Value > 0)
    //        {
    //            //pool = new TInfinityStack();
    //            InitializePool(key, out pool);
    //            await pool.CreateAsync(autoTryCreateElementsWhenPoolNotEnough.Value, CreateEventPost);
    //            Pools.Add(key, pool);

    //            return await pool.PopAsync(1);
    //        }

    //        return null;
    //    }



    //    ///======================================================================================================================================================
    //}


    ////! 각각 객체가 다를때 사용해야되는걸 기반으로 설계됨 이거는
    //[Serializable]
    //public abstract class InfinityStackManagerBase2222<TElement, TKey, TInfinityStack>
    //    where TElement : class
    //    where TInfinityStack : InfinityStackBase<TElement>
    //{
    //    ///======================================================================================================================================================



    //    public InfinityStackManagerBase2222(TKey[] keys, TElement elementOneeeee, Action<TKey, TElement> createEvent = null)
    //    {
    //        ElementOnlyOneeeeee = elementOneeeee;
    //        CreateEventPost = createEvent;

    //        Pools = new Dictionary<TKey, TInfinityStack>(keys.Length);

    //        for (int i = 0; i < keys.Length; i++)
    //        {
    //            InitializePool(keys[i], out var pool);
    //            Pools.Add(keys[i], pool);
    //        }
    //    }



    //    protected readonly TElement ElementOnlyOneeeeee;



    //    protected abstract void InitializePool(TKey key, out TInfinityStack pool);



    //    ///======================================================================================================================================================



    //    //? 풀 관리



    //    /// <summary>
    //    /// 무한 스택을 각 객체의 이름 (<see cref="IName"/>)을 Key로 사용하여 각각의 무한 스택을 관리하는 풀
    //    /// </summary>
    //    private readonly Dictionary<TKey, TInfinityStack> Pools;



    //    /// <summary>
    //    /// 특정 이름의 스택에 현재 몇 개가 있는지 반환합니다.
    //    /// </summary>
    //    /// <param name="key">스택 이름 (ThisName)</param>
    //    /// <param name="value">스택에 있는 객체 수</param>
    //    /// <returns>해당 이름의 스택이 존재하면 true, 없으면 false</returns>
    //    public bool TryGet_Count_Pool(TKey key, out int value)
    //    {
    //        if (Pools.TryGetValue(key, out var result))
    //        {
    //            value = result.PoolCount;
    //            return true;
    //        }

    //        value = 0;
    //        return false;
    //    }



    //    /// <summary>
    //    /// 특정 이름의 스택이 비어있는지 여부를 반환합니다.
    //    /// </summary>
    //    /// <param name="key">스택 이름 (ThisName)</param>
    //    /// <param name="value">비어있으면 true</param>
    //    /// <returns>해당 이름의 스택이 존재하면 true, 없으면 false</returns>
    //    public bool TryGet_IsEmpty_Pool(TKey key, out bool value)
    //    {
    //        if (Pools.TryGetValue(key, out var result))
    //        {
    //            value = (result.PoolCount == 0);
    //            return true;
    //        }
    //        value = false;
    //        return false;
    //    }



    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// 요소가 생성 된 이후에 추가로 수행할 이벤트
    //    /// <para><see cref="Pools"/>를 수정하는 이벤트는 절대 사용하면 안됨</para>
    //    /// </summary>
    //    public event Action<TKey, TElement> CreateEventPost;



    //    public Dictionary<TKey, Action<TElement>> CreateEventPost_Dictionary = new Dictionary<TKey, Action<TElement>>();


    //    //. 대신 이런 구조라면, CreateEventPost에 체이닝을 하거나 변경되었을때는, 딕셔너리에 반영이 되나?
    //    //! 되곘지 직접 CreateEventPost를 널체크하고 불러오는거니까
    //    private Action<TElement> GetOrAddCachedDelegate(TKey key)
    //    {
    //        if (CreateEventPost_Dictionary.TryGetValue(key, out var act)) return act;

    //        act = element => CreateEventPost?.Invoke(key, element); //! Key 캡처 – 단, 1회만!
    //        CreateEventPost_Dictionary[key] = act;
    //        return act;
    //    }



    //    /// <summary>
    //    /// 요소를 Push 시킨 이후에 추가로 수행할 이벤트
    //    /// <para><see cref="Pools"/>를 수정하는 이벤트는 절대 사용하면 안됨</para>
    //    /// </summary>
    //    public event Action<TElement> PushEventPre;



    //    /// <summary>
    //    /// 요소를 Release 시킨 이후에 추가로 수행할 이벤트
    //    /// <para><see cref="Pools"/>를 수정하는 이벤트는 절대 사용하면 안됨</para>
    //    /// </summary>
    //    public event Action<TElement> ReleaseEventPost;


    //    public void ExecuteCreateEventPost(TKey key, TElement element)
    //    {
    //        Debug.Log($"<color=green>생성 이벤트 실행! {key} / {element}</color>");
    //        CreateEventPost?.Invoke(key, element);
    //    }


    //    ///======================================================================================================================================================



    //    //? 요소 생성 (Create)



    //    public void Create(TKey key, int createCount)
    //    {
    //        if (Pools.TryGetValue(key, out var pool))
    //        {
    //            //pool.Create(createCount, CreateEventPost);
    //            //pool.Create(createCount, (x) => CreateEventPost?.Invoke(key, x));
    //            //pool.Create(createCount, (x) => ExecuteCreateEventPost(key, x));
    //            pool.Create(createCount, GetOrAddCachedDelegate(key));
    //        }
    //        else
    //        {
    //            //pool = new TInfinityStack();
    //            InitializePool(key, out pool);
    //            //pool.Create(createCount, CreateEventPost);
    //            //pool.Create(createCount, (x) => CreateEventPost?.Invoke(key, x));
    //            //pool.Create(createCount, (x) => ExecuteCreateEventPost(key, x));
    //            pool.Create(createCount, GetOrAddCachedDelegate(key));
    //            Pools.Add(key, pool);
    //        }
    //    }



    //    public async UniTask CreateAsync(TKey key, int createCount)
    //    {
    //        if (Pools.TryGetValue(key, out var pool))
    //        {
    //            //await pool.CreateAsync(createCount, CreateEventPost);
    //            //pool.Create(createCount, (x) => CreateEventPost?.Invoke(key, x));
    //            //pool.Create(createCount, (x) => ExecuteCreateEventPost(key, x));
    //            await pool.CreateAsync(createCount, GetOrAddCachedDelegate(key));
    //        }
    //        else
    //        {
    //            //pool = new TInfinityStack();
    //            InitializePool(key, out pool);
    //            //await pool.CreateAsync(createCount, CreateEventPost);
    //            //await pool.CreateAsync(createCount, (x) => CreateEventPost?.Invoke(key, x));
    //            //await pool.CreateAsync(createCount, (x) => ExecuteCreateEventPost(key, x));
    //            await pool.CreateAsync(createCount, GetOrAddCachedDelegate(key));
    //            Pools.Add(key, pool);
    //        }
    //    }



    //    ///======================================================================================================================================================



    //    //? 요소 넣기 (Push)



    //    public void Push(TKey key, TElement element)
    //    {
    //        if (Pools.TryGetValue(key, out var pool))
    //        {
    //            pool.Push(element);
    //        }
    //        else
    //        {
    //            //pool = new TInfinityStack();
    //            InitializePool(key, out pool);
    //            pool.Push(element);
    //            Pools.Add(key, pool);
    //        }
    //    }



    //    public void PushRange(TKey key, params TElement[] elements)
    //    {
    //        if (Pools.TryGetValue(key, out var pool))
    //        {
    //            pool.PushRange(elements);
    //        }
    //        else
    //        {
    //            //pool = new TInfinityStack();
    //            InitializePool(key, out pool);
    //            pool.PushRange(elements);
    //            Pools.Add(key, pool);
    //        }
    //    }



    //    public void PushRange(TKey key, IList<TElement> elements)
    //    {
    //        if (Pools.TryGetValue(key, out var pool))
    //        {
    //            pool.PushRange(elements);
    //        }
    //        else
    //        {
    //            //pool = new TInfinityStack();
    //            InitializePool(key, out pool);
    //            pool.PushRange(elements);
    //            Pools.Add(key, pool);
    //        }
    //    }



    //    public void PushRange(TKey key, IEnumerable<TElement> elements)
    //    {
    //        if (Pools.TryGetValue(key, out var pool))
    //        {
    //            pool.PushRange(elements);
    //        }
    //        else
    //        {
    //            //pool = new TInfinityStack();
    //            InitializePool(key, out pool);
    //            pool.PushRange(elements);
    //            Pools.Add(key, pool);
    //        }
    //    }



    //    ///======================================================================================================================================================



    //    //? 요소 해제하기 (Release)



    //    public void Release(TKey key, int releaseCount)
    //    {
    //        if (Pools.TryGetValue(key, out var pool))
    //        {
    //            pool.Release(releaseCount);
    //        }
    //    }



    //    public void ReleaseClear(TKey key, bool removePools)
    //    {
    //        if (Pools.TryGetValue(key, out var pool))
    //        {
    //            pool.ReleaseClear();

    //            if (removePools) { Pools.Remove(key); }
    //        }
    //    }



    //    ///======================================================================================================================================================



    //    //? 요소 꺼내기 (Pop)



    //    /// <summary>
    //    /// <typeparamref name="TElement"/>를 꺼낸다
    //    /// <para>풀의 상태와 파라미터에 따라, <b>반환을 무조건 보장하지는 않으며</b>, 이때 null을 반환한다</para>
    //    /// </summary>
    //    /// <param name="autoTryCreateElementsWhenPoolNotEnough">
    //    /// 꺼내야 하는데, 풀의 크기가 모자랄 경우, <see cref="Create(int, Action{TElement})"/>을 한 이후 반환 할 지 여부
    //    /// <para>null을 받아오면, 풀의 크기가 모자랄경우 반환에 실패한다</para>
    //    /// <para>값을 넣더라도, <see cref="MaxElementCount"/>에 의해 반환에 실패 할 수도 있다</para>
    //    /// <para>기본값: 1</para>
    //    /// </param>
    //    /// <returns></returns>
    //    public TElement Pop(TKey key, int? autoTryCreateElementsWhenPoolNotEnough = 1)
    //    {
    //        if (Pools.TryGetValue(key, out var pool))
    //        {
    //            //return pool.Pop(autoTryCreateElementsWhenPoolNotEnough, (x) => ExecuteCreateEventPost(key, x));
    //            return pool.Pop(autoTryCreateElementsWhenPoolNotEnough, GetOrAddCachedDelegate(key));
    //        }
    //        else if (autoTryCreateElementsWhenPoolNotEnough.HasValue && autoTryCreateElementsWhenPoolNotEnough.Value > 0)
    //        {
    //            InitializePool(key, out pool);
    //            pool.Create(autoTryCreateElementsWhenPoolNotEnough.Value, GetOrAddCachedDelegate(key));
    //            Pools.Add(key, pool);
    //            return pool.Pop(1);
    //        }

    //        return null;
    //    }



    //    /// <summary>
    //    /// [비동기] <typeparamref name="TElement"/>를 꺼낸다
    //    /// <para>풀의 상태와 파라미터에 따라, <b>반환을 무조건 보장하지는 않으며</b>, 이때 null을 반환한다</para>
    //    /// </summary>
    //    /// <param name="autoTryCreateElementsWhenPoolNotEnough">
    //    /// 꺼내야 하는데, 풀의 크기가 모자랄 경우, <see cref="Create(int, Action{TElement})"/>을 한 이후 반환 할 지 여부
    //    /// <para>null을 받아오면, 풀의 크기가 모자랄경우 반환에 실패한다</para>
    //    /// <para>값을 넣더라도, <see cref="MaxElementCount"/>에 의해 반환에 실패 할 수도 있다</para>
    //    /// <para>기본값: 1</para>
    //    /// </param>
    //    /// <returns></returns>
    //    public async UniTask<TElement> PopAsync(TKey key, int? autoTryCreateElementsWhenPoolNotEnough = 1)
    //    {
    //        if (Pools.TryGetValue(key, out var pool))
    //        {
    //            return await pool.PopAsync(autoTryCreateElementsWhenPoolNotEnough, GetOrAddCachedDelegate(key));
    //        }
    //        else if (autoTryCreateElementsWhenPoolNotEnough.HasValue && autoTryCreateElementsWhenPoolNotEnough.Value > 0)
    //        {
    //            InitializePool(key, out pool);
    //            await pool.CreateAsync(autoTryCreateElementsWhenPoolNotEnough.Value, GetOrAddCachedDelegate(key));
    //            Pools.Add(key, pool);
    //            return await pool.PopAsync(1);
    //        }

    //        return null;
    //    }



    //    ///======================================================================================================================================================
    //}

    #endregion



    #region 레거시 InfinityStackManager_NamedMonoBehaviour
    ///// <summary>
    ///// <see cref="INamedCreateCount"/>를 가진 객체를  Type 딕셔너리로 무한 스택을 관리하는 매니저
    ///// </summary>
    ///// <typeparam name="TMonoBehaviour"><see cref="INamedCreateCount"/> 를 보유중인 <see cref="MonoBehaviour"/></typeparam>
    //[Serializable]
    //public class InfinityStackManager_NamedMonoBehaviour<TMonoBehaviour> where TMonoBehaviour : MonoBehaviour, INamedCreateCount
    //{
    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// <paramref name="monoBehaviourArray"/>의 각 요소를 기반으로 무한 스택을 생성합니다.
    //    /// <para>각 요소의 <c>ThisName</c>을 Key로 사용하여 <see cref="Pools"/>를 구성합니다.</para>
    //    /// </summary>
    //    /// <param name="monoBehaviourArray">스택을 구성할 기준이 되는 객체들</param>
    //    /// <param name="createEvent">생성 시점에 실행할 추가 액션</param>
    //    public InfinityStackManager_NamedMonoBehaviour(IEnumerable<TMonoBehaviour> monoBehaviourArray, Action<TMonoBehaviour> createEvent = null)
    //    {
    //        Pools = new Dictionary<string, InfinityStack_MonoBehaviour<TMonoBehaviour>>(monoBehaviourArray.Count());


    //        CreatePlusEvent = createEvent;


    //        foreach (var a in monoBehaviourArray)
    //        {
    //            var stack = new InfinityStack_MonoBehaviour<TMonoBehaviour>(a, a.CreateCount, createEvent);
    //            stack.Create(a.CreateCount);
    //            Pools.Add(a.CurrentName, stack);
    //        }
    //    }



    //    ///======================================================================================================================================================



    //    //? 풀 관리



    //    /// <summary>
    //    /// 무한 스택을 각 객체의 이름 (<see cref="IName"/>)을 Key로 사용하여 각각의 무한 스택을 관리하는 풀
    //    /// </summary>
    //    private readonly Dictionary<string, InfinityStack_MonoBehaviour<TMonoBehaviour>> Pools;



    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// <see cref="TMonoBehaviour"/>를 생성 할때 실행할 이벤트
    //    /// </summary>
    //    public Action<TMonoBehaviour> CreatePlusEvent { get; set; } = null;



    //    /// <summary>
    //    /// Push 시점에 실행할 액션
    //    /// </summary>
    //    public Action<TMonoBehaviour> PushPlusAction { get; set; } = null;



    //    /// <summary>
    //    /// 특정 이름의 스택에 현재 몇 개가 있는지 반환합니다.
    //    /// </summary>
    //    /// <param name="name">스택 이름 (ThisName)</param>
    //    /// <param name="value">스택에 있는 객체 수</param>
    //    /// <returns>해당 이름의 스택이 존재하면 true, 없으면 false</returns>
    //    public bool TryGet_Count_Pool(string name, out int value)
    //    {
    //        if (Pools.TryGetValue(name, out var result))
    //        {
    //            value = result.PoolCount;
    //            return true;
    //        }

    //        value = 0;
    //        return false;
    //    }



    //    /// <summary>
    //    /// 특정 이름의 스택이 비어있는지 여부를 반환합니다.
    //    /// </summary>
    //    /// <param name="name">스택 이름 (ThisName)</param>
    //    /// <param name="value">비어있으면 true</param>
    //    /// <returns>해당 이름의 스택이 존재하면 true, 없으면 false</returns>
    //    public bool TryGet_IsEmpty_Pool(string name, out bool value)
    //    {
    //        if (Pools.TryGetValue(name, out var result))
    //        {
    //            value = (result.PoolCount == 0);
    //            return true;
    //        }
    //        value = false;
    //        return false;
    //    }



    //    /// <summary>
    //    /// 내부적으로 스택에 Pop을 요청하여 객체를 가져옵니다.
    //    /// </summary>
    //    private TMonoBehaviour Pop_Pool(string name)
    //    {
    //        if (Pools.TryGetValue(name, out var value))
    //        {
    //            return value.Pop();
    //        }
    //        return null;
    //    }



    //    /// <summary>
    //    /// 특정 이름의 스택에 <paramref name="count"/>만큼 객체를 생성하여 추가합니다.
    //    /// <para>useCreatePlusAction이 true면 <see cref="CreatePlusEvent"/>도 실행됩니다.</para>
    //    /// </summary>
    //    /// <param name="name">스택 이름 (ThisName)</param>
    //    /// <param name="count">생성할 개수</param>
    //    /// <param name="useCreatePlusAction">true면 CreatePlusAction도 실행</param>
    //    /// <param name="postCreateEvent">추가로 실행할 액션</param>
    //    public void Create(string name, int count, Action<TMonoBehaviour> postCreateEvent = null)
    //    {
    //        if (Pools.TryGetValue(name, out var stack))
    //        {
    //            stack.Create(count, x =>
    //            {
    //                CreatePlusEvent?.Invoke(x);
    //                postCreateEvent?.Invoke(x);
    //            });
    //        }
    //    }



    //    /// <summary>
    //    /// 특정 오브젝트를 Push하여 스택에 반환합니다.
    //    /// </summary>
    //    /// <param name="obj">Push할 객체 (ThisName으로 구분)</param>
    //    public void Push(TMonoBehaviour obj)
    //    {
    //        PushPlusAction?.Invoke(obj);
    //        Pools[obj.CurrentName].Push(obj);
    //    }



    //    /// <summary>
    //    /// 특정 이름의 스택에서 객체를 Pop합니다. (비어 있으면 <paramref name="addCount"/>만큼 새로 생성)
    //    /// </summary>
    //    /// <param name="name">스택 이름 (ThisName)</param>
    //    /// <param name="addCount">비어 있을 때 생성할 개수</param>
    //    /// <returns>Pop한 객체</returns>
    //    public TMonoBehaviour Pop(string name, int addCount = 1)
    //    {
    //        if (TryGet_IsEmpty_Pool(name, out var value) && value == true)
    //        {
    //            Create(name, addCount);
    //        }
    //        return Pop_Pool(name);
    //    }

    //    ///======================================================================================================================================================
    //} 
    #endregion



    #region 레거시 InfinityStack_For_Type_ScriptObject

    ///// <summary>
    ///// <typeparamref name="Ttype"/> 목록을 기반으로, <typeparamref name="TObject"/>를 무한 스택으로 관리하는 클래스입니다.
    ///// <para>
    ///// Ttype은 IGetTypeCached, ICreateCount를 구현하며,  
    ///// TObject는 MonoBehaviour &amp; IBaseClassDB&lt;Ttype&gt;를 구현해야 합니다.
    ///// </para>
    ///// </summary>
    ///// <typeparam name="Ttype">DB 역할 (IGetTypeCached, ICreateCount)</typeparam>
    ///// <typeparam name="TObject">MonoBehaviour + IBaseClassDB&lt;Ttype&gt;</typeparam>
    //[Serializable]
    //public class InfinityStack_For_Type_ScriptObject<Ttype, TObject>
    //    where Ttype : class, ICreateCount
    //    where TObject : MonoBehaviour, IBaseClassDB<Ttype>
    //{
    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// 전달받은 <paramref name="typesArray"/>의 각 요소를 기반으로,
    //    /// <typeparamref name="TObject"/> 무한 스택을 생성하여 <see cref="PoolStack"/>에 저장합니다.
    //    /// </summary>
    //    /// <param name="typesArray">Ttype 객체들의 배열</param>
    //    /// <param name="value">복제할 <typeparamref name="TObject"/> (원형)</param>
    //    /// <param name="createAction">생성 시점에 실행할 추가 액션 (Ttype, TObject)</param>
    //    public InfinityStack_For_Type_ScriptObject(IEnumerable<Ttype> typesArray, TObject value, Action<Ttype, TObject> createAction = null)
    //    {
    //        PoolStack = new Dictionary<Type, InfinityStack_MonoBehaviour<TObject>>();

    //        foreach (var a in typesArray)
    //        {
    //            var stack = new InfinityStack_MonoBehaviour<TObject>(
    //                value, a.CreateCount,
    //                x => createAction?.Invoke(a, x)
    //            );
    //            stack.Create(a.CreateCount);
    //            PoolStack.Add(a.GetType(), stack);
    //        }
    //    }



    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// Type 키 -> <see cref="InfinityStack_MonoBehaviour{TObject}"/>
    //    /// </summary>
    //    private readonly Dictionary<Type, InfinityStack_MonoBehaviour<TObject>> PoolStack;



    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// Push 시점에 실행할 액션
    //    /// </summary>
    //    public Action<TObject> PushPlusAction { get; set; } = null;



    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// 지정한 제네릭 타입 <typeparamref name="T"/>이 몇 개나 스택에 있는지 반환합니다.
    //    /// </summary>
    //    /// <typeparam name="T">Ttype 구현체</typeparam>
    //    /// <param name="value">스택에 있는 객체 수</param>
    //    /// <returns>스택 존재 여부</returns>
    //    public bool TryGet_Count_Pool<T>(out int value) where T : Ttype
    //    {
    //        if (PoolStack.TryGetValue(typeof(T), out var result))
    //        {
    //            value = result.PoolCount;
    //            return true;
    //        }

    //        value = 0;
    //        return false;
    //    }



    //    /// <summary>
    //    /// 지정한 제네릭 타입 <typeparamref name="T"/> 스택이 비어있는지 여부를 반환합니다.
    //    /// </summary>
    //    /// <typeparam name="T">Ttype 구현체</typeparam>
    //    /// <param name="value">비어있으면 true</param>
    //    /// <returns>스택 존재 여부</returns>
    //    public bool TryGet_IsEmpty_Pool<T>(out bool value) where T : Ttype
    //    {
    //        if (PoolStack.TryGetValue(typeof(T), out var result))
    //        {
    //            value = (result.PoolCount == 0);
    //            return true;
    //        }
    //        value = false;
    //        return false;
    //    }



    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// 내부적으로 스택에서 Pop을 요청합니다.
    //    /// </summary>
    //    private TObject Pop_Pool<T>() where T : Ttype
    //    {
    //        if (PoolStack.TryGetValue(typeof(T), out var value))
    //        {
    //            return value.Pop();
    //        }
    //        return null;
    //    }



    //    /// <summary>
    //    /// 특정 제네릭 타입 <typeparamref name="T"/> 스택에 <paramref name="count"/>개를 생성하여 추가합니다.
    //    /// </summary>
    //    /// <typeparam name="T">Ttype 구현체</typeparam>
    //    /// <param name="count">생성할 개수</param>
    //    /// <param name="useCreatePlusAction">true면 <see cref="InfinityStackBase{T}.CreateEventPost"/> 실행</param>
    //    /// <param name="plusAction">별도 액션</param>
    //    public void Create<T>(int count, Action<TObject> plusAction = null) where T : Ttype
    //    {
    //        if (PoolStack.TryGetValue(typeof(T), out var stack))
    //        {
    //            stack.Create(count, x =>
    //            {
    //                plusAction?.Invoke(x);
    //            });
    //        }
    //    }



    //    /// <summary>
    //    /// 특정 <paramref name="typeObj"/>에 해당하는 스택에 <paramref name="obj"/>를 Push합니다.
    //    /// </summary>
    //    public void Push(Ttype typeObj, TObject obj)
    //    {
    //        PushPlusAction?.Invoke(obj);
    //        PoolStack[typeObj.GetType()].Push(obj);
    //    }



    //    /// <summary>
    //    /// 특정 제네릭 타입 <typeparamref name="T"/> 스택에 <paramref name="obj"/>를 Push합니다.
    //    /// </summary>
    //    /// <typeparam name="T">Ttype 구현체</typeparam>
    //    /// <param name="obj">Push할 객체</param>
    //    public void Push<T>(TObject obj) where T : Ttype
    //    {
    //        PushPlusAction?.Invoke(obj);
    //        PoolStack[typeof(T)].Push(obj);
    //    }



    //    /// <summary>
    //    /// 특정 제네릭 타입 <typeparamref name="T"/> 스택에서 객체를 Pop합니다.
    //    /// <para>비어 있으면 <paramref name="addCount"/>만큼 새로 생성합니다.</para>
    //    /// </summary>
    //    /// <typeparam name="T">Ttype 구현체</typeparam>
    //    /// <param name="addCount">비어 있을 때 생성할 개수</param>
    //    /// <returns>Pop한 객체</returns>
    //    public TObject Pop<T>(int addCount = 1) where T : Ttype
    //    {
    //        if (TryGet_IsEmpty_Pool<T>(out var value) && value == true)
    //        {
    //            Create<T>(addCount);
    //        }
    //        return Pop_Pool<T>();
    //    }



    //    ///======================================================================================================================================================
    //}

    #endregion



    #region 레거시 InfinityTypeStack_For_Class (그대로 먼저 남기고 제작시작)
    //    /// <summary>
    //    /// <typeparamref name="TClass"/>를 상속받는 모든 클래스(추상 제외)를 Reflection으로 찾아,
    //    /// 각 타입별 <see cref="InfinityStack_ClassInstance{TClass}"/>를 구성하여 관리하는 클래스입니다.
    //    /// </summary>
    //    /// <typeparam name="TClass">ICreateCount를 구현하는 클래스 타입</typeparam>
    //    [Serializable]
    //    public class InfinityTypeStack_For_Class<TClass> where TClass : class, ICreateCount
    //    {
    //        ///======================================================================================================================================================



    //        /// <summary>
    //        /// 생성자.
    //        /// <para>내부적으로 <c>SU_Collection_Types.SetClassArray_Mode1&lt;TClass&gt;</c>를 통해
    //        /// TClass를 상속받는 클래스들을 전부 찾고, 해당 클래스마다 스택을 생성해 <see cref="Stacks"/>에 저장합니다.</para>
    //        /// </summary>
    //        public InfinityTypeStack_For_Class()
    //        {
    //            InstanceFactoryManager = new InstanceFactoryManager<TClass>();

    //            // TClass를 상속받는 (인터페이스, 추상 제외) 모든 클래스를 불러오기
    //            SU_Collection_Types.CreateTypeArray<TClass>(SU_Collection_Types.TypeSearch.ConcreteClasses, out var classes);
    //            Stacks = new Dictionary<Type, InfinityStack_ClassInstance<TClass>>(classes.Length);

    //            for (int i = 0; i < classes.Length; i++)
    //            {
    //                var type = classes[i].GetType();
    //                var stack = new InfinityStack_ClassInstance<TClass>(type, classes[i].CreateCount, InstanceFactoryManager);
    //                stack.Create(classes[i].CreateCount);
    //                Stacks.Add(type, stack);
    //            }
    //        }



    //        ///======================================================================================================================================================



    //        /// <summary>
    //        /// Type 키 -> <see cref="InfinityStack_ClassInstance{TClass}"/> 맵
    //        /// </summary>
    //        private readonly Dictionary<Type, InfinityStack_ClassInstance<TClass>> Stacks;



    //        /// <summary>
    //        /// 스택들을 읽기 전용으로 반환합니다.
    //        /// </summary>
    //        public IReadOnlyDictionary<Type, InfinityStack_ClassInstance<TClass>> GetStacks => Stacks;



    //#if UNITY_EDITOR

    //        /// <summary>
    //        /// 에디터에 띄우기위해 사용하는 리스트 버전의 스택
    //        /// </summary>
    //        [ShowInInspector, DictionaryDrawerSettings(IsReadOnly = true), Sirenix.OdinInspector.ReadOnly, ShowIf("@Stacks!=null")]
    //        [LabelText("풀 스택 딕셔너리 보기")]
    //        public IReadOnlyDictionary<string, InfinityStack_ClassInstance<TClass>> Editor_GetPoolStackConvertedList => Stacks.ToDictionary(kv => kv.Key.Name, kv => kv.Value);

    //#endif


    //        /// <summary>
    //        /// <see cref="TClass"/>들을 생성할때, 더 효율적이게 생성하게 해주는 인스턴스 팩토리 매니저
    //        /// </summary>
    //        private readonly InstanceFactoryManager<TClass> InstanceFactoryManager;



    //        ///======================================================================================================================================================



    //        /// <summary>
    //        /// 특정 제네릭 타입 <typeparamref name="T"/> 스택에서 객체를 Pop합니다.
    //        /// </summary>
    //        /// <typeparam name="T">TClass를 상속하는 구체 클래스 (new() 가능)</typeparam>
    //        /// <returns>Pop한 객체</returns>
    //        public T Pop<T>(int? autoTryCreateElementsWhenPoolNotEnough = 0) where T : class, TClass, new()
    //        {
    //            return Stacks[typeof(T)].Pop(autoTryCreateElementsWhenPoolNotEnough) as T;
    //        }



    //        /// <summary>
    //        /// 특정 TClass 객체(<paramref name="value"/>)를 Push합니다. (런타임 타입으로 찾음)
    //        /// </summary>
    //        /// <param name="value">Push할 객체</param>
    //        public void Push(TClass value)
    //        {
    //            Stacks[value.GetType()].Push(value);
    //        }



    //        /// <summary>
    //        /// 특정 제네릭 타입 <typeparamref name="T"/> 스택에 객체(<paramref name="value"/>)를 Push합니다.
    //        /// </summary>
    //        /// <typeparam name="T">TClass를 상속하는 구체 클래스</typeparam>
    //        /// <param name="value">Push할 객체</param>
    //        public void Push<T>(TClass value) where T : TClass
    //        {
    //            Stacks[typeof(T)].Push(value);
    //        }



    //        ///======================================================================================================================================================



    //        /// <summary>
    //        /// 특정 제네릭 타입 <typeparamref name="T"/> 스택에 저장된 객체 수를 가져옵니다.
    //        /// </summary>
    //        /// <typeparam name="T">TClass를 상속하는 구체 클래스 (new() 가능)</typeparam>
    //        /// <param name="value">스택 내 객체 수</param>
    //        /// <returns>스택 존재 여부</returns>
    //        public bool TryGet_StackCount<T>(out int value) where T : class, TClass, new()
    //        {
    //            if (Stacks.TryGetValue(typeof(T), out var result))
    //            {
    //                value = result.PoolCount;
    //                return true;
    //            }

    //            value = 0;
    //            return false;
    //        }



    //        /// <summary>
    //        /// 특정 <paramref name="key"/>에 해당하는 스택의 객체 수를 가져옵니다.
    //        /// </summary>
    //        /// <param name="key">런타임 Type 키</param>
    //        /// <param name="value">스택 내 객체 수</param>
    //        /// <returns>스택 존재 여부</returns>
    //        public bool TryGet_StackCount(Type key, out int value)
    //        {
    //            if (Stacks.TryGetValue(key, out var result))
    //            {
    //                value = result.PoolCount;
    //                return true;
    //            }

    //            value = 0;
    //            return false;
    //        }



    //        /// <summary>
    //        /// 특정 제네릭 타입 <typeparamref name="T"/> 스택에서 <paramref name="count"/>개를 제거합니다.
    //        /// <para>ifGameObjectDestroy가 true면 제거 시 게임오브젝트도 Destroy</para>
    //        /// </summary>
    //        /// <typeparam name="T">TClass를 상속하는 구체 클래스 (new() 가능)</typeparam>
    //        public int Release<T>(int count) where T : class, TClass, new()
    //        {
    //            return Stacks[typeof(T)].Release(count);
    //        }



    //        /// <summary>
    //        /// 특정 제네릭 타입 <typeparamref name="T"/> 스택을 모두 비웁니다.
    //        /// <para>ifGameObjectDestroy가 true면 제거 시 게임오브젝트도 Destroy</para>
    //        /// </summary>
    //        /// <typeparam name="T">TClass를 상속하는 구체 클래스 (new() 가능)</typeparam>
    //        /// <param name="ifGameObjectDestroy">true면 GameObject/MonoBehaviour를 Destroy</param>
    //        public void ReleaseClear<T>() where T : class, TClass, new()
    //        {
    //            Stacks[typeof(T)].ReleaseClear();
    //        }



    //        /// <summary>
    //        /// 모든 스택을 비웁니다.
    //        /// <para>ifGameObjectDestroy가 true면 제거 시 GameObject/MonoBehaviour를 Destroy</para>
    //        /// </summary>
    //        public void ReleaseClear()
    //        {
    //            foreach (var stack in Stacks)
    //            {
    //                stack.Value.ReleaseClear();
    //            }
    //        }



    //        ///======================================================================================================================================================



    //        public void CheckDuplicate()
    //        {
    //            foreach (var stack in Stacks)
    //            {
    //                var duplicates = stack.Value.GetPoolStack.GroupBy(x => x?.GetType())
    //                                  .Where(g => g.Count() > 1)
    //                                  .Select(g => g.Key?.Name)
    //                                  .ToArray();

    //                if (duplicates.Length > 0)
    //                {
    //                    throw new InvalidOperationException(
    //                        $"중복된 타입이 존재합니다: {string.Join(", ", duplicates)}");
    //                }
    //            }
    //        }



    //        ///======================================================================================================================================================
    //    } 
    #endregion

    #endregion



    ///======================================================================================================================================================
}