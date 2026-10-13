"""Pure Python temporal rules; coordinates are unmirrored image x/y.
Thresholds are starting values, not measured accuracy guarantees.
"""
from collections import deque
from dataclasses import dataclass
import math

LABELS = ('NONE', 'FIRE', 'WATER', 'AIR', 'EARTH')

@dataclass
class Hand:
    x: float
    y: float
    size: float
    pose: str

def hand_features(points, aspect=1.0):
    def distance(a, b):
        return math.hypot((points[a][0]-points[b][0])*aspect,
                          points[a][1]-points[b][1])
    size = max(distance(0, 9), 0.001)
    # Four non-thumb fingers: orientation-independent joint angle + tip reach.
    extended = 0
    for mcp, pip, tip in ((5,6,8), (9,10,12), (13,14,16), (17,18,20)):
        a = ((points[mcp][0]-points[pip][0])*aspect, points[mcp][1]-points[pip][1])
        b = ((points[tip][0]-points[pip][0])*aspect, points[tip][1]-points[pip][1])
        cosine = sum(x*y for x,y in zip(a,b)) / max(math.hypot(*a)*math.hypot(*b), 1e-9)
        extended += cosine < -0.65 and distance(0,tip) > distance(0,pip)*1.12
    pose = 'OPEN' if extended >= 3 else 'FIST' if extended == 0 else 'OTHER'
    ids = (0,5,9,13,17)
    return Hand(sum(points[i][0] for i in ids)/5,
                sum(points[i][1] for i in ids)/5, size, pose)

class Recognizer:
    def __init__(self, window=1.4, cooldown=1.2):
        self.window, self.cooldown = window, cooldown
        self.history = deque()
        self.last_fire = -100.0
        self.pose = None
        self.neutral_since = None
        self.armed = True

    def update(self, t, hands):
        valid = len(hands)==2 and hands[0].pose==hands[1].pose and hands[0].pose in ('FIST','OPEN')
        if not valid:
            self.history.clear()
            self.pose = None
            if self.neutral_since is None:
                self.neutral_since = t
            if t-self.neutral_since >= 0.25:
                self.armed = True
            return 'NONE'
        self.neutral_since = None
        # Sorted screen sides. Crossing hands clears history rather than assigning identities.
        hands = sorted(hands, key=lambda h:h.x)
        pose = hands[0].pose
        if pose != self.pose or (self.history and t-self.history[-1][0] > 0.2):
            self.history.clear()
        self.pose = pose
        if self.history and any(math.hypot(h.x-p.x,h.y-p.y)>0.25
                                for h,p in zip(hands,self.history[-1][1])):
            self.history.clear()
        self.history.append((t,hands))
        while self.history and t-self.history[0][0] > self.window:
            self.history.popleft()
        if not self.armed or t-self.last_fire < self.cooldown or len(self.history)<8:
            return 'NONE'
        if t-self.history[0][0] < 0.35:
            return 'NONE'
        start = self.history[0][1]
        dx = [h.x-s.x for h,s in zip(hands,start)]
        dy = [h.y-s.y for h,s in zip(hands,start)]
        label = 'NONE'
        if pose=='FIST':
            if max(dy)<-0.13 and max(abs(v) for v in dx)<0.12:
                label = 'EARTH'
            elif dx[0]<-0.09 and dx[1]>0.09 and max(abs(v) for v in dy)<0.12:
                label = 'FIRE'
        else:
            if all(h.size/s.size>1.4 for h,s in zip(hands,start)) and max(abs(v) for v in dy)<0.13 and max(abs(v) for v in dx)<0.16:
                label = 'AIR'
            elif all(self._circle(i) for i in (0,1)):
                label = 'WATER'
        if label!='NONE':
            self.last_fire = t
            self.armed = False
            self.history.clear()
        return label

    def _circle(self, i):
        pts = [(hs[i].x,hs[i].y) for _,hs in self.history]
        xs,ys = zip(*pts)
        if max(xs)-min(xs)<0.12 or max(ys)-min(ys)<0.12:
            return False
        cx,cy = (max(xs)+min(xs))/2, (max(ys)+min(ys))/2
        angles = [math.atan2(y-cy,x-cx) for x,y in pts]
        turns = [math.atan2(math.sin(b-a),math.cos(b-a)) for a,b in zip(angles,angles[1:])]
        total = abs(sum(turns))
        consistent = total/max(sum(abs(v) for v in turns),1e-9)
        closure = math.dist(pts[0],pts[-1])
        radius = min(max(xs)-min(xs),max(ys)-min(ys))/2
        return total>5.0 and consistent>0.8 and closure<radius*0.8
