# LE PLAN DES 100 — « tout de malade » (v49, 11/11)

Martin : « tu fais un plan et tu le suis ; t'as 100 trucs à améliorer ; un château de malade ;
digne d'Elden Ring et de GTA. »

**Ce qu'il faut savoir avant de lire.** Claude ne peut pas lancer Unity. Tout ce qui est marqué
**FAIT** est écrit, compilé par le compilateur C# de contrôle, mais **jamais vu à l'écran** :
c'est le test de Martin qui dira si c'est beau. Les shaders (le code qui tourne sur la carte
graphique) ne peuvent même pas être compilés ici : s'il y en a un de cassé, l'objet devient
**rose** — dans ce cas, **Réglages ▸ Graphismes ▸ Rapide** remet l'ancien rendu, et il suffit
d'envoyer la ligne rouge de la Console.

Rien de tout ça ne change une règle du jeu : mêmes murs, mêmes portes, mêmes obstacles, mêmes
bots. Seul ajout solide : le socle des statues (collé au mur, comme un contrefort).

## A. L'image (20) — FAIT en v49

1. **FAIT** Une matière maison pour tout le jeu (`Fief/Lit`) : plus aucune couleur « plate », un grain dans la teinte.
2. **FAIT** Le liseré de ciel sur les bords des formes (elles se détachent du fond).
3. **FAIT** La pierre et le sol avec relief (`Fief/LitBump`), mousse et salissure au pied des murs.
4. **FAIT** Le sol de l'île peint pixel par pixel (`Fief/Terrain`) : deux verts, herbe sèche, terre battue.
5. **FAIT** Les flancs de l'île en strates de roche, mousse sous le bord, roche qui s'assombrit vers la pointe.
6. **FAIT** De la vraie herbe : des dizaines de milliers de touffes (`World/Grass.cs`), dessinées d'un coup.
7. **FAIT** Le vent dans l'herbe (vagues lentes + frisson).
8. **FAIT** L'herbe se couche sous tes pieds.
9. **FAIT** Des prairies de fleurs (blanches, jaunes, rouges) par taches.
10. **FAIT** Un ciel peint (`Fief/Sky`) : dégradé, bande dorée à l'horizon, vrais nuages qui dérivent.
11. **FAIT** Le soleil : disque, grand halo, petit halo.
12. **FAIT** La mer de nuages en vraie surface qui ondule, éclairée par le soleil.
13. **FAIT** Le halo (bloom) : l'or, le feu, les vitraux, les sorts débordent de lumière.
14. **FAIT** Les rayons du soleil entre les tours.
15. **FAIT** L'ombre de contact (Ultra) : les coins et les pieds des murs s'assombrissent.
16. **FAIT** L'étalonnage : les blancs ne crament plus, contraste, saturation, ombres froides, lumières chaudes.
17. **FAIT** Vignettage, grain très fin, netteté, bords à peine irisés.
18. **FAIT** Réglages ▸ **Graphismes** : Rapide / Beau / Ultra (Rapide = l'ancien rendu, la roue de secours).
19. **FAIT** Les lueurs 60 % plus fortes (pour nourrir le halo), ombres jusqu'à 170 m en Ultra.
20. **FAIT** La mer de nuages suit les six ciels du Mode Dieu.

## B. Le château (20)

21. **FAIT** Quatre **échauguettes** par tour d'angle (tourelles suspendues, toit en cloche, fenêtre allumée).
22. **FAIT** Le **gothique** : chaque contrefort monte en pile et finit en **pinacle** à boule d'or.
23. **FAIT** Un **arc brisé** en claveaux devant chaque porte, clef de voûte dorée.
24. **FAIT** Un **châtelet** au-dessus de chaque porte : pignon à redents, toit d'ardoise.
25. **FAIT** La **rosace** de vitrail à la couleur de la porte, qui brille.
26. **FAIT** Deux vasques de feu sur chaque châtelet.
27. **FAIT** Les **rois-haricots** de pierre, huit mètres, couronnés d'or, deux par porte, au blason de la porte.
28. **FAIT** Le **lierre** qui grimpe sur les murailles.
29. **FAIT** Le lierre au pied des tours d'angle.
30. À faire — La tour de la Couronne : des arcs-boutants et une lanterne dorée sous le sommet.
31. À faire — Des vitraux allumés sur le fût de la tour, un par tour de rampe, à la couleur du tour.
32. À faire — Les toits : de vraies ardoises (relief en écailles) au lieu d'un bleu uni.
33. À faire — Des hourds de bois sur deux murailles (galeries couvertes en surplomb).
34. À faire — Une fontaine au centre de chaque jardin, avec de l'eau qui bouge.
35. À faire — Des bannières qui ondulent vraiment (un shader de tissu) au lieu de planches raides.
36. À faire — Les torches : de vraies flammes en particules, avec des étincelles.
37. À faire — Le dallage de la cour : des pavés en relief, usés dans les allées.
38. À faire — Les ponts-levis : chaînes à maillons, treuil, clous.
39. À faire — Des chaînes géantes qui « ancrent » l'île aux îlots.
40. À faire — Le château de nuit dans les menus (fenêtres allumées) : un vrai plan de carte postale.

## C. Le paysage (15)

41. **FAIT** Des **ruines** au bord de l'île : colonnades effondrées, tambours tombés dans l'herbe.
42. **FAIT** Des arches anciennes, certaines sans leur clef.
43. **FAIT** Des cercles de pierres levées.
44. **FAIT** Des buissons par bosquets.
45. **FAIT** Des rochers moussus dans l'herbe.
46. **FAIT** Sept **îles-ruines** au loin dans le ciel (tours brisées, grandes arches).
47. **FAIT** Deux volées d'**oiseaux** (autour du sommet, et au large).
48. À faire — Le dessous de l'île : stalactites, racines, lianes qui pendent.
49. À faire — Les cascades : un shader d'eau (reflets, écume), la brume au pied.
50. À faire — Les rivières : de l'eau qui coule vraiment, des galets.
51. À faire — Papillons et pollen près du sol, lucioles près des sanctuaires.
52. À faire — Les îlots des Monuments : chacun son ambiance (ruines, jardin, cristaux).
53. À faire — Les plateformes de départ : de vrais petits rochers habités (lanterne, fanion, herbe).
54. À faire — Des nuages en volume qui passent ENTRE les îlots.
55. À faire — Un arc-en-ciel dans la brume des cascades.

## D. Les personnages et les effets (15)

56. **FAIT** L'image « claque » quand une explosion te touche (halo qui enfle, bords irisés).
57. **FAIT** Les bords de l'image s'irisent avec la vitesse en vol.
58. À faire — Le haricot : une matière « bonbon » (reflet doux sous la peau), des yeux qui brillent.
59. À faire — Une ombre ronde douce sous chaque joueur, même à l'ombre des murs.
60. À faire — La Couronne : un shader d'or avec des éclats qui tournent, des joyaux qui luisent.
61. À faire — Les ailes : des plumes translucides à contre-jour.
62. À faire — Traînées de vitesse au sprint et en piqué.
63. À faire — Les explosions : une onde qui déforme l'image (réfraction).
64. À faire — Les Monuments : une colonne de lumière volumétrique, des runes qui s'allument pendant le sacre.
65. À faire — Les anneaux de vent : un tourbillon en shader au lieu d'un anneau plein.
66. À faire — Les sanctuaires : un cristal qui tourne, des éclats, une lumière qui pulse.
67. À faire — Les courants d'air : des filets de vent visibles de loin.
68. À faire — Les obstacles : traînée de mouvement sur les pendules et les béliers.
69. À faire — Le sceau de la citadelle : un dôme de runes qui ondule quand on le touche.
70. À faire — La poussière et les éclats de pierre quand un obstacle te frappe.

## E. La caméra et la sensation (10)

71. À faire — Flou de mouvement léger en vol et quand on est projeté.
72. À faire — Profondeur de champ pendant la danse du vainqueur et à l'écran-titre.
73. À faire — Un plan de survol du château au début de chaque manche (3 s, passable).
74. À faire — La caméra de victoire : trois plans de cinéma au lieu d'un.
75. À faire — Exposition qui s'adapte (sortir d'un couloir sombre éblouit un instant).
76. À faire — L'atterrissage : la caméra plonge selon la hauteur de chute.
77. À faire — Le vertige au bord de la tour (léger zoom quand on regarde en bas).
78. À faire — Ralenti d'une demi-seconde sur le vol de Couronne décisif (seulement chez toi).
79. À faire — Le souffle du vent qui monte avec l'altitude.
80. À faire — Mode photo (F9) : caméra libre, pause, sans HUD.

