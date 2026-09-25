# LA COURONNE — la bible du jeu

> Réécrite le 26/09/2026 au soir, sur la demande de Martin : « on oublie tout ce qui
> est stèle, récolter pour gagner de la gloire. Ce qui compte, c'est d'avoir la
> couronne et la ramener au monument. » C'est **la référence** : quand le code et ce
> document ne disent pas la même chose, c'est un des deux qu'il faut corriger.
> Les versions précédentes (stèles, butin, mage, reliques…) sont dans l'historique Git.

---

## En une phrase

**Quatre joueurs, chacun pour soi. Une Couronne au sommet d'un château gardé. La
première personne qui la porte jusqu'au Monument gagne la manche. Entre deux
manches, chacun choisit un pouvoir — le vainqueur choisit en dernier.**

---

## Le match

| | |
|---|---|
| Joueurs | 2 à **4** (toi + des bots en Phase 1 ; des joueurs en ligne en Phase 3) |
| Manches | **3, 5, 7 ou 10** (choisi au salon ; 5 par défaut) |
| Durée max d'une manche | **4, 6, 8 ou 10 min** (6 par défaut) → un match de 5 manches ≈ 30 min |
| Vainqueur du match | le plus de manches gagnées ; à égalité, une **manche de départage** entre ex æquo |

### Une manche

1. **Le départ.** Les quatre joueurs apparaissent à la lisière, aux quatre coins de la
   forêt. Le **Monument** change de place à chaque manche : on voit sa colonne de
   lumière bleue au-dessus des arbres. Le **château** est au centre ; sa tour de guet
   dépasse de la brume.
2. **La forêt.** On peut y foncer droit au château, ou fouiller : coffres, objets
   enterrés (pelle + détecteur), objets magiques.
3. **Le château.** La Couronne est sur la terrasse du donjon, gardée par **le Roi
   Creux** (le boss) et toute la **Garde Pâle**.
4. **La Couronne.** Qui la prend **brille** : une colonne dorée monte au-dessus de lui,
   tout le monde sait où il est. Il marche plus lentement (−18 %), **ne peut ni frapper
   ni pousser** (il la tient à deux mains) et **la lâche s'il tombe, s'il se fait
   pousser, s'il marche dans un piège ou si le Roi le frappe**. Tombée, elle attend
   **45 s** qu'on la ramasse (un compte à rebours sous l'icône), puis rentre sur son socle.
5. **Le Monument.** Porter la Couronne jusqu'à lui et maintenir E deux secondes :
   **manche gagnée**.
6. **Le temps.** Si le chrono tombe à zéro, celui qui tient la Couronne gagne la
   manche ; si personne ne la tient, personne ne gagne.

### Le choix des pouvoirs (entre deux manches)

On étale **(nombre de joueurs + 1) cartes** tirées au hasard. Chacun en prend une, dans
l'ordre : **le moins de manches gagnées choisit en premier, le vainqueur de la manche
choisit en dernier.** Un pouvoir se garde jusqu'à la fin du match.

| Pouvoir | Effet |
|---|---|
| **Double saut** | un second saut en l'air |
| **Ruée** | R : un bond de 8 m vers l'avant (toutes les 6 s) |
| **Coureur** | +15 % de vitesse |
| **Poigne** | ta poussée envoie deux fois plus loin, et revient plus vite |
| **Colosse** | +50 % de vie |
| **Ombre** | les gardes te voient deux fois moins vite |
| **Flair** | la Couronne et ses porteurs brillent pour toi à travers les murs |
| **Porteur** | avec la Couronne, tu cours à pleine vitesse |
| **Sang vif** | la vie remonte trois fois plus vite |
| **Seconde chance** | la première fois que tu tombes dans une manche, tu te relèves sur place |

---

## Ce qu'on fait avec ses mains

- **Clic gauche : l'épée.** Quatre coups tuent un joueur. On la garde toujours.
- **Clic droit : pousser.** Tout le monde peut pousser : l'autre part en arrière — et
  **s'il porte la Couronne, il la lâche**. Toutes les 3 s.
- **1, 2, 3 : les objets trouvés** (trois emplacements). On les prend en main, clic
  gauche pour s'en servir.
