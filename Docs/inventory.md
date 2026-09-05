# 14개 패키지 지도

2026-09-06 현재 manifest·소스·테스트 파일을 대조했습니다. 테스트 수는 파일 개수이며 통과 개수가 아닙니다. 실제 버전 제약은 각 package.json이 기준입니다.

| 패키지 | 버전 | 역할 | 테스트 파일 |
| --- | --- | --- | ---: |
| [PanAddressableManager](../Libraries/PanAddressableManager/package.json) | 1.1.3 | 로드·캐시·다운로드 | 1 |
| [PanCharacterControllerProExtend](../Libraries/PanCharacterControllerProExtend/package.json) | 1.1.3 | CCP 이동 보조 | 1 |
| [PanEventManager](../Libraries/PanEventManager/package.json) | 2.0.1 | 기능 부착·풀링 | 8 |
| [PanEventManagerPlus](../Libraries/PanEventManagerPlus/package.json) | 1.1.3 | 관찰·연결·상태 확장 | 1 |
| [PanGridCompatible2](../Libraries/PanGridCompatible2/package.json) | 1.1.1 | 그리드 좌표·배치 | 1 |
| [PanHighDensityElement](../Libraries/PanHighDensityElement/package.json) | 0.6.1 | 다수 요소 갱신·질의·표시 | 4 |
| [PanHighDensityElementPanEvent](../Libraries/PanHighDensityElementPanEvent/package.json) | 0.6.1 | HDE와 PanEvent 연결 | 1 |
| [PanHighDensityElementPhysics2DBridge](../Libraries/PanHighDensityElementPhysics2DBridge/package.json) | 0.5.1 | 기존 Collider2D 연결 | 1 |
| [PanHighDensityProjectile](../Libraries/PanHighDensityProjectile/package.json) | 0.5.1 | 구형 호환 패키지 | 7 |
| [PanSpinePackage](../Libraries/PanSpinePackage/package.json) | 1.1.7 | Spine 프레임워크·정책 전환 | 3 |
| [PanStageGenerator2](../Libraries/PanStageGenerator2/package.json) | 1.1.2 | 방·복도 생성 | 5 |
| [PanTan](../Libraries/PanTan/package.json) | 0.5.0 | 현재 탄환 런타임 | 3 |
| [PanUtilityEssential](../Libraries/PanUtilityEssential/package.json) | 1.1.3 | 공통 유틸리티·풀·업데이트 | 5 |
| [PanUtilityForSpine](../Libraries/PanUtilityForSpine/package.json) | 1.1.2 | 하위 Spine 확장 | 1 |

## 패키지별 의존성과 근거

### PanAddressableManager

- 선언 Unity: 6000.0
- 선언 의존성: `com.kikuhn.panutilityessential` 1.0.0, `com.unity.addressables` 2.2.2, `com.cysharp.unitask` 2.5.10
- 원본 문서: `AGENTS.md`, `CHANGELOG.md`, `Documentation~/CODEX_THREAD_ROUTING.md`, `Documentation~/PHASE2_EXECUTION_NOTES.md`, `Documentation~/QUESTIONS_FOR_USER_KO.md`, `Documentation~/REFACTORING_PLAN.md`, `README.md`
- 테스트 근거:
  - [Tests/EditMode/AddressableHandlePolicyEditModeTests.cs](../Libraries/PanAddressableManager/Tests/EditMode/AddressableHandlePolicyEditModeTests.cs)

### PanCharacterControllerProExtend

- 선언 Unity: 6000.0
- 선언 의존성: `com.kikuhn.panutilityessential` 1.0.0
- 원본 문서: `AGENTS.md`, `CHANGELOG.md`, `Documentation~/CODEX_THREAD_ROUTING.md`, `Documentation~/PHASE2_EXECUTION_NOTES.md`, `Documentation~/QUESTIONS_FOR_USER_KO.md`, `Documentation~/REFACTORING_PLAN.md`, `README.md`
- 테스트 근거:
  - [Tests/PhysicsVelocityUtilityTests.cs](../Libraries/PanCharacterControllerProExtend/Tests/PhysicsVelocityUtilityTests.cs)

