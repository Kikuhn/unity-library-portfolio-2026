using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Events;
using DG.Tweening;
using Pan.Util;
using System.Linq;



//? [Math] 수학 연산이 정리되어있는 정도의 코드



namespace Pan.Util
{
    public static class SU_Math
    {
        ///======================================================================================================================================================



        //? 수학 연산



        /// <summary>
        /// 주어진 두 수의 최대공약수를 구합니다. (Greatest common divisor)
        /// </summary>
        /// <param name="a">첫 번째 숫자</param>
        /// <param name="b">두 번째 숫자</param>
        /// <returns>두 수의 최대공약수</returns>
        public static int CalculateGCD(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }



        /// <summary>
        /// 주어진 두 수의 최소공배수를 구합니다. (Least common multiple)
        /// </summary>
        /// <param name="a">첫 번째 숫자</param>
        /// <param name="b">두 번째 숫자</param>
        /// <returns>두 수의 최소공배수</returns>
        public static int CalculateLCM(int a, int b)
        {
            // 두 수의 곱을 최대공약수로 나누어 최소공배수를 구함
            return Math.Abs(a * b) / CalculateGCD(a, b);
        }



        /// <summary>
        /// 주어진 정수를 소인수분해하여, 결과를 새로운 List<int>로 반환합니다.
        /// </summary>
        /// <param name="target">소인수분해할 정수</param>
        /// <returns>소인수들의 리스트</returns>
        public static List<int> GetPrimeFactors(int target)
        {
            List<int> primeFactors = new List<int>();
            AppendPrimeFactors(primeFactors, target);
            return primeFactors;
        }



        /// <summary>
        /// 주어진 정수를 소인수분해하여, 지정된 IList<int>에 소인수를 추가합니다.
        /// </summary>
        /// <param name="factors">소인수가 추가될 IList<int>입니다.</param>
        /// <param name="target">소인수분해할 정수</param>
        public static void AppendPrimeFactors(IList<int> factors, int target)
        {
            //! 2로 나누어 떨어지는 모든 경우
            while (target % 2 == 0)
            {
                factors.Add(2);
                target /= 2;
            }

            //! 3부터 시작하여, 홀수로 나누어가며 소인수 구하기
            for (int i = 3; i * i <= target; i += 2)
            {
                while (target % i == 0)
                {
                    factors.Add(i);
                    target /= i;
                }
            }

            //! 마지막으로 남은 값이 2 이상이면 소수이므로 추가
            if (target > 2)
            {
                factors.Add(target);
            }
        }



        ///======================================================================================================================================================



        //? float, 부동소수점



        #region float



        /// <summary>
        /// 이 float가 정수인지 확인 (나머지 연산으로 확인)
        /// </summary>
        /// <param name="var"></param>
        /// <returns></returns>
        public static bool IsInteger(this float var)
        {
            return var % 1 == 0;
        }



        //? 부동소수점 비교 유틸리티 (오차 보정)



        /// <summary>
        /// 두 부동소수점 값이 지정된 오차 범위 내에서 같은지 확인합니다.
        /// </summary>
        /// <param name="value1">첫 번째 비교 대상 부동소수점 값</param>
        /// <param name="value2">두 번째 비교 대상 부동소수점 값</param>
        /// <param name="tolerance">비교 시 허용할 오차 값 (기본값: 0.0001f)</param>
        /// <returns>두 값이 오차 범위 내에서 같다면 true, 그렇지 않다면 false</returns>
        public static bool CheckFloatEqual(this float value1, float value2, float tolerance = 0.0001f)
        {
            return Mathf.Abs(value1 - value2) <= tolerance;
        }



        /// <summary>
        /// 두 2D 벡터가 지정된 오차 범위 내에서 같은지 확인합니다.
        /// </summary>
        /// <param name="vector1">첫 번째 비교 대상 벡터</param>
        /// <param name="vector2">두 번째 비교 대상 벡터</param>
        /// <param name="tolerance">비교 시 허용할 오차 값 (기본값: 0.0001f)</param>
        /// <returns>두 벡터가 오차 범위 내에서 같다면 true, 그렇지 않다면 false</returns>
        public static bool CheckVectorEqual(this Vector2 vector1, Vector2 vector2, float tolerance = 0.0001f)
        {
            return (vector2 - vector1).sqrMagnitude < tolerance * tolerance;
        }



        /// <summary>
        /// 두 3D 벡터가 지정된 오차 범위 내에서 같은지 확인합니다.
        /// </summary>
        /// <param name="vector1">첫 번째 비교 대상 벡터</param>
        /// <param name="vector2">두 번째 비교 대상 벡터</param>
        /// <param name="tolerance">비교 시 허용할 오차 값 (기본값: 0.0001f)</param>
        /// <returns>두 벡터가 오차 범위 내에서 같다면 true, 그렇지 않다면 false</returns>
        public static bool CheckVectorEqual(this Vector3 vector1, Vector3 vector2, float tolerance = 0.0001f)
        {
            return (vector2 - vector1).sqrMagnitude < tolerance * tolerance;
        }



        #endregion



        ///======================================================================================================================================================



        //? 어림수 계산



        #region int 어림수



        ///======================================================================================================================================================



        /// <summary>
        /// 주어진 값에 대해 지정된 어림수 모드를 적용합니다.
        /// </summary>
        /// <param name="value">어림수를 구할 값입니다.</param>
        /// <param name="roundMode">어림수 모드입니다. 반올림, 내림, 올림 중 하나를 선택합니다.</param>
        /// <returns>어림수 모드에 따라 처리된 값이 반환됩니다.</returns>
        public static int Round(this int value, ERounds roundMode = ERounds.Round)
        {
            switch (roundMode)
            {
                case ERounds.Floor: return Mathf.FloorToInt(value);
                case ERounds.Ceil: return Mathf.CeilToInt(value);
                case ERounds.Round: return Mathf.RoundToInt(value);
                default: return value;
            }
        }



        /// <summary>
        /// 주어진 값을 특정 단위로 반올림, 올림 또는 내림하여, 참조로 전달된 변수에 그 값을 저장합니다.
        /// </summary>
        /// <param name="value">어림수를 구할 값입니다. 이 값이 참조로 수정됩니다.</param>
        /// <param name="unit">단위입니다. 이 단위의 배수로 어림수가 계산됩니다.</param>
        /// <param name="roundMode">어림수 모드입니다.</param>
        public static void SetRound(this ref int value, int unit, ERounds roundMode = ERounds.Round)
        {
            value = value.Round_Unit(unit, roundMode);
        }



        /// <summary>
        /// 주어진 값을 특정 단위로 반올림, 올림 또는 내림하여 반환합니다.
        /// </summary>
        /// <param name="value">반올림할 값입니다.</param>
        /// <param name="unit">단위입니다. 이 단위의 배수로 어림수가 계산됩니다.</param>
        /// <param name="roundMode">어림수 모드입니다.</param>
        /// <returns>지정된 단위와 어림수 모드에 따라 처리된 값이 반환됩니다.</returns>
        public static int Round_Unit(this int value, int unit, ERounds roundMode = ERounds.Round)
        {
            return Round((value / unit), roundMode) * unit;
        }



        /// <summary>
        /// 주어진 값을 특정 단위로 반올림, 올림 또는 내림하여, 참조로 전달된 변수에 그 값을 저장합니다.
        /// </summary>
        /// <param name="value">반올림할 값입니다.</param>
        /// <param name="unit">단위입니다. 이 단위의 배수로 어림수가 계산됩니다.</param>
        /// <param name="roundMode">어림수 모드입니다.</param>
        /// <returns>지정된 단위와 어림수 모드에 따라 처리된 값이 반환됩니다.</returns>
        public static int SetRound_Unit(this int value, int unit, ERounds roundMode = ERounds.Round)
        {
            return value = Round((value / unit), roundMode) * unit;
        }



        ///======================================================================================================================================================



        #endregion



        #region float 어림수



        ///======================================================================================================================================================



        //? float



        /// <summary>
        /// 주어진 값에 대해 지정된 어림수 모드를 적용합니다.
        /// </summary>
        /// <param name="value">어림수를 구할 값입니다.</param>
        /// <param name="roundMode">어림수 모드입니다. 반올림, 내림, 올림 중 하나를 선택합니다.</param>
        /// <returns>어림수 모드에 따라 처리된 값이 반환됩니다.</returns>
        public static float Round(this float value, ERounds roundMode = ERounds.Round)
        {
            switch (roundMode)
            {
                case ERounds.Floor: return Mathf.Floor(value);
                case ERounds.Ceil: return Mathf.Ceil(value);
                case ERounds.Round: return Mathf.Round(value);
                default: return value;
            }
        }



