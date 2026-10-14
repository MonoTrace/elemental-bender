import argparse
import json
from pathlib import Path
import time
import urllib.request
from gestures import Recognizer, hand_features
from transport import Sender
from preview_stream import PreviewStream

MODEL_URL = 'https://storage.googleapis.com/mediapipe-models/hand_landmarker/hand_landmarker/float16/1/hand_landmarker.task'

def main():
    p = argparse.ArgumentParser()
    p.add_argument('--camera',type=int,default=0)
    p.add_argument('--video',help='Read a phone-recorded video instead of a webcam')
    p.add_argument('--no-udp',action='store_true',help='Inspect recognition without sending game input')
    p.add_argument('--no-preview',action='store_true',help='Run without the video window')
    p.add_argument('--no-mirror-preview',action='store_true',help='Disable mirrored webcam preview (recognition coordinates stay unchanged)')
    p.add_argument('--preview-port',type=int,default=5056)
    p.add_argument('--no-unity-preview',action='store_true',help='Disable the local Unity image bridge')
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
    if args.video and not Path(args.video).is_file():
        p.error('Video file not found: '+args.video)
    cap = cv2.VideoCapture(args.video if args.video else args.camera)
    if not cap.isOpened():
        cap.release()
        raise RuntimeError('Input unavailable. Check video format or camera permission.')
    fps=cap.get(cv2.CAP_PROP_FPS)
    if args.video and not (1 <= fps <= 240):
        cap.release()
        raise RuntimeError('Video frame rate unavailable. Export the clip as constant-rate MP4.')
    sender, recognizer = (None if args.no_udp else Sender(args.host,args.port)), Recognizer()
    output = None
    stream = None
    start = time.monotonic()
    last_ms, display, display_until = -1,'NONE',0
    frame_index, last_status, last_log = 0,None,-100
    print('INPUT: '+(args.video or f'camera {args.camera}')+' | Q: quit',flush=True)
    try:
        if not args.no_unity_preview:
            try:
                stream=PreviewStream(args.preview_port)
                print(f'Unity preview: http://127.0.0.1:{args.preview_port}/frame.jpg',flush=True)
            except OSError as error:
                print(f'Unity preview unavailable: {error}. Recognition remains active.',flush=True)
        if args.record:
            path = Path(args.record)
            path.parent.mkdir(parents=True,exist_ok=True)
            output = path.open('x',encoding='utf-8')
        with mp.tasks.vision.HandLandmarker.create_from_options(options) as detector:
            while True:
                ok, frame = cap.read()
                if not ok:
                    if args.video:
                        print('Video finished.',flush=True)
                        break
                    raise RuntimeError('Camera frame read failed.')
                now = time.monotonic()
                t = frame_index/fps if args.video else now-start
                frame_index+=1
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
                if sender: sender.send(event,len(hands))
                status=(len(hands),tuple(h.pose for h in sorted(hands,key=lambda h:h.x)))
                if status!=last_status or t-last_log>=1 or event!='NONE':
                    poses='/'.join(status[1]) or '-'
                    print(f'{t:7.2f}s | hands={len(hands)} | pose={poses:12} | event={event:5} | infer={inference_ms:.1f}ms',flush=True)
                    last_status,last_log=status,t
                if event!='NONE':
                    display,display_until = event,t+0.8
                if output:
                    output.write(json.dumps(dict(t=t, label=args.label, subject=args.subject,
                        condition=args.condition, aspect=aspect, landmarks=landmarks,
                        poses=[h.pose for h in hands], event=event, inference_ms=inference_ms))+ '\n')
                for hand in landmarks:
                    for x,y,_ in hand:
                        cv2.circle(frame,(int(x*frame.shape[1]),int(y*frame.shape[0])),3,(0,255,180),-1)
                # Mirror the webcam display and landmark overlay together; keep original
                # coordinates for recognition/recordings and draw readable text afterwards.
                if not args.video and not args.no_mirror_preview:
                    frame=cv2.flip(frame,1)
                cv2.putText(frame,f'{display if t<display_until else "NONE"} | '+ '/'.join(h.pose for h in hands),
                            (20,35),cv2.FONT_HERSHEY_SIMPLEX,0.8,(0,255,255),2)
                cv2.putText(frame,'Release hands between casts | Q: quit', (20,65),cv2.FONT_HERSHEY_SIMPLEX,0.55,(255,255,255),1)
                if stream: stream.publish(frame)
                delay=max(1,int((start+frame_index/fps-time.monotonic())*1000)) if args.video else 1
                key=-1
                if not args.no_preview:
                    cv2.imshow('ELEMENTAL BENDER - input',frame)
                    key=cv2.waitKey(delay)&0xff
                elif args.video:
                    time.sleep(max(0,(start+frame_index/fps-time.monotonic())))
                if key==ord('q') or (args.duration>0 and t>=args.duration):
                    break
    finally:
        if output: output.close()
        cap.release()
        cv2.destroyAllWindows()
        if sender: sender.close()
        if stream: stream.close()

if __name__=='__main__':
    main()
