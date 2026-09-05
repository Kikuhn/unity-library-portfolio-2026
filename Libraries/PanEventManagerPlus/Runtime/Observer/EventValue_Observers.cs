using Pan.Util;
using System;
using Pan.Util.IOB;



namespace Pan.Event
{
    /// <summary>옵저버 이벤트 밸류</summary>
    /// <typeparam name="T">대상 옵저버 인터페이스 Type</typeparam>
    public abstract class BaseEventValue_Observer<TEventValue, T> : PanBaseEventValue.EventAbles<TEventValue>
        where TEventValue : BaseEventValue_Observer<TEventValue, T>, new()
        where T : IObserver
    {
        ///======================================================================================================================================================



        public BaseEventValue_Observer()
        {
            Observer = new Observer_Main<T>();
        }



        public static implicit operator Observer_Main<T>(BaseEventValue_Observer<TEventValue, T> value) => value.Observer;



        ///======================================================================================================================================================



        protected readonly Observer_Main<T> Observer;



        /// <summary>
        /// 옵저버 초기 용량 (재정의 가능)
        /// </summary>
        protected virtual int ObserversCapacity { get; } = 0;



        ///======================================================================================================================================================



        /// <summary>
        /// 옵저버에 추가
        /// </summary>
        public bool AddOB(T value)
        {
            return Observer.AddOB(value);
        }



        /// <summary>
        /// 옵저버에 제거
        /// </summary>
        public bool RemoveOB(T value)
        {
            return Observer.RemoveOB(value);
        }



        protected sealed override void Disable()
        {
            Observer.ClearOB();
            DisableCurrent();
        }



        /// <summary>옵저버 이벤트 밸류 전용 Disable 함수</summary>
        protected virtual void DisableCurrent() { }



        ///======================================================================================================================================================
    }



    /// <summary>옵저버 이벤트 밸류 확장, 기본 구독자 클래스를 보유</summary>
    /// <typeparam name="T">대상 옵저버 인터페이스 Type</typeparam>
    /// <typeparam name="TBase">기본 구독자 클래스들의 베이스</typeparam>
    public abstract class BaseEventValue_ObserverPlus<TEventValue, T, TBase> : BaseEventValue_Observer<TEventValue, T>
        where TEventValue : BaseEventValue_Observer<TEventValue, T>, new()
        where T : IObserver where TBase : BaseEventValue_ObserverPlus<TEventValue, T, TBase>.BaseSub
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 기본 구독자 클래스들을 관리하는 타입 딕셔너리
        /// </summary>
        protected static AutoTypeDictionary<TBase> BasicSubscribes
        {
            get
            {
                basicSubscribes ??= basicSubscribes = new AutoTypeDictionary<TBase>();
                return basicSubscribes;
            }
        }
        private static AutoTypeDictionary<TBase> basicSubscribes;



        public abstract class BaseSub { }



        ///======================================================================================================================================================



        /// <summary>이 옵저버에 기본 구독자 클래스 추가</summary>
        /// <typeparam name="TSub">구독자 클래스 타입</typeparam>
        public bool AddSubscribe<TSub>() where TSub : class, T, TBase, new()
        {
            return AddOB(BasicSubscribes.Get<TSub>());
        }



        /// <summary>이 옵저버에 기본 구독자 클래스 제거</summary>
        /// <typeparam name="TSub">구독자 클래스 타입</typeparam>
        public bool RemoveSubscribe<TSub>() where TSub : class, T, TBase, new()
        {
            return RemoveOB(BasicSubscribes.Get<TSub>());
        }



        ///======================================================================================================================================================
    }



    /// <summary>옵저버 이벤트 밸류 확장2, 기본 구독자 클래스를 보유 + Enum 옵저버 보유</summary>
    /// <typeparam name="T">대상 옵저버 인터페이스 Type</typeparam>
    /// <typeparam name="TBase">기본 구독자 클래스들의 베이스</typeparam>
    public abstract class BaseEventValue_ObserverPlus2<TEventValue, T, TBase, TEnum> : BaseEventValue_ObserverPlus<TEventValue, T, TBase>
        where TEventValue : BaseEventValue_Observer<TEventValue, T>, new()
        where T : class, IObserver
        where TBase : BaseEventValue_ObserverPlus<TEventValue, T, TBase>.BaseSub
        where TEnum : struct, Enum, IComparable, IConvertible, IFormattable
    {
        ///======================================================================================================================================================



        public BaseEventValue_ObserverPlus2()
        {
            OneOB = new ObserverSet_EnumOnes<TEnum, T>();
        }



        /// <summary>
        /// <see cref="TEnum"/>로 관리되는 One 옵저버
        /// </summary>
        protected readonly ObserverSet_EnumOnes<TEnum, T> OneOB;



        ///======================================================================================================================================================



        /// <summary>One 옵저버 설정하기</summary>
        /// <param name="type">Enum 타입</param>
        /// <param name="targetOB">대상 클래스</param>
        /// <param name="rank">랭크</param>
        /// <param name="overlap">중첩 여부</param>
        /// <returns></returns>
        public bool SetOneOB(TEnum type, T targetOB, int rank = 0, bool overlap = true)
        {
            return OneOB.GetOB(type).SetOB(targetOB, rank, overlap);
        }

        /// <summary>One 옵저버 구독자 클래스 설정하기</summary>
        /// <typeparam name="TSub">구독자 클래스 타입</typeparam>
        /// <param name="type">Enum 타입</param>
        /// <param name="rank">랭크</param>
        /// <param name="overlap">중첩 여부</param>
        public bool SetOneOB<TSub>(TEnum type, int rank = 0, bool overlap = true) where TSub : TBase, T, new()
        {
            return OneOB.GetOB(type).SetOB(BasicSubscribes.Get<TSub>(), rank, overlap);
        }



        /// <summary>One 옵저버 제거하기</summary>
        /// <param name="type">Enum 타입</param>
        public bool ClearOneDB(TEnum type, int rank)
        {
            return OneOB.GetOB(type).ClearOB(rank);
        }

        /// <summary>One 옵저버 제거하기 (넣었던 옵저버로)</summary>
        /// <param name="type">Enum 타입</param>
        public bool ClearOneDB(TEnum type, T value)
        {
            return OneOB.GetOB(type).ClearOB(value);
        }



        ///======================================================================================================================================================



        protected override void DisableCurrent()
        {
            OneOB.ClearOB_Absolute();

            DisableCurrent2();
        }



        protected virtual void DisableCurrent2() { }



        ///======================================================================================================================================================


    }
}