// Renders Design/Weather/svg/<Name>.svg to Assets/Resources/Icons/Weather/<Name>.png (128 px). Run from Design/Weather: NODE_PATH=$(npm root -g) node render_weather.js
const { chromium } = require('playwright');
const fs = require('fs');
(async () => {
  const b = await chromium.launch();
  const p = await b.newPage({ viewport: { width: 128, height: 128 }, deviceScaleFactor: 1 });
  let n = 0;
  for (const f of fs.readdirSync('svg')) {
    if (!f.endsWith('.svg')) continue;
    const name = f.replace('.svg', '');
    await p.setContent('<html><body style="margin:0;background:transparent">' + fs.readFileSync('svg/' + f, 'utf8') + '</body></html>');
    await p.screenshot({ path: '../../Assets/Resources/Icons/Weather/' + name + '.png', omitBackground: true, clip: { x: 0, y: 0, width: 128, height: 128 } });
    n++;
  }
  await b.close();
  console.log('rendered', n);
})();
