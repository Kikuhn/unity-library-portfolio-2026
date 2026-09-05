using System;
using UnityEngine;



namespace Pan.Tan.Normal
{
    /// <summary>
    /// consumer의 Legacy Physics2D object factory에 전달할 backend 설정입니다.
    /// </summary>
    [Serializable]
    public readonly struct NormalTanBackendOptions
    {
        private readonly TanCcdPolicy defaultCcdPolicy;



        /// <summary>
        /// NormalTan factory 설정과 runtime 기본 CCD 정책을 만듭니다.
        /// </summary>
        public NormalTanBackendOptions(
            int factoryKey = 0,
            CollisionDetectionMode2D collisionDetectionMode = CollisionDetectionMode2D.Continuous,
            bool isTrigger = true,
            TanCcdPolicy defaultCcdPolicy = default)
        {
            FactoryKey = factoryKey;
            CollisionDetectionMode = collisionDetectionMode;
            IsTrigger = isTrigger;
            this.defaultCcdPolicy = NormalizeDefaultCcdPolicy(in defaultCcdPolicy);
        }



        public int FactoryKey { get; }
        public CollisionDetectionMode2D CollisionDetectionMode { get; }
        public bool IsTrigger { get; }

        /// <summary>
        /// Tan별 정책이 RuntimeDefault일 때 NormalTan runtime이 적용할 CCD 정책입니다.
        /// </summary>
        /// <remarks>
        /// default 구조체와 RuntimeDefault 입력은 모두 Auto, 비율 0.5로 정규화됩니다.
        /// </remarks>
        public TanCcdPolicy DefaultCcdPolicy => NormalizeDefaultCcdPolicy(in defaultCcdPolicy);



        private static TanCcdPolicy NormalizeDefaultCcdPolicy(in TanCcdPolicy policy)
        {
            return policy.Mode == TanCcdMode.RuntimeDefault
                ? TanCcdPolicy.Auto
                : policy;
        }
    }



    /// <summary>
    /// package가 구체 project pool을 참조하지 않고 factory에 전달하는 생성 context입니다.
    /// </summary>
    public readonly struct NormalTanSpawnContext
    {
        public NormalTanSpawnContext(
            in TanKey key,
            in TanSpawnRequest request,
            in NormalTanBackendOptions options)
        {
            Key = key;
            Request = request;
            Options = options;
        }



        public TanKey Key { get; }
        public TanSpawnRequest Request { get; }
        public NormalTanBackendOptions Options { get; }
    }



    /// <summary>
    /// consumer factory가 대여한 NormalTan GameObject의 표준 Physics2D 표면입니다.
    /// </summary>
    public readonly struct NormalTanInstance
    {
        public NormalTanInstance(
            in TanTargetHandle target,
            GameObject root,
            Rigidbody2D body,
            Collider2D collider)
        {
            Target = target;
            Root = root;
            Body = body;
            Collider = collider;
        }



        public TanTargetHandle Target { get; }
        public GameObject Root { get; }
        public Rigidbody2D Body { get; }
        public Collider2D Collider { get; }
        public bool IsValid => Root != null && Body != null && Collider != null;
    }



    /// <summary>
    /// NormalTan의 GameObject 생성·대여·반환을 consumer project에 위임하는 계약입니다.
    /// </summary>
    public interface INormalTanFactory
    {
        bool TryRent(in NormalTanSpawnContext context, out NormalTanInstance instance);
        void Return(in NormalTanInstance instance);
    }
}
