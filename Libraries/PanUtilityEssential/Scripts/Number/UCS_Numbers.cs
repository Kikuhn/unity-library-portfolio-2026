using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



//? 숫자들 관련 유틸리티들이 들어있는 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    //? 열거형



    /// <summary>
    /// 경계(최소/최대)를 나타내는 열거형입니다.
    /// </summary>
    public enum EBoundary
    {
        /// <summary>최소값을 나타냅니다.</summary>
        Min,
        /// <summary>최대값을 나타냅니다.</summary>
        Max
    }



    /// <summary>
    /// 어림수 구하기 (반올림, 올림, 내림)
    /// </summary>
    public enum ERounds
    {
        /// <summary> 반올림 </summary>
        Round,
        /// <summary> 내림 </summary>
        Floor,
        /// <summary> 올림 </summary>
        Ceil
    }



    /// <summary>
    /// 숫자 잠금 여부
    /// </summary>
    public enum ENumberValueStatus
    {
        /// <summary> 잠금해제, 자유 변형 가능 </summary>
        UnLock,
        /// <summary> 잠금, 변형 불가능 </summary>
        Lock,
        /// <summary> 감소만 가능</summary>
        OnlyMinus,
        /// <summary> 증가만 가능</summary>
        OnlyPlus
    }



    ///======================================================================================================================================================



    //? 커스텀 밸류 넘버 (~250922)



    /// <summary>
    /// 숫자형 값을 다루기 위한 추상 클래스입니다.<br/>
    /// 기본 값(<see cref="Basic"/>)과 곱할 값(<see cref="Multiple"/>)을 통해 최종 값(<see cref="Value"/>)을 계산합니다.
    /// </summary>
    /// <typeparam name="T">숫자형 타입 (unmanaged, IComparable 인터페이스를 구현한 타입)</typeparam>
    [Serializable]
    public abstract class CustomValueNumberBase<T> where T : unmanaged, IComparable<T>, IComparable
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 기본 생성자입니다.
        /// 생성자 호출 시 <see cref="Reset(bool, bool)"/>를 통해 값을 초기화합니다.
        /// </summary>
        public CustomValueNumberBase()
        {
            Reset(true, true);
        }



        /// <summary>
        /// 기본 값과 곱할 값으로 초기화하는 생성자입니다.
        /// </summary>
        /// <param name="defaultValue">초기 기본 값</param>
        /// <param name="multiple">초기 곱할 값</param>
        public CustomValueNumberBase(T defaultValue, T multiple)
        {
            SettingValue(defaultValue, multiple);
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 암시적 형변환 연산자입니다.
        /// <see cref="Value"/> 프로퍼티의 값으로 변환됩니다.
        /// </summary>
        /// <param name="value">변환할 CustomValueNumberBase 객체</param>
        public static implicit operator T(CustomValueNumberBase<T> value) => value.Value;



        ///======================================================================================================================================================



        /// <summary>
        /// 기본 값입니다. 값을 설정하면 <see cref="RefreshValue"/>가 호출되어 최종 값이 갱신됩니다.
        /// </summary>
        public T Basic
        {
            get => basicValue;
            set
            {
                basicValue = value;
                RefreshValue();
            }
        }
        [SerializeField]
        private T basicValue;



        /// <summary>
        /// 곱할 값입니다. 값을 설정하면 <see cref="RefreshValue"/>가 호출되어 최종 값이 갱신됩니다.
        /// </summary>
        public T Multiple
        {
            get => multiple;
            set
            {
                multiple = value;
                RefreshValue();
            }
        }
        [SerializeField]
        [ReadOnlyCustom(true)]
        private T multiple;



        ///======================================================================================================================================================



        /// <summary>
        /// 최종 값입니다.
        /// </summary>
        [field: SerializeField]
        public T Value { get; protected set; }



        ///======================================================================================================================================================



        /// <summary>
        /// 기본 값과 곱할 값을 설정합니다.
        /// </summary>
        /// <param name="defaultValue">설정할 기본 값</param>
        /// <param name="multiple">설정할 곱할 값</param>
        /// <returns>현재 객체를 반환합니다.</returns>
        public CustomValueNumberBase<T> SettingValue(T defaultValue, T multiple)
        {
            Basic = defaultValue;
            Multiple = multiple;
            return this;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 값 초기화를 수행합니다.
        /// </summary>
        /// <param name="resetExpandValues">
        /// 파생 클래스에서 추가된 확장 값들까지 초기화할지 여부입니다.
        /// </param>
        /// <param name="isConstructor">
        /// 생성자 내에서 호출되었는지 여부입니다.
        /// </param>
        public abstract void Reset(bool resetExpandValues = false, bool isConstructor = false);



        /// <summary>
        /// 기본 값과 곱할 값을 바탕으로 최종 값(<see cref="Value"/>)을 갱신합니다.
        /// </summary>
        protected abstract void RefreshValue();



        ///======================================================================================================================================================
    }



    /// <summary>
    /// <see cref="CustomValueNumberBase{T}"/>를 int 타입으로 구현한 클래스입니다.<br/>
    /// 기본 값과 곱할 값의 곱을 최종 값으로 계산합니다.
    /// </summary>
    [Serializable]
    public class CValueInt : CustomValueNumberBase<int>, ICopyable<CValueInt>
    {
        ///======================================================================================================================================================



        public CValueInt() : base() { }



        /// <summary>
        /// 기본 값과 곱할 값으로 초기화하는 생성자입니다.
        /// </summary>
        /// <param name="defaultValue">초기 기본 값</param>
        /// <param name="multiple">초기 곱할 값</param>
        public CValueInt(int defaultValue, int multiple) : base(defaultValue, multiple) { }



        ///======================================================================================================================================================



        /// <summary>
        /// 값 초기화를 수행합니다. 기본 값은 0, 곱할 값은 1로 설정됩니다.
        /// </summary>
        /// <param name="resetExpandValues">확장 값 초기화 여부 (현재 사용되지 않음)</param>
        /// <param name="isConstructor">생성자 내 호출 여부 (현재 사용되지 않음)</param>
        public override void Reset(bool resetExpandValues = false, bool isConstructor = false)
        {
            Basic = 0;
            Multiple = 1;

            RefreshValue();
        }



        /// <summary>
        /// 기본 값과 곱할 값을 곱하여 최종 값(<see cref="Value"/>)을 갱신합니다.
        /// </summary>
        protected override void RefreshValue()
        {
            Value = Basic * Multiple;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 주어진 <paramref name="original"/> 객체의 데이터를 복사하여 현재 객체에 적용합니다.
        /// </summary>
        /// <param name="original">복사할 원본 데이터입니다.</param>
        public void Copy(CValueInt original)
        {
            Basic = original.Basic;
            Multiple = original.Multiple;
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// <see cref="CustomValueNumberBase{T}"/>를 float 타입으로 구현한 클래스입니다.<br/>
    /// 기본 값과 곱할 값의 곱을 최종 값으로 계산합니다.
    /// </summary>
    [Serializable]
    public class CValueFloat : CustomValueNumberBase<float>, ICopyable<CValueFloat>
    {
        ///======================================================================================================================================================



        public CValueFloat() : base() { }



        /// <summary>
        /// 기본 값과 곱할 값으로 초기화하는 생성자입니다.
        /// </summary>
        /// <param name="defaultValue">초기 기본 값</param>
        /// <param name="multiple">초기 곱할 값</param>
        public CValueFloat(float defaultValue, float multiple) : base(defaultValue, multiple) { }



        ///======================================================================================================================================================



        /// <summary>
        /// 값 초기화를 수행합니다. 기본 값은 0, 곱할 값은 1로 설정됩니다.
        /// </summary>
        /// <param name="resetExpandValues">확장 값 초기화 여부 (현재 사용되지 않음)</param>
        /// <param name="isConstructor">생성자 내 호출 여부 (현재 사용되지 않음)</param>
        public override void Reset(bool resetExpandValues = false, bool isConstructor = false)
        {
            Basic = 0;
            Multiple = 1;

            RefreshValue();
        }



        /// <summary>
        /// 기본 값과 곱할 값을 곱하여 최종 값(<see cref="Value"/>)을 갱신합니다.
        /// </summary>
        protected override void RefreshValue()
        {
            Value = Basic * Multiple;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 주어진 <paramref name="original"/> 객체의 데이터를 복사하여 현재 객체에 적용합니다.
        /// </summary>
        /// <param name="original">복사할 원본 데이터입니다.</param>
        public void Copy(CValueFloat original)
        {
            Basic = original.Basic;
            Multiple = original.Multiple;
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// <see cref="CValueFloat"/>의 확장 클래스입니다.<br/>
    /// 기본 값과 곱할 값의 곱을 계산한 후, 지정된 최소값(<see cref="Min"/>)과 최대값(<see cref="Max"/>) 사이로 클램핑합니다.
    /// </summary>
    [Serializable]
    public class CValueFloat_Clamp : CValueFloat, ICopyable<CValueFloat_Clamp>
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 기본 생성자입니다.
        /// </summary>
        public CValueFloat_Clamp() : base() { }



        /// <summary>
        /// 기본 값과 곱할 값으로 초기화하는 생성자입니다.
        /// </summary>
        /// <param name="defaultValue">초기 기본 값</param>
        /// <param name="multiple">초기 곱할 값</param>
        public CValueFloat_Clamp(float defaultValue, float multiple) : base(defaultValue, multiple) { }



        ///======================================================================================================================================================



        /// <summary>
        /// 클램프 최소값입니다. 값이 변경되면 <see cref="RefreshValue"/>가 호출되어 최종 값이 갱신됩니다.
        /// </summary>
        [SerializeField]
        private float min = Mathf.NegativeInfinity;

        /// <summary>
        /// 클램프 최소값을 나타냅니다.
        /// </summary>
        public float Min
        {
            get => min;
            set
            {
                min = value;
                RefreshValue();
            }
        }



        /// <summary>
        /// 클램프 최대값입니다. 값이 변경되면 <see cref="RefreshValue"/>가 호출되어 최종 값이 갱신됩니다.
        /// </summary>
        [SerializeField]
        private float max = Mathf.Infinity;

        /// <summary>
        /// 클램프 최대값을 나타냅니다.
        /// </summary>
        public float Max
        {
            get => max;
            set
            {
                max = value;
                RefreshValue();
            }
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 기본 값, 곱할 값, 클램프 범위를 설정합니다.
        /// </summary>
        /// <param name="defaultValue">설정할 기본 값</param>
        /// <param name="multiple">설정할 곱할 값</param>
        /// <param name="min">설정할 클램프 최소값</param>
        /// <param name="max">설정할 클램프 최대값</param>
        /// <returns>현재 객체를 반환합니다.</returns>
        public CValueFloat_Clamp Settings(float defaultValue, float multiple, float min, float max)
        {
            Basic = defaultValue;
            Multiple = multiple;
            SettingClamp(min, max);
            return this;
        }



        /// <summary>
        /// 클램프 범위를 설정합니다.
        /// </summary>
        /// <param name="min">설정할 클램프 최소값</param>
        /// <param name="max">설정할 클램프 최대값</param>
        /// <returns>현재 객체를 반환합니다.</returns>
        public CValueFloat_Clamp SettingClamp(float min, float max)
        {
            Min = min;
            Max = max;
            return this;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 값 초기화를 수행합니다.
        /// 기본 값은 0, 곱할 값은 1로 설정됩니다.
        /// </summary>
        /// <param name="resetExpandValues">
        /// 확장 값(Min, Max)도 함께 초기화할지 여부입니다.
        /// </param>
        /// <param name="isConstructor">
        /// 생성자 내에서 호출되었는지 여부입니다.
        /// </param>
        public override void Reset(bool resetExpandValues = false, bool isConstructor = false)
        {
            Basic = 0;
            Multiple = 1;

            if (resetExpandValues)
            {
                min = Mathf.NegativeInfinity;
                max = Mathf.Infinity;
            }

            RefreshValue();
        }



        /// <summary>
        /// 기본 값과 곱할 값을 곱한 후, 클램프 범위(<see cref="Min"/>와 <see cref="Max"/>) 내로 제한하여 최종 값(<see cref="Value"/>)을 갱신합니다.
        /// </summary>
        protected override void RefreshValue()
        {
            Value = Mathf.Clamp(Basic * Multiple, Min, Max);
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 주어진 <paramref name="original"/> 객체의 데이터를 복사하여 현재 객체에 적용합니다.
        /// </summary>
        /// <param name="original">복사할 원본 데이터입니다.</param>
        public void Copy(CValueFloat_Clamp original)
        {
            base.Copy(original);

            Min = original.Min;
            Max = original.Max;
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// float 타입의 값을 이벤트로 제어하는 클래스입니다.
    /// 이 클래스는 기본 값과 곱할 값을 가지고 있으며, 값 변경 전후에 이벤트를 발생시킵니다.
    /// </summary>
    public class CEventAbleFloat
    {
        ///======================================================================================================================================================



        /// <summary>
        /// EventAbleFloat 클래스의 생성자입니다.
        /// 내부의 CValueFloat 및 값 제어 상태를 관리하는 ValueControlManager를 초기화합니다.
        /// </summary>
        public CEventAbleFloat()
        {
            FloatValue = new CValueFloat();

            BasicValue_Status = new ValueControlManager<ENumberValueStatus>(null, ENumberValueStatus.UnLock);
            MultipleValue_Status = new ValueControlManager<ENumberValueStatus>(null, ENumberValueStatus.UnLock);
        }



        ///======================================================================================================================================================



        private readonly CValueFloat FloatValue;



        ///======================================================================================================================================================



        /// <summary>
        /// 기본 값의 상태를 관리하는 ValueControlManager입니다.
        /// </summary>
        public readonly ValueControlManager<ENumberValueStatus> BasicValue_Status;
        /// <summary>
        /// 곱할 값의 상태를 관리하는 ValueControlManager입니다.
        /// </summary>
        public readonly ValueControlManager<ENumberValueStatus> MultipleValue_Status;



        ///======================================================================================================================================================



        /// <summary>
        /// 값 변경 전에 호출되는 이벤트입니다.
        /// </summary>
        public event Action Before_ChangeValueEvent = null;

        /// <summary>
        /// 값 변경 후에 호출되는 이벤트입니다.
        /// 첫 번째 매개변수는 현재 인스턴스, 두 번째 매개변수는 변경 전후의 차이값을 나타냅니다.
        /// </summary>
        public event Action<CEventAbleFloat, float> After_ChangeValueEvent = null;
        //? float : difference, 값이 적용되기 전과 이후를 비교해 얼마가 차이가 났는지 표시


        /// <summary>
        /// 기본 값 설정 시 커스텀 로직을 적용할 수 있는 이벤트입니다.
        /// 첫 번째 매개변수는 현재 값, 두 번째 매개변수는 새로 설정하려는 값을 의미하며,
        /// 반환값은 실제 적용할 값입니다.
        /// </summary>
        public event Func<float, float, float> CustomSetterBasicEvent = null;

        /// <summary>
        /// 곱할 값 설정 시 커스텀 로직을 적용할 수 있는 이벤트입니다.
        /// 첫 번째 매개변수는 현재 값, 두 번째 매개변수는 새로 설정하려는 값을 의미하며,
        /// 반환값은 실제 적용할 값입니다.
        /// </summary>
        public event Func<float, float, float> CustomSetterMultipleEvent = null;



        ///======================================================================================================================================================



        /// <summary>
        /// 계산된 최종 값을 반환합니다.
        /// </summary>
        public float Value => FloatValue.Value;



        /// <summary>
        /// 기본 값을 가져오거나 설정합니다.
        /// 값을 설정할 때, 변경 전/후 이벤트가 발생하며 최종 값이 갱신됩니다.
        /// </summary>
        public float Basic
        {
            get => FloatValue.Basic;
            set
            {
                float previousValue = Value;
                float willSetvalue;

                Before_ChangeValueEvent?.Invoke();



                if (CustomSetterBasicEvent != null)
                {
                    willSetvalue = CustomSetterBasicEvent.Invoke(FloatValue.Value, value);
                }
                else
                {
                    SetValue(BasicValue_Status.GetValue(), FloatValue.Basic, value, out willSetvalue);
                }



                //? 값이 바뀌었어야 After_ChangeValue 발동

                if (willSetvalue != FloatValue.Basic)
                {
                    FloatValue.Basic = willSetvalue;
                    After_ChangeValueEvent?.Invoke(this, Value - previousValue);
                }
            }
        }



        /// <summary>
        /// 곱할 값을 가져오거나 설정합니다.
        /// 값을 설정할 때, 변경 전/후 이벤트가 발생하며 최종 값이 갱신됩니다.
        /// </summary>
        public float Multiple
        {
            get => FloatValue.Multiple;
            set
            {
                float previousValue = Value;
                float willSetvalue;

                Before_ChangeValueEvent?.Invoke();



                if (CustomSetterMultipleEvent != null)
                {
                    willSetvalue = CustomSetterMultipleEvent.Invoke(FloatValue.Value, value);
                }
                else
                {
                    SetValue(MultipleValue_Status.GetValue(), FloatValue.Multiple, value, out willSetvalue);
                }



                //? 값이 바뀌었어야 After_ChangeValue 발동

                if (willSetvalue != FloatValue.Multiple)
                {
                    FloatValue.Multiple = willSetvalue;
                    After_ChangeValueEvent?.Invoke(this, Value - previousValue);
                }
            }
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 지정된 상태에 따라 새로운 값을 계산합니다.
        /// </summary>
        /// <param name="status">값 제어 상태입니다.</param>
        /// <param name="oldValue">기존 값입니다.</param>
        /// <param name="newValue">새로 적용하려는 값입니다.</param>
        /// <param name="calculatedValue">계산된 최종 값을 출력합니다.</param>
        public static void SetValue(ENumberValueStatus status, float oldValue, float newValue, out float calculatedValue)
        {
            if (oldValue == newValue)
            {
                calculatedValue = oldValue;
                return;
            }



            switch (status)
            {
                case ENumberValueStatus.UnLock:

                calculatedValue = newValue;

                return;



                case ENumberValueStatus.Lock:

                calculatedValue = oldValue;

                return;



                case ENumberValueStatus.OnlyMinus:

                if (oldValue > newValue)
                {
                    calculatedValue = newValue;
                }
                else
                {
                    calculatedValue = oldValue;
                }

                return;



                case ENumberValueStatus.OnlyPlus:

                if (oldValue < newValue)
                {
                    calculatedValue = newValue;
                }
                else
                {
                    calculatedValue = oldValue;
                }

                return;
            }

            calculatedValue = oldValue;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 내부 CValueFloat의 값을 초기화합니다.
        /// </summary>
        public void ResetValue()
        {
            FloatValue.Reset();
        }



        /// <summary>
        /// 값을 초기화하고, 값 변경 관련 이벤트들을 해제합니다.
        /// </summary>
        public void Reset()
        {
            ResetValue();

            Before_ChangeValueEvent = null;
            After_ChangeValueEvent = null;
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// int 타입의 값을 이벤트로 제어하는 클래스입니다.
    /// 이 클래스는 기본 값과 곱할 값을 가지고 있으며, 값 변경 전후에 이벤트를 발생시킵니다.
    /// </summary>
    public class CEventAbleInt
    {
        ///======================================================================================================================================================



        /// <summary>
        /// EventAbleInt 클래스의 생성자입니다.
        /// 내부의 CValueInt 및 값 제어 상태를 관리하는 ValueControlManager를 초기화합니다.
        /// </summary>
        public CEventAbleInt()
        {
            IntValue = new CValueInt();

            BasicValue_Status = new ValueControlManager<ENumberValueStatus>(null, ENumberValueStatus.UnLock);
            MultipleValue_Status = new ValueControlManager<ENumberValueStatus>(null, ENumberValueStatus.UnLock);
        }



        ///======================================================================================================================================================



        private readonly CValueInt IntValue;



        ///======================================================================================================================================================



        /// <summary>
        /// 기본 값의 상태를 관리하는 ValueControlManager입니다.
        /// </summary>
        public readonly ValueControlManager<ENumberValueStatus> BasicValue_Status;

        /// <summary>
        /// 곱할 값의 상태를 관리하는 ValueControlManager입니다.
        /// </summary>
        public readonly ValueControlManager<ENumberValueStatus> MultipleValue_Status;



        ///======================================================================================================================================================



        /// <summary>
        /// 값 변경 전에 호출되는 이벤트입니다.
        /// 인자로 현재 EventAbleInt 인스턴스를 전달합니다.
        /// </summary>
        public event Action<CEventAbleInt> Before_ChangeValueEvent = null;
        /// <summary>
        /// 값 변경 후에 호출되는 이벤트입니다.
        /// 첫 번째 매개변수는 현재 인스턴스, 두 번째 매개변수는 변경 전후의 차이값을 나타냅니다.
        /// </summary>
        public event Action<CEventAbleInt, int> After_ChangeValueEvent = null;
        //? int : difference, 값이 적용되기 전과 이후를 비교해 얼마가 차이가 났는지 표시



        /// <summary>
        /// 기본 값 설정 시 커스텀 로직을 적용할 수 있는 이벤트입니다.
        /// 첫 번째 매개변수는 현재 값, 두 번째 매개변수는 새로 설정하려는 값을 의미하며,
        /// 반환값은 실제 적용할 값입니다.
        /// </summary>
        public event Func<int, int, int> CustomSetterBasicEvent = null;
        /// <summary>
        /// 곱할 값 설정 시 커스텀 로직을 적용할 수 있는 이벤트입니다.
        /// 첫 번째 매개변수는 현재 값, 두 번째 매개변수는 새로 설정하려는 값을 의미하며,
        /// 반환값은 실제 적용할 값입니다.
        /// </summary>
        public event Func<int, int, int> CustomSetterMultipleEvent = null;



        ///======================================================================================================================================================



        /// <summary>
        /// 계산된 최종 값을 반환합니다.
        /// </summary>
        public int Value => IntValue.Value;



        /// <summary>
        /// 기본 값을 가져오거나 설정합니다.
        /// 값을 설정할 때, 변경 전/후 이벤트가 발생하며 최종 값이 갱신됩니다.
        /// </summary>
        public int Basic
        {
            get => IntValue.Basic;
            set
            {
                int previousValue = Value;
                int willSetvalue;

                Before_ChangeValueEvent?.Invoke(this);



                if (CustomSetterBasicEvent != null)
                {
                    willSetvalue = CustomSetterBasicEvent.Invoke(IntValue.Value, value);
                }
                else
                {
                    SetValue(BasicValue_Status.GetValue(), IntValue.Basic, value, out willSetvalue);
                }



                //? 값이 바뀌었어야 After_ChangeValue 발동

                if (willSetvalue != IntValue.Basic)
                {
                    IntValue.Basic = willSetvalue;
                    After_ChangeValueEvent?.Invoke(this, Value - previousValue);
                }
            }
        }



        /// <summary>
        /// 곱할 값을 가져오거나 설정합니다.
        /// 값을 설정할 때, 변경 전/후 이벤트가 발생하며 최종 값이 갱신됩니다.
        /// </summary>
        public int Multiple
        {
            get => IntValue.Multiple;
            set
            {
                int previousValue = Value;
                int willSetvalue;

                Before_ChangeValueEvent?.Invoke(this);



                if (CustomSetterMultipleEvent != null)
                {
                    willSetvalue = CustomSetterMultipleEvent.Invoke(IntValue.Value, value);
                }
                else
                {
                    SetValue(MultipleValue_Status.GetValue(), IntValue.Multiple, value, out willSetvalue);
                }



                //? 값이 바뀌었어야 After_ChangeValue 발동

                if (willSetvalue != IntValue.Multiple)
                {
                    IntValue.Multiple = willSetvalue;
                    After_ChangeValueEvent?.Invoke(this, Value - previousValue);
                }
            }
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 지정된 상태에 따라 새로운 값을 계산합니다.
        /// </summary>
        /// <param name="status">값 제어 상태입니다.</param>
        /// <param name="oldValue">기존 값입니다.</param>
        /// <param name="newValue">새로 적용하려는 값입니다.</param>
        /// <param name="calculatedValue">계산된 최종 값을 출력합니다.</param>
        public static void SetValue(ENumberValueStatus status, int oldValue, int newValue, out int calculatedValue)
        {
            if (oldValue == newValue)
            {
                calculatedValue = oldValue;
                return;
            }



            switch (status)
            {
                case ENumberValueStatus.UnLock:

                calculatedValue = newValue;

                return;



                case ENumberValueStatus.Lock:

                calculatedValue = oldValue;

                return;



                case ENumberValueStatus.OnlyMinus:

                if (oldValue > newValue)
                {
                    calculatedValue = newValue;
                }
                else
                {
                    calculatedValue = oldValue;
                }

                return;



                case ENumberValueStatus.OnlyPlus:

                if (oldValue < newValue)
                {
                    calculatedValue = newValue;
                }
                else
                {
                    calculatedValue = oldValue;
                }

                return;
            }

            calculatedValue = oldValue;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 내부 CValueInt의 값을 초기화합니다.
        /// </summary>
        public void ResetValue()
        {
            IntValue.Reset();
        }



        /// <summary>
        /// 값을 초기화하고, 값 변경 관련 이벤트들을 해제합니다.
        /// </summary>
        public void Reset()
        {
            ResetValue();

            Before_ChangeValueEvent = null;
            After_ChangeValueEvent = null;
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================



    //. 숫자 확장



    /// <summary>
    /// 숫자형 값을 다루기 위한 최소 추상 베이스입니다.
    /// <see cref="Basic"/> × <see cref="Multiple"/> → <see cref="Value"/> 를 유지/갱신합니다.
    /// 파생 클래스가 산술(곱셈/비교)을 구현합니다.
    /// </summary>
    [Serializable]
    public abstract class NumberExtendedBase<T>
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 기본 생성자.
        /// </summary>
        protected NumberExtendedBase()
        {
            Reset(isConstructor: true);
        }



        /// <summary>
        /// 기본 값과 곱할 값으로 초기화하는 생성자입니다.
        /// </summary>
        public NumberExtendedBase(T basic, T multiple)
        {
            _basic = basic;
            _multiple = multiple;
            RefreshValue_Internal();
        }



        /// <summary>
        /// 암시적 형변환: 최종 값으로 변환됩니다.
        /// </summary>
        public static implicit operator T(NumberExtendedBase<T> v) => v is null ? default : v.Value;



        ///======================================================================================================================================================



        [SerializeField] private T _basic;
        [SerializeField] private T _multiple;



        /// <summary>
        /// 기본 값입니다. 설정 시 최종 값이 즉시 갱신됩니다.
        /// </summary>
        public T Basic
        {
            get => _basic;
            set => SetInternal(value, _multiple, suppressEvent: false);
        }



        /// <summary>
        /// 곱할 값입니다. 설정 시 최종 값이 즉시 갱신됩니다.
        /// </summary>
        public T Multiple
        {
            get => _multiple;
            set => SetInternal(_basic, value, suppressEvent: false);
        }



        /// <summary>
        /// 최종 값입니다. <see cref="Basic"/>과 <see cref="Multiple"/>이 바뀌면 자동 갱신됩니다.
        /// </summary>
        public T Value { get; protected set; }



        /// <summary>
        /// 값이 바뀌었을 때 호출됩니다. (old, @new)
        /// </summary>
        public event Action<T, T> OnValueChanged;



        ///======================================================================================================================================================



        /// <summary>
        /// 기본 값과 곱할 값을 한 번에 설정합니다.
        /// </summary>
        public NumberExtendedBase<T> Set(T basic, T multiple)
        {
            SetInternal(basic, multiple, suppressEvent: false);
            return this;
        }



        /// <summary>
        /// 값 초기화(기본=0, 곱=1)를 수행합니다.
        /// </summary>
        public void Reset(bool isConstructor = false)
        {
            SetInternal(GetZero(), GetOne(), suppressEvent: isConstructor);
        }



        private void SetInternal(T basic, T multiple, bool suppressEvent)
        {
            bool ch1 = !IsSame(_basic, basic);
            bool ch2 = !IsSame(_multiple, multiple);
            if (!ch1 && !ch2) return;

            var prev = Value;
            _basic = basic;
            _multiple = multiple;
            RefreshValue_Internal();

            if (!suppressEvent)
                RaiseIfChanged(prev, Value);
        }



        private void RefreshValue_Internal()
        {
            Value = Multiply(_basic, _multiple);
        }



        ///======================================================================================================================================================



        /// <summary>
        /// a × b 를 반환합니다.
        /// </summary>
        protected abstract T Multiply(in T a, in T b);

        /// <summary>
        /// 0 값을 반환합니다. (Reset 시 기본값)
        /// </summary>
        protected abstract T GetZero();

        /// <summary>
        /// 1 값을 반환합니다. (Reset 시 곱할값)
        /// </summary>
        protected abstract T GetOne();

        /// <summary>
        /// 값 동등 비교. float의 경우 오차 허용 등 커스터마이즈.
        /// </summary>
        protected virtual bool IsSame(in T a, in T b) => EqualityComparer<T>.Default.Equals(a, b);

        /// <summary>
        /// 변경 이벤트 호출 유틸.
        /// </summary>
        protected void RaiseIfChanged(in T prev, in T next)
        {
            if (!IsSame(prev, next))
                OnValueChanged?.Invoke(prev, next);
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// float 전용 구현. <see cref="Mathf.Approximately(float, float)"/> 유사 기준으로 비교합니다.
    /// </summary>
    [Serializable]
    public sealed class FloatExtend : NumberExtendedBase<float>
    {
        public FloatExtend() : base() { }
        public FloatExtend(float basic, float multiple) : base(basic, multiple) { }

        protected override float Multiply(in float a, in float b) => a * b;
        protected override float GetZero() => 0f;
        protected override float GetOne() => 1f;

        protected override bool IsSame(in float a, in float b)
        {
            return Mathf.Abs(a - b) <= 1e-6f; //. 오차 허용 비교
        }
    }



    /// <summary>
    /// int 전용 구현.
    /// </summary>
    [Serializable]
    public sealed class IntExtend : NumberExtendedBase<int>
    {
        public IntExtend() : base() { }
        public IntExtend(int basic, int multiple) : base(basic, multiple) { }

        protected override int Multiply(in int a, in int b) => a * b;
        protected override int GetZero() => 0;
        protected override int GetOne() => 1;
    }



    ///======================================================================================================================================================



    /// <summary>
    /// 숫자 상태를 관리하는 클래스입니다.
    /// 내부적으로 <see cref="ValueControlManager{ENumberValueStatus}"/>를 사용하여 현재 숫자 제어 상태를 관리합니다.
    /// </summary>
    public class NumberStatusManager
    {
        ///======================================================================================================================================================



        /// <summary>
        /// <see cref="NumberStatusManager"/>의 생성자입니다.
        /// 내부 <see cref="ValueControlManager{ENumberValueStatus}"/>를 <see cref="ENumberValueStatus.UnLock"/> 상태로 초기화합니다.
        /// </summary>
        public NumberStatusManager()
        {
            Manager = new ValueControlManager<ENumberValueStatus>(null, ENumberValueStatus.UnLock, null);
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 숫자 상태 제어를 담당하는 <see cref="ValueControlManager{ENumberValueStatus}"/>입니다.
        /// </summary>
        public readonly ValueControlManager<ENumberValueStatus> Manager;



        /// <summary>
        /// 현재 숫자 제어 상태를 반환합니다.
        /// </summary>
        /// <returns>현재의 <see cref="ENumberValueStatus"/> 값</returns>
        public ENumberValueStatus GetValue() => Manager.GetValue();



        ///======================================================================================================================================================



        /// <summary>
        /// 주어진 <paramref name="oldValue"/>와 <paramref name="newValue"/>의 변화에 대해,
        /// 현재 상태에서 변경을 취소해야 하는지 검사합니다.
        /// </summary>
        /// <param name="oldValue">이전 값 (float)</param>
        /// <param name="newValue">새로운 값 (float)</param>
        /// <returns>
        /// 상태가 <see cref="ENumberValueStatus.Lock"/>인 경우 또는 
        /// 변화가 <see cref="ENumberValueStatus.OnlyMinus"/> 상태에서 증가하는 경우 또는 
        /// <see cref="ENumberValueStatus.OnlyPlus"/> 상태에서 감소하는 경우 <c>true</c>를 반환합니다.
        /// 그렇지 않으면 <c>false</c>를 반환합니다.
        /// </returns>
        public bool Check_IsCancel(float oldValue, float newValue)
        {
            var status = Manager.GetValue();

            if (status == ENumberValueStatus.Lock) { return true; }

            if (oldValue < newValue && status == ENumberValueStatus.OnlyMinus) { return true; }

            if (oldValue > newValue && status == ENumberValueStatus.OnlyPlus) { return true; }

            return false;
        }



        /// <summary>
        /// 주어진 <paramref name="oldValue"/>와 <paramref name="newValue"/>의 변화에 대해,
        /// 현재 상태에서 변경을 취소해야 하는지 검사합니다.
        /// </summary>
        /// <param name="oldValue">이전 값 (int)</param>
        /// <param name="newValue">새로운 값 (int)</param>
        /// <returns>
        /// 상태가 <see cref="ENumberValueStatus.Lock"/>인 경우 또는 
        /// 변화가 <see cref="ENumberValueStatus.OnlyMinus"/> 상태에서 증가하는 경우 또는 
        /// <see cref="ENumberValueStatus.OnlyPlus"/> 상태에서 감소하는 경우 <c>true</c>를 반환합니다.
        /// 그렇지 않으면 <c>false</c>를 반환합니다.
        /// </returns>
        public bool Check_IsCancel(int oldValue, int newValue)
        {
            var status = Manager.GetValue();

            if (status == ENumberValueStatus.Lock) { return true; }

            if (oldValue < newValue && status == ENumberValueStatus.OnlyMinus) { return true; }

            if (oldValue > newValue && status == ENumberValueStatus.OnlyPlus) { return true; }

            return false;
        }



        ///======================================================================================================================================================
    }



    namespace Legacy
    {
        public class NumberStatusManager
        {



            public NumberStatusManager()
            {
                Manager = new ValueControlManager<ENumberValueStatus>(null, ENumberValueStatus.UnLock, null);
            }



            public readonly ValueControlManager<ENumberValueStatus> Manager;



            public ENumberValueStatus GetValue() => Manager.GetValue();



            public bool Check_IsCancel(float oldValue, float newValue)
            {
                var status = Manager.GetValue();

                if (status == ENumberValueStatus.Lock) { return true; }

                if (oldValue < newValue && status == ENumberValueStatus.OnlyMinus) { return true; }

                if (oldValue > newValue && status == ENumberValueStatus.OnlyPlus) { return true; }

                return false;
            }



            public bool Check_IsCancel(int oldValue, int newValue)
            {
                var status = Manager.GetValue();

                if (status == ENumberValueStatus.Lock) { return true; }

                if (oldValue < newValue && status == ENumberValueStatus.OnlyMinus) { return true; }

                if (oldValue > newValue && status == ENumberValueStatus.OnlyPlus) { return true; }

                return false;
            }
        }
    }



    ///======================================================================================================================================================
}