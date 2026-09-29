# Le designer chiant, deuxième passage : 100 nouveaux reproches

*01/10/2026, le soir. Martin : « Tu peux faire une nouvelle session du gamer chiant et du
designer chiant. […] One hundred. » Premier passage : `docs/DESIGNER-CHIANT.md` (v12).*

Le designer chiant a regardé la v12 : les haricots, le château assagi, l'or mat. Il a
une règle, celle que Martin a posée le 01/10 : **« l'or mat — presque rien ne brille »**.
Il est allé la vérifier matière par matière dans la tour, les couloirs et les obstacles,
que la v12 n'avait pas repeints. Puis la cohérence (un même style partout), l'écran, le
son, et la dette qui finit par se voir.

Légende : **✔** corrigé dans la v13 · **→** reste à faire (avec la piste), ou goût
qui revient à Martin.

---

## I. « Presque rien ne brille » : la tour

1. **✔ Les six filets d'or qui ceinturent le fût luisaient** (matière lumineuse, 0,9). Le château avait l'or mat, la tour l'or néon. → **Or mat** (métal satiné), comme les portes du château.
2. **✔ Les fenêtres de la tour brillaient à 1,3**, deux fois plus que celles du château (0,55) : deux lumières différentes pour le même bâtiment. → 0,55, comme le château.
3. **✔ Le liseré de couleur au bord des rampes** brillait à 1,2 : en plein jour, une piste de néon. → 0,8 : on voit encore où finit la rampe (c'est un repère), sans fluo.
4. **✔ Les lanternes de la rampe étaient des cubes lumineux** (2,2) collés au fût : des ampoules carrées. → Une **lanterne ronde** sur une console de fer, coiffée d'un petit chapeau conique, lumière 1,4.
5. **✔ Les bannières luisaient** (0,55) : du tissu qui éclaire. → Tissu **satiné** à la couleur de la bande, qui prend la lumière du soleil.
6. **✔ Le blason doré de chaque bannière brillait à 1,2.** → Or mat.
7. **✔ Le filet d'or des arches au pied des rampes brillait à 2**, la matière la plus vive de la tour. → Or mat.
8. **✔ Les runes des boulets brillaient à 2,2.** → 1,1 : on voit venir le boulet, ce n'est pas une boule disco.
9. **✔ Les merlons du sommet étaient des pavés sombres**, sans rapport avec les créneaux du château (pierre claire, chaperon). → Pierre claire **coiffée d'un chaperon** sombre, comme la muraille.
10. **→ Le cercle de runes du sommet (0,55) et la dalle** restent : c'est la « scène » de la Couronne, il doit se voir du ciel.
11. **→ Les consoles sous les rampes sont des cubes** (une tous les quatre pas). Une console en quart de cercle (un cylindre coupé) serait plus belle — pas de primitive pour ça : le **Castle Kit de Kenney** en a.
12. **→ Les dalles des rampes sont des pavés inclinés** qui se chevauchent : de près, on voit les marches. Un vrai ruban de rampe (un seul maillage courbe, généré) serait lisse — et bien moins d'objets (voir le gamer, 88).
13. **→ Le fût est le cylindre d'Unity (20 faces)** : de près, on voit les facettes. Un cylindre maison à 48 faces pour le fût seul, ou une texture de pierre (Kenney).
14. **→ Six bandes de couleur, du bleu au violet** : elles servent de repère (« je suis au orange »), mais toutes saturées sur un château crème, c'est un arc-en-ciel. Piste : les désaturer de 20 %.

## II. « Presque rien ne brille » : les couloirs et les obstacles

15. **✔ La rune de l'arche des couloirs brillait à 2.** → 1,1 : elle se voit du ciel au soleil couchant, sans fluo.
16. **✔ La rune des chicanes brillait à 1,8.** → 1,1.
17. **✔ Les piliers de l'arche étaient des poteaux carrés** (1,8 × 6 m). → **Piliers ronds**, chapiteau et boule, comme les piliers des Monuments.
18. **✔ Les merlons des murets n'avaient pas de chaperon** (le château, si). → Chaperon.
19. **✔ Les bouts des moulinets étaient des cubes lumineux à 2,6** — la matière la plus vive du jeu, sur un obstacle. → **Boules** de fer à lueur ambre (1,3).
20. **✔ La masse des marteaux était un pavé** avec une bande à 2,4. → Un **tonneau de fer couché**, bouts arrondis, cerclé d'une bande ambre (1,3).
21. **✔ Les embouts de l'arbaleste brillaient comme des ampoules** (2,2) et la collerette du carreau à 2,5. → Embouts en **or mat** ; collerette à 1,2.
22. **→ La herse (grille) passe à 3 de lumière pendant l'alerte** : c'est le seul vrai signal de danger, il garde sa force. Mais au repos (1), elle luit encore un peu : à 0,5, elle se fondrait dans le sol.
23. **→ Les montants et la traverse des marteaux sont des pavés de bois.** Des rondins (cylindres) iraient mieux avec les piliers ronds.
24. **→ Les clous des moulinets sont des cubes tournés de 45°** : de près, des diamants. Des demi-sphères.
25. **→ Les murs de la capacité Mur sont huit pavés gris foncé** (0,34) : plus sombres que tout le reste du décor crème. Pierre claire du château.
26. **→ La mine est un disque noir et une rune orange (2)** : lisible, mais c'est la seule rune orange du jeu à cette force.

## III. La cohérence (un même style partout)

27. **✔ Les refus sans signe** : quand l'Échange ou le Grappin ne pouvaient pas passer la muraille, rien ne le disait. → Les **étincelles d'or du sceau**, la même couleur que le renvoi des airs : le même langage pour la même règle.
28. **→ Trois ors différents dans le code** : `Tower.Gold` (1 ; 0,8 ; 0,4), `Castle.GoldTrim` (1 ; 0,8 ; 0,32), l'or de la Couronne (1 ; 0,86 ; 0,35), sans compter `Wings.Gold`. Un seul or, dans `Palette`.
29. **→ Trois pierres différentes** : la tour (0,80 ; 0,73 ; 0,62), le château (0,82 ; 0,75 ; 0,64), les couloirs (0,78 ; 0,71 ; 0,60). Presque pareilles — donc des raccords qui se voient au pied de la tour. Une seule pierre.
30. **→ Deux styles d'obstacles** : les obstacles de la tour (pendules, béliers) sont « pierre et runes », ceux des couloirs « bois et fer ». Choisir, ou assumer (la tour magique, les couloirs artisanaux).
31. **→ Les piliers ronds des couloirs et des Monuments côtoient les tours carrées des portes.** Le château a des tours rondes aux coins et carrées aux portes : c'est historique, mais le regard hésite.
32. **→ Les gargouilles rondes (v12) et les obstacles anguleux** ne sont pas de la même famille. Le haricot, les gargouilles, la Couronne sont « jouet » ; le château est « maquette ». Le Castle Kit de Kenney est « jouet » : il réglerait l'écart.
33. **→ Les torches des couloirs** (cylindre et sphère, v12) brûlent en plein jour à 2,2. Un feu se voit au soleil couchant, mais c'est la seule lumière vive des couloirs. 1,5.
34. **→ Les fanions des plateformes luisent à 0,9** et leurs anneaux à 1,2 : ce sont des repères (ta couleur), ils peuvent rester — mais ils sont plus vifs que la tour.

## IV. Le personnage, les bots

35. **→ Le haricot n'a pas encore de modèle** : `Resources/Modeles/Personnage` est vide. Tant qu'il est vide, tout le monde est un haricot procédural. La vraie marche en avant est là (`docs/MODELES.md`).
36. **→ Les haricots ont tous la même silhouette** : seule la couleur change. Un casque, une plume, une cape différente par place (sept variantes) aideraient à reconnaître un rival de loin.
37. **→ La cape est une plaque rigide** qui ne flotte pas en vol. Un « Cloth » Unity serait lourd ; deux segments articulés qui traînent derrière suffiraient.
38. **→ Les ailes sont les mêmes pour tous** ; les « ailes d'or » ne changent que la couleur. Une forme différente (plus longues) dirait « plus rapides ».
39. **→ En première personne, on ne voit rien de soi** (règle du 22/09) — même pas ses ailes quand on plane. Voir le bout des ailes au bord de l'écran donnerait la sensation de voler. **À demander à Martin** (la règle dit : rien sans qu'il le demande).
40. **→ La danse de victoire a six figures** : au bout de trois matchs, on les connaît. Le modèle Quaternius apporterait ses propres danses.
41. **→ Les yeux des haricots ne clignent pas.** Un clignement toutes les 3-5 s (réduire la pupille à un trait) : dix lignes de code, beaucoup de vie.
42. **→ Les joues roses restent roses quand on est étourdi.** Des yeux en croix pendant l'étourdissement (0,2-0,7 s) : un classique de Fall Guys.

