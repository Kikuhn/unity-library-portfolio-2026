using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Events;
using DG.Tweening;
using Pan.Util;
using Sirenix.OdinInspector;



//? [Monobehaviour Expand] 스마트-모노-오브젝트 + OC



namespace Pan.Util
{
    ///<summary>
    ///스마트-모노-오브젝트 (+ <see cref="ObjectCaching"/>)
    ///<para>Enable, Disable, Destroy 마다 등록된 이벤트를 자동으로 호출하고 관리한다</para>
    /// </summary>
    public class SmartMonoObjectOC<T> : SmartMonoObject<T>, IObjectCaching where T : SmartMonoObjectOC<T>
    {
        ///======================================================================================================================================================



        public ObjectCaching OC => oc;
        [FoldoutGroup("Smart Mono Object OC")]
        [SerializeField, ReadOnly]
        protected ObjectCaching oc;



        [FoldoutGroup("Smart Mono Object OC")]
        [LabelText("OC Reset When Enable")]
        /// <summary>
        /// 활성화시, <b>Enable</b> 될 때 마다 <see cref="OC"/>의 <see cref="ObjectCaching.ResetTransform(bool)"/> 이 호출된다
        /// </summary>
        public bool IsAutoResetOC_Enable = false;



        /// <summary>
        /// 활성화시, <b>Disable</b> 될 때 마다 <see cref="OC"/>의 <see cref="ObjectCaching.ResetTransform(bool)"/> 이 호출된다
        /// </summary>
        [FoldoutGroup("Smart Mono Object OC")]
        [LabelText("OC Reset When Disable")]
        public bool IsAutoResetOC_Disable = false;



        ///======================================================================================================================================================



        ///<summary>
        ///Awake, 재정의시 반드시 base도 실행해야함
        /// </summary>
        protected virtual void Awake()
        {
            oc = new ObjectCaching(this);
        }



        /// <inheritdoc/>
        protected override void OnEnable()
        {
            base.OnEnable();
            if (IsAutoResetOC_Enable) { OC?.ResetTransform(); }
        }



        /// <inheritdoc/>
        protected override void OnDisable()
        {
            base.OnDisable();
            if (IsAutoResetOC_Disable) { OC?.ResetTransform(); }
        }



        ///======================================================================================================================================================
    }


}