using Contigu.Core;
using NUnit.Framework;
using UnityEngine;

namespace Contigu.Tests
{
    /// <summary>
    /// Covers the enemy/combat vertical slice (spec extension, explicit
    /// request: "Je veux faire un shift du jeu pour ne pas simplement être
    /// un simple puzzle mais ajouter un petit peu d'autobattling" — see GDD
    /// §07/EncounterCatalog/EnemyInstance). Scoped to exactly what this
    /// first pass built: Basic/Locker/Poisoner across Classic's rounds 1-4.
    /// Sibling of RunManagerTests rather than a part of it so the two stay
    /// easy to tell apart; duplicates a few of its small private helpers
    /// (FindAnyValidAnchor/FirstOccupiedHandSlot/AdvanceToRound) since
    /// those are private to that class.
    /// </summary>
    public class EnemyEncounterTests
    {
        [Test]
        public void EncounterCatalog_OnlyClassicsFirstFourRounds_HaveAnEncounter()
        {
            Assert.AreEqual(1, EncounterCatalog.GetEncounter(ChallengeId.Classic, 0).Count);
            Assert.AreEqual(EnemyId.Basic, EncounterCatalog.GetEncounter(ChallengeId.Classic, 0)[0]);
            Assert.AreEqual(1, EncounterCatalog.GetEncounter(ChallengeId.Classic, 1).Count);
            Assert.AreEqual(EnemyId.Locker, EncounterCatalog.GetEncounter(ChallengeId.Classic, 1)[0]);
            Assert.AreEqual(1, EncounterCatalog.GetEncounter(ChallengeId.Classic, 2).Count);
            Assert.AreEqual(EnemyId.Poisoner, EncounterCatalog.GetEncounter(ChallengeId.Classic, 2)[0]);

            var round4 = EncounterCatalog.GetEncounter(ChallengeId.Classic, 3);
            Assert.AreEqual(2, round4.Count);
            Assert.AreEqual(EnemyId.Locker, round4[0]);
            Assert.AreEqual(EnemyId.Poisoner, round4[1]);

            // Rounds 5-8, Endless, and every non-Classic challenge are
            // deliberately left on the old quota system for now (explicit
            // scope: "Vertical slice: Basic + Locker + Poisoner").
            Assert.AreEqual(0, EncounterCatalog.GetEncounter(ChallengeId.Classic, 4).Count);
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
            Assert.AreEqual(2, run.CurrentEncounter.Count);
            Assert.AreEqual(EnemyId.Locker, run.CurrentEncounter[0].Definition.Id);
            Assert.AreEqual(EnemyId.Poisoner, run.CurrentEncounter[1].Definition.Id);
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
        public void AllEnemiesDefeated_EndsTheRound_WellBeforeTheOldQuotaWouldHaveBeenReached()
        {
            var run = new RunManager(new SystemRandomProvider(42));
            // Every cell golden so score accumulates fast — same proven
            // setup as RunManagerTests' own quota tests, just used here to
            // show the round now ends on the ENCOUNTER, not the quota: round
            // 1's Basic (150 HP) dies in 1-2 placements, well under the old
            // 300-point quota.
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
            Assert.Less(run.RoundScore, run.CurrentQuota, "Should have ended via Basic dying, not via reaching the old 300-point quota");
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
            AdvanceToRound(run, 1); // round 2 (index 1): Locker alone
            var locker = run.CurrentEncounter[0];
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
            AdvanceToRound(run, 1); // round 2 (index 1): Locker alone
            var locker = run.CurrentEncounter[0];

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
            AdvanceToRound(run, 1); // round 2 (index 1): Locker alone
            var locker = run.CurrentEncounter[0];

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
        public void Poisoner_PoisonsOneFilledCellPerShuffle_AndNormalizesThemAllOnDeath()
        {
            var run = new RunManager(new SystemRandomProvider(1));
            AdvanceToRound(run, 2); // round 3 (index 2): Poisoner alone
            var poisoner = run.CurrentEncounter[0];
            Assert.AreEqual(EnemyId.Poisoner, poisoner.Definition.Id);

            // Poisoner only ever targets an already-FILLED cell (GDD §07) —
            // manually fill exactly one so the very first Shuffle has a
            // single, unambiguous candidate regardless of RNG state.
            var target = new Vector2Int(0, 0);
            run.Grid.GetCell(target).IsFilled = true;
            run.Grid.GetCell(target).FilledColor = PieceColor.Coral;

            Assert.IsTrue(run.ShuffleHand());

            Assert.IsTrue(run.Grid.GetCell(target).IsPoisoned);
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
            AdvanceToRound(run, 2); // round 3 (index 2): Poisoner alone

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
            run.CurrentEncounter[0].AddPoisonedCell(poisonedPos);

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
        public void Poisoner_PoisonRoamsOneCellAtATime_NeverAccumulatingAcrossShuffles()
        {
            // Explicit request: "Les poison tiles doivent être retiré lors
            // d'un shuffle pour mieux être replacé aléatoirement, comme pour
            // les locked cell" — Poisoner's poison now roams exactly like
            // Locker's lock (see Locker_LocksOneCellPerShuffle... above)
            // instead of accumulating one more cell every Shuffle.
            var run = new RunManager(new SystemRandomProvider(1));
            AdvanceToRound(run, 2); // round 3 (index 2): Poisoner alone
            var poisoner = run.CurrentEncounter[0];

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
            AdvanceToRound(run, 2); // round 3 (index 2): Poisoner alone
            var target = new Vector2Int(0, 0);
            run.Grid.GetCell(target).IsFilled = true;
            run.Grid.GetCell(target).FilledColor = PieceColor.Coral;

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
            AdvanceToRound(run, 2); // round 3 (index 2): Poisoner alone
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
            run.CurrentEncounter[0].AddPoisonedCell(poisonedPos);

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
