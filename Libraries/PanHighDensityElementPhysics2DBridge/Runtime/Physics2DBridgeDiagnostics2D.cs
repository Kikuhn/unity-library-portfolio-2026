using Unity.U2D.Physics;
using UnityEngine;



namespace Pan.HighDensityElement.Physics2DBridge
{
    /// <summary>
    /// 빠르게 이동한 Legacy Projection의 이전·현재 pose와 Core proxy를 묶은 읽기 전용 스냅샷입니다.
    /// </summary>
    public readonly struct Physics2DBridgeStrictMotion2D
    {
        internal Physics2DBridgeStrictMotion2D(
            Physics2DBridgeRegistrationHandle registration,
            int bodyIndex,
            PhysicsBody coreBody,
            Vector2 previousPosition,
            float previousRotationDegrees,
            Vector2 position,
            float rotationDegrees,
            bool teleported,
            bool strictCcdRequested)
        {
            Registration = registration;
            BodyIndex = bodyIndex;
            CoreBody = coreBody;
            PreviousPosition = previousPosition;
            PreviousRotationDegrees = previousRotationDegrees;
            Position = position;
            RotationDegrees = rotationDegrees;
            Teleported = teleported;
            StrictCcdRequested = strictCcdRequested;
        }



        /// <summary>
        /// Projection을 소유하는 세대형 브리지 등록 Handle입니다.
        /// </summary>
        public Physics2DBridgeRegistrationHandle Registration { get; }

        /// <summary>
        /// 한 브리지 등록 안에서 Rigidbody 그룹을 구분하는 번호입니다.
        /// </summary>
        public int BodyIndex { get; }

        /// <summary>
        /// Legacy 형상을 수동적으로 투영한 Core Body입니다.
        /// </summary>
        public PhysicsBody CoreBody { get; }

        /// <summary>
        /// 직전 동기화 경계의 Legacy 위치입니다.
        /// </summary>
        public Vector2 PreviousPosition { get; }

        /// <summary>
        /// 직전 동기화 경계의 Legacy 회전 각도입니다.
        /// </summary>
        public float PreviousRotationDegrees { get; }

        /// <summary>
        /// 현재 동기화 경계의 Legacy 위치입니다.
        /// </summary>
        public Vector2 Position { get; }

        /// <summary>
        /// 현재 동기화 경계의 Legacy 회전 각도입니다.
        /// </summary>
        public float RotationDegrees { get; }

        /// <summary>
        /// 이번 이동이 중간 경로를 검사하지 않는 순간이동인지 나타냅니다.
        /// </summary>
        public bool Teleported { get; }

        /// <summary>
        /// 이번 동기화에서 상대 운동 CCD 입력으로 제출되었는지 나타냅니다.
        /// </summary>
        public bool StrictCcdRequested { get; }
    }



    /// <summary>
    /// Editor와 Development 진단 도구가 브리지 내부 컬렉션에 접근하지 않고 표시할 수 있는 집계 값입니다.
    /// </summary>
    public readonly struct Physics2DBridgeRegistryDiagnostics
    {
        internal Physics2DBridgeRegistryDiagnostics(
            int targetCount,
            int bodyCount,
            int shapeCount,
            int strictMotionCount,
            long synchronizationSequence)
        {
            TargetCount = targetCount;
            BodyCount = bodyCount;
            ShapeCount = shapeCount;
            StrictMotionCount = strictMotionCount;
            SynchronizationSequence = synchronizationSequence;
        }



        /// <summary>
        /// 원본 Legacy identity로 복원할 수 있는 논리 대상 수입니다.
        /// </summary>
        public int TargetCount { get; }

        /// <summary>
        /// attached Rigidbody 단위로 그룹화된 Projection Body 수입니다.
        /// </summary>
        public int BodyCount { get; }

        /// <summary>
        /// Core World에 생성된 Projection Shape 수입니다.
        /// </summary>
        public int ShapeCount { get; }

        /// <summary>
        /// 최근 동기화에서 상대 운동 CCD 후보가 된 Body 수입니다.
        /// </summary>
        public int StrictMotionCount { get; }

        /// <summary>
        /// 최근 완료된 Projection 동기화의 단조 증가 순번입니다.
        /// </summary>
        public long SynchronizationSequence { get; }
    }
}
