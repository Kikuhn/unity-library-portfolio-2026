using System;
using System.IO;
using Pan.Event;
using Pan.HighDensityElement;
using Pan.HighDensityElement.PanEvent;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;



[PanEventUsageProfile(PanEventUsageProfile.ValidationOnly)]
public sealed class HighDensityElementPerformanceEventValue :
    PanBaseEventValue.EventAbles<HighDensityElementPerformanceEventValue>,
    IEventAbleSignalReceiver<ElementFactSignal>
{
    public int ReceivedFactCount { get; private set; }



    public void ReceiveLocalSignal(IEventAble source, in ElementFactSignal signal)
    {
        ReceivedFactCount++;
    }



    protected override void Disable()
    {
        ReceivedFactCount = 0;
    }
}



public sealed class HighDensityElementPerformanceValidation : MonoBehaviour
{
    private const int ElementCount = 5000;
    private const int TargetCount = 256;
    private const int EventValueElementCount = 1000;
    private const float WarmupSeconds = 5f;
    private const float MeasurementSeconds = 30f;
    private const int MaximumFrameSamples = 120000;

    [Serializable]
    private sealed class ValidationReport
    {
        public string unityVersion;
        public string graphicsDevice;
        public string reportPath;
        public int elementCount;
        public int targetCount;
        public int eventValueElementCount;
        public int activeEventHostCount;
        public int frameSampleCount;
        public float measurementDurationSeconds;
        public float frameTimeAverageMs;
        public float frameTimeP95Ms;
        public float frameTimeMaximumMs;
        public float cpuTargetSyncAverageMs;
        public float cpuSimulationAverageMs;
        public float cpuFactDispatchAverageMs;
        public float cpuRenderSubmitAverageMs;
        public int gpuFrameSampleCount;
        public float gpuFrameTimeAverageMs;
        public string gpuTimingSource;
        public bool supportsGpuRecorder;
        public bool supportsFrameTimingManager;
        public long maximumGcAllocBytesPerFrame;
        public int framesWithGcAllocation;
        public long maximumElementAllocBytesPerFrame;
        public long maximumTargetSyncAllocBytesPerFrame;
        public long maximumSimulationAllocBytesPerFrame;
        public long maximumRenderAllocBytesPerFrame;
        public long maximumMeasurementAllocBytesPerFrame;
        public int renderBatchCount;
        public int renderedInstanceCount;
        public int coreBodyCount;
        public int coreQueryFactCount;
        public int gameObjectDelta;
        public int transformDelta;
        public int monoBehaviourDelta;
        public int spriteRendererDelta;
        public bool passed;
    }

    private enum Phase : byte
    {
        Warmup,
        Measure,
        Finished
    }

    [SerializeField]
    private Material validationMaterial;

    [SerializeField]
    private Camera validationCamera;

    private ElementWorld world;
    private EventElementHostRegistry eventHosts;
    private ElementVisualRegistry visualRegistry;
    private ElementSpriteRenderer spriteRenderer;
    private ElementHandle[] targets;
    private Texture2D spriteTexture;
    private Sprite sprite;
    private ProfilerRecorder gcAllocationRecorder;
    private ProfilerRecorder gpuFrameTimeRecorder;
    private readonly float[] frameTimeSamples = new float[MaximumFrameSamples];
    private readonly double[] gpuFrameTimeSamples = new double[MaximumFrameSamples];
    private readonly FrameTiming[] frameTimingBuffer = new FrameTiming[1];
    private Phase phase;
    private float phaseElapsed;
    private int frameSampleCount;
    private int gpuFrameSampleCount;
    private long maximumGcAllocBytesPerFrame;
    private int framesWithGcAllocation;
    private long maximumTargetSyncAllocBytesPerFrame;
    private long maximumSimulationAllocBytesPerFrame;
    private long maximumRenderAllocBytesPerFrame;
    private long maximumMeasurementAllocBytesPerFrame;
    private double totalTargetSyncMilliseconds;
    private double totalSimulationMilliseconds;
    private double totalFactDispatchMilliseconds;
    private double totalRenderSubmitMilliseconds;
    private bool usedFrameTimingManager;
    private bool frameTimingManagerEnabled;
    private int baselineGameObjectCount;
    private int baselineTransformCount;
    private int baselineMonoBehaviourCount;
    private int baselineSpriteRendererCount;
    private float fixedStepAccumulator;
    private ushort substepSequence;



