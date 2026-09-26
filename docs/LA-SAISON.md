# LA COURONNE — la bible du jeu

> Réécrite le 27/09/2026 au soir, sur la demande de Martin : « le jeu est incroyable, il
> faut juste un truc : supprimer la forêt ; une tour énorme avec des obstacles bien
> faits ; une belle couronne, un beau respawn ; des méga effets ; qu'on commence tous à
> côté ; en haut on prend un planeur et on plane jusqu'à un endroit hors du château ;
> des énormes arbalètes pour se tirer dessus ; et quand quelqu'un court avec la
> couronne, il la reprend en une demi-seconde ».
> Complétée le 28/09 : « je ne sais même pas comment on fait pour planer » (le vol
> refait : plus rien à apprendre), « des endroits où poser la couronne, où on veut »
> (trois Monuments), « des meilleures arbalètes », de nouveaux Yeux, et des pouvoirs
> bien plus forts (« le souffle, que ça passe toute la map »).
>
> Puis, le 28/09 au soir : « un jeu plein d'aura (la Yara Yara funk) », « la route
> jusqu'au château plus compliquée », « chacun commence sur sa plateforme, les arbalètes
> pour remonter direct, c'est cheaté », « plus d'obstacles, aléatoires », « les cartes
> sont mal faites », « on comprend pas les touches », « la première carte : une passive ;
> une active sur le clic gauche, une sur E, pas sur C », « choper facilement la couronne
> en l'air », « les yeux : un truc plus moyenâgeux ».
>
> Puis le 29/09 : « quand tu gagnes, on te voit TOI, avec ton pseudo », « les pseudos
> au-dessus des persos », « plus on avance dans les manches, plus c'est compliqué »,
> « les catapultes te posent sur la terre ferme devant le château », « quatre portes, une
> au milieu de chaque muraille », « plus fair pour tout le monde », « un passif et un
> clic gauche, qui changent à chaque manche ; E pour interagir », « l'œil trop facile :
> un BOUM qui fait redescendre », « du combat », « enlève les courants de la rampe »,
> et une musique de référence : « Montagem Orquestra – Isagi ».
>
> C'est **la référence** : quand le code et ce document ne disent pas la même chose,
> c'est un des deux qu'il faut corriger. L'histoire des choix : `docs/100-RAISONS.md`,
> `docs/100-PROBLEMES.md`. Les versions précédentes (la forêt, l'épée, les objets, la
> Garde Pâle…) sont dans l'historique Git.

---

## En une phrase

**Jusqu'à huit joueurs sur une île qui flotte au-dessus des nuages. Une Couronne au
sommet d'une tour de 100 m. On la prend, on saute, on PLANE jusqu'à l'un des trois
Monuments posés sur les îlots — et tous les autres vous volent dessus pour vous la voler.**

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

1. **Le départ.** Chacun apparaît sur **SA PLATEFORME** : un petit rocher volant à
   42 m de haut, **en face d'une des quatre portes** (deux plateformes par porte, de
   part et d'autre de son axe) : tout le monde est **à la même distance de sa porte et
   de la tour**. Un cercle et un fanion à sa couleur, et **SON ARBALESTE**. Un panneau
   montre **les touches**. **3, 2, 1, PARTEZ !** — trois secondes de protection.
2. **L'approche.** **E** pour monter sur son arbaleste, **clic gauche** : elle te pose
   **en cloche sur le parvis devant ta porte** (jamais dans les pièges). **On n'entre
   pas dans la citadelle par les airs** : le **sceau** renvoie dehors qui essaie. On
   passe le **couloir piégé**, le **pont-levis**, la porte.
3. **La montée.** En face de chaque porte, **sa rampe** (quatre rampes, le même chemin
   pour tous), **à pied**, en se battant. Au sommet, la Couronne sur son socle (**E
   maintenu 1 s**), et tout autour les **planeurs** : des **ailes d'or**.
4. **Le vol.** On saute dans le vide : **les ailes s'ouvrent toutes seules**. On va où
   l'on regarde (voir « Le vol plané »). Destination : **un des trois Monuments**, sur
   trois îlots flottants (leurs colonnes bleues), d'autres à chaque manche — **celui
   qu'on veut** : le plus proche, ou celui que personne ne garde. On peut aussi s'y
   faire **tirer par une arbaleste**.
