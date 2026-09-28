# Le designer chiant : 100 reproches

*01/10/2026. Martin : « Puis après, tu fais la même chose avec le designer chiant… et avec
100 trucs aussi. »*

Le gamer chiant (`docs/GAMER-CHIANT.md`) râle sur ce qui l'empêche de jouer. Le designer
chiant, lui, râle sur **ce qui fait amateur** : la cohérence, les formes, les couleurs,
la hiérarchie à l'écran, la clarté des règles, la dette du code. Même méthode : Claude a
relu le code et refait les calculs, sans voir le jeu.

Légende : **✔** corrigé dans la v12 · **→** reste à faire (avec la piste), ou décision
qui revient à Martin.

---

## I. La direction artistique

1. **✔ Trois styles se battaient** : du cube (château, Monument, torches, sanctuaires), du rond satiné (personnages, Couronne, arbalestes), des aplats de couleur (l'interface). → Une seule règle : **tout ce qui vit est rond** (gélules, sphères). Monuments, sanctuaires, torches, gargouilles passés au rond.
2. **✔ Trop de choses brillaient** : arcs d'or, blasons, losanges des bannières, fenêtres, runes… Quand tout luit, rien ne ressort — c'est ce qui faisait « IA ». → L'or est **mat** ; la lumière est réservée à ce qui compte (Couronne, Monuments, dons, gargouilles qui chargent).
3. **✔ Le bleu roi des toits était criard** (0,20 / 0,32 / 0,78 : un bleu de jouet). → Ardoise bleu nuit, une nuance différente par tour.
4. **✔ La pierre noire des sanctuaires jurait** avec l'île claire. → Le marbre clair des Monuments.
5. **→ Pas de post-traitement** (bloom, occlusion ambiante, étalonnage). Le paquet *Post Processing* d'Unity (gratuit) donnerait d'un coup la douceur « pro ». À faire avec Martin (Window ▸ Package Manager).
6. **→ Pas d'occlusion ambiante** : les pieds des murs et les coins ne s'assombrissent pas, les objets semblent posés. (Même paquet.)
7. **→ La mer de nuages est plate vue du bord** : une deuxième couche plus sombre, plus bas, donnerait la profondeur.
8. **→ Le ciel n'a pas de nuages hauts** (cirrus) : le ciel de fin de journée paraît vide.
9. **→ Trois ors différents dans le code** (`Castle.GoldTrim`, `CardArt.Gold`, `Wings.Gold`). Un seul `Palette.Gold`.
10. **→ La palette des joueurs** (8 couleurs) n'a pas été vérifiée pour les daltoniens (rouge/vert). Un motif de cimier différent par joueur ?

## II. Le château et le décor

11. **✔ Les créneaux étaient troués au hasard** (10 % manquants) : ça faisait abîmé et bâclé sur un château de conte. → Réguliers, chacun **coiffé d'un chaperon** plus sombre.
12. **✔ Les murailles étaient des pans lisses de 18 m.** → Un **bandeau** sous le chemin de ronde, des **meurtrières** sur deux rangs, un **parapet** côté cour.
13. **✔ Les toits étaient des cônes simples.** → Des **toits en cloche** : une jupe évasée qui déborde du mur, puis la pointe élancée.
14. **✔ Les tours de garde des portes n'avaient pas de toit** : une silhouette tronquée. → Toutes les tours ont leur toit (douze pointes) : la citadelle a une vraie silhouette de conte.
15. **✔ Les arcs dorés des portes et le blason luisaient.** → Or mat.
16. **✔ Les torches : un bâton carré et une flamme cubique.** → Poteau rond, coupe de fer, flamme en goutte.
17. **✔ Les fenêtres éclairées luisaient autant qu'en pleine nuit** (1,5) en plein jour. → 0,55.
18. **→ Le château reste fait de primitives** : c'est la limite du « tout par le code ». La vraie solution est la **loi n°1** : le **Castle Kit de Kenney** (gratuit, CC0) — murs, tours, portes, toits modulaires. Voir `docs/MODELES.md` : Claude écrira l'assemblage dès que les fichiers sont dans le projet.
19. **→ Les jardins de la cour** (boules de fleurs, arbres-sucettes) sont d'un autre jeu que le château.
20. **→ La cour est vide entre les jardins** : puits, charrettes, étals (Kenney *Fantasy Town*) la rendraient vivante.
21. **→ Les bannières sont des plaques rigides** : un léger ondoiement (comme les fanions) les rendrait textiles.
22. **→ Les cascades n'ont pas d'écume en bas** ni de bruit qui s'entend en approchant.
23. **→ Les îlots flottants se ressemblent** : un détail propre à chacun (un arbre, une ruine, une statue) aiderait à les nommer.
24. **→ Les plateformes de départ** : un cercle, une dalle, un fanion cubique. Le même soin que les Monuments.
25. **→ Les rochers flottants tournent tous à la même vitesse.**

## III. Les personnages et les créatures

26. **✔ Le chevalier était un bonhomme de neige** : un casque posé sur un tonneau, sans visage (« je n'ai jamais vu des personnages aussi horribles »). → **Un seul haricot à sa couleur**, le visage dedans : deux grands yeux blancs à pupilles noires (et leur reflet), des joues roses, un petit casque d'acier qui descend sur le front, le cimier, la cape, des moufles blanches.
27. **✔ L'écharpe était de la même couleur que le corps** (et maintenant cachée dedans). → Retirée ; le haricot entier porte la couleur.
28. **✔ Aucun moyen d'utiliser un vrai modèle 3D.** → `ModelCharacter` : un personnage animé rangé dans `Resources/Modeles/Personnage` remplace le haricot pour tout le monde (course, saut, coup, danse), teinté à la couleur du joueur. Voir `docs/MODELES.md`.
29. **✔ Les gargouilles : des cubes et des cônes.** → Corps de lion accroupi en sphères, ailes de chauve-souris à trois doigts (os et voiles), cornes qui s'enroulent, queue en crochet, griffes noires, crocs, dans une pierre claire polie.
30. **✔ Les gargouilles étaient « polies » après coup** (`Polish`), en plus de leurs couleurs mates : un rendu au hasard. → Leurs matières sont choisies une à une.
31. **→ Le haricot n'a pas d'expression** : des paupières qui clignent, des yeux qui se plissent à l'effort, qui s'écarquillent quand il tombe.
32. **→ Pas de personnalisation** (couleur, chapeau) : Phase 5 (vitrine).
33. **→ Les bots se reconnaissent à leur couleur seule** : un accessoire par bot (plume, corne, couronne de fleurs) aiderait.
34. **→ Les gargouilles restent immobiles tant qu'elles guettent** : une respiration, un battement d'aile de temps en temps.

## IV. Les effets

35. **✔ L'onde de choc était une boule laiteuse** (un matériau de particules sur une sphère). → Un petit shader (`Resources/Shaders/Onde.shader`) : **centre transparent, bord qui brille** — une bulle d'énergie nette.
36. **✔ Les confettis étaient des étincelles additives** : en plein jour, blanchies, sans couleur. → De **vrais confettis** : des carrés de papier qui tournoient et gardent leurs couleurs.
37. **✔ La victoire n'avait pas de rythme.** → Tout le spectacle bat **sur le temps** de la musique (anneau au sol, pluie de confettis, feux d'artifice, projecteur).
38. **→ Les étincelles additives restent pâles en plein jour.** Même remède que les confettis pour la poussière et les débris (matériau non additif).
39. **→ Pas de traînée d'air derrière qui plane**, vu de l'extérieur (les bots). Un ruban blanc au bout des ailes.
40. **→ L'impact d'une poussée n'a pas de son grave** (le « boum » qui fait sentir la masse).
41. **→ Le respawn est une colonne de lumière** : lisible, mais la même que les Monuments (confusion).
42. **→ Pas de secousse de caméra réglable** (certains joueurs la détestent) : un réglage « secousses ».

## V. L'interface

43. **✔ Les pastilles étaient plates.** → Un liseré sombre, une **ombre franche** (décalée, pas floue), le bas un peu plus sombre, un reflet clair en haut : le « bouton de jeu ».
44. **✔ Les cartes : une plaque de pierre granuleuse, un ruban de texte, un nom, un fleuron, une phrase, une recharge, un filet d'or.** Trop de choses. → Quatre couches lisibles : **fenêtre + icône, nom, phrase, pastilles (touche, recharge)**.
45. **✔ La phrase des cartes était en gris sur pierre sombre** : faible contraste. → **Encre sombre sur cartouche clair.**
46. **✔ La touche d'une carte active était écrite dans un ruban** (« ACTIVE · CLIC GAUCHE »). → L'icône de la souris, dans la fenêtre.
47. **✔ Le dos des cartes était un treillis étiré.** → Bleu nuit, un rond, la Couronne d'or.
48. **✔ Des phrases dans presque tous les menus**, alors que la règle est « zéro texte ». → Salon, pause, fin de manche, choix, podium, en ligne, départage : pastilles et icônes.
49. **✔ Les en-têtes de colonnes n'existaient pas dans les Commandes.** → Un gros rond par colonne (le joueur, la cible, les ailes).
50. **✔ Du code mort dans les menus** (ombres de texte, pieds de page vides, deux textures jamais utilisées). → Retiré.
51. **→ Les boutons de menu gardent un mot** (Jouer, Réglages…). Voulu (CLAUDE.md), mais on pourrait n'avoir que l'icône sur les boutons secondaires.
52. **→ Les réglages sont des lignes « mot ‹ valeur › »** : des curseurs dessinés seraient plus clairs (sensibilité, volume, champ de vision).
53. **→ Pas d'animation d'arrivée des écrans** (les pastilles pourraient glisser et rebondir, comme dans Fall Guys).
54. **→ Pas de son au survol des boutons** (seulement au changement au clavier).
55. **→ La jauge de la tour** (à gauche) est un tube de couleurs sans légende : on la comprend en montant, pas avant.
56. **→ Le fil d'événements** (à gauche) et la jauge de la tour peuvent se chevaucher sur un petit écran.
57. **→ La taille de l'interface suit la hauteur de l'écran** ; en 21:9, le HUD paraît petit et loin du centre.
58. **→ Les icônes des capacités passives et actives ont le même cadre** : une forme différente (carré arrondi pour les passives) aiderait.
59. **→ Aucune icône n'indique le verrou de la Couronne** (3 s après un vol).
60. **→ Le point de visée devient rouge quand un ennemi est à portée** : bien ; mais il est minuscule (5 px à 1080p).

## VI. Les règles (game design)

61. **✔ Tenir la Couronne à la fin gagnait la manche** : on pouvait se cacher deux minutes. → **Plus de victoire au chrono** : il faut le sacre.
62. **✔ La poussée ne décidait rien** (4 m). → Elle **projette** (≈15 m) : la tour sans parapet devient enfin le lieu où l'on éjecte.
63. **✔ Deux gestes différents pour la même intention** (passer sur la Couronne à terre / tenir E sur le socle). → **Un seul** : on passe dessus (E marche aussi).
64. **✔ Tenir E (Couronne, sanctuaire)** : une barre de chargement déguisée. → Plus rien à tenir, nulle part (le sacre se fait en restant dans le cercle, pas en tenant une touche).
65. **→ Les manches nulles vont devenir fréquentes** (plus de victoire au chrono). Proposition : la **prolongation** (voir `v2-ideas.md`). À Martin de trancher.
66. **→ Le vainqueur de la manche choisit en dernier** : bon rééquilibrage ; mais il a déjà la meilleure *position* au départ ? (non : chacun sa plateforme — c'est bon).
67. **→ Les capacités changent à chaque manche** : on ne s'attache à aucune. Proposition : garder la passive si on la reprend (aujourd'hui interdit).
68. **→ Poigne (poussée ×2) devient énorme** avec la nouvelle poussée (≈30 m). À surveiller ; ×1,5 si c'est trop.
69. **→ Ancrage (moitié moins loin) devient la passive la plus forte** pour la même raison.
70. **→ Le Souffle traverse toute l'île (150 m)** : avec des poussées qui projettent, il peut vider la tour d'un coup.
71. **→ Pas de retour de bâton pour qui pousse trop** (spam). La recharge de 0,9 s suffit ?
72. **→ La manche dure 4 à 10 min** ; avec des poussées fortes, une manche risque de s'étirer (tout le monde se renvoie en bas). Chronométrer.
73. **→ Le sanctuaire donne une capacité au hasard** : aucune stratégie. Montrer le don (couleur du cristal) — c'est fait ; le rendre lisible à distance.

## VII. Le terrain (level design)

74. **✔ Les gargouilles des tours d'angle étaient enfouies sous les toits.** → Sur des consoles, côté cour.
75. **→ La citadelle (100 m) occupe la moitié de l'île (200 m)** : peu d'espace pour les courses-poursuites dehors.
76. **→ Les quatre portes et quatre rampes sont identiques** : aucun choix de route. Une rampe plus courte mais plus piégée ?
77. **→ Le sommet est un disque plat** : aucun relief pour se cacher ou se battre.
78. **→ Les Monuments sur les îlots se voient de partout** (colonnes bleues) : bien ; mais on ne voit pas lequel est gardé.
79. **→ Les courants d'air sont tous à mi-chemin île-îlot** : prévisibles. Un courant « piège » qui pousse vers le bas ?
80. **→ La mer de nuages est une limite invisible** (on réapparaît) : une récompense à qui plonge et remonte par un courant ?

## VIII. Le son

81. **✔ Une seule musique pour tout ce qui n'est pas le jeu** (le titre). → Une **musique de danse** pour la victoire.
82. **✔ La musique ne connaissait pas son tempo.** → `MusicDirector.DanceBeat` : le jeu sait où en est le temps.
83. **✔ Des noms d'humeurs d'une autre époque** (« forêt », « le mage descend »). → CALME, TENSION, DANSE…
84. **→ Les morceaux fabriqués sont des nappes** : c'est sobre, mais Martin a promis des musiques. `Resources/Music` : titre, calme, tension, danse, fin.
85. **→ Pas de mixage** (musique / effets / voix) : un seul volume.
86. **→ Pas de son 3D pour les gargouilles qui chargent** (on l'entend partout pareil).
87. **→ Les bruits de pas sont les mêmes sur l'herbe et la pierre** (sauf un froissement dehors).

## IX. Le code (la dette)

88. **✔ `Icons` fabriquait des textures avec mipmaps et filtre trilinéaire** pour une interface. → Sans mipmaps, à la taille, filtrage simple.
89. **✔ `CardArt.Draw` avait deux surcharges et onze paramètres de texte** (ruban, pied de carte…). → Une seule signature, des valeurs (recharge, touche).
90. **✔ `Menus` gardait des fonctions vides** (`Footer`) appelées neuf fois. → Retirées.
91. **✔ `Hud.Tip` avait du code inatteignable** après un `return`. → Il note seulement l'astuce.
92. **→ `Rival.cs` fait 1 200 lignes**, `Menus.cs` 1 500. À couper (le cerveau des bots d'un côté, le corps de l'autre ; un fichier par écran).
93. **→ `FloatingTexts` n'affiche plus rien** depuis la fin de l'aura : à retirer.
94. **→ Les phrases d'astuces restent dans le code** (`Hud.Tip("…")`) alors qu'elles ne s'affichent plus : à remplacer par des icônes, ou à retirer.
95. **→ `UiStyle` porte encore le style « manuscrit » du 26/09** (cadres de cuir, plaques de bronze) que plus rien n'utilise.
96. **→ Les couleurs sont écrites partout en dur** (`new Color(1f, 0.86f, 0.35f)` : l'or de la Couronne, une vingtaine de fois). Un seul endroit : `Palette`.
97. **→ Aucun test automatique** du déroulé d'une manche (prise, vol, sacre). Le vérificateur et le compilateur attrapent la syntaxe, pas la logique.

## X. La production

98. **✔ Pas de mode d'emploi pour les modèles gratuits.** → `docs/MODELES.md` : quels packs (Kenney, Quaternius, tous CC0), où les ranger, quels réglages dans l'Inspector.
99. **→ Le frère de Martin n'a pas encore de tâche claire** : lui confier le choix des modèles (personnage, château) dans ces packs, avec `docs/MODELES.md`.
100. **→ Personne n'a encore vu le jeu tourner sauf Martin** : une partie filmée (OBS) envoyée à Claude, image par image, vaudrait cent lectures de code.
