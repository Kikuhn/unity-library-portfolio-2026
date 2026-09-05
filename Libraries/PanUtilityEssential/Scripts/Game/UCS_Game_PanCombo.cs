using System;
using System.Collections.Generic;
using UnityEngine;



//? [Game] 콤보 유틸리를 정리한 정도의 코드



namespace Pan.Util.Game
{
    ///======================================================================================================================================================



    //? 콤보



    /// <summary>
    /// 콤보용 클래스.
    /// 이 클래스는 일련의 콤보 동작을 관리하며, 콤보 실행, 업데이트, 예약 및 리셋 기능을 제공합니다.
    /// </summary>
    public class PanOneCombo
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 콤보 인터페이스.
        /// 각 콤보는 실행 시간, 대기 시간, 예약 가능 여부 및 실행 액션을 정의합니다.
        /// </summary>
        public interface ICombo
        {
            /// <summary>
            /// 콤보 동작 실행 시간.
            /// </summary>
            float ActionTime { get; }



            /// <summary>
            /// 콤보 동작 실행 후 대기해야 하는 시간.
            /// </summary>
            float WaitTime { get; }



            /// <summary>
            /// 다음 콤보 예약 허용 여부.
            /// </summary>
            bool WaitForNext { get; }



            /// <summary>
            /// 콤보 실행 액션.
            /// ICombo 인스턴스를 받아 실행 결과를 bool로 반환합니다.
            /// </summary>
            Func<ICombo, bool> Action { get; }



            /// <summary>
            /// 콤보를 실행합니다.
            /// </summary>
            /// <returns>실행 결과 (true/false).</returns>
            bool Execute();
        }



        /// <summary>
        /// 콤보 실행 액션을 구현한 클래스.
        /// </summary>
        public class ComboAction : ICombo
        {
            /// <summary>
            /// ComboAction 생성자.
            /// </summary>
            /// <param name="actionTime">실행 시간.</param>
            /// <param name="waitTime">대기 시간.</param>
            /// <param name="waitForNextAction">예약 허용 여부.</param>
            /// <param name="action">실행 액션 함수.</param>
            public ComboAction(float actionTime, float waitTime, bool waitForNextAction, Func<ICombo, bool> action)
            {
                ActionTime = actionTime;
                WaitTime = waitTime;
                WaitForNext = waitForNextAction;
                Action = action;
            }



            /// <summary>
            /// 실행 시간.
            /// </summary>
            public float ActionTime { get; set; }



            /// <summary>
            /// 대기 시간.
            /// </summary>
            public float WaitTime { get; set; }



            /// <summary>
            /// 예약 허용 여부.
            /// </summary>
            public bool WaitForNext { get; set; }



            /// <summary>
            /// 실행 액션.
            /// </summary>
            public Func<ICombo, bool> Action { get; set; }



            /// <summary>
            /// 콤보를 실행합니다.
            /// </summary>
            /// <returns>실행 결과를 반환합니다.</returns>
            public bool Execute()
            {
                return Action.Invoke(this);
            }
        }



        ///======================================================================================================================================================



        /// <summary>
        /// PanOneCombo 생성자.
        /// 최대 대기 티켓 수를 설정하고 콤보 리스트를 초기화합니다.
        /// </summary>
        /// <param name="maxWaitTicket">최대 대기 티켓 수.</param>
        public PanOneCombo(int maxWaitTicket)
        {
            ComboList = new LinkedList<ICombo>();
            MaxWaitTicket = maxWaitTicket;
        }



        private readonly LinkedList<ICombo> ComboList;



        /// <summary>
        /// 현재 실행 중인 콤보 노드.
        /// </summary>
        public LinkedListNode<ICombo> NowCombo { get; private set; }



        /// <summary>
        /// 콤보가 활성 상태인지 여부 (현재 콤보가 존재하면 true).
        /// </summary>
        public bool Active => NowCombo != null;



        ///======================================================================================================================================================



        /// <summary>
        /// 콤보 실행 타이머.
        /// </summary>
        public float Timer { get; private set; } = 0f;



        ///======================================================================================================================================================



        private int waitTicket = 0;



        /// <summary>
        /// 현재 대기 티켓 수.
        /// 값은 0과 MaxWaitTicket 사이로 제한됩니다.
        /// </summary>
        public int WaitTicket
        {
            get => waitTicket;
            private set
            {
                waitTicket.SetClamp0Max(value, MaxWaitTicket);
                //waitTicket = Mathf.Clamp(value, 0, MaxWaitTicket);
            }
        }