### PanEventManager

- 선언 Unity: 6000.0
- 선언 의존성: `com.kikuhn.panutilityessential` 1.0.0, `com.cysharp.unitask` 2.5.10
- 원본 문서: `AGENTS.md`, `CHANGELOG.md`, `Documentation~/ARCHITECTURE_AND_LIFECYCLE.md`, `Documentation~/CODEX_THREAD_ROUTING.md`, `Documentation~/PHASE2_EXECUTION_NOTES.md`, `Documentation~/QUESTIONS_FOR_USER_KO.md`, `Documentation~/REFACTORING_PLAN.md`, `Documentation~/ZLINQ_DEPENDENCY.md`, `README.md`
- 테스트 근거:
  - [Tests/EditMode/EventAbleEditorSessionEditModeTests.cs](../Libraries/PanEventManager/Tests/EditMode/EventAbleEditorSessionEditModeTests.cs)
  - [Tests/EditMode/PanEventManagerCoreEditModeTests.cs](../Libraries/PanEventManager/Tests/EditMode/PanEventManagerCoreEditModeTests.cs)
  - [Tests/EditMode/PanEventManagerInspectorEditModeTests.cs](../Libraries/PanEventManager/Tests/EditMode/PanEventManagerInspectorEditModeTests.cs)
  - [Tests/EditMode/PanEventManagerLifecycleEditModeTests.cs](../Libraries/PanEventManager/Tests/EditMode/PanEventManagerLifecycleEditModeTests.cs)
  - [Tests/EditMode/PanEventManagerMutationEditModeTests.cs](../Libraries/PanEventManager/Tests/EditMode/PanEventManagerMutationEditModeTests.cs)
  - [Tests/EditMode/PanEventManagerPoolEditModeTests.cs](../Libraries/PanEventManager/Tests/EditMode/PanEventManagerPoolEditModeTests.cs)
  - [Tests/EditMode/PanEventManagerSignalEditModeTests.cs](../Libraries/PanEventManager/Tests/EditMode/PanEventManagerSignalEditModeTests.cs)
  - [Tests/EditMode/PanEventManagerZLinqDiagnostics.cs](../Libraries/PanEventManager/Tests/EditMode/PanEventManagerZLinqDiagnostics.cs)

### PanEventManagerPlus

- 선언 Unity: 6000.0
- 선언 의존성: `com.kikuhn.panutilityessential` 1.0.0, `com.kikuhn.paneventmanager` 2.0.1
- 원본 문서: `AGENTS.md`, `CHANGELOG.md`, `Documentation~/CODEX_THREAD_ROUTING.md`, `Documentation~/PHASE2_EXECUTION_NOTES.md`, `Documentation~/QUESTIONS_FOR_USER_KO.md`, `Documentation~/REFACTORING_PLAN.md`, `README.md`
- 테스트 근거:
  - [Tests/EditMode/EventLinkLifecycleEditModeTests.cs](../Libraries/PanEventManagerPlus/Tests/EditMode/EventLinkLifecycleEditModeTests.cs)

### PanGridCompatible2

- 선언 Unity: 6000.0
- 선언 의존성: `com.kikuhn.panutilityessential` 1.0.0, `com.unity.mathematics` 1.3.3
- 원본 문서: `AGENTS.md`, `CHANGELOG.md`, `Documentation~/CODEX_THREAD_ROUTING.md`, `Documentation~/PHASE2_EXECUTION_NOTES.md`, `Documentation~/QUESTIONS_FOR_USER_KO.md`, `Documentation~/REFACTORING_PLAN.md`, `README.md`
- 테스트 근거:
  - [Tests/Editor/GridCompatible2Tests.cs](../Libraries/PanGridCompatible2/Tests/Editor/GridCompatible2Tests.cs)

### PanHighDensityElement

