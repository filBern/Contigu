#!/usr/bin/env python3
"""The poison marker (Icons/Traits/Poison.png, 128 px): a toothy green skull drawn by hand in the notebook style (crayon fill, thick ink outline).
inkify.py skips this file. Run from this folder:  python3 generate_poison.py && NODE_PATH=$(npm root -g) node render.js jobs_poison.json"""
import json
from ink import D
d = D(4242)
skull = [(64, 12), (92, 20), (106, 44), (100, 70), (88, 82), (88, 104), (40, 104), (40, 82), (28, 70), (22, 44), (36, 20)]
d.line(skull, w=9, closed=True, fill='#b8e04a', a=2.2, step=18)
d.blob(46, 52, 14, 15, 10, w=0, fill='#2a2420', a=1)
d.blob(82, 52, 14, 15, 10, w=0, fill='#2a2420', a=1)
d.o.append('<circle cx="46" cy="52" r="12" fill="#2a2420"/><circle cx="82" cy="52" r="12" fill="#2a2420"/>')
d.line([(64, 66), (56, 80), (72, 80)], w=5, closed=True, fill='#2a2420', a=.8)
for x in (52, 64, 76): d.line([(x, 90), (x, 104)], w=5, a=.8)      # teeth
d.blob(34, 36, 5, 4, 8, w=2.5, fill='#f4ffc2', a=.6)              # highlight
open('svg/Poison.svg', 'w').write(d.svg(128, 128))
json.dump([('svg/Poison.svg', '../../Assets/Resources/Icons/Traits/Poison.png', 128, 128, True)], open('jobs_poison.json', 'w'))
