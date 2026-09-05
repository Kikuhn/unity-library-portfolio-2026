using System;
using System.Collections.Generic;
using UnityEngine;



namespace Pan.Event
{
    public partial class EventAble
    {
        /// <summary>
        /// 재진입 가능한 로컬 신호 전달에서 snapshot 목록을 재사용합니다.
        /// </summary>
        private List<List<PanBaseEventValue>> LocalSignalDispatchScratchByDepth;
        private int LocalSignalDispatchDepth;



        /// <summary>
        /// 첫 로컬 신호가 gameplay frame에 들어오기 전에 기본 snapshot 저장소를 준비합니다.
        /// </summary>
        private void EnsureLocalSignalDispatchScratch()
        {
            LocalSignalDispatchScratchByDepth ??= new List<List<PanBaseEventValue>>(1);
            if (LocalSignalDispatchScratchByDepth.Count == 0)
            {
                LocalSignalDispatchScratchByDepth.Add(
                    new List<PanBaseEventValue>(EventValueTables.EventValueTablesInitializeCapacity));
            }
        }



        /// <summary>
        /// 현재 <see cref="EventAble"/>에 장착된 수신기에게만 신호를 전달합니다.
        /// </summary>
        /// <typeparam name="TSignal">전달할 신호 타입입니다.</typeparam>
        /// <param name="signal">전달할 신호 값입니다.</param>
        /// <returns>신호를 실제로 수신한 이벤트 밸류 수입니다.</returns>
        public int DispatchLocal<TSignal>(in TSignal signal)
        {
            EnsureLocalSignalDispatchScratch();

            if (LocalSignalDispatchDepth == LocalSignalDispatchScratchByDepth.Count)
            {
                LocalSignalDispatchScratchByDepth.Add(new List<PanBaseEventValue>(EventValueTables.EventValueTablesInitializeCapacity));
            }

            List<PanBaseEventValue> snapshot = LocalSignalDispatchScratchByDepth[LocalSignalDispatchDepth++];
            EventValueTables.CopyEventValuesTo(snapshot);

            int receivedCount = 0;

            try
            {
                for (int i = 0; i < snapshot.Count; i++)
                {
                    PanBaseEventValue eventValue = snapshot[i];
                    if (!EventValueTables.ContainsCurrent(eventValue)) { continue; }
                    if (!(eventValue is IEventAbleSignalReceiver<TSignal> receiver)) { continue; }

                    try
                    {
                        receiver.ReceiveLocalSignal(Master, in signal);
                        receivedCount++;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception, Master as UnityEngine.Object);
                    }
                }
            }
            finally
            {
                snapshot.Clear();
                LocalSignalDispatchDepth--;
            }

            return receivedCount;
        }
    }
}
