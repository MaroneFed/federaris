# Le gamer chiant, quatrième passage : 100 nouveaux reproches

*01/10/2026, le soir. Martin : « Tu peux faire une nouvelle session du gamer chiant et du
designer chiant. […] One hundred. » Passages précédents : `docs/100-RAISONS.md`,
`docs/100-PROBLEMES.md`, `docs/GAMER-CHIANT.md` (v12).*

Le gamer chiant a joué la v12 (net au pixel, danse, haricots). Il ne répète pas ses
anciens reproches : ceux-là sont nouveaux. Claude ne peut toujours pas lancer Unity ; il a
relu cette fois les systèmes qu'il n'avait que survolés — la tour et ses obstacles, les
couloirs, chaque capacité jusqu'à ses cas limites (en l'air, contre un protégé, à travers
un mur), les gargouilles, le choix des capacités, la caméra — et joué chaque cas dans sa
tête. Les chiffres sont ceux du code.

Légende : **✔** corrigé dans la v13 · **→** reste à faire (avec la piste), ou décision
qui revient à Martin.

---

## I. « C'est pas juste » (l'équité)

1. **✔ Les boulets tombaient toujours du même côté selon la rampe.** Le boulet n° *n* partait sur la rampe *n* modulo 4, côté mur si *n* était pair : les rampes 0 et 2 avaient **toujours** leur boulet côté mur, les rampes 1 et 3 **toujours** côté vide. → Une **volée** : un boulet sur **chaque** rampe au même instant, du même côté pour toutes, en alternant (`BoulderChute`).
2. **✔ Du coup, chaque rampe ne voyait passer qu'un boulet toutes les 80 s.** → Une volée toutes les 34 s (moins aux manches suivantes, `Tower.Hardness`) : plus de boulets, et les mêmes pour tous.
3. **✔ Les couloirs piégés n'étaient pas les mêmes devant les quatre portes.** Chaque porte tirait ses quatre stations à la suite dans le même hasard : l'un avait trois marteaux, l'autre trois chicanes. → On tire **une** suite de stations, et les quatre portes la construisent à l'identique, comme les rampes de la tour (« personne n'a le couloir facile »).
4. **✔ Même chose pour la vitesse des moulinets et le rythme des herses** d'une porte à l'autre. → Tirés une fois pour les quatre.
5. **✔ Au choix des capacités, à égalité de victoires, le même joueur choisissait le premier sa passive ET son active** (la même graine pour les deux tours de table). → Une graine par tour de table.
6. **→ Deux joueurs par porte partent de deux plateformes « à la même distance »** : vrai à plat, mais l'arbaleste pose sur le parvis, et celui qui atterrit du côté du couloir le plus court gagne une demi-seconde. À mesurer en jeu.
7. **→ Le vainqueur de la manche choisit en dernier, mais il garde tout le reste** (sa place, sa plateforme). C'est l'unique frein au « snowball » : à surveiller sur un match de 10 manches (Phase 4).
8. **→ Une manche sans vainqueur ne compte pour personne** ; sur un match de 3 manches, deux manches blanches et c'est un match à une manche. Voir la **prolongation** dans `v2-ideas.md`.
9. **→ En départage, un non-qualifié peut encore prendre la Couronne** et la garder loin des ex æquo. C'est un « trouble-fête » amusant ; il faut juste que Martin le veuille.

## II. Les capacités dans les coins

10. **✔ Le Rappel après une chute dans les nuages te renvoyait… dans le vide.** Il ramène là où l'on était il y a 4 s — et 4 s avant le respawn, on tombait sous l'île. → Le respawn **efface la mémoire** du Rappel (`PlayerController.Forget`, `Rival.Forget`).
11. **✔ L'Échange à travers la muraille** : en vol au-dessus des remparts, on prenait la place d'un joueur **dans la cour** — le sceau contourné, sans porte ni couloir. → Pas d'échange entre dedans et dehors : la capacité ne part pas et ne coûte rien (quelques étincelles d'or sur la cible).
12. **✔ L'Échange déplaçait un joueur protégé** (au départ, après un respawn). « Protégé : rien ne le touche. » → Refusé, rendu.
13. **✔ La Mine lancée en plein vol** restait suspendue dans le vide, au niveau du sol de l'île. → Pas de sol à 4 m sous toi : elle ne part pas et ne coûte rien.
14. **✔ Le Mur lancé en plein vol** sortait… en l'air, à ta hauteur. → Il sort du **sol** ; pas de sol : rien, et pas de recharge.
15. **✔ Un joueur protégé déclenchait une mine** sans rien prendre : la mine était gâchée. → Il passe dessus sans la réveiller.
16. **✔ Le Givre ralentissait un joueur protégé** pendant 3 s (et lui teintait l'écran) alors que le coup, lui, glissait sur la protection. → Le protégé n'a rien.
17. **→ Le Souffle traverse tout** (c'est du vent, 150 m) : depuis une rampe, il balaie aussi la rampe d'en face à travers le fût de la tour. Voulu (28/09 : « que ça passe toute la map »), mais avec **9 s** de recharge c'est la capacité la plus forte du jeu. Piste : 12 s.
18. **→ Le Clignement en bord de rampe** peut te poser dans le vide : il s'arrête avant un mur, pas avant un précipice. C'est ton choix, mais les ailes ne s'ouvrent qu'au-dessus de 6 m de vide.
19. **→ La Ruée non plus ne regarde pas le vide.** Douze mètres, c'est plus que la largeur de la rampe (6,5 m).
20. **✔ Le Grappin s'accrochait à la muraille depuis dehors** et te hissait sur le rempart : on sautait ensuite dans la cour, porte et couloir contournés (le sceau ne regarde que le vol plané, l'arbaleste et le piqué). → Depuis dehors, le croc ne mord pas la muraille : quelques étincelles d'or, et la capacité ne coûte rien.
21. **→ Le Rappel ramène la Couronne avec toi** (on la porte) : 4 s en arrière, sur la tour, peut valoir un sommet. Voulu ?
22. **→ La Nuée (fumée) bloque la vue des gargouilles et des bots, pas celle du joueur** (il voit à travers les particules). Normal pour un jeu solo ; en ligne, la fumée devra cacher les pseudos.
23. **→ Le Voile (7 s) cache ton pseudo aux autres**, mais pas ton ombre ni ta lanterne à ta couleur. À décider : invisible pour de vrai, ou « difficile à viser » ?
24. **→ Deux mines par joueur** : la troisième fait sauter la plus vieille **sans explosion**. On pourrait la faire exploser pour de vrai (un piège qu'on rappelle).
25. **→ Le Mur (10 m sur 4,5) peut fermer une rampe entière** pendant 8 s. Contre un porteur qui descend, c'est parfait ; contre toute la file derrière soi, c'est frustrant. À tester.

## III. La tour et les couloirs

26. **✔ Les herses ne piquaient qu'à l'instant où elles jaillissaient** : on traversait ensuite, pendant une demi-seconde, une grille hérissée de pointes sans rien sentir. → Elles piquent **tant qu'elles sont sorties** (une fois par passage).
27. **→ Sur la tour, un coup de herse t'éjecte de la tour** (règle des obstacles, 29/09) : c'est plus dur qu'avant la v13, puisque la herse pique plus longtemps. Si c'est trop, rendre la herse de rampe plus lente (`SpikeTrap` période).
28. **✔ Les gargouilles finissaient leur charge sur un joueur devenu protégé** (il venait de réapparaître) : le coup glissait, mais le rayon rouge et la cible au sol le suivaient. → La charge s'éteint.
29. **✔ … et quand le jet touchait un protégé, l'écran tremblait et la statistique « touché par une gargouille » montait** quand même. → Rien sur un protégé.
30. **→ Les boulets ne s'arrêtent pas aux obstacles** (ils traversent pendules et balayeurs). Personne ne le verra à 9 m/s, mais un boulet qui explose contre un bélier serait un beau moment.
31. **→ Entre deux rampes, 12,5 m de haut** : tomber d'une rampe sur celle du dessous est une chute de 12 m sans ailes (elles ne s'ouvrent qu'à 6 m de vide *sous* soi). On ne meurt pas, mais on se sent puni deux fois.
32. **→ Le sommet n'a qu'un seul moyen de redescendre** : sauter. Un joueur qui y arrive sans la Couronne (elle est déjà partie) n'a rien à faire là-haut.
33. **→ La Couronne revient au sommet 20 s après être tombée** : pendant ce temps, ceux qui sont en haut attendent. Piste : 12 s si la manche est courte (4 min).
34. **→ Les moulinets des couloirs tournent à la vitesse `Hardness`** dès la manche 1 : 1,3 à 1,9 tour par seconde, ça se saute ; à la manche 8, 2,7. À essayer.
35. **→ Le marteau des couloirs projette « très loin sur le côté »… contre le muret de 3 m** : on ne part pas, on rebondit. C'est un marteau qui gêne, pas un marteau qui éjecte.
36. **→ Les chicanes n'ont pas de repère au sol** du côté ouvert : on le voit, mais seulement à 10 m.

## IV. Le vol, les arbalestes, le sceau

37. **→ Le porteur plane à 11 m/s et tombe à 8 m/s** : du sommet (100 m), il fait 140 m à plat, et les Monuments sont plus loin. Il **doit** passer par un courant d'air ou une arbaleste, comme voulu (30/09). Mais rien ne le lui dit (plus de texte) → l'**astuce en icônes** (voir 60).
38. **→ Les courants d'air montent jusqu'à 80 m** : un porteur qui tombe dans un courant remonte à 80 m, pas au sommet. Voulu.
39. **→ Le sceau renvoie aussi un joueur qui tombe de la tour en planant et revient au-dessus de la cour** : il vient de dedans, il voulait juste retourner à la porte. Le sceau ne regarde que « passer de dehors à dedans » : à vérifier sur les bords.
40. **→ Le sceau ne s'applique pas à l'Échange, au Clignement, ni au Rappel.** L'Échange est bouché (11) ; les deux autres restent à courte portée (15 m, 4 s) et ne passent pas de mur.
41. **→ L'arbaleste de départ pose sur le parvis sans viser, ±22°** : une fois lancé, on ne peut plus rien faire pendant 2 s. Voulu (« sans viser »).
42. **→ Les arbalestes des îlots ne montrent leur point d'arrivée qu'une fois monté.** Choisir la bonne arbaleste demande de monter sur chacune.
43. **→ Le piqué d'aigle vise le porteur** ; s'il n'y a pas de porteur, il ne fait rien — et la touche ne le dit pas (plus de texte).

## V. La Couronne et le sacre

44. **✔ En manche de départage, un joueur non qualifié qui entrait dans le cercle faisait monter la barre du sacre jusqu'au bout… et rien ne se passait** (les cloches, la barre à l'écran de tous, pour rien). → Pas de sacre pour lui : ni barre, ni cloches.
45. **→ Le sacre se remplit en 3 s et se vide deux fois plus vite** : sortir 1 s du cercle coûte 2 s. Juste, mais personne ne le sait.
46. **→ Le cercle fait 3,6 m de rayon et compte jusqu'à 4 m au-dessus** : on peut se faire sacrer en planant au-dessus du Monument, sans se poser. Voulu ?
47. **→ Pendant le sacre, le porteur peut encore lancer sa capacité** (défensive seulement : les offensives sont interdites au porteur). Un Mur autour du cercle rend le sacre presque imparable. À tester.
48. **→ La Couronne prise en passant** : pendant un saut au-dessus du socle, on la prend aussi (4 m de marge au-dessus). Voulu.
49. **✔ Le verrou de 3 s après un vol** (la victime ne peut pas la reprendre) n'était montré nulle part (noté en v12). → Une **croix rouge** sur la Couronne du HUD, le temps du verrou.

## VI. Les bots

50. **→ Un bot non qualifié en départage qui prend la Couronne file quand même au Monument** et s'assoit dans le cercle, pour rien. Il ferait mieux de la garder loin des ex æquo (en volant). À écrire dans `Rival`.
51. **→ Les bots n'utilisent jamais la Mine ailleurs qu'en courant avec la Couronne** (ou au hasard, 5 % en balade). Ils ne piègent pas le parvis ni la sortie d'un couloir.
52. **→ Les bots ne se servent pas du Mur pour bloquer une rampe**, seulement pour couvrir leur fuite, et jamais sur la tour.
53. **→ Les bots lancent le Souffle sur le porteur jusqu'à 90 m**, même s'ils ne le voient pas (ils « savent » où il est). Un joueur ne le pourrait pas sans le voir.
54. **→ Les bots ne se protègent pas des gargouilles** autrement qu'avec la Nuée ou le Voile : ils ne se cachent pas derrière le fût.
55. **→ Les bots coriaces et faciles ont les mêmes règles** (voulu), mais les faciles ne ratent jamais une poussée : ils sont seulement plus lents. Un peu d'erreur les rendrait plus humains.
56. **→ Les bots ne font jamais de feinte** (se laisser tomber, repartir). Phase 4.
57. **→ Un bot poussé contre le socle prend la Couronne en passant** : c'est la règle (01/10), mais un bot ne le fait jamais exprès.

## VII. L'écran, les sensations

58. **✔ La secousse de l'écran était un tirage au hasard à chaque image** : à 144 images par seconde, ça grésillait au lieu de secouer. → Un bruit doux (Perlin, 22 oscillations par seconde), le même à 30 ou à 240 images/s.
59. **✔ Les astuces ne s'affichaient plus du tout** depuis le « zéro texte » du 30/09 : un nouveau joueur ne savait pas qu'on monte sur l'arbaleste avec E. → **Des astuces en icônes**, une fois par match, sous le viseur : sur ta plateforme **[E] arbaleste [clic] ↑**, près d'une arbaleste de l'île **[E] arbaleste [clic] cible**, sur la rampe **tour ↑ Couronne**, quand un autre porte la Couronne **[pousser] Couronne**, quand tu la portes **Couronne → courant → Monument → sacre**, avec un don **don [capacité]**, contre le sceau **ailes ✗**.
60. **→ … et l'astuce « obstacle » n'a pas d'icône** (il faudrait dessiner une herse). `IconArt` : ajouter `obstacle`.
61. **✔ Ton pseudo pouvait être celui d'un bot** (« Oswin ») : deux « Oswin » au-dessus des têtes, le fil des événements illisible. → Le bot prend un autre nom.
62. **→ Changer de pseudo en plein match** ne renomme pas un bot qui aurait déjà ce nom (il n'est vérifié qu'au début du match).
63. **→ Pas de son distinct pour « on t'a volé la Couronne »** : le même « thud » que n'importe quel coup.
64. **→ Pas de réglage « réduire les secousses »** (certains joueurs ont le mal des transports en première personne). Phase 5, mais facile : un multiplicateur dans `Settings`.
65. **→ Le champ de vision par défaut est 78°** : sur un écran large, c'est étroit pour un jeu de course en première personne. Fall Guys est en troisième personne ; les FPS de course sont à 90°+.
66. **→ La vue se penche en vol (roulis)** : agréable, mais pas réglable.
67. **→ Refuser une capacité** (« Rien à viser », « Mains prises », « Recharge ») : une icône, mais pas de son de refus.

## VIII. Les petits bugs relus

68. **✔ Le Souffle fabriquait trois dégradés neufs à chaque image** pendant 2,5 s : du travail pour le ramasse-miettes (des saccades possibles). → Un seul dégradé, réutilisé.
69. **✔ La mine se posait à la hauteur de l'île (`Ground.Place`) avant d'être reposée** : deux calculs contradictoires. → Un seul rayon vers le sol.
70. **→ `Mine.Place` fait sauter la plus vieille mine par `Destroy`** : elle disparaît à la fin de l'image, donc pendant une image on a trois mines. Invisible.
71. **→ `Tether` (la chaîne lumineuse) suit sa cible même si elle se fait éjecter** : une chaîne de 40 m pendant 0,6 s. Joli ou bizarre ?
72. **✔ On pouvait crocheter ou échanger à travers la fumée de la Nuée** (`Combat.Aimed` ignorait les voilés, pas la fumée). → La fumée bloque la visée, pour toi comme pour les bots.
73. **→ `Monument` compte le sacre même pendant l'intro de fin** tant que `Season.Running` : sans effet, la manche est déjà finie.
74. **→ `Crown.PickUpByTouch` ne vérifie pas `Graced`** : un joueur protégé peut prendre la Couronne. Voulu (la protection empêche d'être frappé, pas d'agir).
75. **→ `Respawn.Of` ne remet pas à zéro `HiddenUntil`** : un joueur voilé qui tombe réapparaît encore invisible. Sans gravité.
76. **→ `Smoke.Clouds` n'est nettoyé qu'au changement de manche ou quand on l'interroge** : sans effet (les nuages expirés sont retirés à la lecture).
77. **→ `Eye.Spot` (le regard qui « fixe » avant de charger) ne distingue pas un porteur voilé d'un porteur visible** : le Voile coupe tout. Voulu ?
78. **→ Le rayon du `PlayerInteractor` (3,4 m) passe à travers les murs** : on peut « voir » l'invite d'une arbaleste de l'autre côté d'un muret.
79. **→ `Boulder` : le même boulet ne frappe pas deux fois le même joueur en 1,2 s**, mais deux boulets d'une même volée peuvent le frapper l'un après l'autre sur deux rampes (s'il tombe de l'une sur l'autre). Rare et drôle.

