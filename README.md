# FIEF — La Couronne

> Une Couronne au sommet d'un château gardé. Quatre joueurs dans une forêt noire.
> Le premier qui la porte au Monument gagne la manche.

Ce dossier est le projet Unity. Les règles du jeu : `docs/LA-SAISON.md`.

---

## 0. RÉCUPÉRER LA DERNIÈRE VERSION (à faire avant chaque test)

**Le jeu est sur la branche `claude/pensive-dijkstra-848h9o`, pas sur `main`** (`main`
ne contient qu'un ancien site web).

**Comment savoir quelle version tourne :** en bas à droite de l'écran-titre est écrit
`La Couronne · v2 · 27/09` (et la même ligne dans la Console au lancement : `[FIEF] La
Couronne…`). Si tu ne vois pas ça — ou si le menu dit encore « Entrer dans la sylve » —,
Unity fait tourner une vieille copie.

**Attention :** Unity Hub ▸ `Add project from repository` télécharge le projet **une
seule fois**. Les mises à jour n'arrivent jamais toutes seules dans ce dossier-là.

**La bonne méthode : GitHub Desktop** (gratuit, desktop.github.com)

1. *Une seule fois* : `File` ▸ `Clone repository` ▸ onglet `URL` ▸
   `https://github.com/MaroneFed/federaris` ▸ choisis un dossier ▸ `Clone`.
2. *Une seule fois* : en haut, `Current branch` ▸ choisis **`claude/pensive-dijkstra-848h9o`**.
3. *Une seule fois* : Unity Hub ▸ `Add` ▸ `Add project from disk` ▸ ce dossier `federaris`.
4. **Avant chaque test** : GitHub Desktop ▸ bouton **`Fetch origin`**, puis **`Pull origin`**
   s'il apparaît. Reviens dans Unity : il recompile tout seul (petite roue en bas à
   droite). Sinon : menu `Assets` ▸ `Refresh` (`Ctrl+R`).

**Si ça ne change toujours pas :** ouvre la Console (`Window` ▸ `General` ▸ `Console`).
Une ligne rouge = une erreur de compilation, et Unity garde alors l'ancien code. Copie-
la moi telle quelle.

## 1. Ouvrir le projet (2 min)

Le projet Unity est **à la racine du dépôt** (`Assets/`, `ProjectSettings/`), pour qu'Unity
Hub le détecte tout seul.

Clone avec GitHub Desktop (section 0 ci-dessus), puis :

1. Unity Hub ▸ `Add` ▸ `Add project from disk`.
2. Sélectionne le dossier **`federaris`** lui-même (celui qui contient `Assets/`).

