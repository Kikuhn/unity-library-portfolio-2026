#if UNITY_EDITOR || DEVELOPMENT_BUILD

using System;
using System.Collections.Generic;
using Pan.HighDensityElement;



namespace Pan.Tan.Element
{
    /// <summary>
    /// Element debugger가 선택된 Element를 실제 ElementTan runtime과 연결한 결과입니다.
    /// </summary>
    public readonly struct ElementTanDebugMapping
    {
        internal ElementTanDebugMapping(
            ElementTanRuntime runtime,
            TanHandle handle)
        {
            Runtime = runtime;
            Handle = handle;
        }



        /// <summary>
        /// 이 ElementTan을 소유하는 실제 런타임입니다.
        /// </summary>
        public ElementTanRuntime Runtime { get; }

        /// <summary>
        /// 선택된 generation을 검증하는 gameplay handle입니다.
        /// </summary>
        public TanHandle Handle { get; }

        /// <summary>
        /// 선택된 Tan의 generation-safe 키입니다.
        /// </summary>
        public TanKey Key => Handle.Key;

        /// <summary>
        /// debugger 행에 표시할 generation 포함 이름입니다.
        /// </summary>
        public string DisplayName => $"ElementTan {Key.Slot}:{Key.Generation}";

        /// <summary>
        /// debugger가 분류에 사용하는 종류 이름입니다.
        /// </summary>
        public string Kind => "ElementTan";

        /// <summary>
        /// 실제 실행 모델을 설명하는 도움말입니다.
        /// </summary>
        public string Tooltip => "PhysicsCore2D bodyless Query/Kinematic ElementTan";
    }



    /// <summary>
    /// 활성 ElementTan runtime의 generation-safe ElementKey 매핑을 진단 도구에 제공합니다.
    /// </summary>
    public static class ElementTanDebugRegistry
    {
        private static readonly List<ElementTanRuntime> Runtimes = new List<ElementTanRuntime>(4);



        /// <summary>
        /// 정확한 world와 generation의 ElementKey를 살아 있는 ElementTan에만 매핑합니다.
        /// </summary>
        public static bool TryResolve(
            ElementWorld world,
            ElementKey key,
            out ElementTanDebugMapping mapping)
        {
            if (world == null || world.IsDisposed || !key.IsValid || key.WorldId != world.WorldId)
            {
                mapping = default;
                return false;
            }

            for (int i = Runtimes.Count - 1; i >= 0; i--)
            {
                ElementTanRuntime runtime = Runtimes[i];
                if (runtime == null || !runtime.IsAvailable)
                {
                    Runtimes.RemoveAt(i);
                    continue;
                }
                if (!ReferenceEquals(runtime.ElementWorld, world) ||
                    !runtime.TryGetDebugHandle(key, out TanHandle handle))
                {
                    continue;
                }

                mapping = new ElementTanDebugMapping(runtime, handle);
                return true;
            }

            mapping = default;
            return false;
        }



        internal static void Register(ElementTanRuntime runtime)
        {
            if (runtime != null && !Runtimes.Contains(runtime)) { Runtimes.Add(runtime); }
        }

        internal static void Unregister(ElementTanRuntime runtime)
        {
            if (runtime != null) { Runtimes.Remove(runtime); }
        }
    }



    public sealed partial class ElementTanRuntime
    {
        internal bool TryGetDebugHandle(ElementKey elementKey, out TanHandle handle)
        {
            TanKey key = new TanKey(elementKey.WorldId, elementKey.Slot, elementKey.Generation);
            if (IsAlive(in key))
            {
                handle = new TanHandle(this, in key);
                return true;
            }

            handle = default;
            return false;
        }
    }
}

#endif
