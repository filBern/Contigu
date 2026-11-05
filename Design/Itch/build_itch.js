// Builds the itch.io page kit (cover, banner, background, icon, key-art screenshots) from the game's own art.
// Usage: NODE_PATH=$(npm root -g) node build_itch.js   (needs Playwright + Chromium)
const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');
const os = require('os');

const ROOT = path.resolve(__dirname, '../..');
const R = 'file://' + ROOT + '/Assets/Resources/';
const OUT = path.join(__dirname, 'out');
const FONT = 'file://' + ROOT + '/Assets/Resources/Colorful_UI/colorful/font/Digitalt.ttf';
const HAND = 'file://' + ROOT + '/Assets/Resources/Font/PatrickHand-Regular.ttf';

const BASE_CSS = `
@font-face { font-family: Digitalt; src: url('${FONT}'); }
@font-face { font-family: Hand; src: url('${HAND}'); }
* { box-sizing: border-box; margin: 0; padding: 0; }
body { width: 100vw; height: 100vh; overflow: hidden; font-family: Digitalt, 'Arial Black', sans-serif; color: #fff7e8; position: relative; background: #12304a; }
.abs { position: absolute; }
.bg { position: absolute; inset: 0; background-size: cover; background-position: center; }
.title-shadow { filter: drop-shadow(0 6px 0 rgba(10,20,30,.55)) drop-shadow(0 0 18px rgba(0,0,0,.4)); }
.tag { font-family: Hand, cursive; font-style: normal; text-shadow: 0 3px 0 rgba(10,20,30,.7); }
.tile { position: absolute; border-radius: 14%; filter: drop-shadow(0 6px 0 rgba(10,20,30,.45)); }
.pill { position: absolute; padding: .25em .8em; border-radius: 999px; border: 4px solid #13212e; font-weight: 900; text-shadow: 0 3px 0 rgba(0,0,0,.35); }
`;
const bg = (name, dim = 0.35) => `<div class="bg" style="background-image:url('${R}Backgrounds/${name}.png')"></div><div class="bg" style="background:linear-gradient(180deg,rgba(8,16,28,${dim}) 0%,rgba(8,16,28,${dim / 2}) 55%,rgba(8,16,28,${dim + 0.25}) 100%)"></div>`;
const T = { poppy: 'Tiles/Ember.png', iris: 'Tiles/Frost.png', sun: 'Tiles/Gold.png', fern: 'Tiles/Moss.png', empty: 'Tiles/Empty.png' };
const tile = (k, x, y, s, rot = 0) => `<img class="tile" src="${R}${T[k]}" style="left:${x}px;top:${y}px;width:${s}px;height:${s}px;transform:rotate(${rot}deg)">`;
const sprig = (mood, x, y, s) => `<img class="abs" src="${R}Characters/Sprig_${mood}.png" style="left:${x}px;top:${y}px;width:${s}px;height:${s}px;filter:drop-shadow(0 8px 0 rgba(10,20,30,.4))">`;
const logo = (x, y, w) => `<img class="abs title-shadow" src="${R}Title/LastBloom.png" style="left:${x}px;top:${y}px;width:${w}px">`;
const icon = (cat, name, x, y, s, rot = 0) => `<img class="abs" src="${R}Icons/${cat}/${name}.png" style="left:${x}px;top:${y}px;width:${s}px;height:${s}px;transform:rotate(${rot}deg);filter:drop-shadow(0 6px 0 rgba(10,20,30,.45))">`;
const enemy = (name, x, y, s, flip = false) => `<img class="abs" src="${R}Enemies/${name}.png" style="left:${x}px;top:${y}px;width:${s}px;height:${s}px;${flip ? 'transform:scaleX(-1);' : ''}filter:drop-shadow(0 8px 0 rgba(10,20,30,.45))">`;
const ground = (h, color = '#0d2a22') => `<div class="abs" style="left:0;right:0;bottom:0;height:${h}px;background:${color};border-top:6px solid #13212e;border-radius:60% 60% 0 0 / 30px 30px 0 0"></div>`;
const tagline = 'The last garden still blooms, at the edge of ruin.';

