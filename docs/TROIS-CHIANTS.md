# Les trois chiants, v21 : le clipper, le gamer, le designer

*03/10/2026. Martin : « Quand il y a du son, il y a une sorte d'émoji son qui apparaît, et
pareil pour la lumière, c'est horrible. Le jeu n'est pas fluide du tout. Il faut revoir
tous les obstacles pour les rendre magnifiques. Tous, tous, tous. Puis tu peux refaire le
clipper chiant, le gamer chiant, le designer chiant, carte blanche. »
Passages précédents : `docs/CLIPPER-CHIANT.md` (v20), `docs/GAMER-CHIANT-3.md` (v18),
`docs/DESIGNER-CHIANT-3.md` (v14).*

Claude ne peut pas lancer Unity : les chiffres viennent du code, les formes des obstacles
ont été vérifiées sur des **aperçus dessinés hors d'Unity** (mêmes pièces, mêmes cotes),
le reste est **à confirmer en jeu**.

Légende : **✔** fait dans ce passage (v21) · **→** proposé, avec la piste · **?** à
vérifier en jouant · **✘** refusé.

---

## 0. Les trois demandes de Martin

1. **✔ L'émoji son et l'ampoule.** Ce ne sont pas des objets du jeu, ce sont les **gizmos**
   d'Unity : l'éditeur pose un haut-parleur sur chaque source de son et une ampoule sur
   chaque lumière. Depuis que les bruits viennent de l'endroit où ils arrivent (v19), il y
   en avait un à chaque son. → `Editor/NoGizmos.cs` coupe ces icônes et décoche le bouton
   **Gizmos** de la fenêtre Game, **tout seul**, à l'ouverture et à chaque Play. (Menu
   **FIEF ▸ Cacher les icônes son et lumière** pour le relancer à la main.) Dans le jeu
   exporté, ils n'ont jamais existé.
2. **✔ La fluidité** (détail au II).
3. **✔ Tous les obstacles refaits** (détail au III).

---

## I. Le clipper chiant (1-25)

*Il ne regarde que les 12 secondes qui feront tourner la tête dans le fil.*

1. **✔ Un obstacle doit se reconnaître dans un clip de 2 secondes, le son coupé.** Tous les
   pièges parlent maintenant la même langue : **le rouge frappe**, la face crème est la face
   qui te touche. Un butoir, un poing de bélier, une masse de marteau, une barre à rayures,
   un boulet : on sait d'un coup d'œil ce qui va t'envoyer dans les nuages.
2. **✔ La barre à rayures rouges et crèmes** (balayeurs, moulinets) : une barre qui tourne,
   rayée comme un sucre d'orge, se lit **en mouvement** même floue dans un clip compressé.
3. **✔ Le poing du bélier** sort d'un portail rond cerclé d'or, sa rune vire au rouge avant
   de frapper : le « il va sortir… BAM » se voit à l'image.
4. **✔ Les boulets rayés** : une boule rouge à deux bandes crèmes croisées **qu'on voit
   rouler**. Avant, une boule grise : on ne voyait pas qu'elle roulait vers toi.
5. **✔ La Couronne qui change de mains** fait un **éclair d'or** sur la tête du nouveau
   porteur, pour tous ceux qui le voient : dans un clip, on voit *où* elle est passée.
6. **✔ Le « whiff »** : une poussée dans le vide fait déjà un souffle (vérifié).
7. **✔ La herse montre ses trous** : un trou noir sous chaque pointe, sur une grille qui
   rougit. Le clip du « je savais qu'elle allait sortir » devient possible.
8. **✔ Les chevrons de la chicane** montrent le passage : le clip du bot qui se jette dans
   le mauvais côté devient drôle, parce qu'on voit qu'il y avait une flèche.
9. **→ Le « bonk » propre à chaque obstacle** : butoir (caoutchouc), bélier (bois sourd),
   marteau (cloche), barre (sifflet). Dossiers `Sons/Butoir`, `Sons/Belier`… à remplir.
10. **→ Le rebond de caméra quand un obstacle te sort** (un petit recul, pas un ralenti).
11. **→ « PHOTO FINISH »** : deux joueurs au sommet à moins d'une seconde (docs/CLIPPER-CHIANT n° 60).
12. **→ « À UN MÈTRE ! »** : éjecté à moins de 5 m du sommet (n° 53).
13. **→ Le premier en haut annoncé à tous** (n° 59).
14. **→ La voix de l'arène sur les KO** (n° 89).
15. **→ Varier la hauteur du boum de poussée** de ±5 % (n° 8) : le son 2D passe par une seule
    source, il faudrait une petite source à part.
