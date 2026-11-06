# Notes de projet pour Claude (LASTBLOOM, repo Contigu)

Garde ce fichier court : seulement ce qui change la façon de travailler. L'état du jeu est dans `docs/GDD.md`, les décisions dans `docs/DECISIONS.md`.

## Règles de travail
- Répondre en français, court, direct. Dire clairement quand un changement de présentation n'est **pas vérifié dans Unity** (Unity ne tourne pas dans l'environnement cloud).
- Toujours committer et pousser sur la branche de la session ; ne jamais créer de PR sans demande. Messages de commit terminés par les lignes d'attribution demandées par le harnais.
- Ne pas enchaîner un commit après un test en échec. Quand un test échoue, le dire avec la sortie.
- Ne pas guider ni « booster » le joueur (pas d'indices, pas de tirages favorisés) : décision explicite de l'auteur.
- Le jeu est testé sur iPad via des builds itch.io : le tactile prime (pas de survol, cibles ≥ 56, un doigt).

## Commandes
- Tests Core : `cd Tools/Headless/Tests && dotnet test` (≈ 626 tests).
- Audits : `python3 Tools/qa/content_audit.py`, `python3 Tools/qa/glyph_audit.py`, `python3 Tools/check_icon_tags.py` (ce dernier est dans le CI).
- Simulateur : `cd Tools/Headless/RunSim && dotnet run -- --check --runs N --policy random|planner --seed S` (`PLAN_DEPTH=2`, `--challenge`, `--endless`, `--each`).
- Art : SVG générés par les scripts de `Design/` ; PNG rendus avec Chromium/Playwright (`NODE_PATH=$(npm root -g) node …`). Tout nouvel asset ou `.cs` a besoin d'un `.meta` avec un nouveau guid.

## Conventions de code
- `Core` = logique pure (pas de MonoBehaviour), testée ; `Presentation` = uGUI construit à l'exécution ; ne pas oublier les `using` (`Contigu.Core`, `Contigu.Data`) : le code de présentation n'est compilé que par le CI Unity.
- Ids sauvegardés en entiers : ne jamais supprimer un membre d'enum (le retirer du catalogue et le convertir à la reprise, voir `ModifierCatalog.RetiredReplacement`).
- Mouvement réduit : toute animation décorative passe par `MotionSettings.Reduced`.
- Toute modification d'un texte ou chiffre de fertilizer : relancer `check_icon_tags.py` (pastille de l'icône).

## Points ouverts
- Undo d'une pose : non fait. Le Core n'a pas de copie profonde de l'état d'une manche (le snapshot est celui du début de manche et ne garde pas l'ordre de la pioche), donc un vrai Undo demande une décision de conception (voir le résumé de la session du 2026-11-01).
- Bug mobile du bouton Seed bag jamais élucidé (soupçon : le bouton Sell le recouvre).
- Build WebGL `QaBuild/` à régénérer pour rejouer `Tools/qa/web/flows.js`, puis une passe QA sur les nouveautés (résumé de manche, pledges, Transplant, sons, bulles de Sprig).
- Équilibre : victoires du planner (profondeur 2) de 5 % à 14 % selon le challenge.