## V. Les effets

43. **✔ La secousse de l'écran était du bruit blanc** (un tirage par image). → Un bruit doux ; le choc se sent au lieu de grésiller.
44. **→ Toutes les capacités font « éclair + anneau + gerbe »** : une grammaire commune, c'est bien, mais le Clignement et le Rappel se ressemblent trop (même couleur verte, même éclair). Le Rappel pourrait laisser une **trace fantôme** de son chemin.
45. **→ La Nuée est un nuage gris uniforme** : il cache, mais il ne tourbillonne pas.
46. **→ Le Voile n'a pas de silhouette visible** pour les autres : on disparaît. Une ondulation (le fond déformé) serait plus « magique », mais demande un shader.
47. **→ Les impacts des obstacles (`Fx.ObstacleHit`) et ceux des joueurs (`Fx.Impact`)** sont presque les mêmes. Un obstacle devrait faire « bois/fer » (des éclats), un coup « magie » (des étincelles).
48. **→ L'onde de choc (shader `Fief/Onde`)** est belle de près ; de loin, elle disparaît (additif sur un ciel clair). Une ombre au sol (un disque sombre qui s'étale) la rendrait lisible de haut.
49. **→ Les confettis de victoire sont des carrés** : des rectangles fins qui tournent (effet papier) seraient plus vrais.
50. **→ Le feu des gargouilles** est un trait + une sphère : il manque la fumée qui reste une seconde après.
51. **→ Pas de traînée derrière un joueur éjecté** de la tour, alors qu'il vole 20 m en saltos : un petit sillage de poussière (celui de la Ruée) dirait « c'est un obstacle qui t'a envoyé ».

