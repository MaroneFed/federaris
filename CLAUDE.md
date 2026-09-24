# FIEF — règles de travail sur ce projet

## Contexte

Jeu multijoueur de rivalité dans une forêt noire, en première personne, 3D low-poly,
parties de 30 min (« Saisons ») à 4-6 joueurs, destiné à Steam. Unity 6 + C#.

**Le jeu est décrit dans `docs/LA-SAISON.md`.** C'est la référence : on y lit la boucle,
les chiffres, et ce qui est en Phase 1 ou plus tard. Le lire avant de toucher au gameplay.

Martin développe seul (JS/web à l'origine, **débutant Unity et C#**), avec son frère
de 16 ans pour les assets et la map (débutant complet).

## Comment travailler ici

- **Petites étapes testables.** Après chaque étape, le jeu doit se lancer et montrer
  quelque chose qui marche. Pas de bloc de 500 lignes sans test.
- **Expliquer les concepts Unity au fur et à mesure** (GameObject, Prefab, Component,
  ScriptableObject). Le but est qu'il apprenne, pas qu'il copie.
- **Manipulation dans l'éditeur ⇒ donner le chemin exact des menus.**
- **Mauvais choix technique ⇒ le dire franchement.**

## Les 2 lois inviolables

1. **Look SIMPLE** — low-poly stylisé, assets de banques (Kenney, Synty, Asset Store).
   **Zéro création 3D sur mesure.** L'art ne doit jamais être un goulot.
2. **Parties de 30-60 min** — Saisons autonomes. **Pas de monde persistant**, pas de base
   de données, pas de serveur dédié.

## Décisions verrouillées (ne pas rediscuter)

| Sujet | Décision |
|---|---|
| Joueurs | 4 min, **6 cible** — l'architecture vise 6 dès le départ |
| Vue | **Première personne, et elle seule** (révisé le 22/09/2026 par Martin : « une vue et une seule »). La touche V et la 3e personne jouable sont supprimées ; le mode orbital ne sert plus qu'à l'écran-titre. ZQSD/WASD + souris, le corps suit le regard. |
| Corps subjectif | **On ne voit rien de soi en première personne** (tranché le 22/09/2026 après six tentatives). Dans l'ordre : masquer `CharacterRig` pièce par pièce (il en restait toujours une devant l'œil), un corps subjectif avec poncho et bâton (le poncho forme un **anneau** qui encercle l'image), le même réduit à quatre membres (des boîtes qui flottent, **noires** parce que `CharacterRig` resté en `ShadowsOnly` leur projette son ombre dessus). Ce qui donne le corps, c'est **l'ombre** : `CharacterRig` reste en `ShadowsOnly`, sa silhouette complète se projette au sol. Ne pas rajouter de mains, de bâton ou de vêtement en vue subjective sans que Martin le demande explicitement — c'est un vrai travail d'animation, pas un réglage.
| Monde | **LA SYLVE** (refonte du 23/09/2026 par Martin : « tu peux tout supprimer sauf le personnage et sa vue »). Forêt dense et sombre de **700 × 700 m**, brume à **14 m** (resserrée trois fois, les 23 et 24/09 : « le champ de vision doit être encore plus court »), relief doux tiré de la graine. Plus de marché, plus de fiefs, plus de routes ni de lacs : supprimés, pas désactivés. Au centre, **un seul grand château** (demandé par Martin le 23/09 : « un énorme château ») avec la stèle et les réserves de fer. Voir `docs/LA-SAISON.md`. |
| Arbres | **Bibliothèque partagée, jamais fusionnée.** 16 maillages d'arbres (sapin, hêtre, bouleau, mort) + 6 touffes + 4 blocs, instanciés ~15 000 fois. C'est l'inverse du `Batcher` et c'est volontaire : un objet fusionné ne sort pas du champ tout seul, des objets séparés si. **Ne jamais fusionner la forêt** — et se rappeler que le plantage du lancement venait de 17 600 *maillages distincts*, pas de 17 600 objets. Ce qui coûte, c'est le nombre de modèles. |
| Lumière | Brume exponentielle teintée (jamais grise), lumière rasante froide et faible qui **découpe** au lieu d'éclairer, ambiant sombre mais jamais noir, et **une lanterne portée par le joueur**. La lanterne n'est pas un ornement : sans elle, sombre veut dire « on ne voit rien » et le jeu devient pénible. |
| Ressources | 3 en v1, **renommées le 23/09/2026** : **Bois mort** (partout, 1 kg), **Pierre-lune** (dans les creux, 3 kg), **Fer ancien** (dans le château, 2 kg). Toujours trois, toujours placées dans des zones fixes. |
| Boucle | **Récolter → cacher → porter au mage → forger une relique → la poser sur la stèle.** Le mage errant n'accepte que ce qu'on *porte* : c'est ce qui oblige à ressortir sa cache et à traverser la forêt chargé. Détails et chiffres : `docs/LA-SAISON.md`. |
| Camp et caches | Un camp, planté **une seule fois**. Trois caches au plus, creusées n'importe où ; seul leur propriétaire sait où elles sont. |
| Stèles et vol | **Chacun sa stèle**, plantée une fois où il veut (révisé le 24/09/2026 par Martin). Seule la relique posée sur **sa** stèle compte. On peut **voler** celle des autres (60 % seulement se fond dans la sienne). Trois **rivaux PNJ** jouent avec exactement les mêmes règles que le joueur (classe `Seeker`). |
| Monnaie | L'Or, unique. Il sert à **soudoyer** (Phase 2), pas à acheter. |
| Combat | Simple et lisible. **Épée** fabriquée (2 bois, 3 fer), quatre coups tuent ; **qui porte une relique ne peut pas frapper** (Martin, 24/09). Tomber = tout lâcher dans une dépouille, se relever à sa stèle. L'arc reste à faire. |
| Outils | Deux emplacements (1 / 2) : **hache** (abattre les arbres) et **épée**, qui s'usent et cassent. L'outil tenu se voit à l'écran — **l'outil seul, jamais de mains** (la règle du corps invisible tient). |
| Persistance | **Aucune** entre parties |
| Réseau | Listen-server + Steam P2P. **Autorité serveur absolue sur l'économie** |

## Le différenciateur à protéger

**Le sabotage par les salaires.** Il n'y a plus de fief par joueur, mais le château
central a sa garde, **et elle est mal payée**. Chaque garde a une solde et une jauge de
loyauté ; un garde mécontent accepte de l'or pour ouvrir la poterne, détourner les yeux
ou éteindre une torche. **La meilleure façon d'entrer dans le château n'est pas la force,
mais la trahison achetée.** Aucun jeu ne fait ça. Rien ne doit diluer cette idée.

Second pilier : **la destruction à règles**. On peut tout casser, mais pas avec n'importe
quoi. Porte dérobée = impossible par la force, uniquement par quelqu'un de l'intérieur.

## Phases — ne jamais travailler sur une phase ultérieure sans demande explicite

- **PHASE 1 — la Saison en solo** ← *on est ici* (redéfinie le 23/09/2026)
  Porte 1 : une Saison solo de 30 minutes est-elle haletante du début à la fin ?
  Ressources, camp, caches, mage, relique, château, stèle, cloche — et depuis le 24/09 :
  six talismans, cinq lieux-dits, et des habitants qui ne se battent pas (Veilleur,
  Ermite, feux-follets, cerf blanc). Et, demandés explicitement par Martin le 24/09 :
  **chacun sa stèle, trois rivaux PNJ, le vol, six gardes et le soudoiement (poterne)**.
- **PHASE 2 — le conflit.** Porte 2 : un vol de relique crée-t-il une histoire qu'on se
  raconte après ? Ce qui reste : **pièges**, **combat** (mêlée + arc), rôdeur PNJ, et les
  gardes qui trahissent **pour les rivaux** aussi (le vol et le soudoiement existent déjà).
- **PHASE 3 — multijoueur.** Porte 3 : une Saison de 30-45 min à 4 joueurs sans crash ni désync.
- **PHASE 4 — la Saison complète.** Anti-snowball, événements, équilibrage fin.
- **PHASE 5 — vitrine.** Page Steam, démo, Next Fest, localisation EN.
- **PHASE 6 — lancement** en Early Access.

## Règle anti-dérive

Toute idée hors de la phase en cours va dans **`docs/v2-ideas.md`**, jamais dans le code.
Si Martin propose une idée hors-phase, lui rappeler cette règle.

## Règles techniques du dépôt

- Les fichiers `.meta` sont versionnés. **Ne jamais les supprimer** : ils portent l'identité
  (GUID) des assets, sans eux toutes les références se cassent.
- La logique de jeu ne vit pas dans les `MonoBehaviour` quand elle peut en être extraite
  (voir `docs/ARCHITECTURE.md`).
- Or, prix, stocks et inventaires ne se modifient **que** par les méthodes `Request*` / `Try*`.

## Le vérificateur

Claude ne peut pas lancer Unity : il ne voit jamais ses propres erreurs de compilation.
`Tools/verifier.py` les cherche à sa place. **À lancer avant chaque push**, et tu peux le
lancer toi-même avant d'ouvrir l'éditeur :

```
python3 Tools/verifier.py
```

Il attrape les cinq fautes qui sont *réellement* arrivées sur ce projet : accolades
déséquilibrées, appel à une méthode supprimée, référence à `Type.Membre` inexistant,
mauvais nombre d'arguments, type inconnu ou `[Header]` posé devant une méthode.

Si tu ajoutes du code utilisant un type Unity absent de `Tools/types-externes.txt`,
le vérificateur rouspète : ajoute son nom au fichier, une ligne chacun.
