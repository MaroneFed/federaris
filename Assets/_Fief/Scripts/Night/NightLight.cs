using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA LUMIERE (21/11). Martin : "je veux qu'on ait une torche qu'on tienne en main, qu'elle s'agite un peu, que ca fasse
    /// style ; pour empecher les monstres, sauf que la torche s'abaisse au fur et a mesure ; on la reprend dans un feu ;
    /// on allume les bougies, les petites torches dans les couloirs, comme ca les monstres ne peuvent pas passer, mais ca
    /// reste allume pendant quelque temps, ca se consume, on voit la bougie blanche se consumer".
    ///
    ///   Faith (NightSurvival.cs)  la reserve de ta torche, de 0 (eteinte) a 1 (pleine). Elle baisse toute seule.
    ///   HeldTorch                 la torche dans ta main droite : elle suit ton regard avec un peu de retard, se balance
    ///                             quand tu marches, frappe (clic gauche) ; sa flamme a la taille de ta reserve.
    ///   Ward                      un feu qu'on allume avec sa torche (E) : une torche murale, un bouquet de bougies, un
    ///                             brasero. Il brule un temps, se consume a vue d'oeil, puis s'eteint. Tant qu'il brule,
    ///                             AUCUN monstre ne passe a cote (NightMap.Passable, Ward.Hold).
    ///   NightLight                ce qui fabrique les feux du chateau (NightMap les pose).
    /// </summary>
    public static class NightLight
    {
        static Transform parent;

        public static void Clear() { Ward.All.Clear(); parent = null; }

        /// <summary>Une torche de fer sur un mur : "d" est la direction du mur vu du milieu de la case (0 nord, 1 est, 2 sud, 3 ouest).</summary>
        public static Ward WallTorch(Transform under, float x, float z, int d, float floor)
        {
            float yaw = d == 0 ? 180f : d == 1 ? -90f : d == 2 ? 0f : 90f;
            NightKit.FireAtY("Torche_Murale_Feu", x, z, yaw, false, floor);
            GameObject fire = NightKit.LastFire;
            if (fire == null) return null;
            Vector3 face = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Ward w = Ward.Make(under, Ward.Kind.Torch, new Vector3(x, floor, z) + face * 0.9f, 150f);
            w.UseFire(fire);
            return w;
        }

        /// <summary>Un brasero sur pied : il brule longtemps et repousse loin.</summary>
        public static Ward Brazier(Transform under, Vector3 at)
        {
            at.y = NightMap.FloorY(at);
            NightKit.FireAtY("Brasero_Feu", at.x, at.z, 0f, false, at.y);
            GameObject fire = NightKit.LastFire;
            if (fire == null) return null;
            Ward w = Ward.Make(under, Ward.Kind.Brazier, at, 260f);
            w.UseFire(fire);
            BoxCollider box = new GameObject("Brasero").AddComponent<BoxCollider>();
            box.transform.SetParent(w.transform, false);
            box.transform.position = at + Vector3.up * 0.6f;
            box.size = new Vector3(0.8f, 1.2f, 0.8f);
            return w;
        }

        /// <summary>
        /// Trois bougies blanches dans une coupelle de fer, posees au sol. "placed" : celles que tu poses toi-meme (objet 3),
        /// allumees tout de suite. Elles fondent a vue d'oeil.
        /// </summary>
        public static Ward Candles(Transform under, Vector3 at, float life, bool placed)
        {
            at.y = NightMap.FloorY(at);
            Ward w = Ward.Make(under, Ward.Kind.Candles, at, life);
            w.BuildCandles(false);
            if (placed) w.Ignite();
            return w;
        }

        /// <summary>(21/11) LE CIERGE ETERNEL, la recompense de l'ossuaire : une haute bougie qui ne meurt jamais.</summary>
        public static Ward Eternal(Transform under, Vector3 at)
        {
            at.y = NightMap.FloorY(at);
            Ward w = Ward.Make(under, Ward.Kind.Eternal, at, 9999f);
            w.BuildCandles(true);
            w.Ignite();
            return w;
        }

        /// <summary>Le parent des feux poses en cours de partie.</summary>
        public static Transform Root
        {
            get
            {
                if (parent == null) parent = new GameObject("Feux posés").transform;
                return parent;
            }
        }
    }

    /// <summary>Un feu a allumer : torche murale, bougies, brasero, cierge eternel.</summary>
    public class Ward : MonoBehaviour
    {
        public enum Kind { Torch, Candles, Brazier, Eternal }
        public static readonly List<Ward> All = new List<Ward>();

        public Kind kind;
        /// <summary>Le point au sol que les monstres ne passent pas.</summary>
        public Vector3 Center;
        public float Life;
        public float Burn;
        public bool Lit;
        public bool Burning { get { return Lit && (kind == Kind.Eternal || Burn > 0f); } }
        /// <summary>Une bougie fondue ne se rallume plus ; une torche, si (il reste de la poix).</summary>
        public bool Spent { get { return kind == Kind.Candles && Lit && Burn <= 0f; } }
        public float Radius { get { return kind == Kind.Brazier ? 4.2f : kind == Kind.Torch ? 3.3f : kind == Kind.Eternal ? 3.8f : 2.7f; } }
        /// <summary>De 1 (neuf) a 0 (mort).</summary>
        public float Left { get { return kind == Kind.Eternal ? 1f : Mathf.Clamp01(Burn / Life); } }

        GameObject fire;
        Light glow;
        float range0;
        readonly List<Transform> waxes = new List<Transform>();
        readonly List<float> heights = new List<float>();
        readonly List<Transform> flames = new List<Transform>();
        float seed;

        public static Ward Make(Transform under, Kind kind, Vector3 center, float life)
        {
            GameObject go = new GameObject(kind == Kind.Torch ? "Torche à allumer" : kind == Kind.Brazier ? "Brasero à allumer" : kind == Kind.Eternal ? "Cierge éternel" : "Bougies à allumer");
            go.transform.SetParent(under != null ? under : NightLight.Root, false);
            go.transform.position = center;
            Ward w = go.AddComponent<Ward>();
            w.kind = kind; w.Center = center; w.Life = life; w.Burn = life;
            w.seed = center.x * 1.3f + center.z * 2.1f;
            All.Add(w);
            return w;
        }

        void OnDestroy() { All.Remove(this); }

        /// <summary>Un feu du kit (son "Feu", eteint) devient celui de ce Ward.</summary>
        public void UseFire(GameObject kitFire)
        {
            fire = kitFire;
            glow = fire.GetComponentInChildren<Light>(true);
            if (glow != null) range0 = glow.range;
            fire.SetActive(false);
        }

        /// <summary>Les bougies : une coupelle de fer et trois (ou une grande) colonnes de cire blanche, chacune sa flamme.</summary>
        public void BuildCandles(bool eternal)
        {
            Material wax = MaterialFactory.GetShiny(eternal ? new Color(0.98f, 0.93f, 0.78f) : new Color(0.93f, 0.9f, 0.82f), 0.35f, 0f);
            Material iron = NightKit.Mat("Fer");
            Proto.BeginVisualOnly();
            GameObject dish = Proto.Cylinder(transform, new Vector3(0f, 0.03f, 0f), new Vector3(eternal ? 0.5f : 0.42f, 0.03f, eternal ? 0.5f : 0.42f), new Color(0.1f, 0.1f, 0.1f), "Coupelle");
            if (iron != null) dish.GetComponent<Renderer>().sharedMaterial = iron;
            Vector3[] spots = eternal ? new[] { Vector3.zero } : new[] { new Vector3(-0.08f, 0f, 0.05f), new Vector3(0.09f, 0f, 0.07f), new Vector3(0.01f, 0f, -0.1f) };
            float[] tall = eternal ? new[] { 0.95f } : new[] { 0.42f, 0.3f, 0.22f };
            for (int i = 0; i < spots.Length; i++)
            {
                float r = eternal ? 0.11f : 0.055f + 0.01f * (i % 2);
                GameObject col = Proto.Cylinder(transform, spots[i] + new Vector3(0f, 0.06f + tall[i] * 0.5f, 0f), new Vector3(r, tall[i] * 0.5f, r), Color.white, "Cire");
                col.GetComponent<Renderer>().sharedMaterial = wax;
                waxes.Add(col.transform);
                heights.Add(tall[i]);
                // Une coulure de cire figee le long de la colonne.
                GameObject drip = Proto.Sphere(col.transform, new Vector3(0.9f, 0.35f, 0f), new Vector3(0.35f, 0.25f, 0.35f), Color.white, "Coulure");
                drip.GetComponent<Renderer>().sharedMaterial = wax;
            }
            Proto.EndVisualOnly();
            for (int i = 0; i < waxes.Count; i++)
            {
                Transform holder = new GameObject("Flamme").transform;
                holder.SetParent(transform, false);
                GameObject f = NightKit.LooseFlame("Cierge_Feu", holder);
                if (f != null) f.transform.localScale = Vector3.one * (eternal ? 1.1f : 0.6f);
                holder.gameObject.SetActive(false);
                flames.Add(holder);
            }
            glow = new GameObject("Lueur").AddComponent<Light>();
            glow.transform.SetParent(transform, false);
            glow.transform.localPosition = new Vector3(0f, eternal ? 1.25f : 0.62f, 0f);
            glow.type = LightType.Point;
            glow.color = eternal ? new Color(1f, 0.86f, 0.6f) : new Color(1f, 0.7f, 0.42f);
            range0 = eternal ? 9f : 6f;
            glow.range = range0;
            glow.intensity = 0f;
            glow.enabled = false;
            Place();
        }

        public bool Ignite()
        {
            if (Burning || Spent) return false;
            Lit = true;
            if (Burn <= 0f) Burn = Life * 0.6f;      // une torche rallumee : il ne reste qu'un peu de poix
            if (fire != null) fire.SetActive(true);
            foreach (Transform f in flames) f.gameObject.SetActive(true);
            if (glow != null) glow.enabled = true;
            try { Sfx.BellAt(transform.position); } catch (System.Exception) { }
            return true;
        }

        /// <summary>On y reprend du feu pour sa torche : ce qu'on prend, il le perd.</summary>
        public float Take(float amount)
        {
            if (!Burning || kind == Kind.Eternal) return Burning ? amount : 0f;
            float seconds = amount * 160f;
            float got = Mathf.Min(seconds, Burn) / 160f;
            Burn -= seconds;
            return got;
        }

        /// <summary>Un monstre (le Demon) souffle ce feu.</summary>
        public void Snuff()
        {
            if (!Burning || kind == Kind.Eternal) return;
            Burn = 0f;
            Out();
        }

        void Out()
        {
            if (fire != null) fire.SetActive(false);
            foreach (Transform f in flames) f.gameObject.SetActive(false);
            if (glow != null) glow.enabled = false;
            try { Sfx.ChipAt(transform.position); } catch (System.Exception) { }
        }

        /// <summary>La cire fond : chaque colonne raccourcit, sa flamme descend avec elle.</summary>
        void Place()
        {
            float k = Left;
            for (int i = 0; i < waxes.Count; i++)
            {
                float h = Mathf.Max(0.015f, heights[i] * (0.08f + 0.92f * k));
                Transform col = waxes[i];
                Vector3 s = col.localScale;
                col.localScale = new Vector3(s.x, h * 0.5f, s.z);
                col.localPosition = new Vector3(col.localPosition.x, 0.06f + h * 0.5f, col.localPosition.z);
                if (i < flames.Count) flames[i].localPosition = new Vector3(col.localPosition.x, 0.06f + h + 0.005f, col.localPosition.z);
            }
        }

        void Update()
        {
            if (!Lit) return;
            if (kind != Kind.Eternal && Burn > 0f)
            {
                Burn -= Time.deltaTime;
                if (Burn <= 0f) { Burn = 0f; Out(); if (kind != Kind.Candles) Lit = false; }
            }
            if (!Burning) return;
            float k = Left;
            if (waxes.Count > 0) Place();
            if (fire != null) fire.transform.localScale = Vector3.one * (0.45f + 0.55f * k);
            if (glow != null)
            {
                glow.range = range0 * (0.45f + 0.55f * k);
                if (waxes.Count > 0)
                {
                    float t = Time.time * 1.1f + seed;
                    glow.intensity = (kind == Kind.Eternal ? 1.4f : 1.05f) * (0.85f + 0.15f * Mathf.PerlinNoise(t * 7f, seed)) * (0.5f + 0.5f * k);
                }
            }
        }

        // ------------------------------------------------------------------ les regles

        static float Flat(Vector3 a, Vector3 b) { a.y = 0f; b.y = 0f; return (a - b).magnitude; }

        /// <summary>Un feu brule-t-il assez pres de ce point pour qu'un monstre n'y passe pas ?</summary>
        public static bool Blocks(Vector3 p)
        {
            for (int i = 0; i < All.Count; i++)
            {
                Ward w = All[i];
                if (w != null && w.Burning && Mathf.Abs(w.Center.y - p.y) < 3f && Flat(w.Center, p) < w.Radius) return true;
            }
            return false;
        }

        static Ward Closest(Vector3 p, bool burning, float within)
        {
            Ward best = null;
            float bestD = within;
            for (int i = 0; i < All.Count; i++)
            {
                Ward w = All[i];
                if (w == null || w.Burning != burning || w.Spent || Mathf.Abs(w.transform.position.y - p.y) > 2.5f) continue;
                float d = Flat(w.transform.position, p);
                if (d < bestD) { bestD = d; best = w; }
            }
            return best;
        }

        public static Ward NearestUnlit(Vector3 p) { return Closest(p, false, 2.3f); }
        public static Ward NearestBurning(Vector3 p, float within) { return Closest(p, true, within); }

        /// <summary>
        /// Un monstre voudrait aller de "from" a "to" : s'il entrerait dans la lumiere d'un feu, il reste ou il est ; s'il y est
        /// deja (on vient d'allumer a cote de lui), il en sort, a reculons.
        /// </summary>
        public static Vector3 Hold(Vector3 from, Vector3 to)
        {
            for (int i = 0; i < All.Count; i++)
            {
                Ward w = All[i];
                if (w == null || !w.Burning || Mathf.Abs(w.Center.y - from.y) > 3f) continue;
                float r = w.Radius;
                float dFrom = Flat(w.Center, from), dTo = Flat(w.Center, to);
                if (dFrom < r)
                {
                    Vector3 away = from - w.Center;
                    away.y = 0f;
                    if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
                    return from + away.normalized * Mathf.Max(0.02f, (to - from).magnitude);
                }
                if (dTo < r) return from;
            }
            return to;
        }
    }

    /// <summary>
    /// LA TORCHE EN MAIN. Un baton de bois sombre, une bague de fer, des chiffons enroules, et le feu du kit par-dessus.
    /// Elle n'est pas collee a l'image : elle suit le regard avec un peu de retard (un ressort), se balance a chaque pas,
    /// respire quand on ne bouge pas, et part en arc quand on frappe. Sa flamme a la taille de ta reserve.
    ///
    /// Concept Unity : un objet enfant de la camera bouge avec elle ; on ajoute par-dessus un petit decalage calcule a
    /// chaque image (le "sway" des jeux a la premiere personne).
    /// </summary>
    public class HeldTorch : MonoBehaviour
    {
        Transform rig;
        Transform flame;
        Vector3 home = new Vector3(0.30f, -0.40f, 0.55f);
        Vector2 lag, lagSpeed;
        Quaternion lastLook;
        float swing = -1f, raise = 1f, phase;
        public static bool Shown = true;
        /// <summary>De 0 (cachee, sous l'ecran) a 1 (en main). L'ouverture la fait monter quand le mourant te la donne.</summary>
        public static float Raise = 1f;

        public static HeldTorch Build(Camera eye)
        {
            GameObject go = new GameObject("Torche en main");
            go.transform.SetParent(eye.transform, false);
            HeldTorch t = go.AddComponent<HeldTorch>();
            t.rig = new GameObject("Bras").transform;
            t.rig.SetParent(go.transform, false);
            Material wood = MaterialFactory.GetShiny(new Color(0.17f, 0.1f, 0.055f), 0.18f, 0f);
            Material rag = MaterialFactory.GetShiny(new Color(0.09f, 0.06f, 0.035f), 0.05f, 0f);
            Material iron = NightKit.Mat("Fer");
            Proto.BeginVisualOnly();
            GameObject stick = Proto.Cylinder(t.rig, new Vector3(0f, -0.05f, 0f), new Vector3(0.042f, 0.26f, 0.042f), Color.white, "Manche");
            stick.GetComponent<Renderer>().sharedMaterial = wood;
            GameObject ring = Proto.Cylinder(t.rig, new Vector3(0f, 0.19f, 0f), new Vector3(0.058f, 0.025f, 0.058f), Color.white, "Bague");
            ring.GetComponent<Renderer>().sharedMaterial = iron != null ? iron : wood;
            GameObject ring2 = Proto.Cylinder(t.rig, new Vector3(0f, -0.2f, 0f), new Vector3(0.05f, 0.02f, 0.05f), Color.white, "Bague basse");
            ring2.GetComponent<Renderer>().sharedMaterial = iron != null ? iron : wood;
            for (int i = 0; i < 4; i++)
            {
                GameObject wrap = Proto.Cylinder(t.rig, new Vector3(0f, 0.235f + i * 0.035f, 0f), new Vector3(0.078f - i * 0.006f, 0.022f, 0.078f - i * 0.006f), Color.white, "Chiffon");
                wrap.transform.localRotation = Quaternion.Euler(i * 7f - 9f, i * 40f, i * 5f);
                wrap.GetComponent<Renderer>().sharedMaterial = rag;
            }
            Proto.EndVisualOnly();
            foreach (Renderer r in t.rig.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Transform holder = new GameObject("Feu").transform;
            holder.SetParent(t.rig, false);
            holder.localPosition = new Vector3(0f, 0.33f, 0f);
            GameObject fire = NightKit.LooseFlame("Torche_Murale_Feu", holder);
            if (fire != null)
            {
                // Sa lumiere a lui : celle du chevalier suffit (NightPlayer.torch).
                foreach (Light l in fire.GetComponentsInChildren<Light>(true)) Object.Destroy(l.gameObject);
                fire.transform.localScale = Vector3.one * 0.7f;
            }
            t.flame = holder;
            t.rig.localPosition = t.home;
            t.lastLook = eye.transform.rotation;
            return t;
        }

        /// <summary>Un coup de torche : l'arc part de la droite vers la gauche.</summary>
        public void Swing() { if (swing < 0f) swing = 0f; }
        public bool Swinging { get { return swing >= 0f; } }
        /// <summary>Le moment du coup ou la flamme passe devant toi (le coup touche a cet instant).</summary>
        public bool Striking { get { return swing > 0.25f && swing < 0.45f; } }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            Transform cam = transform.parent;
            if (cam == null) return;
            // Le retard : de combien le regard a tourne depuis l'image d'avant ? La torche reste un peu en arriere.
            Quaternion look = cam.rotation;
            Quaternion delta = Quaternion.Inverse(lastLook) * look;
            lastLook = look;
            Vector3 e = delta.eulerAngles;
            float yawD = Mathf.DeltaAngle(0f, e.y), pitchD = Mathf.DeltaAngle(0f, e.x);
            Vector2 target = new Vector2(Mathf.Clamp(-yawD * 0.012f, -0.09f, 0.09f), Mathf.Clamp(pitchD * 0.010f, -0.07f, 0.07f));
            lag = Vector2.SmoothDamp(lag, target, ref lagSpeed, 0.09f, Mathf.Infinity, Mathf.Max(dt, 0.0001f));

            // Le pas : la torche monte et descend, de gauche a droite.
            Vector2 move = NightGames.Busy ? Vector2.zero : FiefInput.Move;
            float pace = move.sqrMagnitude > 0.01f ? (FiefInput.SprintHeld ? 13f : 8.5f) : 1.6f;
            phase += dt * pace;
            float amp = move.sqrMagnitude > 0.01f ? (FiefInput.SprintHeld ? 1.6f : 1f) : 0.35f;
            Vector3 bob = new Vector3(Mathf.Cos(phase * 0.5f) * 0.014f, -Mathf.Abs(Mathf.Sin(phase * 0.5f)) * 0.018f, 0f) * amp;

            raise = Mathf.MoveTowards(raise, Shown ? Raise : 0f, dt * 2.5f);
            Vector3 pos = home + new Vector3(lag.x, lag.y, 0f) + bob + Vector3.down * (1f - raise) * 0.6f;
            Quaternion rot = Quaternion.Euler(-8f + lag.y * 120f, -6f, -14f - lag.x * 160f + Mathf.Sin(phase * 0.5f) * 2.5f * amp);

            if (swing >= 0f)
            {
                swing += dt / 0.5f;
                float s = Mathf.Sin(Mathf.Clamp01(swing) * Mathf.PI);
                pos += new Vector3(-0.32f * s, 0.06f * s, 0.18f * s);
                rot = rot * Quaternion.Euler(-45f * s, 0f, 55f * s);
                if (swing >= 1f) swing = -1f;
            }
            rig.localPosition = pos;
            rig.localRotation = rot;

            // La flamme : grande quand la reserve est pleine, une braise quand elle est vide.
            float fuel = Faith.Value;
            if (flame != null)
            {
                bool on = fuel > 0.001f && raise > 0.05f;
                if (flame.gameObject.activeSelf != on) flame.gameObject.SetActive(on);
                flame.localScale = Vector3.one * (0.3f + 0.75f * fuel);
            }
        }
    }
}
