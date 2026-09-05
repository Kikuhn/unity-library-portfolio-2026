# PanSpinePackage — 모션 전환 중 방향과 이산 정책의 처리

## 1. 사용 목적

Spine의 일반 모션 보간을 유지하면서 전환 중 방향 변경과 Plane·반전 같은 이산 정책의 적용을 관리합니다.

## 2. 사용 예

```csharp
// 원본 제약조건 이름에 (IGNORE_BLEND) 태그를 지정하고 다시 export합니다.
// SkelObject.CurrentDB 연결 후 기존 SkelObject.Ani 경로로 재생합니다.
// AnimationIgnoreBlendManager를 임의로 중복 생성하지 않습니다.
```

설정과 의존성이 준비된 호출 형태를 설명하는 예시입니다. 새 Unity 샘플에서 컴파일·실행한 결과는 아직 없습니다.

## 3. 핵심 설계

정책 Timeline 분리 → 일반 모션 native Mix → 이전·현재 정책 평가 → 캐릭터별 전환 보정

AnimationIgnoreBlendManager는 태그된 제약조건에 연결된 정책 Timeline을 메모리에서 분리합니다. 종료 포즈와 정책을 보관하며 방향이 바뀌면 이전과 현재 정책을 각각 새 방향에서 평가합니다. 공유 정책 프로필과 캐릭터별 포즈·버퍼 수명을 분리합니다. 같은 트랙 교체와 시간 배율 처리는 SkelObject의 AniCore 경로가 담당합니다.

## 4. 선택 이유와 한계

특정 Run/Idle 이름에 예외를 붙이지 않고 태그와 정책 그래프를 계약으로 사용합니다. 모든 임의 리그의 연속성을 보장하는 범용 IK 해법은 아니며, 특이행렬·상충 입력 등에는 경고와 정책 적용 fallback이 있습니다. 최초 생성 setup의 장거리 회전까지 해결했다고 확대하지 않습니다.

## 5. 본인 기여

사용자가 문제 증상·기대 동작·일반화 조건을 제시하고 AI와 수정한 후 직접 확인한 대화 기록이 있습니다. 코드 작성과 설계별 구체적인 역할 분담은 추가 확인 중입니다.

## 6. 검증 근거와 읽을 코드

- [Scripts/SkelSbject/SkelSbject_pBoxes.cs](../Libraries/PanSpinePackage/Scripts/SkelSbject/SkelSbject_pBoxes.cs)
- [Scripts/SkelObject/SkelObject_pCoreAni.cs](../Libraries/PanSpinePackage/Scripts/SkelObject/SkelObject_pCoreAni.cs)
- [Tests/Editor/AnimationIgnoreBlendManagerTests.cs](../Libraries/PanSpinePackage/Tests/Editor/AnimationIgnoreBlendManagerTests.cs)

[시연 명세](demo-specs.md#spine) · [테스트 파일 목록](inventory.md)

현재 상태: 구현·호출 흐름 정적 대조. 새 샘플 실행, 화면 촬영, 배포 의존성 검증은 미실행입니다. 성능 수치와 전후 비교 영상은 만들어 넣지 않습니다.
