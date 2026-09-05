using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Pan.HighDensityProjectile;



namespace Pan.HighDensityProjectile.Tests
{
    public sealed class ProjectileInstancedSpritePixelPlayModeTests
    {
        [UnityTest]
        public IEnumerator ProjectileInstancedSpriteRenderProxy_DrawsNativeBurstTanPixelsToRenderTexture()
        {
            GameObject cameraObject = new GameObject("ProjectileInstancedSpritePixelCamera");
            GameObject projectileRoot = new GameObject("ProjectileInstancedSpritePixelRoot");
            RenderTexture renderTexture = null;
            Texture2D spriteTexture = null;
            Texture2D readbackTexture = null;
            Sprite sprite = null;

            try
            {
                renderTexture = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32)
                {
                    name = "ProjectileInstancedSpritePixelRenderTexture",
                };
                renderTexture.Create();

                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.orthographic = true;
                camera.orthographicSize = 2f;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 100f;
                camera.transform.position = new Vector3(0f, 0f, -10f);
                camera.cullingMask = 1 << 0;
                camera.targetTexture = renderTexture;

                NativeProjectileBody body = projectileRoot.AddComponent<NativeProjectileBody>();
                ProjectileInstancedSpriteRenderProxy renderProxy =
                    projectileRoot.AddComponent<ProjectileInstancedSpriteRenderProxy>();
                body.Initialize(4, 4);
                body.SimulateOnUpdate = false;
                renderProxy.Initialize(body, 4);
                renderProxy.RenderOnLateUpdate = true;
                renderProxy.RenderDuringCameraRendering = true;
                renderProxy.RenderLayer = 0;
                renderProxy.ScaleMode = ProjectileRenderScaleMode.FixedWorldScale;
                renderProxy.FixedWorldScale = 1.2f;
                renderProxy.RenderZOffset = 0f;

                spriteTexture = CreateSolidTexture("ProjectileInstancedSpritePixelProbeTexture", 16, 16, new Color(1f, 0f, 1f, 1f));
                sprite = Sprite.Create(
                    spriteTexture,
                    new Rect(0f, 0f, spriteTexture.width, spriteTexture.height),
                    new Vector2(0.5f, 0.5f),
                    16f);
                sprite.name = "ProjectileInstancedSpritePixelProbeSprite";
                renderProxy.Sprite = sprite;

                ProjectileSpawnRequest spawnData = new ProjectileSpawnRequest
                {
                    Source = body,
                    Position = Vector3.zero,
                    Velocity = Vector3.zero,
                    Radius = 0.25f,
                    Lifetime = 1f,
                    Damage = 1f,
                    TeamId = 1,
                    HitLayers = 0,
                    Use2D = true,
                };
                body.Spawn(in spawnData);

                yield return null;
                yield return new WaitForEndOfFrame();

                readbackTexture = new Texture2D(9, 9, TextureFormat.RGBA32, false);
                RenderTexture previousActive = RenderTexture.active;
                RenderTexture.active = renderTexture;
                readbackTexture.ReadPixels(new Rect(60, 60, 9, 9), 0, 0);
                readbackTexture.Apply(false);
                RenderTexture.active = previousActive;

                float maxBrightness = 0f;
                Color brightestPixel = Color.black;
                for (int y = 0; y < readbackTexture.height; y++)
                {
                    for (int x = 0; x < readbackTexture.width; x++)
                    {
                        Color pixel = readbackTexture.GetPixel(x, y);
                        float brightness = pixel.r + pixel.g + pixel.b;
                        if (brightness > maxBrightness)
                        {
                            maxBrightness = brightness;
                            brightestPixel = pixel;
                        }
                    }
                }

                WritePixelValidationReport(renderProxy, maxBrightness, brightestPixel);

                Assert.AreEqual(1, body.ActiveCount);
                Assert.Greater(renderProxy.LastSubmittedInstanceCount, 0);
                Assert.Greater(renderProxy.LastDrawCallCount, 0);
                Assert.Greater(renderProxy.LastCameraVisibleCount, 0);
                Assert.AreEqual("Pan/HighDensityProjectile/NativeBurstTanInstanced", renderProxy.LastMaterialShaderName);
                Assert.Greater(maxBrightness, 0.5f);
                Assert.Greater(brightestPixel.a, 0.5f);
            }
            finally
            {
                RenderTexture.active = null;

                if (renderTexture != null)
                {
                    renderTexture.Release();
                    Object.Destroy(renderTexture);
                }

                if (sprite != null)
                {
                    Object.Destroy(sprite);
                }

                if (spriteTexture != null)
                {
                    Object.Destroy(spriteTexture);
                }

                if (readbackTexture != null)
                {
                    Object.Destroy(readbackTexture);
                }

                Object.Destroy(projectileRoot);
                Object.Destroy(cameraObject);
            }
        }



