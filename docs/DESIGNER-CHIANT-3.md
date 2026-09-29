# Le designer chiant, troisième passage : 500 reproches

*02/10/2026. Martin : « il faut qu'on voie tout le temps où est la couronne. […] Quand mon
frère a testé hier […] quand il ne la voyait pas, on ne savait jamais où elle était. Et les
icônes, on ne comprenait rien à ce que c'était. […] Et quand il l'a, il ne sait même pas
qu'il l'a. Il faut absolument régler ça. Et tu peux encore refaire l'exercice, cette
fois-ci, du designer chiant, avec 500 points. »*

Passages précédents : `docs/DESIGNER-CHIANT.md` (v12), `docs/DESIGNER-CHIANT-2.md` (v13).

Cette fois, le designer chiant a un **vrai témoin** : le frère de Martin, premier joueur
qui n'a pas fait le jeu. Son verdict tient en trois phrases, et elles ouvrent la liste.
Ensuite le designer chiant a tout relu, fichier par fichier — le HUD, les menus, les
cartes, le château, la tour, l'île, le ciel, les personnages, les effets, les sons — et
n'a gardé que ce qu'il pouvait appuyer sur le code (les chiffres sont ceux du code). Ce
qu'il n'a pas pu vérifier sans voir le jeu tourner est dit « à vérifier ».

Légende : **✔** corrigé dans la v14 · **→** reste à faire (avec la piste), ou goût qui
revient à Martin.

---

## I. « On ne savait jamais où elle était » (la Couronne)

