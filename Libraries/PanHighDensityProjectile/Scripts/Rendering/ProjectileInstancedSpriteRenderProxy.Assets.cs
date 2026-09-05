using UnityEngine;
namespace Pan.HighDensityProjectile
{

    public sealed partial class ProjectileInstancedSpriteRenderProxy
    {
        /// <summary>
        /// 풀링으로 비활성화되거나 body가 비었을 때, 이전 프레임의 instancing 상태를 즉시 비웁니다.
        /// </summary>
        public void ClearRenderData()
        {
            if (matrices != null && visibleCount > 0)
            {
                ClearMatrixRange(0, visibleCount);
            }

            visibleCount = 0;
            batchCount = 0;
            renderCallCount = 0;
            lastSubmittedInstanceCount = 0;
            lastAutomaticSubmittedInstanceCount = 0;
            lastDrawCallCount = 0;
            ResetVisibilityDiagnostics();
        }



        private void EnsureInitialized()
        {
            EnsureProjectileBody();

            if (snapshotBuffer == null)
            {
                int capacity = Mathf.Max(1, initialMatrixCapacity);
                snapshotBuffer = new ProjectileSnapshot[capacity];
                matrices = new Matrix4x4[capacity];
                propertyBlock = new MaterialPropertyBlock();
                quadMesh = CreateQuadMesh();
            }
        }



        private void EnsureProjectileBody()
        {
            if (projectileBody != null)
            {
                return;
            }

            if (ProjectileBodyResolver.TryResolveBodyInSelfOrParents(
            transform,
            this,
            projectileBodySource,
            projectileManager,
            resolvedProjectileBodySource,
            bodyLookupBuffer,
            out MonoBehaviour resolvedSource,
            out IPanProjectileBody resolvedBody))
            {
                projectileBody = resolvedBody;
                resolvedProjectileBodySource = resolvedSource == projectileBodySource || resolvedSource == projectileManager
                ? null
                : resolvedSource;
                if (projectileBodySource == null)
                {
                    projectileManager = resolvedSource as ManagedProjectileBody;
                }
            }
        }



        private void EnsureCapacity(int requiredCapacity)
        {
            if (requiredCapacity <= 0)
            {
                return;
            }

            if (matrices.Length >= requiredCapacity)
            {
                return;
            }

            int newCapacity = Mathf.Max(requiredCapacity, matrices.Length * 2);
            System.Array.Resize(ref snapshotBuffer, newCapacity);
            System.Array.Resize(ref matrices, newCapacity);
        }



        private void EnsureFrameCapacity(int requiredCapacity)
        {
            if (requiredCapacity <= 0)
            {
                return;
            }

            if (frameMatrices != null && frameMatrices.Length >= requiredCapacity)
            {
                return;
            }

            int currentCapacity = frameMatrices != null ? frameMatrices.Length : 0;
            int newCapacity = Mathf.Max(requiredCapacity, Mathf.Max(1, currentCapacity * 2));
            System.Array.Resize(ref frameMatrices, newCapacity);
        }



        private void ClearMatrixRange(int startIndex, int endIndex)
        {
            if (matrices == null)
            {
                return;
            }

            int start = Mathf.Clamp(startIndex, 0, matrices.Length);
            int count = Mathf.Clamp(endIndex, start, matrices.Length) - start;
            if (count > 0)
            {
                System.Array.Clear(matrices, start, count);
            }
        }



        private Sprite ResolveBaseSprite()
        {
            if (sprite != null)
            {
                return sprite;
            }

            Sprite firstFrameSprite = GetFirstFrameSprite();
            return firstFrameSprite != null ? firstFrameSprite : GetDefaultSprite();
        }



        private Sprite GetFirstFrameSprite()
        {
            if (frameSprites == null)
            {
                return null;
            }

            for (int i = 0; i < frameSprites.Length; i++)
            {
                if (frameSprites[i] != null)
                {
                    return frameSprites[i];
                }
            }

            return null;
        }



        private int ResolveAnimatedFrameIndex(in ProjectileSnapshot snapshot)
        {
            if (!animateFramesByLifetime ||
            frameSprites == null ||
            frameSprites.Length == 0 ||
            frameAnimationFramesPerSecond <= 0f)
            {
                return snapshot.Visual.FrameIndex;
            }

            int baseFrameIndex = snapshot.Visual.FrameIndex > 0 && snapshot.Visual.FrameIndex <= frameSprites.Length
            ? snapshot.Visual.FrameIndex
            : 1;
            int frameOffset = Mathf.FloorToInt(snapshot.Lifetime.ElapsedSeconds * frameAnimationFramesPerSecond);
            int resolvedFrameIndex = baseFrameIndex + frameOffset;
            if (loopFrameAnimation)
            {
                return ((resolvedFrameIndex - 1) % frameSprites.Length) + 1;
            }

            return Mathf.Clamp(resolvedFrameIndex, 1, frameSprites.Length);
        }



