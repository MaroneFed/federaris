using UnityEngine;

namespace Fief
{
    /// <summary>
    /// L'ARRIVEE A CHEVAL (21/11). Martin : "une video en debut de jeu ou on arrive en cheval et tout est delabre
    /// et on comprend pas".
    ///
    /// Devant le chateau, au sud, un chemin de dalles brisees, des ruines, des arbres morts, un cimetiere ouvert, et
    /// la facade : des murailles de douze metres, deux tours, une grande porte, deux braseros allumes (par qui ?).
    /// Le menu du debut se dessine par-dessus ce paysage. "Entrer dans la nuit" : on part au pas, puis au galop.
    /// Un eclair : une silhouette traverse le chemin, loin devant. Au suivant, plus rien. On arrive, le cheval
    /// renacle, la porte s'ouvre toute seule, on met pied a terre, le noir. Puis l'ouverture d'avant (l'homme).
    ///
    /// Concept Unity : une cinematique n'est pas une video. C'est la vraie camera, deplacee par le code selon
    /// l'heure : on voit le vrai jeu, et on peut la passer (Entree).
    /// </summary>
    public static class NightIntro
    {
        public static bool Playing;
        public static bool Finished;
        static float t, black, nextHoof, menuBolt;
        static int hoofBeat;
        static NightPlayer rider;
        static GameObject horse, shade;
        static Transform leaf;
        static Material shadow;
        static GUIStyle titleStyle;
        static System.Random rng = new System.Random(5);
        static float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }

        static readonly Vector3 RoadStart = new Vector3(26f, 0.05f, -84f);
        const float GateZ = 3.4f, StopZ = -2.4f;
        static readonly Color FogOut = new Color(0.028f, 0.034f, 0.05f);

        // ------------------------------------------------------------------ le paysage

