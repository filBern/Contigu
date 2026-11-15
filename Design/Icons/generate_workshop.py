#!/usr/bin/env python3
"""Shop art without any sign: bare glyphs (the motif in the middle of every upgrade's sign) and the Workshop's own objects
(a crate per pack, a compost bucket, an anvil). Output: glyphs/<UpgradeId>.svg and workshop/<Name>.svg (viewBox 256)."""
import os
from generate_icons import *
from outline import strip_outline

def glyph(motif):
    # Motifs are drawn around (50, 46) in a ~100x92 box; centered here in a square with a little margin.
    # No dark outline on the shop glyphs (the filled shapes keep only their colors; lines and rings that are the drawing stay).
    return strip_outline('<svg xmlns="http://www.w3.org/2000/svg" viewBox="-14 -18 128 128" width="256" height="256">%s</svg>\n' % motif)

# ---- garden style for the shop objects: soft gradients and shadows instead of a dark outline (same language as the fertilizer pots)
POOL_UI = {'Bank': '#db736b', 'Grid': '#6b99e0', 'Mastery': '#a880cc', 'Modifier': '#edb84d', 'Workshop': '#59ad7a'}   # the pool colors of the tooltips' boxes
DEFS = ('<defs>'
        '<linearGradient id="wood" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#e2ad6c"/><stop offset="0.6" stop-color="#c98d4a"/><stop offset="1" stop-color="#a86f36"/></linearGradient>'
        '<linearGradient id="wood2" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#ecbb7e"/><stop offset="1" stop-color="#c98d4a"/></linearGradient>'
        '<linearGradient id="steel" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#9aa3b0"/><stop offset="0.55" stop-color="#6f7886"/><stop offset="1" stop-color="#4f5766"/></linearGradient>'
        '<linearGradient id="leaf" x1="0" y1="1" x2="1" y2="0"><stop offset="0" stop-color="#4f9a3c"/><stop offset="1" stop-color="#8fd25a"/></linearGradient>'
        '<filter id="soft" x="-20%" y="-20%" width="140%" height="150%"><feDropShadow dx="0" dy="5" stdDeviation="4" flood-color="#2a1a0c" flood-opacity="0.45"/></filter>'
        '<filter id="lab" x="-20%" y="-20%" width="140%" height="150%"><feDropShadow dx="0" dy="3" stdDeviation="2" flood-color="#2a1a0c" flood-opacity="0.5"/></filter>'
        '<filter id="ms" x="-20%" y="-20%" width="140%" height="150%"><feDropShadow dx="0" dy="2" stdDeviation="1.4" flood-color="#4a2e10" flood-opacity="0.6"/></filter>'
        '</defs>\n')
PLAQUE = '#f6e8c8'
HEAD = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256">\n' + DEFS

def plaque(x, y, w, h, rim, motif, scale, rx=14):
    """A cream plaque rimmed in `rim`, with the (outline-free, softly shadowed) motif centered on it."""
    inner = 5
    cx, cy = x + w / 2, y + h / 2
    return ('<g filter="url(#lab)"><rect x="%g" y="%g" width="%g" height="%g" rx="%g" fill="%s"/></g>'
            '<rect x="%g" y="%g" width="%g" height="%g" rx="%g" fill="%s"/>'
            '<g filter="url(#ms)"><g transform="translate(%g %g) scale(%g)">%s</g></g>\n') % (
        x, y, w, h, rx, rim, x + inner, y + inner, w - 2 * inner, h - 2 * inner, rx - 4, PLAQUE, cx - 50 * scale, cy - 46 * scale, scale, strip_outline(motif))

def crate(motif, band):
    s = HEAD
    s += '<ellipse cx="128" cy="242" rx="104" ry="11" fill="#2a1a0c" opacity="0.28"/>\n'
    s += '<g filter="url(#soft)"><rect x="30" y="92" width="196" height="146" rx="16" fill="url(#wood)"/></g>\n'
    s += '<path d="M36 142 H220 M36 190 H220" stroke="#8a5a2b" stroke-width="4" opacity="0.4"/><path d="M40 104 C38 150 40 200 44 230" fill="none" stroke="#fff0d0" stroke-width="7" stroke-linecap="round" opacity="0.28"/>\n'
    s += ''.join('<circle cx="%d" cy="%d" r="4" fill="#7a4e24" opacity="0.8"/>' % (x, y) for x in (48, 208) for y in (110, 220))
    s += '<g filter="url(#soft)"><rect x="20" y="58" width="216" height="46" rx="14" fill="%s"/></g><rect x="30" y="64" width="196" height="9" rx="4.5" fill="#fff" opacity="0.38"/>\n' % band
    s += plaque(68, 114, 120, 100, band, motif, 0.9)
    return s + '</svg>\n'

