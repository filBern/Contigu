#!/usr/bin/env python3
"""Ten plant varieties for the buttons. Each has an A (taller, left) and a B (small, right) sprite, both upright on a 64x64 canvas
with their base at the bottom center, so they can stand on a button's top edge like a planter box.
Output: plants/V<n>A.svg, plants/V<n>B.svg"""
import math, os
O = '#13212e'
G1, G2, G3 = '#6fb04f', '#4f9440', '#3f7a35'

def svg(body): return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" width="64" height="64"><g stroke-linejoin="round" stroke-linecap="round">%s</g></svg>\n' % body
def stem(d, w=3.6, col=G2): return '<path d="%s" fill="none" stroke="%s" stroke-width="%g"/><path d="%s" fill="none" stroke="%s" stroke-width="%g"/>' % (d, O, w + 4, d, col, w)
def leaf(x, y, length, ang, fill=G1):
    w = length * 0.4
    return '<path d="M0 0 C%g %g, %g %g, %g 0 C%g %g, %g %g, 0 0 Z" transform="translate(%g %g) rotate(%g)" fill="%s" stroke="%s" stroke-width="2.2"/>' % (length*.2, -w, length*.7, -w, length, length*.7, w, length*.2, w, x, y, ang, fill, O)
def circ(x, y, r, fill, sw=2.2): return '<circle cx="%g" cy="%g" r="%g" fill="%s" stroke="%s" stroke-width="%g"/>' % (x, y, r, fill, O, sw)
def ell(x, y, rx, ry, fill, rot=0, sw=2.2): return '<ellipse cx="%g" cy="%g" rx="%g" ry="%g" transform="rotate(%g %g %g)" fill="%s" stroke="%s" stroke-width="%g"/>' % (x, y, rx, ry, rot, x, y, fill, O, sw)
def flower(cx, cy, r, petal, center, n=5):
    s = ''
    for i in range(n):
        a = 360.0 * i / n - 90
        s += ell(cx + r*.6*math.cos(math.radians(a)), cy + r*.6*math.sin(math.radians(a)), r*.46, r*.36, petal, a, 2)
    return s + circ(cx, cy, r*.32, center, 2)
def poly(pts, fill): return '<path d="M%s Z" fill="%s" stroke="%s" stroke-width="2.2"/>' % (' L'.join('%g %g' % p for p in pts), fill, O)
def bell(x, y, r, col): return '<path d="M%g %g C%g %g, %g %g, %g %g L%g %g Q%g %g %g %g Q%g %g %g %g Z" fill="%s" stroke="%s" stroke-width="2"/>' % (x-r*.4, y-r, x-r*1.2, y-r*.4, x-r*1.1, y+r*.6, x-r*1.3, y+r*1.1, x+r*1.3, y+r*1.1, x+r*.4, y+r*.7, x+r*.2, y+r*1.1, x+r*.0, y+r*.6) if False else \
    '<path d="M%g %g C%g %g %g %g %g %g L%g %g C%g %g %g %g %g %g Z" fill="%s" stroke="%s" stroke-width="2"/>' % (x-r*.35, y-r, x-r*1.1, y-r*.2, x-r*1.1, y+r*.7, x-r*1.3, y+r*1.1, x+r*1.3, y+r*1.1, x+r*1.1, y+r*.7, x+r*1.1, y-r*.2, x+r*.35, y-r, col, O)
def mushroom(x, base, h, cap, spots=True):
    s = '<path d="M%g %g C%g %g %g %g %g %g L%g %g Z" fill="#efe3c4" stroke="%s" stroke-width="2.2"/>' % (x-h*.22, base, x-h*.2, base-h*.4, x-h*.2, base-h*.55, x-h*.18, base-h*.62, x+h*.18, base-h*.62, O)
    s = '<rect x="%g" y="%g" width="%g" height="%g" rx="%g" fill="#efe3c4" stroke="%s" stroke-width="2.2"/>' % (x-h*.17, base-h*.7, h*.34, h*.7, h*.1, O)
    s += '<path d="M%g %g C%g %g %g %g %g %g Z" fill="%s" stroke="%s" stroke-width="2.4"/>' % (x-h*.55, base-h*.62, x-h*.55, base-h*1.25, x+h*.55, base-h*1.25, x+h*.55, base-h*.62, cap, O)
    if spots:
        s += circ(x-h*.2, base-h*.9, h*.1, '#fff7e8', 1.5) + circ(x+h*.15, base-h*1.0, h*.08, '#fff7e8', 1.5)
    return s
def tulip(x, base, h, col):
    top = base - h
    body = ('<path d="M%g %g C%g %g %g %g %g %g L%g %g L%g %g L%g %g L%g %g C%g %g %g %g %g %g Z" fill="%s" stroke="%s" stroke-width="2.2"/>'
            % (x-9, top+12, x-11, top+2, x-9, top-4, x-6, top-6, x-3, top, x, top-6, x+3, top, x+6, top-6, x+9, top-4, x+11, top+2, x+9, top+12, col, O))
    return stem('M%g %g V%g' % (x, base, top+8), 3) + body

V = {}
# 1 poppy
V[1] = (stem('M32 62 V26') + leaf(32, 52, 20, 205) + leaf(32, 42, 18, -25, G2) + flower(32, 20, 13, '#c8403f', '#e8a82e'),
        stem('M32 62 V40') + leaf(32, 54, 14, 200) + ell(32, 34, 8, 10, '#c8403f') + leaf(32, 42, 10, 120, G2))
