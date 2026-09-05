using System;
using UnityEngine;



namespace Pan.Tan
{
    /// <summary>
    /// backend 내부 저장소를 노출하지 않고 현재 Tan 상태를 읽는 불변 복사본입니다.
    /// </summary>
    public readonly struct TanSnapshot
    {
        /// <summary>
        /// 회전 정보가 없던 기존 consumer를 위한 호환 생성자입니다.
        /// 회전은 0도로 초기화됩니다.
        /// </summary>
        public TanSnapshot(
            in TanKey key,
            TanBackendKind backendKind,
            Vector2 previousPosition,
            Vector2 position,
            Vector2 velocity,
            float radius,
            OptionalTanFloat remainingLifetime,
            int teamId,
            int visualId,
            Vector2 visualScale,
            Color32 color)
            : this(
                in key,
                backendKind,
                previousPosition,
                position,
                velocity,
                radius,
                remainingLifetime,
                teamId,
                visualId,
                visualScale,
                color,
                0f)
        {
        }

        /// <summary>
        /// backend 내부 상태를 회전을 포함한 불변 Tan snapshot으로 복사합니다.
        /// </summary>
        public TanSnapshot(
            in TanKey key,
            TanBackendKind backendKind,
            Vector2 previousPosition,
            Vector2 position,
            Vector2 velocity,
            float radius,
            OptionalTanFloat remainingLifetime,
            int teamId,
            int visualId,
            Vector2 visualScale,
            Color32 color,
            float rotationDegrees)
        {
            Key = key;
            BackendKind = backendKind;
            PreviousPosition = previousPosition;
            Position = position;
            Velocity = velocity;
            Radius = radius;
            RemainingLifetime = remainingLifetime;
            TeamId = teamId;
            VisualId = visualId;
            VisualScale = visualScale;
            Color = color;
            RotationDegrees = float.IsFinite(rotationDegrees) ? rotationDegrees : 0f;
        }



        public TanKey Key { get; }
        public TanBackendKind BackendKind { get; }
        public Vector2 PreviousPosition { get; }
        public Vector2 Position { get; }
        public Vector2 Velocity { get; }
        public float Radius { get; }
        public OptionalTanFloat RemainingLifetime { get; }
        public int TeamId { get; }
        public int VisualId { get; }
        public Vector2 VisualScale { get; }
        public Color32 Color { get; }

        /// <summary>
        /// 현재 Tan 그래픽의 월드 회전 각도입니다.
        /// </summary>
        public float RotationDegrees { get; }
    }



    /// <summary>
    /// NormalTan과 ElementTan consumer가 공유하는 최소 runtime 계약입니다.
    /// </summary>
    public interface ITanRuntimeBackend : IDisposable
    {
        int ContextId { get; }
        TanBackendKind BackendKind { get; }
        int ActiveCount { get; }
        bool IsAvailable { get; }
        bool IsAlive(in TanKey key);
        bool TrySpawn(in TanSpawnRequest request, out TanHandle handle);
        bool TryGetSnapshot(in TanKey key, out TanSnapshot snapshot);
        bool TrySubmit(in TanKey key, in TanCommand command);
        event TanFactHandler FactRaised;
    }



    /// <summary>
    /// 선택형 이동·표시 Feature 진단을 지원하는 backend가 구현하는 확장 계약입니다.
    /// 기존 runtime 계약을 깨지 않도록 기본 <see cref="ITanRuntimeBackend"/>와 분리합니다.
    /// </summary>
    public interface ITanFeatureRuntimeBackend
    {
        bool TryGetFeatureSnapshot(in TanKey key, out TanFeatureSnapshot snapshot);
    }



    /// <summary>
    /// World 또는 Camera의 공유 경계를 Tan별 반복 명령 없이 한 번 갱신하는 backend 확장 계약입니다.
    /// Camera 탐색과 Bounds 계산은 consumer가 담당합니다.
    /// </summary>
    public interface ITanBoundaryRuntimeBackend
    {
        bool TrySetBoundaryBounds2D(TanBoundaryMode mode, Rect bounds);
    }



    /// <summary>
    /// managed gameplay 경계에서 runtime과 generation-safe key를 함께 보관하는 값 형식 handle입니다.
    /// </summary>
    /// <remarks>
    /// Runtime 참조를 강하게 유지하므로 장기 보관보다 gameplay 수명 안에서 사용해야 합니다.
    /// Native/Burst 상태에는 이 handle을 저장하지 말고 <see cref="TanKey"/>만 저장합니다.
    /// </remarks>
    public readonly struct TanHandle : IEquatable<TanHandle>
    {
        public TanHandle(ITanRuntimeBackend runtime, in TanKey key)
        {
            Runtime = runtime;
            Key = key;
        }



        private ITanRuntimeBackend Runtime { get; }
        internal ITanRuntimeBackend RuntimeOwner => Runtime;
        public TanKey Key { get; }
        public bool IsAlive
        {
            get
            {
                TanKey key = Key;
                return IsRuntimeValid() && Runtime.IsAlive(in key);
            }
        }
        public TanBackendKind BackendKind => IsRuntimeValid() ? Runtime.BackendKind : TanBackendKind.Unknown;



        public bool TryGetSnapshot(out TanSnapshot snapshot)
        {
            TanKey key = Key;
            if (IsRuntimeValid() && Runtime.TryGetSnapshot(in key, out snapshot)) { return true; }
            snapshot = default;
            return false;
        }

        /// <summary>
        /// runtime이 지원할 때 실제로 할당된 선택형 Tan Feature 상태를 조회합니다.
        /// </summary>
        public bool TryGetFeatureSnapshot(out TanFeatureSnapshot snapshot)
        {
            TanKey key = Key;
            if (IsRuntimeValid() &&
                Runtime is ITanFeatureRuntimeBackend featureRuntime &&
                featureRuntime.TryGetFeatureSnapshot(in key, out snapshot))
            {
                return true;
            }

            snapshot = default;
            return false;
        }

        public bool TrySubmit<TCommand>(in TCommand command)
            where TCommand : unmanaged, ITanCommand
        {
            if (!IsRuntimeValid()) { return false; }
            TanCommand value = command.ToTanCommand();
            TanKey key = Key;
            return Runtime.TrySubmit(in key, in value);
        }

        public bool Equals(TanHandle other) => ReferenceEquals(Runtime, other.Runtime) && Key.Equals(other.Key);
        public override bool Equals(object obj) => obj is TanHandle other && Equals(other);
        public override int GetHashCode() => ((Runtime != null ? Runtime.GetHashCode() : 0) * 397) ^ Key.GetHashCode();
        public static bool operator ==(TanHandle left, TanHandle right) => left.Equals(right);
        public static bool operator !=(TanHandle left, TanHandle right) => !left.Equals(right);



        private bool IsRuntimeValid() =>
            Runtime != null && Runtime.IsAvailable && Key.IsValid && Runtime.ContextId == Key.ContextId;

        internal bool IsOwnedBy(ITanRuntimeBackend runtime) => ReferenceEquals(Runtime, runtime);
    }
}
