using UnityEngine;
using Pan.Util;
using System.Collections.Generic;
using System;
using Sirenix.OdinInspector;
using System.Linq;



//? 이벤트 가능, 이벤트 에이블



namespace Pan.Event
{
    /// <summary>
    /// <see cref="EventAble"/> 내부 이벤트 테이블의 할당 시점을 지정합니다.
    /// </summary>
    public enum EventAbleTableAllocationMode
    {
        Eager,
        Lazy
    }



    /// <summary>
    /// <see cref="EventAble.DispatchLocal{TSignal}(in TSignal)"/>로 전달되는 로컬 신호를 수신합니다.
    /// </summary>
    /// <typeparam name="TSignal">전달할 값 형식 또는 참조 형식 신호입니다.</typeparam>
    public interface IEventAbleSignalReceiver<TSignal>
    {
        /// <summary>
        /// 현재 <see cref="IEventAble"/>에 한정된 신호를 수신합니다.
        /// </summary>
        void ReceiveLocalSignal(IEventAble source, in TSignal signal);
    }



    /// <summary>
    /// <see cref="EventAble"/> 보유 인터페이스
    /// </summary>
    public interface IEventAble
    {
        EventAble EventAble { get; }
    }



    ///// <summary>
    ///// <see cref="IEventAble"/>를 보유한 객체의 상위  <see cref="IEventAble"/>의 보유 여부를 관리하는 인터페이스
    ///// </summary>
    //public interface IMasterEventAble
    //{
    //    /// <summary>
    //    /// 이 객체의 상위 <see cref="IEventAble"/>
    //    /// </summary>
    //    IEventAble MasterEventAble { get; set; }



    //    ///// <summary>
    //    ///// <see cref="IEventAble"/>의 최상위인 <see cref="MasterEventAble"/> 를 반환한다
    //    ///// <para><see cref="IMasterEventAble"/>가 존재하지 않는다면, 받아온 <paramref name="target"/>을 그대로 반환한다</para>
    //    ///// </summary>
    //    //public static IEventAble GetTopMasterEventAble(IMasterEventAble target)
    //    //{
    //    //    //? 일단 현재 마스터이벤터블을 변수에 저장
    //    //    IEventAble current_EventAble = target.MasterEventAble;

    //    //    while (true)
    //    //    {
    //    //        //? 이 EventAble의 마스터이벤터블이 존재하고, null이 아닐경우
    //    //        //! 계속 반복하기위해, 현재 그 마스터이벤터블을 변수에 새로 저장한다
    //    //        if (current_EventAble is IMasterEventAble hasMaster && hasMaster.MasterEventAble != null)
    //    //        {
    //    //            current_EventAble = hasMaster.MasterEventAble;
    //    //        }

    //    //        //? 그 EventAble의 마스터이벤터블이 존재하지 않거나, null일경우 무한반복 탈출
    //    //        else { break; }
    //    //    }

    //    //    return current_EventAble;
    //    //}



    //    ///// <summary>
    //    ///// <see cref="IEventAble"/>의 최상위인 <see cref="MasterEventAble"/> 를 반환해본다
    //    ///// <para><see cref="IMasterEventAble"/>가 존재하지 않는다면, 받아온 <paramref name="target"/>을 그대로 반환한다</para>
    //    ///// </summary>
    //    //public static IEventAble TryGetTopMasterEventAble(IEventAble target)
    //    //{
    //    //    if (target is IMasterEventAble master)
    //    //    {
    //    //        return GetTopMasterEventAble(master);
    //    //    }
    //    //    return target;
    //    //}
    //}



    /// <summary>
    /// 이벤트에이블
    /// <para>이 객체를 소유한 스크립트는 판 이벤트 밸류, 이벤트 테이블 시스템 사용이 가능</para>
    /// </summary>
    [Serializable]
    //[LabelText("이벤트에이블")]
    [ShowInInspector]
    [HideReferenceObjectPicker]
    public partial class EventAble : IEventAble
    {
        ///======================================================================================================================================================



        //? 생성자