        [UnityTest]
        public IEnumerator ProjectileInstancedSpriteRenderProxy_DefaultAutomaticRender_DrawsToTwoGameCameras()
        {
            GameObject firstCameraObject = new GameObject("ProjectileInstancedSpritePixelGameCameraA");
            GameObject secondCameraObject = new GameObject("ProjectileInstancedSpritePixelGameCameraB");
            GameObject projectileRoot = new GameObject("ProjectileInstancedSpritePixelMultiCameraRoot");
            RenderTexture firstRenderTexture = null;
            RenderTexture secondRenderTexture = null;
            Texture2D spriteTexture = null;
            Sprite sprite = null;

            try
            {
                firstRenderTexture = CreateRenderTexture("ProjectileInstancedSpritePixelCameraA");
                secondRenderTexture = CreateRenderTexture("ProjectileInstancedSpritePixelCameraB");
                ConfigureRenderTextureCamera(firstCameraObject, firstRenderTexture, 0f);
                ConfigureRenderTextureCamera(secondCameraObject, secondRenderTexture, 1f);

                NativeProjectileBody body = projectileRoot.AddComponent<NativeProjectileBody>();
                ProjectileInstancedSpriteRenderProxy renderProxy =
                    projectileRoot.AddComponent<ProjectileInstancedSpriteRenderProxy>();
                body.Initialize(4, 4);
                body.SimulateOnUpdate = false;
                renderProxy.Initialize(body, 4);
                renderProxy.RenderOnLateUpdate = true;
                renderProxy.RenderDuringCameraRendering = false;
                renderProxy.RenderLayer = 0;
                renderProxy.ScaleMode = ProjectileRenderScaleMode.FixedWorldScale;
                renderProxy.FixedWorldScale = 1.2f;
                renderProxy.RenderZOffset = 0f;

                spriteTexture = CreateSolidTexture("ProjectileInstancedSpritePixelMultiCameraTexture", 16, 16, new Color(0f, 1f, 1f, 1f));
                sprite = Sprite.Create(
                    spriteTexture,
                    new Rect(0f, 0f, spriteTexture.width, spriteTexture.height),
                    new Vector2(0.5f, 0.5f),
                    16f);
                sprite.name = "ProjectileInstancedSpritePixelMultiCameraSprite";
                renderProxy.Sprite = sprite;

                ProjectileSpawnRequest spawnData = new ProjectileSpawnRequest
                {
                    Source = body,
                    Position = Vector3.zero,
                    Velocity = Vector3.zero,
                    Radius = 0.25f,
                    Lifetime = 1f,
                    Damage = 1f,
                    TeamId = 1,
                    HitLayers = 0,
                    Use2D = true,
                };
                body.Spawn(in spawnData);

                yield return null;
                yield return new WaitForEndOfFrame();

                BrightnessSample firstSample = ReadBrightnessSample(firstRenderTexture);
                BrightnessSample secondSample = ReadBrightnessSample(secondRenderTexture);
                WriteMultiCameraValidationReport(renderProxy, firstSample, secondSample);

                Assert.AreEqual(1, body.ActiveCount);
                Assert.Greater(renderProxy.LastAutomaticSubmittedInstanceCount, 0);
                Assert.Greater(renderProxy.LastDrawCallCount, 0);
                Assert.GreaterOrEqual(renderProxy.LastGameCameraVisibleCount, 2);
                Assert.Greater(firstSample.MaxBrightness, 0.5f);
                Assert.Greater(secondSample.MaxBrightness, 0.5f);
                Assert.Greater(firstSample.BrightestPixel.a, 0.5f);
                Assert.Greater(secondSample.BrightestPixel.a, 0.5f);
            }
            finally
            {
                RenderTexture.active = null;
                ReleaseRenderTexture(firstRenderTexture);
                ReleaseRenderTexture(secondRenderTexture);

                if (sprite != null)
                {
                    Object.Destroy(sprite);
                }

                if (spriteTexture != null)
                {
                    Object.Destroy(spriteTexture);
                }

                Object.Destroy(projectileRoot);
                Object.Destroy(firstCameraObject);
                Object.Destroy(secondCameraObject);
            }
        }



