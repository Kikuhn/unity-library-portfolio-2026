using System;
using System.Collections.Generic;
using UnityEngine;



namespace Pan.HighDensityElement.Physics2DBridge
{
    public sealed partial class Physics2DBridgeRegistry
    {
        ///======================================================================================================================================================
        //? Core 접촉 사실을 Legacy 대상과 receiver로 복원
        ///======================================================================================================================================================



        /// <summary>
        /// lane-only 생성자를 사용하는 통합 코드가 정렬된 Element Fact를 Legacy receiver에 명시적으로 전달합니다.
        /// ElementWorld 생성자는 이 메서드를 자동으로 구독합니다.
        /// </summary>
        /// <returns>호출한 논리 receiver 수입니다.</returns>
        public int DispatchElementFact(in ElementFact fact)
        {
            if (disposed || fact.BridgeTargetId <= 0 ||
                !targets.TryGetValue(fact.BridgeTargetId, out TargetRecord targetRecord))
            {
                return 0;
            }

            IElementPhysicsFactReceiver2D[] receivers = targetRecord.Receivers;
            if (receivers == null || receivers.Length == 0) { return 0; }

            Physics2DBridgeTarget target = CreatePublicTarget(fact.BridgeTargetId, targetRecord);
            var bridgedFact = new ElementPhysicsFact2D(in fact, in target, synchronizationSequence);
            var stamp = new ReceiverDispatchStamp(
                fact.Element,
                fact.Type,
                fact.SubstepIndex,
                synchronizationSequence);
            int invoked = 0;
            for (int i = 0; i < receivers.Length; i++)
            {
                IElementPhysicsFactReceiver2D receiver = receivers[i];
                if (receiver == null || receiver is UnityEngine.Object unityObject && unityObject == null) { continue; }
                if (receiverDispatchStamps.TryGetValue(receiver, out ReceiverDispatchStamp previous) &&
                    previous.Equals(stamp))
                {
                    continue;
                }

                receiverDispatchStamps[receiver] = stamp;
                try
                {
                    receiver.ReceiveElementPhysicsFact2D(in bridgedFact);
                    invoked++;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, receiver as UnityEngine.Object);
                }
            }
            return invoked;
        }



        /// <summary>
        /// Core contact의 bridge target id를 원본 Collider2D로 복원합니다.
        /// </summary>
        public bool TryGetCollider(int bridgeTargetId, out Collider2D collider)
        {
            if (!disposed && targets.TryGetValue(bridgeTargetId, out TargetRecord target) && target.Collider != null)
            {
                collider = target.Collider;
                return true;
            }

            collider = null;
            return false;
        }



        /// <summary>
        /// Core contact의 bridge target id를 원본 Collider/Rigidbody/owner 정보로 복원합니다.
        /// </summary>
        public bool TryGetTarget(int bridgeTargetId, out Physics2DBridgeTarget target)
        {
            if (!disposed && targets.TryGetValue(bridgeTargetId, out TargetRecord record))
            {
                target = CreatePublicTarget(bridgeTargetId, record);
                return target.IsValid;
            }

            target = default;
            return false;
        }



        private static Physics2DBridgeTarget CreatePublicTarget(
            int bridgeTargetId,
            TargetRecord record)
        {
            return new Physics2DBridgeTarget(
                bridgeTargetId,
                record.Collider,
                record.Body != null ? record.Body.Rigidbody : null,
                record.Owner,
                record.Layer,
                record.IsTrigger);
        }



        private void RefreshFactReceiverCache(BodyRecord record)
        {
            if (record == null) { return; }
            for (int i = 0; i < record.TargetIds.Count; i++)
            {
                int targetId = record.TargetIds[i];
                if (!targets.TryGetValue(targetId, out TargetRecord targetRecord)) { continue; }

                receiverScratch.Clear();
                receiverDedupScratch.Clear();
                Transform stop = record.Owner != null ? record.Owner.transform : null;
                Transform current = targetRecord.Collider != null
                    ? targetRecord.Collider.transform
                    : targetRecord.Owner != null ? targetRecord.Owner.transform : stop;
                while (current != null)
                {
                    AddFactReceivers(current.gameObject);
                    if (current == stop) { break; }
                    current = current.parent;
                }

                if (stop != null && current == null) { AddFactReceivers(stop.gameObject); }
                if (targetRecord.Owner != null) { AddFactReceivers(targetRecord.Owner.gameObject); }
                targetRecord.Receivers = receiverScratch.Count == 0
                    ? Array.Empty<IElementPhysicsFactReceiver2D>()
                    : receiverScratch.ToArray();
            }
        }



        private void AddFactReceivers(GameObject gameObject)
        {
            if (gameObject == null) { return; }
            receiverComponentScratch.Clear();
            gameObject.GetComponents(receiverComponentScratch);
            for (int i = 0; i < receiverComponentScratch.Count; i++)
            {
                if (receiverComponentScratch[i] is IElementPhysicsFactReceiver2D receiver &&
                    receiverDedupScratch.Add(receiver))
                {
                    receiverScratch.Add(receiver);
                }
            }
        }



        private void OnElementFactDispatched(in ElementFact fact)
        {
            DispatchElementFact(in fact);
        }



    }
}
