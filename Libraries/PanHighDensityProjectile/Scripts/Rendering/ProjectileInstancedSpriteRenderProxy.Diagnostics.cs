using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace Pan.HighDensityProjectile
{

    public sealed partial class ProjectileInstancedSpriteRenderProxy
    {
        private void ResetVisibilityDiagnostics()
        {
            lastCameraVisibleCount = 0;
            lastGameCameraVisibleCount = 0;
            lastSceneViewCameraVisibleCount = 0;
            lastCameraCanRenderLayer = false;
            lastGameCameraCanRenderLayer = false;
            lastSceneViewCameraCanRenderLayer = false;
            lastWorldBounds = default;
            lastViewportBounds = default;
        }



        private void AccumulateRenderDiagnostics(Vector3 renderPosition, float worldScale, bool first)
        {
            Vector3 size = new Vector3(
            Mathf.Max(0.01f, worldScale),
            Mathf.Max(0.01f, worldScale),
            Mathf.Max(0.01f, worldScale));
            Bounds instanceBounds = new Bounds(renderPosition, size);
            if (first)
            {
                lastWorldBounds = instanceBounds;
                return;
            }

            lastWorldBounds.Encapsulate(instanceBounds);
        }



        private void UpdateCameraVisibilityDiagnostics(int renderCount, Camera cameraOverride)
        {
            if (renderCount <= 0)
            {
                return;
            }

            if (cameraOverride != null)
            {
                UpdateVisibilityDiagnosticsForCamera(renderCount, cameraOverride, true, true);
                return;
            }

            Camera primaryCamera = ResolveDiagnosticCamera();
            if (primaryCamera != null)
            {
                UpdateVisibilityDiagnosticsForCamera(renderCount, primaryCamera, true, false);
            }

            UpdateAllCameraVisibilityDiagnostics(renderCount);
            #if UNITY_EDITOR
            UpdateSceneViewVisibilityDiagnostics(renderCount);
            #endif
        }



        private void UpdateAllCameraVisibilityDiagnostics(int renderCount)
        {
            int cameraCount = Camera.allCamerasCount;
            if (cameraCount <= 0)
            {
                return;
            }

            if (cameraBuffer == null || cameraBuffer.Length < cameraCount)
            {
                cameraBuffer = new Camera[cameraCount];
            }

            Camera.GetAllCameras(cameraBuffer);
            for (int i = 0; i < cameraCount; i++)
            {
                UpdateVisibilityDiagnosticsForCamera(renderCount, cameraBuffer[i], false, true);
            }
        }



        #if UNITY_EDITOR
        private void UpdateSceneViewVisibilityDiagnostics(int renderCount)
        {
            foreach (SceneView sceneView in SceneView.sceneViews)
            {
                UpdateVisibilityDiagnosticsForCamera(renderCount, sceneView != null ? sceneView.camera : null, false, true);
            }
        }
        #endif



        private void UpdateVisibilityDiagnosticsForCamera(int renderCount, Camera camera, bool updatePrimary, bool updateCategory)
        {
            if (camera == null || !camera.isActiveAndEnabled)
            {
                return;
            }

            bool canRenderLayer = CameraCanRenderLayer(camera, renderLayer);
            if (updatePrimary)
            {
                lastCameraCanRenderLayer = canRenderLayer;
            }

            if (updateCategory && camera.cameraType == CameraType.Game)
            {
                lastGameCameraCanRenderLayer |= canRenderLayer;
            }
            else if (updateCategory && camera.cameraType == CameraType.SceneView)
            {
                lastSceneViewCameraCanRenderLayer |= canRenderLayer;
            }

            if (!canRenderLayer)
            {
                return;
            }

            int visibleCountForCamera = 0;
            bool hasViewportBounds = false;
            Vector2 viewportMin = Vector2.zero;
            Vector2 viewportMax = Vector2.zero;
            for (int i = 0; i < renderCount; i++)
            {
                Vector3 position = matrices[i].GetColumn(3);
                Vector3 viewportPosition = camera.WorldToViewportPoint(position);
                if (!IsViewportPointVisible(camera, viewportPosition))
                {
                    continue;
                }

                if (updatePrimary)
                {
                    lastCameraVisibleCount++;
                }

                visibleCountForCamera++;
                Vector2 viewportPoint = new Vector2(viewportPosition.x, viewportPosition.y);
                if (!hasViewportBounds)
                {
                    viewportMin = viewportPoint;
                    viewportMax = viewportPoint;
                    hasViewportBounds = true;
                }
                else
                {
                    viewportMin = Vector2.Min(viewportMin, viewportPoint);
                    viewportMax = Vector2.Max(viewportMax, viewportPoint);
                }
            }

            if (hasViewportBounds)
            {
                Rect viewportBounds = Rect.MinMaxRect(viewportMin.x, viewportMin.y, viewportMax.x, viewportMax.y);
                if (updatePrimary)
                {
                    lastViewportBounds = viewportBounds;
                }
            }

            if (updateCategory && camera.cameraType == CameraType.Game)
            {
                lastGameCameraVisibleCount += visibleCountForCamera;
            }
            else if (updateCategory && camera.cameraType == CameraType.SceneView)
            {
                lastSceneViewCameraVisibleCount += visibleCountForCamera;
            }
        }



        private void UpdateMaterialDiagnostics(Material resolvedMaterial)
        {
            lastMaterialShaderName = resolvedMaterial != null && resolvedMaterial.shader != null
            ? resolvedMaterial.shader.name
            : string.Empty;
            Texture mainTexture = resolvedMaterial != null ? resolvedMaterial.mainTexture : null;
            lastMainTextureName = mainTexture != null ? mainTexture.name : string.Empty;
        }



        private Camera ResolveDiagnosticCamera()
        {
            if (Camera.main != null)
            {
                return Camera.main;
            }

            int cameraCount = Camera.allCamerasCount;
            if (cameraCount <= 0)
            {
                return null;
            }

            if (cameraBuffer == null || cameraBuffer.Length < cameraCount)
            {
                cameraBuffer = new Camera[cameraCount];
            }

            Camera.GetAllCameras(cameraBuffer);
            for (int i = 0; i < cameraCount; i++)
            {
                Camera candidate = cameraBuffer[i];
                if (candidate != null && candidate.isActiveAndEnabled)
                {
                    return candidate;
                }
            }

            return null;
        }



        private static bool CameraCanRenderLayer(Camera camera, int layer)
        {
            if (camera == null || layer < 0 || layer > 31)
            {
                return false;
            }

            return (camera.cullingMask & (1 << layer)) != 0;
        }



        private static bool CanUseCameraForAutomaticRender(Camera camera)
        {
            if (camera == null || !camera.isActiveAndEnabled)
            {
                return false;
            }

            return camera.cameraType == CameraType.Game ||
            camera.cameraType == CameraType.SceneView ||
            camera.cameraType == CameraType.VR;
        }



        private static bool IsViewportPointVisible(Camera camera, Vector3 viewportPosition)
        {
            return viewportPosition.x >= 0f &&
            viewportPosition.x <= 1f &&
            viewportPosition.y >= 0f &&
            viewportPosition.y <= 1f &&
            viewportPosition.z >= camera.nearClipPlane &&
            viewportPosition.z <= camera.farClipPlane;
        }
    }
}
