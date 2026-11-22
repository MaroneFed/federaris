using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE CLIGNEMENT (16/11). Martin : "le monstre fait des mouvements previsibles", "il ne fait pas peur".
    /// De temps en temps, les yeux du chevalier se ferment un quart de seconde : l'ecran est noir. Plus on
    /// a peur, plus on cligne. Et c'est la que le Penitent bouge, MEME si on le fixait : on rouvre les yeux,
    /// il n'est plus ou il etait. On ne peut plus jamais etre tranquille en le regardant.
    /// </summary>
    public static class NightBlink
    {
        static float next = 7f, shut;
        /// <summary>Les yeux sont fermes : personne ne voit rien.</summary>
        public static bool Closed { get { return shut > 0.05f && shut < 0.26f; } }

        public static void Reset() { next = 9f; shut = 0f; }

        public static void Tick(float dt, float fear)
        {
            // (21/11) Martin : "fermer les yeux, c'est une mecanique horrible". Le chevalier ne cligne plus jamais.
            shut = 0f;
        }

        public static void Draw(float w, float h)
        {
            if (shut <= 0f) return;
            // Deux paupieres qui se referment par le haut et par le bas, puis se rouvrent.
            float k = Mathf.Clamp01(Mathf.Min(shut, 0.30f - shut) / 0.05f);
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, w, h * 0.5f * k + 1f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0f, h - h * 0.5f * k - 1f, w, h * 0.5f * k + 1f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }

    /// <summary>
    /// LE CHATEAU TE REGARDE (16/11). Rien de tout ca ne te fait de mal : c'est fait pour que tu ne saches
    /// plus ce qui est dangereux et ce qui ne l'est pas.
    ///
    ///   les armures   des que tu ne les regardes plus, elles se tournent vers toi. Tu ne les vois jamais bouger ;
    ///   les pas       quand tu marches, quelqu'un marche derriere toi. Tu t'arretes : il fait un pas de trop ;
    ///   l'apparition  une silhouette, loin, au bout d'un couloir. Tu approches, tu clignes : il n'y a plus rien.
    ///
    /// Concept Unity : WorldToViewportPoint dit ou un point du monde tombe dans l'image (entre 0 et 1 :
    /// il est a l'ecran). C'est comme ca qu'on sait ce que le joueur regarde.
    /// </summary>
    public static class NightDirector
    {
        const int Solid = ~(1 << 2);
        static float armorTimer, stepTimer, stepGap, ghostTimer, ghostLife;
        static int extraSteps;
        static bool wasMoving;
        static Vector3 lastPos;
        static GameObject ghost;
        static float visitorStare;
        static bool visitorEyes;

        public static void Begin(NightPlayer player)
        {
            armorTimer = 1f; stepTimer = Random.Range(25f, 45f); stepGap = 0f; extraSteps = 0;
            ghostTimer = Random.Range(35f, 60f); ghost = null; wasMoving = false;
            lastPos = player.transform.position;
            NightBlink.Reset();
        }

        /// <summary>Une silhouette, la, tout de suite (la bibliotheque s'en sert).</summary>
        public static void Summon(Vector3 spot, Vector3 me)
        {
            if (ghost != null) return;
            ghost = NightKit.Model("penitent", null);
            if (ghost == null) return;
            Material dark = NightKit.Shadow();
            if (dark != null)
            {
                dark.SetColor("_EmissionColor", Color.black);
                foreach (Renderer r in ghost.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = dark;
            }
            ghost.transform.position = spot;
            Vector3 face = me - spot;
            face.y = 0f;
            if (face.magnitude > 0.1f) ghost.transform.rotation = Quaternion.LookRotation(face.normalized, Vector3.up);
            ghostLife = 14f;
        }

        /// <summary>Le joueur voit-il ce point ? (dans l'image, et rien devant)</summary>
        public static bool Sees(Camera cam, Vector3 point)
        {
            if (cam == null || NightBlink.Closed) return false;
            Vector3 v = cam.WorldToViewportPoint(point);
            if (v.z <= 0.1f || v.x < -0.05f || v.x > 1.05f || v.y < -0.05f || v.y > 1.05f) return false;
            return !Physics.Linecast(cam.transform.position, point, Solid, QueryTriggerInteraction.Ignore);
        }

        public static void Tick(float dt, NightPlayer player, float fear, bool night)
        {
            if (NightIntro.Playing || NightUI.MenuOpen) return;
            NightBlink.Tick(dt, night ? fear : 0f);
            if (!night || player == null || NightCinema.Playing) return;
            Camera cam = player.View;
            Vector3 me = player.transform.position;
            Vector3 moved = me - lastPos;
            moved.y = 0f;
            bool moving = moved.magnitude > dt * 1.5f;
            lastPos = me;

            // --- les armures : elles te suivent du regard quand tu as le dos tourne
            armorTimer -= dt;
            if (armorTimer <= 0f)
            {
                armorTimer = 0.6f;
                List<Transform> armors = NightMap.Armors;
                for (int i = 0; i < armors.Count; i++)
                {
                    Transform a = armors[i];
                    if (a == null) continue;
                    Vector3 to = me - a.position;
                    to.y = 0f;
                    if (to.magnitude > 28f || to.magnitude < 1.2f) continue;
                    if (Sees(cam, a.position + Vector3.up * 1.6f)) continue;
                    a.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
                }
            }

            // --- les pas derriere toi
            stepTimer -= dt;
            if (stepTimer <= 0f && moving) { stepTimer = Random.Range(40f, 80f); stepGap = 0.01f; extraSteps = Random.Range(5, 10); }
            if (stepGap > 0f)
            {
                stepGap -= dt;
                if (stepGap <= 0f)
                {
                    NightAudio.Step(me - player.transform.forward * 2.6f, 0.9f);
                    extraSteps--;
                    // Tu t'es arrete : encore UN pas, puis plus rien.
                    if (!moving && wasMoving) extraSteps = Mathf.Min(extraSteps, 1);
                    stepGap = extraSteps > 0 && (moving || extraSteps == 1) ? 0.52f : 0f;
                    if (!moving && extraSteps <= 0) stepGap = 0f;
                }
            }
            wasMoving = moving;

            // --- (21/11 soir) LE VISITEUR. Une silhouette immense, tout au bout du couloir, qui te regarde. Tant que tu la
            // fixes, ta torche tousse de temps en temps... et a chaque fois que la lumiere revient, elle est PLUS PRES.
            // Detourne les yeux : elle disparait. Fixe-la trop longtemps : elle est sur toi, elle hurle, et plus rien.
            if (ghost == null)
            {
                ghostTimer -= dt;
                if (ghostTimer <= 0f)
                {
                    ghostTimer = Random.Range(50f, 100f);
                    Vector3 spot = NightMap.RandomCell(me, 13f, 22f);
                    spot.y = NightMap.FloorY(spot);
                    if (Mathf.Abs(spot.y - me.y) < 1f && !Physics.Linecast(me + Vector3.up * 1.6f, spot + Vector3.up * 1.6f, Solid, QueryTriggerInteraction.Ignore))
                    {
                        ghost = new GameObject("LE VISITEUR");
                        ghost.transform.position = spot;
                        Transform body = new GameObject("Corps").transform;
                        body.SetParent(ghost.transform, false);
                        body.localScale = new Vector3(0.55f, 1.6f, 0.55f);
                        GameObject m = Puppet.Spawn("Modeles/Monstres/Monstre_Ninja", body, 3.2f);
                        if (m != null)
                        {
                            Puppet.Silhouette(m, 0.006f);
                            CreepyRig.Add(m, ghost.transform, player.transform, 75f, 30f);
                            Puppet.Play(m.GetComponentInChildren<Animation>(), 0.2f, false, "|Idle");
                        }
                        else { Object.Destroy(ghost); ghost = null; }
                        if (ghost != null)
                        {
                            NightKit.Smoke(ghost.transform, 3.2f);
                            Vector3 face = me - spot;
                            face.y = 0f;
                            ghost.transform.rotation = Quaternion.LookRotation(face.normalized, Vector3.up);
                            ghostLife = 16f; visitorStare = 0f; visitorEyes = false;
                        }
                    }
                }
            }
            else
            {
                ghostLife -= dt;
                Vector3 d = ghost.transform.position - me;
                d.y = 0f;
                bool seen = Sees(cam, ghost.transform.position + Vector3.up * 2.2f);
                if (!visitorEyes && ghostLife < 15.5f)
                {
                    // Ses yeux s'allument une demi-seconde apres qu'il est apparu (le modele a sa taille finale).
                    visitorEyes = true;
                    GameObject model = ghost.GetComponentInChildren<Animation>() != null ? ghost.GetComponentInChildren<Animation>().gameObject : null;
                    if (model != null) Puppet.EmberEyes(model, 0.05f);
                }
                if (seen)
                {
                    visitorStare += dt;
                    if (visitorStare > 1.4f)
                    {
                        // La torche tousse : le noir un quart de seconde... et il a avance.
                        visitorStare = 0f;
                        NightPlayer.TorchOut = 0.28f;
                        NightAudio.Bang(ghost.transform.position, 0.35f);
                        float step = Mathf.Min(d.magnitude * 0.4f, d.magnitude - 1.2f);
                        Vector3 to = ghost.transform.position - d.normalized * Mathf.Max(0f, step);
                        to.y = NightMap.FloorY(to);
                        ghost.transform.position = to;
                        if (d.magnitude - step < 3f)
                        {
                            // Il est sur toi : le hurlement, l'image qui saute. Puis plus rien.
                            NightAudio.Scream(me + Vector3.up * 1.6f + player.transform.forward, 1f);
                            NightAudio.Sting(1f);
                            NightPlayer.Shake = 1f;
                            NightPlayer.Punch = 1f;
                            NightPlayer.TorchOut = 0.6f;
                            Object.Destroy(ghost);
                            ghost = null;
                            return;
                        }
                    }
                }
                // Tu detournes les yeux (ou tu attends trop) : quand tu regardes de nouveau, il n'y a plus personne.
                if ((!seen && visitorStare > 0f) || (ghostLife <= 0f && !seen))
                {
                    Object.Destroy(ghost);
                    ghost = null;
                }
            }
        }
    }

    /// <summary>
    /// LA RESERVE DE LUMIERE (21/11). Martin : "le crucifix en bas a gauche, j'aime pas du tout son design ; que ce soit
    /// notre reserve de lumiere". Plus de croix : UNE BOUGIE BLANCHE dans sa coupelle de fer. Sa cire a la hauteur de ta
    /// reserve, elle fond quand ta torche s'use, sa flamme vacille, retrecit, vire au bleu quand il ne reste presque rien.
    /// Quand tu ravives ta torche a un feu, des etincelles d'or montent ; quand tu en perds, un eclat rouge.
    ///
    /// Concept Unity : les images sont fabriquees pixel par pixel une seule fois (SetPixels), puis seulement etirees.
    /// </summary>
    public static class LightGauge
    {
        static Texture2D wax, top, flame, halo, dish;

        static float Smooth(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        static Texture2D Make(int w, int h)
        {
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.hideFlags = HideFlags.HideAndDontSave;
            t.wrapMode = TextureWrapMode.Clamp;
            return t;
        }

        static void Build()
        {
            if (wax != null) return;
            // La cire : une colonne creme, ombree sur les bords comme un cylindre, avec des coulures le long du flanc.
            wax = Make(64, 128);
            Color[] px = new Color[64 * 128];
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 64; x++)
                {
                    float u = (x + 0.5f) / 64f * 2f - 1f;
                    float shade = Mathf.Sqrt(Mathf.Max(0f, 1f - u * u));
                    float drip = Mathf.PerlinNoise(x * 0.25f, y * 0.02f) * 0.08f;
                    Color c = Color.Lerp(new Color(0.55f, 0.5f, 0.42f), new Color(0.98f, 0.95f, 0.86f), shade * 0.85f + 0.15f - drip);
                    c += new Color(1f, 0.8f, 0.5f, 0f) * Smooth(0.3f, -0.4f, u) * 0.06f;
                    c.a = 1f - Smooth(0.9f, 1f, Mathf.Abs(u));
                    px[y * 64 + x] = c;
                }
            wax.SetPixels(px); wax.Apply();

            // Le haut : une cuvette de cire fondue, translucide, eclairee par la flamme.
            top = Make(64, 32);
            px = new Color[64 * 32];
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 64; x++)
                {
                    float u = (x + 0.5f) / 64f * 2f - 1f, v = (y + 0.5f) / 32f * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);
                    Color c = Color.Lerp(new Color(1f, 0.93f, 0.72f), new Color(0.92f, 0.86f, 0.74f), Smooth(0.2f, 0.9f, r));
                    c.a = 1f - Smooth(0.85f, 1f, r);
                    px[y * 64 + x] = c;
                }
            top.SetPixels(px); top.Apply();

            // La flamme : une goutte, coeur blanc, puis jaune, orange, et un bord rouge qui s'efface.
            flame = Make(64, 128);
            px = new Color[64 * 128];
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 64; x++)
                {
                    float u = (x + 0.5f) / 64f * 2f - 1f, v = (y + 0.5f) / 128f;
                    // Une goutte : large en bas (v ~ 0.25), pointue en haut.
                    float width = v < 0.25f ? Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((0.25f - v) / 0.25f, 2f))) : Mathf.Pow(1f - (v - 0.25f) / 0.75f, 0.85f);
                    float d = width <= 0.001f ? 9f : Mathf.Abs(u) / width;
                    float core = 1f - Smooth(0f, 0.55f, d + Mathf.Max(0f, v - 0.45f));
                    Color c = Color.Lerp(new Color(1f, 0.35f, 0.08f), new Color(1f, 0.78f, 0.3f), Smooth(0.2f, 0.9f, 1f - d));
                    c = Color.Lerp(c, new Color(1f, 0.98f, 0.9f), core);
                    // Le pied de la flamme est bleu, comme une vraie flamme de bougie.
                    c = Color.Lerp(c, new Color(0.35f, 0.5f, 1f), Smooth(0.14f, 0.02f, v) * 0.7f);
                    c.a = (1f - Smooth(0.6f, 1f, d)) * Smooth(0f, 0.06f, v);
                    px[y * 64 + x] = c;
                }
            flame.SetPixels(px); flame.Apply();

            // Le halo : une lueur ronde, tres douce.
            halo = Make(64, 64);
            px = new Color[64 * 64];
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float r = Mathf.Sqrt((x - 31.5f) * (x - 31.5f) + (y - 31.5f) * (y - 31.5f)) / 31.5f;
                    float a = Mathf.Clamp01(1f - r);
                    px[y * 64 + x] = new Color(1f, 1f, 1f, a * a * a);
                }
            halo.SetPixels(px); halo.Apply();

            // La coupelle : une assiette de fer vue un peu de dessus, avec son anneau pour la tenir.
            dish = Make(128, 48);
            px = new Color[128 * 48];
            for (int y = 0; y < 48; y++)
                for (int x = 0; x < 128; x++)
                {
                    float u = (x + 0.5f) / 128f * 2f - 1f, v = (y + 0.5f) / 48f * 2f - 1f;
                    float plateD = Mathf.Sqrt(u * u * 0.95f + v * v * 2.6f) - 0.92f;
                    float ringD = Mathf.Abs(Mathf.Sqrt((u - 0.88f) * (u - 0.88f) * 6f + v * v * 3f) - 0.32f) - 0.07f;
                    float d = Mathf.Min(plateD, ringD);
                    float n = Mathf.PerlinNoise(x * 0.15f, y * 0.15f);
                    Color c = Color.Lerp(new Color(0.09f, 0.09f, 0.1f), new Color(0.38f, 0.36f, 0.34f), Smooth(-0.6f, 0.6f, v) * 0.7f + n * 0.3f);
                    c.a = 1f - Smooth(-0.02f, 0.02f, d);
                    px[y * 128 + x] = c;
                }
            dish.SetPixels(px); dish.Apply();
        }

        public static void Draw(float w, float h, float value, float flash)
        {
            Build();
            float t = Time.time;
            float unit = Mathf.Round(h * 0.01f);
            float baseX = Mathf.Round(h * 0.07f), baseY = Mathf.Round(h * 0.95f);
            float waxW = unit * 3.4f, waxMax = unit * 17f;
            float waxH = Mathf.Max(unit * 0.8f, waxMax * value);
            float cx = baseX + unit * 5f;
            bool dying = value < 0.25f;

            // Une ombre chaude derriere : l'endroit du coin de l'ecran ou la bougie eclaire.
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(cx - unit * 12f, baseY - unit * 28f, unit * 24f, unit * 32f), halo);
            float flick = 0.85f + 0.15f * Mathf.PerlinNoise(t * 9f, 3f);
            if (value > 0.001f)
            {
                GUI.color = dying ? new Color(0.5f, 0.6f, 1f, 0.18f * flick) : new Color(1f, 0.62f, 0.25f, (0.16f + 0.22f * value) * flick);
                float g = unit * (10f + 10f * value);
                GUI.DrawTexture(new Rect(cx - g, baseY - waxH - unit * 4f - g, g * 2f, g * 2f), halo);
            }

            // La cire, puis sa cuvette.
            GUI.color = Color.white;
            Rect column = new Rect(Mathf.Round(cx - waxW * 0.5f), Mathf.Round(baseY - unit * 1.2f - waxH), Mathf.Round(waxW), Mathf.Round(waxH));
            GUI.DrawTexture(column, wax);
            GUI.DrawTexture(new Rect(column.x - unit * 0.15f, column.y - unit * 0.55f, column.width + unit * 0.3f, unit * 1.1f), top);
            // Une coulure qui descend lentement le long du flanc.
            float run = Mathf.Repeat(t * 0.05f, 1f);
            GUI.color = new Color(1f, 0.97f, 0.88f, 0.9f);
            GUI.DrawTexture(new Rect(column.xMax - unit * 0.9f, column.y + unit * 0.2f, unit * 0.55f, Mathf.Min(column.height * 0.7f, unit * 1.2f + run * column.height * 0.6f)), wax);
            // La meche.
            GUI.color = new Color(0.08f, 0.06f, 0.05f);
            GUI.DrawTexture(new Rect(Mathf.Round(cx - unit * 0.12f), column.y - unit * 1.1f, Mathf.Max(1f, Mathf.Round(unit * 0.25f)), unit * 1.1f), Texture2D.whiteTexture);
            // La coupelle de fer, devant la cire.
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(cx - unit * 6f, baseY - unit * 2.2f, unit * 12f, unit * 4.5f), dish);

            // La flamme : sa taille suit la reserve ; elle se couche un peu, tremble, bleuit quand elle meurt.
            if (value > 0.001f)
            {
                float size = unit * (2.2f + 5.5f * Mathf.Sqrt(value)) * flick;
                float lean = (Mathf.PerlinNoise(t * 1.7f, 9f) - 0.5f) * 10f;
                Rect f = new Rect(cx - size * 0.32f, column.y - unit * 0.9f - size, size * 0.64f, size);
                Matrix4x4 keep = GUI.matrix;
                GUIUtility.RotateAroundPivot(lean, new Vector2(cx, column.y - unit * 0.9f));
                GUI.color = dying ? Color.Lerp(new Color(0.55f, 0.7f, 1f), Color.white, 0.5f + 0.5f * Mathf.Sin(t * 6f)) : Color.white;
                GUI.DrawTexture(f, flame);
                GUI.matrix = keep;
            }
            else
            {
                // Eteinte : un mince fil de fumee qui monte de la meche.
                GUI.color = new Color(0.6f, 0.6f, 0.62f, 0.35f);
                for (int i = 0; i < 4; i++)
                {
                    float k = Mathf.Repeat(t * 0.4f + i * 0.25f, 1f);
                    GUI.DrawTexture(new Rect(cx - unit * 0.6f + Mathf.Sin(t * 2f + i) * unit * 0.6f, column.y - unit * (1.5f + 6f * k), unit * 1.2f, unit * 1.2f), halo);
                }
            }

            // On vient de la raviver : des etincelles d'or qui montent. D'en perdre : un eclat rouge.
            if (flash > 0f)
                for (int i = 0; i < 6; i++)
                {
                    float k = Mathf.Repeat(t * 0.9f + i / 6f, 1f);
                    GUI.color = new Color(1f, 0.8f, 0.35f, flash * (1f - k));
                    float s = unit * 0.9f;
                    GUI.DrawTexture(new Rect(cx + Mathf.Sin(i * 2.3f + t) * unit * 4f - s * 0.5f, column.y - unit * (2f + 10f * k), s, s), halo);
                }
            else if (flash < 0f)
            {
                GUI.color = new Color(1f, 0.15f, 0.08f, -flash * 0.5f);
                GUI.DrawTexture(new Rect(cx - unit * 9f, column.y - unit * 9f, unit * 18f, unit * 18f), halo);
            }
            GUI.color = Color.white;
        }
    }
}
