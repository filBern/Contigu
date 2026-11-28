#!/usr/bin/env python3
"""Packs the game's real icons into 3 atlases (out/img/*.webp) and adds their index to out/data.json. Run after build_data.py."""
import json, os, math
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, 'out'); IMG = os.path.join(OUT, 'img')
R = os.path.join(HERE, '../../Assets/Resources/') 
os.makedirs(IMG, exist_ok=True)
d = json.load(open(os.path.join(OUT, 'dump.json')))
CELL = 128
def atlas(name, items):
    n = len(items); cols = math.ceil(math.sqrt(n)); rows = math.ceil(n / cols)
    im = Image.new('RGBA', (cols * CELL, rows * CELL), (0, 0, 0, 0)); idx = {}
    for i, (key, path) in enumerate(items):
        t = Image.open(path).convert('RGBA'); bb = t.getbbox()
        if bb: t = t.crop(bb)
        t.thumbnail((CELL - 6, CELL - 6), Image.LANCZOS)
        c, r = i % cols, i // cols
        im.alpha_composite(t, (c * CELL + (CELL - t.width) // 2, r * CELL + (CELL - t.height) // 2)); idx[key] = [c, r]
    im.save(os.path.join(IMG, name + '.webp'), 'WEBP', quality=86, method=6)
    return dict(cols=cols, rows=rows, idx=idx)
A = {}
A['fert'] = atlas('fert', [(f['id'], R + 'Icons/Fertilizers/%s.png' % f['id']) for f in d['fertilizers']])
A['spr'] = atlas('spr', [(u['id'], R + 'Icons/Upgrades/%s.png' % u['id']) for u in d['upgrades']])
misc = [('e:' + e['id'], R + 'Enemies/%s.png' % e['id']) for e in d['enemies']] + [('w:' + w['id'], R + 'Icons/Weather/%s.png' % w['id']) for w in d['weather']] + [('c:' + c['Name'], R + 'Icons/Challenges/%s.png' % c['Name']) for c in d['challenges']] + [('x:' + n, R + 'Icons/Workshop/%s.png' % n) for n in ['Anvil', 'Compost', 'FertilizerPack', 'MasteryPack', 'SeedPack', 'SproutPack']] + [('g:Pollen', R + 'Icons/Pollen.png'), ('g:Shield', R + 'Icons/Shield.png')]
A['misc'] = atlas('misc', misc)
for name, src, w in [('logo', 'Title/LastBloom.png', 720), ('sprig', 'Characters/Sprig_Happy.png', 320), ('sprig_n', 'Characters/Sprig_Neutral.png', 160)]:
    t = Image.open(R + src).convert('RGBA'); t.thumbnail((w, w), Image.LANCZOS); t.save(os.path.join(IMG, name + '.webp'), 'WEBP', quality=88, method=6)
for n in ['Spring', 'Winter', 'Summer', 'Autumn', 'Orchard', 'Greenhouse']:
    t = Image.open(R + 'Backgrounds/%s.png' % n).convert('RGB'); t.thumbnail((640, 360), Image.LANCZOS); t.save(os.path.join(IMG, 'bg_%s.webp' % n), 'WEBP', quality=70, method=6)
p = os.path.join(OUT, 'data.json'); w = json.load(open(p)); w['atlas'] = A
json.dump(w, open(p, 'w'), ensure_ascii=False, separators=(',', ':'))
print('atlases:', {k: len(v['idx']) for k, v in A.items()})
