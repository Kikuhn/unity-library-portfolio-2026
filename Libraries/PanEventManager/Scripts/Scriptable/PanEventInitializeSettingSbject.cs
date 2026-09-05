using Pan.Util;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.Utilities;
using Sirenix.Serialization;
using System.Linq.Expressions;



namespace Pan.Event
{
    /// <summary>
    /// <see cref="PanEventInitializeSetting"/>의 리스트를 보유
    /// </summary>
    public interface IHoldPanEventInitializeSetting
    {
        IReadOnlyList<PanEventInitializeSetting> GetPanEventInitializeSettings { get; }
    }



    /// <summary>
    /// <see cref="PanBaseEvent"/>의 초기화 정보가 들어있는 클래스
    /// <para>이 클래스를 List에 담아 <see cref="PanEventInitializeSettingSbject"/> 에서 관리한다</para>
    /// </summary>
    [Serializable]
    [InlineProperty]
    [HideReferenceObjectPicker]
    public class PanEventInitializeSetting
    {
        ///======================================================================================================================================================



        private PanEventInitializeSetting(IHoldPanEventInitializeSetting list)
        {
            PanEventInitializeSettingList = list;
        }



        public static PanEventInitializeSetting Initialize(Type type, IHoldPanEventInitializeSetting list)
        {
            var result = new PanEventInitializeSetting(list);
            result.PanBaseEventType = type;
            result.UsageProfile = PanEventsInitializeSettingSbjectBase.GetDefaultUsageProfile(type);
            result.SetTypeNamePanBaseEvent();
            return result;
        }



        public static PanEventInitializeSetting Initialize<T>(IHoldPanEventInitializeSetting list) where T : PanBaseEvent, new()
        {
            return Initialize(typeof(T), list);
        }



        [field: HideInInspector]
        public IHoldPanEventInitializeSetting PanEventInitializeSettingList { get; set; }



        ///======================================================================================================================================================



        public void RefreshType(Type type, IHoldPanEventInitializeSetting list)
        {
            PanEventInitializeSettingList = list;
            PanBaseEventType = type;
            SetTypeNamePanBaseEvent();
        }



        public string GetTypeIdPanBaseEvent
        {
            get
            {
                EnsureTypeInfo();
                return TypeIdPanBaseEvent;
            }
        }



        public string GetAssemblyName => PanBaseEventType?.Assembly.GetName().Name;



        public bool IsRuntimeUsageProfile => UsageProfile == PanEventUsageProfile.Runtime;



        public bool IsInitializeTarget(bool runtimeProfileOnly)
        {
            return IsValid && (!runtimeProfileOnly || IsRuntimeUsageProfile);
        }



        ///======================================================================================================================================================



        #region 인스펙터에 필요한 요소

        private IEnumerable<Type> Inspector_GetTypes
        {
            get
            {
                if (PanEventInitializeSettingList == null)
                    return Array.Empty<Type>();

                var usedTypes = new HashSet<Type>();
                foreach (var setting in PanEventInitializeSettingList.GetPanEventInitializeSettings)
                {
                    if (setting != this && setting.PanBaseEventType != null)
                    {
                        usedTypes.Add(setting.PanBaseEventType);
                    }
                }

                return PanEventInitializeSettingSbject.GetPanBaseEventTypesAll()
                    .Where(type => !usedTypes.Contains(type));
            }
        }

        private string Inspector_PanBaseEventTypeLabel
        {
            get
            {
                if (PanBaseEventType == null)
                {
                    return "* PanBaseEvent 타입";
                }
                else
                {
                    return "PanBaseEvent 타입";
                }
            }
        }

        private string Inspector_TypeMissingInfoBoxMessage => $"<size=12>Missing <b>{TypeNamePanBaseEvent}</b></size>";

        private string Inspector_PanBaseEventTitleGroupName => (IsValid ? TypeNamePanBaseEvent : (TypeNamePanBaseEvent != null ? $"{TypeNamePanBaseEvent} (마지막으로 지정된 타입)" : "Type 미지정"));

        private Color Inspector_PanBaseEventTypeColor()
        {
            return Color.white;
        }

