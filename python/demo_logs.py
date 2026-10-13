"""Synthetic feature trajectories only: no camera, video, or hand estimation."""
import math
from gestures import Hand, Recognizer

print('SIMULATED INPUT - this is NOT real hand recognition',flush=True)
for label in ('FIRE','EARTH','WATER','AIR'):
    recognizer=Recognizer()
    for i in range(37):
        t=i/30
        f=i/36
        if label=='FIRE':
            hands=[Hand(.4-.16*f,.6,.1,'FIST'),Hand(.6+.16*f,.6,.1,'FIST')]
        elif label=='EARTH':
            hands=[Hand(.3,.65-.25*f,.1,'FIST'),Hand(.7,.65-.25*f,.1,'FIST')]
        elif label=='WATER':
            angle=f*2*math.pi
            hands=[Hand(x+.1*math.cos(angle),.5+.1*math.sin(angle),.1,'OPEN') for x in (.3,.7)]
        else:
            hands=[Hand(.3,.5,.1*(1+.6*f),'OPEN'),Hand(.7,.5,.1*(1+.6*f),'OPEN')]
        event=recognizer.update(t,hands)
        if i==0 or event!='NONE':
            print(f'{label:5} trial | {t:.2f}s | hands=2 | pose={hands[0].pose}/{hands[1].pose} | event={event}',flush=True)
