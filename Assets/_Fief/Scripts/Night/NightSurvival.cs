using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA SURVIE (14/11). Martin : "le scenario, c'est qu'on survive ; on doit garder la foi en faisant
    /// des actions ; des petits trucs a la main, comme la serrure de Fallout, pour les portes, pour les
    /// cierges ; des monstres qu'on voit, un peu cons, qui rodent : derriere eux ils ne te voient pas,
    /// devant eux t'es dans la merde".
    ///
    /// Ce fichier porte quatre choses, toutes branchees par NightBootstrap :
    ///   Faith       LA FOI : une jauge qui baisse toute seule, plus vite quand on est traque ;
    ///   NightGames  LES GESTES : battre le silex (cierges), crocheter une serrure (portes) ;
    ///   NightDoor   UNE PORTE verrouillee, qui tourne sur ses gonds ;
    ///   Prowler     LE RODEUR : il fait sa ronde, sa lanterne EST son regard.
    /// </summary>
    public static class Faith
    {
        // (21/11) Ce n'est plus la foi : c'est LA RESERVE DE TA TORCHE (Martin : "le crucifix, que ce soit notre reserve de
        // lumiere, qu'on puisse la reprendre dans un feu"). Le nom de la classe reste, pour ne rien casser ailleurs.
        /// <summary>De 0 (la torche est morte) a 1 (pleine).</summary>
        public static float Value = 1f;
        static float flash;
        /// <summary>Le dernier changement : +1 (on vient de la raviver), -1 (on vient d'en perdre).</summary>
        public static float Flash { get { return flash; } }

        /// <summary>La lanterne du tresor : la torche s'use moins vite.</summary>
        public static float Scale = 1f;

        public static void Reset() { Value = 1f; flash = 0f; Scale = 1f; }

        public static void Add(float amount)
        {
            Value = Mathf.Clamp01(Value + amount);
            if (Mathf.Abs(amount) > 0.004f) flash = amount < 0f ? -1f : 1f;
        }

        /// <summary>Elle baisse toute seule : cinq minutes pour mourir au calme, plus vite quand on court ou qu'on est traque.</summary>
        public static void Tick(float dt, bool hunted)
        {
            float drain = Scale * (1f / 210f) * (1f + 0.8f * Mathf.Max(Penitent.Dread, Beast.Dread) + (hunted ? 0.8f : 0f) + (FiefInput.SprintHeld ? 0.6f : 0f));      // (22/11) plus dur
            Value = Mathf.Clamp01(Value - drain * dt);
            flash = Mathf.MoveTowards(flash, 0f, dt * 1.6f);
        }

        /// <summary>La bougie de la reserve, en bas a gauche (voir LightGauge, dans NightDirector.cs).</summary>
        public static void Draw(float w, float h) { LightGauge.Draw(w, h, Value, flash); }
    }

    /// <summary>
    /// LES GESTES. Pendant un geste, le chevalier ne bouge plus (NightPlayer lit Busy) : c'est la que ca
    /// fait peur, on a les mains prises et le dos tourne.
    ///
    ///   LE SILEX   un point tourne sur un cercle ; frapper (Espace ou clic) quand il passe dans l'arc d'or.
    ///              Trois etincelles pour allumer ; l'arc retrecit a chaque fois.
    ///   LA SERRURE comme dans Fallout : la souris tourne le crochet, Espace (ou clic) force le barillet.
    ///              Plus le crochet est pres du bon angle, plus le barillet tourne loin. Forcer trop : il casse.
    ///
    /// Un rate fait du BRUIT (le Rodeur l'entend) et coute un peu de foi. E : lacher.
    ///
    /// Concept Unity : OnGUI dessine par-dessus le jeu, en pixels. GUIUtility.RotateAroundPivot fait
    /// tourner tout ce qu'on dessine ensuite autour d'un point ; on remet GUI.matrix a la fin.
    /// </summary>
    public static class NightGames
    {
        public enum Kind { None, Flint, Lock }
        public static Kind Current = Kind.None;
        /// <summary>Les mains prises (un geste) ou la tete tenue (une cinematique) : le chevalier ne se controle plus.</summary>
        public static bool Busy { get { return Current != Kind.None || NightCinema.Playing || NightIntro.Playing || NightUI.Blocking; } }

        static System.Action<bool> done;
        static Vector3 where;
        static float started;
        static Texture2D ring;

        // le silex
        static float angle, speed, arcAt, arcSize, sparkFlash, missFlash;
        static int sparks;
        // la serrure
        static float sweet, pick, turn, strain, snap;
        static int picks;

        public static void StartFlint(Vector3 at, System.Action<bool> onDone)
        {
            Current = Kind.Flint; done = onDone; where = at; started = Time.time;
            sparks = 0; angle = 0f; speed = 150f; arcSize = 58f; arcAt = Random.Range(60f, 300f);
            sparkFlash = missFlash = 0f;
        }

        public static void StartLock(Vector3 at, System.Action<bool> onDone)
        {
            Current = Kind.Lock; done = onDone; where = at; started = Time.time;
            sweet = Random.Range(-80f, 80f); pick = 0f; turn = 0f; strain = 0f; snap = 0f; picks = 4;
        }

        /// <summary>On lache tout sans rien dire (le joueur vient d'etre attrape).</summary>
        public static void Abort() { Current = Kind.None; done = null; }

        static void Finish(bool success)
        {
            Current = Kind.None;
            System.Action<bool> callback = done;
            done = null;
            if (callback != null) callback.Invoke(success);
        }

        /// <summary>Un bruit dans le chateau : le Rodeur vient voir, et la foi en prend un coup.</summary>
        static void Fumble(float faithCost)
        {
            Faith.Add(-faithCost * 0.5f);
            Prowler.Hear(where);
            Beast.Hear(where);
        }

        public static void Tick(float dt)
        {
            if (Current == Kind.None) return;
            bool act = FiefInput.JumpPressed || FiefInput.ShootPressed;
            if (FiefInput.InteractPressed && Time.time - started > 0.3f) { Finish(false); return; }
            sparkFlash = Mathf.MoveTowards(sparkFlash, 0f, dt * 3f);
            missFlash = Mathf.MoveTowards(missFlash, 0f, dt * 3f);

            if (Current == Kind.Flint)
            {
                angle = Mathf.Repeat(angle + speed * dt, 360f);
                if (!act) return;
                bool hit = Mathf.Abs(Mathf.DeltaAngle(angle, arcAt)) <= arcSize * 0.5f;
                if (hit)
                {
                    sparks++;
                    sparkFlash = 1f;
                    NightAudio.Lock(where);
                    if (sparks >= 3) { Finish(true); return; }
                    arcSize *= 0.72f; speed *= 1.22f; arcAt = Mathf.Repeat(arcAt + Random.Range(90f, 250f), 360f);
                }
                else
                {
                    missFlash = 1f;
                    try { NightAudio.Bang(where, 0.3f); } catch (System.Exception) { }
                    Fumble(0.04f);
                }
                return;
            }

            // --- la serrure
            snap = Mathf.MoveTowards(snap, 0f, dt * 2f);
            if (snap > 0f) return;
            pick = Mathf.Clamp(pick + FiefInput.Look.x * 0.35f, -90f, 90f);
            bool force = FiefInput.JumpHeld || FiefInput.ShootHeld;
            float allowed = 1f - Mathf.Clamp01((Mathf.Abs(pick - sweet) - 4f) / 38f);
            if (force)
            {
                turn = Mathf.MoveTowards(turn, allowed, dt * 1.3f);
                if (turn >= 0.999f) { NightAudio.Lock(where); Finish(true); return; }
                if (turn >= allowed - 0.001f) strain += dt;
                if (strain > 0.6f)
                {
                    picks--; strain = 0f; turn = 0f; snap = 1f; missFlash = 1f;
                    try { NightAudio.Lock(where); } catch (System.Exception) { }
                    Fumble(0.06f);
                    if (picks <= 0) { Finish(false); return; }
                }
            }
            else
            {
                turn = Mathf.MoveTowards(turn, 0f, dt * 2.5f);
                strain = Mathf.MoveTowards(strain, 0f, dt);
            }
        }

        static Texture2D Ring()
        {
            if (ring != null) return ring;
            const int size = 256;
            ring = new Texture2D(size, size, TextureFormat.RGBA32, false);
            ring.hideFlags = HideFlags.HideAndDontSave;
            Color[] px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float r = Mathf.Sqrt((x - 127.5f) * (x - 127.5f) + (y - 127.5f) * (y - 127.5f)) / 127.5f;
                    float a = Mathf.Clamp01((r - 0.84f) * 40f) * Mathf.Clamp01((0.97f - r) * 40f);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            ring.SetPixels(px);
            ring.Apply();
            return ring;
        }

        /// <summary>Un petit rectangle pose sur le cercle, a "degrees" (0 en haut, sens des aiguilles).</summary>
        static void Mark(Vector2 c, float radius, float degrees, float width, float length, Color color)
        {
            Matrix4x4 saved = GUI.matrix;
            GUIUtility.RotateAroundPivot(degrees, c);
            GUI.color = color;
            GUI.DrawTexture(new Rect(c.x - width * 0.5f, c.y - radius - length * 0.5f, width, length), Texture2D.whiteTexture);
            GUI.matrix = saved;
        }

        // ------------------------------------------------------------------ (21/11) le dessin, refait
        // Martin : "le crochetage de serrure, j'aime pas du tout le design". Plus de traits blancs sur fond noir : une
        // vraie serrure, vue de pres. Une platine de fer forge rivetee, un barillet de laiton avec son trou de cle, la
        // clef de tension en bas (elle tourne quand tu forces), le crochet d'acier qui entre par le trou et suit la
        // souris. Il tremble et rougit quand il force trop ; il casse dans un eclat. Tout est fabrique par le calcul.

        static Texture2D plate, plug, pickTex, wrench, sparkTex;

        static float Smooth(float edge0, float edge1, float x) { float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0)); return t * t * (3f - 2f * t); }

        /// <summary>La platine : un disque de fer brosse, bord biseaute, huit rivets, la rouille dans les creux.</summary>
        static Texture2D Plate()
        {
            if (plate != null) return plate;
            const int N = 512;
            plate = new Texture2D(N, N, TextureFormat.RGBA32, false);
            plate.hideFlags = HideFlags.HideAndDontSave;
            Color[] px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);
                    float a = 1f - Smooth(0.965f, 0.985f, r);
                    float n = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.5f + Mathf.PerlinNoise(x * 0.21f, y * 0.013f) * 0.35f + Mathf.PerlinNoise(x * 0.6f, y * 0.6f) * 0.15f;
                    Color iron = Color.Lerp(new Color(0.16f, 0.16f, 0.17f), new Color(0.33f, 0.32f, 0.31f), n);
                    // Le biseau : clair en haut a gauche, sombre en bas a droite.
                    float bevel = Smooth(0.86f, 0.95f, r) * (1f - Smooth(0.95f, 0.98f, r));
                    float lightDir = (-u + v) * 0.5f;
                    iron += new Color(1f, 1f, 1f, 0f) * bevel * lightDir * 0.35f;
                    // Le creux central ou tourne le barillet.
                    float well = 1f - Smooth(0.5f, 0.56f, r);
                    iron = Color.Lerp(iron, iron * 0.35f, well);
                    iron += new Color(1f, 1f, 1f, 0f) * Smooth(0.54f, 0.56f, r) * (1f - Smooth(0.56f, 0.6f, r)) * (u - v) * 0.25f;
                    // La rouille.
                    float rust = Mathf.Clamp01((Mathf.PerlinNoise(x * 0.03f + 40f, y * 0.03f) - 0.55f) * 3f) * Smooth(0.6f, 0.9f, r);
                    iron = Color.Lerp(iron, new Color(0.32f, 0.14f, 0.06f), rust * 0.6f);
                    // Huit rivets.
                    for (int k = 0; k < 8; k++)
                    {
                        float ang = k * Mathf.PI / 4f + Mathf.PI / 8f;
                        float dx = u - Mathf.Cos(ang) * 0.76f, dy = v - Mathf.Sin(ang) * 0.76f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        if (d < 0.06f)
                        {
                            float dome = Mathf.Sqrt(Mathf.Max(0f, 1f - (d / 0.06f) * (d / 0.06f)));
                            iron = Color.Lerp(new Color(0.08f, 0.08f, 0.08f), new Color(0.55f, 0.53f, 0.5f), dome * (0.5f + 0.5f * (-dx + dy) / 0.06f));
                        }
                    }
                    iron.a = a;
                    px[y * N + x] = iron;
                }
            plate.SetPixels(px);
            plate.Apply();
            return plate;
        }

        /// <summary>Le barillet : un disque de laiton use, et son trou de serrure (un rond et une fente).</summary>
        static Texture2D Plug()
        {
            if (plug != null) return plug;
            const int N = 256;
            plug = new Texture2D(N, N, TextureFormat.RGBA32, false);
            plug.hideFlags = HideFlags.HideAndDontSave;
            Color[] px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);
                    float a = 1f - Smooth(0.96f, 0.99f, r);
                    float n = Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
                    Color brass = Color.Lerp(new Color(0.38f, 0.27f, 0.1f), new Color(0.72f, 0.55f, 0.25f), n * 0.6f + 0.4f * (1f - r));
                    brass += new Color(1f, 0.9f, 0.7f, 0f) * Smooth(0.85f, 0.95f, r) * (1f - Smooth(0.95f, 0.99f, r)) * (-u + v) * 0.3f;
                    // Le trou : un rond en haut, une fente qui descend.
                    float hole = Mathf.Min(Mathf.Sqrt(u * u + (v - 0.18f) * (v - 0.18f)) - 0.17f, Mathf.Max(Mathf.Abs(u) - 0.075f, Mathf.Abs(v + 0.2f) - 0.32f));
                    float dark = 1f - Smooth(-0.01f, 0.015f, hole);
                    brass = Color.Lerp(brass, new Color(0.01f, 0.008f, 0.005f), dark);
                    brass += new Color(1f, 0.85f, 0.5f, 0f) * (Smooth(0.0f, 0.02f, hole) * (1f - Smooth(0.02f, 0.05f, hole))) * 0.25f;
                    brass.a = a;
                    px[y * N + x] = brass;
                }
            plug.SetPixels(px);
            plug.Apply();
            return plug;
        }

        /// <summary>Le crochet : une tige d'acier fine, son bout recourbe en haut, un manche entoure de cuir en bas.</summary>
        static Texture2D Pick()
        {
            if (pickTex != null) return pickTex;
            const int W = 48, H = 512;
            pickTex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            pickTex.hideFlags = HideFlags.HideAndDontSave;
            Color[] px = new Color[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (x + 0.5f) / W - 0.5f, v = (y + 0.5f) / H;
                    Color c = new Color(0f, 0f, 0f, 0f);
                    if (v < 0.36f)
                    {
                        // Le manche de cuir, avec ses tours de laniere.
                        float half = 0.32f;
                        if (Mathf.Abs(u) < half)
                        {
                            float wrap = 0.5f + 0.5f * Mathf.Sin(v * 220f + u * 8f);
                            c = Color.Lerp(new Color(0.12f, 0.06f, 0.03f), new Color(0.32f, 0.18f, 0.08f), wrap * (1f - Mathf.Abs(u) / half));
                            c.a = 1f - Smooth(half - 0.06f, half, Mathf.Abs(u));
                        }
                    }
                    else
                    {
                        // La tige d'acier, qui s'affine, et le crochet au bout.
                        float half = Mathf.Lerp(0.12f, 0.07f, (v - 0.36f) / 0.64f);
                        float hook = v > 0.93f ? (v - 0.93f) / 0.07f : 0f;
                        float cx = -hook * hook * 0.28f;
                        float d = Mathf.Abs(u - cx);
                        if (d < half)
                        {
                            float shine = 1f - d / half;
                            c = Color.Lerp(new Color(0.35f, 0.36f, 0.38f), new Color(0.92f, 0.93f, 0.95f), shine * shine);
                            c.a = 1f - Smooth(half * 0.75f, half, d);
                        }
                    }
                    px[y * W + x] = c;
                }
            pickTex.SetPixels(px);
            pickTex.Apply();
            return pickTex;
        }

        /// <summary>La clef de tension : une barre de fer pliee en L.</summary>
        static Texture2D Wrench()
        {
            if (wrench != null) return wrench;
            const int N = 128;
            wrench = new Texture2D(N, N, TextureFormat.RGBA32, false);
            wrench.hideFlags = HideFlags.HideAndDontSave;
            Color[] px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N, v = (y + 0.5f) / N;
                    float bar = Mathf.Max(Mathf.Abs(u - 0.5f) - 0.035f, Mathf.Abs(v - 0.45f) - 0.45f);
                    float foot = Mathf.Max(Mathf.Abs(u - 0.62f) - 0.15f, Mathf.Abs(v - 0.06f) - 0.035f);
                    float d = Mathf.Min(bar, foot);
                    Color c = Color.Lerp(new Color(0.3f, 0.3f, 0.32f), new Color(0.75f, 0.75f, 0.78f), Mathf.Clamp01(-d * 30f));
                    c.a = 1f - Smooth(-0.004f, 0.004f, d);
                    px[y * N + x] = c;
                }
            wrench.SetPixels(px);
            wrench.Apply();
            return wrench;
        }

        /// <summary>Une etincelle ronde et douce (le silex, la casse).</summary>
        public static Texture2D Spark()
        {
            if (sparkTex != null) return sparkTex;
            const int N = 64;
            sparkTex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            sparkTex.hideFlags = HideFlags.HideAndDontSave;
            Color[] px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float r = Mathf.Sqrt((x - 31.5f) * (x - 31.5f) + (y - 31.5f) * (y - 31.5f)) / 31.5f;
                    float a = Mathf.Clamp01(1f - r);
                    px[y * N + x] = new Color(1f, 1f, 1f, a * a);
                }
            sparkTex.SetPixels(px);
            sparkTex.Apply();
            return sparkTex;
        }

        static void Rotated(Texture2D tex, Rect r, float degrees, Vector2 pivot, Color color)
        {
            Matrix4x4 saved = GUI.matrix;
            GUIUtility.RotateAroundPivot(degrees, pivot);
            GUI.color = color;
            GUI.DrawTexture(r, tex);
            GUI.matrix = saved;
        }

        public static void Draw(float w, float h)
        {
            if (Current == Kind.None) return;
            Vector2 c = new Vector2(Mathf.Round(w * 0.5f), Mathf.Round(h * 0.45f));
            float R = Mathf.Round(h * 0.2f);
            // Le reste du monde s'assombrit : on est penche sur la serrure.
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
            Color gold = new Color(1f, 0.78f, 0.32f);
            Color red = new Color(1f, 0.25f, 0.12f);

            if (Current == Kind.Flint)
            {
                GUI.color = Color.Lerp(new Color(1f, 1f, 1f, 0.35f), red, missFlash);
                GUI.DrawTexture(new Rect(c.x - R * 0.7f, c.y - R * 0.7f, R * 1.4f, R * 1.4f), Ring());
                for (float d = -arcSize * 0.5f; d <= arcSize * 0.5f; d += 3f)
                    Mark(c, R * 0.63f, arcAt + d, R * 0.04f, R * 0.1f, Color.Lerp(gold, Color.white, sparkFlash));
                Mark(c, R * 0.63f, angle, R * 0.035f, R * 0.2f, Color.white);
                for (int i = 0; i < 3; i++)
                {
                    GUI.color = i < sparks ? gold : new Color(1f, 1f, 1f, 0.2f);
                    float s = R * 0.12f;
                    GUI.DrawTexture(new Rect(c.x + (i - 1) * s * 1.8f - s * 0.5f, c.y - s * 0.5f, s, s), Spark());
                }
                GUI.color = Color.white;
                return;
            }

            float stress = Mathf.Clamp01(strain / 0.6f);
            float shake = strain > 0f ? Mathf.Sin(Time.unscaledTime * 75f) * 2.2f * stress : 0f;
            // Un halo chaud derriere la platine : la flamme de ta torche, tout pres.
            GUI.color = new Color(1f, 0.6f, 0.25f, 0.18f);
            GUI.DrawTexture(new Rect(c.x - R * 1.8f, c.y - R * 1.8f, R * 3.6f, R * 3.6f), Spark());
            // La platine, puis le barillet qui tourne avec la tension.
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(c.x - R, c.y - R, R * 2f, R * 2f), Plate());
            float plugR = R * 0.54f;
            Rotated(Plug(), new Rect(c.x - plugR, c.y - plugR, plugR * 2f, plugR * 2f), turn * 90f + shake * 0.3f, c, Color.white);
            // La clef de tension : elle entre par le bas de la fente et tourne avec le barillet.
            float wr = R * 0.62f;
            Rotated(Wrench(), new Rect(c.x - wr * 0.5f, c.y + plugR * 0.1f, wr, wr), turn * 90f, c, new Color(1f, 1f, 1f, 0.95f));
            // Le crochet : il sort du trou de la serrure, pivote autour, suit la souris. Rouge quand il force trop.
            float len = R * 1.45f, thick = R * 0.12f;
            Vector2 pivot = c + new Vector2(0f, -plugR * 0.18f);
            Color steel = snap > 0f ? red : Color.Lerp(Color.white, red, stress * 0.8f);
            Rotated(Pick(), new Rect(pivot.x - thick * 0.5f, pivot.y, thick, len), 180f + pick + shake, pivot, steel);
            // Quand le crochet est tout pres du bon angle et qu'on force, une lueur d'or tremble dans la fente.
            float near = 1f - Mathf.Clamp01((Mathf.Abs(pick - sweet) - 4f) / 38f);
            if (turn > 0.05f)
            {
                GUI.color = new Color(1f, 0.8f, 0.35f, 0.35f * near * turn);
                float g = R * 0.5f;
                GUI.DrawTexture(new Rect(c.x - g, c.y - g, g * 2f, g * 2f), Spark());
            }
            // La casse : un eclat blanc.
            if (snap > 0f)
            {
                GUI.color = new Color(1f, 0.85f, 0.6f, snap);
                float g = R * (0.3f + (1f - snap) * 0.8f);
                GUI.DrawTexture(new Rect(pivot.x - g, pivot.y - g, g * 2f, g * 2f), Spark());
            }
            // Les crochets qui restent : de petites tiges d'acier, sous la serrure.
            for (int i = 0; i < 4; i++)
            {
                Color left = i < picks ? new Color(1f, 1f, 1f, 0.95f) : new Color(1f, 1f, 1f, 0.12f);
                float pw = R * 0.07f, ph = R * 0.5f;
                Rect slot = new Rect(c.x + (i - 1.5f) * pw * 2.6f - pw * 0.5f, c.y + R * 1.1f, pw, ph);
                GUI.color = left;
                GUI.DrawTexture(slot, Pick());
            }
            GUI.color = Color.white;
        }
    }

    /// <summary>
    /// UNE PORTE. Fermee et verrouillee, elle barre le passage (un BoxCollider) ; ouverte, elle tourne sur
    /// ses gonds et le collider s'en va. Concept Unity : le modele a son pivot sur les gonds, donc il
    /// suffit de changer son angle (Quaternion.Euler) pour qu'il s'ouvre comme une vraie porte.
    /// </summary>
    public class NightDoor : MonoBehaviour
    {
        public bool Locked = true;
        public bool Open { get; private set; }
        public Vector3 Center;
        Transform leaf;
        Collider block;
        float closedYaw, openYaw, t;

        public static NightDoor Build(Transform parent, Transform leaf, Vector3 center, Vector3 blockSize, float closedYaw, float openYaw)
        {
            GameObject go = new GameObject("Porte");
            go.transform.SetParent(parent, false);
            NightDoor d = go.AddComponent<NightDoor>();
            d.leaf = leaf; d.Center = center; d.closedYaw = closedYaw; d.openYaw = openYaw;
            GameObject wall = new GameObject("Barrage");
            wall.transform.SetParent(go.transform, false);
            wall.transform.position = center;
            BoxCollider box = wall.AddComponent<BoxCollider>();
            box.size = blockSize;
            d.block = box;
            return d;
        }

        public void Unlock()
        {
            Locked = false; Open = true;
            if (block != null) block.enabled = false;
            try { NightAudio.Lock(Center); } catch (System.Exception) { }
        }

        void Update()
        {
            if (!Open || t >= 1f || leaf == null) return;
            t = Mathf.MoveTowards(t, 1f, Time.deltaTime / 1.1f);
            float ease = t * t * (3f - 2f * t);
            leaf.rotation = Quaternion.Euler(0f, Mathf.Lerp(closedYaw, openYaw, ease), 0f);
        }
    }

    /// <summary>
    /// LE RODEUR. Un monstre qu'on VOIT, et qui est un peu con : il fait toujours la meme ronde, et il ne
    /// voit que ce que sa LANTERNE eclaire, devant lui. Derriere lui, a cote de lui, dans le noir : rien.
    /// Si sa lumiere te touche une demi-seconde, elle vire au rouge et il te court apres.
    ///
    ///   la ronde     il marche de repere en repere, s'arrete, tourne lentement (on a le temps de le lire) ;
    ///   le bruit     un geste rate (silex, serrure) : il vient voir ou ca a fait du bruit ;
    ///   la chasse    plus rapide que ta marche, moins que ta course ; il lache s'il te perd cinq secondes.
    ///
    /// Concept Unity : une lumiere "Spot" a un angle (spotAngle) et une portee (range). On reprend
    /// exactement ces deux nombres pour sa vue : ce que tu vois eclaire, c'est ce qu'il voit.
    /// </summary>
    public class Prowler : MonoBehaviour
    {
        const float WalkSpeed = 1.7f, ChaseSpeed = 5.1f, ViewAngle = 70f, ViewRange = 13f;      // (22/11) plus dur : il court presque aussi vite que toi
        const int Solid = ~(1 << 2);
        enum State { Round, Listen, Hunt, Lost }

        /// <summary>(15/11) Tous les Rodeurs du chateau.</summary>
        public static readonly List<Prowler> All = new List<Prowler>();
        public static void Forget() { All.Clear(); }
        public static bool AnyHunting { get { for (int i = 0; i < All.Count; i++) if (All[i] != null && All[i].Hunting) return true; return false; } }
        public static Prowler Toucher { get { for (int i = 0; i < All.Count; i++) if (All[i] != null && All[i].isActiveAndEnabled && All[i].Touching) return All[i]; return null; } }
        public static void HoldAll(bool held) { for (int i = 0; i < All.Count; i++) if (All[i] != null) All[i].Held = held; }
        public static void AllHome() { for (int i = 0; i < All.Count; i++) if (All[i] != null) All[i].SendHome(); }
        public static void AllOff() { for (int i = 0; i < All.Count; i++) if (All[i] != null) All[i].gameObject.SetActive(false); }
        NightPlayer prey;
        Vector3[] round;
        int next;
        State state;
        float wait, seen, unseen, step;
        // (16/11) Imprevisible : il va ou il veut, se retourne d'un coup, eteint sa lanterne.
        Vector3 roam;
        bool roaming;
        float spin, dark;
        Vector3 noise;
        Light lantern;
        Material skin;

        /// <summary>Tenu par la mise en scene (la prise) : il ne decide plus de rien.</summary>
        public bool Held;
        public bool Hunting { get { return state == State.Hunt; } }
        public bool Touching { get { return prey != null && Flat(prey.transform.position - transform.position).magnitude < 1.1f; } }

        public static Prowler Spawn(Transform parent, Vector3[] round, NightPlayer prey)
        {
            GameObject go = new GameObject("LE RÔDEUR");
            go.transform.SetParent(parent, false);
            go.transform.position = round[0];
            Prowler p = go.AddComponent<Prowler>();
            p.prey = prey; p.round = round; p.next = 1 % round.Length;
            GameObject model = NightKit.Model("rodeur", go.transform);
            if (model != null)
            {
                Renderer r = model.GetComponentInChildren<Renderer>();
                if (r != null && r.sharedMaterial != null)
                {
                    p.skin = NightKit.Shadow();
                    if (p.skin == null) p.skin = new Material(r.sharedMaterial);
                    foreach (Renderer each in model.GetComponentsInChildren<Renderer>(true)) each.sharedMaterial = p.skin;
                    p.skin.color = new Color(0.010f, 0.010f, 0.013f);       // une silhouette, pas une statue
                    NightKit.Smoke(go.transform, 2.7f);
                }
            }
            // Sa lanterne : la ou le modele la tient (main droite, devant lui), tournee vers l'avant et un peu vers le sol.
            GameObject lamp = new GameObject("Lanterne");
            lamp.transform.SetParent(go.transform, false);
            lamp.transform.localPosition = new Vector3(-0.30f, 1.36f, 0.78f);
            lamp.transform.localRotation = Quaternion.Euler(9f, 0f, 0f);
            p.lantern = lamp.AddComponent<Light>();
            p.lantern.type = LightType.Spot;
            p.lantern.spotAngle = ViewAngle;
            p.lantern.range = ViewRange + 1f;
            p.lantern.intensity = 2.6f;
            p.lantern.shadows = LightShadows.Soft;
            p.lantern.shadowStrength = 0.9f;
            p.Tint(false);
            All.Add(p);
            return p;
        }

        /// <summary>Un bruit a cet endroit : s'il fait sa ronde et qu'il n'est pas trop loin, il vient voir.</summary>
        public static void Hear(Vector3 at)
        {
            for (int i = 0; i < All.Count; i++)
            {
                Prowler p = All[i];
                if (p == null || p.state == State.Hunt) continue;
                if (Flat(at - p.transform.position).magnitude > 26f) continue;
                p.noise = at; p.state = State.Listen; p.wait = 0f;
            }
        }

        /// <summary>(21/11) Un coup de torche en pleine face : il recule, souffle sa lanterne, et perd ta trace.</summary>
        public void Burn(Vector3 from)
        {
            Vector3 away = Flat(transform.position - from);
            if (away.sqrMagnitude < 0.01f) away = -transform.forward;
            Vector3 to = transform.position + away.normalized * 2.5f;
            to.y = NightMap.FloorY(to);
            transform.position = to;
            state = State.Lost; wait = 4f; seen = 0f;
            Douse(3f);
            try { NightAudio.Bang(transform.position + Vector3.up, 0.9f); } catch (System.Exception) { }
            NightAudio.Scream(transform.position + Vector3.up * 1.6f, 0.7f);
        }

        public void SendHome()
        {
            transform.position = round[0];
            next = 1 % round.Length; state = State.Round; roaming = false; seen = unseen = wait = 0f;
            Tint(false);
        }

        void Tint(bool angry)
        {
            Color c = angry ? new Color(1f, 0.16f, 0.08f) : new Color(0.45f, 1f, 0.6f);
            if (lantern != null) lantern.color = c;
            if (skin != null) skin.SetColor("_EmissionColor", c * 2.2f);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        /// <summary>Il souffle sa lanterne quelques secondes : on ne le voit plus, on l'entend marcher.</summary>
        void Douse(float seconds)
        {
            dark = seconds;
            if (lantern != null) lantern.enabled = false;
            if (skin != null) skin.SetColor("_EmissionColor", Color.black);
        }

        /// <summary>Sa lanterne t'eclaire-t-elle ? Dans le cone, a portee, et rien entre vous deux.</summary>
        bool Sees()
        {
            if (prey == null || lantern == null) return false;
            if (dark > 0f)
            {
                // Lanterne eteinte : il ne voit plus rien. Mais il ECOUTE : courir pres de lui, c'est se trahir.
                Vector3 gap = prey.transform.position - transform.position;
                gap.y = 0f;
                return gap.magnitude < 8f && FiefInput.SprintHeld && FiefInput.Move.sqrMagnitude > 0.01f;
            }
            Vector3 eye = lantern.transform.position;
            Vector3 body = prey.transform.position + Vector3.up * 1.2f;
            Vector3 to = body - eye;
            if (to.magnitude > ViewRange) return false;
            if (Vector3.Angle(lantern.transform.forward, to) > ViewAngle * 0.5f) return false;
            return !Physics.Linecast(eye, body, Solid, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Avance vers "goal" (en passant par les carrefours du niveau si un mur barre). Renvoie false s'il ne peut pas.</summary>
        bool Go(Vector3 goal, float speed, float turn)
        {
            bool ok;
            Vector3 target = NightMap.Toward(transform.position, goal, out ok);
            if (!ok) return false;
            Vector3 step3 = Flat(target - transform.position);
            if (step3.magnitude < 0.05f) return true;
            Quaternion face = Quaternion.LookRotation(step3.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, face, turn * Time.deltaTime);
            // Il n'avance que s'il regarde a peu pres ou il va : ses demi-tours prennent du temps, et c'est voulu.
            if (Quaternion.Angle(transform.rotation, face) < 35f)
            {
                Vector3 next = transform.position + step3.normalized * Mathf.Min(speed * Time.deltaTime, step3.magnitude);
                // (21/11) Ta torche le tient a distance tant qu'elle brule fort ; les feux allumes, il ne les passe pas.
                if (prey != null && Faith.Value > 0.4f && Flat(prey.transform.position - next).magnitude < 2.6f && Flat(prey.transform.position - transform.position).magnitude >= 2.6f) next = transform.position;
                next = NightWard.Hold(transform.position, next);
                next.y = NightMap.FloorY(next);
                transform.position = next;
            }
            step -= Time.deltaTime * speed;
            if (step <= 0f) { step = 1.05f; try { NightAudio.Step(transform.position, 0.9f); } catch (System.Exception) { } }
            return true;
        }

        void Update()
        {
            if (prey == null || Held) return;
            float dt = Time.deltaTime;
            if (dark > 0f)
            {
                dark -= dt;
                if (dark <= 0f && lantern != null) { lantern.enabled = true; Tint(state == State.Hunt); }
            }
            bool sees = Sees();
            seen = sees ? seen + dt : Mathf.Max(0f, seen - dt * 2f);

            if (state != State.Hunt && seen > 0.5f)
            {
                state = State.Hunt; unseen = 0f;
                Tint(true);
                try { NightAudio.Roar(transform.position, 0.8f); } catch (System.Exception) { }
                NightAudio.Sting(0.8f);
            }

            if (state == State.Hunt)
            {
                unseen = sees ? 0f : unseen + dt;
                bool can = Go(prey.transform.position, ChaseSpeed, 300f);
                if (!can || unseen > 5f) { state = State.Lost; wait = 3f; Tint(false); }
            }
            else if (state == State.Lost)
            {
                // Il t'a perdu : il tourne sur place, lentement, puis reprend sa ronde.
                transform.Rotate(0f, 55f * dt, 0f);
                wait -= dt;
                if (wait <= 0f) state = State.Round;
            }
            else if (state == State.Listen)
            {
                if (!Go(noise, WalkSpeed * 1.5f, 140f) || Flat(noise - transform.position).magnitude < 2.2f)
                { state = State.Lost; wait = 3.5f; }
            }
            else
            {
                if (wait > 0f)
                {
                    wait -= dt;
                    if (spin != 0f) transform.Rotate(0f, spin * dt, 0f);
                    return;
                }
                spin = 0f;
                // Plus de ronde : a chaque arrivee, il choisit n'importe quel autre endroit du chateau.
                if (!roaming) { roam = NightMap.RandomCell(transform.position, 8f, 46f); roaming = true; }
                bool can = Go(roam, WalkSpeed, 80f);
                if (!can || Flat(roam - transform.position).magnitude < 0.6f)
                {
                    roaming = false;
                    wait = Random.Range(0.3f, 3.5f);
                    float dice = Random.value;
                    if (dice < 0.3f) spin = Random.value < 0.5f ? 150f : -150f;        // il regarde autour de lui
                    else if (dice < 0.5f) Douse(Random.Range(4f, 9f));                  // il eteint sa lanterne
                }
                else if (Random.value < dt * 0.05f) { wait = 1.4f; spin = 190f; }         // en pleine marche : il se retourne
            }
        }
    }
}
