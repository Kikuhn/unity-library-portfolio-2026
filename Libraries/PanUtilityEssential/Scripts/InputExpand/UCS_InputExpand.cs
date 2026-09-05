using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Events;
using DG.Tweening;
using Pan.Util;
using UnityEngine.InputSystem;
using System.Reflection;



//? 유니티 인풋 시스템, 기본 인풋을 확장하는 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    /// <summary>
    /// UniTask / DOTween의 시퀀스를 활용하여 Update 방식으로 멀티 입력(탭) 기록을 관리하는 클래스입니다.
    /// </summary>
    public class CustomInputMultiPress
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 다중 파라미터 생성자
        /// </summary>
        /// <param name="deltaTimeFunc">
        /// 현재 프레임의 델타 타임을 반환하는 함수<br/>
        /// null을 받아오면 <see cref="Time.unscaledDeltaTime"/>를 사용합니다.
        /// </param>
        /// <param name="tapCount">
        /// 입력(탭) 성공을 판정하기 위한 필요한 탭 횟수
        /// </param>
        /// <param name="maxTapSpacing">
        /// 탭 간의 최대 허용 간격 (초)
        /// </param>
        /// <param name="maxTapDuration">
        /// 탭 입력이 유지될 수 있는 최대 지속 시간 (초)
        /// </param>
        public CustomInputMultiPress(TimeUpdateEvent.TimeUpdateMode updateMode, Func<float> deltaTimeFunc, int? tapCount = 2, float? maxTapSpacing = 0.4f, float? maxTapDuration = 0.2f)
        {
            DeltaTimeFunc = deltaTimeFunc ?? (() => Time.unscaledDeltaTime);

            TimeUpdater = new TimeUpdateEvent();
            TimeUpdater.EndTime = -1;
            TimeUpdater.Enable(DeltaTimeFunc, null, Update, updateMode);


            if (tapCount.HasValue) TapCount = tapCount.Value;
            if (maxTapSpacing.HasValue) MaxTapSpacing = maxTapSpacing.Value;
            if (maxTapDuration.HasValue) MaxTapDuration = maxTapDuration.Value;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 현재 프레임의 델타 타임을 반환하는 함수 (외부에서 전달)
        /// </summary>
        private readonly Func<float> DeltaTimeFunc;



        /// <summary>
        /// 업데이터
        /// </summary>
        private readonly TimeUpdateEvent TimeUpdater;



        /// <summary>
        /// 입력 성공을 판정하기 위한 필요한 탭 횟수 (기본값 2)
        /// </summary>
        public int TapCount = 2;



        /// <summary>
        /// 탭 간의 최대 허용 간격 (초). 기본값은 0.4초. (Spacing은 Duration의 두배가 디폴트)
        /// </summary>
        public float MaxTapSpacing = 0.4f;



        /// <summary>
        /// 탭 입력이 유지될 수 있는 최대 지속 시간 (초). 기본값은 0.2초.
        /// </summary>
        public float MaxTapDuration = 0.2f;



        /// <summary>
        /// 현재 입력 처리가 진행 중인지 여부
        /// </summary>
        public bool IsWorking { get; private set; } = false;



        /// <summary>
        /// 현재까지 입력된 탭 횟수를 기록하는 변수
        /// </summary>
        private int TapCounting = 0;



        /// <summary>
        /// 전체 경과 시간(누적된 델타 타임)을 기록하는 타이머
        /// </summary>
        private float SpaceTimer = 0f;



        /// <summary>
        /// 입력 지속 시간을 기록하는 타이머 (입력 발생 시마다 0으로 초기화)
        /// </summary>
        private float DurationTimer = 0f;



        ///======================================================================================================================================================



        /// <summary>
        /// 입력 성공 시 호출되는 이벤트
        /// </summary>
        public event Action<InputAction.CallbackContext> SuccesEvent;



        /// <summary>
        /// 입력 트리거가 발생할 때마다 호출되는 이벤트
        /// </summary>
        public event Action<InputAction.CallbackContext> TriggerEvent;



        /// <summary>
        /// 입력 트리거가 중단되거나 종료될 때 호출되는 이벤트
        /// </summary>
        public event Action EndEvent;



        ///======================================================================================================================================================



        /// <summary>
        /// 업데이트 콜백 함수
        /// </summary>
        private void Update(TimeUpdateEvent timeUpdateEvent, float deltaTime)
        {
            if (deltaTime == 0f)
            {
                //! 델타 타임이 0이면 업데이트를 진행하지 않음
                return;
            }
            SpaceTimer += deltaTime;    //. 총 경과 시간 증가
            DurationTimer += deltaTime; //. 현재 입력 지속 시간 증가

            //? 최대 탭 간격 또는 최대 입력 지속 시간이 초과되면 시간 초과로 처리
            if (MaxTapSpacing <= SpaceTimer || MaxTapDuration <= DurationTimer)
            {
                TimeOver();
            }
        }



        /// <summary>
        /// 입력 제한 시간이 초과되었을 때 처리하는 함수
        /// </summary>
        private void TimeOver()
        {
            //? 시간 초과 시 입력 관련 값들을 초기화하고 종료 이벤트 호출
            ResetValues();
            EndEvent?.Invoke();
        }



        /// <summary>
        /// 가변값들(타이머, 카운트 등) 초기화 (이벤트 핸들러 제외)
        /// </summary>
        public void ResetValues()
        {
            //Sequence.Pause(); //? DOTween 시퀀스 일시 정지
            TimeUpdater.PauseWorking();
            IsWorking = false;  //! 작업 중지 상태로 전환
            SpaceTimer = 0f;    //. 전체 경과 타이머 초기화
            DurationTimer = 0f; //. 입력 지속 타이머 초기화
            TapCounting = 0;    //. 입력 카운트 초기화
        }



        /// <summary>
        /// 매니저를 리셋하는 함수
        /// </summary>
        /// <param name="resetValues">
        /// true이면 내부 가변값(타이머, 카운트)을 초기화합니다. (기본값: true)
        /// </param>
        /// <param name="killUpdate">
        /// true이면 DOTween 시퀀스 업데이트를 종료시킵니다. (기본값: true)
        /// </param>
        public void Reset(bool resetValues = true, bool killUpdate = true)
        {
            //? 필요시 내부 값들을 초기화
            if (resetValues) { ResetValues(); }

            //? 모든 이벤트 핸들러 제거
            SuccesEvent = null;
            TriggerEvent = null;
            EndEvent = null;

            //? 업데이터 종료
            if (killUpdate) { TimeUpdater.ShutDown(false); }
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 입력 트리거를 실행하는 함수
        /// </summary>
        /// <param name="ctx">
        /// 입력 이벤트 컨텍스트 (예: InputAction.CallbackContext)
        /// </param>
        /// <param name="addCount">
        /// 입력 카운트에 더할 값 (기본값: 1)
        /// </param>
        public void ExecuteTrigger(InputAction.CallbackContext ctx, int addCount = 1)
        {
            if (IsWorking == false)
            {
                //! 첫 입력 시 작업 상태로 전환하고 DOTween 시퀀스를 재생 시작
                IsWorking = true;
                //? 업데이터 실행 (강제)
                TimeUpdater.StartWorking(true);
            }

            TapCounting += addCount; //? 입력 횟수 증가
            DurationTimer = 0f;      //. 입력 지속 타이머를 초기화

            TriggerEvent?.Invoke(ctx); //? 입력 트리거 발생 시 외부 이벤트 호출

            //? 입력 횟수가 설정된 탭 횟수 이상이면 성공 처리
            if (TapCount <= TapCounting)
            {
                Succes(ctx);
            }
        }



        /// <summary>
        /// 입력 트리거를 실행하는 함수 (기본적으로 1회 입력 추가)
        /// </summary>
        /// <param name="ctx">
        /// 입력 이벤트 컨텍스트 (예: InputAction.CallbackContext)
        /// </param>
        public void ExecuteTrigger(InputAction.CallbackContext ctx)
        {
            //? 기본적으로 1회 입력 추가하여 처리
            ExecuteTrigger(ctx, 1);
        }



        public void ShutDown(bool success)
        {
            TimeUpdater.ShutDown(success);
        }



        /// <summary>
        /// 입력이 성공한 경우 처리하는 함수
        /// </summary>
        /// <param name="ctx">
        /// 입력 성공 시 전달되는 이벤트 컨텍스트
        /// </param>
        private void Succes(InputAction.CallbackContext ctx)
        {
            //? 입력 성공 시 성공 이벤트 호출
            SuccesEvent?.Invoke(ctx);
            //? 입력 관련 가변값들을 초기화
            ResetValues();
            //? 종료 이벤트 호출
            EndEvent?.Invoke();
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================
}