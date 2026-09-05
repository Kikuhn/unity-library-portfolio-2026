using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Pan.Util;
using Sirenix.OdinInspector;



namespace Pan.Util
{
    public abstract class CustomScriptableObject : ScriptableObject
    {
        ///======================================================================================================================================================



        ///<summary>
        /// WakeUp 실행 여부 (비직렬화)
        /// </summary>
        [NonSerialized]
        protected bool m_IsWakeUp;



        public bool IsWakeUp => m_IsWakeUp;



        ///======================================================================================================================================================



        //? WakeUp



        ///<summary>
        ///<see cref="WakeUp_ScriptableObject"/>가 구현되어있으니,<br/>
        ///재정의시 반드시 base.OnEnable를 사용할것
        /// </summary>
        protected virtual void OnEnable()
        {
            WakeUp_ScriptableObject();
        }



        protected virtual void Reset()
        {
            WakeUp_ScriptableObject();
        }



        /// <summary>
        /// 스크립터블 오브젝트 WakeUp 시도 (이미 실행되었다면 false 반환)
        /// </summary>
        /// <returns></returns>
        public bool WakeUp_ScriptableObject()
        {
            if (m_IsWakeUp) { return false; }

            m_IsWakeUp = true;
            WakeUps();
            return true;
        }


        [HorizontalGroup("wakeups"), Button(Name = "WakeUps (강제)", Icon = SdfIconType.ExclamationCircle), GUIColor(1, 0.2f, 0), PropertyOrder(-1000)]
        [PropertySpace(SpaceBefore = 10, SpaceAfter = 10)]
        protected void WakeUps()
        {
            WakeUp();
            LateWakeUp();
        }



        [HorizontalGroup("wakeups"), Button(Name = "WakeUp (강제)", Icon = SdfIconType.ExclamationCircle), GUIColor(1, 0.2f, 0), PropertyOrder(-1000)]
        [PropertySpace(SpaceBefore = 10, SpaceAfter = 10)]
        protected virtual void WakeUp() { }



        [HorizontalGroup("wakeups"), Button(Name = "LateWakeUp (강제)", Icon = SdfIconType.ExclamationCircle), GUIColor(1, 0.2f, 0), PropertyOrder(-1000)]
        [PropertySpace(SpaceBefore = 10, SpaceAfter = 10)]
        protected virtual void LateWakeUp() { }



        ///======================================================================================================================================================
    }



    public abstract class CustomScriptableObjectSerialized : SerializedScriptableObject
    {
        ///======================================================================================================================================================



        ///<summary>
        /// WakeUp 실행 여부 (비직렬화)
        /// </summary>
        [NonSerialized]
        protected bool m_IsWakeUp;



        public bool IsWakeUp => m_IsWakeUp;



        ///======================================================================================================================================================



        //? WakeUp



        ///<summary>
        ///<see cref="WakeUp_ScriptableObject"/>가 구현되어있으니,<br/>
        ///재정의시 반드시 base.OnEnable를 사용할것
        /// </summary>
        protected virtual void OnEnable()
        {
            WakeUp_ScriptableObject();
        }



        protected virtual void Reset()
        {
            WakeUp_ScriptableObject();
        }



        /// <summary>
        /// 스크립터블 오브젝트 WakeUp 시도 (이미 실행되었다면 false 반환)
        /// </summary>
        /// <returns></returns>
        public bool WakeUp_ScriptableObject()
        {
            if (m_IsWakeUp) { return false; }

            m_IsWakeUp = true;
            WakeUps();
            return true;
        }



        protected void WakeUps()
        {
            WakeUp();
            LateWakeUp();
        }



        [HorizontalGroup("wakeups"), Button(Name = "WakeUp (강제)", Icon = SdfIconType.ExclamationCircle), GUIColor(1, 0.2f, 0), PropertyOrder(-1000)]
        [PropertySpace(SpaceBefore = 10, SpaceAfter = 10)]
        protected virtual void WakeUp() { }



        [HorizontalGroup("wakeups"), Button(Name = "LateWakeUp (강제)", Icon = SdfIconType.ExclamationCircle), GUIColor(1, 0.2f, 0), PropertyOrder(-1000)]
        [PropertySpace(SpaceBefore = 10, SpaceAfter = 10)]
        protected virtual void LateWakeUp() { }



        ///======================================================================================================================================================
    }
}