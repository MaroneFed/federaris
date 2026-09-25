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
4. **Jouer** ▸ le **salon** : règle Joueurs, Manches, Durée avec **← →** (ou clique les
   ‹ ›) ▸ **Commencer** ▸ choisis ta **première capacité** ▸ la manche 1 commence.
   Tous les menus se font **au clavier** (↑ ↓ ← → Entrée Échap) ou à la souris.

> **Rien ne s'affiche / la scène est vide ?** Menu **`FIEF` ▸ `Reparer la scene Main`**,
> puis re-Play. Le monde est entièrement généré par le code : il n'y a jamais rien à
> perdre dans la scène.

## 3. Les commandes

| Touche | Action |
|---|---|
| **ZQSD** / WASD | Se déplacer |
| **Souris** | Regarder |
| **Maj** | Courir |
| **Espace** | Sauter (encore en l'air : Double saut ; maintenu : Planeur) |
| **Clic gauche** | **Pousser** (le porteur lâche la Couronne) |
| **Clic droit** | Ta 1re capacité |
| **R** | Ta 2e capacité |
| **C** | Ta 3e capacité |
| **V** | Le don d'un sanctuaire (pour la manche) |
| **E** (maintenu) | Prendre la Couronne, prendre un don, poser au Monument |
| **F** | Grimper à un arbre |
| **Tab** | Le score et les capacités de chacun |
| **Échap** | Pause |
| **F3** | Diagnostic |

## 4. Ce qu'il faut tester — la Porte 1

> **« Un match de 30 minutes contre trois bots est-il haletant du début à la fin ? »**

1. Avant la manche 1, choisis ta première **capacité** (lis la phrase : elle dit tout).
2. Va à la **citadelle** (la tour de 64 m perce la brume). En chemin, un **sanctuaire**
   (cristal qui flotte, E maintenu) te donne une capacité de plus, sur **V**.
3. Monte la **rampe en spirale** de la tour : saute les trous, évite les pendules et
   les rayons des **Yeux**, pousse les bots dans le vide.
4. Prends la **Couronne** au sommet (E maintenu) et porte-la au **Monument** (la colonne
   bleue). Si tu sautes de la tour, elle reste en haut !
5. Entre deux manches, choisis une **capacité** de plus.

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
| Les manches, le départage, le choix des capacités | `Scripts/Match/Match.cs` |
| Les 26 capacités (noms, phrases, recharges, couleurs) | `Scripts/Match/Abilities.cs` |
| Ce que fait chaque capacité | `Scripts/World/AbilityCaster.cs` (mines, murs : `Effects.cs`) |
| Pousser, projeter | `Scripts/World/Combat.cs` ; les touches : `Scripts/Player/AbilityUser.cs` |
| La Couronne, le Monument, les sanctuaires | `Scripts/World/Crown.cs`, `Monument.cs`, `Shrine.cs` |
| Les Yeux (sentinelles) | `Scripts/World/Eye.cs` |
| La citadelle, la tour, les pendules | `Scripts/World/Castle.cs`, `Tower.cs` |
| Les bots | `Scripts/World/Rival.cs` |
| L'écran de jeu | `Scripts/UI/Hud.cs` |
| Titre, salon, pause, fin de manche, choix des capacités, podium | `Scripts/UI/Menus.cs` |
| Les sons (synthétisés par le code) | `Scripts/Core/Sfx.cs` |
| Les couleurs du jeu | `Scripts/Core/Palette.cs` |

## 7. Structure

```
Assets/_Fief/
  Scenes/Main.unity   <- 1 seul objet : le Bootstrap. Tout le reste est généré par code,
                         et reconstruit à chaque manche.
  Scripts/
    Core/     GameBootstrap (construit la manche), GameConfig (réglages), Sfx, Proto
    Match/    le match, les capacités (C# pur : survit aux manches)
    Season/   le joueur (Seeker), le chrono, les stats
    Player/   déplacement, caméra, interactions, poussée et capacités, entrées
    World/    forêt, citadelle, tour, Yeux, Couronne, Monument, sanctuaires, bots
    UI/       écran de jeu, menus (texte seul, sans icône), style
  Editor/     menu FIEF (outils éditeur, jamais dans le build)
docs/
  LA-SAISON.md    <- LA référence : les règles du jeu
  100-RAISONS.md  <- les 100 raisons pour lesquelles c'était nul, et ce qu'on a corrigé
  RESEAU.md       <- le jeu en ligne : ce qui est prêt, ce qui reste
  ARCHITECTURE.md <- pourquoi c'est découpé comme ça
  v2-ideas.md     <- la règle anti-dérive : toute idée hors-phase va ici
```

## 8. Ce qui n'est PAS dans cette phase (et c'est voulu)

Le jeu en ligne (Phase 3 — l'architecture est prête : `docs/RESEAU.md`), l'arc,
d'autres pièges et capacités (Phase 2), l'équilibrage fin (Phase 4).
→ Voir `docs/v2-ideas.md`.
