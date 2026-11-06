namespace Contigu.Core
{
    /// <summary>
    /// A piece's orientation, in quarter-turns counter-clockwise from its shape's base
    /// (unrotated) layout in <see cref="PieceShapeCatalog"/>. Assigned randomly
    /// per hand slot by <see cref="DeckManager"/> when a piece is drawn — the
    /// player can't rotate a piece themselves, only see and play whichever
    /// orientation it was dealt in.
    /// </summary>
    public enum PieceRotation
    {
        Deg0 = 0,
        Deg90 = 1,
        Deg180 = 2,
        Deg270 = 3
    }
}
