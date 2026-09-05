using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;



//? [ValueControl] OverrideField 가 정리되어있는 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    /// <summary>
    /// TValue 형식의 값과 해당 값 변경 시 적용된 Rank를 관리하는 기본 추상 클래스입니다. <br/>
    /// 이 클래스는 상태 변경 이벤트 및 커스텀 값 할당 기능을 제공하며, <br/>
    /// 자식 클래스에서 RefreshValue() 메서드를 구현하여 최종 값(Value)을 갱신할 수 있습니다.
    /// </summary>
    /// <typeparam name="TValue">관리할 값의 형식 (값 형식이어야 합니다)</typeparam>
    /// <typeparam name="TController">값 변경을 주도하는 컨트롤러의 형식 (참조 형식이어야 합니다)</typeparam>
    [Serializable]
    public abstract class BaseValueControlManager<TValue, TController>
        where TValue : struct
        where TController : class
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 기본 생성자입니다. <br/>
        /// 초기 ChangeValueEvent, 기본값(BasicValue), 커스텀 세터(CustomSetterEvent)를 설정합니다.
        /// </summary>
        /// <param name="changeValueEvent">
        /// 상태가 변경될 때 호출되는 이벤트 핸들러입니다. <br/>
        /// 인자 1: 기존 값 (nullable), 인자 2: 새 값
        /// </param>
        /// <param name="firstBasic">초기 기본값. null이면 기본값이 설정되지 않습니다.</param>
        /// <param name="customSetterEvent">
        /// 값 할당 시 커스텀 처리를 하고 싶을 때 사용하는 델리게이트입니다. <br/>
        /// 인자 1: 기존 값, 인자 2: 새 값을 받아 처리한 결과 값을 반환합니다.
        /// </param>
        public BaseValueControlManager(Action<TValue?, TValue> changeValueEvent, TValue? firstBasic = null, Func<TValue, TValue, TValue> customSetterEvent = null, Action emptyValueEvent = null)
        {
            ChangeValueEvent = changeValueEvent;

            if (firstBasic is not null)
            {
                BasicValue = (TValue)firstBasic;
            }

            CustomSetterEvent = customSetterEvent;

            EmptyValueEvent = emptyValueEvent;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 상태를 나타내는 내부 구조체입니다. <br/>
        /// 각 Chip은 KeyRank와 하나의 값과 해당 값을 갱신한 컨트롤러(Updater)를 보유합니다.
        /// </summary>
        [Serializable]
        public struct ValueChips
        {
            /// <summary>
            /// Chip 생성자입니다.
            /// </summary>
            /// <param name="keyRank"></param>
            /// <param name="value">Chip에 저장할 값</param>
            /// <param name="controller">해당 값을 갱신한 컨트롤러</param>
            public ValueChips(int keyRank, TValue value, TController controller)
            {
                KeyRank = keyRank;
                Value = value;
                Controller = controller;
            }

            [SerializeField] public int KeyRank;

            /// <summary>
            /// Chip에 저장된 값입니다.
            /// </summary>
            [SerializeField] public TValue Value;

            /// <summary>
            /// 값을 갱신한 컨트롤러입니다.
            /// </summary>
            [SerializeField] public TController Controller;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 기본값(BasicValue)입니다. <br/>
        /// 이 값은 Value가 null인 경우에도 반환됩니다.
        /// </summary>
        [field: SerializeField] public TValue BasicValue { protected get; set; }



        /// <summary>
        /// 현재 상태 값입니다. <br/>
        /// 값을 설정하면 이전에 적용된 Rank (LastChangedRank)는 초기화되며, <br/>
        /// CustomSetterEvent가 설정되어 있다면 그 결과가 적용됩니다.
        /// </summary>
        protected TValue? Value
        {
            get => value;
            set
            {
                TValue? applyValue;

                // 기존 값, 새 값, 그리고 커스텀 세터가 모두 존재하면 커스텀 세터를 통해 값을 결정함
                if (this.value is not null && value is not null && CustomSetterEvent is not null)
                {
                    applyValue = CustomSetterEvent.Invoke((TValue)this.value, (TValue)value);
                }
                else
                {
                    applyValue = value;
                }

                // 새로 적용된 값이 null이 아니라면 ChangeValueEvent를 호출하여 값 변경을 알림
                if (applyValue is not null)
                {
                    ChangeValueEvent?.Invoke(this.value, (TValue)applyValue);
                }

                this.value = applyValue;
            }
        }
        [SerializeField] private TValue? value;



        /// <summary>
        /// 현재 상태 값이 null일 경우 BasicValue를 반환하고, 그렇지 않으면 Value를 반환합니다.
        /// </summary>
        /// <returns>현재 유효한 값</returns>
        public TValue GetValue()
        {
            return Value ?? BasicValue;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 상태 값이 변경될 때마다 호출되는 이벤트입니다. <br/>
        /// 인자 1: 이전 값, 인자 2: 새 값
        /// </summary>
        public readonly Action<TValue?, TValue> ChangeValueEvent;



        /// <summary>
        /// 값 할당 시 커스텀 처리를 위한 델리게이트입니다. <br/>
        /// 인자 1: 기존 값, 인자 2: 새 값을 받아 처리 결과 값을 반환합니다.
        /// </summary>
        public readonly Func<TValue, TValue, TValue> CustomSetterEvent;



        /// <summary>
        /// 값을 제거하여, 값이 전혀 존재하지 않을때 실행되는 이벤트
        /// </summary>
        public readonly Action EmptyValueEvent;



        ///======================================================================================================================================================



        /// <summary>
        /// 자식 클래스에서 구현해야 하는 추상 메서드입니다. <br/>
        /// 이 메서드를 호출하여 내부 값(Value)을 최신 상태로 갱신합니다.
        /// </summary>
        protected abstract void RefreshValue();



        /// <summary>
        /// 상태를 재설정합니다. <br/>
        /// 내부 값(Value)를 null로 초기화하고, BasicValue를 새 값으로 설정한 후 ChangeValueEvent를 호출합니다.
        /// </summary>
        /// <param name="resetBasicValue">재설정할 기본값</param>
        public virtual void Reset(TValue resetBasicValue)
        {
            Value = null;
            BasicValue = resetBasicValue;
            ChangeValueEvent?.Invoke(this.value, BasicValue);
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// BaseValueControlManager를 상속받아, 여러 개의 Chip(값과 컨트롤러 쌍)을 관리하며, <br/>
    /// 가장 높은 Rank를 가진 Chip의 값을 현재 값으로 사용하는 클래스입니다.
    /// </summary>
    /// <typeparam name="TValue">관리할 값의 형식 (구조체)</typeparam>
    /// <typeparam name="TController">값 변경을 주도하는 컨트롤러의 형식 (참조형)</typeparam>
    [Serializable]
    public class ValueControlManager<TValue, TController> : BaseValueControlManager<TValue, TController>
        where TValue : struct
        where TController : class
    {

        ///======================================================================================================================================================



        /// <summary>
        /// 생성자입니다. <br/>
        /// BaseValueControlManager의 생성자를 호출하여 초기 이벤트, 기본값, 커스텀 세터를 설정합니다.
        /// </summary>
        /// <param name="changeValueEvent">값 변경 시 호출되는 이벤트 핸들러</param>
        /// <param name="firstBasic">초기 기본값 (없으면 null)</param>
        /// <param name="customSetterEvent">값 할당 시 커스텀 처리할 델리게이트 (선택 사항)</param>
        public ValueControlManager(Action<TValue?, TValue> changeValueEvent, TValue? firstBasic = null, Func<TValue, TValue, TValue> customSetterEvent = null, Action emptyValueEvent = null)
            : base(changeValueEvent, firstBasic, customSetterEvent, emptyValueEvent) { }



        ///======================================================================================================================================================



        /// <summary>
        /// Chip들을 Rank(정수 키)를 기준으로 관리하는 리스트
        /// </summary>
        [SerializeField]
        private List<ValueChips> Chips = new List<ValueChips>();



        ///======================================================================================================================================================



        /// <summary>
        /// 현재 등록된 Chip들 중 가장 높은 Rank 값을 반환합니다. <br/>
        /// Chip이 하나도 없으면 0을 반환합니다.
        /// </summary>
        /// <returns>가장 높은 Rank 값</returns>
        public int GetHighstChip
        {
            get
            {
                if (Chips.Count == 0)
                {
                    return 0;
                }
                int highestRank = int.MinValue;
                for (int i = 0; i < Chips.Count; i++)
                {
                    if (Chips[i].KeyRank > highestRank) { highestRank = Chips[i].KeyRank; }
                }

                return highestRank;
            }
        }



        /// <summary>
        /// 지정된 Rank와 컨트롤러에 해당하는 Chip을 추가하거나 업데이트합니다. <br/>
        /// 만약 같은 Rank가 의 Chip이 이미 존재하면, overlap 매개변수에 따라 업데이트 여부를 결정합니다.
        /// </summary>
        /// <param name="chip">추가할 Chip (값과 컨트롤러 쌍)</param>
        /// <param name="overlap">
        /// 동일한 Rank가 존재할 경우 true이면 덮어쓰고, false이면 변경하지 않습니다.
        /// </param>
        /// <returns>Chip 추가 또는 업데이트에 성공하면 true, 실패하면 false</returns>
        protected bool AddChip(ValueChips chip, bool overlap)
        {
            int chipFoundedIndex = -1;

            for (int i = 0; i < Chips.Count; i++)
            {
                if (Chips[i].KeyRank == chip.KeyRank) { chipFoundedIndex = i; break; }
            }

            if (chipFoundedIndex != -1)
            {
                if (overlap)
                {
                    Chips[chipFoundedIndex] = chip;
                    RefreshValue();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                Chips.Add(chip);
                RefreshValue();
                return true;
            }
        }



        /// <summary>
        /// 지정된 Rank와 컨트롤러에 해당하는 Chip을 삭제합니다. <br/>
        /// 컨트롤러가 일치하는 경우에만 삭제합니다.
        /// </summary>
        /// <param name="controller">삭제를 요청한 컨트롤러</param>
        /// <param name="keyRank">삭제할 Chip의 Rank 키</param>
        /// <returns>삭제에 성공하면 true, 그렇지 않으면 false</returns>
        protected bool RemoveChip(TController controller, int keyRank)
        {
            int chipFoundedIndex = -1;

            for (int i = 0; i < Chips.Count; i++)
            {
                if (Chips[i].Controller == controller) { chipFoundedIndex = i; break; }
            }

            if (chipFoundedIndex != -1)
            {
                Chips.RemoveAt(chipFoundedIndex);
                RefreshValue();
                if (Chips.Count == 0) { EmptyValueEvent?.Invoke(); } //. 칩이 비었으니 Empty 이벤트 실행
                return true;
            }
            return false;
        }



        /// <summary>
        /// 지정된 Rank의 Chip을 삭제합니다.
        /// </summary>
        /// <param name="keyRank">삭제할 Chip의 Rank 키</param>
        /// <returns>삭제에 성공하면 true, 그렇지 않으면 false</returns>
        protected bool RemoveChip(int keyRank)
        {
            int chipFoundedIndex = -1;

            for (int i = 0; i < Chips.Count; i++)
            {
                if (Chips[i].KeyRank == keyRank) { chipFoundedIndex = i; break; }
            }

            if (chipFoundedIndex != -1)
            {
                Chips.RemoveAt(chipFoundedIndex);
                RefreshValue();
                if (Chips.Count == 0) { EmptyValueEvent?.Invoke(); } //. 칩이 비었으니 Empty 이벤트 실행
                return true;
            }
            return false;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 컨트롤러와 Rank를 기반으로 새 Chip을 추가하여 상태 값을 변경합니다.
        /// </summary>
        /// <param name="controller">Chip을 추가할 컨트롤러 (없으면 null)</param>
        /// <param name="keyRank">Chip의 Rank 값</param>
        /// <param name="value">Chip에 저장할 값</param>
        /// <param name="overlap">
        /// 동일 Rank가 존재할 경우 true이면 업데이트, false이면 변경하지 않습니다.
        /// </param>
        /// <returns>Chip 추가 및 상태 변경에 성공하면 true, 그렇지 않으면 false</returns>
        public bool SetValue(TController controller, int keyRank, TValue value, bool overlap = false)
        {
            return AddChip(new ValueChips(keyRank, value, controller), overlap);
        }



        /// <summary>
        /// Rank와 값을 기반으로 Chip을 추가하여 상태 값을 변경합니다. <br/>
        /// 컨트롤러가 지정되지 않은 경우 호출됩니다.
        /// </summary>
        /// <param name="keyRank">Chip의 Rank 값</param>
        /// <param name="value">Chip에 저장할 값</param>
        /// <param name="overlap">
        /// 동일 Rank가 존재할 경우 true이면 업데이트, false이면 변경하지 않습니다.
        /// </param>
        /// <returns>Chip 추가 및 상태 변경에 성공하면 true, 그렇지 않으면 false</returns>
        public bool SetValue(int keyRank, TValue value, bool overlap = false)
        {
            return SetValue(null, keyRank, value, overlap);
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 지정된 컨트롤러와 Rank의 Chip을 삭제하고, 상태 값을 갱신합니다.
        /// </summary>
        /// <param name="controller">삭제 요청한 컨트롤러</param>
        /// <param name="keyRank">삭제할 Chip의 Rank 값</param>
        /// <returns>삭제에 성공하면 true, 그렇지 않으면 false</returns>
        public bool RemoveValue(TController controller, int keyRank)
        {
            return RemoveChip(controller, keyRank);
        }



        /// <summary>
        /// Rank를 기준으로 Chip을 삭제하고, 상태 값을 갱신합니다.
        /// </summary>
        /// <param name="keyRank">삭제할 Chip의 Rank 값</param>
        /// <returns>삭제에 성공하면 true, 그렇지 않으면 false</returns>
        public bool RemoveValue(int keyRank)
        {
            return RemoveChip(keyRank);
        }



        ///======================================================================================================================================================



        /// <summary>
        /// Chip이 존재하면, 가장 높은 Rank 값을 가진 Chip의 값을 현재 상태 값으로 갱신합니다. <br/>
        /// Chip이 없으면 기본값(BasicValue)로 상태 값을 초기화하며, ChangeValueEvent를 발생시킵니다.
        /// </summary>
        protected override void RefreshValue()
        {
            if (Chips.Count == 0)
            {
                ChangeValueEvent?.Invoke(Value, BasicValue);
                Value = null;
                return;
            }

            //. 등록된 Chip 중 가장 높은 Rank의 값을 현재 값으로 적용
            int highestChipIndex = 0;
            int highestChipKeyRank = int.MinValue;
            for (int i = 0; i < Chips.Count; i++)
            {
                if (highestChipKeyRank < Chips[i].KeyRank) { highestChipIndex = i; }
            }
            Value = Chips[highestChipIndex].Value;
        }



        /// <summary>
        /// 상태 값을 재설정합니다. <br/>
        /// 내부 상태(Value)를 null로 초기화하고, 기본값을 재설정한 후 Chip 딕셔너리를 모두 삭제합니다.
        /// </summary>
        /// <param name="resetBasicValue">새로운 기본값</param>
        public override void Reset(TValue resetBasicValue)
        {
            base.Reset(resetBasicValue);
            Chips.Clear();
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// ValueControlManager의 컨트롤러 타입을 object로 고정한 특수화 버전입니다.
    /// </summary>
    /// <typeparam name="TValue">관리할 값의 형식 (구조체)</typeparam>
    [Serializable]
    public class ValueControlManager<TValue> : ValueControlManager<TValue, object> where TValue : struct
    {
        public ValueControlManager(Action<TValue?, TValue> changeValueEvent, TValue? firstBasic = null, Func<TValue, TValue, TValue> customSetterEvent = null, Action emptyValueEvent = null)
            : base(changeValueEvent, firstBasic, customSetterEvent, emptyValueEvent) { }
    }



    /// <summary>
    /// 단일 컨트롤러와 단일 값을 관리하는 클래스입니다. <br/>
    /// 기본 값(BasicValue)은 Func를 통해 제공되며, 값 변경 시 CustomSetterEvent를 통해 처리할 수 있습니다.
    /// </summary>
    /// <typeparam name="TValue">관리할 값의 형식 (구조체)</typeparam>
    /// <typeparam name="TController">컨트롤러의 형식 (참조형)</typeparam>
    [Serializable]
    public class ValueControlOneManager<TValue, TController> where TValue : struct where TController : class
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 생성자입니다. <br/>
        /// 값 변경 이벤트(ChangeValueEvent), 기본 값 제공 함수(BasicValueEvent), 커스텀 세터(CustomSetterEvent)를 설정합니다.
        /// </summary>
        /// <param name="changeValueEvent">
        /// 값이 변경될 때 호출되는 이벤트 핸들러. <br/>
        /// 인자 1: 이전 값 (nullable), 인자 2: 새 값.
        /// </param>
        /// <param name="basicValueEvent">
        /// 기본 값을 제공하는 함수. <br/>
        /// 값이 null일 경우 이 함수의 반환값을 사용합니다.
        /// </param>
        /// <param name="customSetterEvent">
        /// 값 할당 시 커스텀 처리를 위한 델리게이트. <br/>
        /// 인자 1: 기존 값, 인자 2: 새 값을 받아 처리 결과를 반환합니다.
        /// </param>
        public ValueControlOneManager(Action<TValue?, TValue> changeValueEvent, Func<TValue> basicValueEvent, Func<TValue, TValue, TValue> customSetterEvent = null, Action emptyValueEvent = null)
        {
            ChangeValueEvent = changeValueEvent;
            BasicValueEvent = basicValueEvent;
            CustomSetterEvent = customSetterEvent;
            EmptyValueEvent = emptyValueEvent;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 현재 관리되는 값입니다. <br/>
        /// 내부적으로 CustomSetterEvent를 통해 값이 갱신될 수 있으며, 값이 변경되면 ChangeValueEvent가 호출됩니다.
        /// </summary>
        public TValue? Value
        {
            get => value;
            private set
            {
                TValue? applyValue;
                // 기존 값과 새 값이 모두 null이 아니고, 커스텀 세터가 존재하면 그 결과를 사용
                if (this.value is not null && value is not null && CustomSetterEvent is not null)
                {
                    applyValue = CustomSetterEvent.Invoke((TValue)this.value, (TValue)value);
                }
                else
                {
                    applyValue = value;
                }

                // 새 값이 null이 아니면 ChangeValueEvent 호출
                if (applyValue is not null)
                {
                    ChangeValueEvent?.Invoke(this.value, (TValue)applyValue);
                }

                this.value = applyValue;
            }
        }
        [SerializeField] private TValue? value;



        /// <summary>
        /// 현재 값을 반환합니다. <br/>
        /// 값이 null이면 BasicValueEvent 함수를 호출하여 기본 값을 반환합니다.
        /// </summary>
        /// <returns>현재 값 또는 기본 값</returns>
        public TValue GetValue()
        {
            return Value ?? BasicValueEvent.Invoke();
        }



        /// <summary>
        /// 현재 값이 존재하면 그 값을 out 매개변수에 할당하고 true를 반환합니다. <br/>
        /// 값이 null이면 기본 값을 할당하고 false를 반환합니다.
        /// </summary>
        /// <param name="resultValue">현재 값 또는 기본 값이 할당됩니다.</param>
        /// <returns>값이 존재하면 true, 그렇지 않으면 false</returns>
        public bool TryGetValue(TValue resultValue)
        {
            if (Value.HasValue)
            {
                resultValue = (TValue)Value;
                return true;
            }
            else
            {
                resultValue = BasicValueEvent.Invoke();
                return false;
            }
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 현재 컨트롤러를 나타냅니다.
        /// </summary>
        public TController Controller
        {
            get => controller;
            private set
            {
                controller = value;

                if (controller == null)
                {
                    EmptyValueEvent?.Invoke(); //. 컨트롤러가 비었으니 Empty 이벤트 실행
                }
            }
        }
        [SerializeField] private TController controller;



        ///======================================================================================================================================================



        /// <summary>
        /// 현재 컨트롤러의 우선순위를 나타내는 Rank 값입니다.
        /// </summary>
        [field: SerializeField] public int KeyRank { get; private set; } = 0;



        ///======================================================================================================================================================



        private readonly Action<TValue?, TValue> ChangeValueEvent;
        private readonly Func<TValue> BasicValueEvent;
        private readonly Func<TValue, TValue, TValue> CustomSetterEvent;
        private readonly Action EmptyValueEvent;



        ///======================================================================================================================================================



        /// <summary>
        /// 컨트롤러를 설정합니다. <br/>
        /// 기존 KeyRank보다 낮은 Rank를 가진 컨트롤러는 설정할 수 없습니다.
        /// </summary>
        /// <param name="controller">설정할 컨트롤러</param>
        /// <param name="keyRank">컨트롤러의 우선순위 Rank</param>
        /// <returns>설정 성공 시 true, 실패 시 false</returns>
        public bool SetController(TController controller, int keyRank = 0)
        {
            // 현재 KeyRank보다 낮은 경우에는 설정 실패
            if (KeyRank > keyRank) { return false; }

            Controller = controller;
            return true;
        }



        /// <summary>
        /// 지정된 컨트롤러와 일치할 경우 컨트롤러를 제거합니다.
        /// </summary>
        /// <param name="controller">제거할 컨트롤러</param>
        /// <returns>제거에 성공하면 true, 그렇지 않으면 false</returns>
        public bool RemoveController(TController controller)
        {
            if (Controller != controller) { return false; }

            Controller = null;

            return true;
        }



        /// <summary>
        /// 컨트롤러를 무조건 제거합니다.
        /// </summary>
        public void AbsoluteRemoveController()
        {
            Controller = null;
        }



        /// <summary>
        /// 컨트롤러를 무조건 설정합니다.
        /// </summary>
        /// <param name="controller">설정할 컨트롤러</param>
        public void AbsoluteSetController(TController controller)
        {
            Controller = controller;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 지정된 컨트롤러와 일치할 경우 현재 값을 새 값으로 변경합니다.
        /// </summary>
        /// <param name="controller">값 변경을 요청하는 컨트롤러</param>
        /// <param name="value">설정할 새 값</param>
        /// <returns>변경 성공 시 true, 그렇지 않으면 false</returns>
        public bool TrySetValue(TController controller, TValue value)
        {
            if (Controller != controller) { return false; }

            Value = value;
            return true;
        }



        /// <summary>
        /// 지정된 컨트롤러와 Rank를 기준으로 컨트롤러를 설정한 후 값을 변경합니다.
        /// </summary>
        /// <param name="controller">값 변경을 요청하는 컨트롤러</param>
        /// <param name="value">설정할 새 값</param>
        /// <param name="keyRank">컨트롤러의 우선순위 Rank</param>
        /// <returns>변경 성공 시 true, 그렇지 않으면 false</returns>
        public bool TrySetControllerValue(TController controller, TValue value, int keyRank = 0)
        {
            if (!SetController(controller, keyRank)) { return false; }

            Value = value;
            return true;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 모든 설정을 초기화하여 컨트롤러와 Rank, 값(Value)을 제거합니다.
        /// </summary>
        public void Reset()
        {
            KeyRank = 0;
            Controller = null;
            Value = null;
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// ValueControlOneManager의 컨트롤러 타입을 object로 고정한 특수화 버전입니다.
    /// </summary>
    /// <typeparam name="TValue">관리할 값의 형식 (구조체)</typeparam>
    [Serializable]
    public class ValueControlOneManager<TValue> : ValueControlOneManager<TValue, object> where TValue : struct
    {
        public ValueControlOneManager(Action<TValue?, TValue> changeValueEvent, Func<TValue> basicValueEvent, Func<TValue, TValue, TValue> customSetterEvent = null, Action emptyValueEvent = null)
            : base(changeValueEvent, basicValueEvent, customSetterEvent, emptyValueEvent) { }
    }



    ///======================================================================================================================================================



    namespace BackUp
    {
        public abstract class BaseValueControlManager<TValue, TController> where TValue : struct where TController : class
        {
            ///======================================================================================================================================================



            /// <param name="changeValueEvent">( item 1 : 기존값 (oldV) item 2 : 새로운 값 (newV) </param>
            /// <param name="firstBasic">맨 처음 넣을 Basic 값 (없으면 null)</param>
            /// <param name="customSetterEvent">(item 1 : 기존값 (oldV) item 2 : 새로운 값 (newV) 값을 할당할때, 따로 커스텀해서 넣고싶을때</param>
            public BaseValueControlManager(Action<TValue?, TValue> changeValueEvent, TValue? firstBasic = null, Func<TValue, TValue, TValue> customSetterEvent = null)
            {
                ChangeValueEvent = changeValueEvent;

                if (firstBasic is not null)
                {
                    BasicValue = (TValue)firstBasic;
                }

                CustomSetterEvent = customSetterEvent;
            }



            ///======================================================================================================================================================



            public struct Chip
            {
                public Chip(TValue value, TController updater)
                {
                    Value = value;
                    Updater = updater;
                }

                public TValue Value;
                public TController Updater;
            }



            ///======================================================================================================================================================



            public TValue BasicValue { protected get; set; }



            protected TValue? Value
            {
                get => value;
                set
                {
                    TValue? applyValue;

                    //? this.value, value, CustomSetterEvent가 null이 아니면 CustomSetterEvent로 값을 넣는다
                    if (this.value is not null && value is not null && CustomSetterEvent is not null)
                    {
                        //this.value = CustomSetterEvent.Invoke((TValue)this.value, (TValue)value);
                        applyValue = CustomSetterEvent.Invoke((TValue)this.value, (TValue)value);
                    }

                    //? 그냥 값을 넣는다
                    else
                    {
                        //this.value = value;
                        applyValue = value;
                    }



                    //? null이 아닌 값이 적용됐으면, ChangeValueVent 실행
                    if (applyValue is not null)
                    {
                        //Debug.Log("Basic이 아닌 받아온 Value가 할당이 됐을때 발동되는 ChangeValueEvent");
                        ChangeValueEvent?.Invoke(this.value, (TValue)applyValue);
                    }



                    this.value = applyValue;
                }
            }
            private TValue? value;



            public TValue GetValue()
            {
                //Value가 null이 아니면 Value, null이면 BasicValue 리턴
                return Value ?? BasicValue;
            }



            /// <summary>Value값이 바뀔때마다 발동되는 이벤트</summary>
            public readonly Action<TValue?, TValue> ChangeValueEvent;



            public readonly Func<TValue, TValue, TValue> CustomSetterEvent;



            ///======================================================================================================================================================



            protected abstract void RefreshValue();



            public virtual void Reset(TValue resetBasicValue)
            {
                Value = null;
                BasicValue = resetBasicValue;
                ChangeValueEvent?.Invoke(this.value, BasicValue);
            }



            ///======================================================================================================================================================
        }



        public class ValueControlManager<TValue, TController> : BaseValueControlManager<TValue, TController> where TValue : struct where TController : class
        {
            ///======================================================================================================================================================



            /// <param name="changeValueEvent">( item 1 : 기존값 (oldV) item 2 : 새로운 값 (newV) </param>
            /// <param name="firstBasic">맨 처음 넣을 Basic 값 (없으면 null)</param>
            /// <param name="customSetterEvent">(item 1 : 기존값 (oldV) item 2 : 새로운 값 (newV) 값을 할당할때, 따로 커스텀해서 넣고싶을때</param>
            public ValueControlManager(Action<TValue?, TValue> changeValueEvent, TValue? firstBasic = null, Func<TValue, TValue, TValue> customSetterEvent = null) : base(changeValueEvent, firstBasic, customSetterEvent)
            {

            }



            ///======================================================================================================================================================




            private readonly Dictionary<int, Chip> Chips = new Dictionary<int, Chip>();



            public int GetHighstChip()
            {
                if (Chips.Count == 0) { return 0; }
                return Chips.Keys.Max();
            }



            ///======================================================================================================================================================



            public bool SetValue(TController controller, int keyRank, TValue value, bool overlap = false)
            {
                return AddChip(keyRank, new Chip(value, controller), overlap);
            }



            public bool SetValue(int keyRank, TValue value, bool overlap = false)
            {
                return SetValue(null, keyRank, value, overlap);
            }



            public bool RemoveValue(TController controller, int keyRank)
            {
                return RemoveChip(controller, keyRank);
            }



            public bool RemoveValue(int keyRank)
            {
                return RemoveChip(keyRank);
            }



            ///======================================================================================================================================================



            protected bool AddChip(int keyRank, Chip chip, bool overlap)
            {
                if (Chips.ContainsKey(keyRank))
                {
                    if (overlap)
                    {
                        Chips[keyRank] = chip;
                        RefreshValue();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    Chips.Add(keyRank, chip);
                    RefreshValue();
                    return true;
                }

                return false;
            }



            protected bool RemoveChip(TController controller, int keyRank)
            {
                if (Chips.TryGetValue(keyRank, out var resultChip) && resultChip.Updater == controller)
                {
                    Chips.Remove(keyRank);
                    RefreshValue();
                    return true;
                }

                return false;
            }



            protected bool RemoveChip(int keyRank)
            {
                if (Chips.TryGetValue(keyRank, out var resultChip))
                {
                    Chips.Remove(keyRank);
                    RefreshValue();
                    return true;
                }

                return false;
            }



            ///======================================================================================================================================================



            protected override void RefreshValue()
            {
                if (Chips.Count == 0)
                {
                    //Debug.Log("칩이 모두 비워저 BasicValue로 ChangeValueEvent 발동");
                    ChangeValueEvent?.Invoke(Value, BasicValue);
                    Value = null;
                    return;
                }

                //? Chips가 존재하면, Chips중 가장 높은 키의 값이 Value가 된다

                Value = Chips[GetHighstChip()].Value;
            }



            public override void Reset(TValue resetBasicValue)
            {
                base.Reset(resetBasicValue);
                Chips.Clear();
            }



            ///======================================================================================================================================================
        }



        public class ValueControlManager<TValue> : ValueControlManager<TValue, object> where TValue : struct
        {
            public ValueControlManager(Action<TValue?, TValue> changeValueEvent, TValue? firstBasic = null, Func<TValue, TValue, TValue> customSetterEvent = null)
                : base(changeValueEvent, firstBasic, customSetterEvent) { }
        }



        public class ValueControlOneManager<TValue, TController> where TValue : struct where TController : class
        {

            ///======================================================================================================================================================



            public ValueControlOneManager(Action<TValue?, TValue> changeValueEvent, Func<TValue> basicValueEvent, Func<TValue, TValue, TValue> customSetterEvent = null)
            {
                ChangeValueEvent = changeValueEvent;


                BasicValueEvent = basicValueEvent;


                CustomSetterEvent = customSetterEvent;
            }



            ///======================================================================================================================================================



            public TValue? Value
            {
                get => value;
                private set
                {
                    TValue? applyValue;

                    //? this.value, value, CustomSetterEvent가 null이 아니면 CustomSetterEvent로 값을 넣는다
                    if (this.value is not null && value is not null && CustomSetterEvent is not null)
                    {
                        applyValue = CustomSetterEvent.Invoke((TValue)this.value, (TValue)value);
                    }

                    //? 그냥 값을 넣는다
                    else
                    {
                        applyValue = value;
                    }



                    //? null이 아닌 값이 적용됐으면, ChangeValueVent 실행
                    if (applyValue is not null)
                    {
                        ChangeValueEvent?.Invoke(this.value, (TValue)applyValue);
                    }

                    this.value = applyValue;
                }
            }
            private TValue? value;



            public TValue GetValue()
            {
                return Value ?? BasicValueEvent.Invoke();
            }




            public bool TryGetValue(TValue resultValue)
            {
                if (Value.HasValue)
                {
                    resultValue = (TValue)Value;
                    return true;
                }
                else
                {
                    resultValue = BasicValueEvent.Invoke();
                    return false;
                }
            }



            public TController Controller { get; private set; }



            public int KeyRank { get; private set; } = 0;



            private readonly Action<TValue?, TValue> ChangeValueEvent;
            private readonly Func<TValue> BasicValueEvent;
            private readonly Func<TValue, TValue, TValue> CustomSetterEvent;



            ///======================================================================================================================================================



            public bool SetController(TController controller, int keyRank = 0)
            {
                //? 받아온 keyRank가 낮으면 실패
                if (KeyRank > keyRank) { return false; }

                Controller = controller;
                return true;
            }



            public bool RemoveController(TController controller)
            {
                if (Controller != controller) { return false; }

                Controller = null;
                return true;
            }



            public void AbsoluteRemoveController()
            {
                Controller = null;
            }



            public void AbsoluteSetController(TController controller)
            {
                Controller = controller;
            }



            ///======================================================================================================================================================



            public bool TrySetValue(TController controller, TValue value)
            {
                if (Controller != controller) { return false; }

                Value = value;

                return true;
            }



            public bool TrySetControllerValue(TController controller, TValue value, int keyRank = 0)
            {
                if (!SetController(controller, keyRank)) { return false; }

                Value = value;

                return true;
            }



            public void Reset()
            {
                KeyRank = 0;
                Controller = null;
                Value = null;
            }



            ///======================================================================================================================================================
        }



        public class ValueControlOneManager<TValue> : ValueControlOneManager<TValue, object> where TValue : struct
        {
            public ValueControlOneManager(Action<TValue?, TValue> changeValueEvent, Func<TValue> basicValueEvent, Func<TValue, TValue, TValue> customSetterEvent = null)
                : base(changeValueEvent, basicValueEvent, customSetterEvent) { }
        }
    }



    namespace backUp250721
    {
        /// <summary>
        /// TValue 형식의 값과 해당 값 변경 시 적용된 Rank를 관리하는 기본 추상 클래스입니다. <br/>
        /// 이 클래스는 상태 변경 이벤트 및 커스텀 값 할당 기능을 제공하며, <br/>
        /// 자식 클래스에서 RefreshValue() 메서드를 구현하여 최종 값(Value)을 갱신할 수 있습니다.
        /// </summary>
        /// <typeparam name="TValue">관리할 값의 형식 (값 형식이어야 합니다)</typeparam>
        /// <typeparam name="TController">값 변경을 주도하는 컨트롤러의 형식 (참조 형식이어야 합니다)</typeparam>
        [Serializable]
        public abstract class BaseValueControlManager<TValue, TController>
            where TValue : struct
            where TController : class
        {
            ///======================================================================================================================================================



            /// <summary>
            /// 기본 생성자입니다. <br/>
            /// 초기 ChangeValueEvent, 기본값(BasicValue), 커스텀 세터(CustomSetterEvent)를 설정합니다.
            /// </summary>
            /// <param name="changeValueEvent">
            /// 상태가 변경될 때 호출되는 이벤트 핸들러입니다. <br/>
            /// 인자 1: 기존 값 (nullable), 인자 2: 새 값
            /// </param>
            /// <param name="firstBasic">초기 기본값. null이면 기본값이 설정되지 않습니다.</param>
            /// <param name="customSetterEvent">
            /// 값 할당 시 커스텀 처리를 하고 싶을 때 사용하는 델리게이트입니다. <br/>
            /// 인자 1: 기존 값, 인자 2: 새 값을 받아 처리한 결과 값을 반환합니다.
            /// </param>
            public BaseValueControlManager(Action<TValue?, TValue> changeValueEvent, TValue? firstBasic = null, Func<TValue, TValue, TValue> customSetterEvent = null, Action emptyValueEvent = null)
            {
                ChangeValueEvent = changeValueEvent;

                if (firstBasic is not null)
                {
                    BasicValue = (TValue)firstBasic;
                }

                CustomSetterEvent = customSetterEvent;

                EmptyValueEvent = emptyValueEvent;
            }



            ///======================================================================================================================================================



            /// <summary>
            /// 상태를 나타내는 내부 구조체입니다. <br/>
            /// 각 Chip은 하나의 값과 해당 값을 갱신한 컨트롤러(Updater)를 보유합니다.
            /// </summary>
            public readonly struct Chip
            {
                /// <summary>
                /// Chip 생성자입니다.
                /// </summary>
                /// <param name="value">Chip에 저장할 값</param>
                /// <param name="updater">해당 값을 갱신한 컨트롤러</param>
                public Chip(TValue value, TController updater)
                {
                    Value = value;
                    Updater = updater;
                }

                /// <summary>
                /// Chip에 저장된 값입니다.
                /// </summary>
                public readonly TValue Value;
                /// <summary>
                /// 값을 갱신한 컨트롤러입니다.
                /// </summary>
                public readonly TController Updater;
            }



            ///======================================================================================================================================================



            /// <summary>
            /// 기본값(BasicValue)입니다. <br/>
            /// 이 값은 Value가 null인 경우에도 반환됩니다.
            /// </summary>
            [field: SerializeField] public TValue BasicValue { protected get; set; }



            /// <summary>
            /// 현재 상태 값입니다. <br/>
            /// 값을 설정하면 이전에 적용된 Rank (LastChangedRank)는 초기화되며, <br/>
            /// CustomSetterEvent가 설정되어 있다면 그 결과가 적용됩니다.
            /// </summary>
            protected TValue? Value
            {
                get => value;
                set
                {
                    TValue? applyValue;

                    // 기존 값, 새 값, 그리고 커스텀 세터가 모두 존재하면 커스텀 세터를 통해 값을 결정함
                    if (this.value is not null && value is not null && CustomSetterEvent is not null)
                    {
                        applyValue = CustomSetterEvent.Invoke((TValue)this.value, (TValue)value);
                    }
                    else
                    {
                        applyValue = value;
                    }

                    // 새로 적용된 값이 null이 아니라면 ChangeValueEvent를 호출하여 값 변경을 알림
                    if (applyValue is not null)
                    {
                        ChangeValueEvent?.Invoke(this.value, (TValue)applyValue);
                    }

                    this.value = applyValue;
                }
            }
            [SerializeField] private TValue? value;



            /// <summary>
            /// 현재 상태 값이 null일 경우 BasicValue를 반환하고, 그렇지 않으면 Value를 반환합니다.
            /// </summary>
            /// <returns>현재 유효한 값</returns>
            public TValue GetValue()
            {
                return Value ?? BasicValue;
            }



            ///======================================================================================================================================================



            /// <summary>
            /// 상태 값이 변경될 때마다 호출되는 이벤트입니다. <br/>
            /// 인자 1: 이전 값, 인자 2: 새 값
            /// </summary>
            public readonly Action<TValue?, TValue> ChangeValueEvent;



            /// <summary>
            /// 값 할당 시 커스텀 처리를 위한 델리게이트입니다. <br/>
            /// 인자 1: 기존 값, 인자 2: 새 값을 받아 처리 결과 값을 반환합니다.
            /// </summary>
            public readonly Func<TValue, TValue, TValue> CustomSetterEvent;



            /// <summary>
            /// 값을 제거하여, 값이 전혀 존재하지 않을때 실행되는 이벤트
            /// </summary>
            public readonly Action EmptyValueEvent;



            ///======================================================================================================================================================



            /// <summary>
            /// 자식 클래스에서 구현해야 하는 추상 메서드입니다. <br/>
            /// 이 메서드를 호출하여 내부 값(Value)을 최신 상태로 갱신합니다.
            /// </summary>
            protected abstract void RefreshValue();



            /// <summary>
            /// 상태를 재설정합니다. <br/>
            /// 내부 값(Value)를 null로 초기화하고, BasicValue를 새 값으로 설정한 후 ChangeValueEvent를 호출합니다.
            /// </summary>
            /// <param name="resetBasicValue">재설정할 기본값</param>
            public virtual void Reset(TValue resetBasicValue)
            {
                Value = null;
                BasicValue = resetBasicValue;
                ChangeValueEvent?.Invoke(this.value, BasicValue);
            }



            ///======================================================================================================================================================
        }



        /// <summary>
        /// BaseValueControlManager를 상속받아, 여러 개의 Chip(값과 컨트롤러 쌍)을 관리하며, <br/>
        /// 가장 높은 Rank를 가진 Chip의 값을 현재 값으로 사용하는 클래스입니다.
        /// </summary>
        /// <typeparam name="TValue">관리할 값의 형식 (구조체)</typeparam>
        /// <typeparam name="TController">값 변경을 주도하는 컨트롤러의 형식 (참조형)</typeparam>
        [Serializable]
        public class ValueControlManager<TValue, TController> : BaseValueControlManager<TValue, TController>
            where TValue : struct
            where TController : class
        {

            ///======================================================================================================================================================



            /// <summary>
            /// 생성자입니다. <br/>
            /// BaseValueControlManager의 생성자를 호출하여 초기 이벤트, 기본값, 커스텀 세터를 설정합니다.
            /// </summary>
            /// <param name="changeValueEvent">값 변경 시 호출되는 이벤트 핸들러</param>
            /// <param name="firstBasic">초기 기본값 (없으면 null)</param>
            /// <param name="customSetterEvent">값 할당 시 커스텀 처리할 델리게이트 (선택 사항)</param>
            public ValueControlManager(Action<TValue?, TValue> changeValueEvent, TValue? firstBasic = null, Func<TValue, TValue, TValue> customSetterEvent = null, Action emptyValueEvent = null)
                : base(changeValueEvent, firstBasic, customSetterEvent, emptyValueEvent) { }



            ///======================================================================================================================================================



            /// <summary>
            /// Chip들을 Rank(정수 키)를 기준으로 관리하는 딕셔너리입니다.
            /// </summary>
            private readonly Dictionary<int, Chip> Chips = new Dictionary<int, Chip>();



            ///======================================================================================================================================================



            /// <summary>
            /// 현재 등록된 Chip들 중 가장 높은 Rank 값을 반환합니다. <br/>
            /// Chip이 하나도 없으면 0을 반환합니다.
            /// </summary>
            /// <returns>가장 높은 Rank 값</returns>
            public int GetHighstChip()
            {
                if (Chips.Count == 0)
                {
                    return 0;
                }
                return Chips.Keys.Max();
            }



            /// <summary>
            /// 지정된 Rank와 컨트롤러에 해당하는 Chip을 추가하거나 업데이트합니다. <br/>
            /// 만약 같은 Rank가 이미 존재하면, overlap 매개변수에 따라 업데이트 여부를 결정합니다.
            /// </summary>
            /// <param name="keyRank">Chip의 우선순위를 나타내는 정수 키</param>
            /// <param name="chip">추가할 Chip (값과 컨트롤러 쌍)</param>
            /// <param name="overlap">
            /// 동일한 Rank가 존재할 경우 true이면 덮어쓰고, false이면 변경하지 않습니다.
            /// </param>
            /// <returns>Chip 추가 또는 업데이트에 성공하면 true, 실패하면 false</returns>
            protected bool AddChip(int keyRank, Chip chip, bool overlap)
            {
                if (Chips.ContainsKey(keyRank))
                {
                    if (overlap)
                    {
                        Chips[keyRank] = chip;
                        RefreshValue();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    Chips.Add(keyRank, chip);
                    RefreshValue();
                    return true;
                }
            }



            /// <summary>
            /// 지정된 Rank와 컨트롤러에 해당하는 Chip을 삭제합니다. <br/>
            /// 컨트롤러가 일치하는 경우에만 삭제합니다.
            /// </summary>
            /// <param name="controller">삭제를 요청한 컨트롤러</param>
            /// <param name="keyRank">삭제할 Chip의 Rank 키</param>
            /// <returns>삭제에 성공하면 true, 그렇지 않으면 false</returns>
            protected bool RemoveChip(TController controller, int keyRank)
            {
                if (Chips.TryGetValue(keyRank, out var resultChip) && resultChip.Updater == controller)
                {
                    Chips.Remove(keyRank);
                    RefreshValue();
                    if (Chips.Count == 0) { EmptyValueEvent?.Invoke(); } //. 칩이 비었으니 Empty 이벤트 실행
                    return true;
                }
                return false;
            }



            /// <summary>
            /// 지정된 Rank의 Chip을 삭제합니다.
            /// </summary>
            /// <param name="keyRank">삭제할 Chip의 Rank 키</param>
            /// <returns>삭제에 성공하면 true, 그렇지 않으면 false</returns>
            protected bool RemoveChip(int keyRank)
            {
                if (Chips.TryGetValue(keyRank, out var resultChip))
                {
                    Chips.Remove(keyRank);
                    RefreshValue();
                    if (Chips.Count == 0) { EmptyValueEvent?.Invoke(); } //. 칩이 비었으니 Empty 이벤트 실행
                    return true;
                }
                return false;
            }



            ///======================================================================================================================================================



            /// <summary>
            /// 컨트롤러와 Rank를 기반으로 새 Chip을 추가하여 상태 값을 변경합니다.
            /// </summary>
            /// <param name="controller">Chip을 추가할 컨트롤러 (없으면 null)</param>
            /// <param name="keyRank">Chip의 Rank 값</param>
            /// <param name="value">Chip에 저장할 값</param>
            /// <param name="overlap">
            /// 동일 Rank가 존재할 경우 true이면 업데이트, false이면 변경하지 않습니다.
            /// </param>
            /// <returns>Chip 추가 및 상태 변경에 성공하면 true, 그렇지 않으면 false</returns>
            public bool SetValue(TController controller, int keyRank, TValue value, bool overlap = false)
            {
                return AddChip(keyRank, new Chip(value, controller), overlap);
            }



            /// <summary>
            /// Rank와 값을 기반으로 Chip을 추가하여 상태 값을 변경합니다. <br/>
            /// 컨트롤러가 지정되지 않은 경우 호출됩니다.
            /// </summary>
            /// <param name="keyRank">Chip의 Rank 값</param>
            /// <param name="value">Chip에 저장할 값</param>
            /// <param name="overlap">
            /// 동일 Rank가 존재할 경우 true이면 업데이트, false이면 변경하지 않습니다.
            /// </param>
            /// <returns>Chip 추가 및 상태 변경에 성공하면 true, 그렇지 않으면 false</returns>
            public bool SetValue(int keyRank, TValue value, bool overlap = false)
            {
                return SetValue(null, keyRank, value, overlap);
            }



            ///======================================================================================================================================================



            /// <summary>
            /// 지정된 컨트롤러와 Rank의 Chip을 삭제하고, 상태 값을 갱신합니다.
            /// </summary>
            /// <param name="controller">삭제 요청한 컨트롤러</param>
            /// <param name="keyRank">삭제할 Chip의 Rank 값</param>
            /// <returns>삭제에 성공하면 true, 그렇지 않으면 false</returns>
            public bool RemoveValue(TController controller, int keyRank)
            {
                return RemoveChip(controller, keyRank);
            }



            /// <summary>
            /// Rank를 기준으로 Chip을 삭제하고, 상태 값을 갱신합니다.
            /// </summary>
            /// <param name="keyRank">삭제할 Chip의 Rank 값</param>
            /// <returns>삭제에 성공하면 true, 그렇지 않으면 false</returns>
            public bool RemoveValue(int keyRank)
            {
                return RemoveChip(keyRank);
            }



            ///======================================================================================================================================================



            /// <summary>
            /// Chip이 존재하면, 가장 높은 Rank 값을 가진 Chip의 값을 현재 상태 값으로 갱신합니다. <br/>
            /// Chip이 없으면 기본값(BasicValue)로 상태 값을 초기화하며, ChangeValueEvent를 발생시킵니다.
            /// </summary>
            protected override void RefreshValue()
            {
                if (Chips.Count == 0)
                {
                    ChangeValueEvent?.Invoke(Value, BasicValue);
                    Value = null;
                    return;
                }

                // 등록된 Chip 중 가장 높은 Rank의 값을 현재 값으로 적용
                Value = Chips[GetHighstChip()].Value;
            }



            /// <summary>
            /// 상태 값을 재설정합니다. <br/>
            /// 내부 상태(Value)를 null로 초기화하고, 기본값을 재설정한 후 Chip 딕셔너리를 모두 삭제합니다.
            /// </summary>
            /// <param name="resetBasicValue">새로운 기본값</param>
            public override void Reset(TValue resetBasicValue)
            {
                base.Reset(resetBasicValue);
                Chips.Clear();
            }



            ///======================================================================================================================================================
        }



        /// <summary>
        /// ValueControlManager의 컨트롤러 타입을 object로 고정한 특수화 버전입니다.
        /// </summary>
        /// <typeparam name="TValue">관리할 값의 형식 (구조체)</typeparam>
        [Serializable]
        public class ValueControlManager<TValue> : ValueControlManager<TValue, object> where TValue : struct
        {
            public ValueControlManager(Action<TValue?, TValue> changeValueEvent, TValue? firstBasic = null, Func<TValue, TValue, TValue> customSetterEvent = null, Action emptyValueEvent = null)
                : base(changeValueEvent, firstBasic, customSetterEvent, emptyValueEvent) { }
        }



        /// <summary>
        /// 단일 컨트롤러와 단일 값을 관리하는 클래스입니다. <br/>
        /// 기본 값(BasicValue)은 Func를 통해 제공되며, 값 변경 시 CustomSetterEvent를 통해 처리할 수 있습니다.
        /// </summary>
        /// <typeparam name="TValue">관리할 값의 형식 (구조체)</typeparam>
        /// <typeparam name="TController">컨트롤러의 형식 (참조형)</typeparam>
        [Serializable]
        public class ValueControlOneManager<TValue, TController> where TValue : struct where TController : class
        {
            ///======================================================================================================================================================



            /// <summary>
            /// 생성자입니다. <br/>
            /// 값 변경 이벤트(ChangeValueEvent), 기본 값 제공 함수(BasicValueEvent), 커스텀 세터(CustomSetterEvent)를 설정합니다.
            /// </summary>
            /// <param name="changeValueEvent">
            /// 값이 변경될 때 호출되는 이벤트 핸들러. <br/>
            /// 인자 1: 이전 값 (nullable), 인자 2: 새 값.
            /// </param>
            /// <param name="basicValueEvent">
            /// 기본 값을 제공하는 함수. <br/>
            /// 값이 null일 경우 이 함수의 반환값을 사용합니다.
            /// </param>
            /// <param name="customSetterEvent">
            /// 값 할당 시 커스텀 처리를 위한 델리게이트. <br/>
            /// 인자 1: 기존 값, 인자 2: 새 값을 받아 처리 결과를 반환합니다.
            /// </param>
            public ValueControlOneManager(Action<TValue?, TValue> changeValueEvent, Func<TValue> basicValueEvent, Func<TValue, TValue, TValue> customSetterEvent = null, Action emptyValueEvent = null)
            {
                ChangeValueEvent = changeValueEvent;
                BasicValueEvent = basicValueEvent;
                CustomSetterEvent = customSetterEvent;
                EmptyValueEvent = emptyValueEvent;
            }



            ///======================================================================================================================================================



            /// <summary>
            /// 현재 관리되는 값입니다. <br/>
            /// 내부적으로 CustomSetterEvent를 통해 값이 갱신될 수 있으며, 값이 변경되면 ChangeValueEvent가 호출됩니다.
            /// </summary>
            public TValue? Value
            {
                get => value;
                private set
                {
                    TValue? applyValue;
                    // 기존 값과 새 값이 모두 null이 아니고, 커스텀 세터가 존재하면 그 결과를 사용
                    if (this.value is not null && value is not null && CustomSetterEvent is not null)
                    {
                        applyValue = CustomSetterEvent.Invoke((TValue)this.value, (TValue)value);
                    }
                    else
                    {
                        applyValue = value;
                    }

                    // 새 값이 null이 아니면 ChangeValueEvent 호출
                    if (applyValue is not null)
                    {
                        ChangeValueEvent?.Invoke(this.value, (TValue)applyValue);
                    }

                    this.value = applyValue;
                }
            }
            [SerializeField] private TValue? value;



            /// <summary>
            /// 현재 값을 반환합니다. <br/>
            /// 값이 null이면 BasicValueEvent 함수를 호출하여 기본 값을 반환합니다.
            /// </summary>
            /// <returns>현재 값 또는 기본 값</returns>
            public TValue GetValue()
            {
                return Value ?? BasicValueEvent.Invoke();
            }



            /// <summary>
            /// 현재 값이 존재하면 그 값을 out 매개변수에 할당하고 true를 반환합니다. <br/>
            /// 값이 null이면 기본 값을 할당하고 false를 반환합니다.
            /// </summary>
            /// <param name="resultValue">현재 값 또는 기본 값이 할당됩니다.</param>
            /// <returns>값이 존재하면 true, 그렇지 않으면 false</returns>
            public bool TryGetValue(TValue resultValue)
            {
                if (Value.HasValue)
                {
                    resultValue = (TValue)Value;
                    return true;
                }
                else
                {
                    resultValue = BasicValueEvent.Invoke();
                    return false;
                }
            }



            ///======================================================================================================================================================



            /// <summary>
            /// 현재 컨트롤러를 나타냅니다.
            /// </summary>
            public TController Controller
            {
                get => controller;
                private set
                {
                    controller = value;

                    if (controller == null)
                    {
                        EmptyValueEvent?.Invoke(); //. 컨트롤러가 비었으니 Empty 이벤트 실행
                    }
                }
            }
            [SerializeField] private TController controller;



            ///======================================================================================================================================================



            /// <summary>
            /// 현재 컨트롤러의 우선순위를 나타내는 Rank 값입니다.
            /// </summary>
            [field: SerializeField] public int KeyRank { get; private set; } = 0;



            ///======================================================================================================================================================



            private readonly Action<TValue?, TValue> ChangeValueEvent;
            private readonly Func<TValue> BasicValueEvent;
            private readonly Func<TValue, TValue, TValue> CustomSetterEvent;
            private readonly Action EmptyValueEvent;


            ///======================================================================================================================================================



            /// <summary>
            /// 컨트롤러를 설정합니다. <br/>
            /// 기존 KeyRank보다 낮은 Rank를 가진 컨트롤러는 설정할 수 없습니다.
            /// </summary>
            /// <param name="controller">설정할 컨트롤러</param>
            /// <param name="keyRank">컨트롤러의 우선순위 Rank</param>
            /// <returns>설정 성공 시 true, 실패 시 false</returns>
            public bool SetController(TController controller, int keyRank = 0)
            {
                // 현재 KeyRank보다 낮은 경우에는 설정 실패
                if (KeyRank > keyRank) { return false; }

                Controller = controller;
                return true;
            }



            /// <summary>
            /// 지정된 컨트롤러와 일치할 경우 컨트롤러를 제거합니다.
            /// </summary>
            /// <param name="controller">제거할 컨트롤러</param>
            /// <returns>제거에 성공하면 true, 그렇지 않으면 false</returns>
            public bool RemoveController(TController controller)
            {
                if (Controller != controller) { return false; }

                Controller = null;

                return true;
            }



            /// <summary>
            /// 컨트롤러를 무조건 제거합니다.
            /// </summary>
            public void AbsoluteRemoveController()
            {
                Controller = null;
            }



            /// <summary>
            /// 컨트롤러를 무조건 설정합니다.
            /// </summary>
            /// <param name="controller">설정할 컨트롤러</param>
            public void AbsoluteSetController(TController controller)
            {
                Controller = controller;
            }



            ///======================================================================================================================================================



            /// <summary>
            /// 지정된 컨트롤러와 일치할 경우 현재 값을 새 값으로 변경합니다.
            /// </summary>
            /// <param name="controller">값 변경을 요청하는 컨트롤러</param>
            /// <param name="value">설정할 새 값</param>
            /// <returns>변경 성공 시 true, 그렇지 않으면 false</returns>
            public bool TrySetValue(TController controller, TValue value)
            {
                if (Controller != controller) { return false; }

                Value = value;
                return true;
            }



            /// <summary>
            /// 지정된 컨트롤러와 Rank를 기준으로 컨트롤러를 설정한 후 값을 변경합니다.
            /// </summary>
            /// <param name="controller">값 변경을 요청하는 컨트롤러</param>
            /// <param name="value">설정할 새 값</param>
            /// <param name="keyRank">컨트롤러의 우선순위 Rank</param>
            /// <returns>변경 성공 시 true, 그렇지 않으면 false</returns>
            public bool TrySetControllerValue(TController controller, TValue value, int keyRank = 0)
            {
                if (!SetController(controller, keyRank)) { return false; }

                Value = value;
                return true;
            }



            ///======================================================================================================================================================



            /// <summary>
            /// 모든 설정을 초기화하여 컨트롤러와 Rank, 값(Value)을 제거합니다.
            /// </summary>
            public void Reset()
            {
                KeyRank = 0;
                Controller = null;
                Value = null;
            }



            ///======================================================================================================================================================
        }



        /// <summary>
        /// ValueControlOneManager의 컨트롤러 타입을 object로 고정한 특수화 버전입니다.
        /// </summary>
        /// <typeparam name="TValue">관리할 값의 형식 (구조체)</typeparam>
        [Serializable]
        public class ValueControlOneManager<TValue> : ValueControlOneManager<TValue, object> where TValue : struct
        {
            public ValueControlOneManager(Action<TValue?, TValue> changeValueEvent, Func<TValue> basicValueEvent, Func<TValue, TValue, TValue> customSetterEvent = null, Action emptyValueEvent = null)
                : base(changeValueEvent, basicValueEvent, customSetterEvent, emptyValueEvent) { }
        }

    }



    ///======================================================================================================================================================



    //! 새로운 레거시
    namespace Legacy
    {
        //! Chips를 저장하는 구조를 Dictionary에서 List로 변경함에 따라 폐기
        ///

        //[Obsolete]
        //public class ValueControlManager_Stack<TValue, TController> : BaseValueControlManager<TValue, TController> where TValue : struct where TController : class
        //{
        //    ///======================================================================================================================================================



        //    /// <param name="changeValueEvent">( item 1 : 기존값 (oldV) item 2 : 새로운 값 (newV) </param>
        //    /// <param name="firstBasic">맨 처음 넣을 Basic 값 (없으면 null)</param>
        //    /// <param name="customSetterEvent">(item 1 : 기존값 (oldV) item 2 : 새로운 값 (newV) 값을 할당할때, 따로 커스텀해서 넣고싶을때</param>
        //    public ValueControlManager_Stack(Action<TValue?, TValue> changeValueEvent, TValue? firstBasic = null, Func<TValue, TValue, TValue> customSetterEvent = null) : base(changeValueEvent, firstBasic, customSetterEvent)
        //    {

        //    }



        //    ///======================================================================================================================================================



        //    private readonly Stack<ValueChips> ChipStack = new Stack<ValueChips>();



        //    ///======================================================================================================================================================



        //    public bool PushValue(TController controller, TValue value)
        //    {
        //        return PushChip(new Chip(value, controller));
        //    }



        //    public bool SetValue(TValue value)
        //    {
        //        return PushValue(null, value);
        //    }



        //    public bool PopRemoveValue(TController controller)
        //    {
        //        return PopRemoveRemoveChip(controller);
        //    }



        //    ///======================================================================================================================================================



        //    protected bool PushChip(ValueChips chip)
        //    {
        //        ChipStack.Push(chip);
        //        RefreshValue();
        //        return true;
        //    }



        //    protected bool PopRemoveRemoveChip(TController controller)
        //    {
        //        if (ChipStack.Count == 0) { return false; }

        //        var top = ChipStack.Peek();

        //        if (top.Controller == controller)
        //        {
        //            ChipStack.Pop();
        //            RefreshValue();
        //            return true;
        //        }

        //        return false;
        //    }



        //    protected bool PopRemoveRemoveChip()
        //    {
        //        if (ChipStack.Count == 0) { return false; }
        //        ChipStack.Pop();
        //        RefreshValue();
        //        return true;
        //    }



        //    ///======================================================================================================================================================



        //    protected override void RefreshValue()
        //    {
        //        if (ChipStack.Count == 0)
        //        {
        //            //Debug.Log("칩이 모두 비워저 BasicValue로 ChangeValueEvent 발동");
        //            ChangeValueEvent?.Invoke(Value, BasicValue);
        //            Value = null;
        //            return;
        //        }

        //        //? Chips가 존재하면, Chips중 가장 높은 키의 값이 Value가 된다
        //        Value = ChipStack.Peek().Value;
        //    }



        //    public override void Reset(TValue resetBasicValue)
        //    {
        //        base.Reset(resetBasicValue);
        //        ChipStack.Clear();
        //    }



        //    ///======================================================================================================================================================
        //}



        //[Obsolete]
        //public class ValueControlManager_Stack<TValue> : ValueControlManager_Stack<TValue, object> where TValue : struct
        //{
        //    public ValueControlManager_Stack(Action<TValue?, TValue> changeValueEvent, TValue? firstBasic = null, Func<TValue, TValue, TValue> customSetterEvent = null)
        //        : base(changeValueEvent, firstBasic, customSetterEvent) { }
        //}
    }



    //! 오래된 레거시
    namespace Legacy
    {
        /// <summary> 값의 변화를 제한/구분/계급화 시키는 클래스, Value는 직접 관리해야됨 그냥 public임</summary>
        /// <typeparam name="TValue">대상 값 타입</typeparam>
        /// <typeparam name="TUpdater">업데이터 타입 (인터페이스 추천)</typeparam>
        [Obsolete]
        public class ValueUpdater<TValue, TUpdater> where TValue : struct where TUpdater : class
        {



            public ValueUpdater(TValue firstValue)
            {
                Value = firstValue;
                Reset();
            }



            public ValueUpdater(Func<TValue, TValue> valueResetAction, Action<TValue> setValueAction = null)
            {
                ValueResetAction = valueResetAction;
                SetValueAction = setValueAction;
                Reset();
            }



            ///======================================================================================================================================================



            /// <summary>업데이트되는 대상 Value</summary>
            public TValue Value { get; private set; }



            /// <summary>현재 Updater 값</summary>
            public TUpdater Updater { get; private set; } = null;



            /// <summary>현재 Updater 랭크</summary>
            public int UpdaterRank { get; private set; } = 0;



            /// <summary>Value가 초기화될때마다 발동되는 이벤트</summary>
            private readonly Func<TValue, TValue> ValueResetAction = null;



            /// <summary>Value에 값을 넣는데 성공할때 발동되는 이벤트</summary>
            private readonly Action<TValue> SetValueAction = null;



            ///======================================================================================================================================================



            public bool TrySetValue(TUpdater updater, TValue value)
            {
                if (Updater == updater)
                {
                    Value = value;
                    SetValueAction?.Invoke(value);
                    return true;
                }
                else
                {
                    return false;
                }
            }



            ///======================================================================================================================================================



            /// <summary> Updater 설정 시도</summary>
            /// <param name="updater">넣을 Updater</param>
            /// <param name="updaterRank">넣을 랭크</param>
            /// <param name="overlap">중첩 허용 여부</param>
            public bool SetUpdater(TUpdater updater, int updaterRank = 0, bool overlap = true)
            {
                //이미 들어있다

                if (Updater != null)
                {

                    //?중첩 허용, 받아온 랭크가 기존 랭크보다 크거나 같으면 덮어씌운다
                    if (overlap && UpdaterRank <= updaterRank)
                    {
                        Updater = updater;
                        UpdaterRank = updaterRank;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                //새로 넣는다

                else
                {

                    Updater = updater;
                    UpdaterRank = updaterRank;
                    return true;
                }
            }



            /// <summary>Updater 제거 시도</summary>
            /// <param name="updater">대상 Updater</param>
            public bool RemoveUpdater(TUpdater updater)
            {
                if (Updater == updater)
                {
                    Updater = null;
                    return true;
                }

                return false;
            }



            /// <summary> (무조건) Updater 설정하기</summary>
            public void SetUpdater_Absolute(TUpdater value)
            {
                Updater = value;
            }



            /// <summary> (무조건) Updater 제거하기</summary>
            public void RemoveUpdater_Absolute()
            {
                Updater = null;
            }



            ///======================================================================================================================================================



            public void Reset()
            {
                Updater = null;
                UpdaterRank = 0;

                if (ValueResetAction != null)
                {
                    Value = ValueResetAction.Invoke(Value);
                }
            }



            ///======================================================================================================================================================
        }



        [Obsolete]
        public class ValueRankUpdater_Legacy<TValue, TUpdater> where TValue : struct where TUpdater : class
        {



            public class Chip
            {
                public Chip() { }

                public virtual TValue ThisValue { get; set; }

                public TUpdater ThisUpdater { get => thisUpdater; set => thisUpdater = value; }
                public TUpdater thisUpdater;

                public virtual void Reset()
                {
                    //Value는 어차피 이 Chip이 Pop되자마자 바로 값을 넣기때문에 굳이굳이 값을바꾸진않는다
                    thisUpdater = null;
                }
            }



            public class MainChip : Chip
            {
                //ThisChip용 생성자
                public MainChip(Func<TValue> valueResetEvent, Func<TValue, TValue, TValue> valueSetterEvent)
                {
                    ValueResetEvent = valueResetEvent;
                    ValueSetterEvent = valueSetterEvent;
                }

                public int MaxKeyRank = 0;

                private readonly Func<TValue> ValueResetEvent = null;
                private readonly Func<TValue, TValue, TValue> ValueSetterEvent = null;

                public override TValue ThisValue
                {
                    get => thisValue;
                    set
                    {
                        thisValue = ValueSetterEvent.Invoke(thisValue, value);
                    }
                }
                protected TValue thisValue;

                public override void Reset()
                {
                    base.Reset();
                    if (ValueResetEvent != null)
                    {
                        ThisValue = ValueResetEvent.Invoke();

                    }
                }

                public void Copy(int maxKeyRank, Chip targetChip)
                {
                    //Reset();
                    //! 230205 리셋시키고 시작하면 알람이 두번울린다

                    MaxKeyRank = maxKeyRank;

                    ThisValue = targetChip.ThisValue;
                    ThisUpdater = targetChip.ThisUpdater;
                }
            }



            /// <param name="valueResetEvent">값을 초기화시킬때의 이벤트</param>
            /// <param name="valueSetterEvent">값을 Set할때의 이벤트 ( item1 : 원래 value / item 2 : 들어온 value )</param>
            public ValueRankUpdater_Legacy(int chipPoolCount, Func<TValue> valueResetEvent, Func<TValue, TValue, TValue> valueSetterEvent)
            {
                ChipPool = new InfinityStack_ClassNew<Chip>(chipPoolCount);
                ChipPool.Create(chipPoolCount);

                Chips = new Dictionary<int, Chip>();

                ThisMainChip = new MainChip(valueResetEvent, valueSetterEvent);
            }



            /// <param name="valueResetEvent">값을 초기화시킬때의 이벤트</param>
            /// /// <param name="valueAlarmEvent">값을 Set할때의 이벤트 (새로들어온 값을 무조건 넣고, ) (주의 : 안에서 새로 Func생성함, 인라인 LambDa Func로 넣을것)</param>
            public ValueRankUpdater_Legacy(int chipPoolCount, Func<TValue> valueResetEvent, Action<TValue> valueAlarmEvent)
            {
                ChipPool = new InfinityStack_ClassNew<Chip>(chipPoolCount);
                ChipPool.Create(chipPoolCount);

                Chips = new Dictionary<int, Chip>();

                ThisMainChip = new MainChip(valueResetEvent, (oriV, newV) =>
                {
                    valueAlarmEvent(newV);
                    return newV;
                });
            }



            public static implicit operator TValue(ValueRankUpdater_Legacy<TValue, TUpdater> value) => value.MainValue;

            protected readonly MainChip ThisMainChip;


            public TValue MainValue
            {
                get => ThisMainChip.ThisValue;
                set => ThisMainChip.ThisValue = value;
            }


            public TUpdater MainUpdater
            {
                get => ThisMainChip.ThisUpdater;
                set => ThisMainChip.ThisUpdater = value;
            }


            private int MainChipKeyRank => ThisMainChip.MaxKeyRank;



            private readonly InfinityStack_ClassNew<Chip> ChipPool;
            private readonly Dictionary<int, Chip> Chips;



            public bool TryGetMainUpdater(out TUpdater updater)
            {
                if (MainUpdater == null) { updater = null; return false; }
                updater = MainUpdater;
                return true;
            }



            /// <summary>KeyRank와 Value로 추가하기</summary>
            public void SetChip(int keyRank, TValue value, bool overlap = false)
            {
                var popChip = ChipPool.Pop();
                popChip.ThisValue = value;

                AddChip(keyRank, popChip, overlap);
            }



            /// <summary>Updater + KeyRank와 Value로 추가하기</summary>
            public void SetChip(TUpdater updater, int keyRank, TValue value, bool overlap = false)
            {
                var popChip = ChipPool.Pop();
                popChip.ThisValue = value;
                popChip.ThisUpdater = updater;

                AddChip(keyRank, popChip, overlap);
            }



            public void SetChip_MaxPlusOne(TValue value)
            {
                var popChip = ChipPool.Pop();
                popChip.ThisValue = value;

                AddChip(MainChipKeyRank + 1, popChip, true);
            }



            public void SetChip_MaxPlusOne(TUpdater updater, TValue value)
            {
                var popChip = ChipPool.Pop();
                popChip.ThisValue = value;
                popChip.ThisUpdater = updater;

                AddChip(MainChipKeyRank + 1, popChip, true);
            }



            private bool AddChip(int keyRank, Chip chip, bool overlap)
            {
                if (Chips.ContainsKey(keyRank))
                {
                    if (overlap)
                    {
                        Chips[keyRank] = chip;
                        RefreshMainChip();
                        return true;
                    }
                }
                else
                {
                    Chips.Add(keyRank, chip);
                    RefreshMainChip();
                    return true;
                }

                ChipPool.Push(chip);
                return false;
            }



            public bool RemoveChip(int keyRank)
            {
                if (Chips.TryGetValue(keyRank, out var resultChip))
                {
                    ChipPool.Push(resultChip);
                    Chips.Remove(keyRank);
                    RefreshMainChip();
                    return true;
                }

                return false;
            }



            public bool RemoveChip(TUpdater updater, int keyRank)
            {
                if (Chips.TryGetValue(keyRank, out var resultChip) && resultChip.ThisUpdater == updater)
                {
                    ChipPool.Push(resultChip);
                    Chips.Remove(keyRank);
                    RefreshMainChip();

                    return true;
                }

                return false;
            }


            // MainChip이 갱신될때마다 발동        
            private void RefreshMainChip()
            {
                if (Chips.Count == 0)
                {
                    ThisMainChip.Reset();
                    return;
                }

                int maxKeyRank = Chips.Keys.Max();

                ThisMainChip.Copy(maxKeyRank, Chips[maxKeyRank]);
            }

            public void Reset()
            {
                foreach (var chip in Chips)
                {
                    ChipPool.Push(chip.Value);
                }
                Chips.Clear();

                RefreshMainChip();
            }
        }



        [Obsolete]
        public class ValueRankUpdater_Legacy<TValue> : ValueRankUpdater_Legacy<TValue, object> where TValue : struct
        {
            /// <param name="valueResetEvent">값을 초기화시킬때의 이벤트</param>
            /// <param name="valueSetterEvent">값을 Set할때의 이벤트 ( item1 : 원래 value / item 2 : 들어온 value )</param>
            public ValueRankUpdater_Legacy(int chipPoolCount, Func<TValue> valueResetEvent, Func<TValue, TValue, TValue> valueSetterEvent) : base(chipPoolCount, valueResetEvent, valueSetterEvent) { }

            /// <param name="valueResetEvent">값을 초기화시킬때의 이벤트</param>
            /// /// <param name="valueAlarmEvent">값을 Set할때의 이벤트 (새로들어온 값을 무조건 넣고, ) (주의 : 안에서 새로 Func생성함, 인라인 LambDa Func로 넣을것)</param>
            public ValueRankUpdater_Legacy(int chipPoolCount, Func<TValue> valueResetEvent, Action<TValue> valueAlarmEvent) : base(chipPoolCount, valueResetEvent, valueAlarmEvent) { }
        }
    }



    ///======================================================================================================================================================
}