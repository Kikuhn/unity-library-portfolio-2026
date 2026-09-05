using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;



namespace Pan.HighDensityElement
{
    public readonly struct ElementVisualDefinition
    {
        public ElementVisualDefinition(
            Sprite sprite,
            Material material,
            int sortingLayerId = 0,
            int sortingOrder = 0,
            SpriteMaskInteraction maskInteraction = SpriteMaskInteraction.None,
            uint renderingLayerMask = uint.MaxValue,
            int layer = 0)
        {
            Sprite = sprite;
            Material = material;
            SortingLayerId = sortingLayerId;
            SortingOrder = sortingOrder;
            MaskInteraction = maskInteraction;
            RenderingLayerMask = renderingLayerMask;
            Layer = layer;
        }



        public Sprite Sprite { get; }
        public Material Material { get; }
        public int SortingLayerId { get; }
        public int SortingOrder { get; }
        public SpriteMaskInteraction MaskInteraction { get; }
        public uint RenderingLayerMask { get; }
        public int Layer { get; }
        public bool IsValid => Sprite != null && Material != null;
    }



    public sealed class ElementVisualRegistry
    {
        private readonly Dictionary<int, ElementVisualDefinition> definitions =
            new Dictionary<int, ElementVisualDefinition>();



        public void Register(int visualId, in ElementVisualDefinition definition)
        {
            if (!definition.IsValid) { throw new ArgumentException("Sprite와 Material이 필요합니다.", nameof(definition)); }
            definitions[visualId] = definition;
        }



        public bool Unregister(int visualId) => definitions.Remove(visualId);
        public bool TryGet(int visualId, out ElementVisualDefinition definition) =>
            definitions.TryGetValue(visualId, out definition);
        public void Clear() => definitions.Clear();
    }



    [StructLayout(LayoutKind.Sequential)]
    public struct RenderSpriteInstanceData
    {
        public Matrix4x4 objectToWorld;
        public Color spriteColor;
        public uint renderingLayerMask;
    }



    public sealed class ElementSpriteRenderer : IDisposable
    {
        public const int UnityMaximumInstancesPerBatch = 1023;
        public const int ConservativeDefaultInstancesPerBatch = 511;

        private readonly ElementVisualRegistry registry;
        private NativeList<ElementRenderItem> renderItems;
        private NativeList<RenderSpriteInstanceData> instanceData;
        private bool disposed;