        private int GetFrameGroupCount()
        {
            return HasFrameSpriteCandidates() ? frameSprites.Length + 1 : 1;
        }



        private void EnsureFrameCountBuffer()
        {
            int groupCount = GetFrameGroupCount();
            if (frameInstanceCounts == null || frameInstanceCounts.Length != groupCount)
            {
                frameInstanceCounts = new int[groupCount];
            }
        }



        private Mesh GetFrameQuadMesh(int spriteIndex, Sprite frameSprite)
        {
            EnsureFrameMeshBuffers();
            if (spriteIndex < 0 || spriteIndex >= frameQuadMeshes.Length || frameSprite == null)
            {
                return quadMesh;
            }

            if (frameQuadMeshes[spriteIndex] == null)
            {
                frameQuadMeshes[spriteIndex] = CreateQuadMesh();
                frameQuadMeshes[spriteIndex].name = "ProjectileInstancedSpriteFrameQuad";
            }

            if (frameMeshSprites[spriteIndex] != frameSprite)
            {
                frameMeshSprites[spriteIndex] = frameSprite;
                ApplySpriteUv(frameQuadMeshes[spriteIndex], frameSprite);
            }

            return frameQuadMeshes[spriteIndex];
        }



        private void EnsureFrameMeshBuffers()
        {
            int length = frameSprites != null ? frameSprites.Length : 0;
            if (frameQuadMeshes != null && frameQuadMeshes.Length == length)
            {
                return;
            }

            DestroyFrameQuadMeshes();
            frameQuadMeshes = length > 0 ? new Mesh[length] : null;
            frameMeshSprites = length > 0 ? new Sprite[length] : null;
        }



        private void DestroyFrameQuadMeshes()
        {
            if (frameQuadMeshes == null)
            {
                frameMeshSprites = null;
                return;
            }

            for (int i = 0; i < frameQuadMeshes.Length; i++)
            {
                if (frameQuadMeshes[i] != null)
                {
                    DestroyGeneratedObject(frameQuadMeshes[i]);
                }
            }

            frameQuadMeshes = null;
            frameMeshSprites = null;
        }



        private static void DestroyGeneratedObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }



        private Material ResolveMaterial()
        {
            if (material != null)
            {
                material.enableInstancing = true;
                material.renderQueue = 3100;
                return material;
            }

            if (runtimeMaterial != null)
            {
                return runtimeMaterial;
            }

            Shader shader = ResolveDefaultInstancedSpriteShader();
            if (shader == null)
            {
                return null;
            }

            runtimeMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave,
                enableInstancing = true,
                renderQueue = 3100,
            };
            return runtimeMaterial;
        }



        private static Shader ResolveDefaultInstancedSpriteShader()
        {
            Shader shader = Shader.Find("Pan/HighDensityProjectile/NativeBurstTanInstanced");
            if (shader != null)
            {
                return shader;
            }

            shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                return shader;
            }

            shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                return shader;
            }

            return Shader.Find("Unlit/Transparent");
        }



        private void ApplySprite(Sprite resolvedSprite)
        {
            if (resolvedSprite == null || appliedSprite == resolvedSprite)
            {
                return;
            }

            appliedSprite = resolvedSprite;
            Material resolvedMaterial = ResolveMaterial();
            if (resolvedMaterial != null)
            {
                resolvedMaterial.mainTexture = resolvedSprite.texture;
            }

            if (quadMesh == null)
            {
                return;
            }

            ApplySpriteUv(quadMesh, resolvedSprite);
        }



        private static void ApplySpriteUv(Mesh mesh, Sprite resolvedSprite)
        {
            if (mesh == null || resolvedSprite == null || resolvedSprite.texture == null)
            {
                return;
            }

            Rect textureRect = resolvedSprite.textureRect;
            float textureWidth = resolvedSprite.texture.width;
            float textureHeight = resolvedSprite.texture.height;
            if (textureWidth <= 0f || textureHeight <= 0f)
            {
                return;
            }

            Vector2 uvMin = new Vector2(textureRect.xMin / textureWidth, textureRect.yMin / textureHeight);
            Vector2 uvMax = new Vector2(textureRect.xMax / textureWidth, textureRect.yMax / textureHeight);
            mesh.uv = new[]
            {
                new Vector2(uvMin.x, uvMin.y),
                new Vector2(uvMax.x, uvMin.y),
                new Vector2(uvMax.x, uvMax.y),
                new Vector2(uvMin.x, uvMax.y),
            };
        }



        private static Mesh CreateQuadMesh()
        {
            Mesh mesh = new Mesh
            {
                name = "ProjectileInstancedSpriteQuad",
                hideFlags = HideFlags.HideAndDontSave,
            };

            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }



        private static Sprite GetDefaultSprite()
        {
            if (defaultSprite != null)
            {
                return defaultSprite;
            }

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, true);

            defaultSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            defaultSprite.hideFlags = HideFlags.HideAndDontSave;
            return defaultSprite;
        }

    }
}
