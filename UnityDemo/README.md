# Unity 라이브러리 샘플

Unity **6000.5.6f1**에서 사용하는 편집 가능한 샘플 프로젝트입니다. [설치 조건](../Docs/dependencies.md)에 따라 의존성을 준비한 뒤 Unity Hub에서 이 폴더를 엽니다.

## Stage 에디터 시연

| 씬 | 구성 |
| --- | --- |
| [Stage](Assets/Portfolio/Scenes/Stage.unity) | 기본 방·복도·테두리 |
| [Stage_MultiDoor](Assets/Portfolio/Scenes/Stage_MultiDoor.unity) | 방 13종, 상하좌우 각 3개의 문 후보 |

씬에서 **Stage Generator**를 선택하고 기존 StageGenerator2 Inspector의 생성·정리 기능을 사용합니다. 설정 SO와 방 프리팹은 [StageScenes](Assets/Portfolio/StageScenes)에 씬별로 분리되어 있습니다. 문 후보가 모두 연결되는 것은 아니며, 실제 연결은 설정과 주변 방 배치에 따라 달라집니다.

최신 Stage 구성의 생성·시드 테스트는 수행하지 않았습니다. [설정 안내](STAGE_촬영안내.md)에서 리소스 구성을 확인할 수 있습니다.

## 기타 샘플

[Scenes](Assets/Portfolio/Scenes)에는 Event, Addressables, Density, Spine 등의 샘플도 포함되어 있습니다. Menu 씬에서 Play 모드로 접근할 수 있습니다. Stage 두 씬은 위의 에디터 조작 방식을 사용합니다.

[시연 코드](Assets/Portfolio/Runtime)는 상위 [Libraries](../Libraries)의 실제 구현을 호출합니다. 시연용 코드와 도형은 Codex로 제작했습니다. Spine 샘플은 일반 전환 예제이며 과거 게임 리그의 오류 재현 자료는 아닙니다.

## 실행 조건

상용 도구와 일부 보조 코드는 저장소에 포함하지 않았으므로, 내려받은 소스만으로 바로 컴파일·실행할 수 있다고 보장하지 않습니다. 필요한 항목은 [의존성 안내](../Docs/dependencies.md)에 정리했습니다.
