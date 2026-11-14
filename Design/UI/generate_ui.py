#!/usr/bin/env python3
"""Garden-style UI parts (same chunky outline look as Design/Tiles): panel card, bar track + fills, badge, slider knob.
Renders each SVG to PNG with headless Chromium and copies it to Assets/Resources/UI/Garden. Run from this folder."""
import os, subprocess, shutil
O = '#13212e'
CHROME = '/opt/pw-browsers/chromium-1194/chrome-linux/chrome'
OUT = '../../Assets/Resources/UI/Garden'

def wrap(w, h, body):
    return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 %d %d" width="%d" height="%d">%s</svg>' % (w, h, w, h, body)

def card():  # 9-slice, border 28 @128px. Light parchment so per-slot tints stay readable
    return 128, 128, ('<rect x="5" y="5" width="118" height="118" rx="24" fill="#f6ecd2" stroke="%s" stroke-width="6"/>' % O +
        '<rect x="14" y="14" width="100" height="100" rx="16" fill="none" stroke="#ffffff" stroke-width="3" opacity="0.55"/>' +
        '<path d="M16 100 C40 112 88 112 112 100" fill="none" stroke="#b79b62" stroke-width="4" opacity="0.5"/>')

def track():  # soil trough, 9-slice border 28/24 @246x54
    return 246, 54, ('<rect x="4" y="4" width="238" height="46" rx="23" fill="#2a1c12" stroke="%s" stroke-width="6"/>' % O +
        '<rect x="12" y="10" width="222" height="12" rx="6" fill="#000" opacity="0.28"/>')

def fill(c1, c2, hi):  # glossy leaf-colored pill, 9-slice border 20/16 @238x40
    return 238, 40, ('<defs><linearGradient id="g" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="%s"/><stop offset="1" stop-color="%s"/></linearGradient></defs>' % (c1, c2) +
        '<rect x="3" y="3" width="232" height="34" rx="17" fill="url(#g)" stroke="%s" stroke-width="5"/>' % O +
        '<rect x="14" y="8" width="210" height="8" rx="4" fill="%s" opacity="0.55"/>' % hi)

def badge():  # round seed-count badge
    return 50, 50, ('<circle cx="25" cy="25" r="21" fill="#e8a82e" stroke="%s" stroke-width="5"/>' % O +
        '<ellipse cx="20" cy="15" rx="10" ry="5" fill="#fff7e8" opacity="0.6"/>')

def knob():  # flower-bud slider knob
    s = '<g transform="translate(25 25)">'
    for a in range(0, 360, 72):
        s += '<ellipse cx="0" cy="-12" rx="7" ry="9" fill="#ff9aa8" stroke="%s" stroke-width="3" transform="rotate(%d)"/>' % (O, a)
    s += '<circle r="9" fill="#ffd54a" stroke="%s" stroke-width="3.5"/></g>' % O
    return 50, 50, s

PARTS = {
    'Card': card(), 'BarTrack': track(), 'CountBadge': badge(), 'SliderHandle': knob(),
    'FillGold': fill('#ffd54a', '#e8a82e', '#fff7e8'),
    'FillBlue': fill('#7cc7e8', '#3b8cc2', '#e6f6ff'),
    'FillGreen': fill('#8fd16a', '#4a9a3a', '#eaffd8'),
}
SCALE = 2
for name, (w, h, body) in PARTS.items():
    svg = wrap(w, h, body)
    open(name + '.svg', 'w').write(svg)
    html = '<html><body style="margin:0;background:transparent"><div style="width:%dpx;height:%dpx;zoom:%d">%s</div></body></html>' % (w, h, SCALE, svg)
    open('_t.html', 'w').write(html)
    subprocess.run([CHROME, '--headless', '--no-sandbox', '--disable-gpu', '--hide-scrollbars', '--default-background-color=00000000',
                    '--window-size=%d,%d' % (w * SCALE, h * SCALE + 300), '--screenshot=_t.png', 'file://' + os.path.abspath('_t.html')],
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    subprocess.run(['convert', '_t.png', '-crop', '%dx%d+0+0' % (w * SCALE, h * SCALE), '+repage', '%s/%s.png' % (OUT, name)])
    print('wrote', name)
os.remove('_t.html'); os.remove('_t.png')
