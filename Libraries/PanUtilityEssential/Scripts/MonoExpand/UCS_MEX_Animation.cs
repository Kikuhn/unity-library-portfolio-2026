using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Events;
using DG.Tweening;
using Pan.Util;



//? [Monobehaviour Expand] 애니메이션 확장 관련이 정리된 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    ///<summary>
    ///스크립트 컴포넌트를 추가해서 사용하는 애니메이션 콜백 매니저, SendMessage라 굉장히 비효율적임
    ///</summary>
    [Obsolete("새로운 방식으로 구현 권장")]
    public class CustomAnimatorCallbackManager
    {
        ///======================================================================================================================================================



        //! CustomAnimatorCallback 컴포넌트가 자동으로 추가됨



        public CustomAnimatorCallbackManager(Animator animator)
        {
            Animator = animator;

            ClipCount = Animator.runtimeAnimatorController.animationClips.Length;

            Clips = new AnimationClip[ClipCount];

            EventCallBacks_Start = new(ClipCount);
            EventCallBacks_End = new(ClipCount);

            AnimatorOverrideController overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);

            for (int i = 0; i < ClipCount; i++)
            {
                var oriClip = animator.runtimeAnimatorController.animationClips[i];
                AnimationClip clip = UnityEngine.Object.Instantiate(oriClip); //? 에셋번들로 불러온 AnimationClip으로 인스턴스하면 버그남

                clip.name = oriClip.name; //? 에셋번들에서 작동안함 ㅅㅂ 캐싱해놔도

                Clips[i] = clip;

                string clipName = oriClip.name;

                AddCallBack_Start(animator, clip, () => { Clip_Start(clipName); });
                AddCallBack_End(animator, clip, () => { Clip_End(clipName); });

                overrideController[clipName] = clip;

                EventCallBacks_Start.Add(clipName, new HashSet<Action>());
                EventCallBacks_End.Add(clipName, new HashSet<Action>());
            }

            animator.runtimeAnimatorController = overrideController;
        }



        ///======================================================================================================================================================



        public readonly Animator Animator;

        public readonly int ClipCount;

        public readonly AnimationClip[] Clips;

        public event Action<AnimationClip> Start_Event;
        public event Action<AnimationClip> End_Event;

        private Dictionary<string, HashSet<Action>> EventCallBacks_Start;
        private Dictionary<string, HashSet<Action>> EventCallBacks_End;



        ///======================================================================================================================================================



        public void AddCallBack_Start(string clipName, Action callBack)
        {
            AddCallBack(EventCallBacks_Start, clipName, callBack);
        }



        public void AddCallBack_End(string clipName, Action callBack)
        {
            AddCallBack(EventCallBacks_End, clipName, callBack);
        }



        private void AddCallBack(Dictionary<string, HashSet<Action>> dic, string clipName, Action callBack)
        {
            if (dic.TryGetValue(clipName, out var callbacks))
            {
                callbacks.Add(callBack);
            }
        }



        ///======================================================================================================================================================



        public void RemoveCallBack_Start(string clipName, Action callBack)
        {
            RemoveCallBack(EventCallBacks_Start, clipName, callBack);
        }



        public void RemoveCallBack_Start_End(string clipName, Action callBack)
        {
            RemoveCallBack(EventCallBacks_End, clipName, callBack);
        }



        private void RemoveCallBack(Dictionary<string, HashSet<Action>> dic, string clipName, Action callBack)
        {
            if (callBack == null) { return; }

            if (dic.TryGetValue(clipName, out var callbacks))
            {
                callbacks.Remove(callBack);
            }
        }



        public void ClearAll_CallBacks()
        {
            foreach (var callback in EventCallBacks_Start)
            {
                callback.Value.Clear();
            }

            foreach (var callback in EventCallBacks_End)
            {
                callback.Value.Clear();
            }
        }



        ///======================================================================================================================================================



        public void Play(string clipName)
        {
            if (!Animator.enabled) { Animator.enabled = true; }
            Animator.Play(clipName);
        }



        public void Enable()
        {
            //Animator.enabled = true;
        }



        public void Disable()
        {
            Animator.enabled = false;
            ClearAll_CallBacks();
        }



        ///======================================================================================================================================================



        private void Clip_Start(string clipName)
        {
            if (EventCallBacks_Start.TryGetValue(clipName, out var callbacks))
            {
                foreach (var callback in callbacks)
                {
                    callback.Invoke();
                }
            }
        }



        private void Clip_End(string clipName)
        {
            if (EventCallBacks_End.TryGetValue(clipName, out var callbacks))
            {
                foreach (var callback in callbacks)
                {
                    callback.Invoke();
                }
            }
        }



        ///======================================================================================================================================================



        //? 애니메이션 콜백 확장기능 추가하기 (컴포넌트 스크립트 강제추가)



        private static void AddCallBack_Start(Animator animator, AnimationClip animationClip, Action callBack)
        {
            AnimationClip_BindCallBack(animationClip, animator.gameObject, 0f, callBack);
        }



        private static void AddCallBack_End(Animator animator, AnimationClip animationClip, Action callBack)
        {
            AnimationClip_BindCallBack(animationClip, animator.gameObject, animationClip.length, callBack);
        }



        private static void Add_CallBack(Animator animator, AnimationClip animationClip, float clipTimePosition, Action callBack)
        {
            AnimationClip_BindCallBack(animationClip, animator.gameObject, clipTimePosition, callBack);
        }



        private static void AnimationClip_BindCallBack(AnimationClip clip, GameObject targetObject, float clipTimePosition, Action callBack)
        {
            if (!targetObject.TryGetComponent(out CustomAnimatorCallback animatorCallback))
            {
                animatorCallback = targetObject.AddComponent<CustomAnimatorCallback>();
            }

            AnimationEvent animationEvent = new AnimationEvent();

            animatorCallback.AddEvent(clip.name, clipTimePosition, callBack);

            Add_AnimationClip_AnimationEvent(clip, CustomAnimatorCallback.EVENT_METHOD_NAME, clipTimePosition, clipTimePosition);

            return;
        }



        private static void Add_AnimationClip_AnimationEvent(AnimationClip clip, string methodName, float floatParameter, float time)
        {
            var clipAnimationEvents = clip.events;

            var animationEvent =
                Array.Find(clipAnimationEvents,
                e => e.functionName == methodName && e.floatParameter == floatParameter && e.time == time);

            if (animationEvent == null)
            {
                animationEvent = new AnimationEvent
                {
                    functionName = methodName,
                    stringParameter = clip.name + CustomAnimatorCallback.PARSE_CHAR + floatParameter,
                    time = time
                };
                clip.AddEvent(animationEvent);
            }
        }



        ///======================================================================================================================================================
    }



    [Obsolete("새로운 방식으로 구현 권장")]
    public abstract class BaseExtendCustomAnimator
    {

        /// <summary>같은 RuntimeAnimatorController 쓰는 것들 캐싱해두기</summary>
        public class AnimationClipCacheManager
        {
            ///======================================================================================================================================================



            public AnimationClipCacheManager()
            {
                AnimatorController_Dictionary = new Dictionary<RuntimeAnimatorController, AnimatorClipsDictionary>();
            }



            ///======================================================================================================================================================



            public class AnimatorClipsDictionary
            {
                public AnimatorClipsDictionary(RuntimeAnimatorController runTimeAnimatorController, int plusReferenceCount = 1)
                {
                    AnimationClips = new Dictionary<string, AnimationClip>(runTimeAnimatorController.animationClips.Length);

                    foreach (var clip in runTimeAnimatorController.animationClips)
                    {
                        AnimationClips.Add(clip.name, clip);
                    }

                    ReferenceCount += plusReferenceCount;

                }

                public readonly Dictionary<string, AnimationClip> AnimationClips;

                public int ReferenceCount = 0;
            }



            ///======================================================================================================================================================



            private readonly Dictionary<RuntimeAnimatorController, AnimatorClipsDictionary> AnimatorController_Dictionary;



            public int Count => AnimatorController_Dictionary.Count;



            ///======================================================================================================================================================



            public bool TryGet(RuntimeAnimatorController acon, out Dictionary<string, AnimationClip> resultDictionary)
            {
                if (AnimatorController_Dictionary.TryGetValue(acon, out var result))
                {
                    resultDictionary = result.AnimationClips;
                    return true;
                }


                resultDictionary = null;
                return false;
            }



            public bool TryGets(RuntimeAnimatorController acon, string clipName, out AnimationClip resultClip)
            {
                if (AnimatorController_Dictionary.TryGetValue(acon, out var resultDictionary)
                    && resultDictionary.AnimationClips.TryGetValue(clipName, out resultClip))
                {
                    return true;
                }

                resultClip = null;
                return false;
            }



            public Dictionary<string, AnimationClip> Get(RuntimeAnimatorController acon)
            {
                if (AnimatorController_Dictionary.TryGetValue(acon, out var resultDictionary))
                {
                    return resultDictionary.AnimationClips;
                }

                return null;
            }



            public AnimationClip Gets(RuntimeAnimatorController acon, string clipName)
            {
                if (AnimatorController_Dictionary.TryGetValue(acon, out var resultDictionary)
                    && resultDictionary.AnimationClips.TryGetValue(clipName, out var resultClip))
                {
                    return resultClip;
                }

                return null;
            }



            public void Add_AnimatorController(RuntimeAnimatorController key)
            {
                if (AnimatorController_Dictionary.TryGetValue(key, out var result)) { result.ReferenceCount++; return; }

                var animatorClipsDictionary = new AnimatorClipsDictionary(key);
                AnimatorController_Dictionary.Add(key, animatorClipsDictionary);
            }



            public bool Remove_AnimatorController(RuntimeAnimatorController key)
            {
                if (AnimatorController_Dictionary.TryGetValue(key, out var result))
                {
                    result.ReferenceCount--;

                    if (result.ReferenceCount <= 0) { AnimatorController_Dictionary.Remove(key); }

                    return true;
                }

                return false;
            }



            public void Clear_AnimatorController()
            {
                foreach (var controller in AnimatorController_Dictionary)
                {
                    controller.Value.ReferenceCount = 0;
                    controller.Value.AnimationClips.Clear();
                }

                AnimatorController_Dictionary.Clear();
            }



            ///======================================================================================================================================================
        }


        public static AnimationClipCacheManager StaticManager = new AnimationClipCacheManager();
    }



    [Obsolete("새로운 방식으로 구현 권장")]
    public class ExtendCustomAnimator_One : BaseExtendCustomAnimator
    {
        ///======================================================================================================================================================



        ///<summary>Time관련 Func 받아올때, 안전하게 람다식으로 받아오자</summary>
        public ExtendCustomAnimator_One(Animator animator, RuntimeAnimatorController runtimeAnimatorController, Func<float> timeScaleFunc, Func<float> deltaTimeFunc)
        {
            ThisAnimator = animator;
            ThisRuntimeAnimatorController = runtimeAnimatorController;

            TimeUpdater = new TimeUpdateEvent();
            TimeScaleFunc = timeScaleFunc;
            DeltaTimeFunc = deltaTimeFunc;

            StaticManager.Add_AnimatorController(ThisRuntimeAnimatorController);
        }



        ///======================================================================================================================================================


        //? 애니메이터, 런타임 애니메이터 컨트롤러

        private readonly Animator ThisAnimator;
        private readonly RuntimeAnimatorController ThisRuntimeAnimatorController;



        ///======================================================================================================================================================



        //? 애니메이션 실행시, 같이 재생되는 타임 업데이터

        private readonly TimeUpdateEvent TimeUpdater;



        //? 타임스케일, 델타타임 Func

        public readonly Func<float> TimeScaleFunc = null;
        public readonly Func<float> DeltaTimeFunc = null;



        //? 애니메이션 추가속도 (기본값 1)

        private float BonusSpeed = 1f;



        //? 일시정지용 애니메이션 속도 (재생시 적용, 변경불가)

        private float PauseSpeed = 1f;



        //? 일시정지 여부
        public bool IsPause
        {
            get => isPause;

            private set
            {
                //? 재생중에 일시정지 되었다면
                if (value == true && isPause == false)
                {
                    TimeUpdater.PauseWorking();
                    //ThisAnimator.speed = 0f;
                    PauseSpeed = 0f;
                    isPause = value;
                    return;
                }

                //? 일시정지중에 다시 재생되었다면
                if (value == false && isPause == true)
                {
                    TimeUpdater.ReStartWorking();
                    //ThisAnimator.speed = 1f;
                    PauseSpeed = 1f;
                    isPause = value;
                    return;
                }

            }
        }
        private bool isPause = false;



        ///======================================================================================================================================================



        ///<summary>애니메이션 재생</summary>
        ///<param name="overlapEndCallBack">활성화시, 기존에 애니메이션이 재생중이라면, 종료 콜백을 실행시킨뒤 종료시킨다</param>
        public void Play(string clipName, Action endCallBack, bool overlapEndCallBack = true, float? bonusSpeed = null)
        {
            if (!ThisAnimator.enabled) { ThisAnimator.enabled = true; }
            ThisAnimator.Play(clipName);



            if (TimeUpdater.IsWorking) { TimeUpdater.ShutDown(overlapEndCallBack); }



            if (bonusSpeed.HasValue) { BonusSpeed = bonusSpeed.Value; }



            TimeUpdater.EndTime = StaticManager.Gets(ThisRuntimeAnimatorController, clipName).length * (1f / bonusSpeed.Value);
            TimeUpdater.Enable(DeltaTimeFunc, endCallBack, WorkingUpdate);
            TimeUpdater.StartWorking();
        }



        //? 애니메이션 재생시, TimeUpdater에 같이 실행되는 함수 (타임, 일시정지 을 적용 하기 위한 속도계산)

        private void WorkingUpdate(TimeUpdateEvent timeUpdater, float dt)
        {
            ThisAnimator.speed = TimeScaleFunc.Invoke() * BonusSpeed * PauseSpeed;
        }



        /// <summary>애니메이션 일시정지 시키기</summary>
        public void Pause()
        {
            IsPause = true;
        }



        /// <summary>일시정지된 애니메이션 재개 시키기</summary>
        public void Replay()
        {
            if (IsPause)
            {
                IsPause = false;
            }
        }



        /// <summary>애니메이션 정지 시키기</summary>
        public void Stop(bool endCallBack = true)
        {
            ThisAnimator.StopPlayback();
            TimeUpdater.ShutDown(endCallBack);
        }



        ///======================================================================================================================================================



        public void Enable()
        {
            Reset();
            ThisAnimator.enabled = true;
        }



        public void Disable()
        {
            Reset();
            ThisAnimator.enabled = false;
        }



        public void Reset()
        {
            Stop();
            BonusSpeed = 1f;
            PauseSpeed = 1f;
        }



        public void OnDestroy()
        {
            Reset();
            StaticManager?.Remove_AnimatorController(ThisRuntimeAnimatorController);
        }



        ///======================================================================================================================================================
    }



    [Obsolete("새로운 방식으로 구현 권장")]
    public class CustomSpriteAnimator
    {
        ///======================================================================================================================================================



        public CustomSpriteAnimator(List<Sprite> sprites, Func<SpriteRenderer> spriteRenderer)
        {
            TimeUpdater = new TimeUpdateEvent();
            TimeUpdater.IsWorkingOnce = false;
            Sprites = sprites;
            Renderer = spriteRenderer;
        }



        ///======================================================================================================================================================



        public float TickRate = 0.01f;



        private readonly TimeUpdateEvent TimeUpdater;



        public readonly Func<SpriteRenderer> Renderer;



        public readonly List<Sprite> Sprites;



        public int Counting = 0;



        public Func<float> DeltaTimeFunc;



        ///======================================================================================================================================================



        private void KeepGoing()
        {
            Renderer.Invoke().sprite = Sprites[Counting];
            Counting++;
        }



        private void End()
        {
            Renderer.Invoke().sprite = null;
            Counting = 0;
        }



        public void Start()
        {
            Counting = 0;
            TimeUpdater.EndTime = TickRate;
            TimeUpdater.EndEvent += End;
            TimeUpdater.Enable(DeltaTimeFunc, KeepGoing); ;



            TimeUpdater.IsLoop = true;
            TimeUpdater.LoopCondition = () => Sprites.Count > Counting;



            TimeUpdater.StartWorking();

        }




        public void ShutDown()
        {
            TimeUpdater.ShutDown(false);
            End();
            TimeUpdater.Disable(true);
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================
}