#!/usr/bin/env python3
"""Shop dressing in the scratchy notebook style: shelf surface (Icons/ShelfTop.png), shelf front (Icons/ShelfFront.png) and the striped awning with a
scalloped edge (Icons/ShopAwning.png). Run from this folder:  python3 generate_shop.py && NODE_PATH=$(npm root -g) node render.js jobs_shop.json"""
import json, math
from ink import D, INK
JOBS = []
def emit(name, d, w, h, png):
    open('svg/%s.svg' % name, 'w').write(d.svg(w, h)); JOBS.append(('svg/%s.svg' % name, '../../Assets/Resources/Icons/%s' % png, w, h, True))
d = D(701, hatch=False); d.box(6, 6, 1188, 36, '#ecd9a8', 6, 1.4, 8, 60)
for x in range(40, 1180, 60): d.line([(x, 14), (x + 30, 34)], w=2, a=1, op=.45, step=40)
emit('ShelfTop', d, 1200, 48, 'ShelfTop.png')
d = D(702, hatch=False); d.box(6, 6, 1188, 28, '#c79a62', 6, 1.4, 6, 60)
for x in range(30, 1180, 44): d.line([(x, 12), (x + 8, 28)], w=2.2, a=1, op=.5, step=40)
emit('ShelfFront', d, 1200, 40, 'ShelfFront.png')
W, H = 1280, 80
a = D(703, hatch=False)
n = 16; sw = W / n
for i in range(n):
    col = '#ee9a8e' if i % 2 == 0 else '#fdf3d6'
    x0, x1 = i * sw, (i + 1) * sw
    pts = [(x0, 4), (x1, 4)] + [(x1 - sw * .5 * (1 - math.cos(math.pi * t / 6)) , 52 + 18 * math.sin(math.pi * t / 6)) for t in range(0, 7)]
    pts = [(x0, 4), (x1, 4), (x1, 52)] + [(x1 - sw * t / 8, 52 + 20 * math.sin(math.pi * t / 8)) for t in range(1, 8)] + [(x0, 52)]
    a.line(pts, w=5, closed=True, fill=col, a=1.2, step=40)
emit('ShopAwning', a, W, H, 'ShopAwning.png')
json.dump(JOBS, open('jobs_shop.json', 'w')); print(len(JOBS))
