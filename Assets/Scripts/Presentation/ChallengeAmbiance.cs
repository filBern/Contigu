using Contigu.Core;
using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>What one particle family of a challenge's ambiance looks like (see AmbientLifeView.ParticleKind).</summary>
    public struct AmbientSpec
    {
        public AmbientLifeView.ParticleKind Kind;
        public int Count;
        public Color Tint;

        public AmbientSpec(AmbientLifeView.ParticleKind kind, int count, Color tint)
        {
            Kind = kind;
            Count = count;
            Tint = tint;
        }
    }

    /// <summary>
    /// The look of one challenge's world behind the board: a full-screen scenery (Resources/Backgrounds/&lt;SceneryName&gt;.png), the tint of
    /// the slow drifting petals/decor and fireflies of AnimatedBackgroundView, and which creatures and particles roam (AmbientLifeView) —
    /// Spring a blossoming night garden, Winter snowfall under an aurora, Summer a hot dusk of sun motes and bees, Autumn drifting maple leaves
    /// under a harvest moon, Orchard falling blossom between apple trees, Greenhouse condensation drips under glass.
    /// </summary>
    public sealed class ChallengeAmbiance
    {
        public string SceneryName;

        /// <summary>Tints of the slow falling petals/leaves of the shared background; null keeps the four bloom colors (Spring).</summary>
        public Color[] FallingTints;

        /// <summary>Tone of the big faint decor shapes (blended over the base background).</summary>
        public Color DecorTone;

        public float DecorAmount;
        public Color FireflyColor;

        /// <summary>How many butterflies can visit (0 in a frozen or glassed-in world).</summary>
        public int MaxButterflies;

        /// <summary>Bees that always buzz about (on top of the ones fine weather brings).</summary>
        public int Bees;

        public Color ButterflyTint;
        public AmbientSpec[] Particles;

        private static readonly ChallengeAmbiance Spring = new ChallengeAmbiance
        {
            SceneryName = "Spring",
            FallingTints = null,
            DecorTone = new Color(0.35f, 0.62f, 0.4f),
            DecorAmount = 0.22f,
            FireflyColor = new Color(1f, 0.85f, 0.4f),
            MaxButterflies = 5,
            Bees = 0,
            ButterflyTint = Color.white,
            Particles = new AmbientSpec[0]
        };

        private static readonly ChallengeAmbiance Winter = new ChallengeAmbiance
        {
            SceneryName = "Winter",
            FallingTints = new[] { new Color(0.88f, 0.95f, 1f), new Color(0.7f, 0.86f, 1f), new Color(0.8f, 0.9f, 1f) },
            DecorTone = new Color(0.6f, 0.8f, 1f),
            DecorAmount = 0.2f,
            FireflyColor = new Color(0.72f, 0.92f, 1f),
            MaxButterflies = 0,
            Bees = 0,
            ButterflyTint = Color.white,
            Particles = new[]
            {
                new AmbientSpec(AmbientLifeView.ParticleKind.Snow, 46, Color.white),
                new AmbientSpec(AmbientLifeView.ParticleKind.Sparkle, 8, new Color(0.8f, 0.93f, 1f))
            }
        };

        private static readonly ChallengeAmbiance Summer = new ChallengeAmbiance
        {
            SceneryName = "Summer",
            FallingTints = new[] { new Color(1f, 0.82f, 0.38f), new Color(1f, 0.62f, 0.3f), new Color(0.95f, 0.5f, 0.35f) },
            DecorTone = new Color(1f, 0.6f, 0.3f),
            DecorAmount = 0.22f,
            FireflyColor = new Color(1f, 0.78f, 0.3f),
            MaxButterflies = 3,
            Bees = 3,
            ButterflyTint = new Color(1f, 0.8f, 0.55f),
            Particles = new[] { new AmbientSpec(AmbientLifeView.ParticleKind.SunMote, 16, new Color(1f, 0.85f, 0.45f)) }
        };

        private static readonly ChallengeAmbiance Autumn = new ChallengeAmbiance
        {
            SceneryName = "Autumn",
            FallingTints = new[] { new Color(0.95f, 0.52f, 0.2f), new Color(0.82f, 0.32f, 0.2f), new Color(0.65f, 0.42f, 0.2f), new Color(0.95f, 0.75f, 0.3f) },
            DecorTone = new Color(0.85f, 0.45f, 0.2f),
            DecorAmount = 0.2f,
            FireflyColor = new Color(1f, 0.6f, 0.25f),
            MaxButterflies = 0,
            Bees = 0,
            ButterflyTint = Color.white,
            Particles = new[]
            {
                new AmbientSpec(AmbientLifeView.ParticleKind.Maple, 10, Color.white),
                new AmbientSpec(AmbientLifeView.ParticleKind.Maple, 6, new Color(1f, 0.85f, 0.6f)),
                new AmbientSpec(AmbientLifeView.ParticleKind.Maple, 4, new Color(0.8f, 0.6f, 0.45f))
            }
        };

        private static readonly ChallengeAmbiance Orchard = new ChallengeAmbiance
        {
            SceneryName = "Orchard",
            FallingTints = new[] { new Color(1f, 0.74f, 0.82f), new Color(1f, 0.92f, 0.95f), new Color(0.75f, 0.92f, 0.6f) },
            DecorTone = new Color(0.55f, 0.85f, 0.55f),
            DecorAmount = 0.2f,
            FireflyColor = new Color(0.92f, 1f, 0.6f),
            MaxButterflies = 4,
            Bees = 2,
            ButterflyTint = new Color(1f, 0.85f, 0.9f),
            Particles = new[] { new AmbientSpec(AmbientLifeView.ParticleKind.Petal, 18, Color.white) }
        };

        private static readonly ChallengeAmbiance Greenhouse = new ChallengeAmbiance
        {
            SceneryName = "Greenhouse",
            FallingTints = new[] { new Color(0.62f, 0.95f, 0.8f), new Color(0.5f, 0.85f, 0.9f), new Color(0.75f, 1f, 0.85f) },
            DecorTone = new Color(0.4f, 0.8f, 0.7f),
            DecorAmount = 0.24f,
            FireflyColor = new Color(0.7f, 1f, 0.9f),
            MaxButterflies = 0,
            Bees = 1,
            ButterflyTint = Color.white,
            Particles = new[]
            {
                new AmbientSpec(AmbientLifeView.ParticleKind.Drip, 14, new Color(0.7f, 0.96f, 1f)),
                new AmbientSpec(AmbientLifeView.ParticleKind.Sparkle, 8, new Color(0.7f, 1f, 0.85f))
            }
        };

        public static ChallengeAmbiance For(ChallengeId id)
        {
            switch (id)
            {
                case ChallengeId.Marathon: return Winter;
                case ChallengeId.Summer: return Summer;
                case ChallengeId.Autumn: return Autumn;
                case ChallengeId.Orchard: return Orchard;
                case ChallengeId.Greenhouse: return Greenhouse;
                default: return Spring;
            }
        }
    }
}
