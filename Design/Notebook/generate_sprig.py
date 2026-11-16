#!/usr/bin/env python3
"""Sprig (the mascot) in the notebook style, 4 expressions -> Assets/Resources/Characters/Sprig_{Neutral,Happy,Sad,Cheer}.png (256 px, transparent).
Run from this folder:  python3 generate_sprig.py && NODE_PATH=$(npm root -g) node render.js jobs_sprig.json"""
import math, json
from ink import D
R = '../../Assets/Resources/Characters/'
JOBS = []
def sprig(seed, p):
    d = D(seed); dy = p.get('dy', 0)
    d.line([(150, 404), (240, 410), (330, 404)], w=2, a=1.5, op=.5)
    for sx in (-1, 1):
        lx = 240 + sx * 30; fx = lx + p.get('leg', 8) * sx
        d.line([(lx, 330 + dy), (fx, 372)], w=3, a=.8)
        d.line([(fx - 18, 372), (fx + 18, 372), (fx + 26, 396), (fx - 24, 396)], w=2.6, closed=True, fill='#a06a38', a=1)
    for sx, (ax, ay) in ((-1, p['armL']), (1, p['armR'])):
        d.line([(240 + sx * 84, 262 + dy), (ax + 30, ay + dy)], w=3.4, a=.8); d.blob(ax + 30, ay + dy, 10, 10, 9, w=2.4, fill='#f1d598', a=.7)
    d.blob(240, 240 + dy, 88, 100, 20, w=3, fill='#ebca8a', a=1.8)
    d.hatch(280, 200 + dy, 320, 290 + dy, 5)
    d.line([(180, 292 + dy), (192, 276 + dy), (288, 276 + dy), (300, 292 + dy), (292, 328 + dy), (188, 328 + dy)], w=3, closed=True, fill='#4fae70', a=1.2)
    for a in range(0, 360, 72): d.blob(240 + 13 * math.cos(math.radians(a)), 302 + dy + 13 * math.sin(math.radians(a)), 7, 7, 7, w=2, fill='#fff6e6', a=.5)
    d.blob(240, 302 + dy, 5, 5, 7, w=2, fill='#ffc83a', a=.4)
    ey = 226 + dy; kind = p['face']
    for ex in (208, 272):
        if kind in ('cheer', 'happy_blink'): d.line([(ex - 14, ey + 4), (ex, ey - 6), (ex + 14, ey + 4)], w=3, a=.6)
        else: d.blob(ex, ey, 13, 16, 10, w=2.8, fill='#fffdf5', a=.7); d.dot(ex + p.get('look', 3), ey + 2 + (4 if kind == 'sad' else 0), 5.5)
    if kind == 'sad': d.line([(190, 214 + dy), (220, 200 + dy)], w=3, a=.6); d.line([(260, 200 + dy), (290, 214 + dy)], w=3, a=.6)
    else: d.line([(190, 206 + dy), (220, 199 + dy)], w=3, a=.6); d.line([(260, 199 + dy), (290, 206 + dy)], w=3, a=.6)
    if kind in ('cheer', 'happy'): d.line([(214, 252 + dy), (240, 284 + dy), (266, 252 + dy)], w=2.8, closed=True, fill='#8a3a3a', a=.8)
    elif kind == 'sad':
        d.line([(222, 270 + dy), (240, 258 + dy), (258, 270 + dy)], w=3, a=.7)
        d.line([(284, 244 + dy), (278, 262 + dy), (290, 262 + dy)], w=2.2, closed=True, fill='#8fd6f5', a=.4)
    else: d.line([(224, 258 + dy), (240, 266 + dy), (256, 258 + dy)], w=3, a=.6)
    for cx in (184, 296): d.blob(cx, 246 + dy, 12, 8, 8, w=1.2, fill='#ff8fa3', a=.8, op=.5)
    sy = p.get('sprout', 0) + (-12 if kind == 'sad' else 0)
    d.line([(240, 140 + dy), (242, 118 + dy), (240 + sy * .4, 96 + dy)], w=4, a=.7)
    d.line([(240 + sy * .4, 102 + dy), (280 + sy, 60 + dy + (18 if kind == 'sad' else 0)), (324 + sy, 70 + dy + (30 if kind == 'sad' else 0)), (314 + sy, 106 + dy + (24 if kind == 'sad' else 0)), (270 + sy * .5, 112 + dy)], w=2.8, closed=True, fill='#5bb04a', a=1)
    d.line([(238 + sy * .4, 112 + dy), (200 - sy * .6, 80 + dy + (18 if kind == 'sad' else 0)), (158 - sy * .6, 90 + dy + (30 if kind == 'sad' else 0)), (176 - sy * .6, 126 + dy), (214, 130 + dy)], w=2.8, closed=True, fill='#3f8f3a', a=1)
    return d.svg(256, 256).replace('viewBox="0 0 256 256"', 'viewBox="55 40 370 370"')
POSES = {
    'Neutral': dict(face='neutral', armL=(130, 300), armR=(350, 300)),
    'Happy': dict(face='happy', armL=(120, 270), armR=(360, 270), sprout=8),
    'Sad': dict(face='sad', armL=(140, 320), armR=(340, 320), leg=4, dy=4),
    'Cheer': dict(face='cheer', armL=(112, 200), armR=(368, 200), dy=-10, leg=14, sprout=14),
}
import random
def vary(p, k):
    r = random.Random(500 + k); q = dict(p)
    q['armL'] = (p['armL'][0] + r.uniform(-7, 7), p['armL'][1] + r.uniform(-9, 9)); q['armR'] = (p['armR'][0] + r.uniform(-7, 7), p['armR'][1] + r.uniform(-9, 9))
    q['sprout'] = p.get('sprout', 0) + r.uniform(-6, 6); q['dy'] = p.get('dy', 0) + r.uniform(-3, 3); q['leg'] = p.get('leg', 8) + r.uniform(-3, 3)
    return q
k = 0
for i, (n, p) in enumerate(POSES.items()):
    variants = [(n, p, 40 + i)]
    if n in ('Happy', 'Cheer'):   # three drawings each, alternated by MainMenuView (boiling-line flipbook)
        variants += [('%s_%d' % (n, j), vary(p, k + j), 60 + i * 5 + j) for j in (1, 2)]; k += 3
    for name, pose, seed in variants:
        open('svg/Sprig_%s.svg' % name, 'w').write(sprig(seed, pose)); JOBS.append(('svg/Sprig_%s.svg' % name, R + 'Sprig_%s.png' % name, 256, 256, True))
json.dump(JOBS, open('jobs_sprig.json', 'w')); print(len(JOBS))
