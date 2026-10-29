using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>Fertilizers resolved before lines are cleared (group, neighbor, color and format effects) and the context they share.</summary>
    public sealed partial class GridManager
    {
        private delegate int PreClearModifierEffect(PreClearModifierContext ctx);

        /// <summary>One entry per pre-clear-evaluable modifier, built per instance since most Apply* helpers read grid state via <see cref="_cells"/>. A modifier missing from this table silently contributes 0.</summary>
        private Dictionary<ModifierId, PreClearModifierEffect> BuildPreClearEffects()
        {
            return new Dictionary<ModifierId, PreClearModifierEffect>
            {
                { ModifierId.Haie, ctx => PointsToMult(ctx, () => ApplyHaie(ctx.PlacedCells, ctx.Events)) },
                { ModifierId.Coeur, ctx => { ctx.Multiplier *= ApplyCoeur(ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Treillis, ctx => { ctx.AdditiveMult += ApplyDirectionalPiece(ctx.PlacedCells, true, ScoringConstants.TrellisMultBonus, ctx.Events); return 0; } },
                { ModifierId.Sillon, ctx => { ctx.AdditiveMult += ApplyDirectionalPiece(ctx.PlacedCells, false, ScoringConstants.FurrowMultBonus, ctx.Events); return 0; } },
                { ModifierId.Haricot, ctx => PointsToMult(ctx, () => ApplyHaricot(ctx.PlacedCells, ctx.Events)) },
                { ModifierId.Patchwork, ctx => PointsToMult(ctx, () => ApplyPatchwork(ctx.PlacedCells, ctx.Events)) },
                { ModifierId.Racines, ctx => { ctx.AdditiveMult += ApplyRacines(ctx.GroupCells, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Emboite, ctx => { ctx.AdditiveMult += ApplyEmboite(ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Mosaique, ctx => { ctx.AdditiveMult += ApplyMosaique(ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Foule, ctx => { ctx.Multiplier *= ApplyFoule(ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Prairie, ctx => { ctx.AdditiveMult += ApplyPrairie(ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Guirlande, ctx => PointsToMult(ctx, () => ApplyGuirlande(ctx.PlacedCells, ctx.Events)) },
                { ModifierId.Pollinisateur, ctx => { ctx.Lueur += ApplyPollinisateur(ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Miel, ctx => { ctx.Lueur += ApplyMiel(ctx.GroupCells, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Nectar, ctx => { ctx.Lueur += ApplyNectar(ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.PierreAngulaire, ctx => { ctx.Multiplier *= ApplyPierreAngulaire(ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Prisme, ctx => { ctx.Multiplier *= ApplyPrisme(ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Chaine, ctx => PointsToMult(ctx, () => ApplyChaine(ctx.GroupCells, ctx.PlacedCells, ctx.Events)) },
                { ModifierId.MegaChaine, ctx => PointsToMult(ctx, () => ApplyMegaChaine(ctx.GroupCells, ctx.PlacedCells, ctx.Events)) },
                { ModifierId.Forteresse, ctx => PointsToMult(ctx, () => ApplyForteresse(ctx.GroupCells, ctx.Events)) },
                { ModifierId.Prisonnier, ctx => PointsToMult(ctx, () => ApplyPrisonnier(ctx.GroupCells, ctx.Events)) },
                { ModifierId.Architecte, ctx => { ctx.Multiplier *= ApplyArchitecte(ctx.Shape, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Tricolore, ctx => { ctx.Multiplier *= ApplyTricolore(ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Complementaire, ctx => { ctx.Multiplier *= ApplyComplementaire(ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Ilot, ctx => { ctx.Multiplier *= ApplyIlot(ctx.GroupCells, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Couronne, ctx => PointsToMult(ctx, () => ApplyCouronne(ctx.GroupCells, ctx.Events)) },
                { ModifierId.Carrefour, ctx => PointsToMult(ctx, () => ApplyCarrefour(ctx.GroupCells, ctx.Events)) },
                { ModifierId.CercleChromatique, ctx => PointsToMult(ctx, () => ApplyCercleChromatique(ctx.GroupCells, ctx.Events)) },
                { ModifierId.Monochrome, ctx => { ApplyMonochrome(ctx.GroupCells, ctx.Events); return 0; } },
                { ModifierId.Contraste, ctx => PointsToMult(ctx, () => ApplyContraste(ctx.PlacedCells, ctx.Events)) },
                { ModifierId.Degrade, ctx => { ctx.Multiplier *= ApplyDegrade(ctx.GroupCells.Count, ctx.PreviousGroupSize, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Emmitouflee, ctx => PointsToMult(ctx, () => ApplyEmmitouflee(ctx.GroupCells, ctx.Events)) },
                { ModifierId.Jardinier, ctx => PointsToMult(ctx, () => ApplyJardinier(ctx.GroupCells, ctx.Events)) },
                { ModifierId.DevotionCoral, ctx => { ctx.AdditiveMult += ApplyColorDevotionBonus(PieceColor.Coral, ctx.JokerResolvedColor, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.DevotionTeal, ctx => { ctx.AdditiveMult += ApplyColorDevotionBonus(PieceColor.Teal, ctx.JokerResolvedColor, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.DevotionViolet, ctx => { ctx.AdditiveMult += ApplyColorDevotionBonus(PieceColor.Violet, ctx.JokerResolvedColor, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.DevotionLime, ctx => { ctx.AdditiveMult += ApplyColorDevotionBonus(PieceColor.Lime, ctx.JokerResolvedColor, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.FormatPetitSpecialiste, ctx => { ctx.Multiplier *= ApplyFormatSpecialistMultiplier(1, ScoringConstants.FormatPetitMaxCells, ctx.Shape, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.FormatMoyenSpecialiste, ctx => { ctx.Multiplier *= ApplyFormatSpecialistMultiplier(ScoringConstants.FormatMoyenCells, ScoringConstants.FormatMoyenCells, ctx.Shape, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.FormatGrandSpecialiste, ctx => { ctx.Multiplier *= ApplyFormatSpecialistMultiplier(ScoringConstants.FormatGrandMinCells, int.MaxValue, ctx.Shape, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.FormatPetitGlow, ctx => PointsToMult(ctx, () => ApplyFormatGlow(1, ScoringConstants.FormatPetitMaxCells, ctx.Shape, ctx.PlacedCells, ctx.GroupCells, ctx.Events)) },
                { ModifierId.FormatMoyenGlow, ctx => PointsToMult(ctx, () => ApplyFormatGlow(ScoringConstants.FormatMoyenCells, ScoringConstants.FormatMoyenCells, ctx.Shape, ctx.PlacedCells, ctx.GroupCells, ctx.Events)) },
                { ModifierId.FormatGrandGlow, ctx => PointsToMult(ctx, () => ApplyFormatGlow(ScoringConstants.FormatGrandMinCells, int.MaxValue, ctx.Shape, ctx.PlacedCells, ctx.GroupCells, ctx.Events)) },
                { ModifierId.GrandFormat, ctx => PointsToMult(ctx, () => ApplyGrandFormat(ctx.PlacedCells, ctx.Events)) },
                { ModifierId.HorsNorme, ctx => PointsToMult(ctx, () => ApplyHorsNorme(ctx.PlacedCells, ctx.Events)) },
                { ModifierId.Diagonale, ctx => PointsToMult(ctx, () => ApplyDiagonale(ctx.GroupCells, ctx.Events)) },
                { ModifierId.Nid, ctx => PointsToMult(ctx, () => ApplyNid(ctx.GroupCells, ctx.Events)) },
                { ModifierId.Solitaire, ctx => { ctx.Multiplier *= ApplySolitaire(ctx.GroupCells, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.PetitFormat, ctx => PointsToMult(ctx, () => ApplyPetitFormat(ctx.PlacedCells, ctx.Events)) },
                { ModifierId.Fraicheur, ctx => { ctx.Multiplier *= ApplyFraicheur(ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Pont, ctx => { ctx.Multiplier *= ApplyPont(ctx.OwnColor, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Encerclement, ctx => PointsToMult(ctx, () => ApplyEncerclement(ctx.GroupCells, ctx.Events)) },
                { ModifierId.Boucher, ctx => PointsToMult(ctx, () => ApplyBoucher(ctx.PlacedCells, ctx.Events)) },
                { ModifierId.GrosseFamille, ctx => { ctx.Multiplier *= ApplyGrosseFamille(ctx.GroupCells, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Repetition, ctx => { ctx.Multiplier *= ApplyRepetition(ctx.RepetitionStreak, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.RepetitionLueur, ctx => { ctx.Lueur += ApplyRepetitionLueur(ctx.RepetitionStreak, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.AlternancePieces, ctx => { ctx.AdditiveMult += ApplyAlternancePieces(ctx.OwnColor, ctx.PreviousPlacedColor, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Precision, ctx => PointsToMult(ctx, () => ApplyPrecision(ctx.PlacedCells, ctx.Events)) },
                { ModifierId.Surpopulation, ctx => PointsToMult(ctx, () => ApplySurpopulation(ctx.PlacedCells, ctx.Events)) },
                { ModifierId.Minimaliste, ctx => { ctx.Multiplier *= ApplyMinimaliste(ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Pair, ctx => { ctx.Multiplier *= ApplyParitePaire(ctx.GroupCells, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Impair, ctx => { ctx.Multiplier *= ApplyPariteImpaire(ctx.GroupCells, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.CharmedPatch, ctx => { ctx.AdditiveMult += ApplyCharmedPatch(ctx.GroupCells, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Grafter, ctx => { ctx.AdditiveMult += ApplyGrafter(ctx.PlacedCells, ctx.Events, 1, ScoringConstants.GrafterMultBonus); return 0; } },
                { ModifierId.TwinGraft, ctx => { ctx.AdditiveMult += ApplyGrafter(ctx.PlacedCells, ctx.Events, 2, ScoringConstants.TwinGraftMultBonus); return 0; } },
                { ModifierId.Botanist, ctx => { ctx.AdditiveMult += ApplyBotanist(ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Hothouse, ctx => { ctx.AdditiveMult += ApplyHothouse(ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.GoodNeighbors, ctx => { ctx.AdditiveMult += ApplyGoodNeighbors(ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Polyvalence, ctx => { ctx.AdditiveMult += ApplyPolyvalence(ctx.ActiveModifiers, ctx.PlacedCells, ctx.Events); return 0; } },
                // Renfort Joker/Arsenal/Siphon are resolved in RunManager
                // instead (enemy damage/Lueur scaling, or deck scanning that
                // GridManager has no reference for).
                { ModifierId.RenfortJoker, ctx => 0 },
                { ModifierId.Arsenal, ctx => 0 },
                { ModifierId.Siphon, ctx => 0 },
                { ModifierId.CollectionChromatique, ctx => { ctx.AdditiveMult += ApplyCollectionChromatique(ctx.ActiveModifiers, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Cadence, ctx => { ctx.Multiplier *= ApplyCadence(ctx.ActiveModifiers, ctx.Shape, ctx.GroupCells, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Echo, ctx => ApplyEcho(ctx) },
                // Joker: a passive rule change resolved before the loop starts
                // (see JokerResolvedColor), no score of its own.
                { ModifierId.Joker, ctx => 0 },
                // Combo is resolved separately as PlacementResult.ComboMultiplier
                // (see ComputeComboMultiplier).
                { ModifierId.Combo, ctx => 0 },
                { ModifierId.MultUn, ctx => { ctx.AdditiveMult += ApplyFlatAdditiveMult(ScoringConstants.MultUnBonus, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.MultDeux, ctx => { ctx.AdditiveMult += ApplyFlatAdditiveMult(ScoringConstants.MultDeuxBonus, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.MultQuatre, ctx => { ctx.AdditiveMult += ApplyFlatAdditiveMult(ScoringConstants.MultQuatreBonus, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.MultCinqRisque, ctx => { ctx.AdditiveMult += ApplyFlatAdditiveMult(ScoringConstants.MultCinqRisqueBonus, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Solidarite, ctx => { ctx.AdditiveMult += ApplySolidarite(ctx.ActiveModifierCount, ctx.PlacedCells, ctx.Events); return 0; } },
                { ModifierId.Epuisement, ctx => PointsToMult(ctx, () => ApplyEpuisement(ctx.PlacedCells, ctx.Events)) },
                // Copieur is never actually held — buying it adds another copy
                // of whichever modifier was purchased right before it (see
                // RunManager.BuyBlisterSlot), so this entry is never looked up.
                { ModifierId.Copieur, ctx => 0 }
            };
        }

        /// <summary>
        /// Fertilizers are Mult, never points: this runs one of the older point-scoring effects (kept as is — same conditions, same per-cell counting)
        /// and turns every point event it produced into a +Mult event of <see cref="ScoringConstants.PointsToMult"/> size, added to the additive Mult pool.
        /// </summary>
        private int PointsToMult(PreClearModifierContext ctx, System.Func<int> effect)
        {
            int before = ctx.Events.Count;
            effect();
            ctx.AdditiveMult += ConvertPointEventsToMult(ctx.Events, before);
            return 0;
        }

        private int PointsToMult(PostClearModifierContext ctx, System.Func<int> effect)
        {
            int before = ctx.Events.Count;
            effect();
            ctx.AdditiveMult += ConvertPointEventsToMult(ctx.Events, before);
            return 0;
        }

        /// <summary>Rewrites the point events from <paramref name="fromIndex"/> on as +Mult events; returns the Mult they add up to.</summary>
        private static int ConvertPointEventsToMult(List<ScoreEvent> events, int fromIndex)
        {
            int added = 0;
            for (int i = fromIndex; i < events.Count; i++)
            {
                if (events[i].Type != ScoreEventType.Modifier)
                {
                    continue;
                }
                int mult = ScoringConstants.PointsToMult(events[i].Amount);
                var converted = new ScoreEvent(ScoreEventType.MultBonus, events[i].Position, mult);
                converted.ReferencedPosition = events[i].ReferencedPosition;
                events[i] = converted;
                added += mult;
            }
            return added;
        }

        private int ApplyPreClearModifiers(IReadOnlyList<ModifierId> activeModifiers, PieceShape shape, List<Vector2Int> groupCells, List<Vector2Int> placedCells, int groupBonus, List<ScoreEvent> events, int? previousGroupSize, int repetitionStreak, PieceColor? previousPlacedColor, out int modifierMultiplier, out int lueurBonus, out int additiveMultBonus, IReadOnlyList<int> modifierLevels = null)
        {
            var ownColor = _cells[placedCells[0].x, placedCells[0].y].FilledColor.Value;
            // A Joker piece's own cell(s) stay PieceColor.Joker in storage
            // (see FindConnectedGroup), so every color-conditional modifier
            // below normally never matches it. When the Joker modifier is
            // held, this resolves to whichever base color would score the
            // most from the Devotion modifiers currently active, and
            // ApplyColorDevotionBonus uses this instead of the cell.
            var jokerResolvedColor = ResolveJokerColorForModifiers(ownColor, activeModifiers, groupBonus, groupCells.Count);

            var ctx = new PreClearModifierContext
            {
                Shape = shape,
                GroupCells = groupCells,
                PlacedCells = placedCells,
                Events = events,
                PreviousGroupSize = previousGroupSize,
                RepetitionStreak = repetitionStreak,
                PreviousPlacedColor = previousPlacedColor,
                OwnColor = ownColor,
                JokerResolvedColor = jokerResolvedColor,
                ActiveModifierCount = activeModifiers.Count,
                ActiveModifiers = activeModifiers
            };

            int total = 0;
            for (int i = 0; i < activeModifiers.Count; i++)
            {
                var id = activeModifiers[i];
                int eventsBefore = events.Count;
                int lueurBefore = ctx.Lueur;
                ctx.CurrentIndex = i;
                int bonus = _preClearEffects.TryGetValue(id, out var effect) ? effect(ctx) : 0;

                // Rescales this modifier's own contribution only, by diffing
                // ctx.Lueur before/after this one call. ctx.Multiplier/
                // ctx.AdditiveMult are left untouched: PlacementResult.Mult
                // reads ScoreEvents exclusively, and TagNewEvents' event-level
                // rescale below is sufficient for every xN/+Mult modifier.
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
            additiveMultBonus = ctx.AdditiveMult;
            return total;
        }

        /// <summary>Devotion (per-color): flat +Mult (see PlacementResult.AdditiveMultBonus) when the placement's fill color matches <paramref name="targetColor"/>. <paramref name="ownColor"/> may be Joker-resolved to a different color first (see ResolveJokerColorForModifiers).</summary>
        private static int ApplyColorDevotionBonus(PieceColor targetColor, PieceColor ownColor, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (ownColor != targetColor)
            {
                return 0;
            }

            events.Add(new ScoreEvent(ScoreEventType.MultBonus, placedCells[0], ScoringConstants.DevotionBonus));
            return ScoringConstants.DevotionBonus;
        }

        private bool HasUpgradedTile(int x, int y)
        {
            return InBounds(x, y) && _cells[x, y].IsFilled && _cells[x, y].OriginTrait.HasValue;
        }

        /// <summary>Charmed Patch: +Mult per upgraded tile of the scored group (a Wildroot tile's extra copy counts too).</summary>
        private int ApplyCharmedPatch(List<Vector2Int> groupCells, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            int upgraded = 0;
            for (int i = 0; i < groupCells.Count; i++)
            {
                if (HasUpgradedTile(groupCells[i].x, groupCells[i].y))
                {
                    upgraded++;
                }
            }
            return upgraded == 0 ? 0 : AddMultAndReturn(events, placedCells[0], upgraded * ScoringConstants.CharmedPatchMultPerTile);
        }

        /// <summary>Grafter / Twin Graft: a flat +Mult when the planted seed carries at least <paramref name="minUpgrades"/> upgrades (one per upgraded tile).</summary>
        private int ApplyGrafter(List<Vector2Int> placedCells, List<ScoreEvent> events, int minUpgrades, int bonus)
        {
            int upgraded = 0;
            for (int i = 0; i < placedCells.Count; i++)
            {
                if (HasUpgradedTile(placedCells[i].x, placedCells[i].y))
                {
                    upgraded++;
                }
            }
            return upgraded >= minUpgrades ? AddMultAndReturn(events, placedCells[0], bonus) : 0;
        }

        /// <summary>Botanist: +Mult per different kind of upgrade anywhere in the garden.</summary>
        private int ApplyBotanist(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            var kinds = new HashSet<PieceTraitKind>();
            foreach (var pos in AllPositions())
            {
                var cell = _cells[pos.x, pos.y];
                if (cell.IsFilled && cell.OriginTrait.HasValue)
                {
                    kinds.Add(cell.OriginTrait.Value.Kind);
                }
            }
            return kinds.Count == 0 ? 0 : AddMultAndReturn(events, placedCells[0], kinds.Count * ScoringConstants.BotanistMultPerKind);
        }

        /// <summary>Hothouse: +1 Mult per <see cref="ScoringConstants.HothouseTilesPerMult"/> upgraded tiles in the garden.</summary>
        private int ApplyHothouse(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            int upgraded = 0;
            foreach (var pos in AllPositions())
            {
                if (HasUpgradedTile(pos.x, pos.y))
                {
                    upgraded++;
                }
            }
            int mult = upgraded / ScoringConstants.HothouseTilesPerMult;
            return mult == 0 ? 0 : AddMultAndReturn(events, placedCells[0], mult);
        }

        /// <summary>Good Neighbors: +Mult per upgraded tile touching the planted seed (the seed's own tiles do not count).</summary>
        private int ApplyGoodNeighbors(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            var placed = new HashSet<Vector2Int>(placedCells);
            var counted = new HashSet<Vector2Int>();
            var steps = new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
            for (int i = 0; i < placedCells.Count; i++)
            {
                for (int s = 0; s < steps.Length; s++)
                {
                    var n = placedCells[i] + steps[s];
                    if (!placed.Contains(n) && HasUpgradedTile(n.x, n.y))
                    {
                        counted.Add(n);
                    }
                }
            }
            return counted.Count == 0 ? 0 : AddMultAndReturn(events, placedCells[0], counted.Count * ScoringConstants.GoodNeighborsMultPerTile);
        }

        private int AddMultAndReturn(List<ScoreEvent> events, Vector2Int at, int amount)
        {
            AddMult(events, at, amount);
            return amount;
        }

        /// <summary>Polyvalence: +N Mult, N = the number of distinct ModifierCategory values among currently held modifiers (this one's own category included).</summary>
        private static int ApplyPolyvalence(IReadOnlyList<ModifierId> activeModifiers, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            var categoriesSeen = new HashSet<ModifierCategory>();
            for (int i = 0; i < activeModifiers.Count; i++)
            {
                // A blighted slot arrives as the effect-less ModifierId.Blighted placeholder (not in the catalog): it counts for nothing.
                var definition = ModifierCatalog.Get(activeModifiers[i]);
                if (definition != null)
                {
                    categoriesSeen.Add(definition.Category);
                }
            }

            int bonus = categoriesSeen.Count * ScoringConstants.PolyvalenceMultPerCategory;
            events.Add(new ScoreEvent(ScoreEventType.MultBonus, placedCells[0], bonus));
            return bonus;
        }

        /// <summary>Collection Chromatique: +N Mult, N = how many of the 4 base colors have their Devotion modifier currently held.</summary>
        private static int ApplyCollectionChromatique(IReadOnlyList<ModifierId> activeModifiers, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            int completePairs = 0;
            var baseColors = PieceColorUtility.BaseColors;
            for (int i = 0; i < baseColors.Count; i++)
            {
                if (ContainsModifier(activeModifiers, DevotionModifierFor(baseColors[i])))
                {
                    completePairs++;
                }
            }

            int bonus = completePairs * ScoringConstants.CollectionChromatiqueMultPerCompletePair;
            events.Add(new ScoreEvent(ScoreEventType.MultBonus, placedCells[0], bonus));
            return bonus;
        }

        /// <summary>Cadence: xN multiplier when this placement satisfies both a held parity condition (Pair wants an even scored group, Impair an odd one) and a held Format* Specialist tier (keyed on the placed piece's own cell count). Returns 1 unless both sides match.</summary>
        private static int ApplyCadence(IReadOnlyList<ModifierId> activeModifiers, PieceShape shape, List<Vector2Int> groupCells, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            bool parityMatches =
                (ContainsModifier(activeModifiers, ModifierId.Pair) && groupCells.Count % 2 == 0) ||
                (ContainsModifier(activeModifiers, ModifierId.Impair) && groupCells.Count % 2 != 0);
            if (!parityMatches)
            {
                return 1;
            }

            int cellCount = shape.Cells.Count;
            bool formatMatches =
                (ContainsModifier(activeModifiers, ModifierId.FormatPetitSpecialiste) && cellCount <= ScoringConstants.FormatPetitMaxCells) ||
                (ContainsModifier(activeModifiers, ModifierId.FormatMoyenSpecialiste) && cellCount == ScoringConstants.FormatMoyenCells) ||
                (ContainsModifier(activeModifiers, ModifierId.FormatGrandSpecialiste) && cellCount >= ScoringConstants.FormatGrandMinCells);
            if (!formatMatches)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.CadenceMultiplier));
            return ScoringConstants.CadenceMultiplier;
        }

        /// <summary>Écho: replays whatever modifier sits immediately to its left in <see cref="PreClearModifierContext.ActiveModifiers"/> by invoking that modifier's effect delegate a second time for this placement. Returns 0 if Écho is the leftmost held modifier, or if its left neighbor is another Écho (never chains).</summary>
        private int ApplyEcho(PreClearModifierContext ctx)
        {
            int leftIndex = ctx.CurrentIndex - 1;
            if (leftIndex < 0)
            {
                return 0;
            }

            var targetId = ctx.ActiveModifiers[leftIndex];
            if (targetId == ModifierId.Echo)
            {
                return 0;
            }

            return _preClearEffects.TryGetValue(targetId, out var targetEffect) ? targetEffect(ctx) : 0;
        }

        /// <summary>Specialist (per-piece-size-tier): xN multiplier when the placed piece's cell count falls within [<paramref name="minCells"/>, <paramref name="maxCells"/>] inclusive. Returns 1 otherwise.</summary>
        private static int ApplyFormatSpecialistMultiplier(int minCells, int maxCells, PieceShape shape, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            int cellCount = shape.Cells.Count;
            if (cellCount < minCells || cellCount > maxCells)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.FormeSpecialistMultiplier));
            return ScoringConstants.FormeSpecialistMultiplier;
        }

        /// <summary>Grand Format: bonus per placed cell (the piece's own cell count, not the merged group) once the placed piece is at least ScoringConstants.GrandFormatMinPieceSize cells.</summary>
        private static int ApplyGrandFormat(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (placedCells.Count < ScoringConstants.GrandFormatMinPieceSize)
            {
                return 0;
            }

            int bonus = placedCells.Count * ScoringConstants.GrandFormatBonusPerCell;
            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], bonus));
            return bonus;
        }

        /// <summary>Hors Norme: flat bonus whenever the placed piece's own cell count is anything OTHER than exactly ScoringConstants.HorsNormeExactPieceSize — rewards small or large pieces over "average"-sized ones.</summary>
        private static int ApplyHorsNorme(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (placedCells.Count == ScoringConstants.HorsNormeExactPieceSize)
            {
                return 0;
            }

            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], ScoringConstants.HorsNormeBonus));
            return ScoringConstants.HorsNormeBonus;
        }

        /// <summary>The "+pts" sibling of ApplyFormatSpecialistMultiplier — flat bonus per scored group cell when the placed piece's cell count falls within [<paramref name="minCells"/>, <paramref name="maxCells"/>] inclusive, a flat bonus for piece size.</summary>
        private static int ApplyFormatGlow(int minCells, int maxCells, PieceShape shape, List<Vector2Int> placedCells, List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            int cellCount = shape.Cells.Count;
            if (cellCount < minCells || cellCount > maxCells)
            {
                return 0;
            }

            int bonus = groupCells.Count * ScoringConstants.FormeGlowBonusPerCell;
            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], bonus));
            return bonus;
        }

        /// <summary>Diagonale: bonus per group cell sitting on either of the grid's two main diagonals (x == y, or x + y == Size - 1).</summary>
        private static int ApplyDiagonale(List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            int total = 0;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var pos = groupCells[i];
                if (pos.x != pos.y && pos.x + pos.y != Size - 1)
                {
                    continue;
                }

                events.Add(new ScoreEvent(ScoreEventType.Modifier, pos, ScoringConstants.DiagonaleBonusPerCell));
                total += ScoringConstants.DiagonaleBonusPerCell;
            }
            return total;
        }

        /// <summary>Nid: bonus per group cell with EXACTLY 3 of its 4 cardinal neighbors filled — a softer, more attainable sibling of Prisonnier (needs all 4).</summary>
        private int ApplyNid(List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            int total = 0;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var pos = groupCells[i];
                if (CountFilledCardinalNeighbors(pos.x, pos.y) != 3)
                {
                    continue;
                }

                events.Add(new ScoreEvent(ScoreEventType.Modifier, pos, ScoringConstants.NidBonusPerCell));
                total += ScoringConstants.NidBonusPerCell;
            }
            return total;
        }

        private int CountFilledCardinalNeighbors(int x, int y)
        {
            int count = 0;
            if (InBounds(x - 1, y) && _cells[x - 1, y].IsFilled) count++;
            if (InBounds(x + 1, y) && _cells[x + 1, y].IsFilled) count++;
            if (InBounds(x, y - 1) && _cells[x, y - 1].IsFilled) count++;
            if (InBounds(x, y + 1) && _cells[x, y + 1].IsFilled) count++;
            return count;
        }

        /// <summary>Solitaire: xN multiplier when this placement's scored group is entirely its own piece (nothing pre-existing merged into it) and the piece itself is more than 1 cell (the size-1 case is Îlot's). Returns 1 otherwise.</summary>
        private static int ApplySolitaire(List<Vector2Int> groupCells, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (placedCells.Count <= 1 || groupCells.Count != placedCells.Count)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.SolitaireMultiplier));
            return ScoringConstants.SolitaireMultiplier;
        }

        /// <summary>Pair: xN multiplier (see ScoringConstants.PairMultiplier) when this placement's scored group has an EVEN total cell count. Returns 1 (no-op) otherwise.</summary>
        private static int ApplyParitePaire(List<Vector2Int> groupCells, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (groupCells.Count % 2 != 0)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.PairMultiplier));
            return ScoringConstants.PairMultiplier;
        }

        /// <summary>Impair: xN multiplier (see ScoringConstants.ImpairMultiplier) when this placement's scored group has an ODD total cell count — Pair's exact mirror. Returns 1 (no-op) otherwise.</summary>
        private static int ApplyPariteImpaire(List<Vector2Int> groupCells, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (groupCells.Count % 2 == 0)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.ImpairMultiplier));
            return ScoringConstants.ImpairMultiplier;
        }

        /// <summary>Petit Format: bonus per placed cell when the piece being placed has at most ScoringConstants.PetitFormatMaxPieceSize cells — the small-piece mirror of Grand Format.</summary>
        private static int ApplyPetitFormat(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (placedCells.Count > ScoringConstants.PetitFormatMaxPieceSize)
            {
                return 0;
            }

            int bonus = placedCells.Count * ScoringConstants.PetitFormatBonusPerCell;
            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], bonus));
            return bonus;
        }

        /// <summary>Fraîcheur: xN multiplier (see ScoringConstants.FraicheurMultiplier) when this placement's own fill color is not present ANYWHERE else already on the board (a genuinely new color for this board state) — checked against every other cell, this placement's own cells excluded. Returns 1 (no-op) otherwise.</summary>
        private int ApplyFraicheur(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            var ownColor = _cells[placedCells[0].x, placedCells[0].y].FilledColor.Value;
            var placedSet = new HashSet<Vector2Int>(placedCells);
            foreach (var pos in AllPositions())
            {
                if (placedSet.Contains(pos))
                {
                    continue;
                }
                var cell = _cells[pos.x, pos.y];
                if (cell.IsFilled && cell.FilledColor.HasValue && cell.FilledColor.Value == ownColor)
                {
                    return 1;
                }
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.FraicheurMultiplier));
            return ScoringConstants.FraicheurMultiplier;
        }

        /// <summary>Espace Libre: xN multiplier (see ScoringConstants.EspaceLibreMultiplier) when, right after this placement (and any of its own line clears), at most ScoringConstants.EspaceLibreMaxFilledCells cells on the whole board are still filled — rewards keeping the board deliberately open. Returns 1 (no-op) otherwise.</summary>
        private int ApplyEspaceLibre(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            int filled = 0;
            foreach (var pos in AllPositions())
            {
                if (_cells[pos.x, pos.y].IsFilled)
                {
                    filled++;
                }
            }
            if (filled > ScoringConstants.EspaceLibreMaxFilledCells)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.EspaceLibreMultiplier));
            return ScoringConstants.EspaceLibreMultiplier;
        }

        /// <summary>
        /// Density: progressive +N Mult, the opposite of Espace Libre — N is
        /// how many cells are filled on the board right after this placement
        /// (including any of its own line clears), divided by
        /// ScoringConstants.DensiteFilledCellsPerMultStep as a true float
        /// (never floored mid-calculation). The badge popup shows a rounded
        /// whole number (ScoreEvent.Amount stays int), but the event's own
        /// PreciseAmount carries the true float since <see
        /// cref="PlacementResult.Mult"/>'s ordered fold reads straight off
        /// this event and would otherwise lose precision.
        /// </summary>
        private float ApplyDensite(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            int filled = 0;
            foreach (var pos in AllPositions())
            {
                if (_cells[pos.x, pos.y].IsFilled)
                {
                    filled++;
                }
            }

            float additiveMult = filled / (float)ScoringConstants.DensiteFilledCellsPerMultStep;
            var densiteEvent = new ScoreEvent(ScoreEventType.MultBonus, placedCells[0], Mathf.RoundToInt(additiveMult));
            densiteEvent.PreciseAmount = additiveMult;
            events.Add(densiteEvent);
            return additiveMult;
        }

        /// <summary>Rafale: xN multiplier (see ScoringConstants.RafaleMultiplier) when this placement clears at least one row/column AND the immediately previous placement this round also did — two clears back to back. Returns 1 (no-op) otherwise.</summary>
        private static int ApplyRafale(bool clearedByPreviousPlacement, ClearInfo clearInfo, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (!clearedByPreviousPlacement || clearInfo.ClearedCells.Count == 0)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.RafaleMultiplier));
            return ScoringConstants.RafaleMultiplier;
        }

        /// <summary>Bridge (Pont): xN multiplier per pre-existing group this placement bridges together beyond the first one — bridging 2 formerly-separate groups applies once, 3 groups applies twice (stacking multiplicatively). Returns 1 if this placement touches at most one pre-existing group.</summary>
        private int ApplyPont(PieceColor ownColor, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            int groupsTouched = CountDistinctPreExistingGroupsTouched(placedCells, ownColor);
            if (groupsTouched < 2)
            {
                return 1;
            }

            int multiplier = 1;
            for (int i = 1; i < groupsTouched; i++)
            {
                multiplier *= ScoringConstants.PontMultiplierPerBridge;
            }
            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], multiplier));
            return multiplier;
        }

        /// <summary>
        /// How many distinct already-existing connected components (using
        /// the same color/joker compatibility rule as <see
        /// cref="FindConnectedGroup"/>) touch this placement's own cells.
        /// This placement's own cells are excluded from every flood-fill, so
        /// two pre-existing groups on opposite sides of the piece are
        /// counted separately even though placing the piece would merge
        /// them into one. Each distinct component is only ever counted once
        /// even if several of the piece's own cells touch it.
        /// </summary>
        private int CountDistinctPreExistingGroupsTouched(List<Vector2Int> placedCells, PieceColor pieceColor)
        {
            var placedSet = new HashSet<Vector2Int>(placedCells);
            var globallyVisited = new HashSet<Vector2Int>();
            int distinctGroups = 0;
            for (int i = 0; i < placedCells.Count; i++)
            {
                var pos = placedCells[i];
                distinctGroups += TryCountNeighborGroup(pos.x - 1, pos.y, placedSet, globallyVisited, pieceColor);
                distinctGroups += TryCountNeighborGroup(pos.x + 1, pos.y, placedSet, globallyVisited, pieceColor);
                distinctGroups += TryCountNeighborGroup(pos.x, pos.y - 1, placedSet, globallyVisited, pieceColor);
                distinctGroups += TryCountNeighborGroup(pos.x, pos.y + 1, placedSet, globallyVisited, pieceColor);
            }
            return distinctGroups;
        }

        private int TryCountNeighborGroup(int x, int y, HashSet<Vector2Int> placedSet, HashSet<Vector2Int> globallyVisited, PieceColor pieceColor)
        {
            if (!InBounds(x, y))
            {
                return 0;
            }
            var pos = new Vector2Int(x, y);
            if (placedSet.Contains(pos) || globallyVisited.Contains(pos))
            {
                return 0;
            }
            var cell = _cells[x, y];
            if (!cell.IsFilled || !cell.FilledColor.HasValue)
            {
                return 0;
            }

            var neighborColor = cell.FilledColor.Value;
            if (pieceColor != PieceColor.Joker && neighborColor != PieceColor.Joker && neighborColor != pieceColor)
            {
                return 0;
            }

            var visited = new HashSet<Vector2Int> { pos };
            var stack = new Stack<Vector2Int>();
            stack.Push(pos);
            PieceColor? anchor = neighborColor == PieceColor.Joker ? (PieceColor?)null : neighborColor;
            while (stack.Count > 0)
            {
                var cur = stack.Pop();
                FloodVisitExcludingPiece(cur.x - 1, cur.y, placedSet, ref anchor, visited, stack);
                FloodVisitExcludingPiece(cur.x + 1, cur.y, placedSet, ref anchor, visited, stack);
                FloodVisitExcludingPiece(cur.x, cur.y - 1, placedSet, ref anchor, visited, stack);
                FloodVisitExcludingPiece(cur.x, cur.y + 1, placedSet, ref anchor, visited, stack);
            }
            globallyVisited.UnionWith(visited);
            return 1;
        }

        /// <summary>Same neighbor/anchor-color rules as <see cref="TryVisitGroupNeighbor"/>, but additionally never steps into <paramref name="placedSet"/> — used to flood-fill a PRE-existing component's true extent without the flood leaking through the piece currently being placed into a different, unrelated component on its other side.</summary>
        private void FloodVisitExcludingPiece(int x, int y, HashSet<Vector2Int> placedSet, ref PieceColor? anchorColor, HashSet<Vector2Int> visited, Stack<Vector2Int> stack)
        {
            if (!InBounds(x, y))
            {
                return;
            }
            var pos = new Vector2Int(x, y);
            if (placedSet.Contains(pos) || visited.Contains(pos))
            {
                return;
            }
            var cell = _cells[x, y];
            if (!cell.IsFilled || !cell.FilledColor.HasValue)
            {
                return;
            }

            var color = cell.FilledColor.Value;
            if (color != PieceColor.Joker)
            {
                if (anchorColor.HasValue && anchorColor.Value != color)
                {
                    return;
                }
                anchorColor = color;
            }

            visited.Add(pos);
            stack.Push(pos);
        }

        /// <summary>Encirclement: bonus per group cell whose 8 surrounding tiles are all filled, locked, OR off the edge of the grid — a softer sibling of Fortress, which never credits an edge/corner cell (out of bounds always fails its check).</summary>
        private int ApplyEncerclement(List<Vector2Int> groupCells, List<ScoreEvent> events)
        {
            int total = 0;
            for (int i = 0; i < groupCells.Count; i++)
            {
                var pos = groupCells[i];
                if (!AreAllNeighborsFilledOrOffGrid(pos.x, pos.y))
                {
                    continue;
                }
                events.Add(new ScoreEvent(ScoreEventType.Modifier, pos, ScoringConstants.EncerclementBonusPerCell));
                total += ScoringConstants.EncerclementBonusPerCell;
            }
            return total;
        }

        private bool AreAllNeighborsFilledOrOffGrid(int x, int y)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }
                    int nx = x + dx;
                    int ny = y + dy;
                    if (!InBounds(nx, ny))
                    {
                        continue; // off the grid counts as "filled" for Encerclement
                    }
                    var neighbor = _cells[nx, ny];
                    // A locked-but-empty cell is still a genuine obstacle (see Cell.IsLocked's own doc
                    // comment) — it blocks a line from completing there just like a filled tile would, so
                    // it should surround a cell just as well for Encerclement's purposes.
                    if (!neighbor.IsFilled && !neighbor.IsLocked)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>Sealer (Boucher): bonus per pre-existing tile that this placement itself causes to become encircled (see Encerclement/AreAllNeighborsFilledOrOffGrid) — a tile bordering this placement was by definition not fully encircled before it.</summary>
        private int ApplyBoucher(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            var placedSet = new HashSet<Vector2Int>(placedCells);
            var candidates = new HashSet<Vector2Int>();
            for (int i = 0; i < placedCells.Count; i++)
            {
                var pos = placedCells[i];
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0)
                        {
                            continue;
                        }
                        int x = pos.x + dx;
                        int y = pos.y + dy;
                        if (!InBounds(x, y))
                        {
                            continue;
                        }
                        var npos = new Vector2Int(x, y);
                        if (placedSet.Contains(npos) || !_cells[x, y].IsFilled)
                        {
                            continue;
                        }
                        candidates.Add(npos);
                    }
                }
            }

            int total = 0;
            foreach (var pos in candidates)
            {
                if (!AreAllNeighborsFilledOrOffGrid(pos.x, pos.y))
                {
                    continue;
                }
                events.Add(new ScoreEvent(ScoreEventType.Modifier, pos, ScoringConstants.BoucherBonusPerCell));
                total += ScoringConstants.BoucherBonusPerCell;
            }
            return total;
        }

        /// <summary>Big Family (Grosse Famille): xN multiplier (see ScoringConstants.GrosseFamilleMultiplier) when this placement's color exists in exactly ONE connected group on the whole board — no other same-color cell anywhere outside this placement's own scored group. Returns 1 (no-op) otherwise.</summary>
        private int ApplyGrosseFamille(List<Vector2Int> groupCells, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            var ownColor = _cells[placedCells[0].x, placedCells[0].y].FilledColor.Value;
            var groupSet = new HashSet<Vector2Int>(groupCells);
            foreach (var pos in AllPositions())
            {
                if (groupSet.Contains(pos))
                {
                    continue;
                }
                var cell = _cells[pos.x, pos.y];
                if (cell.IsFilled && cell.FilledColor.HasValue && cell.FilledColor.Value == ownColor)
                {
                    return 1;
                }
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.GrosseFamilleMultiplier));
            return ScoringConstants.GrosseFamilleMultiplier;
        }

        /// <summary>Repetition (progressive): xN multiplier where N is <paramref name="repetitionStreak"/>, the number of placements in a row (this one included) sharing the same shape — x2 on the 2nd consecutive same-shape placement, x3 on the 3rd, and so on. Returns 1 (no-op) on the first placement of a new streak.</summary>
        private static int ApplyRepetition(int repetitionStreak, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (repetitionStreak < 2)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], repetitionStreak));
            return repetitionStreak;
        }

        /// <summary>Golden Repetition (RepetitionLueur): flat Lueur (see EconomyConstants.RepetitionLueurBonus) when this piece is the same shape as the immediately previous placement this round — the Lueur-earning sibling of Repetition, on the same "does this streak continue" condition but unlike it, not progressive (repeats the same flat amount every time it fires rather than scaling with streak length). Returns 0 (no-op) on the first placement of a new streak.</summary>
        private static int ApplyRepetitionLueur(int repetitionStreak, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (repetitionStreak < 2)
            {
                return 0;
            }

            events.Add(new ScoreEvent(ScoreEventType.LueurBonus, placedCells[0], EconomyConstants.RepetitionLueurBonus));
            return EconomyConstants.RepetitionLueurBonus;
        }

        /// <summary>Shared by the 3 flat, unconditional +Mult modifiers (Mult +1/+2/+4) and Risky Mult — always fires, adds <paramref name="amount"/> to PlacementResult.AdditiveMultBonus (a genuine additive pool, unlike every ModifierMultiplier modifier above).</summary>
        private static int ApplyFlatAdditiveMult(int amount, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            events.Add(new ScoreEvent(ScoreEventType.MultBonus, placedCells[0], amount));
            return amount;
        }

        /// <summary>Solidarity (Solidarite): +N Mult where N is the total number of modifiers currently held (this one included, duplicate copies counting separately) divided by ScoringConstants.SolidariteModifierCountDivisor.</summary>
        private static int ApplySolidarite(int modifierCount, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            int bonus = modifierCount / ScoringConstants.SolidariteModifierCountDivisor;
            if (bonus > 0)
            {
                events.Add(new ScoreEvent(ScoreEventType.MultBonus, placedCells[0], bonus));
            }
            return bonus;
        }

        /// <summary>Dwindling (Epuisement): returns the current decaying flat points bonus (see _epuisementValue), then drops it by ScoringConstants.EpuisementDecayPerPlacement for the next placement (floored at 0). Permanent for the whole run, same as Gradient's counter — NOT reset by ResetForNewRound.</summary>
        private int ApplyEpuisement(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            int bonus = _epuisementValue;
            if (bonus > 0)
            {
                events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], bonus));
            }
            _epuisementValue = Mathf.Max(0, _epuisementValue - ScoringConstants.EpuisementDecayPerPlacement);
            return bonus;
        }

        /// <summary>Color Switch (Alternance des pièces): flat +Mult (additive, see PlacementResult.AdditiveMultBonus, see ScoringConstants.AlternancePiecesBonus) when this piece's color differs from the immediately previous placement's color this round — the piece-to-piece sibling of the existing line-level "Alternation" (Alternance) modifier. Returns 0 (no-op) otherwise.</summary>
        private static int ApplyAlternancePieces(PieceColor ownColor, PieceColor? previousPlacedColor, List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            if (!previousPlacedColor.HasValue || previousPlacedColor.Value == ownColor)
            {
                return 0;
            }

            events.Add(new ScoreEvent(ScoreEventType.MultBonus, placedCells[0], ScoringConstants.AlternancePiecesBonus));
            return ScoringConstants.AlternancePiecesBonus;
        }

        /// <summary>Precision: bonus per placed cell when EVERY one of this placement's own cells has at least one pre-existing filled orthogonal neighbor (this placement's own other cells don't count).</summary>
        private int ApplyPrecision(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            var placedSet = new HashSet<Vector2Int>(placedCells);
            for (int i = 0; i < placedCells.Count; i++)
            {
                if (CountExistingOrthogonalNeighbors(placedCells[i], placedSet) < 1)
                {
                    return 0;
                }
            }

            int bonus = placedCells.Count * ScoringConstants.PrecisionBonusPerCell;
            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], bonus));
            return bonus;
        }

        /// <summary>Overcrowding (Surpopulation): bonus per placed cell when EVERY one of this placement's own cells has at least 2 pre-existing filled orthogonal neighbors — a stricter sibling of Precision.</summary>
        private int ApplySurpopulation(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            var placedSet = new HashSet<Vector2Int>(placedCells);
            for (int i = 0; i < placedCells.Count; i++)
            {
                if (CountExistingOrthogonalNeighbors(placedCells[i], placedSet) < 2)
                {
                    return 0;
                }
            }

            int bonus = placedCells.Count * ScoringConstants.SurpopulationBonusPerCell;
            events.Add(new ScoreEvent(ScoreEventType.Modifier, placedCells[0], bonus));
            return bonus;
        }

        private int CountExistingOrthogonalNeighbors(Vector2Int pos, HashSet<Vector2Int> placedSet)
        {
            int count = 0;
            if (IsExistingFilledNeighbor(pos.x - 1, pos.y, placedSet)) count++;
            if (IsExistingFilledNeighbor(pos.x + 1, pos.y, placedSet)) count++;
            if (IsExistingFilledNeighbor(pos.x, pos.y - 1, placedSet)) count++;
            if (IsExistingFilledNeighbor(pos.x, pos.y + 1, placedSet)) count++;
            return count;
        }

        private bool IsExistingFilledNeighbor(int x, int y, HashSet<Vector2Int> placedSet)
        {
            if (!InBounds(x, y))
            {
                return false;
            }
            var pos = new Vector2Int(x, y);
            if (placedSet.Contains(pos))
            {
                return false; // part of this same piece, doesn't count as "pre-existing"
            }
            return _cells[x, y].IsFilled;
        }

        /// <summary>Minimalist (Minimaliste): xN multiplier (see ScoringConstants.MinimalisteMultiplier) when this placement's WHOLE footprint touches EXACTLY one distinct pre-existing filled cell in total — the "just barely touching" middle ground between Îlot (zero neighbors, isolated) and Precision (one or more, checked per cell). Returns 1 (no-op) otherwise.</summary>
        private int ApplyMinimaliste(List<Vector2Int> placedCells, List<ScoreEvent> events)
        {
            var placedSet = new HashSet<Vector2Int>(placedCells);
            var distinctNeighbors = new HashSet<Vector2Int>();
            for (int i = 0; i < placedCells.Count; i++)
            {
                var pos = placedCells[i];
                AddIfExistingFilledNeighbor(pos.x - 1, pos.y, placedSet, distinctNeighbors);
                AddIfExistingFilledNeighbor(pos.x + 1, pos.y, placedSet, distinctNeighbors);
                AddIfExistingFilledNeighbor(pos.x, pos.y - 1, placedSet, distinctNeighbors);
                AddIfExistingFilledNeighbor(pos.x, pos.y + 1, placedSet, distinctNeighbors);
            }
            if (distinctNeighbors.Count != 1)
            {
                return 1;
            }

            events.Add(new ScoreEvent(ScoreEventType.ModifierMultiplier, placedCells[0], ScoringConstants.MinimalisteMultiplier));
            return ScoringConstants.MinimalisteMultiplier;
        }

        private void AddIfExistingFilledNeighbor(int x, int y, HashSet<Vector2Int> placedSet, HashSet<Vector2Int> result)
        {
            if (!InBounds(x, y))
            {
                return;
            }
            var pos = new Vector2Int(x, y);
            if (placedSet.Contains(pos))
            {
                return;
            }
            if (_cells[x, y].IsFilled)
            {
                result.Add(pos);
            }
        }

        /// <summary>
        /// When this placement's own color is actually <see
        /// cref="PieceColor.Joker"/> and the Joker modifier is held, resolves
        /// to whichever base color would score the most from the Devotion
        /// modifiers currently active. Falls back to the piece's real
        /// color otherwise.
        /// </summary>
        private static PieceColor ResolveJokerColorForModifiers(PieceColor actualColor, IReadOnlyList<ModifierId> activeModifiers, int groupBonus, int groupCellCount)
        {
            if (actualColor != PieceColor.Joker || !ContainsModifier(activeModifiers, ModifierId.Joker))
            {
                return actualColor;
            }

            var baseColors = PieceColorUtility.BaseColors;
            PieceColor best = actualColor;
            int bestScore = 0;
            for (int i = 0; i < baseColors.Count; i++)
            {
                var candidate = baseColors[i];
                int score = 0;
                if (ContainsModifier(activeModifiers, DevotionModifierFor(candidate)))
                {
                    score += ScoringConstants.DevotionBonus;
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }
            return bestScore > 0 ? best : actualColor;
        }

        private static ModifierId DevotionModifierFor(PieceColor color)
        {
            switch (color)
            {
                case PieceColor.Coral: return ModifierId.DevotionCoral;
                case PieceColor.Teal: return ModifierId.DevotionTeal;
                case PieceColor.Violet: return ModifierId.DevotionViolet;
                default: return ModifierId.DevotionLime;
            }
        }

        private static bool ContainsModifier(IReadOnlyList<ModifierId> modifiers, ModifierId id)
        {
            if (modifiers == null)
            {
                return false;
            }
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i] == id)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Everything a post-clear modifier's effect delegate (see <see
        /// cref="_postClearEffects"/>) might need to read, plus the
        /// accumulators it mutates as a side effect (Multiplier/Lueur/
        /// ProgressiveAdditiveMult — <see cref="ApplyPostClearModifiers"/>
        /// reads these back out once the whole dispatch loop finishes).
        /// Same rationale as <see cref="PreClearModifierContext"/>.
        /// </summary>
        private sealed class PostClearModifierContext
        {
            public ClearInfo ClearInfo;
            public List<Vector2Int> PlacedCells;
            public List<ScoreEvent> Events;
            public bool ClearedByPreviousPlacement;
            public int Multiplier = 1;
            public int Lueur;
            public float ProgressiveAdditiveMult;
            public int AdditiveMult;
        }
    }
}
