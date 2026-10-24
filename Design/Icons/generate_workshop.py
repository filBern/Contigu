#!/usr/bin/env python3
"""Shop art without any sign: bare glyphs (the motif in the middle of every upgrade's sign) and the Workshop's own objects
(a crate per pack, a compost bucket, an anvil). Output: glyphs/<UpgradeId>.svg and workshop/<Name>.svg (viewBox 256)."""
import os
from generate_icons import *

def glyph(motif):
    # Motifs are drawn around (50, 46) in a ~100x92 box; centered here in a square with a little margin.
    return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="-14 -18 128 128" width="256" height="256">%s</svg>\n' % motif

def crate(motif, band):
    s = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256">\n'
    s += '<ellipse cx="128" cy="236" rx="104" ry="12" fill="#000" opacity="0.28"/>\n'
    s += '<rect x="30" y="92" width="196" height="136" rx="14" fill="#c58a4a" stroke="%s" stroke-width="6"/>\n' % O
    s += '<path d="M34 138 H222 M34 184 H222" stroke="#8a5a2b" stroke-width="4"/>\n'
    s += ''.join('<circle cx="%d" cy="%d" r="4" fill="#5a3a1e"/>' % (x, y) for x in (46, 210) for y in (108, 212))
    s += '<rect x="20" y="62" width="216" height="40" rx="12" fill="%s" stroke="%s" stroke-width="6"/>\n' % (band, O)
    s += '<path d="M34 72 H222" stroke="#fff" stroke-width="4" opacity="0.35" stroke-linecap="round"/>\n'
    s += '<rect x="68" y="112" width="120" height="96" rx="12" fill="%s" stroke="%s" stroke-width="5"/>\n' % (CREAM, O)
    s += '<g transform="translate(%g %g) scale(0.88)">%s</g>\n' % (128 - 50 * 0.88, 160 - 46 * 0.88, motif)
    return s + '</svg>\n'

def bucket():
    s = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256">\n'
    s += '<ellipse cx="128" cy="238" rx="90" ry="11" fill="#000" opacity="0.28"/>\n'
    s += '<path d="M54 112 L202 112 L186 230 L70 230 Z" fill="#8a5a2b" stroke="%s" stroke-width="6" stroke-linejoin="round"/>\n' % O
    s += '<path d="M60 150 H196 M66 192 H190" stroke="#5a3a1e" stroke-width="5"/>\n'
    s += '<path d="M92 118 L84 226 M128 118 V228 M164 118 L172 226" stroke="#a8723a" stroke-width="3" opacity="0.8"/>\n'
    s += '<ellipse cx="128" cy="112" rx="80" ry="16" fill="#3a2a1c" stroke="%s" stroke-width="6"/>\n' % O
    s += '<ellipse cx="108" cy="110" rx="12" ry="5" fill="#5a4030"/><ellipse cx="150" cy="114" rx="14" ry="5" fill="#5a4030"/>\n'
    s += '<g transform="translate(128 108)">%s</g>\n' % sprout(0, 0, 2.2)
    s += '<g transform="translate(176 84)">%s</g>\n' % star(0, 0, 9, '#fff1b0', 0.4, 4, 2.5)
    return s + '</svg>\n'

def anvil():
    s = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256">\n'
    s += '<ellipse cx="128" cy="238" rx="100" ry="11" fill="#000" opacity="0.28"/>\n'
    s += '<path d="M24 124 L204 124 C204 152 184 160 162 162 L168 186 L196 204 L196 232 L60 232 L60 204 L88 186 L94 162 C60 160 36 146 24 124 Z" fill="#6b7280" stroke="%s" stroke-width="6" stroke-linejoin="round"/>\n' % O
    s += '<path d="M30 126 L200 126" stroke="#aab2bd" stroke-width="8" stroke-linecap="round"/>\n'
    s += '<path d="M72 206 H184" stroke="#4b5563" stroke-width="5" stroke-linecap="round"/>\n'
    s += '<path d="M104 168 L108 190" stroke="#8a929e" stroke-width="4" stroke-linecap="round" opacity="0.8"/>\n'
    return s + '</svg>\n'

if __name__ == '__main__':
    os.makedirs('glyphs', exist_ok=True); os.makedirs('workshop', exist_ok=True)
    for k, (m, pool) in U.items():
        open('glyphs/%s.svg' % k, 'w').write(glyph(M[m]()))
    open('workshop/SeedPack.svg', 'w').write(crate(M['u_seedpack'](), POOL['Bank']))
    open('workshop/SproutPack.svg', 'w').write(crate(M['u_sproutpack'](), POOL['Grid']))
    open('workshop/FertilizerPack.svg', 'w').write(crate(M['u_fertpack'](), POOL['Modifier']))
    open('workshop/MasteryPack.svg', 'w').write(crate(M['u_masterypack'](), POOL['Mastery']))
    open('workshop/Compost.svg', 'w').write(bucket())
    open('workshop/Anvil.svg', 'w').write(anvil())
    print(len(U), 'glyphs, 6 workshop')
