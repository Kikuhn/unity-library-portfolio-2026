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



//? [Physics] 중력 관련을 정리한 정도의 코드



namespace Pan.Util.Game
{
    ///======================================================================================================================================================



    ///<summary>
    /// <see cref="GravitySystem"/> 보유
    /// </summary>
    public interface IHoldGravitySystem
    {
        GravitySystem GravitySystem { get; }
    }



    /// <summary>
    /// 통합 중력 시스템 (모드 없이 "값"만으로 현실~플랫포머까지 연속 튜닝)
    /// - 이번 스텝에 적용할 "중력에 의한 Δv(속도 변화 벡터)"를 돌려줍니다.
    /// </summary>
    [Serializable]
    public class GravitySystem : ICopyable<GravitySystem>, IHoldGravitySystem
    {
        ///======================================================================================================================================================



        //. 런타임 상태 (내부 가속 배율 상태)



        [FoldoutGroup("중력 시스템"), ShowInInspector, Sirenix.OdinInspector.ReadOnly, EnableGUI, DisplayAsString, LabelText("중력배율(현재) : G")]
        [BoxGroup("중력 시스템/박스", false)]
        [PropertyOrder(0)]
        private float _gravitingAccel_G = 1f;

        [FoldoutGroup("중력 시스템"), ShowInInspector, Sirenix.OdinInspector.ReadOnly, EnableGUI, DisplayAsString, LabelText("중첩배율(현재) : A")]
        [BoxGroup("중력 시스템/박스", false)]
        [PropertyOrder(0)]
        private float _accelOverlapping_A = 1f;



        ///======================================================================================================================================================



        [FoldoutGroup("중력 시스템"), LabelText("중력 사용")]
        [PropertyOrder(1)]
        public bool UseGravity = true;



        [SerializeField, HideInInspector]
        private float gravityValue = 9.81f;
        [FoldoutGroup("중력 시스템"), LabelText("기본 g (유닛/초²)"), MinValue(0f)]
        [ShowInInspector]
        [PropertyOrder(2)]
        public float GravityValue
        {
            get => gravityValue;
            set => gravityValue = Mathf.Max(0f, value);
        }



        [FoldoutGroup("중력 시스템"), LabelText("중력 방향")]
        [PropertyOrder(3)]
        public Vector3 GravityDirection = Vector3.down;



        [SerializeField, HideInInspector]
        private float terminalSpeed = 0f;
        [FoldoutGroup("중력 시스템"), LabelText("종단속도 |v↓| (유닛/초)"), MinValue(0f)]
        [ShowInInspector]
        [PropertyTooltip("0이면 미사용. 아래 방향 속도 성분만 제한(WithTerminal 계열 메서드에서만 사용).")]
        [PropertyOrder(4)]
        public float TerminalSpeed
        {
            get => terminalSpeed;
            set => terminalSpeed = Mathf.Max(0f, value);
        }



        ///======================================================================================================================================================



        [FoldoutGroup("중력 시스템"), BoxGroup("중력 시스템/중력 가속"), LabelText("중력 가속 사용")]
        [PropertyOrder(5)]
        public bool UseGravityAcceleration = false;



        [SerializeField, HideInInspector]
        private float gravityAccelValue = 0f; //. β
        [FoldoutGroup("중력 시스템"), BoxGroup("중력 시스템/중력 가속"), EnableIf(nameof(UseGravityAcceleration)), Indent, LabelText("β : 가속 증가계수 (1/s)"), MinValue(0f)]
        [ShowInInspector]
        [PropertyTooltip("중력 배율 G의 증가 속도. 0이면 배율이 늘지 않음(현실적인 등가속도).")]
        [PropertyOrder(6)]
        public float GravityAccelValue
        {
            get => gravityAccelValue;
            set => gravityAccelValue = Mathf.Max(0f, value);
        }



