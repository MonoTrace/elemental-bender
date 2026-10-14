# ELEMENTAL BENDER — 양손 컴퓨터비전 미니프로젝트

웹캠 → Python MediaPipe 양손 21개 랜드마크 → 손모양 + 시간창 규칙 → UDP JSON → Unity 메인 스레드 → 4원소 전투.

Windows 데스크톱, 단일 플레이어, 고정 카메라, 같은 PC의 Python/Unity를 대상으로 하는 실행 가능한 MVP입니다. 자체 제스처 학습 모델은 아직 없으며 MediaPipe의 사전학습 손 추정 모델 위에 규칙 기반 분류기를 얹습니다. 온라인 서비스/유료 에셋은 필요하지 않습니다. `sources/`의 참고 자료는 수정하지 않았습니다.

## 빠른 실행

이 폴더를 PowerShell에서 연 뒤 진행합니다. Python 3.12 64비트와 Unity가 필요합니다. 이 컴퓨터에는 `.venv` 설치와 Unity 6000.6.3f1용 `UnityProject` 생성까지 진행했습니다. 다른 PC에서는 Python 설치 시 Python launcher도 설치하세요.

```powershell
powershell -ExecutionPolicy Bypass -File .\setup-python.ps1
```

설치 스크립트는 이 PC에서 검증한 `python/requirements-lock.txt`를 사용합니다. 핵심 직접 의존성은 `requirements.txt`에 따로 정리했습니다. ZIP에는 가상환경과 모델 파일이 제외되므로 압축을 푼 PC에서는 설치와 첫 모델 다운로드가 필요합니다.

1. Unity Hub → Add → **Add project from disk** → 이 폴더의 `UnityProject`를 선택하고 열기. 설치된 프로젝트 버전은 `6000.6.3f1`입니다.
2. `Assets/Scenes/ElementalDemo.unity`를 열기. 없으면 메뉴 **Tools → Elemental Bender → Create Demo Scene** 실행. 현재 씬에 수정 내용이 있다면 저장 여부를 묻습니다.
3. TMP 글꼴 경고가 있으면 **Window → TextMeshPro → Import TMP Essential Resources** 실행. 기본 글꼴이 없을 때 Arial 기반 동적 글꼴을 사용하는 대체 경로도 있습니다.
4. Unity **Play**. Game 탭을 선택하면 HP·처치 수·신호 상태·쿨다운과 접근하는 적이 보입니다.
5. 먼저 웹캠 없이 연결 확인:

```powershell
powershell -ExecutionPolicy Bypass -File .\run-mock.ps1
```

불·물·공기·흙 패킷을 차례로 전송합니다. HUD의 Last 값과 효과가 바뀌면 통신·스킬 경로가 연결된 것입니다. 사거리 밖의 적은 피해를 받지 않습니다.

6. 실제 인식 실행:

```powershell
powershell -ExecutionPolicy Bypass -File .\run-camera.ps1
```

첫 실행에는 Google 공식 모델 약 8MB를 다운로드합니다. 이 PC에서는 환경 검증 중 다운로드할 수 있습니다. 이후에는 로컬 모델을 사용합니다. 카메라 미리보기에서 Q를 누르면 종료합니다. 카메라 번호가 다르면 `run-camera.ps1 --camera 1`을 사용하세요. 웹캠 미리보기는 기본적으로 거울처럼 좌우 반전하며 랜드마크도 함께 반전합니다. 글자는 정상 방향으로 표시합니다. 인식·기록 좌표는 원본을 유지합니다. 원본 미리보기는 `--no-mirror-preview`로 선택할 수 있으며 영상 파일은 반전하지 않습니다.

다음 전투 설계는 [COMBAT_DIRECTION.md](COMBAT_DIRECTION.md)를 따릅니다. 현재 네 제스처는 임시 규칙이며 사용자가 새 공격·방어 동작을 정의한 뒤 교체합니다. 현재 실행 게임에는 아래에 설명한 접근형 적이 남아 있고, 새 고정 거리·상성 방어·사람 모델·애니메이션 전투는 아직 구현되지 않았습니다.

양손이 모두 보이도록 카메라를 가슴 높이에 두고, 손바닥/주먹이 카메라를 향하도록 합니다. 각 제스처 후 손을 내려 화면 밖으로 빼거나 양손의 손모양을 다르게 해 **0.25초 이상 중립 상태**를 만드세요. 단순히 같은 손모양으로 정지하면 재무장되지 않습니다. 전체 인식 쿨다운은 1.2초입니다.

## 제스처 규칙과 전투

