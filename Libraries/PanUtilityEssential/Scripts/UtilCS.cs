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
using System.Threading;
using Sirenix.OdinInspector;



//? UtilCS, 딱히 분류하기에 애매한 것들이 정리되어있는 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    public static class StaticExtensions
    {

    }



    ///======================================================================================================================================================



    /// <summary>
    /// 시간 업데이트 이벤트를 관리하는 클래스입니다.
    /// DOTween 또는 UniTask 기반 업데이트 방식을 지원하며, 외부에서는 동일한 인터페이스로 사용 가능합니다.
    /// </summary>
    public class TimeUpdateEvent
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 타임 업데이트 방식 모드를 나타내는 열거형입니다.
        /// DOTween: DOTween의 Sequence를 사용하여 업데이트합니다.
        /// UniTask: UniTask의 업데이트 루프를 사용하여 업데이트합니다.
        /// 추후 새로운 모드가 추가될 수 있도록 switch문으로 분기 처리합니다.
        /// </summary>
        public enum TimeUpdateMode
        {
            DOTween,
            UniTask
        }



        ///======================================================================================================================================================



        public TimeUpdateEvent()
        {
            TweenUpdateEventCallback = SequenceUpdate;
        }



        /// <summary>
        /// 지정된 업데이트 모드로 필요한 객체들을 미리 준비(캐싱)합니다.
        /// </summary>
        public TimeUpdateEvent(TimeUpdateMode mode) : this()
        {
            Ready(mode);
        }



        ///======================================================================================================================================================



        // Tween 업데이트 콜백 (DOTween 전용)
        private TweenCallback TweenUpdateEventCallback;



        // DOTween 시퀀스 (DOTween 모드에서 캐싱)
        private Sequence sequence;



        // UniTask 모드에서 사용할 CancellationTokenSource
        private CancellationTokenSource uniTaskCTS;



        /// <summary>
        /// 활성화 상태 여부.
        /// </summary>
        public bool IsEnable { get; private set; }



        /// <summary>
        /// 작업 중인지 여부.
        /// </summary>
        public bool IsWorking { get; private set; } = false;



        /// <summary>
        /// 외부에서 강제로 IsWorking 상태를 설정할 수 있는 프로퍼티.
        /// </summary>
        public bool IsWorking_Manual { set => IsWorking = value; }



        /// <summary>
        /// 종료 시간 (타이머가 이 값에 도달하면 종료됩니다).<br/>
        /// 음수일경우, 종료되지 않습니다.
        /// </summary>
        public float EndTime = 0f;



        /// <summary>
        /// 현재 타이머 값 (업데이트 시마다 증가).
        /// </summary>
        public float Timer { get; private set; } = 0f;



        /// <summary>
        /// 일회용 실행 여부.<br/>
        /// true이면 End 시 자동으로 Disable됩니다.
        /// </summary>
        public bool IsWorkingOnce { get; set; } = false;



        /// <summary>
        /// 루프 실행 여부.
        /// </summary>
        public bool IsLoop { get; set; } = false;



        /// <summary>
        /// 루프 실행 조건 함수.
        /// </summary>
        public Func<bool> LoopCondition = () => true;



        // 델타 타임을 반환하는 함수. null이면 기본적으로 Time.deltaTime 사용.
        private Func<float> deltaTimeFunc;



        // 타이머 종료 시 호출할 이벤트
        private Action successEvent = null;



        // 업데이트 시 매 프레임 호출되는 이벤트 (TimeUpdateEvent, deltaTime)
        private Action<TimeUpdateEvent, float> updateEvent = null;



        /// <summary>
        /// 타이머 종료 후 호출되는 이벤트.
        /// </summary>
        public event Action EndEvent = null;



        /// <summary>
        /// 비활성화 시 호출되는 이벤트.
        /// </summary>
        public event Action<TimeUpdateEvent> DisableEvent = null;



        /// <summary>
        /// 남은 시간 (EndTime - Timer)
        /// </summary>
        public float LeftTime => EndTime - Timer;



        /// <summary>
        /// 현재 작업 중이거나 일시정지 상태 여부.
        /// DOTween 모드인 경우 Sequence의 재생 상태, UniTask 모드인 경우 IsWorking 플래그로 판단.
        /// </summary>
        public bool IsPlaying => CurrentUpdateMode switch
        {
            TimeUpdateMode.DOTween => sequence != null && sequence.IsPlaying(),
            TimeUpdateMode.UniTask => IsWorking,
            _ => false,
        };



        /// <summary>
        /// 현재 업데이트 모드
        /// </summary>
        public TimeUpdateMode CurrentUpdateMode { get; private set; } = TimeUpdateMode.DOTween;



        ///======================================================================================================================================================



        /// <summary>
        /// 지정된 업데이트 모드로 필요한 객체들을 미리 준비(캐싱)합니다.
        /// <para>
        /// DOTween 모드이면 DOTween.Sequence를 생성하고, UniTask 모드이면 CancellationTokenSource를 생성합니다.
        /// </para>
        /// </summary>
        /// <param name="mode">선택한 업데이트 방식 모드</param>
        public void Ready(TimeUpdateMode mode)
        {
            CurrentUpdateMode = mode;

            switch (CurrentUpdateMode)
            {
                case TimeUpdateMode.DOTween:
                if (sequence == null)
                {
                    sequence = DOTween.Sequence();
                    // 등록된 콜백으로 Sequence 업데이트를 처리합니다.
                    sequence.ReadyForUpdate(TweenUpdateEventCallback);
                }
                break;
                case TimeUpdateMode.UniTask:
                if (uniTaskCTS == null)
                {
                    uniTaskCTS = new CancellationTokenSource();
                }
                break;
                default:
                throw new NotSupportedException($"TimeUpdateMode '{CurrentUpdateMode}' is not supported.");
            }
        }



        /// <summary>
        /// 대체로 많이 사용하는 설정을 메서드 하나로 적용합니다
        /// </summary>
        /// <param name="endTime">
        /// 종료 시간 (타이머가 이 값에 도달하면 종료됩니다).<br/>
        /// 음수일경우, 종료되지 않습니다.
        /// </param>
        /// <param name="isWorkingOnce">
        /// 일회용 실행 여부.<br/>
        /// true이면 End 시 자동으로 Disable됩니다.
        /// </param>
        public void Setting(float endTime, bool isWorkingOnce)
        {
            EndTime = endTime;
            IsWorkingOnce = isWorkingOnce;
        }



        /// <summary>
        /// 대체로 많이 사용하는 설정을 메서드 하나로 적용합니다
        /// </summary>
        /// <param name="endTime">
        /// 종료 시간 (타이머가 이 값에 도달하면 종료됩니다).<br/>
        /// 음수일경우, 종료되지 않습니다.
        /// </param>
        /// <param name="isWorkingOnece">
        /// 일회용 실행 여부.<br/>
        /// true이면 End 시 자동으로 Disable됩니다.
        /// </param>
        /// <param name="isLoop">
        /// 루프 실행 여부.
        /// </param>
        /// <param name="loopCondition">
        /// 루프 실행 조건 함수.
        /// </param>
        public void Setting(float endTime, bool isWorkingOnece, bool isLoop, Func<bool> loopCondition)
        {
            Setting(endTime, isWorkingOnece);
            IsLoop = isLoop;
            LoopCondition = loopCondition;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 업데이트를 활성화합니다.
        /// <para>
        /// deltaTImeFunc가 null이면 기본적으로 () => Time.deltaTime을 사용합니다.
        /// Enable 호출 시 모드를 파라미터로 받아 Ready 메서드를 통해 미리 준비된 후, 해당 모드에 맞는 업데이트 루프를 시작합니다.
        /// </para>
        /// </summary>
        /// <param name="deltaTimeFunc">
        /// 델타 타임을 반환하는 함수.
        /// null이면 기본적으로 () => Time.deltaTime을 사용합니다.
        /// </param>
        /// <param name="successEvent">타이머 종료 시 호출할 이벤트.</param>
        /// <param name="updateEvent">매 프레임 업데이트 시 호출되는 이벤트 (TimeUpdateEvent, deltaTime).</param>
        /// <param name="mode">
        /// 업데이트 방식 모드. 생략하면 DOTween을 사용하고, null을 명시하면 현재 준비된 모드를 유지합니다.
        /// 이 파라미터를 통해 업데이트 모드를 결정하고, Ready 메서드가 내부 객체들을 미리 준비합니다.
        /// </param>
        /// <returns>이미 활성화되어 있다면 false, 성공적으로 활성화되면 true를 반환합니다.</returns>
        public bool Enable(Func<float> deltaTimeFunc, Action successEvent, Action<TimeUpdateEvent, float> updateEvent = null, TimeUpdateMode? mode = TimeUpdateMode.DOTween)
        {
            // 이미 활성화되어 있다면 false 리턴
            if (IsEnable) { return false; }

            // 모드를 결정하고 필요한 객체들을 준비
            Ready(mode ?? CurrentUpdateMode);

            IsEnable = true;

            // 델타타임 함수가 null이면 기본값 사용
            this.deltaTimeFunc = deltaTimeFunc ?? (() => Time.deltaTime);
            this.successEvent = successEvent;
            this.updateEvent = updateEvent;

            // 선택된 모드에 따른 업데이트 루프 시작
            switch (CurrentUpdateMode)
            {
                case TimeUpdateMode.DOTween:
                // DOTween 모드에서는 준비된 Sequence를 그대로 사용
                if (sequence == null)
                {
                    // Ready에서 반드시 sequence가 생성되어야 함
                    throw new Exception("Sequence is not initialized in DOTween mode.");
                }
                break;
                case TimeUpdateMode.UniTask:
                // UniTask 모드에서는 CancellationTokenSource가 준비되어 있으므로 업데이트 루프 시작
                UpdateUniTaskLoop().Forget();
                break;
                default:
                throw new NotSupportedException($"TimeUpdateMode '{CurrentUpdateMode}' is not supported.");
            }

            return true;
        }



        /// <summary>
        /// 업데이트를 비활성화합니다.
        /// <para>
        /// 모드에 따라 DOTween 시퀀스 또는 UniTask 업데이트 루프를 종료하고 내부 상태를 초기화합니다.
        /// </para>
        /// </summary>
        /// <param name="killSequence">
        /// DOTween 모드에서 시퀀스를 종료할지 여부.
        /// UniTask 모드에서는 무시됩니다.
        /// </param>
        public void Disable(bool killSequence)
        {
            DisableEvent?.Invoke(this);

            switch (CurrentUpdateMode)
            {
                case TimeUpdateMode.DOTween:
                if (sequence != null)
                {
                    if (killSequence)
                    {
                        sequence.Kill();
                        sequence = null;
                    }
                    else
                    {
                        sequence.Pause();
                    }
                }
                break;
                case TimeUpdateMode.UniTask:
                if (uniTaskCTS != null)
                {
                    uniTaskCTS.Cancel();
                    uniTaskCTS.Dispose();
                    uniTaskCTS = null;
                }
                break;
                default:
                throw new NotSupportedException($"TimeUpdateMode '{CurrentUpdateMode}' is not supported.");
            }

            IsEnable = false;
            IsWorking = false;
            Timer = 0f;
            EndTime = 0f;
            deltaTimeFunc = null;
            successEvent = null;
            updateEvent = null;
            EndEvent = null;
            IsWorkingOnce = false;
            IsLoop = false;
            LoopCondition = () => true;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 업데이트를 시작하여 타이머 동작을 활성화합니다.
        /// <para>
        /// 이미 작동 중이면 false를 반환하며, DOTween 모드에서는 Sequence를 재생합니다.
        /// UniTask 모드에서는 IsWorking 플래그를 true로 설정합니다.
        /// </para>
        /// </summary>
        /// <param name="force">강제로 실행, 이전에 작동중이라면 강제로 <see cref="End"/> 시킨 후, 실행합니다</param>
        /// <returns>작업을 시작한 경우 true, 이미 작동 중이면 false</returns>
        public bool StartWorking(bool force = false)
        {
            if (force && IsWorking) { End(); }
            else if (IsWorking) { return false; }

            IsWorking = true;

            switch (CurrentUpdateMode)
            {
                case TimeUpdateMode.DOTween:
                sequence?.Play();
                break;
                case TimeUpdateMode.UniTask:
                // UniTask 모드에서는 IsWorking 플래그만 true로 설정 (업데이트 루프에서 체크)
                break;
                default:
                throw new NotSupportedException($"TimeUpdateMode '{CurrentUpdateMode}' is not supported.");
            }
            return true;
        }



        /// <summary>
        /// 현재 작동 중인 업데이트를 일시정지합니다.
        /// <para>
        /// DOTween 모드에서는 Sequence를 일시정지하고, UniTask 모드에서는 IsWorking 플래그를 false로 설정합니다.
        /// </para>
        /// </summary>
        /// <returns>일시정지가 성공하면 true, 그렇지 않으면 false</returns>
        public bool PauseWorking()
        {
            if (IsWorking)
            {
                switch (CurrentUpdateMode)
                {
                    case TimeUpdateMode.DOTween:
                    sequence?.Pause();
                    break;
                    case TimeUpdateMode.UniTask:
                    IsWorking = false;
                    break;
                    default:
                    throw new NotSupportedException($"TimeUpdateMode '{CurrentUpdateMode}' is not supported.");
                }
                return true;
            }
            return false;
        }



        /// <summary>
        /// 일시정지된 업데이트를 다시 재생합니다.
        /// <para>
        /// DOTween 모드에서는 Sequence를 재생하고, UniTask 모드에서는 IsWorking 플래그를 true로 설정합니다.
        /// </para>
        /// </summary>
        public void ReStartWorking()
        {
            switch (CurrentUpdateMode)
            {
                case TimeUpdateMode.DOTween:
                if (IsWorking)
                {
                    sequence?.Play();
                }
                break;
                case TimeUpdateMode.UniTask:
                IsWorking = true;
                break;
                default:
                throw new NotSupportedException($"TimeUpdateMode '{CurrentUpdateMode}' is not supported.");
            }
        }



        /// <summary>
        /// 타이머를 강제 종료합니다.
        /// <para>
        /// success 매개변수가 true이면 성공 이벤트를 먼저 실행한 후 종료 처리합니다.
        /// </para>
        /// </summary>
        /// <param name="success">성공 이벤트를 발동시킬지 여부</param>
        public void ShutDown(bool success)
        {
            if (success)
            {
                successEvent?.Invoke();
            }
            End();
        }



        ///======================================================================================================================================================



        /// <summary>
        /// UniTask 기반 업데이트 루프.
        /// 활성화 상태(IsEnable)와 CancellationToken에 따라 매 프레임 deltaTimeFunc을 호출하여 Timer를 업데이트하고, 
        /// 업데이트 이벤트를 실행하며, EndTime 도달 시 Success 처리를 호출합니다.
        /// </summary>
        private async UniTask UpdateUniTaskLoop()
        {
            while (IsEnable && !uniTaskCTS.Token.IsCancellationRequested)
            {
                if (IsWorking)
                {
                    float dt = deltaTimeFunc.Invoke();
                    Timer += dt;
                    updateEvent?.Invoke(this, dt);

                    if (EndTime >= 0f && EndTime <= Timer)
                    {
                        Success();
                    }
                }
                await UniTask.Yield();
            }
        }



        /// <summary>
        /// DOTween 기반 Sequence 업데이트 콜백.
        /// 매 프레임 deltaTimeFunc을 호출하여 Timer를 업데이트하고, 업데이트 이벤트를 실행하며, EndTime 도달 시 Success 처리를 호출합니다.
        /// </summary>
        private void SequenceUpdate()
        {
            float dt = deltaTimeFunc.Invoke();
            Timer += dt;
            updateEvent?.Invoke(this, dt);
            if (EndTime >= 0f && EndTime <= Timer)
            {
                Success();
            }
        }



        /// <summary>
        /// 업데이트 종료 처리를 수행합니다.
        /// <para>
        /// IsWorking 플래그를 false로 설정하고, DOTween 모드에서는 Sequence를 일시정지합니다.
        /// 만약 일회용(IsWorkingOnce) 모드라면 Disable을 호출하여 전체를 비활성화합니다.
        /// 종료 이벤트를 호출합니다.
        /// </para>
        /// </summary>
        private void End()
        {
            IsWorking = false;
            if (CurrentUpdateMode == TimeUpdateMode.DOTween)
            {
                sequence?.Pause();
            }
            Timer = 0f;

            EndEvent?.Invoke();

            // 일회용 모드인 경우, 종료 후 Disable 처리
            if (IsWorkingOnce)
            {
                Disable(true);
            }
        }



        /// <summary>
        /// 성공 처리를 수행합니다.
        /// <para>
        /// 성공 이벤트를 호출한 후, 루프 모드 및 조건에 따라 업데이트를 재시작하거나 종료 처리합니다.
        /// </para>
        /// </summary>
        private void Success()
        {
            successEvent?.Invoke();

            if (IsLoop && LoopCondition.Invoke())
            {
                IsWorking = false;
                if (CurrentUpdateMode == TimeUpdateMode.DOTween)
                {
                    sequence?.Pause();
                }
                Timer = 0f;
                StartWorking();
            }
            else
            {
                End();
            }
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 유니티의 Update를 오브젝트가 아닌 <b>UniTask</b> 에서 실행되게끔 해주는 업데이터
    /// </summary>
    [Serializable]
    public class UniTaskUpdater
    {
        ///======================================================================================================================================================



        public UniTaskUpdater()
        {

        }



        public UniTaskUpdater(Action updateEvent, PlayerLoopTiming loopTiming)
        {
            Setting(updateEvent, loopTiming);
        }



        public void Setting(Action updateEvent, PlayerLoopTiming loopTiming)
        {
            this.updateEvent = updateEvent;
            this.loopTiming = loopTiming;
        }



        ///======================================================================================================================================================



        //. Loop 동작 여부 체크용
        [SerializeField][ReadOnlyCustom] private bool isRunning;



        //. Loop 도중 취소(종료)를 위한 토큰
        private CancellationTokenSource cts;



        //. 매 프레임(또는 해당 타이밍)마다 실행할 메서드
        private Action updateEvent;



        //. Loop를 어느 PlayerLoopTiming에서 실행할지
        [SerializeField][ReadOnlyCustom] private PlayerLoopTiming loopTiming = PlayerLoopTiming.Update;



        ///======================================================================================================================================================



        //? 실행

        /// <summary>
        /// Update 루프 <b>실행</b>
        /// <para>업데이트 타입을 직접 지정한다</para>
        /// </summary>
        /// <param name="updateEvent">설정된 매 프레임마다 실행될 이벤트</param>
        /// <param name="loopTiming">어떤 프레임마다 반복할지</param>
        public void ExecuteUpdate(Action updateEvent, PlayerLoopTiming loopTiming)
        {
            //! 이미 동작 중인 경우, 중단 후 다시 시작
            if (isRunning) { QuitUpdate(); }

            //. 실행할 메서드와 타이밍, 스레드 설정
            Setting(updateEvent, loopTiming);

            //. 토큰 초기화 및 Loop 시작
            isRunning = true;
            cts = new CancellationTokenSource();
            RunLoopAsync(cts.Token).Forget();
        }



        /// <summary>
        /// Update 루프 <b>실행</b>
        /// <para>설정된 업데이트 타입을 기반으로 실행한다</para>
        /// </summary>
        public void ExecuteUpdate()
        {
            ExecuteUpdate(updateEvent, loopTiming);
        }



        ///======================================================================================================================================================



        //? 종료



        /// <summary>
        /// Update 루프 <b>종료</b>
        /// </summary>
        public void QuitUpdate()
        {
            if (!isRunning) { return; }

            isRunning = false;
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }



        ///======================================================================================================================================================



        ///<summary>
        ///실질적으로 설정된 매 프레임마다 실행되는 UniTask
        /// </summary>
        private async UniTaskVoid RunLoopAsync(CancellationToken token)
        {
            await UniTask.SwitchToMainThread();

            while (!token.IsCancellationRequested)
            {
                await UniTask.Yield(loopTiming, token);

                try
                {
                    updateEvent?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"{e.Message}\n{e.StackTrace}");
                }
            }
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================
}



namespace Legacy
{
    /// <summary>
    /// DOTween을 사용한 업데이트 이벤트
    /// </summary>
    [Obsolete("이런 방식 대신 UniTask를 기반으로한 업데이트 이벤트 사용 권장")]
    public class TimeUpdateEvent
    {
        ///======================================================================================================================================================


        public TimeUpdateEvent()
        {
            TweenUpdateEvent = SequenceUpdate;
        }



        private readonly TweenCallback TweenUpdateEvent;



        ///======================================================================================================================================================



        private Sequence Sequence;



        public bool IsEnable { get; private set; }



        public bool IsWorking { get; private set; } = false;



        public bool IsWorking_Manual { set => IsWorking = value; }



        /// <summary>종료 시간 (변경가능)</summary>
        public float EndTime = 0f;



        /// <summary> 타이머 (작동시 점차증가)</summary>
        public float Timer { get; private set; } = 0f;



        /// <summary>일회용인가?, true일 경우, End (시간이 다됨)에서 Disable됨</summary>
        public bool IsWorkingOnce { get; set; } = false;


        public bool IsLoop { get; set; } = false;

        public Func<bool> LoopCondition = null;



        private Func<float> DeltaTimeFunc = null;



        private Action SuccessEvent = null;
        private Action<TimeUpdateEvent, float> UpdateEvent = null;

        public event Action EndEvent = null;


        /// <summary>비활성화시 이벤트, 기본적으로 어떤 상황에도 직접 null되지않음</summary>
        public event Action<TimeUpdateEvent> DisableEvent = null;


        /// <summary> 남은시간 </summary>
        public float LeftTime => EndTime - Timer;



        /// <summary>Working 중이나, 일시정지 여부를 알아보기</summary>
        public bool IsPlaying => Sequence.IsPlaying();



        ///======================================================================================================================================================



        ///// <summary>업데이터 활성화 시키기 (시퀀스 자동로드)</summary>
        //public void Enable(Func<float> deltaTimeFunc, Action successEvent, Action<TimeUpdateEvent> updateEvent = null)
        //{
        //    if (Sequence == null)
        //    {
        //        Enable(DOTween.Sequence(), deltaTimeFunc, successEvent, updateEvent);
        //    }
        //    else
        //    {
        //        Enable(Sequence, deltaTimeFunc, successEvent, updateEvent);
        //    }
        //}



        /// <summary>업데이터 활성화 시키기 (시퀀스 자동로드)</summary>
        public bool Enable(Func<float> deltaTImeFunc, Action successEvent, Action<TimeUpdateEvent, float> updateEvent = null)
        {
            //? 이미 활성화 되어있다면 false 리턴
            if (IsEnable) { return false; }



            IsEnable = true;



            if (Sequence == null)
            {
                Sequence = DOTween.Sequence();
                Sequence.ReadyForUpdate(TweenUpdateEvent);
            }



            DeltaTimeFunc = deltaTImeFunc;
            SuccessEvent = successEvent;
            UpdateEvent = updateEvent;



            return true;
        }



        public void Disable(bool killSequence)
        {
            DisableEvent?.Invoke(this);

            if (killSequence)
            {
                Sequence.Kill();
                Sequence = null;
            }

            IsEnable = false;
            IsWorking = false;

            Timer = 0f;
            EndTime = 0f;
            DeltaTimeFunc = null;
            SuccessEvent = null;
            UpdateEvent = null;
            EndEvent = null;
            IsWorkingOnce = false;
            IsLoop = false;
            LoopCondition = null;
        }



        ///======================================================================================================================================================



        /// <summary>작동시키기</summary>
        public bool StartWorking()
        {
            //이미 작동중이라면 false 리턴
            if (IsWorking) { return false; }
            IsWorking = true;
            Sequence.Play();
            return true;
        }



        /// <summary>작동중이라면, 일시정지 시키기</summary>
        public bool PauseWorking()
        {
            if (IsWorking)
            {
                Sequence.Pause();
                return true;
            }

            return false;
        }



        /// <summary>일시정지 중이였다면, 다시재생</summary>
        public void ReStartWorking()
        {
            if (IsWorking)
            {
                Sequence.Play();
            }
        }



        private void SequenceUpdate()
        {
            float dt = DeltaTimeFunc.Invoke();

            Timer += dt;

            UpdateEvent?.Invoke(this, dt);

            if (EndTime <= Timer)
            {
                Success();
            }
        }



        private void End()
        {
            IsWorking = false;
            Sequence.Pause();
            Timer = 0f;

            //? 일회용이면, 한번 End됐을떄 Diable된다
            if (IsWorkingOnce)
            {
                Disable(true);
            }

            EndEvent?.Invoke();
        }



        private void Success()
        {
            SuccessEvent?.Invoke();

            if (IsLoop && LoopCondition != null && LoopCondition.Invoke() == true)
            {
                IsWorking = false;
                Sequence.Pause();
                Timer = 0f;
                StartWorking();
            }
            else
            {
                End();
            }
        }



        /// <summary>타이머 강제 종료</summary>
        /// <param name="success">SuccessEvent는 발동시키기</param>
        public void ShutDown(bool success)
        {
            if (success) { Success(); }

            End();
        }



        ///======================================================================================================================================================
    }

}



///======================================================================================================================================================
