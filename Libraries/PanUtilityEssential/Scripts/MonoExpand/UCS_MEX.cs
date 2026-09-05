using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Events;
using DG.Tweening;
using Pan.Util;



//? [Monobehaviour Expand] 유니티의 스크립트 오브젝트의 확장 요소들을 정리한 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    public static class SU_MonoExpand
    {
        ///======================================================================================================================================================



        //? 게임 오브젝트 계층 관련



        #region 부모 오브젝트 계층을 올라가며 탐색



        /// <summary>
        /// 지정된 게임 오브젝트의 부모 계층을 따라 올라가며 주어진 조건을 만족하는 부모 오브젝트를 찾습니다.
        /// </summary>
        /// <param name="childObject">탐색을 시작할 자식 게임 오브젝트</param>
        /// <param name="condition">
        /// 부모 오브젝트가 만족해야 하는 조건. <br/>
        /// 이 조건이 <c>true</c>를 반환하는 부모 오브젝트를 찾으면 반환합니다.
        /// </param>
        /// <returns>
        /// 조건을 만족하는 부모 오브젝트.<br/>
        /// 찾지 못한 경우 <c>null</c> 반환.
        /// </returns>
        public static GameObject FindParentByCondition(GameObject childObject, Func<GameObject, bool> condition)
        {
            Transform currentParent = childObject.transform.parent;

            while (currentParent != null)
            {
                //? 부모 오브젝트가 주어진 조건을 만족하는지 확인
                if (condition(currentParent.gameObject))
                {
                    return currentParent.gameObject; //. 조건을 만족하면 반환
                }

                //? 다음 부모로 이동
                currentParent = currentParent.parent;
            }

            return null; //! 찾지 못했으면 null 반환
        }



        /// <summary>
        /// 지정된 게임 오브젝트의 부모 계층을 따라 올라가며 주어진 조건을 만족하는 부모 오브젝트를 찾습니다.
        /// </summary>
        /// <param name="childObject">탐색을 시작할 자식 게임 오브젝트</param>
        /// <param name="condition">
        /// 부모 오브젝트가 만족해야 하는 조건. <br/>
        /// 이 조건이 <c>true</c>를 반환하는 부모 오브젝트를 찾으면 <paramref name="foundParent"/>에 할당됩니다.
        /// </param>
        /// <param name="foundParent">
        /// 조건을 만족하는 부모 오브젝트. <br/>
        /// 찾지 못한 경우 <c>null</c>이 할당됩니다.
        /// </param>
        /// <returns>
        /// 조건을 만족하는 부모 오브젝트를 찾으면 <c>true</c>, 찾지 못하면 <c>false</c> 반환.
        /// </returns>
        public static bool TryFindParentByCondition(GameObject childObject, Func<GameObject, bool> condition, out GameObject foundParent)
        {
            Transform currentParent = childObject.transform.parent;

            while (currentParent != null)
            {
                //? 부모 오브젝트가 주어진 조건을 만족하는지 확인
                if (condition(currentParent.gameObject))
                {
                    foundParent = currentParent.gameObject; //. 조건을 만족하면 반환
                    return true;
                }

                //? 다음 부모로 이동
                currentParent = currentParent.parent;
            }

            foundParent = null; //! 찾지 못했으면 null 반환
            return false;
        }



        /// <summary>
        /// 지정된 게임 오브젝트의 부모 계층을 따라 올라가며 특정 타입의 컴포넌트를 찾습니다.
        /// </summary>
        /// <typeparam name="TComponent">찾고자 하는 컴포넌트 타입</typeparam>
        /// <param name="childObject">탐색을 시작할 자식 게임 오브젝트</param>
        /// <returns>
        /// 부모 계층에서 발견된 <typeparamref name="TComponent"/> 타입의 컴포넌트.<br/>
        /// 찾지 못한 경우 <c>null</c> 반환.
        /// </returns>
        public static TComponent FindComponentInParent<TComponent>(GameObject childObject) where TComponent : Component
        {
            Transform currentParent = childObject.transform.parent;

            while (currentParent != null)
            {
                //? 현재 부모에 T 타입의 컴포넌트가 있는지 확인
                TComponent component = currentParent.GetComponent<TComponent>();
                if (component != null)
                {
                    return component; //. 컴포넌트를 찾으면 반환
                }

                //? 다음 부모로 이동
                currentParent = currentParent.parent;
            }

            return null; //! 찾지 못했으면 null 반환
        }



        /// <summary>
        /// 지정된 게임 오브젝트의 부모 계층을 따라 올라가며 특정 타입의 컴포넌트를 찾습니다.
        /// </summary>
        /// <typeparam name="TComponent">찾고자 하는 컴포넌트 타입</typeparam>
        /// <param name="childObject">탐색을 시작할 자식 게임 오브젝트</param>
        /// <param name="foundComponent">
        /// 발견된 <typeparamref name="TComponent"/> 타입의 컴포넌트. <br/>
        /// 찾지 못한 경우 <c>null</c>이 할당됩니다.
        /// </param>
        /// <returns>
        /// 컴포넌트를 찾으면 <c>true</c>, 찾지 못하면 <c>false</c> 반환.
        /// </returns>
        public static bool TryGetComponentInParent<TComponent>(GameObject childObject, out TComponent foundComponent) where TComponent : Component
        {
            Transform currentParent = childObject.transform.parent;

            while (currentParent != null)
            {
                //? 현재 부모에 T 타입의 컴포넌트가 있는지 확인
                foundComponent = currentParent.GetComponent<TComponent>();
                if (foundComponent != null)
                {
                    return true; //. 컴포넌트를 찾으면 true 반환
                }

                //? 다음 부모로 이동
                currentParent = currentParent.parent;
            }

            foundComponent = null; //! 찾지 못했으면 null 반환
            return false;
        }



        #endregion



        ///======================================================================================================================================================



        //? 컴포넌트 관련



        /// <summary>
        /// 지정된 MonoBehaviour가 특정 컴포넌트를 가지고 있는지 확인하고, 없으면 새로 추가합니다.
        /// </summary>
        /// <typeparam name="T">확인하거나 추가할 컴포넌트 타입</typeparam>
        /// <param name="monoBehaviour">컴포넌트를 확인할 기준이 되는 MonoBehaviour</param>
        /// <param name="component">
        /// 발견된 또는 새로 추가된 <typeparamref name="T"/> 컴포넌트. <br/>
        /// 기존에 존재하면 해당 컴포넌트를 반환하고, 없으면 새로 추가된 컴포넌트를 반환합니다.
        /// </param>
        public static void GetComponentEnsure<T>(this MonoBehaviour monoBehaviour, out T component) where T : Component, new()
        {
            if (monoBehaviour.TryGetComponent(out component)) { return; }
            component = monoBehaviour.gameObject.AddComponent<T>();
        }



        ///======================================================================================================================================================



        //? 게임 오브젝트 파괴



        private static void _DestroyAuto(UnityEngine.Object go)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(go);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }



        ///<summary>
        ///<see cref="Application.isPlaying"/>에 따라서<br/>
        ///<see cref="UnityEngine.Object.Destroy"/>와<br/>
        ///<see cref="UnityEngine.Object.DestroyImmediate(UnityEngine.Object)"/><br/>
        ///를 자동으로 선택하여 오브젝트를 파괴
        /// </summary>
        public static void DestroyAuto(this GameObject var)
        {
            _DestroyAuto(var);
        }



        ///<summary>
        ///<see cref="Application.isPlaying"/>에 따라서<br/>
        ///<see cref="UnityEngine.Object.Destroy"/>와<br/>
        ///<see cref="UnityEngine.Object.DestroyImmediate(UnityEngine.Object)"/><br/>
        ///를 자동으로 선택하여 오브젝트를 파괴
        /// </summary>
        public static void DestroyAuto(this Component var)
        {
            _DestroyAuto(var.gameObject);
        }



        ///<summary>
        /// <see cref="Application.isPlaying"/>에 따라 <br/>
        /// <see cref="UnityEngine.Object.Destroy"/> 또는 <see cref="UnityEngine.Object.DestroyImmediate"/>를 선택하여 오브젝트를 파괴
        /// </summary>
        /// <param name="obj">파괴할 UnityEngine.Object</param>
        public static void DestroyAuto(this UnityEngine.Object obj)
        {
            if (obj == null) return;

            //? GameObject나 Component인 경우 처리
            if (obj is GameObject go)
            {
                _DestroyAuto(go);
            }
            else if (obj is Component comp)
            {
                _DestroyAuto(comp.gameObject);
            }
            else
            {
                _DestroyAuto(obj);
            }
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================



    //? 인터페이스



    /// <summary>
    /// 오브젝트의 정렬 계층(Sorting Layer) 관련 속성을 정의하는 인터페이스입니다.
    /// </summary>
    public interface ISortLayer
    {
        /// <summary>
        /// 정렬 순서를 나타냅니다. 값이 클수록 화면의 앞쪽에 렌더링됩니다.
        /// </summary>
        int SortOrder { get; set; }

        /// <summary>
        /// 정렬 계층(Sorting Layer)의 이름을 나타냅니다.
        /// </summary>
        string SortName { get; set; }

        /// <summary>
        /// 정렬 계층(Sorting Layer)의 고유 ID를 나타냅니다.
        /// </summary>
        int SortID { get; set; }
    }



    /// <summary>
    /// 렌더러(Renderer)와 정렬 계층(Sorting Layer) 속성을 제공하는 인터페이스입니다.
    /// </summary>
    public interface IRenderer : ISortLayer
    {
        /// <summary>
        /// 렌더링을 담당하는 <see cref="UnityEngine.Renderer"/> 컴포넌트입니다.
        /// </summary>
        Renderer Renderer { get; }

        /// <inheritdoc/>
        int ISortLayer.SortOrder
        {
            get => Renderer.sortingOrder;
            set => Renderer.sortingOrder = value;
        }

        /// <inheritdoc/>
        string ISortLayer.SortName
        {
            get => Renderer.sortingLayerName;
            set => Renderer.sortingLayerName = value;
        }

        /// <inheritdoc/>
        int ISortLayer.SortID
        {
            get => Renderer.sortingLayerID;
            set => Renderer.sortingLayerID = value;
        }
    }



    /// <summary>
    /// 렌더러(Renderer)와 정렬 계층(Sorting Layer) 속성을 가지며, Transform 캐싱 기능을 포함하는 인터페이스입니다.
    /// </summary>
    public interface IRendererObject : IRenderer
    {
        ObjectCaching OC { get; }
    }



    /// <summary>
    /// 빌보드가 가능한 인터페이스
    /// </summary>
    public interface IBillBoardAble
    {

    }



    ///======================================================================================================================================================



    //? Instantiate 된 게임오브젝트들에게 원본 오브젝트 (주로 프리팹)을 보유(Hold) 하는 것을 권장



    /// <summary>
    /// 원본 GameObject(주로 프리팹)을 보유하는 인터페이스
    /// </summary>
    public interface IBaseGameObjectHolder
    {
        /// <summary>
        /// 원본 GameObject(주로 프리팹)을 반환 또는 설정
        /// </summary>
        GameObject BaseGameObject { get; set; }
    }



    /// <summary>
    /// 원본 MonoBehaviour(주로 프리팹)을 보유하는 인터페이스
    /// </summary>
    public interface IBaseMonoBehaviourHolder<TMono> : IBaseGameObjectHolder where TMono : MonoBehaviour
    {
        /// <summary>
        /// 원본 MonoBehaviour(주로 프리팹)을 반환 또는 설정
        /// </summary>
        TMono BaseMonoBehvaiour { get; set; }
    }



    ///======================================================================================================================================================



    //? 오브젝트 캐싱, OC



    ///<summary>
    ///오브젝트 캐싱 클래스<br/>
    ///<b>(반드시 Awake에서 new 초기화)</b>
    ///</summary>
    public class ObjectCaching : IObjectCaching
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 오브젝트 캐싱 할당!
        /// </summary>
        public ObjectCaching(MonoBehaviour mono)
        {
            Mono = mono;
            GameObject = mono.gameObject;
            Transform = mono.transform;
        }



        ///======================================================================================================================================================



        //? 기본 변수



        private readonly MonoBehaviour Mono;



        public readonly GameObject GameObject;
        public readonly Transform Transform;



        ObjectCaching IObjectCaching.OC => this;



        ///======================================================================================================================================================



        //? Position 변수



        #region Position



        public Vector3 Position
        {
            get => Transform.position;
            set => Transform.position = value;
        }



        public Vector2 PositionV2
        {
            get => Transform.position;
            set => Transform.position = value;
        }



        public Vector3 LocalPosition
        {
            get => Transform.localPosition;
            set => Transform.localPosition = value;
        }



        public Vector2 LocalPositionV2
        {
            get => Transform.localPosition;
            set => Transform.localPosition = value;
        }



        #endregion



        ///======================================================================================================================================================



        //? Scale 변수



        #region Scale



        public Vector3 LocalScale => Transform.localScale;



        public Vector2 LocalScaleV2 => Transform.localScale;



        public Vector3 LossyScale => Transform.lossyScale;



        #endregion



        ///======================================================================================================================================================



        //? 활성화/비활성화



        ///<summary>
        ///게임오브젝트 활성화/비활성화
        ///</summary>
        public void SetOnActive(bool enable)
        {
            GameObject.SetActive(enable);
        }



        ///<summary>target이 oc라면 SetOnActive, 아니라면 그냥 SetActive</summary>
        ///<param name="destory">true라면, SetActive(false) 대신 오브젝트를 파괴함</param>
        public static void TrySetOnActive(MonoBehaviour target, bool value, bool destory = false)
        {
            if (target is IObjectCaching oc)
            {
                oc.OC.SetOnActive(value);
            }
            else
            {
                if (destory && value == false)
                {
                    target.DestroyAuto();
                    return;
                }

                target.gameObject.SetActive(value);
            }
        }



        ///======================================================================================================================================================C



        //? 초기화



        /// <summary>
        /// <b>(원하는 방식으로 초기화 되지 않을수 있으니 주의)</b>
        /// 트랜스폼 초기화 (레이어는 0으로)
        /// </summary>
        /// <param name="resetByLocal">local을 기준으로 초기화 여부</param>
        public void ResetTransform(bool resetByLocal = false)
        {
            if (resetByLocal)
            {
                ResetLocalPosition();
                ResetLocalRotation();
            }
            else
            {
                ResetPosition();
                ResetRotation();
            }

            ResetLocalScale();

            SetLayer(0);
        }



        ///======================================================================================================================================================C



        //? 부모 설정



        #region 부모 설정



        /// <summary>
        /// 지정된 대상(target)을 부모로 설정합니다.<br/>
        /// <i>(제네릭 제약이 없어, 실패할수도 있음!)</i>
        /// </summary>
        /// <typeparam name="T">부모로 설정할 대상의 타입</typeparam>
        /// <param name="target">부모가 될 대상</param>
        /// <param name="worldPositionStays">
        /// <c>true</c>이면 월드 좌표를 유지하고 로컬 좌표를 변경합니다.<br/>
        /// <c>false</c>이면 로컬 좌표를 유지하고 월드 좌표가 이동합니다.
        /// </param>
        public bool SetParent<T>(T target, bool worldPositionStays = false)
        {
            switch (target)
            {
                case IObjectCaching ioc: SetParent(ioc, worldPositionStays); break;
                case Transform tf: SetParent(tf, worldPositionStays); break;
                case GameObject gameObject: SetParent(gameObject, worldPositionStays); break;
                case Component component: SetParent(component, worldPositionStays); break;
                default: return false;
            }

            return true;
        }



        /// <summary>
        /// 지정된 대상(target)을 부모로 설정하려 시도합니다.
        /// </summary>
        /// <typeparam name="T">부모로 설정할 대상의 타입</typeparam>
        /// <param name="target">부모가 될 대상</param>
        /// <param name="worldPositionStays">
        /// <c>true</c>이면 월드 좌표를 유지하고 로컬 좌표를 변경합니다.<br/>
        /// <c>false</c>이면 로컬 좌표를 유지하고 월드 좌표가 이동합니다.
        /// </param>
        /// <returns>
        /// 부모 설정이 성공하면 <c>true</c>, 실패하면 <c>false</c>를 반환합니다.
        /// </returns>
        public bool TrySetParent<T>(T target, bool worldPositionStays = false)
        {
            if (target == null) { return false; }

            switch (target)
            {
                case IObjectCaching ioc: SetParent(ioc, worldPositionStays); return true;
                case Transform tf: SetParent(tf, worldPositionStays); return true;
                case GameObject gameObject: SetParent(gameObject, worldPositionStays); return true;
                case Component component: SetParent(component, worldPositionStays); return true;
                default: return false;
            }
        }



        /// <summary>
        /// <see cref="IObjectCaching"/>을 부모로 설정합니다.
        /// </summary>
        /// <param name="target">부모가 될 <see cref="IObjectCaching"/> 객체</param>
        /// <param name="worldPositionStays">
        /// <c>true</c>이면 월드 좌표를 유지하고 로컬 좌표를 변경합니다.<br/>
        /// <c>false</c>이면 로컬 좌표를 유지하고 월드 좌표가 이동합니다.
        /// </param>
        public void SetParent(IObjectCaching target, bool worldPositionStays = false)
        {
            Transform.SetParent(target.OC.Transform, worldPositionStays);
        }



        /// <summary>
        /// <see cref="Transform"/>을 부모로 설정합니다.
        /// </summary>
        /// <param name="target">부모가 될 <see cref="Transform"/> 객체</param>
        /// <param name="worldPositionStays">
        /// <c>true</c>이면 월드 좌표를 유지하고 로컬 좌표를 변경합니다.<br/>
        /// <c>false</c>이면 로컬 좌표를 유지하고 월드 좌표가 이동합니다.
        /// </param>
        public void SetParent(Transform target, bool worldPositionStays = false)
        {
            Transform.SetParent(target, worldPositionStays);
        }



        /// <summary>
        /// <see cref="Component"/>을 부모로 설정합니다.
        /// </summary>
        /// <param name="target">부모가 될 <see cref="Component"/> 객체</param>
        /// <param name="worldPositionStays">
        /// <c>true</c>이면 월드 좌표를 유지하고 로컬 좌표를 변경합니다.<br/>
        /// <c>false</c>이면 로컬 좌표를 유지하고 월드 좌표가 이동합니다.
        /// </param>
        public void SetParent(Component target, bool worldPositionStays = false)
        {
            Transform.SetParent(target.transform, worldPositionStays);
        }



        /// <summary>
        /// <see cref="GameObject"/>을 부모로 설정합니다.
        /// </summary>
        /// <param name="target">부모가 될 <see cref="GameObject"/> 객체</param>
        /// <param name="worldPositionStays">
        /// <c>true</c>이면 월드 좌표를 유지하고 로컬 좌표를 변경합니다.<br/>
        /// <c>false</c>이면 로컬 좌표를 유지하고 월드 좌표가 이동합니다.
        /// </param>
        public void SetParent(GameObject target, bool worldPositionStays = false)
        {
            Transform.SetParent(target.transform, worldPositionStays);
        }



        #endregion



        ///======================================================================================================================================================C



        //? 레이어 설정


        public int Layer { get => GameObject.layer; set => GameObject.layer = value; }



        /// <summary>레이어 설정 (int)</summary>
        /// <param name="layer">int 레이어</param>
        public void SetLayer(int layer)
        {
            GameObject.layer = layer;
        }



        ///======================================================================================================================================================C



        //? 좌표 설정



        #region 좌표 설정



        ///======================================================================================================================================================



        public void SetPosition(Vector3 value)
        {
            Transform.position = value;
        }
        public void SetPosition(Vector2 value)
        {
            Transform.position = value;
        }
        public void SetPosition(float x, float y)
        {
            Transform.position = new Vector2(x, y);
        }
        public void SetPosition(float x, float y, float z)
        {
            Transform.position = new Vector3(x, y, z);
        }
        public void SetPosition(IObjectCaching target) => SetPosition(target.OC.Position);



        public void AddPosition(Vector3 value)
        {
            Transform.position += value;
        }
        public void AddPosition(Vector2 value)
        {
            Transform.position += new Vector3(value.x, value.y, 0);
        }
        public void AddPosition(float x, float y, float z)
        {
            Transform.position += new Vector3(x, y, z);
        }
        public void AddPosition(float x, float y)
        {
            Transform.position += new Vector3(x, y, 0);
        }



        ///======================================================================================================================================================



        public void SetPositionX(float value)
        {
            Transform.position = new Vector3(value, Transform.position.y, Transform.position.z);
        }
        public void SetPositionY(float value)
        {
            Transform.position = new Vector3(Transform.position.x, value, Transform.position.z);
        }
        public void SetPositionZ(float value)
        {
            Transform.position = new Vector3(Transform.position.x, Transform.position.y, value);
        }
        public void SetPositionAxis(EAxis axis, float value)
        {
            switch (axis)
            {
                case EAxis.X: SetPositionX(value); break;
                case EAxis.Y: SetPositionY(value); break;
                case EAxis.Z: SetPositionZ(value); break;
            }
        }
        public void SetPositionX(IObjectCaching target) => SetPositionX(target.OC.Position.x);
        public void SetPositionY(IObjectCaching target) => SetPositionY(target.OC.Position.y);
        public void SetPositionZ(IObjectCaching target) => SetPositionZ(target.OC.Position.z);



        public void AddPositionX(float value)
        {
            Transform.position += new Vector3(value, 0, 0);
        }
        public void AddPositionY(float value)
        {
            Transform.position += new Vector3(0, value, 0);
        }
        public void AddPositionZ(float value)
        {
            Transform.position += new Vector3(0, 0, value);
        }
        public void AddPositionAxis(EAxis axis, float value)
        {
            switch (axis)
            {
                case EAxis.X: AddPositionX(value); break;
                case EAxis.Y: AddPositionY(value); break;
                case EAxis.Z: AddPositionZ(value); break;
            }
        }



        ///======================================================================================================================================================



        //? 좌표 설정 : 초기화



        public void ResetPosition() => SetPosition(0, 0, 0);



        ///======================================================================================================================================================



        #endregion



        ///======================================================================================================================================================



        //? 로컬 좌표 설정



        #region 로컬 좌표 설정



        ///======================================================================================================================================================



        public void SetLocalPosition(Vector3 value)
        {
            Transform.localPosition = value;
        }
        public void SetLocalPosition(Vector2 value)
        {
            Transform.localPosition = value;
        }
        public void SetLocalPosition(float x, float y)
        {
            Transform.localPosition = new Vector2(x, y);
        }
        public void SetLocalPosition(float x, float y, float z)
        {
            Transform.localPosition = new Vector3(x, y, z);
        }
        public void SetLocalPosition(IObjectCaching target) => SetLocalPosition(target.OC.Position);



        public void AddLocalPosition(Vector3 value)
        {
            Transform.localPosition += value;
        }
        public void AddLocalPosition(Vector2 value)
        {
            Transform.localPosition += new Vector3(value.x, value.y, 0);
        }
        public void AddLocalPosition(float x, float y, float z)
        {
            Transform.localPosition += new Vector3(x, y, z);
        }
        public void AddLocalPosition(float x, float y)
        {
            Transform.localPosition += new Vector3(x, y, 0);
        }



        ///======================================================================================================================================================



        public void SetLocalPositionX(float value)
        {
            Transform.localPosition = new Vector3(value, Transform.localPosition.y, Transform.localPosition.z);
        }
        public void SetLocalPositionY(float value)
        {
            Transform.localPosition = new Vector3(Transform.localPosition.x, value, Transform.localPosition.z);
        }
        public void SetLocalPositionZ(float value)
        {
            Transform.localPosition = new Vector3(Transform.localPosition.x, Transform.localPosition.y, value);
        }
        public void SetLocalPositionX(IObjectCaching target) => SetLocalPositionX(target.OC.LocalPosition.x);
        public void SetLocalPositionY(IObjectCaching target) => SetLocalPositionY(target.OC.LocalPosition.y);
        public void SetLocalPositionZ(IObjectCaching target) => SetLocalPositionZ(target.OC.LocalPosition.z);



        public void AddLocalPositionX(float value)
        {
            Transform.localPosition += new Vector3(value, 0, 0);
        }
        public void AddLocalPositionY(float value)
        {
            Transform.localPosition += new Vector3(0, value, 0);
        }
        public void AddLocalPositionZ(float value)
        {
            Transform.localPosition += new Vector3(0, 0, value);
        }



        ///======================================================================================================================================================



        //? 로컬좌표 설정 : 초기화



        public void ResetLocalPosition() => SetLocalPosition(0, 0, 0);



        ///======================================================================================================================================================



        #endregion



        ///======================================================================================================================================================C



        //? 회전 설정



        #region 회전 설정



        ///======================================================================================================================================================C



        public Vector3 EulerAngle
        {
            get => Transform.eulerAngles;
            set => Transform.eulerAngles = value;
        }



        public Vector3 LocalEulerAngle
        {
            get => Transform.localEulerAngles;
            set => Transform.localEulerAngles = value;
        }



        ///======================================================================================================================================================



        //? EulerAngle



        public void SetEulerAngles(Vector3 value)
        {
            Transform.eulerAngles = value;
        }
        public void SetEulerAngles(Vector2 value)
        {
            Transform.eulerAngles = value;
        }
        public void SetEulerAngles(float x, float y, float z)
        {
            Transform.eulerAngles = new Vector3(x, y, z);
        }
        public void SetEulerAngles(float x, float y)
        {
            Transform.eulerAngles = new Vector2(x, y);
        }



        public void AddEulerAngles(Vector3 value)
        {
            Transform.eulerAngles += value;
        }
        public void AddEulerAngles(Vector2 value)
        {
            Transform.eulerAngles += new Vector3(value.x, value.y, 0);
        }
        public void AddEulerAngles(float x, float y, float z)
        {
            Transform.eulerAngles += new Vector3(x, y, z);
        }
        public void AddEulerAngles(float x, float y)
        {
            Transform.eulerAngles += new Vector3(x, y, 0);
        }



        ///======================================================================================================================================================



        //? EulerAngle



        public void SetEulerAnglesX(float value)
        {
            Transform.eulerAngles = new Vector3(value, Transform.eulerAngles.y, Transform.eulerAngles.z);
        }
        public void SetEulerAnglesY(float value)
        {
            Transform.eulerAngles = new Vector3(Transform.eulerAngles.x, value, Transform.eulerAngles.z);
        }
        public void SetEulerAnglesZ(float value)
        {
            Transform.eulerAngles = new Vector3(Transform.eulerAngles.x, Transform.eulerAngles.y, value);
        }
        public void SetEulerAnglesAxis(EAxis axis, float value)
        {
            switch (axis)
            {
                case EAxis.X: SetEulerAnglesX(value); break;
                case EAxis.Y: SetEulerAnglesY(value); break;
                case EAxis.Z: SetEulerAnglesZ(value); break;
            }
        }



        public void AddEulerAnglesX(float value)
        {
            Transform.eulerAngles += new Vector3(value, 0, 0);
        }
        public void AddEulerAnglesY(float value)
        {
            Transform.eulerAngles += new Vector3(0, value, 0);
        }
        public void AddEulerAnglesZ(float value)
        {
            Transform.eulerAngles += new Vector3(0, 0, value);
        }
        public void AddEulerAnglesAxis(EAxis axis, float value)
        {
            switch (axis)
            {
                case EAxis.X: AddEulerAnglesX(value); break;
                case EAxis.Y: AddEulerAnglesY(value); break;
                case EAxis.Z: AddEulerAnglesZ(value); break;
            }
        }



        ///======================================================================================================================================================



        //? LocalEulerAngle



        public void SetLocalEulerAngles(Vector3 value)
        {
            Transform.localEulerAngles = value;
        }
        public void SetLocalEulerAngles(Vector2 value)
        {
            Transform.localEulerAngles = value;
        }
        public void SetLocalEulerAngles(float x, float y, float z)
        {
            Transform.localEulerAngles = new Vector3(x, y, z);
        }
        public void SetLocalEulerAngles(float x, float y)
        {
            Transform.localEulerAngles = new Vector2(x, y);
        }



        public void AddLocalEulerAngles(Vector3 value)
        {
            Transform.localEulerAngles += value;
        }
        public void AddLocalEulerAngles(Vector2 value)
        {
            Transform.localEulerAngles += new Vector3(value.x, value.y, 0);
        }
        public void AddLocalEulerAngles(float x, float y, float z)
        {
            Transform.localEulerAngles += new Vector3(x, y, z);
        }
        public void AddLocalEulerAngles(float x, float y)
        {
            Transform.localEulerAngles += new Vector3(x, y, 0);
        }



        ///======================================================================================================================================================



        //? LocalEulerAngle



        public void SetLocalEulerAnglesX(float value)
        {
            Transform.localEulerAngles = new Vector3(value, Transform.localEulerAngles.y, Transform.localEulerAngles.z);
        }
        public void SetLocalEulerAnglesY(float value)
        {
            Transform.localEulerAngles = new Vector3(Transform.localEulerAngles.x, value, Transform.localEulerAngles.z);
        }
        public void SetLocalEulerAnglesZ(float value)
        {
            Transform.localEulerAngles = new Vector3(Transform.localEulerAngles.x, Transform.localEulerAngles.y, value);
        }
        public void SetLocalEulerAnglesAxis(EAxis axis, float value)
        {
            switch (axis)
            {
                case EAxis.X: SetLocalEulerAnglesX(value); break;
                case EAxis.Y: SetLocalEulerAnglesY(value); break;
                case EAxis.Z: SetLocalEulerAnglesZ(value); break;
            }
        }



        public void AddLocalEulerAnglesX(float value)
        {
            Transform.localEulerAngles += new Vector3(value, 0, 0);
        }
        public void AddLocalEulerAnglesY(float value)
        {
            Transform.localEulerAngles += new Vector3(0, value, 0);
        }
        public void AddLocalEulerAnglesZ(float value)
        {
            Transform.localEulerAngles += new Vector3(0, 0, value);
        }
        public void AddLocalEulerAnglesAxis(EAxis axis, float value)
        {
            switch (axis)
            {
                case EAxis.X: AddLocalEulerAnglesX(value); break;
                case EAxis.Y: AddLocalEulerAnglesY(value); break;
                case EAxis.Z: AddLocalEulerAnglesZ(value); break;
            }
        }



        ///======================================================================================================================================================



        //? 회전 초기화



        public void ResetRotation() => Transform.rotation = new Quaternion(0, 0, 0, 0);



        public void ResetLocalRotation() => Transform.localRotation = new Quaternion(0, 0, 0, 0);



        ///======================================================================================================================================================



        #endregion



        ///======================================================================================================================================================



        //? 스케일 설정



        #region 스케일 설정



        ///======================================================================================================================================================



        public void SetLocalScale(Vector3 value)
        {
            Transform.localScale = value;
        }
        public void SetLocalScale(Vector2 value)
        {
            Transform.localScale = value;
        }
        public void SetLocalScale(float x, float y, float z)
        {
            Transform.localScale = new Vector3(x, y, z);
        }
        public void SetLocalScale(float x, float y)
        {
            Transform.localScale = new Vector2(x, y);
        }
        public void SetLocalScale(IObjectCaching target) => SetLocalScale(target.OC.LocalScale);



        ///======================================================================================================================================================



        public void SetLocalScaleX(float value)
        {
            Transform.localScale = new Vector3(value, Transform.localScale.y, Transform.localScale.z);
        }
        public void SetLocalScaleY(float value)
        {
            Transform.localScale = new Vector3(Transform.localScale.x, value, Transform.localScale.z);
        }
        public void SetLocalScaleZ(float value)
        {
            Transform.localScale = new Vector3(Transform.localScale.x, Transform.localScale.y, value);
        }



        public void SetLocalScaleX(IObjectCaching target) => SetLocalScaleX(target.OC.LocalScale.x);
        public void SetLocalScaleY(IObjectCaching target) => SetLocalScaleX(target.OC.LocalScale.y);
        public void SetLocalScaleZ(IObjectCaching target) => SetLocalScaleX(target.OC.LocalScale.z);



        ///======================================================================================================================================================



        //? 스케일 설정 : 초기화



        public void ResetLocalScale() => SetLocalScale(1, 1, 1);



        ///======================================================================================================================================================



        #endregion



        ///======================================================================================================================================================
    }



    ///<summary>
    ///오브젝트 캐싱 보유 인터페이스
    ///</summary>
    public interface IObjectCaching
    {
        ObjectCaching OC { get; }
    }



    ///======================================================================================================================================================
}