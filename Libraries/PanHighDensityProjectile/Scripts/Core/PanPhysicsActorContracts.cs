using UnityEngine;

namespace Pan.HighDensityProjectile
{

    /// <summary>
    /// 일반 물리 actor를 투사체/타겟/이동 시스템이 느슨하게 해석할 때 사용하는 대표 shape입니다.
    /// 구현체가 정확한 collider 종류를 노출하지 않아도, 반지름 또는 box 근사치만으로 빠른 query를 구성할 수 있습니다.
    /// </summary>
    public enum PanPhysicsActorShape
    {
        /// <summary>shape 정보를 제공하지 않습니다.</summary>
        Unknown,

        /// <summary>중심점과 반지름으로 근사할 수 있는 actor입니다.</summary>
        Radius,

        /// <summary>2D box 크기로 근사할 수 있는 actor입니다.</summary>
        Box2D,

        /// <summary>3D box 크기로 근사할 수 있는 actor입니다.</summary>
        Box3D
    }



    /// <summary>
    /// `IPanPhysicsActor` 구현체의 현재 물리 상태를 읽기 전용으로 전달하는 snapshot입니다.
    /// 투사체, target area binder, animation/event 계층은 구체 물리 backend를 모르고 이 값만으로 필요한 결정을 내립니다.
    /// </summary>
    public readonly struct PanPhysicsActorSnapshot
    {
        /// <summary>
        /// 물리 actor snapshot을 생성합니다.
        /// 구현체는 자신이 지원하지 않는 값의 `Has*` 또는 `Can*` 플래그를 false로 두어 호출자가 안전하게 분기할 수 있게 합니다.
        /// </summary>
        public PanPhysicsActorSnapshot(
        Component source,
        Vector3 position,
        Vector3 expectedVelocity,
        Vector3 actualVelocity,
        PanPhysicsActorShape shape,
        Vector3 bodySize,
        float radius,
        bool hasPosition,
        bool hasExpectedVelocity,
        bool hasActualVelocity,
        bool canMoveInstant,
        bool canAddScaledMovement,
        bool canAddUnscaledMovement,
        bool hasDimension,
        bool is2D)
        {
            Source = source;
            Position = position;
            ExpectedVelocity = expectedVelocity;
            ActualVelocity = actualVelocity;
            Shape = shape;
            BodySize = bodySize;
            Radius = radius;
            HasPosition = hasPosition;
            HasExpectedVelocity = hasExpectedVelocity;
            HasActualVelocity = hasActualVelocity;
            CanMoveInstant = canMoveInstant;
            CanAddScaledMovement = canAddScaledMovement;
            CanAddUnscaledMovement = canAddUnscaledMovement;
            HasDimension = hasDimension;
            Is2D = is2D;
        }



        /// <summary>snapshot을 만든 원본 component입니다.</summary>
        public Component Source { get; }

        /// <summary>원본 component가 붙어 있는 GameObject입니다.</summary>
        public GameObject GameObject => Source != null ? Source.gameObject : null;

        /// <summary>actor의 현재 world position입니다. `HasPosition`이 true일 때 신뢰합니다.</summary>
        public Vector3 Position { get; }

        /// <summary>입력/AI/외부 force가 의도한 velocity입니다. `HasExpectedVelocity`가 true일 때 신뢰합니다.</summary>
        public Vector3 ExpectedVelocity { get; }

        /// <summary>물리 backend가 실제로 계산한 velocity입니다. `HasActualVelocity`가 true일 때 신뢰합니다.</summary>
        public Vector3 ActualVelocity { get; }

        /// <summary>빠른 query나 target area 동기화에 사용할 actor shape입니다.</summary>
        public PanPhysicsActorShape Shape { get; }

        /// <summary>box shape일 때 사용할 world 기준 body 크기입니다.</summary>
        public Vector3 BodySize { get; }

        /// <summary>radius shape 또는 단순 근사에 사용할 반지름입니다.</summary>
        public float Radius { get; }

        /// <summary>`Position` 값이 유효한지 나타냅니다.</summary>
        public bool HasPosition { get; }

        /// <summary>`ExpectedVelocity` 값이 유효한지 나타냅니다.</summary>
        public bool HasExpectedVelocity { get; }

        /// <summary>`ActualVelocity` 값이 유효한지 나타냅니다.</summary>
        public bool HasActualVelocity { get; }

        /// <summary>actor가 즉시 위치 이동 명령을 받을 수 있는지 나타냅니다.</summary>
        public bool CanMoveInstant { get; }

        /// <summary>actor가 time scale이 반영된 movement delta를 받을 수 있는지 나타냅니다.</summary>
        public bool CanAddScaledMovement { get; }

        /// <summary>actor가 unscaled movement delta를 받을 수 있는지 나타냅니다.</summary>
        public bool CanAddUnscaledMovement { get; }

        /// <summary>`Is2D` 값이 유효한지 나타냅니다.</summary>
        public bool HasDimension { get; }

        /// <summary>2D 물리 actor인지 나타냅니다. `HasDimension`이 true일 때 신뢰합니다.</summary>
        public bool Is2D { get; }
    }



    /// <summary>
    /// 플레이어, 적, 보스, 간단한 물리 box 등 서로 다른 물리 구현체를 같은 방식으로 다루기 위한 최소 계약입니다.
    /// 투사체 시스템은 이 계약 또는 이 계약을 target area로 변환하는 adapter만 바라보고, 구체 actor 구현에는 의존하지 않습니다.
    /// </summary>
    public interface IPanPhysicsActor
    {
        /// <summary>actor를 대표하는 Unity component입니다.</summary>
        Component Source { get; }

        /// <summary>현재 actor 상태를 snapshot으로 가져옵니다. 실패하면 호출자는 해당 actor를 이번 frame 처리에서 제외합니다.</summary>
        bool TryGetSnapshot(out PanPhysicsActorSnapshot snapshot);

        /// <summary>backend가 허용하면 actor 위치를 직접 설정합니다.</summary>
        bool TrySetPhysicsPosition(Vector3 position);

        /// <summary>backend가 허용하면 actor를 즉시 delta만큼 이동합니다.</summary>
        bool TryMoveInstant(Vector3 delta);

        /// <summary>backend가 허용하면 time scale이 반영되는 movement delta를 추가합니다.</summary>
        bool TryAddScaledMovement(Vector3 delta);

        /// <summary>backend가 허용하면 time scale과 무관한 movement delta를 추가합니다.</summary>
        bool TryAddUnscaledMovement(Vector3 delta);
    }
}