1. **✔ Sans la capacité Flair, rien ne disait où était la Couronne.** Le Flair était le seul à afficher un repère ; les sept autres joueurs devaient la chercher des yeux. → **Un repère pour tout le monde**, à sa place dans le monde, à travers les murs (`Hud.DrawCrownMarker`).
2. **✔ Le repère est une pastille ronde avec la Couronne dedans**, et deux points dessous comme la pointe d'une épingle : on comprend qu'il désigne quelque chose en dessous.
3. **✔ Sa distance, en mètres, au-dessus de la pastille** : « 84m ». On sait si c'est loin.
4. **✔ Hors de l'écran (ou dans ton dos), le repère se colle au bord** et trois points filent vers le côté où elle est : tourne-toi par là.
5. **✔ Sa couleur dit où elle est** : or foncé sur son socle, orange à terre, **à la couleur du porteur** sur une tête.
6. **✔ Portée par un autre, elle bat** (la pastille grossit et rapetisse) : c'est l'alerte, il faut la reprendre.
7. **✔ Tout près, le repère pâlit** (de 16 m à 6 m) : il ne cache plus la Couronne quand on est dessus.
8. **✔ La colonne d'or de la Couronne était trop fine et trop pâle** (1,6 m de large, 90 m de haut, opacité 0,45). → 2,2 m, 120 m, 0,7 : on la voit de toute l'île.
9. **✔ La pastille du haut disait « Couronne + silhouette » quand quelqu'un la portait** : une silhouette, c'est n'importe qui. → **Le pseudo du porteur**, dans une pastille à sa couleur.
10. **✔ Dans les scores (en haut à droite), rien ne disait qui l'avait.** → La ligne du porteur **sort de la colonne**, cerclée d'or, et bat.
11. **✔ Le fil des événements disait « rond rouge → main → Couronne → rond bleu »** : on ne sait pas qui est rouge. → **Les pseudos** dans les pastilles : « Mahaut → main → Couronne → Oswin ».
12. **✔ Le Flair devient inutile** si tout le monde voit la Couronne. → Il change : **« Tu vois l'invisible : les mines, les joueurs voilés, à travers la fumée. »** (les mines de tous, le pseudo des voilés, la visée qui traverse la Nuée).
13. **→ Le repère ne dit pas si la Couronne est « prenable »** (ton verrou de 3 s est sur la pastille du haut, pas sur le repère). Piste : le repère grisé pendant ton verrou.
14. **→ Le repère ne dit pas quand elle rentre au sommet** (le décompte de 20 s est seulement en haut). Piste : un petit arc qui se vide autour du repère, quand elle est à terre.
15. **→ Deux repères peuvent se chevaucher** (la Couronne et un Monument, quand un porteur est au-dessus d'un Monument, vu de loin). Rare ; on verra en jeu.
16. **→ Le repère est dessiné même pendant le compte à rebours du départ.** Voulu (on repère le but avant de partir), mais à regarder.
17. **→ Aucun son ne dit « elle est tombée »** à celui qui ne regarde pas : la cloche sonne quand elle rentre, pas quand elle tombe. Un « clonk » métallique au sol, entendu de loin.
18. **→ Quand la Couronne est prise au sommet, rien ne bouge au pied de la tour.** Une « sirène » (la cloche de la tour qui sonne trois fois) préviendrait toute l'île.
19. **→ Le porteur ne brille pas assez de loin.** Sa colonne d'or le suit, mais son corps est à sa couleur ; une aura dorée (un halo sur le corps) serait plus lisible — attention : Martin a interdit « l'aura » (le mot et l'effet de puissance), c'est à lui de dire si un simple reflet doré est permis.
20. **→ La Couronne portée est à 2,6 m au-dessus de la tête** : de loin, on voit une couronne qui flotte, pas une couronne « portée ». Voulu pour la lisibilité ; un modèle de personnage (Quaternius) permettrait de la poser vraiment sur la tête.
21. **→ Le repère ne s'affiche pas pendant la danse de victoire** (la manche est finie) : correct.
22. **→ Le repère est une icône « couronne » blanche cernée** — la même que partout. Une Couronne en couleur (or, pierres rouges) serait plus précieuse, mais demande un dessin.
23. **→ À l'écran-titre, rien ne présente la Couronne.** Le plan d'ensemble tourne autour de l'île ; il pourrait finir sur elle, au sommet, avec sa colonne.
24. **→ Le premier joueur à la voir de près ne sait pas qu'on la prend en marchant dessus.** L'astuce « rampe » (tour → haut → Couronne) le suggère ; avec l'aide écrite : « MONTE JUSQU'À LA COURONNE ». Il manque le dernier geste : « MARCHE DESSUS ».
25. **→ Les bots ne « montrent » pas la Couronne** (pas de geste, pas de cri). Un bot qui la prend pourrait crier (sa voix existe : `Sfx.Voice`).

## II. « Quand il l'a, il ne sait même pas qu'il l'a »

26. **✔ Quand tu portais la Couronne, tu ne la voyais nulle part** : elle est cachée pour toi (sinon la caméra serait dedans) et la colonne aussi. Il ne restait qu'une petite pastille qui changeait de couleur en haut. → **L'écran se borde d'or**, un vrai dégradé sur les quatre côtés, qui bat doucement, **tant que tu l'as**.
27. **✔ La pastille du haut grossit** (50 → 66 pixels), porte **ton pseudo**, un halo clair autour, et bat.
28. **✔ Au moment où tu la prends** : l'icône qui claque au milieu est plus grosse (120 → 150), cerclée de blanc, **un anneau d'or s'ouvre** autour.
29. **✔ Une fanfare** (le son de la découverte) et **la caméra s'ouvre d'un coup** (le champ de vision s'élargit de 8°) : on le sent dans le corps.
30. **✔ Quand tu la voles** (en poussant le porteur), tu n'avais qu'un éclair doré : pas d'icône, pas de fanfare. → La même chose qu'une prise.
31. **✔ Tu ne savais pas où aller.** → **Les trois Monuments** s'affichent à leur place, en bleu, avec leur distance ; **le plus proche est plus gros** et bat.
32. **✔ Quand on te la prend** (vol, coup, chute dans les nuages, ailes repliées) : **l'or s'éteint**, la Couronne **barrée d'une croix rouge** claque au milieu, un éclair rouge, un son de refus. Avant : rien, le cadre disparaissait sans explication (il n'y avait pas de cadre).
33. **✔ Avec l'aide écrite** : « LA COURONNE EST À TOI ! », « TU AS VOLÉ LA COURONNE ! », « TU AS PERDU LA COURONNE », et sous la pastille, tant que tu l'as : « TU AS LA COURONNE : VA À UN MONUMENT ».
34. **✔ Pendant ton sacre** (aide écrite) : « RESTE DANS LE CERCLE ! ». Pendant celui d'un autre : « Mahaut VA GAGNER : POUSSE-LE ! ».
35. **→ En vue subjective, on ne voit toujours pas la Couronne sur sa tête.** Idée : quand on lève les yeux au ciel, la voir flotter au-dessus (elle est à 0,9 m au-dessus de l'œil). À essayer — elle masquerait peut-être le ciel en vol.
36. **→ Le porteur n'entend rien de spécial** pendant qu'il l'a. Un battement sourd, discret, qui s'accélère quand un poursuivant approche (la musique de tension le fait en partie).
37. **→ Le cadre d'or et les bords rouges « on te chasse »** (`Rival.HuntingPlayer`) se superposent : or + rouge = orange sale. Piste : quand on te chasse, le cadre d'or bat plus vite au lieu de rougir.
38. **→ Le porteur ne sait pas qu'il est lourd** (il plane à 11 m/s et tombe à 8 m/s). L'astuce « porte » le dit une fois ; un petit poids (icône) dans la pastille des ailes le rappellerait.
39. **→ Les Monuments repérés ne disent pas lequel est atteignable** : du sommet, sans courant d'air, aucun n'est à portée (le porteur tombe trop vite). Piste : le repère d'un Monument hors d'atteinte en pointillés.
40. **→ Les courants d'air ne sont pas repérés pour le porteur**, alors que c'est par eux qu'il doit passer. Ils ont leurs colonnes blanches ; un petit repère « courant » pour le porteur seulement, quand il plane ?
41. **→ Les arbalestes de l'île non plus** (l'autre chemin du porteur).
42. **→ Le cadre d'or reste pendant le sacre**, avec la barre en plus : c'est beaucoup d'or. Correct, c'est le moment le plus fort.
43. **→ Le splash « tu l'as » dure 3,2 s** et se pose au tiers haut de l'écran, là où l'on regarde en courant. Il cache un peu le ciel (et le Monument qu'on cherche). Piste : 2,2 s.
44. **→ Le son de la prise est le même que celui d'un sanctuaire** (la découverte). La Couronne mérite son propre son (une cloche + un accord de cuivres).
45. **→ Quand un bot la prend, tu entends la cloche et l'alarme**, mais tu ne sais pas qui : c'est le fil qui le dit (maintenant avec son pseudo).
46. **→ À la fin du sacre, la Couronne se pose sur l'autel** : bien, mais la caméra passe tout de suite à la fête. Un temps (0,5 s) sur la Couronne posée, puis la fête.
47. **→ La victoire au Monument n'a pas de son de foule.** Des vivats synthétiques (bruit filtré, montée) : c'est ce que fait Fall Guys.
48. **→ Pas de « vibration » (manette)** : Phase 5.
49. **→ Le verrou de 3 s, sur la pastille du haut, est une petite croix rouge** : visible, mais petite (62 % de l'icône). Piste : l'icône entière grisée.
50. **→ « Il ne sait pas qu'il l'a » vaut aussi pour les dons des sanctuaires** : le don remplace le clic gauche, une étoile apparaît sur le rond. Avec l'aide écrite : « NOUVEAU POUVOIR : RUÉE » (✔) ; sans elle, l'étoile seule est trop discrète.

## III. « Les icônes, on ne comprenait rien »

51. **✔ Le constat, d'abord** : zéro texte, c'est la règle du 30/09 (« je déteste le texte »), et le premier joueur neuf n'a rien compris. Le designer chiant ne rediscute pas la règle : il ajoute un **réglage**. **Réglages ▸ Aide écrite** (oui par défaut) : quelques mots **sous** les icônes, aux moments qui comptent. Martin la coupe en un clic.
52. **✔ La règle de la première manche** (tour → Couronne → ailes → courant d'air → sacre) : cinq icônes et des flèches. → Avec l'aide : « MONTE LA TOUR », « PRENDS-LA », « PLANE », « COURANT D'AIR », « MONUMENT : 3 S », et « POUSSE CELUI QUI L'A : TU LA LUI VOLES ».
53. **✔ Elle défilait en 7 s** : trop court pour lire cinq icônes et une ligne. → 9 s avec l'aide.
54. **✔ Les astuces en icônes** (v13) : « [E] arbaleste [clic] ↑ » ne dit rien à qui ne sait pas ce qu'est une arbaleste. → Avec l'aide, une ligne dessous : « E : MONTE SUR TON ARBALESTE, CLIC : TIRE ».
55. **✔ La même icône voulait dire deux choses** : « étourdi » était l'icône du Clignement. → Une icône à elle : **trois étoiles qui tournent**.
56. **✔ « Tombé dans les nuages » était l'icône de la Nuée** (la capacité de fumée). → **Une flèche qui plonge** vers une ligne (`chute`).
57. **✔ « Tombé de la tour » et « a lâché la Couronne » utilisaient l'icône du Rebond** (une capacité passive). → La même flèche qui plonge.
58. **→ Le Flair et l'alarme des gargouilles ont la même icône** (un œil). Maintenant que le Flair « voit l'invisible », l'œil lui va ; mais l'alarme devrait avoir la sienne (une gueule de gargouille).
59. **→ L'icône « haut » (une flèche) veut dire trois choses** : sauter (Commandes), monter (astuces), et « vers » (le panneau des touches, quand il n'y a pas de touche). Une flèche vers la droite pour « donne », une flèche vers le haut pour « monte ».
60. **→ L'icône « pousser » (une main) sert à la poussée ET au vol de Couronne** (« main → Couronne »). C'est la même action ; c'est juste.
61. **→ L'icône « joueur » (une silhouette) veut dire « toi », « un joueur », « le porteur », « Joueurs » (salon)**. Dans le HUD, on l'a remplacée par des pseudos quand c'était possible (✔ 9, 11).
62. **→ L'icône « ailes » est en permanence dans les pastilles d'état** quand on a les ailes d'or : personne ne sait que « ailes d'or » ≠ « ailes ». Piste : une pastille dorée seulement, sans l'icône quand ce sont les ailes normales (c'est déjà le cas : elle n'apparaît qu'avec les ailes d'or ou le Planeur).
63. **→ Le nombre à côté des ailes, en vol, est ta vitesse en km/h** : rien ne le dit. Piste : « km/h » en petit (c'est une unité, pas une phrase).
64. **→ La jauge de la tour (à gauche)** : six bandes de couleur, ta pastille, la Couronne en haut. Sans explication, on la prend pour une barre de vie. Piste (aide) : rien — mais y montrer aussi les autres joueurs (petits points à leur couleur) la rendrait évidente.
65. **→ Les pastilles des manches** (une par manche, à la couleur du gagnant) : on ne sait pas que ce sont les manches. Avec l'aide : rien pour l'instant. Piste : « MANCHE 2/5 » écrit dans le chrono.
66. **→ Le chrono dit 4:12 sans dire « temps restant »** : on le comprend. OK.
67. **→ La poussée (à droite des capacités) est une main dans un rond rouge** : on ne comprend pas que c'est « pousser ». Sa touche est dessus (souris droite) : c'est déjà beaucoup.
68. **→ La passive (à gauche) n'a pas de touche** et rien ne dit qu'elle est « toujours active ». Piste : un petit cadenas ouvert… ou rien : elle est plus petite, plus pâle, c'est déjà le code « passif ».
69. **→ Le nom de ta capacité n'est écrit nulle part pendant la manche** (seulement sur la carte, au choix). Au bout de trois manches, on ne sait plus ce que fait l'icône. Piste (aide écrite) : son nom sous le rond, en petit.
70. **→ Le panneau des touches (F1/H)** : trois colonnes d'icônes. Même critique : avec l'aide écrite, un mot par ligne (« SAUTER », « POUSSER »…).
71. **→ L'écran Commandes du menu** a des titres de colonnes écrits (BOUGER, TES POUVOIRS, VOLER) mais les lignes en icônes : c'est le bon équilibre. À copier dans le panneau F1.
72. **→ Les icônes de l'arbaleste (où l'on va atterrir)** : coche verte = sol, croix rouge = sceau, bleu = Monument, ailes = vide. Il faut l'avoir vu une fois. Avec l'aide : « TU ATTERRIS : SOL / MONUMENT / DANS LE VIDE ».
73. **→ La barre de tension de l'arbaleste** n'a pas d'icône de souris « maintenir » : on ne sait pas qu'il faut tenir. L'astuce le montre une fois.
74. **→ La croix rouge du refus (sous le viseur)** ne dit pas pourquoi (recharge, mains prises, rien à viser). Piste : l'icône de la raison (un sablier, une Couronne, une cible barrée).
75. **→ L'icône « sacre »** (une Couronne sur un autel ?) n'est vue que pendant le sacre, avec la barre : elle se comprend par le contexte.
76. **→ L'icône « don »** (un cristal) : le sanctuaire est un cristal qui flotte ; c'est cohérent.
77. **→ L'icône « courant »** (des spirales qui montent ?) : il faut avoir vu un courant d'air. Cohérent avec le monde (colonnes blanches) si les colonnes ressemblent au dessin.
78. **→ L'icône « monument »** (un petit temple à fronton) : le Monument du jeu est un **arc sur deux colonnes**, pas un temple à fronton. L'icône devrait être l'arc.
79. **→ L'icône « tour »** : une tour crénelée ; la vraie tour a quatre rampes en spirale. Ajouter la spirale au dessin la rendrait unique.
80. **→ Les icônes des 26 capacités** : certaines se ressemblent (Ruée et Coureur : du mouvement ; Onde et Rebond : des cercles). À revoir avec la planche (`Tools/icones`).

## IV. Le HUD, pixel par pixel

81. **✔ Le liseré de l'écran (le cadre d'or) est un vrai dégradé** : alpha en courbe cubique, du bord vers le centre, sur 64 à 82 pixels. Les anciens bords (coups, gargouilles) utilisent une bande qui s'efface **aux deux bouts** (`UiStyle.FadeBand`) : leurs coins sont vides. → Piste : les passer au même dégradé (`EdgeGlow`).
82. **✔ Les pseudos des scores réduisent leur taille pour tenir** (jusqu'à 10 px) au lieu de déborder.
83. **→ La pastille de la Couronne s'élargit avec le pseudo** (jusqu'à 16 lettres) : la ligne du haut « danse » quand le porteur change. Piste : une largeur fixe, le pseudo réduit s'il le faut.
84. **→ Le chrono est bleu roi, la pastille de la Couronne sur son socle est violette** : deux bleus-violets l'un sous l'autre.
85. **→ Les pastilles des manches font 16 px** : petites sur un écran 4K (tout est multiplié par `UiStyle.S`, mais 16 × 2 = 32 px reste petit à 1 m de l'écran).
86. **→ Le viseur est un point de 5 px** (8 px rouge quand un joueur est à portée) : sur un fond clair (le ciel pêche), le blanc se perd. Un liseré sombre existe (0,5 d'opacité) ; 0,7 serait plus sûr.
87. **→ Le piqué d'aigle affiche une cible d'or de 70 px au centre** : elle couvre le porteur qu'on vise. Piste : l'anneau autour, pas une cible pleine.
88. **→ Le fil des événements est à gauche, à 36 % de la hauteur** : là où la jauge de la tour est aussi (à gauche, au milieu). Quand on est sur la tour, les deux se frôlent (x = 84 px pour le fil, 30 à 46 pour la jauge).
89. **→ Le fil garde 5 lignes de 5 s.** En bagarre à huit, il défile trop vite. Regrouper les événements identiques (« ×3 »).
90. **→ Les pastilles d'état (étourdi, gelé, invisible, protégé, ailes, courant, couronne)** sont au-dessus de la capacité, en ligne : jusqu'à 7 pastilles de 42 px. Leur ordre change selon ce qui est actif : elles sautent de place.
91. **→ La pastille « couronne » des états est en double** avec le cadre d'or et la grosse pastille du haut. À retirer ? Elle est à l'endroit où l'on regarde ses capacités : c'est aussi un rappel utile (« mains prises »).
92. **→ La recharge sous une seconde s'écrit `rem.ToString("0.0")`** : selon la langue de Windows, « 0.4 » ou « 0,4 ». Fixer la culture (ou n'afficher que les secondes entières).
93. **→ La touche de la capacité (souris gauche) déborde en bas à gauche du rond**, la touche de la poussée en bas à droite de son rond : pas symétriques.
94. **→ Le rond de la capacité est gris quand elle recharge**, et son icône pâlit : bien. Mais le « halo qui respire » quand elle est prête (0,35 à 0,55) est discret. Un « ding » existe (le bip) ✔.
95. **→ Pas d'indicateur de chute** : quand on tombe de la tour (ailes fermées, éjecté), rien ne dit « tu vas atterrir dans la cour » ou « dans le vide ». Le bord rouge du coup s'efface en 0,8 s.
96. **→ L'éclair blanc des coups** (22 % d'opacité, `hurtFlash`) et l'éclair de couleur (`Flash`, jusqu'à 50 %) peuvent s'additionner : un écran presque blanc au pire moment.
97. **→ La barre du sacre (360 × 26 px) est au quart haut de l'écran** ; le splash « tu l'as » est au tiers haut : si l'on vole la Couronne pendant un sacre, les deux se chevauchent. Rare.
98. **→ L'icône de l'alarme des gargouilles s'affiche à 70 px au-dessus du viseur** : là où le repère de la Couronne peut aussi être. À surveiller.
99. **→ La jauge de la tour apparaît d'un coup** quand on met le pied sur la rampe, disparaît d'un coup quand on en tombe. Un fondu de 0,3 s.
100. **→ Le panneau de diagnostic (F3) est en texte, style « parchemin »** (UiStyle.Small) : c'est un outil, pas le jeu ; il peut rester laid.

## V. L'écran-titre et les menus

