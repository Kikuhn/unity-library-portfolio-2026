using Pan.Util;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;



namespace Pan.StageGenerators
{
    public partial class RoomObject
    {
        [Serializable]
        public partial class VariationManager : BaseManager
        {
            ///======================================================================================================================================================



            public override void WakeUp(RoomObject main)
            {
                base.WakeUp(main);

                foreach (var variation in Variations)
                {
                    variation.Initialize(main);
                }
            }



            ///======================================================================================================================================================



            //? 바리에이션



            ///<summary>
            ///바리에이션 목록
            ///</summary>
            [OnValueChanged(nameof(RefreshVariation), true)]
            [OnCollectionChanged(nameof(RefreshVariation))]
            [LabelText("바리에이션 전체 목록")]
            [SerializeField]
            private List<Variation> Variations = new List<Variation>();
            public IReadOnlyList<Variation> GetVariations => Variations;



            public int VariationsCount => Variations.Count;



            private void RefreshVariation()
            {
                foreach (var variation in Variations)
                {
                    variation.Initialize(Main);
                }
            }



            /// <summary>
            /// 현재 활성화되어있는 바리에이션
            /// </summary>
            [LabelText("현재 활성화 되어있는 바리에이션")]
            [ShowInInspector]
            public Variation EnabledVariation => enabledVariation;
            private Variation enabledVariation = null;



            //? 현재 활성화된 바리에이션 정보



            /// <summary>
            /// 바리에이션이 적용이 되어있는지 확인
            /// </summary>
            public bool IsActiveVariation => EnabledVariation != null;



            ///======================================================================================================================================================



            //? 바리에이션 활성화, 비활성화



            /// <summary>
            /// 대상 바리에이션을 활성화 시킨다
            /// </summary>
            private void EnableVariation(Variation variation)
            {
                if (IsActiveVariation)
                {
                    enabledVariation.DisableVariation();
                    enabledVariation = null;
                }

                variation.EnableVariation();
                enabledVariation = variation;
            }



            /// <summary>
            /// 활성화된 바리에이션이 있다면 비활성화 시킨다
            /// </summary>
            public bool DisableEnabledVariation()
            {
                if (IsActiveVariation)
                {
                    enabledVariation.DisableVariation();
                    enabledVariation = null;
                    return true;
                }
                return false;
            }



            /// <summary>
            /// 바리에이션을 index로 활성화 시킨다
            /// </summary>
            public bool EnableVariation_ByIndex(int index)
            {
                if (VariationsCount == 0) { return false; }

                //? 반드시 바리에이션을 얻어올수있게 조정하여 얻기
                var variation = Variations[Mathf.Clamp(index, 0, VariationsCount - 1)];

                //? 이미 활성화 되어있다면, 또 활성화할 필요가 없으니 실패
                if (IsActiveVariation && enabledVariation == variation) { return false; }

                EnableVariation(variation);

                return true;
            }



            ///<summary>
            ///무작위 바리에이션을 적용하기 위한 범위 Index를 얻어온다
            ///<para>바리에이션을 미적용 할 수도 있으며, 미적용은 <paramref name="min"/>의 최소값이 <c>0</c>이 아닌 <c>-1</c>도 포함하게된다</para>
            /// </summary>
            ///<param name="canApplyNonVariable">
            ///<para><c>true</c>: 바리에이션이 적용되지 않을수도 있음</para>
            ///<para><c>false</c>: 바리에이션중에 무조건 적용됨</para>
            ///</param>
            public void GetRandomVariationIndex(bool canApplyNonVariable, out int min, out int max)
            {
                if (VariationsCount == 0)
                {
                    min = 0;
                    max = 0;
                    return;
                }

                min = canApplyNonVariable ? -1 : 0;
                max = VariationsCount;
            }



            ///<summary>
            ///<see cref="CustomRandom"/>으로 무작위 바리에이션 하나를 활성화 시킨다
            ///</summary>
            ///<param name="canApplyNonVariable">
            ///<para><c>true</c>: 바리에이션이 적용되지 않을수도 있음</para>
            ///<para><c>false</c>: 바리에이션중에 무조건 적용됨</para>
            ///</param>
            public void EnableVariation_CustomRandom(CustomRandom random, bool canApplyNonVariable)
            {
                if (VariationsCount == 0) { return; }


                //? 기존에 활성화된 바리에이션이 있다면 비활성화
                DisableEnabledVariation();


                //. 무작위 바리에이션을 활성화 하기 위해 범위 Index를 얻어온다 (바리에이션 미적용 여부까지)
                GetRandomVariationIndex(canApplyNonVariable, out int min, out int max);


                //. 범위중에서 무작위로 선택한다
                int randomIndex = random.Range(min, max);


                //. 바리에이션 미적용 (-1) 이 아니라면, 바리에이션을 활성화한다
                if (randomIndex != -1) { EnableVariation_ByIndex(randomIndex); }
            }



#if UNITY_EDITOR

            [ButtonGroup("바리에이션버튼그룹")]
            [Button("무작위 바리에이션 활성화", Icon = SdfIconType.LightningFill), GUIColor(0.97f, 0.85f, 0.39f)]
            private void EnableRandomVariation()
            {
                EnableVariation_CustomRandom(CustomRandom.Instance, false);
                if (enabledVariation != null)
                {
                    Debug.Log($"\"{enabledVariation.Name}\" 바리에이션이 활성화됨");
                }
                else
                {
                    Debug.LogWarning($"바리에이션 활성화 실패");
                }
            }

            [ButtonGroup("바리에이션버튼그룹")]
            [Button("적용된 바리에이션 제거", Icon = SdfIconType.LightningFill), GUIColor(0.97f, 0.85f, 0.39f)]
            private void DisableEnableRandomVariation()
            {
                DisableEnabledVariation();
            }

            [ButtonGroup("바리에이션버튼그룹2")]
            [Button("모든 바리에이션 활성화 이벤트 실행", Icon = SdfIconType.LightningFill), GUIColor(0.97f, 0.85f, 0.39f)]
            [PropertyTooltip("단순 에디터 테스트 용도, 실제로 바리에이션이 적용되지는 않고 이벤트만 실행됨")]
            private void EnableRandomVariation_All()
            {
                foreach (var variation in Variations)
                {
                    variation.EnableVariation();
                    Debug.Log($"\"{variation.Name}\" 바리에이션이 활성화됨");
                }
            }

            [ButtonGroup("바리에이션버튼그룹2")]
            [Button("모든 바리에이션 비활성화 이벤트 실행", Icon = SdfIconType.LightningFill), GUIColor(0.97f, 0.85f, 0.39f)]
            [PropertyTooltip("단순 에디터 테스트 용도, 실제로 바리에이션이 적용되지는 않고 이벤트만 실행됨")]
            private void DisableeRandomVariation_All()
            {
                foreach (var variation in Variations)
                {
                    variation.DisableVariation();
                    Debug.Log($"\"{variation.Name}\" 바리에이션이 비활성화됨");
                }
            }
#endif



            ///======================================================================================================================================================
        }
    }
}
