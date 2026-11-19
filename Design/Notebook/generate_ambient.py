#!/usr/bin/env python3
"""Notebook-style ambient sprites (Assets/Resources/Ambient, 96 px) and the plot-button flowers (Buttons/Plot/Flowers, 64 px).
Run from this folder:  python3 generate_ambient.py && NODE_PATH=$(npm root -g) node render.js jobs_amb.json"""
import math, json
from ink import D
R = '../../Assets/Resources/'
JOBS = []
def emit(name, d, px, png):
    open('svg/%s.svg' % name, 'w').write(d.svg(px, px)); JOBS.append(('svg/%s.svg' % name, R + png, px, px, True))
def amb(name, seed, fn, px=96):
    d = D(seed); fn(d); emit('A_' + name, d, px, 'Ambient/%s.png' % name)
W = 2.6
def bee(d):
    d.line([(14, 44), (4, 40)], w=2, a=.4)
    d.line([(44, 20), (50, 4), (64, 10), (58, 26)], w=2.2, closed=True, fill='#e9f2fb', a=.5)
    d.blob(44, 52, 30, 22, 14, w=W, fill='#ffd24a', a=.8)
    for x in (36, 50): d.line([(x, 32), (x + 3, 72)], w=5, a=.4, step=10)
    d.blob(72, 50, 12, 11, 10, w=2.4, fill='#ffd24a', a=.5); d.dot(76, 47, 2.4)
def butterfly(d):
    for sx in (-1, 1):
        d.blob(48 + sx * 24, 34, 20, 24, 12, w=W, fill='#f2994a', a=.7); d.blob(48 + sx * 18, 66, 14, 16, 10, w=W, fill='#f7b36d', a=.7)
        d.blob(48 + sx * 24, 32, 6, 6, 8, w=1.6, fill='#fff3e0', a=.3)
    d.line([(48, 22), (48, 80)], w=5, a=.4); d.line([(48, 22), (40, 8)], w=2, a=.3); d.line([(48, 22), (58, 8)], w=2, a=.3)
def cat(d):
    d.line([(14, 40), (8, 24), (20, 18)], w=3, a=.5)
    d.blob(44, 52, 28, 16, 12, w=W, fill='#f2a24a', a=.7)
    for x in (24, 36, 54, 64): d.line([(x, 62), (x - 2, 84)], w=3.4, a=.4)
    d.blob(72, 40, 14, 13, 10, w=W, fill='#f2a24a', a=.5)
    d.line([(62, 30), (64, 16), (72, 26)], w=2.4, closed=True, fill='#f2a24a', a=.3); d.line([(74, 27), (82, 16), (84, 32)], w=2.4, closed=True, fill='#f2a24a', a=.3)
    d.dot(70, 38, 2.2); d.dot(78, 38, 2.2)
def flame(d):
    d.line([(48, 6), (66, 34), (76, 58), (62, 84), (34, 84), (20, 60), (30, 38), (38, 46)], w=W, closed=True, fill='#f2762e', a=.8)
    d.line([(48, 40), (58, 60), (54, 78), (40, 78), (36, 62)], w=2.2, closed=True, fill='#ffd24a', a=.5)
def leaf(d):
    d.line([(12, 84), (22, 40), (56, 16), (86, 12), (80, 46), (54, 72), (20, 82)], w=W, closed=True, fill='#6fb04f', a=.8)
    d.line([(14, 82), (66, 30)], w=2, a=.5, op=.7)
def maple(d):
    P = [(48, 6), (58, 28), (80, 22), (72, 44), (90, 52), (66, 64), (68, 82), (50, 74), (48, 92), (46, 74), (28, 82), (30, 64), (6, 52), (24, 44), (16, 22), (38, 28)]
    d.line(P, w=W, closed=True, fill='#e0662e', a=.5); d.line([(48, 92), (48, 34)], w=2, a=.4, op=.7)
def petal(d):
    d.line([(48, 8), (68, 36), (62, 70), (48, 88), (34, 70), (28, 36)], w=W, closed=True, fill='#f8b6cb', a=.8); d.line([(48, 14), (48, 80)], w=1.8, a=.4, op=.6)
def raindrop(d):
    d.line([(48, 8), (66, 44), (68, 62), (58, 80), (38, 80), (28, 62), (30, 44)], w=W, closed=True, fill='#7cc4f0', a=.8)
def snow(d):
    for k in range(6):
        a = k * math.pi / 3; d.line([(48, 48), (48 + 40 * math.cos(a), 48 + 40 * math.sin(a))], w=3.2, a=.5)
        m = (48 + 26 * math.cos(a), 48 + 26 * math.sin(a))
        for s in (-1, 1): d.line([m, (m[0] + 12 * math.cos(a + s * .8), m[1] + 12 * math.sin(a + s * .8))], w=2.6, a=.3)
def sparkle(d):
    k = 12; d.line([(48, 4), (48 + k, 48 - k), (92, 48), (48 + k, 48 + k), (48, 92), (48 - k, 48 + k), (4, 48), (48 - k, 48 - k)], w=W, closed=True, fill='#fff3b0', a=.6)
def sunmote(d):
    d.blob(48, 48, 36, 36, 14, w=W, fill='#ffd966', a=.9); d.blob(38, 38, 9, 9, 8, w=1.6, fill='#fff3c0', a=.4)
for i, (n, f) in enumerate((('Bee', bee), ('Butterfly', butterfly), ('Cat', cat), ('Flame', flame), ('Leaf', leaf), ('MapleLeaf', maple), ('Petal', petal), ('Raindrop', raindrop), ('Snowflake', snow), ('Sparkle', sparkle), ('SunMote', sunmote))):
    amb(n, 400 + i, f)
# flipbook frames of the roaming creatures and particles (Bee_1/_2 ...), alternated in game by SpriteFlipbook
for k in (1, 2):
    for i, (n, f) in enumerate((('Bee', bee), ('Butterfly', butterfly), ('Cat', cat), ('Flame', flame), ('Leaf', leaf), ('MapleLeaf', maple), ('Petal', petal), ('Raindrop', raindrop), ('Snowflake', snow), ('Sparkle', sparkle), ('SunMote', sunmote))):
        amb('%s_%d' % (n, k), 600 + k * 30 + i, f)
# plot-button flowers
for i, (n, c, ctr, k) in enumerate((('Red', '#e5534b', '#2a1418', 5), ('Blue', '#6f9ae6', '#ffd45a', 6), ('Yellow', '#f5c542', '#8a5a20', 8), ('White', '#fff3e0', '#e8a82e', 6))):
    d = D(430 + i)
    for j in range(k):
        a = 2 * math.pi * j / k; d.blob(32 + 15 * math.cos(a), 32 + 15 * math.sin(a), 11, 11, 8, w=2.4, fill=c, a=.5)
    d.blob(32, 32, 8, 8, 8, w=2.2, fill=ctr, a=.4); emit('F_' + n, d, 64, 'Buttons/Plot/Flowers/Flower%s.png' % n)
json.dump(JOBS, open('jobs_amb.json', 'w')); print(len(JOBS))