5. **Le Monument.** Entrer **dans son cercle lumineux** avec la Couronne : manche gagnée.
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
- **En l'air, on l'attrape facilement : LE PIQUÉ D'AIGLE.** Le porteur dans le viseur
  (à 45 m, dans un cône de 30°), la touche pour pousser : on **fond sur lui**, guidé, à
  48 m/s ; au contact, c'est un **vol**. Recharge 3 s. (Choisi parmi : une poussée à
  plus grande portée en l'air, un vol au simple contact, un aimant… Le piqué est le
  plus lisible et le plus spectaculaire, et il récompense la visée.)
- **Replier ses ailes** (Espace en vol) avec la Couronne et tomber comme une pierre :
  elle **reste là où tu as quitté le sol**. Tomber dans les nuages avec : elle
  **rentre au sommet**.

### Le vol plané (refait le 28/09)

- **Tout le monde a des ailes, tout le temps.** Elles **s'ouvrent seules** dès qu'on
  tombe avec plus de 6 m de vide sous les pieds.
- **On vole comme on regarde** : la souris vers le bas, on **pique** et on prend de la
  vitesse (jusqu'à 150 km/h) ; vers le haut, on **remonte** en dépensant cette vitesse.
  À plat, 13 m/s et on descend doucement. **Q/D** glissent de côté, **S** freine.
- **Espace** en vol : replier les ailes ; encore : les rouvrir.
- **Les ailes d'or** (au sommet, ou au départ d'une arbaleste, jusqu'à l'atterrissage ;
  toujours avec le passif Planeur) : 25 % plus vite, on descend moins.
- **Les courants d'air** : une colonne de vent entre l'île et chaque îlot (filets qui
  montent, anneaux pâles). En planant dedans, **on remonte** — jusqu'à 72 m, jamais
  jusqu'au sommet de la tour.
- Le **porteur** vole un peu moins vite (la Couronne pèse) : on peut le rattraper.
- **Touché en vol** (étourdi) : les ailes se ferment, on tombe, elles se rouvrent.
- Ce qu'on sent : la caméra **penche** dans les virages, le champ de vision s'ouvre
  avec la vitesse, le vent souffle, des filets d'air filent autour ; l'écran dit
  « EN VOL — 90 km/h ».

### Tomber dans les nuages

On ne meurt pas. Qui tombe de l'île (ou rate l'îlot) **réapparaît sur sa
plateforme de départ**, dans une **colonne de lumière** à sa couleur, protégé 3 s.

---

## Les touches (refaites le 29/09)

