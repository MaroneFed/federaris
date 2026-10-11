# Le mode mème (v46)

> Martin, 15/10 : « fais le mode mème : une grosse liste de mèmes, avec son ».
> Puis, 17/10 : « t'as rien capté : le mode mème, c'est comme le mode normal et le mode Dieu, sauf que
> là tu mets tous les mèmes d'internet, genre 67, fahhh ».

**Un MODE DE JEU, au salon** : la ligne **Mode** passe de **Normal** à **DIEU** à **MÈME** (contre les
bots comme en ligne, l'hôte décide ; `Match.MemeMode`, envoyé aux invités avec l'état du match,
protocole 13). En v45 ce n'était qu'un réglage : il a disparu des Réglages.

Les **règles du mode normal**, et les mèmes d'internet partout :

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
