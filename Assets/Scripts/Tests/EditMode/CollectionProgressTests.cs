using Contigu.Core;
using NUnit.Framework;

namespace Contigu.Tests
{
    public sealed class CollectionProgressTests
    {
        [Test]
        public void Discover_IsTrueOnlyTheFirstTime()
        {
            var progress = new CollectionProgress();
            Assert.IsTrue(progress.Discover(CollectionCategory.Fertilizer, 5));
            Assert.IsFalse(progress.Discover(CollectionCategory.Fertilizer, 5));
            Assert.IsTrue(progress.Discover(CollectionCategory.Sprout, 5), "Categories are separate");
            Assert.AreEqual(1, progress.Count(CollectionCategory.Fertilizer));
        }

        [Test]
        public void Text_RoundTrips()
        {
            var progress = new CollectionProgress();
            progress.Discover(CollectionCategory.Enemy, 3);
            progress.Discover(CollectionCategory.Weather, 12);
            progress.Discover(CollectionCategory.Weather, 4);
            var loaded = CollectionProgress.FromText(progress.ToText());
            Assert.IsTrue(loaded.IsDiscovered(CollectionCategory.Enemy, 3));
            Assert.IsTrue(loaded.IsDiscovered(CollectionCategory.Weather, 12));
            Assert.IsFalse(loaded.IsDiscovered(CollectionCategory.Weather, 5));
            Assert.AreEqual(progress.ToText(), loaded.ToText());
        }

        [Test]
        public void FromText_ToleratesGarbage()
        {
            Assert.AreEqual(0, CollectionProgress.FromText(null).Count(CollectionCategory.Fertilizer));
            Assert.AreEqual(0, CollectionProgress.FromText("zzz\n99=1,2\n0=x,y").Count(CollectionCategory.Fertilizer));
        }
    }
}
