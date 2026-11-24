#!/usr/bin/env python3
"""Transition doodles (Assets/Resources/Icons/Doodles/Doodle_0..7.png, 256 px): crayon scribbles (spiral, zigzag, swirl) and big flowers, a star and a leaf.
PageTurn pops them in over the screen, a wave at a time, until it is covered. Run from this folder:
python3 generate_doodles.py && NODE_PATH=$(npm root -g) node render.js jobs_doodles.json"""
import math, json
from ink import D, INK
JOBS = []
def thick(d, pts, color, w=12, ink=19):
    p = d.path(pts, False, 2.5, 20)
    d.o.append('<path d="%s" fill="none" stroke="%s" stroke-width="%d" stroke-linecap="round" stroke-linejoin="round"/>' % (p, INK, ink))
    d.o.append('<path d="%s" fill="none" stroke="%s" stroke-width="%d" stroke-linecap="round" stroke-linejoin="round"/>' % (p, color, w))
def emit(i, d):
    open('svg/Doodle_%d.svg' % i, 'w').write(d.svg(256, 256)); JOBS.append(('svg/Doodle_%d.svg' % i, '../../Assets/Resources/Icons/Doodles/Doodle_%d.png' % i, 256, 256, True))
import os; os.makedirs('../../Assets/Resources/Icons/Doodles', exist_ok=True)
d = D(1); thick(d, [(128 + (6 + 2.5 * k) * math.cos(k * .42), 128 + (6 + 2.5 * k) * math.sin(k * .42)) for k in range(42)], '#e8584a'); emit(0, d)           # spiral
d = D(2); thick(d, [(20, 200), (70, 40), (110, 210), (150, 50), (190, 205), (236, 70)], '#5b86e0', 13); thick(d, [(30, 150), (120, 120), (210, 160)], '#5b86e0', 9); emit(1, d)   # zigzag
def flower(i, petals, petal, center, r, seed):
    d = D(seed)
    for k in range(petals):
        a = 2 * math.pi * k / petals - math.pi / 2; d.blob(128 + r * .62 * math.cos(a), 128 + r * .62 * math.sin(a), r * .44, r * .44 if petals < 8 else r * .3, 10, w=7, fill=petal, a=1.5)
    d.blob(128, 128, r * .3, r * .3, 10, w=6, fill=center, a=1); emit(i, d)
flower(2, 5, '#f2a6c0', '#ffc83a', 100, 3); flower(3, 9, '#f7d26a', '#8a5a20', 104, 4); flower(6, 6, '#9ec3ee', '#ffd45a', 98, 5)
d = D(7)
pts = []
for k in range(10):
    rr = 112 if k % 2 == 0 else 50; t = -math.pi / 2 + k * math.pi / 5; pts.append((128 + rr * math.cos(t), 134 + rr * math.sin(t)))
d.line(pts, w=8, closed=True, fill='#f1c84a', a=2); emit(4, d)
d = D(8); d.line([(40, 220), (60, 90), (140, 30), (220, 40), (206, 130), (130, 210), (50, 224)], w=8, closed=True, fill='#7fcf6a', a=2); d.line([(48, 216), (190, 66)], w=5, a=1.5); emit(5, d)
d = D(9); thick(d, [(60 + 70 * math.cos(k * .5) + k * 3, 128 + 70 * math.sin(k * .5) * (1 - k / 40)) for k in range(34)], '#5aa845'); emit(7, d)
json.dump(JOBS, open('jobs_doodles.json', 'w')); print(len(JOBS))
