using System;
using System.Collections.Generic;



namespace Pan.HighDensityElement
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>
    /// 활성 ElementWorld를 에디터와 Development 진단 도구에 노출하는 읽기 중심 registry입니다.
    /// </summary>
    public static class ElementWorldDebugRegistry
    {
        private static readonly List<ElementWorld> Worlds = new List<ElementWorld>(4);
        private static readonly List<IElementWorldDebugSummaryProvider> SummaryProviders =
            new List<IElementWorldDebugSummaryProvider>(4);
        private static readonly List<IElementWorldDebugDetailProvider> DetailProviders =
            new List<IElementWorldDebugDetailProvider>(4);



        public static int Count => Worlds.Count;



        /// <summary>
        /// 여러 진단 창이 같은 World를 열어도 마지막 lease가 끝날 때까지 capture를 유지합니다.
        /// </summary>
        public static ElementWorldDebugCaptureLease AcquireCapture(
            ElementWorld world,
            bool detailedDiagnostics = true,
            bool factCapture = true)
        {
            if (world == null) { throw new ArgumentNullException(nameof(world)); }
            if (world.IsDisposed) { throw new ObjectDisposedException(nameof(ElementWorld)); }
            return new ElementWorldDebugCaptureLease(world, detailedDiagnostics, factCapture);
        }



        public static void CopyWorlds(List<ElementWorld> destination)
        {
            if (destination == null) { throw new ArgumentNullException(nameof(destination)); }

            destination.Clear();
            for (int i = Worlds.Count - 1; i >= 0; i--)
            {
                ElementWorld world = Worlds[i];
                if (world == null || world.IsDisposed)
                {
                    Worlds.RemoveAt(i);
                    continue;
                }

                destination.Add(world);
            }
        }



        public static void CopyExtensionSummaries(ElementWorld world, List<string> destination)
        {
            if (destination == null) { throw new ArgumentNullException(nameof(destination)); }

            destination.Clear();
            for (int i = 0; i < SummaryProviders.Count; i++)
            {
                IElementWorldDebugSummaryProvider provider = SummaryProviders[i];
                if (provider != null && provider.TryGetSummary(world, out string summary) &&
                    !string.IsNullOrWhiteSpace(summary))
                {
                    destination.Add(summary);
                }
            }
        }



        public static void RegisterSummaryProvider(IElementWorldDebugSummaryProvider provider)
        {
            if (provider != null && !SummaryProviders.Contains(provider)) { SummaryProviders.Add(provider); }
        }



        public static void CopyExtensionDetails(
            ElementWorld world,
            ElementKey key,
            List<string> destination)
        {
            if (destination == null) { throw new ArgumentNullException(nameof(destination)); }

            destination.Clear();
            for (int i = 0; i < DetailProviders.Count; i++)
            {
                IElementWorldDebugDetailProvider provider = DetailProviders[i];
                provider?.AppendDetails(world, key, destination);
            }
        }



        public static void RegisterDetailProvider(IElementWorldDebugDetailProvider provider)
        {
            if (provider != null && !DetailProviders.Contains(provider)) { DetailProviders.Add(provider); }
        }



        public static void UnregisterDetailProvider(IElementWorldDebugDetailProvider provider)
        {
            if (provider != null) { DetailProviders.Remove(provider); }
        }



        public static void UnregisterSummaryProvider(IElementWorldDebugSummaryProvider provider)
        {
            if (provider != null) { SummaryProviders.Remove(provider); }
        }



        internal static void Register(ElementWorld world)
        {
            if (world != null && !Worlds.Contains(world)) { Worlds.Add(world); }
        }



        internal static void Unregister(ElementWorld world)
        {
            if (world != null) { Worlds.Remove(world); }
        }
    }



    /// <summary>
    /// ElementWorld 진단 capture를 reference-counted 방식으로 유지하는 수명 token입니다.
    /// </summary>
    public sealed class ElementWorldDebugCaptureLease : IDisposable
    {
        private ElementWorld world;
        private readonly bool detailedDiagnostics;
        private readonly bool factCapture;



        internal ElementWorldDebugCaptureLease(
            ElementWorld world,
            bool detailedDiagnostics,
            bool factCapture)
        {
            this.world = world;
            this.detailedDiagnostics = detailedDiagnostics;
            this.factCapture = factCapture;
            world.AcquireDebugCapture(detailedDiagnostics, factCapture);
        }



        public bool IsValid => world != null && !world.IsDisposed;



        public void Dispose()
        {
            ElementWorld target = world;
            if (target == null) { return; }

            world = null;
            target.ReleaseDebugCapture(detailedDiagnostics, factCapture);
        }
    }



    /// <summary>
    /// 선택형 패키지가 core Editor 창에 순환 참조 없이 한 줄 진단을 추가하는 계약입니다.
    /// </summary>
    public interface IElementWorldDebugSummaryProvider
    {
        bool TryGetSummary(ElementWorld world, out string summary);
    }



    public interface IElementWorldDebugDetailProvider
    {
        void AppendDetails(ElementWorld world, ElementKey key, List<string> destination);
    }
#endif
}
