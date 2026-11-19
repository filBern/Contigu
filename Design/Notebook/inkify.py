#!/usr/bin/env python3
"""Turns the existing flat/gradient icon SVGs (Design/Icons/<set>/*.svg) into the notebook look: gradients flattened to their mid color, drop shadows removed,
the whole drawing wobbled (low-frequency displacement) and wrapped in a thick ink silhouette outline. Renders straight into Assets/Resources/Icons/<set>.
Usage (from this folder):  python3 inkify.py [set ...]   sets: fertilizers glyphs traits workshop growth   (glyphs: then run ../Icons/fit_glyphs.py)"""
import re, sys, os, json, subprocess
SRC = '../Icons/'; RES = '../../Assets/Resources/Icons/'
SETS = {  # set: (svg dir under ../Icons/ or an explicit path, png dir under Resources, px)
    'fertilizers': ('fertilizers', 'Fertilizers', 256), 'glyphs': ('glyphs', 'UpgradeGlyphs', 256), 'traits': ('traits', 'Traits', 128),
    'workshop': ('workshop', 'Workshop', 256), 'growth': ('growth', 'Growth', 256),
    'upgrades': ('upgrades', 'Upgrades', 256), 'mystery': ('mystery', 'Mystery', 256), 'challenges': ('challenges', 'Challenges', 0),
    'enemies': ('../Enemies/bare', '../Enemies', 256), 'weather': ('../Weather/svg', 'Weather', 128)}
INK = '#2a2420'
def mid(stops):
    cols = re.findall(r'stop-color="(#[0-9a-fA-F]{6})"', stops)
    return cols[len(cols) // 2] if cols else '#cccccc'
def flatten(svg):
    grads = {m.group(1): mid(m.group(2)) for m in re.finditer(r'<(?:linear|radial)Gradient id="([^"]+)"[^>]*>(.*?)</(?:linear|radial)Gradient>', svg, re.S)}
    svg = re.sub(r'url\(#([^)]+)\)', lambda m: grads.get(m.group(1), m.group(0)) if m.group(1) in grads else m.group(0), svg)
    return re.sub(r'\sfilter="url\(#[^)]+\)"', '', svg)
def inkify(svg, px, variant=0, thick=False):
    svg = flatten(svg)
    m = re.search(r'viewBox="([-\d.]+) ([-\d.]+) ([-\d.]+) ([-\d.]+)"', svg)
    vw = float(m.group(3)) if m else px
    u = vw / px                      # user units per output pixel
    out, wob = 2.6 * px / 256 * 1.0, 3.2 * px / 256
    if thick:   # small badges (sprout icons on tiles): a heavy outline so they read on any tile color
        out = 6.0 * px / 128
    f = ('<defs><filter id="nb" x="-12%%" y="-12%%" width="124%%" height="124%%"><feTurbulence type="fractalNoise" baseFrequency="0.03" numOctaves="1" seed="%d" result="n"/>'
         '<feDisplacementMap in="SourceGraphic" in2="n" scale="%.2f" result="w"/><feMorphology in="w" operator="dilate" radius="%.2f" result="d"/>'
         '<feFlood flood-color="%s"/><feComposite in2="d" operator="in" result="o"/><feMerge><feMergeNode in="o"/><feMergeNode in="w"/></feMerge></filter></defs>') % (4 + 7 * variant, wob * u * 2, out * u, INK)
    svg = re.sub(r'(width|height)="[\d.]+"', lambda k: '%s="%d"' % (k.group(1), px), svg, count=2)
    i = svg.index('>') + 1
    j = svg.rindex('</svg>')
    jig = ''
    if variant:   # flipbook frame: the whole drawing nudged and tilted a little
        import random
        r = random.Random(variant * 31 + px)
        jig = ' transform="translate(%.1f %.1f) rotate(%.1f %.1f %.1f)"' % (r.uniform(-1.5, 1.5) * px / 128 * u, r.uniform(-1.5, 1.5) * px / 128 * u, r.uniform(-1.8, 1.8), vw / 2, vw / 2)
    return svg[:i] + f + '<g filter="url(#nb)"><g%s>' % jig + svg[i:j] + '</g></g>' + svg[j:]
if __name__ == '__main__':
    args = sys.argv[1:]
    variants = '--variants' in args       # also write <Name>_1 / <Name>_2 (flipbook frames) next to each PNG
    names = [a for a in args if not a.startswith('--')] or list(SETS)
    jobs = []
    os.makedirs('svg_ink', exist_ok=True)
    for s in names:
        sd, pd, px = SETS[s]
        for f in sorted(os.listdir(SRC + sd)):
            if s == 'traits' and f == 'Poison.svg': continue   # redrawn by hand: generate_poison.py
            if s == 'growth': continue   # redrawn by hand: generate_markers.py (Fruit_Glow is a glow overlay)   # a glow overlay, not a drawing
            if s == 'challenges' and f.endswith('.png'):    # raster sources: wrapped in an SVG so the same ink filter applies
                import base64
                from PIL import Image
                n = f[:-4]; png = RES + pd + '/' + n + '.png'; im = Image.open(SRC + sd + '/' + f); px = im.size[0]
                b64 = base64.b64encode(open(SRC + sd + '/' + f, 'rb').read()).decode()
                p = 'svg_ink/%s_%s.svg' % (s, n)
                open(p, 'w').write(inkify('<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" viewBox="0 0 %d %d" width="%d" height="%d"><image width="%d" height="%d" href="data:image/png;base64,%s"/></svg>' % (px, px, px, px, px, px, b64), px))
                jobs.append((p, png, px, px, True)); continue
            if not f.endswith('.svg'): continue
            n = f[:-4]; png = RES + pd + '/' + n + '.png'
            if not os.path.exists(png): continue
            p = 'svg_ink/%s_%s.svg' % (s, n)
            src = open(SRC + sd + '/' + f).read()
            if not variants:
                open(p, 'w').write(inkify(src, px, thick=(s == 'traits'))); jobs.append((p, png, px, px, True))
            else:
                for k in (1, 2):
                    pk = 'svg_ink/%s_%s_%d.svg' % (s, n, k)
                    open(pk, 'w').write(inkify(src, px, k)); jobs.append((pk, RES + pd + '/%s_%d.png' % (n, k), px, px, True))
    json.dump(jobs, open('jobs_ink.json', 'w')); print(len(jobs), 'icons')
    subprocess.run(['node', 'render.js', 'jobs_ink.json'], check=True, env=dict(os.environ, NODE_PATH=subprocess.check_output(['npm', 'root', '-g']).decode().strip()))
