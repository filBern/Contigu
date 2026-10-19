namespace Contigu.Core
{
    /// <summary>The enemy roster (see EnemyId's own doc comment) — same All-array-plus-Get-loop convention as ChallengeCatalog.</summary>
    public static class EnemyCatalog
    {
        /// <summary>The round 1 encounter, teaching the damage-by-score mechanic with nothing else going on.</summary>
        public static readonly EnemyDefinition Basic = new EnemyDefinition { Id = EnemyId.Basic, Name = "Withered Stalk", Role = EnemyRole.Baseline, MaxHp = 250, Description = "No effect. Just absorbs damage.", Lore = "The last of the old harvest, still standing out of habit." };

        /// <summary>See RunManager.ResolveLockerShuffleEffect.</summary>
        public static readonly EnemyDefinition Locker = new EnemyDefinition { Id = EnemyId.Locker, Name = "Bramble", Role = EnemyRole.Creator, MaxHp = 600, Description = "Each Shuffle: locks 1 random plot (unplayable). The previous lock is removed first, so only 1 plot is locked at a time.", Lore = "It remembers every gardener who tried to cut it back." };

        /// <summary>See RunManager.ResolvePoisonerShuffleEffect. Can target any cell, filled or empty, and spreads via "contamination" when triggered (see RunManager.ContaminateAdjacentCell).</summary>
        public static readonly EnemyDefinition Poisoner = new EnemyDefinition { Id = EnemyId.Poisoner, Name = "Blightspore", Role = EnemyRole.Creator, MaxHp = 1350, Description = "Each Shuffle: blights 1 random plot, filled or empty.\nPoints scored on a blighted plot are negative, and the blight spreads to 1 adjacent plot when it happens.\nAll blight is removed on the next Shuffle.", Lore = "Where its spores drift, even clean soil forgets how to feed." };

        /// <summary>Mechanically Locker's own roaming single-cell lock (see RunManager.ResolveLockerShuffleEffect, reused as-is), just at real boss HP.</summary>
        public static readonly EnemyDefinition HeavyLocker = new EnemyDefinition { Id = EnemyId.HeavyLocker, Name = "Deadroot", Role = EnemyRole.Boss, MaxHp = 2000, Description = "Each Shuffle: locks 1 random plot (unplayable), like Bramble.\nThe lock is removed when it dies.", Lore = "Its roots reach deeper than the garden's first stone." };

        /// <summary>Poisoner's own roaming-poison mechanic (see RunManager.ResolvePoisonerShuffleEffect), scaled from 1 cell to 5; same any-tile-targeting and contamination behavior as Poisoner.</summary>
        public static readonly EnemyDefinition Plague = new EnemyDefinition { Id = EnemyId.Plague, Name = "Rotbloom", Role = EnemyRole.Boss, MaxHp = 3550, Description = "Round start: freezes 3 empty plots. A frozen plot can't be planted; harvesting its row or column breaks the ice.\nEach Shuffle: blights 5 random plots, filled or empty.\nPoints scored on a blighted plot are negative, and the blight spreads to 1 adjacent plot when it happens.\nAll blight is removed on the next Shuffle.", Lore = "Once the loveliest bloom in the garden, now it only knows how to spread." };

        /// <summary>See DeckManager.StealRandomHandTile/RunManager.ResolveThiefShuffleEffect.</summary>
        public static readonly EnemyDefinition Thief = new EnemyDefinition { Id = EnemyId.Thief, Name = "Crow", Role = EnemyRole.Boss, MaxHp = 1950, Description = "Each Shuffle: takes 1 random seed from your hand.\nBelow half HP it gets enraged and takes 2 instead.\nStolen seeds come back after a later Shuffle.", Lore = "It has watched you sow since the first frost." };

        /// <summary>See RunManager.HealReclaimer/ApplyPoisonScoreRule. Has no On-Shuffle effect of its own. Also grows on overflow heal — see EnemyInstance.HealOrGrow.</summary>
        public static readonly EnemyDefinition Reclaimer = new EnemyDefinition { Id = EnemyId.Reclaimer, Name = "Creeping Vine", Role = EnemyRole.Boss, MaxHp = 2150, Description = "Shielded: the first hit it takes is absorbed, however big.\nHeals by the amount of points lost on blighted plots.\nAt full HP, it gains max HP instead.", Lore = "It has been waiting for the garden to fail, and it is patient." };

        /// <summary>Not Boss-tagged, unlike the other new enemies above. See RunManager.HealLeech. Grow-on-overflow-heal mechanic shared with Reclaimer's own EnemyInstance.HealOrGrow.</summary>
        public static readonly EnemyDefinition Leech = new EnemyDefinition { Id = EnemyId.Leech, Name = "Root Leech", Role = EnemyRole.Reactor, MaxHp = 1150, Description = "Heals " + ScoringConstants.LeechHealPerLineClear + " HP each time you harvest a row or column.\nAt full HP, it gains max HP instead.", Lore = "Every harvest you bring in is a meal it never had to plant." };

        /// <summary>Picks one random base color on spawn (see RunManager.BuildEncounter) and cancels every point event tied to it for the rest of the round (see RunManager.ApplyCursedColorScoreRule). The HUD names the actual picked color (see HudView.SetEncounter).</summary>
        public static readonly EnemyDefinition ColorHater = new EnemyDefinition { Id = EnemyId.ColorHater, Name = "Invasive Bloom", Role = EnemyRole.Boss, MaxHp = 1950, Description = "Picks 1 random color when the round starts.\nPoints scored on plots of that color are cancelled for the whole round.", Lore = "It came from a garden where only one color was allowed to bloom." };

        /// <summary>Color Hater's exact mirror, keyed by which piece-shape originally filled each cell (see Cell.FilledShapeId) instead of its color (see RunManager.ApplyShapeHaterScoreRule).</summary>
        public static readonly EnemyDefinition ShapeHater = new EnemyDefinition { Id = EnemyId.ShapeHater, Name = "Invasive Weed", Role = EnemyRole.Boss, MaxHp = 1950, Description = "Picks 1 random seed shape when the round starts.\nPoints scored on plots planted by that shape are cancelled for the whole round.", Lore = "It grows in straight lines and cannot forgive anything that doesn't fit." };

        /// <summary>See RunManager.ResolveBlightmothShuffleEffect: a blighted fertilizer scores nothing, stopping a run from leaning on a single technique.</summary>
        public static readonly EnemyDefinition Blightmoth = new EnemyDefinition { Id = EnemyId.Blightmoth, Name = "Blightmoth", Role = EnemyRole.Boss, MaxHp = 800, Description = "Each Shuffle: blights 1 random fertilizer.\nA blighted fertilizer has no effect.\nThe blight is removed on the next Shuffle, or when it dies.", Lore = "It only ever eats what you have come to rely on." };

        private static readonly EnemyDefinition[] All = { Basic, Locker, Poisoner, HeavyLocker, Plague, Thief, Reclaimer, Leech, ColorHater, ShapeHater, Blightmoth };

        public static EnemyDefinition Get(EnemyId id)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Id == id)
                {
                    return All[i];
                }
            }
            return Basic;
        }
    }
}
