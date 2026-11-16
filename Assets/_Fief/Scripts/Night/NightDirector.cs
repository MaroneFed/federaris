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
            if (shut > 0f) { shut -= dt; return; }
            next -= dt * (1f + 2.2f * fear);
            if (next <= 0f) { shut = 0.30f; next = Random.Range(5f, 11f); }
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

        public static void Begin(NightPlayer player)
        {
            armorTimer = 1f; stepTimer = Random.Range(25f, 45f); stepGap = 0f; extraSteps = 0;
            ghostTimer = Random.Range(35f, 60f); ghost = null; wasMoving = false;
            lastPos = player.transform.position;
            NightBlink.Reset();
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

            // --- l'apparition
            if (ghost == null)
            {
                ghostTimer -= dt;
                if (ghostTimer <= 0f)
                {
                    ghostTimer = Random.Range(45f, 90f);
                    Vector3 spot = NightMap.RandomCell(me, 13f, 24f);
                    if (!Physics.Linecast(me + Vector3.up * 1.6f, spot + Vector3.up * 1.6f, Solid, QueryTriggerInteraction.Ignore))
                    {
                        ghost = NightKit.Model("penitent", null);
                        if (ghost != null)
                        {
                            Material dark = NightKit.Shadow();
                            if (dark != null)
                            {
                                dark.SetColor("_EmissionColor", Color.black);
                                foreach (Renderer r in ghost.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = dark;
                            }
                            ghost.transform.position = spot;
                            Vector3 face = me - spot;
                            face.y = 0f;
                            ghost.transform.rotation = Quaternion.LookRotation(face.normalized, Vector3.up);
                            ghostLife = 14f;
                        }
                    }
                }
            }
            else
            {
                ghostLife -= dt;
                Vector3 d = ghost.transform.position - me;
                d.y = 0f;
                // Elle ne s'en va que lorsque tu ne peux pas la voir partir.
                bool unseen = !Sees(cam, ghost.transform.position + Vector3.up * 1.8f);
                if ((d.magnitude < 9f || ghostLife <= 0f) && unseen) { Object.Destroy(ghost); ghost = null; }
            }
        }
    }

    /// <summary>
    /// LA CROIX DE LA FOI (16/11). Martin : "la croix en bas, il faut que ce soit plus detaille".
    /// Une croix aux bouts trefles, dessinee par le calcul : un contour grave, un coeur qui se remplit d'or
    /// par le bas, un cercle au croisement, un halo. Elle se fele et vire au rouge quand la foi est basse.
    ///
    /// Concept Unity : on calcule pour chaque pixel sa DISTANCE au bord de la forme. Pres de zero : c'est le
    /// contour ; negatif : c'est l'interieur. Avec ca on dessine n'importe quelle forme nette a toute taille.
    /// </summary>
    public static class FaithIcon
    {
        const int W = 128, H = 192;
        static Texture2D line, fill, glow;

        static float Shape(float x, float y)
        {
            // x de -1 a 1, y de -1.5 (bas) a 1.5 (haut). Deux barres, quatre bouts ronds, un cercle au croisement.
            float cy = 0.42f;
            float post = Mathf.Max(Mathf.Abs(x) - 0.13f, Mathf.Abs(y + 0.05f) - 1.22f);
            float beam = Mathf.Max(Mathf.Abs(x) - 0.72f, Mathf.Abs(y - cy) - 0.13f);
            float d = Mathf.Min(post, beam);
            d = Mathf.Min(d, Mathf.Sqrt(x * x + (y - 1.2f) * (y - 1.2f)) - 0.22f);
            d = Mathf.Min(d, Mathf.Sqrt(x * x + (y + 1.28f) * (y + 1.28f)) - 0.22f);
            d = Mathf.Min(d, Mathf.Sqrt((x - 0.72f) * (x - 0.72f) + (y - cy) * (y - cy)) - 0.22f);
            d = Mathf.Min(d, Mathf.Sqrt((x + 0.72f) * (x + 0.72f) + (y - cy) * (y - cy)) - 0.22f);
            float ring = Mathf.Abs(Mathf.Sqrt(x * x + (y - cy) * (y - cy)) - 0.40f) - 0.055f;
            return Mathf.Min(d, ring);
        }

        static void Build()
        {
            line = new Texture2D(W, H, TextureFormat.RGBA32, false);
            fill = new Texture2D(W, H, TextureFormat.RGBA32, false);
            glow = new Texture2D(W, H, TextureFormat.RGBA32, false);
            Color[] a = new Color[W * H], b = new Color[W * H], c = new Color[W * H];
            for (int py = 0; py < H; py++)
                for (int px = 0; px < W; px++)
                {
                    float x = (px + 0.5f) / W * 2f - 1f, y = (py + 0.5f) / H * 3f - 1.5f;
                    float d = Shape(x, y);
                    float edge = Mathf.Clamp01(1f - Mathf.Abs(d + 0.018f) / 0.03f);
                    float inside = Mathf.Clamp01(-(d + 0.045f) / 0.02f);
                    // Un leger relief : plus clair en haut a gauche.
                    float shade = 0.78f + 0.22f * Mathf.Clamp01(0.5f + (y - x) * 0.25f);
                    a[py * W + px] = new Color(1f, 1f, 1f, edge);
                    b[py * W + px] = new Color(shade, shade, shade, inside);
                    c[py * W + px] = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d / 0.38f) * 0.55f * Mathf.Clamp01(1f - inside));
                }
            Texture2D[] all = { line, fill, glow };
            Color[][] px3 = { a, b, c };
            for (int i = 0; i < 3; i++)
            {
                all[i].SetPixels(px3[i]);
                all[i].Apply();
                all[i].hideFlags = HideFlags.HideAndDontSave;
                all[i].wrapMode = TextureWrapMode.Clamp;
            }
        }

        public static void Draw(float w, float h, float value, float flash)
        {
            if (line == null) Build();
            float tall = Mathf.Round(h * 0.15f), wide = Mathf.Round(tall * W / H);
            Rect box = new Rect(Mathf.Round(w * 0.028f), Mathf.Round(h * 0.965f - tall), wide, tall);
            Color gold = new Color(1f, 0.76f, 0.30f);
            Color red = new Color(1f, 0.14f, 0.09f);
            float low = Mathf.Clamp01((0.28f - value) / 0.28f);
            float beat = 0.5f + 0.5f * Mathf.Sin(Time.time * (3f + 7f * low));
            Color ink = Color.Lerp(gold, red, low * (0.55f + 0.45f * beat));
            if (flash > 0f) ink = Color.Lerp(ink, Color.white, flash);
            if (flash < 0f) ink = Color.Lerp(ink, red, -flash);

            GUI.color = new Color(ink.r, ink.g, ink.b, (0.25f + 0.5f * value) * (0.7f + 0.3f * beat));
            GUI.DrawTexture(box, glow);
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(box, fill);                                   // le creux, sombre
            // L'or qui monte : on ne dessine que le bas de l'image, jusqu'a la hauteur de la foi.
            float level = Mathf.Clamp01(value);
            GUI.color = ink;
            GUI.DrawTextureWithTexCoords(new Rect(box.x, box.y + tall * (1f - level), wide, tall * level), fill, new Rect(0f, 0f, 1f, level));
            GUI.color = new Color(0.92f, 0.86f, 0.74f, 0.9f);
            GUI.DrawTexture(box, line);
            GUI.color = Color.white;
        }
    }
}
