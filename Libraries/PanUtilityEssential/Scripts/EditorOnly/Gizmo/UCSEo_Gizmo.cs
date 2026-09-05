#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Pan.Util;
using Pan.Util.Editors;
using System.Runtime.CompilerServices;



//? 기즈모 관련이 저장되어있는 정도의 코드 (EditorOnly)



namespace Pan.Util.Editors
{
    ///======================================================================================================================================================



    public static class SU_Gizmo
    {
        ///======================================================================================================================================================



        //? 원형 그리기



        /// <summary>
        /// 내부가 채워진(실제 디스크 형태의) 원 기즈모를 그립니다. <br/>
        /// 지정된 중심 좌표, 반지름, 스위즐 방식, 회전, 세그먼트 수를 기반으로, 원의 각 점을 계산한 후, <br/>
        /// 삼각형들(AA Convex Polygon)을 이용해 원을 채워서 그립니다.
        /// </summary>
        /// <param name="center">원 기즈모의 중심 좌표</param>
        /// <param name="radius">원의 반지름</param>
        /// <param name="swizzle">좌표 스위즐 방식 (예: GridLayout.CellSwizzle.XYZ 등)</param>
        /// <param name="rotation">
        /// 원에 적용할 회전 값. 기본값이 제공되면 (default) Quaternion.Euler(90, 0, 0)로 설정되어, <br/>
        /// 원이 수평으로 보이도록 합니다.
        /// </param>
        /// <param name="segments">원을 구성할 세그먼트 수 (세그먼트 수가 많을수록 원이 부드럽게 보임)</param>
        public static void DrawSolidCircle(Vector3 center, float radius, GridLayout.CellSwizzle swizzle = GridLayout.CellSwizzle.XYZ, Quaternion rotation = default, int segments = 36)
        {
            // 기본 회전값 처리: rotation이 default이면 90도 회전하여 수평 원으로 만듦.
            if (rotation == default)
            {
                rotation = Quaternion.Euler(90, 0, 0);
            }

            // 원을 구성할 점들을 계산 (각도에 따른 위치 계산 후 스위즐 적용)
            Vector3[] vertices = new Vector3[segments];
            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * 2 * Mathf.PI;
                // 로컬 공간에서 원의 한 점 계산
                Vector3 point = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                // 스위즐 변환을 적용하여 올바른 좌표 축으로 변경
                vertices[i] = point.SwizzlesVector(swizzle);
            }

            // 계산된 모든 점에 대해 회전 적용
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = rotation * vertices[i];
            }

            // 현재 Handles 색상 저장 (복원하기 위함)
            Color originalColor = Handles.color;
            // Gizmos의 색상으로 Handles 색상을 설정 (일관된 색상 사용)
            Handles.color = Gizmos.color;

            // 중심과 인접한 두 점을 이용해 삼각형을 그려 디스크를 채움
            for (int i = 0; i < segments; i++)
            {
                Vector3 startPos = center + vertices[i];
                Vector3 endPos = center + vertices[(i + 1) % segments];
                Handles.DrawAAConvexPolygon(center, startPos, endPos);
            }

