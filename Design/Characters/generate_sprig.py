#!/usr/bin/env python3
"""Sprig, the garden guide: a little seed sprite with a sprout on its head, drawn in the garden style (no dark outline: soft gradients, a soft shadow, glossy eyes).
4 expressions -> Sprig_<Expr>.svg here and Assets/Resources/Characters/Sprig_<Expr>.png (256 px, rendered with Playwright: NODE_PATH=$(npm root -g) python3 generate_sprig.py)."""
import os, subprocess

OUT = '../../Assets/Resources/Characters'
INK = '#3a2a1c'
DEFS = ('<defs>'
        '<linearGradient id="body" x1="0.15" y1="0" x2="0.9" y2="1"><stop offset="0" stop-color="#fbe8bd"/><stop offset="0.5" stop-color="#ebca8a"/><stop offset="1" stop-color="#c89a58"/></linearGradient>'
        '<linearGradient id="bib" x1="0" y1="0" x2="0.8" y2="1"><stop offset="0" stop-color="#9be8b0"/><stop offset="1" stop-color="#4fae70"/></linearGradient>'
        '<linearGradient id="boot" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#a06a38"/><stop offset="1" stop-color="#5e3b1c"/></linearGradient>'
        '<linearGradient id="leafA" x1="0" y1="1" x2="1" y2="0"><stop offset="0" stop-color="#5bb04a"/><stop offset="1" stop-color="#b6ec72"/></linearGradient>'
        '<linearGradient id="leafB" x1="0" y1="1" x2="1" y2="0"><stop offset="0" stop-color="#3f8f3a"/><stop offset="1" stop-color="#7fd05a"/></linearGradient>'
        '<filter id="soft" x="-20%" y="-15%" width="140%" height="140%"><feDropShadow dx="0" dy="6" stdDeviation="5" flood-color="#0c1620" flood-opacity="0.5"/></filter>'
        '<filter id="blur"><feGaussianBlur stdDeviation="2.2"/></filter>'
        '</defs>')

def eyes(look=0, sad=False):
    out = ''
    for cx in (104, 152):
        out += '<ellipse cx="%d" cy="136" rx="13.5" ry="16.5" fill="#fffdf5"/>' % cx
        out += '<ellipse cx="%d" cy="%d" rx="9" ry="10.5" fill="%s"/>' % (cx + 1, 139 + look, INK)
        out += '<circle cx="%d" cy="%d" r="3.6" fill="#fff"/><circle cx="%d" cy="%d" r="1.8" fill="#fff" opacity="0.85"/>' % (cx + 4, 134 + look, cx - 2, 143 + look)
    return out

def cheeks():
    return ('<g filter="url(#blur)"><ellipse cx="84" cy="161" rx="13" ry="8.5" fill="#ff8fa3" opacity="0.8"/><ellipse cx="172" cy="161" rx="13" ry="8.5" fill="#ff8fa3" opacity="0.8"/></g>')

def face(expr):
    if expr == 'Happy':
        return eyes() + cheeks() + '<path d="M108 165 Q128 192 148 165 Q128 174 108 165 Z" fill="#8a3a3a"/><path d="M116 176 Q128 185 140 176 Q128 172 116 176 Z" fill="#ff8f9a"/>'
    if expr == 'Neutral':
        return eyes() + cheeks() + '<ellipse cx="128" cy="173" rx="7.5" ry="8.5" fill="#8a3a3a"/><ellipse cx="128" cy="177" rx="4.5" ry="3.5" fill="#ff8f9a"/>'
    if expr == 'Sad':
        brows = '<path d="M88 122 L116 112 M168 122 L140 112" stroke="#6a4424" stroke-width="5" stroke-linecap="round"/>'
        tear = '<path d="M170 158 C164 170 176 170 170 158 Z" fill="#8fd6f5"/>'
        return eyes(look=3) + brows + cheeks() + '<path d="M112 180 Q128 164 144 180" fill="none" stroke="#8a3a3a" stroke-width="5" stroke-linecap="round"/>' + tear
    # Cheer: eyes shut in joy, a big open mouth
    arcs = ''.join('<path d="M%d 142 Q%d 122 %d 142" fill="none" stroke="%s" stroke-width="5.5" stroke-linecap="round"/>' % (cx - 13, cx, cx + 13, INK) for cx in (104, 152))
    return arcs + cheeks() + '<path d="M102 162 Q128 204 154 162 Z" fill="#8a3a3a"/><path d="M114 182 Q128 194 142 182 Q128 176 114 182 Z" fill="#ff8f9a"/>'

