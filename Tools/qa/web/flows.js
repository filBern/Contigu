// node flows.js <build folder> [shop|save|reload|all]
// Longer end-to-end flows: the shop (with purchases) and Save & quit -> Continue. Needs the build's CONTIGU_DEBUG define for the cheats.
const { launch, sleep } = require('./qa');

const dir = process.argv[2];
const which = process.argv[3] || 'all';
if (!dir) { console.error('usage: node flows.js <build folder> [shop|save|reload|all]'); process.exit(2); }
const problems = [];
const note = (m) => { problems.push(m); console.log('PROBLEM: ' + m); };

async function startRun(qa) {
  await qa.clickLabel('Play');
  await qa.waitState((s) => s.scene === 'Main', 30000, 'the game scene');
  await sleep(2000);
  await qa.gameCommand('unlockAll');
  await sleep(400);
  await qa.clickLabel('Play');
  await qa.waitState((s) => s.game && s.game.state === 'InProgress', 30000, 'a run in progress');
  await sleep(2500);
  await qa.dismissGuide();
}

async function placeSome(qa, n) {
  for (let i = 0; i < n; i++) {
    const s = await qa.idle();
    const g = s.game;
    const m = g.moves[0];
    if (!m) { return; }
    const slot = await qa.buttonNamed('Slot' + m.slot);
    if (!slot) { return; }
    await qa.click(slot.x, slot.y);
    await sleep(150);
    const cell = g.cells[m.y * g.grid.length + m.x];
    await qa.click(cell.x, cell.y);
    await sleep(300);
  }
  await qa.idle();
}

async function reachShop(qa) {
  await placeSome(qa, 3);
  const r = await qa.gameCommand('forceRoundWin');
  if (r === 'ok') {
    return qa.waitState((s) => s.game && s.game.state === 'AwaitingShop', 60000, 'the shop');
  }
  // No cheats in this build (CONTIGU_DEBUG not defined): play random legal moves until the round is won.
  console.log('No cheat commands (' + r + '): playing the round for real');
  for (let guard = 0; guard < 150; guard++) {
    const s = await qa.idle();
    const g = s.game;
    if (g.state === 'AwaitingShop') { return s; }
    if (g.state !== 'InProgress') { throw new Error('The run ended (' + g.state + ') before the shop; try again'); }
    const m = g.moves[Math.floor(Math.random() * g.moves.length)];
    if (!m) {
      const shuffle = (await qa.buttons()).find((b) => /shuffle/i.test(b.text + ' ' + b.name));
      if (!shuffle) { throw new Error('No legal move and no shuffle'); }
      await qa.click(shuffle.x, shuffle.y);
      continue;
    }
    const slot = await qa.buttonNamed('Slot' + m.slot);
    if (!slot) { await sleep(300); continue; }
    await qa.click(slot.x, slot.y);
    await sleep(120);
    await qa.click(g.cells[m.y * g.grid.length + m.x].x, g.cells[m.y * g.grid.length + m.x].y);
    await sleep(200);
  }
  throw new Error('Never reached the shop');
}

async function shopFlow() {
  console.log('--- shop flow');
  const qa = await launch({ dir, port: 8811, shots: 'qa-shots/shop' });
  try {
    await startRun(qa);
    let s = await reachShop(qa);
    await sleep(2500);
    await qa.shot('01-shop');
    console.log('Shop: blister ' + JSON.stringify(s.game.blister) + ' workshop ' + JSON.stringify(s.game.workshop));
    await qa.gameCommand('grantPollen', 300);
    await sleep(500);
    s = await qa.state();
    if (s.game.pollen < 20) { console.log('Only ' + s.game.pollen + ' Pollen (no cheats): buying what is affordable'); }
    s = await qa.state();
    // Hover then buy every item once, resolving whatever choice screen follows.
    for (const prefix of ['BlisterSlot_', 'UpgSlot_']) {
      for (let i = 0; i < 4; i++) {
        const b = await qa.buttonNamed(prefix + i);
        if (!b) { continue; }
        await qa.hover(b.x, b.y);
        await sleep(400);
        await qa.shot('hover-' + prefix + i);
        const before = await qa.state();
        await qa.click(b.x, b.y);
        await sleep(1800);
        await qa.shot('after-click-' + prefix + i);
        // A choice screen? pick the first visible option-looking button (confirm if asked).
        for (let step = 0; step < 6; step++) {
          const st = await qa.state();
          if (st.game.state !== 'AwaitingShop') { break; }
          const cancel = (st.buttons || []).filter((x) => x.visible).find((x) => /abandonpurchase|cancel purchase/i.test(x.text + ' ' + x.name));
          if (!cancel) { break; }
          const options = (st.buttons || []).filter((x) => x.visible && /^(Option_|Tile|Seed|Entry)/i.test(x.name));
          if (options.length === 0) { await qa.shot('choice-' + prefix + i + '-' + step); await qa.click(cancel.x, cancel.y); await sleep(500); await qa.click(cancel.x, cancel.y); await sleep(800); break; }
          // "Select N seeds": pick N options; a single-choice screen (packs) takes one.
          const need = Math.max(1, Number(((st.texts || []).map((t) => /select (\d+) seed/i.exec(t.text)).find(Boolean) || [0, 1])[1]));
          for (let k = 0; k < Math.min(need, options.length); k++) { await qa.click(options[k].x, options[k].y); await sleep(500); }
          await sleep(500);
          await qa.shot('choice-' + prefix + i + '-' + step + '-picked');
          const confirm = (await qa.buttons()).find((x) => /confirm|choose|ok/i.test(x.text + ' ' + x.name) && !/cancel/i.test(x.text + ' ' + x.name));
          if (confirm) { await qa.click(confirm.x, confirm.y); await sleep(1200); }
        }
        if (qa.errors.length) { note('Console error after ' + prefix + i + ': ' + qa.errors.slice(-3).join(' | ')); }
      }
    }
    s = await qa.state();
    console.log('After buying: pollen ' + s.game.pollen + ', fertilizers ' + JSON.stringify(s.game.fertilizers));
    await qa.shot('02-shop-after');
    await qa.clickLabel('Leave');
    s = await qa.waitState((st) => st.game && st.game.state === 'InProgress' && st.game.round === 2, 30000, 'round 2');
    await sleep(2500);
    await qa.shot('03-round2');
    console.log('Round 2 started, hand ' + s.game.hand.map((h) => h.shape).join(','));
  } catch (e) {
    note('Shop flow stopped: ' + e.message);
    try { await qa.shot('failure'); } catch (_) {}
  }
  if (qa.errors.length) { note(qa.errors.length + ' console error(s) in the shop flow:\n  ' + qa.errors.slice(0, 8).join('\n  ')); }
  await qa.close();
}

