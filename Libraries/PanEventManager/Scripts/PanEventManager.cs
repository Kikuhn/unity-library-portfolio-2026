using UnityEngine;
using Pan.Util;
using System.Collections.Generic;
using System;
using System.Linq;
using Sirenix.OdinInspector;



//? 이벤트 매니저



namespace Pan.Event
{
    /// <summary>
    /// 이벤트 매니저, <see cref="PanBaseEvent"/>를 상속받는 클래스들을 관리하는 매니저
    /// <para><see cref="PanEventGeneralManager"/>에서 사용된다</para>
    /// </summary>
    [Serializable]
    public class PanEventManager
    {
        ///======================================================================================================================================================



        //? 생성자



        private PanEventManager(Dictionary<Type, PanBaseEvent> events)
        {
            Events = events;
        }



        /// <summary>
        /// 리플렉션을 사용해 모든 <see cref="PanBaseEvent"/> 들을 얻어와 딕셔너리를 초기화한다
        /// </summary>
        public static PanEventManager Initialize_byReflection()
        {
            var eventTypes = PanEventsInitializeSettingSbjectBase.GetInitializableTypes(typeof(PanBaseEvent));
            var events = new Dictionary<Type, PanBaseEvent>(eventTypes.Length);

            foreach (var type in eventTypes)
            {
                events.Add(type, Activator.CreateInstance(type) as PanBaseEvent);
            }

            return new PanEventManager(events);
        }



        /// <summary>
        /// 초기화 설정SO를 사용해 <see cref="PanBaseEvent"/> 들을 얻어와 딕셔너리를 초기화한다
        /// <para>설정값과 상관없이, Dictionary의 Key는 모두 선언된다</para>
        /// </summary>
        public static PanEventManager Initialize_byInitializeSettings(PanEventInitializeSettingSbject panEventInitializeSettingSbject)
        {
            Dictionary<Type, PanBaseEvent> events;

#if UNITY_EDITOR
            panEventInitializeSettingSbject.InitializeSettings(); //. 에디터 환경에서는 무조건 갱신
#endif

            try
            {
                panEventInitializeSettingSbject.CreateEventDictionaryFromThisSettings(out events);
            }
            catch (Exception e)
            {
                try
                {
                    Debug.LogWarning($"이벤트 초기화 설정에서 오류가 발생, 해당 이벤트 SO를 갱신 및 초기화를 한 뒤 재시도 준비\n{e}");
                    panEventInitializeSettingSbject.InitializeSettings();
                    panEventInitializeSettingSbject.CreateEventDictionaryFromThisSettings(out events);
                }
                catch (Exception e2)
                {
                    Debug.LogError($"이벤트 SO를 갱신 및 초기화를 시도하였음에도, 이벤트 초기화 설정에서 오류가 발생하여, 리플렉션으로 대체\n{e2}");
                    events = null;
                }
            }

            return (events != null) ?
                new PanEventManager(events) :
                Initialize_byReflection();
        }



        ///======================================================================================================================================================



        //? Event 저장소



        /// <summary>
        /// <see cref="PanBaseEvent"/>들이 Type-Dictioanry로 저장되어있는 저장소
        /// <para><see cref="PanBaseEvent"/>의 모든 타입은 Key로 선언이 보장되어있는 상태</para>
        /// </summary>
        [ShowInInspector]//DictionaryDrawerSettings(IsReadOnly = true)
        [LabelText("PanEvent 목록 직접 보기")]
        private readonly Dictionary<Type, PanBaseEvent> Events;



        /// <summary>
        /// <see cref="PanBaseEvent"/>들의 Dictionary를 읽기 전용으로 얻기
        /// </summary>
        public IReadOnlyDictionary<Type, PanBaseEvent> GetEventsDictionary => Events;



        ///======================================================================================================================================================



        //? Event 사용



        /// <summary>
        /// <see cref="PanBaseEvent"/> 를 얻기 (제네릭)
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <returns></returns>
        public TEvent GetEvent<TEvent>() where TEvent : PanBaseEvent, new()
        {
            var type = typeof(TEvent);
            var result = Events[type];

            if (result != null) { return result as TEvent; }

            //result = SU_Collection_Types.CreateInstanceByExpression<TEvent>(type); //. 새로 생성하여 등록
            //result = Activator.CreateInstance<TEvent>();//. 새로 생성하여 등록
            result = new TEvent(); //. 새로 생성하여 등록

            Events[type] = result;

            return result as TEvent;
        }



