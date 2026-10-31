# LASTBLOOM sur iPad

## Chemin A — WebGL dans Safari (le plus rapide, fonctionne depuis Windows)
1. Build Profiles → WebGL. **Ne pas** ajouter `CONTIGU_DEBUG` (le pont QA est réservé aux builds de test).
2. Player Settings → Publishing Settings → Compression Format : **Disabled** (simple à héberger).
3. Build dans un dossier à la racine du projet (ex. `WebBuild`, ignoré par git, jamais dans `Assets`).
4. Héberger le dossier en HTTPS (GitHub Pages, itch.io en « SharedArrayBuffer off », Netlify, etc.).
5. Sur l'iPad : ouvrir l'URL dans Safari → Partager → **Sur l'écran d'accueil** : plein écran, sans barre Safari.
6. Le premier toucher active le son (règle iOS).

Le post-process `Assets/Editor/WebGLMobilePostprocess.cs` s'applique tout seul au build : il fait reconnaître l'iPad (iPadOS se déclare « Macintosh »), ajoute les balises « web app » et bloque défilement, zoom et sélection.

## Chemin B — Build iOS natif
Exige **un Mac avec Xcode** (impossible depuis Windows) et le module *iOS Build Support* dans Unity Hub.
- Bundle id : `com.fbern.lastbloom` (modifiable dans Player Settings). Cible : iPad, paysage seulement.
- Dans Xcode : choisir l'équipe de signature, régler le nom affiché, brancher l'iPad, Run.
- Ne pas changer `productName` / `companyName` : cela déplacerait le dossier de sauvegarde.

## Comportement tactile
- Appui long sur un élément = infobulle (remplace le survol).
- Vendre un fertilisant : le toucher, puis bouton **Sell** ; toucher deux fertilisants = les échanger.
- Boutique : un premier toucher sélectionne l'objet (halo doré, infobulle maintenue, « Tap again to buy »), un second toucher dessus l'achète ; toucher ailleurs annule.
- Planter : toucher la graine puis la parcelle, ou la glisser.
- Le jeu se sauvegarde quand l'app passe en arrière-plan ; l'écran ne se met pas en veille ; multitouch désactivé ; 60 images/s.
- L'interface suit la largeur sur les écrans plus carrés que 16:10 (iPad 4:3) pour que rien ne sorte de l'écran.

## Non vérifié (Unity inutilisable ici)
Tout le code de présentation iPad est non compilé/testé : à vérifier sur appareil — taille des zones tactiles, appui long, bouton Sell, zones sûres (encoche/barre d'accueil), son après premier toucher, sauvegarde après fermeture de l'app web.
