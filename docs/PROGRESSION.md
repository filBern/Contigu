# Progression : déblocages et difficulté

Statut : **proposition à valider** (rien n'est codé). Écrit le 2026-11-09 à partir des réponses de l'auteur : objets à débloquer par conditions (comme Balatro), challenges en parallèle avec des conditions différentes, niveaux de difficulté, écran de fin qui annonce les nouveautés, ennemis plus différents d'un challenge à l'autre.

## 1. Principes

- **Départ restreint, pas de hasard, pas de monnaie.** On débloque un objet en jouant d'une certaine façon, jamais en dépensant ni en attendant. Les Stars ne servent plus à rien pour les objets et les challenges (elles peuvent disparaître ou devenir cosmétiques).
- **Une condition visible par objet.** Dans la Collection, un objet verrouillé est grisé et affiche sa condition. Le joueur a donc des objectifs de run.
- **Le noyau de départ enseigne les bases :** 25 Sprouts et 30 fertilizers disponibles dès la première partie (les lignes « Départ » ci-dessous). Le reste est caché du pool (boutique, packs, Forge) tant qu'il n'est pas débloqué.
- **Les conditions suivent le thème de l'objet** (un objet de lignes se débloque en récoltant des lignes, un objet météo par temps variable, etc.).
- **Déblocage immédiat, annonce en fin de run.** Dès qu'une condition est remplie, l'objet est marqué débloqué dans la sauvegarde. Il apparaîtra dans les runs suivants. L'écran de fin liste les nouveautés (« Nouveau : Scythe Sprout »).
- **Aucune condition ne guide le joueur en partie** (règle du projet) : les conditions sont affichées dans la Collection, jamais rappelées pendant la run.

## 2. Challenges en parallèle

Spring reste toujours ouvert. Les cinq autres s'ouvrent **chacun avec sa propre condition**, sans ordre imposé.

| Challenge | Identité | Condition proposée |
|---|---|---|
| Spring | Le départ | Toujours ouvert |
| Winter | Petit sac, peu de graines, Hoarfrost | Gagner Spring une fois |
| Summer | Météo extrême, Locust Swarm | Atteindre la manche 8 en Spring |
| Autumn | Fertilizers qui fanent, Rotgrub | Posséder 5 fertilizers en même temps dans une run |
| Orchard | 2 couleurs, Fruitworm | Récolter une ligne d'une seule couleur |
| Greenhouse | Grille 5x5, Whitefly Cloud | Récolter 2 lignes avec une seule graine |

### Ennemis plus différents d'un challenge à l'autre

Aujourd'hui chaque challenge n'ajoute qu'**un** ennemi résident (Hoarfrost, Locust, Rotgrub, Fruitworm, Whitefly). Pour que chacun se sente différent :
- **Un pool d'ennemis par challenge.** Les manches ordinaires piochent dans un pool propre au challenge (par exemple Winter : gel, voleurs, Hoarfrost ; Orchard : Fruitworm, haters de couleur ; Greenhouse : Whitefly, verrous). Spring garde un pool équilibré et simple.
- **Un boss propre à chaque challenge** (une mécanique unique) à la manche 12, au lieu de la réserve commune.
- Les ennemis ne sont **pas verrouillés derrière la progression** (décision de l'auteur) : seulement répartis différemment.
- Ce point demande une passe de design séparée (quels ennemis dans quel pool) et une étude de balance avec le simulateur.

## 3. Niveaux de difficulté (équivalent des stakes)

Pour chaque challenge : **gagner au niveau N ouvre le niveau N+1**. Proposition de 5 niveaux, cumulatifs :
1. Normal.
2. Ennemis +15 % de PV.
3. 1 Shuffle de moins par manche.
4. Un ennemi de plus dans les manches 6 à 12 (ou un trick de plus pour les ennemis).
5. Les fertilizers de la boutique coûtent +1 Pollen.

Les valeurs sont à régler avec le simulateur. Le niveau gagné s'affiche comme un autocollant sur le challenge.

## 4. Sprouts : 25 au départ, 65 à débloquer

| Sprout | Rareté | Effet | Condition de déblocage |
|---|---|---|---|
| Golden Cells | Common | Scores +18 pts when placed. | **Départ** |
| Tinted Cells | Uncommon | Doubles group and golden score (not harvest), tinted to the seed's color. | **Départ** |
| Catalyst Sprout | Common | +3 pts for each tile already planted that joins the group, counted again every time a new  | **Départ** |
| Detonator Sprout | Uncommon | Doubles the harvest bonus if it harvests a row or column. | **Départ** |
| Chameleon Sprout | Common | Recolors the WHOLE seed to match a filled neighbor when placed, for good. | **Départ** |
| Spark Sprout | Common | Scores more the longer since the last harvest this round. | **Départ** |
| Bastion Sprout | Uncommon | Won't be removed when a line is harvested. | **Départ** |
| Venom Sprout | Uncommon | Poisons every enemy for 1% of its max HP when placed, no scoring required. | **Départ** |
| Pip Sprout | Common | +1 pt when planted. | **Départ** |
| Lucky Sprout | Common | +0 to +20 pts at random when planted. | **Départ** |
| Companion Sprout | Uncommon | +8 pts per touching tile of another color, +25 for one of its chosen color. | **Départ** |
| Colony Sprout | Uncommon | +20 pts when its group has more than 10 cells. | **Départ** |
| Border Sprout | Common | +8 pts when planted on the garden's edge. | **Départ** |
| Heart Sprout | Uncommon | +12 pts when planted in the middle of the garden. | **Départ** |
| Cuddly Sprout | Common | +3 pts per planted tile around it (diagonals and its own seed included). | **Départ** |
| Dawn Sprout | Common | +12 pts if planted among the first 3 seeds of the round. | **Départ** |
| Elder Sprout | Common | +2 pts per round already cleared this run. | **Départ** |
| Breeze Sprout | Common | +3 pts per Shuffle you still have. | **Départ** |
| Harmony Sprout | Uncommon | +10 pts when every touching tile shares its color. | **Départ** |
| Crossfire Sprout | Common | +1 pt per planted tile in its row and column. | **Départ** |
| Even Sprout | Common | +12 pts when its group has an even number of cells. | **Départ** |
| Hunter Sprout | Common | +3 pts per enemy still alive. | **Départ** |
| Honeycomb Sprout | Common | +1 Pollen every time it scores. | **Départ** |
| Pouch Sprout | Common | +1 pt per upgraded seed in your bag. | **Départ** |
| Locksmith Sprout | Uncommon | Each time it scores, frees one locked, frozen or poisoned plot anywhere. | **Départ** |
| Multiplier Zone | Rare | Doubles the whole placement's score, harvest included. | Score 400 points with a single seed. |
| Blast Sprout | Rare | Itself and its 4 neighbors each score +18 pts, like golden plots, if they join the scored  | Harvest 2 lines with a single seed. |
| Multiplier Beacon | Rare | Every sprout in its row and column (its own seed included) doubles the score of the group  | Have 4 different upgrades on the garden at once. |
| Mirror Sprout | Uncommon | Copies its group bonus onto one random other sprout in the group. | Score 3 upgraded tiles in the same group. |
| Seeder | Rare | Stays golden for the rest of the round instead of scoring once. | Plant 10 Golden Cells tiles in one run. |
| Twin Sprout | Rare | Copies its group bonus onto EVERY other sprout in the group. | Unlock Mirror Sprout, then win a round with it on the garden. |
| Void Sprout | Rare | Uproots one random filled sprout elsewhere on the grid (+10 for the sprout broken). | Fill the garden to 20 or more tiles at once. |
| Kamikaze Sprout | Rare | Destroys itself and its 8 surrounding sprouts, and shatters the ice around it (+6 per spro | Destroy 5 tiles with one seed (Detonator, Void, Scythe...). |
| Permanent Bonus Square | Rare | Marks one random empty board square to always double score through it, for the rest of the | Reach round 6 in any challenge. |
| Corner Sprout | Rare | +30 pts when planted on a corner. | Plant on all 4 corners in one round. |
| Hermit Sprout | Uncommon | +12 pts when no other tile touches it. | Plant 5 seeds that touch nothing in one run. |
| Dusk Sprout | Uncommon | +20 pts if planted as the last seed of your hand. | Harvest a line with the last seed of a hand. |
| Miser Sprout | Common | +1 pt per Pollen held, with no limit. | Hold 30 Pollen at once. |
| Stormy Sprout | Uncommon | +12 pts when the weather isn't Clear. | Clear a round on a day with weather (not Clear). |
| Harvester Sprout | Uncommon | +10 pts per line harvested by its seed. | Harvest 4 lines in one round. |
| Prism Sprout | Rare | +20 pts when it touches 3 or more different colors. | Plant a seed touching all 4 colors. |
| Gardener Sprout | Uncommon | +3 pts per fertilizer held. | Hold 5 fertilizers at once. |
| Cluster Sprout | Uncommon | +6 pts per other upgraded tile in its group. | Have 3 upgraded tiles in one group. |
| Rainbow Row Sprout | Uncommon | +15 pts when its row holds every color of the run. | Harvest a row holding all 4 colors. |
| Sunseeker Sprout | Common | +10 pts on Sunshine and Golden Hour days (and their indoor variants). | Clear a round on a Sunshine day. |
| Executioner Sprout | Rare | +30 pts when the front enemy is down to a quarter of its HP or less. | Defeat 10 enemies in total. |
| Spillover Sprout | Uncommon | +25 pts when its tile is harvested by a line. | Harvest 10 lines in one run. |
| Dewdrop Sprout | Uncommon | +2 Pollen per line harvested by its seed. | Harvest 3 lines in one round. |
| Keystone Sprout | Rare | +5 Mult when it sits on a corner or in the middle. | Score a seed planted on a corner. |
| Growth Sprout | Rare | +1 pt, then +1 more every time it scores this round. | Win 3 rounds in a row without a Shuffle. |
| Gambler Sprout | Uncommon | 50%: +40 pts. 50%: -10 pts. | Reach round 3. |
| Warden Sprout | Uncommon | Frees the 4 plots around it from enemy effects and makes them immune for the round. | Get a plot locked, frozen or blighted 10 times in total. |
| Aegis Sprout | Uncommon | Frees its own plot from enemy effects and makes it immune for the round. | Clear a round while an enemy has 3 plots locked or blighted. |
| Sower Sprout | Rare | Plants a free tile of its color on a neighboring empty plot. | Plant 20 seeds in one round. |
| Kin Sprout | Uncommon | +4 pts per other upgraded tile touching it. | Have 2 upgraded tiles touching each other. |
| Amplifier Sprout | Rare | Doubles the points of the upgraded tiles touching it. | Score 100 points with upgraded tiles in one placement. |
| Echo Sprout | Uncommon | Copies the best points bonus among the upgraded tiles touching it. | Unlock Kin Sprout, then plant 2 upgraded seeds in a row. |
| Collector Sprout | Uncommon | +3 pts per different upgrade in the garden. | Hold 6 different upgrades in your Seed Bag. |
| Resonance Sprout | Rare | +2 pts per upgraded tile in the garden. | Have 8 upgraded tiles on the garden at once. |
| Perennial Sprout | Rare | +1 pt, then +1 more every time any Perennial scores, all run long. | Reach round 9. |
| Crescendo Sprout | Uncommon | +2 pts per seed already planted this round. | Plant 12 seeds in one round. |
| Scholar Sprout | Uncommon | +1 pt per mastery level earned this run. | Reach mastery level 3 with a shape. |
| Full House Sprout | Uncommon | +12 pts for its row and +12 for its column when 3/4 or more of it is full. | Have 4 rows and columns 3/4 full at once. |
| Compound Sprout | Uncommon | Earns 10% of the Pollen you hold (rounded down) every time it scores. | Hold 50 Pollen at once. |
| Gap Sprout | Rare | A row or column holding it is harvested even with one empty plot. | Win a round with a row missing exactly one tile at the end. |
| Wildroot Sprout | Uncommon | Counts as one extra tile of its group. | Own a Joker seed. |
| Dye Sprout | Uncommon | Paints the tiles touching it its own color after scoring. | Recolor a seed (Chameleon, Dominant...) 5 times in one run. |
| Fertile Sprout | Uncommon | When its tile is harvested, its plot turns golden for the round. | Plant 3 Golden Cells tiles in one round. |
| Evergreen Sprout | Rare | Its tile survives harvests: it stays planted and still scores the line points. | Have a tile survive 3 harvests. |
| Pruner Sprout | Uncommon | After scoring, uproots the touching tiles of another color: +8 pts each. | Uproot 5 tiles in one run. |
| Glow Sprout | Uncommon | The empty plots touching it turn golden for the round. | Own 3 Golden plots at once. |
| Bridge Sprout | Rare | Joins its group across one empty plot to a tile of the same color. | Merge 3 separate groups with one seed. |
| Seedpod Sprout | Uncommon | When its tile is harvested, plants 2 free tiles of its color nearby. | Harvest a line holding 5 tiles of one color. |
| Mimic Sprout | Rare | Copies the upgrade of every tile of its group touching it, as if it carried them itself. | Have 2 Mirror or Echo Sprouts on the garden at once. |
| Dominant Sprout | Uncommon | Recolors the WHOLE seed to the most common color on the garden when placed, for good. | Plant 8 seeds of the same color in a row. |
| Heritage Sprout | Uncommon | Recolors the WHOLE seed to the most common color in your bag when placed, for good. | Own 20 seeds of one color in your bag. |
| Scythe Sprout | Rare | When its seed is planted, harvests the row AND the column of its tile after the count, eve | Harvest 2 lines with one seed 3 times in total. |
| Tally Sprout | Uncommon | +5 pts per line harvested this round, forced harvests included. | Harvest 6 lines in one round. |
| Sunbeam Sprout | Uncommon | Each time it scores, thaws every frozen plot. | Win a round in Winter. |
| Antidote Sprout | Uncommon | Each time it scores, cures every blighted plot and the blighted fertilizer. | Defeat Blightspore. |
| Lull Sprout | Uncommon | When placed, enemies skip their next Shuffle trick. | Defeat Bramble. |
| Dazzle Sprout | Rare | When placed, enemies lose their shield and their color or shape hatred for the round. | Defeat a boss that hates a color or a shape. |
| Wilt Sprout | Uncommon | On a turn where it scores, enemies cannot heal or grow. | Defeat Root Leech or Creeping Vine. |
| Cleanse Sprout | Uncommon | At every Shuffle, once the enemies have acted, frees one locked, frozen or blighted plot. | Clear a round with no locked or blighted plot left at the end. |
| Clone Sprout | Rare | When planted, 25% chance to add a copy of this seed to your Seed Bag (the copy has no Clon | Own 30 seeds in your bag. |
| Mirror Ward Sprout | Rare | Blighted plots next to it score normally: their points are not negated. | Score through a blighted plot 5 times in total. |
| Seedsack Sprout | Uncommon | If it is the last seed of your hand, the refill does not trigger the enemies. | Empty your hand 20 times in one run. |
| Heirloom Sprout | Rare | If it is still on the garden when the round is cleared, a plain copy of its seed joins you | Win a run. |
| Anchor Sprout | Uncommon | Enemies and weather cannot recolor, swap, move or burn this tile. | Defeat Fruitworm or Whitefly Cloud. |
| Late Bloom Sprout | Rare | From its 3rd trigger on, +1 Mult each time. Resets when a line is harvested or the round e | Win a round without harvesting a line. |

## 5. Fertilizers : 30 au départ, 103 à débloquer

Les quatre « Glow » de couleur sont retirés depuis longtemps et ne figurent pas ici.

| Fertilizer | Catégorie | Effet | Condition de déblocage |
|---|---|---|---|
| Chain | Connexions | +1 Mult if the group has 5+ cells. | **Départ** |
| Prisoner | Voisinage | +1 Mult per group cell with all 4 sides filled. | **Départ** |
| Crown | Voisinage | +1 Mult per group cell on the grid's edge. | **Départ** |
| Mason | Destruction | +2 Mult when no row/column is completed. | **Départ** |
| Poppy Devotion | Couleurs | +3 Mult on Poppy seeds. | **Départ** |
| Iris Devotion | Couleurs | +3 Mult on Iris seeds. | **Départ** |
| Sunflower Devotion | Couleurs | +3 Mult on Sunflower seeds. | **Départ** |
| Fern Devotion | Couleurs | +3 Mult on Fern seeds. | **Départ** |
| Overgrowth | Roguelike | +1 Mult per cell for seeds with 3+ cells. | **Départ** |
| Diagonal | Voisinage | +1 Mult per group cell on either main diagonal. | **Départ** |
| Nest | Voisinage | +1 Mult per group cell with exactly 3 sides filled. | **Départ** |
| Seedling | Roguelike | +1 Mult per cell for seeds with 2 or fewer cells. | **Départ** |
| Mult +1 | Roguelike | +1 Mult. No condition. | **Départ** |
| Mult +2 | Roguelike | +2 Mult. No condition. | **Départ** |
| Waxing | Roguelike | x2 Mult when the scored group has an even number of cells. | **Départ** |
| Waning | Roguelike | x2 Mult when the scored group has an odd number of cells. | **Départ** |
| Thorns | Roguelike | +4 Mult while any enemy has a lock or blight on your plots. | **Départ** |
| Pest Control | Roguelike | +1 Mult per enemy already defeated this round. | **Départ** |
| Whirlwind | Roguelike | +0.25 Mult per Shuffle you still have. | **Départ** |
| Hedgerow | Voisinage | +1 Mult for each tile of the seed you just placed that lands on the edge of the garde | **Départ** |
| Patchwork | Couleurs | +1 Mult for each tile of another color touching the seed. | **Départ** |
| Reaper | Destruction | +3 Mult per line harvested. | **Départ** |
| Row Crop | Destruction | x2 Mult when the seed harvests at least one row. | **Départ** |
| Column Crop | Destruction | x2 Mult when the seed harvests at least one column. | **Départ** |
| Bounty | Destruction | +1 Mult per tile harvested. | **Départ** |
| Underbrush | Roguelike | +1 Mult per enemy still alive. | **Départ** |
| Rain Dancer | Roguelike | +4 Mult while it rains (Rain, Snowfall, Sprinkler Shower). | **Départ** |
| Sun Dancer | Roguelike | +4 Mult on Sunshine and Golden Hour days (and their indoor variants). | **Départ** |
| Early Bird | Roguelike | +6 Mult during the first 5 rounds. | **Départ** |
| Grafter | Roguelike | +3 Mult when the seed you plant carries an upgrade. | **Départ** |
| Prism | Couleurs | x3 Mult if the seed you plant touches all 3 other colors (side by side with any of it | Plant a seed touching 3 other colors. |
| Mega Chain | Connexions | +4 Mult if the group has 10+ cells, +1 Mult per cell past that. | Form a group of 12 tiles. |
| Fortress | Voisinage | +1 Mult per fully surrounded group cell. | Fully surround 5 tiles in one round. |
| Architect | Roguelike | x2 Mult when a 2x2 block is placed. | Place a 2x2 block 5 times in one run. |
| Hoarder | Roguelike | +1 Mult per distinct color in harvested cells. | Harvest a line holding 3 colors. |
| Triad Bloom | Couleurs | x2 Mult if 2 other colors are touched. | Plant a seed touching 2 other colors. |
| Companion Bloom | Couleurs | x2 Mult if a complementary pair is touched (Poppy/Sunflower or Iris/Fern). | Win a round with all 4 colors on the garden. |
| Islet | Voisinage | x2 Mult if the seed lands as a lone single cell. | Plant 3 lone single tiles in one round. |
| Crossroads | Voisinage | +2 Mult per group cell surrounded by 2+ colors. | Have a tile surrounded by 3 colors. |
| Demolisher | Destruction | x2 Mult per row/column harvested at once, stacking (2+ only). | Harvest 3 lines at once. |
| Alchemist's Wheel | Voisinage | +4 Mult per group cell surrounded by all 4 colors. | Have a tile surrounded by all 4 colors. |
| Ashbound | Roguelike | +0.5 Mult per group cell if the group has no jokers. | Win a round with no Joker seed played. |
| Dissonant Bloom | Couleurs | +1 Mult per placed cell next to a different color. | Plant 10 seeds next to a different color in one round. |
| Momentum | Roguelike | x2 Mult when this group is bigger than the last one scored. | Score bigger groups 4 times in a row. |
| Cocooned | Voisinage | +1 Mult per group cell with all 4 diagonals filled. | Fill all 4 diagonals around a tile. |
| Gardener | Roguelike | +1 Mult per group cell next to an upgraded sprout. | Have 3 upgraded tiles touching other upgraded tiles. |
| Rainbow | Couleurs | x2 Mult per harvested line with every color of the run, stacking. | Harvest a line with every color. |
| Alternation | Couleurs | x2 Mult per harvested line alternating between 2 colors, stacking (a joker breaks it) | Harvest a line alternating between 2 colors. |
| Palindrome | Connexions | x2 Mult per harvested line whose colors read the same both ways, stacking. | Harvest a palindromic line. |
| Gradient | Connexions | PERMANENT: +1 Mult (never resets) per harvested line with no two adjacent same-color  | Harvest 3 lines with no two neighbors of the same color. |
| Block | Connexions | x2 Mult per harvested line with no isolated single-color cell, stacking. | Harvest 2 lines with no isolated tile. |
| Ashbound Line | Couleurs | x2 Mult per harvested line that's a single color, stacking. | Harvest a line of a single color. |
| Slot 1 Loyalty | Roguelike | +3 Mult when played from hand slot 1. | Play 10 seeds from slot 1 in one round. |
| Slot 2 Loyalty | Roguelike | +3 Mult when played from hand slot 2. | Play 10 seeds from slot 2 in one round. |
| Slot 3 Loyalty | Roguelike | +3 Mult when played from hand slot 3. | Play 10 seeds from slot 3 in one round. |
| Mutant Growth | Roguelike | +2 Mult if the seed isn't exactly 3 cells. | Plant 10 non-3-cell seeds in one round. |
| Solitaire | Connexions | x2 Mult when the group is only this seed (2+ cells) - nothing merged in. | Plant 3 seeds that merge with nothing in one round. |
| Fallow Ground | Roguelike | x2 Mult when the board ends up with 16 plots filled or fewer. | End a round with 16 tiles or fewer. |
| Burst | Destruction | x3 Mult when this AND the last placement both harvested a line. | Harvest a line on 3 placements in a row. |
| New Growth | Couleurs | x2 Mult when this color is new to the board. | Plant a seed whose color is new to the board 5 times in one round. |
| Bridge | Connexions | x2 Mult per extra pre-existing group bridged together, stacking. | Merge 2 groups with one seed. |
| Encirclement | Voisinage | +1 Mult per group cell boxed in on all 8 sides (filled or edge). | Surround a tile on all 8 sides. |
| Sealer | Voisinage | +1 Mult per pre-existing sprout this placement newly encircles. | Encircle 3 sprouts with one seed. |
| Thriving Colony | Couleurs | x2 Mult when this color forms only one connected group on the board. | Win a round with a single group of each color. |
| Repetition | Roguelike | xn Mult for n consecutive same-shape placements (x2 on the 2nd, x3 on the 3rd...); re | Plant the same shape 4 times in a row. |
| Changeling Bloom | Couleurs | +2 Mult when this seed's color differs from the last one played. | Plant 6 different colors in a row (alternating). |
| Combo | Destruction | x2 the whole score if the last placement harvested a line. | Harvest a line on 2 placements in a row. |
| Precision | Voisinage | +1 Mult per cell if every cell of the seed touches a filled sprout. | Plant a seed whose every cell touches a tile 5 times. |
| Thicket | Voisinage | +1 Mult per cell if every cell of the seed touches 2+ filled sprouts. | Plant a seed whose every cell touches 2 tiles. |
| Lone Bloom | Voisinage | x2 Mult when the seed touches exactly 1 filled sprout, total. | Plant a seed touching exactly one tile 5 times in a round. |
| Wildcard | Roguelike | Joker sprouts count as the color of one of your Devotion fertilizers. | Own 2 Joker seeds. |
| Root Density | Roguelike | +n Mult, n = filled cells on the board ÷ 10 - scales with how full the board is. | Fill the garden to 25 tiles. |
| Mult +4 | Roguelike | +4 Mult. No condition. | Reach round 7. |
| Solidarity | Roguelike | +1 Mult per 2 fertilizers held, this one included. | Hold 4 fertilizers at once. |
| Mimic | Roguelike | Instantly copies whatever fertilizer you bought right before it. Does nothing on your | Buy 3 fertilizers in one visit to the shop. |
| Volatile Brew | Roguelike | +5 Mult, but 1-in-5 chance to lose it at the end of every round. | Lose a fertilizer to a round end or enemy (Rotgrub, Autumn...). |
| Enchanted Seeds | Roguelike | +0.1 Mult per upgraded seed in your seed bag, starting from a baseline of 1. | Have 8 upgraded seeds in your bag. |
| Dwindling | Roguelike | +13 Mult, shrinking a little with every placement for the rest of the run - destroyed | Win 5 rounds in a row. |
| Multitude | Roguelike | +1 Mult per 10 seeds currently in your seed bag. | Own 40 seeds in your bag. |
| Seasoned Growth | Roguelike | +0.1 Mult per upgraded seed you've PLAYED this run, starting from a baseline of 1. | Play 30 upgraded seeds in one run. |
| Seedling Specialist | Formes | x2 Mult on seeds with 2 or fewer cells (Single, Domino). | Play 15 seeds of 1-2 cells in one round. |
| Sapling Specialist | Formes | x2 Mult on 3-cell seeds (any Tromino). | Play 10 three-cell seeds in one round. |
| Overgrowth Specialist | Formes | x2 Mult on 4-cell seeds (Square or any Tetromino). | Play 10 four-cell seeds in one round. |
| Seedling Glow | Formes | +1 Mult per group cell on seeds with 2 or fewer cells (Single, Domino). | Win a round using only 1-2 cell seeds. |
| Sapling Glow | Formes | +1 Mult per group cell on 3-cell seeds (any Tromino). | Win a round using only 3-cell seeds. |
| Overgrowth Glow | Formes | +1 Mult per group cell on 4-cell seeds (Square or any Tetromino). | Win a round using only 4-cell seeds. |
| Hardy Growth | Roguelike | +1 Mult per distinct fertilizer category you hold at least one of. | Hold fertilizers of 4 different categories. |
| Chromatic Hoard | Couleurs | +2 Mult per color for which you hold its Devotion fertilizer. | Hold 3 Devotion fertilizers at once. |
| Cadence | Roguelike | x2 Mult when this placement matches BOTH a held Pair/Impair condition AND a held Form | Hold a Waxing/Waning and a Specialist at once. |
| Echo | Roguelike | Replays the fertilizer right before it in your order, as if you held a second copy of | Hold the fertilizer Mimic and one more in the same run. |
| Fourth Slot | Roguelike | Adds a 4th hand slot. | Win a run. |
| Barometer | Roguelike | +3 Mult whenever the round's weather isn't Clear skies. | Clear 3 rounds with weather in one run. |
| Storm Chaser | Roguelike | x2 Mult while the weather is Frost, Heatwave or Hurricane. | Clear a round in Frost, Heatwave or Hurricane. |
| Weatherproof | Roguelike | Frost, Heatwave and Hurricane have no effect on your garden. | Clear 3 rounds with weather in a Summer run. |
| Haste | Roguelike | +0.1 Mult per seed still left to play this round. | Clear a round with 5 or more seeds left. |
| Cornerstone | Voisinage | x2 Mult when the seed puts a tile on one of the 4 corners. | Plant a seed on 4 corners in one round. |
| Heartwood | Voisinage | x2 Mult when the seed puts a tile in the middle of the garden. | Plant in the middle 5 times in one round. |
| Trellis | Formes | +3 Mult when the seed is a straight vertical piece (1 wide, 2 or more tall). | Play 10 vertical pieces in one round. |
| Furrow | Formes | +3 Mult when the seed is a straight horizontal piece (1 tall, 2 or more wide). | Play 10 horizontal pieces in one round. |
| Beanstalk | Formes | +1 Mult when the seed is a straight piece of 3 or more tiles. | Play 5 straight 3-cell pieces in one round. |
| Taproot | Connexions | +1 Mult per 3 cells in the scored group. | Form a group of 9 tiles. |
| Snug Fit | Voisinage | +5 Mult when every plot touching the seed is already planted (or the edge). | Plant a seed fully surrounded. |
| Mosaic | Couleurs | +3 Mult when the seed touches 2 or more tiles and none of them shares its color. | Plant a seed touching 2 tiles of other colors. |
| Crowd | Voisinage | x2 Mult while two thirds of the garden or more is planted. | Fill two thirds of the garden. |
| Wildflower Meadow | Couleurs | +4 Mult when every color of the run is growing in the garden. | Win a round with every color on the garden at the end. |
| Wreath | Voisinage | +1 Mult per tile of the seed touching 3 or more planted plots (the seed's own other t | Plant a seed whose tiles touch 3 plots each. |
| Cross Harvest | Destruction | +6 Mult when one seed harvests a row and a column at once. | Harvest a row and a column with one seed. |
| Clean Slate | Destruction | x3 Mult when the seed harvests the last planted tile in the garden. | Clear the garden completely. |
| Frostbreaker | Destruction | +6 Mult when the seed breaks a frozen plot free. | Break a frozen plot with a line. |
| Seasoned | Roguelike | +0.5 Mult per round already cleared this run. | Win a Spring run. |
| Underdog | Roguelike | +3 Mult while the enemies still have more than half of their total HP. | Clear a round with the enemies still above half HP at 5 seeds left. |
| Finishing Blow | Roguelike | +6 Mult while the enemies are down to a quarter of their total HP or less. | Finish an enemy with 3 seeds left. |
| Last Seed | Roguelike | x3 Mult on the last seed of the round. | Win a round on its last seed. |
| First Seed | Roguelike | x2 Mult on the first seed of the round. | Harvest a line with the first seed of a round. |
| Piggy Bank | Roguelike | +0.1 Mult per Pollen you hold. | Hold 40 Pollen at the end of a round. |
| Seed Bank | Roguelike | +0.05 Mult per seed in your bag. | Own 25 seeds in your bag. |
| Frost Hardy | Roguelike | +4 Mult while the weather is Frost (or the Freezer Door Ajar). | Clear a round in Frost. |
| Windrider | Roguelike | +4 Mult in Wind and Hurricane weather (and their indoor variants). | Clear a round in Wind. |
| Marathoner | Roguelike | +4 Mult from round 9 onwards. | Reach round 9. |
| Bossbane | Roguelike | x2 Mult during boss rounds. | Defeat a boss. |
| Charmed Patch | Roguelike | +1 Mult per upgraded tile in the scored group. | Score 3 upgraded tiles in a group. |
| Twin Graft | Roguelike | +5 Mult when the seed you plant carries 2 or more upgrades. | Plant a seed with 2 upgrades. |
| Botanist | Roguelike | +1 Mult per different kind of upgrade in the garden. | Have 5 different upgrades on the garden. |
| Hothouse | Roguelike | +1 Mult per 2 upgraded tiles in the garden. | Have 8 upgraded tiles on the garden. |
| Good Neighbors | Voisinage | +2 Mult per upgraded tile touching the seed you plant. | Plant an upgraded seed touching 3 upgraded tiles. |
| Gleaner | Destruction | +2 Mult per upgraded tile harvested by this placement. | Harvest 3 upgraded tiles at once. |
| Wild Cluster | Roguelike | +0.5 Mult per Joker tile in the scored group. | Score a group with 2 Jokers. |
| Wild Reserve | Roguelike | +0.1 Mult per Joker seed in your bag. | Own 3 Joker seeds. |
| Wild Surge | Roguelike | x2 Mult when the scored group contains a Joker tile. | Win a round with a Joker in the scored group 5 times. |
| Crop Rotation | Formes | +0.1 Mult each time you plant a different shape than the last seed this round, stacki | Plant 6 different shapes in one round. |
| Greenhouse Lamp | Roguelike | +0.5 Mult per Shuffle you have left. | Win a Greenhouse round. |
| Seed Library | Formes | +0.5 Mult per different shape in your Seed Bag. | Own all 8 shapes in your bag and win a round. |
| Twin Bloom | Couleurs | Plant the same color as the last seed: +1 Mult, then +2, +3... for each seed in a row | Plant 5 seeds of the same color in a row. |

## 6. À suivre techniquement (compteurs et événements)

Toutes les conditions reposent sur ce que le Core sait déjà ou peut compter facilement :
- **Par placement :** lignes récoltées, score, tuiles améliorées dans le groupe, groupes fusionnés, couleurs touchées, coins, centre, bords, taille du groupe, tuiles détruites ou déracinées.
- **Par manche :** nombre de graines posées, lignes, poses sans récolte, Shuffles utilisés, graines restantes, météo, ennemis vaincus.
- **Par run :** manche atteinte, victoire, challenge, fertilizers et upgrades tenus, taille du sac, Pollen tenu, Jokers possédés, formes dans le sac.
- **Cross-run (sauvegarde) :** ensemble d'ids débloqués (comme `CollectionProgress`), victoires par challenge et par niveau, ennemis vaincus par type.
- Un **`UnlockTracker`** pur dans le Core reçoit ces événements, applique les conditions et renvoie les nouveaux déblocages (testable sans Unity).
- Le **pool de boutique** (`UpgradeSystem`, tirage des fertilizers, packs, Forge, Compost) filtre sur le catalogue débloqué.
- **Simulateur et planner :** option `--unlock-all` (par défaut pour les études de balance) et option `--locked` pour simuler un nouveau joueur.
- **Migration :** un joueur existant garde ce qu'il a déjà découvert dans la Collection (ou tout est débloqué une fois), à décider.

## 7. Points à valider avant de coder

1. Le noyau de départ (25 Sprouts, 30 fertilizers) : trop large, trop étroit, un objet à échanger ?
2. Les conditions : faisables mais pas triviales ? Lesquelles sont trop faciles ou trop dures ?
3. Les 5 conditions de challenge et les 5 niveaux de difficulté.
4. Les Stars : on les supprime complètement ou on les garde comme score cosmétique ?
5. Migration des joueurs existants (tout débloquer une fois, ou reprendre la Collection).
6. Pool d'ennemis par challenge : à faire dans une passe séparée.