16. **→ Le mode « écran propre »** (sans HUD) pour enregistrer (n° 36).
17. **→ Les marqueurs Steam** posés par `Highlights` (Phase 5).
18. **? La micro-pause d'impact** : toujours à faire valider par Martin (pas d'aura).
19. **? L'éclair d'or de la Couronne** ne doit pas se confondre avec celui d'un KO (blanc).
20. **→ Les pendules en contre-temps** : deux pendules voisins qui se croisent font un plan
    parfait. Aujourd'hui, leurs phases sont au hasard.
21. **→ La barre du moulinet qui accélère** les 20 dernières secondes de la manche.
22. **→ Les confettis rouges et crèmes** quand un obstacle sort le porteur.
23. **→ Le « rase-mottes »** sous une barre de moulinet en plein saut (n° 68).
24. **→ La traînée des boulets** (une braise derrière eux) : on suivrait leur course.
25. **✔ Rien de tout ça n'est de l'aura** : pas de ralenti, pas de « +1000 ».

## II. Le gamer chiant (26-55)

*Il joue en Full HD, souris à 800 DPI, et il sent chaque image qui manque.*

26. **✔ « Le jeu n'est pas fluide du tout. »** La cause la plus probable : **les lumières**.
    En rendu *Forward* (celui du projet), chaque lumière « au pixel » qui touche un objet le
    fait **redessiner en entier**. Lanternes des sept bots, sanctuaires, Monuments, sommet,
    gargouilles : une vingtaine de lumières, jusqu'à quatre passes en plus par objet. →
    **Deux lumières au pixel au plus** (le soleil et la plus proche) ; les lanternes, les
    sanctuaires, les Monuments et le sommet éclairent **au sommet** (sans passe en plus) ;
    ta lanterne reste au pixel.
27. **✔ L'anticrénelage x8 → x4.** À l'œil, la même image ; pour la carte graphique, moitié
    moins de travail sur chaque pixel — sur la mer de nuages surtout (de grands voiles
    transparents empilés).
28. **✔ Les ombres jusqu'à 130 m au lieu de 160** (toujours en 4 cascades, toujours fines de près).
29. **✔ La mer de nuages** : moins de voiles superposés (160 + 80 au lieu de 220 + 120), un peu
    plus grands — la même mer, moins de pixels peints trois fois.
30. **✔ Les éclats lointains** : une gerbe, une onde, un anneau, un éclair à plus de 110 m de
    la caméra ne sont plus fabriqués (chacun créait puis détruisait un objet). Les colonnes
    de KO, faites pour être vues de loin, restent.
31. **✔ Les obstacles soudés.** Une herse de couloir, c'étaient **115 pointes** dessinées une
    par une (plus 115 trous) ; une herse de la tour, 24 ; chaque barre, 20 clous ; chaque
    boulet, 4 morceaux. Tout ce qui bouge est maintenant **un seul dessin par pièce**
    (`Proto.Weld`), et ce qui ne bouge pas (potences, portiques, colonnes) est figé avec le
    décor. Des centaines d'appels à la carte graphique en moins à chaque image.
32. **→ Si ça rame encore** : dans l'éditeur, **ferme l'onglet Scene** pendant que tu joues
    (sinon Unity dessine tout **deux fois**), et regarde **Game ▸ Stats** : le chiffre
    « Batches » et les FPS. Envoie-moi une capture de Stats et de la fenêtre **Profiler**
    (**Window ▸ Analysis ▸ Profiler**, onglet CPU) : je saurai ce qui coûte.
33. **→ La synchro verticale** : si le jeu tourne à 55 images par seconde sur un écran 60 Hz,
    la synchro le fait tomber à **30** — une saccade nette. Si Stats montre ~30 FPS pile,
    c'est elle : on la réglera (Réglages ▸ Synchro).
34. **→ Un réglage Qualité (Basse / Haute)** dans Réglages : ombres, anticrénelage, nuages.
35. **✔ Le pendule ne rentre plus dans le mur** : côté mur, sa course s'arrête à 18° au lieu de
    24° (le butoir, plus large, y entrait).
36. **✔ Les hitboxes n'ont pas bougé** : les nouveaux dessins ont exactement les mêmes zones
    de coup qu'avant (butoir 1,9 m, masse 2 m, bélier 5,6 × 2,2 × 1,8, barres à 0,8 m de haut).
37. **? La masse du marteau descend plus bas** (plus grosse) : 15 cm au-dessus du sol au plus
    bas. À vérifier qu'elle ne rentre pas dans le sol là où le couloir monte.
38. **? Le poing du bélier au repos** : il doit se voir, rouge, au fond de son portail. Si on
    ne le voit pas assez, avancer le bélier de 10 cm.
