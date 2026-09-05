using UnityEngine;
using Pan.Util;
using System;
using Sirenix.OdinInspector;
using System.Collections.Generic;



//? 이벤트 밸류 매니저



namespace Pan.Event
{
    /// <summary>
    /// 이벤트 밸류 매니저
    /// </summary>
    [Serializable]
    public class PanEventValueManager
    {
        ///======================================================================================================================================================



        //. 이벤트 밸류 매니저 내부 생성자
        private PanEventValueManager(InfinityStackManager_TypeBaseClassInstance_Improved<PanBaseEventValue> eventValueInfinityPool)
        {
            EventValueInfinityPool = eventValueInfinityPool;
            EventValueInfinityPool.InitialInitialize();
        }



        /// <summary>
        /// 검색된 모든 <see cref="PanBaseEventValue"/> 타입의 풀을 사전 생성 개수 0으로 등록합니다.
        /// <para>타입 인스턴스를 만들어 <see cref="PanBaseEventValue.CreateCount"/>를 조회하지 않으므로 초기화 시 사용자 생성자나 정적 상태를 건드리지 않습니다.</para>
        /// <para>첫 대여 시 필요한 인스턴스가 지연 생성되며, 사전 생성이 필요하면 초기화 설정 또는 <see cref="CachingEventValue{TEventValue}(int)"/>를 사용합니다.</para>
        /// </summary>
        public static PanEventValueManager Initialize_EachCreatCount()
        {
            var eventValueTypes = PanEventsInitializeSettingSbjectBase.GetInitializableTypes(typeof(PanBaseEventValue));
            var initializeCountDictionary = new Dictionary<Type, int>(eventValueTypes.Length);

            foreach (var type in eventValueTypes)
            {
                initializeCountDictionary.Add(type, 0);
            }

            return new PanEventValueManager(new InfinityStackManager_TypeBaseClassInstance_Improved<PanBaseEventValue>(initializeCountDictionary));
        }



        /// <summary>
        /// 초기화 설정SO를 사용해 <see cref="PanBaseEventValue"/>의 각 생성개수들을 얻어와 딕셔너리를 초기화한다
        /// </summary>
        public static PanEventValueManager Initialize_byInitializeSettings(PanEventValueInitializeSettingSbject panEventValueInitializeSettingSbject)
        {
            Dictionary<Type, int> initializeCountDictionary;

#if UNITY_EDITOR
            panEventValueInitializeSettingSbject.InitializeSettings(); //. 에디터 환경에서는 무조건 갱신
#endif

            try
            {
                panEventValueInitializeSettingSbject.CreateEventValuePoolFromThisSettings(out initializeCountDictionary);
            }
            catch (Exception e)
            {
                try
                {
                    Debug.LogWarning($"이벤트 밸류 초기화 설정에서 오류가 발생, 해당 이벤트 밸류 SO를 갱신 및 초기화를 한 뒤 재시도 준비\n{e}");
                    panEventValueInitializeSettingSbject.InitializeSettings();
                    panEventValueInitializeSettingSbject.CreateEventValuePoolFromThisSettings(out initializeCountDictionary);
                }
                catch (Exception e2)
                {
                    Debug.LogError($"이벤트 밸류 SO를 갱신 및 초기화를 시도하였음에도, 이벤트 밸류 초기화 설정에서 오류가 발생하여, 리플렉션으로 대체\n{e2}");
                    initializeCountDictionary = null;
                }
            }

            return (initializeCountDictionary != null) ?
                new PanEventValueManager(new InfinityStackManager_TypeBaseClassInstance_Improved<PanBaseEventValue>(initializeCountDictionary)) :
                Initialize_EachCreatCount();
        }



        ///======================================================================================================================================================



        //? 이벤트 밸류 풀



        [ShowInInspector]
        [LabelText("PanEventValue Pool 직접 보기")]
        [HideReferenceObjectPicker]
        private InfinityStackManager_TypeBaseClassInstance_Improved<PanBaseEventValue> EventValueInfinityPool;



        /// <summary>
        /// 이벤트 밸류 풀 딕셔너리를 직접 얻기 (읽기전용)
        /// </summary>
        public IReadOnlyDictionary<Type, InfinityStack_ClassInstance<PanBaseEventValue>> GetEventValuePoolDictionary => EventValueInfinityPool.GetPoolDictionary;



        ///======================================================================================================================================================



        //? 이벤트 밸류 확인



        ///<summary>
        ///전체 이벤트 밸류 Count 반환
        ///</summary>
        public int GetEventValueCount()
        {
            return EventValueInfinityPool.GetPoolDictionaryCount;
        }



        ///<summary>
        ///이벤트 밸류 Count 반환
        ///</summary>
        public int GetEventValueCount<TEventValue>() where TEventValue : PanBaseEventValue, new()
        {
            int result;
            EventValueInfinityPool.TryGetPoolCount<TEventValue>(out result);
            return result;
        }



        ///<summary>
        ///이벤트 밸류 Count 반환
        ///</summary>
        internal int GetEventValueCount(Type type)
        {
            int result;
            EventValueInfinityPool.TryGetPoolCount(type, out result);
            return result;
        }



        /// <summary>
        /// 대상 이벤트 밸류의 풀링 개수 얻어보기
        /// </summary>
        public bool TryGetCount<TEventValue>(out int value) where TEventValue : PanBaseEventValue, new()
        {
            if (EventValueInfinityPool.TryGetPoolCount<TEventValue>(out var result))
            {
                value = result;
                return true;
            }
            else
            {
                value = 0;
                return false;
            }
        }



