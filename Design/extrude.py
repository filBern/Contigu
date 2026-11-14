#!/usr/bin/env python3
"""Faux-3D extrusion for icons: the art is shrunk a little and a darkened copy of its own silhouette is stacked a few pixels lower behind it, so the icon looks like
a thick block seen from slightly above (works for any outline: chips, shields, shards). Re-renders the PNGs the game loads (same size, same name; the .meta files
stay) with Playwright (NODE_PATH=$(npm root -g)). Sources: the SVGs written by Design/Icons/generate_*.py and Design/UI. Run from this folder."""
import glob, os, re, subprocess

SCALE = 0.93
DEPTH = 0.055      # extrusion depth, as a share of the canvas height
LAYERS = 6

# (svg folder, png folder, optional name filter)
SETS = [
    ('Icons/traits', '../Assets/Resources/Icons/Traits'),
    ('Icons/glyphs', '../Assets/Resources/Icons/UpgradeGlyphs'),
    ('Icons/upgrades', '../Assets/Resources/Icons/Upgrades'),
    ('UI', '../Assets/Resources/Icons', ['Shield', 'ShieldShard']),
    ('Characters', '../Assets/Resources/Characters'),
    ('Title', '../Assets/Resources/Title'),
]
OUT = 'extruded'
os.makedirs(OUT, exist_ok=True)

def wrap(src, w, h):
    m = re.search(r'<svg([^>]*)>(.*)</svg>', src, re.S)
    attrs, inner = m.group(1), m.group(2)
    vb = re.search(r'viewBox="([^"]+)"', attrs)
    vbox = vb.group(1) if vb else '0 0 %g %g' % (w, h)
    x0 = (w - w * SCALE) / 2
    y0 = h * 0.015
    step = h * DEPTH / LAYERS
    layers = ''
    for k in range(LAYERS, 0, -1):
        layers += '<svg x="%.2f" y="%.2f" width="%.2f" height="%.2f" viewBox="%s" filter="url(#dk)">%s</svg>' % (x0, y0 + k * step, w * SCALE, h * SCALE, vbox, inner)
    front = '<svg x="%.2f" y="%.2f" width="%.2f" height="%.2f" viewBox="%s">%s</svg>' % (x0, y0, w * SCALE, h * SCALE, vbox, inner)
    return ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 %g %g" width="%g" height="%g"><defs>'
            '<filter id="dk" color-interpolation-filters="sRGB"><feColorMatrix type="matrix" values="0.32 0 0 0 0  0 0.30 0 0 0  0 0 0.34 0 0  0 0 0 1 0"/></filter></defs>%s%s</svg>'
            ) % (w, h, w, h, layers, front)

jobs = []
for entry in SETS:
    svg_dir, png_dir = entry[0], entry[1]
    only = entry[2] if len(entry) > 2 else None
    for png in sorted(glob.glob(png_dir + '/*.png')):
        name = os.path.splitext(os.path.basename(png))[0]
        if only is not None and name not in only:
            continue
        svg = '%s/%s.svg' % (svg_dir, name)
        if not os.path.exists(svg):
            continue
        w, h = [int(v) for v in subprocess.check_output(['identify', '-format', '%w %h', png]).decode().split()]
        key = '%s__%s' % (os.path.basename(png_dir), name)
        open('%s/%s.svg' % (OUT, key), 'w').write(wrap(open(svg).read(), w, h))
        jobs.append([key, png, w, h])

js = """
const { chromium } = require('playwright');
const fs = require('fs');
const jobs = %s;
(async () => {
  const b = await chromium.launch();
  for (const [key, out, w, h] of jobs) {
    const p = await b.newPage({ viewport: { width: w, height: h }, deviceScaleFactor: 1 });
    await p.setContent('<html><body style="margin:0;background:transparent">' + fs.readFileSync('%s/' + key + '.svg', 'utf8') + '</body></html>');
    await p.screenshot({ path: out, omitBackground: true, clip: { x: 0, y: 0, width: w, height: h } });
    await p.close();
  }
  await b.close();
})();
""" % (jobs, OUT)
open('_render.js', 'w').write(js)
env = dict(os.environ, NODE_PATH=subprocess.check_output(['npm', 'root', '-g']).decode().strip())
subprocess.run(['node', '_render.js'], check=True, env=env)
os.remove('_render.js')
print('extruded', len(jobs), 'icons')
