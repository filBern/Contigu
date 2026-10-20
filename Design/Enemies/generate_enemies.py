#!/usr/bin/env python3
"""Generates the enemy portrait SVGs (same flat vector garden style as Design/Tiles). Output: <EnemyId>.svg next to this script."""
import math

O = '#13212e'  # shared dark outline

def svg(body):
    return '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256">
  <defs>
    <radialGradient id="plate" cx="50%%" cy="40%%" r="70%%"><stop offset="0" stop-color="#5a3f27"/><stop offset="1" stop-color="#2a1c12"/></radialGradient>
    <clipPath id="clip"><rect x="8" y="8" width="240" height="240" rx="44"/></clipPath>
  </defs>
  <rect x="8" y="8" width="240" height="240" rx="44" fill="url(#plate)" stroke="%s" stroke-width="6"/>
  <g clip-path="url(#clip)" stroke-linejoin="round" stroke-linecap="round">
%s
  </g>
  <rect x="8" y="8" width="240" height="240" rx="44" fill="none" stroke="%s" stroke-width="6"/>
</svg>
''' % (O, body, O)

def eye(cx, cy, r=9, color='#ffffff', pupil='#13212e', look=(0, 0), angry=0):
    s = '<circle cx="%g" cy="%g" r="%g" fill="%s" stroke="%s" stroke-width="3"/>' % (cx, cy, r, color, O)
    s += '<circle cx="%g" cy="%g" r="%g" fill="%s"/>' % (cx + look[0], cy + look[1], r * 0.5, pupil)
    if angry:
        s += '<path d="M%g %g L%g %g" stroke="%s" stroke-width="5"/>' % (cx - r - 3, cy - r * 0.9 - angry * 3, cx + r + 3, cy - r * 0.9 + angry * 6, O)
    return s

def thorns(n, cx, cy, r, length, width, fill, rot0=0):
    out = []
    for i in range(n):
        a = rot0 + 360.0 * i / n
        out.append('<path d="M%g %g L%g %g L%g %g Z" transform="rotate(%g %g %g)"/>' % (cx - width, cy - r, cx, cy - r - length, cx + width, cy - r, a, cx, cy))
    return '<g fill="%s" stroke="%s" stroke-width="3">%s</g>' % (fill, O, ''.join(out))

def outlined(d, fill, w=7, ow=5):
    return '<path d="%s" fill="none" stroke="%s" stroke-width="%g"/><path d="%s" fill="none" stroke="%s" stroke-width="%g"/>' % (d, O, w + ow * 2, d, fill, w)

def petal_ring(n, cx, cy, r_in, r_out, w, fills, stroke, sw=3, rot0=0):
    out = []
    for i in range(n):
        a = rot0 + 360.0 * i / n
        out.append('<path d="M%g %g C %g %g, %g %g, %g %g C %g %g, %g %g, %g %g Z" transform="rotate(%g %g %g)" fill="%s" stroke="%s" stroke-width="%g"/>' % (
            cx, cy - r_in, cx - w, cy - r_in - (r_out - r_in) * 0.45, cx - w * 0.6, cy - r_out + 8, cx, cy - r_out,
            cx + w * 0.6, cy - r_out + 8, cx + w, cy - r_in - (r_out - r_in) * 0.45, cx, cy - r_in, a, cx, cy, fills[i % len(fills)], stroke, sw))
    return ''.join(out)

E = {}

# Withered Stalk: a drooping, dying wheat stalk
grains = ''
for i, (x, y) in enumerate([(152, 100), (163, 112), (172, 126), (178, 142), (180, 158)]):
    for side in (-1, 1):
        grains += '<ellipse cx="%g" cy="%g" rx="6" ry="11" transform="rotate(%g %g %g)" fill="#cdb27a" stroke="%s" stroke-width="3"/>' % (x + side * 8, y, side * 25 + 20, x + side * 8, y, O)
E['Basic'] = ('<g transform="translate(-26 -22) scale(1.2)">' + outlined('M118 216 C116 170 124 130 148 94', '#a89060', 9) +
              '<path d="M122 184 C92 172 70 192 62 218 C92 210 112 204 122 184Z" fill="#8a7a4a" stroke="%s" stroke-width="4"/>' % O +
              '<path d="M126 160 C160 158 182 176 190 204 C162 196 142 186 126 160Z" fill="#7a6c40" stroke="%s" stroke-width="4"/>' % O +
              '<path d="M148 94 C170 96 184 120 182 164" fill="none" stroke="%s" stroke-width="12"/><path d="M148 94 C170 96 184 120 182 164" fill="none" stroke="#a89060" stroke-width="5"/>' % O +
              grains + '</g>')

# Bramble: a knot of thorny vines with glowing eyes
ring = 'M56 128 C56 70 200 70 200 128 C200 186 56 186 56 128Z'
cross = 'M70 80 C110 120 150 140 190 180 M190 80 C150 120 110 140 70 180'
E['Locker'] = (thorns(10, 128, 128, 68, 22, 8, '#2c3a1f') + outlined(ring, '#4d6a35', 15, 4) + outlined(cross, '#5a7c3c', 12, 4) +
               '<circle cx="128" cy="128" r="30" fill="#1b2412" stroke="%s" stroke-width="4"/>' % O +
               '<ellipse cx="116" cy="126" rx="8" ry="11" fill="#ffd84d" transform="rotate(-12 116 126)"/><ellipse cx="140" cy="126" rx="8" ry="11" fill="#ffd84d" transform="rotate(12 140 126)"/>' +
               '<ellipse cx="116" cy="128" rx="2.5" ry="7" fill="#13212e"/><ellipse cx="140" cy="128" rx="2.5" ry="7" fill="#13212e"/>')

# Blightspore: a toxic mushroom with drifting spores
spores = ''.join('<circle cx="%g" cy="%g" r="%g" fill="#b8e04a" stroke="%s" stroke-width="2"/>' % (x, y, r, O) for x, y, r in
                 [(54, 70, 6), (84, 48, 4), (200, 60, 7), (176, 40, 4), (214, 108, 5), (44, 112, 4), (200, 200, 5), (52, 196, 5)])
E['Poisoner'] = (spores +
                 '<path d="M104 130 C98 170 96 196 92 214 L164 214 C160 196 158 170 152 130Z" fill="#d8d2a0" stroke="%s" stroke-width="5"/>' % O +
                 '<path d="M44 134 C40 66 216 66 212 134 C190 144 66 144 44 134Z" fill="#7d5aa6" stroke="%s" stroke-width="5"/>' % O +
                 '<g fill="#b8e04a" stroke="%s" stroke-width="3"><circle cx="88" cy="106" r="11"/><circle cx="132" cy="90" r="9"/><circle cx="170" cy="112" r="12"/><circle cx="112" cy="122" r="6"/></g>' % O +
                 '<path d="M60 130 C100 140 156 140 196 130" fill="none" stroke="#4a2f66" stroke-width="4"/>' +
                 eye(114, 168, 8, '#fff7e8', look=(1, 1), angry=1) + eye(142, 168, 8, '#fff7e8', look=(-1, 1), angry=-1))

# Deadroot: a gnarled dead stump with red eyes and thorns
E['HeavyLocker'] = ('<g fill="#2c2218" stroke="%s" stroke-width="3"><path d="M84 96 L58 84 L82 120Z"/><path d="M172 96 L198 84 L174 120Z"/><path d="M80 150 L52 148 L80 176Z"/><path d="M176 150 L204 148 L176 176Z"/></g>' % O +
                    '<path d="M86 64 C76 120 84 168 56 218 L96 214 C102 190 112 176 128 176 C144 176 154 190 160 214 L200 218 C172 168 180 120 170 64 C150 52 106 52 86 64Z" fill="#4a3424" stroke="%s" stroke-width="5"/>' % O +
                    '<g fill="none" stroke="#2c2018" stroke-width="4" opacity="0.8"><path d="M104 80 C98 120 104 150 96 190"/><path d="M150 76 C158 110 152 140 160 176"/><path d="M128 72 C126 110 130 130 128 160"/></g>' +
                    '<path d="M98 110 L120 122 L100 130Z" fill="#ff5a4a" stroke="%s" stroke-width="3"/><path d="M158 110 L136 122 L156 130Z" fill="#ff5a4a" stroke="%s" stroke-width="3"/>' % (O, O) +
                    '<path d="M104 152 C116 160 140 160 152 152" fill="none" stroke="%s" stroke-width="5"/>' % O)

# Rotbloom: a rotting flower with one sickly eye
E['Plague'] = (petal_ring(9, 128, 128, 14, 100, 22, ['#8f5575', '#7a4a66', '#9a6a52'], '#3a1f2e', 3.5, 8) +
               '<g fill="#9ac24a" stroke="%s" stroke-width="2.5"><circle cx="70" cy="86" r="7"/><circle cx="190" cy="72" r="6"/><circle cx="196" cy="168" r="8"/><circle cx="62" cy="170" r="6"/><circle cx="128" cy="38" r="5"/></g>' % O +
               '<path d="M110 178 C108 196 112 206 112 220 M146 180 C148 198 144 208 146 222" stroke="#9ac24a" stroke-width="7" fill="none"/>' +
               '<circle cx="128" cy="128" r="36" fill="#3b2a2a" stroke="%s" stroke-width="4"/>' % O +
               '<circle cx="128" cy="128" r="22" fill="#e8e0a0" stroke="%s" stroke-width="3"/><ellipse cx="128" cy="128" rx="6" ry="15" fill="#7a9a2a" stroke="%s" stroke-width="2"/>' % (O, O))

# Crow: a black crow's head
E['Thief'] = ('<path d="M66 150 C52 204 96 230 150 220 C182 200 182 164 176 142Z" fill="#2a2f3a" stroke="%s" stroke-width="5"/>' % O +
              '<path d="M92 76 C84 56 104 46 120 56 C128 44 150 50 148 66" fill="#2a2f3a" stroke="%s" stroke-width="5"/>' % O +
              '<circle cx="118" cy="116" r="60" fill="#2a2f3a" stroke="%s" stroke-width="5"/>' % O +
              '<path d="M168 100 L232 126 L168 146Z" fill="#e8a82e" stroke="%s" stroke-width="5"/><path d="M168 123 L228 126" stroke="%s" stroke-width="3"/>' % (O, O) +
              '<path d="M72 96 C80 70 110 62 136 70" fill="none" stroke="#5a6478" stroke-width="5" opacity="0.7"/>' +
              '<path d="M84 170 C100 186 126 190 150 180 M80 192 C100 206 124 208 144 200" fill="none" stroke="#4a5568" stroke-width="4"/>' +
              eye(138, 104, 11, '#fff7e8', look=(3, 1), angry=1))

# Enraged Crow (below half HP): ruffled spiky feathers, open beak, glowing red eyes, red-tinted ground
E['ThiefEnraged'] = ('<rect x="0" y="0" width="256" height="256" fill="#8a1f1f" opacity="0.38"/>' +
              '<path d="M60 150 L44 168 L66 172 L52 196 L84 196 L80 218 L112 214 L130 230 L150 218 C182 200 182 164 176 142Z" fill="#1d212b" stroke="%s" stroke-width="5"/>' % O +
              '<path d="M84 80 L70 40 L98 60 L108 30 L124 58 L146 38 L148 72Z" fill="#1d212b" stroke="%s" stroke-width="5"/>' % O +
              '<circle cx="118" cy="116" r="60" fill="#1d212b" stroke="%s" stroke-width="5"/>' % O +
              '<path d="M60 112 L38 104 L58 124Z M66 140 L42 144 L68 156Z" fill="#1d212b" stroke="%s" stroke-width="4"/>' % O +
              '<path d="M166 92 L238 112 L170 124Z" fill="#e8a82e" stroke="%s" stroke-width="5"/>' % O +
              '<path d="M170 132 L232 146 L166 152Z" fill="#d9902a" stroke="%s" stroke-width="5"/>' % O +
              '<path d="M72 96 C80 70 110 62 136 70" fill="none" stroke="#6b4a5a" stroke-width="5" opacity="0.7"/>' +
              '<circle cx="138" cy="104" r="20" fill="#ff3b30" opacity="0.35"/>' +
              eye(138, 104, 11, '#ff5a4a', pupil='#13212e', look=(3, 1), angry=2) +
              '<path d="M40 60 L52 76 M30 90 L48 96" stroke="#ff5a4a" stroke-width="4" opacity="0.8"/>')

# Creeping Vine: a hungry spiral vine with one eye
pts = []
for k in range(121):
    t = k / 120.0
    th = t * 2.9 * math.pi
    r = 10 + 66 * t
    pts.append((128 + r * math.cos(th - math.pi / 2), 116 + r * math.sin(th - math.pi / 2)))
ex, ey = pts[-1]
vd = 'M%.1f %.1f ' % pts[0] + ' '.join('L%.1f %.1f' % p for p in pts[1:]) + ' Q %.1f %.1f %.1f %.1f' % (ex, ey + 40, 112, 226)
tendrils = ''.join('<path d="M%.1f %.1f q%g %g %g %g" fill="none" stroke="%s" stroke-width="4"/>' % (pts[k][0], pts[k][1], dx, dy, dx * 1.6, dy * 0.2, O) for k, dx, dy in [(70, 14, -12), (92, -14, -14), (108, 16, 6)])
E['Reclaimer'] = (outlined(vd, '#4a7a3a', 17, 4) + tendrils +
                  '<path d="M%.1f %.1f c14 -4 26 6 24 20 c-14 4 -26 -6 -24 -20z" fill="#6fb04f" stroke="%s" stroke-width="3"/>' % (ex - 6, ey - 4, O) +
                  '<circle cx="128" cy="116" r="15" fill="#c8403f" stroke="%s" stroke-width="4"/>' % O + eye(128, 116, 8, '#ffe08a', look=(0, 1)))

# Root Leech: a fat root worm with a round toothed mouth
body = 'M62 196 C40 150 96 150 112 118 C128 86 150 112 170 94'
rings = ''.join('<path d="M%g %g l%g %g" stroke="#7a4a22" stroke-width="4"/>' % (x, y, dx, dy) for x, y, dx, dy in
                [(76, 170, 20, -10), (94, 152, 22, 4), (112, 134, 18, 12), (126, 116, 12, 18), (146, 108, 14, -14)])
teeth = ''.join('<path d="M%g %g l3 9 l3 -9z" fill="#fff7e8" transform="rotate(%g 186 92)" stroke="%s" stroke-width="1.5"/>' % (183, 70, a, O) for a in range(0, 360, 40))
E['Leech'] = (outlined(body, '#b57a46', 40, 5) + rings +
              '<circle cx="186" cy="92" r="30" fill="#8a3a32" stroke="%s" stroke-width="5"/>' % O + teeth +
              '<circle cx="186" cy="92" r="14" fill="#3a1414" stroke="%s" stroke-width="3"/>' % O +
              eye(160, 62, 7, look=(2, 1), angry=1) + eye(196, 56, 7, look=(1, 1), angry=-1) +
              '<ellipse cx="96" cy="150" rx="26" ry="8" fill="#d99a62" opacity="0.5" transform="rotate(-30 96 150)"/>')

# Invasive Bloom: an angry flower with a petal of every color
E['ColorHater'] = (petal_ring(8, 128, 128, 16, 104, 26, ['#c8403f', '#4f7fd0', '#e8a82e', '#5a9e45'], O, 4, 0) +
                   '<circle cx="128" cy="128" r="40" fill="#3b2a2a" stroke="%s" stroke-width="4"/>' % O +
                   eye(114, 120, 10, '#fff7e8', look=(1, 2), angry=1) + eye(142, 120, 10, '#fff7e8', look=(-1, 2), angry=-1) +
                   '<path d="M108 148 C118 140 138 140 148 148" fill="none" stroke="#fff7e8" stroke-width="5"/>')

# Invasive Weed: angular blades around a square angry bulb
blades = ''.join('<path d="M%g %g L%g %g L%g %g Z" transform="rotate(%g 128 150)"/>' % (112, 140, 128, 30, 144, 140, a) for a in (-70, -38, 0, 38, 70))
E['ShapeHater'] = ('<g fill="#3f7a35" stroke="%s" stroke-width="4">%s</g>' % (O, blades) +
                   '<rect x="76" y="110" width="104" height="104" rx="18" fill="#6fb04f" stroke="%s" stroke-width="5"/>' % O +
                   '<path d="M92 128 L120 142 L92 152Z M164 128 L136 142 L164 152Z" fill="#fff7e8" stroke="%s" stroke-width="3"/>' % O +
                   '<circle cx="104" cy="143" r="3.5" fill="%s"/><circle cx="152" cy="143" r="3.5" fill="%s"/>' % (O, O) +
                   '<path d="M98 186 L112 176 L128 186 L144 176 L158 186" fill="none" stroke="%s" stroke-width="5"/>' % O)

# Blightmoth: a pale moth with blotched, rotting wings and a feathery antennae pair
wing_l = 'M118 120 C70 60 24 80 30 140 C34 182 82 190 120 156Z'
wing_r = 'M138 120 C186 60 232 80 226 140 C222 182 174 190 136 156Z'
E['Blightmoth'] = ('<g fill="#cfd8a8" stroke="%s" stroke-width="5"><path d="%s"/><path d="%s"/></g>' % (O, wing_l, wing_r) +
                   '<g fill="#6b8a2a" opacity="0.85"><circle cx="64" cy="120" r="14"/><circle cx="84" cy="150" r="9"/><circle cx="192" cy="120" r="14"/><circle cx="172" cy="150" r="9"/></g>' +
                   '<g fill="none" stroke="%s" stroke-width="5"><path d="M114 90 C100 60 84 52 72 56"/><path d="M142 90 C156 60 172 52 184 56"/></g>' % O +
                   '<ellipse cx="128" cy="150" rx="26" ry="52" fill="#8a7a58" stroke="%s" stroke-width="5"/>' % O +
                   '<path d="M106 150 H150 M108 170 H148 M112 190 H144" stroke="#5a4a30" stroke-width="5"/>' +
                   '<circle cx="128" cy="100" r="24" fill="#a89870" stroke="%s" stroke-width="5"/>' % O +
                   eye(118, 98, 8, '#e8f070', look=(1, 1), angry=1) + eye(138, 98, 8, '#e8f070', look=(-1, 1), angry=-1))

# ---- World residents (one per challenge) -------------------------------------------------------------------------
# Hoarfrost (Winter): a six-armed ice crystal with a cold, glaring face
arms = ''.join('<path d="M122 128 L128 34 L134 128 Z" transform="rotate(%d 128 128)"/>' % a for a in range(0, 360, 60))
barbs = ''.join('<path d="M128 74 L112 58 M128 74 L144 58" transform="rotate(%d 128 128)" fill="none" stroke="#dff3ff" stroke-width="5"/>' % a for a in range(0, 360, 60))
E['Hoarfrost'] = ('<g fill="#bfe6ff" stroke="%s" stroke-width="5">%s</g>' % (O, arms) + barbs +
                  '<circle cx="128" cy="128" r="46" fill="#e6f6ff" stroke="%s" stroke-width="5"/>' % O +
                  '<circle cx="112" cy="116" r="12" fill="#ffffff" opacity="0.7"/>' +
                  eye(114, 126, 9, '#ffffff', pupil='#2a6aa8', look=(1, 1), angry=1) + eye(142, 126, 9, '#ffffff', pupil='#2a6aa8', look=(-1, 1), angry=-1) +
                  '<path d="M112 152 C120 144 136 144 144 152" fill="none" stroke="%s" stroke-width="5"/>' % O)

# Locust Swarm (Summer): a big green locust in profile, powerful hind leg, mandibles and veined wings
E['Locust'] = ('<path d="M60 150 C30 120 36 70 96 56 C140 46 196 70 218 112 C196 100 150 90 112 110 Z" fill="#cfd98a" stroke="%s" stroke-width="5"/>' % O +
               '<path d="M96 70 L150 62 M100 86 L170 80 M108 100 L186 98" fill="none" stroke="#8a9a3a" stroke-width="4"/>' +
               '<path d="M62 150 C70 120 120 110 176 130 C200 140 214 156 220 176 L196 176 C186 160 160 150 120 156 C100 160 80 168 62 150 Z" fill="#7fae3a" stroke="%s" stroke-width="5"/>' % O +
               '<path d="M150 134 L186 196 L220 196 L224 210 L170 210 L132 150 Z" fill="#5f8a2a" stroke="%s" stroke-width="5"/>' % O +
               '<path d="M80 160 L72 204 M104 162 L100 206" fill="none" stroke="%s" stroke-width="7"/>' % O +
               '<ellipse cx="56" cy="148" rx="28" ry="24" fill="#8fc24a" stroke="%s" stroke-width="5"/>' % O +
               '<path d="M42 128 C20 100 8 92 8 74 M54 124 C48 92 52 78 60 62" fill="none" stroke="%s" stroke-width="5"/>' % O +
               eye(50, 142, 9, '#ffe14a', look=(-2, 1), angry=1) +
               '<path d="M30 162 L40 176 L52 164 M44 168 L52 182" fill="none" stroke="%s" stroke-width="5"/>' % O)

# Rotgrub (Autumn): a fat pale grub curled on a rotting autumn leaf
E['Rotgrub'] = ('<path d="M20 190 C20 120 90 60 200 70 C220 130 170 200 80 214 C50 218 28 206 20 190 Z" fill="#c9722a" stroke="%s" stroke-width="5"/>' % O +
                '<path d="M30 190 C70 150 120 110 196 78 M70 200 C100 160 140 130 196 108" fill="none" stroke="#8a4a1a" stroke-width="4"/>' +
                '<path d="M56 150 C56 108 112 92 150 114 C190 136 186 188 140 196 C96 204 56 190 56 150 Z" fill="#f1e2b6" stroke="%s" stroke-width="5"/>' % O +
                '<path d="M80 168 C100 176 124 178 150 168 M76 148 C100 156 130 156 160 146 M92 128 C112 134 136 134 156 126" fill="none" stroke="#c9b27a" stroke-width="5"/>' +
                '<circle cx="82" cy="128" r="32" fill="#8a5a2b" stroke="%s" stroke-width="5"/>' % O +
                eye(72, 122, 8, '#fff7e8', look=(-1, 1), angry=1) + eye(94, 122, 8, '#fff7e8', look=(-1, 1), angry=-1) +
                '<path d="M62 148 L56 160 L70 154 M96 148 L102 160 L88 154" fill="#fff7e8" stroke="%s" stroke-width="3"/>' % O)

# Fruitworm (Orchard): a red apple with a worm poking out of a bite-hole
E['Fruitworm'] = ('<path d="M128 70 C90 40 30 64 34 130 C38 190 90 230 128 214 C166 230 218 190 222 130 C226 64 166 40 128 70 Z" fill="#c8403f" stroke="%s" stroke-width="6"/>' % O +
                  '<path d="M64 100 C70 84 86 76 100 78" fill="none" stroke="#ff9a8a" stroke-width="8" opacity="0.7"/>' +
                  '<path d="M128 70 C128 50 134 36 148 28" fill="none" stroke="#5a3a1e" stroke-width="8"/>' +
                  '<path d="M146 34 C166 18 192 24 196 40 C176 52 156 48 146 34 Z" fill="#6fb04f" stroke="%s" stroke-width="4"/>' % O +
                  '<ellipse cx="136" cy="152" rx="36" ry="30" fill="#3a1a14" stroke="%s" stroke-width="5"/>' % O +
                  '<path d="M104 160 C100 120 128 96 150 108 C170 118 164 140 150 138 C140 136 142 126 134 128 C124 130 124 160 128 168 Z" fill="#f1a8c0" stroke="%s" stroke-width="5"/>' % O +
                  '<path d="M112 134 H128 M116 148 H128" stroke="#c97a96" stroke-width="4"/>' +
                  '<circle cx="146" cy="110" r="22" fill="#f6bcd0" stroke="%s" stroke-width="5"/>' % O +
                  eye(138, 106, 7, '#ffffff', look=(1, 1), angry=1) + eye(156, 106, 7, '#ffffff', look=(-1, 1), angry=-1) +
                  '<path d="M138 120 C144 126 152 126 158 120" fill="none" stroke="%s" stroke-width="4"/>' % O)

# Whitefly Cloud (Greenhouse): a pale fly with veined wings and a few tiny ones swirling around it
def fly(cx, cy, k, rot=0):
    return ('<g transform="translate(%g %g) rotate(%g) scale(%g)">' % (cx, cy, rot, k) +
            '<ellipse cx="-22" cy="-14" rx="26" ry="14" transform="rotate(-25 -22 -14)" fill="#ffffff" stroke="%s" stroke-width="%g" opacity="0.95"/>' % (O, 4 / k) +
            '<ellipse cx="22" cy="-14" rx="26" ry="14" transform="rotate(25 22 -14)" fill="#ffffff" stroke="%s" stroke-width="%g" opacity="0.95"/>' % (O, 4 / k) +
            '<ellipse cx="0" cy="8" rx="13" ry="22" fill="#f4eed2" stroke="%s" stroke-width="%g"/>' % (O, 4 / k) +
            '<circle cx="0" cy="-16" r="11" fill="#e8dfb8" stroke="%s" stroke-width="%g"/>' % (O, 4 / k) +
            '<circle cx="-4" cy="-18" r="3.4" fill="#c8403f"/><circle cx="4" cy="-18" r="3.4" fill="#c8403f"/></g>')
E['Whitefly'] = ('<path d="M26 150 C10 110 50 74 90 84 C110 40 190 40 206 90 C246 90 250 150 214 164 C206 200 150 214 126 196 C96 216 40 200 26 150 Z" fill="#dfe9f0" stroke="%s" stroke-width="5" opacity="0.9"/>' % O +
                 fly(128, 128, 2.1) + fly(52, 76, 0.7, -20) + fly(206, 70, 0.6, 18) + fly(44, 190, 0.6, 10) + fly(210, 190, 0.7, -12))

for name, body in E.items():
    open('%s.svg' % name, 'w').write(svg(body))
    print('wrote', name)
