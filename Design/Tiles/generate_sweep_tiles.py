#!/usr/bin/env python3
"""Square-cornered, edge-to-edge variants of the four bloom tiles for the menu->game transition sweep (no gaps between
neighbours). Derives them from the <Name>.svg files here; writes Sweep_<Name>.svg + 128px PNGs into Assets/Resources/Tiles/Sweep.
Run from this folder: python3 generate_sweep_tiles.py"""
import os, re, subprocess
CHROME = '/opt/pw-browsers/chromium-1194/chrome-linux/chrome'
OUT = '../../Assets/Resources/Tiles/Sweep'
os.makedirs(OUT, exist_ok=True)
for n in ('Ember', 'Frost', 'Gold', 'Moss'):
    s = open(n + '.svg').read()
    s = re.sub(r'<rect x="6" y="6" width="244" height="244" rx="28" fill="url\(#b\)" stroke="[^"]*" stroke-width="6"/>',
               '<rect x="0" y="0" width="256" height="256" fill="url(#b)"/>', s)
    open('Sweep_%s.svg' % n, 'w').write(s)
    html = '<html><body style="margin:0;background:transparent">%s</body></html>' % s.replace('width="256" height="256"', 'width="128" height="128"', 1)
    open('/tmp/sw_%s.html' % n, 'w').write(html)
    subprocess.run([CHROME, '--headless', '--no-sandbox', '--disable-gpu', '--default-background-color=00000000',
                    '--screenshot=%s/%s.png' % (OUT, n), '--window-size=128,428', 'file:///tmp/sw_%s.html' % n], check=True, capture_output=True)
    subprocess.run(['convert', '%s/%s.png' % (OUT, n), '-crop', '128x128+0+0', '+repage', '%s/%s.png' % (OUT, n)], check=True)
