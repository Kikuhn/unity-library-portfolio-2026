using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pan.Event;
using Pan.Util;
using System;
using Sirenix.OdinInspector;



//? EventAble 컨트롤러 오브젝트



namespace Pan.Event
{
    /// <summary>
    /// <see cref="EventAbleController"/> 인터페이스
    /// <para>Awake에서 <see cref="EventAbleController"/>를 초기화해주고</para>
    /// <para>OnDisable에서 <see cref="EventAble.Reset()"/>을 호출을 해주면 OK</para>
    /// </summary>
    public interface IEventAbleController : IEventAble
    {
        /// <summary>
        /// <see cref="EventAble"/> 컨트롤러
        /// </summary>
        EventAbleController EventAbleController { get; }
    }



    /// <summary>
    /// <see cref="EventAble"/>를 관리하는 컨트롤러
    /// <para>반드시 사용중인 곳에서 최초 1회 초기화 (<see cref="Initialize{T}(T)"/>)가 필요 (Awake 등)</para>
    /// </summary>
    public class EventAbleController : IEventAble
    {
        ///======================================================================================================================================================



        //. 생성자



        private EventAbleController(
            IEventAble eventAble,
            int initialEventTableCapacity,
            EventAbleTableAllocationMode tableAllocationMode)
        {
            if (initialEventTableCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialEventTableCapacity), initialEventTableCapacity, "이벤트 테이블의 초기 용량은 0 이상이어야 합니다.");
            }

            InitializeEventTableCapacity = initialEventTableCapacity;
            TableAllocationMode = tableAllocationMode;
            EventAble = new EventAble(eventAble, InitializeEventTableCapacity, TableAllocationMode);
        }



        /// <summary>
        /// <see cref="IEventAble"/>를 구현한 <see cref="MonoBehaviour"/>를 기본 이벤트 테이블 용량으로 초기화합니다.
        /// <para>기존 호출과 동일하게 초기 용량 0을 사용합니다.</para>
        /// </summary>
        /// <typeparam name="T">초기화할 <see cref="MonoBehaviour"/> 및 <see cref="IEventAble"/> 구현 타입입니다.</typeparam>
        /// <param name="eventAble"><see cref="EventAble"/>을 소유할 객체입니다.</param>
        /// <returns>초기화된 컨트롤러입니다.</returns>
        public static EventAbleController Initialize<T>(T eventAble) where T : MonoBehaviour, IEventAble
        {
            return new EventAbleController(eventAble, 0, EventAbleTableAllocationMode.Eager);
        }



        /// <summary>
        /// <see cref="IEventAble"/>를 구현한 <see cref="MonoBehaviour"/>를 지정한 이벤트 테이블 초기 용량으로 초기화합니다.
        /// <para>한 소유자에 장착될 이벤트 밸류 타입 수를 대략 알고 있을 때만 용량을 지정합니다.</para>
        /// </summary>
        /// <typeparam name="T">초기화할 <see cref="MonoBehaviour"/> 및 <see cref="IEventAble"/> 구현 타입입니다.</typeparam>
        /// <param name="eventAble"><see cref="EventAble"/>을 소유할 객체입니다.</param>
        /// <param name="initialEventTableCapacity">이벤트 밸류 테이블의 초기 용량입니다. 0 이상이어야 합니다.</param>
        /// <returns>초기화된 컨트롤러입니다.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="initialEventTableCapacity"/>가 0보다 작을 때 발생합니다.</exception>
        public static EventAbleController Initialize<T>(T eventAble, int initialEventTableCapacity) where T : MonoBehaviour, IEventAble
        {
            return new EventAbleController(eventAble, initialEventTableCapacity, EventAbleTableAllocationMode.Eager);
        }



        /// <summary>
        /// <see cref="IEventAble"/>을 지정한 테이블 할당 방식으로 초기화합니다.
        /// </summary>
        public static EventAbleController Initialize<T>(
            T eventAble,
            EventAbleTableAllocationMode tableAllocationMode) where T : MonoBehaviour, IEventAble
        {
            return new EventAbleController(eventAble, 0, tableAllocationMode);
        }



        /// <summary>
        /// <see cref="IEventAble"/>을 지정한 용량과 테이블 할당 방식으로 초기화합니다.
        /// </summary>
        public static EventAbleController Initialize<T>(
            T eventAble,
            int initialEventTableCapacity,
            EventAbleTableAllocationMode tableAllocationMode) where T : MonoBehaviour, IEventAble
        {
            return new EventAbleController(eventAble, initialEventTableCapacity, tableAllocationMode);
        }



        ///======================================================================================================================================================



        [TitleGroup("EventAble 컨트롤러"), BoxGroup("EventAble 컨트롤러/박스", false)]
        //[LabelText("이벤트에이블")]
        [HideLabel, InlineProperty]
        [ShowInInspector]
        [HideReferenceObjectPicker]
        [PropertyOrder(-1000)]
        [ShowIf("@EventAble != null")]
        public EventAble EventAble { get; private set; }



        [TitleGroup("EventAble 컨트롤러"), BoxGroup("EventAble 컨트롤러/박스", false)]
        [ShowInInspector]
        [LabelText("이벤트 테이블 초기 용량")]
        [ReadOnlyCustom(true)]
        [PropertyOrder(-999)]
        private int InitializeEventTableCapacity;



        [ShowInInspector]
        [LabelText("이벤트 테이블 할당 방식")]
        [ReadOnlyCustom(true)]
        [PropertyOrder(-998)]
        private EventAbleTableAllocationMode TableAllocationMode;



