#!/usr/bin/env bash
# Regenerates the wiki page from the game's code: Tools/wiki/out/wiki.html + out/img/*.webp (publish both with the Artifact tool, `files` = out/img/*).
set -euo pipefail
cd "$(dirname "$0")"
mkdir -p out
(cd Dump && dotnet run -c Release > ../out/dump.json)
python3 build_data.py
python3 build_atlas.py
python3 - <<'PY'
t = open('wiki.template.html', encoding='utf-8').read()
d = open('out/data.json', encoding='utf-8').read().replace('</', '<\\/')
open('out/wiki.html', 'w', encoding='utf-8').write(t.replace('/*DATA*/', d))
print('out/wiki.html written')
PY
