# La course d'élytres (v48 — prototype)

> Martin, 28/10 : « Le jeu, il est invendable. J'adore les élytres, depuis que je vole comme dans
> Minecraft. Je veux que le jeu se transforme en course, des courses d'élytres. On garde les mêmes
> persos, on peut se pousser dans les trucs, c'est incroyable. » Puis : « Améliore le full et fais
> des dingueries. »

## L'avis de Claude

**C'est la bonne direction.** Le vol est ce que FIEF fait de mieux, et une course se comprend en une
seconde : une vidéo de 10 s suffit (des haricots à 250 km/h qui se poussent entre des anneaux).
« Mario Kart dans le ciel avec des élytres » se vend bien mieux que « prends une Couronne en haut
d'une tour et plane jusqu'à un Monument ». Le jeu de la Couronne n'est pas perdu : il reste au salon
(Normal, DIEU), et ses morceaux (capacités, bots, île, réseau) serviront à la course.

## Ce que fait le prototype

- **Salon** : Mode **COURSE** (par défaut) / Normal / DIEU. Une manche = une course.
- **Le circuit** : 12 portes autour de Castle (au ras des falaises, juste au-dessus des nuages,
  au-dessus des murailles, slalom autour de la tour), 3 tours, 3 portes d'or. Une porte qui
  touche le décor est remontée toute seule.
- **Le départ** : une plateforme dans le ciel, 70 m avant la ligne. 3, 2, 1 : tout le monde plonge.
- **Le vol** (`Wings.FlyRace`) : croisière 22 m/s, 70 m/s au plus en piqué ; l'air freine peu
  au-dessus de la croisière ; Q/D glissent, S freine, Espace replie/rouvre les élytres.
- **L'élan** : +7 m/s par porte, +14 par porte d'or (avec une traînée d'or) ; **l'aspiration**
  (+3 m/s par seconde à moins de 25 m derrière quelqu'un, dans son axe).
- **Se pousser** : clic droit en plein vol, 6 m de portée, une poussée par seconde ; le poussé part
  en vrille, ailes fermées, et perd sa course.
- **Chute** : dans les nuages, on repart de la dernière porte, déjà en vol (« RETOUR EN PISTE »).
  Posé quelque part : Espace, un grand bond, les élytres s'ouvrent.
- **Fin** : le premier qui boucle ses 3 tours gagne la manche (la danse du vainqueur comme avant).
- **HUD** : ta place (1er / 6), le tour, la porte, ta vitesse en km/h, le repère de la prochaine
  porte (collé au bord quand elle est hors de vue), les bandeaux PLONGEZ !, DERNIER TOUR !, VICTOIRE !.
- **Bots** : ils visent leur prochaine porte (en anticipant la suivante), piquent pour reprendre de
  l'élan quand ils sont trop lents, et poussent en vol (les coriaces souvent, les faciles jamais).
- **En ligne** : le mode part avec l'état du match (protocole 15). Pas encore essayé à deux.

Code : `World/Race.cs` (circuit, portes, classement, HUD, bots), `Wings.FlyRace`,
`PlayerController.StartFlight`, `Rival.StartFlight`.

## Ce qui n'y est pas encore (les prochaines étapes proposées)

1. **Les capacités en course** (comme les objets de Mario Kart) : fusée, mur de vent, bouclier,
   missile… — une table à part, gagnée aux portes plutôt qu'au choix des cartes.
2. **Un vrai circuit dessiné** (au lieu de 12 points autour de Castle) : tunnels, arches, passages
   étroits entre des rochers — c'est là que le frère de Martin peut construire.
3. **Le classement de fin de course** (1er, 2e, 3e… avec des points, comme Mario Kart).
4. **Le contre-la-montre** et les fantômes (rejouer son meilleur tour).
5. Les sensations : lignes de vitesse, vent qui siffle plus fort, caméra qui tremble à 200 km/h.
