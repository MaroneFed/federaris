# FIEF (« Les Veilleurs ») — document de jeu (brouillon du 11/11)

**Le jeu s'appelle FIEF** (Martin, 11/11 : « je veux que le jeu s'appelle FIEF »). « Les Veilleurs » n'est
que le nom de travail du concept, pour le distinguer de l'ancien FIEF (La Couronne).

Nouveau projet décidé par Martin le 11/11 : « on va transformer ça en jeu d'horreur dans un
château hyper incroyable ; t'es un chevalier ; des petites quêtes à la Among Us ; vraiment bien
sombre, mais bien stylé, bien Elden Ring ». **FIEF « La Couronne » est mis de côté, pas supprimé.**

Ce document sépare ce que Martin a **dit** de ce que Claude **propose**. Rien de ce qui est
« proposé » n'entre dans le code tant que Martin ne l'a pas validé.

## En une phrase

Des chevaliers entrent la nuit dans un château maudit pour y accomplir de petites quêtes,
pendant que des choses rôdent — et il faut ressortir avant l'aube.

## Ce que Martin a dit (acquis)

- On joue un **chevalier**, à plusieurs (coop).
- Un **château**, la **nuit**, très sombre, « bien stylé, bien Elden Ring ».
- Des **petites quêtes à la Among Us** (pas des reliques à ramasser).
- Il **n'aime pas les jeux d'horreur** : ça doit rester « sympa ».
- Des graphismes « de malade » : il accepte de passer par de vrais outils de 3D.

## Ce que Claude propose (à valider un par un)

Ces points viennent de la fiche écrite par l'autre Claude sur les goûts de Martin.

1. **La tension, pas la peur.** Aucun sursaut gratuit, aucun gore. Le danger d'Elden Ring, pas Outlast.
2. **Des monstres à règle.** Chaque créature obéit à UNE règle qu'on apprend :
   - *le Pénitent* n'avance que quand personne ne le regarde ;
   - *la Cloche* fonce sur ceux qui courent (l'armure fait du bruit) ;
   - *l'Armure vide* se réveille si on passe trop près de son épée.
   On a peur parce qu'on comprend, et on gagne parce qu'on maîtrise.
3. **La lumière est la ressource.** Ta torche s'use. Sans elle, tu ne vois rien et certaines choses approchent.
   Les braseros qu'on allume restent allumés : le château s'éclaire au fil de la nuit.
4. **On n'est jamais spectateur.** Tombé, tu deviens un fantôme qui peut encore aider un peu (éclairer, ouvrir).
5. **Une progression entre les nuits** (le camp, la forge) — c'est ce qui évite le jeu « qu'on lâche au bout
   de trois heures ». **Pas avant que la nuit elle-même soit amusante.**
6. **La trahison en option** (un valet qu'on soudoie, un guetteur qui peut mentir). Plus tard, et seulement si Martin la veut.

## Une nuit (proposition : 10 à 15 minutes)

1. Le pont-levis s'abaisse, on entre avec une torche chacun.
2. Le parchemin donne **cinq quêtes** tirées au hasard dans le château.
3. Chaque quête finie allume quelque chose (un brasero, un vitrail) : on **voit** la nuit avancer.
4. Plus la nuit avance, plus ce qui rôde est réveillé.
5. Les cinq faites : la herse de sortie s'ouvre. Sortir avant l'aube = nuit gagnée.

### Exemples de quêtes (courtes, une action claire chacune)

| Quête | Ce qu'on fait |
|---|---|
| Rallumer la chapelle | allumer quatre cierges dans le bon ordre (indiqué par un vitrail) |
| Sonner le tocsin | monter au clocher et tirer la corde — tout le château l'entend |
| Remonter la herse | deux joueurs tournent le treuil en même temps |
| Porter le calice | le porteur est lent et ne peut pas courir ; s'il le lâche, ça résonne |
| Nourrir les corbeaux | trouver le sac de grain et le porter à la volière |
| Refermer la crypte | pousser trois dalles sur leurs tombes avant que ça sorte |

## Le look

- **Noir, brouillard, un seul accent de couleur** : l'or des torches. (Rouge pour ce qui est maudit.)
- De **vrais modèles 3D** : packs gratuits (KayKit Dungeon, Quaternius, Kenney) assemblés par le code,
  et/ou des modèles faits avec Claude branché sur Blender, sur le PC de Martin.
- L'obscurité joue pour nous : on ne voit que ce que la torche éclaire.

## Ce qu'on garde de FIEF

Le joueur en première personne, le jeu en ligne (salon, Steam), les sons Kenney, les menus, le
système de réglages, le vérificateur et le compilateur de contrôle. Le château actuel peut servir
d'extérieur ; l'intérieur (salles, couloirs, crypte) est à construire.

## Les étapes (chacune se teste en un soir)

- **V0 — le frisson.** *(écrite le 11/11, jamais lancée dans Unity : `Scripts/Night/NightBootstrap.cs`,
  menu FIEF ▸ LA NUIT - lancer le test ; en blocs provisoires.)* Un couloir et deux salles dans le noir, ta torche, une quête (rallumer la
  chapelle), le Pénitent. Seul. *Si ça ne donne pas un petit frisson, on change avant d'aller plus loin.*
  **(12/11) Habillée avec le kit fait dans Blender** (`Scripts/Night/NightKit.cs`, `docs/BLENDER.md`) : murs en
  pierres de taille, arches, voûte, piliers, torches, braseros, cierges en fer forgé, fenêtre gothique et vitrail,
  rayon de lune. Les blocs restent dessous, invisibles : collisions et règle du Pénitent inchangées. Deux retouches
  du niveau pour la grille de 4 m : la niche du Pénitent va de z 20 à 24, et le couloir a cinq paires de piliers.
  **Lancée dans Unity par Claude** (sans fenêtre) et vue en photos (`Tools/photos-nuit.ps1`) ; **Martin ne l'a pas
  encore jouée** : le frisson reste à juger par lui.
- **V1 — une nuit entière, seul.** Cinq quêtes, trois monstres, la sortie, l'aube.
- **V2 — à plusieurs.** La même nuit en ligne, les fantômes.
- **V3 — entre les nuits.** Le camp, la progression.
- **V4 — la trahison**, si Martin la veut.

## Ce qu'il manque pour démarrer (questions à Martin)

1. ~~La 3D~~ **Tranché le 11/11 : les deux** — packs gratuits ET modèles faits avec Claude + Blender sur le
   PC de Martin (`docs/BLENDER.md`). La loi « zéro création 3D sur mesure » est donc levée pour ce jeu.
2. ~~Ta carte graphique~~ **Vue le 12/11 : NVIDIA GeForce RTX 3050 Ti (portable).** Elle tient des textures 4K et
   une dizaine de lumières ; pas de quoi viser le photoréalisme d'un jeu AAA.
3. **Elden Ring** : qu'est-ce que tu veux en garder — les ruines gothiques, les boss, la difficulté ?
4. **Avec qui tu joues** : ton frère seul, ou un groupe ? Combien ?
5. **Les propositions 1 à 6** ci-dessus : lesquelles tu gardes ?
