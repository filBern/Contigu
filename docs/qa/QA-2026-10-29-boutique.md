# QA 2026-10-29 : boutique (écrans de choix sur étagères), Compost, challenges, retrait des Glow de couleur

Branche `claude/blissful-brahmagupta-u0ywu2`, HEAD `4d535ec`. Rapport rédigé par lecture de code + scripts .NET. Unity n'a pas tourné : tout ce qui touche la Presentation est « d'après le code ».

## 1. Résumé

Exécuté :
- `cd Tools/Headless/Tests && dotnet test` : 621 tests passent, 0 échec.
- `python3 Tools/qa/content_audit.py` : 0 trouvaille (145 fertilizers, 72 sprouts, 76 traits, 16 ennemis, 23 météos). `python3 Tools/qa/glyph_audit.py` : « No missing glyphs ». `check_icon_tags.py` non lancé (hors thème).
- RunSim (Debug) avec `--check`, seed 3, pour Classic, Marathon, Summer, Autumn, Orchard, Greenhouse : `--runs 100 --policy random` (aucune ligne `invariant:` ni `exception:`). `PLAN_DEPTH=2 --policy planner --runs 6 --seed 5` (6 défis, rien). `--each --runs 8` (rien).
  - Écart au protocole : le planner `--runs 40 --endless` ne finit pas en 10 min sur Classic (arrêté), je suis donc passé à 6 runs sans `--endless`. Couverture endless non faite. La politique random meurt en ~1,5 manche : couverture de la boutique faible.
  - Aucun scénario de fuzz ne charge une sauvegarde contenant un ancien fertilizer retiré, d'où le bug n°1 ci-dessous, invisible pour les invariants actuels.
- Revue de code : `GameBootstrap.Shop.cs`, `ShelfStage.cs`, `ShelfChoiceView.cs`, `TileChoiceView.cs`, `ChallengeSelectView.cs`, `RunManager.Shop/Workshop/Save`, `DeckManager`, `UpgradeSystem`, commits `c33aac3`, `c49f25f`, `32bfd3d`, `8d8562b`.
- Compilation Unity impossible. Pas d'erreur de compilation évidente repérée : les API appelées (`ShapePreviewFactory.ColumnsOf/RowsOf/UniformTile/BuildWithTraits/BuildMono/TileOverride`, `PieceBox.Configure/CreateCell`, `HoverTooltip.Pin/Unpin`, `UpgradeVisualDefaults.PlaceGlyph/GetWorkshopArt`, `ModifierBadgeFactory.Create(attachTooltip:)`, clés `Loc` `choice.*`) existent toutes. Seul défaut : deux `<summary>` empilés sur `ShopView.GetShelfLayout` (`ShopView.cs` ~l.600), simple avertissement de doc.

Verdict : le Core est sain sous fuzz, mais le retrait des Glow casse une ancienne sauvegarde qui en contient un (Majeur). Le flux de choix sur étagères n'a pas de défaut bloquant ; deux comportements de Replace/Recolor sont trompeurs.

Trouvailles : 0 Bloquant, 2 Majeur, 3 Mineur, 6 Clarté, 2 Observations.

## 2. Trouvailles

### Majeur

