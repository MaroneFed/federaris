# Le gamer chiant, troisième passage : 100 reproches

*01/10/2026. Martin : « fais l'exercice du gamer chiant que je t'avais déjà dit une fois,
et tu revois tout le jeu une bonne fois, pour voir s'il n'y a pas des petits bugs… avec
100 trucs ». Premier passage : `docs/100-RAISONS.md` ; deuxième : `docs/100-PROBLEMES.md`.*

Le gamer chiant a joué la v11 (icônes, château de conte) et il râle. Claude ne peut pas
lancer Unity : il a relu chaque système ligne à ligne, refait les calculs (distances,
vitesses, tailles à l'écran) et joué la manche dans sa tête. Les chiffres sont ceux du code.

Légende : **✔** corrigé dans la v12 · **→** reste à faire (avec la piste), ou décision
qui revient à Martin.

---

## I. « Tout est flou » (la lisibilité)

1. **✔ Tous les textes et toutes les icônes bavaient.** Chaque icône était une image de 128 pixels que la carte graphique réduisait à 30-40 pixels en mélangeant deux niveaux de « mipmaps » : c'est ce mélange qui floutait. → Chaque icône est maintenant **dessinée à sa taille exacte**, au pixel près (`UI/Icons.cs`).
2. **✔ Les pastilles (tous les boutons, tout le HUD) avaient des bords mous**, même cause. → Des gélules découpées « en 9 tranches » : les bouts ronds gardent leur taille, seul le milieu s'étire.
3. **✔ Un texte centré tombait souvent entre deux pixels** (hauteur impaire) : flou. → Tous les textes sont posés sur des pixels entiers.
4. **✔ Le liseré des gros chiffres faisait un halo baveux** (8 copies décalées de 2,2 px). → Des copies décalées d'un nombre **entier** de pixels (un anneau de 12 à 24), plus une ombre franche dessous.
5. **✔ La carte qu'on vise grossissait par un zoom de l'image** : tout son texte devenait flou. → Elle est **redessinée** plus grande.
6. **✔ Le « grain de pierre » des cartes** était une petite image de bruit étirée à toute la carte : l'« effet de flou bizarre ». → Supprimé.
7. **✔ Les polices étaient en rendu « lisse » sans hinting** : molles en petit. → « Hinted Smooth » (voir les `.meta` des polices).
8. **✔ Les pseudos au-dessus des têtes** : texte + ombre à une position flottante. → Lettres rondes cernées, nettes, à quelques tailles fixes.
9. **→ Si l'image reste floue dans l'éditeur** : la fenêtre *Game* réduit l'image quand sa résolution ne tient pas. Mettre l'échelle à **1x**, décocher **Low Resolution Aspect Ratios** (Mac), ou **Maximize On Play**. Voir le rapport.
10. **✔ Salon : les petites lignes grises** (« plus lents, sans courants ni embuscade ») étaient illisibles. → Supprimées ; les joueurs en **pastilles à leur couleur**.
11. **✔ « un match d'environ 25 min »** : une phrase. → Un chrono et « ~25 min ».
12. **→ Pas de réglage d'opacité du HUD.** Utile pour les vidéos (Phase 5).

## II. Les commandes

13. **✔ L'écran Commandes : douze lignes de phrases** (« il a la flemme »). → Trois colonnes de pastilles — **BOUGER, TES POUVOIRS, VOLER** — une touche, une flèche, une icône.
14. **✔ Prendre la Couronne demandait E maintenu une seconde**, au sommet, trois joueurs dans le dos. → **On passe dessus** (on monte sur le socle), ou **un appui** sur E.
15. **✔ Le sanctuaire aussi demandait E maintenu une seconde.** → Un appui.
16. **✔ L'arbaleste de départ « ne tourne même pas »** : la souris la faisait pivoter de 7° au plus, et pas du tout si l'on regardait ailleurs en montant dessus. → En montant, **la vue se tourne vers la cible** ; la tourelle suit la souris sur **±22°**, l'arrivée tourne avec elle (jamais hors de l'île).
17. **✔ La vue sur l'arbaleste de départ pouvait partir à 180°** de la tourelle. → Elle reste dans l'angle de la tourelle : dès qu'on bouge la souris, elle tourne.
18. **→ Pas de manette.** Le Steam Deck la voudra (Phase 5).
19. **→ Le remappage ne couvre que « capacité » et « pousser ».** Sauter, interagir, F1 : à ouvrir un jour.
20. **→ F1/H ouvre le panneau des touches en pleine partie**, mais rien ne dit qu'il existe (pas de texte, c'est voulu). L'écran Commandes le montre (F1 → icône).
21. **→ Échap pendant le compte à rebours ne fait rien.** Voulu (on ne met pas en pause un départ), mais surprenant.
22. **→ Pas de « maintenir pour courir » vs « appuyer pour courir » au choix.**

