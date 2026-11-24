#!/usr/bin/env python3
"""Page-turn transition art (Icons/PageTurn_Paper.png: ruled notebook page with a margin line, doodles and a wobbly inked leading edge;
Icons/PageTurn_Green.png: a green crayon sheet, the first one to cross the screen). 1280x720, the leading (left) 40 px are the edge.
Run from this folder:  python3 generate_page.py && NODE_PATH=$(npm root -g) node render.js jobs_page.json"""
import math, json
from ink import D
W, H, E = 1280, 720, 40
def edge_pts(d, x0, n=22):
    return [(x0 + d.r.uniform(-7, 7), H * k / n) for k in range(n + 1)]
def sheet(d, fill):
    pts = edge_pts(d, E) + [(W + 20, H), (W + 20, 0)]
    pts = [(E + d.r.uniform(-7, 7), H * k / 22) for k in range(23)][::-1] + [(W + 20, 0), (W + 20, H)]
    d.line(pts, w=0, closed=True, fill=fill, a=0, step=30)   # flat fill (no stroke)
    edge = [(E + d.r.uniform(-6, 6), H * k / 24) for k in range(25)]
    d.line(edge, w=9, a=2.5, step=40)
def star(d, cx, cy, r):
    pts = []
    for i in range(10):
        rr = r if i % 2 == 0 else r * .45; t = -math.pi / 2 + i * math.pi / 5; pts.append((cx + rr * math.cos(t), cy + rr * math.sin(t)))
    d.line(pts, w=3, closed=True, a=1)
def flower(d, cx, cy, r, petal):
    for i in range(5):
        a = 2 * math.pi * i / 5; d.blob(cx + r * .55 * math.cos(a), cy + r * .55 * math.sin(a), r * .42, r * .42, 8, w=3, fill=petal, a=.7)
    d.blob(cx, cy, r * .3, r * .3, 8, w=2.5, fill='#ffc83a', a=.5)
# paper
d = D(91)
sheet(d, '#ecdfc3')
for y in range(34, H, 34): d.o.append('<path d="M%d %d H%d" stroke="#8f9aa6" stroke-width="1.6" opacity=".5"/>' % (E + 6, y, W))
d.o.append('<path d="M%d 0 V%d" stroke="#d98a8a" stroke-width="2" opacity=".6"/>' % (E + 90, H))
flower(d, 330, 170, 52, '#f2a6c0'); flower(d, 960, 520, 46, '#9ec3ee'); star(d, 700, 110, 34); star(d, 190, 560, 26); star(d, 1130, 200, 22)
d.line([(430, 400), (560, 360), (700, 410), (830, 360)], w=4, a=3, step=24)
d.line([(820, 600), (900, 570), (880, 548)], w=4, a=1); d.line([(900, 570), (876, 590)], w=4, a=1)
for k in range(4): d.line([(520 + k * 26, 610), (532 + k * 26, 640)], w=3, a=1)
open('svg/PageTurn_Paper.svg', 'w').write(d.svg(W + 20, H))
# green crayon sheet
g = D(92)
sheet(g, '#9fd37e')
for k in range(10): g.line([(E + 40 + k * 120, 40), (E + 70 + k * 120, 680)], w=3, a=6, step=60, op=.35)
flower(g, 520, 360, 70, '#fff3e0')
open('svg/PageTurn_Green.svg', 'w').write(g.svg(W + 20, H))
json.dump([('svg/PageTurn_Paper.svg', '../../Assets/Resources/Icons/PageTurn_Paper.png', W + 20, H, True), ('svg/PageTurn_Green.svg', '../../Assets/Resources/Icons/PageTurn_Green.png', W + 20, H, True)], open('jobs_page.json', 'w'))
