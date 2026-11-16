#!/usr/bin/env python3
"""Notebook-style tiles (128 px, Assets/Resources/Tiles + Icons/LockedTile, GoldenStar, Petals, Attacks): a crayon bed in the piece's color with an
ink doodle of its flower. Run from this folder:  python3 generate_tiles.py && NODE_PATH=$(npm root -g) node render.js jobs_tiles.json"""
import os, math, json
from ink import D, INK
R = '../../Assets/Resources/'
os.makedirs('svg', exist_ok=True)
JOBS = []
def emit(name, d, w, h, png):
    open('svg/%s.svg' % name, 'w').write(d.svg(w, h)); JOBS.append(('svg/%s.svg' % name, R + png, w, h, True))
def tile(seed, bed, draw, name, png, lw=4):
    d = D(seed); d.box(5, 5, 118, 118, bed, lw, 1.3, 16, 40); draw(d); emit(name, d, 128, 128, png)
def petal(d, cx, cy, ang, r, w, fill, r_in=6):
    a = math.radians(ang); ux, uy = math.cos(a), math.sin(a); nx, ny = -uy, ux
    P = lambda t, s: (cx + ux * t + nx * s, cy + uy * t + ny * s)
    d.line([P(r_in, 0), P(r_in + (r - r_in) * .4, -w), P(r * .9, -w * .6), P(r, 0), P(r * .9, w * .6), P(r_in + (r - r_in) * .4, w)], w=2.4, closed=True, fill=fill, a=.7)
def poppy(d):
    for dx, dy in ((-1, -1), (1, -1), (-1, 1), (1, 1)): d.blob(64 + 14 * dx, 64 + 14 * dy, 21, 21, 12, w=2.6, fill='#e8483f', a=.8)
    d.blob(64, 64, 12, 12, 10, w=2.4, fill='#2a1418', a=.6)
def iris(d):
    for x1, x2 in ((-1, 1),):
        d.line([(64, 76), (48, 100), (30, 104), (42, 84)], w=2.4, closed=True, fill='#3d68c4', a=.7)
        d.line([(64, 76), (80, 100), (98, 104), (86, 84)], w=2.4, closed=True, fill='#3d68c4', a=.7)
        d.line([(64, 74), (44, 60), (30, 38), (50, 50)], w=2.4, closed=True, fill='#6f9ae6', a=.7)
        d.line([(64, 74), (84, 60), (98, 38), (78, 50)], w=2.4, closed=True, fill='#6f9ae6', a=.7)
    d.line([(64, 78), (50, 40), (64, 18), (78, 40)], w=2.6, closed=True, fill='#5b86e0', a=.7)
    d.blob(64, 76, 5, 5, 8, w=2, fill='#ffd45a', a=.4)
def sunflower(d):
    for k in range(12): petal(d, 64, 64, k * 30, 46, 9, '#f1b92a', 14)
    d.blob(64, 64, 18, 18, 12, w=2.6, fill='#6b4020', a=.7)
def fern(d):
    pts = []
    for k in range(60):
        t = k / 59.0; th = t * 2.4 * math.pi; r = 4 + 27 * t; pts.append((64 + r * math.cos(th - math.pi / 2), 50 + r * math.sin(th - math.pi / 2)))
    pts += [(pts[-1][0] - 4, pts[-1][1] + 30), (62, 108)]
    d.line(pts, w=9, a=.4, step=8); d.o.append('<path d="%s" fill="none" stroke="#4f9a3c" stroke-width="5" stroke-linecap="round"/>' % d.path(pts, False, .3, 8))
def joker(d):
    for c, ph in zip(('#e8483f', '#4f7fd0', '#e8a82e', '#5a9e45'), (-45, 45, 135, 225)): petal(d, 64, 64, ph, 44, 16, c, 8)
    d.blob(64, 64, 9, 9, 8, w=2.2, fill='#fff7e8', a=.4)
def star(d, cx, cy, r, fill):
    k = r * .3; d.line([(cx, cy - r), (cx + k, cy - k), (cx + r, cy), (cx + k, cy + k), (cx, cy + r), (cx - k, cy + k), (cx - r, cy), (cx - k, cy - k)], w=2.4, closed=True, fill=fill, a=.5)
