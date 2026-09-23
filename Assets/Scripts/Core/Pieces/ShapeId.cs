namespace Contigu.Core
{
    /// <summary>
    /// The 8 polyomino shapes (spec table 4.3's original 10, minus DomV and
    /// TriIV — explicit request, with a screenshot circling the horizontal/
    /// vertical pairs in the deck view: "j'aimerais vraiment que les deux
    /// versions soulignées ne soit qu'un seul et qu'il n'y ait qu'une seule
    /// version entre horizontal et verticale"). DomV and TriIV were already
    /// fully redundant with DomH and TriIH respectively: every piece gets a
    /// random <see cref="PieceRotation"/> when drawn (see its own doc
    /// comment), and rotating DomH/TriIH by 90° already produces exactly
    /// DomV's/TriIV's cell layout (see PieceShapeCatalog's now-removed
    /// GetRotated_RotatingADomino90Degrees_ProducesTheOtherDominosShape /
    /// GetRotated_RotatingAHorizontalTromino90Degrees_ProducesTheVerticalTrominosShape
    /// tests, which existed specifically to prove this). Having both as
    /// separate deck entries meant every color always showed two
    /// functionally-identical rows in the deck view.
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
