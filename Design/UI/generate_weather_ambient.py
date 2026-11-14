#!/usr/bin/env python3
"""Weather icons (Assets/Resources/Icons/Weather/<WeatherId>.png, 128px) and ambient garden sprites (Assets/Resources/Ambient/<Name>.png, 96px).
Run from this folder: python3 generate_weather_ambient.py"""
import math, os, subprocess
O = '#13212e'
CHROME = '/opt/pw-browsers/chromium-1194/chrome-linux/chrome'
RES = '../../Assets/Resources'

def svg(body, size=256):
    return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="%d" height="%d">%s</svg>' % (size, size, body)

def badge(top, bottom, glyph):
    return svg('<defs><linearGradient id="b" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="%s"/><stop offset="1" stop-color="%s"/></linearGradient></defs>'
               '<rect x="10" y="10" width="236" height="236" rx="52" fill="url(#b)" stroke="%s" stroke-width="10"/>%s' % (top, bottom, O, glyph))

def cloud(cx, cy, s, fill='#f4f8ff'):
    return ('<g transform="translate(%g %g) scale(%g)" stroke="%s" stroke-width="%g" stroke-linejoin="round" fill="%s">'
            '<path d="M-70 24 C-98 24 -98 -18 -62 -18 C-62 -52 -12 -62 6 -34 C28 -56 78 -38 66 -2 C98 -2 98 24 66 24 Z"/></g>') % (cx, cy, s, O, 8 / s, fill)

def sun_rays(cx, cy, r1, r2, n, w):
    out = ''
    for i in range(n):
        a = 2 * math.pi * i / n
        out += '<path d="M%.1f %.1f L%.1f %.1f" stroke="%s" stroke-width="%g" stroke-linecap="round"/>' % (cx + r1 * math.cos(a), cy + r1 * math.sin(a), cx + r2 * math.cos(a), cy + r2 * math.sin(a), O, w + 10)
    for i in range(n):
        a = 2 * math.pi * i / n
        out += '<path d="M%.1f %.1f L%.1f %.1f" stroke="#ffd45a" stroke-width="%g" stroke-linecap="round"/>' % (cx + r1 * math.cos(a), cy + r1 * math.sin(a), cx + r2 * math.cos(a), cy + r2 * math.sin(a), w)
    return out

def sun(cx, cy, r):
    return sun_rays(cx, cy, r + 14, r + 44, 8, 12) + '<circle cx="%g" cy="%g" r="%g" fill="#ffc83a" stroke="%s" stroke-width="8"/><circle cx="%g" cy="%g" r="%g" fill="#ffe58a" opacity="0.8"/>' % (cx, cy, r, O, cx - r * 0.3, cy - r * 0.3, r * 0.35)

def drop(cx, cy, s, fill='#6cc7f0'):
    return '<path transform="translate(%g %g) scale(%g)" d="M0 -22 C14 -4 18 6 18 12 C18 24 9 30 0 30 C-9 30 -18 24 -18 12 C-18 6 -14 -4 0 -22 Z" fill="%s" stroke="%s" stroke-width="%g"/>' % (cx, cy, s, fill, O, 6 / s)

def snowflake(cx, cy, r, col='#ffffff'):
    out = ''
    for i in range(6):
        a = math.pi / 3 * i
        x, y = cx + r * math.cos(a), cy + r * math.sin(a)
        out += '<path d="M%g %g L%.1f %.1f" stroke="%s" stroke-width="%g" stroke-linecap="round"/>' % (cx, cy, x, y, O, 18)
        mx, my = cx + r * 0.62 * math.cos(a), cy + r * 0.62 * math.sin(a)
        for side in (-1, 1):
            bx, by = mx + 0.34 * r * math.cos(a + side * 0.9), my + 0.34 * r * math.sin(a + side * 0.9)
            out += '<path d="M%.1f %.1f L%.1f %.1f" stroke="%s" stroke-width="%g" stroke-linecap="round"/>' % (mx, my, bx, by, O, 16)
    for i in range(6):
        a = math.pi / 3 * i
        x, y = cx + r * math.cos(a), cy + r * math.sin(a)
        out += '<path d="M%g %g L%.1f %.1f" stroke="%s" stroke-width="%g" stroke-linecap="round"/>' % (cx, cy, x, y, col, 8)
        mx, my = cx + r * 0.62 * math.cos(a), cy + r * 0.62 * math.sin(a)
        for side in (-1, 1):
            bx, by = mx + 0.34 * r * math.cos(a + side * 0.9), my + 0.34 * r * math.sin(a + side * 0.9)
            out += '<path d="M%.1f %.1f L%.1f %.1f" stroke="%s" stroke-width="%g" stroke-linecap="round"/>' % (mx, my, bx, by, col, 6)
    return out