        [UnityTest]
        public IEnumerator ProjectileInstancedSpriteRenderProxy_SplitsRenderMeshInstancedBatchesOverUnityLimit()
        {
            GameObject cameraObject = new GameObject("ProjectileInstancedSpriteBatchSplitCamera");
            GameObject projectileRoot = new GameObject("ProjectileInstancedSpriteBatchSplitRoot");
            RenderTexture renderTexture = null;
            Texture2D spriteTexture = null;
            Sprite sprite = null;

            try
            {
                int projectileCount = ProjectileInstancedSpriteRenderProxy.MaxInstancesPerBatch + 1;
                renderTexture = CreateRenderTexture("ProjectileInstancedSpriteBatchSplitRenderTexture");
                Camera camera = ConfigureRenderTextureCamera(cameraObject, renderTexture, 0f);

                NativeProjectileBody body = projectileRoot.AddComponent<NativeProjectileBody>();
                ProjectileInstancedSpriteRenderProxy renderProxy =
                    projectileRoot.AddComponent<ProjectileInstancedSpriteRenderProxy>();
                body.Initialize(projectileCount, 4);
                body.SimulateOnUpdate = false;
                renderProxy.Initialize(body, projectileCount);
                renderProxy.RenderOnLateUpdate = false;
                renderProxy.RenderDuringCameraRendering = false;
                renderProxy.RenderLayer = 0;
                renderProxy.ScaleMode = ProjectileRenderScaleMode.FixedWorldScale;
                renderProxy.FixedWorldScale = 0.05f;
                renderProxy.RenderZOffset = 0f;

                spriteTexture = CreateSolidTexture("ProjectileInstancedSpriteBatchSplitTexture", 4, 4, Color.white);
                sprite = Sprite.Create(
                    spriteTexture,
                    new Rect(0f, 0f, spriteTexture.width, spriteTexture.height),
                    new Vector2(0.5f, 0.5f),
                    4f);
                renderProxy.Sprite = sprite;

                ProjectileSpawnRequest spawnData = new ProjectileSpawnRequest
                {
                    Source = body,
                    Position = Vector3.zero,
                    Velocity = Vector3.zero,
                    Radius = 0.025f,
                    Lifetime = 1f,
                    Damage = 1f,
                    TeamId = 1,
                    HitLayers = 0,
                    Use2D = true,
                };

                for (int i = 0; i < projectileCount; i++)
                {
                    body.Spawn(in spawnData);
                }

                int rendered = renderProxy.RenderNow(camera);

                Assert.AreEqual(projectileCount, body.ActiveCount);
                Assert.AreEqual(projectileCount, rendered);
                Assert.AreEqual(2, renderProxy.BatchCount);
                Assert.AreEqual(2, renderProxy.LastDrawCallCount);
                yield return null;
            }
            finally
            {
                RenderTexture.active = null;
                ReleaseRenderTexture(renderTexture);

                if (sprite != null)
                {
                    Object.Destroy(sprite);
                }

                if (spriteTexture != null)
                {
                    Object.Destroy(spriteTexture);
                }

                Object.Destroy(projectileRoot);
                Object.Destroy(cameraObject);
            }
        }



