using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.IO;
using UnityEngine.Events;
using DG.Tweening;
using Pan.Util;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using JetBrains.Annotations;
using UnityEngine.PlayerLoop;
using System.Text;
using Cysharp.Threading.Tasks;



//? 직접 만든 상호작용 매니저가 있는 정도의 코드



/* 설명
 * 상호작용 매니저 (Interaction Manager)
 * 
 * 이 시스템은 특정 객체와 관련된 상태(Normal, Hindrance)를 관리하며, 
 * 상태의 추가 및 제거에 따라 실행(Execute) 및 종료(Quit) 이벤트를 트리거합니다.
 * 
 * 주요 구성 요소:
 * 
 * 1. BaseInteractionManager<TInteraction>
 *    - 상호작용 매니저의 기본 클래스로, 상태 관리 및 트랙 계산 로직을 정의합니다.
 *    - Normal 트랙과 Hindrance 트랙의 추가/제거, 최대값 계산, 조건 평가를 수행합니다.
 * 
 * 2. InteractionManager
 *    - 단순 실행 및 종료 이벤트를 처리하는 기본 상호작용 매니저입니다.
 *    - 상태 변화에 따라 Action 델리게이트를 호출합니다.
 * 
 * 3. InteractionManager<T>
 *    - 제네릭 타입 T를 추가적으로 처리할 수 있는 상호작용 매니저입니다.
 *    - 이벤트 호출 시, 타입 T 데이터를 함께 전달할 수 있습니다.
 * 
 * 4. InteractionManager<T1, T2>
 *    - 두 개의 제네릭 타입 T1, T2를 처리할 수 있는 상호작용 매니저입니다.
 *    - 이벤트 호출 시, 타입 T1과 T2 데이터를 함께 전달합니다.
 * 
 * 주요 특징:
 * 
 * - Normal 트랙: 기본 상태를 나타냅니다.
 * - Hindrance 트랙: 방해 요소를 나타냅니다.
 * - 트랙의 최대값 비교를 통해 실행/종료 조건을 평가합니다.
 * - 제네릭 타입을 지원하여 다양한 데이터와의 상호작용이 가능합니다.
 * 
 * 사용 예시:
 * - 게임 내 애니메이션 상태 관리
 * - 스킬 및 버프 시스템
 * - 복잡한 상태 전환이 필요한 로직
 */



namespace Pan.Util
{
    ///======================================================================================================================================================



    /// <summary>
    /// 상호작용 매니저의 기본 클래스입니다.
    /// </summary>
    /// <typeparam name="TInteraction">상호작용을 정의하는 타입입니다.</typeparam>

    public abstract class BaseInteractionManager<TInteraction>
        where TInteraction : BaseInteractionManager<TInteraction>.BaseInteraction, new()
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 기본 상호작용을 정의하는 추상 클래스입니다.
        /// </summary>
        public abstract class BaseInteraction
        {
            ///======================================================================================================================================================



            /// <summary>
            /// Normal 트랙을 관리하는 컬렉션입니다.
            /// </summary>
            private readonly HashSet<int> NormalTracks = new();



            /// <summary>
            /// Hindrance 트랙을 관리하는 컬렉션입니다.
            /// </summary>
            private readonly HashSet<int> HindranceTracks = new();



            ///======================================================================================================================================================



            /// <summary>
            /// Normal 트랙의 개수를 반환합니다.
            /// </summary>
            public int Normal_Count => NormalTracks.Count;



            /// <summary>
            /// Hindrance 트랙의 개수를 반환합니다.
            /// </summary>
            public int Hindrance_Count => HindranceTracks.Count;



            /// <summary>
            /// Normal 트랙 중 가장 큰 값을 반환합니다.
            /// </summary>
            public int Normal_Biggist { get; private set; } = 0;



            /// <summary>
            /// Hindrance 트랙 중 가장 큰 값을 반환합니다.
            /// </summary>
            public int Hindrance_Biggist { get; private set; } = 0;



            ///======================================================================================================================================================



            /// <summary>
            /// Normal 트랙에 키를 추가합니다.
            /// </summary>
            /// <param name="key">추가할 키 값입니다.</param>
            protected void _AddNormal(int key)
            {
                NormalTracks.Add(key);
                Calculate_Normal();
            }



