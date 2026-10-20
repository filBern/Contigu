namespace Contigu.Core
{
    /// <summary>
    /// The enemy roster. HeavyLocker/Plague/Thief/Reclaimer are Boss-tagged
    /// escalations of Locker/Poisoner plus two new mechanics (see
    /// EnemyRole). Leech is a non-Boss Reactor. ColorHater/ShapeHater are a
    /// further pair of bosses — see EnemyInstance.HatedColor/HatedShape and
    /// RunManager.ApplyCursedColorScoreRule/ApplyShapeHaterScoreRule.
    /// </summary>
    public enum EnemyId
    {
        Basic,
        Locker,
        Poisoner,
        HeavyLocker,
        Plague,
        Thief,
        Reclaimer,
        Leech,
        ColorHater,
        ShapeHater,
        /// <summary>Blights one of the player's modifiers each Shuffle (see RunManager.ResolveBlightmothShuffleEffect).</summary>
        Blightmoth,

        // One resident per challenge "world" (see ChallengeDefinition.WorldEnemy): each only ever shows up in its own challenge.
        /// <summary>Winter: freezes an empty plot each Shuffle (see RunManager.ResolveHoarfrostShuffleEffect).</summary>
        Hoarfrost,

        /// <summary>Summer: devours Pollen each Shuffle.</summary>
        Locust,

        /// <summary>Autumn: gnaws a fertilizer's remaining life each Shuffle.</summary>
        Rotgrub,

        /// <summary>Orchard: turns a planted tile into the other color each Shuffle.</summary>
        Fruitworm,

        /// <summary>Greenhouse: swaps two planted tiles each Shuffle.</summary>
        Whitefly
    }
}