def arms(expr):
    left, right = {'Cheer': ('M72 172 Q42 142 50 112', 'M184 172 Q214 142 206 112'),
                   'Sad': ('M74 180 Q62 206 70 222', 'M182 180 Q194 206 186 222')}.get(expr, ('M72 178 Q50 190 54 214', 'M184 178 Q206 190 202 214'))
    out = ''
    for d in (left, right):
        out += '<path d="%s" fill="none" stroke="#e4c07e" stroke-width="15" stroke-linecap="round"/>' % d
        # a little hand at the end of the arm
        x, y = d.split()[-2], d.split()[-1]
        out += '<circle cx="%s" cy="%s" r="9.5" fill="#f1d598"/>' % (x, y)
    return out

def svg(expr):
    body = (
        '<ellipse cx="128" cy="244" rx="82" ry="9" fill="#0c1620" opacity="0.3"/>'
        '<g filter="url(#soft)">'
        '<ellipse cx="97" cy="236" rx="23" ry="11" fill="url(#boot)"/><ellipse cx="159" cy="236" rx="23" ry="11" fill="url(#boot)"/>'
        '<ellipse cx="90" cy="232" rx="9" ry="3.5" fill="#d9a066" opacity="0.6"/><ellipse cx="152" cy="232" rx="9" ry="3.5" fill="#d9a066" opacity="0.6"/>'
        + arms(expr) +
        '<path d="M128 62 C196 62 212 140 200 190 C192 232 64 232 56 190 C44 140 60 62 128 62 Z" fill="url(#body)"/>'
        '<path d="M170 78 C206 116 206 176 190 214 C202 196 214 150 196 108 C190 94 182 84 170 78 Z" fill="#a8763a" opacity="0.22"/>'
        '<path d="M80 104 C88 84 106 74 122 74" fill="none" stroke="#fffaf0" stroke-width="8" stroke-linecap="round" opacity="0.7"/>'
        # overalls: a bib with a little flower pocket
        '<path d="M80 202 C80 192 98 187 128 187 C158 187 176 192 176 202 C180 231 76 231 80 202 Z" fill="url(#bib)"/><path d="M92 195 C106 191 150 191 164 195" fill="none" stroke="#c9f6d4" stroke-width="3" stroke-linecap="round" opacity="0.7"/>'
        '<g transform="translate(128 206)">' + ''.join('<circle cx="%g" cy="%g" r="4.2" fill="#fff6e6"/>' % (6 * __import__('math').cos(__import__('math').radians(a)), 6 * __import__('math').sin(__import__('math').radians(a))) for a in range(0, 360, 72)) + '<circle r="3.4" fill="#ffc83a"/></g>'
        # the sprout
        '<path d="M128 68 C127 52 129 44 134 30" fill="none" stroke="#4f9a3c" stroke-width="7" stroke-linecap="round"/>'
        '<path d="M133 38 C152 8 190 10 199 22 C192 52 160 58 133 38 Z" fill="url(#leafA)"/><path d="M140 38 C158 26 174 24 190 24" fill="none" stroke="#e4f9b8" stroke-width="3" stroke-linecap="round" opacity="0.8"/>'
        '<path d="M131 50 C110 28 80 32 69 44 C78 68 108 70 131 50 Z" fill="url(#leafB)"/><path d="M122 50 C106 42 90 40 78 44" fill="none" stroke="#c9f09a" stroke-width="3" stroke-linecap="round" opacity="0.7"/>'
        '</g>' + face(expr))
    return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256">%s%s</svg>\n' % (DEFS, body)

if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    names = ['Happy', 'Neutral', 'Sad', 'Cheer']
    for e in names:
        open('Sprig_%s.svg' % e, 'w').write(svg(e))
    js = """
const { chromium } = require('playwright');
const fs = require('fs');
(async () => {
  const b = await chromium.launch();
  const p = await b.newPage({ viewport: { width: 256, height: 256 }, deviceScaleFactor: 1 });
  for (const e of %s) {
    await p.setContent('<html><body style="margin:0;background:transparent">' + fs.readFileSync('Sprig_' + e + '.svg', 'utf8') + '</body></html>');
    await p.screenshot({ path: '%s/Sprig_' + e + '.png', omitBackground: true, clip: { x: 0, y: 0, width: 256, height: 256 } });
  }
  await b.close();
})();
""" % (names, OUT)
    open('_render.js', 'w').write(js)
    env = dict(os.environ, NODE_PATH=subprocess.check_output(['npm', 'root', '-g']).decode().strip())
    subprocess.run(['node', '_render.js'], check=True, env=env)
    os.remove('_render.js')
    print('Sprig:', ', '.join(names))
