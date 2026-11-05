# Page itch.io de LASTBLOOM : kit visuel et texte

Voir aussi `PAGE_LAYOUT.md` pour l'ordre des blocs de la page et le texte bloc par bloc.

Tout ce qui est dans `out/` est généré par `build_itch.js` à partir des assets du jeu (logo, Sprig, tuiles, ennemis, icônes, fonds) :
`NODE_PATH=$(npm root -g) node build_itch.js` (une seule image : `... node build_itch.js cover`).

## Où mettre chaque image

| Fichier (`out/`) | Champ itch.io | Taille |
|---|---|---|
| `cover-630x500@2x.png` | **Cover image** (itch la réduit ; le 2x reste net) | 1260×1000 |
| `cover-630x500.png` | Cover image, version exacte si le 2x est refusé | 630×500 |
| `banner-1920x500.png` | Theme → **Banner** | 1920×500 |
| `background-1920x1080.png` | Theme → **Background image** (no-repeat, centré, fixe) | 1920×1080 |
| `screenshot-1…5-*.png` | **Screenshots** (dans cet ordre) | 1280×800 |
| `icon-512.png` | Icône pour l'écran d'accueil iPad / favicon, partage | 512×512 |

**Important : les 5 « screenshots » sont des illustrations** montées avec l'art du jeu, pas des captures. Unity ne tourne pas dans l'environnement où elles ont été faites. Pour la page publique, remplacer au moins les n° 1 (partie) et 2 (boutique) par de vraies captures du build WebGL (itch recommande des captures réelles), et garder les autres comme visuels d'ambiance.

## Thème de la page (Edit game → Theme)

- Background color `#12304a` · Text `#fff7e8` · Links `#ffe9a8` · Button `#4e9a45` · Button text `#ffffff`
- Layout : sidebar à droite ; embed en haut, description dessous.

## Réglages du projet

- Kind of project : **HTML**, archive du build WebGL (compression **Disabled**, voir `docs/IPAD.md`).
- Embed options : *Click to launch in fullscreen* + *Mobile friendly* (orientation paysage), viewport **1280×800** (ratio 16:10), *SharedArrayBuffer support* **désactivé**.
- Prix : gratuit (ou « pay what you want »). Genre : **Puzzle**. Plateforme : HTML5 (tablet friendly).

## Texte à coller

**Titre** : LASTBLOOM

**Short description** (≤ 100 caractères) :
> A garden-puzzle roguelike: plant seeds, harvest lines, grow a build, outlast the pests.

**Tags** (10 max) : `puzzle`, `roguelike`, `deckbuilder`, `block-puzzle`, `cute`, `casual`, `strategy`, `touch-friendly`, `singleplayer`, `short`

**Description** (version HTML prête à coller : `description.html`) :

```markdown
*The last garden still blooms, at the edge of ruin.*

**LASTBLOOM** is a block-placing puzzle with the run structure of a deckbuilding roguelike. Drop flower seeds on a small garden, connect groups of one color, harvest whole lines, and turn every placement into damage against the pests that are eating your plants.

Between rounds you visit the shop to build something that could only exist in your run: fertilizers that stack Mult, sprout upgrades that bend the rules of the board, packs of Wild Seeds, and a Forge for the upgrades you really want.

## What you do
- **Plant** seeds of four flowers (poppy, iris, sunflower, fern) on a 6×6 garden
- **Chain** colors and **harvest** lines for points and Pollen
- **Build** a bag of seeds and a rack of fertilizers worth bragging about
- **Beat** 12 rounds, with a boss every four. Enemies and bosses are drawn differently every run
- **Unlock** six seasons, from the gentle Spring to the cramped Greenhouse, and optional pledges for the brave

## Made for touch
One finger, no hover, no tiny buttons. It plays in your browser on iPad or desktop. Add it to your home screen for fullscreen.

## Notes
This is an early, evolving build. Saves live in your browser. Feedback is very welcome in the comments.
```

**Controls** : drag a seed onto the garden · tap a seed or a tile to read it · Shuffle to redraw your hand · (desktop) Tab opens the Seed Bag, Esc opens Settings.

**Credits** :
- Design, code and art: Félix Bernier
- Fonts: Digitalt (SIL Open Font License, see `Assets/Resources/Colorful_UI/colorful/font/OFL_License.txt`), Patrick Hand (SIL Open Font License)
- À vérifier avant la sortie publique : la licence du pack d'interface `Colorful_UI` (boutons et cadres), qui n'est pas documentée dans le dépôt.

## Checklist avant de publier
1. Remplacer les screenshots 1 et 2 par de vraies captures (iPad si possible).
2. Vérifier le build : ouverture sur iPad Safari, premier toucher = son, sauvegarde, 12 manches.
3. Ajouter une devlog courte (ce qui change à chaque build) : itch la met en avant sur la page.
4. Relire la licence de `Colorful_UI` (voir Credits).
