# Stage 씬 구성

`Assets/Portfolio/Scenes/`에서 목적에 맞는 씬을 연다. **Stage Generator**를 선택하고 기존 StageGenerator2 Inspector의 생성·파괴 버튼과 설정 SO를 사용한다. 별도 촬영용 조작 컴포넌트는 없다.

| 씬 | 구성 | 설정 폴더 |
|---|---|---|
| Stage | 기본 방 3종, 복도·테두리 / 90×90 | Assets/Portfolio/StageScenes/Stage |
| Stage_MultiDoor | 상하좌우 각 3개 문 후보를 가진 방 13종 / 120×120 | Assets/Portfolio/StageScenes/Stage_MultiDoor |

각 폴더의 **Stage.asset / Prefabs.asset / Snap.asset / Calculation.asset**은 씬마다 별개다. 프리팹의 Snap 참조도 해당 씬의 SO로 분리했다. 재질은 공유한다. 방·문 구조를 수정하려면 해당 씬 폴더의 방 프리팹을 연다. 파일명 D/U/L/R 숫자는 아래/위/왼쪽/오른쪽의 보유 문 개수다.

- 씬은 생성 결과 없이 저장했다. 직접 생성해 촬영하면 된다.
- 제가 추가했던 문 수 라벨, 문 표시 기즈모, 매 프레임 방·문 순회, Stage Showcase 조작부와 시드 탐색·검증 메뉴를 삭제했다. 기존 라이브러리의 Inspector와 Gizmos는 그대로다.
- 기타 시연 씬과 원본 라이브러리 코드는 변경하지 않았다.
- 이 재구성에서는 맵 생성, 시드 탐색, 성능 측정을 실행하지 않았다. 이전 검증 결과는 새 씬 구성의 검증 결과가 아니다.
- 이전 씬과 제거한 보조 코드의 보관 위치: 프로젝트 옆 `../StageShowcaseBackup/before-scene-split/`, `../StageShowcaseBackup/removed-editor-controls/`.

Stage_MultiDoor의 문 폭은 3, 각 방향의 중심축 위치는 -4 / 0 / 4다. 방 크기는 13×13~19×19이며 문 후보가 실제로 모두 연결된다는 의미는 아니다. 연결 결과는 생성 설정과 주변 방 배치에 따라 달라진다. 기존 생성물에는 변경 전 설정이 남을 수 있으므로 다음 생성부터 새 프리팹을 사용한다.

확장·XZ 씬과 이전 중복 리소스는 프로젝트 밖 `../StageShowcaseBackup/removed-extra-stage-assets/`로 보관했다. 두 씬이 참조하는 공용 재질과 라이브러리는 유지한다.
