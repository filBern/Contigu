using System;
using System.Collections.Generic;

namespace Contigu.Core
{
    /// <summary>
    /// A single (shape, color) token living in the deck / draw pile / hand.
    /// Tokens are value types: two tokens of the same shape+color (and the same upgrades) are
    /// interchangeable, which is all the "Retirer/Dupliquer/Recolorer" upgrades need.
    /// A token can carry several tile upgrades, each tagging a DIFFERENT cell of its own shape:
    /// <see cref="Trait"/> is the first one, <see cref="ExtraTraits"/> the others. Each one fires when this
    /// specific token is placed (and its tile keeps counting afterwards, see RunManager.TileUpgrades).
    /// </summary>
    public readonly struct PieceToken : IEquatable<PieceToken>
    {
        private static readonly PieceTrait[] NoTraits = new PieceTrait[0];

        public readonly ShapeId Shape;
        public readonly PieceColor Color;
        public readonly PieceTrait? Trait;

        /// <summary>Further tile upgrades on other cells of the same piece (never null once built through a constructor; may be empty).</summary>
        public readonly PieceTrait[] ExtraTraits;

        public PieceToken(ShapeId shape, PieceColor color)
            : this(shape, color, null, null)
        {
        }

        public PieceToken(ShapeId shape, PieceColor color, PieceTrait? trait)
            : this(shape, color, trait, null)
        {
        }

        public PieceToken(ShapeId shape, PieceColor color, PieceTrait? trait, PieceTrait[] extraTraits)
        {
            Shape = shape;
            Color = color;
            Trait = trait;
            ExtraTraits = extraTraits ?? NoTraits;
        }

        /// <summary>Every upgrade on this piece, first one first.</summary>
        public IReadOnlyList<PieceTrait> Traits
        {
            get
            {
                if (!Trait.HasValue)
                {
                    return NoTraits;
                }
                var all = new PieceTrait[1 + (ExtraTraits == null ? 0 : ExtraTraits.Length)];
                all[0] = Trait.Value;
                for (int i = 0; ExtraTraits != null && i < ExtraTraits.Length; i++)
                {
                    all[i + 1] = ExtraTraits[i];
                }
                return all;
            }
        }

        public int TraitCount
        {
            get { return Trait.HasValue ? 1 + (ExtraTraits == null ? 0 : ExtraTraits.Length) : 0; }
        }

        /// <summary>This token with <paramref name="trait"/> as its only upgrade (anything it carried is replaced).</summary>
        public PieceToken WithTrait(PieceTrait trait)
        {
            return new PieceToken(Shape, Color, trait, null);
        }

        /// <summary>This token with <paramref name="trait"/> added to the upgrades it already carries (it must tag a cell no other upgrade uses).</summary>
        public PieceToken WithAddedTrait(PieceTrait trait)
        {
            if (!Trait.HasValue)
            {
                return new PieceToken(Shape, Color, trait, null);
            }
            var extras = new PieceTrait[(ExtraTraits == null ? 0 : ExtraTraits.Length) + 1];
            for (int i = 0; ExtraTraits != null && i < ExtraTraits.Length; i++)
            {
                extras[i] = ExtraTraits[i];
            }
            extras[extras.Length - 1] = trait;
            return new PieceToken(Shape, Color, Trait, extras);
        }

        /// <summary>Same piece in another color, keeping every upgrade.</summary>
        public PieceToken WithColor(PieceColor color)
        {
            return new PieceToken(Shape, color, Trait, ExtraTraits);
        }

        /// <summary>True when <paramref name="localCellIndex"/> of this piece already carries an upgrade.</summary>
        public bool UsesCell(int localCellIndex)
        {
            var all = Traits;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].LocalCellIndex == localCellIndex)
                {
                    return true;
                }
            }
            return false;
        }

        public bool Matches(ShapeId shape, PieceColor color)
        {
            return Shape == shape && Color == color;
        }

        public bool Equals(PieceToken other)
        {
            if (Shape != other.Shape || Color != other.Color || !Equals(Trait, other.Trait))
            {
                return false;
            }
            int a = ExtraTraits == null ? 0 : ExtraTraits.Length;
            int b = other.ExtraTraits == null ? 0 : other.ExtraTraits.Length;
            if (a != b)
            {
                return false;
            }
            for (int i = 0; i < a; i++)
            {
                if (!ExtraTraits[i].Equals(other.ExtraTraits[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is PieceToken other && Equals(other);
        }

        public override int GetHashCode()
        {
            int hash = ((int)Shape * 397) ^ (int)Color;
            if (Trait.HasValue)
            {
                hash = (hash * 397) ^ (int)Trait.Value.Kind;
            }
            return hash;
        }

        public override string ToString()
        {
            return Shape + "/" + Color;
        }
    }
}
