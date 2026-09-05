#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Pan.Util;
using System.Linq;



//? Debug의 Draw들을 사용하는 메서드들이 정리되어있는 정도의 코드 (EditorOnly)



namespace Pan.Util.Editors
{
    ///======================================================================================================================================================



    public static class SU_DebugDraw
    {
        /// <summary>
        /// 시작점(origin)에서 주어진 벡터(rayVector) 방향 및 길이로 Debug용 Ray를 그립니다.
        /// </summary>
        /// <param name="origin">Ray의 시작점</param>
        /// <param name="rayVector">
        /// Ray의 방향과 길이를 동시에 나타내는 벡터입니다. <br/>
        /// 예: (1,0,0) * 5는 오른쪽으로 길이 5의 Ray를 의미합니다.
        /// </param>
        /// <param name="color">Ray의 색상</param>
        public static void DrawDebugRay(Vector3 origin, Vector3 rayVector, Color color)
        {
            Debug.DrawRay(origin, rayVector, color);
        }

        /// <summary>
        /// 시작점(startPoint)과 끝점(endPoint)을 잇는 Debug용 선(Line)을 그립니다.
        /// </summary>
        /// <param name="startPoint">선의 시작점</param>
        /// <param name="endPoint">선의 끝점</param>
        /// <param name="color">선의 색상</param>
        public static void DrawDebugLine(Vector3 startPoint, Vector3 endPoint, Color color)
        {
            Debug.DrawLine(startPoint, endPoint, color);
        }



        /// <summary>
        /// 2D XY 평면 기준으로 중심(center)과 크기(boxSize)를 받아
        /// 와이어 박스(외곽선)를 그립니다.
        /// Vector3를 사용하며, Z는 center.z 평면에 고정하여 표시됩니다.
        /// </summary>
        /// <param name="center">박스의 중심 좌표 (Z 평면 고정 기준)</param>
        /// <param name="boxSize">박스의 크기 (너비 X, 높이 Y, 깊이 Z는 사용하지 않음)</param>
        /// <param name="color">박스 외곽선 색상</param>
        public static void DrawDebugBox(Vector3 center, Vector3 boxSize, Color color)
        {
            //. XY 평면, Z는 center.z 고정
            Vector3 half = boxSize * 0.5f;
            Vector3 bottomLeft = new Vector3(center.x - half.x, center.y - half.y, center.z);
            Vector3 bottomRight = new Vector3(center.x + half.x, center.y - half.y, center.z);
            Vector3 topRight = new Vector3(center.x + half.x, center.y + half.y, center.z);
            Vector3 topLeft = new Vector3(center.x - half.x, center.y + half.y, center.z);

            Debug.DrawLine(bottomLeft, bottomRight, color);
            Debug.DrawLine(bottomRight, topRight, color);
            Debug.DrawLine(topRight, topLeft, color);
            Debug.DrawLine(topLeft, bottomLeft, color);
        }

        /// <summary>
        /// 2D XY 평면 기준으로 중심(center), 크기(boxSize), 회전각(angleDegrees)을 받아
        /// 회전된 와이어 박스(외곽선)를 그립니다.
        /// Vector3를 사용하며, 회전은 Z 축(Quaternion.Euler(0,0,angle))을 기준으로 적용됩니다.
        /// </summary>
        /// <param name="center">박스의 중심 좌표</param>
        /// <param name="boxSize">박스의 크기 (너비 X, 높이 Y)</param>
        /// <param name="angleDegrees">Z 축 기준 회전각(도)</param>
        /// <param name="color">박스 외곽선 색상</param>
        public static void DrawDebugRotatedBox(Vector3 center, Vector3 boxSize, float angleDegrees, Color color)
        {
            Quaternion rotation = Quaternion.Euler(0f, 0f, angleDegrees);
            Vector3 half = boxSize * 0.5f;

            //. 로컬 코너를 회전시켜 월드로 배치
            Vector3 bottomLeft = center + rotation * new Vector3(-half.x, -half.y, 0f);
            Vector3 bottomRight = center + rotation * new Vector3(half.x, -half.y, 0f);
            Vector3 topRight = center + rotation * new Vector3(half.x, half.y, 0f);
            Vector3 topLeft = center + rotation * new Vector3(-half.x, half.y, 0f);

            Debug.DrawLine(bottomLeft, bottomRight, color);
            Debug.DrawLine(bottomRight, topRight, color);
            Debug.DrawLine(topRight, topLeft, color);
            Debug.DrawLine(topLeft, bottomLeft, color);
        }

