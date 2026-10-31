# LASTBLOOM : document de conception (état actuel)

Ce document décrit le jeu tel qu'il est maintenant. Le détail des décisions et de leur historique est dans `DECISIONS.md`, les chiffres d'équilibrage dans `BALANCE.md`, la correspondance code / noms affichés dans `GLOSSARY.md`. Les noms ci-dessous sont les noms affichés.

## 1. Pitch

Un puzzle de pose de pièces (façon 1010! / Block Blast) construit comme un roguelike de deckbuilding (façon Balatro) : on pose des *seeds* (graines) sur un jardin, on récolte des lignes, on enchaîne les groupes de même couleur, et chaque pose inflige des dégâts à des nuisibles. Entre deux manches, on passe à la boutique pour améliorer sa build. Ton : jardin cartoon chaleureux, « The last garden still blooms, at the edge of ruin. »

Plateformes : iPad / tablette (cible principale, tactile), navigateur WebGL, desktop. L'interface est entièrement construite à l'exécution.

## 2. Boucle de jeu

1. **Run** : 15 manches (Act 1 : manches 1 à 8, Act 2 : 9 à 15 ; un boss toutes les 4 manches, et un boss final à la manche 15). Victoire après la manche 15 ; « Keep Growing » prolonge en mode Endless.
2. **Manche** : on dispose d'un nombre limité de graines (budget). On pose des graines de la main sur la grille. Chaque pose marque des points et inflige des dégâts à l'ennemi de devant. La manche est gagnée quand tous les ennemis sont vaincus, perdue quand le budget ou les coups possibles sont épuisés.
3. **Boutique** entre les manches (voir §6), puis manche suivante.
4. **Fin de run** : écran de victoire ou de défaite ; les Stars gagnées débloquent les autres challenges.

## 3. Plateau et pièces

- **Grille** 6×6 (5×5 en Greenhouse). Une case s'appelle une *plot* ; une tuile posée, un *sprout*.
- **Seeds** : 8 formes de 1 à 4 cases (pion, dominos, trominos, carré, trois tétrominos), rotation possible, de 4 couleurs : Poppy, Iris, Sunflower, Fern (2 seulement en Orchard). Le *Joker* prend la couleur utile et porte toujours exactement un trait de combat.
- **Seed Bag** (le deck) : 10 graines au minimum ; on en pioche une **main de 3** (4 avec un fertilizer). Un *Shuffle* limité renouvelle la main. Si aucune graine de la main ne peut être posée et plus de Shuffle, le run se termine.
- **Harvest** : une ligne ou colonne pleine est récoltée (les tuiles disparaissent). Plusieurs lignes d'une même pose valent de plus en plus (3, 6, 9 points par case). Vider toute la grille double le score de la pose.
- **Plots spéciales** : dorées (bonus fixe), zones de multiplicateur, teintées, verrouillées, gelées, empoisonnées (effets des ennemis et de la météo).
- **Tile upgrades (sprouts enchantés)** : une tuile d'une graine peut porter une amélioration (plus de 70 : bonus de points, de Mult, de Pollen, effets de voisinage, de ligne, de combat…). Une graine peut en porter une par tuile.

## 4. Score et dégâts

- Chaque pose produit un total de **points (chips) × Mult**. Les groupes de même couleur connectés rapportent un bonus par case ; les lignes récoltées, les plots dorées et les tile upgrades ajoutent des points ; les fertilizers ajoutent surtout du **Mult** (additif, ou multiplicatif ×2, ×3…). Les points se convertissent en Mult à raison de 12 points pour 1 Mult quand un effet est exprimé en Mult.
- Le score de la pose est infligé comme **dégâts** à l'ennemi de devant ; les surplus passent au suivant selon les effets. Les traits de combat (Spore Burst, Long Reach, Overflow, Pruning) modifient qui est touché.
- **Mastery** : chaque forme et chaque couleur a un niveau qui renforce les graines correspondantes ; les upgrades et packs de Mastery les font monter.

## 5. Ennemis, météo, challenges

