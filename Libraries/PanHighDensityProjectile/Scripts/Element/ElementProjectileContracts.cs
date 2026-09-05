using System.Collections.Generic;
using Pan.HighDensityElement;
using UnityEngine;



namespace Pan.HighDensityProjectile
{
    /// <summary>
    /// Element 투사체가 PhysicsCore2D를 사용하는 실행 방식을 선택합니다.
    /// </summary>
    public enum ElementProjectileExecutionModel : byte
    {
        /// <summary>
        /// 실제 PhysicsBody 없이 이동 형상을 query 입력으로 사용하는 대량 투사체입니다.
        /// </summary>
        Query = 0,

        /// <summary>
        /// 실제 PhysicsCore2D Dynamic Body를 사용하는 특수 투사체입니다.
        /// </summary>
        Simulated = 1
    }



    /// <summary>
    /// 실제 PhysicsCore dynamic body를 사용하는 소수 투사체의 shape와 material 설정입니다.
    /// 일반 탄막은 이 옵션을 사용하지 않는 bodyless CoreProjectile이 기본입니다.
    /// </summary>
    public readonly struct SimulatedProjectileOptions2D
    {
        public SimulatedProjectileOptions2D(
            PhysicsCoreShape2D shape,
            in ElementPhysicsMaterial2D material)
        {
            Shape = shape;
            Material = material;
        }



        public PhysicsCoreShape2D Shape { get; }
        public ElementPhysicsMaterial2D Material { get; }
        public bool IsValid => Material.Density > 0f;



        public static SimulatedProjectileOptions2D Default =>
            new SimulatedProjectileOptions2D(
                PhysicsCoreShape2D.Circle,
                ElementPhysicsMaterial2D.Default);
    }



    /// <summary>
    /// Element contact fact를 Collider, bridge provider 또는 다른 Element의 gameplay receiver로 해석합니다.
    /// </summary>
    public interface IElementProjectileTargetResolver
    {
        /// <summary>
        /// 하나의 contact fact를 gameplay identity와 중복 없는 receiver 후보로 복원합니다.
        /// </summary>
        /// <param name="fact">PhysicsCore job이 기록한 값형식 contact fact입니다.</param>
        /// <param name="receivers">호출자가 재사용하는 목록입니다. 구현은 먼저 비운 뒤 receiver를 추가해야 합니다.</param>
        /// <param name="targetComponent">source hierarchy 제외와 GameObject identity에 사용할 대표 component입니다.</param>
        /// <param name="collider">legacy Collider2D에서 온 contact이면 원본 collider입니다.</param>
        /// <returns>target identity를 유효하게 복원했으면 true입니다. receiver가 없는 지형도 true일 수 있습니다.</returns>
        bool TryResolveTargets(
            in ElementFact fact,
            List<IProjectileHitTarget> receivers,
            out Component targetComponent,
            out Collider2D collider);
    }



    /// <summary>
    /// 기존 projectile 계약과 범용 Element identity를 함께 노출합니다.
    /// </summary>
    public readonly struct ElementProjectileHandle
    {
        public ElementProjectileHandle(ProjectileHandle projectile, ElementHandle element)
        {
            Projectile = projectile;
            Element = element;
        }



        public ProjectileHandle Projectile { get; }
        public ElementHandle Element { get; }
        public bool IsAlive => Projectile.IsAlive && Element.IsAlive;



        /// <summary>
        /// 지정한 고정 스텝 수 동안 이 투사체의 엄격 CCD를 강제로 활성화합니다.
        /// </summary>
        public bool RequestStrictCcd(int fixedStepCount)
        {
            return Element.RequestStrictCcd(fixedStepCount);
        }



        /// <summary>
        /// 현재 위치 변경을 순간이동으로 표시해 이전 위치와의 이동 경로 충돌을 생략합니다.
        /// </summary>
        public bool MarkTeleported()
        {
            return Element.MarkTeleported();
        }
    }
}
