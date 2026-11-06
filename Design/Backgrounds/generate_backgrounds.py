#!/usr/bin/env python3
"""One full-screen scenery per challenge (1600x900), drawn tone-on-tone so the board and HUD stay readable over it.
Writes <Name>.svg + <Name>.png here and the PNG into Assets/Resources/Backgrounds. Run from this folder: python3 generate_backgrounds.py
Names match ChallengeCatalog ids: Spring (Classic), Winter (Marathon), Summer, Autumn, Orchard, Greenhouse."""
import math, os, random, re, subprocess, uuid

W, H = 1600, 900
CHROME = '/opt/pw-browsers/chromium-1194/chrome-linux/chrome'
OUT = '../../Assets/Resources/Backgrounds'

def svg(body, defs=''):
    return ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 %d %d" width="%d" height="%d"><defs>%s</defs>%s</svg>' % (W, H, W, H, defs, body))

def sky(top, bottom, mid=None):
    stops = '<stop offset="0" stop-color="%s"/>' % top
    if mid:
        stops += '<stop offset="0.6" stop-color="%s"/>' % mid
    stops += '<stop offset="1" stop-color="%s"/>' % bottom
    return '<linearGradient id="sky" x1="0" y1="0" x2="0" y2="1">%s</linearGradient>' % stops

def stars(n, seed, col='#ffffff', maxy=520, op=0.6):
    r = random.Random(seed)
    return ''.join('<circle cx="%d" cy="%d" r="%.1f" fill="%s" opacity="%.2f"/>' % (r.randint(0, W), r.randint(0, maxy), r.uniform(0.8, 2.2), col, r.uniform(0.2, op)) for _ in range(n))

def hills(color, base, amp, seed, wl=420, op=1.0):
    r = random.Random(seed)
    ph = [r.uniform(0, 6.28) for _ in range(3)]
    pts = []
    for x in range(0, W + 41, 40):
        y = base - amp * (0.55 * math.sin(x / wl + ph[0]) + 0.3 * math.sin(x / (wl * 0.47) + ph[1]) + 0.15 * math.sin(x / (wl * 0.23) + ph[2]))
        pts.append('%d,%d' % (x, y))
    return '<path d="M0,%d L%s L%d,%d Z" fill="%s" opacity="%.2f"/>' % (H, ' L'.join(pts), W, H, color, op)

def pine(x, base, h, col, snow=None):
    s = '<rect x="%g" y="%g" width="%g" height="%g" fill="%s"/>' % (x - h * 0.03, base - h * 0.12, h * 0.06, h * 0.14, col)
    for i in range(4):
        w = h * (0.34 - i * 0.07)
        y = base - h * (0.12 + i * 0.22)
        s += '<path d="M%g,%g L%g,%g L%g,%g Z" fill="%s"/>' % (x - w, y, x, y - h * 0.34, x + w, y, col)
        if snow:
            s += '<path d="M%g,%g L%g,%g L%g,%g Z" fill="%s" opacity="0.55"/>' % (x - w * 0.55, y - h * 0.15, x, y - h * 0.34, x + w * 0.55, y - h * 0.15, snow)
    return s

def branch(x, y, ang, length, depth, col, w, r):
    if depth == 0 or length < 8:
        return ''
    x2, y2 = x + length * math.cos(ang), y - length * math.sin(ang)
    s = '<path d="M%g,%g L%g,%g" stroke="%s" stroke-width="%g" stroke-linecap="round"/>' % (x, y, x2, y2, col, w)
    s += branch(x2, y2, ang + r.uniform(0.3, 0.6), length * r.uniform(0.66, 0.78), depth - 1, col, w * 0.68, r)
    s += branch(x2, y2, ang - r.uniform(0.3, 0.6), length * r.uniform(0.66, 0.78), depth - 1, col, w * 0.68, r)
    return s

def bare_tree(x, base, h, col, seed):
    r = random.Random(seed)
    return '<g>%s</g>' % branch(x, base, math.pi / 2, h * 0.34, 7, col, h * 0.045, r)

def apple_tree(x, base, h, trunk, crown, fruit, seed):
    r = random.Random(seed)
    s = '<path d="M%g,%g L%g,%g L%g,%g L%g,%g Z" fill="%s"/>' % (x - h * 0.05, base, x - h * 0.03, base - h * 0.4, x + h * 0.03, base - h * 0.4, x + h * 0.05, base, trunk)
    cy = base - h * 0.62
    for dx, dy, rr in ((-0.22, 0.04, 0.26), (0.22, 0.04, 0.26), (0, -0.12, 0.3), (-0.1, 0.12, 0.24), (0.12, 0.12, 0.24)):
        s += '<circle cx="%g" cy="%g" r="%g" fill="%s"/>' % (x + dx * h, cy + dy * h, rr * h, crown)
    for _ in range(9):
        a = r.uniform(0, 6.28); d = r.uniform(0.05, 0.36) * h
        s += '<circle cx="%g" cy="%g" r="%g" fill="%s"/>' % (x + d * math.cos(a), cy + d * math.sin(a) * 0.8, h * 0.028, fruit)
    return s

