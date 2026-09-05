using System;
using UnityEngine;



namespace Pan.Tan
{
    public enum TanFactType : byte
    {
        Spawned = 0,
        Contact = 1,
        LifetimeExpired = 2,
        Despawned = 3,

        /// <summary>
        /// World 또는 Camera 경계 Feature가 Tan을 유효 영역 밖으로 판정했습니다.
        /// </summary>
        BoundaryExited = 4
    }



    /// <summary>
    /// 접촉 형상 값 중 실제 물리 쿼리가 제공한 값과 시작 중첩 상태를 구분합니다.
    /// </summary>
    [Flags]
    public enum TanContactGeometryFlags : byte
    {
        None = 0,
        SurfacePointValid = 1 << 0,
        NormalValid = 1 << 1,
        IncomingDisplacementValid = 1 << 2,
        StartedOverlapped = 1 << 3,
        ImpactCenterValid = 1 << 4,
        HasSurfacePoint = SurfacePointValid,
        HasSurfaceNormal = NormalValid,
        HasIncomingDisplacement = IncomingDisplacementValid,
        HasImpactCenter = ImpactCenterValid
    }



    /// <summary>
    /// backend가 계산한 접촉 시점과 고정 스텝 종료 시점을 함께 보존하는 값 형식 형상 정보입니다.
    /// </summary>
    [Serializable]
    public readonly struct TanContactGeometry
    {
        public TanContactGeometry(
            Vector2 surfacePoint,
            Vector2 tanCenterAtImpact,
            Vector2 endOfStepPosition,
            Vector2 normal,
            Vector2 incomingDisplacement,
            float timeOfImpact,
            TanContactGeometryFlags flags)
        {
            SurfacePoint = surfacePoint;
            TanCenterAtImpact = tanCenterAtImpact;
            EndOfStepPosition = endOfStepPosition;
            Normal = normal;
            IncomingDisplacement = incomingDisplacement;
            TimeOfImpact = Mathf.Clamp01(timeOfImpact);
            Flags = flags;
        }



        /// <summary>
        /// 충돌 상대 표면의 월드 좌표입니다. <see cref="SurfacePointValid"/>가 참일 때만 사용합니다.
        /// </summary>
        public Vector2 SurfacePoint { get; }

        /// <summary>
        /// 충돌 시간 비율에 대응하는 탄 중심의 월드 좌표입니다.
        /// </summary>
        public Vector2 TanCenterAtImpact { get; }

        /// <summary>
        /// 충돌 때문에 탄을 되감지 않은 고정 스텝 종료 시점의 탄 중심입니다.
        /// </summary>
        public Vector2 EndOfStepPosition { get; }

        /// <summary>
        /// 충돌 상대에서 탄을 향하는 표면 법선입니다. <see cref="NormalValid"/>가 참일 때만 사용합니다.
        /// </summary>
        public Vector2 Normal { get; }
        public Vector2 SurfaceNormal => Normal;

        /// <summary>
        /// 이번 쿼리 구간에서 탄 중심이 이동한 변위입니다. 대상의 상대 이동량을 뜻하지 않습니다.
        /// </summary>
        public Vector2 IncomingDisplacement { get; }

        /// <summary>
        /// 이전 pose에서 현재 pose까지의 쿼리 구간 안에서 충돌한 정규화 시간 비율입니다.
        /// </summary>
        public float TimeOfImpact { get; }

        public TanContactGeometryFlags Flags { get; }
        public bool SurfacePointValid => (Flags & TanContactGeometryFlags.SurfacePointValid) != 0;
        public bool NormalValid => (Flags & TanContactGeometryFlags.NormalValid) != 0;
        public bool IncomingDisplacementValid =>
            (Flags & TanContactGeometryFlags.IncomingDisplacementValid) != 0;
        public bool StartedOverlapped => (Flags & TanContactGeometryFlags.StartedOverlapped) != 0;
        public bool ImpactCenterValid => (Flags & TanContactGeometryFlags.ImpactCenterValid) != 0;
        public bool HasSurfacePoint => SurfacePointValid;
        public bool HasSurfaceNormal => NormalValid;
        public bool HasImpactCenter => ImpactCenterValid;
    }



    /// <summary>
    /// NormalTan과 ElementTan adapter가 동일한 접촉 수학을 사용하도록 제공하는 순수 계산 도우미입니다.
    /// </summary>
    public static class TanContactMath
    {
        /// <summary>
        /// 쿼리 시작·종료 중심과 정규화 충돌 시간으로 충돌 순간 탄 중심을 계산합니다.
        /// </summary>
        public static Vector2 CalculateCenterAtImpact(
            Vector2 startPosition,
            Vector2 endPosition,
            float timeOfImpact) =>
            Vector2.LerpUnclamped(startPosition, endPosition, Mathf.Clamp01(timeOfImpact));

        /// <summary>
        /// 이번 쿼리 구간에서 탄 중심이 이동한 변위를 계산합니다.
        /// </summary>
        public static Vector2 CalculateIncomingDisplacement(
            Vector2 startPosition,
            Vector2 endPosition) => endPosition - startPosition;

        /// <summary>
        /// 법선이 탄의 진입 변위와 같은 방향이면 뒤집어 상대 표면에서 탄을 향하도록 정규화합니다.
        /// </summary>
        public static Vector2 NormalizeTargetToTanNormal(
            Vector2 normal,
            Vector2 incomingDisplacement)
        {
            if (normal.sqrMagnitude <= 0.00000001f) { return Vector2.zero; }

            Vector2 normalized = normal.normalized;
            if (incomingDisplacement.sqrMagnitude > 0.00000001f &&
                Vector2.Dot(normalized, incomingDisplacement) > 0f)
            {
                normalized = -normalized;
            }

            return normalized;
        }

