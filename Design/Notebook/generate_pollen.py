#!/usr/bin/env python3
"""Pollen icon (Assets/Resources/Icons/Pollen.png, 256 px) in the notebook style: a crayon-yellow grain with six bumps, ink outline, a small highlight.
Run from this folder:  python3 generate_pollen.py && NODE_PATH=$(npm root -g) node render.js jobs_pollen.json"""
import math, json
from ink import D
d = D(2026)
for k in range(6):
    a = math.radians(60 * k - 90); d.blob(128 + 70 * math.cos(a), 128 + 70 * math.sin(a), 34, 34, 12, w=7, fill='#f7c93a', a=2)
d.blob(128, 128, 62, 62, 18, w=0, fill='#f7c93a', a=1)           # covers the inner outlines of the bumps
d.o.append('<circle cx="128" cy="128" r="60" fill="#f7c93a"/>')
d.blob(104, 104, 14, 9, 10, w=3, fill='#fff3b8', a=1)
open('svg/Pollen.svg', 'w').write(d.svg(256, 256))
json.dump([('svg/Pollen.svg', '../../Assets/Resources/Icons/Pollen.png', 256, 256, True)], open('jobs_pollen.json', 'w'))
