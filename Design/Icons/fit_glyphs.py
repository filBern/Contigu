#!/usr/bin/env python3
"""Fits the shop glyphs (Assets/Resources/Icons/UpgradeGlyphs) to their picture: the SVGs leave a wide transparent margin around the motif (about 26% under it and 29%
beside it, varying from one glyph to the next), so the glyphs looked tiny and floated above the shelf. Each PNG is trimmed to its drawing and scaled to fill its 256 px
square (a small margin, the motif standing on the bottom edge), centered horizontally. Run from this folder after Design/extrude.py."""
import glob, os
from PIL import Image

RES = '../../Assets/Resources/Icons/UpgradeGlyphs'
SIDE, MARGIN_SIDE, MARGIN_TOP, MARGIN_BOTTOM = 256, 10, 10, 6

for path in sorted(glob.glob(RES + '/*.png')):
    im = Image.open(path).convert('RGBA')
    box = im.getchannel('A').getbbox()
    if not box:
        continue
    crop = im.crop(box)
    w, h = crop.size
    scale = min((SIDE - 2 * MARGIN_SIDE) / w, (SIDE - MARGIN_TOP - MARGIN_BOTTOM) / h)
    crop = crop.resize((max(1, round(w * scale)), max(1, round(h * scale))), Image.LANCZOS)
    canvas = Image.new('RGBA', (SIDE, SIDE), (0, 0, 0, 0))
    canvas.alpha_composite(crop, ((SIDE - crop.width) // 2, SIDE - MARGIN_BOTTOM - crop.height))
    canvas.save(path)
print('fitted', len(glob.glob(RES + '/*.png')), 'glyphs')
