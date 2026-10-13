import argparse
import json
from pathlib import Path
import time
import urllib.request
from gestures import Recognizer, hand_features
from transport import Sender

MODEL_URL = 'https://storage.googleapis.com/mediapipe-models/hand_landmarker/hand_landmarker/float16/1/hand_landmarker.task'

def main():
    p = argparse.ArgumentParser()
    p.add_argument('--camera',type=int,default=0)
    p.add_argument('--host',default='127.0.0.1')
    p.add_argument('--port',type=int,default=5055)
    p.add_argument('--record',help='Save one labelled trial as JSONL (landmarks, no video)')
    p.add_argument('--label',choices=['NONE','FIRE','WATER','AIR','EARTH'],default='NONE')
    p.add_argument('--subject',default='P01')
    p.add_argument('--condition',default='normal')
    p.add_argument('--duration',type=float,default=0)
    args = p.parse_args()
    import cv2
    import mediapipe as mp
    model = Path(__file__).resolve().parent/'models'/'hand_landmarker.task'
    if not model.exists():
        model.parent.mkdir(parents=True,exist_ok=True)
        print('Downloading official Hand Landmarker model...')
        temporary = model.with_suffix('.download')
        urllib.request.urlretrieve(MODEL_URL,temporary)
        temporary.replace(model)
    options = mp.tasks.vision.HandLandmarkerOptions(
        base_options=mp.tasks.BaseOptions(model_asset_path=str(model)),
        running_mode=mp.tasks.vision.RunningMode.VIDEO,num_hands=2,
        min_hand_detection_confidence=0.6,min_hand_presence_confidence=0.6,
        min_tracking_confidence=0.6)
    cap = cv2.VideoCapture(args.camera)
    if not cap.isOpened():
        raise RuntimeError('Camera unavailable. Check permission or try --camera 1.')
    sender, recognizer = Sender(args.host,args.port), Recognizer()
    output = None
    start = time.monotonic()
    last_ms, display, display_until = -1,'NONE',0
    try:
        if args.record:
            path = Path(args.record)
            path.parent.mkdir(parents=True,exist_ok=True)
            output = path.open('x',encoding='utf-8')
        with mp.tasks.vision.HandLandmarker.create_from_options(options) as detector:
            while True:
                ok, frame = cap.read()
                if not ok:
                    raise RuntimeError('Camera frame read failed.')
                now = time.monotonic()
                t = now-start
                timestamp = max(last_ms+1,int(t*1000))
                last_ms = timestamp
                rgb = cv2.cvtColor(frame,cv2.COLOR_BGR2RGB)
                inference_start = time.perf_counter()
                result = detector.detect_for_video(mp.Image(image_format=mp.ImageFormat.SRGB,data=rgb),timestamp)
                inference_ms = (time.perf_counter()-inference_start)*1000
                landmarks = [[[pt.x,pt.y,pt.z] for pt in hand] for hand in result.hand_landmarks]
                aspect = frame.shape[1]/frame.shape[0]
                hands = [hand_features(hand,aspect) for hand in landmarks]
                event = recognizer.update(t,hands)
                sender.send(event,len(hands))
                if event!='NONE':
                    display,display_until = event,t+0.8
                    print(f'{t:.2f}s {event}')
                if output:
                    output.write(json.dumps(dict(t=t, label=args.label, subject=args.subject,
                        condition=args.condition, aspect=aspect, landmarks=landmarks,
                        poses=[h.pose for h in hands], event=event, inference_ms=inference_ms))+ '\n')
                for hand in landmarks:
                    for x,y,_ in hand:
                        cv2.circle(frame,(int(x*frame.shape[1]),int(y*frame.shape[0])),3,(0,255,180),-1)
                cv2.putText(frame,f'{display if t<display_until else "NONE"} | '+ '/'.join(h.pose for h in hands),
                            (20,35),cv2.FONT_HERSHEY_SIMPLEX,0.8,(0,255,255),2)
                cv2.putText(frame,'Release hands between casts | Q: quit', (20,65),cv2.FONT_HERSHEY_SIMPLEX,0.55,(255,255,255),1)
                cv2.imshow('ELEMENTAL BENDER - camera (unmirrored)',frame)
                if cv2.waitKey(1)&0xff==ord('q') or (args.duration>0 and t>=args.duration):
                    break
    finally:
        if output: output.close()
        cap.release()
        cv2.destroyAllWindows()
        sender.close()

if __name__=='__main__':
    main()
