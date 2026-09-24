using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES AUTELS : trois lieux qui FORCENT le combat, au pied du chateau (Martin,
    /// 25/09 : "a cote du chateau, des monuments qui, une fois controles, te
    /// paient avec de l'or ou du bois -- sans que ca devienne cheate").
    ///
    ///   Autel de l'Or        a l'ouest    5 or toutes les 40 s
    ///   Autel du Bucheron    a l'est      4 bois mort dans ta stele toutes les 40 s
    ///   Autel de la Lune     au nord      2 pierres-lune dans ta stele toutes les 40 s
    ///
    /// LE PRENDRE : se tenir sur le cercle de pierres 10 secondes, SEUL. Deux
    /// chercheurs dessus : personne n'avance. Un autre que son maitre : il le
    /// neutralise d'abord, puis le prend. Et deux REVENANTS le gardent : tant qu'ils
    /// tiennent debout pres de la pierre, rien ne bouge.
    ///
    /// Sur 30 minutes, un autel rapporte environ 200 or, ou 170 bois mort, ou 85
    /// pierres-lune -- s'il n'est jamais repris. Il le sera : il est a trente pas du
    /// chemin de tout le monde.
    /// </summary>
    public class Monument : MonoBehaviour
    {
        public static readonly List<Monument> All = new List<Monument>();

        public enum Kind { Or, Bucheron, Lune }

        const float Radius = 5f;
        const float CaptureSeconds = 10f;
        const float PayEvery = 40f;

        /// <summary>Ou ils se dressent : hors des murs, loin de l'allee des rois (sud) et de la breche (est).</summary>
        static readonly Vector2[] Spots = { new Vector2(-70f, 8f), new Vector2(68f, -26f), new Vector2(-22f, 70f) };
        static readonly Kind[] Kinds = { Kind.Or, Kind.Bucheron, Kind.Lune };

        public Kind kind;
        [System.NonSerialized] public Seeker Owner;
        [System.NonSerialized] public Seeker Taker;
        public float Progress;
        readonly List<Beast> guardians = new List<Beast>();
        float payTimer = PayEvery;
        Renderer[] flags = new Renderer[0];
        Renderer bowl;
        Light fire;
        bool playerInside;

        public static Color Tint(Kind k)
        {
            return k == Kind.Or ? new Color(0.95f, 0.76f, 0.3f) : k == Kind.Bucheron ? new Color(0.72f, 0.52f, 0.3f) : new Color(0.55f, 0.72f, 1f);
        }

        public static string Name(Kind k)
        {
            return k == Kind.Or ? "Autel de l'Or" : k == Kind.Bucheron ? "Autel du Bucheron" : "Autel de la Lune";
        }

        static string Gift(Kind k)
        {
            return k == Kind.Or ? "5 or" : k == Kind.Bucheron ? "4 bois mort dans ta stele" : "2 pierres-lune dans ta stele";
        }

        /// <summary>La foret, les steles, les creux laissent la place aux autels.</summary>
        public static bool Near(float x, float z, float margin)
        {
            for (int i = 0; i < Spots.Length; i++)
                if ((Spots[i] - new Vector2(x, z)).magnitude < 11f + margin) return true;
            return false;
        }

        // ================================================================== construction

        public static void BuildAll(Transform parent)
        {
            GameObject root = new GameObject("AUTELS");
            root.transform.SetParent(parent, false);
            for (int i = 0; i < Spots.Length; i++)
            {
                Vector3 at = Ground.Place(Spots[i].x, Spots[i].y, 0f);
                Build(root.transform, at, Kinds[i], i);
            }
        }

        static void Build(Transform parent, Vector3 at, Kind kind, int index)
        {
            GameObject go = new GameObject(Name(kind).ToUpperInvariant());
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            Monument m = go.AddComponent<Monument>();
            m.kind = kind;
            Transform t = go.transform;
            Color stone = new Color(0.27f, 0.27f, 0.28f);
            Color dark = new Color(0.18f, 0.18f, 0.19f);
            Color tint = Tint(kind);

            // Le cercle : une dalle ronde, huit pierres levees de hauteurs inegales.
            Proto.BeginVisualOnly();
            Proto.Cylinder(t, new Vector3(0f, 0.04f, 0f), new Vector3(Radius * 2f, 0.06f, Radius * 2f), dark, "Dalle");
            Proto.Cylinder(t, new Vector3(0f, 0.07f, 0f), new Vector3(Radius * 1.6f, 0.04f, Radius * 1.6f), stone, "Dalle");
            // Des runes au sol, qui s'allument a la couleur du maitre.
            List<Renderer> marks = new List<Renderer>();
            for (int k = 0; k < 12; k++)
            {
                float a = k / 12f * Mathf.PI * 2f;
                GameObject rune = Proto.Cube(t, new Vector3(Mathf.Cos(a) * (Radius - 0.6f), 0.1f, Mathf.Sin(a) * (Radius - 0.6f)),
                                             new Vector3(0.18f, 0.02f, 0.5f), Palette.Shade(stone, 0.7f), "Rune");
                rune.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                marks.Add(rune.GetComponent<Renderer>());
            }
            Proto.EndVisualOnly();
            for (int k = 0; k < 8; k++)
            {
                float a = k / 8f * Mathf.PI * 2f + 0.2f;
                float h = 1.6f + Mathf.Abs(Mathf.Sin(k * 2.3f + index)) * 1.4f;
                GameObject s = Proto.Cube(t, new Vector3(Mathf.Cos(a) * (Radius + 0.4f), h * 0.5f - 0.1f, Mathf.Sin(a) * (Radius + 0.4f)),
                                          new Vector3(0.6f, h, 0.4f), stone, "Pierre levee");
                s.transform.localRotation = Quaternion.Euler(Mathf.Sin(k * 1.7f) * 5f, -a * Mathf.Rad2Deg, Mathf.Cos(k) * 4f);
            }
            // L'autel : un bloc, une vasque, un feu qui prend la couleur du maitre.
            Proto.Cube(t, new Vector3(0f, 0.5f, 0f), new Vector3(1.4f, 1f, 0.9f), stone, "Autel");
            Proto.BeginVisualOnly();
            Proto.Cube(t, new Vector3(0f, 1.05f, 0f), new Vector3(1.6f, 0.12f, 1.1f), dark, "Table");
            GameObject bowlGo = Proto.Cylinder(t, new Vector3(0f, 1.2f, 0f), new Vector3(0.7f, 0.1f, 0.7f), Palette.Shade(tint, 0.5f), "Vasque");
            m.bowl = bowlGo.GetComponent<Renderer>();
            // Le symbole de ce qu'il donne, grave sur la face.
            Symbol(t, kind, tint);
            // Deux oriflammes, blanches tant que personne ne le tient.
            List<Renderer> banners = new List<Renderer>();
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 p = new Vector3(side * 2.6f, 0f, -1.4f);
                Proto.Cube(t, p + new Vector3(0f, 1.8f, 0f), new Vector3(0.08f, 3.6f, 0.08f), new Color(0.25f, 0.19f, 0.13f), "Hampe");
                GameObject flag = Proto.Cube(t, p + new Vector3(0.36f, 3.0f, 0f), new Vector3(0.7f, 1.0f, 0.03f), new Color(0.7f, 0.68f, 0.62f), "Oriflamme");
                flag.AddComponent<Flutter>();
                banners.Add(flag.GetComponent<Renderer>());
            }
            banners.AddRange(marks);
            m.flags = banners.ToArray();
            Proto.EndVisualOnly();

            GameObject lightGo = new GameObject("Feu de l'autel");
            lightGo.transform.SetParent(t, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            m.fire = lightGo.AddComponent<Light>();
            m.fire.type = LightType.Point;
            m.fire.range = 9f;
            m.fire.intensity = 0.6f;
            m.fire.color = tint;
            m.fire.shadows = LightShadows.None;
            lightGo.AddComponent<LampFlicker>();

            // Les gardiens : deux revenants, a quelques pas.
            for (int g = 0; g < 2; g++)
            {
                float a = g * Mathf.PI + 0.7f;
                Vector3 p = Ground.Place(at.x + Mathf.Cos(a) * 3.5f, at.z + Mathf.Sin(a) * 3.5f, 0.2f);
                m.guardians.Add(Beast.Revenant(parent, p, index * 17 + g));
            }
            All.Add(m);
        }

        static void Symbol(Transform t, Kind kind, Color tint)
        {
            Vector3 face = new Vector3(0f, 0.55f, -0.46f);
            Material glow = MaterialFactory.GetGlow(tint, 1.2f);
            if (kind == Kind.Or)
            {
                GameObject coin = Proto.Cylinder(t, face, new Vector3(0.4f, 0.02f, 0.4f), tint, "Piece");
                coin.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                coin.GetComponent<Renderer>().sharedMaterial = glow;
            }
            else if (kind == Kind.Bucheron)
            {
                for (int i = -1; i <= 1; i += 2)
                {
                    GameObject log = Proto.Cube(t, face, new Vector3(0.5f, 0.1f, 0.02f), tint, "Hache");
                    log.transform.localRotation = Quaternion.Euler(0f, 0f, 35f * i);
                    log.GetComponent<Renderer>().sharedMaterial = glow;
                }
            }
            else
            {
                GameObject moon = Proto.Cube(t, face, new Vector3(0.3f, 0.3f, 0.02f), tint, "Lune");
                moon.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                moon.GetComponent<Renderer>().sharedMaterial = glow;
            }
        }

        void OnDestroy()
        {
            All.Remove(this);
        }

        // ================================================================== la prise

        /// <summary>Un gardien debout a moins de 12 m : l'autel ne se prend pas.</summary>
        public bool Guarded
        {
            get
            {
                for (int i = 0; i < guardians.Count; i++)
                {
                    Beast b = guardians[i];
                    if (b != null && b.Alive && Flat(b.transform.position - transform.position).magnitude < 12f) return true;
                }
                return false;
            }
        }

        void Update()
        {
            Season season = Game.Season;
            if (season == null || !season.Running) return;
            float dt = Time.deltaTime;

            // Qui se tient dans le cercle ?
            Seeker alone = null;
            int count = 0;
            playerInside = false;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == null || !s.Alive || s.Body == null) continue;
                if (Flat(s.Body.position - transform.position).magnitude > Radius) continue;
                count++;
                alone = s;
                if (s.IsPlayer) playerInside = true;
            }

            if (count == 1 && alone != Owner && !Guarded)
            {
                // Un autre que le maitre : il neutralise d'abord (la jauge du maitre
                // redescend), puis il prend.
                if (Taker != alone)
                {
                    if (Progress > 0f && Taker != null) Progress = Mathf.Max(0f, Progress - dt / CaptureSeconds * 2f);
                    if (Progress <= 0f) Taker = alone;
                }
                else
                {
                    Progress += dt / CaptureSeconds;
                    if (Progress >= 1f) Capture(alone);
                }
            }
            else if (count == 0 && Taker != null && Taker != Owner)
            {
                Progress = Mathf.Max(0f, Progress - dt / CaptureSeconds * 0.5f);
                if (Progress <= 0f) Taker = null;
            }

            // Il paie son maitre.
            if (Owner != null)
            {
                payTimer -= dt;
                if (payTimer <= 0f)
                {
                    payTimer = PayEvery;
                    Pay(Owner);
                }
            }
        }

        void Capture(Seeker s)
        {
            Seeker before = Owner;
            Owner = s;
            Taker = s;
            Progress = 1f;
            payTimer = PayEvery;
            Paint(s.Colour);
            Transform player = Game.PlayerTransform;
            bool near = player != null && Flat(player.position - transform.position).magnitude < 60f;
            if (near) Sfx.Discovery();
            if (s.IsPlayer)
            {
                if (Game.Hud != null)
                    Game.Hud.ShowDiscovery("TU TIENS", Name(kind), "Il te donne " + Gift(kind) + " toutes les 40 secondes.",
                                           "Tant que personne ne te le reprend.", Tint(kind));
            }
            else if (before == Game.Me)
                Toasts.Show(s.Name + " t'a pris ton " + Name(kind) + ".", s.Colour);
            else
                Toasts.Show(s.Name + " tient desormais l'" + Name(kind) + ".", s.Colour);
        }

        void Paint(Color c)
        {
            Material cloth = MaterialFactory.Get(c);
            Material glowing = MaterialFactory.GetGlow(c, 1.4f);
            for (int i = 0; i < flags.Length; i++)
            {
                if (flags[i] == null) continue;
                flags[i].sharedMaterial = flags[i].name == "Rune" ? glowing : cloth;
            }
            if (bowl != null) bowl.sharedMaterial = MaterialFactory.GetGlow(c, 2.2f);
            if (fire != null) { fire.color = c; fire.intensity = 1.4f; }
        }

        void Pay(Seeker s)
        {
            Hoard h = s.Hoard;
            if (kind == Kind.Or) s.Money.Add(5);
            else if (h.Store != null)
            {
                ResourceType t = kind == Kind.Bucheron ? ResourceType.Deadwood : ResourceType.Moonstone;
                h.Store.Contents.TryAdd(t, kind == Kind.Bucheron ? 4 : 2);
            }
            if (s.IsPlayer)
                Toasts.Show(Name(kind) + " : +" + Gift(kind) + ".", Tint(kind));
        }

        // ================================================================== affichage

        void OnGUI()
        {
            if (!playerInside || Game.Season == null || !Game.Season.Running) return;
            if (Game.Menus != null && Game.Menus.Blocking) return;
            UiStyle.Ensure();
            float w = UiStyle.S(360);
            Rect box = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.24f, w, UiStyle.S(64));
            GUI.Box(box, GUIContent.none, UiStyle.CardBox);
            string state;
            Color tint = Tint(kind);
            if (Guarded) state = "Ses gardiens veillent : abats les revenants.";
            else if (Owner == Game.Me) state = "Il est a toi. Il te donne " + Gift(kind) + ".";
            else if (Taker == Game.Me) state = "Tu le prends...  " + Mathf.RoundToInt(Progress * 100f) + " %";
            else if (Taker != null && Progress > 0f) state = "Tu effaces la marque de " + Taker.Name + "...";
            else state = "Reste seul dans le cercle pour le prendre.";
            UiStyle.Tinted(new Rect(box.x + UiStyle.S(16), box.y + UiStyle.S(6), w, UiStyle.S(22)), Name(kind).ToUpperInvariant(), UiStyle.Head, tint);
            UiStyle.Tinted(new Rect(box.x + UiStyle.S(16), box.y + UiStyle.S(28), w - UiStyle.S(32), UiStyle.S(18)), state, UiStyle.Small, UiStyle.Ink);
            float p = Taker == Game.Me || Owner == Game.Me ? Progress : Taker != null ? Progress : 0f;
            UiStyle.Bar(new Rect(box.x + UiStyle.S(16), box.yMax - UiStyle.S(12), w - UiStyle.S(32), UiStyle.S(6)), p,
                        Taker != null ? Taker.Colour : tint, UiStyle.BarBg);
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