## F. Les menus et le HUD (10)

81. À faire — L'écran-titre : le château en vrai plan large, caméra qui dérive, titre doré.
82. À faire — Les boutons : relief, reflet qui passe, son à chaque survol.
83. À faire — Les cartes de capacités : une illustration animée par carte.
84. À faire — Transitions entre écrans (fondu, glissé) au lieu de coupes sèches.
85. À faire — Le salon : les haricots des joueurs alignés devant le château.
86. À faire — Le podium : un vrai podium de pierre, confettis qui restent au sol.
87. À faire — Écran de chargement : une vue du château et une barre.
88. À faire — Les pastilles du HUD : apparition animée, pas un simple affichage.
89. À faire — Les réglages graphiques détaillés (halo, ombre de contact, herbe : chacun son curseur).
90. À faire — Un compteur d'images par seconde (F3) pour régler sa qualité.

## G. Le son (5)

91. À faire — Une ambiance par lieu : vent en haut, oiseaux dehors, écho dans la cour.
92. À faire — La réverbération dans les couloirs et sous les arches.
93. À faire — Le bruit des cascades et des feux, en 3D.
94. À faire — Les pas dans l'herbe, sur la pierre, sur le bois : plus de variété.
95. À faire — La musique qui monte par couches avec la hauteur du porteur.

## H. La fluidité (5)

96. À faire — Mesurer : le temps par image de chaque effet, à l'écran (avec F3).
97. À faire — L'herbe : moins de touffes au loin (trois niveaux de détail).
98. À faire — Les ombres : ne pas en projeter pour les tout petits objets.
99. À faire — La qualité qui baisse toute seule si le jeu passe sous 50 images par seconde.
100. À faire — Le premier lancement : choisir Rapide / Beau / Ultra selon la carte graphique.

---

**Bilan v49 : 38 points sur 100 écrits.** La suite se fait dans l'ordre que Martin veut — mais
d'abord **son test** : tant que personne n'a vu les 38 premiers à l'écran, en empiler 62 autres
à l'aveugle serait une mauvaise idée (un shader raté se corrige en cinq minutes quand on a la
ligne d'erreur, pas quand il y en a trente par-dessus).
