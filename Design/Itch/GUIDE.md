# Publier LASTBLOOM sur itch.io : guide pas à pas

Les noms de menus d'itch.io viennent de ma connaissance de leur tableau de bord, que je n'ai pas pu ouvrir d'ici : si un libellé a changé, cherche son équivalent. Les images sont dans `upload/`, **numérotées dans l'ordre où on les utilise**. Les textes à coller sont dans les cadres de code.

## 0. Avant de commencer (5 min)
1. **Fais le build WebGL** dans Unity : Build Profiles → WebGL, **sans** `CONTIGU_DEBUG`, Compression Format **Disabled** (voir `docs/IPAD.md`).
2. **Zippe le contenu du dossier du build** : `index.html` doit être **à la racine du zip** (pas dans un sous-dossier). Nomme-le `lastbloom-web.zip`.
3. Ouvre le dossier `Design/Itch/upload/` : tout ce qu'il faut y est.

## 1. Créer le projet
1. Connecte-toi sur itch.io, menu en haut à droite → **Dashboard** → **Create new project** (ou va sur itch.io/game/new).

## 2. Écran « Details »
| Champ | À mettre |
|---|---|
| **Title** | `LASTBLOOM` |
| **Project URL** | `lastbloom` (donne `felixbernier.itch.io/lastbloom`) |
| **Short description or tagline** | `A garden-puzzle roguelike: plant seeds, harvest lines, grow a build, outlast the pests.` |
| **Classification** | Games |
| **Kind of project** | **HTML** (You have a ZIP or HTML5 file that will be played in the browser) |
| **Release status** | In development |
| **Pricing** | No payments (gratuit), ou « $0 or donate » si tu veux permettre les dons |

## 3. Uploads
1. Clique **Upload files** et choisis `lastbloom-web.zip`.
2. Une fois envoyé, coche **This file will be played in the browser**.
3. Réglages d'embed (juste dessous) :
   - **Viewport dimensions** : `1280` × `800`
   - **Mobile friendly** : coché, orientation **Landscape**
   - **Fullscreen button** : coché
   - **Automatically start on page load** : **décoché** (le premier toucher doit lancer le son sur iPad)
   - **Enable scrollbars** : décoché
   - **SharedArrayBuffer** : **décoché**

## 4. Description
Colle le texte final dans « Description » (éditeur riche : les titres `##` se font avec le menu de style). Les images 04 à 08 se placent à l'endroit indiqué dans `PAGE_LAYOUT.md` (bloc 5 et bloc 7).

```markdown
# LASTBLOOM

*The last garden still blooms, at the edge of ruin.*

A block-placing puzzle with the run structure of a deckbuilding roguelike. Drop flower seeds on a small garden, connect groups of one color, harvest lines, and turn every placement into damage against the pests.

[image 04-screenshot-place-and-chain.png]
Plant a seed, chain its color, harvest the line. Bigger chains, bigger Mult.

## What you do
- **Plant** flower seeds and **chain** colors on a 6×6 garden
- **Build** a bag of seeds and a rack of fertilizers worth bragging about
- **Beat** 12 rounds, with a boss every four. Enemies are drawn differently every run
- **Unlock** six seasons, from gentle Spring to the cramped Greenhouse

## Grow your build
Over a hundred fertilizers and sprout upgrades: stack Mult, bend the rules, break the score.
[image 05-screenshot-fertilizers.png]

## Pests strike back
Locks, poison and thieves fight on the board itself, and bosses wait at the end of every act.
[image 06-screenshot-pests.png]

## Six seasons
Harsher weather, fewer colors, tighter budgets. Earn Stars to unlock each season.
[image 07-screenshot-seasons.png]

## Made for touch
One finger, no hover, no tiny buttons. Plays in your browser on iPad or desktop: add it to your home screen for fullscreen.
[image 08-screenshot-touch.png]

## Controls
- Drag a seed onto the garden. Tap a seed or a tile to read it.
- Shuffle redraws your hand (a few per run).
- Desktop: Tab opens the Seed Bag, Esc opens Settings.

## Notes
LASTBLOOM is an early, evolving build. Saves live in your browser. Feedback is very welcome in the comments.

## Credits
Design, code and art: Félix Bernier. Fonts: Digitalt and Patrick Hand (SIL Open Font License).
```

## 5. Metadata
| Champ | À mettre |
|---|---|
| **Genre** | Puzzle |
| **Tags** (10 max) | `puzzle`, `roguelike`, `deckbuilder`, `block-puzzle`, `cute`, `casual`, `strategy`, `touch-friendly`, `singleplayer`, `short` |
| **Made with** | Unity |
| **Average session** | A few minutes (ou « About an hour » pour une run complète) |
| **Languages** | English |
| **Inputs** | Touchscreen, Mouse |
| **Multiplayer** | None |

## 6. Images
1. **Cover image** : `01-cover-1260x1000.png` (si itch la refuse, prends `01b-cover-630x500-fallback.png`).
2. **Screenshots** : ajoute `04` à `08` **dans cet ordre** (le premier est celui qu'on voit en grand).
3. Rappel : ces 5 images sont des illustrations. **Remplace les n° 04 et 05 par de vraies captures du build** avant de rendre la page publique.

## 7. Thème (Edit theme)
- **Banner** : `02-banner-1920x500.png`
- **Background image** : `03-background-1920x1080.png`, répétition **none**, position **top center**, **fixed**
- **Background color** `#12304a` · **Text color** `#fff7e8` · **Links** `#ffe9a8` · **Buttons** `#4e9a45`

## 8. Visibilité : ne publie pas tout de suite
1. En bas de la page de modification, choisis **Draft** ou **Restricted** (accessible seulement avec un lien), puis **Save**.
2. Clique **View page** pour voir le résultat.

## 9. À tester sur la page, sur l'iPad (Safari)
- [ ] Le jeu se lance au toucher, le premier toucher active le son
- [ ] Glisser une graine fonctionne, rien ne défile ou ne zoome
- [ ] Pas d'infobulle ouverte à l'arrivée dans la boutique
- [ ] « Sur l'écran d'accueil » donne le plein écran
- [ ] Une run complète de 12 manches, puis Save & quit / Continue
- [ ] La cover et le banner sont nets, le texte se lit sans zoom

## 10. Publier
1. Quand tout est bon : visibilité **Public**, puis **Save**.
2. Poste le **devlog n°1** (texte dans `PAGE_LAYOUT.md`, section « Devlog »).

## 11. Les builds suivants (optionnel, plus rapide)
Installe l'outil `butler` (itch.io/docs/butler), puis une seule fois : `butler login`. Ensuite, pour chaque nouveau build :
```
butler push chemin/vers/WebBuild felixbernier/lastbloom:html5
```
(`felixbernier/lastbloom` = ton compte et l'URL du projet ; `html5` est le nom du canal. Le dossier doit contenir `index.html` à la racine.)
