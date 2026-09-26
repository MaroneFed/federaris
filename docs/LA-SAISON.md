# LA COURONNE — la bible du jeu

> Réécrite le 27/09/2026 au soir, sur la demande de Martin : « le jeu est incroyable, il
> faut juste un truc : supprimer la forêt ; une tour énorme avec des obstacles bien
> faits ; une belle couronne, un beau respawn ; des méga effets ; qu'on commence tous à
> côté ; en haut on prend un planeur et on plane jusqu'à un endroit hors du château ;
> des énormes arbalètes pour se tirer dessus ; et quand quelqu'un court avec la
> couronne, il la reprend en une demi-seconde ».
> C'est **la référence** : quand le code et ce document ne disent pas la même chose,
> c'est un des deux qu'il faut corriger. L'histoire des choix : `docs/100-RAISONS.md`,
> `docs/100-PROBLEMES.md`. Les versions précédentes (la forêt, l'épée, les objets, la
> Garde Pâle…) sont dans l'historique Git.

---

## En une phrase

**Jusqu'à huit joueurs sur une île qui flotte au-dessus des nuages. Une Couronne au
sommet d'une tour de 100 m. On la prend, on saute, on PLANE jusqu'au Monument posé sur
un îlot — et tous les autres vous volent dessus pour vous la voler.**

C'est **Smash** (on ne meurt pas, on se fait pousser dans le vide), **Fall Guys** (une
tour à gravir à plusieurs, bourrée d'obstacles) et un peu de **deltaplane**.

---

## Le match

| | |
|---|---|
| Joueurs | **2 à 8** (toi + 1 à 7 bots en Phase 1 ; 6 par défaut ; des joueurs en ligne en Phase 3) |
| Bots | **faciles, normaux ou coriaces** (choisi au salon ; normaux par défaut) |
| Manches | **3, 5, 7 ou 10** (choisi au salon ; 5 par défaut) |
| Durée max d'une manche | **4, 6, 8 ou 10 min** (6 par défaut) |
| Avant la manche 1 | chacun **choisit sa première capacité** (que des actives sur la table) |
| Entre deux manches | chacun choisit **une capacité de plus** ; le vainqueur de la manche en dernier |
| Vainqueur du match | le plus de manches gagnées ; à égalité, une **manche de départage** entre ex æquo |

### Une manche

1. **Le départ.** Chacun apparaît **sur sa petite zone** (un disque lumineux et un
   fanion à sa couleur), sur une **ligne de départ en arc** dans la cour : toutes les
   zones sont **à la même distance du pied de la rampe**, côte à côte. Personne ne part
   avantagé. **3, 2, 1, PARTEZ !** — trois secondes de protection pour s'élancer.
2. **La montée.** La rampe de la tour, en courant — ou une **arbaleste géante** qui
   t'envoie d'un coup haut sur la rampe.
3. **Le sommet.** La Couronne sur son socle (**E maintenu 1 s**). Et tout autour, les
   **planeurs** : en arrivant au sommet, **on prend des ailes**.
4. **Le vol.** On saute du sommet et on **plane** (Espace maintenu ; le porteur plane
   tout seul) jusqu'à l'**îlot flottant du Monument** (sa colonne bleue), qui change à
   chaque manche. Regarder vers le bas : on pique, plus vite. On peut aussi y aller en
   se faisant **tirer par une arbaleste** depuis l'île.
5. **Le Monument.** Entrer **dans son cercle** avec la Couronne : manche gagnée.
6. **Le temps.** Au gong, celui qui tient la Couronne gagne ; sinon, personne.

### La Couronne

- Qui la porte **brille** (colonne dorée), va **15 % moins vite**, **ne pousse pas** et
  ne lance pas de capacité offensive (sauf avec le passif Porteur).
- **POUSSER LE PORTEUR, C'EST LUI VOLER LA COURONNE** : elle passe directement dans
  tes mains (un trait d'or). Le voleur est **protégé 1,5 s** (une bulle de lumière) ;
  la victime **ne peut pas la reprendre pendant 3 s**. C'est ce qui empêche le porteur
  de la « reprendre en une demi-seconde ».
- Les autres coups (onde, souffle, Œil, pendule, bélier, boulet, mine) la font
  **tomber** ; même verrou de 3 s pour qui la perd. À terre, on la **ramasse en passant
  dessus** ; oubliée, elle **rentre au sommet au bout de 20 s**.
- **Sauter sans ailes** avec la Couronne : elle **reste là où tu as quitté le sol**.
  Tomber dans les nuages avec : elle **rentre au sommet**.

### Tomber dans les nuages

On ne meurt pas. Qui tombe de l'île (ou rate l'îlot) **réapparaît sur sa zone de
départ**, dans une **colonne de lumière** à sa couleur, protégé 3 s.

---

## Les mains vides

- **Clic gauche : POUSSER.** Le plus proche devant toi (3 m) part en arrière et en
  l'air, étourdi 0,2 s. **S'il porte la Couronne, tu la lui voles.** Recharge 0,9 s.
