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
> Puis, toujours le 29/09, avec des captures : « quand tu vois ça c'est pas quali, au-dessus
> du château tout est buggé » (le sommet clignotait : corrigé, voir « La tour ») et « les
> obstacles doivent être mieux et nous faire retomber en bas de la tour ».
> Puis le 30/09, après un test avec son frère : « il n'aime pas les graphismes », « les
> trous étaient trop compliqués », « le saut bug », « que les obstacles te fassent
> VRAIMENT partir de la tour », « enlève tout ce qui est aura », « une petite animation
> avec toi quand tu gagnes », « avec les élytres c'est trop facile de gagner : rajoute
> de la complexité », « les bots ne peuvent pas prendre la couronne », « tout doit être
> exceptionnel, lisse », « un meilleur menu », « pas le panneau des touches au départ »,
> « un paramètre pour changer la touche », « des meilleurs designs de couronne ».
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
   **3, 2, 1, PARTEZ !** (les touches : F1 ou H) — trois secondes de protection.
2. **L'approche.** **E** pour monter sur son arbaleste, **clic gauche** : elle te pose
   **en cloche sur le parvis devant ta porte** (jamais dans les pièges). **On n'entre
   pas dans la citadelle par les airs** : le **sceau** renvoie dehors qui essaie. On
   passe le **couloir piégé**, le **pont-levis**, la porte.
3. **La montée.** En face de chaque porte, **sa rampe** (quatre rampes, le même chemin
   pour tous), **à pied**, en se battant. Au sommet, la Couronne sur son socle : **on la
   prend en montant sur le socle** (ou d'un appui sur E ; 01/10 : plus rien à tenir) ; qui
   pose le pied au sommet reçoit des **ailes d'or** — mais le porteur, lui, n'en a jamais.
4. **Le vol — LA COURONNE EST LOURDE** (30/09 : « avec les élytres, c'est trop facile
   de gagner »). On saute dans le vide : les ailes s'ouvrent seules, mais le porteur
   plane à 11 m/s et **tombe à 8 m/s** : du sommet, **il n'atteint pas les Monuments**.
   Il lui faut un **courant d'air** (y tourner pour remonter) ou une **arbaleste de
   l'île**. Pendant ce temps, les autres, plus rapides (ailes d'or), lui fondent dessus.
   Destination : **un des trois Monuments** (colonnes bleues), celui qu'on veut.
5. **Le sacre.** Rester **3 secondes** dans le cercle du Monument avec la Couronne : un
   disque d'or s'étend, une cloche sonne chaque seconde, une barre s'affiche à l'écran de
   TOUT LE MONDE (« X SE FAIT SACRER — VA LE POUSSER ! »). Sorti du cercle, le sacre
   retombe vite. Aucune touche à tenir.
