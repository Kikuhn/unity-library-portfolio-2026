using System;
using System.Collections.Generic;
using UnityEngine;



namespace Pan.HighDensityElement.Editor
{
    /// <summary>
    /// Debugger 창과 Scene 진단이 공유하는 generation-safe Element 선택입니다.
    /// </summary>
    public static class ElementDebuggerSelection
    {
        private static ElementWorld world;
        private static ElementKey key;



        /// <summary>
        /// 선택이 변경되거나 stale 선택이 해제될 때 발생합니다.
        /// </summary>
        public static event Action Changed;



        public static ElementWorld World => world;
        public static ElementKey Key => key;
        public static bool IsValid => Validate();



        /// <summary>
        /// 현재 world에 살아 있는 정확한 generation의 Element만 선택합니다.
        /// </summary>
        public static bool TrySet(ElementWorld selectedWorld, ElementKey selectedKey)
        {
            if (!TryResolve(selectedWorld, selectedKey, out _, out _))
            {
                Clear();
                return false;
            }

            if (ReferenceEquals(world, selectedWorld) && key == selectedKey) { return true; }

            world = selectedWorld;
            key = selectedKey;
            Changed?.Invoke();
            return true;
        }



        /// <summary>
        /// 현재 선택을 generation까지 재검증하고 handle과 snapshot을 반환합니다.
        /// </summary>
        public static bool TryGet(
            out ElementWorld selectedWorld,
            out ElementHandle handle,
            out ElementSnapshot snapshot)
        {
            selectedWorld = world;
            if (TryResolve(selectedWorld, key, out handle, out snapshot)) { return true; }

            Clear();
            selectedWorld = null;
            handle = default;
            snapshot = default;
            return false;
        }



        /// <summary>
        /// 선택된 world와 key가 아직 유효한지 검증하고 stale 선택을 즉시 해제합니다.
        /// </summary>
        public static bool Validate()
        {
            if (TryResolve(world, key, out _, out _)) { return true; }

            Clear();
            return false;
        }



        /// <summary>
        /// 공유 선택을 해제합니다.
        /// </summary>
        public static void Clear()
        {
            if (world == null && !key.IsValid) { return; }

            world = null;
            key = default;
            Changed?.Invoke();
        }



        private static bool TryResolve(
            ElementWorld selectedWorld,
            ElementKey selectedKey,
            out ElementHandle handle,
            out ElementSnapshot snapshot)
        {
            if (selectedWorld != null && !selectedWorld.IsDisposed && selectedKey.IsValid &&
                selectedKey.WorldId == selectedWorld.WorldId &&
                selectedWorld.TryGetHandle(selectedKey, out handle) &&
                handle.TryGetSnapshot(out snapshot))
            {
                return true;
            }

            handle = default;
            snapshot = default;
            return false;
        }
    }



    /// <summary>
    /// Inspector 탭 확장에 전달되는 generation-safe 선택 문맥입니다.
    /// </summary>
    public readonly struct ElementDebuggerInspectorContext
    {
        public ElementDebuggerInspectorContext(
            ElementWorld world,
            ElementHandle handle,
            ElementSnapshot snapshot,
            IReadOnlyList<ElementFact> recentFacts)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
            if (world.IsDisposed || !handle.IsAlive || handle.Key != snapshot.Key ||
                snapshot.Key.WorldId != world.WorldId)
            {
                throw new ArgumentException(
                    "Inspector context에는 현재 world에 살아 있는 동일 generation의 handle과 snapshot이 필요합니다.",
                    nameof(handle));
            }

            Handle = handle;
            Snapshot = snapshot;
            RecentFacts = recentFacts ?? Array.Empty<ElementFact>();
        }



