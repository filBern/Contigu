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
