using System;
using Pan.HighDensityElement;
using UnityEngine;



namespace Pan.HighDensityElement.Physics2DBridge
{
    /// <summary>
    /// 하이브리드 쿼리가 조회할 물리 공간을 선택합니다.
    /// </summary>
    [Flags]
    public enum HybridPhysicsQueryWorldMask : byte
    {
        None = 0,
        Legacy = 1 << 0,
        Core = 1 << 1,
        Both = Legacy | Core
    }



    /// <summary>
    /// 하이브리드 쿼리 결과가 가리키는 원본 대상 종류입니다.
    /// </summary>
    public enum HybridPhysicsHitKind2D : byte
    {
        LegacyCollider = 0,
        CoreBridgeTarget = 1,
        CoreElement = 2
    }



    /// <summary>
    /// Legacy Physics2D와 PhysicsCore2D에 공통으로 적용할 query 조건입니다.
    /// </summary>
    public struct HybridPhysicsQueryFilter2D
    {
        /// <summary>
        /// 양쪽 물리 공간에 동일하게 적용할 Unity layer mask입니다.
        /// </summary>
        public LayerMask LayerMask;

        /// <summary>
        /// layer collision matrix를 평가할 query source layer입니다. 음수면 source layer 검사를 생략합니다.
        /// </summary>
        public int SourceLayer;

        /// <summary>
        /// Unity Physics2D layer collision matrix를 양쪽 결과 필터에 반영할지 결정합니다.
        /// </summary>
        public bool RespectLayerCollisionMatrix;

        /// <summary>
        /// trigger 형상을 결과에 포함할지 결정합니다.
        /// </summary>
        public bool IncludeTriggers;

        /// <summary>
        /// 조회할 물리 공간을 선택합니다.
        /// </summary>
        public HybridPhysicsQueryWorldMask Worlds;



        /// <summary>
        /// 두 물리 공간과 모든 layer, trigger를 조회하는 기본 필터입니다.
        /// </summary>
        public static HybridPhysicsQueryFilter2D Default => new HybridPhysicsQueryFilter2D
        {
            LayerMask = Physics2D.AllLayers,
            SourceLayer = -1,
            RespectLayerCollisionMatrix = true,
            IncludeTriggers = true,
            Worlds = HybridPhysicsQueryWorldMask.Both
        };
    }



    /// <summary>
    /// Legacy Collider 또는 PhysicsCore Element를 같은 형식으로 표현하는 값형식 query 결과입니다.
    /// </summary>
    public readonly struct HybridPhysicsHit2D
    {
        public HybridPhysicsHit2D(
            HybridPhysicsHitKind2D kind,
            Collider2D collider,
            Component owner,
            ElementKey element,
            Vector2 point,
            Vector2 normal,
            float distance,
            float fraction,
            int layer,
            bool isTrigger,
            int stableTargetId)
        {
            Kind = kind;
            Collider = collider;
            Owner = owner;
            Element = element;
            Point = point;
            Normal = normal;
            Distance = distance;
            Fraction = fraction;
            Layer = layer;
            IsTrigger = isTrigger;
            StableTargetId = stableTargetId;
        }



        public HybridPhysicsHitKind2D Kind { get; }
        public Collider2D Collider { get; }
        public Component Owner { get; }
        public GameObject GameObject => Owner != null ? Owner.gameObject : Collider != null ? Collider.gameObject : null;
        public ElementKey Element { get; }
        public Vector2 Point { get; }
        public Vector2 Normal { get; }
        public float Distance { get; }
        public float Fraction { get; }
        public int Layer { get; }
        public bool IsTrigger { get; }
        public int StableTargetId { get; }
    }
}
