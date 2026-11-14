# QA 2026-10-27 : vérification des corrections (build WebGL QaBuild/Build)

Build testé : `QaBuild/Build` (commit `1b53701 New build`, après `6a2d054`). Chromium headless via `Tools/qa/web/qa.js`, canvas redimensionné à **1280x800** et **1024x768**. Scripts jetables dans le scratchpad de session (`v1.js` à `v6.js`, `lib.js`). Captures dans `docs/qa/shots-verif/`. Tout est « observé dans le build », sauf mention.

## 1. Verdict

| Correction | Résultat |
|---|---|
| M1 Sell sous le panneau | OK (1280 et 1024, 2 et 4 fertilizers) |
| m1/m3/c3 Menu cliquable et ouvre Settings | **PARTIEL : le bouton Menu s'ouvre, mais Settings est inutilisable sur les choix de seed pack et de tuiles (sprout)** |
| Save & quit puis Continue | OK depuis boutique, défaite (pas de Continue proposé, voulu), choix de défi ; **impossible depuis les écrans de choix** |
| m2 Rules | OK aux deux résolutions |
| c1 infobulle fertilizer | Texte tactile vérifié ; variante clavier non vérifiable (voir 3) |
| c2 texte Settings | OK, lisible, mais se superpose au HUD en manche (mineur) |
| Nouveaux chevauchements | 2 mineurs (Seed bag/Rules, Shuffle/Slot2) |

Trouvailles : 1 Majeur, 2 Mineurs, 1 Clarté.

## 2. Trouvailles