101. **→ L'écran-titre : un voile sombre à gauche, le plan de l'île qui tourne à droite, « FIEF » en or, un ruban « LA COURONNE ».** Solide. Mais la Couronne (l'icône) est posée au-dessus du F, pas sur lui : elle flotte sans raison.
102. **→ « FIEF » fait 140 px en Titan One**, sans rien d'autre qu'un liseré sombre : c'est un texte, pas un logo. Un logo dessiné (la Couronne posée sur le I) — le frère de Martin, dans un logiciel de dessin gratuit, en PNG.
103. **→ Le ruban « LA COURONNE » est bleu roi (0,3 ; 0,36 ; 0,9)**, la seule surface de cette couleur à l'écran-titre : elle tire l'œil plus que les boutons.
104. **→ La version (« La Couronne · v14 … ») est écrite en bas à droite dans l'ancien style (UiStyle.Tiny, encre pâle)** : c'est le seul texte de l'écran-titre qui n'est pas en Titan/Lilita. Voulu (discret) ; mais il ne suit pas le reste.
105. **→ Les boutons apparaissent en fondu (`late`)** et ne sont cliquables qu'à 90 % : un clic trop tôt ne fait rien, sans le dire.
106. **→ Le premier bouton est plus gros (70 px) que les autres (56)** : il dit « Jouer d'abord ». Bien.
107. **→ Le plan de l'île tourne à vitesse constante** : à la fin d'un tour, on revoit la même chose. Un mouvement de caméra en deux temps (large, puis la tour) serait plus vivant.
108. **→ Pas de musique d'écran-titre propre** si Martin n'en met pas : la « Title » fabriquée est la même famille que la « Calm ».
109. **→ L'écran-titre ne montre pas un joueur** : c'est un jeu de personnages, et on ne voit que du décor. Un haricot qui danse sur le sommet, à côté de la Couronne ?
110. **→ Les écrans passent de l'un à l'autre sans transition** (sauf l'écran-titre qui s'ouvre sur du noir, `curtain`). Un glissé de 0,2 s.
111. **→ Le voile sombre (`Glide`) prend 55 % de la largeur** : sur un écran ultra-large (21:9), c'est un rideau de 1 300 px. Le limiter à 900 px.
112. **→ Les boutons n'ont pas de son au survol**, seulement au choix (`Pop`). Un « tic » très doux au survol.
113. **→ Au clavier, le bouton choisi est jaune**, à la souris aussi : les deux curseurs peuvent être à deux endroits (la souris sur un bouton, le clavier sur un autre). Le dernier utilisé devrait gagner.
114. **→ La pause (Échap)** : les mêmes boutons que l'écran-titre, sur le jeu figé. Pas de rappel de la manche (qui a la Couronne, le chrono) : on ne sait plus où l'on en était en revenant.
115. **→ « Abandonner » a l'icône du drapeau** : le même drapeau que « départage » en haut de l'écran. Un drapeau blanc serait plus clair.
116. **→ Le bouton « En ligne » mène à un écran « BIENTÔT »** : honnête, mais c'est le deuxième bouton du menu. Le descendre en dernier, grisé.
117. **→ L'écran de fin de match (le podium)** : les pseudos, les manches. On n'y voit pas le champion (le personnage) : un podium à trois marches avec les haricots dessus.
118. **→ La fin de match ne montre aucune statistique** (Couronnes prises, vols, chutes : elles existent, `Stats`). Trois « trophées » en icônes : « le plus de vols », « le plus de chutes », « le plus rapide au sommet ».
119. **→ Le compte à rebours de départ (3-2-1)** : à vérifier s'il a un son qui monte (le dernier chiffre plus aigu), comme dans tous les jeux de course.
120. **→ Le triangle « jouer » qui éclate en or au départ** remplace « PARTEZ » : joli, mais c'est l'icône du bouton « Jouer ». Une icône « drapeau à damier » dirait « c'est parti ».
121. **→ Les menus utilisent deux polices (Titan One pour les titres, Lilita One pour le reste)** : deux polices rondes très proches. Une seule suffirait (Titan pour tout, en deux graisses de taille).
122. **→ Les titres sont « espacés » (`UiStyle.Spaced` : une espace entre chaque lettre)** pour le pseudo du vainqueur : « M A H A U T ». Élégant sur le papier, mais les pseudos avec des espaces deviennent illisibles.
123. **→ Les boutons de menu ont une icône et un mot** : c'est permis (« les mots des boutons de menu »). C'est d'ailleurs ce qui se comprend le mieux du jeu : preuve que les mots aident.
124. **→ Pas de bouton « Crédits »** (les polices OFL, les packs CC0 le demandent poliment, pas légalement) : Phase 5.
125. **→ Pas d'écran de chargement** entre les manches : la scène se recharge (`Reload`), l'image se fige un instant. Un fondu au noir de 0,3 s avant.

## VI. Le salon, les réglages, les commandes

126. **✔ La flèche « moins » des réglages était floue** : c'est la flèche « plus » retournée par `GUI.matrix` autour de son milieu, et ce milieu tombait entre deux pixels. → Posée sur des pixels entiers, largeur paire : nette.
127. **✔ Un réglage de plus : « Aide écrite » (oui/non)**, avec l'icône « commandes ». Gardé dans les préférences (`fief.aide`).
128. **→ Le salon : « Joueurs ‹ 6 › », « Bots ‹ Normaux › », « Manches ‹ 5 › », « Durée max ‹ 6 min › »** : quatre lignes, un mot chacune. Clair.
129. **→ Sous « Joueurs », les pastilles des joueurs à leur couleur, avec leur pseudo** : très bien — c'est là qu'on apprend que « Mahaut est rouge ». On ne le revoit plus ensuite (✔ maintenant, dans les scores).
130. **→ Le « ~25 min » (durée estimée) multiplie manches × durée × 0,7** : une estimation honnête, mais rien ne dit « à peu près ». Le « ~ » le dit.
131. **→ « Commencer » est jaune (principal), « Retour » plus petit** : bien.
132. **→ Le niveau des bots (« Faciles, Normaux, Coriaces ») est un mot** sans icône propre (l'icône « bot »). Trois visages (content, neutre, fâché) ?
133. **→ Les réglages : « Sensibilité 1.0 », « Volume 80 % », « Champ de vision 78° », « Taille du texte 100 % », « Plein écran oui »** : clairs. « Sensibilité 1.0 » ne dit pas ce que 1.0 veut dire ; « 100 % » serait plus parlant.
134. **→ « Taille du texte »** : avec le zéro texte, elle ne change presque plus rien (les pseudos, les boutons). Avec l'aide écrite, elle retrouve un sens.
135. **→ Le pseudo se tape dans une pastille bleue**, sans curseur visible (le curseur IMGUI est fin et blanc). Un curseur qui clignote en or.
136. **→ Pas d'aperçu du champ de vision** quand on le règle (le menu cache le jeu).
137. **→ Pas de réglage de la luminosité**, ni des secousses, ni du roulis de la vue en vol. Les trois sont des réglages d'accessibilité attendus sur Steam.
138. **→ Pas de réglage des sous-titres** (il n'y a pas de dialogues : sans objet).
139. **→ L'écran Commandes : trois colonnes titrées (BOUGER, TES POUVOIRS, VOLER)** : le meilleur écran d'aide du jeu. Il faudrait le montrer **une fois, au premier lancement**, avant le premier match.
140. **→ Les touches y sont en AZERTY (« Z » pour avancer)** : un joueur QWERTY verra « Z » et appuiera sur Z (qui recule, chez lui ?). Lire la disposition du clavier, ou montrer les flèches.
141. **→ « Maj » pour courir** : un mot de trois lettres dans une touche : bien.
142. **→ Les colonnes font 210 px** : à 1280 × 720, les trois colonnes et leurs marges (690 px) tiennent. À vérifier en 800 × 600 (Steam Deck en mode fenêtré : 1280 × 800, ça tient).
143. **→ La touche « E » pour interagir n'est pas réglable** (voir le gamer chiant v12, 19).
144. **→ Pas de manette** : Phase 5.
145. **→ Le panneau F1/H en jeu** et l'écran Commandes ne sont pas le même dessin : deux écrans d'aide qui disent la même chose différemment.

## VII. Le choix des capacités (les cartes)

146. **→ Deux tours de table par manche** (une passive, puis une active) : c'est deux fois plus d'attente quand on n'est pas le premier. Avec 8 joueurs : 16 choix avant de jouer. Les bots choisissent vite ? À mesurer.
147. **→ La carte : un cadre d'or, des rayons, l'icône, le nom, la phrase sur une plaque crème** (v12). Belle. Mais toutes les cartes ont le même cadre : passive ou active, rien ne le dit (sauf le titre en haut de l'écran).
148. **→ La phrase de la carte est la seule vraie explication d'une capacité** — et elle disparaît après le choix. Voir 69 : son nom (et sa phrase ?) sur le HUD avec l'aide écrite.
149. **→ Les cartes sont face cachée puis se retournent** : joli la première fois, long la dixième.
150. **→ La carte visée grossit et se lève** : bien.
151. **→ La carte que tu ne peux pas prendre (tu l'as déjà) est cochée** : bien. Elle devrait aussi pâlir.
152. **→ Les pastilles des joueurs en haut de l'écran de choix** : qui choisit bat. Sous qui a choisi : l'icône de ce qu'il a pris. On apprend ce que les autres ont : bien.
153. **→ « À TOI ! » en jaune qui bat** : un mot, et il est utile. Encore la preuve que les mots aident au bon moment.
154. **→ Le titre de l'écran de choix** (espacé, en or) doit dire la différence passive / active en deux mots : à relire en jeu.
155. **→ Les ronds de « tes capacités », en bas** : ce que tu gardes. Mais comme tout change à chaque manche, « ce que tu gardes » est seulement ce que tu avais à la manche d'avant : c'est ce qui sera remplacé.
156. **→ Aucun aperçu animé de la capacité** (un GIF, une petite scène). Phase 5.
157. **→ Les bots choisissent vite** (à mesurer en jeu) : si l'on ne voit pas ce qu'ils prennent avant que ce soit fait, leur laisser 0,8 s avec la carte qui se soulève.
158. **→ Le vainqueur choisit en dernier** : rien ne le montre (sauf l'ordre). Une petite Couronne sur sa pastille, en haut.
159. **→ Les rayons derrière la carte tournent** : sur 5 ou 9 cartes à la fois, c'est beaucoup de mouvement. Seulement derrière la carte visée.
160. **→ Les couleurs des capacités (`AbilityInfo.Tint`) regroupent 26 capacités en 9 teintes** : Ruée, Clignement et Coureur ont la même couleur. Normal (familles), mais sur le HUD, deux capacités de la même famille se confondent.
161. **→ La touche sur la carte (souris gauche)** est la même sur toutes les cartes actives : elle n'apprend rien. Utile seulement si le joueur a changé sa touche.
162. **→ La recharge sur la carte (« 8 s »)** : bien, c'est un chiffre.
163. **→ Pas de son propre au choix d'une carte** (le Pop commun).
164. **→ Pas de « confirmation »** : un clic et c'est pris. Voulu (rapide).
165. **→ Le choix se fait au clavier aussi** (gauche/droite, Entrée) : bien.
166. **→ La passive « Flair » change de sens (v14)** : sa carte dit maintenant « Tu vois l'invisible ». Son icône (l'œil) reste la même que l'alarme des gargouilles (58).
167. **→ La passive « Porteur »** (« avec la Couronne, tu n'es plus ralenti et tu peux pousser ») est la plus forte pour qui prend la Couronne : les bots la choisissent en 12ᵉ position (`BotChoice`). Question d'équilibre, pas de design.
168. **→ Les noms des capacités sont en français simple** (Ruée, Grappin, Crochet…) : très bien. « Onde de choc » et « Prise ferme » sont les seuls à deux mots.
169. **→ Les phrases utilisent « tu »** partout : cohérent avec le ton.
170. **→ Le fond de l'écran de choix est le jeu flouté ?** Non : un voile sombre sur la scène. Un vrai flou (Phase 5, post-traitement) ferait plus « menu ».

## VIII. La fin de manche, la victoire

171. **→ Le vainqueur de la manche : son pseudo en grand, en or, espacé (88 px)**, qui claque. Dessous : Couronne → sacre → le temps. Bien.
172. **→ Sans vainqueur : chrono + Couronne barrée** : clair.
173. **→ La danse sur la musique (six figures, un pas par temps)** : la meilleure idée des dernières versions. Mais la caméra tourne autour à vitesse fixe : elle ne « coupe » pas sur le rythme. Un changement d'angle toutes les 8 mesures.
174. **→ Les confettis sont des carrés** (`Fx.Confetti`) : des rectangles fins qui tournent (du papier) seraient plus vrais.
175. **→ Les feux d'artifice partent tous du même point** (au-dessus du vainqueur) : les répartir autour, à différentes hauteurs.
176. **→ Le projecteur (spot) est blanc** : un projecteur doré irait avec la Couronne.
177. **→ L'anneau d'or au sol bat sur le rythme** : bien.
178. **→ Les autres joueurs ne réagissent pas** pendant la danse (ils s'arrêtent là où ils sont). Les bots pourraient applaudir (lever les bras) ou bouder.
179. **→ Le voile de l'écran de fin de manche** (en haut et en bas, `FadeBand`) utilise la bande qui s'efface aux deux bouts : ses coins gauche et droit sont transparents, le milieu sombre. Sur un fond clair, on voit une « tache » sombre au milieu. Le dégradé vertical (`EdgeGlow`) serait plus propre.
180. **→ Le score en une ligne (`ScoreLine`)** : une pastille par joueur, pseudo, Couronne, manches. Bien ; celle du vainqueur de la manche pourrait sauter.
181. **→ Le bouton « Choisir une capacité » apparaît à 1,8 s** : on peut le cliquer avant d'avoir vu la danse. Voulu (on n'impose pas d'attendre), mais le joueur pressé rate la fête.
182. **→ Le mot « Le podium » pour le dernier bouton** : clair.
183. **→ La fin de match : « CHAMPION » ?** À vérifier : le titre dit-il le pseudo du champion en grand (72 px, espacé) ? Oui (`DrawEnd`). Et s'il n'y a pas de champion (égalité après 3 départages) : que dit l'écran ? À vérifier.
184. **→ Les ex æquo du départage** sont présentés en pastilles à l'intro : bien. Le mot « DÉPARTAGE » en titre : un mot utile.
185. **→ Pas de ralenti sur le dernier geste** (le sacre qui s'achève) : Martin a banni le ralenti avec l'aura (30/09). Respecté.
186. **→ Le vainqueur ne peut pas bouger pendant sa danse** (la caméra le filme) : correct.
187. **→ La musique de danse fabriquée (120 BPM)** est la même pour toutes les victoires : au bout de 5 manches, on la connaît. Voir `docs/MODELES.md` : mettre 2 ou 3 morceaux.
188. **→ Si le joueur gagne, il se voit danser** (troisième personne pour la fête) : la seule fois où il voit son personnage. Bien — c'est aussi le seul moment où il découvre à quoi il ressemble.
189. **→ Si un bot gagne, on le voit danser** : bien, mais on aimerait voir sa propre réaction (perdre fait partie du plaisir).
190. **→ Le temps de la manche (« 3:42 »)** sous le vainqueur : bien ; le record du match (le plus rapide) mériterait une étoile.

## IX. Le château

191. **✔ Les quatre grandes bannières des portes étaient rouges** : on ne savait jamais par quelle porte on était entré, ni laquelle on voyait du sommet. → **Une couleur par porte** : nord rouge, sud bleu, est or, ouest vert (`Castle.GateColours`), en tissu satiné.
192. **✔ Les fanions des toits tournaient sur leur propre milieu** : ils se décollaient de la hampe à chaque battement. → Une charnière sur la hampe, le drap à côté : ils claquent autour du mât.
193. **✔ Les torches allumaient une vraie lampe chacune** (1,5 d'intensité, 11 m de portée) : en plein jour, on ne les voyait pas — et elles étaient près de trente (16 aux portes, 4 dans la cour, 8 dans les couloirs). → Plus de lampes : la flamme seule, un peu moins vive (2,2 → 1,6).
194. **✔ L'en-tête du code du château parlait de « la sylve », d'une tour de 64 m, des « Yeux », de « tout en cubes »**. → À jour.
195. **→ Les murs sont des pavés lisses de 18 m** : de loin, des boîtes crème. Le « Masonry » (appareil de pierre) les dessine ; de près, c'est un motif ; de loin, rien. Le Castle Kit de Kenney a des murs à relief.
196. **→ Les meurtrières sont des fentes noires collées au mur** (0,06 d'épaisseur) : elles ne creusent pas. Une ombre peinte en plus.
197. **→ Les contreforts (tous les 12 m) sont des pavés sombres** : un peu massifs pour un château de conte. Des contreforts en talus (plus larges en bas) ?
198. **→ 12 % des merlons sont « moussus »** (une autre couleur, au hasard) : sur un château « tenu », c'est une tache. Retirer, ou regrouper par pans (un pan moussu entier).
199. **→ Les toits en cloche sont deux cônes empilés** (la jupe et la flèche) : on voit la marche entre les deux. Un seul maillage courbe (généré) serait plus doux.
200. **→ Les cônes ont 24 côtés** : de près, facettés. 32 pour les grands toits.
201. **→ Les pommeaux d'or au sommet des toits brillent un peu (0,35)** : c'est l'un des derniers ors lumineux. Or mat ?
202. **→ Les fenêtres des tours sont un pavé noir et un disque (l'arc)** : de près, on voit que l'arc est un disque plat collé. Une fenêtre en arc (pavé + demi-cylindre) serait juste.
203. **→ Une fenêtre sur trois luit (0,55)** en plein jour : les fenêtres allumées disent « le soir tombe ». Avec la lumière de fin d'après-midi, c'est cohérent ; au sommet de la tour, c'est plus fort (voir la tour).
204. **→ Les machicoulis sont un anneau lisse et 16 consoles** : bien. Des créneaux au-dessus des tours de garde (non couvertes) ? Toutes les tours ont un toit (v12).
205. **→ Le pont-levis est un tablier de bois de 6,2 m, sept joints et deux chaînes** : les chaînes sont des barres (des pavés fins) — de près, pas des chaînes. Des anneaux (tores) en série seraient trop chers ; une texture de chaîne ?
206. **→ La herse relevée est une rangée de barreaux** sous l'arc : on ne voit pas la grille (pas de traverses). Deux traverses horizontales.
207. **→ L'arc d'or des portes : deux montants et un linteau plats** (0,08 d'épaisseur) : ils brillent comme du métal poli (0,8 de lissé, 1 de métal). « Or mat » : 0,5 de lissé.
208. **→ Les escaliers des remparts : une rampe pleine et des nez de marche peints** (0,06) : on monte une pente, pas des marches. C'est plus agréable à monter ; visuellement, des marches en relief (une sur deux) tromperaient l'œil.
209. **→ Les piles sous les escaliers sont des pavés crème** : de profil, on dirait des dents.
210. **→ La cour : un dallage uni et quatre allées sombres** : les allées guident (bien), le dallage est plat. Des joints (un quadrillage peint) diraient « pavés ».
211. **→ Les quatre jardins de la cour : pelouse, bordure, haie, 26 fleurs (des boules), un arbre (trois boules)** : mignons. Mais « Arbres supprimés » (27/09) : ce sont les seuls arbres du jeu, et ils sont en boules.
212. **→ Les fleurs sont des sphères de 0,3 m satinées** (0,35) : des bonbons. Mat (0,1).
213. **→ Les haies sont des capsules couchées** : bien.
214. **→ La pelouse des jardins est plus verte (0,38 ; 0,66 ; 0,28) que l'herbe de l'île** : deux verts voisins, raccord visible.
215. **→ Pas de sons propres à la cour** (fontaine ? bannières qui claquent ?).
216. **→ Pas de fontaine au centre des jardins** : un point d'eau ferait écho aux cascades.
217. **→ Les bannières des murailles : rouge, bleu, or, dans l'ordre**, avec bande d'or, losange d'or, pointe : bien. Elles ne bougent pas (les fanions, si). Une ondulation lente.
218. **→ Les bannières des murailles sont en or brillant (0,8 de lissé)** : elles aussi, or mat.
219. **→ Les tours d'angle et les tours de garde ont le même dessin** (en plus petit) : les portes ne se distinguent pas des coins. Les tours de garde pourraient avoir des toits plus pointus, ou une autre couleur d'ardoise.
220. **→ Le château n'a pas de donjon ni de bâtiments dans la cour** (la tour de la Couronne en tient lieu). Des toits de maisons le long des murailles (Kenney Fantasy Town Kit) meubleraient la cour.
221. **→ L'ombre du château sur l'île est nette et longue (soleil à 27°)** : c'est beau à l'écran-titre.
222. **→ Le château est symétrique en tout** (quatre portes, quatre jardins, quatre escaliers) : c'est voulu (équité), mais l'œil s'ennuie. L'asymétrie peut venir des couleurs (✔ les bannières), des décors (une charrette ici, un puits là).
223. **→ Les chemins de ronde sont vides** : quelques tonneaux, des râteliers (Kenney) les rendraient vivants — sans collider, pour ne pas gêner.
224. **→ Le parapet côté cour fait 0,8 m** : on peut tomber du rempart dans la cour (voulu : on saute).
225. **→ La plinthe sombre au pied des murs (1,8 m)** : bien, elle ancre le mur au sol.
226. **→ Le bandeau sous le chemin de ronde** : bien.
227. **→ Les murs extérieurs n'ont pas de base évasée** (talus) : les tours, si (soubassement). Harmoniser.
228. **→ Rien ne montre que le château est « scellé » par les airs** avant qu'on s'y cogne. Un dôme de runes à peine visible (des traits d'or qui scintillent quand on s'approche en vol) préviendrait.
229. **→ Les runes du sceau (quand il renvoie)** : un éclair d'or et des anneaux. Bien ; et rien d'autre ne rappelle leur existence.
230. **→ La nuit n'existe plus, les fenêtres allumées restent** : un clin d'œil. OK.

## X. La tour de la Couronne

231. **→ Le fût est le cylindre d'Unity (20 faces)** et fait 26 m de large : de près, les facettes se voient.
232. **→ Les six filets d'or (v13 : or mat)** : bien.
233. **→ Les fenêtres de la tour (v13 : 0,55)** : un tiers luisent. Elles sont posées au hasard en hauteur (`(k * 3.7) % 86`) : certaines tombent derrière une rampe.
234. **→ Les rampes : ~400 dalles** posées une par une, en quinconce de 3 cm (contre le z-fighting). De loin, un escalier ; de près, des marches. Voir le gamer chiant (88) : un seul ruban.
235. **→ Le liseré de couleur au bord des rampes (v13 : 0,8)** : il est le repère principal (« je suis au orange »). Il ne va que sur une dalle sur deux : pointillés.
236. **→ Les consoles sous les rampes** : des pavés tous les quatre pas.
237. **✔ Les lanternes (v13 : rondes) n'allument plus de vraie lampe** (il y en avait une sur deux) : invisibles en plein jour.
238. **→ Les bannières de la tour (v13 : satinées)** : six couleurs, bien. Elles pendent contre le fût : on ne les voit que de la rampe.
239. **→ L'arche d'or au pied de chaque rampe (v13 : or mat)** : bonne entrée. Elle pourrait porter la couleur de la porte d'en face (✔ 191) : « rampe rouge, porte rouge ».
240. **→ Le sommet : des merlons (v13 : clairs, à chaperon), un cercle de runes, une dalle, le socle** : le lieu le plus important du jeu est dépouillé. Quatre statues aux arrivées des rampes, des bannières aux quatre couleurs des portes ?
241. **→ Le socle de la Couronne** : à vérifier de près (v11 : « socle en boule », corrigé). Des marches autour (trois cercles) en feraient un trône.
242. **→ Les obstacles de la tour sont « pierre et runes »** (pendules, béliers) et ceux des couloirs « bois et fer ». Deux familles : OK si c'est voulu.
243. **→ Les bandes ambre peintes sur la rampe** là où frappent pendules et béliers (0,7 de lumière) : le meilleur signal du jeu. Toutes les zones de danger devraient en avoir (balayeurs, herses).
244. **→ Les pendules : une boule de pierre à pointes et une chaîne** ; leur rune passe au rouge. Bien.
245. **→ Les béliers sortent du mur** : leur rune rougit avant. Bien.
246. **→ Les balayeurs** (barre cloutée contre le fût) : ✔ clous ronds (v14). Leur bout était un cube lumineux (v13 : boule).
247. **→ Les herses de rampe** : ✔ au repos, la grille luit à peine (0,45) ; en alerte, 3 ; les pointes sortent.
248. **→ Les boulets (v13 : en volée sur les quatre rampes)** : aucun son qui gronde, aucune annonce (la volée tombe sans prévenir).
249. **→ Les gargouilles de la tour** sont perchées sur des consoles (v12) : bien.
250. **→ De l'intérieur de la cour, la tour est un mur de 100 m** : on ne voit pas le sommet, ni la Couronne (le repère ✔ la montre maintenant).
251. **→ La tour n'a pas d'entrée** (on monte par l'extérieur) : logique, mais une porte murée au pied dirait « on ne passe pas par là ».
252. **→ Les rampes sans parapet** : voulu. Le bord est marqué par le liseré (pointillés, 235).
253. **→ Rien ne distingue les quatre rampes entre elles** (elles sont identiques, décalées de 90°). La couleur de la porte d'en face (239) le ferait.
254. **→ Tomber d'une rampe sur celle du dessous (12,5 m)** : on atterrit sur une rampe d'une autre « couleur de porte » — utile pour se repérer si les rampes en portaient une.
255. **→ La tour vue de loin (depuis un îlot)** : un cylindre crème avec des traits d'or. Il lui manque un sommet reconnaissable (une flèche ? une couronne de pierre ?) : c'est LE repère du jeu.

## XI. Les couloirs piégés et leurs obstacles

256. **✔ Le marteau : montants et traverse en pavés de bois.** → Des **rondins** (cylindres), comme les piliers de l'arche.
257. **→ Les couloirs : deux murets de 3 m, neuf merlons chacun (v13 : à chaperon), une arche à l'entrée (v13 : piliers ronds), une torche au bout de chaque muret** : bien bâtis. Le sol du couloir est l'herbe de l'île : un dallage dirait « chemin ».
258. **→ Les quatre couloirs sont identiques (v13)** : l'arche d'entrée pourrait porter la couleur de sa porte (191).
259. **→ La rune de l'arche (v13 : 1,1)** est orange, comme toutes les runes de danger : l'entrée d'un couloir n'est pas un danger. Or mat, ou la couleur de la porte.
260. **→ Les chicanes : un mur en travers, une rune orange** : le passage (5 m) n'a pas de repère au sol. Une bande claire au sol du côté ouvert.
261. **→ Les moulinets : une barre cloutée à hauteur de genou qui tourne** : ✔ clous ronds, ✔ bouts ronds (v13). Il n'y a pas de bande ambre au sol pour dire « ça passe ici ».
262. **→ Les herses du couloir** : la même que celle de la rampe (✔ 247).
263. **→ Le marteau : ✔ tonneau de fer (v13), ✔ rondins (v14)**. Sa zone de balayage n'est pas marquée au sol.
264. **→ Les obstacles des couloirs laissent une traînée de braise** (`Fx.KeepTrail`) : bien, on voit leur mouvement.
265. **→ Le parvis devant le couloir (où les arbalestes posent)** n'est pas dessiné : c'est de l'herbe. Un dallage rond, à la couleur de la porte, dirait « tu arrives ici ».
266. **→ Les couloirs sont en herbe, la cour en dallage** : passer la porte, c'est passer de l'herbe à la pierre ; le couloir, qui mène au château, devrait déjà être pierre.
267. **→ Les murets n'ont pas de plinthe sombre** (les murailles, si).
268. **→ Les torches du bout des murets (v14 : sans lampe)**.
269. **→ Pas de son d'ambiance dans les couloirs** (les obstacles grincent-ils en tournant ? non).
270. **→ Les couloirs font 30 m** : vus du ciel, quatre bras qui sortent du château, une croix. C'est joli ; c'est aussi la forme d'une cible.
271. **→ Les obstacles projettent « par-dessus les murets, parfois »** : on retombe hors du couloir, dans l'herbe. Un repère (« tu es sorti ») : le couloir reprend à l'entrée.
272. **→ L'arche d'entrée est plus haute (6 m) que les murets (3 m)** : bien, on la voit de loin.
273. **→ Les quatre stations sont espacées de 5,5 m** : ça s'enchaîne sans respiration. Voulu (« plus compliqué »).
274. **→ Les couloirs n'ont pas de gargouille** : la seule protection est dans les pièges. Bien.
275. **→ Rien n'annonce la fin du couloir (la porte)** : le pont-levis le fait.
276. **→ Les couloirs ne sont pas montrés dans la règle de la première manche** (tour, Couronne, ailes, courant, sacre) : le premier obstacle réel du joueur n'y est pas.
277. **→ L'arbaleste de départ pose sur le parvis, juste devant le couloir** : le couloir est donc la première chose qu'on voit en atterrissant. Qu'il soit beau !
278. **→ Les couloirs d'une manche sont tirés au hasard, mais identiques pour les quatre portes (v13)** : la carte change à chaque manche, bien.
279. **→ Les obstacles des couloirs s'accélèrent avec les manches** (`Tower.Hardness`) : rien ne le dit visuellement (des runes plus rouges ?).
280. **→ Les murets ne se sautent pas (3 m) mais rien ne le dit** : on essaie, on rate. Des pointes au sommet diraient « non ».

## XII. L'île, les îlots, les plateformes

281. **✔ Les fanions des plateformes de départ étaient figés et lumineux, sur un poteau carré.** → Une hampe ronde, un fanion satiné qui **claque** autour d'elle.
282. **→ Le dessus de l'île est parfaitement plat** (hauteur 0 partout) : c'est pratique pour la citadelle, mais vu de haut, c'est une table. Des bosses douces loin du château (hors du rayon aplani `Castle.FlatRadius`) ?
283. **→ L'herbe de l'île est en cinq tons tirés d'un bruit (v11)** : les triangles sont à facettes (chaque triangle a ses propres sommets) — c'est le style low-poly, voulu.
284. **→ Pas d'herbes hautes, pas de cailloux, pas de fleurs sur l'île** (sauf dans la cour). Des touffes (des cônes verts, quelques centaines, sans collider) casseraient la plaine.
285. **→ Le bord de l'île est une lèvre de roche nette** : on voit bien où l'on tombe. Bien.
286. **→ Le dessous de l'île : six étages de roche qui s'effilent** (`Ground.BuildRock`) : de près, on voit les étages. Des colonnes de roche qui pendent (des cônes renversés) le rendraient moins régulier.
287. **→ Les îlots sont les mêmes rochers en plus petit** (rayon 11 à 15 m) : on ne les distingue pas. Chacun pourrait avoir son décor (un arbre, une ruine, un rocher) : on dirait « l'îlot à l'arbre ».
288. **→ Les îlots flottent entre 6 et 30 m de haut** : bien, ils ne sont pas tous au même niveau.
289. **→ Les plateformes de départ : un rocher, un cercle à ta couleur (1,2 de lumière), une dalle, un fanion, une colonne, ton arbaleste** : on reconnaît la sienne. Bien.
290. **→ Le cercle de la plateforme luit à 1,2** : un des derniers néons du décor. C'est un repère (ta couleur) ; 0,8 suffirait.
291. **→ Les rivières : une bande d'eau de 4 × 16 m et un bassin** : l'eau ne coule pas (pas de mouvement de texture). Un défilement de l'UV (très simple) la ferait couler.
292. **→ Les cascades tombent en longs voiles de particules** : belles de loin. Au pied de la cascade (dans les nuages), rien : un nuage d'embruns (particules blanches) marquerait la chute.
293. **→ L'eau est une matière bleue lisse (0,92)** : elle reflète le ciel (la sonde de reflets) : bien.
294. **→ Les rochers qui flottent (22, entre 108 et 168 m du centre)** tournent lentement sur eux-mêmes et montent et descendent (`Bobber`) — le designer chiant v13 (76) disait qu'ils ne tournaient pas : il se trompait.
295. **→ Les rochers flottants sont des boules et des cônes** (roche + herbe) : de loin, des champignons à l'envers. Kenney Nature Kit a de vrais rochers.
296. **→ L'île n'a qu'un seul type de roche** (4 tons brun rosé) : les îlots, les plateformes, les rochers flottants, tout est la même pierre. Un îlot plus sombre (ardoise), un plus clair (craie) ?
297. **→ Les sanctuaires de l'île (quatre) sont posés sur l'herbe** : un dallage rond sous chacun (comme le Monument) dirait « lieu ».
298. **→ Les arbalestes de l'île (quatre) : même remarque.**
299. **→ Rien ne relie les lieux de l'île** : pas de chemin des plateformes aux couloirs, des couloirs aux sanctuaires. Des chemins de terre (bandes brunes) guideraient sans un mot (« elles guident sans rien dire », comme les allées de la cour).
300. **→ L'île fait 98 m de rayon, la citadelle 50 + 7 (tours)** : il reste une bande de 40 m d'herbe autour du château. C'est là que se passe la moitié du jeu : elle est vide.
301. **→ La mer de nuages est à −58 et −34 m** : l'île ne « plonge » pas dans les nuages, elle flotte au-dessus. Bien.
302. **→ Tomber dans les nuages : on traverse deux nappes de particules** puis on réapparaît. Pas d'effet de traversée (un voile blanc qui envahit l'écran) : on passe du ciel au respawn sans transition. Un blanc de 0,4 s.
303. **→ Les îlots n'ont pas de bord marqué** (pas de lèvre de roche comme l'île ? ils ont la même `BuildRock`, donc si). OK.
304. **→ Les Monuments se posent sur trois des six îlots** : les trois autres ont un sanctuaire. On ne sait pas de loin lequel est lequel (sauf la colonne bleue du Monument). Bien : la colonne suffit.
305. **→ Les plateformes de départ sont à 42 m de haut** : on voit toute l'île au départ. C'est le plus beau point de vue du jeu — et on n'y reste que 3 s.

## XIII. Le ciel, la lumière, les nuages

306. **→ Le ciel est la « Skybox/Procedural » d'Unity** : bleu, horizon pêche, soleil (disque de 0,045). C'est le ciel de tous les prototypes Unity : on le reconnaît. Un ciel peint (une image panoramique, Kenney/itch.io CC0) serait unique.
307. **→ Pas de nuages dans le ciel** (au-dessus de l'île) : il n'y a que la mer de nuages, en dessous. Quelques gros cumulus lointains (des sphères aplaties, blanches, mates, sans ombre) donneraient l'échelle.
308. **→ Le soleil est fixe (27° d'élévation, 35° d'azimut)** pendant toute la manche. Le faire descendre de 5° sur la manche : les ombres s'allongent, le ciel rosit, la tension monte.
309. **→ La brume est exponentielle au carré, 95 % à 320 m** : les îlots (150-176 m) sont nets, l'horizon se fond. Bien.
310. **→ La couleur de la brume (0,93 ; 0,84 ; 0,80)** est un rose poudré : elle teinte tout le lointain en rose. Avec le ciel bleu, c'est « fin d'après-midi » : cohérent.
311. **→ La lumière ambiante en trois tons (ciel, horizon, sol)** : bien, les ombres ne sont jamais noires.
312. **→ Les ombres : 4 cascades, 160 m, douces, force 0,75** : de belles ombres. À 160 m, les îlots lointains n'ont plus d'ombre : on ne le voit pas (brume).
313. **→ L'anticrénelage × 8 (MSAA)** : lourd sur les petites cartes graphiques. Un réglage « Qualité » (bas / haut) en Phase 5.
314. **→ La synchro verticale est forcée** (`vSyncCount = 1`) : sur un écran 144 Hz, 144 images ; sur 60 Hz, 60. Bien.
315. **→ La sonde de reflets est prise une fois, au lancement** : les objets qui bougent (joueurs, obstacles) n'y sont pas. Normal.
316. **→ La lanterne portée (0,5 d'intensité, 10 m)** : en plein jour, invisible. Règle « la lanterne portée reste, discrète » : respectée… et inutile. La couper ne se verrait pas.
317. **→ Chaque bot a aussi sa lanterne (1,1, 8 m)** : sept lampes de plus. En plein jour, idem.
318. **→ Le Monument a deux lampes bleues (2 d'intensité, 12 m)** : celles-là se voient (le bleu sur la pierre). Bien.
319. **→ Pas de « god rays »** (rayons de soleil dans la brume) : Phase 5, post-traitement.
320. **→ Pas de bloom** (le halo autour des lumières fortes) : les matières lumineuses (2, 3…) sont juste des aplats vifs. Le bloom rendrait les runes et la Couronne magiques ; c'est un paquet d'Unity (Post Processing) à installer — un vrai choix technique, à discuter.
321. **→ Pas de correction des couleurs (LUT)** : même remarque.
322. **→ La couleur du soleil (1 ; 0,9 ; 0,74)** : chaude, bien.
323. **→ L'exposition du ciel est à 1,3** : le ciel est plus lumineux que l'île ; à l'écran-titre, le ciel « brûle » un peu autour du soleil.
324. **→ Le sol du ciel (sous l'horizon) est rose clair (0,93 ; 0,86 ; 0,86)** : sous la mer de nuages, le vide est rose. On ne le voit presque jamais (brume).
325. **→ La mer de nuages est blanche et rose** (deux nappes, 220 et 120 particules géantes, 70 à 130 m) : belle. Elle ne bouge presque pas (0,2 à 0,8 m/s) : de haut, elle est figée. Un peu plus de dérive.

## XIV. Les personnages

326. **✔ Les yeux ne clignaient jamais** : c'est ce qui rend un personnage « mort ». → Toutes les 2,5 à 6 s, un battement de 0,14 s (les yeux, les pupilles et le reflet s'aplatissent).
327. **✔ Chaque bot avait une petite flamme lumineuse (4 de lumière !) qui flottait à 2,45 m au-dessus de sa tête** — et quand il portait la Couronne (à 2,6 m), la flamme se logeait dedans. → Retirée : son pseudo, à sa couleur, dit déjà qui c'est.
328. **✔ La lanterne des bots brûlait à 2,6** : → 1,8.
329. **→ Tous les haricots ont la même silhouette** : seule la couleur change. Sept variantes (un casque à cornes, une plume haute, une cape longue, une couronne de fleurs…) les distingueraient de loin.
330. **→ Le haricot est satiné (0,6 de lissé)** : il brille comme un jouet en plastique. C'est le style Fall Guys ; sous le soleil de fin d'après-midi, un reflet blanc sur chaque haricot — beau.
331. **→ Le casque d'acier est très métallique (0,85/0,85)** : il reflète le ciel (la sonde). Bien.
332. **→ Les joues roses restent roses quand on est étourdi** : des yeux en spirale (ou en croix) pendant l'étourdissement (0,2 à 0,7 s).
333. **→ Pas d'expression** : le visage ne change jamais (joie, peur, colère). Des sourcils (deux petites capsules) qui se froncent quand il pousse.
334. **→ La bouche n'existe pas** : Fall Guys n'en a pas non plus. OK.
335. **→ La cape est une plaque (une sphère aplatie)** qui se soulève avec la vitesse : bien. Elle traverse le corps quand il fait un salto (elle suit la rotation). À vérifier en jeu.
336. **→ Les moufles blanches** : bien, Fall Guys aussi.
337. **→ Les bottes de cuir** : des sphères aplaties. Bien.
338. **→ Le squash & stretch à l'atterrissage** (−16 % de hauteur) : bien.
339. **→ En l'air, les bras s'écartent** : bien.
340. **→ La poussée : le bras droit part devant** (0,36 s) : pas de mouvement du corps (épaule, torsion). Un demi-tour du buste vers l'avant.
341. **→ Pas d'animation de prise de la Couronne** (on passe dessus, elle saute sur la tête). Un bond de joie (les bras en l'air, 0,4 s).
342. **→ Pas d'animation de chute (on tombe de la tour)** autre que les saltos de l'éjection. Quand on tombe sans être éjecté (on marche dans le vide), rien.
343. **→ Le planeur : les bras sont-ils écartés pendant le vol ?** Les ailes sont sur le dos (`WingsOnBack`) : à vérifier que le corps prend une pose de vol (couché, bras tendus).
344. **→ La danse de victoire : six figures** (balancé, disco, fil, cancan, poings au ciel, saut et tour). Bien. Une septième « salut » à la fin.
345. **→ Les bots ne font pas de gestes entre eux** (se moquer, saluer) : Fall Guys vit de ces gestes. Phase 2 (emotes) → `v2-ideas.md`.
346. **→ La couleur du joueur (or) et celle de la Couronne (or)** : quand tu la portes, ton haricot doré sous une Couronne dorée. Pour les autres, tu es « celui qui brille ». Mais les autres joueurs doivent te reconnaître comme joueur ET porteur : le pseudo (en or quand on porte) le fait.
347. **→ Les pseudos au-dessus des têtes passent au travers des murs** (dessinés en surimpression) : c'est ce qui permet de voir les autres de loin — et de savoir qui est derrière le mur. Voulu ?
348. **→ Le modèle 3D (Quaternius) n'est toujours pas là** : tant qu'il manque, tout repose sur le haricot. C'est bien fait ; ce n'est pas un personnage de jeu vendu.
349. **→ Quand le modèle est là, il est teint par le nom de ses matières** (Main, Body, Shirt…) : si le pack les nomme autrement, tout le personnage devient à sa couleur, visage compris. Le dire dans `docs/MODELES.md` (à vérifier avec le vrai fichier).
350. **→ Les bots ont des noms médiévaux (Mahaut, Oswin, Guérin, Aliénor, Tancrède, Isaure, Bohémond)** : charmant, et bien plus lisible que « Bot 3 ». (✔ v13 : si ton pseudo est pris, le bot devient Enguerrand.)

## XV. Gargouilles, Monuments, sanctuaires, arbalestes

351. **→ Les gargouilles (v12 : rondes)** : ambre → orange → rouge, la gueule qui rougeoie, la cible au sol. Leur « iris » monte à 6 de lumière au dernier moment : c'est le signal, il doit être violent. Bien.
352. **→ La cible au sol (un cercle rouge qui se resserre)** : la meilleure lecture du jeu.
353. **→ Le jet de feu est un trait de lumière** (`LineRenderer`, 3 et 6 de lumière) : il manque la fumée qui reste une seconde après.
354. **→ L'explosion : sphère, anneau, gerbe, colonne, éclair** : beaucoup d'effets pour 0,4 s. Bien (« ça doit exploser »).
355. **→ Les gargouilles des tours d'angle sont sur des consoles (v12)** : bien, on les voit.
356. **→ Une gargouille au repos ne bouge pas** : un souffle (la tête qui monte et descend de 2°), les yeux qui balaient la cour.
357. **✔ Le Monument : les runes des pierres levées (2,5) et du linteau (2,2)** étaient les lueurs les plus fortes de l'îlot. → 1,8 (le feu bleu garde 3 : c'est le phare).
358. **→ Le Monument : un arc sur deux colonnes rondes, un autel, un cercle, six pierres levées, deux braseros bleus, une colonne de lumière** : c'est un bon lieu. L'icône « monument » (un temple) ne lui ressemble pas (78).
359. **→ Les runes des pierres levées sont des pavés collés sur des capsules** (déformés par l'échelle de la capsule : 0,8 × 1,4 × 0,5).
360. **→ Le cercle du sacre (3,6 m) est un disque bleu** ; la barre d'or s'étend en disque dedans : bien, on voit le sacre de haut.
361. **→ Le Monument « appelle » quand quelqu'un porte la Couronne** (sa colonne bat) : bien — mais les trois appellent pareil. Celui vers lequel va le porteur pourrait battre plus fort.
362. **→ Pas de son propre au Monument** (un bourdonnement grave, entendu à 30 m).
363. **✔ Le cristal des sanctuaires brillait à 2,8** (le plus fort du décor). → 2.
364. **→ Le sanctuaire (v12 : marbre, pierres rondes)** : un cristal qui flotte au-dessus d'une dalle. On ne sait pas quel don il donne avant de le prendre (au hasard) : voulu.
365. **→ Un sanctuaire vide (déjà pris ?)** : à vérifier s'il se voit « éteint ». Un cristal gris, sans lueur.
366. **→ L'arbaleste : bois, fer, or, une barre de tension** : ✔ embouts en or mat (v13), ✔ barre de tension 2,6 → 1,8 (v14). C'est l'objet le mieux dessiné du jeu.
367. **→ Le repère d'atterrissage de l'arbaleste** (un disque et une colonne, 2,2 et 1,6 de lumière) : il doit se voir de loin : OK.
368. **→ L'arbaleste de départ pivote (±22°) avec la souris** : bien. Rien ne montre qu'elle peut pivoter (une flèche courbe au sol ?).
369. **→ Monter sur l'arbaleste : on y est « posé »** (la vue passe derrière le carreau). Bien.
370. **→ Le tir : un claquement, une traînée** : à vérifier que le départ « claque » assez (un bruit de corde).
371. **→ Les arbalestes des îlots** (une par îlot) : même modèle, bien.
372. **→ Les gargouilles ne sont pas montrées dans la règle de la première manche** : le joueur découvre qu'elles tirent quand il se fait éjecter.
373. **→ Les bots ne craignent pas les gargouilles visuellement** (ils ne se couvrent pas) : c'est de l'IA (gamer chiant 54).
374. **→ Le Monument choisi par le porteur n'est pas connu des autres** : ils voient la colonne du porteur, pas sa destination. Voulu (on devine).
375. **→ Les pierres levées (capsules crème)** penchent au hasard (±4°) : bien, moins figé.
376. **→ Le dallage du Monument (deux disques)** : un joli sol.
377. **→ Les braseros bleus : un cylindre et une boule** : une coupe (comme les torches v12) serait plus jolie.
378. **→ Le sanctuaire et le Monument se ressemblent de loin** (pierre claire, lueur) : la colonne bleue fait la différence. Le sanctuaire pourrait avoir une colonne à la couleur de son don ? (Il n'en a pas.)
379. **→ Les gargouilles « ont peur » de la fumée** (elles ne voient pas au travers) : rien ne le montre. Leurs yeux qui balaient sans trouver.
380. **→ Les gargouilles ne ciblent pas les voilés** : leurs yeux ne réagissent pas. Normal.

## XVI. Les effets

381. **✔ La mine brillait à 2** : → 1,3 (on la voit à 5 m, et toujours avec le Flair).
382. **✔ Le Mur de pierre était gris foncé** (0,34), la seule pierre sombre du jeu. → La pierre claire du château, des merlons à chaperon, une plinthe, des runes en losange (1,2).
383. **→ L'onde de choc (shader `Fief/Onde`, bord lumineux)** : belle de près, invisible de haut. Un disque d'ombre qui s'étale au sol.
384. **→ Toutes les capacités font « éclair + anneau + gerbe »** : une grammaire commune, bien, mais le Clignement (orange) et le Rappel (vert) font le même geste — disparaître, réapparaître. Le Rappel pourrait laisser une traînée fantôme du chemin qu'il remonte.
385. **→ Le Souffle : trois croissants de lumière et un sillage** qui traversent l'île : spectaculaire. De face, les croissants sont des traits fins (0,15 d'opacité) : on le voit mal arriver.
386. **→ Le Grappin : une chaîne lumineuse (2,5)** : bien, on voit qui tire.
387. **→ Le Crochet : même chaîne** : on ne distingue pas « je me tire » de « je le tire ». Un crochet (un cône) au bout.
388. **→ L'Échange : deux colonnes et une chaîne** : bien.
389. **→ Le Givre : une boule lumineuse bleue (2,5), un éclat, un anneau au sol** : bien. Les joueurs ralentis n'ont pas de givre sur eux (seulement leur écran bleuit, pour toi). Un voile bleu sur le haricot.
390. **→ La Nuée : un nuage gris uniforme** (des boules grises) : il cache, mais il ne tourbillonne pas.
391. **→ Le Voile : on disparaît** ; pour les autres, rien (pas même une ondulation). Pour toi, une pastille « voile ». Voulu (invisible), mais rien ne dit « il est là, quelque part ».
392. **→ La Ruée : une traînée, un anneau, une gerbe, de la poussière** : bien.
393. **→ Le Bond : une colonne, un anneau au sol** : bien.
394. **→ Le Mur : il jaillit en 0,25 s avec une gerbe de poussière** : bien. Il redescend en 0,6 s sans poussière.
395. **→ La Mine qui explose : colonne, sphère, anneau, gerbe, éclair** : beaucoup pour une mine. Bien.
396. **→ La poussée : une onde (`Fx.Shock`) et un arrêt sur image de 0,07 s** : bien, « ça porte ». Le poussé part en saltos : bien.
397. **→ Le coup d'un obstacle (`Fx.ObstacleHit`) et le coup d'un joueur (`Fx.Impact`) se ressemblent** : un obstacle devrait faire « bois et fer » (des éclats bruns), un coup « magie » (des étincelles).
398. **→ Le respawn : une colonne de lumière à ta couleur** : bien.
399. **→ La protection (3 s) : une pastille « bouclier »** pour toi ; pour les autres, les coups « glissent » (des étincelles blanches). Pas de bulle visible autour du protégé : on le pousse sans savoir que ça ne marchera pas. Une bulle claire, très légère.
400. **→ Les traînées de braise des obstacles (`Fx.KeepTrail`)** : bien, elles montrent le mouvement.
401. **→ Les poussières qui dérivent autour du joueur** : bien (« l'effet le moins cher et le plus efficace »).
402. **→ Les confettis (carrés), les feux d'artifice, le projecteur** : voir la victoire (174-176).
403. **→ Les éclats (`Fx.Burst`) sont des points ronds additifs** : sur le ciel clair, ils disparaissent (l'additif s'efface sur le blanc). Des éclats « mélangés » (pas additifs) pour les effets de jour.
404. **→ Pas d'effet au sol quand on atterrit de haut** (un nuage de poussière existe pour les bots près de toi ; pour toi ?). À vérifier.
405. **→ Les effets n'ont pas de « pool »** : chacun crée et détruit ses objets. En fin de manche (8 joueurs qui se battent), des saccades possibles.

## XVII. Le vol

406. **✔ Le liseré des ailes brillait à 2** : → 1,4.
407. **→ Les ailes : une toile (des pavés fins), des nervures, un longeron, une quille** : de près, du bois et du tissu, bien. Elles sont plates (pas de courbure).
408. **→ Les ailes d'or et les ailes normales ne diffèrent que par la couleur du liseré** : les ailes d'or devraient être plus grandes, dorées en entier.
409. **→ En vol, la vue se penche (roulis)** : bien, on sent le virage.
410. **→ Le champ de vision s'ouvre avec la vitesse** (`FlightFov`) : bien.
411. **→ Le vent qui siffle en vol** (`FlightWind`) : bien.
412. **→ Les courants d'air : des colonnes blanches et des anneaux qui montent** : bien. De loin, on les confond avec la colonne d'un Monument (bleue) ? Le blanc et le bleu clair (0,45 ; 0,7 ; 1) sont proches.
413. **→ Pas de lignes de vitesse** (des traits blancs au bord de l'écran quand on pique vite) : c'est la sensation de vitesse la plus lisible des jeux d'arcade.
414. **→ Le piqué d'aigle : une cible d'or au centre** (voir 87).
415. **→ Rien ne dit qu'on peut replier les ailes (Espace)** : le panneau F1 le montre (« Espace → croix »).
416. **→ L'ouverture automatique à 6 m au-dessus du vide** : un claquement de toile et un anneau. Bien.
417. **→ La vitesse (en km/h) en chiffres, à côté des pastilles** : utile pour les joueurs qui aiment les chiffres ; les autres ne savent pas ce que c'est (63).
418. **→ Pas d'ombre du joueur au sol quand il vole haut** : l'ombre existe (le corps projette en `ShadowsOnly`), mais à 160 m de distance d'ombre, et floue. On ne voit pas où l'on va atterrir. Un petit disque sombre au sol, sous soi, en vol ?
419. **→ Le vol au-dessus de la mer de nuages** : de haut, on ne voit que du blanc sous soi. Magnifique. Pas de repère d'altitude : on ne sait pas si l'on va tomber dedans.
420. **→ Les bots qui planent** : leurs ailes sont-elles visibles de loin ? (À vérifier : des ailes fines, vues de face, disparaissent.)

## XVIII. Le son

421. **✔ 25 sons de l'ancien jeu étaient encore fabriqués** (hibou, loup, corbeau, pluie, tonnerre, grincement, mage, forge, stèle, malédiction, pièces, pelle…) sans plus jamais être joués. → Retirés : le fichier des sons passe de 1 204 à 577 lignes.
422. **→ Tous les sons sont synthétisés par le code** (aucun fichier) : c'est ingénieux, et ça s'entend. Kenney a des packs de sons CC0 (« Impact Sounds », « Interface Sounds », « RPG Audio ») : ils rendraient le jeu dix fois plus pro.
423. **→ `Sfx.Crash` s'appelle « un tronc qui s'abat » dans le code** et joue des « branches sèches » : on l'entend pour l'Onde, le Souffle, le Mur, les coups d'obstacles, les boulets… Des branches pour de la pierre : incohérent.
424. **→ `Sfx.Rustle` (« fourrager dans les branches »)** joue quand on appuie sur E pour un geste : il n'y a plus de branches.
425. **→ `Sfx.Clang` (le fer contre le fer)** : les balayeurs. Bien.
426. **→ `Sfx.Thud`** (un choc sourd) : les coups. Bien.
427. **→ `Sfx.Whoosh`** (un souffle) : les capacités. Bien, mais **toutes** les capacités font le même whoosh (en plus de leur effet).
428. **→ `Sfx.Bell` (la cloche)** : la Couronne prise, rentrée, le sacre. Trois sens pour un son.
429. **→ `Sfx.Alarm` (trois tintements)** : la Couronne prise au socle, une gargouille qui charge, le sacre qui commence. Trois sens.
430. **→ `Sfx.Discovery` (une fanfare)** : un sanctuaire, la Couronne prise (✔ v14), le sacre réussi. La Couronne mérite la sienne.
431. **→ `Sfx.Deny` (un grognement grave)** : un refus, et maintenant « tu as perdu la Couronne » (✔). À distinguer : la perte mérite un son plus dramatique.
432. **→ `Sfx.Pop`** : les menus (19 appels). Bien.
433. **→ `Sfx.Beep`** : les 10 dernières secondes, une capacité qui revient. Deux sens : OK (l'aigu diffère).
434. **→ Les voix des bots (`Sfx.Voice`)** : une seule utilisation. Ils pourraient crier en prenant la Couronne, en tombant, en poussant.
435. **→ Les pas (`Sfx.Step`) sont les mêmes sur l'herbe, la pierre, le bois du pont-levis** : trois sons de pas.
436. **→ Pas de son d'ambiance de l'île** (le vent en altitude, les cascades, les bannières) : le silence entre deux actions est vide.
437. **→ Les cascades ne font pas de bruit** : un bruit d'eau (bruit blanc filtré) en 3D.
438. **→ Pas de son 3D pour les obstacles** : on n'entend pas un pendule arriver dans son dos. Les sons sont joués « à plat » (source 2D), sauf les voix.
439. **→ Les gargouilles qui chargent font l'alarme (2D)** : on ne sait pas d'où elle vient. En 3D, on tournerait la tête vers elle.
440. **→ Le boulet ne gronde pas** (voir 248).
441. **→ Le volume de chaque son est réglé à l'oreille** (0,22 à 0,9) : sans table de mixage, certains sons couvrent la musique. Un « bus » d'effets et un bus de musique, réglables séparément dans les Réglages.
442. **→ Pas de réglage « volume de la musique » séparé du reste** : attendu sur Steam.
443. **→ Les sons ne varient pas en hauteur** (sauf le bip) : le même coup, dix fois, fatigue. Une variation de ±5 % au hasard.
444. **→ Le son du vol plané (le vent) ne change pas avec la vitesse** ? (À vérifier dans `GlideFeel`.)
445. **→ Aucun son de « ça passe » quand on franchit une porte, un couloir, le sommet** : des petits « jingles » de progression (trois notes) comme dans les jeux de course.

## XIX. La musique

446. **→ Les musiques sont fabriquées par le code** (`MusicSynth` : Title, Calm, Tension, End, Dance) : même critique que les sons (422). Martin peut en mettre dans `Resources/Music/` (le nom dit quand elles jouent).
447. **→ La « Calm » tourne en boucle pendant toute la montée** (4 à 10 min).
448. **→ La « Tension » part quand quelqu'un porte la Couronne** : bien, la musique dit l'état du jeu.
449. **→ La danse à 120 BPM** : une seule. Voir 187.
450. **→ Les changements de musique se font en fondu** : bien.
451. **→ La musique continue en pause** (`ignoreListenerPause`) : voulu.
452. **→ Pas de « stinger »** (un accord bref) quand la Couronne change de main : la musique pourrait « claquer » à ce moment.
453. **→ La musique ne suit pas la hauteur sur la tour** (plus on monte, plus elle pourrait se densifier).
454. **→ La musique de fin (« End »)** : à vérifier qu'elle se distingue de la danse.
455. **→ Le volume de la musique n'est pas réglable seul** (442).

## XX. La cohérence et la dette qui se voit

456. **✔ Le code de la brume rasante, des lucioles, des braises et de la pluie d'orage** (la forêt) était toujours là, jamais appelé. → Retiré ; l'en-tête de l'ambiance dit ce qu'elle fait vraiment.
457. **✔ « Presque rien ne brille »** : cette passe a encore éteint ou adouci les torches (sans lampe), les lanternes de la tour, la mine, le Mur, les ailes, la barre de l'arbaleste, les sanctuaires, les runes du Monument, la lanterne des bots, le halo (retiré), le fanion des plateformes, la grille des herses au repos.
458. **→ Les ors du jeu** : `Castle.GoldTrim` (1 ; 0,8 ; 0,32), `Tower.Gold` (1 ; 0,8 ; 0,4), l'or de la Couronne (1 ; 0,86 ; 0,35), `Wings.Gold` (1 ; 0,82 ; 0,4), `Feed.Gold` (1 ; 0,82 ; 0,4), `Palette.Gold`… **six ors**. Un seul, dans `Palette`.
459. **→ Les pierres** : château (0,82 ; 0,75 ; 0,64), tour (0,80 ; 0,73 ; 0,62), couloirs (0,78 ; 0,71 ; 0,60), Mur (0,8 ; 0,73 ; 0,62, v14) : quatre pierres presque pareilles.
460. **→ Les bleus « Monument »** : `Monument.Blue` (0,45 ; 0,7 ; 1), les courants (0,75 ; 0,92 ; 1), `Wings.Glow` (0,7 ; 0,9 ; 1) : trois bleus clairs pour trois choses différentes.
461. **→ Les rouges « danger »** : herse (1 ; 0,15 ; 0,1), gargouilles, refus (1 ; 0,4 ; 0,35), perte de Couronne (0,85 ; 0,25 ; 0,22) : proches, c'est bien (une seule idée : danger).
462. **→ Les orangés « braise »** des obstacles (1 ; 0,45 ; 0,2) sont écrits à la main une dizaine de fois.
463. **→ Les matières** : `GetGlow` (lumineux), `GetShiny` (satiné/métal), `Get` (mat) — utilisées au cas par cas, sans règle écrite. Une règle : décor = mat, personnages/objets = satiné, signaux = lumineux (≤ 1,5 sauf alerte).
464. **→ Les formes** : cubes (murs), cylindres (tours, piliers), sphères (lanternes, feuillage), capsules (haies, pierres levées), cônes (toits, pointes). Cohérent ; il reste des cubes « pour rien » (consoles, contreforts, meurtrières, joints, chaînes).
465. **→ Les noms dans la hiérarchie Unity** : français accentué (« Chapiteau », « Charnière du fanion »), MAJUSCULES pour les ensembles (« CITADELLE », « BOULETS »). Cohérent. Garder.
466. **→ `UiStyle` garde le style « manuscrit » du 26/09** : `Rule`, `CardFrame`, `DropShadow`, `Bar`, `Pill`, `Chip`, `Icon`, `Frame`, `Shadowed` ne sont plus appelés d'ailleurs (vérifié) ; seules `Fill`, `FadeBand`, `Tinted` et `S()` servent encore. À vider (avec leurs textures).
467. **✔ `Hud.cs` gardait des styles de texte inutilisés** (`BigCentered`, `RightSmall`, `WrappedCentered`, `Sized`), du temps du HUD écrit. → Retirés.
468. **→ `Palette.cs` parle encore de « la forêt sombre »** dans ses commentaires.
469. **✔ `CharacterRig` exposait `StaffBone`** (le bâton) : il n'y a plus de bâton. → Retiré.
470. **→ `Menus.cs` fait 1 500 lignes** : un fichier par écran (titre, salon, choix, fin).
471. **→ `Rival.cs` fait 1 250 lignes** : le cerveau d'un côté, le corps de l'autre.
472. **→ `Tower.cs` et `Course.cs` définissent chacun leurs obstacles** (pendule, bélier, boulet / balayeur, herse, marteau) : un fichier `Obstacles.cs`.
473. **→ Les couleurs des joueurs (`Match.Colours`)** : or, rouge, bleu, violet, turquoise, orange, rose, blanc. Le rouge (0,88 ; 0,35 ; 0,28) et l'orange (1 ; 0,55 ; 0,18) se distinguent mal de loin ; le blanc se confond avec le crème du château.
474. **→ Les couleurs des portes (✔ v14 : rouge, bleu, or, vert)** recoupent celles des joueurs (rouge, bleu, or) : « la porte rouge » n'est pas « la porte de Mahaut (rouge) ». Choisir des tons de porte qui ne sont pas des tons de joueur (bordeaux, marine, ocre, sapin).
475. **→ Le jeu n'a pas de « charte » écrite** (couleurs, formes, matières, sons) : une page `docs/CHARTE.md` éviterait de recoller six ors.
476. **→ Les commentaires sont en français sans accents** (convention du code) et les textes affichés avec accents : cohérent.
477. **→ `Seeker` porte `HasWings` (ailes d'or) et la passive `Planeur` (ailes d'or pour toujours)** : deux chemins pour la même chose visuelle.
478. **→ `Icons.cs` fabrique ses textures au vol (4 par image)** : première ouverture d'un écran = quelques images « prêtées » (flou d'une image). Préchauffer au chargement de la manche.
479. **→ Les 66 icônes (`IconArt`) sont toutes blanches cernées de noir**, teintes au besoin : bien, un seul style.
480. **→ Les planches d'aperçu des icônes (`Tools/icones`)** ne sont pas versionnées : le frère de Martin ne peut pas les voir sans .NET. Mettre la planche dans `docs/`.

## XXI. Ce qui coûte à l'écran (lu, pas mesuré)

481. **✔ Une trentaine de lampes inutiles** (torches, lanternes de la tour) : retirées.
482. **→ Il reste des lampes « ponctuelles »** : la lanterne du joueur, celle de chaque bot (7), les deux lampes bleues de chaque Monument (6) : 14 lampes. En « forward », au-delà de 4 lampes par objet, Unity les ignore ou les passe en qualité basse.
483. **→ Les rampes : ~400 objets de dalles**, plus les liserés, les consoles, les lanternes : environ 1 000 objets pour la tour. Les fusionner (`Mesh.CombineMeshes`) par rampe diviserait les appels de dessin par cent.
484. **→ Le château : des milliers de petits cubes** (merlons, chaperons, meurtrières, joints). Même remède.
485. **→ Les particules : 600 par cascade (×4), 220 + 120 pour les nuages, 450 poussières** : raisonnable.
486. **→ Chaque effet de capacité crée ses systèmes de particules** (5 à 6 pour une Onde) : un pool.
487. **→ L'anticrénelage × 8** : le plus gros coût fixe (313).
488. **→ Les ombres en 4 cascades à 160 m** : le deuxième.
489. **→ La sonde de reflets en 128 px, prise une fois** : rien.
490. **→ Les icônes de l'interface : fabriquées au pixel (v12), au plus 900 en cache** : rien.

## XXII. Le chemin

491. **→ Faire tester par une deuxième personne neuve** (un ami, un cousin) **avec l'aide écrite**, puis **sans** : c'est la seule façon de savoir si les icônes seules suffisent. Le frère de Martin a déjà répondu pour la version sans.
492. **→ Filmer la partie** (OBS, gratuit) : cinq minutes, envoyées image par image à Claude, valent cette liste entière.
493. **→ Le Castle Kit de Kenney** (`docs/MODELES.md`) : la plus grosse marche visuelle possible, et c'est la loi n°1 du projet (des assets de banques).
494. **→ Le personnage Quaternius** : même chose, un seul fichier.
495. **→ Un pack de sons Kenney** (Impact, Interface, RPG) : 422.
496. **→ Un ciel peint** (une image panoramique CC0) : 306.
497. **→ Un logo** (le frère de Martin) : 102.
498. **→ Une charte** (`docs/CHARTE.md`) : un or, une pierre, un bois, un bleu, un rouge ; trois matières ; une règle de lumière (475).
499. **→ Le post-traitement (bloom, couleurs)** : un paquet Unity à installer, un vrai choix technique — à discuter avant (320).
500. **→ Et la question que Martin doit trancher** : **l'aide écrite, oui ou non par défaut ?** Elle est là, réglable. Le designer chiant vote oui : Fall Guys écrit « QUALIFIÉ », « ÉLIMINÉ », le nom de chaque épreuve et sa règle en une ligne. Zéro texte, c'est beau sur une capture d'écran ; quelques mots au bon moment, c'est un joueur qui comprend.
