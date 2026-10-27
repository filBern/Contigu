# QA

Un agent QA (`.claude/agents/qa-tester.md`) cherche les bugs, teste les limites et relève les incohérences et les manques de clarté. Lancement : `/qa` (passe complète) ou `/qa boutique` (un thème). Il écrit son rapport dans `docs/qa/`.

Ce qu'il utilise, et que tu peux lancer toi-même :

| Outil | Rôle |
|---|---|
| `cd Tools/Headless/Tests && dotnet test` | tests des règles (Core) |
| `python3 Tools/qa/content_audit.py [--strict]` | contenu manquant ou incohérent : id sans description, prix, icône ou texte de tooltip, doublons de noms, textes douteux |
| `python3 Tools/check_icon_tags.py` | pastilles des icônes de fertilizers contre leurs descriptions |
| `cd Tools/Headless/RunSim && dotnet run --no-build -- --each --runs 8` | joue des runs où chaque fertilizer, puis chaque sprout sur toutes les graines, est seul en jeu (Classic et Orchard), avec les invariants : un plantage dans un contenu précis ressort par son nom |
| `cd Tools/Headless/RunSim && dotnet run --no-build -- --policy random --runs 100 --challenge Classic --check` | joue des runs complets et vérifie après chaque action des invariants (Pollen ≥ 0, sac ≥ taille minimale, PV d'ennemi cohérents, glace cohérente, sauvegarde qui se relit à l'identique…) ; `Tools/Headless/RunSim/Invariants.cs` |

## Tester un build WebGL

Le jeu embarque un crochet de test (`QaBridge`, actif seulement quand la page est ouverte avec `?qa=1`). Il publie les boutons, les textes et l'état de la partie, et exécute de vrais clics à travers le système d'événements d'Unity.

1. Build Settings → WebGL. **Compression : Disabled** (sinon `serve.py` sait servir `.br`/`.gz`, mais c'est plus fragile). Coche **Development Build** pour des erreurs lisibles. Pour les raccourcis de triche (`grantPollen`, `forceRoundWin`), ajoute le symbole `CONTIGU_DEBUG` dans Player Settings → Scripting Define Symbols ; sans lui ces deux commandes répondent « build without CONTIGU_DEBUG » et le reste marche.
2. Mets le dossier du build dans le dépôt (par exemple `Builds/WebGL`, sur une branche à part si c'est lourd) et pousse-le.
3. `cd Tools/qa/web && node smoke.js ../../../Builds/WebGL --rounds 3` : charge le jeu, passe par le menu et la collection, lance une partie, joue des coups légaux tirés de l'état, visite la boutique, et échoue à la moindre erreur de console ou état impossible. Captures dans `qa-shots/`.
4. `qa.js` expose les briques pour écrire d'autres scénarios (`state`, `click`, `clickText`, `realClick`, `hover`, `gameCommand`, `idle`, `shot`). Les coordonnées sont les pixels Unity, origine en haut à gauche.

`node` + `playwright` + Chromium sont déjà installés dans l'environnement cloud. Le harnais est vérifié contre une page simulée (`Tools/qa/web/mock`), pas encore contre un vrai build Unity.

Limite : Unity ne tourne pas dans cet environnement. Le Core est testé pour de vrai ; l'interface est auditée par lecture de code seulement.
