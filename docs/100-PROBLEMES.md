# Le gamer chiant, deuxième passage : 100 problèmes (et ce qui manquait)

*27/09/2026. Martin : « on tient un truc. Refais le gamer chiant, mais version “ce qu'il
manque”, et corrige 100 problèmes. »*

Premier passage : `docs/100-RAISONS.md` (pourquoi l'ancienne version n'était pas drôle).
Celui-ci part de la version « rien que des capacités » et cherche **ce qui casse, ce qui
frustre et ce qui manque**. Claude ne peut pas lancer Unity : il a relu chaque système
ligne à ligne, calculé les distances de saut, les vitesses, les reculs, et joué la
manche dans sa tête. Les chiffres cités sont ceux du code.

Légende : **✔** corrigé dans cette version · **→** reste à faire (avec le chemin), ou
décision qui revient à Martin.

---

## I. Les bots (« ils sont nuls, ou ils trichent »)

1. **✔ Les bots couraient à 7,6 m/s, toi à 10,8.** Avec la Couronne, tu les semais *toujours* : la course-poursuite n'existait pas. → 10,2 m/s (niveau normal).
2. **✔ Et quand un bot portait la Couronne, tu le rattrapais toujours.** Même correction : 8,7 m/s chargé, contre tes 10,8 à vide. Il faut de l'élan, ou une capacité.
3. **✔ Aucun choix de difficulté.** → Salon : **Bots faciles / normaux / coriaces** (vitesse, réflexes, fréquence des capacités, cadence de poussée).
4. **✔ Les bots tombaient dans les trous de la rampe** : ils sautaient 1,5 m trop tôt (il fallait couvrir 5,1 m, ils en faisaient 3,9). → Ils sautent à 0,8 m du bord.
5. **✔ Les trois bots couraient en file derrière le porteur.** → Le plus proche du Monument **va l'y attendre** et coupe la route (sauf en facile).
6. **✔ Les bots ignoraient les raccourcis.** → Ils prennent les **courants** de la tour (sauf en facile).
7. **✔ Lancés par un courant, les bots marchaient en l'air et se cognaient sous la rampe.** → Ils montent droit, puis refont leur chemin une fois posés.
8. **✔ Arrivé au Monument, un bot restait planté deux secondes.** → Le Monument prend la Couronne dès qu'on entre dans le cercle, pour lui comme pour toi.
9. **✔ Les réflexes des bots étaient les mêmes à tous les niveaux.** → Une capacité toutes les 1,4 s (facile), 0,45 s (normal), 0,25 s (coriace).
10. **→ Les bots ne grappinent pas la rampe du dessus.** Essayé : depuis la rampe, on touche le *dessous* de la dalle. Les courants font le même travail. À revoir si le grappin change.
11. **→ Les bots ne guettent pas les pendules** : ils passent et se font balayer. C'est drôle à voir ; à corriger seulement si ça paraît idiot en jeu.
12. **→ Loin de toi (plus de 70 m), un bot « glisse » le long de son chemin, sans physique.** C'est voulu (on ne le voit pas, ça ne coûte rien), mais il ne peut pas tomber d'un trou pendant ce temps.

## II. Le combat (« je pousse et il se passe rien »)

13. **✔ La poussée ne portait que 2,7 m** : sur une rampe de 5,5 m de large, on ne jetait personne dans le vide. → 3,1 m, et **Poigne** double.
14. **✔ On contre-marchait une poussée** en appuyant sur Z. → Un court **étourdissement (0,2 s)** et presque plus de contrôle pendant un gros recul.
15. **✔ On ne savait pas d'où venait un coup.** → **Le bord de l'écran rougit du côté du coup.**
16. **✔ Les Yeux mitraillaient la rampe** (un tir toutes les 3,8 s par Œil). → Repos de 3,4 s après un tir, un peu plus long à te repérer.
17. **✔ Les pendules entraient dans le mur de la tour** (balancement symétrique de 68°). → 22° vers l'intérieur, 68° vers le vide.
18. **✔ On ne voyait pas venir les pendules.** → Ils **sifflent** quand ils passent près de toi.
19. **→ Toutes les capacités font le même son** (un souffle). Un son par capacité, c'est une journée de travail : prochaine passe « sons ».
20. **→ Les capacités ont peu d'effets visuels** (un éclat de couleur). Des traînées, des ondes visibles aideraient à lire le combat.
21. **→ Le « Mur » peut boucher toute la rampe pendant 8 s.** Contre : Bond, Grappin, Clignement ne passent pas un mur de 3 m. À surveiller en jeu : raccourcir à 5 s si c'est pénible.
22. **→ L'« Échange » avec le porteur ne vole pas la Couronne** (vous changez de place, elle reste sur sa tête). C'est logique, mais on s'attend au contraire. À trancher.

