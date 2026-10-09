using Contigu.Core;
using NUnit.Framework;
using UnityEngine;

namespace Contigu.Tests
{
    /// <summary>
    /// Covers the enemy/combat system (spec extension, explicit request:
    /// "Je veux faire un shift du jeu pour ne pas simplement être un simple
    /// puzzle mais ajouter un petit peu d'autobattling" — see GDD §07/
    /// EncounterCatalog/EnemyInstance), now the full 8-enemy roster across
    /// Classic's 8 rounds (follow-up explicit request: "Ajoutons de
    /// nouveaux ennemies et boss"). Sibling of RunManagerTests rather than
    /// a part of it so the two stay easy to tell apart; duplicates a few of
    /// its small private helpers (FindAnyValidAnchor/FirstOccupiedHandSlot/
    /// AdvanceToRound/ChurnUntilHandMatches) since those are private to
    /// that class.
    /// </summary>
    public class EnemyEncounterTests
    {
        [Test]
        public void EncounterCatalog_AllFifteenClassicRounds_HaveAnEncounter()
        {
            Assert.AreEqual(1, EncounterCatalog.GetEncounter(ChallengeId.Classic, 0).Count);
            Assert.AreEqual(EnemyId.Basic, EncounterCatalog.GetEncounter(ChallengeId.Classic, 0)[0]);

            // Basic now LEADS every round from here on (redesign, explicit
            // request: "des niveaux avec de plus en plus d'ennemis... Basic
            // enemi en début de file pour laisser le temps aux autre
            // d'instaurer des malus") instead of being appended last —
            // it's the front-targeted (first-damaged) enemy, buying the
            // enemy(ies) behind it a few Shuffles before the player can
            // touch them.
            var round2 = EncounterCatalog.GetEncounter(ChallengeId.Classic, 1);
            Assert.AreEqual(2, round2.Count);
            Assert.AreEqual(EnemyId.Basic, round2[0]);
            Assert.AreEqual(EnemyId.Locker, round2[1]);

            var round3 = EncounterCatalog.GetEncounter(ChallengeId.Classic, 2);
            Assert.AreEqual(2, round3.Count);
            Assert.AreEqual(EnemyId.Basic, round3[0]);
            Assert.AreEqual(EnemyId.Poisoner, round3[1]);

            var round4 = EncounterCatalog.GetEncounter(ChallengeId.Classic, 3);
            Assert.AreEqual(3, round4.Count);
            Assert.AreEqual(EnemyId.Basic, round4[0]);
            Assert.AreEqual(EnemyId.Locker, round4[1]);
            Assert.AreEqual(EnemyId.Poisoner, round4[2]);

            // Poisoner must stay right before Reclaimer in both Round5 and
            // Round6 (explicit bug report: "la round avec le reclaimer, il
            // doit se trouver après l'empoisonneur") — the front-targeting/
            // grow tension only works that way (see EnemyInstance.HealOrGrow).
            var round5 = EncounterCatalog.GetEncounter(ChallengeId.Classic, 4);
            Assert.AreEqual(3, round5.Count);
            Assert.AreEqual(EnemyId.Basic, round5[0]);
            Assert.AreEqual(EnemyId.Poisoner, round5[1]);
            Assert.AreEqual(EnemyId.Reclaimer, round5[2]);

            // Round6's 4th enemy is a SECOND Poisoner, not Locker (explicit
            // request: "round 6, remplace le locker par un autre poisoner
            // et met le avant le reclaimer") — both Poisoners still stay
            // ahead of Reclaimer.
            var round6 = EncounterCatalog.GetEncounter(ChallengeId.Classic, 5);
            Assert.AreEqual(4, round6.Count);
            Assert.AreEqual(EnemyId.Basic, round6[0]);
            Assert.AreEqual(EnemyId.Poisoner, round6[1]);
            Assert.AreEqual(EnemyId.Poisoner, round6[2]);
            Assert.AreEqual(EnemyId.Reclaimer, round6[3]);

            // Locker moved ahead of Thief/Leech (explicit request: "round
            // 7, met le locket devant le thief").
            var round7 = EncounterCatalog.GetEncounter(ChallengeId.Classic, 6);
            Assert.AreEqual(4, round7.Count);
            Assert.AreEqual(EnemyId.Basic, round7[0]);
            Assert.AreEqual(EnemyId.Locker, round7[1]);
            Assert.AreEqual(EnemyId.Thief, round7[2]);
            Assert.AreEqual(EnemyId.Leech, round7[3]);

            // Round 8 is the Classic run's finale — a full 5-enemy gauntlet
            // now (redesign, explicit report: "Le boss heavy locker je le
            // trouve pas extraordinaire") instead of a lone HeavyLocker.
            var round8 = EncounterCatalog.GetEncounter(ChallengeId.Classic, 7);
            Assert.AreEqual(5, round8.Count);
            Assert.AreEqual(EnemyId.Basic, round8[0]);
            Assert.AreEqual(EnemyId.HeavyLocker, round8[1]);
            Assert.AreEqual(EnemyId.Poisoner, round8[2]);
            Assert.AreEqual(EnemyId.Thief, round8[3]);
            Assert.AreEqual(EnemyId.Leech, round8[4]);

            // Act 2 (explicit request: "Est-ce que tu peux faire 15
            // rounds ... Je te fais confiance sur l'enchainement des
            // enemy encounters") — debuts Plague, then both new Haters,
            // never more than one ColorHater/ShapeHater in the same round.
            var round9 = EncounterCatalog.GetEncounter(ChallengeId.Classic, 8);
            Assert.AreEqual(3, round9.Count);
            Assert.AreEqual(EnemyId.Basic, round9[0]);
            Assert.AreEqual(EnemyId.Locker, round9[1]);
            Assert.AreEqual(EnemyId.Plague, round9[2]);

            var round10 = EncounterCatalog.GetEncounter(ChallengeId.Classic, 9);
            Assert.AreEqual(3, round10.Count);
            Assert.AreEqual(EnemyId.Basic, round10[0]);
            Assert.AreEqual(EnemyId.ColorHater, round10[1]);
            Assert.AreEqual(EnemyId.Thief, round10[2]);

            var round11 = EncounterCatalog.GetEncounter(ChallengeId.Classic, 10);
            Assert.AreEqual(3, round11.Count);
            Assert.AreEqual(EnemyId.Basic, round11[0]);
            Assert.AreEqual(EnemyId.ShapeHater, round11[1]);
            Assert.AreEqual(EnemyId.Leech, round11[2]);

            var round12 = EncounterCatalog.GetEncounter(ChallengeId.Classic, 11);
            Assert.AreEqual(4, round12.Count);
            Assert.AreEqual(EnemyId.Basic, round12[0]);
            Assert.AreEqual(EnemyId.Poisoner, round12[1]);
            Assert.AreEqual(EnemyId.Plague, round12[2]);
            Assert.AreEqual(EnemyId.Reclaimer, round12[3]);

            var round13 = EncounterCatalog.GetEncounter(ChallengeId.Classic, 12);
            Assert.AreEqual(4, round13.Count);
            Assert.AreEqual(EnemyId.Basic, round13[0]);
            Assert.AreEqual(EnemyId.ColorHater, round13[1]);
            Assert.AreEqual(EnemyId.ShapeHater, round13[2]);
            Assert.AreEqual(EnemyId.Thief, round13[3]);

            var round14 = EncounterCatalog.GetEncounter(ChallengeId.Classic, 13);
            Assert.AreEqual(5, round14.Count);
            Assert.AreEqual(EnemyId.Basic, round14[0]);
            Assert.AreEqual(EnemyId.HeavyLocker, round14[1]);
            Assert.AreEqual(EnemyId.Poisoner, round14[2]);
            Assert.AreEqual(EnemyId.Plague, round14[3]);
            Assert.AreEqual(EnemyId.Leech, round14[4]);

            // Round 15 is the true finale — the biggest roster of the
            // whole run.
            var round15 = EncounterCatalog.GetEncounter(ChallengeId.Classic, 14);
            Assert.AreEqual(6, round15.Count);
            Assert.AreEqual(EnemyId.Basic, round15[0]);
            Assert.AreEqual(EnemyId.HeavyLocker, round15[1]);
            Assert.AreEqual(EnemyId.ColorHater, round15[2]);
            Assert.AreEqual(EnemyId.ShapeHater, round15[3]);
            Assert.AreEqual(EnemyId.Thief, round15[4]);
            Assert.AreEqual(EnemyId.Leech, round15[5]);

            // Round 16+ (there is no round 16 in Classic's 15-round run)
            // and every non-Classic challenge are deliberately left on the
            // old quota system for now.
            Assert.AreEqual(0, EncounterCatalog.GetEncounter(ChallengeId.Classic, 15).Count);
            Assert.AreEqual(0, EncounterCatalog.GetEncounter(ChallengeId.Marathon, 0).Count);
            Assert.AreEqual(0, EncounterCatalog.GetEncounter(ChallengeId.Chaos, 0).Count);
        }

        [Test]
        public void StartRound_Round1_BuildsTheBasicEncounter_AndSuppressesTheOldBossRoll()
        {
            var run = new RunManager(new SystemRandomProvider(1));

            Assert.IsTrue(run.HasActiveEncounter);
            Assert.AreEqual(1, run.CurrentEncounter.Count);
            Assert.AreEqual(EnemyId.Basic, run.CurrentEncounter[0].Definition.Id);
            Assert.AreEqual(run.CurrentEncounter[0].Definition.MaxHp, run.CurrentEncounter[0].CurrentHp);
            Assert.AreEqual(BossEffect.None, run.CurrentBossEffect);
        }