| 원소 | 사용자가 수행할 동작 | 현재 규칙의 초기 임계값 | 전투 효과 |
|---|---|---|---|
| 불 FIRE | 양손 주먹을 가슴 앞에서 좌우로 벌리기 | 각 손이 반대 방향으로 화면 폭의 9% 이상 이동, 수직 이동 12% 미만 | 전방 범위 피해 60 |
| 물 WATER | 양손을 펼치고 각각 원을 한 바퀴 그리기 | 각 손의 가로·세로 범위 12% 이상, 회전 5rad 이상, 방향 일관성·닫힌 경로 확인 | 피해 25 + 3초 감속 |
| 공기 AIR | 양손을 펼치고 카메라 쪽으로 밀기 | 양손 크기가 시작 대비 각각 1.4배 이상, 과도한 평면 이동 제외 | 피해 15 + 3m 밀쳐내기 |
| 흙 EARTH | 양손 주먹을 동시에 위로 올리기 | 각각 화면 높이의 13% 이상 위로 이동, 가로 이동 12% 미만 | 피해 35 + 2초 기절 + 3초 방어 |

손 크기는 손목–중지 MCP 사이의 영상 거리입니다. 손모양은 엄지를 제외한 네 손가락의 관절 각도와 손끝 거리로 판정합니다. 3개 이상 펴지면 OPEN, 모두 접히면 FIST, 나머지는 OTHER입니다. 엄지는 MVP에서 판정하지 않습니다.

시간창 1.4초, 최소 8프레임·0.35초를 사용합니다. 양손이 같은 유효 손모양이어야 합니다. 손이 사라지거나 손모양이 바뀌면 궤적을 초기화하고, 프레임 간 긴 공백/큰 위치 변화도 초기화합니다. 화면 좌우 순서로 손을 연결하므로 손 교차·가림은 지원 범위에서 제외합니다. 한 바퀴는 약 0.8–1.3초, 올리기/벌리기/밀기는 약 0.5–1초에 수행하세요. `python/gestures.py`에서 임계값을 조정할 수 있습니다.

**공기는 실제 깊이 측정이 아닙니다.** 단일 웹캠 영상에서 손이 커지는 정도로 전방 움직임을 추정합니다. 손바닥 회전, 몸 전체의 전진도 오인식 원인이므로 평면을 유지하고 몸을 고정하세요. 일반적인 ‘손 z 값’을 두 손의 절대 깊이로 사용하지 않습니다. 추후 몸통 기준 상대 이동·개인별 시작 크기 보정 또는 깊이 카메라를 검토합니다.

전투는 **플레이어와 적 한 명의 1대1**입니다. 적 HP 300, 플레이어 HP 100입니다. 현재 기본 화면은 플레이어(-2,0,0)와 적(2,0,6)이 약 7.2m 떨어져 제자리를 유지하는 구도 확인용 모드입니다. 플레이어는 왼쪽 아래 뒷모습, 적은 오른쪽 위 정면으로 보이며 아래에 상태창과 양측 HP가 표시됩니다. 기존 제스처에 따른 적 피해는 시연할 수 있지만 원소 상성 공격·방어와 애니메이션은 아직 연결하지 않았습니다. 적 HP가 0이면 VICTORY입니다. Inspector의 `Fixed Duel Staging`을 끄면 이전 접근·근접 공격 테스트가 동작합니다. 불·물·공기 쿨다운은 1.5초, 흙은 4초입니다. 게임 종료 후 Stop → Play로 재시작합니다.

## Unity 안에서 웹캠 보기 / ON·OFF

Python이 웹캠을 한 번만 열고, 원소 이벤트는 기존 UDP 5055, 좌우 반전된 미리보기는 로컬 HTTP 5056으로 Unity에 전달합니다. Unity에서 웹캠을 직접 다시 열지 않으므로 두 프로그램이 같은 장치를 점유하지 않습니다. Unity의 RawImage는 최신 JPEG를 약 12FPS로 표시하며 영상 비율을 유지합니다.

```powershell
powershell -ExecutionPolicy Bypass -File .\run-unity-camera.ps1
```

Unity에서 Play 후 오른쪽 위 **WEBCAM: ON / OFF** 버튼으로 미리보기를 켜고 끕니다. OFF에서는 영상 요청과 패널 표시만 중지하고 Python 손 인식·원소 입력은 계속됩니다. Python만 종료하면 `Waiting for Python camera`가 표시되고, 다시 실행하면 자동 재연결됩니다. Python은 터미널에서 Ctrl+C로 종료할 수 있습니다. 웹캠의 완전한 종료와 영상 숨기기는 별개입니다.

