#!/usr/bin/env python3
"""Complete button illustrations: chunky glossy body + the label lettered in the title's style (monoline, dark outline) + plants growing
around it. Drawn directly in game pixels (1 unit = 1 px of the button's on-screen size), so the art can replace the whole button.
Output: full/<Name>.svg. Needs generate_plants.py and generate_buttons_bodies.py (same folder)."""
import os, math
import generate_plants as P
from generate_buttons_bodies import PALETTE

O = '#13212e'
CREAM = '#fff7e8'
# monoline letters in a 0..adv x 0..100 box
GLYPHS = {
 'A': (62, 'M2 100 L31 4 L60 100 M13 68 H49'), 'B': (62, 'M8 4 V96 M8 4 H34 C62 4 62 50 34 50 H8 M34 50 C66 50 66 96 34 96 H8'),
 'C': (58, 'M54 22 C46 0 6 0 6 50 C6 100 46 100 54 78'), 'D': (62, 'M8 4 V96 H28 C66 96 66 4 28 4 H8'),
 'E': (54, 'M52 4 H8 V96 H52 M8 50 H42'), 'F': (52, 'M50 4 H8 V100 M8 50 H40'),
 'G': (62, 'M56 24 C46 0 6 0 6 50 C6 100 56 104 56 58 H34'), 'H': (62, 'M8 4 V96 M54 4 V96 M8 50 H54'),
 'I': (30, 'M15 4 V96'), 'K': (58, 'M8 4 V96 M54 4 L8 58 M24 42 L56 96'),
 'L': (54, 'M8 4 V96 H52'), 'M': (68, 'M8 96 V4 L34 64 L60 4 V96'), 'N': (62, 'M8 96 V4 L54 96 V4'),
 'O': (62, 'M31 4 C-2 4 -2 96 31 96 C64 96 64 4 31 4 Z'), 'P': (60, 'M8 96 V4 H32 C62 4 62 54 32 54 H8'),
 'R': (62, 'M8 96 V4 H32 C62 4 62 54 32 54 H8 M30 54 L56 96'), 'S': (58, 'M54 20 C44 -2 8 0 8 28 C8 56 52 46 52 74 C52 102 14 102 4 82'),
 'T': (60, 'M3 4 H57 M30 4 V96'), 'U': (62, 'M8 4 V70 C8 108 54 108 54 70 V4'),
 'W': (72, 'M3 4 L18 96 L36 38 L54 96 L69 4'), 'X': (60, 'M4 4 L56 96 M56 4 L4 96'),
 'Y': (60, 'M4 4 L30 50 L56 4 M30 50 V96'), ' ': (28, ''),
}
GAP = 14

def lettering(text, cap_h, x0, y0):
    k = cap_h / 100.0
    parts = []
    x = 0.0
    for ch in text.upper():
        adv, d = GLYPHS[ch]
        if d:
            parts.append('<path d="%s" transform="translate(%g %g) scale(%g)"/>' % (d, x, 0, 1))
        x += adv + GAP
    width = (x - GAP) * k
    inner = max(5.0, 0.15 * cap_h) / k
    outer = inner + max(6.0, 0.17 * cap_h) / k
    g = ''.join(parts)
    svg = ('<g transform="translate(%g %g) scale(%g)" fill="none" stroke-linejoin="round" stroke-linecap="round">'
           '<g stroke="%s" stroke-width="%g">%s</g><g stroke="%s" stroke-width="%g">%s</g></g>') % (x0, y0, k, O, outer, g, CREAM, inner, g)
    return svg, width

def plant(n, which, x, y, size, rot=0):
    body = P.V[n][0 if which == 'A' else 1]
    return '<g transform="translate(%g %g) rotate(%g) scale(%g) translate(-32 -62)"><g stroke-linejoin="round" stroke-linecap="round">%s</g></g>' % (x, y, rot, size / 64.0, body)

