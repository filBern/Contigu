#!/usr/bin/env python3
"""The line-clear preview frame (Icons/LineClearFrame.png, 128 px, 9-sliced, hollow): an ink line with a saturated orange line inside, hand-drawn, so the
frame reads on beige paper and on every tile color. Run from this folder:  python3 generate_lineclear.py && NODE_PATH=$(npm root -g) node render.js jobs_lineclear.json"""
import json
from ink import D
d = D(31)
d.box(4, 4, 120, 120, None, 7, 1.2, 18, 30)
d.o.append('<path d="%s" fill="none" stroke="#ff9f1c" stroke-width="6" stroke-linecap="round" stroke-linejoin="round"/>' % d.path([(15, 33), (15, 15), (33, 15), (95, 15), (113, 15), (113, 33), (113, 95), (113, 113), (95, 113), (33, 113), (15, 113), (15, 95)], True, 1.4, 30))
open('svg/LineClearFrame.svg', 'w').write(d.svg(128, 128))
json.dump([('svg/LineClearFrame.svg', '../../Assets/Resources/Icons/LineClearFrame.png', 128, 128, True)], open('jobs_lineclear.json', 'w'))
