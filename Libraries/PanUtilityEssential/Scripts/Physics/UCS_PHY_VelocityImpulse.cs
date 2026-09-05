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
using Sirenix.OdinInspector;



//? [Physics] 감쇠형 임펄스 관련을 정리한 정도의 코드



namespace Pan.Util.Game
{
    ///======================================================================================================================================================



    /// <summary>
    /// <see cref="VelocityImpulseBase"/> 보유
    /// </summary>
    public interface IHoldVelocityImpulse
    {
        /// <summary>
        /// 감쇠형 임펄스(이동 Δv) 소스 
        /// </summary>
        VelocityImpulseBase VelocityImpulse { get; }
    }



    /// <summary>
    /// <see cref="VectorImpulse"/> 보유
    /// </summary>
    public interface IHoldVectorImpulseImpulse : IHoldVelocityImpulse
    {
        /// <summary>
        /// 벡터형 임펄스 소스 
        /// </summary>
        VectorImpulse VectorImpulse { get; }
    }



    /// <summary>
    /// <see cref="DirectionalImpulse"/> 보유
    /// </summary>
    public interface IHoldDirectionalImpulseImpulse : IHoldVelocityImpulse
    {
        /// <summary>
        /// 방향형 임펄스 소스 
        /// </summary>
        DirectionalImpulse DirectionalImpulse { get; }
    }



    /// <summary>
    /// 감쇠형 임펄스(이동 Δv) 소스의 공통 베이스.
    /// - 매 스텝마다 저장치에 감쇠(damping)를 적용하고, "이번 스텝에 실제로 소모된 양"만 Δv로 반환합니다.
    /// - 감쇠식: new = old * (1 / (1 + DampingPerSecond * dt)),  emitted = old - new  → Σemitted = 초기 저장량.
    /// </summary>
    [Serializable]
    public abstract class VelocityImpulseBase : IHoldVelocityImpulse
    {
        //. 파생 클래스가 자신의 FoldoutGroup 이름을 정합니다.
        protected virtual string GroupName => "임펄스(공통)";

        [FoldoutGroup("$GroupName"), PropertyOrder(0)]
        [LabelText("사용")]
        public bool Enabled = true;

        [SerializeField, HideInInspector] private float _dampingPerSecond = 10f;
        /// <summary>감쇠 계수 (1/s). 클수록 더 빨리 소모됩니다.</summary>
        [FoldoutGroup("$GroupName"), PropertyOrder(1)]
        [LabelText("감쇠 (1/s)"), MinValue(0f)]
        [ShowInInspector]
        public float DampingPerSecond
        {
            get => _dampingPerSecond;
            set => _dampingPerSecond = Mathf.Max(0f, value);
        }

        [SerializeField, HideInInspector] private float _zeroSnapThreshold = 1e-4f;
        /// <summary>아주 작은 값은 0으로 스냅하는 임계값.</summary>
        [FoldoutGroup("$GroupName"), PropertyOrder(2)]
        [LabelText("제로 스냅 임계값"), MinValue(0f)]
        [ShowInInspector]
        public float ZeroSnapThreshold
        {
            get => _zeroSnapThreshold;
            set => _zeroSnapThreshold = Mathf.Max(0f, value);
        }


        /// <summary>
        /// 이번 스텝에 방출할 Δv(속도 변화 벡터)를 계산합니다.
        /// </summary>
        /// <param name="deltaTime">Δt(초)</param>
        public abstract Vector3 GetDeltaVelocity(float deltaTime);

        //. 공통 감쇠 인자
        protected float ComputeDamp(float dt)
        {
            //. dt==0 또는 감쇠 0이면 감쇠 없음
            if (dt <= 0f || _dampingPerSecond <= 0f) return 1f;
            return 1f / (1f + _dampingPerSecond * dt);
        }

        //. 스냅 헬퍼
        protected void SnapZero(ref Vector3 v)
        {
            if (v.sqrMagnitude <= _zeroSnapThreshold * _zeroSnapThreshold) v = Vector3.zero;
        }
        protected void SnapZero(ref float f)
        {
            if (Mathf.Abs(f) <= _zeroSnapThreshold) f = 0f;
        }



        public VelocityImpulseBase VelocityImpulse => this;
    }



    /// <summary>
    /// 벡터형 임펄스 소스: 누적 벡터를 저장해 두고, 매 프레임 감쇠하면서 "소모량"만 Δv로 방출합니다.
    /// </summary>
    [Serializable]
    public sealed class VectorImpulse : VelocityImpulseBase, IHoldVectorImpulseImpulse
    {
        protected override string GroupName => "임펄스(벡터)";

        [SerializeField, HideInInspector] private Vector3 _stored = Vector3.zero;

        public Vector3 Stored => _stored; //. 읽기 전용 공개

#if UNITY_EDITOR
        [FoldoutGroup("$GroupName"), ShowInInspector, Sirenix.OdinInspector.ReadOnly, EnableGUI, DisplayAsString]
        [BoxGroup("$GroupName/박스", false)]
        [LabelText("보유 벡터 (현재)"), PropertyOrder(-100)]
        private string _dispStored => _stored.ToString("F5");
#endif

