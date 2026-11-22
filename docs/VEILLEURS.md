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

### Ce que Martin a dit le 14/11 (acquis — remplace « ça doit rester sympa »)

- **Ça doit faire PEUR**, « hyper flippant ». Le Pénitent « câlin » a été refait pour ça.
- **Le but : SURVIVRE dans le château.** On est des chevaliers « hyper stylés », on vient d'entrer, le château
  est vide et bizarre.
- **L'ouverture, en cinématique** : un homme nous dit « c'est plus comme avant, tout a changé », un bruit, il fuit
  en hurlant ; on se retourne, une forme noire ; quand on regarde à nouveau, il n'y a plus personne.
- **Garder LA FOI** en faisant des actions.
- **Des petits gestes à la main**, comme la serrure de Fallout : pour les portes, pour les cierges, etc.
- **Des monstres qu'on voit**, « un peu cons », qui rôdent et refont les mêmes mouvements : derrière eux ils ne
  te voient pas, devant eux « t'es dans la merde ». On peut les observer d'en haut, par une fenêtre, ouvrir une
  porte et courir. **Et des monstres qu'on ne voit pas.**
- **On est au Moyen Âge** (pas de briquet : un silex). **Des cinématiques « trop stylées ».**
- Les 4 scénarios proposés par Claude le 14/11 sont **refusés** ; il veut des textes courts.

