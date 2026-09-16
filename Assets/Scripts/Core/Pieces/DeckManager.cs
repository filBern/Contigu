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
        private readonly List<PieceToken> _hand = new List<PieceToken>();
        private readonly List<PieceRotation> _handRotations = new List<PieceRotation>();
        private readonly IRandomProvider _rng;

        public IReadOnlyList<PieceToken> Deck
        {
            get { return _deck; }
        }

        public IReadOnlyList<PieceToken> Hand
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
                    // Deck is empty (should not happen given MinDeckSize > 0).
                    break;
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
        /// Removes the piece at <paramref name="handIndex"/> from the hand. Per
        /// spec 4.2, a fresh hand of 3 is only drawn once the hand is fully empty.
        /// </summary>
        public void PlayFromHand(int handIndex)
        {
            _hand.RemoveAt(handIndex);
            _handRotations.RemoveAt(handIndex);
            if (_hand.Count == 0)
            {
                DrawNewHand();
            }
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

        public void AddJoker()
        {
            AddToken(new PieceToken(ShapeId.Single, PieceColor.Joker));
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
    }
}