const fert = ['Chaine','Couronne','Faucheur','Jardinier','MultQuatre','Prisme','Saisons','Haie','Combo','Tirelire','Forteresse','Pourfendeur'];
const pages = {};

pages['cover-630x500'] = { w: 630, h: 500, scales: [1, 2], html: `
${bg('Spring', 0.15)}
${ground(70)}
${logo(35, 40, 560)}
<div class="abs tag" style="left:0;right:0;top:215px;text-align:center;font-size:27px;color:#ffe9a8">A garden-puzzle roguelike</div>
${sprig('Cheer', 20, 262, 170)}
${tile('poppy', 215, 296, 100, -5)}${tile('iris', 322, 308, 100, 4)}${tile('sun', 430, 296, 100, -3)}
${tile('fern', 268, 190, 0)}
<div class="abs tag" style="left:0;right:0;bottom:12px;text-align:center;font-size:19px;color:#fff7e8">${tagline}</div>
` };

pages['banner-1920x500'] = { w: 1920, h: 500, scales: [1], html: `
${bg('Spring', 0.1)}
${ground(90)}
${logo(120, 70, 880)}
<div class="abs tag" style="left:140px;top:350px;font-size:44px;color:#ffe9a8">A garden-puzzle roguelike</div>
<div class="abs tag" style="left:140px;top:405px;font-size:30px;color:#fff7e8">${tagline}</div>
${sprig('Cheer', 1090, 195, 250)}
${tile('poppy', 1360, 110, 150, -6)}${tile('iris', 1520, 180, 150, 5)}${tile('sun', 1360, 270, 150, 4)}${tile('fern', 1520, 340, 150, -4)}
` };

pages['background-1920x1080'] = { w: 1920, h: 1080, scales: [1], html: `
<div class="bg" style="background-image:url('${R}Backgrounds/Spring.png');filter:blur(1px)"></div>
<div class="bg" style="background:linear-gradient(180deg,rgba(8,16,28,.55),rgba(8,16,28,.35) 50%,rgba(8,16,28,.7))"></div>
` };

pages['icon-512'] = { w: 512, h: 512, scales: [1], html: `
<div class="bg" style="background:radial-gradient(circle at 50% 38%,#2b6a5a 0%,#12304a 70%)"></div>
<div class="abs" style="inset:26px;border-radius:84px;border:10px solid #e8a82e;box-shadow:0 0 0 8px #13212e inset"></div>
${sprig('Happy', 76, 66, 360)}
` };

// ---- key-art screenshots (1280x800), built from the game's art — illustrations, not captures
const caption = (head, sub) => `
<div class="abs title-shadow" style="left:0;right:0;top:34px;text-align:center;font-size:62px;color:#ffe9a8;letter-spacing:1px">${head}</div>
<div class="abs tag" style="left:0;right:0;top:112px;text-align:center;font-size:34px;color:#fff7e8">${sub}</div>`;

const CELL = 92;
function board(x, y, cells) {
  let h = `<div class="abs" style="left:${x - 22}px;top:${y - 22}px;width:${CELL * 6 + 44}px;height:${CELL * 6 + 44}px;background:#4e3320;border:8px solid #13212e;border-radius:34px;box-shadow:0 12px 0 rgba(10,20,30,.5)"></div>`;
  for (let r = 0; r < 6; r++) for (let c = 0; c < 6; c++) {
    h += `<img class="abs" src="${R}${T.empty}" style="left:${x + c * CELL}px;top:${y + r * CELL}px;width:${CELL - 4}px;height:${CELL - 4}px;opacity:.9">`;
  }
  for (const [c, r, k] of cells) h += `<img class="abs" src="${R}${T[k]}" style="left:${x + c * CELL}px;top:${y + r * CELL}px;width:${CELL - 4}px;height:${CELL - 4}px;filter:drop-shadow(0 4px 0 rgba(10,20,30,.4))">`;
  return h;
}
const popup = (txt, x, y, color = '#ffe9a8', size = 54, rot = -6) => `<div class="abs title-shadow" style="left:${x}px;top:${y}px;font-size:${size}px;color:${color};transform:rotate(${rot}deg);-webkit-text-stroke:3px #13212e;paint-order:stroke fill">${txt}</div>`;