def button(label, color, variety, w=None, h=56, plant_cap=None, plants=True):
    cap_h = h * 0.46
    _, tw = lettering(label, cap_h, 0, 0)
    if w is not None and tw > w - h * 0.9:
        # Fixed-width buttons shrink their lettering to fit.
        cap_h *= (w - h * 0.9) / tw
        _, tw = lettering(label, cap_h, 0, 0)
    if w is None:
        w = tw + h * 1.3
    tall_size = plant_cap if plant_cap else min(88, h * 1.3)
    small_size = tall_size * 0.72 if plant_cap else min(66, h * 0.95)
    mt = tall_size * 0.95 + 6 if plant_cap else max(48, h * 1.0)
    if not plants:
        mt = 8
    mb, mx = 8, 20
    W, H = w + 2 * mx, h + mt + mb
    top, mid, bot = PALETTE[color] if color != 'Wood' else ('#d9a766', '#c58a4a', '#8f5b2b')
    s = ['<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 %g %g" width="%g" height="%g">' % (W, H, W, H),
         '<defs><linearGradient id="g" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="%s"/><stop offset="0.5" stop-color="%s"/><stop offset="1" stop-color="%s"/></linearGradient></defs>' % (top, mid, bot)]
    r = h / 2 if color != 'Wood' else min(14, h / 3)
    # plants behind the body's top edge
    base = mt
    if plants:
        s.append(plant(variety, 'A', mx + w * 0.17, base + 4, tall_size))
        s.append(plant(variety, 'B', mx + w * 0.82, base + 4, small_size))
    s.append('<rect x="%g" y="%g" width="%g" height="%g" rx="%g" fill="url(#g)" stroke="%s" stroke-width="%g"/>' % (mx + 2.5, mt + 2.5, w - 5, h - 5, r, O, 5))
    s.append('<path d="M%g %g H%g" stroke="#fff" stroke-width="%g" stroke-linecap="round" opacity="0.5"/>' % (mx + r + 4, mt + h * 0.2, mx + w - r - 4, max(3, h * 0.07)))
    if color == 'Wood':
        s.append('<circle cx="%g" cy="%g" r="3.2" fill="#5a3a1e" stroke="%s" stroke-width="2"/><circle cx="%g" cy="%g" r="3.2" fill="#5a3a1e" stroke="%s" stroke-width="2"/>' % (mx + 12, mt + h / 2, O, mx + w - 12, mt + h / 2, O))
    txt, tw2 = lettering(label, cap_h, mx + (w - tw) / 2, mt + (h - cap_h) / 2 + h * 0.03)
    s.append(txt)
    s.append('</svg>')
    return '\n'.join(s) + '\n', W, H, mt, mb

# name -> (label, body color, plant variety, fixed width or None)
SPEC = {
 'Play': ('Play', 'Gold', 1, 330), 'Settings': ('Settings', 'Blue', 2, 330), 'Exit': ('Exit', 'Red', 6, 330),
 'Confirm': ('Confirm', 'Green', 3, None), 'OK': ('OK', 'Green', 4, None), 'Gotit': ('Got it', 'Green', 5, None),
 'Close': ('Close', 'Green', 7, None), 'NewRun': ('New Run', 'Green', 8, None), 'Nextround': ('Next round', 'Green', 9, None),
 'Showseedbag': ('Show seed bag', 'Wood', 10, 180), 'Showrules': ('Show rules', 'Wood', 1, 180),
}
HEIGHT = {'Play': 84, 'Settings': 84, 'Exit': 84}
NO_PLANTS = {'Confirm', 'OK', 'Gotit', 'Close', 'NewRun', 'Nextround', 'Showseedbag', 'Showrules'}  # plants only on the main menu buttons (Play, Settings, Exit)
PLANT_CAP = {'Play': 52, 'Settings': 52, 'Exit': 52}  # menu buttons are stacked close together: keep their plants short

if __name__ == '__main__':
    os.makedirs('full', exist_ok=True)
    info = []
    for name, (label, color, variety, fixed_w) in SPEC.items():
        svg, W, H, mt, mb = button(label, color, variety, fixed_w, HEIGHT.get(name, 56), PLANT_CAP.get(name), name not in NO_PLANTS)
        open('full/%s.svg' % name, 'w').write(svg)
        info.append('%s %g %g %g %g %g' % (name, W, H, mt, mb, HEIGHT.get(name, 56)))
    open('full/sizes.txt', 'w').write('\n'.join(info) + '\n')
    print('\n'.join(info))
