# Équilibrage : où sont les chiffres, comment les mesurer

## Où vivent les chiffres

| Quoi | Fichier |
|---|---|
| Graines par round (budget), quotas de score (Classic : affichage seulement, les rounds se gagnent en tuant les ennemis) | `Core/Run/RunConfig.cs` (`PieceBudgets`, `Quotas`) ; variantes dans `Core/Run/ChallengeCatalog.cs` |
| Ennemis par round | `Core/Enemies/EncounterCatalog.cs` |
| PV, rôle et texte de chaque ennemi | `Core/Enemies/EnemyCatalog.cs` |
| Constantes de score (ligne récoltée 3/6/9, ×2 grille vidée, fractions Sap Drain/Siphon et plafond, bonus dorés…) | `Core/Scoring/ScoringConstants.cs` |
| Prix des fertilizers | `Core/Economy/ModifierPricing.cs` ; économie générale dans `Core/Economy/EconomyConstants.cs` |
| Effet de chaque fertilizer | `Core/Grid/GridManager.cs` (tables `BuildPreClearEffects` / post-clear) et `Core/Modifiers/ModifierDefinition.cs` (texte) |

## Mesurer avant de toucher

```
cd Tools/Headless/RunSim
dotnet run -- --runs 2000 --policy greedy
```

Lire : taux de réussite par round (« Cleared-round rate »), round où les runs finissent, cause des défaites, fertilizers
corrélés avec les runs profondes. Le bot est volontairement simple : ses chiffres servent à **comparer avant/après**, pas à
estimer le niveau d'un bon joueur. Relancer avec la même graine (`--seed`) pour comparer deux versions.

## Historique des réglages

- Round 2 : 24 → 28 graines, Bramble 750 → 600 PV (réussite du bot 58 % → 87 %).
- Round 8 : 22 → 28 graines, Deadroot 2350 → 2000 PV (27 % → 54 %).
- Blightmoth : debut moved from round 11 to round 7 (replaces the Bramble there), 1750 → 800 PV so round 7 stays near its neighbours (64 % for the bot).
- Sap Drain : 25 % → 8 % des dégâts en Pollen, Siphon : 15 % → 4 %, plafond de 10 Pollen par déclenchement.

## Météo et ennemis à minuterie

- **Météo** (`Core/Run/WeatherCatalog.cs`, constantes dans `ScoringConstants`) : tirée une fois par run, le round 1 est toujours dégagé et deux rounds de suite n'ont jamais la même météo. Chaque météo a un poids de tirage (commune 3, peu commune 2, rare 1, voir `GetWeight`). Pluie/Soleil visent une couleur de graine tirée au hasard pour chaque round (dans le même tirage que la météo, donc sans décaler le hasard du reste du run) : Pluie +3 Pollen par graine de cette couleur, Soleil +4 pts par case ; Vent : un Shuffle gratuit ; Gel : 2 cases gelées ; Rosée : +2 Pollen par ligne ; Heure dorée : 2 cases dorées ; Averse de pollen : +10 Pollen. Deux météos négatives : Canicule (Heatwave, poids 2) brûle une tuile plantée à chaque Shuffle manuel ; Ouragan (Hurricane, poids 1) déplace 2 tuiles vers d'autres cases vides toutes les 4 graines posées (les cases verrouillées ne bougent pas, et un déplacement peut compléter une ligne). La météo du round suivant est annoncée dans la boutique (boîte « Next round », avec tooltip) et la météo courante est dans la puce à côté du numéro de round.
- **Ennemis à minuterie** : seul Bramble agit aussi toutes les 6 graines posées (`ScoringConstants.EnemyTickInterval`) : il pose un second verrou à côté de celui du Shuffle. Blightmoth n'a plus de minuterie (son blight ne part qu'au Shuffle). Le compte à rebours est affiché sur le portrait de Bramble.
- Les tests qui comptent des cases verrouillées/gelées appellent `DebugDisableWeather()` pour ne pas dépendre de la météo tirée.

## Connu mais pas traité

