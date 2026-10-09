namespace Contigu.Core
{
    public sealed class SystemRandomProvider : IRandomProvider
    {
        private readonly System.Random _random;

        public SystemRandomProvider()
        {
            _random = new System.Random();
        }

        public SystemRandomProvider(int seed)
        {
            _random = new System.Random(seed);
        }

        public int Next(int maxExclusive)
        {
            return _random.Next(maxExclusive);
        }
    }
}
