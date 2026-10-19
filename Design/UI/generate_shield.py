#!/usr/bin/env python3
"""Enemy shield overlay (Shield.png) and a break shard (ShieldShard.png), written to Assets/Resources/Icons. Run from this folder."""
import subprocess, os
CH = '/opt/pw-browsers/chromium-1194/chrome-linux/chrome'
OUT = '../../Assets/Resources/Icons'
O = '#13212e'
shield_path = 'M128 14 C170 40 212 44 230 50 C234 130 206 200 128 244 C50 200 22 130 26 50 C44 44 86 40 128 14 Z'
shield = ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256">'
          '<defs><linearGradient id="g" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#9be8d0" stop-opacity="0.78"/><stop offset="1" stop-color="#3a9a8a" stop-opacity="0.62"/></linearGradient></defs>'
          '<path d="%s" fill="url(#g)" stroke="%s" stroke-width="10" stroke-linejoin="round"/>'
          '<path d="M128 40 C160 58 190 64 208 68 C208 130 188 184 128 220 C68 184 48 130 48 68 C66 64 96 58 128 40 Z" fill="none" stroke="#e8fff6" stroke-width="5" stroke-linejoin="round" opacity="0.8"/>'
          '<path d="M128 170 L128 96 M128 126 C108 120 96 106 96 90 C114 92 126 104 128 126 M128 146 C148 140 160 126 160 110 C142 112 130 124 128 146" fill="none" stroke="%s" stroke-width="9" stroke-linecap="round" stroke-linejoin="round"/>'
          '<path d="M128 170 L128 96 M128 126 C108 120 96 106 96 90 C114 92 126 104 128 126 M128 146 C148 140 160 126 160 110 C142 112 130 124 128 146" fill="none" stroke="#e8fff6" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"/>'
          '</svg>') % (shield_path, O, O)
shard = ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256">'
         '<defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#9be8d0"/><stop offset="1" stop-color="#3a9a8a"/></linearGradient></defs>'
         '<path d="M70 30 L214 90 L120 226 Z" fill="url(#g)" stroke="%s" stroke-width="12" stroke-linejoin="round"/>'
         '<path d="M96 64 L176 98" stroke="#e8fff6" stroke-width="8" stroke-linecap="round" opacity="0.8"/></svg>') % O
for name, svg in (('Shield', shield), ('ShieldShard', shard)):
    open('%s.svg' % name, 'w').write(svg)
    open('/tmp/%s.html' % name, 'w').write('<html><body style="margin:0;background:transparent">%s</body></html>' % svg)
    png = '%s/%s.png' % (OUT, name)
    subprocess.run([CH, '--headless', '--no-sandbox', '--disable-gpu', '--default-background-color=00000000', '--screenshot=' + png, '--window-size=256,556', 'file:///tmp/%s.html' % name], check=True, capture_output=True)
    subprocess.run(['convert', png, '-crop', '256x256+0+0', '+repage', png], check=True)
