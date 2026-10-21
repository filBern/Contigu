#!/usr/bin/env python3
"""Generates one SVG icon per fertilizer (ModifierId) and per upgrade (UpgradeId).

Recognizable at a glance by SILHOUETTE:
  * Fertilizer  = a burlap SACK tied with rope (wide, round) with a cream label; a colored pill in the
                  corner states the effect: red = Mult (x2, +3), blue = points (+12), amber = Pollen.
  * Upgrade     = a wooden garden SIGN on a stake (tall, pointed bottom); its band color is the upgrade's pool.
Output: fertilizers/<ModifierId>.svg, upgrades/<UpgradeId>.svg  (viewBox 256).
"""
import math, os

O = '#13212e'
CREAM = '#fff7e8'
R, B, Y, G, P = '#c8403f', '#4f7fd0', '#e8a82e', '#5a9e45', '#8c63b0'
BROWN, WOOD, SOIL = '#6b4528', '#c58a4a', '#3a2a1c'
GOLD = '#f1c40f'
GREY = '#8a8f98'
LEAF1, LEAF2 = '#6fb04f', '#4f9440'
SW = 3.5  # motif stroke width

def st(fill, sw=SW, extra=''):
    return 'fill="%s" stroke="%s" stroke-width="%g" stroke-linejoin="round" stroke-linecap="round" %s' % (fill, O, sw, extra)

def circ(cx, cy, r, fill, sw=SW): return '<circle cx="%g" cy="%g" r="%g" %s/>' % (cx, cy, r, st(fill, sw))
def rect(x, y, w, h, fill, rx=3, sw=SW, extra=''): return '<rect x="%g" y="%g" width="%g" height="%g" rx="%g" %s/>' % (x, y, w, h, rx, st(fill, sw, extra))
def ell(cx, cy, rx, ry, fill, rot=0, sw=SW): return '<ellipse cx="%g" cy="%g" rx="%g" ry="%g" transform="rotate(%g %g %g)" %s/>' % (cx, cy, rx, ry, rot, cx, cy, st(fill, sw))
def path(d, fill='none', sw=SW, extra=''): return '<path d="%s" %s/>' % (d, st(fill, sw, extra))
def line(x1, y1, x2, y2, col=O, w=SW): return '<path d="M%g %g L%g %g" fill="none" stroke="%s" stroke-width="%g" stroke-linecap="round"/>' % (x1, y1, x2, y2, col, w)
def poly(pts, fill, sw=SW): return '<path d="M%s Z" %s/>' % (' L'.join('%g %g' % p for p in pts), st(fill, sw))
def leaf(cx, cy, length, ang, fill=LEAF1):
    w = length * 0.42
    return '<path d="M0 0 C%g %g, %g %g, %g 0 C%g %g, %g %g, 0 0 Z" transform="translate(%g %g) rotate(%g)" %s/>' % (
        length * 0.2, -w, length * 0.7, -w, length, length * 0.7, w, length * 0.2, w, cx, cy, ang, st(fill, 3))
def star(cx, cy, r, fill='#fff1b0', inner=0.32, n=4, sw=SW):
    pts = []
    for i in range(n * 2):
        a = math.pi * i / n - math.pi / 2
        rr = r if i % 2 == 0 else r * inner
        pts.append((cx + rr * math.cos(a), cy + rr * math.sin(a)))
    return poly(pts, fill, sw)
def tile(x, y, s, col): return rect(x, y, s, s, col, rx=s * 0.18, sw=3)
def text(x, y, s, size, col=O, weight=900):
    return '<text x="%g" y="%g" font-family="Arial Black, Arial, Helvetica, sans-serif" font-weight="%d" font-size="%g" text-anchor="middle" fill="%s">%s</text>' % (x, y, weight, size, col, s)
def sprout(cx, cy, s=1.0):
    return line(cx, cy, cx, cy - 22 * s, O, 6 * s) + line(cx, cy, cx, cy - 22 * s, LEAF2, 3 * s) + leaf(cx, cy - 18 * s, 22 * s, -150, LEAF1) + leaf(cx, cy - 22 * s, 24 * s, -30, LEAF2)
def flower(cx, cy, r, col, n=5, center='#5a3a1e'):
    s = ''
    for i in range(n):
        a = 360.0 * i / n - 90
        s += ell(cx + r * 0.62 * math.cos(math.radians(a)), cy + r * 0.62 * math.sin(math.radians(a)), r * 0.42, r * 0.34, col, a, 3)
    return s + circ(cx, cy, r * 0.3, center, 3)
def heart(cx, cy, s, fill):
    return path('M%g %g C%g %g, %g %g, %g %g C%g %g, %g %g, %g %g Z' % (
        cx, cy + s * 0.9, cx - s * 1.4, cy + s * 0.1, cx - s * 1.1, cy - s * 0.9, cx, cy - s * 0.35,
        cx + s * 1.1, cy - s * 0.9, cx + s * 1.4, cy + s * 0.1, cx, cy + s * 0.9), fill)
def drop(cx, cy, s, fill):
    return path('M%g %g C%g %g, %g %g, %g %g C%g %g, %g %g, %g %g Z' % (cx, cy - s, cx + s * 0.9, cy - s * 0.1, cx + s * 0.7, cy + s * 0.9, cx, cy + s * 0.9, cx - s * 0.7, cy + s * 0.9, cx - s * 0.9, cy - s * 0.1, cx, cy - s), fill)
def arrow(x1, y1, x2, y2, col=O, w=5):
    a = math.atan2(y2 - y1, x2 - x1); h = 11
    p1 = (x2 - h * math.cos(a - 0.5), y2 - h * math.sin(a - 0.5)); p2 = (x2 - h * math.cos(a + 0.5), y2 - h * math.sin(a + 0.5))
    return line(x1, y1, x2, y2, col, w) + '<path d="M%g %g L%g %g L%g %g" fill="none" stroke="%s" stroke-width="%g" stroke-linecap="round" stroke-linejoin="round"/>' % (p1[0], p1[1], x2, y2, p2[0], p2[1], col, w)
def seed(cx, cy, s=1.0, fill='#d8b574'):
    return ell(cx, cy, 11 * s, 16 * s, fill, 25, 3) + line(cx - 4 * s, cy - 6 * s, cx + 2 * s, cy + 4 * s, '#a07a3a', 2)
def flask(cx, cy, fill, bubbles=True):
    s = path('M%g %g L%g %g L%g %g C%g %g, %g %g, %g %g L%g %g L%g %g Z' % (cx - 8, cy - 32, cx - 8, cy - 12, cx - 28, cy + 20, cx - 32, cy + 34, cx + 32, cy + 34, cx + 28, cy + 20, cx + 8, cy - 12, cx + 8, cy - 32), '#e6f4ff', 3.5)
    s += path('M%g %g C%g %g, %g %g, %g %g L%g %g L%g %g Z' % (cx - 22, cy + 10, cx - 28, cy + 24, cx - 30, cy + 32, cx - 26, cy + 32, cx + 26, cy + 32, cx + 22, cy + 10), fill, 3)
    s += rect(cx - 12, cy - 38, 24, 9, '#b8864a', 3, 3)
    if bubbles:
        s += circ(cx - 8, cy + 14, 4, '#ffffff', 2) + circ(cx + 10, cy + 20, 3, '#ffffff', 2) + circ(cx + 2, cy + 4, 2.5, '#ffffff', 2)
    return s
def fcell(x, y, s, col): return tile(x, y, s, col)
def pollen_grain(cx, cy, r):
    bumps = ''.join('<circle cx="%g" cy="%g" r="%g" %s/>' % (cx + r * 0.95 * math.cos(2 * math.pi * i / 9), cy + r * 0.95 * math.sin(2 * math.pi * i / 9), r * 0.22, st('#f6cf3a', 2.5)) for i in range(9))
    return bumps + '<circle cx="%g" cy="%g" r="%g" %s/>' % (cx, cy, r, st('#f6cf3a', 3)) + '<circle cx="%g" cy="%g" r="%g" fill="#fff1a8" opacity="0.8"/>' % (cx - r * 0.3, cy - r * 0.3, r * 0.3)
def num(cx, cy, s, size=44, col=O): return text(cx, cy, s, size, col)

