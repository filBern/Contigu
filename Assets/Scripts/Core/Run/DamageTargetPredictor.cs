using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>
    /// Read-only prediction of which enemies a placement's damage will touch, made BEFORE RunManager.PlacePiece runs so the
    /// presentation layer can hold each target's HP display at its pre-placement value. Mirrors the targeting in
    /// RunManager.ApplyJokerCombatOrDefaultDamage; it never mutates the run.
    /// </summary>
    public static class DamageTargetPredictor
    {
        /// <summary>
        /// Which combat kind drives targeting: read from the placement's whole merged group (cells already on the grid carrying a
        /// Joker combat trait) AND from the piece's own trait, which is not on the grid yet. If a spreading kind (Bombe/Éclat) and a
        /// single-target one are present at once, the spreading kind wins — its "every alive enemy" animation safely covers the
        /// narrower target too.
        /// </summary>
        public static PieceTraitKind? FindPriorityCombatKind(RunManager run, IReadOnlyList<Vector2Int> groupCells, PieceTrait? ownTrait = null)
        {
            bool hasSpreadingKind = false;
            PieceTraitKind? singleTargetKind = null;

            if (ownTrait.HasValue && PieceTrait.IsJokerCombatKind(ownTrait.Value.Kind))
            {
                Classify(ownTrait.Value.Kind, ref hasSpreadingKind, ref singleTargetKind);
            }
            for (int i = 0; i < groupCells.Count; i++)
            {
                var pos = groupCells[i];
                if (!GridManager.InBounds(pos.x, pos.y))
                {
                    continue;
                }
                var origin = run.Grid.GetCell(pos.x, pos.y).OriginTrait;
                if (!origin.HasValue || !PieceTrait.IsJokerCombatKind(origin.Value.Kind))
                {
                    continue;
                }
                Classify(origin.Value.Kind, ref hasSpreadingKind, ref singleTargetKind);
            }
            if (hasSpreadingKind)
            {
                return PieceTraitKind.Bombe;
            }
            return singleTargetKind;
        }

        private static void Classify(PieceTraitKind kind, ref bool hasSpreadingKind, ref PieceTraitKind? singleTargetKind)
        {
            if (kind == PieceTraitKind.Bombe || kind == PieceTraitKind.Eclat)
            {
                hasSpreadingKind = true;
            }
            else if (singleTargetKind == null)
            {
                singleTargetKind = kind;
            }
        }

        /// <summary>
        /// Every enemy index this placement's damage could plausibly touch. Index 0 is the primary target (the one the drain animation
        /// smoothly lerps); further indices are Bombe/Éclat candidates, filtered later to whichever actually took damage.
        /// </summary>
        public static List<int> FindDamagedEnemyIndices(RunManager run, int handIndex, int anchorX, int anchorY, out PieceTraitKind? combatKind)
        {
            var result = new List<int>();
            combatKind = null;
            if (!run.HasActiveEncounter)
            {
                return result;
            }
            var encounter = run.CurrentEncounter;
            var tokenSlot = run.Deck.Hand[handIndex];
            PieceTraitKind? kind = null;
            if (tokenSlot.HasValue)
            {
                var token = tokenSlot.Value;
                var shape = PieceShapeCatalog.GetRotated(token.Shape, run.Deck.HandRotations[handIndex]);
                var previewGroup = run.Grid.PreviewGroup(shape, token.Color, anchorX, anchorY);
                PieceTrait? combatTrait = null;
                foreach (var ownTrait in token.Traits)
                {
                    if (PieceTrait.IsJokerCombatKind(ownTrait.Kind))
                    {
                        combatTrait = ownTrait;
                        break;
                    }
                }
                kind = FindPriorityCombatKind(run, previewGroup, combatTrait);
            }
            combatKind = kind;

            if (kind == PieceTraitKind.Range)
            {
                // Back-to-front instead of front-to-back — the only difference from the default rule below.
                for (int i = encounter.Count - 1; i >= 0; i--)
                {
                    if (!encounter[i].IsDead)
                    {
                        result.Add(i);
                        return result;
                    }
                }
                return result;
            }

            if (kind == PieceTraitKind.Precision)
            {
                int weakest = -1;
                for (int i = 0; i < encounter.Count; i++)
                {
                    if (encounter[i].IsDead)
                    {
                        continue;
                    }
                    if (weakest < 0 || encounter[i].CurrentHp < encounter[weakest].CurrentHp)
                    {
                        weakest = i;
                    }
                }
                if (weakest >= 0)
                {
                    result.Add(weakest);
                }
                return result;
            }

            // Default (including no trait) and Sangsue both hit only the front alive enemy; Bombe/Éclat list every alive enemy.
            for (int i = 0; i < encounter.Count; i++)
            {
                if (!encounter[i].IsDead)
                {
                    result.Add(i);
                }
            }
            if (kind != PieceTraitKind.Bombe && kind != PieceTraitKind.Eclat && result.Count > 1)
            {
                result.RemoveRange(1, result.Count - 1);
            }
            return result;
        }
    }
}
