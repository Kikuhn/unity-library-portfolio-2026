using Pan.HighDensityElement;
using Pan.HighDensityElement.Editor;
using Pan.Tan.Element;
using UnityEditor;
using UnityEngine;



namespace Pan.Tan.Editor
{
    /// <summary>
    /// Element debugger 행에 실제 ElementTan 매핑의 이름, 종류, 타입 아이콘을 제공합니다.
    /// </summary>
    [InitializeOnLoad]
    internal sealed class ElementTanDebuggerRowProvider : IElementDebuggerRowProvider
    {
        internal static readonly ElementTanDebuggerRowProvider Instance = new ElementTanDebuggerRowProvider();



        private static readonly Texture QueryCircleIcon =
            EditorGUIUtility.ObjectContent(null, typeof(CircleCollider2D))?.image;
        static ElementTanDebuggerRowProvider()
        {
            ElementDebuggerRowProviderRegistry.Register(Instance);
            AssemblyReloadEvents.beforeAssemblyReload += Cleanup;
        }

        private ElementTanDebuggerRowProvider()
        {
        }



        public int Order => 100;



        public bool TryGetMetadata(
            in ElementDebuggerRowContext context,
            out ElementDebuggerRowMetadata metadata)
        {
            if (ElementTanDebugRegistry.TryResolve(context.World, context.Key, out ElementTanDebugMapping mapping))
            {
                metadata = new ElementDebuggerRowMetadata(
                    mapping.DisplayName,
                    mapping.Kind,
                    mapping.Tooltip,
                    ResolveElementTanIcon(in context));
                return true;
            }

            metadata = default;
            return false;
        }



        private static void Cleanup()
        {
            ElementDebuggerRowProviderRegistry.Unregister(Instance);
            AssemblyReloadEvents.beforeAssemblyReload -= Cleanup;
        }

        private static Texture ResolveElementTanIcon(in ElementDebuggerRowContext context)
        {
            return QueryCircleIcon;
        }
    }
}
