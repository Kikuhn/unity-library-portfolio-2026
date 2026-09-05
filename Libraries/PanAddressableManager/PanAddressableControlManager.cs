using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;
using System;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;
using System.Reflection;
using System.Collections;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.AddressableAssets.ResourceLocators;
using System.Threading;
using Pan.AddressableManagers;
using System.IO;
using Sirenix.OdinInspector;



namespace Pan.AddressableManagers
{
    public class PanAddressableControlManager : MonoBehaviour, IHoldIPanAddressableManager
    {
        public PanAddressableManager AddressableManager => addressableManager;

        [TitleGroup("어드레서블 매니저")]
        //[LabelText("어드레서블 매니저")]
        [HideLabel, InlineProperty]
        [SerializeField]
        private PanAddressableManager addressableManager;



        [field: TitleGroup("어드레서블 매니저"), BoxGroup("어드레서블 매니저/자동 초기화")]
        [field: LabelText("Awake에서 초기화 시킬지 여부")]
        [field: SerializeField]
        public bool AutoInitializeAwake { get; private set; }



        [field: TitleGroup("어드레서블 매니저"), BoxGroup("어드레서블 매니저/자동 초기화")]
        [field: LabelText("자동 초기화 캐시 초기 용량")]
        [field: SerializeField]
        [field: ShowIf(nameof(AutoInitializeAwake))]
        public int AutoInitializeAwake_CacheCapacity { get; private set; } = 0;



        private void Awake()
        {
            if (AutoInitializeAwake) { addressableManager = new PanAddressableManager(AutoInitializeAwake_CacheCapacity); }
        }



        /// <summary>
        /// 어드레서블 매니저 초기화하기
        /// <para>이미 초기화되었다면 초기화되지않는다</para>
        /// </summary>
        /// <param name="cacheCapacity">어드레서블 캐시 초기 용량</param>
        /// <returns></returns>
        public bool InitializeAddressable(int cacheCapacity = 0)
        {
            //! 이미 초기화되어있다면 실패
            if (addressableManager != null && addressableManager.Initialized) { return false; }


            addressableManager = new PanAddressableManager(cacheCapacity);
            return true;
        }
    }
}