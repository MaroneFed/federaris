# v2-ideas — la boîte à idées hors-phase

> **La règle :** le tueur n°1 des projets de jeu, c'est l'ajout de features.
> Tout ce qui n'est pas dans la phase en cours **atterrit ici, jamais dans le code.**

**Phase en cours : PHASE 1 — boucle économique solo.**

---

## Reporté depuis la Phase 1 (dette assumée, à faire avant la Phase 3)

- **Refaire le HUD en UI Toolkit.** L'interface actuelle est en IMGUI (`OnGUI`) :
  zéro asset, zéro configuration, parfait pour valider la boucle — mais ce n'est pas
  ce qu'on shippe sur Steam. À reprendre quand la Porte 1 est franchie.
- **Passer en URP.** Le projet tourne en pipeline Built-in. Tous les matériaux sont
  créés par code dans `MaterialFactory`, qui essaie déjà le shader URP en premier :
  le basculement ne cassera rien. À faire quand on touchera au look.
- **Remplacer les primitives par des assets Kenney/Synty.** Trois fichiers à modifier,
  et rien d'autre : `NodeFactory.cs`, `BuildingFactory.cs` et `Scenery.cs`.
- **Sons.** Ils existent maintenant, mais ils sont *synthétisés par le code*
  (`Scripts/Core/Sfx.cs`) : aucun fichier audio. C'est suffisant pour sentir la boucle,
  pas pour shipper. À remplacer par de vrais sons — l'appel (`Sfx.Harvest(...)`) ne changera pas.
- **Les statiques de `Game.cs`.** Elles tombent en multijoueur (voir ARCHITECTURE.md).

## Phase 2 — le conflit (ne pas commencer sans validation de la Porte 1)

- Combat mêlée + arc, PV, drop à la mort.
- Vol du coffre d'un rival.
- Destruction à règles v1 : bois → hache/feu, pierre → bélier/trébuchet/sape,
  herse → clé volée, **porte dérobée → uniquement par quelqu'un de l'intérieur**.
- PNJ serviteurs : salaire, jauge de loyauté.
- **v1 du sabotage** — LE différenciateur. Payer un serviteur mécontent pour qu'il
  ouvre la porte la nuit. À protéger absolument, c'est ce que personne d'autre ne fait.
- Cycle jour/nuit.

## Phase 3 — multijoueur

- Netcode for GameObjects, LAN d'abord, puis Steam P2P (Facepunch.Steamworks).
- Lobby héberger/rejoindre, 4 à 6 joueurs.
- Autorité serveur sur l'économie (l'architecture actuelle est déjà faite pour, voir ARCHITECTURE.md).
- Gestion des déconnexions.

## Phase 4 — la Saison complète

- Timer de Saison, score de Prestige, conditions de victoire multiples, écran de fin.
- Anti-snowball : prime sur le leader.
- 2-3 événements de monde.
- Équilibrage.

---

## Idées en vrac (rien de décidé, rien de promis)

*(Ajoute ici tout ce qui te passe par la tête pendant le dev. C'est fait pour ça.)*

- Un serviteur soudoyé pourrait rester loyal et **prévenir son seigneur** → contre-jeu
  du sabotage, et une histoire à raconter après la partie.
- Rumeur publique au marché : « quelqu'un a payé cher pour des renseignements ».
- Les prix du marché visibles de loin depuis une Tour de guet.
