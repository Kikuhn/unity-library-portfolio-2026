using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;
using Pan.Util;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor.Compilation;



//? 유니티 에디터를 확장한 추상 클래스들이 정리가 되어있는 정도의 코드



namespace Pan.Util.Editors
{
    /// <summary>
    /// EditorWindow 또는 Editor에 대해 주기적으로 <c>Repaint()</c>를 호출하는 GC-Free 스케줄러입니다.
    /// <para>EditorApplication.update에 직접 연결되므로, 반드시 <see cref="Dispose"/>를 호출하여 연결 해제를 해야 합니다.</para>
    /// </summary>
    public sealed class EditorRepaintScheduler : IDisposable
    {
        private readonly UnityEngine.Object _target; // Repaint 대상
        private readonly EditorApplication.CallbackFunction _updateLoop; // GC-free delegate 캐싱
        private readonly UpdateMode _mode;
        private readonly int _frameInterval;
        private readonly float _secondInterval;
        // 읽기 전용에서 일반 필드로 변경하여 Dispose 시 null 할당 가능하게 변경
        private Action _postRepaintAction; // 추가된 델리게이트 (선택적)

        private int _frameCounter;
        private double _lastTime;
        private bool _isDisposed;

        /// <summary>
        /// 내부 실행 모드 (프레임 / 초)
        /// </summary>
        private enum UpdateMode
        {
            EveryFrame,
            EveryNFrame,
            EveryNSecond
        }

        /// <summary>
        /// 매 프레임마다 Repaint를 호출하는 스케줄러를 생성합니다.
        /// </summary>
        /// <param name="target">Repaint 가능한 EditorWindow 또는 Editor</param>
        /// <param name="postRepaintAction">
        /// Repaint 호출 후 실행할 후처리 액션. 별도로 실행할 내용이 없으면 null로 둘 수 있습니다.
        /// </param>
        public static EditorRepaintScheduler StartEveryFrame(UnityEngine.Object target, Action postRepaintAction = null)
            => new EditorRepaintScheduler(target, UpdateMode.EveryFrame, 1, 0, postRepaintAction);

        /// <summary>
        /// 지정된 프레임 간격으로 Repaint를 호출하는 스케줄러를 생성합니다.
        /// </summary>
        /// <param name="target">Repaint 가능한 EditorWindow 또는 Editor</param>
        /// <param name="frameInterval">Repaint 호출 간격 (프레임 단위)</param>
        /// <param name="postRepaintAction">
        /// Repaint 호출 후 실행할 후처리 액션. 별도로 실행할 내용이 없으면 null로 둘 수 있습니다.
        /// </param>
        public static EditorRepaintScheduler StartWithFrameInterval(UnityEngine.Object target, int frameInterval, Action postRepaintAction = null)
            => new EditorRepaintScheduler(target, UpdateMode.EveryNFrame, Mathf.Max(1, frameInterval), 0, postRepaintAction);

        /// <summary>
        /// 지정된 초 간격으로 Repaint를 호출하는 스케줄러를 생성합니다.
        /// </summary>
        /// <param name="target">Repaint 가능한 EditorWindow 또는 Editor</param>
        /// <param name="secondInterval">Repaint 호출 간격 (초 단위)</param>
        /// <param name="postRepaintAction">
        /// Repaint 호출 후 실행할 후처리 액션. 별도로 실행할 내용이 없으면 null로 둘 수 있습니다.
        /// </param>
        public static EditorRepaintScheduler StartWithSecondInterval(UnityEngine.Object target, float secondInterval, Action postRepaintAction = null)
            => new EditorRepaintScheduler(target, UpdateMode.EveryNSecond, 0, Mathf.Max(0.01f, secondInterval), postRepaintAction);

        private EditorRepaintScheduler(UnityEngine.Object target, UpdateMode mode, int frameInterval, float secondInterval, Action postRepaintAction)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _mode = mode;
            _frameInterval = frameInterval;
            _secondInterval = secondInterval;
            _postRepaintAction = postRepaintAction;
            _lastTime = EditorApplication.timeSinceStartup;

            _updateLoop = Tick; // GC-free delegate 캐싱
            EditorApplication.update += _updateLoop; // Editor의 매 프레임 업데이트 루프에 등록
        }

        private void Tick()
        {
            // 객체가 파괴되었거나 Dispose된 경우 즉시 제거
            if (_isDisposed || _target == null)
            {
                Dispose();
                return;
            }

            switch (_mode)
            {
                case UpdateMode.EveryFrame:
                Repaint();
                _postRepaintAction?.Invoke();
                break;

                case UpdateMode.EveryNFrame:
                if (++_frameCounter >= _frameInterval)
                {
                    _frameCounter = 0;
                    Repaint();
                    _postRepaintAction?.Invoke();
                }
                break;

                case UpdateMode.EveryNSecond:
                var now = EditorApplication.timeSinceStartup;
                if (now - _lastTime >= _secondInterval)
                {
                    _lastTime = now;
                    Repaint();
                    _postRepaintAction?.Invoke();
                }
                break;
            }
        }

        /// <summary>
        /// <c>Repaint()</c>를 EditorWindow 또는 Editor에 안전하게 실행
        /// </summary>
        [System.Diagnostics.DebuggerHidden]
        private void Repaint()
        {
            // EditorWindow와 Editor 모두 Repaint 메서드가 존재함
            switch (_target)
            {
                case EditorWindow window:
                window.Repaint();
                break;
                case UnityEditor.Editor editor:
                editor.Repaint();
                break;
            }
        }

        /// <summary>
        /// 실행 중인 Repaint 스케줄러를 해제하고, EditorApplication.update에서 제거합니다.
        /// <para><b>이 메서드를 호출하지 않으면, 루프가 영구적으로 남아있어 GC 누수 및 불필요한 호출이 발생할 수 있습니다.</b></para>
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            EditorApplication.update -= _updateLoop; // 반드시 제거 필요

            // 후처리 델리게이트 해제: 강한 참조가 있으면 메모리 누수가 발생할 수 있으므로 명시적으로 null 할당
            _postRepaintAction = null;

            _isDisposed = true;
        }
    }
}