        /// <summary>
        /// 2D XY 평면 기준으로 시작 박스(origin, boxSize)를 그린 뒤,
        /// castDirection/castDistance 만큼 평행 이동한 끝 박스를 그립니다.
        /// 시작/끝 박스의 대응 모서리를 흰색으로 연결하여 캐스트 궤적을 시각화합니다.
        /// </summary>
        /// <param name="origin">시작 박스의 중심 좌표</param>
        /// <param name="boxSize">박스의 크기 (너비 X, 높이 Y)</param>
        /// <param name="angleDegrees">시작 박스의 Z 축 기준 회전각(도)</param>
        /// <param name="castDirection">캐스트 이동 방향(Vector3, XY만 사용)</param>
        /// <param name="castDistance">캐스트 이동 거리</param>
        /// <param name="color">박스 외곽선 색상</param>
        public static void DrawDebugBoxCast(Vector3 origin, Vector3 boxSize, float angleDegrees, Vector3 castDirection, float castDistance, Color color)
        {
            float halfWidth = boxSize.x * 0.5f;
            float halfHeight = boxSize.y * 0.5f;

            Quaternion rotation = Quaternion.AngleAxis(angleDegrees, Vector3.forward);

            //. 시작 박스 4코너 (회전 포함)
            Vector3[] startCorners = new Vector3[4];
            startCorners[0] = origin + rotation * new Vector3(-halfWidth, halfHeight, 0f); // 좌상
            startCorners[1] = origin + rotation * new Vector3(halfWidth, halfHeight, 0f); // 우상
            startCorners[2] = origin + rotation * new Vector3(halfWidth, -halfHeight, 0f); // 우하
            startCorners[3] = origin + rotation * new Vector3(-halfWidth, -halfHeight, 0f); // 좌하

            //. XY 기준 캐스트 오프셋
            Vector3 dirXY = new Vector3(castDirection.x, castDirection.y, 0f);
            Vector3 castOffset = (dirXY.sqrMagnitude > 0f ? dirXY.normalized : Vector3.zero) * castDistance;

            //. 끝 박스 4코너
            Vector3[] endCorners = new Vector3[4];
            for (int i = 0; i < 4; i++)
                endCorners[i] = startCorners[i] + castOffset;

            //. 시작/끝 박스 외곽
            for (int i = 0; i < 4; i++)
            {
                int next = (i + 1) % 4;
                Debug.DrawLine(startCorners[i], startCorners[next], color);
                Debug.DrawLine(endCorners[i], endCorners[next], color);
            }

            //. 대응 모서리 연결선
            for (int i = 0; i < 4; i++)
                Debug.DrawLine(startCorners[i], endCorners[i], Color.white);
        }



        /// <summary>
        /// 중심(center)과 반지름(radius)을 기준으로 Debug용 원(Circle) 외곽선을 그립니다.
        /// </summary>
        /// <param name="center">원 중심 좌표</param>
        /// <param name="radius">원 반지름</param>
        /// <param name="color">원 외곽선의 색상</param>
        public static void DrawDebugCircle(Vector3 center, float radius, Color color)
        {
            int segments = 128;
            float angleStep = 360f / segments;

            // 각 선분을 연결하여 원의 외곽선을 그림
            for (int i = 0; i < segments; i++)
            {
                float angleA = angleStep * i;
                float angleB = angleStep * (i + 1);
                Vector3 pointA = center + Quaternion.Euler(0, 0, angleA) * Vector3.right * radius;
                Vector3 pointB = center + Quaternion.Euler(0, 0, angleB) * Vector3.right * radius;
                Debug.DrawLine(pointA, pointB, color);
            }
        }

