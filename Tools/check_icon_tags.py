#!/usr/bin/env python3
"""Fails when a fertilizer icon's corner tag (Design/Icons/generate_icons.py) is not backed by that fertilizer's description
(Assets/Scripts/Core/Modifiers/ModifierDefinition.cs). Catches icons going stale when a balance change edits a value in one place only.
Usage: python3 Tools/check_icon_tags.py   (exit code 1 on any mismatch)"""
import re, sys, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
gen = open(os.path.join(ROOT, 'Design/Icons/generate_icons.py'), encoding='utf-8').read()
defs = open(os.path.join(ROOT, 'Assets/Scripts/Core/Modifiers/ModifierDefinition.cs'), encoding='utf-8').read()

tags = {m.group(1): m.group(2) for m in re.finditer(r"'(\w+)': \('\w+', (?:'[a-z]'|None), '([^']*)', '\w+'\)", gen)}
descriptions = {m.group(1): m.group(2) for m in re.finditer(r'ModifierId\.(\w+), ModifierCategory\.\w+, "[^"]+",\s*"([^"]*)"', defs)}


def normalize(text):
    return text.replace('×', 'x').replace('−', '-').lower()


problems = []
for name, tag in sorted(tags.items()):
    if name not in descriptions:
        problems.append('%s: icon tag %r but no ModifierDefinition found' % (name, tag))
        continue
    desc = normalize(descriptions[name])
    t = normalize(tag)
    # "xn" / "+n" are placeholders for a value that is a formula in the text.
    if t in ('xn', '+n'):
        continue
    if t not in desc:
        problems.append('%s: icon says %r but the description reads: %s' % (name, tag, descriptions[name]))

if problems:
    print('Icon tag / description mismatches:')
    for p in problems:
        print('  - ' + p)
    sys.exit(1)
print('All %d fertilizer icon tags match their descriptions.' % len(tags))