        /// <summary>
        /// 주어진 값을 특정 단위로 반올림, 올림 또는 내림하여, 참조로 전달된 변수에 그 값을 저장합니다.
        /// </summary>
        /// <param name="value">어림수를 구할 값입니다. 이 값이 참조로 수정됩니다.</param>
        /// <param name="unit">단위입니다. 이 단위의 배수로 어림수가 계산됩니다.</param>
        /// <param name="roundMode">어림수 모드입니다.</param>
        public static void SetRound(this ref float value, float unit, ERounds roundMode = ERounds.Round)
        {
            value = value.Round_Unit(unit, roundMode);
        }



        /// <summary>
        /// 주어진 값을 특정 단위로 반올림, 올림 또는 내림하여 반환합니다.
        /// </summary>
        /// <param name="value">반올림할 값입니다.</param>
        /// <param name="unit">단위입니다. 이 단위의 배수로 어림수가 계산됩니다.</param>
        /// <param name="roundMode">어림수 모드입니다.</param>
        /// <returns>지정된 단위와 어림수 모드에 따라 처리된 값이 반환됩니다.</returns>
        public static float Round_Unit(this float value, float unit, ERounds roundMode = ERounds.Round)
        {
            return Round((value / unit), roundMode) * unit;
        }



        /// <summary>
        /// 주어진 값을 특정 단위로 반올림, 올림 또는 내림하여, 참조로 전달된 변수에 그 값을 저장합니다.
        /// </summary>
        /// <param name="value">반올림할 값입니다.</param>
        /// <param name="unit">단위입니다. 이 단위의 배수로 어림수가 계산됩니다.</param>
        /// <param name="roundMode">어림수 모드입니다.</param>
        /// <returns>지정된 단위와 어림수 모드에 따라 처리된 값이 반환됩니다.</returns>
        public static float SetRound_Unit(this float value, float unit, ERounds roundMode = ERounds.Round)
        {
            return value = Round((value / unit), roundMode) * unit;
        }



        ///======================================================================================================================================================



        //? float int 어림수



        /// <summary>
        /// 주어진 값에 대해 지정된 어림수 모드를 적용하여 정수로 반환합니다.
        /// </summary>
        /// <param name="value">어림수를 구할 값입니다.</param>
        /// <param name="roundMode">어림수 모드입니다. 반올림, 내림, 올림 중 하나를 선택합니다.</param>
        /// <returns>어림수 모드에 따라 처리된 정수 값이 반환됩니다.</returns>
        public static int RoundToInt(this float value, ERounds roundMode = ERounds.Round)
        {
            switch (roundMode)
            {
                case ERounds.Floor: return Mathf.FloorToInt(value);
                case ERounds.Ceil: return Mathf.CeilToInt(value);
                case ERounds.Round: return Mathf.RoundToInt(value);
                default: return (int)value;
            }
        }



        ///======================================================================================================================================================



        #endregion



        #region Vector2 어림수



        ///======================================================================================================================================================



        /// <summary>
        /// 주어진 Vector2의 각 축(x, y)을 특정 단위로 반올림, 올림 또는 내림하여 반환합니다.
        /// </summary>
        /// <param name="vector">어림수를 구할 Vector2입니다.</param>
        /// <param name="unit">단위입니다. 각 축이 이 단위의 배수로 어림수가 계산됩니다.</param>
        /// <param name="roundMode">어림수 모드입니다.</param>
        /// <returns>각 축이 지정된 단위와 어림수 모드에 따라 처리된 Vector2를 반환합니다.</returns>
        public static Vector2 Round_Unit(this Vector2 vector, float unit, ERounds roundMode = ERounds.Round)
        {
            return new Vector2(
                vector.x.Round_Unit(unit, roundMode),
                vector.y.Round_Unit(unit, roundMode)
            );
        }



        /// <summary>
        /// 주어진 Vector2의 각 축(x, y)을 특정 단위로 반올림, 올림 또는 내림하여, 참조로 전달된 벡터에 그 값을 저장합니다.
        /// </summary>
        /// <param name="vector">어림수를 구할 Vector2입니다. 이 값이 참조로 수정됩니다.</param>
        /// <param name="unit">단위입니다. 각 축이 이 단위의 배수로 어림수가 계산됩니다.</param>
        /// <param name="roundMode">어림수 모드입니다.</param>
        public static void SetRound_Unit(this ref Vector2 vector, float unit, ERounds roundMode = ERounds.Round)
        {
            vector.x = vector.x.Round_Unit(unit, roundMode);
            vector.y = vector.y.Round_Unit(unit, roundMode);
        }



        /// <summary>
        /// 주어진 Vector2의 각 축(x, y)을 특정 그리드 단위(Vector2)로 반올림, 올림 또는 내림하여 반환합니다.
        /// </summary>
        /// <param name="vector">어림수를 구할 Vector2입니다.</param>
        /// <param name="unit">각 축이 이 단위의 배수로 어림수가 계산됩니다.</param>
        /// <param name="roundMode">어림수 모드입니다.</param>
        /// <returns>각 축이 지정된 단위와 어림수 모드에 따라 처리된 Vector2를 반환합니다.</returns>
        public static Vector2 Round_Unit(this Vector2 vector, Vector2 unit, ERounds roundMode = ERounds.Round)
        {
            return new Vector2(
                vector.x.Round_Unit(unit.x, roundMode),
                vector.y.Round_Unit(unit.y, roundMode)
            );
        }



        /// <summary>
        /// 주어진 Vector2의 각 축(x, y)을 특정 그리드 단위(Vector2)로 반올림, 올림 또는 내림하여, 참조로 전달된 벡터에 그 값을 저장합니다.
        /// </summary>
        /// <param name="vector">어림수를 구할 Vector2입니다. 이 값이 참조로 수정됩니다.</param>
        /// <param name="unit">각 축이 이 단위의 배수로 어림수가 계산됩니다.</param>
        /// <param name="roundMode">어림수 모드입니다.</param>
        public static void SetRound_Unit(this ref Vector2 vector, Vector2 unit, ERounds roundMode = ERounds.Round)
        {
            vector.x = vector.x.Round_Unit(unit.x, roundMode);
            vector.y = vector.y.Round_Unit(unit.y, roundMode);
        }



        ///======================================================================================================================================================



        //? Vector2 Round, SetRound (기본 반올림 메서드)



        /// <summary>
        /// 주어진 Vector2의 각 축(x, y)에 대해 지정된 어림수 모드를 적용합니다.
        /// </summary>
        /// <param name="vector">어림수를 구할 Vector2입니다.</param>
        /// <param name="roundMode">어림수 모드입니다. 반올림, 내림, 올림 중 하나를 선택합니다.</param>
        /// <returns>각 축에 대해 어림수 모드가 적용된 Vector2를 반환합니다.</returns>
        public static Vector2 Round(this Vector2 vector, ERounds roundMode = ERounds.Round)
        {
            return new Vector2(
                vector.x.Round(roundMode),
                vector.y.Round(roundMode)
            );
        }



        /// <summary>
        /// 주어진 Vector2의 각 축(x, y)에 대해 지정된 어림수 모드를 적용하여 Vector2Int로 반환합니다
        /// </summary>
        /// <param name="vector">어림수를 구할 Vector2입니다.</param>
        /// <param name="roundMode">어림수 모드입니다. 반올림, 내림, 올림 중 하나를 선택합니다.</param>
        /// <returns>각 축에 대해 어림수 모드가 적용된 Vector2Int를 반환합니다.</returns>
        public static Vector2Int RoundToInt2(this Vector3 vector, ERounds roundMode = ERounds.Round)
        {
            return new Vector2Int(
                vector.x.RoundToInt(roundMode),
                vector.y.RoundToInt(roundMode)
            );
        }



        /// <summary>
        /// 주어진 Vector2의 각 축(x, y)에 대해 지정된 어림수 모드를 적용하여, 참조로 전달된 벡터에 그 값을 저장합니다.
        /// </summary>
        /// <param name="vector">어림수를 구할 Vector2입니다. 이 값이 참조로 수정됩니다.</param>
        /// <param name="roundMode">어림수 모드입니다.</param>
        public static void SetRound(this ref Vector2 vector, ERounds roundMode = ERounds.Round)
        {
            vector.x = vector.x.Round(roundMode);
            vector.y = vector.y.Round(roundMode);
        }



        ///======================================================================================================================================================



        #endregion



        #region Vector3 어림수



        ///======================================================================================================================================================