## IX. Le match, les menus

80. **→ Le match peut finir sans champion** (trois départages sans vainqueur : `Played >= Rounds + 3`). Il faudrait un dernier critère (le plus de temps avec la Couronne, par exemple) — à Martin.
81. **→ Une manche de départage sans vainqueur relance un départage**, sans rien dire de plus à l'écran.
82. **→ Le salon garde le nombre de bots d'un match à l'autre, mais pas la durée** ? À vérifier en jeu (`Match.Begin`).
83. **→ Pas de « rejouer la même graine »** : impossible de rejouer une manche qui a bugué pour la montrer à Claude. Une touche de débogage qui affiche la graine (F3 le fait-il ?) aiderait.
84. **→ Quitter en pleine manche ne demande pas de confirmation** si l'on est porteur. À vérifier (`Menus`).
85. **→ La pause arrête tout (`Time.timeScale = 0`), bots compris** ; la musique, elle, continue (`ignoreListenerPause`) : voulu, mais la musique de tension continue de monter pendant qu'on est au menu.
86. **→ Le compte à rebours de départ ne se saute pas** (7 s au premier départ, 3,8 s ensuite).

## X. Les performances (lues, pas mesurées)

87. **→ Chaque boulet, chaque obstacle, chaque gargouille parcourt la liste des joueurs à chaque image** : 8 joueurs × ~120 obstacles, c'est ~1 000 tests par image. Rien pour un PC, mais à garder en tête pour le Steam Deck.
88. **→ Les rampes sont faites de ~400 dalles séparées** : chacune est un objet. Les fusionner (`Mesh.CombineMeshes`) ferait gagner beaucoup d'appels de dessin.
89. **→ Les lanternes de la tour : une vraie lumière sur deux** (8 lumières ponctuelles sans ombre). OK en « forward » ; à surveiller.
90. **→ `Fx.Burst` crée un système de particules par effet** : une Onde de choc en crée 5 à 6. Un pool d'effets éviterait des saccades en fin de manche.
91. **→ Les textures des icônes et des pastilles sont faites à la demande** (4 par image au plus) : la première ouverture du menu peut « clignoter » d'une image.

