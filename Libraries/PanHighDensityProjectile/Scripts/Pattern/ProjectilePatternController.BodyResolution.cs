using UnityEngine;

namespace Pan.HighDensityProjectile
{

    public sealed partial class ProjectilePatternController
    {
        private int ResolveConfiguredCapacity()
        {
            int patternCapacity = patternProfile != null ? patternProfile.MaxProjectilesPerBurst : 0;
            int bodyCapacity = projectileBody != null && !(projectileBody is ManagedProjectileBody) ? projectileBody.Capacity : 0;
            return Mathf.Max(1, Mathf.Max(Mathf.Max(initialCapacity, patternCapacity), bodyCapacity));
        }



        private void ResolveProjectileBody()
        {
            if (projectileBody != null)
            {
                return;
            }

            if (projectileBodySource != null)
            {
                projectileBody = projectileBodySource as IPanProjectileBody;
                projectileManager = projectileBodySource as ManagedProjectileBody;
                resolvedProjectileBodySource = null;
                if (projectileBody != null)
                {
                    return;
                }
            }

            if (bodyBackendMode == ProjectileBodyBackendMode.ManagedProjectileBody && TryResolveExistingProjectileBodyComponent())
            {
                return;
            }

            if (bodyBackendMode == ProjectileBodyBackendMode.ExternalRigidbody2D)
            {
                Debug.LogWarning("[ProjectilePatternController] ExternalRigidbody2D는 consumer 프로젝트의 일반 물리 오브젝트 조립 전용 mode입니다. PatternController backend로 자동 생성하지 않습니다.");
                return;
            }

            if (bodyBackendMode == ProjectileBodyBackendMode.NativeProjectileBody)
            {
                NativeProjectileBody nativeBody = GetComponent<NativeProjectileBody>();
                if (nativeBody == null)
                {
                    nativeBody = gameObject.AddComponent<NativeProjectileBody>();
                }

                projectileBody = nativeBody;
                resolvedProjectileBodySource = nativeBody;
                projectileManager = null;
                return;
            }

            if (bodyBackendMode == ProjectileBodyBackendMode.UnityPhysicsPooled)
            {
                Debug.LogWarning("[ProjectilePatternController] UnityPhysicsPooled는 consumer 프로젝트의 legacy/validation backend입니다. PanHighDensity 패키지 controller는 자동 생성하지 않습니다.");
                return;
            }

            if (projectileManager == null)
            {
                projectileManager = GetComponent<ManagedProjectileBody>();
            }

            if (projectileManager == null)
            {
                projectileManager = gameObject.AddComponent<ManagedProjectileBody>();
            }

            projectileBody = projectileManager;
            resolvedProjectileBodySource = projectileManager;
        }



        /// <summary>
        /// source가 비어 있어도 같은 GameObject나 부모 계층에 이미 붙어 있는 body가 하나뿐이면 그 body를 재사용합니다.
        /// custom/native/ECS body를 parent root에 두고 child controller를 붙이는 prefab 구성에서도 별도 사용법을 만들지 않기 위한 얇은 자동 연결 경로입니다.
        /// </summary>
        private bool TryResolveExistingProjectileBodyComponent()
        {
            if (!ProjectileBodyResolver.TryResolveSingleBodyInSelfOrParents(
            transform,
            this,
            bodyLookupBuffer,
            out MonoBehaviour resolvedSource,
            out IPanProjectileBody resolvedBody,
            out _))
            {
                return false;
            }

            projectileBody = resolvedBody;
            resolvedProjectileBodySource = resolvedSource;
            projectileManager = resolvedSource as ManagedProjectileBody;
            return true;
        }



        /// <summary>
        /// backend mode 전환 뒤 이전 자동 body가 Update에서 계속 simulation하지 않도록 같은 GameObject의 비활성 concrete backend를 정리합니다.
        /// </summary>
        private void DisableInactiveConcreteBackends(
        ManagedProjectileBody activeManager,
        NativeProjectileBody activeNativeBody)
        {
            ManagedProjectileBody localManager = GetComponent<ManagedProjectileBody>();
            if (localManager != null && localManager != activeManager)
            {
                localManager.SimulateOnUpdate = false;
                localManager.ClearAll();
            }

            NativeProjectileBody localNativeBody = GetComponent<NativeProjectileBody>();
            if (localNativeBody != null && localNativeBody != activeNativeBody)
            {
                localNativeBody.SimulateOnUpdate = false;
                localNativeBody.ClearAll();
            }
        }



        private ProjectileRenderMode ResolveEffectiveRenderMode()
        {
            if (!createRenderProxy || renderMode == ProjectileRenderMode.ExternalObjectPool)
            {
                return ProjectileRenderMode.None;
            }

            return renderMode;
        }



        private void ConfigureRenderProxy(int configuredCapacity)
        {
            ProjectileRenderMode effectiveRenderMode = ResolveEffectiveRenderMode();
            if (renderProxy != null)
            {
                renderProxy.ScaleMode = renderScaleMode;
                renderProxy.FixedWorldScale = fixedRenderWorldScale;
                bool useInstanced = effectiveRenderMode == ProjectileRenderMode.InstancedSprite;
                renderProxy.enabled = useInstanced;
                if (useInstanced)
                {
                    renderProxy.Initialize(projectileBody, configuredCapacity);
                }
            }

            if (spriteRenderProxy != null)
            {
                spriteRenderProxy.ScaleMode = renderScaleMode;
                spriteRenderProxy.FixedWorldScale = fixedRenderWorldScale;
                bool useSpritePool = effectiveRenderMode == ProjectileRenderMode.PooledSpriteRenderer;
                spriteRenderProxy.enabled = useSpritePool;
                if (useSpritePool)
                {
                    spriteRenderProxy.Initialize(projectileBody, configuredCapacity);
                }
            }
        }
    }
}