- Les rounds 3 à 6 restent les plus durs pour le bot (58–75 % de réussite) ; le round 3 est maintenant le premier mur.
- `RunConfig.Quotas` n'est plus la condition de victoire de Classic (restée pour l affichage et Marathon).

## Après un changement de valeur

L'étiquette dans le coin d'une icône de fertilizer est écrite dans `Design/Icons/generate_icons.py`. `python3 Tools/check_icon_tags.py`
(lancé aussi par le CI headless) échoue si une étiquette n'est plus dans la description du fertilizer ; corriger alors la table du générateur,
relancer `generate_icons.py` et re-rendre le PNG dans `Assets/Resources/Icons/Fertilizers/`.
- **PV des ennemis (Acte 2)** : jusqu'au round 8 les PV sont ceux du catalogue ; à partir du round 9 ils sont multipliés par `EnemyHpGrowthPerRound` (1,12) cumulé à chaque round (×1,12 au round 9 … ×2,2 au round 15, voir `EncounterCatalog.GetHpScale`). Raison : les multiplicateurs de fertilizers se composent entre eux, des PV plats rendaient la fin de run triviale (≈70 % de victoires pour le bot glouton une fois le round 8 passé). Avec 1,12 : ≈31 % ; 1,08 ≈ 38 % ; 1,17 ≈ 23 % ; 1,25 ≈ 6 % (simulateur glouton, 6 000 runs, graine 77). Le bot est bien plus faible qu'un humain : viser plus bas que la cible réelle.

## Challenges (Spring, Winter, Summer, Autumn, Orchard, Greenhouse) et Endless

- **Spring** (Classic) et **Winter** (Marathon : sac réduit, budgets serrés, rounds à quota sans ennemis) existaient déjà. Wildgrowth a été retiré.
- **Summer** : météo réelle à chaque round (jamais « Clear skies » après le round 1, poids des météos dures relevés, voir `WeatherCatalog.GetWeight(id, profile)`), effets ×1,5 (`WeatherIntensity`, arrondi au plus proche, jamais sous 1).
- **Autumn** : un fertilizer se flétrit après 4 rounds terminés (`FertilizerLifespanRounds`, âge suivi en parallèle de `_activeModifiers`), +10 Pollen à la fin de chaque round, ennemis ×0,85 PV. Le bot glouton y est mauvais (il ne rachète pas intelligemment) : à ajuster aux tests humains.
- **Orchard** : 2 couleurs tirées au hasard au début du run (`PieceColorUtility.BaseColors` est donc dynamique), sac de 32 graines (copies doublées), ennemis ×1,7 PV, et la boutique n'offre plus les fertilizers qui ne pourraient jamais se déclencher (Prism, Triad Bloom, couleurs absentes, Companion Bloom sans paire).
- **Greenhouse** : grille 5×5 (`GridManager.Size` est dynamique, fixé par le `RunManager`), cases agrandies pour garder la même empreinte à l'écran, ennemis ×0,9 PV.
- **Endless** (tous les challenges) : après la victoire au round 15, « Keep Growing » enchaîne sur la boutique puis les rounds 16+. Les rosters de l'Acte 2 (rounds 9 à 15) tournent en boucle, PV ×1,2 par round en plus de la croissance de l'Acte 2 (`EnemyHpGrowthPerRoundEndless`) ; Winter (sans ennemis) voit ses quotas ×1,75 par round. La victoire est comptée une seule fois ; chaque round supplémentaire rapporte 1 Star à la fin (`RecordEndlessEnd`).
- Simulateur : `--challenge Summer|Autumn|Orchard|Greenhouse|Winter` et `--endless`. Résultats du bot glouton (1 500 runs) : Classic 2,0 % de victoires, Summer 2,0 %, Orchard 1,7 %, Greenhouse 0,7 %, Autumn ≈ 0 % (voir plus haut).

### Passe d'équilibrage « ≥ 5 % de victoires » (bot glouton, 3 000 runs, graine 4001)

Chaque challenge a ses propres leviers dans `ChallengeCatalog` : `EnemyHpMultiplier`, `PieceBudgetBonus` (graines en plus à chaque round) et `QuotaMultiplier` (inutilisé : tous les challenges combattent des ennemis).

