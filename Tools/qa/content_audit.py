#!/usr/bin/env python3
"""Static content audit for LASTBLOOM: finds content that exists in one place of the game but is missing or inconsistent in another
(an id with no description, no price, no icon, no tooltip text...) plus wording that hurts clarity. Read-only, fast, no Unity needed.
Usage: python3 Tools/qa/content_audit.py [--strict]   (--strict: exit code 1 when anything is found)"""
import os
import re
import sys
from collections import defaultdict

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
S = os.path.join(ROOT, 'Assets/Scripts')
R = os.path.join(ROOT, 'Assets/Resources')


def read(*parts):
    with open(os.path.join(*parts), encoding='utf-8') as f:
        return f.read()


def enum_members(path, name):
    text = re.sub(r'/\*.*?\*/|//[^\n]*', '', read(path), flags=re.S)
    start = re.search(r'enum\s+%s\b[^{]*\{' % name, text).end()
    depth, end = 1, start
    while depth and end < len(text):
        depth += {'{': 1, '}': -1}.get(text[end], 0)
        end += 1
    body = re.sub(r'\[[^\]]*\]', '', text[start:end - 1])
    members = []
    for part in body.split(','):
        m = re.match(r'\s*([A-Za-z_]\w*)', part)
        if m:
            members.append(m.group(1))
    return members


findings = defaultdict(list)


def report(section, message):
    findings[section].append(message)


def exists(*parts):
    return os.path.exists(os.path.join(*parts))


# ------------------------------------------------------------------ fertilizers
modifier_ids = enum_members(S + '/Core/Modifiers/ModifierId.cs', 'ModifierId')
mod_defs = read(S, 'Core/Modifiers/ModifierDefinition.cs')
pricing = read(S, 'Core/Economy/ModifierPricing.cs')
glossary = read(ROOT, 'docs/GLOSSARY.md')
defined = dict((m.group(1), m.group(2)) for m in re.finditer(r'ModifierId\.(\w+), ModifierCategory\.\w+, "([^"]+)",\s*"([^"]*)"', mod_defs) for _ in [0] if True) if False else {}
for m in re.finditer(r'ModifierId\.(\w+), ModifierCategory\.\w+, "([^"]+)",\s*"([^"]*)"', mod_defs):
    defined[m.group(1)] = (m.group(2), m.group(3))
all_array = re.search(r'ModifierDefinition\[\] All\s*=\s*\{(.*?)\};', mod_defs, re.S).group(1)
in_all = set(re.findall(r'\b(\w+)\b', all_array))
# Ids that are state markers rather than content (no card, no price, no icon).
NOT_CONTENT = {'Blighted'}
for mid in modifier_ids:
    if mid in NOT_CONTENT:
        continue
    if mid not in defined:
        report('Fertilizers', '%s: in the ModifierId enum but has no ModifierDefinition' % mid)
        continue
    name, desc = defined[mid]
    if not desc.strip():
        report('Fertilizers', '%s (%s): empty description' % (mid, name))
    if 'ModifierId.%s' % mid not in pricing:
        report('Fertilizers', '%s (%s): no price in ModifierPricing' % (mid, name))
    if not exists(R, 'Icons/Fertilizers/%s.png' % mid):
        report('Fertilizers', '%s (%s): no sack icon (Resources/Icons/Fertilizers/%s.png)' % (mid, name, mid))
    if ('`%s`' % mid) not in glossary:
        report('Docs', '%s (%s): missing from docs/GLOSSARY.md' % (mid, name))
names = defaultdict(list)
for mid, (name, _) in defined.items():
    names[name.lower()].append(mid)
for name, ids in names.items():
    if len(ids) > 1:
        report('Fertilizers', 'Two fertilizers share the name "%s": %s' % (name, ', '.join(ids)))

# ------------------------------------------------------------------ sprouts (tile upgrades)
upgrade_src = read(S, 'Core/Upgrades/UpgradeSystem.cs')
upgrade_defs = read(S, 'Core/Upgrades/UpgradeDefinition.cs')
trait_src = read(S, 'Data/PieceTraitVisualDefaults.cs')
trait_kinds = enum_members(S + '/Core/Pieces/PieceTrait.cs', 'PieceTraitKind')
kind_for = dict((m.group(1), m.group(2)) for m in re.finditer(r'case UpgradeId\.(\w+): return PieceTraitKind\.(\w+);', upgrade_src))
upgrade_names = {}
for m in re.finditer(r'UpgradeId\.(\w+), UpgradePool\.\w+, "([^"]+)",\s*"([^"]*)"', upgrade_defs):
    upgrade_names[m.group(1)] = (m.group(2), m.group(3))
