using Cysharp.Threading.Tasks;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Pan.Util;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;



//? DOTween을 확장하는 기능이 들어있는 정도의 코드



namespace DG.Tweening
{
    ///======================================================================================================================================================



    public static class SU_DOTweenExpand
    {
        ///======================================================================================================================================================



        #region 이동: 원형 궤도 이동



        //? 월드 좌표 기준 원형 궤도 이동



        /// <summary>
        /// Transform을 월드 좌표 기준으로 원형 궤도를 따라 트윈 이동시킵니다.
        /// </summary>
        /// <remarks>
        /// <para>고정된 반지름(radius)을 기준으로, 지정된 라디안 각도(endValue)만큼만 회전 궤적을 그립니다.</para>
        /// <para>World 좌표에서 원형 이동이 필요할 때 사용합니다.</para>
        /// </remarks>
        /// <param name="radius">이동할 원의 반지름</param>
        /// <param name="endValue">트윈으로 이동할 최종 라디안 값 (2π로 설정 시 360도 회전)</param>
        /// <param name="duration">이동 지속 시간</param>
        /// <param name="plusPosition">트윈 시작 위치에 추가로 더할 라디안 값</param>
        /// <param name="startPosition">이동 시작 기준점 (null이면 현재 Transform의 월드 위치 사용)</param>
        /// <param name="snapping">좌표를 정수 단위로 스냅핑할지 여부</param>
        /// <returns>생성된 DOTween TweenerCore 객체</returns>
        public static TweenerCore<float, float, FloatOptions> DOMoveAlongCircle
        (
            this Transform target,
            float radius,
            float endValue,
            float duration,
            float plusPosition = 0f,
            Vector2? startPosition = null,
            bool snapping = false)
        {
            Vector2 fixStartPosition = startPosition ?? target.position;

            TweenerCore<float, float, FloatOptions> tweenerCore = DOTween.To(
                () => 0f,
                angle => target.position = new Vector2(
                    Mathf.Sin(angle + plusPosition) * radius + fixStartPosition.x,
                    Mathf.Cos(angle + plusPosition) * radius + fixStartPosition.y
                ),
                endValue,
                duration
            );

            tweenerCore.SetOptions(snapping).SetTarget(target);
            return tweenerCore;
        }



        /// <summary>
        /// Transform을 월드 좌표 기준으로 원형 궤도를 따라 트윈 이동시키되, 반지름을 동적으로 조절할 수 있습니다.
        /// </summary>
        /// <remarks>
        /// <para>radiusFunc를 통해 매 이동 프레임마다 반지름을 동적으로 계산하여 원형 궤도를 그립니다.</para>
        /// <para>게임 내에서 반지름이 변동되는 상황(예: 커지는 원, 줄어드는 원 등)에서 활용이 가능합니다.</para>
        /// </remarks>
        /// <param name="radiusFunc">반지름을 반환하는 함수 (실시간으로 호출되어 가변 반지름 적용 가능)</param>
        /// <param name="endValue">트윈으로 이동할 최종 라디안 값 (2π로 설정 시 360도 회전)</param>
        /// <param name="duration">이동 지속 시간</param>
        /// <param name="plusPosition">트윈 시작 위치에 추가로 더할 라디안 값</param>
        /// <param name="startPosition">이동 시작 기준점 (null이면 현재 Transform의 월드 위치 사용)</param>
        /// <param name="snapping">좌표를 정수 단위로 스냅핑할지 여부</param>
        /// <returns>생성된 DOTween TweenerCore 객체</returns>
        public static TweenerCore<float, float, FloatOptions> DOMoveAlongCircle
        (
            this Transform target,
            Func<float> radiusFunc,
            float endValue,
            float duration,
            float plusPosition = 0f,
            Vector2? startPosition = null,
            bool snapping = false)
        {
            Vector2 fixStartPosition = startPosition ?? target.position;

            TweenerCore<float, float, FloatOptions> tweenerCore = DOTween.To(
                () => 0f,
                angle => target.position = new Vector2(
                    Mathf.Sin(angle + plusPosition) * radiusFunc.Invoke() + fixStartPosition.x,
                    Mathf.Cos(angle + plusPosition) * radiusFunc.Invoke() + fixStartPosition.y
                ),
                endValue,
                duration
            );

            tweenerCore.SetOptions(snapping).SetTarget(target);
            return tweenerCore;
        }



        //? 월드 좌표 기준 원형 궤도 한 바퀴(360도) 이동



        /// <summary>
        /// Transform을 월드 좌표 기준으로 한 바퀴(360도) 회전시키는 트윈 애니메이션을 실행합니다.
        /// </summary>
        /// <remarks>
        /// <para>DOMoveAlongCircle 메서드에 2π(360도)를 직접 전달하여 한 바퀴를 도는 확장 메서드입니다.</para>
        /// <para>간단히 전체 회전 궤도(360도)를 만들고 싶을 때 사용하세요.</para>
        /// </remarks>
        /// <param name="radius">이동할 원의 반지름</param>
        /// <param name="duration">이동 지속 시간</param>
        /// <param name="plusPosition">트윈 시작 위치에 추가로 더할 라디안 값</param>
        /// <param name="startPosition">이동 시작 기준점 (null이면 현재 Transform의 월드 위치 사용)</param>
        /// <param name="snapping">좌표를 정수 단위로 스냅핑할지 여부</param>
        /// <returns>생성된 DOTween TweenerCore 객체</returns>
        public static TweenerCore<float, float, FloatOptions> DOMoveCircleFullRotation
        (
            this Transform target,
            float radius,
            float duration,
            float plusPosition = 0f,
            Vector2? startPosition = null,
            bool snapping = false)
        {
            return DOMoveAlongCircle(target, radius, 2f * Mathf.PI, duration, plusPosition, startPosition, snapping);
        }



