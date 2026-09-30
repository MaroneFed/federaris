# Le clipper chiant : 100 reproches

*02/10/2026, le soir. Martin : « Transforme-toi en gars qui ne juge que par les clips
TikTok, Instagram… rajoute plein de trucs à clipper, des moments où tu sais très bien
qu'à ce moment-là il y aura un clip. Fais-le exprès, à balle. » Et : « Quand ça pousse,
il faut un énorme bruit. » Et : « Pas de point de reprise. » Passages voisins :
`docs/GAMER-CHIANT-3.md`, `docs/DESIGNER-CHIANT-3.md`.*

Le clipper chiant ne joue pas pour gagner : il joue avec l'enregistrement lancé
et ne garde que les 12 secondes qui font tourner la tête dans le fil. Sa question, à
chaque instant : **« est-ce que quelqu'un qui scrolle, le son coupé à moitié, comprend
en 2 secondes ce qui vient de se passer ? »** Claude ne peut pas lancer Unity : les
chiffres viennent du code, **à confirmer en jeu**.

**La règle qui ne bouge pas : PAS D'AURA** (30/09). Pas de ralenti, pas de « +1000 », pas
de flammes, pas de phonk. Un clip qui claque, c'est un **son**, un **impact**, une
**lisibilité** — pas le jeu qui s'arrête pour s'applaudir.

Légende : **✔** fait dans ce passage (v20) · **→** proposé, avec la piste · **✘** refusé.

---

## I. Le son de la poussée (1-12)

