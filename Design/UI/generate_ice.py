#!/usr/bin/env python3
"""Ice shard (Assets/Resources/Icons/IceShard.png) and the flash that bursts when a frozen plot breaks (IceFlash.png), 96px.
Run from this folder: python3 generate_ice.py"""
import os, subprocess
O = '#13212e'
CHROME = '/opt/pw-browsers/chromium-1194/chrome-linux/chrome'
OUT = '../../Assets/Resources/Icons'

shard = ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="96" height="96">'
         '<defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#f4fcff"/><stop offset="0.55" stop-color="#9fd8f5"/><stop offset="1" stop-color="#4f9fd6"/></linearGradient></defs>'
         '<path d="M96 14 L206 120 L150 240 L50 150 Z" fill="url(#g)" stroke="%s" stroke-width="12" stroke-linejoin="round"/>'
         '<path d="M96 14 L128 128 L50 150 M128 128 L206 120" fill="none" stroke="#ffffff" stroke-width="7" stroke-linejoin="round" opacity="0.85"/></svg>') % O
flash = ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="96" height="96">'
         '<defs><radialGradient id="r"><stop offset="0" stop-color="#ffffff" stop-opacity="0.95"/><stop offset="0.5" stop-color="#bfe8ff" stop-opacity="0.55"/><stop offset="1" stop-color="#bfe8ff" stop-opacity="0"/></radialGradient></defs>'
         '<circle cx="128" cy="128" r="124" fill="url(#r)"/>'
         '<path d="M128 8 L146 110 L248 128 L146 146 L128 248 L110 146 L8 128 L110 110 Z" fill="#ffffff" opacity="0.9"/></svg>')
for name, svg in (('IceShard', shard), ('IceFlash', flash)):
    html = '/tmp/_ice_%s.html' % name
    open(html, 'w').write('<html><body style="margin:0;background:transparent">%s</body></html>' % svg)
    png = '%s/%s.png' % (OUT, name)
    subprocess.run([CHROME, '--headless', '--no-sandbox', '--disable-gpu', '--hide-scrollbars', '--default-background-color=00000000', '--screenshot=' + png,
                    '--window-size=96,396', 'file://' + html], check=True, capture_output=True)
    subprocess.run(['convert', png, '-crop', '96x96+0+0', '+repage', png], check=True)
print('ice art written')
