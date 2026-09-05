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
using Pan.AddressableManagers;
using Pan.Util;
using Sirenix.OdinInspector;



namespace Pan.AddressableManagers
{
    public abstract class PanAddressablePackageSbjectBase<TLabel, TPack> : CustomScriptableObjectSerialized where TLabel : Enum where TPack : PanAddressablePackageSbjectBase<TLabel, TPack>.BasePack
    {
        ///======================================================================================================================================================



        [SerializeReference, Sirenix.OdinInspector.ReadOnly]
        private AutoTypeDictionary<TPack> PackDictioanry = null;



        [SerializeReference, Sirenix.OdinInspector.ReadOnly]
        private Dictionary<TLabel, string> LabelDictionary;



        ///======================================================================================================================================================



        ///<summary>
        /// 각 프로젝트별로사용할 Pack의 베이스
        /// </summary>
        public abstract class BasePack
        {
            public PanAddressablePackageSbjectBase<TLabel, TPack> Base { get; private set; }

            public PanAddressableManager Manager { get; private set; }

            public bool Initialize { get; private set; }

            public void InitializePack(PanAddressablePackageSbjectBase<TLabel, TPack> addressablePackageSbject, PanAddressableManager addressableManager)
            {
                Base = addressablePackageSbject;
                Manager = addressableManager;
                Initialize = true;
            }

            ///<summary>
            /// <typeparamref name="TLabel"/>을 캐싱된 문자열로 얻기
            /// </summary>
            public string GetLabel(TLabel label) => Base.GetLabel(label);
        }



        ///======================================================================================================================================================



        protected override void WakeUp()
        {
            //. Null이라면 무조건 새로 초기화하고
            //. Null이 아닌데, 에디터 환경+플레이모드가 아니라면 새로 초기화 (딕셔너리를 새로 초기화하여 변경사항이 있다면 갱신되도록)

            if (PackDictioanry == null)
            {
                initialize_PackDictionary();
            }
            else
            {
#if UNITY_EDITOR
                if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    initialize_PackDictionary();
                }
#endif
            }

            if (LabelDictionary == null)
            {
                initialize_LabelDictionary();
            }
            else
            {
#if UNITY_EDITOR
                if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    initialize_LabelDictionary();
                }
#endif
            }



            void initialize_PackDictionary()
            {
                PackDictioanry = new AutoTypeDictionary<TPack>();
            }



            void initialize_LabelDictionary()
            {
                var labelArray = Enum.GetValues(typeof(TLabel)) as TLabel[];

                LabelDictionary = new Dictionary<TLabel, string>(labelArray.Length);

                for (int i = 0; i < labelArray.Length; i++)
                {
                    LabelDictionary.Add(labelArray[i], labelArray[i].ToString());
                }
            }
        }



        ///<summary>
        /// 어드레서블 팩들 초기화하기
        /// </summary>
        public void InitializePacks(IHoldIPanAddressableManager holdAddressableManager)
        {
            foreach (var item in PackDictioanry.GetDictionary)
            {
                item.Value.InitializePack(this, holdAddressableManager.AddressableManager);
            }
        }



        ///======================================================================================================================================================



        //? 얻기



        ///<summary>
        /// <typeparamref name="TLabel"/>을 캐싱된 문자열로 얻기
        /// </summary>
        public string GetLabel(TLabel label)
        {
            return LabelDictionary[label];
        }


        /// <summary>
        /// <typeparamref name="TPack"/>을 얻기 (딕셔너리에서 꺼내기
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Pack<T>() where T : TPack, new()
        {
            return PackDictioanry.Get<T>();
        }



        ///======================================================================================================================================================
    }
}