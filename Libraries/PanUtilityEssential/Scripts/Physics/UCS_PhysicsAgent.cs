using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.IO;
using UnityEngine.Events;
using SitraUtils;
using DG.Tweening;
using Pan.Util;
using Unity.Mathematics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine.Assertions.Must;



//? [Physics] 물리에이전트 스크립트, 물리오브젝트와 상호작용에 있어서 직접적인 참조 대신 인터페이스를 활용하여 더욱 유연한 구조로 짜자! 가 정리되어있는 정도의 코드



namespace Pan.Util.PhysicsAgent
{
    ///======================================================================================================================================================



    //? 베이스 물리 에이전트



    ///<summary>
    ///모든 물리 에이전트 인터페이스는, 이 인터페이스를 상속받아야한다
    ///</summary>
    public interface IBasePhysicsAgent { }

    ///<summary>
    ///모든 물리 에이전트 클래스는 이 클래스를 상속받아야한다
    ///</summary>
    public abstract class BasePhysicsAgent { }



    ///======================================================================================================================================================



    //? 이동 (Movement, Velocity) 에이전트



    #region Vector Ops (공용 어댑터)

    /// <summary>다양한 벡터 타입을 동일한 패턴으로 다루기 위한 연산 어댑터.</summary>
    public interface IVectorOps<TVec>
    {
        TVec Zero { get; }
        bool IsZero(TVec v);
        TVec Add(TVec a, TVec b);
        TVec From(float x, float y, float z = 0f);
    }

    /// <summary>UnityEngine.Vector3용 연산 어댑터.</summary>
    public readonly struct Vector3Ops : IVectorOps<Vector3>
    {
        public Vector3 Zero => Vector3.zero;
        public bool IsZero(Vector3 v) => v == Vector3.zero;
        public Vector3 Add(Vector3 a, Vector3 b) => a + b;
        public Vector3 From(float x, float y, float z = 0f) => new Vector3(x, y, z);
    }

    /// <summary>Unity.Mathematics.float3용 연산 어댑터(DOTS/잡용).</summary>
    public readonly struct Float3Ops : IVectorOps<float3>
    {
        public float3 Zero => float3.zero;
        public bool IsZero(float3 v) => math.all(v == 0f);
        public float3 Add(float3 a, float3 b) => a + b;
        public float3 From(float x, float y, float z = 0f) => new float3(x, y, z);
    }

    #endregion



    #region 코어



    /// <summary>
    /// 이동 코어를 보유한 객체가 구현하는 인터페이스입니다.
    /// 즉시형(<see cref="MovementPhysicsAgentCore{TVec, TOps}"/>)과
    /// 누적형(<see cref="MovementPhysicsAgentCore_Pending{TVec, TOps}"/>) 모두 업캐스트되어 노출됩니다.
    /// </summary>
    public interface IHoldMovementScaledPhysicsAgentCore : IBasePhysicsAgent
    {
        /// <summary>
        /// 보유 중인 이동 코어입니다. 없을 수 있으므로 null 검사 후 사용하세요.
        /// </summary>
        MovementPhysicsAgentCore<Vector3, Vector3Ops> MovementAgentScaled { get; }
    }



    /// <summary>
    /// 이동 코어를 보유한 객체가 구현하는 인터페이스입니다.
    /// 즉시형(<see cref="MovementPhysicsAgentCore{TVec, TOps}"/>)과
    /// 누적형(<see cref="MovementPhysicsAgentCore_Pending{TVec, TOps}"/>) 모두 업캐스트되어 노출됩니다.
    /// </summary>
    public interface IHoldMovementUnScaledPhysicsAgentCore : IBasePhysicsAgent
    {
        /// <summary>
        /// 보유 중인 이동 코어입니다. 없을 수 있으므로 null 검사 후 사용하세요.
        /// </summary>
        MovementPhysicsAgentCore<Vector3, Vector3Ops> MovementAgentUnScaled { get; }
    }



    //. 즉시 전용

    /// <summary>
    /// 즉시 이동 적용만 담당하며, 누적 이동 상태는 보관하지 않는다.
    /// 파생 클래스는 <see cref="ApplyMovement(in TVec)"/>에서 실제 적용을 구현한다.
    /// </summary>
    /// <typeparam name="TVec">Vector3/float3 등.</typeparam>
    /// <typeparam name="TOps">벡터 연산 어댑터.</typeparam>
    public abstract class MovementPhysicsAgentCore<TVec, TOps> : BasePhysicsAgent
        where TOps : struct, IVectorOps<TVec>
    {
        protected static readonly TOps Ops = default;

        /// <summary>즉시 이동을 실제 대상에 적용한다(파생에서 구현).</summary>
        protected abstract void ApplyMovement(in TVec delta);

        /// <summary>통합 Δ 접근자(즉시형:get=0,set→Apply / 누적형:get/set=버퍼)</summary>
        public abstract TVec MovementDelta { get; set; }
    }

    /// <summary>
    /// 런타임(Vector3) 즉시 이동 에이전트의 편의 인터페이스.
    /// </summary>
    public interface IImmediateMovementPhysicsAgent : IBasePhysicsAgent
    {
        /// <summary>이동 Δ를 즉시 적용한다.</summary>
        /// <param name="delta">적용할 이동 Δ.</param>
        void ApplyMovement(Vector3 delta);

        /// <summary>2D 입력(z=0) 이동 Δ를 즉시 적용한다.</summary>
        /// <param name="delta">XY 이동 Δ.</param>
        void ApplyMovement2D(Vector2 delta);

        /// <summary>(x,y,z) 성분으로 구성된 이동 Δ를 즉시 적용한다.</summary>
        void ApplyMovement(float x, float y, float z);

        /// <summary>X 성분 이동 Δ를 즉시 적용한다.</summary>
        /// <param name="x">적용할 X 이동 Δ.</param>
        void ApplyMovementX(float x);

        /// <summary>Y 성분 이동 Δ를 즉시 적용한다.</summary>
        /// <param name="y">적용할 Y 이동 Δ.</param>
        void ApplyMovementY(float y);

