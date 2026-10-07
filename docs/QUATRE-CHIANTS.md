# Les quatre chiants — 400 points (v42, 14/10 ; corrigés en v43)

> Martin, 14/10 : « la phase 3 est validée ; refais le gamer chiant, le designer chiant, le gars qui
> regarde la logique du jeu, et le clipper chiant — cent fois chacun ».

Quatre regards sur FIEF v41, cent points chacun. Chaque point dit **ce qui cloche** puis **ce qu'il
faudrait**. `[FAIT v42]` : corrigé dans cette passe. `[TEST]` : à vérifier en jouant (Claude ne voit
pas le jeu). `[PHASE 4]` / `[PHASE 5]` : à faire plus tard, selon la règle des phases. Le reste :
proposé, à Martin de trancher.

Les dix à faire en premier sont en bas du document.

**v43 (14/10, Martin : « corrige tout ce qu'ont dit les mecs chiants ») — le bilan.** `[FAIT v43]` :
corrigé dans cette passe (49 points, en quatre lots : la logique, les sensations, le design,
les moments). `[DÉJÀ LÀ]` : le jeu le faisait déjà (18 points). `[VERROUILLÉ]` : contredit une
décision de Martin, laissé tel quel (4 points). Les points sans étiquette sont des
**propositions** : la plupart sont de l'équilibrage ou du contenu, c'est-à-dire la **Phase 4**
qui commence maintenant ; à Martin de dire lesquels il veut.

---

## 1. Le gamer chiant (100)

*Celui qui joue 200 heures et ne pardonne rien.*

1. Le premier coup d'œil sur la tour ne dit pas laquelle des quatre rampes est la mienne — la bannière de ma porte devrait se retrouver en grand au pied de ma rampe. `[TEST]`
2. Au départ, les huit joueurs tirent à l'arbaleste en même temps : 8 trajectoires, impossible de suivre la sienne. Un léger décalage (0,3 s par plateforme) rendrait le départ lisible.
3. Le compte à rebours 3-2-1 ne montre pas où l'arbaleste va me poser. Un marqueur au sol pendant le décompte.
4. Je ne sais jamais quand ma passive « travaille » (Riposte, Armure, Ange gardien) : un petit éclat de son icône au HUD quand elle se déclenche. `[FAIT v43]`
5. La montée en puissance (×2,5) est annoncée, mais rien ne dit où on en est entre deux paliers : le chrono pourrait virer à l'orange avec la puissance. `[FAIT v43]`
6. Le combo « x3 » comptait le même joueur plusieurs fois (le Dragon qui brûle). Il compte maintenant des joueurs **différents**. `[FAIT v42]`
7. Le bandeau « GOTAGA COMÈTE ! » tombait sans arrêt depuis que les bots lancent leurs capacités : seulement les légendaires et les divines, un bandeau à la fois. `[FAIT v42]`
8. Pousser rate parfois de peu sans retour : un petit « whiff » (souffle + croix grise) quand la poussée ne touche personne. `[DÉJÀ LÀ]`
9. En vol, rien ne dit à quelle hauteur je vais toucher l'îlot : une ombre au sol sous moi en vol (comme Mario 64). `[FAIT v43]`
10. Les anneaux de vent ratés : le « pfff » est bien, mais on ne voit pas l'anneau suivant. Le prochain anneau pourrait briller plus fort.
11. Le porteur plane à 11 m/s : trop lent pour s'échapper d'un piqué d'aigle, trop rapide pour être rattrapé à pied — tester 12. `[TEST]`
12. Le sacre de 3 s avec cinq bots en embuscade (v40) : peut-être impossible maintenant. Si oui, 2,5 s. `[TEST]`
13. L'embuscade met des bots sur les trois Monuments : la tension est bonne, mais il faut une sortie — un îlot sans garde au hasard. `[TEST]`
14. Quand je tombe dans les nuages, le retour sur ma plateforme prend du temps mort : raccourcir le noir.
15. La protection de 3 s au respawn ne se voit pas assez sur les autres (je les pousse pour rien) : une bulle visible. `[DÉJÀ LÀ]`
16. Les recharges sous 4 s n'existent plus : bien. Mais une recharge de 24 s (Enclumes) en manche de 4 min, c'est 10 lancers : OK.
17. La touche de capacité au clic gauche entre en conflit avec « viser en maintenant » : on lâche parfois sans le vouloir en bougeant la souris. Seuil de 0,15 s avant de passer en visée. `[TEST]`
18. Pas de moyen d'annuler une visée (clic droit pendant la visée = annuler, au lieu de pousser). `[FAIT v43]`
19. Sur la rampe, le champ de vision ne montre pas ce qui arrive d'en haut (boulets) : le grondement aide, une ombre au sol aiderait plus.
20. Les herses de la tour : leur 0,75 s d'avertissement est court en manche 5+ (Hardness). Garder l'avertissement fixe même quand la vitesse monte.
21. Les pendules plus rapides en fin de match rendent la tour presque infranchissable pour un joueur moyen : plafonner `Tower.Hardness`. `[DÉJÀ LÀ]`
22. Le sceau renvoie violemment : un nouveau joueur ne comprend pas pourquoi. L'astuce existe, mais seulement une fois par partie : la remontrer la 2ᵉ fois aussi.
23. Le vol libre (arbaleste) s'arrête net à la muraille : ça surprend. Un « mur d'air » visible (runes) quand on approche à 15 m. `[FAIT v43]`
24. La Couronne lâchée en l'air tombe sur la terre la plus proche : parfait. Mais on ne la voit pas tomber (elle apparaît). Une trajectoire visible de 0,6 s. `[FAIT v43]`
25. Les bots fondent tous sur moi quand je porte : bien. Mais quand un BOT porte, les autres bots aussi : moi je suis seul contre le porteur. Équilibré. OK.
26. Le Mode Dieu : avec la montée ×2,5 en plus, les fins de manche deviennent du flipper. Plafonner la puissance à ×1,8 en Mode Dieu. `[PHASE 4]`
27. Les sorts longs (Prison 10 s) en fin de manche avec puissance ×2,5 : la prison est la meilleure capacité du jeu. La raccourcir à 7 s. `[VERROUILLÉ : la Prison 10 s est une décision de Martin]`
28. La Folie / Tête à l'envers : gauche-droite seulement depuis v37.3 — à re-tester en vol (on vole « où l'on regarde », la souris n'est pas inversée : effet presque nul en vol). `[TEST]`
29. Le Mini (7 s) + puissance ×2,5 : un mini qui reçoit un coup part hors de la carte. C'est drôle — à garder, mais l'annoncer.
30. L'Échange refuse le porteur : on clique et rien ne se passe. Un refus sonore + icône barrée au viseur (existe ?). `[TEST]`
31. Les capacités refusées sur la tour (Bond, Grappin…) : la carte devrait le dire (« pas sur la tour »), sinon on la prend pour rien.
32. Quand ma capacité est refusée, la recharge ne part pas : bien. Mais le son de refus est le même pour tout : une raison en icône.
33. Le sanctuaire remplace mon clic gauche : on perd la capacité qu'on a choisie au draft sans prévenir. Le HUD devrait montrer les deux une seconde.
34. Le draft de 20 s : en 8 joueurs avec deux tours de table, c'est 5 min d'attente au pire. Ramener à 15 s, et les bots choisissent en 1 s. `[FAIT v43]`
35. Pendant le draft des autres, je n'ai rien à faire : la bulle (v37.3) aide. Pouvoir déjà regarder ses futures cartes serait mieux.
36. Le vainqueur choisit en dernier : bien. Mais avec (joueurs + 3) cartes, il a quand même 4 cartes : assez punitif ? OK.
37. Aucune information sur les passives des autres en jeu : la bulle du draft est oubliée en manche. Survoler un pseudo (Tab ?) pour revoir.
38. Pas de tableau des scores à la demande (Tab) en manche : les pastilles en haut suffisent ? Un Tab façon Fall Guys serait attendu sur Steam. `[DÉJÀ LÀ]`
39. Les cris « GOTAGA t'a envoyé valser » : super. Mais en 8 joueurs ils se répètent : élargir le vocabulaire (30 mots). `[FAIT v43]`
40. Le KO compte dans les médailles mais pas dans le score : normal (on gagne au Monument). Le dire au podium.
41. La manche se termine au temps sans vainqueur : frustrant après 10 min. La prolongation (porteur en jeu = on continue) est dans v2-ideas — à trancher. `[PHASE 4]`
42. Si personne ne gagne aucune manche (tout au temps), il n'y a pas de champion ni de départage : écran de fin vide. Un « Match nul » clair. `[DÉJÀ LÀ]`
43. Le départage limité à 3 manches de plus : ensuite match nul — l'écran le dit-il ? `[TEST]`
44. Changer d'onglet (v36 tourne en fond) : bien. Le son continue en fond : couper le son quand la fenêtre n'a pas le focus (réglage). `[FAIT v43]`
45. Le réglage de sensibilité ne distingue pas vol et marche : en vol, la sensibilité devrait être un peu plus basse.
46. Pas d'option « inverser l'axe Y » : attendue par une partie des joueurs Steam. `[FAIT v43]`
47. Pas de réglage de champ de vision au-delà de… vérifier 60-110. `[TEST]`
48. Les touches : ZQSD/WASD auto ? Sur Steam, la moitié des joueurs sont en QWERTY : vérifier la détection. `[TEST]`
49. Pas de manette : Steam Deck = beaucoup de joueurs. `[PHASE 5]`
50. Le pseudo par défaut : si deux joueurs ont le même en ligne, impossible de les distinguer. Suffixe automatique.
51. En ligne, si l'hôte quitte, tout le monde est renvoyé : le dire clairement (« l'hôte est parti ») — existe (Report) ; vérifier en manche. `[TEST]`
52. En ligne, un invité ne peut pas rejoindre une partie commencée : normal, mais la liste devrait griser les parties commencées (v28 le fait).
53. Pas de « rejouer avec les mêmes » en fin de match en ligne : on doit refaire le salon.
54. Le choix « Contre les bots » : la difficulté des bots est-elle visible au salon ? (BotLevel) — oui, ligne 2. OK.
55. Bots niveau difficile + embuscade + capacités : peut-être trop durs pour un débutant. Le niveau facile n'embusque pas (v40) : bien.
56. Les bots ne prennent jamais de sanctuaire maintenant (15 m) : ils jouent sans don. Un bot sur deux pourrait passer par un sanctuaire **sur son chemin**.
57. Les bots ne se servent pas des anneaux de vent quand ils chassent le porteur en vol. `[TEST]`
58. Les bots en garde au Monument se tiennent à 5 m du cercle : un joueur malin passe entre deux. C'est bien (une faille).
59. Le rail des bots (v39) les colle côté mur : ils bloquent le couloir intérieur pour moi. Alterner les couloirs selon le bot. `[FAIT v43]`
60. Les bots attendent les pendules 4 s au plus : en manche 7, ils se font sortir en boucle. `[TEST]`
61. Le chien de garde qui « téléporte » un bot hors de vue : un joueur qui regarde par la tour le verra quand même. Garder rare.
62. Les bots ne poussent jamais un bot sur la rampe : on le remarque (« ils s'entraident »). Une poussée rare entre bots serait plus crédible. `[VERROUILLÉ : jamais de poussée entre bots sur les rampes, décision du 02/10]`
63. Les bots ne lancent pas de capacité de déplacement pour rattraper le porteur en l'air (Fusée). `[TEST]`
64. La poussée à la vitesse du clic (v36) : un autoclicker gagne. Limiter à ~6 par seconde. `[DÉJÀ LÀ]`
65. Pousser le porteur = voler : avec la poussée infinie, le porteur à pied ne garde jamais la Couronne plus d'1 s au milieu de trois joueurs. Protection du voleur 1,5 s : OK. `[TEST]`
66. Le piqué d'aigle : il n'y a pas d'aide de visée ; on rate souvent. Un léger « aimant » sur les derniers mètres.
67. Le Fantôme interdit au porteur : bien. L'Invisibilité (Voile) aussi ? Un porteur invisible, c'est la colonne d'or qui le trahit : OK.
68. La Couronne lourde : on ne sent pas assez qu'on est lent (pas de son, pas de souffle). Un pas plus lourd.
69. Le cadre d'or quand on porte : excellent. Il cache un peu les bords où battent les alertes de danger : les alertes devraient passer devant. `[DÉJÀ LÀ]`
70. Les alertes (cible au sol) quand je porte la Couronne : prioritaires sur tout le reste.
71. Le repère de la Couronne collé au bord : il cache parfois le chrono en haut. Le décaler.
72. Le HUD : la passive à gauche est petite et grisée, on l'oublie. OK, c'est voulu.
73. La recharge de la poussée n'est plus visible (instantanée) : supprimer le rond de recharge de la poussée côté joueur. `[TEST]`
74. Le hit-stop ralentissait toute la machine en ligne : coupé en ligne. `[FAIT v42]`
75. Le flash en jeu (Hud.Flash) au KO + au palier de puissance + à la prise de Couronne : trois flashs d'affilée parfois. Un seul flash à la fois. `[FAIT v43]`
76. Le palier « PUISSANCE ×2 » peut tomber pendant un sacre : il recouvre la barre du sacre. Le décaler. `[FAIT v43]`
77. L'écran propre (F10) : parfait pour filmer. Il faut aussi masquer le viseur. `[DÉJÀ LÀ]`
78. Pas de mode spectateur après la fin d'une manche en ligne pour revoir le sacre d'un autre joueur : la caméra de victoire le fait.
79. La caméra de victoire tourne trop vite pour lire le pseudo. `[TEST]`
80. La danse : six figures, un pas par temps. Les joueurs voudront choisir leur danse. `[PHASE 4]`
81. Les sons des capacités sont forts quand dix bots lancent en même temps : plafonner le nombre de voix (8).
82. La musique de tension se déclenche à chaque prise de Couronne : avec les vols à répétition, elle saute sans arrêt. Garder la tension 10 s après la dernière perte. `[DÉJÀ LÀ]`
83. La voix de l'arène est en anglais, le reste en français : choisir. `[PHASE 5]`
84. Pas d'indication de ping en ligne.
85. Pas de chat ni d'emote : en ligne, impossible de dire « gg ». Une roue de 4 emotes (sans texte : icônes). `[PHASE 4]`
86. La tour fait 100 m : à pied, c'est 60-90 s de montée. En manche de 4 min, ça laisse peu de temps pour le reste. Choix assumé ? `[TEST]`
87. Une fois la Couronne au sommet prise, plus personne ne monte : la tour devient inutile pour le reste de la manche. Le socle pourrait offrir un don (ailes d'or) à qui y monte ensuite.
88. Les sanctuaires sur les îlots sans Monument : personne n'y va. Les mettre sur le trajet des anneaux.
89. Les courants d'air : trop discrets pour un débutant. Le porteur les voit repérés (v19) ; les autres non.
90. Le vol d'un îlot à l'autre est rarement utile : peu de raisons d'y aller.
91. Quand on gagne, on choisit en dernier : bien. Mais le dernier n'a aucun bonus de rattrapage. Une carte « rattrapage » réservée au dernier. `[PHASE 4]`
92. Le podium final : le roi des KO et des moments, très bien. Un « mur de la honte » (le plus de chutes) ferait rire.
93. Pas de statistiques de fin de match (capacités lancées, touches, vols de Couronne) : `Stats` les compte déjà — les montrer.
94. Le Chanceux (1 fois sur 3, la capacité revient) se déclenche sans le dire assez fort : l'étincelle est petite. `[FAIT v43]`
95. Le Phénix (divin) jamais sur la tour : bien ; le dire sur la carte.
96. Bombe atomique : 30 m — le lanceur est-il protégé ? (Le lanceur est-il dans le rayon ?) `[TEST]`
97. La Bombe collante sur le porteur = il lâche la Couronne : prévisible et fort ; OK.
98. Le Gobe-tout recrache « très loin » : avec ×2,5, hors de la carte à coup sûr. Plafonner le recrachat. `[DÉJÀ LÀ]`
99. La Ruée vers le bord de l'île : les bots ne la lancent pas vers le vide ; moi si, par accident. Un garde-fou ? Non : c'est un jeu d'adresse.
100. Le gamer veut savoir : « est-ce que je progresse ? » — aucune progression entre les parties (choix assumé : pas de persistance). OK.

---

## 2. Le designer chiant (100)

*Celle qui regarde chaque pixel et chaque couleur, et qui veut une identité.*

1. Les cartes ont maintenant un rang (cadre, ruban, gemmes) : c'est la première vraie identité visuelle du draft. `[FAIT v41]`
2. La bulle des capacités des autres affiche le rang à sa couleur. `[FAIT v42]`
3. Les quatre couleurs de porte (rouge, bleu, or, vert) ne se retrouvent pas dans le HUD : le pseudo de chaque joueur pourrait porter la couleur de sa porte.
4. Les rangs : l'or du Légendaire est proche de l'or de la Couronne et du cadre du porteur. Trois ors différents : choisir un or « Couronne » réservé. `[FAIT v43]`
5. Le violet de l'Épique est proche de la teinte de plusieurs capacités (Hypnose, Folie). Le ruban suffit à les distinguer : OK.
6. Le ciel de fin d'après-midi est beau, mais toutes les manches ont la même lumière : faire tourner l'heure (aube, midi, crépuscule) par manche.
7. Le château « fait IA » (Martin) : ce sont les formes primitives. La seule vraie réponse est le Castle Kit de Kenney (`docs/MODELES.md`). `[PHASE 5]`
8. Les haricots sont lisibles et attachants : c'est le cœur de l'identité. Il leur manque **un trait distinctif** par joueur : un chapeau, une forme d'yeux. Sans corps subjectif, ça ne gêne pas le joueur. `[PHASE 4]`
9. Les ailes d'oiseau (v38) : blanches pour tout le monde. Le bout des plumes à la couleur du joueur. `[FAIT v43]`
10. La Couronne : modèle chargé de cristaux qui tournent : trop d'effets autour. Un seul halo.
11. Les effets des capacités utilisent tous les mêmes briques (Burst, Ring, Flash) : à la longue, tout se ressemble. Chaque famille devrait avoir **une forme** à elle : glace = éclats anguleux, feu = flammes qui montent, vent = spirales, foudre = zigzags.
12. Les couleurs des capacités : 92 teintes, beaucoup proches. Regrouper en 6 familles de couleurs (feu, glace, vent, nature, arcane, or).
13. Les icônes SDF sont nettes et cohérentes : à garder. Certaines sont trop détaillées pour 40 px (Gobe-tout redessinée v37).
14. Le ruban de rang déborde au-dessus de la carte : vérifier qu'il ne touche pas la file des joueurs en deux rangées. `[TEST]`
15. Les gemmes de rang sous le ruban recouvrent le haut de la fenêtre de l'icône : décaler l'icône de 6 px. `[TEST]`
16. Le reflet permanent des légendaires attire l'œil vers elles : c'est voulu, mais ça pousse à toujours les prendre. Équilibre : OK.
17. La police Titan One partout : les chiffres du chrono et les pseudos se ressemblent. Lilita One pour les pseudos.
18. Le viseur : un point blanc. Il pourrait prendre la couleur de la capacité prête. `[FAIT v43]`
19. La croix de touche (v41) est blanche et couleur alternées : vérifier la lisibilité sur le ciel clair (fond blanc). `[TEST]`
20. Les bords qui s'embrasent (lancer légendaire) et les bords qui battent (danger) se ressemblent : le danger doit rester rouge-orange, le lancer à la couleur de la capacité. OK, déjà le cas. `[DÉJÀ LÀ]`
21. Le HUD en bas : trois ronds (passive, capacité, poussée). Bien. L'anneau de rang autour de la capacité (v41) épaissit l'ensemble : réduire à 3 px. `[TEST]`
22. Les pastilles de manche en haut : quand il y en a 10, elles débordent. `[TEST]`
23. Le fil des événements (3 lignes) : sa pastille sombre est trop opaque. 60 %. `[FAIT v43]`
24. Les cris en haut et le bandeau d'annonce des capacités se superposent parfois : une file unique.
25. L'écran-titre : quatre boutons, la tour en fond. Le logo FIEF n'existe pas : il faut un vrai logo. `[PHASE 5]`
26. Le menu Jouer (v37.2) a deux gros boutons : manque une image derrière chacun (bots = haricots, en ligne = globe).
27. Le salon : la ligne « Mode : Normal / DIEU » a le même poids que « Manches » : le Mode Dieu mérite un bouton à part, en feu.
28. Les réglages : liste longue sans rubriques. Rubriques Image / Son / Commandes / Jeu.
29. L'écran Commandes en liste (02/10) : bien. Il manque les capacités elles-mêmes (comment viser).
30. Les cartes : la phrase en encre sombre sur crème : très lisible. Garder.
31. La carte « face cachée » : le dos bleu nuit avec la Couronne est beau. Le dos pourrait prendre la couleur du rang pendant le retournement (on devine). `[FAIT v43]`
32. Le retournement d'une légendaire : la gerbe d'étincelles (v41). Ajouter un léger tremblement de la carte avant. `[FAIT v43]`
33. Le podium : sous un voile léger, le champion danse. Les médailles pourraient être de vraies pièces 3D.
34. La victoire : confettis + feux d'artifice + projecteur. Beaucoup d'éléments : retirer l'anneau d'or au sol. `[VERROUILLÉ : l'anneau d'or fait partie de la fête de victoire décidée]`
35. Les Monuments (colonnes bleues) : depuis v36 sans arc ni piliers, ils sont nus. Un socle bas rond, sans collision. `[DÉJÀ LÀ]`
36. Les îlots : tous la même forme. Varier la silhouette (pic, champignon, arche).
37. Les cascades : statiques. Un léger défilement de texture.
38. La mer de nuages : belle. Une ombre de nuage qui passe sur l'île casserait l'uniformité.
39. La tour : six bandes de couleur. Bien pour se repérer. Les bandes pourraient porter un numéro discret (1-6) pour les débutants.
40. Les rampes sans parapet : c'est le jeu. Un liseré lumineux au bord aide à juger le vide.
41. Les obstacles « une seule famille » (rouge frappe, ardoise porte, or bague, braise prévient) : la meilleure décision visuelle du projet. L'étendre aux pièges des capacités (mine, glu, piège à loup).
42. Les mines posées sont presque invisibles : volontaire (Œil de lynx). Un clignotement lent rouge à 3 m. `[FAIT v43]`
43. La Prison : la cage est-elle à la couleur du lanceur ? Elle devrait. `[FAIT v43]`
44. Le Mini : on ne voit pas assez qu'un joueur est mini à 30 m. Une petite icône au-dessus de son pseudo pendant le sort. `[FAIT v43]`
45. Le pseudo au-dessus de chaque joueur : mêmes tailles à 5 m et à 80 m ? Diminuer avec la distance, rester lisible. `[DÉJÀ LÀ]`
46. La lanterne des bots : on ne la voit qu'au crépuscule. Retirer si elle ne sert pas. `[TEST]`
47. Le ciel divin (six thèmes) : magnifique dans les menus ; en manche, il change toutes les 22 s : distrayant. Un seul thème par manche. `[FAIT v43]`
48. Le Mode Dieu dans les menus : bords rougeoyants + flammes. Bien. Les cartes divines pourraient avoir un dos en feu.
49. La palette du HUD (blanc cerné de sombre) : cohérente. Le cadre d'or du porteur est le seul élément « gras » : garder.
50. Le splash « LA COURONNE EST À TOI ! » : écrit. Le seul texte en jeu qui n'est pas un cri : le garder (moment clé).
51. Les toasts du fil : icônes + pseudos. Les icônes « clip » et « ko » sont petites : 1,2×. `[FAIT v43]`
52. Les numéros « x2 / x3 » du combo (v41) dans une pastille : vérifier qu'ils ne recouvrent pas le repère de la Couronne. `[TEST]`
53. Les alertes de cible au sol (lignes de lumière + rayon) : très lisibles. La couleur du lanceur devrait apparaître (qui me vise ?). `[FAIT v43]`
54. Le Souffle (vague de 150 m) : on le voit venir ? Une ligne d'horizon qui se déforme.
55. Les explosions : boule de feu (v37) au cœur des grosses. Bien. Les petites explosions n'en ont pas : OK (hiérarchie).
56. Le tonnerre : craquement + grondement. Le ciel qui blanchit : attention au flash répété en Mode Dieu.
57. Les éclats « Ambiance.Burst » : partout. Diminuer de moitié hors des moments forts : l'image respirera.
58. Le cercle magique sous le lanceur (v32) : identique pour toutes. Prendre la forme de la famille (flocon pour la glace, flamme pour le feu).
59. La couleur du rang pourrait teinter le cercle magique du lanceur : un joueur au loin qui lance une légendaire, on le voit d'or. Simple et fort. `[FAIT v43]`
60. Le HUD montre le rang de MA capacité ; les capacités des autres en jeu, non. Une petite pastille de rang au-dessus des pseudos quand ils lancent. `[FAIT v43]`
61. La barre du sacre (à l'écran de tous) : bien. Elle devrait porter la couleur du joueur qui se fait sacrer. `[DÉJÀ LÀ]`
62. La colonne dorée de la Couronne : elle traverse le ciel, parfaite pour s'orienter.
63. Les plateformes de départ : huit identiques. La couleur du joueur au sol de sa plateforme. `[DÉJÀ LÀ]`
64. Les couloirs piégés : chicanes en pierre + chevrons de braise. Bien. Les arches d'entrée pourraient porter la bannière de la porte.
65. Le pont-levis : animé ? `[TEST]`
66. Les jardins de la cour : personne n'y passe. Les remplacer par l'arène (zone dégagée pour se battre au pied de la tour).
67. Les escaliers des remparts : inutiles depuis le sceau. Les retirer (moins de maillages).
68. Les gargouilles retirées (v36) : leurs perchoirs restent-ils sur les tours ? `[TEST]`
69. Le code mort des gargouilles (`World/Eye.cs`) : 646 lignes jamais posées. À garder tant que Martin peut les vouloir — le noter.
70. Les icônes des capacités divines : certaines n'ont pas d'icône propre (réutilisent une autre). `[TEST]`
71. Les noms des capacités : courts et clairs depuis v33. « Tonneau de haricots », « La bûche » : drôles, à garder.
72. La phrase des cartes « tu fais → il arrive ça » : excellente règle. La tenir pour toute nouvelle capacité.
73. Le mot « ÉPIQUE » est long dans le ruban sur les petites cartes (onze cartes) : la taille baisse automatiquement. `[TEST]`
74. Les gemmes de rang : rondes. Des losanges seraient plus « gemmes ». `[FAIT v43]`
75. La bulle de survol au draft : panneau sombre + bord à la couleur du joueur : cohérent avec les pastilles.
76. Le chrono du draft devient rouge à 5 s : bien.
77. La file des joueurs en haut du draft : 8 pastilles + icônes prises = encombré. OK à 8.
78. Le fond du draft (rayons + braises) : dynamique. Les rayons du rang de la carte visée seraient un bel écho.
79. Le voile du podium : léger. Le champion danse : bien cadré ? `[TEST]`
80. Le haricot qui s'affaisse quand il perd : drôle. Il pourrait aussi pleurer une goutte (deux sphères).
81. Les yeux qui suivent la Couronne (v34) : le meilleur détail du jeu. Le montrer dans la bande-annonce.
82. Les étoiles autour de la tête quand on est étourdi : classiques et lisibles. OK.
83. La traînée des gros coups : à la couleur de celui qui frappe ? Elle devrait. `[FAIT v43]`
84. Le geste du lanceur (accroupi puis bras au ciel) : identique pour toutes les capacités. Trois gestes : lancer (projectiles), taper le sol (ondes), bras croisés (sorts).
85. La poussée : le bras recule puis part (v34). Bien.
86. Le pas lourd du porteur (proposé au gamer) : aussi visuel, de la poussière plus grosse.
87. L'écran de chargement entre les manches : un rideau. Afficher l'icône de la manche suivante (« manche 3 ») avec la voix.
88. La voix « round 2 », « final round » : bien. Un « tie breaker » existe.
89. La typographie du bandeau « PUISSANCE ×2 ! » : même style que l'annonce des capacités. Le distinguer (feu orange, comme le palier). `[FAIT v43]`
90. Les flammes de bas d'écran du Mode Dieu dans les menus : très bien.
91. La cohérence « zéro texte » : tenue, sauf les exceptions demandées (cris, bandeaux, cartes). Documenté. OK.
92. Le design général manque d'un **personnage-mascotte** pour la page Steam : un haricot couronné, toujours le même. `[PHASE 5]`
93. Le titre « FIEF » ne dit rien du jeu à un anglophone : à réfléchir avant la page Steam. `[PHASE 5]`
94. La capsule Steam (l'image de la boutique) : un haricot qui vole avec la Couronne au-dessus des nuages, la tour derrière. `[PHASE 5]`
95. Les captures d'écran Steam : il faut des moments, pas des paysages. Le mode F10 + les KO. `[PHASE 5]`
96. La palette globale : ciel pêche, nuages rose, château crème, ardoise bleu nuit, or mat. Harmonieuse : la tenir.
97. Le noir : interdit dans les effets (v35). Tenir la règle partout (y compris le HUD : utiliser le bleu nuit).
98. Les ombres : 4 cascades jusqu'à 130 m. Les îlots lointains n'ont pas d'ombre : OK.
99. L'anticrénelage x4 : bien. Les fils fins (cordes, rubans) scintillent encore : les épaissir.
100. Le designer veut **une capture qui se reconnaît en une seconde** : le haricot, la Couronne dorée, la tour à rampes, la mer de nuages rose. Tout le reste doit servir cette image.

---

## 3. Le logicien (100)

*Celui qui lit le code et cherche ce qui peut casser.*

1. `Combat.Afflict` allumait ta croix de touche avant le Miroir : un sort renvoyé te montrait quand même « touché ». Déplacé après le Miroir. `[FAIT v42]`
2. Un sort (Prison, Hypnose…) ne comptait pas comme un coup pour le KO : enchaîné puis poussé dans le vide par un obstacle, personne n'avait le KO. `Afflict` note maintenant le lanceur (`LastHitBy`). `[FAIT v42]`
3. `Hud.HitStop` (temps à 5 %) ralentissait la machine locale en ligne : coupé en ligne pour le combo. `[FAIT v42]` Les autres hit-stops (KO, Home run) restent en ligne : à couper aussi si un décalage apparaît. `[TEST]`
4. `Highlights.Sniped` et `Dodged` n'étaient appelés que par les gargouilles, retirées en v36 : deux moments morts. Remplacés. `[FAIT v42]`
5. L'en-tête de `Crown.cs` parlait encore de la touche E. `[FAIT v42]`
6. `Match.CheckTie` ne lance pas de départage si personne n'a gagné de manche (`best > 0`) : `Champion` est null — il faut un écran « match nul » explicite. `[DÉJÀ LÀ]`
7. `Match.Over` s'arrête à `Rounds + 3` : trois départages au temps → match nul. Correct, mais à documenter dans LA-SAISON.
8. `Combat.Power` dépend de `Season.Elapsed` (synchronisé par l'hôte) : en ligne, un invité en retard de 0,5 s a une puissance légèrement différente — négligeable (0,002).
9. `Combat.Power` multiplie la vitesse verticale par √P : avec la Rage (×2,5) et la puissance (×2,5), une poussée peut dépasser 150 m/s horizontaux → le `CharacterController` peut traverser un mur fin en une image. Plafonnée : 70 m/s à plat, 60 vers le haut. `[FAIT v42]`
10. `Seeker.CooldownOf` applique la puissance aux recharges, mais `Seeker.Refund` rend la capacité : OK.
11. `CastFeel` est statique : il garde `touched` (des `Seeker`) d'une manche à l'autre — inoffensif (vidé à 1,5 s), mais il faudrait le vider au chargement.
12. `Warnings.surgeTier` est statique : redescend tout seul à la manche suivante (la puissance repart plus bas). OK.
13. `Rival.Opportunity` fait un `Physics.Linecast` par cible potentielle, à chaque réflexe (0,25 s en difficile) : 7 bots × 7 cibles × 4/s = ~200 rayons/s. Acceptable.
14. `Rival.RailClimb` lit la rampe avec `Tower.RampOf` : à la jonction de deux rampes (12,5 m d'écart vertical), un bot sauté peut être lu sur la mauvaise rampe pendant une image. Le test `|y - mid.y| > 3` le filtre. OK.
15. `Rival.RailClimb` vide `path` à chaque image : quand il tombe du rail en l'air, `Waypoint()` renvoie la cible (le sommet) et il « marche » vers le centre de la tour en l'air. Le chemin est refait au sol. OK mais visible ? `[TEST]`
16. `Rival.Ambush` choisit le Monument par `Index % 3` : si deux Monuments sont sur des îlots voisins, deux bots gardent presque le même endroit. Répartir par distance. `[FAIT v43]`
17. `Rival.Ambush` : `preyVelocity` n'est mise à jour que dans `Act` quand `prey` existe — la première image, elle vaut l'ancienne proie. Petit écart.
18. `Rival.TryAnyBallistaTo` limite la recherche à toutes les 2 s : entre deux, `Ambush` envoie le bot vers le bord de l'île, puis vers l'arbaleste : léger aller-retour. `[TEST]`
19. `Rival.LeaveThePad` : le tir est calculé depuis `Seat` avant que l'arbaleste ne pivote : l'atterrissage peut dériver d'un mètre. Le garde-fou du sceau (v38) couvre le pire.
20. `Ballista.Fire` donne `FreeFlight` aux bots aussi : un bot tiré en `Goal.Raid` n'ouvre pas ses ailes (seulement en chasse). Voulu (tir balistique).
21. `Crown.Below` renvoie `Tower.CrownSpot` si rien n'est trouvé : la Couronne peut remonter au sommet dans un cas extrême (au-dessus du vide loin de tout). Rare ; renvoyer plutôt le bord de l'île le plus proche. `[FAIT v43]`
22. `Crown.Update` : `ReturnHome` si elle passe sous `Ground.FallLine` : c'est le seul retour au sommet qui reste. Correct (filet de sécurité).
23. `Crown.TryTakeFor` : 1,5 m de marge réseau au-dessus de `Within`. Un invité à 200 ms de ping peut se voir refuser une prise légitime. `[TEST]`
24. `Monument.Update` : le sacre décroît à 2× quand on sort du cercle au lieu de repartir de zéro. Un porteur poussé dehors 0,5 s garde 2 s de sacre. Choix de design à confirmer.
25. `Monument.Update` : si le porteur change dans le cercle (vol), le sacre repart de zéro pour le voleur : correct.
26. `Monument.TryDeliver` vérifie `Within(4.5)` alors que le cercle fait 3,6 : marge de 0,9 m pour le réseau. OK.
27. `Monument.MirrorWinner` (invité) prend le Monument le plus proche du gagnant : si le gagnant est à égale distance de deux (impossible en pratique). OK.
28. `Menus.Quit` attend l'image suivante (v39) : correct. `OnApplicationQuit` appelle aussi `NetSession.Leave` : double appel inoffensif.
29. `Menus.Update` : `slowMotion` réécrit `Time.timeScale` chaque image et peut écraser un hit-stop ou une pause. Vérifier qu'on ne met jamais en pause pendant un ralenti.
30. `Hud.HitStop` refuse si `timeScale != 1` : un hit-stop pendant un ralenti est ignoré. OK.
31. `Season.Tick(Time.deltaTime)` : le chrono de la manche ralentit pendant les hit-stops (5 %). En solo, la manche dure quelques dixièmes de plus. Négligeable.
32. `Shouts` et `Warnings` gardent des références statiques à des `Seeker` d'une manche passée pendant 2,4 s : affichage d'un pseudo d'avant le rechargement, au pire. OK.
33. `Highlights.recentHits` (nouveau) est vidé par `Reset`. `[FAIT v42]`
34. `AbilityInfo.Count = 133` codé en dur : si on ajoute une capacité sans le changer, la table l'ignore. Le calculer depuis l'enum. `[FAIT v43]`
35. `AbilityInfo.Retired` (Encre) : la capacité reste dans l'enum (réseau). Un bot peut-il l'avoir d'une ancienne table ? Non (tirage à chaque manche). OK.
36. `Match.Draft.Build` : `pool.RemoveAll` des capacités que tout le monde a déjà : avec 8 joueurs et peu de passives, la table peut avoir moins de 6 cartes. `[TEST]`
37. `Match.Draft.BotChoice` : la liste de goûts des bots ne contient pas les classiques ni les divines récentes ? Vérifier qu'il prend quand même une carte (sinon le premier). `[TEST]`
38. Le draft en ligne : la carte visée est prise au bout de 20 s chez le joueur ; si son message n'arrive pas, l'hôte prend-il une carte pour lui ? `[TEST]`
39. `NetLink.Version = 12` inchangé depuis v37 : v41 et v42 ne changent pas le protocole. Correct, mais deux PC en v41 et v42 peuvent jouer ensemble avec des cartes de rang différent (purement visuel). OK.
40. `NetGame.SendCast` envoie chaque lancer : avec les bots qui lancent maintenant beaucoup (v39), le trafic monte. Mesurer. `[TEST]`
41. `Combat.Echoed` ignore les coups des capacités rejouées : correct. Mais les effets de souffle (`Raz`, `Tsunami`) poussent-ils les objets physiques chez l'invité ? Purement visuel. OK.
42. `CastFeel.Hit` est appelé avant la branche réseau (`victim.Remote`) : le lanceur local voit sa croix même si le coup est refusé chez l'autre (protégé là-bas). Rare.
43. `Rival.BestTarget` ignore `o.Hidden` (invisible) : bien. Il ignore aussi `o.Graced` : bien.
44. `Rival.Opportunity` pour les pièges : « quelqu'un derrière lui » utilise `transform.forward`, qui tourne avec le bot : correct.
45. `Rival.Reach` : la Nuke (bombe posée, explose plus tard) est classée « autour de lui à 12 m » : le bot la pose au milieu des autres puis reste dedans. Vérifier qu'il est protégé de sa propre bombe. `[TEST]`
46. `Rival.Reach` : l'Enclume (200 m) est lancée dès qu'une cible existe : en Mode Dieu, un bot la lance toutes les recharges. Voulu (c'est divin).
47. `Rival.Opportunity` ne lance jamais une capacité qui déplace le bot (`MovesMe`) : les bots n'utilisent la Ruée, la Charge, la Fusée que par leurs règles précises. OK.
48. `Rival.WatchStill` peut renvoyer un bot sur sa plateforme s'il est coincé sans chemin (`Teleport(Spawns.Of…)`) : s'il portait la Couronne, non (testé). OK.
49. `Rival.RailClimb` téléporte le bot 3 m plus haut s'il ne monte plus pendant 12 s hors de vue : cela peut le poser dans un obstacle. Rare.
50. `Rival.Think` : en route vers une arbaleste, il ne change pas d'avis pendant 10 s même si le porteur atterrit à côté de lui. Raccourcir à 6 s quand un porteur existe. `[FAIT v43]`
51. `Rival.HuntCarrier` est appelé à chaque réflexion (0,3-0,5 s) : `Ambush` peut alterner Monument A et B quand le porteur en vol change de direction. Garder le choix 2 s. `[FAIT v43]`
52. `Rival.LeaveThePad` : les deux bots d'une porte à ±2,2 m de l'axe : si la porte fait 8 m, ils passent. OK.
53. `Ward.Crossing` ne compte que l'entrée par les airs : un bot projeté (`knock`) au-dessus de la muraille vers l'intérieur est renvoyé (normal).
54. `Wings.NoClimbInside` : dans la citadelle on ne remonte jamais en volant ; le souffle d'un anneau excepté. Les anneaux sont-ils hors de la citadelle ? Oui (du sommet vers les îlots, au-dessus). OK.
55. `Tower.Hardness` monte de 0,14 par manche, plafonné à 2,12 (manche 9) : déjà borné. En manche 9+, les pendules vont deux fois plus vite : peut-être plafonner plus bas (1,7). `[TEST]`
56. `BoulderChute` en volée sur les quatre rampes : équitable. OK.
57. `Course` : les mêmes stations devant les quatre portes : équitable. OK.
58. `Spawns.OnPad` : un joueur poussé SUR une autre plateforme que la sienne, que se passe-t-il ? (Son arbaleste est-elle utilisable par lui ?) `[TEST]`
59. `Ballista.HasFixedTarget` : les arbalestes de plateforme visent le parvis. Un bot ne les utilise pas pour chasser (exclu) : correct.
60. `Respawn.Of` : remet `LastHitBy` à null après `Highlights.Fell` : ordre correct (le KO est compté avant).
61. `Crown.FellWith` d'un porteur qui tombe : la Couronne se pose « sous lui » (v37). S'il tombe sous l'île (y < 0), le rayon vers le bas ne trouve rien → terre la plus proche. Correct.
62. `Crown.Drop` cherche « à côté, au même niveau » : sur la rampe, la Couronne peut tomber dans le vide entre deux spires si `SafeSpot` échoue. `[TEST]`
63. `Combat.Hit` sur un protégé (`Graced`) : rien. Mais `dropsCrown` a déjà été forcé à vrai avant le test de protection : sans effet puisqu'on sort. OK.
64. `Combat.Shove` : la Rage se vide même si la poussée ne touche personne ? (`by.Rage = 0` avant la recherche de cible.) Oui : une poussée dans le vide gaspille la Rage. Elle ne se vide plus que si la poussée touche. `[FAIT v42]`
65. `Combat.Shove` : le Coup de pied (`SuperShoveUntil`) est consommé seulement si on touche : correct.
66. `Combat.Shove` : le Home run compte les poussées réussies, y compris sur un protégé ? (`!best.Graced` testé.) Correct.
67. `Combat.Hit` → `Riposte` frappe le lanceur à 16 m/s × puissance : en fin de manche, la Riposte devient énorme. Voulu ?
68. `Combat.Hit` → `Kamikaze` : `Blast` avec `victim` comme lanceur, donc la puissance s'applique aussi. Cohérent.
69. `Combat.Afflict` : `Prison` libérée au premier coup (`RootedUntil = -1` dans `Hit`) : la propre capacité AoE du lanceur qui touche aussi la victime libère sa prison. Combo impossible : à savoir.
70. `Seeker.GraceUntil` mis au min(…, +1,5 s) à la prise de Couronne : un Fantôme lancé juste avant est raccourci. Voulu.
71. `PlayerController` : Tête à l'envers inverse `input.x` ; en vol, la direction suit le regard (souris non inversée) : la capacité n'a presque pas d'effet en vol. Inverser aussi le lacet de la souris en vol ? À trancher.
72. `Rival` : Tête à l'envers miroir de la direction autour de `transform.forward` : si le bot tourne vers sa direction voulue chaque image, l'effet s'annule en partie. `[TEST]`
73. `GodSky` : `DrawFlash` n'est plus appelé (v37.2) mais `Flash` reste calculée. Code mort minime.
74. `Hud.Flash` : appelé par le palier de puissance (v38) même quand l'écran propre (F10) est actif ? `[TEST]`
75. `CardArt.Draw` : la carte du rang dessine `Grow(card, line4 * 1.6)` avant le cadre : sur une carte grisée (déjà prise), le cadre de rang reste vif. L'atténuer. `[FAIT v43]`
76. `Menus.Card` : `System.Array.IndexOf(cardRects, card)` compare des `Rect` flottants : si `card` est décalée pendant l'entrée (animation), l'index peut ne pas être trouvé → pas de fracas pour cette carte. Passer l'index directement. `[FAIT v43]`
77. `Menus.revealed[]` n'est remis à zéro que quand la carte redevient face cachée : au tour de table suivant (actives), les cartes repartent face cachée → remise à zéro. OK.
78. `Menus.DrawDraftPeek` crée une `List<Ability>` à chaque image de survol : allocation négligeable.
79. `Icons.Width` mesure avec la police du titre : la bulle mesure les noms avec la même : cohérent.
80. `Warnings.Announce` ignore le joueur lui-même : correct (il sait ce qu'il lance).
81. `Warnings.DrawSurge` joue `Sfx.Discovery` : même son que la prise de Couronne et le retournement légendaire. Trois sens pour un son : donner au palier son propre son (gong). `[FAIT v43]`
82. `Sfx.Beep` utilisé pour la touche : c'est aussi le son du chrono qui presse (draft, fin de manche). Deux sens : un « ding » dédié serait mieux (`Sfx.Ding` existe, en 3D).
83. `Stats.Casts` compte les lancers du joueur ; l'Écho (double lancer) compte-t-il deux fois ? Non (`EchoCast.Echoing`). OK.
84. `Highlights.Show` incrémente le compteur de moments du **subissant** quand il n'y a pas d'acteur (`actor != null ? 1 : 0`) : 0, correct.
85. `Highlights.CapacityHit` « abattu en vol » : on teste « rien sous lui à 4 m » ; un porteur qui saute d'une marche compte comme « en vol ». Rare (il faut être touché pile).
86. `Highlights` : TRIPLÉ peut se déclencher pour un bot : le toast n'est montré que si toi tu es concerné. OK.
87. `Match.EndRound` : `RoundSeed = RoundSeed * 31 + 7` peut déborder (int) : en C#, le débordement boucle sans erreur (unchecked). OK.
88. `Match.Mirror` (invité) recopie `Played` : si un message d'état arrive en retard, l'invité peut revenir d'une manche en arrière un instant. `[TEST]`
89. `NetSession.Leave` appelle `Match.Abandon` si `Match.Online` : quitter le salon en plein match en ligne vide les places : l'hôte voit-il le joueur partir ? `[TEST]`
90. `SteamNet.Shutdown` ne fait rien si Steam n'est pas prêt : correct.
91. `Application.runInBackground` (v36) : sur un portable en batterie, le jeu en fond chauffe. Réglage « limiter les images en fond » (30 i/s).
92. `PlayerPrefs` : les réglages sont gardés ; le pseudo aussi. Un pseudo vide ? Le jeu en met un par défaut ? `[TEST]`
93. `GameConfig` : « le code fait foi » sauf la case. Un réglage changé par erreur dans l'Inspector puis la case cochée ? Documenté. OK.
94. Le vérificateur ne sait pas lire les déclarations à plusieurs champs dans les structures imbriquées (vu en v33) : à corriger un jour dans `Tools/verifier.py`.
95. `Tools/compiler.sh` compile avec et sans Steam : très bien. Il ne lance aucun test de logique : un petit test de `Match` (manches, départage, champion) en C# pur, comme `reseau.sh`, attraperait les points 6-7. `[PHASE 4]`
96. `Rival.cs` fait 2 437 lignes : trop pour une seule classe. Le découper (Cerveau, Marche, Capacités, Arbalestes) le rendrait lisible pour Martin. Rien ne presse.
97. `Menus.cs` fait 2 479 lignes : même remarque (un fichier par écran).
98. Beaucoup de constantes de jeu sont en dur dans le code (3,1 m, 4 s, 22 m…) : les regrouper dans `GameConfig` faciliterait l'équilibrage de la Phase 4. `[PHASE 4]`
99. Les commentaires datés (« 05/10, Martin : … ») racontent l'histoire : précieux pour Martin. Les garder, mais les anciens (gargouilles) peuvent partir avec le code mort.
100. Le logicien conclut : aucune faille grave trouvée dans la boucle (Couronne, sacre, manches, réseau). Les vrais risques sont les **valeurs extrêmes** (puissance ×2,5 combinée à Rage, Riposte, Gobe-tout) : la Phase 4 doit poser des plafonds.

---

## 4. Le clipper chiant (100)

*Celui qui veut un clip de 15 secondes toutes les cinq minutes.*

1. « ABATTU EN VOL » revient : ta capacité touche le porteur en plein ciel. `[FAIT v42]`
2. « TRIPLÉ ! » : ta capacité touche trois joueurs différents en 1,2 s. `[FAIT v42]`
3. La foule fait « OOOH » sur ces deux nouveaux moments. `[FAIT v42]`
4. Le combo « x2, x3 » au viseur (v41) : parfait pour un clip, il se lit sans le son. `[DÉJÀ LÀ]`
5. Le clip parfait du jeu : un porteur qui plane, cinq bots qui l'attendent au Monument (v40), il esquive. Il faut que la caméra aide : quand tu portes la Couronne en vol, le champ de vision s'élargit un peu.
6. Le KO (colonne de lumière depuis les nuages) : l'image du jeu. Elle doit être visible de partout : plus haute (200 m). `[FAIT v43]`
7. Le « DOUBLE KO » mériterait une voix (« double KO ! ») avec la voix de l'arène.
8. Pas de « TRIPLE KO » : trois KO en 12 s = moment légendaire. `[FAIT v43]`
9. Le sacre au buzzer : la voix pourrait compter « 3… 2… 1 » à la place des cloches dans les 15 dernières secondes.
10. La remontada : bien. Il faut que l'écran de fin de manche la rappelle.
11. Le « VIRÉ DU SOMMET » : excellent. Un ralenti n'est pas permis (pas d'aura) ; un temps d'arrêt de 0,1 s l'est. `[VERROUILLÉ : pas de ralenti (plus d’aura) ; le temps d’arrêt de 0,1 s est fait]`
12. La patate chaude (4 mains en 15 s) : avec la poussée infinie, elle arrive souvent. Monter à 5 mains. `[FAIT v43]`
13. Un moment « VOLÉ AU MONUMENT » : la Couronne volée **dans le cercle** pendant le sacre (plus fort que le sacre arraché). `[FAIT v43]`
14. Un moment « SAUVÉ IN EXTREMIS » : le porteur rattrapé par un courant d'air à moins de 5 m des nuages.
15. Un moment « L'ENFILADE » : les quatre anneaux de vent avec la Couronne → la voix « perfect ! ». `[FAIT v43]`
16. Un moment « RETOUR À L'ENVOYEUR » : un sort renvoyé par le Miroir. `[FAIT v43]`
17. Un moment « HOME RUN » : déjà un effet ; le compter comme moment. `[FAIT v43]`
18. Un moment « PRISON GÉANTE » : quatre joueurs en cage d'un coup (Mode Dieu).
19. Un moment « LA LUNE » : la chute de la lune qui touche trois joueurs.
20. Un moment « DÉPOSÉ PAR UN BOT » : un bot qui gagne pendant que tu étais à 2 m : la honte, très clippable.
21. Les moments des autres ne s'affichent plus (v27, écran épuré) : bien. Mais au podium, les rappeler (« le moment du match »). `[FAIT v43]`
22. Un replay automatique de 8 s du moment du match au podium : le Graal du clipper. Gros travail. `[PHASE 4]`
23. L'écran propre (F10) : il faut un rappel au démarrage du match (une fois) pour les streamers.
24. Le mode photo : figer le temps et tourner la caméra. `[PHASE 5]`
25. La touche « clip » qui sauvegarde les 30 dernières secondes : impossible sans enregistreur ; renvoyer vers la relecture instantanée d'OBS/Steam (Steam a un enregistreur de jeu). `[PHASE 5]`
26. Les cris « GOTAGA t'a envoyé valser ! » : très clippables. Les mettre aussi quand un bot vole la Couronne à un autre bot que tu vois ? Non (épuré). OK.
27. La danse de victoire : les gens clippent les danses. Six figures ; ajouter « le dab » et « le moonwalk » (haricot qui glisse en arrière). `[PHASE 4]`
28. Les perdants qui s'affaissent : drôle. Un perdant qui tape du pied.
29. La Couronne qui flotte au-dessus du gagnant : plus grosse au podium.
30. Le son de la poussée (BOUM) : parfait. Il sature quand trois poussées tombent ensemble.
31. Le retournement d'une légendaire au draft (v41) : un clip de draft possible (« j'ai eu la Lune »). Ajouter une réaction des autres (haricots qui s'écarquillent sur la file).
32. Le palier « PUISSANCE ×2,5 » : clippable si l'écran le montre bien. Ajouter un grondement de foule.
33. Le Mode Dieu : chaque divine est un clip. Les divines devraient toutes avoir un « avant » visible (la cible au sol) : vérifier les manquantes. `[TEST]`
34. La Comète : la cible au sol + la boule de feu (v37) : bien. Le son d'arrivée doit commencer 2 s avant. `[FAIT v43]`
35. Le Mouton explosif : drôle. Un « bêêê » plus fort juste avant l'explosion. `[FAIT v43]`
36. La Sainte grenade : elle chante (Worms). Le chœur doit couper net avant le BOUM : silence, puis explosion.
37. La Bombe disco : tout le monde danse 3 s. La musique disco doit couvrir la musique du jeu ces 3 s.
38. Le Gobe-tout : aspirer puis recracher. Le recrachat doit faire un « ptou ». `[DÉJÀ LÀ]`
39. La Charge du chevalier : le joueur emporté et écrasé au bout : un « crac » de mur.
40. La Bûche : un bruit de bowling quand elle renverse trois joueurs (« strike ! »).
41. Le Tonneau : quatre petits haricots kamikazes — leurs petits cris.
42. Le Boulet bleu (Mario Kart) : la panique du porteur. Une alerte sonore spéciale pour le porteur quand il est visé.
43. Le Poing du faucon : la charge doit faire « FAAAL-CON » ? Droit d'auteur : un cri maison (« HAAA-PAF »).
44. La Force imparable : la chute de 24 m doit secouer l'écran de tout le monde dans 30 m.
45. La Prison : le cliquetis de la cage qui se ferme + la victime qui tape les barreaux.
46. Le Lasso : le « yiha » du lanceur.
47. L'Hypnose : la victime qui marche vers toi avec des spirales dans les yeux (les yeux du haricot !). Très clippable.
48. Le Ballon : la victime qui gonfle puis éclate avec un « pop » énorme.
49. La Tête à l'envers : des étoiles qui tournent à l'envers au-dessus de la tête.
50. Le Mini : une voix aiguë (« hélium ») pour les cris du mini.
51. Le Géant : le pas qui fait trembler l'écran des autres.
52. La Taupe : la bosse de terre qui court sous le sol.
53. Le Trou noir : le son aspiré (reverse) puis le BOUM.
54. Le Séisme : tous les haricots sautent en même temps : synchroniser leurs yeux écarquillés.
55. Le Raz-de-marée : un surfeur ? Non. Le bruit de vague doit couvrir tout.
56. La Fusée : la traînée de fumée qui dessine une courbe dans le ciel : visible de loin.
57. Le Trampoline : le « boing » qui monte d'une note par rebond.
58. Le Grappin : le « tchac » de l'accroche.
59. La Ruée : la traînée à la couleur du joueur.
60. La Foudre : le tonnerre (v37) : excellent. Le haricot foudroyé devient noir une demi-seconde (squelette visible = trop ? juste noir, puis il cligne des yeux).
61. Le Boomerang : un sifflement qui revient.
62. Le Piège à loup : le « clac » + la victime qui sautille sur place.
63. La Peau de banane : la glissade avec un salto, le son « wiiiz ».
64. La Colle : le bruit de succion à chaque pas.
65. La Fumée : les yeux du haricot qui brillent dans la fumée.
66. L'Invisibilité : seul le pseudo reste… non, le pseudo disparaît (sinon inutile). Un léger scintillement.
67. Le Mur : il sort du sol avec un bruit de pierre ; ceux dessus s'envolent : bien.
68. La Mine : le « bip » qui accélère quand quelqu'un approche.
69. La Boule de glace : la victime ralentie avec du givre sur le corps.
70. L'Onde : un anneau de poussière au sol.
71. Le Bond : la poussière soulevée au décollage.
72. L'Échange : un « fwip » + une fumée violette aux deux endroits.
73. Le Rappel : un rembobinage visible (silhouette fantôme qui retourne en arrière).
74. Le Souffle : la vague qui traverse l'île (150 m) : un des meilleurs clips (huit haricots qui s'envolent d'un coup). Compter « EMPORTÉS : x5 ».
75. La Tornade : les haricots qui tournent dedans, yeux en spirale.
76. Le Météore : le saut puis l'écrasement : l'écran du lanceur doit trembler fort.
77. Le Boulet de canon : le « BOUM » de canon au départ.
78. La Toupie : le haricot qui tourne, yeux qui tournent.
79. Le Gant de boxe : le « DING » de ring.
80. Le Cri : le haricot ouvre une bouche (il n'en a plus !) — les yeux qui se plissent suffisent.
81. Le Déluge : les météores qui tombent avec des sifflements différents.
82. La Traînée de feu : le haricot qui court avec du feu derrière lui : clip « Ghost Rider ».
83. Le Pogo : les rebonds avec un « boing » rythmé.
84. Le Geyser : la victime propulsée en l'air sur un jet d'eau.
85. La Catapulte : le haricot qui vole en hurlant.
86. La Bataille d'oreillers : des plumes partout.
87. Le Coup de pied : l'écran qui se fige un instant au contact.
88. Les passives qui se déclenchent (Riposte, Kamikaze, Armure) : un moment clippable quand elles retournent un combat.
89. Le Kamikaze : l'explosion quand on le pousse : « tu croyais m'avoir ».
90. L'Ange gardien : le retour depuis les nuages avec un halo : « miracle ».
91. Le Pickpocket : invisible après le vol : « où il est passé ? ».
92. Le Saut sur la tête (Mario) : le « boing » + le haricot écrasé en crêpe.
93. Le Home run : la batte (le son « tok » de baseball) et l'étoile qui s'envole.
94. Les divines : chacune un clip en soi. La caméra de chacun pourrait se tourner une demi-seconde vers une divine lancée à moins de 60 m (comme la tête qui se tourne vers qui t'a eu).
95. Les sons des capacités : beaucoup sont fabriqués par le code. Les remplacer par des sons Kenney/achetés pour les 20 plus clippables. `[PHASE 5]`
96. Les clips verticaux (TikTok) : l'action est au centre de l'écran en 1ʳᵉ personne : bon pour le recadrage 9:16. Garder l'action au centre (les alertes au centre aussi).
97. Le pseudo en grand du gagnant (or) : excellent pour le clip.
98. La fin de manche filmée : il faut 1 s de plus avant de couper vers le podium.
99. Un compteur de spectateurs ? Non (pas de serveur).
100. Le clipper conclut : le jeu a maintenant de quoi faire un clip par minute en Mode Dieu, un toutes les trois minutes en normal. Ce qui manque le plus : **des sons uniques** pour les capacités phares, et **une raison de regarder le replay** (le moment du match au podium).

---

## Les dix à faire en premier

1. ~~Plafonner la vitesse de projection~~ (logicien 9) — `[FAIT v42]`.
2. ~~La Rage gaspillée par une poussée dans le vide~~ (logicien 64) — `[FAIT v42]`.
3. ~~Un écran « match nul » clair~~ (logicien 6, gamer 42) — `[DÉJÀ LÀ]` : la Couronne barrée au podium.
4. **Tester le sacre contre l'embuscade** (gamer 12-13) : s'il est impossible, 2,5 s. `[TEST]`
5. **Des sons uniques pour les 10 capacités les plus clippables** (clipper 95). `[PHASE 5]`
6. ~~L'ombre sous soi en vol~~ (gamer 9) — `[FAIT v43]` : un anneau lumineux au sol sous toi dès 4 m.
7. ~~La couleur du rang dans le cercle magique du lanceur~~ (designer 59) — `[FAIT v43]`.
8. ~~Une seule lumière par manche en Mode Dieu~~ (designer 47) — `[FAIT v43]`.
9. ~~Le moment du match rappelé au podium~~ (clipper 21) — `[FAIT v43]`.
10. **Tester les manches 8 à 10** (logicien 55) : la tour y va deux fois plus vite ; plafonner plus bas si c'est injouable. `[TEST]`
