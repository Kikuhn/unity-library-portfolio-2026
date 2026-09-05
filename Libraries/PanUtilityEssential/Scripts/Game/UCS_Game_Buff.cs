using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.IO;
using UnityEngine.Events;
using SitraUtils;
using DG.Tweening;
using Pan.Util;



//? [Game] 버프 시스템 관련을 정리한 정도의 코드



namespace Pan.Util.Game
{
    /// <summary>
    /// 버프 태그를 나타내는 열거형입니다.
    /// <para>Gain: 버프 증가, Loss: 버프 감소를 의미합니다.</para>
    /// </summary>
    public enum EStatBuffTags { Gain, Loss }



    /// <summary>
    /// 버프를 적용하거나 해제하는 기능을 정의하는 인터페이스입니다.
    /// </summary>
    public interface IStatBuff
    {
        /// <summary>
        /// 버프를 적용합니다.
        /// </summary>
        void ApplyBuff();



        /// <summary>
        /// 버프를 해제합니다.
        /// </summary>
        void RemoveBuff();
    }



    /// <summary>
    /// 버프에 태그를 부여하기 위한 인터페이스입니다.
    /// </summary>
    public interface IStatBuffTag
    {
        /// <summary>
        /// 버프에 부여된 태그 배열입니다.
        /// </summary>
        EStatBuffTags[] Tags { get; set; }



        /// <summary>
        /// 대상 버프에 지정한 태그들을 설정합니다.
        /// </summary>
        /// <param name="target">태그를 설정할 대상</param>
        /// <param name="tags">설정할 태그들</param>
        public static void SetTag(IStatBuffTag target, params EStatBuffTags[] tags)
        {
            target.Tags = tags;
        }
    }



    /// <summary>
    /// 버프 매니저에서 사용하기 위한 버프 식별 정보를 정의하는 인터페이스입니다.
    /// </summary>
    public interface IStatBuff_ForManager
    {
        /// <summary>
        /// 버프의 키 이름을 나타냅니다.
        /// </summary>
        public string BuffKeyName { get; set; }
    }



    /// <summary>
    /// 기본 버프 클래스입니다. 버프 적용 및 해제 기능의 기본 구현을 제공합니다.
    /// </summary>
    public class StatBuff : IStatBuff, IStatBuff_ForManager
    {
        ///======================================================================================================================================================



        string IStatBuff_ForManager.BuffKeyName { get; set; }



        ///======================================================================================================================================================



        /// <summary>
        /// 버프를 적용하는 기본 구현입니다.
        /// </summary>
        public virtual void ApplyBuff()
        {

        }



