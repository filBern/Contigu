# Wiki de conception

`./build.sh` extrait les catalogues du Core (fertilizers, tile upgrades, ennemis, météo, événements, challenges, déblocages, constantes) avec `Dump/`, empaquette les vraies icônes du jeu (`build_atlas.py`) et écrit `out/wiki.html` à partir de `wiki.template.html`. Les pages de prose (règles, interface, équilibrage…) sont dans le gabarit : à relire quand les règles changent. `out/` n'est pas versionné ; publier `out/wiki.html` avec `out/img/*.webp` comme fichiers d'accompagnement. Le guide de style du wiki est la page « Guide de style » du gabarit.
