#!/usr/bin/env python3
"""Reclaimer's shield (Icons/Shield.png) and its shards (Icons/ShieldShard.png), 256 px, as notebook LINEWORK: ink outline only, a pale wash and a paper halo so it
reads over the enemy, a sprout in ink. Run from this folder:  python3 generate_shield.py && NODE_PATH=$(npm root -g) node render.js jobs_shield.json"""
import json
from ink import D, INK
JOBS = []
def shield(variant):
    import random
    r = random.Random(70 + variant)
    d = D(808 + variant * 17)
    body = [(128, 24), (200, 46), (208, 120), (190, 190), (128, 236), (66, 190), (48, 120), (56, 46)]
    d.o.append('<path d="%s" fill="#fff8e6" fill-opacity="0.5" stroke="#fff8e6" stroke-width="17" stroke-linejoin="round" stroke-opacity="0.9"/>' % d.path(body, True, 2.5, 22))   # paper wash + halo so it reads on dark pests
    d.line(body, w=7, closed=True, a=2.5, step=22)          # outline only (linework): the enemy shows through
    d.line([(128, 46), (182, 62), (188, 120), (174, 174), (128, 210), (82, 174), (68, 120), (74, 62)], w=3, closed=True, a=1.8, step=22, op=.8)   # inner line
    d.line([(128, 188), (128, 118)], w=6, a=1.2)                           # sprout stem
    d.line([(128, 130), (160, 100), (176, 112), (156, 138), (130, 140)], w=5, closed=True, a=1.2)
    d.line([(128, 116), (96, 88), (80, 100), (100, 126), (126, 128)], w=5, closed=True, a=1.2)

    svg = d.svg(256, 256)
    if variant:   # flipbook frame: nudged and tilted a little
        svg = svg.replace('</defs>', '</defs><g transform="translate(%.1f %.1f) rotate(%.1f 128 128)">' % (r.uniform(-4, 4), r.uniform(-4, 4), r.uniform(-3, 3)), 1).replace('</svg>', '</g></svg>')
    return svg
for v in range(3):
    name = 'Shield' if v == 0 else 'Shield_%d' % v
    open('svg/%s.svg' % name, 'w').write(shield(v)); JOBS.append(('svg/%s.svg' % name, '../../Assets/Resources/Icons/%s.png' % name, 256, 256, True))
s = D(809)
s.o.append('<path d="M60 52 L206 100 L160 214 Z" fill="#fff8e6" fill-opacity="0.6" stroke="#fff8e6" stroke-width="15" stroke-linejoin="round" stroke-opacity="0.9"/>')
s.line([(60, 52), (206, 100), (160, 214)], w=7, closed=True, a=2, step=30)
s.line([(86, 76), (176, 106)], w=3, a=1.2, op=.7)
open('svg/ShieldShard.svg', 'w').write(s.svg(256, 256))
JOBS.append(('svg/ShieldShard.svg', '../../Assets/Resources/Icons/ShieldShard.png', 256, 256, True))
json.dump(JOBS, open('jobs_shield.json', 'w'))
