#!/usr/bin/env python3
"""Fertilizer bags redrawn in the notebook style (instead of inkify's silhouette filter, which looked jagged): a hand-drawn bag shell (ink double stroke,
crayon fill by RARITY, crimped ends, category band) around the motif of Design/Icons/generate_icons.py (kept as clean vector, only recolored to ink and
very lightly wobbled). Writes svg/Bag_<ModifierId>.svg and renders Assets/Resources/Icons/Fertilizers/<ModifierId>.png (256 px).
Run from this folder:  python3 generate_bags.py && NODE_PATH=$(npm root -g) node render.js jobs_bags.json"""
import sys, os, json, math, re
sys.path.insert(0, '../Icons')
import generate_icons as G
from ink import D, INK
RES = '../../Assets/Resources/Icons/Fertilizers/'
os.makedirs('svg', exist_ok=True)
FILL = {'Common': '#f4ecd6', 'Uncommon': '#9ec3ee', 'Rare': '#f2a56a', 'Legendary': '#f7d26a'}
CATPASTEL = {'#8c63b0': '#c3a6e0', '#2fa4a8': '#8fd6d2', '#5a9e45': '#9fd37e', '#e8863a': '#f2b07a', '#c8403f': '#ec9a8c', '#4f7fd0': '#9ec3ee'}
def window(d, shape, cx, cy, r):
    if shape == 0: d.blob(cx, cy, r, r, 16, w=3, fill='#fffaf0', a=1)
    elif shape == 1: d.box(cx - r, cy - r, 2 * r, 2 * r, '#fffaf0', 3, 1, r * .35)
    elif shape == 2: d.line([(cx - r * .5, cy - r), (cx + r * .5, cy - r), (cx + r, cy), (cx + r * .5, cy + r), (cx - r * .5, cy + r), (cx - r, cy)], w=3, closed=True, fill='#fffaf0', a=1)
    else: d.line([(cx - r, cy - r * .9), (cx + r, cy - r * .9), (cx + r, cy + r * .1), (cx, cy + r * 1.05), (cx - r, cy + r * .1)], w=3, closed=True, fill='#fffaf0', a=1)
JOBS = []
# The four "Glow" fertilizers have no entry in generate_icons.F (their old sacks were drawn by hand): same bag, with the glow star motif of their color.
EXTRA = {'EclatCoral': G.glow('#c8403f'), 'EclatTeal': G.glow('#4f7fd0'), 'EclatViolet': G.glow('#e8a82e'), 'EclatLime': G.glow('#5a9e45')}
ITEMS = [(k, v[0], v[3], None) for k, v in G.F.items()] + [(k, None, 'Couleurs', fn) for k, fn in EXTRA.items()]
for k, m, cat, motif_fn in ITEMS:
    name, category, rarity = G.game_info(k)
    catcol = G.CAT_COLORS[G.sack_category(k, cat)]
    band = CATPASTEL.get(G.CAT[G.sack_category(k, cat)] if G.sack_category(k, cat) in G.CAT else '#8c63b0', '#c3a6e0')
    h = sum(ord(c) * (i + 7) for i, c in enumerate(k)); wshape, bstyle = (h // 4) % 4, (h // 16) % 3
    d = D(1000 + h % 997)
    body = [(52, 36), (204, 36), (210, 60), (216, 130), (210, 210), (204, 232), (52, 232), (46, 210), (40, 130), (46, 60)]
    d.line(body, w=4.5, closed=True, fill=FILL[rarity], a=2.2, step=22)
    # crimped ends: hatch ticks
    for y0 in (40, 218):
        for x in range(60, 200, 9): d.line([(x, y0), (x + 2, y0 + 12)], w=2, a=.6, op=.7)
    d.line([(52, 52), (204, 52)], w=2.4, a=1.2, op=.8); d.line([(52, 216), (204, 216)], w=2.4, a=1.2, op=.8)
    # category band
    if bstyle == 0: d.line([(50, 190), (206, 190), (208, 214), (48, 214)], w=2.6, closed=True, fill=band, a=1)
    elif bstyle == 1: d.line([(50, 58), (206, 58), (206, 70), (50, 70)], w=2.4, closed=True, fill=band, a=.8); d.line([(50, 194), (206, 194), (208, 214), (48, 214)], w=2.4, closed=True, fill=band, a=1)
    else: d.line([(46, 62), (66, 62), (66, 212), (48, 212)], w=2.4, closed=True, fill=band, a=1); d.line([(190, 62), (210, 62), (208, 212), (190, 212)], w=2.4, closed=True, fill=band, a=1)
    window(d, wshape, 128, 124, 58)
    motif = (motif_fn() if motif_fn else G.M[m]()).replace(G.O, INK)
    d.o.append('<g filter="url(#lw)"><g transform="translate(%g %g) scale(1.0)">%s</g></g>' % (128 - 50, 124 - 46, motif))
    svg = d.svg(256, 256).replace('<defs>', '<defs><filter id="lw" x="-5%" y="-5%" width="110%" height="110%"><feTurbulence type="fractalNoise" baseFrequency="0.04" numOctaves="1" seed="2"/><feDisplacementMap in="SourceGraphic" scale="3"/></filter>', 1)
    open('svg/Bag_%s.svg' % k, 'w').write(svg)
    png = RES + k + '.png'
    if os.path.exists(png): JOBS.append(('svg/Bag_%s.svg' % k, png, 256, 256, True))
json.dump(JOBS, open('jobs_bags.json', 'w')); print(len(JOBS), 'bags')