- 선언 Unity: 6000.5
- 선언 의존성: `com.unity.burst` 1.8.29, `com.unity.collections` 2.6.6, `com.unity.mathematics` 1.3.3, `com.unity.modules.physicscore2d` 1.0.0, `com.unity.render-pipelines.universal` 17.5.0
- 원본 문서: `AGENTS.md`, `CHANGELOG.md`, `Documentation~/CODEX_THREAD_ROUTING.md`, `Documentation~/MIGRATION_0_5.md`, `Documentation~/MOTION_PRESENTATION_BOUNDARY_0_6.md`, `Documentation~/PHYSICS_CORE_VALIDATION_2026-08-10.md`, `Documentation~/UNITY_65_API_NOTES.md`, `README.md`
- 테스트 근거:
  - [Tests/EditMode/ElementDebuggerEditorEditModeTests.cs](../Libraries/PanHighDensityElement/Tests/EditMode/ElementDebuggerEditorEditModeTests.cs)
  - [Tests/EditMode/ElementDebuggerRuntimeEditModeTests.cs](../Libraries/PanHighDensityElement/Tests/EditMode/ElementDebuggerRuntimeEditModeTests.cs)
  - [Tests/EditMode/ElementWorldEditModeTests.cs](../Libraries/PanHighDensityElement/Tests/EditMode/ElementWorldEditModeTests.cs)
  - [Tests/PlayMode/ElementSpriteRendererPlayModeTests.cs](../Libraries/PanHighDensityElement/Tests/PlayMode/ElementSpriteRendererPlayModeTests.cs)

### PanHighDensityElementPanEvent

- 선언 Unity: 6000.5
- 선언 의존성: `com.kikuhn.panhighdensityelement` 0.6.1, `com.kikuhn.paneventmanager` 2.0.1
- 원본 문서: `AGENTS.md`, `CHANGELOG.md`, `Documentation~/CODEX_THREAD_ROUTING.md`, `Documentation~/EVENT_ELEMENT_LIFECYCLE.md`, `README.md`
- 테스트 근거:
  - [Tests/EditMode/EventElementBridgeEditModeTests.cs](../Libraries/PanHighDensityElementPanEvent/Tests/EditMode/EventElementBridgeEditModeTests.cs)

### PanHighDensityElementPhysics2DBridge

- 선언 Unity: 6000.5
- 선언 의존성: `com.kikuhn.panhighdensityelement` 0.6.1
- 원본 문서: `AGENTS.md`, `CHANGELOG.md`, `Documentation~/CODEX_THREAD_ROUTING.md`, `Documentation~/MIGRATION_0.4.md`, `Documentation~/PROJECTION_SYNC.md`, `Documentation~/REGISTRATION_LAYERS_AND_QUERIES.md`, `README.md`
- 테스트 근거:
  - [Tests/EditMode/Physics2DBridgeRegistryEditModeTests.cs](../Libraries/PanHighDensityElementPhysics2DBridge/Tests/EditMode/Physics2DBridgeRegistryEditModeTests.cs)

### PanHighDensityProjectile

- 선언 Unity: 6000.5
- 선언 의존성: `com.unity.burst` 1.8.29, `com.unity.collections` 2.6.6, `com.unity.mathematics` 1.3.3, `com.kikuhn.panhighdensityelement` 0.2.0
- 원본 문서: `AGENTS.md`, `CHANGELOG.md`, `docs/API_CONTRACTS.md`, `docs/AUTHORING_GUIDE_KO.md`, `docs/GETTING_STARTED.md`, `docs/INTEGRATION_BOUNDARY.md`, `docs/LLM_WIKI.md`, `docs/MIGRATION_SEMVER.md`, `docs/PERFORMANCE_GUIDE.md`, `docs/VALIDATION_MATRIX.md`, `Documentation~/CODEX_THREAD_ROUTING.md`, `README.md`, `Samples~/HighDensity Projectile Validation/README.md`, `Samples~/Quick Start/README.md`
- 테스트 근거:
  - [Tests/EditMode/ElementProjectileBodyEditModeTests.cs](../Libraries/PanHighDensityProjectile/Tests/EditMode/ElementProjectileBodyEditModeTests.cs)
  - [Tests/EditMode/ProjectileEmitterInspectorEditModeTests.cs](../Libraries/PanHighDensityProjectile/Tests/EditMode/ProjectileEmitterInspectorEditModeTests.cs)
  - [Tests/EditMode/ProjectileMotionContractEditModeTests.cs](../Libraries/PanHighDensityProjectile/Tests/EditMode/ProjectileMotionContractEditModeTests.cs)
  - [Tests/EditMode/ProjectilePatternImmediateIdEditModeTests.cs](../Libraries/PanHighDensityProjectile/Tests/EditMode/ProjectilePatternImmediateIdEditModeTests.cs)
  - [Tests/EditMode/ProjectileRelayInspectorEditModeTests.cs](../Libraries/PanHighDensityProjectile/Tests/EditMode/ProjectileRelayInspectorEditModeTests.cs)
  - [Tests/EditMode/ProjectileRenderProxyInspectorEditModeTests.cs](../Libraries/PanHighDensityProjectile/Tests/EditMode/ProjectileRenderProxyInspectorEditModeTests.cs)
  - [Tests/PlayMode/ProjectileInstancedSpritePixelPlayModeTests.cs](../Libraries/PanHighDensityProjectile/Tests/PlayMode/ProjectileInstancedSpritePixelPlayModeTests.cs)

