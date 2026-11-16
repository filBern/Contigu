#!/usr/bin/env python3
"""Notebook-style sprites for the UI (same names/sizes as the old garden art, so no C# change is needed): plot buttons, card, bars, badges,
slot group, empty cell, challenge sceneries. Writes svg/<name>.svg, then render.js renders them into Assets/Resources (transparent PNGs).
Run from this folder:  python3 generate_notebook.py && NODE_PATH=$(npm root -g) node render.js"""
import os, math, json
from ink import D, INK
R = '../../Assets/Resources/'
os.makedirs('svg', exist_ok=True)
JOBS = []   # (svg file, png path, w, h)
def emit(name, d, w, h, png, bg=None):
    open('svg/%s.svg' % name, 'w').write(d.svg(w, h, bg)); JOBS.append(('svg/%s.svg' % name, R + png, w, h, bg is None))

# --- plot buttons: crayon bed + ink outline. Light = pale inset with a colored band
PASTEL = {'Green': '#7fcf6a', 'Gold': '#ffd95a', 'Blue': '#9ec3ee', 'Red': '#e8908a', 'Wood': '#d7c38e'}
TINT = {'Green': '#d4ebb8', 'Gold': '#f8ebb0', 'Blue': '#d3e4f7', 'Red': '#f2cfc8', 'Wood': '#e8d8b0'}
for i, n in enumerate(PASTEL):
    d = D(100 + i); d.box(6, 6, 372, 148, PASTEL[n], 5, 1.6, 24, 40); emit('Plot' + n, d, 384, 160, 'Buttons/Plot/Plot%s.png' % n)
    d = D(120 + i); d.box(6, 6, 372, 148, PASTEL[n], 5, 1.6, 24, 40); d.box(26, 26, 332, 108, TINT[n], 3, 1.2, 14, 40)
    emit('Plot%sLight' % n, d, 384, 160, 'Buttons/Plot/Plot%sLight.png' % n)
d = D(140); d.blob(64, 64, 52, 52, 14, w=5, fill='#ffd95a', a=1.4); emit('SlotBadge', d, 128, 128, 'Buttons/Plot/SlotBadge.png')

# --- panels
d = D(150); d.box(8, 8, 240, 240, '#e5d3a8', 5, 1.8, 40, 40); emit('Card', d, 256, 256, 'UI/Garden/Card.png')
d = D(151); d.box(6, 6, 480, 96, '#fff8e6', 5, 1.6, 44, 40); emit('BarTrack', d, 492, 108, 'UI/Garden/BarTrack.png')
for i, (n, c) in enumerate((('Gold', '#ffd95a'), ('Blue', '#9ec3ee'), ('Green', '#7fcf6a'))):
    d = D(160 + i); d.box(5, 5, 466, 70, c, 4, 1.4, 30, 40); emit('Fill' + n, d, 476, 80, 'UI/Garden/Fill%s.png' % n)
d = D(170); d.blob(50, 50, 40, 40, 14, w=5, fill='#ffd95a', a=1.3); emit('CountBadge', d, 100, 100, 'UI/Garden/CountBadge.png')
d = D(171); d.blob(50, 50, 40, 40, 14, w=5, fill='#fff8e6', a=1.3); d.blob(50, 50, 16, 16, 10, w=3, fill='#9ec3ee', a=.8); emit('SliderHandle', d, 100, 100, 'UI/Garden/SliderHandle.png')
d = D(172); d.box(6, 6, 332, 130, '#d7c38e', 5, 1.6, 26, 40); emit('SlotGroup', d, 344, 142, 'Buttons/SlotGroup.png')
d = D(173); d.box(8, 8, 519, 282, '#e5d3a8', 5, 1.8, 34, 40); emit('BackgroundCombo', d, 535, 298, 'Buttons/BackgroundCombo.png')
d = D(174); d.box(5, 5, 118, 118, '#e0cd9e', 4, 1.6, 16, 40); d.hatch(86, 16, 112, 42, 3); emit('Empty', d, 128, 128, 'Tiles/Empty.png')

# --- challenge sceneries: tinted ruled paper + a few themed doodles in the margins
THEMES = {'Spring': '#ecdfc3', 'Summer': '#f0dfb0', 'Autumn': '#ecd2a8', 'Winter': '#e3e4e0', 'Orchard': '#ebdcb6', 'Greenhouse': '#dde5cd'}
def star(d, cx, cy, r, k=5):
    pts = []
    for i in range(2 * k):
        rr = r if i % 2 == 0 else r * .45; t = -math.pi / 2 + i * math.pi / k; pts.append((cx + rr * math.cos(t), cy + rr * math.sin(t)))
    d.line(pts, w=2.4, closed=True, a=.9)
