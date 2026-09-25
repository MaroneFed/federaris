# LA SAISON — la bible du jeu

> Réécrite le 26/09/2026, après la refonte demandée par Martin ce jour-là : « je
> m'ennuie, y a pas assez d'action », « tu récoltes du bois, tu sais même pas pourquoi »,
> « les bots sont censés être des vrais joueurs, t'es pas censé leur parler, les gardes
> t'es pas censé les acheter non plus ». C'est **la référence** : quand le code et ce
> document ne disent pas la même chose, c'est un des deux qu'il faut corriger.
>
> Les versions précédentes (le mage, la relique, les talismans, les quatre victoires,
> la Malédiction, le soudoiement) sont dans l'historique Git.

---

## L'idée en une phrase

**Quatre chercheurs dans une forêt noire, autour d'un château gardé. Le plus d'or
(★) sur sa stèle quand la cloche sonne, au bout de trente minutes, gagne.**

On l'obtient de trois façons, de la plus sûre à la plus risquée :

1. **La forêt** — la pierre-lune dans les creux, les coffres enfouis aux lieux-dits.
2. **Le château** — ses trésors, étage par étage, jusqu'à la Couronne tout en haut.
3. **Les autres** — piller leur stèle quand ils n'y sont pas, les détrousser en
   chemin.

Et on le protège : pièges, barricades, alarmes autour de sa stèle, épée à la main.

---

## Le butin (★)

Tout ce qui compte se compte en étoiles. **On ne marque que ce qu'on a DÉPOSÉ à sa
stèle** : le butin porté pèse, et on le lâche si on tombe.

| D'où | Combien | Où |
|---|---|---|
| Pierre-lune | ★2 chacune | les creux de la forêt, qui luisent (24 creux) |
| Coffre enfoui | ★8 | un par lieu-dit (le Grand Chêne, le Cercle, la Cabane, le Tertre, la Tour) |
| Calice | ★5 | la cour du château, le rez-de-chaussée et le 2e étage du donjon |
| Coffret | ★12 | les étages du donjon |
| **La Couronne** | **★40** | **la terrasse du donjon — une seule** |
| Autel de l'Or / de la Lune | ★3 / ★2 toutes les 40 s | directement dans la stèle de qui les tient |
| Garde abattu | ★3 | sa bourse |
| Revenant abattu | ★6 | ses haillons |
| Stèle pillée | **la moitié de son or** | E maintenu 3 s, quand son maître est à plus de 9 m |

Les trésors reviennent : un calice deux minutes après avoir été pris, un coffret trois,
la Couronne cinq, un coffre de lieu-dit quatre. Le butin pèse 350 g l'étoile : la
Couronne, 14 kg.

---

## À quoi sert ce qu'on ramasse

C'était la plainte : on ramassait sans savoir pourquoi. Maintenant, une ressource,
un usage :

| | **Bois mort** | **Pierre-lune** | **Fer ancien** |
|---|---|---|---|
| Où | faisceaux au pied des arbres morts, troncs couchés, arbres abattus | les creux | les réserves du château (sous le nez des gardes) |
| Pour quoi | **construire** (touche T) | **l'or** : ★2 déposée | **les armes** et les pièges de fer |
| Poids | 1 kg | 3 kg | 2 kg |

---

## Le château (refait le 26/09 : « faut mettre des étages, plein de gardes »)

- **La cour** : un calice sur son socle au centre, trois réserves de fer le long des
  murs, la grande porte au sud, **la poterne ouverte au nord** (l'entrée des discrets).
- **Le donjon**, trois niveaux et une terrasse, reliés par des escaliers droits qui
  alternent (mur nord, mur sud, mur nord) : pour monter, il faut traverser chaque salle.
  - rez-de-chaussée (0,9 m) : la grande salle, le banquet, deux calices ;
  - 1er étage (7,2 m) : l'armurerie, deux coffrets ;
  - 2e étage (13,5 m) : la salle des coffres, un coffret, un calice ;
  - terrasse (19,8 m), à ciel ouvert : **la Couronne**.
- Une tour de guet de 42 m au coin de la terrasse : la silhouette qu'on devine de loin.

### Les gardes (quinze)

On ne leur parle pas, on ne les achète pas. **Ils gardent.**

- Huit dans la cour (porte, réserves, rondes, poterne), un par niveau du donjon, et
  **trois rôdeurs** qui tournent dans la forêt autour du château.
- Leur lanterne est un cône : ce qu'elle éclaire, c'est ce qu'ils voient.
- Ils s'intéressent à qui est **dans le donjon ou une réserve**, et à qui **porte du
  butin** dans l'enceinte (les rôdeurs : partout autour). « ? » au-dessus de la tête,
  puis « ! » : il court.
- **Prendre un trésor fait du bruit** : les gardes du même étage accourent.
- S'il te rattrape, il frappe (quatre coups te couchent). On peut le semer (brume,
  murs, étages), ou **le tuer** : quatre coups d'épée. Il revient 90 s plus tard.

---

## Construire (touche T) — « un menu facile, un truc sympa »

Une barre de cinq cases ; 1 à 5 pour choisir, un fantôme vert/rouge devant soi, la
molette pour tourner, clic pour poser, clic droit pour fermer.