| Challenge | PV ennemis | Graines en plus | Autre | Victoires du bot |
|---|---|---|---|---|
| Spring | ×0,72 | +6 | | 7,6 % |
| Winter | ×0,8 | +10 | sac réduit (13 graines), budgets serrés | 6,5 % |
| Summer | ×0,72 | +6 | météo ×1,5 | 7,0 % |
| Autumn | ×0,3 | +12 | fane après 4 rounds, +10 Pollen, un fertilizer aléatoire repousse | ≈ 8 % |
| Orchard | ×1,4 | +6 | 2 couleurs | 6,4 % |
| Greenhouse | ×0,5 | +10 | grille 5×5 | 6,1 % |

Attention : le bot est bien plus faible qu'un humain (il perd surtout faute de graines, pas de PV), donc ces réglages rendent les challenges nettement plus faciles pour un joueur réel. Autumn est le cas extrême : le bot ne sait pas reconstruire ses fertilizers, il faut un PV ×0,3 pour qu'il gagne 5 % ; à resserrer dès qu'on a des parties humaines.
- Tous les challenges affrontent les mêmes 15 rounds d'ennemis (`ChallengeDefinition.UsesEncounters`, vrai partout). Winter (ex-Marathon, rounds à quota sans ennemis) a été converti ; les tests qui ont besoin d'un round sans ennemi utilisent `TestChallenges.QuotaOnlyMarathon`.

### Courbe de difficulté entre challenges (Spring = didacticiel)

Les réglages ont été resserrés pour que la difficulté monte d'un challenge à l'autre (victoires du bot glouton, 3 000 runs, graine 31337) : **Spring ≈ 12,5 %** (PV ×0,5, +6 graines), **Winter ≈ 7,4 %** (PV ×0,8), **Summer ≈ 6,8 %** (PV ×0,72), **Orchard ≈ 6,1 %** (PV ×1,4), **Greenhouse ≈ 5,9 %** (PV ×0,55, +10 graines), **Autumn ≈ 5,4 %** (PV ×0,35, +12 graines). Tous restent au-dessus de 5 %, le plus facile (Spring) fait environ le double du plus dur. Le bot reste bien plus faible qu'un humain.

## Nouveaux fertilizers (Barometer, Storm Chaser, Weatherproof, Thorns, Pest Control, Haste, Whirlwind, Cornerstone)

- **Barometer** (5 Pollen) +3 Mult si la météo n'est pas « Clear skies » ; **Storm Chaser** (6) ×2 Mult en Frost/Heatwave/Hurricane ; **Weatherproof** (4) annule Frost (cases gelées), Heatwave (brûlure) et Hurricane (déplacement) ; **Thorns** (6) +4 Mult tant qu'un ennemi vivant a un verrou ou un blight actif ; **Pest Control** (7) +1 Mult par ennemi déjà vaincu dans la manche ; **Haste** (7) +0,1 Mult par graine restante dans la manche ; **Whirlwind** (6) +0,25 Mult par Shuffle en réserve ; **Cornerstone** (6, catégorie Voisinage) ×2 Mult si une case posée est dans un des 4 coins (suit la taille de la grille, donc Greenhouse).
- Les valeurs initiales de Haste (0,2) et Whirlwind (0,5) faisaient gonfler le bot (≈ +35 % de victoires) : elles ont été divisées par 2. Après réglage, victoires du bot (graine 31337, 3 000 runs) : Spring 13,3 %, Winter 7,7 %, Summer 7,7 %, Orchard 6,0 %, Greenhouse 5,6 %, Autumn 6,7 %.
- Implémentation : Cornerstone est évalué par la grille (`GridManager`), les autres par `RunManager.ApplyDeckStateModifierBonuses` (état de run : météo, ennemis, graines, Shuffles). `RunManager.IsModifierUsableThisRun` filtre toujours la boutique d'Orchard.

## Un monde par challenge (ennemis résidents)

