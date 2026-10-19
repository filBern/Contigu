#!/usr/bin/env python3
"""Splits each colored tile SVG into a Base (frame + soil, no flower) and an Emblem (the flower alone, unclipped, transparent) so the
landing bloom animation can pop only the flower while the frame stays at full size (see GridCellView.LandingSquashRoutine).
Writes 128px PNGs + .meta into Assets/Resources/Tiles/Bloom. Run from this folder: python3 split_tiles_for_bloom.py"""
import os, re, shutil, subprocess, uuid
CHROME = '/opt/pw-browsers/chromium-1194/chrome-linux/chrome'
OUT = '../../Assets/Resources/Tiles/Bloom'
META_SRC = '../../Assets/Resources/Tiles/Ember.png.meta'
FOLDER_META_SRC = '../../Assets/Resources/Tiles.meta'

def render(svg, png):
    open('_t.html', 'w').write('<html><body style="margin:0;background:transparent">%s</body></html>' % svg)
    subprocess.run([CHROME, '--headless', '--no-sandbox', '--disable-gpu', '--hide-scrollbars', '--default-background-color=00000000',
                    '--window-size=256,556', '--screenshot=_t.png', 'file://' + os.path.abspath('_t.html')], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    subprocess.run(['convert', '_t.png', '-crop', '256x256+0+0', '+repage', '-resize', '128x128', png])

def write_meta(path, src):
    text = open(src).read()
    text = re.sub(r'^guid: \w+', 'guid: ' + uuid.uuid4().hex, text, count=1, flags=re.M)
    open(path + '.meta', 'w').write(text)

os.makedirs(OUT, exist_ok=True)
if not os.path.exists(OUT + '.meta'):
    write_meta(OUT, FOLDER_META_SRC)
for name in ['Ember', 'Frost', 'Gold', 'Moss', 'Joker']:
    svg = open(name + '.svg').read()
    i = svg.index('<g clip-path')
    head, group = svg[:i], svg[i:]
    base = head + '</svg>'
    # Emblem: drop the frame/soil rects (keep defs for gradients) and the clip — Unity masks it to the soil area instead.
    defs = re.search(r'<defs>.*?</defs>', head, re.S).group(0)
    group = group.replace(' clip-path="url(#c)"', '')
    emblem = head[:head.index('<defs>')] + defs + group
    for suffix, s in (('Base', base), ('Emblem', emblem)):
        png = '%s/%s_%s.png' % (OUT, name, suffix)
        render(s, png)
        if not os.path.exists(png + '.meta'):
            write_meta(png, META_SRC)
    print('wrote', name)
os.remove('_t.html'); os.remove('_t.png')
