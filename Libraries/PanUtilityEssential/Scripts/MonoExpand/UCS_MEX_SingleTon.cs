using UnityEngine;



//? [Monobehaviour Expand] 싱글톤 관련 코드를 정리한 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    /// <summary>
    /// 제네릭 MonoBehaviour 싱글톤 추상 클래스입니다.
    /// </summary>
    /// <typeparam name="T">싱글톤을 구현할 MonoBehaviour 타입</typeparam>
    public abstract class SingleTon<T> : MonoBehaviour where T : MonoBehaviour
    {
        /// <summary>
        /// 싱글톤 인스턴스가 저장되는 정적 필드입니다.
        /// </summary>
        protected static T instance;



        /// <summary>
        /// 싱글톤 인스턴스를 반환합니다. <br/>
        /// 만약 인스턴스가 존재하지 않으면 씬에서 해당 타입의 오브젝트를 찾습니다. <br/>
        /// <see cref="Application.isEditor"/> 상태에서 인스턴스를 찾을 수 없을 경우 <c>null</c>을 반환합니다.
        /// </summary>
        public static T O
        {
            get
            {
                if (instance == null)
                {
                    //// 에디터 상태에서는 null을 반환
                    //if (Application.isEditor)
                    //{
                    //    return null;
                    //}

                    // 씬에서 해당 타입을 검색
                    instance = FindFirstObjectByType<T>();

                    //if (instance == null)
                    //{
                    //    Debug.LogError($"싱글톤 로드 실패: {typeof(T)} 타입의 오브젝트를 찾을 수 없습니다.");
                    //}
                }
                return instance;
            }
        }



        public static bool TryGetSingleTon(out T result)
        {
            result = O;
            return result != null;
        }



        /// <summary>
        /// 이 객체를 싱글톤 인스턴스로 등록하고, <see cref="GameObject"/>를 불멸 오브젝트로 설정합니다.
        /// </summary>
        /// <param name="target">싱글톤 인스턴스로 설정할 대상 <typeparamref name="T"/>입니다.</param>
        /// <returns>싱글톤 등록에 성공하면 true, 이미 다른 인스턴스가 존재하여 현재 객체를 파괴한 경우 false</returns>
        protected bool SetThisObjectEternity(T target)
        {
            if (instance != null && instance != target)
            {
                if (instance.gameObject == gameObject)
                {
                    Debug.LogWarning($"{GetType()} | {gameObject.name} | 같은 게임오브젝트에 중복 싱글톤이 존재합니다. 하지만 파괴하지 않습니다.");
                }
                else
                {
                    Debug.LogWarning($"{GetType()} | {gameObject.name} | 중복된 싱글톤 오브젝트 발견 ({instance.name}), 이 싱글톤 오브젝트를 파괴합니다.");
                    gameObject.DestroyAuto();
                    return false;
                }
            }

            instance = target;
            DontDestroyOnLoad(gameObject);

            if (instance == null)
            {
                Debug.LogWarning($"{gameObject.name} | 싱글톤 등록 실패");
            }

            return true;
        }




        protected virtual void Awake()
        {
            SetThisObjectEternity(this as T);
        }
    }




    ///======================================================================================================================================================
}
