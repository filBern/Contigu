namespace Contigu.Core
{
    /// <summary>
    /// Mastery (Piece Mastery/Color Mastery) has its own pool — see
    /// UpgradeCatalog.MasteryPool and UpgradeVisualDefaults.GetPoolLabel for
    /// its "Mastery Upgrade" label, and RunManager.RollUpgradeSlot for the
    /// shop's roll weighting between the pools. Modifier (Random Modifier/
    /// Modifier Upgrade) is split out the same way — see
    /// UpgradeCatalog.ModifierPool.
    /// </summary>
    public enum UpgradePool
    {
        Bank,
        Grid,
        Mastery,
        Modifier
    }

    /// <summary>The Bank upgrades and Grid (piece-enchantment) upgrades.</summary>
    public enum UpgradeId
    {
        /// <summary>
        /// A straight swap between two types already in the deck (see
        /// UpgradeSystem.Apply/DeckManager.ReplaceOneOfType): removing a
        /// type outright would shrink the deck, which could backfire;
        /// replacing it with a duplicate of another type already owned
        /// keeps the deck size exactly where it was.
        /// </summary>
        ReplacePiece,
        DuplicatePiece,
        JokerPiece,
        RecolorPiece,
        RandomModifier,

        RandomPiece,
        ModifierUpgrade,

        GoldenCells,
        TintedCells,
        MultiplierZone,
        BlastTile,
        MultiplierBeacon,
        MirrorTile,
        Seeder,

        CatalystTile,
        TwinTile,
        DetonatorTile,
        ChameleonTile,
        SparkTile,
        VoidTile,

        BastionTile,
        KamikazeTile,

        PieceMastery,

        ColorMastery
    }
}
