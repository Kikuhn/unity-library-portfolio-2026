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



//? 대개 범용적인 클래스 기반의 확장 인터페이스를 정리한 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    //? 클래스 확장 인터페이스 (일반)



    /// <summary>
    /// 객체 이름을 문자열로 얻을수 있는 인터페이스
    /// </summary>
    public interface IName
    {
        /// <summary>
        /// 이 객체 이름.
        /// </summary>
        string CurrentName { get; }
    }



    /// <summary>
    /// 생성 횟수를 나타내는 int 값을 제공하는 인터페이스입니다.
    /// </summary>
    public interface ICreateCount
    {
        /// <summary>
        /// 생성 횟수를 반환합니다.
        /// </summary>
        abstract int CreateCount { get; }
    }



    /// <summary>
    ///  객체 이름 + 생성 횟수를 얻을 수 있는 인터페이스
    /// </summary>
    public interface INamedCreateCount : IName, ICreateCount { }



    /// <summary>
    /// <c>void Copy(T original)</c> 메서드를 제공하는 인터페이스입니다.<br/>
    /// 이 인터페이스를 구현한 쪽이 <b>"붙여넣기"</b> 대상입니다.
    /// </summary>
    /// <typeparam name="T">복사할 원본 데이터의 타입입니다.</typeparam>
    public interface ICopyable<T>
    {
        /// <summary>
        /// 원본 데이터를 복사하여 현재 객체에 붙여넣습니다.
        /// </summary>
        /// <param name="original">복사할 원본 데이터입니다.</param>
        void Copy(T original);
    }



    ///======================================================================================================================================================



    //? 생성자 초기화 대신, 초기화가 필요한 클래스들을 "WakeUp" 로 관리하는 인터페이스



    /// <summary>
    /// 초기화 메서드를 제공하는 인터페이스입니다.<br/>
    /// 생성자를 대신하여 초기화가 필요한 경우 사용됩니다.
    /// </summary>
    public interface IWakeUp
    {
        /// <summary>
        /// 객체를 초기화합니다.
        /// </summary>
        void WakeUp();
    }



    /// <summary>
    /// 지연 초기화 메서드를 제공하는 인터페이스입니다.<br/>
    /// 생성자를 대신하여 초기화가 필요한 경우 사용됩니다.
    /// </summary>
    public interface ILateWakeUp
    {
        /// <summary>
        /// 객체를 지연 초기화합니다.
        /// </summary>
        void LateWakeUp();
    }



    /// <summary>
    /// 제네릭 타입 초기화 메서드를 제공하는 인터페이스입니다.<br/>
    /// 생성자를 대신하여 초기화가 필요한 경우 사용됩니다.
    /// </summary>
    /// <typeparam name="T">초기화에 필요한 주요 데이터의 타입입니다.</typeparam>
    public interface IWakeUp<T>
    {
        /// <summary>
        /// 객체를 초기화합니다.
        /// </summary>
        /// <param name="main">초기화에 필요한 주요 데이터입니다.</param>
        void WakeUp(T main);
    }



    ///======================================================================================================================================================
}