        /// <summary>
        /// 주어진 Vector3의 각 축(x, y, z)을 특정 단위로 반올림, 올림 또는 내림하여 반환합니다.
        /// </summary>
        /// <param name="vector">어림수를 구할 Vector3입니다.</param>
        /// <param name="unit">단위입니다. 각 축이 이 단위의 배수로 어림수가 계산됩니다.</param>
        /// <param name="roundMode">어림수 모드입니다.</param>
        /// <returns>각 축이 지정된 단위와 어림수 모드에 따라 처리된 Vector3를 반환합니다.</returns>
        public static Vector3 Round_Unit(this Vector3 vector, float unit, ERounds roundMode = ERounds.Round)
        {
            return new Vector3(
                vector.x.Round_Unit(unit, roundMode),
                vector.y.Round_Unit(unit, roundMode),
                vector.z.Round_Unit(unit, roundMode)
            );
        }



        /// <summary>
        /// 주어진 Vector3의 각 축(x, y, z)을 특정 단위로 반올림, 올림 또는 내림하여, 참조로 전달된 벡터에 그 값을 저장합니다.
        /// </summary>
        /// <param name="vector">어림수를 구할 Vector3입니다. 이 값이 참조로 수정됩니다.</param>
        /// <param name="unit">단위입니다. 각 축이 이 단위의 배수로 어림수가 계산됩니다.</param>
        /// <param name="roundMode">어림수 모드입니다.</param>
        public static void SetRound_Unit(this ref Vector3 vector, float unit, ERounds roundMode = ERounds.Round)
        {
            vector.x = vector.x.Round_Unit(unit, roundMode);
            vector.y = vector.y.Round_Unit(unit, roundMode);
            vector.z = vector.z.Round_Unit(unit, roundMode);
        }



        /// <summary>
        /// 주어진 Vector3의 각 축(x, y, z)을 특정 그리드 단위(Vector3)로 반올림, 올림 또는 내림하여 반환합니다.
        /// </summary>
        /// <param name="vector">어림수를 구할 Vector3입니다.</param>
        /// <param name="unit">각 축이 이 단위의 배수로 어림수가 계산됩니다.</param>
        /// <param name="roundMode">어림수 모드입니다.</param>
        /// <returns>각 축이 지정된 단위와 어림수 모드에 따라 처리된 Vector3를 반환합니다.</returns>
        public static Vector3 Round_Unit(this Vector3 vector, Vector3 unit, ERounds roundMode = ERounds.Round)
        {
            return new Vector3(
                vector.x.Round_Unit(unit.x, roundMode),
                vector.y.Round_Unit(unit.y, roundMode),
                vector.z.Round_Unit(unit.z, roundMode)
            );
        }



        /// <summary>
        /// 주어진 Vector3의 각 축(x, y, z)을 특정 그리드 단위(Vector3)로 반올림, 올림 또는 내림하여, 참조로 전달된 벡터에 그 값을 저장합니다.
        /// </summary>
        /// <param name="vector">어림수를 구할 Vector3입니다. 이 값이 참조로 수정됩니다.</param>
        /// <param name="unit">각 축이 이 단위의 배수로 어림수가 계산됩니다.</param>
        /// <param name="roundMode">어림수 모드입니다.</param>
        public static void SetRound_Unit(this ref Vector3 vector, Vector3 unit, ERounds roundMode = ERounds.Round)
        {
            vector.x = vector.x.Round_Unit(unit.x, roundMode);
            vector.y = vector.y.Round_Unit(unit.y, roundMode);
            vector.z = vector.z.Round_Unit(unit.z, roundMode);
        }



        ///======================================================================================================================================================



        //? Vector3 Round, SetRound (기본 반올림 메서드)



        /// <summary>
        /// 주어진 Vector3의 각 축(x, y, z)에 대해 지정된 어림수 모드를 적용합니다.
        /// </summary>
        /// <param name="vector">어림수를 구할 Vector3입니다.</param>
        /// <param name="roundMode">어림수 모드입니다. 반올림, 내림, 올림 중 하나를 선택합니다.</param>
        /// <returns>각 축에 대해 어림수 모드가 적용된 Vector3를 반환합니다.</returns>
        public static Vector3 Round(this Vector3 vector, ERounds roundMode = ERounds.Round)
        {
            return new Vector3(
                vector.x.Round(roundMode),
                vector.y.Round(roundMode),
                vector.z.Round(roundMode)
            );
        }



        /// <summary>
        /// 주어진 Vector3의 각 축(x, y, z)에 대해 지정된 어림수 모드를 적용하여 Vector3Int로 반환합니다
        /// </summary>
        /// <param name="vector">어림수를 구할 Vector3입니다.</param>
        /// <param name="roundMode">어림수 모드입니다. 반올림, 내림, 올림 중 하나를 선택합니다.</param>
        /// <returns>각 축에 대해 어림수 모드가 적용된 Vector3Int를 반환합니다.</returns>
        public static Vector3Int RoundToInt3(this Vector3 vector, ERounds roundMode = ERounds.Round)
        {
            return new Vector3Int(
                vector.x.RoundToInt(roundMode),
                vector.y.RoundToInt(roundMode),
                vector.z.RoundToInt(roundMode)
            );
        }



        /// <summary>
        /// 주어진 Vector3의 각 축(x, y, z)에 대해 지정된 어림수 모드를 적용하여, 참조로 전달된 벡터에 그 값을 저장합니다.
        /// </summary>
        /// <param name="vector">어림수를 구할 Vector3입니다. 이 값이 참조로 수정됩니다.</param>
        /// <param name="roundMode">어림수 모드입니다.</param>
        public static void SetRound(this ref Vector3 vector, ERounds roundMode = ERounds.Round)
        {
            vector.x = vector.x.Round(roundMode);
            vector.y = vector.y.Round(roundMode);
            vector.z = vector.z.Round(roundMode);
        }



        ///======================================================================================================================================================



        #endregion



        #region Vector2Int 어림수



        ///======================================================================================================================================================



        /// <summary>
        /// 주어진 Vector2Int의 각 축(x, y)에 대해 지정된 어림수 모드를 적용합니다.
        /// </summary>
        /// <param name="vector">어림수를 구할 Vector2Int입니다.</param>
        /// <param name="roundMode">어림수 모드입니다. 반올림, 내림, 올림 중 하나를 선택합니다.</param>
        /// <returns>각 축에 대해 어림수 모드가 적용된 Vector2Int를 반환합니다.</returns>
        public static Vector2Int Round(this Vector2Int vector, ERounds roundMode = ERounds.Round)
        {
            return new Vector2Int(
                vector.x.Round(roundMode),
                vector.y.Round(roundMode)
            );
        }



        /// <summary>
        /// 주어진 Vector2Int의 각 축(x, y)에 대해 지정된 어림수 모드를 적용하여, 참조로 전달된 벡터에 그 값을 저장합니다.
        /// </summary>
        /// <param name="vector">어림수를 구할 Vector2Int입니다. 이 값이 참조로 수정됩니다.</param>
        /// <param name="roundMode">어림수 모드입니다.</param>
        public static void SetRound(this ref Vector2Int vector, ERounds roundMode = ERounds.Round)
        {
            vector.x = vector.x.Round(roundMode);
            vector.y = vector.y.Round(roundMode);
        }



        ///======================================================================================================================================================



        #endregion



        #region Vector3Int 어림수



        ///======================================================================================================================================================



        /// <summary>
        /// 주어진 Vector3Int의 각 축(x, y, z)에 대해 지정된 어림수 모드를 적용합니다.
        /// </summary>
        /// <param name="vector">어림수를 구할 Vector3Int입니다.</param>
        /// <param name="roundMode">어림수 모드입니다. 반올림, 내림, 올림 중 하나를 선택합니다.</param>
        /// <returns>각 축에 대해 어림수 모드가 적용된 Vector3Int를 반환합니다.</returns>
        public static Vector3Int Round(this Vector3Int vector, ERounds roundMode = ERounds.Round)
        {
            return new Vector3Int(
                vector.x.Round(roundMode),
                vector.y.Round(roundMode),
                vector.z.Round(roundMode)
            );
        }



        /// <summary>
        /// 주어진 Vector3Int의 각 축(x, y, z)에 대해 지정된 어림수 모드를 적용하여, 참조로 전달된 벡터에 그 값을 저장합니다.
        /// </summary>
        /// <param name="vector">어림수를 구할 Vector3Int입니다. 이 값이 참조로 수정됩니다.</param>
        /// <param name="roundMode">어림수 모드입니다.</param>
        public static void SetRound(this ref Vector3Int vector, ERounds roundMode = ERounds.Round)
        {
            vector.x = vector.x.Round(roundMode);
            vector.y = vector.y.Round(roundMode);
            vector.z = vector.z.Round(roundMode);
        }