| Touche | Ce qu'elle fait |
|---|---|
| **Clic gauche** | **TA capacité active** — une seule, nouvelle à chaque manche (le don d'un sanctuaire la remplace pour la manche) |
| **Clic droit** | **POUSSER** (3 m, recharge 0,9 s, projette fort) — pousser le porteur, c'est lui **voler** la Couronne ; **en l'air, sur le porteur : le piqué d'aigle** |
| **E** | **interagir** : prendre la Couronne, un don ; **monter sur une arbaleste** (et en descendre) |
| **Espace** | sauter ; en vol, replier ou rouvrir les ailes |
| **Sur l'arbaleste de ta plateforme** | clic gauche : elle te pose devant ta porte |
| **Sur une autre arbaleste** | souris : viser ; **clic gauche maintenu : tendre**, relâché : tiré |
| **F1 ou H** | le panneau des touches, à tout moment |
| **Tab** | le score et les capacités de chacun |

Plus de R, de C ni de V (« pas d'autres conneries »). Le réglage **« Pousser sur »**
inverse les deux clics. Les capacités qu'on **vise** : **maintiens** le clic, un
**aperçu** montre où elle ira ; **relâche** pour lancer.

Il n'y a **pas de vie**, pas de mort, pas d'objet, rien en main. Un coup projette et
étourdit un court instant (0,2 à 0,7 s), jamais plus.

---

## Les 26 capacités

**Un passif et un clic gauche, c'est tout — et ils CHANGENT À CHAQUE MANCHE.** Avant
chaque manche, deux tours de table : une **PASSIVE** d'abord, puis une **ACTIVE** (le
clic gauche). Chaque carte prise **remplace** celle de la manche d'avant. Le vainqueur de
la manche choisit en dernier.

**Les cartes** (refaites le 28/09) : elles arrivent **face cachée** (un dos de velours,
un losange d'or) et **se retournent** une à une ; la face : une pierre granuleuse, un
lavis à la couleur de la capacité, un **cadre d'or** ouvragé, un ruban (ACTIVE · CLIC
GAUCHE / PASSIVE), le nom, un fleuron, la phrase, la recharge. Celle qu'on vise se
soulève, s'entoure de **rayons qui tournent** et d'étincelles ; la prendre : un éclair,
une gerbe d'étincelles, un **coup de phonk**.

### Actives (le clic gauche)

| Capacité | Ce que ça fait | Recharge |
|---|---|---|
| **Ruée** | Douze mètres d'un trait : qui est sur ta route est bousculé. | 6 s |
| **Grappin** | Vise un mur, un rebord, la tour (48 m) : le grappin t'y tire. | 7 s |
| **Crochet** | Vise un joueur, jusqu'à 32 m : il est tiré jusqu'à toi. | 10 s |
| **Onde de choc** | Une explosion : tout le monde à neuf mètres s'envole. | 8 s |
| **Clignement** | Tu disparais et réapparais quinze mètres plus loin. | 5 s |
| **Bond** | Un saut immense ; le souffle du départ repousse ceux qui sont tout près. | 8 s |
| **Mur** | Un mur de 10 m sur 4,5 m surgit devant toi ; qui est dessus s'envole. | 12 s |
| **Nuée** | Un nuage de fumée de 16 m : les gargouilles et les autres ne voient plus rien. | 14 s |
| **Mine** | Pose une mine : elle envoie en l'air tous ceux qui passent à 4 m. | 9 s |
| **Givre** | Une boule de givre (26 m) : l'éclat bouscule et ralentit, 6,5 m autour. | 8 s |
| **Voile** | Tu deviens invisible pendant sept secondes. | 16 s |
| **Échange** | Vise un joueur, jusqu'à 45 m : vous échangez vos places. | 14 s |
| **Rappel** | Tu reviens là où tu étais il y a quatre secondes. | 10 s |
| **Souffle** | **Une vague de vent qui traverse toute l'île** (150 m, 60 m/s, de 3 à 18 m de large) et emporte tout le monde, même en vol. | 9 s |

### Passives (toujours là)

| Capacité | Ce que ça fait |
|---|---|
| **Double saut** | Appuie encore sur Espace en l'air : un second saut. |
| **Planeur** | Des ailes d'or pour toujours : tu voles plus vite et plus loin. |
| **Coureur** | Tu vas quinze pour cent plus vite. |
| **Porteur** | Avec la Couronne, tu n'es plus ralenti et tu peux pousser. |
| **Poigne** | Ta poussée envoie deux fois plus loin. |
| **Ancrage** | On te pousse deux fois moins loin. |
| **Flair** | Tu vois toujours où est la Couronne, même à travers les murs. |
| **Ombre** | Les gargouilles mettent deux fois plus de temps à te repérer. |
| **Prise ferme** | Le premier coup ne te fait pas lâcher la Couronne. |
| **Recharge** | Tes capacités reviennent un tiers plus vite. |
| **Rebond** | Retomber de haut fait une onde de choc autour de toi. |
| **Aimant** | La Couronne à terre vole jusqu'à toi. |

(Le code : `Match/Abilities.cs` pour la liste, `World/AbilityCaster.cs` pour les effets.)

### Les sanctuaires

Un cercle de pierres levées, un cristal qui flotte à la couleur de son don. **E
maintenu 1 s** : ce don (une capacité active au hasard) **remplace ton clic gauche pour
la manche**. Un seul don à la fois ; un sanctuaire ne sert qu'une fois. Il y en
a quatre sur l'île, hors des murs, et un sur chaque îlot flottant sans Monument (la
récompense de ceux qui y vont en planant ou par l'arbaleste).

---

## L'île

- **Une île flottante** d'environ 200 m au-dessus d'une **mer de nuages** ; herbe dorée
  dessus, roche qui s'effile vers le bas. Un ciel de fin de journée, un soleil bas.
- **Six îlots flottants** autour, à 150-175 m du centre, entre 6 et 30 m de haut.
  **Trois Monuments** s'y posent (d'autres à chaque manche) : un arc de pierre, deux
  braseros bleus, six pierres levées, un **cercle lumineux** qui bat quand quelqu'un
  porte la Couronne. Les autres îlots portent un **sanctuaire**. Chaque îlot a son
  **arbaleste**, tournée vers la tour, pour revenir.