### PanSpinePackage

- 선언 Unity: 6000.0
- 선언 의존성: `com.unity.inputsystem` 1.11.2, `com.esotericsoftware.spine.spine-csharp` 4.3.25, `com.esotericsoftware.spine.spine-unity` 4.3.0, `com.kikuhn.panutilityessential` 1.0.0, `com.kikuhn.panutilityforspine` 1.0.0, `com.kikuhn.paneventmanager` 2.0.1
- 원본 문서: `AGENTS.md`, `CHANGELOG.md`, `Documentation~/CODEX_THREAD_ROUTING.md`, `Documentation~/PHASE2_EXECUTION_NOTES.md`, `Documentation~/PLANE_BLEND_GUIDE.md`, `Documentation~/QUESTIONS_FOR_USER_KO.md`, `Documentation~/REFACTORING_PLAN.md`, `Documentation~/ZLINQ_DEPENDENCY.md`, `README.md`
- 테스트 근거:
  - [Tests/Editor/AnimationIgnoreBlendManagerTests.cs](../Libraries/PanSpinePackage/Tests/Editor/AnimationIgnoreBlendManagerTests.cs)
  - [Tests/Editor/EnumIndexTests.cs](../Libraries/PanSpinePackage/Tests/Editor/EnumIndexTests.cs)
  - [Tests/Editor/RunTimeSkinManagerTests.cs](../Libraries/PanSpinePackage/Tests/Editor/RunTimeSkinManagerTests.cs)

### PanStageGenerator2

- 선언 Unity: 6000.0
- 선언 의존성: `com.kikuhn.panutilityessential` 1.0.0, `com.kikuhn.pangridcompatible2` 1.0.0, `com.cysharp.unitask` 2.5.10, `com.unity.mathematics` 1.3.3, `com.unity.collections` 2.6.6, `com.unity.burst` 1.8.29
- 원본 문서: `AGENTS.md`, `CHANGELOG.md`, `Documentation~/CODEX_THREAD_ROUTING.md`, `Documentation~/PHASE2_EXECUTION_NOTES.md`, `Documentation~/QUESTIONS_FOR_USER_KO.md`, `Documentation~/REFACTORING_PLAN.md`, `Documentation~/ZLINQ_DEPENDENCY.md`, `README.md`
- 테스트 근거:
  - [Tests/Editor/DoorManagerOptimizationTests.cs](../Libraries/PanStageGenerator2/Tests/Editor/DoorManagerOptimizationTests.cs)
  - [Tests/Editor/GenerateRetryPolicyTests.cs](../Libraries/PanStageGenerator2/Tests/Editor/GenerateRetryPolicyTests.cs)
  - [Tests/Editor/StageGenerationAlgorithmTests.cs](../Libraries/PanStageGenerator2/Tests/Editor/StageGenerationAlgorithmTests.cs)
  - [Tests/Editor/StageGenerationPerformanceDiagnostics.cs](../Libraries/PanStageGenerator2/Tests/Editor/StageGenerationPerformanceDiagnostics.cs)
  - [Tests/PlayMode/StageDestroyLifecycleTests.cs](../Libraries/PanStageGenerator2/Tests/PlayMode/StageDestroyLifecycleTests.cs)