            /// <summary>
            /// Normal 트랙에서 키를 제거합니다.
            /// </summary>
            /// <param name="key">제거할 키 값입니다.</param>
            protected void _RemoveNormal(int key)
            {
                NormalTracks.Remove(key);
                Calculate_Normal();
            }



            /// <summary>
            /// Normal 트랙의 가장 큰 값을 다시 계산합니다.
            /// </summary>
            private void Calculate_Normal()
            {
                Normal_Biggist = 0;
                foreach (var t in NormalTracks)
                {
                    if (Normal_Biggist < t)
                    {
                        Normal_Biggist = t;
                    }
                }
            }



            ///======================================================================================================================================================



            /// <summary>
            /// Hindrance 트랙에 키를 추가합니다.
            /// </summary>
            /// <param name="track">추가할 트랙 값입니다.</param>
            protected void _AddHindrance(int track)
            {
                HindranceTracks.Add(track);
                Calculate_Hindrance();
            }



            /// <summary>
            /// Hindrance 트랙에서 키를 제거합니다.
            /// </summary>
            /// <param name="track">제거할 트랙 값입니다.</param>
            protected void _RemoveHindrance(int track)
            {
                HindranceTracks.Remove(track);
                Calculate_Hindrance();
            }



            /// <summary>
            /// Hindrance 트랙의 가장 큰 값을 다시 계산합니다.
            /// </summary>
            private void Calculate_Hindrance()
            {
                Hindrance_Biggist = 0;
                foreach (var t in HindranceTracks)
                {
                    if (Hindrance_Biggist < t)
                    {
                        Hindrance_Biggist = t;
                    }
                }
            }



            ///======================================================================================================================================================



            /// <summary>
            /// Normal 트랙을 추가하고 조건을 평가합니다.
            /// </summary>
            /// <param name="normal">추가할 Normal 값입니다.</param>
            /// <returns>Hindrance보다 Normal이 큰 경우 true를 반환합니다.</returns>
            protected bool Calculate_AddNormal(int normal)
            {
                _AddNormal(normal);
                return Hindrance_Biggist < normal;
            }



            /// <summary>
            /// Normal 트랙을 제거하고 조건을 평가합니다.
            /// </summary>
            /// <param name="normal">제거할 Normal 값입니다.</param>
            /// <returns>종료 조건을 충족하는 경우 true를 반환합니다.</returns>
            protected bool Calculate_RemoveNormal(int normal)
            {
                _RemoveNormal(normal);
                return Normal_Count == 0 || (Hindrance_Count > 0 && Normal_Biggist < Hindrance_Biggist);
            }



            /// <summary>
            /// Hindrance 트랙을 추가하고 조건을 평가합니다.
            /// </summary>
            /// <param name="hindrance">추가할 Hindrance 값입니다.</param>
            /// <returns>Hindrance가 Normal보다 큰 경우 true를 반환합니다.</returns>
            protected bool Calculate_AddHindrance(int hindrance)
            {
                _AddHindrance(hindrance);
                return Normal_Biggist < hindrance;
            }



            /// <summary>
            /// Hindrance 트랙을 제거하고 조건을 평가합니다.
            /// </summary>
            /// <param name="hindrance">제거할 Hindrance 값입니다.</param>
            /// <returns>Normal이 Hindrance보다 큰 경우 true를 반환합니다.</returns>
            protected bool Calculate_RemoveHindrance(int hindrance)
            {
                _RemoveHindrance(hindrance);
                return Normal_Count > 0 && Hindrance_Biggist < Normal_Biggist;
            }



            ///======================================================================================================================================================
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 상호작용 매니저를 관리하는 클래스입니다.
        /// </summary>
        private Dictionary<string, TInteraction> InteractionManagers;



        ///======================================================================================================================================================



        /// <summary>
        /// 매니저 활성화 여부입니다.
        /// </summary>
        protected bool IsEnable = false;



        ///======================================================================================================================================================



