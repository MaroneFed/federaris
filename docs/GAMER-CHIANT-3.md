# Le gamer chiant, cinquième passage : 200 reproches

*02/10/2026, le soir. Martin : « Fais un exercice de gamer chiant, 200, pendant que je
cherche ta musique. » Passages précédents : `docs/100-RAISONS.md`, `docs/100-PROBLEMES.md`,
`docs/GAMER-CHIANT.md` (v12), `docs/GAMER-CHIANT-2.md` (v13).*

Le gamer chiant a joué la v16-v17 : commandes en liste, arbaleste fluide, les vrais sons
Kenney, la voix de l'arène. Il part de ce que Martin et son frère ont vu en vrai (« les
sons sont horribles », « des fois on ne voit plus les touches, des fois tout se barre »,
« une ampoule et un haut-parleur quand on regarde en bas ») puis il fait le tour du jeu,
manche après manche. Claude ne peut pas lancer Unity : les chiffres viennent du code, les
impressions sont celles d'un joueur qu'on imagine exigeant — **à confirmer en jeu**.

Légende : **✔** corrigé (v16, v17 ou v17.1, dans ce passage) · **→** reste à faire, avec la
piste · **?** à vérifier en jouant (Claude ne le voit pas).

---

## I. Ce qui casse l'écran (1-20)

1. **✔ « Des fois tout se barre, on ne voit plus rien. »** Tout le HUD était dessiné d'un seul bloc : si un morceau plantait (un joueur qui disparaît entre deux images…), tout ce qui venait après disparaissait, et s'il plantait en pleine transparence, il laissait la couleur du GUI à zéro — plus rien ne se voyait. → Chaque morceau est dessiné à part (`Hud.Part`) ; la couleur et le zoom sont remis à chaque fois ; l'erreur va **une fois** dans la Console.
2. **✔ Pareil pour les menus** (pause, choix, fin de manche) : un écran qui plante ne laisse plus rien de travers.
3. **? « Des fois on ne voit plus les touches »** : si c'était le plantage ci-dessus, c'est réglé ; sinon, **copie-moi la ligne rouge de la Console** (elle commence par `[FIEF] HUD :`).
4. **→ L'ampoule et le haut-parleur quand on regarde en bas** : ce ne sont pas des objets du jeu, ce sont les **Gizmos** d'Unity (la lanterne = une lumière, les sons = une source audio). Fenêtre **Game**, en haut à droite : décoche **Gizmos**. Dans le jeu exporté, ils n'existent pas.
5. **✔ Le flou partout** : la fenêtre Game en *Free Aspect* étirait l'image. → Une résolution fixe (Full HD), noté dans le README.
6. **✔ Réglages sortait par le bas de l'écran** (il partait du milieu moins 170 px pour 650 px de haut). → Centré sur sa vraie hauteur.
7. **✔ Nouveau match aussi**, centré.
8. **✔ La danse du vainqueur était cachée** par un voile noir à 86 % dès 1,3 s. → Plus de voile en fin de manche.
9. **✔ On passait la fête en 1,8 s** sans l'avoir vue. → Le bouton n'arrive qu'à 3 s.
10. **✔ La liste des Commandes ne suivait pas la souris** en jeu (Unity n'envoie pas de « mouvement de souris » hors de l'éditeur). → Elle suit la souris comme les autres menus, avec le petit son de survol.
11. **✔ Changer sa touche de capacité, il fallait viser les flèches.** → Un clic sur la touche elle-même passe à la suivante.
12. **→ La barre de défilement des Commandes ne se tire pas à la souris** (molette et flèches seulement).
13. **→ Le repère de la Couronne et celui de l'arbaleste peuvent se chevaucher** au bord de l'écran : les empiler.
14. **→ Le fil des événements (5 lignes, 5 s) déborde** quand huit joueurs se poussent en même temps : grouper « X a poussé Y » répétés.
15. **→ Les pseudos au-dessus des têtes se superposent** dans la mêlée au pied des rampes (le code essaie trois places, pas plus).
16. **→ La vitesse de vol** s'affichait en chiffre nu. **✔** « km/h » ajouté.
17. **→ Pas de réglage de l'opacité du HUD** pour qui veut filmer.
18. **→ Le cadre d'or du porteur** est plein écran : sur un petit écran, il mange les bords (à régler en jeu).
19. **? La croix rouge sur la Couronne pendant le verrou** : se voit-elle assez ?
20. **→ Aucune confirmation quand on quitte un match** au clavier par Échap-Entrée trop vite (il y en a une au clic).