        public ElementWorld World { get; }
        public ElementHandle Handle { get; }
        public ElementSnapshot Snapshot { get; }
        public IReadOnlyList<ElementFact> RecentFacts { get; }
        public ElementKey Key => Snapshot.Key;
    }



    /// <summary>
    /// 가상화된 Element 행에 선택형 표시 metadata를 요청할 때 전달되는 값 문맥입니다.
    /// </summary>
    public readonly struct ElementDebuggerRowContext
    {
        public ElementDebuggerRowContext(ElementWorld world, ElementSnapshot snapshot)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
            Snapshot = snapshot;
        }



        public ElementWorld World { get; }
        public ElementSnapshot Snapshot { get; }
        public ElementKey Key => Snapshot.Key;
    }



    /// <summary>
    /// Element 행의 선택형 이름, 종류, tooltip을 나타냅니다.
    /// </summary>
    public readonly struct ElementDebuggerRowMetadata
    {
        public ElementDebuggerRowMetadata(
            string displayName,
            string kind = null,
            string tooltip = null,
            Texture icon = null)
        {
            DisplayName = displayName ?? string.Empty;
            Kind = kind ?? string.Empty;
            Tooltip = tooltip ?? string.Empty;
            Icon = icon;
        }



        public string DisplayName { get; }
        public string Kind { get; }
        public string Tooltip { get; }
        public Texture Icon { get; }
    }



    /// <summary>
    /// 프로젝트 계층이 ElementWorld의 실제 관리 주체와 PlayerLoop 경로를 설명할 때 사용하는 값입니다.
    /// </summary>
    public readonly struct ElementDebuggerWorldMetadata
    {
        public ElementDebuggerWorldMetadata(
            string stableOwnerId,
            string displayName,
            UnityEngine.Object ownerObject,
            string runtimeTypeName,
            bool isRunning,
            string updatePath,
            string fixedUpdatePath,
            string presentationPath)
        {
            StableOwnerId = stableOwnerId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            OwnerObject = ownerObject;
            RuntimeTypeName = runtimeTypeName ?? string.Empty;
            IsRunning = isRunning;
            UpdatePath = updatePath ?? string.Empty;
            FixedUpdatePath = fixedUpdatePath ?? string.Empty;
            PresentationPath = presentationPath ?? string.Empty;
        }



        public string StableOwnerId { get; }
        public string DisplayName { get; }
        public UnityEngine.Object OwnerObject { get; }
        public string RuntimeTypeName { get; }
        public bool IsRunning { get; }
        public string UpdatePath { get; }
        public string FixedUpdatePath { get; }
        public string PresentationPath { get; }
    }



    /// <summary>
    /// 코어 패키지에 프로젝트 manager 의존성을 추가하지 않고 World 관리 정보를 제공합니다.
    /// </summary>
    public interface IElementDebuggerWorldMetadataProvider
    {
        int Order { get; }
        bool TryGetMetadata(ElementWorld world, out ElementDebuggerWorldMetadata metadata);
    }



    /// <summary>
    /// World metadata provider를 결정적인 순서로 관리합니다.
    /// </summary>
    public static class ElementDebuggerWorldMetadataProviderRegistry
    {
        private static readonly List<IElementDebuggerWorldMetadataProvider> Providers =
            new List<IElementDebuggerWorldMetadataProvider>(4);



        public static event Action Changed;



        public static void Register(IElementDebuggerWorldMetadataProvider provider)
        {
            if (provider == null) { throw new ArgumentNullException(nameof(provider)); }
            if (Providers.Contains(provider)) { return; }

            Providers.Add(provider);
            Providers.Sort(CompareProviders);
            Changed?.Invoke();
        }

        public static void Unregister(IElementDebuggerWorldMetadataProvider provider)
        {
            if (provider == null || !Providers.Remove(provider)) { return; }
            Changed?.Invoke();
        }

        public static void CopyProviders(List<IElementDebuggerWorldMetadataProvider> destination)
        {
            if (destination == null) { throw new ArgumentNullException(nameof(destination)); }
            destination.Clear();
            destination.AddRange(Providers);
        }



        private static int CompareProviders(
            IElementDebuggerWorldMetadataProvider left,
            IElementDebuggerWorldMetadataProvider right)
        {
            int order = left.Order.CompareTo(right.Order);
            if (order != 0) { return order; }
            return string.Compare(left.GetType().FullName, right.GetType().FullName, StringComparison.Ordinal);
        }
    }



    /// <summary>
    /// core에 선택형 패키지 의존성을 추가하지 않고 visible Element 행 metadata를 제공합니다.
    /// </summary>
    public interface IElementDebuggerRowProvider
    {
        int Order { get; }
        bool TryGetMetadata(
            in ElementDebuggerRowContext context,
            out ElementDebuggerRowMetadata metadata);
    }



    /// <summary>
    /// 선택형 component가 현재 Element에서 가지는 상태입니다.
    /// </summary>
    public enum ElementDebuggerComponentStatus : byte
    {
        Available = 0,
        Disabled = 1,
        Lazy = 2,
        Allocated = 3,
        Conflict = 4,
        Unavailable = 5
    }



    /// <summary>
    /// 선택된 Element의 inline component header를 나타냅니다.
    /// </summary>
    public readonly struct ElementDebuggerComponentDescriptor
    {
        public ElementDebuggerComponentDescriptor(
            string id,
            string displayName,
            ElementDebuggerComponentStatus status = ElementDebuggerComponentStatus.Available,
            string summary = null)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Status = status;
            Summary = summary ?? string.Empty;
        }



        public string Id { get; }
        public string DisplayName { get; }
        public ElementDebuggerComponentStatus Status { get; }
        public string Summary { get; }
    }



    /// <summary>
    /// 선택된 Element의 Inspector component를 inline으로 그리는 선택형 provider입니다.
    /// </summary>
    public interface IElementDebuggerComponentProvider
    {
        int Order { get; }
        bool TryGetDescriptor(
            in ElementDebuggerInspectorContext context,
            out ElementDebuggerComponentDescriptor descriptor);
        void OnInspectorGUI(in ElementDebuggerInspectorContext context);
    }



    /// <summary>
    /// Element 행 provider를 결정적인 순서로 관리합니다.
    /// </summary>
    public static class ElementDebuggerRowProviderRegistry
    {
        private static readonly List<IElementDebuggerRowProvider> Providers =
            new List<IElementDebuggerRowProvider>(4);



        public static event Action Changed;



        public static void Register(IElementDebuggerRowProvider provider)
        {
            if (provider == null) { throw new ArgumentNullException(nameof(provider)); }
            if (Providers.Contains(provider)) { return; }

            Providers.Add(provider);
            Providers.Sort(CompareProviders);
            Changed?.Invoke();
        }

        public static void Unregister(IElementDebuggerRowProvider provider)
        {
            if (provider == null || !Providers.Remove(provider)) { return; }
            Changed?.Invoke();
        }

        public static void CopyProviders(List<IElementDebuggerRowProvider> destination)
        {
            if (destination == null) { throw new ArgumentNullException(nameof(destination)); }
            destination.Clear();
            destination.AddRange(Providers);
        }



        private static int CompareProviders(
            IElementDebuggerRowProvider left,
            IElementDebuggerRowProvider right)
        {
            int order = left.Order.CompareTo(right.Order);
            if (order != 0) { return order; }
            return string.Compare(
                left.GetType().FullName,
                right.GetType().FullName,
                StringComparison.Ordinal);
        }
    }



    /// <summary>
    /// inline component provider를 결정적인 순서로 관리합니다.
    /// </summary>
    public static class ElementDebuggerComponentProviderRegistry
    {
        private static readonly List<IElementDebuggerComponentProvider> Providers =
            new List<IElementDebuggerComponentProvider>(4);



        public static event Action Changed;



        public static void Register(IElementDebuggerComponentProvider provider)
        {
            if (provider == null) { throw new ArgumentNullException(nameof(provider)); }
            if (Providers.Contains(provider)) { return; }

            Providers.Add(provider);
            Providers.Sort(CompareProviders);
            Changed?.Invoke();
        }

        public static void Unregister(IElementDebuggerComponentProvider provider)
        {
            if (provider == null || !Providers.Remove(provider)) { return; }
            Changed?.Invoke();
        }

        public static void CopyProviders(List<IElementDebuggerComponentProvider> destination)
        {
            if (destination == null) { throw new ArgumentNullException(nameof(destination)); }
            destination.Clear();
            destination.AddRange(Providers);
        }



        private static int CompareProviders(
            IElementDebuggerComponentProvider left,
            IElementDebuggerComponentProvider right)
        {
            int order = left.Order.CompareTo(right.Order);
            if (order != 0) { return order; }
            return string.Compare(
                left.GetType().FullName,
                right.GetType().FullName,
                StringComparison.Ordinal);
        }
    }



    /// <summary>
    /// 선택된 Element의 우측 Inspector에 진단 탭을 추가하는 Editor 확장 계약입니다.
    /// </summary>
    public interface IElementDebuggerInspectorExtension
    {
        string DisplayName { get; }
        int Order { get; }
        bool IsVisible(in ElementDebuggerInspectorContext context);
        void OnInspectorGUI(in ElementDebuggerInspectorContext context);
    }



    /// <summary>
    /// Debugger Inspector 탭 확장을 결정적인 순서로 관리하는 Editor registry입니다.
    /// </summary>
    public static class ElementDebuggerInspectorExtensionRegistry
    {
        private static readonly List<IElementDebuggerInspectorExtension> Extensions =
            new List<IElementDebuggerInspectorExtension>(4);



        public static event Action Changed;



        public static void Register(IElementDebuggerInspectorExtension extension)
        {
            if (extension == null) { throw new ArgumentNullException(nameof(extension)); }
            if (Extensions.Contains(extension)) { return; }

            Extensions.Add(extension);
            Extensions.Sort(CompareExtensions);
            Changed?.Invoke();
        }



        public static void Unregister(IElementDebuggerInspectorExtension extension)
        {
            if (extension == null || !Extensions.Remove(extension)) { return; }

            Changed?.Invoke();
        }



        public static void CopyExtensions(List<IElementDebuggerInspectorExtension> destination)
        {
            if (destination == null) { throw new ArgumentNullException(nameof(destination)); }

            destination.Clear();
            destination.AddRange(Extensions);
        }



        private static int CompareExtensions(
            IElementDebuggerInspectorExtension left,
            IElementDebuggerInspectorExtension right)
        {
            int orderComparison = left.Order.CompareTo(right.Order);
            if (orderComparison != 0) { return orderComparison; }

            return string.Compare(left.DisplayName, right.DisplayName, StringComparison.Ordinal);
        }
    }
}
