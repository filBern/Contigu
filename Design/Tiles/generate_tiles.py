#!/usr/bin/env python3
"""Generates the Frost/Gold/Moss/Joker/Empty tile SVGs from the same template as Ember.svg
(thin garden-bed border with furrows, seedlings and corner stakes, a tilled-soil inset, a big emblem).
Run from this folder: python3 generate_tiles.py   (then render with Chromium, see README in the commit message)."""
import math

def frame(p, emblem, leaves=True, sprouts=True, extra_border=""):
    out = []
    out.append('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256">')
    out.append('''  <defs>
    <linearGradient id="clay" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="%(b_top)s"/><stop offset="1" stop-color="%(b_bot)s"/></linearGradient>
    %(defs)s
    <clipPath id="inset"><rect x="34" y="34" width="188" height="188" rx="12"/></clipPath>
  </defs>''' % dict(b_top=p['b_top'], b_bot=p['b_bot'], defs=p.get('defs', '')))
    out.append('  <rect x="6" y="6" width="244" height="244" rx="28" fill="url(#clay)" stroke="#13212e" stroke-width="6"/>')
    out.append('''  <g stroke="%(furrow)s" stroke-width="2.6" stroke-linecap="round" opacity="0.75">
    <path d="M40 15H216M40 25H216"/><path d="M40 231H216M40 241H216"/>
    <path d="M15 40V216M25 40V216"/><path d="M231 40V216M241 40V216"/>
  </g>
  <path d="M34 11h188" stroke="%(hi)s" stroke-width="2.5" stroke-linecap="round" opacity="0.55"/>''' % dict(furrow=p['furrow'], hi=p['hi']))
    out.append(extra_border)
    if sprouts:
        out.append('''  <g stroke="#13212e" stroke-width="2" stroke-linejoin="round" stroke-linecap="round">
    <g id="sprout"><path d="M0 0v-6" fill="none"/>
      <path d="M0 -5c-8 -1 -10 -8 -9 -11c6 0 10 4 9 11z" fill="%(s1)s"/>
      <path d="M0 -5c8 -1 10 -8 9 -11c-6 0 -10 4 -9 11z" fill="%(s2)s"/></g>
  </g>
  <g stroke="#13212e" stroke-width="2" stroke-linejoin="round" stroke-linecap="round">''' % dict(s1=p['s1'], s2=p['s2']))
        for x in (80, 128, 176):
            out.append('    <use href="#sprout" transform="translate(%d 17) rotate(180) scale(0.75)"/>' % x)
            out.append('    <use href="#sprout" transform="translate(%d 239) scale(0.75)"/>' % x)
            out.append('    <use href="#sprout" transform="translate(17 %d) rotate(90) scale(0.75)"/>' % x)
            out.append('    <use href="#sprout" transform="translate(239 %d) rotate(-90) scale(0.75)"/>' % x)
        out.append('  </g>')
    out.append('''  <g fill="%(wood)s" stroke="#13212e" stroke-width="3" stroke-linejoin="round">
    <rect x="12" y="12" width="26" height="26" rx="6"/><rect x="218" y="12" width="26" height="26" rx="6"/>
    <rect x="12" y="218" width="26" height="26" rx="6"/><rect x="218" y="218" width="26" height="26" rx="6"/>
  </g>
  <g stroke="%(grain)s" stroke-width="2" stroke-linecap="round" opacity="0.8">
    <path d="M19 20h12M19 26h12M19 32h8"/><path d="M225 20h12M225 26h12M225 32h8"/>
    <path d="M19 226h12M19 232h12M19 238h8"/><path d="M225 226h12M225 232h12M225 238h8"/>
  </g>''' % dict(wood=p.get('wood', '#c58a4a'), grain=p.get('grain', '#7a4a22')))
    out.append('''  <rect x="32" y="32" width="192" height="192" rx="14" fill="%(soil)s" stroke="#13212e" stroke-width="5"/>
  <g clip-path="url(#inset)">
    <g stroke="%(soil_d)s" stroke-width="3" stroke-linecap="round" opacity="0.8"><path d="M40 62H216M40 96H216M40 160H216M40 194H216"/></g>
    <g fill="%(soil_l)s" opacity="0.8"><circle cx="58" cy="108" r="2.5"/><circle cx="196" cy="84" r="2.5"/><circle cx="70" cy="206" r="3"/><circle cx="190" cy="150" r="2.5"/><circle cx="48" cy="150" r="2"/><circle cx="208" cy="206" r="2.5"/></g>
  </g>''' % dict(soil=p.get('soil', '#6b4528'), soil_d=p.get('soil_d', '#53331b'), soil_l=p.get('soil_l', '#8a6038')))
    out.append('  <g clip-path="url(#inset)">')
    if leaves:
        out.append('''    <g fill="%(leaf)s" stroke="#13212e" stroke-width="3" stroke-linejoin="round">
      <path d="M40 40c30 2 44 16 46 46c-30 -2 -44 -16 -46 -46z"/><path d="M216 40c-30 2 -44 16 -46 46c30 -2 44 -16 46 -46z"/>
      <path d="M40 216c30 -2 44 -16 46 -46c-30 2 -44 16 -46 46z"/><path d="M216 216c-30 -2 -44 -16 -46 -46c30 2 44 16 46 46z"/>
    </g>
    <g stroke="%(vein)s" stroke-width="2" stroke-linecap="round" fill="none"><path d="M44 44L76 76M212 44L180 76M44 212L76 180M212 212L180 180"/></g>''' % dict(leaf=p.get('leaf', '#5a9e45'), vein=p.get('vein', '#2f5a28')))
    out.append(emblem)
    out.append('  </g>\n</svg>\n')
    return '\n'.join(out)