    public void Configure(Material material, Camera camera)
    {
        validationMaterial = material;
        validationCamera = camera;
    }



    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        if (!PanEventGeneralManager.IsInitialize) { PanEventGeneralManager.Initialize(); }

        InitializeRuntime();
        phase = Phase.Warmup;
    }



    private void Update()
    {
        if (phase == Phase.Finished || world == null) { return; }

        float deltaTime = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        if (phase == Phase.Measure && frameTimingManagerEnabled)
        {
            FrameTimingManager.CaptureFrameTimings();
        }

        long targetSyncStart = System.Diagnostics.Stopwatch.GetTimestamp();
        long allocationStart = GC.GetAllocatedBytesForCurrentThread();
        UpdateTargets(Time.unscaledTime);
        long targetSyncEnd = System.Diagnostics.Stopwatch.GetTimestamp();
        long afterTargetSync = GC.GetAllocatedBytesForCurrentThread();
        long simulationStart = System.Diagnostics.Stopwatch.GetTimestamp();
        double factDispatchMilliseconds = 0d;
        fixedStepAccumulator = Mathf.Min(fixedStepAccumulator + deltaTime, 0.08f);
        while (fixedStepAccumulator + 0.000001f >= 0.02f)
        {
            substepSequence = substepSequence == ushort.MaxValue ? (ushort)1 : (ushort)(substepSequence + 1);
            world.Tick(0.02f, substepSequence);
            factDispatchMilliseconds += world.LastFactDispatchMilliseconds;
            fixedStepAccumulator -= 0.02f;
        }
        long simulationEnd = System.Diagnostics.Stopwatch.GetTimestamp();
        long afterSimulation = GC.GetAllocatedBytesForCurrentThread();
        long renderSubmitStart = System.Diagnostics.Stopwatch.GetTimestamp();
        spriteRenderer.Render(world, validationCamera);
        long renderSubmitEnd = System.Diagnostics.Stopwatch.GetTimestamp();
        long afterRender = GC.GetAllocatedBytesForCurrentThread();
        phaseElapsed += deltaTime;

        if (phase == Phase.Warmup)
        {
            if (phaseElapsed >= WarmupSeconds) { BeginMeasurement(); }
            return;
        }

        RecordFrame(
            afterTargetSync - allocationStart,
            afterSimulation - afterTargetSync,
            afterRender - afterSimulation,
            TicksToMilliseconds(targetSyncEnd - targetSyncStart),
            TicksToMilliseconds(simulationEnd - simulationStart),
            factDispatchMilliseconds,
            TicksToMilliseconds(renderSubmitEnd - renderSubmitStart),
            allocationStart);
        if (phaseElapsed >= MeasurementSeconds || frameSampleCount >= MaximumFrameSamples)
        {
            FinishMeasurement();
        }
    }



    private void OnDestroy()
    {
        if (gcAllocationRecorder.Valid) { gcAllocationRecorder.Dispose(); }
        if (gpuFrameTimeRecorder.Valid) { gpuFrameTimeRecorder.Dispose(); }
        spriteRenderer?.Dispose();
        eventHosts?.Dispose();
        world?.Dispose();
        if (sprite != null) { Destroy(sprite); }
        if (spriteTexture != null) { Destroy(spriteTexture); }
    }



    private void InitializeRuntime()
    {
        if (validationMaterial == null || validationCamera == null)
        {
            throw new InvalidOperationException("성능 검증 Material과 Camera가 필요합니다.");
        }

        validationMaterial.enableInstancing = true;
        spriteTexture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        var pixels = new Color32[64];
        for (int i = 0; i < pixels.Length; i++) { pixels[i] = new Color32(255, 255, 255, 255); }
        spriteTexture.SetPixels32(pixels);
        spriteTexture.Apply(false, true);
        sprite = Sprite.Create(spriteTexture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);

        world = new ElementWorld(ElementCount + TargetCount + 512);
        world.EnableTimingMetrics = true;
        eventHosts = new EventElementHostRegistry(world, initialEventTableCapacity: 1, initialHostCapacity: EventValueElementCount);
        visualRegistry = new ElementVisualRegistry();
        visualRegistry.Register(1, new ElementVisualDefinition(sprite, validationMaterial));
        spriteRenderer = new ElementSpriteRenderer(
            visualRegistry,
            initialCapacity: ElementCount,
            maximumInstancesPerBatch: ElementSpriteRenderer.ConservativeDefaultInstancesPerBatch,
            spatialChunkSize: 16f);
        targets = new ElementHandle[TargetCount];

        SpawnElements();
        SpawnTargets();
        UpdateTargets(0f);
    }



    private void SpawnElements()
    {
        if (!ElementCompiledArchetype.TryCreate(
                ElementCapabilities.Lifetime |
                ElementCapabilities.KinematicMotion2D |
                ElementCapabilities.SpriteVisual2D |
                ElementCapabilities.QuerySensor2D,
                radius: 0.08f,
                lifetime: 120f,
                visualId: 1,
                periodicInterval: 0f,
                penetrating: false,
                PhysicsCoreBodyMode.Kinematic,
                PhysicsCoreShape2D.Circle,
                out ElementCompiledArchetype archetype,
                out ElementSpawnStatus failure))
        {
            throw new InvalidOperationException($"성능 검증 archetype 생성 실패: {failure}");
        }

        for (int i = 0; i < ElementCount; i++)
        {
            int xIndex = i % 100;
            int yIndex = i / 100;
            float2 position = new float2(-39.6f + xIndex * 0.8f, -39.6f + yIndex * 0.8f);
            float direction = (i & 1) == 0 ? 1f : -1f;
            float2 velocity = new float2(0.12f * direction, 0.04f * -direction);
            ElementSpawnBuilder builder = ElementSpawnBuilder.From(archetype)
                .WithPose(position, new float2(0.22f))
                .WithVelocity(velocity)
                .WithOwner((uint)(i + 1), 1u)
                .WithColor(new Color32(255, (byte)(128 + i % 127), 255, 255));
            ElementSpawnResult result = world.Spawn(in builder);
            if (!result.Succeeded) { throw new InvalidOperationException($"Element spawn 실패: {result.Status}"); }

            if (i < EventValueElementCount)
            {
                EventElementHandle eventElement = eventHosts.Wrap(result.Handle);
                HighDensityElementPerformanceEventValue.Require(eventElement);
            }
        }
    }



    private void SpawnTargets()
    {
        if (!ElementCompiledArchetype.TryCreate(
                ElementCapabilities.QueryTarget2D,
                radius: 0.18f,
                lifetime: 120f,
                visualId: 0,
                periodicInterval: 0f,
                penetrating: false,
                PhysicsCoreBodyMode.Kinematic,
                PhysicsCoreShape2D.Circle,
                out ElementCompiledArchetype archetype,
                out ElementSpawnStatus failure))
        {
            throw new InvalidOperationException($"성능 검증 target archetype 생성 실패: {failure}");
        }

        for (int i = 0; i < targets.Length; i++)
        {
            ElementSpawnResult result = world.Spawn(ElementSpawnBuilder.From(archetype));
            if (!result.Succeeded) { throw new InvalidOperationException($"Target spawn 실패: {result.Status}"); }
            targets[i] = result.Handle;
        }
    }



    private void UpdateTargets(float time)
    {
        for (int i = 0; i < TargetCount; i++)
        {
            int xIndex = i % 16;
            int yIndex = i / 16;
            float2 center = new float2(-37.5f + xIndex * 5f, -37.5f + yIndex * 5f);
            float2 position = center + new float2(Mathf.Sin(time + i * 0.1f), Mathf.Cos(time * 0.7f + i * 0.13f)) * 0.35f;
            targets[i].TrySubmit(new SetElementPosition2D(position));
        }
    }



    private void BeginMeasurement()
    {
        phase = Phase.Measure;
        phaseElapsed = 0f;
        baselineGameObjectCount = FindObjectsByType<GameObject>(FindObjectsInactive.Include).Length;
        baselineTransformCount = FindObjectsByType<Transform>(FindObjectsInactive.Include).Length;
        baselineMonoBehaviourCount = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include).Length;
        baselineSpriteRendererCount = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include).Length;
        frameSampleCount = 0;
        gpuFrameSampleCount = 0;
        maximumGcAllocBytesPerFrame = 0;
        framesWithGcAllocation = 0;
        maximumTargetSyncAllocBytesPerFrame = 0;
        maximumSimulationAllocBytesPerFrame = 0;
        maximumRenderAllocBytesPerFrame = 0;
        maximumMeasurementAllocBytesPerFrame = 0;
        totalTargetSyncMilliseconds = 0d;
        totalSimulationMilliseconds = 0d;
        totalFactDispatchMilliseconds = 0d;
        totalRenderSubmitMilliseconds = 0d;
        usedFrameTimingManager = false;
        frameTimingManagerEnabled = FrameTimingManager.IsFeatureEnabled();
        gcAllocationRecorder = new ProfilerRecorder(
            ProfilerCategory.Memory,
            "GC.Alloc",
            1,
            ProfilerRecorderOptions.WrapAroundWhenCapacityReached |
            ProfilerRecorderOptions.SumAllSamplesInFrame |
            ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
        gcAllocationRecorder.Start();
        gpuFrameTimeRecorder = new ProfilerRecorder(
            ProfilerCategory.Render,
            "GPU Frame Time",
            1,
            ProfilerRecorderOptions.WrapAroundWhenCapacityReached |
            ProfilerRecorderOptions.SumAllSamplesInFrame);
        gpuFrameTimeRecorder.Start();
    }



    private void RecordFrame(
        long targetSyncAllocBytes,
        long simulationAllocBytes,
        long renderAllocBytes,
        double targetSyncMilliseconds,
        double simulationMilliseconds,
        double factDispatchMilliseconds,
        double renderSubmitMilliseconds,
        long allocationStart)
    {
        frameTimeSamples[frameSampleCount++] = Time.unscaledDeltaTime * 1000f;
        totalTargetSyncMilliseconds += targetSyncMilliseconds;
        totalSimulationMilliseconds += simulationMilliseconds;
        totalFactDispatchMilliseconds += factDispatchMilliseconds;
        totalRenderSubmitMilliseconds += renderSubmitMilliseconds;
        maximumTargetSyncAllocBytesPerFrame = Math.Max(maximumTargetSyncAllocBytesPerFrame, targetSyncAllocBytes);
        maximumSimulationAllocBytesPerFrame = Math.Max(maximumSimulationAllocBytesPerFrame, simulationAllocBytes);
        maximumRenderAllocBytesPerFrame = Math.Max(maximumRenderAllocBytesPerFrame, renderAllocBytes);
        if (gcAllocationRecorder.Valid)
        {
            long allocatedBytes = gcAllocationRecorder.LastValue;
            maximumGcAllocBytesPerFrame = Math.Max(maximumGcAllocBytesPerFrame, allocatedBytes);
            if (allocatedBytes > 0) { framesWithGcAllocation++; }
        }

        bool recordedGpuTiming = false;
        if (gpuFrameTimeRecorder.Valid && gpuFrameTimeRecorder.LastValue > 0)
        {
            gpuFrameTimeSamples[gpuFrameSampleCount++] = gpuFrameTimeRecorder.LastValue * 0.000001d;
            recordedGpuTiming = true;
        }

        if (!recordedGpuTiming &&
            frameTimingManagerEnabled &&
            FrameTimingManager.GetLatestTimings(1, frameTimingBuffer) > 0 &&
            frameTimingBuffer[0].gpuFrameTime > 0d)
        {
            gpuFrameTimeSamples[gpuFrameSampleCount++] = frameTimingBuffer[0].gpuFrameTime;
            usedFrameTimingManager = true;
        }

        maximumMeasurementAllocBytesPerFrame = Math.Max(
            maximumMeasurementAllocBytesPerFrame,
            GC.GetAllocatedBytesForCurrentThread() - allocationStart -
            targetSyncAllocBytes - simulationAllocBytes - renderAllocBytes);
    }



    private void FinishMeasurement()
    {
        phase = Phase.Finished;
        if (gcAllocationRecorder.Valid) { gcAllocationRecorder.Stop(); }
        if (gpuFrameTimeRecorder.Valid) { gpuFrameTimeRecorder.Stop(); }
        Array.Sort(frameTimeSamples, 0, frameSampleCount);
        Array.Sort(gpuFrameTimeSamples, 0, gpuFrameSampleCount);

        string reportPath = Path.GetFullPath(Path.Combine(
            Application.dataPath,
            "..",
            "PanHighDensityElementPerformanceReport.json"));
        var report = new ValidationReport
        {
            unityVersion = Application.unityVersion,
            graphicsDevice = SystemInfo.graphicsDeviceName,
            reportPath = reportPath,
            elementCount = world.AliveCount - targets.Length,
            targetCount = targets.Length,
            eventValueElementCount = EventValueElementCount,
            activeEventHostCount = eventHosts.ActiveHostCount,
            frameSampleCount = frameSampleCount,
            measurementDurationSeconds = phaseElapsed,
            frameTimeAverageMs = CalculateAverage(frameTimeSamples, frameSampleCount),
            frameTimeP95Ms = Percentile95(frameTimeSamples, frameSampleCount),
            frameTimeMaximumMs = frameSampleCount > 0 ? frameTimeSamples[frameSampleCount - 1] : 0f,
            cpuTargetSyncAverageMs = Average(totalTargetSyncMilliseconds, frameSampleCount),
            cpuSimulationAverageMs = Average(totalSimulationMilliseconds, frameSampleCount),
            cpuFactDispatchAverageMs = Average(totalFactDispatchMilliseconds, frameSampleCount),
            cpuRenderSubmitAverageMs = Average(totalRenderSubmitMilliseconds, frameSampleCount),
            gpuFrameSampleCount = gpuFrameSampleCount,
            gpuFrameTimeAverageMs = (float)CalculateAverage(gpuFrameTimeSamples, gpuFrameSampleCount),
            gpuTimingSource = gpuFrameSampleCount == 0
                ? "Unavailable"
                : usedFrameTimingManager ? "FrameTimingManager" : "ProfilerRecorder",
            supportsGpuRecorder = SystemInfo.supportsGpuRecorder,
            supportsFrameTimingManager = frameTimingManagerEnabled,
            maximumGcAllocBytesPerFrame = gcAllocationRecorder.Valid ? maximumGcAllocBytesPerFrame : -1,
            framesWithGcAllocation = framesWithGcAllocation,
            maximumElementAllocBytesPerFrame = Math.Max(
                Math.Max(maximumTargetSyncAllocBytesPerFrame, maximumSimulationAllocBytesPerFrame),
                Math.Max(maximumRenderAllocBytesPerFrame, maximumMeasurementAllocBytesPerFrame)),
            maximumTargetSyncAllocBytesPerFrame = maximumTargetSyncAllocBytesPerFrame,
            maximumSimulationAllocBytesPerFrame = maximumSimulationAllocBytesPerFrame,
            maximumRenderAllocBytesPerFrame = maximumRenderAllocBytesPerFrame,
            maximumMeasurementAllocBytesPerFrame = maximumMeasurementAllocBytesPerFrame,
            renderBatchCount = spriteRenderer.LastBatchCount,
            renderedInstanceCount = spriteRenderer.LastInstanceCount,
            coreBodyCount = world.PhysicsCoreLane.BodyCount,
            coreQueryFactCount = world.LastCoreQueryFactCount,
            gameObjectDelta = FindObjectsByType<GameObject>(FindObjectsInactive.Include).Length - baselineGameObjectCount,
            transformDelta = FindObjectsByType<Transform>(FindObjectsInactive.Include).Length - baselineTransformCount,
            monoBehaviourDelta = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include).Length - baselineMonoBehaviourCount,
            spriteRendererDelta = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include).Length - baselineSpriteRendererCount
        };
        report.passed =
            report.elementCount == ElementCount &&
            report.targetCount == TargetCount &&
            report.activeEventHostCount == EventValueElementCount &&
            report.renderedInstanceCount == ElementCount &&
            report.measurementDurationSeconds >= MeasurementSeconds &&
            report.frameTimeP95Ms <= 16.67f &&
            report.maximumElementAllocBytesPerFrame == 0 &&
            report.gameObjectDelta == 0 &&
            report.transformDelta == 0 &&
            report.monoBehaviourDelta == 0 &&
            report.spriteRendererDelta == 0;

        File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
        Debug.Log($"PAN_HIGH_DENSITY_ELEMENT_PERFORMANCE_COMPLETE passed={report.passed} report={reportPath}");
        Application.Quit(report.passed ? 0 : 2);
    }
    private static float CalculateAverage(float[] values, int count)
    {
        if (count <= 0) { return 0f; }
        double total = 0d;
        for (int i = 0; i < count; i++) { total += values[i]; }
        return (float)(total / count);
    }



    private static float Average(double total, int count)
    {
        return count > 0 ? (float)(total / count) : 0f;
    }



    private static double TicksToMilliseconds(long ticks)
    {
        return ticks * 1000d / System.Diagnostics.Stopwatch.Frequency;
    }



    private static double CalculateAverage(double[] values, int count)
    {
        if (count <= 0) { return 0d; }
        double total = 0d;
        for (int i = 0; i < count; i++) { total += values[i]; }
        return total / count;
    }



    private static float Percentile95(float[] sortedValues, int count)
    {
        if (count <= 0) { return 0f; }
        int index = Mathf.Clamp(Mathf.CeilToInt(count * 0.95f) - 1, 0, count - 1);
        return sortedValues[index];
    }
}