        [SerializeField, HideInInspector]
        private float accelOverlapValue = 0f; //. α
        [FoldoutGroup("중력 시스템"), BoxGroup("중력 시스템/중력 가속"), EnableIf(nameof(UseGravityAcceleration)), Indent, LabelText("α : 중첩 증가속도 (1/s)"), MinValue(0f)]
        [ShowInInspector]
        [PropertyTooltip("중력 배율 중첩 A의 증가 속도. 0이면 G가 선형 증가, 양수면 이차적으로 더 가파르게 증가.")]
        [PropertyOrder(7)]
        public float AccelOverlapValue
        {
            get => accelOverlapValue;
            set => accelOverlapValue = Mathf.Max(0f, value);
        }



        [SerializeField, HideInInspector]
        private float gravityAccelMaxValue = 1f; //. Gmax
        [FoldoutGroup("중력 시스템"), BoxGroup("중력 시스템/중력 가속"), EnableIf(nameof(UseGravityAcceleration)), Indent, LabelText("Gmax : 배율 상한"), MinValue(1f)]
        [ShowInInspector]
        [PropertyTooltip("중력 배율 G의 최대값. 최소 1 이상이어야 안전.")]
        [PropertyOrder(8)]
        public float GravityAccelMaxValue
        {
            get => gravityAccelMaxValue;
            set => gravityAccelMaxValue = Mathf.Max(1f, value);
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 현재 설정과 내부 가속 상태(G, A)를 반영한 "중력 가속도 벡터 a"를 반환합니다. (dt 미사용)
        /// </summary>
        /// <param name="advanceAccel">
        /// <c>true</c>면 내부 가속 상태(G,A)를 이 스텝 이후로 진행합니다(아래의 dt 필요).
        /// <c>false</c>면 진행하지 않고, 계측용으로만 사용합니다.
        /// </param>
        /// <param name="deltaTime">
        /// advanceAccel이 <c>true</c>일 때 가속 누적에 사용할 dt(초). advanceAccel=false면 무시됩니다.
        /// </param>
        public Vector3 GetAccelerationVector(bool advanceAccel, float deltaTime)
        {
            if (!UseGravity) return Vector3.zero;

            //. 방향 정규화
            Vector3 down = GravityDirection.sqrMagnitude > 0f ? GravityDirection.normalized : Vector3.down;

            //. 기본 g 스칼라
            float g = GravityValue;

            //. 가속 배율 모델 적용
            if (UseGravityAcceleration)
            {
                //? 이번 프레임: 현재 G를 곱해 적용
                g *= _gravitingAccel_G;

                //? 다음 프레임을 위한 누적(요청 시)
                if (advanceAccel && deltaTime > 0f)
                {
                    AddGravityAccel(deltaTime);
                }
            }
            else
            {
                //. 현실형: 항상 G=1 유지
                _gravitingAccel_G = 1f;
                _accelOverlapping_A = 1f;
            }

            return down * g;
        }



        /// <summary>
        /// 이번 스텝에 적용할 "Δv(속도 변화 벡터) = a * dt" 를 반환합니다.
        /// </summary>
        /// <param name="deltaTime">스텝 dt(초)</param>
        /// <param name="advanceAccel">가속 상태를 이 스텝 종료 시점으로 진행할지 여부</param>
        public Vector3 GetDeltaVelocity(float deltaTime, bool advanceAccel = true)
        {
            if (deltaTime <= 0f || !UseGravity) return Vector3.zero;
            //. a(t) * dt
            return GetAccelerationVector(advanceAccel, deltaTime) * deltaTime;
        }



        /// <summary>
        /// 현재 속도를 함께 전달하면, "아래 방향 종단속도"를 고려해 클램프된 Δv를 반환합니다.
        /// </summary>
        /// <param name="currentVelocity">현재 속도</param>
        /// <param name="deltaTime">스텝 dt(초)</param>
        /// <param name="advanceAccel">가속 상태 진행 여부</param>
        public Vector3 GetDeltaVelocityWithTerminal(Vector3 currentVelocity, float deltaTime, bool advanceAccel = true)
        {
            if (deltaTime <= 0f || !UseGravity) return Vector3.zero;

            Vector3 down = GravityDirection.sqrMagnitude > 0f ? GravityDirection.normalized : Vector3.down;

            //. 기본 Δv
            Vector3 dv = GetAccelerationVector(advanceAccel, deltaTime) * deltaTime;

            //. 종단속도 적용(옵션): 아래 성분만 제한
            if (TerminalSpeed > 0f)
            {
                Vector3 vNew = currentVelocity + dv;

                float vDownNew = Vector3.Dot(vNew, down); //. 아래(+)
                if (vDownNew > TerminalSpeed)
                {
                    //. 아래 성분을 vt로 맞추도록 dv를 보정
                    float vDownCur = Vector3.Dot(currentVelocity, down);
                    float needDown = TerminalSpeed - vDownCur; //. 목표까지 남은 아래 성분
                    Vector3 dvDown = down * needDown;
                    Vector3 dvLat = dv - Vector3.Project(dv, down);
                    dv = dvLat + dvDown;

                    //! 여기서 needDown이 음수면 이미 vt를 초과 중 → 아래 성분 증가를 막음
                }
            }

            return dv;
        }



        /// <summary>
        /// 내부 가속 배율 상태(G, A)를 초기화합니다.
        /// </summary>
        public void ResetAcceleration()
        {
            ResetGravityAccel();
        }



        ///======================================================================================================================================================



        //. 내부: 가속 배율 누적(Platformer 계열 핵심식)

        private void AddGravityAccel(float dt)
        {
            //. A ← A + α·dt
            _accelOverlapping_A += Mathf.Max(0f, AccelOverlapValue) * dt;

            //. G ← G + β·A·dt
            _gravitingAccel_G += Mathf.Max(0f, GravityAccelValue) * _accelOverlapping_A * dt;

            //. G ← Clamp(1, Gmax)
            float gmax = Mathf.Max(1f, GravityAccelMaxValue);     //! 최소 1 보장
            _gravitingAccel_G = Mathf.Clamp(_gravitingAccel_G, 1f, gmax);
        }

        private void ResetGravityAccel()
        {
            _gravitingAccel_G = 1f;
            _accelOverlapping_A = 1f;
        }



        ///======================================================================================================================================================



        GravitySystem IHoldGravitySystem.GravitySystem => this;



        public void Copy(GravitySystem original)
        {
            //! 런타임 가속 상태(G, A)는 복사하지 않는다 (외부 상태 유지)
            //? 설정값만 복사한다

            UseGravity = original.UseGravity;

            GravityValue = original.GravityValue;          //. 프로퍼티 경유(가드 적용)
            GravityDirection = original.GravityDirection;

            UseGravityAcceleration = original.UseGravityAcceleration;
            GravityAccelValue = original.GravityAccelValue;  //. β
            AccelOverlapValue = original.AccelOverlapValue;  //. α
            GravityAccelMaxValue = original.GravityAccelMaxValue; // Gmax

            TerminalSpeed = original.TerminalSpeed;
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================



    #region Legacy ~250902



    ///// <summary>
    ///// 중력 설정 옵션을 제공하는 객체에 대한 인터페이스입니다.
    ///// </summary>
    //[Obsolete]
    //public interface IHaveGravitySettingLegacy
    //{
    //    /// <summary>
    //    /// 중력 설정 옵션을 가져옵니다.
    //    /// </summary>
    //    GravitySettingOptionsLegacy GravitySetting { get; }
    //}



    ///// <summary>
    ///// 중력 설정 옵션을 나타내는 클래스입니다.
    ///// <para>중력 사용 여부, 중력 값, 방향, 가속도 관련 옵션 등을 포함합니다.</para>
    ///// </summary>
    //[Serializable]
    //[Obsolete]
    //public class GravitySettingOptionsLegacy : ICopyable<GravitySettingOptionsLegacy>, IHaveGravitySettingLegacy
    //{
    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// 중력 사용 여부
    //    /// </summary>
    //    public bool UseGravity;



    //    /// <summary>
    //    /// 중력 값
    //    /// </summary>
    //    public CValueFloat GravityValue = new CValueFloat();



    //    /// <summary>
    //    /// 중력 방향
    //    /// </summary>
    //    public Vector2 GravityDir;



    //    /// <summary>
    //    /// 중력 가속도 값
    //    /// </summary>
    //    public float GravityAccelValue;



    //    /// <summary>
    //    /// 중력 가속도 최대값
    //    /// </summary>
    //    public float GravityAccelMaxValue;



    //    /// <summary>
    //    /// 중력 가속도 중첩 배율에 더해지는 값
    //    /// </summary>
    //    public float AccelOverlapValue;



    //    ///======================================================================================================================================================



    //    GravitySettingOptionsLegacy IHaveGravitySettingLegacy.GravitySetting => this;



    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// 지정된 GravitySettingOptions의 값을 현재 인스턴스로 복사합니다.
    //    /// </summary>
    //    /// <param name="settings">복사할 GravitySettingOptions 인스턴스</param>
    //    public void Copy(GravitySettingOptionsLegacy settings)
    //    {
    //        if (settings == this) { return; }

    //        UseGravity = settings.UseGravity;
    //        GravityValue.Copy(settings.GravityValue);
    //        GravityDir = settings.GravityDir;
    //        GravityAccelValue = settings.GravityAccelValue;
    //        GravityAccelMaxValue = settings.GravityAccelMaxValue;
    //        AccelOverlapValue = settings.AccelOverlapValue;
    //    }



    //    /// <summary>
    //    /// 중력 설정 옵션을 초기 상태로 리셋합니다.
    //    /// </summary>
    //    public void Reset()
    //    {
    //        UseGravity = false;
    //        GravityValue.Reset();
    //        GravityDir = Vector2.zero;
    //        GravityAccelValue = 0;
    //        GravityAccelMaxValue = 0;
    //        AccelOverlapValue = 0;
    //    }



    //    ///======================================================================================================================================================
    //}



    /////======================================================================================================================================================



    ///// <summary>
    ///// 중력 시스템을 관리하는 클래스입니다.
    ///// </summary>
    //[Obsolete]
    //public class GravitySystemLegacy
    //{
    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// GravitySystem 생성자
    //    /// </summary>
    //    /// <param name="setting">중력 설정 옵션</param>
    //    public GravitySystemLegacy(GravitySettingOptionsLegacy setting)
    //    {
    //        Setting = setting;
    //    }



    //    ///======================================================================================================================================================



    //    private readonly GravitySettingOptionsLegacy Setting;



    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// 중력 사용 여부 변경 시 호출할 알람 액션입니다.
    //    /// </summary>
    //    public Action<bool> Alarm_UseGravity = null;



    //    /// <summary>
    //    /// 중력 사용 여부를 대신 결정하는 함수입니다.
    //    /// </summary>
    //    public Func<bool, bool> Instead_UseGravity = null;



    //    /// <summary>
    //    /// 중력 사용 여부를 전환하는 함수입니다.
    //    /// </summary>
    //    public Func<bool, bool> Switch_UseGravity = null;



    //    /// <summary>
    //    /// 중력 사용 여부를 가져오거나 설정합니다.
    //    /// </summary>
    //    public bool UseGravity
    //    {
    //        get => Setting.UseGravity;
    //        set
    //        {
    //            Setting.UseGravity = value;
    //        }
    //    }



    //    ///======================================================================================================================================================
    //}




    ///// <summary>
    ///// 플랫폼 형식의 게임에서 중력 가속 및 적용을 관리하는 시스템입니다.
    ///// </summary>
    //[Obsolete]
    //public class PlatformerGravitySystemLegacy
    //{
    //    /// <summary>
    //    /// PlatformerGravitySystem 생성자
    //    /// </summary>
    //    /// <param name="settings">중력 설정 옵션</param>
    //    public PlatformerGravitySystemLegacy(GravitySettingOptionsLegacy settings)
    //    {
    //        Acceler = new Accel(settings);
    //    }



    //    /// <summary>
    //    /// 중력 가속도 처리를 위한 내부 클래스
    //    /// </summary>
    //    [Obsolete]
    //    public class Accel
    //    {
    //        /// <summary>
    //        /// Accel 생성자
    //        /// </summary>
    //        /// <param name="settings">중력 설정 옵션</param>
    //        public Accel(GravitySettingOptionsLegacy settings)
    //        {
    //            Settings = settings;
    //        }

    //        private readonly GravitySettingOptionsLegacy Settings;

    //        /// <summary>
    //        /// 현재 적용 중인 중력 가속도 (배율값, 초기값은 1)
    //        /// </summary>
    //        public float GravitingAccel { get; private set; } = 1;

    //        /// <summary>
    //        /// 현재 적용 중인 중력 가속 중첩 배율 (배율값, 초기값은 1)
    //        /// </summary>
    //        public float AccelOverlapping => accelOverlapping;
    //        [SerializeField] private float accelOverlapping = 1f;

    //        /// <summary>
    //        /// 중력 가속도 값을 증가시킵니다.
    //        /// <para>
    //        /// 중첩 배율에 중첩 배율 추가값과 델타 타임을 곱한 값을 더한 후,
    //        /// 중력 가속도 값에 중력 가속도 값과 중첩 배율 및 델타 타임을 곱한 값을 더합니다.
    //        /// 이후, 최대값으로 제한합니다.
    //        /// </para>
    //        /// </summary>
    //        /// <param name="deltaTime">델타 타임</param>
    //        public void AddGravityAccel(float deltaTime)
    //        {
    //            //! 적용중인 중력가속 중첩 배율에, 중첩 배율 추가값과 DeltaTime을 곱해서 더하기

    //            accelOverlapping += Settings.AccelOverlapValue * deltaTime;

    //            //? ( 중력 가속 중첩배율 += 중첩배율 추가값 x 델타타임 )

    //            //! 적용중인 중력가속도 값에, 중력 가속도 값과 중첩배율과 DeltaTime을 곱해서 더하기

    //            GravitingAccel += Settings.GravityAccelValue * accelOverlapping * deltaTime;

    //            //? ( 적용중인 중력 가속도 += 중력 가속도값 x 중력 가속중첩배율 x 델타타임 )

    //            // 최대 크기 제한
    //            GravitingAccel = Mathf.Clamp(GravitingAccel, 1f, Settings.GravityAccelMaxValue);
    //        }

    //        /// <summary>
    //        /// 중력 가속도 값을 초기 상태(1)로 리셋합니다.
    //        /// </summary>
    //        public void ResetGravityAccel()
    //        {
    //            GravitingAccel = 1f;
    //            accelOverlapping = 1f;
    //        }
    //    }



    //    /// <summary>
    //    /// 중력 가속도를 관리하는 Accel 인스턴스입니다.
    //    /// </summary>
    //    public readonly Accel Acceler;



    //    /// <summary>
    //    /// 중력 값에 중력 가속도를 적용합니다.
    //    /// </summary>
    //    /// <param name="gravityValue">적용할 중력 값 (ref)</param>
    //    /// <param name="deltaTime">델타 타임</param>
    //    /// <param name="addGravityAccel">
    //    /// 연산 후 중력 가속도 값을 증가시킬지 여부 (기본값: true)
    //    /// </param>
    //    public void ApplyGravityAccel(ref float gravityValue, float deltaTime, bool addGravityAccel = true)
    //    {
    //        // 중력 값에 중력 가속도 곱해서 적용
    //        gravityValue *= Acceler.GravitingAccel;

    //        // 중력 가속도 값 증가
    //        if (addGravityAccel) { Acceler.AddGravityAccel(deltaTime); }
    //    }



    //    /// <summary>
    //    /// 중력 가속도를 초기화합니다.
    //    /// </summary>
    //    public void Refresh()
    //    {
    //        Acceler.ResetGravityAccel();
    //    }
    //}


    #endregion



    ///======================================================================================================================================================
}