## XI. Le code lui-même

92. **✔ Le code des trous et des courants de la rampe** (supprimés le 29-30/09) était toujours là : `Gaps`, `IsGap`, les barres rouges « Bord du trou », la classe `Updraft` entière (60 lignes), qu'un bot et le HUD interrogeaient encore. → Supprimés.
93. **✔ `FloatingTexts`** (les « +1000 AURA » du 28/09) n'affichait plus rien depuis la fin de l'aura. → Supprimé (le script et son `.meta`).
94. **✔ Des commentaires qui mentaient** : « la forêt vit derrière le menu », « le mendiant », « touche V », « 45 s », « les coffres ». → Remis à jour ; le journal de la console dit « gargouilles » et plus « Yeux ».
95. **✔ Trois doubles `<summary>`** (deux documentations collées sur le même membre : `Match.History`, `Course.Plaza`, `OrbitCamera.wide`). → Un seul, le bon.
96. **→ `Hud.cs` garde les phrases d'astuces** (elles disent l'intention, et l'icône est à côté). À traduire en anglais en Phase 5 si on les réaffiche un jour.
97. **→ `Rival.cs` fait toujours 1 250 lignes.** Le couper en `RivalBrain` (décisions) et `RivalBody` (mouvement).
98. **→ Aucun test automatique** des cas limites de cette liste (Rappel après respawn, Échange à travers la muraille…). Un petit banc d'essai en mode « Play Mode Test » d'Unity pourrait les rejouer.
99. **→ `Seeker` porte 20 minuteurs (`…Until`)** : un seul `Status` (protégé, étourdi, lent, caché, éjecté) serait plus lisible et plus facile à envoyer en réseau (Phase 3).
100. **→ Le gamer chiant n'a toujours pas joué pour de vrai.** Une vidéo de 5 minutes d'une manche (OBS, envoyée à Claude image par image) reste le meilleur prochain pas.