pages['screenshot-1-place-and-chain'] = { w: 1280, h: 800, scales: [1], html: `
${bg('Spring', 0.45)}
${caption('Plant seeds. Chain colors.', 'Connect groups of one color and harvest whole lines.')}
${board(120, 205, [
  [0,0,'poppy'],[1,0,'poppy'],[2,0,'iris'],[3,0,'iris'],[4,0,'sun'],[5,0,'fern'],
  [0,1,'poppy'],[1,1,'iris'],[2,1,'iris'],[3,1,'sun'],[4,1,'sun'],[5,1,'fern'],
  [0,2,'sun'],[1,2,'sun'],[2,2,'fern'],[3,2,'fern'],[4,2,'poppy'],[5,2,'poppy'],
  [0,3,'fern'],[1,3,'fern'],[2,3,'poppy'],[3,3,'poppy'],[4,3,'iris'],[5,3,'iris'],
  [0,4,'iris'],[1,4,'poppy'],[2,4,'sun'],[3,4,'sun'],[4,4,'fern'],[5,4,'sun']
])}
<div class="abs" style="left:790px;top:205px;width:370px;height:560px;background:#4e3320;border:8px solid #13212e;border-radius:34px;box-shadow:0 12px 0 rgba(10,20,30,.5)"></div>
<div class="abs" style="left:822px;top:236px;width:306px;height:150px;background:#fff7e8;border:6px solid #13212e;border-radius:22px;color:#13212e;text-align:center">
  <div style="font-size:30px;margin-top:14px;opacity:.65">SCORE</div><div style="font-size:60px;margin-top:2px">1,240 × 8</div></div>
${tile('poppy', 840, 430, 90)}${tile('poppy', 934, 430, 90)}${tile('poppy', 840, 524, 90)}
${tile('iris', 1040, 430, 90)}${tile('iris', 1040, 524, 90)}
${tile('sun', 840, 646, 90)}${tile('sun', 934, 646, 90)}${tile('sun', 1028, 646, 90)}
${popup('+36', 330, 160, '#ffe9a8', 76, -8)}
${popup('x8', 600, 430, '#ff9a8a', 96, 8)}
${sprig('Cheer', 36, 18, 130)}${sprig('Happy', 1120, 18, 130)}
` };

pages['screenshot-2-fertilizers'] = { w: 1280, h: 800, scales: [1], html: `
${bg('Summer', 0.45)}
${caption('Grow a build of fertilizers.', 'Over a hundred fertilizers and sprout upgrades. Stack Mult, bend the rules.')}
${fert.map((n, i) => icon('Fertilizers', n, 150 + (i % 4) * 255, 190 + Math.floor(i / 4) * 195, 185, (i % 2 ? 4 : -4))).join('')}
${popup('x2', 50, 215, '#ff9a8a', 96, -10)}${popup('+4 Mult', 1010, 175, '#ffe9a8', 60, 8)}
` };

pages['screenshot-3-pests'] = { w: 1280, h: 800, scales: [1], html: `
${bg('Autumn', 0.5)}
${caption('Pests strike back.', 'Locks, poison and thieves fight on the board. No two runs meet them in the same order.')}
${['Locker','Poisoner','Thief','Blightmoth','Leech'].map((n, i) => enemy(n, 65 + i * 240, 215, 215, i % 2 === 1)).join('')}
${enemy('HeavyLocker', 300, 450, 300)}${enemy('ColorHater', 680, 450, 300, true)}
${popup('BOSS', 560, 700, '#ff9a8a', 84, -4)}
` };

