# ELEMENTAL BENDER 데이터셋·참조 코드 조사

발표: 2026-10-16 / 2조 송기훈·최원제. 조사 기준일: 2026-10-15.

## 1. 현재 사용한 것과 앞으로 사용할 것

- **사용 중:** MediaPipe Hand Landmarker 사전학습 모델, 직접 작성한 손모양·양손 궤적 규칙, JSON/UDP 기반 Unity 연결.
- **조사 완료, 데이터 미다운로드:** HaGRIDv2, Jester, EgoGesture의 공식 설명 및 활용 적합성 비교.
- **수집 예정:** 직접 정의하는 4원소 공격·방어 8동작 및 NONE 데이터. 정식 사용자 정확도·혼동행렬은 아직 측정하지 않았다.
- 합성 특징의 자동 검사 통과나 실시간 발동 로그를 실제 사용자 정확도로 보고하지 않는다.

## 2. 공개 데이터셋 조사 및 선정

| 데이터셋 | 구성 | 활용 계획 | 한계·선정 판단 |
|---|---|---|---|
| HaGRIDv2 | 공식 저장소 기준 1,086,158 RGB 이미지, 33개 제스처와 별도 no_gesture 클래스. 손 박스·라벨·user_id·자동 추정 랜드마크 제공 | peace(V), fist, palm과 중립 표본을 골라 정적 손모양의 오인식 분석. 향후 정적 분류기 비교 자료 | 원소 공격·방어의 시간 정보가 직접 포함된 데이터가 아니다. 전체 다운로드보다 관련 클래스 일부를 우선 검토 |
| Jester | 공식 설명 기준 148,092개 영상, 27개 손동작 클래스, 웹캠/노트북 카메라 촬영 | 동적 인식의 시간축 처리와 정규화·평가 방식 비교 | 사용자 정의 양손 공격·방어와 라벨이 달라 그대로 게임 명령으로 사용하지 않음 |
| EgoGesture | 논문 기준 83개 정적·동적 제스처, RGB·깊이 영상, 50명, 6개 실내외 환경 | 다양한 촬영 조건과 연속 동작 분할의 관련 연구 | 착용형 1인칭 촬영과 현재 정면 웹캠의 시점이 다르므로 주 데이터로 채택하지 않음 |

**활용 결정:** HaGRIDv2는 정적 손모양 비교용, Jester는 동적 인식 관련 연구로 삼고, 최종 게임 성능은 자체 수집 데이터에서 평가한다. 공개 데이터를 MediaPipe 사전학습의 원천 데이터로 단정하지 않는다. 다운로드·재배포 전 각 제공처의 이용 조건을 확인하며 데이터 원본은 GitHub에 올리지 않는다.

공식 출처:

- HaGRID: https://github.com/hukenovs/hagrid
- Jester: https://www.qualcomm.com/developer/software/jester-dataset
- EgoGesture: https://ieeexplore.ieee.org/document/8299578/

## 3. 자체 데이터 수집·전처리 계획

최종 동작을 먼저 확정하고 FIRE/WATER/AIR/EARTH 각각에 ATTACK/DEFEND를 붙인 8개 라벨과 NONE을 사용한다. 현재 수집 CLI와 평가기는 원소 단위 라벨이므로 먼저 확장해야 한다.

- 계획: 6명 × 8동작 × 3조건 × 10회 = **1,440개 양성 시험**. 시험당 4초면 동작 영상 약 96분.
- NONE: 6명 × 3조건 × 3회 × 20초 = **18분**. 일상 손짓, 한 손만 보임, 손 가림, 유사한 비대상 동작을 포함한다.
- 조건: 기준 거리·밝은 조명 / 먼 거리·밝은 조명 / 기준 거리·어두운 조명. 카메라, 실제 거리, FPS, 누락률을 기록한다.
- 분할: P01–P03 개발·학습, P04 검증, P05–P06 최종 테스트. 같은 사람의 프레임을 임의로 섞어 나누지 않는다.
- 저장: 참가자 익명 ID, 시험 라벨, 시간, 양손 랜드마크, 손 검출 마스크, 환경. 기본은 원본 영상 저장 없이 랜드마크를 기록한다. 전체 지연 측정용 영상은 별도 동의를 받는다.
- 전처리 계획: 손목 기준 상대 좌표·손 크기 정규화, 원본 두 손의 상대 위치 유지, 시계열 길이 통일과 누락 마스크. 양손 관계가 사라지지 않도록 각 손의 정규화 특징과 두 손 간 거리·상대 이동을 함께 보관한다.
- 좌우 반전 증강은 라벨 의미가 유지되는 동작에만 적용하고, 검증·테스트는 증강 이전 참가자 기준으로 분리한다.

## 4. 참조 코드와 AI 모델 분석

### A. Google 공식 Hand Landmarker 예제 — 실제 API 구현 근거

원본: https://github.com/google-ai-edge/mediapipe-samples/blob/main/examples/hand_landmarker/python/hand_landmarker.ipynb

보관본: `reference_code/mediapipe-samples/hand_landmarker.ipynb`와 원본 `LICENSE`.

