# Unity Library Lab — 시연 초안

Unity **6000.5.6f1**에서 Codex가 제작하는 편집 가능한 샘플입니다. 시작 화면과 다섯 사례의 씬·자체 도형·설정 에셋을 생성했습니다. 기존 사례의 검사·촬영 기록은 이전 버전의 결과입니다. 최신 Stage 씬은 에디터 시연용으로 재구성했으며 생성·시드 테스트를 하지 않았습니다. 제출 완료본은 아닙니다.

## Stage 에디터 시연

`Assets/Portfolio/Scenes/Stage.unity` 또는 `Stage_MultiDoor.unity`를 열고 **Stage Generator**의 기존 Inspector에서 생성합니다. 씬별 설정 SO와 프리팹은 `Assets/Portfolio/StageScenes/`에 있습니다. MultiDoor의 방 13종은 방향마다 3개씩 문 후보를 가지며, 실제 연결 개수는 배치와 설정에 따라 달라집니다. 추가 시연용 기즈모·상시 갱신 코드와 확장·XZ 씬은 포함하지 않습니다. [Stage 편집 안내](STAGE_촬영안내.md)를 참고하세요.

아래 메뉴·Player 자동 검사 안내 중 Stage 항목은 이전 구성에 해당합니다. 최신 두 Stage 씬은 에디터에서 직접 생성해 사용합니다.

## 열기와 편집

1. 아래 로컬 의존성을 설치한 뒤 Unity Hub에서 이 폴더를 엽니다.
2. `Assets/Portfolio/Scenes/Menu.unity`를 열고 Play를 누릅니다.
3. 각 사례의 버튼으로 입력·생성·해제·재사용을 조작합니다.
4. 씬의 `Demo - edit Inspector settings`에서 색상, seed, 요소 수, 속도, 혼합 시간을 수정합니다. Stage의 설정 에셋과 자체 Art 프리팹도 Unity 기본 Inspector에서 편집할 수 있습니다.

`Portfolio > Create Missing Demo Assets and Scenes`는 없는 씬만 생성합니다. 기존 씬을 삭제하거나 다시 덮어쓰지 않습니다. `Assets/Portfolio/Runtime`은 시연 UI·조작 코드이며, 실제 기능 구현은 상위 `Libraries`의 패키지를 호출합니다.

## 사례

| 씬 | 조작·확인 |
| --- | --- |
| Stage | seed 변경, 실제 StageGenerator 생성·초기화, 자체 방·복도·벽 도형 |
| Event | A/B 대상의 기능 부착·해제, 인스턴스 식별, 활성화 예외 후 재시도 |
| Addressables | 자체 Cube 프리팹 로딩, 없는 키, 요청 직후 대상 해제, 완료 결과의 적용 조건과 핸들 해제 |
| Density | 실제 ElementWorld 생성·이동·인스턴싱, 요소 수 조절, Tick CPU 구간 측정 |
| Spine | 자체 최소 뼈대 JSON, Pan SkelObject 모션 전환·방향 반전·재진입·중간 포즈 정지 |

Spine은 일반 전환 시연입니다. 기존 실제 리그의 과거 오류 재현이나 수정 증명으로 표시하지 않습니다.

## 로컬 의존성

Unity 패키지·공개 Git 의존성은 `Packages/manifest.json`과 lock 파일에 고정합니다. 내부 패키지는 `../Libraries`를 참조하며 원본 private Git 인증을 사용하지 않습니다.

로컬 작업 환경에는 정식 Sirenix/Odin, DOTween과 ZLinq 1.5.6 core DLL, PanUtilityEssential 보조 파일을 `Assets/LocalDependencies`에 설치했습니다. 이 폴더는 공개 대상에서 제외합니다. `EnumComparer.cs`, `ReadOnlyAttribute.cs`, `CustomAnimatorCallback.cs`의 출처·배포 조건 확인이 남아 있어 **현재 공개 소스만으로 동일 환경을 바로 재현할 수 있다고 보장하지 않습니다.** 상세 조건은 [의존성 안내](../Docs/dependencies.md)를 참조하세요.

## 검증·촬영 도구

- `Portfolio > Build Windows Development Player`: Addressables 콘텐츠와 Windows Development Player를 빌드합니다.
- `Portfolio > Build Performance Validation Player`: 메인 프로젝트의 성능 검증 코드를 사용하는 별도 Player입니다. 영상 녹화와 분리합니다.
- `Portfolio2026.Editor.PortfolioCapture.Run`: `-portfolioCase Event -portfolioAuto` 등과 함께 실행해 Play Mode 검증·PNG·무음 MP4 초안을 생성합니다. Recorder **5.1.7**, 출력 1440×900, 30 fps를 사용합니다.
- 시연 자동 검증의 `PASS/FAIL`, `AUTO FINISHED`, `PORTFOLIO_CAPTURE_COMPLETE` 로그를 확인합니다. 단순 파일 생성·코드 존재는 실행 성공의 근거가 아닙니다.

촬영 초안은 로컬 `Recordings/`, 실행 파일은 `Builds/`에 생성됩니다. 검증 완료 후 선별한 자료만 공개 문서와 연결합니다.

## Player 자동 검사

빌드한 `Builds/Windows/Portfolio.exe`에 `-portfolioAuto -portfolioCase Stage`를 전달하면 메뉴에서 해당 씬으로 이동해 자동 검사 후 종료합니다. 사례 이름은 Stage, Event, Addressables, Spine, Density입니다. `PlayerVerification/<사례>.json`의 completed·failures와 프로세스 종료 코드를 확인하세요.

렌더링을 검사할 때는 **실제 창을 표시한 일반 Player 실행**을 사용합니다. 창을 숨기거나 batch mode로 실행하면 이 환경에서는 렌더 콜백·ScreenCapture가 실행되지 않아 시뮬레이션만 통과할 수 있습니다. 이 결과를 정상 화면 표시의 증거로 사용하지 않습니다.

`Portfolio > Build Windows Development Player`는 Addressables 콘텐츠를 먼저 빌드한 뒤 Player를 빌드합니다. 기본 Build 메뉴만 사용할 경우에는 Addressables 콘텐츠를 별도로 최신 상태로 빌드해야 합니다.

## 독립 설치 검증 결과

Library 캐시를 복사하지 않은 별도 소스 폴더에서 정식 로컬 의존성을 설치한 뒤 새 가져오기·빌드와 5개 사례의 실제 Player 검사를 통과했습니다. [검사 기록](../Docs/Media/independent-install-verification.json)을 참조하세요. 공개 제외 보조 파일의 배포 조건은 여전히 미확정입니다.