        private bool Inspector_TypeMissingInfoBoxMessageCondition => !IsValid && TypeNamePanBaseEvent != null;

        #endregion



        ///======================================================================================================================================================



        //? Key인 PanBaseEvent의 타입



        [Sirenix.OdinInspector.ReadOnly, OdinSerialize, ShowInInspector, ValueDropdown(nameof(Inspector_GetTypes)), OnValueChanged(nameof(SetTypeNamePanBaseEvent)), GUIColor(nameof(Inspector_PanBaseEventTypeColor))]
        [InfoBox("@Inspector_TypeMissingInfoBoxMessage", InfoMessageType.Error, VisibleIf = nameof(Inspector_TypeMissingInfoBoxMessageCondition))]
        [HideLabel]
        [VerticalGroup("타입그룹")]
        [BoxGroup("타입그룹/타입박스그룹", ShowLabel = false)]
        ///<summary>
        /// 이 타입의 <see cref="PanBaseEvent"/>의 초기화 정보의 설정값이 이 클래스에 지정되어있음
        /// <para>이 타입이 곧 <c>Key</c> 라고 봐도 무방</para>
        /// </summary>
        private Type PanBaseEventType;



        ///<summary>
        /// 이 타입의 <see cref="PanBaseEvent"/>의 초기화 정보의 설정값이 이 클래스에 지정되어있음
        /// <para>이 타입이 곧 <c>Key</c> 라고 봐도 무방</para>
        /// </summary>
        public Type GetPanBaseEventType => PanBaseEventType;



        ///======================================================================================================================================================



        //? PanBaseEvent를 지정할때 타입의 이름 문자열을 캐싱



        [field: SerializeField, HideInInspector]
        ///<summary>
        ///<see cref="PanBaseEventType"/>가 지정될때마다, 그 타입의 이름 문자열을 이 필드에 저장하여 캐싱한다
        /// </summary>
        public string TypeNamePanBaseEvent { get; private set; }



        [field: SerializeField, HideInInspector]
        ///<summary>
        /// 타입 이름이 충돌해도 같은 타입을 다시 찾기 위한 안정적인 타입 ID
        /// </summary>
        public string TypeIdPanBaseEvent { get; private set; }



        private void SetTypeNamePanBaseEvent()
        {
            if (PanBaseEventType != null)
            {
                TypeNamePanBaseEvent = PanBaseEventType.Name;
                TypeIdPanBaseEvent = PanEventsInitializeSettingSbjectBase.GetStableTypeId(PanBaseEventType);
            }
            else
            {
                TypeNamePanBaseEvent = null;
                TypeIdPanBaseEvent = null;
            }
        }



        private void EnsureTypeInfo()
        {
            if (PanBaseEventType == null) { return; }

            if (string.IsNullOrEmpty(TypeNamePanBaseEvent))
            {
                TypeNamePanBaseEvent = PanBaseEventType.Name;
            }

            if (string.IsNullOrEmpty(TypeIdPanBaseEvent))
            {
                TypeIdPanBaseEvent = PanEventsInitializeSettingSbjectBase.GetStableTypeId(PanBaseEventType);
            }
        }



        ///======================================================================================================================================================



        //? 이 클래스의 정보확인



        /// <summary>
        /// 이 클래스가 유효한가
        /// <para><see cref="PanBaseEventType"/>의 null여부에 따라 결정</para>
        /// </summary>
        public bool IsValid => PanBaseEventType != null;



        ///======================================================================================================================================================



        //? 핵심 설정 필드



        [BoxGroup("타입그룹/설정 값", ShowLabel = false), HorizontalGroup("타입그룹/설정 값/프로필가로그룹", Width = 0.5f), EnableIf(nameof(IsValid))]
        [LabelText("프로필")]
        [LabelWidth(60)]
        public PanEventUsageProfile UsageProfile = PanEventUsageProfile.Runtime;



        [BoxGroup("타입그룹/설정 값", ShowLabel = false), HorizontalGroup("타입그룹/설정 값/프로필가로그룹", Width = 0.5f), EnableIf(nameof(IsValid))]
        [LabelText("초기 자동 초기화")]
        [LabelWidth(110)]
        [Tooltip("활성화시, 이벤트 매니저가 초기화 될때 이 이벤트도 같이 생성된다")]
        public bool AutoInitialize;



