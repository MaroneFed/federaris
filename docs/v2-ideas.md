# v2-ideas — la boîte à idées hors-phase

> **La règle :** le tueur n°1 des projets de jeu, c'est l'ajout de features.
> Tout ce qui n'est pas dans la phase en cours **atterrit ici, jamais dans le code.**

**Phase en cours : PHASE 1 — boucle économique solo.**

---

## Reporté depuis la Phase 1 (dette assumée, à faire avant la Phase 3)

- **Refaire le HUD en UI Toolkit.** L'interface actuelle est en IMGUI (`OnGUI`) :
  zéro asset, zéro configuration, parfait pour valider la boucle — mais ce n'est pas
  ce qu'on shippe sur Steam. À reprendre quand la Porte 1 est franchie.
- **Passer en URP.** Le projet tourne en pipeline Built-in. Tous les matériaux sont
  créés par code dans `MaterialFactory`, qui essaie déjà le shader URP en premier :
  le basculement ne cassera rien. À faire quand on touchera au look.
- **Remplacer les primitives par des assets Kenney/Synty.** Trois fichiers à modifier,
  et rien d'autre : `NodeFactory.cs`, `BuildingFactory.cs` et `Scenery.cs`.
- **Sons.** Ils existent maintenant, mais ils sont *synthétisés par le code*
  (`Scripts/Core/Sfx.cs`) : aucun fichier audio. C'est suffisant pour sentir la boucle,
  pas pour shipper. À remplacer par de vrais sons — l'appel (`Sfx.Harvest(...)`) ne changera pas.
- **Les statiques de `Game.cs`.** Elles tombent en multijoueur (voir ARCHITECTURE.md).

## PROPOSITION DE PIVOT — Martin, 21/09/2026 (non tranchée)

Martin propose de **changer de jeu**, pas d'ajuster celui-ci. Résumé fidèle de ce
qu'il a dit, pour que la décision se prenne sur une base écrite :

- **Plus de fief par joueur** (« si chacun a son château, c'est mort »).
- **Un seul énorme château au centre**, tenu par l'adversité, pas par un joueur.
- On y **vole des artefacts** et on les rapporte à **une petite base cachée** :
  on arrive avec **une tente à planter**, dissimulée.
- **Forêt partout, très dense** : on ne voit qu'un mètre ou deux devant soi.
  Le jeu devient de l'infiltration — on se cache.
- **Toutes les constructions sont déjà là.** On ne bâtit pas, on explore.
  Au plus, on se fait des cabanes.

**Ce que ce pivot supprime :** le marché à prix dynamiques, la boucle
récolter→vendre→construire, les fiefs — et donc **le sabotage par les salaires**,
puisqu'il n'y a plus ni fief à tenir ni serviteurs à sous-payer. C'est-à-dire le
différenciateur que le brief demande de protéger avant tout le reste.

**Ce que ce pivot est :** un jeu d'infiltration et d'extraction (la forme de
*Hunt: Showdown* ou *Tarkov*, en médiéval). C'est un vrai genre, qui peut être
excellent — mais c'est un autre projet, avec une autre Porte 1.

**Point technique à trancher si le pivot est retenu :** voir à 1-2 m est
injouable (on ne peut plus se repérer ni viser une direction). 15 à 25 m de
visibilité avec un sous-bois épais donne la même sensation d'aveuglement tout en
restant jouable.

**DÉCISION (21/09/2026) :** Martin a répondu « fais comme tu le penses ».
→ **On part sur l'option B**, décrite ci-dessous. Elle lui donne tout ce qu'il a
demandé sans sacrifier le différenciateur.

### Option B — le château central, la planque, et le sabotage conservé

- **Un seul château au centre**, à la place du marché. Il n'appartient à personne :
  c'est la cible commune. On y entre pour **voler des artefacts**.
- **Chaque joueur a une planque**, pas un château : une tente à planter, cachée,
  qu'on améliore en cabane. C'est là qu'on ramène le butin.
- **Le sabotage par les salaires survit, et devient même plus clair** : les
  serviteurs sont ceux **du château central**. On les soudoie pour qu'ils laissent
  une porte ouverte, détournent le regard, ou éteignent une torche. La trahison
  s'achète toujours — c'est juste la garde du château qu'on retourne, au lieu de
  celle d'un rival.
- **Forêt dense partout**, visibilité réduite, on se cache. Viser 15-25 m de
  visibilité réelle : 1-2 m est injouable.
- **Rien à construire au sens de la Phase 1** : les lieux existent déjà, on les
  explore. On garde les caisses à fouiller, les lieux à découvrir, la faune.

Ce qui est déjà en place et resservira tel quel : le terrain, les forêts, le
fusionneur de maillages, le décor, les lieux, le butin, la faune, le personnage,
les menus, l'audio. Ce qui saute : le marché à prix dynamiques et les six
emplacements de construction.

## Demandé par Martin le 21/09/2026 — HORS PHASE, en attente

> Ces quatre idées sont bonnes, mais aucune n'est de la Phase 1. La règle anti-dérive
> du projet (fin de `CLAUDE.md`) dit qu'elles restent ici jusqu'à ce que la Porte 1
> soit franchie. C'est sa propre règle, et c'est elle qui empêche le projet de mourir.

- **Cycle jour / nuit.** C'est explicitement de la **Phase 2** dans le brief. Tout est
  prêt pour l'accueillir (lumière directionnelle unique, torches déjà posées partout,
  matériaux créés par code), mais ça n'arrive qu'après la Porte 1.
