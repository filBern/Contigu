namespace Contigu.Core
{
    /// <summary>The enemy roster (see EnemyId's own doc comment) — same All-array-plus-Get-loop convention as ChallengeCatalog.</summary>
    public static class EnemyCatalog
    {
        /// <summary>The round 1 encounter, teaching the damage-by-score mechanic with nothing else going on.</summary>
        public static readonly EnemyDefinition Basic = new EnemyDefinition { Id = EnemyId.Basic, Name = "Withered Stalk", Role = EnemyRole.Baseline, MaxHp = 250, Description = "A dying stalk with nothing left to fight back with. No special effect — a straightforward target to learn the ropes on." };

        /// <summary>See RunManager.ResolveLockerShuffleEffect.</summary>
        public static readonly EnemyDefinition Locker = new EnemyDefinition { Id = EnemyId.Locker, Name = "Bramble", Role = EnemyRole.Creator, MaxHp = 750, Description = "Chokes one plot with thorny overgrowth every Shuffle. The previous growth is cleared first, so only ever one plot is choked at a time." };

        /// <summary>See RunManager.ResolvePoisonerShuffleEffect. Can target any cell, filled or empty, and spreads via "contamination" when triggered (see RunManager.ContaminateAdjacentCell).</summary>
        public static readonly EnemyDefinition Poisoner = new EnemyDefinition { Id = EnemyId.Poisoner, Name = "Blightspore", Role = EnemyRole.Creator, MaxHp = 1350, Description = "Taints one plot with blight every Shuffle (planted or bare). Points scored through a blighted tile count negative instead of positive, and scoring through one spreads the rot to a random neighboring tile too — all of it clears on the next Shuffle." };

        /// <summary>Mechanically Locker's own roaming single-cell lock (see RunManager.ResolveLockerShuffleEffect, reused as-is), just at real boss HP.</summary>
        public static readonly EnemyDefinition HeavyLocker = new EnemyDefinition { Id = EnemyId.HeavyLocker, Name = "Deadroot", Role = EnemyRole.Boss, MaxHp = 2350, Description = "Chokes one plot with thorny overgrowth every Shuffle — the same roaming growth as Bramble, just far tougher. Killing it clears the growth immediately." };

        /// <summary>Poisoner's own roaming-poison mechanic (see RunManager.ResolvePoisonerShuffleEffect), scaled from 1 cell to 5; same any-tile-targeting and contamination behavior as Poisoner.</summary>
        public static readonly EnemyDefinition Plague = new EnemyDefinition { Id = EnemyId.Plague, Name = "Rotbloom", Role = EnemyRole.Boss, MaxHp = 3550, Description = "Spreads blight across 5 plots at once every Shuffle, planted or bare (clearing its previous 5 and any spread rot first). Points scored through any of them count negative, and spread to a random neighboring tile." };

        /// <summary>See DeckManager.StealRandomHandTile/RunManager.ResolveThiefShuffleEffect.</summary>
        public static readonly EnemyDefinition Thief = new EnemyDefinition { Id = EnemyId.Thief, Name = "Crow", Role = EnemyRole.Boss, MaxHp = 1950, Description = "Swoops in every Shuffle and snatches 1 random seed from your hand — it's not lost, just out of reach until a later Shuffle turns it up again." };

        /// <summary>See RunManager.HealReclaimer/ApplyPoisonScoreRule. Has no On-Shuffle effect of its own. Also grows on overflow heal — see EnemyInstance.HealOrGrow.</summary>
        public static readonly EnemyDefinition Reclaimer = new EnemyDefinition { Id = EnemyId.Reclaimer, Name = "Creeping Vine", Role = EnemyRole.Boss, MaxHp = 2150, Description = "Feeds on rot — heals by the exact amount any blighted tile's points get flipped negative, anywhere on the garden. If it's already at full health, that feeding grows its max HP instead — the more you're forced to play into blight once it's topped up, the tougher it grows." };

        /// <summary>Not Boss-tagged, unlike the other new enemies above. See RunManager.HealLeech. Grow-on-overflow-heal mechanic shared with Reclaimer's own EnemyInstance.HealOrGrow.</summary>
        public static readonly EnemyDefinition Leech = new EnemyDefinition { Id = EnemyId.Leech, Name = "Root Leech", Role = EnemyRole.Reactor, MaxHp = 1150, Description = "Draws " + ScoringConstants.LeechHealPerLineClear + " HP from the soil every time you clear a row or column, for as long as it's alive. If it's already at full health, that draw grows its max HP instead." };

        /// <summary>Picks one random base color on spawn (see RunManager.BuildEncounter) and cancels every point event tied to it for the rest of the round (see RunManager.ApplyCursedColorScoreRule). The HUD names the actual picked color (see HudView.SetEncounter).</summary>
        public static readonly EnemyDefinition ColorHater = new EnemyDefinition { Id = EnemyId.ColorHater, Name = "Invasive Bloom", Role = EnemyRole.Boss, MaxHp = 1950, Description = "Rejects one bloom color at random on taking root. Every point scored through a tile of that color withers away for the rest of the round." };

        /// <summary>Color Hater's exact mirror, keyed by which piece-shape originally filled each cell (see Cell.FilledShapeId) instead of its color (see RunManager.ApplyShapeHaterScoreRule).</summary>
        public static readonly EnemyDefinition ShapeHater = new EnemyDefinition { Id = EnemyId.ShapeHater, Name = "Invasive Weed", Role = EnemyRole.Boss, MaxHp = 1950, Description = "Rejects one seed shape at random on taking root. Every point scored through a tile originally planted by that shape withers away for the rest of the round." };

        private static readonly EnemyDefinition[] All = { Basic, Locker, Poisoner, HeavyLocker, Plague, Thief, Reclaimer, Leech, ColorHater, ShapeHater };

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