        ///======================================================================================================================================================
    }



    [CreateAssetMenu(fileName = "PanEventInitializeSetting", menuName = PanEventCreateAssetMenuInfo.EVENT_INITIALIZESBJECT)]
    public class PanEventInitializeSettingSbject : PanEventsInitializeSettingSbjectBase, IHoldPanEventInitializeSetting
    {
        ///======================================================================================================================================================



        protected override bool Inspector_ShowCondition_InitializeSettings_InValid => PanEventInitializeSettings_InValid.Count != 0;



        ///======================================================================================================================================================



        [OdinSerialize, SerializeField, ShowInInspector]
        [PropertyOrder(-20)]
        [TitleGroup("프로필")]
        [LabelText("Runtime 프로필만 초기화에 사용")]
        [InfoBox("@Inspector_ProfileSummary", InfoMessageType.None)]
        [ToggleLeft]
        private bool UseRuntimeProfileOnlyForInitialize = false;



        public bool IsUseRuntimeProfileOnlyForInitialize => UseRuntimeProfileOnlyForInitialize;



        private string Inspector_ProfileSummary => BuildProfileSummary(PanEventInitializeSettings);



        private string BuildProfileSummary(IReadOnlyList<PanEventInitializeSetting> settings)
        {
            int total = 0;
            int runtime = 0;
            int validationOnly = 0;
            int legacy = 0;
            int experimental = 0;

            if (settings != null)
            {
                for (int i = 0; i < settings.Count; i++)
                {
                    var setting = settings[i];
                    if (setting == null || !setting.IsValid) { continue; }

                    total++;

                    switch (setting.UsageProfile)
                    {
                        case PanEventUsageProfile.Runtime:
                            runtime++;
                            break;
                        case PanEventUsageProfile.ValidationOnly:
                            validationOnly++;
                            break;
                        case PanEventUsageProfile.Legacy:
                            legacy++;
                            break;
                        case PanEventUsageProfile.Experimental:
                            experimental++;
                            break;
                    }
                }
            }

            int target = UseRuntimeProfileOnlyForInitialize ? runtime : total;

            return $"프로필 요약\n전체 {total} / 현재 초기화 대상 {target}\nRuntime {runtime} / ValidationOnly {validationOnly}\nLegacy {legacy} / Experimental {experimental}";
        }



        ///======================================================================================================================================================



        //? 이벤트 초기화 설정 목록 그리기



        [OdinSerialize, SerializeField, ShowInInspector]
        [ListDrawerSettings(ShowFoldout = true, NumberOfItemsPerPage = 20, DraggableItems = false, HideRemoveButton = true, HideAddButton = true)]
        [Searchable]
        [TitleGroup("초기화 설정 목록")]
        [LabelText("초기화 설정 목록", Icon = SdfIconType.FileEarmarkCodeFill)]
        [PropertySpace(SpaceAfter = 20)]
        ///<summary>
        /// 모든 <see cref="PanBaseEvent"/>가 각기 타입으로 등록되어있는 (이는 매번 갱신하여 보장) 초기화 설정 리스트
        /// </summary>
        private List<PanEventInitializeSetting> PanEventInitializeSettings = new();



        ///<summary>
        /// 모든 <see cref="PanBaseEvent"/>가 각기 타입으로 등록되어있는 (이는 매번 갱신하여 보장) 초기화 설정 리스트 를 얻기
        /// </summary>
        public IReadOnlyList<PanEventInitializeSetting> GetPanEventInitializeSettings => PanEventInitializeSettings;