        static GameObject Put(string piece, float x, float z, float yaw, float y, Vector3 tilt, float scale)
        {
            GameObject go = NightKit.Place(piece, x, z, yaw, y, false);
            if (go == null) return null;
            go.transform.rotation = Quaternion.Euler(tilt.x, yaw, tilt.z);
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        public static void Build(Transform world)
        {
            rng = new System.Random(5);

            // Le sol : de la boue, tout le long de l'approche.
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Boue";
            ground.transform.SetParent(world, false);
            ground.transform.position = new Vector3(26f, -0.03f, -40f);
            ground.transform.localScale = new Vector3(14f, 1f, 8.6f);
            Material mud = new Material(Shader.Find("Standard"));
            mud.color = new Color(0.05f, 0.045f, 0.04f);
            mud.SetFloat("_Glossiness", 0.12f);
            ground.GetComponent<Renderer>().sharedMaterial = mud;

            // Le chemin : des dalles brisees, de travers, il en manque.
            for (float z = -84f; z <= 0f; z += 4f)
            {
                if (rng.NextDouble() < 0.22) continue;
                Put("dalle_sol", 26f + R(-0.4f, 0.4f), z, R(-6f, 6f), R(-0.02f, 0.06f), new Vector3(R(-3f, 3f), 0f, R(-3f, 3f)), 1f);
            }

            // Des pans de murs effondres, des colonnes couchees, de chaque cote.
            for (int i = 0; i < 16; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = 26f + side * R(6f, 16f), z = R(-78f, -10f);
                Put("mur_droit", x, z, Mathf.Round(R(0f, 4f)) * 90f + R(-15f, 15f), R(-2.8f, -1.1f), new Vector3(R(-12f, 12f), 0f, R(-12f, 12f)), 1f);
                if (i % 3 == 0) Put("pilier", x + R(-3f, 3f), z + R(-3f, 3f), R(0f, 360f), 0.45f, new Vector3(0f, 0f, 84f), 1f);
            }

            // Le cimetiere, a gauche : des tombes ouvertes, leurs dalles jetees a cote.
            for (int i = 0; i < 6; i++)
            {
                float x = R(11f, 18f), z = -44f + i * 3.4f + R(-0.6f, 0.6f), yaw = R(-12f, 12f);
                Put("tombeau", x, z, yaw, R(-0.35f, -0.1f), new Vector3(R(-5f, 5f), 0f, R(-5f, 5f)), 1f);
                Put("tombeau_couvercle", x + R(0.9f, 1.6f), z + R(-0.5f, 0.5f), yaw + R(20f, 60f), 0.05f, new Vector3(R(-25f, -10f), 0f, R(-8f, 8f)), 1f);
            }

            // Des arbres morts.
            for (int i = 0; i < 16; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                Put("arbre_mort", 26f + side * R(5f, 20f), R(-82f, -6f), R(0f, 360f), 0f, new Vector3(R(-6f, 6f), 0f, R(-6f, 6f)), R(0.8f, 1.5f));
            }

            // La facade : trois rangs de murs, des trous dans le rang du haut, deux tours, la grande porte.
            for (float x = 10f; x <= 42f; x += 4f)
            {
                bool gate = Mathf.Abs(x - 26f) < 0.1f;
                if (gate) NightKit.ArchAt(x, GateZ, 0f);
                else Put("mur_droit", x, GateZ, 0f, 0f, Vector3.zero, 1f);
                Put("mur_droit", x, GateZ, 0f, 4f, Vector3.zero, 1f);
                if (rng.NextDouble() > 0.3) Put("mur_droit", x, GateZ, 0f, 8f, Vector3.zero, 1f);
            }
            foreach (float x in new[] { 7f, 45f })
            {
                GameObject tower = Put("pilier", x, GateZ, 0f, 0f, Vector3.zero, 1f);
                if (tower != null) tower.transform.localScale = new Vector3(3.2f, 3.6f, 3.2f);
            }
            GameObject door = NightKit.Place("porte", 26f + 0.95f, GateZ, 0f, 0f, false);
            leaf = door != null ? door.transform : null;
            // Deux braseros allumes devant la porte. Le chateau est vide : qui les a allumes ?
            NightKit.FireAt("Brasero_Feu", 22.6f, GateZ - 1.6f, 0f, true);
            NightKit.KeepLit();
            NightKit.FireAt("Brasero_Feu", 29.4f, GateZ - 1.6f, 0f, true);
            NightKit.KeepLit();
        }

        // ------------------------------------------------------------------ le menu, devant le chateau

        public static void MenuView(NightPlayer p)
        {
            rider = p;
            NightPlayer.EyeHeight = 1.7f;
            p.Place(new Vector3(20.5f, 0.05f, -26f));
            p.Face(18f, -9f);
            Outside(true);
            menuBolt = 3f;
        }

        public static void MenuTick(float dt)
        {
            if (rider == null) return;
            rider.Face(18f + Mathf.Sin(Time.unscaledTime * 0.1f) * 3f, -9f + Mathf.Sin(Time.unscaledTime * 0.13f) * 1.5f);
            menuBolt -= dt;
            if (menuBolt <= 0f) { menuBolt = Random.Range(6f, 12f); NightQuest.Strike(rider.transform.position); }
        }

        static void Outside(bool on)
        {
            RenderSettings.fogColor = on ? FogOut : Color.black;
            RenderSettings.fogDensity = on ? 0.03f : 0.072f;
            if (rider != null && rider.View != null) rider.View.backgroundColor = RenderSettings.fogColor;
        }

        // ------------------------------------------------------------------ la chevauchee

        public static void Begin(NightPlayer p)
        {
            rider = p;
            Playing = true;
            Finished = false;
            t = 0f;
            prevT = -1f;
            black = 1f;
            nextHoof = 0f;
            hoofBeat = 0;
            NightPlayer.EyeHeight = 2.45f;
            p.Place(RoadStart);
            p.Face(0f, -6f);
            Outside(true);
            horse = NightKit.Model("cheval", p.transform);
            shadow = NightKit.Shadow();
            if (horse != null && shadow != null)
            {
                shadow.SetColor("_EmissionColor", Color.black);
                foreach (Renderer r in horse.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = shadow;
            }
        }

        static float Speed(float time)
        {
            if (time < 3.5f) return 1.7f;
            if (time < 13.5f) return Mathf.Lerp(1.7f, 6.4f, Mathf.Clamp01((time - 3.5f) / 1.5f));
            if (time < 17.5f) return Mathf.Lerp(6.4f, 0.9f, (time - 13.5f) / 4f);
            return 0.7f;
        }

        public static void Tick(float dt)
        {
            if (!Playing || rider == null) return;
            if (FiefInput.ConfirmPressed && t > 1f) { End(); return; }
            prevT = t;
            t += dt;
            float speed = Speed(t);
            Vector3 at = rider.transform.position;
            if (t < 22.6f) at.z = Mathf.Min(at.z + speed * dt, StopZ);
            bool gallop = speed > 3f;

            // Le pas, puis le galop : la camera monte et descend, le cou du cheval aussi.
            float freq = gallop ? 2.3f : 1.3f, amp = gallop ? 0.07f : 0.025f;
            float phase = t * freq * Mathf.PI * 2f;
            NightPlayer.RideBob = Mathf.Sin(phase) * amp;
            if (horse != null)
            {
                horse.transform.localPosition = new Vector3(0f, NightPlayer.RideBob * 0.6f, 0f);
                horse.transform.localRotation = Quaternion.Euler(Mathf.Sin(phase + 0.8f) * (gallop ? 5f : 2f), 0f, 0f);
            }
            float sway = Mathf.Sin(t * 0.35f) * 4f;
            rider.Face(sway, -6f + Mathf.Sin(phase + 1.3f) * (gallop ? 1.4f : 0.5f));
            if (t < 22.6f) rider.Place(at);

            // Les sabots : trois coups par foulee au galop, quatre au pas.
            nextHoof -= dt;
            if (nextHoof <= 0f && t < 18.5f)
            {
                NightAudio.Hoof(gallop ? 0.9f : 0.6f, gallop ? 1f : 0.85f);
                hoofBeat++;
                nextHoof = gallop ? (hoofBeat % 3 == 0 ? 0.25f : 0.09f) : 0.3f;
            }

            black = t < 2f ? Mathf.MoveTowards(black, 0f, dt / 1.6f) : black;
            if (Bolt(7.8f)) NightQuest.Strike(at);
            if (Bolt(10.4f))
            {
                // La silhouette, loin devant, en travers du chemin : on ne la voit que pendant l'eclair.
                NightQuest.Strike(at);
                shade = NightKit.Model("penitent", null);
                if (shade != null)
                {
                    Material dark = NightKit.Shadow();
                    if (dark != null) foreach (Renderer r in shade.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = dark;
                    shade.transform.position = new Vector3(23f, 0f, at.z + 26f);
                    shade.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                }
            }
            if (shade != null)
            {
                shade.transform.position += Vector3.right * dt * 3f;
                if (t > 10.9f) { Object.Destroy(shade); shade = null; }
            }
            if (Bolt(14.6f)) NightQuest.Strike(at);
            if (Bolt(18.2f)) NightAudio.Snort();
            if (Bolt(19.6f)) NightAudio.Scream(new Vector3(26f, 2f, GateZ), 0.9f);      // la porte grince
            if (t > 19.6f && leaf != null) leaf.rotation = Quaternion.Euler(0f, Mathf.Lerp(0f, 98f, Mathf.SmoothStep(0f, 1f, (t - 19.6f) / 3.2f)), 0f);
            if (t > 22.6f)
            {
                // On met pied a terre, on avance dans le noir.
                float k = Mathf.Clamp01((t - 22.6f) / 1.6f);
                NightPlayer.EyeHeight = Mathf.Lerp(2.45f, 1.65f, k);
                if (horse != null) horse.SetActive(false);
                rider.Place(new Vector3(at.x, at.y, Mathf.Lerp(StopZ, StopZ + 3f, k)));
                black = Mathf.Clamp01((t - 23.4f) / 1f);
            }
            if (t > 24.8f) End();
        }

        static float prevT;
        /// <summary>Vrai a l'image ou l'on passe l'instant "when".</summary>
        static bool Bolt(float when) { return prevT < when && t >= when; }

        static void End()
        {
            Playing = false;
            Finished = true;
            NightPlayer.EyeHeight = 1.65f;
            NightPlayer.RideBob = 0f;
            if (horse != null) Object.Destroy(horse);
            if (shade != null) Object.Destroy(shade);
            horse = null; shade = null;
            Outside(false);
            black = 0f;
        }

        // ------------------------------------------------------------------ le dessin

        public static void Draw(float w, float h)
        {
            if (!Playing) return;
            if (titleStyle == null || titleStyle.fontSize != Mathf.RoundToInt(h * 0.14f))
            {
                titleStyle = new GUIStyle(GUI.skin.label);
                titleStyle.font = NightFont.Title();
                titleStyle.fontSize = Mathf.RoundToInt(h * 0.14f);
                titleStyle.alignment = TextAnchor.MiddleCenter;
            }
            if (black > 0f)
            {
                GUI.color = new Color(0f, 0f, 0f, black);
                GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
            }
            float band = Mathf.Round(h * 0.1f);
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, w, band), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0f, h - band, w, band), Texture2D.whiteTexture);
            float a = Mathf.Clamp01((t - 0.8f) / 1.2f) * Mathf.Clamp01((5.2f - t) / 1.2f);
            if (a > 0f)
            {
                titleStyle.normal.textColor = new Color(0.85f, 0.76f, 0.58f, a);
                GUI.color = Color.white;
                NightFont.Label(new Rect(0f, h * 0.3f, w, h * 0.2f), "Fief", titleStyle);
            }
            GUI.color = Color.white;
        }
    }
}