        /// <summary>
        /// 상대 표면에서 탄을 향하는 법선을 기준으로 Kinematic 탄의 반사 속도를 계산합니다.
        /// </summary>
        public static Vector2 ReflectVelocity(
            Vector2 velocity,
            Vector2 targetToTanNormal,
            float restitution = 1f)
        {
            if (targetToTanNormal.sqrMagnitude <= 0.00000001f) { return velocity; }

            Vector2 normal = targetToTanNormal.normalized;
            float incomingSpeed = Vector2.Dot(velocity, normal);
            if (incomingSpeed >= 0f) { return velocity; }
            return velocity - (1f + Mathf.Max(0f, restitution)) * incomingSpeed * normal;
        }

        /// <summary>
        /// 충돌 순간 탄 중심을 표면 바깥쪽으로 작은 거리만큼 이동시켜 즉시 재중첩을 피합니다.
        /// </summary>
        public static Vector2 ApplySeparationEpsilon(
            Vector2 tanCenterAtImpact,
            Vector2 targetToTanNormal,
            float epsilon = 0.001f)
        {
            if (targetToTanNormal.sqrMagnitude <= 0.00000001f || epsilon <= 0f)
            {
                return tanCenterAtImpact;
            }

            return tanCenterAtImpact + targetToTanNormal.normalized * epsilon;
        }
    }



    /// <summary>
    /// 물리 runtime이 main thread gameplay 계층에 전달하는 값 형식 사실입니다.
    /// </summary>
    [Serializable]
    public readonly struct TanFact
    {
        public TanFact(
            TanFactType type,
            in TanKey tan,
            in TanTargetHandle target,
            Vector2 position,
            Vector2 normal = default,
            float timeOfImpact = 0f,
            uint fixedStepIndex = 0,
            ushort substepIndex = 0)
        {
            Type = type;
            Tan = tan;
            Target = target;
            Position = position;
            ContactGeometry = type == TanFactType.Contact
                ? new TanContactGeometry(
                    position,
                    position,
                    position,
                    normal,
                    default,
                    timeOfImpact,
                    BuildLegacyContactFlags(normal))
                : default;
            FixedStepIndex = fixedStepIndex;
            SubstepIndex = substepIndex;
        }

        private TanFact(
            in TanKey tan,
            in TanTargetHandle target,
            in TanContactGeometry contactGeometry,
            uint fixedStepIndex,
            ushort substepIndex)
        {
            Type = TanFactType.Contact;
            Tan = tan;
            Target = target;
            Position = contactGeometry.SurfacePoint;
            ContactGeometry = contactGeometry;
            FixedStepIndex = fixedStepIndex;
            SubstepIndex = substepIndex;
        }



        /// <summary>
        /// 완전한 접촉 형상 정보를 가진 Contact 사실을 생성합니다.
        /// </summary>
        public static TanFact CreateContact(
            in TanKey tan,
            in TanTargetHandle target,
            in TanContactGeometry contactGeometry,
            uint fixedStepIndex = 0,
            ushort substepIndex = 0) =>
            new TanFact(in tan, in target, in contactGeometry, fixedStepIndex, substepIndex);



        public TanFactType Type { get; }
        public TanKey Tan { get; }
        public TanTargetHandle Target { get; }

        /// <summary>
        /// Contact에서는 <see cref="SurfacePoint"/>와 같은 호환 위치이며, 다른 사실에서는 해당 사건의 위치입니다.
        /// </summary>
        public Vector2 Position { get; }

        public TanContactGeometry ContactGeometry { get; }
        public Vector2 SurfacePoint => ContactGeometry.SurfacePoint;
        public Vector2 TanCenterAtImpact => ContactGeometry.TanCenterAtImpact;
        public Vector2 EndOfStepPosition => ContactGeometry.EndOfStepPosition;
        public Vector2 Normal => ContactGeometry.Normal;
        public Vector2 SurfaceNormal => ContactGeometry.SurfaceNormal;
        public Vector2 IncomingDisplacement => ContactGeometry.IncomingDisplacement;
        public float TimeOfImpact => ContactGeometry.TimeOfImpact;
        public bool SurfacePointValid => ContactGeometry.SurfacePointValid;
        public bool NormalValid => ContactGeometry.NormalValid;
        public bool IncomingDisplacementValid => ContactGeometry.IncomingDisplacementValid;
        public bool StartedOverlapped => ContactGeometry.StartedOverlapped;
        public bool HasSurfacePoint => ContactGeometry.HasSurfacePoint;
        public bool HasSurfaceNormal => ContactGeometry.HasSurfaceNormal;
        public bool HasImpactCenter => ContactGeometry.HasImpactCenter;
        public uint FixedStepIndex { get; }
        public ushort SubstepIndex { get; }



        private static TanContactGeometryFlags BuildLegacyContactFlags(Vector2 normal)
        {
            TanContactGeometryFlags flags = TanContactGeometryFlags.SurfacePointValid;
            if (normal.sqrMagnitude > 0.00000001f) { flags |= TanContactGeometryFlags.NormalValid; }
            return flags;
        }
    }



    public delegate void TanFactHandler(in TanFact fact);
}