39. **→ Les bots lisent déjà les obstacles** (`Hazards`) : ils ne regardent pas la couleur, ça
    ne change rien pour eux.
40. **→ Les pendules : phase opposée sur deux rampes voisines** (l'équité est déjà là : le
    même nombre par rampe).
41. **→ Les trous de la herse** pourraient servir aux bots débutants (ils attendent déjà).
42. **→ Le moulinet du couloir** est long (8,6 m de bras) : on ne voit pas toujours l'autre
    bout. Les bouts luisent, les rayures aident ; à confirmer.
43. **→ Un son quand le bélier rentre** (un « clonk ») : on saurait qu'on peut passer.
44. **→ La herse qui cliquette** 0,5 s avant de sortir (en plus du rouge).
45. **→ Un réglage FPS affichés** (petit chiffre dans un coin, pour les testeurs).
46. **✘ Un point de reprise sur la tour** : refusé par Martin (02/10).
47. **→ Les gargouilles** : la même famille de couleurs ? Non : elles sont des bêtes, pas des
    machines ; on les garde en pierre bleu ardoise (elles le sont déjà).
48. **→ La jauge de la tour** au HUD pourrait montrer les pièges devant toi.
49. **→ La poussée sur un obstacle** : si tu pousses quelqu'un sous un pendule, le KO devrait
    être à toi (aujourd'hui : le dernier coup compte).
50. **? Les chevrons de la chicane** : lisibles de face, à confirmer de biais.
51. **→ L'arbaleste de départ** : garder la main sur la visée plus longtemps (v16).
52. **→ Des obstacles différents selon la manche** (par exemple le marteau seulement à partir
    de la manche 2) : un peu de nouveauté à chaque manche.
53. **→ Les pièges de la cour** (hors couloirs) : aujourd'hui aucun ; c'est voulu (on s'y bat).
54. **✔ Aucun changement de règle** dans ce passage : seulement des dessins, des sons et de la fluidité.
55. **→ Un test de 30 minutes** avec Stats ouvert : la porte 1 de la Phase 1.

## III. Le designer chiant (56-90)

*Il a travaillé sur des jeux de plateau colorés, et il déteste ce qui a l'air « fait par IA ».*

56. **✔ Une famille, pas un catalogue.** Avant : un butoir rouge, un bélier gris carré, une
    barre en bois à clous, une masse en tonneau de fer, une herse grise, des boulets gris —
    six objets de six jeux différents. → **Une seule famille** (`World/ObstacleKit.cs`) :
    - **ROUGE satiné** : ce qui frappe ;
    - **CRÈME** : la face de frappe, les rayures, les bouts ;
    - **ARDOISE bleu nuit** : ce qui porte (potences, portails, perches, traverses) — la
      couleur des toits du château ;
    - **OR MAT** : les bagues, les cercles, les boules de faîte — jamais brillant ;
    - **BRAISE** (ambre qui luit) : ce qui prévient.
57. **✔ Rien de carré qui frappe** : des fûts aux bords arrondis tournés d'une pièce, des
    dômes, des boules. La lumière du soir glisse dessus au lieu de s'arrêter sur une arête.
58. **✔ LE PENDULE** : une platine ronde d'ardoise cerclée d'or et rivetée sur le fût, une
    potence ronde baguée d'or tenue par une jambe de force d'acier, un moyeu d'or à axe
    rouge ; le bras d'acier à deux bagues ; le butoir en **palet rouge** aux bords ronds,
    ceinture d'or, une face crème et sa cible rouge de chaque côté.
59. **✔ LE BÉLIER** : un **portail rond** d'ardoise dans le fût, cerclé d'or, quatre rivets,
    un trou noir ; le bélier est un fût d'ardoise bagué d'or, terminé par un **poing rouge**
    à face crème, la rune au centre (bleue au repos, rouge avant de frapper).
60. **✔ LE BALAYEUR ET LE MOULINET** : une **perche tournée** (socle d'ardoise, fût crème,
    moyeu d'or, dôme d'ardoise, boule d'or) et une barre à **rayures rouges et crèmes**,
    bouts crème cerclés de braise.
61. **✔ LA HERSE** : une plaque d'ardoise sombre dans un **cadre d'or**, la grille de runes qui
    luit, **un trou noir sous chaque pointe** ; les pointes tournées (un collet, une pointe fine).
62. **✔ LE MARTEAU** : un **portique** de colonnes crème sur socle d'ardoise, chapiteau d'or,
    tailloir, dôme et boule — les colonnes des pieds de rampe ; une traverse d'ardoise
    baguée d'or, un moyeu d'or ; la masse est un **maillet rouge** à faces crèmes et cibles,
    bagues d'or et bande de braise.
63. **✔ LES BOULETS** : rouges, deux bandes crèmes croisées, une bande de braise.
64. **✔ LA CHICANE** : un chaperon d'ardoise, un filet d'or, une **colonne** au bout libre
    du mur (là où l'on passe), et des **chevrons** de braise sur les deux faces qui montrent
    le passage.
65. **✔ LES BANDES DE DANGER** sur les rampes : de vraies bandes de chantier, braise et ardoise
    jointives, bordées d'un liseré d'or (avant : quatre barres orange qui flottaient).
66. **✔ Même famille que le château** : l'ardoise des toits, l'or mat, la pierre crème des
    colonnes. Les obstacles ont l'air construits par les mêmes gens.
67. **✔ L'or reste mat** (décision du 01/10) : lissé 0,38, jamais un miroir.
68. **→ Le rouge des obstacles** et le rouge de la **porte rouge** (bannières) : à comparer en
    jeu. Si ça se confond, passer les obstacles à un rouge plus cerise.
69. **→ Les potences et les portiques** pourraient porter la **couleur de la bande de hauteur**
    (bleu, vert, or…) en petit (une bague) : on saurait à quelle hauteur on est.
70. **→ Des fanions** aux coins des portiques du marteau (qui claquent au vent, `Flutter`).
71. **→ Les murets des couloirs** : même chaperon d'ardoise que la chicane.
72. **→ Les arches des couloirs** (une poutre de pierre aujourd'hui) : même dessin que le
    portique du marteau (colonnes, dômes).
73. **→ Les pointes de la herse** en acier bleui (un peu d'ardoise dedans) plutôt que gris.
74. **→ Les gargouilles** gardent leur pierre bleu ardoise : bien. Leur jet de feu est orange
    comme la braise des obstacles — cohérent.
75. **→ Les arbalestes** (bois et fer) : les passer dans la famille (ardoise et or) ?
    À Martin de dire ; l'arbaleste a été refaite le 02/10 et il l'aime bien.
76. **→ Le socle de la Couronne** : or mat et ardoise, déjà dans la famille.
77. **→ Les sanctuaires** : même perche tournée que les balayeurs, en plus grand.
78. **→ La cible rouge** au centre des faces : on pourrait y graver l'icône du piège (SDF) —
    trop petit pour se lire, on l'oublie.
79. **✔ Proportions** : tout a été redessiné aux **mêmes cotes** que les zones de coup ; ce
    qu'on voit est ce qui frappe.
80. **→ Une animation au repos** : le poing du bélier qui « respire » (5 cm) avant de frapper.
81. **→ Les rivets** : 4 sur la platine du pendule et le portail du bélier ; on pourrait en
    mettre sur la plaque de la herse.
82. **→ Un vernis plus mat sur l'ardoise** (0,55 → 0,45) si ça brille trop au soleil couchant.
83. **→ Le bleu de l'ardoise des obstacles** est un peu plus clair que celui des toits
    (lisibilité sur la pierre crème de la tour) : voulu.
84. **→ Les moulinets du couloir** pourraient avoir un **dôme rouge** au lieu d'ardoise (ils
    sont au milieu du chemin : on doit les voir de loin).
85. **→ Aperçus** : `Tools/icones` fait des aperçus des icônes ; un `Tools/obstacles` ferait
    ceux des obstacles (Claude s'en est fait un pour ce passage, hors du dépôt).
86. **→ Le « Castle Kit » de Kenney** remplacera les murs ; les obstacles, eux, restent faits
    par le code : aucun pack ne les a.
87. **✔ Zéro création 3D sur mesure** : tout est fait de formes simples (fûts, dômes, boules,
    pavés), aucun modèle dessiné à la main — la loi n° 1 tient.
88. **✔ Plus rien de gris** dans les obstacles, sauf l'acier des bras et des pointes.
89. **✔ Plus de clous en diamant**, plus de cubes tournés, plus de tonneau de fer.
90. **→ À Martin et à son frère de dire** lesquels sont « magnifiques » et lesquels ne le sont
    pas encore : une capture de chaque, et je reprends ceux qui ne vont pas.

---

**Résumé v21** : plus d'icônes son et lumière dans la fenêtre Game ; un jeu plus léger
(deux lumières au pixel, anticrénelage x4, obstacles soudés, éclats lointains ignorés,
nuages allégés) ; **tous les obstacles refaits dans une seule famille** (rouge qui frappe,
crème, ardoise, or mat, braise) ; l'éclair d'or quand la Couronne change de mains.
