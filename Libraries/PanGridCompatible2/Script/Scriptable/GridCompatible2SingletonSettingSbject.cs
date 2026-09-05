using Pan.Util;
using Sirenix.OdinInspector;
using UnityEngine;



namespace Pan.GridCompatibles2
{
    [CreateAssetMenu(fileName = nameof(GridCompatible2SingletonSettingSbject), menuName = CreateAssetMenuInfo.GRIDCOMPATIBLE_SINGLETON_SETTING)]
    public class GridCompatible2SingletonSettingSbject : SingleTon_ScriptableObject<GridCompatible2SingletonSettingSbject>
    {
        [TitleGroup("그리드 기즈모")]
        [LabelText("그리드 기즈모 사용")]
        public bool UseGridGizmo = true;

        [TitleGroup("그리드 기즈모")]
        [EnableIf(nameof(UseGridGizmo))]
        [Indent(1)]
        [LabelText("선택 되었을 때만 사용")]
        public bool UseGridGizmoOnlySelect = false;

        [TitleGroup("그리드 기즈모")]
        [EnableIf(nameof(UseGridGizmo))]
        [Indent(1)]
        [LabelText("그리드 색")]
        public ToggleColor GridColor = new(Color.white);

        [TitleGroup("그리드 기즈모")]
        [EnableIf(nameof(UseGridGizmo))]
        [Indent(1)]
        [LabelText("그리드 구분선 색")]
        public ToggleColor GridLineColor = new(Color.gray.WithMultipliedAlpha(0.5f));

        [TitleGroup("그리드 기즈모")]
        [EnableIf(nameof(UseGridGizmo))]
        [Indent(1)]
        [LabelText("그리드 중앙 표식 색")]
        public ToggleColor GridCenterColor = new ToggleColor(Color.red);

        [TitleGroup("그리드 기즈모")]
        [EnableIf(nameof(UseGridGizmo))]
        [Indent(1)]
        [LabelText("바닥 좌표 깊이 화살표 색")]
        public ToggleColor FloorToCeilingArrowColor = new ToggleColor(Color.red);



        [TitleGroup("그리드 기즈모")]
        [EnableIf(nameof(UseGridGizmo))]
        [Indent(1)]
        [Range(0, 1)]
        [LabelText("기즈모 표식 반지름 정도 (GridUnit)")]
        public float GridUnitLength_GizmoMarkRadius = 0.5f;

        /// <summary>
        /// 기즈모: 표식 지름 얻기
        /// </summary>
        /// <param name="snapSetting"></param>
        public float GetGizmoMarkRadius(GridCompatible2SnapSetting snapSetting)
        {
            return snapSetting.GridUnitAverageCorrection * GridUnitLength_GizmoMarkRadius * 0.5f;
        }

        /// <summary>
        /// 기즈모: 표식 지름 얻기
        /// </summary>
        /// <param name="snapSetting"></param>
        public float GetGizmoMarkRadius(GridCompatible2 gridCompatible)
        => GetGizmoMarkRadius(gridCompatible.CurrentSnapSetting);



        [TitleGroup("그리드 기즈모")]
        [EnableIf(nameof(UseGridGizmo))]
        [Indent(1)]
        [Range(0, 1)]
        [LabelText("기즈모 텍스트 보정 거리(GridUnit)")]
        public float GridUnitLength_GizmoTextCorrectionDistance = 0.2f;

        /// <summary>
        /// 기즈모: 텍스트 보정 거리 벡터 얻기
        /// </summary>
        /// <param name="snapSetting"></param>
        public Vector3 GetGizmoTextCorrectionDistance(GridCompatible2SnapSetting snapSetting)
        {
            float length = snapSetting.GridUnitAverageCorrection * GridUnitLength_GizmoTextCorrectionDistance;
            return new Vector3(0, length, 0).SwizzlesVector(snapSetting.Swizzle);
        }

        /// <summary>
        /// 기즈모: 텍스트 보정 거리 벡터 얻기
        /// </summary>
        public Vector3 GetGizmoTextCorrectionDistance(GridCompatible2 gridCompatible)
        => GetGizmoTextCorrectionDistance(gridCompatible.CurrentSnapSetting);



        [TitleGroup("그리드 기즈모")]
        [EnableIf(nameof(UseGridGizmo))]
        [Indent(1)]
        [LabelText("깊이Z축 크기 보정값")]
        public float CorrectionSizeDepthZLength = 0.001f;

        /// <summary>
        /// 기즈모: 깊이Z축 크기 보정값 벡터 얻기
        /// </summary>
        /// <param name="snapSetting"></param>
        public Vector3 GetCorrectionSizeDepthZLength(GridCompatible2SnapSetting snapSetting, bool half)
        {
            return new Vector3(0, 0, CorrectionSizeDepthZLength).SwizzlesVector(snapSetting.Swizzle) * (half ? 0.5f : 1f);
        }

        /// <summary>
        /// 기즈모: 깊이Z축 크기 보정값 벡터 얻기
        /// </summary>
        public Vector3 GetCorrectionSizeDepthZLength(GridCompatible2 gridCompatible, bool half)
        => GetCorrectionSizeDepthZLength(gridCompatible.CurrentSnapSetting, half);
    }
}