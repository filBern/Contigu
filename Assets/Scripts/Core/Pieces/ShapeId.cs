namespace Contigu.Core
{
    /// <summary>
    /// The 10 polyomino shapes from spec table 4.3. Directional variants (e.g. a
    /// horizontal vs. vertical domino) are modeled as distinct shapes since pieces
    /// are never rotated at runtime.
    /// </summary>
    public enum ShapeId
    {
        Single,
        DomH,
        DomV,
        TriL,
        TriIH,
        TriIV,
        Sq2,
        LTetro,
        TTetro,
        STetro
    }
}
