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

- **Évaluation de fin de manche** : écart volontaire par rapport à la spec
  3.4 initiale, sur demande explicite — la manche se termine dès que le
  quota est atteint (succès immédiat, peu importe le budget de pièces
  restant), pas seulement quand le budget est épuisé. L'échec reste évalué
  au budget épuisé sans avoir atteint le quota, **ou** dès que la main
  devient injouable (aucune des 3 pièces en main ne rentre nulle part sur la
  grille — `GridManager.HasAnyValidPlacement`), ce qui évite un
  soft-lock si le joueur se retrouve coincé avant d'épuiser son budget.
- **Bonus de groupe connecté ("mot Scrabble")** : à chaque pose, le groupe de
  cases connectées de même couleur que la pièce touche est entièrement
  recalculé — chaque case du groupe rapporte `GroupBonusPerCell` (1 pt), pas
  seulement les cases nouvellement posées. Poser un carré de 4 cases seul
  rapporte donc 4 pts ; y coller ensuite un autre bloc qui porte le groupe à
  8 cases rapporte 8 pts *pour cette seconde pose* (le groupe entier est
  "rejoué", comme on rescore un mot entier au Scrabble en l'allongeant).
  - **Joker** : rejoint le groupe de la couleur réelle à laquelle il touche,
    mais un groupe scoré ne contient jamais qu'**une seule** couleur réelle
    à la fois — un joker ne "ponte" jamais deux couleurs différentes en un
    seul groupe. **Bug corrigé** : une version antérieure laissait un joker
    faire transitivement le pont entre deux couleurs incompatibles (ex.
    vert-joker-bleu comptait comme un seul groupe de 3, alors que vert et
    bleu ne sont pas la même couleur) — signalé par le joueur, corrigé dans
    `GridManager.FindConnectedGroup` (la première couleur réelle rencontrée
    devient l'"ancre" du groupe ; toute autre couleur réelle qui la
    contredit, même atteinte via un joker, est exclue). Concrètement :
    vert puis joker collé au vert forment bien un groupe de 2 ; poser
    ensuite une 3e couleur collée à ce même joker ne rejoint PAS le vert —
    elle forme son propre groupe de 2 avec le joker (le joker "change de
    camp" selon la pose qui le touche).
    ⚠️ Effet de bord sur les modificateurs : un groupe scoré ne pouvant
    plus jamais mélanger deux couleurs réelles, **Prisme**, **Tricolore** et
    **Complémentaire** (qui comptaient les couleurs distinctes *dans le
    groupe*) ont été redéfinis pour compter les couleurs distinctes
    *touchant la pose* (elle-même + ses 4 voisins directs) à la place —
    sinon leur condition serait devenue impossible à atteindre. **Puriste**
    n'a pas eu besoin d'être changé, mais sa branche "groupe non-monochrome"
    (`return 0` dans `ApplyPuriste`) n'est plus atteignable en jeu normal
    depuis ce correctif — un groupe est maintenant *toujours* monochrome (à
    l'exception d'un groupe 100% joker, toujours vacuously monochrome), donc
    Puriste rapporte désormais son bonus à chaque fois qu'il est actif sur
    un groupe d'au moins 2 cases avec une couleur réelle. Laissé tel quel
    (le bonus reste réel et scalé sur la taille du groupe), mais signalé ici
    si un rééquilibrage est voulu plus tard.
  - **Cases teintées** : chaque case teintée dont la couleur matche, présente
    n'importe où dans le groupe, contribue son propre ×2 — deux cases
    teintées dans le même combo se combinent en ×4, trois en ×8, etc. Une
    zone multiplicatrice reste elle basée sur la présence (une seule case
    suffit pour son ×2, peu importe combien il y en a dans le groupe), et se
    multiplie avec le facteur teinté.
  - **Case dorée** : bonus fixe (+18) indépendant, calculé à part et
    simplement additionné (jamais multiplié par le groupe). Elle se
    redéclenche à chaque fois que son groupe est recompté (pas seulement à
    la pose initiale) — puisque tout le groupe est rejoué à chaque
    extension, la case dorée qui en fait partie l'est aussi.
  Couvert par des tests.
  ⚠️ Les quotas des manches (300→2500) n'ont pas été retouchés depuis ce
  changement — ils étaient calibrés pour l'ancien système où une pose isolée
  ne rapportait rien ; à rebalancer après playtesting si les manches
  deviennent trop faciles.
- **Draft de fin de manche : 1 choix parmi 3, pools Banque/Grille mélangés**
  (spec 5.2) — un pick de banque et un pick de grille sont garantis dans les
  3 options offertes (le 3e est tiré au hasard parmi le reste), le joueur en
  choisit un seul. Une version antérieure de ce prototype avait
  temporairement scindé ceci en deux picks indépendants (1 pièce + 1
  grille) ; revenu en arrière sur demande explicite pour laisser la place au
  système de modificateurs ci-dessous à la place.
- **Modificateurs persistants ("Joker" façon Balatro)** : extension du même
  écran de fin de manche, sur demande explicite — juste après avoir choisi
  son amélioration (pièce/grille), le joueur choisit en plus **1
  modificateur parmi 3**, tiré sans remise du catalogue (`ModifierCatalog`).
  Les modificateurs sont permanents pour le reste du run (jusqu'à
  **5 actifs simultanément** — `RunManager.MaxActiveModifiers`) ; en choisir
  un 6e force le joueur à en retirer un immédiatement avant que la manche
  suivante démarre (`RunManager.State` passe par
  `AwaitingModifierPick` puis, si besoin, `AwaitingModifierRemoval`). Ils
  sont affichés en permanence dans un panneau à gauche de l'écran
  (`ModifierPanelView`) et chaque bonus qu'ils déclenchent apparaît comme un
  `ScoreEvent` de type `Modifier` (popup violet) au même titre que les
  bonus de groupe/dorés/lignes. Chaque `ScoreEvent` de type `Modifier`
  porte aussi `TriggeringModifier` (quel modificateur l'a produit) —
  `GameBootstrap` s'en sert pour faire un petit effet (flash + pulse) sur
  le nom du modificateur concerné dans le panneau de gauche à chaque fois
  qu'il rapporte des points (`ModifierPanelView.Pulse`), pas seulement la
  première fois.
  - Catalogue livré (16 sur la quarantaine d'idées brainstormées, en deux
    passes) — choisis parce que calculables avec les données déjà
    disponibles dans `GridManager.PlacePiece` sans refonte plus profonde :
    **Prisme** / **Tricolore** / **Complémentaire** (couleurs du groupe),
    **Chaîne** / **Méga-chaîne** (taille de groupe), **Forteresse** /
    **Prisonnier** / **Carrefour** (voisinage 8 cases / 4 cases / 4 cases
    encerclée par ≥2 couleurs différentes de la sienne — les voisins qui
    partagent la couleur de la case elle-même ne comptent pas), **Îlot**
    (case isolée, l'inverse de Chaîne), **Couronne**
    (cases en bordure de grille), **Trou dans la grille** (cases adjacentes
    à une case verrouillée), **Architecte** (pièce 2x2), **Puriste** (groupe
    monochrome, adaptation au niveau du *groupe* plutôt que de la *ligne*
    pour éviter une refonte de `CheckAndClearLines`), **Collectionneur**
    (couleurs distinctes parmi les cases effacées), **Maçon** (pose sans
    aucune ligne/colonne complétée) et **Démolisseur** (bonus par ligne
    quand ≥2 lignes/colonnes se complètent en même temps).
    ⚠️ Quelques noms de la liste originale (Complémentaire, Maçon,
    Démolisseur) n'avaient qu'un nom + catégorie à implémenter, pas la règle
    détaillée d'origine — leur condition exacte est donc une interprétation
    de ce prototype, documentée directement dans `ModifierCatalog`.
    *Dernier espace* a été remplacé par **Carrefour** : tel que décrit
    ("remplir complètement la grille"), il est structurellement
    inatteignable — dès qu'une ligne/colonne se complète elle se vide
    aussitôt (`CheckAndClearLines`), donc la grille ne peut jamais être
    100% pleine en jeu normal.
  - Non livrés (backlog futur) : les modificateurs de ligne (Arc-en-ciel,
    Alternance, Symétrie, Palindrome, Gradient, Sans doublon, Bloc,
    Monochrome-ligne — nécessitent une refonte de `CheckAndClearLines` pour
    exposer la forme d'une ligne avant son clear), les modificateurs de
    voisinage restants (Cœur de pierre, Cercle chromatique, Diagonale
    verrouillée, Dernier espace), les modificateurs de destruction restants
    (Overkill, Cascade, Réaction en chaîne, Combo parfait, Nettoyage,
    Récolte — nécessitent de suivre un historique de poses/clears d'une
    manche à l'autre), et le reste des idées couleurs/roguelike (Monochrome,
    Contraste, Dégradé, Chaos coloré, Emmitouflée, Chromatique, Jardinier).
- **Positionnement des cases dorées/teintées/multiplicatrices** : choisi
  aléatoirement parmi les cases libres au moment du pick (comme le prototype
  HTML de référence), plutôt que par sélection manuelle du joueur — point
  explicitement laissé ouvert par la spec (section 5.4) et tranché en faveur
  de la version simple pour rester dans le budget de ce prototype.
- **Nombre de cases affectées par un upgrade de grille : 1 au lieu de
  3** (dorées et multiplicatrices) **/ 2** (teintées) — nerf explicite,
  3 cases dorées/multiplicatrices ou 2 teintées d'un coup était bien trop
  puissant vu que leur bonus se redéclenche à chaque repassage du groupe.
  `UpgradeSystem.GoldenCellsCount` / `TintedCellsCount` /
  `MultiplierZoneCount` valent maintenant tous 1.
- **Combo en cours affiché en gros entre la grille et la main** : pendant
  qu'une pose déroule sa cascade de popups (groupe, doré, modificateurs,
  clears de ligne), un indicateur "Combo: +N" en gros texte violet
  (`ComboView`, composant dédié plutôt qu'inséré dans `HudView`) additionne
  en direct tous les points de CETTE pose au fur et à mesure qu'ils
  s'affichent, séparément du score de manche — pour que le joueur voie
  clairement combien un seul coup vient de rapporter. D'abord placé dans
  la barre du HUD tout en haut, déplacé sur demande explicite (trop
  discret là-haut) dans l'espace sous la grille — la main a depuis
  déménagé à droite de la grille (voir plus bas), mais ce même espace
  sous la grille, maintenant entre elle et la barre "pieces restantes",
  reste l'endroit où l'œil du joueur est déjà pendant qu'il joue.
- **Nouvelle main** : une main de 3 n'est retirée que lorsque les 3 pièces
  précédentes ont été posées (spec 4.2, comportement du prototype HTML).
- **Rotation aléatoire des pièces** : sur demande explicite — chaque pièce
  qui arrive dans la main obtient une orientation aléatoire parmi les 4
  quarts de tour (`PieceRotation`, 0°/90°/180°/270°), tirée au moment de la
  pioche (`DeckManager.DrawNewHand` → `HandRotations`, en parallèle de
  `Hand`), pas figée sur le type de pièce dans le deck — la même pièce
  peut donc ressortir dans une orientation différente une prochaine fois.
  Le joueur ne peut pas faire pivoter une pièce lui-même, il joue
  l'orientation telle que distribuée. `PieceShapeCatalog.GetRotated`
  précalcule les 4 rotations de chacune des 10 formes (rotation pure,
  jamais un miroir — le S-tetromino ne devient jamais un Z) ; le slot de
  main (`HandView`), la prévisualisation au survol de la grille et la pose
  réelle (`RunManager.PlacePiece`) utilisent tous la même forme pivotée,
  donc ce qui est affiché correspond exactement à ce qui sera posé.
- **Interface entièrement en anglais** : sur demande explicite, tout le
  texte affiché au joueur (HUD, statuts, écrans de draft/modificateurs,
  écrans de fin, noms de pièces/couleurs dans `VisualDefaults`, noms et
  descriptions des upgrades/modificateurs dans `UpgradeCatalog`/
  `ModifierCatalog`) est en anglais. Les commentaires de code, eux, restent
  tels quels (majoritairement en français, comme le reste de ce document)
  puisqu'ils ne font pas partie du jeu tel que vu par le joueur. Le popup
  de score d'un clear de ligne/colonne a aussi perdu son mot indicatif
  ("+12 ligne" → "+12"), sur demande explicite.
- **Palette v1 (8 couleurs fournies)** : `#372e4d` `#5f699c` `#65aed6`
  `#a4ebcc` `#effae6` `#f0b38d` `#b56d7f` `#614363`, appliquée à
  `UITheme` (chrome) et `VisualDefaults` (pièces/grille). Avec 8 couleurs
  pour ~20 rôles UI, plusieurs rôles réutilisent délibérément le même hex
  — sans risque là où les deux rôles ne se superposent jamais directement
  (ex. le fond d'un bouton et une tuile de pièce). Deux décisions à noter :
  - `TextMuted` réutilise le hex de `TextPrimary` (`#effae6`) réduit à
    68% d'alpha plutôt qu'un hex distinct — un hex plat pour "muted"
    serait tombé exactement sur `PanelLight`/`ButtonIdle` (`#5f699c`),
    rendant le texte secondaire invisible sur une carte ou un bouton de
    cette couleur.
  - Les 4 couleurs de pièces prennent les 4 teintes les plus saturées de
    la palette (une chacune) ; Joker prend la teinte moyenne restante
    (`#5f699c`) pour un rendu délibérément plus sobre, cohérent avec son
    rôle de "wildcard" face aux 4 couleurs vives.
- **Icônes d'accessibilité daltonisme + textures de tuile** : petites
  icônes par couleur de pièce (feu de camp Coral, étoile Joker, feuille
  Lime, goutte Teal, fleur Violet) affichées au centre de chaque case
  remplie de la grille, en plus du remplissage de couleur, pour ne jamais
  dépendre de la couleur seule. `VisualDefaults.GetColorIcon` retourne
  `null` et `GridCellView` cache simplement le badge pour toute couleur
  sans icône (le mécanisme reste en place au cas où une future couleur
  arriverait sans sprite tout de suite).
  `gold-tile.png`/`DisabledTile.png` remplacent aussi le remplissage plat
  des cases dorées (badge coin) et verrouillées (texture X) quand la
  sprite est disponible, avec le même repli sur l'ancienne couleur plate
  sinon. Les fichiers vivent dans `Assets/Resources/Icons/` (et non
  `Assets/Sprites/`) car toute l'UI de ce projet est construite par code
  sans référence d'asset câblée dans l'éditeur — `Resources.Load<Sprite>`
  au runtime est donc le seul point d'accroche disponible.
- **Cartes/panneau de modificateurs : texte remplacé par un badge +
  tooltip au survol** : le nom/la description en texte plein sur les
  cartes de draft et le panneau latéral restaient peu lisibles même après
  contours + fonds redéfinis (voir plus haut) — `UITheme.Modifier`
  (`#b56d7f`) et `UITheme.PanelLight` (`#5f699c`) restent trop proches en
  luminosité pour être vraiment nets ensemble, contour ou pas. À la
  place : chaque modificateur montre un badge carré coloré par
  `ModifierCategory` (Couleurs/Voisinage/Connexions/Destruction/
  Roguelike, chacune une couleur de la palette v1) avec une abréviation à
  2 lettres (ex. `PR` pour Prism, `MC` pour Mega Chain — voir
  `ModifierVisualDefaults`), et le nom + la description complète
  n'apparaissent que dans un tooltip flottant (`TooltipView`, un seul
  instancié par `GameBootstrap` et partagé par toutes les cartes/lignes)
  au survol du badge (`ModifierBadgeView`, `IPointerEnterHandler`/
  `IPointerExitHandler`). Aucun art par-modificateur n'existe encore —
  `ModifierVisualDefaults` documente comment un futur sprite par icône
  s'insérerait au même endroit que l'abréviation, sur le même principe de
  repli que `VisualDefaults.GetColorIcon` (Coral) plus haut.
- **Texture de case remplie** (`fill-tile-piece.png`, dans
  `Assets/Resources/Tiles/`) : une 3e couche insérée entre le
  remplissage plat de couleur (`Background`) et le badge de couleur
  (`_badgeColorIcon`) — un cadre/bevel neutre (non teinté) qui donne à
  chaque case remplie un aspect "bloc" plutôt qu'un simple rectangle
  plat, quelle que soit la couleur de la pièce. Visible uniquement sur
  une case réellement remplie et non verrouillée
  (`VisualDefaults.FillTileSprite`, même repli que golden/locked tile si
  jamais le sprite venait à manquer). Le nouvel enfant `FillTile` est
  construit juste avant les badges dans `GridView.CreateCell` pour
  garder le bon ordre d'empilement (Background → FillTile → badges).
- **Fix : un même modificateur pouvait être obtenu deux fois dans la
  même run** — `RunManager.RollModifierDraftOptions` tirait ses 3
  options depuis `ModifierCatalog.All` en entier (16 entrées), sans
  exclure les modificateurs déjà dans `ActiveModifiers` ; rien
  n'empêchait donc de se refaire proposer, puis reprendre, un
  modificateur déjà actif. La méthode filtre maintenant le catalogue
  aux seuls modificateurs pas encore possédés avant de tirer les 3
  options — chaque modificateur ne peut plus être actif qu'une seule
  fois par run. Test de régression :
  `RollModifierDraftOptions_NeverOffersAModifierAlreadyActive`.
- **Fix : les slots de main restaient cliquables pendant l'animation de
  placement** — `OnCellClicked` bloquait déjà les clics sur la GRILLE
  pendant `_isPlayingPlacementSequence`, mais rien n'empêchait de
  sélectionner un AUTRE slot de main pendant ce temps. `HandView` a
  maintenant `SetInteractable(bool)` : désactive `Button.interactable`
  sur les 3 slots (bloque le clic) et grise leur fond en le mélangeant
  vers `UITheme.Background` (même traitement "recede into the void" que
  les cases verrouillées — voir `VisualDefaults.LockedColor`).
  `GameBootstrap` l'appelle avec `false` juste avant de lancer
  `PlayPlacementSequence` et avec `true` juste après qu'elle se termine.
- **Icône de couleur dans le preview du slot de main + le survol
  vert/rouge sur la grille** : sur demande explicite, le même badge
  d'accessibilité daltonisme utilisé sur les cases remplies apparaît
  maintenant à deux autres endroits où seule la couleur plate indiquait
  la couleur de la pièce :
  - `HandView.BuildShapePreview` superpose l'icône sur chaque carré
    rempli du preview de pièce dans le slot de main.
  - `GridView`/`GridCellView` : `SetSelectedShape` accepte maintenant
    la couleur de la pièce sélectionnée (`GameBootstrap` la passe
    depuis `PieceToken.Color`) et la propage jusqu'à
    `GridCellView.SetHoverTint`, qui affiche le badge d'icône par-dessus
    la teinte verte/rouge de survol — un aperçu exact de ce que
    `ApplyState` afficherait si la case était vraiment remplie.
    `ClearHover` (qui rappelle `ApplyState` sur chaque case relâchée)
    réinitialise l'icône automatiquement, sans code de nettoyage
    séparé. Comme pour les cases remplies, l'icône est simplement
    absente pour une couleur qui n'en a pas encore (Coral).
- **Retiré le nom de forme + couleur en texte des slots de main** :
  devenu redondant maintenant que le preview affiche déjà l'icône de
  couleur sur chaque case. Le conteneur de preview (`HandView`) est
  agrandi (90x68 → 100x110) et recentré sur tout le slot pour occuper
  l'espace libéré par le label retiré.
- **Marqueur rouge plus visible pour un placement invalide au survol** :
  la teinte rouge translucide seule sur le fond était trop discrète.
  `GridCellView` a maintenant un `_invalidMarker` dédié — un petit
  carré plein (`UITheme.Danger`, 20px, contour sombre) centré sur
  chaque case du survol dès que `GridManager.CanPlace` retourne faux,
  au lieu de l'aperçu de l'icône de couleur (qui suggérerait à tort que
  la pièce peut atterrir là). `SetHoverTint` prend maintenant un
  paramètre `isValid` explicite plutôt que de déduire la validité de la
  couleur de la teinte. Purement décoratif et jamais lié à l'état réel
  d'une case : `ApplyState` le cache systématiquement, donc
  `ClearHover` le réinitialise sans code de nettoyage séparé (même
  principe que l'icône de couleur en survol ci-dessus).
- **Réorganisation du HUD** (sur demande explicite) :
  - `HudView` remplace ses 5 textes (round, quota, score de manche,
    pièces restantes, score total) par 2 barres de progression, chacune
    une `Image` en `Type.Filled` (`FillMethod.Horizontal`) avec un
    texte centré par-dessus. La barre du haut (`UITheme.Success`,
    verte) affiche `score de manche / quota` — remplace l'ancien
    "Quota X / Y" texte. La barre du bas (`UITheme.ButtonSelected`,
    bleue — délibérément une couleur différente de celle du haut) est
    nouvelle : `remainingPieces / budget` avec le texte "Remaining
    pieces: N", ancrée au bord inférieur de l'écran. Le round actuel et
    le score total du run ont été retirés de l'affichage permanent
    (spec demandée : ne garder que ce qui concerne la manche en cours).
    `HudView.SetScores` a perdu son paramètre `totalScore` en
    conséquence (et tout le suivi `displayedTotalScore` /
    `totalScoreBefore` dans `GameBootstrap`, devenu mort).
  - Les 3 slots de main (`HandView`) passent d'un `HorizontalLayoutGroup`
    à un `VerticalLayoutGroup` — empilés à la verticale à droite de la
    grille (calé sur son centre vertical) plutôt qu'en rangée sous elle.
  - `ComboView` reste sous la grille (maintenant qu'il n'y a plus de
    main en dessous) mais recalé pour tenir dans l'espace entre le bas
    de la grille et la nouvelle barre "pieces restantes".
  - `ModifierPanelView` (panneau de gauche) : `PanelWidth` 190→230
    (plus large), hauteur du panneau 640→460 (moins long — 5 lignes de
    modificateurs max ne remplissaient qu'une fraction des 640 d'avant),
    décalage horizontal 10→40 (plus vers la droite). Toute cette
    repositionnement se fait dans `GameBootstrap.BuildUI`, avec les
    calculs en commentaire pour que les futurs ajustements de mise en
    page restent faciles à suivre.
- **Itération sur le HUD** (sur demande explicite) :
  - Les 2 barres de progression prennent maintenant toute la largeur de
    l'écran et sont collées à leur bord (haut/bas) sans marge ni
    contour — `HudView.BuildBar` ancre chaque barre en `(0, edgeY)` à
    `(1, edgeY)` (stretch horizontal complet) avec `anchoredPosition`
    à zéro (aucun décalage par rapport au bord), et le contour noir
    (`Outline`) qui cerclait le fond a été retiré ; le remplissage
    (`Fill`) colle aussi maintenant exactement au bord du fond (plus
    d'insertion de 3px) pour une barre bien continue.
  - La grille est repassée au centre exact de l'écran (ancrée/pivot
    `(0.5, 0.5)`, `anchoredPosition = Vector2.zero`) au lieu d'être
    calée en haut — elle paraissait décentrée vers le haut une fois les
    barres du HUD retirées du flux normal (elles flottent par-dessus
    plutôt que de pousser le contenu). La position de la main (calée
    sur le centre vertical de la grille) et du combo (entre le bas de
    la grille et la barre du bas) ont été recalculées en conséquence.
  - Barres doublées d'épaisseur (`BarHeight` 34→68) et le contour noir
    sur le texte des barres retiré (sur demande explicite). Le texte du
    statut (au-dessus de la grille) et la position du combo ont été
    redécalés pour tenir compte de la barre du haut/bas maintenant deux
    fois plus épaisse.
  - **Fix : la barre ne fonctionnait pas visuellement** — le fond de la
    barre (`UITheme.Panel`, `#614363`) était trop proche en luminosité
    du fond de l'écran (`UITheme.Background`, `#372e4d`), et sans
    contour (retiré juste avant) la portion non remplie se fondait dans
    l'écran : la barre ne se lisait pas comme un conteneur avec un
    remplissage, juste comme une tache de couleur. Le fond passe à
    `UITheme.PanelLight` (`#5f699c`), nettement plus clair, pour un
    contraste net avec l'écran sans avoir besoin de contour. Le texte
    double aussi de taille (16→32), et la barre du bas affiche
    maintenant `piècesRestantes / piècesInitiales` (même format
    "X / Y" que la barre du haut) plutôt que la phrase "Remaining
    pieces: N" — `piècesInitiales` est `RunManager.CurrentBudget`, déjà
    disponible en paramètre de `HudView.UpdatePieces`.
  - **Fix : la barre ne représentait toujours pas vraiment le ratio**
    (retour explicite après le fix précédent) — `Image.Type.Filled`
    (fillAmount/fillMethod/fillOrigin sur un `Image` sans sprite)
    dépend de détails de rendu (mesh/shader) impossibles à vérifier
    visuellement dans ce bac à sable sans éditeur Unity. Remplacé par
    une approche 100% mise en page : le rectangle `Fill` est un `Image`
    tout simple (`Type.Simple`) dont le bord droit est piloté
    directement par `anchorMax.x` (`HudView.SetRatio`), qui redimensionne
    le rectangle proportionnellement à la largeur du parent — un pur
    calcul d'ancrage RectTransform, garanti de fonctionner
    indépendamment du rendu du shader de remplissage.
- **Fix : l'écran "Too many modifiers!" débordait de l'écran** —
  `ModifierDraftView._cardsContainer` utilisait un
  `HorizontalLayoutGroup`, mettant TOUTES les cartes sur une seule
  rangée. Ça passait pour l'écran de draft (toujours 3 cartes, 640px),
  mais l'écran de retrait peut afficher jusqu'à
  `MaxActiveModifiers + 1 = 6` cartes, soit 1300px sur une seule
  rangée — bien plus large que l'écran. Remplacé par un
  `GridLayoutGroup` à 2 colonnes fixes (`FixedColumnCount`), qui donne
  un bloc 2×2 pour le draft et 2×3 pour le retrait (demandé
  explicitement) au lieu d'une rangée qui déborde. Comme
  `GridLayoutGroup` pilote directement la taille de chaque carte via
  son propre `cellSize`, le `LayoutElement`/`sizeDelta` que chaque
  carte devait fixer elle-même pour un `HorizontalLayoutGroup` est
  devenu inutile et a été retiré.
- **Refonte des upgrades de tuile (Golden/Tinted/Multiplier) : de la
  case de grille à la pièce enchantée** (demande explicite — l'ancien
  système "tague une case vide aléatoire de la grille pour toujours"
  "feelait nul") :
  - Nouveau modèle : l'upgrade tague maintenant une case précise à
    l'intérieur d'une pièce du deck plutôt qu'une case fixe de la
    grille — dans l'esprit de "mets une tuile dorée sur une tuile
    aléatoire, pour 3 tétrominos". L'effet se déclenche une seule fois,
    au moment où cette pièce précise est posée.
  - `PieceTrait` (nouveau, `Core/Pieces/PieceTrait.cs`) : struct
    `{ PieceTraitKind Kind, int LocalCellIndex, PieceColor? TintedColor }`.
    `LocalCellIndex` indexe dans la liste `Cells` de la forme de BASE
    (`Deg0`) de la pièce — comme `PieceShapeCatalog.GetRotated` applique
    la rotation case par case en conservant l'ordre (`rotated[i]`
    correspond toujours à `baseCells[i]`), ce même index reste valide
    quelle que soit la rotation réellement distribuée à la pièce.
  - `PieceToken` gagne un champ optionnel `Trait` (+ `WithTrait(...)`) ;
    comme les tokens sont des `readonly struct` copiés par valeur, un
    trait posé sur une entrée du deck (`DeckManager._deck`, la source
    de vérité) se propage naturellement à la main/pioche seulement au
    prochain reshuffle — c'est voulu, cohérent avec "permanent pour
    toute la run" sans bookkeeping supplémentaire.
  - `DeckManager.TagGoldenTokensRandom` / `TagTintedTokensRandom` /
    `TagMultiplierTokensRandom` : taguent N tokens distincts du deck
    (en préférant les tokens pas encore tagués, avec repli sur
    n'importe quel token si pas assez de candidats libres), chacun sur
    une case locale aléatoire de sa propre forme.
    `UpgradeSystem.Apply` appelle maintenant ces méthodes au lieu des
    anciennes `GridManager.ApplyGoldenCellsRandom` / `ApplyTintedCellsRandom`
    / `ApplyMultiplierCellsRandom` (retirées, devenues mortes — avec
    leur méthode privée `PickRandomUnmodifiedCells`). Comme plus aucun
    cas de `UpgradeSystem.Apply` n'a besoin de `GridManager`, son
    paramètre `grid` (devenu inutile) a été retiré de la signature.
  - `RunManager.PlacePiece` traduit le trait de la pièce posée en un
    flag `Cell.IsGolden` / `IsTinted` / `IsMultiplierZone` juste avant
    d'appeler `Grid.PlacePiece` (réutilise tout le moteur de score
    existant), puis le retire aussitôt après — contrairement à l'ancien
    système, le flag sur la case de grille est maintenant transitoire
    (un seul déclenchement, au placement), pas un modificateur
    permanent de la grille.
  - Ajout d'un badge visuel sur la tuile enchantée : dans l'aperçu de
    la main (`HandView.BuildShapePreview`) et dans le survol
    vert/rouge de la grille (`GridView`/`GridCellView.SetHoverTint`),
    la case correspondant à `LocalCellIndex` affiche le même badge
    (sprite doré / couleur teintée / contour multiplicateur) qu'une
    case réellement dorée/teintée/multiplicatrice sur la grille.
  - Textes des 3 upgrades (`UpgradeCatalog`) mis à jour pour refléter
    le nouveau fonctionnement ("Enchants 1 random piece in the deck...").
  - Tests : nouveaux tests dans `DeckManagerTests` (tag exact count,
    index de case valide, préférence pour les tokens non tagués, repli
    quand il n'y en a plus assez) et `RunManagerTests` (le trait produit
    bien le bonus de score attendu sur ce placement, puis la case de
    grille redevient non-modifiée juste après) ; tests réécrits dans
    `UpgradeSystemTests` (vérifient le tag sur le deck, plus la case de
    grille) et `GridManagerTests` (méthode retirée).
  - **Ajustement (sur demande explicite)** : chaque upgrade enchante
    maintenant 3 pièces distinctes du deck au lieu d'une seule
    (`UpgradeSystem.GoldenCellsCount`/`TintedCellsCount`/`MultiplierZoneCount`
    passés de 1 à 3) — l'ancien 1 datait du réglage d'équilibrage de
    l'ancien système "case de grille permanente" et ne s'appliquait
    plus vraiment à la nouvelle sémantique "enchantement à usage
    unique par pièce".
- **4 nouveaux upgrades de pièce enchantée** (`BlastTile`/`MultiplierBeacon`/
  `MirrorTile`/`Seeder`, sur demande explicite — brainstorm initial fourni
  dans la conversation) + **tooltip au survol du badge d'enchantement** :
  - `PieceTraitKind` gagne 4 nouvelles valeurs : `Blast`, `Beacon`,
    `Mirror`, `Seeder`.
  - **Blast Tile** — au placement, la case enchantée ET ses 4 voisines
    orthogonales marquent `IsGolden` (jusqu'à 5 cases dorées d'un coup
    si les voisines sont déjà remplies et rejoignent le même groupe).
    Contrairement à Beacon (voir plus bas), le marquage des voisines ne
    vérifie PAS `IsFilled` au préalable — une voisine vide reste
    inoffensive puisqu'une case vide ne peut jamais entrer dans
    `groupCells`.
  - **Multiplier Beacon** — au placement, la case enchantée ET toutes
    les cases DÉJÀ remplies de sa ligne/colonne marquent
    `IsMultiplierZone`. Ça n'avait aucun effet utile tant que
    `GridManager.ComputeGroupMultiplier` ne comptait le facteur
    multiplicateur qu'une seule fois par groupe (peu importe le nombre
    de cases marquées) — comportement changé pour empiler chaque case
    marquée (x2, x4, x8...), exactement comme Tinted le fait déjà.
    Ça change aussi (légèrement) l'upgrade de base "Multiplier Zone" :
    si 2 pièces taguées Multiplier finissent par atterrir dans le même
    groupe scoré, leurs x2 s'empilent maintenant au lieu de plafonner à
    un seul x2 — cohérent avec Tinted, mais un changement d'équilibrage
    à noter.
  - **Mirror Tile** — le bonus de groupe de la case enchantée est
    dupliqué sur la case symétriquement opposée dans le groupe scoré
    (réflexion à travers le centre de la bounding box du groupe), si
    elle existe. Ne pose AUCUN flag de case avant le placement — calculé
    APRÈS `Grid.PlacePiece` (dans `RunManager.ApplyMirrorBonus`) à
    partir des `ScoreEvent` de type `Group` déjà renvoyés (toutes les
    cases d'un même groupe reçoivent le même montant par case, donc
    n'importe quel événement `Group` donne directement le montant à
    dupliquer). Nouveau champ `PlacementResult.TraitBonus` (inclus dans
    `TotalScore`) + nouveau `ScoreEventType.Trait` pour que ce bonus
    apparaisse comme son propre popup dans la séquence de feedback.
  - **Seeder** — comme Golden, mais le flag `IsGolden` n'est PAS retiré
    après le score : la case reste dorée en permanence sur la grille
    pour le reste de la run (seule trait qui n'entre pas dans la liste
    `transientCells` nettoyée après coup par `RunManager`).
  - `RunManager.ApplyTokenTrait` retravaillé pour renvoyer une LISTE de
    cases transitoires (au lieu d'une seule) — Blast/Beacon touchent
    plusieurs cases, Seeder n'en nettoie aucune, Mirror n'en pose aucune.
  - `PieceTraitVisualDefaults` (nouveau, `Data/`) : nom + description +
    couleur de badge par `PieceTraitKind`, même esprit que
    `ModifierVisualDefaults` (pas d'art dédié pour l'instant — juste un
    chip coloré + tooltip). Golden/Blast/Seeder partagent la couleur
    dorée existante (ils marquent tous `IsGolden`), Multiplier/Beacon
    partagent le bleu multiplicateur — le badge seul ne les distingue
    plus, d'où le tooltip.
  - **Tooltip au survol** (demande explicite) : `TraitBadgeView` (nouveau,
    même patron que `ModifierBadgeView`) affiche le `TooltipView` déjà
    partagé au survol d'un badge d'enchantement dans un slot de main,
    avec le nom + l'effet complet du trait. Comme le badge est un petit
    carré niché dans un slot qui est lui-même un `Button` (sélection de
    la pièce), `TraitBadgeView` intercepte aussi le clic et le relaie
    manuellement (`ExecuteEvents.Execute(...pointerClickHandler)`) vers
    le `Button` du slot — sinon cliquer précisément sur le badge
    n'aurait plus sélectionné la pièce. Scope volontairement limité au
    badge de la main (une pièce y est stable, "survolable" ; le badge
    de preview au survol de la grille est trop transitoire pour un
    second niveau de survol imbriqué).
  - `UpgradeCatalog`/`UpgradeId` gagnent 4 entrées (`BlastTile`,
    `MultiplierBeacon`, `MirrorTile`, `Seeder`), toutes dans le pool
    Grid, `RequiresSubChoice = false` (même chemin de draft simple que
    les 3 upgrades existants).
  - Tests : nouveaux tests d'intégration dans `RunManagerTests` pour
    chacun des 4 nouveaux traits (dont un test de stacking pour Beacon
    et un test vérifiant que Seeder ne nettoie pas son flag), nouveau
    test de stacking multiplicatif dans `GridManagerTests`
    (`PlacePiece_TwoMultiplierZoneCellsInSameGroup_CombineMultiplicatively`),
    plus les tests `Apply_*`/`Tag*` habituels dans `UpgradeSystemTests`/
    `DeckManagerTests`.
- **Raccourci debug éditeur-only pour tester les upgrades plus vite**
  (sur demande explicite) : touche **F9** force la fin instantanée de la
  manche en cours (`RunManager.DebugForceRoundComplete` — met
  `RoundScore` à `CurrentQuota` puis relance l'évaluation normale de fin
  de manche), déclenchant le draft d'upgrade tout de suite au lieu de
  devoir vraiment jouer une manche complète. Le raccourci clavier
  (`GameBootstrap.Update`) est entièrement dans un bloc `#if
  UNITY_EDITOR` — absent des builds réels. `DebugForceRoundComplete`
  lui-même reste en C# pur (pas de dépendance UnityEditor) donc reste
  disponible aux tests ; ne fait rien si la run n'est pas `InProgress`
  (évite de perturber un draft déjà en cours si on spam la touche).
- **Le picker de type de pièce (Retirer/Dupliquer/Recolorer) montre
  maintenant un aperçu visuel de la forme au lieu du texte "Forme /
  Couleur"** (sur demande explicite, plus clair) :
  - Nouveau `ShapePreviewFactory` (Presentation) : factorise le rendu
    grille-de-carrés + icône couleur + badge d'enchantement partagé
    par `HandView` (main du joueur, forme tournée) et `DraftView`
    (ligne de la liste de types, forme de base non tournée).
    `HandView.BuildShapePreview` délègue maintenant entièrement à ce
    factory (code dupliqué retiré).
  - Chaque ligne du picker (`DraftView.BuildTypeRow`) montre l'aperçu
    de la forme/couleur + un badge d'enchantement si AU MOINS une
    copie de ce type dans le deck est enchantée
    (`DraftView.FindRepresentativeTrait` — les upgrades Retirer/
    Dupliquer/Recolorer opèrent sur un TYPE, pas un token précis, donc
    on ne peut montrer qu'un échantillon représentatif, pas garantir
    quelle copie exacte serait touchée) + le compte "xN" à droite.
  - Le badge d'enchantement se redimensionne maintenant proportionnellement
    à la taille de la case plutôt qu'une taille fixe de 14px — sur
    l'aperçu compact du picker (case ~15px), un badge fixe de 14px
    aurait quasiment recouvert toute la case.
  - Même tooltip au survol que dans la main (réutilise `TraitBadgeView`),
    avec le même relais de clic vers le bouton de la ligne.
- **Fix d'équilibrage : la tuile dorée de Seeder redevient normale à la
  fin de la manche** (sur demande explicite — permanente pour toute la
  run était jugée beaucoup trop forte). `Cell.ResetForNewRound()`
  efface maintenant `IsGolden`/`IsTinted`/`IsMultiplierZone` en plus du
  remplissage/verrouillage (avant, ces 3 flags n'étaient jamais touchés
  par un reset de manche — un reliquat de l'ancien système où
  Golden/Tinted/Multiplier taguaient une case de grille en
  permanence). Puisque Seeder est la SEULE source d'un flag qui
  survit à son propre placement (tous les autres traits se nettoient
  dans le même placement), ce changement revient exactement à "Seeder
  dure jusqu'à la fin de la manche, pas toute la run", sans toucher au
  reste du système.
  - Tests : réécriture de `GridManagerTests.ResetForNewRound_...` pour
    vérifier que les 3 flags sont bien effacés (au lieu de l'inverse) ;
    nouveau test `RunManagerTests.PlacePiece_SeederTrait_ClearsOnceTheRoundItWasSetInEnds`
    (utilise le nouveau raccourci `DebugForceRoundComplete` pour sauter
    directement à la manche suivante) ; les 2 tests qui simulaient
    plusieurs manches d'affilée en dorant toute la grille UNE SEULE
    FOIS avant leur boucle (`ApplyModifierPick_RequiresRemoval_...`,
    `RollModifierDraftOptions_NeverOffersAModifierAlreadyActive`) ont
    dû être ajustés — `PlayRoundToAwaitingDraft` redore maintenant
    toute la grille à CHAQUE appel plutôt qu'une fois par l'appelant,
    sinon les manches 2+ n'auraient plus eu de bonus doré du tout.
- **Fix : la main se réinitialisait à 3 pièces aléatoires en changeant de
  manche** (bug rapporté en jeu normal, pas via F9). Racine du problème :
  `RunManager.PlacePiece` appelait `DeckManager.PlayFromHand` (qui
  redessine automatiquement dès que la main tombe à 0) AVANT d'évaluer
  si la manche se terminait. Quand le placement qui vidait la main
  était AUSSI celui qui faisait atteindre le quota, la main était
  redessinée immédiatement — avant même que l'écran de draft
  n'apparaisse — donnant l'impression que "chaque nouvelle manche
  démarre avec 3 pièces fraîches" alors que ce tirage appartenait en
  réalité à la fin de l'ancienne manche.
  - `DeckManager.PlayFromHand` gagne un paramètre optionnel
    `refillIfEmpty = true` (défaut inchangé, donc rétro-compatible avec
    tous les appels existants/tests) : `refillIfEmpty: false` retire la
    pièce sans redessiner, laissant l'appelant décider quand tirer.
  - `RunManager.PlacePiece` appelle maintenant `PlayFromHand(handIndex,
    refillIfEmpty: false)`, évalue la fin de manche, PUIS ne redessine
    immédiatement que si la manche continue (`State == InProgress`) —
    sinon le tirage est différé.
  - `RunManager.StartRound()` gagne un filet de sécurité (`if
    (Deck.Hand.Count == 0) Deck.DrawNewHand();`) qui effectue ce tirage
    différé exactement au moment où la nouvelle manche démarre pour de
    vrai — pas avant.
  - Effet de bord positif : `HasAnyHandPlacement()` (détection de
    plateau bloqué) évalue maintenant la vraie main restante au moment
    du placement au lieu d'une main déjà re-tirée par erreur.
  - Tests : nouveau `DeckManagerTests.PlayFromHand_WithRefillIfEmptyFalse_LeavesHandEmptyInstead`
    et nouveau `RunManagerTests.PlacePiece_DefersHandRefill_WhenTheEmptyingPlacementAlsoEndsTheRound`
    (scénario construit : 2 pièces jouées normalement, puis la grille
    est remplie/dorée autour d'une poche 3×3 pour garantir qu'un seul
    placement de la 3e pièce vide la main ET dépasse le quota de la
    manche 1 — vérifie que la main reste à 0 jusqu'à l'avancement réel
    de manche).
- **Fix : régression — défaite immédiate après avoir placé 3 pièces**
  (introduite par le fix précédent). `EvaluateRoundEnd`'s détection de
  plateau bloqué (`HasAnyHandPlacement`) lisait `Deck.Hand` juste après
  `PlayFromHand` — auparavant toujours repeuplée à 3 avant cet appel,
  mais depuis le fix du tirage différé, la main reste maintenant à 0
  pièce exactement le temps de cet appel quand la manche continue.
  `HasAnyHandPlacement` sur une liste vide retourne `false`
  ("aucune pièce ne peut être placée" par absence de pièces à
  vérifier), ce que `!HasAnyHandPlacement()` interprétait à tort comme
  "plateau bloqué" → défaite immédiate à chaque 3e pièce jouée. Fix :
  `EvaluateRoundEnd` ignore maintenant complètement ce test quand la
  main est vide (`Deck.Hand.Count > 0 && !HasAnyHandPlacement()`) — une
  main vide n'a simplement rien à évaluer, elle n'est jamais "bloquée"
  par définition ; le tirage qui suit juste après redonne une vraie
  main que le PROCHAIN placement vérifiera normalement. Nouveau test
  `RunManagerTests.PlacePiece_DoesNotTriggerDefeat_WhenTheHandMerelyEmptiesMidRound`.
- **16 modificateurs supplémentaires** (deuxième lot, sur demande explicite —
  le catalogue passe de 16 à 32 entrées, `ModifierCatalog.All`). Piochés dans
  la même grande liste de brainstorm que le premier lot (voir plus haut) ;
  comme pour Complémentaire/Maçon/Démolisseur déjà notés, la plupart de ces
  16 noms n'avaient qu'un nom + catégorie à partir desquels travailler, pas la
  règle détaillée d'origine — leur condition exacte ci-dessous est
  l'interprétation de ce prototype, documentée directement dans
  `ModifierCatalog` et `ScoringConstants`.
  - **8 modificateurs de voisinage/couleurs/roguelike calculables sans
    refonte** (évalués en pré-clear comme le premier lot) : **Cœur de
    Pierre** (bonus par case du groupe totalement encerclée — chacun de ses
    8 voisins est rempli, verrouillé, OU hors grille — une version de
    Forteresse qui n'exclut plus les coins/bords), **Cercle Chromatique**
    (bonus par case dont les 4 voisins cardinaux, tous remplis, montrent
    ensemble les 4 couleurs de base), **Diagonale Verrouillée** (version
    diagonale de Trou dans la Grille — case adjacente en diagonale à une
    case verrouillée), **Monochrome** (variante plus stricte de Puriste :
    bonus PAR CASE au lieu d'un pourcentage, mais exige ZÉRO joker dans le
    groupe au lieu de simplement les ignorer), **Contraste** (bonus par
    case posée ayant au moins un voisin orthogonal rempli d'une couleur
    différente), **Dégradé** ("Momentum" en anglais pour éviter la
    confusion avec le Gradient de ligne ci-dessous — bonus quand le groupe
    scoré par CE placement est strictement plus grand que celui du
    placement précédent, dans la même manche ; nécessite un petit état
    supplémentaire, `GridManager._lastGroupSize`, remis à `null` par
    `ResetForNewRound`), **Emmitouflée** (bonus par case dont les 4 voisins
    DIAGONAUX — pas cardinaux — sont tous remplis) et **Jardinier** (bonus
    par case du groupe adjacente orthogonalement à une case dorée/teintée/
    multiplicatrice — fonctionne aussi bien avec l'enchantement d'une pièce
    tout juste posée qu'avec une case Semeur restée dorée depuis plus tôt
    dans la manche).
  - **8 modificateurs de ligne, le "backlog futur" du premier lot,
    maintenant livrés** : nécessitaient d'exposer la séquence de couleurs
    ordonnée de chaque ligne/colonne complétée AVANT qu'elle ne soit
    effacée — `GridManager.CheckAndClearLines` retourne maintenant aussi
    `ClearInfo.ClearedLines` (une entrée par ligne/colonne complétée par CE
    placement, avec sa séquence de couleurs pré-clear, cases verrouillées
    exclues), évalués en post-clear (`ApplyPostClearModifiers`, comme
    Collectionneur/Maçon/Démolisseur). **Arc-en-ciel** (ligne contenant les
    4 couleurs de base), **Alternance** (exactement 2 couleurs qui
    alternent strictement sur toute la ligne — un joker casse le motif),
    **Symétrie** (seul modificateur qui compare DEUX lignes différentes
    entre elles plutôt qu'une ligne à elle-même : bonus si la ligne
    miroir — rangée y ↔ rangée 7-y, colonne x ↔ colonne 7-x — se complète
    AUSSI dans ce même placement avec un motif de couleurs identique),
    **Palindrome** (la ligne se lit pareil dans les deux sens), **Gradient**
    (aucune paire de cases adjacentes ne partage sa couleur — condition plus
    large que Alternance, qui plafonne en plus à exactement 2 couleurs),
    **Sans Doublon** (chaque couleur apparaît au plus une fois — avec
    seulement 5 valeurs de couleur possibles, dont Joker, une ligne pleine
    de 8 cases ne peut jamais qualifier ; n'est atteignable que quand des
    cases verrouillées (manche boss) réduisent la ligne à moins de 6 cases
    réelles) et **Bloc** (la ligne n'est faite que de blocs contigus d'au
    moins 2 cases de la même couleur — aucune case isolée).
  - Toujours pas livrés : les modificateurs de destruction restants
    (Overkill, Cascade, Réaction en chaîne, Combo parfait, Nettoyage,
    Récolte — nécessiteraient de suivre un historique de poses/clears
    d'une manche à l'autre, refonte plus profonde que celle faite ici) et
    Dernier Espace (toujours structurellement inatteignable, voir plus
    haut).
  - Nouveaux badges 2 lettres (`ModifierVisualDefaults.Abbreviations`) :
    CP/CC/DV/MO/CN/DG/EM/JA/AC/AL/SY/PA/GR/SD/BL/ML — vérifiés uniques par
    rapport aux 16 du premier lot.
  - Tests : `GridManagerModifierTests` gagne 2 tests (fire / ne fire pas)
    par modificateur (sauf quelques cas combinés dans un seul test, comme
    pour le premier lot), y compris un test dédié pour la remise à zéro de
    `_lastGroupSize` de Dégradé à `ResetForNewRound`, et un test Symétrie
    qui complète 2 rangées miroir en un seul placement (domino vertical) via
    `ShapeId.DomV`.
- **7 upgrades de tuiles supplémentaires + système de rareté** (sur demande
  explicite — brainstorm fourni par l'utilisateur, 7 idées retenues sur les
  7 proposées). `PieceTraitKind` passe de 7 à 14 valeurs (2 lots), tout comme
  `UpgradeId`/`UpgradeCatalog.GridPool`.
  - **Catalyseur** (`Catalyst`) — bonus par case déjà présente sur la grille
    avant ce placement, fusionnée dans le groupe scoré (taille du groupe
    moins le nombre de cases propres à la pièce) — récompense de jouer DANS
    une structure existante plutôt qu'isolément.
  - **Foreuse** (`Driller`) — gros bonus plat, mais uniquement si la case
    enchantée est adjacente (orthogonalement) à une case verrouillée (manche
    boss) ; rien sinon.
  - **Jumelle** (`Twin`) — comme Miroir, mais duplique la part de bonus de
    groupe de la case enchantée sur TOUTES les autres cases du groupe scoré,
    pas seulement sur une case symétrique — scale sans limite avec la taille
    du groupe (d'où sa rareté "Rare").
  - **Détonateur** (`Detonator`) — si ce placement complète au moins une
    ligne/colonne, double le bonus de clear de ligne ENTIER de ce placement
    (pas seulement celui d'une case précise).
  - **Caméléon** (`Chameleon`) — si la case enchantée a un voisin
    orthogonal déjà rempli, la pièce ENTIÈRE se recolore pour matcher cette
    couleur avant le scoring (au lieu de garder sa propre couleur), fusionnant
    ainsi dans un groupe existant. Contrairement aux autres traits, ce n'est
    pas un bonus de score mais une mutation de la couleur de placement —
    résolue par `RunManager` AVANT d'appeler `GridManager.PlacePiece` (scan
    fixe gauche/droite/bas/haut du premier voisin rempli, repli sur la
    couleur propre de la pièce si aucun voisin n'est rempli).
  - **Étincelle** (`Spark`) — bonus croissant avec le nombre de poses
    consécutives sans clear de ligne/colonne cette manche (remis à zéro dès
    qu'un clear survient). Nécessite un nouveau compteur round-scoped,
    `GridManager.PlacementsSinceLastClear` (mis à jour en fin de
    `PlacePiece`, remis à zéro par `ResetForNewRound`) — lu par `RunManager`
    AVANT d'appeler `Grid.PlacePiece` pour ce placement précis, pour que le
    clear éventuel de CE placement ne remette pas à zéro le streak contre
    lequel il score.
  - **Vide** (`Void`) — en plus de scorer normalement, efface aussitôt une
    case aléatoire déjà remplie ailleurs sur la grille (jamais parmi les
    cases de ce placement) — premier trait "utilitaire/risque" du jeu, sans
    aucun bonus de score propre : libère de l'espace, au risque de défaire
    un combo en cours de construction. Nouvelle primitive
    `GridManager.ClearRandomFilledCell` (même patron que `LockRandomCells`),
    invoquée par `RunManager` après le retour de `Grid.PlacePiece`.
  - Aucun de ces 7 traits ne tamponne de flag sur `Cell` (contrairement à
    Golden/Tinted/Multiplier/Blast/Beacon/Seeder) : ils sont soit résolus
    AVANT `Grid.PlacePiece` (Caméléon, en changeant la couleur du placement),
    soit calculés APRÈS coup à partir du `PlacementResult`/de l'état de la
    grille (les 6 autres) — même patron que Miroir déjà établi au premier
    lot, factorisé dans `RunManager.ApplyPostPlacementTraitBonus` (dispatcher
    unique) + `AddTraitBonus`/`GetGroupShare` (aides partagées).
  - **Rareté des upgrades** (`UpgradeRarity` — Common/Uncommon/Rare),
    portée volontairement limitée aux **upgrades** (`UpgradeDefinition`,
    pools Banque + Grille/tuiles confondus) et PAS aux modificateurs
    (`ModifierDefinition`), qui restent tirés uniformément — l'utilisateur a
    demandé "un système de rareté pour les upgrades" juste après une
    discussion sur les upgrades de tuiles, pas sur les modificateurs.
    - Affecte maintenant la pondération du tirage de fin de manche
      (`UpgradeSystem.RollDraft` → `PickWeighted`, poids 8/4/2 pour
      Common/Uncommon/Rare, soit Common 4x plus probable que Rare) — avant,
      chaque pool était tiré uniformément. `UpgradeSystem.PickDistinct`
      (utilisé par les modificateurs) reste inchangé, toujours uniforme.
    - Affichée avec le "type" de l'upgrade (Banque vs Grille, relabellisé
      côté joueur en "Piece Upgrade"/"Tile Upgrade" — `UpgradeVisualDefaults.
      GetPoolLabel`) sur une ligne colorée sous le nom : directement visible
      sur les cartes de draft (`DraftView.BuildCard`, carte agrandie
      210→234px de haut pour lui faire de la place) et, pour un trait de
      tuile, dans son tooltip au survol (`TraitBadgeView`, puisqu'un
      `PieceTrait` ne porte pas de référence vers l'`UpgradeDefinition` qui
      l'a créé — sa rareté est donc dupliquée dans
      `PieceTraitVisualDefaults.GetRarity`, à tenir manuellement synchronisée
      avec `UpgradeCatalog`, même limitation déjà acceptée pour
      Name/Description).
    - `TooltipView.Show` gagne un sous-titre optionnel (`subtitle`,
      `subtitleColor`) — s'il est omis (cas des badges de modificateurs,
      inchangés), la description remonte occuper l'espace laissé libre.
  - Tests : `DeckManagerTests`/`UpgradeSystemTests` gagnent un test par
    nouvel upgrade (tag en deck) + un test statistique
    (`RollDraft_OverManySeeds_PicksCommonRarityUpgradesMoreOftenThanRare`,
    500 seeds) vérifiant que Common sort plus souvent que Rare ;
    `RunManagerTests` gagne 2 tests par trait (fire / ne fire pas, ou un
    scénario dédié pour Caméléon/Étincelle) ; `GridManagerTests` gagne des
    tests directs pour `PlacementsSinceLastClear` et
    `ClearRandomFilledCell` (y compris la garantie qu'une case verrouillée
    n'est jamais choisie, même si elle se retrouvait remplie).
- **14 modificateurs "basiques" supplémentaires : 1 par couleur + 1 par
  forme** (sur demande explicite — "il manque beaucoup de modifiers
  basique: points doublé pour une couleur, un upgrade par couleur. Idem
  pour les formes de tuiles"). Catalogue 32→46 entrées. Contrairement aux
  deux premiers lots, aucun n'a nécessité de nouvel état ou de nouvelle
  donnée — les deux sont calculables directement depuis les paramètres déjà
  reçus par `GridManager.ApplyPreClearModifiers` (`shape` et `placedCells`).
  - **4 modificateurs "Dévotion"** (`DevotionCoral`/`Teal`/`Violet`/`Lime`,
    catégorie `Couleurs`) — double intégralement (100%, pas les 50% de
    Puriste) le bonus de groupe de CE placement quand la couleur propre de
    la pièce posée correspond. Une seule case suffit à vérifier (toutes
    les cases d'un même placement partagent forcément la même couleur).
  - **10 modificateurs "Spécialiste"** (`FormeSingle`/`FormeDomH`/.../
    `FormeSTetro`, un par valeur de `ShapeId`), même mécanique (double le
    bonus de groupe à 100%) mais sur la FORME de la pièce posée plutôt que
    sa couleur. Nouvelle catégorie dédiée `ModifierCategory.Formes` (aucune
    des 5 catégories existantes ne correspondait) — accent couleur
    `#614363`, dernière teinte de la palette v1 encore inutilisée.
  - Tests : un test dédié fire/ne-fire-pas pour `DevotionCoral` et
    `FormeSq2` (vérifie explicitement le doublement à 100%, pas 50%), plus
    un test en boucle par lot (`DevotionModifiers_...`/`FormeModifiers_...`)
    qui vérifie qu'AUCUN des 4 (ou 10) ne se déclenche pour une couleur/
    forme différente de la sienne.
- **Drag-and-drop des pièces de la main** (sur demande explicite, retour de
  playtesting — "les gens auraient voulu drag and drop les tuiles au lieu
  de toggle"). Le clic (sélectionner puis cliquer une case) reste
  fonctionnel en parallèle plutôt que d'être retiré : Unity ne déclenche
  `OnBeginDrag` qu'une fois le pointeur passé le seuil de drag de
  l'`EventSystem`, donc un simple tap continue de résoudre comme un clic
  normal (`Button.onClick`) sans code supplémentaire pour les distinguer.
  - Nouveau `HandSlotDragHandler` (implémente `IBeginDragHandler`/
    `IDragHandler`/`IEndDragHandler`), un par slot de main, qui délègue à
    `HandView` : `BeginSlotDrag` sélectionne le slot exactement comme un
    clic (réutilise `OnSlotClicked`, donc l'événement `SlotSelected`
    existant et tout ce qui en dépend côté `GameBootstrap`/`GridView` ne
    change pas), puis affiche un "ghost" (aperçu flottant de la pièce,
    construit via `ShapePreviewFactory`, alpha 0.85, `CanvasGroup.
    blocksRaycasts = false` pour ne jamais voler le raycast de drop destiné
    à la grille en dessous) qui suit le pointeur.
  - `GridCellView` gagne `IDropHandler.OnDrop`, qui appelle simplement
    `OnCellClicked(X, Y)` — exactement le même chemin qu'un clic sur la
    case, donc toute la logique de placement/animation/erreur existante
    est réutilisée telle quelle, sans duplication.
  - L'aperçu de survol case-par-case (vert/rouge, déjà géré par
    `GridView.OnCellHoverEnter`/`OnCellHoverExit` via `IPointerEnterHandler`
    / `IPointerExitHandler` sur chaque `GridCellView`) fonctionne sans
    modification pendant un drag : dans le pipeline d'événements standard
    d'uGUI, le survol (hover) d'un AUTRE objet continue d'être évalué
    indépendamment de l'objet en cours de drag — aucune détection de case
    par raycast manuel n'a été nécessaire.
  - Ordre d'événements uGUI exploité : au relâchement du pointeur, `OnDrop`
    (sur la case ciblée) se déclenche TOUJOURS avant `OnEndDrag` (sur le
    slot source) — donc au moment où `HandView.EndSlotDrag` masque le
    ghost, le placement (succès ou échec) a déjà été entièrement traité.
  - Relâcher en dehors de toute case ne place rien (pas de `IDropHandler`
    sous le pointeur) : le ghost disparaît et la pièce reste sélectionnée,
    exactement comme après un clic simple — pas de logique d'annulation
    distincte nécessaire.
  - Texte de statut (`GameBootstrap`) mis à jour pour mentionner les deux
    méthodes ("Select or drag a piece onto the grid." / "Drag onto the
    grid, or click a tile, to place: ...").
- **Fix : lisibilité des textes de description** (bordure par-lettre
  retirée + taille 12→14 + gras) sur le tooltip partagé (`TooltipView`,
  modificateurs + traits de tuile) et les cartes de draft d'upgrade
  (`DraftView`).
- **Mirror Tile simplifiée** (sur demande explicite — "je ne comprend pas
  l'upgrade mirror, elle est trop compliqué à comprendre"). L'ancienne
  règle ("duplique le bonus de la case enchantée sur la case
  symétriquement opposée dans le groupe, calculée par réflexion à travers
  le centre de la boîte englobante du groupe — si une telle case existe")
  demandait de visualiser un calcul géométrique abstrait pour savoir si/où
  l'effet allait se déclencher. Remplacée par une règle simple à énoncer
  en une phrase : duplique le bonus sur **une case aléatoire parmi les
  autres cases du groupe scoré** — se déclenche systématiquement dès que
  le groupe a au moins 2 cases (même condition que Twin Tile), au lieu de
  ne fonctionner que pour des formes de groupe symétriques par rapport à
  la case enchantée.
  - `RunManager.ApplyMirrorBonus` passe de `static` à instance (a besoin
    de `_rng`) ; collecte les positions des autres cases du groupe (via
    les `ScoreEvent` de type `Group`, en excluant la case du trait
    elle-même) et pioche une cible au hasard parmi elles avec
    `_rng.Next(...)`.
  - Rareté rétrogradée de Rare à Uncommon (`UpgradeCatalog.MirrorTile`,
    `PieceTraitVisualDefaults.GetRarity`) — l'effet se déclenche
    désormais beaucoup plus fiablement qu'avant (quasi tout le temps au
    lieu de seulement sur des formes symétriques), donc moins "rare" en
    pratique, même s'il reste plus faible que Twin Tile (une seule case
    dupliquée au lieu de toutes).
  - Descriptions de Mirror Tile ET Twin Tile mises à jour (Twin se
    définissait par contraste avec le "partenaire symétrique" de Mirror —
    désormais par contraste avec sa cible aléatoire).
  - Tests : `RunManagerTests.PlacePiece_MirrorTrait_DuplicatesGroupBonusOntoSymmetricPartner`
    remplacé par 3 tests — cible déterministe quand une seule autre case
    existe, aucun déclenchement si le groupe ne contient que la pièce
    posée, et vérification (sur plusieurs graines RNG) que la cible
    choisie reste toujours une case du groupe. `UpgradeSystemTests`'s
    test statistique de pondération par rareté compare maintenant
    `GoldenCells` (Common) à `VoidTile` (Rare, resté inchangé) plutôt
    qu'à `MirrorTile` (désormais Uncommon).
- **Scoring "à la Balatro" pour le multiplicateur de groupe** (sur demande
  explicite — "j'aimerais qu'on mette x2 à la fin comme dans le calcule de
  balatro... les score seront certe plus gros, mais plus satisfaisant,
  surtout avec les golden tiles et row clear"). Changement de portée
  volontairement contenu au multiplicateur de groupe existant (Tinted
  Tile / Multiplier Zone / Multiplier Beacon) — tous les autres bonus
  "doublants" (Puriste, Devotion, Twin, Détonateur, etc.) restent des
  montants additifs totalement indépendants, inchangés.
  - Avant : le multiplicateur était appliqué case par case, gonflant
    directement `GroupBonus`, et ne touchait ni le bonus golden ni le
    bonus de clear de ligne/colonne.
  - Après : `PlacementResult.GroupBonus` redevient la simple somme non
    multipliée (cases × `ScoringConstants.GroupBonusPerCell`). Le nouveau
    champ `PlacementResult.GroupMultiplier` (calculé par
    `GridManager.ComputeGroupMultiplier`, logique de stacking inchangée)
    est appliqué UNE SEULE FOIS, à la toute fin, sur la somme complète du
    placement : `TotalScore = (GroupBonus + GoldenBonus + LineClearScore)
    * GroupMultiplier + ModifierBonus + TraitBonus`. Les bonus golden et
    de clear de ligne sont donc désormais eux aussi multipliés, ce qui
    était explicitement le but ("surtout avec les golden tiles et row
    clear").
  - Descriptions de Tinted Tile et Multiplier Tile mises à jour pour
    refléter que l'effet double "l'ENTIER du score du placement (groupe,
    golden et clear de ligne ensemble)" plutôt que "le bonus de groupe".
  - Présentation : comme les popups individuels par `ScoreEvent` portent
    maintenant des montants non multipliés, une étape finale a été
    ajoutée à `GameBootstrap.PlayPlacementSequence` (après la boucle de
    clear de ligne) qui affiche un popup "xN" au centre de la grille,
    déclenche `ComboView.Pulse()` (nouveau, même pattern de bounce
    d'échelle que `GridCellView.Pulse()`), puis rattrape le score HUD
    affiché avec le surplus manquant (`(GroupBonus + GoldenBonus +
    LineClearScore) * (GroupMultiplier - 1)`) — sans cette étape, le
    score animé à l'écran aurait fini plus bas que le score réel
    (`RunManager` ajoute déjà le `TotalScore` complet, déjà multiplié, au
    score de la manche).
  - Tests : les 6 tests de `GridManagerTests` sur Tinted/Multiplier Zone
    (seul(e), stacké, mauvaise couleur, combiné) et le test Beacon de
    `RunManagerTests` réécrits pour vérifier séparément `GroupBonus` (non
    multiplié), `GroupMultiplier` et `TotalScore` plutôt qu'un seul
    `GroupBonus` déjà multiplié. Nouveau test
    `PlacePiece_GroupMultiplier_AlsoAppliesToGoldenAndLineClearBonuses`
    qui remplit une ligne complète avec une case Multiplier Zone et une
    case golden dans le groupe, et vérifie que `TotalScore` reflète bien
    `(GroupBonus + GoldenBonus + LineClearScore) * 2`.
- **Fix : ghost du drag-and-drop trop chargé** (sur demande explicite —
  le carré bleu-mauve de fond faisait doublon avec le halo vert/rouge
  déjà affiché par `GridView` sur les cases survolées, et restait visible
  même par-dessus un emplacement valide).
  - `HandView.BuildDragGhost` construit maintenant le ghost avec
    `UIFactory.CreateUIObject` au lieu de `CreatePanel` — plus aucun
    `Image` de fond, seul l'aperçu de la pièce (`ShapePreviewFactory`)
    reste visible.
  - Nouvel évènement `GridView.HoverValidityChanged` (fired dans
    `OnCellHoverEnter`/`OnCellHoverExit`, indépendant du drag comme le
    reste du hover) câblé dans `GameBootstrap` vers
    `HandView.SetHoveringValidDrop` : pendant un drag, l'alpha du ghost
    (`CanvasGroup`) tombe à 0 dès que la case survolée accepterait la
    pièce, et remonte à `DragGhostAlpha` sinon — le halo vert/rouge de la
    grille reste alors seul visible à l'endroit exact où la pièce
    tomberait.
- **Habillage avec le pack d'assets "Colorful UI"** (sur demande explicite,
  asset pack ajouté par l'utilisateur sous `Assets/Resources/Colorful_UI/`).
  Nouvelle classe `Presentation.UISprites` : mêmes lazy-`Resources.Load`
  mis en cache que `UIFactory.DefaultFont()`, une propriété par sprite
  plutôt que de répéter le chemin `Resources` à chaque site d'appel.
  Nouveau `UIFactory.CreateSlicedImage` (Image 9-sliced, `color` neutre
  blanc) à côté de `CreatePanel` (couleur plate) pour construire ces
  panneaux/cartes à partir d'un sprite plutôt que d'une couleur.
  - Les deux barres de progression du HUD (`HudView`) partagent maintenant
    `slider/progress_bar (1).png` comme fond ; la barre de score utilise
    `slider/blueBarFill.png` et la barre de tuiles restantes
    `slider/purpleBarFill.png` comme remplissage — remplace les
    rectangles de couleur plate (`UITheme.PanelLight`/`Success`/
    `ButtonSelected`).
  - Le panneau des modifiers actifs (`ModifierPanelView`) utilise
    `gameUI/panel_bg.png` comme fond, et chaque ligne de modifier
    `gameUI/card_bg_2.png` — l'`Outline` noir de chaque ligne a été
    retiré, l'art de la carte portant déjà son propre contour/ombre.
  - Le fond de chaque slot de la main (`HandView`) utilise
    `gameUI/card_bg_3.png`.
  - `UIFactory.DefaultFont()` charge maintenant `font/Digitalt.ttf` (avec
    repli sur `LegacyRuntime.ttf` si le pack n'est pas présent dans le
    checkout) — comme c'est la seule fonction qui crée des `Text` dans
    tout le projet (`UIFactory.CreateText`, utilisée partout), la police
    change globalement sans toucher aux appelants.
  - `spriteBorder` (9-slice) réglé à la main dans le `.meta` de chacun de
    ces 6 sprites (0 par défaut à l'import) — les barres et remplissages
    (forme "pilule") ont un bord réglé sur les 4 côtés pour garder leurs
    bouts arrondis sous étirement horizontal ET vertical, les panneaux/
    cartes un bord adapté à leurs coins arrondis. Non vérifié visuellement
    dans l'éditeur Unity (indisponible dans cet environnement) — à ajuster
    au besoin via le Sprite Editor si un bord semble mal calé.
- **Fix : texte flou + retrait du gras/des bordures de texte** (sur
  constat que le nouveau texte semblait flou une fois `Digitalt.ttf` en
  place, + demande explicite de retirer le gras et les bordures des
  textes). Cause probable : le composant `Outline` d'Unity (utilisé sur
  la plupart des labels pour rester lisible sur un fond de couleur
  variable) fonctionne en dupliquant le maillage du texte, décalé de
  1-2px dans 4 directions, pour simuler un contour — combiné à
  l'anti-aliasing d'un texte déjà petit, ces copies légèrement décalées
  se chevauchent et donnent un rendu flou/baveux, d'autant plus visible
  que `Digitalt.ttf` ne fournit qu'une seule graisse : `FontStyle.Bold`
  dessus est un gras synthétique (Unity ne fait pas de vrai synthetic-bold
  sur les polices dynamiques), qui ajoute encore du flou sans gagner de
  contraste. Les deux étaient déjà présents sur beaucoup de labels avant
  le changement de police (Arial encaissait mieux ce traitement) — le
  nouveau look "pilule/carte" du pack Colorful UI ne les rendait plus
  nécessaires de toute façon (les fonds sont maintenant des panneaux
  dessinés, pas des couleurs plates changeant sous le texte).
  - `FontStyle.Bold`/`BoldAndItalic` retiré de tous les `Text` du jeu
    (HUD, tooltip, cartes de draft, panneau de modifiers, badges,
    popups de score, texte d'effet sur les tuiles, combo) — le sous-titre
    de rareté (`DraftView`) et le sous-titre du tooltip
    (`TooltipView`) gardent `FontStyle.Italic` seul (le style italique
    n'est pas concerné par le problème, seul le gras l'était).
  - Le composant `Outline` retiré de tous les `Text` (mêmes emplacements)
    — les méthodes `AddTextOutline`/`AddOutline` désormais inutilisées
    supprimées dans `ModifierPanelView`, `ModifierDraftView` et
    `TooltipView`. `FeedbackLayer.AnimatePopup` simplifié en conséquence
    (n'anime plus une couleur d'outline en parallèle du fade du texte).
  - Les `Outline` sur des éléments non-textuels (badges de trait/
    modifier, panneau du tooltip, carte de la modifier draft, marqueur
    "case invalide") sont hors scope de cette demande et restent
    inchangés — seules les bordures DE TEXTE ont été retirées.
- **Suite de l'habillage Colorful UI + fix du curseur de placement** (sur
  demande explicite).
  - Bouton "Choose" (`DraftView` et `ModifierDraftView`, ce dernier
    partagé avec le mode "Remove") : fond
    `button/emptyButtons/blueButton.png`. Bouton "Cancel" (les deux
    écrans de sous-choix dans `DraftView`) : fond `gameUI/red_btn.png`.
    Nouvelle surcharge `UIFactory.CreateButton(..., Sprite bgSprite, ...)`
    à côté de celle par couleur plate, les deux déléguant maintenant à un
    `FinishButton` privé commun (bouton + label) pour éviter la
    duplication.
  - Fond des cartes de choix d'upgrade (`DraftView.BuildCard`) :
    `gameUI/card_bg_3.png` — même sprite que les slots de la main
    (exposé séparément en tant que `UISprites.UpgradeCardBackground`,
    plutôt que de réutiliser `HandSlotBackground` directement, pour que
    chaque site d'appel garde un nom qui documente son propre usage).
  - Bannière `gameUI/Union.png` ajoutée derrière le nom de l'upgrade sur
    chaque carte de draft — vu sa forme de ruban à pointes (pas un simple
    rectangle arrondi), le `spriteBorder` du `.meta` lui donne une marge
    horizontale généreuse (28px) pour ne pas écraser les pointes en
    9-slice.
  - **Fix : curseur excentré lors du placement.** Les formes de pièce
    stockent toujours leur cellule d'origine en bas-à-gauche de leur
    boîte englobante (`PieceShapeCatalog`) ; comme la case survolée/
    cliquée servait directement de cette origine, la pièce apparaissait
    décalée en haut-à-droite du curseur au lieu d'être centrée dessus.
    Nouvelle méthode `GridView.GetPlacementOrigin(x, y)` qui décale la
    case par la moitié (arrondie vers le bas) de la largeur/hauteur de la
    boîte englobante de la forme sélectionnée — utilisée identiquement
    par l'aperçu de survol (`OnCellHoverEnter`) ET par le placement réel
    (`OnCellClicked`), pour que l'aperçu affiché corresponde toujours
    exactement à ce qui sera posé.
  - Texte "Modifiers" du panneau de modifiers actifs (`ModifierPanelView`)
    doublé (15→30) et repositionné légèrement plus haut (`anchoredPosition`
    -10→-6) ; la liste des lignes en dessous (`_rowsContainer`) décalée en
    conséquence (-40→-62) pour ne pas chevaucher le titre agrandi.
- **Nouvelle passe sur le panneau de modifiers et les cartes de draft
  d'upgrade** (sur demande explicite, avec un screenshot de référence
  pour le premier point — non reçu côté outils, ajusté au jugé).
  - Texte "Modifiers" remonté encore un peu plus (`anchoredPosition`
    -6→-2, aussi proche du bord haut du panneau que l'art le permet sans
    sortir de la bande d'en-tête de `panel_bg.png`) ; la liste des lignes
    suit (-62→-58).
  - Cartes de draft d'upgrade (`DraftView.BuildCard`, PAS les cartes de
    modifiers dans `ModifierDraftView`, vocabulaire distinct dans le
    jeu) 25% plus grandes — nouvelle constante `CardScale = 1.25f`,
    `CardWidth`/`CardHeight` passent de 200x234 à 250x292.5, et chaque
    autre décalage/taille en pixels à l'intérieur de `BuildCard`
    (bannière du nom, rareté, description, bouton Choose) multiplié par
    le même facteur pour garder une mise en page proportionnelle plutôt
    que de tasser les mêmes marges dans une carte plus grande. Les
    tailles de police du nom/de la rareté ne changent PAS (non demandé).
  - Texte de description : couleur passée de `UITheme.TextMuted` à noir
    plein (`Color.black`).
  - Texte du bouton "Choose" (sur les cartes d'upgrade) : taille 14→21
    (+50%).
- **Fix : lisibilité de la ligne rareté/type sur les cartes d'upgrade**
  (sur demande explicite avec screenshot — "les textes d'upgrades ... difficile
  à lire ... color theory, la grosseur"). Cause : `UpgradeVisualDefaults.
  GetRarityColor` (Common = `#effae6` quasi-blanc, Rare = `#f0b38d` pêche
  pâle) a été conçu pour les fonds SOMBRES où il est utilisé ailleurs
  (panneau du tooltip, badge de trait) — sur le fond lavande clair de
  `card_bg_3.png` (`DraftView.BuildCard`), ces teintes pâles sur fond
  clair devenaient quasiment invisibles (Uncommon, en bleu moyen
  `#65aed6`, restait le seul à peu près lisible des trois).
  - Nouveau `UpgradeVisualDefaults.GetRarityColorOnLight` : mêmes trois
    teintes (gris-ardoise pour Common, bleu pour Uncommon, orange pour
    Rare) mais assombries/saturées pour contraster sur fond clair au
    lieu de fond sombre — même famille de teinte donc la rareté reste
    reconnaissable au premier coup d'œil dans les deux contextes, seule
    la clarté change selon le fond. `GetRarityColor` (fonds sombres)
    reste inchangée et toujours utilisée par `TraitBadgeView` pour le
    tooltip.
  - `DraftView.BuildCard` utilise maintenant `GetRarityColorOnLight` pour
    la ligne rareté/type, avec une taille de police augmentée (12→14) et
    une boîte légèrement plus haute (16→20 avant mise à l'échelle
    `CardScale`) ; le nom de l'upgrade sur la bannière passe aussi de
    16 à 18 pour renforcer la hiérarchie visuelle de la carte. La
    description reste inchangée (déjà en noir plein sur fond clair,
    contraste déjà optimal) pour ne pas risquer un débordement sur le
    bouton "Choose" en dessous avec les descriptions les plus longues.
- **Nouvelle passe de lisibilité sur les cartes d'upgrade + effet de clic
  sur les boutons** (sur demande explicite).
  - Nom de l'upgrade (bannière) : 18→27 (+50%). Ligne rareté/type :
    14→21 (+50%). Les deux décalés/agrandis pour ne pas se chevaucher
    (bannière 40→46 avant `CardScale`, boîte de la ligne rareté 20→30).
  - Description : 14→15, plus une bonne partie du "flou" ressenti vient
    probablement du texte redevenu petit par rapport aux éléments
    voisins désormais bien plus grands — `CardHeight` gagne +30px fixes
    (en plus du `CardScale` existant) pour laisser à la description
    assez de place même dans le pire cas (~190 caractères, la
    description la plus longue du jeu) sans chevaucher le bouton
    "Choose" en dessous.
  - **Fix : effet de survol/clic manquant sur TOUS les boutons** (pas
    seulement Choose/Cancel — c'est la même fonction partagée). Cause :
    `Selectable`/`Button` assigne normalement son `targetGraphic` tout
    seul via `Reset()`, mais Unity n'appelle `Reset()` que pour les
    composants ajoutés depuis l'Inspector — comme tout ce jeu est
    construit par script (`AddComponent`), `targetGraphic` restait
    `null` sur tous les boutons du jeu et la transition de couleur
    (survol/pression, déjà configurée dans `FinishButton`) ne s'est en
    fait jamais affichée. `UIFactory.FinishButton` assigne maintenant
    `btn.targetGraphic` explicitement.
  - En plus de ce fix, nouveau `ButtonPunchEffect` (même technique de
    scale-bounce que `GridCellView.Pulse()`/`ComboView.Pulse()`) —
    petit "squish" au clic, ajouté automatiquement par
    `UIFactory.FinishButton` à tous les boutons du jeu pour un retour
    tactile plus net que la seule teinte de couleur.
- **Retrait des upgrades/modifiers liés aux locked cells (boss round),
  retrait de Symétrie, buff de Color Wheel, preview visuelle pour les
  Specialist** (sur demande explicite).
  - **Driller Tile** (upgrade de tuile) retiré entièrement : `UpgradeId`,
    `UpgradeDefinition`, `UpgradeSystem` (compteur + case `Apply`),
    `PieceTraitKind.Driller`, `DeckManager.TagDrillerTokensRandom`,
    `RunManager` (case de dispatch, `ApplyDrillerBonus` + ses deux
    helpers de détection de case verrouillée), `PieceTraitVisualDefaults`
    (nom/description/rareté/couleur de badge), `ScoringConstants.
    DrillerBonus`, et les tests associés (`DeckManagerTests`,
    `UpgradeSystemTests`, `RunManagerTests`). Raison : "trop abstrait
    trop longtemps pour le joueur" — le joueur porte cette pièce
    enchantée potentiellement plusieurs manches avant qu'une manche boss
    (avec des cases verrouillées) ne la rende pertinente.
  - **Trou dans la Grille, Cœur de Pierre, Diagonale Verrouillée, Sans
    Doublon** (4 modifiers) retirés entièrement — mêmes raisons : leur
    condition de déclenchement dépend des cases verrouillées des manches
    boss (Sans Doublon n'a pas de check de verrouillage dans son code,
    mais est structurellement quasi impossible à déclencher en dehors
    d'une manche boss — seulement 5 couleurs possibles pour une ligne de
    8 cases). `ModifierId`, `ModifierDefinition` (+ `All[]`),
    `GridManager` (cases de dispatch + méthodes `Apply*`/helpers
    associées : `IsAdjacentToLockedCell`, `IsFullyBoxedIn`,
    `IsDiagonallyAdjacentToLockedCell`, `HasNoDuplicateColor`),
    `ScoringConstants`, `ModifierVisualDefaults` (abréviations), et les
    tests associés (`GridManagerModifierTests`) tous retirés/nettoyés.
  - **Symétrie** (modifier) retiré entièrement — jugée peu claire et
    difficile ("je n'aime plus ... trop compliqué à comprendre" pattern
    déjà vu pour Mirror Tile). Même nettoyage : `ModifierId`,
    `ModifierDefinition`, `GridManager.ApplySymetrie` + son helper
    `ColorsMatch`, `ScoringConstants.SymetrieBonusPerLine`,
    `ModifierVisualDefaults`, tests.
  - **Color Wheel** (`CercleChromatique`) : bonus par cellule 18→35 (sur
    demande explicite — très difficile à déclencher, il faut que les 4
    voisins cardinaux soient remplis ET montrent les 4 couleurs de base
    à la fois).
  - **Fix : les modifiers "...Specialist" (Forme\*) montrent maintenant
    un aperçu de la forme en carrés noirs au lieu d'un nom de
    domino/tromino/tétromino** (sur demande explicite — "je n'aime pas
    qu'on ait les nom des tetromino"). Nouveau
    `ShapePreviewFactory.BuildMono(container, shape, color)` — variante
    simplifiée de `Build` (pas de couleur de pièce/icône/badge de trait,
    juste la silhouette de la forme en un carré plein par cellule).
    Nouveau `ModifierVisualDefaults.GetSpecialistShape(ModifierId)` —
    associe chacun des 10 modifiers Forme\* à son `ShapeId`.
    `ModifierBadgeFactory.Create` construit maintenant ce mini-aperçu à
    la place de l'abréviation 2-lettres sur le badge, uniquement pour
    ces 10 modifiers (les autres gardent l'abréviation textuelle
    inchangée). Le nom complet ("L-Tetromino Specialist", etc.) reste
    affiché dans le tooltip au survol — seul le badge, l'élément visible
    en permanence dans le panneau/les cartes de draft, ne montre plus le
    nom du polyomino.
- **Joker piece à forme aléatoire + descriptions d'upgrades de tuile
  raccourcies** (sur demande explicite).
  - `DeckManager.AddJoker` prenait toujours `ShapeId.Single` en dur ;
    prend maintenant un `IRandomProvider` et pioche uniformément parmi
    les 10 formes (`InitialDeckFactory.ShapeOrder`, rendu `public` pour
    être réutilisé ici plutôt que de dupliquer la liste des formes).
    `UpgradeSystem.Apply` lui passe son `_rng` existant. Description de
    l'upgrade "Joker piece" mise à jour ("random shape" plutôt que
    "single"). Tests : `AddJoker_AddsSingleJokerToken` remplacé par
    `AddJoker_AddsAJokerColoredTokenOfSomeShape` (ne vérifie plus la
    forme) + nouveau `AddJoker_OverManySeeds_PicksMoreThanJustSingleShape`
    (statistique sur 100 graines, vérifie que plus d'une forme sort).
  - Les 13 upgrades de tuile ("Golden Cells", "Tinted Cells", etc.)
    avaient toutes la même intro verbeuse répétée ("Enchants 3 random
    pieces in the deck: one tile on each ..."), jugée trop longue.
    Remplacée par un gabarit court et uniforme : "3 pieces get a tile
    that ...", qui garde l'information essentielle (3 pièces affectées,
    une tuile enchantée chacune) sans la répéter en toutes lettres à
    chaque fois — le reste du texte (l'effet propre à chaque upgrade)
    est inchangé dans le fond, juste débarrassé du superflu.
- **Fix : texte flou dans les descriptions d'upgrade — tentative #1
  (annulée)** : diagnostic initial via la fiche du pack de polices
  (`Digitalt_spec.pdf`) — `Digitalt.ttf` est une police d'affichage très
  grasse/épaisse conçue pour des titres courts en gros caractères, pas
  pour du texte de paragraphe ; à 14-15pt ses traits épais mangent
  l'espace entre les lettres. Première tentative : basculer les textes
  de description sur la police standard d'Unity (`UIFactory.BodyFont()`
  + `CreateBodyText`). **Annulée sur demande explicite** ("je ne veux
  pas la font de unity, je veux celle que je t'ai donné") — `BodyFont`/
  `CreateBodyText` retirés, `DraftView`/`TooltipView` repassés sur
  `CreateText` (`Digitalt` partout, y compris les descriptions).
  - **Tentative #2 (en place)** : garder `Digitalt` partout, mais
    attaquer le flou par les réglages d'import/rendu plutôt que par un
    changement de police :
    - `Digitalt.ttf.meta`/`Digitalt.otf.meta` : `fontRenderingMode`
      0 (Smooth) → 1 (Hinted) — force les contours des glyphes à
      s'aligner sur la grille de pixels au lieu d'un antialiasing pur,
      normalement plus net à petite taille pour une police grasse.
      `characterPadding` 1 → 2 — marge un peu plus généreuse autour de
      chaque glyphe dans l'atlas de la police, pour éviter tout
      débordement/bavure entre glyphes voisins à petite taille.
    - `GameBootstrap.BuildCanvas` : `canvas.pixelPerfect = true` — avec
      `CanvasScaler.ScaleWithScreenSize`, le facteur d'échelle réel n'est
      généralement pas un nombre entier, ce qui laisse le texte à des
      positions sous-pixel (flou d'antialiasing, plus visible sur les
      traits épais de Digitalt) ; `pixelPerfect` arrondit la position de
      rendu de chaque élément UI au pixel le plus proche.
    - Non vérifié visuellement (pas d'éditeur Unity dans cet
      environnement) — ces trois réglages sont les leviers standards pour
      ce symptôme sans changer la police elle-même ; si le flou persiste
      après ça, la cause est probablement la graisse/le style de la
      police elle-même (un seul poids disponible dans le pack, pas de
      variante plus fine à utiliser à la place).
- **New Run button skinné + cap de 5 modifiers retiré (illimité) +
  panneau de modifiers en grille 2 colonnes sans fond + 9 nouveaux
  modifiers (slots de main / taille de pièce / couleur)** (sur demande
  explicite, un seul message groupant les trois).
  - **Bouton "New Run"** : `EndScreenView` utilisait encore
    `UITheme.ButtonSelected` (couleur plate) au lieu du sprite du pack
    "Colorful UI" comme le bouton "Choose" du draft. Remplacé par
    `UISprites.ChooseButtonBackground`, même pattern que les boutons
    Choose/Cancel déjà skinnés plus tôt dans la session.
  - **Cap de modifiers retiré** : `RunManager.MaxActiveModifiers` (5)
    supprimé entièrement, avec tout le flux de retrait forcé qu'il
    déclenchait — `RunState.AwaitingModifierRemoval`,
    `RunManager.RemoveModifierAndAdvance`,
    `ModifierDraftView.ShowRemoval`/`ModifierRemoved`/`_isRemovalMode`,
    et le branchement correspondant dans `GameBootstrap.OnModifierPicked`/
    `OnModifierRemoved`. `ApplyModifierPick` ajoute maintenant le
    modifier et avance directement au tour suivant, sans jamais
    redemander de retrait — le nombre de modifiers actifs n'a plus de
    plafond. Tests : `ApplyModifierPick_RequiresRemoval_WhenPushing...`
    remplacé par `ApplyModifierPick_NeverRequiresRemoval_ModifierCount...`
    (vérifie 7 picks d'affilée, au-delà de l'ancien cap de 5, sans jamais
    passer par un état de retrait).
  - **Panneau de modifiers (`ModifierPanelView`)** : le fond
    9-slice par ligne (`UISprites.ModifierCardBackground`, désormais
    inutilisé et retiré de `UISprites`) est retiré — chaque modifier
    n'affiche plus que son badge (icône + abréviation/aperçu de forme).
    Layout passé de `VerticalLayoutGroup` (1 colonne) à
    `GridLayoutGroup` 2 colonnes (`constraintCount = 2`), même pattern
    que `ModifierDraftView`. Le panneau recalcule sa propre hauteur à
    chaque `Refresh` (`HeaderHeight + lignes×badge + espacements +
    padding`) pour rester ajusté au contenu quel que soit le nombre de
    modifiers actifs (désormais illimité).
  - **Fix : le titre "MODIFIERS" changeait de hauteur à l'écran selon le
    nombre de modifiers actifs** (rapporté avec captures d'écran à 1, 3
    et 5 modifiers). Cause : `_root` était ancré/pivoté au CENTRE
    vertical (`anchorMin/Max = (0, 0.5)`, `pivot = (0, 0.5)`) — un
    pivot centré fait grandir le panneau symétriquement dans les deux
    directions quand `sizeDelta.y` change, donc le bord haut (et le
    header qui y est ancré) se déplaçait de la moitié du delta de
    hauteur à chaque changement du nombre de modifiers. Fix : ancrage/
    pivot déplacés au HAUT (`(0, 1)`), avec un `anchoredPosition`
    équivalent à l'ancien centrage vertical (hauteur fixe 460 d'avant
    ce lot) — le panneau ne grandit plus que vers le bas, le bord haut
    (et donc le header) reste maintenant à une position écran
    constante quel que soit le nombre de modifiers.
  - **Fix #2 (cause réelle) : fond du panneau remplacé, "Modifiers" sur
    sa propre bannière** — le fix d'ancrage ci-dessus n'était qu'une
    partie du problème ; la vraie cause, signalée avec des captures
    montrant le titre à des hauteurs différentes selon le nombre de
    modifiers, c'est qu'on étirait une image 9-slice (`panel_bg.png`)
    dont l'art contient une bande d'en-tête bleue distincte "cuite"
    dans ses bordures 9-slice — cette bande se comprime/étire de façon
    visible quand la hauteur totale du panneau change, faisant bouger
    le texte par-dessus. Remplacé par le même principe que les cartes
    d'upgrade : fond du panneau = `card_bg_2.png` (une carte plate sans
    bande spéciale, s'étire proprement à n'importe quelle hauteur —
    `UISprites.ModifierPanelBackground` remplace `panel_bg` par
    `card_bg_2`) + le titre "Modifiers" posé sur sa propre bannière
    `Union.png` (nouveau `UISprites.ModifierHeaderBanner`, même sprite
    que `UpgradeNameBanner` mais propriété séparée par convention —
    même pattern que le nom d'une carte d'upgrade dans
    `DraftView.BuildCard`). La bannière a une taille fixe indépendante
    du panneau, donc son rendu ne varie plus jamais avec le nombre de
    modifiers actifs.
  - **Fix #3 : coins blancs de la carte visibles aux deux angles
    supérieurs de la bannière** (capture d'écran à l'appui) — la
    bannière `Union` avait une marge (`PanelWidth - 16`, décalée de 8px
    depuis le haut), même pattern que la bannière de nom d'upgrade dans
    `DraftView`. Mais les coins arrondis du haut de `card_bg_2` sont
    plus larges que sa bordure 9-slice du haut (seulement 6px), donc un
    encart de 8px ne suffisait pas à les couvrir — ils dépassaient de
    chaque côté de la bannière, visibles comme deux formes blanches
    arrondies. Fix : bannière posée à ras du bord (`anchoredPosition
    (0, 0)`, largeur = `PanelWidth` pleine, sans marge) pour couvrir
    entièrement le haut de la carte, coins compris.
  - **Fix #4 (abandon de la bannière) : "Modifiers" en texte plat, dans
    la couleur de fond du jeu** (sur demande explicite — "on essaye
    autre chose") — après le fix #3, la bannière `Union` couvrait bien
    les coins mais l'approche restait fragile (dépendante d'un
    alignement précis entre deux sprites différents). Remplacée par du
    texte simple directement sur la carte `card_bg_2`, coloré avec
    `UITheme.Background` (`#372e4d`, la couleur de fond du jeu) plutôt
    que `TextPrimary` — plus besoin d'aligner une bannière du tout.
    `UISprites.ModifierHeaderBanner` retiré (propriété plus utilisée).
    Le fichier `Union.png` lui-même N'A PAS été supprimé du dépôt —
    `UISprites.UpgradeNameBanner` s'en sert encore pour le nom des
    cartes d'upgrade dans `DraftView`, le retirer casserait cette
    fonctionnalité distincte.
  - **Fix #5 : "Modifiers" en blanc, collé au tout haut de la boîte**
    (sur demande explicite) — couleur passée de `UITheme.Background`
    (sombre, choisie pour le fix #4) à `Color.white` (blanc pur,
    pas `TextPrimary` qui est crème `#effae6`), et `anchoredPosition`
    remis à `(0, 0)` (au lieu de `(0, -8)`) pour que le texte touche
    directement le bord haut du panneau plutôt que d'en être décalé.
  - **Fix #6 : texte blanc invisible sur la carte quasi-blanche**
    ("le texte est disparu", capture d'écran à l'appui) — le fix #5 a
    rendu le titre littéralement illisible : blanc sur `card_bg_2`
    (elle-même quasi blanche) = zéro contraste. Fix : petite barre
    plate (un simple `UIFactory.CreatePanel`, pas un sprite 9-slice)
    dans la couleur de fond du jeu (`UITheme.Background`) posée à ras
    du bord haut et sur toute la largeur, DERRIÈRE le texte blanc —
    donne le contraste nécessaire tout en couvrant les coins arrondis
    de la carte par-dessus (des coins DROITS n'ont besoin d'aucun
    alignement avec les coins arrondis de la carte en dessous,
    contrairement aux tentatives précédentes avec la bannière `Union`,
    donc ce problème-là ne peut plus revenir). Pas d'`Outline`/contour
    de texte ajouté (respecte la demande explicite antérieure de
    retirer bold/outline de tous les textes) — le contraste vient
    entièrement de la barre derrière, pas du texte lui-même.
  - **Fix #7 : barre retirée, titre flotte au-dessus de la carte** (sur
    demande explicite — "retire la barre... on va le monter en haut de
    card_bg_2.png") — `HeaderBar` retiré entièrement ; le texte
    "Modifiers" est maintenant ancré au bord haut de la carte avec un
    pivot BAS et un petit décalage positif (`anchoredPosition (0, 6)`,
    `pivot (0.5, 0)`), donc il se dessine juste AU-DESSUS de la carte,
    par-dessus le fond sombre du jeu lui-même — le blanc y est
    parfaitement lisible sans aucun élément supplémentaire. `HeaderHeight`
    (58, l'espace réservé pour le header À L'INTÉRIEUR de la carte)
    remplacé par `TopPadding` (16, symétrique à `BottomPadding`) —
    les badges commencent maintenant juste sous le bord haut de la
    carte plutôt que sous un bandeau qui n'existe plus.
  - **Fix #8 : carte déformée/dédoublée à 0 modifier actif** (capture
    d'écran à l'appui — "comme si deux images se superposent") — avec
    zéro modifier, `TopPadding + BottomPadding` seul ne donne que 32 de
    hauteur, à peine au-dessus des bordures 9-slice de `card_bg_2`
    elle-même (24 bas + 6 haut = 30) : il ne reste presque plus de
    place pour la portion étirable du milieu, et les morceaux de
    bordure haut/bas se chevauchent visuellement (le "dédoublement"
    observé). Nouvelle constante `MinPanelHeight` (64) — la hauteur du
    panneau (calculée dans `Build` ET dans chaque `Refresh`) ne
    descend plus jamais en dessous, quel que soit le nombre de
    modifiers actifs.
  - **Refonte finale : retour à `panel_bg.png`, texte dans le bandeau
    d'en-tête, hauteur STATIQUE pour 10 modifiers (2x5)** (sur demande
    explicite — "pour éviter les problèmes on va faire autrement").
    Après une longue série de fixes (#1 à #8) tous liés d'une manière
    ou d'une autre au fait que le panneau changeait de hauteur à
    l'exécution, la cause commune est éliminée à la racine : le
    panneau utilise maintenant une taille FIXE, calculée une fois à la
    compilation (`PanelHeight = HeaderHeight + 5×BadgeSize +
    4×BadgeSpacing + BottomPadding`, pour 2 colonnes × 5 lignes = 10
    badges) et jamais modifiée dans `Refresh`. Ça permet de:
    - Remettre `panel_bg.png` comme fond (`UISprites.ModifierPanelBackground`
      repasse de `card_bg_2` à `panel_bg`) — sa bande d'en-tête bleue
      "cuite" dans les bordures 9-slice, la cause du tout premier bug
      de cette série, ne pose plus problème puisqu'elle n'a plus jamais
      besoin de s'étirer/se comprimer à l'exécution.
    - Remettre le texte "Modifiers" directement DANS cette bande
      d'en-tête (`UITheme.TextPrimary`, `TextAnchor.UpperCenter`,
      `anchoredPosition (0, -4)`) — le design d'origine, avant toute
      cette série de fixes, qui fonctionnait très bien tant que le
      panneau ne changeait pas de taille.
    - Ancrage remis au CENTRE vertical (`anchorMin/Max (0, 0.5)`,
      pivot `(0, 0.5)`) — safe maintenant que la taille ne change
      plus jamais, le bug de centre-pivot du tout premier fix (#1)
      ne peut plus se reproduire non plus.
    Les modifiers actifs restent illimités (`RunManager` n'a toujours
    aucun plafond) — au-delà de 10, la grille continue simplement de
    s'étendre par-delà la zone visible de la carte plutôt que de
    redimensionner le panneau.
  - **9 nouveaux modifiers (4ème lot)** — "slot de main, taille de
    pièce, bonus par couleur" demandés sans valeurs numériques précises ;
    interprétation de ce projet, documentée ici et dans le code :
    - **Slot 1/2/3 Loyalty** (`SlotUn`/`SlotDeux`/`SlotTrois`) : double
      le bonus de groupe de ce placement quand la pièce jouée vient du
      slot de main correspondant (0/1/2). Seul cas parmi tous les
      modifiers où `GridManager.PlacePiece` ne peut PAS évaluer
      lui-même l'effet — il ignore totalement de quel slot vient une
      pièce, seul `RunManager.PlacePiece(handIndex, x, y)` le sait.
      Résolu après coup dans `RunManager` (nouvelle
      `ApplyHandSlotModifierBonus`), même pattern déjà utilisé pour les
      `PieceTrait` du 2ème lot (Mirror/Catalyst/etc.) via
      `ApplyPostPlacementTraitBonus`.
    - **Large Format** (`GrandFormat`) : +8 pts par cellule placée
      quand la pièce jouée a au moins 3 cellules (récompense les
      pièces moyennes/grosses : tromino et tétromino).
    - **Off-Size** (`HorsNorme`) : +12 pts flat quand la pièce jouée
      n'a PAS exactement 3 cellules (récompense les tailles extrêmes —
      1, 2 ou 4 cellules — plutôt que la taille "moyenne").
      Interprétation du texte source ambigu ("point bonus lorsqu'il y a
      plus qu'un exactement un certain nombre de tuile ... point bonus
      lorsqu'il y a moins ou plus qu'un certain nombre de tuile") comme
      une paire complémentaire autour d'un seuil à 3 cellules.
    - **Coral/Teal/Violet/Lime Glow** (`EclatCoral`/`EclatTeal`/
      `EclatViolet`/`EclatLime`) : +4 pts par cellule du groupe quand la
      couleur de ce placement correspond — contrairement à
      "Devotion" (double intégralement le bonus de groupe), un bonus
      plat par tuile, plus proche du "bonus par tuile d'une certaine
      couleur" demandé littéralement. Un groupe étant toujours
      monochrome (`FindConnectedGroup`), ça revient à
      `groupCells.Count × 4` dès que la couleur correspond.
    - Fichiers touchés pour ce 4ème lot, suivant la checklist standard :
      `ModifierId`, `ModifierDefinition` (+ `All[]`),
      `GridManager` (dispatch pré-clear + `ApplyGrandFormat`/
      `ApplyHorsNorme`/`ApplyEclat`), `RunManager`
      (`ApplyHandSlotModifierBonus`), `ScoringConstants`,
      `ModifierVisualDefaults` (abréviations), tests
      (`GridManagerModifierTests` pour les 6 modifiers évaluables par
      `GridManager`, `RunManagerTests` pour les 3 modifiers de slot qui
      ne le sont pas).
- **3 upgrades visuels de feedback** (sur demande explicite, un seul
  message groupant les trois) :
  - **Popup de points d'un modifier affiché sur son icône, pas sur une
    tuile** — `GameBootstrap.PlayPlacementSequence` ancrait tous les
    popups "+X" (y compris ceux des modifiers) sur la case de la
    grille via `GridView.GetCellTransform`. Nouveau
    `ModifierPanelView.GetBadgeTransform(ModifierId)` (parcourt les
    listes parallèles `_rowIds`/`_rowBadges` déjà utilisées par
    `Pulse`) — pour un `ScoreEvent` de type `Modifier`, le popup et la
    pulsation de cellule (`GridView.PulseCell`) sont remplacés par une
    ancre sur le badge du modifier dans le panneau latéral (avec repli
    sur la case si le badge est introuvable, par prudence). Les
    événements Golden/Group/Trait ne changent pas — seuls ceux
    provoqués par un modifier actif changent d'ancre.
  - **Nombre d'utilisations d'un modifier dans son tooltip** —
    `RunManager` garde maintenant un `Dictionary<ModifierId, int>`
    (`_modifierUsageCounts`), incrémenté par une nouvelle
    `CountModifierUsage(placement)` appelée en toute fin de
    `PlacePiece` (après le bonus des modifiers de `GridManager` ET
    celui des modifiers de slot ajouté après coup — elle relit donc la
    liste finale de `ScoreEvents`, peu importe qui a produit chaque
    entrée) : chaque `ScoreEvent` de type `Modifier` compte comme une
    "utilisation". Nouveau `RunManager.GetModifierUsageCount(id)`.
    Exposé jusqu'au tooltip via un délégué optionnel
    `System.Func<ModifierId, int>` enfilé à travers
    `ModifierPanelView.Build` → `ModifierBadgeFactory.Create` →
    `ModifierBadgeView.Init` — interrogé À CHAQUE survol (pas mis en
    cache à la construction du badge, puisque le compte change à
    chaque placement) et affiché comme sous-titre du tooltip ("Used Nx
    this run"), en réutilisant le slot `subtitle` déjà existant de
    `TooltipView.Show`. Seul `ModifierPanelView` (modifiers déjà
    actifs) passe ce délégué ; `ModifierDraftView` (modifiers pas
    encore choisis) laisse le paramètre à `null`, donc aucun sous-titre
    ne s'affiche là. `GameBootstrap` passe une LAMBDA
    (`id => _run.GetModifierUsageCount(id)`), pas la méthode liée
    `_run.GetModifierUsageCount` directement — `_run` est réaffecté au
    restart mais `ModifierPanelView` n'est jamais reconstruit (juste
    `Refresh`), donc un délégué lié à l'ancienne instance aurait
    continué d'interroger un run abandonné pour toujours.
  - **Highlight du groupe complet lors du survol d'un emplacement
    valide** — jusqu'ici, survoler une case avec une pièce en main ne
    prévisualisait que l'empreinte de la pièce elle-même
    (`GridView.OnCellHoverEnter`/`SetHoverTint`), pas le reste du
    groupe connecté qu'elle rejoindrait. Nouveau
    `GridManager.PreviewGroup(shape, color, x, y)` : réutilise
    TEL QUEL le flood-fill privé `TryVisitGroupNeighbor` déjà employé
    par `FindConnectedGroup`/`PlacePiece`, mais amorcé avec les
    cellules de la pièce elle-même pré-ajoutées à `visited`/`stack`
    COMME SI elles étaient déjà remplies — sans jamais toucher l'état
    réel de la grille (aucune mutation), donc utilisable en pur
    aperçu pendant que le joueur choisit encore où déposer. Côté
    présentation : `GridView.OnCellHoverEnter` appelle `PreviewGroup`
    uniquement quand le placement est valide, puis surligne toutes les
    cases du groupe résultant qui NE font PAS partie de l'empreinte de
    la pièce elle-même (celles-ci gardent leur propre highlight
    vert/rouge existant). Tests : `GridManagerTests` (`PreviewGroup_*`,
    y compris une vérification explicite qu'aucune mutation de grille
    ne se produit).
  - **Fix : highlight de groupe invisible + popup de modifier invisible**
    (rapporté juste après coup, sans capture d'écran cette fois) :
    - **Highlight de groupe** : la première implémentation utilisait un
      lerp de couleur statique (`GridCellView.SetGroupPreviewHighlight`,
      0.35 vers `UITheme.HoverValid`) — trop subtil pour être remarqué
      contre la couleur déjà saturée d'une case remplie ("je ne vois
      aucun highlight... essaye de les faire pulser un peu au lieu").
      Remplacé par un simple appel à `GridCellView.Pulse()` (le même
      rebond d'échelle déjà utilisé pour le feedback de score) sur
      chaque case du groupe prévisualisé — `SetGroupPreviewHighlight`
      et la liste `_groupPreviewCells` qu'il fallait réinitialiser dans
      `ClearHover` sont retirés entièrement, `Pulse()` se réinitialisant
      déjà tout seul.
    - **Popup de modifier invisible** : le popup de points ET son
      animation vers le haut fonctionnaient déjà (voir plus haut) —
      mais `FeedbackLayer` est construit AVANT `ModifierPanelView` dans
      `GameBootstrap.BuildUI`, et en uGUI un sibling construit plus tôt
      se dessine EN DESSOUS d'un sibling construit plus tard. Le popup
      apparaissait donc bien sur le badge du modifier, mais caché
      derrière le fond opaque du panneau de modifiers, invisible.
      `FeedbackLayer.SpawnPopup` appelle maintenant
      `_root.SetAsLastSibling()` avant de créer chaque popup — même
      fix déjà en place pour `TooltipView`, pour la même raison
      (toujours au-dessus de tout le reste à l'écran).
- **La ou les tuiles qui ont produit les points d'un modifier pulsent
  maintenant en même temps que son icône** (sur demande explicite —
  "j'aime où on s'en va"). Depuis le déplacement du popup de points sur
  le badge (voir plus haut), `PlayPlacementSequence` n'appelait plus
  `GridView.PulseCell` du tout pour un `ScoreEvent` de type `Modifier`
  — seul le badge pulsait. Remis en place : la case correspondant à
  `scoreEvent.Position` pulse maintenant TOUJOURS (comme pour Golden/
  Group/Trait), en plus du badge pour un événement de type `Modifier`,
  pas à sa place. Les modifiers par-cellule (Forteresse, Carrefour,
  Prisonnier, etc.) génèrent déjà un `ScoreEvent` séparé par case
  qualifiante — donc chacune pulse à son tour au fil de la séquence,
  sans changement supplémentaire nécessaire ; les modifiers à bonus
  plat (Prisme, Devotion, etc.) n'ont qu'une seule case représentative
  (`placedCells[0]`) à faire pulser.
- **Le combo accélère à chaque ajout** (sur demande explicite — "pour
  que le joueur n'ait pas trop à attendre lors d'un gros combo").
  `PlayPlacementSequence` attendait un délai FIXE (`ScoreEventStaggerSeconds`
  = 0.22s, `LineClearStaggerSeconds` = 0.14s) entre chaque popup — un
  gros combo (beaucoup de cellules de groupe + lignes complétées +
  modifiers) prenait donc un temps qui grandissait linéairement avec sa
  taille. Nouvelle variable locale `staggerSpeed` (démarre à 1, multipliée
  par `ComboSpeedupFactor` après CHAQUE ajout au combo — que ce soit un
  événement de score, une cellule de ligne effacée, ou le rattrapage du
  multiplicateur final) : le délai réel de chaque étape est
  `délaiDeBase × staggerSpeed`, borné en dessous par `MinStaggerSeconds`
  pour qu'une très longue chaîne garde un minimum de rythme perceptible
  plutôt que de s'effondrer en un dump instantané. Une seule variable
  partagée entre les deux boucles (score events puis lignes effacées)
  plutôt qu'une par boucle — le combo accélère comme UNE séquence
  continue, pas deux qui repartiraient chacune à pleine vitesse.
  `ComboSpeedupFactor` = 0.9 (10% plus vite par ajout) jugé trop rapide
  sur premier essai — ramené à 0.97 (3% par ajout), et
  `MinStaggerSeconds` remonté de 0.03s à 0.1s en même temps (sur
  explicite demande — "10% c'est trop rapide, faisons 3% et floored
  plus haut").
- **Les slots de main ne se décalent plus quand on joue une pièce** (sur
  demande explicite — "laisse la slot 1 vide au lieu de transférer la
  slot 2 et 3 vers la slot 1 et 2"). `DeckManager._hand` était un
  `List<PieceToken>` qui RÉTRÉCISSAIT à chaque `PlayFromHand`
  (`_hand.RemoveAt(handIndex)`) — retirer l'index 0 décalait
  automatiquement les pièces des slots 1/2 vers 0/1 (comportement natif
  de `List.RemoveAt`), une main pleine ne se remplissant qu'une fois
  totalement vide. Au-delà de contredire la demande explicite, ça
  minait aussi la clarté des modifiers "Slot N Loyalty" (4ème lot,
  `SlotUn`/`SlotDeux`/`SlotTrois`) — l'identité "slot" d'une pièce
  n'était pas stable tant que la main n'était pas complètement vide.
  - `DeckManager._hand` devient `List<PieceToken?>` — TOUJOURS
    exactement `HandSize` (3) entrées ; un slot vide vaut `null` plutôt
    que d'être retiré de la liste. Nouvelle
    `DeckManager.IsHandFullyEmpty()` (remplace les anciens tests sur
    `Hand.Count == 0`). `PlayFromHand` fait maintenant
    `_hand[handIndex] = null;` (aucun décalage) et ne redistribue une
    main fraîche que quand `IsHandFullyEmpty()` est vrai. `DrawNewHand`
    laisse aussi chaque slot restant à `null` (plutôt que de
    raccourcir la liste) si jamais la pioche venait à s'épuiser en
    cours de tirage.
  - `Hand` est maintenant `IReadOnlyList<PieceToken?>` — répercuté
    partout où un appelant lisait directement `Hand[i]`/`Hand.Count` :
    `RunManager.PlacePiece` (vérifie `.HasValue` avant de déréférencer
    via `.Value`), `RunManager.HasAnyHandPlacement`/`EvaluateRoundEnd`
    (ignore les slots vides plutôt que de planter dessus),
    `GameBootstrap` (mêmes vérifications), et `HandView` — qui, en
    réalité, construisait DÉJÀ ses 3 emplacements de slot comme des
    objets fixes et indépendants (`_slotBackgrounds[3]`,
    `_previewContainers[3]`) en basculant juste leur contenu visible
    via `i < Hand.Count` : il a suffi de remplacer ce test par
    `Hand[i].HasValue` pour que l'UI affiche déjà correctement un slot
    vide à sa vraie position, sans aucun autre changement visuel.
  - Tests : `DeckManagerTests` (nouveaux
    `PlayFromHand_LeavesThatSlotEmpty_WithoutShiftingTheOthers`,
    réécriture de `PlayFromHand_OnlyRefillsWhenHandFullyEmpty`/
    `..._WithRefillIfEmptyFalse_...`/`Draw_IsWithoutReplacement_...`,
    nouveau helper `AllSlotsFilled`). `RunManagerTests` — la plupart des
    tests plaçaient une pièce via `Deck.Hand[0]` en supposant qu'il y
    en aurait toujours une là après un `PlayFromHand` précédent ; trois
    nouveaux helpers (`FirstOccupiedHandSlot`, `AllHandSlotsFilled`, et
    `ChurnUntilHandMatches` qui renvoie maintenant l'INDEX du slot
    trouvé au lieu de rien, en alternant les slots 0/1/2 plutôt que de
    ne rejouer que le slot 0) portent cette adaptation à travers la
    quinzaine de tests concernés, chacun utilisant maintenant l'index
    réel du slot occupé/trouvé plutôt que de supposer `0`.
- **Badge d'origine des upgrades de tuile + vue de deck en jeu** (sur
  demande explicite — "j'aimerais qu'on montre les upgrades des tuiles
  lorsqu'elles sont sur la grille, ça aide à keep track de son deck.
  Aussi ça me prendrait un input in game pour afficher son deck").
  - `Cell.OriginTrait` (nouveau champ `PieceTrait?`) : marque, purement
    cosmétique, du trait ayant enchanté cette case, posée par
    `RunManager.ApplyTokenTrait` pour TOUS les traits (y compris ceux
    du 2ème lot — Mirror/Catalyst/Twin/Detonator/Chameleon/Spark/Void —
    qui ne posent aucun des drapeaux Golden/Tinted/MultiplierZone).
    Contrairement à ces drapeaux (effacés juste après le score de la
    pose par `ClearTokenTraitCells`, sauf Seeder), `OriginTrait` n'est
    jamais dans la liste `transientCells` qu'on efface — il survit donc
    au score ET aux effacements de ligne (`GridManager.CheckAndClearLines`
    ne touche déjà pas Golden/Tinted/MultiplierZone), pour rester le
    seul indice visuel durable du trait posé. Remis à `null` seulement
    à la fin de la manche (`Cell.ResetForNewRound`), comme le reste.
  - `GridCellView` : 4ème badge (coin haut-droit, seul coin encore
    libre — Golden est haut-gauche, le badge spécial Tinted/Multiplier
    bas-droite, l'icône couleur et le marqueur invalide au centre),
    coloré via `PieceTraitVisualDefaults.GetBadgeColor` et portant un
    `TraitBadgeView` pour le tooltip au survol — même composant déjà
    utilisé par les badges de trait des pièces en main
    (`ShapePreviewFactory.BuildTraitBadge`). `GridView`/`GameBootstrap`
    doivent donc maintenant construire le `TooltipView` AVANT le
    `GridView` (et pas seulement avant `HandView` comme avant), et le
    lui passer en paramètre de `Build`.
  - `DeckView` (nouvelle vue) : montre la composition complète du deck
    persistant (`DeckManager.GetDeckComposition`), une ligne par
    combo forme/couleur avec son décompte et un badge de trait
    représentatif — même approche que la liste de types de
    `DraftView` (Retirer/Dupliquer/Recolorer), réutilisée telle quelle
    mais sans bouton ni flux de sous-choix puisque cette vue est
    purement informative. Basculée par la touche **Tab**
    (`GameBootstrap.Update`, toujours actif — contrairement au
    raccourci de debug F9 qui reste réservé à l'éditeur).
- **Bug corrigé : les tiles Tinted ne matchaient presque jamais** (sur
  signalement explicite du joueur — "les tinted tiles sont vraiment
  chiantes, il se peut qu'elle serve a rien parfois"). La description
  affichée en draft (`UpgradeDefinition.TintedCells`) a toujours promis
  "doubles the placement's ENTIRE score if it lands as **the piece's
  own color**", mais l'implémentation (`DeckManager.TagTintedTokensRandom`)
  tirait la couleur cible au hasard, **indépendamment** de la couleur
  réelle (fixe) de la pièce — un bug d'implémentation, pas un choix de
  design : la couleur d'une pièce ne change jamais d'elle-même, donc
  dans l'écrasante majorité des cas (toutes les couleurs sauf une sur
  ~7-8) la tuile ne pouvait plus JAMAIS matcher pour le reste de la
  partie, pas juste "parfois". `TagTintedTokensRandom` cible maintenant
  toujours la couleur propre du token — la tuile matche donc
  systématiquement. Les tokens Joker sont exclus de la sélection
  (nouveau paramètre `eligible` sur `TagRandomTokens`, partagé par tous
  les `Tag*TokensRandom`) : une cellule Joker posée garde `FilledColor
  = PieceColor.Joker` telle quelle en mémoire (jamais résolue vers la
  couleur d'un voisin, voir `GridManager.PlacePiece`), donc aucune
  TintedColor non-Joker ne pourrait jamais la matcher — l'enchantement
  serait resté mort de la même façon. Texte de tooltip
  (`PieceTraitVisualDefaults.GetDescription`) mis à jour en conséquence.
- **Différenciation Tinted Tile / Multiplier Zone** (sur remarque
  explicite du joueur, juste après le correctif ci-dessus : "A ce
  moment elle a le même effet que MultiplierZone, il faudrait trouver
  une manière de les différencier"). Une fois Tinted garanti de
  toujours matcher, les deux traits doublaient tous les deux
  intégralement le score de la pose (`(GroupBonus + GoldenBonus +
  LineClearScore) * multiplicateur`) — Tinted (Common) devenait donc
  une version strictement moins chère de Multiplier Zone (Uncommon),
  sans aucune différence mécanique. Le multiplicateur est maintenant
  scindé en deux sur `PlacementResult` : `GroupMultiplier` (inchangé,
  cumule Tinted ET Multiplier Zone) s'applique seulement à `GroupBonus
  + GoldenBonus` ; un nouveau `LineClearMultiplier` — qui ne compte
  QUE les cellules Multiplier Zone, jamais Tinted — s'applique
  seulement à `LineClearScore`. `Multiplier Zone` reste donc "double
  tout, bonus de ligne inclus" (portée large, rareté Uncommon) tandis
  que `Tinted Tile` devient "double le groupe et le golden, mais
  jamais le bonus de ligne" (portée plus étroite, rareté Common) —
  toujours garanti de se déclencher, juste plus faible. Aucun test
  existant n'a dû changer (`LineClearScore` valait 0 dans tous les cas
  déjà couverts, donc `TotalScore` reste identique) ; deux nouveaux
  tests (`GridManagerTests`) posent une ligne complète avec une
  cellule Tinted d'un côté et Multiplier Zone de l'autre pour figer la
  différence. Le popup de rattrapage du multiplicateur
  (`GameBootstrap.PlayPlacementSequence`) recalcule maintenant
  `multipliedExtra` à partir des deux facteurs séparément au lieu d'un
  seul `GroupMultiplier` global.
- **Ghost overlay sur le slot de main sélectionné + dé-sélection au
  re-clic** (sur demande explicite — "Il faut rajouter un ghost
  overlay quand on click sur un slot pour savoir qu'on l'a d'actif.
  Aussi si on re-click sur le même slot, on devrait dé-selectionner la
  slot"). Le seul signal "sélectionné" existant (`UpdateSelectionVisuals`
  teinte le SPRITE DE FOND du slot) est posé DERRIÈRE l'aperçu de la
  pièce — facile à manquer une fois que les couleurs de la pièce
  couvrent la majorité du slot. `HandView.Build` ajoute maintenant une
  Image supplémentaire par slot (`SelectionOverlay`, même sprite
  9-slice `HandSlotBackground` que le fond, teinté `UITheme.
  ButtonSelected` à 55% d'opacité), posée en DERNIER enfant du slot
  (donc au-dessus de l'aperçu) et `raycastTarget = false` (ne doit
  jamais avaler le clic destiné au Button du slot) — activée seulement
  sur le slot actuellement sélectionné, dans `UpdateSelectionVisuals`.
  Pour le re-clic : `HandView.OnSlotClicked` vérifie maintenant si
  `idx == _selectedIndex` et appelle `ClearSelection()` + un nouvel
  événement `SelectionCleared` (au lieu de re-sélectionner
  silencieusement le même slot) ; `GameBootstrap` s'y abonne pour vider
  l'aperçu de la grille (`_gridView.SetSelectedShape(null)`), comme
  après une pose réussie. La logique de sélection elle-même est
  extraite dans un nouveau `SelectSlot(idx)` privé (sans le test de
  toggle) — nécessaire parce que `BeginSlotDrag` appelait auparavant
  `OnSlotClicked` pour sélectionner le slot au début d'un drag ; avec
  le toggle en place, démarrer un drag sur un slot DÉJÀ sélectionné
  l'aurait dé-sélectionné par erreur au lieu de le garder actif pour le
  drop. `BeginSlotDrag` appelle maintenant `SelectSlot` directement, en
  contournant le toggle.
- **Le "ghost" suit maintenant le curseur en click-select, pas
  seulement en drag-and-drop + opacité liée à la validité** (sur
  demande explicite — "Lorsque je récupère une pièce d'une des slots,
  j'ai une tuile avec mon curseur seulement lors du drag and drop,
  j'aimerais que ce soit le cas pour les deux. Aussi j'aimerais que
  cette tuile là soit en 50% d'opacité lorsque le placement n'est pas
  règlementaire et 100% lorsque la position est valide"). Avant, le
  ghost (`HandView._dragGhost`) n'était montré/déplacé que pendant un
  drag actif (`BeginSlotDrag`/`DragSlot`/`EndSlotDrag`, via
  `HandSlotDragHandler`) ; un simple clic sélectionnait la pièce sans
  aucun aperçu suivant le curseur. Sa visibilité est maintenant purement
  fonction de la SÉLECTION (`HandView._selectedIndex >= 0`), plus de la
  présence d'un drag : `SelectSlot` (appelé aussi bien par
  `OnSlotClicked` que par `BeginSlotDrag`) affiche le ghost via le
  nouveau `ShowCursorGhost`, et un nouveau `Update()` le fait suivre
  `Input.mousePosition` à chaque frame tant qu'une pièce est
  sélectionnée — plus besoin d'un drag actif pour ça.
  `DragSlot`/`OnDrag` reste branché (et met toujours à jour la
  position depuis `eventData.position`) uniquement pour éviter un
  retard d'une frame pendant un drag réel — Update() ferait
  sensiblement la même chose de toute façon. `EndSlotDrag` ne cache
  plus le ghost inconditionnellement (il ne fait plus rien) : un drop
  raté doit laisser la pièce sélectionnée ET son ghost visible, prêts à
  retenter, au lieu de tout effacer silencieusement. Opacité :
  `CursorGhostValidAlpha` / `CursorGhostInvalidAlpha` remplacent
  l'ancien `DragGhostAlpha = 0.85f` — `SetHoveringValidDrop` (toujours
  branché sur `GridView.HoverValidityChanged`, qui se déclenche pareil
  qu'on soit en train de driver ou juste survoler en ayant cliqué)
  choisit maintenant directement entre les deux. Champ
  `_hoveringValidDrop` supprimé au passage (jamais lu nulle part, mort
  depuis le départ). `BeginSlotDrag` perd son paramètre
  `PointerEventData` (plus utilisé, la position initiale du ghost vient
  maintenant de `Input.mousePosition` dans `ShowCursorGhost`) —
  `HandSlotDragHandler.OnBeginDrag` mis à jour en conséquence.
  - **Clarification immédiate** : le premier jet mettait
    `CursorGhostValidAlpha = 1f` (opaque sur une position valide,
    fondu à 50% sinon), mais le joueur voulait dire autre chose par
    "100% d'opacité sur une position valide" — le preview DÉJÀ
    parfaitement aligné sur la grille (`GridCellView.SetHoverTint`,
    icône couleur sur chaque case du footprint), pas le ghost lui-même
    qui ne fait que suivre le curseur brut sans jamais vraiment
    s'aligner sur les cases. `CursorGhostValidAlpha` repassé à `0f`
    (le ghost redevient invisible sur une position valide, comme
    avant `DragGhostAlpha`) — seul `CursorGhostInvalidAlpha = 0.5f`
    (au lieu de l'ancien 0.85f) reste du changement précédent.
- **Preview de pièce à taille réelle** (sur signalement explicite —
  "J'ai un problème avec la single cell, le preview dans la slot et le
  ghost ne sont pas à taille réelle, j'aimerais que ce le soit").
  `ShapePreviewFactory.Build`/`BuildMono` calculaient la taille d'une
  case en divisant simplement la boîte du conteneur par le nombre de
  colonnes/lignes de la pièce (`Mathf.Min(container.sizeDelta.x / cols,
  container.sizeDelta.y / rows)`) — sans aucun plafond. Pour une pièce
  à 2+ cellules ça reste proche de la vraie taille de case (54px), mais
  pour **Single** (1x1), ça remplissait toute la boîte de preview
  (100x110 dans un slot de main ou le ghost) : ~100px, presque le
  double de la vraie taille de case sur la grille. Nouveau
  `VisualDefaults.GridCellSize = 54f` (Data ne pouvant pas dépendre de
  Presentation, la constante vit côté Data — même valeur que
  `GameBootstrap.CellSize`, qui la référence maintenant au lieu de
  dupliquer le littéral `54f`) sert de plafond supplémentaire dans le
  `Mathf.Min` des deux méthodes : une pièce qui a déjà besoin de plus
  de place garde exactement le même rendu qu'avant, seule une pièce qui
  aurait autrement été étirée AU-DELÀ de la vraie taille de case est
  maintenant bridée à 54px, peu importe la taille de sa boîte de
  preview.
- **Bug corrigé : les upgrades de tuile survivaient à leur propre
  destruction** (sur signalement explicite — "On oublie de détruire
  les tile upgrade, lorsque celles-ci sont détruite avec des line
  clear ou autre"). `GridManager` avait deux endroits qui vident une
  case (`CheckAndClearLines` pour un line clear, et
  `ClearRandomFilledCell` pour le trait "Void Tile") — tous les deux
  ne remettaient à zéro que `IsFilled`/`FilledColor`, en oubliant
  `IsGolden`/`IsTinted`/`IsMultiplierZone`/`OriginTrait`. Résultat : une
  case enchantée (notamment "Seeder", golden pour le reste de la manche
  tant qu'elle reste remplie) gardait son enchantement même une fois
  vidée — si une pièce complètement différente atterrissait ensuite sur
  cette même case, elle héritait injustement du bonus (golden, tinted,
  multiplicateur) d'un enchantement qui ne lui appartenait pas.
  Nouveau `Cell.ClearFill()` — centralise le vidage d'une case (tout
  sauf `IsLocked`, propriété de la manche boss et non de ce qui la
  remplit) et remet À LA FOIS `IsFilled`/`FilledColor` ET les 4 champs
  d'enchantement à zéro — appelé aux deux endroits ci-dessus à la place
  du couple `IsFilled = false; FilledColor = null;` répété
  manuellement. Nouveaux tests (`GridManagerTests`) : compléter une
  ligne contenant des cases Golden/Tinted/MultiplierZone/OriginTrait
  vide bien les 4 (et une pièce non-liée posée ensuite au même endroit
  ne score plus de bonus golden fantôme) ; `ClearRandomFilledCell` fait
  pareil sur la case qu'il vide.
- **Bug corrigé : impasse non détectée après un redraw de main
  complète** (sur signalement explicite, avec capture d'écran d'une
  grille bloquée — "Je suis dans une impasse... je ne peux pas jouer
  de tuile et pourtant je n'ai pas perdu"). `RunManager.EvaluateRoundEnd`
  saute volontairement sa vérification d'impasse quand la main est
  complètement VIDE (rien à évaluer) — mais `PlacePiece` tire
  IMMÉDIATEMENT une toute nouvelle main de 3 pièces juste après (`Deck.
  DrawNewHand()`) sans jamais revérifier si CETTE nouvelle main a ne
  serait-ce qu'un seul emplacement légal sur le plateau. Une grille
  devenue injouable pile au moment où la main se vidait passait donc
  inaperçue indéfiniment : le joueur se retrouvait avec 3 pièces qu'il
  ne pourra jamais poser, l'état restant `InProgress` pour toujours.
  Même bug (plus rare) dans `StartRound` : le tirage de la toute
  première main d'une manche boss (grille très verrouillée) n'était
  pas non plus revérifié. `EvaluateRoundEnd()` est maintenant rappelé
  juste après chaque `DrawNewHand()` (dans `PlacePiece` ET dans
  `StartRound`) — la garde "main vide → on saute la vérif" ne
  s'applique plus puisque la main venant d'être tirée n'est justement
  plus vide. Nouveau test (`RunManagerTests`) qui construit une grille
  entièrement verrouillée sauf l'empreinte exacte de la dernière pièce
  en main, purge du deck (via `RemoveOneOfType`, en respectant
  `DeckManager.MinDeckSize`) toute forme qui pourrait encore rentrer
  dans le trou que cette pose va rouvrir en complétant ses lignes/
  colonnes (vérification géométrique générique — testée à chaque
  rotation, pas de seed codé en dur), puis confirme que `State`
  bascule bien sur `RunDefeat` dès cette pose plutôt que de rester
  `InProgress` avec une main fraîche injouable.
- **Bastion Tile (upgrade de tuile "locked cell")** (sur demande
  explicite — "Locked cell upgraded. N'est pas cleared mais fait quand
  même les points cleared"). Nouveau `PieceTraitKind.Bastion` : une
  fois la pièce enchantée posée, sa case se verrouille en place pour
  le reste de la manche au lieu d'être vidée par un line clear, mais
  continue à rapporter le bonus de line clear à chaque fois que sa
  ligne/colonne se complète — exactement comme si elle avait été
  vidée, sauf qu'elle ne l'est jamais. Contrairement à la plupart des
  traits (stampés puis nettoyés dans la même pose via
  `ClearTokenTraitCells`), Bastion ne peut pas se verrouiller AVANT
  l'appel à `Grid.PlacePiece` : `CanPlace` la rejetterait comme une
  case déjà verrouillée. `RunManager.ApplyBastionEffect` s'exécute
  donc APRÈS que la pose a été résolue (comme "Void Tile"), et vérifie
  d'abord que la case est bien toujours remplie — si le line clear de
  cette pose même l'a déjà vidée avant qu'on ait pu la verrouiller,
  rien à faire cette fois-ci.
  - Côté `GridManager` : nouveau `Cell.IsBastion`, inclus dans
    `HasAnyModifier`. `CheckAndClearLines` (via le nouveau helper
    `CollectLineCell`) distingue maintenant une case verrouillée
    ordinaire (jamais ajoutée à `cellsToClear`, jamais comptée) d'une
    case Bastion verrouillée (jamais ajoutée à `cellsToClear` non
    plus, mais ajoutée à un nouveau `ClearInfo.BastionBonusCells`).
    `PlacePiece` ajoute ce compte au `LineClearScore` au même tarif
    que `ClearedCells` (`ScoringConstants.LineClearBonusPerCell`) et
    émet un nouveau `ScoreEventType.Bastion` par case (au lieu de
    `LineClear`, pour que la présentation ne tente pas de l'animer
    comme vidée — voir plus bas).
  - Côté présentation (`GameBootstrap`) : les événements `Bastion`
    passent par la boucle normale des popups (pulse + "+N", couleur
    `UITheme.Success`) mais ne touchent jamais `GridView.ClearCellVisual`
    (réservé à `ClearedCells`), donc la case reste visuellement remplie.
    `GridCellView.ApplyState` distinguait jusqu'ici "verrouillé" et
    "rempli" comme mutuellement exclusifs (une case boss verrouillée
    n'était jamais aussi remplie) ; nouveau `renderAsLockedObstacle =
    cell.IsLocked && !cell.IsBastion` pour qu'une case Bastion se
    rende comme une case normale remplie (couleur, fill tile, icône
    couleur, badge d'origine) plutôt que comme l'obstacle gris/pattern
    d'une case verrouillée classique.
  - `UpgradeId.BastionTile` (pool Grid, rareté Uncommon — entre les
    upgrades Common et les plus puissants comme Void/Beacon/Twin) +
    `DeckManager.TagBastionTokensRandom` + wiring `UpgradeSystem`/
    `PieceTraitVisualDefaults`, même schéma que tous les autres
    upgrades de tuile.
  - Nouveaux tests : `GridManagerTests` (une case Bastion verrouillée
    manuellement survit à un line clear tout en rapportant le bonus,
    `LockRandomCells`/nouveau `LockFreeCellsAndCheckClears` n'y
    touchent jamais puisqu'ils ne ciblent que des cases libres),
    `RunManagerTests` (flux complet via `DeckManager.TagBastionTokensRandom`
    — la case se verrouille à la pose, survit à un line clear déclenché
    par une pose complètement différente ailleurs sur le plateau).
- **Kamikaze Tile (upgrade de tuile destructrice)** (sur demande
  explicite — "Clear les 8 cell autour. +6 points par tuile détruite
  sauf tuile kamikaze posé à l'instant"). Nouveau
  `PieceTraitKind.Kamikaze` : à la pose, détruit (vide, via
  `Cell.ClearFill()`) chacune des 8 cases environnantes (voisinage de
  Moore) qui sont remplies et non verrouillées, et rapporte
  `ScoringConstants.KamikazeBonusPerDestroyedCell` (6) par case
  effectivement détruite. Comme "Void Tile", les propres cases de
  cette pose sont exclues de la destruction (même convention "on ne
  détruit jamais ce qu'on vient tout juste de poser") — géré dans
  `RunManager.ApplyKamikazeEffect`, résolu après coup comme Bastion
  (les deux rejoignent la liste des traits "second/troisième lot" qui
  ne stampent rien avant `Grid.PlacePiece`). `UpgradeId.KamikazeTile`
  (pool Grid, rareté Rare comme Void Tile — effet destructeur puissant)
  + `DeckManager.TagKamikazeTokensRandom` + wiring habituel. Nouveaux
  tests (`RunManagerTests`) : les 8 voisins remplis d'une case
  enchantée sont bien détruits et rapportent le bonus attendu ; une
  pièce à 2 cases (Domino H) dont l'autre case tombe dans le rayon de
  destruction de la case enchantée survit toujours, peu importe
  laquelle des deux cases a été tirée au sort par l'upgrade.
- **Refonte de la manche boss : verrouillage progressif au lieu d'un
  bloc fixe au démarrage** (sur demande explicite — "le boss est
  beaucoup trop difficile, on va faire autre chose. Chaque 3 pièce
  joué (après avoir compté les bonus), le boss va lock 2 nouvelle
  tuile dans un emplacement de la grille qui est libre. Il va falloir
  valider pour clear line si jamais ça permet de clear line").
  L'ancien verrouillage de 14 cases dès `StartRound` (avant même la
  première pose de la manche) est supprimé ; `RunConfig
  .BossLockedCellCount` disparaît au profit de deux nouvelles
  constantes, `BossLockPiecesInterval` (3) et `BossLockCellsPerInterval`
  (2). Pendant la manche boss, une fois tous les
  `RunConfig.BossLockPiecesInterval` pièces jouées, `RunManager
  .ApplyBossLockTick` verrouille 2 cases vides de plus — le plateau se
  resserre donc progressivement sur toute la manche plutôt que d'un
  coup, et le score de CETTE pose est déjà entièrement compté avant
  que le verrouillage n'intervienne ("après avoir compté les bonus").
  - Nouveau `GridManager.LockFreeCellsAndCheckClears` : ne choisit ses
    candidats QUE parmi les cases encore vides et non verrouillées
    (jamais une case que le joueur a réellement remplie — "un
    emplacement de la grille qui est libre"), puis revalide
    immédiatement pour un clear via le même `CheckAndClearLines` que
    pour une vraie pose ("il va falloir valider pour clear line si
    jamais ça permet de clear line") : si verrouiller la toute
    dernière case vide d'une ligne/colonne la complète, elle se vide
    et rapporte son score exactement comme d'habitude, y compris pour
    les cases Bastion. Retourne un nouveau `BossLockOutcome` (cases
    verrouillées, cases vidées, score, événements) — `LockRandomCells`
    (utilisée ailleurs pour d'anciens tests) filtre désormais aussi
    les cases déjà remplies par cohérence, même si son seul site
    d'appel restant s'exécute toujours sur un plateau tout juste
    remis à zéro.
  - `RunManager.PlacePiece` compte les pièces jouées cette manche
    (`CurrentBudget - PiecesRemainingThisRound`) et déclenche le tick
    tous les `BossLockPiecesInterval`, ajoutant son éventuel score au
    round/total AVANT `EvaluateRoundEnd()` (une victoire ou une
    impasse déclenchée par le verrouillage lui-même est donc bien
    détectée immédiatement). Nouveau `PlacementOutcome.BossLockedCells`
    pour que la présentation sache quand rafraîchir la grille
    (`GameBootstrap` appelle `GridView.Refresh()` seulement quand ce
    tick a effectivement eu lieu) ; texte de statut de la manche boss
    mis à jour pour décrire le nouveau mécanisme au lieu de l'ancien
    nombre fixe.
  - Nouveaux tests : `GridManagerTests` (`LockFreeCellsAndCheckClears`
    ne verrouille jamais une case remplie ; verrouiller la toute
    dernière case vide du plateau complète et vide effectivement ses
    lignes/colonnes), `RunManagerTests` (une manche boss démarre sans
    aucune case verrouillée, et exactement 2 nouvelles apparaissent
    pile après le 3e placement, pas avant — le run est avancé
    jusqu'à la manche boss via `DebugForceRoundComplete` en boucle
    plutôt qu'en jouant réellement 7 manches complètes).
- **8 nouveaux modificateurs** (sur demande explicite, conçus par
  Claude faute de liste fournie — brainstorm libre suivant les mêmes
  familles que les lots précédents, en évitant tout ce qui a déjà été
  retiré pour être "peu clair" comme Symétrie ou lié aux cases
  verrouillées comme Diagonale Verrouillée) :
  - **Diagonal** (`Diagonale`, Voisinage) : +5 pts par case du groupe
    posée sur l'une des deux diagonales principales du plateau
    (`x == y` ou `x + y == Size - 1`).
  - **Nest** (`Nid`, Voisinage) : +3 pts par case du groupe dont
    EXACTEMENT 3 des 4 voisins cardinaux sont remplis — variante plus
    accessible de Forteresse/Prisonnier (qui exigent respectivement 8
    et 4 voisins remplis).
  - **Solitaire** (`Solitaire`, Connexions) : +12 pts quand le groupe
    obtenu par cette pose est entièrement la pièce elle-même (aucune
    case préexistante fusionnée dedans) ET fait plus d'une case —
    l'inverse de Catalyst Tile (qui récompense au contraire la fusion
    avec l'existant) ; le cas à 1 case reste le pré carré d'Îlot.
  - **Open Space** (`EspaceLibre`, Roguelike) : +15 pts tant que le
    plateau entier compte au plus 16 cases remplies (25%) une fois la
    pose résolue — récompense un jeu qui garde le plateau dégagé,
    inspiré directement de "bonus si le plateau est à moins de X% de
    remplissage".
  - **Burst** (`Rafale`, Destruction) : +20 pts quand cette pose
    complète une ligne ET que la pose immédiatement précédente cette
    manche en avait déjà complété une — deux clears d'affilée,
    inspiré directement de "bonus pour enchaîner 2 clears sur 2
    placements consécutifs". Réutilise le compteur de streak déjà
    suivi pour Spark Tile (`GridManager.PlacementsSinceLastClear`,
    capturé avant que cette pose ne le mette à jour) plutôt que
    d'ajouter un nouveau champ : un streak de 0 juste avant cette pose
    veut dire que la précédente a cleared — sauf sur la toute première
    pose de la manche, où le streak commence aussi à 0 sans qu'il y ait
    eu de pose précédente (`previousGroupSize.HasValue` sert à
    distinguer les deux cas).
  - **Small Format** (`PetitFormat`, Roguelike) : +5 pts par case
    posée quand la pièce fait au plus 2 cases — le pendant "petit
    format" de Grand Format (qui récompense l'inverse, ≥3 cases).
  - **Freshness** (`Fraicheur`, Couleurs) : +10 pts quand la couleur
    de cette pose n'est encore nulle part ailleurs sur le plateau —
    un vrai "nouvel arrivant", distinct des Devotion/Éclat (qui visent
    toujours une couleur fixe précise) et de Prisme/Tricolore/
    Complémentaire (qui mesurent la diversité autour de la pose, pas
    sur tout le plateau).
  - Wiring complet (`ModifierId`, `ModifierCatalog`, `ModifierVisualDefaults`
    — abréviations `DI`/`NI`/`SO`/`IM`/`SL`/`RA`/`PF`/`FR`) + tests
    dédiés par modificateur dans `GridManagerModifierTests` (11 tests,
    y compris les cas négatifs pour Nid/Solitaire/Rafale).
- **11 modificateurs de plus, d'un brainstorm fourni cette fois par
  l'utilisateur (14 idées au départ)** — 3 écartées avant implémentation :
  - **Équilibriste** retirée sur demande explicite ("je ne le trouve pas
    bon finalement") après une question de clarification sur ce que
    "chaque côté de la pièce" voulait dire pour une forme non
    rectangulaire.
  - **Longue série** (+1 pt par pose consécutive sans line clear)
    retirée sur demande explicite — trop proche du trait de pièce
    "Spark Tile" déjà existant (même idée de streak sans clear), une
    fois la question posée sur la coexistence des deux.
  - **Solitaire** (+X pts si aucune case de la pièce ne touche une case
    de même couleur) retirée par Claude sans même demander : c'est
    exactement ce que fait déjà le modificateur `Solitaire` ajouté
    juste avant dans ce même lot (mêmes règles de fusion de groupe que
    `FindConnectedGroup`), jusqu'au nom identique.
  - Les 11 restantes, avec les décisions prises pour les points encore
    ambigus dans le texte original (jamais posées comme question,
    documentées ici à la place — même convention que Complémentaire/
    Maçon/Démolisseur en tout début de projet) :
  - **Bridge** (`Pont`, Connexions) : "+X pts pour chaque groupe que
    cette pièce relie à un autre groupe" — interprété comme :
    +15 pts par groupe préexistant fusionné AU-DELÀ du premier (relier
    2 groupes score une fois, 3 groupes deux fois). Nouveau
    `GridManager.CountDistinctPreExistingGroupsTouched` : flood-fill
    depuis chaque voisin de la pièce en excluant les propres cases de
    la pièce (`TryCountNeighborGroup`/`FloodVisitExcludingPiece`, même
    règle de compatibilité couleur/joker que `FindConnectedGroup`),
    pour compter combien de composantes DISTINCTES et déjà existantes
    touchent la pièce — chacune n'est comptée qu'une fois même si
    plusieurs cases de la pièce la touchent.
  - **Encirclement** (`Encerclement`, Voisinage) : +6 pts par case du
    groupe dont les 8 voisins sont tous remplis OU hors de la grille
    (contrairement à Forteresse, qui n'accorde jamais aucun crédit à
    une case de bord/coin — hors-grille échoue toujours son test).
    Choix (jamais posé en question, tranché directement) : 8 directions
    (Moore), pas 4 — "encerclé" au sens propre implique tous les côtés,
    cohérent avec la convention déjà établie par Forteresse.
  - **Sealer** (`Boucher`, Voisinage) : +10 pts par case préexistante
    que CETTE pose fait devenir "encerclée" (voir Encirclement).
    Optimisation clé : toute case déjà remplie voisine d'une case de
    cette pièce ne pouvait PAS être encerclée avant cette pose (ce
    voisin précis était encore vide) — donc si elle qualifie
    maintenant, cette pose vient forcément de la sceller ; pas besoin
    de comparer avant/après.
  - **Big Family** (`GrosseFamille`, Couleurs) : +15 pts quand la
    couleur de cette pose forme EXACTEMENT un seul groupe connecté sur
    tout le plateau — aucune autre case de cette couleur nulle part
    ailleurs.
  - **Repetition** (Roguelike) : +10 pts quand cette pièce a la même
    FORME que la pose immédiatement précédente cette manche. Nouveau
    `GridManager._lastPlacedShapeId`, même pattern que `_lastGroupSize`
    pour Dégradé (capturé avant d'être écrasé, remis à zéro par
    `ResetForNewRound`).
  - **Color Switch** (`AlternancePieces`, Couleurs) : "+X pts si la
    couleur de cette pièce est différente de celle de la pièce
    précédente" — renommé (le nom `Alternance` existe déjà pour le
    modificateur de PATTERN DE LIGNE : 2 couleurs qui alternent sur
    toute une ligne complétée). Même pattern que Repetition, nouveau
    `GridManager._lastPlacedColor`.
  - **Combo** (Destruction) : x2 sur le score TOTAL de cette pose si la
    pose immédiatement précédente cette manche a complété une ligne.
    Seul modificateur qui soit un vrai MULTIPLICATEUR plutôt qu'un
    bonus plat/par-case (sur demande explicite, après une question sur
    l'architecture) — nouveau `PlacementResult.ComboMultiplier`,
    appliqué en dernier dans `TotalScore` (après `GroupMultiplier` et
    `LineClearMultiplier`, qui ne portent chacun que sur une partie du
    score). Réutilise le même signal "la pose précédente a-t-elle
    cleared ?" que Rafale plutôt que de dupliquer le tracking. Stack
    en x4/x8/... si tenu plusieurs fois, même convention que
    `GroupMultiplier`. Côté présentation (`GameBootstrap`), un nouveau
    popup "COMBO x2" rattrape le score affiché après le rattrapage
    existant de `GroupMultiplier`/`LineClearMultiplier`, en
    multipliant tout ce qui a déjà été affiché pour cette pose.
  - **Precision** (Voisinage) : +5 pts par case posée quand CHAQUE
    case de la pièce touche au moins une case préexistante remplie
    (orthogonal seulement, comme le reste du projet).
  - **Overcrowding** (`Surpopulation`, Voisinage) : même chose que
    Precision mais avec un seuil de 2 voisins préexistants minimum par
    case — variante plus stricte, du coup mieux payée (+8/case).
  - **Minimalist** (`Minimaliste`, Voisinage) : +12 pts quand
    l'ENSEMBLE de l'empreinte de la pièce ne touche qu'UNE SEULE case
    préexistante distincte, au total (pas par case) — le juste milieu
    entre Îlot (zéro voisin, groupe isolé) et Precision (un ou plus,
    vérifié par case).
  - **Wildcard** (`Joker`, Roguelike) : "Les Jokers comptent comme la
    couleur qui maximise le bonus de cette pose." Portée décidée
    directement (jamais posée en question) : seulement Devotion (x4
    couleurs) et Éclat (x4 couleurs) — les deux familles où "la couleur
    de cette pose" détermine directement un bonus ; Prisme/Puriste/
    Monochrome/Cercle Chromatique etc., qui ont chacun leur propre
    logique Joker déjà intentionnelle (Puriste les ignore, Monochrome
    les interdit complètement), restent inchangés. N'altère jamais la
    vraie couleur stockée sur la case (`Cell.FilledColor` reste
    `PieceColor.Joker`, donc `FindConnectedGroup` continue à le traiter
    comme un joker pour les poses futures) : `ApplyColorDevotion`/
    `ApplyEclat` sont passés de méthodes lisant `FilledColor`
    directement à des méthodes statiques recevant une couleur déjà
    résolue (`ResolveJokerColorForModifiers`, calculée une fois en
    tête d'`ApplyPreClearModifiers`), qui ne change quoi que ce soit
    que si "Joker" est effectivement tenu.
  - Tests dédiés par modificateur (19 tests au total, y compris les cas
    négatifs) dans `GridManagerModifierTests`.
- **"Lueur" — monnaie façon Balatro, boutique entre les manches, refonte
  complète de la progression méta** (sur demande explicite — "j'aimerais
  qu'on ait un shot avec une currency comme Balatro. Mais il faut penser
  a une manière unique de le représenter et de l'implémenter"). L'ancien
  système (draft de 3 upgrades → pick 1 → draft de 3 modifiers → pick 1,
  gratuit, une seule fois par manche) est entièrement retiré — remplacé
  par une vraie boutique où le joueur achète ce qu'il veut avec une
  monnaie accumulée pendant la partie.
  - **Concept retenu pour la monnaie** (discuté et confirmé avec
    l'utilisateur avant l'implémentation) : contrairement au score, qui
    récompense surtout les gros groupes monochromes (Chaîne/Éclat/le
    bonus de groupe), la Lueur récompense la DIVERSITÉ de couleur d'une
    ligne/colonne complétée — +1 pt pour 1 couleur, +3 pour 2, +6 pour
    3, +10 pour 4 (toutes les couleurs de base à la fois), une
    progression disproportionnée pour que compléter une ligne
    arc-en-ciel se sente comme un jackpot plutôt qu'un simple bonus
    linéaire (`EconomyConstants.LueurByDistinctColors`). Crée une vraie
    tension stratégique avec le score (qui pousse plutôt vers le
    monochrome) sans dupliquer aucun modificateur existant. Calculée
    dans `GridManager.ComputeLueurEarned`, qui réutilise directement
    `ClearedLine.Colors` — la même donnée déjà exposée pour les 8
    modificateurs de pattern de ligne (Arc-en-ciel, Alternance, ...),
    donc aucun nouveau suivi d'état nécessaire côté grille. Exposée via
    `PlacementResult.LueurEarned`, accumulée dans `RunManager.Lueur`
    (persiste pour TOUTE la partie, comme `TotalScore` — jamais remise
    à zéro entre les manches, sur confirmation explicite). Le
    verrouillage du boss (`GridManager.LockFreeCellsAndCheckClears`) en
    rapporte aussi s'il complète une ligne par lui-même, pour rester
    cohérent avec le reste du système de clear.
  - **La boutique** (`RunState.AwaitingShop`, remplace `AwaitingDraft`/
    `AwaitingModifierPick`) : 5 emplacements simultanés — 3 modifiers
    (affichés précisément, "les modifiers sont précis, pas de type") et
    2 upgrades (mystère : seul le `UpgradePool` — Bank ou Grid — est
    visible avant achat, "tout ce que tu sais c'est l'upgrade se situe
    dans quel UpgradePool"). Le joueur achète autant d'emplacements
    qu'il peut se permettre, dans n'importe quel ordre — plus de choix
    forcé "1 parmi 3". Un bouton "Rafraîchir" (nom retenu après
    discussion — l'utilisateur cherchait mieux que "reset") re-tire
    tous les emplacements encore NON vendus contre de la Lueur (un
    emplacement déjà acheté ne change jamais), même prix croissant que
    les achats. Prix croissants à chaque achat de la visite (emplacement
    OU rafraîchissement, sur demande explicite — "prix qui montent à
    chaque achat"), `EconomyConstants.ShopPriceEscalationPerPurchase`
    (+50% par achat déjà fait cette visite), remis à zéro à chaque
    nouvelle ouverture de boutique.
    - Nouveau `Core/Economy/` : `EconomyConstants` (tous les prix/
      valeurs de Lueur, séparé de `ScoringConstants` qui reste focalisé
      sur le score d'une pose) et `ShopSlot` (un emplacement — modifier
      précis OU upgrade mystère avec son `UpgradeDefinition` caché
      jusqu'à l'achat).
    - `RunManager` : `Lueur`, `ShopModifierSlots`/`ShopUpgradeSlots`,
      `GetModifierSlotPrice`/`GetUpgradeSlotPrice`/`GetRerollPrice`,
      `BuyModifierSlot`/`BuyUpgradeSlot`/`RerollShop`/`LeaveShop`.
      `PendingUpgrade`/`PendingUpgradeTileCandidates` suivent un achat
      d'upgrade qui a encore besoin d'un choix de suivi (voir plus bas)
      — tant qu'un achat est en attente, rien d'autre n'est utilisable
      dans la boutique (pas de nouvel achat, pas de rafraîchissement,
      pas de sortie).
  - **Cap de modificateurs : 10** (`EconomyConstants.MaxActiveModifiers`,
    sur demande explicite). Le cap de 5 avait déjà été retiré plus tôt
    dans le projet (uncapped) quand un seul modificateur gratuit par
    manche rendait ça inoffensif ; maintenant que la Lueur permet d'en
    acheter plusieurs par visite sur toute une partie, un plafond
    redevient nécessaire. `BuyModifierSlot` refuse l'achat une fois le
    cap atteint (bouton grisé "Full (10)" côté boutique) plutôt que
    d'exiger un retrait.
  - **Upgrades mystère + choix de tuile** (sur demande explicite) :
    acheter un emplacement d'upgrade révèle immédiatement quel upgrade
    précis se cachait dedans (déjà tiré au sort — pondéré par rareté,
    `UpgradeSystem.RollFromPool` — au moment où l'emplacement a été
    généré, jamais au moment de l'achat) :
    - **Pool Bank** (Retirer/Dupliquer/Recolorer/Joker) : passe par le
      même flux de sous-choix qu'avant (type de pièce, puis couleur
      cible pour Recolorer) — `DraftView`, largement simplifiée (elle
      ne dessine plus les 3 cartes de l'ancien draft, seulement ce
      sous-choix, ouvert directement via `ShowForPendingUpgrade`).
      Joker (sans sous-choix) s'applique immédiatement à l'achat.
    - **Pool Grid** (upgrade de tuile) : au lieu de taguer 3 pièces du
      deck au hasard comme avant, le joueur voit maintenant 5 pièces
      candidates (`EconomyConstants.ShopTileCandidateCount`, nouveau
      `DeckManager.GetCandidateTokenIndices` — même règle de sélection
      que l'ancien tirage aléatoire : préfère les pièces pas encore
      enchantées) et en choisit 3
      (`EconomyConstants.ShopTileChoiceCount`, nouveau
      `DeckManager.TagSpecificTokens`) — nouvelle vue dédiée
      `TileChoiceView`. Le nombre choisi (3) reste identique à l'ancien
      tirage aléatoire pour ne pas changer l'équilibrage, seul le
      hasard devient un choix.
    - `UpgradeSystem` largement réduit : `Apply` ne gère plus que les 4
      upgrades Bank (les 15 upgrades Grid ne passent plus jamais par
      là) ; `RollDraft`/`PickDistinct`/`UpgradeDraft` (classe) et les 15
      constantes `XCount` individuelles supprimés entièrement (plus
      besoin, `ShopTileChoiceCount` unique suffit puisqu'elles valaient
      toutes 3). Nouveaux `RollFromPool`, `GetCandidateTilesFor`,
      `ApplyToChosenTiles`, `TraitKindFor` (table de correspondance
      UpgradeId → PieceTraitKind).
  - **Présentation** : nouvelles `ShopView` (les 5 emplacements + solde
    de Lueur + rafraîchir + bouton "Next round"), `TileChoiceView` (les
    5 candidats, sélection multiple avec confirmation à exactement 3) ;
    `DraftView` réduite au sous-choix seul ; `ModifierDraftView`
    supprimée entièrement (les modifiers ne se piochent plus, ils
    s'achètent). `HudView` affiche désormais la Lueur en permanence
    (coin haut-droit). `GameBootstrap` entièrement rebranché sur le
    nouveau flux (`OnModifierBuyRequested`/`OnUpgradeBuyRequested`/
    `OnRerollRequested`/`OnLeaveShopRequested`/`OnSubChoiceConfirmed`/
    `OnTileChoiceConfirmed`).
  - **Aides de test** : `RunManager.DebugGrantModifier`/
    `DebugGrantLueur` (jamais reliées à un raccourci en jeu, contrairement
    à `DebugForceRoundComplete` qui a F9 — elles n'existent que pour les
    tests EditMode, qui ont maintenant besoin de contourner le tirage
    aléatoire de la boutique et le coût en Lueur pour rester
    déterministes) documentées comme telles. Tests réécrits en
    conséquence dans `RunManagerTests`/`UpgradeSystemTests` (recherche
    de seed bornée, même technique que le test de plateau bloqué, pour
    les scénarios où un emplacement précis doit être Bank ou Grid) plus
    les nouveaux tests dédiés à la boutique (achat, prix croissants,
    rafraîchissement, cap de modificateurs, flux upgrade mystère
    complet) et à la Lueur (`GridManagerTests`).
- **Boutique Lueur — suite de retours de gameplay** (sur demandes
  explicites successives) :
  - Prix de base rééquilibrés (une manche moyenne rapportait ~15 Lueur,
    insuffisant pour acheter quoi que ce soit aux prix d'origine) :
    modifier 20→8, upgrade Bank/Grid 25/40→3 chacun, reroll 15→5
    (`EconomyConstants`).
  - Révélation d'un upgrade d'emplacement mystère : affiche désormais
    nom, rareté+pool et description (`UpgradeCardFactory`, partagée par
    les 3 points de révélation) au lieu d'un simple nom ou de rien du
    tout — sur demande explicite ("il faut pouvoir comprendre
    l'upgrade, on peut réutiliser le visuel qu'on avait avant").
    `DraftView` (sous-choix Bank) et `TileChoiceView` (choix de tuiles
    Grid) l'affichent en en-tête fixe ; nouvelle `UpgradeRevealView`
    pour Joker, le seul cas qui s'appliquait déjà sans jamais rien
    montrer au joueur. Le premier essai réutilisait littéralement la
    carte visuelle bordée de l'ancien draft (bannière de nom, fond
    illustré, hauteur fixe de 234px) mais elle débordait en haut de
    l'écran par-dessus le reste de la boutique — remplacée sur demande
    explicite ("c'est trop gros comme écran, au lieu d'une carte on va
    juste mettre la description en texte blanc") par du texte blanc
    simple sans fond ni bannière, dont la hauteur réelle est calculée
    via `Text.cachedTextGenerator` plutôt que fixée à l'avance.
  - Modificateur "Imminent" retiré entièrement (sur demande explicite) :
    `ModifierId`, `ModifierDefinition`, hook de scoring dans
    `GridManager` (plus les deux méthodes utilitaires de ligne/colonne
    qui n'étaient utilisées que par lui), abréviation d'icône et test
    dédié.
  - Le sélecteur de type de pièce du sous-choix Bank (Retirer/
    Dupliquer/Recolorer) listait tous les types distincts du deck à la
    fois, sans limite — sur demande explicite ("il faut seulement en
    afficher 5"), plafonné à `EconomyConstants.ShopTileCandidateCount`
    comme le choix de tuiles Grid l'était déjà. Nouveau
    `DeckManager.GetCandidateTypes` (même principe que
    `GetCandidateTokenIndices` mais par TYPE plutôt que par jeton
    individuel, puisque ces upgrades agissent sur un type entier) et
    `UpgradeSystem.GetCandidateTypesFor` (filtre en plus Retirer aux
    types que le deck peut encore réellement retirer, via
    `DeckManager.CanRemove`) ; nouveau
    `RunManager.PendingUpgradeTypeCandidates`, tiré une seule fois à
    l'achat et réutilisé tel quel pour toute la durée du sous-choix
    (y compris l'étape couleur de Recolorer, qui n'en a pas besoin
    elle-même). Tests dans `UpgradeSystemTests`/`RunManagerTests`.
  - Titre du nom d'upgrade flou dans la révélation : c'était
    `FontStyle.Bold` sur la police Digitalt (qui n'a pas de vraie
    graisse grasse, donc Unity la simule en redessinant une copie
    décalée — d'où le flou plutôt qu'un vrai gras) — retiré, la taille
    seule porte l'emphase. Les 3 lignes de la révélation (nom,
    rareté+pool, description) sont passées à 1.5x leur taille (sur
    demande explicite) ; la largeur reste fixe pour que la description
    passe sur plus de lignes plutôt que de s'élargir ("le texte de la
    description ne soit pas trop large"). `UpgradeCardFactory.Build`
    positionne désormais ses lignes à la main (plus de
    VerticalLayoutGroup/ContentSizeFitter, qui ne se résolvent qu'au
    prochain passage de layout) pour renvoyer la hauteur réelle du bloc
    de façon synchrone ; `DraftView`/`TileChoiceView` placent le
    titre/la liste en dessous à partir de cette hauteur mesurée plutôt
    qu'un offset fixe deviné.
  - Raccourci éditeur F10 (`UNITY_EDITOR`, même garde que F9) : accorde
    100 Lueur instantanément (sur demande explicite), pour tester la
    boutique sans passer par un vrai clear de ligne.
  - Choix de tuiles Grid — 3 retouches sur demandes explicites
    successives : (1) les descriptions des 15 upgrades Grid ne
    mentionnent plus "3 pieces" (le nombre est un détail de la
    boutique — `EconomyConstants.ShopTileChoiceCount` — pas une
    propriété de l'upgrade elle-même) ; (2) le titre "Choose 3 of 5
    pieces" devient "Select 3 pieces" ; (3) chaque candidat n'affiche
    plus qu'un preview de pièce agrandi (plus de nom en texte), les 5
    côte à côte horizontalement au lieu d'empilés verticalement — en
    plus de prendre beaucoup moins de place à l'écran. Sélectionner un
    candidat fait maintenant apparaître PROGRESSIVEMENT (fondu, ~0.35s)
    un aperçu du vrai badge de trait que l'upgrade donnerait sur ce
    preview, au lieu d'un simple surlignage générique, pour que le
    joueur voie concrètement ce que choisir cette pièce va faire (sur
    demande explicite : "animer le fait qu'on améliore une tuile").
    `ShapePreviewFactory.Build` renvoie maintenant le RectTransform du
    badge qu'il construit (pour permettre ce fondu) ;
    `UpgradeSystem.TraitKindFor` rendue publique pour que la
    Présentation puisse prévisualiser quel trait un upgrade Grid donne
    avant qu'il soit réellement appliqué.
  - Bug signalé : désélectionner une pièce dans `TileChoiceView`
    pendant qu'on survole son badge de trait laissait le tooltip
    affiché — `RebuildPreview` détruit le badge au clic sans jamais
    déclencher `OnPointerExit`. Corrigé à la source dans
    `TraitBadgeView.OnDestroy` (même `Hide()` inconditionnel que
    `OnPointerExit`), donc valable pour n'importe quel badge détruit
    pendant qu'il est survolé, pas seulement ce cas précis.
  - Bug signalé : la popup "You got:" (Joker) se retrouvait mal
    positionnée depuis les changements de hauteur du texte de
    révélation, cachant les emplacements de la boutique derrière elle.
    Cause : `UpgradeCardFactory.Build` ancre son conteneur renvoyé en
    (0.5, 1) (haut-centre) de son parent — ça ne marche que si le
    pivot du parent est LUI AUSSI (0.5, 1) ; celui d'`UpgradeRevealView`
    était resté à (0.5, 0.5), donc avec sa taille par défaut arbitraire
    (100x100, jamais fixée) la carte se retrouvait décalée de 50
    unités sans rapport avec le contenu réel. `DraftView`/
    `TileChoiceView` avaient déjà le bon pivot, d'où le bug limité à
    cette seule vue. Corrigé, et `UpgradeRevealView` a reçu au passage
    le même layout mesuré/centré que `TileChoiceView.LayoutBlock`.
  - **Reroll** (sur demande explicite) : ne touchait auparavant que
    les emplacements encore invendus ("on retire les modifiers et
    upgrades restantes dans la lueur") — un emplacement déjà acheté
    restait marqué "SOLD" pour le reste de la visite. Changé sur
    retour explicite ("mes upgrades et modifiers que j'ai acheté sont
    encore marqué sold, il faut que j'aie tout de disponible") :
    `RerollShop` retire maintenant la garde `!Purchased` et retire les
    5 emplacements systématiquement. Ça ne reprend rien de déjà
    accordé — acheter un emplacement applique déjà le modifier/upgrade
    de façon permanente ; le reroll ne remplace que l'OFFRE affichée
    dans cet emplacement. Test `RerollShop_OnlyReplacesStillUnsoldSlots`
    renommé/réécrit en `RerollShop_ReplacesEverySlot_IncludingAlreadyPurchasedOnes`.
  - **Sous-choix Bank (Retirer/Dupliquer/Recolorer/Joker) — refonte
    complète sur demandes explicites groupées** :
    - Le sélecteur de type (`DraftView.ShowTypeChoice`) n'affiche plus
      que le preview de la pièce (plus de nom/compteur), les
      candidats côte à côte horizontalement — même traitement que
      `TileChoiceView`. Sélection exclusive (un seul type nécessaire,
      style bouton radio) suivie d'un bouton Confirm plutôt qu'un
      effet immédiat au clic ("il faut un confirm au lieu d'un
      immediate effect").
    - **Retirer** : la mention "(floor of 10)" retirée de la
      description ; Confirm déclenche un fondu de sortie (~0.4s) sur
      le preview de la pièce choisie avant de résoudre l'upgrade, pour
      montrer qu'elle quitte le deck.
    - **Dupliquer** : Confirm fait apparaître en fondu (~0.4s) une
      copie de la pièce choisie juste à côté, pour montrer l'ajout,
      avant de résoudre l'upgrade — purement visuel, c'est toujours
      `DeckManager.DuplicateOfType` (déclenché par `ResolveUpgradeSubChoice`
      une fois le fondu terminé) qui ajoute la vraie copie.
    - **Recolorer** : Confirm au premier écran avance simplement vers
      le choix de couleur (pas encore un effet, il manque toujours la
      couleur cible) ; le second écran (choix de couleur) reste
      inchangé.
    - **Joker** : `UpgradeRevealView` affiche maintenant un preview de
      la pièce réellement ajoutée (forme + couleur Joker) sous la
      carte de révélation, plutôt que la description seule. Nouveau
      `DeckManager.AddJoker` renvoie la forme tirée ;
      `UpgradeSystem.ApplyJoker` (remplace le cas Joker dans `Apply`,
      qui ne le gère plus) et `RunManager.LastJokerShapeAdded`
      exposent cette forme jusqu'à la Présentation.
    - Tests : `UpgradeSystemTests` (`ApplyJoker_...`,
      `Apply_JokerPiece_ReturnsFalse_...`),
      `RunManagerTests.BuyUpgradeSlot_Joker_AppliesImmediately_AndSurfacesTheShapeAdded`.
- **Refonte du calcul de la Lueur + animation dédiée** (sur demande
  explicite) :
  - Nouvelle formule : "chaque groupe d'une couleur sur la ligne = 2
    points" (`EconomyConstants.LueurPerColorGroup`), remplace l'ancien
    barème indexé sur le nombre de couleurs DISTINCTES
    (`LueurByDistinctColors`, supprimé). Un "groupe" est une suite
    contiguë de cellules de même couleur au sein d'une ligne clearée —
    même notion que `GridManager.IsAllBlocksOfAtLeastTwo` (le
    modificateur Bloc), pas juste "cette couleur est présente" :
    une ligne monochrome de 8 cases = 1 groupe (2 Lueur) ; une ligne où
    chaque case diffère de sa voisine = 8 groupes (16 Lueur, le
    maximum) ; 3 couleurs mais 4 séquences (ex.
    Coral,Coral,Teal,Teal,Teal,Lime,Lime,Coral) = 4 groupes (8 Lueur),
    pas 3. Les Joker n'ont jamais gagné de Lueur (inchangé), mais une
    suite de Joker coupe quand même la contiguïté entre les groupes de
    part et d'autre au lieu de les fusionner.
  - Nouveau `GridManager.ComputeLueurGroups` (remplace
    `ComputeLueurEarned`) renvoie la liste détaillée des groupes
    (`LueurGroup` : cellules + montant), pas juste le total — nécessaire
    pour l'animation ci-dessous. `PlacementResult.LueurGroups` expose
    cette liste ; `LueurEarned` reste la somme, inchangé pour
    `RunManager.Lueur`.
  - **Animation** (sur demande explicite : "je veux que les points se
    comptent au début du décompte du score", "chaque groupe pulse un a
    la fois", "les points lueur soient progressif et non d'un coup",
    "les points lueur partent du milieu du groupe... et aillent vers
    le texte du score de lueur") : dans
    `GameBootstrap.PlayPlacementSequence`, une nouvelle boucle sur
    `placement.LueurGroups` joue EN PREMIER, avant la cascade de score
    existante. Pour chaque groupe : pulse toutes ses cellules
    d'un coup, calcule le centre du groupe (moyenne des positions
    monde des cellules), fait voler un popup "+N" de ce centre vers le
    label Lueur du HUD (nouveau
    `FeedbackLayer.SpawnFlyingPopup` — accélération plutôt que le
    flottement linéaire de `SpawnPopup`, fondu seulement dans les 30%
    finaux du trajet), puis avance l'affichage du Lueur du HUD
    (nouveau `HudView.SetLueur`, même principe que `SetScores` pour le
    score) — un groupe à la fois, avec une pause entre chaque. Le HUD
    est ramené à sa valeur AVANT la pose juste après `Refresh` (comme
    `SetScores(roundScoreBefore, ...)` le fait déjà pour le score) pour
    que la boucle ait vraiment quelque chose à animer.
  - Tests réécrits dans `GridManagerTests` : ligne monochrome (1
    groupe), ligne alternée (8 groupes), plusieurs séquences de la
    même couleur (compte les séquences, pas les couleurs distinctes),
    séquence de Joker (aucun gain mais coupe la contiguïté).
- **Badges des 4 modificateurs "Glow" (Coral/Teal/Violet/Lime Glow)** :
  montrent maintenant une tuile de leur propre couleur au lieu de leur
  code à 2 lettres opaque (EC/ET/EV/EL) — sur demande explicite
  ("on peut mettre l'icon d'une simple tuile comme icon de modifier,
  ce sera rapidement clair"). Même mécanisme que les badges Forme*
  (`ModifierVisualDefaults.GetSpecialistShape`) : nouveau
  `GetGlowColor` dans `ModifierBadgeFactory.Create`, une tuile Single
  de la bonne couleur via `ShapePreviewFactory.Build` au lieu du label
  texte. Partagé par `ShopView` et `ModifierPanelView` (les deux
  passent par `ModifierBadgeFactory`), donc aucun câblage
  supplémentaire nécessaire. Les 4 modificateurs "Devotion"
  (Coral/Teal/Violet/Lime Devotion) sont une famille différente, non
  concernée — gardent leur code à 2 lettres (OC/OT/OV/OL).
- **Lueur : "par groupe" → "par couleur"** (sur demande explicite,
  revient sur le tout dernier changement) : `GridManager.ComputeLueurGroups`
  groupe maintenant les cellules d'une ligne clearée par COULEUR
  DISTINCTE (toutes les cellules de cette couleur dans la ligne,
  adjacentes ou non), plutôt que par suite contiguë. Une couleur
  éclatée en plusieurs segments non-adjacents ne paie donc plus qu'une
  fois — ex. Coral,Coral,Teal,Teal,Teal,Lime,Lime,Coral (Coral en 2
  segments) : 3 groupes/6 Lueur maintenant, contre 4 groupes/8 Lueur
  avec la formule "par groupe" juste précédente. Une ligne monochrome
  reste 1 groupe (2 Lueur) ; une ligne touchant les 4 couleurs de base
  reste 4 groupes (8 Lueur), que ce soit en segments contigus ou
  éparpillés. `EconomyConstants.LueurPerColorGroup` (toujours 2) et la
  structure `LueurGroup` (toujours utilisée par l'animation —
  `GameBootstrap.PlayPlacementSequence` n'a pas eu besoin de changer,
  la logique de pulse/vol de popup par groupe ne suppose pas la
  contiguïté) sont inchangés dans leur rôle, seul le découpage change.
  Tests réécrits dans `GridManagerTests`.
- **Score de clear de ligne : 12 → 6 points par tuile**
  (`ScoringConstants.LineClearBonusPerCell`, sur demande explicite).
- **Fix : le badge d'upgrade de tuile n'apparaissait jamais sur la
  grille** (sur demande explicite — "j'aimerais qu'on affiche les
  upgrades des tuiles lorsque celles-ci sont sur la grille et que
  lorsque ces tuiles sont cleared, on clear aussi les upgrades
  nécessaire"). La fonctionnalité elle-même existait déjà entièrement
  côté Core (`Cell.OriginTrait`, effacé par `ClearFill`) et Presentation
  (badge en coin de `GridCellView`) — clarifié avec le joueur via
  question directe ("le badge n'apparaît pas du tout en jeu") avant de
  chercher plus loin, ce qui a permis de trouver le vrai bug plutôt que
  de re-livrer une fonctionnalité déjà présente. Cause réelle : les
  upgrades de boutique qui taguent des tuiles existantes
  (`DeckManager.TagSpecificTokens`/`TagRandomTokens`, utilisés par les
  upgrades Bastion/Kamikaze/Golden/Tinted/etc.) ne modifiaient que
  `_deck`, la liste persistante — or `PieceToken` est un `struct`
  immuable, donc taguer l'entrée dans `_deck` ne se répercutait jamais
  sur la copie de ce même jeton déjà présente dans `_drawPile` ou en
  main (`_hand`), contrairement au pattern déjà établi par
  `RemoveOneOfType`/`RecolorOneOfType` qui synchronisent toujours les
  deux. Comme `_drawPile` n'est re-tiré de `_deck` qu'en cas de
  réapprovisionnement (pas à chaque manche) et que la boutique peut
  s'ouvrir alors qu'il reste encore des pièces en main
  (`RunState.AwaitingShop` se déclenche dès que le quota est atteint,
  sans attendre que la main soit vide), une tuile taguée en boutique
  pouvait rester invisible pendant plusieurs manches. Fix : nouvelle
  méthode privée `DeckManager.SyncTagIntoLiveCopy` qui recherche la
  copie vivante correspondante (par forme+couleur+trait d'origine)
  d'abord dans `_drawPile` puis dans `_hand` et la remplace par la
  version taguée ; appelée depuis `TagRandomTokens` et
  `TagSpecificTokens`. Nouveaux tests dans `DeckManagerTests` :
  `TagSpecificTokens_SyncsTheTagIntoLiveHandAndDrawPileCopies_NotJustDeck`
  et `TagRandomTokens_AlsoSyncsTheTagIntoTheHand_NotJustDeck`.
- **Fix : le badge d'upgrade de tuile n'apparaissait TOUJOURS pas** après
  le fix ci-dessus (retour explicite : "Les badges ne s'affichent
  toujours pas"). Cause réelle, cette fois côté rendu pur : dans
  `GridView.CreateCell`, le badge colorblind `BadgeColorIcon` (icône de
  54x54 sans aucun pixel transparent, affiché à 48x48 centré sur la
  cellule) était construit APRÈS les 3 badges de coin
  (`BadgeGolden`/`BadgeTraitOrigin`/`BadgeSpecial`, 16x16 chacun) — en
  UI Unity, un enfant construit plus tard s'affiche PAR-DESSUS les
  précédents, donc ce badge plein et opaque recouvrait entièrement les
  trois autres dès qu'une cellule était remplie d'une couleur ayant une
  icône (les 5 couleurs en ont une). Ce bug préexistait déjà pour
  Golden/Special avant même l'ajout du badge d'origine de trait, mais
  passait inaperçu car ces deux-là ont un texte de secours redondant
  (`EffectLabel`, "+18"/"x2"/"x4", construit en tout dernier donc
  toujours visible) — le badge d'origine de trait, lui, n'a aucun texte
  de secours, ce qui en a fait le premier cas où la perte est
  réellement remarquée par le joueur. Fix : `BadgeColorIcon` est
  maintenant construit en premier (juste après `FillTile`), avant les 3
  badges de coin, qui s'affichent donc désormais correctement par-dessus
  lui. Purement un changement d'ordre de construction dans `GridView.cs`
  (aucun test EditMode possible pour de l'ordre de rendu uGUI — à
  vérifier visuellement en jeu).
- **Badge d'upgrade de tuile : position incohérente + destruction trop
  tôt au clear de ligne** (retour explicite : "dans la slot le badge
  est en haut a gauche, dans le preview de position valide sur la
  grille il est en bas a droite et lorsqu'il est déposé il devient en
  haut a droite. Il faut que ce soit uniform" + "lorsqu'on clear une
  ligne, le badge doit se détruire en même temps que sa tuile, pas au
  début du décomptage de point").
  - Position : les 3 endroits où ce badge apparaît utilisaient chacun un
    coin différent — la pièce en main (`ShapePreviewFactory.BuildTraitBadge`,
    codé en dur en haut-à-gauche), le preview de survol sur la grille
    (`GridCellView.SetHoverTint`, qui réutilisait `_badgeGolden`
    haut-gauche ou `_badgeSpecial` bas-droite selon le type de trait) et
    la tuile réellement posée (`_badgeTraitOrigin`, haut-droite —
    voir GridCellView.ApplyState). Uniformisé sur haut-droite partout :
    `BuildTraitBadge` ancré en `(1,1)`/pivot `(1,1)` comme
    `_badgeTraitOrigin`, et `SetHoverTint` affiche désormais lui aussi
    `_badgeTraitOrigin` (avec sa couleur de rareté) au lieu de
    golden/special — ce qui simplifie au passage la logique de preview
    (plus besoin de distinguer les traits "golden sprite" des autres) ;
    `PieceTraitVisualDefaults.UsesGoldenSprite`, devenu inutilisé,
    supprimé.
  - Timing au clear : `GridCellView.ApplyState` lisait `cell.OriginTrait`
    directement, qui est déjà remis à `null` par `Cell.ClearFill()` dans
    `GridManager.CheckAndClearLines` — AVANT même que la présentation ne
    commence à jouer l'animation de score (`GridView.RefreshHoldingClearedCells`
    tient la ligne visuellement remplie avec `fillColorOverride`
    pendant que le score s'anime, mais ne recevait aucun équivalent
    pour le trait, donc le badge disparaissait au tout début du
    décompte plutôt qu'au moment où `ClearCellVisual` vide vraiment
    cette cellule). Même principe que `ClearedCellColors` : nouveau
    `PlacementResult.ClearedCellTraits` (parallèle à `ClearedCells`,
    capturé dans `GridManager.CheckAndClearLines` juste avant
    `cell.ClearFill()`), propagé par `GridView.RefreshHoldingClearedCells`
    (nouveau paramètre) jusqu'à `GridCellView.ApplyState` (nouveau
    paramètre `originTraitOverride`, suivant la même convention que
    `fillColorOverride` : actif seulement pendant le "hold"). Nouveau
    test `GridManagerTests.PlacePiece_ClearedCellTraits_CapturesEachClearedCellsOriginTraitBeforeWipingIt`.
- **Score de clear de ligne : retour de 6 à 12 points par tuile**
  (`ScoringConstants.LineClearBonusPerCell`, sur demande explicite —
  revient sur le changement précédent "passons de 12 à 6 points par
  tuile").
- **Fix : NullReferenceException dans `TraitBadgeView.OnPointerEnter`**
  (crash report avec stack trace). Cause : en unifiant la position du
  badge de trait sur le coin haut-droite (voir point précédent),
  `GridCellView.SetHoverTint` a été changé pour activer
  `_badgeTraitOrigin` (qui porte le composant `TraitBadgeView`) pendant
  le preview de survol, mais sans jamais appeler `Init()` dessus — seul
  `ApplyState` le fait, une fois le trait réellement posé. Si le
  curseur reste sur ce coin pendant le survol d'un preview, Unity
  déclenche `OnPointerEnter` sur un `TraitBadgeView` dont `_tooltip` est
  encore `null`. Sur demande explicite ("il ne faut pas changer le
  design, probablement juste rajouter une validation") : simple garde
  `if (_tooltip == null) return;` ajoutée en tête de `OnPointerEnter`
  et `OnPointerExit`, sans toucher au comportement du preview lui-même
  (le survol ne montre toujours pas de tooltip, exactement comme avant
  ce point précédent).
- **Joker et Chameleon Tile rejoignent le groupe qui rapporte le plus de
  points** (sur demande explicite : "lorsqu'un joker est posé, il
  devrait être jumelé avec le groupe faisant le plus de points, idem
  pour une tuile caméléon"). Avant, les deux se contentaient du premier
  voisin réel trouvé selon un ordre de balayage fixe (gauche, droite,
  bas, haut) :
  - **Joker** : `GridManager.FindConnectedGroup`/`PreviewGroup` ne
    verrouillaient plus dynamiquement leur couleur d'ancrage sur la
    première couleur réelle rencontrée pendant le flood-fill (ordre
    arbitraire dépendant du DFS) — désormais, quand la cellule de
    départ est Joker, `FindCandidateAnchorColors` explore d'abord tout
    l'amas de cellules Joker transitivement connectées et recense
    CHAQUE couleur réelle distincte à sa frontière, puis
    `ResolveBestJokerGroup` fait un flood-fill séparé pour chaque
    candidate (ancrage fixé dès le départ, plus de verrouillage
    dynamique) et garde celui dont le score estimé
    (`EstimateGroupScore` : bonus de groupe + bonus doré, multiplié
    comme `PlacePiece` le ferait réellement) est le plus élevé. Sans
    aucune couleur réelle atteignable, comportement inchangé (l'amas de
    Joker forme son propre groupe). `FindConnectedGroup` (placement
    réel) et `PreviewGroup` (aperçu au survol, utilisé par `GridView`)
    partagent maintenant la même logique via un nouveau
    `FloodFillGroup` commun, donc l'aperçu au survol montre déjà le
    groupe qui sera réellement choisi.
  - **Chameleon Tile** : `RunManager.ResolveChameleonColor` recense
    maintenant les couleurs réelles distinctes parmi les 4 voisins
    orthogonaux de la cellule enchantée (au lieu de s'arrêter à la
    première), puis compare leur score via le nouveau
    `GridManager.PreviewGroupScore(shape, couleur, x, y)` (wrapper
    public autour de `PreviewGroup` + `EstimateGroupScore`, pour ne pas
    exposer directement le helper de score privé de GridManager) et
    choisit la couleur qui rapporterait le plus. Comportement de repli
    inchangé si aucun voisin n'est encore rempli (garde sa propre
    couleur).
  - Nouveaux tests :
    `GridManagerTests.PlacePiece_JokerJoinsWhicheverAdjacentGroupScoresTheMost`
    et
    `RunManagerTests.PlacePiece_ChameleonTrait_PicksTheNeighborColorThatScoresTheMost`
    (voisin de gauche = petit groupe isolé, premier dans l'ancien ordre
    de balayage ; voisin de droite = groupe de 3 cellules déjà
    connectées — les deux vérifient que le résultat rejoint bien le
    plus gros groupe, pas le premier trouvé).
- **24 modifiers convertis de bonus fixe (+X pts) vers multiplicateur
  (xN)** (sur demande explicite : "j'aimerais qu'on utilise plus de
  multiplicateur dans les modifiers, peut-être en changer pour changer
  de point vers multiplicateur" — question de suivi sur l'ampleur
  répondue "une bonne partie" avec "remplace entièrement" plutôt que
  cumuler bonus+multiplicateur). Choix des 24 (sur ~45 additifs) :
  uniquement les bonus CONDITIONNELS ONE-SHOT (au plus une fois par
  pose, ou une fois par LIGNE cleared) — Prisme, Architecte, Puriste,
  Tricolore, Complémentaire, Îlot, Maçon, Démolisseur, Dégradé,
  Solitaire, Espace Libre, Rafale, Pont, Grosse Famille, Repetition,
  Alternance des pièces, Minimaliste, et les 6 modifiers "par ligne"
  (Arc-en-ciel, Alternance, Palindrome, Gradient, Bloc, Monochrome
  Ligne). Gardés en +X pts : tout ce qui scale déjà avec la taille du
  groupe/de la pièce (Forteresse, Prisonnier, Couronne, Carrefour,
  Contraste, Emmitouflée, Jardinier, Cercle Chromatique, Monochrome,
  Collectionneur, Diagonale, Nid, Encerclement, Boucher, Éclat x4,
  Grand/Petit/Hors-Norme Format, Precision, Surpopulation,
  Chaîne/Méga-chaîne) — un multiplicateur qui grossirait avec le
  nombre de cellules aurait un plafond totalement imprévisible.
  Facteurs : xN mostly = x2 (même convention que Devotion/Forme/Slot
  qui doublent déjà), x3 pour les plus rares/puissants (Prisme, Rafale,
  Puriste — qui était "+50% des points du groupe", devient un x3 propre
  plutôt qu'un ×1.5 cousu à la main).
  - Nouveau `PlacementResult.ModifierMultiplier` (parallèle à
    `GroupMultiplier`/`LineClearMultiplier`/`ComboMultiplier`, stacke
    multiplicativement comme eux) ; `TotalScore` refactorisé en
    `Chips * Mult` (nouvelles propriétés `Chips`/`Mult`, exactement le
    split Balatro "score de base" / "multiplicateur final" —
    prépare le prochain point sur l'affichage façon Balatro).
  - `GridManager.ApplyPreClearModifiers`/`ApplyPostClearModifiers`
    renvoient maintenant aussi un `out int modifierMultiplier` (stack
    multiplicatif de chaque modifier converti qui a fait mouche cette
    pose), en plus du bonus additif existant (`total`) pour les
    modifiers non-convertis. Les 24 méthodes `ApplyXxx` correspondantes
    renvoient désormais un FACTEUR (1 = no-op, N = déclenché) au lieu
    d'un bonus (0 = no-op, N = déclenché) ; `ApplyPerLineBonus` (les 6
    modifiers de ligne) renommée `ApplyPerLineMultiplier` avec la même
    logique.
  - Nouveau `ScoreEventType.ModifierMultiplier` (Amount = le facteur,
    pas des points) pour que la présentation distingue un événement
    "+X points" d'un événement "×N".
  - `GameBootstrap.PlayPlacementSequence` : chaque événement
    `ModifierMultiplier` pulse le badge du modifier concerné avec un
    popup "xN" immédiat (feedback par-modifier, comme avant pour les
    bonus fixes), mais son montant n'est PAS ajouté directement au
    score affiché (ce serait faux : c'est un facteur, pas des points) —
    un unique rattrapage combiné après la boucle (même mécanique que
    les rattrapages `GroupMultiplier`/`ComboMultiplier` déjà en place)
    applique `placement.ModifierMultiplier` sur tout le sous-total
    affiché jusque-là.
  - Fix collatéral découvert en cours de route :
    `RunManager.CountModifierUsage` (stat "utilisé N fois" du tooltip)
    ne comptait que les événements `ScoreEventType.Modifier` — les 24
    modifiers convertis auraient donc arrêté d'incrémenter ce compteur
    silencieusement. Corrigé pour compter aussi
    `ScoreEventType.ModifierMultiplier`. Nouveau test
    `RunManagerTests.GetModifierUsageCount_AlsoIncrements_ForAModifierMultiplierEvent`.
  - Descriptions des 24 modifiers dans `ModifierCatalog` réécrites de
    "+X pts" vers "xN multiplier".
  - Tests : les ~45 assertions de `GridManagerModifierTests` touchant
    ces 24 modifiers réécrites (`ModifierBonus`→`ModifierMultiplier`,
    valeurs attendues recalculées), y compris les tests "ne se
    déclenche pas" qui vérifiaient auparavant `ModifierBonus == 0` —
    une assertion devenue vide de sens pour un modifier qui n'écrit
    plus jamais dans `ModifierBonus`, corrigée en
    `ModifierMultiplier == 1`.
- **Objectifs de score (`RunConfig.Quotas`) : progression exponentielle**
  (sur demande explicite, en accompagnement direct du point précédent —
  "je te laisse faire monter les objectifs de points en exponentiels").
  L'ancienne progression (300, 450, 650, 900, 1200, 1550, 1950, 2500)
  était grosso modo quadratique (écarts croissant arithmétiquement :
  +150, +200, +250...) — remplacée par une vraie progression
  géométrique (~x1,7 par manche) : 300, 500, 850, 1450, 2450, 4150,
  7050, 12000. Nécessaire maintenant que plusieurs des 24 modifiers
  convertis en multiplicateur peuvent s'empiler sur une même pose
  (multiplicativement avec `GroupMultiplier`/`ComboMultiplier` en plus)
  et faire largement exploser les anciens objectifs de fin de run.
- **Décompte du score façon Balatro (chips bleus x multiplicateur
  rouge)** (sur demande explicite, avec capture d'écran de Balatro à
  l'appui — "le bleu est le score et le rouge le multiplicateur").
  - Nouvelles propriétés `PlacementResult.Chips`/`.Mult`, qui
    reprennent exactement le split Balatro "score de base" /
    "multiplicateur" : `Chips` = tout ce qui est additif (groupe, doré,
    ligne clearée, bonus des modifiers non-convertis, trait), avec les
    facteurs PAR-CELLULE (`GroupMultiplier`/`LineClearMultiplier`,
    Tinted/Multiplier Zone) déjà appliqués dedans ; `Mult` =
    `ModifierMultiplier * ComboMultiplier`, les deux seuls facteurs qui
    s'appliquent à TOUTE la pose. `TotalScore` devient simplement
    `Chips * Mult`.
  - `ComboView` (l'ancien simple texte "Combo: +N" entre la grille et
    la main) refondu en deux pastilles arrondies côte à côte — bleue
    pour les chips, rouge pour le multiplicateur — en réutilisant les
    sprites de bouton déjà présents dans le pack "Colorful UI"
    (`blueButton`/`red_btn`, via `UISprites.ChooseButtonBackground`/
    `CancelButtonBackground`) plutôt que de dessiner un nouvel élément
    d'art. `Show(int chips, int mult)` remplace l'ancien `Show(int
    amount)` ; nouveau `PulseMult()` qui ne fait rebondir QUE la
    pastille rouge (distinct de `Pulse()` sur tout le bloc), pour que
    le moment où le multiplicateur augmente se voie clairement,
    séparément d'un simple gain de chips.
  - `GameBootstrap.PlayPlacementSequence` reconstruit `chipsTotal`/
    `multTotal` progressivement pendant toute la séquence (au lieu
    d'un seul total agrégé "Combo" comme avant) : chaque `ScoreEvent`
    (hors `LineClear`/`ModifierMultiplier`, déjà traités à part) et
    chaque cellule de ligne clearée alimentent `chipsTotal` ; le
    rattrapage `GroupMultiplier`/`LineClearMultiplier` alimente aussi
    `chipsTotal` (ces facteurs vivent côté Chips dans le split Core,
    pas côté Mult) ; les rattrapages `ModifierMultiplier` et Combo
    alimentent `multTotal` et déclenchent `PulseMult()`. Invariant
    vérifié à la main : `chipsTotal * multTotal` égale toujours le
    sous-total déjà affiché à chaque étape, exactement comme `Chips *
    Mult == TotalScore` côté Core.
- **Coloration des mots "points"/"multiplicateur" dans les descriptions**
  (sur demande explicite : "à chaque fois que le mot point apparait
  dans les description, que le mot soit bleu et idem pour le rouge et
  le multiplicateur"). Nouveau `DescriptionTextFormatter.Colorize`
  (Presentation) : découpe la description mot par mot (pas de regex,
  cohérent avec le reste du projet) et entoure chaque occurrence de
  "pts"/"pt"/"point"/"points" d'un tag `<color=#65AED6>` (bleu,
  `UITheme.ButtonSelected`) et chaque occurrence de
  "multiplier"/"multipliers" ou du facteur littéral lui-même
  ("x2", "x3"...) d'un `<color=#B56D7F>` (rouge, `UITheme.Danger`) —
  mêmes couleurs que les nouvelles pastilles chips/mult de `ComboView`
  ci-dessus, pour une convention bleu=score/rouge=multiplicateur
  cohérente dans tout le jeu. Repose sur le support rich-text natif du
  composant `Text` d'Unity (`<color>`, actif par défaut, jamais
  désactivé dans `UIFactory`), donc aucun changement de rendu requis.
  Reconnaît uniquement les mots littéraux, pas tous les synonymes
  ("doubles", "+18 flat" restent non colorés) — une interprétation
  volontairement littérale de la demande.
  - Branché aux 3 seuls endroits de tout le projet qui affichent une
    description telle quelle : `ModifierBadgeView` (tooltip de
    modifier), `TraitBadgeView` (tooltip de trait de pièce) et
    `UpgradeCardFactory` (carte d'upgrade, partagée par la boutique, le
    choix de tuiles et la révélation Joker).
- **`ComboView` (suite) : pulse séparé sur chips/mult + total du
  placement affiché au-dessus** (sur demande explicite : "faisons
  pulse le texte pour point et mult lorsque ceux-ci sont augmenté" +
  "on peut rajouter en dessous ou en haut de ces deux chiffres le
  score total du placement").
  - `ComboView.Pulse()` (rebond sur tout le bloc) supprimée —
    remplacée entièrement par `PulseChips()`/`PulseMult()`, chacune ne
    faisant rebondir QUE sa propre pastille. `PulseChips()` est
    maintenant appelée à chaque fois que `chipsTotal` augmente
    réellement dans `GameBootstrap.PlayPlacementSequence` (boucle des
    `ScoreEvent`, boucle des cellules de ligne clearée, et le
    rattrapage `GroupMultiplier`/`LineClearMultiplier`, qui vit côté
    chips — voir le point précédent), au même titre que `PulseMult()`
    était déjà appelée aux rattrapages `ModifierMultiplier`/Combo.
  - Nouveau texte `TotalText` au-dessus de la rangée chips/mult,
    affichant `chips * mult` (le score total de la pose en cours,
    identique à ce que `HudView` finira par refléter dans le score de
    manche cumulé une fois la séquence terminée) — mis à jour à chaque
    appel de `Show(chips, mult)`, donc suit exactement le même rythme
    progressif que les deux pastilles. `ComboView.Build` réorganisé en
    `VerticalLayoutGroup` (total en haut, rangée chips×mult en bas) au
    lieu du simple `HorizontalLayoutGroup` d'avant.
- **Chameleon Tile recolore aussi son entrée dans le deck** (idée du
  joueur : "si une pièce est recolorée, elle est recolorée dans le deck
  aussi (on garde l'upgrade sur la pièce recolorée)"). Jusqu'ici, la
  couleur dynamiquement résolue par Chameleon (voir le point plus haut
  sur le choix du groupe le plus payant) ne s'appliquait qu'à LA POSE —
  le jeton d'origine dans `_deck` gardait sa couleur de départ pour
  toujours, donc à chaque nouveau tirage de ce même jeton, Chameleon
  repartait de zéro sans "mémoire" de la dernière couleur adoptée.
  - Nouveau `DeckManager.RecolorHandToken(int handIndex, PieceColor
    newColor)` : recolore le jeton de CET emplacement de main précis
    (pas de recherche par shape/couleur ambiguë comme
    `RecolorOneOfType`, qui en plus perd le trait) et propage le même
    changement vers l'entrée correspondante dans `_deck` — trait
    conservé intact. `RunManager.PlacePiece` l'appelle juste après
    `ResolveChameleonColor`, uniquement quand la couleur résolue
    diffère réellement de la couleur d'origine du jeton (aucun-op si
    Chameleon garde sa propre couleur faute de voisin).
  - Résultat : la prochaine fois que ce même jeton est retiré du deck
    et posé sans voisin rempli à côté, il prend par défaut la DERNIÈRE
    couleur qu'il a effectivement adoptée plutôt que sa couleur de
    départ — Chameleon continue bien sûr de préférer un voisin rempli
    quand il y en a un.
  - Nouveaux tests : `DeckManagerTests.RecolorHandToken_UpdatesTheHandSlotAndItsOwnDeckEntry_KeepingTrait`,
    `DeckManagerTests.RecolorHandToken_NoOp_WhenSlotIsEmpty`, et
    `RunManagerTests.PlacePiece_ChameleonTrait_AlsoRecolorsItsOwnDeckEntry_KeepingItsTrait`.
- **Badges des 4 modificateurs "Devotion" (Coral/Teal/Violet/Lime
  Devotion)** : montrent maintenant eux aussi une tuile de leur propre
  couleur, au lieu de leur code à 2 lettres opaque (OC/OT/OV/OL) — sur
  demande explicite ("Violet devotion manque le preview single piece comme
  icon de modifier"). Une entrée précédente de ce journal disait
  Devotion "non concerné" par ce traitement — corrigé ici : Devotion et
  Glow/Éclat sont en fait la même sorte de modificateur "par couleur" et
  se lisent exactement pareil (une vraie tuile colorée plutôt qu'un code
  opaque), donc `ModifierVisualDefaults.GlowColors`/`GetGlowColor` ont été
  renommés en `ColorTileColors`/`GetColorTileColor` et étendus avec les 4
  entrées Devotion en plus des 4 Éclat existantes.
  `ModifierBadgeFactory.Create` appelle `GetColorTileColor` au lieu de
  l'ancien `GetGlowColor`. Les abréviations OC/OT/OV/OL restent définies
  dans `Abbreviations` mais ne sont plus utilisées pour ces 4 IDs
  (même précédent que EC/ET/EV/EL, gardées comme donnée de repli
  inoffensive).
- **3 modificateurs progressifs** (demande explicite : "Il manque
  d'upgrade progressif (+5 ou x1 pour chaque pièce d'un même type de
  suite, +10 ou x2 pour la 2e de suite, etc...) (x1 par modifiers possédé)
  (x0.1 par tuile sur la grille) On peut soit adapter des modifiers déjà
  présent ou en faire des nouveaux") — jusqu'ici, chaque modificateur
  avait une force FIXE (toujours xN, ou toujours +X pts). Ces 3
  modificateurs scalent plutôt avec un compteur qui grandit pendant la
  partie, sur le même modèle multiplicateur (xN) que la conversion
  Balatro de cette session :
  - **Répétition (adaptée)** : n'était qu'un x2 plat quand la pièce posée
    avait la même forme que la précédente. Devient progressive :
    `GridManager` garde maintenant `_repetitionStreak`, la longueur de la
    série de poses consécutives de même forme (this-round), mise à jour à
    CHAQUE pose (que Répétition soit possédée ou non, comme
    `_lastPlacedShapeId`/`_lastGroupSize`). Le multiplicateur devient
    directement cette longueur : x1 (aucun bonus) à la 1ère pose d'une
    série, x2 à la 2e forme identique d'affilée, x3 à la 3e, etc. — sans
    plafond, comme les autres modificateurs "stacking" du jeu (Démolisseur,
    Arc-en-ciel...). L'ancienne constante `ScoringConstants.RepetitionMultiplier`
    (toujours 2) a été retirée, la valeur venant maintenant directement du
    compteur.
  - **Synergie (nouveau, `x1 par modifiers possédé`)** : xN où N est le
    nombre TOTAL de modificateurs actuellement possédés (elle-même
    incluse, et chaque copie compte séparément si le joueur en possède
    plusieurs exemplaires — comme "posséder 2x le même modificateur
    double son effet" déjà établi pour les autres). Lue directement depuis
    `activeModifiers.Count` dans `GridManager.ApplyPreClearModifiers`
    (déjà disponible, aucun nouvel état à tracker) — récompense
    directement le fait d'accumuler des modificateurs, effet roguelike
    "boule de neige" volontaire.
  - **Densité (nouveau, `x0.1 par tuile sur la grille`)** : xN où N est le
    nombre de cases remplies sur la grille (après la pose et ses clears
    éventuels) divisé par 10, arrondi à l'entier inférieur — donc
    mathématiquement identique à "+0.1x par tuile" mais en gardant un
    multiplicateur entier plutôt que d'introduire des fractions dans tout
    le système de score (`PlacementResult.ModifierMultiplier`,
    `ScoreEvent.Amount`, l'affichage "xN" du pill mult, etc. sont tous des
    `int`). Opposé thématique d'Espace Libre (qui récompense un plateau
    presque vide) — réutilise le même scan `AllPositions()`/`IsFilled`.
    Ne se déclenche pas tant que moins de 10 cases sont remplies.
  - Les 3 sont catégorie Roguelike, enregistrés dans
    `ModifierCatalog.All` (donc piochables normalement dans le draft, rien
    d'autre à câbler). Nouveaux tests :
    `GridManagerModifierTests.Repetition_MultiplierGrowsWithConsecutiveSameShapePlacements`,
    `.Synergie_MultipliesByTheTotalNumberOfModifiersHeld`,
    `.Densite_MultiplierGrowsWithHowManyCellsAreFilledOnTheBoard`.
- **Slot N Loyalty (SlotUn/Deux/Trois) : "tout doubler" au lieu de
  "doubler le group bonus"** (demande explicite : "Les slots loyalty
  modifier au lieu de double group placement, on va tout doubler") —
  jusqu'ici, `RunManager.ApplyHandSlotModifierBonus` doublait
  spécifiquement `PlacementResult.GroupBonus` en le rajoutant une 2e fois
  dans `ModifierBonus` (un event `ScoreEventType.Modifier`). Devient un
  vrai x2 sur le score ENTIER de la pose : la méthode multiplie
  maintenant directement `placement.ModifierMultiplier` (le même champ
  que les modificateurs xN de GridManager utilisent) et émet un event
  `ScoreEventType.ModifierMultiplier` à la place — donc `Chips * Mult`
  double bien tout (bonus de groupe, golden, ligne clearée, bonus des
  AUTRES modificateurs...), pas seulement le bonus de groupe. Le garde-fou
  `placement.GroupBonus <= 0` a aussi été retiré : ça n'a plus de sens de
  conditionner un doublement "de tout" sur la valeur d'UNE seule
  composante. Nouvelle constante `ScoringConstants.SlotLoyaltyMultiplier`
  (2) remplace l'ancien doublement implicite. Tests mis à jour dans
  `RunManagerTests.cs` (`SlotUn_MultipliesEntireScore_WhenPlacingFromHandSlotZero`,
  `AssertSlotFiresOnlyForHandIndex`) pour vérifier `ModifierMultiplier`
  au lieu de `ModifierBonus`.
- **Gradient devient un modificateur PERMANENT** (demande explicite :
  "Gradiant modifier est tellement difficile a faire que je pense que ça
  devrait plus être genre: Ajoute x1 a ton multiplier pour toutes les
  round a chaque fois que tu réussi a accomplir le modifier. Ne se reset
  jamais.") — jusqu'ici, Gradient était un des 6 modificateurs "de ligne"
  génériques (`ApplyPerLineMultiplier`) : xN par ligne clearée qualifiante
  (aucune paire de cases adjacentes de la même couleur), remis à zéro à
  chaque pose comme ses 5 cousins (Arc-en-ciel, Alternance, Palindrome,
  Bloc, Monochrome-ligne).
  - Gradient sort de ce mécanisme partagé et obtient sa propre méthode
    `GridManager.ApplyGradient`, adossée à un nouveau compteur d'instance
    `_gradientPermanentBonus` (int) — **jamais remis à zéro**, y compris
    par `ResetForNewRound` (délibérément exclu, contrairement à TOUS les
    autres compteurs de la classe). Chaque ligne clearée qui satisfait la
    condition de Gradient l'incrémente de +1, pour toujours (+2 si 2
    lignes qualifiantes clearent en même temps, etc.).
  - Le multiplicateur retourné à CHAQUE pose (même celles qui ne clearent
    aucune ligne) est désormais `1 + _gradientPermanentBonus` — donc x1
    tant que Gradient n'a jamais encore été accompli ce run, x2 dès le
    tout premier succès (appliqué immédiatement, sur cette même pose), x3
    au 2e succès (potentiellement plusieurs runs/manches plus tard), etc.,
    sans jamais redescendre.
  - `GridManager` crée une seule instance de `GridManager` par run (dans
    le constructeur de `RunManager`), donc "ne se reset jamais" veut
    concrètement dire : survit à `ResetForNewRound` (changement de
    manche) mais repart bien de 0 sur un tout nouveau run (New Run crée
    un nouveau `RunManager`/`GridManager`).
  - Ancienne constante `ScoringConstants.GradientMultiplierPerLine`
    (toujours 2) retirée, plus aucun besoin d'une valeur fixe par ligne
    puisque le pas est toujours de +1 par construction ("ajoute x1").
  - Nouveau test `GridManagerModifierTests.Gradient_PermanentlyGrowsAndNeverResets_EvenAcrossRounds`
    couvrant : no-op avant le premier succès, application immédiate à la
    pose qui vient de déclencher Gradient, application à une pose
    suivante qui ne cleare rien du tout, et survie à `ResetForNewRound`.
- **Icons des 9 premiers modificateurs** (le joueur a déposé les PNG
  lui-même : "J'ai fait un icon pour les 9 premier modifiers, je les ai
  nommé par leur nom dans le dossier
  Assets/Resources/Icons/Modifiers") — Prisme, Chaîne, Méga-chaîne,
  Forteresse, Prisonnier, Architecte, Puriste, Collectionneur et
  Tricolore montrent maintenant leur propre icône au lieu du chip
  coloré + abréviation à 2 lettres, chaque PNG étant nommé d'après le
  `Name` anglais du modificateur sans espace (ex. "Mega Chain" →
  `MegaChain.png`).
  - Nouveau `ModifierVisualDefaults.Icons`/`GetIcon(ModifierId)` : même
    mécanisme lazy-au-démarrage que `VisualDefaults.IconMap`
    (`Resources.Load<Sprite>("Icons/Modifiers/...")`, `null` = pas encore
    d'icône pour ce modificateur plutôt qu'une exception) — exactement le
    point d'extension que le commentaire de classe annonçait déjà
    ("Swapping in real per-modifier art later only needs a sprite lookup
    added alongside GetAbbreviation").
  - `ModifierBadgeFactory.Create` vérifie `GetIcon` EN PREMIER, avant les
    2 exceptions existantes (silhouette Forme*, tuile colorée
    Éclat/Devotion) et avant le fallback abréviation — donc une icône
    réelle gagne toujours quand elle existe. Même cadrage que les autres
    previews (80% de la taille du badge, centré).
  - Les ~60 autres modificateurs n'ont pas encore d'art et continuent de
    montrer leur chip/abréviation comme avant — ajouter une icône plus
    tard ne demande qu'une entrée de plus dans `Icons`.
- **`TooltipView` : ne cache plus jamais l'icône survolée + hauteur
  dynamique** (demande explicite : "Le tooltip est dans le chemin et
  moche. Je propose qu'il ne soit jamais pas dessus l'icon qu'on est en
  train d'essayer de comprendre. Aussi, j'aimerais que la hauteur du
  tooltip soit dynamique pour qu'il 'fit' avec la longueur du texte") —
  deux problèmes distincts, corrigés ensemble dans `TooltipView.cs` :
  - **Recouvrement** : `PositionNear` décalait le panneau d'une simple
    marge fixe (16px) depuis le CENTRE de l'icône survolée (badge
    `anchor.position`) — pour une icône de ~40-56px, ce décalage plaçait
    le coin du tooltip encore À L'INTÉRIEUR de l'icône. Le calcul tient
    maintenant compte de la taille réelle de l'ancre (`anchor.rect`) : le
    tooltip démarre entièrement à DROITE de l'icône (bord droit de
    l'icône + marge), et bascule à GAUCHE si la place manque à droite —
    pour ne jamais se faire repousser par-dessus l'icône par l'ancien
    clamp de bord d'écran.
  - **Hauteur dynamique** : la hauteur du panneau (et de la zone de
    description) n'est plus la constante fixe `Height = 150f` mais
    calculée à chaque `Show()` via `Text.preferredHeight` de la
    description — `UIFactory.CreateText` met déjà `horizontalOverflow =
    Wrap` sur tout texte créé, donc `preferredHeight` reflète le
    wrapping réel à la largeur fixe du panneau (300px). La largeur reste
    fixe (seule la hauteur devait "fit"), et est réglée une seule fois
    au `Build()` pour que le tout premier `Show()` mesure déjà avec la
    bonne largeur.
- **Cartes de modificateurs du shop : nom + description sur la carte,
  fond coloré retiré** (demande explicite : "dans le shop, les 'cartes'
  de modifiers on peut rajouter le nom en haut de l'icon et sa
  description sous son icon. Peux-tu enlever le carré coloré derrière
  l'icon aussi?") — jusqu'ici une carte modificateur du shop ne montrait
  que le badge (chip coloré + icône/abréviation) et le bouton Acheter ;
  nom et description n'apparaissaient que dans le tooltip au survol.
  - `ModifierBadgeFactory.Create` gagne un paramètre optionnel
    `showBackground = true` : à `false`, ni le chip category-coloré ni
    son contour ne sont créés — seule l'icône/silhouette/tuile/
    abréviation brute reste. Les autres appelants (panneau latéral
    persistant) ne passent pas ce paramètre et gardent leur chip coloré
    inchangé.
  - `ShopView.BuildModifierCard` ajoute un label `Name` (16pt) juste
    au-dessus de l'icône et un label `Desc` (12pt, coloré via
    `DescriptionTextFormatter.Colorize` comme partout ailleurs) juste en
    dessous, et appelle `ModifierBadgeFactory.Create(..., showBackground:
    false)`.
  - **Hauteur de carte dynamique en 2 passes** (même philosophie que le
    fix du tooltip juste au-dessus, et même technique que
    `UpgradeCardFactory.PreferredHeight` déjà utilisée ailleurs dans le
    projet) : les descriptions vont de ~30 à ~210 caractères selon le
    modificateur (ex. Répétition/Gradient, les 2 plus récents, sont les
    plus longues), donc une hauteur fixe aurait soit gâché de l'espace
    soit fait déborder le texte par-dessus le bouton Acheter. Passe 1 :
    `BuildModifierCard` construit chaque carte et mesure la hauteur
    naturelle (wrappée) de SA propre description sans dépendre de l'ordre
    d'exécution. Passe 2 : `BuildModifierCards` applique la plus grande
    hauteur trouvée à TOUTES les cartes de la rangée (et à leur zone de
    description), pour que le bouton Acheter reste toujours à la même
    hauteur peu importe quels 3 modificateurs sont actuellement proposés.
  - Les cartes d'upgrade (mystery box) ne sont pas concernées — elles
    gardent leur `CardHeight` fixe existante (elles n'ont ni nom ni
    description avant achat).
- **2 corrections suite au screenshot des nouvelles cartes de shop**
  (demande explicite : "Il y a des overlaps entre modifiers et
  upgrades" + "Dans le modifier Repetition XN devrait être en rouge.
  Normalement X et N collé ne devrait pas arriver dans un mot normal,
  on peut créer une règle pour toutes les description. Aussi le N
  devrais être en minuscule") :
  - **Overlap modifiers/upgrades** : la section "Upgrades" avait un Y
    fixe (`-344f`) calculé pour l'ancienne hauteur fixe des cartes
    modificateur (200px) — une fois les cartes rendues dynamiques (voir
    entrée précédente), une description longue (ex. Répétition, la plus
    longue du catalogue) pousse la rangée bien plus bas que ça, et la
    section Upgrades se retrouvait recouverte. `ShopView.Refresh`
    récupère maintenant la hauteur réelle retournée par
    `BuildModifierCards` et repositionne `_upgradeSectionLabel`/
    `_upgradeCardsContainer` juste en dessous à chaque refresh, au lieu
    d'un offset figé.
  - **"xN" non coloré + minuscule** : `DescriptionTextFormatter.
    IsMultiplierFactor` ne reconnaissait que "x" + chiffres ("x2", "x3"...)
    comme token de multiplicateur, pas le placeholder "xN" utilisé dans
    les 4 descriptions progressives (Répétition, Gradient, Synergie,
    Densité). Étendu pour aussi reconnaître "x" + exactement "n" — sur
    l'observation du joueur que "x" collé à une lettre n'arrive jamais
    dans un mot anglais normal, donc sans risque de faux positif. En
    parallèle, les 4 descriptions concernées remplacent tous leurs "N"
    (dans "xN" et dans "where N is...") par un "n" minuscule.
