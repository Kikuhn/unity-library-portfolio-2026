using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.IO;
using UnityEngine.Events;
using SitraUtils;
using DG.Tweening;
using Pan.Util;



//? [Physics] 물리 관련이 저장되어있는 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    public static class SU_Phy2D
    {
        ///======================================================================================================================================================



        #region 기즈모 메서드 (에디터에서만 동작함)



        private static void Gizmo_RayCast(Vector2 position, Vector2 dir, float distance, int layer = 0)
        {
#if UNITY_EDITOR
            //? 250331 다른 프로젝트에서 안불러와져서 비활성화
            //. SU_Layers.GetColor_ByCollisionLayers는 환무소 전처리기에서만 실행됨
            //Editors.SU_DebugDraw.DrawDebugRay(position, dir * distance, SU_Layers.GetColor_ByCollisionLayers(layer));
#endif
        }



        private static void Gizmo_LineCast(Vector2 position, Vector2 end, int layer = 0)
        {
#if UNITY_EDITOR
            //? 250331 다른 프로젝트에서 안불러와져서 비활성화
            //. SU_Layers.GetColor_ByCollisionLayers는 환무소 전처리기에서만 실행됨
            //Editors.SU_DebugDraw.DrawDebugLine(position, end, SU_Layers.GetColor_ByCollisionLayers(layer));
#endif
        }



        private static void Gizmo_Area(Vector2 pointA, Vector2 pointB, int layer = 0)
        {
#if UNITY_EDITOR


            //? 250331 다른 프로젝트에서 안불러와져서 비활성화
            //. SU_Layers.GetColor_ByCollisionLayers는 환무소 전처리기에서만 실행됨
            //Editors.SU_DebugDraw.DrawDebugRectangle(pointA, pointB, SU_Layers.GetColor_ByCollisionLayers(layer));

#endif
        }



        private static void Gizmo_CircleCast(Vector2 position, float radius, Vector3 dir, float distance, int layer = 0)
        {
#if UNITY_EDITOR
            //? 250331 다른 프로젝트에서 안불러와져서 비활성화
            //. SU_Layers.GetColor_ByCollisionLayers는 환무소 전처리기에서만 실행됨
            //Editors.SU_DebugDraw.DrawDebugCircleCast(position, radius, dir, distance, SU_Layers.GetColor_ByCollisionLayers(layer));
#endif
        }



        private static void Gizmo_Circle(Vector2 position, float radius, int layer = 0)
        {
#if UNITY_EDITOR
            //? 250331 다른 프로젝트에서 안불러와져서 비활성화
            //. SU_Layers.GetColor_ByCollisionLayers는 환무소 전처리기에서만 실행됨
            //Editors.SU_DebugDraw.DrawDebugCircle(position, radius, SU_Layers.GetColor_ByCollisionLayers(layer));
#endif
        }



        private static void Gizmo_BoxCast(Vector3 position, Vector2 boxSize, float angle, Vector3 dir, float distance, int layer = 0)
        {
#if UNITY_EDITOR
            //? 250331 다른 프로젝트에서 안불러와져서 비활성화
            //. SU_Layers.GetColor_ByCollisionLayers는 환무소 전처리기에서만 실행됨
            //Editors.SU_DebugDraw.DrawDebugBoxCast(position, boxSize, angle, dir, distance, SU_Layers.GetColor_ByCollisionLayers(layer));
#endif
        }



        private static void Gizmo_Box(Vector3 position, Vector2 boxSize, float angle, int layer = 0)
        {
#if UNITY_EDITOR
            //? 250331 다른 프로젝트에서 안불러와져서 비활성화
            //. SU_Layers.GetColor_ByCollisionLayers는 환무소 전처리기에서만 실행됨
            //Editors.SU_DebugDraw.DrawDebugRotatedBox(position, boxSize, angle, SU_Layers.GetColor_ByCollisionLayers(layer));
#endif
        }



        #endregion



        ///======================================================================================================================================================



        //? 레이캐스트


        public static bool RayCast(Vector2 origin, Vector2 dir, float distance, int layerMask, out RaycastHit2D hitInfo)
        {
            hitInfo = Physics2D.Raycast(origin, dir, distance, layerMask);

#if UNITY_EDITOR
            Gizmo_RayCast(origin, dir, distance, layerMask);
#endif

            return hitInfo.collider != null;
        }



        public static bool RayCastAll(Vector2 origin, Vector2 dir, float distance, int layerMask, out RaycastHit2D[] hitInfo)
        {
            hitInfo = Physics2D.RaycastAll(origin, dir, distance, layerMask);

#if UNITY_EDITOR
            Gizmo_RayCast(origin, dir, distance, layerMask);
#endif

            return hitInfo.Length != 0;
        }



        public static int RayCastNonAlloc(Vector2 origin, Vector2 dir, float distance, int layerMask, RaycastHit2D[] targetHitInfo)
        {
#if UNITY_EDITOR
            Gizmo_RayCast(origin, dir, distance, layerMask);
#endif

            return Physics2D.RaycastNonAlloc(origin, dir, targetHitInfo, distance, layerMask);
        }



        ///======================================================================================================================================================


        //? 라인캐스트



        public static bool LineCast(Vector2 start, Vector2 end, int layerMask, out RaycastHit2D hitInfo)
        {
            hitInfo = Physics2D.Linecast(start, end, layerMask);

#if UNITY_EDITOR
            Gizmo_LineCast(start, end, layerMask);
#endif

            return hitInfo.collider != null;
        }



        public static bool LineCastAll(Vector2 start, Vector2 end, int layerMask, out RaycastHit2D[] hitInfo)
        {
            hitInfo = Physics2D.LinecastAll(start, end, layerMask);

#if UNITY_EDITOR
            Gizmo_LineCast(start, end, layerMask);
#endif

            return hitInfo.Length != 0;
        }



        public static int LineCast(Vector2 start, Vector2 end, int layerMask, RaycastHit2D[] targetHitInfo)
        {
#if UNITY_EDITOR
            Gizmo_LineCast(start, end, layerMask);
#endif

            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.SetLayerMask(layerMask);
            contactFilter.useTriggers = false; // 트리거를 무시하려면 false로 설정

            return Physics2D.Linecast(start, end, contactFilter, targetHitInfo);

            //return Physics2D.LinecastNonAlloc(start, end, targetHitInfo, layerMask);
        }



        ///======================================================================================================================================================



        //? 박스캐스트



        public static bool BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 dir, float distance, int layerMask, out RaycastHit2D hitInfo)
        {
            hitInfo = Physics2D.BoxCast(origin, size, angle, dir, distance, layerMask);

#if UNITY_EDITOR
            Gizmo_BoxCast(origin, size, angle, dir, distance, layerMask);
#endif

            return hitInfo.collider != null;
        }



        public static bool BoxCastAll(Vector2 origin, Vector2 size, float angle, Vector2 dir, float distance, int layerMask, out RaycastHit2D[] hitInfo)
        {
            hitInfo = Physics2D.BoxCastAll(origin, size, angle, dir, distance, layerMask);

#if UNITY_EDITOR
            Gizmo_BoxCast(origin, size, angle, dir, distance, layerMask);
#endif

            return hitInfo.Length != 0;
        }



        public static int BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, int layerMask, RaycastHit2D[] results)
        {
#if UNITY_EDITOR
            Gizmo_BoxCast(origin, size, angle, direction, distance, layerMask);
#endif

            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.SetLayerMask(layerMask);
            contactFilter.useTriggers = false; // 트리거 콜라이더를 무시하려면 false로 설정

            return Physics2D.BoxCast(origin, size, angle, direction, contactFilter, results, distance);

            //return Physics2D.BoxCastNonAlloc(origin, size, angle, dir, targetHitInfo, distance);
        }



        //? 박스캐스트 제자리



        public static bool BoxCast(Vector2 origin, Vector2 size, int layerMask, out RaycastHit2D hitInfo)
        {
#if UNITY_EDITOR
            Gizmo_Box(origin, size, 0, layerMask);
#endif

            return BoxCast(origin, size, 0, Vector2.zero, 0, layerMask, out hitInfo);
        }



        public static bool BoxCastAll(Vector2 origin, Vector2 size, int layerMask, out RaycastHit2D[] hitInfo)
        {
#if UNITY_EDITOR
            Gizmo_Box(origin, size, 0, layerMask);
#endif

            return BoxCastAll(origin, size, 0, Vector2.zero, 0, layerMask, out hitInfo);
        }



        public static int BoxCast(Vector2 origin, Vector2 size, int layerMask, RaycastHit2D[] hitInfo)
        {
#if UNITY_EDITOR
            Gizmo_Box(origin, size, 0, layerMask);
#endif

            return BoxCast(origin, size, 0, Vector2.zero, 0, layerMask, hitInfo);
        }



        ///======================================================================================================================================================



        //? 서클캐스트



        public static bool CircleCast(Vector2 origin, float radius, Vector2 dir, float distance, int layerMask, out RaycastHit2D hitInfo)
        {
            hitInfo = Physics2D.CircleCast(origin, radius, dir, distance, layerMask);

#if UNITY_EDITOR
            Gizmo_CircleCast(origin, radius, dir, distance, layerMask);
#endif

            return hitInfo.collider != null;
        }



        public static bool CircleCastAll(Vector2 origin, float radius, Vector2 dir, float distance, int layerMask, out RaycastHit2D[] hitInfo)
        {
            hitInfo = Physics2D.CircleCastAll(origin, radius, dir, distance, layerMask);

#if UNITY_EDITOR
            Gizmo_CircleCast(origin, radius, dir, distance, layerMask);
#endif

            return hitInfo.Length != 0;
        }



        public static int CircleCast(Vector2 origin, float radius, Vector2 direction, float distance, int layerMask, RaycastHit2D[] results)
        {
#if UNITY_EDITOR
            Gizmo_CircleCast(origin, radius, direction, distance, layerMask);
#endif

            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.SetLayerMask(layerMask);
            contactFilter.useTriggers = false; // 트리거 콜라이더를 무시하려면 false로 설정

            return Physics2D.CircleCast(origin, radius, direction, contactFilter, results, distance);

            //return Physics2D.CircleCastNonAlloc(origin, radius, dir, targetHitInfo, distance, layerMask);
        }



        //? 서클캐스트 제자리



        public static bool CircleCast(Vector2 origin, float radius, int layerMask, out RaycastHit2D hitInfo)
        {
#if UNITY_EDITOR
            Gizmo_Circle(origin, radius, layerMask);
#endif

            return CircleCast(origin, radius, Vector2.zero, 0, layerMask, out hitInfo);
        }



        public static bool CircleCastAll(Vector2 origin, float radius, int layerMask, out RaycastHit2D[] hitInfo)
        {
#if UNITY_EDITOR
            Gizmo_Circle(origin, radius, layerMask);
#endif

            return CircleCastAll(origin, radius, Vector2.zero, 0, layerMask, out hitInfo);
        }



        public static int CircleCast(Vector2 origin, float radius, int layerMask, RaycastHit2D[] targetHitInfo)
        {
#if UNITY_EDITOR
            Gizmo_Circle(origin, radius, layerMask);
#endif

            return CircleCast(origin, radius, Vector2.zero, 0, layerMask, targetHitInfo);
        }



        ///======================================================================================================================================================



        //? 오버랩 박스



        public static bool OverlapBox(Vector2 point, Vector2 size, float angle, int layerMask, out Collider2D colliderInfo)
        {
            colliderInfo = Physics2D.OverlapBox(point, size, angle, layerMask);

#if UNITY_EDITOR
            Gizmo_Box(point, size, angle, layerMask);
#endif

            return colliderInfo;
        }



        public static bool OverlapBoxAll(Vector2 point, Vector2 size, float angle, int layerMask, out Collider2D[] colliderInfo)
        {
            colliderInfo = Physics2D.OverlapBoxAll(point, size, angle, layerMask);

#if UNITY_EDITOR
            Gizmo_Box(point, size, angle, layerMask);
#endif

            return colliderInfo.Length != 0;
        }



        public static int OverlapBox(Vector2 point, Vector2 size, float angle, int layerMask, Collider2D[] results)
        {
#if UNITY_EDITOR
            Gizmo_Box(point, size, angle, layerMask);
#endif

            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.SetLayerMask(layerMask);
            contactFilter.useTriggers = false; // 트리거 콜라이더를 무시하려면 false로 설정

            return Physics2D.OverlapBox(point, size, angle, contactFilter, results);

            //return Physics2D.OverlapBoxNonAlloc(point, size, angle, targetColliderInfo, layerMask);
        }



        //? 오버랩 박스 각도 X



        public static bool OverlapBox(Vector2 point, Vector2 size, int layerMask, out Collider2D colliderInfo)
        {
#if UNITY_EDITOR
            Gizmo_Box(point, size, 0, layerMask);
#endif
            return OverlapBox(point, size, 0, layerMask, out colliderInfo);
        }



        public static bool OverlapBoxAll(Vector2 point, Vector2 size, int layerMask, out Collider2D[] colliderInfo)
        {
#if UNITY_EDITOR
            Gizmo_Box(point, size, 0, layerMask);
#endif
            return OverlapBoxAll(point, size, 0, layerMask, out colliderInfo);
        }



        public static int OverlapBox(Vector2 point, Vector2 size, int layerMask, Collider2D[] targetColliderInfo)
        {
#if UNITY_EDITOR
            Gizmo_Box(point, size, 0, layerMask);
#endif
            return OverlapBox(point, size, 0, layerMask, targetColliderInfo);
        }



        ///======================================================================================================================================================



        //? 오버랩 포인트



        public static bool OverlapPoint(Vector2 point, int layerMask, out Collider2D colliderInfo)
        {
            colliderInfo = Physics2D.OverlapPoint(point, layerMask);

            return colliderInfo;
        }



        public static bool OverlapPointAll(Vector2 point, int layerMask, out Collider2D[] colliderInfo)
        {
            colliderInfo = Physics2D.OverlapPointAll(point, layerMask);

            return colliderInfo.Length != 0;
        }



        public static int OverlapPoint(Vector2 point, int layerMask, Collider2D[] results)
        {
            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.SetLayerMask(layerMask);
            contactFilter.useTriggers = false; // 트리거 콜라이더를 무시하려면 false로 설정

            return Physics2D.OverlapPoint(point, contactFilter, results);

            //return Physics2D.OverlapPointNonAlloc(point, targetColliderInfo, layerMask);
        }



        ///======================================================================================================================================================



        //? 오버랩 서클



        public static bool OverlapCircle(Vector2 point, float radius, int layerMask, out Collider2D colliderInfo)
        {
            colliderInfo = Physics2D.OverlapCircle(point, radius, layerMask);

#if UNITY_EDITOR
            Gizmo_Circle(point, radius, layerMask);
#endif

            return colliderInfo;
        }



        public static bool OverlapCircleAll(Vector2 point, float radius, int layerMask, out Collider2D[] colliderInfo)
        {
            colliderInfo = Physics2D.OverlapCircleAll(point, radius, layerMask);

#if UNITY_EDITOR
            Gizmo_Circle(point, radius, layerMask);
#endif

            return colliderInfo.Length != 0;
        }



        public static int OverlapCircle(Vector2 point, float radius, int layerMask, Collider2D[] results)
        {
#if UNITY_EDITOR
            Gizmo_Circle(point, radius, layerMask);
#endif

            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.SetLayerMask(layerMask);
            contactFilter.useTriggers = false; // 트리거 콜라이더를 무시하려면 false로 설정

            return Physics2D.OverlapCircle(point, radius, contactFilter, results);

            //return Physics2D.OverlapCircleNonAlloc(point, radius, targetColliderInfo, layerMask);
        }



        ///======================================================================================================================================================



        //? 오버렙 에리아



        public static bool OverlapArea(Vector2 pointA, Vector2 pointB, int layerMask, out Collider2D colliderInfo)
        {
            colliderInfo = Physics2D.OverlapArea(pointA, pointB, layerMask);

#if UNITY_EDITOR
            Gizmo_Area(pointA, pointB, layerMask);
#endif

            return colliderInfo;
        }



        public static bool OverlapAreaAll(Vector2 pointA, Vector2 pointB, int layerMask, out Collider2D[] colliderInfo)
        {
            colliderInfo = Physics2D.OverlapAreaAll(pointA, pointB, layerMask);

#if UNITY_EDITOR
            Gizmo_Area(pointA, pointB, layerMask);
#endif

            return colliderInfo.Length != 0;
        }



        public static int OverlapArea(Vector2 pointA, Vector2 pointB, int layerMask, Collider2D[] results)
        {
#if UNITY_EDITOR
            Gizmo_Area(pointA, pointB, layerMask);
#endif

            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.SetLayerMask(layerMask);
            contactFilter.useTriggers = false; // 트리거 콜라이더를 무시하려면 false로 설정

            return Physics2D.OverlapArea(pointA, pointB, contactFilter, results);
            //return Physics2D.OverlapAreaNonAlloc(pointA, pointB, targetColliderInfo, layerMask);
        }



        ///======================================================================================================================================================



        //? 오버랩 콜라이더 (쓰긴할까?)

        public static int OverlapCollider(Collider2D collider, ContactFilter2D contactFilter2D, Collider2D[] targetColliderInfo)
        {
            return Physics2D.OverlapCollider(collider, contactFilter2D, targetColliderInfo);
        }



        public static int OverlapCollider(Collider2D collider, ContactFilter2D contactFilter2D, List<Collider2D> targetColliderInfo)
        {
            return Physics2D.OverlapCollider(collider, contactFilter2D, targetColliderInfo);
        }



        ///======================================================================================================================================================
    }



    public static class SU_Phy3D
    {
        ///======================================================================================================================================================



        #region 기즈모 메서드 (에디터에서만 동작함)



        private static void Gizmo_RayCast(Vector3 position, Vector3 dir, float distance, int layer = 0)
        {

#if UNITY_EDITOR
            //? 250331 다른 프로젝트에서 안불러와져서 비활성화
            //. SU_Layers.GetColor_ByCollisionLayers는 환무소 전처리기에서만 실행됨
            //Editors.SU_DebugDraw.DrawDebugRay(position, dir * distance, SU_Layers.GetColor_ByCollisionLayers(layer));
#endif

        }



        private static void Gizmo_LineCast(Vector3 position, Vector3 end, int layer = 0)
        {

#if UNITY_EDITOR
            //? 250331 다른 프로젝트에서 안불러와져서 비활성화
            //. SU_Layers.GetColor_ByCollisionLayers는 환무소 전처리기에서만 실행됨
            //Editors.SU_DebugDraw.DrawDebugLine(position, end, SU_Layers.GetColor_ByCollisionLayers(layer));
#endif

        }



        private static void Gizmo_Area(Vector3 pointA, Vector3 pointB, int layer = 0)
        {

#if UNITY_EDITOR


            //? 250331 다른 프로젝트에서 안불러와져서 비활성화
            //. SU_Layers.GetColor_ByCollisionLayers는 환무소 전처리기에서만 실행됨
            //Editors.SU_DebugDraw.DrawDebugRectangle(pointA, pointB, SU_Layers.GetColor_ByCollisionLayers(layer));

#endif

        }



        private static void Gizmo_CircleCast(Vector3 position, float radius, Vector3 dir, float distance, int layer = 0)
        {

#if UNITY_EDITOR
            //? 250331 다른 프로젝트에서 안불러와져서 비활성화
            //. SU_Layers.GetColor_ByCollisionLayers는 환무소 전처리기에서만 실행됨
            //Editors.SU_DebugDraw.DrawDebugCircleCast(position, radius, dir, distance, SU_Layers.GetColor_ByCollisionLayers(layer));
#endif

        }



        private static void Gizmo_Circle(Vector3 position, float radius, int layer = 0)
        {

#if UNITY_EDITOR
            //? 250331 다른 프로젝트에서 안불러와져서 비활성화
            //. SU_Layers.GetColor_ByCollisionLayers는 환무소 전처리기에서만 실행됨
            //Editors.SU_DebugDraw.DrawDebugCircle(position, radius, SU_Layers.GetColor_ByCollisionLayers(layer));
#endif

        }



        private static void Gizmo_BoxCast(Vector3 position, Vector3 boxSize, float angle, Vector3 dir, float distance, int layer = 0)
        {

#if UNITY_EDITOR
            //? 250331 다른 프로젝트에서 안불러와져서 비활성화
            //. SU_Layers.GetColor_ByCollisionLayers는 환무소 전처리기에서만 실행됨
            //Editors.SU_DebugDraw.DrawDebugBoxCast(position, boxSize, angle, dir, distance, SU_Layers.GetColor_ByCollisionLayers(layer));
#endif

        }



        private static void Gizmo_Box(Vector3 position, Vector3 boxSize, float angle, int layer = 0)
        {

#if UNITY_EDITOR
            //? 250331 다른 프로젝트에서 안불러와져서 비활성화
            //. SU_Layers.GetColor_ByCollisionLayers는 환무소 전처리기에서만 실행됨
            //Editors.SU_DebugDraw.DrawDebugRotatedBox(position, boxSize, angle, SU_Layers.GetColor_ByCollisionLayers(layer));
#endif

        }



        #endregion



        ///======================================================================================================================================================



        //? 레이캐스트



        public static bool RayCast(Vector3 origin, Vector3 dir, float maxDistance, int layerMask, out RaycastHit hitInfo)
        {
            Physics.Raycast(origin, dir, out hitInfo, maxDistance, layerMask);

#if UNITY_EDITOR
            Gizmo_RayCast(origin, dir, maxDistance, layerMask);
#endif

            return hitInfo.collider != null;
        }



        public static bool RayCastAll(Vector3 origin, Vector3 dir, float maxDistance, int layerMask, out RaycastHit[] hitInfo)
        {
            hitInfo = Physics.RaycastAll(origin, dir, maxDistance, layerMask);

#if UNITY_EDITOR
            Gizmo_RayCast(origin, dir, maxDistance, layerMask);
#endif

            return hitInfo.Length != 0;
        }



        public static int RayCastNonAlloc(Vector3 origin, Vector3 dir, float maxDistance, int layerMask, RaycastHit[] targetHitInfo)
        {
#if UNITY_EDITOR
            Gizmo_RayCast(origin, dir, maxDistance, layerMask);
#endif

            return Physics.RaycastNonAlloc(origin, dir, targetHitInfo, maxDistance, layerMask);
        }



        ///======================================================================================================================================================



        //? 라인캐스트



        public static bool LineCast(Vector3 start, Vector3 end, int layerMask, out RaycastHit hitInfo)
        {
            Physics.Linecast(start, end, out hitInfo, layerMask);

            Gizmo_LineCast(start, end, layerMask);

            return hitInfo.collider != null;
        }



        ///======================================================================================================================================================



        //? 박스캐스트



        public static bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 dir, Quaternion orientation, float maxDistance, int layerMask, out RaycastHit hitInfo)
        {
            Physics.BoxCast(center, halfExtents, dir, out hitInfo, orientation, maxDistance, layerMask);

#if UNITY_EDITOR
            //Gizmo_BoxCast(center, halfExtents, angle, dir, orientation, layerMask);
#endif

            return hitInfo.collider != null;
        }



        public static bool BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 dir, Quaternion orientation, float maxDistance, int layerMask, out RaycastHit[] hitInfo)
        {
            hitInfo = Physics.BoxCastAll(center, halfExtents, dir, orientation, maxDistance, layerMask);

#if UNITY_EDITOR
            //Gizmo_BoxCast(origin, size, angle, dir, distance, layerMask);
#endif

            return hitInfo.Length != 0;
        }



        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 dir, Quaternion orientation, float maxDistance, int layerMask, RaycastHit[] targetHitInfo)
        {
#if UNITY_EDITOR
            //Gizmo_BoxCast(origin, size, angle, dir, distance, layerMask);
#endif

            return Physics.BoxCastNonAlloc(center, halfExtents, dir, targetHitInfo, orientation, maxDistance, layerMask);
        }



        ///======================================================================================================================================================



        //? 박스캐스트 제자리



        public static bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 dir, float maxDistance, int layerMask, out RaycastHit hitInfo)
        {
#if UNITY_EDITOR
            //Gizmo_Box(origin, size, 0, layerMask);
#endif

            return BoxCast(center, halfExtents, dir, Quaternion.identity, maxDistance, layerMask, out hitInfo);
        }



        public static bool BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 dir, float maxDistance, int layerMask, out RaycastHit[] hitInfo)
        {
#if UNITY_EDITOR
            //Gizmo_Box(origin, size, 0, layerMask);
#endif

            return BoxCastAll(center, halfExtents, dir, Quaternion.identity, maxDistance, layerMask, out hitInfo);
        }



        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 dir, float maxDistance, int layerMask, RaycastHit[] hitInfo)
        {
#if UNITY_EDITOR
            //Gizmo_Box(origin, size, 0, layerMask);
#endif

            return BoxCastNonAlloc(center, halfExtents, dir, Quaternion.identity, maxDistance, layerMask, hitInfo);
        }



        ///======================================================================================================================================================



        //? 스피어 캐스트



        public static bool SphereCast(Vector3 origin, float radius, Vector3 dir, float maxDistance, int layerMask, out RaycastHit hitInfo)
        {
            Physics.SphereCast(origin, radius, dir, out hitInfo, maxDistance, layerMask);

#if UNITY_EDITOR
            //Gizmo_SphereCast(origin, radius, dir, distance, layerMask);
#endif

            return hitInfo.collider != null;
        }



        public static bool SphereCastAll(Vector3 origin, float radius, Vector3 dir, float maxDistance, int layerMask, out RaycastHit[] hitInfo)
        {
            hitInfo = Physics.SphereCastAll(origin, radius, dir, maxDistance, layerMask);

#if UNITY_EDITOR
            //Gizmo_SphereCast(origin, radius, dir, distance, layerMask);
#endif

            return hitInfo.Length != 0;
        }



        public static int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 dir, float maxDistance, int layerMask, RaycastHit[] targetHitInfo)
        {
#if UNITY_EDITOR
            //Gizmo_SphereCast(origin, radius, dir, distance, layerMask);
#endif

            return Physics.SphereCastNonAlloc(origin, radius, dir, targetHitInfo, maxDistance, layerMask);
        }



        ///======================================================================================================================================================



        //? 스피어 캐스트 제자리



        public static bool SphereCast(Vector3 origin, float radius, int layerMask, out RaycastHit hitInfo)
        {
#if UNITY_EDITOR
            //Gizmo_Sphere(origin, radius, layerMask);
#endif

            return SphereCast(origin, radius, Vector3.zero, 0, layerMask, out hitInfo);
        }



        public static bool SphereCastAll(Vector3 origin, float radius, int layerMask, out RaycastHit[] hitInfo)
        {
#if UNITY_EDITOR
            //Gizmo_Sphere(origin, radius, layerMask);
#endif

            return SphereCastAll(origin, radius, Vector3.zero, 0, layerMask, out hitInfo);
        }



        public static int SphereCastNonAlloc(Vector3 origin, float radius, int layerMask, RaycastHit[] targetHitInfo)
        {
#if UNITY_EDITOR
            //Gizmo_Sphere(origin, radius, layerMask);
#endif

            return SphereCastNonAlloc(origin, radius, Vector3.zero, 0, layerMask, targetHitInfo);
        }



        ///======================================================================================================================================================



        //? 오버랩 박스



        public static bool OverlapBoxAll(Vector3 point, Vector3 size, Quaternion orientation, int layerMask, out Collider[] colliderInfo)
        {
            colliderInfo = Physics.OverlapBox(point, size, orientation, layerMask);

#if UNITY_EDITOR
            //Gizmo_Box(point, size, orientation, layerMask);
#endif

            return colliderInfo.Length != 0;
        }



        public static int OverlapBoxNonAlloc(Vector3 point, Vector3 size, Quaternion orientation, int layerMask, Collider[] targetColliderInfo)
        {
#if UNITY_EDITOR
            //Gizmo_Box(point, size, angle, layerMask);
#endif
            return Physics.OverlapBoxNonAlloc(point, size, targetColliderInfo, orientation, layerMask);
        }



        ///======================================================================================================================================================



        //? 오버랩 박스 각도 X



        public static bool OverlapBoxAll(Vector3 point, Vector3 size, int layerMask, out Collider[] colliderInfo)
        {
#if UNITY_EDITOR
            //Gizmo_Box(point, size, 0, layerMask);
#endif
            return OverlapBoxAll(point, size, Quaternion.identity, layerMask, out colliderInfo);
        }



        public static int OverlapBoxNonAlloc(Vector3 point, Vector3 size, int layerMask, Collider[] targetColliderInfo)
        {
#if UNITY_EDITOR
            //Gizmo_Box(point, size, 0, layerMask);
#endif
            return OverlapBoxNonAlloc(point, size, Quaternion.identity, layerMask, targetColliderInfo);
        }



        ///======================================================================================================================================================



        //? 오버랩 스피어


        public static bool OverlapSphereAll(Vector3 position, float radius, int layerMask, out Collider[] colliderInfo)
        {
            colliderInfo = Physics.OverlapSphere(position, radius, layerMask);

#if UNITY_EDITOR
            //Gizmo_Sphere(position, radius, layerMask);
#endif

            return colliderInfo.Length != 0;
        }



        public static int OverlapSphereNonAlloc(Vector3 position, float radius, int layerMask, Collider[] targetColliderInfo)
        {
#if UNITY_EDITOR
            //Gizmo_Sphere(position, radius, layerMask);
#endif
            return Physics.OverlapSphereNonAlloc(position, radius, targetColliderInfo, layerMask);
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================
}