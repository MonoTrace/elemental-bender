# 검증 기록 — 2026-10-13

- Windows / Python 3.12.14 / MediaPipe 0.10.32 / OpenCV contrib 4.13.0 / NumPy 2.2.6.
- Python 7개 자동 검사 통과: 네 원소 합성 궤적, 정지·한 손 입력 억제, 중립 복귀 재무장, 추적 누락 초기화, UDP 전송·반복 seq, 평가기 미발동 집계.
- 실제 Google Hand Landmarker 모델을 내려받아 VIDEO 모드의 빈 영상 추론 성공. 양손이 없는 결과 확인.
- 의존성 호환성 검사와 Python 문법 검사 통과.
- Unity 6000.6.3f1 URP 템플릿 프로젝트 생성, C# 컴파일, 데모 씬 저장 성공.
- Unity Play 모드 자동 검사: 전투 오브젝트/HUD 시작, FIRE/WATER/AIR/EARTH UDP 이벤트 수신, 각 이벤트 3회 재전송해 총 4회만 발동, EARTH HUD 표시 확인.
- 초기 HUD 기본 글꼴 오류를 발견해 TMP Essential Resources 자동 가져오기·준비 후 Play 시작으로 수정. 데모 생성 시 Play 상태 초기화를 켜 반복 실행의 상태 누적을 방지.

위 검사는 코드/프로토콜 동작 검증입니다. 실제 웹캠 손모양 정확도, 사람의 동작별 성공률, 모든 환경의 오발동률, 전체 지연, 게임 화면의 사용자 사용성은 아직 측정하지 않았습니다. 합성 궤적 검사를 사용자 인식 정확도 100%로 해석하면 안 됩니다. HUD 검사는 생성·텍스트 확인이며 화면 배치의 시각 검수와는 다릅니다.

로컬 세부 로그: `unity-compile.log`, `unity-smoke-play.log`. 배포 ZIP에는 가상환경·Unity 캐시·로그·사용자 데이터를 포함하지 않습니다.
