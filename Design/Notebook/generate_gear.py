#!/usr/bin/env python3
"""Settings gear (Assets/Resources/Icons/Gear.png, 128 px), scratchy notebook style. Run: python3 generate_gear.py && NODE_PATH=$(npm root -g) node render.js jobs_gear.json"""
import math, json
from ink import D
d = D(33)
pts = []
n = 8
for i in range(n * 2):
    a0 = 2 * math.pi * i / (n * 2) - math.pi / (n * 2)
    r = 54 if i % 2 == 0 else 40
    pts.append((64 + r * math.cos(a0), 64 + r * math.sin(a0)))
    pts.append((64 + r * math.cos(a0 + math.pi / (n * 2) * .9), 64 + r * math.sin(a0 + math.pi / (n * 2) * .9)))
d.line(pts, w=5, closed=True, fill='#d9d2c0', a=1.2)
d.blob(64, 64, 18, 18, 12, w=5, fill='#ecdfc3', a=1)
open('svg/Gear.svg', 'w').write(d.svg(128, 128))
json.dump([('svg/Gear.svg', '../../Assets/Resources/Icons/Gear.png', 128, 128, True)], open('jobs_gear.json', 'w'))