        /// <summary>
        /// CircleCast를 시각화하기 위해, 시작 원과 이동 후의 원 외곽선을 그리고, <br/>
        /// 각 원의 대응 점들을 연결하는 선을 그립니다.
        /// </summary>
        /// <param name="origin">시작 원의 중심 좌표</param>
        /// <param name="radius">원 반지름</param>
        /// <param name="castDirection">이동 방향 (정규화되어야 함)</param>
        /// <param name="castDistance">이동 거리</param>
        /// <param name="color">원 외곽선의 색상</param>
        public static void DrawDebugCircleCast(Vector3 origin, float radius, Vector3 castDirection, float castDistance, Color color)
        {
            // 최소 4개의 선분으로 원을 그리도록 설정
            int segments = 4;
            float angleStep = 360f / segments;
            Vector3 endCenter = origin + castDirection.normalized * castDistance;

            // 시작 원과 끝 원의 대응 점들을 계산하여 연결선 그리기 (흰색)
            for (int i = 0; i < segments; i++)
            {
                float baseAngle = angleStep * i;
                // 보정 각도: Vector2.SignedAngle(Vector2.right, castDirection)
                float correction = Vector2.SignedAngle(Vector2.right, castDirection);
                Vector3 startPoint = origin + Quaternion.Euler(0, 0, baseAngle + correction) * Vector3.right * radius;
                Vector3 endPoint = endCenter + Quaternion.Euler(0, 0, baseAngle + correction) * Vector3.right * radius;
                Debug.DrawLine(startPoint, endPoint, Color.white);
            }
            // 시작 원과 끝 원 외곽선 그리기
            DrawDebugCircle(origin, radius, color);
            DrawDebugCircle(endCenter, radius, color);
        }

        /// <summary>
        /// 중심(center)과 반지름(radius), 추가 회전(rotation)을 적용하여 Debug용 원(Circle) 외곽선을 그립니다.
        /// </summary>
        /// <param name="center">원 중심 좌표</param>
        /// <param name="radius">원 반지름</param>
        /// <param name="color">원 외곽선의 색상</param>
        /// <param name="rotation">추가 회전 (Quaternion) 값</param>
        public static void DrawDebugCircleWithRotation(Vector3 center, float radius, Color color, Quaternion rotation)
        {
            int segments = 128;
            float angleStep = 360f / segments;

            for (int i = 0; i < segments; i++)
            {
                float angleA = angleStep * i;
                float angleB = angleStep * (i + 1);
                // 회전 적용: 먼저 개별 각도를 회전한 후, 추가 회전(rotation)을 적용
                Vector3 pointA = center + rotation * (Quaternion.Euler(0, 0, angleA) * Vector3.right * radius);
                Vector3 pointB = center + rotation * (Quaternion.Euler(0, 0, angleB) * Vector3.right * radius);
                Debug.DrawLine(pointA, pointB, color);
            }
        }

        /// <summary>
        /// 두 점(cornerA, cornerB)을 대각으로 갖는 2D XY 평면의 직사각형 외곽선을 그립니다.
        /// Vector3를 사용하며, Z 평면은 cornerA.z 값을 기준으로 고정합니다.
        /// cornerB.z가 다르더라도 그리기는 동일 Z 평면에 표시됩니다.
        /// </summary>
        /// <param name="cornerA">대각 코너 A (Z 평면 기준)</param>
        /// <param name="cornerB">대각 코너 B</param>
        /// <param name="color">사각형 외곽선 색상</param>
        public static void DrawDebugRectangle(Vector3 cornerA, Vector3 cornerB, Color color)
        {
            //! 동일 Z 평면으로 고정 (cornerA.z 사용)
            float z = cornerA.z;

            Vector3 bottomLeft = new Vector3(Mathf.Min(cornerA.x, cornerB.x), Mathf.Min(cornerA.y, cornerB.y), z);
            Vector3 topRight = new Vector3(Mathf.Max(cornerA.x, cornerB.x), Mathf.Max(cornerA.y, cornerB.y), z);
            Vector3 bottomRight = new Vector3(topRight.x, bottomLeft.y, z);
            Vector3 topLeft = new Vector3(bottomLeft.x, topRight.y, z);

            Debug.DrawLine(bottomLeft, bottomRight, color);
            Debug.DrawLine(bottomRight, topRight, color);
            Debug.DrawLine(topRight, topLeft, color);
            Debug.DrawLine(topLeft, bottomLeft, color);
        }



