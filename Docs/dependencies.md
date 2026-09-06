# 설치 조건과 공개 범위

기준 Unity: **6000.5.6f1**. 아래 의존성을 별도로 준비해야 합니다.

## 내부 패키지

UnityDemo에는 필요한 9개 패키지와 전이 의존성을 manifest의 로컬 file 참조로 연결했습니다. 경로는 Unity 프로젝트 Packages 폴더 기준입니다. 예: `"com.kikuhn.panhighdensityelement": "file:../../Libraries/PanHighDensityElement"`. 모든 내부 전이 의존성을 동일 공개 스냅샷에서 해결해야 하며 private Git URL은 사용하지 않습니다. 14개를 무조건 전부 설치하지 않습니다.

원본 package.json의 버전 하한과 스냅샷 버전이 다를 수 있습니다. 특히 구형 PanHighDensityProjectile은 HDE 0.2.0을 선언합니다. 현재 HDE와의 조합을 검증했다고 보장하지 않으며 신규 탄환 관련 구현은 PanTan에 있습니다. 원본 manifest는 수정하지 않았습니다.

## 외부 의존성

- Unity 패키지: Burst, Collections, Mathematics, URP, Physics Core 2D, Addressables, Input System, Behavior 등. 실제 필요한 목록은 패키지 지도와 asmdef를 함께 확인합니다.
- UniTask: manifest에 선언되지만 별도 registry/Git 설치 설정이 필요할 수 있습니다. 설치한 정확한 버전을 lock 파일로 기록합니다.
- Odin Inspector/Sirenix, DOTween, Character Controller Pro: 소스의 암묵적 참조가 있습니다. 해당 사례에 필요한 정식 설치본과 라이선스를 사용합니다. 상용 파일은 이 저장소에 복제하지 않습니다.
- Spine: 해당 Spine runtime 및 필요 라이선스, 공개 가능한 리그가 필요합니다. manifest 최소 버전과 실제 검증 runtime은 구분합니다.

## 제외 및 재현 한계

원본 Git 이력·인증·캐시·에이전트 작업 기록을 포함하지 않습니다. 원본 Samples는 일부 리소스 출처와 참조를 검토해야 하므로 현재 후보에서 제외했습니다. 존재하지 않는 Samples~ 경로로 Package Manager 오류가 발생하여 공개본 7개 package.json에서 해당 samples 항목만 제거했습니다. 버전과 라이브러리 API는 유지했으며 원본 저장소는 변경하지 않았습니다.

PanUtilityEssential의 DLL/LitJson.dll, Scripts/ThirdParty 및 과거 백업, PanUtilityForSpine의 파생 셰이더·에디터는 출처·배포 조건 검토 전까지 제외했습니다. 그 결과 관련 코드의 컴파일 또는 기능이 부족할 수 있습니다. 제거한 기능을 동일하게 제공한다고 주장하지 않습니다. 해당 파일을 포함한 실행 환경은 별도로 준비해야 합니다.

새 오픈소스 라이선스를 부여하지 않았습니다. 공개 열람이 모든 소스·상용 의존성의 자유로운 재배포 허가를 의미하지 않습니다. 각 출처와 기존 고지를 보존해야 합니다.

## 로컬 시연 환경

- Unity Recorder 5.1.7을 고정했습니다. Unity 6000.5에서 컴파일되지 않는 이전 Recorder 5.1.3을 교체했습니다.
- Input Handling은 Both이며 기존 입력 기반 시연 UI와 설치된 Input System을 함께 사용합니다.
- `Assets/LocalDependencies/`에는 정식 로컬 Sirenix, DOTween, ZLinq core DLL 및 원본의 보조 코드 3개를 설치했습니다. 이 폴더는 Git에서 제외합니다.
- 보조 코드 `EnumComparer.cs`, `ReadOnlyAttribute.cs`, `CustomAnimatorCallback.cs`의 재배포 조건은 미확정입니다. 직접 작성한 코드로 표시하거나 공개 저장소에 포함하지 않습니다.
- 외부 의존성을 설치하지 않아도 공개 프로젝트가 즉시 실행된다고 보장하지 않습니다. 별도 위치의 설치 검증은 로컬 의존성을 포함한 조건으로 통과했습니다. 외부 보조 파일의 출처·배포 조건 확정은 남아 있습니다.
