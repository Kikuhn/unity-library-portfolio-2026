using System.Collections;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;



namespace Pan.HighDensityElement.Tests
{
    public sealed class ElementSpriteRendererPlayModeTests
    {
        [UnityTest]
        public IEnumerator RenderSpriteInstanced_DrawsInstanceColorWithoutGameObjectRenderer()
        {
            if (!SystemInfo.supportsInstancing) { Assert.Ignore("현재 graphics device가 GPU instancing을 지원하지 않습니다."); }

            GameObject cameraObject = new GameObject("HighDensityElementPixelCamera");
            RenderTexture renderTexture = null;
            Texture2D spriteTexture = null;
            Texture2D readbackTexture = null;
            Sprite sprite = null;
            Material material = null;

            try
            {
                renderTexture = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32);
                renderTexture.Create();
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.orthographic = true;
                camera.orthographicSize = 2f;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 100f;
                camera.transform.position = new Vector3(0f, 0f, -10f);
                camera.targetTexture = renderTexture;

                spriteTexture = CreateSolidTexture(Color.white);
                sprite = Sprite.Create(
                    spriteTexture,
                    new Rect(0f, 0f, spriteTexture.width, spriteTexture.height),
                    new Vector2(0.5f, 0.5f),
                    spriteTexture.width);
                Shader shader = Shader.Find("Pan/HighDensityElement/Sprite-Unlit-Instanced");
                Assert.IsNotNull(shader);
                material = new Material(shader) { enableInstancing = true };

                using var world = new ElementWorld(4);
                using var renderer = new ElementSpriteRenderer(
                    CreateRegistry(sprite, material, 1),
                    initialCapacity: 4,
                    maximumInstancesPerBatch: 511,
                    spatialChunkSize: 16f);
                ElementCompiledArchetype archetype = CreateVisualArchetype(1);
                ElementSpawnBuilder builder = ElementSpawnBuilder.From(archetype)
                    .WithPose(float2.zero, new float2(1.5f))
                    .WithColor(new Color32(255, 0, 255, 255));
                world.Spawn(in builder);

                void RenderForCamera(ScriptableRenderContext _, Camera renderingCamera)
                {
                    if (renderingCamera == camera) { renderer.Render(world, camera); }
                }

                RenderPipelineManager.beginCameraRendering += RenderForCamera;
                try
                {
                    yield return null;
                    yield return new WaitForEndOfFrame();
                }
                finally
                {
                    RenderPipelineManager.beginCameraRendering -= RenderForCamera;
                }

                readbackTexture = new Texture2D(9, 9, TextureFormat.RGBA32, false);
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = renderTexture;
                readbackTexture.ReadPixels(new Rect(60, 60, 9, 9), 0, 0);
                readbackTexture.Apply(false);
                RenderTexture.active = previous;

                Color brightest = FindBrightest(readbackTexture);
                Assert.AreEqual(1, renderer.LastInstanceCount);
                Assert.AreEqual(1, renderer.LastBatchCount);
                Assert.AreEqual(0, renderer.LastFallbackSubmissionCount);
                Assert.Greater(brightest.r, 0.7f);
                Assert.Less(brightest.g, 0.25f);
                Assert.Greater(brightest.b, 0.7f);
                Assert.Greater(brightest.a, 0.7f);
            }
            finally
            {
                RenderTexture.active = null;
                if (renderTexture != null) { renderTexture.Release(); }
                Object.Destroy(renderTexture);
                Object.Destroy(readbackTexture);
                Object.Destroy(sprite);
                Object.Destroy(spriteTexture);
                Object.Destroy(material);
                Object.Destroy(cameraObject);
            }
        }



        [UnityTest]
        public IEnumerator RenderSpriteInstanced_SplitsGroupsAtMeasuredConservativeLimit()
        {
            if (!SystemInfo.supportsInstancing) { Assert.Ignore("현재 graphics device가 GPU instancing을 지원하지 않습니다."); }

            Texture2D texture = CreateSolidTexture(Color.white);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);
            Shader shader = Shader.Find("Pan/HighDensityElement/Sprite-Unlit-Instanced");
            Assert.IsNotNull(shader);
            Material material = new Material(shader) { enableInstancing = true };