## VI. L'écran (HUD)

52. **✔ Les astuces avaient disparu** avec le « zéro texte ». → **Des astuces en icônes** : une pastille sous le viseur, les touches puis ce qu'elles font, une fois par match, 5 s, en fondu.
53. **✔ Le verrou de la Couronne était invisible.** → Une croix rouge sur la Couronne du HUD.
54. **→ La pastille d'astuce et l'invite « E » se superposent** si l'on est près d'une arbaleste pendant l'astuce de la plateforme (70 px et 150 px sous le viseur : elles tiennent, de justesse, en 1080p ; en 720p, à vérifier).
55. **→ Le chrono est bleu roi, la Couronne violette, les scores à la couleur des joueurs** : trois bleus-violets proches en haut de l'écran. Le chrono en blanc sur fond sombre.
56. **→ La jauge de la tour, à gauche**, n'apparaît que sur la tour : elle surgit sans transition. Un fondu de 0,3 s.
57. **→ Le viseur est le même pour pousser et pour viser une capacité.** Il pourrait s'ouvrir quand un joueur est à portée de poussée (3,2 m).
58. **→ Les pseudos au-dessus des têtes** gardent leur taille de 22 m à 3 m (quelques tailles fixes) : de près, ils masquent le visage. Plus petits de très près.
59. **→ Aucun indicateur hors-écran pour le porteur** (voulu : « pas de marqueur ») — la colonne dorée de la Couronne suffit, sauf quand le porteur est derrière toi.
60. **→ La barre du sacre à l'écran de tous** est une pastille qui se remplit : c'est LE moment de la manche, il mérite plus (l'écran qui s'assombrit un peu sur les bords, la cloche plus forte à chaque seconde).
61. **→ Le compte à rebours de départ** (3-2-1, puis un triangle « jouer » qui éclate en or) : pas de son montant sur 3-2-1 qui prépare le départ, comme dans Fall Guys.
62. **→ Le fil des événements (icônes) garde cinq lignes, 5 s chacune** (`Toasts`) : en bagarre à huit, il défile trop vite pour être lu. Regrouper (« ×3 ») les mêmes événements.

## VII. Les menus et les cartes

63. **→ L'écran-titre montre l'île de loin, en tournant** : très beau, mais le logo « FIEF » est en police Titan One sans traitement. Un logo dessiné (couronne sur le I) — le frère de Martin ?
64. **→ Les boutons ronds jaunissent quand on les vise** : pas de son de survol. Un « tic » doux.
65. **→ Le salon** : les pastilles des joueurs à leur couleur, mais les bots ont tous l'icône « bot ». Leur **haricot en miniature** (un rendu 3D dans l'interface) serait plus vivant.
66. **→ Les cartes de capacités** ont toutes le même cadre d'or. Les passives et les actives pourraient avoir deux cadres (argent / or) : on sait tout de suite quel tour de table on joue.
67. **→ La carte retournée montre l'icône, le nom, la phrase** : la phrase reste du texte (autorisé, « les noms des capacités sur les cartes »), mais elle fait souvent deux lignes. Un GIF de la capacité (Phase 5).
68. **→ Le podium de fin de match** : trois marches, les pseudos. Les haricots dessus, qui dansent (le vainqueur) ou boudent (les autres).
69. **→ La pause fige le jeu (`Time.timeScale = 0`)** mais la caméra, elle, vit sur le temps réel : une secousse en cours continue de s'amortir derrière le menu. Détail.
70. **→ Les réglages** gardent leurs mots (« Sensibilité », « Champ de vision »…) à côté des icônes : c'est permis (« les mots des boutons de menu »), mais c'est l'écran le plus bavard du jeu.
71. **→ Pas d'aperçu du champ de vision** quand on le règle (le menu cache le jeu).

