# Le mode mème (v45)

> Martin, 15/10 : « fais le mode mème : une grosse liste de mèmes, avec son ».

**Réglages ▸ Mode mème : oui.** Rien ne change aux règles. Sur les grands moments qui **te**
concernent, une **légende de mème** claque au milieu de l'écran (lettres blanches cernées de noir,
qui tremblent, un trait de couleur dessous), avec **son bruit de mème**, un **coup de caméra**, et
parfois l'écran qui **« frit »** (un éclair saturé). Un mème à la fois, jamais deux du même genre en
6 s. Code : `UI/Memes.cs` (la liste), `Core/SfxMemes.cs` (les sons).

**33 moments, 137 légendes** (une au hasard à chaque fois) :

| # | Quand | Son | Légendes |
|---|---|---|---|
| 1 | quelqu'un te fait tomber dans les nuages | Trombone | ET C'EST LE DRAME · BON BAH... · C'ÉTAIT PAS PRÉVU · ADIEU MONDE CRUEL · JE REVIENS... PEUT-ÊTRE · LES NUAGES M'APPELLENT · SKILL ISSUE · OUPS. |
| 2 | tu tombes tout seul | Sifflet | TOUT SEUL ?? · PERSONNE NE T'A TOUCHÉ · LA GRAVITÉ GAGNE ENCORE · GÉNIE. · C'ÉTAIT VOULU (NON) |
| 3 | tu fais tomber quelqu'un (KO) | Klaxon + écran frit | AU REVOIR · BYE BYE · CIAO · DÉGAGÉ · DIRECTION LES NUAGES · BON VOYAGE · IL EST PARTI OÙ ? |
| 4 | deux KO en 12 s | Klaxon + écran frit | DOUBLÉ !! · ET DE DEUX · ENCORE UN · C'EST GRATUIT |
| 5 | trois KO en 12 s | Fanfare + écran frit | MAIS ARRÊTE · TRIPLÉ !!! · IL EST INARRÊTABLE · QUELQU'UN L'ARRÊTE ? · C'EST UN MASSACRE |
| 6 | tu prends la Couronne | Caisse | ON EST RICHE · LE ROI, C'EST MOI · À GENOUX · MON PRÉCIEUX · TROP FACILE · QUI VEUT LA COURONNE ? |
| 7 | on te prend la Couronne | Scratch | QUOI ?! · NON NON NON · C'ÉTAIT À MOI · PERDUE... · REVIENS !! |
| 8 | tu voles la Couronne | Caisse + écran frit | MERCI ! · C'EST À MOI MAINTENANT · VOL À L'ÉTALAGE · ON PARTAGE ? NON. · LIVRAISON EXPRESS |
| 9 | Couronne volée dans le cercle du Monument | Scratch + écran frit | À UNE SECONDE PRÈS · LE VOL DU SIÈCLE · C'EST CRUEL · IMPOSSIBLE |
| 10 | ton sacre commence | Suspense | PLUS QUE TROIS SECONDES · NE BOUGE PLUS · RESPIRE... · C'EST MAINTENANT |
| 11 | ton sacre est arrêté | Scratch | SI PRÈS... · C'ÉTAIT LÀ · ÇA FAIT MAL · NOOON |
| 12 | sacre dans les 15 dernières secondes | Klaxon + écran frit | DERNIÈRE SECONDE !! · AU BUZZER · LE TIMING PARFAIT · ON N'Y CROYAIT PLUS |
| 13 | tu gagnes la manche | Fanfare + écran frit | GG · TROP FORT · CHAMPION · LÉGENDAIRE · ÇA, C'EST FAIT · ON NE M'ARRÊTE PAS |
| 14 | un autre gagne la manche | Trombone | DOMMAGE · LA PROCHAINE, PEUT-ÊTRE · C'EST LA VIE · ON A ESSAYÉ |
| 15 | tu gagnes le match | Fanfare + écran frit | VICTOIRE ROYALE · LE ROI DES NUAGES · GG À TOUS (SAUF EUX) · INVAINCU |
| 16 | tu perds le match | Trombone | BIEN ESSAYÉ · REVANCHE ? · ON DIRA QUE C'ÉTAIT LE LAG |
| 17 | on te pousse fort | Bonk | BONK · AÏE · OUCH · ÇA PIQUE · POURQUOI MOI ? |
| 18 | tu pousses quelqu'un | Bonk | BONK · DÉGAGE · POUSSE-TOI · PARDON (NON) |
| 19 | tu pousses celui qui venait de te pousser | Boum + écran frit | LA VENGEANCE · RETOUR DE BÂTON · TU L'AS CHERCHÉ |
| 20 | tu vires quelqu'un du sommet | Boum + écran frit | VIRÉ DU SOMMET · REDESCENDS · PAS AUJOURD'HUI |
| 21 | un obstacle te frappe (hors de la tour) | Bonk | LE PENDULE A GAGNÉ · BIEN FAIT · IL FALLAIT SAUTER · REGARDE DEVANT TOI |
| 22 | un obstacle t'éjecte de la tour | Sifflet | RETOUR AU DÉBUT · TOUT À REFAIRE · NOOOON · C'EST PAS GRAVE (SI) |
| 23 | on t'enferme (Prison, temps arrêté) | Suspense | EN TAULE · PRISON · PAS DE SORTIE · DIX SECONDES DE RÉFLEXION |
| 24 | on te rétrécit | Couic | PETIT HARICOT · TOUT MIGNON · CHÉRIE, J'AI RÉTRÉCI LE HARICOT · COUIC |
| 25 | tête à l'envers | Faux | GAUCHE ? DROITE ? · TOUT EST À L'ENVERS · JE COMPRENDS RIEN |
| 26 | tu danses (B) ou on te fait danser | Fanfare | C'EST L'HEURE · LA PISTE EST À MOI · ON DANSE ! |
| 27 | ta capacité est refusée | Faux | NOPE · NON. · PAS ICI · REFUSÉ |
| 28 | ton Miroir renvoie un sort | Boum + écran frit | RETOUR À L'EXPÉDITEUR · MIROIR, MON BEAU MIROIR · ÇA TE REVIENT |
| 29 | ton Home run part | Bonk + écran frit | HOME RUN !! · DANS LES TRIBUNES · IL VOLE ! |
| 30 | la Couronne change 5 fois de mains en 15 s | Scratch | PATATE CHAUDE · À QUI LE TOUR ? · PERSONNE NE LA GARDE |
| 31 | les quatre anneaux avec la Couronne | Fanfare | PILOTE DE LIGNE · QUATRE SUR QUATRE · TROP PROPRE |
| 32 | palier de puissance (×1,5, ×2, ×2,5) | Boum + écran frit | ÇA DEVIENT SÉRIEUX · PLUS FORT ! · ATTENTION... |
| 33 | le « fight » du départ | Klaxon | C'EST PARTI · QUE LE MEILLEUR GAGNE · BONNE CHANCE (VOUS EN AUREZ BESOIN) |

## Les sons

Onze sons, **fabriqués par le code** (aucun fichier à télécharger) : Klaxon, Trombone, Scratch,
Bonk, Boum, Fanfare, Suspense, Couic, Faux, Sifflet, Caisse. Chacun a son dossier dans
`Assets/_Fief/Resources/Sons/Memes/` : **un fichier posé dans le dossier remplace le son du code**
(au hasard s'il y en a plusieurs). Mode d'emploi : `Sons/Memes/LISEZ-MOI.txt`.

**Pourquoi Claude n'a pas téléchargé 500 mèmes du web** : les images et les sons des mèmes connus
(vine boom, bruh, klaxon MLG, les images avec texte…) appartiennent à leurs auteurs. Dans un jeu
**vendu sur Steam**, c'est un risque de retrait de la page. Les légendes et les sons du mode mème
sont donc à nous. Pour jouer entre amis, Martin peut déposer ses sons dans les dossiers ; avant la
sortie, il faudra des sons libres (CC0 : freesound.org avec le filtre « Creative Commons 0 », Kenney).