        /// <summary>
        /// Transform을 월드 좌표 기준으로 한 바퀴(360도) 회전시키는 트윈 애니메이션을 실행하며, 반지름을 동적으로 조절할 수 있습니다.
        /// </summary>
        /// <remarks>
        /// <para>DOMoveAlongCircle(동적 반지름 버전)에 2π(360도)를 전달하여 전체 회전을 도는 확장 메서드입니다.</para>
        /// <para>회전 중에 반지름이 변하는 다양한 연출에 적합합니다.</para>
        /// </remarks>
        /// <param name="radiusFunc">반지름을 반환하는 함수</param>
        /// <param name="duration">이동 지속 시간</param>
        /// <param name="plusPosition">트윈 시작 위치에 추가로 더할 라디안 값</param>
        /// <param name="startPosition">이동 시작 기준점 (null이면 현재 Transform의 월드 위치 사용)</param>
        /// <param name="snapping">좌표를 정수 단위로 스냅핑할지 여부</param>
        /// <returns>생성된 DOTween TweenerCore 객체</returns>
        public static TweenerCore<float, float, FloatOptions> DOMoveCircleFullRotation
        (
            this Transform target,
            Func<float> radiusFunc,
            float duration,
            float plusPosition = 0f,
            Vector2? startPosition = null,
            bool snapping = false)
        {
            return DOMoveAlongCircle(target, radiusFunc, 2f * Mathf.PI, duration, plusPosition, startPosition, snapping);
        }



        //? 로컬 좌표 기준 원형 궤도 이동



        /// <summary>
        /// Transform을 로컬 좌표 기준으로 원형 궤도를 따라 트윈 이동시킵니다.
        /// </summary>
        /// <remarks>
        /// <para>부모 Transform 기준 좌표계에서 원형 이동이 필요할 때 사용합니다.</para>
        /// <para>고정된 반지름(radius)을 활용하여 부분 회전 궤적을 만듭니다.</para>
        /// </remarks>
        /// <param name="radius">이동할 원의 반지름</param>
        /// <param name="endValue">트윈으로 이동할 최종 라디안 값 (2π로 설정 시 360도 회전)</param>
        /// <param name="duration">이동 지속 시간</param>
        /// <param name="plusPosition">트윈 시작 위치에 추가로 더할 라디안 값</param>
        /// <param name="startPosition">이동 시작 기준점 (null이면 현재 Transform의 로컬 위치 사용)</param>
        /// <param name="snapping">좌표를 정수 단위로 스냅핑할지 여부</param>
        /// <returns>생성된 DOTween TweenerCore 객체</returns>
        public static TweenerCore<float, float, FloatOptions> DOLocalMoveAlongCircle
        (
            this Transform target,
            float radius,
            float endValue,
            float duration,
            float plusPosition = 0f,
            Vector2? startPosition = null,
            bool snapping = false)
        {
            Vector2 fixStartPosition = startPosition ?? target.localPosition;

            TweenerCore<float, float, FloatOptions> tweenerCore = DOTween.To(
                () => 0f,
                angle => target.localPosition = new Vector2(
                    Mathf.Sin(angle + plusPosition) * radius + fixStartPosition.x,
                    Mathf.Cos(angle + plusPosition) * radius + fixStartPosition.y
                ),
                endValue,
                duration
            );

            tweenerCore.SetOptions(snapping).SetTarget(target);
            return tweenerCore;
        }



        /// <summary>
        /// Transform을 로컬 좌표 기준으로 원형 궤도를 따라 트윈 이동시키되, 반지름을 동적으로 조절할 수 있습니다.
        /// </summary>
        /// <remarks>
        /// <para>radiusFunc를 통해 로컬 좌표계에서 반지름을 동적으로 계산하여 이동합니다.</para>
        /// <para>부모 객체가 움직이면서도 반지름이 변동되어야 하는 상황에 유용합니다.</para>
        /// </remarks>
        /// <param name="radiusFunc">반지름을 반환하는 함수</param>
        /// <param name="endValue">트윈으로 이동할 최종 라디안 값 (2π로 설정 시 360도 회전)</param>
        /// <param name="duration">이동 지속 시간</param>
        /// <param name="plusPosition">트윈 시작 위치에 추가로 더할 라디안 값</param>
        /// <param name="startPosition">이동 시작 기준점 (null이면 현재 Transform의 로컬 위치 사용)</param>
        /// <param name="snapping">좌표를 정수 단위로 스냅핑할지 여부</param>
        /// <returns>생성된 DOTween TweenerCore 객체</returns>
        public static TweenerCore<float, float, FloatOptions> DOLocalMoveAlongCircle
        (
            this Transform target,
            Func<float> radiusFunc,
            float endValue,
            float duration,
            float plusPosition = 0f,
            Vector2? startPosition = null,
            bool snapping = false)
        {
            Vector2 fixStartPosition = startPosition ?? target.localPosition;

            TweenerCore<float, float, FloatOptions> tweenerCore = DOTween.To(
                () => 0f,
                angle => target.localPosition = new Vector2(
                    Mathf.Sin(angle + plusPosition) * radiusFunc.Invoke() + fixStartPosition.x,
                    Mathf.Cos(angle + plusPosition) * radiusFunc.Invoke() + fixStartPosition.y
                ),
                endValue,
                duration
            );

            tweenerCore.SetOptions(snapping).SetTarget(target);
            return tweenerCore;
        }



        //? 로컬 좌표 기준 원형 궤도 한 바퀴(360도) 이동

        /// <summary>
        /// Transform을 로컬 좌표 기준으로 한 바퀴(360도) 회전시키는 트윈 애니메이션을 실행합니다.
        /// </summary>
        /// <remarks>
        /// <para>2π(360도)를 이동하는 DOLocalMoveAlongCircle를 간단히 호출할 수 있도록 만든 확장 메서드입니다.</para>
        /// <para>로컬 좌표에서 완전한 원 궤도 이동이 필요할 때 사용합니다.</para>
        /// </remarks>
        /// <param name="radius">이동할 원의 반지름</param>
        /// <param name="duration">이동 지속 시간</param>
        /// <param name="plusPosition">트윈 시작 위치에 추가로 더할 라디안 값</param>
        /// <param name="startPosition">이동 시작 기준점 (null이면 현재 Transform의 로컬 위치 사용)</param>
        /// <param name="snapping">좌표를 정수 단위로 스냅핑할지 여부</param>
        /// <returns>생성된 DOTween TweenerCore 객체</returns>
        public static TweenerCore<float, float, FloatOptions> DOLocalMoveCircleFullRotation
        (
            this Transform target,
            float radius,
            float duration,
            float plusPosition,
            Vector2? startPosition = null,
            bool snapping = false)
        {
            return DOLocalMoveAlongCircle(target, radius, 2f * Mathf.PI, duration, plusPosition, startPosition, snapping);
        }



