using System.Collections.Generic;

namespace Contigu.Core
{
    /// <summary>
    /// Owns the persistent run-long deck, a shuffled draw pile, and the current
    /// hand. The deck is mutated in place by bank upgrades and never allowed
    /// to drop below <see cref="MinDeckSize"/>.
    /// </summary>
    public sealed class DeckManager
    {
        public const int MinDeckSize = 10;
        public const int HandSize = 3;

        private readonly List<PieceToken> _deck = new List<PieceToken>();
        private readonly List<PieceToken> _drawPile = new List<PieceToken>();
        // Fixed-size — always exactly HandSize entries, a null meaning that
        // slot is currently empty. Playing a piece never shifts the other
        // slots (needed so Slot Loyalty modifiers can track which slot a
        // piece came from) — a slot only refills once the WHOLE hand is
        // empty (see PlayFromHand/DrawNewHand).
        private readonly List<PieceToken?> _hand = new List<PieceToken?>();
        private readonly List<PieceRotation> _handRotations = new List<PieceRotation>();
        private readonly IRandomProvider _rng;

        public IReadOnlyList<PieceToken> Deck
        {
            get { return _deck; }
        }

        /// <summary>The current hand, one entry per fixed slot (always <see cref="HandSize"/> long) — null means that slot is empty, played but not yet refilled.</summary>
        public IReadOnlyList<PieceToken?> Hand
        {
            get { return _hand; }
        }

        /// <summary>
        /// Each hand slot's random orientation, parallel to <see cref="Hand"/> —
        /// rolled fresh whenever that slot is (re)dealt, not tied to the token's
        /// identity in the deck (so the same piece type can come up in a
        /// different rotation next time it's drawn).
        /// </summary>
        public IReadOnlyList<PieceRotation> HandRotations
        {
            get { return _handRotations; }
        }

        public int DeckCount
        {
            get { return _deck.Count; }
        }

        public DeckManager(IEnumerable<PieceToken> initialDeck, IRandomProvider rng)
        {
            _rng = rng;
            _deck.AddRange(initialDeck);
            ReshuffleDrawPile();
            DrawNewHand();
        }

        private void ReshuffleDrawPile()
        {
            _drawPile.Clear();
            _drawPile.AddRange(_deck);
            Shuffle(_drawPile);
        }

