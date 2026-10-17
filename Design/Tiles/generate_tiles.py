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
        lc = p.get('leaf_colors') or [p.get('leaf', '#5a9e45')] * 4
        out.append('''    <g stroke="#13212e" stroke-width="3" stroke-linejoin="round">
      <path d="M40 40c30 2 44 16 46 46c-30 -2 -44 -16 -46 -46z" fill="%s"/><path d="M216 40c-30 2 -44 16 -46 46c30 -2 44 -16 46 -46z" fill="%s"/>
      <path d="M40 216c30 -2 44 -16 46 -46c-30 2 -44 16 -46 46z" fill="%s"/><path d="M216 216c-30 -2 -44 -16 -46 -46c30 2 44 16 46 46z" fill="%s"/>
    </g>''' % tuple(lc))
        out.append('''    <g stroke="%(vein)s" stroke-width="2" stroke-linecap="round" fill="none"><path d="M44 44L76 76M212 44L180 76M44 212L76 180M212 212L180 180"/></g>''' % dict(vein=p.get('vein', '#2f5a28')))
    if False:
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

# ---------------- Joker: the wildcard — a four-petal flower in the four bloom colors, echoed by four colored corner leaves ----------------
JOKER_COLORS = ['#c8403f', '#4f7fd0', '#e8a82e', '#5a9e45']  # Poppy, Bluebell, Sunflower, Fern
joker4_emblem = ('    <g>\n      %s\n'
                 '      <circle cx="128" cy="128" r="17" fill="#fff7e8" stroke="#13212e" stroke-width="3.5"/>\n'
                 '      <path d="M128 114 L131.5 124.5 L142 128 L131.5 131.5 L128 142 L124.5 131.5 L114 128 L124.5 124.5 Z" fill="#8c63b0" stroke="#13212e" stroke-width="1.5"/>\n'
                 '    </g>') % ''.join(petals(1, 128, 128, 14, 78, 27, c, '#13212e', 3.5, ph) for c, ph in zip(JOKER_COLORS, (-45, 45, 135, 225)))

tiles = {
    'Frost': (dict(b_top='#6f9ae6', b_bot='#2f56a6', furrow='#2a4a8a', hi='#c4d9fb', s1='#6fb04f', s2='#4f9440', defs=frost_defs, leaf='#5a9e45'), frost_emblem, True, True, ''),
    'Gold': (dict(b_top='#f5c95c', b_bot='#b87a14', furrow='#a8700f', hi='#ffe9a6', s1='#6fb04f', s2='#4f9440', defs=gold_defs, wood='#8f5b2b', grain='#5a3414', leaf='#5a9e45'), gold_emblem, True, True, ''),
    'Moss': (dict(b_top='#82c564', b_bot='#3a7a2e', furrow='#2c5a25', hi='#bfe8a2', s1='#d6e87a', s2='#b5d45a', leaf='#3f7a35', vein='#1f4a1c'), moss_emblem, True, True, ''),
    'Joker': (dict(b_top='#ad88d0', b_bot='#5e3d85', furrow='#4a2f66', hi='#e0cdf5', s1='#6fb04f', s2='#4f9440', defs=lav_defs, leaf='#5a9e45', leaf_colors=[JOKER_COLORS[0], JOKER_COLORS[1], JOKER_COLORS[3], JOKER_COLORS[2]]), joker4_emblem, True, True, '<g>%s</g>' % fireflies),
}
empty_p = dict(b_top='#9a9080', b_bot='#6f6558', furrow='#5a5044', hi='#cfc6b6', s1='#8a8a5a', s2='#6f7a4a', soil='#4a3220', soil_d='#392414', soil_l='#6a4a2c')
pebbles = '<g fill="#8a8072" stroke="#13212e" stroke-width="2"><ellipse cx="92" cy="110" rx="9" ry="6"/><ellipse cx="170" cy="152" rx="8" ry="5.5"/><ellipse cx="120" cy="186" rx="6" ry="4"/></g><g stroke="#2f241a" stroke-width="3" stroke-linecap="round" opacity="0.7"><path d="M44 128H212M44 228H212"/></g>'
tiles['Empty'] = (empty_p, '    ' + pebbles, False, False, '')

for name, (p, emb, leaves, sprouts, extra) in tiles.items():
    open('%s.svg' % name, 'w').write(frame(p, emb, leaves, sprouts, extra))
    print('wrote', name)