Chaque challenge hors Spring a son propre ennemi (`ChallengeDefinition.WorldEnemy`), qui rejoint la composition **une manche sur trois** (manches 3, 6, 9, 12, 15… Endless inclus), juste derrière le Basic de tête ; une composition pleine (6) perd son dernier ennemi pour lui faire de la place. Tous agissent à chaque Shuffle (manuel ou refill automatique) :

| Monde | Ennemi (PV de base) | Effet |
|---|---|---|
| Winter | Hoarfrost (1500) | gèle 1 case vide par Shuffle ; 3 cases gelées max (la plus ancienne dégèle), tout dégèle à sa mort |
| Summer | Locust Swarm (1400) | dévore 3 Pollen par Shuffle (jamais plus que ce que tu as) |
| Autumn | Rotgrub (1300) | vieillit d'1 manche le fertilizer le plus proche de faner |
| Orchard | Fruitworm (1400) | change une tuile plantée en l'autre couleur |
| Greenhouse | Whitefly Cloud (1450) | échange deux tuiles plantées |

Rééquilibrage après leur arrivée (victoires du bot, graines 31337 / 4001) : Spring ≈ 13-14 %, Winter 6,1-6,8 %, Summer 6,5-7,0 %, Autumn 6,1-7,9 % (PV ×0,36), Orchard 5,9-6,6 % (PV ×1,2), Greenhouse 5,2-6,3 % (PV ×0,5).

## Lot de 40 fertilizers (Hedgerow … Granary)

- **Grille, avant les lignes** (`GridManager.NewModifiers.cs`) : Hedgerow, Heartwood, Trellis, Furrow, Beanstalk, Patchwork, Taproot, Snug Fit, Mosaic, Crowd, Wildflower Meadow, Wreath, plus les trois qui rapportent du Pollen (Pollinator, Honey, Nectar).
- **Grille, après les lignes** : Cross Harvest, Reaper, Clean Slate, Tithe (Pollen), Row Crop, Column Crop, Bounty, Frostbreaker. Clean Slate se cumule avec le ×2 intégré quand la grille est vidée.
- **État de la run** (`RunManager.NewModifiers.cs`) : Seasoned, Underdog, Finishing Blow, Last Seed, First Seed, Piggy Bank (plafonné à +5), Seed Bank, Underbrush, Rain Dancer, Sun Dancer, Frost Hardy, Windrider, Early Bird, Bossbane, Marathoner ; Rain Barrel et Granary paient du Pollen à la fin de la manche (ajoutés à `LastRoundEndLueurBonus`).
- `IsModifierUsableThisRun` écarte ceux qui ne pourraient jamais se déclencher : Underdog, Finishing Blow et Underbrush sans ennemis, Frost Hardy dans un monde sans gel (Summer).
- Prix 4 à 7 Pollen ; les valeurs sont dans `ScoringConstants` / `EconomyConstants` (section « 40 fertilizers »).

## Tile upgrades à points de placement (19)

