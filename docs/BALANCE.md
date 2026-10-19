# Équilibrage : où sont les chiffres, comment les mesurer

## Où vivent les chiffres

| Quoi | Fichier |
|---|---|
| Graines par round (budget), quotas de score (Classic : affichage seulement, les rounds se gagnent en tuant les ennemis) | `Core/Run/RunConfig.cs` (`PieceBudgets`, `Quotas`) ; variantes dans `Core/Run/ChallengeCatalog.cs` |
| Ennemis par round | `Core/Enemies/EncounterCatalog.cs` |
| PV, rôle et texte de chaque ennemi | `Core/Enemies/EnemyCatalog.cs` |
| Constantes de score (ligne récoltée 3/6/9, ×2 grille vidée, fractions Sap Drain/Siphon et plafond, bonus dorés…) | `Core/Scoring/ScoringConstants.cs` |
| Prix des fertilizers | `Core/Economy/ModifierPricing.cs` ; économie générale dans `Core/Economy/EconomyConstants.cs` |
| Effet de chaque fertilizer | `Core/Grid/GridManager.cs` (tables `BuildPreClearEffects` / post-clear) et `Core/Modifiers/ModifierDefinition.cs` (texte) |

## Mesurer avant de toucher

```
cd Tools/Headless/RunSim
dotnet run -- --runs 2000 --policy greedy
```

Lire : taux de réussite par round (« Cleared-round rate »), round où les runs finissent, cause des défaites, fertilizers
corrélés avec les runs profondes. Le bot est volontairement simple : ses chiffres servent à **comparer avant/après**, pas à
estimer le niveau d'un bon joueur. Relancer avec la même graine (`--seed`) pour comparer deux versions.

## Historique des réglages

- Round 2 : 24 → 28 graines, Bramble 750 → 600 PV (réussite du bot 58 % → 87 %).
- Round 8 : 22 → 28 graines, Deadroot 2350 → 2000 PV (27 % → 54 %).
- Blightmoth : debut moved from round 11 to round 7 (replaces the Bramble there), 1750 → 800 PV so round 7 stays near its neighbours (64 % for the bot).
- Sap Drain : 25 % → 8 % des dégâts en Pollen, Siphon : 15 % → 4 %, plafond de 10 Pollen par déclenchement.

## Météo et ennemis à minuterie

- **Météo** (`Core/Run/WeatherCatalog.cs`, constantes dans `ScoringConstants`) : tirée une fois par run, le round 1 est toujours dégagé et deux rounds de suite n'ont jamais la même météo. Pluie : +3 Pollen par graine Iris ; Soleil : +4 pts par case de graine Tournesol ; Vent : un Shuffle gratuit ; Gel : 2 cases gelées. La météo du round suivant est annoncée dans la boutique (boîte « Next round », avec tooltip) et la météo courante est dans la puce à côté du numéro de round.
- **Ennemis à minuterie** : Bramble et Blightmoth agissent aussi toutes les 6 graines posées (`ScoringConstants.EnemyTickInterval`), en plus du Shuffle. Le compte à rebours est affiché sur leur portrait.
- Les tests qui comptent des cases verrouillées/gelées appellent `DebugDisableWeather()` pour ne pas dépendre de la météo tirée.

## Connu mais pas traité

- Les rounds 3 à 6 restent les plus durs pour le bot (58–75 % de réussite) ; le round 3 est maintenant le premier mur.
- `RunConfig.Quotas` n'est plus la condition de victoire de Classic (restée pour l'affichage et Marathon/Chaos).

## Après un changement de valeur

L'étiquette dans le coin d'une icône de fertilizer est écrite dans `Design/Icons/generate_icons.py`. `python3 Tools/check_icon_tags.py`
(lancé aussi par le CI headless) échoue si une étiquette n'est plus dans la description du fertilizer ; corriger alors la table du générateur,
relancer `generate_icons.py` et re-rendre le PNG dans `Assets/Resources/Icons/Fertilizers/`.