pages['screenshot-4-seasons'] = { w: 1280, h: 800, scales: [1], html: `
${bg('Winter', 0.55)}
${caption('Six seasons, six challenges.', 'Spring to Greenhouse: harsher weather, fewer colors, tighter budgets. Earn Stars to unlock each one.')}
${['Spring','Summer','Autumn','Winter','Orchard','Greenhouse'].map((n, i) => {
  const x = 95 + (i % 3) * 385, y = 215 + Math.floor(i / 3) * 285;
  return `<div class="abs" style="left:${x}px;top:${y}px;width:350px;height:250px;border-radius:30px;border:7px solid #13212e;background-image:url('${R}Backgrounds/${n}.png');background-size:cover;background-position:center;box-shadow:0 10px 0 rgba(10,20,30,.5)"></div>
  <div class="abs" style="left:${x}px;top:${y + 170}px;width:350px;height:80px;border-radius:0 0 24px 24px;background:rgba(10,20,30,.7);text-align:center;font-size:44px;line-height:82px">${n.toUpperCase()}</div>
  ${icon('Challenges', n, x + 95, y + 18, 160)}`;
}).join('')}
` };

pages['screenshot-5-touch'] = { w: 1280, h: 800, scales: [1], html: `
${bg('Orchard', 0.45)}
${caption('Made for touch.', 'One finger, no hover, no menus to fight. Plays in your browser, on iPad or desktop.')}
${sprig('Happy', 70, 230, 400)}
<div class="abs title-shadow" style="left:520px;top:240px;width:700px;font-size:44px;line-height:1.5">
  <div>1  Drag a seed onto the garden</div>
  <div style="color:#ffe9a8">2  Chain colors, harvest lines</div>
  <div style="color:#a4ebcc">3  Shuffle when you are stuck</div>
  <div style="color:#ff9a8a">4  Earn Stars, unlock seasons</div>
</div>
${tile('poppy', 540, 590, 120, -6)}${tile('iris', 690, 606, 120, 5)}${tile('sun', 840, 590, 120, -4)}${tile('fern', 990, 606, 120, 6)}
` };