## II. Les sons (21-40)

21. **✔ « Quand tu choisis une carte, on dirait que c'est nul. »** Le jingle (Kenney PIZZI07) **descendait** — analysé : −2,5 demi-tons du début à la fin ; l'oreille entend « raté ». → Un jingle qui **monte** (PIZZI04, +9 demi-tons).
22. **✔ « Quand t'as la Couronne, pareil. »** Même jingle. → PIZZI02 (+8 demi-tons, 1 s).
23. **✔ Perdre la Couronne faisait le même bip que « refusé »**. → Un jingle qui descend (PIZZI05).
24. **✔ Le don d'un sanctuaire** jouait le son de la Couronne. → Le son de la carte.
25. **✔ La voix de l'arène était trop forte** (−13,7 dB de moyenne, jouée à 0,9). → 0,7.
26. **✔ Le « bam » des chocs** revenait sans cesse, trop fort. → Baissé.
27. **✔ Le coup de poing** couvrait tout. → Baissé.
28. **→ Les pas sur l'herbe** sont deux fois plus longs que ceux sur la pierre (0,68 s contre 0,11 s) : on entend un froissement qui traîne. À couper court.
29. **→ Le treuil de l'arbaleste** (un grincement de porte) : pas sûr qu'on reconnaisse un treuil. À écouter.
30. **→ Pas de son quand on atterrit après un vol** (on garde le « bam » générique).
31. **→ Pas de son propre pour le piqué d'aigle** (un sifflement).
32. **→ Les obstacles n'ont pas de son** tant qu'ils ne touchent personne (un pendule qui passe devrait souffler).
33. **→ Les gargouilles** : un grondement de pierre quand elles te repèrent, avant l'alarme.
34. **→ Les sons ne sont pas en 3D** : un fracas à 80 m sonne comme à côté.
35. **→ Le volume de la musique n'est pas séparé** de celui des effets.
36. **→ La voix « you lose »** à chaque manche perdue, c'est dur quand on en perd sept. Piste : seulement à la dernière.
37. **→ La voix est en anglais** dans un jeu tout en français. Voulu pour l'ambiance arcade ; à Martin de dire.
38. **→ Les jingles Kenney** vont se répéter ; en garder deux ou trois par moment et les tirer au hasard.
39. **→ Le survol des boutons** fait un petit son à chaque bouton : agaçant si on promène la souris.
40. **→ Les musiques Suno** arrivent : penser à leur volume par rapport aux sons (la danse est poussée à ×1,35).

## III. Le départ (41-60)