        /// <summary>
        /// 지정된 Bounds(박스 경계)를 기준으로 와이어 큐브(정사각형 박스)를 그립니다. <br/>
        /// 박스의 8개 모서리를 계산하여 앞면, 뒷면, 그리고 양 면을 연결하는 선을 그립니다.
        /// </summary>
        /// <param name="boxBounds">박스의 Bounds (center와 extents를 포함)</param>
        /// <param name="color">박스 외곽선에 사용할 색상</param>
        public static void DrawWireCube(Bounds boxBounds, Color color)
        {
            Vector3 center = boxBounds.center;
            Vector3 extents = boxBounds.extents;

            // 앞면 모서리 좌표 (Z 감소 방향)
            Vector3 frontTopLeft = new Vector3(center.x - extents.x, center.y + extents.y, center.z - extents.z);
            Vector3 frontTopRight = new Vector3(center.x + extents.x, center.y + extents.y, center.z - extents.z);
            Vector3 frontBottomLeft = new Vector3(center.x - extents.x, center.y - extents.y, center.z - extents.z);
            Vector3 frontBottomRight = new Vector3(center.x + extents.x, center.y - extents.y, center.z - extents.z);

            // 뒷면 모서리 좌표 (Z 증가 방향)
            Vector3 backTopLeft = new Vector3(center.x - extents.x, center.y + extents.y, center.z + extents.z);
            Vector3 backTopRight = new Vector3(center.x + extents.x, center.y + extents.y, center.z + extents.z);
            Vector3 backBottomLeft = new Vector3(center.x - extents.x, center.y - extents.y, center.z + extents.z);
            Vector3 backBottomRight = new Vector3(center.x + extents.x, center.y - extents.y, center.z + extents.z);

            // 설정된 색상 적용
            Handles.color = color;

            // 앞면 그리기
            Handles.DrawLine(frontTopLeft, frontTopRight);
            Handles.DrawLine(frontTopRight, frontBottomRight);
            Handles.DrawLine(frontBottomRight, frontBottomLeft);
            Handles.DrawLine(frontBottomLeft, frontTopLeft);

            // 뒷면 그리기
            Handles.DrawLine(backTopLeft, backTopRight);
            Handles.DrawLine(backTopRight, backBottomRight);
            Handles.DrawLine(backBottomRight, backBottomLeft);
            Handles.DrawLine(backBottomLeft, backTopLeft);

            // 앞면과 뒷면 연결 선 그리기
            Handles.DrawLine(frontTopLeft, backTopLeft);
            Handles.DrawLine(frontTopRight, backTopRight);
            Handles.DrawLine(frontBottomRight, backBottomRight);
            Handles.DrawLine(frontBottomLeft, backBottomLeft);
        }

        /// <summary>
        /// 중심 좌표와 크기를 이용하여 와이어 큐브(정사각형 박스)를 그립니다. <br/>
        /// 내부적으로 Bounds를 생성하여 DrawWireCube(Bounds, Color)를 호출합니다.
        /// </summary>
        /// <param name="center">박스의 중심 좌표</param>
        /// <param name="dimensions">박스의 크기 (너비, 높이, 깊이)</param>
        /// <param name="color">박스 외곽선의 색상</param>
        public static void DrawWireCube(Vector3 center, Vector3 dimensions, Color color)
        {
            DrawWireCube(new Bounds(center, dimensions), color);
        }

