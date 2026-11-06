# Ajuster l'interface sans toucher au code

Toute l'interface est construite par le code au lancement : une scène Unity ne contient donc pas les écrans à déplacer à la main. À la place, les valeurs qu'on règle à l'œil sont dans **un seul fichier de réglages**, et des **scènes de démo** ouvrent chaque écran déjà rempli.

## 1. Le fichier de réglages

`Assets/Resources/UITweaks.asset` (sélectionne-le dans le Project, tout est dans l'Inspector, avec des infobulles) :

| Groupe | Ce qu'on y règle |
|---|---|
| Boutique : étagères | taille et espacement des articles, marge des étagères, taille des icônes d'upgrades |
| Boutique : étiquette de prix | couleurs, inclinaison, taille du chiffre, de l'icône, décalage |
| Écrans de choix | descente de l'étagère du haut, tailles de cases (graines, icônes, rangée seule), taille des icônes dans les cases, espacement |
| Plateau | marges et écart des colonnes, planche de bois (couleurs, débord), taille et espacement des emplacements de la main |
| Menu principal | hauteur du logo, de la phrase, des boutons, position et taille de Sprig, rythme de son saut |
| Sélecteur de challenge | largeur, hauteur de la carte, hauteur de la bannière |
| Animations | délai avant le reflet qui traverse le plateau, durée du vol d'un fertilizer |

Les valeurs par défaut sont celles du jeu. Pour revenir en arrière : clic droit sur le champ, **Revert**, ou supprime l'asset et lance `python3 Tools/gen_uitweaks_asset.py`.

## 2. Les scènes de démo

Dossier `Assets/Scenes/Sandbox/` : une scène par écran, avec une run d'exemple (4 fertilizers, des tuiles améliorées, du Pollen) :
`00_MainMenu`, `01_Board`, `02_Shop`, `03_RoundSummary`, `04_FertilizerPack`, `05_SproutPack`, `06_SproutSeedChoice`, `07_Forge`, `08_Compost`, `09_MasteryPack`, `10_SeedPack`, `11_Transplant`, `12_Victory`, `13_Defeat`, `14_ChallengeSelect`.

1. Ouvre une scène, lance **Play**.
2. Sélectionne `UITweaks.asset` et change une valeur dans l'Inspector : **la scène se recharge toute seule une demi-seconde plus tard**, avec la nouvelle valeur. (`R` recharge à la main.)
3. Les modifications d'un asset faites pendant Play **restent enregistrées** à la sortie de Play. Commite `UITweaks.asset` : c'est tout.

Ces scènes ne servent qu'à l'éditeur et aux builds de debug (`CONTIGU_DEBUG`) ; elles ne sont pas dans les Build Settings.

## 3. Ajouter un réglage

1. Ajouter un champ public dans `Assets/Scripts/Presentation/UITweaks.cs` (avec `[Tooltip]` et `[Range]` si utile), valeur par défaut = valeur actuelle.
2. Remplacer la constante du code par `UITweaks.Current.<champ>` (une constante `const` devient une propriété `static`).
3. `python3 Tools/gen_uitweaks_asset.py` (met à jour l'asset ; ne l'exécute pas pour « réinitialiser » des valeurs que tu as réglées).
