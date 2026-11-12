#!/usr/bin/env python3
"""Rectangular "garden plot" buttons: a colored bed border around a dark soil inset (the text sits on the soil, in the bed's color), plus four
tiny flowers that decorate the soil. Writes plots/<Name>.svg, then renders with Playwright (NODE_PATH=$(npm root -g)) into
Assets/Resources/Buttons/Plot (9-sliced 384x160 bodies, ppu 400, border 36) and Assets/Resources/Buttons/Plot/Flowers (64 px). Run from this folder."""
import os, subprocess

O = '#13212e'
RES = '../../Assets/Resources/Buttons/Plot'
SVG = 'plots'
os.makedirs(SVG, exist_ok=True)
os.makedirs(RES + '/Flowers', exist_ok=True)

# name: (bed top, bed bottom, soil)
ROLES = {
    'Green': ('#7fcb5a', '#3e8a35', '#23391f'),
    'Gold':  ('#f6cf62', '#c8871a', '#45330f'),
    'Blue':  ('#7fb0ee', '#2f64b4', '#1b2f4c'),
    'Red':   ('#e8665b', '#a93030', '#4a1d1d'),
    'Wood':  ('#b78552', '#74492a', '#3a2a1c'),
    # Beige soil, for the hand slots and the boxes of the left column (art and dark text sit on them).
    'WoodLight': ('#b78552', '#74492a', '#d9c3a0'),
    'GoldLight': ('#f6cf62', '#c8871a', '#ecd9a0'),
    'RedLight':  ('#e8665b', '#a93030', '#e3b4ab'),
    'GreenLight': ('#7fcb5a', '#3e8a35', '#c5d9ab'),
}
GUIDS = {
    'Green': '2d71a0c4e9b3485f8a16d3c07e52b9f1', 'Gold': '6f08c3b7a1e4492d9b35e0d8a74c1f26', 'Blue': 'b3e5d927c0a84f16a97d2c48e1f06b53',
    'Red': '91c4f7a2d6e0453b8c1a5e79b3d02f68', 'Wood': 'e72a5b1c9d38406fb4e0a1c6d85f3927',
    'WoodLight': '3f81d2a6c5b94e07a0d3b8f1e64c2975', 'GoldLight': 'd09a4e7b1c3f4825b6e1a9d2f07c8b34', 'RedLight': '58b2c9e0a4d74f13b7a6d1e8c3f92a60',
    'GreenLight': '27c6a09d4b1e4f83a5d2e7b0c91f3a68',
}
FLOWER_GUIDS = {
    'Red': '5c19e8a3b7d24f60a2c4d9e1f8b30675', 'Blue': 'a8d3f0b6c1e94725b7a2e4c8d0f13961',
    'Yellow': '0e6b4c92a5d7418fb3c9e2a7d4f81b05', 'White': 'f4a71d3e8c0b4926a5d1b7e3c9f20846',
}

def body(top, bot, soil):
    return ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 384 160" width="384" height="160"><defs>'
            '<linearGradient id="b" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="%s"/><stop offset="1" stop-color="%s"/></linearGradient>'
            '<linearGradient id="h" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#fff" stop-opacity="0.6"/><stop offset="0.5" stop-color="#fff" stop-opacity="0"/></linearGradient>'
            '<linearGradient id="s" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#000" stop-opacity="0.6"/><stop offset="0.55" stop-color="#000" stop-opacity="0"/></linearGradient></defs>'
            '<rect x="3" y="3" width="378" height="154" rx="26" fill="url(#b)" stroke="%s" stroke-width="6"/>'
            '<rect x="9" y="9" width="366" height="142" rx="21" fill="none" stroke="url(#h)" stroke-width="4"/>'
            '<rect x="28" y="28" width="328" height="104" rx="14" fill="%s" stroke="%s" stroke-width="5"/>'
            '<rect x="31.5" y="31.5" width="321" height="97" rx="12" fill="none" stroke="url(#s)" stroke-width="6"/></svg>') % (top, bot, O, soil, O)

def flower(petal, center, n=6):
    import math
    s = ''
    for i in range(n):
        a = 360.0 * i / n
        s += '<ellipse cx="32" cy="15" rx="9" ry="12" transform="rotate(%g 32 32)" fill="%s" stroke="%s" stroke-width="3"/>' % (a, petal, O)
    s += '<circle cx="32" cy="32" r="9" fill="%s" stroke="%s" stroke-width="3"/>' % (center, O)
    return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" width="64" height="64">%s</svg>' % s

FLOWERS = {'Red': ('#e5534b', '#2a1418', 5), 'Blue': ('#6f9ae6', '#ffd45a', 6), 'Yellow': ('#f5c542', '#6b4020', 8), 'White': ('#fff3e0', '#e8a82e', 6)}

jobs = []  # (svg name, png path, size)
for name, (top, bot, soil) in ROLES.items():
    open('%s/Plot%s.svg' % (SVG, name), 'w').write(body(top, bot, soil))
    jobs.append(('Plot' + name, '%s/Plot%s.png' % (RES, name), (384, 160)))
for name, (petal, center, n) in FLOWERS.items():
    open('%s/Flower%s.svg' % (SVG, name), 'w').write(flower(petal, center, n))
    jobs.append(('Flower' + name, '%s/Flowers/Flower%s.png' % (RES, name), (64, 64)))

js = """
const { chromium } = require('playwright');
const fs = require('fs');
const jobs = %s;
(async () => {
  const b = await chromium.launch();
  for (const [name, out, w, h] of jobs) {
    const p = await b.newPage({ viewport: { width: w, height: h }, deviceScaleFactor: 1 });
    await p.setContent('<html><body style="margin:0;background:transparent">' + fs.readFileSync('%s/' + name + '.svg', 'utf8') + '</body></html>');
    await p.screenshot({ path: out, omitBackground: true, clip: { x: 0, y: 0, width: w, height: h } });
    await p.close();
  }
  await b.close();
})();
""" % ([[n, o, s[0], s[1]] for n, o, s in jobs], SVG)
open('_render.js', 'w').write(js)
env = dict(os.environ, NODE_PATH=subprocess.check_output(['npm', 'root', '-g']).decode().strip())
subprocess.run(['node', '_render.js'], check=True, env=env)
os.remove('_render.js')

META = open('../../Assets/Resources/Buttons/Garden/BtnGreen.png.meta').read().split('\n', 2)[2]
def write_meta(path, guid, border, ppu):
    m = META.replace('spritePixelsToUnits: 400', 'spritePixelsToUnits: %d' % ppu).replace('spriteBorder: {x: 96, y: 0, z: 96, w: 0}', 'spriteBorder: {x: %d, y: %d, z: %d, w: %d}' % (border, border, border, border))
    open(path + '.meta', 'w').write('fileFormatVersion: 2\nguid: %s\n%s' % (guid, m))
for name in ROLES:
    write_meta('%s/Plot%s.png' % (RES, name), GUIDS[name], 36, 400)
for name in FLOWERS:
    write_meta('%s/Flowers/Flower%s.png' % (RES, name), FLOWER_GUIDS[name], 0, 100)
def folder_meta(path, guid):
    open(path + '.meta', 'w').write('fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n' % guid)
folder_meta('../../Assets/Resources/Buttons/Plot', '7a30c5e8b2d14f96a1e7c3b09d8f4a25')
folder_meta('../../Assets/Resources/Buttons/Plot/Flowers', 'c1e84a07d3b94f52b6a0e9d7c2f51b38')
print('ok')
