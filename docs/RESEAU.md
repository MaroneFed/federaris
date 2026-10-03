# Le jeu en ligne — ce qui est prêt, ce qui reste (Phase 3)

> **04/10/2026 — Martin : « Tout est parfait. Donc, on peut faire le en ligne. » La Phase 3
> commence.** Ce qui suit la ligne « ÉTAPE 1 » est l'état réel ; la suite du document est
> le plan d'origine (26/09), toujours valable pour l'architecture. **L'étape 2 (04/10) : on lance
> le match ensemble** (voir plus bas).

## ÉTAPE 1 (04/10) : héberger, rejoindre, le même salon — FAIT

**Le choix technique, franchement** : le plan disait *Netcode for GameObjects* + Steam. Claude
ne peut ni installer ni compiler ces paquets, donc il aurait codé à l'aveugle la partie la plus
risquée du projet. À la place, **une couche réseau maison, petite, en C# pur**
(`Scripts/Net/NetLink.cs`, ~400 lignes) que Claude **teste vraiment** hors d'Unity :
`sh Tools/reseau.sh` fait parler un hôte et deux invités sur la même machine (connexion,
salon, départs, refus, ping) — « RESEAU OK ». Elle parle **UDP** (le protocole des jeux).

- **Ce que ça permet aujourd'hui** : jouer sur **le même PC** (deux fenêtres) ou **le même
  réseau** (la box de la maison). **Pas encore par Internet** : il faudrait ouvrir un port
  sur la box. C'est le rôle de **Steam** (ses serveurs relais traversent les box) : à
  l'étape « Steam », seul le *tuyau* de `NetLink` change, pas le jeu.
- **Le modèle reste** : un hôte (listen-server), jusqu'à **sept invités** (8 joueurs),
  autorité absolue de l'hôte.

**Comment tester (Martin)** :
1. **File ▸ Build Profiles ▸ Windows ▸ Build And Run** : le jeu s'exporte et se lance dans
   sa propre fenêtre. (La première fois, Windows demande d'autoriser le réseau : *Autoriser*.)
2. Dans Unity, **Play** : tu as deux jeux ouverts.
3. Dans l'un : **En ligne ▸ Héberger**. Dans l'autre : **En ligne**, tape `127.0.0.1` à côté
   de **Rejoindre**, puis **Rejoindre**. Les deux salons montrent les deux pseudos.
4. Sur deux PC de la maison : l'hôte lit son adresse (affichée en haut du salon, du genre
   `192.168.1.23`), l'autre la tape et rejoint.

## ÉTAPE 2 (04/10, v25) : lancer le match ensemble — FAIT

> Martin, après le premier essai : « on arrive à se connecter, sauf qu'après, on ne peut pas
> lancer de game. On est juste dans le truc. »

- **L'hôte règle et lance** : dans son salon, *Bots* (en plus des amis), *Niveau*, *Manches*,
  *Durée max*, puis **Lancer**. Le salon se ferme (`NetLink.Locked`).
- **Le choix des cartes** : chacun choisit **chez lui**, à son tour (`NetGame.SendPick` →
  l'hôte fait `Match.Draft.TryPick`). L'hôte fait choisir les bots ; un ami qui ne choisit pas
  en 30 s se voit choisir une carte.
- **La même île** : l'hôte envoie l'état du match 4 fois par seconde (graine, manches, victoires,
  capacités, cartes) ; quand il charge une manche, il augmente le **numéro de l'île**
  (`NetGame.RoundToken`) et les invités chargent la même.
- **Se voir bouger** : 20 fois par seconde, chaque invité dit où il est ; l'hôte dit à tous où
  sont tous les joueurs (lui, ses bots, les invités) et la Couronne. Chez chacun, les autres sont
  des **marionnettes** (`Rival` en mode `Remote` : il suit, il ne pense pas).
- **Chacun joue SON joueur chez lui** (aucune latence pour bouger, sauter, planer). **Les coups**
  sur un ami partent chez lui, par l'hôte (`Combat.Hit` → `NetGame.RemoteHit`) ; c'est sa
  machine qui le projette. Les obstacles et les gargouilles, chacun a les siens.
- **L'hôte décide de la Couronne** (prise, vol, chute) et **de la fin de manche** (le sacre,
  le chrono). Un invité **demande** (`NetGame.AskCrown`) ; l'hôte passe par les mêmes portes que
  tout le monde (`Crown.TryTakeFor`, `TrySteal`, `KnockOff`…) et le dit à tous (`Crown.Mirror`).
