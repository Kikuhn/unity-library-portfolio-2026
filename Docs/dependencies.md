# 설치 조건과 공개 범위

기준 Unity: **6000.5.6f1**. 현재 자료는 소스 검토 후보이며 완성된 재현 프로젝트가 아닙니다.

## 내부 패키지

UnityDemo를 만든 뒤 필요한 패키지와 전이 의존성을 프로젝트 manifest의 로컬 file 참조로 연결합니다. 경로는 Unity 프로젝트 Packages 폴더 기준입니다. 예: `"com.kikuhn.panhighdensityelement": "file:../../Libraries/PanHighDensityElement"`. 모든 내부 전이 의존성을 동일 공개 스냅샷에서 해결해야 하며 private Git URL은 사용하지 않습니다. 14개를 무조건 전부 설치하지 않습니다.

원본 package.json의 버전 하한과 스냅샷 버전이 다를 수 있습니다. 특히 구형 PanHighDensityProjectile은 HDE 0.2.0을 선언합니다. 현재 HDE와의 조합을 검증했다고 보장하지 않으며 신규 탄환 시연은 PanTan을 우선 검토합니다. 원본 manifest는 수정하지 않았습니다.

## 외부 의존성

- Unity 패키지: Burst, Collections, Mathematics, URP, Physics Core 2D, Addressables, Input System, Behavior 등. 실제 필요한 목록은 패키지 지도와 asmdef를 함께 확인합니다.
- UniTask: manifest에 선언되지만 별도 registry/Git 설치 설정이 필요할 수 있습니다. 설치한 정확한 버전을 lock 파일로 기록합니다.
- Odin Inspector/Sirenix, DOTween, Character Controller Pro: 소스의 암묵적 참조가 있습니다. 해당 사례에 필요한 정식 설치본과 라이선스를 사용합니다. 상용 파일은 이 저장소에 복제하지 않습니다.
- Spine: 해당 Spine runtime 및 필요 라이선스, 공개 가능한 리그가 필요합니다. manifest 최소 버전과 실제 검증 runtime은 구분합니다.

## 제외 및 재현 한계

원본 Git 이력·인증·캐시·에이전트 작업 기록을 포함하지 않습니다. 원본 Samples는 일부 리소스 출처와 참조를 검토해야 하므로 현재 후보에서 제외했습니다. Samples 항목이 manifest에 남아 있어 원본 샘플 가져오기는 완성되지 않은 상태입니다.

PanUtilityEssential의 DLL/LitJson.dll, Scripts/ThirdParty 및 과거 백업, PanUtilityForSpine의 파생 셰이더·에디터는 출처·배포 조건 검토 전까지 제외했습니다. 그 결과 관련 코드의 컴파일 또는 기능이 부족할 수 있습니다. 제거한 기능을 동일하게 제공한다고 주장하지 않습니다. 임의 대체 구현이나 원본 API 변경 없이 별도 정식 설치·공개 허용 파일 복원 여부를 확정한 뒤 재현 검증합니다.

새 오픈소스 라이선스를 부여하지 않았습니다. 공개 열람이 모든 소스·상용 의존성의 자유로운 재배포 허가를 의미하지 않습니다. 각 출처와 기존 고지를 보존해야 합니다.
