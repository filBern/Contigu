using System.Collections.Generic;

namespace Contigu.Core
{
    /// <summary>
    /// Owns the persistent run-long deck, a shuffled draw pile, and the current
    /// hand of 3 pieces (spec section 4). The deck is mutated in place by the
    /// bank upgrades and never allowed to drop below <see cref="MinDeckSize"/>.
    /// </summary>
    public sealed class DeckManager
    {
        public const int MinDeckSize = 10;
        public const int HandSize = 3;

        private readonly List<PieceToken> _deck = new List<PieceToken>();
        private readonly List<PieceToken> _drawPile = new List<PieceToken>();
        // Fixed-size — always exactly HandSize entries, a null meaning that
        // slot is currently empty. Playing a piece never shifts the other
        // slots (on explicit request: playing slot 1 while slots 2/3 are
        // still full used to shift them down into slots 1/2, which also kept
        // undermining "which slot did this piece come from" for the
        // Slot Loyalty modifiers) — a slot only refills once the WHOLE hand
        // is empty (see PlayFromHand/DrawNewHand).
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
                    // Deck is empty (should not happen given MinDeckSize > 0)
                    // — leave this and every remaining slot empty rather than
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
        /// Empties the <paramref name="handIndex"/> slot in place — the other
        /// slots are never shifted (on explicit request; see the field
        /// comment on <see cref="_hand"/>). Per spec 4.2, a fresh hand of 3
        /// is only drawn once every slot is empty — unless
        /// <paramref name="refillIfEmpty"/> is false, in which case the
        /// caller takes responsibility for drawing later (see
        /// RunManager.PlacePiece: when the placement that empties the hand
        /// also ends the round, drawing immediately would hand out the NEXT
        /// round's pieces before the player has even picked their upgrade
        /// for THIS one — deferred to RunManager.StartRound instead, so the
        /// fresh hand belongs to the round it's actually drawn for).
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
        /// acts on a whole type at once. Explicit request: the piece-type
        /// picker used to list every distinct type in the deck, which could
        /// run well past a screenful; capped the same way the Grid-pool tile
        /// choice already was.
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

        /// <summary>Adds a joker-colored piece in a uniformly random shape (on explicit request — used to always be a fixed Single tile).</summary>
        public void AddJoker(IRandomProvider rng)
        {
            var shape = InitialDeckFactory.ShapeOrder[rng.Next(InitialDeckFactory.ShapeOrder.Length)];
            AddToken(new PieceToken(shape, PieceColor.Joker));
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

        private void AddToken(PieceToken token)
        {
            _deck.Add(token);
        }

        /// <summary>Tags up to <paramref name="count"/> distinct deck tokens with a permanent-for-the-run golden trait on one random cell each (spec 5.4 redesign).</summary>
        public IReadOnlyList<int> TagGoldenTokensRandom(int count, IRandomProvider rng)
        {
            return TagRandomTokens(count, rng, (token, localIndex) => new PieceTrait(PieceTraitKind.Golden, localIndex));
        }

        /// <summary>
        /// Tags up to <paramref name="count"/> distinct deck tokens with a
        /// permanent-for-the-run tinted trait on one random cell each. The
        /// target color is always the TOKEN'S OWN color (never rolled
        /// independently) — a token's color never changes on its own, so a
        /// tinted tile always matches and always fires; the earlier
        /// independent-random-color version left most tinted tiles
        /// permanently unable to ever match (on explicit player feedback:
        /// "les tinted tiles sont vraiment chiantes, il se peut qu'elle
        /// serve a rien parfois" — it wasn't "sometimes", a mismatched tile
        /// could never trigger for the rest of the run short of a
        /// Recolorer pick landing on that exact type). Joker tokens are
        /// excluded from candidacy entirely: a placed Joker cell's own
        /// FilledColor always stays PieceColor.Joker (never resolved to a
        /// neighbor's color in storage, see GridManager.PlacePiece), so no
        /// non-Joker TintedColor could ever match it either — tagging one
        /// would just recreate the same dead-enchantment problem this fix
        /// is for.
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
        /// token it returns false for from candidacy entirely (both the
        /// untagged-preferred pass and the any-token fallback) — used by
        /// <see cref="TagTintedTokensRandom"/> to keep Joker tokens out, since
        /// their trait could never fire either way (see there). Returns the
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
                _deck[deckIndex] = token.WithTrait(makeTrait(token, localIndex));
            }
            return chosen;
        }

        /// <summary>
        /// Up to <paramref name="count"/> random distinct deck indices, using
        /// the EXACT same candidate-selection rule as <see cref="TagRandomTokens"/>
        /// (prefer untagged tokens, fall back to any token if there aren't
        /// enough) — but these are only SHOWN to the player, not tagged. Feeds
        /// the shop's "choose which tiles get this upgrade" flow (spec: "un
        /// choix de 5 tiles") — see <see cref="TagSpecificTokens"/> for the
        /// other half.
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
        /// Tags EXACTLY the given deck indices with a trait of <paramref
        /// name="kind"/> — the player-chosen counterpart to the random
        /// Tag*TokensRandom methods above, used once the shop reveals which
        /// Grid-pool upgrade a purchased slot actually grants and the player
        /// picks which of the shown candidates (see
        /// <see cref="GetCandidateTokenIndices"/>) receive it. One random
        /// valid local cell index per token, same convention as every
        /// Tag*TokensRandom method — Tinted is the one kind that also needs
        /// its target color pinned to the token's own (see
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
                _deck[deckIndex] = token.WithTrait(trait);
            }
        }
    }
}
