using UnityEngine;



//? [Transformation] 좌표, 이동, 등등 이 들어있는 정도의 코드



namespace Pan.Util
{
    public static class SU_Bounds
    {
        public static Vector3 GetTopLeft(this Bounds var, Vector3 position)
        {
            return new Vector3
                (
                var.center.x + position.x - var.extents.x,
                var.center.y + position.y + var.extents.y,
                var.center.z + position.z - var.extents.z
                );
        }
    }
}
