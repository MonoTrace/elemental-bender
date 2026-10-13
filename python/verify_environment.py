"""Load actual model, infer on a blank image. No camera access."""
from pathlib import Path
import urllib.request
import cv2
import mediapipe as mp
import numpy as np
from app import MODEL_URL

model=Path(__file__).resolve().parent/'models'/'hand_landmarker.task'
model.parent.mkdir(parents=True,exist_ok=True)
if not model.exists():
    temporary=model.with_suffix('.download')
    urllib.request.urlretrieve(MODEL_URL,temporary)
    temporary.replace(model)
options=mp.tasks.vision.HandLandmarkerOptions(
    base_options=mp.tasks.BaseOptions(model_asset_path=str(model)),
    running_mode=mp.tasks.vision.RunningMode.VIDEO,num_hands=2)
with mp.tasks.vision.HandLandmarker.create_from_options(options) as detector:
    result=detector.detect_for_video(mp.Image(image_format=mp.ImageFormat.SRGB,
        data=np.zeros((480,640,3),dtype=np.uint8)),0)
    assert not result.hand_landmarks
print(f'PASS: MediaPipe {mp.__version__}, OpenCV {cv2.__version__}, actual model inference (blank image)')
