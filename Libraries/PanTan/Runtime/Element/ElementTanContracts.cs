using System;
using Pan.HighDensityElement;



namespace Pan.Tan.Element
{
    /// <summary>
    /// Query/Kinematic/Circle ElementTan runtime에 고정되는 PhysicsCore2D 설정입니다.
    /// </summary>
    [Serializable]
    public readonly struct ElementTanBackendOptions
    {
        public ElementTanBackendOptions(
            StrictCcdOverride2D strictCcdOverride = StrictCcdOverride2D.Auto,
            float strictCcdThresholdRatio = 0.5f)
        {
            StrictCcdOverride = strictCcdOverride;
            StrictCcdThresholdRatio = Math.Max(0.01f, strictCcdThresholdRatio);
        }



        public StrictCcdOverride2D StrictCcdOverride { get; }
        public float StrictCcdThresholdRatio { get; }
        public static ElementTanBackendOptions Query =>
            new ElementTanBackendOptions(StrictCcdOverride2D.Auto, 0.5f);
    }



    /// <summary>
    /// PhysicsCore contact fact를 generation-safe gameplay target으로 복원합니다.
    /// </summary>
    public interface IElementTanTargetResolver
    {
        bool TryResolveTarget(in ElementFact fact, out TanTargetHandle target);
    }
}
