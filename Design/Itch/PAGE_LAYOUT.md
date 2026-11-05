# Agencement de la page itch.io

Le schéma `out/page-layout-guide.png` montre la page finie. Les numéros ci-dessous sont ceux du schéma. Le texte à coller est en anglais (le jeu l'est), dans l'ordre de la page. **Version prête à coller en HTML : `description.html`** (les extraits Markdown ci-dessous ne servent qu'à lire le contenu bloc par bloc).

## De haut en bas

| N° | Bloc | Quoi mettre | Où (itch.io) |
|---|---|---|---|
| 1 | **Banner** | `banner-1920x500.png` | Edit game → Theme → Banner |
| 2 | **Le jeu** | Embed HTML5 (1280×800, plein écran au clic) | Uploads + Embed options |
| 3 | **Colonne de droite** | Cover `cover-630x500@2x.png`, statut, plateformes, genre, tags | Cover image, Metadata, Tags |
| 4 | **Accroche** (les 2 premières lignes sont ce qu'on lit avant de défiler) | Titre, devise, 2 phrases | Description |
| 5 | **Grande image** | `screenshot-1-place-and-chain.png` pleine largeur | Image insérée dans la description |
| 6 | **« What you do »** | 4 puces courtes | Description |
| 7 | **Bandeau d'images** | Les screenshots 2 à 5, côte à côte ou l'un sous l'autre | Screenshots (galerie) ou images de la description |

Règles : pas plus de 4 puces par liste, un titre `##` par section, une image entre deux blocs de texte, jamais deux blocs de texte de plus de 4 lignes à la suite. La galerie de screenshots d'itch montre les 5 images en vignettes sous le jeu : garder l'ordre 1 à 5.

## Le texte, bloc par bloc

### 4 · Accroche
```markdown
# LASTBLOOM

*The last garden still blooms, at the edge of ruin.*

A block-placing puzzle with the run structure of a deckbuilding roguelike. Drop flower seeds on a small garden, connect groups of one color, harvest lines, and turn every placement into damage against the pests.
```

### 5 · Image
`screenshot-1-place-and-chain.png`, avec cette phrase dessous :
```markdown
Plant a seed, chain its color, harvest the line. Bigger chains, bigger Mult.
```

### 6 · What you do
```markdown
## What you do
- **Plant** flower seeds and **chain** colors on a 6×6 garden
- **Build** a bag of seeds and a rack of fertilizers worth bragging about
- **Beat** 12 rounds, with a boss every four. Enemies are drawn differently every run
- **Unlock** six seasons, from gentle Spring to the cramped Greenhouse
```

### 7 · Les quatre autres images, avec une phrase chacune
```markdown
## Grow your build
Over a hundred fertilizers and sprout upgrades: stack Mult, bend the rules, break the score.
[image: screenshot-2-fertilizers]

## Pests strike back
Locks, poison and thieves fight on the board itself, and bosses wait at the end of every act.
[image: screenshot-3-pests]

## Six seasons
Harsher weather, fewer colors, tighter budgets. Earn Stars to unlock each season.
[image: screenshot-4-seasons]

## Made for touch
One finger, no hover, no tiny buttons. Plays in your browser on iPad or desktop: add it to your home screen for fullscreen.
[image: screenshot-5-touch]
```

### Fin de page
```markdown
## Controls
- Drag a seed onto the garden. Tap a seed or a tile to read it.
- Shuffle redraws your hand (a few per run).
- Desktop: Tab opens the Seed Bag, Esc opens Settings.

## Notes
LASTBLOOM is an early, evolving build. Saves live in your browser. Feedback is very welcome in the comments.

## Credits
Design, code and art: Félix Bernier. Fonts: Digitalt and Patrick Hand (SIL Open Font License).
```

## Dans les réglages (rappel)
Short description : `A garden-puzzle roguelike: plant seeds, harvest lines, grow a build, outlast the pests.`
Tags : `puzzle`, `roguelike`, `deckbuilder`, `block-puzzle`, `cute`, `casual`, `strategy`, `touch-friendly`, `singleplayer`, `short`.
Thème : fond `#12304a`, texte `#fff7e8`, liens `#ffe9a8`, bouton `#4e9a45`. Voir `PAGE.md` pour le reste des réglages et la checklist.

## Devlog n°1 (à publier avec le premier build)
```markdown
# LASTBLOOM is on itch.io

The first public build is up. Runs are 12 rounds long, with a boss at rounds 4, 8 and 12, and the pests you meet change from run to run.

What to try first: a Wild Pack (rare) in the Workshop, the Scythe Sprout that harvests a whole row and column, and Crop Rotation for a steady Mult that grows with variety.

Tell me what feels good and what feels off. I read every comment.
```
