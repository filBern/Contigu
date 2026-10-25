using System.Collections.Generic;
using Contigu.Core;
using NUnit.Framework;
using UnityEngine;

namespace Contigu.Tests
{
    /// <summary>The 40-fertilizer batch: Hedgerow … Granary.</summary>
    public sealed class FortyFertilizerTests
    {
        private static PlacementResult Place(GridManager grid, ShapeId shape, PieceColor color, int x, int y, ModifierId mod, PieceRotation rot = PieceRotation.Deg0)
        {
            return grid.PlacePiece(PieceShapeCatalog.GetRotated(shape, rot), color, x, y, new List<ModifierId> { mod });
        }

        private static void Fill(GridManager grid, PieceColor color, params (int x, int y)[] cells)
        {
            foreach (var c in cells)
            {
                grid.PlacePiece(PieceShapeCatalog.Get(ShapeId.Single), color, c.x, c.y);
            }
        }

        private static RunManager NewRun(params ModifierId[] held)
        {
            var run = new RunManager(new SystemRandomProvider(5));
            run.DebugDisableWeather();
            foreach (var id in held)
            {
                Assert.IsTrue(run.DebugGrantModifier(id), id.ToString());
            }
            return run;
        }

        private static PlacementResult PlaceAny(RunManager run)
        {
            for (int slot = 0; slot < run.Deck.Hand.Count; slot++)
            {
                if (!run.Deck.Hand[slot].HasValue || run.IsHandSlotLocked(slot))
                {
                    continue;
                }
                var shape = PieceShapeCatalog.GetRotated(run.Deck.Hand[slot].Value.Shape, run.Deck.HandRotations[slot]);
                foreach (var pos in GridManager.AllPositions())
                {
                    if (run.Grid.CanPlace(shape, pos.x, pos.y))
                    {
                        var outcome = run.PlacePiece(slot, pos.x, pos.y);
                        Assert.IsTrue(outcome.Placement.Success);
                        return outcome.Placement;
                    }
                }
            }
            Assert.Fail("no placement found");
            return null;
        }

        [Test]
        public void CloneForSimulation_IsIndependent_AndScoresLikeTheOriginal()
        {
            var grid = new GridManager();
            Fill(grid, PieceColor.Coral, (0, 0), (1, 0), (2, 0), (4, 0));
            var copy = grid.CloneForSimulation();
            var mods = new List<ModifierId> { ModifierId.Chaine };
            var viaCopy = copy.PlacePiece(PieceShapeCatalog.Get(ShapeId.Single), PieceColor.Coral, 3, 0, mods);
            Assert.IsFalse(grid.GetCell(3, 0).IsFilled, "the real board is untouched");
            var real = grid.PlacePiece(PieceShapeCatalog.Get(ShapeId.Single), PieceColor.Coral, 3, 0, mods);
            Assert.AreEqual(real.TotalScore, viaCopy.TotalScore);
        }

        [Test]
        public void AllFortyAreInTheCatalog_WithAPriceAndAnAbbreviation()
        {
            var ids = new[]
            {
                ModifierId.Haie, ModifierId.Coeur, ModifierId.Treillis, ModifierId.Sillon, ModifierId.Haricot, ModifierId.Patchwork, ModifierId.Racines, ModifierId.Emboite,
                ModifierId.Mosaique, ModifierId.Foule, ModifierId.Prairie, ModifierId.Guirlande, ModifierId.Pollinisateur, ModifierId.Miel, ModifierId.Nectar, ModifierId.MoissonCroisee,
                ModifierId.Faucheur, ModifierId.TableRase, ModifierId.Dime, ModifierId.CultureRang, ModifierId.CultureColonne, ModifierId.Prime, ModifierId.BriseGlace, ModifierId.Saisons,
                ModifierId.Outsider, ModifierId.CoupFinal, ModifierId.DerniereGraine, ModifierId.PremiereGraine, ModifierId.Tirelire, ModifierId.BanqueGraines, ModifierId.SousBois,
                ModifierId.DanseDePluie, ModifierId.DanseDuSoleil, ModifierId.Rustique, ModifierId.CavalierVent, ModifierId.Matinal, ModifierId.Marathonien, ModifierId.Pourfendeur,
                ModifierId.Tonneau, ModifierId.Grange
            };
            Assert.AreEqual(40, ids.Length);
            foreach (var id in ids)
            {
                var def = ModifierCatalog.Get(id);
                Assert.IsNotNull(def, id.ToString());
                Assert.IsNotEmpty(def.Description, id.ToString());
                Assert.Greater(ModifierPricing.GetPrice(id), 0, id.ToString());
            }
        }