        /// <summary>
        /// 대상 이벤트 밸류의 풀링 개수 얻어보기
        /// </summary>
        internal bool TryGetCount(Type type, out int value)
        {
            if (EventValueInfinityPool.TryGetPoolCount(type, out var result))
            {
                value = result;
                return true;
            }
            else
            {
                value = 0;
                return false;
            }
        }



        ///======================================================================================================================================================



        //? 이벤트 밸류 캐싱 (생성, Create)



        ///<summary>
        ///<see cref="TEventValue"/> 캐싱하기 (생성, Create)
        ///
        ///</summary>
        public int CachingEventValue<TEventValue>(int cachingCount) where TEventValue : PanBaseEventValue, new()
        {
            return EventValueInfinityPool.Create<TEventValue>(cachingCount);
        }



        ///<summary>
        ///<see cref="TEventValue"/> 캐싱하기 (생성, Create)
        ///</summary>
        internal int CachingEventValue(Type type, int cachingCount)
        {
            return EventValueInfinityPool.Create(type, cachingCount);
        }



        ///======================================================================================================================================================



        //? 이벤트 밸류 넣기 (Push)



        /// <summary>
        ///<see cref="PanBaseEventValue"/> 넣기 (Push)
        /// </summary>
        /// <param name="eventValue"></param>
        public void PushEventValue<TEventValue>(TEventValue eventValue) where TEventValue : PanBaseEventValue, new()
        {
            EventValueInfinityPool.Push(eventValue);
        }



        /// <summary>
        ///<see cref="PanBaseEventValue"/> 넣기 (Push)
        /// </summary>
        /// <param name="eventValue"></param>
        internal void PushEventValue(Type type, PanBaseEventValue eventValue)
        {
            EventValueInfinityPool.Push(type, eventValue);
        }



        ///======================================================================================================================================================



        //? 이벤트 밸류 꺼내기 (Pop)



        ///<summary>
        ///<typeparamref name="TEventValue"/> 꺼내기 (Pop)
        ///</summary>
        /// <param name="autoTryCreateElementsWhenPoolNotEnough">
        /// 꺼내야 하는데, 풀의 크기가 모자랄 경우 (+풀이 존재 하지 않거나), <see cref="Create(int, Action{TKey})"/>을 한 이후 반환 할 지 여부
        /// <para>null을 받아오면, 풀의 크기가 모자랄경우 반환에 실패한다</para>
        /// <para>값을 넣더라도, <see cref="TInfinityStack.MaxElementCount"/>에 의해 반환에 실패 할 수도 있다</para>
        /// <para>기본값: 1</para>
        /// </param>
        public TEventValue PopEventValue<TEventValue>(int? autoTryCreateElementsWhenPoolNotEnough = 1) where TEventValue : PanBaseEventValue, new()
        {
            return EventValueInfinityPool.Pop<TEventValue>(autoTryCreateElementsWhenPoolNotEnough);
        }



        ///<summary>
        ///<see cref="PanBaseEventValue"/>꺼내기 (Pop) (internal 타입)
        ///</summary>
        /// <param name="autoTryCreateElementsWhenPoolNotEnough">
        /// 꺼내야 하는데, 풀의 크기가 모자랄 경우 (+풀이 존재 하지 않거나), <see cref="Create(int, Action{TKey})"/>을 한 이후 반환 할 지 여부
        /// <para>null을 받아오면, 풀의 크기가 모자랄경우 반환에 실패한다</para>
        /// <para>값을 넣더라도, <see cref="TInfinityStack.MaxElementCount"/>에 의해 반환에 실패 할 수도 있다</para>
        /// <para>기본값: 1</para>
        /// </param>
        internal PanBaseEventValue PopEventValue(Type type, int? autoTryCreateElementsWhenPoolNotEnough = 1)
        {
            return EventValueInfinityPool.Pop(type, autoTryCreateElementsWhenPoolNotEnough);
        }



        ///======================================================================================================================================================



        //? 이벤트 밸류 해제하기 (Release)



        ///<summary>
        ///이벤트 밸류 제거하기
        ///</summary>
        public void ReleaseEventValue<TEventValue>(int releaseCount) where TEventValue : PanBaseEventValue, new()
        {
            EventValueInfinityPool.Release<TEventValue>(releaseCount);
        }



        ///<summary>
        ///모든 이벤트 밸류 제거하기
        ///</summary>
        ///<param name="removePoolInPoolDictionary"><typeparamref name="TEventValue"/>를 기반으로 만들어진 풀 그 자체를 제거할지 여부</param>
        public void ReleaseClearEventValue<TEventValue>(bool removePoolInPoolDictionary) where TEventValue : PanBaseEventValue, new()
        {
            EventValueInfinityPool.ReleaseClear<TEventValue>(removePoolInPoolDictionary);
        }



        ///<summary>
        ///전체 모든이벤트 밸류 제거하기
        ///</summary>
        ///<param name="clearPoolInPoolDictionary">각각의 <see cref="PanBaseEventValue"/>를 기반으로 만들어진 풀 그 자체를 모두 제거할지 여부</param>
        public void ReleaseClearAllEventValue(bool clearPoolInPoolDictionary)
        {
            EventValueInfinityPool.ReleaseClearAll(clearPoolInPoolDictionary);
        }



        ///======================================================================================================================================================



        //? 검사



        ///<summary>
        /// 이벤트 밸류 중복 검사
        /// </summary>
        public void CheckEventValuePoolDictionary_IsDuplicate()
        {
            EventValueInfinityPool?.CheckPoolDictionary_IsDuplicate();
        }



        ///======================================================================================================================================================
    }
}