| | Coût | Effet |
|---|---|---|
| **Piège** | 3 bois, 1 fer | qui marche dessus tombe et lâche tout ; invisible à plus de 3,5 m (4 au plus) |
| **Barricade** | 5 bois | un mur de pieux de 3 m ; il faut le casser (3 coups) pour passer (6 au plus) |
| **Alarme** | 2 bois | un fil à clochettes : il sonne, la boussole montre où (4 au plus) |
| **Épée** | 2 bois, 3 fer | dans la main |
| **Hache** | 3 bois, 1 fer | dans la main |

On part avec une épée (1) et une hache (2). Elles s'usent.

---

## Les arbres géants (« des arbres plus hauts pour voir au loin »)

Douze sapins immenses, avec des échelons. F au pied : on grimpe jusqu'à la plate-forme,
à 15-30 m. **En hauteur, la brume s'ouvre** (au-dessus de 8 m, elle s'éclaircit
jusqu'à un cinquième de son épaisseur) et la carte se dessine sur un large rayon. La
terrasse du donjon fait le même effet. Les géants sont toujours sur la carte (M).

---

## Les rivaux : de futurs joueurs

Trois PNJ, **avec exactement les mêmes règles que toi** (classe `Seeker`) : en
Phase 3, de vrais joueurs prendront leur place. On ne leur parle donc pas. Ils
ramassent la pierre-lune, **montent au donjon par les escaliers** (les plus hardis
jusqu'à la Couronne), rapportent, pillent les stèles qu'ils connaissent, posent des
pièges, cassent les barricades qui les gênent, se battent, fouillent les dépouilles.
Ils ont une voix (un cri, un murmure), pas de bulles de texte.

- **Mahaut la Rousse** : pilleuse, agressive.
- **Oswin le Borgne** : monte au donjon, vise la Couronne.
- **Guerin des Marais** : reste dans la forêt.

---

## Ce qui fait monter la tension (26/09, « défonce tout »)

- **Qui porte la Couronne est vu de tous** (boussole, carte) et les rivaux armés à moins
  de 90 m lui tombent dessus, jusqu'à ce qu'il la dépose.
- **À 3:00 de la fin, tous les coffres se remplissent** d'un coup : le sprint final se
  joue au château.
- Porter beaucoup d'or se sent de loin : les rivaux armés viennent chercher qui porte
  ★15 ou plus à 35 m.
- **Deux éclats de pierre-lune près de chaque stèle** : la première minute a déjà
  quelque chose à faire.
- Déposer se **sent** (gerbe d'or, secousse, ta pastille du classement qui s'allume) ;
  chaque coup d'épée qui touche fige le temps un vingtième de seconde.
- Quand on te court après, les bords de l'écran battent en rouge.
- **Confort** : champ de vision 78° (au lieu de 62°), balancement de tête divisé par
  deux — c'était la cause probable du mal de tête. On peut toujours courir.
- **Presque plus de texte** : les touches sont dessinées sous le réticule (F ↑), les
  messages se limitent à l'essentiel, l'objectif tient en trois mots.

## Le reste

- **Chacun sa stèle**, fixe, tirée au hasard ; on naît à côté. Un **fil d'or**, que toi
  seul vois, monte au-dessus d'elle. Près d'elle, la vie remonte vite.
- **Tomber** : tout ce qu'on porte (sac, butin, outils) reste dans une dépouille ; on se
  relève à sa stèle 5 s plus tard. La dépouille est sur la boussole et la carte.
- **Loups** (deux meutes) et **revenants** (autour des trois Autels) attaquent tout le
  monde. **Les Autels** paient qui les tient.
- **Camp et caches** : C plante le camp (une fois), G creuse une cache (trois).
- **Boussole et carte (M)** : château, ta stèle, ton camp, tes caches, ta dépouille,
  les stèles rivales connues, le voleur de ton or, tes alarmes quand elles sonnent.
- **L'écran** : la boussole, l'horloge, le classement des quatre (★ sur chaque stèle),
  l'inventaire en cases, une seule ligne d'objectif, des messages courts à gauche.
- **Le récit d'ouverture** : cinq phrases qui passent seules.

---

## Ce qui a été retiré le 26/09/2026, et pourquoi

| Retiré | Pourquoi |
|---|---|
| Le mage, la forge, la relique | trop d'étapes entre « je ramasse » et « je marque » |
| Les talismans, la couronne devant le château, le trône, les quatre victoires | « même moi je sais pas pourquoi il y a une couronne » |
| La Malédiction | un chronomètre qui punit sans action |
| Les améliorations, la besace (Tab) | « ça donne mal au ventre à lire » |
| L'Ermite, le Veilleur, le Registre, parler aux rivaux | « t'es pas censé leur parler » |
| L'or pour soudoyer, la poterne achetée, le serment | « les gardes, t'es pas censé les acheter » |

---

## Ce qui est dans la Phase 1 (solo) — et ce qui n'y est pas

**Porte 1** : une Saison solo de 30 minutes est-elle haletante du début à la fin ?

Dedans : tout ce qui est décrit ci-dessus.

Pas dedans : l'arc, les autres pièges (fosse), les vrais joueurs (Phase 3).