        ///======================================================================================================================================================



        #endregion



        //? 정수 어림수 인터벌 반올림



        /// <summary>
        /// 주어진 값을 특정 인터벌(간격)에 맞추어 가장 가까운 배수로 반올림합니다.
        /// </summary>
        /// <param name="value">반올림할 값입니다.</param>
        /// <param name="interval">반올림 기준이 되는 인터벌(간격)입니다. (예: 1000, 100 등)</param>
        /// <param name="ignoreIfBelowInterval">값이 인터벌보다 작을 경우, true라면 원래 값을 반환합니다.</param>
        /// <returns>인터벌 기준으로 반올림된 값, 또는 원래 값</returns>
        public static int RoundToInterval(this int value, int interval, bool ignoreIfBelowInterval = false)
        {
            //! interval보다 작은 경우, 원래 값 반환 (옵션에 따라)
            if (ignoreIfBelowInterval && value < interval) return value;

            //! (float) 변환 후 Mathf.RoundToInt
            return Mathf.RoundToInt(value / (float)interval) * interval;
        }



        /// <summary>
        /// 주어진 값을 특정 인터벌(간격)에 맞추어 내림 처리합니다.
        /// </summary>
        /// <param name="value">내림할 값입니다.</param>
        /// <param name="interval">내림 기준이 되는 인터벌(간격)입니다. (예: 1000, 100 등)</param>
        /// <param name="ignoreIfBelowInterval">값이 인터벌보다 작을 경우, true라면 원래 값을 반환합니다.</param>
        /// <returns>인터벌 기준으로 내림된 값, 또는 원래 값</returns>
        public static int FloorToInterval(this int value, int interval, bool ignoreIfBelowInterval = false)
        {
            //! interval보다 작은 경우, 원래 값 반환 (옵션에 따라)
            if (ignoreIfBelowInterval && value < interval) return value;

            //! Mathf.FloorToInt로 내림 처리
            return Mathf.FloorToInt(value / (float)interval) * interval;
        }



        /// <summary>
        /// 주어진 값을 특정 인터벌(간격)에 맞추어 올림 처리합니다.
        /// </summary>
        /// <param name="value">올림할 값입니다.</param>
        /// <param name="interval">올림 기준이 되는 인터벌(간격)입니다. (예: 1000, 100 등)</param>
        /// <param name="ignoreIfBelowInterval">값이 인터벌보다 작을 경우, true라면 원래 값을 반환합니다.</param>
        /// <returns>인터벌 기준으로 올림된 값, 또는 원래 값</returns>
        public static int CeilToInterval(this int value, int interval, bool ignoreIfBelowInterval = false)
        {
            //! interval보다 작은 경우, 원래 값 반환 (옵션에 따라)
            if (ignoreIfBelowInterval && value < interval) return value;

            //! Mathf.CeilToInt로 올림 처리
            return Mathf.CeilToInt(value / (float)interval) * interval;
        }



        ///======================================================================================================================================================



        //? 홀수 짝수



        #region 홀수 짝수



        /// <summary>
        /// 정수가 짝수인지 여부를 반환합니다.
        /// </summary>
        /// <param name="var">확장 메서드가 적용될 정수</param>
        /// <returns>짝수이면 true, 홀수이면 false</returns>
        public static bool IsEven(this int var)
        {
            return Mathf.Abs(var) % 2 == 0;
        }




        /// <summary>
        /// 부동 소수점이 짝수인지 여부를 반환합니다.<br/>
        /// (어림수로 연산한 후 사용 권장)
        /// </summary>
        /// <param name="var">확장 메서드가 적용될 부동소수점</param>
        /// <param name="rounds">어림수를 연산한 후 적용</param>
        /// <returns>짝수이면 true, 홀수이면 false</returns>
        public static bool IsEven(this float var, ERounds? rounds = null)
        {
            return Mathf.Abs(rounds.HasValue ? var.Round(rounds.Value) : var) % 2 == 0;
        }



        /// <summary>
        /// 부동 소수점 수가 짝수인지 여부를 반환합니다. (소수점 버림)
        /// </summary>
        /// <param name="var">확장 메서드가 적용될 부동 소수점 수</param>
        /// <returns>짝수이면 true, 홀수이면 false</returns>
        public static bool IsEven_Truncate(this float var)
        {
            // 소수점을 무시하고 정수 부분만 확인합니다.
            return (Mathf.Abs((int)var)) % 2 == 0;
        }



        #endregion



        ///======================================================================================================================================================



        //? Clamp 제약



        #region Clamp 제약



        ///======================================================================================================================================================



        #region int 



        /// <summary>
        /// 현재 int 값을 최소값과 최대값 사이로 클램프하고 원본 변수를 업데이트합니다.
        /// </summary>
        public static int SetClamp(this ref int var, int min, int max)
        {
            return var = Mathf.Clamp(var, min, max);
        }

        /// <summary>
        /// 주어진 value를 최소값과 최대값 사이로 클램프하여 원본 변수에 저장합니다.
        /// </summary>
        public static int SetClamp(this ref int var, int value, int min, int max)
        {
            return var = Mathf.Clamp(value, min, max);
        }

        /// <summary>
        /// int 값을 최소값과 최대값 사이로 클램프한 결과를 반환합니다.
        /// </summary>
        public static int Clamped(this int value, int min, int max)
        {
            return Mathf.Clamp(value, min, max);
        }



        /// <summary>
        /// 현재 int 값을 0과 주어진 최대값 사이로 클램프하고 원본 변수를 업데이트합니다.
        /// </summary>
        public static int SetClamp0Max(this ref int var, int max)
        {
            return var = Mathf.Clamp(var, 0, max);
        }

        /// <summary>
        /// 주어진 value를 0과 주어진 최대값 사이로 클램프하여 원본 변수에 저장합니다.
        /// </summary>
        public static int SetClamp0Max(this ref int var, int value, int max)
        {
            return var = Mathf.Clamp(value, 0, max);
        }

        /// <summary>
        /// int 값을 0과 주어진 최대값 사이로 클램프한 결과를 반환합니다.
        /// </summary>
        public static int Clamped0Max(this int value, int max)
        {
            return Mathf.Clamp(value, 0, max);
        }



        /// <summary>
        /// 현재 int 값을 0과 int.MaxValue 사이로 클램프하고 원본 변수를 업데이트합니다.
        /// </summary>
        public static int SetClamp0(this ref int var)
        {
            return var = Mathf.Clamp(var, 0, int.MaxValue);
        }

        /// <summary>
        /// 주어진 value를 0과 int.MaxValue 사이로 클램프하여 원본 변수에 저장합니다.
        /// </summary>
        public static int SetClamp0(this ref int var, int value)
        {
            return var = Mathf.Clamp(value, 0, int.MaxValue);
        }

        /// <summary>
        /// int 값을 0과 int.MaxValue 사이로 클램프한 결과를 반환합니다.
        /// </summary>
        public static int Clamped0(this int value)
        {
            return Mathf.Clamp(value, 0, int.MaxValue);
        }



        /// <summary>
        /// 현재 int 값을 주어진 최소값과 int.MaxValue 사이로 클램프하고 원본 변수를 업데이트합니다.
        /// </summary>
        public static int SetClampMin(this ref int var, int min)
        {
            return var = Mathf.Clamp(var, min, int.MaxValue);
        }

        /// <summary>
        /// 주어진 value를 주어진 최소값과 int.MaxValue 사이로 클램프하여 원본 변수에 저장합니다.
        /// </summary>
        public static int SetClampMin(this ref int var, int value, int min)
        {
            return var = Mathf.Clamp(value, min, int.MaxValue);
        }
        /// <summary>
        /// int 값을 주어진 최소값과 int.MaxValue 사이로 클램프한 결과를 반환합니다.
        /// </summary>
        public static int ClampedMin(this int value, int min)
        {
            return Mathf.Clamp(value, min, int.MaxValue);
        }



        /// <summary>
        /// 현재 int 값을 -int.MaxValue와 주어진 최대값 사이로 클램프하고 원본 변수를 업데이트합니다.
        /// </summary>
        public static int SetClampMax(this ref int var, int max)
        {
            return var = Mathf.Clamp(var, -int.MaxValue, max);
        }

        /// <summary>
        /// 주어진 value를 -int.MaxValue와 주어진 최대값 사이로 클램프하여 원본 변수에 저장합니다.
        /// </summary>
        public static int SetClampMax(this ref int var, int value, int max)
        {
            return var = Mathf.Clamp(value, -int.MaxValue, max);
        }

