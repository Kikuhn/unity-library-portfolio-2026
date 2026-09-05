# 정판영 — Unity 라이브러리 포트폴리오

현재 소스의 검토용 스냅샷입니다. 시연 프로젝트·영상·새 실행 검증은 준비 중이며 제출 완료본이 아닙니다.

| 대표 사례 | 설명·코드 | 시연 명세 |
| --- | --- | --- |
| 설정으로 방과 복도를 생성하는 시스템 | [PanStageGenerator2](Docs/stage.md) | [준비할 장면](Docs/demo-specs.md#stage) |
| 재사용하는 기능의 부착과 수명 관리 | [PanEventManager](Docs/event.md) | [준비할 장면](Docs/demo-specs.md#event) |
| 비동기 로딩 결과와 핸들 소유권 | [PanAddressableManager](Docs/addressable.md) | [준비할 장면](Docs/demo-specs.md#addressable) |
| 다수 2D 요소를 데이터와 Job으로 갱신 | [PanHighDensityElement](Docs/density.md) | [준비할 장면](Docs/demo-specs.md#density) |
| 모션 전환 중 방향과 이산 정책의 처리 | [PanSpinePackage](Docs/spine.md) | [준비할 장면](Docs/demo-specs.md#spine) |

[14개 패키지 지도](Docs/inventory.md) · [설치 조건과 공개 범위](Docs/dependencies.md) · [Unity 시연 프로젝트 상태](UnityDemo/README.md)

대표 설명에서 읽을 파일로 바로 이동할 수 있습니다. 라이브러리는 현재 작업 트리에서 선별 복사했으며 원본 Git 이력을 포함하지 않습니다. 원본 API·버전은 변경하지 않았습니다. 제출 버전의 태그는 실제 시연 통합·검증 후 고정합니다.

## 기여와 검증 범위

본인 개발 라이브러리를 기반으로 구성했습니다. 각 설계의 최초 동기, 직접 구현·검증 범위와 AI 지원 범위는 확인 중입니다. 소스와 테스트 존재는 이번 Unity 실행 성공이나 단독 작성의 증거로 간주하지 않습니다. 기존 개발 기록과 새 시연 결과는 구분합니다.
