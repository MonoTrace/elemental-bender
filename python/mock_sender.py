"""Test UDP / combat without webcam or MediaPipe."""
import argparse
import time
from transport import Sender

if __name__=='__main__':
    p=argparse.ArgumentParser()
    p.add_argument('--port',type=int,default=5055)
    args=p.parse_args()
    sender=Sender(port=args.port)
    try:
        for element in ('FIRE','WATER','AIR','EARTH'):
            print(sender.send(element,2))
            for _ in range(15):
                time.sleep(0.1)
                sender.send()
    finally:
        sender.close()
