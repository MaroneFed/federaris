# FIEF — règles de travail sur ce projet

## Contexte

Jeu multijoueur de rivalité dans une forêt noire, en première personne, 3D low-poly,
parties de 30 min (« Saisons ») à 4 joueurs, destiné à Steam. Unity 6 + C#.

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
2. **Parties de 30-60 min** — Saisons autonomes. **Pas de monde persistant**, pas de base
   de données, pas de serveur dédié.

## Décisions verrouillées (ne pas rediscuter)

| Sujet | Décision |
|---|---|
| Joueurs | **4, et 4 au plus** (Martin, 26/09 : « c'est maximum quatre joueurs sur la map ») : toi et trois rivaux en Phase 1, quatre joueurs en Phase 3. |
| Vue | **Première personne, et elle seule** (révisé le 22/09/2026 par Martin : « une vue et une seule »). La touche V et la 3e personne jouable sont supprimées ; le mode orbital ne sert plus qu'à l'écran-titre. ZQSD/WASD + souris, le corps suit le regard. |
| Corps subjectif | **On ne voit rien de soi en première personne** (tranché le 22/09/2026 après six tentatives). Dans l'ordre : masquer `CharacterRig` pièce par pièce (il en restait toujours une devant l'œil), un corps subjectif avec poncho et bâton (le poncho forme un **anneau** qui encercle l'image), le même réduit à quatre membres (des boîtes qui flottent, **noires** parce que `CharacterRig` resté en `ShadowsOnly` leur projette son ombre dessus). Ce qui donne le corps, c'est **l'ombre** : `CharacterRig` reste en `ShadowsOnly`, sa silhouette complète se projette au sol. Ne pas rajouter de mains, de bâton ou de vêtement en vue subjective sans que Martin le demande explicitement — c'est un vrai travail d'animation, pas un réglage.
| Monde | **LA SYLVE** (refonte du 23/09/2026 par Martin : « tu peux tout supprimer sauf le personnage et sa vue »). Forêt dense et sombre de **420 × 420 m** (réduite de 700 le 26/09 : « on se perd complètement dans la map »), brume à **14 m** qui **s'ouvre en hauteur** (arbres géants, terrasse du donjon), relief doux tiré de la graine. Au centre, **un grand château** dont le **donjon a trois niveaux et une terrasse** (26/09 : « faut mettre des étages »). Voir `docs/LA-SAISON.md`. |
| Arbres | **Bibliothèque partagée, jamais fusionnée.** 16 maillages d'arbres (sapin, hêtre, bouleau, mort) + 6 touffes + 4 blocs, instanciés ~15 000 fois. C'est l'inverse du `Batcher` et c'est volontaire : un objet fusionné ne sort pas du champ tout seul, des objets séparés si. **Ne jamais fusionner la forêt** — et se rappeler que le plantage du lancement venait de 17 600 *maillages distincts*, pas de 17 600 objets. Ce qui coûte, c'est le nombre de modèles. |
| Lumière | Brume exponentielle teintée (jamais grise), lumière rasante froide et faible qui **découpe** au lieu d'éclairer, ambiant sombre mais jamais noir, et **une lanterne portée par le joueur**. La lanterne n'est pas un ornement : sans elle, sombre veut dire « on ne voit rien » et le jeu devient pénible. |
| Ressources | 3 en v1 : **Bois mort** (partout, 1 kg) → **construire** ; **Pierre-lune** (dans les creux, 3 kg) → **★2 déposée** ; **Fer ancien** (réserves du château, 2 kg) → **armes et pièges**. Une ressource, un usage (26/09 : « tu récoltes du bois, tu sais même pas pourquoi »). |
| Boucle | **Un seul but : le plus de butin (★) sur sa stèle à la cloche** (refonte du 26/09 par Martin : « y a pas assez d'action »). Le butin vient de la forêt (pierre-lune, coffres des lieux-dits), du **château** (calices, coffrets, **la Couronne ★40 sur la terrasse**) et des **autres** (piller leur stèle). Le butin porté ne compte pas : il pèse et tombe avec soi. Détails : `docs/LA-SAISON.md`. |
| Camp et caches | Un camp, planté **une seule fois**. Trois caches au plus, creusées n'importe où ; seul leur propriétaire sait où elles sont. |
| Stèles et vol | **Chacun sa stèle, FIXE, tirée au hasard ; on naît à côté.** C'est le coffre-fort et le score. Qui la trouve sans son maître (à plus de 9 m) en **pille la moitié de l'or** (E maintenu 3 s). Trois **rivaux PNJ** jouent avec exactement les mêmes règles (classe `Seeker`) : ce sont de **futurs joueurs**, on ne leur parle pas (26/09). |
| Boussole et carte | **Les repères sont revenus** (Martin, 26/09 : « si on ne se souvient pas où est la stèle, ni le château, c'est bof » — il revient sur la boussole vide du 25/09). La boussole montre le château, ta stèle, ton camp, tes caches, ta dépouille, les stèles rivales connues. Touche **M** : une carte en parchemin qui ne dévoile que ce qu'on a parcouru. Un fil d'or, que toi seul vois, monte au-dessus de ta stèle. |
| Malédiction | **Retirée le 26/09** (avec le mage, la relique, les talismans, les quatre victoires, les améliorations : « ça donne mal au ventre »). Ne pas les réintroduire sans que Martin le demande. |
| Monnaie | **Le butin (★)**, unique : c'est le score. L'or pour soudoyer est supprimé (26/09 : « les gardes, t'es pas censé les acheter »). |
| Combat | Simple et lisible. **Épée** (2 bois, 3 fer), quatre coups tuent — rivaux, bêtes **et gardes**. Tomber = tout lâcher dans une dépouille (butin compris), se relever à sa stèle. **Quinze gardes** (dont trois rôdeurs en forêt) repèrent, poursuivent et frappent qui entre au donjon ou porte du butin. **Loups** et **revenants** attaquent tout le monde. Trois **Autels** paient ★ à qui les tient. L'arc reste à faire. |
| Outils | Deux emplacements (1 / 2, ou la molette) : **épée** et **hache**, qui s'usent. **T : construire** (piège, barricade, alarme, épée, hache) avec un fantôme vert/rouge (26/09 : « un menu facile d'édition »). L'outil tenu se voit à l'écran — **l'outil seul, jamais de mains**. |
| Persistance | **Aucune** entre parties |
| Réseau | Listen-server + Steam P2P. **Autorité serveur absolue sur l'économie** |

## Le différenciateur à protéger

**Révisé le 26/09/2026 par Martin.** Le « sabotage par les salaires » (acheter les gardes)
est **supprimé** à sa demande : « les gardes, t'es pas censé les acheter non plus ». Ce
qui le remplace :

**Le pillage à quatre dans le noir.** Une Couronne au sommet d'un donjon gardé, quatre
joueurs qui se la disputent dans une forêt où l'on ne voit pas à quinze mètres, et des
stèles qu'on pille dès que leur maître s'éloigne. On ne gagne pas en accumulant : on
gagne en **prenant**, et en **défendant** ce qu'on a pris (pièges, barricades, alarmes).

Second pilier, qui tient toujours : **la destruction à règles**. On peut tout casser,
mais pas avec n'importe quoi (une barricade cède à trois coups de hache ou d'épée).

## Phases — ne jamais travailler sur une phase ultérieure sans demande explicite

- **PHASE 1 — la Saison en solo** ← *on est ici* (redéfinie le 23/09/2026)
  Porte 1 : une Saison solo de 30 minutes est-elle haletante du début à la fin ?
  Ressources, camp, caches, château, stèle, cloche, cinq lieux-dits, feux-follets et
  cerf blanc ; le 24/09 : **chacun sa stèle, trois rivaux PNJ, le vol, les gardes**.
  Puis le 25/09 : **stèle fixe, pièges, loups et revenants, trois Autels**. Le 26/09 :
  **carte de 420 m, repères sur la boussole, carte (M), moins de texte, nouvelles
  polices, récit automatique, inventaire en cases** — puis **la refonte « action »** :
  un seul but (le butin ★), donjon à étages et Couronne, quinze gardes qui chassent,
  construction (T), arbres géants, rivaux sans dialogue. Retirés : mage, relique,
  talismans, Malédiction, améliorations, quatre victoires, soudoiement.
- **PHASE 2 — le conflit.** Porte 2 : un vol de relique crée-t-il une histoire qu'on se
  raconte après ? Ce qui reste : l'**arc** et les autres pièges (fosse).
- **PHASE 3 — multijoueur.** Porte 3 : une Saison de 30-45 min à 4 joueurs sans crash ni désync.
- **PHASE 4 — la Saison complète.** Anti-snowball, événements, équilibrage fin.
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
- Or, prix, stocks et inventaires ne se modifient **que** par les méthodes `Request*` / `Try*`.
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