        /// <summary>
        /// 2D용으로 중심 좌표와 크기를 받아 와이어 큐브(정사각형 박스)를 그립니다. <br/>
        /// X, Y 좌표를 기준으로 Bounds를 생성하며, Z는 0으로 설정됩니다.
        /// </summary>
        /// <param name="centerX">박스 중심의 X 좌표</param>
        /// <param name="centerY">박스 중심의 Y 좌표</param>
        /// <param name="width">박스의 가로 크기</param>
        /// <param name="height">박스의 세로 크기</param>
        /// <param name="color">박스 외곽선의 색상</param>
        public static void DrawWireCube2D(float centerX, float centerY, float width, float height, Color color)
        {
            DrawWireCube(new Bounds(new Vector2(centerX, centerY), new Vector2(width, height)), color);
        }

        /// <summary>
        /// 주어진 Bounds에 오프셋(offset)을 적용한 후 와이어 큐브(정사각형 박스)를 그립니다. <br/>
        /// 오프셋을 통해 박스의 위치를 조정할 수 있습니다.
        /// </summary>
        /// <param name="boxBounds">원본 박스 Bounds</param>
        /// <param name="offset">적용할 위치 오프셋</param>
        /// <param name="color">박스 외곽선의 색상</param>
        public static void DrawWireCubeOffset(Bounds boxBounds, Vector3 offset, Color color)
        {
            DrawWireCube(new Bounds(boxBounds.center + offset, boxBounds.size), color);
        }

        /// <summary>
        /// 지정된 위치(position)에 GUIStyle을 사용하여 텍스트 라벨을 그립니다. <br/>
        /// Handles.Label을 이용하여 에디터 상에 텍스트를 표시합니다.
        /// </summary>
        /// <param name="position">텍스트가 그려질 위치</param>
        /// <param name="text">표시할 텍스트 내용</param>
        /// <param name="style">사용할 GUIStyle</param>
        public static void DrawLabel(Vector3 position, string text, GUIStyle style)
        {
            Handles.Label(position, text, style);
        }

        /// <summary>
        /// 지정된 위치(position)와 색상(color)을 사용하여 기본 GUIStyle (GUIStyle.none)로 텍스트 라벨을 그립니다. <br/>
        /// 텍스트 색상은 style.normal.textColor로 설정됩니다.
        /// </summary>
        /// <param name="position">텍스트가 그려질 위치</param>
        /// <param name="text">표시할 텍스트 내용</param>
        /// <param name="color">텍스트 색상</param>
        public static void DrawLabel(Vector3 position, string text, Color color)
        {
            GUIStyle style = GUIStyle.none;
            style.normal.textColor = color;
            DrawLabel(position, text, style);
        }

        /// <summary>
        /// 지정된 박스(Bounds)에 오프셋(offset)을 적용하고, 박스의 좌측 상단에 텍스트 라벨을 그린 후, <br/>
        /// 해당 박스의 와이어 큐브를 함께 그립니다. <br/>
        /// 텍스트 라벨은 추가 Y축 오프셋(textYOffset)만큼 상단에 배치됩니다.
        /// </summary>
        /// <param name="boxBounds">원본 박스 Bounds</param>
        /// <param name="offset">박스 위치에 적용할 오프셋</param>
        /// <param name="text">표시할 텍스트 내용</param>
        /// <param name="color">텍스트 및 박스 외곽선 색상</param>
        /// <param name="textYOffset">텍스트의 Y축 오프셋 (기본값: 1)</param>
        public static void DrawLabelWithBoxTopLeft(Bounds boxBounds, Vector3 offset, string text, Color color, int textYOffset = 1)
        {
            // boxBounds의 좌측 상단 좌표를 가져오는 확장 메서드 (별도 구현 필요)
            Vector3 labelPosition = boxBounds.GetTopLeft(offset + new Vector3(0, textYOffset, 0));
            DrawLabel(labelPosition, text, color);
            DrawWireCubeOffset(boxBounds, offset, color);
        }



