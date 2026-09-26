namespace Contigu.Core
{
    /// <summary>
    /// Extra input the player supplies to resolve an upgrade that
    /// <see cref="UpgradeDefinition.RequiresSubChoice"/>: the piece type to act on
    /// (Retirer/Dupliquer/Recolorer) and, for Recolorer, the target color.
    /// </summary>
    public readonly struct UpgradeSubChoice
    {
        public readonly ShapeId Shape;
        public readonly PieceColor Color;
        public readonly PieceColor TargetColor;

        public UpgradeSubChoice(ShapeId shape, PieceColor color, PieceColor targetColor = PieceColor.Coral)
        {
            Shape = shape;
            Color = color;
            TargetColor = targetColor;
        }
    }
}
