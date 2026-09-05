using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Pan.HighDensityProjectile
{

    /// <summary>
    /// `IPanProjectileBody` snapshot을 pooled `SpriteRenderer` 배열로 표시하는 디버그/검증용 renderer입니다.
    /// 대량 production 탄막은 `ProjectileInstancedSpriteRenderProxy`가 기본이며, 이 renderer는 개별 object 관찰이 필요할 때 사용합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProjectileSpriteRenderProxy : MonoBehaviour
    {
        [FoldoutGroup("연결"), SerializeField, LabelText("검증용 Managed Body"), Tooltip("비워두면 같은 GameObject 또는 부모의 ManagedProjectileBody/IPanProjectileBody를 자동으로 찾습니다.")]
        private ManagedProjectileBody projectileManager;
        [FoldoutGroup("연결"), SerializeField, LabelText("Body Source"), Tooltip("ManagedProjectileBody가 아닌 IPanProjectileBody 구현체를 직접 연결할 때 사용합니다.")]
        private MonoBehaviour projectileBodySource;
        [FoldoutGroup("버퍼"), SerializeField, Min(1), LabelText("초기 Renderer Capacity")] private int initialRendererCapacity = 512;
        [FoldoutGroup("렌더링"), SerializeField, LabelText("LateUpdate 자동 렌더")] private bool renderOnLateUpdate = true;
        [FoldoutGroup("표시"), SerializeField, LabelText("기본 Sprite")] private Sprite sprite;
        [SerializeField, Tooltip("VisualFrameIndex가 1 이상일 때 사용할 sprite 목록입니다. index 1은 배열 0번에 대응합니다.")]
        private Sprite[] frameSprites;
        [SerializeField, Tooltip("켜면 snapshot의 경과 수명과 FPS를 기준으로 FrameSprites를 자동 진행합니다.")]
        private bool animateFramesByLifetime;
        [SerializeField, Min(0f), Tooltip("수명 기반 frame animation 속도입니다. 0이면 자동 진행하지 않습니다.")]
        private float frameAnimationFramesPerSecond = 12f;
        [SerializeField, Tooltip("켜면 마지막 frame 뒤 첫 frame으로 돌아가고, 끄면 마지막 frame에 고정됩니다.")]
        private bool loopFrameAnimation = true;
        [SerializeField] private Material material;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private int sortingLayerId;
        [SerializeField] private int sortingOrder;
        [SerializeField, Min(0f)] private float radiusToScale = 2f;
        [SerializeField] private ProjectileRenderScaleMode scaleMode = ProjectileRenderScaleMode.PhysicsDiameter;
        [SerializeField, Min(0f)] private float fixedWorldScale = 1f;
        [SerializeField] private bool resetRenderersOnDisable = true;



        private ProjectileSnapshot[] snapshotBuffer;
        private SpriteRenderer[] renderers;
        private int visibleCount;
        private IPanProjectileBody projectileBody;
        private MonoBehaviour resolvedProjectileBodySource;
        private Sprite appliedSprite;
        private Material appliedMaterial;
        private Color appliedColor;
        private int appliedSortingLayerId;
        private int appliedSortingOrder;
        private bool visualSettingsApplied;
        private readonly List<MonoBehaviour> bodyLookupBuffer = new List<MonoBehaviour>(4);
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
        public int RendererCount => renderers != null ? renderers.Length : 0;
        public bool RenderOnLateUpdate { get => renderOnLateUpdate; set => renderOnLateUpdate = value; }
        public bool ResetRenderersOnDisable { get => resetRenderersOnDisable; set => resetRenderersOnDisable = value; }
        public ProjectileRenderScaleMode ScaleMode { get => scaleMode; set => scaleMode = value; }
        public float FixedWorldScale { get => fixedWorldScale; set => fixedWorldScale = Mathf.Max(0f, value); }
        public int SortingLayerId
        {
            get => sortingLayerId;
            set
            {
                if (sortingLayerId == value)
                {
                    return;
                }

                sortingLayerId = value;
                visualSettingsApplied = false;
            }
        }

        public int SortingOrder
        {
            get => sortingOrder;
            set
            {
                if (sortingOrder == value)
                {
                    return;
                }

                sortingOrder = value;
                visualSettingsApplied = false;
            }
        }

        /// <summary>모든 pooled SpriteRenderer가 기본으로 사용할 sprite입니다.</summary>
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
                visualSettingsApplied = false;
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
            initialRendererCapacity = Mathf.Max(1, initialRendererCapacity);
            radiusToScale = Mathf.Max(0f, radiusToScale);
            fixedWorldScale = Mathf.Max(0f, fixedWorldScale);
            frameAnimationFramesPerSecond = Mathf.Max(0f, frameAnimationFramesPerSecond);
        }



        [Button("현재 Render 상태 로그")]
        private void LogInspectorDiagnostics()
        {
            Debug.Log($"[ProjectileSpriteRenderProxy] Visible={visibleCount}, RendererCapacity={RendererCount}", this);
        }



        private void Awake()
        {
            EnsureInitialized();
        }



        private void LateUpdate()
        {
            if (renderOnLateUpdate)
            {
                RenderNow();
            }
        }



        private void OnDisable()
        {
            if (resetRenderersOnDisable)
            {
                ClearRenderedProjectiles();
            }
        }



        public void Initialize(IPanProjectileBody body, int rendererCapacity = 512)
        {
            ProjectileBody = body;
            InitializeRendererPool(rendererCapacity);
        }



        private void InitializeRendererPool(int rendererCapacity)
        {
            initialRendererCapacity = Mathf.Max(1, rendererCapacity);

            if (renderers == null)
            {
                snapshotBuffer = new ProjectileSnapshot[initialRendererCapacity];
                renderers = new SpriteRenderer[initialRendererCapacity];
                CreateRenderers(0, initialRendererCapacity);
            }
            else
            {
                EnsureCapacity(initialRendererCapacity);
                HideRange(0, visibleCount);
            }

            visibleCount = 0;
        }



        public int RenderNow()
        {
            EnsureInitialized();
            if (projectileBody == null)
            {
                HideVisibleRenderers();
                return 0;
            }

            int activeCount = projectileBody.ActiveCount;
            EnsureCapacity(activeCount);

            int renderCount = projectileBody.CopySnapshots(snapshotBuffer);
            Sprite resolvedSprite = sprite != null ? sprite : GetDefaultSprite();
            ApplyRendererVisuals(resolvedSprite);

            for (int i = 0; i < renderCount; i++)
            {
                ProjectileSnapshot snapshot = snapshotBuffer[i];
                SpriteRenderer renderer = renderers[i];
                Transform rendererTransform = renderer.transform;

                rendererTransform.position = snapshot.Kinematic.Position;
                float worldScale = ResolveWorldScale(in snapshot);
                rendererTransform.localScale = new Vector3(worldScale, worldScale, 1f);

                renderer.sprite = ResolveSprite(in snapshot, resolvedSprite);
                renderer.enabled = true;
            }

            if (visibleCount > renderCount)
            {
                HideRange(renderCount, visibleCount);
            }

            visibleCount = renderCount;
            return renderCount;
        }



        private float ResolveWorldScale(in ProjectileSnapshot snapshot)
        {
            return scaleMode == ProjectileRenderScaleMode.FixedWorldScale
            ? Mathf.Max(0f, fixedWorldScale)
            : Mathf.Max(0f, snapshot.Kinematic.Radius * radiusToScale);
        }



        /// <summary>
        /// 풀링으로 비활성화되거나 body가 교체될 때, 이전 프레임의 sprite renderer 잔상을 즉시 숨깁니다.
        /// </summary>
        public void ClearRenderedProjectiles()
        {
            if (renderers != null)
            {
                HideRange(0, renderers.Length);
            }

            visibleCount = 0;
        }



        private void HideVisibleRenderers()
        {
            if (renderers != null && visibleCount > 0)
            {
                HideRange(0, visibleCount);
            }

            visibleCount = 0;
        }



        private void EnsureInitialized()
        {
            EnsureProjectileBody();

            if (renderers != null)
            {
                return;
            }

            int capacity = Mathf.Max(1, initialRendererCapacity);
            snapshotBuffer = new ProjectileSnapshot[capacity];
            renderers = new SpriteRenderer[capacity];
            CreateRenderers(0, capacity);
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

            if (renderers.Length >= requiredCapacity)
            {
                return;
            }

            int oldCapacity = renderers.Length;
            int newCapacity = Mathf.Max(requiredCapacity, oldCapacity * 2);
            System.Array.Resize(ref snapshotBuffer, newCapacity);
            System.Array.Resize(ref renderers, newCapacity);
            CreateRenderers(oldCapacity, newCapacity);
            visualSettingsApplied = false;
        }



        private void CreateRenderers(int startIndex, int endIndex)
        {
            for (int i = startIndex; i < endIndex; i++)
            {
                GameObject rendererObject = new GameObject($"ProjectileSprite_{i:D4}");
                rendererObject.transform.SetParent(transform, false);

                SpriteRenderer renderer = rendererObject.AddComponent<SpriteRenderer>();
                renderer.enabled = false;
                renderer.sortingLayerID = sortingLayerId;
                renderer.sortingOrder = sortingOrder;
                renderers[i] = renderer;
            }
        }



        private void HideRange(int startIndex, int endIndex)
        {
            int count = Mathf.Min(endIndex, renderers.Length);
            for (int i = startIndex; i < count; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = false;
                }
            }
        }



        private void ApplyRendererVisuals(Sprite resolvedSprite)
        {
            if (visualSettingsApplied &&
            appliedSprite == resolvedSprite &&
            appliedMaterial == material &&
            appliedColor == color &&
            appliedSortingLayerId == sortingLayerId &&
            appliedSortingOrder == sortingOrder)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.sprite = resolvedSprite;
                renderer.color = color;
                renderer.sortingLayerID = sortingLayerId;
                renderer.sortingOrder = sortingOrder;
                if (material != null)
                {
                    renderer.sharedMaterial = material;
                }
            }

            appliedSprite = resolvedSprite;
            appliedMaterial = material;
            appliedColor = color;
            appliedSortingLayerId = sortingLayerId;
            appliedSortingOrder = sortingOrder;
            visualSettingsApplied = true;
        }



        private Sprite ResolveSprite(in ProjectileSnapshot snapshot, Sprite fallbackSprite)
        {
            int visualFrameIndex = ResolveAnimatedFrameIndex(in snapshot);
            if (visualFrameIndex <= 0 || frameSprites == null)
            {
                return fallbackSprite;
            }

            int spriteIndex = visualFrameIndex - 1;
            if (spriteIndex < 0 || spriteIndex >= frameSprites.Length)
            {
                return fallbackSprite;
            }

            return frameSprites[spriteIndex] != null ? frameSprites[spriteIndex] : fallbackSprite;
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
