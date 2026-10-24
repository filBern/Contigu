namespace Contigu.Core
{
    /// <summary>One of the two levelings offered by Piece Mastery (<see cref="Shape"/> set) or Color Mastery (<see cref="Color"/> set), with the number of levels it would grant.</summary>
    public struct MasteryOption
    {
        public ShapeId? Shape;
        public PieceColor? Color;
        public int Gain;
    }
}
