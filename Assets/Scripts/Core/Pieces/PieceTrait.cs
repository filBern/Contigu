namespace Contigu.Core
{
    /// <summary>Which effect a tagged piece's enchanted cell applies on placement.</summary>
    public enum PieceTraitKind
    {
        /// <summary>The cell scores a flat golden bonus (mirrors <see cref="Cell.IsGolden"/>).</summary>
        Golden,

        /// <summary>The cell doubles the group bonus if its own fill color matches (mirrors <see cref="Cell.IsTinted"/>).</summary>
        Tinted,

        /// <summary>The cell doubles the group bonus (mirrors <see cref="Cell.IsMultiplierZone"/>).</summary>
        Multiplier,

        /// <summary>"Blast Tile" — the cell AND its 4 orthogonal neighbors all score golden for this one placement.</summary>
        Blast,

        /// <summary>"Multiplier Beacon" — the cell, plus every already-filled cell in its row and column, become multiplier zones for this one placement.</summary>
        Beacon,

        /// <summary>"Mirror Tile" — this cell's own group-bonus contribution is also duplicated onto one random OTHER cell in the scored group.</summary>
        Mirror,

        /// <summary>"Seeder" — like <see cref="Golden"/>, but the golden flag isn't cleared right after scoring like every other trait: the grid cell stays golden for the rest of the CURRENT ROUND (cleared at the next round's reset — permanent for the whole run was too powerful).</summary>
        Seeder,

        /// <summary>"Catalyst Tile" — scores extra points for every cell in this placement's scored group that was already on the grid before this placement (group size minus this piece's own cell count).</summary>
        Catalyst,

        /// <summary>"Twin Tile" — like <see cref="Mirror"/>, but duplicates the enchanted cell's own group-bonus share onto EVERY other cell in the scored group, not just one at random.</summary>
        Twin,

        /// <summary>"Detonator Tile" — if this placement clears at least one row/column, doubles the WHOLE placement's line-clear bonus.</summary>
        Detonator,

        /// <summary>"Chameleon Tile" — if the enchanted cell has an already-filled orthogonal neighbor, the WHOLE piece is recolored to match it before scoring, merging into an existing group instead of keeping its own color.</summary>
        Chameleon,

        /// <summary>"Spark Tile" — scores more points the longer it's been (in placements) since the last row/column clear this round; resets once a clear happens.</summary>
        Spark,

        /// <summary>"Void Tile" — also clears one random already-filled, unlocked cell elsewhere on the grid (excluding this placement's own cells) when placed, scoring ScoringConstants.VoidBonusPerDestroyedCell for the broken tile.</summary>
        Void,

        /// <summary>"Bastion Tile" — once placed, this tile locks in place for the rest of the round: it's never cleared by a completed row/column, but it still scores the line-clear bonus every time one of those completes, as if it actually had been.</summary>
        Bastion,

        /// <summary>"Kamikaze Tile" — when placed, destroys itself and its 8 surrounding tiles (including this same placement's own other cells), scoring a flat bonus per tile actually destroyed.</summary>
        Kamikaze,

        /// <summary>"Venom Tile" — when placed (regardless of whether it scores), poisons every alive enemy for ScoringConstants.VenomDamagePercentOfMaxHp% of ITS OWN current max HP as direct damage — unlike the ordinary score-driven hit, this doesn't depend on this placement's score at all.</summary>
        Venom,

        /// <summary>"Pip Sprout" — +1 pt when planted.</summary>
        Pip,

        /// <summary>"Lucky Sprout" — +0 to +20 pts at random when planted.</summary>
        Lucky,

        /// <summary>"Companion Sprout" — +8 pts per touching tile of another color, +25 for its chosen color.</summary>
        Companion,

        /// <summary>"Colony Sprout" — +20 pts when its group has more than 10 cells.</summary>
        Colony,

        /// <summary>"Border Sprout" — +8 pts when planted on the garden's edge.</summary>
        Border,

        /// <summary>"Heart Sprout" — +12 pts when planted in the middle of the garden.</summary>
        Heart,

        /// <summary>"Corner Sprout" — +30 pts when planted on a corner.</summary>
        Corner,

        /// <summary>"Hermit Sprout" — +12 pts when no other tile touches it.</summary>
        Hermit,

        /// <summary>"Cuddly Sprout" — +3 pts per planted tile touching it.</summary>
        Cuddly,

        /// <summary>"Dawn Sprout" — +12 pts if planted among the first 3 seeds of the round.</summary>
        Dawn,

        /// <summary>"Dusk Sprout" — +20 pts if planted as the last seed of the round.</summary>
        Dusk,

        /// <summary>"Miser Sprout" — +1 pt per Pollen held, no cap.</summary>
        Miser,

        /// <summary>"Stormy Sprout" — +12 pts when the weather isn't Clear.</summary>
        Stormy,

        /// <summary>"Elder Sprout" — +2 pts per round already cleared this run.</summary>
        Elder,

        /// <summary>"Harvester Sprout" — +10 pts per line harvested by its seed.</summary>
        Harvester,

        /// <summary>"Breeze Sprout" — +3 pts per Shuffle you still have.</summary>
        Breeze,

        /// <summary>"Harmony Sprout" — +10 pts when every touching tile shares its color.</summary>
        Harmony,

        /// <summary>"Prism Sprout" — +20 pts when it touches 3 or more different colors.</summary>
        Prism,

        /// <summary>"Gardener Sprout" — +3 pts per fertilizer held.</summary>
        Gardener,

        /// <summary>"Cluster Sprout" — +6 pts per other upgraded tile in its group.</summary>
        Cluster,

        /// <summary>"Crossfire Sprout" — +1 pt per planted tile in its row and column.</summary>
        Crossfire,

        /// <summary>"Even Sprout" — +12 pts when its group has an even number of cells.</summary>
        Even,

        /// <summary>"Rainbow Row Sprout" — +15 pts when its row holds every color of the run.</summary>
        RainbowRow,

        /// <summary>"Sunseeker Sprout" — +10 pts on Sunshine and Golden Hour days (and their indoor variants).</summary>
        Sunseeker,

        /// <summary>"Hunter Sprout" — +3 pts per enemy still alive.</summary>
        Hunter,

        /// <summary>"Executioner Sprout" — +30 pts when the front enemy is down to a quarter of its HP or less.</summary>
        Executioner,

        /// <summary>"Spillover Sprout" — +25 pts when its tile is harvested by a line.</summary>
        Spillover,

        /// <summary>"Honeycomb Sprout" — +1 Pollen every time it scores.</summary>
        Honeycomb,

        /// <summary>"Dewdrop Sprout" — +2 Pollen per line harvested by its seed.</summary>
        Dewdrop,

        /// <summary>"Keystone Sprout" — +5 Mult when it sits on a corner or in the middle.</summary>
        Keystone,

        /// <summary>"Growth Sprout" — +1 pt, then +1 more every time it scores this round.</summary>
        Growth,

        /// <summary>"Gambler Sprout" — 50%: +40 pts. 50%: -10 pts.</summary>
        Gambler,

        /// <summary>"Warden Sprout" — Frees the 4 plots around it from enemy effects and makes them immune for the round.</summary>
        Warden,

        /// <summary>"Aegis Sprout" — Frees its own plot from enemy effects and makes it immune for the round.</summary>
        Aegis,

        /// <summary>"Sower Sprout" — Plants a free tile of its color on a neighboring empty plot.</summary>
        Sower,

        /// <summary>"Kin Sprout" — +4 pts per other upgraded tile touching it.</summary>
        Kin,

        /// <summary>"Amplifier Sprout" — Doubles the points of the upgraded tiles touching it.</summary>
        Amplifier,

        /// <summary>"Echo Sprout" — Copies the best points bonus among the upgraded tiles touching it.</summary>
        Echo,

        /// <summary>"Collector Sprout" — +3 pts per different upgrade in the garden.</summary>
        Collector,

        /// <summary>"Resonance Sprout" — +2 pts per upgraded tile in the garden.</summary>
        Resonance,

        /// <summary>"Pouch Sprout" — +1 pt per upgraded seed in your bag.</summary>
        Pouch,

        /// <summary>"Perennial Sprout" — +1 pt, then +1 more every time any Perennial scores, all run long.</summary>
        Perennial,

        /// <summary>"Crescendo Sprout" — +2 pts per seed already planted this round.</summary>
        Crescendo,

        /// <summary>"Scholar Sprout" — +1 pt per mastery level earned this run.</summary>
        Scholar,

        /// <summary>"Full House Sprout" — +12 pts for its row and +12 for its column when 6 or more plots of it are full.</summary>
        FullHouse,

        /// <summary>"Compound Sprout" — Earns 10% of the Pollen you hold (rounded down) every time it scores.</summary>
        Compound,

        /// <summary>"Gap Sprout" — A row or column holding it is harvested even with one empty plot.</summary>
        Gap,

        /// <summary>"Wildroot Sprout" — Counts as one extra tile of its group.</summary>
        Wildroot,

        /// <summary>"Dye Sprout" — Paints the tiles touching it its own color after scoring.</summary>
        Dye,

        /// <summary>"Fertile Sprout" — When its tile is harvested, its plot turns golden for the round.</summary>
        Fertile,

        /// <summary>"Evergreen Sprout" — Its tile survives harvests: it stays planted and still scores the line points.</summary>
        Evergreen,

        /// <summary>"Pruner Sprout" — After scoring, uproots the touching tiles of another color: +8 pts each.</summary>
        Pruner,

        /// <summary>"Glow Sprout" — The empty plots touching it turn golden for the round.</summary>
        Glow,

        /// <summary>"Bridge Sprout" — Joins its group across one empty plot to a tile of the same color.</summary>
        Bridge,

        /// <summary>"Seedpod Sprout" — When its tile is harvested, plants 2 free tiles of its color nearby.</summary>
        Seedpod,

        /// <summary>"Locksmith Sprout" — Each time it scores, frees one locked, frozen or poisoned plot anywhere.</summary>
        Locksmith,

        /// <summary>"Mimic Sprout" — replays the upgrade of every tile of its group touching it (orthogonally), as if it carried those upgrades itself. Explicit id 100: 76 belonged to the retired Sap Drain, which old saves still carry (converted on restore).</summary>
        Mimic = 100,

        /// <summary>"Dominant Sprout" — when placed, the WHOLE seed recolors, for good, to the most common color on the garden (its own color on a tie).</summary>
        Dominant = 101,

        /// <summary>"Heritage Sprout" — when placed, the WHOLE seed recolors, for good, to the most common color in the Seed Bag (its own color on a tie).</summary>
        Heritage = 102,

        /// <summary>"Scythe Sprout" — after the count, harvests the whole row of its tile even if the row is not full.</summary>
        Scythe = 103,

        /// <summary>"Tally Sprout" — scores +5 points per line harvested this round (forced breaks included).</summary>
        Tally = 104,

        /// <summary>"Sunbeam Sprout" — each time it scores, thaws every frozen plot.</summary>
        Sunbeam = 105,

        /// <summary>"Antidote Sprout" — each time it scores, cures every blighted plot and the blighted fertilizer.</summary>
        Antidote = 106,

        /// <summary>"Lull Sprout" — when placed, enemies skip their next Shuffle trick.</summary>
        Lull = 107,

        /// <summary>"Dazzle Sprout" — when placed, enemies lose their shield and their color/shape hatred for the round.</summary>
        Dazzle = 108,

        /// <summary>"Wilt Sprout" — on a turn where it scores, enemies cannot heal or grow.</summary>
        Wilt = 109,

        /// <summary>"Cleanse Sprout" — at every Shuffle, once the enemies have acted, it frees one afflicted plot.</summary>
        Cleanse = 110,

        /// <summary>"Clone Sprout" — when its seed is planted, a small chance to add a copy of that seed (without Clone) to the Seed Bag.</summary>
        Clone = 111
    }

    /// <summary>
    /// A one-time scoring enchantment tagged onto a single cell of a specific
    /// <see cref="PieceToken"/> — the upgrade marks a piece in the deck rather
    /// than a fixed grid cell. <see cref="LocalCellIndex"/> indexes into the
    /// piece's base (Deg0) shape's cell list — since <see
    /// cref="PieceShapeCatalog.GetRotated"/> maps that list 1:1 across every
    /// rotation, the same index still identifies the correct cell once the
    /// piece is placed at whatever rotation it was actually dealt.
    /// </summary>
    public readonly struct PieceTrait
    {
        public readonly PieceTraitKind Kind;
        public readonly int LocalCellIndex;

        /// <summary>Only meaningful for <see cref="PieceTraitKind.Tinted"/>; null for the others.</summary>
        public readonly PieceColor? TintedColor;

        public PieceTrait(PieceTraitKind kind, int localCellIndex, PieceColor? tintedColor = null)
        {
            Kind = kind;
            LocalCellIndex = localCellIndex;
            TintedColor = tintedColor;
        }

        /// <summary>The color a freshly tagged trait of <paramref name="kind"/> is pinned to: Tinted keeps its token's own, Companion looks for the next color of the run after it (so a Companion Sprout always wants a different color); null for every other kind.</summary>
        public static PieceColor? TargetColorFor(PieceTraitKind kind, PieceColor tokenColor)
        {
            if (kind == PieceTraitKind.Tinted)
            {
                return tokenColor;
            }
            if (kind != PieceTraitKind.Companion)
            {
                return null;
            }
            var colors = PieceColorUtility.BaseColors;
            int own = -1;
            for (int i = 0; i < colors.Count; i++)
            {
                if (colors[i] == tokenColor)
                {
                    own = i;
                }
            }
            return colors[(own + 1) % colors.Count];
        }

        /// <summary>
        /// Upgrades a Joker seed cannot carry: the ones that need a real color (Tinted) or that would turn the seed into a plain colored one for
        /// good (Chameleon, Dominant, Heritage). A Joker takes every other upgrade like any seed.
        /// </summary>
        public static bool IsIncompatibleWithJoker(PieceTraitKind kind)
        {
            return kind == PieceTraitKind.Tinted || kind == PieceTraitKind.Chameleon || kind == PieceTraitKind.Dominant || kind == PieceTraitKind.Heritage;
        }
    }
}