6. **Le temps.** Au gong, **personne** ne gagne la manche (01/10, Martin : « la victoire,
   il ne faut pas la donner s'il a la couronne à la fin »). On ne gagne **qu'au Monument**.
7. **La fête.** Le vainqueur **danse sur la musique** (six figures, un pas par temps) ;
   la Couronne flotte au-dessus de sa tête ; confettis, feux d'artifice et projecteur
   battent sur le rythme (`CharacterRig.Party`, `VictoryShow`, `MusicDirector.DanceBeat`).

### La Couronne

- Qui la porte **brille** (colonne dorée), va **15 % moins vite**, **ne pousse pas** et
  ne lance pas de capacité offensive (sauf avec le passif Porteur).
- **POUSSER LE PORTEUR, C'EST LUI VOLER LA COURONNE** : elle passe directement dans
  tes mains (un trait d'or). Le voleur est **protégé 1,5 s** (une bulle de lumière) ;
  la victime **ne peut pas la reprendre pendant 3 s**. C'est ce qui empêche le porteur
  de la « reprendre en une demi-seconde ».
- Les autres coups (onde, souffle, Œil, pendule, bélier, boulet, mine) la font
  **tomber** ; même verrou de 3 s pour qui la perd. À terre, on la **ramasse en passant
  dessus**. **Elle reste là où elle tombe** (02/10 : plus de retour au sommet au bout de
  20 s) ; si elle tombe hors d'atteinte (dans le vide), elle revient au dernier endroit
  où son porteur touchait le sol.
- **En l'air, on l'attrape facilement : LE PIQUÉ D'AIGLE.** Le porteur dans le viseur
  (à 45 m, dans un cône de 30°), la touche pour pousser : on **fond sur lui**, guidé, à
  48 m/s ; au contact, c'est un **vol**. Recharge 3 s. (Choisi parmi : une poussée à
  plus grande portée en l'air, un vol au simple contact, un aimant… Le piqué est le
  plus lisible et le plus spectaculaire, et il récompense la visée.)
- **Replier ses ailes** (Espace en vol) avec la Couronne, sortir d'une arbaleste, tomber :
  elle **reste dans tes mains** (04/10, v24.2 : « hop, elle s'enlève, tout le temps »).
  Seul un porteur **assommé** en l'air (étourdi, éjecté) la lâche, là où il a quitté le
  sol. Tomber dans les nuages avec : elle
  **reste au dernier endroit où tu touchais le sol** (02/10, Martin : « c'est horrible de
  tout remonter à chaque fois ») — on ne refait plus toute la tour.

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
  montent, anneaux pâles). En planant dedans, **on remonte** (poussée 15 m/s) — jusqu'à
  80 m, jamais jusqu'au sommet de la tour. Le porteur doit **tourner dedans**.
- **Les anneaux de vent** (03/10, v22 — Martin : « de la tour jusqu'à un îlot, je comprends
  pas bien ; des boosts en l'air, comme dans Minecraft », `World/WindRing.cs`) : du sommet
  vers **chaque îlot**, une chaîne de **quatre grands anneaux** qui luisent, posés sur la
  trajectoire du porteur. On passe dedans en planant : la vitesse remonte d'un coup (20 m/s
  pour le porteur, 28 pour les autres) et le vent **soulève 1,3 s** (26 m/s au début), avec
  un « whoup » qui monte. Les quatre d'affilée : **l'enfilade**, un gros éclat d'or.
- **LE VOL LIBRE** (04/10 — Martin : « voler à fond comme on veut, mais seulement si on part
  d'une arbalète ») : **tiré par une arbaleste**, les ailes s'ouvrent dès le haut de la
  courbe et l'on **vole pour de vrai** : on va où l'on regarde (vers le haut, on monte),
  sans tomber, à 24 m/s (16 avec la Couronne), S freine, jusqu'à **se poser**. Le sceau de
  la citadelle tient toujours (pas de raccourci vers la tour).
- Le **porteur** : 11 m/s, chute de 8 m/s, jamais d'ailes d'or, la vitesse d'un piqué
  se perd vite. Il remonte dans un courant — ou **enfile les anneaux** : les quatre, et il
  atteint l'îlot (simulé : 8 à 9 s) ; un de raté, et il tombe court. Les bots porteurs
  suivent la chaîne.
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
| **Clic droit** | **POUSSER** (3,2 m, recharge 0,9 s) : **le poussé part en cloche à une quinzaine de mètres** (01/10), en faisant des saltos — pousser le porteur, c'est lui **voler** la Couronne ; **en l'air, sur le porteur : le piqué d'aigle** |
| **E** | **interagir** : prendre la Couronne, un don ; **monter sur une arbaleste** (et en descendre) |
| **Espace** | sauter ; en vol, replier ou rouvrir les ailes |
| **Sur l'arbaleste de ta plateforme** | clic gauche : elle te pose devant ta porte |
| **Sur une autre arbaleste** | souris : viser ; **clic gauche maintenu : tendre**, relâché : tiré |
| **F1 ou H** | le panneau des touches, à tout moment |
| **Tab** | le score et les capacités de chacun |

Plus de R, de C ni de V (« pas d'autres conneries »). **Réglages ▸ Touche capacité** et
**Touche pousser** (30/09) : au choix parmi les trois clics, F, R, X et V (jamais la
même pour les deux). Les capacités qu'on **vise** : **maintiens** le clic, un
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
| **Grappin** | Vise un mur, un rebord, la tour (48 m) : le grappin t'y tire. Jamais la muraille depuis dehors (on entre par une porte). | 7 s |
| **Crochet** | Vise un joueur, jusqu'à 32 m : il est tiré jusqu'à toi. | 10 s |
| **Onde de choc** | Une explosion : tout le monde à neuf mètres s'envole. | 8 s |
| **Clignement** | Tu disparais et réapparais quinze mètres plus loin. | 5 s |
| **Bond** | Un saut immense ; le souffle du départ repousse ceux qui sont tout près. | 8 s |
| **Mur** | Un mur de 10 m sur 4,5 m surgit du sol devant toi ; qui est dessus s'envole. Pas en plein vol. | 12 s |
| **Nuée** | Un nuage de fumée de 16 m : les gargouilles et les autres ne voient plus rien. | 14 s |
| **Mine** | Pose une mine au sol : elle envoie en l'air tous ceux qui passent à 4 m. Pas en plein vol. | 9 s |
| **Givre** | Une boule de givre (26 m) : l'éclat bouscule et ralentit, 6,5 m autour. | 8 s |
| **Voile** | Tu deviens invisible pendant sept secondes. | 16 s |
| **Échange** | Vise un joueur, jusqu'à 45 m : vous échangez vos places. Jamais à travers la muraille (l'un dedans, l'autre dehors), jamais avec un protégé. | 14 s |
| **Rappel** | Tu reviens là où tu étais il y a quatre secondes (jamais d'avant un respawn). | 10 s |
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
| **Flair** | Tu vois l'invisible : les mines, les joueurs voilés, à travers la fumée. (02/10 : la Couronne, tout le monde la voit maintenant.) |
| **Ombre** | Les gargouilles mettent deux fois plus de temps à te repérer. |
| **Prise ferme** | Le premier coup ne te fait pas lâcher la Couronne. |
| **Recharge** | Tes capacités reviennent un tiers plus vite. |
| **Rebond** | Retomber de haut fait une onde de choc autour de toi. |
| **Aimant** | La Couronne à terre vole jusqu'à toi. |

(Le code : `Match/Abilities.cs` pour la liste, `World/AbilityCaster.cs` pour les effets.)

### Les sanctuaires

Un cercle de pierres levées, un cristal qui flotte à la couleur de son don. **Un appui
sur E** (01/10) : ce don (une capacité active au hasard) **remplace ton clic gauche pour
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
- **Le parcours des portes** (tiré au hasard à chaque manche, **le même devant les quatre
  portes** — v13 : personne n'a le couloir facile) : devant chaque porte, un
  **couloir** de 30 m bordé de murets de 3 m (on ne les saute pas), une arche de
  lumière à l'entrée, un **parvis** devant, et **quatre stations** parmi (plus rapides à
  chaque manche) :
  - une **chicane** : un mur en travers, un passage d'un côté (on zigzague) ;
  - un **moulinet** : une barre cloutée qui tourne à hauteur de genou (on saute) ;
  - une **herse** : des pointes qui jaillissent du sol (ses runes rougissent avant) et
    piquent **tant qu'elles sont sorties** ;
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
  pendules, 2 béliers, 2 balayeurs, 1 herse par rampe ; jusqu'à 4, 3, 4, 3 — **02/10 : la
  première manche n'a qu'un pendule et un bélier**, cinq obstacles au lieu de sept). **Plus de
  trous** (30/09 : « trop compliqués »).
  Tous annoncés avant de frapper (des **bandes ambre peintes sur la rampe** là où
  frappent pendules et béliers), tous laissent une **traînée de braise**, et
  **UN OBSTACLE T'ÉJECTE DE LA TOUR** (29-30/09 : « qu'ils te fassent VRAIMENT
  partir ») : touché sur la rampe par un obstacle ou une gargouille, tu pars **en
  cloche** — 22 m/s vers l'extérieur, 11 vers le haut, l'élan ne retombe presque pas —
  en faisant des **saltos**, ailes fermées jusqu'au sol (`Seeker.Tumble`) : tu
  t'écrases dans la cour et tu remontes. Jamais au-delà de la muraille. Les coups des
  joueurs, eux, ne font que projeter.
  **Une seule famille** (03/10, v21 — Martin : « rends-les magnifiques, tous, tous, tous »,
  `World/ObstacleKit.cs`) : **le rouge frappe** (face de frappe crème à cible rouge),
  l'**ardoise** bleu nuit porte (comme les toits), l'**or mat** bague, la **braise** prévient ;
  des fûts aux bords ronds, des dômes, des boules — rien de carré qui frappe. Chaque pièce
  mobile est soudée en un seul dessin.
  - des **pendules** : un butoir en palet rouge au bout d'un bras d'acier, pendu à une
    potence d'ardoise sortie d'une platine ronde cerclée d'or, qui balaient la rampe du
    mur vers le vide ;
  - des **béliers** : un poing rouge sur un fût d'ardoise, qui jaillit d'un portail rond
    cerclé d'or dans le fût (leur rune **rougit** avant) ;
  - des **balayeurs** : une barre à rayures rouges et crèmes à hauteur de genou, sur une
    perche tournée contre le fût, qui balaie
    la rampe **vers le vide** (on saute par-dessus) ;
  - des **herses** : des pointes qui jaillissent de la rampe (elles piquent tant qu'elles
    sont sorties) ;
  - des **boulets** : une **volée** — un boulet sur **chaque** rampe, du même côté pour
    toutes (le mur, puis le vide, en alternance) — toutes les 34 s, moins aux manches
    suivantes ; (04/10) ils partent **un peu sous le sommet**, **s'annoncent** 1,8 s (ils
    tremblent, grondent deux fois) avant de rouler, et ne touchent **jamais** quelqu'un sur
    la plate-forme de la Couronne ;
  - les **gargouilles** (ci-dessous) ;
  - **plus de courants** sur la rampe (« ça c'est n'importe quoi »).
- **Le saut** (30/09 : « le saut bug ») : on colle à la pente en la descendant, un appui
  un poil trop tôt est gardé 0,15 s, un appui juste après le bord saute quand même
  (« coyote time »), Espace n'ouvre les ailes que s'il y a du vide dessous.
- **Au sommet** : la Couronne sur un socle à trois marches cerclées d'or, un cercle de
  runes qui tourne, quatre cristaux qui gravitent ; quatre braseros. (Les huit
  planeurs sur chevalets sont partis le 29/09 : les ailes s'ouvrent seules.)
  **Pas deux surfaces au même niveau** : le dernier filet d'or de la tour tombait pile
  sur le sol du sommet et clignotait en traits blancs (le *z-fighting*).
  **Les marches du socle** avaient un collider en **boule** de 2,6 m (le cylindre
  d'Unity reçoit une capsule, et une capsule plus large que haute devient une sphère) :
  les bots ne pouvaient pas approcher de la Couronne. `Proto.Cylinder` pose maintenant
  un collider à la vraie forme (30/09).

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
| **Rouge** | elle **charge** 1,25 s : ailes déployées, gueule ouverte qui rougeoie, un trait rouge vous relie et une **cible rouge se resserre à tes pieds**. La dernière demi-seconde, elle ne te suit plus — bouge ! |
| **Blanc** | elle **crache un jet de feu** : une **explosion** là où il frappe ; projeté, étourdi 0,7 s, et **tu lâches la Couronne** |

Elles ne regardent que la citadelle, la tour — et le porteur de la Couronne. Elles
ignorent qui est protégé. **Justes pour qui débute** (02/10 : « il a trois tirs sur lui
qui font bam ») : **une seule à la fois** sur la même cible, **9 s de répit** après un
coup (3 s après une esquive), une explosion de **2,4 m** : qui court hors de la cible
rouge s'en sort. Dessin du 02/10 : pierre bleu ardoise (elle se détache sur la pierre
crème), grosse tête ronde, **grands yeux** dans des orbites sombres (c'est eux qu'on lit
de loin), sourcil en V, cornes courtes, queue en fer de pique. La Nuée les aveugle, le Voile te cache, l'Ombre les ralentit.

## La victoire (plus d'aura)

Le 30/09, Martin : « enlève tout ce qui est aura, je déteste ça ». Plus de moments
d'aura, de ralenti, de « +1000 AURA », de flammes, de phonk, d'aura d'or du porteur.

- **LA VICTOIRE** (29/09 : « on te voit TOI, avec ton pseudo » ; 30/09 : « une petite
  animation avec toi ») : la caméra quitte tes yeux et **tourne autour du gagnant** ; son
  **PSEUDO en grand, en lettres d'or** en haut de l'écran, le score en bas — rien
  par-dessus lui. Et **il fête** : il saute les bras en l'air, fait un tour sur lui-même
  tous les quatre sauts, s'écrase à chaque réception ; **la Couronne vient flotter
  au-dessus de sa tête** ; des **confettis** à sa couleur.

## Le personnage et les graphismes (30/09 : « lisse, lisse »)

- **Le haricot** (toi et les bots ; simplifié le 03/10 — Martin : « hyper simples, garde le
  haricot et ses yeux ») : un **haricot** satiné à sa couleur et **deux grands yeux**, de
  tout petits pieds et mains ronds de la même couleur — rien d'autre. Il se dandine,
  s'écrase à l'atterrissage, s'étire en l'air, **fait des saltos** quand un obstacle
  l'éjecte, **écarquille les yeux** quand il est projeté ou qu'il danse ; quand un autre a
  gagné la manche, il **s'affaisse**, déçu. En première personne, on ne voit que son ombre.
- **L'image lissée** : anticrénelage x4 (x8 jusqu'au 03/10 : « le jeu n'est pas fluide »), filtrage anisotrope, ombres très fines en
  quatre cascades, synchro verticale, et une **sonde de reflets** qui photographie
  l'île et le ciel : l'or et l'acier reflètent le soir.
- **La Couronne** refaite : or poli, bandeau lisse entre deux joncs, lys à trois
  branches et pointes perlées, joyaux ronds, deux arceaux perlés, velours, globe.
- **L'arbaleste** refaite en formes rondes (poutres, bagues de bronze, roues cerclées,
  braseros), les **obstacles**, les gargouilles et les Monuments en matières satinées
  ou métal. Le décor (pierre, herbe) reste mat : c'est lui qui fait ressortir le reste.
- **Le menu** : un voile doré à gauche sur le plan de l'île qui tourne, « FIEF » avec
  un halo, l'entrée choisie sur une bande d'or.

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
- **Les joueurs se voient** (01/10) : chacun est **un haricot à sa couleur** (façon Fall
  Guys) avec de grands yeux (qui clignent, 02/10), un petit casque d'acier, un cimier et une
  cape ; une petite lanterne ; son **pseudo** à sa couleur au-dessus de la tête (plus de
  halo : il se logeait dans la Couronne) ; ses **ailes dans le dos** (repliées au sol, grandes ouvertes
  en vol, liseré d'or pour les ailes d'or) ; une **bulle** quand il est protégé. Un vrai
  modèle 3D animé peut le remplacer (`docs/MODELES.md`).
- **Les effets** : chaque capacité a sa signature (onde qui gonfle — une **bulle à bord
  lumineux** —, anneaux, gerbes, éclairs, traînées), les coups ont leur impact, le respawn
  sa colonne de lumière, la victoire ses **confettis** et ses feux d'artifice.
- **L'écran, sans texte, en icônes** (30/09 au soir : « je déteste le texte, des icônes,
  comme Fall Guys »), **net au pixel** (01/10 : chaque icône dessinée à sa taille exacte,
  plus de flou) :
  - en haut : le **chrono** dans une pastille bleue (rouge et qui bat les dix dernières
    secondes), une **pastille par manche** (à la couleur de son gagnant, la courante
    bat), la **Couronne** dans une pastille à la couleur de qui la tient (au sommet : la
    tour ; à terre : les secondes avant son retour) ;
  - à droite : une pastille par joueur, à sa couleur, une petite Couronne et ses manches ;
  - à gauche, sur la tour : la **jauge** des six bandes de couleur et ta pastille ;
  - en bas : ta **capacité** dans un gros rond à sa couleur (son icône, sa touche — une
    souris ou une lettre —, la recharge qui descend), ta **passive** à gauche, la
    **poussée** à droite, ton état en petites pastilles (ailes, courant, gel, bouclier…) ;
  - au centre : le point de visée, le **piqué** (la cible d'or), l'invite **E + icône** ;
  - le **fil des événements** : « pastille du joueur → main → Couronne → pastille » ;
  - le **sacre** : la Couronne et une barre à la couleur de qui se fait sacrer ;
  - plus d'astuces écrites : des **astuces en icônes** (v13), une fois par match, sous le
    viseur — la touche, puis ce qu'elle fait (**[E] arbaleste [clic] ↑** sur ta
    plateforme…) ; F1/H : les touches en icônes ;
  - le **verrou** (on vient de te voler la Couronne, tu ne peux pas la reprendre
    pendant 3 s) : une croix rouge sur la Couronne du HUD ;
  - **LE REPÈRE DE LA COURONNE** (02/10 — Martin : « il faut qu'on voie tout le temps où
    est la couronne ») : pour tout le monde, à sa place dans le monde, à travers les murs,
    avec sa distance ; hors de l'écran, collé au bord avec trois points vers elle ; or sur
    son socle, orange à terre, à la couleur du porteur sur une tête (et il bat) ;
  - **QUAND TU LA PORTES** : l'écran se borde d'or tant que tu l'as, la pastille du haut
    grossit avec ton pseudo, les **trois Monuments** ont leur repère (le plus proche plus
    gros) ; à la prise, une fanfare ; à la perte, la Couronne barrée et un éclair rouge ;
  - **Réglages ▸ Aide écrite** (oui par défaut) : quelques mots sous les icônes aux moments
    qui comptent ;
  - **v23, les dix priorités du clipper fou** (`docs/CLIPPER-FOU-500.md`) : chaque obstacle a
    **sa voix** (le butoir fait « boing », le poing du bélier et les barres « paf », le
    maillet « gong », la herse **cliquette** avant de sortir) ; frôler un anneau de vent sans
    y entrer fait « pfff » ; quand un gros coup te projette, **ta tête se tourne** vers d'où il
    vient (une demi-seconde) ; **pousser quelqu'un sous un obstacle** qui le jette dans les
    nuages dans les 8 s, c'est **ton KO** ; le porteur voit une **pastille rouge** sur qui
    fond sur lui en piqué ; au podium, des **médailles d'or** (roi des KO, roi des moments) ;
    **F10 : l'écran propre** (rien par-dessus le monde, pour filmer) ; **une foule** qui fait
    « ooooh » sur les gros moments et exulte à la victoire ;
  - **LES MOMENTS À CLIPPER** (v20, 02/10 — Martin : « rajoute plein de trucs à clipper »,
    `World/Highlights.cs`, `docs/CLIPPER-CHIANT.md`) : le jeu repère douze moments — **KO**
    (poussé dans les nuages dans les 8 s : une colonne de lumière à sa couleur jaillit des
    nuages, un boum de canon), double KO, volée en plein ciel (piqué d'aigle), sacre arraché
    (aux deux tiers), au buzzer (15 dernières secondes), remontada, doublé, revanche, patate
    chaude, porteur abattu, esquive, viré du sommet (au sommet, la poussée envoie **35 % plus
    loin**). Pour qui le fait : son icône au centre, un éclair, la caméra qui encaisse, une
    fanfare ; pour tous : une ligne marquée de la claquette dans le fil ; au classement de
    fin de manche : la claquette et le nombre de moments du joueur. **Pas d'aura** : ni
    ralenti ni « +1000 ». **La poussée fait un énorme BOUM** (son Kenney + grosse caisse
    fabriquée + souffle, `Sfx.BigPush`). **Pas de point de reprise sur la tour** (Martin).
- **Les menus** : gros boutons ronds avec icône, jaunes quand on les vise ; « FIEF » en
  lettres rondes (Titan One) ; l'intro de manche montre la règle en icônes ; **les
  Commandes en trois colonnes de pastilles** (touche → icône) ; salon, pause, fin de
  manche, podium en pastilles (01/10). **Les cartes** : l'icône qui brille dans sa
  fenêtre, le nom sur un bandeau, la phrase sur un cartouche clair, jamais coupée.
- **Le château de conte** (30/09 au soir, assagi le 01/10) : pierre crème, tours rondes à
  **toits en cloche d'ardoise bleu nuit** (toutes, portes comprises), créneaux réguliers
  à chaperon, meurtrières, bandeau, l'or mat ; jardins ronds dans la cour ; quatre
  cascades tombent du bord de l'île dans les nuages ; des rochers flottent autour. Les
  **gargouilles** en formes rondes (corps de lion, ailes de chauve-souris, cornes
  enroulées), sur des consoles.

## Les autres joueurs (des bots, en attendant le jeu en ligne)

Ils jouent **avec tes règles, par les mêmes méthodes** (`World/Rival.cs`) et courent
presque aussi vite que toi (9 / 10,2 / 10,7 m/s selon leur niveau, toi 10,8). Ils
quittent leur plateforme par **leur arbaleste** (visée devant une porte) ou en planant,
passent le **couloir** de la porte (ses chicanes, en contournant les moulinets), montent
la rampe de leur porte (sautent **les balayeurs et les moulinets**,
**changent de côté devant un boulet**), prennent les **arbalestes**, sautent du sommet et
**planent** jusqu'au Monument le plus commode (près d'eux, que personne ne garde), vont
chercher un **courant d'air** quand ils sont trop bas — avec la Couronne, ils sautent
vers le courant d'air du Monument et y tournent pour remonter (30/09), **chassent** le porteur (en vol
aussi, avec le **piqué d'aigle**) en **visant là où il va** (01/10), et l'un d'eux va
**l'attendre au Monument** le plus proche de lui. **Ils se battent en montant** (29/09 :
« faut qu'il y ait du combat ») : qui passe à portée dans la citadelle ou sur la rampe
peut se faire pousser (pas un joueur protégé). **Ils lisent les obstacles** (02/10 : « les bots
n'arrivent pas à monter la tour ») : pendules, béliers, herses et marteaux disent où ils
frapperont (`World/Hazards.cs`) ; le bot **attend son tour** (3,5 s au plus) puis passe,
et **fonce** quand une gargouille a verrouillé son tir. **Sur les rampes, ils ne se
poussent plus entre eux** (ils s'éjectaient de la tour les uns les autres) : ils ne
poussent que toi ou le porteur, et rarement. Coincés plus de 5 s, ils refont leur
chemin ; poussés hors de l'île, ils cherchent un courant d'air. Ils se servent de toutes
leurs capacités (le Souffle, jusqu'à 90 m).

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
- Le 30/09 : **l'aura** (moments, ralenti, phonk, flammes), **les trous** de la rampe,
  le panneau des touches automatique, le mendiant en poncho (remplacé par le petit
  chevalier).
- Le 01/10 : **la victoire au chrono** (tenir la Couronne à la fin ne gagne plus), **le
  maintien de E** (Couronne, sanctuaires), l'écharpe (le haricot entier est à sa couleur),
  les dernières phrases des menus.

## Phases

- **Phase 1 (ici)** : le match complet contre des bots, avec le salon.
- **Phase 3** : le jeu en ligne (voir `docs/RESEAU.md` : les bots cèdent leur place à
  des joueurs, rien d'autre ne change).
