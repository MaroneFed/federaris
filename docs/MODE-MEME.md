# Le mode mème (v46.1)

> Martin, 15/10 : « fais le mode mème : une grosse liste de mèmes, avec son ».
> Puis, 17/10 : « t'as rien capté : le mode mème, c'est comme le mode normal et le mode Dieu, sauf que
> là tu mets tous les mèmes d'internet, genre 67, fahhh ».

**Un MODE DE JEU, au salon** : la ligne **Mode** passe de **Normal** à **DIEU** à **MÈME** (contre les
bots comme en ligne, l'hôte décide ; `Match.MemeMode`, envoyé aux invités avec l'état du match,
protocole 13). En v45 ce n'était qu'un réglage : il a disparu des Réglages.

Les **règles du mode normal**, et les mèmes d'internet partout :

- **Les capacités SONT des mèmes** (v46.1, Martin : « t'as pas capté : les capacités sont des mèmes
  que t'adaptes ») : comme le Mode Dieu a sa table (les divines), le mode mème a la sienne —
  **46 actives et 26 passives**, chacune une capacité du jeu habillée du mème qui dit ce
  qu'elle fait (FAHHH sonne, le 6 7 fait danser le 6 7, BONK assomme, RICKROLL piège, FANUM TAX te
  ramène le joueur…). Sur la carte : le nom du mème et sa phrase. **Au lancer, le haricot crie son
  mème** (le son part de lui, tout le monde l'entend) **et le mot claque au-dessus de sa tête** ;
  le tien claque au milieu de ton écran (`Match/MemeSkins.cs`, `Memes.CastShout`). Les numéros des
  capacités ne changent pas : le réseau marche pareil.

- **Le « 6 7 »** : toutes les **67 secondes** de manche (67, 134, 201…), la même seconde chez tout le
  monde, « 6 7 !! » claque à l'écran, la voix crie « SIX SEVEN », et **tous les haricots font le
  geste** trois secondes (les deux mains paumes en l'air qui pèsent chacune son tour —
  `CharacterRig.SixSevenUntil`). Le geste est aussi la **dixième danse sur B**.
- **Les légendes** : sur chaque grand moment qui **te** concerne, un mème claque au milieu de l'écran
  (lettres blanches cernées de noir, qui tremblent), avec **son cri**, un coup de caméra, parfois
  l'écran qui « frit ». Un à la fois, jamais deux du même genre en 6 s.
- **Les cris** : une petite **voix de haricot fabriquée par le code** (synthèse à formants,
  `Core/SfxVoix.cs`) crie **FAHHH**, **SIX SEVEN**, **BRUH**, **SHEESH**, **OOF**, **WOW**, **NOOON** ;
  plus les onze bruits de v45 (klaxon, trombone triste, BONK…, `Core/SfxMemes.cs`).
- **Les bots** s'appellent Skibidi, Gigachad, Le NPC, Rizzler, Fanum, Sigma, Ohio.
- Au salon, choisir MÈME fait crier « SIX SEVEN », et le titre tremble en rose.

Pas d'« AURA » dans la liste : Martin déteste (30/09).

**34 moments, 194 légendes** (une au hasard à chaque fois) :

| # | Quand | Son | Légendes |
|---|---|---|---|
| 1 | quelqu'un te fait tomber dans les nuages | Fahhh | FAHHH · BRUH · SKILL ISSUE · EMOTIONAL DAMAGE · IT'S SO OVER · L · WASTED · MISSION FAILED · TO BE CONTINUED... · OOF · JE SUIS MORT · HOUSTON, ON A UN PROBLÈME |
| 2 | tu tombes tout seul | Bruh | BRUH · NPC MOVE · SKILL ISSUE · OHIO MOMENT · BRAINROT · 100 % GÉNIE · MAIS POURQUOI · C'EST QUOI CE GAMEPLAY |
| 3 | tu fais tomber quelqu'un (KO) | Klaxon + écran frit | YEET · GET OUT · RATIO · CHEH · SEE YOU NEVER · BYE BYE · GO TOUCH GRASS · NAH, I'D WIN · CIAO |
| 4 | deux KO en 12 s | Sheesh + écran frit | SHEESH · DOUBLE KILL · W · ON GOD · LET HIM COOK · IL EST CHAUD |
| 5 | trois KO en 12 s | Sheesh + écran frit | SHEEEESH · GIGACHAD · TRIPLE KILL · HE'S HIM · INARRÊTABLE · SIGMA GRINDSET |
| 6 | tu prends la Couronne | Wow | W · LET HIM COOK · MAIN CHARACTER · SIGMA · NAH, I'D WIN · C'EST LE S · KING · RIZZ MAXIMUM |
| 7 | on te prend la Couronne | Fahhh | FAHHH · NOOON · IT'S SO OVER · L · EMOTIONAL DAMAGE · NAN MAIS ALLÔ · BRUH |
| 8 | tu voles la Couronne | Caisse + écran frit | FANUM TAX · MINE · W · NO CAP · C'EST LE S · SKILL ISSUE (LUI) · MERCI BIEN |
| 9 | Couronne volée dans le cercle du Monument | Klaxon + écran frit | FANUM TAX · EMOTIONAL DAMAGE · ABSOLUTE CINEMA · COLD · LE VOL DU SIÈCLE |
| 10 | ton sacre commence | Suspense | LOCK IN · WE'RE SO BACK · TIC... TAC... · 6... 7... · BOUGE PAS |
| 11 | ton sacre est arrêté | Nooo | NOOON · IT'S SO OVER · FAHHH · SO CLOSE · L · ÇA FAIT MAL |
| 12 | sacre dans les 15 dernières secondes | Klaxon + écran frit | ABSOLUTE CINEMA · CLUTCH · SHEESH · WE'RE SO BACK · CINÉMA |
| 13 | tu gagnes la manche | Sheesh + écran frit | W · GG EZ · SIGMA · GIGACHAD · ABSOLUTE CINEMA · MOGGED · C'EST LE S · HE'S HIM |
| 14 | un autre gagne la manche | Trombone | L · BRUH · IT'S SO OVER · NPC · SKILL ISSUE · C'EST LE LAG |
| 15 | tu gagnes le match | Fanfare + écran frit | LE GOAT · GIGACHAD · ABSOLUTE CINEMA · SIGMA DU MOIS · GG EZ · THE GOAT HAS SPOKEN |
| 16 | tu perds le match | Trombone | IT'S SO OVER · L + RATIO · C'EST LE LAG · BRUH · GG (PAS EZ) |
| 17 | on te pousse fort | Oof | BONK · OOF · BRUH · EMOTIONAL DAMAGE · RIP · AÏE |
| 18 | tu pousses quelqu'un | Bonk | BONK · YEET · CHEH · GET OUT · SIGMA PUSH · SKILL ISSUE |
| 19 | tu pousses celui qui venait de te pousser | Boum + écran frit | UNO REVERSE · KARMA · CHEH · INSTANT KARMA · NO U |
| 20 | tu vires quelqu'un du sommet | Boum + écran frit | YEET · MOGGED · GET OUT · PAS AUJOURD'HUI · RATIO |
| 21 | un obstacle te frappe (hors de la tour) | Bonk | BONK · OOF · SKILL ISSUE · NPC MOVE · REGARDE DEVANT |
| 22 | un obstacle t'éjecte de la tour | Fahhh | FAHHH · TO BE CONTINUED... · IT'S SO OVER · BACK TO SPAWN · NOOON |
| 23 | on t'enferme (Prison, temps arrêté) | Suspense | BONK. JAIL. · EN TAULE · NO ESCAPE · TIME OUT · L |
| 24 | on te rétrécit | Couic | SMOL · TINY · MINI MOI · COUIC · BÉBOU |
| 25 | tête à l'envers | Faux | OHIO · BRAINROT · ERROR 404 · WHAT · JE COMPRENDS RIEN |
| 26 | tu danses (B) ou on te fait danser | SixSept | 6 7 · SKIBIDI · VIBE CHECK · LA PISTE EST À MOI · IT'S GIVING |
| 27 | ta capacité est refusée | Faux | NOPE · NAH · ACCESS DENIED · NON. · BRUH |
| 28 | ton Miroir renvoie un sort | Boum + écran frit | UNO REVERSE · NO U · RETURN TO SENDER |
| 29 | ton Home run part | Bonk + écran frit | HOME RUN · YEET · TO THE MOON · BYE |
| 30 | la Couronne change 5 fois de mains en 15 s | Bruh | PATATE CHAUDE · CHAOS · BRAINROT · OHIO |
| 31 | les quatre anneaux avec la Couronne | Wow | SHEESH · CLEAN · MAIN CHARACTER · W |
| 32 | palier de puissance (×1,5, ×2, ×2,5) | Boum + écran frit | LOCK IN · ÇA CHAUFFE · SHEESH · IT'S GETTING HOT |
| 33 | le « fight » du départ | Klaxon | LOCK IN · LET'S GO · 6 7 · GL HF · SKIBIDI |
| 34 | toutes les 67 secondes de manche (tout le monde) | SixSept + écran frit | 6 7 !! · SIX SEVEN · 6 7 6 7 · SIX-SEPT !! |

## Les capacités-mèmes

### Les actives

| Mème | Ce que ça fait | Cri | Capacité d'origine |
|---|---|---|---|
| **FAHHH** | Tu cries FAHHH : tous ceux devant toi sont sonnés. | Fahhh | `Cri` |
| **6 7** | Tu cries SIX SEVEN : tous ceux à 9 m font le 6 7, bloqués 3 s. | SixSept | `Disco` |
| **Vine boom** | BOUM : tout le monde à 11 m de toi s'envole. | Boum | `Onde` |
| **It's so over** | Une vague de larmes part de toi et emporte tout le monde à 20 m. | Nooo | `Raz` |
| **Bass boosted** | Tu tapes le sol : tous ceux qui sont debout à 25 m sautent en l'air. | Boum | `Seisme` |
| **Bonk** | Un énorme BONK frappe droit devant toi. | Bonk | `Gant` |
| **Ultra bonk** | Tu sautes, puis tu retombes : BONK sur tout le monde à 10 m. | Bonk | `Meteore` |
| **This is Sparta** | Ta prochaine poussée envoie 3 fois plus loin. SPARTAAA. | Boum | `CoupDePied` |
| **Sheesh punch** | Tu charges ton poing, puis tu frappes : le joueur touché part très loin. | Sheesh | `PoingFaucon` |
| **Leeroy** | Tu fonces 25 m en hurlant : le premier touché est emporté et écrasé au bout. | Fahhh | `Charge` |
| **Sigma** | Tu fonces comme un sigma sur 30 m et tu renverses tout. | Sheesh | `Boulet` |
| **Speedrun** | Tu fonces 15 m droit devant. Ceux sur ton chemin sont bousculés. | Sifflet | `Ruee` |
| **UwU** | 6 oreillers tout mignons droit devant toi : paf, paf, paf ! | Couic | `Oreillers` |
| **Emotional damage** | Vise un joueur : la foudre lui tombe dessus. | Boum | `Foudre` |
| **Ratio** | Un ratio guidé part chercher le joueur le plus proche et explose. | Klaxon | `Missile` |
| **Bombardiro** | Des bombes tombent du ciel là où tu vises. | Klaxon | `Deluge` |
| **Aw man** | Une caisse de TNT clignote 3 s, puis explose (10 m). | Suspense | `Tnt` |
| **Surprise** | Tu poses une mine. Le premier qui passe à côté a une surprise. | Klaxon | `Mine` |
| **Ice cold** | Une boule de glace : ceux qu'elle touche sont poussés et ralentis. | Wow | `Gel` |
| **It's back** | Un boomerang : il frappe à l'aller et au retour. | Wow | `Boomerang` |
| **Skibidi** | Une tornade skibidi part devant toi et envoie tout le monde en l'air. | SixSept | `Tornade` |
| **Ohio** | Un trou noir d'Ohio aspire tout le monde devant toi, puis explose. | Suspense | `TrouNoir` |
| **OIIA OIIA** | Tu tournes comme le chat : ceux qui te touchent sont éjectés. | Couic | `Toupie` |
| **This is fine** | Pendant 4 s, du feu derrière toi. Qui marche dedans est éjecté. | Suspense | `Flammes` |
| **Rizz** | Vise un joueur : ton rizz l'attire, il marche vers toi 3 s. | Wow | `Hypnose` |
| **Fanum tax** | Vise un joueur, jusqu'à 40 m : tu le tires jusqu'à toi. Taxé. | Caisse | `Crochet` |
| **Mukbang** | Tu aspires le joueur devant toi (15 m), puis tu le recraches très loin. | Caisse | `GobeTout` |
| **Yeet** | Vise un joueur : tu l'attrapes et tu le YEET là où tu regardes. | Wow | `Lasso` |
| **En taule** | Vise un joueur : en taule 10 s. Un coup le libère. | Suspense | `Prison` |
| **Patate chaude** | Vise un joueur : une bombe se colle à lui et explose 2 s après. | Suspense | `Bombe` |
| **Brainrot** | Vise un joueur : sa gauche et sa droite sont inversées 6 s. | Faux | `Inversion` |
| **Smol** | Vise un joueur : il devient tout smol, lent, et s'envole au moindre coup. | Couic | `Mini` |
| **To the moon** | Vise un joueur : il gonfle, s'envole, puis éclate. | Wow | `Ballon` |
| **Rickroll** | Tu poses un piège : qui marche dessus est rickrollé, bloqué 3 s. | Fanfare | `PiegeLoup` |
| **Skill issue** | 3 peaux de banane derrière toi : qui glisse a un skill issue. | Faux | `Banane` |
| **Touch grass** | Une flaque de colle : qui marche dedans reste collé à l'herbe. | Bruh | `Glu` |
| **Uno swap** | Vise un joueur, jusqu'à 45 m : vous échangez vos places. | Scratch | `Echange` |
| **Lag** | Tu lagues et tu réapparais 20 m plus loin. | Scratch | `Clignement` |
| **Glitch** | Pendant 4 s, plus rien ne te touche. Pas avec la Couronne. | Scratch | `Fantome` |
| **Tu me vois pas** | Tu deviens invisible pendant 7 secondes. | Wow | `Voile` |
| **Absolute unit** | Tu deviens énorme 7 s : on ne peut plus te pousser. | Boum | `Geant` |
| **Stonks** | Tu décolles to the moon, puis tu voles où tu veux. Pas dans le château. | Wow | `Fusee` |
| **YOLO** | Tu te lances très loin devant toi. Pas avec la Couronne. | Wow | `Catapulte` |
| **Tung tung tung** | Pendant 5 s, tu rebondis très haut à chaque pas : tung, tung, tung. | Bonk | `Pogo` |
| **Boing** | Un trampoline apparaît à tes pieds. Tout le monde peut sauter dessus. | Couic | `Ressort` |
| **Jumpscare** | Tu sautes très haut. Ceux qui sont collés à toi sont repoussés. | Boum | `Bond` |

### Les passives

| Mème | Ce que ça fait | Cri | Capacité d'origine |
|---|---|---|---|
| **Uno reverse** | Les sorts qu'on te lance repartent sur celui qui les a lancés. | Boum | `Miroir` |
| **Karma** | Celui qui te pousse est repoussé à son tour. | Boum | `Riposte` |
| **We're so back** | Quand tu tombes dans les nuages, tu repars avec des ailes d'or, protégé 6 s. | Sheesh | `SecondSouffle` |
| **Light weight baby** | Avec la Couronne, tu voles aussi vite que les autres. | Wow | `Plume` |
| **Mewing** | Mâchoire de béton : les pièges de la tour ne te font plus tomber. | Sheesh | `TeteDure` |
| **Gigachad** | Le premier coup que tu prends dans la manche ne te fait rien. | Sheesh | `Armure` |
| **Unbothered** | Quand on te pousse, tu pars 2 fois moins loin. | Bruh | `Ancrage` |
| **Sigma push** | Ta poussée envoie 2 fois plus loin. | Bonk | `Poigne` |
| **Plot armor** | Les sorts qu'on te lance durent 2 fois moins longtemps. | Wow | `Increvable` |
| **Not today** | Une fois par manche, si tu tombes dans les nuages, tu reviens où tu étais. | Nooo | `AngeGardien` |
| **Kaboom** | Quand on te pousse, tu exploses : ceux autour de toi s'envolent. | Boum | `Kamikaze` |
| **Gamer rage** | Chaque coup que tu prends rend ta prochaine poussée plus forte. | Fahhh | `Rage` |
| **Lowkey** | Reste sans bouger 1 seconde : tu deviens invisible. | Wow | `Ninja` |
| **Main character** | Chaque fois que tu frappes quelqu'un, tu cours plus vite 3 secondes. | Sheesh | `Vampire` |
| **W luck** | Une fois sur trois, ta capacité revient tout de suite. | Fanfare | `Chanceux` |
| **Sneaky** | Quand tu prends la Couronne à quelqu'un, tu deviens invisible 2 s. | Caisse | `Pickpocket` |
| **Speedrunner** | Tu cours un peu plus vite que les autres. | Sifflet | `Coureur` |
| **Gotta go fast** | Au début de chaque manche, tu cours très vite pendant 20 secondes. | Sifflet | `Sprinter` |
| **Jump boost** | En l'air, appuie encore sur Espace : tu sautes une 2e fois. | Couic | `DoubleSaut` |
| **Bunny hop** | Tu sautes beaucoup plus haut. | Couic | `Kangourou` |
| **I believe I can fly** | Tu as toujours des ailes d'or : tu voles plus vite et plus loin. | Wow | `Planeur` |
| **Ground pound** | Quand tu retombes de haut, tu fais une onde de choc. | Boum | `Rebond` |
| **Grindset** | Ta capacité revient plus vite. | Sheesh | `Recharge` |
| **Sus detector** | Tu vois ce qui est caché : les mines, les invisibles, à travers la fumée. | Suspense | `Flair` |
| **Spaghetti arms** | Ta poussée touche de plus loin : 5 m. | Couic | `BrasLongs` |
| **Home run** | Toutes les 4 poussées réussies, la suivante envoie 3 fois plus loin. | Bonk | `HomeRun` |

## Les sons

**Dix-huit sons, tous fabriqués par le code** : sept cris (Fahhh, SixSept, Bruh, Sheesh, Oof, Wow,
Nooo) et onze bruits (Klaxon, Trombone, Scratch, Bonk, Boum, Fanfare, Suspense, Couic, Faux, Sifflet,
Caisse). Chacun a son dossier dans `Assets/_Fief/Resources/Sons/Memes/` : **un fichier posé dans le
dossier remplace le son du code** (au hasard s'il y en a plusieurs). Mode d'emploi :
`Sons/Memes/LISEZ-MOI.txt`. Exemple : le vrai « fahhh » dans `Sons/Memes/Fahhh/`, le son « 67 » dans
`Sons/Memes/SixSept/`.

**Pourquoi Claude n'a pas téléchargé les vrais sons** : les enregistrements des mèmes (le « fahhh »,
le son « 67 », le vine boom…) appartiennent à leurs auteurs ; dans un jeu **vendu sur Steam**, c'est un
risque de retrait de la page. Les **mots** (BRUH, SKILL ISSUE, 6 7…) ne sont à personne ; les sons du
code sont à nous. Pour jouer entre amis, Martin peut déposer les vrais dans les dossiers ; avant la
sortie, il faudra des sons libres (CC0 : freesound.org avec le filtre « Creative Commons 0 »).
