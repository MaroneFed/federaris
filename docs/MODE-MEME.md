# Le mode mème (v46.2)

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

| Mème | Ce que ça fait | Son (dossier MemesSons/) | Capacité d'origine |
|---|---|---|---|
| **FAHHH** | Tu cries FAHHH : tous ceux devant toi sont sonnés. | Fahhh | `Cri` |
| **6 7** | Tu cries SIX SEVEN : tous ceux à 9 m font le 6 7, bloqués 3 s. | SixSept | `Disco` |
| **Vine boom** | BOUM : tout le monde à 11 m de toi s'envole. | VineBoom | `Onde` |
| **Metal pipe** | Une barre de métal frappe droit devant toi : CLANG. | MetalPipe | `Gant` |
| **Bonk** | Tu sautes, puis tu retombes : BONK sur tout le monde à 10 m. | Bonk | `Meteore` |
| **This is Sparta** | Ta prochaine poussée envoie 3 fois plus loin. SPARTAAA. | Sparta | `CoupDePied` |
| **Sheesh** | Tu charges ton poing, puis tu frappes : le joueur touché part très loin. | Sheesh | `PoingFaucon` |
| **Leeroy Jenkins** | Tu fonces 25 m en hurlant : le premier touché est emporté et écrasé au bout. | Leeroy | `Charge` |
| **Sigma boy** | Tu fonces comme un sigma sur 30 m et tu renverses tout. | Sigma | `Boulet` |
| **Tralalero tralala** | Tu fonces 15 m comme le requin en baskets. Ceux sur ton chemin sont bousculés. | Tralalero | `Ruee` |
| **Emotional damage** | Vise un joueur : la foudre lui tombe dessus. | EmotionalDamage | `Foudre` |
| **Bombardiro crocodilo** | Un crocodile-bombardier part chercher le joueur le plus proche et explose. | Bombardiro | `Missile` |
| **MLG airstrike** | Des bombes tombent du ciel là où tu vises. | Airhorn | `Deluge` |
| **Aw man** | Une caisse de TNT clignote 3 s, puis explose (10 m). | AwMan | `Tnt` |
| **Sus** | Un piège sus : le premier qui passe à côté saute en l'air. | AmongUs | `Mine` |
| **Skibidi** | Une tornade skibidi part devant toi et envoie tout le monde en l'air. | Skibidi | `Tornade` |
| **Ohio** | Un trou noir d'Ohio aspire tout le monde devant toi, puis explose. | Ohio | `TrouNoir` |
| **OIIA OIIA** | Tu tournes comme le chat : ceux qui te touchent sont éjectés. | Oiia | `Toupie` |
| **This is fine** | Pendant 4 s, du feu derrière toi. Qui marche dedans est éjecté. | ThisIsFine | `Flammes` |
| **Rizz** | Vise un joueur : ton rizz l'attire, il marche vers toi 3 s. | Rizz | `Hypnose` |
| **Fanum tax** | Vise un joueur, jusqu'à 40 m : tu le tires jusqu'à toi. Taxé. | FanumTax | `Crochet` |
| **Yeet** | Vise un joueur : tu l'attrapes et tu le YEET là où tu regardes. | Yeet | `Lasso` |
| **Coffin dance** | Vise un joueur : enfermé 10 s, la danse du cercueil joue. Un coup le libère. | CoffinDance | `Prison` |
| **Mission failed** | Vise un joueur : une bombe se colle à lui et explose 2 s après. | MissionFailed | `Bombe` |
| **Huh cat** | Vise un joueur : sa gauche et sa droite sont inversées 6 s. HUH ? | Huh | `Inversion` |
| **Roblox oof** | Vise un joueur : il devient tout petit, lent, et s'envole au moindre coup. | Oof | `Mini` |
| **Shooting stars** | Vise un joueur : il gonfle et part dans les étoiles, puis éclate. | ShootingStars | `Ballon` |
| **Rickroll** | Tu poses un piège : qui marche dessus est rickrollé, bloqué 3 s. | Rickroll | `PiegeLoup` |
| **Windows error** | 3 peaux de banane derrière toi : qui glisse plante comme Windows. | WindowsError | `Banane` |
| **Brr brr patapim** | Une flaque de colle : qui marche dedans prend racine comme Patapim. | Patapim | `Glu` |
| **Uno reverse** | Vise un joueur, jusqu'à 45 m : vous échangez vos places. | VineBoom | `Echange` |
| **Lag** | Tu lagues et tu réapparais 20 m plus loin. | Lag | `Clignement` |
| **You can't see me** | Tu deviens invisible pendant 7 secondes. | JohnCena | `Voile` |
| **Gigachad** | Tu deviens énorme 7 s : on ne peut plus te pousser. | Gigachad | `Geant` |
| **Stonks** | Tu décolles to the moon, puis tu voles où tu veux. Pas dans le château. | Stonks | `Fusee` |
| **Tung tung tung sahur** | Pendant 5 s, tu rebondis très haut à chaque pas : tung, tung, tung. | TungTung | `Pogo` |
| **Boing** | Un trampoline apparaît à tes pieds. Tout le monde peut sauter dessus. | Boing | `Ressort` |
| **Sad violin** | Une vague de larmes part de toi et emporte tout le monde à 20 m. | SadViolin | `Raz` |
| **Taco Bell** | Tu tapes le sol (BONG) : tous ceux qui sont debout à 25 m sautent en l'air. | TacoBell | `Seisme` |
| **Chipi chipi chapa chapa** | Tu te lances très loin devant toi en dansant. Pas avec la Couronne. | ChipiChapa | `Catapulte` |

