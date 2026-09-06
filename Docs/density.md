# PanHighDensityElement — 다수 2D 요소를 데이터와 Job으로 갱신

## 1. 사용 목적

요소마다 GameObject를 두지 않고 다수의 2D 요소를 이동·질의·표시하는 런타임입니다.

## 2. 사용 예

```csharp
// 초기화된 world와 컴파일된 archetype이 필요합니다.
var builder = ElementSpawnBuilder.From(archetype)
    .WithPose(new float2(0, 0), new float2(1, 1))
    .WithVelocity(new float2(1, 0));
var result = world.Spawn(builder);
// 소유자의 고정 갱신 경계에서 호출
world.Tick(fixedDeltaTime);
```

설정과 의존성이 준비된 호출 형태를 설명하는 예시입니다. 새 Unity 샘플에서 이 API 흐름의 생성·갱신·표시를 검증했습니다.

## 3. 핵심 설계

요소 상태 배열 → lane별 이동 Job 예약 → 의존 Job 완료 → 물리 질의·동기화 → 결과와 표시

ElementMotionJob은 Burst와 IJobParallelFor, NativeContainer를 사용합니다. Tick은 kinematic/area/physics lane의 이동 작업을 예약하고 finally에서 결합된 작업의 완료를 보장합니다. Physics Core 2D world와 bodyless 질의, 특수 물리 lane을 구분합니다. WorldId·Slot·Generation 식별자는 재사용된 슬롯과 이전 참조를 구별합니다. 핵심 패키지와 PanEvent 연동·기존 Physics2D bridge·PanTan은 별도 패키지입니다.

## 4. 선택 이유와 한계

핵심 데이터 갱신을 게임별 이벤트나 대상 Transform 수명에서 분리한 구조입니다. Job 완료 대기·질의·렌더링 비용은 남으며 Burst를 사용했다는 사실이 성능 우위를 증명하지 않습니다. PanHighDensityProjectile은 구형 호환 패키지이므로 신규 시연의 기본 API로 삼지 않습니다.

## 5. 본인 기여

직접 개발한 라이브러리의 공개 소스입니다. 아래 설명과 링크는 이 스냅샷의 구현을 기준으로 합니다.

## 6. 읽을 코드

- [Runtime/ElementJobs.cs](../Libraries/PanHighDensityElement/Runtime/ElementJobs.cs)
- [Runtime/ElementWorld.Tick.cs](../Libraries/PanHighDensityElement/Runtime/ElementWorld.Tick.cs)
- [Runtime/ElementWorld.SpawnLifecycle.cs](../Libraries/PanHighDensityElement/Runtime/ElementWorld.SpawnLifecycle.cs)
- [Runtime/ElementArchetype.cs](../Libraries/PanHighDensityElement/Runtime/ElementArchetype.cs)
