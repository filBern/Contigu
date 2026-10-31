#!/usr/bin/env python3
"""Sprig, the garden guide: a little seed sprite with a sprout on its head. 4 expressions -> Assets/Resources/Characters/Sprig_<Expr>.png"""
import os, subprocess
O = '#13212e'
OUT = '../../Assets/Resources/Characters'
CHROME = '/opt/pw-browsers/chromium-1194/chrome-linux/chrome'
S = 'stroke="%s" stroke-width="6" stroke-linejoin="round" stroke-linecap="round"' % O
S4 = S.replace('6"', '4"', 1)

def face(expr):
    cheeks = '<ellipse cx="86" cy="158" rx="12" ry="8" fill="#ff9aa8" opacity="0.85"/><ellipse cx="170" cy="158" rx="12" ry="8" fill="#ff9aa8" opacity="0.85"/>'
    def eyes(dy=0, pupil_dy=0):
        e = ''
        for cx in (104, 152):
            e += '<ellipse cx="%d" cy="%d" rx="12" ry="15" fill="#fff7e8" stroke="%s" stroke-width="4"/>' % (cx, 138 + dy, O)
            e += '<circle cx="%d" cy="%d" r="7" fill="%s"/><circle cx="%d" cy="%d" r="2.5" fill="#fff"/>' % (cx + 1, 140 + dy + pupil_dy, O, cx + 3, 136 + dy)
        return e
    if expr == 'Happy':
        return eyes() + cheeks + '<path d="M110 166 Q128 186 146 166" fill="#7a2e2e" %s/>' % S4
    if expr == 'Neutral':
        return eyes() + cheeks + '<ellipse cx="128" cy="172" rx="8" ry="9" fill="#7a2e2e" stroke="%s" stroke-width="4"/>' % O
    if expr == 'Sad':
        brows = '<path d="M88 124 L116 112 M168 124 L140 112" stroke="%s" stroke-width="5" stroke-linecap="round"/>' % O
        tear = '<path d="M168 156 C162 168 174 168 168 156Z" fill="#7cc7e8" stroke="%s" stroke-width="2.5"/>' % O
        return eyes(pupil_dy=3) + brows + cheeks + '<path d="M112 178 Q128 164 144 178" fill="none" %s/>' % S4 + tear
    if expr == 'Cheer':
        arcs = ''.join('<path d="M%d 142 Q%d 124 %d 142" fill="none" %s/>' % (cx - 13, cx, cx + 13, S4) for cx in (104, 152))
        return arcs + cheeks + '<path d="M104 164 Q128 200 152 164Z" fill="#7a2e2e" %s/><path d="M114 178 Q128 190 142 178" fill="#ff9aa8"/>' % S4

def svg(expr):
    arms = {'Cheer': ('M72 170 Q44 140 52 112', 'M184 170 Q212 140 204 112'),
            'Sad': ('M74 178 Q62 204 70 220', 'M182 178 Q194 204 186 220')}.get(expr, ('M72 176 Q50 188 54 212', 'M184 176 Q206 188 202 212'))
    body = ('<ellipse cx="96" cy="236" rx="22" ry="10" fill="#6b4a2a" %s/><ellipse cx="160" cy="236" rx="22" ry="10" fill="#6b4a2a" %s/>' % (S, S) +
            '<path d="%s" fill="none" stroke="%s" stroke-width="16" stroke-linecap="round"/><path d="%s" fill="none" stroke="%s" stroke-width="16" stroke-linecap="round"/>' % (arms[0], O, arms[1], O) +
            '<path d="%s" fill="none" stroke="#e8c98a" stroke-width="7" stroke-linecap="round"/><path d="%s" fill="none" stroke="#e8c98a" stroke-width="7" stroke-linecap="round"/>' % (arms[0], arms[1]) +
            '<path d="M128 74 C190 74 200 150 190 196 C182 238 74 238 66 196 C56 150 66 74 128 74Z" fill="#e8c98a" %s/>' % S +
            '<path d="M84 100 C92 84 108 78 120 78" fill="none" stroke="#fff7e8" stroke-width="6" stroke-linecap="round" opacity="0.7"/>' +
            # overalls bib
            '<path d="M84 200 C84 190 96 188 128 188 C160 188 172 190 172 200 C176 226 80 226 84 200Z" fill="#5a9e45" %s/>' % S4 +
            '<circle cx="128" cy="204" r="5" fill="#ffd54a" stroke="%s" stroke-width="2.5"/>' % O +
            # sprout
            '<path d="M128 76 C128 54 130 44 134 30" fill="none" %s/>' % S.replace('stroke-width="6"', 'stroke-width="8"') +
            '<path d="M128 76 C128 54 130 44 134 30" fill="none" stroke="#5a9e45" stroke-width="3"/>' +
            '<path d="M133 36 C150 10 184 12 192 22 C186 48 158 52 133 36Z" fill="#7bc65a" %s/><path d="M138 36 C156 26 170 24 184 24" fill="none" stroke="#d9f2b0" stroke-width="3" stroke-linecap="round"/>' % S4 +
            '<path d="M131 46 C112 28 84 30 74 40 C82 62 108 64 131 46Z" fill="#5a9e45" %s/>' % S4 +
            face(expr))
    return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256">%s</svg>' % body

os.makedirs(OUT, exist_ok=True)
for e in ['Happy', 'Neutral', 'Sad', 'Cheer']:
    s = svg(e)
    open('Sprig_%s.svg' % e, 'w').write(s)
    open('_s.html', 'w').write('<html><body style="margin:0;background:transparent">%s</body></html>' % s)
    subprocess.run([CHROME, '--headless', '--no-sandbox', '--disable-gpu', '--hide-scrollbars', '--default-background-color=00000000',
                    '--window-size=256,556', '--screenshot=_s.png', 'file://' + os.path.abspath('_s.html')], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    subprocess.run(['convert', '_s.png', '-crop', '256x256+0+0', '+repage', '%s/Sprig_%s.png' % (OUT, e)])
os.remove('_s.html'); os.remove('_s.png')