        [OdinSerialize, SerializeField, ShowInInspector, ShowIf(nameof(Inspector_ShowCondition_InitializeSettings_InValid))]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = false, HideAddButton = true), HideDuplicateReferenceBox]
        [Searchable]
        [LabelText("Missing인 설정", Icon = SdfIconType.QuestionSquareFill), HideLabel]
        [TitleGroup("Missing인 설정 객체 존재", HorizontalLine = false)]
        ///<summary>
        /// 모든 <see cref="PanBaseEvent"/>가 각기 타입으로 등록되어있는 (이는 매번 갱신하여 보장) 초기화 설정 리스트 에서,
        /// <para>객체 삭제 / 이름 변경 등으로 인해 유효하지 않는 타입들이 격리되어있는 리스트</para>
        /// </summary>
        private List<PanEventInitializeSetting> PanEventInitializeSettings_InValid = new();



        /// <summary>
        /// Missing인 초기화 설정 객체가 있는지 여부
        /// </summary>
        public bool IsInitializeSettings_HasInValid => PanEventInitializeSettings_InValid.Count == 0;



        /// <summary>
        /// Missing인 초기화 설정 객체가 있는지 확인하고
        /// <para>Missing인 설정 객체가 존재한다면 그것들을 반환함</para>
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool CheckInitializeSettings_InValid(out List<PanEventInitializeSetting> result)
        {
            result = PanEventInitializeSettings_InValid;
            return IsInitializeSettings_HasInValid;
        }



        ///======================================================================================================================================================



        //? 이 설정 SO를 통해 PanEventManager 초기화



        ///<summary>
        /// 이 <see cref="PanEventInitializeSettingSbject"/> 의 설정값을 통해, <see cref="PanBaseEvent"/> 들의 타입 딕셔너리를 만들어 반환한다
        /// </summary>
        public void CreateEventDictionaryFromThisSettings(out Dictionary<Type, PanBaseEvent> events)
        {
            var targetSettings = PanEventInitializeSettings
                .Where(x => x != null && x.IsInitializeTarget(UseRuntimeProfileOnlyForInitialize))
                .ToList();

            events = new Dictionary<Type, PanBaseEvent>(targetSettings.Count);
            for (int i = 0; i < targetSettings.Count; i++)
            {
                var currentItem = targetSettings[i];
                var key = currentItem.GetPanBaseEventType;
                var value = (currentItem.AutoInitialize) ? Activator.CreateInstance(key) as PanBaseEvent : null;
                events.Add(key, value);
                //. Key는 모두 사용하고, 초기화 할 필요가 없는 Value를 null을 넣는 식으로 작동한다
            }
        }



        ///======================================================================================================================================================



        //? 초기화



        [Button("Missing 리스트 초기화", Icon = SdfIconType.EraserFill), GUIColor(0.93f, 0.33f, 0.40f), ShowIf(nameof(Inspector_ShowCondition_InitializeSettings_InValid))]
        [PropertySpace(SpaceAfter = 20)]
        public void Clear_PanEventInitializeSettings_InValid()
        {
            PanEventInitializeSettings_InValid.Clear();
        }



        ///<summary>
        /// 직렬화를 초기화 하지 않는 선에서 초기화 (갱신)
        /// </summary>
        [TitleGroup("초기화")]
        [ButtonGroup("초기화/버튼그룹"), Button("갱신 및 초기화", Icon = SdfIconType.ArrowDownUp), GUIColor(0.31f, 0.76f, 0.91f)]
        public void InitializeSettings()
        {
            var filteredTypes = GetPanBaseEventTypesAll();
            var previousSettings = PanEventInitializeSettings
                .Where(x => x != null)
                .ToList();

            AddInvalidSettings(previousSettings.Where(x => !x.IsValid));

            var previousSettingsByTypeId = previousSettings
                .Where(x => x.IsValid && !string.IsNullOrEmpty(x.GetTypeIdPanBaseEvent))
                .GroupBy(x => x.GetTypeIdPanBaseEvent)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);

            var nextSettings = new List<PanEventInitializeSetting>(filteredTypes.Length);

            foreach (var type in filteredTypes)
            {
                var typeId = PanEventsInitializeSettingSbjectBase.GetStableTypeId(type);

                if (previousSettingsByTypeId.TryGetValue(typeId, out var setting))
                {
                    setting.RefreshType(type, this);
                    nextSettings.Add(setting);
                }
                else
                {
                    nextSettings.Add(PanEventInitializeSetting.Initialize(type, this));
                }
            }

            PanEventInitializeSettings = nextSettings.OrderBy(x => x.GetPanBaseEventType?.Name).ToList();

