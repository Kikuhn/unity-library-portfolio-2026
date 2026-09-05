using UnityEngine;
namespace Pan.HighDensityProjectile
{

    public sealed partial class ProjectileInstancedSpriteRenderProxy
    {
        public int RenderNow()
        {
            return RenderNow(null, false);
        }



        public int RenderNow(Camera renderCamera)
        {
            return RenderNow(renderCamera, false);
        }



        private int RenderAutomatic(Camera renderCamera)
        {
            return RenderNow(renderCamera, true);
        }



        private int RenderNow(Camera renderCamera, bool automaticRender)
        {
            int renderCount = BuildRenderData(renderCamera);
            if (renderCount == 0)
            {
                if (automaticRender)
                {
                    lastAutomaticSubmittedInstanceCount = 0;
                }

                return 0;
            }

            Sprite resolvedSprite = ResolveBaseSprite();
            Material resolvedMaterial = ResolveMaterial();
            if (resolvedMaterial == null || quadMesh == null)
            {
                lastDrawCallCount = 0;
                if (automaticRender)
                {
                    lastAutomaticSubmittedInstanceCount = renderCount;
                }

                return renderCount;
            }

            lastSubmittedInstanceCount = renderCount;
            if (automaticRender)
            {
                lastAutomaticSubmittedInstanceCount = renderCount;
            }

            UpdateMaterialDiagnostics(resolvedMaterial);
            renderCallCount = 0;
            propertyBlock.SetColor("_Color", color);
            if (resolvedSprite != null && resolvedSprite.texture != null)
            {
                propertyBlock.SetTexture("_MainTex", resolvedSprite.texture);
            }

            if (HasFrameSpriteCandidates())
            {
                RenderGroupedFrames(resolvedMaterial, renderLayer, renderCount, resolvedSprite, renderCamera);
            }
            else
            {
                DrawInstancedBatches(resolvedMaterial, renderLayer, quadMesh, matrices, renderCount, renderCamera);
            }

            batchCount = renderCallCount;
            lastDrawCallCount = renderCallCount;
            return renderCount;
        }



        public int BuildRenderData()
        {
            return BuildRenderData(null);
        }



        public int BuildRenderData(Camera diagnosticCamera)
        {
            EnsureInitialized();
            if (projectileBody == null)
            {
                ClearRenderData();
                return 0;
            }

            int activeCount = projectileBody.ActiveCount;
            EnsureCapacity(activeCount);

            int previousVisibleCount = visibleCount;
            int renderCount = projectileBody.CopySnapshots(snapshotBuffer);
            Sprite resolvedSprite = ResolveBaseSprite();
            ApplySprite(resolvedSprite);
            ResetVisibilityDiagnostics();

            for (int i = 0; i < renderCount; i++)
            {
                ProjectileSnapshot snapshot = snapshotBuffer[i];
                float worldScale = ResolveWorldScale(in snapshot);
                Vector3 renderPosition = snapshot.Kinematic.Position;
                renderPosition.z += renderZOffset;
                matrices[i] = Matrix4x4.TRS(renderPosition, Quaternion.identity, new Vector3(worldScale, worldScale, 1f));
                AccumulateRenderDiagnostics(renderPosition, worldScale, i == 0);
            }

            if (previousVisibleCount > renderCount)
            {
                ClearMatrixRange(renderCount, previousVisibleCount);
            }

            visibleCount = renderCount;
            batchCount = CalculateBatchCount(renderCount, resolvedSprite);
            renderCallCount = 0;
            lastSubmittedInstanceCount = renderCount;
            UpdateCameraVisibilityDiagnostics(renderCount, diagnosticCamera);
            return renderCount;
        }



        private float ResolveWorldScale(in ProjectileSnapshot snapshot)
        {
            return scaleMode == ProjectileRenderScaleMode.FixedWorldScale
            ? Mathf.Max(0f, fixedWorldScale)
            : Mathf.Max(0f, snapshot.Kinematic.Radius * radiusToScale);
        }
    }
}