weather = {
    'Clear': badge('#9fd8f5', '#5fa8dc', cloud(128, 140, 1.0) + '<circle cx="172" cy="84" r="24" fill="#ffd45a" stroke="%s" stroke-width="7"/>' % O),
    'Rain': badge('#8aa6c4', '#4a6486', cloud(128, 104, 1.1, '#dfe7f2') + drop(88, 188, 0.9) + drop(128, 206, 0.9) + drop(168, 188, 0.9)),
    'Sunshine': badge('#ffe08a', '#f2a93a', sun(128, 128, 46)),
    'Wind': badge('#b7e3d0', '#5fae94',
                  '<g fill="none" stroke-linecap="round"><path d="M44 100 C100 100 150 84 170 66 C190 48 214 70 194 90" stroke="%s" stroke-width="24"/><path d="M44 100 C100 100 150 84 170 66 C190 48 214 70 194 90" stroke="#ffffff" stroke-width="10"/>'
                  '<path d="M44 150 C110 150 170 136 190 156 C206 172 188 196 168 184" stroke="%s" stroke-width="24"/><path d="M44 150 C110 150 170 136 190 156 C206 172 188 196 168 184" stroke="#ffffff" stroke-width="10"/></g>'
                  '<path d="M62 196 C84 168 116 176 118 204 C92 214 70 210 62 196 Z" fill="#6fb04f" stroke="%s" stroke-width="7" stroke-linejoin="round"/>' % (O, O, O)),
    'Frost': badge('#cfeaff', '#7fb4e8', snowflake(128, 128, 78)),
    # Morning Dew: a leaf holding one big dewdrop and two small ones.
    'Dew': badge('#d6f1df', '#6fbf8f',
                 '<path d="M40 190 C60 90 150 56 216 70 C214 150 150 206 40 190 Z" fill="#6fb04f" stroke="%s" stroke-width="8" stroke-linejoin="round"/>'
                 '<path d="M52 184 C100 140 150 108 200 82" stroke="%s" stroke-width="6" stroke-linecap="round" fill="none"/>' % (O, O)
                 + drop(122, 142, 1.5) + drop(184, 118, 0.8) + drop(84, 176, 0.7)),
    # Golden Hour: a low sun on the horizon with long golden rays.
    'GoldenHour': badge('#ffd98a', '#e8892e',
                        ''.join('<path d="M%.1f %.1f L%.1f %.1f" stroke="%s" stroke-width="%d" stroke-linecap="round"/>' % (128 + r1 * math.cos(a), 160 - r1 * math.sin(a), 128 + r2 * math.cos(a), 160 - r2 * math.sin(a), c, w) for a in [math.pi * k / 6 for k in range(0, 7)] for r1, r2, c, w in ((78, 112, O, 22), (78, 112, '#fff0b0', 12)))
                        + '<path d="M58 160 A70 70 0 0 1 198 160 Z" fill="#ffc83a" stroke="%s" stroke-width="8" stroke-linejoin="round"/>' % O
                        + '<rect x="24" y="160" width="208" height="56" rx="10" fill="#7a4a24" stroke="%s" stroke-width="8"/>' % O),
    # Pollen Shower: a flower scattering golden pollen grains.
    'PollenShower': badge('#fff3b8', '#f2c14a',
                          ''.join('<circle cx="%d" cy="%d" r="%d" fill="#ffd23a" stroke="%s" stroke-width="5"/>' % (x, y, r, O)
                                  for x, y, r in ((70, 74, 12), (128, 56, 15), (186, 80, 12), (96, 118, 10), (160, 124, 11), (52, 134, 9), (206, 138, 9)))
                          + '<g stroke="%s" stroke-width="7" stroke-linejoin="round">' % O
                          + ''.join('<circle cx="%d" cy="%d" r="24" fill="#e8584f"/>' % (128 + dx, 186 + dy) for dx, dy in ((-22, 0), (22, 0), (0, -20), (0, 20)))
                          + '<circle cx="128" cy="186" r="14" fill="#5a2a1c"/></g>'),
    # Heatwave: a blazing sun over a shimmering heat haze.
    'Heatwave': badge('#ffb27a', '#d9482b',
                      sun(128, 106, 40)
                      + ''.join('<path d="M40 %d C70 %d 100 %d 128 %d S186 %d 216 %d" stroke="%s" stroke-width="16" stroke-linecap="round" fill="none"/>' % (y, y - 14, y + 14, y, y - 14, y, O)
                                for y in (190, 220))
                      + ''.join('<path d="M40 %d C70 %d 100 %d 128 %d S186 %d 216 %d" stroke="#fff3d0" stroke-width="7" stroke-linecap="round" fill="none"/>' % (y, y - 14, y + 14, y, y - 14, y)
                                for y in (190, 220))),
    # Hurricane: a dark swirling storm spiral with lightning.
    'Hurricane': badge('#8a96b4', '#3b4568',
                       '<path d="M128 128 m0 0 C128 100 160 100 160 128 C160 168 104 172 96 128 C88 84 150 64 180 100 C212 140 176 204 124 200 C60 196 40 124 70 84" '
                       'stroke="%s" stroke-width="26" stroke-linecap="round" fill="none"/>' % O
                       + '<path d="M128 128 C128 100 160 100 160 128 C160 168 104 172 96 128 C88 84 150 64 180 100 C212 140 176 204 124 200 C60 196 40 124 70 84" '
                       'stroke="#e8eefc" stroke-width="12" stroke-linecap="round" fill="none"/>'
                       '<path d="M196 150 L176 188 L194 188 L178 226 L222 176 L202 176 L216 150 Z" fill="#ffd23a" stroke="%s" stroke-width="6" stroke-linejoin="round"/>' % O),
}

