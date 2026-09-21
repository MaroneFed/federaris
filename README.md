# FIEF — Phase 1 (boucle économique solo)

> Bâtis ton fief, domine le marché, et prends les châteaux de tes rivaux.

Ce dossier est le projet Unity. **Tout ce qui suit prend 5 minutes.**

---

## 1. Ouvrir le projet (2 min)

Le projet Unity est **à la racine du dépôt** (`Assets/`, `ProjectSettings/`), pour qu'Unity
Hub le détecte tout seul.

**Méthode A — directement depuis GitHub (la plus simple)**

1. Unity Hub ▸ `Add` ▸ **`Add project from repository`**.
2. Dépôt : `MaroneFed/federaris` — Branche : `claude/pensive-dijkstra-848h9o`.
3. Choisis où le télécharger ▸ **`Ajouter un projet`**.

**Méthode B — si tu as déjà cloné le dépôt (GitHub Desktop, ZIP…)**

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

1. Dans Unity, en haut : menu **`FIEF` ▸ `Ouvrir la scène Main`** (ou `Ctrl+Shift+M`).
   *Sinon : dans la fenêtre `Project` en bas, double-clique `Assets/_Fief/Scenes/Main.unity`.*
2. Appuie sur le **bouton ▶ Play** en haut au centre.
3. L'**écran-titre** apparaît, avec la caméra qui tourne autour de ton fief.
   Clique **COMMENCER LA SAISON**.

**Ce que tu dois voir :** un monde de **2200 × 2200 m — 484 hectares** en low-poly à
facettes. Ton **château fort** (enceinte de 76 m, quatre tours, donjon de 28 m), des
forêts épaisses, cinq lacs, des cerfs qui détalent, des moulins et des chapelles à
découvrir, des caisses à fouiller, et des montagnes enneigées qui ferment l'horizon.

**Les autres châteaux sont invisibles depuis le tien** : des collines-barrières sont
posées sur les lignes de vue. Il faut ~2 min de marche pour rejoindre un voisin.

> **Rien ne s'affiche / la scène est vide ?** Menu **`FIEF` ▸ `Réparer la scène Main`**,
> puis re-Play. C'est le filet de sécurité : le monde est entièrement généré par le code,
> il n'y a donc jamais rien à perdre dans la scène.

## 3. Les commandes

| Touche | Action |
|---|---|
| **ZQSD** / WASD / flèches | Se déplacer |
| **Maj (Shift)** | Courir — seulement si le sac est léger |
| **Souris** | Caméra orbitale |
| **Molette** | Zoom |
| **Espace** | Sauter |
| **E** | Récolter (maintenir) / interagir |
| **Échap** | Pause / fermer un panneau |
| **F1** | Afficher les commandes |

## 4. Ce qu'il faut tester — la Porte 1

> **« La boucle récolter → vendre → construire est-elle satisfaisante pendant 20 min, en solo ? »**

Le parcours :

1. Pars vers un bosquet (repère **MARCHE** et **TON FIEF** sont affichés en permanence
   avec la distance). **Maintiens E** sur un arbre.
2. Regarde la **jauge de poids** en bas à gauche. Bois = 1 kg, Pierre = 2 kg, Fer = 3 kg,
   pour **60 kg** de charge max. Plus tu es chargé, plus **tes gestes deviennent lents**
   (jusqu'à ×2,4 par coup de hache) — la ligne « Gestes » sous la jauge te le dit.
   Tu restes mobile (−26 % de vitesse seulement), mais **tu ne peux plus courir**.
3. Va au marché central, **E**, et vends. **Regarde le prix chuter** pendant que tu écoules
   ta cargaison : le prix se recalcule à chaque unité. Vendre 45 bois d'un coup rapporte
   nettement moins que 2 fois 22 espacés.
4. Rentre à ton fief, **E** sur un emplacement, construis.
   Commence par le **Coffre** (150 or), puis la **Scierie** (300 or) qui produit toute seule.
5. Recommence. Objectif : les 5 constructions payées (2380 or au total).
   **Calibré pour ~27 min et 6 voyages** — c'est exactement la durée de la Porte 1.

**Ce que tu dois me dire après 20 min :** est-ce que c'est *satisfaisant*, ou est-ce que
ça traîne ? Les chiffres qui se règlent en 10 secondes sont dans `GameConfig`
(voir §6) : vitesse, poids max, rendement de récolte, délai de repousse, prix de base.

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
| Vitesse, poids max, prix, rendements, taille de la carte | `Scripts/Core/GameConfig.cs` |
| Le personnage et son animation | `Scripts/Player/CharacterRig.cs` |
| L'écran-titre et le menu pause | `Scripts/UI/Menus.cs` |
| L'apparence de l'interface (panneaux, boutons) | `Scripts/UI/UiStyle.cs` |
| **La carte** (zones de ressources, positions des fiefs) | `GameConfig.DefaultZones()` |
| Les 5 constructions (coût, effet, prestige) | `Scripts/Building/BuildingCatalog.cs` |
| La formule des prix dynamiques | `Scripts/Economy/Market.cs` |
| Le relief, les lacs, les teintes du terrain | `Scripts/World/Ground.cs` |
| Les forêts, le décor, les nuages, les repères | `Scripts/World/Scenery.cs` |
| Le bourg du marché et ton fief | `Scripts/Core/GameBootstrap.cs` |
| Les sons (synthétisés par le code) | `Scripts/Core/Sfx.cs` |
| L'apparence des arbres/rochers | `Scripts/World/NodeFactory.cs` |
| L'apparence des bâtiments | `Scripts/Building/BuildingFactory.cs` |
| Les couleurs du jeu | `Scripts/Core/Palette.cs` |

## 7. Structure

```
Assets/_Fief/
  Scenes/Main.unity        <- 1 seul objet : le Bootstrap. Tout le reste est généré par code.
  Scripts/
    Core/      GameBootstrap (construit le monde), GameConfig (réglages), Palette, Proto
    Player/    déplacement, caméra orbitale, interactions, abstraction des entrées
    World/     gisements et leur apparence
    Inventory/ inventaire + poids, bourse
    Economy/   marché à prix dynamiques (autorité serveur)
    Building/  catalogue, emplacements, état du fief
    UI/        HUD, panneaux marché / construction / coffre
  Editor/      menu FIEF (outils éditeur, jamais dans le build)
docs/
  ARCHITECTURE.md          <- pourquoi c'est découpé comme ça, et ce que ça change en Phase 3
  v2-ideas.md              <- LA règle anti-dérive : toute idée hors-phase va ici

index.html, src/           <- ancien site web, sans rapport. Unity ne les lit pas.
```

## 8. Ce qui n'est PAS dans cette phase (et c'est voulu)

Combat, PNJ serviteurs, salaires et loyauté, **sabotage**, destruction à règles, cycle
jour/nuit, multijoueur, timer de Saison, score de Prestige final.
→ Phases 2, 3 et 4. Voir `docs/v2-ideas.md`.