        /// <summary>Z 성분 이동 Δ를 즉시 적용한다.</summary>
        /// <param name="z">적용할 Z 이동 Δ.</param>
        void ApplyMovementZ(float z);
    }

    /// <summary>
    /// 런타임(Vector3) 즉시 이동 에이전트의 추상 기본 구현.
    /// </summary>
    public abstract class MovementPhysicsAgent
        : MovementPhysicsAgentCore<Vector3, Vector3Ops>, IImmediateMovementPhysicsAgent
    {

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ApplyMovement(Vector3 delta) => ApplyMovement(in delta);

        /// <inheritdoc />
        public void ApplyMovement(float x, float y, float z) => ApplyMovement(Ops.From(x, y, z));

        /// <inheritdoc />
        public void ApplyMovement2D(Vector2 delta) => ApplyMovement(Ops.From(delta.x, delta.y, 0f));

        /// <inheritdoc />
        public void ApplyMovementX(float x) => ApplyMovement(Ops.From(x, 0f, 0f));

        /// <inheritdoc />
        public void ApplyMovementY(float y) => ApplyMovement(Ops.From(0f, y, 0f));

        /// <inheritdoc />
        public void ApplyMovementZ(float z) => ApplyMovement(Ops.From(0f, 0f, z));

        /// <summary>파생에서 실제 이동 적용을 구현한다.</summary>
        protected abstract override void ApplyMovement(in Vector3 delta);

        /// <inheritdoc />
        public override Vector3 MovementDelta { get => Vector3.zero; set => ApplyMovement(value); }

    }

    /// <summary>컨텍스트(Main 등)를 보유하는 즉시 이동 에이전트 제네릭 베이스.</summary>
    public abstract class MovementPhysicsAgent<T> : MovementPhysicsAgent
    {
        protected readonly T Main;
        protected MovementPhysicsAgent(T main) { Main = main; }
    }



    //. 펜딩 전용

    /// <summary>
    /// “모아두었다가 한 번에 적용”하는 Pending 코어.
    /// </summary>
    public abstract class MovementPhysicsAgentCore_Pending<TVec, TOps> : MovementPhysicsAgentCore<TVec, TOps>
        where TOps : struct, IVectorOps<TVec>
    {
        protected TVec pendingMovement;

        /// <summary>아직 적용되지 않은 이동 Δ가 존재하는지 여부.</summary>
        public bool HasPendingMovement => !Ops.IsZero(pendingMovement); //! Δ 유무

        /// <summary>누적된 이동 Δ를 영벡터로 초기화한다.</summary>
        public void ClearPendingMovement() => pendingMovement = Ops.Zero;

        /// <summary>이동 Δ를 기존 값에 누적한다.</summary>
        /// <param name="movement">누적할 이동 Δ.</param>
        public virtual void AddMovement(TVec movement) => pendingMovement = Ops.Add(pendingMovement, movement);


        /// <summary>이동 Δ를 새 값으로 덮어쓴다.</summary>
        /// <param name="movement">설정할 이동 Δ.</param>
        public virtual void SetMovement(TVec movement) => pendingMovement = movement;

        /// <summary>누적된 이동 Δ를 소비하여 즉시 적용한다.</summary>
        public void FlushPendingMovement()
        {
            //if (Ops.IsZero(pendingMovement)) return false; //! 적용할 Δ 없음
            var delta = pendingMovement;
            pendingMovement = Ops.Zero;
            ApplyMovement(in delta); //? 즉시 적용
        }
    }

    /// <summary>
    /// 런타임(Vector3) Pending 이동 에이전트 인터페이스.
    /// </summary>
    public interface IPendingMovementPhysicsAgent : IBasePhysicsAgent
    {
        /// <summary>누적된 이동 Δ.</summary>
        Vector3 PendingMovement { get; set; }

        /// <summary>아직 적용되지 않은 이동 Δ가 존재하는지 여부.</summary>
        bool HasPendingMovement { get; }

        /// <summary>누적된 이동 Δ를 영벡터로 초기화한다.</summary>
        void ClearPendingMovement();

        /// <summary>이동 Δ를 기존 값에 누적한다.</summary>
        void AddMovement(Vector3 movement);

        /// <summary>(x,y,z) 성분을 누적한다.</summary>
        void AddMovement(float x, float y, float z);

        /// <summary>2D 이동 Δ(z=0)를 누적한다.</summary>
        void AddMovement2D(Vector2 movement);

        /// <summary>X 성분을 누적한다.</summary>
        void AddMovementX(float x);

        /// <summary>Y 성분을 누적한다.</summary>
        void AddMovementY(float y);

        /// <summary>Z 성분을 누적한다.</summary>
        void AddMovementZ(float z);

        /// <summary>이동 Δ를 새 값으로 덮어쓴다.</summary>
        void SetMovement(Vector3 movement);

        /// <summary>(x,y,z) 성분으로 덮어쓴다.</summary>
        void SetMovement(float x, float y, float z);

        /// <summary>2D 이동 Δ(z=0)로 덮어쓴다.</summary>
        void SetMovement2D(Vector2 movement);

        /// <summary>X 성분만 덮어쓴다.</summary>
        void SetMovementX(float x);

        /// <summary>Y 성분만 덮어쓴다.</summary>
        void SetMovementY(float y);

        /// <summary>Z 성분만 덮어쓴다.</summary>
        void SetMovementZ(float z);

        /// <summary>누적된 이동 Δ를 즉시 적용(Flush)한다.</summary>
        void FlushPendingMovement();
    }