        [Test]
        public void Hedgerow_PaysPerEdgeTile()
        {
            var edge = Place(new GridManager(), ShapeId.Single, PieceColor.Coral, 0, 3, ModifierId.Haie);
            var middle = Place(new GridManager(), ShapeId.Single, PieceColor.Coral, 2, 2, ModifierId.Haie);
            Assert.AreEqual(middle.AdditiveMultBonus + ScoringConstants.PointsToMult(ScoringConstants.HedgerowBonusPerCell), edge.AdditiveMultBonus);
        }

        [Test]
        public void Heartwood_DoublesOnlyInTheMiddle()
        {
            Assert.AreEqual(ScoringConstants.HeartwoodMultiplier, Place(new GridManager(), ShapeId.Single, PieceColor.Coral, 2, 2, ModifierId.Coeur).ModifierMultiplier);
            Assert.AreEqual(ScoringConstants.HeartwoodMultiplier, Place(new GridManager(), ShapeId.Single, PieceColor.Coral, 3, 3, ModifierId.Coeur).ModifierMultiplier);
            Assert.AreEqual(1, Place(new GridManager(), ShapeId.Single, PieceColor.Coral, 0, 0, ModifierId.Coeur).ModifierMultiplier);
            Assert.AreEqual(1, Place(new GridManager(), ShapeId.Single, PieceColor.Coral, 1, 2, ModifierId.Coeur).ModifierMultiplier);
        }

        [Test]
        public void TrellisAndFurrow_ReadTheDirectionOfAStraightPiece()
        {
            var horizontal = ShapeId.DomH;
            var h = Place(new GridManager(), horizontal, PieceColor.Coral, 1, 1, ModifierId.Sillon);
            Assert.AreEqual(ScoringConstants.FurrowMultBonus, h.AdditiveMultBonus);
            Assert.AreEqual(0, Place(new GridManager(), horizontal, PieceColor.Coral, 1, 1, ModifierId.Treillis).AdditiveMultBonus);
            var v = Place(new GridManager(), horizontal, PieceColor.Coral, 1, 1, ModifierId.Treillis, PieceRotation.Deg90);
            Assert.AreEqual(ScoringConstants.TrellisMultBonus, v.AdditiveMultBonus);
            Assert.AreEqual(0, Place(new GridManager(), ShapeId.Single, PieceColor.Coral, 1, 1, ModifierId.Treillis).AdditiveMultBonus, "a single tile has no direction");
        }

        [Test]
        public void Beanstalk_NeedsAStraightPieceOfThree()
        {
            var straight = Place(new GridManager(), ShapeId.TriIH, PieceColor.Coral, 1, 1, ModifierId.Haricot);
            var plain = Place(new GridManager(), ShapeId.TriIH, PieceColor.Coral, 1, 1, ModifierId.Chaine);
            Assert.AreEqual(ScoringConstants.PointsToMult(ScoringConstants.BeanstalkBonus), straight.AdditiveMultBonus - plain.AdditiveMultBonus);
            Assert.AreEqual(0, Place(new GridManager(), ShapeId.TriL, PieceColor.Coral, 1, 1, ModifierId.Haricot).AdditiveMultBonus);
            Assert.AreEqual(0, Place(new GridManager(), ShapeId.DomH, PieceColor.Coral, 1, 1, ModifierId.Haricot).AdditiveMultBonus);
        }

        [Test]
        public void Patchwork_PaysPerDifferentColoredNeighbor()
        {
            var grid = new GridManager();
            Fill(grid, PieceColor.Teal, (2, 2), (4, 2));
            Fill(grid, PieceColor.Coral, (3, 3));
            var result = Place(grid, ShapeId.Single, PieceColor.Coral, 3, 2, ModifierId.Patchwork);
            Assert.AreEqual(ScoringConstants.PointsToMult(2 * ScoringConstants.PatchworkBonusPerTile), result.AdditiveMultBonus, "two Teal neighbors, the Coral one doesn't count");
        }

