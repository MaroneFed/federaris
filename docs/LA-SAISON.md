# LA COURONNE — la bible du jeu

> Réécrite le 27/09/2026, sur la demande de Martin : « réduis un peu la map, fais en
> sorte qu'on s'amuse à balle ; les PNJ, soit un truc tellement excellent soit rien ;
> un giga château ; les pouvoirs on comprend rien ; pas d'épée, juste des capacités,
> on tiendra jamais rien en main ; j'aime pas les icônes et les menus moches ».
> C'est **la référence** : quand le code et ce document ne disent pas la même chose,
> c'est un des deux qu'il faut corriger. Pourquoi chaque choix : `docs/100-RAISONS.md`.
> Les versions précédentes (épée, objets, Garde Pâle, Roi Creux…) sont dans l'historique Git.

---

## En une phrase

**Quatre joueurs, chacun pour soi, les mains vides. Une Couronne au sommet d'une tour
de 64 m. Le premier qui la porte jusqu'au Monument gagne la manche. On ne se bat pas :
on se pousse, on se projette, on se vole la Couronne — avec des capacités.**

C'est **Smash** (on ne meurt pas, on perd sa place) croisé avec **Fall Guys** (une tour
à gravir à quatre en se poussant dans le vide), dans une forêt noire.

---

## Le match

| | |
|---|---|
| Joueurs | 2 à **4** (toi + des bots en Phase 1 ; des joueurs en ligne en Phase 3) |
| Manches | **3, 5, 7 ou 10** (choisi au salon ; 5 par défaut) |
| Durée max d'une manche | **4, 6, 8 ou 10 min** (6 par défaut) |
| Avant la manche 1 | chacun **choisit sa première capacité** (que des actives sur la table) |
| Entre deux manches | chacun choisit **une capacité de plus** ; le vainqueur de la manche en dernier |
| Vainqueur du match | le plus de manches gagnées ; à égalité, une **manche de départage** entre ex æquo |

### Une manche

1. **Le départ.** Chacun apparaît à la lisière, à égale distance de la citadelle. Le
   **Monument** (colonne de lumière bleue) change de place à chaque manche.
2. **La forêt** (320 × 320 m, brume à 18 m). On peut foncer à la citadelle, ou passer
   par un **sanctuaire** (voir plus bas) pour gagner un don.
3. **La citadelle.** Quatre portes ouvertes. Dans la cour, **la tour de la Couronne** ;
   sur les murs et autour de la tour, **les Yeux**.
4. **La tour.** Une rampe en spirale, à l'extérieur, quatre tours complets, **sans
   parapet**, avec trois trous et quatre pendules. On monte à quatre, on se pousse.
5. **La Couronne.** Au sommet. **E maintenu 1,2 s** pour la prendre (0,5 s si elle est
   à terre). Qui la porte **brille** (colonne dorée), va **15 % moins vite**, **ne peut
   ni pousser ni lancer de capacité offensive** (crochet, onde, souffle, givre) — sauf
   avec le passif Porteur. Il la **lâche** si on le pousse, si une capacité le projette,
   si un Œil le touche, s'il marche sur une mine, si un pendule le balaie.
6. **Tomber avec la Couronne.** Si le porteur chute de haut (sans Planeur), **elle
   reste là où il a quitté le sol**. On ne redescend pas la tour d'un saut : il faut
   la rampe — ou le Planeur.
7. **À terre**, elle attend **45 s** qu'on la ramasse, puis revient au sommet.
8. **Le Monument.** Porter la Couronne jusqu'à lui et **maintenir E 2 s** : manche
   gagnée. Ralenti, la Couronne se pose sur l'autel, la caméra tourne autour.
9. **Le temps.** Au gong, celui qui tient la Couronne gagne ; sinon, personne.

---

## Les mains vides

- **Clic gauche : POUSSER.** Le plus proche devant toi (3 m) part en arrière et en
  l'air. **S'il porte la Couronne, il la lâche.** Recharge 0,9 s.
