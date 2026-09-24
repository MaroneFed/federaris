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
un creux. On ne le voit qu'à 26 m (la brume) ; sa voix, elle, s'entend à deux cents
mètres. Un message dit seulement « au nord-est » au moment où il arrive.

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
| Où | fagots au pied des arbres morts | pierres qui luisent, **dans les creux** | les réserves **du château** |
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

- **Ta stèle** : touche **P**, une seule fois, où tu veux (pas dans le château, pas sur
  un lieu-dit). À la cloche, seule compte la relique posée sur **ta** stèle.
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
  il vient la piller. Un marqueur « VOLEUR » le montre : rattrape-le, E pour reprendre.
- **Être chassé** : si tu voles un rival, il te poursuit. S'il te rattrape avant que tu
  aies fondu sa relique, il la reprend.

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

Les gardes, le rôdeur qui pille les caches et les pièges restent en Phase 2 : ils
n'ont de sens qu'avec quelqu'un à arrêter.

---

## Ce qui est dans la Phase 1 (solo) — et ce qui n'y est pas

**Porte 1 redéfinie :** *une Saison solo de 30 minutes est-elle haletante du début à
la fin ?* En solo, l'adversaire est le temps : le mage qui s'en va, la cloche qui
approche, le sac trop lourd.

| Phase 1 — maintenant | Phase 2 — le conflit |
|---|---|
| Les trois ressources et leurs lieux | **Les gardes** du château et leur **solde** |
| Le camp (une fois) et les caches (trois) | **Soudoyer** un garde : la porte dérobée, la torche éteinte |
| Le mage errant, sa voix, la forge | **Les pièges** : collet, fil d'alarme, fosse |
| La relique, la stèle, la cloche | **Voler** une cache, une relique, la stèle |
| Le château, ses réserves, ses torches | Combat simple |
| L'écran de fin | Un **rôdeur** PNJ qui pille les caches mal protégées (donne un sens aux pièges en solo) |

Les pièges et les PNJ sont demandés par Martin et **ils viendront** — mais un piège
n'a de sens que s'il y a quelqu'un pour tomber dedans. Ils arrivent avec ceux qu'ils
doivent arrêter.

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
