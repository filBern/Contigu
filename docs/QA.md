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

Limite : Unity ne tourne pas dans cet environnement. Le Core est testé pour de vrai ; l'interface est auditée par lecture de code seulement.