#if UNITY_EDITOR

        private bool editorInitializeEventAbleQueued;

        [TitleGroup("EventAble 컨트롤러"), BoxGroup("EventAble 컨트롤러/박스", false)]
        [Button("EventAble 강제 초기화", Icon = SdfIconType.ExclamationSquareFill, Stretch = false, ButtonAlignment = 1f), GUIColor(0.93f, 0.33f, 0.40f)]
        [PropertyOrder(-998)]
        private void Editor_InitializeEventAble()
        {
            if (editorInitializeEventAbleQueued) { return; }

            //? Odin이 현재 EventAble property tree를 그리는 동안 참조를 교체하지 않도록 GUI 프레임 밖으로 미룬다
            editorInitializeEventAbleQueued = true;
            UnityEditor.EditorApplication.delayCall += Editor_ApplyInitializeEventAble;
        }



        /// <summary>
        /// 예약된 EventAble 강제 초기화를 현재 Inspector GUI 프레임이 끝난 뒤 적용합니다.
        /// </summary>
        private void Editor_ApplyInitializeEventAble()
        {
            editorInitializeEventAbleQueued = false;

            EventAble?.Reset();
            EventAble = new EventAble(this, InitializeEventTableCapacity, TableAllocationMode);
            Sirenix.Utilities.Editor.GUIHelper.RequestRepaint();
        }

#endif



        public static implicit operator EventAble(EventAbleController eventAbleController)
        {
            return eventAbleController.EventAble;
        }
    }



    [DisallowMultipleComponent]
    public class EventAbleControllerObject : SmartMonoObjectOC<EventAbleControllerObject>, IObjectCaching, IEventAbleController
    {
        ///======================================================================================================================================================



        [TitleGroup("EventAble 컨트롤러 오브젝트"), BoxGroup("EventAble 컨트롤러 오브젝트/박스", false)]
        [InfoBox("Awake에서 컨트롤러가 초기화되며, OnDisable에서 EventAble이 Reset된다")]
        [ShowInInspector]
        [LabelText("EventAble 컨트롤러")]
        public EventAbleController EventAbleController { get; private set; }



        public EventAble EventAble => EventAbleController.EventAble;



        ///======================================================================================================================================================



        //? 초기화



        protected override void Awake()
        {
            base.Awake();
            EventAbleController = EventAbleController.Initialize(this);
        }



        ///======================================================================================================================================================



        //? Disable



        protected override void OnDisable()
        {
            base.OnDisable();
            EventAble.Reset();
        }



        ///======================================================================================================================================================
    }
}