## III. La Couronne, le sacre, la victoire

23. **✔ « Il tenait la Couronne à la fin, il a gagné »** : Martin ne veut plus de victoire au chrono. → Le temps écoulé, **personne** ne gagne la manche. On gagne au Monument, point.
24. **✔ L'écran de fin disait « Le temps s'est écoulé, et personne ne tenait la Couronne. »** → Un chrono et une Couronne barrée.
25. **✔ Et « Tu as posé la Couronne sur un Monument en 3:42. »** → Couronne → sacre → chrono, en icônes.
26. **✔ Gagner une manche : une petite animation, trois sauts, et c'est tout.** → **Le vainqueur DANSE sur la musique** (six figures, un pas par temps), confettis, feux d'artifice sur le rythme, projecteur, Couronne au-dessus de la tête.
27. **✔ La Couronne portée traversait le cimier du chevalier** (posée à 2,25 m). → 2,6 m.
28. **✔ Pendant la fête, la Couronne flottait dans le casque.** → 3,1 m.
29. **→ Une manche sans vainqueur ne compte pour personne.** Proposition : une **prolongation** (tant que la Couronne est portée à 0:00, la manche continue jusqu'à ce qu'elle tombe ou soit sacrée). À Martin de trancher → `v2-ideas.md`.
30. **→ Le sacre (3 s) se vide en 1,5 s si l'on sort du cercle.** Juste ? À régler en jeu.
31. **→ Pendant le sacre, le porteur ne peut rien faire d'autre qu'attendre.** Voulu : c'est le moment où les autres foncent.
32. **→ La Couronne sur son socle se prend maintenant en passant.** Un joueur poussé contre le socle la prend par accident : c'est drôle, mais à surveiller.
33. **→ Le verrou de 3 s après un vol n'est montré nulle part** : la victime ne comprend pas pourquoi elle ne la reprend pas. Une icône de cadenas sur la Couronne ?
34. **→ La Couronne qui rentre au sommet (20 s)** : on voit le décompte en haut, pas sur la Couronne elle-même.

## IV. Le combat

