using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>Fertilizers resolved after lines are cleared (line patterns, destruction) and their helpers.</summary>
    public sealed partial class GridManager
    {
        private delegate int PostClearModifierEffect(PostClearModifierContext ctx);

        /// <summary>One entry per post-clear-evaluable modifier — same rationale as <see cref="BuildPreClearEffects"/>.</summary>
        private Dictionary<ModifierId, PostClearModifierEffect> BuildPostClearEffects()
        {
            return new Dictionary<ModifierId, PostClearModifierEffect>
            {
                { ModifierId.Collectionneur, ctx => ApplyCollectionneur(ctx.ClearInfo, ctx.PlacedCells, ctx.Events) },
                { ModifierId.CollectionneurLueur, ctx => { ctx.Lueur += ApplyCollectionneurLueur(ctx.ClearInfo, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Macon, ctx => { ctx.AdditiveMult += ApplyMaconBonus(ctx.ClearInfo, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Demolisseur, ctx => { ctx.Multiplier *= ApplyDemolisseur(ctx.ClearInfo, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.ArcEnCiel, ctx => { ctx.Multiplier *= ApplyPerLineMultiplier(ctx.ClearInfo, ctx.PlacedCells, ctx.Events, ContainsAllBaseColors, ScoringConstants.ArcEnCielMultiplierPerLine); return 0; } },
                { ModifierId.ArcEnCielLueur, ctx => { ctx.Lueur += ApplyPerLineLueur(ctx.ClearInfo, ctx.PlacedCells, ctx.Events, ContainsAllBaseColors, EconomyConstants.ArcEnCielLueurPerLine); return 0; } },
                { ModifierId.Alternance, ctx => { ctx.Multiplier *= ApplyPerLineMultiplier(ctx.ClearInfo, ctx.PlacedCells, ctx.Events, IsAlternatingTwoColors, ScoringConstants.AlternanceMultiplierPerLine); return 0; } },
                { ModifierId.AlternanceLueur, ctx => { ctx.Lueur += ApplyPerLineLueur(ctx.ClearInfo, ctx.PlacedCells, ctx.Events, IsAlternatingTwoColors, EconomyConstants.AlternanceLueurPerLine); return 0; } },
                { ModifierId.Palindrome, ctx => { ctx.Multiplier *= ApplyPerLineMultiplier(ctx.ClearInfo, ctx.PlacedCells, ctx.Events, IsPalindrome, ScoringConstants.PalindromeMultiplierPerLine); return 0; } },
                { ModifierId.Gradient, ctx => { ctx.Multiplier *= ApplyGradient(ctx.ClearInfo, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Bloc, ctx => { ctx.Multiplier *= ApplyPerLineMultiplier(ctx.ClearInfo, ctx.PlacedCells, ctx.Events, IsAllBlocksOfAtLeastTwo, ScoringConstants.BlocMultiplierPerLine); return 0; } },
                { ModifierId.MonochromeLigne, ctx => { ctx.Multiplier *= ApplyPerLineMultiplier(ctx.ClearInfo, ctx.PlacedCells, ctx.Events, IsMonochromeLine, ScoringConstants.MonochromeLigneMultiplierPerLine); return 0; } },
                { ModifierId.MonochromeLigneLueur, ctx => { ctx.Lueur += ApplyPerLineLueur(ctx.ClearInfo, ctx.PlacedCells, ctx.Events, IsMonochromeLine, EconomyConstants.MonochromeLigneLueurPerLine); return 0; } },
                { ModifierId.EspaceLibre, ctx => { ctx.Multiplier *= ApplyEspaceLibre(ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Rafale, ctx => { ctx.Multiplier *= ApplyRafale(ctx.ClearedByPreviousPlacement, ctx.ClearInfo, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.MoissonCroisee, ctx => { ctx.AdditiveMult += ApplyMoissonCroisee(ctx.ClearInfo, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Faucheur, ctx => { ctx.AdditiveMult += ApplyFaucheur(ctx.ClearInfo, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.TableRase, ctx => { ctx.Multiplier *= ApplyTableRase(ctx.ClearInfo, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Dime, ctx => { ctx.Lueur += ApplyDime(ctx.ClearInfo, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.CultureRang, ctx => { ctx.Multiplier *= ApplyCrop(ctx.ClearInfo, true, ScoringConstants.RowCropMultiplier, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.CultureColonne, ctx => { ctx.Multiplier *= ApplyCrop(ctx.ClearInfo, false, ScoringConstants.ColumnCropMultiplier, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Prime, ctx => ApplyPrime(ctx.ClearInfo, ctx.PlacedCells, ctx.Events) },
                { ModifierId.BriseGlace, ctx => { ctx.AdditiveMult += ApplyBriseGlace(ctx.ClearInfo, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Densite, ctx => { ctx.ProgressiveAdditiveMult += ApplyDensite(ctx.PlacedCells, ctx.Events); return 0; } }
            };
        }

        /// <summary>Collectionneur/Maçon/Démolisseur/the 8 line-pattern modifiers all need the outcome of this placement's line clears, so they can only be evaluated after <see cref="CheckAndClearLines"/> runs.</summary>
        private int ApplyPostClearModifiers(IReadOnlyList<ModifierId> activeModifiers, ClearInfo clearInfo, List<Vector2Int> placedCells, bool clearedByPreviousPlacement, List<ScoreEvent> events, out int modifierMultiplier, out int lueurBonus, out float progressiveAdditiveMult, out int additiveMultBonus, IReadOnlyList<int> modifierLevels = null)
        {
            var ctx = new PostClearModifierContext
            {
                ClearInfo = clearInfo,
                PlacedCells = placedCells,
                Events = events,
                ClearedByPreviousPlacement = clearedByPreviousPlacement
            };

            int total = 0;
            for (int i = 0; i < activeModifiers.Count; i++)
            {
                var id = activeModifiers[i];
                int eventsBefore = events.Count;
                int lueurBefore = ctx.Lueur;
                int bonus = _postClearEffects.TryGetValue(id, out var effect) ? effect(ctx) : 0;

                // Same reasoning as ApplyPreClearModifiers above — only this
                // call's own ctx.Lueur delta is rescaled, and
                // ctx.Multiplier/ctx.ProgressiveAdditiveMult/ctx.AdditiveMult
                // are left alone since Mult is events-derived, not read from
                // them.
                float levelFactor = GetModifierLevelFactor(modifierLevels, i);
                if (levelFactor != 1f)
                {
                    bonus = Mathf.RoundToInt(bonus * levelFactor);
                    int lueurDelta = ctx.Lueur - lueurBefore;
                    ctx.Lueur = lueurBefore + Mathf.RoundToInt(lueurDelta * levelFactor);
                }

                TagNewEvents(events, eventsBefore, id, i, levelFactor);
                total += bonus;
            }
            modifierMultiplier = ctx.Multiplier;
            lueurBonus = ctx.Lueur;
            progressiveAdditiveMult = ctx.ProgressiveAdditiveMult;
            additiveMultBonus = ctx.AdditiveMult;
            return total;
        }

        /// <summary>
        /// Stamps every event appended since <paramref name="startIndex"/>
        /// with the modifier that produced it (<paramref
        /// name="modifierIndex"/> is its position within activeModifiers,
        /// needed because Copieur can make the same id occupy more than one
        /// position). <paramref name="levelFactor"/> also rescales each
        /// event's Amount/PreciseAmount here, since PlacementResult.Mult is
        /// derived purely from ScoreEvents.
        /// </summary>
        private static void TagNewEvents(List<ScoreEvent> events, int startIndex, ModifierId id, int modifierIndex, float levelFactor = 1f)
        {
            for (int i = startIndex; i < events.Count; i++)
            {
                events[i].TriggeringModifier = id;
                events[i].TriggeringModifierIndex = modifierIndex;
                if (levelFactor != 1f)
                {
                    events[i].Amount = Mathf.RoundToInt(events[i].Amount * levelFactor);
                    if (events[i].PreciseAmount.HasValue)
                    {
                        events[i].PreciseAmount = events[i].PreciseAmount.Value * levelFactor;
                    }
                }
            }
        }

        /// <summary>The scoring multiplier for modifier slot <paramref name="index"/> — 1f (no-op) when <paramref name="modifierLevels"/> is null or doesn't cover that index. See ModifierLevelUtility.</summary>
        private static float GetModifierLevelFactor(IReadOnlyList<int> modifierLevels, int index)
        {
            if (modifierLevels == null || index < 0 || index >= modifierLevels.Count)
            {
                return 1f;
            }
            return ModifierLevelUtility.LevelToFactor(modifierLevels[index]);
        }

        /// <summary>Prisme: xN multiplier (see ScoringConstants.PrismeMultiplier) when the placement (itself + its direct neighbors) touches 4 distinct non-joker colors (or 3 + a joker). Returns 1 (no-op) when it doesn't qualify.</summary>
        private int ApplyPrisme(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            var touching = CollectTouchingColors(placedCells);
            bool hasJoker = touching.Remove(PieceColor.Joker);

            bool qualifies = touching.Count >= ScoringConstants.PrismeMinDistinctColors
                || (touching.Count == ScoringConstants.PrismeMinDistinctColors - 1 && hasJoker);
            if (!qualifies)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.PrismeMultiplier));
            return ScoringConstants.PrismeMultiplier;
        }

        /// <summary>
        /// Every distinct color among the placement's own cells and every cell
        /// orthogonally adjacent to any of them — used by Prisme/Tricolore/
        /// Complémentaire, which measure color diversity around a placement
        /// rather than within its scored group, since a scored group can
        /// never contain more than one non-joker color (see FindConnectedGroup).
        /// </summary>
        private HashSet<PieceColor> CollectTouchingColors(List<Vector2Int> placedCells)
        {
            var colors = new HashSet<PieceColor>();
            for (int i = 0; i < placedCells.Count; i++)
            {
                var pos = placedCells[i];
                colors.Add(_cells[pos.x, pos.y].FilledColor.Value);
                AddColorIfFilled(colors, pos.x - 1, pos.y);
                AddColorIfFilled(colors, pos.x + 1, pos.y);
                AddColorIfFilled(colors, pos.x, pos.y - 1);
                AddColorIfFilled(colors, pos.x, pos.y + 1);
            }
            return colors;
        }

        private void AddColorIfFilled(HashSet<PieceColor> colors, int x, int y)
        {
            if (InBounds(x, y) && _cells[x, y].IsFilled && _cells[x, y].FilledColor.HasValue)
            {
                colors.Add(_cells[x, y].FilledColor.Value);
            }
        }

        private int ApplyChaine(List<Vector2Int> groupCells, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (groupCells.Count < ScoringConstants.ChaineMinGroupSize)
            {
                return 0;
            }

            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], ScoringConstants.ChaineBonus));
            return ScoringConstants.ChaineBonus;
        }

        private int ApplyMegaChaine(List<Vector2Int> groupCells, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (groupCells.Count < ScoringConstants.MegaChaineMinGroupSize)
            {
                return 0;
            }

            int extraCells = groupCells.Count - ScoringConstants.MegaChaineMinGroupSize;
            int bonus = ScoringConstants.MegaChaineBaseBonus + extraCells * ScoringConstants.MegaChaineBonusPerExtraCell;
            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], bonus));
            return bonus;
        }

        private int ApplyForteresse(List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            int total = 0;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var pos = groupCells[i];
                if (!AreAllNeighborsFilled(pos.x, pos.y, includeDiagonals: true))
                {
                    continue;
                }

                events.Add(new ScoreEvent(ScoreEventType.Modifier, pos, ScoringConstants.ForteresseBonusPerCell));
                total += ScoringConstants.ForteresseBonusPerCell;
            }
            return total;
        }

        private int ApplyPrisonnier(List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            int total = 0;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var pos = groupCells[i];
                if (!AreAllNeighborsFilled(pos.x, pos.y, includeDiagonals: false))
                {
                    continue;
                }

                events.Add(new ScoreEvent(ScoreEventType.Modifier, pos, ScoringConstants.PrisonnierBonusPerCell));
                total += ScoringConstants.PrisonnierBonusPerCell;
            }
            return total;
        }

        /// <summary>
        /// True if every one of a cell's neighbors is in-bounds and filled — an
        /// out-of-bounds neighbor always fails this, so edge/corner cells can
        /// never qualify for Forteresse/Prisonnier.
        /// </summary>
        private bool AreAllNeighborsFilled(int x, int y, bool includeDiagonals)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }
                    if (!includeDiagonals && dx != 0 && dy != 0)
                    {
                        continue;
                    }

                    int nx = x + dx;
                    int ny = y + dy;
                    if (!InBounds(nx, ny) || !_cells[nx, ny].IsFilled)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>Architecte: xN multiplier (see ScoringConstants.ArchitecteMultiplier) for placing a 2x2 square piece. Returns 1 (no-op) otherwise.</summary>
        private int ApplyArchitecte(PieceShape shape, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (shape.Id != ShapeId.Sq2)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.ArchitecteMultiplier));
            return ScoringConstants.ArchitecteMultiplier;
        }

        private int ApplyCollectionneur(ClearInfo clearInfo, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (clearInfo.ClearedCellColors.Count == 0)
            {
                return 0;
            }

            var distinctColors = new HashSet<PieceColor>();
            for (int i = 0; i < clearInfo.ClearedCellColors.Count; i++)
            {
                // A broken-free frozen plot had no piece, so it has no color to count.
                if (!clearInfo.ClearedCellFrozen[i])
                {
                    distinctColors.Add(clearInfo.ClearedCellColors[i]);
                }
            }
            if (distinctColors.Count == 0)
            {
                return 0;
            }

            int bonus = distinctColors.Count * ScoringConstants.CollectionneurBonusPerColor;
            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], bonus));
            return bonus;
        }

        /// <summary>Glowing Collector (CollectionneurLueur): Lueur (see EconomyConstants.CollectionneurLueurPerColor) per distinct color among this placement's cleared cells — the Lueur-earning sibling of Collectionneur, same distinct-color count, same trigger condition (no cleared cells this placement = 0, no event).</summary>
        private int ApplyCollectionneurLueur(ClearInfo clearInfo, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (clearInfo.ClearedCellColors.Count == 0)
            {
                return 0;
            }

            var distinctColors = new HashSet<PieceColor>();
            for (int i = 0; i < clearInfo.ClearedCellColors.Count; i++)
            {
                // A broken-free frozen plot had no piece, so it has no color to count.
                if (!clearInfo.ClearedCellFrozen[i])
                {
                    distinctColors.Add(clearInfo.ClearedCellColors[i]);
                }
            }
            if (distinctColors.Count == 0)
            {
                return 0;
            }

            int lueur = distinctColors.Count * EconomyConstants.CollectionneurLueurPerColor;
            events.Add(new ScoreEvent(ScoreEventType.LueurBonus, placedCells[0], lueur));
            return lueur;
        }

        /// <summary>Tricolore: xN multiplier (see ScoringConstants.TricoloreMultiplier) when the placement (itself + its direct neighbors) touches exactly TricoloreExactDistinctColors distinct non-joker colors. Returns 1 (no-op) otherwise.</summary>
        private int ApplyTricolore(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            var touching = CollectTouchingColors(placedCells);
            touching.Remove(PieceColor.Joker);

            if (touching.Count != ScoringConstants.TricoloreExactDistinctColors)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.TricoloreMultiplier));
            return ScoringConstants.TricoloreMultiplier;
        }

        /// <summary>Whether the given set of colors contains one of Companion Bloom's complementary pairs (false for a color set that can never form one).</summary>
        public static bool HasComplementaryPair(IReadOnlyList<PieceColor> colors)
        {
            for (int i = 0; i < ComplementaryPairs.Length; i++)
            {
                bool hasFirst = false;
                bool hasSecond = false;
                for (int c = 0; c < colors.Count; c++)
                {
                    hasFirst |= colors[c] == ComplementaryPairs[i][0];
                    hasSecond |= colors[c] == ComplementaryPairs[i][1];
                }
                if (hasFirst && hasSecond)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Arbitrary complementary pairing across the 4 base colors — not derived from a color wheel, just a fixed pairing for this modifier.</summary>
        private static readonly PieceColor[][] ComplementaryPairs =
        {
            new[] { PieceColor.Coral, PieceColor.Violet },
            new[] { PieceColor.Teal, PieceColor.Lime }
        };

        /// <summary>Complémentaire: xN multiplier (see ScoringConstants.ComplementaireMultiplier) when the placement (itself + its direct neighbors) touches both colors of a complementary pair. Returns 1 (no-op) otherwise.</summary>
        private int ApplyComplementaire(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            var present = CollectTouchingColors(placedCells);

            bool qualifies = false;
            for (int i = 0; i < ComplementaryPairs.Length; i++)
            {
                if (present.Contains(ComplementaryPairs[i][0]) && present.Contains(ComplementaryPairs[i][1]))
                {
                    qualifies = true;
                    break;
                }
            }

            if (!qualifies)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.ComplementaireMultiplier));
            return ScoringConstants.ComplementaireMultiplier;
        }

        /// <summary>Îlot: xN multiplier (see ScoringConstants.IlotMultiplier) when the placement's resulting group is a single isolated cell. Returns 1 (no-op) otherwise.</summary>
        private int ApplyIlot(List<Vector2Int> groupCells, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (groupCells.Count != 1)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.IlotMultiplier));
            return ScoringConstants.IlotMultiplier;
        }

        /// <summary>Pierre angulaire: xN multiplier (see ScoringConstants.CornerstoneMultiplier) when any placed cell sits on one of the grid's 4 corners. Returns 1 (no-op) otherwise.</summary>
        private int ApplyPierreAngulaire(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            int last = Size - 1;
            for (int i = 0; i < placedCells.Count; i++)
            {
                var pos = placedCells[i];
                if ((pos.x == 0 || pos.x == last) && (pos.y == 0 || pos.y == last))
                {
                    events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.CornerstoneMultiplier));
                    return ScoringConstants.CornerstoneMultiplier;
                }
            }
            return 1;
        }

        private int ApplyCouronne(List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            int total = 0;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var pos = groupCells[i];
                if (pos.x != 0 && pos.x != Size - 1 && pos.y != 0 && pos.y != Size - 1)
                {
                    continue;
                }

                events.Add(new ScoreEvent(ScoreEventType.Modifier, pos, ScoringConstants.CouronneBonusPerCell));
                total += ScoringConstants.CouronneBonusPerCell;
            }
            return total;
        }

        private int ApplyCarrefour(List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            int total = 0;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var pos = groupCells[i];
                if (!AreAllNeighborsFilled(pos.x, pos.y, includeDiagonals: false))
                {
                    continue;
                }
                if (!HasAtLeastTwoDistinctCardinalNeighborColorsDifferentFromOwn(pos.x, pos.y))
                {
                    continue;
                }

                events.Add(new ScoreEvent(ScoreEventType.Modifier, pos, ScoringConstants.CarrefourBonusPerCell));
                total += ScoringConstants.CarrefourBonusPerCell;
            }
            return total;
        }

        /// <summary>
        /// Assumes all 4 cardinal neighbors are already known filled (see
        /// <see cref="AreAllNeighborsFilled"/>), so each has a non-null
        /// <see cref="Cell.FilledColor"/>. Requires at least 2 distinct neighbor
        /// colors that ALSO differ from the cell's own color — a neighbor sharing
        /// the cell's own color doesn't count toward the "crossroads" of
        /// different colors.
        /// </summary>
        private bool HasAtLeastTwoDistinctCardinalNeighborColorsDifferentFromOwn(int x, int y)
        {
            var ownColor = _cells[x, y].FilledColor.Value;
            var colors = new HashSet<PieceColor>();
            AddIfDifferentFromOwn(colors, _cells[x - 1, y].FilledColor.Value, ownColor);
            AddIfDifferentFromOwn(colors, _cells[x + 1, y].FilledColor.Value, ownColor);
            AddIfDifferentFromOwn(colors, _cells[x, y - 1].FilledColor.Value, ownColor);
            AddIfDifferentFromOwn(colors, _cells[x, y + 1].FilledColor.Value, ownColor);
            return colors.Count >= 2;
        }

        private static void AddIfDifferentFromOwn(HashSet<PieceColor> colors, PieceColor neighborColor, PieceColor ownColor)
        {
            if (neighborColor != ownColor)
            {
                colors.Add(neighborColor);
            }
        }

        private int ApplyCercleChromatique(List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            int total = 0;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var pos = groupCells[i];
                if (!AreAllNeighborsFilled(pos.x, pos.y, includeDiagonals: false))
                {
                    continue;
                }
                if (!CardinalNeighborsCoverAllBaseColors(pos.x, pos.y))
                {
                    continue;
                }

                events.Add(new ScoreEvent(ScoreEventType.Modifier, pos, ScoringConstants.CercleChromatiqueBonusPerCell));
                total += ScoringConstants.CercleChromatiqueBonusPerCell;
            }
            return total;
        }

        /// <summary>Assumes all 4 cardinal neighbors are already known filled (see <see cref="AreAllNeighborsFilled"/>). A joker neighbor consumes one of the 4 slots without contributing a base color, so it can never complete the wheel on its own.</summary>
        private bool CardinalNeighborsCoverAllBaseColors(int x, int y)
        {
            var colors = new HashSet<PieceColor>
            {
                _cells[x - 1, y].FilledColor.Value,
                _cells[x + 1, y].FilledColor.Value,
                _cells[x, y - 1].FilledColor.Value,
                _cells[x, y + 1].FilledColor.Value
            };
            colors.Remove(PieceColor.Joker);
            return colors.Count == PieceColorUtility.BaseColors.Count;
        }

        /// <summary>The group must be a single real color with ZERO jokers anywhere in it (jokers not merely ignored, disqualifying).</summary>
        private int ApplyMonochrome(List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            PieceColor? monoColor = null;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var color = _cells[groupCells[i].x, groupCells[i].y].FilledColor.Value;
                if (color == PieceColor.Joker)
                {
                    return 0;
                }
                if (!monoColor.HasValue)
                {
                    monoColor = color;
                }
                else if (monoColor.Value != color)
                {
                    return 0;
                }
            }

            int total = 0;
            for (int i = 0; i < groupCells.Count; i++)
            {
                events.Add(new ScoreEvent(ScoreEventType.Modifier, groupCells[i], ScoringConstants.MonochromeBonusPerCell));
                total += ScoringConstants.MonochromeBonusPerCell;
            }
            return total;
        }

        private int ApplyContraste(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            int total = 0;
            for (int i = 0; i < placedCells.Count; i++)
            {
                var pos = placedCells[i];
                var ownColor = _cells[pos.x, pos.y].FilledColor.Value;
                if (!TryFindContrastingNeighbor(pos.x, pos.y, ownColor, out Vector2Int neighbor))
                {
                    continue;
                }

                // ReferencedPosition lets RunManager.ApplyPoisonScoreRule
                // also flip this event negative when the CONTRASTING
                // NEIGHBOR itself is poisoned, not just pos — see
                // ScoreEvent.ReferencedPosition's own doc comment.
                events.Add(new ScoreEvent(ScoreEventType.Modifier, pos, ScoringConstants.ContrasteBonusPerCell) { ReferencedPosition = neighbor });
                total += ScoringConstants.ContrasteBonusPerCell;
            }
            return total;
        }

        /// <summary>Same 4-neighbor check HasContrastingNeighbor used to do alone, now also reporting back WHICH neighbor qualified (the first found, in a fixed left/right/down/up order) — needed so ApplyContraste can tag its ScoreEvent with ReferencedPosition.</summary>
        private bool TryFindContrastingNeighbor(int x, int y, PieceColor ownColor, out Vector2Int neighbor)
        {
            if (IsFilledWithDifferentColor(x - 1, y, ownColor)) { neighbor = new Vector2Int(x - 1, y); return true; }
            if (IsFilledWithDifferentColor(x + 1, y, ownColor)) { neighbor = new Vector2Int(x + 1, y); return true; }
            if (IsFilledWithDifferentColor(x, y - 1, ownColor)) { neighbor = new Vector2Int(x, y - 1); return true; }
            if (IsFilledWithDifferentColor(x, y + 1, ownColor)) { neighbor = new Vector2Int(x, y + 1); return true; }
            neighbor = default;
            return false;
        }

        private bool IsFilledWithDifferentColor(int x, int y, PieceColor ownColor)
        {
            return InBounds(x, y) && _cells[x, y].IsFilled && _cells[x, y].FilledColor.HasValue && _cells[x, y].FilledColor.Value != ownColor;
        }

        /// <summary>Dégradé (Momentum): xN multiplier (see ScoringConstants.DegradeMultiplier) whenever this placement's scored group is strictly larger than the previous placement's this round. Returns 1 (no-op) otherwise.</summary>
        private int ApplyDegrade(int currentGroupSize, int? previousGroupSize, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (!previousGroupSize.HasValue || currentGroupSize <= previousGroupSize.Value)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.DegradeMultiplier));
            return ScoringConstants.DegradeMultiplier;
        }

        private int ApplyEmmitouflee(List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            int total = 0;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var pos = groupCells[i];
                if (!AreAllDiagonalNeighborsFilled(pos.x, pos.y))
                {
                    continue;
                }

                events.Add(new ScoreEvent(ScoreEventType.Modifier, pos, ScoringConstants.EmmitoufleeBonusPerCell));
                total += ScoringConstants.EmmitoufleeBonusPerCell;
            }
            return total;
        }

        private bool AreAllDiagonalNeighborsFilled(int x, int y)
        {
            return IsInBoundsAndFilled(x - 1, y - 1) && IsInBoundsAndFilled(x + 1, y - 1)
                && IsInBoundsAndFilled(x - 1, y + 1) && IsInBoundsAndFilled(x + 1, y + 1);
        }

        private bool IsInBoundsAndFilled(int x, int y)
        {
            return InBounds(x, y) && _cells[x, y].IsFilled;
        }

        private int ApplyJardinier(List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            int total = 0;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var pos = groupCells[i];
                if (!HasAnyModifierNeighbor(pos.x, pos.y))
                {
                    continue;
                }

                events.Add(new ScoreEvent(ScoreEventType.Modifier, pos, ScoringConstants.JardinierBonusPerCell));
                total += ScoringConstants.JardinierBonusPerCell;
            }
            return total;
        }

        private bool HasAnyModifierNeighbor(int x, int y)
        {
            return HasModifierAt(x - 1, y) || HasModifierAt(x + 1, y) || HasModifierAt(x, y - 1) || HasModifierAt(x, y + 1);
        }

        private bool HasModifierAt(int x, int y)
        {
            return InBounds(x, y) && _cells[x, y].HasAnyModifier;
        }

        /// <summary>Maçon: flat +Mult (additive, see PlacementResult.AdditiveMultBonus, see ScoringConstants.MaconBonus) for a placement that clears no line/column at all. Returns 0 (no-op) otherwise.</summary>
        private int ApplyMaconBonus(ClearInfo clearInfo, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (clearInfo.ClearedCells.Count > 0)
            {
                return 0;
            }

            events.Add(new ScoreEvent(ScoreEventType.MultBonus, placedCells[0], ScoringConstants.MaconBonus));
            return ScoringConstants.MaconBonus;
        }

        /// <summary>Démolisseur: xN multiplier (see ScoringConstants.DemolisseurMultiplierPerLine) PER simultaneously-cleared line, once at least DemolisseurMinLines rows/columns clear at once — stacks multiplicatively (3 lines at once is xN*xN*xN). Returns 1 (no-op) otherwise.</summary>
        private int ApplyDemolisseur(ClearInfo clearInfo, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (clearInfo.ClearedLineCount < ScoringConstants.DemolisseurMinLines)
            {
                return 1;
            }

            int multiplier = 1;
            for (int i = 0; i < clearInfo.ClearedLineCount; i++)
            {
                multiplier *= ScoringConstants.DemolisseurMultiplierPerLine;
            }
            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], multiplier));
            return multiplier;
        }

        /// <summary>Shared driver for the line-pattern modifiers that need a per-line yes/no predicate over their ordered color sequence — xN multiplier fires once per qualifying cleared line, stacking multiplicatively, reset every placement. Gradient is handled separately (see <see cref="ApplyGradient"/>).</summary>
        private int ApplyPerLineMultiplier(ClearInfo clearInfo, List<Vector2Int> placedCells, List<ScoreEvent> events, System.Func<IReadOnlyList<PieceColor>, bool> predicate, int multiplierPerLine)
        {
            int multiplier = 1;
            var lines = clearInfo.ClearedLines;
            for (int i = 0; i < lines.Count; i++)
            {
                if (!predicate(lines[i].Colors))
                {
                    continue;
                }

                events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], multiplierPerLine));
                multiplier *= multiplierPerLine;
            }
            return multiplier;
        }

        /// <summary>Shared driver for the 3 Lueur-earning line-pattern modifiers (ArcEnCielLueur/AlternanceLueur/MonochromeLigneLueur) — same idea as ApplyPerLineMultiplier above, reusing the exact same predicates, but adds flat Lueur per qualifying cleared line instead of multiplying a score factor.</summary>
        private int ApplyPerLineLueur(ClearInfo clearInfo, List<Vector2Int> placedCells, List<ScoreEvent> events, System.Func<IReadOnlyList<PieceColor>, bool> predicate, int lueurPerLine)
        {
            int total = 0;
            var lines = clearInfo.ClearedLines;
            for (int i = 0; i < lines.Count; i++)
            {
                if (!predicate(lines[i].Colors))
                {
                    continue;
                }

                events.Add(new ScoreEvent(ScoreEventType.LueurBonus, placedCells[0], lueurPerLine));
                total += lueurPerLine;
            }
            return total;
        }

        /// <summary>
        /// Gradient never resets, unlike its line-pattern siblings above.
        /// Every cleared row/column satisfying <see cref="IsGradientLine"/>
        /// permanently increments <see cref="_gradientPermanentBonus"/> by 1.
        /// The returned multiplier is always (1 + that counter), applied to
        /// this placement immediately and to every placement for the rest of
        /// the run. Returns 1 only while the counter is still 0.
        /// </summary>
        private int ApplyGradient(ClearInfo clearInfo, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            var lines = clearInfo.ClearedLines;
            for (int i = 0; i < lines.Count; i++)
            {
                if (IsGradientLine(lines[i].Colors))
                {
                    _gradientPermanentBonus++;
                }
            }

            int multiplier = 1 + _gradientPermanentBonus;
            if (multiplier <= 1)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], multiplier));
            return multiplier;
        }

        /// <summary>Non-joker colors present, ignoring how many times each repeats.</summary>
        private static bool ContainsAllBaseColors(IReadOnlyList<PieceColor> colors)
        {
            var seen = new HashSet<PieceColor>();
            for (int i = 0; i < colors.Count; i++)
            {
                if (colors[i] != PieceColor.Joker)
                {
                    seen.Add(colors[i]);
                }
            }
            return seen.Count >= PieceColorUtility.BaseColors.Count;
        }

        /// <summary>Exactly 2 distinct colors present, AND every adjacent pair differs (ABAB...). A joker anywhere breaks the strict adjacency check, so it never qualifies.</summary>
        private static bool IsAlternatingTwoColors(IReadOnlyList<PieceColor> colors)
        {
            if (colors.Count < 2)
            {
                return false;
            }
            if (new HashSet<PieceColor>(colors).Count != 2)
            {
                return false;
            }
            for (int i = 1; i < colors.Count; i++)
            {
                if (colors[i] == colors[i - 1])
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsPalindrome(IReadOnlyList<PieceColor> colors)
        {
            if (colors.Count < 2)
            {
                return false;
            }
            for (int i = 0, j = colors.Count - 1; i < j; i++, j--)
            {
                if (colors[i] != colors[j])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>No two ADJACENT cells share a color — a weaker, more general condition than <see cref="IsAlternatingTwoColors"/> (which additionally caps the line at exactly 2 distinct colors), so a 3+ color cycling line can satisfy Gradient without satisfying Alternance.</summary>
        private static bool IsGradientLine(IReadOnlyList<PieceColor> colors)
        {
            if (colors.Count < 2)
            {
                return false;
            }
            for (int i = 1; i < colors.Count; i++)
            {
                if (colors[i] == colors[i - 1])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Every cell shares its color with at least one immediate neighbor in the line — no isolated single-cell color anywhere.</summary>
        private static bool IsAllBlocksOfAtLeastTwo(IReadOnlyList<PieceColor> colors)
        {
            if (colors.Count < 2)
            {
                return false;
            }
            for (int i = 0; i < colors.Count; i++)
            {
                bool matchesLeft = i > 0 && colors[i - 1] == colors[i];
                bool matchesRight = i < colors.Count - 1 && colors[i + 1] == colors[i];
                if (!matchesLeft && !matchesRight)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Every non-joker color in the line is the same one (jokers ignored).</summary>
        private static bool IsMonochromeLine(IReadOnlyList<PieceColor> colors)
        {
            if (colors.Count == 0)
            {
                return false;
            }
            PieceColor? mono = null;
            for (int i = 0; i < colors.Count; i++)
            {
                if (colors[i] == PieceColor.Joker)
                {
                    continue;
                }
                if (!mono.HasValue)
                {
                    mono = colors[i];
                }
                else if (mono.Value != colors[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
