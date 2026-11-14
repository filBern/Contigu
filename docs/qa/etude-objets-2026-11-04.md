# Étude de puissance des objets (2026-11-04)

Commande : `cd Tools/Headless/RunSim && dotnet run -c Release -- --study --runs 300 --seed 1000 --csv ...` (bot glouton, Classic, 12 manches). Données complètes : `etude-objets-2026-11-04.csv`.

Méthode : chaque fertilizer (tenu dès le départ) et chaque sprout (sur 3 graines) est joué sur les mêmes 300 seeds qu'une partie de référence ; on compare les manches réussies (écart moyen, erreur type, z). Référence : 3,23 manches, 7,7 % de victoires.

## À retenir
- **Piggy Bank (+0,1 Mult par Pollen tenu) est très au-dessus de tout** : +8,4 manches, 96 % de victoires (z = +41). Le bot garde son Pollen et le Mult explose. À revoir en priorité.
- Ensuite : Mult +4 (+4,2), Multiplier Beacon (+4,0, sprout), Multitude (+3,5), Overgrowth Glow (+3,3), Bounty (+3,3).
- Sprouts : seul Multiplier Beacon dépasse nettement le lot ; Miser Sprout (+1,5) suit.
- 48 objets sur 212 sont nettement au-dessus de la référence, 19 nettement en dessous (|z| > 2,5).
- Les « plus faibles » sont à -0,5 / -0,6 manche : c'est le coût de tenir un objet sans effet (il occupe un des 4 emplacements et décale le hasard), pas un objet nuisible. Lire ces valeurs comme « sans effet pour ce bot ».

## Limites
Bot glouton (pas un joueur) ; objets tenus dès le départ (prix et disponibilité ignorés) ; sprouts testés sur 3 graines tirées au hasard ; les synergies entre objets ne sont pas mesurées. Sert à l'équilibrage, jamais à conseiller le joueur.
