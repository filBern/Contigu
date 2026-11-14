#if UNITY_EDITOR || CONTIGU_DEBUG
using System.Collections.Generic;
using Contigu.Core;
using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>Opens any screen with sample data for the demo scenes (see <see cref="SandboxScenario"/>). Editor and debug builds only.</summary>
    public sealed partial class GameBootstrap
    {
        public void ApplySandbox(SandboxScreen screen)
        {
            _challengeSelectView.Hide();
            _endScreenView.Hide();
            if (screen == SandboxScreen.ChallengeSelect)
            {
                // Every challenge unlocked and the pledges available, so the whole card can be looked at.
                _metaStats = MetaStatsRecorder.UnlockAll(_metaStats);
                _unlockState = _metaStats.ReadUnlocks();
                _challengeSelectView.Show(_metaStats);
                return;
            }

            StartNewRun(ChallengeCatalog.Classic);
            PrepareSandboxRun();

            switch (screen)
            {
                case SandboxScreen.Board:
                    FillSandboxGrid();
                    RefreshAll();
                    break;
                case SandboxScreen.RoundSummary:
                    _run.DebugForceRoundComplete();
                    RefreshAll();
                    _roundSummaryView.Show(_run.LastRoundSummary, null);
                    break;
                case SandboxScreen.Victory:
                case SandboxScreen.Defeat:
                    FillSandboxStats();
                    RefreshAll();
                    _endScreenView.SetRunStats(_run.Stats, TopFertilizerLine());
                    if (screen == SandboxScreen.Victory) { _endScreenView.ShowVictory(48250, _metaStats, true, "New in your collection (3): Scythe Sprout, Greenhouse Lamp, Mult +4"); }
                    else { _endScreenView.ShowDefeat(9, 31200, _metaStats, false, false, "New in your collection (2): Clone Sprout, Seed Library", true); }
                    break;
                default:
                    OpenSandboxShop(screen);
                    break;
            }
        }

        /// <summary>A rich run: four fertilizers, tile upgrades on several seeds, plenty of Pollen.</summary>
        private void PrepareSandboxRun()
        {
            _run.DebugDisableWeather();
            _run.DebugGrantModifier(ModifierId.DevotionCoral);
            _run.DebugGrantModifier(ModifierId.Chaine);
            _run.DebugGrantModifier(ModifierId.Polyvalence);
            _run.DebugGrantModifier(ModifierId.Forteresse);
            var tokens = new List<PieceToken>(_run.Deck.Deck);
            var kinds = new[] { PieceTraitKind.Corner, PieceTraitKind.Scholar, PieceTraitKind.Golden, PieceTraitKind.Chameleon, PieceTraitKind.Spark };
            for (int i = 0; i < kinds.Length && i < tokens.Count; i++)
            {
                tokens[i] = tokens[i].WithTrait(new PieceTrait(kinds[i], 0));
            }
            _run.Deck.RestoreDeck(tokens);
            _run.Deck.RedealHand();
            _run.DebugGrantLueur(60);
            FillSandboxStats();
        }

        private void FillSandboxGrid()
        {
            var colors = PieceColorUtility.BaseColors;
            int n = 0;
            for (int y = 0; y < GridManager.Size - 2; y++)
            {
                for (int x = 0; x < GridManager.Size; x++)
                {
                    if ((x + y) % 3 == 0) { continue; }
                    var color = colors[(x / 2 + y) % colors.Count];
                    if (_run.Grid.CanPlace(PieceShapeCatalog.Get(ShapeId.Single), x, y))
                    {
                        _run.Grid.PlacePiece(PieceShapeCatalog.Get(ShapeId.Single), color, x, y);
                        n++;
                    }
                }
            }
            _gridView.Refresh();
        }

        private void FillSandboxStats()
        {
            var stats = _run.Stats;
            stats.Placements = 64;
            stats.BestPlacementScore = 1840;
            stats.BestPlacementRound = 6;
            stats.BestPlacementShape = ShapeId.Sq2;
            stats.BestPlacementColor = PieceColor.Teal;
            stats.BestMultTimes100 = 1450;
            stats.BestGroupSize = 11;
            stats.LinesHarvested = 23;
            stats.BestHarvestLines = 3;
            stats.PollenFromPlacements = 88;
            stats.ShufflesUsed = 4;
        }

        /// <summary>The shop, or one of its choice screens (the workshop item is placed in its slot and bought).</summary>
        private void OpenSandboxShop(SandboxScreen screen)
        {
            _run.DebugForceRoundComplete();
            RefreshAll();
            _shopView.Show(_run);
            int slot = 0;
            UpgradeDefinition item = null;
            switch (screen)
            {
                case SandboxScreen.FertilizerPack: item = UpgradeCatalog.FertilizerPack; break;
                case SandboxScreen.SproutPack:
                case SandboxScreen.SproutSeedChoice: item = UpgradeCatalog.SproutPack; break;
                case SandboxScreen.MasteryPack: item = UpgradeCatalog.MasteryPack; break;
                case SandboxScreen.SeedPack: item = UpgradeCatalog.SeedPack; break;
                case SandboxScreen.Transplant: item = UpgradeCatalog.Transplant; break;
                case SandboxScreen.Forge: item = UpgradeCatalog.Forge; slot = 3; break;
                case SandboxScreen.Compost: item = UpgradeCatalog.Compost; slot = 2; break;
            }
            if (item == null)
            {
                return;
            }
            _run.DebugSetWorkshopSlot(slot, item);
            OnUpgradeBuyRequested(slot);
            if (screen == SandboxScreen.SproutSeedChoice)
            {
                OnPackOptionChosen(0);
            }
        }
    }
}
#endif