Dans les deux cas : choisis une version **Unity 6** (n'importe quelle `6000.x`). Si le Hub
dit que le projet vient d'une autre version, clique **Continue** — il met à jour tout seul.
Le premier import prend 1 à 3 minutes. Une seule fois.

> **Pas de Unity 6 installé ?** Hub ▸ `Installs` ▸ `Install Editor` ▸ la version LTS 6000.
> Laisse les cases par défaut.

> **Note :** `index.html` et `src/` à la racine sont les restes d'un ancien site web,
> sans rapport avec le jeu. Unity les ignore (il ne lit que `Assets/`).

## 2. Lancer le jeu (30 s)

1. Dans Unity, en haut : menu **`FIEF` ▸ `Ouvrir la scene Main`** (ou `Ctrl+Shift+M`).
   *Sinon : dans la fenêtre `Project` en bas, double-clique `Assets/_Fief/Scenes/Main.unity`.*
2. Appuie sur le **bouton ▶ Play** en haut au centre.
3. L'**écran-titre** apparaît : FIEF, LA COURONNE, et la version en bas à droite.
4. **Jouer** ▸ le **salon** : clique les places pour ajouter/retirer des bots, choisis le
   nombre de manches et la durée ▸ **COMMENCER**.

> **Rien ne s'affiche / la scène est vide ?** Menu **`FIEF` ▸ `Reparer la scene Main`**,
> puis re-Play. Le monde est entièrement généré par le code : il n'y a jamais rien à
> perdre dans la scène.

## 3. Les commandes

| Touche | Action |
|---|---|
| **ZQSD** / WASD | Se déplacer |
| **Souris** | Regarder |
| **Maj** | Courir |
| **Espace** | Sauter (deux fois avec le pouvoir Double saut) |
| **Clic gauche** | Épée — ou se servir de l'objet en main |
| **Clic droit** | Pousser (le porteur lâche la Couronne) |
| **1 / 2 / 3**, molette | Les objets |
| **E** | Prendre, ouvrir, poser la Couronne, tirer le levier |
| **F** | Grimper à un arbre |
| **R** | Ruée (le pouvoir) |
| **Tab** | Le score du match |
| **Échap** | Pause |
| **F3** | Diagnostic |

## 4. Ce qu'il faut tester — la Porte 1

> **« Un match de 30 minutes contre trois bots est-il haletant du début à la fin ? »**

1. Trouve le château (sa tour de guet dépasse de la brume) et entre : la herse est
   baissée — passe par la **poterne** (nord) ou la **brèche** (est), puis tire le
   **levier** dans la cour.
2. Monte le donjon jusqu'à la terrasse, prends la **Couronne** (E maintenu). Le **Roi
   Creux** se réveille.
3. Porte-la au **Monument** (la colonne bleue) sans te faire pousser.
4. Entre deux manches, choisis un **pouvoir**.

**Ce que tu dois me dire :** qu'est-ce qui t'a fait rire, qu'est-ce qui t'a ennuyé, et
à quel moment tu as eu envie de lâcher.

## 5. Étape optionnelle (30 s) — activer le nouvel Input System

Le jeu marche **immédiatement** avec l'Input System historique d'Unity. Le brief prévoit
le **nouvel** Input System (nécessaire pour les manettes et le rebinding plus tard) :

1. `Window` ▸ `Package Manager` ▸ onglet **Unity Registry** ▸ cherche **Input System** ▸ `Install`.
2. Unity demande de redémarrer et d'activer le nouveau backend : réponds **Yes**.
3. C'est tout. `FiefInput.cs` bascule automatiquement (directives `#if ENABLE_INPUT_SYSTEM`),
   aucune ligne à changer.

> Sur un clavier **AZERTY**, ZQSD fonctionne dans les deux cas : le nouvel Input System
> raisonne en *position physique* de touche.

## 6. Où changer quoi

Sélectionne l'objet **`FIEF (Bootstrap)`** dans la fenêtre `Hierarchy` (à gauche) :
le composant **GameConfig** dans l'`Inspector` (à droite) contient **tous** les réglages.
Tu peux les modifier **pendant que le jeu tourne** pour sentir l'effet immédiatement
(⚠ les changements faits en Play sont perdus à l'arrêt — note ceux qui te plaisent).

| Je veux changer… | Fichier |
|---|---|
| Vitesse, saut, brume, lanterne, taille de la carte | `Scripts/Core/GameConfig.cs` |
| Les manches, le départage, le choix des pouvoirs | `Scripts/Match/Match.cs` |
| Les dix pouvoirs (noms, effets, couleurs) | `Scripts/Match/Powers.cs` |
| Les objets de la forêt | `Scripts/Match/Items.cs` (effets : `Scripts/Player/ToolUser.cs`) |
| La Couronne, le Monument, les coffres | `Scripts/World/Crown.cs`, `Monument.cs`, `Chest.cs` |
| La Garde Pâle et le Roi Creux | `Scripts/World/Guard.cs` ; où ils se tiennent : `Garrison.cs` |
| Le château, le donjon | `Scripts/World/Castle.cs`, `Keep.cs`, `Gatehouse.cs` (herse, levier, porte dérobée) |
| Les bots | `Scripts/World/Rival.cs` |
| L'écran de jeu | `Scripts/UI/Hud.cs` |
| Titre, salon, pause, fin de manche, pouvoirs, podium | `Scripts/UI/Menus.cs` |
| Les sons (synthétisés par le code) | `Scripts/Core/Sfx.cs` |
| Les couleurs du jeu | `Scripts/Core/Palette.cs` |

## 7. Structure

```
Assets/_Fief/
  Scenes/Main.unity   <- 1 seul objet : le Bootstrap. Tout le reste est généré par code,
                         et reconstruit à chaque manche.
  Scripts/
    Core/     GameBootstrap (construit la manche), GameConfig (réglages), Sfx, Proto
    Match/    le match, les pouvoirs, les objets (C# pur : survit aux manches)
    Season/   le joueur (Seeker), le chrono, les stats
    Player/   déplacement, caméra, interactions, épée/poussée/objets, entrées
    World/    forêt, château, Garde Pâle, Couronne, Monument, bots, bêtes
    UI/       écran de jeu, menus, pictogrammes, style
  Editor/     menu FIEF (outils éditeur, jamais dans le build)
docs/
  LA-SAISON.md    <- LA référence : les règles du jeu
  RESEAU.md       <- le jeu en ligne : ce qui est prêt, ce qui reste
  ARCHITECTURE.md <- pourquoi c'est découpé comme ça
  v2-ideas.md     <- la règle anti-dérive : toute idée hors-phase va ici
```

## 8. Ce qui n'est PAS dans cette phase (et c'est voulu)

Le jeu en ligne (Phase 3 — l'architecture est prête : `docs/RESEAU.md`), l'arc,
d'autres pièges et pouvoirs (Phase 2), l'équilibrage fin (Phase 4).
→ Voir `docs/v2-ideas.md`.