        [Test]
        public void Taproot_AddsOneMultPerThreeGroupCells()
        {
            var grid = new GridManager();
            Fill(grid, PieceColor.Coral, (1, 1), (2, 1), (4, 1));
            var result = Place(grid, ShapeId.Single, PieceColor.Coral, 3, 1, ModifierId.Racines);
            Assert.AreEqual(5 / ScoringConstants.TaprootCellsPerMult, result.AdditiveMultBonus);
        }

        [Test]
        public void SnugFit_NeedsEveryNeighborPlanted()
        {
            var grid = new GridManager();
            Fill(grid, PieceColor.Teal, (1, 2), (3, 2), (2, 1), (2, 3));
            Assert.AreEqual(ScoringConstants.SnugFitMultBonus, Place(grid, ShapeId.Single, PieceColor.Coral, 2, 2, ModifierId.Emboite).AdditiveMultBonus);
            var open = new GridManager();
            Fill(open, PieceColor.Teal, (1, 2), (3, 2), (2, 1));
            Assert.AreEqual(0, Place(open, ShapeId.Single, PieceColor.Coral, 2, 2, ModifierId.Emboite).AdditiveMultBonus);
            Assert.AreEqual(0, Place(new GridManager(), ShapeId.Single, PieceColor.Coral, 2, 2, ModifierId.Emboite).AdditiveMultBonus, "nothing around it");
        }

        [Test]
        public void Mosaic_WantsTwoNeighborsAndNoMatch()
        {
            var grid = new GridManager();
            Fill(grid, PieceColor.Teal, (1, 2));
            Fill(grid, PieceColor.Violet, (3, 2));
            Assert.AreEqual(ScoringConstants.MosaicMultBonus, Place(grid, ShapeId.Single, PieceColor.Coral, 2, 2, ModifierId.Mosaique).AdditiveMultBonus);
            var twin = new GridManager();
            Fill(twin, PieceColor.Teal, (1, 2));
            Fill(twin, PieceColor.Coral, (3, 2));
            Assert.AreEqual(0, Place(twin, ShapeId.Single, PieceColor.Coral, 2, 2, ModifierId.Mosaique).AdditiveMultBonus);
            var lone = new GridManager();
            Fill(lone, PieceColor.Teal, (1, 2));
            Assert.AreEqual(0, Place(lone, ShapeId.Single, PieceColor.Coral, 2, 2, ModifierId.Mosaique).AdditiveMultBonus);
        }

        [Test]
        public void Crowd_NeedsTwoThirdsOfTheGardenPlanted()
        {
            var cells = new List<Vector2Int>();
            foreach (var pos in GridManager.AllPositions())
            {
                if ((pos.x + pos.y) % 3 != 0)
                {
                    cells.Add(pos);
                }
            }
            Assert.AreEqual(GridManager.Size * GridManager.Size * 2 / 3, cells.Count, "a pattern of exactly two thirds that completes no line");
            var grid = new GridManager();
            var mods = new List<ModifierId> { ModifierId.Foule };
            for (int i = 0; i < cells.Count - 1; i++)
            {
                Assert.AreEqual(1, grid.PlacePiece(PieceShapeCatalog.Get(ShapeId.Single), (PieceColor)(i % 4), cells[i].x, cells[i].y, mods).ModifierMultiplier, "still sparse at tile " + i);
            }
            var last = cells[cells.Count - 1];
            Assert.AreEqual(ScoringConstants.CrowdMultiplier, grid.PlacePiece(PieceShapeCatalog.Get(ShapeId.Single), PieceColor.Coral, last.x, last.y, mods).ModifierMultiplier);
        }

        [Test]
        public void Meadow_NeedsEveryRunColorOnTheBoard()
        {
            var grid = new GridManager();
            Fill(grid, PieceColor.Teal, (0, 0));
            Fill(grid, PieceColor.Violet, (0, 1));
            Fill(grid, PieceColor.Lime, (0, 2));
            Assert.AreEqual(ScoringConstants.MeadowMultBonus, Place(grid, ShapeId.Single, PieceColor.Coral, 3, 3, ModifierId.Prairie).AdditiveMultBonus);
            var missing = new GridManager();
            Fill(missing, PieceColor.Teal, (0, 0));
            Assert.AreEqual(0, Place(missing, ShapeId.Single, PieceColor.Coral, 3, 3, ModifierId.Prairie).AdditiveMultBonus);
        }