**M1. Une sauvegarde ancienne contenant un Glow de couleur (Eclat*) charge, puis casse la run (null, Pollen négatif).** Confirmé (Core), Suspecté (écrans Unity).
- Où : `Core/Modifiers/ModifierDefinition.cs:194-210` (le commentaire dit « gardés définis pour qu'une vieille sauvegarde se charge », mais ils ne sont pas dans `All`, donc `ModifierCatalog.Get` renvoie null, l. 668), `Core/Run/RunSnapshot.cs` (aucune validation des ids), `Core/Run/RunManager.Save.cs:ApplySnapshot`.
- Reproduction (programme jetable `…/scratchpad/repro/P.cs`) : prendre un `RoundStartSnapshot`, y ajouter `ModifierId.EclatCoral` (niveau 0, âge 0), `Restore`. Résultats :
  - la restauration réussit : `ActiveModifiers = [Polyvalence, EclatCoral]` ;
  - `ModifierCatalog.Get(EclatCoral)` vaut `null` ;
  - `ModifierPricing.GetPrice(EclatCoral)` vaut `-1` ; `SellModifier` rembourse `-2` : Pollen 50 → 48 (le joueur paie pour vendre) ;
  - `RunManager.CountDistinctModifierCategoriesHeld()` (tooltip de Hardy Growth, `RunManager.Traits.cs:725`) lève `NullReferenceException`.
- Autres déréférencements sans garde de `Get(id)` sur les modifiers actifs (d'après le code, non exécutés) : `Presentation/ModifierPanelView.cs:375` (`ModifierBadgeFactory.Create(null, …)` lit `def.Id` : le panneau de fertilizers plante au Continue), `GameBootstrap.cs:279` (« Sold … » levé après la vente, le Core a déjà modifié l'état), `RunManager.WorldEnemies.cs:142` (Rotgrub en Autumn), `GameBootstrap.RunFlow.cs:37/42`, `GameBootstrap.Qa.cs:89`.
- Le scoring, lui, ignore silencieusement l'id (dictionnaires `TryGetValue`) : le joueur garde un emplacement de fertilizer mort.
- Attendu : une vieille sauvegarde se reprend proprement (Eclat converti en Devotion de la couleur, ou retiré avec remboursement), ou au minimum aucune exception.
- Impact : le joueur qui avait un Glow dans sa run sauvegardée et clique Continue obtient un panneau cassé / une exception, ou perd du Pollen en vendant.
- Piste : dans `ApplySnapshot` (ou `FromText`), remplacer les ids retirés (Eclat* → Devotion de même couleur, ou suppression) ; ajouter l'invariant « tout id de `ActiveModifiers` a un `ModifierCatalog.Get` non null et un `GetPrice` ≥ 4 » et un test de restauration avec un id retiré.

**M2. Replace (Transmute a Seed) et Recolor (Rebloom a Seed) frappent la première copie du type, pas celle montrée à l'écran : ils peuvent détruire les sprouts de la graine.** Confirmé (Core), comportement d'écran d'après le code.
- Où : `GameBootstrap.Shop.cs:188-200` (`RepresentativeTraits` affiche la copie la plus enchantée du type) contre `Core/Pieces/DeckManager.cs:377` (`ReplaceOneOfType`) et `:411-416` (`RecolorOneOfType`) : `FindIndex(t => t.Matches(shape, color))` prend la première copie (les traits sont ignorés), et Recolor recrée `new PieceToken(shape, toColor)` sans traits.
- Reproduction (script jetable) : un type avec 2 copies, la première enchantée (Golden). Replace : après coup il ne reste que la copie sans trait (`traits=0`, l'enchantée est partie). Recolor : somme des traits du bag 1 → 0 (les upgrades de la copie recolorée disparaissent).
- Attendu : l'écran présente la graine enchantée comme « le type » ; le joueur s'attend à ce que le choix concerne la copie montrée, ou à être prévenu. Aucune mention nulle part (texte de l'étape, tooltip « Mastery level N » seulement).
- Impact : perte silencieuse de sprouts achetés (Forge/Sprout Pack), le joueur ne peut ni choisir la copie ni le savoir.
- Piste : sélectionner en priorité la copie sans trait (et préserver les traits sur Recolor), ou afficher « the plain copy goes » ; à décider avec le design (voir DECISIONS l.7270+, qui ne tranche pas ce cas).

### Mineur

**m1. Course de 0,25 s après l'achat d'un sprout du Blister : Skip/Esc possible avant l'écran de choix, puis révélation parasite.** Suspecté.
- Où : `GameBootstrap.Shop.cs:37-48` (le dispatch attend `PlayBlisterRemoval`, `ShopView.cs:122` `BlisterRemoveDuration = 0.25`) ; `UpdateAbandonButton` (l.438) affiche déjà Skip dès que `PendingUpgrade != null`, et Esc appelle `RequestAbandonPurchase` (`GameBootstrap.cs:225`).
- Scénario : acheter un sprout Grid du Blister (mouvement réduit désactivé), appuyer sur Esc dans les 0,25 s. `AbandonPending` vide le pending, puis le callback appelle `HandleUpgradePurchaseResult` avec `PendingUpgrade == null` : la chaîne tombe sur `else if (_run.LastRandomModifierGranted.HasValue)` (l.116), valeur périmée jamais remise à null (`RunManager.Shop.cs:497` n'est assigné que pour un Random Modifier), donc le carousel « Random fertilizer » d'un ancien achat peut rejouer.
- Impact : fenêtre très courte, mais l'écran affiche un fertilizer qui n'a pas été gagné maintenant.
- Piste : ne dispatcher que si `_run.PendingUpgrade != null` ; remettre `LastRandomModifierGranted` à null au début de `ApplyPurchasedUpgrade`.

**m2. « Select 1 seeds for … » : pluriel fixe.** Confirmé (texte). `Loc.cs:36` `choice.selectSeeds` = « Select {0} seeds for {1} », `TileChoiceView.cs:~82` passe `_requiredCount = min(3, candidats)`. Pas atteint avec un sac ≥ 10 sauf Tinted (sans Joker), donc rare. Piste : forme singulier/pluriel.

**m3. Choisir « Play » dans le sélecteur de défi écrase silencieusement la sauvegarde existante.** Confirmé par lecture. `ChallengeSelectView.Show` affiche à la fois « Continue saved run » et Play ; `OnChallengeChosen` → `StartNewRun` → `BeginRun` → `SaveCheckpoint()` (`GameBootstrap.RunFlow.cs:~369-376`, `RunSaveStore.Save`) remplace la run sauvegardée par la manche 0 de la nouvelle run, sans avertissement. Impact : perte de la run en cours si le joueur clique Play au lieu de Continue. Piste : demander confirmation quand `RunSaveStore.Exists`.

### Clarté

**C1. La description du Compost dit « Costs more each time » alors que la hausse repart à zéro à chaque visite.** `UpgradeDefinition.cs:358-360` (`Compost`), logique `RunManager.Shop.cs:15` (`_compostsBought = 0` à `OpenShop`, commit `c33aac3`). Le joueur qui revient en boutique voit un prix de base sans comprendre ; celui qui lit « chaque fois » s'attend à un prix cumulé. Piste : « Costs more each time you buy it this visit. »

**C2. Skip (bouton et Esc) abandonne l'achat sans confirmation et sans dire que le Pollen est perdu.** `GameBootstrap.Shop.cs:420-435`, `GameBootstrap.cs:225`. Décidé exprès (commit `8d8562b`, « un clic »), mais le libellé affiché est « Skip » seul ; les clés `choice.skipPack` (« Skip (no refund) ») et `choice.abandonConfirm` existent dans `Loc.cs:41,43` et ne sont utilisées nulle part. Un Esc réflexe sur un écran de choix coûte le prix du pack et rend le prochain Compost plus cher. Piste : tooltip ou sous-titre « no refund » sur le bouton.

**C3. Étapes 2 de Replace : rien n'indique laquelle part et laquelle est dupliquée, et le titre de l'étape 1 est générique.** `GameBootstrap.Shop.cs:160/217/245` : titres codés en dur « Choose a seed type » (étape 1 de Replace, Propagate et Recolor, identique pour trois effets différents), « Choose a seed to duplicate instead », « Choose the target color » ; la source (panneau de l'objet) rappelle le nom de l'upgrade mais pas le choix déjà fait à l'étape 1. Ces titres, ainsi que « Mastery level N » (l.181, niveau couleur + forme − 1) et « Random fertilizer » (l.118), ne passent pas par `Loc` alors que le reste des écrans de choix l'utilise. Piste : titres par upgrade (« Choose the seed to transmute » / « …to propagate » / « …to rebloom ») et rappel du choix d'étape 1.

**C4. Deux noms pour chaque défi.** Le sélecteur affiche Spring/Winter/Summer/Autumn/Orchard/Greenhouse, mais l'identifiant et les docs/outils parlent de Classic/Marathon. `ChallengeCatalog.cs` (Classic → « Spring », Marathon → « Winter »). Pas un bug de jeu, mais source de confusion pour la QA et les docs. De plus, `ChallengeSelectView.StatsFor` contient une branche « ENDLESS » (`RoundCount >= 60`) que aucun défi n'atteint (code mort).

**C5. Chromatic Hoard et Wildcard : l'effet est cohérent avec les textes, mais Wildcard n'a plus de choix.** `ModifierDefinition.cs:281-282` dit « whichever color scores best with your Devotion fertilizers » ; depuis `c49f25f` tous les Devotion valent +3 Mult (`ResolveJokerColorForModifiers`, `GridManager.PreClearModifiers.cs:1011-1042`), donc le « meilleur » est simplement la première couleur de base dont on a le Devotion (ordre fixe). Texte exact mais trompeur : aucune optimisation n'est faite. Chromatic Hoard (« +2 Mult per color for which you hold its Devotion ») correspond bien au code. Pas d'écart chiffré.

**C6. Restes de vocabulaire retiré.** `ModifierVisualDefaults.cs:101-104, 206-209` gardent des entrées pour Eclat*, `ModifierBadgeFactory.cs:12` parle encore de « Glow »/Devotion, `ScoringConstants.CollectionChromatiqueMultPerCompletePair` garde un nom de « paire » (il n'y a plus de paire). Aucun impact joueur.

### Observations d'équilibre / de conception

**O1. Save & quit pendant un achat en attente rembourse tout et re-tire le contenu des packs.** La reprise se fait sur le checkpoint d'ouverture de la boutique (`RunManager.Save.cs:15`, `:~160-185`, voulu : « la boutique telle qu'à l'ouverture »). Conséquence : acheter un pack, voir les options, Save & quit, Continue rend le Pollen et retire un nouveau contenu (rng neuf) : reroll gratuit des packs, de la Forge et du Compost. À confirmer si c'est acceptable.

**O2. Le champ `_compostsBought` reste sauvegardé alors qu'il est remis à 0 à chaque ouverture de boutique.** Inoffensif (`RunSnapshot.CompostsBought`), mais ne sert plus qu'au checkpoint de boutique (toujours 0).

## Vérifié sans trouvaille

- Compost : prix de base 3, +2 par achat dans la visite, réinitialisé par `OpenShop`, conservé au Reroll (`keepCompost`), non proposé quand le sac est à `MinDeckSize` (`IsUpgradeEligibleForOffer`, `RunManager.Shop.cs:189`), Skip compte comme un achat (`AbandonPending`), un retrait réel incrémente (`ResolveCompostChoice`). Pas d'achat qui perd le Pollen sans retour sur sac minimal.
- Candidats : Compost ≤ 5 graines, Forge/Sprout Pack/upgrade Grid 8 (`ForgeSeedCandidateCount`), Seed/Sprout/Fertilizer/Mastery Pack 3 ; types de Replace/Propagate/Recolor ≤ 5. Un seul type dans le sac : Replace retombe sur « inclure le type lui-même » (`GetReplacementCandidateTypesFor`), l'écran n'est jamais vide. Orchard : Recolor propose 1 seule couleur cible, correct.
- Double-clic : `BuyUpgradeSlot`/`BuyBlisterSlot` refusent tant que `PendingUpgrade != null` ; Confirm masque la vue avant `onChosen`, Replace rouvre la même vue (reset de `_selectedIndex`, Confirm désactivé) sans double résolution.
- Skip/Abandon à l'étape 2 : `PendingUpgrade` reste posé pendant les deux étapes, `AbandonPending` nettoie tout et la vue d'étagères est masquée ; Abandon run et Save & quit rechargent la scène, donc aucune vue de choix ne survit.
- Reprise avec choix en attente : l'état « pending » n'est jamais sauvegardé (le checkpoint est celui d'ouverture), sans incohérence.
- Tooltips épinglés au toucher : `HoverTooltip.OnDisable` masque le tooltip quand la vue se ferme.
- Packs sans tuiles : le Sprout Pack et les upgrades Grid ont toujours des candidats (3 paliers de repli dans `GetCandidateTokenIndices`) ; `ResolvePackModifierChoice` ne peut pas échouer faute de place (éligibilité vérifiée à l'achat).
- Texte des défis et de l'indice « 1 per round cleared, +2 for a victory » : conformes à `MetaStatsRecorder`.

## 3. Non testé et suite proposée

- Tout ce qui exige Unity : mise en page des étagères (1 à 8 options, pièces de 3-4 de large, `ShelfStage.Begin`), `ShelfCellFeedback`, tooltips, animation de retrait de carte, barre Skip au-dessus des overlays, rendu du sélecteur de défi (bannière, icônes `Resources/Icons/Challenges`, points). Aucun build WebGL dans le dépôt, donc `Tools/qa/web/smoke.js` non lancé.
- Fuzz endless du planner non terminé (trop lent) ; couverture de la boutique par la politique random faible.
- Suite proposée : (1) corriger/tester M1 puis ajouter l'invariant sur les ids actifs et un test de restauration d'id retiré ; (2) décider du comportement de M2 ; (3) un scénario `qa.js` : acheter un Sprout Pack, passer par les deux étapes, Skip à l'étape 2, vérifier Pollen/prix ; (4) un scénario Replace en deck mono-type.
