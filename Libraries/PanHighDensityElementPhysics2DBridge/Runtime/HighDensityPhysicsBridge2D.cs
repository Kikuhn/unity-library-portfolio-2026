using System;
using System.Collections.Generic;
using UnityEngine;



namespace Pan.HighDensityElement.Physics2DBridge
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Pan/High Density Element/Physics 2D Bridge")]
    public sealed class HighDensityPhysicsBridge2D : MonoBehaviour
    {
        private sealed class ManualShapeProvider : IPhysics2DBridgeShapeProvider
        {
            private readonly HighDensityPhysicsBridge2D owner;



            public ManualShapeProvider(HighDensityPhysicsBridge2D owner)
            {
                this.owner = owner;
            }



            public Component TargetComponent => owner.ResolveManualTarget();
            public int GeometryRevision => owner.manualGeometryRevision;



            public bool TryGetBodyState(out Physics2DBridgeBodyState state) =>
                owner.TryGetManualBodyState(out state);



            public int CopyShapes(
                List<Physics2DBridgeShapeDescriptor> shapes,
                List<Vector2> vertices) =>
                owner.CopyManualShapes(shapes, vertices);
        }



        [SerializeField]
        private bool includeChildColliders = true;

        [SerializeField]
        private bool includeInactiveChildColliders;

        [SerializeField]
        private Physics2DBridgeShapeSourceMode shapeSourceMode =
            Physics2DBridgeShapeSourceMode.CollidersOnly;

        [SerializeField]
        private Physics2DBridgeDebugDrawMode debugDrawMode =
            Physics2DBridgeDebugDrawMode.Presentation;

        [SerializeField]
        private List<Collider2D> selectedColliders = new List<Collider2D>();

        [SerializeField]
        private List<Physics2DBridgeManualShape2D> manualShapes =
            new List<Physics2DBridgeManualShape2D>();

        [SerializeField]
        private Rigidbody2D manualPoseRigidbody;

        [SerializeField]
        private Transform manualPoseTransform;

        [SerializeField]
        private Component manualTargetComponent;

        [SerializeField]
        private Physics2DBridgeManualBodyMode manualBodyMode =
            Physics2DBridgeManualBodyMode.Auto;

        private Physics2DBridgeRegistry boundRegistry;
        private Physics2DBridgeRegistry registeredRegistry;
        private IPhysics2DBridgeShapeProvider shapeProvider;
        private Physics2DBridgeRegistrationHandle registrationHandle;
        private Collider2D[] runtimeSelectedColliders = Array.Empty<Collider2D>();
        private Physics2DBridgeManualShape2D[] runtimeManualShapes =
            Array.Empty<Physics2DBridgeManualShape2D>();
        private Rigidbody2D runtimePoseRigidbody;
        private Transform runtimePoseTransform;
        private Component runtimeTargetComponent;
        private Physics2DBridgeManualBodyMode runtimeManualBodyMode;
        private IPhysics2DBridgeShapeProvider runtimeShapeProvider;
        private Physics2DBridgeShapeSourceMode runtimeShapeSourceMode;
        private ManualShapeProvider manualShapeProvider;
        private int manualGeometryRevision = 1;
        private int pendingStrictCcdSteps;
        private bool pendingTeleport;
        private bool hasRuntimeConfiguration;



        /// <summary>
        /// 이 GameObject 아래의 Collider2D를 함께 수집할지 결정합니다.
        /// 하위에 별도 활성 bridge가 있으면 그 subtree는 해당 bridge가 소유합니다.
        /// </summary>
        public bool IncludeChildColliders
        {
            get => includeChildColliders;
            set
            {
                if (includeChildColliders == value) { return; }
                includeChildColliders = value;
                NotifyGeometryConfigurationChanged();
            }
        }



        /// <summary>
        /// 비활성 child까지 검색 범위에 넣을지 결정합니다.
        /// 활성 상태나 형상이 바뀐 뒤에는 <see cref="MarkGeometryDirty"/>를 호출해야 합니다.
        /// </summary>
        public bool IncludeInactiveChildColliders
        {
            get => includeInactiveChildColliders;
            set
            {
                if (includeInactiveChildColliders == value) { return; }
                includeInactiveChildColliders = value;
                NotifyGeometryConfigurationChanged();
            }
        }



        /// <summary>
        /// 현재 registry 등록을 식별하는 generation-safe handle입니다.
        /// </summary>
        public Physics2DBridgeRegistrationHandle RegistrationHandle => registrationHandle;



        /// <summary>
        /// 선택된 bridge의 Scene View 진단 형상을 어떤 pose로 표시할지 결정합니다.
        /// </summary>
        public Physics2DBridgeDebugDrawMode DebugDrawMode
        {
            get => debugDrawMode;
            set => debugDrawMode = value;
        }



        /// <summary>
        /// Collider 대신 형상을 제공하는 선택적 provider입니다.
        /// 실제 선택 순서는 <see cref="ShapeSourceMode"/>에 따르며, 기존 2인자 Configure에서는 provider가 우선합니다.
        /// </summary>
        public IPhysics2DBridgeShapeProvider ShapeProvider
        {
            get
            {
                if (ShapeSourceMode == Physics2DBridgeShapeSourceMode.ManualShapes)
                {
                    return manualShapeProvider ??= new ManualShapeProvider(this);
                }
                return hasRuntimeConfiguration ? runtimeShapeProvider : shapeProvider;
            }
        }



        /// <summary>
        /// PhysicsCore2D proxy 형상을 Collider와 provider 중 어디에서 구성하는지 나타냅니다.
        /// </summary>
        public Physics2DBridgeShapeSourceMode ShapeSourceMode =>
            hasRuntimeConfiguration ? runtimeShapeSourceMode : shapeSourceMode;



        /// <summary>
        /// factory가 적용한 런타임 설정이 직렬화 기본값을 덮어쓰고 있는지 나타냅니다.
        /// </summary>
        public bool HasRuntimeConfiguration => hasRuntimeConfiguration;



        /// <summary>
        /// 명시적 registry를 사용하거나, <paramref name="registry"/>가 null이면 현재 Default registry를 사용합니다.
        /// 활성 객체를 다시 구성해도 이전 registry 등록은 먼저 안전하게 해제됩니다.
        /// </summary>
        /// <param name="registry">명시적으로 연결할 registry입니다. null이면 자동 Default 모드입니다.</param>
        public void Bind(Physics2DBridgeRegistry registry)
        {
            TryBind(registry, out _);
        }



        /// <summary>
        /// registry binding을 변경하고 현재 활성 상태라면 즉시 등록을 시도합니다.
        /// </summary>
        /// <param name="registry">명시 registry이며 null이면 자동 Default 모드입니다.</param>
        /// <param name="handle">즉시 등록된 경우의 handle입니다.</param>
        /// <returns>binding이 적용되고, 등록 대상이 있으면 등록 또는 pending 처리가 가능했을 때 true입니다.</returns>
        public bool TryBind(
            Physics2DBridgeRegistry registry,
            out Physics2DBridgeRegistrationHandle handle)
        {
            if (ReferenceEquals(boundRegistry, registry))
            {
                handle = registrationHandle;
                if (!isActiveAndEnabled) { return true; }
                if (registeredRegistry != null && registeredRegistry.IsRegistered(handle)) { return true; }

                if (boundRegistry == null)
                {
                    Physics2DBridgeRegistry.NotifyEnabled(this);
                    handle = registrationHandle;
                    return true;
                }

                bool registeredCurrent = boundRegistry.TryRegisterOrQueue(this, out handle);
                registrationHandle = handle;
                return registeredCurrent;
            }

            if (boundRegistry == null && isActiveAndEnabled)
            {
                Physics2DBridgeRegistry.NotifyDisabled(this);
            }
            else if (registeredRegistry != null && registrationHandle.IsValid)
            {
                registeredRegistry.Unregister(registrationHandle);
            }

            boundRegistry = registry;
            registeredRegistry = null;
            registrationHandle = default;
            if (!isActiveAndEnabled)
            {
                handle = default;
                return true;
            }

            if (boundRegistry == null)
            {
                Physics2DBridgeRegistry.NotifyEnabled(this);
                handle = registrationHandle;
                return true;
            }

            bool registered = boundRegistry.TryRegisterOrQueue(this, out handle);
            registrationHandle = handle;
            return registered;
        }



        /// <summary>
        /// provider와 registry를 한 번에 설정하는 간단한 조립 진입점입니다.
        /// </summary>
        /// <param name="registry">명시 registry이며 null이면 자동 Default 모드입니다.</param>
        /// <param name="provider">Collider가 없는 객체의 형상 provider입니다. null이면 Collider를 수집합니다.</param>
        /// <returns>현재 활성 상태의 등록 또는 pending 처리가 가능했을 때 true입니다.</returns>
        public bool Configure(
            Physics2DBridgeRegistry registry,
            IPhysics2DBridgeShapeProvider provider = null)
        {
            Physics2DBridgeShapeSourceMode sourceMode = provider == null
                ? Physics2DBridgeShapeSourceMode.CollidersOnly
                : Physics2DBridgeShapeSourceMode.ProviderOnly;
            return Configure(registry, provider, sourceMode);
        }



        /// <summary>
        /// 자신과 자식의 유효 Collider를 자동으로 투영하는 가장 단순한 설정입니다.
        /// </summary>
        public bool ConfigureAutomatic(Physics2DBridgeRegistry registry = null)
        {
            return Configure(registry, null, Physics2DBridgeShapeSourceMode.CollidersOnly);
        }



        /// <summary>
        /// 지정한 Collider만 투영하도록 런타임 설정을 원자적으로 교체합니다.
        /// </summary>
        public bool ConfigureSelected(
            IReadOnlyList<Collider2D> colliders,
            Physics2DBridgeRegistry registry = null,
            Rigidbody2D poseRigidbody = null,
            Transform poseTransform = null,
            Component targetComponent = null)
        {
            var configuration = new Physics2DBridgeRuntimeConfiguration(
                Physics2DBridgeShapeSourceMode.SelectedColliders,
                selectedColliders: colliders,
                poseRigidbody: poseRigidbody,
                poseTransform: poseTransform,
                targetComponent: targetComponent);
            return Configure(registry, in configuration);
        }



        /// <summary>
        /// Collider와 독립된 게임플레이 전용 수동 형상을 투영하도록 런타임 설정을 원자적으로 교체합니다.
        /// </summary>
        public bool ConfigureManual(
            IReadOnlyList<Physics2DBridgeManualShape2D> shapes,
            Physics2DBridgeRegistry registry = null,
            Rigidbody2D poseRigidbody = null,
            Transform poseTransform = null,
            Component targetComponent = null,
            Physics2DBridgeManualBodyMode bodyMode = Physics2DBridgeManualBodyMode.Auto)
        {
            var configuration = new Physics2DBridgeRuntimeConfiguration(
                Physics2DBridgeShapeSourceMode.ManualShapes,
                manualShapes: shapes,
                poseRigidbody: poseRigidbody,
                poseTransform: poseTransform,
                targetComponent: targetComponent,
                manualBodyMode: bodyMode);
            return Configure(registry, in configuration);
        }



        /// <summary>
        /// registry, provider와 형상 원본 선택 정책을 한 번에 설정하는 범용 조립 진입점입니다.
        /// 실제 Collider를 우선하고 Collider가 없을 때만 provider를 사용하려면
        /// <see cref="Physics2DBridgeShapeSourceMode.CollidersThenProvider"/>를 사용합니다.
        /// </summary>
        /// <param name="registry">명시 registry이며 null이면 자동 Default 모드입니다.</param>
        /// <param name="provider">Collider가 없거나 선택 정책이 provider를 포함할 때 사용할 형상 provider입니다.</param>
        /// <param name="sourceMode">Collider와 provider를 조합하는 정책입니다.</param>
        /// <returns>현재 활성 상태에서 등록 또는 pending 처리가 가능하면 true입니다.</returns>
        public bool Configure(
            Physics2DBridgeRegistry registry,
            IPhysics2DBridgeShapeProvider provider,
            Physics2DBridgeShapeSourceMode sourceMode)
        {
            if (ApplyShapeSources(provider, sourceMode) &&
                registeredRegistry != null && registrationHandle.IsValid)
            {
                registeredRegistry.MarkGeometryDirty(this);
            }
            return TryBind(registry, out _);
        }



        /// <summary>
        /// factory가 선택 Collider, 수동 형상과 pose source를 한 번에 덮어쓰는 범용 조립 진입점입니다.
        /// 전달된 목록은 내부 복사되며 실제 Core 구조 변경은 다음 bridge 동기화 경계에서 수행됩니다.
        /// </summary>
        public bool Configure(
            Physics2DBridgeRegistry registry,
            in Physics2DBridgeRuntimeConfiguration configuration)
        {
            if (!CanApplyRuntimeConfiguration(in configuration)) { return false; }

            runtimeShapeSourceMode = configuration.ShapeSourceMode;
            runtimeSelectedColliders = CopyColliders(configuration.SelectedColliders);
            runtimeManualShapes = CopyManualShapes(configuration.ManualShapes);
            runtimePoseRigidbody = configuration.PoseRigidbody;
            runtimePoseTransform = configuration.PoseTransform;
            runtimeTargetComponent = configuration.TargetComponent;
            runtimeManualBodyMode = configuration.ManualBodyMode;
            runtimeShapeProvider = configuration.Provider;
            hasRuntimeConfiguration = true;
            IncrementManualGeometryRevision();
            if (registeredRegistry != null && registrationHandle.IsValid)
            {
                registeredRegistry.MarkGeometryDirty(this);
            }
            return TryBind(registry, out _);
        }



        /// <summary>
        /// factory의 런타임 덮어쓰기를 제거하고 Inspector에 저장된 기본 형상 설정으로 돌아갑니다.
        /// </summary>
        public void ClearRuntimeConfiguration()
        {
            if (!hasRuntimeConfiguration) { return; }

            hasRuntimeConfiguration = false;
            runtimeSelectedColliders = Array.Empty<Collider2D>();
            runtimeManualShapes = Array.Empty<Physics2DBridgeManualShape2D>();
            runtimePoseRigidbody = null;
            runtimePoseTransform = null;
            runtimeTargetComponent = null;
            runtimeShapeProvider = null;
            runtimeManualBodyMode = Physics2DBridgeManualBodyMode.Auto;
            IncrementManualGeometryRevision();
            NotifyGeometryConfigurationChanged();
        }



        /// <summary>
        /// Collider 수집을 대체할 커스텀 shape provider를 설정합니다.
        /// 등록 중이라면 같은 handle을 유지한 채 다음 동기화 경계에서 geometry를 재구성합니다.
        /// </summary>
        /// <param name="provider">새 provider이며 null이면 Collider 기반으로 돌아갑니다.</param>
        public void SetShapeProvider(IPhysics2DBridgeShapeProvider provider)
        {
            SetShapeSources(
                provider,
                provider == null
                    ? Physics2DBridgeShapeSourceMode.CollidersOnly
                    : Physics2DBridgeShapeSourceMode.ProviderOnly);
        }



        /// <summary>
        /// Collider와 provider의 선택 정책을 변경하고 등록된 proxy 형상을 다음 동기화에서 재구성합니다.
        /// </summary>
        /// <param name="provider">선택 정책이 provider를 사용할 때 호출할 형상 공급자입니다.</param>
        /// <param name="sourceMode">Collider와 provider를 조합하는 정책입니다.</param>
        public void SetShapeSources(
            IPhysics2DBridgeShapeProvider provider,
            Physics2DBridgeShapeSourceMode sourceMode)
        {
            if (!ApplyShapeSources(provider, sourceMode)) { return; }
            NotifyGeometryConfigurationChanged();
        }



        /// <summary>
        /// 다음 bridge 동기화 경계에서 Collider/provider 형상을 다시 추출하도록 표시합니다.
        /// </summary>
        public void MarkGeometryDirty()
        {
            registeredRegistry?.MarkGeometryDirty(this);
        }



        /// <summary>
        /// 다음 bridge 동기화 경계에서 활성 상태와 pose를 다시 반영하도록 표시합니다.
        /// 움직이는 Rigidbody2D와 provider는 매 substep pose가 동기화되므로 수동 호출이 필요하지 않습니다.
        /// </summary>
        public void MarkStateDirty()
        {
            registeredRegistry?.MarkStateDirty(this);
        }



        /// <summary>
        /// 빠른 이동이 예상되는 동안 상대 운동 CCD 후보로 유지하도록 다음 고정 스텝 수를 예약합니다.
        /// </summary>
        public void RequestStrictCcd(int fixedStepCount = 1)
        {
            if (fixedStepCount <= 0) { return; }
            if (registeredRegistry != null && registrationHandle.IsValid)
            {
                registeredRegistry.RequestStrictCcd(this, fixedStepCount);
                return;
            }
            pendingStrictCcdSteps = Mathf.Max(pendingStrictCcdSteps, fixedStepCount);
        }



        /// <summary>
        /// 다음 동기화에서 이전 pose와 현재 pose 사이를 이동 경로로 취급하지 않도록 순간이동을 표시합니다.
        /// </summary>
        public void MarkTeleported()
        {
            if (registeredRegistry != null && registrationHandle.IsValid)
            {
                registeredRegistry.MarkTeleported(this);
                return;
            }
            pendingTeleport = true;
        }



        /// <summary>
        /// 런타임에 접촉 receiver 구성이 바뀐 뒤 다음 고정 경계에서 캐시를 다시 만들도록 요청합니다.
        /// </summary>
        public void RefreshFactReceivers()
        {
            registeredRegistry?.MarkFactReceiversDirty(this);
        }



        internal int CollectColliders(System.Collections.Generic.List<Collider2D> destination)
        {
            destination.Clear();
            if (ShapeSourceMode == Physics2DBridgeShapeSourceMode.SelectedColliders)
            {
                IReadOnlyList<Collider2D> sources = hasRuntimeConfiguration
                    ? runtimeSelectedColliders
                    : selectedColliders;
                for (int i = 0; i < sources.Count; i++)
                {
                    Collider2D collider = sources[i];
                    if (collider != null && !destination.Contains(collider)) { destination.Add(collider); }
                }
            }
            else if (includeChildColliders)
            {
                GetComponentsInChildren(includeInactiveChildColliders, destination);
            }
            else
            {
                GetComponents(destination);
            }

            for (int i = destination.Count - 1; i >= 0; i--)
            {
                Collider2D collider = destination[i];
                if (collider == null ||
                    (!includeInactiveChildColliders && !collider.isActiveAndEnabled) ||
                    !OwnsCollider(collider.transform))
                {
                    destination.RemoveAt(i);
                }
            }
            return destination.Count;
        }



        private bool OwnsCollider(Transform target)
        {
            if (target == null || (target != transform && !target.IsChildOf(transform))) { return false; }
            while (target != null)
            {
                HighDensityPhysicsBridge2D candidate = target.GetComponent<HighDensityPhysicsBridge2D>();
                if (candidate != null && candidate.isActiveAndEnabled)
                {
                    return ReferenceEquals(candidate, this);
                }
                target = target.parent;
            }
            return true;
        }



        private Component ResolveManualTarget()
        {
            Component configured = hasRuntimeConfiguration ? runtimeTargetComponent : manualTargetComponent;
            return configured != null ? configured : this;
        }



        private bool TryGetManualBodyState(out Physics2DBridgeBodyState state)
        {
            Rigidbody2D rigidbody = hasRuntimeConfiguration ? runtimePoseRigidbody : manualPoseRigidbody;
            Transform poseTransform = hasRuntimeConfiguration ? runtimePoseTransform : manualPoseTransform;
            Physics2DBridgeManualBodyMode bodyMode = hasRuntimeConfiguration
                ? runtimeManualBodyMode
                : manualBodyMode;
            IPhysics2DBridgeShapeProvider externalPoseProvider = hasRuntimeConfiguration
                ? runtimeShapeProvider
                : shapeProvider;

            //? ManualShapes는 형상만 수동으로 고정하고, CharacterController 같은 커스텀 물리 pose는 기존 provider에서 받을 수 있습니다.
            if (externalPoseProvider != null && externalPoseProvider.TryGetBodyState(out Physics2DBridgeBodyState providerState))
            {
                bool providerIsStatic = bodyMode == Physics2DBridgeManualBodyMode.Static ||
                    (bodyMode == Physics2DBridgeManualBodyMode.Auto && providerState.IsStatic);
                state = new Physics2DBridgeBodyState(
                    providerState.Position,
                    providerState.RotationDegrees,
                    providerState.LinearVelocity,
                    providerState.AngularVelocity,
                    providerState.Enabled,
                    providerIsStatic);
                return true;
            }

            if (rigidbody != null)
            {
                bool isStatic = bodyMode == Physics2DBridgeManualBodyMode.Static ||
                    (bodyMode == Physics2DBridgeManualBodyMode.Auto && rigidbody.bodyType == RigidbodyType2D.Static);
                state = new Physics2DBridgeBodyState(
                    rigidbody.position,
                    rigidbody.rotation,
                    rigidbody.linearVelocity,
                    rigidbody.angularVelocity,
                    rigidbody.simulated && rigidbody.gameObject.activeInHierarchy,
                    isStatic);
                return true;
            }

            poseTransform = poseTransform != null ? poseTransform : transform;
            if (poseTransform == null)
            {
                state = default;
                return false;
            }

            bool transformIsStatic = bodyMode != Physics2DBridgeManualBodyMode.Kinematic;
            Vector3 position = poseTransform.position;
            state = new Physics2DBridgeBodyState(
                position,
                poseTransform.eulerAngles.z,
                Vector2.zero,
                0f,
                isActiveAndEnabled && poseTransform.gameObject.activeInHierarchy,
                transformIsStatic);
            return true;
        }



        internal bool TryGetConfiguredPresentationPose(
            out Vector2 position,
            out float rotationDegrees)
        {
            Transform poseTransform = hasRuntimeConfiguration ? runtimePoseTransform : manualPoseTransform;
            if (poseTransform != null)
            {
                position = poseTransform.position;
                rotationDegrees = poseTransform.eulerAngles.z;
                return true;
            }

            Rigidbody2D poseRigidbody = hasRuntimeConfiguration ? runtimePoseRigidbody : manualPoseRigidbody;
            if (poseRigidbody != null)
            {
                Transform rigidbodyTransform = poseRigidbody.transform;
                position = rigidbodyTransform.position;
                rotationDegrees = rigidbodyTransform.eulerAngles.z;
                return true;
            }

            position = default;
            rotationDegrees = default;
            return false;
        }



        private int CopyManualShapes(
            List<Physics2DBridgeShapeDescriptor> shapes,
            List<Vector2> vertices)
        {
            shapes.Clear();
            vertices.Clear();
            IReadOnlyList<Physics2DBridgeManualShape2D> sources = hasRuntimeConfiguration
                ? runtimeManualShapes
                : manualShapes;
            for (int i = 0; i < sources.Count; i++)
            {
                sources[i]?.TryAppendDescriptor(shapes, vertices);
            }
            return shapes.Count;
        }



        private void NotifyGeometryConfigurationChanged()
        {
            if (registeredRegistry != null && registrationHandle.IsValid)
            {
                registeredRegistry.MarkGeometryDirty(this);
                return;
            }

            if (!isActiveAndEnabled) { return; }
            if (boundRegistry != null)
            {
                boundRegistry.TryRegisterOrQueue(this, out registrationHandle);
                return;
            }
            Physics2DBridgeRegistry.NotifyEnabled(this);
        }



        private void IncrementManualGeometryRevision()
        {
            manualGeometryRevision = manualGeometryRevision == int.MaxValue ? 1 : manualGeometryRevision + 1;
        }



        private static Collider2D[] CopyColliders(IReadOnlyList<Collider2D> source)
        {
            if (source == null || source.Count == 0) { return Array.Empty<Collider2D>(); }
            var copy = new Collider2D[source.Count];
            for (int i = 0; i < copy.Length; i++) { copy[i] = source[i]; }
            return copy;
        }



        private static Physics2DBridgeManualShape2D[] CopyManualShapes(
            IReadOnlyList<Physics2DBridgeManualShape2D> source)
        {
            if (source == null || source.Count == 0) { return Array.Empty<Physics2DBridgeManualShape2D>(); }
            var copy = new Physics2DBridgeManualShape2D[source.Count];
            for (int i = 0; i < copy.Length; i++) { copy[i] = source[i]?.Clone(); }
            return copy;
        }



        private static bool CanApplyRuntimeConfiguration(
            in Physics2DBridgeRuntimeConfiguration configuration)
        {
            switch (configuration.ShapeSourceMode)
            {
                case Physics2DBridgeShapeSourceMode.ProviderOnly:
                    return configuration.Provider != null;

                case Physics2DBridgeShapeSourceMode.SelectedColliders:
                    return ContainsNonNull(configuration.SelectedColliders);

                case Physics2DBridgeShapeSourceMode.ManualShapes:
                    return ContainsValidManualShape(configuration.ManualShapes);

                case Physics2DBridgeShapeSourceMode.CollidersOnly:
                case Physics2DBridgeShapeSourceMode.CollidersThenProvider:
                    return true;

                default:
                    return false;
            }
        }



        private bool ApplyShapeSources(
            IPhysics2DBridgeShapeProvider provider,
            Physics2DBridgeShapeSourceMode sourceMode)
        {
            if (!hasRuntimeConfiguration && ReferenceEquals(shapeProvider, provider) && shapeSourceMode == sourceMode)
            {
                return false;
            }

            hasRuntimeConfiguration = false;
            runtimeSelectedColliders = Array.Empty<Collider2D>();
            runtimeManualShapes = Array.Empty<Physics2DBridgeManualShape2D>();
            runtimePoseRigidbody = null;
            runtimePoseTransform = null;
            runtimeTargetComponent = null;
            runtimeShapeProvider = null;
            shapeProvider = provider;
            shapeSourceMode = sourceMode;
            IncrementManualGeometryRevision();
            return true;
        }



        private static bool ContainsNonNull<T>(IReadOnlyList<T> source) where T : class
        {
            if (source == null) { return false; }
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null) { return true; }
            }
            return false;
        }



        private static bool ContainsValidManualShape(
            IReadOnlyList<Physics2DBridgeManualShape2D> source)
        {
            if (source == null) { return false; }
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null && source[i].IsValid) { return true; }
            }
            return false;
        }



        private void OnValidate()
        {
            IncrementManualGeometryRevision();
            if (Application.isPlaying) { NotifyGeometryConfigurationChanged(); }
        }



