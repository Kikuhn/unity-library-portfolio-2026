using UnityEngine;
using UnityEngine.Rendering;

namespace Pan.HighDensityProjectile
{

    public sealed partial class ProjectileInstancedSpriteRenderProxy
    {
        private bool HasFrameSpriteCandidates()
        {
            return frameSprites != null && frameSprites.Length > 0;
        }



        private int CalculateBatchCount(int renderCount, Sprite fallbackSprite)
        {
            if (renderCount <= 0)
            {
                return 0;
            }

            if (!HasFrameSpriteCandidates())
            {
                return Mathf.CeilToInt(renderCount / (float)MaxInstancesPerBatch);
            }

            EnsureFrameCountBuffer();
            System.Array.Clear(frameInstanceCounts, 0, frameInstanceCounts.Length);

            for (int i = 0; i < renderCount; i++)
            {
                int visualFrameIndex = ResolveAnimatedFrameIndex(in snapshotBuffer[i]);
                int groupIndex = ResolveFrameGroupIndex(visualFrameIndex, fallbackSprite);
                frameInstanceCounts[groupIndex]++;
            }

            int batches = 0;
            for (int i = 0; i < frameInstanceCounts.Length; i++)
            {
                int count = frameInstanceCounts[i];
                if (count > 0)
                {
                    batches += Mathf.CeilToInt(count / (float)MaxInstancesPerBatch);
                }
            }

            return batches;
        }



        /// <summary>
        /// 같은 atlas texture 안의 sprite frame을 mesh UV별 그룹으로 나누어 그립니다.
        /// 탄마다 renderer를 만들지 않고, frame 종류만큼 draw batch가 늘어나는 단순 경로입니다.
        /// </summary>
        private void RenderGroupedFrames(Material resolvedMaterial, int layer, int renderCount, Sprite fallbackSprite, Camera renderCamera)
        {
            EnsureFrameCapacity(renderCount);
            int groupCount = GetFrameGroupCount();
            for (int groupIndex = 0; groupIndex < groupCount; groupIndex++)
            {
                int groupMatrixCount = 0;
                for (int i = 0; i < renderCount; i++)
                {
                    int visualFrameIndex = ResolveAnimatedFrameIndex(in snapshotBuffer[i]);
                    if (ResolveFrameGroupIndex(visualFrameIndex, fallbackSprite) == groupIndex)
                    {
                        frameMatrices[groupMatrixCount] = matrices[i];
                        groupMatrixCount++;
                    }
                }

                if (groupMatrixCount == 0)
                {
                    continue;
                }

                Mesh mesh = groupIndex == 0
                ? quadMesh
                : GetFrameQuadMesh(groupIndex - 1, frameSprites[groupIndex - 1]);
                DrawInstancedBatches(resolvedMaterial, layer, mesh, frameMatrices, groupMatrixCount, renderCamera);
            }
        }



        private void DrawInstancedBatches(Material resolvedMaterial, int layer, Mesh mesh, Matrix4x4[] sourceMatrices, int renderCount, Camera renderCamera)
        {
            if (resolvedMaterial == null || mesh == null || sourceMatrices == null || renderCount <= 0)
            {
                return;
            }

            int rendered = 0;
            while (rendered < renderCount)
            {
                int currentBatchSize = Mathf.Min(MaxInstancesPerBatch, renderCount - rendered);
                RenderParams renderParams = new RenderParams(resolvedMaterial)
                {
                    camera = renderCamera,
                    layer = layer,
                    matProps = propertyBlock,
                    receiveShadows = false,
                    shadowCastingMode = ShadowCastingMode.Off,
                    worldBounds = lastWorldBounds.size == Vector3.zero
                    ? new Bounds(transform.position, Vector3.one)
                    : lastWorldBounds,
                };
                Graphics.RenderMeshInstanced(renderParams, mesh, 0, sourceMatrices, currentBatchSize, rendered);

                rendered += currentBatchSize;
                renderCallCount++;
            }
        }



        private int ResolveFrameGroupIndex(int visualFrameIndex, Sprite fallbackSprite)
        {
            if (visualFrameIndex <= 0 || frameSprites == null)
            {
                return 0;
            }

            int spriteIndex = visualFrameIndex - 1;
            if (spriteIndex < 0 || spriteIndex >= frameSprites.Length)
            {
                return 0;
            }

            Sprite frameSprite = frameSprites[spriteIndex];
            if (frameSprite == null || fallbackSprite == null || frameSprite.texture != fallbackSprite.texture)
            {
                return 0;
            }

            return spriteIndex + 1;
        }
    }
}