        private static Texture2D CreateSolidTexture(string textureName, int width, int height, Color color)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = textureName,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply(false);
            return texture;
        }



        private static RenderTexture CreateRenderTexture(string textureName)
        {
            RenderTexture renderTexture = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32)
            {
                name = textureName,
            };
            renderTexture.Create();
            return renderTexture;
        }



        private static Camera ConfigureRenderTextureCamera(GameObject cameraObject, RenderTexture renderTexture, float depth)
        {
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.orthographicSize = 2f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.cullingMask = 1 << 0;
            camera.targetTexture = renderTexture;
            camera.depth = depth;
            return camera;
        }



        private static void ReleaseRenderTexture(RenderTexture renderTexture)
        {
            if (renderTexture == null)
            {
                return;
            }

            renderTexture.Release();
            Object.Destroy(renderTexture);
        }



        private static BrightnessSample ReadBrightnessSample(RenderTexture renderTexture)
        {
            Texture2D readbackTexture = new Texture2D(9, 9, TextureFormat.RGBA32, false);
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                RenderTexture.active = renderTexture;
                readbackTexture.ReadPixels(new Rect(60, 60, 9, 9), 0, 0);
                readbackTexture.Apply(false);

                float maxBrightness = 0f;
                Color brightestPixel = Color.black;
                for (int y = 0; y < readbackTexture.height; y++)
                {
                    for (int x = 0; x < readbackTexture.width; x++)
                    {
                        Color pixel = readbackTexture.GetPixel(x, y);
                        float brightness = pixel.r + pixel.g + pixel.b;
                        if (brightness > maxBrightness)
                        {
                            maxBrightness = brightness;
                            brightestPixel = pixel;
                        }
                    }
                }

                return new BrightnessSample(maxBrightness, brightestPixel);
            }
            finally
            {
                RenderTexture.active = previousActive;
                Object.Destroy(readbackTexture);
            }
        }



        private static void WritePixelValidationReport(
            ProjectileInstancedSpriteRenderProxy renderProxy,
            float maxBrightness,
            Color brightestPixel)
        {
            string directory = Path.Combine(Application.temporaryCachePath, "ProjectileValidationReports");
            Directory.CreateDirectory(directory);
            string reportPath = Path.Combine(directory, "NativeBurstTanPixelRenderTextureValidation.txt");
            File.WriteAllText(
                reportPath,
                $"maxBrightness={maxBrightness}\n" +
                $"brightestPixel={brightestPixel}\n" +
                $"submittedInstances={renderProxy.LastSubmittedInstanceCount}\n" +
                $"drawCalls={renderProxy.LastDrawCallCount}\n" +
                $"cameraVisibleCount={renderProxy.LastCameraVisibleCount}\n" +
                $"cameraCanRenderLayer={renderProxy.LastCameraCanRenderLayer}\n" +
                $"worldBounds={renderProxy.LastWorldBounds}\n" +
                $"viewportBounds={renderProxy.LastViewportBounds}\n" +
                $"materialShader={renderProxy.LastMaterialShaderName}\n" +
                $"mainTexture={renderProxy.LastMainTextureName}\n");
        }



        private static void WriteMultiCameraValidationReport(
            ProjectileInstancedSpriteRenderProxy renderProxy,
            BrightnessSample firstSample,
            BrightnessSample secondSample)
        {
            string directory = Path.Combine(Application.temporaryCachePath, "ProjectileValidationReports");
            Directory.CreateDirectory(directory);
            string reportPath = Path.Combine(directory, "NativeBurstTanMultiCameraRenderTextureValidation.txt");
            File.WriteAllText(
                reportPath,
                $"firstMaxBrightness={firstSample.MaxBrightness}\n" +
                $"firstBrightestPixel={firstSample.BrightestPixel}\n" +
                $"secondMaxBrightness={secondSample.MaxBrightness}\n" +
                $"secondBrightestPixel={secondSample.BrightestPixel}\n" +
                $"automaticSubmittedInstances={renderProxy.LastAutomaticSubmittedInstanceCount}\n" +
                $"submittedInstances={renderProxy.LastSubmittedInstanceCount}\n" +
                $"drawCalls={renderProxy.LastDrawCallCount}\n" +
                $"gameCameraVisibleCount={renderProxy.LastGameCameraVisibleCount}\n" +
                $"sceneViewCameraVisibleCount={renderProxy.LastSceneViewCameraVisibleCount}\n" +
                $"gameCameraCanRenderLayer={renderProxy.LastGameCameraCanRenderLayer}\n" +
                $"sceneViewCameraCanRenderLayer={renderProxy.LastSceneViewCameraCanRenderLayer}\n" +
                $"worldBounds={renderProxy.LastWorldBounds}\n" +
                $"materialShader={renderProxy.LastMaterialShaderName}\n" +
                $"mainTexture={renderProxy.LastMainTextureName}\n");
        }



        private readonly struct BrightnessSample
        {
            public BrightnessSample(float maxBrightness, Color brightestPixel)
            {
                MaxBrightness = maxBrightness;
                BrightestPixel = brightestPixel;
            }

            public float MaxBrightness { get; }

            public Color BrightestPixel { get; }
        }
    }
}
