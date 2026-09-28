# Les modèles 3D gratuits : où les trouver, où les ranger

*01/10/2026. Martin : « Les personnages, je n'ai jamais vu des personnages aussi horribles.
Si tu veux, je vais aller sur Internet chercher des designs 3D gratuits. »* — Oui. C'est
la **loi n°1** du projet : des assets de banques, zéro création 3D sur mesure.

Claude ne peut pas télécharger (le réseau de sa machine est fermé à ces sites). Toi, si.
Tout ce qui suit est **gratuit et libre (CC0)** : tu peux le mettre dans un jeu vendu
sur Steam, sans rien demander ni citer.

---

## 1. Le personnage (le plus important)

Le jeu sait déjà utiliser **un personnage animé** à la place du haricot, pour toi et pour
les bots (`Scripts/Player/ModelCharacter.cs`). Il faut **un seul fichier**.

**Où le trouver** (au choix) :

- **Quaternius — « Ultimate Animated Character Pack »** : `quaternius.com` ▸ Packs. Des
  personnages ronds, colorés, avec leurs animations (Idle, Walk, Run, Jump, Punch,
  Victory…). C'est le meilleur choix.
- **Kenney — « Animated Characters »** (1, 2 ou 3) : `kenney.nl/assets`. Plus simples,
  très propres.

**Ce que tu fais** :

1. Dans le pack, choisis UN personnage (par exemple un chevalier) au format **FBX**.
2. Renomme-le **`Personnage.fbx`**.
3. Glisse-le dans Unity, dans **`Assets/_Fief/Resources/Modeles/`** (crée le dossier
   *Modeles* s'il n'existe pas : clic droit dans le panneau Project ▸ Create ▸ Folder).
4. Clique sur le fichier dans Unity. Dans l'**Inspector**, onglet **Rig** ▸
   **Animation Type** : **Generic**. Clique **Apply**.
5. Lance le jeu. Dans la **Console** (Window ▸ General ▸ Console), tu dois lire :
   `[FIEF] Personnage trouve dans Resources/Modeles (N animations).`

Le code trouve les animations **par leur nom anglais** : `Idle`, `Walk`, `Run`, `Jump`,
`Punch` (ou `Attack`), `Dance` (ou `Victory`). S'il n'y a pas d'`Idle`, il garde le
haricot et le dit dans la Console.

**Concept Unity** : un dossier nommé **Resources** (n'importe où dans Assets) est le seul
endroit d'où le code peut charger un fichier *par son nom*, sans le glisser dans un champ
de l'Inspector. C'est comme ça que marchent aussi les polices et les musiques du jeu.

La couleur : le code teinte les matières qui s'appellent *Main*, *Body*, *Shirt*,
*Cloth*… à la couleur du joueur. Sinon, il teinte le plus gros morceau.

## 2. Le château

Le château est aujourd'hui fait de formes simples (cubes, cylindres, cônes). Pour un vrai
château de conte, le bon pack est :

- **Kenney — « Castle Kit »** : `kenney.nl/assets/castle-kit`. Murs, tours, portes,
  toits, drapeaux, modulaires.

**Ce que tu fais** : télécharge-le, et mets **tout le dossier des modèles** (les `.fbx`)
dans **`Assets/_Fief/Resources/Modeles/Chateau/`**. Puis dis-le à Claude : il écrira
l'assemblage (quel morceau à quelle place) en lisant les noms des fichiers. Il faut les
voir pour les poser juste — c'est pour ça qu'il ne le fait pas à l'aveugle.

## 3. Pour plus tard (même règle, même dossier)

- Kenney **« Fantasy Town Kit »** : puits, étals, charrettes pour la cour.
- Kenney **« Nature Kit »** : rochers et buissons pour les îlots.
- Quaternius **« Ultimate Monsters »** : des créatures (pour des gargouilles vivantes ?).

## 4. La musique

Mets tes morceaux (mp3, ogg ou wav) dans **`Assets/_Fief/Resources/Music/`**. Le NOM dit
quand ils jouent :

| Nom qui contient | Quand |
|---|---|
| `titre`, `menu` | l'écran-titre et les menus |
| `tension`, `combat` | quand quelqu'un porte la Couronne, qu'on se bat |
| `danse`, `victoire`, `fete` | **la danse du vainqueur** |
| `fin`, `podium` | la fin du match |
| autre chose | la montée, le calme |

**Pour la danse** : mets le **tempo** dans le nom, par exemple **`danse-128.mp3`** (128
temps par minute ; un site comme *tunebat* ou l'appli de ton choix te le donne). Sans
chiffre, le jeu compte 120. Le vainqueur fait un pas **par temps** : avec le bon tempo,
il danse vraiment sur ta musique. Coupe le silence du début du morceau.