        public EventAble(IEventAble master, int tableCapacity)
            : this(master, tableCapacity, EventAbleTableAllocationMode.Eager)
        {
        }



        /// <summary>
        /// 이벤트 테이블의 할당 시점을 지정하여 <see cref="EventAble"/>을 생성합니다.
        /// </summary>
        public EventAble(IEventAble master, int tableCapacity, EventAbleTableAllocationMode tableAllocationMode)
        {
            if (tableCapacity < 0) { throw new ArgumentOutOfRangeException(nameof(tableCapacity)); }
            if (tableAllocationMode != EventAbleTableAllocationMode.Eager &&
                tableAllocationMode != EventAbleTableAllocationMode.Lazy)
            {
                throw new ArgumentOutOfRangeException(nameof(tableAllocationMode));
            }

            Master = master;

            EventValueTables = new EventValueTable(this, tableCapacity, tableAllocationMode);
        }



        ///======================================================================================================================================================



        ///======================================================================================================================================================



        //? 바로가기 필드



        ///<summary>
        ///<see cref="IEventAble"/> 자기 자신 반환
        ///</summary>
        EventAble IEventAble.EventAble => this;



        /// <summary>
        /// 이벤트 밸류 매니저 얻어오기
        /// </summary>
        private PanEventValueManager EventValueManager => PanEventGeneralManager.EventValueManager;



        ///======================================================================================================================================================



        /// <summary>
        /// 이 <see cref="EventAble"/>를 보유중인 객체
        /// </summary>
        [HideInInspector]
        private readonly IEventAble Master;



        private bool IsMaster_Valid => Master != null;



        /// <summary>
        /// 이벤트 밸류 테이블
        /// <para>이 <see cref="EventAble"/>에 등록된 모든 <see cref="PanBaseEventValue"/>들을 딕셔너리로 관리한다</para>
        /// </summary>
        [ShowInInspector, EnableGUI, HideReferenceObjectPicker, InlineProperty]
        [HideLabel]
        private readonly EventValueTable EventValueTables;



        /// <summary>
        /// 이벤트 밸류 Dictionary가 실제로 할당되었는지 확인합니다.
        /// </summary>
        public bool IsEventValueTableAllocated => EventValueTables.IsAllocated;



        /// <summary>
        /// 이벤트 테이블의 현재 구조 변경 번호입니다.
        /// </summary>
        public uint EventValueTableRevision => EventValueTables.Revision;
        /// <summary>
        /// 이벤트 밸류가 비활성화 중인지 확인
        /// <para>이벤트 밸류 테이블 내에서,  <see cref="PanBaseEventValue"/>를 <see cref="PanBaseEventValue.DisableValue(EventAble)"/> 을 호출 하여 비활성화 할 때,</para>
        /// <para><see cref="PanBaseEventValue.Disable"/> 를 호출 이전에, 이 객체에 값을 할당하고,</para>
        /// <para><see cref="PanBaseEventValue.Disable"/>의 호출 이후에, 이 객체를 null로 만든다</para>
        /// <para>이 플래그를 활용하여, <see cref="PanBaseEventValue.Disable"/> 도중에 이벤트 밸류 테이블에 값이 추가가 되는 불상사를 디버깅 할 수 있다</para>
        /// </summary>
        public PanBaseEventValue IsDisablingEventValue { get; set; } = null;



        /// <summary>
        /// <see cref="PanBaseEventValue.Disable"/> 내에서 이벤트 테이블 내에
        /// <para><b>"이벤트 밸류 테이블에 이벤트 밸류 새로 추가"</b> 를 감지하고 디버깅 할 지 여부</para>
        /// <para><see cref="PanBaseEventValue.Disable"/> 도중에 <see cref="RequireInternal{TEventValue, TTarget}(Type, TTarget)"/>가 호출 되었는지 여부 까지 감지한다</para>
        /// </summary>
        [BoxGroup("디버깅")]
        [LabelText("Disable중 이벤트 추가 검사"), LabelWidth(200)]
        public bool Debug_CheckAddEventValue_WhenEventValueDisableing = false;
        ///======================================================================================================================================================                       
    }
}
