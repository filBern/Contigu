#!/usr/bin/env python3
"""Plant decorations for every button: a corner vine (placed top-left and, mirrored, bottom-right) and a small sprout (top edge).
Drawn at 64x64 in the same flat garden style as the title letters. Output: VineCorner.svg, VineSprout.svg"""
import math
O = '#13212e'
def leaf(cx, cy, length, ang, fill):
    w = length * 0.42
    return '<path d="M0 0 C%g %g, %g %g, %g 0 C%g %g, %g %g, 0 0 Z" transform="translate(%g %g) rotate(%g)" fill="%s" stroke="%s" stroke-width="2.4" stroke-linejoin="round"/>' % (
        length * 0.2, -w, length * 0.7, -w, length, length * 0.7, w, length * 0.2, w, cx, cy, ang, fill, O)
def flower(cx, cy, r, petal, center):
    s = ''
    for i in range(5):
        a = 72.0 * i - 90
        px, py = cx + r * 0.58 * math.cos(math.radians(a)), cy + r * 0.58 * math.sin(math.radians(a))
        s += '<ellipse cx="%g" cy="%g" rx="%g" ry="%g" transform="rotate(%g %g %g)" fill="%s" stroke="%s" stroke-width="2"/>' % (px, py, r * 0.46, r * 0.36, a, px, py, petal, O)
    return s + '<circle cx="%g" cy="%g" r="%g" fill="%s" stroke="%s" stroke-width="2"/>' % (cx, cy, r * 0.3, center, O)

corner = ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" width="64" height="64"><g transform="translate(9 8) scale(0.82)">'
          '<path d="M8 58 C4 40 8 22 22 14 C32 8 44 8 56 10" fill="none" stroke="%s" stroke-width="8" stroke-linecap="round"/>'
          '<path d="M8 58 C4 40 8 22 22 14 C32 8 44 8 56 10" fill="none" stroke="#4f9440" stroke-width="4.5" stroke-linecap="round"/>'
          '%s%s%s%s%s</g></svg>\n') % (O,
          leaf(8, 44, 20, 200, '#6fb04f'), leaf(14, 26, 20, -140, '#4f9440'), leaf(34, 12, 20, -70, '#6fb04f'), leaf(46, 12, 16, 10, '#4f9440'),
          flower(22, 24, 9, '#c8403f', '#e8a82e'))
sprout = ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" width="64" height="64"><g transform="translate(0 4) scale(0.92) translate(2.5 0)">'
          '<path d="M32 60 V34" fill="none" stroke="%s" stroke-width="8" stroke-linecap="round"/>'
          '<path d="M32 60 V34" fill="none" stroke="#4f9440" stroke-width="4.5" stroke-linecap="round"/>'
          '%s%s</g></svg>\n') % (O, leaf(32, 40, 26, -155, '#6fb04f'), leaf(32, 40, 26, -25, '#4f9440'))
open('VineCorner.svg', 'w').write(corner)
open('VineSprout.svg', 'w').write(sprout)
print('ok')