## VIII. Le ciel, l'île, la lumière

72. **→ La mer de nuages est plate** : de haut, c'est une nappe. Quelques gros cumulus qui dépassent (sphères aplaties, blanches, mates) donneraient la profondeur.
73. **→ Le soleil ne bouge pas pendant la manche** (fin de journée fixe). Le faire descendre de 5° sur 10 min : les ombres s'allongent, la tension monte.
74. **→ Les îlots flottants se ressemblent** : même roche, même herbe. Un arbre (Kenney Nature Kit), un rocher, une ruine par îlot : on les reconnaît.
75. **→ Les cascades tombent dans le vide sans fin** : elles pourraient se perdre dans une brume au niveau des nuages.
76. **→ Les rochers qui flottent ne tournent pas.** Une rotation très lente (1°/s) ferait vivre le ciel.
77. **→ La brume est la même partout** ; au sommet de la tour (100 m), on devrait voir plus loin et plus clair.
78. **→ Pas d'oiseaux.** Trois silhouettes qui tournent autour de la tour (des triangles, pas de modèle) : le ciel a une échelle.
79. **→ La lanterne portée (discrète)** ne sert plus à rien en plein jour. À retirer, ou à garder pour une manche au crépuscule (Phase 2).

## IX. Le son et la musique

80. **→ Tous les obstacles font « clang » ou « crash »** : le pendule, le bélier, le moulinet, le marteau ont le même son. Un son par obstacle (Kenney Impact Sounds, CC0).
81. **→ Les boulets ne grondent pas en roulant** (le commentaire dit « elle gronde », le code ne joue rien pendant la descente). Un grondement 3D qui monte quand il approche.
82. **→ La volée de quatre boulets (v13)** part sans signal : une corne au sommet, deux secondes avant, préviendrait tout le monde.
83. **→ La musique « Calm » tourne en boucle** pendant toute la montée (4 à 10 min). Une deuxième couche qui entre quand quelqu'un approche du sommet.
84. **→ La cloche du sacre est la même que celle des Monuments** : une cloche plus grave pour la troisième seconde.
85. **→ Pas de son d'ambiance d'altitude** (le vent qui siffle plus fort en haut de la tour).
86. **→ La voix des bots** (ils ne parlent pas, « une voix ») : ils ont tous la même. Une hauteur différente par bot.

## X. La dette qui finit par se voir

87. **✔ Le code des trous et des courants de rampe** fabriquait encore des barres rouges « Bord du trou »… pour zéro trou. → Retiré, avec la classe `Updraft`.
88. **✔ `FloatingTexts`** (les « +1000 AURA ») tournait encore à chaque image, pour rien. → Retiré.
89. **✔ Des commentaires d'une autre époque** (la forêt, le mendiant, la touche V, les coffres, 45 s, les Yeux). → Remis au goût du jour.
90. **✔ Trois documentations en double** sur le même membre. → Une seule.
91. **✔ `Sweeper` calculait `reach = wiper ? length : length`.** → `reach = length`.
92. **→ `UiStyle` garde les styles « manuscrit » du 26/09** (cuir, bronze) que plus rien n'appelle. À vider.
93. **→ Les couleurs en dur** (voir 28-29) : 215 `new Color(...)` dans les seuls scripts du monde (`World/`).
94. **→ Chaque obstacle construit ses matières à la main** (`GetGlow(new Color(1f, 0.45f, 0.2f), 1.3f)` écrit cinq fois). Une petite fonction `Palette.Ember(force)`.
95. **→ Le nom des objets dans la hiérarchie Unity** est tantôt français accentué (« Chapiteau »), tantôt majuscule (« BOULET »), tantôt anglais (« Cylinder » par défaut). Pour le frère de Martin qui ouvrira la scène : une convention.
96. **→ Pas d'écran de chargement** : la manche se construit en une image (la console donne le temps : « construite en … ms »). Le fondu au noir couvre la saccade ; une Couronne qui tourne serait plus pro.

## XI. Le chemin

97. **→ Le Castle Kit de Kenney** reste la meilleure marche en avant visuelle : tours, murs, toits, portes, tous du même style « jouet ». Il suffit de le mettre dans `Resources/Modeles/Chateau` (voir `docs/MODELES.md`).
98. **→ Le personnage Quaternius**, même chose : un seul fichier, `Personnage.fbx`.
99. **→ Une passe « lumière »** avec une vraie capture d'écran : le designer chiant a relu les nombres, il n'a pas vu l'image. Une capture de chaque lieu (plateforme, couloir, rampe, sommet, îlot) envoyée à Claude vaudrait une liste entière.
100. **→ Le frère de Martin** pourrait prendre la liste III (cohérence) : choisir **un** or, **une** pierre, **un** bois, dans des captures, et les donner à Claude en couleurs (un compte-gouttes suffit).
