#!/usr/bin/env python3
"""Simplified garden tiles: a plain colored bed border, one flat soil inset, one bold flat emblem. (The detailed version is
generate_tiles_detailed.py.) Writes <Name>.svg + <Name>_256.png here, and 128px copies into Assets/Resources.
Run from this folder: python3 generate_tiles.py"""
import math, os, subprocess
O = '#13212e'
CHROME = '/opt/pw-browsers/chromium-1194/chrome-linux/chrome'
RES = '../../Assets/Resources'

def tile(b_top, b_bot, soil, emblem, extra_defs=''):
    return ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256">'
            '<defs><linearGradient id="b" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="%s"/><stop offset="1" stop-color="%s"/></linearGradient>%s'
            '<clipPath id="c"><rect x="32" y="32" width="192" height="192" rx="14"/></clipPath></defs>'
            '<rect x="6" y="6" width="244" height="244" rx="28" fill="url(#b)" stroke="%s" stroke-width="6"/>'
            '<rect x="32" y="32" width="192" height="192" rx="14" fill="%s" stroke="%s" stroke-width="5"/>'
            '<g clip-path="url(#c)" stroke-linejoin="round" stroke-linecap="round">%s</g></svg>') % (b_top, b_bot, extra_defs, O, soil, O, emblem)

def petals(n, r, w, fill, stroke, sw=5, phase=0, cx=128, cy=128, r_in=10):
    s = ''
    for i in range(n):
        a = phase + 360.0 * i / n
        s += ('<path d="M%g %g C%g %g %g %g %g %g C%g %g %g %g %g %g Z" transform="rotate(%g %g %g)" fill="%s" stroke="%s" stroke-width="%g"/>'
              % (cx, cy - r_in, cx - w, cy - r_in - (r - r_in) * 0.4, cx - w * 0.6, cy - r + 4, cx, cy - r,
                 cx + w * 0.6, cy - r + 4, cx + w, cy - r_in - (r - r_in) * 0.4, cx, cy - r_in, a, cx, cy, fill, stroke, sw))
    return s

SOIL = '#6b4528'
tiles = {}

# Poppy
tiles['Ember'] = tile('#d9433f', '#a73232', SOIL,
    ''.join('<circle cx="%g" cy="%g" r="40" fill="#d9433f" stroke="#7a1f22" stroke-width="5"/>' % (128 + 26 * dx, 128 + 26 * dy) for dx, dy in ((-1, -1), (1, -1), (-1, 1), (1, 1))) +
    '<circle cx="128" cy="128" r="24" fill="#2a1418" stroke="%s" stroke-width="4"/>' % O)

# Iris: three upright petals and three drooping falls, a fleur-de-lis silhouette unlike the round flowers
iris_up = '<path d="M128 150 C100 112 106 62 128 40 C150 62 156 112 128 150 Z" fill="#5b86e0" stroke="#1d3470" stroke-width="6"/>'
iris_side = ''.join('<path d="M128 150 C%d 138 %d 92 %d 78 C%d 100 %d 128 128 150 Z" fill="#6f9ae6" stroke="#1d3470" stroke-width="6"/>' % (x1, x2, x3, x4, x5)
                    for x1, x2, x3, x4, x5 in ((96, 70, 62, 100, 112), (160, 186, 194, 156, 144)))
iris_fall = ''.join('<path d="M128 150 C%d 150 %d 190 %d 206 C%d 196 %d 176 128 150 Z" fill="#3d68c4" stroke="#1d3470" stroke-width="6"/>' % (x1, x2, x3, x4, x5)
                    for x1, x2, x3, x4, x5 in ((92, 56, 62, 90, 108), (164, 200, 194, 166, 148)))
tiles['Frost'] = tile('#6f9ae6', '#2f56a6', SOIL,
    iris_fall + iris_side + iris_up +
    '<path d="M128 152 C124 170 126 186 128 204" fill="none" stroke="#ffd45a" stroke-width="9" stroke-linecap="round"/>' +
    '<path d="M128 56 C126 92 126 120 128 146" fill="none" stroke="#a9c4ff" stroke-width="5" opacity="0.8"/>' +
    '<circle cx="128" cy="152" r="9" fill="#ffd45a" stroke="%s" stroke-width="3"/>' % O)

# Sunflower
tiles['Gold'] = tile('#f5c95c', '#b87a14', SOIL,
    petals(12, 88, 18, '#f1b92a', '#8a5a10', 5) + '<circle cx="128" cy="128" r="36" fill="#6b4020" stroke="#2a1608" stroke-width="5"/>')

# Fern fiddlehead: one bold spiral + stem
pts = []
for k in range(101):
    t = k / 100.0
    th = t * 2.4 * math.pi
    r = 8 + 52 * t
    pts.append((129 + r * math.cos(th - math.pi / 2), 104 + r * math.sin(th - math.pi / 2)))
ex, ey = pts[-1]
d = 'M%.1f %.1f ' % pts[0] + ' '.join('L%.1f %.1f' % p for p in pts[1:]) + ' Q%.1f %.1f 124 214' % (ex, ey + 40)
tiles['Moss'] = tile('#82c564', '#3a7a2e', SOIL,
    '<path d="%s" fill="none" stroke="%s" stroke-width="26"/><path d="%s" fill="none" stroke="#5aa845" stroke-width="16"/>' % (d, O, d))

