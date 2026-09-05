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
using UnityEngine.InputSystem;



//? 딱히 분류하기 애매한 열거형(Enum) 들이 정리되어있는 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    /// <summary>
    /// 활성화 선택 모드를 나타내는 열거형입니다.
    /// </summary>
    public enum ESelectActivesMode
    {
        /// <summary>모든 객체를 선택합니다.</summary>
        All,
        /// <summary>활성화된 객체만 선택합니다.</summary>
        OnlyEnable,
        /// <summary>비활성화된 객체만 선택합니다.</summary>
        OnlyDisable
    }



    ///======================================================================================================================================================
}