def butterfly():
    return svg('<g stroke="%s" stroke-width="7" stroke-linejoin="round">'
               '<path d="M128 130 C70 40 20 70 38 126 C48 156 98 160 128 138 Z" fill="#f08a3c"/>'
               '<path d="M128 130 C186 40 236 70 218 126 C208 156 158 160 128 138 Z" fill="#f08a3c"/>'
               '<path d="M128 138 C84 150 56 190 76 214 C98 232 122 196 128 160 Z" fill="#ffc86a"/>'
               '<path d="M128 138 C172 150 200 190 180 214 C158 232 134 196 128 160 Z" fill="#ffc86a"/>'
               '<ellipse cx="128" cy="144" rx="9" ry="50" fill="#3a2a22"/></g>'
               '<path d="M122 96 C110 70 100 62 92 60 M134 96 C146 70 156 62 164 60" stroke="%s" stroke-width="6" fill="none" stroke-linecap="round"/>'
               '<circle cx="70" cy="104" r="12" fill="#fff3d0"/><circle cx="186" cy="104" r="12" fill="#fff3d0"/>' % (O, O), 96)

def bee():
    return svg('<ellipse cx="108" cy="80" rx="42" ry="30" fill="#e8f4ff" stroke="%s" stroke-width="6" opacity="0.9"/>'
               '<ellipse cx="160" cy="76" rx="40" ry="28" fill="#e8f4ff" stroke="%s" stroke-width="6" opacity="0.9"/>'
               '<g stroke="%s" stroke-width="7"><ellipse cx="128" cy="140" rx="64" ry="46" fill="#ffc83a"/>'
               '<path d="M104 98 C96 124 96 156 106 182 M142 96 C150 124 150 156 140 184" fill="none" stroke-width="14"/></g>'
               '<circle cx="186" cy="132" r="22" fill="#ffc83a" stroke="%s" stroke-width="7"/><circle cx="194" cy="126" r="5" fill="%s"/>'
               '<path d="M64 148 L38 156" stroke="%s" stroke-width="8" stroke-linecap="round"/>' % (O, O, O, O, O, O), 96)

def leaf():
    return svg('<path d="M40 216 C28 120 110 44 220 40 C216 150 160 224 40 216 Z" fill="#6fb04f" stroke="%s" stroke-width="9" stroke-linejoin="round"/>'
               '<path d="M40 216 C96 156 150 106 206 58" fill="none" stroke="#3f7a35" stroke-width="9" stroke-linecap="round"/>' % O, 96)