### Les passives

| Mème | Ce que ça fait | Son (dossier MemesSons/) | Capacité d'origine |
|---|---|---|---|
| **Uno reverse card** | Les sorts qu'on te lance repartent sur celui qui les a lancés. | VineBoom | `Miroir` |
| **Instant karma** | Celui qui te pousse est repoussé à son tour. | VineBoom | `Riposte` |
| **We're so back** | Quand tu tombes dans les nuages, tu repars avec des ailes d'or, protégé 6 s. | Sheesh | `SecondSouffle` |
| **Light weight baby** | Avec la Couronne, tu voles aussi vite que les autres. | Wow | `Plume` |
| **Mewing** | Mâchoire de béton : les pièges de la tour ne te font plus tomber. | Sheesh | `TeteDure` |
| **Plot armor** | Le premier coup que tu prends dans la manche ne te fait rien. | Sheesh | `Armure` |
| **Chill guy** | Quand on te pousse, tu pars 2 fois moins loin. Tranquille. | Bruh | `Ancrage` |
| **Sigma push** | Ta poussée envoie 2 fois plus loin. | Bonk | `Poigne` |
| **Built different** | Les sorts qu'on te lance durent 2 fois moins longtemps. | Wow | `Increvable` |
| **Main character** | Une fois par manche, si tu tombes dans les nuages, tu reviens où tu étais. | Nooo | `AngeGardien` |
| **Kaboom** | Quand on te pousse, tu exploses : ceux autour de toi s'envolent. | VineBoom | `Kamikaze` |
| **Gamer rage** | Chaque coup que tu prends rend ta prochaine poussée plus forte. | Fahhh | `Rage` |
| **Hide and seek** | Reste sans bouger 1 seconde : tu deviens invisible. | Wow | `Ninja` |
| **Sigma grindset** | Chaque fois que tu frappes quelqu'un, tu cours plus vite 3 secondes. | Sigma | `Vampire` |
| **W** | Une fois sur trois, ta capacité revient tout de suite. | Sheesh | `Chanceux` |
| **Sneaky** | Quand tu prends la Couronne à quelqu'un, tu deviens invisible 2 s. | FanumTax | `Pickpocket` |
| **Speedrunner** | Tu cours un peu plus vite que les autres. | Tralalero | `Coureur` |
| **Gotta go fast** | Au début de chaque manche, tu cours très vite pendant 20 secondes. | Tralalero | `Sprinter` |
| **Double jump** | En l'air, appuie encore sur Espace : tu sautes une 2e fois. | Boing | `DoubleSaut` |
| **Bunny hop** | Tu sautes beaucoup plus haut. | Boing | `Kangourou` |
| **I believe I can fly** | Tu as toujours des ailes d'or : tu voles plus vite et plus loin. | ShootingStars | `Planeur` |
| **Ground pound** | Quand tu retombes de haut, tu fais une onde de choc. | VineBoom | `Rebond` |
| **No cooldown** | Ta capacité revient plus vite. | Sheesh | `Recharge` |
| **Sus detector** | Tu vois ce qui est caché : les mines, les invisibles, à travers la fumée. | AmongUs | `Flair` |
| **Spaghetti arms** | Ta poussée touche de plus loin : 5 m. | Boing | `BrasLongs` |
| **Home run** | Toutes les 4 poussées réussies, la suivante envoie 3 fois plus loin. | Bonk | `HomeRun` |

