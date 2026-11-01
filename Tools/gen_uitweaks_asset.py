#!/usr/bin/env python3
"""Writes Assets/Resources/UITweaks.asset from the defaults declared in Assets/Scripts/Presentation/UITweaks.cs.
Only needed when a field is added to UITweaks (Unity keeps the values you edited in the Inspector, so do not run it to 'reset': delete the asset instead).
Usage: python3 Tools/gen_uitweaks_asset.py"""
import os, re, uuid

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
src = open(os.path.join(ROOT, 'Assets/Scripts/Presentation/UITweaks.cs'), encoding='utf-8').read()
script_guid = re.search(r'guid: (\w+)', open(os.path.join(ROOT, 'Assets/Scripts/Presentation/UITweaks.cs.meta')).read()).group(1)

def num(s):
    return s.strip().rstrip('f')

lines = []
for m in re.finditer(r'public (float|int|Color|Vector2) (\w+) = (.+?);', src):
    kind, name, value = m.groups()
    if kind in ('float', 'int'):
        lines.append('  %s: %s' % (name, num(value)))
    elif kind == 'Color':
        r, g, b, a = [num(x) for x in re.search(r'\((.*)\)', value).group(1).split(',')]
        lines.append('  %s: {r: %s, g: %s, b: %s, a: %s}' % (name, r, g, b, a))
    else:
        x, y = [num(v) for v in re.search(r'\((.*)\)', value).group(1).split(',')]
        lines.append('  %s: {x: %s, y: %s}' % (name, x, y))

asset = """%%YAML 1.1
%%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: %s, type: 3}
  m_Name: UITweaks
  m_EditorClassIdentifier: 
%s
""" % (script_guid, '\n'.join(lines))
path = os.path.join(ROOT, 'Assets/Resources/UITweaks.asset')
open(path, 'w', encoding='utf-8').write(asset)
meta = path + '.meta'
if not os.path.exists(meta):
    open(meta, 'w').write("fileFormatVersion: 2\nguid: %s\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % uuid.uuid4().hex)
print('wrote', path, len(lines), 'values')