        /// <summary>
        /// 버프를 해제하는 기본 구현입니다.
        /// </summary>
        public virtual void RemoveBuff()
        {

        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 사용자 정의 버프 클래스입니다.
    /// 버프 적용 및 해제 시 실행할 액션을 이벤트 형태로 관리합니다.
    /// </summary>
    public class CustomStatBuff : StatBuff
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 사용자 정의 버프를 생성합니다.
        /// </summary>
        /// <param name="applyBuff_Event">버프 적용 시 실행할 액션</param>
        /// <param name="removeBuff_Event">버프 해제 시 실행할 액션</param>
        public CustomStatBuff(Action applyBuff_Event, Action removeBuff_Event)
        {
            ApplyBuff_Event = applyBuff_Event;
            RemoveBuff_Event = removeBuff_Event;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 버프 적용 시 실행할 액션입니다.
        /// </summary>
        public Action ApplyBuff_Event { get; private set; }



        /// <summary>
        /// 버프 해제 시 실행할 액션입니다.
        /// </summary>
        public Action RemoveBuff_Event { get; private set; }



        ///======================================================================================================================================================



        /// <summary>
        /// 버프 적용 및 해제 액션을 모두 수정합니다.
        /// </summary>
        /// <param name="newApplyBuff_Event">새로운 버프 적용 액션</param>
        /// <param name="newRemoveBuff_Event">새로운 버프 해제 액션</param>
        public void EditBuff(Action newApplyBuff_Event, Action newRemoveBuff_Event)
        {
            ApplyBuff_Event = newApplyBuff_Event;
            RemoveBuff_Event = newRemoveBuff_Event;
        }



        /// <summary>
        /// 버프 적용 액션만 수정합니다.
        /// </summary>
        /// <param name="newApplyBuff_Event">새로운 버프 적용 액션</param>
        public void EditBuff_Apply(Action newApplyBuff_Event)
        {
            ApplyBuff_Event = newApplyBuff_Event;
        }



        /// <summary>
        /// 버프 해제 액션만 수정합니다.
        /// </summary>
        /// <param name="newRemoveBuff_Event">새로운 버프 해제 액션</param>
        public void EditBuff_Remove(Action newRemoveBuff_Event)
        {
            RemoveBuff_Event = newRemoveBuff_Event;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 버프를 적용합니다.
        /// </summary>
        public override void ApplyBuff()
        {
            ApplyBuff_Event?.Invoke();
        }



        /// <summary>
        /// 버프를 해제합니다.
        /// </summary>
        public override void RemoveBuff()
        {
            RemoveBuff_Event?.Invoke();
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 태그가 부여된 사용자 정의 버프 클래스입니다.
    /// </summary>
    public class CustomStatBuffwithTag : CustomStatBuff, IStatBuffTag
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 태그가 부여된 사용자 정의 버프를 생성합니다.
        /// </summary>
        /// <param name="applyBuff_Event">버프 적용 시 실행할 액션</param>
        /// <param name="removeBuff_Event">버프 해제 시 실행할 액션</param>
        /// <param name="tags">부여할 버프 태그들</param>
        public CustomStatBuffwithTag(Action applyBuff_Event, Action removeBuff_Event, params EStatBuffTags[] tags) : base(applyBuff_Event, removeBuff_Event)
        {
            IStatBuffTag.SetTag(this, tags);
        }



        /// <summary>
        /// 버프에 부여된 태그 배열입니다.
        /// </summary>
        public EStatBuffTags[] Tags { get; set; }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 이름이 부여된 버프 클래스입니다.
    /// 버프 이름과 버프 인스턴스를 함께 관리하며, 매니저를 통해 적용 또는 제거할 수 있습니다.
    /// </summary>
    /// <typeparam name="TBuff">버프 타입</typeparam>
    public class NamedStatBuff<TBuff> : IName where TBuff : StatBuff
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 버프 이름과 인스턴스를 지정하여 NamedStatBuff를 생성합니다.
        /// </summary>
        /// <param name="buffName">버프 이름</param>
        /// <param name="newStatBuff">버프 인스턴스</param>
        public NamedStatBuff(string buffName, TBuff newStatBuff)
        {
            BuffName = buffName;
            StatBuff = newStatBuff;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 버프의 이름을 나타냅니다.
        /// </summary>
        string IName.CurrentName => BuffName;



        /// <summary>
        /// 버프 이름 (읽기 전용)
        /// </summary>
        public readonly string BuffName;



        /// <summary>
        /// 버프 인스턴스 (읽기 전용)
        /// </summary>
        public readonly TBuff StatBuff;



        ///======================================================================================================================================================



        /// <summary>
        /// 지정된 버프 매니저에 버프를 적용합니다.
        /// </summary>
        /// <param name="manager">버프 매니저</param>
        /// <returns>버프 적용 성공 여부</returns>
        public bool ApplyBuff(StatBuffManager manager)
        {
            return manager.AddBuff(BuffName, StatBuff);
        }



        /// <summary>
        /// 지정된 버프 매니저에서 버프를 제거합니다.
        /// </summary>
        /// <param name="manager">버프 매니저</param>
        /// <returns>버프 제거 성공 여부</returns>
        public bool RemoveBuff(StatBuffManager manager)
        {
            return manager.RemoveBuff(BuffName);
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 태그가 부여된 기본 버프 클래스입니다.
    /// </summary>
    public class StatBuffwithTag : StatBuff, IStatBuffTag
    {
        ///======================================================================================================================================================
     

        
        /// <summary>
        /// 지정한 태그들을 부여하여 StatBuffwithTag 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="tags">부여할 버프 태그들</param>
        public StatBuffwithTag(params EStatBuffTags[] tags)
        {
            IStatBuffTag.SetTag(this, tags);
        }



        /// <summary>
        /// 버프에 부여된 태그 배열입니다.
        /// </summary>
        public EStatBuffTags[] Tags { get; set; }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 간단한 버프 값을 표현하는 구조체입니다.
    /// </summary>
    public struct StatBuffValue : IStatBuff
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 버프 값을 생성합니다.
        /// </summary>
        /// <param name="applyBuffEvent">버프 적용 시 실행할 액션</param>
        /// <param name="removeBuffEvent">버프 해제 시 실행할 액션</param>
        public StatBuffValue(Action applyBuffEvent, Action removeBuffEvent)
        {
            ApplyBuffEvent = applyBuffEvent;
            RemoveBuffEvent = removeBuffEvent;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 버프 적용 액션입니다.
        /// </summary>
        public Action ApplyBuffEvent;



        /// <summary>
        /// 버프 해제 액션입니다.
        /// </summary>
        public Action RemoveBuffEvent;



        ///======================================================================================================================================================



        /// <summary>
        /// 버프를 적용합니다.
        /// </summary>
        void IStatBuff.ApplyBuff()
        {
            ApplyBuffEvent?.Invoke();
        }



        /// <summary>
        /// 버프를 해제합니다.
        /// </summary>
        void IStatBuff.RemoveBuff()
        {
            RemoveBuffEvent?.Invoke();
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 버프를 관리하는 매니저 클래스입니다.
    /// 버프의 추가, 제거, 전체 초기화 및 태그별 초기화 기능을 제공합니다.
    /// </summary>
    public class StatBuffManager
    {
        ///======================================================================================================================================================



        /// <summary>
        /// StatBuffManager 인스턴스를 생성합니다.
        /// 내부 버프 딕셔너리와 태그별 버프 딕셔너리를 초기화합니다.
        /// </summary>
        public StatBuffManager()
        {
            Buff_Dictionary = new Dictionary<string, StatBuff>();

            SU_Collection_Enums.SetEnumDictionaryNew(out Tag_Buffs);
        }



        ///======================================================================================================================================================



        private readonly Dictionary<string, StatBuff> Buff_Dictionary;
        private readonly Dictionary<EStatBuffTags, HashSet<StatBuff>> Tag_Buffs;



        ///======================================================================================================================================================



        /// <summary>
        /// 버프에 태그를 추가합니다.
        /// </summary>
        /// <param name="statBuff">태그를 추가할 버프</param>
        /// <returns>버프에 태그가 추가되었으면 true, 아니면 false</returns>
        private bool TryAddTagBuff(StatBuff statBuff)
        {
            if (statBuff is not IStatBuffTag tagBuff) { return false; }

            foreach (var tag in tagBuff.Tags)
            {
                Tag_Buffs[tag].Add(statBuff);
            }

            return true;
        }



        /// <summary>
        /// 버프에서 태그를 제거합니다.
        /// </summary>
        /// <param name="statBuff">태그를 제거할 버프</param>
        /// <returns>버프에서 태그가 제거되었으면 true, 아니면 false</returns>
        private bool TryRemoveTagBuff(StatBuff statBuff)
        {
            if (statBuff is not IStatBuffTag tagBuff) { return false; }

            foreach (var tag in tagBuff.Tags)
            {
                Tag_Buffs[tag].Remove(statBuff);
            }

            return true;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 버프를 추가하고 적용합니다.
        /// </summary>
        /// <typeparam name="TBuff">추가할 버프 타입</typeparam>
        /// <param name="buffName">버프 이름</param>
        /// <param name="buff">추가할 버프 인스턴스</param>
        /// <returns>버프 추가 및 적용에 성공하면 true, 실패하면 false</returns>
        public bool AddBuff<TBuff>(string buffName, TBuff buff) where TBuff : StatBuff, IStatBuff_ForManager
        {
            if (Buff_Dictionary.ContainsKey(buffName)) { return false; }

            buff.BuffKeyName = buffName;

            Buff_Dictionary[buffName] = buff;
            buff.ApplyBuff();

            TryAddTagBuff(buff);

            return true;
        }



        /// <summary>
        /// 지정된 버프를 제거합니다.
        /// </summary>
        /// <param name="buffName">제거할 버프의 이름</param>
        /// <returns>버프 제거에 성공하면 true, 실패하면 false</returns>
        public bool RemoveBuff(string buffName)
        {
            if (Buff_Dictionary.TryGetValue(buffName, out var statBuff) == false) { return false; }

            (statBuff as IStatBuff_ForManager).BuffKeyName = null;

            statBuff.RemoveBuff();
            Buff_Dictionary.Remove(buffName);

            TryRemoveTagBuff(statBuff);

            return true;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 모든 버프를 제거하고 초기화합니다.
        /// </summary>
        public void ClearAllBuff()
        {
            foreach (var buff in Buff_Dictionary)
            {
                buff.Value.RemoveBuff();
            }

            Buff_Dictionary.Clear();
        }



        /// <summary>
        /// 지정된 태그에 해당하는 모든 버프를 제거합니다.
        /// </summary>
        /// <param name="tag">제거할 버프의 태그</param>
        public void ClearAllBuff_ByTag(EStatBuffTags tag)
        {
            var tagBuffs = Tag_Buffs[tag];

            foreach (var buff in tagBuffs)
            {
                buff.RemoveBuff();
                Buff_Dictionary.Remove((buff as IStatBuff_ForManager).BuffKeyName);
            }

            tagBuffs.Clear();
        }



        ///======================================================================================================================================================
    }
}