- **Vue première personne la nuit.** Nouveau mode de caméra, alors que la décision
  verrouillée dit « 3e personne, caméra orbitale ». Faisable en une demi-journée
  (`OrbitCamera` gagne un mode), mais c'est un changement de décision verrouillée :
  à trancher explicitement, pas à glisser.
- **« La nuit on s'occupe du château, la journée ça bosse ».** Excellente idée de
  structure : elle donne un rythme à la Saison et une raison d'exister au château.
  Elle dépend du cycle jour/nuit, donc Phase 2, et elle se conçoit **avec** le
  sabotage par les salaires (c'est la nuit qu'un serviteur soudoyé ouvre la porte).
- **Bourse, paris et contrats.** C'est une **mécanique entièrement nouvelle**, absente
  du brief. Elle est séduisante et colle au pilier économique — mais c'est aussi
  exactement le genre d'ajout qui dilue le différenciateur. À discuter **en Phase 4**,
  quand la Saison sera complète, et seulement si elle sert le sabotage plutôt que de
  lui voler la vedette.

## Reporté depuis la Phase 1



- **Croiser un autre joueur dans la forêt et pouvoir se défendre.** L'épée est déjà
  **visible dans le dos du personnage** (décor). Le combat lui-même reste Phase 2 :
  c'est la première chose qu'on fera une fois la Porte 1 franchie.
- **Vrais assets 3D** (Kenney / Synty) à la place des primitives. Trois fichiers à
  échanger, rien d'autre : `NodeFactory.cs`, `BuildingFactory.cs`, `Scenery.cs`.
- **Les 5 fiefs rivaux**, masqués en solo depuis le 20/09/2026 (`showRivalFiefs`).
  Ils reviennent en Phase 3, occupés par de vrais joueurs.
- **Nager / sortir de l'eau proprement** : les lacs existent, mais le joueur les
  traverse à pied. Ça suffit pour l'instant.
- **Refonte complète de l'interface** en UI Toolkit (l'actuelle est en IMGUI, soignée
  mais pas définitive).

## Phase 2 — le conflit (ne pas commencer sans validation de la Porte 1)

- Combat mêlée + arc, PV, drop à la mort.
- Vol du coffre d'un rival.
- Destruction à règles v1 : bois → hache/feu, pierre → bélier/trébuchet/sape,
  herse → clé volée, **porte dérobée → uniquement par quelqu'un de l'intérieur**.
- PNJ serviteurs : salaire, jauge de loyauté.
- **v1 du sabotage** — LE différenciateur. Payer un serviteur mécontent pour qu'il
  ouvre la porte la nuit. À protéger absolument, c'est ce que personne d'autre ne fait.
- Cycle jour/nuit.

## Phase 3 — multijoueur

- Netcode for GameObjects, LAN d'abord, puis Steam P2P (Facepunch.Steamworks).
- Lobby héberger/rejoindre, 4 à 6 joueurs.
- Autorité serveur sur l'économie (l'architecture actuelle est déjà faite pour, voir ARCHITECTURE.md).
- Gestion des déconnexions.

## Phase 4 — la Saison complète

- Timer de Saison, score de Prestige, conditions de victoire multiples, écran de fin.
- Anti-snowball : prime sur le leader.
- 2-3 événements de monde.
- Équilibrage.

---

## Idées en vrac (rien de décidé, rien de promis)

*(Ajoute ici tout ce qui te passe par la tête pendant le dev. C'est fait pour ça.)*

- Un serviteur soudoyé pourrait rester loyal et **prévenir son seigneur** → contre-jeu
  du sabotage, et une histoire à raconter après la partie.
- Rumeur publique au marché : « quelqu'un a payé cher pour des renseignements ».
- Les prix du marché visibles de loin depuis une Tour de guet.
