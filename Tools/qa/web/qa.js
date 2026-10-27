// Drives a LASTBLOOM WebGL build opened with ?qa=1 (see Assets/Scripts/Presentation/QaBridge.cs).
// Coordinates are Unity screen pixels, top-left origin (what the state reports).
const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');
const { spawn } = require('child_process');

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function launch({ dir, port = 8765, headless = true, shots = 'qa-shots', loadTimeoutMs = 240000, viewport = { width: 1280, height: 800 } }) {
  const server = spawn('python3', [path.join(__dirname, 'serve.py'), dir, String(port)], { stdio: 'ignore' });
  await sleep(600);
  const browser = await chromium.launch({
    headless,
    args: ['--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist', '--enable-webgl', '--autoplay-policy=no-user-gesture-required'],
  });
  const page = await browser.newPage({ viewport });
  const consoleLines = [];
  const errors = [];
  page.on('console', (m) => {
    const line = '[' + m.type() + '] ' + m.text();
    consoleLines.push(line);
    if (m.type() === 'error' || /exception|error|NullReference|IndexOutOfRange/i.test(m.text())) { errors.push(line); }
  });
  page.on('pageerror', (e) => errors.push('[pageerror] ' + e.message));
  fs.mkdirSync(shots, { recursive: true });

  const qa = {
    page, consoleLines, errors, shotsDir: shots,
    async state() {
      const json = await page.evaluate(() => (window.__qa && window.__qa.stateJson) || null);
      return json ? JSON.parse(json) : null;
    },
    async waitState(predicate, timeoutMs = 20000, what = 'a state') {
      const end = Date.now() + timeoutMs;
      let last = null;
      while (Date.now() < end) {
        last = await qa.state();
        if (last && predicate(last)) { return last; }
        await sleep(150);
      }
      throw new Error('Timed out waiting for ' + what + '. Last state: ' + JSON.stringify(last && { scene: last.scene, game: last.game && { state: last.game.state, round: last.game.round, sequence: last.game.sequence }, buttons: (last.buttons || []).filter((b) => b.visible).map((b) => b.text || b.name) }));
    },
    async command(cmd) {
      const id = await page.evaluate((c) => window.__qa.send(c), cmd);
      const end = Date.now() + 10000;
      while (Date.now() < end) {
        const r = await page.evaluate((i) => window.__qa.results[i], id);
        if (r !== undefined && r !== null) { return r; }
        await sleep(60);
      }
      throw new Error('No answer to command ' + JSON.stringify(cmd));
    },
    /** A pointer click through the game's event system at Unity pixel (x, y). */
    click(x, y) { return qa.command({ cmd: 'click', x, y }); },
    hover(x, y) { return qa.command({ cmd: 'hover', x, y }); },
    clickText(text) { return qa.command({ cmd: 'clickText', text }); },
    /** A real mouse click in the browser (goes through the whole input stack), mapped from Unity pixels. */
    async realClick(x, y) {
      const st = await qa.state();
      const box = await page.locator('canvas').first().boundingBox();
      await page.mouse.click(box.x + (x / st.screenW) * box.width, box.y + (y / st.screenH) * box.height);
    },
    async buttons() { const s = await qa.state(); return (s.buttons || []).filter((b) => b.visible); },
    async buttonNamed(name) { return (await qa.buttons()).find((b) => b.name === name); },
    async clickButtonNamed(name) {
      const b = await qa.buttonNamed(name);
      if (!b) { throw new Error('No visible button named ' + name); }
      return qa.click(b.x, b.y);
    },
    gameCommand(cmd, arg) { return qa.command({ cmd, arg: arg === undefined ? '' : String(arg) }); },
    async shot(name) {
      const file = path.join(shots, name + '.png');
      await page.screenshot({ path: file });
      return file;
    },
    /** Waits until the game stops playing its placement/shuffle sequence and the state has settled. */
    async idle(timeoutMs = 60000) {
      const end = Date.now() + timeoutMs;
      let calm = 0;
      while (Date.now() < end) {
        const s = await qa.state();
        if (s && (!s.game || !s.game.sequence)) { calm += 1; if (calm >= 4) { return s; } } else { calm = 0; }
        await sleep(200);
      }
      throw new Error('The game never became idle');
    },
    async close() { await browser.close(); server.kill(); },
  };

  await page.goto('http://127.0.0.1:' + port + '/index.html?qa=1');
  await qa.waitState((s) => s.screenW > 0, loadTimeoutMs, 'the game to load (is the build in ' + dir + '?)');
  return qa;
}

module.exports = { launch, sleep };