def golden(d): star(d, 64, 64, 28, '#f1c84a'); star(d, 34, 36, 9, '#e0a83a'); star(d, 94, 92, 10, '#e0a83a')
def frozen(d):
    for k in range(6):
        a = math.radians(60 * k - 90); d.line([(64, 64), (64 + 36 * math.cos(a), 64 + 36 * math.sin(a))], w=5, a=.4)
        m = (64 + 22 * math.cos(a), 64 + 22 * math.sin(a))
        for s in (-1, 1): d.line([m, (m[0] + 10 * math.cos(a + s * .8), m[1] + 10 * math.sin(a + s * .8))], w=3, a=.3)
def bonus(d):
    p = []
    for i in range(16):
        a = math.pi * i / 8 - math.pi / 2; rr = 44 if i % 2 == 0 else 31; p.append((64 + rr * math.cos(a), 64 + rr * math.sin(a)))
    d.line(p, w=2.6, closed=True, fill='#7fb0ee', a=.6)
    d.o.append('<text x="64" y="80" font-family="PH, Patrick Hand, Comic Sans MS, cursive" font-size="50" font-weight="700" text-anchor="middle" fill="#2a2420">x2</text>')
def locked(d):
    for a in ((16, 28, 112, 104), (112, 30, 16, 100)):
        d.line([(a[0], a[1]), ((a[0] + a[2]) / 2 + 8, (a[1] + a[3]) / 2 - 6), (a[2], a[3])], w=8, a=.8, step=10)
        d.o.append('<path d="M%d %d Q%d %d %d %d" fill="none" stroke="#6a8f4a" stroke-width="4" stroke-linecap="round"/>' % (a[0], a[1], (a[0] + a[2]) / 2 + 8, (a[1] + a[3]) / 2 - 6, a[2], a[3]))
    d.hatch(20, 18, 108, 44, 6)
tile(300, '#ec9a8c', poppy, 'T_Ember', 'Tiles/Ember.png'); tile(301, '#9ec3ee', iris, 'T_Frost', 'Tiles/Frost.png')
tile(302, '#f7d26a', sunflower, 'T_Gold', 'Tiles/Gold.png'); tile(303, '#9fd37e', fern, 'T_Moss', 'Tiles/Moss.png')
tile(304, '#c3a6e0', joker, 'T_Joker', 'Tiles/Joker.png'); tile(305, '#e0cd9e', golden, 'T_Golden', 'Tiles/Golden.png')
tile(306, '#cfe8f5', frozen, 'T_Frozen', 'Tiles/Frozen.png'); tile(307, '#9ec3ee', bonus, 'T_Bonus', 'Tiles/Bonus.png')
tile(308, '#c9bfa8', locked, 'T_Locked', 'Icons/LockedTile.png')
d = D(309); star(d, 64, 64, 52, '#f1c84a'); emit('GoldenStar', d, 128, 128, 'Icons/GoldenStar.png')
# --- harvest petals (128, pointing up) and attack projectiles (256, pointing right)
COL = {'Coral': '#e8483f', 'Teal': '#5b86e0', 'Violet': '#f1b92a', 'Lime': '#5aa845', 'Joker': '#a77ccf'}
for i, (n, c) in enumerate(COL.items()):
    d = D(320 + i); petal(d, 64, 118, -90, 100, 30, c, 4); d.line([(64, 112), (64, 40)], w=1.8, a=.5, op=.6); emit('Petal_' + n, d, 128, 128, 'Icons/Petals/%s.png' % n)
    d = D(340 + i)
    for k, off in enumerate((-18, 0, 18)): d.line([(8, 128 + off), (60, 128 + off * .8)], w=3, a=1.2, op=.55)
    petal(d, 70, 128, 0, 170, 52, c, 6); d.line([(80, 128), (220, 128)], w=2.2, a=.8, op=.6); emit('Attack_' + n, d, 256, 256, 'Icons/Attacks/%s.png' % n)
json.dump(JOBS, open('jobs_tiles.json', 'w')); print(len(JOBS), 'sprites')

# --- menu -> game transition sweep: square, edge-to-edge versions (no gaps between neighbours): flat bed to the edge, ink lines inset
for i, (n, bed, fn) in enumerate((('Ember', '#ec9a8c', poppy), ('Frost', '#9ec3ee', iris), ('Gold', '#f7d26a', sunflower), ('Moss', '#9fd37e', fern))):
    d = D(360 + i); d.o.append('<rect width="128" height="128" fill="%s"/>' % bed)
    d.line([(2, 2), (126, 2), (126, 126), (2, 126)], w=4, closed=True, a=1.6, step=20)
    fn(d); emit('Sweep_' + n, d, 128, 128, 'Tiles/Sweep/%s.png' % n)
json.dump(JOBS, open('jobs_tiles.json', 'w')); print(len(JOBS), 'sprites')