41. **✔ Un drapeau devant les yeux sur l'arbaleste.** → Retiré.
42. **✔ On ne voyait pas où l'arbaleste allait nous poser.** → Un repère à l'écran sur l'arrivée, collé au bord s'il sort.
43. **✔ L'arbaleste sautait d'un angle à l'autre** quand on bougeait la souris. → Elle suit en douceur.
44. **✔ Le fût bouchait le milieu de l'écran.** → L'estrade est plus haute : on voit par-dessus.
45. **→ Les deux joueurs d'une même porte** atterrissent au même endroit et se poussent aussitôt.
46. **→ La protection de 3 s au départ** est invisible pour les autres (une bulle pâle aiderait).
47. **→ Le couloir piégé** : pas d'indication qu'il y a quatre stations, ni où il finit.
48. **→ Le sceau renvoie** qui arrive par les airs, sans expliquer pourquoi (une icône « porte » au moment du rebond).
49. **→ Le compte à rebours** : on peut regarder autour mais pas se retourner vers sa porte d'un coup (pas de raccourci).
50. **→ À la manche 2, on ne sait plus par où on est passé** : un rappel de sa porte (sa couleur) au compte à rebours.
51. **→ Sauter de la plateforme** au lieu de prendre l'arbaleste : on plane jusqu'où ? Rien ne le dit.
52. **→ Tomber de la plateforme au départ** te remet… sur la plateforme, sans pénalité : c'est bien, mais rien ne le dit non plus.
53. **→ La plateforme est à 42 m** : de là-haut, les bots en l'air se confondent avec les oiseaux (il n'y en a pas, mais les fanions bougent).
54. **→ Le pont-levis** est large ; le couloir derrière est étroit : bouchon à quatre.
55. **? Les chicanes du couloir** : un joueur qui recule depuis la cour peut-il bloquer ?
56. **→ Le premier obstacle d'un couloir** arrive tout de suite après la porte : on n'a pas le temps de lire.
57. **→ Le couloir est le même pour les quatre portes** (équité, v13) : au bout de trois manches on le connaît par cœur. Voulu ?
58. **→ La colonne de lumière de ta plateforme** ne se voit plus quand on est dans la cour (elle est dehors).
59. **→ Pas de moyen de revenir sur sa plateforme** exprès (utile pour reprendre l'arbaleste).
60. **→ Les arbalestes de l'île** ne sont pas marquées quand on les cherche (une petite icône à 30 m).

## IV. La tour (61-95)

61. **→ Manche 1 : sept obstacles par rampe** (2 pendules, 2 béliers, 2 balayeurs, 1 herse) plus deux gargouilles : pour un débutant, c'est déjà beaucoup. Piste : 5 à la première manche.
62. **→ Manche 8 : quatorze par rampe.** La difficulté monte pour **tout le monde**, y compris celui qui n'a jamais gagné.
63. **→ Pas de point de reprise** : éjecté au 90e mètre, on repart du pied. C'est le cœur du jeu (Fall Guys), mais un **palier à mi-hauteur** (où l'on réapparaît au sol) rendrait moins cruel.
64. **→ On ne voit pas l'obstacle suivant** dans la courbe de la rampe : le fût cache tout ce qui est à plus de 20 m.
65. **→ Les bandes ambre « danger »** sont peintes sous les pendules, pas sous les balayeurs.
66. **✔ Les pendules « boulets à une liane »** : refaits en butoirs rouges à bras rigide (v16).
67. **→ Le bélier** frappe sans prévenir quand on arrive de dos (il recule avant, mais on ne le voit pas reculer depuis l'arrière).
68. **→ La herse** : on ne sait pas si elle pique quand elle est à moitié sortie (oui, v13).
69. **→ Le balayeur** se saute ; le pendule non ; le bélier se contourne : trois règles à apprendre, aucune n'est montrée.
70. **→ Les boulets en volée** tombent toutes les 34 s : le premier arrive sans son d'annonce.
71. **→ Aucun son ni lumière** ne signale qu'une volée de boulets arrive.
72. **→ Éjecté de la tour, on tombe « ailes fermées jusqu'au sol »** : 7 s de chute sans rien faire.
73. **→ On retombe dans la cour, parfois sur le toit d'une tour d'angle** (on glisse, on retombe).
74. **→ La rampe n'a pas de parapet** : c'est voulu (le vide où l'on pousse), mais dans les virages serrés, la caméra en 1re personne donne le vertige.
75. **→ Les quatre rampes s'entrelacent** : de la rampe 2 on voit un joueur de la rampe 3 juste au-dessus, on croit pouvoir sauter dessus.
76. **? Peut-on sauter d'une rampe à celle du dessus ?** Si oui, c'est un raccourci énorme.
77. **→ La jauge de la tour** (à droite) ne dit pas où sont les autres.
78. **→ Le sommet** : la Couronne sur son socle est au centre, les quatre arrivées aux bords — le premier arrivé la prend en 2 s, les autres arrivent pour rien.
79. **→ Au sommet, pas de repère des Monuments** tant qu'on n'a pas la Couronne : on saute sans savoir où aller.
80. **✔ Les bornes d'arrivée** à la couleur de ta porte (v15).
81. **→ Le seuil rond au pied des rampes** (v15) : visuel seulement, les pieds s'y enfoncent de 13 cm.
82. **→ Les colonnes au pied des rampes** ont un collider : un joueur qui longe le mur s'y cogne (0,3 m).
83. **→ Les lanternes du fût** (une tous les 14 pas) ne servent qu'à décorer.
84. **→ Pas de variété visuelle** d'une manche à l'autre dans la tour (même lumière, mêmes couleurs).
85. **→ Les obstacles tirés au hasard** peuvent tomber collés : deux pendules à 4 m l'un de l'autre.
86. **→ Le temps moyen pour monter** : sans pépin, ~210 m de rampe à 10,8 m/s = 20 s ; avec les attentes et une chute, 2 à 3 min. Sur une manche de 6 min, c'est la moitié du temps.
87. **→ Les bots attendent devant un pendule** (v15) : bien, mais ils bouchent la rampe pour toi.
88. **→ Pas de moyen de dépasser** un bot qui attend (la rampe fait 6,5 m, lui est au milieu).
89. **→ Les gargouilles du fût** tirent vers l'extérieur : un tir raté fait une explosion dans le vide, spectaculaire mais inutile.
90. **→ La rampe est grise** dans la lumière du soir : les bandes de couleur sont au sol, peu visibles de près.
91. **→ Pas de « tu montes » sonore** (une note qui monte avec la hauteur).
92. **→ La Couronne sur son socle** se prend « en passant » : on la prend parfois sans le vouloir en visant autre chose. Voulu.
93. **→ Au sommet, la chute** vers la cour fait 100 m : ailes d'or, on plane loin — mais les bots restent en haut à se pousser.
94. **→ Le sommet n'a qu'un cercle de runes** : pas de protection contre la poussée pendant qu'on prend la Couronne.
95. **→ La lumière du sommet** (une lampe de 22 m) ne se voit pas en plein jour.

## V. Les gargouilles (96-110)

96. **✔ « Trois tirs sur lui qui font bam »** : une gargouille à la fois, 9 s de répit (v15).
97. **→ Le cône de lumière** n'apparaît que quand elle t'a vu (v15) : avant, on ne sait pas où elle regarde.
98. **→ Ambre / orange / rouge** : les yeux sont plus grands (v15), mais de loin, ambre et orange se confondent.
99. **→ La cible rouge au sol** est un simple cercle de ligne : peu visible sur la pierre claire.
100. **→ La dernière demi-seconde** (elle ne suit plus) n'est pas marquée : un flash, un son sec.
101. **→ Leur tir t'étourdit 0,6 s** puis te projette 34 m/s : on ne sait pas qui t'a eu (le fil dit « chute »).
102. **→ Les gargouilles des portes** tirent dans la cour, sur ceux qui descendent des rampes.
103. **→ Une gargouille tire aussi sur les bots** : on les voit se faire éjecter, c'est drôle — mais rien ne le montre dans le fil.
104. **→ Le Voile te cache** des gargouilles, la Nuée les aveugle : ça ne se voit pas sur leur tête (pas d'yeux qui se ferment).
105. **→ Pas de son quand une gargouille abandonne** (répit, cible protégée).
106. **→ Les gargouilles tirent sur le porteur jusqu'à 45 m** en dehors de la citadelle : au décollage, on se fait abattre en vol.
107. **→ Un tir de gargouille sur le porteur** fait tomber la Couronne… dans la cour, où elle reste (v15) : tout le monde redescend. Voulu ?
108. **→ Elles sont en pierre ardoise** sur des consoles crème : bien. Mais celles des tours d'angle sont dans l'ombre des toits.
109. **→ On ne peut rien leur faire.** Piste Phase 2 : les assommer (une poussée les fait taire 10 s).
110. **→ Seize gargouilles** mais seulement deux par rampe : la rampe du milieu de journée (celle au soleil) est plus facile à lire.

## VI. La Couronne et le sacre (111-135)

111. **✔ Elle reste là où elle tombe** (v15), plus de retour au sommet.
112. **✔ On ne voyait pas quand elle tombait.** → Une gerbe d'or qui monte, un anneau au sol, le repère qui grossit et bat 3 s (v16).
113. **✔ La prendre en marchant dessus** : zone élargie (2,3 m).
114. **✔ Le fil disait « Couronne ↑ »** quand le porteur tombait dans les nuages (reste de l'ancien retour au sommet). → « chute, Couronne ».
115. **✔ Le verrou de 3 s** après une perte : on passait dessus, rien ne se passait, on ne comprenait pas (la croix rouge est en haut de l'écran). → Le repère de la Couronne passe au **gris** pendant ton verrou.
116. **→ Le vol au contact** (piqué d'aigle) : la cible fait 2,4 m, on la rate souvent à 48 m/s. À mesurer.
117. **→ Le porteur plane à 11 m/s et tombe à 8 m/s** : du sommet (100 m), il fait ~140 m avant de toucher. Les Monuments sont à 150-200 m : il faut toujours un courant d'air. Voulu (30/09), mais personne ne le sait.
118. **→ Les courants d'air** ne sont pas signalés au porteur (il voit les Monuments, pas les courants).
119. **→ Le sacre de 3 s** : une barre pour tous. Mais le porteur ne voit pas qui fonce sur lui pendant ces 3 s.
120. **→ Sortir du cercle d'un pas** fait retomber le sacre de 2/s : on perd tout en une seconde et demie.
121. **→ Deux joueurs dans le cercle** : seul le porteur compte, l'autre peut le pousser — mais il est sacré s'il reste dedans malgré la poussée ? (Le vol le fait sortir.)
122. **→ La Couronne au-dessus de la tête du porteur** : quand il est derrière un mur, on ne le voit pas (le repère, si).
123. **→ Porter la Couronne ralentit de 15 %** : pas affiché.
124. **→ Le porteur ne peut pas pousser** : il ne peut que fuir. Montrer la main barrée dans son rond (c'est fait si « Mains prises »).
125. **→ La Prise ferme** (le premier coup ne la fait pas lâcher) : aucun effet visible quand elle sert (un petit éclat).
126. **→ L'Aimant (8 m)** attire la Couronne au sol : invisible pour les autres, on croit à un bug.
127. **→ La Couronne tombée dans un couloir** peut être prise par un bot qui n'a jamais monté la tour : c'est du vol à l'étalage. Voulu.
128. **→ Une Couronne tombée sur un îlot sans arbaleste** : il faut planer jusqu'à elle. Les bots savent-ils ? (ReachTo à 170 m.)
129. **? Le filet « sous l'île »** : si elle passe sous l'île, elle rentre au socle. Le reste du temps, jamais.
130. **→ Le porteur qui se déconnecte** (Phase 3) : la Couronne tombe où il est.
131. **→ Les trois Monuments** changent à chaque manche : on les apprend, puis ils bougent.
132. **→ Le Monument le plus proche** du sommet est parfois à 150 m, parfois à 250 m selon la manche : injuste d'une manche à l'autre (mais le même pour tous dans la manche).
133. **→ La colonne bleue d'un Monument** et celle, dorée, de la Couronne se croisent en vol : on confond.
134. **→ Le repère des Monuments** n'apparaît que pour le porteur : les chasseurs ne savent pas où l'attendre.
135. **→ Aucune fanfare pour les autres** quand quelqu'un prend la Couronne (juste le fil).

## VII. Le vol (136-150)

136. **→ Les ailes s'ouvrent seules à 6 m** : au bord d'une rampe, un simple saut les ouvre et on part dans le vide sans l'avoir voulu.
137. **→ Replier les ailes (Espace)** puis les rouvrir : le même bouton, on se trompe en panique.
138. **→ « On vole où l'on regarde »** : pour regarder derrière soi (un poursuivant), on fait demi-tour.
139. **→ Pas d'indicateur d'altitude** : on ne sait pas si on arrivera au Monument.
140. **→ La vitesse** s'affiche en km/h, mais « trop lent » n'est pas signalé (en dessous de 7 m/s on décroche).
141. **→ Un mur en face coupe la vitesse de moitié** : contre une tour, on s'arrête net et on tombe.
142. **→ Les ailes d'or** (au sommet, à l'arbaleste) ne se distinguent pas assez des ailes normales en 1re personne.
143. **→ Le piqué d'aigle** se déclenche sur la touche pousser en l'air : on le déclenche en voulant pousser un voisin à l'atterrissage.
144. **→ Le cône du piqué (30°, 45 m)** : pas de viseur qui dit « il est dans le cône ».
145. **→ Les courants d'air** montent jusqu'à 80 m : on tourne dedans, mais la caméra tourne avec, on a le tournis.
146. **→ Atterrir sur un îlot** : pas de ralenti à l'approche, on glisse et on tombe de l'autre côté.
147. **→ Le vent qui siffle** en chute libre ne joue qu'une fois par chute.
148. **→ Le champ de vision s'ouvre** avec la vitesse : au-delà de 100° réglés, c'est trop.
149. **→ Planer sous la mer de nuages** : on ne voit plus rien avant de réapparaître (quelques secondes perdues).
150. **→ Les bots planent mieux que toi** (ils visent le point exact) : frustrant pour intercepter.

## VIII. Pousser et se battre (151-165)

151. **→ La poussée envoie à 15 m** : sur une rampe, c'est une éjection garantie. Les joueurs vont passer leur temps à se pousser au lieu de monter.
152. **→ Pousser dans le vide ne coûte rien** (0,9 s de recharge) : on spamme le clic droit.
153. **→ Le poussé ne sait pas qui l'a poussé** (le fil le dit, mais on regarde ailleurs).
154. **→ Pas de riposte possible en l'air** (sauf piqué sur le porteur).
155. **→ La protection après respawn (3 s)** s'annule-t-elle si on pousse ? (Non : on peut pousser protégé.) Abusable.
156. **→ L'Ancrage** (moitié moins loin) : ça ne se voit pas sur le joueur ancré.
157. **→ La Poigne** : la carte ne disait pas que la poussée revient aussi plus vite. **✔** Corrigé.
158. **→ Deux joueurs qui se poussent en même temps** : le premier dans la boucle gagne (ordre du tableau).
159. **→ La poussée ignore la hauteur jusqu'à 2,2 m** : on pousse quelqu'un sur la marche du dessus.
160. **→ Le coup de poing sonore** (v17) joue aussi quand un obstacle te frappe : on croit qu'un joueur t'a eu.
161. **→ Les saltos du poussé** : spectaculaires en 3e personne, invisibles pour lui (1re personne).
162. **→ Pas de « combo »** : pousser trois joueurs d'affilée ne donne rien.
163. **→ Le HitStop (0,07 s)** sur chaque poussée : à haute fréquence, l'image saccade.
164. **→ Pousser un bot dans la cour** ne sert à rien (il revient en 5 s).
165. **→ Pas de blocage / parade** : Smash en a une. Phase 2.

## IX. Les capacités (166-180)

166. **→ Les cartes sont tirées au hasard** : on peut n'avoir que des passives faibles trois manches de suite.
167. **→ Le vainqueur choisit en dernier** : il récupère toujours la moins bonne carte. Bien. Mais le dernier du classement n'a aucun bonus.
168. **→ La visée maintenue** (grappin, crochet…) : l'aperçu est gris quand il n'y a rien à viser — on ne sait pas si c'est trop loin ou bloqué.
169. **→ Le Souffle (9 s)** reste la plus forte capacité (v13, non corrigé).
170. **→ Le Mur** (12 s) bloque une rampe de 6,5 m entière : un joueur peut fermer la rampe derrière lui.
171. **→ La Mine** posée au pied d'une rampe : tout le monde y passe.
172. **→ Le Voile (invisibilité)** : les bots te retrouvent-ils ? (Ils perdent la trace, v13.)
173. **→ Le Clignement** à travers le sceau ? (Refusé pour l'Échange, à vérifier pour le Clignement.)
174. **→ Le Rappel** ramène 4 s en arrière : utile pour annuler une éjection, mais rien ne le suggère.
175. **→ Le Double saut** en haut d'une rampe peut sauter sur la rampe du dessus (voir 76).
176. **→ La Recharge** (−33 %) ne se voit pas sur le rond.
177. **→ Le don du sanctuaire** remplace ta capacité : on perd celle qu'on a choisie sans prévenir.
178. **→ Les 26 icônes** : on ne retient pas lesquelles sont actives ou passives (les passives ont un rond plus petit, c'est tout).
179. **→ Les noms simples** sont bien ; les phrases des cartes ne sont lues qu'une fois.
180. **→ Pas d'écran « mes capacités »** en jeu (Tab montre les scores, pas les capacités).

## X. Les bots (181-190)

181. **✔ Ils montent la tour** (v15) — Martin : « les bots fonctionnent bien ».
182. **✔ Ils choisissaient leurs cartes en 0,9 s chacun** : avec sept bots, deux tours de table et quatre secondes d'attente, ~16 s entre deux manches. → 0,55 s chacun, 2,5 s à la fin (~10 s).
183. **→ Quand le porteur s'envole**, les bots qui ne gardent pas un Monument remontent tous la tour chercher des ailes d'or : la cour se vide.
184. **→ Un seul bot garde le Monument** le plus proche : le porteur va à un autre.
185. **→ Les bots n'utilisent pas les courants d'air** pour intercepter (seulement avec la Couronne).
186. **→ Les bots faciles** ne prennent jamais l'arbaleste vers la rampe (voulu) : ils arrivent toujours après.
187. **→ Les bots ne se méfient pas des mines** des autres.
188. **→ Les bots ne font pas d'emote** : un petit geste à la victoire d'un autre les rendrait vivants (le pack d'emotes Kenney est là).
189. **→ Les bots ont des pseudos médiévaux** (Mahaut, Oswin…) : bien, mais toujours les mêmes, dans le même ordre.
190. **→ En difficile, un bot gagne souvent la manche 1** avant que le joueur ait compris : proposer « normaux » par défaut (c'est le cas).

## XI. Menus, choix, fin de manche (191-200)

191. **→ Le choix des capacités** montre huit cartes pour huit joueurs : sur un écran 16:9, les cartes rapetissent.
192. **→ Pas de temps limite pour choisir** : un joueur indécis bloque (en ligne, ce sera un problème).
193. **→ « À TOI ! »** clignote, mais la carte visée n'a pas de son de survol.
194. **→ La fin de manche** : « you win / you lose » et le pseudo en or. Pas de récapitulatif (qui a pris la Couronne combien de fois).
195. **→ Le podium final** ne montre pas les haricots (proposé en v14 : Phase 2).
196. **→ Pause** : on ne peut pas mettre en pause pendant le 3-2-1.
197. **→ Le salon ne retient pas** les derniers réglages (joueurs, manches) d'un match à l'autre.
198. **→ Pas de bouton « revanche »** à la fin : il faut repasser par le salon.
199. **→ L'aide écrite** est « oui » par défaut : à Martin de trancher.
200. **→ Aucune statistique** à la fin (poussées, chutes, temps au sommet) : elles existent dans le code (`Stats`), il suffit de les montrer.

---

**Ce passage a corrigé** : le HUD et les menus qui disparaissaient (1-2), les sons de la
carte, de la Couronne prise et perdue (21-24), les volumes (25-27), la liste des Commandes
(10-11), le fil des chutes (114), le verrou visible (115), la phrase de la Poigne (157), le rythme du choix des bots
(182), l'unité de la vitesse (16) — plus ce que Martin avait signalé plus tôt dans la
journée (v16-v17 : 5-9, 41-44, 66, 111-113).

**Les plus importants à faire ensuite, pour le gamer chiant** : 61 (moins d'obstacles à la
première manche), 63 (un palier à mi-hauteur), 118 (les courants d'air montrés au porteur), 151-152 (la poussée sur les rampes),
183-184 (les bots qui interceptent). Ce sont des choix de jeu : à Martin de dire.
