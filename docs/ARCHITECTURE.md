# Architecture — pourquoi c'est découpé comme ça

Document court, à relire avant la Phase 3. L'objectif de ce découpage est **un seul** :
pouvoir greffer le combat, l'IA, la diplomatie et surtout le **multijoueur** sans réécrire.

---

## 1. La scène ne contient qu'un objet

`Main.unity` = un GameObject `FIEF (Bootstrap)` portant `GameConfig` + `GameBootstrap`.
Tout le monde (sol, marché, fiefs, gisements, joueur, caméra, HUD) est construit par code.

**Pourquoi :** une scène Unity est un fichier YAML de plusieurs milliers de lignes.
À deux sur le même projet, deux modifications de scène = un conflit Git impossible à
résoudre à la main. Ici, la map est du **code** : `GameConfig.DefaultZones()` se lit,
se relit et se merge normalement.

**Contrepartie :** on ne peut pas placer un arbre à la souris. C'est le bon compromis
tant que la map est faite de zones et d'emplacements, ce qui est la décision verrouillée
du brief. Quand ton frère voudra placer les choses à la main, on fera l'inverse :
un script éditeur qui *écrit* dans `GameConfig` ce qu'il a posé dans la scène.

## 2. Logique pure ≠ MonoBehaviour

| Classe C# pure (testable, sérialisable, réplicable) | MonoBehaviour (a besoin d'Unity) |
|---|---|
| `Inventory`, `Wallet`, `Market`, `FiefState`, `BuildingCatalog` | `PlayerController`, `OrbitCamera`, `ResourceNode`, `BuildPlot`, `Hud` |

Toute la **règle du jeu** est dans la colonne de gauche. Elle ne connaît ni Unity,
ni le réseau, ni l'affichage. C'est elle qui partira côté hôte en Phase 3.

## 3. Autorité serveur : déjà en place, sans réseau

Décision verrouillée du brief : *« Autorité serveur absolue sur l'économie. Les clients
envoient des demandes, le serveur valide et réplique. Aucun client ne modifie son état
directement. »*

C'est déjà respecté, même en solo :

- `Wallet.Gold` est en **lecture seule**. On ne peut le changer que par `Add` / `TrySpend`.
- Toute transaction passe par `Market.RequestSell` / `Market.RequestBuy`.
  Ces méthodes **valident** (assez d'or ? assez de place ? assez de stock ?) puis appliquent.
- Toute construction passe par `BuildPlot.TryBuild`, qui débite puis pose.
- Le HUD ne calcule **rien**. Il lit, il affiche, il envoie des demandes.

**En Phase 3 :** ces trois méthodes deviennent des `ServerRpc`, elles ne s'exécutent que
chez l'hôte, et le résultat est répliqué. Le reste du code ne change pas.

## 4. Le point faible connu : `Game.cs`

`Game` est un porteur de références **statique** (`Game.Inventory`, `Game.Wallet`…).
C'est pratique en solo et **ça tombe à 2 joueurs** : il n'y a qu'un seul `Game.Inventory`
dans le processus, alors qu'il en faudra un par joueur.

**Le plan :** `Inventory`, `Wallet` et `FiefState` deviennent des composants portés par
l'objet joueur (`PlayerState`), et `Game.Market` reste unique mais **côté hôte uniquement**.
C'est une refonte d'une demi-journée, pas d'une semaine, parce que ces trois classes ne
dépendent de rien. C'est un choix assumé, pas un oubli.

## 5. Les entrées

`FiefInput` est une façade compilée en deux variantes (`#if ENABLE_INPUT_SYSTEM`).
Le jeu tourne donc avec l'ancien **et** le nouveau système d'input, sans changer une ligne
ailleurs. Quand on ajoutera les manettes et le rebinding, tout se passera dans ce fichier.

## 6. Le rendu

Aucun matériau n'est stocké en asset : `MaterialFactory` les crée à la volée et cherche
le shader **URP d'abord**, Built-in ensuite. Conséquence : passer le projet en URP est
une opération sans risque, et aucun asset ne sera à re-lier.

## 7. Ce qu'il ne faut pas faire

- **Ne mets pas de logique de jeu dans un `OnGUI`.** L'UI lit et demande, point.
- **Ne modifie pas l'or ou l'inventaire depuis l'extérieur** d'un `Request*` / `Try*`.
  Le jour où le réseau arrive, chaque entorse devient une triche exploitable.
- **N'ajoute pas de feature hors-phase.** Elle va dans `v2-ideas.md`.