        /// <summary>
        /// 최대 대기 티켓 수.
        /// </summary>
        public int MaxWaitTicket = 5;



        /// <summary>
        /// 대기 티켓이 존재하는지 여부.
        /// </summary>
        public bool IsWaitTicket => WaitTicket > 0;



        ///======================================================================================================================================================



        /// <summary>
        /// 새로운 콤보를 추가합니다.
        /// </summary>
        /// <param name="combo">추가할 콤보.</param>
        /// <param name="isFirst">첫 번째 콤보로 추가할지 여부 (리스트가 비어있을 경우 자동 적용).</param>
        public void AddNewCombo(ICombo combo, bool isFirst = false)
        {
            var value = new LinkedListNode<ICombo>(combo);

            if (isFirst || (!isFirst && ComboList.Count == 0))
            {
                ComboList.AddFirst(value);
                //NowCombo = value;
                //! 20230201 비활성화, 이게채워져있으면 맨처음 공격이 2타부터 나감 (그이후로는 정상작동
            }
            else
            {
                ComboList.AddLast(value);
            }
        }



        /// <summary>
        /// 경과 시간(deltaTime)에 따라 타이머를 업데이트하고, 조건에 따라 콤보 실행 또는 종료를 처리합니다.
        /// </summary>
        /// <param name="deltatime">경과 시간.</param>
        public void UpdateTime(float deltatime)
        {
            //? [활성화] : Timer에 시간 더하기 / [비활성화] : return
            if (Active) { Timer += deltatime; } else { return; }

            //? Timer가 현재 콤보의 ActionTime을 초과하면 예약된 콤보 실행 시도
            if (NowCombo.Value.ActionTime <= Timer)
            {
                // 예약 티켓이 있으면 자동 실행
                if (IsWaitTicket)
                {
                    StartCombo();
                }
            }

            //? Timer가 WaitTime을 초과하면 콤보 종료
            if (NowCombo.Value.WaitTime <= Timer)
            {
                End();
            }
        }



        /// <summary>
        /// 콤보 실행 조건에 따라 실행 또는 예약 티켓 증가를 처리합니다.
        /// </summary>
        public void Execute()
        {
            //? [콤보 활성화 상태]
            if (Active)
            {
                //? Timer가 ActionTime 이상이면 콤보 실행
                if (Timer >= NowCombo.Value.ActionTime)
                {
                    StartCombo();
                    return;
                }

                //? ActionTime 미만이지만 예약 허용이면 예약 티켓 증가
                else if (NowCombo.Value.WaitForNext)
                {
                    WaitTicket += 1;
                    return;
                }

                //? 예약 허용이 없으면 예약 티켓 초기화
                else
                {
                    WaitTicket = 0;
                    return;
                }
            }

            //? [콤보 비활성화 상태] => 콤보 실행
            else
            {
                StartCombo();
                return;
            }
        }



        /// <summary>
        /// 콤보 실행을 시작합니다.
        /// 예약 티켓을 감소시키고, 타이머를 초기화한 후 다음 콤보로 전환하여 실행합니다.
        /// </summary>
        private void StartCombo()
        {
            //? 콤보 시작 시 예약 티켓 감소 및 타이머 리셋
            WaitTicket -= 1;
            Timer = 0;

            //? 현재 콤보가 없거나 리스트의 마지막이면 첫 번째 콤보로, 그렇지 않으면 다음 콤보로 전환
            NowCombo = NowCombo == null || NowCombo == ComboList.Last ? ComboList.First : NowCombo.Next;

            //? 현재 콤보의 액션 실행
            NowCombo.Value.Action?.Invoke(NowCombo.Value);
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 콤보를 종료합니다.
        /// 예약 티켓을 초기화하고, 현재 콤보 및 타이머를 리셋합니다.
        /// </summary>
        private void End()
        {
            WaitTicket = 0;
            NowCombo = null;
            Timer = 0;
        }



        /// <summary>
        /// 콤보 시스템을 리셋합니다.
        /// 옵션에 따라 콤보 리스트를 비울 수 있습니다.
        /// </summary>
        /// <param name="clearComboList">콤보 리스트를 비울지 여부.</param>
        public void Reset(bool clearComboList = false)
        {
            End();

            if (clearComboList)
            {
                ComboList.Clear();
            }
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================
}
