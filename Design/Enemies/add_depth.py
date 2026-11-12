#!/usr/bin/env python3
"""Faux-3D pass for the enemy portraits: each <Name>.svg (written by generate_enemies.py) is wrapped in a block look seen from slightly above: the plate
shrinks a little and a darker front face (the block's thickness) shows under it, with a soft light edge on the top-left. Renders 256 px PNGs with
Playwright (NODE_PATH=$(npm root -g)) into Assets/Resources/Enemies/<Name>.png (the files the game loads). Run after generate_enemies.py, from this folder."""
import glob, os, re, subprocess

O = '#13212e'
OUT_SVG = '3d'
RES = '../../Assets/Resources/Enemies'
os.makedirs(OUT_SVG, exist_ok=True)

S = 0.94                      # uniform shrink of the original art
X = (256 - 256 * S) / 2       # centered horizontally
PLATE_X, PLATE_W = X + 8 * S, 240 * S
LIP_Y = 7.5 + 14.5            # the front face sits this much lower than the plate

def wrap(inner):
    return ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256"><defs>'
            '<linearGradient id="hl" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#fff" stop-opacity="0.55"/><stop offset="0.5" stop-color="#fff" stop-opacity="0"/></linearGradient>'
            '<linearGradient id="front" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#2a1c12"/><stop offset="1" stop-color="#150d08"/></linearGradient></defs>'
            '<rect x="%.1f" y="%.1f" width="%.1f" height="%.1f" rx="42" fill="url(#front)" stroke="%s" stroke-width="6"/>'
            '<svg x="%.2f" y="0" width="%.2f" height="%.2f" viewBox="0 0 256 256">%s</svg>'
            '<rect x="%.1f" y="14" width="%.1f" height="%.1f" rx="34" fill="none" stroke="url(#hl)" stroke-width="4"/></svg>'
            ) % (PLATE_X, LIP_Y, PLATE_W, PLATE_W, O, X, 256 * S, 256 * S, inner, PLATE_X + 6, PLATE_W - 12, PLATE_W - 14)

names = []
for path in sorted(glob.glob('*.svg')):
    name = os.path.splitext(path)[0]
    src = open(path).read()
    m = re.search(r'<svg[^>]*>(.*)</svg>', src, re.S)
    if not m or not os.path.exists('%s/%s.png' % (RES, name)):
        continue
    open('%s/%s.svg' % (OUT_SVG, name), 'w').write(wrap(m.group(1)))
    names.append(name)

js = """
const { chromium } = require('playwright');
const fs = require('fs');
(async () => {
  const b = await chromium.launch();
  const p = await b.newPage({ viewport: { width: 256, height: 256 }, deviceScaleFactor: 1 });
  for (const name of %s) {
    await p.setContent('<html><body style="margin:0;background:transparent">' + fs.readFileSync('%s/' + name + '.svg', 'utf8') + '</body></html>');
    await p.screenshot({ path: '%s/' + name + '.png', omitBackground: true, clip: { x: 0, y: 0, width: 256, height: 256 } });
  }
  await b.close();
})();
""" % (names, OUT_SVG, RES)
open('_render.js', 'w').write(js)
env = dict(os.environ, NODE_PATH=subprocess.check_output(['npm', 'root', '-g']).decode().strip())
subprocess.run(['node', '_render.js'], check=True, env=env)
os.remove('_render.js')
print('wrote', len(names), 'portraits')
