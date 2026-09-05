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
        [Serializable]
        public class GenerateInfoManager : BaseManager
        {
            ///======================================================================================================================================================



            ///<summary>
            ///활성화시, 생성되기 이전에 강제로 하위 오브젝트들을 모두 파괴한다, 에디터에서 테스트용으로 사용됨
            /// </summary>
            [TitleGroup("생성 설정"), BoxGroup("생성 설정/박스", false)]
            [ShowInInspector]
            [LabelText("생성 전, 강제 파괴")]
            [PropertyOrder(0)]
            public bool UseDestroyBeforeGenerate
            {
                get => useDestroyBeforeGenerate;
                private set => useDestroyBeforeGenerate = value;
            }
            [SerializeField, HideInInspector] private bool useDestroyBeforeGenerate;



            /// <summary>
            /// 생성 실패시 재생성 이벤트
            /// </summary>
            [TitleGroup("생성 설정"), BoxGroup("생성 설정/박스", false)]
            [ShowInInspector]
            [LabelText("생성 실패 이벤트")]
            [PropertyOrder(2)]
            public EFailureGenerateEvent FailureGenerateEvent
            {
                get => failureGenerateEvent;
                private set => failureGenerateEvent = value;
            }
            [SerializeField, HideInInspector] private EFailureGenerateEvent failureGenerateEvent = EFailureGenerateEvent.Regenrate_Absolute;



            /// <summary>
            /// 재생성 최대 시행 횟수
            /// </summary>
            [TitleGroup("생성 설정"), BoxGroup("생성 설정/박스", false)]
            [ShowInInspector]
            [LabelText("최대 재생성 시행 횟수")]
            [Tooltip("최초 생성 1회 이후 허용할 추가 재시도 횟수입니다. 음수 값은 0회로 처리됩니다.")]
            [MinValue(0)]
            [PropertyOrder(3)]
            public int ReGenerateMaxCount { get => reGenerateMaxCount; private set => reGenerateMaxCount = value; }
            [SerializeField, HideInInspector] private int reGenerateMaxCount = 3;



            ///======================================================================================================================================================



            //? 랜덤 시드



            public CustomRandom Random => random;
            [SerializeField, HideInInspector] private CustomRandom random = new CustomRandom(CustomRandom.EMode.LateCreate);



            ///<summary>"다음에 적용될 시드" 기능 활성화 여부<br/>
            ///(<see cref="Seed_NextWillApplied"/>를 사용할지 여부)</summary>
            [TitleGroup("생성 설정"), BoxGroup("생성 설정/박스", false)]
            [ShowInInspector]
            [LabelText("시드 고정")]
            [PropertyOrder(10)]
            [Indent(0)]
            public bool UseSeed_NextWillApplied;



            ///<summary>
            ///다음에 적용될 시드
            ///</summary>
            [TitleGroup("생성 설정"), BoxGroup("생성 설정/박스", false)]
            [HorizontalGroup("생성 설정/박스/시드정보가로", 0.7f)]
            [EnableIf(nameof(UseSeed_NextWillApplied))]
            [ShowInInspector]
            [LabelText("다음 시드")]
            [LabelWidth(120)]
            [PropertyOrder(10)]
            [Indent(1)]
            public int Seed_NextWillApplied;



            [TitleGroup("생성 설정"), BoxGroup("생성 설정/박스", false)]
            [HorizontalGroup("생성 설정/박스/시드정보가로")]
            [EnableIf(nameof(UseSeed_NextWillApplied))]
            [Button("생성", Icon = SdfIconType.Dice6), GUIColor(0.67f, 0.57f, 0.93f)]
            [PropertyOrder(10)]
            [Indent(1)]
            private void GenerateRandomSeed()
            {
                CustomRandom.Instance.RefreshRandom();
                Seed_NextWillApplied = CustomRandom.Instance.Seed;
            }



            ///<summary>
            ///마지막으로 적용된 시드
            ///</summary>
            public int Seed_LastApplied => random.Seed;



            [TitleGroup("생성 설정"), BoxGroup("생성 설정/박스", false)]
            [HorizontalGroup("생성 설정/박스/시드정보가로2")]
            [ShowInInspector]
            [HideLabel, DisplayAsString(EnableRichText = true, Overflow = true), EnableGUI]
            [PropertyOrder(10)]
            [Indent(1)]
            private string Seed_LastAppliedInfoText { get => $"최근 적용된 시드: <color=#2ecc71>{Seed_LastApplied}</color>"; }



            [TitleGroup("생성 설정"), BoxGroup("생성 설정/박스", false)]
            [HorizontalGroup("생성 설정/박스/시드정보가로2")]
            [ButtonGroup("생성 설정/박스/시드정보가로2/버튼그룹")]
            [EnableIf(nameof(UseSeed_NextWillApplied))]
            [Button("다음 시드에 적용", Icon = SdfIconType.LightningFill, Stretch = false), GUIColor(0.97f, 0.85f, 0.39f)]
            [PropertyOrder(10)]
            private void ApplyLastAppliedSeedToNext()
            {
                Seed_NextWillApplied = Seed_LastApplied;
            }



            [TitleGroup("생성 설정"), BoxGroup("생성 설정/박스", false)]
            [HorizontalGroup("생성 설정/박스/시드정보가로2")]
            [ButtonGroup("생성 설정/박스/시드정보가로2/버튼그룹")]
            [EnableIf(nameof(UseSeed_NextWillApplied))]
            [Button("복사", Icon = SdfIconType.Clipboard, Stretch = false), GUIColor(0.18f, 0.80f, 0.44f)]
            [PropertyOrder(10)]
            private void CopyToClipBoardLastAppliedSeed()
            {
                GUIUtility.systemCopyBuffer = Seed_LastApplied.ToString();
            }



            ///======================================================================================================================================================



            [TitleGroup("생성 설정"), BoxGroup("생성 설정/박스", false)]
            [LabelText("커튼콜에서, 캐시 초기화")]
            [PropertyTooltip("생성 작업 마무리 단계에서 실행되며\n캐시와 관련된 요소들을 초기화 한다")]
            [PropertyOrder(11)]
            public bool CurtenCall_ClearCaches;



            ///======================================================================================================================================================



            ///<summary>무작위 랜덤 시드 생성,<br/>
            ///seed를 받아오면, 해당 시드로 적용된다.
            ///null을 받아오면, 시드가 무작위로 생성되어 적용된다
            /// </summary>
            /// <param name="customOtherSeed">
            /// null일 경우, 시드를 무작위로 생성하여 적용되며,
            /// null이 아닐경우, 해당 시드값이 적용된다
            /// </param>
            public bool RefreshRandom(int? customOtherSeed = null)
            {
                //? #1 파라미터로 받아온 커스텀 시드를 적용
                if (customOtherSeed.HasValue)
                {
                    random.RefreshRandom(customOtherSeed.Value);
                    return true;
                }



                //? #2 다음에 적용될 시드값을 적용
                if (UseSeed_NextWillApplied)
                {
                    random.RefreshRandom(Seed_NextWillApplied);
                    return true;
                }



                //? #3 시드를 무작위로 생성하여 적용
                random.RefreshRandom();

                return true;
            }



            ///<summary>
            ///무작위 랜덤 시드 생성,<br/>
            ///기존 설정값을 무시하고 진짜 무작위 시드가 지정된다
            /// </summary>
            public void RefreshRealyRandom()
            {
                random.RefreshRandom();
            }



            ///======================================================================================================================================================
        }
    }
}