        /// <summary>
        /// 상호작용 매니저를 활성화합니다.
        /// </summary>
        /// <param name="capacity">초기 딕셔너리 용량입니다.</param>
        public void SetEnable(int capacity = 0)
        {
            if (!IsEnable)
            {
                IsEnable = true;
                InteractionManagers = new Dictionary<string, TInteraction>(capacity);
            }
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 새로운 상호작용 매니저를 추가합니다.
        /// </summary>
        /// <param name="key">매니저의 키 값입니다.</param>
        /// <returns>추가된 상호작용 매니저입니다.</returns>
        public TInteraction AddManger(string key)
        {
            SetEnable();
            if (!InteractionManagers.ContainsKey(key))
            {
                InteractionManagers[key] = new TInteraction();
            }
            return InteractionManagers[key];
        }



        /// <summary>
        /// 특정 키에 해당하는 상호작용 매니저를 제거합니다.
        /// </summary>
        /// <param name="key">제거할 매니저의 키 값입니다.</param>
        /// <returns>제거 성공 여부를 반환합니다.</returns>
        public bool RemoveManger(string key)
        {
            SetEnable();
            return InteractionManagers.Remove(key);
        }



        /// <summary>
        /// 특정 키에 해당하는 상호작용 매니저를 가져옵니다.
        /// </summary>
        /// <param name="key">가져올 매니저의 키 값입니다.</param>
        /// <returns>해당 매니저를 반환하거나, 없으면 null을 반환합니다.</returns>
        public TInteraction GetManager(string key)
        {
            SetEnable();
            InteractionManagers.TryGetValue(key, out var manager);
            return manager;
        }



        /// <summary>
        /// 특정 키의 매니저를 시도하여 가져옵니다.
        /// </summary>
        /// <param name="key">매니저의 키 값입니다.</param>
        /// <param name="result">결과 매니저를 출력합니다.</param>
        /// <returns>매니저를 성공적으로 가져왔는지 여부를 반환합니다.</returns>
        public bool TryGetManager(string key, out TInteraction result)
        {
            SetEnable();
            if (InteractionManagers.TryGetValue(key, out result))
            {
                return true;
            }
            result = null;
            return false;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 딕셔너리 메모리를 최적화합니다.
        /// </summary>
        public void Memory_TrimExcess()
        {
            InteractionManagers?.TrimExcess();
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 상호작용을 관리하는 클래스입니다.
    /// </summary>
    public class InteractionManager : BaseInteractionManager<InteractionManager.Interaction>
    {
        /// <summary>
        /// 기본 상호작용을 정의하는 클래스입니다.
        /// </summary>
        public class Interaction : BaseInteraction
        {
            ///======================================================================================================================================================



            private Action ExecuteEvent { get; set; } = null;
            private Action QuitEvent { get; set; } = null;



            ///======================================================================================================================================================



            /// <summary>
            /// 실행 및 종료 이벤트를 설정합니다.
            /// </summary>
            /// <param name="executeEvent">실행 이벤트입니다.</param>
            /// <param name="quitEvent">종료 이벤트입니다.</param>
            public void SettingEvents(Action executeEvent, Action quitEvent)
            {
                ExecuteEvent = executeEvent;
                QuitEvent = quitEvent;
            }



            ///======================================================================================================================================================



            /// <summary>
            /// Normal 트랙을 추가한뒤,
            /// <para>받아온 <paramref name="normal"/>이 최고 Hindrance 값 보다 높다면,</para>
            /// <para><b><see cref="ExecuteEvent"/>를 실행한다</b></para>
            /// </summary>
            /// <param name="normal">추가할 Normal 값입니다.</param>
            public bool Add_NormalTrack_TryExecuteEvent(int normal)
            {
                if (Calculate_AddNormal(normal))
                {
                    ExecuteEvent?.Invoke();
                    return true;
                }
                return false;
            }



            /// <summary>
            /// <para>Normal 트랙을 제거한뒤,</para>
            /// <para> (더는 Normal 트랙이 존재하지 않거나) || </para>
            /// <para> (Hindrance 트랙이 존재하고 + Hindrance의 최고 값이 Normal 최고 값 보다 크다면)</para>
            /// <para><b><see cref="QuitEvent"/>를 실행한다</b></para>
            /// </summary>
            /// <param name="normal">제거할 Normal 값입니다.</param>
            public bool Remove_NormalTrack_TryQuitEvent(int normal)
            {
                if (Calculate_RemoveNormal(normal))
                {
                    QuitEvent?.Invoke();
                    return true;
                }
                return false;
            }



            /// <summary>
            /// Hindrance 트랙을 추가한뒤,
            /// <para> <paramref name="hindrance"/>이 Normal 트랙보다 높다면,</para>
            /// <para><b><see cref="QuitEvent"/>를 실행한다</b></para>
            /// </summary>
            /// <param name="hindrance">추가할 Hindrance 값입니다.</param>
            public bool Add_HindranceTrack_TryQuitEvent(int hindrance)
            {
                if (Calculate_AddHindrance(hindrance))
                {
                    QuitEvent?.Invoke();
                    return true;
                }
                return false;
            }



            /// <summary>
            /// <para>Hindrance 트랙을 제거한뒤,</para>
            /// <para> Normal 트랙이 존재하고 + Normal의 최고 값이 Hindrance의 최고 값 보다 크다면 </para>
            /// <para><b><see cref="ExecuteEvent"/>를 실행한다</b></para>
            /// </summary>
            /// <param name="hindrance">제거할 Hindrance 값입니다.</param>
            public bool Remove_HindranceTrack_TryExecuteEVent(int hindrance)
            {
                if (Calculate_RemoveHindrance(hindrance))
                {
                    ExecuteEvent?.Invoke();
                    return true;
                }
                return false;
            }



            ///======================================================================================================================================================
        }
    }



    /// <summary>
    /// 특정 데이터 타입을 처리하는 상호작용 매니저 클래스입니다.
    /// </summary>
    /// <typeparam name="T">처리할 데이터 타입입니다.</typeparam>
    public class InteractionManager<T> : BaseInteractionManager<InteractionManager<T>.Interaction>
    {
        /// <summary>
        /// 특정 데이터 타입을 처리하는 상호작용 클래스입니다.
        /// </summary>
        public class Interaction : BaseInteraction
        {
            ///======================================================================================================================================================



            private Action<T> ExecuteEvent { get; set; } = null;



            private Action<T> QuitEvent { get; set; } = null;



            ///======================================================================================================================================================



            /// <summary>
            /// 실행 및 종료 이벤트를 설정합니다.
            /// </summary>
            /// <param name="executeEvent">실행 이벤트입니다.</param>
            /// <param name="quitEvent">종료 이벤트입니다.</param>
            public void SettingEvents(Action<T> executeEvent, Action<T> quitEvent)
            {
                ExecuteEvent = executeEvent;
                QuitEvent = quitEvent;
            }



            ///======================================================================================================================================================



            /// <summary>
            /// Normal 트랙을 추가한뒤,
            /// <para>받아온 <paramref name="normal"/>이 최고 Hindrance 값 보다 높다면,</para>
            /// <para><b><see cref="ExecuteEvent"/>를 실행한다</b></para>
            /// </summary>
            /// <param name="normal">추가할 Normal 값입니다.</param>
            /// <param name="v1">추가적인 데이터입니다.</param>
            public bool Add_NormalTrack_TryExecuteEvent(int normal, T v1)
            {
                if (Calculate_AddNormal(normal))
                {
                    ExecuteEvent?.Invoke(v1);
                    return true;
                }
                return false;
            }



            /// <summary>
            /// <para>Normal 트랙을 제거한뒤,</para>
            /// <para> (더는 Normal 트랙이 존재하지 않거나) || </para>
            /// <para> (Hindrance 트랙이 존재하고 + Hindrance의 최고 값이 Normal 최고 값 보다 크다면)</para>
            /// <para><b><see cref="QuitEvent"/>를 실행한다</b></para>
            /// </summary>
            /// <param name="normal">제거할 Normal 값입니다.</param>
            /// <param name="v1">추가적인 데이터입니다.</param>
            public bool Remove_NormalTrack_TryQuitEvent(int normal, T v1)
            {
                if (Calculate_RemoveNormal(normal))
                {
                    QuitEvent?.Invoke(v1);
                    return true;
                }
                return false;
            }



            /// <summary>
            /// Hindrance 트랙을 추가한뒤,
            /// <para> <paramref name="hindrance"/>이 Normal 트랙보다 높다면,</para>
            /// <para><b><see cref="QuitEvent"/>를 실행한다</b></para>
            /// </summary>
            /// <param name="hindrance">추가할 Hindrance 값입니다.</param>
            /// <param name="v1">추가적인 데이터입니다.</param>
            public bool Add_HindranceTrack_TryQuitEvent(int hindrance, T v1)
            {
                if (Calculate_AddHindrance(hindrance))
                {
                    QuitEvent?.Invoke(v1);
                    return true;
                }
                return false;
            }



            /// <summary>
            /// <para>Hindrance 트랙을 제거한뒤,</para>
            /// <para> Normal 트랙이 존재하고 + Normal의 최고 값이 Hindrance의 최고 값 보다 크다면 </para>
            /// <para><b><see cref="ExecuteEvent"/>를 실행한다</b></para>
            /// </summary>
            /// <param name="hindrance">제거할 Hindrance 값입니다.</param>
            /// <param name="v1">추가적인 데이터입니다.</param>
            public bool Remove_HindranceTrack_TryExecuteEVent(int hindrance, T v1)
            {
                if (Calculate_RemoveHindrance(hindrance))
                {
                    ExecuteEvent?.Invoke(v1);
                    return true;
                }
                return false;
            }



            ///======================================================================================================================================================
        }
    }



    /// <summary>
    /// 두 개의 데이터 타입을 처리하는 상호작용 매니저 클래스입니다.
    /// </summary>
    /// <typeparam name="T1">첫 번째 데이터 타입입니다.</typeparam>
    /// <typeparam name="T2">두 번째 데이터 타입입니다.</typeparam>
    public class InteractionManager<T1, T2> : BaseInteractionManager<InteractionManager<T1, T2>.Interaction>
    {
        /// <summary>
        /// 두 개의 데이터 타입을 처리하는 상호작용 클래스입니다.
        /// </summary>
        public class Interaction : BaseInteraction
        {
            ///======================================================================================================================================================



            public Action<T1, T2> ExecuteEvent { get; set; } = null;



            public Action<T1, T2> QuitEvent { get; set; } = null;


            ///======================================================================================================================================================



            /// <summary>
            /// 실행 및 종료 이벤트를 설정합니다.
            /// </summary>
            /// <param name="executeEvent">실행 이벤트입니다.</param>
            /// <param name="quitEvent">종료 이벤트입니다.</param>
            public void SettingEvents(Action<T1, T2> executeEvent, Action<T1, T2> quitEvent)
            {
                ExecuteEvent = executeEvent;
                QuitEvent = quitEvent;
            }



            ///======================================================================================================================================================



            /// <summary>
            /// Normal 트랙을 추가한뒤,
            /// <para>받아온 <paramref name="normal"/>이 최고 Hindrance 값 보다 높다면,</para>
            /// <para><b><see cref="ExecuteEvent"/>를 실행한다</b></para>
            /// </summary>
            /// <param name="normal">추가할 Normal 값입니다.</param>
            /// <param name="v1">첫 번째 데이터입니다.</param>
            /// <param name="v2">두 번째 데이터입니다.</param>
            public bool Add_NormalTrack_TryExecuteEvent(int normal, T1 v1, T2 v2)
            {
                if (Calculate_AddNormal(normal))
                {
                    ExecuteEvent?.Invoke(v1, v2);
                    return true;
                }
                return false;
            }



            /// <summary>
            /// <para>Normal 트랙을 제거한뒤,</para>
            /// <para> (더는 Normal 트랙이 존재하지 않거나) || </para>
            /// <para> (Hindrance 트랙이 존재하고 + Hindrance의 최고 값이 Normal 최고 값 보다 크다면)</para>
            /// <para><b><see cref="QuitEvent"/>를 실행한다</b></para>
            /// </summary>
            /// <param name="normal">제거할 Normal 값입니다.</param>
            /// <param name="v1">첫 번째 데이터입니다.</param>
            /// <param name="v2">두 번째 데이터입니다.</param>
            public bool Remove_NormalTrack_TryQuitEvent(int normal, T1 v1, T2 v2)
            {
                if (Calculate_RemoveNormal(normal))
                {
                    QuitEvent?.Invoke(v1, v2);
                    return true;
                }
                return false;
            }



            /// <summary>
            /// Hindrance 트랙을 추가한뒤,
            /// <para> <paramref name="hindrance"/>이 Normal 트랙보다 높다면,</para>
            /// <para><b><see cref="QuitEvent"/>를 실행한다</b></para>
            /// </summary>
            /// <param name="hindrance">추가할 Hindrance 값입니다.</param>
            /// <param name="v1">첫 번째 데이터입니다.</param>
            /// <param name="v2">두 번째 데이터입니다.</param>
            public bool Add_HindranceTrack_TryQuitEvent(int hindrance, T1 v1, T2 v2)
            {
                if (Calculate_AddHindrance(hindrance))
                {
                    QuitEvent?.Invoke(v1, v2);
                    return true;
                }
                return false;
            }



            /// <summary>
            /// <para>Hindrance 트랙을 제거한뒤,</para>
            /// <para> Normal 트랙이 존재하고 + Normal의 최고 값이 Hindrance의 최고 값 보다 크다면 </para>
            /// <para><b><see cref="ExecuteEvent"/>를 실행한다</b></para>
            /// </summary>
            /// <param name="hindrance">제거할 Hindrance 값입니다.</param>
            /// <param name="v1">첫 번째 데이터입니다.</param>
            /// <param name="v2">두 번째 데이터입니다.</param>
            public bool Remove_HindranceTrack_TryExecuteEVent(int hindrance, T1 v1, T2 v2)
            {
                if (Calculate_RemoveHindrance(hindrance))
                {
                    ExecuteEvent?.Invoke(v1, v2);
                    return true;
                }
                return false;
            }



            ///======================================================================================================================================================
        }
    }



    ///======================================================================================================================================================



    #region GPT 개선 이전
    //public abstract class BaseInteractionManagerClass<TInteraction> where TInteraction : BaseInteractionManagerClass<TInteraction>.BaseInteraction, new()
    //{
    //    public abstract class BaseInteraction
    //    {
    //        ///======================================================================================================================================================

    //        private readonly HashSet<int> NormalTracks = new();
    //        private readonly HashSet<int> HindranceTracks = new();

    //        ///======================================================================================================================================================

    //        /// <summary>Normal 트랙의 개수</summary>
    //        public int Normal_Count => NormalTracks.Count;

    //        /// <summary>Hindrance 트랙의 개수</summary>
    //        public int Hindrance_Count => HindranceTracks.Count;

    //        /// <summary>Normal 트랙중 가장 큰 수</summary>
    //        public int Normal_Biggist { get; private set; } = 0;

    //        /// <summary>Hindrance 트랙중 가장 큰 수</summary>
    //        public int Hindrance_Biggist { get; private set; } = 0;

    //        ///======================================================================================================================================================

    //        /// <summary>Normal 트랙 추가하기</summary>
    //        protected void _AddNormal(int key)
    //        {
    //            NormalTracks.Add(key);
    //            Calculate_Normal();
    //        }

    //        /// <summary>Normal 트랙 제거하기</summary>
    //        protected void _RemoveNormal(int key)
    //        {
    //            NormalTracks.Remove(key);
    //            Calculate_Normal();
    //        }

    //        private void Calculate_Normal()
    //        {
    //            Normal_Biggist = 0;

    //            if (NormalTracks.Count == 0)
    //            {
    //                Normal_Biggist = 0;
    //                return;
    //            }

    //            foreach (var t in NormalTracks)
    //            {
    //                if (Normal_Biggist < t)
    //                {
    //                    Normal_Biggist = t;
    //                }
    //            }
    //        }

    //        ///======================================================================================================================================================

    //        /// <summary>Hindrance 트랙 추가하기</summary>
    //        protected void _AddHindrance(int track)
    //        {
    //            HindranceTracks.Add(track);
    //            Calculate_Hindrance();
    //        }

    //        /// <summary>Hindrance 트랙 제거하기</summary>
    //        protected void _RemoveHindrance(int track)
    //        {
    //            HindranceTracks.Remove(track);
    //            Calculate_Hindrance();
    //        }

    //        private void Calculate_Hindrance()
    //        {
    //            Hindrance_Biggist = 0;

    //            if (HindranceTracks.Count == 0)
    //            {
    //                Hindrance_Biggist = 0;
    //                return;
    //            }

    //            foreach (var t in HindranceTracks)
    //            {
    //                if (Hindrance_Biggist < t)
    //                {
    //                    Hindrance_Biggist = t;
    //                }
    //            }
    //        }

    //        ///======================================================================================================================================================

    //        protected bool Calculate_AddNormal(int normal)
    //        {
    //            //Normal이 추가된다
    //            _AddNormal(normal);

    //            if (Hindrance_Biggist < normal)
    //            {
    //                return true;
    //            }

    //            return false;
    //        }

    //        protected bool Calculate_RemoveNormal(int normal)
    //        {
    //            //Normal이 제거된다
    //            _RemoveNormal(normal);
    //            //모든 Normal이 제거되면 QUIT 한다
    //            //또는 방해꾼이 존재하고, 가장큰 Normal보다 가장큰Hindrance가 존재하면 애니메이션이 종료된다
    //            if (Normal_Count == 0 ||
    //                (Hindrance_Count != 0 && Normal_Biggist < Hindrance_Biggist))
    //            {
    //                return true;
    //            }
    //            return false;
    //        }

    //        protected bool Calculate_AddHindrance(int hindrance)
    //        {
    //            //Hindrance가 추가된다
    //            _AddHindrance(hindrance);

    //            //Debug.Log("방해꾼추가 : " + Normal_Biggist + " / " + hindranceTrack);

    //            //가장큰 Normal보다 Hindrance가 더 크면 QUIT 한다
    //            if (Normal_Biggist < hindrance)
    //            //if (Normal_Biggist < Hindrance_Biggist)
    //            {
    //                //Debug.Log("네애니메이션을종료합니다");
    //                return true;
    //            }

    //            return false;
    //        }

    //        protected bool Calculate_RemoveHindrance(int hindrance)
    //        {
    //            //Hindrance가 제거된다
    //            _RemoveHindrance(hindrance);
    //            //Debug.Log("방해꾼 종료됐다! 방해꾼 제거");
    //            //Normal이 존재하고, 가장큰 Hindrance보다 Normal이 더 크면 EXECUTE 한다
    //            if (Normal_Count != 0 && Hindrance_Biggist < Normal_Biggist)
    //            {
    //                //Debug.Log("노말카운트가 0이 아님, 큰방해꾼보다 큰노말이 더큼, 팜락부활");
    //                return true;
    //            }

    //            return false;
    //        }

    //        ///======================================================================================================================================================
    //    }

    //    private Dictionary<string, TInteraction> InteractionManagers;

    //    protected bool Active = false;

    //    public void SetActive(int capacity = 0)
    //    {
    //        Active = true;
    //        InteractionManagers = new Dictionary<string, TInteraction>(capacity);
    //    }

    //    private void CheckSetActive()
    //    {
    //        if (Active == false) { SetActive(); }
    //    }

    //    public TInteraction AddManger(string key)
    //    {
    //        CheckSetActive();

    //        if (InteractionManagers.ContainsKey(key)) { return InteractionManagers[key]; }

    //        InteractionManagers.Add(key, new TInteraction());
    //        return InteractionManagers[key];
    //    }

    //    public bool RemoveManger(string key)
    //    {
    //        CheckSetActive();
    //        return InteractionManagers.Remove(key);
    //    }

    //    public TInteraction GetManager(string key)
    //    {
    //        CheckSetActive();
    //        if (InteractionManagers.ContainsKey(key)) { return InteractionManagers[key]; }
    //        return null;
    //    }

    //    public bool TryGetManager(string key, out TInteraction result)
    //    {
    //        CheckSetActive();
    //        if (InteractionManagers.ContainsKey(key))
    //        {
    //            result = InteractionManagers[key];
    //            return true;
    //        }

    //        result = null;
    //        return false;
    //    }

    //    public void Memory_TrimExcess(int capacity)
    //    {
    //        InteractionManagers.TrimExcess();
    //    }
    //}



    //public class InteractionManagerClass : BaseInteractionManagerClass<InteractionManagerClass.Interaction>
    //{
    //    public class Interaction : BaseInteraction
    //    {
    //        private Action ExecuteEvent;
    //        private Action QuitEvent;

    //        public void SettingEvents(Action executeEvent, Action quitEvent)
    //        {
    //            ExecuteEvent = executeEvent;
    //            QuitEvent = quitEvent;
    //        }

    //        public void Add_Normal(int normal)
    //        {
    //            if (Calculate_AddNormal(normal))
    //            {
    //                ExecuteEvent?.Invoke();
    //            }

    //            //Normal이 추가된다
    //            //가장큰 Hindrance보다 Normal이 더 크면 EXECUTE 한다
    //        }

    //        public void Remove_Normal(int normal)
    //        {
    //            if (Calculate_RemoveNormal(normal))
    //            {
    //                QuitEvent?.Invoke();
    //            }

    //            //Normal이 제거된다
    //            //모든 Normal이 제거되면 QUIT 한다
    //            //또는 방해꾼이 존재하고, 가장큰 Normal보다 가장큰Hindrance가 존재하면 애니메이션이 종료된다
    //        }

    //        public void Add_Hindrance(int hindrance)
    //        {
    //            if (Calculate_AddHindrance(hindrance))
    //            {
    //                QuitEvent?.Invoke();
    //            }

    //            //Hindrance가 추가된다
    //            //가장큰 Normal보다 Hindrance가 더 크면 QUIT 한다
    //        }

    //        public void Remove_Hindrance(int hindrance)
    //        {
    //            if (Calculate_RemoveHindrance(hindrance))
    //            {
    //                ExecuteEvent?.Invoke();
    //            }

    //            //Hindrance가 제거된다
    //            //Normal이 존재하고, 가장큰 Hindrance보다 Normal이 더 크면 EXECUTE 한다
    //        }
    //    }
    //}



    //public class InteractionManagerClass<T> : BaseInteractionManagerClass<InteractionManagerClass<T>.Interaction>
    //{
    //    public class Interaction : BaseInteraction
    //    {
    //        private Action<T> ExecuteEvent;
    //        private Action<T> QuitEvent;

    //        public void SettingEvents(Action<T> executeEvent, Action<T> quitEvent)
    //        {
    //            ExecuteEvent = executeEvent;
    //            QuitEvent = quitEvent;
    //        }

    //        public void Add_Normal(int normal, T v1)
    //        {
    //            if (Calculate_AddNormal(normal))
    //            {
    //                ExecuteEvent?.Invoke(v1);
    //            }
    //        }

    //        public void Remove_Normal(int normal, T v1)
    //        {
    //            if (Calculate_RemoveNormal(normal))
    //            {
    //                QuitEvent?.Invoke(v1);
    //            }
    //        }

    //        public void Add_Hindrance(int hindrance, T v1)
    //        {
    //            if (Calculate_AddHindrance(hindrance))
    //            {
    //                QuitEvent?.Invoke(v1);
    //            }
    //        }

    //        public void Remove_Hindrance(int hindrance, T v1)
    //        {
    //            if (Calculate_RemoveHindrance(hindrance))
    //            {
    //                ExecuteEvent?.Invoke(v1);
    //            }
    //        }
    //    }
    //}



    //public class InteractionManagerClass<T1, T2> : BaseInteractionManagerClass<InteractionManagerClass<T1, T2>.Interaction>
    //{
    //    public class Interaction : BaseInteraction
    //    {
    //        public Action<T1, T2> ExecuteEvent { get; set; } = null;
    //        public Action<T1, T2> QuitEvent { get; set; } = null;

    //        //public void SettingEvents(Action<T1, T2> executeEvent, Action<T1, T2> quitEvent)
    //        //{
    //        //    ExecuteEvent = executeEvent;
    //        //    QuitEvent = quitEvent;
    //        //}

    //        public void Add_Normal(int normal, T1 v1, T2 v2)
    //        {
    //            if (Calculate_AddNormal(normal))
    //            {
    //                ExecuteEvent?.Invoke(v1, v2);
    //            }
    //        }

    //        public void Remove_Normal(int normal, T1 v1, T2 v2)
    //        {
    //            if (Calculate_RemoveNormal(normal))
    //            {
    //                QuitEvent?.Invoke(v1, v2);
    //            }
    //        }

    //        public void Add_Hindrance(int hindrance, T1 v1, T2 v2)
    //        {
    //            if (Calculate_AddHindrance(hindrance))
    //            {
    //                QuitEvent?.Invoke(v1, v2);
    //            }
    //        }

    //        public void Remove_Hindrance(int hindrance, T1 v1, T2 v2)
    //        {
    //            if (Calculate_RemoveHindrance(hindrance))
    //            {
    //                ExecuteEvent?.Invoke(v1, v2);
    //            }
    //        }
    //    }
    //} 
    #endregion



    ///======================================================================================================================================================
}