- **Clic droit, R, C : tes trois capacités actives**, dans l'ordre où tu les as prises.
- **V : le don** d'un sanctuaire (pour la manche seulement).
- **Espace** : sauter (encore une fois en l'air avec Double saut) ; **maintenu en l'air,
  avec des ailes : planer**.
- **E** : prendre la Couronne sur son socle, prendre un don, **monter sur une arbaleste**.
- **Sur une arbaleste** : la souris vise (la **trajectoire se dessine en lumière**, un
  anneau marque l'arrivée), **clic gauche tire**, E descend.
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
| **Planeur** | Tu as des ailes partout, pas seulement depuis le sommet. |
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
a quatre sur l'île, hors des murs, et un sur chaque îlot flottant sauf celui du
Monument (la récompense de ceux qui y vont en planant ou par l'arbaleste).

---

## L'île

- **Une île flottante** d'environ 200 m au-dessus d'une **mer de nuages** ; herbe dorée
  dessus, roche qui s'effile vers le bas. Un ciel de fin de journée, un soleil bas.
- **Six îlots flottants** autour, à 150-175 m du centre, entre 6 et 30 m de haut. Le
  **Monument** se pose sur l'un d'eux (un autre à chaque manche) ; les autres portent
  un **sanctuaire**.
- **La citadelle** au centre : enceinte de 100 m, murs de 18 m, quatre tours d'angle,
  **quatre portes ouvertes**, quatre escaliers vers les remparts.
- **Huit arbalestes géantes** : quatre dans la cour (elles visent la tour), quatre
  dehors sur l'herbe (elles visent les îlots). Elles tirent toujours à 50 m/s : on règle
  la distance avec l'angle — le milieu de la tour, ou un îlot.

## La tour de la Couronne

- **100 m de haut**, 26 m de large. Une **rampe en spirale** extérieure de 6,5 m de
  large, **six tours**, **sans parapet**. **Chaque tour a sa couleur** (bleu, vert, or,
  orange, rouge, violet : bannières et liseré du bord) : on lit sa hauteur d'un regard.
- **Les obstacles**, tous annoncés avant de frapper :
  - **5 trous** à sauter en courant (une barre rouge au bord) — et sous chacun, un tour
    plus bas, un **courant** (disque pâle au bord intérieur) qui renvoie d'un tour vers
    le haut, à travers le trou ;
  - **5 pendules** à pointes qui balaient la rampe du mur vers le vide (ils sifflent) ;
  - **6 béliers** qui jaillissent du mur toutes les 4 s (leur rune **rougit** une
    demi-seconde avant) ;
  - des **boulets** qui dévalent la rampe depuis le sommet toutes les 20 s, d'un côté
    ou de l'autre : on change de côté ;
  - les **Yeux** (ci-dessous).
- **Au sommet** : la Couronne sur un socle à trois marches cerclées d'or, un cercle de
  runes qui tourne, quatre cristaux qui gravitent ; **huit planeurs** sur leurs
  chevalets ; quatre braseros.

### Les Yeux (pas de PNJ humains)

**Quatorze Yeux** : une sphère de pierre qui flotte, un iris qui luit, deux anneaux qui
tournent. Huit sur les tours et les portes de la citadelle, six autour de la tour (un
par tour de rampe).

| Couleur | Ce qu'il fait |
|---|---|
| **Bleu** | il balaie la cour de son regard (un cône de lumière) |
| **Orange** | il t'a aperçu : il te fixe |
| **Rouge** | il **charge** 1,1 s : un trait rouge vous relie. La dernière demi-seconde, il ne te suit plus — bouge ! |
| **Blanc** | il tire : projeté, étourdi 0,7 s, et **tu lâches la Couronne** |

Ils ne regardent que la citadelle, la tour — et le porteur de la Couronne. Ils
ignorent qui est protégé. La Nuée les aveugle, le Voile te cache, l'Ombre les ralentit.

---

## Ce qu'on voit

- **Pas de boussole, pas de carte, pas de marqueur** : la tour, la colonne bleue du
  Monument, la colonne dorée de la Couronne, les fanions des zones de départ.
- **Les joueurs se voient** : écharpe, lanterne et halo à leur couleur ; leurs **ailes
  dans le dos** quand ils en ont (grandes ouvertes quand ils planent) ; une **bulle**
  quand ils sont protégés.
- **Les effets** : chaque capacité a sa signature (onde qui gonfle, anneaux, gerbes,
  éclairs, traînées), les coups ont leur impact, le respawn sa colonne de lumière.
- **L'écran, sans une seule icône** : le chrono, une phrase qui dit où est la Couronne
  (« … en plein vol », « … sur la rampe »), le score, tes capacités, ton état (AILES,
  EN VOL, PROTÉGÉ), le fil des événements, des astuces au bon moment.

## Les autres joueurs (des bots, en attendant le jeu en ligne)

Ils jouent **avec tes règles, par les mêmes méthodes** (`World/Rival.cs`) et courent
presque aussi vite que toi (9 / 10,2 / 10,7 m/s selon leur niveau, toi 10,8). Ils
montent la rampe (sautent les trous, prennent les courants, **changent de côté devant
un boulet**), prennent les **arbalestes** (ils calculent l'angle), sautent du sommet et
**planent** jusqu'au Monument, **chassent** le porteur (en vol aussi) et l'un d'eux va
**l'attendre au Monument**. Ils se servent de toutes leurs capacités.

## Retiré

- Le 26/09 : stèles, butin, ressources, camp et caches, construction, Autels,
  boussole, carte.
- Le 27/09 : l'épée, la vie et la mort, tous les objets, les coffres, la Garde Pâle et
  le Roi Creux, les loups, les revenants, le cerf blanc, le donjon, toutes les icônes.
- **Le 27/09 au soir : LA FORÊT** (arbres, lieux-dits, creux, feux-follets, sons du
  sous-bois, escalade des arbres), la brume épaisse, l'orage et la nuit.

## Phases

- **Phase 1 (ici)** : le match complet contre des bots, avec le salon.
- **Phase 3** : le jeu en ligne (voir `docs/RESEAU.md` : les bots cèdent leur place à
  des joueurs, rien d'autre ne change).