### PanTan

- 선언 Unity: 6000.5
- 선언 의존성: `com.unity.mathematics` 1.3.3, `com.unity.modules.physics2d` 1.0.0, `com.kikuhn.panhighdensityelement` 0.6.0
- 원본 문서: `AGENTS.md`, `CHANGELOG.md`, `Documentation~/API_CONTRACTS.md`, `Documentation~/CODEX_THREAD_ROUTING.md`, `Documentation~/DATA_LAYOUT.md`, `Documentation~/MIGRATION_0_3.md`, `Documentation~/MIGRATION_0_4.md`, `Documentation~/MIGRATION_0_5.md`, `Documentation~/VALIDATION_MATRIX.md`, `PanEvent~/CHANGELOG.md`, `PanEvent~/README.md`, `README.md`
- 테스트 근거:
  - [PanEvent~/Tests/EditMode/EventTanBridgeEditModeTests.cs](../Libraries/PanTan/PanEvent~/Tests/EditMode/EventTanBridgeEditModeTests.cs)
  - [Tests/EditMode/ElementTanRuntimeEditModeTests.cs](../Libraries/PanTan/Tests/EditMode/ElementTanRuntimeEditModeTests.cs)
  - [Tests/EditMode/NormalTanContractsEditModeTests.cs](../Libraries/PanTan/Tests/EditMode/NormalTanContractsEditModeTests.cs)

### PanUtilityEssential

- 선언 Unity: 6000.0
- 선언 의존성: `com.unity.inputsystem` 1.11.2, `com.cysharp.unitask` 2.5.10, `com.unity.nuget.newtonsoft-json` 3.2.1, `com.unity.addressables` 2.2.2, `com.unity.behavior` 1.0.12, `com.unity.mathematics` 1.3.3, `com.unity.collections` 2.6.6, `com.unity.burst` 1.8.29
- 원본 문서: `AGENTS.md`, `CHANGELOG.md`, `Documentation~/CODEX_THREAD_ROUTING.md`, `Documentation~/PHASE2_EXECUTION_NOTES.md`, `Documentation~/QUESTIONS_FOR_USER_KO.md`, `Documentation~/REFACTORING_PLAN.md`, `Documentation~/ZLINQ_DEPENDENCY.md`, `README.md`
- 테스트 근거:
  - [Tests/Editor/InfinityStackEditorCacheTests.cs](../Libraries/PanUtilityEssential/Tests/Editor/InfinityStackEditorCacheTests.cs)
  - [Tests/EditorOnly/CollectionPerformanceTester.cs](../Libraries/PanUtilityEssential/Tests/EditorOnly/CollectionPerformanceTester.cs)
  - [Tests/EditorOnly/ManualMemoryControl.cs](../Libraries/PanUtilityEssential/Tests/EditorOnly/ManualMemoryControl.cs)
  - [Tests/Scripts/TimeUpdateEventTests.cs](../Libraries/PanUtilityEssential/Tests/Scripts/TimeUpdateEventTests.cs)
  - [Tests/Scripts/ZLinqHotPathTests.cs](../Libraries/PanUtilityEssential/Tests/Scripts/ZLinqHotPathTests.cs)

### PanUtilityForSpine

- 선언 Unity: 2022.3
- 선언 의존성: `com.esotericsoftware.spine.spine-csharp` 4.3.25, `com.esotericsoftware.spine.spine-unity` 4.3.0
- 원본 문서: `AGENTS.md`, `CHANGELOG.md`, `Documentation~/CODEX_THREAD_ROUTING.md`, `Documentation~/PHASE2_EXECUTION_NOTES.md`, `Documentation~/QUESTIONS_FOR_USER_KO.md`, `Documentation~/REFACTORING_PLAN.md`, `README.md`
- 테스트 근거:
  - [Tests/Editor/SpineExtensionTests.cs](../Libraries/PanUtilityForSpine/Tests/Editor/SpineExtensionTests.cs)
