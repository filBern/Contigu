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
   - **Viewport dimensions** : `980` × `620` (le cadre du jeu sur la page ; le jeu s'y adapte en gardant ses proportions, il peut avoir de fines marges)
   - **Mobile friendly** : coché, orientation **Landscape**
   - **Fullscreen button** : coché
   - **Automatically start on page load** : **décoché** (le premier toucher doit lancer le son sur iPad)
   - **Enable scrollbars** : décoché
   - **SharedArrayBuffer** : **décoché**

## 4. Description (en HTML)
Le texte est dans **`description.html`** (même dossier), prêt à coller, balises `h1`, `h2`, `p`, `ul`, `img`.
1. Dans l'éditeur de la description, passe en **mode source / HTML** (bouton `<>` ou « Source » dans la barre).
2. Colle tout le contenu de `description.html` (sans le commentaire du début si l'éditeur le montre).
3. Les images : téléverse `04` à `08` avec le bouton image de l'éditeur, copie l'URL de chacune, puis remplace `IMAGE_04` à `IMAGE_08` dans le HTML par ces URL.
4. Enregistre et regarde l'aperçu : les images doivent être pleine largeur, les titres sur leur propre ligne.
5. **Si les images ne se chargent pas dans la description** : utilise `description-text-only.html` (même texte, sans images) et mets les 5 visuels dans la galerie **Screenshots** (étape 6), qu'itch affiche sous le jeu. Les images restent visibles, seulement pas au milieu du texte.

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

## 7. Thème (Edit theme) : pas de banner, un fond qui encadre le jeu
- **Banner** : laisse **vide** (le fond fait le travail).
- **Background image** : `03b-background-embed-1920x1080.png`, répétition **none**, position **top center**, attachement **fixed**. Le centre est volontairement sombre et calme : c'est là que se placent le jeu et le texte ; le logo, Sprig, les tuiles et les nuisibles sont sur les côtés.
- **Background color** `#12304a` (visible si l'écran est plus grand que l'image) · **Text color** `#fff7e8` · **Links** `#ffe9a8` · **Buttons** `#4e9a45`
- Si tu préfères un bandeau en haut plutôt que ce fond, utilise `02-banner-1920x500.png` et `03-background-1920x1080.png` (fond sobre).

## 7b. Image de l'embed (980×620)
`03c-embed-background-980x620.png` (et la version nette `03c-embed-background-1960x1240.png`) est l'image faite pour **remplir le cadre du jeu** avant le lancement : logo, Sprig, tuiles, et le **bas du centre laissé libre** pour le bouton de lancement d'itch. Je ne connais pas de champ d'itch dédié à cette image : si tu en vois un (ou si tu l'utilises comme image de couverture ou comme fond de ta page), mets-la là. Dis-moi ce que propose ton tableau de bord et j'ajuste.

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
