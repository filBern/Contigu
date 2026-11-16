# Plan de dossier : Fonds des médias du Canada (FMC)

Plan de travail pour préparer une demande au **Fonds des médias du Canada (Canada Media Fund, CMF)** pour LASTBLOOM. Ce n'est pas un formulaire : c'est la liste de ce qu'il faut vérifier, rassembler et rédiger, avec ce que le dépôt fournit déjà.

> **À confirmer d'abord auprès du FMC** (cmf-fmc.ca, ligne d'information). Une recherche web (nov. 2026) donne des sources tierces qui se contredisent. Tout ce qui est marqué **[À CONFIRMER]** est une hypothèse, pas un fait.

## 1. À vérifier avant d'écrire une ligne

| Question | Pourquoi | Statut |
|---|---|---|
| Quel volet : *Innovation and Experimentation* ou *Commercial Projects* (ancien « Experimental Stream ») ? | Les critères, les montants et les attentes (éditeur, historique) diffèrent | **[À CONFIRMER]** |
| Dates des rondes (deux par an ?) et date limite de la prochaine | Une ronde a peut-être ouvert fin août 2026 | **[À CONFIRMER]** |
| Admissibilité : entreprise à but lucratif sous contrôle canadien, siège au Canada | Un projet porté par une personne seule n'est en général pas admissible | **[À CONFIRMER]** |
| Faut-il un historique de jeux publiés, un producteur expérimenté ou un éditeur ? | Si oui, il faut un partenaire avant le dépôt | **[À CONFIRMER]** |
| Plafonds et taux de couverture (sources tierces : de 15 000 $ à 1,5 M$ ; jusqu'à 75 % des coûts admissibles) | Détermine la taille du projet à présenter | **[À CONFIRMER]** |
| Le financement est-il remboursable (récupérable sur les revenus) ? | Change le plan financier | **[À CONFIRMER]** |
| Quelles étapes sont financées (conception, prototype, production, mise en marché) ? | Le jeu est déjà bien avancé : il faut viser la bonne étape | **[À CONFIRMER]** |
| Dépenses admissibles, et à partir de quelle date | Les coûts déjà engagés ne comptent peut-être pas | **[À CONFIRMER]** |

## 2. Prérequis administratifs (hors dépôt)

- [ ] **Société** canadienne constituée, contrôlée par des Canadiens, avec NEQ / numéro d'entreprise. Si elle n'existe pas, c'est la première étape.
- [ ] **Compte FMC** (portail de dépôt) et personne-ressource désignée.
- [ ] **Titularité de la propriété intellectuelle** : tout le code, l'art et le son appartiennent à la société, ou sont sous licence écrite.
- [ ] **Inventaire des éléments tiers** (voir §6, risque IP).
- [ ] **Comptabilité** : comptes distincts, justificatifs des dépenses.
- [ ] **Crédit d'impôt pour titres multimédias (Québec)** : à évaluer en parallèle ; il suppose une entreprise avec employés et des dépenses admissibles. **[À CONFIRMER]**

## 3. Sections du dossier : contenu, matériel disponible, reste à produire

### 3.1 Entreprise et équipe
- **Contenu** : présentation de la société, rôles (design, programmation, art, son, production), parcours, jeux ou projets antérieurs.
- **Déjà disponible** : rien dans le dépôt (le fil de commits montre une production en continu depuis le 14 sept. 2026, plus de 1 100 commits).
- **À produire** : CV de chaque personne, organigramme, contrats ou lettres d'engagement des collaborateurs.

### 3.2 Description du projet
- **Contenu** : pitch d'une phrase, genre, public cible, plateformes, durée d'une partie et d'une campagne, ce qui rend le jeu unique.
- **Déjà disponible** : `docs/GDD.md` §1 (pitch), §2 (boucle de jeu).
- **Pitch type** : *un puzzle de pose de pièces façon Block Blast bâti comme un roguelike à synergies façon Balatro, dans un jardin cartoon : on plante des graines, on récolte des lignes, on fait mûrir ses fertilizers et on affronte des nuisibles.*
- **À produire** : fiche d'une page, version courte (150 mots) et version longue.

### 3.3 Conception et contenu
- **Contenu** : règles, progression, économie, contenu actuel et prévu.
- **Déjà disponible** : `docs/GDD.md` (§3 à §8), `docs/GLOSSARY.md`, `docs/PROGRESSION.md`, `docs/BALANCE.md`.
- **Chiffres actuels (à reverifier le jour du dépôt)** : 6 challenges, 12 manches par campagne, environ 130 fertilizers, environ 90 tile upgrades, 16 ennemis, 22 météos, boutique à deux étagères (Blister, Workshop), pledges facultatifs.
- **À produire** : résumé de 2 pages tiré du GDD, avec un schéma de la boucle (manche → boutique → manche).

### 3.4 Innovation (surtout pour le volet Innovation et expérimentation)
- **Contenu** : en quoi le projet fait avancer le médium, et pas seulement « un jeu de plus ».
- **Arguments possibles, tirés du jeu** :
  - hybride de pose de pièces et de roguelike à synergies, pensé **pour le tactile** (un doigt, aucun survol) ;
  - **Maturation** et **Roots** : les fertilizers grandissent d'une manche à l'autre et se nourrissent entre voisins, ce qui donne une profondeur propre au jeu plutôt qu'un système d'éditions copié ;
  - outillage : **simulateur d'équilibre** (bots qui jouent des milliers de parties, progression mesurée) et plus de 650 tests automatisés sur la logique, qui garantissent un équilibre mesuré plutôt que deviné ;
  - interface entièrement construite à l'exécution, ce qui permet d'adapter rapidement les écrans et d'itérer.
- **À produire** : un paragraphe d'une page, avec une comparaison honnête aux jeux cités (Balatro, Block Blast, Inscryption) et ce qui diffère.

### 3.5 État d'avancement
- **Contenu** : ce qui existe et fonctionne.
- **Déjà disponible** : build jouable (WebGL pour itch.io, iPad testé), `docs/DECISIONS.md` (historique daté), `docs/QA.md` et les rapports de `docs/qa/`, tests (`cd Tools/Headless/Tests && dotnet test`), simulateur (`Tools/Headless/RunSim`).
- **À produire** : un build de démonstration stable, une capture par écran clé, une vidéo de gameplay.

### 3.5b Marché, public et comparables
- **Contenu** : public (joueurs de roguelikes à cartes, de puzzles courts, de jeux « cozy »), comparables chiffrés, positionnement.
- **À produire** : 4 à 6 comparables avec leurs ventes publiques, le prix visé, le segment mobile (iPad d'abord) et PC.
- **Piste éditeur** : voir la discussion avec les éditeurs possibles (Playstack, Devolver, Raw Fury…), à citer comme *intérêt exploré* seulement s'il y a un échange réel.

### 3.6 Plan de mise en marché et de distribution
- **Contenu** : plateformes (Steam, iPad / App Store, éventuellement Apple Arcade), calendrier, plan de liste de souhaits, démo publique, présence en festivals.
- **À produire** : calendrier de mise en marché sur 12 mois, budget marketing, page Steam à mettre en ligne tôt.

### 3.7 Plan de production et jalons
- **Contenu** : ce qui reste à faire, étape par étape, avec des livrables vérifiables.
- **Reste à faire visible dans le dépôt** : Undo d'une pose (décision de conception ouverte), passe QA complète sur un build à jour, localisation (les textes sont en anglais), son et musique définitifs, ajustements de rythme, tutoriel et premières minutes, version Steam et iPad finales.
- **À produire** : jalons trimestriels (alpha, bêta, version 1.0), critères de sortie de chaque jalon, risques.

### 3.8 Budget et plan de financement
- **Contenu** : coûts par poste, sources de financement, part demandée au FMC.
- **Modèle à remplir** (aucun chiffre n'est inventé ici) :

| Poste | Coût | Part FMC | Autre source |
|---|---|---|---|
| Programmation et outillage | | | |
| Art (personnages, interface, animations) | | | |
| Son et musique | | | |
| Conception, production, QA | | | |
| Localisation | | | |
| Mise en marché (trailer, page, démo, festivals) | | | |
| Frais juridiques et comptables | | | |
| Frais généraux admissibles | | | |
| **Total** | | | |

- **À produire** : budget complet, calendrier de trésorerie, financement de la part non couverte (fonds propres, crédit d'impôt, éditeur).

### 3.9 Revenus et viabilité
- **Contenu** : modèle économique (achat unique, prix, éventuellement contenu additionnel), scénarios de ventes prudent, réaliste, optimiste, seuil de rentabilité.
- **À produire** : feuille de calcul sur 3 ans avec hypothèses écrites.

### 3.10 Risques et parades
- **Marché saturé de jeux « façon Balatro »** : insister sur la différenciation (§3.4) et sur le tactile.
- **Portée** : le contenu est déjà large ; le plan de production doit nommer ce qui ne sera *pas* fait.
- **Dépendance à un éditeur** : montrer un plan B (auto-édition Steam et App Store).
- **Propriété intellectuelle de tiers** : voir §6.

## 4. Annexes à préparer

- [ ] Bande-annonce de 60 secondes (une manche complète, une synergie de fertilizers qui s'emballe, la météo, un boss).
- [ ] 8 à 10 captures d'écran propres (menu, grille, boutique, Collection, boss…).
- [ ] Build jouable (lien ou fichier) et mode d'emploi en 5 lignes.
- [ ] GDD en une page et extraits de `docs/GDD.md`, `docs/BALANCE.md`.
- [ ] Page de chiffres du simulateur (taux de victoire par challenge, rythme de déblocage ; voir `CLAUDE.md`, section « Équilibre »).
- [ ] CV de l'équipe, lettres d'appui ou d'intention (éditeur, distributeur, partenaire).
- [ ] Budget, calendrier, plan de financement.

## 5. Calendrier de préparation (à ajuster à la date limite réelle)

| Semaine | Travail |
|---|---|
| 1 | Appeler le FMC, confirmer volet, dates, admissibilité. Régler la question de la société. |
| 2 | Inventaire de la propriété intellectuelle (§6). Rédiger pitch, description et innovation. |
| 3 | Budget, plan de financement, calendrier de production. |
| 4 | Plan de mise en marché, comparables, scénarios de revenus. Bande-annonce et captures. |
| 5 | Assemblage, relecture, lettres d'appui, test de dépôt sur le portail. |
| 6 | Dépôt, avec une marge d'au moins quelques jours avant la date limite. |

## 6. Risque à régler tôt : éléments tiers dans le jeu

Le dépôt contient des éléments qui ne viennent pas d'un travail original et dont les licences doivent être **documentées par écrit** avant tout dépôt (le FMC et un éventuel éditeur demanderont qui détient quoi) :

- le pack d'interface `Assets/Resources/Colorful_UI/` ;
- la police `Assets/Resources/Font/PatrickHand-Regular.ttf` (licence à confirmer) ;
- les sons et la musique de `Assets/Resources/SFX/` ;
- tout autre pack ou outil tiers sous `Assets/`.

À faire : un tableau (élément, origine, licence, usage commercial autorisé ?, remplacement prévu si non).

## 7. Questions à poser au FMC

1. Mon projet relève-t-il d'*Innovation and Experimentation* ou de *Commercial Projects* ?
2. Un éditeur ou une expérience antérieure sont-ils obligatoires pour ce volet ?
3. Une société sans jeu publié est-elle admissible ? Avec quelles conditions ?
4. Quelle est la prochaine date limite, et quand sont rendues les décisions ?
5. Les dépenses déjà engagées sont-elles admissibles ? À partir de quelle date ?
6. Le financement est-il remboursable ? Sur quelle base ?
7. Quel est le plafond pour un jeu de cette taille et quel pourcentage des coûts est couvert ?
8. Quels documents sont obligatoires au dépôt (états financiers, lettres, contrats) ?
9. Peut-on cumuler le FMC avec le crédit d'impôt provincial et d'autres fonds ?

## 8. Suites possibles

- Rédiger les sections 3.2 à 3.5 dans leur forme finale à partir du GDD.
- Préparer le tableau de l'inventaire de propriété intellectuelle (§6).
- Lister les captures et les scènes de la bande-annonce.
- Construire la feuille de calcul du budget une fois les réponses du FMC obtenues.