        [Test]
        public void StartRound_Round4_BuildsTheLockerAndPoisonerEncounter_EvenThoughItsTheOldModuloBossRound()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            AdvanceToRound(run, 3);

            Assert.IsTrue(run.IsBossRound, "Round 4 is still every-4th-round by the numbers");
            Assert.IsTrue(run.HasActiveEncounter);
            Assert.AreEqual(3, run.CurrentEncounter.Count);
            Assert.AreEqual(EnemyId.Basic, run.CurrentEncounter[0].Definition.Id);
            Assert.AreEqual(EnemyId.Locker, run.CurrentEncounter[1].Definition.Id);
            Assert.AreEqual(EnemyId.Poisoner, run.CurrentEncounter[2].Definition.Id);
            // The two systems never run at once (see RunManager.HasActiveEncounter).
            Assert.AreEqual(BossEffect.None, run.CurrentBossEffect);
            Assert.IsNull(run.BossLockedHandSlotIndex);
            Assert.IsNull(run.BossCursedColor);
        }

        [Test]
        public void Marathon_NeverGetsAnEncounter_AndKeepsTheOldQuotaRules()
        {
            var run = new RunManager(new SystemRandomProvider(1), ChallengeCatalog.Marathon);

            Assert.IsFalse(run.HasActiveEncounter);
            Assert.AreEqual(0, run.CurrentEncounter.Count);
        }

        [Test]
        public void DebugForceRoundComplete_OnAnEncounterRound_KillsEveryEnemy_AndStillSetsRoundScoreToQuota()
        {
            var run = new RunManager(new SystemRandomProvider(1));

            var state = run.DebugForceRoundComplete();

            Assert.AreEqual(RunState.AwaitingShop, state);
            Assert.IsTrue(run.CurrentEncounter[0].IsDead);
            // Kept unconditional (see DebugForceRoundComplete's own doc
            // comment) so every pre-existing caller/test reading RoundScore
            // back still sees the same value as before this feature existed.
            Assert.AreEqual(run.CurrentQuota, run.RoundScore);
        }

        [Test]
        public void AllEnemiesDefeated_EndsTheRound_WithoutExhaustingThePieceBudget()
        {
            var run = new RunManager(new SystemRandomProvider(42));
            // Every cell golden so score accumulates fast — same proven
            // setup as RunManagerTests' own quota tests, just used here to
            // show the round now ends on the ENCOUNTER, not the (now
            // largely vestigial for Classic, see RunConfig.Quotas) quota.
            // Doesn't compare RoundScore against CurrentQuota any more
            // (explicit request doubled every EnemyCatalog.MaxHp — "Augmente
            // de 100% les pH des ennemis" — which pushed round 1's Basic
            // past the legacy 300-point quota: killing it legitimately now
            // takes more score than that moot number, so the only
            // meaningful invariant left is that the encounter still ends
            // the round before the piece budget runs out).
            foreach (var pos in GridManager.AllPositions())
            {
                run.Grid.GetCell(pos).IsGolden = true;
            }

            int budget = run.CurrentBudget;
            int piecesPlaced = 0;
            while (run.State == RunState.InProgress)
            {
                int slot = FirstOccupiedHandSlot(run);
                var token = run.Deck.Hand[slot].Value;
                var rotation = run.Deck.HandRotations[slot];
                var shape = PieceShapeCatalog.GetRotated(token.Shape, rotation);
                var anchor = FindAnyValidAnchor(run.Grid, shape);
                Assert.IsTrue(anchor.HasValue);
                run.PlacePiece(slot, anchor.Value.x, anchor.Value.y);
                piecesPlaced++;
                Assert.Less(piecesPlaced, budget);
            }

            Assert.AreEqual(RunState.AwaitingShop, run.State);
            Assert.IsTrue(run.CurrentEncounter[0].IsDead);
            Assert.Less(piecesPlaced, budget, "Should have ended via Basic dying, not via exhausting the round's piece budget");
        }

        [Test]
        public void PlacePiece_DamagesTheFrontAliveEnemy_ByExactlyThisPlacementsTotalScore()
        {
            var run = new RunManager(new SystemRandomProvider(3));
            var enemy = run.CurrentEncounter[0];
            int hpBefore = enemy.CurrentHp;

            int slot = FirstOccupiedHandSlot(run);
            var token = run.Deck.Hand[slot].Value;
            var shape = PieceShapeCatalog.GetRotated(token.Shape, run.Deck.HandRotations[slot]);
            var anchor = FindAnyValidAnchor(run.Grid, shape);
            Assert.IsTrue(anchor.HasValue);

            var outcome = run.PlacePiece(slot, anchor.Value.x, anchor.Value.y);

            int expectedHp = Mathf.Clamp(hpBefore - outcome.Placement.TotalScore, 0, enemy.Definition.MaxHp);
            Assert.AreEqual(expectedHp, enemy.CurrentHp);
        }

        [Test]
        public void Locker_LocksOneCellPerShuffle_MovesItNextShuffle_AndReleasesItOnDeath()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            AdvanceToRound(run, 1); // round 2 (index 1): Basic, Locker
            var locker = run.CurrentEncounter[1];
            Assert.AreEqual(EnemyId.Locker, locker.Definition.Id);
            Assert.IsNull(locker.LockedCell);

            Assert.IsTrue(run.ShuffleHand());
            Assert.IsTrue(locker.LockedCell.HasValue);
            var firstLock = locker.LockedCell.Value;
            Assert.IsTrue(run.Grid.GetCell(firstLock).IsLocked);
            Assert.AreEqual(1, CountLockedCells(run.Grid));

            Assert.IsTrue(run.ShuffleHand());
            Assert.IsTrue(locker.LockedCell.HasValue);
            // The lock roams (never accumulates) — exactly one cell locked
            // at a time, whatever Locker is currently alive.
            Assert.AreEqual(1, CountLockedCells(run.Grid));

