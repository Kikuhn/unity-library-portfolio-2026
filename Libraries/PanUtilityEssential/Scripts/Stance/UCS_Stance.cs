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
using Sirenix.OdinInspector;



//? 반드시 하나의 상태만 유지하는 "Stance"를 정리한 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    public abstract class CustomStancePackage<T> where T : class
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 스탠스의 기본 인터페이스입니다.<br/>
        /// 각 스탠스는 시작, 종료, 중첩 메서드와 랭크를 정의해야 합니다.
        /// </summary>
        public interface IStance
        {
            bool CanStart(T target, IStance oldStance);

            /// <summary>
            /// 스탠스를 시작합니다.
            /// </summary>
            /// <param name="target">스탠스를 적용할 대상입니다.</param>
            /// <param name="oldStance">이전 스탠스입니다.</param>
            void StartStance(T target, IStance oldStance);

            /// <summary>
            /// 스탠스를 종료합니다.
            /// </summary>
            /// <param name="target">스탠스를 적용할 대상입니다.</param>
            /// <param name="newStance">새로운 스탠스입니다.</param>
            void EndStance(T target, IStance newStance);

            /// <summary>
            /// 현재 스탠스를 중첩하여 처리합니다.
            /// </summary>
            /// <param name="target">스탠스를 적용할 대상입니다.</param>
            void OverlapStance(T target);

            /// <summary>
            /// 스탠스의 랭크(우선순위)입니다.<br/>
            /// 높은 랭크의 스탠스가 낮은 랭크를 대체할 수 있습니다.
            /// </summary>
            int Rank { get; }
        }



        /// <summary>
        /// 동작을 정의할 수 있는 유연한(Flexible) 스탠스입니다.
        /// </summary>
        public sealed class FlexStance : IStance
        {
            public Func<T, IStance, bool> CanStartEvent;
            public Action<T, IStance> StartStanceEvent;
            public Action<T, IStance> EndStanceEvent;
            public Action<T> OverlapStanceEvent;

            public int Rank { get; set; }

            public bool CanStart(T target, IStance oldStance) => CanStartEvent?.Invoke(target, oldStance) ?? true;
            public void StartStance(T target, IStance oldStance) => StartStanceEvent?.Invoke(target, oldStance);
            public void EndStance(T target, IStance newStance) => EndStanceEvent?.Invoke(target, newStance);
            public void OverlapStance(T target) => OverlapStanceEvent?.Invoke(target);
        }



        /// <summary>
        /// 기본 스탠스를 정의하는 추상 클래스입니다.
        /// </summary>
        public abstract class BaseStance : IStance
        {
            public virtual bool CanStart(T target, IStance oldStance) => true;
            public virtual void StartStance(T target, IStance oldStance) { }
            public virtual void EndStance(T target, IStance newStance) { }
            public virtual void OverlapStance(T target) { }
            public virtual int Rank { get; } = 0;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 스탠스를 관리하는 기본 매니저 클래스입니다.
        /// </summary>
        public class Manager
        {
#if UNITY_EDITOR

            [ShowInInspector, DisplayAsString(EnableRichText = true), HideLabel, EnableGUI]
            public string Editor_CurrentStanceName => $"CurrentStance: <b>{CurrentStance?.GetType().Name ?? "None"}</b>";

#endif

            /// <summary>
            /// 현재 활성화된 스탠스입니다.
            /// </summary>
            [ShowInInspector]
            public IStance CurrentStance { get; protected set; } = null;



            /// <summary>
            /// 새로운 스탠스로 전환합니다.
            /// </summary>
            /// <param name="target">스탠스를 적용할 대상입니다.</param>
            /// <param name="stance">새로운 스탠스입니다.</param>
            public bool TryChangeStance(T target, IStance stance)
            {
                //! 활성화된 스탠스와 똑같은 스탠스를 받아올경우
                //?     [스탠스 중첩] 실행후 리턴
                if (CurrentStance == stance) { CurrentStance.OverlapStance(target); return false; }


                //! 활성화된 스탠스가 존재하고, 받아온 스탠스의 랭크가 더 낮을경우, 리턴
                if (CurrentStance != null && CurrentStance.Rank > stance.Rank) { return false; }


                //! 받아온 스탠스의 시작 조건이 충족되지않으면, 리턴
                if (stance.CanStart(target, CurrentStance) == false) { return false; }


                //. 기존 스탠스 종료 (존재했다면), 새로운 스탠스 시작
                CurrentStance?.EndStance(target, stance);
                stance.StartStance(target, CurrentStance);
                CurrentStance = stance;
                return true;
            }



            /// <summary>
            /// 현재 스탠스를 제거합니다.
            /// </summary>
            /// <param name="target">스탠스를 적용할 대상입니다.</param>
            /// <param name="endStance">종료 처리를 수행할지 여부입니다.</param>
            /// <returns>스탠스 제거 성공 여부입니다.</returns>
            public bool KillStance(T target, bool endStance = true)
            {
                if (CurrentStance == null) return false;

                if (endStance) CurrentStance.EndStance(target, null);
                CurrentStance = null;
                return true;
            }
        }



        /// <summary>
        /// 특정 기본 스탠스를 관리하는 매니저 클래스입니다.
        /// </summary>
        public class Manager<TBaseStance> : Manager where TBaseStance : BaseStance
        {
            public Manager(Action<TBaseStance> afterEvent = null)
            {
                SU_Collection_Types.CreateTypeDictionary(SU_Collection_Types.TypeSearch.ConcreteClasses, out Stances, afterEvent);
            }



            protected readonly Dictionary<Type, TBaseStance> Stances;



            /// <summary>
            /// 특정 스탠스로 전환합니다.
            /// </summary>
            /// <typeparam name="TStance">전환할 스탠스의 타입입니다.</typeparam>
            /// <param name="target">스탠스를 적용할 대상입니다.</param>
            public bool TryChangeStance<TStance>(T target) where TStance : TBaseStance, new()
            {
                if (Stances.TryGetValue(typeof(TStance), out var value))
                {
                    return TryChangeStance(target, value);
                }

                return false;
            }
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================



    //? 미니 스탠스 (UCS_Delegate)



    /// <summary>
    /// 간단한 스탠스 전환 및 관리 기능을 제공하는 클래스입니다.
    /// </summary>
    public class MiniStanceCenter
    {
        /// <summary>
        /// 기본 생성자로, 스탠스 전환 이벤트를 초기화합니다.
        /// </summary>
        public MiniStanceCenter()
        {
            ChangeStanceEvent = ChangeStance;
        }



        /// <summary>
        /// 현재 활성화된 스탠스입니다.
        /// </summary>
        private ICustomExecuteEvent NowStance;



        /// <summary>
        /// 스탠스를 변경하는 이벤트입니다.
        /// </summary>
        public readonly Action<ICustomExecuteEvent> ChangeStanceEvent;



        /// <summary>
        /// 현재 스탠스를 변경합니다.
        /// </summary>
        /// <param name="stance">새로운 스탠스입니다.</param>
        public void ChangeStance(ICustomExecuteEvent stance)
        {
            //? 첫 번째 스탠스 전환일 경우
            if (NowStance == null)
            {
                NowStance = stance;
                NowStance.Start(); // 스탠스 시작
                return;
            }

            //? 이미 스탠스가 존재하는 경우
            if (NowStance != null)
            {
                //? 동일한 스탠스를 또 받아왔다면 Overlap 호출
                if (NowStance == stance)
                {
                    NowStance.Overlap();
                    return;
                }

                //? 다른 스탠스를 받아왔다면 기존 스탠스를 종료 후 변경
                NowStance.End(); // 기존 스탠스 종료
                NowStance = stance;
                NowStance.Start(); // 새로운 스탠스 시작
            }
        }



        /// <summary>
        /// 현재 스탠스를 제거합니다.
        /// </summary>
        /// <param name="endStance">스탠스를 종료할지 여부입니다.</param>
        /// <returns>스탠스 제거 성공 여부를 반환합니다.</returns>
        public bool KillStance(bool endStance)
        {
            //? 현재 스탠스가 없으면 실패
            if (NowStance == null) { return false; }

            //? 스탠스 종료가 허용된 경우 종료 호출
            if (endStance) { NowStance.End(); }

            //? 스탠스를 비움
            NowStance = null;
            return true;
        }
    }



    ///======================================================================================================================================================
}