#!/usr/bin/env python3
"""One tiny bold icon per sprout trait (tile upgrade) + the poison marker. 64px target, so: one chip color, one glyph.
Joker combat traits share the purple chip. Output: traits/<PieceTraitKind>.svg and traits/Poison.svg"""
import math, os
O = '#13212e'; W = '#fff7e8'

def chip(col, glyph):
    return ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" width="64" height="64">'
            '<rect x="3" y="3" width="58" height="58" rx="14" fill="%s" stroke="%s" stroke-width="5"/>'
            '<path d="M10 14 Q10 10 16 10 H34" fill="none" stroke="#fff" stroke-width="3" stroke-linecap="round" opacity="0.45"/>'
            '%s</svg>\n') % (col, O, glyph)
def g(s, fill=W, sw=3.5): return '<g fill="%s" stroke="%s" stroke-width="%g" stroke-linejoin="round" stroke-linecap="round">%s</g>' % (fill, O, sw, s)
def star(cx, cy, r, n=4, inner=0.4):
    pts = []
    for i in range(n * 2):
        a = math.pi * i / n - math.pi / 2; rr = r if i % 2 == 0 else r * inner
        pts.append('%.1f %.1f' % (cx + rr * math.cos(a), cy + rr * math.sin(a)))
    return '<path d="M%s Z"/>' % ' L'.join(pts)
def text(s, size=30, y=44, col=W):
    return '<text x="32" y="%g" font-family="Arial Black, Arial, sans-serif" font-weight="900" font-size="%g" text-anchor="middle" fill="%s" stroke="%s" stroke-width="2.2" paint-order="stroke">%s</text>' % (y, size, col, O, s)