for uid, kind in sorted(kind_for.items()):
    if uid not in upgrade_names:
        report('Sprouts', '%s: maps to trait %s but has no UpgradeDefinition' % (uid, kind))
        continue
    name, desc = upgrade_names[uid]
    if not exists(R, 'Icons/Upgrades/%s.png' % uid):
        report('Sprouts', '%s (%s): no sign icon' % (uid, name))
    if not exists(R, 'Icons/UpgradeGlyphs/%s.png' % uid):
        report('Sprouts', '%s (%s): no glyph icon' % (uid, name))
    if not exists(R, 'Icons/Traits/%s.png' % kind):
        report('Sprouts', '%s (%s): no trait badge icon Traits/%s.png' % (uid, name, kind))
    for what, marker in (('a name', 'case PieceTraitKind.%s: return "' % kind), ('a rarity', 'case PieceTraitKind.%s: return UpgradeRarity' % kind)):
        if marker not in trait_src:
            report('Sprouts', '%s (%s): PieceTraitVisualDefaults has no %s for %s' % (uid, name, what, kind))
    if not re.search(r'case PieceTraitKind\.%s:\s*\n\s*return ' % kind, trait_src):
        report('Sprouts', '%s (%s): PieceTraitVisualDefaults has no description for %s' % (uid, name, kind))
    if desc != desc.strip() or '  ' in desc:
        report('Clarity', '%s: stray spaces in its description' % uid)
for kind in trait_kinds:
    if kind not in kind_for.values() and kind not in ('Bombe', 'Range', 'Eclat', 'Precision', 'Golden', 'Tinted', 'Multiplier'):
        report('Sprouts', 'Trait kind %s is not reachable from any upgrade (dead trait?)' % kind)

# ------------------------------------------------------------------ enemies
enemy_ids = enum_members(S + '/Core/Enemies/EnemyId.cs', 'EnemyId')
enemy_src = read(S, 'Core/Enemies/EnemyCatalog.cs')
for eid in enemy_ids:
    line = next((l for l in enemy_src.split('\n') if 'Id = EnemyId.%s,' % eid in l), None)
    if line is None:
        report('Enemies', '%s: no EnemyDefinition' % eid)
    else:
        if 'Description = ' not in line:
            report('Enemies', '%s: no description' % eid)
        if 'Lore = ' not in line:
            report('Enemies', '%s: no lore line' % eid)
    if not exists(R, 'Enemies/%s.png' % eid):
        report('Enemies', '%s: no portrait (Resources/Enemies/%s.png)' % (eid, eid))

# ------------------------------------------------------------------ weather
weather_ids = enum_members(S + '/Core/Run/WeatherId.cs', 'WeatherId')
weather_src = read(S, 'Core/Run/WeatherCatalog.cs')
for wid in weather_ids:
    if not exists(R, 'Icons/Weather/%s.png' % wid):
        report('Weather', '%s: no icon' % wid)
    if wid != 'Clear' and ('case WeatherId.%s: return "' % wid) not in weather_src:
        report('Weather', '%s: no display name in WeatherCatalog.GetName' % wid)

# ------------------------------------------------------------------ wording that hurts clarity
unit_variants = defaultdict(int)
texts = [(m, d) for m, (n, d) in defined.items()] + [(u, d) for u, (n, d) in upgrade_names.items()]
for ident, text in texts:
    low = text.lower()
    if '  ' in text or text != text.strip():
        report('Clarity', '%s: double or stray spaces in its description: "%s"' % (ident, text))
    if text and text[-1] not in '.)!?':
        report('Clarity', '%s: description does not end with a full stop: "%s"' % (ident, text))
    if text.count('(') != text.count(')'):
        report('Clarity', '%s: unbalanced parentheses: "%s"' % (ident, text))
    if re.search(r'\btodo\b|\bxxx\b|\blorem\b|\bplaceholder\b', low):
        report('Clarity', '%s: looks unfinished: "%s"' % (ident, text))
    if re.search(r'\bscores?\s+golden\b', low):
        report('Clarity', '%s: uses "golden" as jargon instead of a value: "%s"' % (ident, text))
    for variant in ('multiplier', 'mult', 'points', 'pts', 'pt', 'lueur', 'pollen'):
        if re.search(r'\b%s\b' % variant, low):
            unit_variants[variant] += 1
    if re.search(r'\+\d+(\.\d+)?\s*(?!pts|pt|mult|pollen|points|x|%|per|to|more|extra)\w*\s*$', low) and not re.search(r'pts|mult|pollen|points|seed|tile|shuffle|slot|round|rarity|level|cell|sprout|line|group|fertilizer', low):
        report('Clarity', '%s: a "+N" with no unit nearby: "%s"' % (ident, text))
if unit_variants.get('lueur'):
    report('Clarity', 'The old word "Lueur" still appears in %d description(s); the game calls it Pollen' % unit_variants['lueur'])

# ------------------------------------------------------------------ report
print('Checked: %d fertilizers, %d sprout upgrades, %d traits, %d enemies, %d weathers' % (len(modifier_ids), len(kind_for), len(trait_kinds), len(enemy_ids), len(weather_ids)))
order = ['Fertilizers', 'Sprouts', 'Enemies', 'Weather', 'Docs', 'Clarity']
total = 0
for section in order + [s for s in findings if s not in order]:
    items = findings.get(section, [])
    total += len(items)
    print('\n== %s: %d finding(s)' % (section, len(items)))
    for item in items:
        print('  - ' + item)
print('\nTotal: %d' % total)
if '--strict' in sys.argv and total:
    sys.exit(1)
