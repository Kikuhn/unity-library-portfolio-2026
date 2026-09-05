using UnityEngine;
using Pan.Util;
using System;
using Sirenix.OdinInspector;



//? PanBaseEvent



//[AttributeUsage(AttributeTargets.Class, Inherited = false)]
//public class PanBaseEventAutoInitializeAttribute : Attribute
//{
//    public bool AutoInitialize { get; }
//    public PanBaseEventAutoInitializeAttribute(bool autoInitialize)
//    {
//        AutoInitialize = autoInitialize;
//    }
//}



namespace Pan.Event
{
    /// <summary>
    /// 단일 이벤트 의 기반이 되는 베이스 클래스
    /// <para>모든 이벤트들은, 이 객체를 상속받는다</para>
    /// </summary>
    [Serializable]
    public abstract class PanBaseEvent : IDisposable
    {
        ///======================================================================================================================================================



        #region IDisposable 패턴



        private bool _disposed;



        /// <summary>
        /// 이벤트에서 사용하는 리소스를 해제합니다.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            DisposeInternal();
            _disposed = true;

            GC.SuppressFinalize(this); //. 해당 객체에 대해 GC가 Finalizer(소멸자)를 호출하지 않도록 막는 것
        }



        /// <summary>
        /// 파생 클래스에서 해제 로직을 구현합니다.
        /// </summary>
        protected virtual void DisposeInternal()
        {

        }



        #endregion



        ///======================================================================================================================================================



        //? Static



        /// <summary>
        /// <see cref="PanEventGeneralManager"/>로부터  <see cref="PanEventManager.GetEvent(Type)"/>
        /// </summary>
        /// <returns></returns>
        public static PanBaseEvent Event(PanBaseEvent currentEvent) => PanEventGeneralManager.EventManager.GetEvent(currentEvent.GetType());



        /// <summary>
        /// <see cref="PanEventGeneralManager"/>로부터  <see cref="PanEventManager.ContainsEvent(Type)"/>
        /// </summary>
        /// <returns></returns>
        public static bool ContainsEvent(PanBaseEvent currentEvent) => PanEventGeneralManager.EventManager.ContainsEvent(currentEvent.GetType());



        /// <summary>
        /// <see cref="PanEventGeneralManager"/>로부터  <see cref="PanEventManager.ReleaseEvent(Type)"/>
        /// </summary>
        /// <returns></returns>
        public static bool ReleaseEvent(PanBaseEvent currentEvent) => PanEventGeneralManager.EventManager.ReleaseEvent(currentEvent.GetType());



        ///======================================================================================================================================================



#if UNITY_EDITOR



        private string EditorTypeName => GetType().Name;
        private string EditorTypeFullNameWithHashcode => $"{GetType().FullName} ({GetHashCode()})";



        [ShowInInspector]
        [HideLabel]
        [TitleGroup("@EditorTypeName", Subtitle = "@EditorTypeFullNameWithHashcode", Alignment = TitleAlignments.Split, HorizontalLine = false, BoldTitle = true)]
        [DisplayAsString]
        [PropertyOrder(-100)]
        [PropertySpace(-8, -8)]
        private readonly string EditorDrawTitleField = "";



#endif



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 단일 이벤트 의 기반이 되는 베이스 클래스
    /// <see cref="Event"/>를 지원하기 위해 제네릭으로 선언
    /// </summary>
    [Serializable]
    public abstract class PanBaseEvent<TEvent> : PanBaseEvent where TEvent : PanBaseEvent, new()
    {
        /// <summary>
        /// <see cref="PanEventGeneralManager"/>로부터  <see cref="PanEventManager.GetEvent{TEvent}()"/>
        /// </summary>
        /// <returns></returns>
        public static TEvent Event() => PanEventGeneralManager.EventManager.GetEvent<TEvent>();



        /// <summary>
        /// <see cref="PanEventGeneralManager"/>로부터  <see cref="PanEventManager.ContainsEvent{TEvent}()"/>
        /// </summary>
        /// <returns></returns>
        public static bool ContainsEvent() => PanEventGeneralManager.EventManager.ContainsEvent<TEvent>();



        /// <summary>
        /// <see cref="PanEventGeneralManager"/>로부터  <see cref="PanEventManager.ReleaseEvent{TEvent}()"/>
        /// </summary>
        /// <returns></returns>
        public static bool ReleaseEvent() => PanEventGeneralManager.EventManager.ReleaseEvent<TEvent>();
    }
}