def wheat(x, base, h, col, seed):
    r = random.Random(seed)
    s = ''
    for i in range(7):
        dx = (i - 3) * h * 0.05
        lean = r.uniform(-0.12, 0.12) * h
        s += '<path d="M%g,%g Q%g,%g %g,%g" stroke="%s" stroke-width="%g" fill="none" stroke-linecap="round"/>' % (x + dx, base, x + dx + lean * 0.5, base - h * 0.5, x + dx + lean, base - h, col, h * 0.018)
        for k in range(5):
            yy = base - h * (0.62 + k * 0.07)
            s += '<ellipse cx="%g" cy="%g" rx="%g" ry="%g" fill="%s" transform="rotate(%d %g %g)"/>' % (x + dx + lean * (0.62 + k * 0.07), yy, h * 0.012, h * 0.035, col, 20 if k % 2 else -20, x + dx + lean, yy)
    return s

def sunflower(x, base, h, stem, petal, centre):
    s = '<path d="M%g,%g Q%g,%g %g,%g" stroke="%s" stroke-width="%g" fill="none"/>' % (x, base, x + h * 0.06, base - h * 0.5, x, base - h, stem, h * 0.035)
    s += '<ellipse cx="%g" cy="%g" rx="%g" ry="%g" fill="%s" transform="rotate(-30 %g %g)"/>' % (x - h * 0.14, base - h * 0.35, h * 0.14, h * 0.05, stem, x, base)
    for i in range(12):
        a = i * 30
        s += '<ellipse cx="%g" cy="%g" rx="%g" ry="%g" fill="%s" transform="rotate(%d %g %g)"/>' % (x, base - h - h * 0.13, h * 0.05, h * 0.12, petal, a, x, base - h)
    s += '<circle cx="%g" cy="%g" r="%g" fill="%s"/>' % (x, base - h, h * 0.1, centre)
    return s

def pumpkin(x, base, r, col, rib):
    s = ''
    for dx in (-0.55, 0.0, 0.55):
        s += '<ellipse cx="%g" cy="%g" rx="%g" ry="%g" fill="%s"/>' % (x + dx * r, base - r * 0.8, r * 0.62, r * 0.8, col)
    s += '<path d="M%g,%g L%g,%g" stroke="%s" stroke-width="%g"/>' % (x, base - r * 1.5, x + r * 0.1, base - r * 1.9, rib, r * 0.14)
    return s

def glow(cx, cy, r, color, op, gid):
    return ('<radialGradient id="%s"><stop offset="0" stop-color="%s" stop-opacity="%.2f"/><stop offset="1" stop-color="%s" stop-opacity="0"/></radialGradient>' % (gid, color, op, color),
            '<circle cx="%g" cy="%g" r="%g" fill="url(#%s)"/>' % (cx, cy, r, gid))

S = {}

# Spring: the original night garden — soft hills, blossom branches hanging into the corners, a pale moon
d_glow, b_glow = glow(1240, 170, 260, '#cfe6ff', 0.5, 'moon')
S['Spring'] = svg(
    '<rect width="%d" height="%d" fill="url(#sky)"/>' % (W, H) + stars(70, 1) + b_glow +
    '<circle cx="1240" cy="170" r="46" fill="#e8f2ff" opacity="0.8"/>' +
    hills('#163a3e', 720, 70, 11, 380) + hills('#10302f', 800, 60, 12, 300) + hills('#0b2423', 880, 40, 13, 220) +
    '<g stroke="#2a4a3a" fill="none" stroke-linecap="round">' + branch(-20, 120, -0.35, 230, 5, '#2a4a3a', 12, random.Random(5)) + '</g>' +
    ''.join('<circle cx="%d" cy="%d" r="%d" fill="#f4b6c8" opacity="0.5"/>' % (x, y, r) for x, y, r in ((190, 120, 12), (260, 200, 10), (120, 230, 9), (330, 150, 8), (60, 80, 11), (390, 260, 9))) +
    '<g stroke="#2a4a3a" fill="none" stroke-linecap="round">' + branch(1620, 160, math.pi + 0.35, 210, 5, '#2a4a3a', 11, random.Random(6)) + '</g>' +
    ''.join('<circle cx="%d" cy="%d" r="%d" fill="#f4b6c8" opacity="0.45"/>' % (x, y, r) for x, y in ((1480, 160), (1400, 240), (1540, 260)) for r in (10,)),
    sky('#0a1d33', '#16494a', '#12304a') + d_glow)