# ---------------------------------------------------------------- motifs (drawn in a 100 x 90 box, center (50, 45))
M = {}
M['prism'] = lambda: poly([(22, 72), (46, 18), (70, 72)], '#dff3ff') + ''.join(line(60, 40 + i * 12, 94, 28 + i * 18, c, 5) for i, c in enumerate((R, Y, B)))
M['chain'] = lambda: ''.join(ell(28 + i * 22, 45, 17, 11, 'none', 0 if i % 2 == 0 else 0, 5.5) + ell(28 + i * 22, 45, 17, 11, 'none', 0, 2) for i in range(3)) + ''.join(ell(28 + i * 22, 45, 17, 11, 'none', 0, 5.5) for i in range(0))
M['megachain'] = lambda: ''.join(ell(20 + i * 20, 45 + (-12 if i % 2 else 12), 15, 9, 'none', 25 if i % 2 else -25, 5.5) for i in range(4)) + ''.join(ell(20 + i * 20, 45 + (-12 if i % 2 else 12), 15, 9, 'none', 25 if i % 2 else -25, 1.8).replace(O, GOLD) for i in range(4))
M['fortress'] = lambda: poly([(24, 78), (24, 36), (32, 36), (32, 26), (42, 26), (42, 36), (58, 36), (58, 26), (68, 26), (68, 36), (76, 36), (76, 78)], '#c9c2b4') + rect(42, 54, 16, 24, '#5a3f27', 8, 3) + line(32, 48, 38, 48) + line(62, 48, 68, 48)
M['prisoner'] = lambda: rect(24, 24, 52, 52, '#e9e2d0', 6) + ''.join(line(34 + i * 11, 26, 34 + i * 11, 74, O, 4) for i in range(4)) + sprout(50, 70, 0.5)
M['architect'] = lambda: tile(24, 20, 24, R) + tile(52, 20, 24, B) + tile(24, 48, 24, Y) + tile(52, 48, 24, G)
M['hoarder'] = lambda: path('M20 44 L80 44 L72 78 L28 78 Z', '#c58a4a') + path('M30 44 C30 20 70 20 70 44', 'none', 5) + tile(32, 54, 12, R) + tile(46, 56, 12, B) + tile(58, 54, 12, Y)
M['triad'] = lambda: flower(50, 46, 40, R, 3) .replace(R, R) if False else ''.join(ell(50 + 21 * math.cos(math.radians(a)), 46 + 21 * math.sin(math.radians(a)), 17, 13, c, a, 3) for a, c in ((-90, R), (30, B), (150, Y))) + circ(50, 46, 9, BROWN, 3)
M['companion'] = lambda: flower(30, 50, 28, R, 5) + flower(70, 42, 28, B, 5)
M['islet'] = lambda: circ(50, 46, 36, '#7fb6e0') + rect(38, 34, 24, 24, G, 5)
M['crown'] = lambda: poly([(20, 72), (20, 32), (36, 50), (50, 24), (64, 50), (80, 32), (80, 72)], GOLD) + circ(20, 30, 5, R, 3) + circ(50, 22, 5, B, 3) + circ(80, 30, 5, G, 3)
M['crossroads'] = lambda: path('M42 78 L42 54 L18 54 L18 38 L42 38 L42 14 L58 14 L58 38 L82 38 L82 54 L58 54 L58 78 Z', '#d8c9a0') + circ(50, 46, 6, R, 3)
M['mason'] = lambda: ''.join(rect(x, y, 30, 18, '#c46a4a', 3) for x, y in ((18, 22), (52, 22), (35, 42), (69 - 8, 42) if False else (2, 42))) + rect(18, 22, 30, 18, '#c46a4a', 3) + rect(52, 22, 30, 18, '#c46a4a', 3) + rect(34, 42, 30, 18, '#c46a4a', 3) + rect(18, 62, 30, 14, '#c46a4a', 3) + rect(52, 62, 30, 14, '#c46a4a', 3)
M['scythe'] = lambda: line(30, 80, 62, 22, '#8a5a2b', 7) + path('M60 22 C84 18 90 40 84 60 C80 40 70 32 58 34 Z', '#d4d8de', 3.5)
M['wheel'] = lambda: ''.join('<path d="M50 46 L%g %g A36 36 0 0 1 %g %g Z" %s/>' % (50 + 36 * math.cos(math.radians(a)), 46 + 36 * math.sin(math.radians(a)), 50 + 36 * math.cos(math.radians(a + 90)), 46 + 36 * math.sin(math.radians(a + 90)), st(c, 3)) for a, c in ((-90, R), (0, B), (90, Y), (180, G))) + circ(50, 46, 8, CREAM, 3)
M['ashbound'] = lambda: ell(50, 66, 34, 12, '#9a9a9a') + ell(40, 52, 20, 14, '#b0b0b0') + ell(60, 48, 16, 12, '#c2c2c2') + circ(48, 40, 5, '#ff8a3d', 2.5)
M['dissonant'] = lambda: ''.join(ell(50 + 22 * math.cos(math.radians(a)), 46 + 22 * math.sin(math.radians(a)), 16, 11, R if i < 3 else B, a, 3) for i, a in enumerate(range(-90, 270, 60))) + circ(50, 46, 8, BROWN, 3) + path('M40 28 L48 40 L42 44 L52 58', 'none', 3, '')
M['momentum'] = lambda: tile(14, 56, 16, G) + tile(36, 44, 22, G) + tile(64, 24, 28, G) + arrow(18, 34, 56, 18, R, 5)
M['cocoon'] = lambda: ell(50, 46, 20, 34, '#e8dcc0') + ''.join(line(32, 24 + i * 12, 68, 30 + i * 12, '#b8a888', 3) for i in range(5)) + line(50, 6, 50, 14, O, 3)
M['trowel'] = lambda: path('M30 56 L50 20 L70 56 C70 66 30 66 30 56 Z', '#cfd4da') + line(50, 62, 50, 82, '#8a5a2b', 9) + line(50, 28, 50, 56, '#a0a6ae', 3)
M['rainbow'] = lambda: ''.join('<path d="M%g 74 A%g %g 0 0 1 %g 74" fill="none" stroke="%s" stroke-width="9"/>' % (50 - r, r, r, 50 + r, c) for r, c in ((40, R), (31, Y), (22, G), (13, B)))
M['alternation'] = lambda: tile(24, 20, 24, R) + tile(52, 20, 24, B) + tile(24, 48, 24, B) + tile(52, 48, 24, R)
M['palindrome'] = lambda: ''.join(circ(14 + i * 18, 46, 8, c) for i, c in enumerate((R, B, Y, B, R))) + line(50, 20, 50, 72, O, 3)
M['gradient'] = lambda: ''.join(rect(14 + i * 19, 20, 17, 52, c, 3) for i, c in enumerate(('#bfe3a8', '#8ccb6a', '#5a9e45', '#2f6a2a')))
M['block'] = lambda: ''.join(tile(20 + (i % 3) * 22, 18 + (i // 3) * 22, 20, G) for i in range(9))
M['monoline'] = lambda: ''.join(tile(12 + i * 20, 34, 18, GREY) for i in range(4))
def devotion(col): return lambda: heart(50, 44, 28, col) + leaf(50, 76, 18, -70, LEAF1)
M['dev_R'] = devotion(R); M['dev_B'] = devotion(B); M['dev_Y'] = devotion(Y); M['dev_G'] = devotion(G)
def slot(n): return lambda: rect(22, 18, 56, 58, CREAM, 8, 4) + rect(30, 26, 40, 42, '#e0d6bf', 5, 3) + num(50, 62, n, 40)
M['slot1'] = slot('1'); M['slot2'] = slot('2'); M['slot3'] = slot('3')
M['overgrowth'] = lambda: sprout(50, 82, 1.9) + leaf(50, 60, 34, -20, LEAF1) + leaf(50, 52, 30, -160, LEAF2)
M['mutant'] = lambda: star(50, 46, 40, '#b9e04a', 0.5, 7) + circ(40, 40, 6, '#fff', 2.5) + circ(60, 40, 6, '#fff', 2.5) + circ(40, 40, 2.5, O, 1) + circ(60, 40, 2.5, O, 1)
def glow(col): return lambda: star(50, 46, 36, col, 0.38, 4) + star(22, 22, 12, '#fff1b0', 0.4, 4, 3) + star(80, 70, 10, '#fff1b0', 0.4, 4, 3)
M['glow_R'] = glow(R); M['glow_B'] = glow(B); M['glow_Y'] = glow(Y); M['glow_G'] = glow(G)
M['diagonal'] = lambda: tile(12, 14, 22, G) + tile(38, 34, 22, G) + tile(64, 54, 22, G) + line(10, 12, 92, 82, O, 2)
M['nest'] = lambda: ell(50, 62, 36, 14, '#a07a3a') + ell(50, 54, 32, 11, '#c8a45a') + ell(36, 48, 8, 11, '#f4efe0', 10, 3) + ell(52, 46, 8, 11, '#f4efe0', 0, 3) + ell(66, 49, 8, 11, '#f4efe0', -10, 3)
M['solitaire'] = lambda: poly([(50, 12), (78, 36), (50, 80), (22, 36)], '#7ad0e8') + line(22, 36, 78, 36, O, 3) + line(50, 12, 38, 36, O, 2.5) + line(50, 12, 62, 36, O, 2.5)
M['fallow'] = lambda: ''.join(path('M10 %d C34 %d 66 %d 90 %d' % (y, y - 8, y + 8, y), 'none', 6) + path('M10 %d C34 %d 66 %d 90 %d' % (y, y - 8, y + 8, y), 'none', 2.5).replace(O, '#8a6038') for y in (34, 52, 70)) + sprout(50, 56, 0.55)
M['burst'] = lambda: star(50, 46, 42, '#ffb03d', 0.5, 8) + star(50, 46, 22, '#fff1b0', 0.5, 8, 3)
M['seedling'] = lambda: sprout(50, 74, 1.4)
M['newgrowth'] = lambda: sprout(46, 78, 1.3) + circ(78, 20, 11, GOLD, 3) + ''.join(line(78 + 17 * math.cos(math.radians(a)), 20 + 17 * math.sin(math.radians(a)), 78 + 22 * math.cos(math.radians(a)), 20 + 22 * math.sin(math.radians(a)), GOLD, 3) for a in range(0, 360, 45))
M['bridge'] = lambda: path('M8 70 L8 48 C8 14 92 14 92 48 L92 70 Z', '#c9c2b4') + path('M30 70 C30 44 70 44 70 70', '#7fb6e0') + line(8, 48, 92, 48, O, 2.5)
M['ring'] = lambda: ''.join(tile(50 - 9 + 28 * math.cos(math.radians(a)), 46 - 9 + 28 * math.sin(math.radians(a)), 18, B) for a in range(0, 360, 45)) + tile(41, 37, 18, Y)
M['seal'] = lambda: ''.join(circ(50 + 30 * math.cos(math.radians(a)), 46 + 30 * math.sin(math.radians(a)), 9, '#a83232', 3) for a in range(0, 360, 40)) + circ(50, 46, 32, '#c8403f') + sprout(50, 62, 0.9)
M['colony'] = lambda: ''.join(circ(x, y, 14, G) for x, y in ((30, 34), (66, 32), (48, 54), (24, 62), (74, 62)))
M['repeat'] = lambda: '<path d="M78 46 A28 28 0 1 1 50 18" fill="none" stroke="%s" stroke-width="9" stroke-linecap="round"/><path d="M78 46 A28 28 0 1 1 50 18" fill="none" stroke="%s" stroke-width="4" stroke-linecap="round"/>' % (O, Y) + poly([(50, 4), (66, 18), (50, 32)], Y) + sprout(50, 62, 0.7)
M['swap'] = lambda: arrow(16, 34, 84, 34, O, 6) + arrow(84, 60, 16, 60, O, 6) + circ(28, 34, 0.1, O, 0)
M['combo'] = lambda: path('M50 10 C70 30 84 44 78 62 C74 76 60 82 50 82 C38 82 24 74 24 58 C24 44 34 36 38 22 C44 30 46 36 50 10 Z', '#ff8a3d') + path('M50 40 C60 50 64 58 60 68 C58 74 54 76 50 76 C44 76 40 72 40 64 C40 56 46 52 50 40 Z', '#ffd84d', 3)
M['target'] = lambda: circ(50, 46, 38, CREAM) + circ(50, 46, 26, R) + circ(50, 46, 14, CREAM) + circ(50, 46, 6, R) + arrow(88, 6, 54, 42, O, 4)
M['thicket'] = lambda: ''.join(circ(x, y, r, c) for x, y, r, c in ((30, 56, 22, '#3f7a35'), (68, 54, 24, '#3f7a35'), (50, 36, 26, G), (38, 46, 14, LEAF1), (64, 40, 12, LEAF1)))
M['lonebloom'] = lambda: flower(50, 34, 36, R, 6) + line(50, 56, 50, 84, O, 6) + line(50, 56, 50, 84, LEAF2, 3) + leaf(50, 74, 22, -160, LEAF1)
M['wildcard'] = lambda: star(50, 46, 40, P, 0.45, 5) + circ(50, 46, 10, CREAM, 3) + ''.join(circ(50 + 28 * math.cos(math.radians(a)), 46 + 28 * math.sin(math.radians(a)), 4, c, 2) for a, c in ((-90, R), (-18, B), (54, Y), (126, G), (198, '#fff')))
M['density'] = lambda: rect(8, 16, 84, 18, '#6b4528', 3) + ''.join(path('M%d 34 C%d 50 %d 56 %d 76' % (x, x + dx, x - dx, x + dx * 2), 'none', 4) for x, dx in ((26, 8), (50, -6), (74, 8))) + sprout(50, 18, 0.7)

# ---- weather / run-state fertilizers
M['barometer'] = lambda: circ(50, 48, 34, CREAM, 4) + ''.join(line(50 + 26 * math.cos(math.radians(a)), 48 + 26 * math.sin(math.radians(a)), 50 + 31 * math.cos(math.radians(a)), 48 + 31 * math.sin(math.radians(a)), O, 3) for a in range(-180, 1, 30)) + line(50, 48, 68, 28, R, 5) + circ(50, 48, 5, O, 2) + ell(50, 66, 14, 6, '#8fd0f0', 0, 3)
M['stormchaser'] = lambda: ell(40, 36, 22, 14, '#7d8aa3') + ell(62, 32, 20, 16, '#8f9cb5') + ell(52, 44, 30, 12, '#7d8aa3') + poly([(56, 40), (40, 62), (50, 62), (42, 84), (68, 54), (56, 54), (64, 40)], '#ffd23a')
M['umbrella'] = lambda: path('M10 52 C10 14 90 14 90 52 C80 44 70 44 62 52 C54 44 46 44 38 52 C30 44 20 44 10 52 Z', R) + line(50, 22, 50, 74, O, 5) + path('M50 74 C50 84 38 84 38 76', 'none', 5) + ''.join(drop(x, 80, 5, '#8fd0f0') for x in (16, 84))
M['thorns'] = lambda: path('M14 78 C30 50 40 60 52 40 C62 26 74 30 86 14', 'none', 8) + path('M14 78 C30 50 40 60 52 40 C62 26 74 30 86 14', 'none', 4).replace(O, LEAF2) + poly([(30, 62), (24, 50), (38, 56)], '#c9c2b4', 2.5) + poly([(48, 46), (50, 32), (58, 42)], '#c9c2b4', 2.5) + poly([(66, 32), (72, 20), (78, 32)], '#c9c2b4', 2.5) + circ(86, 16, 7, R, 3)
M['ladybug'] = lambda: ell(50, 52, 28, 26, R) + line(50, 28, 50, 78, O, 3) + circ(38, 46, 5, O, 1) + circ(62, 46, 5, O, 1) + circ(40, 62, 4, O, 1) + circ(60, 62, 4, O, 1) + circ(50, 24, 10, O, 3) + line(44, 16, 38, 6, O, 3) + line(56, 16, 62, 6, O, 3)
M['stopwatch'] = lambda: circ(50, 52, 32, CREAM, 4) + rect(44, 12, 12, 8, GREY, 2, 3) + line(50, 52, 50, 32, O, 5) + line(50, 52, 66, 60, R, 4) + circ(50, 52, 4, O, 2) + ''.join(line(2 + i * 4, 40 + i * 12, 18 + i * 2, 40 + i * 12, '#8fd0f0', 3) for i in range(3))
M['whirl'] = lambda: path('M50 46 C50 38 60 38 60 46 C60 58 40 60 38 46 C36 30 62 24 70 44 C76 62 54 74 36 66 C18 58 16 34 30 22', 'none', 7) + path('M50 46 C50 38 60 38 60 46 C60 58 40 60 38 46 C36 30 62 24 70 44 C76 62 54 74 36 66 C18 58 16 34 30 22', 'none', 3).replace(O, '#bfe9ff')
M['cornerstone'] = lambda: ''.join(tile(14 + (i % 3) * 25, 12 + (i // 3) * 25, 22, '#d8c9a0') for i in range(9) if i != 2) + tile(64, 12, 22, GOLD) + star(80, 20, 9, '#fff1b0', 0.4, 4, 2.5)
M['pollen'] = lambda: pollen_grain(50, 46, 26) + pollen_grain(22, 70, 12) + pollen_grain(80, 24, 13)
def bignum(s): return lambda: num(50, 64, s, 58) + star(80, 18, 10, '#fff1b0', 0.4, 4, 3)
M['n1'] = bignum('+1'); M['n2'] = bignum('+2'); M['n4'] = bignum('+4')
M['pots'] = lambda: path('M20 62 L80 62 L72 84 L28 84 Z', '#c8603f') + rect(18, 54, 64, 10, '#d9745a', 3) + path('M30 54 L70 54 L64 36 L36 36 Z', '#c8603f') + rect(28, 28, 44, 9, '#d9745a', 3) + sprout(50, 28, 0.7)
M['copy'] = lambda: rect(14, 22, 40, 50, '#d8b574', 8) + rect(44, 14, 40, 50, '#e8c98a', 8) + arrow(30, 82, 70, 82, O, 5)
M['brew'] = lambda: flask(50, 44, '#b469e0') + text(76, 32, '?', 26, O)
M['enchseed'] = lambda: seed(48, 50, 1.9) + star(76, 22, 12, '#fff1b0', 0.4, 4, 3) + star(22, 24, 8, '#fff1b0', 0.4, 4, 3)
M['hourglass'] = lambda: poly([(24, 12), (76, 12), (54, 46), (76, 80), (24, 80), (46, 46)], '#e6f4ff') + poly([(30, 76), (70, 76), (54, 60), (46, 60)], '#e8c070', 3) + poly([(40, 22), (60, 22), (50, 36)], '#e8c070', 3)
M['multitude'] = lambda: ''.join(seed(x, y, 1.0, c) for x, y, c in ((26, 40, '#d8b574'), (50, 34, '#e8c98a'), (74, 40, '#d8b574'), (38, 62, '#e8c98a'), (62, 62, '#d8b574')))
M['rings'] = lambda: ell(50, 50, 38, 30, '#d8a868') + ell(50, 50, 28, 21, 'none', 0, 3) + ell(50, 50, 18, 13, 'none', 0, 3) + ell(50, 50, 8, 6, 'none', 0, 3) + ell(50, 74, 38, 10, '#b88848')
def shapes(cells, col):
    return lambda: ''.join(tile(14 + x * 24 if False else 50 - len(set(c[0] for c in cells)) * 12 + x * 24, 46 - len(set(c[1] for c in cells)) * 12 + y * 24, 22, col) for x, y in cells)
M['sh2'] = shapes([(0, 0), (1, 0)], R); M['sh3'] = shapes([(0, 0), (0, 1), (1, 1)], B); M['sh4'] = shapes([(0, 0), (1, 0), (0, 1), (1, 1)], G)
def withspark(fn): return lambda: fn() + star(84, 14, 11, '#fff1b0', 0.4, 4, 3) + star(14, 78, 8, '#fff1b0', 0.4, 4, 3)
M['sh2g'] = withspark(M['sh2']); M['sh3g'] = withspark(M['sh3']); M['sh4g'] = withspark(M['sh4'])
M['waxing'] = lambda: circ(50, 46, 34, '#2c3a52') + '<path d="M50 12 A34 34 0 0 1 50 80 A18 34 0 0 0 50 12 Z" %s/>' % st('#fff1b0', 3)
M['waning'] = lambda: circ(50, 46, 34, '#2c3a52') + '<path d="M50 12 A34 34 0 0 0 50 80 A18 34 0 0 1 50 12 Z" %s/>' % st('#fff1b0', 3)
M['cactus'] = lambda: rect(40, 18, 20, 58, G, 10) + rect(18, 34, 16, 28, G, 8) + rect(24, 50, 22, 12, G, 5) + rect(66, 28, 16, 26, G, 8) + rect(56, 44, 22, 12, G, 5) + path('M26 80 L74 80 L68 90 L32 90 Z', '#c8603f') + circ(50, 14, 6, '#ff8aa8', 2.5)
M['tonic'] = lambda: flask(50, 46, '#e8a82e') + star(50, 62, 10, '#fff1b0', 0.4, 4, 2.5)
M['quiver'] = lambda: poly([(36, 30), (64, 30), (68, 84), (32, 84)], '#8a5a2b') + ''.join(line(x, 28, x + dx, 6, O, 3.5) + poly([(x + dx - 5, 12), (x + dx, 2), (x + dx + 5, 12)], '#cfd4da', 2.5) for x, dx in ((42, -6), (50, 0), (58, 6)))
M['hoardjar'] = lambda: rect(26, 22, 48, 56, '#e6f4ff', 10) + rect(32, 12, 36, 12, '#b8864a', 4) + ''.join(tile(32 + (i % 2) * 20, 34 + (i // 2) * 20, 16, c) for i, c in enumerate((R, B, Y, G)))
M['metronome'] = lambda: poly([(30, 84), (40, 14), (60, 14), (70, 84)], '#8a5a2b') + line(50, 70, 64, 24, O, 4) + circ(60, 34, 6, GOLD, 3)
M['echo'] = lambda: sprout(30, 64, 1.0) + ''.join('<path d="M%g %g A%g %g 0 0 1 %g %g" fill="none" stroke="%s" stroke-width="5" stroke-linecap="round"/>' % (52 + i * 10, 46 - 10 - i * 8, 14 + i * 8, 14 + i * 8, 52 + i * 10, 46 + 10 + i * 8, O) for i in range(3))
M['siphon'] = lambda: drop(30, 70, 20, '#7ad0e8') + path('M36 56 C44 30 56 22 74 22', 'none', 7) + path('M36 56 C44 30 56 22 74 22', 'none', 3).replace(O, '#e8c070') + pollen_grain(78, 22, 10)
M['slot4'] = lambda: rect(22, 18, 56, 58, CREAM, 8, 4) + rect(30, 26, 40, 42, '#e0d6bf', 5, 3) + num(50, 62, '4', 40) + star(80, 20, 11, '#fff1b0', 0.4, 4, 3)


# ---- batch of 40 fertilizers
M['n_hedge'] = lambda: rect(8, 62, 84, 14, '#8a5a2b', 3) + ''.join(circ(18 + i * 21, 52, 13, LEAF1) + circ(24 + i * 21, 46, 6, LEAF2, 2.5) for i in range(4))
M['n_heart'] = lambda: circ(50, 46, 36, '#c58a4a') + circ(50, 46, 26, '#d8a566', 3) + circ(50, 46, 16, '#c58a4a', 3) + circ(50, 46, 6, '#8a5a2b', 2.5)
M['n_trellis'] = lambda: rect(30, 8, 40, 74, '#d8b574', 4) + ''.join(line(36 + i * 14, 10, 36 + i * 14, 80, '#a07a3a', 3) for i in range(3)) + ''.join(line(32, 22 + i * 18, 68, 22 + i * 18, '#a07a3a', 3) for i in range(3)) + path('M40 80 C28 60 60 56 46 36 C36 22 56 16 50 8', 'none', 5) + path('M40 80 C28 60 60 56 46 36 C36 22 56 16 50 8', 'none', 2.5).replace(O, LEAF1)
M['n_furrow'] = lambda: ''.join(rect(8, 20 + i * 22, 84, 12, SOIL, 5, 3) + ''.join(sprout(22 + j * 24, 22 + i * 22, 0.45) for j in range(3)) for i in range(3))
M['n_bean'] = lambda: path('M46 82 C20 62 72 52 42 34 C26 22 56 16 50 6', 'none', 8) + path('M46 82 C20 62 72 52 42 34 C26 22 56 16 50 6', 'none', 4).replace(O, LEAF2) + leaf(54, 56, 26, -20, LEAF1) + leaf(40, 38, 24, 200, LEAF1) + ell(66, 70, 6, 12, LEAF1, 20, 2.5) + ell(30, 20, 5, 10, LEAF1, -25, 2.5)
M['n_patch'] = lambda: tile(14, 14, 36, R) + tile(50, 14, 36, B) + tile(14, 50, 36, Y) + tile(50, 50, 36, G) + path('M50 16 V84 M14 50 H86', 'none', 2.5, 'stroke-dasharray="5 4"').replace(O, CREAM)
M['n_root'] = lambda: poly([(32, 28), (68, 28), (52, 84)], '#e8863a') + ''.join(line(38 + i * 6, 38 + i * 10, 56 - i * 4, 38 + i * 10, '#b8621d', 2.5) for i in range(4)) + leaf(46, 28, 28, -110, LEAF1) + leaf(54, 28, 28, -70, LEAF2)
M['n_snug'] = lambda: ''.join(tile(x, y, 22, '#d8c9a0') for x, y in ((14, 34), (64, 34), (39, 9), (39, 59))) + tile(39, 34, 22, GOLD)
M['n_mosaic'] = lambda: ''.join(tile(14 + (i % 3) * 25, 12 + (i // 3) * 25, 22, (R, B, Y, G, P)[(i * 2 + i // 3) % 5]) for i in range(9))
M['n_crowd'] = lambda: ''.join(tile(14 + (i % 4) * 19, 10 + (i // 4) * 19, 17, (R, B, Y, G)[(i + i // 4) % 4]) for i in range(16))
M['n_meadow'] = lambda: ''.join(line(18 + i * 21, 80, 18 + i * 21, 52, LEAF2, 4) + flower(18 + i * 21, 42, 26, c, 5) for i, c in enumerate((R, B, Y, G)))
M['n_wreath'] = lambda: ''.join(leaf(50 + 30 * math.cos(math.radians(a)), 46 + 30 * math.sin(math.radians(a)), 24, a + 90, LEAF1 if i % 2 else LEAF2) for i, a in enumerate(range(0, 360, 40))) + circ(50, 16, 6, R, 3) + circ(34, 22, 5, Y, 3)
M['n_bee'] = lambda: ell(36, 30, 16, 12, '#dff3ff', -30, 3) + ell(62, 28, 16, 12, '#dff3ff', 30, 3) + ell(50, 54, 24, 18, '#f6cf3a') + ''.join(line(40 + i * 10, 38, 40 + i * 10, 70, O, 5) for i in range(3)) + circ(26, 54, 9, '#f6cf3a', 3) + line(70, 66, 82, 72, O, 4)
M['n_honey'] = lambda: path('M26 34 C20 56 24 80 50 82 C76 80 80 56 74 34 Z', '#e8a82e') + rect(24, 24, 52, 12, '#c58a4a', 4) + path('M36 46 C44 42 58 42 64 46', 'none', 3).replace(O, '#fff1b0') + drop(50, 68, 10, '#f6cf3a')
M['n_nectar'] = lambda: flower(50, 36, 38, R, 5) + drop(50, 74, 11, '#f6cf3a') + drop(30, 70, 6, '#f6cf3a') + drop(70, 70, 6, '#f6cf3a')
M['n_cross'] = lambda: ''.join(tile(x, y, 22, G) for x, y in ((39, 8), (39, 34), (39, 60), (14, 34), (64, 34))) + star(76, 16, 9, '#fff1b0', 0.4, 4, 2.5)
M['n_sickle'] = lambda: path('M26 78 C10 40 40 12 78 24 C54 28 38 48 38 74 Z', '#d4d8de', 4) + line(30, 76, 22, 86, '#8a5a2b', 8) + ''.join(line(70 + i * 7, 84, 70 + i * 7, 50 - i * 4, '#e8c050', 4) + ell(70 + i * 7, 46 - i * 4, 4, 8, '#e8c050', 0, 2.5) for i in range(3))
M['n_clean'] = lambda: rect(12, 24, 76, 52, CREAM, 8, 4) + rect(20, 32, 60, 36, '#e0d6bf', 5, 3) + star(34, 44, 10, '#fff1b0', 0.4, 4, 2.5) + star(66, 56, 12, '#fff1b0', 0.4, 4, 2.5) + star(60, 36, 7, '#fff1b0', 0.4, 4, 2)
M['n_tithe'] = lambda: ''.join(line(32 + i * 9, 78, 24 + i * 13, 22, '#e8c050', 4) + ell(24 + i * 13, 20, 4, 9, '#e8c050', 0, 2.5) for i in range(5)) + rect(28, 52, 44, 9, '#8a5a2b', 4) + pollen_grain(78, 30, 9)
M['n_rowcrop'] = lambda: rect(6, 56, 88, 18, SOIL, 5, 3) + ''.join(sprout(18 + i * 21, 58, 0.9) for i in range(4)) + arrow(14, 24, 84, 24, R, 5)
M['n_colcrop'] = lambda: rect(36, 4, 28, 82, SOIL, 5, 3) + ''.join(sprout(50, 22 + i * 24, 0.7) for i in range(3)) + arrow(80, 12, 80, 78, R, 5)
M['n_bounty'] = lambda: ''.join(ell(50, 70 - i * 12, 28, 9, GOLD, 0, 3) for i in range(3)) + circ(50, 28, 15, GOLD, 3.5) + num(50, 36, '$', 22, '#8a5a00')
M['n_icebreak'] = lambda: poly([(18, 74), (14, 34), (40, 14), (80, 22), (88, 62), (62, 82)], '#bfe6ff') + path('M44 18 L52 40 L40 52 L56 66 L52 80', 'none', 3.5) + poly([(60, 20), (78, 6), (84, 18)], '#fff', 2.5)
M['n_season'] = lambda: ''.join('<path d="M50 46 L%g %g A36 36 0 0 1 %g %g Z" %s/>' % (50 + 36 * math.cos(math.radians(a)), 46 + 36 * math.sin(math.radians(a)), 50 + 36 * math.cos(math.radians(a + 90)), 46 + 36 * math.sin(math.radians(a + 90)), st(c, 3)) for a, c in ((-90, '#8fd05a'), (0, '#ffd23a'), (90, '#e8863a'), (180, '#bfe6ff')))
M['n_under'] = lambda: sprout(50, 78, 1.5) + arrow(24, 60, 24, 22, R, 5) + arrow(76, 60, 76, 22, R, 5)
M['n_finish'] = lambda: circ(50, 46, 34, CREAM, 3.5) + circ(50, 46, 22, R, 3) + circ(50, 46, 10, CREAM, 3) + poly([(58, 6), (40, 40), (52, 40), (44, 80), (72, 36), (58, 36), (68, 6)], '#ffd23a')
M['n_last'] = lambda: seed(50, 46, 2.2) + star(80, 18, 11, '#fff1b0', 0.4, 4, 3) + num(22, 80, '1', 30)
M['n_first'] = lambda: seed(44, 52, 2.0) + line(70, 12, 70, 78, '#8a5a2b', 5) + poly([(70, 12), (92, 22), (70, 34)], R, 3)
M['n_pig'] = lambda: ell(50, 52, 34, 26, '#f4a6b8') + circ(24, 52, 12, '#f08aa4', 3) + circ(21, 52, 2.5, O, 1) + circ(21, 48, 2.5, O, 1) + poly([(60, 28), (68, 14), (74, 32)], '#f08aa4', 3) + circ(60, 44, 3, O, 1) + rect(42, 30, 22, 5, O, 2, 1) + line(34, 76, 34, 86, O, 6) + line(66, 76, 66, 86, O, 6)
M['n_bank'] = lambda: poly([(10, 34), (50, 8), (90, 34)], '#d8c9a0') + ''.join(rect(18 + i * 22, 38, 12, 34, CREAM, 2, 3) for i in range(4)) + rect(8, 72, 84, 10, '#c9c2b4', 3) + seed(50, 24, 0.7)
M['n_brush'] = lambda: ell(50, 56, 38, 26, LEAF2) + ell(34, 42, 22, 18, LEAF1) + ell(66, 44, 22, 18, LEAF1) + circ(40, 50, 5, '#ffe58a', 2) + circ(60, 50, 5, '#ffe58a', 2) + circ(40, 50, 2, O, 1) + circ(60, 50, 2, O, 1)
M['n_raindance'] = lambda: ell(36, 30, 20, 13, '#dfe7f2') + ell(60, 26, 22, 15, '#eef3fa') + ell(48, 38, 32, 12, '#dfe7f2') + ''.join(drop(26 + i * 16, 66 + (i % 2) * 6, 7, '#6cc7f0') for i in range(4))
M['n_sundance'] = lambda: ''.join(line(50 + 26 * math.cos(math.radians(a)), 46 + 26 * math.sin(math.radians(a)), 50 + 40 * math.cos(math.radians(a)), 46 + 40 * math.sin(math.radians(a)), '#ffc83a', 5) for a in range(0, 360, 45)) + circ(50, 46, 20, '#ffc83a', 3.5) + circ(43, 39, 6, '#ffe58a', 1)
M['n_frosthardy'] = lambda: ''.join(line(50, 46, 50 + 34 * math.cos(math.radians(a)), 46 + 34 * math.sin(math.radians(a)), '#7fb4e8', 5) for a in range(0, 360, 60)) + circ(50, 46, 8, '#e8f4ff', 3) + sprout(50, 80, 0.6)
M['n_wind'] = lambda: path('M10 36 C40 36 60 22 76 30 C92 40 80 56 68 48', 'none', 7) + path('M10 58 C40 58 66 52 78 62 C88 72 74 82 66 76', 'none', 7) + leaf(60, 44, 22, -20, LEAF1)
M['n_bird'] = lambda: path('M8 70 A42 42 0 0 1 92 70 Z', '#ffc83a', 3.5) + ell(52, 38, 18, 12, '#5b8fd0', -10) + circ(68, 30, 8, '#5b8fd0', 3) + poly([(75, 28), (86, 31), (75, 34)], '#f6a02a', 2.5) + circ(70, 28, 1.8, O, 1) + poly([(36, 40), (20, 30), (30, 46)], '#4a78b8', 3)
M['n_flag'] = lambda: line(24, 10, 24, 84, '#8a5a2b', 6) + rect(24, 12, 54, 38, CREAM, 2, 3) + ''.join(rect(24 + (i % 3) * 18, 12 + (i // 3) * 19, 18, 19, O if (i + i // 3) % 2 == 0 else CREAM, 0, 2) for i in range(6))
M['n_bossbane'] = lambda: poly([(20, 52), (20, 18), (36, 34), (50, 10), (64, 34), (80, 18), (80, 52)], GOLD) + rect(20, 52, 60, 10, '#c9a227', 3, 3) + line(14, 80, 86, 38, R, 8) + line(86, 80, 14, 38, R, 8)
M['n_barrel'] = lambda: path('M22 28 C14 46 14 64 22 82 L78 82 C86 64 86 46 78 28 Z', '#a8723a') + ''.join(path('M%d %d C%d %d %d %d %d %d' % (x0, 28, x0 - 4, 50, x0 - 4, 62, x0, 82), 'none', 2.5) for x0 in (38, 50, 62)) + rect(16, 40, 68, 6, '#6b4528', 2, 2.5) + rect(16, 66, 68, 6, '#6b4528', 2, 2.5) + ell(50, 28, 28, 8, '#7fb6e0', 0, 3) + drop(50, 12, 7, '#6cc7f0')
M['n_granary'] = lambda: poly([(18, 82), (18, 34), (50, 12), (82, 34), (82, 82)], '#c46a4a') + rect(38, 52, 24, 30, '#8a5a2b', 3, 3) + line(50, 52, 50, 82, O, 2.5) + ''.join(line(34 + i * 16, 36, 28 + i * 22, 26, '#e8c050', 3) for i in range(2)) + pollen_grain(50, 34, 8)

# ---------------------------------------------------------------- fertilizer table: ModifierId -> (motif, tag kind, tag text, category)
CAT = {'Couleurs': '#8c63b0', 'Connexions': '#2fa4a8', 'Voisinage': '#5a9e45', 'Roguelike': '#e8863a', 'Destruction': '#c8403f', 'Formes': '#4f7fd0'}
F = {
 'Prisme': ('prism', 'm', '×3', 'Couleurs'), 'Chaine': ('chain', 'p', '+15', 'Connexions'), 'MegaChaine': ('megachain', 'p', '+45', 'Connexions'),
 'Forteresse': ('fortress', 'p', '+9', 'Voisinage'), 'Prisonnier': ('prisoner', 'p', '+6', 'Voisinage'), 'Architecte': ('architect', 'm', '×2', 'Roguelike'),
 'Collectionneur': ('hoarder', 'p', '+12', 'Roguelike'), 'Tricolore': ('triad', 'm', '×2', 'Couleurs'), 'Complementaire': ('companion', 'm', '×2', 'Couleurs'),
 'Ilot': ('islet', 'm', '×2', 'Voisinage'), 'Couronne': ('crown', 'p', '+8', 'Voisinage'), 'Carrefour': ('crossroads', 'p', '+18', 'Voisinage'),
 'Macon': ('mason', 'm', '+2', 'Destruction'), 'Demolisseur': ('scythe', 'm', '×2', 'Destruction'), 'CercleChromatique': ('wheel', 'p', '+52', 'Voisinage'),
 'Monochrome': ('ashbound', 'p', '+5', 'Roguelike'), 'Contraste': ('dissonant', 'p', '+9', 'Couleurs'), 'Degrade': ('momentum', 'm', '×2', 'Roguelike'),
 'Emmitouflee': ('cocoon', 'p', '+12', 'Voisinage'), 'Jardinier': ('trowel', 'p', '+9', 'Roguelike'), 'ArcEnCiel': ('rainbow', 'm', '×2', 'Couleurs'),
 'Alternance': ('alternation', 'm', '×2', 'Couleurs'), 'Palindrome': ('palindrome', 'm', '×2', 'Connexions'), 'Gradient': ('gradient', 'm', '+1', 'Connexions'),
 'Bloc': ('block', 'm', '×2', 'Connexions'), 'MonochromeLigne': ('monoline', 'm', '×2', 'Couleurs'),
 'DevotionCoral': ('dev_R', 'm', '+3', 'Couleurs'), 'DevotionTeal': ('dev_B', 'm', '+3', 'Couleurs'), 'DevotionViolet': ('dev_Y', 'm', '+3', 'Couleurs'), 'DevotionLime': ('dev_G', 'm', '+3', 'Couleurs'),
 'SlotUn': ('slot1', 'm', '+3', 'Roguelike'), 'SlotDeux': ('slot2', 'm', '+3', 'Roguelike'), 'SlotTrois': ('slot3', 'm', '+3', 'Roguelike'),
 'GrandFormat': ('overgrowth', 'p', '+12', 'Roguelike'), 'HorsNorme': ('mutant', 'p', '+18', 'Roguelike'),
 'EclatCoral': ('glow_R', 'p', '+6', 'Couleurs'), 'EclatTeal': ('glow_B', 'p', '+6', 'Couleurs'), 'EclatViolet': ('glow_Y', 'p', '+6', 'Couleurs'), 'EclatLime': ('glow_G', 'p', '+6', 'Couleurs'),
 'Diagonale': ('diagonal', 'p', '+8', 'Voisinage'), 'Nid': ('nest', 'p', '+5', 'Voisinage'), 'Solitaire': ('solitaire', 'm', '×2', 'Connexions'),
 'EspaceLibre': ('fallow', 'm', '×2', 'Roguelike'), 'Rafale': ('burst', 'm', '×3', 'Destruction'), 'PetitFormat': ('seedling', 'p', '+8', 'Roguelike'),
 'Fraicheur': ('newgrowth', 'm', '×2', 'Couleurs'), 'Pont': ('bridge', 'm', '×2', 'Connexions'), 'Encerclement': ('ring', 'p', '+9', 'Voisinage'),
 'Boucher': ('seal', 'p', '+15', 'Voisinage'), 'GrosseFamille': ('colony', 'm', '×2', 'Couleurs'), 'Repetition': ('repeat', 'm', '×n', 'Roguelike'),
 'AlternancePieces': ('swap', 'm', '+2', 'Couleurs'), 'Combo': ('combo', 'm', '×2', 'Destruction'), 'Precision': ('target', 'p', '+8', 'Voisinage'),
 'Surpopulation': ('thicket', 'p', '+12', 'Voisinage'), 'Minimaliste': ('lonebloom', 'm', '×2', 'Voisinage'), 'Joker': ('wildcard', None, None, 'Roguelike'),
 'Densite': ('density', 'm', '+n', 'Roguelike'),
 'ArcEnCielLueur': ('rainbow', 'l', '+6', 'Couleurs'), 'AlternanceLueur': ('alternation', 'l', '+4', 'Couleurs'), 'MonochromeLigneLueur': ('monoline', 'l', '+4', 'Couleurs'),
 'CollectionneurLueur': ('hoarder', 'l', '+2', 'Roguelike'), 'RepetitionLueur': ('repeat', 'l', '+5', 'Roguelike'),
 'MultUn': ('n1', None, None, 'Roguelike'), 'MultDeux': ('n2', None, None, 'Roguelike'), 'MultQuatre': ('n4', None, None, 'Roguelike'),
 'Solidarite': ('pots', 'm', '+1', 'Roguelike'), 'Copieur': ('copy', None, None, 'Roguelike'), 'MultCinqRisque': ('brew', 'm', '+5', 'Roguelike'),
 'CartesEnchantees': ('enchseed', 'm', '+0.1', 'Roguelike'), 'Epuisement': ('hourglass', 'p', '+150', 'Roguelike'), 'Multitude': ('multitude', 'p', '+2', 'Roguelike'),
 'Experience': ('rings', 'm', '+0.1', 'Roguelike'),
 'FormatPetitSpecialiste': ('sh2', 'm', '×2', 'Formes'), 'FormatMoyenSpecialiste': ('sh3', 'm', '×2', 'Formes'), 'FormatGrandSpecialiste': ('sh4', 'm', '×2', 'Formes'),
 'FormatPetitGlow': ('sh2g', 'p', '+6', 'Formes'), 'FormatMoyenGlow': ('sh3g', 'p', '+6', 'Formes'), 'FormatGrandGlow': ('sh4g', 'p', '+6', 'Formes'),
 'Pair': ('waxing', 'm', '×2', 'Roguelike'), 'Impair': ('waning', 'm', '×2', 'Roguelike'), 'Polyvalence': ('cactus', 'm', '+1', 'Roguelike'),
 'RenfortJoker': ('tonic', 'd', '+25%', 'Roguelike'), 'Arsenal': ('quiver', 'm', '+1', 'Roguelike'), 'CollectionChromatique': ('hoardjar', 'm', '+2', 'Couleurs'),
 'Barometre': ('barometer', 'm', '+3', 'Roguelike'), 'ChasseurTempete': ('stormchaser', 'm', '×2', 'Roguelike'), 'Impermeable': ('umbrella', None, None, 'Roguelike'),
 'Epines': ('thorns', 'm', '+4', 'Roguelike'), 'Exterminateur': ('ladybug', 'm', '+1', 'Roguelike'), 'Hate': ('stopwatch', 'm', '+0.1', 'Roguelike'),
 'Tourbillon': ('whirl', 'm', '+0.25', 'Roguelike'), 'PierreAngulaire': ('cornerstone', 'm', '×2', 'Voisinage'),
 'Haie': ('n_hedge', 'p', '+4', 'Voisinage'),
 'Coeur': ('n_heart', 'm', '×2', 'Voisinage'),
 'Treillis': ('n_trellis', 'm', '+3', 'Formes'),
 'Sillon': ('n_furrow', 'm', '+3', 'Formes'),
 'Haricot': ('n_bean', 'p', '+12', 'Formes'),
 'Patchwork': ('n_patch', 'p', '+4', 'Couleurs'),
 'Racines': ('n_root', 'm', '+1', 'Connexions'),
 'Emboite': ('n_snug', 'm', '+5', 'Voisinage'),
 'Mosaique': ('n_mosaic', 'm', '+3', 'Couleurs'),
 'Foule': ('n_crowd', 'm', '×2', 'Voisinage'),
 'Prairie': ('n_meadow', 'm', '+4', 'Couleurs'),
 'Guirlande': ('n_wreath', 'p', '+8', 'Voisinage'),
 'Pollinisateur': ('n_bee', 'l', '+2', 'Couleurs'),
 'Miel': ('n_honey', 'l', '+1', 'Connexions'),
 'Nectar': ('n_nectar', 'l', '+1', 'Roguelike'),
 'MoissonCroisee': ('n_cross', 'm', '+6', 'Destruction'),
 'Faucheur': ('n_sickle', 'm', '+3', 'Destruction'),
 'TableRase': ('n_clean', 'm', '×3', 'Destruction'),
 'Dime': ('n_tithe', 'l', '+2', 'Destruction'),
 'CultureRang': ('n_rowcrop', 'm', '×2', 'Destruction'),
 'CultureColonne': ('n_colcrop', 'm', '×2', 'Destruction'),
 'Prime': ('n_bounty', 'p', '+3', 'Destruction'),
 'BriseGlace': ('n_icebreak', 'm', '+6', 'Destruction'),
 'Saisons': ('n_season', 'm', '+0.5', 'Roguelike'),
 'Outsider': ('n_under', 'm', '+3', 'Roguelike'),
 'CoupFinal': ('n_finish', 'm', '+6', 'Roguelike'),
 'DerniereGraine': ('n_last', 'm', '×3', 'Roguelike'),
 'PremiereGraine': ('n_first', 'm', '×2', 'Roguelike'),
 'Tirelire': ('n_pig', 'm', '+0.1', 'Roguelike'),
 'BanqueGraines': ('n_bank', 'm', '+0.05', 'Roguelike'),
 'SousBois': ('n_brush', 'm', '+1', 'Roguelike'),
 'DanseDePluie': ('n_raindance', 'm', '+4', 'Roguelike'),
 'DanseDuSoleil': ('n_sundance', 'm', '+4', 'Roguelike'),
 'Rustique': ('n_frosthardy', 'm', '+4', 'Roguelike'),
 'CavalierVent': ('n_wind', 'm', '+4', 'Roguelike'),
 'Matinal': ('n_bird', 'm', '+6', 'Roguelike'),
 'Marathonien': ('n_flag', 'm', '+4', 'Roguelike'),
 'Pourfendeur': ('n_bossbane', 'm', '×2', 'Roguelike'),
 'Tonneau': ('n_barrel', 'l', '+1', 'Roguelike'),
 'Grange': ('n_granary', 'l', '+1', 'Roguelike'),
 'Cadence': ('metronome', 'm', '×2', 'Roguelike'), 'Echo': ('echo', None, None, 'Roguelike'), 'Siphon': ('siphon', 'l', '4%', 'Roguelike'), 'QuatriemeEmplacement': ('slot4', None, None, 'Roguelike'),
}
for k in ('scythe', 'wheel', 'ashbound', 'dissonant', 'momentum', 'cocoon', 'trowel', 'rainbow', 'alternation'):
    pass
M['scythe'] = M['scythe']
TAGCOL = {'m': ('#c8403f', '#ffffff'), 'p': ('#4f7fd0', '#ffffff'), 'l': ('#f1c40f', O), 'd': ('#3a2a1c', '#ffd966')}

def tag(kind, txt, cx, cy):
    if not kind: return ''
    bg, fg = TAGCOL[kind]
    w = max(52, 17 * len(txt) + 14)
    return '<g><rect x="%g" y="%g" width="%g" height="30" rx="15" fill="%s" stroke="%s" stroke-width="4"/>%s</g>' % (cx - w / 2, cy - 15, w, bg, O, text(cx, cy + 8, txt, 22, fg))

def sack(motif_svg, catcol, tagkind, tagtxt):
    s = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256">\n'
    s += '<defs><clipPath id="sk"><path d="M96 60 C52 78 26 140 38 198 C44 226 74 238 128 238 C182 238 212 226 218 198 C230 140 204 78 160 60 Z"/></clipPath></defs>\n'
    # gathered neck (frilled top of the tied sack)
    s += '<path d="M92 64 C84 40 100 26 128 26 C156 26 172 40 164 64 Z" fill="#c9a064" stroke="%s" stroke-width="6" stroke-linejoin="round"/><path d="M112 60 C108 46 112 38 118 32 M128 60 V32 M144 60 C148 46 144 38 138 32" fill="none" stroke="#a8823e" stroke-width="3" stroke-linecap="round"/>\n' % O
    s += '<path d="M96 60 C52 78 26 140 38 198 C44 226 74 238 128 238 C182 238 212 226 218 198 C230 140 204 78 160 60 Z" fill="#d8b574" stroke="%s" stroke-width="6" stroke-linejoin="round"/>\n' % O
    s += '<g clip-path="url(#sk)" stroke="#b8924e" stroke-width="2.5" opacity="0.7">' + ''.join('<path d="M20 %d H240"/>' % y for y in range(80, 240, 12)) + ''.join('<path d="M%d 60 V240"/>' % x for x in range(40, 220, 14)) + '</g>\n'
    s += '<path d="M60 110 C46 150 50 200 66 228" fill="none" stroke="#fff3cf" stroke-width="6" stroke-linecap="round" opacity="0.35"/>\n'
    # rope + knot
    s += '<rect x="88" y="54" width="80" height="16" rx="8" fill="#8a5a2b" stroke="%s" stroke-width="5"/><circle cx="128" cy="62" r="10" fill="#a8723a" stroke="%s" stroke-width="4"/>\n' % (O, O)
    s += '<path d="M128 70 C120 84 112 88 106 100 M128 70 C138 84 146 90 150 98" fill="none" stroke="%s" stroke-width="8" stroke-linecap="round"/><path d="M128 70 C120 84 112 88 106 100 M128 70 C138 84 146 90 150 98" fill="none" stroke="#a8723a" stroke-width="4" stroke-linecap="round"/>\n' % O
    # label
    s += '<rect x="64" y="104" width="128" height="110" rx="16" fill="%s" stroke="%s" stroke-width="5"/><rect x="71" y="111" width="114" height="96" rx="11" fill="none" stroke="%s" stroke-width="5"/>\n' % (CREAM, O, catcol)
    s += '<g transform="translate(75.5 111.5) scale(1.05)">%s</g>\n' % motif_svg
    s += tag(tagkind, tagtxt, 176, 214)
    return s + '</svg>\n'

# ---------------------------------------------------------------- upgrades: UpgradeId -> (motif, pool)
POOL = {'Bank': '#c8403f', 'Grid': '#4f7fd0', 'Mastery': '#8c63b0', 'Modifier': '#e8a82e'}
def sprite_seed(col=None): return seed(50, 46, 2.2)
M['u_replace'] = lambda: seed(28, 46, 1.5, '#d8b574') + seed(72, 46, 1.5, '#e8c98a') + arrow(40, 26, 62, 26, O, 4) + arrow(60, 68, 38, 68, O, 4)
M['u_dup'] = lambda: seed(40, 52, 1.8, '#d8b574') + seed(62, 42, 1.8, '#e8c98a')
M['u_joker'] = lambda: seed(50, 50, 2.2, P) + star(50, 46, 12, '#fff1b0', 0.4, 5, 3)
M['u_recolor'] = lambda: seed(36, 50, 1.6) + drop(70, 26, 14, R) + drop(80, 54, 11, B) + drop(64, 70, 10, Y)
M['u_dice'] = lambda: rect(22, 18, 56, 56, CREAM, 10, 4) + ''.join(circ(x, y, 5.5, O, 1) for x, y in ((38, 34), (62, 34), (50, 46), (38, 58), (62, 58)))
M['u_randseed'] = lambda: seed(38, 54, 1.8, '#d8b574') + rect(54, 20, 34, 34, CREAM, 7, 3.5) + ''.join(circ(x, y, 3.2, O, 1) for x, y in ((64, 30), (78, 30), (71, 37), (64, 44), (78, 44)))
M['u_fertup'] = lambda: M['pots']() + arrow(80, 44, 80, 14, G, 6)
M['u_goldtile'] = lambda: tile(22, 22, 56, GOLD) + star(50, 50, 18, '#fff1b0', 0.4, 4, 3)
M['u_tint'] = lambda: tile(22, 30, 50, '#e0d6bf') + drop(66, 26, 20, R) + line(30, 44, 56, 44, R, 6)
M['u_mult'] = lambda: tile(14, 16, 54, '#ffe9a6') + num(42, 58, '×2', 34) 
M['u_blast'] = lambda: star(50, 46, 40, '#ff8a3d', 0.5, 8) + circ(50, 46, 14, '#3a2a1c', 3)
M['u_beacon'] = lambda: poly([(38, 82), (44, 30), (56, 30), (62, 82)], '#f4efe0') + rect(38, 18, 24, 14, GOLD, 3) + ''.join(line(50, 25, 50 + dx, 25 + dy, GOLD, 4) for dx, dy in ((-34, -10), (34, -10), (-30, 16), (30, 16)))
M['u_mirror'] = lambda: tile(14, 28, 30, B) + tile(56, 28, 30, B) + line(50, 14, 50, 78, O, 3)
M['u_seeder'] = lambda: path('M26 38 L74 38 C84 56 84 78 66 82 L34 82 C16 78 16 56 26 38 Z', '#d8b574') + path('M34 38 C36 26 64 26 66 38', '#c9a064') + ''.join(seed(x, y, 0.6) for x, y in ((50, 12), (36, 20), (66, 18)))
M['u_catalyst'] = lambda: flask(50, 46, '#7ad0e8') + star(76, 18, 10, '#fff1b0', 0.4, 4, 3)
M['u_twin'] = lambda: tile(12, 30, 32, G) + tile(56, 30, 32, G) + path('M44 46 L56 46', 'none', 5)
M['u_detonator'] = lambda: rect(26, 38, 48, 40, '#8a5a2b', 5) + rect(40, 20, 20, 18, '#cfd4da', 4) + line(50, 20, 50, 8, O, 5) + star(50, 8, 8, '#ff8a3d', 0.5, 6, 2.5)
M['u_chameleon'] = lambda: ell(48, 50, 30, 18, G) + circ(70, 40, 8, '#fff', 3) + circ(71, 40, 3, O, 1) + ''.join(circ(30 + i * 12, 48, 5, c, 2) for i, c in enumerate((R, Y, B))) + path('M18 54 C8 70 20 78 28 66', 'none', 5)
M['u_spark'] = lambda: star(50, 46, 40, '#fff1b0', 0.3, 4) + circ(50, 46, 8, GOLD, 3)
M['u_void'] = lambda: circ(50, 46, 36, '#1b1230') + circ(50, 46, 22, '#3a2060') + circ(50, 46, 10, O, 1) + star(26, 20, 7, '#d8c4ff', 0.4, 4, 2)
M['u_bastion'] = lambda: path('M50 10 L84 22 L84 50 C84 70 66 80 50 86 C34 80 16 70 16 50 L16 22 Z', '#a0a6ae') + path('M50 22 L72 30 L72 50 C72 62 60 70 50 74 C40 70 28 62 28 50 L28 30 Z', '#d4d8de', 3)
M['u_kamikaze'] = lambda: circ(50, 54, 26, '#3a3a44') + line(60, 32, 72, 14, '#8a5a2b', 5) + star(76, 12, 10, '#ff8a3d', 0.5, 6, 2.5) + circ(42, 46, 5, '#fff', 2)
M['u_venom'] = lambda: drop(50, 46, 32, '#8ac24a') + circ(40, 52, 6, '#3a5a1a', 2) + circ(60, 56, 4, '#3a5a1a', 2) + '<path d="M40 18 L38 6 M60 18 L62 6" stroke="#13212e" stroke-width="3"/>'
M['u_bonussq'] = lambda: tile(18, 18, 64, '#e0d6bf') + num(50, 62, '★', 40, '#e8a82e') if False else tile(18, 18, 64, '#e0d6bf') + star(50, 50, 22, GOLD, 0.45, 5, 3) + tile(18, 18, 64, 'none')
M['u_seedmastery'] = lambda: seed(50, 52, 2.0) + ''.join(star(x, 14, 8, GOLD, 0.45, 5, 2.5) for x in (30, 50, 70))
M['u_colormastery'] = lambda: rect(10, 26, 80, 46, '#c58a4a', 14) + ''.join(circ(x, 42, 8, c, 3) for x, c in ((26, R), (42, B), (58, Y), (74, G))) + circ(50, 64, 6, CREAM, 3) + star(80, 16, 9, GOLD, 0.45, 5, 2.5)

U = {
 'ReplacePiece': ('u_replace', 'Bank'), 'DuplicatePiece': ('u_dup', 'Bank'), 'JokerPiece': ('u_joker', 'Bank'), 'RecolorPiece': ('u_recolor', 'Bank'),
 'RandomModifier': ('u_dice', 'Bank'), 'RandomPiece': ('u_randseed', 'Bank'), 'ModifierUpgrade': ('u_fertup', 'Modifier'),
 'GoldenCells': ('u_goldtile', 'Grid'), 'TintedCells': ('u_tint', 'Grid'), 'MultiplierZone': ('u_mult', 'Grid'), 'BlastTile': ('u_blast', 'Grid'),
 'MultiplierBeacon': ('u_beacon', 'Grid'), 'MirrorTile': ('u_mirror', 'Grid'), 'Seeder': ('u_seeder', 'Grid'), 'CatalystTile': ('u_catalyst', 'Grid'),
 'TwinTile': ('u_twin', 'Grid'), 'DetonatorTile': ('u_detonator', 'Grid'), 'ChameleonTile': ('u_chameleon', 'Grid'), 'SparkTile': ('u_spark', 'Grid'),
 'VoidTile': ('u_void', 'Grid'), 'BastionTile': ('u_bastion', 'Grid'), 'KamikazeTile': ('u_kamikaze', 'Grid'), 'VenomTile': ('u_venom', 'Grid'),
 'PermanentBonusSquare': ('u_bonussq', 'Grid'), 'PieceMastery': ('u_seedmastery', 'Mastery'), 'ColorMastery': ('u_colormastery', 'Mastery'),
}

def sign(motif_svg, poolcol):
    s = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256">\n'
    s += '<path d="M112 170 L112 226 L128 250 L144 226 L144 170 Z" fill="#b8864a" stroke="%s" stroke-width="6" stroke-linejoin="round"/><path d="M122 180 V226 M134 180 V222" stroke="#8a5a2b" stroke-width="3"/>\n' % O
    s += '<rect x="20" y="14" width="216" height="166" rx="22" fill="#c58a4a" stroke="%s" stroke-width="6"/>\n' % O
    s += '<path d="M30 70 H226 M30 124 H226" stroke="#8a5a2b" stroke-width="3" opacity="0.7"/>\n'
    s += '<rect x="20" y="14" width="216" height="26" rx="14" fill="%s" stroke="%s" stroke-width="6"/>\n' % (poolcol, O)
    s += ''.join('<circle cx="%d" cy="%d" r="6" fill="#5a3a1e" stroke="%s" stroke-width="3"/>' % (x, y, O) for x, y in ((38, 156), (218, 156)))
    s += '<rect x="48" y="52" width="160" height="108" rx="14" fill="%s" stroke="%s" stroke-width="5"/>\n' % (CREAM, O)
    s += '<g transform="translate(73 56.5) scale(1.1)">%s</g>\n' % motif_svg
    return s + '</svg>\n'

if __name__ == '__main__':
    os.makedirs('fertilizers', exist_ok=True); os.makedirs('upgrades', exist_ok=True)
    for k, (m, kind, txt, cat) in F.items():
        open('fertilizers/%s.svg' % k, 'w').write(sack(M[m](), CAT[cat], kind, txt))
    for k, (m, pool) in U.items():
        open('upgrades/%s.svg' % k, 'w').write(sign(M[m](), POOL[pool]))
    print(len(F), 'fertilizers,', len(U), 'upgrades')

# ---------------------------------------------------------------- mystery plates for the shop's Casino cards
# Same silhouettes as the real items (sign = upgrade, sack = fertilizer) but the label is a dark night plate holding only the
# dark outline of the pool's motif and a golden "?": the player sees the CATEGORY, never the exact item.
SIL_DEFS = ('<defs><filter id="sil" x="-10%" y="-10%" width="120%" height="120%"><feColorMatrix type="matrix" '
            'values="0 0 0 0 0.36  0 0 0 0 0.31  0 0 0 0 0.58  0 0 0 1 0"/></filter></defs>')
MYSTERY_BG = '#2a2342'
def _qmark(cx, cy):
    stars = star(cx - 46, cy - 30, 9, 0.4) if False else ''
    return ('<text x="%g" y="%g" font-family="Arial Black, Arial, sans-serif" font-weight="900" font-size="74" text-anchor="middle" '
            'fill="#ffd966" stroke="%s" stroke-width="7" paint-order="stroke" stroke-linejoin="round">?</text>') % (cx, cy + 26, O)
def _sparkles(cx, cy):
    s = ''
    for dx, dy, r in ((-52, -34, 8), (50, -22, 6), (-40, 40, 5), (52, 44, 7)):
        s += star(cx + dx, cy + dy, r, '#fff1b0', 0.4, 4, 2.5)
    return s

def mystery_sign(motif_svg, poolcol):
    svg = sign(motif_svg, poolcol)
    svg = svg.replace('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256">\n', '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256">\n' + SIL_DEFS + '\n', 1)
    svg = svg.replace('<rect x="48" y="52" width="160" height="108" rx="14" fill="%s"' % CREAM, '<rect x="48" y="52" width="160" height="108" rx="14" fill="%s"' % MYSTERY_BG, 1)
    svg = svg.replace('<g transform="translate(73 56.5) scale(1.1)">', '<g filter="url(#sil)" transform="translate(73 56.5) scale(1.1)">', 1)
    return svg.replace('</svg>', _sparkles(128, 106) + _qmark(128, 100) + '</svg>')

def mystery_sack(motif_svg, catcol):
    svg = sack(motif_svg, catcol, None, None)
    svg = svg.replace('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256">\n', '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256">\n' + SIL_DEFS + '\n', 1)
    svg = svg.replace('<rect x="64" y="104" width="128" height="110" rx="16" fill="%s"' % CREAM, '<rect x="64" y="104" width="128" height="110" rx="16" fill="%s"' % MYSTERY_BG, 1)
    svg = svg.replace('<g transform="translate(75.5 111.5) scale(1.05)">', '<g filter="url(#sil)" transform="translate(75.5 111.5) scale(1.05)">', 1)
    return svg.replace('</svg>', _sparkles(128, 159) + _qmark(128, 153) + '</svg>')

if __name__ == '__main__':
    os.makedirs('mystery', exist_ok=True)
    open('mystery/Bank.svg', 'w').write(mystery_sign(seed(50, 46, 2.4), POOL['Bank']))
    open('mystery/Grid.svg', 'w').write(mystery_sign(tile(20, 14, 60, '#e0d6bf'), POOL['Grid']))
    open('mystery/Mastery.svg', 'w').write(mystery_sign(M['u_seedmastery'](), POOL['Mastery']))
    open('mystery/Modifier.svg', 'w').write(mystery_sign(M['u_fertup'](), POOL['Modifier']))
    open('mystery/Boon.svg', 'w').write(mystery_sack(flask(50, 46, '#b469e0'), '#e8863a'))
    print('mystery: 5')
