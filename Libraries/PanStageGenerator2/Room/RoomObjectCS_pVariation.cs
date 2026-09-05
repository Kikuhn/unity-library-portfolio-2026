using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Pan.StageGenerators
{
    public partial class RoomObject
    {
        public interface IRoomVariation
        {
            void EnableVariation();
            void DisableVariation();
        }

        [Serializable]
        public class Variation : RoomComponent, IRoomVariation
        {
            ///======================================================================================================================================================



#if UNITY_EDITOR

            [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Center, EnableRichText = true, Overflow = false), EnableGUI]
            [PropertySpace(8, 8)]
            [PropertyOrder(0)]
            private string dummy_Title
            {
                get
                {
                    return $"<size=15><b>{((name == "") ? "noname" : name)}</b></size>";
                }
            }

#endif



            ///======================================================================================================================================================



            public string Name => name;
            [LabelText("바리에이션 이름")]
            [SerializeField]
            [PropertyOrder(1)]
            private string name;



            ///======================================================================================================================================================



            [LabelText("바리에이션 활성화 이벤트")]

            [SerializeField]
            [PropertyOrder(3)]
            private UnityEvent EnableVariationEvent;

            [LabelText("바리에이션 비활성화 이벤트")]
            [SerializeField]
            [PropertyOrder(3)]
            private UnityEvent DisableVariationEvent;



            ///======================================================================================================================================================



            /// <summary>
            /// 이 바리에이션을 활성화한다
            /// </summary>
            [ButtonGroup("바리에이션버튼그룹")]
            [Button("이 바리에이션 활성화", Icon = SdfIconType.LightningFill), GUIColor(0.97f, 0.85f, 0.39f)]
            [PropertyOrder(2)]
            public void EnableVariation()
            {
                EnableVariationEvent?.Invoke();
            }



            /// <summary>
            /// 이 바리에이션을 비활성화한다
            /// </summary>
            [ButtonGroup("바리에이션버튼그룹")]
            [Button("이 바리에이션 비활성화", Icon = SdfIconType.LightningFill), GUIColor(0.97f, 0.85f, 0.39f)]
            [PropertyOrder(2)]
            public void DisableVariation()
            {
                DisableVariationEvent?.Invoke();
            }



            ///======================================================================================================================================================
        }
    }
}
