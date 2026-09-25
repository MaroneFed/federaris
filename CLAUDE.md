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
| Monde | **LA SYLVE** (refonte du 23/09/2026 par Martin : « tu peux tout supprimer sauf le personnage et sa vue »). Forêt dense et sombre de **320 × 320 m** (700 → 420 le 26/09, → 320 le 27/09 : « réduis un peu la map »), brume à **18 m** qui **s'ouvre en hauteur**, relief doux tiré de la graine. Au centre, **la citadelle** (27/09, « un giga château ») : enceinte de **100 m**, quatre portes ouvertes, remparts, et **la tour de la Couronne, 64 m**, rampe en spirale extérieure **sans parapet** (trois trous, quatre pendules). Voir `docs/LA-SAISON.md`. |
| Arbres | **Bibliothèque partagée, jamais fusionnée.** 16 maillages d'arbres (sapin, hêtre, bouleau, mort) + 6 touffes + 4 blocs, instanciés ~15 000 fois. C'est l'inverse du `Batcher` et c'est volontaire : un objet fusionné ne sort pas du champ tout seul, des objets séparés si. **Ne jamais fusionner la forêt** — et se rappeler que le plantage du lancement venait de 17 600 *maillages distincts*, pas de 17 600 objets. Ce qui coûte, c'est le nombre de modèles. |
| Lumière | Brume exponentielle teintée (jamais grise), lumière rasante froide et faible qui **découpe** au lieu d'éclairer, ambiant sombre mais jamais noir, et **une lanterne portée par le joueur**. La lanterne n'est pas un ornement : sans elle, sombre veut dire « on ne voit rien » et le jeu devient pénible. |
| Boucle | **LA COURONNE** (26/09 au soir). Une Couronne au **sommet de la tour** ; la première personne qui la porte au **Monument** (un seul, qui change de place à chaque manche, colonne bleue) gagne la manche. Le porteur brille, ralentit, ne pousse pas, et **la lâche** si on le pousse ou le projette. S'il tombe de haut (sans Planeur), elle **reste là où il a quitté le sol** (`Crown.Slip`). À terre, on la **ramasse en passant dessus** ; on gagne en **entrant dans le cercle** du Monument (27/09, `docs/100-PROBLEMES.md`). Détails : `docs/LA-SAISON.md`. |
| Manches | Match en **3, 5, 7 ou 10 manches**, durée max **4 à 10 min** par manche (choisies au salon). Le temps écoulé : celui qui tient la Couronne gagne, sinon personne. Égalité finale : manche de **départage**. Chaque manche **recharge la scène** ; seul `Match` (statique) traverse les manches. |
| Capacités | **Rien que des capacités, jamais rien en main** (27/09 : « faut pas l'épée, faut juste des capacités »). **26 capacités** (14 actives, 12 passives, `Match/Abilities.cs`), chacune avec **un nom simple et une phrase de tous les jours**. Actives sur **clic droit, R, C** (trois au plus : la 4e remplace la plus ancienne), **V** pour le don d'un sanctuaire. On en choisit **une avant la manche 1**, puis une entre chaque manche parmi (joueurs + 1) cartes, **le vainqueur en dernier**. |
| Objets | **Aucun** (27/09). Plus d'épée, de détecteur, de pelle, de fioles, de coffres. À la place, les **sanctuaires** (≈ 9 par manche) : E maintenu 1 s, une capacité active au hasard pour la manche, sur V. |
| Autres joueurs | Des **bots** en Phase 1 (`World/Rival.cs`) qui jouent avec les mêmes règles, par les mêmes méthodes (`AbilityCaster.Cast`, `Combat.Shove`…) : ce sont de **futurs joueurs**, ils ne parlent pas (une voix, pas de texte). On les **voit** de loin : écharpe, lanterne et halo à leur couleur. |
| Repères | **Pas de boussole, pas de carte, pas de marqueur**. On se repère aux lumières : la tour qui perce la brume, la colonne bleue du Monument, la colonne dorée de la Couronne, les creux bleus, les arbres géants. |
| Retirés | Le 26/09 : mage, relique, talismans, Malédiction, améliorations, quatre victoires, soudoiement ; au soir : stèles, butin (★), ressources, camp et caches, construction (T), Autels, boussole, carte (M). **Le 27/09 : épée, vie et mort, tous les objets, coffres, Garde Pâle, Roi Creux, loups, revenants, cerf blanc, donjon à étages, toutes les icônes.** Ne pas les réintroduire sans que Martin le demande. |
| Combat | **Pas de vie, pas de mort** (27/09) : c'est Smash. **Clic gauche : pousser** (3 m, recharge 0,9 s) ; le porteur lâche la Couronne. Les capacités projettent et étourdissent **0,2 à 0,7 s, jamais plus**. **Pas de PNJ humanoïdes** (Martin : « soit un truc tellement excellent, soit rien ») : les **Yeux** (12 sentinelles de pierre qui flottent ; bleu → orange → rouge 1,1 s de charge → rayon blanc qui projette et fait lâcher la Couronne) et les **pendules** de la tour. |
| Écran et menus | **Zéro icône** (27/09 : « j'aime pas les icônes, les menus sont moches et buggés »). HUD en **texte** : chrono, une phrase pour la Couronne, score en chiffres, capacités (touche, nom, recharge). Menus = **une colonne de mots** sur la forêt, **tout au clavier** (↑ ↓ ← → Entrée Échap) comme à la souris. Ne jamais modifier un `GUIStyle` partagé en plein dessin : en faire une copie. |
| Persistance | **Aucune** entre parties. Seuls les **réglages du joueur** (sensibilité, volume, champ de vision, taille du texte, plein écran) sont gardés, dans `PlayerPrefs` (`Core/Settings.cs`). |
| Réseau | Listen-server + Steam P2P. **Autorité absolue de l'hôte** (Couronne, coups, fin de manche). L'architecture est prête (places, graines, gestes par méthodes) ; le transport est pour la Phase 3 : voir `docs/RESEAU.md`. |

## Le différenciateur à protéger

**Révisé le 26/09/2026 au soir par Martin (La Couronne), précisé le 27/09 (capacités).**

**Une seule Couronne, quatre joueurs dans le noir.** On ne gagne pas en accumulant : on
gagne en **prenant** la Couronne au sommet d'un château gardé, puis en la **portant**
jusqu'au Monument pendant que tout le monde vous voit briller et vous court après.
Chaque manche est une course-poursuite ; chaque capacité choisie entre deux manches
change la suivante (le vainqueur choisit en dernier : ça rééquilibre). On ne meurt pas :
on se pousse, on se projette, on se vole la Couronne — **Smash dans une forêt noire**.

Second pilier : **la tour de la Couronne, une course verticale** (Fall Guys) — quatre
joueurs sur une rampe sans parapet, des trous, des pendules, des Yeux qui chargent, et
64 m de vide où l'on pousse les autres.

## Phases — ne jamais travailler sur une phase ultérieure sans demande explicite

- **PHASE 1 — le match contre des bots** ← *on est ici* (redéfinie le 26/09/2026 au soir :
  **La Couronne** — manches, capacités, citadelle et tour, Yeux, salon, bots
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
  Le 26/09 au soir : **La Couronne** (manches, draft, Monument, Garde Pâle, objets).
  Le 27/09 : **rien que des capacités** (26), plus d'épée ni de vie ni d'objets, plus
  de PNJ humains (les Yeux), citadelle et tour de 64 m, carte de 320 m, HUD et menus
  en texte sans icônes (`docs/100-RAISONS.md`).
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
- La Couronne, les capacités, les dons et les victoires ne changent **que** par les
  méthodes qui prennent le joueur qui agit (`Crown.TryTakeFor`, `Monument.TryDeliver`,
  `AbilityCaster.Cast`, `Shrine.TryTakeFor`, `Combat.Shove`, `Match.Draft.TryPick`…) : toi, un bot, ou demain un
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
