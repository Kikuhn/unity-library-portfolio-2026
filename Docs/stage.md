# PanStageGenerator2 — 설정으로 방과 복도를 생성하는 시스템

## 1. 사용 목적

반복 제작하는 맵에서 방의 배치, 공간 간 연결, 실제 복도 경로를 단계별로 구성합니다.

## 2. 사용 예

```csharp
// 설정과 방 리소스가 연결된 GenerateManager를 전달합니다.
// 호출 예시이며 독립 실행 샘플이 아닙니다.
bool succeeded = await generateManager.GenerateAsync(customSeed: 12345);
```

설정과 의존성이 준비된 호출 형태를 설명하는 예시입니다. 아래 링크에서 실제 구현을 확인할 수 있습니다.

## 3. 핵심 설계

GenerateAsync → 공간 생성 → 공간 연결 선택 → 복도 경로 탐색 → 배치 결과 적용

공간 연결은 후보 간선의 비용을 정렬하고 Union-Find로 순환 여부를 판정하는 최소 신장 트리 구조입니다. 복도의 실제 이동 경로는 비용과 휴리스틱, IndexedPriorityQueue를 사용하는 A*로 구합니다. 방 배치 좌표 이동 일부는 Burst와 IJobParallelForTransform에 맡기며 예약한 작업의 완료와 NativeArray 해제를 관리합니다.

## 4. 선택 이유와 한계

연결 관계와 통로 형상을 분리하면 각각의 규칙을 설명하고 수정하기 쉽습니다. MST만으로 게임의 재미나 모든 배치의 유효성이 보장되지는 않으며, A*도 탐색 경계와 금지 영역 설정에 영향을 받습니다. 비동기 진입점이 모든 계산의 병렬 실행을 뜻하지는 않습니다.

## 5. 본인 기여

직접 개발한 라이브러리의 공개 소스입니다. 아래 설명과 링크는 이 스냅샷의 구현을 기준으로 합니다.

## 6. 읽을 코드

- [StageGenerator/StageGenerator_pGenerateManager.cs](../Libraries/PanStageGenerator2/StageGenerator/StageGenerator_pGenerateManager.cs)
- [StageGenerator/StageGenerator_pSpaceManager.cs](../Libraries/PanStageGenerator2/StageGenerator/StageGenerator_pSpaceManager.cs)
- [StageGenerator/StageGenerator_pPlaceManager_pGen.cs](../Libraries/PanStageGenerator2/StageGenerator/StageGenerator_pPlaceManager_pGen.cs)
- [StageGenerator/StageGenerator_pPlaceManager.cs](../Libraries/PanStageGenerator2/StageGenerator/StageGenerator_pPlaceManager.cs)

## Unity 샘플

[Stage와 Stage_MultiDoor](../UnityDemo/README.md)는 기존 Inspector에서 생성하는 에디터 시연용 씬입니다. 최신 구성의 생성·시드 테스트는 수행하지 않았습니다.
