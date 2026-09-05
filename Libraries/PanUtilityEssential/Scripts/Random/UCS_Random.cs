using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;



//? 커스텀 랜덤이 정리되어있는 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    /// <summary>
    /// 직접 만든 커스텀 랜덤 보유 여부
    /// </summary>
    public interface IHaveCustomRandom
    {
        CustomRandom GetRandom { get; }
    }



    /// <summary>
    /// 직접 만든 커스텀 랜덤 인터페이스
    /// </summary>
    public interface ICustomRandom
    {
        /// <summary>
        /// 지정된 범위 내에서 정수형 난수를 생성합니다.
        /// </summary>
        int Range(int min, int max);

        /// <summary>
        /// 지정된 범위 내에서 실수형 난수를 생성합니다.
        /// </summary>
        float Range(float min, float max);

        /// <summary>
        /// 3차원 단위 벡터 방향의 난수를 생성합니다.
        /// </summary>
        Vector3 RangeVector3Dir();

        /// <summary>
        /// 2차원 단위 벡터 방향의 난수를 생성합니다.
        /// </summary>
        Vector2 RangeVector2Dir();

        /// <summary>
        /// 무작위 색상을 생성합니다. 선택적으로 알파 값을 무작위로 설정할 수 있습니다.
        /// </summary>
        Color RandomColor(bool randAlpha = false);

        /// <summary>
        /// 0과 1 사이의 실수형 난수 값을 반환합니다.
        /// </summary>
        float ValueFloat();

        /// <summary>
        /// 0 또는 1의 정수형 난수 값을 반환합니다.
        /// </summary>
        int ValueInt();

        /// <summary>
        /// 0과 1 사이의 실수형 난수 값을 반환합니다.
        /// </summary>
        double ValueDouble();

        /// <summary>
        /// 무작위 회전(Quaternion) 값을 반환합니다.
        /// </summary>
        Quaternion RangeRotation();

        /// <summary>
        /// 지정된 확률에 따라 true 또는 false를 반환합니다.
        /// </summary>
        bool Rand1percent(float percent);

        /// <summary>
        /// 지정된 백분율 확률에 따라 true 또는 false를 반환합니다.
        /// </summary>
        bool Rand100percent(float percent100);
    }



    /// <summary>
    /// 직접 만든 커스텀 랜덤
    /// </summary>
    [Serializable]
    public class CustomRandom : ICustomRandom
    {
        ///======================================================================================================================================================



        /// <summary>
        /// <see cref="CustomRandom"/> 클래스의 새 인스턴스를 초기화합니다. 선택적으로 시드 값을 지정할 수 있습니다.
        /// </summary>
        /// <param name="seed">난수 생성에 사용될 기본 시드 값입니다.</param>
        public CustomRandom(int seed)
        {
            RefreshRandom(seed);
        }



        /// <summary>
        /// 모드에 따라 생성
        /// </summary>
        /// <param name="mode"></param>
        public CustomRandom(EMode mode)
        {
            switch (mode)
            {
                case EMode.RandomSeed:

                RefreshRandom();

                break;

                case EMode.LateCreate:



                break;
            }
        }



        public enum EMode
        {
            /// <summary>
            /// 시드값을 무작위로 생성합니다
            /// </summary>
            RandomSeed,
            /// <summary>
            /// 미리 <see cref="System.Random"/>를 생성할 필요가 없으면, 사용합니다.<br/>
            /// <see cref="RefreshRandom()"/> 을 호출하지 않으면 null 에러가 발생합니다
            /// </summary>
            LateCreate
        }



        ///======================================================================================================================================================



        ///<summary>
        ///언제나 어디서나 쉽게 호출할수있는<br/>
        ///전역 커스텀 랜덤 인스턴스
        /// </summary>
        public static CustomRandom Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CustomRandom(EMode.RandomSeed);
                }
                return instance;
            }
            set => instance = value;
        }
        private static CustomRandom instance;



        ///======================================================================================================================================================



        private System.Random Rand = null;



        [field: NonSerialized][field: ReadOnlyCustom] public bool Ready_Rand { get; private set; } = false;



        /// <summary>
        /// 난수 생성기의 현재 시드 값입니다.
        /// </summary>
        [field: SerializeField][field: ReadOnlyCustom] public int Seed { get; private set; }



        /// <summary>
        /// 난수 생성기가 새로 고침된 횟수를 나타냅니다.
        /// </summary>
        [field: NonSerialized][field: ReadOnlyCustom] public int RefreshCount { get; private set; } = 0;



        /// <summary>
        /// 시드 값을 기반으로 난수 생성기를 새로 고칩니다. 시드 값이 자동으로 생성되어 사용됩니다.
        /// </summary>
        /// <returns>새로운 시드 값 생성이 성공하면 true를 반환하고, 그렇지 않으면 false를 반환합니다.</returns>
        public bool RefreshRandom()
        {
            CreateRandomSeed();
            CreateRand();
            RefreshCount++;
            return true;
        }



        /// <summary>
        /// 지정된 시드 값으로 난수 생성기를 새로 고칩니다, 같은 Seed를 받아올경우 적용되지 않고 즉시 false를 반환합니다
        /// </summary>
        /// <param name="seed">난수 생성에 사용될 시드 값입니다.</param>
        public bool RefreshRandom(int seed)
        {
            Seed = seed;
            CreateRand();
            RefreshCount++;

            return true;
        }



        /// <summary>
        /// <see cref="Rand"/> 를 <see cref="Seed"/> 값을 기반으로 생성합니다.<br/>
        /// <see cref="Ready_Rand"/>가 활성화 됩니다.
        /// </summary>
        private void CreateRand()
        {
            Rand = new System.Random(Seed);
            if (!Ready_Rand) Ready_Rand = true;
        }



        /// <summary>
        /// 시간 또는 GUID를 기반으로 새로운 고유 시드 값을 생성합니다.
        /// </summary>
        private void CreateRandomSeed()
        {
            int currentSeed = Guid.NewGuid().GetHashCode();

            if (currentSeed == Seed)
            {
                if (currentSeed == int.MaxValue)
                {
                    currentSeed--;
                }
                else
                {
                    currentSeed++;
                }
            }

            Seed = currentSeed;
        }



        ///======================================================================================================================================================



        //? 간단한 값 생성



        /// <summary>
        /// 0과 1 사이의 실수형 난수를 생성합니다.
        /// </summary>
        /// <returns>생성된 실수형 난수입니다.</returns>
        public float ValueFloat()
        {
            return (float)Rand.NextDouble();
        }



        /// <summary>
        /// 0 또는 1의 정수형 난수를 생성합니다.
        /// </summary>
        /// <returns>생성된 정수형 난수입니다.</returns>
        public int ValueInt()
        {
            return Rand.Next(2);
        }



        /// <summary>
        /// 0과 1 사이의 실수형 난수를 생성합니다.
        /// </summary>
        /// <returns>생성된 실수형 난수입니다.</returns>
        public double ValueDouble()
        {
            return Rand.NextDouble();
        }



        /// <summary>
        /// true 또는 false를 동일한 확률로 생성합니다.
        /// </summary>
        /// <returns></returns>
        public bool ValueBool()
        {
            return ValueInt() == 0;
        }



        ///======================================================================================================================================================



        //? 범위 내 생성



        /// <summary>
        /// 지정된 범위 내의 정수형 난수를 생성합니다.<br/>
        /// <i><paramref name="max"/>값은 범위에 포함되지 않습니다</i>
        /// </summary>
        /// <param name="min">생성될 난수의 최소값입니다.</param>
        /// <param name="max">생성될 난수의 최대값입니다. 이 값의 -1 까지 범위에 포함됩니다.</param>
        /// <returns>지정된 범위 내에서 생성된 정수형 난수입니다.</returns>
        public int Range(int min, int max)
        {
            return Rand.Next(min, max);
        }



        /// <summary>
        /// 지정된 범위 내의 정수형 난수를 생성합니다.<br/>
        /// <i><paramref name="max"/>값도 범위에 포함됩니다</i>
        /// </summary>
        /// <param name="min">생성될 난수의 최소값입니다.</param>
        /// <param name="max">생성될 난수의 최대값입니다. 이 값도 범위에 포함됩니다.</param>
        /// <returns>지정된 범위 내에서 생성된 정수형 난수입니다.</returns>
        public int Range2(int min, int max)
        {
            return Rand.Next(min, max + 1);
        }



        /// <summary>
        /// 지정된 범위 내의 실수형 난수를 생성합니다.
        /// </summary>
        /// <param name="min">생성될 난수의 최소값입니다.</param>
        /// <param name="max">생성될 난수의 최대값입니다.</param>
        /// <returns>지정된 범위 내에서 생성된 실수형 난수입니다.</returns>
        public float Range(float min, float max)
        {
            return (ValueFloat() * (max - min) + min);
        }



        /// <summary>
        /// 특정 범위에서 제외된 값을 건너뛰며 무작위 값을 반환하는 메서드입니다.
        /// </summary>
        public int? RangeExcluding(int min, int max, params int[] excluding)
        {
            return RangeExcluding(min, max, (IEnumerable<int>)excluding);
        }



        /// <summary>
        /// 특정 범위에서 제외된 값을 건너뛰며 무작위 값을 반환하는 메서드입니다.
        /// </summary>
        public int? RangeExcluding(int min, int max, IEnumerable<int> excluding)
        {
            //. 가능한 값의 총 개수를 계산합니다.
            int rangeSize = max - min + 1;
            int excludeCount = 0;

            //. 제외 값의 개수를 계산 (범위 내에 포함된 값만 확인)
            foreach (int ex in excluding)
            {
                if (ex >= min && ex <= max) excludeCount++;
            }

            //! 모든 값이 제외되었다면 null을 반환합니다.
            if (excludeCount >= rangeSize)
            {
                return null;
            }

            //. 제외된 값을 제외한 범위 내 무작위 인덱스를 계산합니다.
            int randomIndex = Range(0, rangeSize - excludeCount);

            //. 범위를 순회하며 무작위 값을 선택합니다.
            for (int i = min; i <= max; i++)
            {
                //. 현재 값이 제외된 값인지 확인
                bool isExcluded = false;
                foreach (int ex in excluding)
                {
                    if (i == ex)
                    {
                        isExcluded = true;
                        break;
                    }
                }

                //. 제외된 값이라면 건너뜁니다.
                if (isExcluded) continue;

                //! 무작위 인덱스가 0이라면 해당 값을 반환합니다.
                if (randomIndex == 0) return i;

                //. 다음 값을 위해 인덱스를 감소시킵니다.
                randomIndex--;
            }

            //! 이론적으로 여기 도달하지 않습니다.
            return null;
        }



        /// <summary>
        /// 특정 범위에서 단일 값을 제외하고 무작위 값을 반환하는 메서드입니다.
        /// </summary>
        public int? RangeExcluding(int min, int max, int excluding)
        {
            //. 제외 값이 범위 밖에 있다면 바로 무작위 값을 반환합니다.
            if (excluding < min || excluding > max)
            {
                return Range(min, max + 1);
            }

            //. 범위에서 제외된 값을 뺀 총 개수를 계산합니다.
            int rangeSize = max - min;

            //. 제외된 값을 고려한 무작위 인덱스를 계산합니다.
            int randomIndex = Range(0, rangeSize);

            //! 제외된 값 이후의 값으로 이동하도록 조정합니다.
            return (randomIndex + min >= excluding) ? randomIndex + min + 1 : randomIndex + min;
        }



        #region Legacy



        [Obsolete]
        public int? RandExcluding_Legacy(int min, int max, IEnumerable<int> excluding)
        {
            // Enumerable.Range를 사용하여 가능한 모든 값을 생성하고,
            // Except 메서드로 excluding 리스트에 있는 값을 제외합니다.
            var possibleValues = Enumerable.Range(min, max + 1 - min).Except(excluding).ToList();

            if (possibleValues.Count == 0)
            {
                return null;
            }

            // 가능한 값들 중에서 무작위로 하나를 선택합니다.
            int randomIndex = Range(0, possibleValues.Count);

            return possibleValues[randomIndex];
        }



        [Obsolete]
        public int? RandExcluding_Legacy(int min, int max, int excluding)
        {
            var possibleValues = Enumerable.Range(min, max + 1 - min)
                                           .Where(x => x != excluding)
                                           .ToList();

            if (possibleValues.Count == 0)
            {
                return null;
            }

            int randomIndex = Range(0, possibleValues.Count);
            return possibleValues[randomIndex];
        }



        #endregion



        ///======================================================================================================================================================



        //? 범위 내에 지정된 단위로 생성



        /// <summary>
        /// 지정된 범위 내의 단위로 나누어진 float형 난수를 생성합니다.
        /// </summary>
        /// <param name="min">생성될 난수의 최소값입니다.</param>
        /// <param name="max">생성될 난수의 최대값입니다.</param>
        /// <param name="unit">생성될 난수의 단위입니다.</param>
        /// <returns>지정된 범위 내에서 단위로 나누어진 float형 난수입니다.</returns>
        public float RangeUnit(float min, float max, float unit)
        {
            if (unit <= 0)
            {
                throw new ArgumentException("Unit must be greater than zero.");
            }

            return GenerateRandomValue(min, max, unit);
        }



        /// <summary>
        /// 지정된 비율에 따라 min에서부터 단위로 나누어진 float형 난수를 생성합니다.
        /// </summary>
        /// <param name="min">생성될 난수의 최소값입니다.</param>
        /// <param name="max">생성될 난수의 최대값입니다.</param>
        /// <param name="unit">생성될 난수의 단위입니다.</param>
        /// <param name="percentage">생성될 난수의 비율입니다. (0 ~ 1 사이의 값)</param>
        /// <returns>지정된 비율에 따라 min에서부터 단위로 나누어진 float형 난수입니다.</returns>
        public float RangeUnitFromMin(float min, float max, float unit, float percentage)
        {
            if (unit <= 0)
            {
                throw new ArgumentException("Unit must be greater than zero.");
            }

            if (percentage < 0f || percentage > 1f)
            {
                throw new ArgumentException("Percentage must be between 0 and 1.");
            }

            if (percentage == 0f)
            {
                return min;
            }

            if (percentage == 1f)
            {
                return max;
            }

            float targetValue = Mathf.Lerp(min, max, percentage);
            return GenerateRandomValue(min, targetValue, unit);
        }



        /// <summary>
        /// 지정된 비율에 따라 중간값을 기준으로 단위로 나누어진 float형 난수를 생성합니다.
        /// </summary>
        /// <param name="min">생성될 난수의 최소값입니다.</param>
        /// <param="max">생성될 난수의 최대값입니다.</param>
        /// <param name="unit">생성될 난수의 단위입니다.</param>
        /// <param name="percentage">생성될 난수의 비율입니다. (0 ~ 1 사이의 값)</param>
        /// <returns>지정된 비율에 따라 중간값을 기준으로 단위로 나누어진 float형 난수입니다.</returns>
        public float RangeUnitFromMid(float min, float max, float unit, float percentage)
        {
            if (unit <= 0)
            {
                throw new ArgumentException("Unit must be greater than zero.");
            }

            if (percentage < 0f || percentage > 1f)
            {
                throw new ArgumentException("Percentage must be between 0 and 1.");
            }

            float midPoint = (min + max) / 2f;
            float range = (max - min) / 2f * percentage;

            float rangeMin = midPoint - range;
            float rangeMax = midPoint + range;

            // rangeMin과 rangeMax를 unit 단위로 조정
            rangeMin = Mathf.Floor(rangeMin / unit) * unit;
            rangeMax = Mathf.Floor(rangeMax / unit) * unit;

            return GenerateRandomValue(rangeMin, rangeMax, unit);
        }



        /// <summary>
        /// 지정된 비율에 따라 max에서부터 단위로 나누어진 float형 난수를 생성합니다.
        /// </summary>
        /// <param name="min">생성될 난수의 최소값입니다.</param>
        /// <param="max">생성될 난수의 최대값입니다.</param>
        /// <param="unit">생성될 난수의 단위입니다.</param>
        /// <param name="percentage">생성될 난수의 비율입니다. (0 ~ 1 사이의 값)</param>
        /// <returns>지정된 비율에 따라 max에서부터 단위로 나누어진 float형 난수입니다.</returns>
        public float RangeUnitFromMax(float min, float max, float unit, float percentage)
        {
            if (unit <= 0)
            {
                throw new ArgumentException("Unit must be greater than zero.");
            }

            if (percentage < 0f || percentage > 1f)
            {
                throw new ArgumentException("Percentage must be between 0 and 1.");
            }

            if (percentage == 0f)
            {
                return max;
            }

            if (percentage == 1f)
            {
                return min;
            }

            float targetValue = Mathf.Lerp(max, min, percentage);
            return GenerateRandomValue(targetValue, max, unit);
        }



        /// <summary>
        /// 공통 로직을 사용하여 지정된 범위 내의 단위로 나누어진 float형 난수를 생성합니다.
        /// </summary>
        /// <param name="min">범위의 최소값입니다.</param>
        /// <param name="max">범위의 최대값입니다.</param>
        /// <param="unit">단위입니다.</param>
        /// <returns>지정된 범위 내에서 단위로 나누어진 float형 난수입니다.</returns>
        private float GenerateRandomValue(float min, float max, float unit)
        {
            int steps = Mathf.FloorToInt((max - min) / unit) + 1;
            int randomStep = Rand.Next(steps);
            return min + randomStep * unit;
        }



        ///======================================================================================================================================================



        //? 벡터, 회전 생성



        /// <summary>
        /// 3차원 벡터 방향을 무작위로 생성합니다.<br/>
        /// <i>(모든 축이 -1f ~ 1f) 내에 결정되고 normalized</i>
        /// </summary>
        /// <returns>생성된 3차원 단위 벡터입니다.</returns>
        public Vector3 RangeVector3Dir()
        {
            float x = Range(-1f, 1f);
            float y = Range(-1f, 1f);
            float z = Range(-1f, 1f);
            return new Vector3(x, y, z).normalized;
        }



        /// <summary>
        /// 3차원 정수 벡터 방향을 무작위로 생성합니다.<br/>
        /// <i>(모든 축이 -1 ~ 1) 내에 결정</i>
        /// </summary>
        /// <returns>생성된 3차원 정수 단위 벡터입니다.</returns>
        public Vector3Int RangeVector3IntDir()
        {
            int x = Range2(-1, 1);
            int y = Range2(-1, 1);
            int z = Range2(-1, 1);
            return new Vector3Int(x, y, z);
        }



        /// <summary>
        /// 2차원 벡터 방향을 무작위로 생성합니다.<br/>
        /// <i>(모든 축이 -1f ~ 1f) 내에 결정되고 normalized</i>
        /// </summary>
        /// <returns>생성된 2차원 단위 벡터입니다.</returns>
        public Vector2 RangeVector2Dir()
        {
            float x = Range(-1f, 1f);
            float y = Range(-1f, 1f);
            return new Vector2(x, y).normalized;
        }



        /// <summary>
        /// 2차원 정수 벡터 방향을 무작위로 생성합니다.<br/>
        /// <i>(모든 축이 -1 ~ 1) 내에 결정</i>
        /// </summary>
        /// <returns>생성된 2차원 정수 단위 벡터입니다.</returns>
        public Vector2Int RangeVector2IntDir()
        {
            int x = Range2(-1, 1);
            int y = Range2(-1, 1);
            return new Vector2Int(x, y);
        }



        /// <summary>
        /// 무작위 회전(Quaternion)을 생성합니다.
        /// </summary>
        /// <returns>생성된 무작위 회전입니다.</returns>
        public Quaternion RangeRotation()
        {
            Vector3 axis = RangeVector3Dir();
            float angle = Range(0f, 360f);
            return Quaternion.AngleAxis(angle, axis);
        }



        ///======================================================================================================================================================



        //? 기타 생성



        /// <summary>
        /// 무작위 색상을 생성합니다. 선택적으로 알파값도 무작위로 설정할 수 있습니다.
        /// </summary>
        /// <param name="randAlpha">알파값도 무작위로 설정할지 여부입니다. 기본값은 false입니다.</param>
        /// <returns>생성된 무작위 색상입니다.</returns>
        public Color RandomColor(bool randAlpha = false)
        {
            float r = Range(0f, 1f);
            float g = Range(0f, 1f);
            float b = Range(0f, 1f);
            float a = randAlpha ? Range(0f, 1f) : 1f;
            return new Color(r, g, b, a);
        }



        ///======================================================================================================================================================



        //? 확률 얻기



        /// <summary>
        /// 지정된 확률로 true를 반환합니다.
        /// </summary>
        /// <param name="percent">true를 반환할 확률(0에서 1 사이).</param>
        /// <returns>지정된 확률에 따라 true 또는 false.</returns>
        public bool Rand1percent(float percent)
        {
            return Rand100percent(percent * 100f);
        }



        /// <summary>
        /// 지정된 백분율 확률로 true를 반환합니다.
        /// </summary>
        /// <param name="percent100">true를 반환할 확률(0에서 100 사이).</param>
        /// <returns>지정된 확률에 따라 true 또는 false.</returns>
        public bool Rand100percent(float percent100)
        {
            percent100 = Math.Clamp(percent100, 0, 100);
            return Range2(0, 100) <= percent100;
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================
}