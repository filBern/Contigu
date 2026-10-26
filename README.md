# Contigu

Prototype Unity/C# d'un puzzle de block-fitting façon 1010!/Block Blast, avec
un bonus de score façon "mot Scrabble" pour les groupes connectés de même
couleur, deck de pièces persistant, et structure de run roguelike-deckbuilder
(façon Balatro) avec draft d'améliorations entre les manches. Voir la
spécification complète dans la description du projet pour le détail des
règles.

## Vocabulaire du jeu (thème jardin)

Le jeu s'appelle **LASTBLOOM**. Les noms affichés ont été rebaptisés ; les
noms de code et le reste de ce document gardent l'ancien vocabulaire.

| Dans le code / ce document | Affiché dans le jeu |
|---|---|
| pièce (`Piece`) | Seed (graine) |
| tuile / case remplie (`Tile`) | Sprout (pousse) ; Plot (parcelle) pour une case de la grille |
| deck | Seed Bag |
| line clear | Harvest (récolte) |
| modificateur (`Modifier`) | Fertilizer |
| Lueur | Pollen |
| Coral / Teal / Violet / Lime | Poppy / Iris / Sunflower / Fern |
| Classic / Marathon / Chaos | Spring / Winter / Wildgrowth |
| Common / Uncommon / Rare | Wild / Cultivated / Heirloom |
| Bombe / Range / Éclat / Précision | Spore Burst / Long Reach / Overflow / Pruning |

Les visuels vectoriels et leurs générateurs sont dans `Design/` ; les PNG
utilisés par le jeu sont dans `Assets/Resources/` :

| Dossier `Design/` | Contenu | Destination dans `Assets/Resources/` |
|---|---|---|
| `Tiles/` | tuiles de couleur, vide, dorée, verrouillée (+ `generate_tiles.py`) | `Tiles/`, `Icons/LockedTile.png` |
| `Enemies/` | portraits des 10 ennemis (+ `generate_enemies.py`) | `Enemies/` |
| `Icons/` | fertilizers (sacs), upgrades (panneaux), traits de tuile, Pollen | `Icons/Fertilizers`, `Upgrades`, `Traits`, `Pollen.png` |
| `Title/` | logo LASTBLOOM | `Title/` |
| `Buttons/` | corps de boutons, boutons complets (texte + plantes), 10 variétés de plantes | `Buttons/Garden`, `Buttons/Full`, `UI/Plants` |

Les générateurs sont des scripts Python (rendu PNG via Chromium + ImageMagick) ;
les SVG et PNG déjà produits sont versionnés, Python n'est pas nécessaire pour jouer.

## Ouvrir le projet

1. Unity Hub → Add → sélectionner ce dossier (`Contigu/`).
2. Version d'éditeur recommandée : 2022.3 LTS (voir `ProjectSettings/ProjectVersion.txt`).
   Unity proposera d'ouvrir avec la version installée la plus proche si celle
   listée n'est pas disponible.
3. Ouvrir `Assets/Scenes/Main.unity` puis lancer le mode Play.

Toute l'UI (grille, main, HUD, draft, écrans de fin) est construite au runtime
depuis le seul composant `GameBootstrap` — aucun prefab à assigner, il suffit
que la scène contienne le GameObject `GameBootstrap`.

## Architecture

Le code est séparé en 3 assemblies (voir `Assets/Scripts/*/Contigu.*.asmdef`) :

- **`Contigu.Core`** — logique de jeu pure (pas de `MonoBehaviour`), testable
  unitairement : `GridManager` (grille 8×8, placement, bonus de groupe connecté,
  clears de ligne/colonne, modificateurs persistants), `DeckManager` (deck,
  pioche, main, upgrades de banque), `RunManager` (manches, quotas, budgets,
  victoire/défaite), `UpgradeSystem` (draft + application des améliorations).
- **`Contigu.Data`** — métadonnées d'affichage optionnelles et éditables sans
  recompiler (`PieceColorSO`, `PieceShapeSO` + leurs databases), avec un jeu
  de valeurs par défaut intégré (`VisualDefaults`) pour que le jeu tourne sans
  qu'aucun asset ne soit créé dans l'éditeur.
- **`Contigu.Presentation`** — couche `MonoBehaviour`/uGUI : `GameBootstrap`
  construit tout le Canvas au runtime et relie `GridView`, `HandView`,
  `HudView`, `DraftView`, `EndScreenView` et `FeedbackLayer` (popups de score)
  au `RunManager`.

Les tests EditMode (NUnit) sont dans `Assets/Scripts/Tests/EditMode/` et
couvrent le scoring de la grille, le deck (tirage sans remise, reshuffle,
plancher de 10, upgrades) et le système de draft/upgrades. Ils s'exécutent
depuis `Window > General > Test Runner > EditMode` dans l'éditeur.

## Tests, simulation et documentation

- **Tests rapides sans Unity** : `cd Tools/Headless/Tests && dotnet test` (les mêmes tests EditMode, contre Core compilé comme bibliothèque .NET).
- **Simulateur d'équilibrage** : `cd Tools/Headless/RunSim && dotnet run -- --runs 1000 --policy greedy` (voir `Tools/Headless/README.md`).
- **CI** : `.github/workflows/headless-ci.yml` (tests + simulateur, toutes les branches) et `unity-ci.yml` (compile tout le projet dans Unity, sur `main`/PR).
- `docs/BALANCE.md` : où vivent les chiffres d'équilibrage et comment les mesurer.
- `docs/GLOSSARY.md` : correspondance identifiants de code (français) / noms affichés (anglais).
- `docs/DECISIONS.md` : journal des décisions de conception.

