namespace Pan.Event
{
    /// <summary>
    /// PanEvent와 EventValue 런타임을 한 번에 초기화하는 전역 진입점입니다.
    /// </summary>
    public static class PanEventGeneralManager
    {
        ///======================================================================================================================================================



        //? 초기화와 런타임 소유권



        /// <summary>
        /// PanEvent와 EventValue manager를 최초 한 번 초기화합니다.
        /// </summary>
        /// <param name="panEventInitializeSetting">
        /// Event 설정입니다. null이면 reflection으로 Event 타입을 검색합니다.
        /// </param>
        /// <param name="panEventValueInitializeSetting">
        /// EventValue 설정입니다. null이면 타입별 빈 pool만 등록하고 최초 대여 시 인스턴스를 생성합니다.
        /// </param>
        public static void Initialize(PanEventInitializeSettingSbject panEventInitializeSetting = null, PanEventValueInitializeSettingSbject panEventValueInitializeSetting = null)
        {
            if (IsInitialize)
            {
                return;
            }


            //? 설정 기반 초기화와 reflection 초기화는 같은 manager 계약을 제공해야 합니다.
            if (panEventInitializeSetting != null)
            {
                EventManager = PanEventManager.Initialize_byInitializeSettings(panEventInitializeSetting);
            }
            else
            {
                EventManager = PanEventManager.Initialize_byReflection();
            }


            if (panEventValueInitializeSetting != null)
            {
                EventValueManager = PanEventValueManager.Initialize_byInitializeSettings(panEventValueInitializeSetting);
            }
            else
            {
                EventValueManager = PanEventValueManager.Initialize_EachCreatCount();
            }


            IsInitialize = true;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 전역 Event·EventValue manager 초기화가 끝났는지 나타냅니다.
        /// </summary>
        public static bool IsInitialize { get; private set; }



        ///======================================================================================================================================================



        //? 런타임 상태와 진단



        /// <summary>
        /// 현재 세션의 Event manager입니다.
        /// </summary>
        public static PanEventManager EventManager { get; private set; }



        /// <summary>
        /// 현재 세션의 EventValue manager와 pool 소유자입니다.
        /// </summary>
        public static PanEventValueManager EventValueManager { get; private set; }



        ///======================================================================================================================================================
    }
}
