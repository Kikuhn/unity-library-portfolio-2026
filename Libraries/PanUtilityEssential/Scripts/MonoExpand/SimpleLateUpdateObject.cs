using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Pan.Util;



//? [Mono Expand] Late 업데이트 기능 만을 수행하는 객체



namespace Pan.Util
{
    public class SimpleLateUpdateObject : MonoBehaviour
    {
        public event Action LateUpdateEvent;

        private void LateUpdate()
        {
            LateUpdateEvent?.Invoke();
        }
    }
}