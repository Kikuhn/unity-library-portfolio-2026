using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



//? [Transformation] 주로 "Rect" 들이 들어있는 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    public static class SU_TF_Rect
    {
        ///======================================================================================================================================================



        //? Rect 생성



        /// <summary>
        /// 중심 좌표와 크기를 받아, 중심이 해당 좌표인 Rect를 반환합니다. <br/>
        /// </summary>
        /// <param name="center">중심 좌표</param> <br/>
        /// <param name="size">크기</param> <br/>
        /// <returns>중심점을 기준으로 생성된 Rect</returns> <br/>
        public static Rect RectFromCenter(Vector2 center, Vector2 size)
        {
            float x = center.x - size.x / 2f;
            float y = center.y - size.y / 2f;
            return new Rect(x, y, size.x, size.y);
        }



        /// <summary>
        /// 좌측 하단과 우측 상단 좌표를 받아, 해당 코너들을 포함하는 Rect를 반환합니다. <br/>
        /// </summary>
        /// <param name="bottomLeft">좌측 하단 좌표</param> <br/>
        /// <param name="topRight">우측 상단 좌표</param> <br/>
        /// <returns>코너 좌표를 기준으로 생성된 Rect</returns> <br/>
        public static Rect RectFromCorners(Vector2 bottomLeft, Vector2 topRight)
        {
            float width = topRight.x - bottomLeft.x;
            float height = topRight.y - bottomLeft.y;
            return new Rect(bottomLeft.x, bottomLeft.y, width, height);
        }



        /// <summary>
        /// 좌측 하단(X, Y)과 우측 상단(X, Y) 좌표를 받아, 해당 코너들을 포함하는 Rect를 반환합니다.
        /// </summary>
        /// <param name="bottomLeftX">좌측 하단 X좌표</param>
        /// <param name="bottomLeftY">좌측 하단 Y좌표</param>
        /// <param name="topRightX">우측 상단 X좌표</param>
        /// <param name="topRightY">우측 상단 Y좌표</param>
        /// <returns>코너 좌표를 기준으로 생성된 Rect</returns>
        public static Rect RectFromCorners(float bottomLeftX, float bottomLeftY, float topRightX, float topRightY)
        {
            float width = topRightX - bottomLeftX;
            float height = topRightY - bottomLeftY;
            return new Rect(bottomLeftX, bottomLeftY, width, height);
        }



        /// <summary>
        /// 중심 좌표와 크기를 받아, 중심이 해당 좌표인 RectInt를 반환합니다. <br/>
        /// (Vector2Int는 소수점을 지원하지 않으므로, 결과는 항상 정수 단위입니다.) <br/>
        /// </summary>
        /// <param name="center">중심 좌표 (정수형)</param> <br/>
        /// <param name="size">크기 (정수형)</param> <br/>
        /// <returns>중심점을 기준으로 생성된 RectInt</returns> <br/>
        public static RectInt RectIntFromCenter(Vector2Int center, Vector2Int size)
        {
            int x = center.x - size.x / 2;
            int y = center.y - size.y / 2;
            return new RectInt(x, y, size.x, size.y);
        }



        /// <summary>
        /// 좌측 하단과 우측 상단 좌표를 받아, 해당 코너들을 포함하는 RectInt를 반환합니다. <br/>
        /// (Vector2Int는 소수점을 지원하지 않으므로, 결과는 항상 정수 단위입니다.) <br/>
        /// </summary>
        /// <param name="bottomLeft">좌측 하단 좌표 (정수형)</param> <br/>
        /// <param name="topRight">우측 상단 좌표 (정수형)</param> <br/>
        /// <returns>코너 좌표를 기준으로 생성된 RectInt</returns> <br/>
        public static RectInt RectIntFromCorners(Vector2Int bottomLeft, Vector2Int topRight)
        {
            int width = topRight.x - bottomLeft.x;
            int height = topRight.y - bottomLeft.y;
            return new RectInt(bottomLeft.x, bottomLeft.y, width, height);
        }



        /// <summary>
        /// 좌측 하단(X, Y)과 우측 상단(X, Y) 좌표를 받아, 해당 코너들을 포함하는 RectInt를 반환합니다.
        /// </summary>
        /// <param name="bottomLeftX">좌측 하단 X좌표 (정수형)</param>
        /// <param name="bottomLeftY">좌측 하단 Y좌표 (정수형)</param>
        /// <param name="topRightX">우측 상단 X좌표 (정수형)</param>
        /// <param name="topRightY">우측 상단 Y좌표 (정수형)</param>
        /// <returns>코너 좌표를 기준으로 생성된 RectInt</returns>
        public static RectInt RectIntFromCorners(int bottomLeftX, int bottomLeftY, int topRightX, int topRightY)
        {
            int width = topRightX - bottomLeftX;
            int height = topRightY - bottomLeftY;
            return new RectInt(bottomLeftX, bottomLeftY, width, height);
        }



        ///======================================================================================================================================================



        //? Rect 생성 (심화)



        /// <summary>
        /// 주어진 Rect 목록에서 가장 작은 좌측 하단 좌표와 가장 큰 우측 상단 좌표를 포함하는 Bounding Rect를 계산하여 반환합니다.
        /// </summary>
        /// <param name="rects">Bounding Rect를 계산할 Rect들의 컬렉션입니다.</param>
        /// <returns>모든 Rect를 포함하는 Bounding Rect를 반환합니다. 만약 rects가 비어있거나 null이면 빈 Rect를 반환합니다.</returns>
        public static Rect GetBoundingRect(IEnumerable<Rect> rects)
        {
            if (rects == null) { return new Rect(); }

            using (IEnumerator<Rect> enumerator = rects.GetEnumerator())
            {
                bool hasAny = false;
                float xMin = float.MaxValue;
                float yMin = float.MaxValue;
                float xMax = float.MinValue;
                float yMax = float.MinValue;

                while (enumerator.MoveNext())
                {
                    hasAny = true;
                    Rect rect = enumerator.Current;
                    if (rect.xMin < xMin) xMin = rect.xMin;
                    if (rect.yMin < yMin) yMin = rect.yMin;
                    if (rect.xMax > xMax) xMax = rect.xMax;
                    if (rect.yMax > yMax) yMax = rect.yMax;
                }

                if (!hasAny) { return new Rect(); }
                return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            }
        }



        ///======================================================================================================================================================



        //? Rect 비교



        /// <summary>
        /// 두 개의 Rect를 비교해서, 서로 인접해 있는지 여부를 반환
        /// </summary>
        /// <param name="main">기준이 되는 Rect</param>
        /// <param name="target">비교할 Rect</param>
        /// <param name="mustNotOverlap">두 Rect가 인접해 있을 때, 경계선이 벗어나지 않아야 하는지 여부</param>
        /// <returns>인접 여부</returns>
        public static bool CheckRectsAreClose(this Rect main, in Rect target, bool mustNotOverlap)
        {
            // 두 Rect가 x축에서 겹치는지 확인
            bool overlapX = main.xMin < target.xMax && main.xMax > target.xMin;

            // 두 Rect가 y축에서 겹치는지 확인
            bool overlapY = main.yMin < target.yMax && main.yMax > target.yMin;

            // 두 Rect가 수직으로 인접해 있는지 확인
            bool adjacentVertical = overlapX && (main.yMin == target.yMax || main.yMax == target.yMin);

            // 두 Rect가 수평으로 인접해 있는지 확인
            bool adjacentHorizontal = overlapY && (main.xMin == target.xMax || main.xMax == target.xMin);

            if (mustNotOverlap)
            {
                // 두 Rect의 경계선이 벗어나지 않도록 확인
                bool notOutOfBoundsX = main.xMin <= target.xMin && main.xMax >= target.xMax || target.xMin <= main.xMin && target.xMax >= main.xMax;
                bool notOutOfBoundsY = main.yMin <= target.yMin && main.yMax >= target.yMax || target.yMin <= main.yMin && target.yMax >= main.yMax;

                return (adjacentVertical || adjacentHorizontal) && (notOutOfBoundsX || notOutOfBoundsY);
            }
            else
            {
                return adjacentVertical || adjacentHorizontal;
            }
        }



        /// <summary>
        /// 두 개의 Rect를 비교해서, 서로 인접해 있는지 여부를 반환
        /// </summary>
        /// <param name="main">기준이 되는 Rect</param>
        /// <param name="target">비교할 Rect</param>
        /// <param name="mustNotOverlap">두 Rect가 인접해 있을 때, 경계선이 벗어나지 않아야 하는지 여부</param>
        /// <returns>인접 여부</returns>
        public static bool CheckRectsAreClose(this CustomRect2DCentered main, in CustomRect2DCentered target, bool mustNotOverlap)
        {
            // 두 Rect가 x축에서 겹치는지 확인
            bool overlapX = main.xMin < target.xMax && main.xMax > target.xMin;

            // 두 Rect가 y축에서 겹치는지 확인
            bool overlapY = main.yMin < target.yMax && main.yMax > target.yMin;

            // 두 Rect가 수직으로 인접해 있는지 확인
            bool adjacentVertical = overlapX && (main.yMin == target.yMax || main.yMax == target.yMin);

            // 두 Rect가 수평으로 인접해 있는지 확인
            bool adjacentHorizontal = overlapY && (main.xMin == target.xMax || main.xMax == target.xMin);

            if (mustNotOverlap)
            {
                // 두 Rect의 경계선이 벗어나지 않도록 확인
                bool notOutOfBoundsX = main.xMin <= target.xMin && main.xMax >= target.xMax || target.xMin <= main.xMin && target.xMax >= main.xMax;
                bool notOutOfBoundsY = main.yMin <= target.yMin && main.yMax >= target.yMax || target.yMin <= main.yMin && target.yMax >= main.yMax;

                return (adjacentVertical || adjacentHorizontal) && (notOutOfBoundsX || notOutOfBoundsY);
            }
            else
            {
                return adjacentVertical || adjacentHorizontal;
            }
        }




        /// <summary>
        /// 두 Rect 간의 수평 또는 수직 간격을 계산합니다.
        /// </summary>
        /// <param name="rect">기준 Rect입니다.</param>
        /// <param name="other">비교할 다른 Rect입니다.</param>
        /// <param name="isVertical">true이면 수직 간격을, false이면 수평 간격을 계산합니다.</param>
        /// <returns>두 Rect 간의 간격을 반환합니다. 겹쳐있을 경우 음수 값을 가집니다.</returns>
        public static float GetGap(this Rect rect, Rect other, bool isVertical)
        {
            if (isVertical)
            {
                float delta1 = other.yMin - rect.yMax;
                float delta2 = rect.yMin - other.yMax;
                return Mathf.Max(delta1, delta2);
            }
            else
            {
                float delta1 = other.xMin - rect.xMax;
                float delta2 = rect.xMin - other.xMax;
                return Mathf.Max(delta1, delta2);
            }
        }



        /// <summary>
        /// 두 <see cref="Rect"/> 의 상대적 위치를 계산해
        /// <see cref="EDirection4"/> (Left·Right·Up·Down) 중 하나를 반환합니다.
        /// </summary>
        /// <param name="reference">기준이 되는 Rect</param>
        /// <param name="target">방향을 판정할 Rect</param>
        /// <returns>reference 기준 target 의 방향</returns>
        public static EDirection4 GetRelativeDirection(Rect reference, Rect target)
        {
            //. 좌·우 -----------------------------------------------------------
            if (target.xMax <= reference.xMin) { return EDirection4.Left; }   //! reference 왼쪽
            if (target.xMin >= reference.xMax) { return EDirection4.Right; }  //! reference 오른쪽

            //. 상·하 -----------------------------------------------------------
            if (target.yMin >= reference.yMax) { return EDirection4.Up; }     //! reference 위
            if (target.yMax <= reference.yMin) { return EDirection4.Down; }   //! reference 아래

            //. 경계가 겹치는 경우: 중심 차이로 판별
            Vector2 delta = target.center - reference.center;
            return Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                   ? (delta.x >= 0 ? EDirection4.Right : EDirection4.Left)
                   : (delta.y >= 0 ? EDirection4.Up : EDirection4.Down);
        }



        ///======================================================================================================================================================



        //? Rect 확장



        /// <summary>
        /// 주어진 Rect의 크기를 중심점을 기준으로 확장합니다.
        /// </summary>
        /// <param name="rect">확장할 Rect입니다.</param>
        /// <param name="sizeIncrease">확장할 크기입니다.</param>
        public static void ExpandFromCenterRef(this ref Rect rect, Vector2 sizeIncrease)
        {
            Vector2 currentCenter = rect.center;
            Vector2 newSize = rect.size + sizeIncrease;
            rect.size = newSize;
            rect.center = currentCenter;
        }



        /// <summary>
        /// 주어진 Rect의 크기를 중심점을 기준으로 확장합니다.
        /// </summary>
        /// <param name="rect">확장할 Rect입니다.</param>
        /// <param name="x">확장할 너비입니다.</param>
        /// <param name="y">확장할 높이입니다.</param>
        public static void ExpandFromCenterRef(this ref Rect rect, float x, float y)
        {
            ExpandFromCenterRef(ref rect, new Vector2(x, y));
        }



        /// <summary>
        /// 주어진 Rect의 크기를 중심점을 기준으로 확장합니다.
        /// </summary>
        /// <param name="rect">확장할 Rect입니다.</param>
        /// <param name="sizeIncrease">확장할 크기입니다.</param>
        public static Rect ExpandFromCenter(this Rect rect, Vector2 sizeIncrease)
        {
            Vector2 currentCenter = rect.center;
            Vector2 newSize = rect.size + sizeIncrease;
            rect.size = newSize;
            rect.center = currentCenter;
            return rect;
        }



        /// <summary>
        /// 주어진 Rect의 크기를 중심점을 기준으로 확장합니다.
        /// </summary>
        /// <param name="rect">확장할 Rect입니다.</param>
        /// <param name="x">확장할 너비입니다.</param>
        /// <param name="y">확장할 높이입니다.</param>
        public static Rect ExpandFromCenter(this Rect rect, float x, float y)
        {
            return ExpandFromCenter(rect, new Vector2(x, y));
        }



        /// <summary>
        /// 지정된 기준점을 중심으로 Rect의 크기를 확장합니다.
        /// </summary>
        /// <param name="rect">확장할 Rect</param>
        /// <param name="standard">확장의 기준점</param>
        /// <param name="widthDelta">추가할 너비</param>
        /// <param name="heightDelta">추가할 높이</param>
        /// <returns>확장된 Rect</returns>
        public static Rect ExpandRef(this ref Rect rect, ECenterStandard standard, float widthDelta, float heightDelta)
        {
            float newWidth = rect.width + widthDelta;
            float newHeight = rect.height + heightDelta;

            float x = rect.x;
            float y = rect.y;

            switch (standard)
            {
                case ECenterStandard.MiddleCenter:
                x -= widthDelta / 2;
                y -= heightDelta / 2;
                break;
                case ECenterStandard.MiddleLeft:
                y -= heightDelta / 2;
                break;
                case ECenterStandard.MiddleRight:
                x -= widthDelta;
                y -= heightDelta / 2;
                break;
                case ECenterStandard.LowerCenter:
                x -= widthDelta / 2;
                break;
                case ECenterStandard.LowerLeft:
                break;
                case ECenterStandard.LowerRight:
                x -= widthDelta;
                break;
                case ECenterStandard.UpperCenter:
                x -= widthDelta / 2;
                y -= heightDelta;
                break;
                case ECenterStandard.UpperLeft:
                y -= heightDelta;
                break;
                case ECenterStandard.UpperRight:
                x -= widthDelta;
                y -= heightDelta;
                break;
            }

            return new Rect(x, y, newWidth, newHeight);
        }



        ///======================================================================================================================================================



        //? Rect 좌표



        /// <summary>
        /// Rect의 특정 위치에 대한 좌표를 반환하는 확장 메서드
        /// </summary>
        /// <param name="rect">확장 메서드를 호출하는 Rect</param>
        /// <param name="standard">기준 위치</param>
        /// <returns>기준 위치의 좌표</returns>
        public static Vector2 GetPosition(this Rect rect, ECenterStandard standard)
        {
            switch (standard)
            {
                case ECenterStandard.MiddleCenter:
                return new Vector2(rect.x + rect.width / 2, rect.y + rect.height / 2);
                case ECenterStandard.MiddleLeft:
                return new Vector2(rect.x, rect.y + rect.height / 2);
                case ECenterStandard.MiddleRight:
                return new Vector2(rect.x + rect.width, rect.y + rect.height / 2);
                case ECenterStandard.LowerCenter:
                return new Vector2(rect.x + rect.width / 2, rect.y);
                case ECenterStandard.LowerLeft:
                return new Vector2(rect.x, rect.y);
                case ECenterStandard.LowerRight:
                return new Vector2(rect.x + rect.width, rect.y);
                case ECenterStandard.UpperCenter:
                return new Vector2(rect.x + rect.width / 2, rect.y + rect.height);
                case ECenterStandard.UpperLeft:
                return new Vector2(rect.x, rect.y + rect.height);
                case ECenterStandard.UpperRight:
                return new Vector2(rect.x + rect.width, rect.y + rect.height);
                default:
                return Vector2.zero;
            }
        }



        ///======================================================================================================================================================



        //? Rect 좌표 확인



        /// <summary>
        /// 주어진 <paramref name="point"/> 가 <paramref name="rect"/> 내부에 존재하는지 확인합니다.
        /// </summary>
        /// <param name="rect">검사 대상 <see cref="Rect"/>.</param>
        /// <param name="point">Rect 안에 포함되는지 확인할 <see cref="Vector2"/> 좌표.</param>
        /// <param name="includeEdge">
        /// 테두리(경계선)를 내부로 인정할지 여부.<br/>
        /// <c>true</c> : 경계선 위의 점도 내부로 간주 (기본값).<br/>
        /// <c>false</c> : 경계선 위의 점은 제외.
        /// </param>
        /// <returns>포함되면 <c>true</c>, 아니면 <c>false</c>.</returns>
        public static bool ContainsPoint(this Rect rect, Vector2 point, bool includeEdge = true)
        {
            //. Rect 경계 값 (xMin은 항상 xMax 이하, yMin은 yMax 이하로 정규화됨)
            float xMin = rect.xMin;
            float xMax = rect.xMax;
            float yMin = rect.yMin;
            float yMax = rect.yMax;

            //! includeEdge 값에 따라 비교 연산자 결정
            if (includeEdge)
            {
                //? 경계 포함
                return point.x >= xMin && point.x <= xMax &&
                       point.y >= yMin && point.y <= yMax;
            }

            //? 경계 제외
            return point.x > xMin && point.x < xMax &&
                   point.y > yMin && point.y < yMax;
        }



        ///======================================================================================================================================================




        //? Rect 연산



        /// <summary>
        /// 두 Rect가 겹치는 영역을 계산하여 반환합니다.
        /// 겹치지 않을 경우 <c>Rect.zero</c>를 반환합니다.
        /// </summary>
        /// <param name="a">Rect A.</param>
        /// <param name="b">Rect B.</param>
        /// <returns>겹치는 영역 Rect, 없으면 <c>Rect.zero</c>.</returns>
        public static Rect GetIntersectionRect(Rect a, Rect b)
        {
            //. 교차 영역의 최소/최대 좌표 계산
            float xMin = Mathf.Max(a.xMin, b.xMin);
            float xMax = Mathf.Min(a.xMax, b.xMax);
            float yMin = Mathf.Max(a.yMin, b.yMin);
            float yMax = Mathf.Min(a.yMax, b.yMax);

            //! 겹치는 부분이 없는 경우
            if (xMax <= xMin || yMax <= yMin)
            {
                return Rect.zero;
            }

            //? 계산된 좌표로 교차 Rect 생성
            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }



        /// <summary>
        /// A에서 B를 제거(차집합)한 결과를 <see cref="Rect"/> 리스트로 반환합니다.
        /// 교차 형태에 따라 최대 4개의 Rect가 생성됩니다.
        /// 교차가 없으면 A 한 개가 반환되고,
        /// A가 B에 완전히 포함되면 빈 리스트가 반환됩니다.
        /// </summary>
        /// <param name="a">기준이 되는 큰(또는 같은) Rect.</param>
        /// <param name="b">제거할 Rect.</param>
        /// <returns>A - B 영역을 구성하는 Rect 집합.</returns>
        public static List<Rect> SubtractRect(Rect a, Rect b)
        {
            var result = new List<Rect>();

            //. 교차 영역 확인
            Rect intersection = GetIntersectionRect(a, b);

            //! 교차가 없으면 A 그대로 반환
            if (intersection == Rect.zero)
            {
                result.Add(a);
                return result;
            }

            //! A가 B에 완전히 포함될 경우
            if (intersection.width == a.width && intersection.height == a.height)
            {
                return result; // 빈 리스트
            }

            //. 교차 경계 좌표
            float left = a.xMin;
            float right = a.xMax;
            float bottom = a.yMin;
            float top = a.yMax;

            //. 교차 영역 좌표
            float ixMin = intersection.xMin;
            float ixMax = intersection.xMax;
            float iyMin = intersection.yMin;
            float iyMax = intersection.yMax;

            //? 왼쪽 영역
            float leftWidth = ixMin - left;
            if (leftWidth > 0f)
            {
                result.Add(new Rect(left, iyMin, leftWidth, iyMax - iyMin));
            }

            //? 오른쪽 영역
            float rightWidth = right - ixMax;
            if (rightWidth > 0f)
            {
                result.Add(new Rect(ixMax, iyMin, rightWidth, iyMax - iyMin));
            }

            //? 아래쪽 영역
            float bottomHeight = iyMin - bottom;
            if (bottomHeight > 0f)
            {
                result.Add(new Rect(left, bottom, right - left, bottomHeight));
            }

            //? 위쪽 영역
            float topHeight = top - iyMax;
            if (topHeight > 0f)
            {
                result.Add(new Rect(left, iyMax, right - left, topHeight));
            }

            return result;
        }



        /// <summary>
        /// 기준 Rect <paramref name="a"/>에서 <paramref name="subtractRects"/> 로 지정된
        /// 여러 Rect를 순차적으로 제거(차집합)하여 남은 영역을 반환합니다.
        /// 교차가 전혀 없으면 A 하나만, A가 전부 덮이면 빈 리스트가 반환됩니다.
        /// </summary>
        /// <param name="a">기준이 되는 Rect.</param>
        /// <param name="subtractRects">제거할 Rect 집합.</param>
        /// <returns>A - Σ(subtractRects) 영역 Rect 리스트.</returns>
        public static List<Rect> SubtractRects(Rect a, IEnumerable<Rect> subtractRects)
        {
            //. 초기 결과는 A 하나로 시작
            var working = new List<Rect> { a };

            foreach (Rect b in subtractRects)
            {
                if (working.Count == 0) break; //! 더 남은 영역이 없으면 조기 종료

                var next = new List<Rect>();

                foreach (Rect part in working)
                {
                    //? part에서 b를 제거한 결과 누적
                    next.AddRange(SubtractRect(part, b));
                }

                working = next;
            }

            return working;
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 사각형(Rect)의 이동 및 충돌 처리를 위한 헬퍼 클래스입니다.
    /// </summary>
    public static class SU_RectPlace
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 부동소수점 연산 시 발생할 수 있는 오차를 보정하기 위한 매우 작은 값입니다. <br/>
        /// 이 값은 충돌 계산 및 경계 검사 등에서 사용됩니다.
        /// </summary>
        private const float Epsilon = 0.0001f;



        ///======================================================================================================================================================



        /// <summary>
        /// 이동하는 Rect(movingRect)가 스테이지 경계 내에서 장애물들과 충돌 없이 이동할 수 있는 <br/>
        /// 최대 이동 비율에 따른 실제 이동 벡터를 계산합니다. <br/>
        /// 내부적으로는 각 장애물과의 충돌 시간을 계산하여 가장 가까운 충돌 시점(tEntry)을 구하고, <br/>
        /// 스테이지 경계와의 충돌 제한도 반영합니다.
        /// </summary>
        /// <param name="movingRect">이동하는 사각형의 현재 위치 및 크기를 나타내는 Rect</param>
        /// <param name="movementVector">이동하려는 방향과 거리를 나타내는 벡터 (예: (1, 0) 또는 (0, -1))</param>
        /// <param name="obstacles">충돌 검사 대상인 다른 Rect들의 컬렉션</param>
        /// <param name="boundingRects">Rect들이 배치될 수 있는 스테이지 전체 경계를 나타내는 Rect</param>
        /// <returns>
        /// 충돌 없이 실제로 이동할 수 있는 벡터. <br/>
        /// 계산된 이동 비율을 이동 벡터에 곱하여 반환합니다.
        /// </returns>
        public static Vector2 CalculateMovement(Rect movingRect, Vector2 movementVector, IEnumerable<Rect> obstacles, Rect boundingRects)
        {
            float tMax = 1.0f; //. 최대 이동 비율 (0~1)
            float vx = movementVector.x;
            float vy = movementVector.y;

            //? 각 장애물과의 충돌 여부를 검사하여, 이동 가능 비율(tMax)을 갱신함
            foreach (Rect obstacle in obstacles)
            {
                if (obstacle.Equals(movingRect))
                    continue; //! 자기 자신은 검사 대상에서 제외

                float tEntry, tExit;
                //? ComputeCollisionTime: movingRect와 장애물 간 충돌 시점을 계산
                if (ComputeCollisionTime(movingRect, movementVector, obstacle, out tEntry, out tExit))
                {
                    //? tEntry가 유효한 범위 내(0~1)라면 가장 가까운 충돌 시점을 선택
                    if (tEntry >= 0 && tEntry <= 1)
                    {
                        tMax = Mathf.Min(tMax, tEntry);
                    }
                }
                else
                {
                    //? 충돌이 없더라도, 두 Rect가 매우 근접해 있는 경우를 확인
                    if (IsRectNear(movingRect, obstacle, Epsilon))
                    {
                        //? 이동 방향에 따라 인접하면 이동을 차단함
                        if ((vx > 0 && movingRect.xMax <= obstacle.xMin) ||
                            (vx < 0 && movingRect.xMin >= obstacle.xMax))
                        {
                            tMax = 0f;
                        }
                        if ((vy > 0 && movingRect.yMax <= obstacle.yMin) ||
                            (vy < 0 && movingRect.yMin >= obstacle.yMax))
                        {
                            tMax = 0f;
                        }
                    }
                }
            }

            //? 스테이지 경계와의 충돌 검사: 경계를 넘어가지 않도록 이동 가능 비율을 제한
            float tMaxStage = ComputeStageCollisionTime(movingRect, movementVector, boundingRects);
            tMax = Mathf.Min(tMax, tMaxStage);

            //? 최종 이동 가능한 벡터 계산: 이동 벡터의 각 성분에 최대 이동 비율을 곱함
            Vector2 actualMovement = new Vector2(vx * tMax, vy * tMax);
            return actualMovement;
        }



        /// <summary>
        /// 이동하는 Rect(movingRect)가 특정 방향(EDirection4)으로 이동할 때, <br/>
        /// 다른 Rect와 겹치지 않는 충돌 경계(Edge) 좌표를 반환합니다. <br/>
        /// 충돌이 없으면, 이동 방향에 따라 무한대(Infinity) 값을 반환합니다. <br/>
        /// 예를 들어, 위쪽 이동의 경우 기본값은 (movingRect.center.x, +Infinity)이며, <br/>
        /// 장애물과 충돌 시 장애물의 yMin 값으로 대체됩니다.
        /// </summary>
        /// <param name="movingRect">이동하는 사각형의 Rect</param>
        /// <param name="allRoomRects">충돌 검사 대상인 다른 Rect들의 컬렉션</param>
        /// <param name="boundingRects">스테이지 경계를 나타내는 Rect</param>
        /// <param name="direction">이동 방향 (EDirection4: Up, Down, Left, Right)</param>
        /// <returns>
        /// 충돌 경계 좌표. <br/>
        /// 충돌이 없으면 해당 방향의 무한대 값을 포함하는 벡터를 반환합니다.
        /// </returns>
        public static Vector2 GetCollisionEdgePoint(
          Rect movingRect,
          IEnumerable<Rect> allRoomRects,
          Rect boundingRects,
          EDirection4 direction)
        {
            //? 충돌이 없을 경우 기본 경계를 무한대로 설정 (방향에 따라)
            Vector2 collisionEdge = direction switch
            {
                EDirection4.Up => new Vector2(movingRect.center.x, Mathf.Infinity),
                EDirection4.Down => new Vector2(movingRect.center.x, Mathf.NegativeInfinity),
                EDirection4.Left => new Vector2(Mathf.NegativeInfinity, movingRect.center.y),
                EDirection4.Right => new Vector2(Mathf.Infinity, movingRect.center.y),
                _ => Vector2.zero
            };

            //? 모든 장애물에 대해 충돌 여부와 경계 좌표를 계산
            foreach (var obstacle in allRoomRects)
            {
                if (obstacle.Equals(movingRect))
                    continue; //! 자기 자신은 검사에서 제외

                switch (direction)
                {
                    case EDirection4.Up:
                    //? 위쪽 이동: movingRect의 yMax가 장애물의 yMin보다 작고, x축에서 겹침이 있는 경우
                    if (movingRect.yMax <= obstacle.yMin && movingRect.xMax > obstacle.xMin && movingRect.xMin < obstacle.xMax)
                    {
                        float edgeY = obstacle.yMin;
                        if (edgeY < collisionEdge.y)
                            collisionEdge = new Vector2(movingRect.center.x, edgeY);
                    }
                    break;
                    case EDirection4.Down:
                    //? 아래쪽 이동: movingRect의 yMin이 장애물의 yMax보다 크고, x축 겹침이 있는 경우
                    if (movingRect.yMin >= obstacle.yMax && movingRect.xMax > obstacle.xMin && movingRect.xMin < obstacle.xMax)
                    {
                        float edgeY = obstacle.yMax;
                        if (edgeY > collisionEdge.y)
                            collisionEdge = new Vector2(movingRect.center.x, edgeY);
                    }
                    break;
                    case EDirection4.Left:
                    //? 왼쪽 이동: movingRect의 xMin이 장애물의 xMax보다 크고, y축 겹침이 있는 경우
                    if (movingRect.xMin >= obstacle.xMax && movingRect.yMax > obstacle.yMin && movingRect.yMin < obstacle.yMax)
                    {
                        float edgeX = obstacle.xMax;
                        if (edgeX > collisionEdge.x)
                            collisionEdge = new Vector2(edgeX, movingRect.center.y);
                    }
                    break;
                    case EDirection4.Right:
                    //? 오른쪽 이동: movingRect의 xMax가 장애물의 xMin보다 작고, y축 겹침이 있는 경우
                    if (movingRect.xMax <= obstacle.xMin && movingRect.yMax > obstacle.yMin && movingRect.yMin < obstacle.yMax)
                    {
                        float edgeX = obstacle.xMin;
                        if (edgeX < collisionEdge.x)
                            collisionEdge = new Vector2(edgeX, movingRect.center.y);
                    }
                    break;
                }
            }

            //? 스테이지 경계와의 충돌 검사: 경계를 넘어가지 않도록 충돌 경계값을 조정
            switch (direction)
            {
                case EDirection4.Up:
                if (boundingRects.yMax < collisionEdge.y)
                    collisionEdge = new Vector2(movingRect.center.x, boundingRects.yMax);
                break;
                case EDirection4.Down:
                if (boundingRects.yMin > collisionEdge.y)
                    collisionEdge = new Vector2(movingRect.center.x, boundingRects.yMin);
                break;
                case EDirection4.Left:
                if (boundingRects.xMin > collisionEdge.x)
                    collisionEdge = new Vector2(boundingRects.xMin, movingRect.center.y);
                break;
                case EDirection4.Right:
                if (boundingRects.xMax < collisionEdge.x)
                    collisionEdge = new Vector2(boundingRects.xMax, movingRect.center.y);
                break;
            }

            return collisionEdge;
        }



        /// <summary>
        /// 이동하는 Rect(movingRect)가 지정된 방향(EDirection4)으로 이동할 때, <br/>
        /// 충돌한 장애물(Rect)들 중 가장 가까운 충돌 경계(Edge) 좌표를 계산하여 반환합니다. <br/>
        /// 만약 충돌하는 장애물이 없다면, 해당 방향에 따라 무한대(Infinity) 값을 포함하는 좌표를 반환합니다. <br/>
        /// 예를 들어, 위쪽(Up) 이동의 경우 기본값은 (movingRect.center.x, +Infinity)입니다.
        /// </summary>
        /// <param name="movingRect">이동 중인 Rect</param>
        /// <param name="allRoomRects">충돌 검사 대상인 다른 Rect들의 컬렉션</param>
        /// <param name="boundingRects">Rect들이 배치될 수 있는 스테이지 전체 경계를 나타내는 Rect</param>
        /// <param name="direction">이동 방향 (EDirection4: Up, Down, Left, Right)</param>
        /// <param name="isObstacleLeftOrBelow">
        /// 충돌한 장애물이 이동 Rect에 대해 왼쪽 또는 아래쪽에 위치하면 true, 오른쪽 또는 위쪽에 위치하면 false를 반환합니다. <br/>
        /// (이 값은 충돌 좌표를 계산할 때 상대적인 위치 판단에 사용됩니다.)
        /// </param>
        /// <returns>
        /// 계산된 충돌 경계 좌표. <br/>
        /// 충돌하는 장애물이 없으면 지정된 방향에 따라 무한대(Infinity) 값을 포함하는 벡터를 반환합니다.
        /// </returns>
        public static Vector2 GetCollisionRectEdge(
            Rect movingRect,
            IEnumerable<Rect> allRoomRects,
            Rect boundingRects,
            EDirection4 direction,
            out bool isObstacleLeftOrBelow)
        {
            // 기본값 설정: 충돌이 없을 경우, 방향에 따라 무한대 값으로 초기화합니다.
            // 예를 들어, Up 방향이면 (movingRect.center.x, +Infinity), Down이면 (movingRect.center.x, -Infinity)로 설정합니다.
            Vector2 collisionEdge = direction switch
            {
                EDirection4.Up => new Vector2(movingRect.center.x, Mathf.Infinity),
                EDirection4.Down => new Vector2(movingRect.center.x, Mathf.NegativeInfinity),
                EDirection4.Left => new Vector2(Mathf.NegativeInfinity, movingRect.center.y),
                EDirection4.Right => new Vector2(Mathf.Infinity, movingRect.center.y),
                _ => Vector2.zero
            };

            // 초기값: 아직 장애물의 상대 위치는 결정되지 않음.
            isObstacleLeftOrBelow = false;
            // 가장 가까운 충돌 거리를 무한대로 초기화
            float closestDistance = Mathf.Infinity;

            // 모든 장애물 Rect에 대해 반복 검사
            foreach (var obstacle in allRoomRects)
            {
                if (obstacle.Equals(movingRect))
                    continue; // 자기 자신은 검사하지 않음

                // 방향에 따라 장애물과의 충돌 여부 및 거리를 계산
                switch (direction)
                {
                    case EDirection4.Up:
                    // 위쪽 이동: movingRect의 상단(yMax)이 장애물의 하단(yMin)보다 작고, x축에서 어느 정도 겹치는 경우
                    if (movingRect.yMax <= obstacle.yMin && movingRect.xMax > obstacle.xMin && movingRect.xMin < obstacle.xMax)
                    {
                        float distance = obstacle.yMin - movingRect.yMax; // y축 상의 거리 계산
                                                                          // 가장 가까운 충돌 거리를 업데이트
                        if (distance < closestDistance)
                        {
                            // 충돌 경계는 장애물의 하단(yMin)과, 장애물의 x축 중앙값으로 결정
                            collisionEdge = new Vector2(obstacle.center.x, obstacle.yMin);
                            // 이동 Rect의 x 중심이 장애물의 x 중심보다 왼쪽이면 true, 그렇지 않으면 false
                            isObstacleLeftOrBelow = movingRect.center.x <= obstacle.center.x;
                            closestDistance = distance;
                        }
                    }
                    break;

                    case EDirection4.Down:
                    // 아래쪽 이동: movingRect의 하단(yMin)이 장애물의 상단(yMax)보다 크고, x축에서 겹침이 있는 경우
                    if (movingRect.yMin >= obstacle.yMax && movingRect.xMax > obstacle.xMin && movingRect.xMin < obstacle.xMax)
                    {
                        float distance = movingRect.yMin - obstacle.yMax; // y축 상의 거리 계산
                        if (distance < closestDistance)
                        {
                            collisionEdge = new Vector2(obstacle.center.x, obstacle.yMax);
                            isObstacleLeftOrBelow = movingRect.center.x <= obstacle.center.x;
                            closestDistance = distance;
                        }
                    }
                    break;

                    case EDirection4.Left:
                    // 왼쪽 이동: movingRect의 왼쪽(xMin)이 장애물의 오른쪽(xMax)보다 크고, y축에서 겹침이 있는 경우
                    if (movingRect.xMin >= obstacle.xMax && movingRect.yMax > obstacle.yMin && movingRect.yMin < obstacle.yMax)
                    {
                        float distance = movingRect.xMin - obstacle.xMax; // x축 상의 거리 계산
                        if (distance < closestDistance)
                        {
                            // 충돌 경계 x좌표는 장애물의 오른쪽(xMax)이며, y좌표는 장애물의 y 범위 중 결정
                            collisionEdge = new Vector2(obstacle.xMax, (movingRect.center.y <= obstacle.center.y) ? obstacle.yMin : obstacle.yMax);
                            // Left 방향인 경우, 장애물은 항상 왼쪽에 있으므로 true로 설정
                            isObstacleLeftOrBelow = true;
                            closestDistance = distance;
                        }
                    }
                    break;

                    case EDirection4.Right:
                    // 오른쪽 이동: movingRect의 오른쪽(xMax)이 장애물의 왼쪽(xMin)보다 작고, y축에서 겹침이 있는 경우
                    if (movingRect.xMax <= obstacle.xMin && movingRect.yMax > obstacle.yMin && movingRect.yMin < obstacle.yMax)
                    {
                        float distance = obstacle.xMin - movingRect.xMax; // x축 상의 거리 계산
                        if (distance < closestDistance)
                        {
                            collisionEdge = new Vector2(obstacle.xMin, (movingRect.center.y <= obstacle.center.y) ? obstacle.yMin : obstacle.yMax);
                            // Right 방향인 경우, 장애물은 항상 오른쪽에 있으므로 false로 설정
                            isObstacleLeftOrBelow = false;
                            closestDistance = distance;
                        }
                    }
                    break;
                }
            }

            // 스테이지 경계(boundingRects)와의 충돌 처리: 계산된 충돌 경계가 스테이지 경계를 벗어나지 않도록 조정
            switch (direction)
            {
                case EDirection4.Up:
                if (boundingRects.yMax < collisionEdge.y)
                    collisionEdge = new Vector2(movingRect.center.x, boundingRects.yMax);
                break;
                case EDirection4.Down:
                if (boundingRects.yMin > collisionEdge.y)
                    collisionEdge = new Vector2(movingRect.center.x, boundingRects.yMin);
                break;
                case EDirection4.Left:
                if (boundingRects.xMin > collisionEdge.x)
                    collisionEdge = new Vector2(boundingRects.xMin, movingRect.center.y);
                break;
                case EDirection4.Right:
                if (boundingRects.xMax < collisionEdge.x)
                    collisionEdge = new Vector2(boundingRects.xMax, movingRect.center.y);
                break;
            }

            return collisionEdge;
        }



        /// <summary>
        /// 이동하는 Rect(movingRect)와 정적 Rect들 사이의 충돌 시점을 계산하여, <br/>
        /// 이동 시작 시점(tEntry)와 종료 시점(tExit)을 결정합니다. <br/>
        /// x축과 y축 각각에서의 충돌 진입 및 종료 시간을 계산한 후, <br/>
        /// 두 축의 최대 진입 시간과 최소 종료 시간을 사용하여 실제 충돌이 발생하는지를 판단합니다. <br/>
        /// 충돌이 발생하면 true를 반환하고, tEntry 및 tExit에 해당 값을 할당합니다.
        /// </summary>
        /// <param name="movingRect">이동하는 Rect</param>
        /// <param name="movementVector">이동 벡터 (x, y 속도 포함)</param>
        /// <param name="staticRect">충돌 대상인 정적 Rect</param>
        /// <param name="tEntry">계산된 충돌 진입 시간 (0~1 범위)</param>
        /// <param name="tExit">계산된 충돌 종료 시간 (0~1 범위)</param>
        /// <returns>
        /// 두 Rect 간에 충돌이 발생하면 true를 반환합니다. <br/>
        /// 이동 벡터에 따라 계산된 tEntry가 유효하지 않거나, tEntry > tExit인 경우 충돌이 없다고 판단합니다.
        /// </returns>
        private static bool ComputeCollisionTime(Rect movingRect, Vector2 movementVector, Rect staticRect, out float tEntry, out float tExit)
        {
            tEntry = 0f;
            tExit = 1f;
            //? 이동 벡터 분리 (vx, vy)
            float vx = movementVector.x;
            float vy = movementVector.y;

            float xEntry, yEntry;
            float xExit, yExit;

            //? x축에 대한 충돌 진입 및 종료 시간 계산
            if (vx > 0.0f)
            {
                xEntry = (staticRect.xMin - movingRect.xMax - Epsilon) / vx;
                xExit = (staticRect.xMax - movingRect.xMin + Epsilon) / vx;
            }
            else if (vx < 0.0f)
            {
                xEntry = (staticRect.xMax - movingRect.xMin + Epsilon) / vx;
                xExit = (staticRect.xMin - movingRect.xMax - Epsilon) / vx;
            }
            else //. vx == 0
            {
                //? x축 이동이 없으므로, 두 Rect의 x축 겹침 여부로 판단
                if (movingRect.xMax <= staticRect.xMin || movingRect.xMin >= staticRect.xMax)
                {
                    return false; //! x축에서 겹침이 없으므로 충돌이 발생하지 않습니다.
                }
                else
                {
                    xEntry = float.NegativeInfinity;
                    xExit = float.PositiveInfinity;
                }
            }

            //? y축에 대한 충돌 진입 및 종료 시간 계산
            if (vy > 0.0f)
            {
                yEntry = (staticRect.yMin - movingRect.yMax - Epsilon) / vy;
                yExit = (staticRect.yMax - movingRect.yMin + Epsilon) / vy;
            }
            else if (vy < 0.0f)
            {
                yEntry = (staticRect.yMax - movingRect.yMin + Epsilon) / vy;
                yExit = (staticRect.yMin - movingRect.yMax - Epsilon) / vy;
            }
            else //. vy == 0
            {
                if (movingRect.yMax <= staticRect.yMin || movingRect.yMin >= staticRect.yMax)
                {
                    return false; //! y축에서 겹침이 없으므로 충돌이 발생하지 않습니다.
                }
                else
                {
                    yEntry = float.NegativeInfinity;
                    yExit = float.PositiveInfinity;
                }
            }

            //? 두 축에서의 진입/종료 시간 결합
            tEntry = Mathf.Max(xEntry, yEntry);
            tExit = Mathf.Min(xExit, yExit);

            //? 충돌 조건 검증: tEntry가 tExit보다 크거나 유효 범위를 벗어나면 충돌 없음
            if (tEntry > tExit || tEntry > 1.0f || tEntry < 0.0f)
            {
                return false; //! 충돌이 발생하지 않습니다.
            }
            return true;
        }



        /// <summary>
        /// 이동하는 Rect(movingRect)와 스테이지 경계(boundingRects) 사이의 충돌로 인해 <br/>
        /// 이동 가능성이 제한되는 최대 이동 비율을 계산합니다. <br/>
        /// 이동 벡터의 각 축에 대해 경계와의 거리를 구하고, 해당 축의 이동 비율을 계산한 후, <br/>
        /// 두 축 중 작은 값을 최종 이동 비율로 반환합니다.
        /// </summary>
        /// <param name="movingRect">이동하는 Rect</param>
        /// <param name="movementVector">이동 벡터</param>
        /// <param name="boundingRects">스테이지 경계를 나타내는 Rect</param>
        /// <returns>
        /// 경계 충돌로 인해 제한되는 최대 이동 비율 (0과 1 사이의 값)
        /// </returns>
        private static float ComputeStageCollisionTime(Rect movingRect, Vector2 movementVector, Rect boundingRects)
        {
            float tMaxX = 1.0f;
            float tMaxY = 1.0f;
            float vx = movementVector.x;
            float vy = movementVector.y;

            //? x축 경계 검사: 오른쪽 혹은 왼쪽 경계를 넘어가지 않도록 이동 비율 계산
            if (vx > 0.0f)
            {
                float xMaxDistance = boundingRects.xMax - movingRect.xMax;
                tMaxX = xMaxDistance / vx;
            }
            else if (vx < 0.0f)
            {
                float xMinDistance = boundingRects.xMin - movingRect.xMin;
                tMaxX = xMinDistance / vx;
            }
            else
            {
                tMaxX = float.PositiveInfinity;
            }

            //? y축 경계 검사: 위쪽 혹은 아래쪽 경계를 넘어가지 않도록 이동 비율 계산
            if (vy > 0.0f)
            {
                float yMaxDistance = boundingRects.yMax - movingRect.yMax;
                tMaxY = yMaxDistance / vy;
            }
            else if (vy < 0.0f)
            {
                float yMinDistance = boundingRects.yMin - movingRect.yMin;
                tMaxY = yMinDistance / vy;
            }
            else
            {
                tMaxY = float.PositiveInfinity;
            }

            //? 두 축의 제한 중 작은 값이 최종 이동 가능 비율이며, 0과 1 사이로 클램핑
            float tMaxStage = Mathf.Min(tMaxX, tMaxY);
            tMaxStage = Mathf.Clamp(tMaxStage, 0.0f, 1.0f);
            return tMaxStage;
        }



        /// <summary>
        /// 두 Rect(rectA와 rectB)가 지정된 임계값(threshold) 이내로 근접해 있는지를 판단합니다. <br/>
        /// x축 및 y축에서 겹치지 않는 거리가 threshold 이하이면 두 Rect는 근접해 있다고 판단합니다.
        /// </summary>
        /// <param name="rectA">첫 번째 Rect</param>
        /// <param name="rectB">두 번째 Rect</param>
        /// <param name="threshold">근접 여부 판단에 사용되는 임계값 (기본값: Epsilon)</param>
        /// <returns>
        /// 두 Rect가 임계값 이하로 근접하면 true, 그렇지 않으면 false
        /// </returns>
        private static bool IsRectNear(Rect rectA, Rect rectB, float threshold = Epsilon)
        {
            //? x축과 y축에서 겹치지 않는 거리를 계산하여 비교
            float xDistance = Mathf.Max(0, Mathf.Max(rectA.xMin, rectB.xMin) - Mathf.Min(rectA.xMax, rectB.xMax));
            float yDistance = Mathf.Max(0, Mathf.Max(rectA.yMin, rectB.yMin) - Mathf.Min(rectA.yMax, rectB.yMax));
            return xDistance <= threshold && yDistance <= threshold;
        }



        /// <summary>
        /// 두 Rect(referenceRect와 adjacentRect)가 경계면에서 인접해 있을 경우, <br/>
        /// 겹치지 않는 부분의 길이를 계산하고, 인접한 축이 X축인지 Y축인지를 반환합니다. <br/>
        /// X축으로 인접하면 (아래쪽, 위쪽)의 비율을, Y축으로 인접하면 (왼쪽, 오른쪽)의 비율을 계산합니다.
        /// </summary>
        /// <param name="referenceRect">기준 Rect</param>
        /// <param name="adjacentRect">인접 여부를 확인할 Rect</param>
        /// <param name="nonOverlapLengthSide1">
        /// X축 인접: 기준 Rect의 아래쪽 비중, Y축 인접: 기준 Rect의 왼쪽 비중 (겹치지 않는 길이)
        /// </param>
        /// <param name="nonOverlapLengthSide2">
        /// X축 인접: 기준 Rect의 위쪽 비중, Y축 인접: 기준 Rect의 오른쪽 비중 (겹치지 않는 길이)
        /// </param>
        /// <param name="isAdjacentOnX">두 Rect가 X축으로 인접하면 true, 그렇지 않으면 Y축 인접</param>
        /// <param name="epsilon">부동소수점 오차 보정 값 (기본값: Epsilon)</param>
        /// <returns>
        /// 두 Rect가 인접하면 true, 그렇지 않으면 false
        /// </returns>
        public static bool GetNonOverlappingLengths(
            Rect referenceRect,
            Rect adjacentRect,
            out float nonOverlapLengthSide1,
            out float nonOverlapLengthSide2,
            out bool isAdjacentOnX,
            float epsilon = Epsilon)
        {
            nonOverlapLengthSide1 = 0f;
            nonOverlapLengthSide2 = 0f;
            isAdjacentOnX = false;

            //? 두 Rect가 경계에서 인접한지와 인접 축을 확인
            if (!AreRectsAdjacent(referenceRect, adjacentRect, out isAdjacentOnX, epsilon))
            {
                return false; //! 인접하지 않으면 계산 불가
            }

            if (isAdjacentOnX)
            {
                //? X축 인접: y축 겹침 영역을 기준으로 아래쪽 및 위쪽의 비율 계산
                float overlapMinY = Mathf.Max(referenceRect.yMin, adjacentRect.yMin);
                float overlapMaxY = Mathf.Min(referenceRect.yMax, adjacentRect.yMax);
                float nonOverlapBottom = overlapMinY - referenceRect.yMin;
                float nonOverlapTop = referenceRect.yMax - overlapMaxY;
                nonOverlapLengthSide1 = Mathf.Max(0f, nonOverlapBottom); //. 아래쪽
                nonOverlapLengthSide2 = Mathf.Max(0f, nonOverlapTop);    //. 위쪽
            }
            else
            {
                //? Y축 인접: x축 겹침 영역을 기준으로 왼쪽 및 오른쪽의 비율 계산
                float overlapMinX = Mathf.Max(referenceRect.xMin, adjacentRect.xMin);
                float overlapMaxX = Mathf.Min(referenceRect.xMax, adjacentRect.xMax);
                float nonOverlapLeft = overlapMinX - referenceRect.xMin;
                float nonOverlapRight = referenceRect.xMax - overlapMaxX;
                nonOverlapLengthSide1 = Mathf.Max(0f, nonOverlapLeft);  //. 왼쪽
                nonOverlapLengthSide2 = Mathf.Max(0f, nonOverlapRight); //. 오른쪽
            }

            return true; //! 인접해 있으므로 true를 반환합니다.
        }



        /// <summary>
        /// 두 Rect(referenceRect와 targetRect)가 인접하거나 떨어져 있는 경우, <br/>
        /// 사이에 다른 Rect가 방해하지 않는지 확인하고, 겹치지 않는 부분의 길이 및 두 Rect 사이의 분리 거리를 계산하여 반환합니다. <br/>
        /// 이 메서드는 두 Rect 사이의 '명확한 경로(clear path)'가 존재하는지를 판단하는 데 사용됩니다.
        /// </summary>
        /// <param name="referenceRect">기준 Rect</param>
        /// <param name="targetRect">대상 Rect</param>
        /// <param name="allRects">전체 Rect 컬렉션 (경로 장애물 검사에 사용)</param>
        /// <param name="nonOverlapLengthSide1">
        /// 인접 축 기준 첫 번째 방향(왼쪽 또는 아래쪽)의 겹치지 않는 길이
        /// </param>
        /// <param name="nonOverlapLengthSide2">
        /// 인접 축 기준 두 번째 방향(오른쪽 또는 위쪽)의 겹치지 않는 길이
        /// </param>
        /// <param name="isAdjacentOnX">두 Rect가 X축으로 인접하면 true, 그렇지 않으면 Y축 인접</param>
        /// <param name="separationDistance">두 Rect 사이의 실제 분리 거리</param>
        /// <param name="epsilon">부동소수점 오차 보정 값 (기본값: Epsilon)</param>
        /// <returns>
        /// 두 Rect 사이에 장애물이 없고 조건을 만족하면 true, 그렇지 않으면 false
        /// </returns>
        public static bool GetNonOverlappingLengthsWithClearPath(
       Rect referenceRect,
       Rect targetRect,
       IEnumerable<Rect> allRects,
       out float nonOverlapLengthSide1,
       out float nonOverlapLengthSide2,
       out bool isAdjacentOnX,
       out float separationDistance,
       float epsilon = Epsilon)
        {
            nonOverlapLengthSide1 = 0f;
            nonOverlapLengthSide2 = 0f;
            isAdjacentOnX = false;
            separationDistance = 0f;

            //? 두 Rect가 인접한지 여부와 인접 축 결정
            bool isAdjacent = AreRectsAdjacent(referenceRect, targetRect, out isAdjacentOnX, epsilon);

            //? 인접하지 않더라도, 두 Rect 사이에 다른 Rect가 방해하지 않는지 확인
            if (!HasClearPath(referenceRect, targetRect, allRects, isAdjacentOnX, epsilon))
            {
                return false; //! 사이에 다른 Rect가 있으므로 false를 반환합니다.
            }

            if (isAdjacentOnX)
            {
                //? X축 인접 또는 분리된 경우: y축 겹침 영역을 기준으로 아래쪽, 위쪽 길이 및 x축 분리 거리 계산
                float overlapMinY = Mathf.Max(referenceRect.yMin, targetRect.yMin);
                float overlapMaxY = Mathf.Min(referenceRect.yMax, targetRect.yMax);
                float nonOverlapBottom = overlapMinY - referenceRect.yMin;
                float nonOverlapTop = referenceRect.yMax - overlapMaxY;
                nonOverlapLengthSide1 = Mathf.Max(0f, nonOverlapBottom);
                nonOverlapLengthSide2 = Mathf.Max(0f, nonOverlapTop);

                //? x축 분리 거리 계산
                if (referenceRect.xMax <= targetRect.xMin)
                {
                    separationDistance = targetRect.xMin - referenceRect.xMax;
                }
                else if (targetRect.xMax <= referenceRect.xMin)
                {
                    separationDistance = referenceRect.xMin - targetRect.xMax;
                }
                else
                {
                    separationDistance = 0f; //. 겹쳐있거나 인접해있음
                }
            }
            else
            {
                //? Y축 인접 또는 분리된 경우: x축 겹침 영역을 기준으로 왼쪽, 오른쪽 길이 및 y축 분리 거리 계산
                float overlapMinX = Mathf.Max(referenceRect.xMin, targetRect.xMin);
                float overlapMaxX = Mathf.Min(referenceRect.xMax, targetRect.xMax);
                float nonOverlapLeft = overlapMinX - referenceRect.xMin;
                float nonOverlapRight = referenceRect.xMax - overlapMaxX;
                nonOverlapLengthSide1 = Mathf.Max(0f, nonOverlapLeft);
                nonOverlapLengthSide2 = Mathf.Max(0f, nonOverlapRight);

                //? y축 분리 거리 계산
                if (referenceRect.yMax <= targetRect.yMin)
                {
                    separationDistance = targetRect.yMin - referenceRect.yMax;
                }
                else if (targetRect.yMax <= referenceRect.yMin)
                {
                    separationDistance = referenceRect.yMin - targetRect.yMax;
                }
                else
                {
                    separationDistance = 0f; //. 겹쳐있거나 인접해있음
                }
            }
            return true;
        }



        /// <summary>
        /// 두 Rect(rectA와 rectB) 사이에 장애물이 없는 명확한 경로(clear path)가 존재하는지 확인합니다. <br/>
        /// 두 Rect 사이의 영역(checkingArea)을 계산한 후, 해당 영역에 다른 Rect가 겹치는지 검사합니다. <br/>
        /// 경로 상에 장애물이 없으면 true를 반환합니다.
        /// </summary>
        /// <param name="rectA">첫 번째 Rect</param>
        /// <param name="rectB">두 번째 Rect</param>
        /// <param name="allRects">검사할 전체 Rect들의 컬렉션</param>
        /// <param name="isCheckingAlongX">
        /// true이면 X축 방향으로 경로를 검사, false이면 Y축 방향으로 검사합니다.
        /// </param>
        /// <param name="epsilon">부동소수점 오차 보정 값 (기본값: Epsilon)</param>
        /// <returns>두 Rect 사이에 장애물이 없으면 true, 있으면 false</returns>
        private static bool HasClearPath(Rect rectA, Rect rectB, IEnumerable<Rect> allRects, bool isCheckingAlongX, float epsilon)
        {
            Rect checkingArea;
            //? 두 Rect의 최소 및 최대 좌표 계산
            float minX = Mathf.Min(rectA.xMin, rectB.xMin);
            float maxX = Mathf.Max(rectA.xMax, rectB.xMax);
            float minY = Mathf.Min(rectA.yMin, rectB.yMin);
            float maxY = Mathf.Max(rectA.yMax, rectB.yMax);

            if (isCheckingAlongX)
            {
                //? X축 방향 검사: 두 Rect 사이의 x좌표 영역과 전체 y축 영역을 사용
                float startX = rectA.xMax;
                float endX = rectB.xMin;
                if (startX > endX)
                {
                    startX = rectB.xMax;
                    endX = rectA.xMin;
                }
                checkingArea = new Rect(startX, minY, endX - startX, maxY - minY);
            }
            else
            {
                //? Y축 방향 검사: 두 Rect 사이의 y좌표 영역과 전체 x축 영역을 사용
                float startY = rectA.yMax;
                float endY = rectB.yMin;
                if (startY > endY)
                {
                    startY = rectB.yMax;
                    endY = rectA.yMin;
                }
                checkingArea = new Rect(minX, startY, maxX - minX, endY - startY);
            }

            //? 음수 영역이 발생하면 보정하여 양수로 변환
            if (checkingArea.width < 0)
            {
                checkingArea.x += checkingArea.width;
                checkingArea.width = -checkingArea.width;
            }
            if (checkingArea.height < 0)
            {
                checkingArea.y += checkingArea.height;
                checkingArea.height = -checkingArea.height;
            }

            //? 영역의 크기가 너무 작으면 장애물이 없다고 판단
            if (checkingArea.width <= epsilon || checkingArea.height <= epsilon)
            {
                return true;
            }

            //? 계산된 영역에 다른 Rect가 겹치는지 검사
            foreach (var rect in allRects)
            {
                if (rect.Equals(rectA) || rect.Equals(rectB))
                    continue;
                if (rect.Overlaps(checkingArea))
                {
                    return false;
                }
            }
            return true;
        }



        /// <summary>
        /// 두 Rect(rectA와 rectB)가 경계면에서 정확히 인접해 있는지 판단하고, <br/>
        /// 인접한 축이 X축인지(Y축이면 false) Y축인지(true이면 false)를 반환합니다. <br/>
        /// 경계 인접 여부는 두 Rect의 경계 차이가 Epsilon 이하이며, 다른 축에서 일정 부분 겹침이 있는지를 기준으로 합니다.
        /// </summary>
        /// <param name="rectA">첫 번째 Rect</param>
        /// <param name="rectB">두 번째 Rect</param>
        /// <param name="isAdjacentOnX">
        /// Rect들이 X축으로 인접하면 true, 그렇지 않으면 Y축 인접(즉, false)
        /// </param>
        /// <param name="epsilon">부동소수점 오차 보정 값 (기본값: Epsilon)</param>
        /// <returns>두 Rect가 인접하면 true, 그렇지 않으면 false</returns>
        public static bool AreRectsAdjacent(Rect rectA, Rect rectB, out bool isAdjacentOnX, float epsilon = Epsilon)
        {
            isAdjacentOnX = false;

            //? X축 인접 검사: 두 Rect의 x경계가 맞닿고(y축 일부 겹침) 있는지 확인
            bool isAdjacentX =
                (Mathf.Abs(rectA.xMax - rectB.xMin) <= epsilon || Mathf.Abs(rectA.xMin - rectB.xMax) <= epsilon) &&
                rectA.yMin < rectB.yMax - epsilon && rectA.yMax > rectB.yMin + epsilon;

            //? Y축 인접 검사: 두 Rect의 y경계가 맞닿고(x축 일부 겹침) 있는지 확인
            bool isAdjacentY =
                (Mathf.Abs(rectA.yMax - rectB.yMin) <= epsilon || Mathf.Abs(rectA.yMin - rectB.yMax) <= epsilon) &&
                rectA.xMin < rectB.xMax - epsilon && rectA.xMax > rectB.xMin + epsilon;

            if (isAdjacentX)
            {
                isAdjacentOnX = true;
                return true;
            }
            else if (isAdjacentY)
            {
                isAdjacentOnX = false;
                return true;
            }
            else
            {
                return false;
            }
        }



        /// <summary>
        /// 두 Rect가 경계면에서 인접해 있는지 단순히 판단합니다. <br/>
        /// 내부적으로 AreRectsAdjacent(Rect, Rect, out bool, float)를 호출하여 결과를 반환합니다.
        /// </summary>
        /// <param name="rectA">첫 번째 Rect</param>
        /// <param name="rectB">두 번째 Rect</param>
        /// <param name="epsilon">부동소수점 오차 보정 값 (기본값: Epsilon)</param>
        /// <returns>두 Rect가 인접하면 true, 그렇지 않으면 false</returns>
        public static bool AreRectsAdjacent(Rect rectA, Rect rectB, float epsilon = Epsilon)
        {
            bool isAdjacentOnX;
            return AreRectsAdjacent(rectA, rectB, out isAdjacentOnX, epsilon);
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================



    /// <summary>
    /// 2D 영역(Rect)과 해당 영역의 오프셋(Vector2)을 함께 저장하는 구조체입니다.
    /// </summary>
    [Serializable]
    public struct RectWithOffset
    {
        /// <summary>
        /// 지정된 Rect와 Offset을 사용하여 RectWithOffset 구조체를 초기화합니다.
        /// </summary>
        /// <param name="rect">저장할 2D 영역(Rect)입니다.</param>
        /// <param name="offset">Rect에 적용할 오프셋(Vector2)입니다.</param>
        public RectWithOffset(Rect rect, Vector2 offset)
        {
            Rect = rect;
            Offset = offset;
        }



        /// <summary>
        /// 저장된 2D 영역(Rect)입니다.
        /// </summary>
        public Rect Rect;



        /// <summary>
        /// Rect에 적용되는 2D 오프셋(Vector2)입니다.
        /// </summary>
        public Vector2 Offset;



        /// <summary>
        /// Rect의 중심 좌표에 Offset을 더한 결과를 반환합니다.
        /// </summary>
        /// <returns>
        /// Offset이 적용된 Rect의 중심 좌표(Vector2)입니다.
        /// </returns>
        public Vector2 GetCenterWithOffset()
        {
            return Rect.center + Offset;
        }



        public void ApplyNewRectCenterWithOffset(Vector2 rectCenter)
        {
            Rect.center = rectCenter - Offset;
        }

        public override string ToString()
        {
            //. Rect(xMin, yMin, width, height) + Offset(x, y) 형식
            return $"Rect(x:{Rect.xMin}, y:{Rect.yMin}, w:{Rect.width}, h:{Rect.height})  Offset(x:{Offset.x}, y:{Offset.y})";
        }
    }



    [Serializable]
    public struct CustomRect2DRelative
    {
        ///======================================================================================================================================================



        public CustomRect2DRelative(Vector2 offset, Vector2 size)
        {
            Offset = offset;
            Size = size;
        }



        public Vector2 Offset;

        public Vector2 Size;



        ///======================================================================================================================================================



        public float xMin
        {
            get => Offset.x - Size.x * 0.5f;
            set
            {
                float right = xMax;
                float newW = Mathf.Max(0f, right - value);
                Size = new Vector2(newW, Size.y);                        // Size.x 수정
                Offset = new Vector2((right + value) * 0.5f, Offset.y);    // 중심 재계산
            }
        }

        public float xMax
        {
            get => Offset.x + Size.x * 0.5f;
            set
            {
                float left = xMin;
                float newW = Mathf.Max(0f, value - left);
                Size = new Vector2(newW, Size.y);
                Offset = new Vector2((left + value) * 0.5f, Offset.y);
            }
        }

        public float yMin
        {
            get => Offset.y - Size.y * 0.5f;
            set
            {
                float top = yMax;
                float newH = Mathf.Max(0f, top - value);
                Size = new Vector2(Size.x, newH);
                Offset = new Vector2(Offset.x, (top + value) * 0.5f);
            }
        }

        public float yMax
        {
            get => Offset.y + Size.y * 0.5f;
            set
            {
                float bottom = yMin;
                float newH = Mathf.Max(0f, value - bottom);
                Size = new Vector2(Size.x, newH);
                Offset = new Vector2(Offset.x, (bottom + value) * 0.5f);
            }
        }



        ///======================================================================================================================================================



        public override readonly string ToString()
        {
            return $"Offset(x:{Offset.x}, y:{Offset.y}) Size(x:{Size.x}, y:{Size.y})";
        }



        ///======================================================================================================================================================

    }



    [Serializable]
    public struct CustomRect2DCentered
    {

        ///======================================================================================================================================================



        public CustomRect2DCentered(Vector2 center, Vector2 offset, Vector2 size)
        {
            Center = center;
            CustomRect2DRelative = new CustomRect2DRelative(offset, size);
        }

        public CustomRect2DCentered(Vector2 center, CustomRect2DRelative customRect2DRelative)
        {
            Center = center;
            CustomRect2DRelative = customRect2DRelative;
        }



        ///======================================================================================================================================================



        public Vector2 Center;
        public CustomRect2DRelative CustomRect2DRelative;



        ///======================================================================================================================================================



        public Vector2 Offset
        {
            get => CustomRect2DRelative.Offset;
            set => CustomRect2DRelative.Offset = value;
        }



        public Vector2 Size
        {
            get => CustomRect2DRelative.Size;
            set => CustomRect2DRelative.Size = value;
        }



        public Vector2 CenterWithOffset
        {
            get => Center + Offset;
            private set => Offset = value - Center;
        }



        ///======================================================================================================================================================



        //? 세계 좌표 네 경계



        public float xMin
        {
            get => CenterWithOffset.x - Size.x * 0.5f;
            set
            {
                float right = xMax;
                float newW = Mathf.Max(0f, right - value);
                Size = new Vector2(newW, Size.y);
                CenterWithOffset = new Vector2((right + value) * 0.5f, CenterWithOffset.y);
            }
        }

        public float xMax
        {
            get => CenterWithOffset.x + Size.x * 0.5f;
            set
            {
                float left = xMin;
                float newW = Mathf.Max(0f, value - left);
                Size = new Vector2(newW, Size.y);
                CenterWithOffset = new Vector2((left + value) * 0.5f, CenterWithOffset.y);
            }
        }

        public float yMin
        {
            get => CenterWithOffset.y - Size.y * 0.5f;
            set
            {
                float top = yMax;
                float newH = Mathf.Max(0f, top - value);
                Size = new Vector2(Size.x, newH);
                CenterWithOffset = new Vector2(CenterWithOffset.x, (top + value) * 0.5f);
            }
        }

        public float yMax
        {
            get => CenterWithOffset.y + Size.y * 0.5f;
            set
            {
                float bottom = yMin;
                float newH = Mathf.Max(0f, value - bottom);
                Size = new Vector2(Size.x, newH);
                CenterWithOffset = new Vector2(CenterWithOffset.x, (bottom + value) * 0.5f);
            }
        }



        ///======================================================================================================================================================



        public override readonly string ToString()
        {
            return $"Center(x:{Center.x}, y:{Center.y}) + {CustomRect2DRelative}";
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================
}