#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (registeredRegistry == null || !registrationHandle.IsValid ||
                debugDrawMode == Physics2DBridgeDebugDrawMode.Off)
            {
                return;
            }

            float alpha = Application.isPlaying && Time.fixedDeltaTime > 0f
                ? Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime)
                : 1f;
            for (int bodyIndex = 0; registeredRegistry.TryGetDiagnostics(
                    registrationHandle,
                    bodyIndex,
                    alpha,
                    out Physics2DBridgePoseDiagnostics diagnostics); bodyIndex++)
            {
                registeredRegistry.DrawDiagnostics(
                    registrationHandle,
                    bodyIndex,
                    in diagnostics,
                    debugDrawMode);
            }
        }
#endif



        private void OnEnable()
        {
            if (boundRegistry != null)
            {
                boundRegistry.TryRegisterOrQueue(this, out registrationHandle);
                return;
            }

            Physics2DBridgeRegistry.NotifyEnabled(this);
        }



        private void OnDisable()
        {
            if (boundRegistry != null)
            {
                if (registeredRegistry != null && registrationHandle.IsValid)
                {
                    registeredRegistry.Unregister(registrationHandle);
                }
                registeredRegistry = null;
                registrationHandle = default;
                return;
            }

            Physics2DBridgeRegistry.NotifyDisabled(this);
        }



        private void OnTransformChildrenChanged()
        {
            if (!Application.isPlaying) { return; }
            NotifyGeometryConfigurationChanged();
        }



        internal void SetRegistration(
            Physics2DBridgeRegistry registry,
            Physics2DBridgeRegistrationHandle handle)
        {
            registeredRegistry = registry;
            registrationHandle = handle;
            if (pendingStrictCcdSteps > 0)
            {
                registry.RequestStrictCcd(this, pendingStrictCcdSteps);
                pendingStrictCcdSteps = 0;
            }
            if (pendingTeleport)
            {
                registry.MarkTeleported(this);
                pendingTeleport = false;
            }
        }



        internal void ClearRegistration(Physics2DBridgeRegistry registry)
        {
            if (!ReferenceEquals(registeredRegistry, registry)) { return; }
            registeredRegistry = null;
            registrationHandle = default;
        }



        internal bool IsExplicitlyBoundTo(Physics2DBridgeRegistry registry) =>
            ReferenceEquals(boundRegistry, registry);



        internal void OnRegistryDisposed(Physics2DBridgeRegistry registry)
        {
            ClearRegistration(registry);
            if (!ReferenceEquals(boundRegistry, registry)) { return; }

            boundRegistry = null;
            if (isActiveAndEnabled) { Physics2DBridgeRegistry.NotifyEnabled(this); }
        }
    }
}