        //? Vector2 값을 기준으로 원형 궤도 이동 (Setter 사용)

        /// <summary>
        /// Vector2 값을 Setter를 통해 원형 궤도를 따라 트윈 이동시킵니다.
        /// </summary>
        /// <remarks>
        /// <para>startPositionFunc와 radiusFunc를 통해 시작 위치와 반지름을 동적으로 지정할 수 있습니다.</para>
        /// <para>Vector2 값만 필요하다면 Transform 대신 이 메서드를 직접 활용해 보세요.</para>
        /// </remarks>
        /// <param name="setter">트윈 중인 Vector2 값을 설정할 Setter</param>
        /// <param name="radiusFunc">반지름을 반환하는 함수</param>
        /// <param name="endValue">트윈으로 이동할 최종 라디안 값</param>
        /// <param name="duration">이동 지속 시간</param>
        /// <param name="startPositionFunc">이동 시작 기준점 함수 (null이면 (0,0) 사용)</param>
        /// <param name="plusPosition">트윈 시작 위치에 추가로 더할 라디안 값</param>
        /// <param name="snapping">좌표를 정수 단위로 스냅핑할지 여부</param>
        /// <returns>생성된 DOTween TweenerCore 객체</returns>
        public static TweenerCore<float, float, FloatOptions> TweenVector2_MoveAlongCircle
        (
            DOSetter<Vector2> setter,
            Func<float> radiusFunc,
            float endValue,
            float duration,
            Func<Vector2> startPositionFunc = null,
            float plusPosition = 0f,
            bool snapping = false)
        {
            startPositionFunc ??= () => Vector2.zero;

            TweenerCore<float, float, FloatOptions> tweenerCore = DOTween.To(
                () => 0f,
                angle =>
                {
                    float radius = radiusFunc.Invoke();
                    Vector2 startPosition = startPositionFunc.Invoke();
                    setter.Invoke(new Vector2(
                        Mathf.Sin(angle + plusPosition) * radius + startPosition.x,
                        Mathf.Cos(angle + plusPosition) * radius + startPosition.y
                    ));
                },
                endValue,
                duration
            );

            tweenerCore.SetOptions(snapping);
            return tweenerCore;
        }



        /// <summary>
        /// Vector2 값을 Setter를 이용해 원형 궤도를 따라 트윈 이동시킵니다. (시작 위치를 고정)
        /// </summary>
        /// <remarks>
        /// <para>startPositionFunc 대신 고정된 startPosition을 사용할 때 호출합니다.</para>
        /// <para>radiusFunc로 반지름을 동적으로 계산하지만, 시작 위치는 고정합니다.</para>
        /// </remarks>
        /// <param name="setter">트윈 중인 Vector2 값을 설정할 Setter</param>
        /// <param name="radiusFunc">반지름을 반환하는 함수</param>
        /// <param name="endValue">트윈으로 이동할 최종 라디안 값</param>
        /// <param name="duration">이동 지속 시간</param>
        /// <param name="startPosition">이동 시작 기준점</param>
        /// <param name="plusPosition">트윈 시작 위치에 추가로 더할 라디안 값</param>
        /// <param name="snapping">좌표를 정수 단위로 스냅핑할지 여부</param>
        /// <returns>생성된 DOTween TweenerCore 객체</returns>
        public static TweenerCore<float, float, FloatOptions> TweenVector2_MoveAlongCircle
        (
            DOSetter<Vector2> setter,
            Func<float> radiusFunc,
            float endValue,
            float duration,
            Vector2 startPosition,
            float plusPosition = 0f,
            bool snapping = false)
        {
            TweenerCore<float, float, FloatOptions> tweenerCore = DOTween.To(
                () => 0f,
                angle =>
                {
                    float radius = radiusFunc.Invoke();
                    setter.Invoke(new Vector2(
                        Mathf.Sin(angle + plusPosition) * radius + startPosition.x,
                        Mathf.Cos(angle + plusPosition) * radius + startPosition.y
                    ));
                },
                endValue,
                duration
            );

            tweenerCore.SetOptions(snapping);
            return tweenerCore;
        }



        //? 벨로시티 기반 원형 궤도 이동



        /// <summary>
        /// Vector2 값을 Setter를 통해, 벨로시티(속도) 기반으로 원형 궤도를 따라 트윈 이동시킵니다.
        /// </summary>
        /// <remarks>
        /// <para>별도의 기준점 없이, 순수하게 원점을 중심으로 한 벨로시티 이동을 구현합니다.</para>
        /// <para>원점이 아닌 다른 위치를 기준점으로 삼고 싶다면 <see cref="TweenVector2_MoveAlongCircle"/> 계열 메서드를 사용하세요.</para>
        /// </remarks>
        /// <param name="setter">트윈 중인 Vector2 값을 설정할 Setter</param>
        /// <param name="radiusFunc">반지름을 반환하는 함수</param>
        /// <param name="endValue">트윈으로 이동할 최종 라디안 값</param>
        /// <param name="duration">이동 지속 시간</param>
        /// <param name="plusPosition">트윈 시작 위치에 추가로 더할 라디안 값</param>
        /// <param name="snapping">좌표를 정수 단위로 스냅핑할지 여부</param>
        /// <returns>생성된 DOTween TweenerCore 객체</returns>
        public static TweenerCore<float, float, FloatOptions> TweenVector2_MoveAlongCircleWithVelocity
        (
            this DOSetter<Vector2> setter,
            Func<float> radiusFunc,
            float endValue,
            float duration,
            float plusPosition = 0f,
            bool snapping = false)
        {
            TweenerCore<float, float, FloatOptions> tweenerCore = DOTween.To(
                () => 0f,
                angle => setter.Invoke(new Vector2(
                    Mathf.Sin(angle + plusPosition) * radiusFunc.Invoke(),
                    Mathf.Cos(angle + plusPosition) * radiusFunc.Invoke()
                )),
                endValue,
                duration
            );

            tweenerCore.SetOptions(snapping);
            return tweenerCore;
        }