## III. La Couronne (« je l'avais, je l'ai plus, pourquoi ? »)

23. **✔ Il fallait maintenir E pour ramasser la Couronne à terre**, au milieu de la mêlée. → **On la ramasse en passant dessus.**
24. **✔ Qui venait de la perdre la rattrapait aussitôt** (il vole dans la même direction qu'elle). → **Il ne peut pas la reprendre pendant 1,2 s.**
25. **✔ BUG : la Couronne pouvait tomber DANS la tour.** Un rayon d'Œil pousse le porteur contre le mur ; la Couronne tombait 2,2 m plus loin, dans le fût de pierre, et restait introuvable 45 s. → Elle ne tombe jamais dans le fût ni dans un mur.
26. **✔ Il fallait tenir E deux secondes au Monument**, trois joueurs dans le dos. → **Entrer dans le cercle suffit.**
27. **✔ Prendre la Couronne sur son socle prenait 1,2 s.** → 1 s.
28. **✔ On ne savait jamais qui avait pris ou fait tomber la Couronne.** → **Un fil d'événements** à gauche : « Mahaut t'a fait lâcher la Couronne ! », « Oswin a ramassé la Couronne ! ».
29. **✔ « Mahaut porte la Couronne » — oui, mais où ?** → « … sur la rampe », « … en forêt », « … près du Monument ».
30. **✔ À terre, l'écran disait juste « retour dans 12 s ».** → « passe dessus ! ».
31. **✔ Le porteur ne savait pas s'il approchait.** → « Monument à 64 m » sous le chrono (une distance, pas une flèche : la règle « pas de marqueur » tient).
32. **✔ Le grand titre « LA COURONNE / revient au donjon » : il n'y a plus de donjon.** → Une ligne dans le fil : « La Couronne est revenue au sommet de la tour. »
33. **✔ Le grand titre s'affichait même quand un bot prenait la Couronne.** → Seulement quand c'est toi ; pour les autres, le fil.
34. **→ Temps écoulé = le porteur gagne**, même s'il s'est caché en forêt pendant deux minutes. C'est la règle verrouillée ; une **prolongation** (la manche continue tant que la Couronne est portée) serait plus haletante. **À Martin de trancher** (proposée dans `v2-ideas.md`).

## IV. La tour (« je tombe, je recommence tout, j'arrête »)

35. **✔ Tomber de la tour coûtait 30 secondes de montée.** → **Trois courants** : sous chaque trou, un tour plus bas, un disque pâle qui te renvoie seize mètres plus haut.
36. **✔ Les trous n'étaient qu'une punition.** → Ils marchent dans les deux sens : on y tombe, ou on y remonte par le courant.
37. **✔ Les courants auraient pu te projeter sans le vouloir.** → Ils sont au **bord intérieur** de la rampe : on n'y entre qu'exprès.
38. **✔ Le grappin te laissait pendu sous un rebord**, puis tu retombais. → Contre un rebord, il vise un peu au-dessus, et à l'arrivée **tu te hisses**.
39. **✔ Sur la rampe, on ne savait pas où on en était.** → « Tour 2 sur 4 », « Au sommet ».
40. **✔ Tomber de la tour ne se racontait pas.** → « Oswin est tombé de la tour. » dans le fil.
41. **→ La rampe se ressemble d'un tour à l'autre.** Une couleur de bannière par tour aiderait à lire la hauteur d'un coup d'œil.
42. **→ Le sommet n'a pas de couvert** contre les poussées. C'est voulu (arène) ; à revoir si prendre la Couronne devient impossible à quatre.

## V. L'écran (« il me dit pas ce que j'ai besoin de savoir »)

43. **✔ Une capacité qui vise (Crochet, Échange, Grappin) ne disait pas si elle toucherait.** → « **→ Mahaut** » ou « rien en vue » à côté de son nom.
44. **✔ Une capacité qui revenait ne se remarquait pas.** → **Une note claire**, et sa ligne s'éclaire.
45. **✔ « clic gauche pousser » s'affichait à chaque bagarre.** → Seulement tes trois premières poussées.
46. **✔ Aucune explication en jeu.** → **Des astuces**, une seule fois par match, au moment où elles servent : premier pas sur la rampe, premier trou, premier courant, première Couronne, premier Œil, premier don.
47. **✔ Les dix dernières secondes ne se sentaient pas.** → Le chrono devient **un grand chiffre qui bat**, avec un son à chaque seconde.
48. **✔ Le fil des messages dessinait des boîtes et modifiait le style partagé** (cause des tailles de texte qui sautent). → Du texte avec son ombre, sans boîte.
49. **✔ Le fil ne gardait que 3 messages, 3 secondes.** → 5 messages, 5 secondes.
50. **✔ Le texte était trop petit sur certains écrans.** → Réglage **Taille du texte** (80 à 150 %).
51. **→ Pas de « qui me pousse dans le dos »** avant le coup. Des bruits de pas proches, plus forts derrière, aideraient.
52. **→ Les noms des bots ne s'affichent qu'à 22 m.** Au-delà, on ne voit que la lanterne. À régler en jouant.

## VI. Les sensations (« ça manque de patate »)

53. **✔ La ruée ne donnait aucune impression de vitesse.** → **Le champ de vision s'ouvre** d'un coup (ruée, grappin, courant).
54. **✔ Tomber de haut était muet.** → **Le vent siffle** pendant une vraie chute.
55. **✔ Atterrir ne laissait rien.** → Un nuage de poussière (toi, et les bots près de toi).
56. **✔ Le départ de manche était mou** : on arrivait en jeu, l'horloge tournait déjà. → **3, 2, 1, PARTEZ !** Tout le monde figé sur sa ligne, l'horloge, les bots et les Yeux partent ensemble.
57. **✔ Pendant ce décompte, on ne pouvait pas regarder autour de soi.** → La souris marche, les jambes attendent.
58. **→ Les capacités n'ont pas d'animation propre** (le même geste pour tout). Travail d'animation : plus tard.

## VII. Les menus (« il manque les trucs de base »)

59. **✔ Aucun réglage.** → **Réglages** au titre et en pause.
60. **✔ Sensibilité de la souris non réglable.** → 0,3 à 2,5.
61. **✔ Volume non réglable.** → 0 à 100 %.
62. **✔ Champ de vision non réglable.** → 65 à 100°.
63. **✔ Plein écran non réglable.** → Oui / non.
64. **✔ Les réglages étaient à refaire à chaque lancement.** → Gardés sur l'ordinateur (PlayerPrefs — ce ne sont pas des données de partie).
65. **✔ La fin de manche ne disait pas comment elle avait été gagnée.** → « Mahaut a porté la Couronne au Monument en 2:41. » / « … tenait la Couronne quand le temps s'est écoulé. »
66. **✔ Pendant le choix, on ne savait pas ce que les bots venaient de prendre.** → « Oswin a pris Grappin · Mahaut a pris Onde de choc ».
67. **✔ « Abandonner le match » partait au premier clic égaré.** → Il faut confirmer.
68. **✔ Le niveau des bots ne s'expliquait pas.** → Une ligne : « aussi rapides que toi », « rapides, vifs, sans pitié »…
69. **✔ Échap pendant le décompte mettait une pause bizarre.** → Échap attend le « PARTEZ ».
70. **→ Pas de choix des touches.** Prochaine étape des réglages.
71. **→ Pas de manette.** Phase 5.
72. **→ L'entrée « En ligne » ne mène qu'à « pas encore ».** À cacher ou garder ? On la garde : elle annonce la suite.

## VIII. L'accessibilité

73. **✔ Deux bots rouge et vert** : indistinguables pour un daltonien (1 garçon sur 12). → Le vert devient **violet**.
74. **✔ Texte trop petit** : voir 50.
75. **→ Aucun sous-titre pour les sons importants** (l'alarme d'un Œil, un pendule). Les Yeux ont déjà un signal visuel (bords rouges) ; les pendules non.
76. **→ Pas d'option pour réduire les secousses de caméra.** À ajouter aux réglages.

## IX. Ce qui manque encore au jeu (honnêtement)

77. **→ Personne n'a encore joué à cette version.** Tout ce document est de la lecture de code.
78. **→ La citadelle est en cubes gris.** Des murs Kenney/Synty la rendraient belle sans rien changer au jeu.
79. **→ Les personnages sont des silhouettes simples.** Même remède : un pack de personnages low-poly.
80. **→ Les sons sont synthétisés par le code.** Un pack de sons (Kenney en a de gratuits) changerait tout.
81. **→ La musique est faite de nappes synthétiques.** Martin peut donner ses morceaux (dans un dossier `Resources/Music`).
82. **→ Pas de ralenti « replay » du but** en dehors du ralenti de fin de manche.
83. **→ Pas d'emotes ni de moqueries** (un classique du genre). Phase 5.
84. **→ Pas de cosmétiques.** Phase 5 au plus tôt.
85. **→ Pas de jeu en ligne.** Phase 3 (l'architecture est prête).
86. **→ Pas d'événements de manche** (pluie, éclipse, Couronne qui change de tour). Phase 4.
87. **→ Pas d'anti-snowball au-delà du choix en dernier.** Phase 4.
88. **→ L'équilibrage des 26 capacités n'est pas testé.** Certaines sont sûrement trop fortes (Voile ? Échange ?), d'autres trop faibles (Rappel ?).
89. **→ Le tirage des cartes est au hasard.** On peut tomber sur trois passives inutiles : garantir une active par choix ?
90. **→ La durée par défaut (6 min) est une estimation.** Une manche dure sans doute 2 à 3 min en réalité.
91. **→ Chaque manche recharge la scène.** Le temps de construction du monde (affiché dans F3) donne un écran noir de quelques secondes.
92. **→ Les performances avec 15 000 arbres, 12 Yeux et 4 joueurs** n'ont pas été mesurées depuis la refonte.
93. **→ Pas de terrain d'essai** pour tester les capacités sans pression (`v2-ideas.md`).
94. **→ Pas de statistiques pour les bots** au podium : seulement les tiennes.
95. **→ Pas de « meilleur moment » du match** au podium (« la plus longue chute », « la Couronne volée à 2 m du Monument »). Ce serait la Porte 2 : l'histoire qu'on se raconte.
96. **→ Le Voile laisse voir la Couronne au-dessus d'un porteur invisible.** Drôle et lisible ; mais à confirmer.
97. **→ Les mines des autres ne se voient qu'à 5 m.** Voulu ; à vérifier en jeu que ce n'est pas injuste.
98. **→ On ne peut pas revoir les règles pendant la partie** (seulement les commandes). Une page « Règles » dans la pause ?
99. **→ Les noms des bots sont fixes** (Mahaut, Oswin, Guerin).
100. **→ Le vrai test manque : Martin et son frère, manette — pardon, souris — en main.** Chaque « c'est nul » de leur part vaut plus que ces cent lignes.

---

**Bilan : 58 problèmes corrigés dans cette version, 42 notés avec leur chemin** (dont
un qui touche une décision verrouillée — la prolongation — et revient à Martin).