def raindrop():
    return svg('<path d="M128 24 C170 92 190 124 190 156 C190 198 162 226 128 226 C94 226 66 198 66 156 C66 124 86 92 128 24 Z" fill="#6cc7f0" stroke="%s" stroke-width="10"/>'
               '<ellipse cx="108" cy="156" rx="12" ry="26" fill="#ffffff" opacity="0.7" transform="rotate(14 108 156)"/>' % O, 96)

def sparkle():
    return svg('<path d="M128 14 L152 104 L242 128 L152 152 L128 242 L104 152 L14 128 L104 104 Z" fill="#ffffff" stroke="%s" stroke-width="9" stroke-linejoin="round"/>' % O, 96)

def sunmote():
    return svg('<circle cx="128" cy="128" r="70" fill="#ffe58a" stroke="%s" stroke-width="10"/><circle cx="106" cy="106" r="22" fill="#fffbe0"/>' % O, 96)

def flame():
    return svg('<path d="M128 14 C150 62 214 96 206 166 C200 220 160 246 128 246 C90 246 52 222 50 168 C48 126 76 112 90 78 C104 116 120 96 128 14 Z" fill="#ff7a1a" stroke="%s" stroke-width="12" stroke-linejoin="round"/>'
               '<path d="M128 96 C142 130 176 150 170 188 C166 218 148 232 128 232 C106 232 88 218 88 190 C88 160 116 150 128 96 Z" fill="#ffd23a"/>' % O, 96)


def cat_sprite():
    # A ginger cat mid-stride, facing right.
    return svg('<g stroke="%s" stroke-width="9" stroke-linejoin="round" stroke-linecap="round">'
               '<path d="M54 120 C22 110 20 60 44 52" fill="none" stroke-width="22"/><path d="M54 120 C22 110 20 60 44 52" fill="none" stroke="#f0a64a" stroke-width="10"/>'
               '<path d="M76 176 L52 214 M112 180 L102 218" fill="none" stroke-width="22"/><path d="M76 176 L52 214 M112 180 L102 218" fill="none" stroke="#e08f35" stroke-width="10"/>'
               '<ellipse cx="116" cy="140" rx="68" ry="40" fill="#f0a64a"/>'
               '<path d="M150 178 L176 214 M184 160 L222 192" fill="none" stroke-width="22"/><path d="M150 178 L176 214 M184 160 L222 192" fill="none" stroke="#f0a64a" stroke-width="10"/>'
               '<path d="M96 112 L116 118 M120 108 L140 116" stroke="#c9792a" stroke-width="7" fill="none"/>'
               '<path d="M162 92 L168 54 L188 76 L214 76 L226 54 L232 92 C240 130 158 134 162 92 Z" fill="#f0a64a"/>'
               '<circle cx="186" cy="98" r="6" fill="#13212e" stroke="none"/><circle cx="214" cy="98" r="6" fill="#13212e" stroke="none"/>'
               '<path d="M196 108 L204 108 L200 114 Z" fill="#e8584f" stroke-width="4"/></g>' % O, 96)


def snowflake_sprite():
    out = ''
    for i in range(6):
        a = math.pi / 3 * i
        x, y = 128 + 96 * math.cos(a), 128 + 96 * math.sin(a)
        out += '<path d="M128 128 L%.1f %.1f" stroke="%s" stroke-width="22" stroke-linecap="round"/>' % (x, y, O)
    for i in range(6):
        a = math.pi / 3 * i
        x, y = 128 + 96 * math.cos(a), 128 + 96 * math.sin(a)
        out += '<path d="M128 128 L%.1f %.1f" stroke="#f2f9ff" stroke-width="10" stroke-linecap="round"/>' % (x, y)
        mx, my = 128 + 58 * math.cos(a), 128 + 58 * math.sin(a)
        for side in (-1, 1):
            bx, by = mx + 30 * math.cos(a + side * 0.9), my + 30 * math.sin(a + side * 0.9)
            out += '<path d="M%.1f %.1f L%.1f %.1f" stroke="#f2f9ff" stroke-width="8" stroke-linecap="round"/>' % (mx, my, bx, by)
    return svg(out + '<circle cx="128" cy="128" r="14" fill="#f2f9ff"/>', 96)

def petal_sprite():
    return svg('<path d="M128 22 C196 70 206 150 128 232 C50 150 60 70 128 22 Z" fill="#f6b8cc" stroke="%s" stroke-width="10" stroke-linejoin="round"/>'
               '<path d="M128 40 C128 100 128 160 128 214" stroke="#e58aa8" stroke-width="6" fill="none"/>' % O, 96)