            run.DebugForceRoundComplete();
            Assert.IsTrue(locker.IsDead);
            Assert.IsFalse(locker.LockedCell.HasValue, "Dying should release the lock immediately (GDD §07: effects cancelled on death)");
            Assert.AreEqual(0, CountLockedCells(run.Grid));
        }

        [Test]
        public void Locker_LockedCellBlocksItsWholeRowFromClearing()
        {
            // Explicit request: "Tu ne devrais pas pouvoir clear une ligne
            // qui contient une locked cell" — unlike the old boss
            // ProgressiveCellLock and a Bastion cell (both deliberately
            // exempt, see Cell.IsLineClearObstacle), a row/column containing
            // Locker's own lock must never complete, even once every other
            // cell in it is filled.
            var run = new RunManager(new SystemRandomProvider(1));
            AdvanceToRound(run, 1); // round 2 (index 1): Basic, Locker
            var locker = run.CurrentEncounter[1];

            Assert.IsTrue(run.ShuffleHand());
            Assert.IsTrue(locker.LockedCell.HasValue);
            var lockedPos = locker.LockedCell.Value;
            Assert.IsTrue(run.Grid.GetCell(lockedPos).IsLineClearObstacle);

            for (int x = 0; x < GridManager.Size; x++)
            {
                if (x == lockedPos.x)
                {
                    continue;
                }
                var cell = run.Grid.GetCell(x, lockedPos.y);
                cell.IsFilled = true;
                cell.FilledColor = PieceColor.Coral;
            }

            // A real placement elsewhere still runs GridManager's normal
            // clear-check, which must leave the locked row untouched.
            int otherY = (lockedPos.y + 1) % GridManager.Size;
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            var result = run.Grid.PlacePiece(single, PieceColor.Teal, 0, otherY);
            Assert.IsTrue(result.Success);

            for (int x = 0; x < GridManager.Size; x++)
            {
                if (x == lockedPos.x)
                {
                    continue;
                }
                Assert.IsTrue(run.Grid.GetCell(x, lockedPos.y).IsFilled,
                    "A row containing Locker's locked cell should never clear");
            }
        }

        [Test]
        public void Locker_LockedCellAlsoBlocksTheLineClearPreviewHighlight()
        {
            // Follow-up explicit report, after confirming the real clear is
            // correctly blocked: "En fait je ne peux pas line break mais
            // j'ai toujours le highlight de line clear avant de deposer une
            // pièce" — GridManager.PreviewClearedLineCells (the hover
            // highlight) had the exact same "skip every locked cell" bug as
            // the real clear check, via its own IsRowCompleteWithFootprint/
            // IsColumnCompleteWithFootprint.
            var run = new RunManager(new SystemRandomProvider(1));
            AdvanceToRound(run, 1); // round 2 (index 1): Basic, Locker
            var locker = run.CurrentEncounter[1];

            Assert.IsTrue(run.ShuffleHand());
            Assert.IsTrue(locker.LockedCell.HasValue);
            var lockedPos = locker.LockedCell.Value;

            for (int x = 0; x < GridManager.Size; x++)
            {
                if (x == lockedPos.x)
                {
                    continue;
                }
                var cell = run.Grid.GetCell(x, lockedPos.y);
                cell.IsFilled = true;
                cell.FilledColor = PieceColor.Coral;
            }

            // Preview a placement elsewhere, nowhere near the locked row.
            int otherY = (lockedPos.y + 1) % GridManager.Size;
            var single = PieceShapeCatalog.Get(ShapeId.Single);
            Assert.IsTrue(run.Grid.CanPlace(single, 0, otherY));
            var preview = run.Grid.PreviewClearedLineCells(single, 0, otherY);

            for (int x = 0; x < GridManager.Size; x++)
            {
                Assert.IsFalse(preview.Contains(new Vector2Int(x, lockedPos.y)),
                    "The row containing Locker's locked cell should never be previewed as clearable");
            }
        }

        [Test]
        public void Poisoner_CanPoisonAnEmptyCell_NotJustAFilledOne_AndNormalizesOnDeath()
        {
            // Redesign, explicit request: "Toutes les tuiles peuvent être
            // empoisonné, pas juste les tuiles rempli" — unlike the old
            // filled-only targeting, an EMPTY cell is now just as valid a
            // candidate. Pre-poison every other cell so this Shuffle's only
            // remaining candidate is the one deliberately left EMPTY,
            // regardless of RNG state.
            var run = new RunManager(new SystemRandomProvider(1));
            AdvanceToRound(run, 2); // round 3 (index 2): Basic, Poisoner
            var poisoner = run.CurrentEncounter[1];
            Assert.AreEqual(EnemyId.Poisoner, poisoner.Definition.Id);

            var target = new Vector2Int(0, 0);
            PoisonEveryCellExcept(run.Grid, target);

            Assert.IsTrue(run.ShuffleHand());

            Assert.IsFalse(run.Grid.GetCell(target).IsFilled, "Target should still be empty");
            Assert.IsTrue(run.Grid.GetCell(target).IsPoisoned, "An empty cell should now be a valid poison target");
            Assert.AreEqual(1, poisoner.PoisonedCells.Count);
            Assert.AreEqual(target, poisoner.PoisonedCells[0]);

            run.DebugForceRoundComplete();
            Assert.IsTrue(poisoner.IsDead);
            Assert.IsFalse(run.Grid.GetCell(target).IsPoisoned, "Dying should normalize every tile this instance poisoned (GDD §07)");
            Assert.AreEqual(0, poisoner.PoisonedCells.Count);
        }

        [Test]
        public void Poisoner_RescoringAGroupThroughItsPoisonedCell_FlipsThatCellsOwnScoreEventNegative()
        {
            var run = new RunManager(new SystemRandomProvider(5));
            AdvanceToRound(run, 2); // round 3 (index 2): Basic, Poisoner

            int slotA = FirstOccupiedHandSlot(run);
            var tokenA = run.Deck.Hand[slotA].Value;
            var shapeA = PieceShapeCatalog.GetRotated(tokenA.Shape, run.Deck.HandRotations[slotA]);
            var anchorA = FindAnyValidAnchor(run.Grid, shapeA);
            Assert.IsTrue(anchorA.HasValue);
            var outcomeA = run.PlacePiece(slotA, anchorA.Value.x, anchorA.Value.y);
            Assert.IsTrue(outcomeA.Placement.Success);

            // Manually poison one of the cells THIS placement itself just
            // filled, bypassing the real Shuffle-targeting RNG — deterministic
            // setup for the scoring-rule check below (the Shuffle-targeting
            // mechanism itself is covered separately).
            var poisonedPos = outcomeA.Placement.PlacedCells[0];
            run.Grid.GetCell(poisonedPos).IsPoisoned = true;
            run.CurrentEncounter[1].AddPoisonedCell(poisonedPos);

            int slotB = -1;
            for (int i = 0; i < DeckManager.HandSize; i++)
            {
                if (i != slotA && run.Deck.Hand[i].HasValue)
                {
                    slotB = i;
                    break;
                }
            }
            Assert.GreaterOrEqual(slotB, 0, "Hand should still have another piece to play this round");
            // Force the same color as piece A so the two merge into ONE
            // connected group once placed touching each other.
            run.Deck.RecolorHandToken(slotB, tokenA.Color);
            var tokenB = run.Deck.Hand[slotB].Value;
            var shapeB = PieceShapeCatalog.GetRotated(tokenB.Shape, run.Deck.HandRotations[slotB]);
            var anchorB = FindAnchorTouchingCell(run.Grid, shapeB, poisonedPos);
            Assert.IsTrue(anchorB.HasValue, "Should find room to grow the group right next to the poisoned cell");

            var outcomeB = run.PlacePiece(slotB, anchorB.Value.x, anchorB.Value.y);
            Assert.IsTrue(outcomeB.Placement.Success);

            bool foundNegatedEvent = false;
            for (int i = 0; i < outcomeB.Placement.ScoreEvents.Count; i++)
            {
                var evt = outcomeB.Placement.ScoreEvents[i];
                if (evt.Position == poisonedPos && evt.Amount < 0)
                {
                    foundNegatedEvent = true;
                }
            }
            Assert.IsTrue(foundNegatedEvent, "The poisoned cell's own re-scored contribution should have flipped negative");
        }

        [Test]
        public void PoisonedTile_WhenTriggeredByScoring_ContaminatesOneOrthogonallyAdjacentCell()
        {
            // Redesign, explicit request: "Si une tuile empoisonnée est
            // triggered, une de ses 4 tuile adjacente est contaminée."
            var run = new RunManager(new SystemRandomProvider(5));
            AdvanceToRound(run, 2); // round 3 (index 2): Basic, Poisoner
            var poisoner = run.CurrentEncounter[1];

            int slotA = FirstOccupiedHandSlot(run);
            var tokenA = run.Deck.Hand[slotA].Value;
            var shapeA = PieceShapeCatalog.GetRotated(tokenA.Shape, run.Deck.HandRotations[slotA]);
            var anchorA = FindAnyValidAnchor(run.Grid, shapeA);
            Assert.IsTrue(anchorA.HasValue);
            var outcomeA = run.PlacePiece(slotA, anchorA.Value.x, anchorA.Value.y);
            Assert.IsTrue(outcomeA.Placement.Success);

            var poisonedPos = outcomeA.Placement.PlacedCells[0];
            run.Grid.GetCell(poisonedPos).IsPoisoned = true;
            poisoner.AddPoisonedCell(poisonedPos);

            int slotB = -1;
            for (int i = 0; i < DeckManager.HandSize; i++)
            {
                if (i != slotA && run.Deck.Hand[i].HasValue)
                {
                    slotB = i;
                    break;
                }
            }
            Assert.GreaterOrEqual(slotB, 0, "Hand should still have another piece to play this round");
            run.Deck.RecolorHandToken(slotB, tokenA.Color);
            var tokenB = run.Deck.Hand[slotB].Value;
            var shapeB = PieceShapeCatalog.GetRotated(tokenB.Shape, run.Deck.HandRotations[slotB]);
            var anchorB = FindAnchorTouchingCell(run.Grid, shapeB, poisonedPos);
            Assert.IsTrue(anchorB.HasValue, "Should find room to grow the group right next to the poisoned cell");

            var outcomeB = run.PlacePiece(slotB, anchorB.Value.x, anchorB.Value.y);
            Assert.IsTrue(outcomeB.Placement.Success);

            Assert.AreEqual(2, CountPoisonedCells(run.Grid), "Triggering the poisoned tile should contaminate exactly 1 extra cell, on top of the original");
            Assert.AreEqual(2, poisoner.PoisonedCells.Count, "The contaminated cell should be owned by the same Poisoner instance");

            bool foundAdjacentContamination = false;
            foreach (var pos in GridManager.AllPositions())
            {
                if (pos != poisonedPos && run.Grid.GetCell(pos).IsPoisoned)
                {
                    int manhattan = Mathf.Abs(pos.x - poisonedPos.x) + Mathf.Abs(pos.y - poisonedPos.y);
                    Assert.AreEqual(1, manhattan, "Contamination should land on an orthogonally adjacent cell");
                    foundAdjacentContamination = true;
                }
            }
            Assert.IsTrue(foundAdjacentContamination);
        }

        [Test]
        public void Contamination_ExtraPoisonedCellsAreAlsoReleased_OnThePoisonersNextShuffle()
        {
            // Follow-up explicit request: "Toutes les tuiles supplémentaires
            // sont aussi effacé on shuffle" — the existing roam-release-all
            // logic in ResolvePoisonerShuffleEffect already covers this for
            // free, since contamination adds to the SAME instance's own
            // PoisonedCells list.
            var run = new RunManager(new SystemRandomProvider(5));
            AdvanceToRound(run, 2); // round 3 (index 2): Basic, Poisoner
            var poisoner = run.CurrentEncounter[1];

            int slotA = FirstOccupiedHandSlot(run);
            var tokenA = run.Deck.Hand[slotA].Value;
            var shapeA = PieceShapeCatalog.GetRotated(tokenA.Shape, run.Deck.HandRotations[slotA]);
            var anchorA = FindAnyValidAnchor(run.Grid, shapeA);
            Assert.IsTrue(anchorA.HasValue);
            var outcomeA = run.PlacePiece(slotA, anchorA.Value.x, anchorA.Value.y);
            Assert.IsTrue(outcomeA.Placement.Success);

            var poisonedPos = outcomeA.Placement.PlacedCells[0];
            run.Grid.GetCell(poisonedPos).IsPoisoned = true;
            poisoner.AddPoisonedCell(poisonedPos);

            int slotB = -1;
            for (int i = 0; i < DeckManager.HandSize; i++)
            {
                if (i != slotA && run.Deck.Hand[i].HasValue)
                {
                    slotB = i;
                    break;
                }
            }
            Assert.GreaterOrEqual(slotB, 0, "Hand should still have another piece to play this round");
            run.Deck.RecolorHandToken(slotB, tokenA.Color);
            var tokenB = run.Deck.Hand[slotB].Value;
            var shapeB = PieceShapeCatalog.GetRotated(tokenB.Shape, run.Deck.HandRotations[slotB]);
            var anchorB = FindAnchorTouchingCell(run.Grid, shapeB, poisonedPos);
            Assert.IsTrue(anchorB.HasValue);

            run.PlacePiece(slotB, anchorB.Value.x, anchorB.Value.y);
            Assert.AreEqual(2, CountPoisonedCells(run.Grid), "Should have 1 original + 1 contaminated cell before the next Shuffle");

            Assert.IsTrue(run.ShuffleHand());

            Assert.AreEqual(1, poisoner.PoisonedCells.Count, "The Shuffle should release every tracked cell (original AND contaminated) before picking exactly 1 new one");
            Assert.AreEqual(1, CountPoisonedCells(run.Grid));
        }

        [Test]
        public void Poisoner_PoisonRoamsOneCellAtATime_NeverAccumulatingAcrossShuffles()
        {
            // Explicit request: "Les poison tiles doivent être retiré lors
            // d'un shuffle pour mieux être replacé aléatoirement, comme pour
            // les locked cell" — Poisoner's poison now roams exactly like
            // Locker's lock (see Locker_LocksOneCellPerShuffle... above)
            // instead of accumulating one more cell every Shuffle.
            var run = new RunManager(new SystemRandomProvider(1));
            AdvanceToRound(run, 2); // round 3 (index 2): Basic, Poisoner
            var poisoner = run.CurrentEncounter[1];

            for (int x = 0; x < GridManager.Size; x++)
            {
                for (int y = 0; y < GridManager.Size; y++)
                {
                    var cell = run.Grid.GetCell(x, y);
                    cell.IsFilled = true;
                    cell.FilledColor = PieceColor.Coral;
                }
            }

            Assert.IsTrue(run.ShuffleHand());
            Assert.AreEqual(1, poisoner.PoisonedCells.Count);
            Assert.AreEqual(1, CountPoisonedCells(run.Grid));

            Assert.IsTrue(run.ShuffleHand());
            Assert.AreEqual(1, poisoner.PoisonedCells.Count);
            Assert.AreEqual(1, CountPoisonedCells(run.Grid));
        }

        [Test]
        public void Poisoner_PoisonSurvivesALineClear_UntilTheNextShuffleRemovesIt()
        {
            // Explicit request: "Lorsqu'une poison tile est cleared, elle
            // doit rester présente sur la grille. Pas la tuile, seulement
            // l'effet poison jusqu'au prochain shuffle."
            var run = new RunManager(new SystemRandomProvider(1));
            AdvanceToRound(run, 2); // round 3 (index 2): Basic, Poisoner
            var target = new Vector2Int(0, 0);
            run.Grid.GetCell(target).IsFilled = true;
            run.Grid.GetCell(target).FilledColor = PieceColor.Coral;
            // Any cell is now a valid poison candidate (see
            // Poisoner_CanPoisonAnEmptyCell...), so pre-poison every OTHER
            // cell to keep this Shuffle's target deterministic.
            PoisonEveryCellExcept(run.Grid, target);

            Assert.IsTrue(run.ShuffleHand());
            Assert.IsTrue(run.Grid.GetCell(target).IsPoisoned);

            run.Grid.GetCell(target).ClearFill();

            Assert.IsFalse(run.Grid.GetCell(target).IsFilled);
            Assert.IsTrue(run.Grid.GetCell(target).IsPoisoned,
                "Poison should stay attached to the grid position until the next Shuffle, even once the tile itself is cleared/emptied");
        }

        [Test]
        public void Contraste_FlipsNegative_WhenItsContrastingNeighborIsPoisoned()
        {
            // Explicit request: "Valider pour les poison tiles, si un
            // modifier utilise cette case là spécifiquement c'est négatif
            // aussi. Exemple pour le modifier contrast, si la tuile
            // adjacente d'une autre couleur est négative."
            var run = new RunManager(new SystemRandomProvider(5));
            AdvanceToRound(run, 2); // round 3 (index 2): Basic, Poisoner
            run.DebugGrantModifier(ModifierId.Contraste);

            int slotA = FirstOccupiedHandSlot(run);
            run.Deck.RecolorHandToken(slotA, PieceColor.Coral);
            var tokenA = run.Deck.Hand[slotA].Value;
            var shapeA = PieceShapeCatalog.GetRotated(tokenA.Shape, run.Deck.HandRotations[slotA]);
            var anchorA = FindAnyValidAnchor(run.Grid, shapeA);
            Assert.IsTrue(anchorA.HasValue);
            var outcomeA = run.PlacePiece(slotA, anchorA.Value.x, anchorA.Value.y);
            Assert.IsTrue(outcomeA.Placement.Success);

            // Manually poison one of the cells THIS placement itself just
            // filled, bypassing the real Shuffle-targeting RNG — same
            // deterministic bypass as Poisoner_RescoringAGroupThroughItsPoisonedCell...
            var poisonedPos = outcomeA.Placement.PlacedCells[0];
            run.Grid.GetCell(poisonedPos).IsPoisoned = true;
            run.CurrentEncounter[1].AddPoisonedCell(poisonedPos);

            int slotB = -1;
            for (int i = 0; i < DeckManager.HandSize; i++)
            {
                if (i != slotA && run.Deck.Hand[i].HasValue)
                {
                    slotB = i;
                    break;
                }
            }
            Assert.GreaterOrEqual(slotB, 0, "Hand should still have another piece to play this round");
            // A DIFFERENT color than the poisoned cell so Contraste actually
            // triggers against it.
            run.Deck.RecolorHandToken(slotB, PieceColor.Teal);
            var tokenB = run.Deck.Hand[slotB].Value;
            var shapeB = PieceShapeCatalog.GetRotated(tokenB.Shape, run.Deck.HandRotations[slotB]);
            var anchorB = FindAnchorTouchingCell(run.Grid, shapeB, poisonedPos);
            Assert.IsTrue(anchorB.HasValue, "Should find room to place a different-colored piece next to the poisoned cell");

            var outcomeB = run.PlacePiece(slotB, anchorB.Value.x, anchorB.Value.y);
            Assert.IsTrue(outcomeB.Placement.Success);

            bool foundNegatedContraste = false;
            for (int i = 0; i < outcomeB.Placement.ScoreEvents.Count; i++)
            {
                var evt = outcomeB.Placement.ScoreEvents[i];
                if (evt.Type == ScoreEventType.Modifier && evt.ReferencedPosition == poisonedPos && evt.Amount < 0)
                {
                    foundNegatedContraste = true;
                }
            }
            Assert.IsTrue(foundNegatedContraste, "Contraste's own bonus should flip negative when the contrasting neighbor it read is poisoned");
        }

        [Test]
        public void EnemyInstance_ApplyDamage_NegativeDamageHealsItBackUp_ClampedAtMaxHp()
        {
            var enemy = new EnemyInstance(EnemyCatalog.Basic);

            bool diedFromHit = enemy.ApplyDamage(100);
            Assert.IsFalse(diedFromHit);
            Assert.AreEqual(enemy.Definition.MaxHp - 100, enemy.CurrentHp);

            bool diedFromHeal = enemy.ApplyDamage(-500);
            Assert.IsFalse(diedFromHeal);
            Assert.AreEqual(enemy.Definition.MaxHp, enemy.CurrentHp, "Healing should clamp at MaxHp, never overflow past it");
        }

        [Test]
        public void EnemyInstance_ApplyDamage_ReturnsTrueOnlyOnTheHitThatActuallyKillsIt()
        {
            var enemy = new EnemyInstance(EnemyCatalog.Basic);

            Assert.IsFalse(enemy.ApplyDamage(enemy.Definition.MaxHp - 1));
            Assert.IsTrue(enemy.ApplyDamage(1), "The exact lethal hit should report true");
            Assert.IsTrue(enemy.IsDead);
            Assert.IsFalse(enemy.ApplyDamage(50), "Hitting an already-dead enemy again should report false, not re-trigger a kill");
        }

        [Test]
        public void EnemyInstance_HealOrGrow_WhileAtFullHealth_GrowsMaxAndCurrentHp()
        {
            // Explicit request: "j'aimerais ajouter pour le reclaimer que
            // s'il est heal ET qu'il est full health, il augmente son max
            // health et son health pour devenir plus fort."
            var enemy = new EnemyInstance(EnemyCatalog.Reclaimer);
            int maxBefore = enemy.CurrentMaxHp;
            Assert.AreEqual(maxBefore, enemy.CurrentHp, "Should start at full HP");

            enemy.HealOrGrow(50);

            Assert.AreEqual(maxBefore + 50, enemy.CurrentMaxHp, "Already-full heal should grow the ceiling");
            Assert.AreEqual(maxBefore + 50, enemy.CurrentHp, "...and heal up to that new, grown ceiling");
        }

        [Test]
        public void EnemyInstance_HealOrGrow_WhileNotFull_ClampsNormally_NoGrowth()
        {
            var enemy = new EnemyInstance(EnemyCatalog.Reclaimer);
            int maxBefore = enemy.CurrentMaxHp;
            enemy.ApplyDamage(50);

            enemy.HealOrGrow(1000);

            Assert.AreEqual(maxBefore, enemy.CurrentMaxHp, "Wasn't full before this heal, so no growth");
            Assert.AreEqual(maxBefore, enemy.CurrentHp, "Just clamps at the ordinary (un-grown) max, same as Heal");
        }

        [Test]
        public void HeavyLocker_LocksOneCellPerShuffle_SameMechanicAsLocker_JustAtBossHp()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            AdvanceToRound(run, 7); // round 8 (index 7): Basic, Heavy Locker, Poisoner, Thief, Leech
            var heavyLocker = run.CurrentEncounter[1];
            Assert.AreEqual(EnemyId.HeavyLocker, heavyLocker.Definition.Id);
            Assert.AreEqual(EnemyCatalog.HeavyLocker.MaxHp, heavyLocker.CurrentHp);
            Assert.IsNull(heavyLocker.LockedCell);

            Assert.IsTrue(run.ShuffleHand());
            Assert.IsTrue(heavyLocker.LockedCell.HasValue);
            Assert.AreEqual(1, CountLockedCells(run.Grid));

            Assert.IsTrue(run.ShuffleHand());
            Assert.AreEqual(1, CountLockedCells(run.Grid), "The lock should roam, never accumulate");

            run.DebugForceRoundComplete();
            Assert.IsTrue(heavyLocker.IsDead);
            Assert.AreEqual(0, CountLockedCells(run.Grid), "Killing it should release the current lock immediately (GDD §07)");
        }

        [Test]
        public void Plague_PoisonsFiveFilledCellsPerShuffle_RoamsAsABlock_AndNormalizesThemAllOnDeath()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.DebugSetEncounter(EnemyId.Plague);
            var plague = run.CurrentEncounter[0];

            for (int x = 0; x < GridManager.Size; x++)
            {
                for (int y = 0; y < GridManager.Size; y++)
                {
                    var cell = run.Grid.GetCell(x, y);
                    cell.IsFilled = true;
                    cell.FilledColor = PieceColor.Coral;
                }
            }

            Assert.IsTrue(run.ShuffleHand());
            Assert.AreEqual(5, plague.PoisonedCells.Count);
            Assert.AreEqual(5, CountPoisonedCells(run.Grid));

            Assert.IsTrue(run.ShuffleHand());
            Assert.AreEqual(5, plague.PoisonedCells.Count, "Plague's 5 poisoned cells should roam as a block, never accumulating past 5");
            Assert.AreEqual(5, CountPoisonedCells(run.Grid));

            run.DebugForceRoundComplete();
            Assert.IsTrue(plague.IsDead);
            Assert.AreEqual(0, CountPoisonedCells(run.Grid), "Killing it should normalize every tile it poisoned (GDD §07)");
        }

        [Test]
        public void Thief_StealsOneRandomHandTileOnShuffle_WithoutShrinkingTheDeck()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            AdvanceToRound(run, 6); // round 7 (index 6): Basic, Locker, Thief, Leech
            int deckSizeBefore = run.Deck.DeckCount;

            Assert.IsTrue(run.ShuffleHand());

            int occupiedAfter = 0;
            for (int i = 0; i < DeckManager.HandSize; i++)
            {
                if (run.Deck.Hand[i].HasValue)
                {
                    occupiedAfter++;
                }
            }
            Assert.AreEqual(DeckManager.HandSize - 1, occupiedAfter, "Thief should empty exactly 1 of the 3 freshly-dealt hand slots");
            Assert.AreEqual(deckSizeBefore, run.Deck.DeckCount, "The stolen piece should go to the bottom of the draw pile, not disappear — deck composition never shrinks");
        }

        [Test]
        public void ShuffleHand_SetsThiefStoleOnLastShuffle_OnlyWhenAnAliveThiefActuallySteals()
        {
            // Explicit request: "Thief manque un effet visuel pour
            // indiquer qu'il vole une pièce" — this flag is what
            // Presentation reads right after ShuffleHand/PlacePiece to
            // know whether to show that effect at all (see
            // GameBootstrap.PlayThiefStealEffect).
            var withThief = new RunManager(new SystemRandomProvider(1));
            AdvanceToRound(withThief, 6); // round 7 (index 6): Basic, Locker, Thief, Leech
            Assert.IsTrue(withThief.ShuffleHand());
            Assert.IsTrue(withThief.ThiefStoleOnLastShuffle);

            var withoutThief = new RunManager(new SystemRandomProvider(1));
            // Round 1 (index 0): Basic alone — no Thief in this encounter.
            Assert.IsTrue(withoutThief.ShuffleHand());
            Assert.IsFalse(withoutThief.ThiefStoleOnLastShuffle);
        }

        [Test]
        public void Reclaimer_HealsByTheExactMagnitude_OfAnyNegativePoisonScoreEvent()
        {
            // Redesign, explicit request: "Chaque points négatifs triggered
            // par une tuile empoisonné, l'ennemi reclaimer récupère en
            // point de vie ce montant là" — Reclaimer no longer has an
            // On-Shuffle effect at all; it heals reactively, in lockstep
            // with ApplyPoisonScoreRule flipping a placement's own score
            // events negative. Basic goes first in encounter order so
            // ApplyDamageToEncounter's own front-enemy hit lands on IT, not
            // Reclaimer — keeping Reclaimer's HP change attributable to
            // nothing but the new poison heal.
            var run = new RunManager(new SystemRandomProvider(5));
            run.DebugSetEncounter(EnemyId.Basic, EnemyId.Reclaimer);
            var reclaimer = run.CurrentEncounter[1];
            reclaimer.ApplyDamage(500);
            int hpBefore = reclaimer.CurrentHp;

            // Same deterministic "poison one of this placement's own just-
            // filled cells, then grow the group through it" setup as
            // Poisoner_RescoringAGroupThroughItsPoisonedCell....
            int slotA = FirstOccupiedHandSlot(run);
            var tokenA = run.Deck.Hand[slotA].Value;
            var shapeA = PieceShapeCatalog.GetRotated(tokenA.Shape, run.Deck.HandRotations[slotA]);
            var anchorA = FindAnyValidAnchor(run.Grid, shapeA);
            Assert.IsTrue(anchorA.HasValue);
            var outcomeA = run.PlacePiece(slotA, anchorA.Value.x, anchorA.Value.y);
            Assert.IsTrue(outcomeA.Placement.Success);

            var poisonedPos = outcomeA.Placement.PlacedCells[0];
            run.Grid.GetCell(poisonedPos).IsPoisoned = true;
            // Ownership can be anyone's — GetPoisonedPositionsSnapshot reads
            // every enemy's own PoisonedCells list, not Cell.IsPoisoned
            // directly, so this cell needs SOME owner to be seen as poisoned
            // for scoring purposes at all (see RunManager.PlacePiece).
            reclaimer.AddPoisonedCell(poisonedPos);

            int slotB = -1;
            for (int i = 0; i < DeckManager.HandSize; i++)
            {
                if (i != slotA && run.Deck.Hand[i].HasValue)
                {
                    slotB = i;
                    break;
                }
            }
            Assert.GreaterOrEqual(slotB, 0, "Hand should still have another piece to play this round");
            run.Deck.RecolorHandToken(slotB, tokenA.Color);
            var tokenB = run.Deck.Hand[slotB].Value;
            var shapeB = PieceShapeCatalog.GetRotated(tokenB.Shape, run.Deck.HandRotations[slotB]);
            var anchorB = FindAnchorTouchingCell(run.Grid, shapeB, poisonedPos);
            Assert.IsTrue(anchorB.HasValue, "Should find room to grow the group right next to the poisoned cell");

            var outcomeB = run.PlacePiece(slotB, anchorB.Value.x, anchorB.Value.y);
            Assert.IsTrue(outcomeB.Placement.Success);

            int expectedMagnitude = 0;
            for (int i = 0; i < outcomeB.Placement.ScoreEvents.Count; i++)
            {
                var evt = outcomeB.Placement.ScoreEvents[i];
                if (evt.Amount < 0)
                {
                    expectedMagnitude += -evt.Amount;
                }
            }
            Assert.Greater(expectedMagnitude, 0, "This placement should have scored at least one negative event through the poisoned cell");
            Assert.AreEqual(hpBefore + expectedMagnitude, reclaimer.CurrentHp);
        }

        [Test]
        public void Reclaimer_HasNoOnShuffleEffectOfItsOwn_AnyMore()
        {
            // The old "consumes poisoned tiles on Shuffle" mechanic is gone
            // entirely (redesign) — a Shuffle by itself, with poison
            // present elsewhere on the board, should never change
            // Reclaimer's HP; only an actual scoring event through poison
            // does (see Reclaimer_HealsByTheExactMagnitude...).
            var run = new RunManager(new SystemRandomProvider(1));
            run.DebugSetEncounter(EnemyId.Poisoner, EnemyId.Reclaimer);
            var reclaimer = run.CurrentEncounter[1];
            reclaimer.ApplyDamage(50);
            int hpBefore = reclaimer.CurrentHp;

            run.Grid.GetCell(0, 0).IsFilled = true;
            run.Grid.GetCell(0, 0).FilledColor = PieceColor.Coral;

            Assert.IsTrue(run.ShuffleHand());

            Assert.AreEqual(hpBefore, reclaimer.CurrentHp, "A Shuffle alone should never heal Reclaimer any more");
        }

        [Test]
        public void Leech_HealsOnLineClear_ViaRunManagerPlacePiece()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            AdvanceToRound(run, 6); // round 7 (index 6): Basic, Locker, Thief, Leech
            var leech = run.CurrentEncounter[3];
            Assert.AreEqual(EnemyId.Leech, leech.Definition.Id);
            // More than LeechHealPerLineClear, so the heal below lands
            // strictly below CurrentMaxHp — a heal that OVERFLOWS past the
            // cap while not already AT full just clamps with no growth
            // (see EnemyInstance.HealOrGrow's own documented contract,
            // covered by EnemyInstance_HealOrGrow_WhileNotFull_
            // ClampsNormally_NoGrowth); this test is about the plain
            // per-line heal amount, not that edge case (see
            // Leech_HealingWhileAlreadyFull_GrowsItsMaxHp for growth).
            leech.ApplyDamage(ScoringConstants.LeechHealPerLineClear + 50);
            int hpBefore = leech.CurrentHp;

            int slot = ChurnUntilHandMatches(run, t => t.Shape == ShapeId.Single);
            var anchor = FindRowCompletingAnchor(run.Grid);
            Assert.IsTrue(anchor.HasValue, "Should find a row with exactly 1 cell free for the Single piece to complete");

            var outcome = run.PlacePiece(slot, anchor.Value.x, anchor.Value.y);
            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(1, outcome.Placement.ClearedLineCount);
            Assert.AreEqual(hpBefore + ScoringConstants.LeechHealPerLineClear, leech.CurrentHp,
                "Leech should heal exactly LeechHealPerLineClear HP for the 1 line this placement cleared");
        }

        [Test]
        public void Leech_HealingWhileAlreadyFull_GrowsItsMaxHp()
        {
            // Explicit request: "Pour le boss leech ... il devient de plus
            // en plus fort s'il est déjà full, son max HP augmente aussi" —
            // Reclaimer's own grow-on-overflow-heal mechanic (see
            // EnemyInstance.HealOrGrow), now shared by Leech.
            var run = new RunManager(new SystemRandomProvider(1));
            AdvanceToRound(run, 6); // round 7 (index 6): Basic, Locker, Thief, Leech
            var leech = run.CurrentEncounter[3];
            Assert.AreEqual(EnemyId.Leech, leech.Definition.Id);
            int maxBefore = leech.CurrentMaxHp;
            Assert.AreEqual(maxBefore, leech.CurrentHp, "Should start at full HP");

            int slot = ChurnUntilHandMatches(run, t => t.Shape == ShapeId.Single);
            var anchor = FindRowCompletingAnchor(run.Grid);
            Assert.IsTrue(anchor.HasValue, "Should find a row with exactly 1 cell free for the Single piece to complete");

            var outcome = run.PlacePiece(slot, anchor.Value.x, anchor.Value.y);
            Assert.IsTrue(outcome.Placement.Success);
            Assert.AreEqual(1, outcome.Placement.ClearedLineCount);
            Assert.AreEqual(maxBefore + ScoringConstants.LeechHealPerLineClear, leech.CurrentMaxHp,
                "Already-full Leech should grow its max HP from the line-clear heal, same as Reclaimer");
            Assert.AreEqual(maxBefore + ScoringConstants.LeechHealPerLineClear, leech.CurrentHp);
        }

        [Test]
        public void Leech_StopsHealing_OnceDead()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            AdvanceToRound(run, 6); // round 7 (index 6): Basic, Locker, Thief, Leech
            var leech = run.CurrentEncounter[3];
            leech.ApplyDamage(leech.Definition.MaxHp); // kill it
            Assert.IsTrue(leech.IsDead);

            int slot = ChurnUntilHandMatches(run, t => t.Shape == ShapeId.Single);
            var anchor = FindRowCompletingAnchor(run.Grid);
            Assert.IsTrue(anchor.HasValue);

            run.PlacePiece(slot, anchor.Value.x, anchor.Value.y);

            Assert.AreEqual(0, leech.CurrentHp, "A dead Leech should never heal back up, even when a line clears");
        }

        [Test]
        public void ColorHater_CancelsAllPointsForItsHatedColor_ButNotOtherColors()
        {
            // Explicit request: "J'aimerais rajouter un boss: Color hater.
            // Tous les points effectué par une certaine couleur sont
            // annulé" — same mechanism as the old quota system's
            // "Cursed Color" boss effect (see RunManagerTests.
            // RunManager_CursedColorCanBePlayedButItsPlacementScoresZero),
            // just keyed off a per-instance EnemyInstance.HatedColor
            // instead of RunManager.BossCursedColor.
            var run = new RunManager(new SystemRandomProvider(1));
            run.DebugSetEncounter(EnemyId.ColorHater);
            var hater = run.CurrentEncounter[0];

            int handIndex = FirstOccupiedHandSlot(run);
            hater.HatedColor = run.Deck.Hand[handIndex].Value.Color;

            var outcome = run.PlacePiece(handIndex, 0, 0);
            Assert.IsTrue(outcome.Placement.Success, "The hated color remains playable");
            Assert.AreEqual(0, outcome.Placement.TotalScore, "Every point scored through the hated color should be cancelled");
        }

        [Test]
        public void ShapeHater_CancelsAllPointsForItsHatedShape_ButNotOtherShapes()
        {
            // Explicit request: "Idem pour les shapes, il faut un Shape
            // hater" — ColorHater's exact mirror, keyed by Cell.
            // FilledShapeId/EnemyInstance.HatedShape instead of color.
            var run = new RunManager(new SystemRandomProvider(1));
            run.DebugSetEncounter(EnemyId.ShapeHater);
            var hater = run.CurrentEncounter[0];
            hater.HatedShape = ShapeId.Single;

            int slot = ChurnUntilHandMatches(run, t => t.Shape == ShapeId.Single);
            var outcome = run.PlacePiece(slot, 0, 0);
            Assert.IsTrue(outcome.Placement.Success, "The hated shape remains playable");
            Assert.AreEqual(0, outcome.Placement.TotalScore, "Every point scored through a tile originally placed by the hated shape should be cancelled");

            // A DIFFERENT shape placed well away from the first (no shared
            // group) should score normally — proves the cancellation is
            // keyed to the hated shape specifically, not just "any lone
            // placement scores 0".
            int otherSlot = ChurnUntilHandMatches(run, t => t.Shape != ShapeId.Single);
            var otherShape = PieceShapeCatalog.GetRotated(run.Deck.Hand[otherSlot].Value.Shape, run.Deck.HandRotations[otherSlot]);
            var anchor = FindAnyValidAnchor(run.Grid, otherShape);
            Assert.IsTrue(anchor.HasValue);
            var otherOutcome = run.PlacePiece(otherSlot, anchor.Value.x, anchor.Value.y);
            Assert.IsTrue(otherOutcome.Placement.Success);
            Assert.Greater(otherOutcome.Placement.TotalScore, 0, "A non-hated shape's points should score normally");
        }

        [Test]
        public void Bombe_SplitsDamageEquallyAcrossEveryAliveEnemy()
        {
            // Explicit request: "bombe: divize équitablement les dégâts
            // sur tous les ennemies présent".
            var run = new RunManager(new SystemRandomProvider(1));
            run.DebugSetEncounter(EnemyId.Basic, EnemyId.Basic, EnemyId.Basic);
            var e0 = run.CurrentEncounter[0];
            var e1 = run.CurrentEncounter[1];
            var e2 = run.CurrentEncounter[2];
            e1.ApplyDamage(e1.CurrentMaxHp); // kill the middle one — only e0/e2 should share the split
            Assert.IsTrue(e1.IsDead);
            int hp0Before = e0.CurrentHp;
            int hp2Before = e2.CurrentHp;

            // A 2-cell shape (not Single) guarantees a nonzero per-enemy
            // share once split in two (progressive group scoring: a lone
            // Single would only ever score 1 point, which floor-divides to
            // a silent, untestable 0 share).
            run.Deck.AddPreparedToken(new PieceToken(ShapeId.DomH, PieceColor.Joker, new PieceTrait(PieceTraitKind.Bombe, 0)));
            int slot = ChurnUntilHandMatches(run, t => t.Color == PieceColor.Joker && t.Trait.HasValue && t.Trait.Value.Kind == PieceTraitKind.Bombe);
            var outcome = run.PlacePiece(slot, 0, 0);
            Assert.IsTrue(outcome.Placement.Success);

            int totalScore = outcome.Placement.TotalScore;
            int expectedShare = totalScore / 2;
            Assert.Greater(expectedShare, 0, "Need a large enough score for the split to actually be observable");
            Assert.AreEqual(hp0Before - expectedShare, e0.CurrentHp);
            Assert.AreEqual(hp2Before - expectedShare, e2.CurrentHp);
            Assert.IsTrue(e1.IsDead, "Bombe should never revive an already-dead enemy");
        }

        [Test]
        public void Range_DamagesTheLastAliveEnemyInsteadOfTheFront()
        {
            // Explicit request: "Range: attaque le dernier ennemi en liste".
            var run = new RunManager(new SystemRandomProvider(1));
            run.DebugSetEncounter(EnemyId.Basic, EnemyId.Basic);
            var front = run.CurrentEncounter[0];
            var back = run.CurrentEncounter[1];
            int frontHpBefore = front.CurrentHp;
            int backHpBefore = back.CurrentHp;

            run.Deck.AddPreparedToken(new PieceToken(ShapeId.Single, PieceColor.Joker, new PieceTrait(PieceTraitKind.Range, 0)));
            int slot = ChurnUntilHandMatches(run, t => t.Color == PieceColor.Joker && t.Trait.HasValue && t.Trait.Value.Kind == PieceTraitKind.Range);
            var outcome = run.PlacePiece(slot, 0, 0);
            Assert.IsTrue(outcome.Placement.Success);

            Assert.AreEqual(frontHpBefore, front.CurrentHp, "Range should never touch the front enemy");
            Assert.AreEqual(backHpBefore - outcome.Placement.TotalScore, back.CurrentHp);
        }

        [Test]
        public void Precision_AlwaysDamagesTheWeakestAliveEnemy_RegardlessOfOrder()
        {
            // Explicit request: find 2 more combat-upgrade ideas — Précision
            // always finishes off whichever alive enemy has the least HP.
            var run = new RunManager(new SystemRandomProvider(1));
            run.DebugSetEncounter(EnemyId.Basic, EnemyId.Basic, EnemyId.Basic);
            var e0 = run.CurrentEncounter[0];
            var e1 = run.CurrentEncounter[1];
            var e2 = run.CurrentEncounter[2];
            // e2 (LAST in order) is made the weakest, to prove targeting
            // ignores front-to-back order entirely.
            e0.ApplyDamage(50);
            e1.ApplyDamage(20);
            e2.ApplyDamage(150);
            int e0HpBefore = e0.CurrentHp;
            int e1HpBefore = e1.CurrentHp;
            int e2HpBefore = e2.CurrentHp;

            run.Deck.AddPreparedToken(new PieceToken(ShapeId.Single, PieceColor.Joker, new PieceTrait(PieceTraitKind.Precision, 0)));
            int slot = ChurnUntilHandMatches(run, t => t.Color == PieceColor.Joker && t.Trait.HasValue && t.Trait.Value.Kind == PieceTraitKind.Precision);
            var outcome = run.PlacePiece(slot, 0, 0);
            Assert.IsTrue(outcome.Placement.Success);

            Assert.AreEqual(e2HpBefore - outcome.Placement.TotalScore, e2.CurrentHp, "Précision should hit the weakest alive enemy (e2), not the front one");
            Assert.AreEqual(e0HpBefore, e0.CurrentHp);
            Assert.AreEqual(e1HpBefore, e1.CurrentHp);
        }

        [Test]
        public void Eclat_OverkillCascadesToTheNextAliveEnemy()
        {
            // Explicit request: find 2 more combat-upgrade ideas — Éclat
            // pierces through a lethal hit into whatever's behind it.
            var run = new RunManager(new SystemRandomProvider(1));
            run.DebugSetEncounter(EnemyId.Basic, EnemyId.Basic);
            var front = run.CurrentEncounter[0];
            var next = run.CurrentEncounter[1];
            front.ApplyDamage(front.CurrentMaxHp - 1); // leaves exactly 1 HP
            Assert.AreEqual(1, front.CurrentHp);
            int nextHpBefore = next.CurrentHp;

            // A 2-cell shape (not Single) guarantees this placement's own
            // group scores at least 1+2=3 points (progressive group
            // scoring), comfortably more than front's 1 remaining HP, so
            // the cascade actually has overkill to carry forward.
            run.Deck.AddPreparedToken(new PieceToken(ShapeId.DomH, PieceColor.Joker, new PieceTrait(PieceTraitKind.Eclat, 0)));
            int slot = ChurnUntilHandMatches(run, t => t.Color == PieceColor.Joker && t.Trait.HasValue && t.Trait.Value.Kind == PieceTraitKind.Eclat);
            var outcome = run.PlacePiece(slot, 0, 0);
            Assert.IsTrue(outcome.Placement.Success);

            int totalScore = outcome.Placement.TotalScore;
            Assert.Greater(totalScore, 1, "Need more damage than front's 1 remaining HP for this test to actually produce overkill");
            Assert.IsTrue(front.IsDead);
            Assert.AreEqual(nextHpBefore - (totalScore - 1), next.CurrentHp, "The overkill (totalScore minus front's 1 HP) should cascade onto the next alive enemy");
        }

        [Test]
        public void Sangsue_DamagesTheFrontEnemy_AndConvertsAFractionOfDamageIntoLueur()
        {
            // Explicit request: find 2 more combat-upgrade ideas — Sangsue
            // ties combat back into the Lueur economy.
            var run = new RunManager(new SystemRandomProvider(1));
            run.DebugSetEncounter(EnemyId.Basic);
            var front = run.CurrentEncounter[0];
            int frontHpBefore = front.CurrentHp;
            int lueurBefore = run.Lueur;

            // A 4-cell shape scores 1+2+3+4=10 (progressive group scoring)
            // — large enough that 25% of it floors to a clearly nonzero,
            // testable Lueur gain, unlike a lone Single's 1-point score.
            run.Deck.AddPreparedToken(new PieceToken(ShapeId.Sq2, PieceColor.Joker, new PieceTrait(PieceTraitKind.Sangsue, 0)));
            int slot = ChurnUntilHandMatches(run, t => t.Color == PieceColor.Joker && t.Trait.HasValue && t.Trait.Value.Kind == PieceTraitKind.Sangsue);
            var outcome = run.PlacePiece(slot, 0, 0);
            Assert.IsTrue(outcome.Placement.Success);

            int totalScore = outcome.Placement.TotalScore;
            Assert.AreEqual(frontHpBefore - totalScore, front.CurrentHp);
            int expectedLueurGain = Mathf.FloorToInt(totalScore * ScoringConstants.SangsueLueurFraction);
            Assert.Greater(expectedLueurGain, 0, "Need a large enough score for the Lueur siphon to actually be observable");
            Assert.AreEqual(lueurBefore + expectedLueurGain, run.Lueur);
        }

        [Test]
        public void JokerCombatTrait_RetriggersWhenALaterPlacementMergesIntoItsStampedGroup()
        {
            // Explicit request: "Pour les jokers, s'ils sont retrigger plus
            // tard dans une pièce jouée, son effet aussi est retrigger".
            var run = new RunManager(new SystemRandomProvider(1));
            run.DebugSetEncounter(EnemyId.Basic, EnemyId.Basic);
            var e0 = run.CurrentEncounter[0];
            var e1 = run.CurrentEncounter[1];

            // First placement: a lone Joker Bombe piece — fires once, same
            // as the existing Bombe test above.
            run.Deck.AddPreparedToken(new PieceToken(ShapeId.Single, PieceColor.Joker, new PieceTrait(PieceTraitKind.Bombe, 0)));
            int jokerSlot = ChurnUntilHandMatches(run, t => t.Color == PieceColor.Joker && t.Trait.HasValue && t.Trait.Value.Kind == PieceTraitKind.Bombe);
            var firstOutcome = run.PlacePiece(jokerSlot, 0, 0);
            Assert.IsTrue(firstOutcome.Placement.Success);
            int hp0AfterFirst = e0.CurrentHp;
            int hp1AfterFirst = e1.CurrentHp;

            // Second placement: an ORDINARY (non-Joker, untagged) piece
            // adjacent to the Joker cell — a Joker cell's wildcard color
            // always matches (see GridManager.TryVisitGroupNeighbor), so
            // this merges into the same group, rescoring it and — per this
            // fix — retriggering Bombe's already-stamped effect too.
            int ordinarySlot = ChurnUntilHandMatches(run, t => t.Shape == ShapeId.Single && t.Color != PieceColor.Joker && !t.Trait.HasValue);
            var secondOutcome = run.PlacePiece(ordinarySlot, 1, 0);
            Assert.IsTrue(secondOutcome.Placement.Success);
            Assert.AreEqual(2, secondOutcome.Placement.GroupCells.Count, "Should have merged with the Joker cell into one 2-cell group");

            int secondScore = secondOutcome.Placement.TotalScore;
            int expectedShare = secondScore / 2; // Bombe splits equally across the 2 alive enemies
            Assert.Greater(expectedShare, 0, "Need a nonzero split for the retrigger to actually be observable");
            Assert.AreEqual(hp0AfterFirst - expectedShare, e0.CurrentHp,
                "Bombe should retrigger and split this SECOND placement's damage too, now that merging pulled its already-stamped cell into the scored group");
            Assert.AreEqual(hp1AfterFirst - expectedShare, e1.CurrentHp);
        }

        [Test]
        public void RenfortJoker_Boosts25PercentDamage_OnlyOnJokerCombatTraitPlacements()
        {
            // Explicit request: synergy between Joker combat traits and
            // the rest of the shop ("je veux qu'on regarde plus de
            // synergies" -> "1, 2 et 4" -> "Je veux juste le 1 et le 2
            // finalement").
            var run = new RunManager(new SystemRandomProvider(1));
            run.DebugSetEncounter(EnemyId.Basic);
            run.DebugGrantModifier(ModifierId.RenfortJoker);
            var enemy = run.CurrentEncounter[0];
            int hpBefore = enemy.CurrentHp;

            run.Deck.AddPreparedToken(new PieceToken(ShapeId.DomH, PieceColor.Joker, new PieceTrait(PieceTraitKind.Range, 0)));
            int slot = ChurnUntilHandMatches(run, t => t.Color == PieceColor.Joker && t.Trait.HasValue && t.Trait.Value.Kind == PieceTraitKind.Range);
            var outcome = run.PlacePiece(slot, 0, 0);
            Assert.IsTrue(outcome.Placement.Success);

            int totalScore = outcome.Placement.TotalScore;
            int expectedDamage = Mathf.RoundToInt(totalScore * (1f + ScoringConstants.RenfortJokerDamageBonusPercent / 100f));
            Assert.Greater(expectedDamage, totalScore, "Need the boost to actually be observable");
            Assert.AreEqual(hpBefore - expectedDamage, enemy.CurrentHp);
        }

        [Test]
        public void RenfortJoker_DoesNotBoost_OrdinaryFrontHitDamage()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.DebugSetEncounter(EnemyId.Basic);
            run.DebugGrantModifier(ModifierId.RenfortJoker);
            var enemy = run.CurrentEncounter[0];
            int hpBefore = enemy.CurrentHp;

            int slot = ChurnUntilHandMatches(run, t => t.Color != PieceColor.Joker && !t.Trait.HasValue);
            var token = run.Deck.Hand[slot].Value;
            var shape = PieceShapeCatalog.GetRotated(token.Shape, run.Deck.HandRotations[slot]);
            var anchor = FindAnyValidAnchor(run.Grid, shape);
            Assert.IsTrue(anchor.HasValue);
            var outcome = run.PlacePiece(slot, anchor.Value.x, anchor.Value.y);
            Assert.IsTrue(outcome.Placement.Success);

            Assert.AreEqual(hpBefore - outcome.Placement.TotalScore, enemy.CurrentHp,
                "RenfortJoker should never boost an ordinary (non-Joker-combat) placement's damage");
        }

        [Test]
        public void Siphon_ConvertsAFractionOfAnyCombatKindsDamageIntoLueur_NotJustSangsue()
        {
            // Explicit request: generalize Sangsue's siphon to every combat
            // kind ("un modifier qui convertit une partie des PV retirés à
            // un ennemi en Lueur bonus pour TOUT type de combat (pas juste
            // Sangsue)").
            var run = new RunManager(new SystemRandomProvider(1));
            run.DebugSetEncounter(EnemyId.Basic);
            run.DebugGrantModifier(ModifierId.Siphon);
            int lueurBefore = run.Lueur;

            run.Deck.AddPreparedToken(new PieceToken(ShapeId.Sq2, PieceColor.Joker, new PieceTrait(PieceTraitKind.Range, 0)));
            int slot = ChurnUntilHandMatches(run, t => t.Color == PieceColor.Joker && t.Trait.HasValue && t.Trait.Value.Kind == PieceTraitKind.Range);
            var outcome = run.PlacePiece(slot, 0, 0);
            Assert.IsTrue(outcome.Placement.Success);

            int totalScore = outcome.Placement.TotalScore;
            int expectedLueurGain = Mathf.FloorToInt(totalScore * ScoringConstants.SiphonLueurFraction);
            Assert.Greater(expectedLueurGain, 0, "Need a large enough score for the siphon to actually be observable");
            Assert.AreEqual(lueurBefore + expectedLueurGain, run.Lueur);
        }

        [Test]
        public void Siphon_StacksWithSangsue_WhenBothAreHeldOnASangsuePlacement()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            run.DebugSetEncounter(EnemyId.Basic);
            run.DebugGrantModifier(ModifierId.Siphon);
            int lueurBefore = run.Lueur;

            run.Deck.AddPreparedToken(new PieceToken(ShapeId.Sq2, PieceColor.Joker, new PieceTrait(PieceTraitKind.Sangsue, 0)));
            int slot = ChurnUntilHandMatches(run, t => t.Color == PieceColor.Joker && t.Trait.HasValue && t.Trait.Value.Kind == PieceTraitKind.Sangsue);
            var outcome = run.PlacePiece(slot, 0, 0);
            Assert.IsTrue(outcome.Placement.Success);

            int totalScore = outcome.Placement.TotalScore;
            int expectedFromSangsue = Mathf.FloorToInt(totalScore * ScoringConstants.SangsueLueurFraction);
            int expectedFromSiphon = Mathf.FloorToInt(totalScore * ScoringConstants.SiphonLueurFraction);
            Assert.AreEqual(lueurBefore + expectedFromSangsue + expectedFromSiphon, run.Lueur,
                "Both siphons should stack independently on a Sangsue placement");
        }

        /// <summary>Fills every cell of some free row but its last, then returns the anchor where a Single-shaped piece would complete it — null if the grid has no free row to set up this way (shouldn't happen on a fresh board).</summary>
        private static Vector2Int? FindRowCompletingAnchor(GridManager grid)
        {
            for (int y = 0; y < GridManager.Size; y++)
            {
                bool rowAlreadyTouched = false;
                for (int x = 0; x < GridManager.Size; x++)
                {
                    if (grid.GetCell(x, y).IsFilled)
                    {
                        rowAlreadyTouched = true;
                        break;
                    }
                }
                if (rowAlreadyTouched)
                {
                    continue;
                }
                for (int x = 0; x < GridManager.Size - 1; x++)
                {
                    grid.GetCell(x, y).IsFilled = true;
                    grid.GetCell(x, y).FilledColor = PieceColor.Coral;
                }
                return new Vector2Int(GridManager.Size - 1, y);
            }
            return null;
        }

        private static int ChurnUntilHandMatches(RunManager run, System.Func<PieceToken, bool> predicate)
        {
            int guard = 0;
            int nextSlotToPlay = 0;
            while (true)
            {
                for (int i = 0; i < DeckManager.HandSize; i++)
                {
                    var slot = run.Deck.Hand[i];
                    if (slot.HasValue && predicate(slot.Value))
                    {
                        return i;
                    }
                }
                run.Deck.PlayFromHand(nextSlotToPlay);
                nextSlotToPlay = (nextSlotToPlay + 1) % DeckManager.HandSize;
                guard++;
                Assert.Less(guard, 300, "A matching token should reach the hand well within a few reshuffle cycles");
            }
        }

        // ---- helpers (duplicated from RunManagerTests, which keeps its own copies private) ----

        private static int CountLockedCells(GridManager grid)
        {
            int count = 0;
            foreach (var pos in GridManager.AllPositions())
            {
                if (grid.GetCell(pos).IsLocked)
                {
                    count++;
                }
            }
            return count;
        }

        private static int CountPoisonedCells(GridManager grid)
        {
            int count = 0;
            foreach (var pos in GridManager.AllPositions())
            {
                if (grid.GetCell(pos).IsPoisoned)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>Poisons every cell on the grid except <paramref name="except"/> — used to keep a Poisoner Shuffle's RNG-picked target deterministic now that any cell (filled or empty) is a valid candidate, by leaving exactly one.</summary>
        private static void PoisonEveryCellExcept(GridManager grid, Vector2Int except)
        {
            foreach (var pos in GridManager.AllPositions())
            {
                if (pos != except)
                {
                    grid.GetCell(pos).IsPoisoned = true;
                }
            }
        }

        private static Vector2Int? FindAnyValidAnchor(GridManager grid, PieceShape shape)
        {
            for (int y = 0; y < GridManager.Size; y++)
            {
                for (int x = 0; x < GridManager.Size; x++)
                {
                    if (grid.CanPlace(shape, x, y))
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }
            return null;
        }

        /// <summary>Like <see cref="FindAnyValidAnchor"/>, but only returns an anchor whose placement would include at least one cell orthogonally adjacent to <paramref name="target"/> — used to guarantee a same-color placement actually merges into the group containing it.</summary>
        private static Vector2Int? FindAnchorTouchingCell(GridManager grid, PieceShape shape, Vector2Int target)
        {
            for (int y = 0; y < GridManager.Size; y++)
            {
                for (int x = 0; x < GridManager.Size; x++)
                {
                    if (!grid.CanPlace(shape, x, y))
                    {
                        continue;
                    }
                    for (int i = 0; i < shape.Cells.Count; i++)
                    {
                        var cell = new Vector2Int(x, y) + shape.Cells[i];
                        int manhattan = Mathf.Abs(cell.x - target.x) + Mathf.Abs(cell.y - target.y);
                        if (manhattan == 1)
                        {
                            return new Vector2Int(x, y);
                        }
                    }
                }
            }
            return null;
        }

        private static int FirstOccupiedHandSlot(RunManager run)
        {
            for (int i = 0; i < DeckManager.HandSize; i++)
            {
                if (run.Deck.Hand[i].HasValue)
                {
                    return i;
                }
            }
            Assert.Fail("Hand should have at least one occupied slot while the round is in progress");
            return -1;
        }

        /// <summary>Drives a fresh run straight to the start of <paramref name="targetRoundIndex"/> via DebugForceRoundComplete, without needing to actually clear each round's real win condition.</summary>
        private static void AdvanceToRound(RunManager run, int targetRoundIndex)
        {
            while (run.CurrentRoundIndex < targetRoundIndex)
            {
                run.DebugForceRoundComplete();
                Assert.AreEqual(RunState.AwaitingShop, run.State);
                run.LeaveShop();
            }
        }
    }
}
