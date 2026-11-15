#!/usr/bin/env python3
"""Bare weather icons: the pictogram without its rounded colored tile and without its dark outline. The PNGs of the old tiles (kept in this folder, the only source: there
is no SVG) are processed: the tile's background is removed by growing a region from its border (it stops at the pictogram's dark outlines), the dark outline that faces
the outside is peeled off, and the drawing is fitted to its square with a soft shadow. Run from this folder; writes Assets/Resources/Icons/Weather/<Name>.png (128 px)."""
import glob, os
from collections import deque
from PIL import Image, ImageFilter

OUT = '../../Assets/Resources/Icons/Weather'
SIDE, MARGIN = 128, 7
MIN_ISLAND = 8   # pixels: a smaller island is a speck, not a part of the drawing
EDGE = 6   # nothing of the drawing is this close to the canvas edge; what is there is the tile's rim
DARK_LUMA = 95
RING_DEPTH = 7   # the tile's outline is at most this many pixels thick
RING_LUMA = 70   # the tile's outline is #13212e (luma about 30)
TOL = 26   # a pixel joins the background when it is this close (per channel) to the neighbor it grows from

def luma(p):
    return 0.299 * p[0] + 0.587 * p[1] + 0.114 * p[2]

def process(src, dst):
    im = Image.open(src).convert('RGBA')
    w, h = im.size
    px = im.load()
    bg = [[False] * h for _ in range(w)]
    # 1. the tile's own dark outline ring: grown from the transparent corners through dark pixels
    q = deque()
    depth = {}
    for x in range(w):
        for y in range(h):
            if px[x, y][3] < 10:
                bg[x][y] = True
                depth[(x, y)] = 0
                q.append((x, y))
    while q:
        x, y = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < w and 0 <= ny < h and not bg[nx][ny] and depth[(x, y)] < RING_DEPTH:
                n = px[nx, ny]
                if n[3] < 255 or luma(n) < RING_LUMA:
                    bg[nx][ny] = True
                    depth[(nx, ny)] = depth[(x, y)] + 1
                    q.append((nx, ny))
    # 2. the tile's background: grown from what touches the ring, through pixels close in color to their neighbor; it stops at the pictogram's dark outlines
    q = deque()
    seeds = set()
    for x in range(w):
        for y in range(h):
            if not bg[x][y]:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and not bg[nx][ny]:
                    seeds.add((nx, ny))
    for x, y in seeds:   # (marked after the scan: marking while scanning floods the whole image)
        bg[x][y] = True
        q.append((x, y))
    # a night scene (Aurora, DiscoLights) has a dark tile: the background then grows through dark pixels too
    samples = []
    for t in range(RING_DEPTH + 3, RING_DEPTH + 9):
        for u in range(int(w * 0.2), int(w * 0.8)):
            samples += [luma(px[u, t]), luma(px[u, h - 1 - t]), luma(px[t, u]), luma(px[w - 1 - t, u])]
    samples.sort()
    dark_bg = samples[len(samples) // 2] <= DARK_LUMA + 15
    while q:
        x, y = q.popleft()
        p = px[x, y]
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < w and 0 <= ny < h and not bg[nx][ny]:
                n = px[nx, ny]
                if n[3] > 200 and (dark_bg or luma(n) > DARK_LUMA) and max(abs(n[0] - p[0]), abs(n[1] - p[1]), abs(n[2] - p[2])) <= TOL:
                    bg[nx][ny] = True
                    q.append((nx, ny))
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    po = out.load()
    for x in range(w):
        for y in range(h):
            if not bg[x][y] and px[x, y][3] > 0:
                po[x, y] = px[x, y]
    # the tile's own outer outline (dark, connected to the canvas edge) and the pictogram's outer outline: peel dark pixels that touch transparency
    for _ in range(5):
        kill = []
        for x in range(w):
            for y in range(h):
                p = po[x, y]
                if p[3] == 0:
                    continue
                if luma(p) < DARK_LUMA or p[3] < 255:
                    if any((not (0 <= x + dx < w and 0 <= y + dy < h)) or po[x + dx, y + dy][3] == 0 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                        kill.append((x, y))
        for x, y in kill:
            po[x, y] = (0, 0, 0, 0)
    # specks: tiny islands of leftover anti-aliased background
    seen = [[False] * h for _ in range(w)]
    for x0 in range(w):
        for y0 in range(h):
            if seen[x0][y0] or po[x0, y0][3] == 0:
                continue
            comp, qq = [], deque([(x0, y0)])
            seen[x0][y0] = True
            while qq:
                x, y = qq.popleft()
                comp.append((x, y))
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1), (1, -1), (-1, 1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < w and 0 <= ny < h and not seen[nx][ny] and po[nx, ny][3] > 0:
                        seen[nx][ny] = True
                        qq.append((nx, ny))
            if len(comp) < MIN_ISLAND:
                for x, y in comp:
                    po[x, y] = (0, 0, 0, 0)
    for x in range(w):
        for y in range(h):
            if min(x, y, w - 1 - x, h - 1 - y) < EDGE:
                po[x, y] = (0, 0, 0, 0)
    box = out.getchannel('A').getbbox()
    if not box:
        print('  (nothing left of', src, ')')
        return False
    crop = out.crop(box)
    cw, ch = crop.size
    scale = min((SIDE - 2 * MARGIN) / cw, (SIDE - 2 * MARGIN) / ch)
    crop = crop.resize((max(1, round(cw * scale)), max(1, round(ch * scale))), Image.LANCZOS)
    canvas = Image.new('RGBA', (SIDE, SIDE), (0, 0, 0, 0))
    ox, oy = (SIDE - crop.width) // 2, (SIDE - crop.height) // 2
    shadow = Image.new('RGBA', (SIDE, SIDE), (0, 0, 0, 0))
    shadow.paste((26, 15, 8, 255), (ox, oy + 2), crop.getchannel('A'))
    shadow = shadow.filter(ImageFilter.GaussianBlur(2.2))
    sa = shadow.getchannel('A').point(lambda v: int(v * 0.55))
    shadow.putalpha(sa)
    canvas.alpha_composite(shadow)
    canvas.alpha_composite(crop, (ox, oy))
    canvas.save(dst)
    return True

for path in sorted(glob.glob('*.png')):
    name = os.path.basename(path)
    if os.path.exists('%s/%s' % (OUT, name)):
        process(path, '%s/%s' % (OUT, name))
print('done')