// ---- how the finished page is arranged (a layout guide, not a copy of itch.io's own UI)
const SHOTS = { 1: 'screenshot-1-place-and-chain', 2: 'screenshot-2-fertilizers', 3: 'screenshot-3-pests', 4: 'screenshot-4-seasons', 5: 'screenshot-5-touch' };
const shot = (n, w) => `<img src="file://${OUT}/${SHOTS[n]}.png" style="width:${w}px;border-radius:14px;border:4px solid #13212e;box-shadow:0 8px 0 rgba(10,20,30,.45)">`;
pages['page-layout-guide'] = { w: 1400, h: 2600, scales: [1], html: `
<style>
  body { background:#12304a url('${R}Backgrounds/Spring.png') center top / cover no-repeat; font-family: Arial, sans-serif; }
  .col { position:absolute; left:100px; width:1200px; }
  .num { position:absolute; width:46px; height:46px; border-radius:50%; background:#e8a82e; color:#13212e; border:4px solid #13212e; font:900 26px/38px Digitalt, Arial; text-align:center; z-index:5 }
  .note { position:absolute; width:300px; font:22px/1.3 Hand, cursive; color:#ffe9a8; text-shadow:0 2px 0 #000 }
  h2 { font-family: Digitalt; font-size:44px; color:#ffe9a8; margin:0 0 12px; letter-spacing:1px }
  p, li { font-size:23px; line-height:1.5; color:#fff7e8 }
  ul { margin:8px 0 0 28px }
</style>
<div class="bg" style="background:linear-gradient(180deg,rgba(8,16,28,.6),rgba(8,16,28,.82))"></div>
<div class="col" style="top:30px"><img src="file://${OUT}/banner-1920x500.png" style="width:1200px;border-radius:0 0 18px 18px;border:4px solid #13212e"></div>
<div class="num" style="left:52px;top:130px">1</div><div class="note" style="left:1310px;top:60px;width:80px"></div>
<div class="col" style="top:340px;height:640px">
  <div class="abs" style="left:0;top:0;width:880px;height:550px;background:#0a1420;border:4px solid #13212e;border-radius:16px;text-align:center;line-height:550px;font:44px Digitalt;color:#ffe9a8">▶ CLICK TO LAUNCH IN FULLSCREEN</div>
  <div class="abs" style="left:0;top:560px;width:880px;color:#fff7e8;font:20px Arial;opacity:.8;text-align:center">Game embed — viewport 1280×800, mobile friendly, landscape</div>
  <div class="abs" style="left:910px;top:0;width:290px;background:#0f2538;border:4px solid #13212e;border-radius:14px;padding:18px;font:20px/1.6 Arial;color:#fff7e8">
    <img src="file://${OUT}/cover-630x500@2x.png" style="width:100%;border-radius:10px;margin-bottom:12px">
    <b>Status:</b> In development<br><b>Platforms:</b> HTML5<br><b>Genre:</b> Puzzle<br><b>Made with:</b> Unity<br><b>Tags:</b> puzzle, roguelike, deckbuilder, cute, touch-friendly<br><b>Input:</b> Touchscreen, mouse
  </div>
</div>
<div class="num" style="left:52px;top:350px">2</div><div class="num" style="left:1005px;top:350px">3</div>
<div class="col" style="top:1010px">
  <h2>LASTBLOOM</h2>
  <p><i>The last garden still blooms, at the edge of ruin.</i></p>
  <p>A block-placing puzzle with the run structure of a deckbuilding roguelike. Drop flower seeds on a small garden, connect groups of one color, harvest lines, and turn every placement into damage against the pests.</p>
</div>
<div class="num" style="left:52px;top:1020px">4</div>
<div class="col" style="top:1300px">${shot(1, 1200)}</div>
<div class="num" style="left:52px;top:1310px">5</div>
<div class="col" style="top:2090px">
  <h2>What you do</h2>
  <ul><li>Plant seeds, chain colors, harvest lines</li><li>Build a bag of seeds and a rack of fertilizers</li><li>Beat 12 rounds, a boss every four, drawn differently every run</li><li>Unlock six seasons</li></ul>
</div>
<div class="num" style="left:52px;top:2100px">6</div>
<div class="col" style="top:2380px;display:flex;gap:16px">${shot(2, 290)}${shot(3, 290)}${shot(4, 290)}${shot(5, 290)}</div>
<div class="num" style="left:52px;top:2390px">7</div>
` };

(async () => {
  const browser = await chromium.launch();
  const only = process.argv[2];
  for (const [name, p] of Object.entries(pages)) {
    if (only && !name.includes(only)) continue;
    for (const scale of p.scales) {
      const page = await browser.newPage({ viewport: { width: p.w, height: p.h }, deviceScaleFactor: scale });
      // Loaded from a real file: a page set with setContent cannot read the file:// art and fonts.
      const htmlFile = path.join(os.tmpdir(), 'itch_' + name + '.html');
      fs.writeFileSync(htmlFile, `<html><head><meta charset="utf-8"><style>${BASE_CSS}</style></head><body>${p.html}</body></html>`);
      await page.goto('file://' + htmlFile);
      await page.evaluate(() => document.fonts.ready);
      await page.waitForTimeout(400);
      const file = scale === 1 ? name : `${name}@${scale}x`;
      await page.screenshot({ path: path.join(OUT, file + '.png'), clip: { x: 0, y: 0, width: p.w, height: p.h } });
      await page.close();
      console.log('wrote', file);
    }
  }
  await browser.close();
})();
