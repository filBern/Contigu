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
