# 정판영 — Unity 라이브러리 포트폴리오

게임 개발에 사용한 Unity 라이브러리의 소스와 기술 설명을 모았습니다. 원본 개발 저장소에서 분리한 공개 스냅샷입니다.

## 주요 구현

| 라이브러리 | 주요 내용 | 기술 설명 |
| --- | --- | --- |
| PanStageGenerator2 | 설정과 생성 단계를 분리한 방·복도 생성 시스템 | [설명과 코드](Docs/stage.md) |
| PanEventManager | 기능 객체의 장착·해제와 클래스 풀링 기반 재사용 | [설명과 코드](Docs/event.md) |
| PanSpinePackage | Spine 런타임 확장과 데이터·동작 관리 | [설명과 코드](Docs/spine.md) |
| PanAddressableManager | 비동기 로딩 결과와 핸들 수명 관리 | [설명과 코드](Docs/addressable.md) |
| PanHighDensityElement | 다수 요소의 데이터·Job 기반 갱신 | [설명과 코드](Docs/density.md) |

## 저장소 구성

- [Libraries](Libraries): 라이브러리 소스. [전체 패키지 목록](Docs/inventory.md)에서 의존성과 주요 파일을 확인할 수 있습니다.
- [Docs](Docs): 사용 예와 핵심 설계 설명.
- [UnityDemo](UnityDemo): 편집 가능한 Unity 샘플. Stage와 Stage_MultiDoor는 기존 StageGenerator2 Inspector로 생성하는 에디터 시연용 씬입니다.

## 개발과 공개 범위

라이브러리는 직접 개발한 코드를 기반으로 합니다. AI는 기존 코드 분석·리팩토링·문서화에 활용했으며, AI 구현을 적극 활용한 부분에서는 아키텍처를 설계하고 세부 구현을 맡겼습니다. Unity 시연용 코드와 도형은 Codex로 제작했습니다.

실행에는 Unity **6000.5.6f1**과 별도 의존성이 필요합니다. 상용 도구와 재배포 조건이 확인되지 않은 보조 파일은 포함하지 않았습니다. [설치 조건과 공개 범위](Docs/dependencies.md)를 먼저 확인해 주세요.