        private void Shuffle(List<PieceToken> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                var tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        private void EnsureDrawPileHasEnough(int needed)
        {
            if (_drawPile.Count < needed)
            {
                ReshuffleDrawPile();
            }
        }

        /// <summary>
        /// Draws a fresh hand of 3 from the draw pile (without replacement),
        /// reshuffling from the current deck state first if needed. Each slot
        /// also gets a fresh random <see cref="PieceRotation"/>.
        /// </summary>
        public void DrawNewHand()
        {
            _hand.Clear();
            _handRotations.Clear();
            for (int i = 0; i < HandSize; i++)
            {
                EnsureDrawPileHasEnough(1);
                if (_drawPile.Count == 0)
                {
                    // Deck is empty (should not happen given MinDeckSize > 0):
                    // leave this and every remaining slot empty rather than
                    // shrinking the hand below HandSize entries.
                    _hand.Add(null);
                    _handRotations.Add(RandomRotation());
                    continue;
                }
                int lastIndex = _drawPile.Count - 1;
                var token = _drawPile[lastIndex];
                _drawPile.RemoveAt(lastIndex);
                _hand.Add(token);
                _handRotations.Add(RandomRotation());
            }
        }

        private PieceRotation RandomRotation()
        {
            return (PieceRotation)_rng.Next(4);
        }

        /// <summary>
        /// "Thief" enemy mechanic: empties one random currently-occupied hand
        /// slot, returning its token to the bottom of the draw pile rather
        /// than destroying it. No-op (returns false, out params default) if
        /// every slot is empty. Called from RunManager.DrawFreshHand after
        /// the fresh hand is dealt. The stolen slot's index/token/rotation
        /// are handed back so Presentation can animate the actual piece
        /// (RunManager.ThiefStolenHandIndex/ThiefStolenToken/
        /// ThiefStolenRotation) flying off, even though by the time
        /// Presentation sees the result the slot is already empty.
        /// </summary>
        public bool StealRandomHandTile(IRandomProvider rng, out int stolenIndex, out PieceToken stolenToken, out PieceRotation stolenRotation)
        {
            var occupied = new List<int>();
            for (int i = 0; i < HandSize; i++)
            {
                if (_hand[i].HasValue)
                {
                    occupied.Add(i);
                }
            }
            if (occupied.Count == 0)
            {
                stolenIndex = -1;
                stolenToken = default;
                stolenRotation = default;
                return false;
            }
            int idx = occupied[rng.Next(occupied.Count)];
            stolenIndex = idx;
            stolenToken = _hand[idx].Value;
            stolenRotation = _handRotations[idx];
            _drawPile.Insert(0, _hand[idx].Value);
            _hand[idx] = null;
            return true;
        }

        /// <summary>
        /// Empties the <paramref name="handIndex"/> slot in place — other
        /// slots are never shifted (see the field comment on <see cref="_hand"/>).
        /// A fresh hand is drawn once every slot is empty, unless
        /// <paramref name="refillIfEmpty"/> is false, in which case the caller
        /// defers the draw (see RunManager.PlacePiece/StartRound: when the
        /// placement that empties the hand also ends the round, the fresh
        /// hand must belong to the next round, not be dealt early).
        /// </summary>
        public void PlayFromHand(int handIndex, bool refillIfEmpty = true)
        {
            _hand[handIndex] = null;
            if (refillIfEmpty && IsHandFullyEmpty())
            {
                DrawNewHand();
            }
        }

        /// <summary>True once every hand slot is empty — the trigger for PlayFromHand's automatic refill, and for RunManager's own deferred-refill check.</summary>
        public bool IsHandFullyEmpty()
        {
            for (int i = 0; i < _hand.Count; i++)
            {
                if (_hand[i].HasValue)
                {
                    return false;
                }
            }
            return true;
        }

        public IReadOnlyDictionary<(ShapeId Shape, PieceColor Color), int> GetDeckComposition()
        {
            var composition = new Dictionary<(ShapeId, PieceColor), int>();
            for (int i = 0; i < _deck.Count; i++)
            {
                var key = (_deck[i].Shape, _deck[i].Color);
                composition.TryGetValue(key, out int count);
                composition[key] = count + 1;
            }
            return composition;
        }

        /// <summary>
        /// Up to <paramref name="count"/> distinct (shape, color) types from
        /// the deck's composition, randomly sampled — same idea as
        /// <see cref="GetCandidateTokenIndices"/> but per-TYPE rather than
        /// per-token, since a Bank sub-choice (Retirer/Dupliquer/Recolorer)
        /// acts on a whole type at once.
        /// </summary>
        public IReadOnlyList<(ShapeId Shape, PieceColor Color)> GetCandidateTypes(int count, IRandomProvider rng, System.Func<(ShapeId Shape, PieceColor Color), bool> eligible = null)
        {
            var candidates = new List<(ShapeId Shape, PieceColor Color)>();
            foreach (var kvp in GetDeckComposition())
            {
                if (eligible == null || eligible(kvp.Key))
                {
                    candidates.Add(kvp.Key);
                }
            }

            var chosen = new List<(ShapeId Shape, PieceColor Color)>();
            int take = count < candidates.Count ? count : candidates.Count;
            for (int i = 0; i < take; i++)
            {
                int pick = rng.Next(candidates.Count);
                chosen.Add(candidates[pick]);
                candidates.RemoveAt(pick);
            }
            return chosen;
        }

        public bool CanRemove(ShapeId shape, PieceColor color)
        {
            if (_deck.Count <= MinDeckSize)
            {
                return false;
            }
            return _deck.Exists(t => t.Matches(shape, color));
        }

        public bool RemoveOneOfType(ShapeId shape, PieceColor color)
        {
            if (!CanRemove(shape, color))
            {
                return false;
            }

            int idx = _deck.FindIndex(t => t.Matches(shape, color));
            if (idx < 0)
            {
                return false;
            }
            _deck.RemoveAt(idx);

            int dpIdx = _drawPile.FindIndex(t => t.Matches(shape, color));
            if (dpIdx >= 0)
            {
                _drawPile.RemoveAt(dpIdx);
            }
            return true;
        }

        public bool DuplicateOfType(ShapeId shape, PieceColor color)
        {
            if (!_deck.Exists(t => t.Matches(shape, color)))
            {
                return false;
            }
            AddToken(new PieceToken(shape, color));
            return true;
        }

        /// <summary>
        /// Removes one copy of (<paramref name="removeShape"/>, <paramref
        /// name="removeColor"/>) and immediately adds one copy of (<paramref
        /// name="addShape"/>, <paramref name="addColor"/>) — both types must
        /// already exist in the deck (see UpgradeSystem.GetCandidateTypesFor/
        /// GetReplacementCandidateTypesFor), so the net deck count never
        /// changes and MinDeckSize is never a concern. Replacing a type with
        /// itself is a harmless no-op.
        /// </summary>
        public bool ReplaceOneOfType(ShapeId removeShape, PieceColor removeColor, ShapeId addShape, PieceColor addColor)
        {
            int idx = _deck.FindIndex(t => t.Matches(removeShape, removeColor));
            if (idx < 0 || !_deck.Exists(t => t.Matches(addShape, addColor)))
            {
                return false;
            }

            _deck.RemoveAt(idx);
            int dpIdx = _drawPile.FindIndex(t => t.Matches(removeShape, removeColor));
            if (dpIdx >= 0)
            {
                _drawPile.RemoveAt(dpIdx);
            }

            AddToken(new PieceToken(addShape, addColor));
            return true;
        }

        /// <summary>
        /// Adds a joker-colored piece in a uniformly random shape, tagged
        /// with one of the Joker-exclusive combat traits rolled uniformly
        /// from <see cref="PieceTrait.JokerCombatKinds"/> — every Joker gets
        /// exactly one, never none. Returns the shape actually rolled;
        /// <paramref name="combatKind"/> carries the trait roll.
        /// </summary>
        public ShapeId AddJoker(IRandomProvider rng, out PieceTraitKind combatKind)
        {
            var shape = InitialDeckFactory.ShapeOrder[rng.Next(InitialDeckFactory.ShapeOrder.Length)];
            combatKind = PieceTrait.JokerCombatKinds[rng.Next(PieceTrait.JokerCombatKinds.Length)];
            AddToken(new PieceToken(shape, PieceColor.Joker, new PieceTrait(combatKind, 0)));
            return shape;
        }

        public bool RecolorOneOfType(ShapeId shape, PieceColor fromColor, PieceColor toColor)
        {
            int idx = _deck.FindIndex(t => t.Matches(shape, fromColor));
            if (idx < 0)
            {
                return false;
            }
            _deck[idx] = new PieceToken(shape, toColor);

            int dpIdx = _drawPile.FindIndex(t => t.Matches(shape, fromColor));
            if (dpIdx >= 0)
            {
                _drawPile[dpIdx] = new PieceToken(shape, toColor);
            }
            return true;
        }

        /// <summary>
        /// Chameleon Tile: once RunManager.ResolveChameleonColor resolves
        /// this hand token's placement color to something other than its
        /// own, permanently recolors this exact deck entry to match — unlike
        /// <see cref="RecolorOneOfType"/>, this keeps <see cref="PieceToken.Trait"/>
        /// intact and targets the specific hand slot's token by reference
        /// rather than by shape/color match. No-op if the slot is empty.
        /// </summary>
        public void RecolorHandToken(int handIndex, PieceColor newColor)
        {
            if (!_hand[handIndex].HasValue)
            {
                return;
            }

            var current = _hand[handIndex].Value;
            var recolored = new PieceToken(current.Shape, newColor, current.Trait);
            _hand[handIndex] = recolored;

            int deckIdx = _deck.FindIndex(t => t.Shape == current.Shape && t.Color == current.Color && Equals(t.Trait, current.Trait));
            if (deckIdx >= 0)
            {
                _deck[deckIdx] = recolored;
            }
        }

        private void AddToken(PieceToken token)
        {
            _deck.Add(token);
        }

        /// <summary>
        /// Adds a fully-formed token (shape, color, and optionally an
        /// already-rolled trait) straight to the deck. Only reaches _deck —
        /// not _drawPile — so a newly added piece isn't drawable until the
        /// next reshuffle, same as every other deck-growing upgrade.
        /// </summary>
        public void AddPreparedToken(PieceToken token)
        {
            AddToken(token);
        }

        /// <summary>
        /// Mirrors a _deck token edit (<paramref name="before"/> becoming
        /// <paramref name="after"/>) into whichever of _drawPile/_hand
        /// currently holds a live copy of it. PieceToken is a struct copied
        /// by value, so tagging _deck alone would not retroactively update a
        /// copy already drawn into the draw pile or hand. No-op if neither
        /// holds a matching token.
        /// </summary>
        private void SyncTagIntoLiveCopy(PieceToken before, PieceToken after)
        {
            int dpIndex = _drawPile.FindIndex(t => t.Shape == before.Shape && t.Color == before.Color && Equals(t.Trait, before.Trait));
            if (dpIndex >= 0)
            {
                _drawPile[dpIndex] = after;
                return;
            }
            for (int i = 0; i < _hand.Count; i++)
            {
                if (!_hand[i].HasValue)
                {
                    continue;
                }
                var handToken = _hand[i].Value;
                if (handToken.Shape == before.Shape && handToken.Color == before.Color && Equals(handToken.Trait, before.Trait))
                {
                    _hand[i] = after;
                    return;
                }
            }
        }

        /// <summary>Tags up to <paramref name="count"/> distinct deck tokens with a permanent-for-the-run golden trait on one random cell each.</summary>
        public IReadOnlyList<int> TagGoldenTokensRandom(int count, IRandomProvider rng)
        {
            return TagRandomTokens(count, rng, (token, localIndex) => new PieceTrait(PieceTraitKind.Golden, localIndex));
        }

        /// <summary>
        /// Tags up to <paramref name="count"/> distinct deck tokens with a
        /// permanent-for-the-run tinted trait on one random cell each. The
        /// target color is always the token's own color, so the tinted
        /// condition always matches. Joker tokens are excluded: a placed
        /// Joker cell's FilledColor always stays PieceColor.Joker (see
        /// GridManager.PlacePiece), so no TintedColor could ever match it.
        /// </summary>
        public IReadOnlyList<int> TagTintedTokensRandom(int count, IRandomProvider rng)
        {
            return TagRandomTokens(count, rng, (token, localIndex) => new PieceTrait(PieceTraitKind.Tinted, localIndex, token.Color), token => token.Color != PieceColor.Joker);
        }

        /// <summary>Tags up to <paramref name="count"/> distinct deck tokens with a permanent-for-the-run multiplier trait on one random cell each.</summary>
        public IReadOnlyList<int> TagMultiplierTokensRandom(int count, IRandomProvider rng)
        {
            return TagRandomTokens(count, rng, (token, localIndex) => new PieceTrait(PieceTraitKind.Multiplier, localIndex));
        }

        /// <summary>Tags up to <paramref name="count"/> distinct deck tokens with a permanent-for-the-run "Blast Tile" trait on one random cell each.</summary>
        public IReadOnlyList<int> TagBlastTokensRandom(int count, IRandomProvider rng)
        {
            return TagRandomTokens(count, rng, (token, localIndex) => new PieceTrait(PieceTraitKind.Blast, localIndex));
        }

        /// <summary>Tags up to <paramref name="count"/> distinct deck tokens with a permanent-for-the-run "Multiplier Beacon" trait on one random cell each.</summary>
        public IReadOnlyList<int> TagBeaconTokensRandom(int count, IRandomProvider rng)
        {
            return TagRandomTokens(count, rng, (token, localIndex) => new PieceTrait(PieceTraitKind.Beacon, localIndex));
        }

        /// <summary>Tags up to <paramref name="count"/> distinct deck tokens with a permanent-for-the-run "Mirror Tile" trait on one random cell each.</summary>
        public IReadOnlyList<int> TagMirrorTokensRandom(int count, IRandomProvider rng)
        {
            return TagRandomTokens(count, rng, (token, localIndex) => new PieceTrait(PieceTraitKind.Mirror, localIndex));
        }

        /// <summary>Tags up to <paramref name="count"/> distinct deck tokens with a permanent-for-the-run "Seeder" trait on one random cell each.</summary>
        public IReadOnlyList<int> TagSeederTokensRandom(int count, IRandomProvider rng)
        {
            return TagRandomTokens(count, rng, (token, localIndex) => new PieceTrait(PieceTraitKind.Seeder, localIndex));
        }

        /// <summary>Tags up to <paramref name="count"/> distinct deck tokens with a permanent-for-the-run "Catalyst" trait on one random cell each.</summary>
        public IReadOnlyList<int> TagCatalystTokensRandom(int count, IRandomProvider rng)
        {
            return TagRandomTokens(count, rng, (token, localIndex) => new PieceTrait(PieceTraitKind.Catalyst, localIndex));
        }

        /// <summary>Tags up to <paramref name="count"/> distinct deck tokens with a permanent-for-the-run "Twin" trait on one random cell each.</summary>
        public IReadOnlyList<int> TagTwinTokensRandom(int count, IRandomProvider rng)
        {
            return TagRandomTokens(count, rng, (token, localIndex) => new PieceTrait(PieceTraitKind.Twin, localIndex));
        }

        /// <summary>Tags up to <paramref name="count"/> distinct deck tokens with a permanent-for-the-run "Detonator" trait on one random cell each.</summary>
        public IReadOnlyList<int> TagDetonatorTokensRandom(int count, IRandomProvider rng)
        {
            return TagRandomTokens(count, rng, (token, localIndex) => new PieceTrait(PieceTraitKind.Detonator, localIndex));
        }

        /// <summary>Tags up to <paramref name="count"/> distinct deck tokens with a permanent-for-the-run "Chameleon" trait on one random cell each.</summary>
        public IReadOnlyList<int> TagChameleonTokensRandom(int count, IRandomProvider rng)
        {
            return TagRandomTokens(count, rng, (token, localIndex) => new PieceTrait(PieceTraitKind.Chameleon, localIndex));
        }

        /// <summary>Tags up to <paramref name="count"/> distinct deck tokens with a permanent-for-the-run "Spark" trait on one random cell each.</summary>
        public IReadOnlyList<int> TagSparkTokensRandom(int count, IRandomProvider rng)
        {
            return TagRandomTokens(count, rng, (token, localIndex) => new PieceTrait(PieceTraitKind.Spark, localIndex));
        }

        /// <summary>Tags up to <paramref name="count"/> distinct deck tokens with a permanent-for-the-run "Void" trait on one random cell each.</summary>
        public IReadOnlyList<int> TagVoidTokensRandom(int count, IRandomProvider rng)
        {
            return TagRandomTokens(count, rng, (token, localIndex) => new PieceTrait(PieceTraitKind.Void, localIndex));
        }

        /// <summary>Tags up to <paramref name="count"/> distinct deck tokens with a permanent-for-the-run "Bastion" trait on one random cell each.</summary>
        public IReadOnlyList<int> TagBastionTokensRandom(int count, IRandomProvider rng)
        {
            return TagRandomTokens(count, rng, (token, localIndex) => new PieceTrait(PieceTraitKind.Bastion, localIndex));
        }

        /// <summary>Tags up to <paramref name="count"/> distinct deck tokens with a permanent-for-the-run "Kamikaze" trait on one random cell each.</summary>
        public IReadOnlyList<int> TagKamikazeTokensRandom(int count, IRandomProvider rng)
        {
            return TagRandomTokens(count, rng, (token, localIndex) => new PieceTrait(PieceTraitKind.Kamikaze, localIndex));
        }

        /// <summary>
        /// Picks up to <paramref name="count"/> distinct deck indices — preferring
        /// tokens that don't already carry a trait, falling back to any token if
        /// there aren't enough untagged ones — and applies <paramref name="makeTrait"/>
        /// (given the token itself and a random valid local cell index for its
        /// shape) to each. <paramref name="eligible"/>, when given, excludes any
        /// token it returns false for from candidacy in both passes. Returns the
        /// tagged deck indices.
        /// </summary>
        private IReadOnlyList<int> TagRandomTokens(int count, IRandomProvider rng, System.Func<PieceToken, int, PieceTrait> makeTrait, System.Func<PieceToken, bool> eligible = null)
        {
            var candidates = new List<int>();
            for (int i = 0; i < _deck.Count; i++)
            {
                if (!_deck[i].Trait.HasValue && (eligible == null || eligible(_deck[i])))
                {
                    candidates.Add(i);
                }
            }
            if (candidates.Count < count)
            {
                candidates.Clear();
                for (int i = 0; i < _deck.Count; i++)
                {
                    if (eligible == null || eligible(_deck[i]))
                    {
                        candidates.Add(i);
                    }
                }
            }

            var chosen = new List<int>();
            int take = count < candidates.Count ? count : candidates.Count;
            for (int i = 0; i < take; i++)
            {
                int pick = rng.Next(candidates.Count);
                chosen.Add(candidates[pick]);
                candidates.RemoveAt(pick);
            }

            for (int i = 0; i < chosen.Count; i++)
            {
                int deckIndex = chosen[i];
                var token = _deck[deckIndex];
                int cellCount = PieceShapeCatalog.Get(token.Shape).Cells.Count;
                int localIndex = rng.Next(cellCount);
                var taggedToken = token.WithTrait(makeTrait(token, localIndex));
                _deck[deckIndex] = taggedToken;
                SyncTagIntoLiveCopy(token, taggedToken);
            }
            return chosen;
        }

        /// <summary>
        /// Up to <paramref name="count"/> random distinct deck indices, using
        /// the same candidate-selection rule as <see cref="TagRandomTokens"/>
        /// (prefer untagged tokens, fall back to any token if there aren't
        /// enough) — but these are only shown to the player, not tagged. Feeds
        /// the shop's "choose which tiles get this upgrade" flow — see
        /// <see cref="TagSpecificTokens"/> for the other half.
        /// </summary>
        public IReadOnlyList<int> GetCandidateTokenIndices(int count, IRandomProvider rng, System.Func<PieceToken, bool> eligible = null)
        {
            var candidates = new List<int>();
            for (int i = 0; i < _deck.Count; i++)
            {
                if (!_deck[i].Trait.HasValue && (eligible == null || eligible(_deck[i])))
                {
                    candidates.Add(i);
                }
            }
            if (candidates.Count < count)
            {
                candidates.Clear();
                for (int i = 0; i < _deck.Count; i++)
                {
                    if (eligible == null || eligible(_deck[i]))
                    {
                        candidates.Add(i);
                    }
                }
            }

            var chosen = new List<int>();
            int take = count < candidates.Count ? count : candidates.Count;
            for (int i = 0; i < take; i++)
            {
                int pick = rng.Next(candidates.Count);
                chosen.Add(candidates[pick]);
                candidates.RemoveAt(pick);
            }
            return chosen;
        }

        /// <summary>
        /// Tags exactly the given deck indices with a trait of <paramref
        /// name="kind"/> — the player-chosen counterpart to the random
        /// Tag*TokensRandom methods above. Tinted is the one kind that also
        /// needs its target color pinned to the token's own (see
        /// TagTintedTokensRandom), handled the same way here.
        /// </summary>
        public void TagSpecificTokens(IReadOnlyList<int> deckIndices, PieceTraitKind kind, IRandomProvider rng)
        {
            for (int i = 0; i < deckIndices.Count; i++)
            {
                int deckIndex = deckIndices[i];
                var token = _deck[deckIndex];
                int cellCount = PieceShapeCatalog.Get(token.Shape).Cells.Count;
                int localIndex = rng.Next(cellCount);
                var trait = kind == PieceTraitKind.Tinted
                    ? new PieceTrait(kind, localIndex, token.Color)
                    : new PieceTrait(kind, localIndex);
                var taggedToken = token.WithTrait(trait);
                _deck[deckIndex] = taggedToken;
                SyncTagIntoLiveCopy(token, taggedToken);
            }
        }
    }
}
