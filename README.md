# FIEF — La Couronne

> Une Couronne au sommet d'une tour de 100 m, sur une île qui flotte au-dessus des nuages. Jusqu'à huit joueurs.
> Le premier qui la porte à l'un des trois Monuments gagne la manche. Plein d'aura.

Ce dossier est le projet Unity. Les règles du jeu : `docs/LA-SAISON.md`.

---

## 0. RÉCUPÉRER LA DERNIÈRE VERSION (à faire avant chaque test)

**Le jeu est sur la branche `claude/pensive-dijkstra-848h9o`, pas sur `main`** (`main`
ne contient qu'un ancien site web).

**Comment savoir quelle version tourne :** en bas à droite de l'écran-titre est écrit
`La Couronne · v19 · musiques et bruitage · 02/10` (et la même ligne dans la Console au lancement : `[FIEF] La
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
4. **Jouer** ▸ le **salon** : règle Joueurs, Bots, Manches, Durée avec **← →** (ou clique les
   ‹ ›) ▸ **Commencer** ▸ choisis ta **première capacité** ▸ la manche 1 commence.
   Tous les menus se font **au clavier** (↑ ↓ ← → Entrée Échap) ou à la souris.

> **Rien ne s'affiche / la scène est vide ?** Menu **`FIEF` ▸ `Reparer la scene Main`**,
> puis re-Play. Le monde est entièrement généré par le code : il n'y a jamais rien à
> perdre dans la scène.

> **L'image est floue (surtout les textes) ?** Depuis la v12, le jeu dessine tout au
> pixel près. S'il reste du flou, c'est la **fenêtre Game** d'Unity qui réduit l'image :
> 1. en haut de la fenêtre **Game**, le curseur **Scale** doit être à **1x** (tout à
>    gauche) ;
> 2. dans la liste des résolutions juste à côté (« Free Aspect », « Full HD »…), choisis
>    une **vraie résolution** (« Full HD (1920x1080) ») et **pas Free Aspect** — testé le
>    02/10 par Martin : avec Free Aspect tout était flou, avec une résolution fixe tout est
>    net ; si la case « Low Resolution Aspect Ratios » existe, décoche-la ;
> 3. **l'ampoule et le haut-parleur** qu'on voit en regardant en bas : ce sont les
>    **Gizmos** d'Unity (la lanterne, les sons). En haut à droite de la fenêtre Game,
>    décoche **Gizmos** — ils n'existent pas dans le jeu exporté ;
> 4. le plus simple : clique **Maximize On Play** (ou appuie sur **Maj+Espace** avec la
>    souris sur la fenêtre Game) — le jeu prend tout l'écran d'Unity.

## 3. Les commandes

| Touche | Action |
|---|---|
| **ZQSD** / WASD | Se déplacer |
| **Souris** | Regarder |
| **Maj** | Courir |
| **Espace** | Sauter ; en vol : replier ou rouvrir les ailes |
| **Voler** | Tombe dans le vide : **les ailes s'ouvrent seules**. Souris en bas : piquer (vite) ; en haut : remonter. Q/D : glisser, S : freiner |
| **Clic gauche** | **TA capacité** (une seule, nouvelle à chaque manche ; celles qu'on vise : **maintiens** pour voir l'aperçu, **relâche** pour lancer) |
| **Clic droit** | **Pousser** (le poussé part à une quinzaine de mètres) — pousser le porteur, c'est lui **voler la Couronne** ; **en l'air, sur le porteur : le piqué d'aigle** |
| **E** | **Interagir**, d'un simple appui : prendre la Couronne (ou passe dessus), un don ; **monter sur une arbaleste** (et en descendre) |
| **Sur l'arbaleste de ta plateforme** | Clic gauche : elle te pose devant ta porte |
| **Sur une autre arbaleste** | Souris : viser · **clic gauche maintenu : tendre**, relâché : tirer |
| **F1** ou **H** | Le panneau des touches |
| **Tab** | Le score et les capacités de chacun |
| **Échap** | Pause |
| **F3** | Diagnostic |

## 4. Ce qu'il faut tester — la Porte 1

> **« Un match de 30 minutes contre des bots est-il haletant du début à la fin ? »**

0. **Réglages ▸ Pseudo** : tape ton pseudo.
1. Avant chaque manche, choisis **une passive**, puis **ton clic gauche**. Elles
   changent à chaque manche.
2. **3, 2, 1, PARTEZ !** Tu es sur **ta plateforme**, en face d'une porte. **E** : monte
   sur **ton arbaleste**, **clic gauche** : elle te pose sur le parvis devant ta porte.
3. Passe le **couloir piégé** et le **pont-levis**. Tu voles vers les remparts ? **Le
   sceau te renvoie.**
4. Monte **la rampe en face de ta porte** (il y en a quatre, une par porte) : saute les
   trous et les **balayeurs**, esquive pendules, béliers, herses, boulets — et le feu
   des **gargouilles**, qui explose et te fait redescendre (une seule te vise à la fois,
   puis elles te laissent 9 s). **Regarde les bots : ils doivent arriver en haut** (ils
   attendent le pendule, le bélier, la herse). Ils te poussent parfois.
5. Au sommet : la **Couronne** (passe dessus). Si tu tombes avec, **elle reste là
   où tu touchais le sol** : plus besoin de tout remonter. Saute : **tes ailes s'ouvrent toutes
   seules**. Vole jusqu'à **l'un des trois Monuments**. Entre dans son cercle.
6. **Gagne la manche** : la caméra te filme, **ton pseudo en or**, et ton haricot danse.
7. Joue plusieurs manches : **les obstacles changent et deviennent plus nombreux**.

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
| La Couronne (et le vol), le Monument, les sanctuaires | `Scripts/World/Crown.cs`, `Monument.cs`, `Shrine.cs` |
| Les Yeux (sentinelles) | `Scripts/World/Eye.cs` |
| L'île, les îlots, la mer de nuages | `Scripts/World/Ground.cs`, `Ambiance.cs` ; le ciel : `Atmosphere.cs` |
| La tour et ses obstacles (courants, pendules, béliers, boulets) | `Scripts/World/Tower.cs` ; la citadelle : `Castle.cs` |
| Les planeurs, le vol | `Scripts/World/Wings.cs` |
| Les arbalestes géantes | `Scripts/World/Ballista.cs` |
| Le vol plané, les courants d'air, le sceau de la citadelle | `Scripts/World/Wings.cs` |
| Le parcours des portes (chicanes, moulinets, herses, marteaux) | `Scripts/World/Course.cs` |
| L'aura (moments, flammes, phonk) | `Scripts/World/Aura.cs` |
| Les gargouilles | `Scripts/World/Eye.cs` |
| Les cartes du choix | `Scripts/UI/CardArt.cs` |
| Les effets spéciaux | `Scripts/World/Fx.cs` |
| La ligne de départ, le respawn | `Scripts/World/Combat.cs` (Spawns, Respawn) |
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
    World/    île, citadelle, tour, Yeux, Couronne, Monument, planeurs, arbalestes, effets, bots
    UI/       écran de jeu, menus (texte seul, sans icône), style
  Editor/     menu FIEF (outils éditeur, jamais dans le build)
docs/
  LA-SAISON.md    <- LA référence : les règles du jeu
  100-RAISONS.md  <- les 100 raisons pour lesquelles c'était nul, et ce qu'on a corrigé
  100-PROBLEMES.md <- le deuxième passage : ce qui cassait, ce qui manquait
  RESEAU.md       <- le jeu en ligne : ce qui est prêt, ce qui reste
  ARCHITECTURE.md <- pourquoi c'est découpé comme ça
  v2-ideas.md     <- la règle anti-dérive : toute idée hors-phase va ici
```

## 8. Ce qui n'est PAS dans cette phase (et c'est voulu)

Le jeu en ligne (Phase 3 — l'architecture est prête : `docs/RESEAU.md`), l'arc,
d'autres pièges et capacités (Phase 2), l'équilibrage fin (Phase 4).
→ Voir `docs/v2-ideas.md`.
