using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace Pan.HighDensityProjectile
{

    /// <summary>
    /// `IPanProjectileBody` snapshot을 Unity 6000 `Graphics.RenderMeshInstanced` draw call로 제출하는 대량 탄막 renderer입니다.
    /// GameObject/Renderer를 탄마다 만들지 않고 matrix buffer와 sprite frame mesh만 재사용합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class ProjectileInstancedSpriteRenderProxy : MonoBehaviour
    {
        public const int MaxInstancesPerBatch = 1023;



        [FoldoutGroup("연결"), SerializeField, LabelText("검증용 Managed Body"), Tooltip("비워두면 같은 GameObject 또는 부모의 ManagedProjectileBody/IPanProjectileBody를 자동으로 찾습니다.")]
        private ManagedProjectileBody projectileManager;
        [FoldoutGroup("연결"), SerializeField, LabelText("Body Source"), Tooltip("ManagedProjectileBody가 아닌 IPanProjectileBody 구현체를 직접 연결할 때 사용합니다.")]
        private MonoBehaviour projectileBodySource;
        [FoldoutGroup("버퍼"), SerializeField, Min(1), LabelText("초기 Matrix Capacity")] private int initialMatrixCapacity = 512;
        [FoldoutGroup("렌더링"), SerializeField, LabelText("LateUpdate 자동 렌더")] private bool renderOnLateUpdate = true;
        [FoldoutGroup("렌더링"), SerializeField, LabelText("Camera Rendering 훅 사용")] private bool renderDuringCameraRendering;
        [FoldoutGroup("표시"), SerializeField, LabelText("기본 Sprite")] private Sprite sprite;
        [SerializeField, Tooltip("VisualFrameIndex가 1 이상일 때 사용할 sprite 목록입니다. instancing 경로에서는 같은 texture atlas의 sprite만 frame mesh로 사용하고, 다른 texture는 기본 sprite로 fallback합니다.")]
        private Sprite[] frameSprites;
        [SerializeField, Tooltip("켜면 snapshot의 경과 수명과 FPS를 기준으로 FrameSprites를 자동 진행합니다.")]
        private bool animateFramesByLifetime;
        [SerializeField, Min(0f), Tooltip("수명 기반 frame animation 속도입니다. 0이면 자동 진행하지 않습니다.")]
        private float frameAnimationFramesPerSecond = 12f;
        [SerializeField, Tooltip("켜면 마지막 frame 뒤 첫 frame으로 돌아가고, 끄면 마지막 frame에 고정됩니다.")]
        private bool loopFrameAnimation = true;
        [SerializeField] private Material material;
        [SerializeField] private Color color = Color.white;
        [SerializeField, Min(0f)] private float radiusToScale = 2f;
        [SerializeField] private ProjectileRenderScaleMode scaleMode = ProjectileRenderScaleMode.PhysicsDiameter;
        [SerializeField, Min(0f)] private float fixedWorldScale = 1f;
        [SerializeField] private float renderZOffset = -0.25f;
        [SerializeField] private int renderLayer;
        [SerializeField] private bool resetRenderDataOnDisable = true;



        private ProjectileSnapshot[] snapshotBuffer;
        private Matrix4x4[] matrices;
        private MaterialPropertyBlock propertyBlock;
        private Mesh quadMesh;
        private Mesh[] frameQuadMeshes;
        private Sprite[] frameMeshSprites;
        private Matrix4x4[] frameMatrices;
        private int[] frameInstanceCounts;
        private Material runtimeMaterial;
        private Sprite appliedSprite;
        private int visibleCount;
        private int batchCount;
        private int renderCallCount;
        private int lastSubmittedInstanceCount;
        private int lastAutomaticSubmittedInstanceCount;
        private int lastDrawCallCount;
        private int lastCameraVisibleCount;
        private int lastGameCameraVisibleCount;
        private int lastSceneViewCameraVisibleCount;
        private bool lastCameraCanRenderLayer;
        private bool lastGameCameraCanRenderLayer;
        private bool lastSceneViewCameraCanRenderLayer;
        private Bounds lastWorldBounds;
        private Rect lastViewportBounds;
        private string lastMaterialShaderName = string.Empty;
        private string lastMainTextureName = string.Empty;
        private IPanProjectileBody projectileBody;
        private MonoBehaviour resolvedProjectileBodySource;
        private readonly List<MonoBehaviour> bodyLookupBuffer = new List<MonoBehaviour>(4);
        private Camera[] cameraBuffer;
        private static Sprite defaultSprite;



        /// <summary>Inspector에서 `IPanProjectileBody` 구현 MonoBehaviour를 직접 주입할 수 있는 source입니다.</summary>
        public MonoBehaviour ProjectileBodySource
        {
            get => projectileBodySource != null
            ? projectileBodySource
            : projectileManager != null
            ? projectileManager
            : resolvedProjectileBodySource;
            set
            {
                projectileBodySource = value;
                resolvedProjectileBodySource = null;
                projectileBody = value as IPanProjectileBody;
                projectileManager = value as ManagedProjectileBody;
            }
        }

        public IPanProjectileBody ProjectileBody
        {
            get
            {
                EnsureProjectileBody();
                return projectileBody;
            }
            set
            {
                projectileBody = value;
                projectileManager = value as ManagedProjectileBody;
                projectileBodySource = value as MonoBehaviour;
                resolvedProjectileBodySource = null;
            }
        }

        public int VisibleCount => visibleCount;
        public int BatchCount => batchCount;
        public int RenderCallCount => renderCallCount;
        /// <summary>직전 렌더 호출에서 GPU draw로 넘기려고 한 instance 수입니다. 실제 화면 픽셀 보장값은 아닙니다.</summary>
        public int LastSubmittedInstanceCount => lastSubmittedInstanceCount;
        public int LastAutomaticSubmittedInstanceCount => lastAutomaticSubmittedInstanceCount;
        /// <summary>직전 렌더 호출에서 실행한 instanced draw call 수입니다.</summary>
        public int LastDrawCallCount => lastDrawCallCount;
        /// <summary>직전 snapshot 중 현재 카메라 viewport 안에 들어온 instance 수입니다.</summary>
        public int LastCameraVisibleCount => lastCameraVisibleCount;
        public int LastGameCameraVisibleCount => lastGameCameraVisibleCount;
        public int LastSceneViewCameraVisibleCount => lastSceneViewCameraVisibleCount;
        /// <summary>직전 진단에서 카메라가 instanced draw layer를 렌더링할 수 있었는지 여부입니다.</summary>
        public bool LastCameraCanRenderLayer => lastCameraCanRenderLayer;
        public bool LastGameCameraCanRenderLayer => lastGameCameraCanRenderLayer;
        public bool LastSceneViewCameraCanRenderLayer => lastSceneViewCameraCanRenderLayer;
        /// <summary>직전 렌더 instance 전체를 감싸는 world bounds입니다.</summary>
        public Bounds LastWorldBounds => lastWorldBounds;
        /// <summary>직전 렌더 instance가 차지한 viewport bounds입니다.</summary>
        public Rect LastViewportBounds => lastViewportBounds;
        public string LastMaterialShaderName => lastMaterialShaderName;
        public string LastMainTextureName => lastMainTextureName;
        public int MatrixCapacity => matrices != null ? matrices.Length : 0;
        public string ResolvedMaterialShaderName
        {
            get
            {
                Material resolvedMaterial = ResolveMaterial();
                return resolvedMaterial != null && resolvedMaterial.shader != null
                ? resolvedMaterial.shader.name
                : string.Empty;
            }
        }
        public bool RenderOnLateUpdate { get => renderOnLateUpdate; set => renderOnLateUpdate = value; }
        public bool RenderDuringCameraRendering { get => renderDuringCameraRendering; set => renderDuringCameraRendering = value; }
        public bool ResetRenderDataOnDisable { get => resetRenderDataOnDisable; set => resetRenderDataOnDisable = value; }
        public ProjectileRenderScaleMode ScaleMode { get => scaleMode; set => scaleMode = value; }
        public float FixedWorldScale { get => fixedWorldScale; set => fixedWorldScale = Mathf.Max(0f, value); }
        public float RenderZOffset { get => renderZOffset; set => renderZOffset = value; }
        public int RenderLayer { get => renderLayer; set => renderLayer = Mathf.Clamp(value, 0, 31); }
        /// <summary>instanced sprite draw에 사용할 기본 sprite입니다.</summary>
        public Sprite Sprite
        {
            get => sprite;
            set
            {
                if (sprite == value)
                {
                    return;
                }

                sprite = value;
                appliedSprite = null;
            }
        }
        public Sprite[] FrameSprites { get => frameSprites; set => frameSprites = value; }
        /// <summary>snapshot의 수명 진행도로 frame sprite를 자동 진행할지 여부입니다.</summary>
        public bool AnimateFramesByLifetime { get => animateFramesByLifetime; set => animateFramesByLifetime = value; }
        /// <summary>수명 기반 frame animation의 초당 frame 수입니다.</summary>
        public float FrameAnimationFramesPerSecond { get => frameAnimationFramesPerSecond; set => frameAnimationFramesPerSecond = Mathf.Max(0f, value); }
        /// <summary>수명 기반 frame animation이 마지막 frame 뒤에 반복될지 여부입니다.</summary>
        public bool LoopFrameAnimation { get => loopFrameAnimation; set => loopFrameAnimation = value; }



        private void OnValidate()
        {
            initialMatrixCapacity = Mathf.Max(1, initialMatrixCapacity);
            radiusToScale = Mathf.Max(0f, radiusToScale);
            fixedWorldScale = Mathf.Max(0f, fixedWorldScale);
            frameAnimationFramesPerSecond = Mathf.Max(0f, frameAnimationFramesPerSecond);
        }



        [Button("현재 Render 상태 로그")]
        private void LogInspectorDiagnostics()
        {
            Debug.Log($"[ProjectileInstancedSpriteRenderProxy] Visible={visibleCount}, Batch={batchCount}, DrawCalls={lastDrawCallCount}, MatrixCapacity={MatrixCapacity}", this);
        }



        private void Awake()
        {
            EnsureInitialized();
        }



        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;
            RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;
        }



        private void LateUpdate()
        {
            if (renderOnLateUpdate && !renderDuringCameraRendering)
            {
                RenderAutomatic(null);
            }
        }



        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;

            if (resetRenderDataOnDisable)
            {
                ClearRenderData();
            }
        }



        private void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;

            if (runtimeMaterial != null)
            {
                DestroyGeneratedObject(runtimeMaterial);
            }

            if (quadMesh != null)
            {
                DestroyGeneratedObject(quadMesh);
            }

            DestroyFrameQuadMeshes();
        }



        public void Initialize(IPanProjectileBody body, int matrixCapacity = 512)
        {
            ProjectileBody = body;
            InitializeMatrixPool(matrixCapacity);
        }



        private void InitializeMatrixPool(int matrixCapacity)
        {
            initialMatrixCapacity = Mathf.Max(1, matrixCapacity);

            EnsureInitialized();
            EnsureCapacity(initialMatrixCapacity);
            visibleCount = 0;
            batchCount = 0;
            renderCallCount = 0;
        }



        private void HandleBeginCameraRendering(ScriptableRenderContext context, Camera renderingCamera)
        {
            if (!renderOnLateUpdate || !renderDuringCameraRendering)
            {
                return;
            }

            if (!CanUseCameraForAutomaticRender(renderingCamera))
            {
                return;
            }

            RenderAutomatic(renderingCamera);
        }
    }
}
