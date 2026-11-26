#!/usr/bin/env python3
"""Sprig (the mascot) in the notebook style, 4 expressions -> Assets/Resources/Characters/Sprig_{Neutral,Happy,Sad,Cheer}.png (256 px, transparent).
Run from this folder:  python3 generate_sprig.py && NODE_PATH=$(npm root -g) node render.js jobs_sprig.json"""
import math, json
from ink import D
R = '../../Assets/Resources/Characters/'
JOBS = []
def sprig(seed, p):
    """No arms, no legs: a plump seed with a sprout on top, sitting in a little mound of soil. Expression = face + leaves + a hop (dy) with a squash."""
    d = D(seed); dy = p.get('dy', 0); sq = p.get('squash', 1.0); kind = p['face']; sad = kind == 'sad'
    # ground shadow (smaller when hopping) and the mound of soil
    d.line([(150, 408), (240, 414), (330, 408)], w=2, a=1.5, op=.5)
    d.blob(240, 392, 96, 22, 14, w=3, fill='#a06a38', a=1.4)
    for fx, fy in ((200, 388), (240, 396), (282, 386)): d.dot(fx, fy, 3.2)
    # body: a plump seed (squashed when it lands, stretched when it hops)
    bw = 92 * (1 / sq) ** .5; bh = 104 * sq
    cy = 290 + dy - (sq - 1) * 60
    d.blob(240, cy, bw, bh, 22, w=3, fill='#ebca8a', a=1.8)
    d.hatch(250, cy - 40, 320, cy + 70, 5)
    d.line([(196, cy + 40), (214, cy + 58)], w=2.2, a=.6, op=.6); d.line([(262, cy + 62), (284, cy + 46)], w=2.2, a=.6, op=.6)
    # face
    ey = cy - 24
    for ex in (206, 274):
        if kind in ('cheer', 'happy_blink'): d.line([(ex - 14, ey + 4), (ex, ey - 6), (ex + 14, ey + 4)], w=3, a=.6)
        else: d.blob(ex, ey, 14, 17, 10, w=2.8, fill='#fffdf5', a=.7); d.dot(ex + p.get('look', 3), ey + 2 + (4 if sad else 0), 6)
    if sad: d.line([(186, ey - 16), (222, ey - 30)], w=3, a=.6); d.line([(258, ey - 30), (294, ey - 16)], w=3, a=.6)
    else: d.line([(188, ey - 26), (222, ey - 33)], w=3, a=.6); d.line([(258, ey - 33), (292, ey - 26)], w=3, a=.6)
    if kind in ('cheer', 'happy'): d.line([(212, cy + 14), (240, cy + 48), (268, cy + 14)], w=2.8, closed=True, fill='#8a3a3a', a=.8)
    elif sad:
        d.line([(222, cy + 40), (240, cy + 26), (258, cy + 40)], w=3, a=.7)
        d.line([(288, ey + 14), (282, ey + 36), (294, ey + 36)], w=2.2, closed=True, fill='#8fd6f5', a=.4)
    else: d.line([(224, cy + 26), (240, cy + 34), (256, cy + 26)], w=3, a=.6)
    for cx in (176, 304): d.blob(cx, cy + 6, 13, 8, 8, w=1.2, fill='#ff8fa3', a=.8, op=.5)
    # sprout: stem and two leaves (droop when sad, perk up when cheering)
    sy = p.get('sprout', 0); top = cy - bh + 6; droop = 1 if sad else 0
    d.line([(240, top + 6), (242, top - 20), (240 + sy * .4, top - 44)], w=4, a=.7)
    bx = 240 + sy * .4; by = top - 38
    d.line([(bx, by), (bx + 40 + sy, by - 42 + droop * 22), (bx + 86 + sy, by - 30 + droop * 38), (bx + 76 + sy, by + 6 + droop * 30), (bx + 30 + sy * .5, by + 6)], w=2.8, closed=True, fill='#5bb04a', a=1)
    d.line([(bx - 2, by + 6), (bx - 40 - sy * .6, by - 30 + droop * 22), (bx - 84 - sy * .6, by - 18 + droop * 38), (bx - 66 - sy * .6, by + 18 + droop * 30), (bx - 22, by + 20)], w=2.8, closed=True, fill='#3f8f3a', a=1)
    d.line([(bx + 8, by - 4), (bx + 56 + sy, by - 14 + droop * 24)], w=1.6, a=.5, op=.6); d.line([(bx - 8, by + 4), (bx - 54 - sy * .6, by - 4 + droop * 24)], w=1.6, a=.5, op=.6)
    return d.svg(256, 256).replace('viewBox="0 0 256 256"', 'viewBox="55 20 370 400"')
POSES = {
    'Neutral': dict(face='neutral'),
    'Happy': dict(face='happy', sprout=8, squash=1.03),
    'Sad': dict(face='sad', dy=6, squash=.94),
    'Cheer': dict(face='cheer', dy=-26, squash=1.1, sprout=16),
}
import random
def vary(p, k):
    r = random.Random(500 + k); q = dict(p)
    q['sprout'] = p.get('sprout', 0) + r.uniform(-8, 8); q['dy'] = p.get('dy', 0) + r.uniform(-5, 5); q['squash'] = p.get('squash', 1.0) + r.uniform(-.04, .04)
    return q
k = 0
for i, (n, p) in enumerate(POSES.items()):
    variants = [(n, p, 40 + i)]
    if n in ('Happy', 'Cheer'):   # three drawings each, alternated by MainMenuView (boiling-line flipbook)
        variants += [('%s_%d' % (n, j), vary(p, k + j), 60 + i * 5 + j) for j in (1, 2)]; k += 3
    for name, pose, seed in variants:
        open('svg/Sprig_%s.svg' % name, 'w').write(sprig(seed, pose)); JOBS.append(('svg/Sprig_%s.svg' % name, R + 'Sprig_%s.png' % name, 256, 256, True))
json.dump(JOBS, open('jobs_sprig.json', 'w')); print(len(JOBS))