            // 원래의 Handles 색상으로 복원
            Handles.color = originalColor;
        }



        /// <summary>
        /// 원 기즈모(와이어 형태)를 그립니다. <br/>
        /// 지정된 중심 좌표, 반지름, 스위즐 방식, 회전 및 세그먼트 수를 기반으로, <br/>
        /// 원의 둘레를 따라 선을 그려 원 외곽선을 생성합니다.
        /// </summary>
        /// <param name="center">원 기즈모의 중심 좌표</param>
        /// <param name="radius">원의 반지름</param>
        /// <param name="swizzle">좌표 스위즐 방식 (예: GridLayout.CellSwizzle.XYZ 등)</param>
        /// <param name="rotation">
        /// 원에 적용할 회전 값. 기본값이면 Quaternion.Euler(-90, 0, 0)이 적용되어, <br/>
        /// 2D 평면에서 올바른 방향으로 표시됩니다.
        /// </param>
        /// <param name="segments">원을 구성할 선분 수 (값이 클수록 원이 둥글게 보임)</param>
        public static void DrawCircle(Vector3 center, float radius, GridLayout.CellSwizzle swizzle = GridLayout.CellSwizzle.XYZ, Quaternion rotation = default, int segments = 36)
        {
            // 기본 회전값 처리: rotation이 default이면 -90도 회전 적용 (2D 모드에 적합)
            if (rotation == default)
            {
                rotation = Quaternion.Euler(-90, 0, 0);
            }

            // 각 선분의 시작점과 끝점을 계산하여 원의 외곽선을 그림
            for (int i = 0; i < segments; i++)
            {
                float startAngle = (i / (float)segments) * 2 * Mathf.PI;
                float endAngle = ((i + 1) / (float)segments) * 2 * Mathf.PI;

                // 로컬 좌표계에서 점 계산 후 스위즐 변환
                Vector3 startPos = (new Vector3(Mathf.Cos(startAngle), Mathf.Sin(startAngle)) * radius).SwizzlesVector(swizzle);
                Vector3 endPos = (new Vector3(Mathf.Cos(endAngle), Mathf.Sin(endAngle)) * radius).SwizzlesVector(swizzle);

                // 회전 적용
                startPos = rotation * startPos;
                endPos = rotation * endPos;

                // 계산된 점들을 중심에 더해 월드 좌표로 변환하고 선을 그림
                Gizmos.DrawLine(center + startPos, center + endPos);
            }
        }



        /// <summary>
        /// 2D 환경에서 원 기즈모(와이어 형태)를 그립니다. <br/>
        /// 내부적으로 DrawGizmoCircle 메서드를 호출하며, 기본 스위즐(GridLayout.CellSwizzle.XYZ) 및 회전(default)을 사용합니다.
        /// </summary>
        /// <param name="center">원 기즈모의 중심 좌표</param>
        /// <param name="radius">원의 반지름</param>
        /// <param name="segments">원을 구성할 선분 수</param>
        public static void DrawCircle2D(Vector3 center, float radius, int segments = 36)
        {
            DrawCircle(center, radius, GridLayout.CellSwizzle.XYZ, default, segments);
        }



        /// <summary>
        /// 원 내부에 X 모양을 그립니다. <br/>
        /// isStickOut 매개변수에 따라, 선들이 원의 경계를 벗어나는 방식과 내부에서만 그리는 방식이 결정됩니다. <br/>
        /// 원 자체는 그리지 않습니다.
        /// </summary>
        /// <param name="center">원 내부 X 모양의 중심 좌표</param>
        /// <param name="radius">대상 원의 반지름</param>
        /// <param name="swizzle">좌표 스위즐 방식 (예: GridLayout.CellSwizzle.XYZ 등)</param>
        /// <param name="isStickOut">
        /// false이면, 중심에서부터 반지름 길이만큼 X 모양을 그립니다. <br/>
        /// true이면, 원의 경계까지 선이 확장됩니다.
        /// </param>
        /// <param name="rotation">X 모양에 적용할 회전 값 (기본값: Quaternion.identity)</param>
        public static void DrawCircleX(Vector3 center, float radius, GridLayout.CellSwizzle swizzle = GridLayout.CellSwizzle.XYZ, bool isStickOut = false, Quaternion rotation = default)
        {
            if (rotation == default)
            {
                rotation = Quaternion.identity;
            }

            if (!isStickOut)
            {
                // 내부에서 X 모양을 그릴 때: (1,1,0)과 (-1,1,0) 벡터를 이용하여 대각선 방향 계산
                Vector3 diagDir1 = rotation * (new Vector3(1, 1, 0).SwizzlesVector(swizzle).normalized * radius);
                Vector3 diagDir2 = rotation * (new Vector3(-1, 1, 0).SwizzlesVector(swizzle).normalized * radius);

                Gizmos.DrawLine(center - diagDir1, center + diagDir1);
                Gizmos.DrawLine(center - diagDir2, center + diagDir2);
            }
            else
            {
                // 원의 경계까지 선을 확장하는 경우: 코너 좌표 계산 후 회전 적용
                Vector3 diag1Start = new Vector3(-radius, -radius, 0).SwizzlesVector(swizzle);
                Vector3 diag1End = new Vector3(radius, radius, 0).SwizzlesVector(swizzle);
                Vector3 diag2Start = new Vector3(-radius, radius, 0).SwizzlesVector(swizzle);
                Vector3 diag2End = new Vector3(radius, -radius, 0).SwizzlesVector(swizzle);

                diag1Start = rotation * diag1Start;
                diag1End = rotation * diag1End;
                diag2Start = rotation * diag2Start;
                diag2End = rotation * diag2End;

                Gizmos.DrawLine(center + diag1Start, center + diag1End);
                Gizmos.DrawLine(center + diag2Start, center + diag2End);
            }
        }



        /// <summary>
        /// 2D 환경에서 원 내부에 X 모양을 그립니다. <br/>
        /// 내부적으로 DrawGizmoXCircle 메서드를 호출하며, 기본 스위즐(GridLayout.CellSwizzle.XYZ) 및 회전(default)을 사용합니다.
        /// </summary>
        /// <param name="center">원 내부 X 모양의 중심 좌표</param>
        /// <param name="radius">대상 원의 반지름</param>
        /// <param name="isStickOut">
        /// X 모양 선들이 원의 경계를 벗어날지 여부를 결정합니다.
        /// </param>
        public static void DrawCircleX2D(Vector3 center, float radius, bool isStickOut = false)
        {
            DrawCircleX(center, radius, GridLayout.CellSwizzle.XYZ, isStickOut, default);
        }



        /// <summary>
        /// 원 내부에 + 모양을 그립니다. <br/>
        /// isStickOut 매개변수에 따라, 선들이 원의 경계를 벗어나도록 할지 결정됩니다. <br/>
        /// 원 자체는 그리지 않으며, + 모양의 선만 그립니다.
        /// </summary>
        /// <param name="center">원 내부 + 모양의 중심 좌표</param>
        /// <param name="radius">대상 원의 반지름</param>
        /// <param name="swizzle">좌표 스위즐 방식 (예: GridLayout.CellSwizzle.XYZ 등)</param>
        /// <param name="isStickOut">
        /// false이면, 중심에서부터 반지름 길이만큼 선을 그립니다. <br/>
        /// true이면, 선이 원의 경계를 벗어나도록 확장됩니다.
        /// </param>
        /// <param name="rotation">+ 모양에 적용할 회전 값 (기본값: Quaternion.identity)</param>
        public static void DrawCirclePlus(Vector3 center, float radius, GridLayout.CellSwizzle swizzle = GridLayout.CellSwizzle.XYZ, bool isStickOut = false, Quaternion rotation = default)
        {
            if (rotation == default)
            {
                rotation = Quaternion.identity;
            }

            float adjustedRadius = isStickOut ? radius * Mathf.Sqrt(2) : radius;

            if (!isStickOut)
            {
                // 내부 + 모양: 수직, 수평 방향 계산 후 회전 적용
                Vector3 vertical = rotation * (new Vector3(0, 1, 0).SwizzlesVector(swizzle).normalized * radius);
                Vector3 horizontal = rotation * (new Vector3(1, 0, 0).SwizzlesVector(swizzle).normalized * radius);

                Gizmos.DrawLine(center - vertical, center + vertical);
                Gizmos.DrawLine(center - horizontal, center + horizontal);
            }
            else
            {
                // 선이 원의 경계를 벗어나도록: 확장된 길이(adjustedRadius)로 시작/끝 점 계산 후 회전 적용
                Vector3 verticalStart = new Vector3(0, -adjustedRadius, 0).SwizzlesVector(swizzle);
                Vector3 verticalEnd = new Vector3(0, adjustedRadius, 0).SwizzlesVector(swizzle);
                Vector3 horizontalStart = new Vector3(-adjustedRadius, 0, 0).SwizzlesVector(swizzle);
                Vector3 horizontalEnd = new Vector3(adjustedRadius, 0, 0).SwizzlesVector(swizzle);

                verticalStart = rotation * verticalStart;
                verticalEnd = rotation * verticalEnd;
                horizontalStart = rotation * horizontalStart;
                horizontalEnd = rotation * horizontalEnd;

                Gizmos.DrawLine(center + verticalStart, center + verticalEnd);
                Gizmos.DrawLine(center + horizontalStart, center + horizontalEnd);
            }
        }



        /// <summary>
        /// 2D 환경에서 원 내부에 + 모양을 그립니다. <br/>
        /// 내부적으로 DrawGizmoPlusCircle 메서드를 호출하며, 기본 스위즐(GridLayout.CellSwizzle.XYZ) 및 회전(default)을 사용합니다.
        /// </summary>
        /// <param name="center">원 내부 + 모양의 중심 좌표</param>
        /// <param name="radius">대상 원의 반지름</param>
        /// <param name="isStickOut">선이 원의 경계를 벗어날지 여부</param>
        public static void DrawCirclePlus2D(Vector3 center, float radius, bool isStickOut = false)
        {
            DrawCirclePlus(center, radius, GridLayout.CellSwizzle.XYZ, isStickOut, default);
        }



        ///======================================================================================================================================================



        //? 정삼각형 그리기



        static readonly Vector3[] s_Tri = new Vector3[3]; //. 재사용 버퍼 (params 방지용) — 참조만 고정, 내부 값은 매 호출 갱신



        /// <summary>
        /// 정삼각형 기즈모(와이어 형태)를 그립니다. <br/>
        /// 중심, 반지름, 방향을 기반으로 꼭짓점을 계산하여 외곽선을 생성합니다.
        /// </summary>
        /// <param name="center">정삼각형 기즈모의 중심 좌표</param>
        /// <param name="radius">외접원 반지름</param>
        /// <param name="direction">정삼각형이 향할 방향 (EDirection4)</param>
        /// <param name="swizzle">좌표 스위즐 방식</param>
        /// <param name="rotation">
        /// 기본값이면 Quaternion.Euler(-90, 0, 0)이 적용되어 2D 평면에서 올바른 방향으로 표시됩니다.
        /// </param>
        public static void DrawTriangle(Vector3 center, float radius, EDirection4 direction,
                                        GridLayout.CellSwizzle swizzle = GridLayout.CellSwizzle.XYZ,
                                        Quaternion rotation = default)
        {
            if (rotation == default) { rotation = Quaternion.Euler(-90, 0, 0); }

            GetTriPoints(center, radius, GetDirOffsetDeg(direction), swizzle, rotation, out var v0, out var v1, out var v2);

            Gizmos.DrawLine(v0, v1);
            Gizmos.DrawLine(v1, v2);
            Gizmos.DrawLine(v2, v0);
        }



        /// <summary>
        /// 내부가 채워진 정삼각형 기즈모를 그립니다. <br/>
        /// 중심, 반지름, 방향을 기반으로 꼭짓점을 계산하여 면을 채웁니다.
        /// </summary>
        /// <param name="center">정삼각형 기즈모의 중심 좌표</param>
        /// <param name="radius">외접원 반지름</param>
        /// <param name="direction">정삼각형이 향할 방향 (EDirection4)</param>
        /// <param name="swizzle">좌표 스위즐 방식</param>
        /// <param name="rotation">
        /// 기본값이면 Quaternion.Euler(90, 0, 0)이 적용되어 수평으로 보이도록 합니다.
        /// </param>
        public static void DrawSolidTriangle(Vector3 center, float radius, EDirection4 direction,
                                             GridLayout.CellSwizzle swizzle = GridLayout.CellSwizzle.XYZ,
                                             Quaternion rotation = default)
        {
            if (rotation == default) { rotation = Quaternion.Euler(90, 0, 0); }

            GetTriPoints(center, radius, GetDirOffsetDeg(direction), swizzle, rotation, out s_Tri[0], out s_Tri[1], out s_Tri[2]);

            //. Handles 색/변환 스코프는 구조체라 GC 없음
            using (new Handles.DrawingScope(Gizmos.color))
            {
                Handles.DrawAAConvexPolygon(s_Tri);   //? 기존 params 호출에 새 배열을 만들지 않도록, 재사용 배열 전달
            }
        }



        /// <summary>
        /// 정삼각형 기즈모(와이어 형태)를 그립니다. <br/>
        /// 중심, 반지름, 8방향을 기반으로 꼭짓점을 계산하여 외곽선을 생성합니다.
        /// </summary>
        /// <param name="center">정삼각형 기즈모의 중심 좌표</param>
        /// <param name="radius">외접원 반지름</param>
        /// <param name="direction">정삼각형이 향할 방향 (EDirection8)</param>
        /// <param name="swizzle">좌표 스위즐 방식</param>
        /// <param name="rotation">
        /// 기본값이면 Quaternion.Euler(-90, 0, 0)이 적용되어 2D 평면에서 올바른 방향으로 표시됩니다.
        /// </param>
        public static void DrawTriangle(Vector3 center, float radius, EDirection8 direction,
                                        GridLayout.CellSwizzle swizzle = GridLayout.CellSwizzle.XYZ,
                                        Quaternion rotation = default)
        {
            if (rotation == default) { rotation = Quaternion.Euler(-90, 0, 0); }

            GetTriPoints(center, radius, GetDirOffsetDeg(direction), swizzle, rotation, out var v0, out var v1, out var v2);

            Gizmos.DrawLine(v0, v1);
            Gizmos.DrawLine(v1, v2);
            Gizmos.DrawLine(v2, v0);
        }



        /// <summary>
        /// 내부가 채워진 정삼각형 기즈모를 그립니다. <br/>
        /// 중심, 반지름, 8방향을 기반으로 꼭짓점을 계산하여 면을 채웁니다.
        /// </summary>
        /// <param name="center">정삼각형 기즈모의 중심 좌표</param>
        /// <param name="radius">외접원 반지름</param>
        /// <param name="direction">정삼각형이 향할 방향 (EDirection8)</param>
        /// <param name="swizzle">좌표 스위즐 방식</param>
        /// <param name="rotation">
        /// 기본값이면 Quaternion.Euler( 90, 0, 0)이 적용되어 수평으로 보이도록 합니다.
        /// </param>
        public static void DrawSolidTriangle(Vector3 center, float radius, EDirection8 direction,
                                             GridLayout.CellSwizzle swizzle = GridLayout.CellSwizzle.XYZ,
                                             Quaternion rotation = default)
        {
            if (rotation == default) { rotation = Quaternion.Euler(90, 0, 0); }

            GetTriPoints(center, radius, GetDirOffsetDeg(direction), swizzle, rotation, out s_Tri[0], out s_Tri[1], out s_Tri[2]);

            using (new Handles.DrawingScope(Gizmos.color))
            {
                Handles.DrawAAConvexPolygon(s_Tri);   //? GC 0: 재사용 배열 전달
            }
        }



        //==================================================================================================================



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float GetDirOffsetDeg(EDirection4 d)
        {
            //. 스위치 표현식 자체는 GC 없음
            return d switch
            {
                EDirection4.Up => 0f,
                EDirection4.Right => -90f,
                EDirection4.Down => 180f,
                EDirection4.Left => 90f,
                _ => 0f
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float GetDirOffsetDeg(EDirection8 d)
        {
            return d switch
            {
                EDirection8.Up => 0f,
                EDirection8.RightUp => -45f,
                EDirection8.Right => -90f,
                EDirection8.RightDown => -135f,
                EDirection8.Down => 180f,
                EDirection8.LeftDown => 135f,
                EDirection8.Left => 90f,
                EDirection8.LeftUp => 45f,
                _ => 0f
            };
        }

        /// <summary>
        /// 정삼각형 꼭짓점 3개를 계산하여 반환 (GC 0)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void GetTriPoints(in Vector3 center, float radius, float dirOffsetDeg,
                                 GridLayout.CellSwizzle swizzle, in Quaternion rotation,
                                 out Vector3 v0, out Vector3 v1, out Vector3 v2)
        {
            //. 각도 계산 — 120° 간격
            const float STEP = 120f * Mathf.Deg2Rad;
            float a0 = (90f + dirOffsetDeg) * Mathf.Deg2Rad;

            //. cos/sin 3회 — 구조체만 사용, 힙 할당 없음
            float c0 = Mathf.Cos(a0); float s0 = Mathf.Sin(a0);
            float c1 = Mathf.Cos(a0 + STEP); float s1 = Mathf.Sin(a0 + STEP);
            float c2 = Mathf.Cos(a0 + STEP * 2); float s2 = Mathf.Sin(a0 + STEP * 2);

            //. 회전/스위즐 후 중심 더하기
            Vector3 p0 = new Vector3(c0, s0, 0f) * radius;
            Vector3 p1 = new Vector3(c1, s1, 0f) * radius;
            Vector3 p2 = new Vector3(c2, s2, 0f) * radius;

            //. 스위즐 → 회전 → 중심
            v0 = center + (rotation * p0.SwizzlesVector(swizzle));
            v1 = center + (rotation * p1.SwizzlesVector(swizzle));
            v2 = center + (rotation * p2.SwizzlesVector(swizzle));
        }



        #region Legacy

        ///// <summary>
        ///// 정삼각형 기즈모(와이어 형태)를 그립니다. <br/>
        ///// 중심, 반지름, 방향을 기반으로 꼭짓점을 계산하여 외곽선을 생성합니다.
        ///// </summary>
        ///// <param name="center">정삼각형 기즈모의 중심 좌표</param>
        ///// <param name="radius">외접원 반지름</param>
        ///// <param name="direction">정삼각형이 향할 방향 (EDirection4)</param>
        ///// <param name="swizzle">좌표 스위즐 방식</param>
        ///// <param name="rotation">
        ///// 기본값이면 Quaternion.Euler(-90, 0, 0)이 적용되어 2D 평면에서 올바른 방향으로 표시됩니다.
        ///// </param>
        //public static void DrawTriangle(Vector3 center, float radius, EDirection4 direction,
        //                                GridLayout.CellSwizzle swizzle = GridLayout.CellSwizzle.XYZ,
        //                                Quaternion rotation = default)
        //{
        //    if (rotation == default) { rotation = Quaternion.Euler(-90, 0, 0); }

        //    //. 방향 오프셋
        //    float dirOffsetDeg = direction switch
        //    {
        //        EDirection4.Up => 0f,
        //        EDirection4.Right => -90f,
        //        EDirection4.Down => 180f,
        //        EDirection4.Left => 90f,
        //        _ => 0f
        //    };

        //    Vector3[] v = new Vector3[3];
        //    for (int i = 0; i < 3; i++)
        //    {
        //        float angDeg = 90f + 120f * i + dirOffsetDeg;
        //        Vector3 p = new Vector3(Mathf.Cos(angDeg * Mathf.Deg2Rad),
        //                                Mathf.Sin(angDeg * Mathf.Deg2Rad)) * radius;
        //        v[i] = rotation * p.SwizzlesVector(swizzle);
        //    }

        //    Gizmos.DrawLine(center + v[0], center + v[1]);
        //    Gizmos.DrawLine(center + v[1], center + v[2]);
        //    Gizmos.DrawLine(center + v[2], center + v[0]);
        //}



        ///// <summary>
        ///// 내부가 채워진 정삼각형 기즈모를 그립니다. <br/>
        ///// 중심, 반지름, 방향을 기반으로 꼭짓점을 계산하여 면을 채웁니다.
        ///// </summary>
        ///// <param name="center">정삼각형 기즈모의 중심 좌표</param>
        ///// <param name="radius">외접원 반지름</param>
        ///// <param name="direction">정삼각형이 향할 방향 (EDirection4)</param>
        ///// <param name="swizzle">좌표 스위즐 방식</param>
        ///// <param name="rotation">
        ///// 기본값이면 Quaternion.Euler(90, 0, 0)이 적용되어 수평으로 보이도록 합니다.
        ///// </param>
        //public static void DrawSolidTriangle(Vector3 center, float radius, EDirection4 direction,
        //                                     GridLayout.CellSwizzle swizzle = GridLayout.CellSwizzle.XYZ,
        //                                     Quaternion rotation = default)
        //{
        //    if (rotation == default) { rotation = Quaternion.Euler(90, 0, 0); }

        //    float dirOffsetDeg = direction switch
        //    {
        //        EDirection4.Up => 0f,
        //        EDirection4.Right => -90f,
        //        EDirection4.Down => 180f,
        //        EDirection4.Left => 90f,
        //        _ => 0f
        //    };

        //    Vector3[] v = new Vector3[3];
        //    for (int i = 0; i < 3; i++)
        //    {
        //        float angDeg = 90f + 120f * i + dirOffsetDeg;
        //        Vector3 p = new Vector3(Mathf.Cos(angDeg * Mathf.Deg2Rad),
        //                                Mathf.Sin(angDeg * Mathf.Deg2Rad)) * radius;
        //        v[i] = rotation * p.SwizzlesVector(swizzle);
        //    }

        //    Color org = Handles.color;
        //    Handles.color = Gizmos.color;

        //    Handles.DrawAAConvexPolygon(center + v[0], center + v[1], center + v[2]);

        //    Handles.color = org;
        //}



        ///// <summary>
        ///// 정삼각형 기즈모(와이어 형태)를 그립니다. <br/>
        ///// 중심, 반지름, 8방향을 기반으로 꼭짓점을 계산하여 외곽선을 생성합니다.
        ///// </summary>
        ///// <param name="center">정삼각형 기즈모의 중심 좌표</param>
        ///// <param name="radius">외접원 반지름</param>
        ///// <param name="direction">정삼각형이 향할 방향 (EDirection8)</param>
        ///// <param name="swizzle">좌표 스위즐 방식</param>
        ///// <param name="rotation">
        ///// 기본값이면 Quaternion.Euler(-90, 0, 0)이 적용되어 2D 평면에서 올바른 방향으로 표시됩니다.
        ///// </param>
        //public static void DrawTriangle(Vector3 center, float radius, EDirection8 direction,
        //                                GridLayout.CellSwizzle swizzle = GridLayout.CellSwizzle.XYZ,
        //                                Quaternion rotation = default)
        //{
        //    if (rotation == default) { rotation = Quaternion.Euler(-90, 0, 0); }

        //    //. 방향 오프셋
        //    float dirOffsetDeg = direction switch
        //    {
        //        EDirection8.Up => 0f,
        //        EDirection8.RightUp => -45f,
        //        EDirection8.Right => -90f,
        //        EDirection8.RightDown => -135f,
        //        EDirection8.Down => 180f,
        //        EDirection8.LeftDown => 135f,
        //        EDirection8.Left => 90f,
        //        EDirection8.LeftUp => 45f,
        //        _ => 0f
        //    };

        //    Vector3[] v = new Vector3[3];
        //    for (int i = 0; i < 3; i++)
        //    {
        //        float angDeg = 90f + 120f * i + dirOffsetDeg;          //? 꼭짓점 회전 각도
        //        Vector3 p = new Vector3(Mathf.Cos(angDeg * Mathf.Deg2Rad),
        //                                Mathf.Sin(angDeg * Mathf.Deg2Rad)) * radius;
        //        v[i] = rotation * p.SwizzlesVector(swizzle);
        //    }

        //    Gizmos.DrawLine(center + v[0], center + v[1]);
        //    Gizmos.DrawLine(center + v[1], center + v[2]);
        //    Gizmos.DrawLine(center + v[2], center + v[0]);
        //}



        ///// <summary>
        ///// 내부가 채워진 정삼각형 기즈모를 그립니다. <br/>
        ///// 중심, 반지름, 8방향을 기반으로 꼭짓점을 계산하여 면을 채웁니다.
        ///// </summary>
        ///// <param name="center">정삼각형 기즈모의 중심 좌표</param>
        ///// <param name="radius">외접원 반지름</param>
        ///// <param name="direction">정삼각형이 향할 방향 (EDirection8)</param>
        ///// <param name="swizzle">좌표 스위즐 방식</param>
        ///// <param name="rotation">
        ///// 기본값이면 Quaternion.Euler( 90, 0, 0)이 적용되어 수평으로 보이도록 합니다.
        ///// </param>
        //public static void DrawSolidTriangle(Vector3 center, float radius, EDirection8 direction,
        //                                     GridLayout.CellSwizzle swizzle = GridLayout.CellSwizzle.XYZ,
        //                                     Quaternion rotation = default)
        //{
        //    if (rotation == default) { rotation = Quaternion.Euler(90, 0, 0); }

        //    float dirOffsetDeg = direction switch
        //    {
        //        EDirection8.Up => 0f,
        //        EDirection8.RightUp => -45f,
        //        EDirection8.Right => -90f,
        //        EDirection8.RightDown => -135f,
        //        EDirection8.Down => 180f,
        //        EDirection8.LeftDown => 135f,
        //        EDirection8.Left => 90f,
        //        EDirection8.LeftUp => 45f,
        //        _ => 0f
        //    };

        //    Vector3[] v = new Vector3[3];
        //    for (int i = 0; i < 3; i++)
        //    {
        //        float angDeg = 90f + 120f * i + dirOffsetDeg;          //? 꼭짓점 회전 각도
        //        Vector3 p = new Vector3(Mathf.Cos(angDeg * Mathf.Deg2Rad),
        //                                Mathf.Sin(angDeg * Mathf.Deg2Rad)) * radius;
        //        v[i] = rotation * p.SwizzlesVector(swizzle);
        //    }

        //    Color org = Handles.color;
        //    Handles.color = Gizmos.color;

        //    Handles.DrawAAConvexPolygon(center + v[0], center + v[1], center + v[2]);

        //    Handles.color = org;
        //} 

        #endregion



        ///======================================================================================================================================================



        //? 큐브 그리기



        /// <summary>
        /// 지정된 위치와 크기로 회전된 큐브를 그립니다.
        /// </summary>
        /// <param name="position">큐브의 위치</param>
        /// <param name="size">큐브의 크기</param>
        /// <param name="rotation">큐브의 회전 각도</param>
        public static void DrawCubeRotated(Vector3 position, Vector3 size, Vector3 rotation)
        {
            // 이전 행렬을 저장
            Matrix4x4 oldMatrix = Gizmos.matrix;

            // 회전과 위치를 포함한 변환 행렬 설정
            Gizmos.matrix = Matrix4x4.TRS(position, Quaternion.Euler(rotation), Vector3.one);

            // 회전된 정육면체 그리기
            Gizmos.DrawCube(Vector3.zero, size);

            // 이전 행렬로 복원
            Gizmos.matrix = oldMatrix;
        }



        /// <summary>
        /// 지정된 위치와 크기로 회전된 와이어 큐브를 그립니다.
        /// </summary>
        /// <param name="position">와이어 큐브의 위치</param>
        /// <param name="size">와이어 큐브의 크기</param>
        /// <param name="rotation">와이어 큐브의 회전 각도</param>
        public static void DrawWireCubeRotated(Vector3 position, Vector3 size, Vector3 rotation)
        {
            // 이전 행렬을 저장
            Matrix4x4 oldMatrix = Gizmos.matrix;

            // 회전과 위치를 포함한 변환 행렬 설정
            Gizmos.matrix = Matrix4x4.TRS(position, Quaternion.Euler(rotation), Vector3.one);

            // 회전된 와이어 정육면체 그리기
            Gizmos.DrawWireCube(Vector3.zero, size);

            // 이전 행렬로 복원
            Gizmos.matrix = oldMatrix;
        }



        /// <summary>
        /// 기본 45도 각도로 회전된 큐브를 지정된 위치와 크기로 그립니다.
        /// </summary>
        /// <param name="position">큐브의 위치</param>
        /// <param name="size">큐브의 크기</param>
        public static void DrawCubeRotated45(Vector3 position, Vector3 size, GridLayout.CellSwizzle swizzle)
        {
            DrawCubeRotated(position, size, (new Vector3(0, 0, 45).SwizzlesVector(swizzle)));
        }



        /// <summary>
        /// 기본 45도 각도로 회전된 와이어 큐브를 지정된 위치와 크기로 그립니다.
        /// </summary>
        /// <param name="position">와이어 큐브의 위치</param>
        /// <param name="size">와이어 큐브의 크기</param>
        public static void DrawWireCubeRotated45(Vector3 position, Vector3 size, GridLayout.CellSwizzle swizzle)
        {
            DrawWireCubeRotated(position, size, (new Vector3(0, 0, 45).SwizzlesVector(swizzle)));
        }



        /// <summary>
        /// 투명한 큐브를 그립니다.
        /// </summary>
        /// <param name="center">큐브의 중심</param>
        /// <param name="size">큐브의 크기</param>
        public static void DrawCubeTransparent(Vector3 center, Vector3 size)
        {
            TempGizmoColor_Action(Color.clear, () =>
            {
                Gizmos.DrawCube(center, size);
            });
        }



        /// <summary>
        /// 다수의 투명한 큐브를 그립니다.
        /// </summary>
        /// <param name="boundsArray">큐브의 범위 배열</param>
        public static void DrawCubesTransparent(params Bounds[] boundsArray)
        {
            foreach (var bounds in boundsArray)
            {
                DrawCubeTransparent(bounds.center, bounds.size);
            }
        }



        /// <summary>
        /// 지정된 크기를 가진 여러 개의 실체 큐브를 그립니다. <br/>
        /// 이 메서드는 각 위치에 대해 Gizmos.DrawCube를 호출하여 큐브를 그립니다.
        /// </summary>
        /// <param name="cubeSize">각 큐브의 크기</param>
        /// <param name="positions">그릴 큐브들의 위치 배열</param>
        public static void DrawCubesMultiple(Vector3 cubeSize, params Vector3[] positions)
        {
            foreach (var position in positions)
            {
                Gizmos.DrawCube(position, cubeSize);
            }
        }



        /// <summary>
        /// 지정된 크기를 가진 여러 개의 와이어 큐브를 그립니다. <br/>
        /// 이 메서드는 각 위치에 대해 Gizmos.DrawWireCube를 호출하여 와이어 큐브를 그립니다.
        /// </summary>
        /// <param name="cubeSize">각 큐브의 크기</param>
        /// <param name="positions">그릴 큐브들의 위치 배열</param>
        public static void DrawWireCubesMultiple(Vector3 cubeSize, params Vector3[] positions)
        {
            foreach (var position in positions)
            {
                Gizmos.DrawWireCube(position, cubeSize);
            }
        }



        ///======================================================================================================================================================



        //? 십자 그리기



        /// <summary>
        /// 2D 환경에서 중심점을 기준으로 + 형태의 십자를 그립니다.
        /// </summary>
        /// <param name="center">십자의 중심 월드 좌표</param>
        /// <param name="crossSize">가로·세로 길이 (각 축 전체 길이)</param>
        /// <param name="swizzle">좌표 스위즐 설정</param>
        public static void DrawCross2D(Vector3 center, Vector2 crossSize, GridLayout.CellSwizzle swizzle)
        {
            //. 축별 절반 길이 계산 후 스위즐 적용
            Vector3 halfHorizontal = new Vector3(crossSize.x / 2f, 0f, 0f).SwizzlesVector(swizzle);
            Vector3 halfVertical = new Vector3(0f, crossSize.y / 2f, 0f).SwizzlesVector(swizzle);
            Gizmos.DrawLine(center - halfHorizontal, center + halfHorizontal);
            Gizmos.DrawLine(center - halfVertical, center + halfVertical);
        }



        /// <summary>
        /// 3D 공간에서 중심점을 기준으로 X·Y·Z 세 축 방향의 십자를 그립니다.
        /// </summary>
        /// <param name="center">십자의 중심 월드 좌표</param>
        /// <param name="crossSize">각 축의 전체 길이 (x, y, z)</param>
        public static void DrawCross(Vector3 center, Vector3 crossSize)
        {
            //. 축별 절반 길이 계산
            Vector3 halfX = new Vector3(crossSize.x / 2f, 0f, 0f);
            Vector3 halfY = new Vector3(0f, crossSize.y / 2f, 0f);
            Vector3 halfZ = new Vector3(0f, 0f, crossSize.z / 2f);

            Gizmos.DrawLine(center - halfX, center + halfX);
            Gizmos.DrawLine(center - halfY, center + halfY);
            Gizmos.DrawLine(center - halfZ, center + halfZ);
        }



        ///======================================================================================================================================================



        //? 그리드



        /// <summary>
        /// 2D 평면에서 중심을 기준으로 <paramref name="size"/> 만큼의 영역에 격자선을 그립니다.
        /// </summary>
        /// <param name="center">그리드 중심 월드 좌표</param>
        /// <param name="size">그리드 전체 크기 (가로, 세로)</param>
        /// <param name="gridUnit">격자 단위 (가로, 세로)</param>
        /// <param name="swizzle">좌표 스위즐 설정</param>
        /// <param name="drawEndLine">true면 가장자리 선 포함, false면 내부 격자만 그림</param>
        public static void DrawGridLine2D(Vector3 center, Vector2 size, Vector2 gridUnit, GridLayout.CellSwizzle swizzle, bool drawEndLine)
        {
            //! 단위가 0 이하이면 그릴 수 없음
            if (gridUnit.x <= 0f || gridUnit.y <= 0f) { return; }

            //. 반폭·반높이 계산
            float halfW = size.x * 0.5f;
            float halfH = size.y * 0.5f;

            //. 경계 좌표
            float left = -halfW;
            float right = halfW;
            float bottom = -halfH;
            float top = halfH;

            float epsilon = 0.0001f;

            //? 수직선 (Y 축 방향) ---------------------------------------------------
            for (float x = left; x <= right + epsilon; x += gridUnit.x)
            {
                if (!drawEndLine && (Mathf.Abs(x - left) < epsilon || Mathf.Abs(x - right) < epsilon))
                    continue;

                Vector3 localStart = new Vector3(x, bottom, 0f);
                Vector3 localEnd = new Vector3(x, top, 0f);

                Gizmos.DrawLine(center + localStart.SwizzlesVector(swizzle),
                                center + localEnd.SwizzlesVector(swizzle));
            }

            //? 수평선 (X 축 방향) ---------------------------------------------------
            for (float y = bottom; y <= top + epsilon; y += gridUnit.y)
            {
                if (!drawEndLine && (Mathf.Abs(y - bottom) < epsilon || Mathf.Abs(y - top) < epsilon))
                    continue;

                Vector3 localStart = new Vector3(left, y, 0f);
                Vector3 localEnd = new Vector3(right, y, 0f);

                Gizmos.DrawLine(center + localStart.SwizzlesVector(swizzle),
                                center + localEnd.SwizzlesVector(swizzle));
            }
        }




        /// <summary>
        /// Grid 셀들의 <b>중심 좌표 배열</b>을 받아, <u>중복 라인 없이</u> 격자 기즈모를 한 번에 그립니다.
        /// </summary>
        /// <param name="cellCenters">그리드를 이루는 셀들의 중심 월드 좌표 목록</param>
        /// <param name="cellSize">
        ///   셀 하나의 크기 (가로, 세로). <br/>
        ///   ※ <see cref="GridLayout.CellSwizzle"/> 로 X·Y 축이 뒤바뀌는 경우가 있으므로
        ///     ‘Swizzle 적용 전’ 원본 값을 전달해야 합니다.
        /// </param>
        /// <param name="swizzle">카메라 모드·좌표계에 맞춘 Swizzle</param>
        /// <param name="drawEndLine">외곽선 포함 여부</param>
        public static void DrawGridByCells2D
        (
            IEnumerable<Vector3> cellCenters,
            Vector2 cellSize,
            float zOffset=0f,
            GridLayout.CellSwizzle swizzle = GridLayout.CellSwizzle.XYZ,
            bool drawEndLine = true
        )
        {
            //! 유효성 검사 --------------------------------------------------------
            if (cellCenters == null) { return; }

            bool hasAny = false;

            //. 바운딩 박스 계산 ---------------------------------------------------
            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;

            foreach (var wp in cellCenters)
            {
                Vector3 p = wp.SwizzlesVector(swizzle);  //. Swizzle 변환 후 XY 평면 기준
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y);
                maxY = Mathf.Max(maxY, p.y);
                hasAny = true;
            }

            if (!hasAny) { return; }

            //. 바운드 → 중심·전체 크기 ------------------------------------------
            //  (셀 중심 간 간격 == 셀 크기) 라는 가정하에, 외곽까지 포함하려면
            //  min 은 -½ CellSize, max 는 +½ CellSize 보정
            //float halfW = cellSize.x * 0.5f;
            //float halfH = cellSize.y * 0.5f;

            Vector2 size = new Vector2((maxX - minX) + cellSize.x,
                                         (maxY - minY) + cellSize.y);
            Vector3 center = new Vector3((minX + maxX) * 0.5f,
                                         (minY + maxY) * 0.5f,
                                         zOffset);

            //. Swizzle 역변환 → 월드 좌표계 --------------------------------------
            center = center.SwizzlesVector(swizzle);

            //. 실제 그리기 -------------------------------------------------------
            DrawGridLine2D(center, size, cellSize, swizzle, drawEndLine);
        }



        ///======================================================================================================================================================



        //? 스피어 그리기



        /// <summary>
        /// 투명한 스피어를 그립니다.
        /// </summary>
        /// <param name="center">스피어의 중심</param>
        /// <param name="radius">스피어의 반지름</param>
        public static void DrawSphereTransparent(Vector3 center, float radius)
        {
            TempGizmoColor_Action(Color.clear, () =>
            {
                Gizmos.DrawSphere(center, radius);
            });
        }



        ///======================================================================================================================================================



        //? 기타 도형 그리기



        /// <summary>
        /// 원뿔을 지정된 위치와 방향으로 그립니다.
        /// </summary>
        /// <param name="position">원뿔의 중심 위치</param>
        /// <param name="direction">원뿔의 방향</param>
        /// <param name="baseRadius">원뿔의 밑면 반지름</param>
        /// <param name="height">원뿔의 높이</param>
        public static void DrawCone(Vector3 position, Vector3 direction, float baseRadius, float height)
        {
            int segments = 20;
            Vector3 baseCenter = position + direction.normalized * height;

            // 원뿔 밑면의 점들 계산
            Vector3[] basePoints = new Vector3[segments];
            float angleStep = 360f / segments;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 point = baseCenter + Quaternion.AngleAxis(angle, direction) * Vector3.Cross(direction.normalized, Vector3.up).normalized * baseRadius;
                basePoints[i] = point;
            }

            // 원뿔 옆면 그리기
            for (int i = 0; i < segments; i++)
            {
                Gizmos.DrawLine(position, basePoints[i]);
                Gizmos.DrawLine(basePoints[i], basePoints[(i + 1) % segments]);
            }
        }



        ///======================================================================================================================================================



        //? 점선 그리기



        /// <summary>
        /// 두 지점 <paramref name="start"/> 와 <paramref name="end"/> 를 잇는 <b>점선</b>을 그립니다. <br/>
        /// <paramref name="dashSize"/> 는 선이 실제로 그려지는 “점”(or 대시)의 길이이고, <paramref name="gapSize"/> 는 점과 점 사이 공백 길이입니다.
        /// </summary>
        /// <param name="start">점선의 시작 월드 좌표</param>
        /// <param name="end">점선의 끝 월드 좌표</param>
        /// <param name="dashSize">각 점(또는 대시) 길이 (기본: 0.2)</param>
        /// <param name="gapSize">점 사이 간격 (기본: 0.1)</param>
        public static void DrawDottedLine(Vector3 start, Vector3 end, float dashSize = 0.2f, float gapSize = 0.1f)
        {
            //! 유효성 검사
            if (dashSize <= 0f) { dashSize = 0.01f; }   // 최소 확보
            if (gapSize < 0f) { gapSize = 0f; }

            //. 방향·거리 계산
            Vector3 dir = end - start;
            float dist = dir.magnitude;

            //? 동일한 점일 때는 그리지 않음
            if (dist <= Mathf.Epsilon) { return; }

            Vector3 dirN = dir / dist;                  //. 단위 방향
            float pos = 0f;                          //. 현재 진행 거리

            while (pos < dist)
            {
                float dash = Mathf.Min(dashSize, dist - pos);        //. 남은 구간보다 길면 자름
                Vector3 segStart = start + dirN * pos;
                Vector3 segEnd = segStart + dirN * dash;

                Gizmos.DrawLine(segStart, segEnd);                   //. 실제 점(대시) 그리기

                pos += dash + gapSize;                               //. 다음 점 시작 위치로 이동
            }
        }



        /// <summary>
        /// <see cref="Gizmos.DrawWireCube(UnityEngine.Vector3,UnityEngine.Vector3)"/> 와 동일한 인자·행렬 동작을 따르며,
        /// 한 축이 0(또는 거의 0)인 경우에는 4개 모서리만 점선으로 그려 불필요한 호출을 줄입니다.
        /// </summary>
        /// <param name="center">큐브(또는 평면)의 중심 (현재 <see cref="Gizmos.matrix"/> 기준)</param>
        /// <param name="size">전체 크기. 세 축 중 하나가 0(≈Epsilon)이면 2D 사각형으로 간주</param>
        /// <param name="dashSize">대시 길이 (기본: 0.2)</param>
        /// <param name="gapSize">대시 사이 간격 (기본: 0.1)</param>
        public static void DrawDottedWireCube(Vector3 center, Vector3 size,
                                              float dashSize = 0.2f, float gapSize = 0.1f)
        {
            //! 크기가 전부 0이면 그릴 필요 없음
            if (size == Vector3.zero) { return; }

            //! 유효성 보정
            if (dashSize <= 0f) { dashSize = 0.01f; }
            if (gapSize < 0f) { gapSize = 0f; }

            //. 반 크기 및 로컬 꼭짓점
            Vector3 half = size * 0.5f;
            Vector3[] v =
            {
                new Vector3(-half.x, -half.y, -half.z),
                new Vector3( half.x, -half.y, -half.z),
                new Vector3( half.x, -half.y,  half.z),
                new Vector3(-half.x, -half.y,  half.z),
                new Vector3(-half.x,  half.y, -half.z),
                new Vector3( half.x,  half.y, -half.z),
                new Vector3( half.x,  half.y,  half.z),
                new Vector3(-half.x,  half.y,  half.z),
            };

            //. 월드 변환 (center 포함) ─ Gizmos.matrix 고려
            Matrix4x4 m = Gizmos.matrix;
            for (int i = 0; i < 8; ++i)
            {
                v[i] = m.MultiplyPoint3x4(v[i] + center);
            }

            //. 어느 축이 0(≈Epsilon)인가?
            bool xFlat = Mathf.Abs(size.x) <= Mathf.Epsilon;
            bool yFlat = Mathf.Abs(size.y) <= Mathf.Epsilon;
            bool zFlat = Mathf.Abs(size.z) <= Mathf.Epsilon;

            //. 모서리 인덱스 집합 선택
            //? 2D 평면 사각형 (4개)
            if (xFlat ^ yFlat ^ zFlat)                     //! 정확히 한 축만 0일 때
            {
                int[,] edges =
                    xFlat ? new int[,] { { 0, 4 }, { 4, 7 }, { 7, 3 }, { 3, 0 } }   // YZ 평면
                 : yFlat ? new int[,] { { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 } }   // XZ 평면
                         : new int[,] { { 0, 1 }, { 1, 5 }, { 5, 4 }, { 4, 0 } };  // XY 평면

                for (int i = 0; i < 4; ++i)
                {
                    DrawDottedLine(v[edges[i, 0]], v[edges[i, 1]], dashSize, gapSize);
                }
                return;
            }

            //. 일반 큐브 (12개)
            int[,] full =
            {
                {0,1},{1,2},{2,3},{3,0},
                {4,5},{5,6},{6,7},{7,4},
                {0,4},{1,5},{2,6},{3,7}
            };
            for (int i = 0; i < 12; ++i)
            {
                DrawDottedLine(v[full[i, 0]], v[full[i, 1]], dashSize, gapSize);
            }
        }



        ///======================================================================================================================================================



        //? 곡선 그리기



        /// <summary>
        /// <see cref="Gizmos.DrawLine"/> 과 유사하지만, 직선이 아닌 <b>곡선</b>을 그립니다. <br/>
        /// 두 점 간의 거리를 비교해 더 긴 축(X‧Y 중)에 수직인 축으로 굴곡을 주며, <paramref name="bendFactor"/> 로 휘어짐 정도를 조절합니다.
        /// </summary>
        /// <param name="start">곡선 시작 월드 좌표</param>
        /// <param name="end">곡선 끝 월드 좌표</param>
        /// <param name="swizzle">좌표 스위즐 방식</param>
        /// <param name="bendFactor">곡선 휘어짐 강도 (기본: 0.3, 값이 클수록 더 크게 휩니다)</param>
        /// <param name="segments">샘플링 세그먼트 수 (기본: 20, 값이 클수록 곡선이 부드러워집니다)</param>
        public static void DrawCurvedLine(Vector3 start, Vector3 end,
                                          GridLayout.CellSwizzle swizzle = GridLayout.CellSwizzle.XYZ,
                                          float bendFactor = 0.3f, int segments = 20)
        {
            //! 동일한 점이면 그릴 필요 없음
            if (start == end) { return; }

            //? Swizzle에 따른 로컬 XY 축 매핑 -------------------------------------------------
            Vector3 xAxis = Vector3.right;
            Vector3 yAxis = Vector3.up;

            switch (swizzle)
            {
                case GridLayout.CellSwizzle.XYZ: break;
                case GridLayout.CellSwizzle.XZY:
                yAxis = Vector3.forward;
                break;
                case GridLayout.CellSwizzle.YXZ:
                xAxis = Vector3.up; yAxis = Vector3.right;
                break;
                case GridLayout.CellSwizzle.YZX:
                xAxis = Vector3.up; yAxis = Vector3.forward;
                break;
                case GridLayout.CellSwizzle.ZXY:
                xAxis = Vector3.forward; yAxis = Vector3.right;
                break;
                case GridLayout.CellSwizzle.ZYX:
                xAxis = Vector3.up; yAxis = Vector3.forward;
                break;
            }

            //. 더 긴 축과 수직인 축을 휘어짐 방향으로 선택 ------------------------------
            Vector3 delta = end - start;
            float xDist = Mathf.Abs(Vector3.Dot(delta, xAxis));
            float yDist = Mathf.Abs(Vector3.Dot(delta, yAxis));

            Vector3 bendAxis = (xDist >= yDist) ? yAxis : xAxis;    // 긴 축(X‧Y) ⟂ bendAxis

            //. 컨트롤 포인트(1개) 계산 ― 2차 베지어 -------------------------------------
            float dist = delta.magnitude;
            Vector3 mid = (start + end) * 0.5f;
            Vector3 ctrl = mid + bendAxis * dist * bendFactor;

            //. 곡선 그리기 (선분 분할) ----------------------------------------------------
            segments = Mathf.Max(1, segments);

            Vector3 prev = start;
            for (int i = 1; i <= segments; ++i)
            {
                float t = i / (float)segments;

                //. 2차 베지어 공식
                Vector3 p0 = Vector3.Lerp(start, ctrl, t);
                Vector3 p1 = Vector3.Lerp(ctrl, end, t);
                Vector3 curr = Vector3.Lerp(p0, p1, t);

                Gizmos.DrawLine(prev, curr);
                prev = curr;
            }
        }



        ///======================================================================================================================================================



        //? 화살표 그리기



        /// <summary>
        /// 지정된 시작점과 끝점 사이에 기즈모로 화살표를 그립니다.
        /// 화살표의 끝에 화살촉을 추가하여 방향을 명확히 표시합니다.
        /// </summary>
        /// <param name="start">화살표의 시작점</param>
        /// <param name="end">화살표의 끝점</param>
        /// <param name="arrowHeadAngle">화살촉의 각도</param>
        /// <param name="arrowHeadLength">화살촉의 길이</param>
        public static void DrawArrow(Vector3 start, Vector3 end, float arrowHeadAngle = 20.0f, float arrowHeadLength = 0.25f)
        {
            // 화살표 본체 그리기
            Gizmos.DrawLine(start, end);

            // 화살표 방향 계산
            Vector3 direction = (end - start).normalized;

            if (direction == Vector3.zero)
            {
                return; // direction 벡터가 zero vector인 경우 함수 종료
            }

            // 카메라의 위치에서 end까지의 방향 벡터 계산
            Vector3 cameraDirection = (Camera.current.transform.position - end).normalized;

            // 카메라 방향과 화살표 방향의 수직 벡터 계산
            Vector3 right = Vector3.Cross(direction, cameraDirection).normalized;
            Vector3 up = Vector3.Cross(right, direction).normalized;

            // 화살촉의 오른쪽 및 왼쪽 벡터 계산
            Vector3 rightArrowHead = Quaternion.LookRotation(direction) * Quaternion.Euler(0, arrowHeadAngle, 0) * Vector3.back;
            Vector3 leftArrowHead = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -arrowHeadAngle, 0) * Vector3.back;

            // 화살촉 그리기
            Gizmos.DrawLine(end, end + rightArrowHead * arrowHeadLength);
            Gizmos.DrawLine(end, end + leftArrowHead * arrowHeadLength);
        }



        /// <summary>
        /// 시작점과 끝점 모두에 화살촉이 있는 양방향 화살표를 기즈모로 그립니다.
        /// </summary>
        /// <param name="start">화살표의 시작점</param>
        /// <param name="end">화살표의 끝점</param>
        /// <param name="arrowHeadAngle">화살촉의 각도</param>
        /// <param name="arrowHeadLength">화살촉의 길이</param>
        public static void DrawDoubleArrow(Vector3 start, Vector3 end,
                                           float arrowHeadAngle = 20.0f,
                                           float arrowHeadLength = 0.25f)
        {
            //. 본체 라인
            Gizmos.DrawLine(start, end);

            Vector3 direction = (end - start).normalized;
            //! 동일 지점이면 화살촉을 그릴 수 없음
            if (direction == Vector3.zero) { return; }

            //? 화살촉을 한 번에 그리는 지역 함수
            static void DrawHead(Vector3 pos, Vector3 dir,
                                 float angle, float length)
            {
                Vector3 camDir = (Camera.current.transform.position - pos).normalized;
                Vector3 right = Vector3.Cross(dir, camDir).normalized;
                Vector3 up = Vector3.Cross(right, dir).normalized;

                // Quaternion 방식 대신 직관적 계산 (카메라 기준 기즈모 뒤틀림 방지)
                Vector3 headDir1 = (Quaternion.AngleAxis(angle, up) * -dir).normalized;
                Vector3 headDir2 = (Quaternion.AngleAxis(-angle, up) * -dir).normalized;

                Gizmos.DrawLine(pos, pos + headDir1 * length);
                Gizmos.DrawLine(pos, pos + headDir2 * length);
            }

            DrawHead(end, direction, arrowHeadAngle, arrowHeadLength); //. 끝쪽 화살촉
            DrawHead(start, -direction, arrowHeadAngle, arrowHeadLength); //. 시작쪽 화살촉
        }



        /// <summary>
        /// 주어진 위치(<paramref name="center"/>)를 <b>기즈모 형상의 정중앙</b>으로 하여
        /// <paramref name="direction"/> 방향으로 “화살촉(chevron)”만을 그립니다.
        /// <br/>※ 여기서 ‘정중앙’은 팁과 두 날개 끝점(총 3점)의 기하학적 중심(centroid)입니다.
        /// </summary>
        /// <param name="center">그려질 화살촉 형상의 <b>정중앙</b> 좌표</param>
        /// <param name="arrowHeadLength">화살촉의 길이(크기). <paramref name="center"/> 바로 다음 인자</param>
        /// <param name="direction">화살촉이 가리킬 방향 벡터(스위즐 적용 전)</param>
        /// <param name="swizzle">좌표 스위즐(그릴 좌표계)</param>
        /// <param name="arrowHeadAngle">화살촉 각도(도). 기본 20°</param>
        public static void DrawArrowHead
        (
            Vector3 center,
            float arrowHeadLength,
            Vector3 direction,
            GridLayout.CellSwizzle swizzle = GridLayout.CellSwizzle.XYZ,
            float arrowHeadAngle = 20.0f
        )
        {
            //. 방향 벡터 스위즐 적용 및 정규화
            Vector3 dir = direction.SwizzlesVector(swizzle);
            if (dir == Vector3.zero) { return; }                          //! 방향이 없으면 그릴 수 없음
            dir.Normalize();

            //? 씬 뷰 카메라 기준 보정(뒤틀림 최소화용 보조 축 계산)
            Vector3 camDir = Camera.current != null
                ? (Camera.current.transform.position - center).normalized
                : Vector3.forward;

            Vector3 right = Vector3.Cross(dir, camDir);
            if (right.sqrMagnitude <= 1e-6f)
            {
                //. 카메라가 방향과 거의 평행한 특수 케이스 보정
                Vector3 arbitrary = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
                right = Vector3.Cross(dir, arbitrary);
            }
            right.Normalize();

            Vector3 up = Vector3.Cross(right, dir).normalized;

            //. 화살촉 양 날개 방향(팁에서 뒤로 꺾이게 -dir 기준 ±각도 회전)
            Quaternion rotPos = Quaternion.AngleAxis(+arrowHeadAngle, up);
            Quaternion rotNeg = Quaternion.AngleAxis(-arrowHeadAngle, up);

            Vector3 wingDir1 = (rotPos * -dir).normalized;
            Vector3 wingDir2 = (rotNeg * -dir).normalized;

            //? 중심 맞추기: 3점(팁 T, 양 날개 끝 E1/E2)의 centroid가 center가 되도록 팁 위치를 역산
            //. centroid = (T + (T + L*wing1) + (T + L*wing2)) / 3 = T + (L/3)*(wing1 + wing2)
            //. => T = center - (L/3)*(wing1 + wing2)
            Vector3 tip = center - (arrowHeadLength / 3f) * (wingDir1 + wingDir2);

            //. 최종 그리기: 팁 → 양 날개 끝
            Gizmos.DrawLine(tip, tip + wingDir1 * arrowHeadLength);
            Gizmos.DrawLine(tip, tip + wingDir2 * arrowHeadLength);
        }




        /// <summary>
        /// 주어진 위치(<paramref name="center"/>)에서 <paramref name="direction"/> (8방향)으로
        /// “화살촉(chevron)”만을 그립니다. 내부적으로 Vector3 방향 버전을 호출합니다.
        /// <br/>※ <paramref name="center"/> 는 팁이 아닌 <b>베이스</b>이며,
        /// 팁은 <c>center + dir * arrowHeadLength</c> 로 계산됩니다.
        /// </summary>
        /// <param name="center">화살촉을 시작할 기준 위치 (팁 아님)</param>
        /// <param name="arrowHeadLength">화살촉의 길이(크기). <paramref name="center"/> 다음 인자</param>
        /// <param name="direction">화살촉이 가리킬 8방향</param>
        /// <param name="swizzle">좌표 스위즐(그릴 좌표계)</param>
        /// <param name="arrowHeadAngle">화살촉 각도(도). 기본 20°</param>
        public static void DrawArrowHead
        (
            Vector3 center,
            float arrowHeadLength,
            EDirection8 direction,
            GridLayout.CellSwizzle swizzle = GridLayout.CellSwizzle.XYZ,
            float arrowHeadAngle = 20.0f
        )
        {
            DrawArrowHead(center, arrowHeadLength, Direction8ToVector(direction), swizzle, arrowHeadAngle);

            /// <summary>
            /// <see cref="EDirection8"/> 을 (Swizzle 적용 전) XY 평면상의 기본 방향 벡터로 변환합니다.
            /// </summary>
            static Vector3 Direction8ToVector(EDirection8 dir)
            {
                switch (dir)
                {
                    case EDirection8.Left: return Vector3.left;
                    case EDirection8.Right: return Vector3.right;
                    case EDirection8.Down: return Vector3.down;
                    case EDirection8.Up: return Vector3.up;
                    case EDirection8.LeftDown: return (Vector3.left + Vector3.down).normalized;
                    case EDirection8.LeftUp: return (Vector3.left + Vector3.up).normalized;
                    case EDirection8.RightDown: return (Vector3.right + Vector3.down).normalized;
                    case EDirection8.RightUp: return (Vector3.right + Vector3.up).normalized;
                    default: return Vector3.right;
                }
            }
        }



        ///======================================================================================================================================================



        //? 방향 기즈모 그리기



        /// <summary>
        /// 지정된 시작점과 끝점 사이에 기즈모로 선을 그립니다.
        /// 끝점에서 반지름이 r인 원의 가장자리에 닿는 점까지 그립니다.
        /// </summary>
        /// <param name="start">선의 시작점</param>
        /// <param name="end">선의 끝점</param>
        /// <param name="radius">원 반지름</param>
        public static void DrawLineToCircle(Vector3 start, Vector3 end, float radius = 0.25f)
        {
            // 방향 벡터 계산
            Vector3 direction = end - start;

            // 방향 벡터 정규화
            Vector3 normalizedDirection = direction.normalized;

            // 원 위의 점 계산
            Vector3 pointOnCircle = end - normalizedDirection * radius;

            // 선 그리기
            Gizmos.DrawLine(start, pointOnCircle);
        }



        /// <summary>
        /// GridLayout.CellSwizzle 값을 사용하여 변환된 축 방향으로 화살표와 라벨을 그립니다.
        /// </summary>
        /// <param name="position">기준 위치</param>
        /// <param name="swizzle">GridLayout.CellSwizzle 값</param>
        /// <param name="arrowLength">화살표 길이</param>
        public static void DrawGizmoAxes0(Vector3 position, GridLayout.CellSwizzle swizzle, float arrowLength = 1.0f)
        {
            // 기본 축 벡터
            Vector3 xAxis = Vector3.right;
            Vector3 yAxis = Vector3.up;
            Vector3 zAxis = Vector3.forward;

            // Swizzle 적용
            switch (swizzle)
            {
                case GridLayout.CellSwizzle.XYZ:
                break;
                case GridLayout.CellSwizzle.XZY:
                zAxis = Vector3.up;
                yAxis = Vector3.forward;
                break;
                case GridLayout.CellSwizzle.YXZ:
                yAxis = Vector3.right;
                xAxis = Vector3.up;
                break;
                case GridLayout.CellSwizzle.YZX:
                yAxis = Vector3.right;
                zAxis = Vector3.up;
                xAxis = Vector3.forward;
                break;
                case GridLayout.CellSwizzle.ZXY:
                zAxis = Vector3.right;
                xAxis = Vector3.forward;
                break;
                case GridLayout.CellSwizzle.ZYX:
                zAxis = Vector3.right;
                yAxis = Vector3.forward;
                xAxis = Vector3.up;
                break;
            }

            // 각 축에 대해 화살표와 라벨 그리기
            Gizmos.color = Color.red;
            DrawArrow(position, position + xAxis * arrowLength, 20.0f, 0.25f);
            Handles.Label(position + xAxis * arrowLength, "X", new GUIStyle() { normal = new GUIStyleState() { textColor = Color.red } });

            Gizmos.color = Color.green;
            DrawArrow(position, position + yAxis * arrowLength, 20.0f, 0.25f);
            Handles.Label(position + yAxis * arrowLength, "Y", new GUIStyle() { normal = new GUIStyleState() { textColor = Color.green } });

            Gizmos.color = Color.blue;
            DrawArrow(position, position + zAxis * arrowLength, 20.0f, 0.25f);
            Handles.Label(position + zAxis * arrowLength, "Z", new GUIStyle() { normal = new GUIStyleState() { textColor = Color.blue } });
        }



        /// <summary>
        /// 주어진 위치와 Swizzle 값을 사용하여 축 방향을 나타내는 화살표와 라벨을 그립니다.
        /// </summary>
        /// <param name="position">기준 위치</param>
        /// <param name="swizzle">GridLayout.CellSwizzle 값</param>
        /// <param name="arrowLength">화살표 길이</param>
        public static void DrawGizmoAxes(Vector3 position, GridLayout.CellSwizzle swizzle, float arrowLength = 1.0f)
        {
            // 기본 축 벡터
            Vector3 xAxis = Vector3.right;
            Vector3 yAxis = Vector3.up;
            Vector3 zAxis = Vector3.forward;

            // 라벨에 표시할 축 변환
            string xLabel = "<b><color=red>X</color></b>";
            string yLabel = "<b><color=green>Y</color></b>";
            string zLabel = "<b><color=cyan>Z</color></b>";

            // Swizzle 적용
            ApplySwizzle(ref xAxis, ref yAxis, ref zAxis, ref xLabel, ref yLabel, ref zLabel, swizzle);

            // Swizzle 값 라벨 그리기
            float mouseRadius = 1.0f; // 예시로 반경 1.0f 사용
            if (SU_Editor_Input.IsWithinEditorMouseRadius(position, mouseRadius, swizzle, position))
            {
                string swizzleLabel = $"Swizzle:\n{xLabel}\n{yLabel}\n{zLabel}";
                Handles.Label(position + new Vector3(-0.75f, 0.75f, 0), swizzleLabel, new GUIStyle(CreateLabelStyle(Color.white)) { alignment = TextAnchor.MiddleLeft });
            }

            // 2D 뷰 확인
            bool is2D = Camera.current.orthographic && (Mathf.Abs(Vector3.Dot(Camera.current.transform.forward, Vector3.forward)) > 0.99f || Mathf.Abs(Vector3.Dot(Camera.current.transform.forward, Vector3.back)) > 0.99f);

            // 각 축에 대해 화살표와 라벨 그리기
            float labelOffset = 0.15f; // 라벨 위치 조정

            // 축 그리기 로컬 함수
            void DrawAxis(Vector3 axis, string label, Color color)
            {
                Gizmos.color = color;
                DrawArrow(position, position + axis * arrowLength, 20.0f, 0.25f);
                Handles.Label(position + axis * (arrowLength + labelOffset), label, CreateLabelStyle(color));
            }

            if (!is2D || Mathf.Abs(Vector3.Dot(Camera.current.transform.forward, xAxis)) < 0.1f)
            {
                DrawAxis(xAxis, xLabel, Color.red);
            }

            if (!is2D || Mathf.Abs(Vector3.Dot(Camera.current.transform.forward, yAxis)) < 0.1f)
            {
                DrawAxis(yAxis, yLabel, Color.green);
            }

            if (!is2D || Mathf.Abs(Vector3.Dot(Camera.current.transform.forward, zAxis)) < 0.1f)
            {
                DrawAxis(zAxis, zLabel, Color.cyan);
            }

            // Swizzle 적용 로컬 함수
            void ApplySwizzle(ref Vector3 xAxis, ref Vector3 yAxis, ref Vector3 zAxis, ref string xLabel, ref string yLabel, ref string zLabel, GridLayout.CellSwizzle swizzle)
            {
                switch (swizzle)
                {
                    case GridLayout.CellSwizzle.XYZ:
                    break;
                    case GridLayout.CellSwizzle.XZY:
                    zAxis = Vector3.up;
                    yAxis = Vector3.forward;
                    yLabel = "<b><color=cyan>Z</color> (기존: <color=green>Y</color>)</b>";
                    zLabel = "<b><color=green>Y</color> (기존: <color=cyan>Z</color>)</b>";
                    break;
                    case GridLayout.CellSwizzle.YXZ:
                    yAxis = Vector3.right;
                    xAxis = Vector3.up;
                    xLabel = "<b><color=green>Y</color> (기존: <color=red>X</color>)</b>";
                    yLabel = "<b><color=red>X</color> (기존: <color=green>Y</color>)</b>";
                    break;
                    case GridLayout.CellSwizzle.YZX:
                    yAxis = Vector3.right;
                    zAxis = Vector3.up;
                    xAxis = Vector3.forward;
                    xLabel = "<b><color=cyan>Z</color> (기존: <color=red>X</color>)</b>";
                    yLabel = "<b><color=red>X</color> (기존: <color=green>Y</color>)</b>";
                    zLabel = "<b><color=green>Y</color> (기존: <color=cyan>Z</color>)</b>";
                    break;
                    case GridLayout.CellSwizzle.ZXY:
                    zAxis = Vector3.right;
                    xAxis = Vector3.forward;
                    xLabel = "<b><color=cyan>Z</color> (기존: <color=red>X</color>)</b>";
                    zLabel = "<b><color=red>X</color> (기존: <color=cyan>Z</color>)</b>";
                    break;
                    case GridLayout.CellSwizzle.ZYX:
                    zAxis = Vector3.right;
                    yAxis = Vector3.forward;
                    xAxis = Vector3.up;
                    xLabel = "<b><color=green>Y</color> (기존: <color=red>X</color>)</b>";
                    yLabel = "<b><color=cyan>Z</color> (기존: <color=green>Y</color>)</b>";
                    zLabel = "<b><color=red>X</color> (기존: <color=cyan>Z</color>)</b>";
                    break;
                }
            }

            // 라벨 스타일 생성 로컬 함수
            GUIStyle CreateLabelStyle(Color textColor)
            {
                return new GUIStyle()
                {
                    richText = true,
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    normal = new GUIStyleState() { textColor = textColor, background = Texture2D.blackTexture }
                };
            }
        }



        ///======================================================================================================================================================



        //? 가이드라인 그리기 



        /// <summary>
        /// 교차 가이드라인을 그리기 위한 공통 메서드입니다.
        /// </summary>
        /// <param name="origin">기준이 되는 원래 위치</param>
        /// <param name="up">위쪽 끝점</param>
        /// <param name="down">아래쪽 끝점</param>
        /// <param name="left">왼쪽 끝점</param>
        /// <param name="right">오른쪽 끝점</param>
        /// <param name="correctionDU">상하 보조선의 간격</param>
        /// <param name="correctionLR">좌우 보조선의 간격</param>
        private static void DrawCrossGuideLinesInternal(Vector3 origin, Vector3 up, Vector3 down, Vector3 left, Vector3 right, Vector3? correctionDU, Vector3? correctionLR)
        {
            // 상하좌우 방향으로 선 그리기
            Gizmos.DrawLine(origin, down);
            Gizmos.DrawLine(origin, up);
            Gizmos.DrawLine(origin, left);
            Gizmos.DrawLine(origin, right);

            // 보조선 그리기 (correction 값이 null이 아닐 때만)
            if (correctionDU.HasValue && correctionLR.HasValue)
            {
                TempGizmoColor_Action(SU_Color.WithAlpha(Gizmos.color, 0.5f), () =>
                {
                    Gizmos.DrawLine(origin + correctionDU.Value, down + correctionDU.Value);
                    Gizmos.DrawLine(origin - correctionDU.Value, down - correctionDU.Value);
                    Gizmos.DrawLine(origin + correctionDU.Value, up + correctionDU.Value);
                    Gizmos.DrawLine(origin - correctionDU.Value, up - correctionDU.Value);
                    Gizmos.DrawLine(origin + correctionLR.Value, left + correctionLR.Value);
                    Gizmos.DrawLine(origin - correctionLR.Value, left - correctionLR.Value);
                    Gizmos.DrawLine(origin + correctionLR.Value, right + correctionLR.Value);
                    Gizmos.DrawLine(origin - correctionLR.Value, right - correctionLR.Value);
                });
            }
        }



        /// <summary>
        /// 중심 위치로부터 주어진 길이만큼 교차 가이드라인을 그립니다.
        /// </summary>
        /// <param name="origin">기준이 되는 원래 위치</param>
        /// <param name="lengthUp">위쪽 가이드라인의 길이</param>
        /// <param name="lengthDown">아래쪽 가이드라인의 길이</param>
        /// <param name="lengthLeft">왼쪽 가이드라인의 길이</param>
        /// <param name="lengthRight">오른쪽 가이드라인의 길이</param>
        /// <param name="swizzle">적용할 Swizzle 설정</param>
        /// <param name="gridUnitOffset">보조선의 간격 (null이면 보조선을 그리지 않음)</param>
        public static void DrawCrossGuideLine(Vector3 origin, float lengthUp, float lengthDown, float lengthLeft, float lengthRight, GridLayout.CellSwizzle swizzle, float? gridUnitOffset)
        {
            // Swizzle 변환 적용
            Vector3 swizzledOrigin = origin.SwizzlesVector(swizzle);

            // 각 방향의 끝점 계산 (Swizzle 변환된 공간에서)
            Vector3 swizzledUp = new Vector3(swizzledOrigin.x, swizzledOrigin.y + lengthUp, swizzledOrigin.z);
            Vector3 swizzledDown = new Vector3(swizzledOrigin.x, swizzledOrigin.y - lengthDown, swizzledOrigin.z);
            Vector3 swizzledLeft = new Vector3(swizzledOrigin.x - lengthLeft, swizzledOrigin.y, swizzledOrigin.z);
            Vector3 swizzledRight = new Vector3(swizzledOrigin.x + lengthRight, swizzledOrigin.y, swizzledOrigin.z);

            // Swizzle 변환을 원래 공간으로 되돌림
            Vector3 up = swizzledUp.SwizzlesVector(swizzle);
            Vector3 down = swizzledDown.SwizzlesVector(swizzle);
            Vector3 left = swizzledLeft.SwizzlesVector(swizzle);
            Vector3 right = swizzledRight.SwizzlesVector(swizzle);

            // 보조선 간격 계산
            Vector3? correctionDU = gridUnitOffset.HasValue ? new Vector3(gridUnitOffset.Value / 2f, 0, 0) : null;
            Vector3? correctionLR = gridUnitOffset.HasValue ? new Vector3(0, gridUnitOffset.Value / 2f, 0) : null;

            if (correctionDU.HasValue)
            {
                correctionDU = correctionDU.Value.SwizzlesVector(swizzle);
            }
            if (correctionLR.HasValue)
            {
                correctionLR = correctionLR.Value.SwizzlesVector(swizzle);
            }

            DrawCrossGuideLinesInternal(origin, up, down, left, right, correctionDU, correctionLR);
        }



        /// <summary>
        /// 중심 위치와 크기를 사용하여 교차 가이드라인을 그립니다.
        /// </summary>
        /// <param name="origin">기준이 되는 원래 위치</param>
        /// <param name="centerPosition">가이드라인의 중심 위치</param>
        /// <param name="dimensions">가이드라인의 크기 (width, height, depth)</param>
        /// <param name="swizzle">적용할 Swizzle 설정</param>
        /// <param name="gridUnitOffset">보조선의 간격 (null이면 보조선을 그리지 않음)</param>
        public static void DrawCrossGuideLine(Vector3 origin, Vector3 centerPosition, Vector3 dimensions, GridLayout.CellSwizzle swizzle, float? gridUnitOffset)
        {
            // Swizzle 변환 적용
            Vector3 swizzledOrigin = origin.SwizzlesVector(swizzle);
            Vector3 swizzledCenterPos = centerPosition.SwizzlesVector(swizzle);

            // Swizzle 변환된 공간에서의 dimensions 계산
            Vector3 swizzledDimensions = dimensions.SwizzlesVector(swizzle);

            // 각 방향의 끝점 계산 (Swizzle 변환된 공간에서)
            Vector3 swizzledDown = new Vector3(swizzledOrigin.x, swizzledCenterPos.y - swizzledDimensions.y / 2, swizzledOrigin.z);
            Vector3 swizzledUp = new Vector3(swizzledOrigin.x, swizzledCenterPos.y + swizzledDimensions.y / 2, swizzledOrigin.z);
            Vector3 swizzledLeft = new Vector3(swizzledCenterPos.x - swizzledDimensions.x / 2, swizzledOrigin.y, swizzledOrigin.z);
            Vector3 swizzledRight = new Vector3(swizzledCenterPos.x + swizzledDimensions.x / 2, swizzledOrigin.y, swizzledOrigin.z);

            // Swizzle 변환을 원래 공간으로 되돌림
            Vector3 down = swizzledDown.SwizzlesVector(swizzle);
            Vector3 up = swizzledUp.SwizzlesVector(swizzle);
            Vector3 left = swizzledLeft.SwizzlesVector(swizzle);
            Vector3 right = swizzledRight.SwizzlesVector(swizzle);

            // 보조선 간격 계산
            Vector3? correctionDU = gridUnitOffset.HasValue ? (Vector3?)new Vector3(gridUnitOffset.Value / 2f, 0, 0) : null;
            Vector3? correctionLR = gridUnitOffset.HasValue ? (Vector3?)new Vector3(0, gridUnitOffset.Value / 2f, 0) : null;

            if (correctionDU.HasValue)
            {
                correctionDU = correctionDU.Value.SwizzlesVector(swizzle);
            }
            if (correctionLR.HasValue)
            {
                correctionLR = correctionLR.Value.SwizzlesVector(swizzle);
            }

            DrawCrossGuideLinesInternal(origin, up, down, left, right, correctionDU, correctionLR);
        }



        /// <summary>
        /// Rect를 사용하여 교차 가이드라인을 그립니다.
        /// </summary>
        /// <param name="origin">기준이 되는 원래 위치</param>
        /// <param name="rect">가이드라인의 중심 위치와 크기를 나타내는 Rect</param>
        /// <param name="swizzle">적용할 Swizzle 설정</param>
        /// <param name="gridUnitOffset">보조선의 간격 (null이면 보조선을 그리지 않음)</param>
        public static void DrawCrossGuideLine(Vector3 origin, Rect rect, GridLayout.CellSwizzle swizzle, float? gridUnitOffset)
        {
            Vector3 centerPosition = new Vector3(rect.center.x, rect.center.y, origin.z);
            Vector3 dimensions = new Vector3(rect.width, rect.height, 0);
            DrawCrossGuideLine(origin, centerPosition, dimensions, swizzle, gridUnitOffset);
        }



        /// <summary>
        /// Vector2 위치와 크기를 사용하여 교차 가이드라인을 그립니다.
        /// </summary>
        /// <param name="origin">기준이 되는 원래 위치</param>
        /// <param name="centerPosition2D">가이드라인의 중심 위치 (2D)</param>
        /// <param name="dimensions2D">가이드라인의 크기 (2D)</param>
        /// <param name="swizzle">적용할 Swizzle 설정</param>
        /// <param name="gridUnitOffset">보조선의 간격 (null이면 보조선을 그리지 않음)</param>
        public static void DrawCrossGuideLine(Vector3 origin, Vector2 centerPosition2D, Vector2 dimensions2D, GridLayout.CellSwizzle swizzle, float? gridUnitOffset)
        {
            Vector3 centerPosition = new Vector3(centerPosition2D.x, centerPosition2D.y, origin.z);
            Vector3 dimensions = new Vector3(dimensions2D.x, dimensions2D.y, 0);
            DrawCrossGuideLine(origin, centerPosition, dimensions, swizzle, gridUnitOffset);
        }



        ///======================================================================================================================================================



        //? 주위 그리드 그리기



        /// <summary>
        /// 지정된 중심 그리드 위치를 기준으로 주위의 그리드를 그립니다.
        /// </summary>
        /// <param name="swizzle">그리드의 스위즐 설정</param>
        /// <param name="centerPosition">중심 그리드의 위치</param>
        /// <param name="centerSize">중심 그리드의 크기</param>
        /// <param name="gridSize">개별 그리드의 크기</param>
        /// <param name="gridCount">
        /// 중심 그리드로부터 그리드를 확장할 범위입니다.<br/>
        /// 이 값은 중심 그리드로부터 좌우, 상하 및 대각선 방향으로 확장될 최대 거리(그리드 수)를 나타냅니다.<br/>
        /// 중심 그리드의 크기와 위치를 기준으로, gridCount에 따라 주변 그리드가 그려지며, 확장된 그리드 범위 안에 포함되지 않는 그리드만 그려집니다.
        /// </param>
        /// <param name="insteadCenterInnerEvent">
        /// 인접 그리드가 중심 그리드 크기 안에 있을경우, 대신 실행되는 이벤트<br/>
        /// (첫번째 Vector3는 좌표, 두번째 Vector3는 크기)
        /// </param>
        public static void DrawGridsNearby(GridLayout.CellSwizzle swizzle, Vector3 centerPosition, Vector3 centerSize, Vector3 gridSize, int gridCount, Action<Vector3, Vector3> insteadCenterInnerEvent = null)
        {
            if (gridCount <= 0) { return; }

            // 중심 그리드 크기의 절대값 계산
            float centerWidth = SU_TF_Vector.GetAxisBeforeSwizzle(centerSize, EAxis.X, swizzle);
            float centerHeight = SU_TF_Vector.GetAxisBeforeSwizzle(centerSize, EAxis.Y, swizzle);
            float gridWidth = SU_TF_Vector.GetAxisBeforeSwizzle(gridSize, EAxis.X, swizzle);
            float gridHeight = SU_TF_Vector.GetAxisBeforeSwizzle(gridSize, EAxis.Y, swizzle);


            // 추가 그리드 범위 계산
            int rangeX = (gridCount - 1) + Mathf.CeilToInt(centerWidth / gridWidth / 2f);
            int rangeY = (gridCount - 1) + Mathf.CeilToInt(centerHeight / gridHeight / 2f);


            if ((centerWidth / gridWidth).IsEven_Truncate()) { rangeX -= 1; }
            if ((centerHeight / gridHeight).IsEven_Truncate()) { rangeY -= 1; }


            Vector3 halfOffset = Vector3.zero;


            // 중심 그리드의 크기와 짝수 여부
            bool isCenterWidthEven = (centerWidth / gridWidth).IsEven_Truncate();
            bool isCenterHeightEven = (centerHeight / gridHeight).IsEven_Truncate();


            // 그리드 범위 설정
            int minX = -rangeX;
            int maxX = rangeX;
            int minY = -rangeY;
            int maxY = rangeY;


            if (isCenterWidthEven)
            {
                halfOffset.x += gridWidth / (2f * gridWidth);
                minX -= 1;
                maxX = rangeX;
            }

            if (isCenterHeightEven)
            {
                halfOffset.y += gridHeight / (2f * gridHeight);
                minY -= 1;
                maxY = rangeY;
            }


            // 그리드 그리기
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (x == 0 && y == 0) { continue; }

                    // 각 그리드의 오프셋을 계산합니다.
                    Vector3 offset = (new Vector3(x, y, 0) + halfOffset).SwizzlesVector(swizzle);
                    offset.Scale(gridSize);

                    // 중심 그리드의 위치를 기준으로 각 그리드의 위치를 계산합니다.
                    Vector3 position = centerPosition + offset;

                    // 중심 그리드 내부에 있는지 확인합니다.
                    if (position.x > centerPosition.x - centerSize.x / 2f && position.x < centerPosition.x + centerSize.x / 2f &&
                        position.y > centerPosition.y - centerSize.y / 2f && position.y < centerPosition.y + centerSize.y / 2f &&
                        position.z > centerPosition.z - centerSize.z / 2f && position.z < centerPosition.z + centerSize.z / 2f)
                    {
                        insteadCenterInnerEvent?.Invoke(position, gridSize);
                        continue;
                    }

                    // 주위 그리드의 크기를 유지하며 그립니다.
                    Gizmos.DrawWireCube(position, gridSize);
                }
            }
        }



        ///======================================================================================================================================================



        //? 기타



        /// <summary>
        /// action에서 실행되는 기즈모 들의 색을 <paramref name="tempColor"/>로 지정해 실행한다<br/>
        /// 액션이 끝나면 원래대로 돌아간다
        /// </summary>
        /// <param name="tempColor"></param>
        /// <param name="action"></param>
        public static void TempGizmoColor_Action(Color tempColor, Action action)
        {
            Color temp = Gizmos.color;
            Gizmos.color = tempColor;

            action?.Invoke();

            Gizmos.color = temp;
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================
}



#endif