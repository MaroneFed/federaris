using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// L'HABILLAGE DE LA NUIT (12/11). NightBootstrap construit le niveau en blocs (des cubes) :
    /// ce sont eux qui arretent le joueur et qui cachent le Penitent. Ici, on CACHE ces blocs et
    /// on pose par-dessus les vraies pieces du kit fait dans Blender : murs en pierres de taille,
    /// arches, dalles, voutes, piliers, torches, braseros.
    ///
    /// Les blocs restent la, invisibles : la physique et les regles ne changent pas d'un poil.
    /// Si le kit manque (dossier Resources/Modeles/Kit absent), on ne touche a rien : le niveau
    /// reste en blocs, comme avant.
    ///
    /// Concept Unity : Resources.Load va chercher un fichier range dans un dossier "Resources",
    /// par son nom, pendant le jeu. Instantiate en pose une copie dans la scene.
    /// Les pieces font 4 m et se posent sur une grille de 4 m, le pivot au sol.
    /// </summary>
    public static class NightKit
    {
        const string Dir = "Modeles/Kit/";
        static readonly Dictionary<string, GameObject> Sources = new Dictionary<string, GameObject>();
        static readonly List<GameObject> DawnFires = new List<GameObject>();
        static Transform root;

        static GameObject Source(string name)
        {
            GameObject src;
            if (Sources.TryGetValue(name, out src) && src != null) return src;
            src = Resources.Load<GameObject>(Dir + name);
            Sources[name] = src;
            return src;
        }

        /// <summary>Habille le niveau. Renvoie false si le kit n'est pas dans le projet.</summary>
        public static bool Dress(Transform world)
        {
            DawnFires.Clear();
            Sources.Clear();
            if (Source("mur_droit") == null || Source("dalle_sol") == null) return false;

            // Les blocs deviennent invisibles (ils gardent leur collider).
            MeshRenderer[] blocks = world.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < blocks.Length; i++)
            {
                string n = blocks[i].gameObject.name;
                if (n == "Mur" || n == "Sol" || n == "Plafond" || n == "Pilier") blocks[i].enabled = false;
            }

            root = new GameObject("Kit (le décor)").transform;
            root.SetParent(world, false);

            // --- LE VESTIBULE (x -4..4, z 0..8, 4 m de haut)
            for (int x = -2; x <= 2; x += 4)
                for (int z = 2; z <= 6; z += 4) { Floor(x, z); Ceiling(x, z, 4f); }
            Wall(-2f, 0f, 0f); Wall(2f, 0f, 0f);
            Wall(-4f, 2f, 90f); Wall(-4f, 6f, 90f);
            Wall(4f, 2f, 90f); Wall(4f, 6f, 90f);
            Wall(-4f, 8f, 0f); Wall(4f, 8f, 0f);          // ils depassent dehors de 2 m : personne ne le voit
            Arch(0f, 8f, 0f);
            Fire("Torche_Murale_Feu", -3f, 8f, 180f, true);
            Fire("Torche_Murale_Feu", 3f, 8f, 180f, true);

            // --- LE COULOIR (x -2..2, z 8..32) sous sa voute, la niche du Penitent a droite (z 20..24)
            for (int z = 10; z <= 30; z += 4)
            {
                Floor(0f, z);
                Put("voute_couloir", 0f, z, 0f);
                Wall(-2f, z, 90f);
                if (z == 22) Arch(2f, z, 90f); else Wall(2f, z, 90f);
            }
            for (int i = 0; i < 5; i++)
                for (int s = -1; s <= 1; s += 2) Put("pilier", s * 1.55f, 12f + i * 4f, 0f);
            Put("torche_murale", -2f, 14f, 90f);          // eteintes : seule ta torche eclaire
            Put("torche_murale", 2f, 18f, -90f);
            Put("torche_murale", -2f, 26f, 90f);
            Put("torche_murale", 2f, 30f, -90f);

            // La niche.
            Floor(4f, 22f); Ceiling(4f, 22f, 4f);
            Wall(4f, 20f, 0f); Wall(4f, 24f, 0f); Wall(6f, 22f, 90f);

            // --- LA CHAPELLE (x -6..6, z 32..44, 8 m sous plafond)
            for (int x = -4; x <= 4; x += 4)
                for (int z = 34; z <= 42; z += 4) { Floor(x, z); Ceiling(x, z, 8f); }
            for (int level = 0; level < 2; level++)
            {
                float y = level * 4f;
                Wall(-4f, 32f, 0f, y); Wall(4f, 32f, 0f, y);
                if (level == 0) Arch(0f, 32f, 0f); else Wall(0f, 32f, 0f, y);
                for (int x = -4; x <= 4; x += 4) Wall(x, 44f, 0f, y);
                for (int z = 34; z <= 42; z += 4) { Wall(-6f, z, 90f, y); Wall(6f, z, 90f, y); }
            }
            Put("torche_murale", -6f, 38f, 90f);
            Put("torche_murale", 6f, 38f, -90f);
            // Deux braseros de part et d'autre de l'autel : ils s'embrasent a l'aube.
            Fire("Brasero_Feu", -2.6f, 41.2f, 0f, false);
            Fire("Brasero_Feu", 2.6f, 41.2f, 0f, false);
            return true;
        }

        /// <summary>L'aube : les braseros de la chapelle s'allument.</summary>
        public static void LightDawnFires()
        {
            for (int i = 0; i < DawnFires.Count; i++) if (DawnFires[i] != null) DawnFires[i].SetActive(true);
        }

        // ------------------------------------------------------------------ les pieces

        static GameObject Put(string piece, float x, float z, float yaw, float y = 0f, bool upsideDown = false)
        {
            GameObject src = Source(piece);
            if (src == null) return null;
            GameObject go = Object.Instantiate(src, root);
            go.name = piece;
            go.transform.position = new Vector3(x, y, z);
            go.transform.rotation = Quaternion.Euler(upsideDown ? 180f : 0f, yaw, 0f) * src.transform.rotation;
            return go;
        }

        static void Floor(float x, float z) { Put("dalle_sol", x, z, 0f); }

        /// <summary>Un plafond : la meme dalle, retournee, a la hauteur voulue.</summary>
        static void Ceiling(float x, float z, float height) { Put("dalle_sol", x, z, 0f, height, true); }

        static void Wall(float x, float z, float yaw, float y = 0f) { Put("mur_droit", x, z, yaw, y); }

        /// <summary>Un mur a arche : la porte fait 2 m de large, donc deux blocs invisibles pour ses montants.</summary>
        static void Arch(float x, float z, float yaw)
        {
            GameObject arch = Put("mur_arche", x, z, yaw);
            if (arch == null) return;
            for (int s = -1; s <= 1; s += 2)
            {
                GameObject jamb = new GameObject("Montant");
                jamb.transform.SetParent(arch.transform, false);
                jamb.transform.localPosition = new Vector3(s * 1.5f, 2f, 0f);
                BoxCollider box = jamb.AddComponent<BoxCollider>();
                box.size = new Vector3(1f, 4f, 0.5f);
            }
        }

        /// <summary>Une torche ou un brasero avec son feu ; "lit" a false : le feu attend l'aube.</summary>
        static void Fire(string prefab, float x, float z, float yaw, bool lit)
        {
            GameObject src = Source("Prefabs/" + prefab);
            if (src == null) return;
            GameObject go = Object.Instantiate(src, root);
            go.name = prefab;
            go.transform.position = new Vector3(x, 0f, z);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * src.transform.rotation;
            Transform fire = go.transform.Find("Feu");
            if (fire != null && !lit)
            {
                fire.gameObject.SetActive(false);
                DawnFires.Add(fire.gameObject);
            }
        }
    }
}
