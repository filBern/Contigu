using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>Boss-round effects and the Hater enemies' scoring rules.</summary>
    public sealed partial class RunManager
    {
        /// <summary>
        /// Boss round mechanic (see ChallengeDefinition.BossLockPiecesInterval):
        /// locks _challenge.BossLockCellsPerInterval more random empty cells
        /// and folds in any score that locking produces (see
        /// GridManager.LockFreeCellsAndCheckClears). Returns the newly locked
        /// cells so the presentation layer can refresh their visuals.
        /// </summary>
        private IReadOnlyList<Vector2Int> ApplyBossLockTick()
        {
            var lockOutcome = Grid.LockFreeCellsAndCheckClears(_challenge.BossLockCellsPerInterval, _rng);
            if (lockOutcome.LineClearScore > 0)
            {
                RoundScore += lockOutcome.LineClearScore;
                TotalScore += lockOutcome.LineClearScore;
            }
            Lueur += lockOutcome.LueurEarned;
            return lockOutcome.LockedCells;
        }

        /// <summary>
        /// Playtest request, cited textually: "Boss qui en début de round, ajoute une ou plusieurs Frozen
        /// tile que la seule manière de retirer c'est en line break. Sinon aucune pièce peut être ajouté
        /// dessus" — applied to Plague (an existing boss, rather than a brand-new enemy with no art; not
        /// HeavyLocker, whose own roaming-lock tests assert an exact CountLockedCells invariant this would
        /// break) once per round it's alive and part of the encounter, freezing PlagueFreezeCellCount empty
        /// cells (see GridManager.FreezeRandomCells/Cell.IsFrozen). Called from StartRound, right after the
        /// fresh encounter is built, so the frozen cells are already in place before the player's first
        /// placement of the round.
        /// </summary>
        private const int PlagueFreezeCellCount = 3;

        private void ApplyPlagueRoundStartFreeze()
        {
            for (int i = 0; i < _currentEncounter.Count; i++)
            {
                if (_currentEncounter[i].Definition.Id == EnemyId.Plague && !_currentEncounter[i].IsDead)
                {
                    Grid.FreezeRandomCells(PlagueFreezeCellCount, _rng);
                    return;
                }
            }
        }

        /// <summary>See UpgradeDefinition.PermanentBonusSquare's own doc comment — picks one random board position via GridManager.MarkRandomPermanentBonusSquare. No candidate index/surfaced result needed: unlike a Bank-pool "no sub-choice" upgrade (Joker, Piece/Color Mastery), there's nothing for the reveal UI to show beyond the upgrade's own name.</summary>
        private void ApplyPermanentBonusSquare()
        {
            LastPermanentBonusPosition = Grid.MarkRandomPermanentBonusSquare(_rng);
        }

        /// <summary>Removes point events tied to the cursed color while preserving play legality, clears, Lueur, and non-point multiplier effects.</summary>
        private void ApplyCursedColorScoreRule(PlacementResult placement, PieceColor cursedColor)
        {
            var clearedColors = new Dictionary<Vector2Int, PieceColor>();
            for (int i = 0; i < placement.ClearedCells.Count; i++)
            {
                clearedColors[placement.ClearedCells[i]] = placement.ClearedCellColors[i];
            }
            for (int i = 0; i < placement.DestroyedCells.Count; i++)
            {
                if (placement.DestroyedCellColors[i].HasValue)
                {
                    clearedColors[placement.DestroyedCells[i]] = placement.DestroyedCellColors[i].Value;
                }
            }

            var keptEvents = new List<ScoreEvent>(placement.ScoreEvents.Count);
            placement.GroupBonus = 0;
            placement.GoldenBonus = 0;
            placement.LineClearScore = 0;
            placement.ModifierBonus = 0;
            placement.TraitBonus = 0;
            placement.ShapeMasteryBonus = 0;
            placement.ColorMasteryBonus = 0;

            for (int i = 0; i < placement.ScoreEvents.Count; i++)
            {
                var scoreEvent = placement.ScoreEvents[i];
                if (IsPointEvent(scoreEvent.Type) && IsPositionColor(scoreEvent.Position, clearedColors, cursedColor))
                {
                    RecordCancelledPoints(placement, scoreEvent);
                    continue;
                }

                keptEvents.Add(scoreEvent);
                switch (scoreEvent.Type)
                {
                    case ScoreEventType.Group: placement.GroupBonus += scoreEvent.Amount; break;
                    case ScoreEventType.Golden: placement.GoldenBonus += scoreEvent.Amount; break;
                    case ScoreEventType.LineClear:
                    case ScoreEventType.Bastion: placement.LineClearScore += scoreEvent.Amount; break;
                    case ScoreEventType.Modifier: placement.ModifierBonus += scoreEvent.Amount; break;
                    case ScoreEventType.Trait: placement.TraitBonus += scoreEvent.Amount; break;
                    case ScoreEventType.ShapeMastery: placement.ShapeMasteryBonus += scoreEvent.Amount; break;
                    case ScoreEventType.ColorMastery: placement.ColorMasteryBonus += scoreEvent.Amount; break;
                }
            }
            placement.ScoreEvents = keptEvents;
        }

        private static void RecordCancelledPoints(PlacementResult placement, ScoreEvent dropped)
        {
            if (dropped.Amount == 0)
            {
                return;
            }
            var list = new List<(Vector2Int, int)>(placement.CancelledPoints);
            list.Add((dropped.Position, dropped.Amount));
            placement.CancelledPoints = list;
        }

        private static bool IsPointEvent(ScoreEventType type)
        {
            return type == ScoreEventType.Group || type == ScoreEventType.Golden ||
                type == ScoreEventType.LineClear || type == ScoreEventType.Bastion ||
                type == ScoreEventType.Modifier || type == ScoreEventType.Trait ||
                type == ScoreEventType.ShapeMastery || type == ScoreEventType.ColorMastery;
        }

        private bool IsPositionColor(Vector2Int position, Dictionary<Vector2Int, PieceColor> clearedColors, PieceColor cursedColor)
        {
            if (clearedColors.TryGetValue(position, out var clearedColor))
            {
                return clearedColor == cursedColor;
            }
            var cell = Grid.GetCell(position);
            return cell.IsFilled && cell.FilledColor == cursedColor;
        }

        /// <summary>"Shape Hater" - <see cref="ApplyCursedColorScoreRule"/>'s exact mirror, keyed by <see cref="Cell.FilledShapeId"/> instead of color.</summary>
        private void ApplyShapeHaterScoreRule(PlacementResult placement, ShapeId hatedShape)
        {
            var clearedShapes = new Dictionary<Vector2Int, ShapeId>();
            for (int i = 0; i < placement.ClearedCells.Count; i++)
            {
                if (placement.ClearedCellShapes[i].HasValue)
                {
                    clearedShapes[placement.ClearedCells[i]] = placement.ClearedCellShapes[i].Value;
                }
            }
            for (int i = 0; i < placement.DestroyedCells.Count; i++)
            {
                if (placement.DestroyedCellShapes[i].HasValue)
                {
                    clearedShapes[placement.DestroyedCells[i]] = placement.DestroyedCellShapes[i].Value;
                }
            }

            var keptEvents = new List<ScoreEvent>(placement.ScoreEvents.Count);
            placement.GroupBonus = 0;
            placement.GoldenBonus = 0;
            placement.LineClearScore = 0;
            placement.ModifierBonus = 0;
            placement.TraitBonus = 0;
            placement.ShapeMasteryBonus = 0;
            placement.ColorMasteryBonus = 0;

            for (int i = 0; i < placement.ScoreEvents.Count; i++)
            {
                var scoreEvent = placement.ScoreEvents[i];
                if (IsPointEvent(scoreEvent.Type) && IsPositionShape(scoreEvent.Position, clearedShapes, hatedShape))
                {
                    RecordCancelledPoints(placement, scoreEvent);
                    continue;
                }

                keptEvents.Add(scoreEvent);
                switch (scoreEvent.Type)
                {
                    case ScoreEventType.Group: placement.GroupBonus += scoreEvent.Amount; break;
                    case ScoreEventType.Golden: placement.GoldenBonus += scoreEvent.Amount; break;
                    case ScoreEventType.LineClear:
                    case ScoreEventType.Bastion: placement.LineClearScore += scoreEvent.Amount; break;
                    case ScoreEventType.Modifier: placement.ModifierBonus += scoreEvent.Amount; break;
                    case ScoreEventType.Trait: placement.TraitBonus += scoreEvent.Amount; break;
                    case ScoreEventType.ShapeMastery: placement.ShapeMasteryBonus += scoreEvent.Amount; break;
                    case ScoreEventType.ColorMastery: placement.ColorMasteryBonus += scoreEvent.Amount; break;
                }
            }
            placement.ScoreEvents = keptEvents;
        }

        private bool IsPositionShape(Vector2Int position, Dictionary<Vector2Int, ShapeId> clearedShapes, ShapeId hatedShape)
        {
            if (clearedShapes.TryGetValue(position, out var clearedShape))
            {
                return clearedShape == hatedShape;
            }
            var cell = Grid.GetCell(position);
            return cell.IsFilled && cell.FilledShapeId == hatedShape;
        }

        /// <summary>Applies Color Hater's and/or Shape Hater's score-cancellation rule for this placement, if either is alive in the current encounter.</summary>
        private void ApplyHaterScoreRules(PlacementResult placement)
        {
            for (int i = 0; i < _currentEncounter.Count; i++)
            {
                var enemy = _currentEncounter[i];
                if (enemy.IsDead)
                {
                    continue;
                }
                int cancelledBefore = placement.CancelledPoints.Count;
                if (enemy.Definition.Id == EnemyId.ColorHater && enemy.HatedColor.HasValue)
                {
                    ApplyCursedColorScoreRule(placement, enemy.HatedColor.Value);
                }
                else if (enemy.Definition.Id == EnemyId.ShapeHater && enemy.HatedShape.HasValue)
                {
                    ApplyShapeHaterScoreRule(placement, enemy.HatedShape.Value);
                }
                if (placement.CancelledPoints.Count > cancelledBefore)
                {
                    placement.CancelledBy = enemy.Definition.Id;
                }
            }
        }
    }
}
