using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.IO;
using UnityEngine.Events;
using DG.Tweening;
using Pan.Util;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using JetBrains.Annotations;
using UnityEngine.PlayerLoop;
using System.Text;
using Cysharp.Threading.Tasks;



//? 대개 범용적인 클래스 기반의 확장을 정리한 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    //? Main을 반드시 보유해야하는 클래스



    /// <summary>
    /// 반드시 Main을 보유해야 하는 클래스의 추상 기반 클래스입니다.
    /// </summary>
    /// <typeparam name="TMain">Main으로 사용될 클래스 타입입니다.</typeparam>
    public abstract class MainSlave<TMain> where TMain : class
    {
        /// <summary>
        /// 생성자를 통해 Main을 초기화합니다.
        /// </summary>
        /// <param name="main">Main으로 사용될 객체입니다.</param>
        public MainSlave(TMain main)
        {
            Main = main;
        }



        /// <summary>
        /// Main으로 설정된 객체입니다.
        /// </summary>
        protected readonly TMain Main;
    }



    /// <summary>
    /// (직렬화됨) 반드시 Main을 보유해야 하는 클래스의 추상 기반 클래스입니다.
    /// </summary>
    /// <typeparam name="TMain">Main으로 사용될 클래스 타입입니다.</typeparam>
    public abstract class MainSlaveSerialized<TMain> where TMain : class
    {
        /// <summary>
        /// 생성자를 통해 Main을 초기화합니다.
        /// </summary>
        /// <param name="main">Main으로 사용될 객체입니다.</param>
        public MainSlaveSerialized(TMain main)
        {
            Main = main;
        }



        /// <summary>
        ///(SerializeReference 직렬화됨)  Main으로 설정된 객체입니다.
        /// </summary>
        [field: SerializeReference][field: HideInInspector] protected TMain Main { get; private set; }
    }



    /// <summary>
    /// 반드시 Main을 보유해야 하며, WakeUp 메서드를 통해 초기화가 필요한 클래스의 추상 기반 클래스입니다.
    /// </summary>
    /// <typeparam name="TMain">Main으로 사용될 클래스 타입입니다.</typeparam>
    public abstract class MainSlave_WakeUpVer<TMain> : IWakeUp<TMain> where TMain : class
    {
        /// <summary>
        /// <see cref="WakeUp(TMain)"/> 의 실행 여부
        /// <para>단순히 실행되었는지 확인하는 용도이며, 중복 <see cref="WakeUp(TMain)"/> 호출을 막는 플래그 기능은 없음</para>
        /// </summary>
        public bool IsWakeUp { get; private set; }



        /// <summary>
        /// WakeUp 메서드를 통해 Main을 초기화합니다.
        /// </summary>
        /// <param name="main">Main으로 사용될 객체입니다.</param>
        public virtual void WakeUp(TMain main)
        {
            Main = main;
            IsWakeUp = true;
        }



        /// <summary>
        /// Main으로 설정된 객체입니다.
        /// </summary>
        protected TMain Main { get; private set; }
    }



    /// <summary>
    /// (직렬화됨) 반드시 Main을 보유해야 하며, WakeUp 메서드를 통해 초기화가 필요한 클래스의 추상 기반 클래스입니다.
    /// </summary>
    /// <typeparam name="TMain">Main으로 사용될 클래스 타입입니다.</typeparam>
    public abstract class MainSlaveSerialized_WakeUpVer<TMain> : IWakeUp<TMain> where TMain : class
    {
        /// <summary>
        /// <see cref="WakeUp(TMain)"/> 의 실행 여부
        /// <para>단순히 실행되었는지 확인하는 용도이며, 중복 <see cref="WakeUp(TMain)"/> 호출을 막는 플래그 기능은 없음</para>
        /// </summary>
        public bool IsWakeUp { get; private set; }



        /// <summary>
        /// WakeUp 메서드를 통해 Main을 초기화합니다.
        /// </summary>
        /// <param name="main">Main으로 사용될 객체입니다.</param>
        public virtual void WakeUp(TMain main)
        {
            Main = main;
            IsWakeUp = true;
        }



        /// <summary>
        /// (SerializeReference 직렬화됨) Main으로 설정된 객체입니다.
        /// </summary>
        [field: SerializeReference][field: HideInInspector] protected TMain Main { get; private set; }
    }



    ///======================================================================================================================================================
}