// node smoke.js <build folder> [--rounds N]
// Loads the build, walks the menus, plays a few rounds by choosing legal moves from the reported state, visits the shop,
// and fails on any console error or impossible state. Screenshots land in qa-shots/.
const { launch, sleep } = require('./qa');

const dir = process.argv[2];
const rounds = Number((process.argv.indexOf('--rounds') > 0 && process.argv[process.argv.indexOf('--rounds') + 1]) || 3);
if (!dir) { console.error('usage: node smoke.js <build folder> [--rounds N]'); process.exit(2); }

(async () => {
  const problems = [];
  const note = (m) => { problems.push(m); console.log('PROBLEM: ' + m); };
  const qa = await launch({ dir });
  try {
    let s = await qa.state();
    console.log('Loaded scene ' + s.scene + ' at ' + s.screenW + 'x' + s.screenH);
    await qa.shot('01-menu');

    // Main menu: Collection and back, then Play.
    if (s.scene !== 'Main') {
      await qa.clickLabel('Collection'); await sleep(800); await qa.shot('02-collection');
      await qa.clickLabel('Close'); await sleep(500);
      await qa.clickLabel('Play');
      await qa.waitState((st) => st.scene === 'Main', 30000, 'the game scene');
    }
    await sleep(1500);
    await qa.gameCommand('unlockAll');
    await sleep(300);
    await qa.shot('03-challenges');
    try { await qa.clickLabel('Play'); } catch (e) { note('No Play button on the challenge screen'); }
    s = await qa.waitState((st) => st.game && st.game.state === 'InProgress', 30000, 'a run in progress');
    await sleep(2500);
    const guideSteps = await qa.dismissGuide();
    console.log('Sprig guide: ' + guideSteps + ' step(s) dismissed');
    await qa.shot('04-round1');

    let placements = 0;
    let lastRound = 0;
    for (let guard = 0; guard < 400 && placements < rounds * 30; guard++) {
      s = await qa.idle();
      const g = s.game;
      if (!g) { note('The run vanished from the state'); break; }
      if (g.state === 'RunDefeat' || g.state === 'RunVictory') { console.log('Run ended: ' + g.state + ' in round ' + g.round); await qa.shot('99-end'); break; }
      if (g.state === 'AwaitingShop') {
        await qa.shot('shop-round' + g.round);
        for (const slot of (g.blister || []).concat(g.workshop || [])) { if (!slot.name) { note('A shop slot has no name'); } }
        if (g.round >= rounds) { console.log('Reached round ' + g.round + ' shop, enough'); break; }
        await qa.gameCommand('grantPollen', 60);
        const leave = (await qa.buttons()).find((b) => /next round|leave|continue/i.test(b.text + ' ' + b.name));
        if (!leave) { note('No way out of the shop'); break; }
        await qa.click(leave.x, leave.y);
        await sleep(1500);
        continue;
      }
      if (g.round !== lastRound) { lastRound = g.round; console.log('Round ' + g.round + ' (' + g.challenge + ')'); }
      const move = g.moves[Math.floor(Math.random() * g.moves.length)];
      if (!move) {
        const shuffle = (await qa.buttons()).find((b) => /shuffle/i.test(b.text + ' ' + b.name));
        if (!shuffle) { note('No legal move and no shuffle button'); break; }
        await qa.click(shuffle.x, shuffle.y);
        continue;
      }
      const slotBtn = await qa.buttonNamed('Slot' + move.slot);
      if (!slotBtn) { await sleep(300); continue; }
      await qa.click(slotBtn.x, slotBtn.y);
      await sleep(120);
      const cell = g.cells[move.y * g.grid.length + move.x];
      await qa.click(cell.x, cell.y);
      placements++;
      await sleep(200);
      if (placements % 10 === 0) { await qa.shot('placement-' + placements); }
    }
    console.log('Placements played: ' + placements);
  } catch (e) {
    note('Scenario stopped: ' + e.message);
    try { await qa.shot('failure'); } catch (_) {}
  }
  if (qa.errors.length) { note(qa.errors.length + ' console error(s):\n  ' + qa.errors.slice(0, 10).join('\n  ')); }
  await qa.close();
  console.log(problems.length ? '\n' + problems.length + ' problem(s).' : '\nSmoke test passed.');
  process.exit(problems.length ? 1 : 0);
})();