    /// <summary>
    /// 런타임(Vector3) Pending 이동 에이전트의 추상 기본 구현.
    /// 누적(펜딩) Δ를 내부에 보관하다가 필요 시 즉시 적용한다.
    /// </summary>
    public abstract class MovementPhysicsAgent_Pending
        : MovementPhysicsAgentCore_Pending<Vector3, Vector3Ops>, IPendingMovementPhysicsAgent
    {
        /// <inheritdoc />
        public Vector3 PendingMovement
        {
            get => pendingMovement;
            set => pendingMovement = value;
        }

        //. 누적/설정 오버로드
        /// <inheritdoc />
        public void AddMovement(float x, float y, float z) => base.AddMovement(Ops.From(x, y, z));

        /// <inheritdoc />
        public void AddMovement2D(Vector2 movement) => base.AddMovement(Ops.From(movement.x, movement.y, 0f));

        /// <inheritdoc />
        public void AddMovementX(float x) => base.AddMovement(Ops.From(x, 0f, 0f));

        /// <inheritdoc />
        public void AddMovementY(float y) => base.AddMovement(Ops.From(0f, y, 0f));

        /// <inheritdoc />
        public void AddMovementZ(float z) => base.AddMovement(Ops.From(0f, 0f, z));

        /// <inheritdoc />
        public void SetMovement(float x, float y, float z) => base.SetMovement(Ops.From(x, y, z));

        /// <inheritdoc />
        public void SetMovement2D(Vector2 movement) => base.SetMovement(Ops.From(movement.x, movement.y, 0f));

        /// <inheritdoc />
        public void SetMovementX(float x) => base.SetMovement(Ops.From(x, pendingMovement.y, pendingMovement.z));

        /// <inheritdoc />
        public void SetMovementY(float y) => base.SetMovement(Ops.From(pendingMovement.x, y, pendingMovement.z));

        /// <inheritdoc />
        public void SetMovementZ(float z) => base.SetMovement(Ops.From(pendingMovement.x, pendingMovement.y, z));

        /// <summary>파생에서 실제 이동 적용을 구현한다.</summary>
        protected abstract override void ApplyMovement(in Vector3 delta);

        /// <inheritdoc />
        public override Vector3 MovementDelta { get => PendingMovement; set => PendingMovement = value; }
    }

    /// <summary>컨텍스트(Main 등)를 보유하는 Pending 이동 에이전트 제네릭 베이스.</summary>
    public abstract class MovementPhysicsAgent_Pending<T> : MovementPhysicsAgent_Pending where T : class
    {
        protected readonly T Main;
        protected MovementPhysicsAgent_Pending(T main) { Main = main; }
    }

    #endregion



    #region DOTS/Jobs용 float3 버퍼

    /// <summary>DOTS/잡에서 사용할 수 있는 순수 float3 이동 버퍼.</summary>
    public struct MovementPhysicsBufferFloat3
    {
        public float3 pending;

        /// <summary>Δ가 존재하는지 여부.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool HasPending() => !math.all(pending == 0f);

        /// <summary>누적된 Δ를 0으로 초기화.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => pending = 0f;

        /// <summary>현재 Δ가 0인지 확인.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsZero() => math.all(pending == 0f);

        /// <summary>소비 없이 현재 Δ를 조회.</summary>
        /// <param name="d">조회된 Δ.</param>
        /// <returns>Δ가 존재하면 true.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPeek(out float3 d) { d = pending; return !IsZero(); }

        /// <summary>Δ를 누적(Add).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in float3 d) => pending += d;

        /// <summary>2D Δ(z=0)를 누적(Add).</summary>
        public void Add(in float2 d) => pending += new float3(d.x, d.y, 0f);

        /// <summary>(x,y,z) 성분을 누적(Add).</summary>
        public void Add(float x, float y, float z) => pending += new float3(x, y, z);

        /// <summary>X 성분만 누적(Add).</summary>
        public void AddX(float x) => pending.x += x;

        /// <summary>Y 성분만 누적(Add).</summary>
        public void AddY(float y) => pending.y += y;

        /// <summary>Z 성분만 누적(Add).</summary>
        public void AddZ(float z) => pending.z += z;

        /// <summary>Δ를 설정(Set).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(in float3 d) => pending = d;

        /// <summary>2D Δ(z=0)로 설정(Set).</summary>
        public void Set(in float2 d) => pending = new float3(d.x, d.y, 0f);

        /// <summary>Δ를 반환하고 내부 값을 0으로 초기화(Consume).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 Consume()
        {
            var d = pending; //. 반환할 Δ
            pending = 0f;    //. 소비 후 초기화
            return d;
        }

        /// <summary>Δ가 있을 때만 Consume하고 결과를 반환.</summary>
        /// <param name="d">소비된 Δ.</param>
        /// <returns>소비가 일어났다면 true.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryConsume(out float3 d)
        {
            if (math.all(pending == 0f)) { d = 0f; return false; } //. Δ 없음
            d = pending;
            pending = 0f;
            return true;
        }
    }



    /// <summary>각 버퍼의 Δ를 소비하여 출력 배열에 기록하는 잡 예시.</summary>
    [BurstCompile]
    public struct ConsumeMotionJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<MovementPhysicsBufferFloat3> Buffers;

        [WriteOnly] public NativeArray<float3> OutDelta;