        /// <summary>
        /// <see cref="PanBaseEvent"/> 를 얻기
        /// </summary>
        /// <param name="type">반드시 <see cref="PanBaseEvent"/>의 타입 이여야 함</param>
        /// <returns></returns>
        public PanBaseEvent GetEvent(Type type)
        {
            var result = Events[type];

            if (result != null) { return result; }

            //result = SU_Collection_Types.CreateInstanceByExpression<PanBaseEvent>(type); //. 새로 생성하여 등록
            result = Activator.CreateInstance(type) as PanBaseEvent; //. 새로 생성하여 등록
            Events[type] = result;

            return result;
        }



        /// <summary>
        /// <typeparamref name="TEvent"/> 타입이 이벤트 테이블에 등록되어 있는지 확인합니다.
        /// <para>이벤트 인스턴스가 해제되어 값이 <c>null</c>이어도 타입 등록은 유지되므로 <c>true</c>를 반환합니다.</para>
        /// <para>현재 사용할 수 있는 인스턴스의 존재 여부는 <see cref="HasEventInstance{TEvent}()"/>를 사용합니다.</para>
        /// </summary>
        /// <typeparam name="TEvent">확인할 이벤트 타입입니다.</typeparam>
        /// <returns>타입이 등록되어 있으면 <c>true</c>입니다.</returns>
        public bool ContainsEvent<TEvent>() where TEvent : PanBaseEvent, new()
        {
            return Events.ContainsKey(typeof(TEvent));
        }



        /// <summary>
        /// 지정한 이벤트 타입이 이벤트 테이블에 등록되어 있는지 확인합니다.
        /// <para>이벤트 인스턴스가 해제되어 값이 <c>null</c>이어도 타입 등록은 유지되므로 <c>true</c>를 반환합니다.</para>
        /// <para>현재 사용할 수 있는 인스턴스의 존재 여부는 <see cref="HasEventInstance(Type)"/>를 사용합니다.</para>
        /// </summary>
        /// <param name="type">확인할 <see cref="PanBaseEvent"/> 파생 타입입니다.</param>
        /// <returns>타입이 등록되어 있으면 <c>true</c>입니다.</returns>
        public bool ContainsEvent(Type type)
        {
            return Events.ContainsKey(type);
        }



        /// <summary>
        /// <typeparamref name="TEvent"/> 타입에 현재 사용할 수 있는 이벤트 인스턴스가 있는지 확인합니다.
        /// </summary>
        /// <typeparam name="TEvent">확인할 이벤트 타입입니다.</typeparam>
        /// <returns>등록된 값이 <c>null</c>이 아니면 <c>true</c>입니다.</returns>
        public bool HasEventInstance<TEvent>() where TEvent : PanBaseEvent, new()
        {
            return HasEventInstance(typeof(TEvent));
        }



        /// <summary>
        /// 지정한 이벤트 타입에 현재 사용할 수 있는 이벤트 인스턴스가 있는지 확인합니다.
        /// </summary>
        /// <param name="type">확인할 <see cref="PanBaseEvent"/> 파생 타입입니다.</param>
        /// <returns>등록된 값이 <c>null</c>이 아니면 <c>true</c>입니다.</returns>
        public bool HasEventInstance(Type type)
        {
            return Events.TryGetValue(type, out PanBaseEvent currentEvent) && currentEvent != null;
        }



        /// <summary>
        /// <see cref="PanBaseEvent"/> 해제 (제네릭)
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        public bool ReleaseEvent<TEvent>() where TEvent : PanBaseEvent, new()
        {
            var type = typeof(TEvent);
            var result = Events[type];

            if (result != null)
            {
                result.Dispose(); //. 안전하게 해제
                Events[type] = null!; //. 자리 유지용 null 대입 (컴파일러 경고 억제)

                return true;
            }
            else
            {
                return false;
            }
        }



        /// <summary>
        /// <see cref="PanBaseEvent"/> 해제
        /// </summary>
        /// <param name="type">반드시 <see cref="PanBaseEvent"/>의 타입 이여야 함</param>
        public bool ReleaseEvent(Type type)
        {
            var result = Events[type];

            if (result != null)
            {
                result.Dispose(); //. 안전하게 해제
                Events[type] = null!; //. 자리 유지용 null 대입 (컴파일러 경고 억제)

                return true;
            }
            else
            {
                return false;
            }
        }



        /// <summary>
        /// 모든 <see cref="PanBaseEvent"/> 해제
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        public void ReleaseEvents()
        {
            var eventList = Events.ToList();

            for (int i = 0; i < eventList.Count; i++)
            {
                var item = eventList[i];

                if (item.Value != null)
                {
                    Events[item.Key].Dispose(); //. 안전하게 해제
                    Events[item.Key] = null!; //. 자리 유지용 null 대입 (컴파일러 경고 억제)
                }
            }
        }



        ///======================================================================================================================================================
    }
}
