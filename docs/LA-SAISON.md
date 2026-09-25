# LA SAISON — la bible du jeu

> Écrit le 23/09/2026 à partir de ce que Martin a demandé ce jour-là, en prolongeant
> l'« option B » qu'il avait validée le 21/09. C'est **la référence** : quand le code et
> ce document ne disent pas la même chose, c'est un des deux qu'il faut corriger.

---

## L'idée en une phrase

**Une forêt noire autour d'un château mort. Un mage errant forge des reliques avec ce
qu'on lui apporte. À la fin de la Saison, la plus puissante relique posée sur la stèle
du château l'emporte.**

La phrase de l'écran-titre la résume : *« Ce que tu caches, un autre le cherche. »*

---

## Pourquoi cette boucle, et pas juste la liste des idées

Martin a donné des ingrédients : un énorme château, un camp caché, des caches
creusées, des pièges, un mage qui apparaît au hasard, une stèle, des PNJ. Une liste
d'ingrédients n'est pas un jeu. Ce qui en fait un jeu, c'est que **chaque ingrédient
crée une tension avec un autre** :

| Ingrédient | Il existe parce que… |
|---|---|
| **Le poids** (60 kg, on ne court plus chargé) | on ne peut pas tout porter → il faut **cacher** |
| **Les caches** | ce qu'on cache, il faut le **retrouver** dans une forêt sans repères, et un autre peut le **trouver** |
| **Le mage errant** | il n'accepte que ce qu'on **porte** → il faut aller rechercher sa cache, se charger, et traverser la forêt lentement, **vulnérable** |
| **La relique** | elle concentre tout ce qu'on a amassé en **un seul objet**, qu'on peut perdre d'un coup |
| **La stèle** | elle est **au château** → tout le monde converge au même endroit à la fin |
| **Le château** | il contient la ressource la plus rare → on est obligé d'y entrer avant la fin |
| **Les gardes soudoyables** | c'est le **sabotage par les salaires** : on ne force pas une porte, on **achète** celui qui l'ouvre |

Enlever un de ces éléments casse les autres. C'est le test qu'on appliquera à toute
nouvelle idée.

---

## Le déroulé d'une Saison (30 minutes)

1. **L'arrivée.** On apparaît à la lisière, seul, avec une lanterne. Il faut trouver
   **où planter son camp** — et on ne peut le planter **qu'une fois**.
2. **La cueillette.** La sylve donne deux ressources ; le château, la troisième.
3. **Les caches.** Le sac est vite plein. On **creuse des caches** (trois au maximum)
   pour y déposer. Seul leur propriétaire sait où elles sont.
4. **Le mage.** Il apparaît à un endroit au hasard, **pour deux minutes et demie**,
   puis disparaît. On ne le voit pas de loin — **on l'entend** : un bourdonnement grave
   qui porte à travers la brume. Il faut le trouver, les bras chargés.
5. **La forge.** Le mage fond tout ce qu'on porte en **une relique**. On peut revenir
   la renforcer à chacune de ses apparitions.
6. **La stèle.** Au cœur du château. La relique n'y compte qu'une fois **posée**.
7. **La cloche.** À la trentième minute, la Saison s'achève. La plus puissante relique
   posée sur la stèle gagne.

### Les apparitions du mage

| | |
|---|---|
| Première apparition | **2:00** |
| Intervalle entre deux apparitions | **4:30** |
| Durée de chaque apparition | **2:30** |
| Dernière apparition | **24:30** → il reste 3 minutes pour rejoindre la stèle |

Six apparitions par Saison. La rareté du mage **est** la tension : le rater, c'est
attendre quatre minutes et demie.

Il apparaît **à 110–260 m de toi**, dans une place dégagée, jamais au château ni dans
un creux. On ne le voit qu'à 14 m (la brume) ; sa voix, elle, s'entend à deux cents
mètres.