        public void Execute(int index)
        {
            var buf = Buffers[index];
            if (buf.TryConsume(out var d)) OutDelta[index] = d;
            Buffers[index] = buf;
        }
    }

    #endregion



    #region 기타



    ///<summary>
    /// <b>물리</b>로 관리되는 좌표를 보유중인 물리 에이전트
    ///</summary>
    public interface IPhysicsPositionPhysicsAgent : IBasePhysicsAgent
    {
        Vector3 PhysicsPosition { get; set; }
    }



    /// <summary>
    /// 메서드가 실행되는 즉시 물리 이동이 되는 물리 에이전트
    /// </summary>
    public interface IInstantMovePositionPhysicsAgent : IBasePhysicsAgent
    {
        /// <summary>
        /// 즉시 받아온 <paramref name="velocity"/> 만큼 이동한다
        /// </summary>
        void ApplyInstanceMovement_Velocity(Vector3 velocity);

        /// <summary>
        /// 즉시 받아온 <paramref name="position"/>의 위치로 이동한다
        /// </summary>
        void ApplyInstanceMovement_Position(Vector3 position);
    }



    #endregion



    ///======================================================================================================================================================



    //? 상태 (Status) 에이전트



    /// <summary>
    /// 2D/3D 물리 모드를 노출하는 물리 에이전트
    ///</summary> 
    public interface IDimensionPhysicsAgent : IBasePhysicsAgent
    {
        /// <summary>
        /// 2D 물리 모드인지 여부 
        ///</summary>
        bool Is2D { get; }
    }



    #region 이동



    /// <summary>
    /// 다음 물리 프레임(시뮬레이션 스텝)에서 적용될 것으로
    /// <b>예상</b>하는 목표 속도(m/s)를 노출하는 공급자 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 이 값은 <b>즉시 이동을 발생시키지 않습니다</b>.
    /// 입력/AI/네트워크 등에서 프레임 사이(Update 등) 동안 갱신해 두고,
    /// 소비 측(예: CharacterControllerPro 래퍼)이 물리 프레임 경계
    /// (예: <c>OnPreSimulation(float dt)</c>)에서 읽어 적용합니다.
    /// </para>
    /// <para>
    /// 좌표계(월드/로컬)나 블렌딩/클램프 정책은 구현체가 결정합니다.
    /// 여러 소스의 기여를 합치려면 외부에서 집계한 값을 제공하세요.
    /// </para>
    /// </remarks>
    public interface IExpectedVelocityPhysicsAgent : IBasePhysicsAgent
    {
        /// <summary>
        /// 물리 프레임 경계에서 소비될 <b>목표(예상) 속도</b>(m/s)입니다.
        /// </summary>
        Vector3 ExpectedVelocity { get; }

        /// <summary>예상 속도로 “이동 중인지” 판정.</summary>
        /// <param name="applyDeadzone">미세값을 0으로 간주할지.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMovingExpected(bool applyDeadzone = true)
        {
            var v = ExpectedVelocity;

            if (!applyDeadzone)
                return (v.x * v.x + v.y * v.y + v.z * v.z) > 0f;

            const float axisEps = 1e-3f;   //. 축별 임계값(≤ 0 간주)
            const float magEpsSqr = 1e-12f;

            if (Mathf.Abs(v.x) <= axisEps) v.x = 0f;
            if (Mathf.Abs(v.y) <= axisEps) v.y = 0f;
            if (Mathf.Abs(v.z) <= axisEps) v.z = 0f;

            return (v.x * v.x + v.y * v.y + v.z * v.z) > magEpsSqr;
        }

        /// <summary>X축 예상 속도 기준 이동 판정.</summary>
        /// <param name="applyDeadzone">미세값을 0으로 간주할지.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMovingExpectedX(bool applyDeadzone = true)
        {
            float x = ExpectedVelocity.x;
            if (!applyDeadzone) return x != 0f;
            const float eps = 1e-3f;
            return x < -eps || x > eps;
        }

        /// <summary>Y축 예상 속도 기준 이동 판정.</summary>
        /// <param name="applyDeadzone">미세값을 0으로 간주할지.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMovingExpectedY(bool applyDeadzone = true)
        {
            float y = ExpectedVelocity.y;
            if (!applyDeadzone) return y != 0f;
            const float eps = 1e-3f;
            return y < -eps || y > eps;
        }

        /// <summary>Z축 예상 속도 기준 이동 판정.</summary>
        /// <param name="applyDeadzone">미세값을 0으로 간주할지.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMovingExpectedZ(bool applyDeadzone = true)
        {
            float z = ExpectedVelocity.z;
            if (!applyDeadzone) return z != 0f;
            const float eps = 1e-3f;
            return z < -eps || z > eps;
        }
    }



    /// <summary>
    /// 직전 물리 프레임에서 실제로 적용/반영된
    /// <b>실측 속도</b>(m/s)를 노출하는 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 일반적으로 물리 사후 단계(예: <c>OnPostSimulation(float dt)</c>)에서
    /// <c>(현재위치 - 이전위치) / dt</c>로 갱신하며,
    /// 충돌/마찰/슬라이딩/스냅 등 엔진의 모든 보정을 포함한 <b>결과 값</b>입니다.
    /// </para>
    /// <para>
    /// 디버깅, 카메라 보정, 이펙트 트리거 등에 유용합니다.
    /// </para>
    /// </remarks>
    public interface IActualVelocityPhysicsAgent : IBasePhysicsAgent
    {
        /// <summary>
        /// 직전 물리 스텝의 <b>실측 평균 속도</b>(m/s)입니다.
        /// </summary>
        Vector3 ActualVelocity { get; }

        /// <summary>실측 속도로 “이동 중인지” 판정.</summary>
        /// <param name="applyDeadzone">미세값을 0으로 간주할지.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMovingActual(bool applyDeadzone = true)
        {
            var v = ActualVelocity;

            if (!applyDeadzone)
                return (v.x * v.x + v.y * v.y + v.z * v.z) > 0f;

            const float axisEps = 1e-3f;
            const float magEpsSqr = 1e-12f;

            if (Mathf.Abs(v.x) <= axisEps) v.x = 0f;
            if (Mathf.Abs(v.y) <= axisEps) v.y = 0f;
            if (Mathf.Abs(v.z) <= axisEps) v.z = 0f;

            return (v.x * v.x + v.y * v.y + v.z * v.z) > magEpsSqr;
        }

        /// <summary>X축 실측 속도 기준 이동 판정.</summary>
        /// <param name="applyDeadzone">미세값을 0으로 간주할지.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMovingActualX(bool applyDeadzone = true)
        {
            float x = ActualVelocity.x;
            if (!applyDeadzone) return x != 0f;
            const float eps = 1e-3f;
            return x < -eps || x > eps;
        }

        /// <summary>Y축 실측 속도 기준 이동 판정.</summary>
        /// <param name="applyDeadzone">미세값을 0으로 간주할지.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMovingActualY(bool applyDeadzone = true)
        {
            float y = ActualVelocity.y;
            if (!applyDeadzone) return y != 0f;
            const float eps = 1e-3f;
            return y < -eps || y > eps;
        }

        /// <summary>Z축 실측 속도 기준 이동 판정.</summary>
        /// <param name="applyDeadzone">미세값을 0으로 간주할지.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMovingActualZ(bool applyDeadzone = true)
        {
            float z = ActualVelocity.z;
            if (!applyDeadzone) return z != 0f;
            const float eps = 1e-3f;
            return z < -eps || z > eps;
        }
    }



    ///<summary>
    ///움직이고 있는지를 확인하는 상태 물리 에이전트
    ///</summary>
    public interface IMovingPhysicsAgent : IExpectedVelocityPhysicsAgent, IActualVelocityPhysicsAgent { }



    /// <summary>
    /// 이전의 물리 좌표를 노출하는 인터페이스 
    /// </summary>
    public interface IPreviousFramePhysicsPositionPhysicsAgent : IBasePhysicsAgent
    {
        /// <summary>
        /// 이전 프레임의 물리 좌표 
        /// </summary>
        Vector3 PreviousFramePhysicsPosition { get; }
    }



    #endregion



    #region 땅



    ///<summary>
    ///접지 상태인지 확인하는 물리 상태 에이전트
    ///</summary>
    public interface IGroundedPhysicsAgent : IBasePhysicsAgent
    {
        /// <summary>
        /// 접지 상태를 반환
        /// </summary>
        bool IsGrounded { get; }
    }



    #region 이벤트



    /// <summary>
    /// 캐릭터가 비접지 상태에서 <b>접지(grounded)</b> 상태로 전환될 때 발생하는
    /// 이벤트를 노출합니다.
    /// </summary>
    /// <remarks>
    /// 일반적으로 물리 스텝에서 상태가 <c>false → true</c>로 바뀌는 “진입 순간”에 한 번만
    /// 발화되며, 접지 상태가 유지되는 동안에는 반복 발화되지 않습니다.
    /// 발행은 메인 스레드(게임 스레드)에서 이루어지는 것을 전제로 합니다.
    /// </remarks>
    public interface IGroundedEnterPhysicsAgentEvent : IGroundedPhysicsAgent
    {
        /// <summary>
        /// 접지 대상(<see cref="UnityEngine.Object"/>)과 함께 발생하는 진입 이벤트입니다.
        /// </summary>
        /// <remarks>
        /// 인자로 전달되는 오브젝트는 바닥 콜라이더나 플랫폼 등 “접지로 판정된 대상”입니다.
        /// 구현에 따라 <c>null</c> 일 수도 있습니다.
        /// </remarks>
        event Action<UnityEngine.Object> OnEnterGroundedObjectEvent;
    }

    /// <summary>
    /// 제네릭 형식의 접지 진입 이벤트를 노출합니다.
    /// </summary>
    /// <typeparam name="T">구독자에게 전달할 접지 대상의 형식.</typeparam>
    /// <remarks>
    /// <see cref="IGroundedEnterPhysicsAgentEvent"/> 와 동일한 타이밍에 발화되며,
    /// 대상 타입을 강하게 지정하고 싶을 때 사용합니다.
    /// </remarks>
    public interface IGroundedEnterPhysicsAgentEvent<T> : IGroundedEnterPhysicsAgentEvent
    {
        /// <summary>접지 대상 <typeparamref name="T"/>와 함께 발생하는 진입 이벤트입니다.</summary>
        event Action<T> OnEnterGroundedEvent;
    }



    /// <summary>
    /// 캐릭터가 <b>접지</b> 상태에서 비접지 상태로 전환될 때 발생하는
    /// 이벤트를 노출합니다.
    /// </summary>
    /// <remarks>
    /// 물리 스텝에서 상태가 <c>true → false</c>로 바뀌는 “이탈 순간”에 한 번만 발화됩니다.
    /// </remarks>
    public interface IGroundedExitPhysicsAgentEvent : IGroundedPhysicsAgent
    {
        /// <summary>
        /// 접지 대상(<see cref="UnityEngine.Object"/>)과 함께 발생하는 이탈 이벤트입니다.
        /// </summary>
        /// <remarks>
        /// 인자로 전달되는 오브젝트는 “직전까지 접지로 판정되던 대상”입니다.
        /// 구현에 따라 <c>null</c> 일 수도 있습니다.
        /// </remarks>
        event Action<UnityEngine.Object> OnExitGroundedObjectEvent;
    }

    /// <summary>
    /// 제네릭 형식의 접지 이탈 이벤트를 노출합니다.
    /// </summary>
    /// <typeparam name="T">구독자에게 전달할 접지 대상의 형식.</typeparam>
    /// <remarks>
    /// <see cref="IGroundedExitPhysicsAgentEvent"/> 와 동일한 타이밍에 발화됩니다.
    /// </remarks>
    public interface IGroundedExitPhysicsAgentEvent<T> : IGroundedExitPhysicsAgentEvent
    {
        /// <summary>접지 대상 <typeparamref name="T"/>와 함께 발생하는 이탈 이벤트입니다.</summary>
        event Action<T> OnExitGroundedEvent;
    }



    /// <summary>
    /// 접지 진입/이탈 두 이벤트를 함께 노출하는 통합 인터페이스입니다.
    /// </summary>
    public interface IGroundedPhysicsAgentEvents
        : IGroundedEnterPhysicsAgentEvent, IGroundedExitPhysicsAgentEvent
    { }

    /// <summary>
    /// 제네릭 형식의 접지 진입/이탈 이벤트를 함께 노출하는 통합 인터페이스입니다.
    /// </summary>
    /// <typeparam name="T">구독자에게 전달할 접지 대상의 형식.</typeparam>
    public interface IGroundedPhysicsAgentEvents<T>
        : IGroundedEnterPhysicsAgentEvent<T>, IGroundedExitPhysicsAgentEvent<T>
    { }



    #endregion



    ///<summary>
    ///안정적인 접지 상태인지 확인하는 물리 상태 에이전트
    ///</summary>
    public interface IStableGroundedPhysicsAgent : IBasePhysicsAgent
    {
        /// <summary>
        /// 안정적인 접지 상태를 반환
        /// </summary>
        bool IsStablyGrounded { get; }
    }

    #region 이벤트



    /// <summary>
    /// 캐릭터가 비안정 접지에서 <b>안정 접지(stable grounded)</b> 상태로
    /// 전환될 때 발생하는 이벤트를 노출합니다.
    /// </summary>
    /// <remarks>
    /// “안정 접지”는 엔진/컨트롤러가 서 있을 수 있다고 판단하는 표면(예: 최대 경사 각 내)의
    /// 접지 상태를 의미합니다. 물리 스텝에서 <c>false → true</c>로 바뀌는 순간에 발화됩니다.
    /// </remarks>
    public interface IStableGroundedEnterPhysicsAgentEvent : IStableGroundedPhysicsAgent
    {
        /// <summary>
        /// 안정 접지 대상(<see cref="UnityEngine.Object"/>)과 함께 발생하는 진입 이벤트입니다.
        /// </summary>
        /// <remarks>
        /// 인자는 안정 접지로 판정된 바닥/플랫폼 등입니다(구현에 따라 <c>null</c> 가능).
        /// </remarks>
        event Action<UnityEngine.Object> OnEnterStableGroundedObjectEvent;
    }

    /// <summary>
    /// 제네릭 형식의 안정 접지 진입 이벤트를 노출합니다.
    /// </summary>
    /// <typeparam name="T">구독자에게 전달할 안정 접지 대상의 형식.</typeparam>
    public interface IStableGroundedEnterPhysicsAgentEvent<T> : IStableGroundedEnterPhysicsAgentEvent
    {
        /// <summary>안정 접지 대상 <typeparamref name="T"/>와 함께 발생하는 진입 이벤트입니다.</summary>
        event Action<T> OnEnterStableGroundedEvent;
    }



    /// <summary>
    /// 캐릭터가 <b>안정 접지</b> 상태에서 벗어날 때 발생하는 이벤트를 노출합니다.
    /// </summary>
    /// <remarks>
    /// 물리 스텝에서 <c>true → false</c>로 바뀌는 순간에 발화됩니다.
    /// </remarks>
    public interface IStableGroundedExitPhysicsAgentEvent : IStableGroundedPhysicsAgent
    {
        /// <summary>
        /// 안정 접지 대상(<see cref="UnityEngine.Object"/>)과 함께 발생하는 이탈 이벤트입니다.
        /// </summary>
        /// <remarks>
        /// 인자는 직전까지 안정 접지로 판정되던 대상입니다(구현에 따라 <c>null</c> 가능).
        /// </remarks>
        event Action<UnityEngine.Object> OnExitStableGroundedObjectEvent;
    }

    /// <summary>
    /// 제네릭 형식의 안정 접지 이탈 이벤트를 노출합니다.
    /// </summary>
    /// <typeparam name="T">구독자에게 전달할 안정 접지 대상의 형식.</typeparam>
    public interface IStableGroundedExitPhysicsAgentEvent<T> : IStableGroundedExitPhysicsAgentEvent
    {
        /// <summary>안정 접지 대상 <typeparamref name="T"/>와 함께 발생하는 이탈 이벤트입니다.</summary>
        event Action<T> OnExitStableGroundedEvent;
    }



    /// <summary>
    /// 안정 접지 진입/이탈 두 이벤트를 함께 노출하는 통합 인터페이스입니다.
    /// </summary>
    public interface IStableGroundedPhysicsAgentEvents
        : IStableGroundedEnterPhysicsAgentEvent, IStableGroundedExitPhysicsAgentEvent
    { }

    /// <summary>
    /// 제네릭 형식의 안정 접지 진입/이탈 이벤트를 함께 노출하는 통합 인터페이스입니다.
    /// </summary>
    /// <typeparam name="T">구독자에게 전달할 안정 접지 대상의 형식.</typeparam>
    public interface IStableGroundedPhysicsAgentEvents<T>
        : IStableGroundedEnterPhysicsAgentEvent<T>, IStableGroundedExitPhysicsAgentEvent<T>
    { }



    #endregion



    #endregion



    public static partial class PhysicsAgentExtension
    {
        /// <summary>
        /// 예상 속도를 기준으로 “이동 중인지”를 판정합니다.
        /// </summary>
        /// <param name="applyDeadzone">
        /// <c>true</c>면 아주 작은 잡음(미세 속도)을 0으로 간주하는 데드존을 적용한 뒤 판정합니다.
        /// <c>false</c>면 원시 값으로 판정합니다.
        /// </param>
        /// <returns>이동 중이면 <c>true</c>, 아니면 <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMovingExpected(this IExpectedVelocityPhysicsAgent agent, bool applyDeadzone = true) => agent.IsMovingExpected(applyDeadzone);

        /// <summary>X축 예상 속도 기준 이동 판정.</summary>
        /// <param name="applyDeadzone">미세값을 0으로 간주할지.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMovingExpectedX(this IExpectedVelocityPhysicsAgent agent, bool applyDeadzone = true) => agent.IsMovingExpectedX(applyDeadzone);

        /// <summary>Y축 예상 속도 기준 이동 판정.</summary>
        /// <param name="applyDeadzone">미세값을 0으로 간주할지.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMovingExpectedY(this IExpectedVelocityPhysicsAgent agent, bool applyDeadzone = true) => agent.IsMovingExpectedY(applyDeadzone);

        /// <summary>Z축 예상 속도 기준 이동 판정.</summary>
        /// <param name="applyDeadzone">미세값을 0으로 간주할지.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMovingExpectedZ(this IExpectedVelocityPhysicsAgent agent, bool applyDeadzone = true) => agent.IsMovingExpectedZ(applyDeadzone);



        /// <summary>
        /// 실제 속도를 기준으로 “이동 중인지”를 판정합니다.
        /// </summary>
        /// <param name="applyDeadzone">
        /// <c>true</c>면 아주 작은 잡음(미세 속도)을 0으로 간주하는 데드존을 적용한 뒤 판정합니다.
        /// <c>false</c>면 원시 값으로 판정합니다.
        /// </param>
        /// <returns>이동 중이면 <c>true</c>, 아니면 <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMovingActual(this IActualVelocityPhysicsAgent agent, bool applyDeadzone = true) => agent.IsMovingActual(applyDeadzone);

        /// <summary>X축 실측 속도 기준 이동 판정.</summary>
        /// <param name="applyDeadzone">미세값을 0으로 간주할지.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMovingActualX(this IActualVelocityPhysicsAgent agent, bool applyDeadzone = true) => agent.IsMovingActualX(applyDeadzone);

        /// <summary>Y축 실측 속도 기준 이동 판정.</summary>
        /// <param name="applyDeadzone">미세값을 0으로 간주할지.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMovingActualY(this IActualVelocityPhysicsAgent agent, bool applyDeadzone = true) => agent.IsMovingActualY(applyDeadzone);

        /// <summary>Z축 실측 속도 기준 이동 판정.</summary>
        /// <param name="applyDeadzone">미세값을 0으로 간주할지.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMovingActualZ(this IActualVelocityPhysicsAgent agent, bool applyDeadzone = true) => agent.IsMovingActualZ(applyDeadzone);
    }



    ///======================================================================================================================================================



    //? 물리



    ///<summary>
    /// 물리 오브젝트의 질량(<see cref="float"/>)를 노출하는 물리 에이전트
    /// </summary>
    public interface IMassPhysicsAgent : IBasePhysicsAgent
    {
        /// <summary>
        /// 물리 오브젝트 질량 (<see cref="float"/>) 
        /// </summary>
        float Mass { get; set; }
    }



    /// <summary>
    /// 강제로 “지면 접촉 상태를 해제(비접지)”시키는 기능을 노출하는 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 점프 직전처럼, 다음 물리 스텝에서 캐릭터를 공중 상태로 처리해야 할 때 사용합니다.
    /// 예를 들어 <b>CharacterControllerPro</b>를 사용할 경우, 점프 이벤트 직전에 호출하여
    /// 스냅·지면 고정·지면 마찰 등의 접지 로직이 다음 스텝에 적용되지 않도록 합니다.
    /// </para>
    /// <para>
    /// 실제로 어떤 콜라이더/스냅/지면 캐시를 해제할지는 구현체에 따라 다를 수 있습니다.
    /// 이 호출은 속도(velocity)를 직접 변경하지 않으며, 단지 “접지 판정” 상태만 강제로 끕니다.
    /// </para>
    /// </remarks>
    public interface IForceNotGroundedPhysicsAgent : IBasePhysicsAgent
    {
        /// <summary>
        /// 다음 물리 업데이트에서 캐릭터가 “지면에 붙어있지 않은 상태”로 처리되도록 강제합니다.
        /// </summary>
        /// <remarks>
        /// 일반적으로 점프 임펄스(상향 속도)를 적용하기 <b>직전</b>에 호출합니다.
        /// 이렇게 하면 스냅/접지 마찰이 임펄스를 상쇄하거나 축소시키는 것을 방지할 수 있습니다.
        /// </remarks>
        void ForceNotGrounded();
    }



    //? 레이어마스크



    /// <summary>
    /// 일방통행(One-way) 플랫폼 판정을 위한 레이어 마스크를 제공하는 물리 에이전트입니다.
    /// </summary>
    /// <remarks>
    /// 상향 통과 / 하향 차단과 같은 동작을 구현할 때, Raycast/Overlap/캐스트의 충돌 필터로 사용됩니다.
    /// </remarks>
    public interface IOneWayPlatformsLayerMaskPhysicsAgent : IBasePhysicsAgent
    {
        /// <summary>
        /// One-way 플랫폼으로 취급할 레이어 묶음입니다.
        /// </summary>
        /// <remarks>
        /// 구현체에서 이 값을 사용해 충돌 필터링을 수행하세요.
        /// </remarks>
        //. 충돌 필터용 레이어 마스크
        LayerMask OneWayPlatformLayerMask { get; set; }
    }



    /// <summary>
    /// “안정적인 지면(Stable Ground)”의 최대 허용 경사 각도를 제공하는 물리 에이전트입니다.
    /// </summary>
    /// <remarks>
    /// 일반적으로 단위는 “도(degree)”를 가정합니다. 구현체 규칙에 맞춰 해석하세요.
    /// </remarks>
    public interface IStableGroundAnglePhysicsAgent : IBasePhysicsAgent
    {
        /// <summary>
        /// 안정적인 지면으로 간주할 수 있는 최대 경사 각도입니다.
        /// </summary>
        /// <remarks>
        /// 예: 값 이하의 경사면에서만 접지/스탠딩을 허용하도록 사용할 수 있습니다.
        /// </remarks>
        //! 구현체에서 유효 범위 검증 권장 (예: 0~90도). 범위를 벗어나면 비정상 동작 가능.
        float StableGroundSlopeAngle { get; set; }
    }



    /// <summary>
    /// “안정적인 지면(Stable Ground)” 판정을 위한 레이어 마스크를 제공하는 물리 에이전트입니다.
    /// </summary>
    /// <remarks>
    /// 접지(Raycast/ShapeCast) 시 어떤 레이어를 지면으로 볼지 결정합니다.
    /// </remarks>
    public interface IStableGroundLayerMaskPhysicsAgent : IBasePhysicsAgent
    {
        /// <summary>
        /// 안정적인 지면으로 취급할 레이어 묶음입니다.
        /// </summary>
        /// <remarks>
        /// 경사 체크, 스텝 오프셋 판정 등 다양한 지면 판정 로직에서 참조됩니다.
        /// </remarks>
        //. 지면 판정용 레이어 마스크
        LayerMask StableGroundLayerMask { get; set; }
    }



    /// <summary>
    /// “동적인 지면(Dynamic Ground)” 전용 레이어 마스크를 선택적으로 사용하는 물리 에이전트입니다.
    /// </summary>
    /// <remarks>
    /// 움직이는 플랫폼/리프트 등 동적 표면을 분리해 필터링하려는 경우 사용합니다.
    /// </remarks>
    public interface IDynamicGroundLayerMaskPhysicsAgent : IBasePhysicsAgent
    {
        /// <summary>
        /// 동적 지면 레이어 마스크 사용 여부입니다.
        /// </summary>
        /// <remarks>
        /// <c>false</c>면 <see cref="DynamicGroundLayerMask"/>는 무시하는 구현을 권장합니다.
        /// </remarks>
        //! false인 경우, 구현체에서 DynamicGroundLayerMask를 참조하지 않아야 합니다.
        bool UseDynamicGroundLayerMask { get; set; }

        /// <summary>
        /// 동적 지면으로 취급할 레이어 묶음입니다.
        /// </summary>
        /// <remarks>
        /// <see cref="UseDynamicGroundLayerMask"/>가 <c>true</c>일 때만 적용하는 것을 권장합니다.
        /// </remarks>
        //. 움직이는 플랫폼 등 동적 표면 전용 필터
        LayerMask DynamicGroundLayerMask { get; set; }
    }



    /// <summary>
    /// “밀 수 있는(Rigidbody) 오브젝트” 전용 레이어 마스크를 선택적으로 사용하는 물리 에이전트입니다.
    /// </summary>
    /// <remarks>
    /// 상호작용 가능한 물리 오브젝트를 별도 레이어로 관리하려는 경우에 사용합니다.
    /// </remarks>
    public interface IPushableRigidbodyLayerMaskPhysicsAgent : IBasePhysicsAgent
    {
        /// <summary>
        /// 밀 수 있는 리지드바디 레이어 마스크 사용 여부입니다.
        /// </summary>
        /// <remarks>
        /// <c>false</c>면 <see cref="PushableRigidbodyLayerMask"/>는 무시하는 구현을 권장합니다.
        /// </remarks>
        //! false인 경우, 구현체에서 PushableRigidbodyLayerMask를 참조하지 않아야 합니다.
        bool UsePushableRigidbodyLayerMask { get; set; }

        /// <summary>
        /// 밀 수 있는 리지드바디로 취급할 레이어 묶음입니다.
        /// </summary>
        /// <remarks>
        /// 캐릭터의 밀기/당기기, 충돌 반응 등 상호작용 판정에 사용합니다.
        /// </remarks>
        //. 상호작용 가능한 Rigidbody 전용 필터
        LayerMask PushableRigidbodyLayerMask { get; set; }
    }



    //? 플랫포머



    /// <summary>
    /// 지면 오르기 스텝 보정 (계단 등) 높이(<see cref="float"/>)를 노출하는 물리 에이전트 
    /// </summary>
    public interface IStepUpDistancePhysicsAgent : IBasePhysicsAgent
    {
        /// <summary>
        /// 지면 오르기 스텝 보정 (계단 등) 높이
        /// </summary>
        float StepUpDistance { get; set; }
    }



    /// <summary>
    /// 지면 내려가기 스텝 보정 (계단 등) 높이(<see cref="float"/>)를 노출하는 물리 에이전트 
    /// </summary>
    public interface IStepDownDistancePhysicsAgent : IBasePhysicsAgent
    {
        /// <summary>
        /// 지면 내려가기 스텝 보정 (계단 등) 높이
        /// </summary>
        float StepDownDistance { get; set; }
    }



    //? 기준 방향



    public interface IStandardTopPositionPhysicsAgent : IBasePhysicsAgent
    {
        /// <summary>
        /// 물리 오브젝트의 상단 기준 방향
        /// </summary>
        Vector3 StandardTopDirection { get; set; }
    }



    public interface IStandardFrontPositionPhysicsAgent : IBasePhysicsAgent
    {
        /// <summary>
        /// 물리 오브젝트의 전방 기준 방향
        /// </summary>
        Vector3 StandardFrontDirection { get; set; }
    }



    ///======================================================================================================================================================



    //? 크기



    ///<summary>
    /// 물리 오브젝트의 Body 크기(<see cref="Vector2"/>)를 노출하는 물리 에이전트
    /// </summary>
    public interface IBodySizeVector2PhysicsAgent : IBasePhysicsAgent
    {
        /// <summary>
        /// 물리 오브젝트 Body (<see cref="Vector2"/>)
        /// </summary>
        Vector2 BodySizeVector2 { get; set; }
    }



    ///<summary>
    /// 물리 오브젝트의 Body 크기(<see cref="Vector3"/>)를 노출하는 물리 에이전트
    /// </summary>
    public interface IBodySizeVector3PhysicsAgent : IBodySizeVector2PhysicsAgent
    {
        /// <summary>
        /// 물리 오브젝트 Body (<see cref="Vector3"/>)
        /// </summary>
        Vector3 BodySizeVector3 { get; set; }
    }



    ///<summary>
    /// 물리 오브젝트의 Body 지름(<see cref="float"/>)를 노출하는 물리 에이전트
    /// </summary>
    public interface IBodySizeRadiusPhysicsAgent : IBasePhysicsAgent
    {
        /// <summary>
        /// 물리 오브젝트 Body (<see cref="float"/>)
        /// </summary>
        float BodySizeRadius { get; set; }
    }



    ///======================================================================================================================================================



    //? 물리 시뮬레이션 업데이트



    public interface IPhysicsUpdatePreSimulationPhysicsAgent
    {
        event Action<UnityEngine.Object> OnPreSimulationObjectEvent;
    }



    public interface IPhysicsUpdatePreSimulationPhysicsAgent<T> : IPhysicsUpdatePreSimulationPhysicsAgent where T : UnityEngine.Object
    {
        event Action<T> OnPreSimulationEvent;
    }



    public interface IPhysicsUpdatePostSimulationPhysicsAgent
    {
        event Action<UnityEngine.Object> OnPostSimulationObjectEvent;
    }



    public interface IPhysicsUpdatePostSimulationPhysicsAgent<T> : IPhysicsUpdatePostSimulationPhysicsAgent where T : UnityEngine.Object
    {
        event Action<T> OnPostSimulationEvent;
    }



    ///======================================================================================================================================================
}