- 입력: RGB 이미지. 손 검출과 손 랜드마크 추정으로 각 손의 21개 관절 좌표를 출력한다.
- 모델 구성: 손바닥 검출 모델과 손 랜드마크 모델. 영상 모드에서는 추적을 활용한다. 과거 MediaPipe Hands 논문은 파이프라인 이해의 근거이며 현재 배포 모델의 세부 구조와 완전히 같다고 단정하지 않는다.
- 공식 노트북은 기본 이미지 예제다. 본 프로젝트는 `RunningMode.VIDEO`, 증가하는 timestamp와 `num_hands=2`로 실시간 입력에 적용했다.
- 현재 구현 함수: `cv2.VideoCapture`, `detect_for_video`, `hand_features`, `Recognizer.update`.
- 출력 좌표의 x/y를 손모양·궤적 규칙에 사용한다. z를 두 손의 절대 거리로 사용하지 않는다.
- 사전학습 모델은 재학습하지 않고 사용한다. 현재 원소 분류기는 신경망이 아닌 규칙 기반이다.

문서: https://developers.google.com/edge/mediapipe/solutions/vision/hand_landmarker/python

논문: https://arxiv.org/abs/2006.10214

### B. kinivi 제스처 인식 예제 — 학습 기반 확장의 참고 코드

원본: https://github.com/kinivi/hand-gesture-recognition-mediapipe

보관본: `reference_code/hand-gesture-recognition-mediapipe/`의 `app.py`, 학습 노트북 2개, `LICENSE`. 현재 실행 코드에 이 분류기를 통합하거나 학습한 것은 아니다.

- `pre_process_landmark`: 기준 좌표를 빼고 정규화한 손 관절 특징 생성.
- `logging_csv`: 정적 관절 및 움직임 이력을 라벨과 함께 저장.
- `KeyPointClassifier`: 정적 손모양 분류. 노트북은 21×2=42차원 입력 → Dense(20, ReLU) → Dense(10, ReLU) → 클래스 Softmax 구조이며 Dropout을 포함한다.
- `point_history`: 기본 16개 위치 이력. 단순히 현재 한 프레임만 보지 않고 이동 정보를 사용한다.
- 동적 노트북: 기본 MLP 경로는 Dense(24) → Dense(10) → Softmax. 선택적인 LSTM(16) 경로도 있으나 기본 `use_lstm=False`이다.
- 적용 차이: 예제의 기본 설정은 한 손, 기존 `mp.solutions.hands` API다. 프로젝트는 Tasks API·양손·누락 처리·공격/방어 라벨로 확장해야 한다. 그대로 실행되는 호환 코드를 의미하지 않는다.

원본 버전, 경로, SHA-256은 `reference_code/SOURCES.json`에 기록했다. 수정하지 않은 보관본이며 각 원본 라이선스를 유지한다.

## 5. 참조 구현 대비 추가할 컴퓨터비전 기능

| 참조 구현의 기능 | 이번 프로젝트에서 추가할 내용 |
|---|---|
| 손의 21개 관절 추정 | 두 손의 손모양·상대 위치·이동 궤적을 함께 사용 |
| 한 손의 정적 자세·위치 이력 분류 예제 | 4원소 × 공격/방어 8동작과 NONE 구분 |
| 프레임/이력별 인식 결과 | 지속 시간·중립 복귀·쿨다운으로 의도한 한 번의 명령 판정 |
| 일반적인 손동작 데모 | 조명·거리·가림 조건과 참가자 분리를 통한 동작별 검증 |

Unity 연결만을 추가 구현으로 제시하지 않는다. 컴퓨터비전의 핵심 추가 내용은 **양손 시계열 특징 결합과 게임 고유 공격·방어 동작의 구분 및 평가**다. 다만 참조 예제도 이미 동적 제스처를 포함하므로 ‘동적 인식 자체를 처음 추가한다’고 주장하지 않는다.

통합 기능으로 JSON/UDP 스킬 입력, 원소 상성 방어, 고정 거리의 3D 1대1 전투, 공격/방어 애니메이션을 연결할 계획이다. 현재는 모델/Idle, 입력 전달, 웹캠 미리보기 ON/OFF, 임시 스킬 효과가 구현되어 있고 상성표·최종 8동작·전투 애니메이션은 미구현이다.

## 6. 평가 계획

- 실제 사용자 단위 혼동행렬, 각 클래스 precision/recall/F1, 미발동률과 중복 발동 수.
- NONE 구간 오발동/분. 동작 없이 카메라가 켜져 있는 구간도 포함.
- 추론 시간·처리 FPS와 전체 입력→화면 지연을 분리한다. 완료 시점→게임 효과의 p50/p95를 별도 측정한다.
- 목표 수치는 측정 결과가 아니다: 클래스 recall 85% 이상, precision 90% 이상, NONE 오발동 1회/분 이하, 동작 완료 후 반응 p95 300ms 이하를 초기 목표로 둔다.
- 규칙 기준선과 학습 기반 MLP/시계열 모델은 같은 테스트 참가자로 비교한다. 모델 학습을 하지 않으면 규칙 기반 결과만 정직하게 보고한다.

## 7. GitHub 제출 위치

- 프로젝트: https://github.com/MonoTrace/elemental-bender
- 조사·분석: https://github.com/MonoTrace/elemental-bender/blob/main/research/DATASETS_AND_CODE.md
- 참조 코드 보관본: https://github.com/MonoTrace/elemental-bender/tree/main/research/reference_code
- 실제 구현 코드: `python/`과 `UnityProject/Assets/ElementalBender/`.

2026년 10월 15일 소유자 승인으로 저장소를 공개로 전환했다. 제출 안내문이나 강의 예제의 개인정보·학교 로고를 프로젝트 자료에 복사하지 않았다.
