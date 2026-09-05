using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using SitraUtils;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Jobs;
using UnityEngine;



//? 무한스택이 정리되어있는 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    //? 무한 스택 베이스



    /// <summary>
    /// 모든 무한 스택의 공통 베이스 클래스입니다.
    /// <para>클래스 타입 <typeparamref name="TElement"/>를 스택으로 관리하되, 부족하면 자동으로 새 객체를 생성합니다.</para>
    /// </summary>
    /// <typeparam name="TElement">클래스 타입(참조 타입)으로서 스택에 저장될 대상</typeparam>
    [Serializable]
    [HideReferenceObjectPicker]
    public abstract class InfinityStackBase<TElement> : IDisposable
        where TElement : class
    {
        ///======================================================================================================================================================



        ///<summary>
        /// 무한 스택 생성 (베이스)
        /// </summary>
        /// <param name="poolCapacity">풀의 초기 용량</param>
        /// <param name="createEventPost">생성 할 때 마다 실행할 이벤트 지정</param>
        /// <param name="debugPushDuplicate">풀에 중복 요소를 추가시, 이를 디버깅 할지 여부</param>
        /// <param name="useMaxElementCount">최대 요소 개수를 사용 할 지 여부 </param>
        public InfinityStackBase(int poolCapacity, Action<TElement> createEventPost = null, bool debugPushDuplicate = true, int? useMaxElementCount = null)
        {
            poolStack = new Stack<TElement>(poolCapacity);
            inPool = new HashSet<TElement>(poolCapacity);
            inUse = new HashSet<TElement>(poolCapacity);
            CreateEventPost += createEventPost;
            Debug_PushDuplicate = debugPushDuplicate;
            MaxElementCount = useMaxElementCount ?? int.MaxValue;
        }



        ///======================================================================================================================================================



        //? 요소들을 관리하는 풀



        /// <summary>
        /// 풀링된 요소들을 저장하는 풀 (스택으로 구현)
        /// </summary>
        private readonly Stack<TElement> poolStack;



        private Stack<TElement> GetPoolStackInternal => poolStack;



        /// <summary>
        /// 풀링된 요소들의 중복을 방지하기 위한 해시셋
        /// </summary>
        private readonly HashSet<TElement> inPool;



        private HashSet<TElement> GetInPoolInternal => inPool;



        /// <summary>
        /// Pop된 요소들을 대여하기 위한 해시셋
        /// </summary>
        private readonly HashSet<TElement> inUse;



        /// <summary>
        /// <see cref="GetPoolStackInternal"/>을 읽기 전용으로 얻어오기
        /// </summary>
        public IReadOnlyCollection<TElement> GetPoolStack
        {
            get
            {
#if UNITY_EDITOR
                EditorRefresh_cachedPoolStack();
#endif
                return GetPoolStackInternal;
            }
        }



        [FoldoutGroup("무한 스택")]
        [FoldoutGroup("무한 스택/요소 개수 정보")]
        [LabelText("누적된 총 생성된 요소"), DisplayAsString]
        [ShowInInspector]
        [ReadOnly]
        [PropertyOrder(0)]
        [LabelWidth(120)]
        [EnableGUI]
        public int CreatedElementsCount_Accumulate { get; private set; } = 0;



        [FoldoutGroup("무한 스택")]
        [FoldoutGroup("무한 스택/요소 개수 정보")]
        [LabelText("누적된 총 해제된 요소"), DisplayAsString]
        [ShowInInspector]
        [ReadOnly]
        [PropertyOrder(1)]
        [LabelWidth(120)]
        [EnableGUI]
        public int ReleasedElementsCount_Accumulate { get; private set; } = 0;



        [FoldoutGroup("무한 스택")]
        [FoldoutGroup("무한 스택/요소 개수 정보")]
        [LabelText("현재 가용중인 요소"), DisplayAsString]
        [ShowInInspector]
        [ReadOnly]
        [PropertyOrder(3)]
        [LabelWidth(120)]
        [EnableGUI]
        public int CurrentElementsCount => GetPoolStackInternal.Count + inUse.Count;



        /// <summary>
        /// 풀 크기 구하기
        /// </summary>
        [FoldoutGroup("무한 스택")]
        [TabGroup("무한 스택/탭", "풀 스택")]
        [LabelText("풀 스택 크기")]
        [ShowInInspector, ReadOnly, DisplayAsString, ShowIf(nameof(IsValid_PoolStack))]
        [PropertyOrder(4)]
        [LabelWidth(100)]
        [EnableGUI]
        public int PoolCount
        {
            get
            {
#if UNITY_EDITOR
                EditorRefresh_cachedPoolStack();
#endif
                return GetPoolStackInternal?.Count ?? 0;
            }
        }



        /// <summary>
        /// 풀이 비어있는지 확인하기
        /// </summary>
        public bool PoolIsEmpty => GetPoolStackInternal.Count == 0;



        [FoldoutGroup("무한 스택")]
        [TabGroup("무한 스택/탭", "대여 목록")]
        [LabelText("대여중인 요소")]
        [ShowInInspector, DisplayAsString, ShowIf(nameof(IsValid_PoolStack))]
        [PropertyOrder(5)]
        [LabelWidth(100)]
        [EnableGUI]
        public int InUseCount
        {
            get
            {
#if UNITY_EDITOR
                EditorRefresh_cachedPoolStack();
#endif
                return inUse?.Count ?? 0;
            }
        }



        private bool IsValid_PoolStack => poolStack != null;




#if UNITY_EDITOR



        ///======================================================================================================================================================



        //? 런타임 컬렉션 접근과 에디터 표시용 스냅샷 갱신을 분리하여, 풀 연산 중 전체 복사가 발생하지 않게 한다



        private bool editorCacheDirty = true;



        [OnInspectorGUI, PropertyOrder(-9999)]
        private void EditorRefreshCacheWhenDrawn()
        {
            EditorRefresh_cachedPoolStack();
        }



        //? 풀 스택을 에디터에 띄우기


        [FoldoutGroup("무한 스택")]
        [TabGroup("무한 스택/탭", "풀 스택"), FoldoutGroup("무한 스택/탭/풀 스택/풀 스택 보기")]
        [ShowInInspector, ListDrawerSettings(IsReadOnly = true, ShowFoldout = false), ShowIf(nameof(IsValid_PoolStack))]
        [LabelText("풀 스택")]
        [PropertyOrder(6)]
        [HideReferenceObjectPicker]
        public List<TElement> editorCachedPoolStack = null;



        private void EditorRefresh_cachedPoolStack()
        {
            if (!editorCacheDirty) { return; }

            EditorRefresh_cachedInUse();

            if (poolStack == null)
            {
                editorCachedPoolStack = null;
                editorCacheDirty = false;
                return;
            }

            editorCachedPoolStack ??= new List<TElement>(poolStack.Count);

            editorCachedPoolStack.Clear();
            foreach (var element in poolStack) { editorCachedPoolStack.Add(element); }
            editorCachedPoolStack.Reverse();
            editorCacheDirty = false;
        }



        ///======================================================================================================================================================



        //? 대여 목록을 에디터에 띄우기



        private bool Editor_IsValid_InUse => inUse != null;



        [FoldoutGroup("무한 스택")]
        [TabGroup("무한 스택/탭", "대여 목록"), FoldoutGroup("무한 스택/탭/대여 목록/대여중인 요소 목록 보기", Order = 2)]
        [ShowInInspector, ListDrawerSettings(IsReadOnly = true, ShowFoldout = false), ShowIf(nameof(Editor_IsValid_InUse))]
        [LabelText("대여중인 요소 목록")]
        [PropertyOrder(7)]
        [HideReferenceObjectPicker]
        public List<TElement> editorCache_InUseList = null;



        private void EditorRefresh_cachedInUse()
        {
            if (inUse == null)
            {
                editorCache_InUseList = null;
                return;
            }

            editorCache_InUseList ??= new List<TElement>(inUse.Count);

            editorCache_InUseList.Clear();
            editorCache_InUseList.AddRange(inUse);
        }



        //! 리스트를 접을수없게하고, 그걸 한번 더 FoldoutGroup로 감싸는 이유는,
        //! getter로 인스펙터에 그리는 것이기에 에디터에서 펼치고 접힌 상태가 매번 갱신되어서 저장되지않음



        ///======================================================================================================================================================

#endif



        /// <summary>
        /// 에디터 표시용 풀 스냅샷을 다음 조회 시점에 한 번만 갱신하도록 표시합니다.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void MarkEditorCacheDirty()
        {
#if UNITY_EDITOR
            editorCacheDirty = true;
#endif
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 스택과 대여 상태를 함께 변경하는 구간을 보호합니다.
        /// <para>공개 메서드 전체의 스레드 안전성을 보장하는 잠금은 아닙니다.</para>
        /// </summary>
        protected readonly object lock_Pool = new object();



        ///======================================================================================================================================================



        //? 풀 관련 확장 필드



        ///<summary>
        ///최대 요소 개수
        ///<para>이 개수 이상으로 요소들이 생성되지않는다, 기본값은 <see cref="int.MaxValue"/></para>
        ///<para><see cref="GetPoolStackInternal"/>의 최대 크기를 제약하는 것이 아닌, 생성된 모든 요소들을 기준으로 계산된다</para>
        ///</summary>
        [FoldoutGroup("무한 스택")]
        [FoldoutGroup("무한 스택/요소 개수 정보")]
        [LabelText("최대 생성 가능 요소")]
        [PropertyOrder(3)]
        [LabelWidth(125)]
        public int MaxElementCount;



        /// <summary>
        /// 요소가 생성 된 이후에 추가로 수행할 이벤트
        /// <para><see cref="GetPoolStackInternal"/>를 수정하는 이벤트는 절대 사용하면 안됨</para>
        /// </summary>
        public event Action<TElement> CreateEventPost;



        /// <summary>
        /// 요소를 Push 하기 직전에 추가로 수행할 이벤트
        /// <para><see cref="GetPoolStackInternal"/>를 수정하는 이벤트는 절대 사용하면 안됨</para>
        /// </summary>
        public event Action<TElement> PushEventPre;



        /// <summary>
        /// 요소를 Release 시킨 이후에 추가로 수행할 이벤트
        /// <para><see cref="GetPoolStackInternal"/>를 수정하는 이벤트는 절대 사용하면 안됨</para>
        /// </summary>
        public event Action<TElement> ReleaseEventPost;



        /// <summary>
        /// 요소가 생성될때 추가로 수행할 이벤트 실행
        /// <para><see cref="CreateInternal"/> 에서만 실행</para>
        /// </summary>
        /// <param name="target"></param>
        protected void ExecuteCreateEvent(TElement target)
        {
            CreateEventPost?.Invoke(target);
        }



        ///======================================================================================================================================================



        //? 디버깅



        ///<summary>
        /// Push에서 중복을 발견했을때, 디버깅 여부
        /// </summary>
        [FoldoutGroup("무한 스택"), BoxGroup("무한 스택/디버깅")]
        [LabelText("중복 Push 디버깅")]
        [PropertyOrder(7)]
        [LabelWidth(120)]
        public bool Debug_PushDuplicate;



        ///======================================================================================================================================================



        //? 요소 생성 (Create)



        /// <summary>
        /// <typeparamref name="TElement"/>를 <paramref name="createCount"/> 만큼 생성한다
        /// <para><see cref="MaxElementCount"/> 를 넘으면 반드시 생성을 보장하지 않으며,</para>
        /// <para>그렇게 <typeparamref name="TElement"/>를 생성된 개수를 반환한다 (int), 하나도 생성하지 못하면 0을 반환한다</para>
        /// <para>내부에서 <see cref="CreateInternal(int, Action{TElement})"/>을 재정의하여 그 곳에서 각 <typeparamref name="TElement"/>에 맞게 생성한다</para>
        /// </summary>
        /// <param name="createCount"><typeparamref name="TElement"/>를 생성할 개수</param>
        /// <param name="postCreateEvent"><see cref="CreateEventPost"/>와 더불어 또 추가로 실행할 </param>
        /// <returns></returns>
        public int Create(int createCount, Action<TElement> postCreateEvent = null)
        {
            //! 애초에 0개(?)를 받아왔다면 0개 생성
            if (createCount == 0) { return 0; }


            //. 최대 요소 개수와, 가용 가능한 요소 개수를 계산하여, createCount를 조정한다
            int actualCount = Mathf.Min(createCount, Mathf.Max(0, MaxElementCount - CurrentElementsCount));


            //! 조정된 개수가 0개라면 0개 생성
            if (actualCount == 0) { return 0; }


            //. 내부 생성 메서드를 실행하여 요소들을 생성한다
            CreateInternal(actualCount, postCreateEvent);


            //. 내부 생성 메서드가 actualCount만큼 생성 하는 것을 보장하기에, 그만큼 더해 생성된 요소 개수를 계산한다
            CreatedElementsCount_Accumulate += actualCount;


            //. 생성된 요소 개수를 반환한다
            return actualCount;
        }



        /// <summary>
        /// 이 메서드를 재정의하여, <typeparamref name="TElement"/>를 생성한다
        /// <para>반드시 <paramref name="createCount"/>만큼만 생성 해야 한다 (<see cref="CreatedElementsCount_Accumulate"/> 기록 위함)</para>
        /// </summary>
        /// <param name="createCount"><typeparamref name="TElement"/>를 생성할 개수</param>
        /// <param name="postCreateEvent"><see cref="CreateEventPost"/>와 더불어 또 추가로 실행할 </param>
        protected abstract void CreateInternal(int createCount, Action<TElement> postCreateEvent = null);



        /// <summary>
        /// [비동기] <typeparamref name="TElement"/>를 <paramref name="createCount"/> 만큼 생성한다
        /// <para><see cref="MaxElementCount"/> 를 넘으면 반드시 생성을 보장하지 않으며,</para>
        /// <para>그렇게 <typeparamref name="TElement"/>를 생성된 개수를 반환한다 (int), 하나도 생성하지 못하면 0을 반환한다</para>
        /// <para>내부에서 <see cref="CreateInternal(int, Action{TElement})"/>을 재정의하여 그 곳에서 각 <typeparamref name="TElement"/>에 맞게 생성한다</para>
        /// </summary>
        /// <param name="createCount"><typeparamref name="TElement"/>를 생성할 개수</param>
        /// <param name="postCreateEvent"><see cref="CreateEventPost"/>와 더불어 또 추가로 실행할 </param>
        /// <returns></returns>
        public async UniTask<int> CreateAsync(int createCount, Action<TElement> postCreateEvent = null)
        {
            //! 애초에 0개(?)를 받아왔다면 0개 생성
            if (createCount == 0) { return 0; }


            //. 최대 요소 개수와, 생성 개수를 계산하여, createCount를 조정한다
            int actualCount = Mathf.Min(createCount, Mathf.Max(0, MaxElementCount - CreatedElementsCount_Accumulate));


            //! 조정된 개수가 0개라면 0개 생성
            if (actualCount == 0) { return 0; }


            //. 내부 생성 메서드를 실행하여 요소들을 생성한다
            await CreateInternalAsync(actualCount, postCreateEvent);


            //. 내부 생성 메서드가 actualCount만큼 생성 하는 것을 보장하기에, 그만큼 더해 생성된 요소 개수를 계산한다
            CreatedElementsCount_Accumulate += actualCount;


            //. 생성된 요소 개수를 반환한다
            return actualCount;
        }



        /// <summary>
        /// [비동기] 이 메서드를 재정의하여, <typeparamref name="TElement"/>를 생성한다
        /// <para>반드시 <paramref name="createCount"/>만큼만 생성 해야 한다 (<see cref="CreatedElementsCount_Accumulate"/> 기록 위함)</para>
        /// </summary>
        /// <param name="createCount"><typeparamref name="TElement"/>를 생성할 개수</param>
        /// <param name="postCreateEvent"><see cref="CreateEventPost"/>와 더불어 또 추가로 실행할 </param>
        public abstract UniTask CreateInternalAsync(int createCount, Action<TElement> postCreateEvent = null);



        ///======================================================================================================================================================



        //? 요소 넣기 (Push)



        /// <summary>
        /// <typeparamref name="TElement"/>를 넣는다 (반환한다)
        /// <para><see cref="GetInPoolInternal"/>에서 중복 검사를 하여, 이미 추가된 <typeparamref name="TElement"/> 일 경우 false를 반환한다</para>
        /// <para>디버그 모드에 따라 이를 메세지로 출력 하기도 한다</para>
        /// </summary>
        /// <param name="element">넣을(반환할) 요소</param>
        public bool Push(TElement element)
        {
            //. 요소가 이미 풀에 추가되었을경우, 동일한 객체를 또 추가하면 안되니 실패한다
            if (GetInPoolInternal.Contains(element))
            {
                if (Debug_PushDuplicate) { Debug.LogError($"{typeof(TElement)} 무한스택 Push 과정에서 중복 Push 발견, Push 실패"); }
                return false;
            }


            //. 넣기 전 이벤트 실행
            PushEventPre?.Invoke(element);


            lock (lock_Pool)
            {
                //. 풀과 해시셋에 요소를 추가한다
                GetPoolStackInternal.Push(element);
                GetInPoolInternal.Add(element);
                inUse.Remove(element); //. 대여 딕셔너리에서 제거
                MarkEditorCacheDirty();
            }


            //. 넣기 성공
            return true;
        }



        /// <summary>
        /// <typeparamref name="TElement"/>들을 "배열"로 넣는다 (반환한다)
        /// </summary>
        /// <param name="elements">넣을(반환할) 요소 들의 배열</param>
        public void PushRange(params TElement[] elements)
        {
            for (int i = 0; i < elements.Length; i++) { Push(elements[i]); }
        }



        /// <summary>
        /// <typeparamref name="TElement"/>들을 "리스트"로 넣는다 (반환한다)
        /// </summary>
        /// <param name="elements">넣을(반환할) 요소 들의 배열</param>
        public void PushRange(IList<TElement> elements)
        {
            for (int i = 0; i < elements.Count; i++) { Push(elements[i]); }
        }



        /// <summary>
        /// <typeparamref name="TElement"/>들을 "IEnumberable"로 넣는다 (반환한다)
        /// </summary>
        /// <param name="elements">넣을(반환할) 요소 들의 배열</param>
        public void PushRange(IEnumerable<TElement> elements)
        {
            foreach (var element in elements) { Push(element); }
        }



        ///======================================================================================================================================================



        //? 요소 해제하기 (Release)



        /// <summary>
        /// <see cref="GetPoolStackInternal"/>에 생성한 <typeparamref name="TElement"/>들을 해제(제거)하며
        /// <para>해제에 성공한 개수를 반환한다</para>
        /// <para>정말 더이상 사용 하지 않을때, 메모리를 확보 하기 위해 사용한다</para>
        /// </summary>
        /// <param name="releaseCount">해제할 <typeparamref name="TElement"/>의 개수</param>
        public int Release(int releaseCount)
        {
            //. 총 해제된 개수 (0 부터 시작)
            int relasedCount = 0;


            //. 해제할 개수 만큼 순회하면서 풀에서부터 해제한다
            for (int i = 0; i < releaseCount; i++)
            {
                //! 이미 풀에서 더 해제할 것이 없다면, 순회가 종료된다
                if (GetPoolStackInternal.Count == 0) { break; }


                //. 풀에서 객체를 꺼낸다
                var elementReleased = GetPoolStackInternal.Pop();
                GetInPoolInternal.Remove(elementReleased); //. 해시셋에서도 제거한다
                MarkEditorCacheDirty();


                //. 해제 이벤트를 실행한다
                ReleaseEventPost?.Invoke(elementReleased);
                CurrentReleaseEventExtend(elementReleased);

                //. 총 해제된 개수를 더한다
                relasedCount++;
            }


            //. 해제된 개수 기록
            ReleasedElementsCount_Accumulate += relasedCount;


            //. 총 해제된 개수를 반환한다
            return relasedCount;
        }



        /// <summary>
        /// <see cref="GetPoolStackInternal"/>에 생성한 모든<typeparamref name="TElement"/>들을 해제(제거)한다
        /// <para>정말 더이상 사용 하지 않을때, 메모리를 확보 하기 위해 사용한다</para>
        /// </summary>
        public void ReleaseClear()
        {
            Release(GetPoolStackInternal.Count);


            //. 이론상 전부 비워져야하지만, 모두 해제되지 않았을경우, 에러 메시지를 뱉고 직접 Clear
            if (GetPoolStackInternal.Count > 0)
            {
                Debug.LogError($"{typeof(TElement)} 무한스택 ReleaseClear 과정에서 전부 비우기 실패 (poolStack), 수동으로 Clear 실행");
                GetPoolStackInternal.Clear();
                MarkEditorCacheDirty();
            }
            if (GetInPoolInternal.Count > 0)
            {
                Debug.LogError($"{typeof(TElement)} 무한스택 ReleaseClear 과정에서 전부 비우기 실패 (inPool), 수동으로 Clear 실행");
                GetInPoolInternal.Clear();
                MarkEditorCacheDirty();
            }
        }



        /// <summary>
        /// 이 메서드를 재정의하여, <see cref="Release(int)"/> 될때 추가로 해제 이벤트를 실행 할 수 있다
        /// </summary>
        /// <param name="element"></param>
        protected virtual void CurrentReleaseEventExtend(TElement element)
        {

        }



        ///======================================================================================================================================================



        //? 요소 꺼내기 (Pop)



        /// <summary>
        /// <typeparamref name="TElement"/>를 꺼낸다
        /// <para>풀의 상태와 파라미터에 따라, <b>반환을 무조건 보장하지는 않으며</b>, 이때 null을 반환한다</para>
        /// </summary>
        /// <param name="autoTryCreateElementsWhenPoolNotEnough">
        /// 꺼내야 하는데, 풀의 크기가 모자랄 경우, <see cref="Create(int, Action{TElement})"/>을 한 이후 반환 할 지 여부
        /// <para>null을 받아오면, 풀의 크기가 모자랄경우 반환에 실패한다</para>
        /// <para>값을 넣더라도, <see cref="MaxElementCount"/>에 의해 반환에 실패 할 수도 있다</para>
        /// <para>기본값: 1</para>
        /// </param>
        /// <returns></returns>
        public TElement Pop(int? autoTryCreateElementsWhenPoolNotEnough = 1, Action<TElement> postCreateEvent = null)
        {
            //? 꺼내야 하는데, 풀의 크기가 모자랄경우
            if (GetPoolStackInternal.Count == 0)
            {
                //? 풀의 크기가 모자랄때 자동 Create 되게끔 파라미터를 받아왔, 그 크기가 0보다 크다면, 그만큼 Create 한다
                if (autoTryCreateElementsWhenPoolNotEnough.HasValue && autoTryCreateElementsWhenPoolNotEnough.Value > 0)
                {
                    Create(autoTryCreateElementsWhenPoolNotEnough.Value, postCreateEvent);
                }
                //! 그렇지 않으면 Pop에 실패한다, null 반환
                else
                {
                    return null;
                }

            }


            lock (lock_Pool)
            {
                //? 풀에서 하나를 꺼내고, 반환한다
                var pop = GetPoolStackInternal.Pop();
                GetInPoolInternal.Remove(pop); //. 풀에서 꺼냈으므로 중복 방지에서도 제거한다
                inUse.Add(pop); //. 대여 딕셔너리에 추가
                MarkEditorCacheDirty();
                return pop;
            }
        }



        /// <summary>
        /// [비동기] <typeparamref name="TElement"/>를 꺼낸다
        /// <para>풀의 상태와 파라미터에 따라, <b>반환을 무조건 보장하지는 않으며</b>, 이때 null을 반환한다</para>
        /// </summary>
        /// <param name="autoTryCreateElementsWhenPoolNotEnough">
        /// 꺼내야 하는데, 풀의 크기가 모자랄 경우, <see cref="Create(int, Action{TElement})"/>을 한 이후 반환 할 지 여부
        /// <para>null을 받아오면, 풀의 크기가 모자랄경우 반환에 실패한다</para>
        /// <para>값을 넣더라도, <see cref="MaxElementCount"/>에 의해 반환에 실패 할 수도 있다</para>
        /// <para>기본값: 1</para>
        /// </param>
        /// <returns></returns>
        public async UniTask<TElement> PopAsync(int? autoTryCreateElementsWhenPoolNotEnough = 1, Action<TElement> postCreateEvent = null)
        {
            //? 꺼내야 하는데, 풀의 크기가 모자랄경우
            if (GetPoolStackInternal.Count == 0)
            {
                //? 풀의 크기가 모자랄때 자동 Create 되게끔 파라미터를 받아왔, 그 크기가 0보다 크다면, 그만큼 Create 한다
                if (autoTryCreateElementsWhenPoolNotEnough.HasValue && autoTryCreateElementsWhenPoolNotEnough.Value > 0)
                {
                    await CreateAsync(autoTryCreateElementsWhenPoolNotEnough.Value, postCreateEvent);
                }
                //! 그렇지 않으면 Pop에 실패한다, null 반환
                else
                {
                    return null;
                }
            }


            lock (lock_Pool)
            {
                //? 풀에서 하나를 꺼내고, 반환한다
                var pop = GetPoolStackInternal.Pop();
                GetInPoolInternal.Remove(pop); //. 풀에서 꺼냈으므로 중복 방지에서도 제거한다
                inUse.Add(pop); //. 대여 딕셔너리에 추가
                MarkEditorCacheDirty();
                return pop;
            }
        }



        ///======================================================================================================================================================



#if UNITY_EDITOR

        [FoldoutGroup("무한 스택")]
        [FoldoutGroup("무한 스택/요소 테스트")]
        [HorizontalGroup("무한 스택/요소 테스트/캐싱가로그룹")]
        [LabelText("캐싱 개수")]
        [ShowInInspector]
        [PropertyOrder(8)]
        private int Editor_CachingTestCount;

        [FoldoutGroup("무한 스택")]
        [FoldoutGroup("무한 스택/요소 테스트")]
        [HorizontalGroup("무한 스택/요소 테스트/캐싱가로그룹")]
        [PropertyOrder(9)]
        [Button("캐싱 해보기 (Create 호출)", Icon = SdfIconType.ExclamationSquareFill), GUIColor(0.93f, 0.33f, 0.40f)]
        private void Editor_ExecuteCaching() => Create(Editor_CachingTestCount);


        [FoldoutGroup("무한 스택")]
        [FoldoutGroup("무한 스택/요소 테스트")]
        [HorizontalGroup("무한 스택/요소 테스트/해제가로그룹")]
        [LabelText("해제 개수")]
        [ShowInInspector]
        [PropertyOrder(10)]
        private int Editor_ReleaseTestCount;

        [FoldoutGroup("무한 스택")]
        [FoldoutGroup("무한 스택/요소 테스트")]
        [HorizontalGroup("무한 스택/요소 테스트/해제가로그룹")]
        [PropertyOrder(11)]
        [Button("해제 해보기 (Release 호출)", Icon = SdfIconType.ExclamationSquareFill), GUIColor(0.93f, 0.33f, 0.40f)]
        private void Editor_ExecuteRelease() => Release(Editor_ReleaseTestCount);

#endif



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
            ReleaseClear();
            GetPoolStackInternal.Clear();
            GetInPoolInternal.Clear();

            CreateEventPost = null;
            PushEventPre = null;
            ReleaseEventPost = null;
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================



    //? 무한 스택



    /// <summary>
    /// 기본 new()를 통해 생성 가능한 클래스 타입 <typeparamref name="TClass"/>를 무한 스택으로 관리합니다.
    /// </summary>
    /// <typeparam name="TClass">new()가 가능한 클래스 타입</typeparam>
    [Serializable]
    [HideReferenceObjectPicker]
    public class InfinityStack_ClassNew<TClass> : InfinityStackBase<TClass> where TClass : class, new()
    {
        ///======================================================================================================================================================



        ///<summary>
        /// 무한 스택 생성 (클래스 New)
        /// </summary>
        /// <param name="poolCapacity">풀의 초기 용량</param>
        /// <param name="createEventPost">생성자에서 바로 생성할 요소의 개수</param>
        /// <param name="createEventPost">생성 할 때 마다 실행할 이벤트 지정</param>
        /// <param name="debugPushDuplicate">풀에 중복 요소를 추가시, 이를 디버깅 할지 여부</param>
        /// <param name="useMaxElementCount">최대 요소 개수를 사용 할 지 여부 </param>
        public InfinityStack_ClassNew(int poolCapacity, Action<TClass> createEventPost = null, bool debugPushDuplicate = true, int? useMaxElementCount = null) : base(poolCapacity, createEventPost, debugPushDuplicate, useMaxElementCount)
        {

        }



        ///======================================================================================================================================================



        /// <inheritdoc/>
        protected override void CreateInternal(int createCount, Action<TClass> postCreateEvent = null)
        {
            for (int i = 0; i < createCount; i++)
            {
                TClass element = new TClass();
                ExecuteCreateEvent(element);
                postCreateEvent?.Invoke(element);
                Push(element);
            }
        }



        /// <inheritdoc/>
        public async override UniTask CreateInternalAsync(int createCount, Action<TClass> postCreateEvent = null)
        {
            CreateInternal(createCount, postCreateEvent);
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// Activator를 통해 <typeparamref name="TBaseClass"/> 인스턴스를 생성하는 무한 스택입니다.
    /// </summary>
    /// <typeparam name="TBaseClass">클래스 타입. new() 제약은 없으며 Type으로 동적 생성</typeparam>
    [Serializable]
    [HideReferenceObjectPicker]
    public class InfinityStack_ClassInstance<TBaseClass> : InfinityStackBase<TBaseClass> where TBaseClass : class
    {
        ///======================================================================================================================================================



        ///<summary>
        /// 무한 스택 생성 (클래스 Instance)
        /// </summary>
        /// <param name="elementType">클래스를 생성할때, 이 <see cref="Type"/>을 기준으로 생성한다</param>
        /// <param name="createCount">생성자에서 생성할 개수</param>
        /// <param name="poolCapacity">풀의 초기 용량</param>
        /// <param name="instanceFactoryManager">참조할 인스턴스 팩토리 매니저 (받아오지 않으면 내부에서 새로 생성함)</param>
        /// <param name="createEventPost">생성자에서 바로 생성할 요소의 개수</param>
        /// <param name="createEventPost">생성 할 때 마다 실행할 이벤트 지정</param>
        /// <param name="debugPushDuplicate">풀에 중복 요소를 추가시, 이를 디버깅 할지 여부</param>
        /// <param name="useMaxElementCount">최대 요소 개수를 사용 할 지 여부 </param>
        public InfinityStack_ClassInstance(Type elementType, int poolCapacity, InstanceFactoryManager<TBaseClass> instanceFactoryManager, Action<TBaseClass> createEventPost = null, bool debugPushDuplicate = true, int? useMaxElementCount = null) : base(poolCapacity, createEventPost, debugPushDuplicate, useMaxElementCount)
        {
            ElementType = elementType;
            InstanceFactoryManager = instanceFactoryManager ?? new InstanceFactoryManager<TBaseClass>();
        }



        ///======================================================================================================================================================



        ///<summary>
        /// 요소의 타입, 이 타입을 기준으로 <see cref="CreateInternal(int, Action{TBaseClass})"/> 에서 클래스를 동적으로 생성한다
        /// </summary>
        private readonly Type ElementType;



        /// <summary>
        /// <see cref="TClass"/>들을 생성할때, 더 효율적이게 생성하게 해주는 인스턴스 팩토리 매니저
        /// </summary>
        private InstanceFactoryManager<TBaseClass> InstanceFactoryManager;



        /// <inheritdoc/>
        protected override void CreateInternal(int createCount, Action<TBaseClass> postCreateEvent = null)
        {
            for (int i = 0; i < createCount; i++)
            {
                TBaseClass element = InstanceFactoryManager.CreateInstance(ElementType);
                ExecuteCreateEvent(element);
                postCreateEvent?.Invoke(element);
                Push(element);
            }
        }



        /// <inheritdoc/>
        public async override UniTask CreateInternalAsync(int createCount, Action<TBaseClass> postCreateEvent = null)
        {
            CreateInternal(createCount, postCreateEvent);
        }



        ///======================================================================================================================================================



        //? Dispose 패턴



        protected override void DisposeInternal()
        {
            InstanceFactoryManager = null;
            base.DisposeInternal();
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// <see cref="MonoBehaviour"/>를 무한 스택으로 관리하는 클래스입니다.
    /// </summary>
    /// <typeparam name="TMono">MonoBehaviour를 상속받는 타입</typeparam>
    [Serializable]
    [HideReferenceObjectPicker]
    public class InfinityStack_MonoBehaviour<TMono> : InfinityStackBase<TMono> where TMono : MonoBehaviour
    {
        ///======================================================================================================================================================



        ///<summary>
        /// 무한 스택 생성 (스크립터블 오브젝트)
        /// </summary>
        /// <param name="targetMono">이 오브젝트를 기준으로 Instance하여 생성한다</param>
        /// <param name="createCount">생성자에서 생성할 개수</param>
        /// <param name="poolCapacity">풀의 초기 용량</param>
        /// <param name="createEventPost">생성자에서 바로 생성할 요소의 개수</param>
        /// <param name="createEventPost">생성 할 때 마다 실행할 이벤트 지정</param>
        /// <param name="debugPushDuplicate">풀에 중복 요소를 추가시, 이를 디버깅 할지 여부</param>
        /// <param name="useMaxElementCount">최대 요소 개수를 사용 할 지 여부 </param>
        public InfinityStack_MonoBehaviour(TMono targetMono, int poolCapacity, Action<TMono> createEventPost = null, bool debugPushDuplicate = true, int? useMaxElementCount = null) : base(poolCapacity, createEventPost, debugPushDuplicate, useMaxElementCount)
        {
            TargetMono = targetMono;
        }



        ///======================================================================================================================================================



        ///<summary>
        ///<see cref="CreateInternal(int, Action{TMono})"/> 에서 이 객체를 가지고 생성한다
        /// </summary>
        private readonly TMono TargetMono;



        ///======================================================================================================================================================



        /// <inheritdoc/>
        protected override void CreateInternal(int createCount, Action<TMono> postCreateEvent = null)
        {
            for (int i = 0; i < createCount; i++)
            {
                TMono instancedMono = GameObject.Instantiate<TMono>(TargetMono);
                ExecuteCreateEvent(instancedMono);
                postCreateEvent?.Invoke(instancedMono);
                Push(instancedMono);
            }
        }



        /// <inheritdoc/>
        public async override UniTask CreateInternalAsync(int createCount, Action<TMono> postCreateEvent = null)
        {
            var instancedMonos = await GameObject.InstantiateAsync(TargetMono, createCount);

            for (int i = 0; i < instancedMonos.Length; i++)
            {
                var instancedMono = instancedMonos[i];
                ExecuteCreateEvent(instancedMono);
                postCreateEvent?.Invoke(instancedMono);
                Push(instancedMono);
            }
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================
}