using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.IO;
using UnityEngine.Events;
using SitraUtils;
using DG.Tweening;
using UnityEngine.InputSystem;
using Pan.Util;
using Sirenix.OdinInspector;



//? 옵저버 패턴을 정리한 정도의 코드



namespace Pan.Util.IOB
{
    ///======================================================================================================================================================



    //? 옵저버



    #region 옵저버



    /// <summary>
    /// 옵저버 기본 인터페이스
    /// </summary>
    public interface IObserver { }



    /// <summary>
    /// 옵저버 <b>알람</b> 인터페이스
    /// </summary>
    public interface IObserver_Alarm<TOB> where TOB : IObserver
    {
        void AlarmLA<TAction>(Action<TAction> act) where TAction : TOB;

        void AlarmLA<T1, TAction>(T1 v1, Action<T1, TAction> act) where TAction : TOB;

        void AlarmLA<T1, T2, TAction>(T1 v1, T2 v2, Action<T1, T2, TAction> act) where TAction : TOB;

        void AlarmLA<T1, T2, T3, TAction>(T1 v1, T2 v2, T3 v3, Action<T1, T2, T3, TAction> act) where TAction : TOB;

        void Alarm<TAlarm>() where TAlarm : TOB, ISubscribe_Alarm;

        void Alarm<T1, TAlarm>(T1 v1) where TAlarm : TOB, ISubscribe_Alarm<T1>;

        void Alarm<T1, T2, TAlarm>(T1 v1, T2 v2) where TAlarm : TOB, ISubscribe_Alarm<T1, T2>;

        void Alarm<T1, T2, T3, TAlarm>(T1 v1, T2 v2, T3 v3) where TAlarm : TOB, ISubscribe_Alarm<T1, T2, T3>;
    }



    /// <summary>
    /// 옵저버 <b>대신</b> 인터페이스
    /// </summary>
    public interface IObserver_Instead<TOB> where TOB : IObserver
    {
        bool InsteadLA<TAction>(Action<TAction> act) where TAction : TOB;

        bool InsteadLA<T1, TAction>(T1 v1, Action<T1, TAction> act) where TAction : TOB;

        bool InsteadLA<T1, T2, TAction>(T1 v1, T2 v2, Action<T1, T2, TAction> act) where TAction : TOB;

        bool InsteadLA<T1, T2, T3, TAction>(T1 v1, T2 v2, T3 v3, Action<T1, T2, T3, TAction> act) where TAction : TOB;

        bool Instead<TInstead>() where TInstead : TOB, ISubscribe_Instead;

        bool Instead<T1, TInstead>(T1 v1) where TInstead : TOB, ISubscribe_Instead<T1>;

        bool Instead<T1, T2, TInstead>(T1 v1, T2 v2) where TInstead : TOB, ISubscribe_Instead<T1, T2>;

        bool Instead<T1, T2, T3, TInstead>(T1 v1, T2 v2, T3 v3) where TInstead : TOB, ISubscribe_Instead<T1, T2, T3>;

    }



    /// <summary>
    /// 옵저버 <b>스위치</b> 인터페이스
    /// </summary>
    public interface IObserver_Switch<TOB> where TOB : IObserver
    {
        bool SwitchLA<TAction>(Func<TAction, bool> func) where TAction : TOB;

        bool SwitchLA<T1, TAction>(T1 v1, Func<T1, TAction, bool> func) where TAction : TOB;

        bool SwitchLA<T1, T2, TAction>(T1 v1, T2 v2, Func<T1, T2, TAction, bool> func) where TAction : TOB;

        bool SwitchLA<T1, T2, T3, TAction>(T1 v1, T2 v2, T3 v3, Func<T1, T2, T3, TAction, bool> func) where TAction : TOB;

        bool Switch<TSwitch>() where TSwitch : TOB, ISubscribe_Switch;

        bool Switch<T1, TSwitch>(T1 v1) where TSwitch : TOB, ISubscribe_Switch<T1>;

        bool Switch<T1, T2, TSwitch>(T1 v1, T2 v2) where TSwitch : TOB, ISubscribe_Switch<T1, T2>;

        bool Switch<T1, T2, T3, TSwitch>(T1 v1, T2 v2, T3 v3) where TSwitch : TOB, ISubscribe_Switch<T1, T2, T3>;
    }



    ///======================================================================================================================================================



    ///<summary>
    ///옵저버의 근간이 되는 클래스<br/>
    ///<typeparamref name="TOB"/> 를 상속받은 대상만 관리가 가능
    /// </summary>
    [Serializable]
    public abstract class BaseObserver<TOB> : IObserver_Alarm<TOB>, IObserver_Instead<TOB>, IObserver_Switch<TOB> where TOB : IObserver
    {
        ///======================================================================================================================================================

        protected readonly object _lock = new object();

        ///======================================================================================================================================================

        ///<summary>[알람 : 람다 액션] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 액션 실행</summary>
        ///<param name="act">액션</param>
        public abstract void AlarmLA<TAction>(Action<TAction> act) where TAction : TOB;

        ///<summary>[알람 : 람다 액션 매개변수 1] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 액션 실행</summary>
        ///<param name="act">액션</param>
        public abstract void AlarmLA<T1, TAction>(T1 v1, Action<T1, TAction> act) where TAction : TOB;

        ///<summary>[알람 : 람다 액션 매개변수 2] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 액션 실행</summary>
        ///<param name="act">액션</param>
        public abstract void AlarmLA<T1, T2, TAction>(T1 v1, T2 v2, Action<T1, T2, TAction> act) where TAction : TOB;

        ///<summary>[알람 : 람다 액션 매개변수 3] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 액션 실행</summary>
        ///<param name="act">액션</param>
        public abstract void AlarmLA<T1, T2, T3, TAction>(T1 v1, T2 v2, T3 v3, Action<T1, T2, T3, TAction> act) where TAction : TOB;


        ///<summary>
        ///[알람 : 전용옵저버] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 해당 알람 실행
        ///</summary>
        public abstract void Alarm<TAlarm>() where TAlarm : TOB, ISubscribe_Alarm;

        ///<summary>
        ///[알람 전용옵저버 매개변수 1] : 모든 옵저버를 순회한다, 추가 매개변수를 받아오며, 받아온 제네릭 타입일경우 해당 알람 실행
        ///</summary>
        public abstract void Alarm<T1, TAlarm>(T1 v1) where TAlarm : TOB, ISubscribe_Alarm<T1>;

        ///<summary>
        ///[알람 전용옵저버 매개변수 2] : 모든 옵저버를 순회한다, 추가 매개변수를 받아오며, 받아온 제네릭 타입일경우 해당 알람 실행
        ///</summary>
        public abstract void Alarm<T1, T2, TAlarm>(T1 v1, T2 v2) where TAlarm : TOB, ISubscribe_Alarm<T1, T2>;

        ///<summary>
        ///[알람 전용옵저버 매개변수 3] : 모든 옵저버를 순회한다, 추가 매개변수를 받아오며, 받아온 제네릭 타입일경우 해당 알람 실행
        ///</summary>
        public abstract void Alarm<T1, T2, T3, TAlarm>(T1 v1, T2 v2, T3 v3) where TAlarm : TOB, ISubscribe_Alarm<T1, T2, T3>;

        ///======================================================================================================================================================

        ///<summary>[대신 : 람다 액션] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 액션 실행, 한번이라도 실행에 성공할경우 true 반환 그외 false</summary>
        ///<param name="act">액션</param>
        public abstract bool InsteadLA<TAction>(Action<TAction> act) where TAction : TOB;

        ///<summary>[대신 : 람다 액션 매개변수 1] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 액션 실행, 한번이라도 실행에 성공할경우 true 반환 그외 false</summary>
        ///<param name="act">액션</param>
        public abstract bool InsteadLA<T1, TAction>(T1 v1, Action<T1, TAction> act) where TAction : TOB;

        ///<summary>[대신 : 람다 액션 매개변수 2] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 액션 실행, 한번이라도 실행에 성공할경우 true 반환 그외 false</summary>
        ///<param name="act">액션</param>
        public abstract bool InsteadLA<T1, T2, TAction>(T1 v1, T2 v2, Action<T1, T2, TAction> act) where TAction : TOB;

        ///<summary>[대신 : 람다 액션 매개변수 3] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 액션 실행, 한번이라도 실행에 성공할경우 true 반환 그외 false</summary>
        ///<param name="act">액션</param>
        public abstract bool InsteadLA<T1, T2, T3, TAction>(T1 v1, T2 v2, T3 v3, Action<T1, T2, T3, TAction> act) where TAction : TOB;


        ///<summary>[대신 : 전용옵저버] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 해당 알람 실행, 한번이라도 실행에 성공할경우 true 반환 그외 false</summary>
        ///<param name="act">액션</param>
        public abstract bool Instead<TInstead>() where TInstead : TOB, ISubscribe_Instead;

        ///<summary>[대신 : 전용옵저버 매개변수 1] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 해당 알람 실행, 한번이라도 실행에 성공할경우 true 반환 그외 false</summary>
        ///<param name="act">액션</param>
        public abstract bool Instead<T1, TInstead>(T1 v1) where TInstead : TOB, ISubscribe_Instead<T1>;

        ///<summary>[대신 : 전용옵저버 매개변수 2] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 해당 알람 실행, 한번이라도 실행에 성공할경우 true 반환 그외 false</summary>
        ///<param name="act">액션</param>
        public abstract bool Instead<T1, T2, TInstead>(T1 v1, T2 v2) where TInstead : TOB, ISubscribe_Instead<T1, T2>;

        ///<summary>[대신 : 전용옵저버 매개변수 3] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 해당 알람 실행, 한번이라도 실행에 성공할경우 true 반환 그외 false</summary>
        ///<param name="act">액션</param>
        public abstract bool Instead<T1, T2, T3, TInstead>(T1 v1, T2 v2, T3 v3) where TInstead : TOB, ISubscribe_Instead<T1, T2, T3>;

        ///======================================================================================================================================================

        ///<summary>[스위치 : 람다 액션] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 펑션 실행, 한번이라도 그 펑션의 값이 true일경우 true 반환</summary>
        ///<param name="func">펑션</param>
        public abstract bool SwitchLA<TFunc>(Func<TFunc, bool> func) where TFunc : TOB;

        ///<summary>[스위치 : 람다 액션 매개변수 1] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 펑션 실행, 한번이라도 그 펑션의 값이 true일경우 true 반환</summary>
        ///<param name="func">펑션</param>
        public abstract bool SwitchLA<T1, TFunc>(T1 v1, Func<T1, TFunc, bool> func) where TFunc : TOB;

        ///<summary>[스위치 : 람다 액션 매개변수 2] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 펑션 실행, 한번이라도 그 펑션의 값이 true일경우 true 반환</summary>
        ///<param name="func">펑션</param>
        public abstract bool SwitchLA<T1, T2, TFunc>(T1 v1, T2 v2, Func<T1, T2, TFunc, bool> func) where TFunc : TOB;

        ///<summary>[스위치 : 람다 액션 매개변수 3] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 펑션 실행, 한번이라도 그 펑션의 값이 true일경우 true 반환</summary>
        ///<param name="func">펑션</param>
        public abstract bool SwitchLA<T1, T2, T3, TFunc>(T1 v1, T2 v2, T3 v3, Func<T1, T2, T3, TFunc, bool> func) where TFunc : TOB;


