using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Events;
using DG.Tweening;
using Pan.Util;
using System.Reflection.Emit;
using Sirenix.OdinInspector;



//? [CentralizedUpdateManager] 중앙-집중형 업데이트 매니저 가 정리되어있는 정도의 코드



namespace Pan.Util
{
    /// <summary>
    /// Update 메서드들을, 하나의 게임오브젝트에서,
    /// <para>중앙-집중 형으로 효율적이게 관리하기 위한 업데이터</para>
    /// </summary>
    [Serializable]
    public class CentralizedUpdater
    {
        ///======================================================================================================================================================



        ///<summary>
        /// 중앙-집중형 업데이터 생성 (capacity 각각 지정)
        /// </summary>
        public CentralizedUpdater(bool debugLogErrorEnabled, int capacity_UpdateModules = 0, int capacity_WaitersWillAdd = 1, int capacity_WaitersWillRemove = 1)
        {
            DebugLogErrorEnabled = debugLogErrorEnabled;
            UpdateModules = new IndexedSet<IUpdateModule>(Mathf.Max(1, capacity_UpdateModules));
            Waiters_WillAdd = new List<IUpdateModule>(Mathf.Max(1, capacity_WaitersWillAdd));
            Waiters_WillRemove = new List<IUpdateModule>(Mathf.Max(1, capacity_WaitersWillRemove));
        }



        ///<summary>
        /// 업데이트 모듈 캐싱 (Waiters 리스트의 capacity는 <paramref name="capacity"/>의 <b>1/4</b> 로 자동지정)
        /// </summary>
        public CentralizedUpdater(bool debugLogErrorEnabled, int capacity = 0) : this(debugLogErrorEnabled, capacity, Mathf.Max(1, capacity / 4), Mathf.Max(1, capacity / 4))
        {
            DebugLogErrorEnabled = debugLogErrorEnabled;

        }



        ///======================================================================================================================================================



        /// <summary>
        /// 순회 대상인 업데이트 메서드들의 콜렉션
        /// </summary>
        private readonly IndexedSet<IUpdateModule> UpdateModules;



        /// <summary>
        /// <see cref="ExecuteUpdateModules"/> 메세드의 순회 도중,
        /// <para><b>Update 이벤트 추가</b>를 순회가 끝난 뒤에 시행하기 위한 콜렉션</para>
        /// </summary>
        private readonly List<IUpdateModule> Waiters_WillAdd;



        /// <summary>
        /// <see cref="ExecuteUpdateModules"/> 메세드의 순회 도중,
        /// <para><b>Update 이벤트 제거</b>를 순회가 끝난 뒤에 시행하기 위한 콜렉션</para>
        /// </summary>
        private readonly List<IUpdateModule> Waiters_WillRemove;



        /// <summary>
        /// 내부에서 스레드 안전성을 위해 사용하는 락 오브젝트입니다.
        /// </summary>
        private readonly object _lockObj = new object();



#if UNITY_EDITOR

        [BoxGroup("중앙-집중형 업데이터")]
        [LabelText("업데이트 모듈 개수")]
        [ShowInInspector, DisplayAsString, EnableGUI]
        private int editor_UpdateModuleCount => UpdateModules != null ? UpdateModules.Count : -1;

        [BoxGroup("중앙-집중형 업데이터")]
        [LabelText("대기 WillAdd 업데이트 모듈 개수")]
        [ShowInInspector, DisplayAsString, EnableGUI]
        private int editor_WaitersWillAddCount => Waiters_WillAdd != null ? Waiters_WillAdd.Count : -1;

        [BoxGroup("중앙-집중형 업데이터")]
        [LabelText("대기 WillRemove 업데이트 모듈 개수")]
        [ShowInInspector, DisplayAsString, EnableGUI]
        private int editor_WaitersWillRemoveCount => Waiters_WillRemove != null ? Waiters_WillRemove.Count : -1;

#endif



        public bool DebugLogErrorEnabled = true;



        ///======================================================================================================================================================



        //? 순회용 변수



        /// <summary>
        /// 현재 Update 루프가 진행 중인지 여부를 나타냅니다.
        /// </summary>
        [BoxGroup("중앙-집중형 업데이터")]
        [ShowInInspector]
        public bool IsLooping { get; private set; }



        ///======================================================================================================================================================



