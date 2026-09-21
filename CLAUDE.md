# FIEF — règles de travail sur ce projet

## Contexte

Jeu de stratégie/économie médiévale multijoueur, 3D low-poly, parties de 30-60 min
(« Saisons ») à 4-6 joueurs, destiné à Steam. Unity 6 + C#.

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
| Vue | **Première personne par défaut** (révisé le 21/09/2026 par Martin). La 3e personne orbitale existe toujours et se récupère avec **V** — utile pour regarder son personnage, et c'est elle qui tourne sur l'écran-titre. En 1re personne le corps suit le regard, la tête est masquée, mais **le poncho, les mains, le bâton et les pieds restent visibles** : on est dans le personnage, pas derrière une caméra qui flotte. ZQSD/WASD + souris. |
| Map | **~2200×2200 m — 484 hectares** (agrandie le 21/09/2026), **dessinée à la main**, marché au centre, 6 fiefs en étoile à 750 m. **Les châteaux sont cachés les uns des autres** par des collines-barrières posées sur les lignes de vue : 12 collines dont la crête dépasse de 57 à 67 m la ligne œil→tour. **En solo, seuls le marché et le château du joueur sont construits** (`showRivalFiefs = false`). |
| Ressources | 3 en v1 : Bois, Pierre, Fer. Positions pseudo-aléatoires **dans des zones fixes** |
| Poids | Mécanique centrale. **Révisée le 20/09/2026 par Martin** : la charge ralentit surtout les **gestes** (récolte, actions), et seulement un peu la marche (−26 % à pleine charge). Un joueur chargé reste mobile mais ne peut plus courir : il demeure rattrapable, donc vulnérable en Phase 2. Ne pas revenir à l'ancienne version (vitesse divisée par 3), elle rendait le jeu pénible. |
| Construction | Sur **emplacements définis**, payée en or. Les 6 emplacements sont dans la cour du château. |
| Monnaie | L'Or, unique |
| Combat | Simple et lisible (mêlée + arc). L'intérêt est tactique/préparatoire |
| Persistance | **Aucune** entre parties |
| Réseau | Listen-server + Steam P2P. **Autorité serveur absolue sur l'économie** |

## Le différenciateur à protéger

**Le sabotage par les salaires.** Chaque fief a des PNJ serviteurs avec un salaire et une
jauge de loyauté. Un serviteur mal payé devient corruptible : un rival le soudoie pour
qu'il ouvre la porte dérobée, empoisonne les vivres ou affaiblisse un mur.
**La meilleure façon de prendre un château n'est souvent pas la force, mais la trahison
achetée.** Aucun jeu ne fait ça. Rien ne doit diluer cette idée.

Second pilier : **la destruction à règles**. On peut tout casser, mais pas avec n'importe
quoi. Porte dérobée = impossible par la force, uniquement par quelqu'un de l'intérieur.

## Phases — ne jamais travailler sur une phase ultérieure sans demande explicite

- **PHASE 1 — boucle économique solo** ← *on est ici*
  Porte 1 : la boucle récolter→vendre→construire est-elle satisfaisante 20 min en solo ?
- **PHASE 2 — le conflit.** Porte 2 : un raid crée-t-il une histoire qu'on se raconte après ?
- **PHASE 3 — multijoueur.** Porte 3 : une Saison de 30-45 min à 4 joueurs sans crash ni désync.
- **PHASE 4 — la Saison complète.** Timer, Prestige, victoire, anti-snowball, équilibrage.
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
