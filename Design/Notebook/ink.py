"""Notebook ("cahier") style toolkit: wobbly double-stroke ink + misregistered crayon fills + ruled paper. Output SVG strings, rendered by render.js."""
import math, random
INK = '#2a2420'
DEFS = '''<defs>
<filter id="crayon" x="-5%" y="-5%" width="110%" height="110%"><feTurbulence type="fractalNoise" baseFrequency="0.9" numOctaves="2" seed="3" result="n"/><feDisplacementMap in="SourceGraphic" in2="n" scale="5"/><feComponentTransfer><feFuncA type="discrete" tableValues="0 .85 1 1"/></feComponentTransfer></filter>
<filter id="paper"><feTurbulence type="fractalNoise" baseFrequency="0.04" numOctaves="3" seed="9"/><feColorMatrix values="0 0 0 0 .55  0 0 0 0 .45  0 0 0 0 .3  0 0 0 .22 0"/></filter>
</defs>'''

class D:
    def __init__(s, seed, scratch=False): s.r = random.Random(seed); s.o = []; s.scratch = scratch; s._clip = 0
    def j(s, v, a): return v + s.r.uniform(-a, a)
    def path(s, pts, closed, a, step=18):
        P = []; m = len(pts) - (0 if closed else 1)
        for i in range(m):
            x0, y0 = pts[i]; x1, y1 = pts[(i + 1) % len(pts)]
            n = max(1, int(math.hypot(x1 - x0, y1 - y0) / step))
            for k in range(n):
                t = k / n; P.append((s.j(x0 + (x1 - x0) * t, a), s.j(y0 + (y1 - y0) * t, a)))
        if not closed: P.append((s.j(pts[-1][0], a), s.j(pts[-1][1], a)))
        d = "M%.1f %.1f " % P[0]
        for i in range(1, len(P) - 1):
            d += "Q%.1f %.1f %.1f %.1f " % (P[i][0], P[i][1], (P[i][0] + P[i + 1][0]) / 2, (P[i][1] + P[i + 1][1]) / 2)
        d += "L%.1f %.1f" % P[-1]
        return d + ("Z" if closed else "")
    def line(s, pts, w=2.6, closed=False, fill=None, a=1.1, op=1, step=18):
        if s.scratch:
            a = a * 1.6
        if fill:
            off = (s.r.uniform(-2.5, 2.5) * (1.8 if s.scratch else 1), s.r.uniform(-2.5, 2.5) * (1.8 if s.scratch else 1))
            s.o.append('<path d="%s" fill="%s" stroke="none" opacity="0.92" transform="translate(%.1f %.1f)" filter="url(#crayon)"/>' % (s.path(pts, True, a * 1.2, step), fill, *off))
            if s.scratch:
                s._scribble(pts)
        strokes = 3 if s.scratch else (2 if w > 2 else 1)
        for k in range(strokes):
            width = w * (1 if k == 0 else (.6 if not s.scratch else (.55 if k == 1 else .35)))
            s.o.append('<path d="%s" fill="none" stroke="%s" stroke-width="%.1f" stroke-linecap="round" stroke-linejoin="round" opacity="%s"/>' % (s.path(pts, closed, a * (1 + k * .7), step), INK, width, op if k == 0 else op * .85))

    def _scribble(s, pts):
        """Scratchy crayon texture: short diagonal strokes inside the shape (clipped to it), like a kid shading with a pencil."""
        xs = [p[0] for p in pts]; ys = [p[1] for p in pts]
        x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
        if x1 - x0 < 14 or y1 - y0 < 14:
            return
        s._clip += 1
        cid = 'cl%d_%d' % (id(s) % 100000, s._clip)
        s.o.append('<clipPath id="%s"><path d="%s"/></clipPath>' % (cid, s.path(pts, True, 0, 40)))
        n = int((x1 - x0) * (y1 - y0) / 160)
        strokes = []
        for _ in range(min(n, 90)):
            x = s.r.uniform(x0, x1); y = s.r.uniform(y0, y1); L = s.r.uniform(8, 20)
            strokes.append('M%.1f %.1f l%.1f %.1f' % (x, y, L * .7, L * .6 + s.r.uniform(-3, 3)))
        s.o.append('<path d="%s" fill="none" stroke="%s" stroke-width="1.5" stroke-linecap="round" opacity="0.22" clip-path="url(#%s)"/>' % (' '.join(strokes), INK, cid))
    def box(s, x, y, w, h, fill=None, lw=4, a=1.4, r=12, step=18):
        r = min(r, w / 2, h / 2); pts = []
        for (cx, cy, a0) in ((x + w - r, y + r, -90), (x + w - r, y + h - r, 0), (x + r, y + h - r, 90), (x + r, y + r, 180)):
            for k in range(7):
                t = math.radians(a0 + 90 * k / 6); pts.append((cx + r * math.cos(t), cy + r * math.sin(t)))
        s.line(pts, w=lw, closed=True, fill=fill, a=a * 1.5, step=min(step, 22))
    def blob(s, cx, cy, rx, ry, n=18, **k):
        s.line([(cx + rx * math.cos(2 * math.pi * i / n), cy + ry * math.sin(2 * math.pi * i / n)) for i in range(n)], closed=True, **k)
    def dot(s, x, y, r=4): s.o.append('<circle cx="%.1f" cy="%.1f" r="%s" fill="%s"/>' % (s.j(x, .5), s.j(y, .5), r, INK))
    def hatch(s, x0, y0, x1, y1, n=5):
        for i in range(n):
            t = i / max(1, n - 1); s.line([(x0 + (x1 - x0) * t, y0), (x0 + (x1 - x0) * t - 12, y1)], w=1.6, a=.8, op=.7)
    def svg(s, w, h, bg=None):
        b = '<rect width="%d" height="%d" fill="%s"/>' % (w, h, bg) if bg else ''
        return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 %d %d" width="%d" height="%d">%s%s%s</svg>' % (w, h, w, h, DEFS, b, "".join(s.o))
