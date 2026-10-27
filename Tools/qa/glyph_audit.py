#!/usr/bin/env python3
"""Finds characters in the game's visible strings that the main UI font (Digitalt) cannot draw: they vanish on screen.
Usage: python3 Tools/qa/glyph_audit.py   (exit code 1 when any is found). Needs fontTools (pip install fonttools)."""
import glob, os, re, sys
from fontTools.ttLib import TTFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
cmap = set(TTFont(os.path.join(ROOT, 'Assets/Resources/Colorful_UI/colorful/font/Digitalt.ttf')).getBestCmap().keys())
ALLOWED = {'◆'}  # only a constant in DescriptionTextFormatter; no visible text contains it
found = {}
for path in glob.glob(os.path.join(ROOT, 'Assets/Scripts/**/*.cs'), recursive=True):
    if '/Tests/' in path:
        continue
    for n, line in enumerate(open(path, encoding='utf-8'), 1):
        if line.strip().startswith('//'):
            continue
        for lit in re.findall(r'"((?:[^"\\]|\\.)*)"', line):
            for ch in set(lit):
                if ord(ch) > 127 and ord(ch) not in cmap and ch not in ALLOWED:
                    found.setdefault(ch, []).append('%s:%d' % (os.path.relpath(path, ROOT), n))
for ch, locs in found.items():
    print('%r U+%04X not in Digitalt (%d): %s' % (ch, ord(ch), len(locs), ', '.join(locs[:8])))
print('No missing glyphs.' if not found else '')
sys.exit(1 if found else 0)
