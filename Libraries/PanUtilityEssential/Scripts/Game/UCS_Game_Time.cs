using Sirenix.OdinInspector;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;



//? [Game] 시간 관련을 정리한 정도의 코드



namespace Pan.Util.Game
{
    ///======================================================================================================================================================



    ///<summary>
    /// TimeScale를 관리하는 시간 채널
    /// </summary>
    [Serializable]
    public class TimeChannel
    {
        [BoxGroup("타임 채널", false)]
        [LabelText("타임 스케일")]
        [ShowInInspector]
#if UNITY_EDITOR
        [GUIColor(nameof(editorGUIColor))]
#endif
        public float TimeScale
        {
            get => timeScale;
            set
            {
                float prev = timeScale;

                if (pause)
                {
                    lastTimeScale = Mathf.Max(0, value);
                }

                timeScale = Mathf.Max(0, value);

                //! 값이 바뀌었을 때만 이벤트 호출
                if (!Mathf.Approximately(prev, timeScale))
                {
                    OnTimeScaleChangeEvent?.Invoke(timeScale);
                }
            }
        }
        private float timeScale = 1f;



        public Func<float> TimeScaling
        {
            get
            {
                timeScaling ??= () => TimeScale;
                return timeScaling;
            }
        }
        private Func<float> timeScaling;



        public Func<float> DeltaTimeFunc
        {
            get
            {
                deltaTimeFunc ??= () => Time.unscaledDeltaTime * TimeScale;
                return deltaTimeFunc;
            }
        }
        private Func<float> deltaTimeFunc;



        [BoxGroup("타임 채널", false)]
        [LabelText("일시정지")]
        [ShowInInspector]
        public bool Pause
        {
            get => pause;
            set
            {
                if (pause == value) { return; }

                //. 일시정지 작동
                if (value)
                {
                    lastTimeScale = TimeScale;
                    TimeScale = 0;
                }
                //. 일시정지 해제
                else
                {
                    TimeScale = lastTimeScale;
                }

                pause = value;

                //. 값이 바뀌었을 때 이벤트 호출
                OnPauseChangedEvent?.Invoke(pause);
            }
        }
        private bool pause;



        [BoxGroup("타임 채널", false)]
        [LabelText("재개시 적용할 타임스케일"), LabelWidth(200)]
        [ShowInInspector, ReadOnly, DisplayAsString, ShowIf(nameof(pause))]
        [PropertyOrder(99)]
        private float lastTimeScale;



        public event Action<float> OnTimeScaleChangeEvent;
        /// <summary>
        /// 일시 정지 시 호출 (true = 일시정지, false = 재개)
        /// </summary>
        public event Action<bool> OnPauseChangedEvent;



#if UNITY_EDITOR
        private Color editorGUIColor => pause ? Color.red : Color.white;
#endif
    }



    ///======================================================================================================================================================



    /// <summary>
    /// 시간 스케일 값을 반환하는 함수를 설정하기 위한 인터페이스입니다.
    /// 외부에서 시간 스케일 값을 가져오는 함수를 지정할 수 있습니다.
    /// </summary>
    public interface ITimeScaling
    {
        /// <summary>
        /// 시간 스케일 값을 반환하는 함수를 설정합니다.
        /// 이 프로퍼티의 setter를 통해 시간 스케일을 반환하는 델리게이트를 지정할 수 있습니다.<br/>
        /// <para>OnDisable, OnDestry 등에서 제거 권장</para>
        /// </summary>
        Func<float> TimeScaling { get; set; }