        /// <summary>
        /// 업데이트 모듈을 <b>추가</b>
        /// </summary>
        /// <param name="updateModule">등록할 모듈</param>
        public void AddUpdateModule(IUpdateModule updateModule)
        {
            lock (_lockObj)
            {
                //? "순회" 중이라면, 순회가 끝난 뒤에 "추가" 되게끔 예약한다
                if (IsLooping) { Waiters_WillAdd.Add(updateModule); }

                //. "순회" 중이 아니라면, 즉시 "추가" 되게끔 한다
                else { UpdateModules.Add(updateModule); }
            }
        }



        /// <summary>
        /// 업데이트 모듈을 <b>제거</b>
        /// </summary>
        /// <param name="updateModule">제거할 모듈</param>
        public void RemoveUpdateModule(IUpdateModule updateModule)
        {
            lock (_lockObj)
            {
                //? "순회" 중이라면, 순회가 끝난 뒤에 "제거" 되게끔 예약한다
                if (IsLooping) { Waiters_WillRemove.Add(updateModule); }

                //. "순회" 중이 아니라면, 즉시 "제거" 되게끔 한다
                else { UpdateModules.Remove(updateModule); }
            }
        }



        ///======================================================================================================================================================



        /// <summary>
        /// <see cref="UpdateModules"/>에 담긴 모든 업데이트 모듈을 순회하면서 실행
        /// <para>순회 도중에 업데이트 모듈이 추가되었다면, 순회가 끝난뒤 추가</para>
        /// <para>순회 도중, 비활성화된 모듈은 실행하지 않고, 순회가 끝난뒤 제거</para>
        /// </summary>
        public void ExecuteUpdateModules()
        {
            //. Update가 비어있다면, return (의도된 대로라면, 애초에 Update가 비어있다면 이 메서드가 호출되지않긴함)
            if (UpdateModules.Count == 0) { return; }

            //? --- 순회 시작 ---

            IsLooping = true;

            for (int i = 0; i < UpdateModules.Count; i++)
            {
                //. 현재 순회의 업데이트
                var update = UpdateModules[i];

                //! 비활성화되어있다면, 제거를 예약하고 continue
                if (!update.EnabledUpdateModule)
                {
                    Waiters_WillRemove.Add(update);
                    continue;
                }

                //. 현재 순회의 업데이트를 실행한다
                //! 에러 발생시, 다른 업데이트 모듈에 영향이 가지 않게 예외처리를 한다
                try { update.ExecuteModuleUpdate(); }
                catch (Exception e)
                {
                    if (DebugLogErrorEnabled) { Debug.LogError($"{e.Message}\n{e.StackTrace}"); }
                }
            }

            IsLooping = false;

            //? --- 순회 종료 ---


            //? "추가"가 예약되어있는 업데이트 모듈 추가
            if (Waiters_WillAdd.Count != 0)
            {
                UpdateModules.AddRange(Waiters_WillAdd);
                Waiters_WillAdd.Clear();
            }

            //? "제거"가 예약되어있는 업데이트 모듈 추가
            if (Waiters_WillRemove.Count != 0)
            {
                UpdateModules.RemoveRange(Waiters_WillRemove);
                Waiters_WillRemove.Clear();
            }
        }



        ///======================================================================================================================================================
    }



    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public class CentralizedUpdateSingleTonManager : SingleTon<CentralizedUpdateSingleTonManager>
    {
        ///======================================================================================================================================================



        [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Center, EnableRichText = true, Overflow = false), EnableGUI]
        [PropertyOrder(-100)]
        private string editor_Title
        {
            get
            {
                return "<b><size=14>중앙-집중형 업데이트 싱글톤 매니저</size></b>\n 이곳에 유니티 기본 Update, LateUpdate, FixedUpdate 가 존재한다\n각 업데이트에서 업데이터 내의 객체들의 업데이트 이벤트가 호출된다";
            }
        }



        ///======================================================================================================================================================



        //? 업데이터



        /// <summary>
        /// Update 될 때 마다 실행되는 업데이터
        /// </summary>
        [TitleGroup("업데이터"), BoxGroup("업데이터/Update 업데이터")]
        [InlineProperty, HideLabel, ShowInInspector, EnableGUI]
        [PropertyOrder(-90)]
        public CentralizedUpdater Updater => updater;
        [NonSerialized, HideInInspector] private CentralizedUpdater updater;



