#!/usr/bin/env python3
"""Per-color enemy-hit projectiles (pointing right, 128x128). Output: Assets/Resources/Icons/Attacks/<PieceColor>.png"""
import os, subprocess
O = '#13212e'
OUT = '../../Assets/Resources/Icons/Attacks'
CHROME = '/opt/pw-browsers/chromium-1194/chrome-linux/chrome'
os.makedirs(OUT, exist_ok=True)
st = 'stroke="%s" stroke-width="6" stroke-linejoin="round" stroke-linecap="round"' % O

# Coral / Poppy: fireball with trailing flames (ember)
coral = ('<path d="M10 64 C28 46 36 52 44 30 C58 40 66 36 74 22 C100 34 118 52 118 64 C118 78 100 96 74 106 C66 92 58 88 44 98 C36 76 28 82 10 64Z" fill="#e0502f" %s/>' % st +
         '<path d="M46 64 C60 50 78 52 96 64 C78 76 60 78 46 64Z" fill="#ffd54a"/>' +
         '<path d="M58 64 C68 58 78 59 86 64 C78 69 68 70 58 64Z" fill="#fff7e8"/>')
# Teal / Bluebell: ice shard
teal = ('<path d="M8 64 L40 40 L76 50 L120 64 L76 78 L40 88Z" fill="#7cc7e8" %s/>' % st +
        '<path d="M40 40 L58 64 L40 88 M58 64 L120 64" fill="none" stroke="#e6f6ff" stroke-width="4" stroke-linecap="round"/>' +
        '<path d="M76 50 L88 64 L76 78" fill="none" stroke="#3b8cc2" stroke-width="4" stroke-linecap="round"/>')
# Violet / Sunflower: spiked solar saw-blade with an angry seed core
import math
def spikes(n, r_out, r_in, off=0):
    pts = []
    for i in range(n * 2):
        r = r_out if i % 2 == 0 else r_in
        a = math.pi * i / n + off
        pts.append('%.1f %.1f' % (64 + r * math.cos(a), 64 + r * math.sin(a)))
    return 'M' + ' L'.join(pts) + 'Z'
violet = ('<path d="%s" fill="#e8721e" %s/>' % (spikes(10, 62, 36, math.pi / 10), st) +
          '<path d="%s" fill="#ffd54a" %s/>' % (spikes(10, 54, 30), st.replace('6"', '4"', 1)) +
          '<circle cx="64" cy="64" r="26" fill="#5a3416" %s/>' % st +
          '<circle cx="64" cy="64" r="9" fill="#ffb347" stroke="%s" stroke-width="3"/>' % O)

# Lime / Fern: thorny leaf dart
lime = ('<path d="M8 64 C30 30 80 28 120 64 C80 100 30 98 8 64Z" fill="#5a9e45" %s/>' % st +
        '<path d="M14 64 L112 64" stroke="#d9f2b0" stroke-width="5" stroke-linecap="round"/>' +
        '<path d="M40 64 L56 46 M60 64 L78 48 M40 64 L56 82 M60 64 L78 80" stroke="#d9f2b0" stroke-width="4" stroke-linecap="round"/>' +
        '<path d="M96 40 L104 26 M96 88 L104 102" stroke="%s" stroke-width="5" stroke-linecap="round"/>' % O)
# Joker / Lavender: sparkling star
star = 'M64 8 L78 48 L120 52 L86 78 L98 120 L64 96 L30 120 L42 78 L8 52 L50 48Z'
joker = ('<path d="%s" fill="#b58be0" %s/>' % (star, st) +
         '<circle cx="64" cy="66" r="12" fill="#fff7e8" stroke="%s" stroke-width="4"/>' % O)

for name, body in {'Coral': coral, 'Teal': teal, 'Violet': violet, 'Lime': lime, 'Joker': joker}.items():
    svg = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128" width="128" height="128">%s</svg>' % body
    open(name + '_attack.svg', 'w').write(svg)
    open('_a.html', 'w').write('<html><body style="margin:0;background:transparent"><div style="width:128px;height:128px;zoom:2">%s</div></body></html>' % svg)
    subprocess.run([CHROME, '--headless', '--no-sandbox', '--disable-gpu', '--hide-scrollbars', '--default-background-color=00000000',
                    '--window-size=256,556', '--screenshot=_a.png', 'file://' + os.path.abspath('_a.html')], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    subprocess.run(['convert', '_a.png', '-crop', '256x256+0+0', '+repage', '%s/%s.png' % (OUT, name)])
os.remove('_a.html'); os.remove('_a.png')