- **Six courants d'air**, un entre l'île et chaque îlot.
- **La citadelle** au centre : enceinte de 100 m, murs de 18 m, quatre tours d'angle,
  **quatre portes, une au milieu de chaque muraille** : deux tours de garde, la herse
  relevée, un **pont-levis** abaissé tenu par deux chaînes, un **arc d'or**, une grande
  **bannière** ; quatre escaliers vers les remparts.
- **Huit plateformes de départ** (une par joueur, deux par porte), à 42 m de haut,
  chacune avec **son arbaleste** qui pose sur le parvis de sa porte. On y réapparaît
  après une chute.
- **Les arbalestes géantes** (ornées de flammes et de filets d'or) : celle de chaque plateforme, quatre dehors sur l'herbe
  (elles visent les îlots), une par îlot (pour revenir). **Plus aucune dans la cour.**
  Un socle de pierre à merlons, une tourelle tournante cerclée de bronze, un treuil à
  deux roues, un fanion ; un arc dont les **bras plient** quand on tend, une **corde**
  qui recule, un **carreau de cinq mètres** sur lequel on s'assoit. On règle l'angle à
  la souris et la **tension** au clic (de 28 à 56 m/s). L'anneau d'arrivée est **vert**
  sur la terre ferme, **bleu** sur un Monument, **rouge** sur le sceau de la citadelle
  (il te renverra) ; pas d'anneau : la ligne file dans le vide, tu planeras. On part
  avec des **ailes d'or**. Les bots montent sur leur **carreau qui vole**.
- **Le sceau de la citadelle** : qui entre dans l'enceinte **par les airs** (en planant,
  tiré, en piqué) est renvoyé dehors dans un éclair de runes. On en sort en volant sans
  souci.
- **Le parcours des portes** (tiré au hasard à chaque manche) : devant chaque porte, un
  **couloir** de 30 m bordé de murets de 3 m (on ne les saute pas), une arche de
  lumière à l'entrée, un **parvis** devant, et **quatre stations** parmi (plus rapides à
  chaque manche) :
  - une **chicane** : un mur en travers, un passage d'un côté (on zigzague) ;
  - un **moulinet** : une barre cloutée qui tourne à hauteur de genou (on saute) ;
  - une **herse** : des pointes qui jaillissent du sol (ses runes rougissent avant) ;
  - un **marteau** : une masse de fer qui balaie le couloir d'un mur à l'autre.

## La tour de la Couronne

