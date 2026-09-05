using System.Collections;
using System.Collections.Generic;



namespace Pan.Event
{
    /// <summary>On / Off가 존재하는 이벤트</summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseEvent_OneSwitch<T> : PanBaseEvent where T : class
    {
        ///======================================================================================================================================================



        private readonly HashSet<T> SwitchBox = new HashSet<T>();



        ///======================================================================================================================================================



        /// <summary>자동 스위치 (꺼져있으면 켜고, 켜져있으면 끄고)</summary>
        /// <param name="target"></param>
        public void SwitchAuto(T target)
        {
            if (SwitchBox.Contains(target))
            {
                SwitchBox.Remove(target);
                SwitchIsOff(target);
            }
            else
            {
                SwitchBox.Add(target);
                SwitchIsOn(target);
            }
        }



        /// <summary>스위치 켜기</summary>
        /// <param name="target"></param>
        public void SwitchOn(T target)
        {
            if (!SwitchBox.Contains(target))
            {
                SwitchBox.Add(target);
                SwitchIsOn(target);
            }
        }



        /// <summary>스위치 끄기</summary>
        /// <param name="target"></param>
        public void SwitchOff(T target)
        {
            if (SwitchBox.Contains(target))
            {
                SwitchBox.Remove(target);
                SwitchIsOff(target);
            }
        }



        /// <summary>스위치 On/Off</summary>
        /// <param name="onOff">켜기 / 끄기</param>
        /// <param name="target"></param>
        public void Switch(bool onOff, T target)
        {
            if (onOff) { SwitchOn(target); }
            else { SwitchOff(target); }
        }



        ///======================================================================================================================================================



        /// <summary>내부용 스위치 On 함수</summary>
        /// <param name="target"></param>
        protected abstract void SwitchIsOn(T target);



        /// <summary>내부용 스위치 Off 함수</summary>
        /// <param name="target"></param>
        protected abstract void SwitchIsOff(T target);



        ///======================================================================================================================================================
    }



    /// <summary>활성 / 비활성화가 존재하는 이벤트</summary>
    /// <typeparam name="TEvent"></typeparam>
    public abstract class BaseEvent_Activity<TEvent> : PanBaseEvent<TEvent> where TEvent : PanBaseEvent, new()
    {
        ///======================================================================================================================================================

        public BaseEvent_Activity()
        {
            bool isNotEditorMode = true;


#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying == false)
            {
                isNotEditorMode = false;
            }
#endif



            if (isNotEditorMode && IsEnable_Constructor)
            {
                Enable = true;
            }
        }



        ///======================================================================================================================================================



        public bool Enable
        {
            get => enable;
            set
            {
                if (!enable && value) //? 비활성화 -> 활성화
                {
                    EnableEvent();
                    enable = value;
                    return;
                }

                if (enable && !value) //? 활성화 -> 비활성화
                {
                    DisableEvent();
                    enable = value;
                    return;
                }

            }
        }
        private bool enable = false;



        /// <summary>
        /// 생성자에서, 이 이벤트가 생성될때 활성화된 상태로 생성할지, 비활성화 된 상태로 생성할지
        /// </summary>
        protected virtual bool IsEnable_Constructor => false;



        ///======================================================================================================================================================



        /// <summary>
        /// 자동 스위치 (꺼져있으면 켜고, 켜져있으면 끄고)
        /// </summary>
        public void Switch()
        {
            if (Enable)
            {
                Enable = false;
            }
            else
            {
                Enable = true;
            }
        }




        public void Setting(bool value)
        {
            Enable = value;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 내부용 활성화 함수
        /// </summary>
        protected abstract void EnableEvent();



        /// <summary>
        /// 내부용 비활성화 함수
        /// </summary>
        protected abstract void DisableEvent();



        ///======================================================================================================================================================
    }
}