- **Ennemis** (16) : Withered Stalk (base), Bramble, Blightmoth, Rotbloom, Crow, Creeping Root, Root Leech, Inkcap, … chacun avec un rôle (Baseline, Creator, Reactor, Boss) et un effet sur le plateau ou la main (verrous, poison, vol de Pollen, soin à chaque ligne, couleur ou forme détestée…). Les PV augmentent à partir du round 9.
- **Météo** (10 météos de base et 13 variantes thématiques qui jouent comme une base) : tirée par manche, jamais deux fois la même d'affilée, manche 1 toujours dégagée. Pluie et Soleil visent une couleur ; les autres changent les scores, le Pollen, la grille ou la main. Les challenges thématiques ont leurs variantes (neige, lampes de serre, chat…).
- **Challenges** : Spring (didacticiel, par défaut), Winter (sac réduit, budgets serrés), Summer (météo dure ×1,5, Locust Swarm), Autumn (les fertilizers fanent après 4 manches, plus de Pollen, Rotgrub), Orchard (2 couleurs seulement, Fruitworm), Greenhouse (grille 5×5, Whitefly, météo de serre). Chacun a un ennemi résident et une signature.
- **Stars** : +1 par manche réussie, +2 pour une victoire ; elles débloquent les challenges suivants.

## 6. Économie et boutique

- **Pollen** : la monnaie. Gagné par les groupes de couleur, certaines météos et fertilizers, la fin de manche en Autumn.
- **Fertilizers** (modificateurs, 145) : jusqu'à **4 actifs**, rangés dans le panneau Fertilizers ; chacun a un prix, une catégorie (Couleurs, Voisinage, Connexions, Formes, Destruction, Roguelike) et un niveau améliorable. On peut les vendre ou les réordonner.
- **La boutique** montre deux étagères :
  - **Blister** (3 emplacements) : fertilizers et tile upgrades à l'achat direct. Seul le Blister se re-tire contre du Pollen (Reroll).
  - **Workshop** (4 emplacements) : deux packs tirés parmi quatre, le Compost et la Forge.
    - *Seed Pack* : 3 graines améliorées, on en garde 1.
    - *Sprout Pack* : 5 tile upgrades au choix, puis 3 graines à enchanter parmi 8.
    - *Fertilizer Pack* : 3 fertilizers, on en garde 1 gratuitement.
    - *Mastery Pack* : 3 masteries, on en garde 1.
    - *Compost* : retire une graine du sac pour l'alléger (prix qui remonte dans la visite, remis à zéro à chaque manche).
    - *Forge* : 3 tile upgrades au choix, puis 3 graines à enchanter parmi 8.
- Chaque achat de la visite renchérit les autres de 50 %. Les objets du Workshop restent achetables mais plus chers à chaque fois. Les choix (packs, Forge, Compost) se font sur les mêmes étagères que la boutique, avec leurs animations (vol vers l'enclume, effritement du Compost, vol du fertilizer vers son emplacement, célébration d'une mastery).
- **Skip** : abandonner un achat en cours fait perdre le Pollen dépensé.

## 7. Méta et sauvegarde

- **Collection** : fertilizers, upgrades, ennemis, météos, graines découvertes ; une barre de progression.
- **Sauvegarde** : « Save & quit » écrit la run telle qu'elle était au début de la manche (ou à l'ouverture de la boutique) ; « Continue saved run » la reprend. Une nouvelle partie demande confirmation avant d'écraser la sauvegarde. Les anciens identifiants retirés (par exemple les Glow de couleur) sont convertis à la reprise.

## 8. Interface et sensations

- Colonne de gauche : score (points × Mult), panneau Fertilizers, Seed Bag, Menu, sur une planche de bois ; colonne de droite : la main, sur la même planche ; au centre la grille. En haut, la bande des ennemis et la météo.
- **Tactile** : un doigt, une pièce sélectionnée puis posée par glisser ; infobulles ouvertes par un appui (et refermées par un appui ailleurs) ; aucun effet de survol ; cibles d'au moins 56 unités.
- **Retours** : popups de score, animations de récolte, de déracinement, d'effritement, de vol des fertilizers ; sons ; mouvement réduit désactive les animations décoratives.
- **Vie au repos** : décor animé, créatures et particules propres à chaque challenge, scintillements sur le plateau, un reflet qui traverse le jardin après 8 secondes sans pose, Sprig (la mascotte) au menu principal et dans le tutoriel.
- **Menu principal** : Play, Collection, Settings, Exit (pas d'Exit en navigateur), logo animé. Le sélecteur de challenge affiche une carte par saison avec sa bannière colorée.

## 9. Outils et qualité

- Core testable sans Unity (`Tools/Headless/Tests`), simulateur d'équilibrage (`Tools/Headless/RunSim`), audits de contenu et de glyphes (`Tools/qa`), scénarios WebGL (`Tools/qa/web`).
- Rapports QA datés dans `docs/qa/`.