1. **✔ « Une poussée, ça fait *tap*. Ça doit faire BOUM. »** → `Sfx.BigPush` : le son Kenney de poussée, **plus un coup de grosse caisse fabriqué** (70 → 35 Hz en un tiers de seconde), **plus un souffle** d'air. Le grave, c'est ce qui passe dans un téléphone haut-parleur coupé à moitié.
2. **✔ Plus de double son** : le coup ne joue plus *aussi* le petit « punch » générique par-dessus (`Combat.quietHit`). Un seul boum, net.
3. **✔ Le boum est à toi quand c'est toi** : si tu pousses ou si tu es poussé, il est joué en plein (2D) ; sinon il vient de l'endroit du coup (3D), on entend les bagarres au loin.
4. **✔ Le piqué d'aigle fait le même boum** : fondre sur quelqu'un doit sonner aussi gros qu'une poussée.
5. **✔ Un protégé ne fait pas de boum** : la poussée sur quelqu'un de protégé ne claque pas (sinon le son ment).
6. **✔ Le KO a son propre son** : un boum de canon **plus grave et plus long** (55 → 24 Hz, plus d'une seconde) et un fracas (`Sfx.KoBoom`). On doit reconnaître un KO les yeux fermés.
7. **✔ Chaque moment a sa fanfare** (`Resources/Sons/Moment/`, trois jingles Kenney qui **montent**), différente de la Couronne et des cartes.
8. **→ Varier la hauteur du boum** de ±5 % à chaque poussée : dix poussées d'affilée qui sonnent pareil, ça fait machine.
9. **→ Un « whiff »** quand la poussée ne touche personne : raté = drôle, et c'est clippable aussi.
10. **→ Le cri du poussé** : un petit « hoo ! » par personnage (pack Kenney *Voiceover* ou voix Suno), au hasard. Les clips Fall Guys vivent de ces cris.
11. **→ La foule** : une rumeur de public (Kenney *Crowd*) qui monte quand le porteur approche d'un Monument, et explose au sacre.
12. **→ Couper la musique 0,3 s au KO** (un « trou » de son avant le boum) : c'est le truc des monteurs, et c'est le jeu qui le fait. (Pas un ralenti : le son seul.)

## II. Les moments repérés par le jeu (13-32)

13. **✔ Le jeu SAIT quand il y a un clip** (`World/Highlights.cs`). Douze moments repérés, chacun avec son icône, sa couleur, ses mots.
14. **✔ KO** : tu pousses quelqu'un et il tombe dans les nuages dans les 8 s. **Une colonne de lumière à sa couleur jaillit des nuages** (150 m, visible de toute l'île), une onde, un anneau, un éclat. Le KO de Smash, au-dessus des nuages.
15. **✔ DOUBLE KO** : deux KO du même joueur en 12 s.
16. **✔ VOLÉE EN PLEIN CIEL** : la Couronne arrachée par un piqué d'aigle. Le moment le plus clippable du jeu.
17. **✔ SACRE ARRACHÉ** : le porteur perd la Couronne alors que sa barre de sacre était **aux deux tiers**. Le « non non non NON ».
18. **✔ AU BUZZER** : un sacre dans les 15 dernières secondes.
19. **✔ REMONTADA** : une manche gagnée par quelqu'un qui n'en avait aucune, contre un meneur qui en avait deux.
20. **✔ DOUBLÉ** : deux joueurs différents poussés en 2,5 s.
21. **✔ REVANCHE** : tu pousses celui qui venait de te pousser (10 s).
22. **✔ PATATE CHAUDE** : la Couronne change quatre fois de mains en 15 s (une fois toutes les 20 s au plus).
23. **✔ LE PORTEUR ABATTU** : une gargouille touche le porteur.
24. **✔ ESQUIVE** : le porteur sort de la cible d'une gargouille verrouillée, et le feu tombe à côté.
25. **✔ VIRÉ DU SOMMET** : une poussée au sommet de la tour **envoie 35 % plus loin** — le *home run* de Smash, à 100 m du sol.
26. **✔ Pour toi, ça claque** : si c'est ton moment, son icône au milieu de l'écran (et les mots si l'aide écrite est là), un éclair à sa couleur, la caméra qui encaisse, la fanfare, et pour les trois plus gros (KO, double KO, vol en plein ciel) **une micro-pause d'impact d'un dixième de seconde** — rien de plus long.
27. **✔ Pour ta victime aussi** : si c'est toi qui te fais avoir, tu le vois (moins fort) : le clip de la victime, c'est l'autre moitié d'Internet.
28. **✔ Pour tout le monde** : une ligne dans le fil, marquée de la **claquette de cinéma**, avec l'icône du moment et les pseudos.
29. **✔ Deux icônes neuves** dessinées par le code : `ko` (étoile d'impact percée) et `clip` (la claquette).
30. **→ TRIPLE KO**, et le **« dernier debout »** : tous les autres dans les nuages en même temps.
31. **→ LE SAUVETAGE** : attraper la Couronne en l'air avant qu'elle ne tombe hors d'atteinte.
32. **→ L'ARROSEUR ARROSÉ** : quelqu'un qui tombe dans les nuages avec sa propre Mine, son propre Mur, son propre Souffle.

## III. Ce que l'image doit montrer (33-50)

33. **✔ La colonne du KO se voit de partout** : un KO à l'autre bout de l'île, tu le vois quand même — et ton clip de la tour a un KO en arrière-plan.
34. **✔ La caméra encaisse**, elle ne tremble pas : un coup sec (`Kick`), la secousse douce de la v13 reste douce.
35. **✔ Pas de voile en fin de manche** (v16) : la danse se voit en entier.
36. **→ Un mode « sans HUD »** (touche F10 ou Réglages ▸ Écran propre) : les clippeurs le demandent tous. Rien à l'écran sauf le monde.
37. **→ La vue du poussé** : quand tu es éjecté, la caméra se tourne **vers celui qui t'a poussé** pendant la chute (sans ralenti) — ton clip a le coupable dedans.
38. **→ Le « point d'impact »** : une étoile blanche à l'endroit exact du contact, une image, pas plus.
39. **→ Des traînées d'air** derrière les éjectés (comme le vol plané) : on suit la trajectoire à l'œil.
40. **→ Le porteur doit briller dans le plan de tout le monde** : le cadre d'or est pour lui ; pour les autres, un **filet doré** derrière lui quand il vole.
41. **→ Le visage du haricot** : bouche en « O » au KO (déjà à l'éjection), yeux fermés en tombant dans les nuages.
42. **→ La Couronne qui tombe doit tourner et briller** : une Couronne qui tombe, c'est la moitié des clips.
43. **→ Les confettis de la victoire aux couleurs du gagnant**, pas en arc-en-ciel.
44. **→ Le cadrage 9:16** : les clips se regardent en vertical. Rien d'important ne doit vivre dans les bords gauche et droit de l'écran (le HUD le respecte déjà presque : scores à droite, à vérifier).
45. **→ Le chrono en gros dans les 10 dernières secondes** : sans ça, le « AU BUZZER » ne se comprend pas dans un clip.
46. **→ La barre de sacre visible sur le Monument lui-même** (un anneau qui se remplit au sol), pas seulement au HUD : sinon le « SACRE ARRACHÉ » vu d'un autre joueur ne se comprend pas.
47. **→ Le pseudo du porteur en or** au-dessus de sa tête (les autres restent à leur couleur).
48. **→ Un éclair blanc d'une image** quand la Couronne change de mains, pour tout le monde qui la voit.
49. **? Les colonnes de KO et les colonnes des Monuments** ne doivent pas se confondre (bleu des Monuments, couleur du joueur pour les KO) : à vérifier en jeu.
50. **? La micro-pause d'impact** ne doit jamais se sentir comme un ralenti : si Martin la trouve « aura », on l'enlève.

## IV. La tour, usine à clips (51-64)

51. **✘ Un point de reprise à mi-hauteur** : refusé par Martin (« il n'y a pas de point de reprise, il n'y a rien du tout »). La chute depuis le 90e mètre, **c'est** le clip.
52. **✔ Le sommet est une arène** : on y pousse 35 % plus fort, avec son moment à lui.
53. **→ Le « presque »** : tomber de la tour à moins de 5 m du sommet mérite son moment (« À UN MÈTRE ! »).
54. **→ Le pendule en pleine face** : le butoir qui te cueille à la sortie d'un virage, avec un « bonk » à part.
55. **→ Le boulet qui en prend trois** : un boulet qui éjecte plusieurs joueurs d'un coup = moment.
56. **→ La herse au dernier moment** : passer sous une herse qui retombe, un petit « ding » de soulagement.
57. **→ Les éjectés qui se croisent** : deux joueurs éjectés en même temps qui se cognent en l'air.
58. **→ Le sommet vu d'en bas** : une gargouille du sommet qui crache sur le porteur au moment où il saute.
59. **→ Une caméra d'arrivée** : le premier en haut de la tour est annoncé à tous (icône de la tour et son pseudo dans le fil).
60. **→ La course des quatre rampes** : quand deux joueurs arrivent au sommet à moins d'une seconde, « PHOTO FINISH ».
61. **→ Le bélier qui pousse dans le vide** a son propre bruit sourd, différent de la poussée.
62. **→ La rampe à la couleur du coupable** : on reconnaît dans le clip « c'est la rampe verte, celle qui ne pardonne pas ».
63. **→ Pousser quelqu'un sur un obstacle** (vers un pendule, un bélier) et qu'il soit éjecté par l'obstacle : le KO doit t'être compté (aujourd'hui, le dernier coup compte : à vérifier).
64. **→ Les chutes dans la cour** doivent avoir un « plouf » de poussière visible de loin.

## V. Le vol et la Couronne (65-80)

65. **✔ Le vol en plein ciel est un moment** (piqué d'aigle sur le porteur).
66. **✔ Le porteur abattu et l'esquive** des gargouilles sont des moments.
67. **→ Le piqué d'aigle doit siffler** en descendant, de plus en plus aigu : le son annonce le clip avant l'impact.
68. **→ Le vol au ras** : passer sous une arche, entre deux tours, à moins de 2 m du sol à pleine vitesse → « RASE-MOTTES ».
69. **→ L'arbaleste en pleine tête** : se faire tirer par une arbaleste et percuter un autre joueur en vol.
70. **→ Le plongeon dans le courant d'air** : entrer à pleine vitesse dans un courant et remonter en flèche mérite un « whoosh » plus gros.
71. **→ Le vol de la Couronne à l'arbaleste** : arriver par une arbaleste directement sur le porteur.
72. **→ La Couronne qui rebondit** en tombant sur le rebord d'une tour avant de s'arrêter.
73. **→ Le sacre commencé seul** : la barre de sacre, **tout le monde l'entend** (les tics montent déjà) ; il faut un **battement de cœur** en plus aux deux tiers.
74. **→ Le sauveur** : pousser le porteur hors du cercle à la dernière demi-seconde, compté comme « SACRE ARRACHÉ » même sans vol.
75. **→ La Couronne perdue par un protégé** : jamais (déjà la règle), mais le son « refus » doit claquer.
76. **→ Le porteur qui tombe dans les nuages** : la Couronne reste où il touchait le sol — un **fanion d'or** y plante le drapeau (le repère existe, un vrai objet dans le monde serait plus clippable).
77. **→ Le vol le plus long** de la manche, en mètres, affiché au podium.
78. **→ La plus haute vitesse** en km/h (déjà au HUD), gardée comme record de manche.
79. **→ Le porteur qui se fait voler trois fois** dans la même manche : « MALCHANCEUX » au podium.
80. **→ La Couronne tenue le plus longtemps** sans gagner : le « presque roi ».

## VI. La fin de manche et le podium (81-92)

81. **✔ Les clips au podium** : chaque ligne du classement de fin de manche porte la claquette et **le nombre de moments** de ce joueur dans le match.
82. **✔ Le compteur repart à zéro** à chaque match (`Match.Begin`), pas à chaque manche.
83. **✔ AU BUZZER et REMONTADA** se déclenchent au sacre, avant la fête : la fête garde son rôle.
84. **→ Le « moment du match »** : à la fin, le plus gros moment (KO > vol en plein ciel > sacre arraché…) rejoué en icônes, avec les deux pseudos.
85. **→ Les titres** au podium final, en icônes : le plus de KO, le plus de vols, le plus de moments.
86. **→ La danse du gagnant avec les perdants autour** : les autres haricots en cercle, déçus (bras ballants). La victime en arrière-plan = le clip.
87. **→ La danse de la défaite** : le dernier du classement final a sa petite animation (s'assoit).
88. **→ Le premier sacre du match** annoncé par la voix de l'arène (« first blood » version Couronne).
89. **→ La voix de l'arène sur les moments** (« K.O. ! », « DOUBLE ! ») : pack Kenney *Voiceover* si les mots y sont, sinon Suno.
90. **→ La manche nulle (temps écoulé)** doit avoir son image : la Couronne qui tombe seule sur le sol, et « time ».
91. **→ Le départage** doit être mis en scène comme une finale (musique à part, voix « final round »).
92. **? Le compteur de moments au podium** : vérifier qu'il se lit (petit chiffre à côté de la claquette).

## VII. Faire circuler les clips (93-100)

93. **→ Une touche pour sauver les 30 dernières secondes** (F9) : Unity ne sait pas le faire seul ; sous Windows, **Win+Alt+G** (Xbox Game Bar) le fait déjà, Steam aussi (enregistrement de jeu). À écrire dans les Commandes quand on sera sur Steam.
94. **→ Les marqueurs Steam** : Steam permet au jeu de poser des **marqueurs** dans l'enregistrement (`SteamTimeline`) : chaque moment de `Highlights` en poserait un. Phase 5 (vitrine), noté ici parce que tout est déjà repéré.
95. **→ Le nom du jeu dans le coin** des clips (petit logo discret, désactivable) : la pub gratuite.
96. **→ Un mode spectateur** (Phase 3) : suivre la Couronne automatiquement, la caméra que les streamers voudront.
97. **→ La « replay cam » de fin de manche** : impossible sans rejouer la physique — **pas en Phase 1**. Les marqueurs Steam font 90 % du travail.
98. **→ Les pseudos marrants des bots** : un bot qui s'appelle « Patate » qui te vole la Couronne, c'est déjà un clip.
99. **→ Un bouton « clip » dans le fil** : cliquer sur une ligne marquée de la claquette pendant la pause pour la revoir en icônes.
100. **✔ Le jeu est construit pour que le clip se comprenne sans le jeu** : un boum, une couleur, une icône, deux pseudos. Le reste, c'est aux joueurs de le faire.

---

**Résumé (v20)** : la poussée fait un énorme boum (grosse caisse + souffle), le KO un boum
de canon et une colonne de lumière, douze moments repérés et mis en scène, la claquette
dans le fil, le compte des moments au podium, le sommet qui éjecte 35 % plus loin. **Pas
de point de reprise** (Martin). Le reste : à trancher par Martin, les idées hors Phase 1
vont dans `docs/v2-ideas.md`.
