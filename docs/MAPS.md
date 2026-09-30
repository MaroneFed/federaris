# Les maps de FIEF

*03/10/2026. Martin : « À l'avenir, FIEF aura plusieurs maps, donc le style entier du menu
va changer en fonction de la map qu'on choisit. Tu me diras combien il faut en faire. Mais
on finit la 1, qui s'intitule **Castle**, et après on passe à une autre. »*

## La map 1 : **Castle**

L'île flottante, le château de conte, la tour de la Couronne à quatre rampes, les six
îlots, les anneaux de vent. C'est **la seule map de la Phase 1**, et on la finit avant de
commencer la suivante (règle anti-dérive). Son nom est dans le code : `Game.MapName`, et
dans la ligne de version de l'écran-titre.

## Combien de maps ?

**Ma recommandation : 3 maps pour l'Early Access, 5 pour la version 1.0.** Pas plus.

Pourquoi pas plus :
- Chez nous, une map n'est pas un mini-jeu de 2 minutes (Fall Guys en a des dizaines) :
  c'est une **arène complète pour 30 minutes** (une tour, des Monuments, du vol, des
  obstacles), comme une carte de *Overwatch* ou un stage de *Smash*. Les joueurs
  apprennent une map par cœur, c'est ce qui crée les clips (« LE pendule du 3e virage »).
- Vous êtes deux (Martin et son frère), débutants : **une map de qualité ≈ 3 à 4 semaines**
  (décor, obstacles propres à la map, lumière, musique, tests). 3 maps, c'est déjà
  3 mois — le reste du temps va au multijoueur (Phase 3) et à la vitrine (Phase 5).
- Au lancement, trois ambiances suffisent pour que chaque match ait l'air différent,
  surtout avec des obstacles tirés au hasard à chaque manche.

Pourquoi pas moins :
- Une seule map, les joueurs Steam s'en lassent en une soirée ; **trois, c'est le minimum**
  pour que le choix au salon ait du sens (et pour les vidéos : trois décors, trois clips
  différents).

## Ce qui change d'une map à l'autre (et ce qui ne change jamais)

**Ne change jamais** (c'est FIEF) : la Couronne au sommet d'une tour, le vol plané, les
Monuments sur des îlots, les poussées, les capacités, les bots, les haricots.

**Change avec la map** :
- **le décor** : le relief, la tour (sa forme, ses rampes), les îlots, la lumière, le ciel ;
- **1 ou 2 obstacles propres à la map** (en plus des pendules, béliers, barres…) ;
- **le style du MENU** (ta demande) : la couleur des boutons et des pastilles, le fond de
  l'écran-titre (le plan d'ensemble de *cette* map), la musique du menu et des manches,
  l'icône de la map ;
- **la palette des obstacles** (Castle : rouge, crème, ardoise, or mat).

Comment ce sera fait (plus tard, pas maintenant) : **une fiche par map** — en Unity, un
*ScriptableObject* (un fichier de réglages qu'on remplit dans l'Inspector, sans code) avec
le nom, les couleurs du menu, les musiques, le ciel, les obstacles propres. Le salon lit la
fiche de la map choisie, et tout le menu prend ses couleurs. Aujourd'hui, les couleurs du
menu sont dans `UI/UiStyle.cs` et `Core/Palette.cs` : c'est là qu'on les sortira dans la
fiche.

## Les idées de maps (rien de décidé)

1. **Castle** — l'île flottante et le château de conte. *En cours.*
2. **Volcan** — une île de roche noire au-dessus d'une mer de lave ; la tour est un
   volcan ; les courants d'air sont des geysers de chaleur ; obstacle propre : les
   jets de lave. Menu : noir et orange.
3. **Glacier** — des pics de glace au-dessus d'une mer de brume bleue ; on glisse sur
   les rampes ; obstacle propre : les stalactites qui tombent. Menu : blanc et bleu glace.
4. **Jungle céleste** — des arbres géants sur des îlots, des lianes entre eux ; obstacle
   propre : les plantes carnivores. Menu : vert et or.
5. **Cité engloutie** — une ville sous une bulle, au fond de la mer ; les anneaux de
   vent deviennent des courants d'eau. Menu : turquoise et nacre.

À Martin et à son frère de choisir les deux suivantes quand Castle sera finie.
