namespace Contigu.Core
{
    /// <summary>
    /// Extra input the player supplies to resolve an upgrade that
    /// <see cref="UpgradeDefinition.RequiresSubChoice"/>: the piece type to act on
    /// (Replace/Dupliquer/Recolorer), and either a target color (Recolorer) or a
    /// second piece type (Replace: AddShape/AddColor name the existing deck
    /// type a duplicate of which replaces the one named by Shape/Color).
    /// </summary>
    public readonly struct UpgradeSubChoice
    {
        public readonly ShapeId Shape;
        public readonly PieceColor Color;
        public readonly PieceColor TargetColor;
        public readonly ShapeId AddShape;
        public readonly PieceColor AddColor;

        public UpgradeSubChoice(ShapeId shape, PieceColor color, PieceColor targetColor = PieceColor.Coral, ShapeId addShape = ShapeId.Single, PieceColor addColor = PieceColor.Coral)
        {
            Shape = shape;
            Color = color;
            TargetColor = targetColor;
            AddShape = addShape;
            AddColor = addColor;
        }
    }
}
