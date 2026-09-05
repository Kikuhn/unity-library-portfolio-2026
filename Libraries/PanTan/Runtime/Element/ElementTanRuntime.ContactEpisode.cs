using System.Collections.Generic;



namespace Pan.Tan.Element
{
    /// <summary>
    /// 고정 스텝 경계에서 논리 대상별 접촉 episode를 결정적으로 관리합니다.
    /// </summary>
    public sealed partial class ElementTanRuntime
    {
        /// <summary>
        /// 공유 ElementWorld Tick 직전에 정확히 한 번 호출하여 접촉 episode의 새 고정 스텝을 엽니다.
        /// 직전 스텝에 없던 대상의 이후 접촉은 새 episode로 전달할 수 있습니다.
        /// </summary>
        public void PrepareFixedStep(ushort substepIndex)
        {
            if (!IsAvailable) { return; }

            keyScratch.Clear();
            foreach (TanKey key in records.Keys) { keyScratch.Add(key); }
            for (int i = 0; i < keyScratch.Count; i++)
            {
                TanKey key = keyScratch[i];
                if (!records.TryGetValue(key, out TanRecord record)) { continue; }

                List<TanTargetHandle> previous = record.PreviousStepContacts;
                record.PreviousStepContacts = record.CurrentStepContacts;
                record.CurrentStepContacts = previous;
                record.CurrentStepContacts?.Clear();
                records[key] = record;
            }
            keyScratch.Clear();
        }



        internal bool TryRecordContactEpisode(in TanKey key, in TanTargetHandle target)
        {
            if (!target.IsValid || !records.TryGetValue(key, out TanRecord record) ||
                (record.Spawn.Source.IsValid && target == record.Spawn.Source))
            {
                return false;
            }

            List<TanTargetHandle> currentContacts = record.CurrentStepContacts;
            if (currentContacts == null)
            {
                currentContacts = RentContactList();
                record.CurrentStepContacts = currentContacts;
                records[key] = record;
            }

            if (currentContacts.Contains(target)) { return false; }
            currentContacts.Add(target);
            return record.PreviousStepContacts == null || !record.PreviousStepContacts.Contains(target);
        }

        private List<TanTargetHandle> RentContactList() =>
            contactListPool.Count > 0 ? contactListPool.Pop() : new List<TanTargetHandle>(2);

        private void ReturnContactList(List<TanTargetHandle> targets)
        {
            if (targets == null) { return; }
            targets.Clear();
            contactListPool.Push(targets);
        }
    }
}
