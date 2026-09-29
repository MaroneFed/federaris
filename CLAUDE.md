# FIEF — règles de travail sur ce projet

## Contexte

Jeu multijoueur de rivalité sur une île flottante, en première personne, 3D low-poly,
matchs d'environ 30 min en **manches** de 2 à 8 joueurs, destiné à Steam. Unity 6 + C#.

**Le jeu est décrit dans `docs/LA-SAISON.md`.** C'est la référence : on y lit la boucle,
les chiffres, et ce qui est en Phase 1 ou plus tard. Le lire avant de toucher au gameplay.

Martin développe seul (JS/web à l'origine, **débutant Unity et C#**), avec son frère
de 16 ans pour les assets et la map (débutant complet).

## Comment travailler ici

- **Petites étapes testables.** Après chaque étape, le jeu doit se lancer et montrer
  quelque chose qui marche. Pas de bloc de 500 lignes sans test.
- **Expliquer les concepts Unity au fur et à mesure** (GameObject, Prefab, Component,
  ScriptableObject). Le but est qu'il apprenne, pas qu'il copie.
- **Manipulation dans l'éditeur ⇒ donner le chemin exact des menus.**
- **Mauvais choix technique ⇒ le dire franchement.**

## Les 2 lois inviolables

1. **Look SIMPLE** — low-poly stylisé, assets de banques (Kenney, Synty, Asset Store).
   **Zéro création 3D sur mesure.** L'art ne doit jamais être un goulot.
2. **Parties de 30-60 min** — matchs autonomes, en manches. **Pas de monde persistant**, pas de base
   de données, pas de serveur dédié.

## Décisions verrouillées (ne pas rediscuter)

| Sujet | Décision |
|---|---|
| Joueurs | **2 à 8** (révisé le 27/09 au soir par Martin : « tu peux monter le nombre de joueurs, on s'en fout du nombre de joueurs ») : toi et 1 à 7 bots en Phase 1 (choisis au salon, 6 joueurs par défaut). **Chacun pour soi.** |
| Vue | **Première personne, et elle seule** (révisé le 22/09/2026 par Martin : « une vue et une seule »). La touche V et la 3e personne jouable sont supprimées ; le mode orbital ne sert plus qu'à l'écran-titre. ZQSD/WASD + souris, le corps suit le regard. |
| Corps subjectif | **On ne voit rien de soi en première personne** (tranché le 22/09/2026 après six tentatives). Dans l'ordre : masquer `CharacterRig` pièce par pièce (il en restait toujours une devant l'œil), un corps subjectif avec poncho et bâton (le poncho forme un **anneau** qui encercle l'image), le même réduit à quatre membres (des boîtes qui flottent, **noires** parce que `CharacterRig` resté en `ShadowsOnly` leur projette son ombre dessus). Ce qui donne le corps, c'est **l'ombre** : `CharacterRig` reste en `ShadowsOnly`, sa silhouette complète se projette au sol. **Le personnage (01/10) : UN HARICOT façon Fall Guys** — un seul corps satiné à sa couleur avec le visage dedans (grands yeux blancs à pupilles noires, joues roses), un petit casque d'acier qui descend sur le front, cimier, cape, moufles ; squash & stretch, saltos quand il est éjecté ou poussé, **danse de victoire** (`CharacterRig.Party`). **Un modèle 3D animé CC0** rangé dans `Resources/Modeles/Personnage` le remplace (`ModelCharacter`, voir `docs/MODELES.md`). Ne pas rajouter de mains, de bâton ou de vêtement en vue subjective sans que Martin le demande explicitement — c'est un vrai travail d'animation, pas un réglage.
| Monde | **L'ÎLE FLOTTANTE** (27/09 au soir, Martin : « il faut supprimer la forêt »). **Un château de conte** (30/09 : « ça fait IA le château, je veux des dingueries » ; **assagi le 01/10** : « on dirait un truc fait par IA ») : pierre crème, tours rondes à **toits en cloche d'ardoise bleu nuit** (toutes, portes comprises), créneaux réguliers à chaperon, meurtrières, bandeau, **l'or mat — presque rien ne brille**, bannières, jardins dans la cour, quatre **cascades** qui tombent de l'île, des rochers qui flottent. Le vrai château viendra du **Castle Kit de Kenney** (`docs/MODELES.md`). Une île d'environ 200 m au-dessus d'une **mer de nuages**, ciel de fin de journée ; **six îlots flottants** autour (**trois Monuments** s'y posent à chaque manche — 28/09 : « des endroits où poser la couronne, où on veut »). Au centre, **la citadelle** (enceinte de 100 m, quatre portes) et **la tour de la Couronne, 100 m**, **quatre rampes** en spirale entrelacées (29/09 : « plus fair pour tout le monde »), une **en face de chaque porte**, deux tours chacune, sans parapet, six bandes de couleur ; **obstacles** : trous, pendules, béliers, balayeurs, herses, boulets — **plus de courants**, et **de plus en plus nombreux à chaque manche** (`Tower.Hardness`). **Chacun part de SA plateforme flottante** (42 m de haut ; 29/09 : **deux par porte**, à la même distance de sa porte, avec **son arbaleste** qui **pose sur le parvis devant la porte**, sans viser) ; on y réapparaît. **Quatre portes, une au milieu de chaque muraille**, à pont-levis. Arbalestes : celle de chaque plateforme, quatre dehors sur l'île, une par îlot — **aucune dans la cour** (« remonter direct tout en haut, c'est cheaté »). **Le sceau de la citadelle** renvoie qui y entre par les airs : on entre **par une porte**, au bout d'un **couloir piégé** (chicanes, moulinets, herses, marteaux). **Obstacles de la tour et des couloirs tirés au hasard à chaque manche** — le même nombre sur chaque rampe, **les mêmes stations devant les quatre portes**, les boulets **en volée** sur les quatre rampes (v13 : l'équité). **Six courants d'air** entre l'île et les îlots. Voir `docs/LA-SAISON.md`. |
| Arbres | **Supprimés** avec la forêt (27/09 au soir). La leçon reste : ce qui coûte, c'est le nombre de *maillages distincts*, pas le nombre d'objets. |
| Lumière | **Fin d'après-midi claire** (27/09 au soir, éclaircie le 30/09 : « plus pro ») : skybox procédurale bleue à l'horizon pêche, soleil chaud à 27°, longues ombres, brume légère, **mer de nuages blanche et rose**. La lanterne portée reste, discrète. |
| Boucle | **LA COURONNE**. Une Couronne au **sommet de la tour** ; on saute et on **plane** jusqu'à **l'un des trois Monuments**, au choix, sur leurs **îlots flottants** (colonnes bleues) — ou on s'y fait tirer par une arbaleste. **La Couronne est lourde** (30/09 : « trop facile de gagner, tu voles et c'est gagné ») : son porteur n'a jamais d'ailes d'or, plane à 11 m/s et tombe à 8 m/s — du sommet, il ne va pas droit aux Monuments : **courant d'air** (on tourne dedans pour remonter) ou **arbaleste** de l'île. **Le sacre** : rester **3 s** dans le cercle d'un Monument avec la Couronne gagne la manche (barre à l'écran de tous). **Pousser le porteur, c'est lui voler la Couronne** (le voleur est protégé 1,5 s, la victime ne peut pas la reprendre pendant 3 s) ; **en l'air, le piqué d'aigle** fond sur le porteur (contact = vol). Les autres coups la font tomber ; **à terre comme sur son socle, on la prend EN PASSANT DESSUS** (01/10 ; E marche aussi, **un simple appui** — plus rien à tenir), elle rentre au sommet au bout de 20 s. Replier ses ailes et tomber : elle reste là-haut (`Crown.Slip`). Tomber dans les nuages : on réapparaît sur sa plateforme. Détails : `docs/LA-SAISON.md`. |
| Manches | Match en **3, 5, 7 ou 10 manches**, durée max **4 à 10 min** par manche (choisies au salon). **Le temps écoulé : PERSONNE ne gagne** (01/10, Martin : « la victoire, il ne faut pas la donner s'il a la couronne à la fin ») — on ne gagne qu'au Monument (prolongation : proposée dans `v2-ideas.md`, à Martin de trancher). Égalité finale : manche de **départage**. Chaque manche **recharge la scène** ; seul `Match` (statique) traverse les manches. |
| Capacités | **Rien que des capacités, jamais rien en main** (27/09 : « faut pas l'épée, faut juste des capacités »). **26 capacités** (14 actives, 12 passives, `Match/Abilities.cs`), chacune avec **un nom simple et une phrase de tous les jours**. **Une passive et UNE active (le clic gauche), c'est tout** (29/09 : « qu'un passif et un clic gauche, pas d'autres conneries ») ; **elles changent à chaque manche** : avant chaque manche, un tour de table pour la passive puis un pour l'active, parmi (joueurs + 1) cartes, **le vainqueur en dernier** ; le don d'un sanctuaire remplace le clic gauche pour la manche. **Pousser au clic droit**, **E** pour interagir ; **Réglages ▸ Touche capacité / Touche pousser** (30/09 : les trois clics, F, R, X, V). Les cartes : face cachée, retournement, cadre d'or, rayons (`UI/CardArt.cs`). Panneau des **touches** **seulement sur F1/H** (30/09 : plus jamais tout seul, « je veux pas le truc à l'avant »). |
| Vol plané | **Tout le monde a des ailes** (28/09 : « je ne sais même pas comment on fait pour planer »). Elles **s'ouvrent seules** au-dessus du vide (6 m) ; **on vole où l'on regarde** (en bas on pique et on accélère, en haut on remonte en dépensant sa vitesse) ; Espace replie/rouvre. **Ailes d'or** au sommet et au départ d'une arbaleste (plus rapides). Courants d'air jusqu'à 80 m (poussée 15), jamais jusqu'au sommet. Réglages : `World/Wings.cs`. **Plus rien à apprendre : ne pas remettre de touche à maintenir.** |
| Victoire | **Plus d'aura** (30/09, Martin : « enlève tout ce qui est aura, je déteste ça ») : plus de moments d'aura, de ralenti, de « +1000 AURA », de flammes, de phonk (`World/Aura.cs` supprimé). **La victoire filme le gagnant** (caméra qui tourne autour de lui) avec **son pseudo en grand, en or**, et **il DANSE SUR LA MUSIQUE** (01/10 : « notre perso qui danse avec la musique ») : six figures (balancé, disco, fil, cancan, poings au ciel, saut et tour), **un pas par temps** (`MusicDirector.DanceBeat`) ; musique de danse fabriquée à 120 BPM, ou celle de Martin (`Resources/Music/danse-128.mp3`, tempo dans le nom) ; la Couronne flotte au-dessus de sa tête ; confettis, feux d'artifice, anneau d'or et projecteur **sur le rythme** (`VictoryShow`). |
| Graphismes | **Lisse** (30/09 : « tout doit être exceptionnel, lisse ») : anticrénelage x8, ombres fines en 4 cascades, synchro verticale, sonde de reflets (`Atmosphere.Smooth`) ; formes rondes (capsules, sphères) et matières satinées ou métal (`MaterialFactory.GetShiny`, `Polish`) pour les personnages, la Couronne, l'arbaleste, les obstacles. Le décor (pierre, herbe) reste mat. |
| Pseudos | **Réglages ▸ Pseudo** (PlayerPrefs). **Le pseudo au-dessus de chaque joueur, et rien d'autre** (29/09 : « pas leurs conneries ») ; l'écran reste épuré. |
| Objets | **Aucun**. À la place, les **sanctuaires** : quatre sur l'île, un sur chaque îlot sans Monument : **un appui sur E** (01/10), une capacité active au hasard qui **remplace le clic gauche** pour la manche. |
| Autres joueurs | Des **bots** en Phase 1 (`World/Rival.cs`) qui jouent avec les mêmes règles, par les mêmes méthodes (`AbilityCaster.Cast`, `Combat.Shove`…) : ce sont de **futurs joueurs**, ils ne parlent pas (une voix, pas de texte). On les **voit** de loin : écharpe, lanterne et halo à leur couleur. |
| Repères | **Pas de boussole, pas de carte, pas de marqueur**. On se repère aux lumières : la tour, les colonnes bleues des Monuments, les courants d'air, les colonnes des plateformes, les arches des couloirs, la colonne dorée de la Couronne, les fanions des zones de départ, la couleur de chaque tour de rampe. |
| Retirés | Le 26/09 : mage, relique, talismans, Malédiction, améliorations, quatre victoires, soudoiement ; au soir : stèles, butin (★), ressources, camp et caches, construction (T), Autels, boussole, carte (M). Le 27/09 : épée, vie et mort, tous les objets, coffres, Garde Pâle, Roi Creux, loups, revenants, cerf blanc, donjon à étages, toutes les icônes. **Le 27/09 au soir : la forêt, l'escalade des arbres, la nuit et l'orage.** Le 28/09 au soir : la ligne de départ dans la cour, les arbalestes de la cour, les Yeux flottants, la touche C. Le 29/09 : la rampe unique, les courants de la rampe, les touches R, V et F, les capacités qui s'accumulent. Le 30/09 : **l'aura** (moments, phonk, flammes, ralenti), **les trous** de la rampe, le panneau des touches automatique, le mendiant en poncho ; au soir : **le texte à l'écran** (astuces, phrases du HUD, fil écrit, lignes d'aide des menus). Le 01/10 : **la victoire au chrono**, **le maintien de E** (Couronne, sanctuaires), l'écharpe, les dernières phrases des menus. Ne pas les réintroduire sans que Martin le demande. |
| Combat | **Pas de vie, pas de mort** : c'est Smash. **Clic droit : pousser** (3,2 m, recharge 0,9 s) : **le poussé part en cloche à une quinzaine de mètres** (01/10 : « quand ça pousse, que ça pousse bien » — 26 m/s, 10 vers le haut, l'élan ne se freine presque plus en l'air, `Seeker.Launch`), en saltos ; sur le porteur, c'est un **vol** (la victime ne part qu'à moitié). Les obstacles projettent **loin** ; **sur la tour, un obstacle (ou une gargouille) t'ÉJECTE de la tour** : 22 m/s dehors, 11 vers le haut, l'élan ne retombe presque pas, saltos, ailes fermées jusqu'au sol, dans la cour (29-30/09, `Seeker.Tumble`, `Combat.KnockDrag`). **Plus de trous** dans les rampes (30/09 : « trop compliqués »). Les capacités projettent et étourdissent **0,2 à 0,7 s, jamais plus**. Chaque capacité a sa **signature visuelle** (`World/Fx.cs`) ; celles qu'on vise se **tiennent pour viser** (un aperçu) et partent au **relâchement**. Le **Souffle** est une vague qui traverse l'île (150 m). **Pas de PNJ humanoïdes** : les **gargouilles** (28/09 au soir, à la place des Yeux : 16 bêtes de pierre perchées sur les tours, les portes et le fût ; ambre → orange → rouge < 1 s de charge, gueule qui rougeoie et cible au sol → jet de feu qui **EXPLOSE** et projette hors de la rampe ; classe `Eye` dans le code) et les obstacles de la tour. **Les bots se battent en montant.** **Protection** de 3 s au départ et après un respawn. |
| Écran et menus | **Zéro texte à l'écran, des icônes** (30/09, Martin : « je veux aucun texte à l'écran, je déteste le texte, je veux des icônes, plus pro comme Fall Guys » — l'inverse du 27/09). Icônes **dessinées par le code** (`UI/IconArt.cs`, formes SDF, aperçus PNG avec `Tools/icones`) et posées par `UI/Icons.cs` : icône blanche cernée de sombre, **pastilles** rondes à reflet, touches (souris ou lettre), gros chiffres cernés. **NET AU PIXEL** (01/10 : « un effet de flou constant partout ») : chaque icône et chaque pastille est **fabriquée à sa taille exacte** (jamais de mipmaps ni de filtre trilinéaire), tout est posé sur des **pixels entiers**, le liseré des textes est fait de copies décalées d'un nombre **entier** de pixels ; **jamais de zoom par `GUI.matrix`** sur du texte (on redessine plus grand) ; polices en « Hinted Smooth ». Commandes, salon, pause, fin de manche, choix, podium : **en pastilles et icônes**. HUD : chrono en pastille, une pastille par manche, la Couronne en icônes, jauge de la tour, scores en pastilles, capacité/passive/poussée en ronds avec recharge, fil des événements en icônes, **pas d'astuces écrites** mais des **astuces en icônes** (v13 : la touche puis ce qu'elle fait, une fois par match, `Hud.TipIcons`), une croix rouge sur la Couronne pendant ton verrou. Menus = **gros boutons ronds** avec icône (jaunes quand on les vise), **tout au clavier** comme à la souris. Seuls restent écrits : les pseudos, les mots des boutons de menu, les noms des capacités sur les cartes. Polices **Titan One** et **Lilita One** (OFL). Ne jamais modifier un `GUIStyle` partagé en plein dessin : en faire une copie. |
| Persistance | **Aucune** entre parties. Seuls les **réglages du joueur** (sensibilité, volume, champ de vision, taille du texte, plein écran) sont gardés, dans `PlayerPrefs` (`Core/Settings.cs`). |
| Réseau | Listen-server + Steam P2P. **Autorité absolue de l'hôte** (Couronne, coups, fin de manche). L'architecture est prête (places, graines, gestes par méthodes) ; le transport est pour la Phase 3 : voir `docs/RESEAU.md`. |

## Le différenciateur à protéger

**Révisé le 26/09/2026 au soir par Martin (La Couronne), précisé le 27/09 (capacités).**

**Une seule Couronne, huit joueurs au-dessus des nuages.** On ne gagne pas en
accumulant : on gagne en **prenant** la Couronne au sommet d'une tour gardée, puis en
**planant** jusqu'à l'un des Monuments pendant que tout le monde vous voit briller, vous vole
dessus et vous la **vole**.
Chaque manche est une course-poursuite ; chaque capacité choisie entre deux manches
change la suivante (le vainqueur choisit en dernier : ça rééquilibre). On ne meurt pas :
on se pousse, on se projette, on se vole la Couronne — **Smash au-dessus des nuages**.

Second pilier : **la tour de la Couronne, une course verticale** (Fall Guys) — une
rampe de 100 m sans parapet, des trous, des courants, des pendules, des béliers, des
boulets, des gargouilles qui crachent le feu, des couloirs piégés, et le vide où l'on pousse
les autres. Puis **le vol** : planer du sommet jusqu'à l'îlot d'un Monument.

## Phases — ne jamais travailler sur une phase ultérieure sans demande explicite

- **PHASE 1 — le match contre des bots** ← *on est ici* (redéfinie le 26/09/2026 au soir :
  **La Couronne** — manches, capacités, île, tour, planeurs, arbalestes, Yeux, salon, bots
  qui jouent comme des joueurs, architecture réseau préparée)
  Porte 1 : un match de 30 minutes contre des bots est-il haletant du début à la fin ?
  Historique :
  Ressources, camp, caches, château, stèle, cloche, cinq lieux-dits, feux-follets et
  cerf blanc ; le 24/09 : **chacun sa stèle, trois rivaux PNJ, le vol, les gardes**.
  Puis le 25/09 : **stèle fixe, pièges, loups et revenants, trois Autels**. Le 26/09 :
  **carte de 420 m, repères sur la boussole, carte (M), moins de texte, nouvelles
  polices, récit automatique, inventaire en cases** — puis **la refonte « action »** :
  un seul but (le butin ★), donjon à étages et Couronne, quinze gardes qui chassent,
  construction (T), arbres géants, rivaux sans dialogue. Retirés : mage, relique,
  talismans, Malédiction, améliorations, quatre victoires, soudoiement.
  Le 26/09 au soir : **La Couronne** (manches, draft, Monument, Garde Pâle, objets).
  Le 27/09 : **rien que des capacités** (26), plus d'épée ni de vie ni d'objets, plus
  de PNJ humains (les Yeux), citadelle et tour de 64 m, carte de 320 m, HUD et menus
  en texte sans icônes (`docs/100-RAISONS.md`). Le 27/09 au soir : **adieu la forêt**,
  l'île flottante, la tour de 100 m et ses obstacles, les planeurs, l'îlot du Monument,
  les arbalestes géantes, le vol de Couronne, les méga effets, 2 à 8 joueurs. Le 28/09 :
  **le vol plané pour tous** (ouverture seule, piqué/remontée, courants d'air), **trois
  Monuments**, arbalestes refaites (tension, une par îlot), Yeux redessinés, capacités
  bien plus fortes (Souffle qui traverse l'île) et visée maintenue, cartes de capacités.
  Le 28/09 au soir : **l'aura** (moments, phonk), les **plateformes de départ** et le
  **sceau**, les **couloirs piégés**, les **obstacles aléatoires**, le **piqué d'aigle**,
  les **gargouilles**, les touches refaites, les cartes refaites. Le 29/09 : **quatre
  rampes** (une par porte), plateformes face aux portes et arbalestes qui posent sur le
  parvis, ponts-levis, obstacles croissants, **une passive + un clic gauche** neufs à
  chaque manche, **pseudos**, **victoire filmée**, gargouilles explosives, bots qui se
  battent, musique « montagem orchestral ». Puis la **passe qualité** : sommet sans
  z-fighting, obstacles qui renvoient en bas de la tour, écrans épurés, plan d'ensemble
  à l'écran-titre. Le 30/09 : **plus d'aura**, le **petit chevalier lisse** et sa **fête de
  victoire**, **plus de trous**, un **saut fiable**, l'**éjection** de la tour, la **Couronne
  lourde** et le **sacre** (3 s), le bug du **socle en boule** (bots), la **Couronne**,
  l'**arbaleste** et les **obstacles** refaits, l'image **lissée**, le **menu** refait, les
  **touches au choix**. Le 30/09 au soir : **zéro texte, des icônes** (HUD et menus façon
  Fall Guys), le **château de conte**, les cascades, la lumière claire, les herses posées
  dans la pente, l'arbaleste de plateforme qui pivote. Le 01/10 : **net au pixel** (plus
  de flou), **Commandes en icônes**, **cartes refaites** (texte jamais coupé), **plus de
  victoire au chrono**, **poussée qui projette**, **Couronne prise en passant** et d'un
  simple E, l'**arbaleste de départ qui suit la souris**, la **danse du vainqueur sur la
  musique**, les **persos en haricot**, les **gargouilles rondes**, le château assagi,
  des bots plus malins, le **gamer chiant** et le **designer chiant** (`docs/GAMER-CHIANT.md`,
  `docs/DESIGNER-CHIANT.md`). Le 01/10 au soir, **deuxième passe** des deux
  (`docs/GAMER-CHIANT-2.md`, `docs/DESIGNER-CHIANT-2.md`, v13) : l'**équité** (boulets en
  volée sur les quatre rampes, les mêmes pièges devant les quatre portes), les capacités
  dans les coins (Rappel après un respawn, Échange et Grappin **jamais à travers la
  muraille**, Mine et Mur refusés en plein vol, protégés épargnés), herses qui piquent tant
  qu'elles sont sorties, **astuces en icônes**, secousse douce, **l'or mat** jusque dans la
  tour et les obstacles, le code mort des trous et des courants retiré.
- **PHASE 2 — le conflit.** Porte 2 : une manche crée-t-elle une histoire qu'on se
  raconte après ? Ce qui reste : l'**arc**, d'autres pièges (fosse), d'autres pouvoirs.
- **PHASE 3 — multijoueur.** Porte 3 : un match de 30-45 min à plusieurs joueurs sans crash ni
  désync. (L'architecture est déjà préparée : `docs/RESEAU.md`.)
- **PHASE 4 — le match complet.** Anti-snowball, événements, équilibrage fin.
- **PHASE 5 — vitrine.** Page Steam, démo, Next Fest, localisation EN.
- **PHASE 6 — lancement** en Early Access.

## Règle anti-dérive

Toute idée hors de la phase en cours va dans **`docs/v2-ideas.md`**, jamais dans le code.
Si Martin propose une idée hors-phase, lui rappeler cette règle.

## Règles techniques du dépôt

- Les fichiers `.meta` sont versionnés. **Ne jamais les supprimer** : ils portent l'identité
  (GUID) des assets, sans eux toutes les références se cassent.
- La logique de jeu ne vit pas dans les `MonoBehaviour` quand elle peut en être extraite
  (voir `docs/ARCHITECTURE.md`).
- La Couronne, les capacités, les dons et les victoires ne changent **que** par les
  méthodes qui prennent le joueur qui agit (`Crown.TryTakeFor`, `Monument.TryDeliver`,
  `Crown.TrySteal`, `Combat.Dive`, `AbilityCaster.Cast`, `Shrine.TryTakeFor`, `Ballista.Mount`,
  `Combat.Shove`, `Match.Draft.TryPick`…) : toi, un bot, ou demain un
  joueur en ligne passez par les mêmes portes (voir `docs/RESEAU.md`).
- **Les réglages de `GameConfig` : le code fait foi.** Unity enregistre les valeurs de
  l'Inspector dans la scène ; au lancement, elles sont remises à celles du code, sauf si
  la case *Keep Inspector Values* est cochée (26/09, après la réduction de la carte à 420 m).

## Le vérificateur

Claude ne peut pas lancer Unity : il ne voit jamais ses propres erreurs de compilation.
`Tools/verifier.py` les cherche à sa place. **À lancer avant chaque push**, et tu peux le
lancer toi-même avant d'ouvrir l'éditeur :

```
python3 Tools/verifier.py
```

Il attrape les cinq fautes qui sont *réellement* arrivées sur ce projet : accolades
déséquilibrées, appel à une méthode supprimée, référence à `Type.Membre` inexistant,
mauvais nombre d'arguments, type inconnu ou `[Header]` posé devant une méthode.

Si tu ajoutes du code utilisant un type Unity absent de `Tools/types-externes.txt`,
le vérificateur rouspète : ajoute son nom au fichier, une ligne chacun.

**Le compilateur de contrôle** (depuis le 24/09/2026, après un CS0119 que le
vérificateur n'avait pas vu) : `sh Tools/compiler.sh` compile *vraiment* les scripts
avec le compilateur C#, contre les assemblies de référence d'Unity téléchargées depuis
NuGet (jamais versionnées). Il donne les mêmes erreurs qu'Unity. Il faut le SDK .NET
(`apt-get install dotnet-sdk-8.0`). Claude le lance avant chaque push quand il le peut ;
les deux outils se complètent (le vérificateur connaît aussi les API périmées d'Unity 6).
