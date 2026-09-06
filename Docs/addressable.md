# PanAddressableManager — 비동기 로딩 결과와 핸들 소유권

## 1. 사용 목적

Addressables의 로딩·캐시·다운로드 접근을 묶고, 호출부가 성공·실패·대상 해제 시점의 핸들 소유권을 추적할 수 있게 합니다.

## 2. 사용 예

```csharp
// 핸들 반환 API의 실제 사용 형태. 적용·해제 정책은 호출부 책임입니다.
var handle = PanAddressableNative.LoadAssetAsyncHandle<GameObject>(key);
// 완료 후 IsValid/Status와 대상 수명을 확인하고 사용합니다.
// 소유자가 더 이상 사용하지 않을 때 유효한 핸들을 한 번 반환합니다.
```

설정과 의존성이 준비된 호출 형태를 설명하는 예시입니다. 아래 링크에서 실제 구현을 확인할 수 있습니다.

## 3. 핵심 설계

핸들 반환 래퍼 → 비동기 완료 → 호출 대상의 현재 수명 확인 → 적용 또는 반환

PanAddressableNative.LoadAssetAsyncHandle은 Addressables.LoadAssetAsync를 반환하는 얇은 경계입니다. 대상이 아직 살아 있는지를 래퍼가 자동 보장하지 않습니다. 실제 소비 프로젝트의 Behavior Graph 초기화에서는 수명 버전·현재 대상·액션을 await 이후 재확인하고 실패 및 해제 경로에서 핸들을 반환합니다. 이 소비 프로젝트 코드는 14개 패키지에 포함되지 않으므로 공개 샘플에서 같은 소유권 규칙을 재현해야 합니다.

## 4. 선택 이유와 한계

공용 로딩 API와 게임별 결과 적용 정책을 분리합니다. 관리자의 캐시는 해당 관리자를 거친 로드의 기록이며 전체 Addressables 의존성 프로파일러가 아닙니다. 동기 WaitForCompletion 경로도 있으므로 모든 API를 비동기라고 설명하지 않습니다. 취소 요청이 내부 로드 완료나 자동 반환을 보장한다고 가정하지 않습니다.

## 5. 본인 기여

직접 개발한 라이브러리의 공개 소스입니다. 아래 설명과 링크는 이 스냅샷의 구현을 기준으로 합니다.

## 6. 읽을 코드

- [PanAddressableNative.cs](../Libraries/PanAddressableManager/PanAddressableNative.cs)
- [PanAddressableManager.cs](../Libraries/PanAddressableManager/PanAddressableManager.cs)