`run-unity-camera.ps1`은 별도 OpenCV 창 없이 실행합니다. 기존 `run-camera.ps1`은 별도 창도 함께 표시합니다. HTTP 기능을 끄려면 Python에 `--no-unity-preview`를 전달하세요. 충돌 시 `--preview-port`와 Unity의 WebcamPreview `Frame Url` 포트를 함께 바꾸세요. 프레임은 기본적으로 메모리에만 보관하고 서버는 `127.0.0.1`에만 바인딩합니다. 미리보기는 Unity Editor/같은 PC 데스크톱 실행 기준입니다. 배포 빌드에서 HTTP 정책에 막히면 localhost HTTP 허용 여부를 따로 확인해야 합니다.

사람 모델은 지금 도형을 조합한 임시 외형입니다. 실제 모델을 사용할 때는 ElementalArena의 `Player Model Prefab` / `Enemy Model Prefab`에 캐릭터 프리팹을 연결하면 됩니다. 바닥이 원점이고 앞 방향이 +Z인 프리팹을 준비하세요. 자동 애니메이션/리타게팅은 아직 구현하지 않았습니다.

## 웹캠 없이 실제 손동작 로그 보기

휴대폰을 고정하고 양손과 상체가 보이게 가로 영상(일반 MP4, 가능하면 30FPS)을 촬영해 PC로 옮기세요. 0.5초 중립 → 불 → 손 내리기 → 흙 → 손 내리기 → 물 → 손 내리기 → 공기 순서로 15–20초 촬영하면 됩니다. 손은 카메라 안에 계속 보이게 움직이고 원형 이동은 0.8–1.3초에 한 바퀴를 그립니다. 슬로모션 영상은 쓰지 않습니다.

`elemental-bender` 폴더에서 PowerShell을 열고 영상의 실제 경로를 넣으세요.

```powershell
powershell -ExecutionPolicy Bypass -File .\run-video.ps1 -VideoPath "C:\Users\whend\Videos\hands.mp4"
```

영상 미리보기와 실제 랜드마크 기반 로그가 나옵니다. 이 실행은 `--no-udp`이므로 Unity 없이 인식만 확인합니다. 손 검출 수/손모양이 바뀔 때, 1초마다, 스킬이 인식될 때 출력합니다. 파일 끝에 도달하면 정상 종료합니다. 영상의 시간은 프레임 번호/파일 FPS로 계산하므로 일정 프레임률 영상으로 준비하세요.

```text
   0.50s | hands=2 | pose=FIST/FIST    | event=NONE  | infer=12.3ms
   1.10s | hands=2 | pose=FIST/FIST    | event=FIRE  | infer=11.8ms
```

위 수치는 로그 형식을 설명하는 예시이며 실측 결과가 아닙니다. `hands=0`이면 손 검출 실패, `pose=OTHER`이면 규칙의 손모양 조건 불충족, `event=NONE`이면 아직 발동 조건 불충족입니다.

녹화 영상을 Unity 입력으로 보내려면 별도로 다음처럼 실행하세요.

```powershell
.\.venv\Scripts\python.exe -u python/app.py --video "C:\Users\whend\Videos\hands.mp4"
```

아직 영상도 없다면 아래로 **합성 입력의 로그 형식**만 바로 확인할 수 있습니다. 이는 실제 손 검출/정확도 시험이 아닙니다.

```powershell
.\.venv\Scripts\python.exe python/demo_logs.py
```

## 구조

```text
elemental-bender/
  README.md                         실행 및 문제 해결
  PLAN_2026-10-16.md                 단계적 MVP·발표·실험 계획
  PROTOCOL.md                       UDP 메시지 정의
  setup-python.ps1 / run-camera.ps1 / run-mock.ps1
  python/
    app.py                          웹캠·MediaPipe·데이터 기록
    gestures.py                     정적 손모양·동적 제스처
    transport.py / mock_sender.py    UDP·웹캠 없는 통신 검사
    evaluate.py                     기록 재생·혼동행렬·P/R/F1
    test_core.py                    합성 궤적·재무장·UDP 검사
    verify_environment.py           실제 모델 로딩 검사
    requirements.txt
    models/hand_landmarker.task      첫 실행 다운로드
  unity-scripts/                    재사용 가능한 C# 원본
  UnityProject/                     Unity Hub에서 열 프로젝트
    Assets/ElementalBender/         같은 C# 파일의 프로젝트 사본
    Assets/Scenes/ElementalDemo.unity
    Packages/ / ProjectSettings/
```

