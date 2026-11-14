#!/usr/bin/env python3
"""Bare enemy icons: the creature alone, with no plate, no dark outline and no block: the SVGs written by generate_enemies.py lose their rounded plate and the
navy outline of their shapes (Icons/outline.py), and get a soft cream glow (so the dark ones still read on a dark panel) and a soft shadow under them.
Renders 256 px PNGs with Playwright (NODE_PATH=$(npm root -g)) into Assets/Resources/Enemies/<Name>.png. Run from this folder, after generate_enemies.py."""
import glob, os, re, subprocess, sys
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Icons'))
from outline import strip_outline

O = '#13212e'
RES = '../../Assets/Resources/Enemies'
OUT = 'bare'
DARK_BELOW = 0.42   # mean luminance under which a creature gets the pale halo
os.makedirs(OUT, exist_ok=True)

PLATE = re.compile(r'\s*<rect[^>]*fill="url\(#plate\)"[^>]*/>')
UNDERLAY = re.compile(r'<path d="([^"]+)" fill="none" stroke="%s" stroke-width="[^"]+"\s*/>\s*(?=<path d="\1")' % O)

def bare(src, glow=True):
    inner = re.search(r'<svg[^>]*>(.*)</svg>', src, re.S).group(1)
    inner = re.sub(r'<defs>.*?</defs>', lambda m: m.group(0) if ('Gradient' in m.group(0) and 'plate' not in m.group(0)) else re.sub(r'<(radial|linear)Gradient id="plate".*?</(radial|linear)Gradient>|<clipPath id="clip">.*?</clipPath>', '', m.group(0), flags=re.S), inner, flags=re.S)
    inner = PLATE.sub('', inner)
    inner = re.sub(r'\s*<rect x="8" y="8" width="240" height="240"[^>]*/>', '', inner)           # the plate's frame
    inner = re.sub(r'<rect x="0" y="0" width="256" height="256"[^>]*/>', '', inner)               # a full-canvas tint (the enraged crow's red wash)
    inner = inner.replace(' clip-path="url(#clip)"', '')
    inner = UNDERLAY.sub('', inner)          # a thick dark copy of a line, drawn under the colored one: that was its outline
    inner = strip_outline(inner)
    return ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256"><defs>'
            '<filter id="glow" x="-20%" y="-20%" width="140%" height="140%"><feDropShadow dx="0" dy="0" stdDeviation="3" flood-color="#fff1d0" flood-opacity="@GLOW@"/></filter>'
            '<filter id="shade" x="-20%" y="-20%" width="140%" height="150%"><feDropShadow dx="0" dy="6" stdDeviation="4" flood-color="#1a0f08" flood-opacity="0.5"/></filter>'
            '</defs><g filter="url(#shade)"><g filter="url(#glow)">@@</g></g></svg>\n').replace('@@', inner).replace('@GLOW@', '0.7' if glow else '0')

def render(names, folder, dest):
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
""" % (names, folder, dest)
    open('_render.js', 'w').write(js)
    env = dict(os.environ, NODE_PATH=subprocess.check_output(['npm', 'root', '-g']).decode().strip())
    subprocess.run(['node', '_render.js'], check=True, env=env)
    os.remove('_render.js')

def luminance(png):
    """Mean luminance (0-1) of the opaque pixels of a PNG."""
    from PIL import Image
    im = Image.open(png).convert('RGBA')
    total = n = 0
    for r, g, b, a in im.getdata():
        if a > 200:
            total += 0.299 * r + 0.587 * g + 0.114 * b
            n += 1
    return total / n / 255 if n else 1

names = [os.path.splitext(p)[0] for p in sorted(glob.glob('*.svg')) if os.path.exists('%s/%s.png' % (RES, os.path.splitext(p)[0]))]
# First pass without the glow, to know which creatures are dark: only those get a pale halo (on a dark panel a halo around a pale creature just washes it out).
tmp = OUT + '/_probe'
os.makedirs(tmp, exist_ok=True)
for name in names:
    open('%s/%s.svg' % (OUT, name), 'w').write(bare(open(name + '.svg').read(), glow=False))
render(names, OUT, tmp)
dark = [n for n in names if luminance('%s/%s.png' % (tmp, n)) < DARK_BELOW]
for name in names:
    open('%s/%s.svg' % (OUT, name), 'w').write(bare(open(name + '.svg').read(), glow=name in dark))
render(names, OUT, RES)
for f in glob.glob(tmp + '/*.png'):
    os.remove(f)
os.rmdir(tmp)
print('bare', len(names), 'enemies; haloed (dark):', ', '.join(dark))