        ///<summary>[스위치 : 전용 옵저버] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 스위치 실행, 한번이라도 그 스위치의 값이 true일경우 true 반환</summary>
        ///<param name="func">펑션</param>
        public abstract bool Switch<TSwitch>() where TSwitch : TOB, ISubscribe_Switch;

        ///<summary>[스위치 : 전용 옵저버 매개변수 1] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 스위치 실행, 한번이라도 그 스위치의 값이 true일경우 true 반환</summary>
        ///<param name="func">펑션</param>
        public abstract bool Switch<T1, TSwitch>(T1 v1) where TSwitch : TOB, ISubscribe_Switch<T1>;

        ///<summary>[스위치 : 전용 옵저버 매개변수 2] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 스위치 실행, 한번이라도 그 스위치의 값이 true일경우 true 반환</summary>
        ///<param name="func">펑션</param>
        public abstract bool Switch<T1, T2, TSwitch>(T1 v1, T2 v2) where TSwitch : TOB, ISubscribe_Switch<T1, T2>;

        ///<summary>[스위치 : 전용 옵저버 매개변수 2] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 스위치 실행, 한번이라도 그 스위치의 값이 true일경우 true 반환</summary>
        ///<param name="func">펑션</param>
        public abstract bool Switch<T1, T2, T3, TSwitch>(T1 v1, T2 v2, T3 v3) where TSwitch : TOB, ISubscribe_Switch<T1, T2, T3>;

        ///======================================================================================================================================================
    }



    /// <summary>
    /// 옵저버 메인<br/>
    ///<typeparamref name="TOB"/> 를 상속받은 대상만 관리가 가능
    /// </summary>
    /// <typeparam name="TOB"></typeparam>
    [Serializable]
    public class Observer_Main<TOB> : BaseObserver<TOB> where TOB : IObserver
    {
        ///======================================================================================================================================================



        public Observer_Main(int capacity = 0, int waitersCapacity = 0)
        {
            Observers = new IndexedSet<TOB>(capacity);
            Waiting_Add_List = new List<TOB>(waitersCapacity);
            Waiting_Remove_List = new List<TOB>(waitersCapacity);
        }



        public Observer_Main() : this(0, 0)
        {

        }



        ///======================================================================================================================================================



        [TabGroup("옵저버탭", "핵심 옵저버 정보", SdfIconType.Gear, TextColor = "green"), ShowInInspector, LabelText("옵저버 IndexedSet"), PropertyOrder(-11)]
        private readonly IndexedSet<TOB> Observers;



        public IReadOnlyList<TOB> GetReadOnlyItems => Observers.GetReadOnlyItems();



        [TabGroup("옵저버탭", "핵심 옵저버 정보", SdfIconType.Gear, TextColor = "green"), ShowInInspector, LabelText("옵저버 개수"), PropertyOrder(-10)]
        public int Count => Observers.Count;



        /// <summary>
        /// 순회중인지 여부<br/>
        /// (순회중이라면, 추가/제거가 미뤄진다)
        /// </summary>
        [TabGroup("옵저버탭", "옵저버 순회 정보", SdfIconType.ExclamationCircleFill, TextColor = "red"), ShowInInspector, LabelText("순회중 인지 여부"), PropertyOrder(-9)]
        public bool IsLooping
        {
            get => isLooping;
            private set
            {
                //? Loop가 종료되서 온거라면
                if (isLooping == true && value == false)
                {
                    isLooping = value;
                    Waiting_Event();
                    return;
                }

                isLooping = value;
            }
        }
        private bool isLooping;



        [TabGroup("옵저버탭", "옵저버 순회 정보", SdfIconType.ExclamationCircleFill, TextColor = "red")]
        [HorizontalGroup("옵저버탭/옵저버 순회 정보/대기리스트들", Width = 0.5f)]
        [DisableIf("@true"), ShowInInspector, LabelText("대기 리스트:추가"), ReadOnlyCustom, PropertyOrder(-8)]
        private readonly List<TOB> Waiting_Add_List;



        [TabGroup("옵저버탭", "옵저버 순회 정보", SdfIconType.ExclamationCircleFill, TextColor = "red")]
        [HorizontalGroup("옵저버탭/옵저버 순회 정보/대기리스트들", Width = 0.5f)]
        [DisableIf("@true"), ShowInInspector, LabelText("대기 리스트:제거"), ReadOnlyCustom, PropertyOrder(-7)]
        private readonly List<TOB> Waiting_Remove_List;



        ///======================================================================================================================================================



        ///<summary>
        /// 옵저버 추가
        /// </summary>
        public bool AddOB(TOB value)
        {
            lock (_lock)
            {
                if (Observers.Contains(value)) { return false; }
                if (IsLooping) { Waiting_Add_List.Add(value); return false; }
                Observers.Add(value);
                return true;
            }
        }



        ///<summary>
        /// 옵저버 제거
        /// </summary>
        public bool RemoveOB(TOB value)
        {
            if (value == null) return false;
            lock (_lock)
            {
                if (!Observers.Contains(value)) { return false; }
                if (IsLooping) { Waiting_Remove_List.Add(value); return false; }
                return Observers.Remove(value);
            }
        }



        ///<summary>
        /// 옵저버 Clear
        /// </summary>
        public virtual void ClearOB()
        {
            lock (_lock)
            {
                Observers.Clear();

            }
        }



        ///<summary>
        /// 옵저버 용량 정리
        /// </summary>
        public virtual void TrimExcess()
        {
            lock (_lock)
            {
                Observers.TrimExcess();

            }
        }



        ///======================================================================================================================================================


        //? 순회가 끝난뒤 시행되는 옵저버 추가/제거
        private void Waiting_Event()
        {
            if (Waiting_Add_List.Count != 0)
            {
                foreach (var adder in Waiting_Add_List) { AddOB(adder); }
                Waiting_Add_List.Clear();
            }

            if (Waiting_Remove_List.Count != 0)
            {
                foreach (var remover in Waiting_Remove_List) { RemoveOB(remover); }
                Waiting_Remove_List.Clear();
            }
        }



        ///======================================================================================================================================================



        #region 옵저버 실행

        ///<summary>[알람 : 람다 액션] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 액션 실행</summary>
        ///<param name="act">액션</param>
        public override void AlarmLA<TAction>(Action<TAction> act)
        {
            lock (_lock)
            {
                if (Count == 0) { return; }

                IsLooping = true;

                for (int i = 0; i < Observers.Count; i++)
                {
                    var item = Observers[i];
                    if (item is TAction X) { act?.Invoke(X); }
                }

                IsLooping = false;
            }
        }

        ///<summary>[알람 : 람다 액션 매개변수 1] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 액션 실행</summary>
        ///<param name="act">액션</param>
        public override void AlarmLA<T1, TAction>(T1 v1, Action<T1, TAction> act)
        {
            lock (_lock)
            {
                if (Count == 0) { return; }

                IsLooping = true;

                for (int i = 0; i < Observers.Count; i++)
                {
                    var item = Observers[i];
                    if (item is TAction X) { act?.Invoke(v1, X); }
                }

                IsLooping = false;
            }
        }

        ///<summary>[알람 : 람다 액션 매개변수 2] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 액션 실행</summary>
        ///<param name="act">액션</param>
        public override void AlarmLA<T1, T2, TAction>(T1 v1, T2 v2, Action<T1, T2, TAction> act)
        {
            lock (_lock)
            {
                if (Count == 0) { return; }

                IsLooping = true;

                for (int i = 0; i < Observers.Count; i++)
                {
                    var item = Observers[i];
                    if (item is TAction X) { act?.Invoke(v1, v2, X); }
                }

                IsLooping = false;
            }
        }

        ///<summary>[알람 : 람다 액션 매개변수 3] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 액션 실행</summary>
        ///<param name="act">액션</param>
        public override void AlarmLA<T1, T2, T3, TAction>(T1 v1, T2 v2, T3 v3, Action<T1, T2, T3, TAction> act)
        {
            lock (_lock)
            {
                if (Count == 0) { return; }

                IsLooping = true;

                for (int i = 0; i < Observers.Count; i++)
                {
                    var item = Observers[i];
                    if (item is TAction X) { act?.Invoke(v1, v2, v3, X); }
                }

                IsLooping = false;
            }
        }


        ///<summary>
        ///[알람 : 전용옵저버] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 해당 알람 실행
        ///</summary>
        public override void Alarm<TAlarm>()
        {
            lock (_lock)
            {
                if (Count == 0) { return; }

                IsLooping = true;

                for (int i = 0; i < Observers.Count; i++)
                {
                    var item = Observers[i];
                    if (item is TAlarm X) { X.Alarm(); }
                }

                IsLooping = false;
            }
        }

        ///<summary>
        ///[알람 전용옵저버 매개변수 1] : 모든 옵저버를 순회한다, 추가 매개변수를 받아오며, 받아온 제네릭 타입일경우 해당 알람 실행
        ///</summary>
        public override void Alarm<T1, TAlarm>(T1 v1)
        {
            lock (_lock)
            {
                if (Count == 0) { return; }

                IsLooping = true;

                for (int i = 0; i < Observers.Count; i++)
                {
                    var item = Observers[i];
                    if (item is TAlarm X) { X.Alarm(v1); }
                }

                IsLooping = false;
            }
        }

        ///<summary>
        ///[알람 전용옵저버 매개변수 2] : 모든 옵저버를 순회한다, 추가 매개변수를 받아오며, 받아온 제네릭 타입일경우 해당 알람 실행
        ///</summary>
        public override void Alarm<T1, T2, TAlarm>(T1 v1, T2 v2)
        {
            lock (_lock)
            {
                if (Count == 0) { return; }

                IsLooping = true;

                for (int i = 0; i < Observers.Count; i++)
                {
                    var item = Observers[i];
                    if (item is TAlarm X) { X.Alarm(v1, v2); }
                }

                IsLooping = false;
            }
        }

        ///<summary>
        ///[알람 전용옵저버 매개변수 3] : 모든 옵저버를 순회한다, 추가 매개변수를 받아오며, 받아온 제네릭 타입일경우 해당 알람 실행
        ///</summary>
        public override void Alarm<T1, T2, T3, TAlarm>(T1 v1, T2 v2, T3 v3)
        {
            lock (_lock)
            {
                if (Count == 0) { return; }

                IsLooping = true;

                for (int i = 0; i < Observers.Count; i++)
                {
                    var item = Observers[i];
                    if (item is TAlarm X) { X.Alarm(v1, v2, v3); }
                }

                IsLooping = false;
            }
        }

        ///======================================================================================================================================================

        ///<summary>[대신 : 람다 액션] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 액션 실행, 한번이라도 실행에 성공할경우 true 반환 그외 false</summary>
        ///<param name="act">액션</param>
        public override bool InsteadLA<TAction>(Action<TAction> act)
        {
            lock (_lock)
            {
                if (Count == 0) { return false; }

                bool result = false;

                IsLooping = true;

                for (int i = 0; i < Observers.Count; i++)
                {
                    var item = Observers[i];
                    if (item is TAction X) { act?.Invoke(X); result = true; }
                }

                IsLooping = false;

                return result;
            }
        }

