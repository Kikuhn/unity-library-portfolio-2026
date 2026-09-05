#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;



//? 에디터에서 입력을 받는 정보를 관리하는 정도의 코드 (EditorOnly)






namespace Pan.Util.Editors
{
    ///======================================================================================================================================================



    public static class SU_Editor_Input
    {
        private static Vector3 lastMouseWorldPosition = Vector3.zero;



        /// <summary>
        /// 현재 에디터 뷰에서 마우스의 월드 좌표를 얻습니다.
        /// GridLayout.CellSwizzle 값을 기준으로 적절한 평면을 설정합니다.
        /// </summary>
        /// <returns>마우스의 월드 좌표</returns>
        public static Vector3 GetEditorMouseWorldPosition(GridLayout.CellSwizzle swizzle, Vector3 center)
        {
            Event e = Event.current;
            if (e == null || SceneView.currentDrawingSceneView == null)
            {
                return lastMouseWorldPosition;
            }

            // Scene 뷰의 카메라를 가져옴
            Camera sceneCamera = SceneView.currentDrawingSceneView.camera;
            Vector3 mousePosition = e.mousePosition;

            // 마우스 좌표를 Scene 뷰 좌표계에 맞게 변환
            mousePosition.y = sceneCamera.pixelHeight - mousePosition.y;

            // 스크린 좌표에서 레이 생성
            Ray ray = sceneCamera.ScreenPointToRay(mousePosition);

            if (SceneView.currentDrawingSceneView.in2DMode)
            {
                // 2D 모드에서는 Z 좌표를 0으로 설정하여 2D 평면에서 작업
                return ray.origin + ray.direction * Mathf.Abs(sceneCamera.transform.position.z / ray.direction.z);
            }
            else
            {
                // 3D 모드에서는 swizzle 값에 따라 평면을 설정하여 Raycast를 사용하여 월드 좌표를 계산
                Plane plane;

                switch (swizzle)
                {
                    case GridLayout.CellSwizzle.XZY:
                    plane = new Plane(Vector3.up, center); // Y=0 평면
                    break;
                    case GridLayout.CellSwizzle.XYZ:
                    case GridLayout.CellSwizzle.YXZ:
                    plane = new Plane(Vector3.forward, center); // Z=0 평면
                    break;
                    case GridLayout.CellSwizzle.YZX:
                    plane = new Plane(Vector3.up, center); // Y=0 평면
                    break;
                    case GridLayout.CellSwizzle.ZXY:
                    case GridLayout.CellSwizzle.ZYX:
                    plane = new Plane(Vector3.right, center); // X=0 평면
                    break;
                    default:
                    plane = new Plane(Vector3.up, center); // 기본적으로 Y=0 평면
                    break;
                }

                if (plane.Raycast(ray, out float enter))
                {
                    return ray.GetPoint(enter);
                }
            }

            return Vector3.zero;
        }



        /// <summary>
        /// 주어진 점이 에디터에서 마우스 위치로부터 주어진 반경 내에 있는지 확인합니다.
        /// </summary>
        /// <param name="point">확인할 월드 좌표</param>
        /// <param name="radius">반경</param>
        /// <param name="swizzle">GridLayout.CellSwizzle 값</param>
        /// <param name="center">중심 좌표</param>
        /// <returns>점이 반경 내에 있으면 true, 아니면 false</returns>
        public static bool IsWithinEditorMouseRadius(Vector3 point, float radius, GridLayout.CellSwizzle swizzle, Vector3? center = null)
        {
            Vector3 mouseWorldPosition = GetEditorMouseWorldPosition(swizzle, center ?? Vector3.zero);
            return Vector3.Distance(mouseWorldPosition, point) < radius;
        }



        /// <summary>
        /// 주어진 점이 에디터 마우스 위치로부터 주어진 2D 크기 영역 안에 있는지 확인합니다.  
        /// <paramref name="size"/> 는 (가로 / 세로) 전체 길이를 의미합니다.
        /// </summary>
        /// <param name="point">확인할 월드 좌표</param>
        /// <param name="size">평면 기준 범위의 전체 크기</param>
        /// <param name="swizzle">GridLayout.CellSwizzle 값</param>
        /// <param name="center">기준 평면의 중심 좌표(옵션)</param>
        /// <returns>범위 안이면 true, 아니면 false</returns>
        public static bool IsWithinEditorMouseBounds(
            Vector3 point,
            Vector2 size,
            GridLayout.CellSwizzle swizzle,
            Vector3? center = null)
        {
            Vector3 mouseWorldPosition = GetEditorMouseWorldPosition(swizzle, center ?? Vector3.zero);

            Vector2 diff = GetPlanarDifference(mouseWorldPosition, point, swizzle);

            Vector2 half = size * 0.5f; //. 절반 크기로 변환

            //! 두 축 절대값이 모두 절반 길이 이하인지 검사
            return Mathf.Abs(diff.x) <= half.x && Mathf.Abs(diff.y) <= half.y;
        }



        /// <summary>
        /// swizzle 에 따라 두 점의 평면 차이를 <see cref="Vector2"/> 로 변환합니다.
        /// </summary>
        private static Vector2 GetPlanarDifference(
            Vector3 a,
            Vector3 b,
            GridLayout.CellSwizzle swizzle)
        {
            Vector3 d = a - b; //. 월드상의 차이

            switch (swizzle)
            {
                case GridLayout.CellSwizzle.XZY:
                case GridLayout.CellSwizzle.YZX:
                //. XZ 평면 → (x, z)
                return new Vector2(d.x, d.z);

                case GridLayout.CellSwizzle.XYZ:
                case GridLayout.CellSwizzle.YXZ:
                //. XY 평면 → (x, y)
                return new Vector2(d.x, d.y);

                case GridLayout.CellSwizzle.ZXY:
                case GridLayout.CellSwizzle.ZYX:
                //. YZ 평면 → (y, z)
                return new Vector2(d.y, d.z);

                default:
                //? 예외: 기본적으로 XZ 평면
                return new Vector2(d.x, d.z);
            }
        }
    }

    ///======================================================================================================================================================
}





#endif