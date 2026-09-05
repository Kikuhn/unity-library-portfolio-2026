using Pan.Util;
using Sirenix.OdinInspector;
using UnityEngine;



namespace Pan.Event
{
    [CreateAssetMenu(fileName = "PanEventGeneralSingletonSetting", menuName = PanEventCreateAssetMenuInfo.GENERAL_SETTING_SBJECT)]
    public class PanEventGeneralSettingSbject : CustomScriptableObjectSerialized
    {
        [SerializeField]
        [LabelText("PanEvent 초기화 설정")]
        private PanEventInitializeSettingSbject panEventInitializeSetting;
        public PanEventInitializeSettingSbject PanEventInitializeSetting => panEventInitializeSetting; [SerializeField]

        [LabelText("PanEventValue 초기화 설정")]
        private PanEventValueInitializeSettingSbject panEventValueInitializeSetting;
        public PanEventValueInitializeSettingSbject PanEventValueInitializeSetting => panEventValueInitializeSetting;
    }
}