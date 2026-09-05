using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Pan.Util;
using System;
using System.Text;
using Cysharp.Threading.Tasks;
using Pan.GridCompatibles2;
using Pan.StageGenerators;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Threading;




namespace Pan.StageGenerators
{
    public partial class StageGenerator : MonoBehaviour
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 절차적 생성 결과 상태
        /// </summary>
        public enum EGenerateState
        {
            /// <summary>절차적 생성에 성공</summary>
            [LabelText("절차적 생성 성공")]
            Success,

            /// <summary>절차적 생성에 실패</summary>
            [LabelText("절차적 생성 실패")]
            Failure,

            /// <summary>절차적 생성에 절대실패, 어떠한 경우에도 재생성 되지 않음<br/>(재생성 이벤트보다 우선으로 적용됨)</summary>
            [LabelText("절차적 생성 절대 실패")]
            [PropertyTooltip("어떠한 경우에도 재생성 되지 않음 (재생성 이벤트보다 우선적으로 적용됨)")]
            AbsoluteFailure
        }



        /// <summary>
        /// 생성 실패시 재생성 이벤트 타입
        /// </summary>
        public enum EFailureGenerateEvent
        {
            /// <summary>시드 강제 재생성후 재시도 <i>(고정시드 유무 고려X ,고정시드라면 이를 해제함)</i></summary>
            [LabelText("시드 강제 재생성후 재시도")]
            [PropertyTooltip("(고정시드 유무 고려X ,고정시드라면 이를 해제함)")]
            Regenrate_Absolute,

            /// <summary>시드 재생성후 재시도 <i>(시드가 고정되어 있지 않다면!)</i></summary>
            [LabelText("시드 재생성후 재시도")]
            [PropertyTooltip("(시드가 고정되어있지 않다면!)")]
            ReGenerate_WhenFixSeedNotUsed,

            /// <summary>무조건 실패</summary>
            [LabelText("무조건 실패")]
            FailureAbsolute
        }



        private const string LOG_TITLE_DESTROYPREVIOUS = "DestroyPrevious";
        private const string LOG_TITLE_REFRESHRANDOM = "RefreshRandom";
        private const string LOG_TITLE_GENERATEGRIDMANAGER = "GenerateGridManager";
        private const string LOG_TITLE_GENERATESPACELIST = "GenerateSpaceList";
        private const string LOG_TITLE_PLACE = "Place";



        ///======================================================================================================================================================



#if UNITY_EDITOR

        [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Center, EnableRichText = true, Overflow = false), EnableGUI]
        [PropertyOrder(-9999)]
        [PropertySpace(8, 8)]
        private string dummy_Title
        {
            get
            {
                return $"<b><size=15>스테이지 생성기</size></b>\n" +
                    $"<size=11><i>(유효한 스테이지 설정, 계산 설정이 할당 되어 있어야 함)</i></size>\n" +
                    $"통합 유효성: " + (IsValid_TotalSettings ? $"✔️ <color=#2ecc71>Valid\n</color>" : $" ❌ <color=#ed5565>Invalid\n</color>") +
                    $"스테이지 생성 설정 유효성: " + (IsValid_Setting ? $"✔️ <color=#2ecc71>Valid\n</color>" : $" ❌ <color=#ed5565>Invalid\n</color>") +
                    $"계산 설정 유효성: " + (IsValid_CalculateSetting ? $"✔️ <color=#2ecc71>Valid\n</color>" : $" ❌ <color=#ed5565>Invalid\n</color>");
            }
        }