T = {}
T['Golden'] = ('#e8a82e', g(star(32, 33, 21, 5, 0.45), '#fff1b0'))
T['Tinted'] = ('#f4efe0', g('<path d="M32 10 C46 28 50 36 44 46 C40 54 24 54 20 46 C14 36 18 28 32 10Z"/>', '#4f7fd0') + '<path d="M26 40 C26 46 30 48 34 48" fill="none" stroke="#fff" stroke-width="3" stroke-linecap="round" opacity="0.7"/>')
T['Multiplier'] = ('#4f7fd0', text('×2', 30, 43))
T['Blast'] = ('#e8863a', g(star(32, 32, 24, 8, 0.5), '#fff1b0') + g('<circle cx="32" cy="32" r="6"/>', '#3a2a1c', 3))
T['Beacon'] = ('#2f4f8c', g('<path d="M32 32 L8 22 M32 32 L56 22 M32 32 L10 44 M32 32 L54 44" fill="none"/>', 'none', 3).replace(O, '#ffd84d') + g('<circle cx="32" cy="32" r="9"/>', '#ffd84d'))
T['Mirror'] = ('#5f699c', g('<path d="M10 20 L26 32 L10 44 Z M54 20 L38 32 L54 44 Z"/>', W) + '<path d="M32 10 V54" stroke="%s" stroke-width="3.5" stroke-linecap="round"/>' % W)
T['Seeder'] = ('#9ad37a', g('<ellipse cx="32" cy="34" rx="12" ry="18" transform="rotate(25 32 34)"/>', '#d8b574') + '<path d="M28 26 L36 40" stroke="#a07a3a" stroke-width="3" stroke-linecap="round"/>')
T['Catalyst'] = ('#7ad0e8', g('<path d="M26 10 H38 V24 L52 50 Q54 54 49 54 H15 Q10 54 12 50 L26 24 Z"/>', '#e6f4ff') + g('<path d="M19 42 H45 L50 52 H14 Z"/>', '#b469e0', 2.5))
T['Twin'] = ('#5a9e45', g('<circle cx="24" cy="32" r="14"/><circle cx="40" cy="32" r="14"/>', W))
T['Detonator'] = ('#b56d7f', g('<rect x="14" y="22" width="30" height="30" rx="5"/>', '#c8403f') + g('<path d="M44 26 Q54 24 54 14"/>', 'none', 3.5) + g(star(54, 12, 8, 6, 0.5), '#ffd84d', 2.5))
T['Chameleon'] = ('#a4ebcc', g('<circle cx="32" cy="32" r="20"/>', '#5a9e45') + '<path d="M32 12 A20 20 0 0 1 32 52 Z" fill="%s" stroke="%s" stroke-width="3"/>' % ('#e8a82e', O) + g('<circle cx="32" cy="32" r="7"/>', W, 3))
T['Spark'] = ('#f1c40f', g('<path d="M36 6 L16 36 H30 L26 58 L48 26 H34 Z"/>', W))
T['Void'] = ('#372e4d', g('<circle cx="32" cy="32" r="21"/>', '#241838') + g('<circle cx="32" cy="32" r="13"/>', '#4a2f80', 3) + g('<circle cx="32" cy="32" r="5"/>', O, 2))
T['Bastion'] = ('#614363', g('<path d="M32 8 L52 15 V32 C52 45 42 52 32 56 C22 52 12 45 12 32 V15 Z"/>', '#d4d8de'))
T['Kamikaze'] = ('#b56d7f', g('<circle cx="30" cy="38" r="17"/>', '#3a3a44') + g('<path d="M40 24 Q48 18 50 10"/>', 'none', 3.5).replace('fill="none"', 'fill="none"') + g(star(52, 9, 8, 6, 0.5), '#ffd84d', 2.5) + '<circle cx="24" cy="32" r="4" fill="#fff" opacity="0.8"/>')
T['Venom'] = ('#5f9f5f', g('<path d="M32 8 C44 24 50 34 44 46 C40 54 24 54 20 46 C14 34 20 24 32 8Z"/>', '#b8e04a') + '<circle cx="28" cy="42" r="4" fill="#3a5a1a"/><circle cx="38" cy="38" r="3" fill="#3a5a1a"/>')
P = '#8c63b0'
T['Bombe'] = (P, g(star(32, 32, 22, 8, 0.45), W) + g('<circle cx="32" cy="32" r="5"/>', P, 3))
T['Range'] = (P, g('<path d="M8 32 H46 M36 20 L50 32 L36 44" fill="none"/>', 'none', 6).replace(O, W) + '<path d="M8 32 H46 M36 20 L50 32 L36 44" fill="none" stroke="%s" stroke-width="2" stroke-linecap="round"/>' % O + '<path d="M54 14 V50" stroke="%s" stroke-width="5" stroke-linecap="round"/>' % W)
T['Eclat'] = (P, g('<rect x="12" y="10" width="40" height="12" rx="4"/>', W) + g('<path d="M32 26 V48 M22 40 L32 52 L42 40" fill="none"/>', 'none', 6).replace(O, W))
T['Precision'] = (P, g('<circle cx="32" cy="32" r="19" fill="none"/>', 'none', 5).replace(O, W) + g('<circle cx="32" cy="32" r="5"/>', W, 2.5) + '<path d="M32 6 V18 M32 46 V58 M6 32 H18 M46 32 H58" stroke="%s" stroke-width="5" stroke-linecap="round"/>' % W)
T['Sangsue'] = (P, g('<path d="M32 8 C44 24 50 34 44 46 C40 54 24 54 20 46 C14 34 20 24 32 8Z"/>', '#c8403f') + '<path d="M26 40 C26 46 30 48 34 48" fill="none" stroke="#fff" stroke-width="3" stroke-linecap="round" opacity="0.7"/>')

