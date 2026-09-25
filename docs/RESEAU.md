# Le jeu en ligne — ce qui est prêt, ce qui reste (Phase 3)

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
| `Wins`, `Powers` | le score et les pouvoirs | idem — c'est **l'hôte** qui les tient et les envoie |

`Match` est **statique** et survit au rechargement de la scène : c'est le seul état qui
traverse les manches. En ligne, l'hôte l'envoie au début de chaque manche (une poignée
d'octets : quatre noms, quatre couleurs, des victoires, des pouvoirs, la graine).

### 2. La graine de la manche

Le monde est **entièrement reconstruit** à partir de deux nombres :

- `GameConfig.worldSeed` : le relief, la forêt, le château (les mêmes à chaque manche) ;
- `Match.RoundSeed` : le Monument, les coffres, les trésors enterrés, les points de
  départ (différents à chaque manche).

**Conséquence** : l'hôte n'envoie pas le monde, seulement la graine. Chaque machine
reconstruit exactement le même. (Attention en Phase 3 : tout ce qui utilise
`UnityEngine.Random` sans graine — le contenu des revenants, les loups — devra passer
par une graine partagée ou être décidé par l'hôte.)

### 3. Les gestes passent tous par des portes

Tout ce qui change l'état du jeu passe par une méthode qui prend **le joueur qui agit**
(`Seeker`) — que ce soit toi, un bot, ou demain un joueur distant :

| Geste | Méthode |
|---|---|
| Prendre la Couronne | `Crown.TryTakeFor(seeker)` |
| La poser au Monument | `Monument.TryDeliver(seeker)` |
| Ouvrir un coffre | `Chest.TryOpenFor(seeker)` |
| Fouiller une dépouille | `Remains.TakeFor(seeker)` |
| Pousser | `Combat.Shove(seeker, direction)` |
| Frapper | `Combat.Hit(victime, seeker, dégâts)` |
| Lancer un objet | `Thrown.Launch(seeker, objet, départ, vitesse)` |
| Poser un piège | `Trap.Place(seeker, position, angle)` |
| Tirer le levier | `Lever.PullFor(seeker)` |
| Porte dérobée | `SecretDoor.UseFor(seeker)` |
| Choisir un pouvoir | `Match.Draft.TryPick(place, carte)` |
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
- **Pas plus de quatre** joueurs.