## Les sons : les VRAIS mèmes (v46.2)

> Martin : « il n'y a pas le son des vrais mèmes qui gueulent ; quand quelqu'un fait un 6 7, ça doit
> faire le vrai mème qui gueule, hyper fort ».

Les vrais enregistrements vivent dans **`MemesSons/`**, **à côté du jeu** : à la racine du projet dans
Unity (hors d'`Assets`, Unity ne les importe pas), à côté du `.exe` après un Build (recopiés tout
seuls, `Editor/SteamAppId.cs`). Un sous-dossier par mème, un ou plusieurs `.mp3` / `.ogg` / `.wav`.
Le jeu les charge au lancement (`Core/MemeSounds.cs`) et les joue **deux fois plus fort** qu'un son
normal ; le son d'un joueur part de lui (on l'entend autour). Dossier vide : le son fabriqué par le
code (la voix du haricot ou un bruit).

**Les télécharger d'un coup** : double-clic sur **`Tools/memes-sons.bat`** (il faut Python). Le
script cherche chaque mème sur myinstants.com et range le premier résultat dans son dossier. Un son ne
te plaît pas : supprime-le et pose le tien.

**Pas dans Git, pas dans le jeu vendu** : ces enregistrements appartiennent à leurs auteurs
(`.gitignore`). Entre amis, ça passe ; avant Steam, il faudra des sons libres.

| Dossier | Ce que le script cherche | Sans fichier |
|---|---|---|
| `SixSept` | 67 six seven | SixSept |
| `Fahhh` | fahhh | Fahhh |
| `VineBoom` | vine boom | Boum |
| `Bruh` | bruh | Bruh |
| `MetalPipe` | metal pipe falling | Bonk |
| `TacoBell` | taco bell bong | Boum |
| `Airhorn` | mlg air horn | Klaxon |
| `WindowsError` | windows xp error | Faux |
| `RobertWeide` | directed by robert b weide | Trombone |
| `SadViolin` | sad violin | Trombone |
| `Rickroll` | rick roll | Fanfare |
| `JohnCena` | john cena | Fanfare |
| `AmongUs` | among us role reveal | Suspense |
| `Sheesh` | sheesh | Sheesh |
| `Wow` | owen wilson wow | Wow |
| `Bonk` | bonk | Bonk |
| `Oof` | roblox oof | Oof |
| `Nooo` | noooo | Nooo |
| `Oiia` | oiia oiia cat | Couic |
| `ChipiChapa` | chipi chipi chapa chapa | Fanfare |
| `CoffinDance` | coffin dance | Fanfare |
| `TungTung` | tung tung tung sahur | Bonk |
| `Bombardiro` | bombardiro crocodilo | Klaxon |
| `Tralalero` | tralalero tralala | Sifflet |
| `Patapim` | brr brr patapim | Bruh |
| `EmotionalDamage` | emotional damage | Boum |
| `Huh` | huh cat | Faux |
| `Skibidi` | skibidi toilet | SixSept |
| `Sparta` | this is sparta | Boum |
| `Leeroy` | leeroy jenkins | Fahhh |
| `AwMan` | creeper aw man | Suspense |
| `Yeet` | yeet | Wow |
| `MissionFailed` | mission failed we'll get em next time | Trombone |
| `Gigachad` | gigachad | Sheesh |
| `Sigma` | sigma boy | Sheesh |
| `Rizz` | rizz sound effect | Wow |
| `FanumTax` | fanum tax | Caisse |
| `Ohio` | ohio meme | Suspense |
| `ShootingStars` | shooting stars meme | Wow |
| `Stonks` | stonks | Wow |
| `ThisIsFine` | this is fine | Suspense |
| `DunDun` | dun dun dunnn | Suspense |
| `Lag` | lag | Scratch |
| `Boing` | boing | Couic |