def petals(n, cx, cy, r_in, r_out, w, fill, stroke, sw=2.5, phase=0.0):
    """n pointed petals radiating from the center, drawn as teardrop paths."""
    s = []
    for i in range(n):
        a = phase + 360.0 * i / n
        s.append('<path d="M%.1f %.1f C %.1f %.1f, %.1f %.1f, %.1f %.1f C %.1f %.1f, %.1f %.1f, %.1f %.1f Z" transform="rotate(%.1f %.1f %.1f)" fill="%s" stroke="%s" stroke-width="%.1f" stroke-linejoin="round"/>' % (
            cx, cy - r_in, cx - w, cy - r_in - (r_out - r_in) * 0.45, cx - w * 0.55, cy - r_out + 6, cx, cy - r_out,
            cx + w * 0.55, cy - r_out + 6, cx + w, cy - r_in - (r_out - r_in) * 0.45, cx, cy - r_in, a, cx, cy, fill, stroke, sw))
    return '\n      '.join(s)

# ---------------- Frost: cornflower ----------------
frost_defs = '''<radialGradient id="cf" cx="50%" cy="50%" r="55%"><stop offset="0" stop-color="#2b3f9a"/><stop offset="0.6" stop-color="#5b86e0"/><stop offset="1" stop-color="#9bbcf5"/></radialGradient>'''
frost_emblem = '''    <g>
      %s
      %s
      <circle cx="128" cy="128" r="16" fill="#2a2f6e" stroke="#13212e" stroke-width="2.5"/>
      <g fill="#c9d9ff">%s</g>
      <circle cx="123" cy="123" r="4" fill="#8aa3e8" opacity="0.8"/>
    </g>''' % (
    petals(16, 128, 128, 14, 78, 15, 'url(#cf)', '#1d3470', 2.5, 11.25),
    petals(16, 128, 128, 12, 56, 11, '#6f9be8', '#1d3470', 2, 0),
    ''.join('<circle cx="%.1f" cy="%.1f" r="2" />' % (128 + 9 * math.cos(math.radians(t)), 128 + 9 * math.sin(math.radians(t))) for t in range(0, 360, 45)))

# ---------------- Gold: sunflower ----------------
gold_defs = '''<radialGradient id="sf" cx="50%" cy="50%" r="55%"><stop offset="0" stop-color="#e8a82e"/><stop offset="1" stop-color="#ffd84d"/></radialGradient>'''
seeds = []
golden = math.pi * (3 - math.sqrt(5))
for i in range(1, 46):
    r = 3.9 * math.sqrt(i)
    a = i * golden
    seeds.append('<circle cx="%.1f" cy="%.1f" r="2.1"/>' % (128 + r * math.cos(a), 128 + r * math.sin(a)))
gold_emblem = '''    <g>
      %s
      %s
      <circle cx="128" cy="128" r="30" fill="#4a2c14" stroke="#2a1608" stroke-width="3"/>
      <circle cx="128" cy="128" r="25" fill="#6b4020"/>
      <g fill="#c9904a" opacity="0.9">%s</g>
    </g>''' % (
    petals(18, 128, 128, 26, 80, 13, '#f1b92a', '#8a5a10', 2.5, 10),
    petals(18, 128, 128, 24, 66, 11, 'url(#sf)', '#8a5a10', 2.5, 0),
    ''.join(seeds))

# ---------------- Moss: fern fiddlehead ----------------
pts = []
cx, cy = 129, 108
steps = 120
for k in range(steps + 1):
    t = k / steps
    th = t * 2.7 * math.pi
    r = 10 + 60 * t
    pts.append((cx + r * math.cos(th - math.pi / 2 + math.pi * 0.0), cy + r * math.sin(th - math.pi / 2)))
# spiral goes from center outward; outer end at angle 2.7pi - pi/2 -> continue as a stem down to the bottom
ex, ey = pts[-1]
path = 'M%.1f %.1f ' % pts[0] + ' '.join('L%.1f %.1f' % p for p in pts[1:]) + ' Q %.1f %.1f %.1f %.1f' % (ex, ey + 40, 126, 210)
leaflets = []
for k in range(30, steps, 9):
    t = k / steps
    x, y = pts[k]
    px, py = pts[k - 1]
    dx, dy = x - px, y - py
    ang = math.degrees(math.atan2(dy, dx))
    size = 7 + 11 * t
    for side in (-1, 1):
        leaflets.append('<ellipse cx="0" cy="0" rx="%.1f" ry="%.1f" transform="translate(%.1f %.1f) rotate(%.1f) translate(0 %.1f)" fill="#6fb04f" stroke="#1f4a1c" stroke-width="2"/>' % (size * 0.45, size, x, y, ang + side * 0 , side * size * 0.9))