        /// <summary>
        /// int 값을 -int.MaxValue와 주어진 최대값 사이로 클램프한 결과를 반환합니다.
        /// </summary>
        public static int ClampedMax(this int value, int max)
        {
            return Mathf.Clamp(value, -int.MaxValue, max);
        }



        #endregion



        ///======================================================================================================================================================



        #region float 



        /// <summary>
        /// 현재 float 값을 최소값과 최대값 사이로 클램프하고 원본 변수를 업데이트합니다.
        /// </summary>
        public static float SetClamp(this ref float var, float min, float max)
        {
            return var = Mathf.Clamp(var, min, max);
        }



        /// <summary>
        /// 주어진 value를 최소값과 최대값 사이로 클램프하여 원본 변수에 저장합니다.
        /// </summary>
        public static float SetClamp(this ref float var, float value, float min, float max)
        {
            return var = Mathf.Clamp(value, min, max);
        }



        /// <summary>
        /// float 값을 최소값과 최대값 사이로 클램프한 결과를 반환합니다.
        /// </summary>
        public static float Clamped(this float value, float min, float max)
        {
            return Mathf.Clamp(value, min, max);
        }



        /// <summary>
        /// 현재 float 값을 0과 1 사이로 클램프하고 원본 변수를 업데이트합니다.
        /// </summary>
        public static float SetClamp01(this ref float var)
        {
            return var = Mathf.Clamp01(var);
        }



        /// <summary>
        /// float 값을 0과 1 사이로 클램프한 결과를 반환합니다.
        /// </summary>
        public static float Clamped01(this float value)
        {
            return Mathf.Clamp01(value);
        }



        /// <summary>
        /// 현재 float 값을 0과 100 사이로 클램프하고 원본 변수를 업데이트합니다.
        /// </summary>
        public static float SetClamp0100(this ref float var)
        {
            return var = Mathf.Clamp(var, 0, 100f);
        }



        /// <summary>
        /// float 값을 0과 100 사이로 클램프한 결과를 반환합니다.
        /// </summary>
        public static float Clamped0100(this float value)
        {
            return Mathf.Clamp(value, 0, 100f);
        }



        /// <summary>
        /// 현재 float 값을 0과 주어진 최대값 사이로 클램프하고 원본 변수를 업데이트합니다.
        /// </summary>
        public static float SetClamp0Max(this ref float var, float max)
        {
            return var = Mathf.Clamp(var, 0, max);
        }



        /// <summary>
        /// 주어진 value를 0과 주어진 최대값 사이로 클램프하여 원본 변수에 저장합니다.
        /// </summary>
        public static float SetClamp0Max(this ref float var, float value, float max)
        {
            return var = Mathf.Clamp(value, 0, max);
        }



        /// <summary>
        /// float 값을 0과 주어진 최대값 사이로 클램프한 결과를 반환합니다.
        /// </summary>
        public static float Clamped0Max(this float value, float max)
        {
            return Mathf.Clamp(value, 0, max);
        }



        /// <summary>
        /// 현재 float 값을 0과 Mathf.Infinity 사이로 클램프하고 원본 변수를 업데이트합니다.
        /// </summary>
        public static float SetClamp0(this ref float var)
        {
            return var = Mathf.Clamp(var, 0, Mathf.Infinity);
        }



        /// <summary>
        /// 주어진 value를 0과 Mathf.Infinity 사이로 클램프하여 원본 변수에 저장합니다.
        /// </summary>
        public static float SetClamp0(this ref float var, float value)
        {
            return var = Mathf.Clamp(value, 0, Mathf.Infinity);
        }



        /// <summary>
        /// float 값을 0과 Mathf.Infinity 사이로 클램프한 결과를 반환합니다.
        /// </summary>
        public static float Clamped0(this float value)
        {
            return Mathf.Clamp(value, 0, Mathf.Infinity);
        }



        /// <summary>
        /// 현재 float 값을 주어진 최소값과 Mathf.Infinity 사이로 클램프하고 원본 변수를 업데이트합니다.
        /// </summary>
        public static float SetClampMin(this ref float var, float min)
        {
            return var = Mathf.Clamp(var, min, Mathf.Infinity);
        }



        /// <summary>
        /// 주어진 int value를 주어진 최소값과 Mathf.Infinity 사이로 클램프하여 float 형으로 원본 변수에 저장합니다.
        /// </summary>
        public static float SetClampMin(this ref float var, int value, float min)
        {
            return var = Mathf.Clamp(value, min, Mathf.Infinity);
        }



        /// <summary>
        /// float 값을 주어진 최소값과 Mathf.Infinity 사이로 클램프한 결과를 반환합니다.
        /// </summary>
        public static float ClampedMin(this float value, float min)
        {
            return Mathf.Clamp(value, min, Mathf.Infinity);
        }



        /// <summary>
        /// 현재 float 값을 Mathf.NegativeInfinity와 주어진 최대값 사이로 클램프하고 원본 변수를 업데이트합니다.
        /// </summary>
        public static float SetClampMax(this ref float var, float max)
        {
            return var = Mathf.Clamp(var, Mathf.NegativeInfinity, max);
        }



        /// <summary>
        /// 주어진 int value를 Mathf.NegativeInfinity와 주어진 최대값 사이로 클램프하여 float 형으로 원본 변수에 저장합니다.
        /// </summary>
        public static float SetClampMax(this ref float var, int value, float max)
        {
            return var = Mathf.Clamp(value, Mathf.NegativeInfinity, max);
        }



        /// <summary>
        /// float 값을 Mathf.NegativeInfinity와 주어진 최대값 사이로 클램프한 결과를 반환합니다.
        /// </summary>
        public static float ClampedMax(this float value, float max)
        {
            return Mathf.Clamp(value, Mathf.NegativeInfinity, max);
        }



        /// <summary>
        /// 현재 float 각도 값이 양수이고 360보다 크면 360을 빼고, 음수이고 -360보다 작으면 360을 더해 보정한 후 원본 변수를 업데이트합니다.
        /// </summary>
        public static float SetMax360(this ref float var)
        {
            if (var > 0 && var > 360)
            {
                return var -= 360;
            }
            if (var < 0 && var < -360)
            {
                return var += 360;
            }
            return var;
        }



        /// <summary>
        /// float 각도 값을 360보다 크면 360을 빼고, -360보다 작으면 360을 더해 보정한 결과를 반환합니다.
        /// </summary>
        public static float Max360(this float angle)
        {
            if (angle > 0 && angle > 360)
            {
                return angle - 360;
            }
            if (angle < 0 && angle < -360)
            {
                return angle + 360;
            }
            return angle;
        }



        #endregion



        ///======================================================================================================================================================



        #region Vector2



        /// <summary>
        /// 현재 Vector2 값을 각 요소별로 최소값과 최대값 사이로 클램프하고 원본 변수를 업데이트합니다.
        /// </summary>
        /// <param name="vector">클램프할 대상 Vector2 (ref)</param>
        /// <param name="min">각 요소의 최소값</param>
        /// <param name="max">각 요소의 최대값</param>
        /// <returns>클램프된 Vector2 값</returns>
        public static Vector2 SetClamp(this ref Vector2 vector, Vector2 min, Vector2 max)
        {
            vector = new Vector2(
                Mathf.Clamp(vector.x, min.x, max.x),
                Mathf.Clamp(vector.y, min.y, max.y)
            );
            return vector;
        }

        /// <summary>
        /// 주어진 Vector2 value를 각 요소별로 최소값과 최대값 사이로 클램프하여 원본 변수에 저장합니다.
        /// </summary>
        /// <param name="vector">결과를 저장할 Vector2 (ref)</param>
        /// <param name="value">클램프할 값</param>
        /// <param name="min">각 요소의 최소값</param>
        /// <param name="max">각 요소의 최대값</param>
        /// <returns>클램프된 Vector2 값</returns>
        public static Vector2 SetClamp(this ref Vector2 vector, Vector2 value, Vector2 min, Vector2 max)
        {
            vector = new Vector2(
                Mathf.Clamp(value.x, min.x, max.x),
                Mathf.Clamp(value.y, min.y, max.y)
            );
            return vector;
        }

        /// <summary>
        /// Vector2 값을 각 요소별로 최소값과 최대값 사이로 클램프한 결과를 반환합니다.
        /// </summary>
        /// <param name="vector">클램프할 Vector2 값</param>
        /// <param name="min">각 요소의 최소값</param>
        /// <param name="max">각 요소의 최대값</param>
        /// <returns>클램프된 Vector2 값</returns>
        public static Vector2 Clamped(this Vector2 vector, Vector2 min, Vector2 max)
        {
            return new Vector2(
                Mathf.Clamp(vector.x, min.x, max.x),
                Mathf.Clamp(vector.y, min.y, max.y)
            );
        }



