#!/usr/bin/env python3
"""LASTBLOOM logo in the notebook style -> Assets/Resources/Title/LastBloom.png (1080x300, transparent): hand lettering (Patrick Hand, thickened by an ink
outline, crayon-colored letters, wobbled), sprouts on top, a hatched soil line under it. Run from this folder:  python3 generate_title.py && NODE_PATH=$(npm root -g) node render.js jobs_title.json"""
import math, json
from ink import D, INK
FONT = 'file:///home/user/Contigu/Assets/Resources/Font/PatrickHand-Regular.ttf'
COLORS = ['#e8584a', '#5b86e0', '#f1b92a', '#5aa845', '#e8584a', '#5b86e0', '#f1b92a', '#5aa845', '#e8584a']
d = D(77)
txt = ''
x = 58; adv = [round(v * .86) for v in (86, 112, 100, 100, 104, 94, 108, 108, 126)]; centers = []   # per-letter advance (px) tuned for the font at 230 px
for ch, col, a in zip('LASTBLOOM', COLORS, adv):
    txt += '<text x="%d" y="214" font-size="198" fill="%s" stroke="%s" stroke-width="14" stroke-linejoin="round" paint-order="stroke">%s</text>' % (x, col, INK, ch)
    centers.append(x + a / 2); x += a + 14
head = ('<style>@font-face{font-family:PH;src:url(%s)} text{font-family:PH,"Comic Sans MS",cursive}</style>'
        '<filter id="wob" x="-5%%" y="-10%%" width="110%%" height="120%%"><feTurbulence type="fractalNoise" baseFrequency="0.02" numOctaves="1" seed="5" result="n"/><feDisplacementMap in="SourceGraphic" in2="n" scale="7"/></filter>') % FONT
def sprout(cx, cy, s=1.0, ang=0):
    d.line([(cx, cy), (cx + 2 * s, cy - 22 * s)], w=3, a=.6)
    d.line([(cx + 2 * s, cy - 18 * s), (cx + 26 * s, cy - 34 * s), (cx + 34 * s, cy - 20 * s), (cx + 10 * s, cy - 12 * s)], w=2.4, closed=True, fill='#5bb04a', a=.8)
    d.line([(cx, cy - 12 * s), (cx - 24 * s, cy - 30 * s), (cx - 32 * s, cy - 16 * s), (cx - 8 * s, cy - 8 * s)], w=2.4, closed=True, fill='#3f8f3a', a=.8)
for i, s in ((0, 1.1), (2, 1.0), (4, 1.2), (6, 0.9), (8, 1.1)): sprout(centers[i], 70, s)
d.line([(40, 254), (320, 260), (600, 254), (1040, 258)], w=4, a=2.2)
d.hatch(70, 262, 1000, 284, 40)
svg = d.svg(1080, 300).replace('<defs>', '<defs>' + head, 1)
svg = svg.replace('</svg>', '')
svg = svg.replace('<defs>', '<defs>', 1)
out = svg + '<g filter="url(#wob)">' + txt + '</g></svg>'
open('svg/Title.svg', 'w').write(out)
json.dump([('svg/Title.svg', '../../Assets/Resources/Title/LastBloom.png', 1080, 300, True)], open('jobs_title.json', 'w'))