- **Un ami part en plein match** : sa place devient un bot, là où il était. **L'hôte part** :
  retour à l'écran En ligne, avec la raison. **Pas la même version** des deux côtés : refusé, et
  le jeu le dit (`NetLink.Version` = 7 depuis v30, le ciel de feu, 106 capacités ; 6 depuis v29, le Mode Dieu dans l'état du match, 93 capacités ; 5 depuis v28, 78 capacités ; 4 depuis v27 : 58 capacités et un message neuf, le
  **sort** — prison, glu, tête à l'envers, mini, encre, ballon — qui part chez la victime par
  l'hôte, `NetGame.RemoteAfflict`, comme un coup).
- Testé hors d'Unity : `sh Tools/reseau.sh` (messages fiables avec 30 % de paquets perdus exprès).

**Ce qui n'est pas encore partagé** (à voir en jouant) : les **effets** d'une capacité lancée
par un autre (tu en subis le coup, tu ne vois pas toujours l'éclair), le **Mur** et les **mines**
d'un autre (ils n'existent que chez lui), un **sanctuaire** pris par un autre (il reste allumé
chez toi), le **piqué d'aigle** d'un autre sur toi (pas d'alerte), le moment exact des
**obstacles** (chacun voit les siens). Depuis v26 : la **Tornade**, le **Trou noir**, le
**Trampoline** et la **Foudre** d'un autre n'existent que chez lui (leurs coups, eux, arrivent
bien chez toi), et un **Géant** n'est grand que chez lui. Depuis v27 : la **Bombe**, la **Glu**, les **Peaux
de banane**, le **Déluge**, le **Gant** et la **Toupie** d'un autre ne se voient que chez lui
(les coups, eux, arrivent) ; les **sorts** (prison, encre…) frappent bien l'ami chez lui. Rien de tout ça ne change qui gagne : la Couronne et la
fin de manche sont à l'hôte.

## ÉTAPE 2,5 (07/10, v28) : la liste des parties et le code — FAIT

> Martin, après avoir joué avec son frère sur le même wifi : « tout est parfait ; après, quand on
> sera sur différents réseaux, il faut un truc comme dans FPS Chess où t'as toutes les games et
> tu peux les rejoindre ; c'est chiant de mettre ton IP, trouve un code ».

- **La liste des parties** (`Net/NetFinder.cs`, du C# pur) : sur l'écran En ligne, le jeu crie
  toutes les 1,5 s sur le réseau (un paquet UDP en *broadcast*, sur chaque carte réseau et sur
  la machine elle-même) : « il y a une partie ? ». Chaque hôte répond (`Msg.Query` / `Msg.Info`
  dans `NetLink`) : son pseudo, le nombre de joueurs, s'il a lancé, sa version. La liste
  s'affiche sous Rejoindre ; **un clic, on rejoint**. Grisée si la partie a commencé ou n'est pas
  la même version. Testé hors d'Unity (`sh Tools/reseau.sh`).
- **Le code de partie** (`NetCode`) : l'adresse de l'hôte écrite en 7 signes faciles à dicter
  (ni O ni 0, ni I ni 1), du genre `4BH-XNUL`. L'hôte le voit en gros dans son salon ; le champ
  Rejoindre accepte le code **ou** l'adresse.
- **Honnêtement** : le code et la liste ne marchent que là où une adresse IP marche aussi — **le
  même réseau** (la box, le wifi) ou un **réseau virtuel** qui imite un réseau local (Radmin VPN :
  la liste y trouve les parties). Pour deux maisons différentes **sans** Radmin, aucun code ne
  suffit : les box bloquent les connexions qui arrivent de dehors. Il faut un intermédiaire sur
  Internet — c'est l'étape 4.

## ÉTAPE 4 (à venir) : Steam — la liste du monde entier, les invitations, la traversée des box

C'est **exactement ce que fait FPS Chess** : sa liste de parties, ce sont les **« lobbies »
Steam** (Steam garde la liste des parties publiques du jeu, pour le monde entier), et ses
connexions passent par les **relais de Steam** (Steam Datagram Relay), qui traversent toutes les
box, sans serveur à nous (la loi n° 2 tient : pas de serveur dédié).

Ce qu'il faudra :
1. **Steamworks.NET** (gratuit, MIT) dans le projet : un paquet Unity par une adresse Git
   (`Window ▸ Package Manager ▸ + ▸ Install package from git URL`). Claude peut écrire le code,
   **mais ne peut pas le tester** : il faut Steam ouvert sur le PC.
2. **Un numéro d'application Steam** : pour les essais, le **480** (« Spacewar », l'appli de test
   de Valve, que tout le monde peut utiliser) ; pour de vrai, **Steam Direct** (100 $ une fois,
   le compte Steamworks de Martin) — de toute façon nécessaire pour la page Steam (Phase 5).
3. Dans le code, seul le **tuyau** change : `NetLink` envoie ses paquets par
   `SteamNetworkingMessages` au lieu d'une prise UDP ; la liste vient de
   `SteamMatchmaking.RequestLobbyList` au lieu de `NetFinder` ; « Inviter un ami » ouvre
   l'overlay Steam. `NetGame` (les messages du jeu) ne bouge pas.

**Étape 3** : partager ce qui manque ci-dessus, si ça gêne en jouant. **Porte 3** : un match de
30-45 min à plusieurs sans plantage ni désynchronisation.

---


> Écrit le 26/09/2026 au soir, avec la refonte « La Couronne ». Martin : « il faut
> commencer que tu fasses le mode en ligne, pour prévoir déjà. 4 joueurs max. »

## Ce qu'il faut savoir d'abord

**Le jeu ne se joue pas encore en ligne.** On ne peut pas, aujourd'hui, héberger une
partie et y inviter un ami. Il faut pour ça deux briques qui n'existent pas encore dans
le projet :

1. **Un transport** : c'est ce qui envoie les messages d'une machine à l'autre. Le choix
   verrouillé est **Steam P2P** (via *Facepunch.Steamworks* ou *Steamworks.NET*).
2. **Une couche réseau Unity** : *Netcode for GameObjects* (le paquet officiel d'Unity).
   Elle synchronise les objets et envoie les appels de l'hôte aux autres.

Les deux s'installent dans l'éditeur (`Window > Package Manager`) et se testent à deux
machines. Claude ne peut ni les télécharger ni les compiler depuis son environnement :
c'est un vrai chantier de Phase 3, pas un réglage.

**Ce qui est fait, c'est l'architecture** : le jeu est déjà découpé comme un jeu en
ligne. Brancher le réseau consistera à *remplacer* un bot par un joueur distant, pas à
réécrire le jeu.

## Le modèle : un hôte, trois invités

- **Listen-server** : l'un des quatre joueurs **héberge**. Sa machine fait tourner le
  « vrai » jeu : les gardes, les coffres, la Couronne, le chrono, les coups.
- Les **invités** envoient ce qu'ils *veulent* faire (avancer, frapper, pousser,
  prendre), et reçoivent ce qui *se passe*.
- **Autorité absolue de l'hôte** : qui a la Couronne, qui est touché, qui gagne la
  manche. Aucun invité ne décide de ça.

## Ce qui est déjà prêt dans le code

### 1. Les places (`Match/Match.cs`)

`Match.Slots` contient **quatre places au plus**. Chaque `PlayerSlot` a :

| Champ | Rôle aujourd'hui | Rôle en Phase 3 |
|---|---|---|
| `IsLocal` | la place de cette machine (toi) | la place de cette machine (une par machine) |
| `IsBot` | tenue par un bot | faux pour un joueur en ligne ; vrai pour une place vide comblée par un bot |
| `Wins`, `Abilities` | le score et les capacités | idem — c'est **l'hôte** qui les tient et les envoie |

`Match` est **statique** et survit au rechargement de la scène : c'est le seul état qui
traverse les manches. En ligne, l'hôte l'envoie au début de chaque manche (une poignée
d'octets : quatre noms, quatre couleurs, des victoires, des capacités, la graine).

### 2. La graine de la manche

Le monde est **entièrement reconstruit** à partir de deux nombres :

- `GameConfig.worldSeed` : le relief, la forêt, le château (les mêmes à chaque manche) ;
- `Match.RoundSeed` : le Monument, les sanctuaires et leurs dons, les points de
  départ (différents à chaque manche).

**Conséquence** : l'hôte n'envoie pas le monde, seulement la graine. Chaque machine
reconstruit exactement le même. (Attention en Phase 3 : tout ce qui utilise
`UnityEngine.Random` sans graine devra passer par une graine partagée ou être décidé
par l'hôte. Les Yeux et les pendules, eux, ne dépendent que du temps : l'hôte les tient.)

### 3. Les gestes passent tous par des portes

Tout ce qui change l'état du jeu passe par une méthode qui prend **le joueur qui agit**
(`Seeker`) — que ce soit toi, un bot, ou demain un joueur distant :

| Geste | Méthode |
|---|---|
| Prendre la Couronne | `Crown.TryTakeFor(seeker)` |
| La poser au Monument | `Monument.TryDeliver(seeker)` |
| La voler (pousser le porteur) | `Crown.TrySteal(voleur, porteur)` (appelée par `Combat.Shove`) |
| Monter sur une arbaleste / tirer | `Ballista.Mount(seeker)`, `Ballista.MountAndAim(seeker, vitesse)` |
| Tomber dans les nuages | `Respawn.Of(seeker)` |
| Prendre un don | `Shrine.TryTakeFor(seeker)` |
| Pousser | `Combat.Shove(seeker, direction)` |
| Lancer une capacité | `AbilityCaster.Cast(seeker, capacité, œil, visée)` |
| Projeter (tout coup) | `Combat.Hit(victime, vitesse, étourdissement, lâche, seeker)` |
| Choisir une capacité | `Match.Draft.TryPick(place, carte)` |
| Fin de manche | `Menus.EndRound(place)` (un seul endroit) |

En Phase 3, un invité enverra « je veux prendre la Couronne » ; **l'hôte** appellera
`Crown.TryTakeFor(sonSeeker)` et dira à tous le résultat. Les bots, eux, appellent déjà
ces méthodes directement (voir `World/Rival.cs`) : un joueur distant n'est qu'un
« bot » dont les décisions viennent du réseau.

### 4. Ce qui se voit de loin est déjà dans le monde

Chaque joueur porte une **écharpe**, une **lanterne à sa couleur** et un **halo**
au-dessus de la tête (`PlayerLook` dans `World/Rival.cs`) : on repère les autres dans
la brume sans marqueur d'interface. En ligne, rien à ajouter.

## Ce qu'il restera à faire en Phase 3 (dans l'ordre)

1. **Installer** Netcode for GameObjects + le transport Steam ; une scène de test à
   deux machines qui se voient bouger.
2. **Le salon en ligne** : l'écran « En ligne » existe déjà (Titre → *En ligne*), avec
   *Héberger* et *Rejoindre* grisés. Les brancher sur les invitations Steam.
3. **Le joueur distant** : un `RemotePlayer` (le pendant de `Rival`) qui reçoit la
   position et les intentions d'un invité, et appelle les mêmes méthodes que le bot.
4. **La synchronisation** : l'hôte envoie 10 à 20 fois par seconde la position des
   quatre joueurs, des gardes proches et de la Couronne ; les événements (coup, coffre
   ouvert, Couronne prise, fin de manche) partent une seule fois, au moment où ils
   arrivent.
5. **La porte 3** : une manche de 30-45 min à quatre, sans plantage ni désynchronisation.

## Ce qu'on ne fera pas

- **Pas de serveur dédié** (loi n° 2 du projet) : c'est toujours l'un des joueurs qui
  héberge.
- **Pas de persistance** entre deux matchs : rien n'est sauvegardé.
- **Pas plus de huit** joueurs (27/09 : 2 à 8).
