using UnityEngine;
using System;
using DG.Tweening;



//? [Game] 스탯 관련을 정리한 정도의 코드



namespace Pan.Util.Game
{
    ///======================================================================================================================================================



    //? 공격속도 매니저



    public class AttackSpeedManager
    {
        ///======================================================================================================================================================



        public AttackSpeedManager(Func<float> deltaTimeFunc)
        {
            DeltaTimeFunc = deltaTimeFunc;
            TimeUpdater = new TimeUpdateEvent();
        }



        ///======================================================================================================================================================



        public Func<float> DeltaTimeFunc;



        private readonly TimeUpdateEvent TimeUpdater;



        ///======================================================================================================================================================



        /// <summary>공격 후, 다음 공격까지 걸리는 시간 (1이면 공격이후 1초 후에 공격가능)</summary>
        public float WaitNextTime
        {
            get => waitNextTime;
            set
            {
                waitNextTime = value;
                TimeUpdater.EndTime = waitNextTime;
            }
        }
        private float waitNextTime = 1f;



        /// <summary>공속 타이머 (작동 시작시 타이머가 더해지기 시작하고, 공격속도 값과 같아지거나 넘어가면 작동중지됨)</summary>
        public virtual float Timer() => TimeUpdater.Timer;



        public bool IsWorking { get; private set; } = false;



        ///======================================================================================================================================================



        public bool StartAttackSpeedTimer()
        {
            //? 이미 작동중이라면 false 반환
            if (IsWorking) { return false; }

            TimeUpdater.Enable(DeltaTimeFunc, EndTimerEvent, null);
            TimeUpdater.EndTime = waitNextTime;
            TimeUpdater.StartWorking();

            IsWorking = true;
            return true;
        }



        private void EndTimerEvent()
        {
            IsWorking = false;
        }



        /// <summary>공격 타이머 초기화 (평캔)</summary>
        public void RefreshTimer()
        {
            if (!IsWorking) { return; }
            TimeUpdater.ShutDown(true);
        }



        public void Reset(bool disableTimeUpdater = false, bool killSequence = false)
        {
            WaitNextTime = 1f;

            if (disableTimeUpdater)
            {
                TimeUpdater.Disable(killSequence);
            }
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================
}
