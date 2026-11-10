#!/usr/bin/env python3
"""Growth-stage medallions for the fertilizers' Maturation (Bud / Bloom / Fruit) and the Fruit's glow.
Writes the SVGs to Design/Icons/growth and 256px PNGs (+ .meta with fixed guids) to Assets/Resources/Icons/Growth.
PNGs are rendered with Playwright (NODE_PATH=$(npm root -g)). Run from this folder."""
import os, subprocess, textwrap
O = '#13212e'
OUT_SVG = 'growth'
OUT_PNG = '../../Assets/Resources/Icons/Growth'
os.makedirs(OUT_SVG, exist_ok=True)
os.makedirs(OUT_PNG, exist_ok=True)

def medallion(fill, rim, inner):
    return ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256">'
            '<circle cx="128" cy="128" r="116" fill="%s" stroke="%s" stroke-width="14"/>'
            '<circle cx="128" cy="128" r="98" fill="none" stroke="%s" stroke-width="6" opacity="0.55"/>%s</svg>' % (fill, O, rim, inner))

# Bud: a sprout, stem and two leaves
bud = medallion('#5fc24c', '#bff0a8', textwrap.dedent('''
  <path d="M128 196 C128 168 128 150 128 120" stroke="%(o)s" stroke-width="22" stroke-linecap="round" fill="none"/>
  <path d="M128 196 C128 168 128 150 128 120" stroke="#e8ffd6" stroke-width="10" stroke-linecap="round" fill="none"/>
  <path d="M128 130 C96 134 70 112 66 78 C100 74 126 94 128 130 Z" fill="#e8ffd6" stroke="%(o)s" stroke-width="10" stroke-linejoin="round"/>
  <path d="M128 112 C156 112 184 92 190 58 C156 56 130 76 128 112 Z" fill="#c9f5a8" stroke="%(o)s" stroke-width="10" stroke-linejoin="round"/>
''' % {'o': O}))

# Bloom: five petals around a golden heart
petals = ''.join('<ellipse cx="128" cy="78" rx="26" ry="38" transform="rotate(%d 128 128)" fill="#fff3f7" stroke="%s" stroke-width="9"/>' % (72 * i, O) for i in range(5))
bloom = medallion('#ee82ac', '#ffc9dd', petals + '<circle cx="128" cy="128" r="26" fill="#f6c93a" stroke="%s" stroke-width="9"/><ellipse cx="119" cy="119" rx="7" ry="5" fill="#fff3b8"/>' % O)

# Fruit: a round berry with a leaf and a highlight
fruit = medallion('#f6c93a', '#fff3b8', textwrap.dedent('''
  <path d="M128 92 C128 76 134 64 148 56" stroke="%(o)s" stroke-width="16" stroke-linecap="round" fill="none"/>
  <path d="M134 74 C150 56 176 54 188 66 C176 84 152 88 134 74 Z" fill="#5fc24c" stroke="%(o)s" stroke-width="9" stroke-linejoin="round"/>
  <circle cx="128" cy="140" r="52" fill="#e0453a" stroke="%(o)s" stroke-width="10"/>
  <ellipse cx="108" cy="122" rx="14" ry="9" transform="rotate(-35 108 122)" fill="#ffd0c8"/>
''' % {'o': O}))