#if UNITY_EDITOR
            SaveSettingsAsset();
#endif
        }



        private void AddInvalidSettings(IEnumerable<PanEventInitializeSetting> settings)
        {
            foreach (var setting in settings)
            {
                if (setting == null || setting.IsValid) { continue; }

                bool alreadyExists = PanEventInitializeSettings_InValid.Any(x => x != null && x.TypeNamePanBaseEvent == setting.TypeNamePanBaseEvent);

                if (!alreadyExists)
                {
                    PanEventInitializeSettings_InValid.Add(setting);
                }
            }
        }



        ///======================================================================================================================================================



        //? 일괄 작업



        [TitleGroup("일괄 작업")]
        [ButtonGroup("일괄 작업/자동초기화"), Button("일괄 AutoInitialize 활성화", Icon = SdfIconType.LightningFill), GUIColor(0.97f, 0.85f, 0.39f)]
        public void TaskBatch_SetEnable_AutoInitialize()
        {
            foreach (var item in PanEventInitializeSettings)
            {
                item.AutoInitialize = true;
            }

#if UNITY_EDITOR
            SaveSettingsAsset();
#endif
        }



        [TitleGroup("일괄 작업")]
        [ButtonGroup("일괄 작업/자동초기화"), Button("일괄 AutoInitialize 비활성화", Icon = SdfIconType.LightningFill), GUIColor(0.97f, 0.85f, 0.39f)]
        public void TaskBatch_DisEnable_AutoInitialize()
        {
            foreach (var item in PanEventInitializeSettings)
            {
                item.AutoInitialize = false;
            }

#if UNITY_EDITOR
            SaveSettingsAsset();
#endif
        }



        [TitleGroup("일괄 작업"), HorizontalGroup("일괄 작업/프로필일괄")]
        [LabelText("프로필 (일괄)"), LabelWidth(100)]
        public PanEventUsageProfile TaskBatch_UsageProfile = PanEventUsageProfile.Runtime;



        [TitleGroup("일괄 작업"), HorizontalGroup("일괄 작업/프로필일괄")]
        [Button("프로필 일괄 지정", Icon = SdfIconType.LightningFill), GUIColor(0.97f, 0.85f, 0.39f)]
        public void TaskBatch_SetUsageProfile()
        {
            foreach (var item in PanEventInitializeSettings)
            {
                if (item == null || !item.IsValid) { continue; }

                item.UsageProfile = TaskBatch_UsageProfile;
            }

#if UNITY_EDITOR
            SaveSettingsAsset();
#endif
        }



        [TitleGroup("일괄 작업")]
        [LabelText("Assembly 이름"), LabelWidth(100)]
        public string TaskBatch_AssemblyName;



        [TitleGroup("일괄 작업")]
        [LabelText("프로필"), LabelWidth(100)]
        public PanEventUsageProfile TaskBatch_AssemblyUsageProfile = PanEventUsageProfile.ValidationOnly;



        [TitleGroup("일괄 작업")]
        [Button("Assembly 이름 기준 프로필 지정", Icon = SdfIconType.LightningFill), GUIColor(0.97f, 0.85f, 0.39f)]
        public void TaskBatch_SetUsageProfile_ByAssemblyName()
        {
            TaskBatch_SetUsageProfile_ByAssemblyName(TaskBatch_AssemblyName, TaskBatch_AssemblyUsageProfile);
        }



        public void TaskBatch_SetUsageProfile_ByAssemblyName(string assemblyName, PanEventUsageProfile usageProfile)
        {
            if (string.IsNullOrWhiteSpace(assemblyName)) { return; }

            foreach (var item in PanEventInitializeSettings)
            {
                if (item == null || !item.IsValid) { continue; }

                if (string.Equals(item.GetAssemblyName, assemblyName, StringComparison.Ordinal))
                {
                    item.UsageProfile = usageProfile;
                }
            }

#if UNITY_EDITOR
            SaveSettingsAsset();
#endif
        }



        ///======================================================================================================================================================
    }



    #region Legacy
    ///// <summary>
    ///// <see cref="PanEventInitializeSetting"/>의 리스트를 보유
    ///// </summary>
    //public interface IHoldPanEventInitializeSetting
    //{
    //    IReadOnlyList<PanEventInitializeSetting> GetPanEventInitializeSettings { get; }
    //}



    ///// <summary>
    ///// <see cref="PanBaseEvent"/>의 초기화 정보가 들어있는 클래스
    ///// <para>이 클래스를 List에 담아 <see cref="PanEventInitializeSettingSbject"/> 에서 관리한다</para>
    ///// </summary>
    //[Serializable]
    //[InlineProperty]
    //[HideReferenceObjectPicker]
    //public class PanEventInitializeSetting
    //{
    //    ///======================================================================================================================================================



    //    public PanEventInitializeSetting(IHoldPanEventInitializeSetting list)
    //    {
    //        PanEventInitializeSettingList = list;
    //    }



    //    /// <summary>
    //    /// 이 
    //    /// </summary>
    //    [field: HideInInspector]
    //    public IHoldPanEventInitializeSetting PanEventInitializeSettingList { get; set; }



    //    ///======================================================================================================================================================



    //    #region 인스펙터에 필요한 요소

    //    private IEnumerable<Type> Inspector_GetTypes
    //    {
    //        get
    //        {
    //            if (PanEventInitializeSettingList == null)
    //                return Array.Empty<Type>();

    //            var usedTypes = PanEventInitializeSettingList.GetPanEventInitializeSettings
    //                .Where(setting => setting != this && setting.PanBaseEventType != null)
    //                .Select(setting => setting.PanBaseEventType);

    //            return PanEventInitializeSettingSbject.GetCachingTypes()
    //                .Where(type => !usedTypes.Contains(type));
    //        }
    //    }

    //    private string Inspector_PanBaseEventTypeLabel
    //    {
    //        get
    //        {
    //            if (PanBaseEventType == null)
    //            {
    //                return "* PanBaseEvent 타입";
    //            }
    //            else
    //            {
    //                return "PanBaseEvent 타입";
    //            }
    //        }
    //    }

    //    private string Inspector_TypeMissingInfoBoxMessage => $"<size=12>타입 Missing! 재지정 필요\n마지막으로 지정된 이름: <b>{typeNamePanBaseEvent}</b></size>";

    //    private string Inspector_PanBaseEventTitleGroupName => PanBaseEventType != null ? PanBaseEventType.Name : "Type 미지정";

    //    private Color Inspector_PanBaseEventTypeColor()
    //    {
    //        if (PanBaseEventType == null)
    //        {
    //            //? 기존에 타입이 지정 되어 있었으나, 찾을수 없을때는 "붉은색"
    //            if (typeNamePanBaseEvent != null)
    //            {
    //                return Color.red;
    //            }
    //            //? 맨 처음 타입 지정이 필요할땐 "노란색"
    //            else
    //            {
    //                return Color.yellow;
    //            }
    //        }

    //        return Color.white;
    //    }

    //    private bool Inspector_TypeMissingInfoBoxMessageCondition => PanBaseEventType == null && typeNamePanBaseEvent != null;

    //    #endregion



    //    ///======================================================================================================================================================



    //    //? Key인 PanBaseEvent의 타입



    //    [OdinSerialize, ShowInInspector, ValueDropdown(nameof(Inspector_GetTypes)), OnValueChanged(nameof(SetTypeNamePanBaseEvent)), GUIColor(nameof(Inspector_PanBaseEventTypeColor))]
    //    [LabelText("@Inspector_PanBaseEventTypeLabel")]
    //    [InfoBox("@Inspector_TypeMissingInfoBoxMessage", InfoMessageType.Error, VisibleIf = nameof(Inspector_TypeMissingInfoBoxMessageCondition))]
    //    [TitleGroup("@Inspector_PanBaseEventTitleGroupName", HorizontalLine = false)]
    //    ///<summary>
    //    /// 이 타입의 <see cref="PanBaseEvent"/>의 초기화 정보의 설정값이 이 클래스에 지정되어있음
    //    /// <para>이 타입이 곧 <c>Key</c> 라고 봐도 무방</para>
    //    /// </summary>
    //    private Type PanBaseEventType;



    //    ///<summary>
    //    /// 이 타입의 <see cref="PanBaseEvent"/>의 초기화 정보의 설정값이 이 클래스에 지정되어있음
    //    /// <para>이 타입이 곧 <c>Key</c> 라고 봐도 무방</para>
    //    /// </summary>
    //    public Type GetPanBaseEventType => PanBaseEventType;



    //    ///======================================================================================================================================================



    //    //? PanBaseEvent를 지정할때 타입의 이름 문자열을 캐싱



    //    private string TypeNamePanBaseEvent
    //    {
    //        get => typeNamePanBaseEvent;
    //        set
    //        {
    //            SetTypeNamePanBaseEvent();
    //        }
    //    }

    //    [SerializeField, HideInInspector]
    //    ///<summary>
    //    ///<see cref="PanBaseEventType"/>가 지정될때마다, 그 타입의 이름 문자열을 이 필드에 저장하여 캐싱한다
    //    /// </summary>
    //    private string typeNamePanBaseEvent;



    //    private void SetTypeNamePanBaseEvent()
    //    {
    //        if (PanBaseEventType != null)
    //        {
    //            typeNamePanBaseEvent = PanBaseEventType.Name;
    //        }
    //        else
    //        {
    //            typeNamePanBaseEvent = null;
    //        }
    //    }



    //    ///======================================================================================================================================================



    //    //? 이 클래스의 정보확인



    //    /// <summary>
    //    /// 이 클래스가 유효한가
    //    /// <para><see cref="PanBaseEventType"/>의 null여부에 따라 결정</para>
    //    /// </summary>
    //    public bool IsValid => PanBaseEventType != null;



    //    ///======================================================================================================================================================



    //    [BoxGroup("설정 값", VisibleIf = nameof(IsValid))]
    //    [LabelText("자동 초기화")]
    //    [Tooltip("활성화시, 이벤트 매니저가 초기화 될때 이 이벤트도 같이 생성된다")]
    //    public bool AutoInitialize;



    //    ///======================================================================================================================================================
    //}



    //[CreateAssetMenu(fileName = "TESTTTT", menuName = "테스트트트")]
    //public class PanEventInitializeSettingSbject : CustomScriptableObjectSerialized, IHoldPanEventInitializeSetting
    //{
    //    private static Type[] cachingTypes;
    //    private static string[] cachingTypeNames;



    //    public static Type[] GetCachingTypes()
    //    {
    //        if (cachingTypes == null)
    //        {
    //            cachingTypes = SU_Collection_Types.GetTypesAssignableTo(typeof(PanBaseEvent), SU_Collection_Types.TypeSearch.ConcreteClasses);
    //        }

    //        return cachingTypes;
    //    }

    //    public static string[] GetCachingTypeNames()
    //    {
    //        if (cachingTypeNames == null)
    //        {
    //            cachingTypeNames = GetCachingTypes().Select(x => x.Name).ToArray();
    //        }

    //        return cachingTypeNames;
    //    }

    //    public static Type GetCachingTypeFromTypeName(string typeName)
    //    {
    //        var types = GetCachingTypes();

    //        return types.First(x => x.Name == typeName);
    //    }



    //    [OdinSerialize, NonSerialized, ShowInInspector, OnValueChanged(nameof(RefreshGetPanEventInitializeSettings))]
    //    [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, CustomAddFunction = nameof(CreateNewPanBaseEventBridgeSetting))]
    //    private List<PanEventInitializeSetting> PanEventInitializeSettings = new();



    //    private PanEventInitializeSetting CreateNewPanBaseEventBridgeSetting()
    //    {
    //        return new PanEventInitializeSetting(this);
    //    }



    //    public IReadOnlyList<PanEventInitializeSetting> GetPanEventInitializeSettings => PanEventInitializeSettings;



    //    public void RefreshGetPanEventInitializeSettings()
    //    {
    //        for (int i = 0; i < PanEventInitializeSettings.Count; i++)
    //        {
    //            PanEventInitializeSettings[i].PanEventInitializeSettingList = this;
    //        }
    //    }
    //} 
    #endregion
}
