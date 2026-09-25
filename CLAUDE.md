# FIEF — règles de travail sur ce projet

## Contexte

Jeu multijoueur de rivalité dans une forêt noire, en première personne, 3D low-poly,
matchs d'environ 30 min en **manches** à 4 joueurs, destiné à Steam. Unity 6 + C#.

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
| Joueurs | **2 à 4, et 4 au plus** (Martin, 26/09 : « c'est maximum quatre joueurs sur la map ») : toi et 1 à 3 bots en Phase 1 (choisis au salon), quatre joueurs en ligne en Phase 3. **Chacun pour soi.** |
| Vue | **Première personne, et elle seule** (révisé le 22/09/2026 par Martin : « une vue et une seule »). La touche V et la 3e personne jouable sont supprimées ; le mode orbital ne sert plus qu'à l'écran-titre. ZQSD/WASD + souris, le corps suit le regard. |
| Corps subjectif | **On ne voit rien de soi en première personne** (tranché le 22/09/2026 après six tentatives). Dans l'ordre : masquer `CharacterRig` pièce par pièce (il en restait toujours une devant l'œil), un corps subjectif avec poncho et bâton (le poncho forme un **anneau** qui encercle l'image), le même réduit à quatre membres (des boîtes qui flottent, **noires** parce que `CharacterRig` resté en `ShadowsOnly` leur projette son ombre dessus). Ce qui donne le corps, c'est **l'ombre** : `CharacterRig` reste en `ShadowsOnly`, sa silhouette complète se projette au sol. Ne pas rajouter de mains, de bâton ou de vêtement en vue subjective sans que Martin le demande explicitement — c'est un vrai travail d'animation, pas un réglage.
| Monde | **LA SYLVE** (refonte du 23/09/2026 par Martin : « tu peux tout supprimer sauf le personnage et sa vue »). Forêt dense et sombre de **420 × 420 m** (réduite de 700 le 26/09 : « on se perd complètement dans la map »), brume à **14 m** qui **s'ouvre en hauteur** (arbres géants, terrasse du donjon), relief doux tiré de la graine. Au centre, **un grand château** dont le **donjon a trois niveaux et une terrasse** (26/09 : « faut mettre des étages »). Voir `docs/LA-SAISON.md`. |
| Arbres | **Bibliothèque partagée, jamais fusionnée.** 16 maillages d'arbres (sapin, hêtre, bouleau, mort) + 6 touffes + 4 blocs, instanciés ~15 000 fois. C'est l'inverse du `Batcher` et c'est volontaire : un objet fusionné ne sort pas du champ tout seul, des objets séparés si. **Ne jamais fusionner la forêt** — et se rappeler que le plantage du lancement venait de 17 600 *maillages distincts*, pas de 17 600 objets. Ce qui coûte, c'est le nombre de modèles. |
| Lumière | Brume exponentielle teintée (jamais grise), lumière rasante froide et faible qui **découpe** au lieu d'éclairer, ambiant sombre mais jamais noir, et **une lanterne portée par le joueur**. La lanterne n'est pas un ornement : sans elle, sombre veut dire « on ne voit rien » et le jeu devient pénible. |
| Boucle | **LA COURONNE** (refonte du 26/09 au soir par Martin : « on oublie tout ce qui est stèle, récolter pour gagner de la gloire. Ce qui compte, c'est d'avoir la couronne et la ramener au monument »). Une Couronne au sommet du donjon, gardée par le **Roi Creux** ; la première personne qui la porte au **Monument** (un seul, qui change de place à chaque manche, colonne bleue) gagne la manche. Le porteur brille, ralentit, ne frappe pas, et **la lâche** s'il tombe ou se fait pousser. Détails : `docs/LA-SAISON.md`. |
| Manches | Match en **3, 5, 7 ou 10 manches**, durée max **4 à 10 min** par manche (choisies au salon). Le temps écoulé : celui qui tient la Couronne gagne, sinon personne. Égalité finale : manche de **départage**. Chaque manche **recharge la scène** ; seul `Match` (statique) traverse les manches. |
| Pouvoirs | Entre deux manches, **(joueurs + 1) cartes** ; chacun en prend une, **le vainqueur de la manche en dernier**. Dix pouvoirs (double saut, ruée, coureur, poigne, colosse, ombre, flair, porteur, sang vif, seconde chance), gardés jusqu'à la fin du match. |
| Objets | Trois emplacements (1, 2, 3) : **détecteur** (bipe près d'un trésor enterré), **pelle** (le déterre), fumigène, fiole de lenteur, piège (immobilise et fait lâcher la Couronne), élixir, plume, cape d'ombre, **clé du donjon** (porte dérobée → terrasse). Dans les coffres (une quinzaine) et sous terre (une dizaine). L'épée est toujours là. Ce qu'on tient se voit à l'écran — **l'objet seul, jamais de mains**. |
| Autres joueurs | Des **bots** en Phase 1 (`World/Rival.cs`) qui jouent avec les mêmes règles, par les mêmes méthodes : ce sont de **futurs joueurs**, on ne leur parle pas. On les **voit** de loin : écharpe, lanterne et halo à leur couleur. |
| Repères | **Pas de boussole, pas de carte, pas de marqueur** (26/09 au soir : « tu n'as pas de boussole, tu n'as rien »). On se repère aux lumières : la tour de guet, la colonne bleue du Monument, la colonne dorée de la Couronne, les creux bleus, les arbres géants. Un bel écran : barre de vie, pouvoirs, objets, chrono, score. |
| Retirés | Le 26/09 : mage, relique, talismans, Malédiction, améliorations, quatre victoires, soudoiement. Le 26/09 au soir : **stèles, butin (★), ressources, camp et caches, construction (T), Autels, boussole, carte (M)**. Ne pas les réintroduire sans que Martin le demande. |
| Combat | Simple et lisible. **Épée** (clic gauche), quatre coups tuent un joueur. **Pousser** (clic droit) : le porteur lâche la Couronne. Tomber = ses objets dans une dépouille, se relever 5 s après à son point de départ. **La Garde Pâle** : 27 chevaliers **lisses et sans visage** (Martin : « soit hyper détaillé, soit un truc lisse ») — sentinelles, arbalétriers, molosses, et le **Roi Creux** (boss, 800 PV). Coups annoncés (la fente du heaume blanchit), carreaux visibles. **Loups** et **revenants** en forêt. |
| Persistance | **Aucune** entre parties |
| Réseau | Listen-server + Steam P2P. **Autorité absolue de l'hôte** (Couronne, coups, fin de manche). L'architecture est prête (places, graines, gestes par méthodes) ; le transport est pour la Phase 3 : voir `docs/RESEAU.md`. |

## Le différenciateur à protéger

**Révisé le 26/09/2026 au soir par Martin (La Couronne).**

**Une seule Couronne, quatre joueurs dans le noir.** On ne gagne pas en accumulant : on
gagne en **prenant** la Couronne au sommet d'un château gardé, puis en la **portant**
jusqu'au Monument pendant que tout le monde vous voit briller et vous court après.
Chaque manche est une course-poursuite ; chaque pouvoir choisi entre deux manches
change la suivante (le vainqueur choisit en dernier : ça rééquilibre).

Second pilier : **le château à la Elden Ring, en contenu** (pas en graphismes) —
plusieurs entrées, des remparts, une porte dérobée, une garde qui se lit, un boss.

## Phases — ne jamais travailler sur une phase ultérieure sans demande explicite

- **PHASE 1 — le match contre des bots** ← *on est ici* (redéfinie le 26/09/2026 au soir :
  **La Couronne** — manches, pouvoirs, objets, Garde Pâle et Roi Creux, salon, bots
  qui jouent comme des joueurs, architecture réseau préparée)
  Porte 1 : un match de 30 minutes contre trois bots est-il haletant du début à la fin ?
  Historique :
  Ressources, camp, caches, château, stèle, cloche, cinq lieux-dits, feux-follets et
  cerf blanc ; le 24/09 : **chacun sa stèle, trois rivaux PNJ, le vol, les gardes**.
  Puis le 25/09 : **stèle fixe, pièges, loups et revenants, trois Autels**. Le 26/09 :
  **carte de 420 m, repères sur la boussole, carte (M), moins de texte, nouvelles
  polices, récit automatique, inventaire en cases** — puis **la refonte « action »** :
  un seul but (le butin ★), donjon à étages et Couronne, quinze gardes qui chassent,
  construction (T), arbres géants, rivaux sans dialogue. Retirés : mage, relique,
  talismans, Malédiction, améliorations, quatre victoires, soudoiement.
- **PHASE 2 — le conflit.** Porte 2 : une manche crée-t-elle une histoire qu'on se
  raconte après ? Ce qui reste : l'**arc**, d'autres pièges (fosse), d'autres pouvoirs.
- **PHASE 3 — multijoueur.** Porte 3 : un match de 30-45 min à 4 joueurs sans crash ni
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
- La Couronne, les objets, les coffres et les victoires ne changent **que** par les
  méthodes qui prennent le joueur qui agit (`Crown.TryTakeFor`, `Monument.TryDeliver`,
  `Chest.TryOpenFor`, `Combat.Shove`, `Match.Draft.TryPick`…) : toi, un bot, ou demain un
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
