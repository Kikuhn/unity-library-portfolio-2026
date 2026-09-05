using UnityEngine;



namespace Pan.HighDensityElement.Physics2DBridge
{
    /// <summary>
    /// 자동 mirror 대상과 PhysicsCore 고정 스텝 기본값을 저장하는 프로젝트 설정입니다.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Pan/High Density Element/Physics2D Bridge Settings",
        fileName = "Physics2DBridgeSettings")]
    public sealed class Physics2DBridgeSettings : ScriptableObject
    {
        [SerializeField]
        private LayerMask automaticMirrorLayers = ~0;

        [SerializeField, Min(16)]
        private int initialTargetCapacity = 256;

        [SerializeField, Min(0.001f)]
        private float fixedStep = 0.02f;

        [SerializeField, Range(1, 16)]
        private int maxSubsteps = 4;

        [SerializeField]
        private bool enableDevelopmentDebugDraw;

        [SerializeField, Min(0.01f)]
        private float strictCcdMotionRatio = 0.5f;



        /// <summary>
        /// scene scan에서 자동 등록할 static Collider2D layer입니다.
        /// </summary>
        public LayerMask AutomaticMirrorLayers
        {
            get => automaticMirrorLayers;
            set => automaticMirrorLayers = value;
        }



        /// <summary>
        /// registry target dictionary의 초기 용량입니다.
        /// </summary>
        public int InitialTargetCapacity
        {
            get => initialTargetCapacity;
            set => initialTargetCapacity = Mathf.Max(16, value);
        }



        /// <summary>
        /// consumer simulation clock이 사용할 기본 PhysicsCore substep 간격입니다.
        /// </summary>
        public float FixedStep
        {
            get => fixedStep;
            set => fixedStep = Mathf.Max(0.001f, value);
        }



        /// <summary>
        /// 한 렌더 프레임에서 실행할 최대 PhysicsCore substep 수입니다.
        /// </summary>
        public int MaxSubsteps
        {
            get => maxSubsteps;
            set => maxSubsteps = Mathf.Clamp(value, 1, 16);
        }



        /// <summary>
        /// Development Build에서 bridge custom debug geometry 제출을 허용할지 결정합니다.
        /// </summary>
        public bool EnableDevelopmentDebugDraw
        {
            get => enableDevelopmentDebugDraw;
            set => enableDevelopmentDebugDraw = value;
        }



        /// <summary>
        /// 한 고정 스텝의 이동 거리와 회전 끝점 이동량이 형상 최소 크기의 이 비율을 넘으면 엄격 CCD 후보로 분류합니다.
        /// </summary>
        public float StrictCcdMotionRatio
        {
            get => Mathf.Max(0.01f, strictCcdMotionRatio);
            set => strictCcdMotionRatio = Mathf.Max(0.01f, value);
        }



        /// <summary>
        /// 지정한 Unity layer가 자동 mirror 범위에 포함되는지 확인합니다.
        /// </summary>
        public bool IncludesLayer(int layer) =>
            layer >= 0 && layer < 32 && (automaticMirrorLayers.value & (1 << layer)) != 0;
    }
}
