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



//? "유효성 검사" 라는 동작이 실행되었는지 기록하는 정도의 코드



/* 설명
 * 주로 에디터에서 동작하며
 * 유효성 검사 자체는 수동으로 할당하여 호출해야함
 */



namespace Pan.Util
{
    ///======================================================================================================================================================



    /// <summary>
    ///유효성 검사 기록기 인터페이스
    /// </summary>
    public interface ICheckValidLogger
    {
        CheckValidLogger CheckValidManager { get; }
    }



    /// <summary>
    ///유효성 검사 기록기
    /// </summary>
    [Serializable]
    public class CheckValidLogger : ICheckValidLogger
    {
        /// <summary>
        /// 마지막으로 유효성 검사가 실행된 시간
        /// </summary>
        public DateTime? LastCheckValidTime = null;



        /// <summary>
        /// 마지막으로 유효성 검사의 결과
        /// </summary>
        public bool? LastCheckValidResult = null;



        public CheckValidLogger CheckValidManager => this;



        /// <summary>
        /// 유효성 검사 갱신하기<br/>
        /// (유효성 검사를 시행한 후에 실행 권장)
        /// </summary>
        /// <param name="checkValid"></param>
        public void ApplyLastCheck(bool checkValid)
        {
            LastCheckValidTime = DateTime.Now;
            LastCheckValidResult = checkValid;
        }
    }



    ///======================================================================================================================================================
}