def maple_sprite():
    pts = [(128, 20), (150, 80), (206, 56), (186, 118), (236, 134), (176, 168), (190, 214), (136, 190), (128, 244), (120, 190), (66, 214), (80, 168), (20, 134), (70, 118), (50, 56), (106, 80)]
    return svg('<path d="M%s Z" fill="#e0662a" stroke="%s" stroke-width="10" stroke-linejoin="round"/>' % (' L'.join('%g %g' % p for p in pts), O) +
               '<path d="M128 70 V230 M128 130 L84 100 M128 130 L172 100" stroke="#a8401a" stroke-width="6" fill="none" stroke-linecap="round"/>', 96)


def tx(x, y, size, txt, fill='#ffffff'):
    return '<text x="%g" y="%g" font-size="%g" font-family="DejaVu Sans,Arial" font-weight="bold" text-anchor="middle" fill="%s" stroke="%s" stroke-width="5" paint-order="stroke">%s</text>' % (x, y, size, fill, O, txt)

def flake_dots(pts):
    return ''.join('<circle cx="%d" cy="%d" r="%d" fill="#ffffff" stroke="%s" stroke-width="5"/>' % (x, y, r, O) for x, y, r in pts)

def bolt(x, y, k=1.0):
    return '<path transform="translate(%g %g) scale(%g)" d="M0 0 L-20 38 L-2 38 L-18 76 L26 26 L6 26 L20 0 Z" fill="#ffd23a" stroke="%s" stroke-width="%g" stroke-linejoin="round"/>' % (x, y, k, O, 6 / k)

def glasshouse(col='#e8f7ff'):
    return ('<path d="M34 110 L128 42 L222 110 L222 206 L34 206 Z" fill="%s" fill-opacity="0.35" stroke="%s" stroke-width="9" stroke-linejoin="round"/>'
            '<path d="M128 42 L128 206 M34 110 L222 110 M81 76 L81 206 M175 76 L175 206" stroke="%s" stroke-width="5" fill="none"/>') % (col, O, O)

def cat(x, y, k=1.0):
    return ('<g transform="translate(%g %g) scale(%g)" stroke="%s" stroke-width="%g" stroke-linejoin="round">'
            '<path d="M-34 -6 L-34 -40 L-18 -26 L18 -26 L34 -40 L34 -6 C34 30 -34 30 -34 -6 Z" fill="#f0a64a"/>'
            '<circle cx="-14" cy="-6" r="5" fill="#13212e"/><circle cx="14" cy="-6" r="5" fill="#13212e"/>'
            '<path d="M-5 6 L5 6 L0 12 Z" fill="#e8584f" stroke-width="3"/></g>') % (x, y, k, O, 6 / k)

