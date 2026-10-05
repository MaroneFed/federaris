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
  ne lance pas de capacité offensive (sauf avec le passif Porteur) : Crochet, Onde, Souffle,
  Givre, Météore, Tornade, Trou noir, Foudre, Boulet de canon ; ni la Fusée (« trop lourd »).
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
  la citadelle tient toujours (pas de raccourci vers la tour), et **le vol libre s'arrête à
  la muraille** : entrer en volant par une porte, c'est se poser (05/10, v25.1).
- **Dans la citadelle, on ne remonte jamais en volant** (05/10 : on remontait la tour à côté
  des obstacles) : on y plane en descendant, la tour se monte à pied. Seul le souffle d'un
  anneau de vent soulève encore, une seconde.
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
| **Clic droit** | **POUSSER** (3,2 m, **à la vitesse où tu cliques** depuis le 12/10 — « pousser à l'infini ») : **le poussé part en cloche à une quinzaine de mètres** (01/10), en faisant des saltos — pousser le porteur, c'est lui **voler** la Couronne ; **en l'air, sur le porteur : le piqué d'aigle** |
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

## Le Mode Dieu

> Martin, le 08/10 : « une version God Mode de la map : un bouton où tu cliques et tu n'as que
> des gods capacités, des trucs de malade mental, de vraiment, vraiment malade mental ».

- **Le bouton** : « **Mode Dieu** » sur l'écran-titre, juste sous Jouer (une fanfare) ; il ouvre
  le salon en Mode Dieu (le titre dit « MODE DIEU », en or qui bat). Dans le salon — et dans le
  salon en ligne, où l'hôte décide pour tout le monde — la ligne **Mode : Normal / DIEU**.
- **La table divine** : on ne pioche que les **quarante-six divines** (trente-cinq actives, onze passives) et les plus folles des autres
  (Météore, Tornade, Trou noir, Boulet, Géant, Foudre, Prison, Bombe, Ballon, Séisme, Gant,
  Déluge, Missile, Raz-de-marée, Lasso, Geyser, Hypnose, Souffle, Apesanteur ; passives :
  Kamikaze, Riposte, Miroir, Rage, Vampire, Planeur, Poigne, Tête dure). Les sanctuaires
  donnent aussi des divines.
- **Tout se recharge en 60 % du temps** (45 % jusqu'au 12/10), **jamais en moins de 4 s**.
- **Le porteur est intouchable par les pouvoirs** (10/10, v32 — Martin : « en God Mode, il faut
  pas taper la couronne, c'est trop cheaté ; il faut pas téléporter ») : les coups et les sorts des
  autres glissent sur lui dans une gerbe d'or. La Couronne se vole **à la main** : la poussée (clic
  droit) et le piqué d'aigle. Les pièges de la tour, eux, le touchent toujours. **La Téléportation
  et le Chaos ne sont plus tirés** en Mode Dieu.
- **Les règles ne bougent pas** : le sceau, la tour qui se monte à pied, la Couronne lourde.
- **Pas de raccourci sur la tour, pas de Couronne à distance** (11/10, v35 — Martin : « la
  couronne se TP sur un gars, il finit le truc en 10 secondes ; il ne faut pas des capacités où on
  peut se TP en haut de la tour ou prendre la couronne direct ») : la Couronne ne se prend plus
  qu'**à deux pas** d'elle (2,6 m de son socle, la portée « en passant » à terre), pour toi, les
  bots et les invités en ligne — avant, l'hôte acceptait une prise de n'importe où. **Sur la
  tour**, les capacités qui font monter sans marcher sont refusées (rendues) : Bond, Ressort,
  Pogo, Grappin, Catapulte, Échange, Taupe, Fusée, Météore, Geyser, Frappe du ciel, Téléportation.
  Partout, le Clignement, la Taupe et le Grappin refusent de te poser **plus haut sur la tour** ;
  l'Échange refuse le porteur et quiconque est sur la tour.

### En ligne, tout le monde voit tout (v37)

> Martin, le 13/10 : « quand on lance une capacité, on est le seul à la voir, les autres ne voient
> pas la capacité qu'on lance, ça n'a aucun sens ».

Chaque capacité lancée part chez les autres joueurs (par l'hôte), qui la **rejouent** : les effets,
les projectiles, les sons, l'annonce en haut de l'écran. Les **coups** ne comptent qu'une fois, sur
la machine du lanceur, qui les envoie comme avant.

**Le tonnerre et la boule de feu** (v37 — « on est en mode god, pas en mode bébé ; des sons d'éclairs
incroyables ») : chaque éclair tombe de 120 m avec ses branches, rampe au sol, blanchit l'écran tout
près, et **tonne** (un craquement sec, puis un grondement qui roule trois secondes) ; chaque grosse
explosion a sa **boule de feu** qui gonfle et retombe.

### Le ciel de feu (v30)

> Martin, le 08/10 : « quand on sélectionne le mode Dieu, tu as tout l'écran qui se met dans un
> autre truc : des flammes dans le ciel, des trucs de fou ; quand tu appuies sur la petite
> pastille qui change, c'est un truc de malade mental ».

`World/GodSky.cs`. Au moment où l'on passe en Mode Dieu (le bouton du titre, la pastille
**Mode** du salon) :

- **le BOUM** : tout l'écran blanchit puis vire à l'orange, une colonne de feu jaillit de la
  tour, une onde, un grondement, l'écran tremble ;
- **le ciel vire au rouge sang et à l'or**, la brume devient braise, le soleil grossit et
  rougit, **la mer de nuages prend feu** ;
- **une pluie de météores** traverse le ciel, **une couronne de feu** de 140 m tourne au-dessus
  de la tour, **des flammes** montent de l'horizon, **des braises** volent autour de toi,
  **des éclairs** tombent au loin ;
- dans les menus (salon, choix des cartes), **les bords de l'écran rougeoient** et des
  flammes montent du bas.

Le ciel reste en feu **tout le match** en Mode Dieu (mais rien de plus à l'écran pendant la
manche : l'écran reste épuré). Repasser en Normal : tout revient doucement.

**Six ciels qui se succèdent** (11/10, v35 — Martin : « pas que du rouge, ça fait enfer, j'aime
pas ; fais que ça change de couleur ») : **le feu** (rouge et or), **l'aurore** (violet et rose),
**le cosmos** (bleu nuit et cyan), **l'émeraude** (vert et turquoise), **la rose** (rose et pêche),
**l'or** (or pâle et blanc). Chacun tient 22 s pendant le match (9 s dans les menus), puis fond
en 5 s dans le suivant ; météores, couronne, flammes, braises, éclairs et bords des menus prennent
la couleur du moment.

**Lisible et bruyant** (v35 — « on ne comprend rien aux effets, des taches noires en plein milieu
de la map » ; « il n'y a pas de son, mets-en à fond ») :
- **Plus aucune tache noire** : les traces brûlées (des disques sombres) et la fumée (des boules
  grises) sortaient noires et opaques ; ce sont maintenant **un cercle de braises** qui s'éteint
  et **de la poussière claire** en particules. L'ombre de la Lune et le cœur du Trou noir géant
  ne sont plus noirs non plus.
- **La cible au sol** : un cercle de lumière à la taille du coup, un second qui grandit jusqu'à
  l'impact, une croix et **un rayon qui monte du centre** — on voit de loin où ça va tomber.
- **Les sons** : chaque capacité a **sa voix au lancement** (huit « ziou » différents ; une divine
  y ajoute un grondement, un accord de cristal et un boum), **un sifflement qui descend** jusqu'à
  l'impact, **un BOUM à la taille du coup** qui porte loin, **un crépitement** sous les éclairs,
  **un « dzing »** à chaque sort, **un grondement** pour le Séisme, le Volcan, le Tsunami et le
  Cataclysme (`Sfx.Cast`, `Incoming`, `Blast`, `ZapAt`, `SpellAt`, `RumbleAt`).

**Les actives divines** (`World/Dieu.cs` et les autres effets réutilisés) :

| Capacité | Ce que ça fait | Recharge (avant les 45 %) |
|---|---|---|
| **Apocalypse** | Vingt-cinq météores s'abattent tout autour de toi (28 m), jamais sur toi. | 20 s |
| **Le temps s'arrête** | Tous les autres, à 70 m, sont figés dans la glace 3,5 s (un coup les libère : pousse-les dans le vide). | 22 s |
| **Rayon divin** | Trois secondes, un laser d'or de 90 m part de tes yeux là où tu regardes ; tout ce qu'il touche est projeté. | 16 s |
| **Téléportation** | Tu te téléportes là où tu vises (150 m). Jamais dans la citadelle ni sur la tour, jamais avec la Couronne. | 12 s |
| **Tempête** | Six tornades partent de toi dans toutes les directions. | 16 s |
| **Bombe atomique** | Posée à tes pieds : 2,5 s de bips… puis tout ce qui est à 30 m s'envole (toi non). | 24 s |
| **Main de Dieu** | Vise un joueur (60 m) : une ombre, puis une main géante tombe du ciel et l'envoie vers le vide. | 18 s |
| **Essaim de missiles** | Huit missiles en éventail, qui poursuivent tout le monde. | 16 s |
| **Gravité zéro** | Tout le monde à 40 m s'envole comme des ballons (4 s), puis éclate. | 18 s |
| **Invincible** | Six secondes intouchable, et géant. Jamais avec la Couronne. | 22 s |
| **Bombardement** (v30) | Un tapis de bombes s'abat devant toi, sur quarante mètres. | 16 s |
| **Singularité** (v30) | Un trou noir géant, là où tu vises (60 m), aspire tout à 45 m puis explose. | 20 s |
| **Souffle du dragon** (v30) | Trois secondes, tu craches un cône de feu qui projette. | 14 s |
| **Comète** (v30) | Une comète géante tombe du ciel là où tu vises (90 m) et souffle tout. | 16 s |
| **Cataclysme** (v30) | Tous ceux qui sont au sol à 100 m sont projetés dans le ciel. | 22 s |
| **Chaos** (v30) | Tout le monde dehors échange de place au hasard. **Jamais le porteur, jamais dans la citadelle** : la Couronne ne se téléporte pas. | 20 s |
| **Anneau de feu** (v30) | Un cercle de feu autour de toi : qui le traverse est éjecté. | 16 s |
| **Geôle** (v30) | Tous ceux qui sont à 50 m sont mis en prison 4 s (un coup les libère). | 22 s |
| **Tsunami** (v30) | Une vague géante emporte tout jusqu'à 60 m. | 18 s |
| **Foudre en chaîne** (v30) | Vise un joueur : la foudre le frappe, puis saute sur ses quatre voisins. | 14 s |
| **Armée de haricots** (v31) | Six petits haricots kamikazes (à ta couleur) courent sur les autres et explosent. | 18 s |
| **Volcan** (v31) | Un volcan surgit là où tu vises (60 m) et crache des bombes de lave cinq secondes, à 22 m. | 20 s |
| **Frappe orbitale** (v31) | Vise un joueur (80 m) : une cible au sol, puis un laser du ciel le poursuit quatre secondes (8,5 m/s : qui court bien s'en sort). | 20 s |
| **Rocher géant** (v31) | Un rocher de 7 m roule devant toi à 20 m/s et écrase tout ce qu'il croise. | 16 s |
| **Chute de la lune** (v31) | La lune tombe, lentement, là où tu vises (120 m) — son ombre grandit 3,5 s — puis tout saute à 45 m. | 26 s |
| **Ouragan** (v31) | Six secondes, une tornade géante de 14 m tourne autour de toi et envoie valser qui s'y trouve. | 20 s |
| **Frappe du ciel** (v31) | Tu bondis à une trentaine de mètres et tu t'écrases là où tu regardes (60 m) : onde de 18 m. Jamais depuis ou vers la citadelle, jamais sur la tour, jamais avec la Couronne. | 16 s |
| **Pluie d'enclumes** (v31) | Une ombre suit **chaque** autre joueur, où qu'il soit, 1,2 s… puis une enclume lui tombe dessus. | 24 s |
| **Lilliput** (v31) | Tous les autres à 60 m deviennent minuscules (6 s). | 18 s |
| **Démence** (v31) | Tous les autres à 60 m ont la tête à l'envers (5 s) et de l'encre plein l'écran (4 s). | 20 s |

**Les passives divines** :

| Capacité | Ce que ça fait |
|---|---|
| **Colosse** | Géant toute la manche : on ne te bouge presque plus, ta poussée porte plus loin. |
| **Éclair** | Tu cours une fois et demie plus vite, toute la manche. |
| **Main lourde** | Ta poussée porte à 5,5 m et envoie deux fois et demie plus loin. |
| **Phénix** | À chaque chute dans les nuages, tu renais là où tu touchais le sol (jamais sur la tour), dans une explosion. |
| **Sablier** | Tes capacités reviennent trois fois plus vite (et encore plus en Mode Dieu). |
| **Explosif** (v30) | Chaque joueur que tu pousses explose : ses voisins à 7 m partent aussi. |
| **Corps de lave** (v30) | Ton corps brûle : qui te touche est projeté. |
| **Écho** (v30) | Ta capacité active part deux fois d'affilée (la seconde une demi-seconde après). |
| **Orage** (v31) | Toutes les trois secondes, la foudre tombe sur le plus proche des autres (18 m). |
| **Orbes de feu** (v31) | Trois boules de feu tournent autour de toi ; qui les touche est projeté. |
| **Pas de titan** (v31) | Chaque fois que tu retombes d'au moins 4 m, le sol tremble (onde de 7 m). |

## Les 133 capacités

> **v36 (12/10) — les CLASSIQUES** (Martin : « regarde dans tous les autres jeux ce qui plaît le
> plus comme capa, et mets-les, en version FIEF du château ») — `World/Classiques.cs`, et toutes
> dans la table du Mode Dieu :
>
> | Capacité | D'où elle vient | Ce que ça fait | Recharge |
> |---|---|---|---|
> | **Boulet bleu** | la carapace bleue (Mario Kart) | vole tout seul jusqu'au porteur de la Couronne et explose sur lui | 22 s |
> | **Mouton explosif** | Worms | fonce en sautillant, demi-tour contre un mur, explose au bout de 4 s | 11 s |
> | **Sainte grenade** | Worms | « Alléluia » pendant 1,7 s, puis une explosion de 12 m | 16 s |
> | **Poing du faucon** | le Falcon Punch (Smash) | le poing se charge en feu une demi-seconde, puis le joueur touché part très loin | 9 s |
> | **Gobe-tout** | Kirby | aspire le joueur visé (15 m), puis le recrache très loin | 12 s |
> | **Roue folle** | la roue de Junkrat (Overwatch) | une roue en feu fonce et explose sur le premier touché | 12 s |
> | **Charge du chevalier** | Reinhardt (Overwatch) | on fonce 25 m ; le premier touché est emporté et écrasé au bout | 12 s |
> | **Caisse de TNT** | Minecraft | posée devant toi, elle clignote 3 s, puis explose (10 m) | 12 s |
> | **La bûche** | Clash Royale | un tronc géant roule 40 m et renverse tout le monde | 13 s |
> | **Tonneau de haricots** | le tonneau de Clash Royale | il éclate et 4 petits haricots kamikazes en sortent | 15 s |
> | **Bombe disco** | la Boogie Bomb (Fortnite) | tous ceux à 9 m dansent 3 s sans pouvoir bouger | 16 s |
> | **Force imparable** | Malphite (League of Legends) | un bond de 24 m, et tout le monde autour de l'arrivée décolle | 14 s |
> | **Saut sur la tête** (passive) | Mario | retomber sur un joueur l'écrase 1 s, et tu rebondis très haut | — |
> | **Home run** (passive) | la batte de Smash | toutes les 4 poussées réussies, la suivante envoie 3 fois plus loin | — |
>
> **v36 — les règles des capacités** (Martin : « quand tu balances un missile, ça fait perdre la
> couronne » ; « les temps déconnent complet, toutes les trois secondes, ça n'a aucun sens ») :
> **toute capacité qui touche le porteur lui fait lâcher la Couronne** (sorts compris, en Normal comme
> en Mode Dieu — plus de bouclier divin) ; **jamais moins de 4 s de recharge** (Recharge rapide ×0,75,
> Recharge éclair ×0,5, Mode Dieu ×0,6). **On voit ce qui arrive** : quand un autre lance une grosse
> capacité, un bandeau en haut — son icône, son pseudo, son nom (« GOTAGA  COMÈTE ! ») ; quand tu es
> dans une cible au sol, les bords de l'écran battent à sa couleur, avec son icône et les secondes
> qui restent (`UI/Warnings.cs`).

> **v33 (10/10) — les noms et les phrases des cartes réécrits** (Martin : « des fois, on ne comprend
> rien du tout à la carte »). Les phrases disent « tu fais ça → il arrive ça », avec des chiffres.
> Noms changés : Nuée → **Fumée**, Givre → **Boule de glace**, Voile → **Invisibilité**, Glu →
> **Colle**, Porteur → **Mains libres**, Poigne → **Grosse poussée**, Ancrage → **Poids lourd**, Flair →
> **Œil de lynx**, Ombre → **Discret**, Prise ferme → **Bien accroché**, Recharge → **Recharge
> rapide**, Rebond → **Atterrissage choc**, Apesanteur → **Flottaison**, Increvable → **Résistant**,
> Plume → **Couronne légère**, Éclair → **Vitesse éclair**, Main lourde → **Poussée géante**,
> Sablier → **Recharge éclair**, Singularité → **Trou noir géant**, Geôle → **Prison géante**,
> Lilliput → **Rétrécissement**, Démence → **Folie**, Écho → **Double lancer**. (Les tableaux
> ci-dessous gardent les anciens noms ; le code fait foi : `Match/Abilities.cs`.)

**Un passif et un clic gauche, c'est tout — et ils CHANGENT À CHAQUE MANCHE.** Avant
chaque manche, deux tours de table : une **PASSIVE** d'abord, puis une **ACTIVE** (le
clic gauche). Chaque carte prise **remplace** celle de la manche d'avant. Le vainqueur de
la manche choisit en dernier. **Le choix est large** (05/10, v26 — Martin : « ouvre le choix
des capacités beaucoup, beaucoup plus large ») : **(joueurs + 3) cartes, six au moins** —
neuf à six joueurs, onze à huit — sur **deux rangées** au-delà de six (flèches haut/bas au
clavier).

**Les cartes** (refaites le 28/09) : elles arrivent **face cachée** (un dos de velours,
un losange d'or) et **se retournent** une à une ; la face : une pierre granuleuse, un
lavis à la couleur de la capacité, un **cadre d'or** ouvragé, un ruban (ACTIVE · CLIC
GAUCHE / PASSIVE), le nom, un fleuron, la phrase, la recharge. Celle qu'on vise se
soulève, s'entoure de **rayons qui tournent** et d'étincelles ; la prendre : un éclair,
une gerbe d'étincelles, un **coup de phonk**.

### Actives (le clic gauche)

| Capacité | Ce que ça fait | Recharge |
|---|---|---|
| **Ruée** | Quinze mètres d'un trait (40 m/s) : qui est sur ta route est bousculé. | 6 s |
| **Grappin** | Vise un mur, un rebord, la tour (60 m) : le grappin t'y tire. Jamais la muraille depuis dehors (on entre par une porte). | 7 s |
| **Crochet** | Vise un joueur, jusqu'à 40 m : il est tiré jusqu'à toi. | 10 s |
| **Onde de choc** | Une explosion : tout le monde à onze mètres s'envole. | 8 s |
| **Clignement** | Tu disparais et réapparais vingt mètres plus loin. | 5 s |
| **Bond** | Un saut immense ; le souffle du départ repousse ceux qui sont tout près. | 8 s |
| **Mur** | Un mur de 10 m sur 4,5 m surgit du sol devant toi ; qui est dessus s'envole. Pas en plein vol. | 12 s |
| **Nuée** | Un nuage de fumée de 16 m : les gargouilles et les autres ne voient plus rien. | 14 s |
| **Mine** | Pose une mine au sol : elle envoie en l'air tous ceux qui passent à 4 m. Pas en plein vol. | 9 s |
| **Givre** | Une boule de givre (26 m) : l'éclat bouscule et ralentit, 6,5 m autour. | 8 s |
| **Voile** | Tu deviens invisible pendant sept secondes. | 16 s |
| **Échange** | Vise un joueur, jusqu'à 45 m : vous échangez vos places. Jamais à travers la muraille (l'un dedans, l'autre dehors), jamais avec un protégé. | 14 s |
| **Rappel** | Tu reviens là où tu étais il y a quatre secondes (jamais d'avant un respawn). | 10 s |
| **Souffle** | **Une vague de vent qui traverse toute l'île** (150 m, 60 m/s, de 3 à 18 m de large) et emporte tout le monde, même en vol — plus fort depuis v26. | 9 s |

**Les capacités de malade** (05/10, v26 — Martin : « fais le mec qui veut vraiment des
capacités de malade mental, qu'on pousse un peu plus loin » ; `World/Powers.cs`) :

| Capacité | Ce que ça fait | Recharge |
|---|---|---|
| **Météore** | Tu bondis, puis tu t'écrases comme une météorite (46 m/s) : tout s'envole à dix mètres. | 10 s |
| **Tornade** | Une tornade file devant toi (14 m/s, 3,5 s) et jette vers le ciel tous ceux qu'elle touche. | 12 s |
| **Trou noir** | Un trou noir s'ouvre là où tu vises (24 m au plus), aspire tout le monde à 15 m, puis explose. | 13 s |
| **Boulet de canon** | Tu fonces trente mètres à 48 m/s : qui est sur ta route décolle. | 9 s |
| **Géant** | Géant sept secondes : on ne te bouge presque plus, ta poussée porte 60 % plus loin, qui te frôle est écarté. | 18 s |
| **Fusée** | Tu décolles, puis tu voles librement, comme après une arbaleste. Pas avec la Couronne, pas dans la citadelle : le sceau tient. | 15 s |
| **Trampoline** | Un trampoline à tes pieds (14 s, deux au plus) : boing, 26 m/s vers le ciel — pour tout le monde. | 10 s |
| **Foudre** | Vise un joueur (60 m) : la foudre tombe là où il était, un instant après (0,8 s) ; qui court s'en sort. | 11 s |

**Les capacités de fou** (06/10, v27 — Martin : « une prison où tu restes, ça t'enchaîne au
sol pendant dix secondes, plein de conneries comme ça, des trucs de fou, il m'en faut une
vingtaine » ; `World/Mayhem.cs`). Les **sorts** (prison, tête à l'envers, mini, ballon,
encre, glu) passent tous par `Combat.Afflict` : un protégé n'est pas touché, et en ligne ils
frappent l'ami **chez lui**. C'est la seule exception à « on n'est jamais étourdi plus de
0,7 s » — voulue par Martin :

| Capacité | Ce que ça fait | Recharge |
|---|---|---|
| **Prison** | Vise un joueur (40 m) : une cage l'**enchaîne au sol dix secondes** (il ne bouge plus, ne saute plus, ne vole plus, ne lance rien — il peut encore pousser). **Le premier coup reçu le libère.** Sur le porteur : tout le monde vient la lui voler. | 16 s |
| **Bombe collante** | Vise un joueur : une bombe se colle à lui, sa mèche crépite plus vite… BOUM 2 s après (il part très loin, et ceux collés à lui aussi). | 12 s |
| **Tête à l'envers** | Vise un joueur : ses commandes sont inversées six secondes. | 13 s |
| **Mini** | Vise un joueur : minuscule sept secondes — 25 % plus lent, et poussé deux fois plus loin. | 13 s |
| **Glu** | Une flaque de glu de 7 m devant toi, dix secondes : qui marche dedans est englué (très lent, ne saute plus). | 12 s |
| **Peau de banane** | Trois peaux en éventail derrière toi : qui marche dessus glisse et fait un salto. | 8 s |
| **Ballon** | Vise un joueur : il gonfle, s'envole (3,5 s) en dérivant loin de toi, puis POP — il retombe, assommé. | 14 s |
| **Séisme** | Tu frappes le sol : tous ceux qui sont debout à 25 m décollent. | 12 s |
| **Gant de boxe** | Un gant géant jaillit devant toi (8 m) : le plus gros coup du jeu. | 8 s |
| **Fantôme** | Quatre secondes : plus rien ne te touche, ni coups ni pièges. Pas avec la Couronne (et qui la ramasse fantôme ne garde qu'1,5 s de protection). | 16 s |
| **Taupe** | Tu plonges sous terre et ressors vingt mètres plus loin en éjectant tout ce qui est au-dessus. | 9 s |
| **Déluge** | Sept météores tombent en 2,5 s autour de là où tu vises (une cible au sol les annonce). | 14 s |
| **Toupie** | Quatre secondes : tu tournes, tu cours plus vite, et qui te touche est éjecté. | 13 s |
| **Encre** | Vise un joueur : de l'encre lui couvre l'écran cinq secondes (un bot avance au hasard). | 12 s |

**La deuxième fournée** (07/10, v28 — Martin : « fais encore plus de capas, plus, plus, plus » ;
`World/Folies.cs`) :

| Capacité | Ce que ça fait | Recharge |
|---|---|---|
| **Lasso** | Vise un joueur (40 m) : tu l'attrapes et tu le jettes **là où tu regardes**. | 11 s |
| **Missile** | Un missile part devant puis poursuit le joueur le plus proche ; BOUM au contact (ou au bout de 4 s). | 12 s |
| **Apesanteur** | Tous ceux qui sont à 14 m s'envolent comme des ballons (2,5 s), puis éclatent. | 14 s |
| **Traînée de feu** | Quatre secondes, tu sèmes des flammes derrière toi ; qui marche dedans est projeté. | 12 s |
| **Pogo** | Cinq secondes de bâton sauteur : dès que tu touches le sol, tu rebondis très haut. | 12 s |
| **Geyser** | Vise un joueur (45 m) : un geyser jaillit sous ses pieds 0,6 s après (on le voit bouillonner). | 10 s |
| **Boomerang** | Il part à 20 m et revient : il frappe à l'aller ET au retour. | 8 s |
| **Piège à loup** | Un piège à tes pieds (deux au plus, armé en 1 s) : qui marche dessus est coincé trois secondes. | 10 s |
| **Catapulte** | Tu te lances en cloche loin devant. Pas avec la Couronne. | 9 s |
| **Bataille d'oreillers** | Six oreillers d'affilée, droit où tu regardes : paf, paf, paf. | 9 s |
| **Hypnose** | Vise un joueur : trois secondes, ses pieds marchent tout seuls vers toi. | 14 s |
| **Raz-de-marée** | Une vague part de toi et grandit jusqu'à 22 m : elle emporte tout ce qu'elle traverse. | 12 s |
| **Coup de pied** | Ta prochaine poussée (dans les 5 s) envoie trois fois plus loin. | 7 s |
| **Cri** | Un cri si fort que tous ceux qui sont devant toi, à 12 m, sont sonnés (0,7 s). | 10 s |

Interdites au porteur (« mains prises », sauf Porteur) : toutes, sauf la Glu, la Peau de
banane, la Traînée de feu, le Piège à loup et le Pogo (pour semer ses poursuivants) et le Coup
de pied ; le Fantôme et la Catapulte, jamais (« trop lourd »).

### Passives (toujours là)

| Capacité | Ce que ça fait |
|---|---|
| **Double saut** | Appuie encore sur Espace en l'air : un second saut. |
| **Planeur** | Des ailes d'or pour toujours : tu voles plus vite et plus loin. |
| **Coureur** | Tu vas quinze pour cent plus vite. |
| **Porteur** | Avec la Couronne, tu n'es plus ralenti et tu peux pousser. |
| **Poigne** | Ta poussée envoie deux fois plus loin, et revient plus vite. |
| **Ancrage** | On te pousse deux fois moins loin. |
| **Flair** | Tu vois l'invisible : les mines, les joueurs voilés, à travers la fumée. (02/10 : la Couronne, tout le monde la voit maintenant.) |
| **Ombre** | Les gargouilles mettent deux fois plus de temps à te repérer. |
| **Prise ferme** | Le premier coup ne te fait pas lâcher la Couronne. |
| **Recharge** | Tes capacités reviennent un tiers plus vite. |
| **Rebond** | Retomber de haut fait une onde de choc autour de toi. |
| **Riposte** | Qui te pousse se prend un retour de bâton. |
| **Vampire** | Chaque coup que tu donnes te fait courir 30 % plus vite, trois secondes. |
| **Tête dure** | Les pièges ne t'éjectent plus de la tour : ils te bousculent, c'est tout. |
| **Second souffle** | Tombé dans les nuages ? Tu repars avec des ailes d'or, protégé six secondes. |
| **Plume** | Avec la Couronne, tu voles aussi vite que les autres (13 m/s au lieu de 11, 24 en vol libre au lieu de 16). Elle pèse toujours : tu descends d'autant, et du sommet tu ne vas pas droit aux Monuments. |
| **Bras longs** | Ta poussée porte à 5 m (au lieu de 3,2). |
| **Kangourou** | Tu sautes une fois et demie plus haut. |
| **Kamikaze** | Quand on te pousse, tu exploses : tout le monde à 7 m s'envole (sauf toi). |
| **Ange gardien** | Une fois par manche, tomber dans les nuages te ramène là où tu touchais le sol — jamais sur la tour (pas de point de reprise sur la tour). |
| **Rage** | Chaque coup reçu rend ta prochaine poussée plus forte (+25 % par coup, ×2,5 au plus). |
| **Ninja** | Immobile une seconde au sol (sans la Couronne), tu deviens invisible. |
| **Miroir** | Les sorts qu'on te lance (prison, encre, mini, hypnose…) reviennent à l'envoyeur. |
| **Increvable** | Les sorts qu'on te lance durent deux fois moins longtemps. |
| **Pickpocket** | Quand tu voles la Couronne, tu disparais deux secondes. |
| **Chanceux** | Une fois sur trois, ta capacité active revient tout de suite. |
| **Armure** | Le premier coup qu'un joueur te donne dans la manche ne te fait rien. |
| **Sprinter** | Les vingt premières secondes de la manche, tu cours 35 % plus vite. |

(06/10 : **l'Aimant est retiré** — Martin : « un truc qui TP la couronne vers toi, c'est
n'importe quoi, il faut quand même monter la tour ».)

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
  tiré, en piqué, ailes repliées, ou en l'air depuis plus d'une seconde — 05/10 : on le
  passait en spammant Espace) est renvoyé dehors dans un éclair de runes. On en sort en volant sans
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

> **RETIRÉES le 12/10 (v36)** — Martin : « les gargouilles, tu peux enlever, ça m'énerve
> fortement ». Le code reste (`World/Eye.cs`) ; elles ne sont plus posées
> (`GameBootstrap.BuildInhabitants`). Ce qui suit est l'historique.

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

- **Réglages ▸ Pseudo** : on tape son pseudo (16 lettres) ; il est gardé. Plus simple
  (05/10) : **la pastille de ton pseudo**, en haut à droite de l'écran-titre et du salon —
  un clic, on tape, Entrée.
- **Au-dessus de chaque joueur, son pseudo** — rien d'autre — à sa couleur (en or pour
  le porteur), plus gros de près, lisible jusqu'à 170 m.
- Le score, la fin de manche et le podium disent les pseudos.
- **Les cris** (05/10 — Martin : « imaginons que Gotaga joue : GOTAGA T'A POUSSÉ, avec un
  mot spécial ») : un bandeau en haut, son pseudo dans une pastille à sa couleur —
  « **GOTAGA** t'a envoyé valser ! », « Tu as atomisé **SQUEEZIE** ! », « … t'a envoyé dans
  les nuages ! », « … t'a piqué la Couronne ! » — un mot différent à chaque fois, deux
  bandeaux au plus, 2,6 s (`UI/Shouts.cs`). L'exception assumée au zéro texte.
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
  - **Réglages ▸ Aide écrite** (**non par défaut** depuis le 06/10 — Martin : « il y a trop
    d'infos ; je chope la Couronne, on me dit ramène-la au Monument, alors qu'on le sait très
    bien ; ça doit être hyper intuitif ») : quelques mots sous les icônes aux moments qui
    comptent, pour qui les veut. Et dans tous les cas, **l'écran s'est vidé** : plus de phrase
    sous la pastille du porteur, le fil ne raconte plus que **la Couronne** (prise, volée,
    tombée — plus les chutes ni les dons des autres, trois lignes au plus), les moments à
    clipper ne s'affichent que **les tiens** (ou ceux qu'on te fait), **un seul cri** à la
    fois (une simple poussée ne crie que si rien n'a crié depuis 4 s), chaque astuce **une
    seule fois par partie lancée** ;
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
**l'attendre au Monument** le plus proche de lui. **Quand quelqu'un a la Couronne, ils
fondent TOUS sur lui** (05/10, v26 — Martin : « ils sont censés tous venir me niquer, il
n'y a personne qui vient ») : à pied s'il est à terre ; du haut de la tour, ils sautent ;
sinon ils prennent **n'importe quelle arbaleste** (à 160 m) qui pose près de lui, ou
filent au bord de l'île le plus proche de lui. Un seul garde le Monument, et seulement
quand le porteur en est encore loin. **Ils se battent en montant** (29/09 :
« faut qu'il y ait du combat ») : qui passe à portée dans la citadelle ou sur la rampe
peut se faire pousser (pas un joueur protégé). **Ils lisent les obstacles** (02/10 : « les bots
n'arrivent pas à monter la tour ») : pendules, béliers, herses et marteaux disent où ils
frapperont (`World/Hazards.cs`) ; le bot **attend son tour** (3,5 s au plus) puis passe,
et **fonce** quand une gargouille a verrouillé son tir. **Sur les rampes, ils ne se
poussent plus entre eux** (ils s'éjectaient de la tour les uns les autres) : ils ne
poussent que toi ou le porteur, et rarement. Coincés, ils montent d'un cran (05/10) : un pas de
côté et un saut (2 s), le repère suivant (4,5 s), un nouveau chemin (7,5 s) et, à 12 s,
hors de ta vue, ils sont reposés sur leur chemin ; poussés hors de l'île, ils cherchent un
courant d'air. **Ils réfléchissent avant de lancer un sort** (06/10) : la prison, l'encre,
le ballon, le mini visent le porteur — ou, si la Couronne est à terre, **celui qui va la
prendre avant eux** ; ils deviennent fantômes devant un obstacle qui les fait attendre, le
porteur poursuivi sème glu et bananes derrière lui, et un bot enchaîné ou aveuglé n'est pas
« coincé » (il ne se fait pas replacer). Ils se servent de toutes leurs capacités — mais plus de Souffle lancé de
l'autre bout de l'île (45 m au plus) ni d'Échange à tout bout de champ (05/10 : « des
fois, les bots sont vraiment trop forts »).

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
- Le 12/10 (v36) : **les gargouilles** ; **l'arc, les piliers et l'autel des Monuments** (on s'y
  cognait en poussant : il reste le dallage, le cercle, les pierres levées et deux braseros sans
  collision) ; **le bouclier divin du porteur** (v32) ; **le temps d'attente de la poussée**.

## Phases

- **Phase 1 (ici)** : le match complet contre des bots, avec le salon.
- **Phase 3** : le jeu en ligne (voir `docs/RESEAU.md` : les bots cèdent leur place à
  des joueurs, rien d'autre ne change).
