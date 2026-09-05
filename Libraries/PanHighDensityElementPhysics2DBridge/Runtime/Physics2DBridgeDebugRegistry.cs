using System;
using System.Collections.Generic;



namespace Pan.HighDensityElement.Physics2DBridge
{
    /// <summary>
    /// Editor와 Development 진단 도구가 같은 ElementWorld의 bridge registry를 읽기 전용으로 찾는 진입점입니다.
    /// </summary>
    public static class Physics2DBridgeDebugRegistry
    {
        /// <summary>
        /// 지정한 World의 PhysicsCore lane을 공유하는 살아 있는 registry를 호출자 재사용 목록에 복사합니다.
        /// </summary>
        public static int CopyRegistriesForWorld(
            ElementWorld world,
            List<Physics2DBridgeRegistry> destination)
        {
            if (world == null) { throw new ArgumentNullException(nameof(world)); }
            if (destination == null) { throw new ArgumentNullException(nameof(destination)); }
            destination.Clear();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return Physics2DBridgeRegistry.CopyDebugRegistries(world.PhysicsCoreLane, destination);
#else
            return 0;
#endif
        }
    }
}
