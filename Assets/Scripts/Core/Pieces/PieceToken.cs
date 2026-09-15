namespace Contigu.Core
{
    /// <summary>
    /// A single (shape, color) token living in the deck / draw pile / hand.
    /// Tokens are value types: two tokens of the same shape+color are
    /// interchangeable, which is all the "Retirer/Dupliquer/Recolorer" upgrades need.
    /// </summary>
    public readonly struct PieceToken
    {
        public readonly ShapeId Shape;
        public readonly PieceColor Color;

        public PieceToken(ShapeId shape, PieceColor color)
        {
            Shape = shape;
            Color = color;
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
