# 참조 코드 원본 보관

과제의 ‘참조할 Code’ 제출을 위해 조사한 공개 소스의 일부를 원본 상태로 보관합니다. 다운로드 기준일은 2026-10-15이며, 고정된 Git 커밋과 파일 해시는 `SOURCES.json`에 있습니다.

| 디렉터리 | 원본 | 포함한 파일 | 이용 상태 |
|---|---|---|---|
| `mediapipe-samples` | google-ai-edge/mediapipe-samples | Hand Landmarker 공식 Python 노트북, LICENSE | Tasks API·입출력 분석의 근거 |
| `hand-gesture-recognition-mediapipe` | kinivi/hand-gesture-recognition-mediapipe | app.py, 정적/동적 학습 노트북, LICENSE | 정규화·CSV 수집·MLP/선택적 LSTM의 참고 후보, 프로젝트에 통합하지 않음 |

각 디렉터리의 Apache-2.0 라이선스와 소스 내 저작자 표기를 유지합니다. 이 보관본을 본 프로젝트의 자체 작성 코드로 주장하지 않습니다. 원본 실행 환경과 API가 다르므로 보관본의 실행 호환성을 보장하지 않습니다.

조사 내용과 변경 계획은 상위 `DATASETS_AND_CODE.md`를 확인하세요. 데이터셋 원본과 외부 학습 가중치는 이 폴더에 포함하지 않습니다.
