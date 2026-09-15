# Contigu

Prototype Unity/C# d'un puzzle de block-fitting façon 1010!/Block Blast, avec
un bonus de score façon "mot Scrabble" pour les groupes connectés de même
couleur, deck de pièces persistant, et structure de run roguelike-deckbuilder
(façon Balatro) avec draft d'améliorations entre les manches. Voir la
spécification complète dans la description du projet pour le détail des
règles.

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

## Décisions d'implémentation notables

- **Évaluation de fin de manche** : suit la spec 3.4 à la lettre — le
  succès/échec n'est évalué qu'une fois le budget de pièces épuisé, pas dès
  que le quota est atteint (le joueur peut continuer à scorer au-delà du
  quota tant qu'il lui reste des pièces à poser).
- **Bonus de groupe connecté ("mot Scrabble")** : à chaque pose, le groupe de
  cases connectées de même couleur (orthogonalement, joker inclus en pont
  transitif) que la pièce touche est entièrement recalculé — chaque case du
  groupe rapporte `GroupBonusPerCell` (1 pt), pas seulement les cases
  nouvellement posées. Poser un carré de 4 cases seul rapporte donc 4 pts ;
  y coller ensuite un autre bloc qui porte le groupe à 8 cases rapporte 8 pts
  *pour cette seconde pose* (le groupe entier est "rejoué", comme on
  rescore un mot entier au Scrabble en l'allongeant). Si le groupe contient une
  case teintée qui matche ou une zone multiplicatrice, le multiplicateur
  (cumulable ×4) s'applique à tout le groupe, pas juste à cette case — même
  logique que les cases bonus au Scrabble qui valorisent tout le mot. La case
  dorée reste un bonus fixe (+18) indépendant, calculé à part et simplement
  additionné (jamais multiplié par le groupe). Couvert par des tests.
  ⚠️ Les quotas des manches (300→2500) n'ont pas été retouchés depuis ce
  changement — ils étaient calibrés pour l'ancien système où une pose isolée
  ne rapportait rien ; à rebalancer après playtesting si les manches
  deviennent trop faciles.
- **Positionnement des cases dorées/teintées/multiplicatrices** : choisi
  aléatoirement parmi les cases libres au moment du pick (comme le prototype
  HTML de référence), plutôt que par sélection manuelle du joueur — point
  explicitement laissé ouvert par la spec (section 5.4) et tranché en faveur
  de la version simple pour rester dans le budget de ce prototype.
- **Nouvelle main** : une main de 3 n'est retirée que lorsque les 3 pièces
  précédentes ont été posées (spec 4.2, comportement du prototype HTML).