        /// <summary>
        /// 현재 Vector2의 각 요소를 주어진 최소값과 Mathf.Infinity 사이로 클램프하고 원본 변수를 업데이트합니다.
        /// </summary>
        public static Vector2 SetClampMin(this ref Vector2 vector, float min)
        {
            return vector = new Vector2(Mathf.Clamp(vector.x, min, Mathf.Infinity),
                                     Mathf.Clamp(vector.y, min, Mathf.Infinity));
        }

        /// <summary>
        /// Vector2의 각 요소를 주어진 최소값과 Mathf.Infinity 사이로 클램프한 결과를 반환합니다.
        /// </summary>
        public static Vector2 ClampedMin(this Vector2 vector, float min)
        {
            return new Vector2(Mathf.Clamp(vector.x, min, Mathf.Infinity),
                               Mathf.Clamp(vector.y, min, Mathf.Infinity));
        }



        /// <summary>
        /// 주어진 Vector2 value의 각 요소를 주어진 최소값과 Mathf.Infinity 사이로 클램프하여 원본 변수에 저장합니다.
        /// </summary>
        public static Vector2 SetClampMin(this ref Vector2 vector, Vector2 value, float min)
        {
            return vector = new Vector2(Mathf.Clamp(value.x, min, Mathf.Infinity),
                                     Mathf.Clamp(value.y, min, Mathf.Infinity));
        }

        /// <summary>
        /// 현재 Vector2의 각 요소를 Mathf.NegativeInfinity와 주어진 최대값 사이로 클램프하고 원본 변수를 업데이트합니다.
        /// </summary>
        public static Vector2 SetClampMax(this ref Vector2 vector, float max)
        {
            return vector = new Vector2(Mathf.Clamp(vector.x, Mathf.NegativeInfinity, max),
                                     Mathf.Clamp(vector.y, Mathf.NegativeInfinity, max));
        }

        /// <summary>
        /// Vector2의 각 요소를 Mathf.NegativeInfinity와 주어진 최대값 사이로 클램프한 결과를 반환합니다.
        /// </summary>
        public static Vector2 ClampedMax(this Vector2 vector, float max)
        {
            return new Vector2(Mathf.Clamp(vector.x, Mathf.NegativeInfinity, max),
                               Mathf.Clamp(vector.y, Mathf.NegativeInfinity, max));
        }



        #endregion



        ///======================================================================================================================================================



        #region Vector3



        /// <summary>
        /// 현재 Vector3 값을 각 요소별로 최소값과 최대값 사이로 클램프하고 원본 변수를 업데이트합니다.
        /// </summary>
        /// <param name="vector">클램프할 대상 Vector3 (ref)</param>
        /// <param name="min">각 요소의 최소값</param>
        /// <param name="max">각 요소의 최대값</param>
        /// <returns>클램프된 Vector3 값</returns>
        public static Vector3 SetClamp(this ref Vector3 vector, Vector3 min, Vector3 max)
        {
            vector = new Vector3(
                Mathf.Clamp(vector.x, min.x, max.x),
                Mathf.Clamp(vector.y, min.y, max.y),
                Mathf.Clamp(vector.z, min.z, max.z)
            );
            return vector;
        }

        /// <summary>
        /// 주어진 Vector3 value를 각 요소별로 최소값과 최대값 사이로 클램프하여 원본 변수에 저장합니다.
        /// </summary>
        /// <param name="vector">결과를 저장할 Vector3 (ref)</param>
        /// <param name="value">클램프할 값</param>
        /// <param name="min">각 요소의 최소값</param>
        /// <param name="max">각 요소의 최대값</param>
        /// <returns>클램프된 Vector3 값</returns>
        public static Vector3 SetClamp(this ref Vector3 vector, Vector3 value, Vector3 min, Vector3 max)
        {
            vector = new Vector3(
                Mathf.Clamp(value.x, min.x, max.x),
                Mathf.Clamp(value.y, min.y, max.y),
                Mathf.Clamp(value.z, min.z, max.z)
            );
            return vector;
        }

        /// <summary>
        /// Vector3 값을 각 요소별로 최소값과 최대값 사이로 클램프한 결과를 반환합니다.
        /// </summary>
        /// <param name="vector">클램프할 Vector3 값</param>
        /// <param name="min">각 요소의 최소값</param>
        /// <param name="max">각 요소의 최대값</param>
        /// <returns>클램프된 Vector3 값</returns>
        public static Vector3 Clamped(this Vector3 vector, Vector3 min, Vector3 max)
        {
            return new Vector3(
                Mathf.Clamp(vector.x, min.x, max.x),
                Mathf.Clamp(vector.y, min.y, max.y),
                Mathf.Clamp(vector.z, min.z, max.z)
            );
        }



        /// <summary>
        /// 현재 Vector3의 각 요소를 주어진 최소값과 Mathf.Infinity 사이로 클램프하고 원본 변수를 업데이트합니다.
        /// </summary>
        public static Vector3 SetClampMin(this ref Vector3 vector, float min)
        {
            return vector = new Vector3(Mathf.Clamp(vector.x, min, Mathf.Infinity),
                                     Mathf.Clamp(vector.y, min, Mathf.Infinity),
                                     Mathf.Clamp(vector.z, min, Mathf.Infinity));
        }

        /// <summary>
        /// Vector3의 각 요소를 주어진 최소값과 Mathf.Infinity 사이로 클램프한 결과를 반환합니다.
        /// </summary>
        public static Vector3 ClampedMin(this Vector3 vector, float min)
        {
            return new Vector3(Mathf.Clamp(vector.x, min, Mathf.Infinity),
                               Mathf.Clamp(vector.y, min, Mathf.Infinity),
                               Mathf.Clamp(vector.z, min, Mathf.Infinity));
        }



        /// <summary>
        /// 현재 Vector3의 각 요소를 Mathf.NegativeInfinity와 주어진 최대값 사이로 클램프하고 원본 변수를 업데이트합니다.
        /// </summary>
        public static Vector3 SetClampMax(this ref Vector3 vector, float max)
        {
            return vector = new Vector3(Mathf.Clamp(vector.x, Mathf.NegativeInfinity, max),
                                     Mathf.Clamp(vector.y, Mathf.NegativeInfinity, max),
                                     Mathf.Clamp(vector.z, Mathf.NegativeInfinity, max));
        }

        /// <summary>
        /// Vector3의 각 요소를 Mathf.NegativeInfinity와 주어진 최대값 사이로 클램프한 결과를 반환합니다.
        /// </summary>
        public static Vector3 ClampedMax(this Vector3 vector, float max)
        {
            return new Vector3(Mathf.Clamp(vector.x, Mathf.NegativeInfinity, max),
                               Mathf.Clamp(vector.y, Mathf.NegativeInfinity, max),
                               Mathf.Clamp(vector.z, Mathf.NegativeInfinity, max));
        }



        #endregion



        ///======================================================================================================================================================



        #region Vector2Int 



        /// <summary>
        /// 현재 Vector2Int 값을 각 요소별로 최소값과 최대값 사이로 클램프하고 원본 변수를 업데이트합니다.
        /// </summary>
        /// <param name="vector">클램프할 대상 Vector2Int (ref)</param>
        /// <param name="min">각 요소의 최소값</param>
        /// <param name="max">각 요소의 최대값</param>
        /// <returns>클램프된 Vector2Int 값</returns>
        public static Vector2Int SetClamp(this ref Vector2Int vector, Vector2Int min, Vector2Int max)
        {
            vector = new Vector2Int(
                Mathf.Clamp(vector.x, min.x, max.x),
                Mathf.Clamp(vector.y, min.y, max.y)
            );
            return vector;
        }

        /// <summary>
        /// 주어진 Vector2Int value를 각 요소별로 최소값과 최대값 사이로 클램프하여 원본 변수에 저장합니다.
        /// </summary>
        /// <param name="vector">결과를 저장할 Vector2Int (ref)</param>
        /// <param name="value">클램프할 값</param>
        /// <param name="min">각 요소의 최소값</param>
        /// <param name="max">각 요소의 최대값</param>
        /// <returns>클램프된 Vector2Int 값</returns>
        public static Vector2Int SetClamp(this ref Vector2Int vector, Vector2Int value, Vector2Int min, Vector2Int max)
        {
            vector = new Vector2Int(
                Mathf.Clamp(value.x, min.x, max.x),
                Mathf.Clamp(value.y, min.y, max.y)
            );
            return vector;
        }

