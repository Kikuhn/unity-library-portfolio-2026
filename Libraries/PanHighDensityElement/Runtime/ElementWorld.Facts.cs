using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;



namespace Pan.HighDensityElement
{
    public sealed partial class ElementWorld
    {



        /// <summary>
        /// 진단 캡처가 활성화된 동안 기록한 최근 Fact를 복사합니다.
        /// </summary>
        public void CopyRecentFacts(List<ElementFact> destination)
        {
            if (destination == null) { throw new ArgumentNullException(nameof(destination)); }
            ThrowIfDisposed();

            destination.Clear();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            recentDebugFacts.CopyTo(destination);
#endif
        }





        private void CollectQueuedFacts()
        {
            CollectQueuedFacts(kinematicFactQueue);
            CollectQueuedFacts(areaFactQueue);
            CollectQueuedFacts(physicsFactQueue);
        }





        private void CollectQueuedFacts(NativeQueue<ElementFact> source)
        {
            while (source.TryDequeue(out ElementFact fact))
            {
                factBuffer.Add(fact);
            }
        }





        private void SortFactsDeterministically()
        {
            factBuffer.Sort();
        }





        private void DeduplicateContactFacts()
        {
            contactFactIdentities.Clear();
            int writeIndex = 0;
            for (int readIndex = 0; readIndex < factBuffer.Length; readIndex++)
            {
                ElementFact fact = factBuffer[readIndex];
                if (fact.Type == ElementFactType.Contact)
                {
                    var identity = new ContactFactIdentity(in fact);
                    if (!contactFactIdentities.Add(identity)) { continue; }
                }

                if (writeIndex != readIndex) { factBuffer[writeIndex] = fact; }
                writeIndex++;
            }

            if (writeIndex != factBuffer.Length)
            {
                factBuffer.ResizeUninitialized(writeIndex);
            }
        }





        private NativeQueue<ElementFact> GetFactQueue(ElementLane lane)
        {
            switch (lane)
            {
                case ElementLane.QuerySprite2D: return kinematicFactQueue;
                case ElementLane.AreaSensorSprite2D: return areaFactQueue;
                case ElementLane.DynamicBodySprite2D: return physicsFactQueue;
                default: throw new ArgumentOutOfRangeException(nameof(lane));
            }
        }





        private void DispatchFact(in ElementFact fact)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (EnableDebugCapture)
            {
                recentDebugFacts.Add(in fact);
            }
#endif

            int currentDepth = dispatchDepth++;
            if (currentDepth == factDispatchScratchByDepth.Count)
            {
                factDispatchScratchByDepth.Add(new List<ElementFactHandler>(factHandlers.Count));
            }

            List<ElementFactHandler> snapshot = factDispatchScratchByDepth[currentDepth];
            snapshot.Clear();
            snapshot.AddRange(factHandlers);
            try
            {
                for (int i = 0; i < snapshot.Count; i++)
                {
                    try
                    {
                        snapshot[i].Invoke(in fact);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }
            }
            finally
            {
                snapshot.Clear();
                dispatchDepth--;
            }
        }
    }
}