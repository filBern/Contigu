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
