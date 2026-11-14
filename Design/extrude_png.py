#!/usr/bin/env python3
"""Faux-3D extrusion for icons that have no SVG source (the challenge banners): the PNG kept in Design/Icons/challenges is shrunk a little and a darkened copy of
its own silhouette is stacked a few pixels lower behind it, like extrude.py does for the SVG icons. Writes Assets/Resources/Icons/Challenges/<Name>.png (same size,
.meta untouched). Run from this folder."""
import glob, os
from PIL import Image

SCALE, DEPTH, LAYERS = 0.93, 0.055, 6
SRC, OUT = 'Icons/challenges', '../Assets/Resources/Icons/Challenges'

for path in sorted(glob.glob(SRC + '/*.png')):
    im = Image.open(path).convert('RGBA')
    w, h = im.size
    sw, sh = round(w * SCALE), round(h * SCALE)
    front = im.resize((sw, sh), Image.LANCZOS)
    r, g, b, a = front.split()
    dark = Image.merge('RGBA', (r.point(lambda v: int(v * 0.32)), g.point(lambda v: int(v * 0.30)), b.point(lambda v: int(v * 0.34)), a))
    x0, y0 = (w - sw) // 2, round(h * 0.015)
    step = h * DEPTH / LAYERS
    canvas = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    for k in range(LAYERS, 0, -1):
        canvas.alpha_composite(dark, (x0, y0 + round(k * step)))
    canvas.alpha_composite(front, (x0, y0))
    canvas.save('%s/%s' % (OUT, os.path.basename(path)))
    print('extruded', os.path.basename(path))
