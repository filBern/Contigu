# Progression : déblocages et difficulté

Statut : **implémenté dans le Core et branché dans la présentation** (à vérifier dans Unity). Décisions de l'auteur (2026-11-09) : objets à débloquer par conditions comme Balatro, Stars supprimées, challenges en parallèle avec des conditions différentes, niveaux de difficulté, annonce des nouveautés en fin de run, anciennes sauvegardes oubliées (reset forcé), ennemis par challenge traités ensuite (voir DECISIONS, 2026-11-10).

## Principes

- **Départ restreint, pas de hasard, pas de monnaie.** Un objet se débloque en jouant d'une certaine façon. Les Stars n'existent plus.
- **Noyau de départ (25 Sprouts et 30 fertilizers)** disponible dès la première partie ; le reste est caché de la boutique, des packs, de la Forge et du Compost jusqu'au déblocage.
- **Une condition par objet verrouillé**, affichée comme texte (`UnlockCondition.Text`). Les conditions reposent sur ce que le Core sait mesurer (`UnlockMetric`) et suivent le thème de l'objet.
- **Au plus 8 déblocages par run** (`UnlockTracker.MaxUnlocksPerRun`) : si une run en remplit davantage (typiquement une victoire), le reste attend dans une file et arrive au début des runs suivants. Une seule grosse run ne débloque donc jamais la moitié du jeu.
- **Les seuils sont calibrés par simulation** (`RunSim --calibrate`, puis `--locked` pour mesurer le rythme) : avec le planner glouton, ~8 déblocages après la 1re run, ~40 après 5, ~80 après 10, ~120 après 25 et ~150 après 50. Un joueur humain, moins efficace, ira plus lentement. Les valeurs sont à régler avec de vrais joueurs.
- **Challenges** : Spring est toujours ouvert ; les cinq autres ont chacun leur condition (en parallèle). Gagner un challenge au niveau N ouvre le niveau N+1 (jusqu'à 5).
- **Niveaux de difficulté** (cumulatifs) : 1 Normal ; 2 ennemis +15 % de PV ; 3 un Shuffle de moins ; 4 ennemis +15 % de PV de plus et une graine de moins par manche ; 5 fertilizers +1 Pollen.
- **Sauvegarde** : `MetaStats.UnlockData` (ids débloqués, file d'attente et compteurs « au total »), `MetaStats.BestLevelWon` par challenge. `MetaStats.SaveVersion` = 2 : une sauvegarde d'une autre version est ignorée (tout recommence).
- **Outils** : `cd Tools/Headless/RunSim && dotnet run -c Release -- --locked --runs 300 --policy greedy` (rythme de déblocage d'un nouveau joueur), `--coverage` (tout est disponible, on mesure ce qui se débloque), `--calibrate --calibration-out fichier.csv` (valeurs atteintes par run pour chaque condition), `UNLOCK_VERBOSE=1` et `UNLOCK_TRACE=1` (détail).

## Challenges

| Challenge | Condition |
|---|---|
| Winter | Win a Spring run |
| Summer | Reach round 6 |
| Autumn | Hold 4 fertilizers at once |
| Orchard | Form a group of 15 tiles |
| Greenhouse | Harvest 3 lines with a single seed |

## Sprouts : 25 au départ, 65 à débloquer

Au départ : Golden Cells, Tinted Cells, Pip, Lucky, Catalyst, Chameleon, Spark, Border, Heart, Cuddly, Dawn, Elder, Breeze, Crossfire, Even, Honeycomb, Pouch, Hunter, Venom, Locksmith, Bastion, Harmony, Colony, Companion, Detonator.

| Sprout | Rareté | Effet | Condition de déblocage |
|---|---|---|---|
| Multiplier Zone | Rare | Doubles the whole placement's score, harvest included. | Score 10000 points with a single seed |
| Blast Sprout | Rare | Itself and its 4 neighbors each score +18 pts, like golden plots, if they join t | Harvest 3 lines with a single seed |
| Multiplier Beacon | Rare | Every sprout in its row and column (its own seed included) doubles the score of  | Have 12 different upgrades on the garden at once |
| Mirror Sprout | Uncommon | Copies its group bonus onto one random other sprout in the group. | Score 5 upgraded tiles in the same group |
| Seeder | Rare | Stays golden for the rest of the round instead of scoring once. | Score 70 golden plots in one run |
| Twin Sprout | Rare | Copies its group bonus onto EVERY other sprout in the group. | Score 6 upgraded tiles in the same group |
| Void Sprout | Rare | Uproots one random filled sprout elsewhere on the grid (+10 for the sprout broke | Have 25 tiles on the garden at once |
| Kamikaze Sprout | Rare | Destroys itself and its 8 surrounding sprouts, and shatters the ice around it (+ | Have 26 tiles on the garden at once |
| Permanent Bonus Square | Rare | Marks one random empty board square to always double score through it, for the r | Reach round 7 |
| Corner Sprout | Rare | +30 pts when planted on a corner. | Fill all 4 corners of the garden at once |
| Hermit Sprout | Uncommon | +12 pts when no other tile touches it. | Plant 25 seeds that touch nothing in one run |
| Dusk Sprout | Uncommon | +20 pts if planted as the last seed of your hand. | Harvest a line with the last seed of a hand (25 times in one run) |
| Miser Sprout | Common | +1 pt per Pollen held, with no limit. | Hold 300 Pollen at once |
| Stormy Sprout | Uncommon | +12 pts when the weather isn't Clear. | Clear a round on a day with weather (9 times in one run) |
| Sunseeker Sprout | Common | +10 pts on Sunshine and Golden Hour days (and their indoor variants). | Clear a round on a Sunshine day (2 times in one run) |
| Harvester Sprout | Uncommon | +10 pts per line harvested by its seed. | Harvest 18 lines in one round |
| Prism Sprout | Rare | +20 pts when it touches 3 or more different colors. | Plant a seed touching 3 other colors |
| Gardener Sprout | Uncommon | +3 pts per fertilizer held. | Hold 3 fertilizers at once |
| Cluster Sprout | Uncommon | +6 pts per other upgraded tile in its group. | Score 5 upgraded tiles in the same group |
| Rainbow Row Sprout | Uncommon | +15 pts when its row holds every color of the run. | Have all 4 colors on the garden at once |
| Executioner Sprout | Rare | +30 pts when the front enemy is down to a quarter of its HP or less. | Defeat 500 enemies in total |
| Spillover Sprout | Uncommon | +25 pts when its tile is harvested by a line. | Harvest 100 lines in one run |
| Dewdrop Sprout | Uncommon | +2 Pollen per line harvested by its seed. | Harvest 18 lines in one round |
| Keystone Sprout | Rare | +5 Mult when it sits on a corner or in the middle. | Plant 80 tiles on corners in one run |
| Growth Sprout | Rare | +1 pt, then +1 more every time it scores this round. | Clear 10 rounds without a Shuffle in one run |
| Gambler Sprout | Uncommon | 50%: +40 pts. 50%: -10 pts. | Reach round 3 |
| Warden Sprout | Uncommon | Frees the 4 plots around it from enemy effects and makes them immune for the rou | Suffer 1500 locked, frozen or blighted plots in total |
| Aegis Sprout | Uncommon | Frees its own plot from enemy effects and makes it immune for the round. | Have 18 plots locked, frozen or blighted at once |
| Sower Sprout | Rare | Plants a free tile of its color on a neighboring empty plot. | Plant 35 seeds in one round |
| Kin Sprout | Uncommon | +4 pts per other upgraded tile touching it. | Have 15 upgraded tiles on the garden at once |
| Amplifier Sprout | Rare | Doubles the points of the upgraded tiles touching it. | Score 500 points from upgrades with a single seed |
| Echo Sprout | Uncommon | Copies the best points bonus among the upgraded tiles touching it. | Have 15 upgraded tiles on the garden at once |
| Collector Sprout | Uncommon | +3 pts per different upgrade in the garden. | Have 25 different upgrades in your bag |
| Resonance Sprout | Rare | +2 pts per upgraded tile in the garden. | Have 15 upgraded tiles on the garden at once |
| Perennial Sprout | Rare | +1 pt, then +1 more every time any Perennial scores, all run long. | Reach round 10 |
| Crescendo Sprout | Uncommon | +2 pts per seed already planted this round. | Plant 35 seeds in one round |
| Scholar Sprout | Uncommon | +1 pt per mastery level earned this run. | Earn 10 mastery levels |
| Full House Sprout | Uncommon | +12 pts for its row and +12 for its column when 3/4 or more of it is full. | Have 24 tiles on the garden at once |
| Compound Sprout | Uncommon | Earns 10% of the Pollen you hold (rounded down) every time it scores. | Hold 400 Pollen at once |
| Gap Sprout | Rare | A row or column holding it is harvested even with one empty plot. | Clear a round with 30 seeds left |
| Wildroot Sprout | Uncommon | Counts as one extra tile of its group. | Own a Joker seed (2) |
| Dye Sprout | Uncommon | Paints the tiles touching it its own color after scoring. | Plant a seed touching 3 other colors |
| Fertile Sprout | Uncommon | When its tile is harvested, its plot turns golden for the round. | Score 15 golden plots in one run |
| Evergreen Sprout | Rare | Its tile survives harvests: it stays planted and still scores the line points. | Harvest 2500 lines in total |
| Pruner Sprout | Uncommon | After scoring, uproots the touching tiles of another color: +8 pts each. | Plant a seed touching 6 tiles |
| Glow Sprout | Uncommon | The empty plots touching it turn golden for the round. | Score 30 golden plots in one run |
| Bridge Sprout | Rare | Joins its group across one empty plot to a tile of the same color. | Join 3 groups together with one seed |
| Seedpod Sprout | Uncommon | When its tile is harvested, plants 2 free tiles of its color nearby. | Form a group of 15 tiles |
| Mimic Sprout | Rare | Copies the upgrade of every tile of its group touching it, as if it carried them | Have 12 different upgrades on the garden at once |
| Dominant Sprout | Uncommon | Recolors the WHOLE seed to the most common color on the garden when placed, for  | Plant 5 seeds of the same color in a row |
| Heritage Sprout | Uncommon | Recolors the WHOLE seed to the most common color in your bag when placed, for go | Own 11 seeds of one color in your bag |
| Scythe Sprout | Rare | When its seed is planted, harvests the row AND the column of its tile after the  | Harvest 3 lines with a single seed |
| Tally Sprout | Uncommon | +5 pts per line harvested this round, forced harvests included. | Harvest 18 lines in one round |
| Sunbeam Sprout | Uncommon | Each time it scores, thaws every frozen plot. | Clear a round in Winter |
| Antidote Sprout | Uncommon | Each time it scores, cures every blighted plot and the blighted fertilizer. | Defeat Blightspore (3 times in total) |
| Lull Sprout | Uncommon | When placed, enemies skip their next Shuffle trick. | Defeat Bramble (3 times in total) |
| Dazzle Sprout | Rare | When placed, enemies lose their shield and their color or shape hatred for the r | Defeat a boss that hates a color or a shape (2 times in total) |
| Wilt Sprout | Uncommon | On a turn where it scores, enemies cannot heal or grow. | Defeat Root Leech or Creeping Vine (3 times in total) |
| Cleanse Sprout | Uncommon | At every Shuffle, once the enemies have acted, frees one locked, frozen or bligh | Clear a round from round 3 on with no locked, frozen or blighted plot left (9 times in one run) |
| Clone Sprout | Rare | When planted, 25% chance to add a copy of this seed to your Seed Bag (the copy h | Own 40 seeds in your bag |
| Mirror Ward Sprout | Rare | Blighted plots next to it score normally: their points are not negated. | Score through a blighted plot 800 times in total |
| Seedsack Sprout | Uncommon | If it is the last seed of your hand, the refill does not trigger the enemies. | Empty your hand 80 times in one run |
| Heirloom Sprout | Rare | If it is still on the garden when the round is cleared, a plain copy of its seed | Win a run |
| Anchor Sprout | Uncommon | Enemies and weather cannot recolor, swap, move or burn this tile. | Defeat a challenge's resident enemy |
| Late Bloom Sprout | Rare | From its 3rd trigger on, +1 Mult each time. Resets when a line is harvested or t | Clear a round without harvesting a line |

## Fertilizers : 30 au départ, 103 à débloquer

Au départ : Mult +1, Mult +2, Chain, Prisoner, Crown, Mason, Nest, Diagonal, les 4 Devotion, Seedling, Overgrowth, Waxing, Waning, Reaper, Bounty, Row Crop, Column Crop, Hedgerow, Patchwork, Early Bird, Underbrush, Rain Dancer, Sun Dancer, Pest Control, Grafter, Thorns, Whirlwind. Les quatre « Glow » de couleur sont retirés depuis longtemps.

| Fertilizer | Catégorie | Effet | Condition de déblocage |
|---|---|---|---|
| Prism | Couleurs | x3 Mult if the seed you plant touches all 3 other colors (side by side with any  | Plant a seed touching 3 other colors |
| Mega Chain | Connexions | +4 Mult if the group has 10+ cells, +1 Mult per cell past that. | Form a group of 18 tiles |
| Fortress | Voisinage | +1 Mult per fully surrounded group cell. | Plant a seed touching 7 tiles |
| Architect | Roguelike | x2 Mult when a 2x2 block is placed. | Plant 25 square seeds in one run |
| Hoarder | Roguelike | +1 Mult per distinct color in harvested cells. | Have 4 colors on the garden at once |
| Triad Bloom | Couleurs | x2 Mult if 2 other colors are touched. | Plant a seed touching 3 other colors |
| Companion Bloom | Couleurs | x2 Mult if a complementary pair is touched (Poppy/Sunflower or Iris/Fern). | Have all 4 colors on the garden at once |
| Islet | Voisinage | x2 Mult if the seed lands as a lone single cell. | Plant 5 seeds that touch nothing in one round |
| Crossroads | Voisinage | +2 Mult per group cell surrounded by 2+ colors. | Plant a seed touching 7 tiles |
| Demolisher | Destruction | x2 Mult per row/column harvested at once, stacking (2+ only). | Harvest 20 lines in one round |
| Alchemist's Wheel | Voisinage | +4 Mult per group cell surrounded by all 4 colors. | Plant a seed touching 3 other colors |
| Ashbound | Roguelike | +0.5 Mult per group cell if the group has no jokers. | Plant 4 seeds of the same color in a row |
| Dissonant Bloom | Couleurs | +1 Mult per placed cell next to a different color. | Plant a seed touching 7 tiles |
| Momentum | Roguelike | x2 Mult when this group is bigger than the last one scored. | Score a bigger group 5 placements in a row |
| Cocooned | Voisinage | +1 Mult per group cell with all 4 diagonals filled. | Form a group of 15 tiles |
| Gardener | Roguelike | +1 Mult per group cell next to an upgraded sprout. | Have 10 upgraded tiles on the garden at once |
| Rainbow | Couleurs | x2 Mult per harvested line with every color of the run, stacking. | Have all 4 colors on the garden at once |
| Alternation | Couleurs | x2 Mult per harvested line alternating between 2 colors, stacking (a joker break | Plant 18 seeds in a row, each of a different color than the last |
| Palindrome | Connexions | x2 Mult per harvested line whose colors read the same both ways, stacking. | Harvest 100 lines in one run |
| Gradient | Connexions | PERMANENT: +1 Mult (never resets) per harvested line with no two adjacent same-c | Harvest 120 lines in one run |
| Block | Connexions | x2 Mult per harvested line with no isolated single-color cell, stacking. | Harvest 18 lines in one round |
| Ashbound Line | Couleurs | x2 Mult per harvested line that's a single color, stacking. | Plant 5 seeds of the same color in a row |
| Slot 1 Loyalty | Roguelike | +3 Mult when played from hand slot 1. | Play 12 seeds from hand slot 1 in one round |
| Slot 2 Loyalty | Roguelike | +3 Mult when played from hand slot 2. | Play 12 seeds from hand slot 2 in one round |
| Slot 3 Loyalty | Roguelike | +3 Mult when played from hand slot 3. | Play 12 seeds from hand slot 3 in one round |
| Mutant Growth | Roguelike | +2 Mult if the seed isn't exactly 3 cells. | Play 10 seeds of 1 or 2 cells in one round |
| Solitaire | Connexions | x2 Mult when the group is only this seed (2+ cells) - nothing merged in. | Plant 20 seeds that touch nothing in one run |
| Fallow Ground | Roguelike | x2 Mult when the board ends up with 16 plots filled or fewer. | Clear a round with the garden a third full or less |
| Burst | Destruction | x3 Mult when this AND the last placement both harvested a line. | Harvest a line on 5 placements in a row |
| New Growth | Couleurs | x2 Mult when this color is new to the board. | Plant 15 seeds in a row, each of a different color than the last |
| Bridge | Connexions | x2 Mult per extra pre-existing group bridged together, stacking. | Join 3 groups together with one seed |
| Encirclement | Voisinage | +1 Mult per group cell boxed in on all 8 sides (filled or edge). | Plant a seed touching 7 tiles |
| Sealer | Voisinage | +1 Mult per pre-existing sprout this placement newly encircles. | Plant a seed touching 7 tiles |
| Thriving Colony | Couleurs | x2 Mult when this color forms only one connected group on the board. | Form a group of 15 tiles |
| Repetition | Roguelike | xn Mult for n consecutive same-shape placements (x2 on the 2nd, x3 on the 3rd... | Plant the same shape 4 times in a row |
| Changeling Bloom | Couleurs | +2 Mult when this seed's color differs from the last one played. | Plant 15 seeds in a row, each of a different color than the last |
| Combo | Destruction | x2 the whole score if the last placement harvested a line. | Harvest a line on 5 placements in a row |
| Precision | Voisinage | +1 Mult per cell if every cell of the seed touches a filled sprout. | Plant 35 seeds in one round |
| Thicket | Voisinage | +1 Mult per cell if every cell of the seed touches 2+ filled sprouts. | Plant a seed touching 7 tiles |
| Lone Bloom | Voisinage | x2 Mult when the seed touches exactly 1 filled sprout, total. | Plant 25 seeds that touch nothing in one run |
| Wildcard | Roguelike | Joker sprouts count as the color of one of your Devotion fertilizers. | Own 3 Joker seeds at once |
| Root Density | Roguelike | +n Mult, n = filled cells on the board ÷ 10 - scales with how full the board is. | Have 25 tiles on the garden at once |
| Mult +4 | Roguelike | +4 Mult. No condition. | Reach round 8 |
| Solidarity | Roguelike | +1 Mult per 2 fertilizers held, this one included. | Hold 4 fertilizers at once |
| Mimic | Roguelike | Instantly copies whatever fertilizer you bought right before it. Does nothing on | Hold fertilizers of 3 different categories |
| Volatile Brew | Roguelike | +5 Mult, but 1-in-5 chance to lose it at the end of every round. | Reach round 5 |
| Enchanted Seeds | Roguelike | +0.1 Mult per upgraded seed in your seed bag, starting from a baseline of 1. | Have 40 upgraded seeds in your bag |
| Dwindling | Roguelike | +13 Mult, shrinking a little with every placement for the rest of the run - dest | Reach round 4 |
| Multitude | Roguelike | +1 Mult per 10 seeds currently in your seed bag. | Own 40 seeds in your bag |
| Seasoned Growth | Roguelike | +0.1 Mult per upgraded seed you've PLAYED this run, starting from a baseline of  | Play 200 upgraded seeds in one run |
| Seedling Specialist | Formes | x2 Mult on seeds with 2 or fewer cells (Single, Domino). | Play 9 seeds of 1 or 2 cells in one round |
| Sapling Specialist | Formes | x2 Mult on 3-cell seeds (any Tromino). | Play 12 three-cell seeds in one round |
| Overgrowth Specialist | Formes | x2 Mult on 4-cell seeds (Square or any Tetromino). | Play 20 four-cell seeds in one round |
| Seedling Glow | Formes | +1 Mult per group cell on seeds with 2 or fewer cells (Single, Domino). | Play 50 seeds of 1 or 2 cells in one run |
| Sapling Glow | Formes | +1 Mult per group cell on 3-cell seeds (any Tromino). | Play 50 three-cell seeds in one run |
| Overgrowth Glow | Formes | +1 Mult per group cell on 4-cell seeds (Square or any Tetromino). | Play 120 four-cell seeds in one run |
| Hardy Growth | Roguelike | +1 Mult per distinct fertilizer category you hold at least one of. | Hold fertilizers of 4 different categories |
| Chromatic Hoard | Couleurs | +2 Mult per color for which you hold its Devotion fertilizer. | Hold 2 Devotion fertilizers at once |
| Cadence | Roguelike | x2 Mult when this placement matches BOTH a held Pair/Impair condition AND a held | Hold 3 fertilizers at once |
| Echo | Roguelike | Replays the fertilizer right before it in your order, as if you held a second co | Have 30 different upgrades in your bag |
| Fourth Slot | Roguelike | Adds a 4th hand slot. | Win a run |
| Barometer | Roguelike | +3 Mult whenever the round's weather isn't Clear skies. | Clear 7 rounds with weather in one run |
| Storm Chaser | Roguelike | x2 Mult while the weather is Frost, Heatwave or Hurricane. | Clear a round in Frost, Heatwave or Hurricane (12 times in total) |
| Weatherproof | Roguelike | Frost, Heatwave and Hurricane have no effect on your garden. | Clear 12 rounds with weather in total |
| Haste | Roguelike | +0.1 Mult per seed still left to play this round. | Clear a round with 30 seeds left |
| Cornerstone | Voisinage | x2 Mult when the seed puts a tile on one of the 4 corners. | Plant 70 tiles on corners in one run |
| Heartwood | Voisinage | x2 Mult when the seed puts a tile in the middle of the garden. | Plant on the middle of the garden 20 times in one run |
| Trellis | Formes | +3 Mult when the seed is a straight vertical piece (1 wide, 2 or more tall). | Play 6 vertical seeds in one round |
| Furrow | Formes | +3 Mult when the seed is a straight horizontal piece (1 tall, 2 or more wide). | Play 6 horizontal seeds in one round |
| Beanstalk | Formes | +1 Mult when the seed is a straight piece of 3 or more tiles. | Play 6 straight seeds of 3 or more tiles in one round |
| Taproot | Connexions | +1 Mult per 3 cells in the scored group. | Form a group of 15 tiles |
| Snug Fit | Voisinage | +5 Mult when every plot touching the seed is already planted (or the edge). | Plant a seed touching 7 tiles |
| Mosaic | Couleurs | +3 Mult when the seed touches 2 or more tiles and none of them shares its color. | Plant a seed touching 3 other colors |
| Crowd | Voisinage | x2 Mult while two thirds of the garden or more is planted. | Have two thirds of the garden planted at once |
| Wildflower Meadow | Couleurs | +4 Mult when every color of the run is growing in the garden. | Have 22 tiles on the garden at once |
| Wreath | Voisinage | +1 Mult per tile of the seed touching 3 or more planted plots (the seed's own ot | Plant a seed touching 7 tiles |
| Cross Harvest | Destruction | +6 Mult when one seed harvests a row and a column at once. | Harvest 3 lines with a single seed |
| Clean Slate | Destruction | x3 Mult when the seed harvests the last planted tile in the garden. | Harvest every tile of the garden at once |
| Frostbreaker | Destruction | +6 Mult when the seed breaks a frozen plot free. | Break a frozen plot with a line (9 times in total) |
| Seasoned | Roguelike | +0.5 Mult per round already cleared this run. | Win a Spring run |
| Finishing Blow | Roguelike | +6 Mult while the enemies are down to a quarter of their total HP or less. | Defeat an enemy with 35 seeds left |
| Last Seed | Roguelike | x3 Mult on the last seed of the round. | Clear a round on its last seed |
| First Seed | Roguelike | x2 Mult on the first seed of the round. | Harvest a line with the first seed of a round |
| Piggy Bank | Roguelike | +0.1 Mult per Pollen you hold. | Hold 1000 Pollen at once |
| Seed Bank | Roguelike | +0.05 Mult per seed in your bag. | Own 40 seeds in your bag |
| Frost Hardy | Roguelike | +4 Mult while the weather is Frost (or the Freezer Door Ajar). | Clear a round in Frost |
| Windrider | Roguelike | +4 Mult in Wind and Hurricane weather (and their indoor variants). | Clear a round in Wind |
| Marathoner | Roguelike | +4 Mult from round 9 onwards. | Reach round 9 |
| Bossbane | Roguelike | x2 Mult during boss rounds. | Defeat a boss (2 times in total) |
| Charmed Patch | Roguelike | +1 Mult per upgraded tile in the scored group. | Score 5 upgraded tiles in the same group |
| Twin Graft | Roguelike | +5 Mult when the seed you plant carries 2 or more upgrades. | Play a seed with 2 upgrades (70 times in one run) |
| Botanist | Roguelike | +1 Mult per different kind of upgrade in the garden. | Have 10 different upgrades on the garden at once |
| Hothouse | Roguelike | +1 Mult per 2 upgraded tiles in the garden. | Have 15 upgraded tiles on the garden at once |
| Good Neighbors | Voisinage | +2 Mult per upgraded tile touching the seed you plant. | Have 15 upgraded tiles on the garden at once |
| Gleaner | Destruction | +2 Mult per upgraded tile harvested by this placement. | Harvest 100 lines in one run |
| Wild Cluster | Roguelike | +0.5 Mult per Joker tile in the scored group. | Own 2 Joker seeds at once |
| Wild Reserve | Roguelike | +0.1 Mult per Joker seed in your bag. | Own 2 Joker seeds at once |
| Wild Surge | Roguelike | x2 Mult when the scored group contains a Joker tile. | Own 4 Joker seeds at once |
| Crop Rotation | Formes | +0.1 Mult each time you plant a different shape than the last seed this round, s | Change the shape you plant 30 times in one round |
| Greenhouse Lamp | Roguelike | +0.5 Mult per Shuffle you have left. | Clear a round in Greenhouse |
| Seed Library | Formes | +0.5 Mult per different shape in your Seed Bag. | Own all 8 shapes in your bag at once |
| Twin Bloom | Couleurs | Plant the same color as the last seed: +1 Mult, then +2, +3... for each seed in  | Plant 5 seeds of the same color in a row |
| Underdog | Roguelike | +3 Mult while the enemies still have more than half of their total HP. | Defeat 100 enemies in total |

## À régler plus tard

- Les valeurs de conditions et de niveaux avec de vrais joueurs (la calibration vient d'un bot).
- La Collection devrait afficher la condition des objets verrouillés (pas encore fait).
