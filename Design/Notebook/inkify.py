#!/usr/bin/env python3
"""Turns the existing flat/gradient icon SVGs (Design/Icons/<set>/*.svg) into the notebook look: gradients flattened to their mid color, drop shadows removed,
the whole drawing wobbled (low-frequency displacement) and wrapped in a thick ink silhouette outline. Renders straight into Assets/Resources/Icons/<set>.
Usage (from this folder):  python3 inkify.py [set ...]   sets: fertilizers glyphs traits workshop growth   (glyphs: then run ../Icons/fit_glyphs.py)"""
import re, sys, os, json, subprocess
SRC = '../Icons/'; RES = '../../Assets/Resources/Icons/'
SETS = {  # set: (svg dir, png dir, px)
    'fertilizers': ('fertilizers', 'Fertilizers', 256), 'glyphs': ('glyphs', 'UpgradeGlyphs', 256), 'traits': ('traits', 'Traits', 128),
    'workshop': ('workshop', 'Workshop', 256), 'growth': ('growth', 'Growth', 256)}
INK = '#2a2420'
def mid(stops):
    cols = re.findall(r'stop-color="(#[0-9a-fA-F]{6})"', stops)
    return cols[len(cols) // 2] if cols else '#cccccc'
def flatten(svg):
    grads = {m.group(1): mid(m.group(2)) for m in re.finditer(r'<(?:linear|radial)Gradient id="([^"]+)"[^>]*>(.*?)</(?:linear|radial)Gradient>', svg, re.S)}
    svg = re.sub(r'url\(#([^)]+)\)', lambda m: grads.get(m.group(1), m.group(0)) if m.group(1) in grads else m.group(0), svg)
    return re.sub(r'\sfilter="url\(#[^)]+\)"', '', svg)
def inkify(svg, px):
    svg = flatten(svg)
    m = re.search(r'viewBox="([-\d.]+) ([-\d.]+) ([-\d.]+) ([-\d.]+)"', svg)
    vw = float(m.group(3)) if m else px
    u = vw / px                      # user units per output pixel
    out, wob = 2.6 * px / 256 * 1.0, 3.2 * px / 256
    f = ('<defs><filter id="nb" x="-12%%" y="-12%%" width="124%%" height="124%%"><feTurbulence type="fractalNoise" baseFrequency="0.03" numOctaves="1" seed="4" result="n"/>'
         '<feDisplacementMap in="SourceGraphic" in2="n" scale="%.2f" result="w"/><feMorphology in="w" operator="dilate" radius="%.2f" result="d"/>'
         '<feFlood flood-color="%s"/><feComposite in2="d" operator="in" result="o"/><feMerge><feMergeNode in="o"/><feMergeNode in="w"/></feMerge></filter></defs>') % (wob * u * 2, out * u, INK)
    svg = re.sub(r'(width|height)="[\d.]+"', lambda k: '%s="%d"' % (k.group(1), px), svg, count=2)
    i = svg.index('>') + 1
    j = svg.rindex('</svg>')
    return svg[:i] + f + '<g filter="url(#nb)">' + svg[i:j] + '</g>' + svg[j:]
if __name__ == '__main__':
    names = sys.argv[1:] or list(SETS)
    jobs = []
    os.makedirs('svg_ink', exist_ok=True)
    for s in names:
        sd, pd, px = SETS[s]
        for f in sorted(os.listdir(SRC + sd)):
            if not f.endswith('.svg'): continue
            n = f[:-4]; png = RES + pd + '/' + n + '.png'
            if not os.path.exists(png): continue
            p = 'svg_ink/%s_%s.svg' % (s, n)
            open(p, 'w').write(inkify(open(SRC + sd + '/' + f).read(), px)); jobs.append((p, png, px, px, True))
    json.dump(jobs, open('jobs_ink.json', 'w')); print(len(jobs), 'icons')
    subprocess.run(['node', 'render.js', 'jobs_ink.json'], check=True, env=dict(os.environ, NODE_PATH=subprocess.check_output(['npm', 'root', '-g']).decode().strip()))
