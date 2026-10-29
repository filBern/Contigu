# QA 2026-10-29 : passe complète (Core, boutique sur étagères, sauvegarde, textes, docs, Presentation)

Branche `claude/blissful-brahmagupta-u0ywu2`, HEAD `c8bc996`. Le Core est testé en vrai (.NET). Unity n'a pas tourné : tout ce qui touche la Presentation est « d'après le code », sauf les captures du build WebGL `QaBuild/` (voir réserve sur son âge plus bas).

## 1. Résumé

### Exécuté

| Passe | Commande | Résultat |
|---|---|---|
| Tests | `cd Tools/Headless/Tests && dotnet test` | 624 passent, 0 échec (621 au rapport précédent, +3 tests du dernier commit) |
| Contenu | `python3 Tools/qa/content_audit.py` | 0 trouvaille (145 fertilizers, 72 sprouts, 76 traits, 16 ennemis, 23 météos) |
| Glyphes | `python3 Tools/qa/glyph_audit.py` | « No missing glyphs » |
| Icônes | `python3 Tools/check_icon_tags.py` | 5 écarts (voir C3), sortie en erreur |
| Fuzz random | `--runs 100 --policy random --challenge <défi> --check`, 6 défis, seed 1 ; puis `--runs 150`, seeds 11 et 23 | aucune ligne `invariant:` ni `exception:` |
| Fuzz planner | `PLAN_DEPTH=2 --runs 40 --policy planner --challenge <défi> --check --endless --seed 7`, 6 défis | aucune ligne |
| Contenu seul | `--each --runs 8` | « Every fertilizer and sprout played cleanly » |
| Fuzz étendu (invariants ajoutés par moi, copie jetable de RunSim) | random 100 runs seed 5 et planner `PLAN_DEPTH=2` 12 runs seed 9 puis 16 runs seeds 100 et 200, 6 défis | 2 violations du nouvel invariant « un Joker porte exactement un trait de combat » : Classic planner seed 9 (run de seed 14, TTetro avec Chameleon) et une autre dans la boucle seeds 100/200 (défi non relevé ; DomH avec Chameleon + Amplifier). Cause confirmée (voir M1) |
| WebGL | `node smoke.js QaBuild/Build --rounds 3` (1280x800) | « Smoke test passed », défaite en manche 2 après 43 placements ; aucune erreur console hors audio |
| WebGL | `node flows.js QaBuild/Build all` (boutique avec achats, Save & quit, rechargement de page) | « All flows passed » |
| WebGL 4:3 | script jetable 1024x768 : menu, 6 cartes de défi | rien de coupé (le canvas reste en 960x600 d'après `screenW`) |
| Équilibre | `PLAN_DEPTH=2 --runs 300 --policy planner --seed 777` par défi | Spring 13,3 %, Winter 14,0 %, Summer 8,0 %, Autumn 10,3 %, Orchard 7,3 %, Greenhouse 5,3 % |

Les invariants ajoutés (copie dans le scratchpad, pas dans le dépôt) : indices de trait dans la forme et uniques par graine, Joker = exactement 1 trait de combat et non-Joker = 0, trait de kind défini, `TotalScore >= 0`, offres de boutique connues du catalogue. Je propose d'ajouter les deux premiers à `Tools/Headless/RunSim/Invariants.cs`.

Réserve sur le build : `QaBuild/` date du 28/10 21:27 (commit `10861aa`), avant les corrections de `c8bc996`. Les captures montrent encore le bouton « Cancel purchase (no refund) » et les cases sélectionnées teintées en vert, alors que le code dit « Skip (no refund) » et une barre dorée. Les correctifs de HEAD (titres d'étapes, Replace/Recolor sur la copie simple, confirmation avant de remplacer une sauvegarde) ne sont donc **pas vérifiés visuellement**.

### Verdict

Le Core tient sous fuzz (plusieurs milliers de runs avec invariants) et les flux boutique/sauvegarde du build passent. Les écrans de choix sur étagères, le Compost remis à zéro par visite et la conversion des Glow à la reprise fonctionnent d'après le code et les tests. Je ne trouve ni bloquant ni perte de sauvegarde sur une sauvegarde créée par ce HEAD. Restent un état de sac incohérent (Joker sans trait de combat), des ids retirés qui échappent encore à la migration des anciennes sauvegardes, et plusieurs écarts mineurs d'accessibilité, de texte et de docs.

Trouvailles : 0 Bloquant, 1 Majeur, 7 Mineurs, 6 Clarté, 3 Observations d'équilibre.

---

## 2. Trouvailles

### Majeur

**M1. Un Joker peut perdre (ou ne jamais avoir) son trait de combat : Chameleon et Propagate créent des « Jokers » sans trait.** Confirmé (Core).

- Où :
  - `Core/Run/RunManager.cs:793-802` (Chameleon) avec `Core/Run/RunManager.Traits.cs:233-262` (`ResolveChameleonColor`/`TryGetFilledNeighborColor` renvoient la couleur d'une tuile voisine **même si elle vaut `PieceColor.Joker`**) puis `Core/Pieces/DeckManager.cs:452-466` (`RecolorHandToken` recolore l'entrée du sac pour de bon) ;
  - `Core/Pieces/DeckManager.cs:356-364` (`DuplicateOfType` : `new PieceToken(shape, color)` sans trait) et `:375-399` (`ReplaceOneOfType`, même défaut pour la graine ajoutée) ; `Core/Pieces/DeckManager.cs:267-283` (`GetCandidateTypes`) propose les types `(forme, Joker)` à Propagate, Replace et Recolor sans filtre.
- Doc de référence : `DeckManager.AddJoker` (« every Joker gets exactly one, never none ») et le commentaire de `PieceTraitKind` (« every Joker piece is tagged with exactly one »).
- Reproduction A (Chameleon), programme .NET minimal :
  ```csharp
  var run = new RunManager(new SystemRandomProvider(3), null); run.DebugDisableWeather();
  var toks = new List<PieceToken>();
  for (int i=0;i<12;i++) toks.Add(new PieceToken(ShapeId.Single, PieceColor.Coral, new PieceTrait(PieceTraitKind.Chameleon,0)));
  run.Deck.RestoreDeck(toks); run.Deck.RedealHand();
  run.Grid.PlacePiece(PieceShapeCatalog.Get(ShapeId.Single), PieceColor.Joker, 1, 0); // une tuile Joker déjà plantée
  run.PlacePiece(0, 2, 0);                                                              // la graine Coral/Chameleon la touche
  // Sac ensuite : Joker x1 (0 trait de combat), Coral x11
  ```
  Dans le fuzz : `PLAN_DEPTH=2 dotnet run --no-build -- --runs 12 --policy planner --challenge Classic --check --seed 9` (run de seed 14, avec l'invariant Joker ajouté) donne « Joker token with 0 combat traits: TTetro traits=Chameleon@2 » après un Forge Chameleon.
- Reproduction B (Propagate) : `var shape = run.Deck.AddJoker(rng, out _); run.Deck.DuplicateOfType(shape, PieceColor.Joker);` donne 2 Jokers, dont un à 0 trait. Le type Joker figure bien dans `GetCandidateTypes`.
- Attendu : un Joker garde un trait de combat ; une graine ordinaire ne devient pas Joker par Chameleon ; Propagate d'un Joker copie aussi son trait (ou les Jokers ne sont pas proposés).
- Obtenu : une graine ordinaire avec Chameleon devient définitivement un Joker (joker de score gratuit, sans combat), et un Joker propagé est un Joker sans badge de combat.
- Impact joueur : un Joker est un joker de couleur qui compte dans tous les groupes ; en obtenir par Chameleon ou en dupliquer sans contrepartie change l'équilibre et affiche un Joker sans badge. Cas rare (tuile Joker voisine requise, ou Propagate sur un type Joker), d'où la gravité limite Majeur/Mineur.
- Piste : filtrer `PieceColor.Joker` dans les candidats Propagate/Replace/Recolor et dans `AddDistinctNeighborColor` de Chameleon (ou copier le trait de combat), puis ajouter l'invariant « Joker ⇔ exactement 1 trait de combat ».
- À noter : j'ai aussi essayé l'inverse (Joker avec trait de combat + Chameleon : il n'est pas recoloré, `Jokerx12 combat=12`). Pas de bug de ce côté.

### Mineur

**m1. Les anciennes sauvegardes gardent d'autres ids retirés que la migration de `ApplySnapshot` ne convertit pas.** Confirmé (Core).
- Où : `Core/Run/RunManager.Save.cs:37-49` (`FromSlotData`) et `:111-130` ; `Core/Run/RunSnapshot.cs` (`usage`, `lastmod`, traits de graine).
- La migration du commit `c8bc996` ne traite que `Modifiers` (le sac de fertilizers). Un id retiré reste dans :
  - une **offre de boutique** sauvegardée (`ShopBlister`) ;
  - `LastPurchasedModifier` (Mimic le copierait) ;
  - `ModifierUsage` ;
  - le trait d'une graine : l'ancien trait Sangsue est l'entier 76, hors enum (retiré en `6990180`).
- Reproduction (snapshot d'une boutique ouverte, `ShopBlister[0]` remplacé par `ModifierId.EclatCoral`, puis `Restore`) : le slot existe, `ModifierCatalog.Get(...)` vaut `null`, `ModifierPricing.GetPrice` vaut `-1`, et `BuyBlisterSlot(0)` **réussit en rendant du Pollen** (30 → 31) et met `EclatCoral` dans les fertilizers actifs. Pour la graine avec trait 76 : la restauration réussit, `Traits[0].Kind` vaut `76` (non défini), sans exception mais le nom affiché serait « 76 » (`PieceTraitVisualDefaults.GetName` : `default: kind.ToString()`).
- Impact : seulement pour une sauvegarde faite avant ces retraits et reprise dans la boutique (ou avec un Sangsue). Une `NullReferenceException` est probable côté interface (`ModifierBadgeFactory.Create(null, …)` ; voir le rapport boutique M1 pour la liste d'appels sans garde).
- Piste : appliquer `RetiredReplacement` aussi aux slots de boutique, à `lastmod` et à `usage` ; ignorer (ou retirer) les traits dont le kind n'est pas défini ; ajouter l'invariant « chaque slot de boutique et chaque trait existe au catalogue ».

**m2. La Collection compte des entrées retirées : le pourcentage peut atteindre 100 % sans tout avoir vu.** Confirmé par lecture.
- Où : `Presentation/CollectionView.cs:132-145` et `Core/Meta/CollectionProgress.cs:40-43`. `Count(category)` est la taille brute de l'ensemble sauvegardé, bornée seulement par `Mathf.Min(count, size)`.
- Un joueur qui avait découvert un Glow de couleur (`EclatCoral/Teal/Violet/Lime`, retirés du catalogue le 28/10) ou le Sangsue garde ces ids dans `collection.txt` : l'en-tête affiche « 141/141 » alors que 4 entrées réelles restent à découvrir.
- Piste : ne compter que les ids présents dans `EntriesFor(category)`.

**m3. Pollinator et Wreath comptent la graine elle-même ; le texte ne le dit pas.** Confirmé (comportement), intention inconnue.
- Pollinator (`GridManager.NewModifiers.cs:~232`, `GridManager.PostClearModifiers.cs:146-159`) : « +2 Pollen when the seed touches 2 or more different colors ». `CollectTouchingColors` inclut la couleur de la graine : **un seul voisin d'une autre couleur suffit**. Reproduction : grille vide, une tuile Teal en (1,2), Single Coral en (2,2) avec Pollinator : `ModifierLueurBonus = 2`. Le test `Pollinator_Honey_AndNectar_PayPollen` ne pose que deux voisins de deux couleurs, donc ne distingue pas. Comparer avec Prism (« touches all 3 other colors ») et Triad Bloom (« 2 other colors »), qui disent « other » et comptent bien la couleur propre.
- Wreath (`ApplyGuirlande`) : « +1 Mult per tile of the seed touching 3 or more planted plots ». Les autres tuiles de la même graine comptent comme parcelles plantées (domino Coral en (2,2)-(3,2), voisins Teal en (1,2) et (2,1) : `AdditiveMultBonus = 1`, la tuile (2,2) a 2 voisins extérieurs + sa propre tuile). Snug Fit, lui, exclut explicitement les tuiles de la graine.
- Piste : préciser le texte (« …touches 1 other color » / « …3 plots, own seed included ») ou aligner le code.

**m4. Deux définitions de « manche de boss » : Bossbane et le tutoriel contre la bannière.** Confirmé par lecture.
- `RunManager.IsBossRound` (`RunManager.cs:638-641`) vaut vrai aux manches 4, 8, 12 (`% BossRoundInterval`). Il pilote **Bossbane** (« x2 Mult during boss rounds ») et l'effet de boss (case verrouillée, emplacement de main, couleur maudite).
- `RoundBannerView.Play` (`RoundBannerView.cs:68-82`) affiche « ROUND N - BOSS » dès que la manche contient un ennemi de rôle `Boss`, c'est-à-dire presque toutes les manches de 5 à 15 (Reclaimer, Thief, Plague, Blightmoth, HeavyLocker, Hater). La manche 15, finale, est « BOSS » à l'écran mais pas pour Bossbane.
- Le tutoriel dit « Every 4th round a boss pest shows up ». Le commentaire de `IsBossRound` dit encore « Rounds 4 and 8 ».
- Impact : le joueur voit « BOSS » en manche 5 avec Bossbane en main et rien ne se passe (la tooltip dit seulement « Inactive: not a boss round »).
- Piste : un seul vocabulaire (par exemple « Boss round » réservé aux manches à effet de boss, et la bannière dit autre chose pour les ennemis boss).

**m5. « Reduced motion » ne couvre que la boutique, mais l'interrupteur est présenté comme global.** Confirmé par lecture.
- `MotionSettings.Reduced` n'est lu qu'à une quinzaine d'endroits : `ShopView`, `MainMenuView`, `ChallengeSelectView`, `ShelfItemHover`, `TileChoiceView`. Le doc de `MotionSettings` dit « the shop's pops… », mais le bouton affiche « Reduced motion: On ».
- Restent animés avec l'option activée : le gel du temps à 0,06 des kills et boss (`GameBootstrap.Placement.cs:284-295`, `PlayHitStop`), le « punch » de chaque bouton (`ButtonPunchEffect`, `ButtonFeedback`), les pulsations du combo (`ComboView`), les pulsations d'ennemi (`FeedbackLayer`), la montée/échelle des cases des écrans de choix (`ShelfCellFeedback.Update`, `ShelfChoiceView.cs:214-243`), le fond animé et les créatures d'ambiance.
- Piste : soit l'étiquette dit « Reduced shop motion », soit les effets de la manche lisent aussi `MotionSettings.Reduced` (hit stop et pulsations en premier).

**m6. Tactile : zones sous le minimum du projet, et un clavier-souris sur écran tactile est traité comme un doigt.** Confirmé par lecture.
- `MobileSetup.MinTouchSize = 56` unités (le canvas passe à 0,8 sur iPad, donc 56 unités ≈ 45 pt). Plusieurs boutons clés ne passent pas par `TouchMin` : `Confirm` (`ShelfStage.cs:87`, 200x46), `Skip (no refund)` (`GameBootstrap.cs:573`, 330x46), `Reroll` et `Next round` (`ShopView.cs:~310-320`, 220x46), `Continue` du sélecteur (`ChallengeSelectView.cs:226`, 300x50). 46 x 0,8 ≈ 37 pt, sous les 44 pt demandés par Apple. Ce sont justement les boutons des écrans de choix récents.
- `MobileSetup.IsTouchDevice` vaut `Application.isMobilePlatform || Input.touchSupported`. `ShopView.cs:995` s'en sert pour exiger « tap puis tap again » : sur un portable à écran tactile (navigateur WebGL qui annonce du tactile), **un utilisateur à la souris doit cliquer deux fois pour acheter**. Non vérifiable ici (pas d'appareil).
- Piste : `TouchMin` sur ces boutons ; décider du mode tactile par le type de pointeur de l'événement (`pointerId >= 0`), comme `ShelfCellFeedback`, plutôt que par la plateforme.

**m7. Le focus clavier/manette est invisible sur les écrans de choix sur étagères.** Suspecté (code), non reproduit faute de Unity.
- `docs/shop-ui-contract.json` promet « focus halo + tooltip ». `ShelfItemHover` (boutique) implémente `ISelectHandler`, mais `ShelfChoiceView`/`TileChoiceView` posent des `Button` à `Transition.None` (ou à image transparente) et `ShelfCellFeedback` ne réagit qu'au pointeur. Au clavier, aucun indicateur de sélection ni de tooltip sur ces cases.
- Piste : faire réagir `ShelfCellFeedback` à `ISelectHandler` et ouvrir le tooltip à la sélection.

### Clarté

**C1. Docs en retard sur le code.** Confirmé.
- `docs/shop-ui-contract.json` : « abandon: 'Cancel purchase (no refund)' button… two-step confirm » alors que le code dit « Skip (no refund) » et que `DECISIONS.md` (`8d8562b`) a choisi un clic sans confirmation ; la liste d'écrans cite encore `draft_type_choice` (DraftView supprimé).
- `docs/BALANCE.md` : « Piggy Bank (plafonné à +5) » (plus de plafond : `RunManager.NewModifiers.cs:62-63` multiplie `Lueur` par 0,1 sans borne) ; « la Forge… une graine au choix parmi 10 » (`ForgeSeedCandidateCount` vaut 8) ; aucune entrée pour le retrait des Glow de couleur ni les nouveaux taux de victoire.
- `RunManager.IsBossRound` (« Rounds 4 and 8 ») et `SettingsView` (« only Master does anything audible today » : Music et SFX s'appliquent via `SfxManager.ApplyVolumes`).
- Un `</summary>` en double sur `RunManager.BuyBlisterSlot` (`RunManager.Shop.cs:~291`) et un `<summary>` double sur `ShopView.GetShelfLayout` (déjà signalé) : avertissements de doc uniquement.

**C2. Chameleon : le texte ne dit pas que la recoloration est permanente.** « the WHOLE seed recolors to match it before scoring » ; en fait la graine du sac est recolorée pour la suite de la run (`RecolorHandToken`). Autre texte à revoir : la phrase « Counts again every time this tile is scored. » est collée à tous les sprouts non-Joker, y compris ceux qui agissent au placement ou à la récolte (Chameleon, Seedpod, Gap, Fertile, Bastion).

**C3. L'icône de Ashbound (`Monochrome`) annonce « +1 » alors que le texte dit +0,5 Mult.** `Tools/check_icon_tags.py` signale aussi 4 icônes retirées (`EclatCoral/Teal/Violet/Lime`, « +6 » contre « +1 Mult »), donc l'outil sort en erreur même sur du contenu mort. Le fichier `Design/Icons/generate_icons.py` garde les anciennes icônes ; à nettoyer pour que l'outil redevienne utilisable en CI.

**C4. Le tutoriel (`TutorialView.BuildRulesColumns`, « How to Play ») ne couvre pas :** Mult (le mot n'apparaît pas, alors que le HUD le montre), Shuffle (limité à la phrase de défaite), la météo, les sprouts et le Workshop (Forge, Compost, packs), le sac de graines, et les effets de boss (emplacement verrouillé, couleur maudite, cases verrouillées). « Every 4th round a boss pest shows up » est vrai pour les effets, pas pour la bannière (m4). Le tutoriel Sprig (`SprigView`) ne donne que des encouragements génériques.

**C5. `Reduced motion`, `Skip (no refund)` et les achats qui échouent.** Un clic sur un objet trop cher donne un tremblement, sans son ni message (`ShopView.cs:1008-1014`) ; avec « Reduced motion » activé, il ne reste que le prix rouge et l'icône pâle. Le contrat d'accessibilité les cite comme suffisants, c'est cohérent, mais le texte « how much Pollen is missing » n'apparaît qu'au survol (pas au clic simple sur tactile avant « tap again »).

**C6. Audio du build WebGL.** Tous les clips affichent « Loading FSB failed for audio clip » dans la console (`Score`, `LueurGain`, `ButtonClick`, `BackgroundMusic`…). Probablement lié à Chromium sans périphérique audio (headless), mais je ne peux pas l'affirmer : à vérifier sur un vrai navigateur avant de conclure que le build n'a pas de son.

### Observations d'équilibre

**O1. Victoires du planner (profondeur 2, 300 runs, seed 777) :** Spring 13,3 %, Winter 14,0 %, Summer 8,0 %, Autumn 10,3 %, Orchard 7,3 %, Greenhouse 5,3 %. `BALANCE.md` vise 4 à 6 % pour les autres défis (profondeur 3). Winter (Marathon) dépasse Spring, alors qu'il est présenté comme le plus dur. À confirmer à profondeur 3 avant de toucher à `EnemyHpMultiplier`.

**O2. Botanist porte les runs gagnantes** (Winter, planner : tenu dans 12 runs, 13,8 manches en moyenne contre 5,75). Les fertilizers les plus faibles ou neutres à ce seuil : Fourth Slot (3,4 manches), Underdog (3,6), Arsenal (3,6), Windrider (3,7), Golden Repetition (3,7). Le bot ne joue pas le Compost et ne gère pas Arsenal : signal seulement.

**O3. Save & quit pendant un achat en attente** rend tout le Pollen et retire une nouvelle boutique (déjà noté au rapport précédent, voulu) ; le Compost étant remis à zéro par visite, c'est maintenant un reroll de packs gratuit et sans limite. À garder en tête si le Compost sert à amincir le sac.

---

## 3. Vérifié sans trouvaille

- Corrections de `c8bc996` lues dans le code et dans les tests : `RetiredReplacement` (sans doublon : le Devotion déjà tenu est détecté avant et après), `IndexOfPlainestCopy` + `Replace/Recolor` (la copie retirée est celle retirée aussi de la pioche : comparaison par `Equals` et non par `Matches`), reset de `LastRandomModifierGranted`, `_purchaseAnimating` (le rappel de `PlayBlisterRemoval` s'appelle même en mouvement réduit), confirmation « Replace saved run? Click again ».
- Conversion points en Mult : `PointsToMult` arrondit au plus près (demi vers le haut) avec minimum 1 ; Prisoner (6 pts) donne bien +1, Carrefour (18 pts) +2, Alchemist's Wheel (52 pts) +4, Chain (15 pts) +1, Mega Chain (4 Mult + 1 par case).
- Texte contre code, confirmés : Early Bird (manches 1 à 5), Marathoner (à partir de la 9e), Underdog/Finishing Blow (seuils 50 % et 25 % du total des PV), Seasoned (0,5 par manche gagnée), Piggy Bank (0,1 par Pollen, sans plafond), Seed Bank, Rain/Sun/Frost/Wind Dancers et leurs variantes d'intérieur (`WeatherCatalog.Mechanic`), Storm Chaser/Weatherproof (`IsHarsh`), Siphon (4 %, max 10), Volatile Brew (1 chance sur 5 à chaque manche), descriptions d'ennemis (Bramble tous les 6 placements, Plague 3 cases gelées au début, Leech 250 PV par ligne).
- Dépassement d'entier : Piggy Bank + Mult +4 avec 2,1 milliards de Pollen donne encore un score positif (630 000 000), pas de dépassement observé.
- `GridManager.Size` et les couleurs actives suivent bien le défi (Greenhouse 5x5, Orchard 2 couleurs, puis retour à Classic 6x6 et 4 couleurs, y compris à la restauration d'une sauvegarde).
- Sauvegarde : `flows.js` (Save & quit en manche, et en boutique, puis rechargement de page et Continue) reprend au même état (manche, Pollen, boutique à l'identique).
- Challenge Select aux 6 défis en 1024x768 : cartes, bannières, points de progression lisibles, rien de coupé.

## 4. Non testé et suite proposée

- Tout ce qui exige Unity : mise en page réelle des étagères (1 à 8 options), `ShelfCellFeedback`, tooltips épinglés au toucher, zones sûres iPad, son. Le build `QaBuild/` est antérieur à `c8bc996` : régénérer le build puis rejouer `node flows.js QaBuild/Build all` et les scénarios « Compost deux fois dans la même visite », « Replace puis Skip à l'étape 2 » et « Play avec une sauvegarde existante ».
- Pas de scénario WebGL de tutoriel Sprig, de Collection (juste la première capture du sélecteur) ni de sélecteur de défi à 4:3 avec sauvegarde existante.
- Le fuzz du planner à profondeur 3 et le planner `--endless` sur 40 runs n'ont pas été relancés pour tous les défis (le `--endless` à 40 runs sur Classic dépasse largement 10 minutes) : 12 à 40 runs selon le défi.
- Invariants à ajouter à `Tools/Headless/RunSim/Invariants.cs` : « Joker ⇔ exactement 1 trait de combat » (M1), « tout trait a un kind défini et un indice dans la forme, unique par graine », « tout slot de boutique et tout fertilizer tenu existent au catalogue » (m1), et un test de restauration qui injecte un slot de boutique retiré.