C#를 수정할 때는 실제 실행 경로 `UnityProject/Assets/ElementalBender`를 수정하세요. `unity-scripts`는 다른 프로젝트로 옮길 때 쓰는 배포 원본입니다. 다른 Unity 6 URP 프로젝트에는 해당 폴더를 Assets 안으로 복사하고 위 메뉴로 씬을 생성하면 됩니다. uGUI 및 TMP가 필요하며 현재 생성된 템플릿에 포함되어 있습니다.

## 데이터 기록과 평가

한 JSONL 파일은 **한 동작 시험**입니다. 시작 중립 → 한 번 수행 → 다시 중립을 4초 안에 기록합니다. 시간창 규칙이 처음부터 동작을 보도록 프로그램 실행 직후 움직이지 말고 약 0.5초 기다립니다. 양손 데이터가 있어도 실제 라벨은 시험자가 의도한 동작입니다.

```powershell
cd python
..\.venv\Scripts\python.exe app.py --record ../data/test/P05_FIRE_normal_01.jsonl --label FIRE --subject P05 --condition normal --duration 4
..\.venv\Scripts\python.exe app.py --record ../data/test/P05_NONE_normal_01.jsonl --label NONE --subject P05 --condition normal --duration 20
..\.venv\Scripts\python.exe evaluate.py ../data/test --out ../evaluation.json
```

동일한 파일명이 있으면 덮어쓰지 않고 중단합니다. 데이터는 원본 영상을 저장하지 않고 프레임별 손 랜드마크·시간·손모양·의도 라벨·피험자 ID·환경·실시간 이벤트·추론 시간을 저장합니다. 손이 누락된 프레임도 남겨 실패를 평가에 포함합니다. 수집 중에도 패킷이 나가므로 전투 없이 수집하려면 Unity Play를 중지하세요.

평가기는 각 시험에 새 인식기를 만들고 기록을 재생합니다. 첫 발동을 시험의 예측 라벨로 삼으며 미발동은 NONE입니다. 추가 발동은 `extra_casts`, 중립 오발동은 `neutral_false_casts_per_min`으로 별도 집계합니다. 혼동행렬의 행은 정답, 열은 예측입니다. 전체 정확도와 클래스별 precision/recall/F1를 출력합니다. 실시간 이벤트 문자열을 정답으로 사용하지 않습니다. 상세 설계·분할·목표는 계획 문서를 확인하세요.

## 확인과 문제 해결

```powershell
.\.venv\Scripts\python.exe -m unittest discover -s python -p test_core.py -v
.\.venv\Scripts\python.exe python/verify_environment.py
```

- 영상이 안 나오면 Windows 카메라 권한, 다른 앱의 카메라 점유, `--camera 1`을 확인하세요.
- Unity의 UDP bind 오류는 다른 실행 인스턴스가 5055 포트를 사용하는 경우입니다. 다른 Play를 끄거나 Receiver 포트와 Python의 `--port`를 함께 변경하세요.
- 연결됨인데 발동하지 않으면 카메라의 OPEN/FIST 표시, 중립 재무장, 스킬 쿨다운을 확인하세요. FPS가 낮거나 손이 잠깐 끊기면 긴 제스처 궤적이 지워질 수 있습니다.
- 손 교차/가림/어두운 조명/화면 밖 이동을 줄이고 두 손 사이 간격을 유지하세요. 화면에 랜드마크가 뜬다는 사실만으로 제스처 정확도가 보장되지는 않습니다.
- UDP는 데스크톱 같은 PC만 대상으로 합니다. WebGL 브라우저에서는 이 UDP 수신기를 사용할 수 없습니다.
- HUD의 transit는 같은 PC 시계 기준 **송신 이후 Unity 처리까지**의 시간이며 카메라 입력→인식→화면 전체 지연은 아닙니다.

## 근거 자료

- [Google 공식 Hand Landmarker Python 문서](https://developers.google.com/edge/mediapipe/solutions/vision/hand_landmarker/python): Tasks API·VIDEO 모드·num_hands·타임스탬프 사용 근거.
- [MediaPipe Hands 논문](https://arxiv.org/abs/2006.10214): 손 검출·랜드마크 추정 파이프라인의 조사 자료.
- [Unity 공식 스크립팅 문서](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MonoBehaviour.Update.html): 프레임별 메인 스레드 처리의 구현 근거.

임계값과 목표 수치는 본 프로젝트의 설계값이며 문헌에서 입증된 성능 수치가 아닙니다. 실제 피험자 평가 전에는 ‘정확도 xx% 달성’으로 발표하지 마세요.
