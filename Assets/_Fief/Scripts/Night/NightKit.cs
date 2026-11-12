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
                if (n == "Vitrail" && Source("vitrail") != null) blocks[i].enabled = false;
                if (n == "Autel" && Source("autel") != null) blocks[i].enabled = false;
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
            Put("autel", 0f, 41.5f, 0f);
            // La fenetre gothique et son vitrail, derriere l'autel, et la lune qui passe au travers.
            if (Source("vitrail") != null)
            {
                Put("fenetre", 0f, 44f, 180f);
                Put("vitrail", 0f, 44f, 180f);
                MoonShaft();
            }
            return true;
        }

        /// <summary>
        /// Le cierge du kit, pose sous "parent". Renvoie son feu (eteint) : l'allumer, c'est SetActive(true).
        /// Renvoie null si le kit n'a pas de cierge.
        /// </summary>
        public static GameObject CandleStand(Transform parent)
        {
            GameObject src = Source("Prefabs/Cierge_Feu");
            if (src == null) return null;
            GameObject go = Object.Instantiate(src, parent);
            go.name = "Cierge (kit)";
            go.transform.localPosition = Vector3.zero;
            Transform fire = go.transform.Find("Feu");
            if (fire == null) { Object.Destroy(go); return null; }
            return fire.gameObject;
        }

        /// <summary>
        /// LE RAYON DE LUNE : un projecteur bleu qui entre par le vitrail et tombe devant l'autel,
        /// un faisceau qu'on voit dans l'air, et de la poussiere qui flotte dedans.
        ///
        /// Concept Unity : une lumiere "Spot" eclaire en cone, comme une lampe torche. Le faisceau,
        /// lui, n'est pas de la lumiere : ce sont quelques grands rectangles transparents qui
        /// s'additionnent a l'image (matiere "Additive"), plus clairs pres de la fenetre.
        /// </summary>
        static void MoonShaft()
        {
            Vector3 from = new Vector3(0f, 5.2f, 43.3f);
            Vector3 to = new Vector3(0f, 0f, 37.6f);
            Light spot = new GameObject("Rayon de lune").AddComponent<Light>();
            spot.transform.SetParent(root, false);
            spot.transform.position = from;
            spot.transform.rotation = Quaternion.LookRotation(to - from, Vector3.up);
            spot.type = LightType.Spot;
            spot.spotAngle = 48f;
            spot.range = 17f;
            spot.color = new Color(0.42f, 0.58f, 1f);
            spot.intensity = 2.8f;
            spot.shadows = LightShadows.Soft;

            Shader additive = Shader.Find("Legacy Shaders/Particles/Additive");
            if (additive != null)
            {
                // Les quatre coins de la fenetre, et la tache de lumiere au sol.
                Vector3 tl = new Vector3(-0.55f, 4.9f, 43.6f), tr = new Vector3(0.55f, 4.9f, 43.6f);
                Vector3 bl = new Vector3(-0.55f, 1.7f, 43.6f), br = new Vector3(0.55f, 1.7f, 43.6f);
                Vector3 fl = new Vector3(-1.7f, 0.02f, 35.2f), fr = new Vector3(1.7f, 0.02f, 35.2f);
                Vector3 nl = new Vector3(-1.2f, 0.02f, 40.4f), nr = new Vector3(1.2f, 0.02f, 40.4f);
                List<Vector3> v = new List<Vector3>();
                List<Color> c = new List<Color>();
                List<int> t = new List<int>();
                // Neuf nappes inclinees, du haut de la fenetre au bas. Chacune est claire en son milieu,
                // pres de la fenetre, et s'efface vers ses bords et vers le sol : aucun bord net.
                const int Sheets = 9, Cols = 6;
                for (int k = 0; k < Sheets; k++)
                {
                    float f = k / (float)(Sheets - 1);
                    Vector3 wl = Vector3.Lerp(tl, bl, f), wr = Vector3.Lerp(tr, br, f);
                    Vector3 gl = Vector3.Lerp(fl, nl, f), gr = Vector3.Lerp(fr, nr, f);
                    int b = v.Count;
                    for (int i = 0; i <= Cols; i++)
                    {
                        float u = i / (float)Cols;
                        float side = Mathf.Sin(u * Mathf.PI);          // 0 aux bords, 1 au milieu
                        v.Add(Vector3.Lerp(wl, wr, u));
                        c.Add(new Color(1f, 1f, 1f, 0.16f * side * side));
                        v.Add(Vector3.Lerp(gl, gr, u));
                        c.Add(new Color(1f, 1f, 1f, 0f));
                    }
                    for (int i = 0; i < Cols; i++)
                    {
                        int a = b + i * 2;
                        t.AddRange(new[] { a, a + 1, a + 3, a, a + 3, a + 2 });
                    }
                }
                Mesh mesh = new Mesh();
                mesh.name = "Faisceau";
                mesh.SetVertices(v);
                mesh.SetColors(c);
                mesh.SetTriangles(t, 0);
                mesh.RecalculateBounds();
                GameObject beam = new GameObject("Faisceau de lune");
                beam.transform.SetParent(root, false);
                beam.AddComponent<MeshFilter>().sharedMesh = mesh;
                Material m = new Material(additive);
                m.SetColor("_TintColor", new Color(0.07f, 0.12f, 0.28f, 0.5f));
                MeshRenderer mr = beam.AddComponent<MeshRenderer>();
                mr.sharedMaterial = m;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }

            Material mote = Resources.Load<Material>(Dir + "Materials/Braise");
            if (mote == null) return;
            GameObject dust = new GameObject("Poussière");
            dust.transform.SetParent(root, false);
            dust.transform.position = new Vector3(0f, 2.4f, 39.5f);
            ParticleSystem ps = dust.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = 9f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.09f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.03f);
            main.startColor = new Color(0.55f, 0.72f, 1f, 0.55f);
            main.maxParticles = 160;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = 14f;
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(2.4f, 4.2f, 6f);
            sh.randomDirectionAmount = 1f;
            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.12f;
            noise.frequency = 0.4f;
            dust.GetComponent<ParticleSystemRenderer>().sharedMaterial = mote;
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
            if (fire == null) return;
            // Ces feux-la projettent des ombres (les autres lumieres du kit n'en font pas : ca coute cher).
            Light glow = fire.GetComponentInChildren<Light>(true);
            if (glow != null) { glow.shadows = LightShadows.Soft; glow.shadowStrength = 0.85f; glow.shadowNearPlane = 0.25f; }
            if (!lit)
            {
                fire.gameObject.SetActive(false);
                DawnFires.Add(fire.gameObject);
            }
        }
    }
}
