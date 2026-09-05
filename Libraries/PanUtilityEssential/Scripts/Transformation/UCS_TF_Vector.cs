using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;



//? [Transformation] 주로 "벡터" 들이 들어있는 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    public static class SU_TF_Vector
    {
        ///======================================================================================================================================================



        //? 버스트 최적화 타입 변환



        #region 버스트 최적화 타입 변환



        /// <summary>
        /// Vector2를 float2로 변환합니다.
        /// </summary>
        public static float2 ToFloat2(this Vector2 v) => new float2(v.x, v.y);

        /// <summary>
        /// Vector3를 float3로 변환합니다.
        /// </summary>
        public static float3 ToFloat3(this Vector3 v) => new float3(v.x, v.y, v.z);

        /// <summary>
        /// Vector4를 float4로 변환합니다.
        /// </summary>
        public static float4 ToFloat4(this Vector4 v) => new float4(v.x, v.y, v.z, v.w);

        /// <summary>
        /// Quaternion을 quaternion으로 변환합니다.
        /// </summary>
        public static quaternion ToQuaternion(this Quaternion q) => new quaternion(q.x, q.y, q.z, q.w);

        /// <summary>
        /// Color를 float4로 변환합니다.
        /// </summary>
        public static float4 ToFloat4(this Color c) => new float4(c.r, c.g, c.b, c.a);

        /// <summary>
        /// Rect를 float4(x, y, width, height)로 변환합니다.
        /// </summary>
        public static float4 ToFloat4(this Rect r) => new float4(r.x, r.y, r.width, r.height);

        /// <summary>
        /// float2를 Vector2로 변환합니다.
        /// </summary>
        public static Vector2 ToVector2(this float2 f) => new Vector2(f.x, f.y);

        /// <summary>
        /// float3를 Vector3로 변환합니다.
        /// </summary>
        public static Vector3 ToVector3(this float3 f) => new Vector3(f.x, f.y, f.z);

        /// <summary>
        /// float4를 Vector4로 변환합니다.
        /// </summary>
        public static Vector4 ToVector4(this float4 f) => new Vector4(f.x, f.y, f.z, f.w);

        /// <summary>
        /// quaternion을 Quaternion으로 변환합니다.
        /// </summary>
        public static Quaternion ToQuaternion(this quaternion q) => new Quaternion(q.value.x, q.value.y, q.value.z, q.value.w);

        /// <summary>
        /// float4를 Color로 변환합니다.
        /// </summary>
        public static Color ToColor(this float4 f) => new Color(f.x, f.y, f.z, f.w);

        /// <summary>
        /// float4(x, y, width, height)를 Rect로 변환합니다.
        /// </summary>
        public static Rect ToRect(this float4 f) => new Rect(f.x, f.y, f.z, f.w);

        /// <summary>
        /// int2를 Vector2Int로 변환합니다.
        /// </summary>
        public static Vector2Int ToVector2Int(this int2 i) => new Vector2Int(i.x, i.y);

        /// <summary>
        /// int3를 Vector3Int로 변환합니다.
        /// </summary>
        public static Vector3Int ToVector3Int(this int3 i) => new Vector3Int(i.x, i.y, i.z);

        /// <summary>
        /// Vector2Int를 int2로 변환합니다.
        /// </summary>
        public static int2 ToInt2(this Vector2Int v) => new int2(v.x, v.y);

        /// <summary>
        /// Vector3Int를 int3로 변환합니다.
        /// </summary>
        public static int3 ToInt3(this Vector3Int v) => new int3(v.x, v.y, v.z);



        #endregion



        ///======================================================================================================================================================



        //? 벡터 오프셋 연산



        /// <summary>
        /// 지정된 기준점과 크기를 기반으로 중심 오프셋을 계산합니다.
        /// </summary>
        /// <param name="centerStandard">객체의 정렬 기준점입니다.</param>
        /// <param name="size">객체의 크기입니다.</param>
        /// <param name="depthAxis">깊이 축을 지정합니다.</param>
        /// <returns>계산된 중심 오프셋을 반환합니다.</returns>
        public static Vector3 CalculateCenterOffset(ECenterStandard centerStandard, Vector3 size, EAxis depthAxis)
        {
            Vector3 resultOffset = Vector3.zero;


            //? 각 축의 절반 크기 계산
            float sizeHalf_X = size.x / 2f;
            float sizeHalf_Y = size.y / 2f;


            //? 중심 기준점에 따라 오프셋을 조정
            switch (centerStandard)
            {
                case ECenterStandard.UpperLeft: resultOffset = new Vector3(-sizeHalf_X, sizeHalf_Y, 0); break;
                case ECenterStandard.UpperCenter: resultOffset = new Vector3(0, sizeHalf_Y, 0); break;
                case ECenterStandard.UpperRight: resultOffset = new Vector3(sizeHalf_X, sizeHalf_Y, 0); break;

                case ECenterStandard.MiddleLeft: resultOffset = new Vector3(-sizeHalf_X, 0, 0); break;
                case ECenterStandard.MiddleCenter: break;
                case ECenterStandard.MiddleRight: resultOffset = new Vector3(sizeHalf_X, 0, 0); break;

                case ECenterStandard.LowerLeft: resultOffset = new Vector3(-sizeHalf_X, -sizeHalf_Y, 0); break;
                case ECenterStandard.LowerCenter: resultOffset = new Vector3(0, -sizeHalf_Y, 0); break;
                case ECenterStandard.LowerRight: resultOffset = new Vector3(sizeHalf_X, -sizeHalf_Y, 0); break;
            }
            //! 250605, Left Right 뒤집음


            //? 깊이 축이 X, Y, Z 중 어디인지에 따라 오프셋을 변환
            switch (depthAxis)
            {
                case EAxis.X: resultOffset = new Vector3(resultOffset.z, resultOffset.y, resultOffset.x); break;
                case EAxis.Y: resultOffset = new Vector3(resultOffset.x, resultOffset.z, resultOffset.y); break;
                case EAxis.Z: break;
            }


            return resultOffset;
        }



        //? 벡터 오프셋 연산 (버스트 최적화)



        /// <summary>
        /// 지정된 기준점과 크기를 기반으로 중심 오프셋을 계산합니다.
        /// </summary>
        /// <param name="centerStandard">객체의 정렬 기준점입니다.</param>
        /// <param name="size">객체의 크기입니다.</param>
        /// <param name="depthAxis">깊이 축을 지정합니다.</param>
        /// <returns>계산된 중심 오프셋을 반환합니다.</returns>
        public static float3 CalculateCenterOffset(ECenterStandard centerStandard, float3 size, EAxis depthAxis)
        {
            float3 resultOffset = float3.zero;


            //? 각 축의 절반 크기 계산
            float sizeHalf_X = size.x / 2f;
            float sizeHalf_Y = size.y / 2f;


            //? 중심 기준점에 따라 오프셋을 조정
            switch (centerStandard)
            {
                case ECenterStandard.UpperLeft: resultOffset = new float3(sizeHalf_X, sizeHalf_Y, 0); break;
                case ECenterStandard.UpperCenter: resultOffset = new float3(0, sizeHalf_Y, 0); break;
                case ECenterStandard.UpperRight: resultOffset = new float3(-sizeHalf_X, sizeHalf_Y, 0); break;
                case ECenterStandard.MiddleLeft: resultOffset = new float3(sizeHalf_X, 0, 0); break;
                case ECenterStandard.MiddleCenter: break;
                case ECenterStandard.MiddleRight: resultOffset = new float3(-sizeHalf_X, 0, 0); break;
                case ECenterStandard.LowerLeft: resultOffset = new float3(sizeHalf_X, -sizeHalf_Y, 0); break;
                case ECenterStandard.LowerCenter: resultOffset = new float3(0, -sizeHalf_Y, 0); break;
                case ECenterStandard.LowerRight: resultOffset = new float3(-sizeHalf_X, -sizeHalf_Y, 0); break;
            }


            //? 깊이 축이 X, Y, Z 중 어디인지에 따라 오프셋을 변환
            switch (depthAxis)
            {
                case EAxis.X: resultOffset = new float3(resultOffset.z, resultOffset.y, resultOffset.x); break;
                case EAxis.Y: resultOffset = new float3(resultOffset.x, resultOffset.z, resultOffset.y); break;
                case EAxis.Z: break;
            }


            return resultOffset;
        }



        ///======================================================================================================================================================



        #region Swizzle



        //? Swizzle에 따른 벡터 변환



        /// <summary>
        /// 받아온 Swizzle에 따라 Vector3의 축을 변환합니다.
        /// </summary>
        /// <param name="vector">변환할 원래 Vector3 위치입니다.</param>
        /// <param name="swizzle">적용할 CellSwizzle 설정입니다.</param>
        /// <param name="reverseZWhenXYZ">활성화시 XYZ 또는 YXZ일 경우, Z축에 -1을 곱한다</param>
        /// <returns>변환된 Vector3 위치입니다.</returns>
        public static Vector3 SwizzlesVector(this Vector3 vector, GridLayout.CellSwizzle swizzle, bool reverseZWhenXYZ = false)
        {
            switch (swizzle)
            {
                case GridLayout.CellSwizzle.XYZ: return !reverseZWhenXYZ ? vector : new Vector3(vector.x, vector.y, -vector.z);
                case GridLayout.CellSwizzle.XZY: return new Vector3(vector.x, vector.z, vector.y);
                case GridLayout.CellSwizzle.YXZ: return !reverseZWhenXYZ ? vector : new Vector3(vector.y, vector.x, -vector.z);
                case GridLayout.CellSwizzle.YZX: return new Vector3(vector.y, vector.z, vector.x);
                case GridLayout.CellSwizzle.ZXY: return new Vector3(vector.z, vector.x, vector.y);
                case GridLayout.CellSwizzle.ZYX: return new Vector3(vector.z, vector.y, vector.x);
                default: return vector;
            }
        }



        /// <summary>
        /// 받아온 Swizzle에 따라 Vector2의 축을 변환합니다.
        /// </summary>
        /// <param name="vector">변환할 원래 Vector2 위치입니다.</param>
        /// <param name="swizzle">적용할 CellSwizzle 설정입니다.</param>
        /// <param name="reverseZWhenXYZ">활성화시 XYZ 또는 YXZ일 경우, Z축에 -1을 곱한다</param>
        /// <returns>변환된 Vector2 위치입니다.</returns>
        public static Vector2 SwizzlesVector2(this Vector2 vector, GridLayout.CellSwizzle swizzle, bool reverseZWhenXYZ = false)
        {
            Vector3 result = SwizzlesVector(vector, swizzle, reverseZWhenXYZ);
            return result;
        }



        /// <summary>
        /// 받아온 Swizzle에 따라 Vector2의 축을 변환하여 Vector3를 반환합니다
        /// </summary>
        /// <param name="vector">변환할 원래 Vector2 위치입니다.</param>
        /// <param name="swizzle">적용할 CellSwizzle 설정입니다.</param>
        /// <param name="reverseZWhenXYZ">활성화시 XYZ 또는 YXZ일 경우, Z축에 -1을 곱한다</param>
        /// <returns>변환된 Vector3 위치입니다.</returns>
        public static Vector3 SwizzlesVector2To3(this Vector2 vector, GridLayout.CellSwizzle swizzle, bool reverseZWhenXYZ = false)
        {
            return SwizzlesVector(vector, swizzle, reverseZWhenXYZ);
        }



        /// <summary>
        /// 받아온 Swizzle에 따라 Vector3의 축을 변환합니다.
        /// </summary>
        /// <param name="vector">변환할 원래 Vector3 위치입니다.</param>
        /// <param name="swizzle">적용할 CellSwizzle 설정입니다.</param>
        /// <param name="reverseZWhenXYZ">활성화시 XYZ 또는 YXZ일 경우, Z축에 -1을 곱한다</param>
        /// <returns>변환된 Vector3 위치입니다.</returns>
        public static void SwizzlesVectorRef(this ref Vector3 vector, GridLayout.CellSwizzle swizzle, bool reverseZWhenXYZ = false)
        {
            vector = vector.SwizzlesVector(swizzle, reverseZWhenXYZ);
        }



        /// <summary>
        /// 받아온 Swizzle에 따라 Vector2의 축을 변환합니다.
        /// </summary>
        /// <param name="vector">변환할 원래 Vector2 위치입니다.</param>
        /// <param name="swizzle">적용할 CellSwizzle 설정입니다.</param>
        /// <param name="reverseZWhenXYZ">활성화시 XYZ 또는 YXZ일 경우, Z축에 -1을 곱한다</param>
        /// <returns>변환된 Vector3 위치입니다.</returns>
        public static void SwizzlesVector2Ref(this ref Vector2 vector, GridLayout.CellSwizzle swizzle, bool reverseZWhenXYZ = false)
        {
            Vector3 result = vector;
            SwizzlesVectorRef(ref result, swizzle, reverseZWhenXYZ);
            vector = result;
        }



        /// <summary>
        /// <see cref="GridLayout.CellSwizzle"/> 설정에 따라 Vector3Int의 축을 변환합니다.
        /// </summary>
        /// <param name="vector">변환할 원래 Vector3Int 위치입니다.</param>
        /// <param name="swizzle">적용할 CellSwizzle 설정입니다.</param>
        /// <param name="reverseZWhenXYZ">활성화시 XYZ 또는 YXZ일 경우, Z축에 -1을 곱한다</param>
        /// <returns>변환된 Vector3Int 위치입니다.</returns>
        /// <returns>변환된 Vector3Int 위치입니다.</returns>
        public static Vector3Int SwizzlesVectorInt(this Vector3Int vector, GridLayout.CellSwizzle swizzle, bool reverseZWhenXYZ = false)
        {
            switch (swizzle)
            {
                case GridLayout.CellSwizzle.XYZ: return !reverseZWhenXYZ ? vector : new Vector3Int(vector.x, vector.y, -vector.z);
                case GridLayout.CellSwizzle.XZY: return new Vector3Int(vector.x, vector.z, vector.y);
                case GridLayout.CellSwizzle.YXZ: return !reverseZWhenXYZ ? vector : new Vector3Int(vector.y, vector.x, -vector.z);
                case GridLayout.CellSwizzle.YZX: return new Vector3Int(vector.y, vector.z, vector.x);
                case GridLayout.CellSwizzle.ZXY: return new Vector3Int(vector.z, vector.x, vector.y);
                case GridLayout.CellSwizzle.ZYX: return new Vector3Int(vector.z, vector.y, vector.x);
                default: return vector;
            }
        }



        /// <summary>
        /// <see cref="GridLayout.CellSwizzle"/> 설정에 따라 Vector3Int의 축을 변환합니다.
        /// </summary>
        /// <param name="vector">변환할 원래 Vector2Int 위치입니다.</param>
        /// <param name="swizzle">적용할 CellSwizzle 설정입니다.</param>
        /// <param name="reverseZWhenXYZ">활성화시 XYZ 또는 YXZ일 경우, Z축에 -1을 곱한다</param>
        /// <returns>변환된 Vector2Int 위치입니다.</returns>
        public static Vector2Int SwizzlesVector2Int(this Vector2Int vector, GridLayout.CellSwizzle swizzle, bool reverseZWhenXYZ = false)
        {
            Vector3Int result = SwizzlesVectorInt(new Vector3Int(vector.x, vector.y), swizzle, reverseZWhenXYZ);
            return new Vector2Int(result.x, result.y);
        }



        /// <summary>
        /// 받아온 Swizzle에 따라 Vector2Int의 축을 변환하여 Vector3Int를 반환합니다
        /// </summary>
        /// <param name="vector">변환할 원래 Vector2Int 위치입니다.</param>
        /// <param name="swizzle">적용할 CellSwizzle 설정입니다.</param>
        /// <param name="reverseZWhenXYZ">활성화시 XYZ 또는 YXZ일 경우, Z축에 -1을 곱한다</param>
        /// <returns>변환된 Vector3 위치입니다.</returns>
        public static Vector3Int SwizzlesVectorInt2To3(this Vector2Int vector, GridLayout.CellSwizzle swizzle, bool reverseZWhenXYZ = false)
        {
            return SwizzlesVectorInt((Vector3Int)vector, swizzle, reverseZWhenXYZ);
        }



        /// <summary>
        /// <see cref="GridLayout.CellSwizzle"/> 설정에 따라 Vector3Int의 축을 변환합니다.
        /// </summary>
        /// <param name="vector">변환할 원래 Vector3Int 위치입니다.</param>
        /// <param name="swizzle">적용할 CellSwizzle 설정입니다.</param>
        /// <param name="reverseZWhenXYZ">활성화시 XYZ 또는 YXZ일 경우, Z축에 -1을 곱한다</param>
        /// <returns>변환된 Vector3Int 위치입니다.</returns>
        /// <returns>변환된 Vector3Int 위치입니다.</returns>
        public static void SwizzlesVectorIntRef(this ref Vector3Int vector, GridLayout.CellSwizzle swizzle, bool reverseZWhenXYZ = false)
        {
            vector = vector.SwizzlesVectorInt(swizzle, reverseZWhenXYZ);
        }



        /// <summary>
        /// <see cref="GridLayout.CellSwizzle"/> 설정에 따라 Vector3Int의 축을 변환합니다.
        /// </summary>
        /// <param name="vector">변환할 원래 Vector2Int 위치입니다.</param>
        /// <param name="swizzle">적용할 CellSwizzle 설정입니다.</param>
        /// <param name="reverseZWhenXYZ">활성화시 XYZ 또는 YXZ일 경우, Z축에 -1을 곱한다</param>
        /// <returns>변환된 Vector2Int 위치입니다.</returns>
        public static void SwizzlesVector2IntRef(this ref Vector2Int vector, GridLayout.CellSwizzle swizzle, bool reverseZWhenXYZ = false)
        {
            Vector3Int result = new Vector3Int(vector.x, vector.y);
            SwizzlesVectorIntRef(ref result, swizzle, reverseZWhenXYZ);
            vector = new Vector2Int(result.x, result.y);
        }



        /// <summary>
        /// 객체의 크기와 중심 기준에 따라 오프셋을 계산하고, 지정된 <see cref="GridLayout.CellSwizzle"/>에 따라 이 오프셋을 조정합니다.
        /// </summary>
        /// <param name="isDepthY">높이 축이 Y축인지 Z인지를 지정합니다. true이면 Y축이 높이로 간주됩니다.</param>
        /// <param name="centerStandard">객체의 중심 기준점을 결정하는 열거형입니다.</param>
        /// <param name="size">객체의 크기(Vector3)입니다.</param>
        /// <param name="swizzle">오프셋의 축을 조정할 CellSwizzle입니다.</param>
        /// <param name="reverseYorZWhenXYZorYXZ">XYZ 또는 YXZ Swizzle을 사용할 때 Y 또는 Z 축을 반전할지의 여부입니다.</param>
        /// <returns>조정된 오프셋(Vector3)을 반환합니다.</returns>
        public static Vector3 Swizzles_GetOffset(this Vector3 size, ECenterStandard centerStandard, GridLayout.CellSwizzle swizzle, bool reverseYorZWhenXYZorYXZ, EAxis? depthAxis = null)
        {
            //? 받아온 벡터 크기와 깊이 축이 Y인지 Z인지 여부를 사용해, ECenterStandard에 맞는 위치의 오프셋을 얻는다
            Vector3 resultOffset = CalculateCenterOffset(centerStandard, size, depthAxis ?? swizzle.GetDepthAxis());

            //? Swizzle에 맞게 변환후 반환한다
            resultOffset = SwizzlesVector(resultOffset, swizzle, reverseYorZWhenXYZorYXZ);

            return resultOffset;
        }



        //? Swizzle로 특정 축만 얻기/수정



        /// <summary>
        /// GridLayout의 셀 스윙즈 방식에 따른 축 인덱스를 반환합니다.
        /// </summary>
        /// <param name="swizzle">GridLayout 셀의 스윙즈 방식</param>
        /// <param name="axisIndex">원래 축 인덱스</param>
        /// <returns>변환된 축 인덱스</returns>
        private static int GetSwizzledIndex(GridLayout.CellSwizzle swizzle, int axisIndex)
        {
            switch (swizzle)
            {
                case GridLayout.CellSwizzle.XYZ:
                return axisIndex; // 그대로 유지
                case GridLayout.CellSwizzle.XZY:
                return (axisIndex == 0) ? 0 : (axisIndex == 1) ? 2 : 1; // X -> X, Y -> Z, Z -> Y
                case GridLayout.CellSwizzle.YXZ:
                return (axisIndex == 0) ? 1 : (axisIndex == 1) ? 0 : 2; // X -> Y, Y -> X, Z -> Z
                case GridLayout.CellSwizzle.YZX:
                return (axisIndex == 0) ? 2 : (axisIndex == 1) ? 0 : 1; // X -> Z, Y -> X, Z -> Y
                case GridLayout.CellSwizzle.ZXY:
                return (axisIndex == 0) ? 1 : (axisIndex == 1) ? 2 : 0; // X -> Y, Y -> Z, Z -> X
                case GridLayout.CellSwizzle.ZYX:
                return (axisIndex == 0) ? 2 : (axisIndex == 1) ? 1 : 0; // X -> Z, Y -> Y, Z -> X
                default:
                throw new System.ArgumentException("지원되지 않는 swizzle 값입니다");
            }
        }



        /// <summary>
        /// GridLayout의 셀 스윙즈 방식에 따라 벡터의 특정 축 값을 변환하여 반환합니다.
        /// </summary>
        /// <param name="vector">변환할 벡터</param>
        /// <param name="axis">변환할 축</param>
        /// <param name="swizzle">GridLayout 셀의 스윙즈 방식</param>
        /// <returns>변환된 축 값</returns>
        public static float GetAxisBeforeSwizzle(this Vector3 vector, EAxis axis, GridLayout.CellSwizzle swizzle)
        {
            int axisIndex = (int)axis;
            int swizzledIndex = GetSwizzledIndex(swizzle, axisIndex);

            // 실제로 변환할 인덱스에 따라 벡터의 값을 반환
            if (swizzledIndex == 0)
            {
                return vector.x;
            }
            else if (swizzledIndex == 1)
            {
                return vector.y;
            }
            else // swizzledIndex == 2
            {
                return vector.z;
            }
        }



        /// <summary>
        /// GridLayout의 셀 스윙즈 방식에 따라 벡터의 특정 축 값을 변환하여 반환합니다.
        /// </summary>
        /// <param name="vector">변환할 벡터</param>
        /// <param name="axis">변환할 축</param>
        /// <param name="swizzle">GridLayout 셀의 스윙즈 방식</param>
        /// <returns>변환된 축 값</returns>
        public static int GetAxisBeforeSwizzle(this Vector3Int vector, EAxis axis, GridLayout.CellSwizzle swizzle)
        {
            int axisIndex = (int)axis;
            int swizzledIndex = GetSwizzledIndex(swizzle, axisIndex);

            // 실제로 변환할 인덱스에 따라 벡터의 값을 반환
            if (swizzledIndex == 0)
            {
                return vector.x;
            }
            else if (swizzledIndex == 1)
            {
                return vector.y;
            }
            else // swizzledIndex == 2
            {
                return vector.z;
            }
        }



        /// <summary>
        /// GridLayout의 Swizzle에 따라 벡터의 특정 축 값을 수정합니다.<br/>
        /// Swizzle 되기 전의 값을 수정합니다
        /// </summary>
        /// <param name="vector">수정할 벡터</param>
        /// <param name="axis">수정할 축</param>
        /// <param name="newAxisValue">새로 설정할 값</param>
        /// <param name="swizzle">GridLayout 셀의 스윙즈 방식</param>
        public static void ModifyVectorBySwizzle(this ref Vector3 vector, EAxis axis, float newAxisValue, GridLayout.CellSwizzle swizzle)
        {
            int axisIndex = (int)axis;
            int swizzledIndex = GetSwizzledIndex(swizzle, axisIndex);

            // 실제로 수정할 인덱스에 따라 벡터의 값을 수정
            if (swizzledIndex == 0)
            {
                vector.x = newAxisValue;
            }
            else if (swizzledIndex == 1)
            {
                vector.y = newAxisValue;
            }
            else // swizzledIndex == 2
            {
                vector.z = newAxisValue;
            }
        }



        //? Swizzle 쿼터니언



        /// <summary>
        /// <see cref="GridLayout.CellSwizzle"/> 설정에 따라 Vector3의 축을 Quaternion으로 변환합니다.
        /// </summary>
        /// <param name="vector">변환할 원래 Vector3 위치입니다.</param>
        /// <param name="swizzle">적용할 CellSwizzle 설정입니다.</param>
        public static Quaternion SwizzlesQuaternion(this Vector3 vector, GridLayout.CellSwizzle swizzle)
        {
            Quaternion result = new Quaternion();

            switch (swizzle)
            {
                case GridLayout.CellSwizzle.XYZ:

                result = Quaternion.Euler(vector.x, vector.y, vector.z);

                break;

                case GridLayout.CellSwizzle.XZY:

                result = Quaternion.Euler(vector.x, vector.z, vector.y);

                break;

                case GridLayout.CellSwizzle.YXZ:

                result = Quaternion.Euler(vector.y, vector.x, vector.z);

                break;

                case GridLayout.CellSwizzle.YZX:

                result = Quaternion.Euler(vector.y, vector.z, vector.x);

                break;

                case GridLayout.CellSwizzle.ZXY:

                result = Quaternion.Euler(vector.z, vector.x, vector.y);

                break;

                case GridLayout.CellSwizzle.ZYX:

                result = Quaternion.Euler(vector.z, vector.y, vector.x);

                break;
            }

            return result;
        }



        //? Swizzle 축 얻기



        /// <summary>
        /// <paramref name="swizzle"/>에 따라 너비 (xyz 기준 <b>X</b>) 축을 반환합니다.
        /// </summary>
        public static EAxis GetWidthAxis(this GridLayout.CellSwizzle swizzle)
        {
            switch (swizzle)
            {
                case GridLayout.CellSwizzle.XYZ:
                case GridLayout.CellSwizzle.XZY:
                return EAxis.X;
                case GridLayout.CellSwizzle.YXZ:
                case GridLayout.CellSwizzle.YZX:
                return EAxis.Y;
                case GridLayout.CellSwizzle.ZXY:
                case GridLayout.CellSwizzle.ZYX:
                return EAxis.Z;
                default: throw new Exception("");
            }
        }



        /// <summary>
        /// <paramref name="swizzle"/>에 따라 높이 (xyz 기준 <b>Y</b>) 축을 반환합니다.
        /// </summary>
        public static EAxis GetHeightAxis(this GridLayout.CellSwizzle swizzle)
        {
            switch (swizzle)
            {
                case GridLayout.CellSwizzle.ZXY:
                case GridLayout.CellSwizzle.YXZ:
                return EAxis.X;
                case GridLayout.CellSwizzle.XYZ:
                case GridLayout.CellSwizzle.ZYX:
                return EAxis.Y;
                case GridLayout.CellSwizzle.XZY:
                case GridLayout.CellSwizzle.YZX:
                return EAxis.Z;
                default: throw new Exception("");
            }
        }



        /// <summary>
        /// <paramref name="swizzle"/>에 따라 깊이  (xyz 기준 <b>Z</b>) 축을 반환합니다.
        /// </summary>
        public static EAxis GetDepthAxis(this GridLayout.CellSwizzle swizzle)
        {
            switch (swizzle)
            {
                case GridLayout.CellSwizzle.YZX:
                case GridLayout.CellSwizzle.ZYX:
                return EAxis.X;
                case GridLayout.CellSwizzle.XZY:
                case GridLayout.CellSwizzle.ZXY:
                return EAxis.Y;
                case GridLayout.CellSwizzle.XYZ:
                case GridLayout.CellSwizzle.YXZ:
                return EAxis.Z;
                default: throw new Exception("");
            }
        }



        #endregion



        #region Swizzle (버스트 최적화)



        //? Swizzle에 따른 float3 변환



        /// <summary>
        /// 받아온 Swizzle에 따라 float3의 축을 변환합니다.
        /// </summary>
        /// <param name="vector">변환할 원래 float3 위치입니다.</param>
        /// <param name="swizzle">적용할 CellSwizzle 설정입니다.</param>
        /// <param name="reverseZWhenXYZ">활성화시 XYZ 또는 YXZ일 경우, Z축에 -1을 곱합니다.</param>
        /// <returns>변환된 float3 위치입니다.</returns>
        public static float3 SwizzlesFloat3(this float3 vector, GridLayout.CellSwizzle swizzle, bool reverseZWhenXYZ = false)
        {
            switch (swizzle)
            {
                case GridLayout.CellSwizzle.XYZ:
                return reverseZWhenXYZ
                    ? new float3(vector.x, vector.y, -vector.z)
                    : vector;

                case GridLayout.CellSwizzle.XZY:
                return new float3(vector.x, vector.z, vector.y);

                case GridLayout.CellSwizzle.YXZ:
                return reverseZWhenXYZ
                    ? new float3(vector.y, vector.x, -vector.z)
                    : new float3(vector.y, vector.x, vector.z);

                case GridLayout.CellSwizzle.YZX:
                return new float3(vector.y, vector.z, vector.x);

                case GridLayout.CellSwizzle.ZXY:
                return new float3(vector.z, vector.x, vector.y);

                case GridLayout.CellSwizzle.ZYX:
                return new float3(vector.z, vector.y, vector.x);

                default:
                return vector;
            }
        }

        /// <summary>
        /// 받아온 Swizzle에 따라 float2의 축을 변환합니다.
        /// </summary>
        /// <param name="vector">변환할 원래 float2 위치입니다.</param>
        /// <param name="swizzle">적용할 CellSwizzle 설정입니다.</param>
        /// <param name="reverseZWhenXYZ">XYZ 또는 YXZ Swizzle 시, Z축에 -1을 곱할지 여부입니다.</param>
        /// <returns>변환된 float2 위치입니다.</returns>
        public static float2 SwizzlesFloat2(this float2 vector, GridLayout.CellSwizzle swizzle, bool reverseZWhenXYZ = false)
        {
            // float2는 z가 없으므로, 내부적으로 float3로 변환하여 로직을 재사용
            float3 converted = new float3(vector.x, vector.y, 0f);
            float3 result = converted.SwizzlesFloat3(swizzle, reverseZWhenXYZ);

            // Swizzle 결과 중 x,y만을 반환
            return new float2(result.x, result.y);
        }

        /// <summary>
        /// 받아온 Swizzle에 따라 float2의 축을 변환하여 float3를 반환합니다.
        /// </summary>
        /// <param name="vector">변환할 원래 float2 위치입니다.</param>
        /// <param name="swizzle">적용할 CellSwizzle 설정입니다.</param>
        /// <param name="reverseZWhenXYZ">XYZ 또는 YXZ Swizzle 시, Z축에 -1을 곱할지 여부입니다.</param>
        /// <returns>Swizzle 적용 후의 float3 위치입니다.</returns>
        public static float3 SwizzlesFloat2To3(this float2 vector, GridLayout.CellSwizzle swizzle, bool reverseZWhenXYZ = false)
        {
            float3 converted = new float3(vector.x, vector.y, 0f);
            return converted.SwizzlesFloat3(swizzle, reverseZWhenXYZ);
        }

        /// <summary>
        /// 받아온 Swizzle에 따라 float3의 축을 변환한 결과를 참조로 대입합니다.
        /// </summary>
        /// <param name="vector">변환할 원래 float3 위치 (ref)</param>
        /// <param name="swizzle">적용할 CellSwizzle 설정입니다.</param>
        /// <param name="reverseZWhenXYZ">XYZ 또는 YXZ Swizzle 시, Z축에 -1을 곱할지 여부입니다.</param>
        public static void SwizzlesFloat3Ref(this ref float3 vector, GridLayout.CellSwizzle swizzle, bool reverseZWhenXYZ = false)
        {
            vector = vector.SwizzlesFloat3(swizzle, reverseZWhenXYZ);
        }

        /// <summary>
        /// 받아온 Swizzle에 따라 float2의 축을 변환한 결과를 참조로 대입합니다.
        /// </summary>
        /// <param name="vector">변환할 원래 float2 위치 (ref)</param>
        /// <param name="swizzle">적용할 CellSwizzle 설정입니다.</param>
        /// <param name="reverseZWhenXYZ">XYZ 또는 YXZ Swizzle 시, Z축에 -1을 곱할지 여부입니다.</param>
        public static void SwizzlesFloat2Ref(this ref float2 vector, GridLayout.CellSwizzle swizzle, bool reverseZWhenXYZ = false)
        {
            float3 temp = new float3(vector.x, vector.y, 0f);
            temp = temp.SwizzlesFloat3(swizzle, reverseZWhenXYZ);
            vector = new float2(temp.x, temp.y);
        }



        //? Swizzle로 특정 축만 얻기/수정



        /// <summary>
        /// GridLayout의 셀 Swizzle 방식에 따라 float3의 특정 축 값을 얻어옵니다.<br/>
        /// (Swizzle 되기 전의 좌표계 기준에서 해당 축이 실질적으로 어느 인덱스로 매핑되는지 계산 후 반환)
        /// </summary>
        /// <param name="vector">대상 float3 벡터</param>
        /// <param name="axis">조회할 축</param>
        /// <param name="swizzle">적용할 CellSwizzle 설정</param>
        /// <returns>Swizzle 전 기준에서의 축 값</returns>
        public static float GetAxisBeforeSwizzle(this float3 vector, EAxis axis, GridLayout.CellSwizzle swizzle)
        {
            int axisIndex = (int)axis;
            int swizzledIndex = GetSwizzledIndex(swizzle, axisIndex);

            if (swizzledIndex == 0) return vector.x;
            if (swizzledIndex == 1) return vector.y;
            return vector.z;
        }

        /// <summary>
        /// GridLayout의 셀 Swizzle 방식에 따라 float3의 특정 축 값을 수정합니다.<br/>
        /// (Swizzle 되기 전의 좌표계에서 해당 축이 실질적으로 어느 인덱스로 매핑되는지 계산 후 값을 바꿉니다)
        /// </summary>
        /// <param name="vector">수정할 float3 (ref)</param>
        /// <param name="axis">수정할 축</param>
        /// <param name="newAxisValue">새로 설정할 값</param>
        /// <param name="swizzle">적용할 CellSwizzle 설정</param>
        public static void ModifyFloat3BySwizzle(this ref float3 vector, EAxis axis, float newAxisValue, GridLayout.CellSwizzle swizzle)
        {
            int axisIndex = (int)axis;
            int swizzledIndex = GetSwizzledIndex(swizzle, axisIndex);

            if (swizzledIndex == 0) vector.x = newAxisValue;
            else if (swizzledIndex == 1) vector.y = newAxisValue;
            else vector.z = newAxisValue;
        }



        //? Swizzle 쿼터니언



        /// <summary>
        /// <see cref="GridLayout.CellSwizzle"/> 설정에 따라 float3의 축을 회전으로 변환합니다.
        /// </summary>
        /// <param name="vector">변환할 원래 float3 (Euler) 값입니다.</param>
        /// <param name="swizzle">적용할 CellSwizzle 설정입니다.</param>
        /// <returns>변환된 quaternion 값입니다.</returns>
        public static quaternion SwizzlesQuaternion(this float3 vector, GridLayout.CellSwizzle swizzle)
        {
            float3 euler;
            switch (swizzle)
            {
                case GridLayout.CellSwizzle.XYZ:
                euler = new float3(vector.x, vector.y, vector.z);
                break;
                case GridLayout.CellSwizzle.XZY:
                euler = new float3(vector.x, vector.z, vector.y);
                break;
                case GridLayout.CellSwizzle.YXZ:
                euler = new float3(vector.y, vector.x, vector.z);
                break;
                case GridLayout.CellSwizzle.YZX:
                euler = new float3(vector.y, vector.z, vector.x);
                break;
                case GridLayout.CellSwizzle.ZXY:
                euler = new float3(vector.z, vector.x, vector.y);
                break;
                case GridLayout.CellSwizzle.ZYX:
                euler = new float3(vector.z, vector.y, vector.x);
                break;
                default:
                euler = vector;
                break;
            }

            // math.quaternion.Euler는 라디안을 사용하므로, 필요시 math.radians(euler) 등을 적용
            // (UnityEngine.Quaternion.Euler는 degree 사용)
            return quaternion.EulerZXY(math.radians(euler));
        }



        #endregion



        ///======================================================================================================================================================



        //? 비교



        #region 비교



        /// <summary>
        /// 두 개의 <see cref="Vector2"/> 간의 제곱 거리를 계산합니다.
        /// </summary>
        /// <param name="vector1">첫 번째 벡터입니다.</param>
        /// <param name="vector2">두 번째 벡터입니다.</param>
        /// <returns>두 벡터 간의 제곱 거리입니다.</returns>
        public static float SqrDistance(this Vector2 vector1, Vector2 vector2)
        {
            float deltaX = vector2.x - vector1.x;
            float deltaY = vector2.y - vector1.y;
            return deltaX * deltaX + deltaY * deltaY;
        }



        /// <summary>
        /// 두 개의 <see cref="Vector3"/> 간의 제곱 거리를 계산합니다.
        /// </summary>
        /// <param name="vector1">첫 번째 벡터입니다.</param>
        /// <param name="vector2">두 번째 벡터입니다.</param>
        /// <returns>두 벡터 간의 제곱 거리입니다.</returns>
        public static float SqrDistance(this Vector3 vector1, Vector3 vector2)
        {
            float deltaX = vector2.x - vector1.x;
            float deltaY = vector2.y - vector1.y;
            float deltaZ = vector2.z - vector1.z;
            return deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ;
        }




        /// <summary>
        /// 2D 벡터의 X 또는 Y 좌표 차이가 지정된 값보다 크면 위치가 변경된 것으로 간주합니다.
        /// (유클리드 거리 계산이 아닌, 각 축별 개별 비교)
        /// </summary>
        /// <param name="previousPosition">기준이 되는 이전 위치</param>
        /// <param name="currentPosition">비교할 새로운 위치</param>
        /// <param name="threshold">위치 변경 감지 기준이 되는 최소 차이값</param>
        /// <returns>어느 한 축(X 또는 Y)에서 이동 차이가 기준값을 초과하면 true, 그렇지 않으면 false</returns>
        public static bool HasPositionChangedAlongAxes(Vector2 previousPosition, Vector2 currentPosition, float threshold)
        {
            return Mathf.Abs(previousPosition.x - currentPosition.x) >= threshold
                || Mathf.Abs(previousPosition.y - currentPosition.y) >= threshold;
        }



        /// <summary>
        /// 3D 벡터의 X, Y 또는 Z 좌표 차이가 지정된 값보다 크면 위치가 변경된 것으로 간주합니다.
        /// (유클리드 거리 계산이 아닌, 각 축별 개별 비교)
        /// </summary>
        /// <param name="previousPosition">기준이 되는 이전 위치</param>
        /// <param name="currentPosition">비교할 새로운 위치</param>
        /// <param name="threshold">위치 변경 감지 기준이 되는 최소 차이값</param>
        /// <returns>어느 한 축(X, Y 또는 Z)에서 이동 차이가 기준값을 초과하면 true, 그렇지 않으면 false</returns>
        public static bool HasPositionChangedAlongAxes(Vector3 previousPosition, Vector3 currentPosition, float threshold)
        {
            return Mathf.Abs(previousPosition.x - currentPosition.x) >= threshold
                || Mathf.Abs(previousPosition.y - currentPosition.y) >= threshold
                || Mathf.Abs(previousPosition.z - currentPosition.z) >= threshold;
        }



        //? 내부 헬퍼 메서드: 두 float 값의 부호 비교
        private static bool AreSameSign(float a, float b)
        {
            return (a < 0 && b < 0) || (a > 0 && b > 0) || (a == 0 && b == 0);
        }



        //? 내부 헬퍼 메서드: 두 int 값의 부호 비교
        private static bool AreSameSign(int a, int b)
        {
            return (a < 0 && b < 0) || (a > 0 && b > 0) || (a == 0 && b == 0);
        }



        /// <summary>
        /// Vector2 타입에서 지정된 축(X 또는 Y) 좌표의 부호가 같은지 검사합니다. <br/>
        /// 만약 EAxis.Z가 지정되면 NotSupportedException을 발생시킵니다.
        /// </summary>
        public static bool HasSameAxisSign(this Vector2 vectorA, Vector2 vectorB, EAxis axis)
        {
            switch (axis)
            {
                case EAxis.X:
                return AreSameSign(vectorA.x, vectorB.x);
                case EAxis.Y:
                return AreSameSign(vectorA.y, vectorB.y);
                case EAxis.Z:
                throw new System.NotSupportedException("Vector2 does not support the Z axis.");
                default:
                throw new System.ArgumentOutOfRangeException(nameof(axis), axis, "Invalid axis specified.");
            }
        }



        /// <summary>
        /// Vector2Int 타입에서 지정된 축(X 또는 Y) 좌표의 부호가 같은지 검사합니다. <br/>
        /// 만약 EAxis.Z가 지정되면 NotSupportedException을 발생시킵니다.
        /// </summary>
        public static bool HasSameAxisSign(this Vector2Int vectorA, Vector2Int vectorB, EAxis axis)
        {
            switch (axis)
            {
                case EAxis.X:
                return AreSameSign(vectorA.x, vectorB.x);
                case EAxis.Y:
                return AreSameSign(vectorA.y, vectorB.y);
                case EAxis.Z:
                throw new System.NotSupportedException("Vector2Int does not support the Z axis.");
                default:
                throw new System.ArgumentOutOfRangeException(nameof(axis), axis, "Invalid axis specified.");
            }
        }



        /// <summary>
        /// Vector3 타입에서 지정된 축(X, Y 또는 Z) 좌표의 부호가 같은지 검사합니다.
        /// </summary>
        public static bool HasSameAxisSign(this Vector3 vectorA, Vector3 vectorB, EAxis axis)
        {
            switch (axis)
            {
                case EAxis.X:
                return AreSameSign(vectorA.x, vectorB.x);
                case EAxis.Y:
                return AreSameSign(vectorA.y, vectorB.y);
                case EAxis.Z:
                return AreSameSign(vectorA.z, vectorB.z);
                default:
                throw new System.ArgumentOutOfRangeException(nameof(axis), axis, "Invalid axis specified.");
            }
        }



        /// <summary>
        /// Vector3Int 타입에서 지정된 축(X, Y 또는 Z) 좌표의 부호가 같은지 검사합니다.
        /// </summary>
        public static bool HasSameAxisSign(this Vector3Int vectorA, Vector3Int vectorB, EAxis axis)
        {
            switch (axis)
            {
                case EAxis.X:
                return AreSameSign(vectorA.x, vectorB.x);
                case EAxis.Y:
                return AreSameSign(vectorA.y, vectorB.y);
                case EAxis.Z:
                return AreSameSign(vectorA.z, vectorB.z);
                default:
                throw new System.ArgumentOutOfRangeException(nameof(axis), axis, "Invalid axis specified.");
            }
        }



        /// <summary>
        /// 주어진 두 개의 Vector2Int 중 메인 Vector2Int와 가장 가까운 Vector2Int를 반환합니다.
        /// </summary>
        /// <param name="main">기준이 되는 메인 Vector2Int입니다.</param>
        /// <param name="a">비교할 첫 번째 Vector2Int입니다.</param>
        /// <param name="b">비교할 두 번째 Vector2Int입니다.</param>
        /// <returns>메인 Vector2Int와 가장 가까운 Vector2Int를 반환합니다.</returns>
        public static Vector2Int Closest(this Vector2Int main, Vector2Int a, Vector2Int b)
        {
            // a와 main 사이의 거리 제곱 계산 (정수 연산)
            int distanceToASq = (main.x - a.x) * (main.x - a.x) + (main.y - a.y) * (main.y - a.y);

            // b와 main 사이의 거리 제곱 계산 (정수 연산)
            int distanceToBSq = (main.x - b.x) * (main.x - b.x) + (main.y - b.y) * (main.y - b.y);

            // 더 가까운 거리의 Vector2Int 반환
            return distanceToASq < distanceToBSq ? a : b;
        }



        #endregion



        #region 비교 (버스트 최적화)



        /// <summary>
        /// 두 개의 float2 간 제곱 거리를 계산합니다.
        /// </summary>
        /// <param name="vector1">첫 번째 float2</param>
        /// <param name="vector2">두 번째 float2</param>
        /// <returns>두 float2 간의 제곱 거리</returns>
        public static float SqrDistance(this float2 vector1, float2 vector2)
        {
            float dx = vector2.x - vector1.x;
            float dy = vector2.y - vector1.y;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// 두 개의 float3 간 제곱 거리를 계산합니다.
        /// </summary>
        /// <param name="vector1">첫 번째 float3</param>
        /// <param name="vector2">두 번째 float3</param>
        /// <returns>두 float3 간의 제곱 거리</returns>
        public static float SqrDistance(this float3 vector1, float3 vector2)
        {
            float dx = vector2.x - vector1.x;
            float dy = vector2.y - vector1.y;
            float dz = vector2.z - vector1.z;
            return dx * dx + dy * dy + dz * dz;
        }

        /// <summary>
        /// 두 개의 int2 간 제곱 거리를 계산합니다. (정수 연산)
        /// </summary>
        /// <param name="vector1">첫 번째 int2</param>
        /// <param name="vector2">두 번째 int2</param>
        /// <returns>두 int2 간의 제곱 거리</returns>
        public static int SqrDistance(this int2 vector1, int2 vector2)
        {
            int dx = vector2.x - vector1.x;
            int dy = vector2.y - vector1.y;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// 두 개의 int3 간 제곱 거리를 계산합니다. (정수 연산)
        /// </summary>
        /// <param name="vector1">첫 번째 int3</param>
        /// <param name="vector2">두 번째 int3</param>
        /// <returns>두 int3 간의 제곱 거리</returns>
        public static int SqrDistance(this int3 vector1, int3 vector2)
        {
            int dx = vector2.x - vector1.x;
            int dy = vector2.y - vector1.y;
            int dz = vector2.z - vector1.z;
            return dx * dx + dy * dy + dz * dz;
        }



        /// <summary>
        /// 2D float2의 X 또는 Y 좌표 차이가 지정된 값보다 크면 위치가 변경된 것으로 간주합니다.
        /// (유클리드 거리 아님, 각 축별로 직접 비교)
        /// </summary>
        /// <param name="previousPosition">이전 위치 (float2)</param>
        /// <param name="currentPosition">현재 위치 (float2)</param>
        /// <param name="threshold">각 축에서 최소 차이값</param>
        /// <returns>어느 한 축에서 이동 차이가 threshold를 초과하면 true, 아니면 false</returns>
        public static bool HasPositionChangedAlongAxes(this float2 previousPosition, float2 currentPosition, float threshold)
        {
            // math.abs는 Unity.Mathematics에서 제공됩니다.
            return math.abs(previousPosition.x - currentPosition.x) >= threshold
                || math.abs(previousPosition.y - currentPosition.y) >= threshold;
        }

        /// <summary>
        /// 3D float3의 X, Y, Z 좌표 차이가 지정된 값보다 크면 위치가 변경된 것으로 간주합니다.
        /// (유클리드 거리 아님, 각 축별로 직접 비교)
        /// </summary>
        /// <param name="previousPosition">이전 위치 (float3)</param>
        /// <param name="currentPosition">현재 위치 (float3)</param>
        /// <param name="threshold">각 축에서 최소 차이값</param>
        /// <returns>어느 한 축에서 이동 차이가 threshold를 초과하면 true, 아니면 false</returns>
        public static bool HasPositionChangedAlongAxes(this float3 previousPosition, float3 currentPosition, float threshold)
        {
            return math.abs(previousPosition.x - currentPosition.x) >= threshold
                || math.abs(previousPosition.y - currentPosition.y) >= threshold
                || math.abs(previousPosition.z - currentPosition.z) >= threshold;
        }



        /// <summary>
        /// float2 타입에서 지정된 축(X 또는 Y) 좌표의 부호가 같은지 검사합니다.<br/>
        /// 만약 EAxis.Z가 지정되면 NotSupportedException을 발생시킵니다.
        /// </summary>
        public static bool HasSameAxisSign(this float2 vectorA, float2 vectorB, EAxis axis)
        {
            switch (axis)
            {
                case EAxis.X: return AreSameSign(vectorA.x, vectorB.x);
                case EAxis.Y: return AreSameSign(vectorA.y, vectorB.y);
                case EAxis.Z:
                throw new System.NotSupportedException("float2 does not support the Z axis.");
                default:
                throw new System.ArgumentOutOfRangeException(nameof(axis), axis, "Invalid axis specified.");
            }
        }

        /// <summary>
        /// float3 타입에서 지정된 축(X, Y 또는 Z) 좌표의 부호가 같은지 검사합니다.
        /// </summary>
        public static bool HasSameAxisSign(this float3 vectorA, float3 vectorB, EAxis axis)
        {
            switch (axis)
            {
                case EAxis.X: return AreSameSign(vectorA.x, vectorB.x);
                case EAxis.Y: return AreSameSign(vectorA.y, vectorB.y);
                case EAxis.Z: return AreSameSign(vectorA.z, vectorB.z);
                default:
                throw new System.ArgumentOutOfRangeException(nameof(axis), axis, "Invalid axis specified.");
            }
        }

        /// <summary>
        /// int2 타입에서 지정된 축(X 또는 Y) 좌표의 부호가 같은지 검사합니다.<br/>
        /// 만약 EAxis.Z가 지정되면 NotSupportedException을 발생시킵니다.
        /// </summary>
        public static bool HasSameAxisSign(this int2 vectorA, int2 vectorB, EAxis axis)
        {
            switch (axis)
            {
                case EAxis.X: return AreSameSign(vectorA.x, vectorB.x);
                case EAxis.Y: return AreSameSign(vectorA.y, vectorB.y);
                case EAxis.Z:
                throw new System.NotSupportedException("int2 does not support the Z axis.");
                default:
                throw new System.ArgumentOutOfRangeException(nameof(axis), axis, "Invalid axis specified.");
            }
        }

        /// <summary>
        /// int3 타입에서 지정된 축(X, Y 또는 Z) 좌표의 부호가 같은지 검사합니다.
        /// </summary>
        public static bool HasSameAxisSign(this int3 vectorA, int3 vectorB, EAxis axis)
        {
            switch (axis)
            {
                case EAxis.X: return AreSameSign(vectorA.x, vectorB.x);
                case EAxis.Y: return AreSameSign(vectorA.y, vectorB.y);
                case EAxis.Z: return AreSameSign(vectorA.z, vectorB.z);
                default:
                throw new System.ArgumentOutOfRangeException(nameof(axis), axis, "Invalid axis specified.");
            }
        }



        /// <summary>
        /// 주어진 두 개의 float2 중 메인 float2와 가장 가까운 float2를 반환합니다.
        /// </summary>
        /// <param name="main">기준이 되는 메인 float2</param>
        /// <param name="a">비교할 첫 번째 float2</param>
        /// <param name="b">비교할 두 번째 float2</param>
        public static float2 Closest(this float2 main, float2 a, float2 b)
        {
            float distA = main.SqrDistance(a);
            float distB = main.SqrDistance(b);
            return distA < distB ? a : b;
        }

        /// <summary>
        /// 주어진 두 개의 float3 중 메인 float3와 가장 가까운 float3를 반환합니다.
        /// </summary>
        /// <param name="main">기준이 되는 메인 float3</param>
        /// <param name="a">비교할 첫 번째 float3</param>
        /// <param name="b">비교할 두 번째 float3</param>
        public static float3 Closest(this float3 main, float3 a, float3 b)
        {
            float distA = main.SqrDistance(a);
            float distB = main.SqrDistance(b);
            return distA < distB ? a : b;
        }

        /// <summary>
        /// 주어진 두 개의 int2 중 메인 int2와 가장 가까운 int2를 반환합니다.
        /// </summary>
        /// <param name="main">기준이 되는 메인 int2</param>
        /// <param name="a">비교할 첫 번째 int2</param>
        /// <param name="b">비교할 두 번째 int2</param>
        public static int2 Closest(this int2 main, int2 a, int2 b)
        {
            int distA = main.SqrDistance(a);
            int distB = main.SqrDistance(b);
            return distA < distB ? a : b;
        }

        /// <summary>
        /// 주어진 두 개의 int3 중 메인 int3와 가장 가까운 int3를 반환합니다.
        /// </summary>
        /// <param name="main">기준이 되는 메인 int3</param>
        /// <param name="a">비교할 첫 번째 int3</param>
        /// <param name="b">비교할 두 번째 int3</param>
        public static int3 Closest(this int3 main, int3 a, int3 b)
        {
            int distA = main.SqrDistance(a);
            int distB = main.SqrDistance(b);
            return distA < distB ? a : b;
        }



        #endregion



        ///======================================================================================================================================================



        //? 방향



        /// <summary>
        /// 두 점 사이의 방향(단위 벡터)을 계산하여 반환합니다. <br/>
        /// 반환되는 벡터는 시작점(from)에서 목표점(to)으로 향하는 정규화된 방향입니다.
        /// </summary>
        /// <param name="from">시작 위치</param>
        /// <param name="to">목표 위치</param>
        /// <returns>from에서 to로 향하는 단위 방향 벡터</returns>
        public static Vector2 GetDirectionVector(this Vector2 from, Vector2 to)
        {
            return (to - from).normalized;
        }



        //? 방향 (버스트 최적화)



        /// <summary>
        /// 두 점 사이의 방향(단위 벡터)을 계산하여 반환합니다. <br/>
        /// 반환되는 벡터는 시작점(from)에서 목표점(to)으로 향하는 정규화된 방향입니다.
        /// </summary>
        /// <param name="from">시작 위치 (float2)</param>
        /// <param name="to">목표 위치 (float2)</param>
        /// <returns>from에서 to로 향하는 단위 방향 벡터 (float2)</returns>
        public static float2 GetDirectionVector(this float2 from, float2 to)
        {
            return math.normalize(to - from);
        }



        ///======================================================================================================================================================



        //? 회전



        /// <summary>
        /// 주어진 방향 벡터에서 z축 기준 각도(도 단위)를 계산하여 반환합니다. <br/>
        /// 기본적으로 결과에 -90도를 적용하지만, 원하는 오프셋 값을 지정할 수 있습니다.
        /// </summary>
        /// <param name="direction">방향 벡터 (정규화된 값이 권장됨)</param>
        /// <param name="angleOffset">
        /// 계산된 각도에 적용할 오프셋 (기본값: -90도). <br/>
        /// 예를 들어, -90를 지정하면 결과에서 90도가 빼집니다.
        /// </param>
        /// <returns>계산된 각도 (도 단위)</returns>
        public static float ToAngleDegrees(this Vector2 direction, float angleOffset = -90f)
        {
            direction.Normalize();
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return angle + angleOffset;
        }



        //? 회전 (버스트 최적화)



        /// <summary>
        /// 주어진 방향 벡터에서 z축 기준 각도(도 단위)를 계산하여 반환합니다. <br/>
        /// 기본적으로 결과에 -90도를 적용하지만, 원하는 오프셋 값을 지정할 수 있습니다.
        /// </summary>
        /// <param name="direction">방향 벡터 (정규화된 값이 권장됨)</param>
        /// <param name="angleOffset">
        /// 계산된 각도에 적용할 오프셋 (기본값: -90도). <br/>
        /// 예를 들어, -90를 지정하면 결과에서 90도가 빼집니다.
        /// </param>
        /// <returns>계산된 각도 (도 단위)</returns>
        public static float ToAngleDegrees(this float2 direction, float angleOffset = -90f)
        {
            direction = math.normalize(direction); //. 정규화된 벡터 보장
            float angle = math.degrees(math.atan2(direction.y, direction.x)); //. 라디안을 도 단위로 변환
            return angle + angleOffset; //? 오프셋 적용
        }



        ///======================================================================================================================================================



        //? 벡터 사이 간격 조절


        /// <summary>
        /// 두 지점 <paramref name="a"/> 와 <paramref name="b"/> 를 서로를 향해 <b>거리의 일정 비율</b>만큼 이동시킵니다. <br/>
        /// <paramref name="ratio"/> 값이 0.1f 라면 - 두 지점은 각각 전체 거리의 10 % 만큼 상대 지점을 향해 이동합니다.
        /// </summary>
        /// <param name="a">첫 번째 지점 (참조 전달)</param>
        /// <param name="b">두 번째 지점 (참조 전달)</param>
        /// <param name="ratio">
        /// 0 ~ 0.5 사이 권장. 0 → 이동 없음, 0.5 → 두 지점이 정확히 중간에서 만남. <br/>
        /// 범위를 벗어나면 자동으로 0 ~ 0.5 로 클램프됩니다.
        /// </param>
        public static void MoveTowardsEachOtherByRatio(ref Vector3 a, ref Vector3 b, float ratio)
        {
            ratio = Mathf.Clamp(ratio, 0f, 0.5f);                 //. 안전 범위 보정
            Vector3 dir = b - a;                                   //. 두 점 사이 방향
            float dist = dir.magnitude;

            //! 두 점이 같은 위치이면 이동 불가
            if (dist <= Mathf.Epsilon) { return; }

            Vector3 dirN = dir / dist;                             //. 정규화
            float offset = dist * ratio;                           //. 이동 길이
            a += dirN * offset;
            b -= dirN * offset;
        }



        /// <summary>
        /// 두 지점 <paramref name="a"/> 와 <paramref name="b"/> 를 서로를 향해 <b>절대 길이</b>만큼 이동시킵니다. <br/>
        /// 개별 거리(<paramref name="offsetA"/>, <paramref name="offsetB"/>)를 지정할 수 있습니다.
        /// </summary>
        /// <param name="a">첫 번째 지점 (참조 전달)</param>
        /// <param name="b">두 번째 지점 (참조 전달)</param>
        /// <param name="offsetA">첫 번째 지점이 이동할 거리 (음수 금지)</param>
        /// <param name="offsetB">두 번째 지점이 이동할 거리 (음수 금지)</param>
        public static void MoveTowardsEachOtherByDistance(ref Vector3 a, ref Vector3 b, float offsetA, float offsetB)
        {
            offsetA = Mathf.Max(0f, offsetA);                      //. 음수 방지
            offsetB = Mathf.Max(0f, offsetB);

            Vector3 dir = b - a;
            float dist = dir.magnitude;

            //! 두 점이 같거나 실제 거리보다 과한 이동 요청 시 – 중간점 기준으로 클램프
            if (dist <= Mathf.Epsilon) { return; }

            float halfDist = dist * 0.5f;
            offsetA = Mathf.Min(offsetA, halfDist);
            offsetB = Mathf.Min(offsetB, halfDist);

            Vector3 dirN = dir / dist;
            a += dirN * offsetA;
            b -= dirN * offsetB;
        }



        ///======================================================================================================================================================



        //? 삼각함수



        /// <summary>
        /// 라디안 값을 기준으로 단위 방향 벡터(Vector2)를 반환합니다. <br/>
        /// 입력된 각도는 라디안 단위입니다.
        /// </summary>
        /// <param name="radians">라디안 단위의 각도 값</param>
        /// <returns>입력 각도에 해당하는 단위 방향 벡터</returns>
        public static Vector2 ToUnitVectorFromRadians(this float radians)
        {
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }



        /// <summary>
        /// 도(degree) 값을 기준으로 단위 방향 벡터(Vector2)를 반환합니다. <br/>
        /// 내부적으로 도 값을 라디안으로 변환하여 처리합니다.
        /// </summary>
        /// <param name="degrees">도 단위의 각도 값</param>
        /// <returns>입력 각도에 해당하는 단위 방향 벡터</returns>
        public static Vector2 ToUnitVectorFromDegrees(this float degrees)
        {
            return ToUnitVectorFromRadians(degrees * Mathf.Deg2Rad);
        }



        //? 삼각함수 (버스트 최적화)



        /// <summary>
        /// 라디안 값을 기준으로 단위 방향 벡터(float2)를 반환합니다. <br/>
        /// 입력된 각도는 라디안 단위입니다.
        /// </summary>
        public static float2 ToUnitDirectionFromRadiansFloat2(this float radians)
        {
            return new float2(math.cos(radians), math.sin(radians));
        }



        /// <summary>
        /// 도(degree) 값을 기준으로 단위 방향 벡터(float2)를 반환합니다. <br/>
        /// 내부적으로 도 값을 라디안으로 변환하여 처리합니다.
        /// </summary>
        public static float2 ToUnitDirectionFromDegreesFloat2(this float degrees)
        {
            return ToUnitDirectionFromRadiansFloat2(math.radians(degrees));
        }



        ///======================================================================================================================================================



        //? 스왑



        /// <summary>
        /// Vector2의 X축과 Y축 값을 서로 바꿉니다.
        /// </summary>
        /// <param name="vector">축 값을 바꿀 Vector2</param>
        public static void SwapAxes(this ref Vector2 vector)
        {
            vector = new Vector2(vector.y, vector.x);
        }



        /// <summary>
        /// Vector2Int의 X축과 Y축 값을 서로 바꿉니다.
        /// </summary>
        /// <param name="vector">축 값을 바꿀 Vector2Int</param>
        public static void SwapAxes(this ref Vector2Int vector)
        {
            vector = new Vector2Int(vector.y, vector.x);
        }



        /// <summary>
        /// Vector3의 지정된 축 값을 서로 바꿉니다.
        /// </summary>
        /// <param name="vector">축 값을 바꿀 Vector3</param>
        /// <param name="axes">바꿀 축 (XY, YZ, XZ 유효)</param>
        /// <exception cref="System.ArgumentException">유효하지 않은 축이 전달된 경우</exception>
        public static void SwapAxes(this ref Vector3 vector, EAxes axes)
        {
            switch (axes)
            {
                case EAxes.XY:
                vector = new Vector3(vector.y, vector.x, vector.z);
                break;
                case EAxes.YZ:
                vector = new Vector3(vector.x, vector.z, vector.y);
                break;
                case EAxes.XZ:
                vector = new Vector3(vector.z, vector.y, vector.x);
                break;
                default:
                throw new System.ArgumentException("Vector3에서는 XY, YZ, XZ 축만 유효합니다.");
            }
        }



        /// <summary>
        /// Vector3Int의 지정된 축 값을 서로 바꿉니다.
        /// </summary>
        /// <param name="vector">축 값을 바꿀 Vector3Int</param>
        /// <param name="axes">바꿀 축 (XY, YZ, XZ 유효)</param>
        /// <exception cref="System.ArgumentException">유효하지 않은 축이 전달된 경우</exception>
        public static void SwapAxes(this ref Vector3Int vector, EAxes axes)
        {
            switch (axes)
            {
                case EAxes.XY:
                vector = new Vector3Int(vector.y, vector.x, vector.z);
                break;
                case EAxes.YZ:
                vector = new Vector3Int(vector.x, vector.z, vector.y);
                break;
                case EAxes.XZ:
                vector = new Vector3Int(vector.z, vector.y, vector.x);
                break;
                default:
                throw new System.ArgumentException("Vector3Int에서는 XY, YZ, XZ 축만 유효합니다.");
            }
        }



        //? 스왑 (버스트 최적화)



        /// <summary>
        /// float2의 X축과 Y축 값을 서로 바꿉니다.
        /// </summary>
        public static void SwapXY(this ref float2 vector)
        {
            vector = new float2(vector.y, vector.x);
        }



        /// <summary>
        /// int2의 X축과 Y축 값을 서로 바꿉니다.
        /// </summary>
        public static void SwapXY(this ref int2 vector)
        {
            vector = new int2(vector.y, vector.x);
        }



        /// <summary>
        /// float3의 지정된 축 값을 서로 바꿉니다. (XY, YZ, XZ 유효)
        /// </summary>
        public static void SwapAxes(this ref float3 vector, EAxes axes)
        {
            switch (axes)
            {
                case EAxes.XY:
                vector = new float3(vector.y, vector.x, vector.z);
                break;
                case EAxes.YZ:
                vector = new float3(vector.x, vector.z, vector.y);
                break;
                case EAxes.XZ:
                vector = new float3(vector.z, vector.y, vector.x);
                break;
                default:
                throw new System.ArgumentException("float3에서는 XY, YZ, XZ 축만 유효합니다.");
            }
        }



        /// <summary>
        /// int3의 지정된 축 값을 서로 바꿉니다. (XY, YZ, XZ 유효)
        /// </summary>
        public static void SwapAxes(this ref int3 vector, EAxes axes)
        {
            switch (axes)
            {
                case EAxes.XY:
                vector = new int3(vector.y, vector.x, vector.z);
                break;
                case EAxes.YZ:
                vector = new int3(vector.x, vector.z, vector.y);
                break;
                case EAxes.XZ:
                vector = new int3(vector.z, vector.y, vector.x);
                break;
                default:
                throw new System.ArgumentException("int3에서는 XY, YZ, XZ 축만 유효합니다.");
            }
        }



        ///======================================================================================================================================================



        //? 변환



        /// <summary>
        /// Vector2Int를 Vector3Int로 변환 (z에는 0이들어간다)
        /// </summary>
        /// <param name="var"></param>
        /// <returns></returns>
        public static Vector3Int ConvertVector3Int(this Vector2Int var)
        {
            return new Vector3Int(var.x, var.y, 0);
        }



        /// <summary>
        /// Vector3Int를 Vector2Int로 변환
        /// </summary>
        /// <param name="var"></param>
        /// <returns></returns>
        public static Vector2Int ConvertVector2Int(this Vector3Int var)
        {
            return new Vector2Int(var.x, var.y);
        }



        //? 변환 (버스트 최적화)



        /// <summary>
        /// int2를 int3로 변환 (z에는 0이들어간다)
        /// </summary>
        /// <param name="var"></param>
        /// <returns></returns>
        public static int3 Convertint3(this int2 var)
        {
            return new int3(var.x, var.y, 0);
        }



        /// <summary>
        /// int3를 int2로 변환
        /// </summary>
        /// <param name="var"></param>
        /// <returns></returns>
        public static int2 Convertint2(this int3 var)
        {
            return new int2(var.x, var.y);
        }



        ///======================================================================================================================================================



        //? 벡터의 가장 (작은/큰) 값 반환



        /// <summary>
        /// Vector3 확장 메서드 - 벡터의 최대 값을 반환합니다.
        /// </summary>
        /// <param name="vector">Vector3 인스턴스</param>
        /// <returns>최대 값</returns>
        public static float GetMaxComponent(this Vector3 vector)
        {
            return Mathf.Max(vector.x, vector.y, vector.z);
        }

        /// <summary>
        /// Vector2 확장 메서드 - 벡터의 최대 값을 반환합니다.
        /// </summary>
        /// <param name="vector">Vector2 인스턴스</param>
        /// <returns>최대 값</returns>
        public static float GetMaxComponent(this Vector2 vector)
        {
            return Mathf.Max(vector.x, vector.y);
        }

        /// <summary>
        /// Vector3Int 확장 메서드 - 벡터의 최대 값을 반환합니다.
        /// </summary>
        /// <param name="vector">Vector3Int 인스턴스</param>
        /// <returns>최대 값</returns>
        public static int GetMaxComponent(this Vector3Int vector)
        {
            return Mathf.Max(vector.x, vector.y, vector.z);
        }

        /// <summary>
        /// Vector2Int 확장 메서드 - 벡터의 최대 값을 반환합니다.
        /// </summary>
        /// <param name="vector">Vector2Int 인스턴스</param>
        /// <returns>최대 값</returns>
        public static int GetMaxComponent(this Vector2Int vector)
        {
            return Mathf.Max(vector.x, vector.y);
        }


        /// <summary>
        /// Vector3 확장 메서드 - 벡터의 최소 값을 반환합니다.
        /// </summary>
        /// <param name="vector">Vector3 인스턴스</param>
        /// <returns>최소 값</returns>
        public static float GetMinComponent(this Vector3 vector)
        {
            return Mathf.Min(vector.x, vector.y, vector.z);
        }

        /// <summary>
        /// Vector2 확장 메서드 - 벡터의 최소 값을 반환합니다.
        /// </summary>
        /// <param name="vector">Vector2 인스턴스</param>
        /// <returns>최소 값</returns>
        public static float GetMinComponent(this Vector2 vector)
        {
            return Mathf.Min(vector.x, vector.y);
        }

        /// <summary>
        /// Vector3Int 확장 메서드 - 벡터의 최소 값을 반환합니다.
        /// </summary>
        /// <param name="vector">Vector3Int 인스턴스</param>
        /// <returns>최소 값</returns>
        public static int GetMinComponent(this Vector3Int vector)
        {
            return Mathf.Min(vector.x, vector.y, vector.z);
        }

        /// <summary>
        /// Vector2Int 확장 메서드 - 벡터의 최소 값을 반환합니다.
        /// </summary>
        /// <param name="vector">Vector2Int 인스턴스</param>
        /// <returns>최소 값</returns>
        public static int GetMinComponent(this Vector2Int vector)
        {
            return Mathf.Min(vector.x, vector.y);
        }



        //? 벡터의 가장 (작은/큰) 값 반환 (버스트 최적화)



        /// <summary>
        /// float2 확장 메서드 - 벡터의 최대 값을 반환합니다.
        /// </summary>
        /// <param name="vector">float2 인스턴스</param>
        /// <returns>최대 값</returns>
        public static float GetMaxComponent(this float2 vector)
        {
            return math.max(vector.x, vector.y);
        }

        /// <summary>
        /// float3 확장 메서드 - 벡터의 최대 값을 반환합니다.
        /// </summary>
        /// <param name="vector">float3 인스턴스</param>
        /// <returns>최대 값</returns>
        public static float GetMaxComponent(this float3 vector)
        {
            return math.max(vector.x, math.max(vector.y, vector.z));
        }

        /// <summary>
        /// int2 확장 메서드 - 벡터의 최대 값을 반환합니다.
        /// </summary>
        /// <param name="vector">int2 인스턴스</param>
        /// <returns>최대 값</returns>
        public static int GetMaxComponent(this int2 vector)
        {
            return math.max(vector.x, vector.y);
        }

        /// <summary>
        /// int3 확장 메서드 - 벡터의 최대 값을 반환합니다.
        /// </summary>
        /// <param name="vector">int3 인스턴스</param>
        /// <returns>최대 값</returns>
        public static int GetMaxComponent(this int3 vector)
        {
            return math.max(vector.x, math.max(vector.y, vector.z));
        }



        /// <summary>
        /// float2 확장 메서드 - 벡터의 최소 값을 반환합니다.
        /// </summary>
        /// <param name="vector">float2 인스턴스</param>
        /// <returns>최소 값</returns>
        public static float GetMinComponent(this float2 vector)
        {
            return math.min(vector.x, vector.y);
        }

        /// <summary>
        /// float3 확장 메서드 - 벡터의 최소 값을 반환합니다.
        /// </summary>
        /// <param name="vector">float3 인스턴스</param>
        /// <returns>최소 값</returns>
        public static float GetMinComponent(this float3 vector)
        {
            return math.min(vector.x, math.min(vector.y, vector.z));
        }

        /// <summary>
        /// int2 확장 메서드 - 벡터의 최소 값을 반환합니다.
        /// </summary>
        /// <param name="vector">int2 인스턴스</param>
        /// <returns>최소 값</returns>
        public static int GetMinComponent(this int2 vector)
        {
            return math.min(vector.x, vector.y);
        }

        /// <summary>
        /// int3 확장 메서드 - 벡터의 최소 값을 반환합니다.
        /// </summary>
        /// <param name="vector">int3 인스턴스</param>
        /// <returns>최소 값</returns>
        public static int GetMinComponent(this int3 vector)
        {
            return math.min(vector.x, math.min(vector.y, vector.z));
        }




        ///======================================================================================================================================================



        //? 벡터 곱하기/나누기



        #region 벡터 곱하기/나누기



        /// <summary>
        /// 두 개의 Vector3 인스턴스의 각 구성 요소를 곱하여, 새로운 Vector3를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다.
        /// </summary>
        /// <param name="a">첫 번째 Vector3.</param>
        /// <param name="b">두 번째 Vector3.</param>
        /// <returns><paramref name="a"/>와 <paramref name="b"/>의 각 구성 요소 곱셈 결과를 포함하는 새로운 Vector3를 반환합니다.</returns>
        public static Vector3 Multiply(this Vector3 a, Vector3 b)
        {
            return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        }

        /// <summary>
        /// Vector3의 각 구성 요소에 스칼라 <paramref name="b"/>를 곱하여, 새로운 Vector3를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다.
        /// </summary>
        /// <param name="a">입력 Vector3.</param>
        /// <param name="b">곱할 스칼라 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소에 <paramref name="b"/>를 곱한 결과를 포함하는 새로운 Vector3를 반환합니다.</returns>
        public static Vector3 Multiply(this Vector3 a, float b)
        {
            return new Vector3(a.x * b, a.y * b, a.z * b);
        }

        /// <summary>
        /// 두 개의 Vector3Int 인스턴스의 각 구성 요소를 곱하여, 새로운 Vector3Int를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다.
        /// </summary>
        /// <param name="a">첫 번째 Vector3Int.</param>
        /// <param name="b">두 번째 Vector3Int.</param>
        /// <returns><paramref name="a"/>와 <paramref name="b"/>의 각 구성 요소 곱셈 결과를 포함하는 새로운 Vector3Int를 반환합니다.</returns>
        public static Vector3Int Multiply(this Vector3Int a, Vector3Int b)
        {
            return new Vector3Int(a.x * b.x, a.y * b.y, a.z * b.z);
        }

        /// <summary>
        /// Vector3Int의 각 구성 요소에 정수 <paramref name="b"/>를 곱하여, 새로운 Vector3Int를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다.
        /// </summary>
        /// <param name="a">입력 Vector3Int.</param>
        /// <param name="b">곱할 정수 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소에 <paramref name="b"/>를 곱한 결과를 포함하는 새로운 Vector3Int를 반환합니다.</returns>
        public static Vector3Int Multiply(this Vector3Int a, int b)
        {
            return new Vector3Int(a.x * b, a.y * b, a.z * b);
        }

        /// <summary>
        /// 두 개의 Vector2 인스턴스의 각 구성 요소를 곱하여, 새로운 Vector2를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다.
        /// </summary>
        /// <param name="a">첫 번째 Vector2.</param>
        /// <param name="b">두 번째 Vector2.</param>
        /// <returns><paramref name="a"/>와 <paramref name="b"/>의 각 구성 요소 곱셈 결과를 포함하는 새로운 Vector2를 반환합니다.</returns>
        public static Vector2 Multiply(this Vector2 a, Vector2 b)
        {
            return new Vector2(a.x * b.x, a.y * b.y);
        }

        /// <summary>
        /// Vector2의 각 구성 요소에 스칼라 <paramref name="b"/>를 곱하여, 새로운 Vector2를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다.
        /// </summary>
        /// <param name="a">입력 Vector2.</param>
        /// <param name="b">곱할 스칼라 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소에 <paramref name="b"/>를 곱한 결과를 포함하는 새로운 Vector2를 반환합니다.</returns>
        public static Vector2 Multiply(this Vector2 a, float b)
        {
            return new Vector2(a.x * b, a.y * b);
        }

        /// <summary>
        /// 두 개의 Vector2Int 인스턴스의 각 구성 요소를 곱하여, 새로운 Vector2Int를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다.
        /// </summary>
        /// <param name="a">첫 번째 Vector2Int.</param>
        /// <param name="b">두 번째 Vector2Int.</param>
        /// <returns><paramref name="a"/>와 <paramref name="b"/>의 각 구성 요소 곱셈 결과를 포함하는 새로운 Vector2Int를 반환합니다.</returns>
        public static Vector2Int Multiply(this Vector2Int a, Vector2Int b)
        {
            return new Vector2Int(a.x * b.x, a.y * b.y);
        }

        /// <summary>
        /// Vector2Int의 각 구성 요소에 정수 <paramref name="b"/>를 곱하여, 새로운 Vector2Int를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다.
        /// </summary>
        /// <param name="a">입력 Vector2Int.</param>
        /// <param name="b">곱할 정수 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소에 <paramref name="b"/>를 곱한 결과를 포함하는 새로운 Vector2Int를 반환합니다.</returns>
        public static Vector2Int Multiply(this Vector2Int a, int b)
        {
            return new Vector2Int(a.x * b, a.y * b);
        }



        /// <summary>
        /// 두 개의 Vector3 인스턴스의 각 구성 요소를 곱하여, 결과를 <paramref name="a"/>에 할당한 후 반환합니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 벡터를 직접 수정합니다.
        /// </summary>
        /// <param name="a">수정될 Vector3 (참조 전달). 각 구성 요소가 수정됩니다.</param>
        /// <param name="b">곱할 Vector3.</param>
        /// <returns><paramref name="a"/>와 <paramref name="b"/>의 각 구성 요소 곱셈 결과로 업데이트된 Vector3를 반환합니다.</returns>
        public static Vector3 MultiplyRef(this ref Vector3 a, Vector3 b)
        {
            return a = new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        }

        /// <summary>
        /// Vector3의 각 구성 요소에 스칼라 <paramref name="b"/>를 곱하여, 결과를 <paramref name="a"/>에 할당한 후 반환합니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 벡터를 직접 수정합니다.
        /// </summary>
        /// <param name="a">수정될 Vector3 (참조 전달).</param>
        /// <param name="b">곱할 스칼라 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소에 <paramref name="b"/>를 곱한 결과로 업데이트된 Vector3를 반환합니다.</returns>
        public static Vector3 MultiplyRef(this ref Vector3 a, float b)
        {
            return a = new Vector3(a.x * b, a.y * b, a.z * b);
        }

        /// <summary>
        /// 두 개의 Vector3Int 인스턴스의 각 구성 요소를 곱하여, 결과를 <paramref name="a"/>에 할당한 후 반환합니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 벡터를 직접 수정합니다.
        /// </summary>
        /// <param name="a">수정될 Vector3Int (참조 전달).</param>
        /// <param name="b">곱할 Vector3Int.</param>
        /// <returns><paramref name="a"/>와 <paramref name="b"/>의 각 구성 요소 곱셈 결과로 업데이트된 Vector3Int를 반환합니다.</returns>
        public static Vector3Int MultiplyRef(this ref Vector3Int a, Vector3Int b)
        {
            return a = new Vector3Int(a.x * b.x, a.y * b.y, a.z * b.z);
        }

        /// <summary>
        /// Vector3Int의 각 구성 요소에 정수 <paramref name="b"/>를 곱하여, 결과를 <paramref name="a"/>에 할당한 후 반환합니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 벡터를 직접 수정합니다.
        /// </summary>
        /// <param name="a">수정될 Vector3Int (참조 전달).</param>
        /// <param name="b">곱할 정수 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소에 <paramref name="b"/>를 곱한 결과로 업데이트된 Vector3Int를 반환합니다.</returns>
        public static Vector3Int MultiplyRef(this ref Vector3Int a, int b)
        {
            return a = new Vector3Int(a.x * b, a.y * b, a.z * b);
        }

        /// <summary>
        /// 두 개의 Vector2 인스턴스의 각 구성 요소를 곱하여, 결과를 <paramref name="a"/>에 할당한 후 반환합니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 벡터를 직접 수정합니다.
        /// </summary>
        /// <param name="a">수정될 Vector2 (참조 전달).</param>
        /// <param name="b">곱할 Vector2.</param>
        /// <returns><paramref name="a"/>와 <paramref name="b"/>의 각 구성 요소 곱셈 결과로 업데이트된 Vector2를 반환합니다.</returns>
        public static Vector2 MultiplyRef(this ref Vector2 a, Vector2 b)
        {
            return a = new Vector2(a.x * b.x, a.y * b.y);
        }

        /// <summary>
        /// Vector2의 각 구성 요소에 스칼라 <paramref name="b"/>를 곱하여, 결과를 <paramref name="a"/>에 할당한 후 반환합니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 벡터를 직접 수정합니다.
        /// </summary>
        /// <param name="a">수정될 Vector2 (참조 전달).</param>
        /// <param name="b">곱할 스칼라 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소에 <paramref name="b"/>를 곱한 결과로 업데이트된 Vector2를 반환합니다.</returns>
        public static Vector2 MultiplyRef(this ref Vector2 a, float b)
        {
            return a = new Vector2(a.x * b, a.y * b);
        }

        /// <summary>
        /// 두 개의 Vector2Int 인스턴스의 각 구성 요소를 곱하여, 결과를 <paramref name="a"/>에 할당한 후 반환합니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 벡터를 직접 수정합니다.
        /// </summary>
        /// <param name="a">수정될 Vector2Int (참조 전달).</param>
        /// <param name="b">곱할 Vector2Int.</param>
        /// <returns><paramref name="a"/>와 <paramref name="b"/>의 각 구성 요소 곱셈 결과로 업데이트된 Vector2Int를 반환합니다.</returns>
        public static Vector2Int MultiplyRef(this ref Vector2Int a, Vector2Int b)
        {
            return a = new Vector2Int(a.x * b.x, a.y * b.y);
        }

        /// <summary>
        /// Vector2Int의 각 구성 요소에 정수 <paramref name="b"/>를 곱하여, 결과를 <paramref name="a"/>에 할당한 후 반환합니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 벡터를 직접 수정합니다.
        /// </summary>
        /// <param name="a">수정될 Vector2Int (참조 전달).</param>
        /// <param name="b">곱할 정수 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소에 <paramref name="b"/>를 곱한 결과로 업데이트된 Vector2Int를 반환합니다.</returns>
        public static Vector2Int MultiplyRef(this ref Vector2Int a, int b)
        {
            return a = new Vector2Int(a.x * b, a.y * b);
        }



        /// <summary>
        /// 두 개의 Vector3 인스턴스의 각 구성 요소를 나누어, 새로운 Vector3를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다. <br/>
        /// 주의: 각 구성 요소를 나누므로, <paramref name="b"/>의 0인 구성 요소는 예외를 발생시킬 수 있습니다.
        /// </summary>
        /// <param name="a">첫 번째 Vector3.</param>
        /// <param name="b">두 번째 Vector3.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>의 해당 구성 요소로 나눈 결과를 포함하는 새로운 Vector3를 반환합니다.</returns>
        public static Vector3 Divide(this Vector3 a, Vector3 b)
        {
            return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
        }

        /// <summary>
        /// Vector3의 각 구성 요소를 스칼라 <paramref name="b"/>로 나누어, 새로운 Vector3를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다. <br/>
        /// 주의: <paramref name="b"/>가 0이면 예외가 발생할 수 있습니다.
        /// </summary>
        /// <param name="target">입력 Vector3.</param>
        /// <param name="b">나눌 스칼라 값.</param>
        /// <returns><paramref name="target"/>의 각 구성 요소를 <paramref name="b"/>로 나눈 결과를 포함하는 새로운 Vector3를 반환합니다.</returns>
        public static Vector3 Divide(this Vector3 target, float b)
        {
            return new Vector3(target.x / b, target.y / b, target.z / b);
        }

        /// <summary>
        /// 두 개의 Vector3Int 인스턴스의 각 구성 요소를 나누어, 새로운 Vector3Int를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다. <br/>
        /// 주의: 각 구성 요소를 나누므로, <paramref name="b"/>의 0인 구성 요소는 예외를 발생시킬 수 있습니다.
        /// </summary>
        /// <param name="a">첫 번째 Vector3Int.</param>
        /// <param name="b">두 번째 Vector3Int.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>의 해당 구성 요소로 나눈 결과를 포함하는 새로운 Vector3Int를 반환합니다.</returns>
        public static Vector3Int Divide(this Vector3Int a, Vector3Int b)
        {
            return new Vector3Int(a.x / b.x, a.y / b.y, a.z / b.z);
        }

        /// <summary>
        /// Vector3Int의 각 구성 요소를 정수 <paramref name="b"/>로 나누어, 새로운 Vector3Int를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다. <br/>
        /// 주의: <paramref name="b"/>가 0이면 예외가 발생할 수 있습니다.
        /// </summary>
        /// <param name="target">입력 Vector3Int.</param>
        /// <param name="b">나눌 정수 값.</param>
        /// <returns><paramref name="target"/>의 각 구성 요소를 <paramref name="b"/>로 나눈 결과를 포함하는 새로운 Vector3Int를 반환합니다.</returns>
        public static Vector3Int Divide(this Vector3Int target, int b)
        {
            return new Vector3Int(target.x / b, target.y / b, target.z / b);
        }

        /// <summary>
        /// 두 개의 Vector2 인스턴스의 각 구성 요소를 나누어, 새로운 Vector2를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다. <br/>
        /// 주의: 각 구성 요소를 나누므로, <paramref name="b"/>의 0인 구성 요소는 예외를 발생시킬 수 있습니다.
        /// </summary>
        /// <param name="a">첫 번째 Vector2.</param>
        /// <param name="b">두 번째 Vector2.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>의 해당 구성 요소로 나눈 결과를 포함하는 새로운 Vector2를 반환합니다.</returns>
        public static Vector2 Divide(this Vector2 a, Vector2 b)
        {
            return new Vector2(a.x / b.x, a.y / b.y);
        }

        /// <summary>
        /// Vector2의 각 구성 요소를 스칼라 <paramref name="b"/>로 나누어, 새로운 Vector2를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다. <br/>
        /// 주의: <paramref name="b"/>가 0이면 예외가 발생할 수 있습니다.
        /// </summary>
        /// <param name="target">입력 Vector2.</param>
        /// <param name="b">나눌 스칼라 값.</param>
        /// <returns><paramref name="target"/>의 각 구성 요소를 <paramref name="b"/>로 나눈 결과를 포함하는 새로운 Vector2를 반환합니다.</returns>
        public static Vector2 Divide(this Vector2 target, float b)
        {
            return new Vector2(target.x / b, target.y / b);
        }

        /// <summary>
        /// 두 개의 Vector2Int 인스턴스의 각 구성 요소를 나누어, 새로운 Vector2Int를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다. <br/>
        /// 주의: 각 구성 요소를 나누므로, <paramref name="b"/>의 0인 구성 요소는 예외를 발생시킬 수 있습니다.
        /// </summary>
        /// <param name="a">첫 번째 Vector2Int.</param>
        /// <param name="b">두 번째 Vector2Int.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>의 해당 구성 요소로 나눈 결과를 포함하는 새로운 Vector2Int를 반환합니다.</returns>
        public static Vector2Int Divide(this Vector2Int a, Vector2Int b)
        {
            return new Vector2Int(a.x / b.x, a.y / b.y);
        }

        /// <summary>
        /// Vector2Int의 각 구성 요소를 정수 <paramref name="b"/>로 나누어, 새로운 Vector2Int를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다. <br/>
        /// 주의: <paramref name="b"/>가 0이면 예외가 발생할 수 있습니다.
        /// </summary>
        /// <param name="a">입력 Vector2Int.</param>
        /// <param name="b">나눌 정수 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>로 나눈 결과를 포함하는 새로운 Vector2Int를 반환합니다.</returns>
        public static Vector2Int Divide(this Vector2Int a, int b)
        {
            return new Vector2Int(a.x / b, a.y / b);
        }



        /// <summary>
        /// 두 개의 Vector3 인스턴스의 각 구성 요소를 나눕니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 벡터를 <paramref name="b"/>의 각 구성 요소로 나눈 결과로 업데이트한 후 반환합니다. <br/>
        /// 각 구성 요소를 나누므로, <paramref name="b"/>의 0인 구성 요소는 예외를 발생시킬 수 있습니다.
        /// </summary>
        /// <param name="a">수정될 Vector3 (참조 전달).</param>
        /// <param name="b">나눌 Vector3.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>의 해당 구성 요소로 나눈 결과를 포함하는 Vector3를 반환합니다.</returns>
        public static Vector3 DivideRef(this ref Vector3 a, Vector3 b)
        {
            return a = new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
        }

        /// <summary>
        /// Vector3의 각 구성 요소를 스칼라 <paramref name="b"/>로 나눕니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 벡터를 스칼라 값으로 나눈 결과로 업데이트한 후 반환합니다. <br/>
        /// <paramref name="b"/>가 0이면 예외가 발생할 수 있습니다.
        /// </summary>
        /// <param name="a">수정될 Vector3 (참조 전달).</param>
        /// <param name="b">나눌 스칼라 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>로 나눈 결과를 포함하는 Vector3를 반환합니다.</returns>
        public static Vector3 DivideRef(this ref Vector3 a, float b)
        {
            return a = new Vector3(a.x / b, a.y / b, a.z / b);
        }

        /// <summary>
        /// 두 개의 Vector3Int 인스턴스의 각 구성 요소를 나눕니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 Vector3Int를 <paramref name="b"/>의 각 구성 요소로 나눈 결과로 업데이트한 후 반환합니다. <br/>
        /// 각 구성 요소를 나누므로, <paramref name="b"/>의 0인 구성 요소는 예외를 발생시킬 수 있습니다.
        /// </summary>
        /// <param name="a">수정될 Vector3Int (참조 전달).</param>
        /// <param name="b">나눌 Vector3Int.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>의 해당 구성 요소로 나눈 결과를 포함하는 Vector3Int를 반환합니다.</returns>
        public static Vector3Int DivideRef(this ref Vector3Int a, Vector3Int b)
        {
            return a = new Vector3Int(a.x / b.x, a.y / b.y, a.z / b.z);
        }

        /// <summary>
        /// Vector3Int의 각 구성 요소를 정수 <paramref name="b"/>로 나눕니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 Vector3Int를 스칼라 값으로 나눈 결과로 업데이트한 후 반환합니다. <br/>
        /// <paramref name="b"/>가 0이면 예외가 발생할 수 있습니다.
        /// </summary>
        /// <param name="a">수정될 Vector3Int (참조 전달).</param>
        /// <param name="b">나눌 정수 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>로 나눈 결과를 포함하는 Vector3Int를 반환합니다.</returns>
        public static Vector3Int DivideRef(this ref Vector3Int a, int b)
        {
            return a = new Vector3Int(a.x / b, a.y / b, a.z / b);
        }

        /// <summary>
        /// 두 개의 Vector2 인스턴스의 각 구성 요소를 나눕니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 Vector2를 <paramref name="b"/>의 각 구성 요소로 나눈 결과로 업데이트한 후 반환합니다. <br/>
        /// 각 구성 요소를 나누므로, <paramref name="b"/>의 0인 구성 요소는 예외를 발생시킬 수 있습니다.
        /// </summary>
        /// <param name="a">수정될 Vector2 (참조 전달).</param>
        /// <param name="b">나눌 Vector2.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>의 해당 구성 요소로 나눈 결과를 포함하는 Vector2를 반환합니다.</returns>
        public static Vector2 DivideRef(this ref Vector2 a, Vector2 b)
        {
            return a = new Vector2(a.x / b.x, a.y / b.y);
        }

        /// <summary>
        /// Vector2의 각 구성 요소를 스칼라 <paramref name="b"/>로 나눕니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 Vector2를 스칼라 값으로 나눈 결과로 업데이트한 후 반환합니다. <br/>
        /// <paramref name="b"/>가 0이면 예외가 발생할 수 있습니다.
        /// </summary>
        /// <param name="a">수정될 Vector2 (참조 전달).</param>
        /// <param name="b">나눌 스칼라 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>로 나눈 결과를 포함하는 Vector2를 반환합니다.</returns>
        public static Vector2 DivideRef(this ref Vector2 a, float b)
        {
            return a = new Vector2(a.x / b, a.y / b);
        }

        /// <summary>
        /// 두 개의 Vector2Int 인스턴스의 각 구성 요소를 나눕니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 Vector2Int를 <paramref name="b"/>의 각 구성 요소로 나눈 결과로 업데이트한 후 반환합니다. <br/>
        /// 각 구성 요소를 나누므로, <paramref name="b"/>의 0인 구성 요소는 예외를 발생시킬 수 있습니다.
        /// </summary>
        /// <param name="a">수정될 Vector2Int (참조 전달).</param>
        /// <param name="b">나눌 Vector2Int.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>의 해당 구성 요소로 나눈 결과를 포함하는 Vector2Int를 반환합니다.</returns>
        public static Vector2Int DivideRef(this ref Vector2Int a, Vector2Int b)
        {
            return a = new Vector2Int(a.x / b.x, a.y / b.y);
        }

        /// <summary>
        /// Vector2Int의 각 구성 요소를 정수 <paramref name="b"/>로 나눕니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 Vector2Int를 스칼라 값으로 나눈 결과로 업데이트한 후 반환합니다. <br/>
        /// <paramref name="b"/>가 0이면 예외를 발생시킬 수 있습니다.
        /// </summary>
        /// <param name="a">수정될 Vector2Int (참조 전달).</param>
        /// <param name="b">나눌 정수 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>로 나눈 결과를 포함하는 Vector2Int를 반환합니다.</returns>
        public static Vector2Int DivideRef(this ref Vector2Int a, int b)
        {
            return a = new Vector2Int(a.x / b, a.y / b);
        }



        #endregion




        #region 벡터 곱하기/나누기 (버스트 최적화)



        /// <summary>
        /// 두 개의 float3 인스턴스의 각 구성 요소를 곱하여, 새로운 float3를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다.
        /// </summary>
        /// <param name="a">첫 번째 float3.</param>
        /// <param name="b">두 번째 float3.</param>
        /// <returns><paramref name="a"/>와 <paramref name="b"/>의 각 구성 요소 곱셈 결과를 포함하는 새로운 float3를 반환합니다.</returns>
        public static float3 Multiply(this float3 a, float3 b)
        {
            return new float3(a.x * b.x, a.y * b.y, a.z * b.z);
        }

        /// <summary>
        /// float3의 각 구성 요소에 스칼라 <paramref name="b"/>를 곱하여, 새로운 float3를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다.
        /// </summary>
        /// <param name="a">입력 float3.</param>
        /// <param name="b">곱할 스칼라 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소에 <paramref name="b"/>를 곱한 결과를 포함하는 새로운 float3를 반환합니다.</returns>
        public static float3 Multiply(this float3 a, float b)
        {
            return new float3(a.x * b, a.y * b, a.z * b);
        }

        /// <summary>
        /// 두 개의 int3 인스턴스의 각 구성 요소를 곱하여, 새로운 int3를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다.
        /// </summary>
        /// <param name="a">첫 번째 int3.</param>
        /// <param name="b">두 번째 int3.</param>
        /// <returns><paramref name="a"/>와 <paramref name="b"/>의 각 구성 요소 곱셈 결과를 포함하는 새로운 int3를 반환합니다.</returns>
        public static int3 Multiply(this int3 a, int3 b)
        {
            return new int3(a.x * b.x, a.y * b.y, a.z * b.z);
        }

        /// <summary>
        /// int3의 각 구성 요소에 정수 <paramref name="b"/>를 곱하여, 새로운 int3를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다.
        /// </summary>
        /// <param name="a">입력 int3.</param>
        /// <param name="b">곱할 정수 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소에 <paramref name="b"/>를 곱한 결과를 포함하는 새로운 int3를 반환합니다.</returns>
        public static int3 Multiply(this int3 a, int b)
        {
            return new int3(a.x * b, a.y * b, a.z * b);
        }

        /// <summary>
        /// 두 개의 float2 인스턴스의 각 구성 요소를 곱하여, 새로운 float2를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다.
        /// </summary>
        /// <param name="a">첫 번째 float2.</param>
        /// <param name="b">두 번째 float2.</param>
        /// <returns><paramref name="a"/>와 <paramref name="b"/>의 각 구성 요소 곱셈 결과를 포함하는 새로운 float2를 반환합니다.</returns>
        public static float2 Multiply(this float2 a, float2 b)
        {
            return new float2(a.x * b.x, a.y * b.y);
        }

        /// <summary>
        /// float2의 각 구성 요소에 스칼라 <paramref name="b"/>를 곱하여, 새로운 float2를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다.
        /// </summary>
        /// <param name="a">입력 float2.</param>
        /// <param name="b">곱할 스칼라 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소에 <paramref name="b"/>를 곱한 결과를 포함하는 새로운 float2를 반환합니다.</returns>
        public static float2 Multiply(this float2 a, float b)
        {
            return new float2(a.x * b, a.y * b);
        }

        /// <summary>
        /// 두 개의 int2 인스턴스의 각 구성 요소를 곱하여, 새로운 int2를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다.
        /// </summary>
        /// <param name="a">첫 번째 int2.</param>
        /// <param name="b">두 번째 int2.</param>
        /// <returns><paramref name="a"/>와 <paramref name="b"/>의 각 구성 요소 곱셈 결과를 포함하는 새로운 int2를 반환합니다.</returns>
        public static int2 Multiply(this int2 a, int2 b)
        {
            return new int2(a.x * b.x, a.y * b.y);
        }

        /// <summary>
        /// int2의 각 구성 요소에 정수 <paramref name="b"/>를 곱하여, 새로운 int2를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다.
        /// </summary>
        /// <param name="a">입력 int2.</param>
        /// <param name="b">곱할 정수 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소에 <paramref name="b"/>를 곱한 결과를 포함하는 새로운 int2를 반환합니다.</returns>
        public static int2 Multiply(this int2 a, int b)
        {
            return new int2(a.x * b, a.y * b);
        }



        /// <summary>
        /// 두 개의 float3 인스턴스의 각 구성 요소를 곱하여, 결과를 <paramref name="a"/>에 할당한 후 반환합니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 벡터를 직접 수정합니다.
        /// </summary>
        /// <param name="a">수정될 float3 (참조 전달). 각 구성 요소가 수정됩니다.</param>
        /// <param name="b">곱할 float3.</param>
        /// <returns><paramref name="a"/>와 <paramref name="b"/>의 각 구성 요소 곱셈 결과로 업데이트된 float3를 반환합니다.</returns>
        public static float3 MultiplyRef(this ref float3 a, float3 b)
        {
            return a = new float3(a.x * b.x, a.y * b.y, a.z * b.z);
        }

        /// <summary>
        /// float3의 각 구성 요소에 스칼라 <paramref name="b"/>를 곱하여, 결과를 <paramref name="a"/>에 할당한 후 반환합니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 벡터를 직접 수정합니다.
        /// </summary>
        /// <param name="a">수정될 float3 (참조 전달).</param>
        /// <param name="b">곱할 스칼라 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소에 <paramref name="b"/>를 곱한 결과로 업데이트된 float3를 반환합니다.</returns>
        public static float3 MultiplyRef(this ref float3 a, float b)
        {
            return a = new float3(a.x * b, a.y * b, a.z * b);
        }

        /// <summary>
        /// 두 개의 int3 인스턴스의 각 구성 요소를 곱하여, 결과를 <paramref name="a"/>에 할당한 후 반환합니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 벡터를 직접 수정합니다.
        /// </summary>
        /// <param name="a">수정될 int3 (참조 전달).</param>
        /// <param name="b">곱할 int3.</param>
        /// <returns><paramref name="a"/>와 <paramref name="b"/>의 각 구성 요소 곱셈 결과로 업데이트된 int3를 반환합니다.</returns>
        public static int3 MultiplyRef(this ref int3 a, int3 b)
        {
            return a = new int3(a.x * b.x, a.y * b.y, a.z * b.z);
        }

        /// <summary>
        /// int3의 각 구성 요소에 정수 <paramref name="b"/>를 곱하여, 결과를 <paramref name="a"/>에 할당한 후 반환합니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 벡터를 직접 수정합니다.
        /// </summary>
        /// <param name="a">수정될 int3 (참조 전달).</param>
        /// <param name="b">곱할 정수 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소에 <paramref name="b"/>를 곱한 결과로 업데이트된 int3를 반환합니다.</returns>
        public static int3 MultiplyRef(this ref int3 a, int b)
        {
            return a = new int3(a.x * b, a.y * b, a.z * b);
        }

        /// <summary>
        /// 두 개의 float2 인스턴스의 각 구성 요소를 곱하여, 결과를 <paramref name="a"/>에 할당한 후 반환합니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 벡터를 직접 수정합니다.
        /// </summary>
        /// <param name="a">수정될 float2 (참조 전달).</param>
        /// <param name="b">곱할 float2.</param>
        /// <returns><paramref name="a"/>와 <paramref name="b"/>의 각 구성 요소 곱셈 결과로 업데이트된 float2를 반환합니다.</returns>
        public static float2 MultiplyRef(this ref float2 a, float2 b)
        {
            return a = new float2(a.x * b.x, a.y * b.y);
        }

        /// <summary>
        /// float2의 각 구성 요소에 스칼라 <paramref name="b"/>를 곱하여, 결과를 <paramref name="a"/>에 할당한 후 반환합니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 벡터를 직접 수정합니다.
        /// </summary>
        /// <param name="a">수정될 float2 (참조 전달).</param>
        /// <param name="b">곱할 스칼라 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소에 <paramref name="b"/>를 곱한 결과로 업데이트된 float2를 반환합니다.</returns>
        public static float2 MultiplyRef(this ref float2 a, float b)
        {
            return a = new float2(a.x * b, a.y * b);
        }

        /// <summary>
        /// 두 개의 int2 인스턴스의 각 구성 요소를 곱하여, 결과를 <paramref name="a"/>에 할당한 후 반환합니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 벡터를 직접 수정합니다.
        /// </summary>
        /// <param name="a">수정될 int2 (참조 전달).</param>
        /// <param name="b">곱할 int2.</param>
        /// <returns><paramref name="a"/>와 <paramref name="b"/>의 각 구성 요소 곱셈 결과로 업데이트된 int2를 반환합니다.</returns>
        public static int2 MultiplyRef(this ref int2 a, int2 b)
        {
            return a = new int2(a.x * b.x, a.y * b.y);
        }

        /// <summary>
        /// int2의 각 구성 요소에 정수 <paramref name="b"/>를 곱하여, 결과를 <paramref name="a"/>에 할당한 후 반환합니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 벡터를 직접 수정합니다.
        /// </summary>
        /// <param name="a">수정될 int2 (참조 전달).</param>
        /// <param name="b">곱할 정수 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소에 <paramref name="b"/>를 곱한 결과로 업데이트된 int2를 반환합니다.</returns>
        public static int2 MultiplyRef(this ref int2 a, int b)
        {
            return a = new int2(a.x * b, a.y * b);
        }



        /// <summary>
        /// 두 개의 float3 인스턴스의 각 구성 요소를 나누어, 새로운 float3를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다. <br/>
        /// 주의: 각 구성 요소를 나누므로, <paramref name="b"/>의 0인 구성 요소는 예외를 발생시킬 수 있습니다.
        /// </summary>
        /// <param name="a">첫 번째 float3.</param>
        /// <param name="b">두 번째 float3.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>의 해당 구성 요소로 나눈 결과를 포함하는 새로운 float3를 반환합니다.</returns>
        public static float3 Divide(this float3 a, float3 b)
        {
            return new float3(a.x / b.x, a.y / b.y, a.z / b.z);
        }

        /// <summary>
        /// float3의 각 구성 요소를 스칼라 <paramref name="b"/>로 나누어, 새로운 float3를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다. <br/>
        /// 주의: <paramref name="b"/>가 0이면 예외가 발생할 수 있습니다.
        /// </summary>
        /// <param name="target">입력 float3.</param>
        /// <param name="b">나눌 스칼라 값.</param>
        /// <returns><paramref name="target"/>의 각 구성 요소를 <paramref name="b"/>로 나눈 결과를 포함하는 새로운 float3를 반환합니다.</returns>
        public static float3 Divide(this float3 target, float b)
        {
            return new float3(target.x / b, target.y / b, target.z / b);
        }

        /// <summary>
        /// 두 개의 int3 인스턴스의 각 구성 요소를 나누어, 새로운 int3를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다. <br/>
        /// 주의: 각 구성 요소를 나누므로, <paramref name="b"/>의 0인 구성 요소는 예외를 발생시킬 수 있습니다.
        /// </summary>
        /// <param name="a">첫 번째 int3.</param>
        /// <param name="b">두 번째 int3.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>의 해당 구성 요소로 나눈 결과를 포함하는 새로운 int3를 반환합니다.</returns>
        public static int3 Divide(this int3 a, int3 b)
        {
            return new int3(a.x / b.x, a.y / b.y, a.z / b.z);
        }

        /// <summary>
        /// int3의 각 구성 요소를 정수 <paramref name="b"/>로 나누어, 새로운 int3를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다. <br/>
        /// 주의: <paramref name="b"/>가 0이면 예외가 발생할 수 있습니다.
        /// </summary>
        /// <param name="target">입력 int3.</param>
        /// <param name="b">나눌 정수 값.</param>
        /// <returns><paramref name="target"/>의 각 구성 요소를 <paramref name="b"/>로 나눈 결과를 포함하는 새로운 int3를 반환합니다.</returns>
        public static int3 Divide(this int3 target, int b)
        {
            return new int3(target.x / b, target.y / b, target.z / b);
        }

        /// <summary>
        /// 두 개의 float2 인스턴스의 각 구성 요소를 나누어, 새로운 float2를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다. <br/>
        /// 주의: 각 구성 요소를 나누므로, <paramref name="b"/>의 0인 구성 요소는 예외를 발생시킬 수 있습니다.
        /// </summary>
        /// <param name="a">첫 번째 float2.</param>
        /// <param name="b">두 번째 float2.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>의 해당 구성 요소로 나눈 결과를 포함하는 새로운 float2를 반환합니다.</returns>
        public static float2 Divide(this float2 a, float2 b)
        {
            return new float2(a.x / b.x, a.y / b.y);
        }

        /// <summary>
        /// float2의 각 구성 요소를 스칼라 <paramref name="b"/>로 나누어, 새로운 float2를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다. <br/>
        /// 주의: <paramref name="b"/>가 0이면 예외가 발생할 수 있습니다.
        /// </summary>
        /// <param name="target">입력 float2.</param>
        /// <param name="b">나눌 스칼라 값.</param>
        /// <returns><paramref name="target"/>의 각 구성 요소를 <paramref name="b"/>로 나눈 결과를 포함하는 새로운 float2를 반환합니다.</returns>
        public static float2 Divide(this float2 target, float b)
        {
            return new float2(target.x / b, target.y / b);
        }

        /// <summary>
        /// 두 개의 int2 인스턴스의 각 구성 요소를 나누어, 새로운 int2를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다. <br/>
        /// 주의: 각 구성 요소를 나누므로, <paramref name="b"/>의 0인 구성 요소는 예외를 발생시킬 수 있습니다.
        /// </summary>
        /// <param name="a">첫 번째 int2.</param>
        /// <param name="b">두 번째 int2.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>의 해당 구성 요소로 나눈 결과를 포함하는 새로운 int2를 반환합니다.</returns>
        public static int2 Divide(this int2 a, int2 b)
        {
            return new int2(a.x / b.x, a.y / b.y);
        }

        /// <summary>
        /// int2의 각 구성 요소를 정수 <paramref name="b"/>로 나누어, 새로운 int2를 반환합니다. <br/>
        /// 원본 벡터는 수정되지 않습니다. <br/>
        /// 주의: <paramref name="b"/>가 0이면 예외가 발생할 수 있습니다.
        /// </summary>
        /// <param name="a">입력 int2.</param>
        /// <param name="b">나눌 정수 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>로 나눈 결과를 포함하는 새로운 int2를 반환합니다.</returns>
        public static int2 Divide(this int2 a, int b)
        {
            return new int2(a.x / b, a.y / b);
        }



        /// <summary>
        /// 두 개의 float3 인스턴스의 각 구성 요소를 나눕니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 벡터를 <paramref name="b"/>의 각 구성 요소로 나눈 결과로 업데이트한 후 반환합니다. <br/>
        /// 각 구성 요소를 나누므로, <paramref name="b"/>의 0인 구성 요소는 예외를 발생시킬 수 있습니다.
        /// </summary>
        /// <param name="a">수정될 float3 (참조 전달).</param>
        /// <param name="b">나눌 float3.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>의 해당 구성 요소로 나눈 결과를 포함하는 float3를 반환합니다.</returns>
        public static float3 DivideRef(this ref float3 a, float3 b)
        {
            return a = new float3(a.x / b.x, a.y / b.y, a.z / b.z);
        }

        /// <summary>
        /// float3의 각 구성 요소를 스칼라 <paramref name="b"/>로 나눕니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 벡터를 스칼라 값으로 나눈 결과로 업데이트한 후 반환합니다. <br/>
        /// <paramref name="b"/>가 0이면 예외가 발생할 수 있습니다.
        /// </summary>
        /// <param name="a">수정될 float3 (참조 전달).</param>
        /// <param name="b">나눌 스칼라 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>로 나눈 결과를 포함하는 float3를 반환합니다.</returns>
        public static float3 DivideRef(this ref float3 a, float b)
        {
            return a = new float3(a.x / b, a.y / b, a.z / b);
        }

        /// <summary>
        /// 두 개의 int3 인스턴스의 각 구성 요소를 나눕니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 int3를 <paramref name="b"/>의 각 구성 요소로 나눈 결과로 업데이트한 후 반환합니다. <br/>
        /// 각 구성 요소를 나누므로, <paramref name="b"/>의 0인 구성 요소는 예외를 발생시킬 수 있습니다.
        /// </summary>
        /// <param name="a">수정될 int3 (참조 전달).</param>
        /// <param name="b">나눌 int3.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>의 해당 구성 요소로 나눈 결과를 포함하는 int3를 반환합니다.</returns>
        public static int3 DivideRef(this ref int3 a, int3 b)
        {
            return a = new int3(a.x / b.x, a.y / b.y, a.z / b.z);
        }

        /// <summary>
        /// int3의 각 구성 요소를 정수 <paramref name="b"/>로 나눕니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 int3를 스칼라 값으로 나눈 결과로 업데이트한 후 반환합니다. <br/>
        /// <paramref name="b"/>가 0이면 예외가 발생할 수 있습니다.
        /// </summary>
        /// <param name="a">수정될 int3 (참조 전달).</param>
        /// <param name="b">나눌 정수 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>로 나눈 결과를 포함하는 int3를 반환합니다.</returns>
        public static int3 DivideRef(this ref int3 a, int b)
        {
            return a = new int3(a.x / b, a.y / b, a.z / b);
        }

        /// <summary>
        /// 두 개의 float2 인스턴스의 각 구성 요소를 나눕니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 float2를 <paramref name="b"/>의 각 구성 요소로 나눈 결과로 업데이트한 후 반환합니다. <br/>
        /// 각 구성 요소를 나누므로, <paramref name="b"/>의 0인 구성 요소는 예외를 발생시킬 수 있습니다.
        /// </summary>
        /// <param name="a">수정될 float2 (참조 전달).</param>
        /// <param name="b">나눌 float2.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>의 해당 구성 요소로 나눈 결과를 포함하는 float2를 반환합니다.</returns>
        public static float2 DivideRef(this ref float2 a, float2 b)
        {
            return a = new float2(a.x / b.x, a.y / b.y);
        }

        /// <summary>
        /// float2의 각 구성 요소를 스칼라 <paramref name="b"/>로 나눕니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 float2를 스칼라 값으로 나눈 결과로 업데이트한 후 반환합니다. <br/>
        /// <paramref name="b"/>가 0이면 예외가 발생할 수 있습니다.
        /// </summary>
        /// <param name="a">수정될 float2 (참조 전달).</param>
        /// <param name="b">나눌 스칼라 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>로 나눈 결과를 포함하는 float2를 반환합니다.</returns>
        public static float2 DivideRef(this ref float2 a, float b)
        {
            return a = new float2(a.x / b, a.y / b);
        }

        /// <summary>
        /// 두 개의 int2 인스턴스의 각 구성 요소를 나눕니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 int2를 <paramref name="b"/>의 각 구성 요소로 나눈 결과로 업데이트한 후 반환합니다. <br/>
        /// 각 구성 요소를 나누므로, <paramref name="b"/>의 0인 구성 요소는 예외를 발생시킬 수 있습니다.
        /// </summary>
        /// <param name="a">수정될 int2 (참조 전달).</param>
        /// <param name="b">나눌 int2.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>의 해당 구성 요소로 나눈 결과를 포함하는 int2를 반환합니다.</returns>
        public static int2 DivideRef(this ref int2 a, int2 b)
        {
            return a = new int2(a.x / b.x, a.y / b.y);
        }

        /// <summary>
        /// int2의 각 구성 요소를 정수 <paramref name="b"/>로 나눕니다. <br/>
        /// 이 메서드는 <paramref name="a"/>를 참조로 전달받아, 원본 int2를 스칼라 값으로 나눈 결과로 업데이트한 후 반환합니다. <br/>
        /// <paramref name="b"/>가 0이면 예외를 발생시킬 수 있습니다.
        /// </summary>
        /// <param name="a">수정될 int2 (참조 전달).</param>
        /// <param name="b">나눌 정수 값.</param>
        /// <returns><paramref name="a"/>의 각 구성 요소를 <paramref name="b"/>로 나눈 결과를 포함하는 int2를 반환합니다.</returns>
        public static int2 DivideRef(this ref int2 a, int b)
        {
            return a = new int2(a.x / b, a.y / b);
        }



        #endregion



        ///======================================================================================================================================================



        //? 벡터 배열



        /// <summary>
        /// Vector2Int 배열과 인덱스를 받아 해당 인덱스의 방향을 계산합니다.
        /// </summary>
        /// <param name="vectors">Vector2Int 배열</param>
        /// <param name="index">계산할 인덱스</param>
        /// <returns>계산된 방향</returns>
        public static EDirection8n Get8DirectionFromVector2Int(this IList<Vector2Int> vectors, int index)
        {
            if (index < 0 || index >= vectors.Count)
            {
                return EDirection8n.None;
            }

            Vector2Int current = vectors[index];
            Vector2Int reference;

            if (index == vectors.Count - 1)
            {
                // 마지막 인덱스인 경우 이전 인덱스를 참조
                reference = vectors[index - 1];
            }
            else
            {
                // 그렇지 않으면 다음 인덱스를 참조
                reference = vectors[index + 1];
            }

            Vector2Int direction = reference - current;

            if (direction.x == 0 && direction.y > 0)
            {
                return EDirection8n.Up;
            }
            else if (direction.x == 0 && direction.y < 0)
            {
                return EDirection8n.Down;
            }
            else if (direction.x > 0 && direction.y == 0)
            {
                return EDirection8n.Right;
            }
            else if (direction.x < 0 && direction.y == 0)
            {
                return EDirection8n.Left;
            }
            else if (direction.x < 0 && direction.y < 0)
            {
                return EDirection8n.LeftDown;
            }
            else if (direction.x < 0 && direction.y > 0)
            {
                return EDirection8n.LeftUp;
            }
            else if (direction.x > 0 && direction.y < 0)
            {
                return EDirection8n.RightDown;
            }
            else if (direction.x > 0 && direction.y > 0)
            {
                return EDirection8n.RightUp;
            }

            return EDirection8n.None;
        }



        /// <summary>
        /// Vector2Int 배열과 인덱스를 받아 해당 인덱스의 방향을 계산합니다.
        /// 방향을 성공적으로 계산하면 true를 반환하고, 그렇지 않으면 false를 반환합니다.
        /// </summary>
        /// <param name="vectors">Vector2Int 배열</param>
        /// <param name="index">계산할 인덱스</param>
        /// <param name="direction">계산된 방향</param>
        /// <returns>계산 성공 여부</returns>
        public static bool TryGet8DirectionFromVector2Int(this IList<Vector2Int> vectors, int index, out EDirection8n direction)
        {
            direction = Get8DirectionFromVector2Int(vectors, index);
            return direction != EDirection8n.None;
        }



        /// <summary>
        /// Vector2Int 배열과 인덱스를 받아 해당 인덱스의 4방향을 계산합니다.
        /// </summary>
        /// <param name="vectors">Vector2Int 배열</param>
        /// <param name="index">계산할 인덱스</param>
        /// <returns>계산된 방향</returns>
        public static EDirection4 Get4DirectionFromVector2Int(this IList<Vector2Int> vectors, int index)
        {
            if (index < 0 || index >= vectors.Count)
            {
                throw new System.ArgumentOutOfRangeException(nameof(index), "인덱스가 범위를 벗어났습니다.");
            }

            Vector2Int current = vectors[index];
            Vector2Int reference;

            if (index == vectors.Count - 1)
            {
                // 마지막 인덱스인 경우 이전 인덱스를 참조
                reference = vectors[index - 1];
            }
            else
            {
                // 그렇지 않으면 다음 인덱스를 참조
                reference = vectors[index + 1];
            }

            Vector2Int direction = reference - current;

            if (direction.x == 0 && direction.y > 0)
            {
                return EDirection4.Up;
            }
            else if (direction.x == 0 && direction.y < 0)
            {
                return EDirection4.Down;
            }
            else if (direction.x > 0 && direction.y == 0)
            {
                return EDirection4.Right;
            }
            else if (direction.x < 0 && direction.y == 0)
            {
                return EDirection4.Left;
            }

            throw new System.InvalidOperationException("유효하지 않은 방향입니다.");
        }



        /// <summary>
        /// Vector2Int 배열과 인덱스를 받아 해당 인덱스의 4방향을 계산합니다.
        /// 방향을 성공적으로 계산하면 true를 반환하고, 그렇지 않으면 false를 반환합니다.
        /// </summary>
        /// <param name="vectors">Vector2Int 배열</param>
        /// <param name="index">계산할 인덱스</param>
        /// <param name="direction">계산된 방향</param>
        /// <returns>계산 성공 여부</returns>
        public static bool TryGet4DirectionFromVector2Int(this IList<Vector2Int> vectors, int index, out EDirection4 direction)
        {
            direction = default;

            if (index < 0 || index >= vectors.Count)
            {
                return false;
            }

            Vector2Int current = vectors[index];
            Vector2Int reference;

            if (index == vectors.Count - 1)
            {
                // 마지막 인덱스인 경우 이전 인덱스를 참조
                reference = vectors[index - 1];
            }
            else
            {
                // 그렇지 않으면 다음 인덱스를 참조
                reference = vectors[index + 1];
            }

            Vector2Int directionVector = reference - current;

            if (directionVector.x == 0 && directionVector.y > 0)
            {
                direction = EDirection4.Up;
            }
            else if (directionVector.x == 0 && directionVector.y < 0)
            {
                direction = EDirection4.Down;
            }
            else if (directionVector.x > 0 && directionVector.y == 0)
            {
                direction = EDirection4.Right;
            }
            else if (directionVector.x < 0 && directionVector.y == 0)
            {
                direction = EDirection4.Left;
            }
            else
            {
                return false;
            }

            return true;
        }



        /// <summary>
        /// Vector2Int 배열과 인덱스를 받아 경로가 꺾여 있는지 확인합니다.
        /// </summary>
        /// <param name="vectors">Vector2Int 배열</param>
        /// <param name="index">계산할 인덱스</param>
        /// <returns>경로가 꺾여 있으면 true, 그렇지 않으면 false</returns>
        public static bool IsPathTurning(this IList<Vector2Int> vectors, int index)
        {
            if (index <= 0 || index >= vectors.Count - 1)
            {
                return false; // 첫 번째나 마지막 인덱스인 경우 false 반환
            }

            Vector2Int previous = vectors[index - 1];
            Vector2Int current = vectors[index];
            Vector2Int next = vectors[index + 1];

            Vector2Int direction1 = current - previous;
            Vector2Int direction2 = next - current;

            // 두 방향이 직선인지 확인
            bool isStraightLine = (direction1.x == 0 && direction2.x == 0) || (direction1.y == 0 && direction2.y == 0);

            return !isStraightLine;
        }



        //? 벡터 배열 (버스트 최적화)



        /// <summary>
        /// int2 배열과 인덱스를 받아 해당 인덱스의 방향을 계산합니다.
        /// </summary>
        /// <param name="path">int2 배열</param>
        /// <param name="index">계산할 인덱스</param>
        /// <returns>계산된 방향</returns>
        public static EDirection8n Get8DirectionFromInt2(this IList<int2> path, int index)
        {
            if (index < 0 || index >= path.Count)
            {
                return EDirection8n.None;
            }

            int2 current = path[index];
            int2 reference;

            if (index == path.Count - 1)
            {
                // 마지막 인덱스인 경우 이전 인덱스를 참조
                reference = path[index - 1];
            }
            else
            {
                // 그렇지 않으면 다음 인덱스를 참조
                reference = path[index + 1];
            }

            int2 direction = reference - current;

            if (direction.x == 0 && direction.y > 0)
            {
                return EDirection8n.Up;
            }
            else if (direction.x == 0 && direction.y < 0)
            {
                return EDirection8n.Down;
            }
            else if (direction.x > 0 && direction.y == 0)
            {
                return EDirection8n.Right;
            }
            else if (direction.x < 0 && direction.y == 0)
            {
                return EDirection8n.Left;
            }
            else if (direction.x < 0 && direction.y < 0)
            {
                return EDirection8n.LeftDown;
            }
            else if (direction.x < 0 && direction.y > 0)
            {
                return EDirection8n.LeftUp;
            }
            else if (direction.x > 0 && direction.y < 0)
            {
                return EDirection8n.RightDown;
            }
            else if (direction.x > 0 && direction.y > 0)
            {
                return EDirection8n.RightUp;
            }

            return EDirection8n.None;
        }



        /// <summary>
        /// int2 배열과 인덱스를 받아 해당 인덱스의 방향을 계산합니다.
        /// 방향을 성공적으로 계산하면 true를 반환하고, 그렇지 않으면 false를 반환합니다.
        /// </summary>
        /// <param name="path">int2 배열</param>
        /// <param name="index">계산할 인덱스</param>
        /// <param name="direction">계산된 방향</param>
        /// <returns>계산 성공 여부</returns>
        public static bool TryGet8DirectionFromInt2(this IList<int2> path, int index, out EDirection8n direction)
        {
            direction = Get8DirectionFromInt2(path, index);
            return direction != EDirection8n.None;
        }



        /// <summary>
        /// int2 배열과 인덱스를 받아 해당 인덱스의 4방향을 계산합니다.
        /// </summary>
        /// <param name="path">int2 배열</param>
        /// <param name="index">계산할 인덱스</param>
        /// <returns>계산된 방향</returns>
        public static EDirection4 Get4DirectionFromInt2(this IList<int2> path, int index)
        {
            if (index < 0 || index >= path.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "인덱스가 범위를 벗어났습니다.");
            }

            int2 current = path[index];
            int2 reference;

            if (index == path.Count - 1)
            {
                // 마지막 인덱스인 경우 이전 인덱스를 참조
                reference = path[index - 1];
            }
            else
            {
                // 그렇지 않으면 다음 인덱스를 참조
                reference = path[index + 1];
            }

            int2 direction = reference - current;

            if (direction.x == 0 && direction.y > 0)
            {
                return EDirection4.Up;
            }
            else if (direction.x == 0 && direction.y < 0)
            {
                return EDirection4.Down;
            }
            else if (direction.x > 0 && direction.y == 0)
            {
                return EDirection4.Right;
            }
            else if (direction.x < 0 && direction.y == 0)
            {
                return EDirection4.Left;
            }

            throw new InvalidOperationException("유효하지 않은 방향입니다.");
        }



        /// <summary>
        /// int2 배열과 인덱스를 받아 해당 인덱스의 4방향을 계산합니다.
        /// 방향을 성공적으로 계산하면 true를 반환하고, 그렇지 않으면 false를 반환합니다.
        /// </summary>
        /// <param name="path">int2 배열</param>
        /// <param name="index">계산할 인덱스</param>
        /// <param name="direction">계산된 방향</param>
        /// <returns>계산 성공 여부</returns>
        public static bool TryGet4DirectionFromInt2(this IList<int2> path, int index, out EDirection4 direction)
        {
            direction = default;

            if (index < 0 || index >= path.Count)
            {
                return false;
            }

            int2 current = path[index];
            int2 reference;

            if (index == path.Count - 1)
            {
                // 마지막 인덱스인 경우 이전 인덱스를 참조
                reference = path[index - 1];
            }
            else
            {
                // 그렇지 않으면 다음 인덱스를 참조
                reference = path[index + 1];
            }

            int2 directionVector = reference - current;

            if (directionVector.x == 0 && directionVector.y > 0)
            {
                direction = EDirection4.Up;
            }
            else if (directionVector.x == 0 && directionVector.y < 0)
            {
                direction = EDirection4.Down;
            }
            else if (directionVector.x > 0 && directionVector.y == 0)
            {
                direction = EDirection4.Right;
            }
            else if (directionVector.x < 0 && directionVector.y == 0)
            {
                direction = EDirection4.Left;
            }
            else
            {
                return false;
            }

            return true;
        }



        /// <summary>
        /// int2 배열과 인덱스를 받아 경로가 꺾여 있는지 확인합니다.
        /// </summary>
        /// <param name="path">int2 배열</param>
        /// <param name="index">계산할 인덱스</param>
        /// <returns>경로가 꺾여 있으면 true, 그렇지 않으면 false</returns>
        public static bool IsPathTurning(this IList<int2> path, int index)
        {
            if (index <= 0 || index >= path.Count - 1)
            {
                return false; //! 첫 번째나 마지막 인덱스인 경우 false 반환
            }

            int2 previous = path[index - 1];
            int2 current = path[index];
            int2 next = path[index + 1];

            int2 direction1 = current - previous;
            int2 direction2 = next - current;

            //? 두 방향이 직선인지 확인
            bool isStraightLine = (direction1.x == 0 && direction2.x == 0) || (direction1.y == 0 && direction2.y == 0);

            return !isStraightLine;
        }



        ///======================================================================================================================================================



        //? 크기 벡터 균일 조정



        /// <summary>
        /// <para>모든 면에 <paramref name="delta"/> 만큼 **동일 간격**을 적용합니다.</para>
        /// <para>‣ <b>delta &gt; 0</b> → 외곽으로 확장<br/>
        /// ‣ <b>delta &lt; 0</b> → 안쪽으로 축소</para>
        /// </summary>
        /// <param name="size">원본 2D 크기</param>
        /// <param name="delta">픽셀·유닛 등 절대 거리</param>
        public static Vector2 OffsetUniform(this Vector2 size, float delta)
        {
            //! 내부 축소 시 음수 크기 방지
            float w = Mathf.Max(0f, size.x + delta * 2f);   //? 좌·우 2×delta
            float h = Mathf.Max(0f, size.y + delta * 2f);   //? 상·하 2×delta
            return new Vector2(w, h);
        }



        /// <inheritdoc cref="OffsetUniform(Vector2,float)"/>
        public static Vector3 OffsetUniform(this Vector3 size, float delta)
        {
            float x = Mathf.Max(0f, size.x + delta * 2f);   //. 앞·뒤 포함 2×delta
            float y = Mathf.Max(0f, size.y + delta * 2f);
            float z = Mathf.Max(0f, size.z + delta * 2f);
            return new Vector3(x, y, z);
        }



        /// <summary>
        /// <para>가장 짧은 변 기준으로 <paramref name="ratio"/> 비율만큼 **동일 간격**을 적용합니다.</para>
        /// <para>‣ <b>ratio &gt; 0</b> → 외곽으로 확장<br/>
        /// ‣ <b>ratio &lt; 0</b> → 안쪽으로 축소</para>
        /// </summary>
        public static Vector2 OffsetUniformRatio(this Vector2 size, float ratio)
        {
            //. 짧은 변 × 비율 = 실간격
            float delta = Mathf.Min(size.x, size.y) * ratio;
            return size.OffsetUniform(delta);
        }



        /// <inheritdoc cref="OffsetUniformRatio(Vector2,float)"/>
        public static Vector3 OffsetUniformRatio(this Vector3 size, float ratio)
        {
            float delta = Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * ratio;
            return size.OffsetUniform(delta);
        }



        ///======================================================================================================================================================



        //? 벡터 리스트 비교



        /// <summary>
        /// 두 리스트에서 조건을 만족하는 (A,B) 쌍 가운데 가장 가까운 한 쌍을 찾습니다.
        /// </summary>
        /// <typeparam name="TA">리스트 A의 요소 타입</typeparam>
        /// <typeparam name="TB">리스트 B의 요소 타입</typeparam>
        /// <param name="listA">리스트 A</param>
        /// <param name="listB">리스트 B</param>
        /// <param name="getPosA">A 요소 → 위치 벡터</param>
        /// <param name="getPosB">B 요소 → 위치 벡터</param>
        /// <param name="condition">
        /// 쌍 (a,b)이 유효한지 여부. null 이면 모든 쌍 허용.
        /// </param>
        /// <param name="maxDistance">
        /// 탐색할 최대 거리(미터). float.PositiveInfinity 면 무제한.
        /// </param>
        /// <param name="bestA">가장 가까운 A 요소</param>
        /// <param name="bestB">가장 가까운 B 요소</param>
        /// <param name="bestDistance">최단 거리(미터)</param>
        /// <returns>찾았으면 true, 없으면 false</returns>
        public static bool TryFindClosestPair<TA, TB>
        (
            IReadOnlyList<TA> listA,
            IReadOnlyList<TB> listB,
            Func<TA, Vector3> getPosA,
            Func<TB, Vector3> getPosB,
            Func<TA, TB, bool> condition,
            float maxDistance,
            out TA bestA,
            out TB bestB,
            out float bestDistance
        )
        {
            bestA = default;
            bestB = default;
            bestDistance = float.PositiveInfinity;

            //! 둘 중 하나라도 비어있다면 실패
            if (listA == null || listB == null || listA.Count == 0 || listB.Count == 0)
                return false;

            //. 비교는 제곱거리로 (sqrt 비용 회피)
            float bestSqr = float.PositiveInfinity;
            float cutoffSqr = float.IsPositiveInfinity(maxDistance) ? float.PositiveInfinity : maxDistance * maxDistance;

            for (int i = 0; i < listA.Count; i++)
            {
                var a = listA[i];
                Vector3 pa = getPosA(a); //. A 위치 캐시

                for (int j = 0; j < listB.Count; j++)
                {
                    var b = listB[j];

                    //. 조건 미충족이면 스킵
                    if (condition != null && !condition(a, b)) { continue; } //? 필터링

                    Vector3 pb = getPosB(b);
                    float d2 = (pa - pb).sqrMagnitude;

                    //! 컷오프 밖이면 스킵
                    if (d2 > cutoffSqr) { continue; }

                    if (d2 < bestSqr)
                    {
                        bestSqr = d2;
                        bestA = a;
                        bestB = b;

                        //. 완전 동일 좌표면 더 볼 필요 없음
                        if (bestSqr <= 1e-12f) { bestDistance = 0f; return true; }
                    }
                }
            }

            if (float.IsPositiveInfinity(bestSqr))
                return false;

            bestDistance = Mathf.Sqrt(bestSqr);
            return true;
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================



    //? 인터페이스



    /// <summary>
    /// 오브젝트의 Y축 위치 고정을 관리하는 인터페이스입니다.
    /// </summary>
    public interface IFreezePosition_Y
    {
        /// <summary>
        /// Y축 위치를 고정하거나 해제합니다.
        /// </summary>
        /// <param name="value">
        /// <c>true</c>이면 Y축 위치를 고정하고, <c>false</c>이면 고정을 해제합니다.
        /// </param>
        /// <param name="positionY">
        /// 고정할 Y축의 위치 값. <paramref name="value"/>가 <c>true</c>일 때 적용됩니다.
        /// </param>
        void SetFreezePosition_Y(bool value, float positionY);
    }



    ///======================================================================================================================================================



    //? 벡터 치수



    /// <summary>
    /// 2D 치수를 나타내는 열거형입니다.
    /// </summary>
    public enum EDimension2D
    {
        /// <summary>너비를 나타냅니다.</summary>
        Width,
        /// <summary>높이를 나타냅니다.</summary>
        Height
    }



    /// <summary>
    /// 3D 치수를 나타내는 열거형입니다.
    /// </summary>
    public enum EDimension3D
    {
        /// <summary>너비를 나타냅니다.</summary>
        Width,
        /// <summary>높이를 나타냅니다.</summary>
        Height,
        /// <summary>깊이를 나타냅니다.</summary>
        Depth
    }



    ///======================================================================================================================================================



    //? 좌표 중심



    /// <summary>
    /// 2D 평면에서 X 또는 Y 축을 나타내는 열거형입니다.
    /// </summary>
    public enum Exy
    {
        /// <summary>X 축을 나타냅니다.</summary>
        X,
        /// <summary>Y 축을 나타냅니다.</summary>
        Y
    }



    /// <summary>
    /// 3D 공간에서의 축을 나타내는 열거형입니다.
    /// </summary>
    public enum EAxis
    {
        /// <summary>X 축을 나타냅니다.</summary>
        X,
        /// <summary>Y 축을 나타냅니다.</summary>
        Y,
        /// <summary>Z 축을 나타냅니다.</summary>
        Z
    }



    /// <summary>
    /// 2축 조합을 나타내는 열거형입니다.
    /// </summary>
    public enum EAxes
    {
        /// <summary>XY 축 조합을 나타냅니다.</summary>
        XY,
        /// <summary>YZ 축 조합을 나타냅니다.</summary>
        YZ,
        /// <summary>XZ 축 조합을 나타냅니다.</summary>
        XZ
    }



    ///======================================================================================================================================================



    //? 좌표 정렬




    /// <summary>
    /// 중심 정렬 기준을 나타내는 열거형입니다.
    /// </summary>
    public enum ECenterStandard
    {
        /// <summary>중앙 정렬입니다.</summary>
        MiddleCenter,
        /// <summary>중앙 왼쪽 정렬입니다.</summary>
        MiddleLeft,
        /// <summary>중앙 오른쪽 정렬입니다.</summary>
        MiddleRight,
        /// <summary>하단 중앙 정렬입니다.</summary>
        LowerCenter,
        /// <summary>하단 왼쪽 정렬입니다.</summary>
        LowerLeft,
        /// <summary>하단 오른쪽 정렬입니다.</summary>
        LowerRight,
        /// <summary>상단 중앙 정렬입니다.</summary>
        UpperCenter,
        /// <summary>상단 왼쪽 정렬입니다.</summary>
        UpperLeft,
        /// <summary>상단 오른쪽 정렬입니다.</summary>
        UpperRight
    }



    /// <summary>
    /// 깊이 정렬 기준을 나타내는 열거형입니다.
    /// </summary>
    public enum EDepthStandard
    {
        Middle,
        Bottom,
        Top,
    }



    public static class ECenterStandardExtensions
    {
        /// <summary>
        /// 열거형이 왼쪽 정렬 계열(MiddleLeft, LowerLeft, UpperLeft)인지 확인합니다.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLeft(this ECenterStandard v)
            => v is ECenterStandard.MiddleLeft
                or ECenterStandard.LowerLeft
                or ECenterStandard.UpperLeft;

        /// <summary>
        /// 열거형이 오른쪽 정렬 계열(MiddleRight, LowerRight, UpperRight)인지 확인합니다.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRight(this ECenterStandard v)
            => v is ECenterStandard.MiddleRight
                or ECenterStandard.LowerRight
                or ECenterStandard.UpperRight;

        /// <summary>
        /// 열거형이 하단 정렬 계열(LowerLeft, LowerCenter, LowerRight)인지 확인합니다.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLower(this ECenterStandard v)
            => v is ECenterStandard.LowerLeft
                or ECenterStandard.LowerCenter
                or ECenterStandard.LowerRight;

        /// <summary>
        /// 열거형이 상단 정렬 계열(UpperLeft, UpperCenter, UpperRight)인지 확인합니다.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsUpper(this ECenterStandard v)
            => v is ECenterStandard.UpperLeft
                or ECenterStandard.UpperCenter
                or ECenterStandard.UpperRight;

        /// <summary>
        /// 열거형이 중앙 열(센터 열) 계열(MiddleCenter, LowerCenter, UpperCenter)인지 확인합니다.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCenter(this ECenterStandard v)
            => v is ECenterStandard.MiddleCenter
                or ECenterStandard.LowerCenter
                or ECenterStandard.UpperCenter;
    }



    ///======================================================================================================================================================



    //? 좌표 관련 인터페이스



    /// <summary>
    /// 중심 위치를 제공하는 인터페이스입니다.
    /// </summary>
    public interface ICenterPosition
    {
        /// <summary>
        /// 월드 좌표계에서의 중심 위치입니다.
        /// </summary>
        Vector3 CenterPosition { get; }



        /// <summary>
        /// 로컬 좌표계에서의 중심 위치입니다.
        /// </summary>
        Vector3 CenterLocalPosition { get; }
    }



    ///======================================================================================================================================================
}