        /// <summary>
        /// 지정된 변환(위치, 회전, 스케일)을 사용하여 와이어 큐브(정사각형 박스)를 그립니다. <br/>
        /// 이 메서드는 TRS 행렬을 생성하여 로컬 공간의 8개 코너를 월드 좌표로 변환한 후, <br/>
        /// 각 코너들을 연결하는 선들을 Debug.DrawLine을 사용하여 그립니다.
        /// </summary>
        /// <param name="position">큐브의 중심 위치</param>
        /// <param name="rotation">큐브에 적용할 회전 값</param>
        /// <param name="scale">큐브의 스케일(크기)</param>
        /// <param name="color">큐브 외곽선에 사용할 색상</param>
        public static void DrawTransformedWireCube(Vector3 position, Quaternion rotation, Vector3 scale, Color color)
        {
            // TRS 행렬 생성: 위치, 회전, 스케일 적용
            Matrix4x4 transformMatrix = Matrix4x4.TRS(position, rotation, scale);

            // 로컬 공간의 정육면체(중심이 원점, 크기가 1인)의 8개 코너 좌표를 계산
            Vector3 p1 = transformMatrix.MultiplyPoint(new Vector3(-0.5f, -0.5f, 0.5f)); // 앞쪽 하단 좌측
            Vector3 p2 = transformMatrix.MultiplyPoint(new Vector3(0.5f, -0.5f, 0.5f)); // 앞쪽 하단 우측
            Vector3 p3 = transformMatrix.MultiplyPoint(new Vector3(0.5f, -0.5f, -0.5f)); // 뒤쪽 하단 우측
            Vector3 p4 = transformMatrix.MultiplyPoint(new Vector3(-0.5f, -0.5f, -0.5f)); // 뒤쪽 하단 좌측

            Vector3 p5 = transformMatrix.MultiplyPoint(new Vector3(-0.5f, 0.5f, 0.5f)); // 앞쪽 상단 좌측
            Vector3 p6 = transformMatrix.MultiplyPoint(new Vector3(0.5f, 0.5f, 0.5f)); // 앞쪽 상단 우측
            Vector3 p7 = transformMatrix.MultiplyPoint(new Vector3(0.5f, 0.5f, -0.5f)); // 뒤쪽 상단 우측
            Vector3 p8 = transformMatrix.MultiplyPoint(new Vector3(-0.5f, 0.5f, -0.5f)); // 뒤쪽 상단 좌측

            // 하단 면 그리기
            Debug.DrawLine(p1, p2, color);
            Debug.DrawLine(p2, p3, color);
            Debug.DrawLine(p3, p4, color);
            Debug.DrawLine(p4, p1, color);

            // 상단 면 그리기
            Debug.DrawLine(p5, p6, color);
            Debug.DrawLine(p6, p7, color);
            Debug.DrawLine(p7, p8, color);
            Debug.DrawLine(p8, p5, color);

            // 상단과 하단 면을 연결하는 선 그리기 (수직 에지)
            Debug.DrawLine(p1, p5, color);
            Debug.DrawLine(p2, p6, color);
            Debug.DrawLine(p3, p7, color);
            Debug.DrawLine(p4, p8, color);
        }

        /// <summary>
        /// 회전 없이 지정된 위치와 스케일만을 사용하여 와이어 큐브(정사각형 박스)를 그립니다. <br/>
        /// 내부적으로 회전 값은 Quaternion.identity로 설정되어 DrawTransformedWireCube를 호출합니다.
        /// </summary>
        /// <param name="position">큐브의 중심 위치</param>
        /// <param name="scale">큐브의 스케일(크기)</param>
        /// <param name="color">큐브 외곽선에 사용할 색상</param>
        public static void DrawTransformedWireCube(Vector3 position, Vector3 scale, Color color)
        {
            DrawTransformedWireCube(position, Quaternion.identity, scale, color);
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================
}



#endif