        /// <summary>
        /// Vector2Int 값을 각 요소별로 최소값과 최대값 사이로 클램프한 결과를 반환합니다.
        /// </summary>
        /// <param name="vector">클램프할 Vector2Int 값</param>
        /// <param name="min">각 요소의 최소값</param>
        /// <param name="max">각 요소의 최대값</param>
        /// <returns>클램프된 Vector2Int 값</returns>
        public static Vector2Int Clamped(this Vector2Int vector, Vector2Int min, Vector2Int max)
        {
            return new Vector2Int(
                Mathf.Clamp(vector.x, min.x, max.x),
                Mathf.Clamp(vector.y, min.y, max.y)
            );
        }



        /// <summary>
        /// 현재 Vector2Int의 각 요소를 주어진 최소값과 int.MaxValue 사이로 클램프하고 원본 변수를 업데이트합니다.
        /// </summary>
        public static Vector2Int SetClampMin(this ref Vector2Int vector, int min)
        {
            return vector = new Vector2Int(Mathf.Clamp(vector.x, min, int.MaxValue),
                                        Mathf.Clamp(vector.y, min, int.MaxValue));
        }

        /// <summary>
        /// Vector2Int의 각 요소를 주어진 최소값과 int.MaxValue 사이로 클램프한 결과를 반환합니다.
        /// </summary>
        public static Vector2Int ClampedMin(this Vector2Int vector, int min)
        {
            return new Vector2Int(Mathf.Clamp(vector.x, min, int.MaxValue),
                                  Mathf.Clamp(vector.y, min, int.MaxValue));
        }



        /// <summary>
        /// 현재 Vector2Int의 각 요소를 -int.MaxValue와 주어진 최대값 사이로 클램프하고 원본 변수를 업데이트합니다.
        /// </summary>
        public static Vector2Int SetClampMax(this ref Vector2Int vector, int max)
        {
            return vector = new Vector2Int(Mathf.Clamp(vector.x, -int.MaxValue, max),
                                        Mathf.Clamp(vector.y, -int.MaxValue, max));
        }

        /// <summary>
        /// Vector2Int의 각 요소를 -int.MaxValue와 주어진 최대값 사이로 클램프한 결과를 반환합니다.
        /// </summary>
        public static Vector2Int ClampedMax(this Vector2Int vector, int max)
        {
            return new Vector2Int(Mathf.Clamp(vector.x, -int.MaxValue, max),
                                  Mathf.Clamp(vector.y, -int.MaxValue, max));
        }



        #endregion



        ///======================================================================================================================================================



        #region Vector3Int



        /// <summary>
        /// 현재 Vector3Int 값을 각 요소별로 최소값과 최대값 사이로 클램프하고 원본 변수를 업데이트합니다.
        /// </summary>
        /// <param name="vector">클램프할 대상 Vector3Int (ref)</param>
        /// <param name="min">각 요소의 최소값</param>
        /// <param name="max">각 요소의 최대값</param>
        /// <returns>클램프된 Vector3Int 값</returns>
        public static Vector3Int SetClamp(this ref Vector3Int vector, Vector3Int min, Vector3Int max)
        {
            vector = new Vector3Int(
                Mathf.Clamp(vector.x, min.x, max.x),
                Mathf.Clamp(vector.y, min.y, max.y),
                Mathf.Clamp(vector.z, min.z, max.z)
            );
            return vector;
        }

        /// <summary>
        /// 주어진 Vector3Int value를 각 요소별로 최소값과 최대값 사이로 클램프하여 원본 변수에 저장합니다.
        /// </summary>
        /// <param name="vector">결과를 저장할 Vector3Int (ref)</param>
        /// <param name="value">클램프할 값</param>
        /// <param name="min">각 요소의 최소값</param>
        /// <param name="max">각 요소의 최대값</param>
        /// <returns>클램프된 Vector3Int 값</returns>
        public static Vector3Int SetClamp(this ref Vector3Int vector, Vector3Int value, Vector3Int min, Vector3Int max)
        {
            vector = new Vector3Int(
                Mathf.Clamp(value.x, min.x, max.x),
                Mathf.Clamp(value.y, min.y, max.y),
                Mathf.Clamp(value.z, min.z, max.z)
            );
            return vector;
        }

        /// <summary>
        /// Vector3Int 값을 각 요소별로 최소값과 최대값 사이로 클램프한 결과를 반환합니다.
        /// </summary>
        /// <param name="vector">클램프할 Vector3Int 값</param>
        /// <param name="min">각 요소의 최소값</param>
        /// <param name="max">각 요소의 최대값</param>
        /// <returns>클램프된 Vector3Int 값</returns>
        public static Vector3Int Clamped(this Vector3Int vector, Vector3Int min, Vector3Int max)
        {
            return new Vector3Int(
                Mathf.Clamp(vector.x, min.x, max.x),
                Mathf.Clamp(vector.y, min.y, max.y),
                Mathf.Clamp(vector.z, min.z, max.z)
            );
        }



        /// <summary>
        /// 현재 Vector3Int의 각 요소를 주어진 최소값과 int.MaxValue 사이로 클램프하고 원본 변수를 업데이트합니다.
        /// </summary>
        public static Vector3Int SetClampMin(this ref Vector3Int vector, int min)
        {
            return vector = new Vector3Int(Mathf.Clamp(vector.x, min, int.MaxValue),
                                        Mathf.Clamp(vector.y, min, int.MaxValue),
                                        Mathf.Clamp(vector.z, min, int.MaxValue));
        }

        /// <summary>
        /// Vector3Int의 각 요소를 주어진 최소값과 int.MaxValue 사이로 클램프한 결과를 반환합니다.
        /// </summary>
        public static Vector3Int ClampedMin(this Vector3Int vector, int min)
        {
            return new Vector3Int(Mathf.Clamp(vector.x, min, int.MaxValue),
                                  Mathf.Clamp(vector.y, min, int.MaxValue),
                                  Mathf.Clamp(vector.z, min, int.MaxValue));
        }



        /// <summary>
        /// 현재 Vector3Int의 각 요소를 -int.MaxValue와 주어진 최대값 사이로 클램프하고 원본 변수를 업데이트합니다.
        /// </summary>
        public static Vector3Int SetClampMax(this ref Vector3Int vector, int max)
        {
            return vector = new Vector3Int(Mathf.Clamp(vector.x, -int.MaxValue, max),
                                        Mathf.Clamp(vector.y, -int.MaxValue, max),
                                        Mathf.Clamp(vector.z, -int.MaxValue, max));
        }

        /// <summary>
        /// Vector3Int의 각 요소를 -int.MaxValue와 주어진 최대값 사이로 클램프한 결과를 반환합니다.
        /// </summary>
        public static Vector3Int ClampedMax(this Vector3Int vector, int max)
        {
            return new Vector3Int(Mathf.Clamp(vector.x, -int.MaxValue, max),
                                  Mathf.Clamp(vector.y, -int.MaxValue, max),
                                  Mathf.Clamp(vector.z, -int.MaxValue, max));
        }



        #endregion 



        ///======================================================================================================================================================


        #endregion



        #region Legacy Clamp


        ///// <summary>[int] 클램프</summary>
        ///// <param name="min">최소값</param>
        ///// <param name="max">최대값</param>
        //public static int SetClamp(this ref int var, int min, int max)
        //{
        //    return var = Mathf.Clamp(var, min, max);
        //}



        //public static int SetClamp(this ref int var, int value, int min, int max)
        //{
        //    return var = Mathf.Clamp(value, min, max);
        //}



        ///// <summary>[float] 클램프</summary>
        ///// <param name="min">최소값</param>
        ///// <param name="max">최대값</param>
        //public static float SetClamp(this ref float var, float min, float max)
        //{
        //    return var = Mathf.Clamp(var, min, max);
        //}



        //public static float SetClamp(this ref float var, float value, float min, float max)
        //{
        //    return var = Mathf.Clamp(value, min, max);
        //}



        ///// <summary>[float] 클램프, 최소값 : 0, 최대값 : 1 고정</summary>
        //public static float SetClamp01(this ref float var)
        //{
        //    return var = Mathf.Clamp01(var);
        //}



        ///// <summary>[float] 클램프, 최소값 : 0, 최대값 : 100 고정</summary>
        //public static float SetClamp0100(this ref float var)
        //{
        //    return var = Mathf.Clamp(var, 0, 100f);
        //}