**Comme un largage** (Martin, 24/09 : « comme un drop sur Fortnite ») : **45 secondes
avant** son arrivée, une colonne de lumière bleue monte au-dessus des arbres à
l'endroit exact où il descendra, et un point bleu apparaît sur la boussole de **tout le
monde**. Tout le monde court au même endroit, rivaux compris : c'est là que les
chemins se croisent. Après ses premières secondes, le point disparaît (sauf avec la
Corne d'appel).

**Après chaque forge, il murmure un secret** : l'emplacement d'une chose utile (un
talisman, un creux riche, la stèle d'un rival…), qui s'ajoute à la boussole en violet.

**Il ne prend que ce qu'on porte, mais on peut faire plusieurs voyages** tant qu'il
chante. C'est ce qui donne leur sens aux caches en solo : une cache pleine près de
l'endroit où il apparaît, c'est un deuxième sac.

### Ce que dit la simulation (`python3 Tools/saison.py`)

| Joueur type | Puissance médiane | Ce que dit l'écran de fin |
|---|---|---|
| Flâneur (3 apparitions, pas de fer) | ~140 | un fétiche |
| Régulier (rate une apparition sur trois) | ~990 | un trésor de mage |
| Expert, six apparitions, sans caches | ~1340 | un trésor de mage |
| Expert qui se sert de ses caches | ~1550 | **une légende** |

Paliers : babiole < 120 ≤ fétiche < 400 ≤ relique < 800 ≤ trésor < 1450 ≤ légende.
La légende **exige** les caches : c'est voulu, c'est la mécanique qu'on veut qu'il
apprenne.

---

## Les trois ressources

La décision verrouillée « trois ressources en v1 » tient. Elles changent de nom et de
lieu, pas de nombre.

| | **Bois mort** | **Pierre-lune** | **Fer ancien** |
|---|---|---|---|
| Où | faisceaux dressés au pied des arbres morts (un appui : six branches), troncs couchés | pierres qui luisent, **dans les creux** | les réserves **du château** |
| Poids | 1 kg | 3 kg | 2 kg |
| Valeur pour le mage | 1 | 4 | 10 |
| Abondance | partout | rare, on la voit briller | **54 lingots pour toute la Saison**, ne reviennent pas ; gardés en Phase 2 |

**La relique** vaut la somme des valeurs, multipliée par un bonus de **variété** :
×1 pour une seule ressource, ×1,25 pour deux, **×1,6 pour les trois**. Sans ce bonus,
tout le monde ne ferait que du fer ; avec lui, il faut **les trois lieux**.

Le fer du château **ne revient pas**. Quand il revenait (toutes les deux minutes), la
simulation montrait que le meilleur plan était la navette château–mage, sans jamais
entrer dans la forêt. Fini, il devient une course : qui vide les réserves le premier.

---

## Quatre façons de gagner (décidé par Martin le 24/09/2026 : « comme dans Civilization »)

| Victoire | Comment | Quand |
|---|---|---|
| **La Relique** | la plus puissante relique posée sur **sa** stèle | à la cloche |
| **La Trahison** | acheter le **serment** des six gardes (44 à 156 or chacun, ~575 or en tout) | immédiate |
| **La Couronne** | réunir les **six talismans**, puis s'asseoir sur le **trône** du donjon | immédiate |
| **L'Offrande** | déposer au **Registre** 60 bois mort, 20 pierres-lune, 10 fer ancien | immédiate |

Les trois victoires immédiates sont des **courses contre la cloche** : si personne n'en
décroche une, la Relique tranche. La besace (**Tab**, onglet « Victoires ») montre où l'on
en est sur chacune. L'or vient des bourses perdues (40 au départ, une près de chaque
lieu-dit, 24 dans la forêt) : la Trahison demande presque toutes les bourses.

---

## Chacun sa stèle, trois rivaux, et le vol (décidé par Martin le 24/09/2026)

Martin : « je veux que chacun ait sa stèle et qu'il la pose quelque part sur la map »,
« on doit pouvoir voler ». Ce qui était prévu en Phase 2 arrive maintenant, **contre des
rivaux PNJ** — les mêmes règles serviront telles quelles en multijoueur.

- **Ta stèle** : **fixe, tirée au hasard à chaque partie** (révisé le 25/09), entre 110
  et 290 m du centre, à 120 m au moins des autres. **On naît à côté d'elle**, et rien ne
  l'indique ensuite : il faut retenir le chemin. À la cloche, seule compte la relique
  posée sur **ta** stèle.
- **Le Registre**, au château (l'ancienne stèle) : il grave le classement de tous.
- **Trois rivaux** : Mahaut la Rousse (pilleuse), Oswin le Borgne (le fer du château),
  Guérin des Marais (les creux, presque honnête). Ils récoltent aux **mêmes gisements**
  que toi, courent au mage, forgent, posent leur relique. On voit leur lanterne dans la
  brume.
- **Voler** : devant la stèle d'un rival, E maintenu 3 s. Impossible s'il la garde (à
  moins de 9 m). On peut aussi **détrousser** un rival qui récolte avec sa relique sur lui.
  La relique volée pèse 8 kg ; il faut la porter à **ta** stèle pour la fondre dans la
  tienne — **60 %** seulement : le reste se perd.
- **Être volé** : un rival qui est passé près de ta stèle s'en souvient. Quand tu es loin,
  il vient la piller — ta relique, et ta réserve. Une alerte dit de quel côté il file :
  rattrape-le, E pour reprendre.
- **Être chassé** : si tu voles un rival, il te poursuit. S'il te rattrape avant que tu
  aies fondu sa relique, il la reprend.

---

## La stèle, ta réserve — et la Malédiction (décidé par Martin le 25/09/2026)

Martin : « la stèle, c'est juste pour déposer nos trucs, c'est notre marché », « toutes
les X minutes, une malédiction enlève tout ce qu'il y a dans notre sac, ce qui force à
tout déposer dans la stèle — mais si elle est sans surveillance, c'est mauvais ».

- **La réserve** : E devant ta stèle ouvre trois onglets — **Réserve** (déposer, reprendre,
  400 kg), **Relique** (la poser, la reprendre pour le mage, fondre une relique volée),
  **Améliorations**.
- **La Malédiction** tombe **75 s après chaque départ du mage** : 5:45, 10:15, 14:45,
  19:15, 23:45, 28:15. Tout ce qui est dans les sacs — le tien, ceux des rivaux — est
  dévoré. Pas la relique, pas l'or, pas les outils, pas les caches, pas la réserve. Un glas
  sonne à 60, 30 et 10 s ; le compte à rebours s'affiche sous l'horloge.
- **Le rythme qui en naît** : récolter → rentrer déposer avant le glas → reprendre sa
  réserve quand la colonne du mage monte → forger → reposer. Six fois par Saison.
- **Le piège qui en naît** : une réserve pleine attire. Qui trouve ta stèle sans toi à côté
  la pille (E maintenu 3 s : ta relique en trophée, et tout ce qui rentre dans son sac).

## S'orienter : boussole, carte, fil d'or (révisé par Martin le 26/09/2026)

Le 25/09, la boussole avait été vidée (« comme ça ça force à retenir »). Le 26/09, Martin
revient dessus : « on se perd complètement dans la map… si on ne se souvient pas où est
la stèle, ni le château, c'est bof ». Donc :

- **La Sylve fait 420 × 420 m** (au lieu de 700). Toutes les distances de placement ont
  suivi : stèles à 75-165 m du centre, mage à 60-150 m, deux meutes de loups, 24 creux.
- **La boussole** montre ce que TU sais : le château, ta stèle, ton camp, tes caches, ta
  dépouille, les lieux-dits découverts, les stèles rivales trouvées, le voleur de ta
  relique, le mage pendant sa descente. Ce qui est à plus de 150 m s'estompe.
- **La carte (touche M)** : un parchemin vu de dessus, qui ne dévoile que ce qu'on a
  parcouru (et d'office les abords du château). Avec une légende.
- **Le fil d'or** : un mince trait de lumière au-dessus de ta stèle, que toi seul vois. Il
  brille plus fort quand la Malédiction approche et que ton sac n'est pas vide.

**Le filet de sécurité (26/09)** : touche **H**, « tendre l'oreille ». Ta stèle joue trois
notes claires, en 3D, audibles à 400 m : on sait de quel côté elle est, pas à quelle
distance. Une fois par minute. Et près de sa stèle (8 m), la vie remonte six fois plus
vite : c'est le seul endroit où l'on est chez soi.

## Les améliorations (décidé par Martin le 25/09/2026)

Payées avec la **réserve** de la stèle (pas le sac), et parfois de l'or :

| Amélioration | Effet | Prix |
|---|---|---|
| Besace renforcée (×2) | +20 kg dans le sac | 10 bois, 2 lune — puis 16 bois, 4 lune, 10 or |
| Amulette du glas | la Malédiction laisse la moitié du sac | 4 lune, 2 fer, 15 or |
| Sentinelle | ta stèle sonne quand un rival rôde, et dit où elle est | 8 bois, 2 lune |
| Verre poli | lanterne +35 % de portée | 3 lune, 1 fer |
| Lame trempée | épée +40 % | 4 fer, 10 or |
| Bottes de cerf (×2) | +10 % de vitesse | 8 bois, 1 lune, 10 or — puis 12 bois, 2 lune |
| Collets (×2) | +2 pièges posés en même temps | 6 bois, 2 fer — puis 8 bois, 3 fer |

## Les pièges (décidé par Martin le 25/09/2026)

« Construire des pièges : quand un gars va dessus, il meurt et perd tout son stuff. »
Artisanat (Tab) : **3 bois mort, 2 fer**. En main, clic : posé devant soi. Qui marche
dessus — rival, joueur, loup, revenant — meurt sur le coup et lâche tout dans sa dépouille.
Invisible au-delà de 3,5 m (sauf les tiens). Usage unique, 3 posés au plus (+2 par Collets).
Les rivaux agressifs piègent les abords de leur propre stèle.

## Les Autels (décidé par Martin le 25/09/2026)

« À côté du château, des monuments qui, une fois contrôlés, te paient avec de l'or ou du
bois — sans que ça devienne cheaté. » Trois, hors des murs :

| Autel | Où | Rapporte (toutes les 40 s) |
|---|---|---|
| de l'Or | à l'ouest | 5 or |
| du Bûcheron | à l'est | 4 bois mort, dans ta réserve |
| de la Lune | au nord | 2 pierres-lune, dans ta réserve |

Le prendre : **10 s seul** dans le cercle. À deux, rien n'avance ; un autre efface d'abord
ta marque. **Deux revenants** le gardent : tant qu'ils sont debout près de la pierre, il ne
se prend pas. Les rivaux armés viennent parfois l'assiéger.

## Ce qui veut ton mal (décidé par Martin le 25/09/2026)

- **Les loups** : trois meutes de trois, repaires tirés au hasard loin des stèles. Yeux
  jaunes dans la brume, hurlement quand ils chassent. Plus rapides que toi à pied, moins
  qu'à la course (à vide) : chargé, il faut se battre. 45 PV (deux coups d'épée), 14 par
  morsure. Ils ne montent pas aux arbres.
- **Les revenants** : deux par Autel. Lents, 90 PV, 22 par coup, 6 or chacun. Ils ne
  s'éloignent pas de leur pierre.
- Les bêtes reviennent à leur repaire 2 min 30 à 3 min après leur mort.

---

## La garde du château et la poterne (le différenciateur, arrivé le 24/09/2026)

Martin : « je veux vraiment des gardes au château ». Six gardes, six soldes :

| Garde | Poste | Loyauté | « Regarde ailleurs » | « Ouvre la poterne » |
|---|---|---|---|---|
| Bertrand | grande porte | très faible (5 mois d'arriéré) | 8 or | 37 or |
| Aubin | grande porte | forte | 46 or | 109 or |
| Lambert | réserve ouest | faible | 23 or | 65 or |
| Jehan | réserve est | incorruptible ou presque | 57 or | 128 or |
| Thibaut | réserve nord | moyenne | 34 or | 84 or |
| Enguerrand | ronde de la cour | faible | 16 or | 51 or |

- **Ce qui les alerte** : du **fer ancien** sur toi, ou un pied **dans une réserve**. On
  traverse la cour librement. Leur **cône de lanterne** est exactement ce qu'ils voient.
  « ? » quand ils se doutent, « ! » quand ils courent.
- **Rattrapé** : ils prennent tout ton fer et te jettent devant la grande porte. Chargé de
  fer, tu cours moins vite qu'eux.
- **Soudoyer** : 3 minutes d'aveuglement pour quelques pièces ; ou la **poterne** du mur
  nord, qui ne s'ouvre **que** comme ça — jamais par la force. Une fois ouverte, elle le
  reste : c'est un raccourci discret vers la forêt du nord.
- **L'or** (40 au départ) ne sert qu'à ça. On en trouve dans les **bourses perdues** : une
  près de chaque lieu-dit, une quinzaine dans la forêt.
- Les rivaux volent du fer aussi : les gardes les jettent dehors pareil.

---

## Les talismans et les lieux-dits (ajoutés le 24/09/2026)

Martin : « je veux des items incroyables ». Six **talismans**, uniques, qu'on trouve une
fois par Saison. Ce ne sont **pas des ressources** : on ne les récolte pas, ils ne
pèsent rien, la règle des trois ressources tient.

| Talisman | Où | Ce qu'il fait |
|---|---|---|
| Lanterne ardente | sur le trône, dans le donjon | la lanterne éclaire jusqu'à 21 m |
| Corne d'appel | le Grand Chêne | quand le mage chante, la boussole le montre |
| Cœur de lune | le Cercle de pierres | chaque pierre-lune ramassée en donne deux |
| Besace cirée | la cabane du braconnier | +15 kg dans le sac |
| Pelle d'os | le Tertre | creuser 3× plus vite, et une cache de plus |
| Couronne sans tête | l'allée des rois | la relique posée vaut +15 % |

Les **lieux-dits** sont cinq endroits de la sylve (le Grand Chêne, le Cercle de
pierres, la cabane du braconnier, le Tertre, la Tour effondrée), placés par la graine,
à 150 m au moins les uns des autres. Une fois découverts, ils restent affichés comme
repères : dans une forêt où tout se ressemble à vingt mètres, c'est ce qui permet de
dire « ma cache est entre le Chêne et le Cercle ».

Pourquoi c'est bon pour la Porte 1 : entre deux apparitions du mage, il y avait un
temps mort. Il devient de l'exploration.

---

## Les habitants (ajoutés le 24/09/2026)

Martin : « je veux des PNJ incroyables ». Les PNJ de la Phase 1 **ne se battent pas** (le
combat est en Phase 2) : ils parlent, guident, et donnent de la valeur à ce qui en
manquait.

| Qui | Où | Ce qu'il apporte |
|---|---|---|
| **Le mage** | au hasard, six fois | la forge (inchangé) |
| **Le Veilleur** | ronde autour de la stèle | dit où chante le mage, où dorment les talismans manquants, si ta relique compte. Et : « On me payait, avant. » — il annonce les gardes soudoyables de la Phase 2 |
| **L'Ermite** | la Tour effondrée | indique le lieu-dit le plus proche jamais visité et le creux le plus proche. **Infusion** : 12 bois mort → 3 minutes où le poids du sac ne ralentit plus les gestes. C'est ce qui donne du prix au bois mort |
| **Les feux-follets** | neuf, dans la sylve | approche-toi : ils s'éloignent, t'attendent, et te mènent au creux à pierres-lune le plus proche |
| **Le cerf blanc** | devant toi, à la limite de la brume | aucune utilité. Il te regarde, puis s'enfuit. C'est ce dont on se souvient |

Les gardes et les rivaux sont arrivés depuis (voir plus haut). Le rôdeur qui pille les
caches et les pièges restent en Phase 2.

---

## Les outils et le combat (ajoutés le 24/09/2026)

Martin : « on peut se créer des petites épées, tuer un gars… celui qui porte la relique
ne peut pas attaquer ». Et : « couper l'arbre, il faut une hache, la hache se casse ».

- **Deux emplacements** (touches **1** et **2**), rien de plus : il faut choisir.
- **La hache** s'use et casse. Elle abat un arbre (clic maintenu) ; l'arbre tombe et
  laisse un tas de bois mort. C'est la façon rapide de faire du bois — et c'est bruyant.
- **L'épée** (2 bois mort, 3 fer ancien) : quatre coups tuent. Elle s'use aussi.
- **Qui porte une relique ne peut pas frapper.** Il ne peut que fuir. C'est ce qui rend
  le trajet mage → stèle dangereux, et c'est voulu.
- **Tomber**, c'est lâcher tout ce qu'on porte dans une dépouille (que n'importe qui
  peut fouiller), puis se relever à sa stèle.
- **Grimper dans un arbre** (touche **F** près d'un tronc) : on voit arriver les gens,
  on n'est pas vu d'en bas.

L'outil tenu se voit à l'écran, **sans mains** (voir CLAUDE.md, le corps invisible).

---

## Le son, les corps, la pierre (24/09/2026)

- **La musique** change d'humeur toute seule : écran-titre, forêt, **tension** (le mage
  descend, un garde court, on est blessé, un voleur file, la cloche approche), fin. Les
  morceaux de Martin vont dans `Assets/_Fief/Resources/Music` — le **nom du fichier** dit
  l'humeur (`titre…`, `tension…`, `fin…`, sinon forêt). Sans fichier, le jeu fabrique
  une musique sobre (nappes en ré mineur, bourdon et battement de cœur).
- **Les PNJ marchent** : squelette articulé (`Walker`), robes en pans qui suivent les
  jambes, hallebardes et lanternes qui restent droites, tête qui te suit. Les rivaux
  portent **le corps du joueur** à leur couleur : en multijoueur, ce seront des joueurs.
- **De vrais modèles** peuvent remplacer ces corps : un pack CC0 (KayKit, Quaternius,
  Kenney) déposé dans `Assets/_Fief/Resources/Modeles/Gardes` (ou `Mage`, `Ermite`,
  `Veilleur`) est chargé, mis à la bonne taille et animé automatiquement.
- **Le château** fait maintenant 15 m de courtines, 30 m de tours, 48 m de donjon sous
  un toit d'ardoise, avec contreforts et mâchicoulis, et toute sa pierre est
  **appareillée** (texture de pierres taillées et relief, fabriqués au lancement).
- **Les polices** : Cinzel (titres) et EB Garamond (texte), licence OFL, dans
  `Assets/_Fief/Resources/Fonts`.

---

## L'écran : peu de mots (Martin, 26/09/2026 : « il y a trop de texte, ça donne pas envie »)

- **En haut** : la boussole, et dessous trois pastilles sans phrase — le mage (point
  bleu), la cloche (le chrono), la Malédiction (triangle violet).
- **En bas** : l'inventaire en cases à pictogrammes — deux outils, trois ressources, la
  relique, l'or ; la jauge de poids au-dessus du sac. La molette change d'outil.
- **À droite** : une seule carte d'objectif, puis seulement ce qui presse.
- **À gauche** : les messages, en quelques mots, trois au plus.
- **Personne ne parle en bulles** : rivaux et gardes ont une voix (cri ou murmure),
  pas de texte au-dessus de la tête.
- **Polices** : Grenze (titres) et Alegreya Sans (texte), toutes deux sous licence OFL.
- **Le récit d'ouverture** : cinq phrases courtes qui passent toutes seules (Échap saute).

## Ce qui est dans la Phase 1 (solo) — et ce qui n'y est pas

**Porte 1 redéfinie :** *une Saison solo de 30 minutes est-elle haletante du début à
la fin ?* En solo, l'adversaire est le temps : le mage qui s'en va, la cloche qui
approche, le sac trop lourd.

| Phase 1 — maintenant | Phase 2 — le conflit |
|---|---|
| Les trois ressources et leurs lieux | Les autres pièges : fil d'alarme, fosse |
| Le camp (une fois) et les caches (trois) | **L'arc** |
| Le mage errant, sa colonne, la forge, ses secrets | Un **rôdeur** PNJ qui pille les caches mal protégées |
| La relique, la stèle fixe et sa réserve, la Malédiction, la cloche, quatre victoires | Les gardes qui trahissent **pour les rivaux** aussi |
| Le château, ses six gardes, leur solde, la poterne (demandés par Martin le 24/09) | |
| Trois rivaux, le vol de relique et de réserve | |
| Hache, épée, piège à mâchoires, combat (demandés par Martin les 24 et 25/09) | |
| Les Autels, les loups, les revenants, les améliorations (demandés par Martin le 25/09) | |
| L'écran de fin | |

Ce qui a glissé de la Phase 2 vers la Phase 1 l'a fait **sur demande explicite de
Martin**, les 24 et 25/09.

---

## Le sabotage par les salaires — où il vit maintenant

Le différenciateur du projet (voir `CLAUDE.md`) ne disparaît pas, il **change
d'adresse**. Il n'y a plus de fief par joueur, donc plus de serviteurs à soi. Mais le
château a sa garde, et **elle est mal payée**. En Phase 2 :

- chaque garde a une **solde** et une **loyauté** ;
- un garde mécontent accepte de l'**or** pour ouvrir la poterne, détourner les yeux,
  éteindre une torche ;
- la **porte dérobée ne se force pas** : on l'ouvre de l'intérieur, en achetant
  quelqu'un. La destruction à règles tient toujours.

La meilleure façon d'entrer dans le château n'est pas la force : c'est la trahison
achetée. C'était vrai des fiefs, c'est vrai du château.