        ///<summary>[대신 : 람다 액션 매개변수 1] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 액션 실행, 한번이라도 실행에 성공할경우 true 반환 그외 false</summary>
        ///<param name="act">액션</param>
        public override bool InsteadLA<T1, TAction>(T1 v1, Action<T1, TAction> act)
        {
            lock (_lock)
            {
                if (Count == 0) { return false; }

                bool result = false;

                IsLooping = true;

                for (int i = 0; i < Observers.Count; i++)
                {
                    var item = Observers[i];
                    if (item is TAction X) { act?.Invoke(v1, X); result = true; }
                }

                IsLooping = false;

                return result;
            }
        }

        ///<summary>[대신 : 람다 액션 매개변수 2] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 액션 실행, 한번이라도 실행에 성공할경우 true 반환 그외 false</summary>
        ///<param name="act">액션</param>
        public override bool InsteadLA<T1, T2, TAction>(T1 v1, T2 v2, Action<T1, T2, TAction> act)
        {
            lock (_lock)
            {
                if (Count == 0) { return false; }

                bool result = false;

                IsLooping = true;

                for (int i = 0; i < Observers.Count; i++)
                {
                    var item = Observers[i];
                    if (item is TAction X) { act?.Invoke(v1, v2, X); result = true; }
                }

                IsLooping = false;

                return result;
            }
        }

        ///<summary>[대신 : 람다 액션 매개변수 3] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 액션 실행, 한번이라도 실행에 성공할경우 true 반환 그외 false</summary>
        ///<param name="act">액션</param>
        public override bool InsteadLA<T1, T2, T3, TAction>(T1 v1, T2 v2, T3 v3, Action<T1, T2, T3, TAction> act)
        {
            lock (_lock)
            {
                if (Count == 0) { return false; }

                bool result = false;

                IsLooping = true;

                for (int i = 0; i < Observers.Count; i++)
                {
                    var item = Observers[i];
                    if (item is TAction X) { act?.Invoke(v1, v2, v3, X); result = true; }
                }

                IsLooping = false;

                return result;
            }
        }


        ///<summary>[대신 : 전용옵저버] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 해당 알람 실행, 한번이라도 실행에 성공할경우 true 반환 그외 false</summary>
        ///<param name="act">액션</param>
        public override bool Instead<TInstead>()
        {
            lock (_lock)
            {
                if (Count == 0) { return false; }

                bool result = false;

                IsLooping = true;

                for (int i = 0; i < Observers.Count; i++)
                {
                    var item = Observers[i];
                    if (item is TInstead X) { X.Instead(); result = true; }
                }

                IsLooping = false;

                return result;
            }
        }

        ///<summary>[대신 : 전용옵저버 매개변수 1] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 해당 알람 실행, 한번이라도 실행에 성공할경우 true 반환 그외 false</summary>
        ///<param name="act">액션</param>
        public override bool Instead<T1, TInstead>(T1 v1)
        {
            lock (_lock)
            {
                if (Count == 0) { return false; }

                bool result = false;

                IsLooping = true;

                for (int i = 0; i < Observers.Count; i++)
                {
                    var item = Observers[i];
                    if (item is TInstead X) { X.Instead(v1); result = true; }
                }

                IsLooping = false;

                return result;
            }
        }

        ///<summary>[대신 : 전용옵저버 매개변수 2] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 해당 알람 실행, 한번이라도 실행에 성공할경우 true 반환 그외 false</summary>
        ///<param name="act">액션</param>
        public override bool Instead<T1, T2, TInstead>(T1 v1, T2 v2)
        {
            lock (_lock)
            {
                if (Count == 0) { return false; }

                bool result = false;

                IsLooping = true;

                for (int i = 0; i < Observers.Count; i++)
                {
                    var item = Observers[i];
                    if (item is TInstead X) { X.Instead(v1, v2); result = true; }
                }

                IsLooping = false;

                return result;
            }
        }

        ///<summary>[대신 : 전용옵저버 매개변수 3] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 해당 알람 실행, 한번이라도 실행에 성공할경우 true 반환 그외 false</summary>
        ///<param name="act">액션</param>
        public override bool Instead<T1, T2, T3, TInstead>(T1 v1, T2 v2, T3 v3)
        {
            lock (_lock)
            {
                if (Count == 0) { return false; }

                bool result = false;

                IsLooping = true;

                for (int i = 0; i < Observers.Count; i++)
                {
                    var item = Observers[i];
                    if (item is TInstead X) { X.Instead(v1, v2, v3); result = true; }
                }

                IsLooping = false;

                return result;
            }
        }

        ///======================================================================================================================================================

        ///<summary>[스위치 : 람다 액션] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 펑션 실행, 한번이라도 그 펑션의 값이 true일경우 true 반환</summary>
        ///<param name="func">펑션</param>
        public override bool SwitchLA<TFunc>(Func<TFunc, bool> func)
        {
            lock (_lock)
            {
                if (Count == 0) { return false; }

                bool result = false;

                IsLooping = true;

                for (int i = 0; i < Observers.Count; i++)
                {
                    var item = Observers[i];

                    if (item is TFunc X)
                    {
                        bool thisFunc = func(X);

                        //? 펑션의 반환값이 true 일 경우에만 true 반환
                        if (thisFunc == true) { result = true; }
                    }
                }

                IsLooping = true;

                return result;
            }
        }

        ///<summary>[스위치 : 람다 액션 매개변수 1] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 펑션 실행, 한번이라도 그 펑션의 값이 true일경우 true 반환</summary>
        ///<param name="func">펑션</param>
        public override bool SwitchLA<T1, TFunc>(T1 v1, Func<T1, TFunc, bool> func)
        {
            lock (_lock)
            {
                if (Count == 0) { return false; }

                bool result = false;

                IsLooping = true;

                for (int i = 0; i < Observers.Count; i++)
                {
                    var item = Observers[i];

                    if (item is TFunc X)
                    {
                        bool thisFunc = func(v1, X);

                        //? 펑션의 반환값이 true 일 경우에만 true 반환
                        if (thisFunc == true) { result = true; }
                    }
                }

                IsLooping = false;

                return result;
            }
        }

        ///<summary>[스위치 : 람다 액션 매개변수 2] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 펑션 실행, 한번이라도 그 펑션의 값이 true일경우 true 반환</summary>
        ///<param name="func">펑션</param>
        public override bool SwitchLA<T1, T2, TFunc>(T1 v1, T2 v2, Func<T1, T2, TFunc, bool> func)
        {
            lock (_lock)
            {
                if (Count == 0) { return false; }

                bool result = false;

                IsLooping = true;

                for (int i = 0; i < Observers.Count; i++)
                {
                    var item = Observers[i];

                    if (item is TFunc X)
                    {
                        bool thisFunc = func(v1, v2, X);

                        //? 펑션의 반환값이 true 일 경우에만 true 반환
                        if (thisFunc == true) { result = true; }
                    }
                }

                IsLooping = false;

                return result;
            }
        }

        ///<summary>[스위치 : 람다 액션 매개변수 3] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 펑션 실행, 한번이라도 그 펑션의 값이 true일경우 true 반환</summary>
        ///<param name="func">펑션</param>
        public override bool SwitchLA<T1, T2, T3, TFunc>(T1 v1, T2 v2, T3 v3, Func<T1, T2, T3, TFunc, bool> func)
        {
            lock (_lock)
            {
                if (Count == 0) { return false; }

                bool result = false;

                IsLooping = true;

                for (int i = 0; i < Observers.Count; i++)
                {
                    var item = Observers[i];

                    if (item is TFunc X)
                    {
                        bool thisFunc = func(v1, v2, v3, X);

                        //? 펑션의 반환값이 true 일 경우에만 true 반환
                        if (thisFunc == true) { result = true; }
                    }
                }

                IsLooping = false;

                return result;
            }
        }


        ///<summary>[스위치 : 전용 옵저버] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 스위치 실행, 한번이라도 그 스위치의 값이 true일경우 true 반환</summary>
        ///<param name="func">펑션</param>
        public override bool Switch<TSwitch>()
        {
            lock (_lock)
            {
                if (Count == 0) { return false; }

                bool result = false;

                IsLooping = true;

                for (int i = 0; i < Observers.Count; i++)
                {
                    var item = Observers[i];

                    if (item is TSwitch X)
                    {
                        bool thisFunc = X.Switch();

                        //? 펑션의 반환값이 true 일 경우에만 true 반환
                        if (thisFunc == true) { result = true; }
                    }
                }

                IsLooping = false;

                return result;
            }
        }

        ///<summary>[스위치 : 전용 옵저버 매개변수 1] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 스위치 실행, 한번이라도 그 스위치의 값이 true일경우 true 반환</summary>
        ///<param name="func">펑션</param>
        public override bool Switch<T1, TSwitch>(T1 v1)
        {
            lock (_lock)
            {
                if (Count == 0) { return false; }

                bool result = false;

                IsLooping = true;

                for (int i = 0; i < Observers.Count; i++)
                {
                    var item = Observers[i];

                    if (item is TSwitch X)
                    {
                        bool thisFunc = X.Switch(v1);

                        //? 펑션의 반환값이 true 일 경우에만 true 반환
                        if (thisFunc == true) { result = true; }
                    }
                }

                IsLooping = false;

                return result;
            }
        }

        ///<summary>[스위치 : 전용 옵저버 매개변수 2] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 스위치 실행, 한번이라도 그 스위치의 값이 true일경우 true 반환</summary>
        ///<param name="func">펑션</param>
        public override bool Switch<T1, T2, TSwitch>(T1 v1, T2 v2)
        {
            lock (_lock)
            {
                if (Count == 0) { return false; }

                bool result = false;

                IsLooping = true;

                for (int i = 0; i < Observers.Count; i++)
                {
                    var item = Observers[i];

                    if (item is TSwitch X)
                    {
                        bool thisFunc = X.Switch(v1, v2);

                        //? 펑션의 반환값이 true 일 경우에만 true 반환
                        if (thisFunc == true) { result = true; }
                    }
                }

                IsLooping = false;

                return result;
            }
        }

        ///<summary>[스위치 : 전용 옵저버 매개변수 2] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 스위치 실행, 한번이라도 그 스위치의 값이 true일경우 true 반환</summary>
        ///<param name="func">펑션</param>
        public override bool Switch<T1, T2, T3, TSwitch>(T1 v1, T2 v2, T3 v3)
        {
            lock (_lock)
            {
                if (Count == 0) { return false; }

                bool result = false;

                IsLooping = true;

                for (int i = 0; i < Observers.Count; i++)
                {
                    var item = Observers[i];

                    if (item is TSwitch X)
                    {
                        bool thisFunc = X.Switch(v1, v2, v3);

                        //? 펑션의 반환값이 true 일 경우에만 true 반환
                        if (thisFunc == true) { result = true; }
                    }
                }

                IsLooping = false;

                return result;
            }
        }

        #endregion



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 단 하나의 옵저버만 허용하는 단일 옵저버<br/>
    ///<typeparamref name="TOB"/> 를 상속받은 대상만 관리가 가능
    /// </summary>
    /// <typeparam name="TOB"></typeparam>
    [Serializable]
    public class Observer_One<TOB> : BaseObserver<TOB> where TOB : class, IObserver
    {
        ///======================================================================================================================================================



        ///<summary>
        /// 현재 옵저버
        /// </summary>
        public TOB Observer { get; private set; }



