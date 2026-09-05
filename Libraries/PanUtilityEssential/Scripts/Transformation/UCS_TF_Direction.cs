using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;



//? [Transformation] 주로 "방향" 들이 들어있는 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    public static class SU_TF_Direction
    {
        ///======================================================================================================================================================



        //? Vector와 방향 열거형



        /// <summary>
        /// 주어진 벡터를 4방향 또는 단일 축 방향으로 변환합니다.<br/>
        /// <i>한가지 방향만 반환하기에, 우선순위가 중요!</i>
        /// </summary>
        /// <param name="vector">대상 벡터</param>
        /// <param name="prioritizeX">X축을 우선할지 여부 (기본값: true)</param>
        /// <param name="preferNegative">대상 축이 0일 때 음수 방향(좌측/하단)을 우선할지 여부 (기본값: true)</param>
        /// <returns>변환된 방향을 나타내는 <see cref="EDirection4nWithSingle"/> 값</returns>
        public static EDirection4nWithSingle ToDirection4AndSingle(this Vector2 vector, bool prioritizeX, bool preferNegative = true)
        {
            //? 벡터의 X 및 Y 값이 모두 0인 경우 방향 없음 반환
            if (vector == Vector2.zero)
            {
                return EDirection4nWithSingle.None;
            }

            //? X축 우선 처리
            if (prioritizeX)
            {
                if (vector.x < 0)
                {
                    return vector.y == 0 ? EDirection4nWithSingle.LeftSingle : EDirection4nWithSingle.Left;
                }
                else if (vector.x > 0)
                {
                    return vector.y == 0 ? EDirection4nWithSingle.RightSingle : EDirection4nWithSingle.Right;
                }
                else // vector.x == 0
                {
                    if (vector.y < 0)
                    {
                        return EDirection4nWithSingle.DownSingle;
                    }
                    else if (vector.y > 0)
                    {
                        return EDirection4nWithSingle.UpSingle;
                    }
                    else // vector.y == 0
                    {
                        return preferNegative ? EDirection4nWithSingle.LeftSingle : EDirection4nWithSingle.RightSingle;
                    }
                }
            }
            //? Y축 우선 처리
            else
            {
                if (vector.y < 0)
                {
                    return vector.x == 0 ? EDirection4nWithSingle.DownSingle : EDirection4nWithSingle.Down;
                }
                else if (vector.y > 0)
                {
                    return vector.x == 0 ? EDirection4nWithSingle.UpSingle : EDirection4nWithSingle.Up;
                }
                else // vector.y == 0
                {
                    if (vector.x < 0)
                    {
                        return EDirection4nWithSingle.LeftSingle;
                    }
                    else if (vector.x > 0)
                    {
                        return EDirection4nWithSingle.RightSingle;
                    }
                    else // vector.x == 0
                    {
                        return preferNegative ? EDirection4nWithSingle.DownSingle : EDirection4nWithSingle.UpSingle;
                    }
                }
            }
        }



        ///======================================================================================================================================================



        #region 방향 열거형 전역 확장 메서드



        ///======================================================================================================================================================



        #region 방향 검사



        //?  EDirectionLR 확장 메서드



        /// <summary>
        /// EDirectionLR 열거형이 Left인지 확인합니다.
        /// </summary>
        public static bool IsLeft(this EDirectionLR dir) => dir == EDirectionLR.Left;

        /// <summary>
        /// EDirectionLR 열거형이 Right인지 확인합니다.
        /// </summary>
        public static bool IsRight(this EDirectionLR dir) => dir == EDirectionLR.Right;



        //? EDirectionLRn 확장 메서드


        /// <summary>
        /// EDirectionLRn 열거형이 None인지 확인합니다.
        /// </summary>
        public static bool IsNone(this EDirectionLRn dir) => dir == EDirectionLRn.None;

        /// <summary>
        /// EDirectionLRn 열거형이 Left인지 확인합니다.
        /// </summary>
        public static bool IsLeft(this EDirectionLRn dir) => dir == EDirectionLRn.Left;

        /// <summary>
        /// EDirectionLRn 열거형이 Right인지 확인합니다.
        /// </summary>
        public static bool IsRight(this EDirectionLRn dir) => dir == EDirectionLRn.Right;

        /// <summary>
        /// EDirectionLRn 열거형이 Left 또는 Right인지 확인합니다.
        /// </summary>
        public static bool IsLeftOrRight(this EDirectionLRn dir)
            => dir == EDirectionLRn.Left || dir == EDirectionLRn.Right;



        //? EDirectionUD 확장 메서드



        /// <summary>
        /// EDirectionUD 열거형이 None인지 확인합니다.
        /// </summary>
        public static bool IsNone(this EDirectionUD dir) => dir == EDirectionUD.None;

        /// <summary>
        /// EDirectionUD 열거형이 Up인지 확인합니다.
        /// </summary>
        public static bool IsUp(this EDirectionUD dir) => dir == EDirectionUD.Up;

        /// <summary>
        /// EDirectionUD 열거형이 Down인지 확인합니다.
        /// </summary>
        public static bool IsDown(this EDirectionUD dir) => dir == EDirectionUD.Down;



        //? EDirectionUDn 확장 메서드



        /// <summary>
        /// EDirectionUDn 열거형이 None인지 확인합니다.
        /// </summary>
        public static bool IsNone(this EDirectionUDn dir) => dir == EDirectionUDn.None;

        /// <summary>
        /// EDirectionUDn 열거형이 Up인지 확인합니다.
        /// </summary>
        public static bool IsUp(this EDirectionUDn dir) => dir == EDirectionUDn.Up;

        /// <summary>
        /// EDirectionUDn 열거형이 Down인지 확인합니다.
        /// </summary>
        public static bool IsDown(this EDirectionUDn dir) => dir == EDirectionUDn.Down;



        //? EDirection4 확장 메서드



        /// <summary>
        /// EDirection4 열거형이 Left인지 확인합니다.
        /// </summary>
        public static bool IsLeft(this EDirection4 dir) => dir == EDirection4.Left;

        /// <summary>
        /// EDirection4 열거형이 Right인지 확인합니다.
        /// </summary>
        public static bool IsRight(this EDirection4 dir) => dir == EDirection4.Right;

        /// <summary>
        /// EDirection4 열거형이 Up인지 확인합니다.
        /// </summary>
        public static bool IsUp(this EDirection4 dir) => dir == EDirection4.Up;

        /// <summary>
        /// EDirection4 열거형이 Down인지 확인합니다.
        /// </summary>
        public static bool IsDown(this EDirection4 dir) => dir == EDirection4.Down;

        /// <summary>
        /// EDirection4 열거형이 좌/우 방향인지 확인합니다.
        /// </summary>
        public static bool IsHorizontal(this EDirection4 dir)
            => dir == EDirection4.Left || dir == EDirection4.Right;

        /// <summary>
        /// EDirection4 열거형이 상/하 방향인지 확인합니다.
        /// </summary>
        public static bool IsVertical(this EDirection4 dir)
            => dir == EDirection4.Up || dir == EDirection4.Down;



        //? EDirection4n 확장 메서드



        /// <summary>
        /// EDirection4n 열거형이 None인지 확인합니다.
        /// </summary>
        public static bool IsNone(this EDirection4n dir) => dir == EDirection4n.None;

        /// <summary>
        /// EDirection4n 열거형이 Left인지 확인합니다.
        /// </summary>
        public static bool IsLeft(this EDirection4n dir) => dir == EDirection4n.Left;

        /// <summary>
        /// EDirection4n 열거형이 Right인지 확인합니다.
        /// </summary>
        public static bool IsRight(this EDirection4n dir) => dir == EDirection4n.Right;

        /// <summary>
        /// EDirection4n 열거형이 Up인지 확인합니다.
        /// </summary>
        public static bool IsUp(this EDirection4n dir) => dir == EDirection4n.Up;

        /// <summary>
        /// EDirection4n 열거형이 Down인지 확인합니다.
        /// </summary>
        public static bool IsDown(this EDirection4n dir) => dir == EDirection4n.Down;

        /// <summary>
        /// EDirection4n 열거형이 좌/우 방향인지 확인합니다.
        /// </summary>
        public static bool IsHorizontal(this EDirection4n dir)
            => dir == EDirection4n.Left || dir == EDirection4n.Right;

        /// <summary>
        /// EDirection4n 열거형이 상/하 방향인지 확인합니다.
        /// </summary>
        public static bool IsVertical(this EDirection4n dir)
            => dir == EDirection4n.Up || dir == EDirection4n.Down;



        //? EDirection4nWithSingle 확장 메서드



        /// <summary>
        /// EDirection4nWithSingle 열거형이 None인지 확인합니다.
        /// </summary>
        public static bool IsNone(this EDirection4nWithSingle dir)
            => dir == EDirection4nWithSingle.None;

        /// <summary>
        /// EDirection4nWithSingle 열거형이 Left인지 확인합니다.
        /// </summary>
        public static bool IsLeft(this EDirection4nWithSingle dir)
            => dir == EDirection4nWithSingle.Left;

        /// <summary>
        /// EDirection4nWithSingle 열거형이 Right인지 확인합니다.
        /// </summary>
        public static bool IsRight(this EDirection4nWithSingle dir)
            => dir == EDirection4nWithSingle.Right;

        /// <summary>
        /// EDirection4nWithSingle 열거형이 Up인지 확인합니다.
        /// </summary>
        public static bool IsUp(this EDirection4nWithSingle dir)
            => dir == EDirection4nWithSingle.Up;

        /// <summary>
        /// EDirection4nWithSingle 열거형이 Down인지 확인합니다.
        /// </summary>
        public static bool IsDown(this EDirection4nWithSingle dir)
            => dir == EDirection4nWithSingle.Down;

        /// <summary>
        /// EDirection4nWithSingle 열거형이 단일 방향(LeftSingle, RightSingle, UpSingle, DownSingle)인지 확인합니다.
        /// </summary>
        public static bool IsSingleDirection(this EDirection4nWithSingle dir)
            => dir == EDirection4nWithSingle.LeftSingle
            || dir == EDirection4nWithSingle.RightSingle
            || dir == EDirection4nWithSingle.UpSingle
            || dir == EDirection4nWithSingle.DownSingle;



        //? EDirection8 확장 메서드 (대각선 포함 선택)



        /// <summary>
        /// EDirection8 열거형이 Left인지 확인합니다.<br/>
        /// includeDiagonal이 true일 경우, LeftUp과 LeftDown도 Left로 판단합니다.
        /// </summary>
        public static bool IsLeft(this EDirection8 dir, bool includeDiagonal = false)
        {
            if (dir == EDirection8.Left)
                return true;
            //? 대각선 포함 여부 확인: includeDiagonal true이면 LeftUp, LeftDown도 Left로 간주
            if (includeDiagonal && (dir == EDirection8.LeftUp || dir == EDirection8.LeftDown))
                return true;
            return false;
        }

        /// <summary>
        /// EDirection8 열거형이 Right인지 확인합니다.<br/>
        /// includeDiagonal이 true일 경우, RightUp과 RightDown도 Right로 판단합니다.
        /// </summary>
        public static bool IsRight(this EDirection8 dir, bool includeDiagonal = false)
        {
            if (dir == EDirection8.Right)
                return true;
            //? 대각선 포함 여부 확인: includeDiagonal true이면 RightUp, RightDown도 Right로 간주
            if (includeDiagonal && (dir == EDirection8.RightUp || dir == EDirection8.RightDown))
                return true;
            return false;
        }

        /// <summary>
        /// EDirection8 열거형이 Up인지 확인합니다.<br/>
        /// includeDiagonal이 true일 경우, LeftUp과 RightUp도 Up으로 판단합니다.
        /// </summary>
        public static bool IsUp(this EDirection8 dir, bool includeDiagonal = false)
        {
            if (dir == EDirection8.Up)
                return true;
            //? 대각선 포함 여부 확인: includeDiagonal true이면 LeftUp, RightUp도 Up으로 간주
            if (includeDiagonal && (dir == EDirection8.LeftUp || dir == EDirection8.RightUp))
                return true;
            return false;
        }

        /// <summary>
        /// EDirection8 열거형이 Down인지 확인합니다.<br/>
        /// includeDiagonal이 true일 경우, LeftDown과 RightDown도 Down으로 판단합니다.
        /// </summary>
        public static bool IsDown(this EDirection8 dir, bool includeDiagonal = false)
        {
            if (dir == EDirection8.Down)
                return true;
            //? 대각선 포함 여부 확인: includeDiagonal true이면 LeftDown, RightDown도 Down으로 간주
            if (includeDiagonal && (dir == EDirection8.LeftDown || dir == EDirection8.RightDown))
                return true;
            return false;
        }

        /// <summary>
        /// EDirection8 열거형이 LeftUp인지 확인합니다.
        /// </summary>
        public static bool IsLeftUp(this EDirection8 dir) => dir == EDirection8.LeftUp;

        /// <summary>
        /// EDirection8 열거형이 LeftDown인지 확인합니다.
        /// </summary>
        public static bool IsLeftDown(this EDirection8 dir) => dir == EDirection8.LeftDown;

        /// <summary>
        /// EDirection8 열거형이 RightUp인지 확인합니다.
        /// </summary>
        public static bool IsRightUp(this EDirection8 dir) => dir == EDirection8.RightUp;

        /// <summary>
        /// EDirection8 열거형이 RightDown인지 확인합니다.
        /// </summary>
        public static bool IsRightDown(this EDirection8 dir) => dir == EDirection8.RightDown;

        /// <summary>
        /// EDirection8 열거형이 대각선 방향(LeftUp, LeftDown, RightUp, RightDown)인지 확인합니다.
        /// </summary>
        public static bool IsDiagonal(this EDirection8 dir)
            => dir == EDirection8.LeftUp
            || dir == EDirection8.LeftDown
            || dir == EDirection8.RightUp
            || dir == EDirection8.RightDown;



        //? EDirection8n 확장 메서드 (대각선 포함 선택)



        /// <summary>
        /// EDirection8n 열거형이 None인지 확인합니다.
        /// </summary>
        public static bool IsNone(this EDirection8n dir) => dir == EDirection8n.None;

        /// <summary>
        /// EDirection8n 열거형이 Left인지 확인합니다.<br/>
        /// includeDiagonal이 true일 경우, LeftUp과 LeftDown도 Left로 판단합니다.
        /// </summary>
        public static bool IsLeft(this EDirection8n dir, bool includeDiagonal = false)
        {
            if (dir == EDirection8n.Left)
                return true;
            //? 대각선 포함 여부 확인
            if (includeDiagonal && (dir == EDirection8n.LeftUp || dir == EDirection8n.LeftDown))
                return true;
            return false;
        }

        /// <summary>
        /// EDirection8n 열거형이 Right인지 확인합니다.<br/>
        /// includeDiagonal이 true일 경우, RightUp과 RightDown도 Right로 판단합니다.
        /// </summary>
        public static bool IsRight(this EDirection8n dir, bool includeDiagonal = false)
        {
            if (dir == EDirection8n.Right)
                return true;
            //? 대각선 포함 여부 확인
            if (includeDiagonal && (dir == EDirection8n.RightUp || dir == EDirection8n.RightDown))
                return true;
            return false;
        }

        /// <summary>
        /// EDirection8n 열거형이 Up인지 확인합니다.<br/>
        /// includeDiagonal이 true일 경우, LeftUp과 RightUp도 Up으로 판단합니다.
        /// </summary>
        public static bool IsUp(this EDirection8n dir, bool includeDiagonal = false)
        {
            if (dir == EDirection8n.Up)
                return true;
            //? 대각선 포함 여부 확인
            if (includeDiagonal && (dir == EDirection8n.LeftUp || dir == EDirection8n.RightUp))
                return true;
            return false;
        }

        /// <summary>
        /// EDirection8n 열거형이 Down인지 확인합니다.<br/>
        /// includeDiagonal이 true일 경우, LeftDown과 RightDown도 Down으로 판단합니다.
        /// </summary>
        public static bool IsDown(this EDirection8n dir, bool includeDiagonal = false)
        {
            if (dir == EDirection8n.Down)
                return true;
            //? 대각선 포함 여부 확인
            if (includeDiagonal && (dir == EDirection8n.LeftDown || dir == EDirection8n.RightDown))
                return true;
            return false;
        }

        /// <summary>
        /// EDirection8n 열거형이 LeftUp인지 확인합니다.
        /// </summary>
        public static bool IsLeftUp(this EDirection8n dir) => dir == EDirection8n.LeftUp;

        /// <summary>
        /// EDirection8n 열거형이 LeftDown인지 확인합니다.
        /// </summary>
        public static bool IsLeftDown(this EDirection8n dir) => dir == EDirection8n.LeftDown;

        /// <summary>
        /// EDirection8n 열거형이 RightUp인지 확인합니다.
        /// </summary>
        public static bool IsRightUp(this EDirection8n dir) => dir == EDirection8n.RightUp;

        /// <summary>
        /// EDirection8n 열거형이 RightDown인지 확인합니다.
        /// </summary>
        public static bool IsRightDown(this EDirection8n dir) => dir == EDirection8n.RightDown;

        /// <summary>
        /// EDirection8n 열거형이 대각선 방향(LeftUp, LeftDown, RightUp, RightDown)인지 확인합니다.
        /// </summary>
        public static bool IsDiagonal(this EDirection8n dir)
            => dir == EDirection8n.LeftUp
            || dir == EDirection8n.LeftDown
            || dir == EDirection8n.RightUp
            || dir == EDirection8n.RightDown;
        #endregion



        ///======================================================================================================================================================



        //? 반대 방향 반환 확장 메서드



        #region 반대 방향 반환 확장 메서드



        /// <summary>
        /// EDirectionLR 열거형의 반대 방향을 반환합니다.
        /// </summary>
        public static EDirectionLR GetOpposite(this EDirectionLR dir)
        {
            switch (dir)
            {
                case EDirectionLR.Left:
                return EDirectionLR.Right;
                case EDirectionLR.Right:
                return EDirectionLR.Left;
                default:
                return dir;
            }
        }

        /// <summary>
        /// EDirectionLRn 열거형의 반대 방향을 반환합니다.
        /// </summary>
        public static EDirectionLRn GetOpposite(this EDirectionLRn dir)
        {
            switch (dir)
            {
                case EDirectionLRn.Left:
                return EDirectionLRn.Right;
                case EDirectionLRn.Right:
                return EDirectionLRn.Left;
                default:
                return dir;
            }
        }

        /// <summary>
        /// EDirectionUD 열거형의 반대 방향을 반환합니다.
        /// </summary>
        public static EDirectionUD GetOpposite(this EDirectionUD dir)
        {
            switch (dir)
            {
                case EDirectionUD.Up:
                return EDirectionUD.Down;
                case EDirectionUD.Down:
                return EDirectionUD.Up;
                default:
                return dir;
            }
        }

        /// <summary>
        /// EDirectionUDn 열거형의 반대 방향을 반환합니다.
        /// </summary>
        public static EDirectionUDn GetOpposite(this EDirectionUDn dir)
        {
            switch (dir)
            {
                case EDirectionUDn.Up:
                return EDirectionUDn.Down;
                case EDirectionUDn.Down:
                return EDirectionUDn.Up;
                default:
                return dir;
            }
        }

        /// <summary>
        /// EDirection4 열거형의 반대 방향을 반환합니다.
        /// </summary>
        public static EDirection4 GetOpposite(this EDirection4 dir)
        {
            switch (dir)
            {
                case EDirection4.Left:
                return EDirection4.Right;
                case EDirection4.Right:
                return EDirection4.Left;
                case EDirection4.Up:
                return EDirection4.Down;
                case EDirection4.Down:
                return EDirection4.Up;
                default:
                return dir;
            }
        }

        /// <summary>
        /// EDirection4n 열거형의 반대 방향을 반환합니다.
        /// </summary>
        public static EDirection4n GetOpposite(this EDirection4n dir)
        {
            switch (dir)
            {
                case EDirection4n.Left:
                return EDirection4n.Right;
                case EDirection4n.Right:
                return EDirection4n.Left;
                case EDirection4n.Up:
                return EDirection4n.Down;
                case EDirection4n.Down:
                return EDirection4n.Up;
                default:
                return dir;
            }
        }

        /// <summary>
        /// EDirection4nWithSingle 열거형의 반대 방향을 반환합니다.
        /// </summary>
        public static EDirection4nWithSingle GetOpposite(this EDirection4nWithSingle dir)
        {
            switch (dir)
            {
                case EDirection4nWithSingle.Left:
                return EDirection4nWithSingle.Right;
                case EDirection4nWithSingle.Right:
                return EDirection4nWithSingle.Left;
                case EDirection4nWithSingle.Up:
                return EDirection4nWithSingle.Down;
                case EDirection4nWithSingle.Down:
                return EDirection4nWithSingle.Up;
                case EDirection4nWithSingle.LeftSingle:
                return EDirection4nWithSingle.RightSingle;
                case EDirection4nWithSingle.RightSingle:
                return EDirection4nWithSingle.LeftSingle;
                case EDirection4nWithSingle.UpSingle:
                return EDirection4nWithSingle.DownSingle;
                case EDirection4nWithSingle.DownSingle:
                return EDirection4nWithSingle.UpSingle;
                default:
                return dir;
            }
        }

        /// <summary>
        /// EDirection8 열거형의 반대 방향을 반환합니다.
        /// </summary>
        public static EDirection8 GetOpposite(this EDirection8 dir)
        {
            switch (dir)
            {
                case EDirection8.Left:
                return EDirection8.Right;
                case EDirection8.Right:
                return EDirection8.Left;
                case EDirection8.Up:
                return EDirection8.Down;
                case EDirection8.Down:
                return EDirection8.Up;
                case EDirection8.LeftDown:
                return EDirection8.RightUp;
                case EDirection8.LeftUp:
                return EDirection8.RightDown;
                case EDirection8.RightDown:
                return EDirection8.LeftUp;
                case EDirection8.RightUp:
                return EDirection8.LeftDown;
                default:
                return dir;
            }
        }

        /// <summary>
        /// EDirection8n 열거형의 반대 방향을 반환합니다.
        /// </summary>
        public static EDirection8n GetOpposite(this EDirection8n dir)
        {
            switch (dir)
            {
                case EDirection8n.Left:
                return EDirection8n.Right;
                case EDirection8n.Right:
                return EDirection8n.Left;
                case EDirection8n.Up:
                return EDirection8n.Down;
                case EDirection8n.Down:
                return EDirection8n.Up;
                case EDirection8n.LeftDown:
                return EDirection8n.RightUp;
                case EDirection8n.LeftUp:
                return EDirection8n.RightDown;
                case EDirection8n.RightDown:
                return EDirection8n.LeftUp;
                case EDirection8n.RightUp:
                return EDirection8n.LeftDown;
                default:
                return dir;
            }
        }



        #endregion



        ///======================================================================================================================================================



        //? Vector 변환 확장 메서드



        #region Vector 변환 확장 메서드



        //? EDirectionLR Vector 변환 확장 메서드


        /// <summary>
        /// EDirectionLR 열거형 값을 Vector2로 변환합니다. <br/>
        /// Left일 경우 Vector2.left (-1, 0), Right일 경우 Vector2.right (1, 0)을 반환합니다. <br/>
        /// </summary>
        /// <returns>변환된 Vector2 값</returns>
        public static Vector2 ToVector2(this EDirectionLR dir)
        {
            switch (dir)
            {
                case EDirectionLR.Left: return Vector2.left;
                case EDirectionLR.Right: return Vector2.right;
                default: return Vector2.zero;
            }
        }

        /// <summary>
        /// EDirectionLR 열거형 값을 Vector2Int로 변환합니다. <br/>
        /// Left일 경우 Vector2Int(-1, 0), Right일 경우 Vector2Int(1, 0)을 반환합니다. <br/>
        /// </summary>
        /// <returns>변환된 Vector2Int 값</returns>
        public static Vector2Int ToVector2Int(this EDirectionLR dir)
        {
            switch (dir)
            {
                case EDirectionLR.Left: return new Vector2Int(-1, 0);
                case EDirectionLR.Right: return new Vector2Int(1, 0);
                default: return Vector2Int.zero;
            }
        }



        //? EDirectionLRn Vector 변환 확장 메서드



        /// <summary>
        /// EDirectionLRn 열거형 값을 Vector2로 변환합니다. <br/>
        /// None일 경우 Vector2.zero, Left일 경우 Vector2.left, Right일 경우 Vector2.right를 반환합니다. <br/>
        /// </summary>
        /// <returns>변환된 Vector2 값</returns>
        public static Vector2 ToVector2(this EDirectionLRn dir)
        {
            switch (dir)
            {
                case EDirectionLRn.Left: return Vector2.left;
                case EDirectionLRn.Right: return Vector2.right;
                default: return Vector2.zero;
            }
        }

        /// <summary>
        /// EDirectionLRn 열거형 값을 Vector2Int로 변환합니다. <br/>
        /// None일 경우 Vector2Int.zero, Left일 경우 Vector2Int(-1, 0), Right일 경우 Vector2Int(1, 0)을 반환합니다. <br/>
        /// </summary>
        /// <returns>변환된 Vector2Int 값</returns>
        public static Vector2Int ToVector2Int(this EDirectionLRn dir)
        {
            switch (dir)
            {
                case EDirectionLRn.Left: return new Vector2Int(-1, 0);
                case EDirectionLRn.Right: return new Vector2Int(1, 0);
                default: return Vector2Int.zero;
            }
        }



        //? EDirectionUD Vector 변환 확장 메서드



        /// <summary>
        /// EDirectionUD 열거형 값을 Vector2로 변환합니다. <br/>
        /// None일 경우 Vector2.zero, Up일 경우 Vector2.up, Down일 경우 Vector2.down을 반환합니다. <br/>
        /// </summary>
        /// <returns>변환된 Vector2 값</returns>
        public static Vector2 ToVector2(this EDirectionUD dir)
        {
            switch (dir)
            {
                case EDirectionUD.Up: return Vector2.up;
                case EDirectionUD.Down: return Vector2.down;
                default: return Vector2.zero;
            }
        }

        /// <summary>
        /// EDirectionUD 열거형 값을 Vector2Int로 변환합니다. <br/>
        /// None일 경우 Vector2Int.zero, Up일 경우 Vector2Int(0, 1), Down일 경우 Vector2Int(0, -1)을 반환합니다. <br/>
        /// </summary>
        /// <returns>변환된 Vector2Int 값</returns>
        public static Vector2Int ToVector2Int(this EDirectionUD dir)
        {
            switch (dir)
            {
                case EDirectionUD.Up: return new Vector2Int(0, 1);
                case EDirectionUD.Down: return new Vector2Int(0, -1);
                default: return Vector2Int.zero;
            }
        }



        //? EDirectionUDn Vector 변환 확장 메서드



        /// <summary>
        /// EDirectionUDn 열거형 값을 Vector2로 변환합니다. <br/>
        /// None일 경우 Vector2.zero, Up일 경우 Vector2.up, Down일 경우 Vector2.down을 반환합니다. <br/>
        /// </summary>
        /// <returns>변환된 Vector2 값</returns>
        public static Vector2 ToVector2(this EDirectionUDn dir)
        {
            switch (dir)
            {
                case EDirectionUDn.Up: return Vector2.up;
                case EDirectionUDn.Down: return Vector2.down;
                default: return Vector2.zero;
            }
        }

        /// <summary>
        /// EDirectionUDn 열거형 값을 Vector2Int로 변환합니다. <br/>
        /// None일 경우 Vector2Int.zero, Up일 경우 Vector2Int(0, 1), Down일 경우 Vector2Int(0, -1)을 반환합니다. <br/>
        /// </summary>
        /// <returns>변환된 Vector2Int 값</returns>
        public static Vector2Int ToVector2Int(this EDirectionUDn dir)
        {
            switch (dir)
            {
                case EDirectionUDn.Up: return new Vector2Int(0, 1);
                case EDirectionUDn.Down: return new Vector2Int(0, -1);
                default: return Vector2Int.zero;
            }
        }



        //? EDirection4 Vector 변환 확장 메서드



        /// <summary>
        /// EDirection4 열거형 값을 Vector2로 변환합니다. <br/>
        /// Left는 Vector2.left, Right는 Vector2.right, Up은 Vector2.up, Down은 Vector2.down을 반환합니다. <br/>
        /// </summary>
        /// <returns>변환된 Vector2 값</returns>
        public static Vector2 ToVector2(this EDirection4 dir)
        {
            switch (dir)
            {
                case EDirection4.Left: return Vector2.left;
                case EDirection4.Right: return Vector2.right;
                case EDirection4.Up: return Vector2.up;
                case EDirection4.Down: return Vector2.down;
                default: return Vector2.zero;
            }
        }

        /// <summary>
        /// EDirection4 열거형 값을 Vector2Int로 변환합니다. <br/>
        /// Left는 Vector2Int(-1, 0), Right는 Vector2Int(1, 0), Up은 Vector2Int(0, 1), Down은 Vector2Int(0, -1)을 반환합니다. <br/>
        /// </summary>
        /// <returns>변환된 Vector2Int 값</returns>
        public static Vector2Int ToVector2Int(this EDirection4 dir)
        {
            switch (dir)
            {
                case EDirection4.Left: return new Vector2Int(-1, 0);
                case EDirection4.Right: return new Vector2Int(1, 0);
                case EDirection4.Up: return new Vector2Int(0, 1);
                case EDirection4.Down: return new Vector2Int(0, -1);
                default: return Vector2Int.zero;
            }
        }



        //? EDirection4n Vector 변환 확장 메서드



        /// <summary>
        /// EDirection4n 열거형 값을 Vector2로 변환합니다. <br/>
        /// None은 Vector2.zero, Left는 Vector2.left, Right는 Vector2.right, Up은 Vector2.up, Down은 Vector2.down을 반환합니다. <br/>
        /// </summary>
        /// <returns>변환된 Vector2 값</returns>
        public static Vector2 ToVector2(this EDirection4n dir)
        {
            switch (dir)
            {
                case EDirection4n.Left: return Vector2.left;
                case EDirection4n.Right: return Vector2.right;
                case EDirection4n.Up: return Vector2.up;
                case EDirection4n.Down: return Vector2.down;
                default: return Vector2.zero;
            }
        }

        /// <summary>
        /// EDirection4n 열거형 값을 Vector2Int로 변환합니다. <br/>
        /// None은 Vector2Int.zero, Left는 Vector2Int(-1, 0), Right는 Vector2Int(1, 0), Up은 Vector2Int(0, 1), Down은 Vector2Int(0, -1)을 반환합니다. <br/>
        /// </summary>
        /// <returns>변환된 Vector2Int 값</returns>
        public static Vector2Int ToVector2Int(this EDirection4n dir)
        {
            switch (dir)
            {
                case EDirection4n.Left: return new Vector2Int(-1, 0);
                case EDirection4n.Right: return new Vector2Int(1, 0);
                case EDirection4n.Up: return new Vector2Int(0, 1);
                case EDirection4n.Down: return new Vector2Int(0, -1);
                default: return Vector2Int.zero;
            }
        }



        //? EDirection4nWithSingle Vector 변환 확장 메서드



        /// <summary>
        /// EDirection4nWithSingle 열거형 값을 Vector2로 변환합니다. <br/>
        /// None은 Vector2.zero, Left 또는 LeftSingle은 Vector2.left, Right 또는 RightSingle은 Vector2.right, <br/>
        /// Up 또는 UpSingle은 Vector2.up, Down 또는 DownSingle은 Vector2.down을 반환합니다. <br/>
        /// </summary>
        /// <returns>변환된 Vector2 값</returns>
        public static Vector2 ToVector2(this EDirection4nWithSingle dir)
        {
            switch (dir)
            {
                case EDirection4nWithSingle.Left:
                case EDirection4nWithSingle.LeftSingle:
                return Vector2.left;
                case EDirection4nWithSingle.Right:
                case EDirection4nWithSingle.RightSingle:
                return Vector2.right;
                case EDirection4nWithSingle.Up:
                case EDirection4nWithSingle.UpSingle:
                return Vector2.up;
                case EDirection4nWithSingle.Down:
                case EDirection4nWithSingle.DownSingle:
                return Vector2.down;
                default:
                return Vector2.zero;
            }
        }

        /// <summary>
        /// EDirection4nWithSingle 열거형 값을 Vector2Int로 변환합니다. <br/>
        /// None은 Vector2Int.zero, Left 또는 LeftSingle은 Vector2Int(-1, 0), Right 또는 RightSingle은 Vector2Int(1, 0), <br/>
        /// Up 또는 UpSingle은 Vector2Int(0, 1), Down 또는 DownSingle은 Vector2Int(0, -1)을 반환합니다. <br/>
        /// </summary>
        /// <returns>변환된 Vector2Int 값</returns>
        public static Vector2Int ToVector2Int(this EDirection4nWithSingle dir)
        {
            switch (dir)
            {
                case EDirection4nWithSingle.Left:
                case EDirection4nWithSingle.LeftSingle:
                return new Vector2Int(-1, 0);
                case EDirection4nWithSingle.Right:
                case EDirection4nWithSingle.RightSingle:
                return new Vector2Int(1, 0);
                case EDirection4nWithSingle.Up:
                case EDirection4nWithSingle.UpSingle:
                return new Vector2Int(0, 1);
                case EDirection4nWithSingle.Down:
                case EDirection4nWithSingle.DownSingle:
                return new Vector2Int(0, -1);
                default:
                return Vector2Int.zero;
            }
        }



        //? EDirection8 Vector 변환 확장 메서드



        /// <summary>
        /// EDirection8 열거형 값을 Vector2로 변환합니다. <br/>
        /// Left, Right, Up, Down은 각각 Vector2.left, Vector2.right, Vector2.up, Vector2.down을 반환하며, <br/>
        /// 대각선 방향일 경우 useHalfDiagonal 파라미터에 따라 (0.5f, 0.5f) 또는 (1f, 1f) 형태로 반환합니다. <br/>
        /// </summary>
        /// <param name="useHalfDiagonal">
        /// 대각선 벡터를 (0.5f, 0.5f) 형태로 반환할지 여부입니다. (false인 경우 (1f, 1f)로 반환) <br/>
        /// </param>
        /// <returns>변환된 Vector2 값</returns>
        public static Vector2 ToVector2(this EDirection8 dir, bool useHalfDiagonal = false)
        {
            switch (dir)
            {
                case EDirection8.Left: return Vector2.left;
                case EDirection8.Right: return Vector2.right;
                case EDirection8.Up: return Vector2.up;
                case EDirection8.Down: return Vector2.down;
                case EDirection8.LeftDown: return useHalfDiagonal ? new Vector2(-0.5f, -0.5f) : new Vector2(-1f, -1f);
                case EDirection8.LeftUp: return useHalfDiagonal ? new Vector2(-0.5f, 0.5f) : new Vector2(-1f, 1f);
                case EDirection8.RightDown: return useHalfDiagonal ? new Vector2(0.5f, -0.5f) : new Vector2(1f, -1f);
                case EDirection8.RightUp: return useHalfDiagonal ? new Vector2(0.5f, 0.5f) : new Vector2(1f, 1f);
                default: return Vector2.zero;
            }
        }

        /// <summary>
        /// EDirection8 열거형 값을 Vector2Int로 변환합니다. <br/>
        /// 대각선의 경우 무조건 정수값인 (1, 1) 형태로 반환합니다. (Vector2Int는 0.5f를 사용할 수 없습니다.) <br/>
        /// </summary>
        /// <returns>변환된 Vector2Int 값</returns>
        public static Vector2Int ToVector2Int(this EDirection8 dir)
        {
            switch (dir)
            {
                case EDirection8.Left: return new Vector2Int(-1, 0);
                case EDirection8.Right: return new Vector2Int(1, 0);
                case EDirection8.Up: return new Vector2Int(0, 1);
                case EDirection8.Down: return new Vector2Int(0, -1);
                case EDirection8.LeftDown: return new Vector2Int(-1, -1);
                case EDirection8.LeftUp: return new Vector2Int(-1, 1);
                case EDirection8.RightDown: return new Vector2Int(1, -1);
                case EDirection8.RightUp: return new Vector2Int(1, 1);
                default: return Vector2Int.zero;
            }
        }



        //? EDirection8n Vector 변환 확장 메서드



        /// <summary>
        /// EDirection8n 열거형 값을 Vector2로 변환합니다. <br/>
        /// None은 Vector2.zero로 반환되며, 그 외 값은 EDirection8과 동일하게 변환됩니다. <br/>
        /// 대각선 방향일 경우 useHalfDiagonal 파라미터에 따라 (0.5f, 0.5f) 또는 (1f, 1f) 형태로 반환합니다. <br/>
        /// </summary>
        /// <param name="useHalfDiagonal">
        /// 대각선 벡터를 (0.5f, 0.5f) 형태로 반환할지 여부입니다. (false인 경우 (1f, 1f)로 반환) <br/>
        /// </param>
        /// <returns>변환된 Vector2 값</returns>
        public static Vector2 ToVector2(this EDirection8n dir, bool useHalfDiagonal = false)
        {
            switch (dir)
            {
                case EDirection8n.Left: return Vector2.left;
                case EDirection8n.Right: return Vector2.right;
                case EDirection8n.Up: return Vector2.up;
                case EDirection8n.Down: return Vector2.down;
                case EDirection8n.LeftDown: return useHalfDiagonal ? new Vector2(-0.5f, -0.5f) : new Vector2(-1f, -1f);
                case EDirection8n.LeftUp: return useHalfDiagonal ? new Vector2(-0.5f, 0.5f) : new Vector2(-1f, 1f);
                case EDirection8n.RightDown: return useHalfDiagonal ? new Vector2(0.5f, -0.5f) : new Vector2(1f, -1f);
                case EDirection8n.RightUp: return useHalfDiagonal ? new Vector2(0.5f, 0.5f) : new Vector2(1f, 1f);
                default: return Vector2.zero;
            }
        }

        /// <summary>
        /// EDirection8n 열거형 값을 Vector2Int로 변환합니다. <br/>
        /// None은 Vector2Int.zero로 반환되며, 대각선의 경우 무조건 정수값인 (1, 1) 형태로 반환합니다. (Vector2Int는 0.5f를 사용할 수 없습니다.) <br/>
        /// </summary>
        /// <returns>변환된 Vector2Int 값</returns>
        public static Vector2Int ToVector2Int(this EDirection8n dir)
        {
            switch (dir)
            {
                case EDirection8n.Left: return new Vector2Int(-1, 0);
                case EDirection8n.Right: return new Vector2Int(1, 0);
                case EDirection8n.Up: return new Vector2Int(0, 1);
                case EDirection8n.Down: return new Vector2Int(0, -1);
                case EDirection8n.LeftDown: return new Vector2Int(-1, -1);
                case EDirection8n.LeftUp: return new Vector2Int(-1, 1);
                case EDirection8n.RightDown: return new Vector2Int(1, -1);
                case EDirection8n.RightUp: return new Vector2Int(1, 1);
                default: return Vector2Int.zero;
            }
        }



        #endregion



        ///======================================================================================================================================================



        //? 기타


        /// <summary>받아온 WayLR이 왼쪽이면 -1, 오른쪽이면 1 반환</summary>
        /// <param name="lr">WayLR값</param>
        /// <param name="reverse">반대로 리버스!</param>
        public static int ConvertWayLRMultiple(EDirectionLR lr, bool reverse = false)
        {
            switch (lr)
            {
                case EDirectionLR.Left: return !reverse ? 1 : -1;
                case EDirectionLR.Right: return !reverse ? -1 : 1;
                default: return 0;
            }
        }



        ///======================================================================================================================================================



        #endregion



        ///======================================================================================================================================================



        /// <summary>
        /// 4개의 bool 값을 받아 DirectionFlags를 반환
        /// </summary>
        /// <param name="down">하단 활성화 여부</param>
        /// <param name="up">상단 활성화 여부</param>
        /// <param name="left">좌측 활성화 여부</param>
        /// <param name="right">우측 활성화 여부</param>
        /// <returns>활성화된 방향에 해당하는 DirectionFlags</returns>
        public static EDirectionFlags ToDirectionFlags(bool down, bool up, bool left, bool right)
        {
            EDirectionFlags result = EDirectionFlags.None;

            if (down)
            {
                result |= EDirectionFlags.Down;
            }

            if (up)
            {
                result |= EDirectionFlags.Up;
            }

            if (left)
            {
                result |= EDirectionFlags.Left;
            }

            if (right)
            {
                result |= EDirectionFlags.Right;
            }

            return result;
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================



    //? 방향 열거형



    /// <summary>
    /// 좌우 방향을 나타내는 열거형입니다.
    /// </summary>
    public enum EDirectionLR
    {
        /// <summary>왼쪽 방향입니다.</summary>
        Left,
        /// <summary>오른쪽 방향입니다.</summary>
        Right
    }



    /// <summary>
    /// 좌우 방향과 없음(None)을 포함하는 열거형입니다.
    /// </summary>
    public enum EDirectionLRn
    {
        /// <summary>방향 없음입니다.</summary>
        None,
        /// <summary>왼쪽 방향입니다.</summary>
        Left,
        /// <summary>오른쪽 방향입니다.</summary>
        Right
    }



    /// <summary>
    /// 상하 방향을 나타내는 열거형입니다.
    /// </summary>
    public enum EDirectionUD
    {
        /// <summary>방향 없음입니다.</summary>
        None,
        /// <summary>위쪽 방향입니다.</summary>
        Up,
        /// <summary>아래쪽 방향입니다.</summary>
        Down
    }



    /// <summary>
    /// 상하 방향을 나타내는 열거형입니다.
    /// </summary>
    public enum EDirectionUDn
    {
        None,
        /// <summary>위쪽 방향입니다.</summary>
        Up,
        /// <summary>아래쪽 방향입니다.</summary>
        Down
    }



    /// <summary>
    /// 4방향(좌, 우, 상, 하)을 나타내는 열거형입니다.
    /// </summary>
    public enum EDirection4
    {
        /// <summary>왼쪽 방향입니다.</summary>
        [LabelText("←좌측")] Left,
        /// <summary>오른쪽 방향입니다.</summary>
        [LabelText("→우측")] Right,
        /// <summary>아래쪽 방향입니다.</summary>
        [LabelText("↓하단")] Down,
        /// <summary>위쪽 방향입니다.</summary>
        [LabelText("↑상단")] Up
    }



    /// <summary>
    /// 4방향(좌, 우, 상, 하)을 나타내는 열거형입니다.
    /// </summary>
    public enum EDirection4n
    {
        /// <summary>방향 없음입니다.</summary>
        None,
        /// <summary>왼쪽 방향입니다.</summary>
        Left,
        /// <summary>오른쪽 방향입니다.</summary>
        Right,
        /// <summary>아래쪽 방향입니다.</summary>
        Down,
        /// <summary>위쪽 방향입니다.</summary>
        Up
    }



    ///<summary>
    /// 4방향(좌, 우, 상, 하) 및 단일 축 방향을 포함한 열거형입니다.
    /// </summary>
    public enum EDirection4nWithSingle
    {
        /// <summary>방향 없음입니다.</summary>
        None,
        /// <summary>왼쪽 방향입니다.</summary>
        Left,
        /// <summary>오른쪽 방향입니다.</summary>
        Right,
        /// <summary>아래쪽 방향입니다.</summary>
        Down,
        /// <summary>위쪽 방향입니다.</summary>
        Up,
        /// <summary>단일 왼쪽 방향입니다.</summary>
        LeftSingle,
        /// <summary>단일 오른쪽 방향입니다.</summary>
        RightSingle,
        /// <summary>단일 아래쪽 방향입니다.</summary>
        DownSingle,
        /// <summary>단일 위쪽 방향입니다.</summary>
        UpSingle
    }



    /// <summary>
    /// 8방향(좌, 우, 상, 하, 대각선)을 포함한 열거형입니다.
    /// </summary>
    public enum EDirection8
    {
        /// <summary>왼쪽 방향입니다.</summary>
        Left,
        /// <summary>오른쪽 방향입니다.</summary>
        Right,
        /// <summary>아래쪽 방향입니다.</summary>
        Down,
        /// <summary>위쪽 방향입니다.</summary>
        Up,
        /// <summary>왼쪽 아래 방향입니다.</summary>
        LeftDown,
        /// <summary>왼쪽 위 방향입니다.</summary>
        LeftUp,
        /// <summary>오른쪽 아래 방향입니다.</summary>
        RightDown,
        /// <summary>오른쪽 위 방향입니다.</summary>
        RightUp
    }



    /// <summary>
    /// 8방향(좌, 우, 상, 하, 대각선)을 포함한 열거형입니다.
    /// </summary>
    public enum EDirection8n
    {
        /// <summary>방향 없음입니다.</summary>
        None,
        /// <summary>왼쪽 방향입니다.</summary>
        Left,
        /// <summary>오른쪽 방향입니다.</summary>
        Right,
        /// <summary>아래쪽 방향입니다.</summary>
        Down,
        /// <summary>위쪽 방향입니다.</summary>
        Up,
        /// <summary>왼쪽 아래 방향입니다.</summary>
        LeftDown,
        /// <summary>왼쪽 위 방향입니다.</summary>
        LeftUp,
        /// <summary>오른쪽 아래 방향입니다.</summary>
        RightDown,
        /// <summary>오른쪽 위 방향입니다.</summary>
        RightUp
    }



    ///======================================================================================================================================================



    //? 성능 특화 4방향



    /// <summary>
    /// BitFlag를 사용한 방향 플래그 열거형
    /// </summary>
    [Flags]
    public enum EDirectionFlags
    {
        /// <summary>
        /// 방향 없음 (0)
        /// </summary>
        None = 0,

        /// <summary>
        /// 하단 방향 (1 ＜＜ 0)
        /// </summary>
        Down = 1 << 0,

        /// <summary>
        /// 상단 방향 (1 ＜＜ 1)
        /// </summary>
        Up = 1 << 1,

        /// <summary>
        /// 좌측 방향 (1 ＜＜ 2)
        /// </summary>
        Left = 1 << 2,

        /// <summary>
        /// 우측 방향 (1 ＜＜ 3)
        /// </summary>
        Right = 1 << 3,

        /// <summary>
        /// 하단 + 상단 (Down | Up)
        /// </summary>
        DownUp = Down | Up,

        /// <summary>
        /// 하단 + 좌측 (Down | Left)
        /// </summary>
        DownLeft = Down | Left,

        /// <summary>
        /// 하단 + 우측 (Down | Right)
        /// </summary>
        DownRight = Down | Right,

        /// <summary>
        /// 상단 + 좌측 (Up | Left)
        /// </summary>
        UpLeft = Up | Left,

        /// <summary>
        /// 상단 + 우측 (Up | Right)
        /// </summary>
        UpRight = Up | Right,

        /// <summary>
        /// 좌측 + 우측 (Left | Right)
        /// </summary>
        LeftRight = Left | Right,

        /// <summary>
        /// 하단 + 상단 + 좌측 (Down | Up | Left)
        /// </summary>
        DownUpLeft = Down | Up | Left,

        /// <summary>
        /// 하단 + 상단 + 우측 (Down | Up | Right)
        /// </summary>
        DownUpRight = Down | Up | Right,

        /// <summary>
        /// 하단 + 좌측 + 우측 (Down | Left | Right)
        /// </summary>
        DownLeftRight = Down | Left | Right,

        /// <summary>
        /// 상단 + 좌측 + 우측 (Up | Left | Right)
        /// </summary>
        UpLeftRight = Up | Left | Right,

        /// <summary>
        /// 모든 방향 (Down | Up | Left | Right)
        /// </summary>
        All = Down | Up | Left | Right
    }



    /// <summary>
    /// 네 방향(Down, Up, Left, Right)에 대한 두 개의 정수 값을 저장하는 구조체입니다.
    /// </summary>
    public struct Directional4DoubleIntValues
    {
        /// <summary>
        /// 아래(Down) 방향의 첫 번째 값입니다.
        /// </summary>
        public int DownValueA;

        /// <summary>
        /// 아래(Down) 방향의 두 번째 값입니다.
        /// </summary>
        public int DownValueB;

        /// <summary>
        /// 위(Up) 방향의 첫 번째 값입니다.
        /// </summary>
        public int UpValueA;

        /// <summary>
        /// 위(Up) 방향의 두 번째 값입니다.
        /// </summary>
        public int UpValueB;

        /// <summary>
        /// 왼쪽(Left) 방향의 첫 번째 값입니다.
        /// </summary>
        public int LeftValueA;

        /// <summary>
        /// 왼쪽(Left) 방향의 두 번째 값입니다.
        /// </summary>
        public int LeftValueB;

        /// <summary>
        /// 오른쪽(Right) 방향의 첫 번째 값입니다.
        /// </summary>
        public int RightValueA;

        /// <summary>
        /// 오른쪽(Right) 방향의 두 번째 값입니다.
        /// </summary>
        public int RightValueB;

        /// <summary>
        /// 네 방향(Down, Up, Left, Right)의 값들을 초기화하는 생성자입니다.
        /// </summary>
        /// <param name="downValueA">아래(Down) 방향의 첫 번째 값</param>
        /// <param name="downValueB">아래(Down) 방향의 두 번째 값</param>
        /// <param name="upValueA">위(Up) 방향의 첫 번째 값</param>
        /// <param name="upValueB">위(Up) 방향의 두 번째 값</param>
        /// <param name="leftValueA">왼쪽(Left) 방향의 첫 번째 값</param>
        /// <param name="leftValueB">왼쪽(Left) 방향의 두 번째 값</param>
        /// <param name="rightValueA">오른쪽(Right) 방향의 첫 번째 값</param>
        /// <param name="rightValueB">오른쪽(Right) 방향의 두 번째 값</param>
        public Directional4DoubleIntValues(int downValueA, int downValueB,
                                 int upValueA, int upValueB,
                                 int leftValueA, int leftValueB,
                                 int rightValueA, int rightValueB)
        {
            DownValueA = downValueA;
            DownValueB = downValueB;

            UpValueA = upValueA;
            UpValueB = upValueB;

            LeftValueA = leftValueA;
            LeftValueB = leftValueB;

            RightValueA = rightValueA;
            RightValueB = rightValueB;
        }
    }



    ///======================================================================================================================================================
}
