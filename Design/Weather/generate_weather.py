#!/usr/bin/env python3
"""The weather icons, drawn from scratch in the garden style: bare pictograms (no tile, no dark outline), soft gradients, a soft shadow under the whole drawing.
Writes svg/<Name>.svg (128 viewBox); render_weather.js (Playwright, NODE_PATH=$(npm root -g)) turns them into Assets/Resources/Icons/Weather/<Name>.png (128 px)."""
import math, os

SKY, CLOUD, CLOUD2 = '#9ed4f5', '#f6f9ff', '#c8d6ec'
SUN, SUN2 = '#ffd54a', '#f5a623'
DROP, DROP2 = '#7fd0f7', '#3b9ee0'
LEAF, LEAF2 = '#8fd25a', '#4f9a3c'
WOOD, WOOD2 = '#c98d4a', '#8a5a2b'
RED, RED2 = '#ef5b4f', '#c93a34'
ICE, ICE2 = '#e8f6ff', '#9ad3f2'

DEFS = ('<defs>'
        '<linearGradient id="cl" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="%s"/><stop offset="1" stop-color="%s"/></linearGradient>'
        '<linearGradient id="dk" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#b3c0d8"/><stop offset="1" stop-color="#7d8aa6"/></linearGradient>'
        '<radialGradient id="su" cx="40%%" cy="35%%" r="75%%"><stop offset="0" stop-color="#fff1a8"/><stop offset="0.55" stop-color="%s"/><stop offset="1" stop-color="%s"/></radialGradient>'
        '<radialGradient id="hot" cx="40%%" cy="35%%" r="75%%"><stop offset="0" stop-color="#ffd27a"/><stop offset="0.55" stop-color="#ff8a3d"/><stop offset="1" stop-color="#d9412a"/></radialGradient>'
        '<linearGradient id="dr" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="%s"/><stop offset="1" stop-color="%s"/></linearGradient>'
        '<linearGradient id="lf" x1="0" y1="1" x2="1" y2="0"><stop offset="0" stop-color="%s"/><stop offset="1" stop-color="%s"/></linearGradient>'
        '<linearGradient id="wd" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="%s"/><stop offset="1" stop-color="%s"/></linearGradient>'
        '<linearGradient id="ic" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#ffffff"/><stop offset="1" stop-color="%s"/></linearGradient>'
        '<filter id="sh" x="-25%%" y="-25%%" width="150%%" height="160%%"><feDropShadow dx="0" dy="3" stdDeviation="2.6" flood-color="#1a0f08" flood-opacity="0.55"/></filter>'
        '</defs>') % (CLOUD, CLOUD2, SUN, SUN2, DROP, DROP2, LEAF2, LEAF, WOOD, WOOD2, ICE2)