- **Clic droit, R, C : tes trois capacités actives**, dans l'ordre où tu les as prises.
- **V : le don** d'un sanctuaire (pour la manche seulement).
- **Espace** : sauter (encore une fois en l'air avec Double saut ; maintenu : planer
  avec Planeur).
- **E** (maintenu) : prendre la Couronne, prendre un don, poser au Monument.
- **F** : grimper à un arbre (les géants dépassent la brume).
- **Tab** : le score et les capacités de chacun.

Il n'y a **pas de vie**, pas de mort, pas d'objet, rien en main. Un coup projette et
étourdit un court instant (0,2 à 0,7 s), jamais plus.

---

## Les 26 capacités

Trois actives au plus : en prendre une quatrième **remplace la plus ancienne** (la carte
le dit avant qu'on choisisse). Les passives s'accumulent.

### Actives (sur clic droit, R, C — ou V pour un don)

| Capacité | Ce que ça fait | Recharge |
|---|---|---|
| **Ruée** | Un bond de huit mètres droit devant toi. | 6 s |
| **Grappin** | Vise un mur, un arbre, la tour : le grappin t'y tire. | 7 s |
| **Crochet** | Vise un joueur : il est tiré jusqu'à toi. | 10 s |
| **Onde de choc** | Projette tout le monde autour de toi. | 8 s |
| **Clignement** | Tu disparais et réapparais dix mètres plus loin. | 5 s |
| **Bond** | Un saut immense, droit vers le ciel. | 8 s |
| **Mur** | Un mur de pierre surgit devant toi pendant huit secondes. | 12 s |
| **Nuée** | Un nuage de fumée : les Yeux et les autres ne voient plus rien. | 14 s |
| **Mine** | Pose une mine : qui marche dessus s'envole et lâche la Couronne. | 9 s |
| **Givre** | Lance une boule de givre : ceux qu'elle touche sont ralentis. | 8 s |
| **Voile** | Tu deviens invisible pendant six secondes. | 16 s |
| **Échange** | Vise un joueur : vous échangez vos places. | 14 s |
| **Rappel** | Tu reviens là où tu étais il y a quatre secondes. | 10 s |
| **Souffle** | Une rafale repousse tout ce qui est devant toi. | 7 s |

### Passives (toujours là)

| Capacité | Ce que ça fait |
|---|---|
| **Double saut** | Appuie encore sur Espace en l'air : un second saut. |
| **Planeur** | Maintiens Espace en l'air : tu planes. La Couronne ne tombe pas. |
| **Coureur** | Tu vas quinze pour cent plus vite. |
| **Porteur** | Avec la Couronne, tu n'es plus ralenti et tu peux pousser. |
| **Poigne** | Ta poussée envoie deux fois plus loin. |
| **Ancrage** | On te pousse deux fois moins loin. |
| **Flair** | Tu vois toujours où est la Couronne, même à travers les murs. |
| **Ombre** | Les Yeux mettent deux fois plus de temps à te repérer. |
| **Prise ferme** | Le premier coup ne te fait pas lâcher la Couronne. |
| **Recharge** | Tes capacités reviennent un tiers plus vite. |
| **Rebond** | Retomber de haut fait une onde de choc autour de toi. |
| **Aimant** | La Couronne à terre vole jusqu'à toi. |

(Le code : `Match/Abilities.cs` pour la liste, `World/AbilityCaster.cs` pour les effets.)

### Les sanctuaires

Un cercle de pierres levées, un cristal qui flotte à la couleur de son don. **E
maintenu 1 s** : ce don (une capacité active au hasard) devient la tienne **pour la
manche**, sur **V**. Un seul don à la fois ; un sanctuaire ne sert qu'une fois. Il y en
a un dans chaque lieu-dit, et quatre dans les clairières (≈ 9 par manche).

---

## La citadelle (« un giga château »)

- **L'enceinte** : 100 m de côté, murs de 18 m, quatre tours d'angle (34 m).
- **Quatre portes ouvertes**, une par face, chacune entre deux tours de garde : on entre
  de partout.
- **Quatre escaliers** montent aux remparts : on y voit loin, on saute sur la rampe.
- **La tour de la Couronne**, au centre : 22 m de large, **64 m de haut** (elle perce la
  brume, on la voit de partout). La rampe extérieure fait 5,5 m de large, quatre tours
  complets, **trois trous** à sauter, **quatre pendules** qui la balaient. Au sommet :
  la Couronne sur son socle et quatre braseros.

### Les Yeux (pas de PNJ humains)

Martin : « les PNJ, soit un truc tellement excellent, soit rien ». Donc rien d'humain.
**Douze Yeux** : une sphère de pierre qui flotte, un iris qui luit, deux anneaux qui
tournent. Huit sur les tours et les portes, quatre autour de la tour (un par tour de
rampe). Ils ne marchent pas : ils ne se coincent jamais.

| Couleur | Ce qu'il fait |
|---|---|
| **Bleu** | il balaie la cour de son regard (un cône de lumière) |
| **Orange** | il t'a aperçu : il te fixe |
| **Rouge** | il **charge** 1,1 s : un trait rouge vous relie. La dernière demi-seconde, il ne te suit plus — bouge ! |
| **Blanc** | il tire : projeté, étourdi 0,7 s, et **tu lâches la Couronne** |

Ils ne regardent que la citadelle, la tour — et le porteur de la Couronne. La Nuée les
aveugle, le Voile te cache, l'Ombre les ralentit.

### Les pendules

Quatre lames de pierre, une par tour de rampe, qui balaient la rampe d'un bord à
l'autre. Touché : projeté (souvent dans le vide), et la Couronne tombe.

---

## Ce qu'on voit

- **Pas de boussole, pas de carte, pas de marqueur** : la tour qui perce la brume, la
  colonne bleue du Monument, la colonne dorée de la Couronne, les creux bleus, les
  arbres géants.
- **Les joueurs se voient** : lanterne, écharpe et halo à leur couleur ; leur nom
  s'écrit au-dessus d'eux à moins de 22 m.
- **L'écran, sans une seule icône** : le chrono et la manche en haut, une phrase qui
  dit où est la Couronne, le score en chiffres en haut à droite, tes capacités en bas à
  gauche (touche, nom, recharge), un point de visée qui rougit quand tu peux pousser.
- **Les menus** : une colonne de mots sur la forêt ; tout au clavier (↑ ↓ ← → Entrée
  Échap) comme à la souris.

## Ce qui reste de la forêt

Les creux à pierre-lune (décor, repères bleus), les feux-follets, les cinq lieux-dits
(chacun avec son sanctuaire), les ruines, les arbres géants. **Plus de loups, de
revenants ni de cerf blanc** : la forêt est le terrain de la poursuite, pas un deuxième
ennemi.

## Les autres joueurs (des bots, en attendant le jeu en ligne)

Ils jouent **avec tes règles, par les mêmes méthodes** (`World/Rival.cs`) : ils passent
par un sanctuaire s'il y en a un près d'eux, montent la tour (et se servent du grappin,
de la ruée, du double saut), prennent la Couronne, la portent au Monument, **chassent
et poussent** celui qui la tient, lancent leurs capacités (`AbilityCaster.Cast`, comme
toi). Ils ne parlent pas : on entend leur voix, pas des phrases.

## Retiré

- Le 26/09 au soir : stèles, butin (★), ressources, camp et caches, construction (T),
  Autels, boussole, carte (M).
- **Le 27/09** : l'**épée** et la **vie** (plus de mort), **tous les objets** (détecteur,
  pelle, fumigène, fiole, piège, élixir, plume, cape, clé), les **coffres**, la **Garde
  Pâle** et le **Roi Creux**, les **loups**, les **revenants**, le **cerf blanc**, le
  **donjon à étages**, la herse et la porte dérobée, **toutes les icônes**.

## Phases

- **Phase 1 (ici)** : le match complet contre des bots, avec le salon.
- **Phase 3** : le jeu en ligne (voir `docs/RESEAU.md` : les bots cèdent leur place à
  des joueurs, rien d'autre ne change).
