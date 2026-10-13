"""Replay each JSONL trial with fresh recognizer; no random frame split."""
import argparse
import json
from pathlib import Path
from gestures import Recognizer, hand_features, LABELS

def evaluate(paths):
    matrix={label:{p:0 for p in LABELS} for label in LABELS}
    extra, neutral_events, neutral_seconds, total = 0,0,0.0,0
    rows=[]
    for path in paths:
        frames=[json.loads(line) for line in path.read_text(encoding='utf-8').splitlines() if line.strip()]
        if not frames: continue
        truth=frames[0]['label']
        if any(f['label']!=truth for f in frames):
            raise ValueError(f'{path}: each file must contain one trial label')
        r=Recognizer()
        events=[]
        for f in frames:
            event=r.update(f['t'],[hand_features(h,f.get('aspect',1)) for h in f['landmarks']])
            if event!='NONE': events.append((f['t'],event))
        predicted=events[0][1] if events else 'NONE'
        matrix[truth][predicted]+=1
        extra+=max(0,len(events)-1)
        if truth=='NONE':
            neutral_events+=len(events)
            neutral_seconds+=frames[-1]['t']-frames[0]['t']
        total+=1
        rows.append(dict(file=str(path),truth=truth,predicted=predicted,events=events))
    metrics={}
    for label in LABELS:
        tp=matrix[label][label]
        actual=sum(matrix[label].values())
        predicted=sum(matrix[g][label] for g in LABELS)
        precision=tp/predicted if predicted else 0
        recall=tp/actual if actual else 0
        metrics[label]=dict(support=actual,precision=precision,recall=recall,
                            f1=2*precision*recall/(precision+recall) if precision+recall else 0)
    return dict(trials=total, accuracy=sum(matrix[l][l] for l in LABELS)/total if total else None,
                confusion_matrix=matrix,metrics=metrics,extra_casts=extra,
                neutral_false_casts_per_min=neutral_events*60/neutral_seconds if neutral_seconds else None,
                trials_detail=rows)

if __name__=='__main__':
    p=argparse.ArgumentParser()
    p.add_argument('directory')
    p.add_argument('--out',default='evaluation.json')
    args=p.parse_args()
    paths=sorted(Path(args.directory).rglob('*.jsonl'))
    if not paths: p.error('No JSONL trials found')
    report=evaluate(paths)
    Path(args.out).write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({k:v for k,v in report.items() if k!='trials_detail'},indent=2))
