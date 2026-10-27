using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Core
{
    /// <summary>Piece traits (stamping, resolving and scoring them) and the deck/hand-driven fertilizer bonuses.</summary>
    public sealed partial class RunManager
    {
        /// <summary>
        /// Stamps every cell of this placement's own piece with whatever flat
        /// Mastery bonus its exact shape/color currently carries (0 for an
        /// axis never leveled up). Must run before <see
        /// cref="GridManager.PlacePiece"/>, which reads <see
        /// cref="Cell.ShapeMasteryBonus"/>/<see cref="Cell.ColorMasteryBonus"/>
        /// in its group-scoring loop. The stamp is frozen at whatever level
        /// applied at placement time, so a later Mastery purchase never
        /// retroactively changes it, but it does rescore every time this
        /// cell's group scores again.
        /// </summary>
        private void StampMasteryBonuses(PieceShape shape, PieceColor color, int x, int y)
        {
            int shapeBonus = GetShapeMasteryLevel(shape.Id) - 1;
            int colorBonus = GetColorMasteryLevel(color) - 1;
            if (shapeBonus <= 0 && colorBonus <= 0)
            {
                return;
            }

            for (int i = 0; i < shape.Cells.Count; i++)
            {
                var pos = shape.Cells[i] + new Vector2Int(x, y);
                var cell = Grid.GetCell(pos);
                cell.ShapeMasteryBonus = shapeBonus;
                cell.ColorMasteryBonus = colorBonus;
            }
        }

        /// <summary>
        /// If <paramref name="trait"/> is present, stamps the grid cell(s) its
        /// effect touches with the matching golden/tinted/multiplier flag(s)
        /// just before <see cref="GridManager.PlacePiece"/> scores this
        /// placement. Returns every cell that should be un-stamped again once
        /// scoring is done (see <see cref="ClearTokenTraitCells"/>) - every
        /// kind except <see cref="PieceTraitKind.Seeder"/> (stays until
        /// Cell.ResetForNewRound) and the kinds whose effects resolve before
        /// Grid.PlacePiece runs (Chameleon) or after it returns (see <see
        /// cref="ApplyPostPlacementTraitBonus"/>), none of which stamp a cell
        /// at all.
        /// </summary>
        private List<Cell> ApplyTokenTrait(PieceTrait? trait, Vector2Int? traitCellPos, PieceShape shape, int anchorX, int anchorY)
        {
            var transientCells = new List<Cell>();
            if (!trait.HasValue)
            {
                return transientCells;
            }

            var pos = traitCellPos.Value;
            var cell = Grid.GetCell(pos.x, pos.y);

            // Cosmetic, persists for the rest of the round regardless of trait
            // kind - not added to transientCells (see Cell.OriginTrait).
            cell.OriginTrait = trait.Value;

            // Joker's own combat traits badge every cell of the piece, not just
            // this one - purely cosmetic; the one-shot combat effect still
            // resolves once per placement regardless (see
            // RunManager.ApplyJokerCombatDamage).
            if (PieceTrait.IsJokerCombatKind(trait.Value.Kind))
            {
                for (int i = 0; i < shape.Cells.Count; i++)
                {
                    var otherPos = new Vector2Int(anchorX, anchorY) + shape.Cells[i];
                    if (otherPos != pos)
                    {
                        Grid.GetCell(otherPos.x, otherPos.y).OriginTrait = trait.Value;
                    }
                }
            }

            switch (trait.Value.Kind)
            {
                case PieceTraitKind.Golden:
                    cell.IsGolden = true;
                    transientCells.Add(cell);
                    break;

                case PieceTraitKind.Tinted:
                    cell.IsTinted = true;
                    cell.TintedColor = trait.Value.TintedColor.Value;
                    transientCells.Add(cell);
                    break;

                case PieceTraitKind.Multiplier:
                    cell.IsMultiplierZone = true;
                    transientCells.Add(cell);
                    break;

                case PieceTraitKind.Blast:
                    cell.IsGolden = true;
                    transientCells.Add(cell);
                    StampGoldenIfInBounds(pos.x - 1, pos.y, transientCells);
                    StampGoldenIfInBounds(pos.x + 1, pos.y, transientCells);
                    StampGoldenIfInBounds(pos.x, pos.y - 1, transientCells);
                    StampGoldenIfInBounds(pos.x, pos.y + 1, transientCells);
                    break;

                case PieceTraitKind.Beacon:
                    cell.IsMultiplierZone = true;
                    transientCells.Add(cell);
                    StampMultiplierAlongRowAndColumn(pos, transientCells);
                    // The seed's own other cells in the beacon's row or column count too (they are not on the garden yet, so the sweep above misses them).
                    for (int c = 0; c < shape.Cells.Count; c++)
                    {
                        var own = new Vector2Int(anchorX, anchorY) + shape.Cells[c];
                        if (own != pos && (own.x == pos.x || own.y == pos.y))
                        {
                            var ownCell = Grid.GetCell(own.x, own.y);
                            ownCell.IsMultiplierZone = true;
                            transientCells.Add(ownCell);
                        }
                    }
                    break;

                case PieceTraitKind.Seeder:
                    // Intentionally NOT added to transientCells, so
                    // ClearTokenTraitCells never un-stamps it after this placement's own
                    // scoring - unlike every other trait, this one keeps scoring as a
                    // normal golden grid cell for the rest of the current round.
                    // Grid.ResetForNewRound (via Cell.ResetForNewRound) eventually
                    // clears it.
                    cell.IsGolden = true;
                    break;

                case PieceTraitKind.Mirror:
                case PieceTraitKind.Catalyst:
                case PieceTraitKind.Twin:
                case PieceTraitKind.Detonator:
                case PieceTraitKind.Chameleon:
                case PieceTraitKind.Spark:
                case PieceTraitKind.Void:
                case PieceTraitKind.Bastion:
                case PieceTraitKind.Kamikaze:
                case PieceTraitKind.Venom:
                case PieceTraitKind.Pip:
                case PieceTraitKind.Lucky:
                case PieceTraitKind.Companion:
                case PieceTraitKind.Colony:
                case PieceTraitKind.Border:
                case PieceTraitKind.Heart:
                case PieceTraitKind.Corner:
                case PieceTraitKind.Hermit:
                case PieceTraitKind.Cuddly:
                case PieceTraitKind.Dawn:
                case PieceTraitKind.Dusk:
                case PieceTraitKind.Miser:
                case PieceTraitKind.Stormy:
                case PieceTraitKind.Elder:
                case PieceTraitKind.Harvester:
                case PieceTraitKind.Breeze:
                case PieceTraitKind.Harmony:
                case PieceTraitKind.Prism:
                case PieceTraitKind.Gardener:
                case PieceTraitKind.Cluster:
                case PieceTraitKind.Crossfire:
                case PieceTraitKind.Even:
                case PieceTraitKind.RainbowRow:
                case PieceTraitKind.Sunseeker:
                case PieceTraitKind.Hunter:
                case PieceTraitKind.Executioner:
                case PieceTraitKind.Spillover:
                case PieceTraitKind.Honeycomb:
                case PieceTraitKind.Dewdrop:
                case PieceTraitKind.Keystone:
                case PieceTraitKind.Growth:
                case PieceTraitKind.Gambler:
                case PieceTraitKind.Warden:
                case PieceTraitKind.Aegis:
                case PieceTraitKind.Sower:
                case PieceTraitKind.Kin:
                case PieceTraitKind.Amplifier:
                case PieceTraitKind.Echo:
                case PieceTraitKind.Collector:
                case PieceTraitKind.Resonance:
                case PieceTraitKind.Pouch:
                case PieceTraitKind.Perennial:
                case PieceTraitKind.Crescendo:
                case PieceTraitKind.Scholar:
                case PieceTraitKind.FullHouse:
                case PieceTraitKind.Compound:
                case PieceTraitKind.Gap:
                case PieceTraitKind.Wildroot:
                case PieceTraitKind.Dye:
                case PieceTraitKind.Fertile:
                case PieceTraitKind.Evergreen:
                case PieceTraitKind.Pruner:
                case PieceTraitKind.Glow:
                case PieceTraitKind.Bridge:
                case PieceTraitKind.Seedpod:
                case PieceTraitKind.Locksmith:
                    // None of these stamp a Cell flag before placement - each is either
                    // resolved before Grid.PlacePiece runs (Chameleon, via
                    // placementColor above) or computed after it returns (see
                    // ApplyPostPlacementTraitBonus). Bastion in particular can't lock its
                    // cell yet: Grid.PlacePiece's own internal CanPlace re-check would
                    // then see this placement's own cell as already locked and reject
                    // it.
                    break;

                case PieceTraitKind.Bombe:
                case PieceTraitKind.Range:
                case PieceTraitKind.Eclat:
                case PieceTraitKind.Precision:
                    // Joker-exclusive combat traits never score anything of their own
                    // (the OriginTrait stamp is purely cosmetic); their effect is
                    // resolved from PlacePiece's own HasActiveEncounter block, after
                    // Grid.PlacePiece returns the final TotalScore to redirect (see
                    // ApplyJokerCombatDamage).
                    break;
            }
            return transientCells;
        }

        /// <summary>
        /// "Chameleon Tile": resolves the color the whole piece should place
        /// as - of every already-filled orthogonal neighbor color around the
        /// enchanted cell, picks whichever would make this placement's own
        /// resulting group score the most (same treatment as Joker - see
        /// GridManager.ResolveBestJokerGroup). Falls back to the piece's own
        /// color when no neighbor is filled yet (checked before
        /// Grid.PlacePiece runs, so only pre-existing board state can match).
        /// </summary>
        private PieceColor ResolveChameleonColor(PieceShape shape, int anchorX, int anchorY, Vector2Int traitCellPos, PieceColor fallbackColor)
        {
            var candidates = new List<PieceColor>();
            AddDistinctNeighborColor(traitCellPos.x - 1, traitCellPos.y, candidates);
            AddDistinctNeighborColor(traitCellPos.x + 1, traitCellPos.y, candidates);
            AddDistinctNeighborColor(traitCellPos.x, traitCellPos.y - 1, candidates);
            AddDistinctNeighborColor(traitCellPos.x, traitCellPos.y + 1, candidates);

            if (candidates.Count == 0)
            {
                return fallbackColor;
            }
            if (candidates.Count == 1)
            {
                return candidates[0];
            }

            PieceColor bestColor = candidates[0];
            int bestScore = Grid.PreviewGroupScore(shape, candidates[0], anchorX, anchorY);
            for (int i = 1; i < candidates.Count; i++)
            {
                int score = Grid.PreviewGroupScore(shape, candidates[i], anchorX, anchorY);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestColor = candidates[i];
                }
            }
            return bestColor;
        }

        private void AddDistinctNeighborColor(int x, int y, List<PieceColor> candidates)
        {
            var color = TryGetFilledNeighborColor(x, y);
            if (color.HasValue && !candidates.Contains(color.Value))
            {
                candidates.Add(color.Value);
            }
        }

        private PieceColor? TryGetFilledNeighborColor(int x, int y)
        {
            if (!GridManager.InBounds(x, y))
            {
                return null;
            }
            var cell = Grid.GetCell(x, y);
            return cell.IsFilled ? cell.FilledColor : null;
        }

        private void StampGoldenIfInBounds(int x, int y, List<Cell> transientCells)
        {
            if (!GridManager.InBounds(x, y))
            {
                return;
            }
            var cell = Grid.GetCell(x, y);
            cell.IsGolden = true;
            transientCells.Add(cell);
        }

        /// <summary>Marks every already-filled cell (pre-existing board state, not this placement's own cells) in <paramref name="origin"/>'s row and column as a multiplier zone, for "Multiplier Beacon".</summary>
        private void StampMultiplierAlongRowAndColumn(Vector2Int origin, List<Cell> transientCells)
        {
            for (int gx = 0; gx < GridManager.Size; gx++)
            {
                if (gx == origin.x)
                {
                    continue;
                }
                var cell = Grid.GetCell(gx, origin.y);
                if (cell.IsFilled)
                {
                    cell.IsMultiplierZone = true;
                    transientCells.Add(cell);
                }
            }
            for (int gy = 0; gy < GridManager.Size; gy++)
            {
                if (gy == origin.y)
                {
                    continue;
                }
                var cell = Grid.GetCell(origin.x, gy);
                if (cell.IsFilled)
                {
                    cell.IsMultiplierZone = true;
                    transientCells.Add(cell);
                }
            }
        }

        /// <summary>Reverts every stamp <see cref="ApplyTokenTrait"/> made - most enchantments fire once, on this placement's own scoring, not as a lasting grid modifier.</summary>
        private static void ClearTokenTraitCells(List<Cell> cells)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                cells[i].IsGolden = cells[i].IsWeatherGolden;
                cells[i].IsTinted = false;
                cells[i].IsMultiplierZone = false;
            }
        }

        /// <summary>
        /// Handles every trait kind whose bonus can't be computed by
        /// GridManager's own per-cell scoring loop: Mirror/Catalyst/Twin need
        /// the group as it stood right after scoring, Detonator needs the
        /// line-clear outcome, Spark needs the pre-placement no-clear streak,
        /// and Void mutates the grid outside this placement's own cells.
        /// </summary>
        private void ApplyPostPlacementTraitBonus(PieceTrait trait, Vector2Int traitCellPos, int sparkStreakBeforePlacement, PlacementResult placement)
        {
            switch (trait.Kind)
            {
                case PieceTraitKind.Mirror:
                    ApplyMirrorBonus(traitCellPos, placement);
                    break;
                case PieceTraitKind.Catalyst:
                    ApplyCatalystBonus(traitCellPos, placement);
                    break;
                case PieceTraitKind.Twin:
                    ApplyTwinBonus(traitCellPos, placement);
                    break;
                case PieceTraitKind.Detonator:
                    ApplyDetonatorBonus(placement);
                    break;
                case PieceTraitKind.Spark:
                    ApplySparkBonus(sparkStreakBeforePlacement, placement);
                    break;
                case PieceTraitKind.Void:
                    ApplyVoidEffect(placement);
                    break;
                case PieceTraitKind.Bastion:
                    ApplyBastionEffect(traitCellPos, placement);
                    break;
                case PieceTraitKind.Kamikaze:
                    ApplyKamikazeEffect(traitCellPos, placement);
                    break;
                case PieceTraitKind.Venom:
                    ApplyVenomEffect(traitCellPos, placement);
                    break;
                case PieceTraitKind.Pip:
                case PieceTraitKind.Lucky:
                case PieceTraitKind.Companion:
                case PieceTraitKind.Colony:
                case PieceTraitKind.Border:
                case PieceTraitKind.Heart:
                case PieceTraitKind.Corner:
                case PieceTraitKind.Hermit:
                case PieceTraitKind.Cuddly:
                case PieceTraitKind.Dawn:
                case PieceTraitKind.Dusk:
                case PieceTraitKind.Miser:
                case PieceTraitKind.Stormy:
                case PieceTraitKind.Elder:
                case PieceTraitKind.Harvester:
                case PieceTraitKind.Breeze:
                case PieceTraitKind.Harmony:
                case PieceTraitKind.Prism:
                case PieceTraitKind.Gardener:
                case PieceTraitKind.Cluster:
                case PieceTraitKind.Crossfire:
                case PieceTraitKind.Even:
                case PieceTraitKind.RainbowRow:
                case PieceTraitKind.Sunseeker:
                case PieceTraitKind.Hunter:
                case PieceTraitKind.Executioner:
                case PieceTraitKind.Spillover:
                case PieceTraitKind.Honeycomb:
                case PieceTraitKind.Dewdrop:
                case PieceTraitKind.Keystone:
                case PieceTraitKind.Growth:
                case PieceTraitKind.Gambler:
                case PieceTraitKind.Warden:
                case PieceTraitKind.Aegis:
                case PieceTraitKind.Sower:
                case PieceTraitKind.Kin:
                case PieceTraitKind.Amplifier:
                case PieceTraitKind.Echo:
                case PieceTraitKind.Collector:
                case PieceTraitKind.Resonance:
                case PieceTraitKind.Pouch:
                case PieceTraitKind.Perennial:
                case PieceTraitKind.Crescendo:
                case PieceTraitKind.Scholar:
                case PieceTraitKind.FullHouse:
                case PieceTraitKind.Compound:
                case PieceTraitKind.Dye:
                case PieceTraitKind.Fertile:
                case PieceTraitKind.Pruner:
                case PieceTraitKind.Glow:
                case PieceTraitKind.Seedpod:
                case PieceTraitKind.Locksmith:
                    ApplyTileUpgradeBonus(trait, traitCellPos, placement);
                    break;
            }
        }

        /// <summary>Adds a flat trait bonus to <paramref name="placement"/> and appends a matching <see cref="ScoreEventType.Trait"/> event.</summary>
        private static void AddTraitBonus(PlacementResult placement, Vector2Int pos, int bonus)
        {
            placement.TraitBonus += bonus;
            var events = new List<ScoreEvent>(placement.ScoreEvents);
            events.Add(new ScoreEvent(ScoreEventType.Trait, pos, bonus));
            placement.ScoreEvents = events;
        }

        /// <summary>Records one cell a trait effect destroyed - see <see cref="PlacementResult.DestroyedCells"/> for why this is separate from <see cref="PlacementResult.ClearedCells"/>.</summary>
        private static void AddDestroyedCell(PlacementResult placement, Vector2Int pos, PieceColor? color, ShapeId? shapeId)
        {
            var cells = new List<Vector2Int>(placement.DestroyedCells);
            cells.Add(pos);
            placement.DestroyedCells = cells;
            var colors = new List<PieceColor?>(placement.DestroyedCellColors);
            colors.Add(color);
            placement.DestroyedCellColors = colors;
            var shapes = new List<ShapeId?>(placement.DestroyedCellShapes);
            shapes.Add(shapeId);
            placement.DestroyedCellShapes = shapes;
        }

        // Which SlotUn/Deux/Trois modifier corresponds to each 0-based hand index.
        private static readonly ModifierId?[] HandSlotModifiers = { ModifierId.SlotUn, ModifierId.SlotDeux, ModifierId.SlotTrois };

        /// <summary>
        /// "Slot N Loyalty": flat +Mult (additive, see
        /// PlacementResult.AdditiveMultBonus, ScoringConstants.SlotLoyaltyBonus)
        /// when the piece was played from hand slot <paramref
        /// name="handIndex"/> (0-based) and the matching modifier is active.
        /// GridManager can't evaluate this itself - it has no idea which hand
        /// slot a piece came from, only this method's caller does - so it's
        /// resolved here post-hoc, same pattern as ApplyPostPlacementTraitBonus.
        /// </summary>
        private void ApplyHandSlotModifierBonus(int handIndex, PlacementResult placement)
        {
            if (handIndex < 0 || handIndex >= HandSlotModifiers.Length)
            {
                return;
            }

            var slotModifier = HandSlotModifiers[handIndex];
            if (!slotModifier.HasValue || !HasEffectiveModifier(slotModifier.Value))
            {
                return;
            }

            // Its own position in _activeModifiers, needed so
            // PlacementResult.Mult's ordered left-to-right fold places this
            // correctly relative to every other Mult modifier - plus this slot's
            // own level factor (see ModifierLevelUtility).
            int slotIndex = -1;
            for (int i = 0; i < _activeModifiers.Count; i++)
            {
                if (_activeModifiers[i] == slotModifier.Value && !_blightedModifiers.Contains(i))
                {
                    slotIndex = i;
                    break;
                }
            }
            int bonus = Mathf.RoundToInt(ScoringConstants.SlotLoyaltyBonus * GetModifierLevelFactor(slotIndex));
            placement.AdditiveMultBonus += bonus;
            var events = new List<ScoreEvent>(placement.ScoreEvents);
            var scoreEvent = new ScoreEvent(ScoreEventType.MultBonus, placement.PlacedCells[0], bonus);
            scoreEvent.TriggeringModifier = slotModifier.Value;
            scoreEvent.TriggeringModifierIndex = slotIndex;
            events.Add(scoreEvent);
            placement.ScoreEvents = events;
        }


        /// <summary>
        /// Enchanted Cards, Multitude and Experience all depend on
        /// RunManager-only state (upgraded-card count, deck size,
        /// special-pieces-played count), not grid/placement state - like
        /// ApplyHandSlotModifierBonus, GridManager can't evaluate these
        /// itself, so they're resolved here post-hoc. Loops over every held
        /// copy of each so holding more than one stacks, same convention as
        /// every other modifier.
        /// </summary>
        private void ApplyDeckStateModifierBonuses(PlacementResult placement)
        {
            if (!HasEffectiveModifier(ModifierId.CartesEnchantees) && !HasEffectiveModifier(ModifierId.Multitude) && !HasEffectiveModifier(ModifierId.Experience) && !HasEffectiveModifier(ModifierId.Arsenal)
                && !HasEffectiveModifier(ModifierId.Barometre) && !HasEffectiveModifier(ModifierId.ChasseurTempete) && !HasEffectiveModifier(ModifierId.Epines) && !HasEffectiveModifier(ModifierId.Exterminateur)
                && !HasEffectiveModifier(ModifierId.Hate) && !HasEffectiveModifier(ModifierId.Tourbillon) && !HoldsAnyNewRunStateModifier())
            {
                return;
            }

            var events = new List<ScoreEvent>(placement.ScoreEvents);
            for (int i = 0; i < _activeModifiers.Count; i++)
            {
                if (_blightedModifiers.Contains(i))
                {
                    continue;
                }
                if (_activeModifiers[i] == ModifierId.CartesEnchantees)
                {
                    int upgradedCount = CountUpgradedDeckCards();
                    // True float, not floored to a whole "+1 Mult" step. Always at
                    // least 0.1 so this always fires; PreciseAmount lets the badge
                    // popup show the exact value rather than a misleadingly rounded
                    // one. Scaled by this slot's own level factor (see
                    // ModifierLevelUtility) same as every other modifier.
                    float trueMult = (1 + upgradedCount) / (float)ScoringConstants.CartesEnchanteesUpgradedCardsPerMultStep * GetModifierLevelFactor(i);
                    placement.ProgressiveAdditiveMult += trueMult;
                    var multEvent = new ScoreEvent(ScoreEventType.MultBonus, placement.PlacedCells[0], Mathf.RoundToInt(trueMult));
                    multEvent.TriggeringModifier = ModifierId.CartesEnchantees;
                    // Its own position in _activeModifiers - feeds PlacementResult.Mult's
                    // ordered fold (see ApplyHandSlotModifierBonus).
                    multEvent.TriggeringModifierIndex = i;
                    multEvent.PreciseAmount = trueMult;
                    events.Add(multEvent);
                }
                else if (_activeModifiers[i] == ModifierId.Multitude)
                {
                    // Mult, not points: +1 Mult per ScoringConstants.MultitudeCardsPerMult seeds in the bag (a true float, like Enchanted Cards).
                    float trueMult = Deck.DeckCount / (float)ScoringConstants.MultitudeCardsPerMult * GetModifierLevelFactor(i);
                    placement.ProgressiveAdditiveMult += trueMult;
                    var multEvent = new ScoreEvent(ScoreEventType.MultBonus, placement.PlacedCells[0], Mathf.RoundToInt(trueMult));
                    multEvent.TriggeringModifier = ModifierId.Multitude;
                    multEvent.TriggeringModifierIndex = i;
                    multEvent.PreciseAmount = trueMult;
                    events.Add(multEvent);
                }
                else if (_activeModifiers[i] == ModifierId.Experience)
                {
                    // Same true-float treatment (and level scaling) as CartesEnchantees above.
                    float trueMult = (1 + _specialPiecesPlayedCount) / (float)ScoringConstants.ExperienceSpecialPiecesPlayedPerMultStep * GetModifierLevelFactor(i);
                    placement.ProgressiveAdditiveMult += trueMult;
                    var multEvent = new ScoreEvent(ScoreEventType.MultBonus, placement.PlacedCells[0], Mathf.RoundToInt(trueMult));
                    multEvent.TriggeringModifier = ModifierId.Experience;
                    multEvent.TriggeringModifierIndex = i;
                    multEvent.PreciseAmount = trueMult;
                    events.Add(multEvent);
                }
                else if (_activeModifiers[i] == ModifierId.Barometre)
                {
                    if (CurrentWeather != WeatherId.Clear)
                    {
                        AddRunStateMultBonus(placement, events, ModifierId.Barometre, i, ScoringConstants.BarometerMultBonus * GetModifierLevelFactor(i), whole: true);
                    }
                }
                else if (_activeModifiers[i] == ModifierId.ChasseurTempete)
                {
                    if (WeatherCatalog.IsHarsh(CurrentWeather))
                    {
                        int amount = Mathf.Max(1, Mathf.RoundToInt(ScoringConstants.StormChaserMultiplier * GetModifierLevelFactor(i)));
                        placement.ModifierMultiplier *= amount;
                        var multEvent = new ScoreEvent(ScoreEventType.ModifierMultiplier, placement.PlacedCells[0], amount);
                        multEvent.TriggeringModifier = ModifierId.ChasseurTempete;
                        multEvent.TriggeringModifierIndex = i;
                        events.Add(multEvent);
                    }
                }
                else if (_activeModifiers[i] == ModifierId.Epines)
                {
                    if (AnyEnemyHasActiveMalus())
                    {
                        AddRunStateMultBonus(placement, events, ModifierId.Epines, i, ScoringConstants.ThornsMultBonus * GetModifierLevelFactor(i), whole: true);
                    }
                }
                else if (_activeModifiers[i] == ModifierId.Exterminateur)
                {
                    int kills = CountDefeatedEnemies();
                    if (kills > 0)
                    {
                        AddRunStateMultBonus(placement, events, ModifierId.Exterminateur, i, kills * ScoringConstants.PestControlMultPerKill * GetModifierLevelFactor(i), whole: true);
                    }
                }
                else if (_activeModifiers[i] == ModifierId.Hate)
                {
                    AddRunStateMultBonus(placement, events, ModifierId.Hate, i, PiecesRemainingThisRound * ScoringConstants.HasteMultPerSeedLeft * GetModifierLevelFactor(i), whole: false);
                }
                else if (_activeModifiers[i] == ModifierId.Tourbillon)
                {
                    AddRunStateMultBonus(placement, events, ModifierId.Tourbillon, i, ShufflesRemaining * ScoringConstants.WhirlwindMultPerShuffle * GetModifierLevelFactor(i), whole: false);
                }
                else if (_activeModifiers[i] == ModifierId.Arsenal)
                {
                    // Whole-number count (at most 5), so this joins the regular
                    // AdditiveMultBonus pool like Solidarite/MultUn, not the fractional
                    // ProgressiveAdditiveMult pool CartesEnchantees/Experience use.
                    int bonus = Mathf.RoundToInt(CountDistinctCombatKindsInDeck() * ScoringConstants.ArsenalMultPerDistinctCombatKind * GetModifierLevelFactor(i));
                    placement.AdditiveMultBonus += bonus;
                    var multEvent = new ScoreEvent(ScoreEventType.MultBonus, placement.PlacedCells[0], bonus);
                    multEvent.TriggeringModifier = ModifierId.Arsenal;
                    multEvent.TriggeringModifierIndex = i;
                    events.Add(multEvent);
                }
                else
                {
                    ApplyNewRunStateModifier(placement, events, i, _activeModifiers[i]);
                }
            }
            placement.ScoreEvents = events;
        }

        /// <summary>
        /// Adds one run-state "+Mult" contribution (see ApplyDeckStateModifierBonuses) at <paramref name="index"/>'s place in the fold: a whole number
        /// joins AdditiveMultBonus like Solidarity, a fractional one ProgressiveAdditiveMult like Enchanted Seeds, with its exact value kept for the fold.
        /// </summary>
        private void AddRunStateMultBonus(PlacementResult placement, List<ScoreEvent> events, ModifierId id, int index, float amount, bool whole)
        {
            if (amount <= 0f)
            {
                return;
            }
            if (whole)
            {
                amount = Mathf.RoundToInt(amount);
                placement.AdditiveMultBonus += (int)amount;
            }
            else
            {
                placement.ProgressiveAdditiveMult += amount;
            }
            var multEvent = new ScoreEvent(ScoreEventType.MultBonus, placement.PlacedCells[0], Mathf.RoundToInt(amount));
            multEvent.TriggeringModifier = id;
            multEvent.TriggeringModifierIndex = index;
            if (!whole)
            {
                multEvent.PreciseAmount = amount;
            }
            events.Add(multEvent);
        }

        /// <summary>Whether any alive enemy currently has a lock or blight on the garden (Thorns' condition).</summary>
        private bool AnyEnemyHasActiveMalus()
        {
            for (int i = 0; i < _currentEncounter.Count; i++)
            {
                var enemy = _currentEncounter[i];
                if (!enemy.IsDead && (enemy.LockedCell.HasValue || enemy.ExtraLockedCell.HasValue || enemy.PoisonedCells.Count > 0))
                {
                    return true;
                }
            }
            return false;
        }

        private int CountDefeatedEnemies()
        {
            int count = 0;
            for (int i = 0; i < _currentEncounter.Count; i++)
            {
                if (_currentEncounter[i].IsDead)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>How many tokens in the whole deck currently carry a PieceTrait - the "upgraded card" count Enchanted Cards scales with.</summary>
        private int CountUpgradedDeckCards()
        {
            int count = 0;
            var deck = Deck.Deck;
            for (int i = 0; i < deck.Count; i++)
            {
                if (deck[i].Trait.HasValue)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>Arsenal's driver: how many distinct Joker-exclusive combat trait kinds (see PieceTrait.JokerCombatKinds) are currently anywhere in the deck - duplicates of the same kind count once.</summary>
        private int CountDistinctCombatKindsInDeck()
        {
            var kindsSeen = new HashSet<PieceTraitKind>();
            var deck = Deck.Deck;
            for (int i = 0; i < deck.Count; i++)
            {
                foreach (var trait in deck[i].Traits)
                {
                    if (PieceTrait.IsJokerCombatKind(trait.Kind))
                    {
                        kindsSeen.Add(trait.Kind);
                    }
                }
            }
            return kindsSeen.Count;
        }

        /// <summary>Polyvalence's tooltip driver - same distinct-category count as GridManager.ApplyPolyvalence, duplicated here for this read-only "currently" display.</summary>
        private int CountDistinctModifierCategoriesHeld()
        {
            var categoriesSeen = new HashSet<ModifierCategory>();
            for (int i = 0; i < _activeModifiers.Count; i++)
            {
                categoriesSeen.Add(ModifierCatalog.Get(_activeModifiers[i]).Category);
            }
            return categoriesSeen.Count;
        }

        /// <summary>
        /// Tallies every <see cref="ScoreEventType.Modifier"/> and <see
        /// cref="ScoreEventType.ModifierMultiplier"/> event in this
        /// placement's final score events, after every modifier bonus has
        /// already been appended. Powers the "used N times" tooltip stat -
        /// a modifier only counts as "used" the instant it actually scores.
        /// </summary>
        private void CountModifierUsage(PlacementResult placement)
        {
            for (int i = 0; i < placement.ScoreEvents.Count; i++)
            {
                var scoreEvent = placement.ScoreEvents[i];
                bool isModifierEvent = scoreEvent.Type == ScoreEventType.Modifier || scoreEvent.Type == ScoreEventType.ModifierMultiplier || scoreEvent.Type == ScoreEventType.LueurBonus || scoreEvent.Type == ScoreEventType.MultBonus;
                if (!isModifierEvent || !scoreEvent.TriggeringModifier.HasValue)
                {
                    continue;
                }

                var id = scoreEvent.TriggeringModifier.Value;
                _modifierUsageCounts.TryGetValue(id, out var count);
                _modifierUsageCounts[id] = count + 1;
            }
        }

        /// <summary>
        /// The scored group's total cell count, and the specific amount the
        /// cell at <paramref name="cellPos"/> itself earned (0 if not part of
        /// the scored group). Group scoring is progressive (the Nth cell is
        /// worth N*GroupBonusPerCell), so a specific cell's share can't be
        /// read off any Group event - it must be the one at that exact
        /// position.
        /// </summary>
        private static void GetGroupShare(PlacementResult placement, Vector2Int cellPos, out int groupSize, out int cellAmount)
        {
            groupSize = 0;
            cellAmount = 0;
            for (int i = 0; i < placement.ScoreEvents.Count; i++)
            {
                var scoreEvent = placement.ScoreEvents[i];
                if (scoreEvent.Type != ScoreEventType.Group)
                {
                    continue;
                }
                groupSize++;
                if (scoreEvent.Position == cellPos)
                {
                    cellAmount = scoreEvent.Amount;
                }
            }
        }

        /// <summary>
        /// "Mirror Tile": duplicates the enchanted cell's own group-bonus
        /// share onto one random other cell in the scored group. No-op if
        /// the group is just this placement's own cell.
        /// </summary>
        private void ApplyMirrorBonus(Vector2Int traitCellPos, PlacementResult placement)
        {
            GetGroupShare(placement, traitCellPos, out _, out int enchantedCellAmount);
            var otherPositions = new List<Vector2Int>();
            for (int i = 0; i < placement.ScoreEvents.Count; i++)
            {
                var scoreEvent = placement.ScoreEvents[i];
                if (scoreEvent.Type == ScoreEventType.Group && scoreEvent.Position != traitCellPos)
                {
                    otherPositions.Add(scoreEvent.Position);
                }
            }

            if (otherPositions.Count == 0)
            {
                return;
            }

            var targetPos = otherPositions[_rng.Next(otherPositions.Count)];
            AddTraitBonus(placement, targetPos, enchantedCellAmount);
        }

        /// <summary>"Catalyst Tile": scores extra points for every cell in this placement's scored group that was already on the grid before this placement — the group's size (from GetGroupShare) minus this piece's own cell count.</summary>
        private static void ApplyCatalystBonus(Vector2Int traitCellPos, PlacementResult placement)
        {
            GetGroupShare(placement, traitCellPos, out int groupSize, out _);
            int existingCells = groupSize - placement.PlacedCells.Count;
            if (existingCells <= 0)
            {
                return;
            }

            int bonus = existingCells * ScoringConstants.CatalystBonusPerExistingCell;
            AddTraitBonus(placement, placement.PlacedCells[0], bonus);
        }

        /// <summary>"Twin Tile": like Mirror, but duplicates the enchanted cell's own group-bonus share onto EVERY other cell in the group, not just one at random — total extra is that specific cell's own amount times (group size - 1).</summary>
        private static void ApplyTwinBonus(Vector2Int traitCellPos, PlacementResult placement)
        {
            GetGroupShare(placement, traitCellPos, out int groupSize, out int enchantedCellAmount);
            if (groupSize < 2)
            {
                return;
            }

            int bonus = enchantedCellAmount * (groupSize - 1);
            AddTraitBonus(placement, placement.PlacedCells[0], bonus);
        }

        /// <summary>"Detonator Tile": doubles this placement's ENTIRE line-clear bonus, if it clears at least one row/column.</summary>
        private static void ApplyDetonatorBonus(PlacementResult placement)
        {
            if (placement.LineClearScore <= 0)
            {
                return;
            }
            AddTraitBonus(placement, placement.PlacedCells[0], placement.LineClearScore);
        }

        /// <summary>"Spark Tile": scores more the longer it's been since the last line/column clear this round — <paramref name="streakBeforePlacement"/> is the streak as captured in PlacePiece BEFORE Grid.PlacePiece ran, so this placement's own clear (if any) doesn't erase the streak it's scoring against.</summary>
        private static void ApplySparkBonus(int streakBeforePlacement, PlacementResult placement)
        {
            if (streakBeforePlacement <= 0)
            {
                return;
            }
            int bonus = streakBeforePlacement * ScoringConstants.SparkBonusPerPlacement;
            AddTraitBonus(placement, placement.PlacedCells[0], bonus);
        }

        /// <summary>"Void Tile": clears one random already-filled, unlocked cell elsewhere on the grid (excludes this placement's own cells) and scores ScoringConstants.VoidBonusPerDestroyedCell for it. No-op if nothing else is eligible.</summary>
        private void ApplyVoidEffect(PlacementResult placement)
        {
            var cleared = Grid.ClearRandomFilledCell(_rng, placement.PlacedCells, out var clearedColor, out var clearedShape);
            if (cleared.HasValue)
            {
                AddDestroyedCell(placement, cleared.Value, clearedColor, clearedShape);
                AddTraitBonus(placement, cleared.Value, ScoringConstants.VoidBonusPerDestroyedCell);
            }
        }

        /// <summary>
        /// "Bastion Tile": once this placement has fully resolved (its own
        /// line clears included), the enchanted cell locks in place for the
        /// rest of the round - GridManager.CheckAndClearLines then skips it
        /// forever after, but still credits it the line-clear bonus every
        /// time its row/column completes. No-op if this same placement's own
        /// line clear already wiped the cell before we got here.
        /// </summary>
        private void ApplyBastionEffect(Vector2Int traitCellPos, PlacementResult placement)
        {
            var cell = Grid.GetCell(traitCellPos);
            if (!cell.IsFilled)
            {
                return;
            }
            cell.IsBastion = true;
            cell.IsLocked = true;
        }

        /// <summary>
        /// "Kamikaze Tile": destroys the enchanted cell itself plus every
        /// already-filled, unlocked cell in its 8 surrounding tiles (Moore
        /// neighborhood), including this same placement's own other cells.
        /// Scores ScoringConstants.KamikazeBonusPerDestroyedCell per tile
        /// actually destroyed, the trait cell included.
        /// </summary>
        private void ApplyKamikazeEffect(Vector2Int traitCellPos, PlacementResult placement)
        {
            int destroyed = 0;

            var centerCell = Grid.GetCell(traitCellPos.x, traitCellPos.y);
            if (centerCell.IsFilled && !centerCell.IsLocked)
            {
                var centerColor = centerCell.FilledColor;
                var centerShape = centerCell.FilledShapeId;
                centerCell.ClearFill();
                destroyed++;
                AddDestroyedCell(placement, traitCellPos, centerColor, centerShape);
            }

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    int x = traitCellPos.x + dx;
                    int y = traitCellPos.y + dy;
                    if (!GridManager.InBounds(x, y))
                    {
                        continue;
                    }

                    var pos = new Vector2Int(x, y);
                    var cell = Grid.GetCell(pos);
                    if (!cell.IsFilled || cell.IsLocked)
                    {
                        continue;
                    }

                    var color = cell.FilledColor;
                    var shapeId = cell.FilledShapeId;
                    cell.ClearFill();
                    destroyed++;
                    AddDestroyedCell(placement, pos, color, shapeId);
                }
            }

            if (destroyed > 0)
            {
                AddTraitBonus(placement, traitCellPos, destroyed * ScoringConstants.KamikazeBonusPerDestroyedCell);
            }
        }

        /// <summary>
        /// "Venom Tile": deals the front alive enemy direct damage equal to ScoringConstants.
        /// VenomDamagePercentOfMaxHp% of its OWN current max HP, independent of whether this
        /// placement scored anything — a no-op outside an active encounter. Unlike
        /// ApplyDamageToEncounter's flat damage parameter, the amount here is computed per-target
        /// since it depends on the target's own CurrentMaxHp, so it can't reuse that helper directly.
        /// </summary>
        private void ApplyVenomEffect(Vector2Int traitCellPos, PlacementResult placement)
        {
            if (!HasActiveEncounter)
            {
                return;
            }
            for (int i = 0; i < _currentEncounter.Count; i++)
            {
                var enemy = _currentEncounter[i];
                if (enemy.IsDead)
                {
                    continue;
                }
                int damage = Mathf.RoundToInt(enemy.CurrentMaxHp * (ScoringConstants.VenomDamagePercentOfMaxHp / 100f));
                var hits = new List<(Vector2Int Position, int EnemyIndex, int Damage)>(placement.VenomHits);
                hits.Add((traitCellPos, i, damage));
                placement.VenomHits = hits;
                if (enemy.ApplyDamage(damage))
                {
                    CleanUpDefeatedEnemy(enemy);
                }
                break;
            }
        }
    }
}
