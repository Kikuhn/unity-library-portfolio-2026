using System.Collections.Generic;



namespace Pan.Event
{
    /// <summary>창고 이벤트 밸류, Key : string, Value : 받아온 T 의 딕셔너리를 관리</summary>
    /// <typeparam name="T">물품 Type</typeparam>
    public abstract class BaseValue_Storage<TEventValue, T> : PanBaseEventValue.EventAbles<TEventValue> where TEventValue : PanBaseEventValue.EventAbles<TEventValue>, new()
    {
        ///======================================================================================================================================================



        public BaseValue_Storage()
        {
            Supplies = new Dictionary<string, T>(SuppliesCapacity);
        }



        ///======================================================================================================================================================



        ///<summary>
        /// 창고 물품 딕셔너리
        ///</summary>
        protected readonly Dictionary<string, T> Supplies;



        /// <summary>
        /// 창고 초기 용량 (재정의 가능)
        /// </summary>
        protected virtual int SuppliesCapacity { get; } = 0;



        ///======================================================================================================================================================



        /// <summary>물품 추가하기</summary>
        /// <param name="key">물품 이름</param>
        /// <param name="supply">물품</param>
        /// <param name="overlap">중첩 가능 여부</param>
        public virtual void Add(string key, T supply, bool overlap = false)
        {
            //!이미 물품이 존재+중첩가능이면 교체후 리턴,
            if (Supplies.ContainsKey(key))
            {
                if (overlap) { Supplies[key] = supply; }
                return;
            }

            Supplies.Add(key, supply);
        }



        /// <summary>물품 교체하기, 기존에 없으면 실패</summary>
        /// <param name="key">물품 이름</param>
        /// <param name="supply">물품</param>
        public virtual bool Change(string key, T supply)
        {
            if (Supplies.ContainsKey(key))
            {
                Supplies[key] = supply;
                return true;
            }
            return false;
        }



        /// <summary>물품 제거하기</summary>
        /// <param name="key">물품 이름</param>
        public virtual bool Remove(string key)
        {
            return Supplies.Remove(key);
        }



        /// <summary>물품 얻어보기</summary>
        /// <param name="key">물품 이름</param>
        /// <param name="resultSupply">반환되는 물품</param>
        public virtual bool TryGetSupply(string key, out T resultSupply) => Supplies.TryGetValue(key, out resultSupply);



        ///======================================================================================================================================================



        protected sealed override void Disable()
        {
            Supplies.Clear();
            DisableCurrent();
        }



        protected virtual void DisableCurrent() { }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 창고 이벤트 밸류 : 숫자 기반
    /// </summary>
    /// <typeparam name="T">
    /// 물품 Type (숫자)
    /// </typeparam>
    public abstract class BaseValueNumberStorage<TEventValue, T> : BaseValue_Storage<TEventValue, T> where TEventValue : PanBaseEventValue.EventAbles<TEventValue>, new() where T : struct
    {
        /// <summary>숫자를 (없으면 추가) 반환</summary>
        /// <param name="key">이름</param>
        /// <param name="firstNumber">최초 값</param>
        public abstract T Number(string key, T firstNumber);

        /// <summary>숫자의 값을 추가</summary>
        /// <param name="key">숫자 이름</param>
        /// <param name="number">더할 값</param>
        public abstract bool AddNumber(string key, T number);

        /// <summary>숫자의 값을 곱하기</summary>
        /// <param name="key">숫자 이름</param>
        /// <param name="number">곱할 값</param>
        public abstract bool MultipleNumber(string key, T number);
    }
}