### Majeur — Settings ouvert sur un écran de choix est dessiné sous le choix et ses boutons ne répondent pas (Confirmé)
- Écrans concernés : choix de seed pack (« Choose 1 seed to add », slot Workshop `UpgSlot_*`) et choix de tuiles d'un sprout (« Select 3 seeds for Glow Sprout », `Tile_*`). Constaté à 1280x800 et 1024x768.
- Reproduction : lancer un run, `forceRoundWin`, boutique, cliquer un `UpgSlot_0` (ou acheter un sprout à tuiles, ex. Glow/Catalyst Sprout), cliquer Menu. Attendu : Settings au premier plan avec Resume / Save & quit utilisables. Obtenu : Settings s'affiche **derrière** le contenu du choix (les tuiles et « CHOOSE 1 SEED » recouvrent les curseurs de volume, CONFIRM et « Cancel purchase » restent par-dessus). Dans l'état QA, `Close` (Resume), `MainMenu` (Save & quit), `AbandonRun`, `ReducedMotion`, `ResetData` sont `visible:false` (raycast bloqué par l'overlay du choix). Un vrai clic souris (`realClick`) sur les coordonnées de Resume et de Save & quit n'a aucun effet. Seul le bouton Menu (toggle) ferme Settings.
- Impact joueur : « Save & quit » n'est pas atteignable depuis ces écrans, donc m1/m3/c3 ne sont corrigés qu'à moitié : Menu est cliquable (il l'était déjà côté bouton), mais l'écran qu'il ouvre est cassé visuellement et fonctionnellement. Les boutons de Settings paraissent actifs alors qu'ils ne le sont pas.
- Cause probable (lecture du code, non confirmée) : seul `MenuButton` a un Canvas propre (`GameBootstrap.cs:703`, ordre 100). La racine de Settings n'est pas remontée ; `ShelfChoiceView.cs:76` et `TileChoiceView.cs:91` font `_root.SetAsLastSibling()` à leur ouverture et passent donc devant Settings.
- Piste : donner à Settings son propre Canvas (ordre > choix, < Menu) ou `SetAsLastSibling()` à l'ouverture de Settings.
- Captures : `seedpack-settings-1280.png`, `tilechoice-settings-1280.png`, `tilechoice-settings-1024.png`.
- Non affecté (vérifié) : Menu en boutique normale, après défaite, sur le choix de défi, en manche, depuis l'écran Rules. Resume et Save & quit répondent, Save & quit puis Continue reprend en boutique (manche 1) ou manche selon l'état.

### Mineur — Seed bag et Rules se touchent (Suspecté visuel, mesuré sur capture)
- En manche à 1280x800, le sprite de « Seed bag » dépasse d'environ 8 px sous celui de « Rules » (les rects de boutons ne se chevauchent pas : 899-1049 et 1061-1211, donc c'est le sprite). Les libellés restent lisibles. Capture : `round-buttons-1280.png`. Piste : écarter les deux boutons de quelques pixels.

### Mineur — Shuffle (56x56 tactile) chevauche le coin de Slot2 (Confirmé par mesure)
- `ShuffleButton` 863-919 x 523-579 contre `Slot2` 737-887 x 555-705 : 24x24 px à 1280x800 (20x20 à 1024x768). Le coin du slot de main est mangé par Shuffle : un clic dans cette zone vise-t-il la graine ou Shuffle, selon l'ordre de dessin. À rapprocher de m4 (taille minimale tactile) : régression probable de la correction 56 unités. Piste : décaler Shuffle de ~24 px vers la droite ou vers le haut.

### Clarté — Settings en manche se superpose au HUD (Confirmé sur capture)
- Le texte c2 (« Save & quit resumes at the start of the current round… ») et le titre « Menu » sont posés sur le HUD (compteur 400/400, icône de pest) à 1280 et 1024, visibles à travers le voile sombre. Lisible mais chargé. Il n'y a pas de chevauchement avec les éléments de Settings (boutons : 0 chevauchement mesuré). Captures : `settings-round-1280.png`, `settings-round-1024.png`. Piste : voile Settings opaque ou plus sombre.

## 3. Détail des vérifications

- **M1 Sell** : avec 2 fertilizers (1280x800) Sell à (224,576), rect 139-309 x 548-604, panneau jusqu'à y=535 : sous le panneau, sur aucun badge. Avec **4 fertilizers** (1024x768, obtenus sur 7 boutiques) : Sell à (222,525), rect 154-290 x 502-547, panneau jusqu'à ~492 ; 0 chevauchement avec les 4 badges, pour chacun des 4 badges armés. La boutique à 4 en 1280x800 n'a pas été atteinte (le script a échoué à « Leave »). Captures : `sell-2fert-1280.png`, `sell-4fert-1024.png`. Note : le bouton Sell apparaît partiellement sous l'infobulle de survol dans la capture du badge armé, sans la cacher (marge de 7 px).
- **Menu / Settings** : cliquable en boutique, sur le choix de défi (`challenge-settings-1280.png`), en manche, sur Rules (Menu reste au-dessus de la carte Rules, il en mord seulement le coin supérieur droit, sans toucher de texte), après défaite (`defeat-settings-1280.png`, Resume et Save & quit répondent, aucun Continue proposé après Save & quit d'une run perdue, 1280 et 1024). Boutons de Settings : 0 chevauchement entre eux, aux deux résolutions.
- **Save & quit puis Continue** : depuis la boutique, Continue reprend en boutique avec le Pollen d'ouverture de visite. Depuis les choix : impossible (voir Majeur).
- **m2 Rules** : « How to play » en deux colonnes, entièrement dans la carte blanche, aucun texte hors écran, « Got it » dans la carte, aux deux résolutions (`rules-1280.png`, `rules-1024.png`).
- **c1 infobulle fertilizer** : le build affiche « Tap, then Sell +4 ». C'est la variante tactile. Le Chromium headless annonce `Input.touchSupported` (les boutons font 56 de haut, preuve que `MobileSetup.IsTouchDevice` est vrai), donc la variante clavier « Sell +N (X) » n'a pas pu être vérifiée dans le build. Lecture du code : `TooltipView.cs:193` fait bien la distinction. Capture : `fert-tooltip-1280.png`.
- **c2 texte Settings** : présent, une ligne, sans coupure aux deux résolutions, et ne chevauche aucun contrôle de Settings.
- **Erreurs JS/Unity** : aucune hors syncfs ignoré (`qa.errors`).

## 4. Non testé
- Variante clavier de l'infobulle (c1) : exige un navigateur sans API tactile (injecter la désactivation de `ontouchstart` avant le chargement de Unity, à ajouter à `qa.js`).
- Sell avec 4 fertilizers à 1280x800 ; Sell avec la variante tactile réelle (doigt).
- Save & quit depuis les choix (bloqué par le Majeur) : à repasser après correction. Scénario à ajouter à `flows.js` : ouvrir un choix, Menu, vérifier que `Close` et `MainMenu` sont `visible:true`.
- Rendu Unity natif (le Development Console IMGUI recouvre le coin bas gauche des captures, ce n'est pas une régression).

## 5. Test à ajouter
- Test de présentation : pour chaque overlay de choix ouvert, `SettingsView` doit avoir un ordre de rendu supérieur à celui de l'overlay, ou `Toggle()` doit le remonter.

## 6. Re-vérification (build `9d2630d New build`, incluant `210b1d3`)

Scripts du scratchpad `v1.js`, `v4.js`, `v7.js`, `v8.js`, à 1280x800 et 1024x768. Captures `docs/qa/shots-verif/re-*.png`. Observé dans le build.

| Point | Résultat |
|---|---|
| Majeur : Settings sur choix de seed pack | **Corrigé.** Resume, Save & quit, Abandon run, Reduced motion, Reset data sont `visible:true`, 0 chevauchement, aux deux résolutions. Le choix reste en place après Resume. |
| Majeur : Settings sur choix de tuiles / de type d'un sprout | **Corrigé** (Tile_* à 1280 et 1024, Type_* à 1280 et 1024). |
| Save & quit depuis un choix, puis Continue | **OK.** Seed pack (1280, 1024), choix de type (1280, 1024), choix de tuiles (1024) : Continue propose la reprise, boutique manche 1, Pollen d'ouverture (30, et non les 3000 accordés avant l'achat), achat annulé, blister identique (`sold:false`). Save & quit depuis un choix de **tuiles (Tile_*) à 1280** non rejoué : ce build a tiré un choix de type à la place, couvert de la même façon. |
| Menu ferme Settings au 2e clic | **OK** en manche, sur le choix de défi (via Resume), sur choix de type et choix de tuiles (1280, 1024). Le choix reste ouvert dessous. |
| Settings lisible ailleurs | OK en boutique, manche, Rules, défaite (non rejoué, inchangé), choix de défi : boutons sans chevauchement, aucun texte hors écran. |
| Shuffle / Slot2 | **Pas résolu au sens strict** : le bouton Shuffle fait maintenant 46x48 (1280) / 37x38 (1024) mais son rect chevauche encore le rect de Slot2 de 14x16 px (1280) et 12x13 px (1024) (Shuffle 873-919 x 523-571, Slot2 737-887 x 555-705). Sur la capture, le cercle Shuffle est posé sur le coin haut-droit du plateau de main, sans masquer de graine visible. Le chevauchement ne concerne que la zone cliquable de Slot2 (150x150, plus grande que le sprite). Reste Mineur. |
| Seed bag / Rules | **Corrigé** : rects 893-1043 et 1069-1219, écart 26 px (`re-round-buttons-1280.png`). |

Observation restante (Clarté, mineure) : le voile de Settings sur les écrans de choix et la boutique est translucide, donc le titre du choix (« PROPAGATE A CUTTING », « The Pollen Shop », le compteur de Pollen) transparaît derrière le titre « Menu » et les curseurs (`re-sprout-settings-1024.png`, `re-seedpack-settings-1280.png`). Lisible, mais chargé ; un voile plus opaque règlerait aussi le point c2 en manche (le texte « Save & quit resumes… » chevauche toujours le compteur 400/400 de 100x20 px à 1280, 80x16 à 1024).

Limite : la variante clavier de l'infobulle (c1) reste non testée (Chromium annonce le tactile). Aucune erreur JS/Unity hors syncfs.
