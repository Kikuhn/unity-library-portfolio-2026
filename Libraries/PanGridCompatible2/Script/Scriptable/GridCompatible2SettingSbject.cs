using Sirenix.OdinInspector;
using UnityEngine;

namespace Pan.GridCompatibles2
{
    [CreateAssetMenu(fileName = nameof(GridCompatible2SettingSbject), menuName = CreateAssetMenuInfo.GRIDCOMPATIBLE_SETTING)]
    public class GridCompatible2SettingSbject : ScriptableObject
    {
        [Title("그리드 호환 설정 SO", HorizontalLine = false, TitleAlignment = TitleAlignments.Centered)]
        [SerializeField]
        [InlineProperty, HideLabel]
        private GridCompatible2 gridCompatible = new();
        public GridCompatible2 GridCompatible => gridCompatible;
    }
}