def svg(body):
    return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128" width="128" height="128">%s<g filter="url(#sh)">%s</g></svg>\n' % (DEFS, body)

def cloud(cx, cy, s, fill='url(#cl)'):
    """A fluffy cloud about 1.15*s wide, centered on (cx, cy)."""
    return ('<g fill="%s"><circle cx="%g" cy="%g" r="%g"/><circle cx="%g" cy="%g" r="%g"/><circle cx="%g" cy="%g" r="%g"/>'
            '<rect x="%g" y="%g" width="%g" height="%g" rx="%g"/></g>') % (
        fill, cx - 0.30 * s, cy, 0.26 * s, cx - 0.02 * s, cy - 0.12 * s, 0.34 * s, cx + 0.30 * s, cy, 0.25 * s,
        cx - 0.56 * s, cy - 0.02 * s, 1.12 * s, 0.28 * s, 0.14 * s)

def drop(cx, cy, s, fill='url(#dr)'):
    return '<path d="M%g %g C%g %g %g %g %g %g C%g %g %g %g %g %g Z" fill="%s"/>' % (
        cx, cy - s, cx + 0.8 * s, cy - 0.1 * s, cx + 0.75 * s, cy + 0.85 * s, cx, cy + 0.85 * s,
        cx - 0.75 * s, cy + 0.85 * s, cx - 0.8 * s, cy - 0.1 * s, cx, cy - s, fill) + \
        '<ellipse cx="%g" cy="%g" rx="%g" ry="%g" fill="#fff" opacity="0.55"/>' % (cx - 0.32 * s, cy + 0.1 * s, 0.14 * s, 0.26 * s)

def sun(cx, cy, r, fill='url(#su)', ray='#ffbf2e', rays=8, ray_in=1.25, ray_out=1.65, width=None):
    out = ''
    for k in range(rays):
        a = math.radians(360.0 * k / rays)
        out += '<line x1="%g" y1="%g" x2="%g" y2="%g" stroke="%s" stroke-width="%g" stroke-linecap="round"/>' % (
            cx + r * ray_in * math.cos(a), cy + r * ray_in * math.sin(a), cx + r * ray_out * math.cos(a), cy + r * ray_out * math.sin(a), ray, width or r * 0.28)
    return out + '<circle cx="%g" cy="%g" r="%g" fill="%s"/>' % (cx, cy, r, fill) + '<ellipse cx="%g" cy="%g" rx="%g" ry="%g" fill="#fff" opacity="0.4"/>' % (cx - r * 0.3, cy - r * 0.32, r * 0.22, r * 0.13)

def star(cx, cy, r, fill='#fff4c8'):
    pts = []
    for i in range(8):
        a = math.pi * i / 4 - math.pi / 2
        rr = r if i % 2 == 0 else r * 0.36
        pts.append('%.1f %.1f' % (cx + rr * math.cos(a), cy + rr * math.sin(a)))
    return '<path d="M%s Z" fill="%s"/>' % (' L'.join(pts), fill)

def flake(cx, cy, r, fill='url(#ic)', width=None):
    out = ''
    w = width or r * 0.2
    for k in range(6):
        a = math.radians(60 * k - 90)
        ex, ey = cx + r * math.cos(a), cy + r * math.sin(a)
        out += '<line x1="%g" y1="%g" x2="%g" y2="%g" stroke="#ffffff" stroke-width="%g" stroke-linecap="round"/>' % (cx, cy, ex, ey, w)
        for t in (0.55,):
            bx, by = cx + r * t * math.cos(a), cy + r * t * math.sin(a)
            for d in (-35, 35):
                b = a + math.radians(d)
                out += '<line x1="%g" y1="%g" x2="%g" y2="%g" stroke="#ffffff" stroke-width="%g" stroke-linecap="round"/>' % (bx, by, bx + r * 0.3 * math.cos(b), by + r * 0.3 * math.sin(b), w * 0.8)
    return out + '<circle cx="%g" cy="%g" r="%g" fill="#ffffff"/>' % (cx, cy, w * 0.9)

def swirl(path, color='#ffffff', width=7):
    return '<path d="%s" fill="none" stroke="%s" stroke-width="%g" stroke-linecap="round" stroke-linejoin="round"/>' % (path, color, width)

def leaf(cx, cy, length, ang, fill='url(#lf)'):
    return '<g transform="translate(%g %g) rotate(%g)"><path d="M0 0 C%g %g %g %g %g 0 C%g %g %g %g 0 0 Z" fill="%s"/><path d="M2 0 H%g" stroke="#d9f5b8" stroke-width="2" stroke-linecap="round" opacity="0.7"/></g>' % (
        cx, cy, ang, length * 0.25, -length * 0.42, length * 0.75, -length * 0.38, length, length * 0.75, length * 0.38, length * 0.25, length * 0.42, fill, length * 0.8)

I = {}
I['Aurora'] = svg(
    '<path d="M16 88 C12 58 22 38 38 18 C36 42 44 56 42 88 Z" fill="#a898f5"/>'
    '<path d="M38 88 C34 54 48 30 66 10 C66 38 76 52 70 88 Z" fill="#6fe0b4"/>'
    '<path d="M64 88 C62 58 80 38 100 26 C94 46 104 62 98 88 Z" fill="#8f7ff0" opacity="0.92"/>'
    '<path d="M10 104 L64 78 L118 104 Z" fill="#f4f6fb"/><path d="M64 78 L118 104 L86 104 Z" fill="#c7d3e6"/>'
    + star(100, 14, 8) + star(22, 30, 6))
I['Blizzard'] = svg(
    swirl('M12 44 C34 36 46 52 66 44 C84 36 96 28 108 40', '#ffffff', 8) + swirl('M10 70 C32 62 50 80 76 68 C94 60 110 66 116 78', '#ffffff', 8)
    + swirl('M20 96 C40 90 52 104 74 96', '#d6efff', 7) + flake(104, 100, 12) + flake(22, 22, 9)
    + '<circle cx="86" cy="22" r="4" fill="#fff"/><circle cx="54" cy="112" r="3.5" fill="#fff"/><circle cx="116" cy="58" r="3.5" fill="#fff"/>')
I['Clear'] = svg(sun(80, 50, 24) + cloud(52, 88, 76))
I['Compost'] = svg(
    leaf(64, 56, 40, -150) + leaf(64, 56, 40, -30) + '<path d="M64 84 V52" stroke="#4f9a3c" stroke-width="6" stroke-linecap="round"/>'
    '<path d="M24 76 H104 L94 120 H34 Z" fill="url(#wd)"/><rect x="18" y="68" width="92" height="16" rx="7" fill="#d8a066"/><rect x="26" y="68" width="76" height="6" rx="3" fill="#6b4a2e"/>'
    + ''.join('<circle cx="%d" cy="%d" r="4" fill="#ffd54a"/>' % p for p in ((48, 96), (66, 106), (82, 94), (58, 88))))
I['Condensation'] = svg(
    '<rect x="14" y="14" width="100" height="100" rx="16" fill="#cfeaf6" opacity="0.95"/><rect x="14" y="14" width="100" height="100" rx="16" fill="url(#cl)" opacity="0.5"/>'
    '<path d="M64 14 V114 M14 64 H114" stroke="#8ec3da" stroke-width="5"/>' + drop(38, 36, 12) + drop(92, 30, 9) + drop(88, 82, 14) + drop(40, 90, 10) + drop(62, 62, 7))
I['Dew'] = svg(
    leaf(14, 104, 108, -42) + '<path d="M16 104 L100 28" stroke="#d9f5b8" stroke-width="3" stroke-linecap="round" opacity="0.7"/>' + drop(66, 76, 20) + drop(94, 54, 9) + drop(40, 66, 7) + star(104, 18, 7))
I['DiscoLights'] = svg(
    '<path d="M64 8 V28" stroke="#9aa4b8" stroke-width="4" stroke-linecap="round"/>'
    '<path d="M64 60 L16 118 L44 118 Z" fill="#ff7bd0" opacity="0.55"/><path d="M64 60 L52 120 L76 120 Z" fill="#7fd0ff" opacity="0.6"/><path d="M64 60 L112 118 L84 118 Z" fill="#ffd54a" opacity="0.6"/>'
    '<circle cx="64" cy="48" r="26" fill="url(#dk)"/>'
    + ''.join('<rect x="%g" y="%g" width="9" height="9" rx="2" fill="%s" opacity="0.9"/>' % (64 + dx - 4.5, 48 + dy - 4.5, c) for dx, dy, c in
              ((-14, -12, '#ffffff'), (0, -16, '#fff0a8'), (14, -10, '#ffffff'), (-18, 2, '#cfeaff'), (-3, 0, '#ffffff'), (13, 4, '#ffd9f4'), (-10, 15, '#ffffff'), (6, 17, '#cfeaff')))
    + star(24, 40, 7) + star(106, 30, 8))
I['FreezerLeak'] = svg(
    '<rect x="26" y="10" width="64" height="108" rx="10" fill="#eef6fb"/><rect x="26" y="10" width="64" height="108" rx="10" fill="url(#cl)" opacity="0.6"/>'
    '<path d="M26 20 L12 30 V112 L26 118 Z" fill="#a9c6dc"/><rect x="72" y="36" width="5" height="22" rx="2.5" fill="#8fb0c8"/>'
    + flake(84, 84, 26))
I['Frost'] = svg(flake(64, 64, 52, width=9))
I['GoldenHour'] = svg(
    '<clipPath id="upper"><rect x="0" y="0" width="128" height="86"/></clipPath><g clip-path="url(#upper)">' + sun(64, 82, 36, fill='url(#hot)', ray='#ffb23f', ray_in=1.2, ray_out=1.6, width=8) + '</g>'
    + '<rect x="6" y="82" width="116" height="40" rx="8" fill="url(#wd)"/><rect x="6" y="82" width="116" height="7" rx="3.5" fill="#f3c27a" opacity="0.8"/>')
I['GrowLamp'] = svg(
    '<path d="M64 6 V22" stroke="#9aa4b8" stroke-width="4" stroke-linecap="round"/><path d="M36 50 L50 22 H78 L92 50 Z" fill="url(#dk)"/><rect x="34" y="46" width="60" height="9" rx="4.5" fill="#ffe9a8"/>'
    '<path d="M42 56 L12 112 H116 L86 56 Z" fill="#e58bff" opacity="0.4"/>'
    + leaf(64, 118, 26, -150) + leaf(64, 118, 26, -30) + '<path d="M64 120 V96" stroke="#4f9a3c" stroke-width="5" stroke-linecap="round"/>')
I['Heatwave'] = svg(
    sun(64, 48, 24, fill='url(#hot)', ray='#ff8a3d') + swirl('M12 94 C28 86 40 102 56 94 C72 86 84 102 100 94 C108 90 114 92 118 94', '#ffe9c2', 7)
    + swirl('M16 114 C32 106 44 122 60 114 C76 106 88 122 104 114', '#ffcf8a', 6))
I['Hurricane'] = svg(
    '<path d="M16 100 C16 84 36 78 64 78 C92 78 112 84 112 100 C112 112 92 118 64 118 C36 118 16 112 16 100 Z" fill="url(#dk)"/>'
    + swirl('M64 62 C54 62 50 52 58 46 C68 40 80 48 78 60 C76 76 56 80 44 70 C30 58 36 38 56 32 C80 26 100 44 96 66', '#ffffff', 8)
    + '<path d="M96 54 L84 78 H94 L88 100 L110 70 H98 L106 54 Z" fill="#ffd54a"/>')
I['IndianSummer'] = svg(
    sun(54, 52, 26) +
    '<path d="M86 118 V100 M86 100 L70 96 L74 84 L62 78 L76 74 L74 62 L86 70 L98 62 L96 74 L110 78 L98 84 L102 96 Z" fill="#e5532f" stroke="none"/><path d="M86 118 V96" stroke="#b83a22" stroke-width="3.5" stroke-linecap="round"/>')
I['PollenShower'] = svg(
    ''.join('<circle cx="%d" cy="%d" r="%d" fill="#ffd54a"/><circle cx="%d" cy="%d" r="%g" fill="#fff6c8" opacity="0.8"/>' % (x, y, r, x - r * 0.3, y - r * 0.3, r * 0.35)
            for x, y, r in ((30, 22, 8), (62, 12, 9), (96, 24, 8), (46, 46, 6), (84, 50, 7), (18, 56, 5), (108, 62, 5)))
    + ''.join('<ellipse cx="%g" cy="%g" rx="16" ry="12" transform="rotate(%d %g %g)" fill="%s"/>' % (64 + 18 * math.cos(math.radians(a)), 96 + 15 * math.sin(math.radians(a)), a, 64 + 18 * math.cos(math.radians(a)), 96 + 15 * math.sin(math.radians(a)), RED if k % 2 == 0 else RED2)
              for k, a in enumerate((0, 72, 144, 216, 288)))
    + '<circle cx="64" cy="96" r="11" fill="#3a2a1c"/><circle cx="60" cy="92" r="3" fill="#7a5a3a"/>')
I['Rain'] = svg(cloud(64, 52, 94, fill='url(#dk)') + drop(34, 94, 12) + drop(64, 106, 12) + drop(94, 94, 12))
I['RogueCat'] = svg(
    leaf(24, 38, 22, -140) + leaf(104, 34, 22, -40) +
    '<path d="M26 54 L30 16 L54 36 Q64 33 74 36 L98 16 L102 54 Q108 70 100 88 Q88 112 64 112 Q40 112 28 88 Q20 70 26 54 Z" fill="#f4a24a"/>'
    '<path d="M32 28 L44 38 L33 46 Z M96 28 L84 38 L95 46 Z" fill="#ffd1a0"/>'
    '<ellipse cx="46" cy="66" rx="8" ry="10" fill="#3a2a1c"/><ellipse cx="82" cy="66" rx="8" ry="10" fill="#3a2a1c"/><circle cx="48" cy="62" r="3" fill="#fff"/><circle cx="84" cy="62" r="3" fill="#fff"/>'
    '<path d="M58 80 H70 L64 88 Z" fill="#e5575a"/><path d="M64 88 V94 M64 94 Q56 100 50 96 M64 94 Q72 100 78 96" fill="none" stroke="#3a2a1c" stroke-width="3" stroke-linecap="round"/>')
I['Snowfall'] = svg(cloud(64, 50, 94, fill='url(#dk)') + flake(34, 96, 11) + flake(64, 108, 9) + flake(94, 94, 11) + '<circle cx="48" cy="80" r="4" fill="#fff"/><circle cx="80" cy="78" r="3.5" fill="#fff"/>')
I['Sprinkler'] = svg(
    '<rect x="54" y="86" width="20" height="34" rx="6" fill="url(#dk)"/><path d="M64 86 V52" stroke="#9aa4b8" stroke-width="8" stroke-linecap="round"/><rect x="50" y="44" width="28" height="14" rx="7" fill="#7d8aa6"/>'
    '<path d="M64 44 C52 14 20 18 12 56" fill="none" stroke="#7fd0f7" stroke-width="6" stroke-linecap="round"/><path d="M64 44 C76 14 108 18 116 56" fill="none" stroke="#7fd0f7" stroke-width="6" stroke-linecap="round"/>'
    '<path d="M64 44 V12" stroke="#bfe8ff" stroke-width="6" stroke-linecap="round"/>' + drop(12, 66, 8) + drop(116, 66, 8) + drop(64, 8, 7))
I['Sunshine'] = svg(sun(64, 64, 30, rays=10, ray_in=1.3, ray_out=1.85, width=9))
I['Thermostat'] = svg(
    '<rect x="50" y="10" width="28" height="78" rx="14" fill="#f4f7fc"/><circle cx="64" cy="96" r="24" fill="#f4f7fc"/><rect x="58" y="30" width="12" height="62" rx="6" fill="%s"/><circle cx="64" cy="96" r="17" fill="%s"/>' % (RED, RED)
    + '<ellipse cx="58" cy="90" rx="4" ry="7" fill="#fff" opacity="0.5"/>' + ''.join('<rect x="86" y="%d" width="%d" height="5" rx="2.5" fill="#f4f7fc"/>' % (y, w) for y, w in ((22, 18), (38, 12), (54, 18)))
    + star(104, 92, 9, '#ffd54a') + star(22, 92, 7, '#ffd54a'))
I['VentFan'] = svg(
    '<circle cx="64" cy="64" r="52" fill="url(#cl)"/><circle cx="64" cy="64" r="46" fill="none" stroke="#c0d0e6" stroke-width="4"/>'
    + ''.join('<path d="M64 64 C56 40 70 18 90 24 C96 42 84 58 64 64 Z" fill="url(#dr)" transform="rotate(%d 64 64)"/>' % a for a in (0, 120, 240))
    + '<circle cx="64" cy="64" r="9" fill="#7d8aa6"/><circle cx="62" cy="62" r="3" fill="#fff" opacity="0.6"/>')
I['Wind'] = svg(
    swirl('M10 46 C36 34 52 56 78 44 C94 36 106 24 114 36 C120 46 108 54 100 48', '#ffffff', 9)
    + swirl('M10 76 C30 68 46 86 70 76 C90 68 108 78 112 90', '#ffffff', 9) + leaf(34, 108, 30, -20))

os.makedirs('svg', exist_ok=True)
for name, art in I.items():
    open('svg/%s.svg' % name, 'w').write(art)
print(len(I), 'weather icons')