        //? 특정 중심점을 따라가면서 원형 궤도 이동



        /// <summary>
        /// 특정 중심점을 따라가면서 원형 궤도를 그리도록 트윈 이동시킵니다.
        /// </summary>
        /// <remarks>
        /// <para>mainPositionFunc를 통해 계속 변하는 중심점을 추적하면서, 매 프레임마다 새 위치를 계산합니다.</para>
        /// <para>플레이어나 보스 같은 오브젝트를 기준점으로 삼는 경우에 유용합니다.</para>
        /// </remarks>
        /// <param name="setter">트윈 중인 Vector2 값을 설정할 Setter</param>
        /// <param name="radiusFunc">반지름을 반환하는 함수</param>
        /// <param name="endValue">트윈으로 이동할 최종 라디안 값</param>
        /// <param name="duration">이동 지속 시간</param>
        /// <param name="mainPositionFunc">중심점의 위치를 반환하는 함수 (실시간으로 값이 변할 수 있음)</param>
        /// <param name="plusPosition">트윈 시작 위치에 추가로 더할 라디안 값</param>
        /// <param name="snapping">좌표를 정수 단위로 스냅핑할지 여부</param>
        /// <returns>생성된 DOTween TweenerCore 객체</returns>
        public static TweenerCore<float, float, FloatOptions> TweenVector2_MoveAlongCircleFollowing
        (
            this DOSetter<Vector2> setter,
            Func<float> radiusFunc,
            float endValue,
            float duration,
            Func<Vector2> mainPositionFunc,
            float plusPosition = 0f,
            bool snapping = false)
        {
            Vector2 prevMainPosition = mainPositionFunc.Invoke();

            TweenerCore<float, float, FloatOptions> tweenerCore = DOTween.To(
                () => 0f,
                angle =>
                {
                    float radius = radiusFunc.Invoke();
                    Vector2 mainPosition = mainPositionFunc.Invoke();
                    Vector2 deltaPosition = mainPosition - prevMainPosition;
                    prevMainPosition = mainPosition;

                    setter.Invoke(new Vector2(
                        Mathf.Sin(angle + plusPosition) * radius + deltaPosition.x,
                        Mathf.Cos(angle + plusPosition) * radius + deltaPosition.y
                    ));
                },
                endValue,
                duration
            );

            tweenerCore.SetOptions(snapping);
            return tweenerCore;
        }



        #endregion



        ///======================================================================================================================================================



        //? 이동: 회전



        /// <summary>
        /// 지정된 Transform의 Z축을 월드 기준으로 회전시킵니다.
        /// </summary>
        /// <param name="target">회전 대상 Transform</param>
        /// <param name="valueZ">회전시킬 Z축 각도</param>
        /// <param name="duration">회전에 소요되는 시간</param>
        /// <param name="mode">회전 모드</param>
        /// <returns>DOTween의 Tween 객체</returns>
        public static Tween DORotateZ(this Transform target, float valueZ, float duration, RotateMode mode = RotateMode.Fast)
        {
            //? 월드 좌표 기준으로 Z축만 회전
            return target.DORotate(new Vector3(0, 0, valueZ), duration, mode);
        }



        /// <summary>
        /// 지정된 Transform의 Z축을 로컬 기준으로 회전시킵니다.
        /// </summary>
        /// <param name="target">회전 대상 Transform</param>
        /// <param name="valueZ">회전시킬 Z축 각도</param>
        /// <param name="duration">회전에 소요되는 시간</param>
        /// <param name="mode">회전 모드</param>
        /// <returns>DOTween의 Tween 객체</returns>
        public static Tween DOLocalRotateZ(this Transform target, float valueZ, float duration, RotateMode mode = RotateMode.Fast)
        {
            //? 로컬 좌표 기준으로 Z축만 회전
            return target.DOLocalRotate(new Vector3(0, 0, valueZ), duration, mode);
        }



        ///======================================================================================================================================================



        //? 시퀀스 확장



        /// <summary>
        /// DOTween 시퀀스를 업데이트 이벤트로 만들기 위해 설정 (일시정지 한채로 반환)
        /// </summary>
        /// <param name="sequence">설정할 DOTween 시퀀스</param>
        /// <param name="updateAction">업데이트 시 실행될 콜백 함수</param>
        /// <returns>설정이 적용된 시퀀스</returns>
        public static Sequence ReadyForUpdate(this Sequence sequence, TweenCallback updateAction)
        {
            sequence.OnUpdate(updateAction); //? 업데이트 시 실행할 콜백 설정
            sequence.SetIfnLoops();
            sequence.Pause();
            return sequence;
        }



        ///======================================================================================================================================================



        //? 트윈 확장



        /// <summary>
        /// DOTween 시퀀스의 반복 횟수를 무한<c>(-1)</c>으로 만들기
        /// </summary>
        /// <param name="var"></param>
        /// <param name="loopType"></param>
        /// <returns></returns>
        public static Tween SetIfnLoops(this Tween var, LoopType? loopType = null)
        {
            if (loopType.HasValue) { var.SetLoops(-1, loopType.Value); }
            else { var.SetLoops(-1); }
            return var;
        }



        /// <summary>
        /// DOTween 시퀀스의 반복 횟수를 무한<c>(-1)</c>으로 만들기
        /// </summary>
        /// <param name="var"></param>
        /// <param name="loopType"></param>
        /// <returns></returns>
        public static Sequence SetIfnLoops(this Sequence var, LoopType? loopType = null)
        {
            if (loopType.HasValue) { var.SetLoops(-1, loopType.Value); }
            else { var.SetLoops(-1); }
            return var;
        }



        ///======================================================================================================================================================



        ///<summary>
        /// 캐싱을 위한 설정을 한다 (실행시 시작할수있게 정지, 종료되어도 자동 Kill 방지)
        /// </summary>
        public static Tween SettingForCaching(this Tween tween)
        {
            tween.Pause();
            tween.SetAutoKill(false);
            return tween;
        }



