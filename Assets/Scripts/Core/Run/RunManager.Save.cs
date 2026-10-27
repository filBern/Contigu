using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>Saving and resuming a run: a checkpoint is taken at the start of every round, and a run resumes from the start of its last checkpoint's round.</summary>
    public sealed partial class RunManager
    {
        /// <summary>The run as it stood when the current round started — what "Save &amp; Quit" writes. Never null.</summary>
        public RunSnapshot RoundStartSnapshot { get; private set; }

        private RunSnapshot CaptureSnapshot()
        {
            var s = new RunSnapshot();
            s.Challenge = _challenge.Id;
            s.RoundIndex = CurrentRoundIndex;
            s.TotalScore = TotalScore;
            s.Lueur = Lueur;
            s.ShufflesRemaining = ShufflesRemaining;
            s.IsEndless = IsEndless;
            s.ActiveColors.AddRange(PieceColorUtility.BaseColors);
            s.Deck.AddRange(Deck.Deck);
            s.Modifiers.AddRange(_activeModifiers);
            s.ModifierLevels.AddRange(_modifierLevels);
            s.ModifierAges.AddRange(_modifierAges);
            foreach (var kv in _modifierUsageCounts) { s.ModifierUsage[kv.Key] = kv.Value; }
            foreach (var kv in _shapeMasteryLevels) { s.ShapeMastery[kv.Key] = kv.Value; }
            foreach (var kv in _colorMasteryLevels) { s.ColorMastery[kv.Key] = kv.Value; }
            s.SpecialPiecesPlayed = _specialPiecesPlayedCount;
            s.CompostsBought = _compostsBought;
            s.PerennialScores = _perennialScores;
            s.LastPurchasedModifier = _lastPurchasedModifierId;
            s.Weather.AddRange(_weatherByRound);
            s.WeatherColors.AddRange(_weatherColorByRound);
            s.WeatherDisabled = _weatherDisabled;
            foreach (var pos in GridManager.AllPositions())
            {
                if (Grid.GetCell(pos).IsPermanentMultiplier)
                {
                    s.PermanentBonusCells.Add(pos);
                }
            }
            return s;
        }

        /// <summary>Rebuilds a run at the start of the round <paramref name="snapshot"/> was taken in. Null when the snapshot is unusable.</summary>
        public static RunManager Restore(RunSnapshot snapshot, IRandomProvider rng)
        {
            if (snapshot == null)
            {
                return null;
            }
            var run = new RunManager(rng, ChallengeCatalog.Get(snapshot.Challenge));
            run.ApplySnapshot(snapshot);
            return run;
        }

        private void ApplySnapshot(RunSnapshot s)
        {
            if (s.ActiveColors.Count > 0 && s.ActiveColors.Count < PieceColorUtility.AllBaseColors.Count)
            {
                PieceColorUtility.SetActiveBaseColors(s.ActiveColors);
            }
            else
            {
                PieceColorUtility.ResetBaseColors();
            }
            Deck.RestoreDeck(s.Deck);

            _activeModifiers.Clear();
            _modifierLevels.Clear();
            _modifierAges.Clear();
            for (int i = 0; i < s.Modifiers.Count; i++)
            {
                AddActiveModifier(s.Modifiers[i]);
                _modifierLevels[i] = s.ModifierLevels[i];
                _modifierAges[i] = s.ModifierAges[i];
            }
            _modifierUsageCounts.Clear();
            foreach (var kv in s.ModifierUsage) { _modifierUsageCounts[kv.Key] = kv.Value; }
            _shapeMasteryLevels.Clear();
            foreach (var kv in s.ShapeMastery) { _shapeMasteryLevels[kv.Key] = kv.Value; }
            _colorMasteryLevels.Clear();
            foreach (var kv in s.ColorMastery) { _colorMasteryLevels[kv.Key] = kv.Value; }

            _specialPiecesPlayedCount = s.SpecialPiecesPlayed;
            _compostsBought = s.CompostsBought;
            _perennialScores = s.PerennialScores;
            _lastPurchasedModifierId = s.LastPurchasedModifier;
            Lueur = s.Lueur;
            TotalScore = s.TotalScore;
            IsEndless = s.IsEndless;

            _weatherByRound.Clear();
            _weatherByRound.AddRange(s.Weather);
            _weatherColorByRound.Clear();
            _weatherColorByRound.AddRange(s.WeatherColors);
            _weatherDisabled = s.WeatherDisabled;

            for (int i = 0; i < s.PermanentBonusCells.Count; i++)
            {
                var pos = s.PermanentBonusCells[i];
                if (GridManager.InBounds(pos.x, pos.y))
                {
                    Grid.GetCell(pos).IsPermanentMultiplier = true;
                }
            }

            CurrentRoundIndex = s.RoundIndex;
            StartRound();
            // The saved count already includes this round's own start bonus (Wind's free Shuffle), which StartRound just granted again.
            ShufflesRemaining = s.ShufflesRemaining;
            RoundStartSnapshot = CaptureSnapshot();
        }
    }
}