        [Test]
        public void Wreath_PaysPerTileHuggedOnThreeSides()
        {
            var grid = new GridManager();
            Fill(grid, PieceColor.Teal, (1, 2), (3, 2), (2, 1));
            Assert.AreEqual(ScoringConstants.PointsToMult(ScoringConstants.WreathBonusPerCell), Place(grid, ShapeId.Single, PieceColor.Coral, 2, 2, ModifierId.Guirlande).AdditiveMultBonus);
            var two = new GridManager();
            Fill(two, PieceColor.Teal, (1, 2), (3, 2));
            Assert.AreEqual(0, Place(two, ShapeId.Single, PieceColor.Coral, 2, 2, ModifierId.Guirlande).AdditiveMultBonus);
        }

        [Test]
        public void Pollinator_Honey_AndNectar_PayPollen()
        {
            var grid = new GridManager();
            Fill(grid, PieceColor.Teal, (1, 2));
            Fill(grid, PieceColor.Violet, (3, 2));
            Assert.AreEqual(EconomyConstants.PollinatorLueur, Place(grid, ShapeId.Single, PieceColor.Coral, 2, 2, ModifierId.Pollinisateur).ModifierLueurBonus);
            Assert.AreEqual(0, Place(new GridManager(), ShapeId.Single, PieceColor.Coral, 2, 2, ModifierId.Pollinisateur).ModifierLueurBonus);

            var line = new GridManager();
            Fill(line, PieceColor.Coral, (0, 0), (1, 0), (2, 0), (4, 0));
            Assert.AreEqual(5 / EconomyConstants.HoneyCellsPerLueur, Place(line, ShapeId.Single, PieceColor.Coral, 3, 0, ModifierId.Miel).ModifierLueurBonus);

            Assert.AreEqual(EconomyConstants.NectarLueurPerSeed, Place(new GridManager(), ShapeId.Single, PieceColor.Coral, 2, 2, ModifierId.Nectar).ModifierLueurBonus);
        }

        /// <summary>Fills row 0 except the last plot, then completes it: one row cleared.</summary>
        private static PlacementResult HarvestOneRow(ModifierId mod)
        {
            var grid = new GridManager();
            for (int x = 0; x < GridManager.Size - 1; x++)
            {
                Fill(grid, PieceColor.Coral, (x, 0));
            }
            return Place(grid, ShapeId.Single, PieceColor.Coral, GridManager.Size - 1, 0, mod);
        }

        private static PlacementResult HarvestOneColumn(ModifierId mod)
        {
            var grid = new GridManager();
            for (int y = 0; y < GridManager.Size - 1; y++)
            {
                Fill(grid, PieceColor.Coral, (0, y));
            }
            return Place(grid, ShapeId.Single, PieceColor.Coral, 0, GridManager.Size - 1, mod);
        }

        [Test]
        public void RowCropAndColumnCrop_ReadTheDirectionOfTheHarvest()
        {
            int baseline = HarvestOneRow(ModifierId.Chaine).ModifierMultiplier;
            Assert.AreEqual(baseline * ScoringConstants.RowCropMultiplier, HarvestOneRow(ModifierId.CultureRang).ModifierMultiplier);
            Assert.AreEqual(baseline, HarvestOneRow(ModifierId.CultureColonne).ModifierMultiplier);
            Assert.AreEqual(baseline * ScoringConstants.ColumnCropMultiplier, HarvestOneColumn(ModifierId.CultureColonne).ModifierMultiplier);
            Assert.AreEqual(baseline, HarvestOneColumn(ModifierId.CultureRang).ModifierMultiplier);
        }

        [Test]
        public void Reaper_Tithe_Bounty_AndCleanSlate_ReadTheHarvest()
        {
            Assert.AreEqual(ScoringConstants.ReaperMultPerLine, HarvestOneRow(ModifierId.Faucheur).AdditiveMultBonus);
            Assert.AreEqual(EconomyConstants.TitheLueurPerLine, HarvestOneRow(ModifierId.Dime).ModifierLueurBonus);
            Assert.AreEqual(ScoringConstants.PointsToMult(GridManager.Size * ScoringConstants.BountyBonusPerTile), HarvestOneRow(ModifierId.Prime).AdditiveMultBonus);
            // The lone row was the only thing planted: the garden is empty after the harvest.
            int baseline = HarvestOneRow(ModifierId.Chaine).ModifierMultiplier;
            Assert.AreEqual(baseline * ScoringConstants.CleanSlateMultiplier, HarvestOneRow(ModifierId.TableRase).ModifierMultiplier);
            var other = new GridManager();
            Fill(other, PieceColor.Teal, (2, 3));
            for (int x = 0; x < GridManager.Size - 1; x++)
            {
                Fill(other, PieceColor.Coral, (x, 0));
            }
            Assert.AreEqual(1, Place(other, ShapeId.Single, PieceColor.Coral, GridManager.Size - 1, 0, ModifierId.TableRase).ModifierMultiplier, "a tile is left standing");
        }

