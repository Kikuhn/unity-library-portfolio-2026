using System;
using System.Collections.Generic;
using UnityEngine;



//? 값을 관리하는 코드들이 정리되어있는 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    public static class SU_ValueControl
    {
        public static void SwitchBool(this ref bool var)
        {
            if (var)
            {
                var = false;
            }
            else
            {
                var = true;
            }
        }



        /// <summary>
        /// 현재 값(currentValue)과 새 값(newValue)을 비교하여, 값이 다른지 여부를 반환합니다. <br/>
        /// 현재 값이 null이면 true를 반환하며, null이 아니더라도 새 값과 다르면 true, 같으면 false를 반환합니다. <br/>
        /// (즉, 중복된 할당을 방지하기 위한 검사 메서드입니다.)
        /// </summary>
        /// <typeparam name="T">값 형식(struct)이며, IEquatable&lt;T&gt;를 구현한 타입입니다.</typeparam>
        /// <param name="currentValue">현재 값 (nullable)</param>
        /// <param name="newValue">비교할 새 값</param>
        /// <returns>
        /// 현재 값이 null이거나 새 값과 다르면 true, 동일하면 false를 반환합니다.
        /// </returns>
        public static bool IsDifferentFrom<T>(this T? currentValue, T newValue) where T : struct, IEquatable<T>
        {
            return !currentValue.HasValue || !currentValue.Value.Equals(newValue);
        }



        /// <summary>
        /// 현재 bool 값(currentValue)과 새 bool 값(newValue)을 비교하여, 값이 다른지 여부를 반환합니다. <br/>
        /// 현재 값이 null이면 true를 반환하며, null이 아니더라도 새 값과 다르면 true, 같으면 false를 반환합니다. <br/>
        /// (즉, 중복된 할당을 방지하기 위한 검사 메서드입니다.)
        /// </summary>
        /// <param name="currentValue">현재 bool 값 (nullable)</param>
        /// <param name="newValue">비교할 새 bool 값</param>
        /// <returns>
        /// 현재 값이 null이거나 새 값과 다르면 true, 동일하면 false를 반환합니다.
        /// </returns>
        public static bool IsDifferentFrom(this bool? currentValue, bool newValue)
        {
            return !currentValue.HasValue || currentValue.Value != newValue;
        }



        /// <summary>
        /// 현재 불리언 값(currentValue)을 새 값(newValue)으로 업데이트하면서, <br/>
        /// 지정한 전환 조건(triggerOnFalseToTrue)에 따라 전환이 발생했는지 여부를 반환합니다. <br/>
        /// - triggerOnFalseToTrue가 true인 경우: false → true 전환 시 true를 반환합니다. <br/>
        /// - triggerOnFalseToTrue가 false인 경우: true → false 전환 시 true를 반환합니다. <br/>
        /// 전환이 발생하지 않거나, 지정한 조건에 맞지 않으면 false를 반환합니다.
        /// </summary>
        /// <param name="currentValue">현재 bool 값 (참조 전달). 이 값은 새 값으로 업데이트됩니다.</param>
        /// <param name="newValue">설정할 새 bool 값.</param>
        /// <param name="triggerOnFalseToTrue">
        /// 전환 조건을 결정하는 플래그: <br/>
        /// true인 경우 false → true 전환 시 true 반환, <br/>
        /// false인 경우 true → false 전환 시 true 반환.
        /// </param>
        /// <returns>
        /// 지정한 전환 조건에 따라 전환이 발생하면 true, 그렇지 않으면 false를 반환합니다.
        /// </returns>
        public static bool SetAndCheckTransition(this ref bool currentValue, bool newValue, bool triggerOnFalseToTrue)
        {
            bool transitionOccurred = false;

            if (!currentValue && newValue)
            {
                // false → true 전환 발생
                if (triggerOnFalseToTrue)
                {
                    transitionOccurred = true;
                }
            }
            else if (currentValue && !newValue)
            {
                // true → false 전환 발생
                if (!triggerOnFalseToTrue)
                {
                    transitionOccurred = true;
                }
            }

            currentValue = newValue;
            return transitionOccurred;
        }
    }




    ///======================================================================================================================================================



    /// <summary>
    /// 열거형 상태와 해당 상태 변경에 적용된 Rank 값을 관리하는 제네릭 클래스입니다. <br/>
    /// 이 클래스는 상태 전환 시 Rank를 이용하여 변경 가능 여부를 결정할 수 있으며, <br/>
    /// Rank 없이 상태를 변경하면 기존 Rank 값은 초기화됩니다.
    /// </summary>
    /// <typeparam name="TEnum">
    /// 관리할 열거형 타입입니다. <br/>
    /// TEnum은 struct, Enum, IComparable, IConvertible, IFormattable 인터페이스를 구현해야 합니다.
    /// </typeparam>
    public class EnumStatusRank<TEnum> where TEnum : struct, Enum, IComparable, IConvertible, IFormattable
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 기본 상태를 사용하여 <see cref="EnumStatusRank{TEnum}"/>의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="defaultStatus">초기 상태 값</param>
        public EnumStatusRank(TEnum defaultStatus)
        {
            Status = defaultStatus;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// <see cref="EnumStatusRank{TEnum}"/> 인스턴스를 TEnum 타입으로 암시적으로 변환합니다. <br/>
        /// 이 연산자는 현재 상태 값(<see cref="Status"/>)을 반환합니다.
        /// </summary>
        /// <param name="value">변환할 <see cref="EnumStatusRank{TEnum}"/> 인스턴스</param>
        public static implicit operator TEnum(EnumStatusRank<TEnum> value) => value.Status;


        ///======================================================================================================================================================



        /// <summary>
        /// 현재 상태 값을 가져오거나 설정합니다. <br/>
        /// 상태를 설정하면, 이전에 적용된 Rank 값은 무조건 초기화됩니다.
        /// </summary>
        public TEnum Status
        {
            get => status;
            set
            {
                LastChangedRank = null;
                status = value;
            }
        }
        private TEnum status;



        ///======================================================================================================================================================



        /// <summary>
        /// 마지막 상태 변경 시 적용된 Rank 값을 나타냅니다. <br/>
        /// Rank 없이 상태가 변경된 경우 이 값은 null입니다.
        /// </summary>
        public int? LastChangedRank { get; private set; } = null;



        ///======================================================================================================================================================


        /// <summary>
        /// 내부적으로 상태와 Rank를 함께 업데이트하는 헬퍼 메서드입니다.
        /// </summary>
        /// <param name="value">새 상태 값</param>
        /// <param name="rank">적용할 Rank 값</param>
        private void SetStatus(TEnum value, int rank)
        {
            status = value;
            LastChangedRank = rank;
        }



        /// <summary>
        /// Rank 없이 상태를 변경을 시도합니다. <br/>
        /// 만약 이전 상태 변경에 Rank가 기록되어 있지 않으면, 새 상태로 변경하고 true를 반환합니다. <br/>
        /// 이미 Rank가 기록된 경우, 중복된 변경으로 간주하여 false를 반환합니다.
        /// </summary>
        /// <param name="value">변경할 새 상태 값</param>
        /// <returns>상태 변경이 성공하면 true, 실패하면 false를 반환합니다.</returns>
        public bool TryChange(TEnum value)
        {
            if (LastChangedRank == null)
            {
                Status = value;
                return true;
            }
            else
            {
                return false;
            }
        }



        /// <summary>
        /// Rank를 포함하여 상태 변경을 시도합니다. <br/>
        /// - 이전에 Rank가 기록되지 않은 경우, 새 상태와 Rank로 변경하고 true를 반환합니다. <br/>
        /// - 이전 Rank가 존재하는 경우, 새 Rank가 기존 Rank보다 낮거나 같은 경우에만 변경을 허용하고 true를 반환하며, <br/>
        ///   새 Rank가 기존 Rank보다 높으면 변경을 거부하고 false를 반환합니다.
        /// </summary>
        /// <param name="value">변경할 새 상태 값</param>
        /// <param name="rank">새로 적용할 Rank 값</param>
        /// <returns>상태 변경이 허용되면 true, 그렇지 않으면 false를 반환합니다.</returns>
        public bool TryChange(TEnum value, int rank)
        {
            //? Rank 값이 없으면 즉시 상태 변경 성공
            if (LastChangedRank == null)
            {
                SetStatus(value, rank);
                return true;
            }

            //? 기존 Rank가 존재하고, 새 Rank가 기존보다 높으면 실패
            if (rank < LastChangedRank)
            {
                return false;
            }
            //? 기존 Rank가 존재하고, 새 Rank가 같거나 낮으면 변경 성공
            else
            {
                SetStatus(value, rank);
                return true;
            }
        }



        /// <summary>
        /// 현재 상태 값이 지정한 <paramref name="value"/>와 동일한지 검사합니다. <br/>
        /// 내부적으로 <see cref="EqualityComparer{T}.Default"/>를 사용하여 비교합니다.
        /// </summary>
        /// <param name="value">비교할 상태 값</param>
        /// <returns>현재 상태가 <paramref name="value"/>와 같으면 true, 그렇지 않으면 false를 반환합니다.</returns>
        public bool Check(TEnum value)
        {
            return EqualityComparer<TEnum>.Default.Equals(status, value);
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================
}