# Wildcard: four petals in the four bloom colors
JC = ['#c8403f', '#4f7fd0', '#e8a82e', '#5a9e45']
tiles['Joker'] = tile('#ad88d0', '#5e3d85', SOIL,
    ''.join(petals(1, 84, 30, c, O, 5, ph, r_in=12) for c, ph in zip(JC, (-45, 45, 135, 225))) +
    '<circle cx="128" cy="128" r="18" fill="#fff7e8" stroke="%s" stroke-width="4"/>' % O)

tiles['Empty'] = tile('#9a9080', '#6f6558', '#4a3220', '')

def star(cx, cy, r, fill):
    k = r * 0.3
    return '<path d="M%g %g L%g %g L%g %g L%g %g L%g %g L%g %g L%g %g L%g %g Z" fill="%s" stroke="%s" stroke-width="4"/>' % (
        cx, cy - r, cx + k, cy - k, cx + r, cy, cx + k, cy + k, cx, cy + r, cx - k, cy + k, cx - r, cy, cx - k, cy - k, fill, O)
tiles['Golden'] = tile('#a8863e', '#5a4220', '#7a5a1a', star(128, 128, 62, '#fff1b0') + star(70, 74, 20, '#ffe08a') + star(190, 182, 22, '#ffe08a'))

arms = ''.join('M128 128 L%.1f %.1f ' % (128 + 62 * math.cos(math.radians(60 * k - 90)), 128 + 62 * math.sin(math.radians(60 * k - 90))) for k in range(6))
tiles['Frozen'] = tile('#d7eefb', '#6fa6cf', '#8cc6e6',
    '<path d="%s" fill="none" stroke="%s" stroke-width="22"/><path d="%s" fill="none" stroke="#f4fbff" stroke-width="11"/>' % (arms, O, arms))

def burst(cx, cy, r):
    p = []
    for i in range(16):
        a = math.pi * i / 8 - math.pi / 2
        rr = r if i % 2 == 0 else r * 0.7
        p.append('%.1f %.1f' % (cx + rr * math.cos(a), cy + rr * math.sin(a)))
    return '<path d="M%s Z" fill="#2c5f93" stroke="%s" stroke-width="5"/>' % (' L'.join(p), O)
tiles['Bonus'] = tile('#8fc3ee', '#2f6fa6', '#1f3d5e',
    burst(128, 128, 80) + '<text x="128" y="160" font-family="Arial Black, Arial, sans-serif" font-weight="900" font-size="90" text-anchor="middle" fill="#ffd966" stroke="%s" stroke-width="10" paint-order="stroke">×2</text>' % O)

def vine(x0, y0, x1, y1, amp, ph):
    n = 60
    dx, dy = x1 - x0, y1 - y0
    L = math.hypot(dx, dy); nx, ny = -dy / L, dx / L
    pts = [(x0 + dx * t / n + nx * amp * math.sin(t / n * 2.5 * math.pi + ph), y0 + dy * t / n + ny * amp * math.sin(t / n * 2.5 * math.pi + ph)) for t in range(n + 1)]
    return 'M%.1f %.1f ' % pts[0] + ' '.join('L%.1f %.1f' % p for p in pts[1:]), pts
def thorns(pts):
    s = ''
    for k in range(8, len(pts) - 4, 12):
        (x, y), (px, py) = pts[k], pts[k - 1]
        tx, ty = x - px, y - py; tl = math.hypot(tx, ty) or 1; tx, ty = tx / tl, ty / tl
        side = 1 if (k // 12) % 2 else -1
        bx, by = -ty * side, tx * side
        s += '<path d="M%.1f %.1f L%.1f %.1f L%.1f %.1f Z" fill="#2c3a1f" stroke="%s" stroke-width="3"/>' % (x - tx * 6, y - ty * 6, x + bx * 20, y + by * 20, x + tx * 6, y + ty * 6, O)
    return s
vd = ''
for a in [(24, 50, 232, 206, 16, 0.3), (232, 56, 24, 200, 16, 1.9)]:
    d, pts = vine(*a)
    vd += thorns(pts) + '<path d="%s" fill="none" stroke="%s" stroke-width="24"/><path d="%s" fill="none" stroke="#4d6a35" stroke-width="14"/>' % (d, O, d)
tiles['Locked'] = tile('#7a6650', '#443626', '#2e2118', vd)

os.makedirs('%s/Tiles' % RES, exist_ok=True)
for name, svg in tiles.items():
    open(name + '.svg', 'w').write(svg)
    open('_t.html', 'w').write('<html><body style="margin:0;background:transparent">%s</body></html>' % svg)
    subprocess.run([CHROME, '--headless', '--no-sandbox', '--disable-gpu', '--hide-scrollbars', '--default-background-color=00000000',
                    '--window-size=256,556', '--screenshot=_t.png', 'file://' + os.path.abspath('_t.html')], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    subprocess.run(['convert', '_t.png', '-crop', '256x256+0+0', '+repage', name + '_256.png'])
    subprocess.run(['convert', name + '_256.png', '-resize', '52x52', name + '_52.png'])
    print('wrote', name)
os.remove('_t.html'); os.remove('_t.png')