weather.update({
    'Snowfall': badge('#b8c8de', '#6a86ae', cloud(128, 92, 1.1, '#e6edf7') + flake_dots([(80, 168, 9), (128, 190, 11), (176, 166, 9), (104, 218, 8), (156, 222, 8), (60, 206, 7), (200, 206, 7)])),
    'Blizzard': badge('#dbe6f4', '#6b7da3',
                      ''.join('<path d="M%d %d C%d %d %d %d %d %d" stroke="%s" stroke-width="22" stroke-linecap="round" fill="none"/>' % (x0, y, x0 + 50, y - 22, x0 + 110, y + 22, x0 + 160, y, O) for x0, y in ((36, 90), (50, 146), (28, 198)))
                      + ''.join('<path d="M%d %d C%d %d %d %d %d %d" stroke="#ffffff" stroke-width="9" stroke-linecap="round" fill="none"/>' % (x0, y, x0 + 50, y - 22, x0 + 110, y + 22, x0 + 160, y) for x0, y in ((36, 90), (50, 146), (28, 198)))
                      + flake_dots([(176, 60, 8), (210, 120, 7), (200, 186, 8), (70, 52, 7)])),
    'Aurora': badge('#1c2b52', '#0c1530',
                    '<path d="M20 190 C60 70 110 130 128 60 C150 120 190 70 236 180 L236 236 L20 236 Z" fill="#46e0a0" fill-opacity="0.55"/>'
                    '<path d="M60 200 C80 110 120 150 140 90 C160 140 190 110 210 190 Z" fill="#8f7bff" fill-opacity="0.6"/>'
                    '<path d="M20 214 L128 160 L236 214 Z" fill="#f4fbff" stroke="%s" stroke-width="7" stroke-linejoin="round"/>' % O
                    + flake_dots([(54, 50, 4), (190, 40, 5), (222, 90, 4), (96, 36, 4)])),
    'IndianSummer': badge('#ffc47a', '#c95b24',
                          sun(128, 112, 40)
                          + '<path transform="translate(128 206) scale(0.8)" d="M0 -40 L12 -22 L34 -26 L24 -8 L44 4 L22 10 L28 32 L8 22 L0 40 L-8 22 L-28 32 L-22 10 L-44 4 L-24 -8 L-34 -26 L-12 -22 Z" fill="#d9482b" stroke="%s" stroke-width="8" stroke-linejoin="round"/>' % O),
    'Sprinkler': badge('#a8e0ee', '#3f8fb0',
                       '<rect x="112" y="170" width="32" height="56" rx="8" fill="#8896a6" stroke="%s" stroke-width="8"/>' % O
                       + '<path d="M128 170 C100 130 60 110 40 130 M128 170 C128 120 128 90 128 58 M128 170 C156 130 196 110 216 130" stroke="%s" stroke-width="20" stroke-linecap="round" fill="none"/>' % O
                       + '<path d="M128 170 C100 130 60 110 40 130 M128 170 C128 120 128 90 128 58 M128 170 C156 130 196 110 216 130" stroke="#bfe9ff" stroke-width="8" stroke-linecap="round" fill="none"/>'
                       + drop(40, 150, 0.8) + drop(128, 40, 0.8) + drop(216, 150, 0.8) + tx(196, 224, 40, '!', '#ffd23a')),
    'GrowLamp': badge('#e9a8ff', '#7a3fb8',
                      '<path d="M84 60 L172 60 L196 118 L60 118 Z" fill="#8896a6" stroke="%s" stroke-width="8" stroke-linejoin="round"/>' % O
                      + '<path d="M70 130 L186 130 L226 226 L30 226 Z" fill="#ff6bd6" fill-opacity="0.55"/>'
                      + '<ellipse cx="128" cy="126" rx="48" ry="12" fill="#fff3a8" stroke="%s" stroke-width="6"/>' % O
                      + '<path d="M112 226 C112 190 128 176 128 160 C128 176 144 190 144 226 Z" fill="#6fb04f" stroke="%s" stroke-width="7" stroke-linejoin="round"/>' % O),
    'VentFan': badge('#c4e8dc', '#5aa690',
                     '<circle cx="128" cy="128" r="92" fill="#e8f1ee" stroke="%s" stroke-width="9"/>' % O
                     + ''.join('<path transform="rotate(%d 128 128)" d="M128 128 C100 84 112 44 148 52 C158 80 148 108 128 128 Z" fill="#7bc4e8" stroke="%s" stroke-width="7" stroke-linejoin="round"/>' % (a, O) for a in (0, 120, 240))
                     + '<circle cx="128" cy="128" r="14" fill="#8896a6" stroke="%s" stroke-width="7"/>' % O),
    'Condensation': badge('#cdeaf0', '#5f9fae',
                          glasshouse('#ffffff') + drop(70, 150, 1.0) + drop(128, 178, 1.2) + drop(180, 146, 1.0) + drop(100, 100, 0.7) + drop(160, 108, 0.7)),
    'Thermostat': badge('#ffc2a0', '#d6452b',
                        '<rect x="104" y="30" width="48" height="140" rx="24" fill="#ffffff" stroke="%s" stroke-width="9"/>' % O
                        + '<circle cx="128" cy="188" r="38" fill="#e8302b" stroke="%s" stroke-width="9"/>' % O
                        + '<rect x="120" y="66" width="16" height="122" rx="8" fill="#e8302b"/>'
                        + ''.join('<path d="M%d %d L%d %d" stroke="%s" stroke-width="8" stroke-linecap="round"/>' % (x1, y, x2, y, O) for x1, x2, y in ((168, 190, 56), (168, 184, 84), (168, 190, 112)))
                        + tx(206, 232, 44, '!!', '#ffd23a')),
    'RogueCat': badge('#ffd9a0', '#d98a3c', cat(128, 130, 1.5)
                      + '<path d="M30 220 C60 200 80 226 110 208" stroke="%s" stroke-width="10" stroke-linecap="round" fill="none"/>' % O
                      + '<g fill="#6fb04f" stroke="%s" stroke-width="5"><path d="M196 56 C214 40 232 56 220 72 C206 76 198 68 196 56 Z"/><path d="M28 70 C44 52 62 66 52 82 C38 88 30 80 28 70 Z"/></g>'  % O),
    'FreezerLeak': badge('#d8f2ff', '#5f9fd8',
                         '<rect x="54" y="36" width="148" height="190" rx="14" fill="#f2f6fa" stroke="%s" stroke-width="9"/>' % O
                         + '<path d="M54 36 L120 56 L120 206 L54 226 Z" fill="#9fc8e8" stroke="%s" stroke-width="8" stroke-linejoin="round"/>' % O
                         + '<rect x="130" y="110" width="10" height="40" rx="5" fill="#8896a6" stroke="%s" stroke-width="4"/>' % O
                         + snowflake(168, 160, 22) + flake_dots([(160, 70, 7), (186, 100, 6)])),
    'Compost': badge('#d5b08a', '#7a5230',
                     '<path d="M52 120 L204 120 L190 214 L66 214 Z" fill="#6b4226" stroke="%s" stroke-width="9" stroke-linejoin="round"/>' % O
                     + '<rect x="46" y="106" width="164" height="24" rx="10" fill="#8a5a32" stroke="%s" stroke-width="8"/>' % O
                     + '<path d="M100 120 C92 78 110 54 128 40 C146 54 164 78 156 120 Z" fill="#8fd05a" stroke="%s" stroke-width="7" stroke-linejoin="round"/>' % O
                     + ''.join('<circle cx="%d" cy="%d" r="%d" fill="#ffd23a" stroke="%s" stroke-width="5"/>' % (x, y, r, O) for x, y, r in ((80, 160, 8), (128, 176, 10), (172, 156, 8), (104, 196, 7), (152, 198, 7)))),
    'DiscoLights': badge('#5b3a8c', '#241447',
                         '<circle cx="128" cy="112" r="56" fill="#ffd45a" stroke="%s" stroke-width="9"/>' % O
                         + ''.join('<path d="M%d %d L%d %d" stroke="#8a6a00" stroke-width="4"/>' % (x1, y1, x2, y2) for x1, y1, x2, y2 in ((74, 96, 182, 96), (80, 130, 176, 130), (112, 58, 112, 168), (144, 58, 144, 168)))
                         + '<path d="M128 40 L128 20" stroke="%s" stroke-width="8" stroke-linecap="round"/>' % O
                         + ''.join('<path d="M%d %d L%d %d L%d %d Z" fill="%s" fill-opacity="0.55"/>' % (128, 112, x1, 236, x2, 236, c) for x1, x2, c in ((20, 70, '#ff6bd6'), (96, 150, '#6bf0ff'), (190, 236, '#fff36b')))),
})