35. **✔ « Quand ça pousse, ça pousse un tout petit peu »** : 20 m/s, freinés de 4,5 par seconde → 4 m. → **26 m/s et 10 vers le haut**, et l'élan ne se freine presque plus tant qu'on est en l'air (`Seeker.Launch`) : **une quinzaine de mètres**, trente avec la Poigne.
36. **✔ Le poussé ne réagissait pas** : il glissait. → Il fait des **saltos** en volant (les bots, et ton ombre).
37. **✔ Voler la Couronne envoyait aussi la victime à 15 m** : le voleur la perdait de vue. → Une victime volée ne part qu'à moitié.
38. **✔ La poussée portait à 3 m** : on ratait d'un pas. → 3,2 m.
39. **✔ Un coup qui portait n'avait qu'une micro-pause.** → Une bulle d'énergie en plus à l'impact, et une pause un poil plus longue.
40. **→ La poussée n'a pas d'aperçu** (où va-t-il tomber ?). Une petite flèche au sol quand quelqu'un est à portée ?
41. **→ Poussé hors de l'île, on plane** (les ailes s'ouvrent) : c'est voulu, mais on peut revenir trop facilement par un courant d'air.
42. **→ Deux joueurs qui se poussent en même temps** : le premier traité gagne. Rare, mais à l'hôte de trancher en Phase 3.
43. **→ Pas de parade** (se protéger d'une poussée). Idée Phase 2.
44. **→ Le Mur peut toujours boucher une porte pendant 8 s.** À surveiller.
45. **→ Les capacités se ressemblent encore au son** (un souffle). Une passe « sons » par capacité.

## V. La tour, les obstacles, les gargouilles

46. **✔ Les gargouilles des tours d'angle étaient posées AU SOMMET des tours… sous le toit pointu.** On ne les voyait pas, mais elles tiraient. → Sur le flanc de la tour, côté cour, sur une console.
47. **✔ Les gargouilles, « moches »** : des cubes et des cônes. → Refaites en formes rondes (voir la liste du designer).
48. **→ La gueule qui rougeoie ne se voit pas de loin** : il faut repérer le trait rouge. La cible au sol aide.
49. **→ Deux gargouilles peuvent viser la même personne** : double explosion. Voulu ? Un « tour de rôle » serait plus juste.
50. **→ Éjecté de la tour, on retombe dans la cour, ailes fermées** : parfois sur un autre joueur. Aucune collision entre joueurs en vol : on traverse.
51. **→ Les obstacles de la tour se ressemblent d'une manche à l'autre** (tirés au hasard, mais la même famille). Idée : un obstacle « vedette » par manche.
52. **→ Le sommet est un cul-de-sac si l'on n'a pas d'ailes** : on redescend à pied. Les ailes d'or y sont données, c'est bon.
53. **→ Le couloir piégé est plus long que la tour à la première manche** pour qui atterrit mal. À chronométrer en jeu.

## VI. Le vol, les arbalestes, les courants

54. **→ Les courants d'air ne se voient pas assez depuis l'intérieur de la citadelle.** Les anneaux montent, mais la brume les mange.
55. **→ Planer avec la Couronne (lourde) est frustrant quand on rate le courant** : on tombe dans les nuages et elle rentre au sommet. Voulu, mais dur.
56. **→ Le piqué d'aigle ne marche que sur le porteur.** Sur les autres, rien : on s'attend à pouvoir les bousculer en l'air.
57. **→ Sur l'arbaleste de départ, on ne peut pas choisir sa porte.** Voulu (chacun la sienne).
58. **→ Les arbalestes de l'île visent n'importe où** : on peut se tirer dans le vide exprès. Drôle ; le sceau protège la citadelle.
59. **→ La vitesse en vol s'affiche en chiffres sans unité.** C'est des km/h ; l'icône des ailes à côté aide.
60. **→ Les ailes s'ouvrent seules à 6 m de vide** : sur un rebord de 7 m on plane alors qu'on voulait sauter. Rare.

## VII. Les bots (« ils déconnent complet »)

61. **✔ Un bot coincé contre un mur y restait jusqu'à la fin de la manche.** → **Chien de garde** : s'il ne se rapproche plus de son but depuis 5 s, il refait son chemin, part de biais et saute.
62. **✔ Les bots poussaient des joueurs protégés** (la bulle) : le coup ne faisait rien et leur recharge partait. → Plus sur un protégé.
63. **✔ Ils couraient là où était leur proie, pas là où elle allait** : toujours un temps de retard. → Ils **anticipent** (sa vitesse, une demi-seconde en avance), sauf en facile.
64. **✔ Poussé hors de l'île, un bot se laissait tomber dans les nuages** (il ne cherchait un courant d'air que pour livrer ou chasser). → Il en cherche un, quel que soit son but.
65. **✔ Au sommet, un bot restait planté une seconde devant la Couronne.** → Il la prend en passant, comme toi.
66. **✔ Au sanctuaire, une seconde plantée.** → Un quart de seconde.
67. **✔ En montant, les bots poussaient presque tout le monde** — avec la poussée à 15 m, c'était une loterie. → Ils la gardent pour les bons moments (30 à 65 % selon le caractère, en normal).
68. **→ Un bot ne sait pas esquiver une gargouille** (il ne regarde pas la cible au sol). En coriace, il devrait.
69. **→ Un bot porteur ne zigzague pas** quand on le chasse : il court droit. Facile à attraper en ligne droite.
70. **→ Les bots ne se méfient pas des mines.**
71. **→ Un bot ne sait pas qu'il perdra la Couronne en tombant** (`Crown.Slip`) : il saute parfois d'un rebord avec.
72. **→ Les bots ne se parlent pas** (voulu : ce sont de futurs joueurs), mais leur voix est la même pour tous à la hauteur près.
73. **→ En facile, un bot ne prend jamais d'arbaleste de l'île** (seulement celle de son départ et pour livrer). Voulu.
74. **→ Le bot gardien du Monument attend à 5 m** : un porteur qui arrive par l'autre côté entre sans être poussé.