# Winter: frozen blue night, aurora ribbons, snowy hills and pines
d_moon, b_moon = glow(300, 150, 240, '#dff1ff', 0.55, 'wmoon')
aur = ''.join('<path d="M-50,%d C300,%d 600,%d 900,%d S1500,%d 1700,%d" stroke="%s" stroke-width="%d" fill="none" opacity="%.2f" filter="url(#blur)"/>' % (y, y - 90, y + 70, y - 40, y + 50, y - 30, c, w, o)
              for y, c, w, o in ((220, '#4fe0c0', 70, 0.22), (290, '#6fa8ff', 56, 0.18), (170, '#9a7aff', 44, 0.14)))
rp = random.Random(7)
S['Winter'] = svg(
    '<rect width="%d" height="%d" fill="url(#sky)"/>' % (W, H) + stars(110, 2, '#e8f4ff', 430, 0.8) + aur + b_moon +
    '<circle cx="300" cy="150" r="40" fill="#f2f9ff" opacity="0.85"/>' +
    hills('#27506c', 700, 80, 21, 460) + hills('#1e425a', 790, 70, 22, 330) +
    ''.join(pine(x, 800 + rp.randint(-20, 20), rp.randint(160, 280), '#142e40', '#c8e4f8') for x in range(60, W, 190)) +
    hills('#d4e8f6', 870, 36, 23, 260, 0.9) + hills('#a8c8e0', 905, 30, 24, 180, 0.9),
    sky('#07162a', '#2c5878', '#12304f') + '<filter id="blur"><feGaussianBlur stdDeviation="22"/></filter>' + d_moon)

# Summer: a hot dusk — low sun, wheat and sunflower silhouettes
d_sun, b_sun = glow(800, 640, 560, '#ffb347', 0.75, 'sun')
rs = random.Random(9)
S['Summer'] = svg(
    '<rect width="%d" height="%d" fill="url(#sky)"/>' % (W, H) + b_sun +
    '<circle cx="800" cy="640" r="110" fill="#ffd27a" opacity="0.9"/>' +
    ''.join('<path d="M800,640 L%g,%g" stroke="#ffcf7a" stroke-width="5" opacity="0.14"/>' % (800 + 1500 * math.cos(math.radians(a)), 640 + 1500 * math.sin(math.radians(a))) for a in range(-170, -10, 14)) +
    hills('#6a2d3a', 700, 40, 31, 520) + hills('#4a2030', 770, 50, 32, 360) +
    ''.join(wheat(x, 900, rs.randint(190, 300), '#23121c', x) for x in range(30, W, 90)) +
    ''.join(sunflower(x, 900, rs.randint(300, 420), '#1c1018', '#3a1c22', '#120a10') for x in (140, 520, 1090, 1470)),
    sky('#2a1240', '#e0803a', '#a8466a') + d_sun)

# Autumn: a harvest moon over bare trees and pumpkins in drifting leaves
d_hm, b_hm = glow(1180, 250, 330, '#ffb15a', 0.6, 'hm')
ra = random.Random(13)
S['Autumn'] = svg(
    '<rect width="%d" height="%d" fill="url(#sky)"/>' % (W, H) + stars(40, 3, '#ffe6c0', 360, 0.5) + b_hm +
    '<circle cx="1180" cy="250" r="96" fill="#ffc77a" opacity="0.9"/>' +
    hills('#4a2414', 730, 60, 41, 480) + hills('#33180e', 810, 50, 42, 340) +
    bare_tree(240, 840, 520, '#1d0e08', 51) + bare_tree(1380, 850, 470, '#1d0e08', 52) + bare_tree(760, 790, 300, '#27120a', 53) +
    ''.join(pumpkin(x, 900, ra.randint(34, 52), '#a8501a', '#2a1408') for x in (120, 600, 980, 1500)) +
    hills('#1c0e07', 905, 30, 43, 240),
    sky('#1a0c14', '#7a3a1a', '#4a2018') + d_hm)