        [TitleGroup("업데이터"), BoxGroup("업데이터/박스", false)]
        [LabelText("초기 용량")]
        [ReadOnlyCustom(true)]
        [PropertyOrder(-89)]
        [Indent(1)]
        public int UpdaterInitialCapacity;



        /// <summary>
        /// LateUpdate 될 때 마다 실행되는 업데이터
        /// </summary>
        [TitleGroup("업데이터"), BoxGroup("업데이터/LateUpdate 업데이터")]
        [InlineProperty, HideLabel, ShowInInspector, EnableGUI]
        [PropertyOrder(-87)]
        public CentralizedUpdater LateUpdater => lateUpdater;
        [NonSerialized, HideInInspector] private CentralizedUpdater lateUpdater;



        [TitleGroup("업데이터"), BoxGroup("업데이터/LateUpdate 업데이터")]
        [ShowInInspector]
        [LabelText("초기 용량")]
        [ReadOnlyCustom(true)]
        [PropertyOrder(-86)]
        [Indent(1)]
        public int LateUpdaterInitialCapacity;



        /// <summary>
        /// FixedUpdate 될 때 마다 실행되는 업데이터
        /// </summary>
        [TitleGroup("업데이터"), BoxGroup("업데이터/FixedUpdate 업데이터")]
        [InlineProperty, HideLabel, ShowInInspector, EnableGUI]
        [PropertyOrder(-85)]
        public CentralizedUpdater FixedUpdater => fixedUpdater;
        [NonSerialized, HideInInspector] private CentralizedUpdater fixedUpdater;



        [TitleGroup("업데이터"), BoxGroup("업데이터/FixedUpdate 업데이터")]
        [LabelText("초기 용량")]
        [ReadOnlyCustom(true)]
        [PropertyOrder(-84)]
        [Indent(1)]
        public int FixedUpdaterInitialCapacity;



        ///======================================================================================================================================================



        //? 추가 업데이트 이벤트



        /// <summary>
        /// Update <b>이전</b>에 실행되는 이벤트
        /// </summary>
        public event Action PreUpdateEvent;
        /// <summary>
        /// Update <b>이후</b>에 실행되는 이벤트
        /// </summary>
        public event Action PostUpdateEvent;



        /// <summary>
        /// LateUpdate <b>이전</b>에 실행되는 이벤트
        /// </summary>
        public event Action PreLateUpdateEvent;
        /// <summary>
        /// LateUpdate <b>이후</b>에 실행되는 이벤트
        /// </summary>
        public event Action PostLateUpdateEvent;



        /// <summary>
        /// FixedUpdate <b>이전</b>에 실행되는 이벤트
        /// </summary>
        public event Action PreFixedUpdateEvent;
        /// <summary>
        /// FixedUpdate <b>이후</b>에 실행되는 이벤트
        /// </summary>
        public event Action PostFixedUpdateEvent;



        ///======================================================================================================================================================



        [TitleGroup("업데이터"), BoxGroup("설정")]
        [LabelText("에러 디버그 로그 사용")]
        [ReadOnlyCustom(true)]
        [PropertyOrder(-83)]
        public bool DebugLogErrorEnabled = true;



        ///======================================================================================================================================================



        protected override void Awake()
        {
            base.Awake();
            updater = new CentralizedUpdater(DebugLogErrorEnabled, UpdaterInitialCapacity);
            lateUpdater = new CentralizedUpdater(DebugLogErrorEnabled, LateUpdaterInitialCapacity);
            fixedUpdater = new CentralizedUpdater(DebugLogErrorEnabled, FixedUpdaterInitialCapacity);
        }



        private void Update()
        {
            PreUpdateEvent?.Invoke();

            updater.ExecuteUpdateModules();

            PostUpdateEvent?.Invoke();
        }



        private void LateUpdate()
        {
            PreLateUpdateEvent?.Invoke();

            lateUpdater.ExecuteUpdateModules();

            PostLateUpdateEvent?.Invoke();
        }



        private void FixedUpdate()
        {
            PreFixedUpdateEvent?.Invoke();

            fixedUpdater.ExecuteUpdateModules();

            PostFixedUpdateEvent?.Invoke();
        }



        ///======================================================================================================================================================
    }
}