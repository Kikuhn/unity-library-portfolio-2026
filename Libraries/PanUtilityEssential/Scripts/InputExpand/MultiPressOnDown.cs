// SPDX-License-Identifier: MIT
// Unity 6 / Input System (com.unity.inputsystem)
#nullable enable

using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

namespace Pan.InputSystemExtensions
{
    public class MultiPressOnDown : IInputInteraction
    {
        public int tapCount;
        public float maxTapSpacing;
        public float maxTapDuration;
        public float pressPoint;

        public bool useDefaultTapSpacing = true;
        public bool useDefaultTapDuration = true;
        public bool useDefaultPressPoint = true;

        int _count; bool _wasPressed; bool _releasedBetween;
        double _pressStartTime, _lastReleaseTime;
        InputControl _sourceControl;

        // 데드라인(엄격 검증)
        double _durationDeadline;
        double _spacingDeadline;

        public void Process(ref InputInteractionContext ctx)
        {
            int required = tapCount > 0 ? tapCount : 2;
            float spacing = useDefaultTapSpacing ? GetDefaultTapSpacing() : Mathf.Max(0f, maxTapSpacing);
            float duration = useDefaultTapDuration ? GetDefaultTapDuration() : Mathf.Max(0f, maxTapDuration);
            float pressP = useDefaultPressPoint ? GetDefaultPressPoint() : (pressPoint >= 0f ? pressPoint : GetDefaultPressPoint());

            bool isPressed = ctx.ControlIsActuated(pressP);

            if (ctx.timerHasExpired) { ctx.Canceled(); Reset(); return; }

            // 엄격 검증: 입력이 늦게 와도 컷
            if (_count > 0 && _wasPressed && isPressed && ctx.time > _durationDeadline) { ctx.Canceled(); Reset(); return; }
            if (_count > 0 && !_wasPressed && isPressed && _releasedBetween && ctx.time > _spacingDeadline) { ctx.Canceled(); Reset(); return; }

            // Down edge
            if (isPressed && !_wasPressed)
            {
                if (_count == 0)
                {
                    _count = 1;
                    _releasedBetween = false;
                    _pressStartTime = ctx.time;
                    _sourceControl = ctx.control;

                    ctx.Started();
                    _durationDeadline = ctx.time + duration;
                    ctx.SetTimeout(duration);

                    if (required == 1) { ctx.Performed(); Reset(); return; }
                }
                else
                {
                    if (_sourceControl != null && ctx.control != _sourceControl) { ctx.Canceled(); Reset(); return; }

                    double sinceLastRelease = ctx.time - _lastReleaseTime;

                    if (_releasedBetween && sinceLastRelease <= spacing && ctx.time <= _spacingDeadline)
                    {
                        _count++;
                        if (_count >= required) { ctx.Performed(); Reset(); return; }

                        _releasedBetween = false;
                        _pressStartTime = ctx.time;
                        _durationDeadline = ctx.time + duration;
                        ctx.SetTimeout(duration);
                    }
                    else { ctx.Canceled(); Reset(); return; }
                }
            }
            // Up edge
            else if (!isPressed && _wasPressed)
            {
                double held = ctx.time - _pressStartTime;

                if (held <= duration)
                {
                    _releasedBetween = true;
                    _lastReleaseTime = ctx.time;
                    _spacingDeadline = ctx.time + spacing;
                    ctx.SetTimeout(spacing);
                }
                else { ctx.Canceled(); Reset(); return; }
            }

            _wasPressed = isPressed;
        }

        public void Reset()
        {
            _count = 0; _wasPressed = false; _releasedBetween = false;
            _pressStartTime = 0; _lastReleaseTime = 0; _sourceControl = null;
            _durationDeadline = 0; _spacingDeadline = 0;
        }

        static float GetDefaultTapSpacing()
        {
            var s = InputSystem.settings; if (s == null) return 0.75f;
            var t = s.GetType();
            foreach (var n in new[] { "multiTapDelayTime", "defaultTapSpacing", "multiTapTime", "tapSpacing", "defaultMultiTapDelay" })
            {
                var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.Instance); if (p?.PropertyType == typeof(float)) return (float)p.GetValue(s);
                var f = t.GetField(n, BindingFlags.Public | BindingFlags.Instance); if (f?.FieldType == typeof(float)) return (float)f.GetValue(s);
            }
            return 2f * GetDefaultTapDuration();
        }
        static float GetDefaultTapDuration()
        {
            var s = InputSystem.settings; if (s == null) return 0.2f;
            var t = s.GetType();
            foreach (var n in new[] { "defaultTapTime", "tapTime", "defaultTapDuration" })
            {
                var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.Instance); if (p?.PropertyType == typeof(float)) return (float)p.GetValue(s);
                var f = t.GetField(n, BindingFlags.Public | BindingFlags.Instance); if (f?.FieldType == typeof(float)) return (float)f.GetValue(s);
            }
            return 0.2f;
        }
        static float GetDefaultPressPoint() => InputSystem.settings?.defaultButtonPressPoint ?? 0.5f;
    }

    public static class MultiPressOnDown_Registrar
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            InputSystem.RegisterInteraction<MultiPressOnDown>("MultiPressOnDown");
            InputSystem.RegisterInteraction<MultiPressOnDown>("MultiTapOnDown");
        }
    }
}
