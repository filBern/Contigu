#!/usr/bin/env python3
"""Button body sprites in the title's visual language: chunky dark outline, glossy color fill, a light inner highlight line.
144x48 (rendered at 2x), 9-sliceable horizontally (24px end caps). No text/icons — Unity draws the label.
Output: bodies/Btn<Name>.svg"""
import os
O = '#13212e'
PALETTE = {
    'Green': ('#9ad37a', '#5a9e45', '#356f2c'),
    'Red': ('#f08a7e', '#c8403f', '#8a2a2a'),
    'Blue': ('#9bbcf5', '#4f7fd0', '#2c4d8c'),
    'Gold': ('#ffe08a', '#e8a82e', '#b0720f'),
}

def pill(name, top, mid, bot):
    return '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 144 48" width="144" height="48">
  <defs><linearGradient id="g" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="%s"/><stop offset="0.5" stop-color="%s"/><stop offset="1" stop-color="%s"/></linearGradient></defs>
  <rect x="3" y="3" width="138" height="42" rx="21" fill="url(#g)" stroke="%s" stroke-width="5"/>
  <path d="M26 12 H118" stroke="#fff" stroke-width="4" stroke-linecap="round" opacity="0.5"/>
  <path d="M26 38 H118" stroke="#000" stroke-width="3" stroke-linecap="round" opacity="0.12"/>
</svg>
''' % (top, mid, bot, O)

def wood():
    return '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 144 48" width="144" height="48">
  <defs><linearGradient id="w" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#d9a766"/><stop offset="0.5" stop-color="#c58a4a"/><stop offset="1" stop-color="#8f5b2b"/></linearGradient></defs>
  <rect x="3" y="3" width="138" height="42" rx="12" fill="url(#w)" stroke="%s" stroke-width="5"/>
  <path d="M26 17 H118 M26 31 H118" stroke="#6b4528" stroke-width="2.5" stroke-linecap="round" opacity="0.55"/>
  <path d="M26 11 H118" stroke="#fff" stroke-width="3.5" stroke-linecap="round" opacity="0.35"/>
  <circle cx="14" cy="24" r="3.2" fill="#5a3a1e" stroke="%s" stroke-width="2"/><circle cx="130" cy="24" r="3.2" fill="#5a3a1e" stroke="%s" stroke-width="2"/>
</svg>
''' % (O, O, O)

if __name__ == '__main__':
    os.makedirs('bodies', exist_ok=True)
    for n, (t, m, b) in PALETTE.items():
        open('bodies/Btn%s.svg' % n, 'w').write(pill(n, t, m, b))
    open('bodies/BtnWood.svg', 'w').write(wood())
    print('ok')
