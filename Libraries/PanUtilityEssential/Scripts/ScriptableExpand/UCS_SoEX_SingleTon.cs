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
using UnityEngine.Animations;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Sirenix.OdinInspector;



//? [Scriptable Expand] 스트립트 오브젝트 싱글톤을 정리한 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    /// <summary>
    /// ScriptableObject 타입의 싱글톤 인스턴스를 제공하는 인터페이스입니다.
    /// </summary>
    public interface ISingleTon_ScriptableObject
    {
        /// <summary>
        /// 싱글톤 ScriptableObject 인스턴스를 반환합니다.
        /// </summary>
        ScriptableObject SingleTon { get; }
    }



    /// <summary>
    /// 특정 ScriptableObject 타입(TSbject)에 대한 싱글톤 인스턴스를 제공하는 인터페이스입니다.
    /// </summary>
    /// <typeparam name="TSbject">ScriptableObject를 상속받는 클래스</typeparam>
    public interface ISingleTon_ScriptableObject<TSbject> where TSbject : ScriptableObject
    {
        /// <summary>
        /// 싱글톤 ScriptableObject 인스턴스를 반환합니다.
        /// </summary>
        TSbject SingleTon { get; }
    }



    /// <summary>
    /// Addressables를 통해 ScriptableObject를 싱글톤으로 로드/언로드하는 추상 클래스입니다.
    /// </summary>
    /// <typeparam name="TSbject">ScriptableObject를 상속받고, ISingleTon_ScriptableObject를 구현한 클래스</typeparam>
    public abstract class SingleTon_ScriptableObject<TSbject> : ScriptableObject, ISingleTon_ScriptableObject, ISingleTon_ScriptableObject<TSbject>
        where TSbject : ScriptableObject, ISingleTon_ScriptableObject
    {
        ///======================================================================================================================================================



#if UNITY_EDITOR

        [TitleGroup("싱글톤 스크립터블 오브젝트", "SingleTon-ScriptableObject", TitleAlignments.Split)]
        [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Center, EnableRichText = true), EnableGUI]
        [PropertyOrder(-1000)]
        [PropertySpace(8, 0)]
        private string dummy_SingleTonMessage1
        {
            get
            {
                return "<color=white><size=12><b>싱글톤 ScriptableObject 사용중</b></size></color>";
            }
        }

        [TitleGroup("싱글톤 스크립터블 오브젝트", "SingleTon-ScriptableObject", TitleAlignments.Split)]
        [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Center, EnableRichText = true), EnableGUI]
        [PropertyOrder(-999)]
        [PropertySpace(0, 8)]
        private string dummy_SingleTonMessage2
        {
            get
            {
                if (O == null)
                {

                    return "<color=red><size=12><b>싱글톤을 불러올 수 없음 (어드레서블 로드 실패)</b></size></color>";
                }
                else
                {
                    return $"<color=white><size=12><b><color=#f7da64>\"{AssetName()}\"</color> 로드 성공</b>";
                }
            }
        }

#endif



        /// <summary>
        /// 현재 싱글톤 인스턴스 참조.
        /// </summary>
        private static TSbject instance;



        /// <summary>
        /// Addressables 로드 핸들.
        /// </summary>
        private static AsyncOperationHandle<TSbject> instanceHandle;



        ///======================================================================================================================================================



        /// <summary>
        /// 싱글톤 인스턴스를 반환합니다. 없으면 자동으로 로드합니다.
        /// </summary>
        public static TSbject O
        {
            get
            {
                if (instance == null)
                {
                    SettingSingleTon();
                }
                return instance;
            }
        }



        /// <summary>
        /// 인터페이스의 싱글톤 ScriptableObject.
        /// </summary>
        ScriptableObject ISingleTon_ScriptableObject.SingleTon => O;



        /// <summary>
        /// 제네릭 인터페이스의 싱글톤 ScriptableObject.
        /// </summary>
        TSbject ISingleTon_ScriptableObject<TSbject>.SingleTon => O;



        ///======================================================================================================================================================



        /// <summary>
        /// 내부적으로 Addressables에 등록된 에셋 이름
        /// </summary>
        private static string AssetName() => typeof(TSbject).Name;



        /// <summary>
        /// 싱글톤 인스턴스를 세팅합니다. 내부적으로 LoadSingleTon(false)를 호출.
        /// </summary>
        /// <returns>인스턴스 로드 성공 여부</returns>
        private static bool SettingSingleTon()
        {
            instance = LoadSingleTon(false);
            return instance != null;
        }



        /// <summary>
        /// 싱글톤을 로드/갱신합니다.
        /// </summary>
        /// <param name="forceReload">true면 기존 번들·인스턴스 언로드 후 재로드</param>
        public static TSbject LoadSingleTon(bool forceReload = false)
        {
            //. ① 강제 리로드가 아니고 인스턴스가 살아 있으면 그대로 반환
            if (instance != null && !forceReload)
                return instance;

#if UNITY_EDITOR
            //! 250617 싱글톤SO가 생성될때 자동으로 어드레서블에 등록하는 시스템 생성 이후 폐기

            ////? ② 도메인 리로드 후에도 메모리에 남아 있을 수 있는 SO 회수
            //TSbject resident = Resources.FindObjectsOfTypeAll<TSbject>()
            //                            .FirstOrDefault(a => a.name == AssetName());

            //if (resident != null && !forceReload)
            //{
            //    instance = resident;

            //    //. Addressables 핸들이 초기화돼 있을 수도 있으니 가짜 완료-연산 생성
            //    if (!instanceHandle.IsValid())
            //        instanceHandle = Addressables.ResourceManager
            //                                     .CreateCompletedOperation(instance, null);
            //    return instance;               //? **이미 로드돼 있으므로 끝**
            //}
#endif

            //! ③ 여기까지 왔으면 진짜 새로 로드해야 함
            if (instanceHandle.IsValid())     // 기존 핸들 있으면 해제
                Addressables.Release(instanceHandle);

            instanceHandle = Addressables.LoadAssetAsync<TSbject>(AssetName());
            instanceHandle.WaitForCompletion();          //! 에디터에서는 블로킹 허용!

            if (instanceHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"어드레서블로부터 싱글톤 ScriptableObject 볼러오기 실패: {AssetName()}");
                if (instanceHandle.IsValid())
                {
                    Addressables.Release(instanceHandle);
                    instanceHandle = default;
                }
                return null;
            }

            instance = instanceHandle.Result;
            return instance;
        }



        /// <summary>
        /// 번들·핸들을 완전히 언로드하고 캐시까지 비웁니다.
        /// </summary>
        public static void UnLoadSingleTon() => UnLoadSingleTonInternal(AssetName());



        //! 번들 충돌을 막기 위한 내부 언로드 로직
        private static void UnLoadSingleTonInternal(string key)
        {
            //! 이미 로드된 핸들 릴리즈
            if (instanceHandle.IsValid())
            {
                Addressables.Release(instanceHandle);
                instanceHandle = default;
            }


            //. 메모리에 남아 있을 수 있는 번들 의존성까지 제거
            var clearHandle = Addressables.ClearDependencyCacheAsync(key, false);
            try
            {
                clearHandle.WaitForCompletion();
            }
            finally
            {
                if (clearHandle.IsValid())
                {
                    Addressables.Release(clearHandle);
                }
            }


            instance = null;
        }



        ///======================================================================================================================================================



        [field: ReadOnlyCustom]
        public bool IsRegisteredAddressable { get; set; }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================
}
