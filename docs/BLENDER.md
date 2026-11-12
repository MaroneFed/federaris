# Le kit du château, fait dans Blender

## Où on en est (12/11)

**Le kit existe et il est dans le jeu.** Claude Desktop l'a fabriqué sur le PC de Martin **sans le connecteur**
décrit plus bas : il lance Blender tout seul, sans fenêtre, avec un script. Le connecteur ne sert que si tu veux
voir les pièces se construire sous tes yeux.

- **Les pièces** (`Assets/_Fief/Resources/Modeles/Kit/`) : `mur_droit`, `mur_arche`, `dalle_sol`, `voute_couloir`,
  `pilier`, `fenetre`, `autel` (pierre), `torche_murale`, `brasero`, `cierge` (fer), `vitrail` (verre), et
  `penitent` (la statue du monstre : sa propre matière **Statue**, ses yeux rouges dans `Statue_Emission`). Moins de
  1000 triangles chacune, 1 unité = 1 mètre, pivot au sol, grille de 4 m.
- **Les textures** (`Kit/Textures/`) : une seule matière **Pierre** pour toute la pierre, une matière **Fer**, une
  matière **Vitrail**. Pour chacune : la couleur (`_Albedo`), le relief (`_Normal`), un masque (`_Mask` : rouge =
  métal, vert = ombre des creux, alpha = lisse), et ce qui brille tout seul (`_Emission` : braises, verre).
  Elles sont **cuites** dans Blender : usure, fissures, joints sales, mousse, rouille sont peints par le calcul,
  pas à la main. La flamme est une planche de 64 images (`Flamme_Flipbook`).
- **Les refaire** : `Tools/kit/kit_fief.py` (les pièces et la cuisson), `Tools/kit/flamme.py`, `Tools/kit/vitrail.py`.
  La commande est en tête de chaque fichier. Puis, dans Unity : menu **FIEF ▸ Kit - refaire les matieres**.
- **Les poser dans le niveau** : `Assets/_Fief/Scripts/Night/NightKit.cs`. Une ligne par pièce (`Wall`, `Floor`,
  `Arch`, `Put`...), en mètres, sur la grille.
- **Voir le résultat sans ouvrir Unity** : `Tools/photos-nuit.ps1` lance La Nuit et range des photos dans
  `Logs/Captures/`. C'est comme ça que Claude Desktop voit le jeu. Unity doit être fermé pendant ce temps.

**Ce que le kit n'est pas** : du photoréalisme. Moins de 1500 triangles par pièce, c'est du low-poly ; le détail
vient des textures et de la lumière. Les murs voisins portent tous les mêmes pierres (une seule texture).

# Brancher Claude sur Blender (sur le PC de Martin)

But : que Claude **fabrique les modèles 3D dans Blender sous tes yeux** (murs, portes, torches,
statues, monstres), puis qu'on les exporte dans le projet Unity.

Ça se passe dans **l'appli Claude de bureau** (Claude Desktop) sur ton PC — pas dans cette session,
qui tourne sur un serveur et ne voit pas ton écran. Le connecteur s'appelle « MCP for Blender »
(source : github.com/ahujasid/blender-mcp, lue le 11/11 ; Claude ne l'a pas installé lui-même).

## Les étapes

1. **Installer Blender** (gratuit) : blender.org ▸ Download. Version 3.0 ou plus.
2. **Installer `uv`** (le petit outil qui lance le connecteur). Ouvre PowerShell et tape :
   `winget install --id=astral-sh.uv -e` — puis ferme et rouvre PowerShell.
3. **Dire à Claude Desktop que le connecteur existe** : Claude Desktop ▸ Settings ▸ Developer ▸
   Edit Config. Dans le fichier `claude_desktop_config.json`, mets :

   ```json
   {
     "mcpServers": {
       "blender": { "command": "cmd", "args": ["/c", "uvx", "mcp-for-blender"] }
     }
   }
   ```

   Puis **quitte vraiment** Claude Desktop (clic droit sur son icône près de l'horloge ▸ Quit) et rouvre-le.
4. **Installer le module dans Blender** : dans PowerShell, `uvx mcp-for-blender install-addon`.
   Ouvre Blender ▸ Edit ▸ Preferences ▸ Add-ons ▸ cherche « MCP for Blender » ▸ coche la case.
5. **Connecter** : dans la vue 3D de Blender, touche **N** (un panneau s'ouvre à droite) ▸ onglet
   **MCP for Blender** ▸ coche **Poly Haven** (textures et modèles gratuits, libres de droits) ▸
   **Connect to Claude**.
6. Dans Claude Desktop, tu dois voir les outils Blender. Demande-lui : « regarde ma scène Blender ».

## À savoir

- Le connecteur laisse Claude **exécuter du code dans Blender** : enregistre ton fichier avant.
- La première tentative échoue parfois ; la deuxième marche en général.
- Les modèles Poly Haven sont libres (CC0). Ceux de Poly Pizza demandent souvent de **citer l'auteur**.
- Demande une chose à la fois (« un mur de 4 m », puis « une porte ») : les grosses demandes plantent.

## Le premier prompt à coller dans Claude Desktop

```
Tu es branché sur Blender. On fabrique le kit de décor d'un jeu Unity : un château médiéval la nuit,
sombre, style Elden Ring, low-poly propre (pas de haricots, pas de style cartoon).
Règles : 1 unité = 1 mètre ; le pivot de chaque pièce au sol, au coin ou au centre ; les pièces
s'emboîtent sur une grille de 4 m ; moins de 1500 triangles par pièce ; une seule matière pierre.
Fabrique d'abord, une par une, en me montrant chaque pièce avant de passer à la suivante :
1. un mur droit 4 x 4 m en pierres de taille, 2. un mur avec une arche de porte, 3. une dalle de sol
4 x 4 m, 4. une voûte de couloir, 5. un pilier, 6. une torche murale en fer, 7. un brasero sur pied.
Exporte chaque pièce en FBX dans un dossier « kit-fief » sur mon Bureau.
```

Ensuite : glisse le dossier `kit-fief` dans Unity, dans `Assets/_Fief/Resources/Modeles/Kit/`, et
pousse sur GitHub. C'est avec ces pièces que le code assemblera le château.