        /// <summary>
        /// 옵저버 등급
        /// </summary>
        public int Rank { get; private set; } = 0;



        ///======================================================================================================================================================




        /// <summary>
        /// 옵저버 지정
        /// </summary>
        /// <param name="ob">대상 옵저버</param>
        /// <param name="rank">추가할 옵저버 등급</param>
        /// <param name="overlap">중첩 여부</param>
        /// <returns></returns>
        public virtual bool SetOB(TOB ob, int rank = 0, bool overlap = true)
        {
            //? 이미 옵저버가 들어있다
            if (Observer != null)
            {

                //? 중첩 허용, 받아온 랭크가 기존 랭크보다 크거나 같으면 덮어씌운다
                if (overlap && Rank <= rank)
                {
                    Observer = ob;
                    Rank = rank;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            //? 옵저버가 없으니 새로 넣는다
            else
            {

                Observer = ob;
                Rank = rank;
                return true;
            }
        }


        /// <summary>
        /// 옵저버 지정 (무조건)
        /// </summary>
        /// <param name="ob">대상 옵저버</param>
        /// <param name="setRankZero">등급을 0으로 초기화하기</param>
        public void SetOB_Absolute(TOB ob, bool setRankZero = false)
        {
            Observer = ob;

            if (setRankZero)
            {
                Rank = 0;
            }
        }



        ///======================================================================================================================================================


        /// <summary>
        /// 옵저버 제거
        /// </summary>
        /// <param name="rank">제거 등급</param>
        /// <returns></returns>
        public virtual bool ClearOB(int rank)
        {
            //? 받아온 랭크가 기존 랭크보다 크거나 같으면 제거한다
            if (Rank <= rank)
            {
                Observer = null;
                return true;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// 옵저버 제거 (직접)
        /// </summary>
        /// <param name="ob">제거할 옵저버, 같아야 제거</param>
        /// <returns></returns>
        public virtual bool ClearOB(TOB ob)
        {
            if (ob == Observer)
            {
                Observer = null;
                return true;
            }
            return false;
        }


        /// <summary>
        /// 옵저버 제거 (무조건)
        /// </summary>
        /// <param name="setRankZero">등급을 0으로 초기화하기</param>
        public virtual void ClearOB_Absolute(bool setRankZero = false)
        {
            Observer = null;

            if (setRankZero)
            {
                Rank = 0;
            }
        }



        ///======================================================================================================================================================



        #region 옵저버 실행

        ///<summary>[알람 : 람다 액션] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 액션 실행</summary>
        ///<param name="act">액션</param>
        public override void AlarmLA<TAction>(Action<TAction> act)
        {
            lock (_lock)
            {
                if (Observer == null) { return; }

                if (Observer is TAction X) { act?.Invoke(X); }
            }
        }

        ///<summary>[알람 : 람다 액션 매개변수 1] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 액션 실행</summary>
        ///<param name="act">액션</param>
        public override void AlarmLA<T1, TAction>(T1 v1, Action<T1, TAction> act)
        {
            lock (_lock)
            {
                if (Observer == default) { return; }

                if (Observer is TAction X) { act?.Invoke(v1, X); }
            }
        }

        ///<summary>[알람 : 람다 액션 매개변수 2] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 액션 실행</summary>
        ///<param name="act">액션</param>
        public override void AlarmLA<T1, T2, TAction>(T1 v1, T2 v2, Action<T1, T2, TAction> act)
        {
            lock (_lock)
            {
                if (Observer == default) { return; }

                if (Observer is TAction X) { act?.Invoke(v1, v2, X); }
            }
        }

        ///<summary>[알람 : 람다 액션 매개변수 3] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 액션 실행</summary>
        ///<param name="act">액션</param>
        public override void AlarmLA<T1, T2, T3, TAction>(T1 v1, T2 v2, T3 v3, Action<T1, T2, T3, TAction> act)
        {
            lock (_lock)
            {
                if (Observer == null) { return; }

                if (Observer is TAction X) { act?.Invoke(v1, v2, v3, X); }
            }
        }


        ///<summary>
        ///[알람 : 전용옵저버] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 해당 알람 실행
        ///</summary>
        public override void Alarm<TAlarm>()
        {
            lock (_lock)
            {
                if (Observer == null) { return; }

                if (Observer is TAlarm X) { X.Alarm(); }
            }
        }

        ///<summary>
        ///[알람 전용옵저버 매개변수 1] : 모든 옵저버를 순회한다, 추가 매개변수를 받아오며, 받아온 제네릭 타입일경우 해당 알람 실행
        ///</summary>
        public override void Alarm<T1, TAlarm>(T1 v1)
        {
            lock (_lock)
            {
                if (Observer == null) { return; }

                if (Observer is TAlarm X) { X.Alarm(v1); }
            }
        }

        ///<summary>
        ///[알람 전용옵저버 매개변수 2] : 모든 옵저버를 순회한다, 추가 매개변수를 받아오며, 받아온 제네릭 타입일경우 해당 알람 실행
        ///</summary>
        public override void Alarm<T1, T2, TAlarm>(T1 v1, T2 v2)
        {
            lock (_lock)
            {
                if (Observer == null) { return; }

                if (Observer is TAlarm X) { X.Alarm(v1, v2); }
            }
        }

        ///<summary>
        ///[알람 전용옵저버 매개변수 3] : 모든 옵저버를 순회한다, 추가 매개변수를 받아오며, 받아온 제네릭 타입일경우 해당 알람 실행
        ///</summary>
        public override void Alarm<T1, T2, T3, TAlarm>(T1 v1, T2 v2, T3 v3)
        {
            lock (_lock)
            {
                if (Observer == null) { return; }

                if (Observer is TAlarm X) { X.Alarm(v1, v2, v3); }
            }
        }

        ///======================================================================================================================================================

        ///<summary>[대신 : 람다 액션] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 액션 실행, 한번이라도 실행에 성공할경우 true 반환 그외 false</summary>
        ///<param name="act">액션</param>
        public override bool InsteadLA<TAction>(Action<TAction> act)
        {
            lock (_lock)
            {
                if (Observer == null) { return false; }

                bool result = false;

                if (Observer is TAction X) { act?.Invoke(X); result = true; }

                return result;
            }
        }

        ///<summary>[대신 : 람다 액션 매개변수 1] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 액션 실행, 한번이라도 실행에 성공할경우 true 반환 그외 false</summary>
        ///<param name="act">액션</param>
        public override bool InsteadLA<T1, TAction>(T1 v1, Action<T1, TAction> act)
        {
            lock (_lock)
            {
                if (Observer == null) { return false; }

                bool result = false;

                if (Observer is TAction X) { act?.Invoke(v1, X); result = true; }

                return result;
            }
        }

        ///<summary>[대신 : 람다 액션 매개변수 2] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 액션 실행, 한번이라도 실행에 성공할경우 true 반환 그외 false</summary>
        ///<param name="act">액션</param>
        public override bool InsteadLA<T1, T2, TAction>(T1 v1, T2 v2, Action<T1, T2, TAction> act)
        {
            lock (_lock)
            {
                if (Observer == null) { return false; }

                bool result = false;

                if (Observer is TAction X) { act?.Invoke(v1, v2, X); result = true; }
                return result;
            }
        }

        ///<summary>[대신 : 람다 액션 매개변수 3] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 액션 실행, 한번이라도 실행에 성공할경우 true 반환 그외 false</summary>
        ///<param name="act">액션</param>
        public override bool InsteadLA<T1, T2, T3, TAction>(T1 v1, T2 v2, T3 v3, Action<T1, T2, T3, TAction> act)
        {
            lock (_lock)
            {
                if (Observer == null) { return false; }

                bool result = false;

                if (Observer is TAction X) { act?.Invoke(v1, v2, v3, X); result = true; }

                return result;
            }
        }


        ///<summary>[대신 : 전용옵저버] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 해당 알람 실행, 한번이라도 실행에 성공할경우 true 반환 그외 false</summary>
        ///<param name="act">액션</param>
        public override bool Instead<TAlarm>()
        {
            lock (_lock)
            {
                if (Observer == null) { return false; }

                bool result = false;

                if (Observer is TAlarm X) { X.Instead(); result = true; }

                return result;
            }
        }

        ///<summary>[대신 : 전용옵저버 매개변수 1] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 해당 알람 실행, 한번이라도 실행에 성공할경우 true 반환 그외 false</summary>
        ///<param name="act">액션</param>
        public override bool Instead<T1, TAlarm>(T1 v1)
        {
            lock (_lock)
            {
                if (Observer == null) { return false; }

                bool result = false;

                if (Observer is TAlarm X) { X.Instead(v1); result = true; }

                return result;
            }
        }

        ///<summary>[대신 : 전용옵저버 매개변수 2] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 해당 알람 실행, 한번이라도 실행에 성공할경우 true 반환 그외 false</summary>
        ///<param name="act">액션</param>
        public override bool Instead<T1, T2, TAlarm>(T1 v1, T2 v2)
        {
            lock (_lock)
            {
                if (Observer == null) { return false; }

                bool result = false;

                if (Observer is TAlarm X) { X.Instead(v1, v2); result = true; }

                return result;
            }
        }

        ///<summary>[대신 : 전용옵저버 매개변수 3] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 해당 알람 실행, 한번이라도 실행에 성공할경우 true 반환 그외 false</summary>
        ///<param name="act">액션</param>
        public override bool Instead<T1, T2, T3, TAlarm>(T1 v1, T2 v2, T3 v3)
        {
            lock (_lock)
            {
                if (Observer == null) { return false; }

                bool result = false;

                if (Observer is TAlarm X) { X.Instead(v1, v2, v3); result = true; }

                return result;
            }
        }

        ///======================================================================================================================================================

        ///<summary>[스위치 : 람다 액션] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 펑션 실행, 한번이라도 그 펑션의 값이 true일경우 true 반환</summary>
        ///<param name="func">펑션</param>
        public override bool SwitchLA<TAction>(Func<TAction, bool> func)
        {
            lock (_lock)
            {
                if (Observer == null) { return false; }

                bool result = false;

                if (Observer is TAction X)
                {
                    bool thisFunc = func(X);

                    //펑션의 반환값이 true 일 경우에만 true 반환
                    if (thisFunc == true) { result = true; }
                }

                return result;
            }
        }

        ///<summary>[스위치 : 람다 액션 매개변수 1] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 펑션 실행, 한번이라도 그 펑션의 값이 true일경우 true 반환</summary>
        ///<param name="func">펑션</param>
        public override bool SwitchLA<T1, TAction>(T1 v1, Func<T1, TAction, bool> func)
        {
            lock (_lock)
            {
                if (Observer == null) { return false; }

                bool result = false;

                if (Observer is TAction X)
                {
                    bool thisFunc = func(v1, X);

                    //펑션의 반환값이 true 일 경우에만 true 반환
                    if (thisFunc == true) { result = true; }
                }

                return result;
            }
        }

        ///<summary>[스위치 : 람다 액션 매개변수 2] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 펑션 실행, 한번이라도 그 펑션의 값이 true일경우 true 반환</summary>
        ///<param name="func">펑션</param>
        public override bool SwitchLA<T1, T2, TAction>(T1 v1, T2 v2, Func<T1, T2, TAction, bool> func)
        {
            lock (_lock)
            {
                if (Observer == null) { return false; }

                bool result = false;

                if (Observer is TAction X)
                {
                    bool thisFunc = func(v1, v2, X);

                    //펑션의 반환값이 true 일 경우에만 true 반환
                    if (thisFunc == true) { result = true; }
                }

                return result;
            }
        }

        ///<summary>[스위치 : 람다 액션 매개변수 3] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 펑션 실행, 한번이라도 그 펑션의 값이 true일경우 true 반환</summary>
        ///<param name="func">펑션</param>
        public override bool SwitchLA<T1, T2, T3, TAction>(T1 v1, T2 v2, T3 v3, Func<T1, T2, T3, TAction, bool> func)
        {
            lock (_lock)
            {
                if (Observer == null) { return false; }

                bool result = false;

                if (Observer is TAction X)
                {
                    bool thisFunc = func(v1, v2, v3, X);

                    //펑션의 반환값이 true 일 경우에만 true 반환
                    if (thisFunc == true) { result = true; }
                }

                return result;
            }
        }


        ///<summary>[스위치 : 전용 옵저버] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 스위치 실행, 한번이라도 그 스위치의 값이 true일경우 true 반환</summary>
        ///<param name="func">펑션</param>
        public override bool Switch<TSwitch>()
        {
            lock (_lock)
            {
                if (Observer == null) { return false; }

                bool result = false;

                if (Observer is TSwitch X)
                {
                    bool thisFunc = X.Switch();

                    //펑션의 반환값이 true 일 경우에만 true 반환
                    if (thisFunc == true) { result = true; }
                }

                return result;
            }
        }

        ///<summary>[스위치 : 전용 옵저버 매개변수 1] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 스위치 실행, 한번이라도 그 스위치의 값이 true일경우 true 반환</summary>
        ///<param name="func">펑션</param>
        public override bool Switch<T1, TSwitch>(T1 v1)
        {
            lock (_lock)
            {
                if (Observer == null) { return false; }

                bool result = false;

                if (Observer is TSwitch X)
                {
                    bool thisFunc = X.Switch(v1);

                    //펑션의 반환값이 true 일 경우에만 true 반환
                    if (thisFunc == true) { result = true; }
                }

                return result;
            }
        }

        ///<summary>[스위치 : 전용 옵저버 매개변수 2] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 스위치 실행, 한번이라도 그 스위치의 값이 true일경우 true 반환</summary>
        ///<param name="func">펑션</param>
        public override bool Switch<T1, T2, TSwitch>(T1 v1, T2 v2)
        {
            lock (_lock)
            {
                if (Observer == null) { return false; }

                bool result = false;

                if (Observer is TSwitch X)
                {
                    bool thisFunc = X.Switch(v1, v2);

                    //펑션의 반환값이 true 일 경우에만 true 반환
                    if (thisFunc == true) { result = true; }
                }

                return result;
            }
        }

        ///<summary>[스위치 : 전용 옵저버 매개변수 2] : 모든 옵저버를 순회한다, 받아온 제네릭 타입일경우 스위치 실행, 한번이라도 그 스위치의 값이 true일경우 true 반환</summary>
        ///<param name="func">펑션</param>
        public override bool Switch<T1, T2, T3, TSwitch>(T1 v1, T2 v2, T3 v3)
        {
            lock (_lock)
            {
                if (Observer == null) { return false; }

                bool result = false;

                if (Observer is TSwitch X)
                {
                    bool thisFunc = X.Switch(v1, v2, v3);

                    //펑션의 반환값이 true 일 경우에만 true 반환
                    if (thisFunc == true) { result = true; }
                }

                return result;
            }
        }

        #endregion



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 단일 옵저버가 열거형으로 분류된 옵저버
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <typeparam name="TOB"></typeparam>
    [Serializable]
    public class ObserverSet_EnumOnes<TEnum, TOB> where TEnum : struct, Enum, IComparable, IConvertible, IFormattable where TOB : class, IObserver
    {
        ///======================================================================================================================================================



        public ObserverSet_EnumOnes()
        {
            SU_Collection_Enums.SetEnumClassDictionary(out OneObservers);
        }



        ///======================================================================================================================================================


        /// <summary>
        /// 열거형으로 구분된 옵저버 딕셔너리
        /// </summary>
        private readonly Dictionary<TEnum, Observer_One<TOB>> OneObservers;



        ///======================================================================================================================================================



        /// <summary>
        /// 열거형으로 옵저버 얻기
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public Observer_One<TOB> GetOB(TEnum type) => OneObservers[type];



        /// <summary>
        /// 열거형으로 옵저버 얻기
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public Observer_One<TOB> this[TEnum type] => GetOB(type);



        ///======================================================================================================================================================



        /// <summary>
        /// 모든 옵저버 제거
        /// </summary>
        public void ClearOB_Absolute()
        {
            foreach (var ob in OneObservers)
            {
                ob.Value.ClearOB_Absolute();
            }
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 옵저버가 열거형으로 분류된 옵저버
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <typeparam name="TOB"></typeparam>
    [Serializable]
    public class ObserverSet_EnumMains<TEnum, TOB> where TEnum : struct, Enum, IComparable, IConvertible, IFormattable where TOB : class, IObserver
    {
        ///======================================================================================================================================================



        public ObserverSet_EnumMains()
        {
            SU_Collection_Enums.SetEnumClassDictionary(out Observers);
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 열거형으로 구분된 옵저버 딕셔너리
        /// </summary>
        private readonly Dictionary<TEnum, Observer_Main<TOB>> Observers;



        ///======================================================================================================================================================


        /// <summary>
        /// 열거형으로 옵저버 얻기
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public Observer_Main<TOB> GetOB(TEnum type) => Observers[type];



        /// <summary>
        /// 열거형으로 옵저버 얻기
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public Observer_Main<TOB> this[TEnum type] => GetOB(type);



        ///======================================================================================================================================================



        /// <summary>
        /// 모든 옵저버 제거
        /// </summary>
        public void ClearOB()
        {
            foreach (var ob in Observers)
            {
                ob.Value.ClearOB();
            }
        }



        ///======================================================================================================================================================
    }



    #endregion



    ///======================================================================================================================================================



    //? 구독자



    #region 구독자



    public interface ISubscribe_Alarm : IObserver
    {
        void Alarm();
    }
    public interface ISubscribe_Alarm<T> : IObserver
    {
        void Alarm(T value);
    }
    public interface ISubscribe_Alarm<T1, T2> : IObserver
    {
        void Alarm(T1 v1, T2 v2);
    }
    public interface ISubscribe_Alarm<T1, T2, T3> : IObserver
    {
        void Alarm(T1 v1, T2 v2, T3 v3);
    }



    public interface ISubscribe_Instead : IObserver
    {
        bool Instead();
    }
    public interface ISubscribe_Instead<T> : IObserver
    {
        bool Instead(T value);
    }
    public interface ISubscribe_Instead<T1, T2> : IObserver
    {
        bool Instead(T1 v1, T2 v2);
    }
    public interface ISubscribe_Instead<T1, T2, T3> : IObserver
    {
        bool Instead(T1 v1, T2 v2, T3 v3);
    }



    public interface ISubscribe_Switch : IObserver
    {
        bool Switch();
    }
    public interface ISubscribe_Switch<T> : IObserver
    {
        bool Switch(T value);
    }
    public interface ISubscribe_Switch<T1, T2> : IObserver
    {
        bool Switch(T1 v1, T2 v2);
    }
    public interface ISubscribe_Switch<T1, T2, T3> : IObserver
    {
        bool Switch(T1 v1, T2 v2, T3 v3);
    }



    //? 옵저버 이벤트를 캐싱시킨 구독자들



    ///<summary>
    ///옵저버 이벤트를 캐싱하여 저장하는 구독자의 베이스
    /// </summary>
    public abstract class BaseSubscribe<TBase, TDelegate> where TBase : IObserver where TDelegate : Delegate
    {
        /// <summary>
        /// 옵저버 이벤트를 캐싱한 구독자
        /// </summary>
        /// <param name="targetObserver">
        /// 대상 옵저버
        /// </param>
        /// <param name="cachedDelegate">
        /// 대상 델리게이트 이벤트<br/>
        /// <i>람다식도 가능</i>
        /// </param>
        public BaseSubscribe(BaseObserver<TBase> targetObserver, TDelegate cachedDelegate)
        {
            TargetObserver = targetObserver;
            CachedDelegate = cachedDelegate;
        }

        /// <summary>
        /// 대상 옵저버
        /// </summary>
        protected readonly BaseObserver<TBase> TargetObserver;

        /// <summary>
        /// 캐싱된 델리게이트 이벤트
        /// </summary>
        protected readonly TDelegate CachedDelegate;
    }



    /// <summary>
    /// 구독자 : 알람 <i>[파라미터 0]</i>
    /// </summary>
    public sealed class Subscribe_Alarm<TBase, Tob> : BaseSubscribe<TBase, Action<Tob>>, ISubscribe_Alarm where TBase : IObserver where Tob : TBase
    {
        /// <inheritdoc/>
        public Subscribe_Alarm(BaseObserver<TBase> targetObserver, Action<Tob> cachedDelegate) : base(targetObserver, cachedDelegate) { }

        public void Alarm()
        {
            TargetObserver.AlarmLA(CachedDelegate);
        }
    }

    /// <summary>
    /// 구독자 : 알람 <i>[파라미터 1]</i>
    /// </summary>
    public sealed class Subscribe_Alarm<TBase, Tob, T1> : BaseSubscribe<TBase, Action<T1, Tob>>, ISubscribe_Alarm<T1> where TBase : IObserver where Tob : TBase
    {
        /// <inheritdoc/>
        public Subscribe_Alarm(BaseObserver<TBase> targetObserver, Action<T1, Tob> cachedDelegate) : base(targetObserver, cachedDelegate) { }

        public void Alarm(T1 v1)
        {
            TargetObserver.AlarmLA(v1, CachedDelegate);
        }
    }

    /// <summary>
    /// 구독자 : 알람 <i>[파라미터 2]</i>
    /// </summary>
    public sealed class Subscribe_Alarm<TBase, Tob, T1, T2> : BaseSubscribe<TBase, Action<T1, T2, Tob>>, ISubscribe_Alarm<T1, T2> where TBase : IObserver where Tob : TBase
    {
        /// <inheritdoc/>
        public Subscribe_Alarm(BaseObserver<TBase> targetObserver, Action<T1, T2, Tob> cachedDelegate) : base(targetObserver, cachedDelegate) { }

        public void Alarm(T1 v1, T2 v2)
        {
            TargetObserver.AlarmLA(v1, v2, CachedDelegate);
        }
    }

    /// <summary>
    /// 구독자 : 알람 <i>[파라미터 3]</i>
    /// </summary>
    public sealed class Subscribe_Alarm<TBase, Tob, T1, T2, T3> : BaseSubscribe<TBase, Action<T1, T2, T3, Tob>>, ISubscribe_Alarm<T1, T2, T3> where TBase : IObserver where Tob : TBase
    {
        /// <inheritdoc/>
        public Subscribe_Alarm(BaseObserver<TBase> targetObserver, Action<T1, T2, T3, Tob> cachedDelegate) : base(targetObserver, cachedDelegate) { }

        public void Alarm(T1 v1, T2 v2, T3 v3)
        {
            TargetObserver.AlarmLA(v1, v2, v3, CachedDelegate);
        }
    }



    /// <summary>
    /// 구독자 : 대신 <i>[파라미터 0]</i>
    /// </summary>
    public sealed class Subscribe_Instead<TBase, Tob> : BaseSubscribe<TBase, Action<Tob>>, ISubscribe_Instead where TBase : IObserver where Tob : TBase
    {
        /// <inheritdoc/>
        public Subscribe_Instead(BaseObserver<TBase> targetObserver, Action<Tob> cachedDelegate) : base(targetObserver, cachedDelegate) { }

        public bool Instead()
        {
            return TargetObserver.InsteadLA(CachedDelegate);
        }
    }

    /// <summary>
    /// 구독자 : 대신 <i>[파라미터 1]</i>
    /// </summary>
    public sealed class Subscribe_Instead<TBase, Tob, T1> : BaseSubscribe<TBase, Action<T1, Tob>>, ISubscribe_Instead<T1> where TBase : IObserver where Tob : TBase
    {
        /// <inheritdoc/>
        public Subscribe_Instead(BaseObserver<TBase> targetObserver, Action<T1, Tob> cachedDelegate) : base(targetObserver, cachedDelegate) { }

        public bool Instead(T1 v1)
        {
            return TargetObserver.InsteadLA(v1, CachedDelegate);
        }
    }

    /// <summary>
    /// 구독자 : 대신 <i>[파라미터 2]</i>
    /// </summary>
    public sealed class Subscribe_Instead<TBase, Tob, T1, T2> : BaseSubscribe<TBase, Action<T1, T2, Tob>>, ISubscribe_Instead<T1, T2> where TBase : IObserver where Tob : TBase
    {
        /// <inheritdoc/>
        public Subscribe_Instead(BaseObserver<TBase> targetObserver, Action<T1, T2, Tob> cachedDelegate) : base(targetObserver, cachedDelegate) { }

        public bool Instead(T1 v1, T2 v2)
        {
            return TargetObserver.InsteadLA(v1, v2, CachedDelegate);
        }
    }

    /// <summary>
    /// 구독자 : 대신 <i>[파라미터 3]</i>
    /// </summary>
    public sealed class Subscribe_Instead<TBase, Tob, T1, T2, T3> : BaseSubscribe<TBase, Action<T1, T2, T3, Tob>>, ISubscribe_Instead<T1, T2, T3> where TBase : IObserver where Tob : TBase
    {
        /// <inheritdoc/>
        public Subscribe_Instead(BaseObserver<TBase> targetObserver, Action<T1, T2, T3, Tob> cachedDelegate) : base(targetObserver, cachedDelegate) { }

        public bool Instead(T1 v1, T2 v2, T3 v3)
        {
            return TargetObserver.InsteadLA(v1, v2, v3, CachedDelegate);
        }
    }



    /// <summary>
    /// 구독자 : 스위치 <i>[파라미터 0]</i>
    /// </summary>
    public sealed class Subscribe_Switch<TBase, Tob> : BaseSubscribe<TBase, Func<Tob, bool>>, ISubscribe_Switch where TBase : IObserver where Tob : TBase
    {
        /// <inheritdoc/>
        public Subscribe_Switch(BaseObserver<TBase> targetObserver, Func<Tob, bool> cachedDelegate) : base(targetObserver, cachedDelegate) { }

        public bool Switch()
        {
            return TargetObserver.SwitchLA(CachedDelegate);
        }
    }

    /// <summary>
    /// 구독자 : 스위치 <i>[파라미터 1]</i>
    /// </summary>
    public sealed class Subscribe_Switch<TBase, Tob, T1> : BaseSubscribe<TBase, Func<T1, Tob, bool>>, ISubscribe_Switch<T1> where TBase : IObserver where Tob : TBase
    {
        /// <inheritdoc/>
        public Subscribe_Switch(BaseObserver<TBase> targetObserver, Func<T1, Tob, bool> cachedDelegate) : base(targetObserver, cachedDelegate) { }

        public bool Switch(T1 v1)
        {
            return TargetObserver.SwitchLA(v1, CachedDelegate);
        }
    }

    /// <summary>
    /// 구독자 : 스위치 <i>[파라미터 2]</i>
    /// </summary>
    public sealed class Subscribe_Switch<TBase, Tob, T1, T2> : BaseSubscribe<TBase, Func<T1, T2, Tob, bool>>, ISubscribe_Switch<T1, T2> where TBase : IObserver where Tob : TBase
    {
        /// <inheritdoc/>
        public Subscribe_Switch(BaseObserver<TBase> targetObserver, Func<T1, T2, Tob, bool> cachedDelegate) : base(targetObserver, cachedDelegate) { }

        public bool Switch(T1 v1, T2 v2)
        {
            return TargetObserver.SwitchLA(v1, v2, CachedDelegate);
        }
    }

    /// <summary>
    /// 구독자 : 스위치 <i>[파라미터 3]</i>
    /// </summary>
    public sealed class Subscribe_Switch<TBase, Tob, T1, T2, T3> : BaseSubscribe<TBase, Func<T1, T2, T3, Tob, bool>>, ISubscribe_Switch<T1, T2, T3> where TBase : IObserver where Tob : TBase
    {
        /// <inheritdoc/>
        public Subscribe_Switch(BaseObserver<TBase> targetObserver, Func<T1, T2, T3, Tob, bool> cachedDelegate) : base(targetObserver, cachedDelegate) { }

        public bool Switch(T1 v1, T2 v2, T3 v3)
        {
            return TargetObserver.SwitchLA(v1, v2, v3, CachedDelegate);
        }
    }



    #endregion



    ///======================================================================================================================================================



    //? 옵저버 샘플, 워낙 헷갈려서 테스트용도



    #region 옵저버 낙서장



#if UNITY_EDITOR


    [Serializable]
    internal class Example_Observer
    {
        public interface ICurrentObserver : IObserver { }
        public class CurrentObservers : ICurrentObserver { }

        public class AlarmClass : ICurrentObserver, ISubscribe_Alarm
        {
            void ISubscribe_Alarm.Alarm()
            {
                //알람!
            }
        }


        public class AlarmClass2 : ICurrentObserver, ISubscribe_Alarm<int>
        {
            public void Alarm(int value)
            {
                throw new NotImplementedException();
            }
        }



        public Observer_Main<ICurrentObserver> Observer = new Observer_Main<ICurrentObserver>();


        ICurrentObserver CurrentObserver;


        public void AddObserver()
        {
            CurrentObserver = new CurrentObservers();
            Observer.AddOB(CurrentObserver);
        }

        public void AlarmObserver()
        {
            Observer.Alarm<AlarmClass>();
            Observer.Alarm<int, AlarmClass2>(0);
        }
    }



#endif 



    #endregion



    ///======================================================================================================================================================



    namespace Legacy
    {
        public abstract class BaseFunc_Switcher<TFunc>
        {
            ///======================================================================================================================================================



            public BaseFunc_Switcher(int capacity)
            {
                HashSet = new HashSet<TFunc>(capacity);
            }



            ///======================================================================================================================================================



            protected readonly HashSet<TFunc> HashSet;



            protected List<TFunc> Waiting_Add_List = new List<TFunc>();
            protected List<TFunc> Waiting_Remove_List = new List<TFunc>();



            ///======================================================================================================================================================



            public bool IsLooping
            {
                get => isLooping;
                protected set
                {
                    //? Loop가 종료되서 온거라면
                    if (isLooping == true && value == false)
                    {
                        isLooping = value;
                        Waiting_Event();
                        return;
                    }

                    isLooping = value;
                }
            }
            private bool isLooping;



            ///======================================================================================================================================================


            public void Add(TFunc value)
            {
                if (IsLooping)
                {
                    Waiting_Add_List.Add(value);
                    return;
                }

                if (value != null) { HashSet.Add(value); }
            }



            public void Remove(TFunc value)
            {
                if (IsLooping)
                {
                    Waiting_Remove_List.Add(value);
                    return;
                }

                if (value != null) { HashSet.Remove(value); }
            }



            ///======================================================================================================================================================



            protected void Waiting_Event()
            {
                if (Waiting_Add_List.Count != 0)
                {
                    foreach (var adder in Waiting_Add_List) { Add(adder); }
                    Waiting_Add_List.Clear();
                }

                if (Waiting_Remove_List.Count != 0)
                {
                    foreach (var remover in Waiting_Remove_List) { Remove(remover); }
                    Waiting_Remove_List.Clear();
                }
            }



            ///======================================================================================================================================================
        }



        public class Func_Switcher : BaseFunc_Switcher<Func<bool>>
        {
            ///======================================================================================================================================================



            public Func_Switcher(int capacity) : base(capacity) { }



            ///======================================================================================================================================================



            public bool Switcher()
            {
                if (HashSet.Count == 0) { return false; }

                IsLooping = true;
                bool result = false;


                foreach (var func in HashSet)
                {
                    bool thisFunc = func.Invoke();

                    //펑션의 반환값이 true 일 경우에만 true 반환
                    if (thisFunc == true) { result = true; }
                }


                IsLooping = false;
                return result;
            }



            ///======================================================================================================================================================
        }



        public class Func_Switcher<T1> : BaseFunc_Switcher<Func<T1, bool>>
        {
            ///======================================================================================================================================================



            public Func_Switcher(int capacity) : base(capacity) { }



            ///======================================================================================================================================================



            public bool Switcher(T1 v)
            {
                if (HashSet.Count == 0) { return false; }

                IsLooping = true;
                bool result = false;


                foreach (var func in HashSet)
                {
                    bool thisFunc = func.Invoke(v);

                    //펑션의 반환값이 true 일 경우에만 true 반환
                    if (thisFunc == true) { result = true; }
                }


                IsLooping = false;
                return result;
            }



            ///======================================================================================================================================================
        }



        public class Func_Switcher<T1, T2> : BaseFunc_Switcher<Func<T1, T2, bool>>
        {
            ///======================================================================================================================================================



            public Func_Switcher(int capacity) : base(capacity) { }



            ///======================================================================================================================================================



            public bool Switcher(T1 v1, T2 v2)
            {
                if (HashSet.Count == 0) { return false; }

                IsLooping = true;
                bool result = false;


                foreach (var func in HashSet)
                {
                    bool thisFunc = func.Invoke(v1, v2);

                    //펑션의 반환값이 true 일 경우에만 true 반환
                    if (thisFunc == true) { result = true; }
                }


                IsLooping = false;
                return result;
            }



            ///======================================================================================================================================================
        }



        public class Func_Switcher<T1, T2, T3> : BaseFunc_Switcher<Func<T1, T2, T3, bool>>
        {
            ///======================================================================================================================================================



            public Func_Switcher(int capacity) : base(capacity) { }



            ///======================================================================================================================================================



            public bool Switcher(T1 v1, T2 v2, T3 v3)
            {
                if (HashSet.Count == 0) { return false; }

                IsLooping = true;
                bool result = false;


                foreach (var func in HashSet)
                {
                    bool thisFunc = func.Invoke(v1, v2, v3);

                    //펑션의 반환값이 true 일 경우에만 true 반환
                    if (thisFunc == true) { result = true; }
                }


                IsLooping = false;
                return result;
            }



            ///======================================================================================================================================================
        }

    }



    ///======================================================================================================================================================



    //? 미니 옵저버



    #region 미니 옵저버



    /// <summary>
    /// 미니 옵저버 이벤트들의 기본 기능을 정의하는 추상 클래스입니다.
    /// 특정 값이나 이벤트를 알람, 스위치 형태로 관리할 수 있게 해줍니다.
    /// </summary>
    [Serializable]
    public abstract class BaseMiniObserverEvents
    {
        /// <summary>
        /// 이벤트 작동 방식을 구분하는 열거형입니다.
        /// Normal은 일반 모드, AbsAlarm은 Switch 결과와 관계없이 알람을 수행합니다.
        /// </summary>
        public enum ESetType
        {
            /// <summary>
            /// 일반 모드
            /// </summary>
            Normal,

            /// <summary>
            /// Switch 여부와 관계없이 무조건 알람이 작동되는 모드
            /// </summary>
            AbsAlarm,
        }

        /// <summary>
        /// 생성자를 통해 ESetType을 설정합니다.
        /// </summary>
        /// <param name="setType">이벤트 작동 방식 (Normal / AbsAlarm)</param>
        public BaseMiniObserverEvents(ESetType setType = ESetType.Normal)
        {
            SetType = setType;
        }

        /// <summary>
        /// 이벤트 작동 방식을 나타냅니다.
        /// </summary>
        public readonly ESetType SetType;
    }



    /// <summary>
    /// 숫자나 구조체(ValueType)를 감시하는 옵저버 이벤트의 기본 추상 클래스입니다.
    /// </summary>
    /// <typeparam name="TValue">감시할 값의 타입(구조체)</typeparam>
    [Serializable]
    public abstract class BaseMiniObserverEventValue<TValue> : BaseMiniObserverEvents where TValue : struct
    {
        /// <summary>
        /// 리셋 시 호출되는 함수를 설정하고, ESetType을 기반으로 초기화합니다.
        /// </summary>
        /// <param name="resetValueEvent">값을 재설정할 때 호출할 델리게이트</param>
        /// <param name="setType">이벤트 작동 방식</param>
        public BaseMiniObserverEventValue(Func<TValue> resetValueEvent, ESetType setType = ESetType.Normal)
            : base(setType)
        {
            ResetValueEvent = resetValueEvent;
            ResetValue();
        }

        /// <summary>
        /// 값을 재설정할 때 사용되는 델리게이트입니다.
        /// </summary>
        protected readonly Func<TValue> ResetValueEvent;

        /// <summary>
        /// 옵저버가 감시하는 값을 기본값(또는 초기값)으로 리셋합니다.
        /// </summary>
        public abstract void ResetValue();

        /// <summary>
        /// 현재 등록된 알람/스위치 이벤트를 모두 해제합니다.
        /// </summary>
        public abstract void ClearEvent();

        /// <summary>
        /// 값을 리셋하고, 모든 이벤트를 제거합니다.
        /// </summary>
        public void Reset()
        {
            ResetValue();
            ClearEvent();
        }
    }



    /// <summary>
    /// 단일 값(TValue)을 감시하는 미니 옵저버 클래스입니다.
    /// AlarmEvent, SwitchEvent로 확장된 이벤트 흐름을 지원합니다.
    /// </summary>
    /// <typeparam name="TValue">감시할 값의 타입(구조체)</typeparam>
    [Serializable]
    public class MiniObserverEventValue<TValue> : BaseMiniObserverEventValue<TValue> where TValue : struct
    {
        /// <summary>
        /// 생성자에서 값 리셋 함수를 받아 초기화합니다.
        /// </summary>
        /// <param name="resetValueEvent">값 리셋 델리게이트</param>
        /// <param name="setType">이벤트 작동 방식</param>
        public MiniObserverEventValue(Func<TValue> resetValueEvent, ESetType setType = ESetType.Normal)
            : base(resetValueEvent, setType)
        {
        }

        /// <summary>
        /// MiniObserverEventValue를 TValue로 암시적 변환할 수 있게 해줍니다.
        /// </summary>
        /// <param name="value">MiniObserverEventValue 인스턴스</param>
        public static implicit operator TValue(MiniObserverEventValue<TValue> value) => value.Value;

        /// <summary>
        /// 감시 대상 값. set 시에 내부 SetValue 로직을 통해 Alarm/Switch가 동작합니다.
        /// </summary>
        public TValue Value
        {
            get => this.value;
            set
            {
                SetValue(value);
            }
        }

        private TValue value; //? 실제 데이터를 저장하는 필드

        /// <summary>
        /// 값이 변경될 때 호출되는 알람 이벤트입니다.
        /// </summary>
        public event Action<TValue> AlarmEvent = null;

        /// <summary>
        /// 값 변경 시 true를 반환하면, 메인 이벤트(값 할당)를 막는 스위치 이벤트입니다.
        /// </summary>
        public event Func<TValue, bool> SwitchEvent = null;

        /// <summary>
        /// 값을 설정하고, SwitchEvent 및 AlarmEvent를 처리합니다.
        /// </summary>
        /// <param name="value">새로 설정할 값</param>
        protected void SetValue(TValue value)
        {
            var tempValue = value;

            switch (SetType)
            {
                case ESetType.Normal:
                //? SwitchEvent가 null이거나, false를 반환해야 실제 값이 갱신됨
                if (SwitchEvent == null || SwitchEvent.Invoke(tempValue) == false)
                {
                    this.value = tempValue;
                    AlarmEvent?.Invoke(tempValue);
                }
                break;

                case ESetType.AbsAlarm:
                //? SwitchEvent가 true를 반환하더라도 AlarmEvent는 무조건 실행
                if (SwitchEvent == null || SwitchEvent.Invoke(tempValue) == false)
                {
                    this.value = tempValue;
                    AlarmEvent?.Invoke(tempValue);
                }
                else
                {
                    AlarmEvent?.Invoke(tempValue);
                }
                break;
            }
        }

        /// <summary>
        /// Alarm/Switch 이벤트를 트리거하지 않고 조용히 값을 설정합니다.
        /// </summary>
        /// <param name="value">새로 설정할 값</param>
        public void SetValue_Quiet(TValue value)
        {
            this.value = value;
        }

        /// <summary>
        /// ResetValueEvent를 통해 값을 다시 불러온 후, Value에 반영합니다.
        /// </summary>
        public override void ResetValue()
        {
            Value = ResetValueEvent.Invoke();
        }

        /// <summary>
        /// AlarmEvent, SwitchEvent에 등록된 델리게이트를 모두 초기화합니다.
        /// </summary>
        public override void ClearEvent()
        {
            AlarmEvent = null;
            SwitchEvent = null;
        }
    }



    /// <summary>
    /// 추가로 제네릭 매개변수 T를 포함하여, (TValue, T) 형태로 확장된 미니 옵저버 클래스입니다.
    /// </summary>
    /// <typeparam name="TValue">감시할 값의 타입(구조체)</typeparam>
    /// <typeparam name="T">추가로 함께 전달될 타입</typeparam>
    [Serializable]
    public class MiniObserverEventValue<TValue, T> : BaseMiniObserverEventValue<TValue> where TValue : struct
    {
        /// <summary>
        /// 생성자에서 값 리셋 함수를 받아 초기화합니다.
        /// </summary>
        /// <param name="resetValueEvent">값 리셋 델리게이트</param>
        /// <param name="setType">이벤트 작동 방식</param>
        public MiniObserverEventValue(Func<TValue> resetValueEvent, ESetType setType = ESetType.Normal)
            : base(resetValueEvent, setType)
        {
        }

        /// <summary>
        /// MiniObserverEventValue를 TValue로 암시적 변환합니다.
        /// </summary>
        /// <param name="value">MiniObserverEventValue 인스턴스</param>
        public static implicit operator TValue(MiniObserverEventValue<TValue, T> value) => value.Value;

        /// <summary>
        /// 감시 대상 값. SetValue 등에서 설정을 담당합니다.
        /// </summary>
        public TValue Value
        {
            get => this.value;
            private set => this.value = value;
        }

        private TValue value; //? 실제 데이터를 저장

        /// <summary>
        /// 값이 변경될 때 호출되는 알람 이벤트 (값과 함께 T를 추가로 전달).
        /// </summary>
        public event Action<TValue, T> AlarmEvent = null;

        /// <summary>
        /// 값 변경 시 true를 반환하면 할당을 막는 스위치 이벤트 (값과 함께 T를 전달).
        /// </summary>
        public event Func<TValue, T, bool> SwitchEvent = null;

        /// <summary>
        /// T와 함께 값을 설정하고, SwitchEvent/AlarmEvent를 처리합니다.
        /// </summary>
        /// <param name="t1">추가 정보</param>
        /// <param name="value">새로 설정할 값</param>
        protected void SetValue(T t1, TValue value)
        {
            var tempValue = value;

            switch (SetType)
            {
                case ESetType.Normal:
                if (SwitchEvent == null || SwitchEvent.Invoke(tempValue, t1) == false)
                {
                    this.value = tempValue;
                    AlarmEvent?.Invoke(tempValue, t1);
                }
                break;

                case ESetType.AbsAlarm:
                if (SwitchEvent == null || SwitchEvent.Invoke(tempValue, t1) == false)
                {
                    this.value = tempValue;
                    AlarmEvent?.Invoke(tempValue, t1);
                }
                else
                {
                    AlarmEvent?.Invoke(tempValue, t1);
                }
                break;
            }
        }

        /// <summary>
        /// 값만 조용히 변경합니다. 알람이나 스위치는 발생하지 않습니다.
        /// </summary>
        /// <param name="value">새 값</param>
        public void SetValue_Quiet(TValue value)
        {
            this.value = value;
        }

        /// <summary>
        /// ResetValueEvent를 통해 값을 다시 불러온 후, Value에 반영합니다.
        /// </summary>
        public override void ResetValue()
        {
            Value = ResetValueEvent.Invoke();
        }

        /// <summary>
        /// AlarmEvent, SwitchEvent에 등록된 모든 델리게이트를 초기화합니다.
        /// </summary>
        public override void ClearEvent()
        {
            AlarmEvent = null;
            SwitchEvent = null;
        }
    }



    /// <summary>
    /// 미니 옵저버 이벤트의 기본 추상 클래스입니다. ESetType만 관리하고, 알람/스위치가 없는 이벤트 구조를 가질 수 있습니다.
    /// </summary>
    [Serializable]
    public abstract class MiniBaseObserverEvent : BaseMiniObserverEvents
    {
        /// <summary>
        /// 생성 시 ESetType을 지정합니다.
        /// </summary>
        /// <param name="setType">이벤트 작동 방식</param>
        public MiniBaseObserverEvent(ESetType setType) : base(setType) { }

        /// <summary>
        /// 등록된 이벤트를 모두 해제하는 추상 메서드입니다.
        /// </summary>
        public abstract void ClearEvent();
    }



    /// <summary>
    /// 단순한 메인 이벤트를 감시하는 미니 옵저버 클래스입니다.
    /// </summary>
    [Serializable]
    public class MiniObserverEvent : MiniBaseObserverEvent
    {
        /// <summary>
        /// 생성 시 ESetType과 MainEvent를 설정합니다.
        /// </summary>
        /// <param name="setType">이벤트 작동 방식</param>
        /// <param name="mainEvent">메인 이벤트(실제로 실행될 동작)</param>
        public MiniObserverEvent(ESetType setType, Action mainEvent) : base(setType)
        {
            MainEvent = mainEvent;
        }

        private readonly Action MainEvent; //? 실제 실행되는 메인 동작

        /// <summary>
        /// 메인 이벤트가 실행된 후 알람을 발생시키는 이벤트입니다.
        /// </summary>
        public event Action AlarmEvent = null;

        /// <summary>
        /// 메인 이벤트 실행 전, true를 반환하면 메인 이벤트를 막는 스위치 이벤트입니다.
        /// </summary>
        public event Func<bool> SwitchEvent = null;

        /// <summary>
        /// 메인 이벤트를 실행하고, SwitchEvent와 AlarmEvent를 처리합니다.
        /// </summary>
        public void Execute()
        {
            switch (SetType)
            {
                case ESetType.Normal:
                //? SwitchEvent == null이거나 false여야 MainEvent 실행
                if (SwitchEvent == null || SwitchEvent.Invoke() == false)
                {
                    MainEvent?.Invoke();
                    AlarmEvent?.Invoke();
                }
                break;

                case ESetType.AbsAlarm:
                //? SwitchEvent가 true여도 AlarmEvent는 무조건 실행
                if (SwitchEvent == null || SwitchEvent.Invoke() == false)
                {
                    MainEvent?.Invoke();
                    AlarmEvent?.Invoke();
                }
                else
                {
                    AlarmEvent?.Invoke();
                }
                break;
            }
        }

        /// <summary>
        /// 등록된 AlarmEvent, SwitchEvent를 모두 제거합니다.
        /// </summary>
        public override void ClearEvent()
        {
            AlarmEvent = null;
            SwitchEvent = null;
        }
    }



    /// <summary>
    /// 제네릭 매개변수를 사용하여, 실행 시에 추가 인자(T)를 전달하는 미니 옵저버 이벤트입니다.
    /// </summary>
    /// <typeparam name="T">메인 이벤트와 알람 이벤트에 전달될 타입</typeparam>
    [Serializable]
    public class MiniObserverEvent<T> : MiniBaseObserverEvent
    {
        /// <summary>
        /// 생성자에서 ESetType과 메인 이벤트를 설정합니다.
        /// </summary>
        /// <param name="setType">이벤트 작동 방식</param>
        /// <param name="mainEvent">메인 이벤트</param>
        public MiniObserverEvent(ESetType setType, Action mainEvent) : base(setType)
        {
            MainEvent = mainEvent;
        }

        private readonly Action MainEvent; //? 실제 실행되는 메인 동작

        /// <summary>
        /// 메인 이벤트 실행 후, 추가 인자 T와 함께 알람을 발생시키는 이벤트입니다.
        /// </summary>
        public event Action<T> AlarmEvent = null;

        /// <summary>
        /// 메인 이벤트 실행 전, (T를 파라미터로) true를 반환하면 메인 이벤트를 막는 스위치 이벤트입니다.
        /// </summary>
        public event Func<T, bool> SwitchEvent = null;

        /// <summary>
        /// 메인 이벤트를 실행하고, SwitchEvent와 AlarmEvent를 처리합니다.
        /// </summary>
        /// <param name="t1">실행 시 전달할 인자</param>
        public void Execute(T t1)
        {
            switch (SetType)
            {
                case ESetType.Normal:
                if (SwitchEvent == null || SwitchEvent.Invoke(t1) == false)
                {
                    MainEvent?.Invoke();
                    AlarmEvent?.Invoke(t1);
                }
                break;

                case ESetType.AbsAlarm:
                if (SwitchEvent == null || SwitchEvent.Invoke(t1) == false)
                {
                    MainEvent?.Invoke();
                    AlarmEvent?.Invoke(t1);
                }
                else
                {
                    AlarmEvent?.Invoke(t1);
                }
                break;
            }
        }

        /// <summary>
        /// 등록된 AlarmEvent, SwitchEvent를 모두 제거합니다.
        /// </summary>
        public override void ClearEvent()
        {
            AlarmEvent = null;
            SwitchEvent = null;
        }
    }



    #endregion



    namespace Legacy
    {
        public abstract class BaseMiniObserverEvents
        {
            public enum ESetType
            {
                Normal,
                /// <summary>Switch여도 무조건 알람이 작동함</summary>
                AbsAlarm,
            }



            public BaseMiniObserverEvents(ESetType setType = ESetType.Normal)
            {
                SetType = setType;
            }



            public readonly ESetType SetType;
        }



        public abstract class BaseMiniObserverEventValue<TValue> : BaseMiniObserverEvents where TValue : struct
        {
            public BaseMiniObserverEventValue(Func<TValue> resetValueEvent, ESetType setType = ESetType.Normal) : base(setType)
            {
                ResetValueEvent = resetValueEvent;
                ResetValue();
            }



            protected readonly Func<TValue> ResetValueEvent;



            public abstract void ResetValue();



            public abstract void ClearEvent();



            public void Reset()
            {
                ResetValue();
                ClearEvent();
            }
        }



        /// <summary>
        /// 미니 옵저버 이벤트 밸류 (옵저버를 사용하는 값을 편하게 클래스로 캡슐화, Alarm + Switch 지원)
        /// </summary>
        public class MiniObserverEventValue<TValue> : BaseMiniObserverEventValue<TValue> where TValue : struct
        {
            public MiniObserverEventValue(Func<TValue> resetValueEvent, ESetType setType = ESetType.Normal) : base(resetValueEvent, setType) { }



            public static implicit operator TValue(MiniObserverEventValue<TValue> value) => value.Value;



            public TValue Value
            {
                get => value;
                set
                {
                    SetValue(value);
                }
            }
            private TValue value;



            public event Action<TValue> AlarmEvent = null;
            public event Func<TValue, bool> SwitchEvent = null;



            protected void SetValue(TValue value)
            {
                var tempValue = value;

                switch (SetType)
                {
                    case ESetType.Normal:

                    if (SwitchEvent == null || SwitchEvent.Invoke(tempValue) == false)
                    {
                        this.value = tempValue;
                        AlarmEvent?.Invoke(tempValue);
                    }

                    break;



                    case ESetType.AbsAlarm:

                    if (SwitchEvent == null || SwitchEvent.Invoke(tempValue) == false)
                    {
                        this.value = tempValue;
                        AlarmEvent?.Invoke(tempValue);
                    }
                    else
                    {
                        AlarmEvent?.Invoke(tempValue);
                    }

                    break;
                }

            }



            public void SetValue_Quiet(TValue value)
            {
                this.value = value;
            }



            public override void ResetValue()
            {
                Value = ResetValueEvent.Invoke();
            }



            public override void ClearEvent()
            {
                AlarmEvent = null;
                SwitchEvent = null;
            }
        }



        /// <summary>
        /// 미니 옵저버 이벤트 밸류 (옵저버처럼 사용하는 값을 편하게 클래스로 캡슐화, Alarm + Switch 지원)
        /// </summary>
        public class MiniObserverEventValue<TValue, T> : BaseMiniObserverEventValue<TValue> where TValue : struct
        {
            public MiniObserverEventValue(Func<TValue> resetValueEvent, ESetType setType = ESetType.Normal) : base(resetValueEvent, setType) { }



            public static implicit operator TValue(MiniObserverEventValue<TValue, T> value) => value.Value;



            public TValue Value
            {
                get => value;
                private set => this.value = value;
            }
            private TValue value;



            public event Action<TValue, T> AlarmEvent = null;
            public event Func<TValue, T, bool> SwitchEvent = null;



            protected void SetValue(T t1, TValue value)
            {
                var tempValue = value;

                switch (SetType)
                {
                    case ESetType.Normal:

                    if (SwitchEvent == null || SwitchEvent.Invoke(tempValue, t1) == false)
                    {
                        this.value = tempValue;
                        AlarmEvent?.Invoke(tempValue, t1);
                    }

                    break;



                    case ESetType.AbsAlarm:

                    if (SwitchEvent == null || SwitchEvent.Invoke(tempValue, t1) == false)
                    {
                        this.value = tempValue;
                        AlarmEvent?.Invoke(tempValue, t1);
                    }
                    else
                    {
                        AlarmEvent?.Invoke(tempValue, t1);
                    }

                    break;
                }
            }



            public void SetValue_Quiet(TValue value)
            {
                this.value = value;
            }



            public override void ResetValue()
            {
                Value = ResetValueEvent.Invoke();
            }



            public override void ClearEvent()
            {
                AlarmEvent = null;
                SwitchEvent = null;
            }
        }



        public abstract class MiniBaseObserverEvent : BaseMiniObserverEvents
        {
            public MiniBaseObserverEvent(ESetType setType) : base(setType) { }

            public abstract void ClearEvent();
        }



        /// <summary>
        /// 미니 옵저버 이벤트 (옵저버처럼 사용하는 함수를 편하게 클래스로 캡슐화, Alarm + Switch 지원)
        /// </summary>
        public class MiniObserverEvent : MiniBaseObserverEvent
        {
            public MiniObserverEvent(ESetType setType, Action mainEvent) : base(setType)
            {
                MainEvent = mainEvent;
            }



            private readonly Action MainEvent;



            public event Action AlarmEvent = null;
            public event Func<bool> SwitchEvent = null;



            public void Execute()
            {
                switch (SetType)
                {
                    case ESetType.Normal:

                    if (SwitchEvent == null || SwitchEvent.Invoke() == false)
                    {
                        MainEvent?.Invoke();
                        AlarmEvent?.Invoke();
                    }

                    break;



                    case ESetType.AbsAlarm:

                    if (SwitchEvent == null || SwitchEvent.Invoke() == false)
                    {
                        MainEvent?.Invoke();
                        AlarmEvent?.Invoke();
                    }
                    else
                    {
                        AlarmEvent?.Invoke();
                    }

                    break;
                }


            }



            public override void ClearEvent()
            {
                AlarmEvent = null;
                SwitchEvent = null;
            }
        }



        /// <summary>
        /// 미니 옵저버 이벤트 (옵저버처럼 사용하는 함수를 편하게 클래스로 캡슐화, Alarm + Switch 지원)
        /// </summary>
        public class MiniObserverEvent<T> : MiniBaseObserverEvent
        {
            public MiniObserverEvent(ESetType setType, Action mainEvent) : base(setType)
            {
                MainEvent = mainEvent;
            }



            private readonly Action MainEvent;



            public event Action<T> AlarmEvent = null;
            public event Func<T, bool> SwitchEvent = null;



            public void Execute(T t1)
            {
                switch (SetType)
                {
                    case ESetType.Normal:

                    if (SwitchEvent == null || SwitchEvent.Invoke(t1) == false)
                    {
                        MainEvent?.Invoke();
                        AlarmEvent?.Invoke(t1);
                    }

                    break;



                    case ESetType.AbsAlarm:

                    if (SwitchEvent == null || SwitchEvent.Invoke(t1) == false)
                    {
                        MainEvent?.Invoke();
                        AlarmEvent?.Invoke(t1);
                    }
                    else
                    {
                        AlarmEvent?.Invoke(t1);
                    }

                    break;
                }
            }



            public override void ClearEvent()
            {
                AlarmEvent = null;
                SwitchEvent = null;
            }
        }
    }



    ///======================================================================================================================================================
}