        public ElementSpriteRenderer(
            ElementVisualRegistry registry,
            int initialCapacity = 512,
            int maximumInstancesPerBatch = ConservativeDefaultInstancesPerBatch,
            float spatialChunkSize = 16f)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            if (initialCapacity < 1) { throw new ArgumentOutOfRangeException(nameof(initialCapacity)); }
            if (maximumInstancesPerBatch < 1 || maximumInstancesPerBatch > UnityMaximumInstancesPerBatch)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumInstancesPerBatch));
            }
            if (spatialChunkSize <= 0f) { throw new ArgumentOutOfRangeException(nameof(spatialChunkSize)); }

            MaximumInstancesPerBatch = maximumInstancesPerBatch;
            SpatialChunkSize = spatialChunkSize;
            renderItems = new NativeList<ElementRenderItem>(initialCapacity, Allocator.Persistent);
            instanceData = new NativeList<RenderSpriteInstanceData>(maximumInstancesPerBatch, Allocator.Persistent);
        }



        public int MaximumInstancesPerBatch { get; }
        public float SpatialChunkSize { get; }
        public int LastBatchCount { get; private set; }
        public int LastInstanceCount { get; private set; }
        public int LastFallbackSubmissionCount { get; private set; }



        public void Render(ElementWorld world, Camera camera = null)
        {
            Render(world, 1f, camera);
        }



        /// <summary>
        /// 이전·현재 고정 스텝 pose 사이를 보간해 GameObject 없는 Sprite instance를 제출합니다.
        /// </summary>
        public void Render(ElementWorld world, float interpolationAlpha, Camera camera = null)
        {
            if (disposed) { throw new ObjectDisposedException(nameof(ElementSpriteRenderer)); }
            if (world == null) { throw new ArgumentNullException(nameof(world)); }

            world.CopyRenderItems(renderItems, interpolationAlpha);
            float inverseChunkSize = 1f / SpatialChunkSize;
            for (int i = 0; i < renderItems.Length; i++)
            {
                ElementRenderItem item = renderItems[i];
                item.SpatialChunk = (int2)math.floor(item.Position * inverseChunkSize);
                renderItems[i] = item;
            }

            renderItems.Sort();
            LastBatchCount = 0;
            LastInstanceCount = renderItems.Length;
            LastFallbackSubmissionCount = 0;

            int groupStart = 0;
            while (groupStart < renderItems.Length)
            {
                ElementRenderItem first = renderItems[groupStart];
                int groupEnd = groupStart + 1;
                while (groupEnd < renderItems.Length &&
                    renderItems[groupEnd].VisualId == first.VisualId &&
                    renderItems[groupEnd].SpatialChunk.Equals(first.SpatialChunk))
                {
                    groupEnd++;
                }

                if (registry.TryGet(first.VisualId, out ElementVisualDefinition definition))
                {
                    RenderGroup(groupStart, groupEnd, in definition, camera);
                }

                groupStart = groupEnd;
            }
        }



        public void Dispose()
        {
            if (disposed) { return; }
            if (renderItems.IsCreated) { renderItems.Dispose(); }
            if (instanceData.IsCreated) { instanceData.Dispose(); }
            disposed = true;
        }



        private void RenderGroup(
            int groupStart,
            int groupEnd,
            in ElementVisualDefinition definition,
            Camera camera)
        {
            int batchStart = groupStart;
            while (batchStart < groupEnd)
            {
                int count = Mathf.Min(MaximumInstancesPerBatch, groupEnd - batchStart);
                FillInstanceData(batchStart, count, definition.RenderingLayerMask, out Bounds bounds);

                RenderParams renderParams = new RenderParams(definition.Material)
                {
                    camera = camera,
                    layer = definition.Layer,
                    renderingLayerMask = definition.RenderingLayerMask,
                    sortingLayerID = definition.SortingLayerId,
                    sortingOrder = definition.SortingOrder,
                    worldBounds = bounds,
                    shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = false
                };
                SpriteParams spriteParams = new SpriteParams(
                    definition.Sprite,
                    Color.white,
                    definition.MaskInteraction);

                NativeArray<RenderSpriteInstanceData> batch = instanceData.AsArray().GetSubArray(0, count);
                if (SystemInfo.supportsInstancing && definition.Material.enableInstancing)
                {
                    Graphics.RenderSpriteInstanced(renderParams, spriteParams, 0, batch);
                    LastBatchCount++;
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        RenderSpriteInstanceData instance = batch[i];
                        SpriteParams fallbackSpriteParams = new SpriteParams(
                            definition.Sprite,
                            instance.spriteColor,
                            definition.MaskInteraction);
                        Graphics.RenderSprite(renderParams, fallbackSpriteParams, 0, instance.objectToWorld);
                        LastFallbackSubmissionCount++;
                    }
                }

                batchStart += count;
            }
        }



        private void FillInstanceData(int start, int count, uint renderingLayerMask, out Bounds bounds)
        {
            instanceData.ResizeUninitialized(count);
            Vector3 minimum = new Vector3(float.PositiveInfinity, float.PositiveInfinity, 0f);
            Vector3 maximum = new Vector3(float.NegativeInfinity, float.NegativeInfinity, 0f);

            for (int i = 0; i < count; i++)
            {
                ElementRenderItem item = renderItems[start + i];
                Vector3 position = new Vector3(item.Position.x, item.Position.y, 0f);
                Vector3 scale = new Vector3(item.Scale.x, item.Scale.y, 1f);
                Quaternion rotation = Quaternion.Euler(0f, 0f, math.degrees(item.RotationRadians));
                instanceData[i] = new RenderSpriteInstanceData
                {
                    objectToWorld = Matrix4x4.TRS(position, rotation, scale),
                    spriteColor = item.Color,
                    renderingLayerMask = renderingLayerMask
                };

                float sine = Mathf.Abs(Mathf.Sin(item.RotationRadians));
                float cosine = Mathf.Abs(Mathf.Cos(item.RotationRadians));
                float halfWidth = Mathf.Abs(scale.x) * 0.5f;
                float halfHeight = Mathf.Abs(scale.y) * 0.5f;
                Vector3 halfScale = new Vector3(
                    cosine * halfWidth + sine * halfHeight,
                    sine * halfWidth + cosine * halfHeight,
                    0.01f);
                minimum = Vector3.Min(minimum, position - halfScale);
                maximum = Vector3.Max(maximum, position + halfScale);
            }

            bounds = new Bounds((minimum + maximum) * 0.5f, maximum - minimum);
        }
    }
}