#endif



        #region 스테이지 생성 설정





        ///<summary>
        /// 스테이지 생성 설정 SO
        ///</summary>
        public StageGeneratorSettingSbject SettingSbject { get => settingSbject; }
        [TitleGroup("스테이지 생성기 설정"), BoxGroup("스테이지 생성기 설정/박스", false)]
        [ShowInInspector]
        [LabelText("스테이지 생성 설정")]
        [PropertyOrder(-100)]
        [InlineEditor]
        [OnValueChanged(nameof(Refresh_StageGeneratorAbsolute))]
        [InfoBox("가용성이 유효한 스테이지 설정이 할당 되어 있어야, 사용이 가능", InfoMessageType.Warning, VisibleIf = "@!IsValid_Setting")]
        [SerializeField] private StageGeneratorSettingSbject settingSbject;



        /// <summary>
        /// 스테이지 생성 설정 SO 할당 여부
        /// </summary>
        public bool IsValid_Setting => SettingSbject != null && SettingSbject.Setting.IsValid;



        //! null 에러 방지용, 더미 설정
        private static StageGeneratorSetting _settingDummy;
        private static StageGeneratorSetting settingDummy
        {
            get
            {
                _settingDummy ??= new StageGeneratorSetting();
                return _settingDummy;
            }
        }



        /// <summary>
        /// 스테이지 생성 설정을 얻는다
        /// </summary>
        public StageGeneratorSetting Setting
                => IsValid_Setting ? settingSbject.Setting : settingDummy;



        ///<summary>
        /// 스테이지 생성 설정의 프리팹 설정
        ///</summary>
        public StageGeneratorSettingPrefab PrefabSetting
            => Setting.PrefabSettingSbject.Setting;



        #endregion



        #region 스테이지 계산 설정



        ///<summary>
        /// 스테이지 생성 계산 설정 SO
        ///</summary>
        public StageGeneratorSettingCalculateSbject CalculateSettingSbject { get => calculateSettingSbject; }
        [TitleGroup("스테이지 생성기 설정"), BoxGroup("스테이지 생성기 설정/박스", false)]
        [ShowInInspector]
        [LabelText("스테이지 생성 계산 설정")]
        [PropertyOrder(-100)]
        [InlineEditor]
        [OnValueChanged(nameof(Refresh_StageGeneratorAbsolute))]
        [SerializeField] private StageGeneratorSettingCalculateSbject calculateSettingSbject;



        /// <summary>
        /// 스테이지 생성 계산 설정 SO 할당 여부
        /// </summary>
        public bool IsValid_CalculateSetting => CalculateSettingSbject != null;



        //! null 에러 방지용, 더미 설정
        private static StageGeneratorSettingCalculate _settingCalculateDummy = new StageGeneratorSettingCalculate();
        private static StageGeneratorSettingCalculate settingCalculateDummy
        {
            get
            {
                _settingCalculateDummy ??= new StageGeneratorSettingCalculate();
                return _settingCalculateDummy;
            }
        }



        ///<summary>
        /// 스테이지 생성 계산 설정
        ///</summary>
        public StageGeneratorSettingCalculate CalculateSetting
            => IsValid_CalculateSetting ? calculateSettingSbject.Setting : settingCalculateDummy;



        #endregion




        //. 필수 설정들이 모두 유효한지 확인
        public bool IsValid_TotalSettings => IsValid_Setting && IsValid_CalculateSetting;



        ///======================================================================================================================================================




        /// <summary>
        /// 이 <see cref="StageGenerator"/> 를 Main으로 만들어지는 하위 매니저 클래스의 Base
        /// </summary>
        public abstract class BaseManager : MainSlave_WakeUpVer<StageGenerator>
        {
            public StageGenerator StageGenerator => Main;

            public StageGeneratorSetting Setting => Main.Setting;

            public StageGeneratorSettingCalculate CalculateSetting => Main.CalculateSetting;
        }



        public GenerateInfoManager GenerateInfoM => generateInfoM;
        [SerializeField, InlineProperty, HideLabel]
        [ShowIf(nameof(IsValid_TotalSettings))]
        private GenerateInfoManager generateInfoM = new();



        public GenerateManager GenerateM => generateM;
        [SerializeField, InlineProperty, HideLabel]
        [ShowIf(nameof(IsValid_TotalSettings))]
        private GenerateManager generateM = new();



        public TransformManager TransformM => transformM;
        [SerializeField, InlineProperty, HideLabel]
        [ShowIf(nameof(IsValid_TotalSettings))]
        private TransformManager transformM = new();


        public PlaceManger PlaceM => placeM;
        [SerializeField, InlineProperty, HideLabel]
        [ShowIf(nameof(IsValid_TotalSettings))]
        private PlaceManger placeM = new();



        public GridManager GridM => gridM;
        [SerializeField, InlineProperty, HideLabel]
        [ShowIf(nameof(IsValid_TotalSettings))]
        private GridManager gridM = new();



        public SpaceManager SpaceM => spaceM;
        [SerializeField, InlineProperty, HideLabel]
        [ShowIf(nameof(IsValid_TotalSettings))]
        private SpaceManager spaceM = new();



        public DebugLogger LogM => logM;
        [SerializeField, InlineProperty, HideLabel]
        [ShowIf(nameof(IsValid_TotalSettings))]
        private DebugLogger logM = new DebugLogger();



#if UNITY_EDITOR



        //. 에디터에서 매니저들 상시 WakeUp 시켜주는 OnValidate
        private void OnValidate()
        {
            Refresh_StageGenerator(true);
        }



#endif


        //? 스테이지 생성기 갱신

        public void Refresh_StageGeneratorAbsolute()
        {
            Refresh_StageGenerator(true);
        }



        [OnInspectorInit]
        public void Refresh_StageGenerator(bool absolute)
        {
            //. 각 매니저들의 Main 외의 요소가 최대한 초기화 되지 않아야한다

            if (absolute || !transformM.IsWakeUp) transformM.WakeUp(this); //? 벡터들 캐싱하기위해 재정의됨
            if (absolute || !gridM.IsWakeUp) gridM.WakeUp(this);
            if (absolute || !spaceM.IsWakeUp) spaceM.WakeUp(this);
            if (absolute || !placeM.IsWakeUp) placeM.WakeUp(this); //? 내부에 매니저있어어 재정의됨
            if (absolute || !generateInfoM.IsWakeUp) generateInfoM.WakeUp(this);
            if (absolute || !generateM.IsWakeUp) generateM.WakeUp(this);
            if (absolute || !logM.IsWakeUp) logM.WakeUp(this);
        }



        ///======================================================================================================================================================



        private void Awake()
        {
            Refresh_StageGenerator(true);
        }



        ///======================================================================================================================================================



        //? 유니티에서 호출하기위한 생성 호출 메서드



        public void InvokeGenerate()
        {
            GenerateM.Generate();
        }



        public void InvokeGenerateAsync()
        {
            GenerateM.GenerateAsync().Forget();
        }



        ///======================================================================================================================================================
    }
}