**Fait le 14/11 (première tranche, `Scripts/Night/NightSurvival.cs`)** : la Foi (croix en bas à gauche, elle baisse
seule, plus vite quand on est traqué ; prier à l'autel ou allumer un cierge la remonte ; à zéro on est pris),
la sacristie verrouillée et sa **serrure à crocheter**, le **silex** à y prendre, les cierges qui s'allument en
**battant le silex** (trois étincelles), et **le Rôdeur** : il fait sa ronde, sa lanterne verte EST son regard,
elle vire au rouge quand il t'a vu. Un geste raté fait du bruit : il vient voir.
**Fait le 15 et le 16/11**, après deux essais de Martin (« le jeu est nul », « pas flippant du tout », « je déteste
la police, les voix, la musique quand on se fait voir », « le monstre n'est pas réaliste », « beaucoup trop petit ») :

- **Le grand château** (`Scripts/Night/NightMap.cs`) : il est DESSINÉ dans un plan, une lettre par case de 4 m, et
  bâti tout seul avec le kit. 14 × 13 cases : vestibule, sacristie, un couloir voûté qui fait le tour, la grande
  salle (24 × 16 m, 8 m de haut, bordée d'armures), la cour à ciel ouvert sous la lune, la chapelle, la
  bibliothèque, le dortoir, l'armurerie, la niche du Pénitent. Les monstres y trouvent leur chemin tout seuls.
- **L'ouverture en cinématique** (`NightFear.cs`, `NightCinema`) : l'homme, le fracas, la forme noire, le noir.
- **Les monstres ne sont plus des statues éclairées** : des SILHOUETTES noires sans aucun reflet (matière
  « Ombre »), dans une fumée noire, avec seulement leurs yeux ou leur lanterne. Deux Rôdeurs : un dans le grand
  couloir, un dans la grande salle.
- **La prise** : attrapé, on voit le monstre en face une seconde, l'image tremble, puis le noir.
- **Le son** fabriqué par le code : vent grave, cœur qui s'emballe, chocs et fer qui gémit au loin.
  **Retirés à la demande de Martin** : toutes les voix (les siennes et les murmures), la musique de traque.
  Sa musique de fond (« Stone Cathedral Reverb ») et celle de l'aube (« Pax et Lux ») restent.
- **La police** : une police à empattements du PC (Garamond, sinon Palatino...), avec une ombre.
- **Plus sombre** : les torches du vestibule s'éteignent à l'ouverture, la torche du joueur porte à 8 m.

**Fait le 16/11 au soir**, après le troisième essai (« le monstre ne fait pas flippant », « pourquoi on voit le
monstre dans l'animation », « ses mouvements sont prévisibles », « le silex est moche », « la croix plus détaillée ») —
`Scripts/Night/NightDirector.cs` :

- **L'ouverture ne montre plus le monstre** : on se retourne, il n'y a rien ; les torches meurent, le noir.
- **Le clignement** : un noir d'un quart de seconde, plus fréquent quand la peur monte. Le Pénitent bondit pendant
  le clignement, même si on le fixe.
- **Imprévisible** : le Pénitent hésite parfois, ou se retrouve ailleurs quand on est loin et qu'on ne le voit pas.
  Les Rôdeurs n'ont plus de ronde : ils vont où ils veulent, se retournent d'un coup, éteignent leur lanterne
  (éteinte, ils ne voient plus mais entendent courir).
- **Le château te regarde** : les armures se tournent vers toi dans ton dos ; des pas te suivent et s'arrêtent un
  pas trop tard ; une silhouette au bout d'un couloir disparaît quand tu ne la regardes plus.
- **Du détail** : un pot à braises en fer à la place du silex, une croix ouvragée pour la Foi, de la poussière
  dans la lumière de la torche, des ténèbres au ras du sol.

**Fait le 21/11** (le frère de Martin : « pas de pièces vides, plus de rebondissements, du style HeroQuest ») —
`Scripts/Night/NightQuest.cs` et `NightMap.Furniture` :

- **Toutes les salles meublées** (dix pièces neuves dans le kit) : le festin abandonné et la cheminée allumée de la
  grande salle, les rayonnages de la bibliothèque, les lits du dortoir, les râteliers de l'armurerie, les tombes de
  la cour, les gisants et les bancs de la chapelle, des coffres un peu partout.
- **La chapelle est fermée à clé.** La clé est cachée au hasard (jamais près du départ) dans un coffre ou une
  bibliothèque. **Fouiller** = tenir E 1,5 s : relique (+foi), piège (−foi, et le bruit attire les Rôdeurs),
  un tombeau d'où sort un Rôdeur de plus, un bruit tout près, ou rien. Trouver la clé réveille le Pénitent.
- **Un événement par salle**, la première fois : les armures se tournent toutes vers toi au prochain clignement
  (grande salle), un livre tombe et une silhouette apparaît (bibliothèque), la torche s'étouffe (dortoir), une lame
  tombe (armurerie), l'orage et ses éclairs (cour).

**Fait le 21/11 au soir** (Martin : « une vidéo où on arrive à cheval, tout délabré ; des passages secrets, plusieurs
quêtes, un boss, beaucoup plus grand ; je n'aime pas la police ni les cubes en haut ; un menu, une carte, un inventaire ») :

- **Le menu** se dessine sur le paysage, devant le château (`NightUI`) : Entrer dans la nuit, Commandes, Quitter.
  **Échap** : la pause. **Tab** (ou M, I) : **le carnet**, sur parchemin — la carte à gauche (elle se révèle salle
  par salle, portes fermées en rouge, une flèche pour toi), les quêtes et ce que tu portes à droite (une lettrine
  par objet). L'objectif s'écrit à la plume en haut à gauche quand il change. Plus de petits cubes en haut.
- **Les polices** : Old English (gothique) pour les titres, Centaur (livre ancien) pour le texte. **Attention** : ce
  sont des polices de Windows/Office, prises sur le PC ; un autre PC sans Office retombe sur Book Antiqua ou Georgia.
  Pour un jeu vendu, il faudra une police libre (OFL) rangée dans le projet.
- **L'arrivée à cheval** (`NightIntro`, 25 s, Entrée pour passer) : le chemin de dalles brisées, les ruines, les
  arbres morts, le cimetière aux tombes ouvertes, la façade de 12 m et ses tours, deux braseros allumés devant la
  porte ; au pas, puis au galop ; un éclair, une silhouette traverse le chemin, au suivant plus rien ; le cheval
  renâcle, la porte s'ouvre seule, on met pied à terre, le noir. Puis l'ouverture d'avant.
- **Le château agrandi** (`NightMap`, 18 × 17 cases) : + la crypte, le trésor, les cuisines (cheminée allumée), les
  cachots. **Deux passages secrets** : derrière l'autel (s'ouvre quand les quatre cierges brûlent) et derrière un
  rayon de la bibliothèque (« un livre dépasse » : le trésor et la lanterne des Veilleurs).
- **Les quêtes** (`Journal`) : trouver du feu → la clé de la chapelle → les quatre cierges → descendre derrière
  l'autel → le boss. En option : trois reliques (bénédiction : la foi s'éteint deux fois moins vite), le passage
  de la bibliothèque (la lanterne : la torche porte plus loin).
- **Le boss, l'Abbé** (`NightBoss.cs`) : la crypte se referme derrière toi. 4,5 m, il ne s'arrête jamais mais
  marche moins vite que toi. Rallumer les quatre brasiers des coins (le geste du feu) : chacun le fait reculer et
  le tient hors de sa lumière, mais il revient plus vite. Au quatrième, il brûle, et l'aube se lève.

### Le 21/11 au soir (acquis — Martin : « une carte toujours là », « la quête toujours visible », « une torche en main
qui s'abaisse », « des monstres plus compliqués », « un château beaucoup plus grand », « plus de clignement »)

- **La torche en main** (`NightLight.cs`, `HeldTorch`) : un bâton, une bague de fer, des chiffons, le vrai feu du kit.
  Elle suit le regard avec retard, se balance au pas, **clic gauche : un coup de torche**. **Sa flamme et sa lumière
  ont la taille de ta réserve** (`Faith` n'est plus la foi : c'est la réserve de la torche, 5 min pour mourir).
- **Le crucifix est remplacé par une bougie blanche** (en bas à gauche, `LightGauge`) : sa cire fond avec la réserve,
  sa flamme vacille, bleuit quand elle meurt ; étincelles d'or quand on la ravive.
- **Raviver** : E tenu près d'une cheminée (grande salle, cuisines, donjon : infinies) ou d'un feu allumé (qui perd ce
  qu'on lui prend). **Allumer** (E) : les torches des couloirs, les bouquets de bougies aux coudes, les braseros —
  ils brûlent un temps (bougies 70 s, torches 150 s, braseros 260 s), **se consument à vue d'œil** (la cire raccourcit),
  et **aucun monstre ne passe leur lumière** (`NightWard`, `NightMap.Passable`, `NightWard.Hold`).
- **Plus de clignement** (`NightBlink` ne ferme plus jamais les yeux).
- **Toujours à l'écran** : la quête en cours en haut à gauche (et deux quêtes de côté), **la petite carte en bas à
  droite** (parchemin cerclé de fer, toi au milieu, les feux allumés en points d'or), les objets en bas au milieu.
- **La serrure refaite** : une platine de fer rivetée, un barillet de laiton, la clé de tension, un crochet d'acier
  qui rougit et casse (tout dessiné par le calcul).
- **Les coffres** : un par salle au plus, **chacun a quelque chose**, une lueur d'or filtre du couvercle. Les
  tombeaux et rayons vides ne se fouillent plus. **Les objets** : huile (1), eau bénite (2, tout fuit), bougies à
  poser (3), le cierge éternel (4, la récompense de l'ossuaire), reliques, lanterne.
- **L'ouverture** : un Veilleur agonise au sol, explique (l'Abbé, la crypte, les trois vivants et où ils sont), te
  donne sa torche et meurt. Plus de « Survis. ».
- **Les survivants** (`Survivor`, modèles Quaternius CC0) : Mathilde la cuisinière (viande + huile), Aubin le
  palefrenier (bougies), frère Anselme le novice, en haut du donjon (la clé de la chapelle + eau bénite). On leur
  parle (E, boîte de dialogue), ils partent à la chapelle ; à l'aube, ils y sont.
- **Croc, le chien des écuries** (`Hound`) : nourri, il te suit, gronde quand un monstre approche, se jette dessus.
- **Six bêtes animées** (`Beast`, Quaternius CC0, converties par Blender) : les **Âmes** (traversent les murs,
  boivent ta flamme, un coup les dissipe), le **Boucher** (aveugle, il entend : ne cours pas), le **Démon** (n'approche
  que si ta torche faiblit, souffle les bougies), **la Bête de l'ossuaire** (charge tout droit, s'assomme contre les
  murs : frappe-la sonnée, cinq coups), l'**Écorcheur** (ne vient que dans ton dos, fuit ton regard), les **loups** de
  la cour (tournent autour de la flamme). Ta torche, forte, les tient à 2,5 m. Après une prise, tu te réveilles au
  dernier feu où tu as ravivé ta torche, et le jeu te dit ce que tu as mal fait.
- **Le château** (`NightMap`, 27 × 19 cases, **trois niveaux**) : + la grande cour (ciel, puits, arbres morts,
  cimetière), les écuries, le donjon (rez et **étage**, par un escalier), les **caves et l'ossuaire en sous-sol**
  (escalier depuis les cuisines). Les murs vont du plus bas au plus haut des deux salles ; les escaliers sont des
  rampes invisibles sous seize marches.

- **Tous les sons refaits** (21/11 au soir, Martin : « change tous les sons et la musique, horribles pour les
  oreilles ») : fini les sons fabriqués par le calcul. De **vrais enregistrements CC0** d'OpenGameArt, rangés par
  rôle dans `Resources/Sons/Nuit/` (Fond, Musique, Aube, Coeur, Frisson, Cri, Gemit, Grogne, Rugit, Souffle, Aboie,
  Hurle, Serrure, Feu, Livre, Coffre, Pierre, Chaine, Tresor, Tonnerre — sources dans `LICENCES.txt`). Musique :
  Juhani Junkala, « Horror Atmosphere » (nappes, sans mélodie) ; à l'aube, un piano. Chaque bête a sa voix (la Bête
  et le Boucher rugissent, les Âmes gémissent, le Démon grogne, l'Écorcheur souffle dans ton dos, les loups hurlent,
  Croc aboie). Les musiques Suno de la nuit sont **mises de côté** dans `Assets/_Fief/Audio/Ancien-Nuit` (pas
  supprimées). Changer un son = remplacer les fichiers de son dossier.

- **Des monstres bien plus flippants** (21/11 au soir, Martin : « je veux que le joueur se chie dessus ») :
  **silhouettes noires** (la matière Ombre du Pénitent) dans une double fumée, **corps étirés** (grands et maigres :
  le Démon et l'Écorcheur font 2,9 et 2,6 m), plus de gros yeux de dessin animé mais **deux points de braise** sur
  l'os de la tête, qui brûlent plus fort quand ils approchent. Ils **se figent net puis repartent** (jamais un mouvement
  régulier), **tressaillent** quand on les regarde, et **la première fois qu'ils entrent dans ta lumière, ils hurlent**
  (l'image se resserre et tremble). Près d'eux, ta torche tousse et le brouillard s'épaissit (`Beast.Dread`) ; le
  Démon et l'Écorcheur **soufflent les feux**. **La prise est un jumpscare** : il fond sur toi jusqu'à 50 cm, le
  visage plein l'écran, hurlement, éclat rouge, images noires qui sautent. **Le Visiteur** (`NightDirector`) : une
  silhouette de 3 m au bout d'un couloir ; tant que tu la fixes, ta torche tousse et **à chaque fois, elle est plus
  près** ; détourne les yeux, elle disparaît ; fixe-la trop longtemps, elle est sur toi.

### Le 22/11 (acquis — Martin : « des animations flippantes », « les bugs », « le duel du gros monstre est nul »,
« beaucoup plus de passages secrets, et des compliqués », « pas toutes les quêtes en même temps », « un niveau de
difficulté beaucoup plus haut », « quand on meurt, on perd tout »)

- **Le test automatique** (`NightAutoTest`) : `FIEF-La-Nuit.exe -autotest` se joue tout seul (menu, arrivée,
  ouverture, carnet, pause, serrure, dialogue, passages, l'Abbé) et prend de **vraies captures, interface comprise**,
  dans `Logs/AutoTest`. C'est ainsi que Claude a trouvé les bugs : **la grande porte s'ouvrait sur un mur** (le
  vestibule a maintenant une arche ; la porte claque derrière toi), **le carnet coupait les noms d'objets et les
  détails de quêtes** (hauteurs calculées).
- **Les animations flippantes** (`NightHorror.cs`, `CreepyRig`) : par-dessus l'animation du pack, la **nuque cassée**
  (tête penchée à 45-70°), la **tête qui te suit toujours** (jusqu'à 120°, plus loin qu'un cou humain), des
  **craquements** (la tête bascule d'un coup), le **dos voûté**, les **bras qui pendent**, un **tremblement** près de toi.
- **L'Abbé refait** (`NightBoss.cs`) : un géant de 4,6 m (le Démon étiré, en silhouette, nuque cassée), en phases :
  traque → il fait sortir des Âmes → il **éteint** un brasier allumé toutes les 20 s → il **charge** en ligne droite.
  Un coup de torche le fait reculer deux secondes. Les quatre brasiers allumés ensemble : il brûle.
- **Huit passages secrets** (`NightSecrets.cs`, trois salles cachées dans le plan : Z, X, I) : la crypte (cierges),
  le trésor (un livre), **le garde-manger** (trois anneaux au mur sud des cuisines, dans l'ordre que donne Mathilde ;
  une erreur : fracas de chaînes, le Boucher entend), sa sortie vers les cachots (levier), **le passage dérobé**
  (les trois bougeoirs de la bibliothèque allumés en même temps), sa sortie vers le dortoir (levier), **la cave
  cachée** (une bouteille d'un râtelier), **la tombe creuse** de la cour (les deux braseros allumés : un escalier
  jusqu'à l'ossuaire). Chaque salle cachée a son coffre.
- **Les quêtes une par une** : Mathilde → Aubin → le novice → la chapelle ; chacun dit où est le suivant. À l'écran,
  la quête en cours et une seule quête de côté.
- **Plus dur** : la torche meurt en 3 min 30 au calme, rien au départ, la flamme doit être au-dessus de 40 % pour
  tenir les monstres, le Rôdeur court à 5,1 m/s et voit à 13 m, trois Rôdeurs, le Démon, deux Âmes et trois loups dès
  le début, le Pénitent éveillé tout de suite, le Boucher entend la course à 24 m.
- **La mort définitive** : pris, c'est l'écran « Tu es mort », puis **tout recommence** (le château est rebâti,
  retour au menu).
- **La luminosité** : une ligne dans le menu et la pause (gauche / droite), gardée d'une partie à l'autre.

**Pas encore fait / limites** : rien de tout ça n'a été joué par un humain ; **Claude n'entend pas** : les sons ont été
choisis sur leur description, pas à l'oreille ; l'écran (quête, carte, objets, serrure,
bougie) est dessiné par OnGUI, que les photos de Claude ne voient pas. Les modèles Quaternius sont low-poly
« mignons » à l'origine : assombris, ils tiennent dans le noir, mais ce ne sont pas des monstres d'Elden Ring.
Les animations des bêtes sont celles du pack (marche, course, coup), pas faites pour elles.

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
  rayon de lune, **le Pénitent en statue de moine à capuche** (capuche pointue, trou noir du visage, yeux
  rouges, camail sur les épaules ; ses bras et ses mains restent anguleux). Lumière refroidie (fond bleu très faible, feux moins orange). Les blocs restent dessous, invisibles : collisions et règle du Pénitent inchangées. Deux retouches
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