def bucket():
    s = HEAD
    s += '<ellipse cx="128" cy="244" rx="90" ry="10" fill="#2a1a0c" opacity="0.28"/>\n'
    s += '<g filter="url(#soft)"><path d="M54 116 L202 116 L188 232 C187 238 182 240 176 240 H80 C74 240 69 238 68 232 Z" fill="url(#wood)"/></g>\n'
    s += '<path d="M60 152 H196 M64 196 H192" stroke="#6a4420" stroke-width="9" stroke-linecap="round" opacity="0.5"/><path d="M92 122 L84 234 M128 122 V236 M164 122 L172 234" stroke="#f0c488" stroke-width="3" opacity="0.35"/>\n'
    s += '<path d="M62 126 C60 160 64 200 70 232" fill="none" stroke="#fff0d0" stroke-width="7" stroke-linecap="round" opacity="0.3"/>\n'
    s += '<ellipse cx="128" cy="116" rx="76" ry="16" fill="#4a3220"/><ellipse cx="128" cy="113" rx="68" ry="12" fill="#6b4a2e"/>\n'
    s += '<ellipse cx="108" cy="112" rx="12" ry="4" fill="#85613c"/><ellipse cx="150" cy="116" rx="14" ry="4" fill="#85613c"/>\n'
    s += '<g filter="url(#soft)"><path d="M128 112 C126 92 126 78 128 66" fill="none" stroke="#4f9a3c" stroke-width="7" stroke-linecap="round"/>'
    s += '<path d="M128 84 C108 84 90 72 86 52 C108 48 126 60 128 84 Z" fill="url(#leaf)"/><path d="M128 76 C148 76 166 64 172 44 C150 42 130 54 128 76 Z" fill="url(#leaf)"/></g>\n'
    return s + '</svg>\n'

def anvil():
    s = HEAD
    s += '<ellipse cx="128" cy="244" rx="100" ry="10" fill="#2a1a0c" opacity="0.28"/>\n'
    s += '<g filter="url(#soft)"><path d="M24 124 L204 124 C204 152 184 160 162 162 L168 186 L196 204 L196 234 L60 234 L60 204 L88 186 L94 162 C60 160 36 146 24 124 Z" fill="url(#steel)"/></g>\n'
    s += '<path d="M30 128 L200 128" stroke="#cfd6e0" stroke-width="8" stroke-linecap="round" opacity="0.8"/><path d="M70 208 H186" stroke="#3b4350" stroke-width="5" stroke-linecap="round" opacity="0.6"/>\n'
    s += '<path d="M104 168 L108 192" stroke="#aab2bd" stroke-width="4" stroke-linecap="round" opacity="0.7"/>\n'
    return s + '</svg>\n'

if __name__ == '__main__':
    os.makedirs('glyphs', exist_ok=True); os.makedirs('workshop', exist_ok=True)
    for k, (m, pool) in U.items():
        open('glyphs/%s.svg' % k, 'w').write(glyph(M[m]()))
    open('workshop/SeedPack.svg', 'w').write(crate(M['u_seedpack'](), POOL['Bank']))
    open('workshop/SproutPack.svg', 'w').write(crate(M['u_sproutpack'](), POOL['Grid']))
    open('workshop/FertilizerPack.svg', 'w').write(crate(M['u_fertpack'](), POOL['Modifier']))
    open('workshop/MasteryPack.svg', 'w').write(crate(M['u_masterypack'](), POOL['Mastery']))
    open('workshop/WildPack.svg', 'w').write(crate(M['u_joker'](), POOL['Bank']))
    open('workshop/Transplant.svg', 'w').write(crate(M['u_replace'](), POOL['Workshop']))
    open('workshop/Compost.svg', 'w').write(bucket())
    open('workshop/Anvil.svg', 'w').write(anvil())
    print(len(U), 'glyphs, 7 workshop')