            try
            {
                using var world = new ElementWorld(1100);
                using var renderer = new ElementSpriteRenderer(
                    CreateRegistry(sprite, material, 1),
                    initialCapacity: 1100,
                    maximumInstancesPerBatch: ElementSpriteRenderer.ConservativeDefaultInstancesPerBatch,
                    spatialChunkSize: 1000f);
                ElementCompiledArchetype archetype = CreateVisualArchetype(1);
                for (int i = 0; i < 1023; i++)
                {
                    float x = (i % 32) * 0.01f;
                    float y = (i / 32) * 0.01f;
                    ElementSpawnBuilder builder = ElementSpawnBuilder.From(archetype)
                        .WithPose(new float2(x, y), new float2(0.1f));
                    world.Spawn(in builder);
                }

                renderer.Render(world);
                yield return null;

                Assert.AreEqual(1023, renderer.LastInstanceCount);
                Assert.AreEqual(3, renderer.LastBatchCount);
                Assert.AreEqual(0, renderer.LastFallbackSubmissionCount);
            }
            finally
            {
                Object.Destroy(material);
                Object.Destroy(sprite);
                Object.Destroy(texture);
            }
        }



        [UnityTest]
        public IEnumerator RenderSpriteInstanced_HonorsSortingVisualChangeAndCameraCulling()
        {
            if (!SystemInfo.supportsInstancing) { Assert.Ignore("현재 graphics device가 GPU instancing을 지원하지 않습니다."); }

            GameObject cameraObject = new GameObject("HighDensityElementSortingCamera");
            RenderTexture renderTexture = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32);
            Texture2D redTexture = CreateSolidTexture(Color.red);
            Texture2D blueTexture = CreateSolidTexture(Color.blue);
            Sprite redSprite = Sprite.Create(redTexture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
            Sprite blueSprite = Sprite.Create(blueTexture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
            Material material = new Material(Shader.Find("Pan/HighDensityElement/Sprite-Unlit-Instanced"))
            {
                enableInstancing = true
            };

            try
            {
                renderTexture.Create();
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.orthographic = true;
                camera.orthographicSize = 2f;
                camera.transform.position = new Vector3(0f, 0f, -10f);
                camera.targetTexture = renderTexture;

                var registry = new ElementVisualRegistry();
                registry.Register(1, new ElementVisualDefinition(redSprite, material, sortingOrder: 0));
                registry.Register(2, new ElementVisualDefinition(blueSprite, material, sortingOrder: 10));
                using var world = new ElementWorld(4);
                using var renderer = new ElementSpriteRenderer(registry, 4);
                ElementHandle red = world.Spawn(
                    ElementSpawnBuilder.From(CreateVisualArchetype(1)).WithPose(float2.zero, new float2(1.5f))).Handle;
                ElementHandle blue = world.Spawn(
                    ElementSpawnBuilder.From(CreateVisualArchetype(2)).WithPose(float2.zero, new float2(1.5f))).Handle;

                void RenderForCamera(ScriptableRenderContext _, Camera renderingCamera)
                {
                    if (renderingCamera == camera) { renderer.Render(world, camera); }
                }

                RenderPipelineManager.beginCameraRendering += RenderForCamera;
                try
                {
                    yield return null;
                    yield return new WaitForEndOfFrame();
                    Color sortedPixel = ReadCenterPixel(renderTexture);
                    Assert.Greater(sortedPixel.b, 0.7f);
                    Assert.Less(sortedPixel.r, 0.25f);

                    var setRedFrame = new SetElementVisualId(1);
                    blue.Submit(in setRedFrame);
                    world.Tick(0f);

                    yield return null;
                    yield return new WaitForEndOfFrame();
                    Color changedPixel = ReadCenterPixel(renderTexture);
                    Assert.Greater(changedPixel.r, 0.7f);
                    Assert.Less(changedPixel.b, 0.25f);

                    var moveOffscreen = new SetElementPosition2D(new float2(100f, 100f));
                    red.Submit(in moveOffscreen);
                    blue.Submit(in moveOffscreen);
                    world.Tick(0f);

                    yield return null;
                    yield return new WaitForEndOfFrame();
                    Color culledPixel = ReadCenterPixel(renderTexture);
                    Assert.Less(culledPixel.r + culledPixel.g + culledPixel.b, 0.1f);
                }
                finally
                {
                    RenderPipelineManager.beginCameraRendering -= RenderForCamera;
                }
            }
            finally
            {
                RenderTexture.active = null;
                renderTexture.Release();
                Object.Destroy(renderTexture);
                Object.Destroy(redSprite);
                Object.Destroy(blueSprite);
                Object.Destroy(redTexture);
                Object.Destroy(blueTexture);
                Object.Destroy(material);
                Object.Destroy(cameraObject);
            }
        }



        [UnityTest]
        public IEnumerator RenderSpriteInstanced_HonorsSpriteMaskInteraction()
        {
            if (!SystemInfo.supportsInstancing) { Assert.Ignore("현재 graphics device가 GPU instancing을 지원하지 않습니다."); }

            GameObject cameraObject = new GameObject("HighDensityElementMaskCamera");
            GameObject maskObject = new GameObject("HighDensityElementSpriteMask");
            RenderTexture renderTexture = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32);
            Texture2D texture = CreateSolidTexture(Color.white);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);
            Material material = new Material(Shader.Find("Pan/HighDensityElement/Sprite-Unlit-Instanced"))
            {
                enableInstancing = true
            };

            try
            {
                renderTexture.Create();
                Camera camera = ConfigurePixelCamera(cameraObject, renderTexture);
                SpriteMask mask = maskObject.AddComponent<SpriteMask>();
                mask.sprite = sprite;
                mask.alphaCutoff = 0.01f;
                mask.isCustomRangeActive = true;
                mask.frontSortingLayerID = 0;
                mask.frontSortingOrder = 10;
                mask.backSortingLayerID = 0;
                mask.backSortingOrder = -10;

                var registry = new ElementVisualRegistry();
                registry.Register(
                    1,
                    new ElementVisualDefinition(
                        sprite,
                        material,
                        sortingOrder: 0,
                        maskInteraction: SpriteMaskInteraction.VisibleInsideMask));
                using var world = new ElementWorld(4);
                using var renderer = new ElementSpriteRenderer(registry, 4);
                ElementSpawnBuilder builder = ElementSpawnBuilder.From(CreateVisualArchetype(1))
                    .WithPose(float2.zero, new float2(1.5f))
                    .WithColor(new Color32(255, 0, 255, 255));
                world.Spawn(in builder);

                void RenderForCamera(ScriptableRenderContext _, Camera renderingCamera)
                {
                    if (renderingCamera == camera) { renderer.Render(world, camera); }
                }

                RenderPipelineManager.beginCameraRendering += RenderForCamera;
                try
                {
                    yield return null;
                    yield return new WaitForEndOfFrame();
                    Color insideMask = ReadCenterPixel(renderTexture);

                    mask.enabled = false;
                    yield return null;
                    yield return new WaitForEndOfFrame();
                    Color withoutMask = ReadCenterPixel(renderTexture);

                    Assert.AreEqual(1, renderer.LastBatchCount);
                    Assert.Greater(insideMask.r, 0.7f);
                    Assert.Greater(insideMask.b, 0.7f);
                    Assert.Less(withoutMask.r + withoutMask.g + withoutMask.b, 0.1f);
                }
                finally
                {
                    RenderPipelineManager.beginCameraRendering -= RenderForCamera;
                }
            }
            finally
            {
                RenderTexture.active = null;
                renderTexture.Release();
                Object.Destroy(renderTexture);
                Object.Destroy(sprite);
                Object.Destroy(texture);
                Object.Destroy(material);
                Object.Destroy(maskObject);
                Object.Destroy(cameraObject);
            }
        }



        [UnityTest]
        public IEnumerator RenderSpriteInstanced_ReceivesUrp2DLight()
        {
            if (!SystemInfo.supportsInstancing) { Assert.Ignore("현재 graphics device가 GPU instancing을 지원하지 않습니다."); }

            Light2D[] existingLights = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var existingLightStates = new bool[existingLights.Length];
            for (int i = 0; i < existingLights.Length; i++)
            {
                existingLightStates[i] = existingLights[i].enabled;
                existingLights[i].enabled = false;
            }

            GameObject cameraObject = new GameObject("HighDensityElementLitCamera");
            GameObject lightObject = new GameObject("HighDensityElementGlobalLight");
            RenderTexture renderTexture = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32);
            Texture2D texture = CreateSolidTexture(Color.white);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            Assert.IsNotNull(shader);
            Material material = new Material(shader) { enableInstancing = true };

            try
            {
                renderTexture.Create();
                Camera camera = ConfigurePixelCamera(cameraObject, renderTexture);
                Light2D light = lightObject.AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Global;
                light.color = Color.red;
                light.intensity = 1f;
                light.overlapOperation = Light2D.OverlapOperation.Additive;
                light.enabled = false;

                using var world = new ElementWorld(4);
                using var renderer = new ElementSpriteRenderer(CreateRegistry(sprite, material, 1), 4);
                ElementSpawnBuilder builder = ElementSpawnBuilder.From(CreateVisualArchetype(1))
                    .WithPose(float2.zero, new float2(1.5f));
                world.Spawn(in builder);

                void RenderForCamera(ScriptableRenderContext _, Camera renderingCamera)
                {
                    if (renderingCamera == camera) { renderer.Render(world, camera); }
                }

                RenderPipelineManager.beginCameraRendering += RenderForCamera;
                try
                {
                    yield return null;
                    yield return new WaitForEndOfFrame();
                    Color withoutLight = ReadCenterPixel(renderTexture);

                    light.enabled = true;
                    yield return null;
                    yield return new WaitForEndOfFrame();
                    Color litPixel = ReadCenterPixel(renderTexture);

                    Assert.AreEqual(1, renderer.LastBatchCount);
                    Assert.Greater(litPixel.r, 0.6f);
                    Assert.Less(litPixel.g, 0.25f);
                    Assert.Less(litPixel.b, 0.25f);
                    float colorDifference =
                        Mathf.Abs(litPixel.r - withoutLight.r) +
                        Mathf.Abs(litPixel.g - withoutLight.g) +
                        Mathf.Abs(litPixel.b - withoutLight.b);
                    Assert.Greater(colorDifference, 0.4f);
                }
                finally
                {
                    RenderPipelineManager.beginCameraRendering -= RenderForCamera;
                }
            }
            finally
            {
                for (int i = 0; i < existingLights.Length; i++)
                {
                    if (existingLights[i] != null) { existingLights[i].enabled = existingLightStates[i]; }
                }

                RenderTexture.active = null;
                renderTexture.Release();
                Object.Destroy(renderTexture);
                Object.Destroy(sprite);
                Object.Destroy(texture);
                Object.Destroy(material);
                Object.Destroy(lightObject);
                Object.Destroy(cameraObject);
            }
        }



        private static ElementVisualRegistry CreateRegistry(Sprite sprite, Material material, int visualId)
        {
            var registry = new ElementVisualRegistry();
            registry.Register(visualId, new ElementVisualDefinition(sprite, material));
            return registry;
        }



        private static ElementCompiledArchetype CreateVisualArchetype(int visualId)
        {
            Assert.IsTrue(ElementCompiledArchetype.TryCreate(
                ElementCapabilities.SpriteVisual2D,
                0.5f,
                5f,
                visualId,
                0f,
                false,
                PhysicsCoreBodyMode.Kinematic,
                PhysicsCoreShape2D.Circle,
                out ElementCompiledArchetype archetype,
                out ElementSpawnStatus failure), failure.ToString());
            return archetype;
        }



        private static Texture2D CreateSolidTexture(Color color)
        {
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            var pixels = new Color[16 * 16];
            for (int i = 0; i < pixels.Length; i++) { pixels[i] = color; }
            texture.SetPixels(pixels);
            texture.Apply(false);
            return texture;
        }



        private static Camera ConfigurePixelCamera(GameObject cameraObject, RenderTexture renderTexture)
        {
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.orthographicSize = 2f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.targetTexture = renderTexture;
            return camera;
        }



        private static Color FindBrightest(Texture2D texture)
        {
            Color brightest = Color.black;
            float maximum = 0f;
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    Color pixel = texture.GetPixel(x, y);
                    float value = pixel.r + pixel.g + pixel.b;
                    if (value <= maximum) { continue; }
                    maximum = value;
                    brightest = pixel;
                }
            }

            return brightest;
        }



        private static Color ReadCenterPixel(RenderTexture renderTexture)
        {
            var readback = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            readback.ReadPixels(new Rect(64, 64, 1, 1), 0, 0);
            readback.Apply(false);
            RenderTexture.active = previous;
            Color pixel = readback.GetPixel(0, 0);
            Object.Destroy(readback);
            return pixel;
        }
    }
}
