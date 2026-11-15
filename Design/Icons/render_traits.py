#!/usr/bin/env python3
"""Renders the sprout icons (traits/*.svg, bare glyphs with a soft shadow) to 128 px PNGs in Assets/Resources/Icons/Traits (the files the game loads; .meta files
stay). Playwright (NODE_PATH=$(npm root -g)). Run from this folder, after generate_traits.py."""
import glob, os, subprocess
OUT = '../../Assets/Resources/Icons/Traits'
names = sorted(os.path.splitext(os.path.basename(p))[0] for p in glob.glob('traits/*.svg') if os.path.exists('%s/%s.png' % (OUT, os.path.splitext(os.path.basename(p))[0])))
js = """
const { chromium } = require('playwright');
const fs = require('fs');
(async () => {
  const b = await chromium.launch();
  const p = await b.newPage({ viewport: { width: 64, height: 64 }, deviceScaleFactor: 2 });
  for (const name of %s) {
    await p.setContent('<html><body style="margin:0;background:transparent">' + fs.readFileSync('traits/' + name + '.svg', 'utf8') + '</body></html>');
    await p.screenshot({ path: '%s/' + name + '.png', omitBackground: true, clip: { x: 0, y: 0, width: 64, height: 64 } });
  }
  await b.close();
})();
""" % (names, OUT)
open('_render.js', 'w').write(js)
env = dict(os.environ, NODE_PATH=subprocess.check_output(['npm', 'root', '-g']).decode().strip())
subprocess.run(['node', '_render.js'], check=True, env=env)
os.remove('_render.js')
print('rendered', len(names), 'sprout icons')