# ---------------- Locked: a plot choked by thorny bramble (see the Bramble/Deadroot enemies) ----------------
def vine(p0, p1, amp, waves, phase, thorn_every=18, thorn_len=14, seed=0):
    n = 90
    dx, dy = p1[0] - p0[0], p1[1] - p0[1]
    length = math.hypot(dx, dy)
    ux, uy = dx / length, dy / length
    nx, ny = -uy, ux
    pts = []
    for k in range(n + 1):
        t = k / n
        off = amp * math.sin(t * waves * 2 * math.pi + phase)
        pts.append((p0[0] + dx * t + nx * off, p0[1] + dy * t + ny * off))
    d = 'M%.1f %.1f ' % pts[0] + ' '.join('L%.1f %.1f' % p for p in pts[1:])
    thorns = []
    step = max(1, int(n * thorn_every / length))
    for k in range(step, n, step):
        x, y = pts[k]
        px, py = pts[k - 1]
        tx, ty = x - px, y - py
        tl = math.hypot(tx, ty) or 1
        tx, ty = tx / tl, ty / tl
        side = 1 if (k // step + seed) % 2 == 0 else -1
        bx, by = -ty * side, tx * side
        tip = (x + bx * thorn_len + tx * 3, y + by * thorn_len + ty * 3)
        a = (x - tx * 4.5, y - ty * 4.5)
        b = (x + tx * 4.5, y + ty * 4.5)
        thorns.append('<path d="M%.1f %.1f L%.1f %.1f L%.1f %.1f Z"/>' % (a[0], a[1], tip[0], tip[1], b[0], b[1]))
    return d, ''.join(thorns)

vines = [
    vine((8, 40), (248, 216), 11, 3.2, 0.3, seed=0),
    vine((248, 44), (8, 212), 11, 3.0, 1.9, seed=1),
    vine((8, 132), (248, 124), 9, 2.6, 4.0, seed=0),
]
locked_defs = '<clipPath id="tile"><rect x="6" y="6" width="244" height="244" rx="28"/></clipPath>'
vine_svg = '  <g clip-path="url(#tile)">\n'
for d, th in vines:
    vine_svg += '    <path d="%s" fill="none" stroke="#13212e" stroke-width="16" stroke-linecap="round" stroke-linejoin="round"/>\n' % d
for d, th in vines:
    vine_svg += '    <g fill="#2c3a1f" stroke="#13212e" stroke-width="2" stroke-linejoin="round">%s</g>\n' % th
    vine_svg += '    <path d="%s" fill="none" stroke="#4d6a35" stroke-width="10" stroke-linecap="round" stroke-linejoin="round"/>\n' % d
    vine_svg += '    <path d="%s" fill="none" stroke="#6f8f4c" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" opacity="0.6" transform="translate(-1.5 -1.5)"/>\n' % d
vine_svg += '''    <g fill="#8a6a3a" stroke="#13212e" stroke-width="2.2" stroke-linejoin="round">
      <path d="M96 84c10 -8 22 -6 26 4c-10 8 -22 6 -26 -4z"/>
      <path d="M170 168c10 -8 22 -6 26 4c-10 8 -22 6 -26 -4z"/>
      <path d="M60 176c8 -10 20 -10 26 -2c-8 10 -20 10 -26 2z"/>
      <path d="M176 76c8 -10 20 -10 26 -2c-8 10 -20 10 -26 2z"/>
    </g>
  </g>
'''
locked_p = dict(b_top='#7a6650', b_bot='#443626', furrow='#33281c', hi='#b09a7c', s1='#8a8a5a', s2='#6f7a4a',
                wood='#6a5238', grain='#3a2a18', soil='#2e2118', soil_d='#221810', soil_l='#4a3626', defs=locked_defs)
svg = frame(locked_p, '', leaves=False, sprouts=False)
svg = svg.replace('  </g>\n</svg>\n', '  </g>\n' + vine_svg + '</svg>\n') if False else svg.rsplit('  <g clip-path="url(#inset)">', 1)[0] + vine_svg + '</svg>\n'
open('Locked.svg', 'w').write(svg)
print('wrote Locked')

# ---------------- Golden: an empty plot of golden soil (a golden cell before anything is planted on it) ----------------
def star(cx, cy, r, fill='#fff1b0'):
    k = r * 0.28
    return '<path d="M%g %g L%g %g L%g %g L%g %g L%g %g L%g %g L%g %g L%g %g Z" fill="%s" stroke="#13212e" stroke-width="2.2" stroke-linejoin="round"/>' % (
        cx, cy - r, cx + k, cy - k, cx + r, cy, cx + k, cy + k, cx, cy + r, cx - k, cy + k, cx - r, cy, cx - k, cy - k, fill)
golden_emblem = '    ' + star(128, 128, 38) + star(76, 84, 18, '#ffe08a') + star(184, 90, 14, '#ffe08a') + star(186, 174, 20, '#ffe08a') + star(78, 176, 13, '#fff1b0') + \
    '<g fill="#ffe08a" opacity="0.9"><circle cx="120" cy="64" r="3"/><circle cx="150" cy="190" r="3"/><circle cx="62" cy="128" r="2.5"/><circle cx="198" cy="130" r="2.5"/></g>'
golden_p = dict(b_top='#a8863e', b_bot='#5a4220', furrow='#4a3418', hi='#ffe9a6', wood='#f1c40f', grain='#8a6a10', soil='#7a5a1a', soil_d='#5e440f', soil_l='#ffe08a')
open('Golden.svg', 'w').write(frame(golden_p, golden_emblem, leaves=False, sprouts=False))
print('wrote Golden')

# ---------------- Frozen: an empty plot sealed under ice (Plague/Rotbloom's round-start freeze; only a harvest of its line frees it) ----------------
def snowflake(cx, cy, r):
    arms = []
    for k in range(6):
        a = math.radians(60 * k - 90)
        ex, ey = cx + r * math.cos(a), cy + r * math.sin(a)
        arms.append('M%g %g L%g %g' % (cx, cy, ex, ey))
        for t, bl in ((0.55, 0.32), (0.8, 0.22)):
            bx, by = cx + r * t * math.cos(a), cy + r * t * math.sin(a)
            for s in (-1, 1):
                ba = a + s * math.radians(50)
                arms.append('M%g %g L%g %g' % (bx, by, bx + r * bl * math.cos(ba), by + r * bl * math.sin(ba)))
    d = ' '.join(arms)
    return ('<path d="%s" fill="none" stroke="#13212e" stroke-width="13" stroke-linecap="round" stroke-linejoin="round"/>'
            '<path d="%s" fill="none" stroke="#f4fbff" stroke-width="6" stroke-linecap="round" stroke-linejoin="round"/>') % (d, d)
frozen_emblem = ('    <g opacity="0.9">'
                 '<path d="M52 70 L84 102 L74 118 L104 146 M204 82 L170 108 L182 126 L158 150 M96 204 L112 176 L140 186 L150 214" fill="none" stroke="#13212e" stroke-width="7" stroke-linecap="round" stroke-linejoin="round" opacity="0.55"/>'
                 '<path d="M52 70 L84 102 L74 118 L104 146 M204 82 L170 108 L182 126 L158 150 M96 204 L112 176 L140 186 L150 214" fill="none" stroke="#e8f6ff" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/>'
                 '</g>\n    ' + snowflake(128, 128, 56) + '\n    '
                 '<g fill="#ffffff" stroke="#13212e" stroke-width="2"><path d="M66 52 l5 9 9 5 -9 5 -5 9 -5 -9 -9 -5 9 -5z"/><path d="M196 190 l4 7 7 4 -7 4 -4 7 -4 -7 -7 -4 7 -4z"/><path d="M204 62 l3 5 5 3 -5 3 -3 5 -3 -5 -5 -3 5 -3z"/></g>')
frozen_p = dict(b_top='#d7eefb', b_bot='#6fa6cf', furrow='#4a7fa8', hi='#ffffff', wood='#bcdcf0', grain='#6a9fc2', soil='#8cc6e6', soil_d='#6aa8cf', soil_l='#f1faff')
open('Frozen.svg', 'w').write(frame(frozen_p, frozen_emblem, leaves=False, sprouts=False))
print('wrote Frozen')

# ---------------- Bonus: an empty plot that doubles whatever scores through it (Permanent Bonus Square / Multiplier Zone) ----------------
def burst8(cx, cy, r):
    pts = []
    for i in range(16):
        a = math.pi * i / 8 - math.pi / 2
        rr = r if i % 2 == 0 else r * 0.68
        pts.append('%.1f %.1f' % (cx + rr * math.cos(a), cy + rr * math.sin(a)))
    return '<path d="M%s Z" fill="#2c5f93" stroke="#13212e" stroke-width="4" stroke-linejoin="round"/>' % ' L'.join(pts)
bonus_emblem = ('    <g>' + burst8(128, 128, 78) +
                '<text x="128" y="158" font-family="Arial Black, Arial, sans-serif" font-weight="900" font-size="86" text-anchor="middle" fill="#ffd966" stroke="#13212e" stroke-width="9" paint-order="stroke" stroke-linejoin="round">×2</text>'
                '</g>' + '<g fill="#ffffff" stroke="#13212e" stroke-width="2"><path d="M62 56 l5 9 9 5 -9 5 -5 9 -5 -9 -9 -5 9 -5z"/><path d="M198 196 l4 7 7 4 -7 4 -4 7 -4 -7 -7 -4 7 -4z"/></g>')
bonus_p = dict(b_top='#8fc3ee', b_bot='#2f6fa6', furrow='#265b8a', hi='#d6ecff', wood='#e8a82e', grain='#8a5a10', soil='#1f3d5e', soil_d='#16304c', soil_l='#6fb0e8')
open('Bonus.svg', 'w').write(frame(bonus_p, bonus_emblem, leaves=False, sprouts=False))
print('wrote Bonus')