# 2 bluebell
V[2] = (stem('M18 62 C16 40 28 22 46 22', 3.4) + ''.join(stem('M%g %g V%g' % (x, y, y+5), 2) for x, y in ((30, 28), (40, 22))) + bell(30, 38, 6, '#4f7fd0') + bell(40, 32, 6, '#4f7fd0') + bell(46, 44, 5.5, '#86aef0') + leaf(18, 56, 16, 200),
        stem('M32 62 C30 48 36 38 44 38') + bell(44, 46, 6, '#4f7fd0') + leaf(32, 56, 12, 200))
# 3 sunflower
V[3] = (stem('M32 62 V28', 4) + leaf(32, 54, 20, 205) + leaf(32, 44, 20, -25, G2) + flower(32, 20, 15, '#f6b930', '#5a3a1e', 9),
        leaf(32, 60, 18, -150) + leaf(32, 60, 18, -30, G2) + stem('M32 62 V54', 3))
# 4 fern
fr = stem('M20 62 C18 40 28 22 46 14', 3.2, G3)
for t in (0.2, 0.35, 0.5, 0.65, 0.8):
    x = 20 + 26 * t * 0.9 + (1 - t) * 0; y = 62 - 48 * t
    fr += ell(x - 7, y, 7 * (1.1 - t * 0.5), 3, G1, -30) + ell(x + 7, y - 4, 7 * (1.1 - t * 0.5), 3, G2, 30)
V[4] = (fr, stem('M24 62 C22 50 26 40 34 40 C42 40 42 50 36 50 C32 50 32 46 34 46', 3.4, G3))
# 5 lavender
def spike(x, base, h):
    s = stem('M%g %g V%g' % (x, base, base-h), 2.6)
    for i in range(5):
        y = base - h + 3 + i * 5
        s += ell(x - 3.5, y, 3.6, 2.8, '#8c63b0', -25, 1.8) + ell(x + 3.5, y + 2, 3.6, 2.8, '#a884c8', 25, 1.8)
    return s + ell(x, base - h - 2, 3, 4, '#8c63b0', 0, 1.8)
V[5] = (spike(18, 62, 40) + spike(32, 62, 48) + spike(46, 62, 38) + leaf(32, 62, 14, 200) + leaf(32, 62, 14, -20, G2), spike(32, 62, 34))
# 6 mushrooms
V[6] = (mushroom(22, 62, 30, '#c8403f') + mushroom(46, 62, 20, '#e0a850', False) + circ(34, 60, 2, G1, 1.5), mushroom(32, 62, 22, '#c8403f'))
# 7 berries
V[7] = (stem('M32 62 C30 44 36 30 34 18', 3.2) + leaf(32, 50, 18, 205) + leaf(33, 38, 18, -20, G2) + leaf(34, 26, 16, 200) + ''.join(circ(x, y, 5, '#c8403f') for x, y in ((24, 20), (40, 24), (30, 12), (44, 12))), stem('M32 62 V44', 3) + ''.join(circ(x, y, 5.5, '#c8403f') for x, y in ((26, 42), (38, 42), (32, 34))) + leaf(32, 56, 12, 200))
# 8 clover
def clover(x, base, h, four=False):
    s = stem('M%g %g V%g' % (x, base, base - h), 2.8)
    cy = base - h
    for a in ((-90 + 45), (-90 - 45), 90 + 45, 90 - 45)[: 4 if four else 3] if four else (-90, 30, 150):
        px, py = x + 7 * math.cos(math.radians(a)), cy + 7 * math.sin(math.radians(a))
        s += circ(px - 3, py, 5.5, G1, 2) + circ(px + 3, py, 5.5, G1, 2)
    return s
cl = stem('M22 62 V40', 2.8) + stem('M42 62 V34', 2.8) + stem('M32 62 V26', 2.8)
for cx, cy in ((22, 38), (42, 32), (32, 24)):
    for a in (-90, 30, 150):
        px, py = cx + 6.5 * math.cos(math.radians(a)), cy + 6.5 * math.sin(math.radians(a))
        cl += circ(px - 3, py - 1, 5, G1, 2) + circ(px + 3, py - 1, 5, G2, 2)
fl = stem('M32 62 V38', 2.8)
for a in (-135, -45, 45, 135):
    px, py = 32 + 8 * math.cos(math.radians(a)), 32 + 8 * math.sin(math.radians(a))
    fl += circ(px - 3.2, py, 5, G1, 2) + circ(px + 3.2, py, 5, G2, 2)
V[8] = (cl, fl)
# 9 tulips
V[9] = (leaf(24, 62, 30, -100) + leaf(40, 62, 30, -80, G2) + tulip(24, 62, 38, '#c8403f') + tulip(42, 62, 30, '#f6b930'), leaf(32, 62, 20, -100) + tulip(32, 62, 30, '#e87aa0'))
# 10 ivy
iv = stem('M32 62 C28 46 38 34 30 16', 3)
for x, y, a in ((26, 52, 200), (36, 42, -20), (28, 30, 205), (36, 20, -15)):
    iv += leaf(x, y, 15, a, G1 if a < 0 or a > 180 and y > 40 else G2)
V[10] = (iv, stem('M32 62 C30 52 34 46 32 40', 2.8) + leaf(32, 52, 14, 200) + leaf(32, 44, 14, -20, G2))

if __name__ == '__main__':
    os.makedirs('plants', exist_ok=True)
    for n, (a, b) in V.items():
        open('plants/V%dA.svg' % n, 'w').write(svg(a))
        open('plants/V%dB.svg' % n, 'w').write(svg(b))
    print(len(V) * 2)
