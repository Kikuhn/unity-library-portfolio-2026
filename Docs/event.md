# PanEventManager — 재사용하는 기능의 부착과 수명 관리

## 1. 사용 목적

게임 대상에 기능을 부착하고 활성화·해제한 뒤 풀에서 재사용할 때 소유 대상과 잔여 상태를 관리합니다. 일반적인 메시지 발행·구독 버스와 역할이 다릅니다.

## 2. 사용 예

```csharp
// 프로젝트에서 정의한 DemoEventValue와 초기화된 EventAble이 필요합니다.
// DemoEventValue는 Require의 제네릭 제약을 충족해야 합니다.
var feature = eventAble.Require<DemoEventValue>();
```

설정과 의존성이 준비된 호출 형태를 설명하는 예시입니다. 아래 링크에서 실제 구현을 확인할 수 있습니다.

## 3. 핵심 설계

기존 테이블 조회 → 풀에서 값 확보 → 테이블 예약 → 활성화 → 소유 상태 검증

RequireInternal은 기존 부착값을 먼저 확인합니다. 새 값은 활성화 콜백 전에 테이블에 예약해 같은 타입의 재진입이 같은 인스턴스를 찾게 합니다. 활성화 후에도 테이블이 같은 인스턴스를 소유하는지 검사합니다. 예외 시 자신이 보유한 예약을 제거할 수 있을 때 풀에 반환하고 예외를 전파합니다. 풀과 대상의 테이블은 서로 다른 수명을 가집니다.

## 4. 선택 이유와 한계

활성화 콜백의 재진입과 제거를 고려한 소유권 확인이 핵심입니다. 풀링만 도입하면 해결되는 문제가 아닙니다. 기능 구현자는 Disable/Reset 경로에서 구독과 대상 참조를 정리해야 하며, 제네릭 제약과 라이프사이클 학습 비용이 있습니다. 기능을 컴포넌트처럼 장착·해제하면서 생성·파괴 반복을 피하기 위해 클래스 풀링 구조로 설계했습니다.

## 5. 본인 기여

직접 개발한 라이브러리의 공개 소스입니다. 아래 설명과 링크는 이 스냅샷의 구현을 기준으로 합니다.

## 6. 읽을 코드

- [Scripts/PanEventAble.Mutation.cs](../Libraries/PanEventManager/Scripts/PanEventAble.Mutation.cs)
- [Scripts/PanEventValueManager.cs](../Libraries/PanEventManager/Scripts/PanEventValueManager.cs)