Nouveaux `PieceTraitKind` / `UpgradeId` du pool Grid, tous évalués après le placement par `RunManager.TileUpgrades.cs` (événement `Trait` sur la tuile enchantée) :
Pip (+1), Lucky (0 à 20 au hasard), Companion (+8 par voisin d'une autre couleur, +25 par voisin de la couleur choisie, la couleur suivante du run), Colony (+20 si le groupe dépasse 10 cases), Border (+8 sur le bord), Heart (+12 au centre), Corner (+30 sur un coin), Hermit (+12 sans voisin), Cuddly (+3 par tuile autour, diagonales et reste de sa propre graine compris), Dawn (+12 parmi les 3 premières graines), Dusk (+20 sur la dernière graine de la main), Miser (+1 par Pollen, sans limite), Stormy (+12 hors Clear skies), Elder (+2 par manche déjà gagnée), Harvester (+10 par ligne récoltée), Breeze (+3 par Shuffle restant), Harmony (+10 si tous les voisins ont sa couleur), Prism (+20 avec 3 couleurs voisines ou plus), Gardener (+3 par fertilizer). Les voisins comptent même si la ligne vient d'être récoltée.
Les valeurs sont dans `ScoringConstants` (section « tile upgrades »). Victoires du bot après ajout : Spring 13,1 %, Greenhouse 6,7 %.

## Tile upgrades permanentes

Une tuile améliorée qui fait partie du groupe marqué par un placement est recomptée : les effets à drapeau (Golden, Tinted, Multiplier, Blast, Beacon) sont ré-estampillés avant le calcul de `GridManager`, les autres (Mirror, Twin, Spark, Void, Venom et les 19 à points fixes) sont rejoués après le placement (`RunManager.TileUpgrades.cs`, `ApplyPersistentTraitBonuses`). Les voisins d'une ancienne tuile comptent les tuiles de la nouvelle graine.
Après ce changement le bot gagnait trop (Spring 16,6 %, Autumn 10,2 %, Orchard 10,1 %, Greenhouse 8,1 %). `EnemyHpMultiplier` relevés : Spring 0,5 → 0,58, Summer 0,72 → 0,8, Autumn 0,36 → 0,41, Orchard 1,2 → 1,35, Greenhouse 0,5 → 0,54 (Winter inchangé, 0,8). Victoires du bot : Spring 14,7 %, Winter 6,3 %, Summer 7,7 %, Autumn 6,3 %, Orchard 7,9 %, Greenhouse 5,9 %.

## Recalibrage sur le planner (profondeur 3)

Le bot de référence est maintenant `--policy planner` (PLAN_DEPTH=3, 400 à 500 parties par challenge ; `--hp X` surcharge le multiplicateur de PV pour un balayage sans recompiler). `EnemyHpMultiplier` : Spring 0,58 (inchangé), Winter 0,8 → 0,9, Summer 0,8 → 0,9, Autumn 0,41 → 0,47, Orchard 1,35 → 1,5, Greenhouse 0,54 → 0,62.
Victoires du planner : Spring 16,0 %, Winter 9,0 %, Summer 9,2 %, Autumn 8,6 %, Orchard 9,0 %, Greenhouse 9,2 %. Le bot glouton, plus faible, tombe à Spring 13,9 %, Winter 4,8 %, Summer 6,2 %, Autumn 4,0 %, Orchard 6,5 %, Greenhouse 4,2 % : le plancher de 5 % ne vaut plus pour lui.

## Cible 4 à 6 % pour le planner (hors Spring)

Les 9 % étaient trop généreux : `EnemyHpMultiplier` Winter 0,9 → 1,1, Summer 0,9 → 1,2, Autumn 0,47 → 0,57, Orchard 1,5 → 1,75, Greenhouse 0,62 → 0,79 (Spring reste à 0,58, le tutoriel). Victoires du planner profondeur 3 (500 à 600 parties) : Spring ≈ 16 %, Winter 5,2 %, Summer 4,8 %, Autumn 5,4 %, Orchard 5,2 %, Greenhouse 5,5 %. Le glouton, plus faible, y tombera nettement plus bas.

Spring à 8-10 % : `EnemyHpMultiplier` 0,58 → 0,87 (planner profondeur 3, 1000 parties : 8,8 %). Spring reste le plus facile des six, au-dessus de la fourchette 4-6 % des autres.

## Casino : plus de Sprout Upgrades

Probabilité de catégorie d'une carte du Casino : Fertilizer 10 %, Mastery 40 → 25 %, Seed 40 → 25 %, Sprout 10 → 40 %. Le bot (qui choisit ses tuiles au hasard et profite donc peu des tuiles) a perdu de la force avec moins de Mastery et de Seed ; `EnemyHpMultiplier` réajustés (planner profondeur 3) : Spring 0,87 → 0,76 (8,3 %), Winter 1,1 → 0,98 (5,4 %), Summer 1,2 → 1,12 (5,7 %), Autumn 0,57 → 0,54 (4,8 %), Orchard 1,75 (5,6 %), Greenhouse 0,79 → 0,84 (5,3 %).

## 16 tile upgrades de plus (Cluster … Sower)

Cluster (+6 par autre tuile améliorée du groupe), Crossfire (+1 par tuile de sa ligne et colonne), Even (+12, groupe pair), Rainbow Row (+15, ligne avec toutes les couleurs), Sunseeker (+10 au soleil), Hunter (+3 par ennemi vivant), Executioner (+30 quand l'ennemi de tête est à 25 % de PV ou moins), Spillover (+25 quand sa tuile est récoltée), Honeycomb (+1 Pollen), Dewdrop (+2 Pollen par ligne), Keystone (+5 Mult dans un coin ou au centre), Growth (+1 puis +1 de plus à chaque score de la manche, max 30), Gambler (50 % +40 / 50 % −10), Warden (libère et immunise les 4 plots voisins), Aegis (idem pour son propre plot), Sower (plante une tuile gratuite voisine avant le pointage, sans limite).
Immunité (`Cell.IsImmune`) : les locks, la glace, le poison et la contamination ne retombent plus sur le plot jusqu'à la fin de la manche. Les tuiles semées n'ont ni score ni compteur.
Le pool Grid grossit (35 → 51 upgrades) et dilue les fertilizers du Blister ; `EnemyHpMultiplier` réajustés sur le planner profondeur 3 (700 parties) : Spring 0,76 (9,4 %), Winter 1,03 (5,1 %), Summer 1,0 (5,9 %), Autumn 0,49 (4,7 %), Orchard 1,65 (4,9 %), Greenhouse 0,8 (5,0 %).


## Workshop : le Casino est remplacé

Le Casino (cartes mystère aléatoires) est remplacé par le **Workshop** : 2 packs distincts (Seed 4, Sprout 5, Fertilizer 6, Mastery 4 Pollen ; on révèle 3 options visibles et on en garde une, Skip possible sur Sprout/Fertilizer), le **Compost** (3 Pollen, +2 par achat : retire une graine du sac) et la **Forge** (4 Pollen : une tile upgrade visible, posée sur une graine au choix parmi 10). Le Blister est inchangé. Le bot (choix de pack au hasard, jamais de Compost) a perdu de la force ; `EnemyHpMultiplier` réajustés (planner profondeur 3, 500 runs) : Spring 0,76 → 0,47 (≈ 9 %), Winter 1,03 → 0,73 (5,2 %), Summer 1,0 → 0,71 (≈ 6 %), Autumn 0,49 → 0,37 (≈ 6 %), Orchard 1,65 → 1,2 (≈ 6 %), Greenhouse 0,8 → 0,58 (≈ 6 %). Un humain choisit ses packs en connaissance de cause : ces cibles restent prudentes.

## Tile upgrades sans plafond

Demande : « retire toutes les limites des tile upgrades, c'est ce qui rend le fun ». Les deux seuls plafonds chiffrés des tile upgrades sont retirés : Miser Sprout (+1 par Pollen, ancien max +15) et Growth Sprout (+1 de plus à chaque score de la manche, ancien max +30). Lucky (0 à 20 au hasard) est une plage de tirage, pas un plafond : inchangé. Une graine ne porte toujours qu'une seule tile upgrade (en poser une nouvelle remplace l'ancienne).

## 11 tile upgrades de plus, pensées pour l'empilement

Une graine peut maintenant porter une tile upgrade par tuile (voir DECISIONS.md) et plus rien n'est plafonné : voici onze upgrades de points qui en profitent (toutes en points ou en Pollen, aucune en Mult).

Kin (+4 par autre tuile améliorée qui la touche), Amplifier (les points des tuiles améliorées voisines comptent deux fois), Echo (copie le plus gros bonus de points d'une tuile améliorée voisine), Collector (+3 par type d'upgrade différent dans le jardin), Resonance (+2 par tuile améliorée dans le jardin), Pouch (+1 par graine améliorée dans le sac), Perennial (+1 puis +1 de plus à chaque score de n'importe quel Perennial, tout au long de la partie), Crescendo (+2 par graine déjà plantée dans la manche), Scholar (+1 par niveau de mastery gagné), Full House (+12 pour sa ligne et +12 pour sa colonne si les trois quarts sont pleins), Compound (gagne 10 % du Pollen détenu à chaque score).

Amplifier et Echo se résolvent en dernier (ils lisent ce que les autres upgrades viennent de marquer) et un bonus copié n'est jamais recopié. Les PV ennemis et les simulations n'ont pas été re-réglés.

## Fertilizers en Mult : plus aucun fertilizer en points

Demande : « change les fertilizers pour qu'ils soient Mult au lieu de points ». Les 31 fertilizers qui donnaient des points (Chain, Mega Chain, Fortress, Prisoner, Hoarder, Crown, Crossroads, Alchemist's Wheel, Ashbound, Dissonant Bloom, Cocooned, Gardener, Overgrowth, Mutant Growth, Diagonal, Nest, Seedling, Encirclement, Sealer, Precision, Thicket, Dwindling, Multitude, les trois Glow, Hedgerow, Beanstalk, Patchwork, Wreath, Bounty) donnent maintenant du +Mult, avec les mêmes conditions et le même comptage (par tuile, par couleur, etc.). Conversion : chaque événement de points devient `max(1, arrondi(points / 12))` de Mult additif (`ScoringConstants.PointsToMult`, réglable via `PointsPerMult`). Cas particuliers : Dwindling (+13 Mult qui décroît à chaque placement), Multitude (+1 Mult par 10 graines dans le sac). Les règles de poison et de couleur maudite ne touchent que les points : un fertilizer en Mult n'est plus annulé par une tuile empoisonnée.

Une première simulation (planner profondeur 3) à PV inchangés donnait 14 à 35 % de victoires : le Mult rend les parties bien plus faciles. `EnemyHpMultiplier` réajustés (500 runs) : Spring 0,47 → 1,62 (≈ 10 %), Winter 0,73 → 1,85 (5,2 %), Summer 0,71 → 1,7 (4,8 %), Autumn 0,37 → 1,5 (≈ 5,5 %), Orchard 1,2 → 3,4 (≈ 5 %), Greenhouse 0,58 → 1,55 (5,2 %). Ces réglages tiennent compte des 11 nouvelles tile upgrades, des upgrades empilables et du retrait des plafonds. La simulation a aussi révélé une lecture hors limites dans Kin / Collector / Resonance sur le bord du jardin (corrigée).

## 5 tile upgrades qui changent le jardin

Gap (une ligne ou colonne qui le contient est récoltée avec une case vide ; l'espace vide reste vide, un Gap par case vide pardonnée), Wildroot (compte comme une tuile de plus dans son groupe : plus de points de groupe et les fertilizers à seuil de taille se déclenchent plus tôt), Dye (peint ses voisines de sa couleur après le score : le groupe grandit), Fertile (quand sa tuile est récoltée par une ligne, sa case devient dorée pour la manche), Evergreen (sa tuile survit aux récoltes : elle reste plantée et marque quand même les points de ligne).

## 5 tile upgrades de plus qui touchent la grille (deuxième lot)

Pruner (après le score, déracine les tuiles voisines d'une autre couleur : +8 pts chacune, et libère les cases), Glow (les cases vides voisines deviennent dorées pour la manche), Bridge (joint son groupe, en ligne droite, à une tuile de la même couleur par-dessus une case vide), Seedpod (quand sa tuile est récoltée, plante 2 tuiles gratuites de sa couleur autour de sa case), Locksmith (à chaque score, libère une case verrouillée, gelée ou empoisonnée n'importe où).

## 7 fertilizers qui lisent les tuiles améliorées (tous en Mult)

Charmed Patch (+1 Mult par tuile améliorée du groupe scoré, la copie d'un Wildroot compte), Grafter (+3 Mult si la graine plantée porte une upgrade), Twin Graft (+5 Mult si elle en porte 2 ou plus), Botanist (+1 Mult par type d'upgrade différent dans le jardin), Hothouse (+1 Mult par 2 tuiles améliorées dans le jardin), Good Neighbors (+2 Mult par tuile améliorée qui touche la graine plantée), Gleaner (+2 Mult par tuile améliorée récoltée par la ligne). Prix 5 à 7 Pollen.