        ///<summary>
        /// 캐싱을 위한 설정을 한다 (실행시 시작할수있게 정지, 종료되어도 자동 Kill 방지)
        /// </summary>
        public static Sequence SettingForCaching(this Sequence sequence)
        {
            sequence.Pause();
            sequence.SetAutoKill(false);
            return sequence;
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================



    //? (폐기) 커스텀 트윈 매니저



    #region Legacy 커스텀 트윈 매니저
    ///// <summary>
    ///// 트윈들을 보관하고 일괄적으로 TimeScale을 적용하는 기본 매니저
    ///// </summary>
    //[Obsolete]
    //public class LegacyCustomTweenManager
    //{
    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// 매니저가 UpdateAll을 어떤 방식으로 실행할지 결정한다.
    //    /// </summary>
    //    public enum UpdateMode
    //    {
    //        /// <summary>아무 모드도 사용하지 않음. 직접 ManualUpdate로 호출</summary>
    //        None,

    //        /// <summary>UniTask를 이용해 매 프레임마다 UpdateAll 실행</summary>
    //        UniTaskUpdate,

    //        /// <summary>DOTween 시퀀스의 OnUpdate를 통해 매 프레임마다 UpdateAll 실행</summary>
    //        DOTweenUpdate,
    //    }



    //    ///======================================================================================================================================================



    //    public LegacyCustomTweenManager(int capacity = 0)
    //    {
    //        TweenList = new List<Tween>(capacity);
    //        _originalTimeScales = new Dictionary<Tween, float>(capacity);
    //    }



    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// 등록된 트윈들이 모이는 리스트
    //    /// </summary>
    //    protected readonly List<Tween> TweenList;



    //    /// <summary>
    //    /// 트윈 리스트 얻기 (읽기전용)
    //    /// </summary>
    //    protected IReadOnlyList<Tween> GetTweenList => TweenList;



    //    /// <summary>
    //    /// 현재 등록된 트윈 개수
    //    /// </summary>
    //    public int TweenCount => TweenList.Count;



    //    /// <summary>
    //    /// 각 트윈이 원래 가지고 있던 timeScale 값을 저장
    //    /// </summary>

    //    protected readonly Dictionary<Tween, float> _originalTimeScales;



    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// 외부에서 원하는 timeScale 값을 제공해주는 함수
    //    /// </summary>
    //    public Func<float> TimeScaleFunc { get; set; }



    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// 현재 매니저가 동작 중인 업데이트 모드 (None, UniTaskUpdate, DOTweenUpdate)
    //    /// </summary>
    //    private UpdateMode _updateMode = UpdateMode.None;


    //    /// <summary>
    //    /// UniTaskUpdate 모드에서 사용되는 취소 토큰
    //    /// </summary>
    //    private CancellationTokenSource _ctsForUniTask;



    //    /// <summary>
    //    /// DOTweenUpdate 모드에서 사용하는 시퀀스
    //    /// </summary>
    //    private Sequence _sequenceForDOTween;


    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// 트윈을 매니저에 등록한다. (중복 등록 방지)
    //    /// </summary>
    //    /// <param name="tween">등록할 Tween</param>
    //    public virtual Tween AddTween(Tween tween)
    //    {
    //        if (tween == null) return tween;
    //        if (TweenList.Contains(tween)) return tween;
    //        //. 이미 리스트에 있다면 중복 등록 무시

    //        //. 트윈 리스트에 추가
    //        TweenList.Add(tween);

    //        //. 트윈의 원래 timeScale을 기억
    //        _originalTimeScales[tween] = tween.timeScale;

    //        return tween;
    //    }



    //    /// <summary>
    //    /// 매니저에서 트윈을 제거한다.
    //    /// </summary>
    //    /// <param name="tween">제거할 트윈</param>
    //    /// <param name="kill">true면 트윈을 Kill한 뒤 제거</param>
    //    /// <returns>제거 성공 여부</returns>
    //    public virtual bool RemoveTween(Tween tween, bool kill = true)
    //    {
    //        int idx = TweenList.IndexOf(tween);
    //        if (idx < 0) return false;

    //        //. kill 옵션이 true이고 트윈이 살아있다면 Kill
    //        if (kill && tween.active)
    //        {
    //            tween.Kill();
    //        }

    //        TweenList.RemoveAt(idx);
    //        _originalTimeScales.Remove(tween);
    //        return true;
    //    }



    //    /// <summary>
    //    /// 보유 중인 모든 트윈을 제거한다.
    //    /// </summary>
    //    /// <param name="kill">true면 모두 Kill 후 제거</param>
    //    public virtual void ClearAll(bool kill = true)
    //    {
    //        if (kill)
    //        {
    //            for (int i = 0; i < TweenList.Count; i++)
    //            {
    //                var tw = TweenList[i];
    //                if (tw != null && tw.active)
    //                {
    //                    tw.Kill();
    //                }
    //            }
    //        }
    //        TweenList.Clear();
    //        _originalTimeScales.Clear();
    //    }



    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// 매니저에 등록된 트윈들을 일괄 갱신한다.
    //    /// 1) 비활성화된 트윈 제거
    //    /// 2) timeScale = 원래 timeScale * 매니저 timeScaleFunc()
    //    /// </summary>
    //    public virtual void UpdateAll()
    //    {
    //        float managerTimeScale = (TimeScaleFunc != null) ? TimeScaleFunc.Invoke() : 1f;

    //        //. 뒤에서부터 검사하여 죽은 트윈은 제거
    //        for (int i = TweenList.Count - 1; i >= 0; i--)
    //        {
    //            Tween tw = TweenList[i];
    //            if (tw == null || !tw.active)
    //            {
    //                TweenList.RemoveAt(i);
    //                _originalTimeScales.Remove(tw);
    //                continue;
    //            }

    //            //. 살아있는 트윈은 timeScale 적용
    //            float baseTS = _originalTimeScales[tw];
    //            tw.timeScale = baseTS * managerTimeScale;
    //        }
    //    }



    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// UpdateAll을 자동 실행할 모드를 설정한다. (None, UniTaskUpdate, DOTweenUpdate)
    //    /// </summary>
    //    /// <param name="mode">선택할 업데이트 모드</param>
    //    public void ExecuteUpdate(UpdateMode mode, Func<float> timeScaleFunc = null)
    //    {
    //        QuitUpdate();

    //        TimeScaleFunc = timeScaleFunc;

    //        _updateMode = mode;
    //        switch (_updateMode)
    //        {
    //            case UpdateMode.None:
    //            //. 수동으로 UpdateAll을 호출하는 모드
    //            break;

    //            case UpdateMode.UniTaskUpdate:
    //            //. UniTask를 이용해 매 프레임 UpdateAll 실행
    //            _ctsForUniTask = new CancellationTokenSource();
    //            UniTaskUpdateLoop(_ctsForUniTask.Token).Forget();
    //            break;

    //            case UpdateMode.DOTweenUpdate:
    //            //. DOTween 시퀀스에서 OnUpdate로 매 프레임 UpdateAll
    //            if (_sequenceForDOTween == null)
    //            {
    //                _sequenceForDOTween = DOTween.Sequence()
    //                    .SetUpdate(UpdateType.Normal)
    //                    .SetAutoKill(false)
    //                    .OnUpdate(() =>
    //                    {
    //                        UpdateAll();
    //                    });
    //            }
    //            _sequenceForDOTween.Play();
    //            break;
    //        }
    //    }



    //    /// <summary>
    //    /// 현재 모드의 Update 루프를 중지한다.
    //    /// </summary>
    //    public void QuitUpdate()
    //    {
    //        //. UniTaskUpdate 모드 중단
    //        if (_ctsForUniTask != null)
    //        {
    //            _ctsForUniTask.Cancel();
    //            _ctsForUniTask.Dispose();
    //            _ctsForUniTask = null;
    //        }

    //        //. DOTweenUpdate 모드 중단(시퀀스 Pause)
    //        if (_sequenceForDOTween != null)
    //        {
    //            _sequenceForDOTween.Pause();
    //        }

    //        _updateMode = UpdateMode.None;
    //    }



    //    /// <summary>
    //    /// UniTaskUpdate 모드 시, 매 프레임마다 UpdateAll을 호출하는 코루틴
    //    /// </summary>
    //    private async UniTaskVoid UniTaskUpdateLoop(CancellationToken token)
    //    {
    //        try
    //        {
    //            while (!token.IsCancellationRequested)
    //            {
    //                UpdateAll();
    //                await UniTask.Yield(PlayerLoopTiming.Update, token);
    //            }
    //        }
    //        catch (OperationCanceledException)
    //        {
    //            //? 토큰 취소 시 빠져나옴
    //        }
    //    }



    //    ///======================================================================================================================================================
    //}



    ///// <summary>
    ///// 문자열 태그 기능을 추가한 매니저
    ///// </summary>
    //[Obsolete]
    //public class LegacyCustomTweenManager_Extend : LegacyCustomTweenManager
    //{
    //    ///======================================================================================================================================================



    //    public LegacyCustomTweenManager_Extend(int capacity = 0, int tagCapacity = 0) : base(capacity)
    //    {
    //        TagMap = new Dictionary<string, HashSet<Tween>>(tagCapacity);
    //    }



    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// 태그 문자열 -> 그 태그를 보유한 트윈 집합
    //    /// </summary>
    //    protected readonly Dictionary<string, HashSet<Tween>> TagMap;



    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// 트윈을 태그와 함께 등록한다. 태그가 1개일 때 사용
    //    /// </summary>
    //    /// <param name="tween">등록할 트윈</param>
    //    /// <param name="tag">문자열 태그</param>
    //    public void AddTween(Tween tween, string tag)
    //    {
    //        base.AddTween(tween);
    //        if (!string.IsNullOrEmpty(tag))
    //        {
    //            AddTag(tween, tag);
    //        }
    //    }



    //    /// <summary>
    //    /// 트윈을 여러 태그와 함께 등록한다.
    //    /// </summary>
    //    /// <param name="tween">등록할 트윈</param>
    //    /// <param name="tags">문자열 태그 배열</param>
    //    public void AddTween(Tween tween, params string[] tags)
    //    {
    //        base.AddTween(tween);
    //        if (tags != null)
    //        {
    //            for (int i = 0; i < tags.Length; i++)
    //            {
    //                if (!string.IsNullOrEmpty(tags[i]))
    //                {
    //                    AddTag(tween, tags[i]);
    //                }
    //            }
    //        }
    //    }



    //    //? 트윈을 태그맵에 추가
    //    private void AddTag(Tween tween, string tag)
    //    {
    //        if (!TagMap.TryGetValue(tag, out var set))
    //        {
    //            set = new HashSet<Tween>();
    //            TagMap[tag] = set;
    //        }
    //        set.Add(tween);
    //    }



    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// 트윈 제거 시, 태그맵에서도 함께 제거
    //    /// </summary>
    //    public override bool RemoveTween(Tween tween, bool kill = true)
    //    {
    //        bool removed = base.RemoveTween(tween, kill);
    //        if (removed)
    //        {
    //            RemoveTweenFromAllTag(tween);
    //        }
    //        return removed;
    //    }



    //    //? 모든 태그 집합에서 해당 트윈 삭제
    //    private void RemoveTweenFromAllTag(Tween tween)
    //    {
    //        foreach (var kvp in TagMap)
    //        {
    //            kvp.Value.Remove(tween);
    //        }
    //    }



    //    /// <summary>
    //    /// 특정 태그에 속한 트윈들을 한꺼번에 제거한다.
    //    /// </summary>
    //    /// <param name="tag">제거할 태그</param>
    //    /// <param name="kill">제거 시 트윈을 Kill할지 여부</param>
    //    public void RemoveByTag(string tag, bool kill = true)
    //    {
    //        if (!TagMap.TryGetValue(tag, out var set)) return;

    //        //. 중간에 set을 수정하면 예외가 발생하므로, 복사본을 만들어 사용
    //        var copyList = new List<Tween>(set);
    //        for (int i = 0; i < copyList.Count; i++)
    //        {
    //            RemoveTween(copyList[i], kill);
    //        }
    //    }



    //    /// <summary>
    //    /// 여러 태그들을 한 번에 받아, 해당되는 트윈 전부 제거
    //    /// </summary>
    //    public void RemoveByTags(bool kill, params string[] tags)
    //    {
    //        if (tags == null || tags.Length == 0) return;

    //        //. 중복을 없애기 위해 해시셋 사용
    //        HashSet<Tween> union = new HashSet<Tween>();
    //        for (int i = 0; i < tags.Length; i++)
    //        {
    //            if (TagMap.TryGetValue(tags[i], out var set))
    //            {
    //                foreach (var tw in set)
    //                {
    //                    union.Add(tw);
    //                }
    //            }
    //        }

    //        var copyList = new List<Tween>(union);
    //        for (int i = 0; i < copyList.Count; i++)
    //        {
    //            RemoveTween(copyList[i], kill);
    //        }
    //    }



    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// 특정 태그를 가진 트윈들을 전부 가져온다.
    //    /// </summary>
    //    /// <param name="tag">문자열 태그</param>
    //    /// <returns>태그를 가진 트윈 리스트</returns>
    //    public List<Tween> GetTweensByTag(string tag)
    //    {
    //        var result = new List<Tween>();
    //        if (TagMap.TryGetValue(tag, out var set))
    //        {
    //            result.AddRange(set);
    //        }
    //        return result;
    //    }



    //    /// <summary>
    //    /// 매니저를 완전히 초기화할 때 태그맵도 함께 비운다.
    //    /// </summary>
    //    /// <param name="kill">트윈들을 Kill할지 여부</param>
    //    public override void ClearAll(bool kill = true)
    //    {
    //        base.ClearAll(kill);
    //        TagMap.Clear();
    //    }



    //    ///======================================================================================================================================================
    //}



    ///// <summary>
    ///// 열거형(TTag)을 태그로 사용하는 확장 매니저
    ///// 열거형 -> 문자열로 변환한 뒤 기존 로직 재활용
    ///// </summary>
    //[Obsolete]
    //public class LegacyCustomTweenManager_Extend<TTag> : LegacyCustomTweenManager_Extend
    //    where TTag : struct, Enum, IComparable, IConvertible, IFormattable
    //{
    //    ///======================================================================================================================================================



    //    public LegacyCustomTweenManager_Extend(int capacity = 0) : base(capacity)
    //    {
    //        if (EnumStringMap == null)
    //        {
    //            EnumStringMap = new Dictionary<TTag, string>();
    //            var values = Enum.GetValues(typeof(TTag));
    //            foreach (TTag val in values)
    //            {
    //                EnumStringMap[val] = val.ToString();
    //            }
    //        }
    //    }



    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// 열거형을 문자열로 캐싱해두는 딕셔너리
    //    /// </summary>
    //    private static Dictionary<TTag, string> EnumStringMap;



    //    ///======================================================================================================================================================



    //    //? 열거형 태그를 문자열로 얻는 헬퍼
    //    private string GetTagString(TTag tag)
    //    {
    //        return EnumStringMap[tag];
    //    }



    //    ///======================================================================================================================================================



    //    /// <summary>
    //    /// 단일 열거형 태그로 트윈 등록
    //    /// </summary>
    //    public void AddTween(Tween tween, TTag tag)
    //    {
    //        string tagStr = GetTagString(tag);
    //        AddTween(tween, tagStr);
    //    }



    //    /// <summary>
    //    /// 복수 열거형 태그로 트윈 등록
    //    /// </summary>
    //    public void AddTween(Tween tween, params TTag[] tags)
    //    {
    //        if (tags == null || tags.Length == 0)
    //        {
    //            base.AddTween(tween);
    //            return;
    //        }

    //        string[] strTags = new string[tags.Length];
    //        for (int i = 0; i < tags.Length; i++)
    //        {
    //            strTags[i] = GetTagString(tags[i]);
    //        }
    //        AddTween(tween, strTags);
    //    }



    //    /// <summary>
    //    /// 열거형 태그로 트윈 제거
    //    /// </summary>
    //    public void RemoveByTag(TTag tag, bool kill = true)
    //    {
    //        string tagStr = GetTagString(tag);
    //        RemoveByTag(tagStr, kill);
    //    }



    //    /// <summary>
    //    /// 여러 열거형 태그를 한 번에 받아 트윈 제거
    //    /// </summary>
    //    public void RemoveByTags(bool kill, params TTag[] tags)
    //    {
    //        if (tags == null || tags.Length == 0) return;

    //        HashSet<Tween> union = new HashSet<Tween>();
    //        for (int i = 0; i < tags.Length; i++)
    //        {
    //            string strTag = GetTagString(tags[i]);
    //            if (TagMap.TryGetValue(strTag, out var set))
    //            {
    //                foreach (var tw in set)
    //                {
    //                    union.Add(tw);
    //                }
    //            }
    //        }

    //        var copyList = new List<Tween>(union);
    //        for (int i = 0; i < copyList.Count; i++)
    //        {
    //            RemoveTween(copyList[i], kill);
    //        }
    //    }



    //    /// <summary>
    //    /// 열거형 태그로 트윈 조회
    //    /// </summary>
    //    public List<Tween> GetTweensByTag(TTag tag)
    //    {
    //        string tagStr = GetTagString(tag);
    //        return GetTweensByTag(tagStr);
    //    }



    //    ///======================================================================================================================================================
    //} 
    #endregion



    ///======================================================================================================================================================



    /// <summary>
    /// 중앙 트윈 매니저(무 OnKill, 저 GC).
    /// 외부에서 SetTimeScale(...) 호출 시에만 일괄 적용/정리하고,
    /// Add/Remove 시에도 기회가 되면 죽은 항목을 증분 정리해 불필요한 배열 확장을 억제합니다.
    /// </summary>
    public sealed class TweenScaleManager
    {
        ///======================================================================================================================================================



        public TweenScaleManager(int capacity = 0)
        {
            _list = new List<Tween>(capacity);
            _index = new Dictionary<Tween, int>(capacity);
        }



        ///======================================================================================================================================================



        //. 내부 컨테이너
        [ShowInInspector, ReadOnly]
        private readonly List<Tween> _list; //. 순회/덮어쓰기 대상
        [ShowInInspector, ReadOnly]
        private readonly Dictionary<Tween, int> _index; //. 스왑-제거용 인덱스 맵

        //. 증분 정리용 커서/예산
        [ShowInInspector, ReadOnly]
        private int _sweepCursor = 0;
        [ShowInInspector, ReadOnly]
        private const int PRUNE_STEP_BUDGET = 16; //. Add 시 최대 확인 슬롯 수(필요 시 조절)

        /// <summary>매니저가 덮어쓸 timeScale</summary>
        [ShowInInspector, ReadOnly]
        public float TimeScale { get; private set; } = 1f;

        public int Count => _list.Count;



        ///======================================================================================================================================================



        /// <summary>
        /// 트윈 등록. SetUpdate(true) 강제 → Unity TimeScale과 완전 분리,
        /// 현재 매니저 스케일을 즉시 1회 적용.
        /// </summary>
        public T Add<T>(T tween) where T : Tween
        {
            if (tween == null) return null;

            //! 중복 방지
            if (_index.ContainsKey(tween)) return tween;

            //. 필요 시, 배열 확장 직전에 소액 예산으로 증분 정리
            //! 새 요소 추가로 Capacity를 넘길 것 같다면 먼저 좀 비운다
            if (_list.Count >= _list.Capacity)
            {
                PruneDeadStep(PRUNE_STEP_BUDGET);  //? 죽은 슬롯을 조금이라도 회수
            }

            //. 등록
            int idx = _list.Count;
            _list.Add(tween);
            _index[tween] = idx;

            //. Unity 글로벌 타임스케일 영향 제거
            tween.SetUpdate(true);

            //. 현재 스케일 덮어쓰기
            tween.timeScale = TimeScale;

            return tween;
        }



        /// <summary>
        /// 트윈 제거(옵션: Kill). 스왑-제거로 O(1) 처리.
        /// </summary>
        public bool Remove(Tween tween, bool kill = false)
        {
            if (tween == null) return false;
            if (!_index.TryGetValue(tween, out int idx)) return false;

            //. 선택적으로 Kill
            if (kill && tween.active) tween.Kill();

            //. 스왑-제거
            SwapRemoveAt(idx, tween);
            return true;
        }



        /// <summary>
        /// 모든 트윈 비우기(옵션: Kill)
        /// </summary>
        public void Clear(bool kill = true)
        {
            if (kill)
            {
                for (int i = 0; i < _list.Count; i++)
                {
                    var t = _list[i];
                    if (t != null && t.active) t.Kill();
                }
            }
            _list.Clear();
            _index.Clear();
            _sweepCursor = 0;
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 외부에서 TimeScale이 변경될 때만 호출.
        /// 살아있는 항목에 덮어쓰고, 죽은 항목은 뒤에서부터 정리.
        /// </summary>
        public void SetTimeScale(float newTimeScale, bool forceApply = false)
        {
            if (newTimeScale < 0f) newTimeScale = 0f;

            //! 동일 값이면 스킵(불필요 순회 방지)
            if (!forceApply && Mathf.Approximately(TimeScale, newTimeScale))
                return;

            TimeScale = newTimeScale;

            //. 뒤에서부터: 죽은 항목 정리 + 살아있는 항목 덮어쓰기
            for (int i = _list.Count - 1; i >= 0; i--)
            {
                var t = _list[i];

                //! null 또는 !active → 제거
                if (t == null || !t.active)
                {
                    SwapRemoveAt(i, t);
                    continue;
                }

                //! 정책: 매니저 스케일로 덮어쓰기(이중 곱 방지)
                t.timeScale = TimeScale;
            }

            //. 증분 커서는 리스트 변동에 맞춰 안전하게 보정
            if (_sweepCursor > _list.Count) _sweepCursor = _list.Count;
        }



        /// <summary>
        /// 스케일 변경 없이 죽은 항목만 “증분” 정리하고 싶을 때 사용.
        /// steps 만큼 슬롯을 훑으면서 죽은 항목을 제거합니다.
        /// </summary>
        public void PruneDeadStep(int steps = PRUNE_STEP_BUDGET)
        {
            //. 빈 리스트면 바로 종료
            if (_list.Count == 0) { _sweepCursor = 0; return; }

            for (int n = 0; n < steps && _list.Count > 0; n++)
            {
                if (_sweepCursor >= _list.Count) _sweepCursor = 0;

                var t = _list[_sweepCursor];

                if (t == null || !t.active)
                {
                    SwapRemoveAt(_sweepCursor, t);  //! 제거 시 현재 인덱스에는 “마지막 항목”이 와서 커서는 그대로 검사
                                                    //? 다음 루프에서 같은 인덱스를 다시 검사하므로 _sweepCursor++ 하지 않음
                }
                else
                {
                    _sweepCursor++;                 //. 살아있으면 다음 슬롯으로
                }
            }
        }



        /// <summary>
        /// 용량 조정
        /// </summary>
        /// <param name="capacity"></param>
        public void SetCapacity(int capacity)
        {
            _list.Capacity = capacity;
            _index.EnsureCapacity(capacity);
        }



        ///======================================================================================================================================================



        //! 스왑-제거 핵심: 리스트의 마지막 요소를 끌어와 덮어쓰고, 맵 업데이트 후 꼬리를 RemoveAt
        private void SwapRemoveAt(int idx, Tween victim)
        {
            int last = _list.Count - 1;

            //. 꼬리 요소가 자기 자신이면 그냥 pop
            if (idx == last)
            {
                _list.RemoveAt(last);
            }
            else
            {
                var tail = _list[last];
                _list[idx] = tail;               //? 꼬리를 앞으로 당김
                _index[tail] = idx;              //! 인덱스 맵 갱신
                _list.RemoveAt(last);
            }

            if (victim != null) _index.Remove(victim);
        }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// 체이닝 등록 헬퍼
    /// </summary>
    public static class TweenScaleManagerExtensions
    {
        public static T ManageBy<T>(this T tween, TweenScaleManager mgr) where T : Tween
        {
            //. 등록과 동시에 SetUpdate(true) + 현 스케일 적용
            return mgr?.Add(tween);
        }
    }



    ///======================================================================================================================================================
}
