#!/usr/bin/env python3
"""Grid markers in the notebook style: growth stage discs and roots badges (Icons/Growth, 256), ice shard / flash (Icons, 96) and the invalid-placement X
(Icons/InvalidMark, 128). Run from this folder:  python3 generate_markers.py && NODE_PATH=$(npm root -g) node render.js jobs_markers.json"""
import math, json
from ink import D
R = '../../Assets/Resources/Icons/'
JOBS = []
def emit(name, d, px, png):
    open('svg/M_%s.svg' % name, 'w').write(d.svg(px, px)); JOBS.append(('svg/M_%s.svg' % name, R + png, px, px, True))
def disc(d, fill):
    d.blob(128, 128, 112, 112, 20, w=9, fill=fill, a=3)
def sprout(d):
    d.line([(128, 188), (128, 118)], w=9, a=1.5)
    d.line([(128, 134), (172, 96), (192, 112), (164, 146), (130, 148)], w=7, closed=True, fill='#d4f2b0', a=1.5)
    d.line([(128, 118), (84, 80), (64, 96), (92, 130), (126, 132)], w=7, closed=True, fill='#d4f2b0', a=1.5)
d = D(1); disc(d, '#7fcf6a'); sprout(d); emit('Bud', d, 256, 'Growth/Stage_Bud.png')
d = D(2); disc(d, '#f2a6c0')
for k in range(5):
    a = math.radians(72 * k - 90); d.blob(128 + 40 * math.cos(a), 128 + 40 * math.sin(a), 30, 30, 10, w=6, fill='#fff3e0', a=1.5)
d.blob(128, 128, 20, 20, 10, w=6, fill='#f1b92a', a=1); emit('Bloom', d, 256, 'Growth/Stage_Bloom.png')
d = D(3); disc(d, '#f7d26a'); d.blob(128, 140, 56, 54, 16, w=8, fill='#e8584a', a=2)
d.line([(128, 86), (134, 62)], w=7, a=1); d.line([(134, 70), (170, 56), (176, 76), (140, 80)], w=6, closed=True, fill='#7fcf6a', a=1.2); d.blob(106, 122, 10, 7, 8, w=0, fill='#ffd0c8', a=1); emit('Fruit', d, 256, 'Growth/Stage_Fruit.png')
d = D(4); disc(d, '#fff8e6'); d.blob(100, 128, 40, 40, 14, w=8, fill=None, a=2); d.blob(156, 128, 40, 40, 14, w=8, fill=None, a=2); emit('Resonance', d, 256, 'Growth/Roots_Resonance.png')
d = D(5); disc(d, '#fff8e6')
d.line([(88, 80), (116, 124), (112, 154), (84, 154), (60, 130), (66, 104)], w=7, closed=True, fill='#8fd6f5', a=2)
d.line([(136, 128), (190, 128)], w=9, a=1.5); d.line([(168, 106), (194, 128), (168, 150)], w=9, a=1.5); emit('Sap', d, 256, 'Growth/Roots_Sap.png')
d = D(6); d.line([(48, 6), (84, 36), (62, 90), (28, 56)], w=5, closed=True, fill='#9ec3ee', a=1.5); d.line([(48, 6), (50, 60)], w=3, a=1, op=.7); emit('IceShard', d, 96, 'IceShard.png')
d = D(7); k = 10; d.line([(48, 4), (48 + k, 48 - k), (92, 48), (48 + k, 48 + k), (48, 92), (48 - k, 48 + k), (4, 48), (48 - k, 48 - k)], w=4, closed=True, fill='#e6f6ff', a=1); emit('IceFlash', d, 96, 'IceFlash.png')
d = D(8); d.line([(24, 24), (104, 104)], w=22, a=2, step=12); d.line([(104, 24), (24, 104)], w=22, a=2, step=12)
d.o.append('<path d="M24 24 L104 104 M104 24 L24 104" stroke="#e8584a" stroke-width="11" stroke-linecap="round"/>'); emit('Invalid', d, 128, 'InvalidMark.png')
json.dump(JOBS, open('jobs_markers.json', 'w')); print(len(JOBS))