        [Test]
        public void CrossHarvest_NeedsARowAndAColumnAtOnce()
        {
            var grid = new GridManager();
            int last = GridManager.Size - 1;
            for (int x = 1; x <= last; x++)
            {
                Fill(grid, PieceColor.Coral, (x, 0));
            }
            for (int y = 1; y <= last; y++)
            {
                Fill(grid, PieceColor.Teal, (0, y));
            }
            var result = Place(grid, ShapeId.Single, PieceColor.Coral, 0, 0, ModifierId.MoissonCroisee);
            Assert.AreEqual(ScoringConstants.CrossHarvestMultBonus, result.AdditiveMultBonus);
            Assert.AreEqual(0, HarvestOneRow(ModifierId.MoissonCroisee).AdditiveMultBonus);
        }

        [Test]
        public void Frostbreaker_NeedsAFrozenPlotOnTheHarvestedLine()
        {
            var grid = new GridManager();
            var frozen = grid.GetCell(0, 0);
            frozen.IsFrozen = true;
            frozen.IsLocked = true;
            for (int x = 1; x < GridManager.Size - 1; x++)
            {
                Fill(grid, PieceColor.Coral, (x, 0));
            }
            var result = Place(grid, ShapeId.Single, PieceColor.Coral, GridManager.Size - 1, 0, ModifierId.BriseGlace);
            Assert.AreEqual(ScoringConstants.FrostbreakerMultBonus, result.AdditiveMultBonus);
            Assert.AreEqual(0, HarvestOneRow(ModifierId.BriseGlace).AdditiveMultBonus, "no ice on a plain row");
        }

        [Test]
        public void SeasonedAndEarlyBird_ReadTheRound()
        {
            var run = NewRun(ModifierId.Saisons, ModifierId.Matinal);
            var first = PlaceAny(run);
            Assert.AreEqual(ScoringConstants.EarlyBirdMultBonus, first.AdditiveMultBonus);
            Assert.AreEqual(0f, first.ProgressiveAdditiveMult, 1e-4f, "no round cleared yet");
            run.DebugForceRoundComplete();
            Assert.IsTrue(run.LeaveShop());
            var second = PlaceAny(run);
            Assert.AreEqual(ScoringConstants.SeasonedMultPerRound, second.ProgressiveAdditiveMult, 1e-4f);
        }

        [Test]
        public void Marathoner_WaitsForRoundNine()
        {
            var run = NewRun(ModifierId.Marathonien);
            Assert.AreEqual(0, PlaceAny(run).AdditiveMultBonus);
            for (int i = 0; i < ScoringConstants.MarathonerFirstRoundIndex; i++)
            {
                run.DebugForceRoundComplete();
                Assert.IsTrue(run.LeaveShop());
            }
            Assert.AreEqual(ScoringConstants.MarathonerMultBonus, PlaceAny(run).AdditiveMultBonus);
        }

        [Test]
        public void FirstSeedAndLastSeed_ReadTheBudget()
        {
            var run = NewRun(ModifierId.PremiereGraine, ModifierId.DerniereGraine);
            Assert.AreEqual(ScoringConstants.FirstSeedMultiplier, PlaceAny(run).ModifierMultiplier);
            Assert.AreEqual(1, PlaceAny(run).ModifierMultiplier, "the second seed is neither first nor last");
        }

        [Test]
        public void PiggyBankAndSeedBank_ScaleWithWhatYouHold()
        {
            var run = NewRun(ModifierId.Tirelire, ModifierId.BanqueGraines);
            run.DebugSetLueur(30);
            float expected = 30 * ScoringConstants.PiggyBankMultPerPollen + run.Deck.DeckCount * ScoringConstants.SeedBankMultPerSeed;
            Assert.AreEqual(expected, PlaceAny(run).ProgressiveAdditiveMult, 1e-3f);
            run.DebugSetLueur(10000);
            float capped = ScoringConstants.PiggyBankMaxMult + run.Deck.DeckCount * ScoringConstants.SeedBankMultPerSeed;
            Assert.AreEqual(capped, PlaceAny(run).ProgressiveAdditiveMult, 1e-3f);
        }

