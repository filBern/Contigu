#!/usr/bin/env python3
"""Pollen icon (Assets/Resources/Icons/Pollen.png + Pollen_1/_2, 256 px): a ball of yellow crayon scribbles, three drawings that SpriteFlipbook alternates so it
seems to be scribbled over and over. Run from this folder:  python3 generate_pollen.py && NODE_PATH=$(npm root -g) node render.js jobs_pollen.json"""
import math, json, random
from ink import D, INK
JOBS = []
for f in range(3):
    d = D(2026 + f * 7)
    r = random.Random(40 + f * 11)
    cx = cy = 128
    # the body of the ball: a rough crayon disc
    pts = [(cx + (96 + r.uniform(-6, 6)) * math.cos(a), cy + (96 + r.uniform(-6, 6)) * math.sin(a)) for a in [i * math.pi / 8 for i in range(16)]]
    d.line(pts, w=0, closed=True, fill='#f7c93a', a=3, step=30)
    # scribbled loops across the ball (darker and lighter yellow), like a kid colouring it in
    for k in range(9):
        a0 = r.uniform(0, 6.28); rr = r.uniform(30, 88); loops = []
        for t in range(26):
            ang = a0 + t * 0.55
            rad = rr * (0.55 + 0.45 * math.sin(t * 0.4 + k)) + r.uniform(-4, 4)
            loops.append((cx + rad * math.cos(ang), cy + rad * math.sin(ang)))
        col = ['#e8a820', '#fbe27a', '#f0b52a'][k % 3]
        p = d.path(loops, False, 3, 24)
        d.o.append('<path d="%s" fill="none" stroke="%s" stroke-width="7" stroke-linecap="round" stroke-linejoin="round" opacity="0.85"/>' % (p, col))
    # the ink: a rough circular outline, scribbled twice around
    for k in range(3):
        ring = [(cx + (98 + r.uniform(-5, 5) + k * 2) * math.cos(a), cy + (98 + r.uniform(-5, 5)) * math.sin(a)) for a in [i * math.pi / 10 + k * 0.2 for i in range(21)]]
        d.o.append('<path d="%s" fill="none" stroke="%s" stroke-width="%.1f" stroke-linecap="round" opacity="%.2f"/>' % (d.path(ring, False, 2, 40), INK, 6 - k * 1.6, 1 - k * 0.15))
    d.blob(96, 92, 16, 9, 10, w=3, fill='#fff3b8', a=1)
    name = 'Pollen' if f == 0 else 'Pollen_%d' % f
    open('svg/%s.svg' % name, 'w').write(d.svg(256, 256))
    JOBS.append(('svg/%s.svg' % name, '../../Assets/Resources/Icons/%s.png' % name, 256, 256, True))
json.dump(JOBS, open('jobs_pollen.json', 'w'))
