using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Events;
using DG.Tweening;
using Pan.Util;



//? [Monobehaviour Expand] 유니티의 스크립트 오브젝트의 이벤트쪽 확장 요소들을 정리한 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    //? OnEnable / OnDisable 이벤트



    /// <summary>
    /// OnEnable 이벤트를 정의하는 인터페이스입니다.
    /// </summary>
    /// <typeparam name="TMonobehaviour">이벤트와 관련된 데이터 타입입니다.</typeparam>
    public interface IOnEnableEvent<TMonobehaviour> where TMonobehaviour : MonoBehaviour
    {
        /// <summary>
        /// 객체의 <b>활성화 (Enable)</b> 에서 실행되는 이벤트
        /// </summary>
        event Action<TMonobehaviour> OnEnableEvent;
    }



    /// <summary>
    /// OnDisable 이벤트를 정의하는 인터페이스입니다.
    /// </summary>
    /// <typeparam name="TMonobehaviour">이벤트와 관련된 데이터 타입입니다.</typeparam>
    public interface IOnDisableEvent<TMonobehaviour> where TMonobehaviour : MonoBehaviour
    {
        /// <summary>
        /// 객체의 <b>비활성화 (Disable)</b> 에서 실행되는 이벤트
        /// </summary>
        event Action<TMonobehaviour> OnDisableEvent;
    }



    /// <summary>
    /// OnDestroy 이벤트를 정의하는 인터페이스입니다.
    /// </summary>
    /// <typeparam name="TMonobehaviour">이벤트와 관련된 데이터 타입입니다.</typeparam>
    public interface IOnDestroyEvent<TMonobehaviour> where TMonobehaviour : MonoBehaviour
    {
        /// <summary>
        /// 객체의 <b>파괴 (Destroy)</b> 에서 실행되는 이벤트
        /// </summary>
        event Action<TMonobehaviour> OnDestroyEvent;
    }



    //? OnEnable / OnDisable 이벤트 (Eternal)



    /// <summary>
    /// OnEnable 이벤트를 정의하는 인터페이스입니다.
    /// <para><b>파괴 되거나, 직접 해제하지 않는 한, OnDisable에서 초기화 되지 않음</b></para>
    /// </summary>
    /// <typeparam name="TMonobehaviour">이벤트와 관련된 데이터 타입입니다.</typeparam>
    public interface IOnEnableEvent_Eternal<TMonobehaviour> where TMonobehaviour : MonoBehaviour
    {
        /// <summary>
        /// 객체의 <b>활성화 (Enable)</b> 에서 실행되는 이벤트
        /// <para><b>파괴 되거나, 직접 해제하지 않는 한, OnDisable에서 초기화 되지 않음</b></para>
        /// </summary>
        event Action<TMonobehaviour> OnEnableEvent_Eternal;
    }



    /// <summary>
    /// OnDisable 이벤트를 정의하는 인터페이스입니다.
    /// <para><b>파괴 되거나, 직접 해제하지 않는 한, OnDisable에서 초기화 되지 않음</b></para>
    /// </summary>
    /// <typeparam name="TMonobehaviour">이벤트와 관련된 데이터 타입입니다.</typeparam>
    public interface IOnDisableEvent_Eternal<TMonobehaviour> where TMonobehaviour : MonoBehaviour
    {
        /// <summary>
        /// 객체의 <b>활성화 (Disable)</b> 에서 실행되는 이벤트
        /// <para><b>파괴 되거나, 직접 해제하지 않는 한, OnDisable에서 초기화 되지 않음</b></para>
        /// </summary>
        event Action<TMonobehaviour> OnDisableEvent_Eternal;
    }



    ///======================================================================================================================================================



    #region 폐기 SetOnEnable / SetOnDisable + OnSetOnSwitch (OnEnable, OnDisable 대체용)
    ////? SetOnEnable / SetOnDisable + OnSetOnSwitch (OnEnable, OnDisable 대체용)



    ///// <summary>
    ///// SetOnEnable 메서드를 정의하는 인터페이스입니다.
    ///// </summary>
    //public interface ISetOnEnable
    //{
    //    /// <summary>
    //    /// 객체를 활성화하는 동작을 정의합니다.
    //    /// </summary>
    //    void SetOnEnable();
    //}

    ///// <summary>
    ///// SetOnDisable 메서드를 정의하는 인터페이스입니다.
    ///// </summary>
    //public interface ISetOnDisable
    //{
    //    /// <summary>
    //    /// 객체를 비활성화하는 동작을 정의합니다.
    //    /// </summary>
    //    void SetOnDisable();
    //}

    ///// <summary>
    ///// SetOnEnable 및 SetOnDisable 메서드를 모두 정의하는 인터페이스입니다.
    ///// </summary>
    //public interface ISetOnSwitch : ISetOnEnable, ISetOnDisable { } 
    #endregion



    ///======================================================================================================================================================



    //? 물리 이벤트



    /// <summary>
    /// 2D 물리 충돌 시 OnTriggerEnter 이벤트를 정의하는 인터페이스입니다.
    /// </summary>
    /// <typeparam name="T">이벤트와 관련된 데이터 타입입니다.</typeparam>
    public interface IOnTriggerEnter2DEvent<T>
    {
        /// <summary>
        /// 2D 충돌이 시작될 때 발생하는 이벤트입니다.
        /// </summary>
        event Action<T, Collider2D> OnTriggerEnter2DEvenT;
    }

    /// <summary>
    /// 3D 물리 충돌 시 OnTriggerEnter 이벤트를 정의하는 인터페이스입니다.
    /// </summary>
    /// <typeparam name="T">이벤트와 관련된 데이터 타입입니다.</typeparam>
    public interface IOnTriggerEnterEvent<T>
    {
        /// <summary>
        /// 3D 충돌이 시작될 때 발생하는 이벤트입니다.
        /// </summary>
        event Action<T, Collider> OnTriggerEnterEvenT;
    }

    /// <summary>
    /// 2D 물리 충돌 시 OnTriggerExit 이벤트를 정의하는 인터페이스입니다.
    /// </summary>
    /// <typeparam name="T">이벤트와 관련된 데이터 타입입니다.</typeparam>
    public interface IOnTriggerExit2DEvent<T>
    {
        /// <summary>
        /// 2D 충돌이 종료될 때 발생하는 이벤트입니다.
        /// </summary>
        event Action<T, Collider2D> OnTriggerExit2DEvenT;
    }

    /// <summary>
    /// 3D 물리 충돌 시 OnTriggerExit 이벤트를 정의하는 인터페이스입니다.
    /// </summary>
    /// <typeparam name="T">이벤트와 관련된 데이터 타입입니다.</typeparam>
    public interface IOnTriggerExitEvent<T>
    {
        /// <summary>
        /// 3D 충돌이 종료될 때 발생하는 이벤트입니다.
        /// </summary>
        event Action<T, Collider> OnTriggerExitEvenT;
    }



    ///======================================================================================================================================================
}