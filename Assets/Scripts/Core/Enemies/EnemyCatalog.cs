namespace Contigu.Core
{
    /// <summary>The enemy roster (see EnemyId's own doc comment) — same All-array-plus-Get-loop convention as ChallengeCatalog.</summary>
    public static class EnemyCatalog
    {
        /// <summary>The round 1 encounter, teaching the damage-by-score mechanic with nothing else going on.</summary>
        public static readonly EnemyDefinition Basic = new EnemyDefinition { Id = EnemyId.Basic, Name = "Basic", Role = EnemyRole.Baseline, MaxHp = 338, Description = "No special effect. A straightforward target to learn the ropes on." };

        /// <summary>See RunManager.ResolveLockerShuffleEffect.</summary>
        public static readonly EnemyDefinition Locker = new EnemyDefinition { Id = EnemyId.Locker, Name = "Locker", Role = EnemyRole.Creator, MaxHp = 900, Description = "On each Shuffle, locks 1 grid cell. The previous lock is released first, so only ever one cell is locked at a time." };

        /// <summary>See RunManager.ResolvePoisonerShuffleEffect. Can target any cell, filled or empty, and spreads via "contamination" when triggered (see RunManager.ContaminateAdjacentCell).</summary>
        public static readonly EnemyDefinition Poisoner = new EnemyDefinition { Id = EnemyId.Poisoner, Name = "Poisoner", Role = EnemyRole.Creator, MaxHp = 1575, Description = "On each Shuffle, poisons 1 tile (filled or empty). Points scored through a poisoned tile count negative instead of positive, and scoring through one spreads it to a random adjacent tile too — all of it clears on the next Shuffle." };

        /// <summary>Mechanically Locker's own roaming single-cell lock (see RunManager.ResolveLockerShuffleEffect, reused as-is), just at real boss HP.</summary>
        public static readonly EnemyDefinition HeavyLocker = new EnemyDefinition { Id = EnemyId.HeavyLocker, Name = "Heavy Locker", Role = EnemyRole.Boss, MaxHp = 2700, Description = "On each Shuffle, locks 1 grid cell — same roaming lock as Locker, just far tougher. Killing it releases the lock immediately." };

        /// <summary>Poisoner's own roaming-poison mechanic (see RunManager.ResolvePoisonerShuffleEffect), scaled from 1 cell to 5; same any-tile-targeting and contamination behavior as Poisoner.</summary>
        public static readonly EnemyDefinition Plague = new EnemyDefinition { Id = EnemyId.Plague, Name = "Plague", Role = EnemyRole.Boss, MaxHp = 4050, Description = "On each Shuffle, poisons 5 tiles at once, filled or empty (releasing its previous 5 and any contamination first). Points scored through any of them count negative, and spread to a random adjacent tile." };

        /// <summary>See DeckManager.StealRandomHandTile/RunManager.ResolveThiefShuffleEffect.</summary>
        public static readonly EnemyDefinition Thief = new EnemyDefinition { Id = EnemyId.Thief, Name = "Thief", Role = EnemyRole.Boss, MaxHp = 2250, Description = "On each Shuffle, steals 1 random piece from your hand — it's not lost, just unavailable until a later Shuffle draws it again." };

        /// <summary>See RunManager.HealReclaimer/ApplyPoisonScoreRule. Has no On-Shuffle effect of its own. Also grows on overflow heal — see EnemyInstance.HealOrGrow.</summary>
        public static readonly EnemyDefinition Reclaimer = new EnemyDefinition { Id = EnemyId.Reclaimer, Name = "Reclaimer", Role = EnemyRole.Boss, MaxHp = 2475, Description = "Heals by the exact amount any poisoned tile's points get flipped negative, anywhere on the board. If it's already at full health, that heal grows its max HP instead — the more you're forced to play into poison once it's topped up, the tougher it gets." };

        /// <summary>Not Boss-tagged, unlike the other new enemies above. See RunManager.HealLeech. Grow-on-overflow-heal mechanic shared with Reclaimer's own EnemyInstance.HealOrGrow.</summary>
        public static readonly EnemyDefinition Leech = new EnemyDefinition { Id = EnemyId.Leech, Name = "Leech", Role = EnemyRole.Reactor, MaxHp = 1350, Description = "Heals " + ScoringConstants.LeechHealPerLineClear + " HP every time you clear a row or column, for as long as it's alive. If it's already at full health, that heal grows its max HP instead." };

        /// <summary>Picks one random base color on spawn (see RunManager.BuildEncounter) and cancels every point event tied to it for the rest of the round (see RunManager.ApplyCursedColorScoreRule). The HUD names the actual picked color (see HudView.SetEncounter).</summary>
        public static readonly EnemyDefinition ColorHater = new EnemyDefinition { Id = EnemyId.ColorHater, Name = "Color Hater", Role = EnemyRole.Boss, MaxHp = 2250, Description = "Hates one color at random. Every point scored through a tile of that color is cancelled for the rest of the round." };

        /// <summary>Color Hater's exact mirror, keyed by which piece-shape originally filled each cell (see Cell.FilledShapeId) instead of its color (see RunManager.ApplyShapeHaterScoreRule).</summary>
        public static readonly EnemyDefinition ShapeHater = new EnemyDefinition { Id = EnemyId.ShapeHater, Name = "Shape Hater", Role = EnemyRole.Boss, MaxHp = 2250, Description = "Hates one piece shape at random. Every point scored through a tile originally placed by that shape is cancelled for the rest of the round." };

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