        [Test]
        public void EnemyReadingFertilizers_FollowTheHpAndTheHeadcount()
        {
            var run = NewRun(ModifierId.Outsider, ModifierId.CoupFinal, ModifierId.SousBois);
            run.DebugSetEncounter(EnemyId.Basic, EnemyId.Locker);
            var full = PlaceAny(run);
            Assert.AreEqual(ScoringConstants.UnderdogMultBonus + 2 * ScoringConstants.UnderbrushMultPerEnemy, full.AdditiveMultBonus, "full HP: Underdog + 2 enemies, no Finishing Blow");
            int alive = 0;
            foreach (var enemy in run.CurrentEncounter)
            {
                if (!enemy.IsDead)
                {
                    enemy.ApplyDamage(enemy.CurrentHp - 1);
                    alive++;
                }
            }
            var low = PlaceAny(run);
            Assert.AreEqual(ScoringConstants.FinishingBlowMultBonus + alive * ScoringConstants.UnderbrushMultPerEnemy, low.AdditiveMultBonus);
        }

        [Test]
        public void WeatherDancers_FireOnTheirOwnWeatherOnly()
        {
            var cases = new (ModifierId Mod, WeatherId On, WeatherId Off)[]
            {
                (ModifierId.DanseDePluie, WeatherId.Rain, WeatherId.Sunshine),
                (ModifierId.DanseDePluie, WeatherId.Sprinkler, WeatherId.Clear),
                (ModifierId.DanseDuSoleil, WeatherId.GoldenHour, WeatherId.Rain),
                (ModifierId.Rustique, WeatherId.Frost, WeatherId.Heatwave),
                (ModifierId.CavalierVent, WeatherId.Hurricane, WeatherId.Dew),
            };
            foreach (var c in cases)
            {
                var on = NewRun(c.Mod);
                on.DebugSetWeather(c.On);
                Assert.AreEqual(ScoringConstants.WeatherDancerMultBonus, PlaceAny(on).AdditiveMultBonus, c.Mod + " on " + c.On);
                var off = NewRun(c.Mod);
                off.DebugSetWeather(c.Off);
                Assert.AreEqual(0, PlaceAny(off).AdditiveMultBonus, c.Mod + " on " + c.Off);
            }
        }

        [Test]
        public void Bossbane_DoublesOnlyInBossRounds()
        {
            var run = NewRun(ModifierId.Pourfendeur);
            Assert.AreEqual(1, PlaceAny(run).ModifierMultiplier);
            for (int i = 0; i < 3; i++)
            {
                run.DebugForceRoundComplete();
                Assert.IsTrue(run.LeaveShop());
            }
            Assert.IsTrue(run.IsBossRound);
            Assert.AreEqual(ScoringConstants.BossbaneMultiplier, PlaceAny(run).ModifierMultiplier);
        }

        [Test]
        public void RainBarrelAndGranary_PayPollenWhenARoundIsCleared()
        {
            var run = NewRun(ModifierId.Tonneau, ModifierId.Grange);
            run.DebugSetShufflesRemaining(3);
            run.DebugSetLueur(0);
            int budgetLeft = run.PiecesRemainingThisRound;
            run.DebugForceRoundComplete();
            int expected = budgetLeft + 3 * EconomyConstants.RainBarrelLueurPerShuffle + 2 * EconomyConstants.GranaryLueurPerFertilizer;
            Assert.AreEqual(expected, run.LastRoundEndLueurBonus);
        }

        [Test]
        public void Underdog_Finishing_Underbrush_AreUnusableWithoutEnemies()
        {
            var run = new RunManager(new SystemRandomProvider(1), ChallengeCatalog.Classic.Clone());
            Assert.IsTrue(run.IsModifierUsableThisRun(ModifierId.Outsider));
            Assert.IsFalse(new RunManager(new SystemRandomProvider(1), ChallengeCatalog.Summer).IsModifierUsableThisRun(ModifierId.Rustique), "Summer never frosts");
            Assert.IsTrue(new RunManager(new SystemRandomProvider(1), ChallengeCatalog.Marathon).IsModifierUsableThisRun(ModifierId.Rustique));
            GridManager.ResetSize();
            PieceColorUtility.ResetBaseColors();
        }
    }
}