        /// <summary>
        /// 현재 시간 스케일 값을 반환합니다.<br/>
        /// <paramref name="defaultTimeScale"/> 가 지정되면, <see cref="TimeScaling"/> 가 null일 경우 해당 값을 반환합니다.<br/>
        /// 아무 값도 지정되지 않았다면, Unity의 <see cref="Time.timeScale"/> 값을 반환합니다.
        /// </summary>
        /// <param name="defaultTimeScale"><see cref="TimeScaling"/>이 없을때, 기본으로 사용할 시간 스케일 값. (기본값은 <see cref="Time.timeScale"/>)</param>
        /// <returns>현재 시간 스케일 값.</returns>
        public float GetTimeScale(float? defaultTimeScale = null)
            => (TimeScaling != null) ? TimeScaling() : (defaultTimeScale ?? Time.timeScale);



        /// <summary>
        /// 현재 델타 타임 값을 반환합니다.<br/>
        /// <see cref="GetTimeScale(float?)"/>과 곱해 반환되며, <paramref name="defaultDeltaTime"/> 가 지정되지않으면, Unity의 <see cref="Time.deltaTime"/>을 사용합니다.
        /// </summary>
        /// <param name="defaultDeltaTime">null이면 Unity의 <see cref="Time.deltaTime"/> 반환.</param>
        /// <param name="defaultTimeScale"><see cref="TimeScaling"/>이 없을때, 기본으로 사용할 시간 스케일 값. (기본값은 <see cref="Time.timeScale"/>)</param>
        /// <returns>현재 델타 타임 값.</returns>
        public float GetDeltaTime(float? defaultDeltaTime = null, float? defaultTimeScale = null)
            => (defaultDeltaTime ?? Time.deltaTime) * GetTimeScale(defaultTimeScale);



        /// <summary>
        /// 현재 고정 델타 타임 값을 반환합니다.<br/>
        /// <see cref="GetTimeScale(float?)"/>과 곱해 반환되며, <paramref name="defaultFixedDeltaTime"/> 가 지정되지않으면, Unity의 <see cref="Time.fixedDeltaTime"/>을 사용합니다.
        /// </summary>
        /// <param name="defaultFixedDeltaTime">null이면 Unity의 <see cref="Time.fixedDeltaTime"/> 반환.</param>
        /// <param name="defaultTimeScale"><see cref="TimeScaling"/>이 없을때, 기본으로 사용할 시간 스케일 값. (기본값은 <see cref="Time.timeScale"/>)</param>
        /// <returns>현재 고정 델타 타임 값.</returns>
        public float GetFixedDeltaTime(float? defaultFixedDeltaTime = null, float? defaultTimeScale = null)
            => (defaultFixedDeltaTime ?? Time.fixedDeltaTime) * GetTimeScale(defaultTimeScale);
    }



    ///======================================================================================================================================================



    public static partial class GameTimeExtensions
    {
        /// <summary>
        /// 현재 시간 스케일 값을 반환합니다.
        /// </summary>
        /// <param name="self">시간 스케일 기능을 가진 객체.</param>
        /// <param name="defaultTimeScale">TimeScaleFunc이 null일 경우 사용할 기본값. null이면 Unity의 <see cref="Time.timeScale"/> 반환.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetTimeScale(this ITimeScaling self, float? defaultTimeScale = null)
            => self.GetTimeScale(defaultTimeScale);

        /// <summary>
        /// 현재 델타 타임 값을 반환합니다.
        /// </summary>
        /// <param name="self">델타 타임 기능을 가진 객체.</param>
        /// <param name="defaultDeltaTime">null이면 Unity의 <see cref="Time.deltaTime"/> 반환.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetDeltaTime(this ITimeScaling self, float? defaultDeltaTime = null)
            => self.GetDeltaTime(defaultDeltaTime);

        /// <summary>
        /// 현재 고정 델타 타임 값을 반환합니다.
        /// </summary>
        /// <param name="self">고정 델타 타임 기능을 가진 객체.</param>
        /// <param name="defaultFixedDeltaTime">null이면 Unity의 <see cref="Time.fixedDeltaTime"/> 반환.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetFixedDeltaTime(this ITimeScaling self, float? defaultFixedDeltaTime = null)
            => self.GetFixedDeltaTime(defaultFixedDeltaTime);
    }



    ///======================================================================================================================================================
}