#!/usr/bin/env python3
"""Simple Pollen icon: one flat golden grain with six small bumps and a single highlight.
Writes Pollen.svg + Pollen_256.png here and a 256px copy into Assets/Resources/Icons. Run from this folder."""
import math, subprocess, shutil
O, FILL, HI = '#13212e', '#f6c93a', '#fff3b8'
bumps = ''.join('<circle cx="%.1f" cy="%.1f" r="22" fill="%s"/>' % (128 + 70 * math.cos(math.radians(60 * i - 90)), 128 + 70 * math.sin(math.radians(60 * i - 90)), FILL) for i in range(6))
bumps_o = ''.join('<circle cx="%.1f" cy="%.1f" r="22" fill="%s" stroke="%s" stroke-width="12"/>' % (128 + 70 * math.cos(math.radians(60 * i - 90)), 128 + 70 * math.sin(math.radians(60 * i - 90)), O, O) for i in range(6))
svg = ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256">'
       + bumps_o + '<circle cx="128" cy="128" r="62" fill="%s" stroke="%s" stroke-width="12"/>' % (O, O)
       + bumps + '<circle cx="128" cy="128" r="58" fill="%s"/>' % FILL
       + '<ellipse cx="108" cy="108" rx="13" ry="9" transform="rotate(-35 108 108)" fill="%s"/></svg>' % HI)
open('Pollen.svg', 'w').write(svg)
open('/tmp/pollen.html', 'w').write('<html><body style="margin:0;background:transparent">%s</body></html>' % svg)
subprocess.run(['/opt/pw-browsers/chromium-1194/chrome-linux/chrome', '--headless', '--no-sandbox', '--disable-gpu', '--default-background-color=00000000',
                '--screenshot=Pollen_256.png', '--window-size=256,556', 'file:///tmp/pollen.html'], check=True, capture_output=True)
subprocess.run(['convert', 'Pollen_256.png', '-crop', '256x256+0+0', '+repage', 'Pollen_256.png'], check=True)
shutil.copy('Pollen_256.png', '../../Assets/Resources/Icons/Pollen.png')