# ---- placement-point tile upgrades
def dots3(cells_fill):
    out = ''
    for i in range(9):
        x, y = 14 + (i % 3) * 14, 14 + (i // 3) * 14
        out += '<rect x="%d" y="%d" width="12" height="12" rx="3" fill="%s" stroke="%s" stroke-width="2.5"/>' % (x, y, '#ffd84d' if i in cells_fill else W, O)
    return out
T['Pip'] = ('#e8a82e', text('+1', 28, 42))
T['Lucky'] = ('#5a9e45', g('<rect x="12" y="12" width="40" height="40" rx="9"/>', W) + ''.join('<circle cx="%d" cy="%d" r="4" fill="%s"/>' % (x, y, O) for x, y in ((22, 22), (42, 22), (32, 32), (22, 42), (42, 42))))
T['Companion'] = ('#8c63b0', g('<circle cx="22" cy="32" r="13"/>', '#e8584f') + g('<circle cx="42" cy="32" r="13"/>', '#4fb0d0') + '<path d="M28 32 H36" stroke="%s" stroke-width="4" stroke-linecap="round"/>' % W)
T['Colony'] = ('#5a9e45', ''.join(g('<circle cx="%d" cy="%d" r="8"/>' % (x, y), W, 3) for x, y in ((32, 32), (20, 24), (44, 24), (20, 42), (44, 42), (32, 16), (32, 48))))
T['Border'] = ('#4f7fd0', dots3({0, 1, 2, 3, 5, 6, 7, 8}))
T['Heart'] = ('#c8403f', dots3({4}))
T['Corner'] = ('#e8863a', dots3({0, 2, 6, 8}))
T['Hermit'] = ('#6b4528', '<circle cx="32" cy="32" r="22" fill="none" stroke="%s" stroke-width="3.5" stroke-dasharray="5 5" stroke-linecap="round"/>' % W + g('<circle cx="32" cy="32" r="9"/>', W))
T['Cuddly'] = ('#f08aa4', g('<path d="M32 52 C8 36 12 14 26 14 C30 14 32 18 32 20 C32 18 34 14 38 14 C52 14 56 36 32 52Z"/>', '#fff0f4'))
T['Dawn'] = ('#ffc83a', g('<path d="M10 42 A22 22 0 0 1 54 42 Z"/>', '#fff1b0') + ''.join('<path d="M%.1f %.1f L%.1f %.1f" stroke="%s" stroke-width="3.5" stroke-linecap="round"/>' % (32 + 28 * math.cos(math.radians(a)), 42 - 28 * math.sin(math.radians(a)), 32 + 36 * math.cos(math.radians(a)), 42 - 36 * math.sin(math.radians(a)), W) for a in (30, 60, 90, 120, 150)) + '<path d="M8 46 H56" stroke="%s" stroke-width="4" stroke-linecap="round"/>' % W)
T['Dusk'] = ('#3a2a6b', g('<path d="M40 10 A22 22 0 1 0 54 40 A17 17 0 1 1 40 10Z"/>', '#ffe58a') + g(star(48, 18, 6, 4, 0.4), W, 2))
T['Miser'] = ('#f1c40f', g('<circle cx="32" cy="32" r="21"/>', '#ffe58a') + text('$', 30, 42, '#8a5a00'))
T['Stormy'] = ('#6a86ae', g('<path d="M18 38 C8 38 8 24 20 24 C20 12 40 10 44 22 C56 20 58 38 46 38 Z"/>', W) + g('<path d="M34 36 L26 50 L33 50 L29 60 L42 44 L35 44 L39 36Z"/>', '#ffd23a', 2.5))
T['Elder'] = ('#8a5a2b', g('<circle cx="32" cy="32" r="22"/>', '#d8a566') + g('<circle cx="32" cy="32" r="14"/>', '#c58a4a', 2.5) + g('<circle cx="32" cy="32" r="6"/>', '#8a5a2b', 2.5))
T['Harvester'] = ('#c8403f', g('<path d="M14 52 C8 28 28 10 54 14 C38 18 28 30 28 50 Z"/>', '#d4d8de') + '<path d="M16 52 L10 58" stroke="%s" stroke-width="5" stroke-linecap="round"/>' % W)
T['Breeze'] = ('#5fb8a8', g('<path d="M8 24 C24 24 34 14 44 20 C54 28 46 38 40 34 M8 42 C24 42 40 38 48 44 C54 50 46 58 40 54" fill="none"/>', 'none', 6).replace(O, W))
T['Harmony'] = ('#2fa4a8', ''.join(g('<circle cx="%d" cy="32" r="9"/>' % x, W, 3) for x in (14, 32, 50)) + '<path d="M23 32 H23 M41 32 H41" stroke="%s" stroke-width="3"/>' % O)
T['Prism'] = ('#8c63b0', g('<path d="M32 8 L54 50 H10 Z"/>', '#e6f4ff') + ''.join('<path d="M%d 34 L%d %d" stroke="%s" stroke-width="3" stroke-linecap="round"/>' % (36, 58, y, c) for y, c in ((26, '#e8584f'), (34, '#ffd23a'), (42, '#4fb0d0'))))
T['Gardener'] = ('#4f9440', g('<path d="M16 34 H48 L44 54 H20 Z"/>', '#c58a4a') + g('<path d="M32 34 V14 M32 24 C22 22 20 14 22 10 C30 10 34 16 32 24 M32 28 C42 26 46 18 44 14 C36 14 32 20 32 28" fill="none"/>', 'none', 4).replace(O, '#6fb04f'))


# ---- second batch of placement-point tile upgrades
T['Cluster'] = ('#5a9e45', ''.join(g('<rect x="%d" y="%d" width="22" height="22" rx="5"/>' % (x, y), W, 3) for x, y in ((8, 28), (34, 28), (21, 8))) + g(star(32, 38, 8, 4, 0.45), '#ffd84d', 2.5))
T['Crossfire'] = ('#c8403f', g('<path d="M26 8 H38 V26 H56 V38 H38 V56 H26 V38 H8 V26 H26 Z"/>', W))
T['Even'] = ('#2fa4a8', g('<circle cx="20" cy="32" r="10"/><circle cx="44" cy="32" r="10"/>', W) + '<path d="M28 28 H36 M28 36 H36" stroke="%s" stroke-width="3.5" stroke-linecap="round"/>' % W)
T['RainbowRow'] = ('#8c63b0', ''.join(g('<rect x="%d" y="22" width="12" height="20" rx="3"/>' % x, c, 2.5) for x, c in ((6, '#e8584f'), (19, '#ffd23a'), (32, '#6fb04f'), (45, '#4fb0d0'))))
T['Sunseeker'] = ('#ffc83a', ''.join('<path d="M%.1f %.1f L%.1f %.1f" stroke="%s" stroke-width="4" stroke-linecap="round"/>' % (32 + 17 * math.cos(math.radians(a)), 32 + 17 * math.sin(math.radians(a)), 32 + 26 * math.cos(math.radians(a)), 32 + 26 * math.sin(math.radians(a)), W) for a in range(0, 360, 45)) + g('<circle cx="32" cy="32" r="12"/>', '#fff1b0'))
T['Hunter'] = ('#6b4528', '<circle cx="32" cy="32" r="19" fill="none" stroke="%s" stroke-width="4"/>' % W + '<path d="M32 6 V20 M32 44 V58 M6 32 H20 M44 32 H58" stroke="%s" stroke-width="4" stroke-linecap="round"/>' % W + g('<circle cx="32" cy="32" r="4"/>', '#e8584f', 2))
T['Executioner'] = ('#8a2e2e', g('<path d="M30 8 L52 14 C54 26 48 36 38 40 L34 34 Z"/>', '#d4d8de') + '<path d="M24 56 L36 22" stroke="%s" stroke-width="6" stroke-linecap="round"/>' % W)
T['Spillover'] = ('#4f7fd0', g('<path d="M14 24 H46 L42 52 H18 Z"/>', W) + g('<path d="M50 30 C56 40 52 48 48 48 C44 48 42 40 50 30Z"/>', '#4fb0d0', 2.5) + g('<path d="M32 10 C36 16 38 20 32 22 C26 22 26 16 32 10Z"/>', '#4fb0d0', 2.5))
T['Honeycomb'] = ('#e8a82e', g('<path d="M32 8 L50 18 V38 L32 48 L14 38 V18 Z"/>', '#fff1b0') + g('<path d="M32 22 L40 27 V35 L32 40 L24 35 V27 Z"/>', '#e8a82e', 2.5))
T['Dewdrop'] = ('#5fb8d8', g('<path d="M32 8 C46 26 50 36 44 46 C40 54 24 54 20 46 C14 36 18 26 32 8Z"/>', '#e6f6ff') + '<path d="M26 40 C26 46 30 48 34 48" fill="none" stroke="#5fb8d8" stroke-width="3" stroke-linecap="round"/>')
T['Keystone'] = ('#c8403f', g('<path d="M10 56 V34 A22 22 0 0 1 54 34 V56 H44 V36 A12 12 0 0 0 20 36 V56 Z"/>', '#c9c2b4') + g('<path d="M26 10 H38 L42 26 H22 Z"/>', '#ffd84d', 3))
T['Growth'] = ('#4f9440', g('<path d="M32 56 V26 M32 38 C20 36 16 26 18 20 C28 20 34 28 32 38 M32 32 C44 30 48 20 46 14 C36 14 30 22 32 32" fill="none"/>', 'none', 5).replace(O, W) + '<path d="M8 56 H56" stroke="%s" stroke-width="4" stroke-linecap="round"/>' % W)
T['Gambler'] = ('#9a3f6b', g('<circle cx="32" cy="32" r="21"/>', '#ffe58a') + text('?', 30, 42, '#7a3a5a'))
T['Warden'] = ('#3a6fb0', g('<path d="M32 8 L52 15 V32 C52 45 42 52 32 56 C22 52 12 45 12 32 V15 Z"/>', '#e6f4ff') + ''.join(g('<circle cx="%d" cy="%d" r="4"/>' % (x, y), '#4f9440', 2) for x, y in ((32, 22), (22, 32), (42, 32), (32, 42))))
T['Aegis'] = ('#2f4f8c', g('<path d="M32 8 L52 15 V32 C52 45 42 52 32 56 C22 52 12 45 12 32 V15 Z"/>', '#e6f4ff') + g('<path d="M32 20 L38 30 H26 Z M26 32 H38 L32 44 Z"/>', '#4f7fd0', 2))
T['Sower'] = ('#6fb04f', g('<ellipse cx="22" cy="42" rx="9" ry="13" transform="rotate(25 22 42)"/>', '#d8b574') + g('<path d="M42 54 V34 M42 42 C34 40 32 32 34 28 C42 28 46 34 42 42 M42 38 C50 36 54 28 52 24 C44 24 40 30 42 38" fill="none"/>', 'none', 4).replace(O, W))

# Poison marker: a toothy green skull on a dark violet chip
skull = (g('<path d="M32 8 C18 8 11 18 11 28 C11 35 15 39 18 41 V50 H46 V41 C49 39 53 35 53 28 C53 18 46 8 32 8Z"/>', '#b8e04a')
         + '<circle cx="23" cy="29" r="6" fill="%s"/><circle cx="41" cy="29" r="6" fill="%s"/>' % (O, O)
         + '<path d="M32 36 L28 42 H36 Z" fill="%s"/>' % O
         + '<path d="M25 50 V43 M32 50 V45 M39 50 V43" stroke="%s" stroke-width="2.5"/>' % O)
T['Poison'] = ('#4a2f66', skull)

if __name__ == '__main__':
    os.makedirs('traits', exist_ok=True)
    for k, (col, glyph) in T.items():
        open('traits/%s.svg' % k, 'w').write(chip(col, glyph))
    print(len(T))