moss_emblem = '''    <g stroke-linecap="round" stroke-linejoin="round">
      <path d="%s" fill="none" stroke="#1f4a1c" stroke-width="19"/>
      <path d="%s" fill="none" stroke="#4f9440" stroke-width="12"/>
      %s
    </g>''' % (path, path, '\n      '.join(leaflets))

# ---------------- Joker: lavender ----------------
lav_defs = '''<radialGradient id="lv" cx="50%" cy="40%" r="60%"><stop offset="0" stop-color="#c9a8ee"/><stop offset="1" stop-color="#7a4fb0"/></radialGradient>'''
buds = []
for i, y in enumerate(range(62, 160, 14)):
    t = i / 7.0
    s = 1.0 - 0.45 * (1 - min(1, t * 1.0)) if False else (0.65 + 0.35 * min(1, i / 4.0))
    for side in (-1, 1):
        buds.append('<ellipse cx="0" cy="0" rx="%.1f" ry="%.1f" transform="translate(%.1f %d) rotate(%d)" fill="url(#lv)" stroke="#3d2466" stroke-width="2.2"/>' % (8 * s, 14 * s, 128 + side * 11 * s, y + 6, side * 28))
buds.append('<ellipse cx="128" cy="56" rx="7" ry="12" fill="url(#lv)" stroke="#3d2466" stroke-width="2.2"/>')
joker_emblem = '''    <g stroke-linecap="round" stroke-linejoin="round">
      <path d="M128 214 C 126 180, 130 140, 128 70" fill="none" stroke="#13212e" stroke-width="9"/>
      <path d="M128 214 C 126 180, 130 140, 128 70" fill="none" stroke="#4f9440" stroke-width="5"/>
      <path d="M126 196 C 96 180, 80 150, 76 130 C 100 140, 120 160, 126 196Z" fill="#5a9e45" stroke="#13212e" stroke-width="2.5"/>
      <path d="M130 196 C 160 180, 176 150, 180 130 C 156 140, 136 160, 130 196Z" fill="#5a9e45" stroke="#13212e" stroke-width="2.5"/>
      %s
    </g>''' % '\n      '.join(buds)
fireflies = ''.join('<circle cx="%d" cy="%d" r="2.6" fill="#ffd966" stroke="#13212e" stroke-width="1"/>' % xy for xy in
                    [(56, 20), (104, 20), (152, 20), (200, 20), (56, 236), (104, 236), (152, 236), (200, 236), (20, 56), (20, 104), (20, 152), (20, 200), (236, 56), (236, 104), (236, 152), (236, 200)])

tiles = {
    'Frost': (dict(b_top='#6f9ae6', b_bot='#2f56a6', furrow='#2a4a8a', hi='#c4d9fb', s1='#6fb04f', s2='#4f9440', defs=frost_defs, leaf='#5a9e45'), frost_emblem, True, True, ''),
    'Gold': (dict(b_top='#f5c95c', b_bot='#b87a14', furrow='#a8700f', hi='#ffe9a6', s1='#6fb04f', s2='#4f9440', defs=gold_defs, wood='#8f5b2b', grain='#5a3414', leaf='#5a9e45'), gold_emblem, True, True, ''),
    'Moss': (dict(b_top='#82c564', b_bot='#3a7a2e', furrow='#2c5a25', hi='#bfe8a2', s1='#d6e87a', s2='#b5d45a', leaf='#3f7a35', vein='#1f4a1c'), moss_emblem, True, True, ''),
    'Joker': (dict(b_top='#ad88d0', b_bot='#5e3d85', furrow='#4a2f66', hi='#e0cdf5', s1='#6fb04f', s2='#4f9440', defs=lav_defs, leaf='#5a9e45'), joker_emblem, True, True, '<g>%s</g>' % fireflies),
}
empty_p = dict(b_top='#9a9080', b_bot='#6f6558', furrow='#5a5044', hi='#cfc6b6', s1='#8a8a5a', s2='#6f7a4a', soil='#4a3220', soil_d='#392414', soil_l='#6a4a2c')
pebbles = '<g fill="#8a8072" stroke="#13212e" stroke-width="2"><ellipse cx="92" cy="110" rx="9" ry="6"/><ellipse cx="170" cy="152" rx="8" ry="5.5"/><ellipse cx="120" cy="186" rx="6" ry="4"/></g><g stroke="#2f241a" stroke-width="3" stroke-linecap="round" opacity="0.7"><path d="M44 128H212M44 228H212"/></g>'
tiles['Empty'] = (empty_p, '    ' + pebbles, False, False, '')

for name, (p, emb, leaves, sprouts, extra) in tiles.items():
    open('%s.svg' % name, 'w').write(frame(p, emb, leaves, sprouts, extra))
    print('wrote', name)
