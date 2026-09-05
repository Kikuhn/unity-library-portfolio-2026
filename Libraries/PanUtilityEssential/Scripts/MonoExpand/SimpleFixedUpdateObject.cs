using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Pan.Util;



//? [Mono Expand] Fixed 업데이트 기능 만을 수행하는 객체



namespace Pan.Util
{
    public class SimpleFixedUpdateObject : MonoBehaviour
    {
        public event Action FixedUpdateEvent;

        private void FixedUpdate()
        {
            FixedUpdateEvent?.Invoke();
        }
    }
}