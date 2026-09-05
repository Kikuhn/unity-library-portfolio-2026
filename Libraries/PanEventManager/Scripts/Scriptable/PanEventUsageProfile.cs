using System;



namespace Pan.Event
{
    /// <summary>
    /// PanEvent/PanEventValue 설정 목록에서 타입의 사용 목적을 구분한다.
    /// </summary>
    public enum PanEventUsageProfile
    {
        Runtime,
        ValidationOnly,
        Legacy,
        Experimental,
    }



    /// <summary>
    /// 새로 발견되는 PanEvent/PanEventValue 타입의 기본 사용 프로필을 지정한다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class PanEventUsageProfileAttribute : Attribute
    {
        public PanEventUsageProfileAttribute(PanEventUsageProfile usageProfile)
        {
            UsageProfile = usageProfile;
        }



        public PanEventUsageProfile UsageProfile { get; }
    }
}