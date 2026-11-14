#!/usr/bin/env python3
"""LASTBLOOM title logo (vector): chunky monoline letters in the four bloom colors, the two O's are flowers, sprouts grow from the letters."""
import math
O = '#13212e'
R, B, Y, G = '#c8403f', '#4f7fd0', '#e8a82e', '#5a9e45'
LIGHT = {R: '#e8756b', B: '#86aef0', Y: '#f6cf6a', G: '#8cc470'}
SW_IN, SW_OUT = 24, 38

# local letter outlines in a 0..80 x 0..120 box
L = 'M12 0 V120 H72'
A = 'M2 120 L40 4 L78 120 M17 82 H63'
S = 'M72 20 C60 -6 10 -4 10 32 C10 66 70 54 70 90 C70 128 16 124 6 98'
T = 'M2 6 H78 M40 6 V120'
Bl = 'M12 4 V116 M12 6 H44 C80 6 80 58 44 60 H12 M44 60 C88 60 88 116 44 116 H12'
M = 'M8 120 V6 L40 74 L72 6 V120'

def sprout(x, y, rot=0, s=1.0):
    return ('<g transform="translate(%g %g) rotate(%g) scale(%g)" stroke="%s" stroke-width="3.5" stroke-linejoin="round" stroke-linecap="round">'
            '<path d="M0 0 V-16" fill="none"/>'
            '<path d="M0 -12 C-18 -14 -26 -26 -22 -34 C-8 -34 -2 -24 0 -12Z" fill="#6fb04f"/>'
            '<path d="M0 -14 C16 -16 26 -28 22 -38 C8 -36 2 -26 0 -14Z" fill="#4f9440"/></g>') % (x, y, rot, s, O)

def flower(cx, cy, r, petal, center, n=5):
    s = ''
    for i in range(n):
        a = 360.0 * i / n - 90
        px, py = cx + r * 0.58 * math.cos(math.radians(a)), cy + r * 0.58 * math.sin(math.radians(a))
        s += '<ellipse cx="%g" cy="%g" rx="%g" ry="%g" transform="rotate(%g %g %g)" fill="%s" stroke="%s" stroke-width="3"/>' % (px, py, r * 0.46, r * 0.36, a, px, py, petal, O)
    return s + '<circle cx="%g" cy="%g" r="%g" fill="%s" stroke="%s" stroke-width="3"/>' % (cx, cy, r * 0.3, center, O)

letters = [('L', L, R), ('A', A, B), ('S', S, Y), ('T', T, G), ('B', Bl, R), ('L', L, B), ('O', None, Y), ('O', None, G), ('M', M, R)]
X0, GAP, W = 56, 30, 80
Y0 = 96
out = ['<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1080 300" width="1080" height="300">',
       '<defs><linearGradient id="soil" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#6b4528"/><stop offset="1" stop-color="#3a2a1c"/></linearGradient></defs>',
       # ground strip
       '<path d="M24 238 Q540 214 1056 238 L1056 262 Q540 282 24 262 Z" fill="url(#soil)" stroke="%s" stroke-width="6" stroke-linejoin="round"/>' % O,
       '<g stroke="#2a1c12" stroke-width="3" stroke-linecap="round" opacity="0.7"><path d="M80 246 Q200 238 320 246"/><path d="M420 248 Q540 240 660 248"/><path d="M760 246 Q880 238 1000 246"/></g>']
extra = []
for i, (ch, d, col) in enumerate(letters):
    x = X0 + i * (W + GAP)
    g = '<g transform="translate(%g %g)">' % (x, Y0)
    if ch == 'O':
        ring = '<ellipse cx="40" cy="60" rx="36" ry="56" fill="none" stroke="%s" stroke-width="%d"/>'
        g += (ring % (O, SW_OUT)) + (ring % (col, SW_IN))
        g += '<ellipse cx="40" cy="60" rx="36" ry="56" fill="none" stroke="%s" stroke-width="6" opacity="0.6" transform="translate(-3 -3)"/>' % LIGHT[col]
        g += flower(40, 60, 24, R if i == 6 else B, Y if i == 6 else '#5a3a1e')
    else:
        g += '<path d="%s" fill="none" stroke="%s" stroke-width="%d" stroke-linejoin="round" stroke-linecap="round"/>' % (d, O, SW_OUT)
        g += '<path d="%s" fill="none" stroke="%s" stroke-width="%d" stroke-linejoin="round" stroke-linecap="round"/>' % (d, col, SW_IN)
        g += '<path d="%s" fill="none" stroke="%s" stroke-width="6" stroke-linejoin="round" stroke-linecap="round" opacity="0.6" transform="translate(-3 -3)"/>' % (d, LIGHT[col])
    g += '</g>'
    out.append(g)
# sprouts and a few leaves growing from the letters
out.append(sprout(X0 + 1 * (W + GAP) + 40, Y0 - 14, 0, 1.1))        # A apex
out.append(sprout(X0 + 3 * (W + GAP) + 6, Y0 - 12, -25, 0.9))       # T left arm
out.append(sprout(X0 + 4 * (W + GAP) + 12, Y0 - 16, 10, 1.0))       # B top
out.append(sprout(X0 + 8 * (W + GAP) + 8, Y0 - 6, -20, 1.0))        # M left peak
out.append(sprout(X0 + 8 * (W + GAP) + 72, Y0 - 6, 20, 1.0))        # M right peak
out.append(sprout(X0 + 0 * (W + GAP) + 12, Y0 + 134, 0, 0.9))       # L foot
out.append(sprout(X0 + 5 * (W + GAP) + 70, Y0 + 134, 0, 0.9))       # second L foot
# tiny pollen sparkles
for x, y, r in [(40, 60, 4), (1040, 70, 4), (520, 40, 3.5), (330, 36, 3), (800, 44, 3)]:
    out.append('<circle cx="%g" cy="%g" r="%g" fill="#ffe08a" stroke="%s" stroke-width="2"/>' % (x, y, r, O))
out.append('</svg>')
open('LastBloom.svg', 'w').write('\n'.join(out) + '\n')
print('ok')
