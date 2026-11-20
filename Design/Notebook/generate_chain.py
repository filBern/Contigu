#!/usr/bin/env python3
"""The fertilizer chain arrows (Icons/ChainLine.png 128x16: a dashed hand-drawn ink line; Icons/ChainHead.png 32x32: a hand-drawn arrowhead pointing right),
drawn between consecutive fertilizers of the panel to show the order 1 -> 2 -> 3 -> 4. Run from this folder:
python3 generate_chain.py && NODE_PATH=$(npm root -g) node render.js jobs_chain.json"""
import json
from ink import D
d = D(61)
for k in range(5):
    x = 4 + k * 25
    d.line([(x, 8), (x + 16, 8)], w=4, a=.8, step=40)
open('svg/ChainLine.svg', 'w').write(d.svg(128, 16))
h = D(62)
h.line([(8, 5), (26, 16), (8, 27)], w=5, a=.7, step=40)
open('svg/ChainHead.svg', 'w').write(h.svg(32, 32))
json.dump([('svg/ChainLine.svg', '../../Assets/Resources/Icons/ChainLine.png', 128, 16, True), ('svg/ChainHead.svg', '../../Assets/Resources/Icons/ChainHead.png', 32, 32, True)], open('jobs_chain.json', 'w'))