async function saveFlow() {
  console.log('--- save and resume flow');
  const qa = await launch({ dir, port: 8812, shots: 'qa-shots/save' });
  try {
    await startRun(qa);
    await placeSome(qa, 2);
    let s = await qa.state();
    const before = { round: s.game.round, pollen: s.game.pollen, shuffles: s.game.shuffles, fertilizers: s.game.fertilizers.length };
    console.log('Before quitting: ' + JSON.stringify(before));
    await qa.clickLabel('Menu'); await sleep(800); await qa.shot('01-settings');
    await qa.clickLabel('Save'); // "Save & quit"
    await qa.waitState((st) => st.scene === 'MainMenu', 30000, 'the main menu');
    await sleep(2000);
    await qa.shot('02-menu-after-quit');
    await qa.clickLabel('Play');
    await qa.waitState((st) => st.scene === 'Main', 30000, 'the game scene');
    await sleep(2500);
    await qa.shot('03-challenge-screen');
    const cont = (await qa.buttons()).find((b) => /continue/i.test(b.text + ' ' + b.name));
    if (!cont) { note('No Continue button after Save & quit'); }
    else {
      await qa.click(cont.x, cont.y);
      s = await qa.waitState((st) => st.game && st.game.state === 'InProgress', 30000, 'the resumed run');
      await sleep(2500);
      await qa.dismissGuide().catch(() => 0);
      s = await qa.state();
      await qa.shot('04-resumed');
      const after = { round: s.game.round, pollen: s.game.pollen, shuffles: s.game.shuffles, fertilizers: s.game.fertilizers.length };
      console.log('After resuming: ' + JSON.stringify(after));
      if (JSON.stringify(before) !== JSON.stringify(after)) { note('The resumed run differs from the saved one: ' + JSON.stringify(before) + ' vs ' + JSON.stringify(after)); }
      if (s.game.seedsLeft === 0) { note('The resumed round has no seeds'); }
    }
  } catch (e) {
    note('Save flow stopped: ' + e.message);
    try { await qa.shot('failure'); } catch (_) {}
  }
  if (qa.errors.length) { note(qa.errors.length + ' console error(s) in the save flow:\n  ' + qa.errors.slice(0, 8).join('\n  ')); }
  await qa.close();
}

async function reloadFlow() {
  console.log('--- save survives a page reload');
  const qa = await launch({ dir, port: 8813, shots: 'qa-shots/reload' });
  try {
    await startRun(qa);
    let s = await reachShop(qa);
    await sleep(1500);
    const before = { state: s.game.state, round: s.game.round, pollen: s.game.pollen };
    console.log('Before quitting from the shop: ' + JSON.stringify(before));
    await qa.clickLabel('Menu'); await sleep(800);
    await qa.clickLabel('Save');
    await qa.waitState((st) => st.scene === 'MainMenu', 30000, 'the main menu');
    await sleep(4000); // let the browser copy the files into IndexedDB
    await qa.page.reload();
    await qa.waitState((st) => st.screenW > 0, 120000, 'the game after the reload');
    await sleep(2000);
    await qa.clickLabel('Play');
    await qa.waitState((st) => st.scene === 'Main', 30000, 'the game scene');
    await sleep(2500);
    const cont = (await qa.buttons()).find((b) => /continue/i.test(b.text + ' ' + b.name));
    if (!cont) { note('No Continue button after reloading the page: the save did not survive'); }
    else {
      await qa.click(cont.x, cont.y);
      s = await qa.waitState((st) => st.game && (st.game.state === 'AwaitingShop' || st.game.state === 'InProgress'), 30000, 'the resumed run');
      await sleep(2000);
      s = await qa.state();
      const after = { state: s.game.state, round: s.game.round, pollen: s.game.pollen };
      console.log('After reload + Continue: ' + JSON.stringify(after));
      if (JSON.stringify(before) !== JSON.stringify(after)) { note('The run resumed after a reload differs from the saved one: ' + JSON.stringify(before) + ' vs ' + JSON.stringify(after)); }
      await qa.shot('01-resumed');
    }
  } catch (e) {
    note('Reload flow stopped: ' + e.message);
    try { await qa.shot('failure'); } catch (_) {}
  }
  if (qa.errors.length) { note(qa.errors.length + ' console error(s) in the reload flow:\n  ' + qa.errors.slice(0, 8).join('\n  ')); }
  await qa.close();
}

(async () => {
  if (which === 'shop' || which === 'all') { await shopFlow(); }
  if (which === 'save' || which === 'all') { await saveFlow(); }
  if (which === 'reload' || which === 'all') { await reloadFlow(); }
  console.log(problems.length ? '\n' + problems.length + ' problem(s).' : '\nAll flows passed.');
  process.exit(problems.length ? 1 : 0);
})();