        ///// <summary>[int] 클램프, 최소값 : 0 , 최대값 정하기</summary>
        ///// <param name="max">최대값</param>
        //public static int SetClamp0Max(this ref int var, int max)
        //{
        //    return var = Mathf.Clamp(var, 0, max);
        //}



        ///// <summary>[float] 클램프, 최소값 : 0 , 최대값 정하기</summary>
        ///// <param name="max">최대값</param>
        //public static float SetClamp0Max(this ref float var, float max)
        //{
        //    return var = Mathf.Clamp(var, 0, max);
        //}



        ///// <summary>[int] 값을 받아와 클램프, 최소값 : 0 , 최대값 정하기</summary>
        ///// <param name="max">최대값</param>
        //public static int SetClamp0Max(this ref int var, int value, int max)
        //{
        //    return var = Mathf.Clamp(value, 0, max);
        //}



        ///// <summary>[float] 값을 받아와 클램프, 최소값 : 0 , 최대값 정하기</summary>
        ///// <param name="max">최대값</param>
        //public static float SetClamp0Max(this ref float var, float value, float max)
        //{
        //    return var = Mathf.Clamp(value, 0, max);
        //}



        ///// <summary>[int] 클램프, 최소값 : 0, 최대값 : 무한</summary>
        //public static int SetClamp0(this ref int var)
        //{
        //    return var = Mathf.Clamp(var, 0, int.MaxValue);
        //}



        ///// <summary>[float] 클램프, 최소값 : 0, 최대값을 : 무한</summary>
        //public static float SetClamp0(this ref float var)
        //{
        //    return var = Mathf.Clamp(var, 0, Mathf.Infinity);
        //}



        ///// <summary>[int] 값을 받아와 클램프, 최소값 : 0, 최대값 : 무한</summary>
        //public static int SetClamp0(this ref int var, int value)
        //{
        //    return var = Mathf.Clamp(value, 0, int.MaxValue);
        //}



        ///// <summary>[float] 값을 받아와 클램프, 최소값 : 0, 최대값을 : 무한</summary>
        //public static float SetClamp0(this ref float var, float value)
        //{
        //    return var = Mathf.Clamp(value, 0, Mathf.Infinity);
        //}



        //public static int SetClampMin(this ref int var, int min)
        //{
        //    return var = Mathf.Clamp(var, min, int.MaxValue);
        //}

        //public static float SetClampMin(this ref float var, float min)
        //{
        //    return var = Mathf.Clamp(var, min, Mathf.Infinity);
        //}
        //public static int SetClampMin(this ref int var, int value, int min)
        //{
        //    return var = Mathf.Clamp(value, min, int.MaxValue);
        //}

        //public static float SetClampMin(this ref float var, int value, float min)
        //{
        //    return var = Mathf.Clamp(value, min, Mathf.Infinity);
        //}

        //public static int SetClampMax(this ref int var, int max)
        //{
        //    return var = Mathf.Clamp(var, -int.MaxValue, max);
        //}

        //public static float SetClampMax(this ref float var, float max)
        //{
        //    return var = Mathf.Clamp(var, Mathf.NegativeInfinity, max);
        //}

        //public static int SetClampMax(this ref int var, int value, int max)
        //{
        //    return var = Mathf.Clamp(value, -int.MaxValue, max);
        //}

        //public static float SetClampMax(this ref float var, int value, float max)
        //{
        //    return var = Mathf.Clamp(value, Mathf.NegativeInfinity, max);
        //}


        //public static float SetMax360(this ref float var)
        //{
        //    //양수 + 360보다크면 -360
        //    if (var > 0 && var > 360)
        //    {
        //        return var -= 360;
        //    }
        //    //음수 + -360보다작으면 +360
        //    if (var < 0 && var < -360)
        //    {
        //        return var += 360;
        //    }

        //    return var;
        //}


        //public static Vector2 SetClampMin(this ref Vector2 var, float min)
        //{
        //    return var = new Vector2(Mathf.Clamp(var.x, min, Mathf.Infinity), Mathf.Clamp(var.y, min, Mathf.Infinity));
        //}

        //public static Vector3 SetClampMin(this ref Vector3 var, float min)
        //{
        //    return var = new Vector3(Mathf.Clamp(var.x, min, Mathf.Infinity), Mathf.Clamp(var.y, min, Mathf.Infinity), Mathf.Clamp(var.z, min, Mathf.Infinity));
        //}

        //public static Vector2Int SetClampMin(this ref Vector2Int var, int min)
        //{
        //    return var = new Vector2Int(Mathf.Clamp(var.x, min, int.MaxValue), Mathf.Clamp(var.y, min, int.MaxValue));
        //}

        //public static Vector3Int SetClampMin(this ref Vector3Int var, int min)
        //{
        //    return var = new Vector3Int(Mathf.Clamp(var.x, min, int.MaxValue), Mathf.Clamp(var.y, min, int.MaxValue), Mathf.Clamp(var.z, min, int.MaxValue));
        //}


        //public static Vector2 SetClampMin(this ref Vector2 var, Vector2 value, float min)
        //{
        //    return var = new Vector2(Mathf.Clamp(value.x, min, Mathf.Infinity), Mathf.Clamp(value.y, min, Mathf.Infinity));
        //}

        //public static Vector3 SetClampMin(this ref Vector3 var, Vector3 value, float min)
        //{
        //    return var = new Vector3(Mathf.Clamp(value.x, min, Mathf.Infinity), Mathf.Clamp(value.y, min, Mathf.Infinity), Mathf.Clamp(value.z, min, Mathf.Infinity));
        //}

        //public static Vector2Int SetClampMin(this ref Vector2Int var, Vector2Int value, int min)
        //{
        //    return var = new Vector2Int(Mathf.Clamp(value.x, min, int.MaxValue), Mathf.Clamp(value.y, min, int.MaxValue));
        //}

        //public static Vector3Int SetClampMin(this ref Vector3Int var, Vector3Int value, int min)
        //{
        //    return var = new Vector3Int(Mathf.Clamp(value.x, min, int.MaxValue), Mathf.Clamp(value.y, min, int.MaxValue), Mathf.Clamp(value.z, min, int.MaxValue));
        //}




        //public static Vector2 SetClampMax(this ref Vector2 var, float max)
        //{
        //    return var = new Vector2(Mathf.Clamp(var.x, Mathf.NegativeInfinity, max), Mathf.Clamp(var.y, Mathf.NegativeInfinity, max));
        //}

        //public static Vector3 SetClampMax(this ref Vector3 var, float max)
        //{
        //    return var = new Vector3(Mathf.Clamp(var.x, Mathf.NegativeInfinity, max), Mathf.Clamp(var.y, Mathf.NegativeInfinity, max), Mathf.Clamp(var.z, Mathf.NegativeInfinity, max));
        //}

        //public static Vector2Int SetClampMax(this ref Vector2Int var, int max)
        //{
        //    return var = new Vector2Int(Mathf.Clamp(var.x, -int.MaxValue, max), Mathf.Clamp(var.y, -int.MaxValue, max));
        //}

        //public static Vector3Int SetClampMax(this ref Vector3Int var, int max)
        //{
        //    return var = new Vector3Int(Mathf.Clamp(var.x, -int.MaxValue, max), Mathf.Clamp(var.y, -int.MaxValue, max), Mathf.Clamp(var.z, -int.MaxValue, max));
        //}


        //public static Vector2 SetClampMax(this ref Vector2 var, Vector2 value, float max)
        //{
        //    return var = new Vector2(Mathf.Clamp(value.x, Mathf.NegativeInfinity, max), Mathf.Clamp(value.y, Mathf.NegativeInfinity, max));
        //}

        //public static Vector3 SetClampMax(this ref Vector3 var, Vector3 value, float max)
        //{
        //    return var = new Vector3(Mathf.Clamp(value.x, Mathf.NegativeInfinity, max), Mathf.Clamp(value.y, Mathf.NegativeInfinity, max), Mathf.Clamp(value.z, Mathf.NegativeInfinity, max));
        //}

        //public static Vector2Int SetClampMax(this ref Vector2Int var, Vector2Int value, int max)
        //{
        //    return var = new Vector2Int(Mathf.Clamp(value.x, -int.MaxValue, max), Mathf.Clamp(value.y, -int.MaxValue, max));
        //}

        //public static Vector3Int SetClampMax(this ref Vector3Int var, Vector3Int value, int max)
        //{
        //    return var = new Vector3Int(Mathf.Clamp(value.x, -int.MaxValue, max), Mathf.Clamp(value.y, -int.MaxValue, max), Mathf.Clamp(value.z, -int.MaxValue, max));
        //}



        #endregion



        ///======================================================================================================================================================
    }
}