- **E : prendre** (Couronne, objets, coffres), **déposer** au Monument, tirer le levier.
- **F : grimper** aux arbres (géants compris : là-haut, la brume s'ouvre).
- **R : la Ruée** (si on a le pouvoir). **Tab** : le score du match.
- La vie remonte seule après 6 s sans coup. Tomber, c'est lâcher ses objets dans une
  **dépouille** (qu'on peut fouiller) et se relever 5 s plus tard à son point de départ.

### Les objets de la forêt

| Objet | Où | Effet |
|---|---|---|
| **Détecteur** | coffres | en main, il bipe de plus en plus vite près d'un **trésor enterré** |
| **Pelle** | coffres | en main, clic : creuse ; sur un trésor enterré, sort un objet rare |
| **Fumigène** | coffres | lancé : un nuage où les gardes ne voient plus rien |
| **Fiole de lenteur** | coffres | lancée : ralentit les joueurs touchés 5 s |
| **Piège à mâchoires** | coffres | posé : immobilise 3 s qui marche dessus — **et lui fait lâcher la Couronne** |
| **Élixir** | coffres | rend toute la vie |
| **Plume** | enterré | 30 s de sauts très hauts |
| **Cape d'ombre** | enterré | 10 s invisible pour les gardes |
| **Clé du donjon** | enterré | ouvre la porte dérobée de la cave du donjon |

Les **coffres** (une quinzaine) s'ouvrent d'un E. Les **trésors enterrés** (une dizaine)
ne se voient pas : il faut le détecteur pour les trouver et la pelle pour les sortir.

---

## Le château (« digne d'Elden Ring en contenu »)

- **Trois entrées** : la **grande porte** (herse baissée — un levier, dans la cour au
  pied du châtelet, la lève pour 45 s), la **poterne** au nord (étroite, gardée), la
  **brèche** à l'est (un talus d'éboulis monte jusqu'au mur effondré, un autre redescend
  dans la cour).
- **Les remparts** : deux escaliers de pierre y montent depuis la cour (nord et sud).
  Les arbalétriers y sont postés.
- **Les réserves** : trois bâtiments le long des murs, chacun avec un coffre (un objet
  rare), sous l'œil des sentinelles.
- **La porte dérobée** : au pied du mur nord du donjon, fermée à clé. Avec la **Clé du
  donjon**, on l'ouvre — un escalier dans le mur monte **droit à la terrasse**. Ouverte,
  elle le reste pour tout le monde.
- **Le donjon** : trois niveaux et une terrasse, reliés par des escaliers ; chaque
  salle a ses gardes.
- **La terrasse** : la Couronne sur son socle, et **le Roi Creux**.

### La Garde Pâle

Des chevaliers **lisses et sans visage** — armures d'ivoire usé, grandes capes
sombres, une fente de visière qui luit comme une braise. Un seul style, cohérent, pas
de cubes : des formes rondes et polies. Quand ils te voient, la fente passe au rouge.

| | Nombre | Comportement |
|---|---|---|
| **Sentinelle** | 16 | ronde, cône de vision (la lanterne), crie et rameute ceux qui l'entendent, poursuit, frappe au glaive (coup annoncé : la fente devient blanche) |
| **Arbalétrier** | 7 | sur les remparts (porte, châtelet, nord, ouest, est) et au 1er étage ; il **vise** une seconde (un trait rouge le relie à toi), puis tire un carreau qu'on voit partir — on l'esquive en bougeant |
| **Molosse** | 3 | chien de garde dans la cour : rapide, fragile, sent à 8 m tout autour, mord |
| **Le Roi Creux** | 1 | le boss de la terrasse : trois mètres, dort devant son trône jusqu'à ce qu'on approche la Couronne (9 m) ou qu'on la prenne ; **balayage** devant lui (35), **frappe au sol** toutes les 6 s (un cercle rouge grandit sous lui pendant 1 s : 45 et projeté au loin) ; 800 PV ; ne quitte pas la terrasse ; revient 4 min après sa chute |

Les gardes s'intéressent à **quiconque entre dans l'enceinte**, et courent après **le
porteur de la Couronne** même hors les murs (jusqu'à 60 m). On les sème (brume, murs,
fumigène, Cape d'ombre) ou on les tue ; ils reviennent 90 s plus tard.

**Vingt-sept en tout** (la liste exacte : `World/Garrison.cs`) : huit sentinelles et
trois molosses dans la cour, six arbalétriers sur les remparts, une sentinelle par
salle du donjon et un arbalétrier au 1er étage, deux gardes royaux et le Roi sur la
terrasse, trois rôdeurs dans la forêt.

**Leur visage dit tout.** La fente du heaume : *braise* (ronde), *orange* (il t'a vu),
*rouge* (il court), *blanc* (il frappe — écarte-toi).

---

## Ce qu'on voit (pas de boussole, pas de carte)

- Pas de boussole, pas de carte, pas de marqueurs : on se repère **aux lumières** —
  la tour de guet du château, la colonne bleue du Monument, la colonne dorée de la
  Couronne — et aux arbres géants (en haut, la brume s'ouvre).
- **Les joueurs se voient** : chacun porte une lanterne à sa couleur et un halo au-dessus
  de la tête, visibles de loin dans la brume.
- L'écran : une belle **barre de vie**, les pouvoirs choisis, les trois objets, le chrono
  de la manche et le score des quatre joueurs. Presque pas de texte.

## Ce qui reste de la forêt

Loups (deux meutes), revenants près des lieux-dits (**abattu, un revenant lâche un
objet**), feux-follets, cerf blanc, les creux à pierre-lune (décor, repères lumineux),
les cinq lieux-dits (chacun avec un coffre), les arbres géants.

## Les autres joueurs (des bots, en attendant le jeu en ligne)

Ils jouent **avec tes règles, par les mêmes portes** (`World/Rival.cs`) : ils fouillent
la forêt au début (plus ou moins longtemps selon leur caractère), montent à la Couronne
(par la herse si elle est levée, sinon la poterne ou la brèche ; par l'escalier dérobé
s'ils ont la clé), la portent au Monument, **chassent et poussent** celui qui la tient,
se jettent sur elle quand elle roule, rendent les coups, et se servent de leurs objets
(élixir, fumigène, fiole sur le porteur, piège sur la route du Monument).

## Retiré le 26/09 au soir

Les stèles, le butin (★), les ressources (bois, pierre-lune, fer), le camp et les
caches, la construction (T), les Autels, la boussole et la carte.

## Phases

- **Phase 1 (ici)** : le match complet contre des bots, avec le menu du salon.
- **Phase 3** : le jeu en ligne (voir `docs/RESEAU.md` : l'architecture est déjà
  préparée pour que les bots cèdent leur place à des joueurs).
