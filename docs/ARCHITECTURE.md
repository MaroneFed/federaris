# Architecture — pourquoi c'est découpé comme ça

Document court, à relire avant la Phase 3. L'objectif de ce découpage est **un seul** :
pouvoir greffer le combat, l'IA, la diplomatie et surtout le **multijoueur** sans réécrire.

---

## 1. La scène ne contient qu'un objet

`Main.unity` = un GameObject `FIEF (Bootstrap)` portant `GameConfig` + `GameBootstrap`.
Tout le monde (relief, forêt, château, gardes, joueurs, caméra, HUD) est construit par code,
et **reconstruit à chaque manche** (la scène est rechargée).

**Pourquoi :** une scène Unity est un fichier YAML de plusieurs milliers de lignes.
À deux sur le même projet, deux modifications de scène = un conflit Git impossible à
résoudre à la main. Ici, la map est du **code** : elle se lit, se relit et se merge
normalement.

**Contrepartie :** on ne peut pas placer un arbre à la souris. C'est le bon compromis
tant que la map est faite de zones et d'emplacements, ce qui est la décision verrouillée
du brief. Quand ton frère voudra placer les choses à la main, on fera l'inverse :
un script éditeur qui *écrit* dans `GameConfig` ce qu'il a posé dans la scène.

## 2. Logique pure ≠ MonoBehaviour

| Classe C# pure (testable, sérialisable, réplicable) | MonoBehaviour (a besoin d'Unity) |
|---|---|
| `Match` (places, manches, départage), `Match.Draft` (le choix des pouvoirs), `PlayerSlot`, `Loadout` (les trois objets), `Seeker` (la vie, les états), `Season` (le chrono), `Stats` | `PlayerController`, `ToolUser`, `Rival` (les bots), `Guard`, `Crown`, `Monument`, `Chest`, `Hud`, `Menus` |

Toute la **règle du jeu** est dans la colonne de gauche. Elle ne connaît ni le réseau,
ni l'affichage. C'est elle qui partira côté hôte en Phase 3. La preuve qu'elle est pure :
`Match` et `Loadout` se testent dans un petit programme .NET, sans Unity (c'est ce que
Claude a fait le 26/09 : manches, départage, choix des pouvoirs, objets en main).

## 3. Autorité de l'hôte : déjà en place, sans réseau

Décision verrouillée : *l'hôte décide de tout ce qui compte* (Couronne, coups, fin de
manche). C'est déjà respecté, même en solo : chaque geste passe par **une méthode qui
prend le joueur qui agit** — `Crown.TryTakeFor(s)`, `Monument.TryDeliver(s)`,
`Chest.TryOpenFor(s)`, `Combat.Shove(s, …)`, `Combat.Hit(…, s, …)`, `Lever.PullFor(s)`,
`SecretDoor.UseFor(s)`, `Match.Draft.TryPick(place, carte)`. Toi et les bots passez par
les mêmes. La manche ne se termine **qu'à un endroit** : `Menus.EndRound(place)`.

**En Phase 3 :** ces méthodes ne s'exécutent que chez l'hôte ; un invité envoie son
intention, l'hôte appelle la méthode et réplique le résultat. Le détail :
`docs/RESEAU.md`.

## 4. Le point faible connu : `Game.cs`

`Game` est un porteur de références **statique** (`Game.Me`, `Game.Player`, `Game.Hud`…).
Ça tient **en ligne** parce qu'il n'y a qu'un joueur local par machine ; les autres
sont dans `Game.Seekers`. Ce qui traverse les manches est dans `Match` (statique aussi,
et c'est voulu : chaque manche recharge la scène).

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
- **Ne change pas la Couronne, les objets ou les victoires** en dehors des méthodes
  `Try…` / `…For(seeker)`. Le jour où le réseau arrive, chaque entorse devient une
  triche exploitable.
- **N'ajoute pas de feature hors-phase.** Elle va dans `v2-ideas.md`.