# Roots: the links between neighboring fertilizers. White medallions, tinted in the game (sap: by the giver's stage; resonance: blue).
G = '#1c5a4a'
sap = medallion('#ffffff', '#e8f6ee', textwrap.dedent('''
  <path d="M96 76 C96 76 66 112 66 138 C66 158 80 172 96 172 C112 172 126 158 126 138 C126 112 96 76 96 76 Z" fill="%(g)s" stroke="%(o)s" stroke-width="8" stroke-linejoin="round"/>
  <ellipse cx="84" cy="132" rx="8" ry="14" transform="rotate(18 84 132)" fill="#b9e8cf"/>
  <path d="M142 112 L196 112 M172 86 L198 112 L172 138" stroke="%(o)s" stroke-width="22" stroke-linecap="round" stroke-linejoin="round" fill="none"/>
  <path d="M142 112 L196 112 M172 86 L198 112 L172 138" stroke="%(g)s" stroke-width="10" stroke-linecap="round" stroke-linejoin="round" fill="none"/>
''' % {'g': G, 'o': O}))
resonance = medallion('#ffffff', '#e4eefb', textwrap.dedent('''
  <circle cx="104" cy="128" r="46" fill="none" stroke="%(o)s" stroke-width="26"/>
  <circle cx="152" cy="128" r="46" fill="none" stroke="%(o)s" stroke-width="26"/>
  <circle cx="104" cy="128" r="46" fill="none" stroke="#2f6fb5" stroke-width="12"/>
  <circle cx="152" cy="128" r="46" fill="none" stroke="#5b9be0" stroke-width="12"/>
  <path d="M128 90 A46 46 0 0 1 128 166" fill="none" stroke="#9fc9f5" stroke-width="12"/>
''' % {'o': O}))

# Glow: soft golden radial halo behind a Fruit
glow = ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256"><defs><radialGradient id="g">'
        '<stop offset="0.45" stop-color="#ffd84a" stop-opacity="0.95"/><stop offset="0.75" stop-color="#ffd84a" stop-opacity="0.35"/>'
        '<stop offset="1" stop-color="#ffd84a" stop-opacity="0"/></radialGradient></defs><circle cx="128" cy="128" r="128" fill="url(#g)"/></svg>')

ASSETS = {
    'Stage_Bud': (bud, '3b6f1c8d5a2e4f0b9c71d84a6e20f315'),
    'Stage_Bloom': (bloom, '8d41b7e29c5a4630a1f8e5d2b7c94a06'),
    'Stage_Fruit': (fruit, 'c52e90a47b1d48e3865f2a9d0e7b3c14'),
    'Fruit_Glow': (glow, '1e6a3f84d7c24b95a08d5c2e9f713b60'),
    'Roots_Sap': (sap, '6b2f8d41a9c74e05b3d1f6a82c97e450'),
    'Roots_Resonance': (resonance, 'a40d7e93c1b64852960f3b5e2d8c1a77'),
}
META = open('../../Assets/Resources/Icons/GoldenStar.png.meta').read().split('\n', 2)[2]
for name, (svg, guid) in ASSETS.items():
    open('%s/%s.svg' % (OUT_SVG, name), 'w').write(svg)
    meta_path = '%s/%s.png.meta' % (OUT_PNG, name)
    open(meta_path, 'w').write('fileFormatVersion: 2\nguid: %s\n%s' % (guid, META.replace('filterMode: 0', 'filterMode: 1')))
open(OUT_PNG + '.meta', 'w').write('fileFormatVersion: 2\nguid: f0a93d6c2b8e4175a4d1c7e83b95f2a8\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
js = '''
const { chromium } = require('playwright');
const fs = require('fs');
(async () => {
  const b = await chromium.launch();
  const p = await b.newPage({ viewport: { width: 256, height: 256 }, deviceScaleFactor: 1 });
  for (const name of %s) {
    const svg = fs.readFileSync('%s/' + name + '.svg', 'utf8');
    await p.setContent('<html><body style="margin:0;background:transparent">' + svg + '</body></html>');
    await p.screenshot({ path: '%s/' + name + '.png', omitBackground: true, clip: { x: 0, y: 0, width: 256, height: 256 } });
  }
  await b.close();
})();
''' % (list(ASSETS.keys()), OUT_SVG, OUT_PNG)
open('/tmp/render_growth.js', 'w').write(js)
env = dict(os.environ, NODE_PATH=subprocess.check_output(['npm', 'root', '-g']).decode().strip())
subprocess.run(['node', '/tmp/render_growth.js'], check=True, env=env)
