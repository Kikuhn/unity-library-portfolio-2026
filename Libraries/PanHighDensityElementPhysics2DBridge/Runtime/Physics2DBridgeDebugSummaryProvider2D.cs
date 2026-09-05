using System.Collections.Generic;
using Pan.HighDensityElement;



namespace Pan.HighDensityElement.Physics2DBridge
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>
    /// 범용 Element 디버거에 현재 World의 Projection 요약을 제공하는 진단 전용 연결부입니다.
    /// </summary>
    internal sealed class Physics2DBridgeDebugSummaryProvider2D : IElementWorldDebugSummaryProvider
    {
        private static readonly Physics2DBridgeDebugSummaryProvider2D Instance =
            new Physics2DBridgeDebugSummaryProvider2D();
        private static int registryCount;
        private readonly List<Physics2DBridgeRegistry> registries = new List<Physics2DBridgeRegistry>(2);



        internal static void RegisterRegistry()
        {
            registryCount++;
            if (registryCount != 1) { return; }

            ElementWorldDebugRegistry.RegisterSummaryProvider(Instance);
        }



        internal static void UnregisterRegistry()
        {
            if (registryCount <= 0) { return; }

            registryCount--;
            if (registryCount == 0)
            {
                ElementWorldDebugRegistry.UnregisterSummaryProvider(Instance);
            }
        }



        public bool TryGetSummary(ElementWorld world, out string summary)
        {
            int registryCount = Physics2DBridgeDebugRegistry.CopyRegistriesForWorld(world, registries);
            if (registryCount == 0)
            {
                summary = null;
                return false;
            }

            int targetCount = 0;
            int bodyCount = 0;
            int shapeCount = 0;
            int strictMotionCount = 0;
            long synchronizationSequence = 0L;

            for (int i = 0; i < registryCount; i++)
            {
                Physics2DBridgeRegistryDiagnostics diagnostics = registries[i].GetDiagnostics();
                targetCount += diagnostics.TargetCount;
                bodyCount += diagnostics.BodyCount;
                shapeCount += diagnostics.ShapeCount;
                strictMotionCount += diagnostics.StrictMotionCount;
                if (diagnostics.SynchronizationSequence > synchronizationSequence)
                {
                    synchronizationSequence = diagnostics.SynchronizationSequence;
                }
            }

            summary =
                $"Projection  Target {targetCount:N0} / Body {bodyCount:N0} / Shape {shapeCount:N0} / " +
                $"Strict {strictMotionCount:N0} / Sync {synchronizationSequence:N0}";
            return true;
        }
    }
#endif
}
