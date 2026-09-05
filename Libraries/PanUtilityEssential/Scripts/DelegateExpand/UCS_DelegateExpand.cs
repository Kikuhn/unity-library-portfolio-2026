using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Events;
using DG.Tweening;
using Pan.Util;
using UnityEngine.InputSystem;



//? Delegate 확장 요소들을 정리한 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    /// <summary>
    /// 커스텀 실행 이벤트를 정의하는 인터페이스입니다.
    /// </summary>
    public interface ICustomExecuteEvent
    {
        /// <summary>
        /// 실행 이벤트를 시작합니다.
        /// </summary>
        void Start();



        /// <summary>
        /// 실행 이벤트를 중첩합니다.
        /// </summary>
        void Overlap();



        /// <summary>
        /// 실행 이벤트를 종료합니다.
        /// </summary>
        void End();



        /// <summary>
        /// 실행 상태가 진행 중인지 여부를 나타냅니다.
        /// </summary>
        bool StanceRunning { get; }
    }



    /// <summary>
    /// 실행 이벤트를 Delegate로 구현 할 수 있는 클래스입니다.
    /// </summary>
    public sealed class FlexExecuteEvent : ICustomExecuteEvent
    {
        /// <summary>
        /// 실행, 중첩, 종료 이벤트를 설정하는 생성자입니다.
        /// </summary>
        /// <param name="start">시작 이벤트입니다.</param>
        /// <param name="end">종료 이벤트입니다.</param>
        /// <param name="overlap">중첩 이벤트입니다.</param>
        public FlexExecuteEvent(Action start, Action end, Action overlap)
        {
            StartEvent = start;
            EndEvent = end;
            OverlapEvent = overlap;
        }



        bool ICustomExecuteEvent.StanceRunning => IsRunning;



        private bool IsRunning;



        private readonly Action StartEvent;
        private readonly Action EndEvent;
        private readonly Action OverlapEvent;



        void ICustomExecuteEvent.Start()
        {
            IsRunning = true;
            StartEvent?.Invoke();
        }



        void ICustomExecuteEvent.End()
        {
            IsRunning = false;
            EndEvent?.Invoke();
        }



        void ICustomExecuteEvent.Overlap()
        {
            OverlapEvent?.Invoke();
        }
    }



    /// <summary>
    /// 실행 이벤트를 정의하는 추상 클래스입니다.
    /// </summary>
    public abstract class ExecuteEvent : ICustomExecuteEvent
    {
        bool ICustomExecuteEvent.StanceRunning => IsRunning;



        private bool IsRunning;



        void ICustomExecuteEvent.Start()
        {
            IsRunning = true;
            Start();
        }



        void ICustomExecuteEvent.End()
        {
            IsRunning = false;
            End();
        }



        void ICustomExecuteEvent.Overlap()
        {
            Overlap();
        }



        /// <summary>
        /// 실행 이벤트 시작 시 호출되는 메서드입니다. 필요 시 오버라이드하여 구현합니다.
        /// </summary>
        protected virtual void Start() { }



        /// <summary>
        /// 실행 이벤트 종료 시 호출되는 메서드입니다. 필요 시 오버라이드하여 구현합니다.
        /// </summary>
        protected virtual void End() { }



        /// <summary>
        /// 실행 이벤트 중첩 시 호출되는 메서드입니다. 필요 시 오버라이드하여 구현합니다.
        /// </summary>
        protected virtual void Overlap() { }
    }



    /// <summary>
    /// 특정 데이터 타입과 연관된 실행 이벤트를 정의하는 추상 클래스입니다.
    /// </summary>
    /// <typeparam name="T">연관된 데이터 타입입니다.</typeparam>
    public abstract class ExecuteEvent<T> : ExecuteEvent where T : class
    {
        /// <summary>
        /// 연관된 데이터를 설정하는 생성자입니다.
        /// </summary>
        /// <param name="main">연관된 데이터입니다.</param>
        public ExecuteEvent(T main)
        {
            Main = main;
        }



        /// <summary>
        /// 연관된 데이터입니다.
        /// </summary>
        protected readonly T Main;
    }



    ///======================================================================================================================================================



    /// <summary>
    /// 온/오프 스위치 이벤트를 관리하는 클래스입니다.
    /// </summary>
    public class SwitchEvent
    {
        /// <summary>
        /// 온/오프 이벤트를 설정하는 생성자입니다.
        /// </summary>
        /// <param name="onEvent">온(On) 이벤트입니다.</param>
        /// <param name="offEvent">오프(Off) 이벤트입니다.</param>
        public SwitchEvent(Action onEvent, Action offEvent)
        {
            OnEvent = onEvent;
            OffEvent = offEvent;
        }



        private readonly Action OnEvent;
        private readonly Action OffEvent;



        /// <summary>
        /// 스위치 상태를 가져오거나 설정합니다.
        /// </summary>
        public bool Switch
        {
            get => @switch ?? false;
            set
            {
                if (@switch != null && @switch == value) { return; }

                if (value)
                {
                    OnEvent?.Invoke();
                }
                else
                {
                    OffEvent?.Invoke();
                }

                @switch = value;
            }
        }

        private bool? @switch = false;
    }



    /// <summary>
    /// 스위치 이벤트를 확장하여 추가적인 체크 기능을 제공하는 클래스입니다.
    /// </summary>
    public class SwitchEvent_Extend : SwitchEvent
    {
        /// <summary>
        /// 온/오프 이벤트와 상태 체크 이벤트를 설정하는 생성자입니다.
        /// </summary>
        /// <param name="onEvent">온(On) 이벤트입니다.</param>
        /// <param name="offEvent">오프(Off) 이벤트입니다.</param>
        /// <param name="checkEvent">체크 이벤트입니다.</param>
        public SwitchEvent_Extend(Action onEvent, Action offEvent, Action<bool> checkEvent)
            : base(onEvent, offEvent)
        {
            CheckEvent = checkEvent;
        }



        private readonly Action<bool> CheckEvent;



        /// <summary>
        /// 현재 스위치 상태를 확인하고 체크 이벤트를 실행합니다.
        /// </summary>
        /// <returns>현재 스위치 상태를 반환합니다.</returns>
        public bool ExecuteCheck()
        {
            bool @switch = Switch;
            CheckEvent?.Invoke(@switch);
            return @switch;
        }
    }



    ///======================================================================================================================================================
}