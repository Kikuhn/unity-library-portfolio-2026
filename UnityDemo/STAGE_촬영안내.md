# Stage 씬 구성

`Assets/Portfolio/Scenes/`에서 목적에 맞는 씬을 연다. **Stage Generator**를 선택하고 기존 StageGenerator2 Inspector의 생성·파괴 버튼과 설정 SO를 사용한다. 별도 촬영용 조작 컴포넌트는 없다.

| 씬 | 구성 | 설정 폴더 |
|---|---|---|
| Stage | 기본 방 3종, 복도·테두리 / 90×90 | Assets/Portfolio/StageScenes/Stage |
| Stage_MultiDoor | 상하좌우 각 3개 문 후보를 가진 방 13종 / 120×120 | Assets/Portfolio/StageScenes/Stage_MultiDoor |

각 폴더의 **Stage.asset / Prefabs.asset / Snap.asset / Calculation.asset**은 씬마다 별개다. 프리팹의 Snap 참조도 해당 씬의 SO로 분리했다. 재질은 공유한다. 방·문 구조를 수정하려면 해당 씬 폴더의 방 프리팹을 연다. 파일명 D/U/L/R 숫자는 아래/위/왼쪽/오른쪽의 보유 문 개수다.

- 씬은 생성 결과 없이 저장했다. 직접 생성해 촬영하면 된다.
- 기타 시연 씬과 원본 라이브러리 코드는 변경하지 않았다.

Stage_MultiDoor의 문 폭은 3, 각 방향의 중심축 위치는 -4 / 0 / 4다. 방 크기는 13×13~19×19이며 문 후보가 실제로 모두 연결된다는 의미는 아니다. 연결 결과는 생성 설정과 주변 방 배치에 따라 달라진다. 기존 생성물에는 변경 전 설정이 남을 수 있으므로 다음 생성부터 새 프리팹을 사용한다.
