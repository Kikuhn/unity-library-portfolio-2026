using UnityEngine;



//? [UI] 유저 인터페이스가 정리된 정도의 코드



namespace Pan.Util.UI
{

    ///======================================================================================================================================================



    public class BaseUI : MonoBehaviour
    {
        public RectTransform RectTF { get; private set; }

        protected virtual void Awake()
        {
            RectTF = GetComponent<RectTransform>();
        }
    }



    public class BaseMiniUI : MonoBehaviour
    {

    }



    ///======================================================================================================================================================
}