## VIII. Les menus, le choix des cartes, la fin

75. **✔ Les cartes des capacités : « moches, flou bizarre, texte coupé »** (« Appuie encore sur Espace en l'air : un second saut » coupé en deux). → Refaites : une fenêtre où l'icône brille, le nom sur un bandeau, la phrase en encre sombre sur un cartouche clair, qui **rapetisse s'il le faut et n'est jamais coupée**.
76. **✔ Une bande rouge « remplace Ruée » sur TOUTES les cartes** (les capacités changent à chaque manche). → Supprimée.
77. **✔ « CHOISIS TON CLIC GAUCHE » + une phrase d'explication.** → « CLIC GAUCHE » et l'icône de la souris.
78. **✔ La file des joueurs : des rectangles et « 1. Mahaut ».** → Des pastilles rondes à leur couleur ; celle qui choisit bat ; sous chacune, l'icône de ce qu'elle a pris.
79. **✔ « Mahaut choisit… » écrit.** → Sa pastille, un chrono qui bat.
80. **✔ « TES CAPACITÉS · Ruée · Coureur » en bas.** → Tes ronds de capacités, comme dans le HUD.
81. **✔ Fin de manche : « PERSONNE N'A RAMENÉ LA COURONNE ».** → Chrono + Couronne barrée.
82. **✔ Le score en fin de manche : « Mahaut   2 » en texte.** → Des pastilles (pseudo, Couronne, chiffre).
83. **✔ Le podium : « TU GAGNES LE MATCH », des lignes, trois phrases de statistiques.** → Le pseudo du champion en or et la Couronne ; le classement en pastilles ; tes chiffres en icônes (sacres, Couronnes prises, poussées, capacités, dons).
84. **✔ Pause : « Manche 2 sur 5 · 3:12 restantes ».** → Deux pastilles : manches, chrono.
85. **✔ « Abandonner ? Encore une fois pour confirmer ».** → « Abandonner ? Encore ! »
86. **✔ En ligne : deux phrases.** → Les icônes (en ligne, bot) et « BIENTÔT ».
87. **✔ Départage : « Seuls comptent : Mahaut · Oswin ».** → Leurs pastilles.
88. **→ On ne peut pas revoir les cartes des autres une fois la manche lancée** : Tab les montre (icônes).
89. **→ Le choix des cartes n'a pas de minuterie** : un joueur peut bloquer tout le monde (en ligne, Phase 3).
90. **→ Les manches rechargent la scène** : 1 à 2 s de noir entre deux. Invisible derrière le rideau, mais long sur un vieux PC.

## IX. Le son et la musique

91. **✔ Pas de musique de victoire.** → Une **musique de danse** (fabriquée : 120 BPM, grosse caisse, clap, basse, accords, mélodie) ; celle de Martin la remplace s'il en met une (`Resources/Music/danse-128.mp3`).
92. **✔ La danse et la musique n'étaient pas liées.** → La danse suit **le temps de la musique**, même celle de Martin (tempo lu dans le nom du fichier).
93. **→ La musique de jeu manque de montée** quand quelqu'un se fait sacrer. Un roulement de tambour pendant les 3 s ?
94. **→ Les voix des bots sont les mêmes à la hauteur près.**
95. **→ Pas de son distinct quand ta capacité est prête** hors d'un bip. Voulu (discret).

## X. Les petits bugs et le reste

96. **✔ L'invite d'interaction pouvait chercher une icône « main » qui n'existe pas.** → Une icône qui existe.
97. **✔ Des gardes de code parlaient encore des touches E, R, V et du « maintien » de E** (commentaires trompeurs pour qui lit). → Remis à jour (`AbilityUser`, `Shrine`, `Crown`).
98. **✔ Le HUD fabriquait chaque icône la première fois qu'elle apparaissait** (un à-coup). → Les deux tailles les plus courantes sont préparées pendant les menus.
99. **→ Une image qui s'anime en taille (le compte à rebours) fabrique des icônes neuves** : au plus quatre par image, les autres empruntent la plus proche. À surveiller en performance.
100. **→ Jouer avant de livrer** : une vraie partie de 30 minutes contre 5 bots, chronomètre en main, pour vérifier tout ce qui est coché ci-dessus. Claude ne voit pas le jeu : **Martin, c'est toi le testeur.**
