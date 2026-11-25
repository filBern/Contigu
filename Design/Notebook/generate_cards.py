#!/usr/bin/env python3
"""Fertilizers as CARDS (replaces the bags of generate_bags.py): a tall hand-drawn card whose frame is the RARITY (cream / blue / orange / gold), a big
motif filling a cream panel (the picture is what you recognise, ~65% of the icon instead of ~40% in the bag's small window), and a ribbon in the
CATEGORY's color along the bottom. Sprouts stay bare glyphs, so a framed card is always a fertilizer. Writes Assets/Resources/Icons/Fertilizers/<ModifierId>.png (256 px).
Run from this folder:  python3 generate_cards.py && NODE_PATH=$(npm root -g) node render.js jobs_cards.json"""
import sys, os, json, math
sys.path.insert(0, '../Icons')
import generate_icons as G
from ink import D, INK
RES = '../../Assets/Resources/Icons/Fertilizers/'
os.makedirs('svg', exist_ok=True)
FRAME = {'Common': '#d9cdb0', 'Uncommon': '#8fb8ee', 'Rare': '#f2a56a', 'Legendary': '#f7cf4a'}
CATPASTEL = {'#8c63b0': '#b996dd', '#2fa4a8': '#6fcfcb', '#5a9e45': '#8fd16e', '#e8863a': '#f2a45c', '#c8403f': '#ea8579', '#4f7fd0': '#86b0ea'}
EXTRA = {'EclatCoral': G.glow('#c8403f'), 'EclatTeal': G.glow('#4f7fd0'), 'EclatViolet': G.glow('#e8a82e'), 'EclatLime': G.glow('#5a9e45')}
ITEMS = [(k, v[0], v[3], None) for k, v in G.F.items()] + [(k, None, 'Couleurs', fn) for k, fn in EXTRA.items()]
JOBS = []
for k, m, cat, motif_fn in ITEMS:
    name, category, rarity = G.game_info(k)
    cc = G.sack_category(k, cat)
    band = G.CAT_COLORS.get(cc, '#9a8fb5')   # the exact color of the category box in the tooltip (ModifierVisualDefaults.CategoryColors)
    h = sum(ord(c) * (i + 7) for i, c in enumerate(k))
    d = D(2000 + h % 991, scratch=True)
    # the card: rarity-colored frame, ink outline
    d.box(30, 10, 196, 236, FRAME[rarity], 7, 1.2, 26, 26)
    # the cream picture panel
    d.box(46, 26, 164, 160, '#fffaf0', 4, 0.8, 14, 26)
    # category ribbon along the bottom of the card
    d.box(46, 196, 164, 36, band, 4, 0.8, 12, 26)
    for x in (70, 128, 186): d.line([(x - 5, 214), (x + 5, 214)], w=3, a=.5, op=.7)      # three stitches: a label look, not text
    motif = (motif_fn() if motif_fn else G.M[m]()).replace(G.O, INK)
    s = 1.52
    d.o.append('<g filter="url(#lw)"><g transform="translate(%g %g) scale(%g)">%s</g></g>' % (128 - 50 * s, 106 - 46 * s, s, motif))
    if rarity == 'Legendary':
        for (cx, cy, r) in ((40, 22, 9), (216, 38, 7)):
            d.line([(cx, cy - r), (cx + r * .3, cy - r * .3), (cx + r, cy), (cx + r * .3, cy + r * .3), (cx, cy + r), (cx - r * .3, cy + r * .3), (cx - r, cy), (cx - r * .3, cy - r * .3)], w=2.5, closed=True, fill='#fff3b0', a=.4)
    svg = d.svg(256, 256).replace('<defs>', '<defs><filter id="lw" x="-5%" y="-5%" width="110%" height="110%"><feTurbulence type="fractalNoise" baseFrequency="0.04" numOctaves="1" seed="2"/><feDisplacementMap in="SourceGraphic" scale="4"/></filter>', 1)
    open('svg/Card_%s.svg' % k, 'w').write(svg)
    png = RES + k + '.png'
    if os.path.exists(png): JOBS.append(('svg/Card_%s.svg' % k, png, 256, 256, True))
json.dump(JOBS, open('jobs_cards.json', 'w')); print(len(JOBS), 'cards')