- **100 m de haut**, 26 m de large. **QUATRE RAMPES** en spirale entrelacées, de 6,5 m
  de large, **sans parapet** : chacune part **en face d'une porte**, fait deux tours et
  arrive au sommet — **le même chemin pour tout le monde**. Entre deux rampes, 12,5 m :
  tomber, c'est atterrir sur la rampe du dessous (ou dans le vide). Une **arche d'or**
  au pied de chacune. **Six bandes de couleur** (bleu, vert, or, orange, rouge,
  violet : bannières, liseré du bord, filets d'or) : on lit sa hauteur d'un regard.
- **Les obstacles, TIRÉS AU HASARD à chaque manche**, **le même nombre sur chaque
  rampe**, et **de plus en plus nombreux et rapides à chaque manche** (manche 1 : 2
  trous, 2 pendules, 2 béliers, 2 balayeurs, 1 herse par rampe ; jusqu'à 4, 4, 3, 4, 3).
  Tous annoncés avant de frapper, tous laissent une **traînée de braise**, et
  projettent **loin** (un impact qui explose) :
  - des **trous** à sauter en courant (une barre rouge au bord) ;
  - des **pendules** à pointes qui balaient la rampe du mur vers le vide ;
  - des **béliers** qui jaillissent du mur (leur rune **rougit** avant) ;
  - des **balayeurs** : une barre cloutée à hauteur de genou, contre le fût, qui balaie
    la rampe **vers le vide** (on saute par-dessus) ;
  - des **herses** : des pointes qui jaillissent de la rampe ;
  - des **boulets** qui dévalent une rampe depuis le sommet, d'un côté ou de l'autre ;
  - les **gargouilles** (ci-dessous) ;
  - **plus de courants** sur la rampe (« ça c'est n'importe quoi »).
- **Au sommet** : la Couronne sur un socle à trois marches cerclées d'or, un cercle de
  runes qui tourne, quatre cristaux qui gravitent ; **huit planeurs** sur leurs
  chevalets ; quatre braseros.

### Les gargouilles (pas de PNJ humains)

**Seize gargouilles** (28/09, à la place des Yeux : « un truc plus moyenâgeux » ; plus
dures le 29/09 : elles voient à 42 m, chargent en moins d'une seconde, et leur jet de
feu **EXPLOSE** — 3,6 m autour — et **projette hors de la rampe** : on redescend) :
des bêtes de pierre accroupies, ailes repliées, cornes, deux yeux qui luisent, une
gueule. Quatre sur les tours d'angle et quatre au-dessus des portes (tournées vers la
cour), huit sur des consoles du fût de la tour (deux par rampe). Leur **tête
tourne** ; elles ne quittent jamais leur perchoir.

| Couleur | Ce qu'elle fait |
|---|---|
| **Ambre** | elle balaie la cour du regard (un cône de lumière) |
| **Orange** | elle t'a aperçu : elle te fixe, ses ailes s'entrouvrent |
| **Rouge** | elle **charge** 1,1 s : ailes déployées, gueule ouverte qui rougeoie, un trait rouge vous relie et une **cible rouge se resserre à tes pieds**. La dernière demi-seconde, elle ne te suit plus — bouge ! |
| **Blanc** | elle **crache un jet de feu** : une **explosion** là où il frappe ; projeté, étourdi 0,7 s, et **tu lâches la Couronne** |

Elles ne regardent que la citadelle, la tour — et le porteur de la Couronne. Elles
ignorent qui est protégé. La Nuée les aveugle, le Voile te cache, l'Ombre les ralentit.

## L'AURA

« Un jeu plein d'aura » : quand tu fais quelque chose de fort — **prendre la Couronne
au sommet**, la **voler**, un **piqué d'aigle**, **éjecter** quelqu'un dans les nuages
(moins de 6 s après l'avoir frappé), un **doublé** ou un **triplé** à l'Onde ou au
Souffle — c'est un **MOMENT D'AURA** : le temps **ralentit** trois quarts de seconde, un
**gros titre** claque (« COURONNE VOLÉE », « +1000 AURA »), un **coup de phonk**, des
**flammes d'aura** montent autour de toi, les bords de l'écran brûlent à ta couleur.
Les bots ont leurs flammes aussi.

- Le **porteur** de la Couronne brûle d'une **aura d'or** qu'on voit de loin ; si c'est
  toi, les bords de l'écran battent au rythme de la musique, qui passe en **PHONK**.
- **LA VICTOIRE** (29/09 : « on te voit TOI, avec ton pseudo ») : la caméra quitte tes
  yeux et **tourne autour du gagnant**, en contre-plongée, pendant que son aura pulse
  (anneaux d'or, gerbes, flammes) ; son **PSEUDO en grand, en lettres d'or**, sur des
  rayons de lumière, puis « REMPORTE LA MANCHE » et « +1000 AURA ».
- La musique d'aura est **fabriquée par le code** façon **« montagem » orchestral**
  (29/09, référence de Martin : « Montagem Orquestra – Isagi ») : cordes piquées,
  cuivres, chœur, basse 808, rythme de funk brésilien, 130 BPM. Le vrai morceau n'est
  pas à nous (Steam le refuserait) ; un fichier dont le nom contient « phonk », « aura »
  ou « funk » dans `Assets/_Fief/Resources/Music` remplace la musique fabriquée — pour
  tester chez toi, n'importe quoi ; pour Steam, un morceau libre de droits.

## Les pseudos

- **Réglages ▸ Pseudo** : on tape son pseudo (16 lettres) ; il est gardé.
- **Au-dessus de chaque joueur, son pseudo** — rien d'autre — à sa couleur (en or pour
  le porteur), plus gros de près, lisible jusqu'à 170 m.
- Le score, la fin de manche et le podium disent les pseudos.
- Tes coups **portent** : une micro-pause et un tremblement à chaque impact.

---

## Ce qu'on voit

- **Pas de boussole, pas de carte, pas de marqueur** : la tour, les colonnes bleues des
  Monuments, la colonne dorée de la Couronne, les courants d'air, les colonnes et
  fanions des plateformes de départ, les arches des couloirs.
- **Les joueurs se voient** : écharpe, lanterne et halo à leur couleur ; leurs **ailes
  dans le dos** (repliées au sol, grandes ouvertes en vol, liseré d'or pour les ailes
  d'or) ; une **bulle** quand ils sont protégés.
- **Les effets** : chaque capacité a sa signature (onde qui gonfle, anneaux, gerbes,
  éclairs, traînées), les coups ont leur impact, le respawn sa colonne de lumière.
- **L'écran, sans une seule icône** : le chrono, une phrase qui dit où est la Couronne
  (« … en plein vol », « … sur la rampe »), le score, **tes capacités en cartes** en bas
  au centre (la touche, le nom, un liseré à sa couleur ; en recharge un rideau sombre
  qui remonte et les secondes ; prête, elle luit ; tenue pour viser, elle se soulève),
  ton état (AILES D'OR, EN VOL — 90 km/h, COURANT D'AIR, PROTÉGÉ), le fil des
  événements, des astuces au bon moment.

## Les autres joueurs (des bots, en attendant le jeu en ligne)

Ils jouent **avec tes règles, par les mêmes méthodes** (`World/Rival.cs`) et courent
presque aussi vite que toi (9 / 10,2 / 10,7 m/s selon leur niveau, toi 10,8). Ils
quittent leur plateforme par **leur arbaleste** (visée devant une porte) ou en planant,
passent le **couloir** de la porte (ses chicanes, en contournant les moulinets), montent
la rampe de leur porte (sautent les trous, **les balayeurs et les moulinets**,
**changent de côté devant un boulet**), prennent les **arbalestes**, sautent du sommet et
**planent** jusqu'au Monument le plus commode (près d'eux, que personne ne garde), vont
chercher un **courant d'air** quand ils sont trop bas, **chassent** le porteur (en vol
aussi, avec le **piqué d'aigle**) et l'un d'eux va **l'attendre au Monument** le plus
proche de lui. **Ils se battent en montant** (29/09 : « faut qu'il y ait du combat ») :
qui passe à portée dans la citadelle ou sur la rampe se fait pousser — toi d'abord. Ils se servent de toutes leurs capacités (le Souffle, jusqu'à 90 m).

## Retiré

- Le 26/09 : stèles, butin, ressources, camp et caches, construction, Autels,
  boussole, carte.
- Le 27/09 : l'épée, la vie et la mort, tous les objets, les coffres, la Garde Pâle et
  le Roi Creux, les loups, les revenants, le cerf blanc, le donjon, toutes les icônes.
- **Le 27/09 au soir : LA FORÊT** (arbres, lieux-dits, creux, feux-follets, sons du
  sous-bois, escalade des arbres), la brume épaisse, l'orage et la nuit.
- Le 28/09 au soir : la ligne de départ dans la cour (remplacée par les plateformes),
  les arbalestes de la cour (elles envoyaient « direct tout en haut »), les Yeux
  flottants (remplacés par les gargouilles), la touche C.
- Le 29/09 : la rampe unique (remplacée par quatre), les courants de la rampe, les
  touches R et V, les capacités qui s'accumulent (une passive + un clic gauche, neufs à
  chaque manche), la touche F (E interagit), les mots à côté des noms.

## Phases

- **Phase 1 (ici)** : le match complet contre des bots, avec le salon.
- **Phase 3** : le jeu en ligne (voir `docs/RESEAU.md` : les bots cèdent leur place à
  des joueurs, rien d'autre ne change).
