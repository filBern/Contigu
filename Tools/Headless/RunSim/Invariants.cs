using Contigu.Core;
using UnityEngine;

namespace Contigu.RunSim
{
    /// <summary>
    /// Rules that must hold after every action of a run, whatever the player does (QA: --check). Each returns a short description of the
    /// first violation found, or null when the run is sane. Kept conservative: a violation is a real bug, not a balance opinion.
    /// </summary>
    public static class Invariants
    {
        public static bool Enabled;
        private static int _lastSnapshotRound = -1;

        public static void Reset()
        {
            _lastSnapshotRound = -1;
        }

        public static string Check(RunManager run)
        {
            if (run.Lueur < 0) { return "Pollen is negative: " + run.Lueur; }
            if (run.ShufflesRemaining < 0) { return "Shuffles remaining is negative: " + run.ShufflesRemaining; }
            if (run.PiecesRemainingThisRound < 0) { return "Seeds remaining is negative: " + run.PiecesRemainingThisRound; }
            if (run.Deck.DeckCount < DeckManager.MinDeckSize) { return "The bag is below its minimum size: " + run.Deck.DeckCount; }
            if (run.ActiveModifiers.Count > EconomyConstants.MaxActiveModifiers) { return "More fertilizers than the cap: " + run.ActiveModifiers.Count; }
            for (int i = 0; i < run.ActiveModifiers.Count; i++)
            {
                if (run.GetModifierLevel(i) < 1) { return "Fertilizer " + run.ActiveModifiers[i] + " has level " + run.GetModifierLevel(i); }
                if (ModifierCatalog.Get(run.ActiveModifiers[i]) == null) { return "Fertilizer " + run.ActiveModifiers[i] + " is held but not in the catalog"; }
            }

            foreach (var token in run.Deck.Deck)
            {
                if (token.Color != PieceColor.Joker) { continue; }
                int combat = 0;
                foreach (var trait in token.Traits) { if (PieceTrait.IsJokerCombatKind(trait.Kind)) { combat++; } }
                if (combat != 1) { return "A Joker seed has " + combat + " combat traits (it must have exactly 1): " + token.Shape; }
            }

            var hand = run.Deck.Hand;
            if (hand.Count != DeckManager.HandSize && hand.Count != DeckManager.MaxHandSize) { return "The hand has " + hand.Count + " slots"; }
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i].HasValue && hand[i].Value.Color != PieceColor.Joker && !PieceColorUtility.IsActive(hand[i].Value.Color))
                {
                    return "Hand slot " + i + " holds a " + hand[i].Value.Color + " seed, a color this run does not have";
                }
            }

            string grid = CheckGrid(run);
            if (grid != null) { return grid; }

            if (run.HasActiveEncounter)
            {
                for (int i = 0; i < run.CurrentEncounter.Count; i++)
                {
                    var enemy = run.CurrentEncounter[i];
                    if (enemy.CurrentHp > enemy.CurrentMaxHp) { return enemy.Definition.Id + " has more HP (" + enemy.CurrentHp + ") than its max (" + enemy.CurrentMaxHp + ")"; }
                    if (enemy.CurrentHp < 0) { return enemy.Definition.Id + " has negative HP: " + enemy.CurrentHp; }
                    if (enemy.IsDead != (enemy.CurrentHp <= 0)) { return enemy.Definition.Id + " dead flag disagrees with its HP"; }
                    if (!enemy.IsDead)
                    {
                        for (int c = 0; c < enemy.FrozenCells.Count; c++)
                        {
                            if (!run.Grid.GetCell(enemy.FrozenCells[c]).IsFrozen) { return enemy.Definition.Id + " remembers a frozen plot that is not frozen: " + enemy.FrozenCells[c]; }
                        }
                    }
                }
            }

            return CheckSnapshotRoundTrip(run);
        }

        private static string CheckGrid(RunManager run)
        {
            foreach (var pos in GridManager.AllPositions())
            {
                var cell = run.Grid.GetCell(pos);
                if (cell.IsFilled != cell.FilledColor.HasValue) { return "Cell " + pos + ": filled flag and color disagree"; }
                if (cell.IsFrozen && cell.IsFilled) { return "Cell " + pos + " is frozen and filled at once"; }
                if (cell.IsFrozen && !cell.IsLocked) { return "Cell " + pos + " is frozen but not locked"; }
                if (cell.IsFilled && cell.FilledColor.Value != PieceColor.Joker && !PieceColorUtility.IsActive(cell.FilledColor.Value))
                {
                    return "Cell " + pos + " holds a " + cell.FilledColor.Value + " tile, a color this run does not have";
                }
                if (!cell.IsFilled && (cell.IsGolden && cell.OriginTrait.HasValue)) { return "Empty cell " + pos + " still carries an upgrade stamp"; }
            }
            return null;
        }

        /// <summary>At each new round's start, saving then restoring the run must reproduce the same checkpoint.</summary>
        private static string CheckSnapshotRoundTrip(RunManager run)
        {
            if (run.State != RunState.InProgress || run.CurrentRoundIndex == _lastSnapshotRound || run.PiecesRemainingThisRound != run.CurrentBudget)
            {
                return null;
            }
            _lastSnapshotRound = run.CurrentRoundIndex;
            string text = run.RoundStartSnapshot.ToText();
            var parsed = RunSnapshot.FromText(text);
            if (parsed == null) { return "The round-start save could not be read back"; }
            if (parsed.ToText() != text) { return "The round-start save changes when written again"; }
            var restored = RunManager.Restore(parsed, new SystemRandomProvider(1));
            if (restored == null) { return "The round-start save could not be resumed"; }
            string again = restored.RoundStartSnapshot.ToText();
            if (again != text)
            {
                var a = text.Split('\n');
                var b = again.Split('\n');
                for (int i = 0; i < a.Length && i < b.Length; i++)
                {
                    if (a[i] != b[i]) { return "Resuming the round-start save does not reproduce it (" + a[i].Split('=')[0] + ": " + a[i] + " vs " + b[i] + ")"; }
                }
                return "Resuming the round-start save does not reproduce it";
            }
            return null;
        }
    }
}