# Orchard: a teal twilight over rows of apple trees
d_o, b_o = glow(400, 200, 300, '#bff0d8', 0.35, 'og')
ro = random.Random(17)
S['Orchard'] = svg(
    '<rect width="%d" height="%d" fill="url(#sky)"/>' % (W, H) + stars(50, 4, '#e8fff4', 380, 0.6) + b_o +
    hills('#1f5a4a', 700, 50, 61, 520) +
    ''.join(apple_tree(x, 760, 190, '#2a3a2a', '#1f5a3e', '#7a2a30', x) for x in range(70, W, 260)) +
    hills('#143e32', 800, 36, 62, 300) +
    ''.join(apple_tree(x, 870, 260, '#1a2a1e', '#154634', '#a8383a', x + 1) for x in range(180, W, 360)) +
    hills('#0c2a22', 900, 24, 63, 240),
    sky('#0a2230', '#2a7a66', '#12504a') + d_o)

# Greenhouse: a glass house at night — lattice, hanging pots and grow-light beams
beams = ''.join('<path d="M%d,0 L%d,900 L%d,900 Z" fill="#cffff0" opacity="0.07"/>' % (x, x - 160, x + 160) for x in (300, 820, 1340))
panes = ''.join('<path d="M%d,0 V900" stroke="#9fd8cc" stroke-width="5" opacity="0.28"/>' % x for x in range(0, W + 1, 200))
panes += ''.join('<path d="M0,%d H%d" stroke="#9fd8cc" stroke-width="5" opacity="0.28"/>' % (y, W) for y in range(0, H + 1, 225))
panes += ''.join('<path d="M0,%d C400,%d 1200,%d 1600,%d" stroke="#9fd8cc" stroke-width="3" opacity="0.15" fill="none"/>' % (y, y - 60, y + 60, y) for y in (150, 450, 750))
def hanging_pot(x, y, col):
    s = '<path d="M%g,0 L%g,%g M%g,0 L%g,%g" stroke="#0d2a28" stroke-width="3"/>' % (x - 22, x - 22, y, x + 22, x + 22, y)
    s += '<path d="M%g,%g L%g,%g L%g,%g L%g,%g Z" fill="%s"/>' % (x - 40, y, x + 40, y, x + 28, y + 46, x - 28, y + 46, col)
    for a in (-70, -35, 0, 35, 70):
        s += '<path d="M%g,%g Q%g,%g %g,%g" stroke="#1f6a4a" stroke-width="10" fill="none" stroke-linecap="round"/>' % (x, y, x + a * 0.8, y + 60, x + a * 1.2, y + 120 + abs(a))
    return s
S['Greenhouse'] = svg(
    '<rect width="%d" height="%d" fill="url(#sky)"/>' % (W, H) + beams + panes +
    hanging_pot(220, 90, '#6b3a24') + hanging_pot(760, 60, '#6b3a24') + hanging_pot(1300, 110, '#6b3a24') +
    '<path d="M0,820 H1600 V900 H0 Z" fill="#0a1f22"/><path d="M0,830 H1600" stroke="#2a5a5a" stroke-width="6"/>' +
    ''.join('<g><path d="M%d,820 L%d,770 L%d,820 Z" fill="#12403a"/><path d="M%d,820 L%d,740 L%d,820 Z" fill="#17503f"/></g>' % (x, x + 36, x + 78, x + 10, x + 40, x + 70) for x in range(40, W, 180)),
    sky('#0a2630', '#1f5a5a', '#144a50'))

if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    meta_src = '../../Assets/Resources/Title/LastBloom.png.meta'
    for name, body in S.items():
        open(name + '.svg', 'w').write(body)
        open('/tmp/_bg.html', 'w').write('<html><body style="margin:0;background:#000">%s</body></html>' % body)
        subprocess.run([CHROME, '--headless', '--no-sandbox', '--disable-gpu', '--hide-scrollbars', '--window-size=%d,%d' % (W, H + 120), '--screenshot=/tmp/_bg.png', 'file:///tmp/_bg.html'], check=True, capture_output=True)
        png = '%s/%s.png' % (OUT, name)
        subprocess.run(['convert', '/tmp/_bg.png', '-crop', '%dx%d+0+0' % (W, H), '+repage', '-resize', '1280x720', png], check=True)
        if not os.path.exists(png + '.meta'):
            m = re.sub(r'^guid: \w+', 'guid: ' + uuid.uuid4().hex, open(meta_src).read(), count=1, flags=re.M)
            open(png + '.meta', 'w').write(m)
        print('wrote', name)
    if not os.path.exists(OUT + '.meta'):
        m = re.sub(r'^guid: \w+', 'guid: ' + uuid.uuid4().hex, open('../../Assets/Resources/Tiles.meta').read(), count=1, flags=re.M)
        open(OUT + '.meta', 'w').write(m)
