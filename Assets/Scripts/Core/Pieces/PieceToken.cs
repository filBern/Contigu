namespace Contigu.Core
{
    /// <summary>
    /// A single (shape, color) token living in the deck / draw pile / hand.
    /// Tokens are value types: two tokens of the same shape+color are
    /// interchangeable, which is all the "Retirer/Dupliquer/Recolorer" upgrades need.
    /// An optional <see cref="Trait"/> tags one of the token's own cells with a
    /// one-time golden/tinted/multiplier enchantment (spec 5.4 redesign) that
    /// fires when this specific token is placed.
    /// </summary>
    public readonly struct PieceToken
    {
        public readonly ShapeId Shape;
        public readonly PieceColor Color;
        public readonly PieceTrait? Trait;

        public PieceToken(ShapeId shape, PieceColor color)
            : this(shape, color, null)
        {
        }

        public PieceToken(ShapeId shape, PieceColor color, PieceTrait? trait)
        {
            Shape = shape;
            Color = color;
            Trait = trait;
        }

        public PieceToken WithTrait(PieceTrait trait)
        {
            return new PieceToken(Shape, Color, trait);
        }

        public bool Matches(ShapeId shape, PieceColor color)
        {
            return Shape == shape && Color == color;
        }

        public override string ToString()
        {
            return Shape + "/" + Color;
        }
    }
}
