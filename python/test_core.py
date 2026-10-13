import json
import math
import socket
import unittest
import tempfile
from pathlib import Path
from evaluate import evaluate
from gestures import Hand, Recognizer, hand_features
from transport import Sender

class GestureTests(unittest.TestCase):
    def trajectory(self,label):
        r=Recognizer()
        events=[]
        for k in range(37):
            t=k/30
            f=k/36
            if label=='FIRE': hands=[Hand(.4-.16*f,.6,.1,'FIST'),Hand(.6+.16*f,.6,.1,'FIST')]
            elif label=='EARTH': hands=[Hand(.3,.65-.25*f,.1,'FIST'),Hand(.7,.65-.25*f,.1,'FIST')]
            elif label=='AIR': hands=[Hand(.3,.5,.1*(1+.6*f),'OPEN'),Hand(.7,.5,.1*(1+.6*f),'OPEN')]
            elif label=='WATER':
                a=f*2*math.pi
                hands=[Hand(cx+.1*math.cos(a),.5+.1*math.sin(a),.1,'OPEN') for cx in (.3,.7)]
            else: hands=[Hand(.3,.5,.1,'OPEN'),Hand(.7,.5,.1,'OPEN')]
            event=r.update(t,hands)
            if event!='NONE': events.append(event)
        return events
    def test_four_trajectories(self):
        for label in ('FIRE','WATER','AIR','EARTH'):
            with self.subTest(label=label): self.assertEqual(self.trajectory(label),[label])
    def test_static_does_not_cast(self): self.assertEqual(self.trajectory('NONE'),[])
    def test_one_hand_does_not_cast(self):
        r=Recognizer()
        for k in range(40): self.assertEqual(r.update(k/30,[Hand(.5,.5-k/200,.1,'FIST')]),'NONE')
    def test_rearm_requires_neutral(self):
        r=Recognizer()
        def sweep(offset):
            return [r.update(offset+k/30,[Hand(.4-k*.005,.5,.1,'FIST'),Hand(.6+k*.005,.5,.1,'FIST')]) for k in range(37)]
        self.assertEqual(sweep(0).count('FIRE'),1)
        self.assertEqual(sweep(2).count('FIRE'),0)
        r.update(3.3,[]);r.update(3.6,[])
        self.assertEqual(sweep(4).count('FIRE'),1)
    def test_hand_loss_clears_motion(self):
        r=Recognizer()
        for k in range(8): r.update(k/30,[Hand(.4,.5,.1,'FIST'),Hand(.6,.5,.1,'FIST')])
        r.update(.3,[])
        self.assertEqual(r.update(.4,[Hand(.2,.5,.1,'FIST'),Hand(.8,.5,.1,'FIST')]),'NONE')
    def test_udp_packet_repeated_cast_same_seq(self):
        server=socket.socket(socket.AF_INET,socket.SOCK_DGRAM)
        server.bind(('127.0.0.1',0));server.settimeout(1)
        sender=Sender(port=server.getsockname()[1])
        try:
            sent=sender.send('FIRE',2)
            received=[json.loads(server.recv(4096)) for _ in range(3)]
            self.assertTrue(all(p==sent for p in received))
            self.assertEqual(sent['version'],1)
        finally: sender.close();server.close()
    def test_evaluation_counts_missed_cast(self):
        with tempfile.TemporaryDirectory() as directory:
            paths=[]
            for label in ('FIRE','NONE'):
                path=Path(directory)/(label+'.jsonl')
                path.write_text('\n'.join(json.dumps(dict(t=t,label=label,landmarks=[]))
                                           for t in (0,1,2)),encoding='utf-8')
                paths.append(path)
            report=evaluate(paths)
            self.assertEqual(report['confusion_matrix']['FIRE']['NONE'],1)
            self.assertEqual(report['accuracy'],0.5)
            self.assertEqual(report['neutral_false_casts_per_min'],0)

if __name__=='__main__': unittest.main()