        //. API
        /// <summary>임펄스를 더합니다.</summary>
        [FoldoutGroup("$GroupName"), PropertyOrder(10)]
        public void AddImpulse(Vector3 v) => _stored += v;

        /// <summary>임펄스를 설정합니다.</summary>
        [FoldoutGroup("$GroupName"), PropertyOrder(11)]
        public void SetImpulse(Vector3 v) => _stored = v;

        /// <inheritdoc/>
        public override Vector3 GetDeltaVelocity(float deltaTime)
        {
            if (!Enabled || _stored == Vector3.zero) return Vector3.zero;

            //. 감쇠 계산
            float damp = ComputeDamp(deltaTime);
            if (damp >= 1f) return Vector3.zero; //! 감쇠 없음 → 방출 없음

            //. 감쇠 전/후 차이를 "방출량"으로 사용 → 총합=초기량
            var before = _stored;
            var after = before * damp;

            SnapZero(ref after);
            _stored = after;

            var emitted = before - after; //? 이번 스텝 실제 소모 Δv
            SnapZero(ref emitted);
            return emitted;
        }

#if UNITY_EDITOR
        [FoldoutGroup("$GroupName"), PropertyOrder(999)]
        [Button(ButtonSizes.Small), GUIColor(0.9f, 0.4f, 0.4f)]
        [LabelText("리셋(보유=0)")]
        private void ResetStored() => _stored = Vector3.zero;
#endif


        VectorImpulse IHoldVectorImpulseImpulse.VectorImpulse => this;
    }



    /// <summary>
    /// 방향형 임펄스 소스: 방향(정규화) + 크기(스칼라)를 저장해 두고, 매 프레임 감쇠하면서 "소모량"만 Δv로 방출합니다.
    /// </summary>
    [Serializable]
    public sealed class DirectionalImpulse : VelocityImpulseBase, IHoldDirectionalImpulseImpulse
    {
        protected override string GroupName => "임펄스(방향)";

        [SerializeField, HideInInspector] private float _magnitude = 0f;
        [SerializeField, HideInInspector] private Vector3 _direction = Vector3.zero; //. 항상 정규화 보관

        public float Magnitude => _magnitude; //. 읽기 전용 공개
        public Vector3 Direction => _direction; //. 읽기 전용 공개

#if UNITY_EDITOR
        [FoldoutGroup("$GroupName"), ShowInInspector, Sirenix.OdinInspector.ReadOnly, EnableGUI, DisplayAsString]
        [BoxGroup("$GroupName/박스", false)]
        [LabelText("보유 파워 × 방향 (현재)"), PropertyOrder(-100)]
        private string _dispStored => $"{_magnitude:F5} × {(_direction == Vector3.zero ? "Zero" : _direction.ToString("F5"))}";
#endif

        //. API
        /// <summary>방향을 유지한 채 크기를 더합니다.</summary>
        [FoldoutGroup("$GroupName"), PropertyOrder(10)]
        public void AddPower(float power)
        {
            _magnitude += power;
            SnapZero(ref _magnitude);
        }

        /// <summary>방향을 유지한 채 크기를 설정합니다.</summary>
        [FoldoutGroup("$GroupName"), PropertyOrder(11)]
        public void SetPower(float power)
        {
            _magnitude = power;
            SnapZero(ref _magnitude);
        }

        /// <summary>방향/크기를 함께 더합니다. (dir은 정규화 저장)</summary>
        [FoldoutGroup("$GroupName"), PropertyOrder(12)]
        public void AddDirectional(float power, Vector3 dir)
        {
            if (dir.sqrMagnitude > 0f) _direction = dir.normalized; //. 유효할 때만 방향 업데이트
            _magnitude += power;
            SnapZero(ref _magnitude);
        }

        /// <summary>방향/크기를 함께 설정합니다. (dir은 정규화 저장)</summary>
        [FoldoutGroup("$GroupName"), PropertyOrder(13)]
        public void SetDirectional(float power, Vector3 dir)
        {
            _direction = (dir.sqrMagnitude > 0f) ? dir.normalized : Vector3.zero;
            _magnitude = power;
            SnapZero(ref _magnitude);
        }

        /// <inheritdoc/>
        public override Vector3 GetDeltaVelocity(float deltaTime)
        {
            if (!Enabled || _magnitude == 0f || _direction == Vector3.zero)
            {
                //. 방향 미지정 시 방출하지 않음
                return Vector3.zero;
            }

            float damp = ComputeDamp(deltaTime);
            if (damp >= 1f) return Vector3.zero;

            var before = _magnitude;
            var after = before * damp;

            SnapZero(ref after);
            _magnitude = after;

            var emittedMag = before - after; //. 이번 스텝 실제 소모 크기
            SnapZero(ref emittedMag);

            return _direction * emittedMag;
        }

#if UNITY_EDITOR
        [FoldoutGroup("$GroupName"), PropertyOrder(999)]
        [Button(ButtonSizes.Small), GUIColor(0.9f, 0.4f, 0.4f)]
        [LabelText("리셋(보유=0)")]
        private void ResetStored()
        {
            _magnitude = 0f;
            _direction = Vector3.zero;
        }
#endif

        DirectionalImpulse IHoldDirectionalImpulseImpulse.DirectionalImpulse => this;
    }



    ///======================================================================================================================================================
}
