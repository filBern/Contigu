#!/usr/bin/env python3
"""One petal per piece color for the harvest (line clear) particle burst -> Assets/Resources/Icons/Petals/<PieceColor>.png (128x128, pointing up)."""
import os, subprocess
O = '#13212e'
OUT = '../../Assets/Resources/Icons/Petals'
CHROME = '/opt/pw-browsers/chromium-1194/chrome-linux/chrome'
os.makedirs(OUT, exist_ok=True)

def petal(path, fill, vein, dark):
    return ('<path d="%s" fill="%s" stroke="%s" stroke-width="7" stroke-linejoin="round"/>' % (path, fill, O) +
            '<path d="M64 112 L64 40" stroke="%s" stroke-width="5" stroke-linecap="round" opacity="0.7"/>' % vein)

P = {
    # poppy: broad, rounded, slightly heart-notched
    'Coral': petal('M64 116 C20 100 10 50 36 24 C48 14 58 20 64 28 C70 20 80 14 92 24 C118 50 108 100 64 116Z', '#d9433f', '#f08a76', '#7a1f22'),
    # bluebell/cornflower: narrow with a notched tip
    'Teal': petal('M64 118 C38 92 36 50 50 22 L64 34 L78 22 C92 50 90 92 64 118Z', '#5b86e0', '#c9d9ff', '#1d3470'),
    # sunflower: long pointed
    'Violet': petal('M64 120 C34 90 38 44 64 8 C90 44 94 90 64 120Z', '#f1b92a', '#fff0a8', '#8a5a10'),
    # fern: leaf with a point
    'Lime': petal('M64 118 C24 96 22 50 64 10 C106 50 104 96 64 118Z', '#5a9e45', '#d9f2b0', '#1f4a1c'),
    # lavender: soft oval
    'Joker': petal('M64 118 C30 100 30 40 64 14 C98 40 98 100 64 118Z', '#b58be0', '#efe0ff', '#3d2466'),
}
for name, body in P.items():
    svg = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128" width="128" height="128">%s</svg>' % body
    open('Petal_%s.svg' % name, 'w').write(svg)
    open('_p.html', 'w').write('<html><body style="margin:0;background:transparent">%s</body></html>' % svg)
    subprocess.run([CHROME, '--headless', '--no-sandbox', '--disable-gpu', '--hide-scrollbars', '--default-background-color=00000000',
                    '--window-size=128,428', '--screenshot=_p.png', 'file://' + os.path.abspath('_p.html')], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    subprocess.run(['convert', '_p.png', '-crop', '128x128+0+0', '+repage', '%s/%s.png' % (OUT, name)])
os.remove('_p.html'); os.remove('_p.png')
