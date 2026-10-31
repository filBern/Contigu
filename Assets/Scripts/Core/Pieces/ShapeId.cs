namespace Contigu.Core
{
    /// <summary>
    /// The 8 polyomino shapes. There is no separate vertical Domino/Tromino
    /// entry: every piece gets a random <see cref="PieceRotation"/> when
    /// drawn (see its own doc comment), and rotating DomH/TriIH by 90°
    /// already produces that cell layout.
    /// </summary>
    public enum ShapeId
    {
        Single,
        DomH,
        TriL,
        TriIH,
        Sq2,
        LTetro,
        TTetro,
        STetro
    }
}
