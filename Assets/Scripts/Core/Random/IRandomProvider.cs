namespace Contigu.Core
{
    /// <summary>
    /// Thin abstraction over a random source so game logic can be unit tested
    /// deterministically (inject a seeded provider) while runtime uses a real one.
    /// </summary>
    public interface IRandomProvider
    {
        /// <summary>Returns an integer in [0, maxExclusive).</summary>
        int Next(int maxExclusive);
    }
}