def flower(d, cx, cy, r, petal, n=5):
    for i in range(n):
        a = 2 * math.pi * i / n; d.blob(cx + r * .55 * math.cos(a), cy + r * .55 * math.sin(a), r * .42, r * .42, 7, w=2.2, fill=petal, a=.5)
    d.blob(cx, cy, r * .3, r * .3, 7, w=2, fill='#ffc83a', a=.4)
def sun(d, cx, cy, r):
    d.blob(cx, cy, r, r, 14, w=3, fill='#ffd95a', a=1)
    for k in range(10):
        a = k * math.pi / 5; d.line([(cx + (r + 8) * math.cos(a), cy + (r + 8) * math.sin(a)), (cx + (r + 26) * math.cos(a), cy + (r + 26) * math.sin(a))], w=2.6, a=.8)
def leaf(d, cx, cy, s, col):
    d.line([(cx, cy), (cx + 20 * s, cy - 26 * s), (cx + 52 * s, cy - 20 * s), (cx + 46 * s, cy + 12 * s)], w=2.6, closed=True, fill=col, a=1)
    d.line([(cx + 4 * s, cy - 2 * s), (cx + 40 * s, cy - 12 * s)], w=1.8, a=.6)
def snow(d, cx, cy, r):
    for k in range(6):
        a = k * math.pi / 3; d.line([(cx, cy), (cx + r * math.cos(a), cy + r * math.sin(a))], w=2.6, a=.5)
        m = (cx + r * .6 * math.cos(a), cy + r * .6 * math.sin(a))
        for sgn in (-1, 1): d.line([m, (m[0] + r * .3 * math.cos(a + sgn * .8), m[1] + r * .3 * math.sin(a + sgn * .8))], w=2, a=.4)
def apple(d, cx, cy, r):
    d.blob(cx, cy, r, r * .9, 12, w=2.8, fill='#e8584a', a=1); d.line([(cx, cy - r * .8), (cx + 4, cy - r * 1.4)], w=2.6, a=.5); leaf(d, cx + 2, cy - r * 1.1, .5, '#7fcf6a')
def pane(d, x, y, w, h):
    d.box(x, y, w, h, '#cfe3dd', 3, 1.4, 4); d.line([(x + w / 2, y), (x + w / 2, y + h)], w=2.4, a=.8); d.line([(x, y + h / 2), (x + w, y + h / 2)], w=2.4, a=.8)
DOOD = {
    'Spring': lambda d: [flower(d, 60, 90, 30, '#f2a6c0'), flower(d, 1210, 640, 26, '#fff3e0'), flower(d, 1160, 70, 22, '#9ec3ee'), star(d, 90, 650, 16)],
    'Summer': lambda d: [sun(d, 1160, 90, 34), flower(d, 70, 640, 28, '#ffd95a', 8), star(d, 90, 100, 14)],
    'Autumn': lambda d: [leaf(d, 70, 120, 1.3, '#e08a3c'), leaf(d, 1180, 110, 1.1, '#c9532a'), leaf(d, 1150, 640, 1.2, '#e6b43a'), leaf(d, 90, 650, 1, '#b8452a')],
    'Winter': lambda d: [snow(d, 90, 100, 40), snow(d, 1180, 110, 30), snow(d, 1200, 640, 36), snow(d, 80, 640, 24)],
    'Orchard': lambda d: [apple(d, 90, 110, 28), apple(d, 1190, 630, 30), apple(d, 1170, 100, 22), star(d, 80, 650, 14)],
    'Greenhouse': lambda d: [pane(d, 50, 60, 90, 90), pane(d, 1150, 60, 90, 90), flower(d, 90, 650, 26, '#f2a6c0'), flower(d, 1200, 640, 24, '#ffd95a')],
}
for i, (n, c) in enumerate(THEMES.items()):
    d = D(200 + i)
    d.o.append('<rect width="1280" height="720" filter="url(#paper)"/>')
    for y in range(34, 720, 34): d.o.append('<path d="M0 %d H1280" stroke="#8f9aa6" stroke-width="1.3" opacity=".5"/>' % y)
    d.o.append('<path d="M30 0 V720" stroke="#d98a8a" stroke-width="1.5" opacity=".55"/>')
    DOOD[n](d); emit('Bg' + n, d, 1280, 720, 'Backgrounds/%s.png' % n, bg=c)
json.dump(JOBS, open('jobs.json', 'w'))
print(len(JOBS), 'sprites')