ambient = {'Snowflake': snowflake_sprite(), 'Petal': petal_sprite(), 'MapleLeaf': maple_sprite(), 'Flame': flame(), 'Butterfly': butterfly(), 'Bee': bee(), 'Leaf': leaf(), 'Raindrop': raindrop(), 'Sparkle': sparkle(), 'SunMote': sunmote(), 'Cat': cat_sprite()}

def render(name, content, out_path, px):
    html = '/tmp/_amb_%s.html' % name
    open(html, 'w').write('<html><body style="margin:0;background:transparent">%s</body></html>' % content.replace('width="256" height="256"', 'width="%d" height="%d"' % (px, px), 1).replace('width="96" height="96"', 'width="%d" height="%d"' % (px, px), 1))
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    subprocess.run([CHROME, '--headless', '--no-sandbox', '--disable-gpu', '--hide-scrollbars', '--default-background-color=00000000', '--screenshot=' + out_path,
                    '--window-size=%d,%d' % (px, px + 300), 'file://' + html], check=True, capture_output=True)
    subprocess.run(['convert', out_path, '-crop', '%dx%d+0+0' % (px, px), '+repage', out_path], check=True)

for name, content in weather.items():
    render('w_' + name, content, '%s/Icons/Weather/%s.png' % (RES, name), 128)
for name, content in ambient.items():
    render('a_' + name, content, '%s/Ambient/%s.png' % (RES, name), 96)
print('weather', len(weather), 'ambient', len(ambient))
