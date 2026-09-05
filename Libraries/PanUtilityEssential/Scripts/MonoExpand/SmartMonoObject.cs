using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Events;
using DG.Tweening;
using Pan.Util;



//? [Monobehaviour Expand] 스마트-모노-오브젝트, 유니티 Monobehaviour에 등록된 인터페이스의 일부를 자동으로 작동시켜주는 오브젝트 가 정리되어있는 정도의 문서



namespace Pan.Util
{

    ///<summary>
    ///스마트-모노-오브젝트
    ///<para>Enable, Disable, Destroy 마다 등록된 이벤트를 자동으로 호출하고 관리한다</para>
    /// </summary>
    public class SmartMonoObject<T> : MonoBehaviour,
        IOnEnableEvent<T>, IOnDisableEvent<T>, IOnDestroyEvent<T>,
        IOnEnableEvent_Eternal<T>, IOnDisableEvent_Eternal<T>
        where T : SmartMonoObject<T>
    {
        ///======================================================================================================================================================



        //? 이벤트



        ///<inheritdoc/>
        public event Action<T> OnEnableEvent = null;
        ///<inheritdoc/>
        public event Action<T> OnDisableEvent = null;
        ///<inheritdoc/>
        public event Action<T> OnDestroyEvent = null;



        ///<inheritdoc/>
        public event Action<T> OnEnableEvent_Eternal = null;
        ///<inheritdoc/>
        public event Action<T> OnDisableEvent_Eternal = null;



        ///======================================================================================================================================================



        //? 이벤트 (일괄 Static)



        /// <summary>
        /// (일괄 Static) 객체의 <b>활성화 (Enable)</b> 에서 실행되는 이벤트
        /// <para><b>직접 해제하지 않는 한, 초기화 되지 않음</b></para>
        /// </summary>
        public static event Func<T, bool> OnEnableEvent_Everyone = null;



        /// <summary>
        /// (일괄 Static) 객체의 <b>비활성화 (Disable)</b> 에서 실행되는 이벤트
        /// <para><b>직접 해제하지 않는 한, 초기화 되지 않음</b></para>
        /// </summary>
        public static event Func<T, bool> OnDisableEvent_Everyone = null;



        /// <summary>
        /// (일괄 Static) 객체의 <b>파괴 (Destroy)</b> 에서 실행되는 이벤트
        /// <para><b>직접 해제하지 않는 한, 초기화 되지 않음</b></para>
        /// </summary>
        public static event Func<T, bool> OnDestroyEvent_Everyone = null;



        ///======================================================================================================================================================



        ///<summary>
        ///OnEnable, 재정의시 반드시 base도 실행해야함
        /// </summary>
        protected virtual void OnEnable()
        {
            //? --- 이벤트 실행 시작 ---
            T current = this as T;
            OnEnableEvent?.Invoke(current);
            OnEnableEvent_Eternal?.Invoke(current);
            OnEnableEvent_Everyone?.Invoke(current);
            //? --- 이벤트 실행 종료 ---
        }



        ///<summary>
        ///OnDisable, 재정의시 반드시 base도 실행해야함
        /// </summary>
        protected virtual void OnDisable()
        {
            //? --- 이벤트 실행 시작 ---
            T current = this as T;
            OnDisableEvent?.Invoke(current);
            OnDisableEvent_Eternal?.Invoke(current);
            OnDisableEvent_Everyone?.Invoke(current);
            //? --- 이벤트 실행 종료 ---


            //. Disable 될 때 마다 Enable,Disable 이벤트 초기화
            OnEnableEvent = null;
            OnDisableEvent = null;
        }



        ///<summary>
        ///OnDestroy, 재정의시 반드시 base도 실행해야함
        /// </summary>
        protected virtual void OnDestroy()
        {
            //? --- 이벤트 실행 시작 ---
            T current = this as T;
            OnDestroyEvent?.Invoke(current);
            OnDestroyEvent_Everyone?.Invoke(current);
            //? --- 이벤트 실행 종료 ---

            //. Destroy 될때, 일괄 static 이벤트를 제외하고 이벤트 초기화
            OnEnableEvent = null;
            OnDisableEvent = null;
            OnDestroyEvent = null;
            OnEnableEvent_Eternal = null;
            OnDisableEvent_Eternal = null;
        }



        ///======================================================================================================================================================
    }

}